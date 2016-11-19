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
	void OnMarkerMadeVisible(ARMarker marker)
	{
		UpdateTitleText();
	}

	static int lastUpdateSecond = 0;
	void UpdateTitleText()
	{
		// Update every .. 2 seconds
		int secondNow = (int) timeNowSeconds;
		if (secondNow == lastUpdateSecond)
			return;
		DownloadJSON();
		
		lastUpdateSecond = secondNow;

		String textToUpdate = "test "+iterations;
		if (jsonDownload.isDone)
		{
			textToUpdate = jsonDownload.text;
		}
		textToUpdate = GetTitle();

		GameObject textObject = GameObject.Find("TitleText");
		if (textObject == null)
		{
			Debug.Log("Yeaheayag no title text D::::");
			return;
		}
		TextMesh textMesh = textObject.GetComponent<TextMesh>();
		iterations += 1;
     	textMesh.text = textToUpdate;		
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


	String GetTitle()
	{

		String json = "{\"name\": \"evergreen\",  \"dependencies\": {\"body-parser\": \"~1.15.2\",\"cookie-parser\": \"~1.4.3\", } }";

		String text = json;
		if (jsonDownload.isDone)
			text = jsonDownload.text;
		JSONObject jsonObj = new JSONObject(text);
		String resp = "Resp: ";
		String keys = "Keys: ";
		for (int i = 0; i < jsonObj.Count; ++i)
		{
			JSONObject lang = jsonObj[i];
			for (int j = 0; j < lang.keys.Count; ++j)
			{
				keys += lang.keys[j]+", ";
			}

			resp += lang.str;
		}
		resp = "Type: "+jsonObj.type+" "+ keys + resp;
		return resp;
		/*
		JSONObject responses = jsonObj.GetField("header");
		for (int i = 0; i < responses.Count; ++i)
		{
			JSONObject resp = responses[i];
			return resp.str;
		}
		return "No responses";
	*/
	}

	// Download JSON once / or when config changes?
	void DownloadJSON()
	{
		if (jsonDownload != null)
			return;
		String url = "https://raw.githubusercontent.com/erenik/ArcticRiders/master/server/data.json";
		url = "http://54.212.196.65:5000/api/getDetails/4";
		// url = "http://www.google.come";
		jsonDownload = new WWW(url);
	}
}

