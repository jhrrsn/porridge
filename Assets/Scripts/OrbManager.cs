using UnityEngine;
using System.Collections;

public class OrbManager : MonoBehaviour {

	private int level;

	// Use this for initialization
	void Start () {
		level = Application.loadedLevel + 1;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnOrbCharged () {
		Invoke ("OrbActions", 1f);
	}

	void OrbActions() {
		// Open Level Exit Door
		GameObject door = GameObject.FindGameObjectWithTag("door");
		door.BroadcastMessage("OpenDoor");
		
		// Level 2
		if (level == 2) {
			Physics2D.gravity = new Vector2 (0f, -9.81f);
		}
	}
}
