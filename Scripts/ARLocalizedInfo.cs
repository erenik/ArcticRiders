/*
 *  ARTrackedObject.cs
 *  ARToolKit for Unity
 *
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ARToolKit for Unity is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with ARToolKit for Unity.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  As a special exception, the copyright holders of this library give you
 *  permission to link this library with independent modules to produce an
 *  executable, regardless of the license terms of these independent modules, and to
 *  copy and distribute the resulting executable under terms of your choice,
 *  provided that you also meet, for each linked independent module, the terms and
 *  conditions of the license of that module. An independent module is a module
 *  which is neither derived from nor based on this library. If you modify this
 *  library, you may extend this exception to your version of the library, but you
 *  are not obligated to do so. If you do not wish to do so, delete this exception
 *  statement from your version.
 *
 *  Copyright 2015 Daqri, LLC.
 *  Copyright 2010-2015 ARToolworks, Inc.
 *
 *  Author(s): Philip Lamb
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(Transform))]
[ExecuteInEditMode]
public class ARLocalizedInfo : MonoBehaviour
{
	private const string LogTag = "ARLocalizedInfo: ";

	private AROrigin _origin = null;
	private ARMarker _marker = null;

	private bool visible = false;					// Current visibility from tracking
	private float timeTrackingLost = 0;				// Time when tracking was last lost
	public float secondsToRemainVisible = 0.0f;		// How long to remain visible after tracking is lost (to reduce flicker)
	private bool visibleOrRemain = false;			// Whether to show the content (based on above variables)

	public GameObject eventReceiver;

    public String language = "english";
    public String errorString = "";

	// Private fields with accessors.
	[SerializeField]
	private string _markerTag = "";					// Unique tag for the marker to get tracking from
	
	public string MarkerTag
	{
		get
		{
			return _markerTag;
		}
		
		set
		{
			_markerTag = value;
			_marker = null;
		}
	}

	// Return the marker associated with this component.
	// Uses cached value if available, otherwise performs a find operation.
	public virtual ARMarker GetMarker()
	{
		if (_marker == null) {
			// Locate the marker identified by the tag
			ARMarker[] ms = FindObjectsOfType<ARMarker>();
			foreach (ARMarker m in ms) {
				if (m.Tag == _markerTag) {
					_marker = m;
					break;
				}
			}
		}
		return _marker;
	}

	// Return the origin associated with this component.
	// Uses cached value if available, otherwise performs a find operation.
	public virtual AROrigin GetOrigin()
	{
		if (_origin == null) {
			// Locate the origin in parent.
			_origin = this.gameObject.GetComponentInParent<AROrigin>(); // Unity v4.5 and later.
		}
		return _origin;
	}

	WWW jsonDownload = null;
	void Start()
	{
		//ARController.Log(LogTag + "Start()");

		if (Application.isPlaying) {
			// In Player, set initial visibility to not visible.
			for (int i = 0; i < this.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(false);
		} else {
			// In Editor, set initial visibility to visible.
			for (int i = 0; i < this.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(true);
		}
	}

	static float timeNowSeconds = 0;
	// Use LateUpdate to be sure the ARMarker has updated before we try and use the transformation.
	void LateUpdate()
	{
		// Local scale is always 1 for now
		transform.localScale = Vector3.one;
		
		// Update tracking if we are running in the Player.
		if (Application.isPlaying) {

			// Sanity check, make sure we have an AROrigin in parent hierachy.
			AROrigin origin = GetOrigin();
			if (origin == null) {
				//visible = visibleOrRemain = false;
				return;
			} 
			// Sanity check, make sure we have an ARMarker assigned.
			ARMarker marker = GetMarker();
			if (marker == null) {
				//visible = visibleOrRemain = false;
				return;
			} 

			// Note the current time
			timeNowSeconds = Time.realtimeSinceStartup;
			
			ARMarker baseMarker = origin.GetBaseMarker();
			if (baseMarker != null && marker.Visible) 
			{
				if (!visible) {
					// Marker was hidden but now is visible.
					visible = visibleOrRemain = true;
					if (eventReceiver != null) eventReceiver.BroadcastMessage("OnMarkerFound", marker, SendMessageOptions.DontRequireReceiver);
					for (int i = 0; i < this.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(true);
				}

		        Matrix4x4 pose;
		        if (marker == baseMarker) {
		            // If this marker is the base, no need to take base inverse etc.
		            pose = origin.transform.localToWorldMatrix;
		        } else {
				    pose = (origin.transform.localToWorldMatrix * baseMarker.TransformationMatrix.inverse * marker.TransformationMatrix);
				}
				transform.position = ARUtilityFunctions.PositionFromMatrix(pose);
				transform.rotation = ARUtilityFunctions.QuaternionFromMatrix(pose);

				if (eventReceiver != null) eventReceiver.BroadcastMessage("OnMarkerTracked", marker, SendMessageOptions.DontRequireReceiver);
				OnMarkerMadeVisible(marker);
			} else {
				OnMarkerLost(marker);
			}

		} // Application.isPlaying

	}

	static int iterations = 0;
	static int lastUpdateSecond = 0;
	void OnMarkerMadeVisible(ARMarker marker)
	{
		// Update every .. 2 seconds
		int secondNow = (int) timeNowSeconds;
		if (secondNow == lastUpdateSecond)
			return;
		lastUpdateSecond = secondNow;
		DownloadJSON();
		// Update UI
		UpdateTitleText();
		UpdateDescription();
	}


    bool showingDescription = true;
    bool ShowingOffers()
    { 
    	return !showingDescription; 
    }
	public void OnOffersButtonClicked()
	{
        showingDescription = !showingDescription;
        UpdateDescription();
        GameObject textObject = GameObject.Find("OffersText");
        textObject.GetComponent<TextMesh>().text = showingDescription? "Current Offers" : "Description"; 
	}
	void UpdateTitleText()
	{		
		String textToUpdate = "test "+iterations;
		textToUpdate = GetTitle();
		GameObject textObject = GameObject.Find("TitleText");
		TextMesh textMesh = textObject.GetComponent<TextMesh>();
		iterations += 1;
     	textMesh.text = textToUpdate;		
	}

	public void UpdateDescription()
	{
		GameObject textObject = GameObject.Find("Description");
		if (textObject == null)
		{
			Debug.Log("Yeaheayag no title text D::::");
			return;
		}
		TextMesh tm = textObject.GetComponent<TextMesh>(); 
		if (ShowingOffers())
			tm.text = "Very offerious!";
		tm.text = GetDescription();
	}
	void OnMarkerLost(ARMarker marker)
	{
		if (visible) {
			// Marker was visible but now is hidden.
			visible = false;
			timeTrackingLost = timeNowSeconds;
		}

		if (visibleOrRemain && (timeNowSeconds - timeTrackingLost >= secondsToRemainVisible)) {
			visibleOrRemain = false;
			if (eventReceiver != null) eventReceiver.BroadcastMessage("OnMarkerLost", marker, SendMessageOptions.DontRequireReceiver);
			for (int i = 0; i < this.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(false);
		}
	}

	JSONObject GetLocalizedData()
	{
		if (JSONDownloaded() == false)
		{
			errorString = "Not downloaded yet, progress: "+DownloadProgress();
			return null;
		}	
		JSONObject jsonObj = new JSONObject(jsonDownload.text);
        String res = "";
        JSONObject data = jsonObj.GetField(language);
        if (data == null)
        {
            errorString = "Could not find language: "+language+" "+JSONKeys(jsonObj);
			return null;
        }
        for (int i = 0; i < data.Count; i++)
        {
        	if (data.keys[i].Equals("localized_data", StringComparison.Ordinal))
        	{
        		return data.list[i];
            }
        }
        return null;
	}
	String JSONKeys(JSONObject obj)
	{
		String keys = "Values: ";
		for (int i = 0; i < obj.keys.Count; ++i)
		{
			keys += ", "+obj.keys[i];
		}
		return keys;
	}
	String GetTitle()
	{
		JSONObject localizedData = GetLocalizedData();
		if (localizedData == null)
			return "Error: "+errorString;
		for (int i = 0; i < localizedData.Count; ++i)
		{
			if (localizedData.keys[i].Equals("header", StringComparison.Ordinal))
				return localizedData.list[i].str;
		}
        return "Title could not be found";
	}
	String GetDescription()
	{
		JSONObject localizedData = GetLocalizedData();
		if (localizedData == null)
			return "Error: "+errorString;
		for (int i = 0; i < localizedData.Count; ++i)
		{
			if (localizedData.keys[i].Equals("description", StringComparison.Ordinal))
				return localizedData.list[i].str;
		}
        return "Description could not be found";
	}
	bool JSONDownloaded()
	{
		if (jsonDownload == null)
			return false;
		if (jsonDownload.isDone)
			return true;
		return false;
	}

	// Download JSON once / or when config changes?
	void DownloadJSON()
	{
		if (jsonDownload != null)
			return;
		String url = "https://raw.githubusercontent.com/erenik/ArcticRiders/master/server/data.json";
		url = "http://54.212.196.65:5000/api/getDetails/1";
		// url = "http://www.google.come";
		jsonDownload = new WWW(url);
	}

	float DownloadProgress()
	{
		if (jsonDownload == null)
			return -1;
		return jsonDownload.progress;
	}
	int JSONDownloadSize()
	{
		if (JSONDownloaded())
			return jsonDownload.bytesDownloaded;
		return -1;
	}
}

