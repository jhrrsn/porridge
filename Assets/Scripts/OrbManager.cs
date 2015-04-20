using UnityEngine;
using System.Collections;

public class OrbManager : MonoBehaviour {

	public GameObject arrows;

	private int level;

	// Use this for initialization
	void Start () {
		level = Application.loadedLevel;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnOrbCharged () {
		Invoke ("OrbActions", 1f);
	}

	void OrbActions() {
		// Open Level Exit Door
		Invoke ("OpenDoor", 0.5f);
		arrows.SetActive (true);
		
		// Level 2
		if (level == 2) {
			Physics2D.gravity = new Vector2 (0f, -9.81f);
		}
	}

	void OpenDoor() {
		GameObject door = GameObject.FindGameObjectWithTag("door");
		door.BroadcastMessage("OpenDoor");
	}
}
