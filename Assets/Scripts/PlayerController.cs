using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour {
	
	public float moveForce;
	public float moveMaxSpeed;
	public float jetForce;
	public float jetMaxSpeed;
	public float jetDrain;
	public float batteryCapacity;
	public float batteryRecharge;
	public float batteryChargeDelay;
	public float uprightSpeed;

	public Text batteryText;

	public float searchBeamWidth;
	public float searchBeamIntensity;
	public float focusBeamWidth;
	public float focusBeamIntensity;
	public float beamTweening;

	public GameObject spotlight;
	public Light2D.LightSprite spotlightBeam;
	public Light2D.LightSprite eyeLight;
	public Light2D.LightSprite glow;

	static public bool facingRight;

	private Quaternion upright;
	private float batteryTimeLastUsed;
	private float batteryPower;
	private bool outOfPower;
	private bool moving;
	private Rigidbody2D rb;
	private SpriteRenderer rend;
	
	// Use this for initialization
	void Start () {
		upright = transform.rotation;
		outOfPower = false;
		moving = false;
		batteryPower = batteryCapacity;
		batteryTimeLastUsed = Time.time;
		rb = GetComponent<Rigidbody2D> ();
		rend = GetComponent<SpriteRenderer> ();
	}


	void Update () {
	
		Vector3 beamScale = spotlight.gameObject.transform.localScale;
		Color beamColor = spotlightBeam.Color;

		if (Input.GetButton ("Fire1")) {
			if (beamScale.x > focusBeamWidth) {
				beamScale.x -= Time.deltaTime * beamTweening;
				spotlight.gameObject.transform.localScale = beamScale;
			}
		} else if (beamScale.x < searchBeamWidth) {
				beamScale.x += Time.deltaTime * beamTweening;
				spotlight.gameObject.transform.localScale = beamScale;
		} 

		if (Input.GetButton ("Fire1")) {
			if (beamColor.a < focusBeamIntensity) {
				beamColor.a += Time.deltaTime;
				spotlightBeam.Color = beamColor;
			}
		} else if (beamColor.a > searchBeamIntensity) {
				beamColor.a -= Time.deltaTime;
				spotlightBeam.Color = beamColor;
		}
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");

//		Debug.Log (batteryPower + ", " + h + ", " + v);

		// Check facing
		if (!outOfPower & rb.fixedAngle) {
			if (!spotlight.activeSelf) {
				spotlight.SetActive(true);
			}
			Vector3 mousePos = Input.mousePosition;

			if (facingRight && mousePos.x < Screen.width/2) {
				Flip ();
			} else if (!facingRight && mousePos.x > Screen.width/2) {
				Flip ();
			}
		}

		if (!outOfPower && batteryPower >= 0.1f && (Mathf.Abs (h) > 0 || v > 0)) {

			// Self-righting rotation adjustment

			transform.rotation = Quaternion.RotateTowards(transform.rotation, upright, uprightSpeed);

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
			rb.AddForce (forceVector);

			// Reduce battery power proportional to the magnitude of the force vector
			batteryPower -= forceVector.magnitude * jetDrain;
			SetBatteryText();
			batteryTimeLastUsed = Time.time;
		} 
		else if (batteryPower < 0.1f) {
			outOfPower = true;
			rb.fixedAngle = false;
			spotlight.SetActive(false);
			Color lightColor = new Color (1f, 0.5f, 0.5f, 0.4f);
			eyeLight.Color = lightColor;
			glow.Color = lightColor;
			rend.color = new Color (0.3f, 0.3f, 0.3f);
		}


		if (rb.velocity.x == 0f && rb.velocity.y == 0f) {
			moving = false;
		}

		Debug.Log (rb.velocity.x + ", " + rb.velocity.y + ", " + moving);

		if (!moving && batteryPower < batteryCapacity && Time.time - batteryTimeLastUsed > batteryChargeDelay) {
			SetBatteryText();
			batteryPower += batteryRecharge;
		} 
		else if (batteryPower >= batteryCapacity) {
			outOfPower = false;
			rend.color = new Color (1, 1, 1);
			Color lightColor = new Color (0.85f, 1f, 1f, 0.4f);
			eyeLight.Color = lightColor;
			glow.Color = lightColor;
			batteryPower = batteryCapacity;
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

	void SetBatteryText() {
		float batteryPercentage = Mathf.Round ((batteryPower / batteryCapacity) * 100);
		batteryText.text = batteryPercentage.ToString() + "%";
		if (batteryPercentage < 1f) {
			batteryText.color = new Color (1f, 0.6f, 0.6f);
		} else if (batteryPercentage > 99f) {
			batteryText.color = new Color (1f, 1f, 1f);
		}
	}
}