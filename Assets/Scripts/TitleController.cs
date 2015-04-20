using UnityEngine;
using System.Collections;

public class TitleController : MonoBehaviour {

	public GameObject titleScreen;
	public AudioClip click;

	private AudioSource sfx;

	void Start () {
		sfx = GetComponent<AudioSource> ();
		sfx.pitch = 1f;
		sfx.PlayOneShot(click);
	}

	// Update is called once per frame
	void Update () {

		if (Application.loadedLevel == 5 && Time.timeSinceLevelLoad > 10f) {
			Application.Quit();
		}
		else if (Application.loadedLevel == 0 && Time.timeSinceLevelLoad > 16f) {
			Application.LoadLevel(1);
		}
		else if (Application.loadedLevel == 0 && Time.timeSinceLevelLoad > 5f) {
			if (titleScreen.activeSelf) {
				sfx.pitch = 0.8f;
				sfx.PlayOneShot(click);
			}
			titleScreen.SetActive(false);
		}
	}
}
