using UnityEngine;
using System.Collections;

public class OrbController : MonoBehaviour {

	public float zeroChargeLight;
	public float fullChargeLight;
	public float chargeRate;
	public Collider2D focusBeam;
	public Light2D.LightSprite orbGlow;
	public AudioClip[] chargeSFX; 

	private float chargeLevel;
	private float maxCharge = 100f;
	private AudioSource orbSFX;
	private bool charging;
	private bool firstChargeLevel;
	private bool secondChargeLevel;
	private bool fullyCharged;

	// Use this for initialization
	void Start () {
		orbSFX = GetComponent<AudioSource> ();
		chargeLevel = 0f;
		charging = false;
		firstChargeLevel = false;
		secondChargeLevel = false;
		fullyCharged = false;
	}
	
	// Update is called once per frame
	void Update () {
		if (!fullyCharged && charging && !GetComponent<Collider2D>().IsTouching(focusBeam)) {
			charging = false;
		}

		if (!fullyCharged && !charging && chargeLevel > 0f) {
			chargeLevel -= (2 * chargeRate * Time.deltaTime);
			if (chargeLevel < 10f && firstChargeLevel) {
				firstChargeLevel = false;
			} else if (chargeLevel < 50f && secondChargeLevel) {
				secondChargeLevel = false;
			}
		}

		ManageLightIntensity ();
	}

	void OnTriggerStay2D(Collider2D other) {
		if (!fullyCharged && other.gameObject.tag == "focusBeam") {
			charging = true;
			if (chargeLevel >= maxCharge) {
				fullyCharged = true;
				orbSFX.PlayOneShot(chargeSFX[2]);
				charging = false;
			} else {
				chargeLevel += (chargeRate * Time.deltaTime);
				if (chargeLevel > 10f && !firstChargeLevel) {
					firstChargeLevel = true;
					orbSFX.PlayOneShot(chargeSFX[0]);
				} else if (chargeLevel > 50f && !secondChargeLevel) {
					secondChargeLevel = true;
					orbSFX.PlayOneShot(chargeSFX[1]);
				}
			}
		}
	}

	void ManageLightIntensity() {
		float intensity = map (chargeLevel, 0f, 100f, zeroChargeLight, fullChargeLight);
		orbGlow.Color = new Color (0.7f, 0.9f, 1f, intensity);
	}

	float map(float s, float a1, float a2, float b1, float b2) {
		return b1 + (s-a1)*(b2-b1)/(a2-a1);
	}
}
