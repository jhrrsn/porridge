using UnityEngine;
using System.Collections;

public class BoxController : MonoBehaviour {

	public AudioClip[] collisionClips;

	private AudioSource boxSFX;

	// Use this for initialization
	void Start () {
		boxSFX = GetComponent<AudioSource>();
		boxSFX.pitch = 0.8f;
	}

	void OnCollisionEnter2D(Collision2D other) {
		if (other.gameObject.tag != "player") {
			int clip = Random.Range (0, 2);
			boxSFX.PlayOneShot (collisionClips [clip]);
		}
	}
}
