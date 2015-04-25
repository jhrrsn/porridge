using UnityEngine;
using System.Collections;

public class AvaController : MonoBehaviour {

	public AudioClip wakeUpClip;
	public AudioClip onAwakeClip;
	public AudioClip spotlightFirstClip;
	public AudioClip lowGClip;
	public AudioClip carefulClip;
	public AudioClip orbClip;
	public AudioClip orbNotEnough;
	public AudioClip doorOpenClip;
	public AudioClip gravityOnlineClip;


	private AudioSource speechSource;

	// Use this for initialization
	void Awake () {
		speechSource = GetComponent<AudioSource>();
	}

	void PlayClip(string clipToPlay) {
		if (clipToPlay == "wakeUpClip")
			speechSource.PlayOneShot (wakeUpClip);
		else if (clipToPlay == "orbClip")
			speechSource.PlayOneShot (orbClip);
		else if (clipToPlay == "orbNotEnough")
			Invoke ("PlayOrbNotEnough", 1f);
		else if (clipToPlay == "doorOpenClip")
			Invoke ("PlayDoorOpenClip", 2f);
		else if (clipToPlay == "onAwakeClip") 
			Invoke ("PlayOnAwakeClip", 2f);
		else if (clipToPlay == "spotlightFirstClip") 
			Invoke ("PlaySpotlightFirstClip", 1f);
		else if (clipToPlay == "gravityOnlineClip")  
			Invoke ("PlayGravityOnlineClip", 1.5f);
		else if (clipToPlay == "carefulClip")
			Invoke ("PlayCarefulClip", 2f);
	}

	void PlayOnAwakeClip() {
		speechSource.PlayOneShot (onAwakeClip);
	}

	void PlaySpotlightFirstClip() {
		speechSource.PlayOneShot (spotlightFirstClip);
	}

	void PlayOrbNotEnough() {
		speechSource.PlayOneShot (orbNotEnough);
	}

	void PlayDoorOpenClip() {
		speechSource.PlayOneShot (doorOpenClip);
	}

	void PlayGravityOnlineClip() {
		speechSource.PlayOneShot (gravityOnlineClip);
	}

	void PlayCarefulClip() {
		speechSource.PlayOneShot (carefulClip);
	}
}
