using UnityEngine;
using System.Collections;

public class SquidController : MonoBehaviour {

	public Transform player;
	public GameObject spotlight;
	public float flySpeed;
	public AudioClip[] noises;
	public float pursuitDistance;
	public Light2D.LightSprite glow;

	private AudioSource squidSFX;
	private bool returning;
	private Vector2 startPosition;
	private int state; // 0 = inactive, 1 = pursuing, 2 = returning, 3 = fleeing

	// Use this for initialization
	void Start () {
		state = 0;
		startPosition = transform.position;
		squidSFX = GetComponent<AudioSource> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (((state == 0 || state == 2) && (spotlight.activeSelf && Vector2.Distance (transform.position, player.position) < pursuitDistance)) || (state == 0 && Vector2.Distance (transform.position, player.position) < pursuitDistance/10f) ) {
			squidSFX.PlayOneShot(noises[2]);
			state = 1;
			glow.Color = new Color(1f, 0.8f, 0.7f, 1f);
		} else if (state == 1 && !spotlight.activeSelf) {
			squidSFX.PlayOneShot(noises[1]);
			state = 2;
			glow.Color = new Color(1f, 0.8f, 0.7f, 0.5f);
		}
	}

	void FixedUpdate () {
		if (state == 1) {
			transform.position = Vector2.MoveTowards (transform.position, player.position, flySpeed);
		} else if (state == 2 && Vector2.Distance (transform.position, startPosition) > 0) {
			transform.position = Vector2.MoveTowards (transform.position, startPosition, flySpeed * 0.4f);
		} else if (state == 3 && Vector2.Distance (transform.position, startPosition) > 0) {
			transform.position = Vector2.MoveTowards (transform.position, startPosition, flySpeed);
		} else if (state >= 2) {
			squidSFX.PlayOneShot(noises[0]);
			glow.Color = new Color(1f, 0.8f, 0.7f, 0.6f);
			state = 0;
		}
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (collision.gameObject.tag == "player") {
			state = 2;
			// play 'zap/drain' sfx
		}
	}

	void OnTriggerEnter2D(Collider2D other) {
		if (other.gameObject.tag == "focusBeam") {
			state = 3;
			//play 'pain/burn' sfx
		}
	}
}
