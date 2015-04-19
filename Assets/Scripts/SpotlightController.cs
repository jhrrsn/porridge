using UnityEngine;
using System.Collections;

public class SpotlightController : MonoBehaviour {

	private float lockPos;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		float lookAdj = 270f;
		float lookRot;

		if (PlayerController.facingRight) {
			lookRot = -1f;
		} else {
			lookRot = 1f;
		}

		var pos = Camera.main.WorldToScreenPoint(transform.position);
		var dir = Input.mousePosition - pos;
		var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.AngleAxis(angle+lookAdj, Vector3.forward * lookRot); 

//		Vector2 mouseAdjusted = new Vector2 (Input.mousePosition.x - Screen.width / 2, Input.mousePosition.y - Screen.height / 2);
//
//
//		Vector2 lookDir = Vector2.zero - mouseAdjusted;
//
//		Debug.Log (lookDir);
//
//		transform.rotation = Quaternion.LookRotation (-lookDir, Vector3.forward);
//		transform.eulerAngles = new Vector3(0, 0,transform.eulerAngles.z);
//
//		Debug.Log (transform.rotation.z);
//
//		Vector3 mousePosition = Input.mousePosition;           
//		mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
//		Debug.Log (mousePosition);
//		Quaternion rot = Quaternion.LookRotation(transform.position - mousePosition, Vector3.forward );
//		transform.rotation = rot;  
//		transform.eulerAngles = new Vector3(0, 0,transform.eulerAngles.z);
	}
}
