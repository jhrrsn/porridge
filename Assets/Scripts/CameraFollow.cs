using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour {

	public Transform target;
	public float smooth;

	private float cameraDistance;

	void Start () {
		cameraDistance = transform.position.z;
	}

	void FixedUpdate () {
//		Vector3 m = Camera.main.ScreenToWorldPoint (Input.mousePosition);
//		Vector3 mNormal = Vector3.Normalize (transform.position - m)/2;
//		transform.position = Vector3.Lerp (transform.position, new Vector3 (target.position.x - mNormal.x, target.position.y - mNormal.y, -1), Time.deltaTime * smooth);
		transform.position = new Vector3 (target.position.x, target.position.y, cameraDistance);
	}
}