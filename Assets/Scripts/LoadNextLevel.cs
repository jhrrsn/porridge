using UnityEngine;
using System.Collections;

public class LoadNextLevel : MonoBehaviour {

	public int nextLevel;
	public float loadDelay;

	// Use this for initialization
	void OnTriggerEnter2D(Collider2D other) {
		if (other.gameObject.tag == "player" && Time.timeSinceLevelLoad > 10f) {
			Invoke("LoadLevel", loadDelay);
		}
	}

	void LoadLevel() {
		Application.LoadLevel(nextLevel);
	}
}
