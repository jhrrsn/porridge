using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour {

	public AudioClip doorClip;
	public AudioClip closeClip;

	private AudioSource doorSFX;

	// Update is called once per frame
	void OpenDoor () {
		doorSFX = GetComponent<AudioSource> ();
		doorSFX.PlayOneShot (doorClip);
		GetComponent<Renderer> ().enabled = false;
		GetComponent<BoxCollider2D> ().enabled = false;
		gameObject.GetComponentInChildren<MeshRenderer> ().enabled = false;
	}

	void PlayOpenSFX () {
		doorSFX = GetComponent<AudioSource> ();
		doorSFX.PlayOneShot (closeClip);
	}
}
