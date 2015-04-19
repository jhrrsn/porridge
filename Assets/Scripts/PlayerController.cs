using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour {
	
	public float moveForce;
	public float moveMaxSpeed;
	public float jetForce;
	public float jetMaxSpeed;
	public float batteryDrain;
	public float batteryMaxCapacity;
	public float batteryRecharge;
	public float batteryChargeDelay;
	public float uprightSpeed;

	public Text batteryText;

	public float searchBeamWidth;
	public float searchBeamIntensity;
	public float searchBeamPassiveUse;
	public float focusBeamWidth;
	public float focusBeamIntensity;
	public float focusBeamDrain;
	public float beamTweening;

	public AudioClip spotlightAudio;
	public AudioClip hoverAudio;
	public AudioClip[] collisionClips;

	public GameObject spotlight;
	public Light2D.LightSprite spotlightBeam;
	public Light2D.LightSprite eyeLight;

	static public bool facingRight;

	private AudioSource miscSFX;
	private AudioSource hoverSFX;
	private Quaternion upright;
	private float batteryCapacity;
	private float batteryTimeLastUsed;
	private float batteryPower;
	private float engineUsage;
	private float hoverClipPlaytime;
	private bool focusBeamActive;
	private bool spotlightOn;
	private bool outOfPower;
	private bool moving;
	private bool hoverClipPlaying;
	private Rigidbody2D rb;
	private SpriteRenderer rend;
	
	// Use this for initialization
	void Start () {
		upright = transform.rotation;
		outOfPower = false;
		moving = false;
		hoverClipPlaying = false;
		spotlightOn = false;
		focusBeamActive = false;
		batteryCapacity = batteryMaxCapacity;
		batteryPower = batteryCapacity;
		engineUsage = 0f;
		batteryTimeLastUsed = Time.time;
		rb = GetComponent<Rigidbody2D> ();
		rend = GetComponent<SpriteRenderer> ();
		AudioSource [] audioSources = GetComponents<AudioSource>();
		miscSFX = audioSources [0];
		miscSFX.volume = 0.8f;
		hoverSFX = audioSources [1];
	}


	void Update () {

		if (Input.GetButtonDown("LightToggle")) {
			SpotlightToggle();
		}


		if (Input.GetButton("Fire1") && spotlightOn) {
			FocusBeam ();
		} else {
			DefocusBeam();
		}

	}
	
	// Update is called once per frame
	void FixedUpdate () {
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");

		// Check facing
		if (!outOfPower & rb.fixedAngle) {
			if (!spotlight.activeSelf && spotlightOn) {
				spotlight.SetActive(true);
			}
			Vector3 mousePos = Input.mousePosition;

			if (facingRight && mousePos.x < Screen.width/2) {
				Flip ();
			} else if (!facingRight && mousePos.x > Screen.width/2) {
				Flip ();
			}
		}


		if (!outOfPower && batteryPower >= 0.1f && (Mathf.Abs (h) > 0 || Mathf.Abs (v) > 0)) {

			// Self-righting rotation adjustment
			transform.rotation = Quaternion.RotateTowards (transform.rotation, upright, uprightSpeed);

			if (transform.rotation.z < 1f || transform.rotation.z > 359f) {
				rb.fixedAngle = true;
			}


			// Horizontal and vertical movement
			moving = true;

			float xForce;
			float yForce;

			if (rb.velocity.x < moveMaxSpeed) {
				xForce = h * moveForce;
			} else {
				xForce = 0f;
			}

			if (rb.velocity.y < jetMaxSpeed) {
				yForce = v * jetForce;
			} else {
				yForce = 0f;
			}

			// Create force vector from horizontal and vertical inputs
			Vector2 forceVector = new Vector2 (xForce, yForce);
			engineUsage = forceVector.magnitude; // 0 to 20
			rb.AddForce (forceVector);

			// Reduce battery power proportional to the magnitude of the force vector
			batteryPower -= forceVector.magnitude * batteryDrain;
			SetBatteryText ();
			batteryTimeLastUsed = Time.time;
		} else if (batteryPower < 0.1f) {
			engineUsage = 0f;
			PowerDown ();
		} else {
			engineUsage = 0f;
		}

		// Check if not moving
		if (rb.velocity.x == 0f && rb.velocity.y == 0f) {
			moving = false;
		}
	
		// Engine noise
		if (engineUsage > 0f && !hoverClipPlaying) {
			hoverSFX.PlayOneShot (hoverAudio);
			hoverClipPlaytime = Time.time;
			hoverSFX.volume = 1f;
			hoverSFX.pitch = 1f;
			hoverClipPlaying = true;
		} else if (engineUsage > 0f && hoverClipPlaying && Time.time - hoverClipPlaytime > 5f) {
			hoverSFX.pitch = map (engineUsage, 0f, 20f, 0.96f, 1.04f);
		} else if ((engineUsage <= 0f && hoverClipPlaying && Time.time - hoverClipPlaytime > 5f) || outOfPower || !moving) {
			hoverSFX.volume -= Time.deltaTime * 0.1f;
			hoverSFX.pitch -= Time.deltaTime * 0.1f;

			if (hoverSFX.volume <= 0.1f) {
				hoverSFX.Stop();
				hoverClipPlaying = false;
			}
			// reduce pitch and volume, check for volume = 0 and if so set hoverClipPlayer = false; and stop audio clip
		}


		// Recharging

		if (!moving && !focusBeamActive) {
			BatteryRecharge();
		}
	}

	void Flip() {
		// Switch the way the player is labelled as facing
		facingRight = !facingRight;
		
		// Multiply the player's x local scale by -1
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

	void PowerDown() {
		outOfPower = true;
		rb.fixedAngle = false;
		if (spotlightOn) {
			SpotlightToggle();
		}
		Color lightColor = new Color (1f, 0.5f, 0.5f, 1f);
		eyeLight.Color = lightColor;
		rend.color = new Color (0.3f, 0.3f, 0.3f);
	}

	void BatteryRecharge() {
		if (batteryPower < batteryCapacity && Time.time - batteryTimeLastUsed > batteryChargeDelay) {
			SetBatteryText();
			if (outOfPower) {
				batteryPower += batteryRecharge/2f;
			} else {
				batteryPower += batteryRecharge;
			}
		}
		else if (batteryPower >= batteryCapacity) {
			outOfPower = false;
			rend.color = new Color (1, 1, 1);
			Color lightColor = new Color (0.85f, 1f, 1f, 0.6f);
			eyeLight.Color = lightColor;
			batteryPower = batteryCapacity;
		}
	}

	void SetBatteryText() {
		float batteryPercentage = Mathf.Ceil ((batteryPower / batteryMaxCapacity) * 100);
		batteryText.text = batteryPercentage.ToString() + "%";
		if (batteryPercentage < 1f) {
			batteryText.color = new Color (1f, 0.6f, 0.6f);
		} else if (batteryPercentage > 99f) {
			batteryText.color = new Color (1f, 1f, 1f);
		}
	}

	void FocusBeam() {
			Vector3 beamScale = spotlight.gameObject.transform.localScale;
			Color beamColor = spotlightBeam.Color;
			
			if (beamScale.x > focusBeamWidth) {
				beamScale.x -= Time.deltaTime * beamTweening;
				spotlight.gameObject.transform.localScale = beamScale;
			}
			if (beamColor.a < focusBeamIntensity) {
				beamColor.a += Time.deltaTime;
				spotlightBeam.Color = beamColor;
			}

			if (!focusBeamActive && beamScale.x <= focusBeamWidth) {
				BoxCollider2D spotlightTrigger = spotlight.gameObject.GetComponent<BoxCollider2D>();
				spotlightTrigger.enabled = true;
				focusBeamActive = true;
			}

			if (focusBeamActive) {
				batteryPower -= focusBeamDrain * batteryDrain;
				SetBatteryText();
			}
	}

	void DefocusBeam() {
			Vector3 beamScale = spotlight.gameObject.transform.localScale;
			Color beamColor = spotlightBeam.Color;

			if (beamScale.x < searchBeamWidth) {
				beamScale.x += Time.deltaTime * beamTweening;
				spotlight.gameObject.transform.localScale = beamScale;
			} 
			if (beamColor.a > searchBeamIntensity) {
				beamColor.a -= Time.deltaTime;
				spotlightBeam.Color = beamColor;
			}
			if (focusBeamActive) {
				BoxCollider2D spotlightTrigger = spotlight.gameObject.GetComponent<BoxCollider2D>();
				spotlightTrigger.enabled = false;
				focusBeamActive = false;
			}
	}

	void SpotlightToggle() {
		if (spotlightOn) {
			miscSFX.pitch = 0.8f;
			miscSFX.PlayOneShot(spotlightAudio);
			spotlightOn = false;
			spotlight.SetActive(false);
			batteryCapacity = batteryMaxCapacity;
			if (!outOfPower) {
				batteryPower += (batteryCapacity - (batteryMaxCapacity * (1-searchBeamPassiveUse)));
			}
		} else {
			miscSFX.pitch = 1f;
			miscSFX.PlayOneShot(spotlightAudio);
			spotlightOn = true;
			spotlight.SetActive(true);
			batteryCapacity = batteryMaxCapacity * (1-searchBeamPassiveUse);
			batteryPower -= (batteryMaxCapacity - batteryCapacity);

			if (batteryPower <= 0f) {
				batteryPower = 0f;
				SetBatteryText();
				PowerDown();
			} 
		}

		SetBatteryText();
	}

	float map(float s, float a1, float a2, float b1, float b2) {
		return b1 + (s-a1)*(b2-b1)/(a2-a1);
	}

	void OnCollisionEnter2D(Collision2D collision) {
		float rel_v = collision.relativeVelocity.magnitude;
		if (rel_v > 5f || (rel_v > 2 && outOfPower)) {
			miscSFX.pitch = 1f;
			int clip = Random.Range(0, 3);
			miscSFX.PlayOneShot(collisionClips[clip]);
		}
	}
}