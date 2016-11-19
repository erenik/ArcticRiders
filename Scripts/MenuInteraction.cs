using UnityEngine;
using System.Collections;

using System;

[RequireComponent(typeof(Camera))]
public class MenuInteraction : MonoBehaviour {

    public Camera camera;
    public GameObject button;

    // Use this for initialization
    void Start () {
	
	}

    // Update is called once per frame
    void Update() 
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);
            
            Vector3 hitPoint = camera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, camera.transform.position.z));
            RaycastHit hit;
            if (Physics.Raycast(camera.transform.position, (hitPoint - camera.transform.position).normalized,out hit))
            {
                GameObject obj = hit.collider.gameObject;
                if (obj.Equals(button)) {
                    // do something

                    GameObject go = GameObject.Find("QRScene");
                    ARLocalizedInfo other = (ARLocalizedInfo) go.GetComponent(typeof(ARLocalizedInfo));
                    other.OnOffersButtonClicked();
                }
            }
        } 
    }

};



