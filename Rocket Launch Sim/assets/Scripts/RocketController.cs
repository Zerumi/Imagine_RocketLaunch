using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// RocketController - Script that controls rocket movement and physics.
/// </summary>
public class RocketController : MonoBehaviour
{
	Rigidbody myRigidbody = null;

	[SerializeField]
	ParticleSystem exhaustParticles = null;

	[SerializeField]
	AudioClip exhaustAudio = null;
	AudioSource exhaustSource = null;

	[SerializeField]
	AudioClip windAudio = null;
	AudioSource windSource = null;

	[SerializeField]
	float speedForMaxPitch = 100f;
	[SerializeField]
	float speedForMaxVolume = 100f;

	[SerializeField]
	[Range(-3f, 3f)]
	float windMinPitch, windMaxPitch;
	[SerializeField]
	[Range(0f, 1f)]
	float windMinVolume, windMaxVolume;

	float startFuel;
	float currentFuel;

	float initialMass;

	float impulse;

	// Center of pressure and mass values here are fractions of the length of the rocket, from tail to nose
	// Center of pressure is required to be lower than center of mass for a stable, self-correcting rocket
	// Center of pressure is where aerodynamic force is applied -- in this case, drag
	float centerOfPressure = 0.45f;
	// Center of mass is where other forces are applied -- in this case, gravity, impulse from fuel, and wind
	float centerOfMass = 0.55f;

	float bodyLength = 1;

	bool flying = false;
	bool outOfFuel = false;

	Vector3 startPosition;

	void Awake()
	{
		startFuel = 1f;
		initialMass = 1f;
		impulse = 1f;
	}

	void Start()
	{
		exhaustSource = AudioHelper.CreateAudioSource(gameObject, exhaustAudio);
		windSource = AudioHelper.CreateAudioSource(gameObject, windAudio);

		myRigidbody = GetComponent<Rigidbody>();
		startPosition = myRigidbody.position;

		// Set center of mass value directly in physics engine.
		// Unfortunately, Unity does not have a center of pressure built-in.
		bodyLength = GetComponent<CapsuleCollider>().height;
		myRigidbody.centerOfMass = new Vector3(0f, centerOfMass * bodyLength, 0f);

		// Set rocket to initial state
		Reset();
	}
	
	/// <summary>
	/// Resets the rocket to its initial state.
	/// </summary>
	public void Reset()
	{
		// Clear exhaust FX
		exhaustParticles.Stop();
		exhaustParticles.Clear();

		// Move rocket back to start platform
		myRigidbody.transform.position = startPosition;
		myRigidbody.transform.rotation = Quaternion.identity;
		myRigidbody.velocity = Vector3.zero;
		myRigidbody.rotation = Quaternion.identity;
		myRigidbody.angularVelocity = Vector3.zero;
		myRigidbody.isKinematic = true;

		flying = false;
		outOfFuel = false;
		windSource.Stop();
	}

	void FixedUpdate()
	{
		if (flying)
        {
            //Use fuel if avaliable
            if (currentFuel > 0f)
            {
                //Exaust particle FX
                exhaustParticles.Play();

                if (!exhaustParticles.isPlaying)
                {
                    exhaustSource.Play();
                }
                // Aply force down the lenght of the rocket
                myRigidbody.AddRelativeForce(Vector3.up * impulse, ForceMode.Force);

                //Substract fuel
                currentFuel = Mathf.Max(0f, currentFuel - Time.fixedDeltaTime);

                //Update mas based on remaing fuel
                myRigidbody.mass = initialMass + currentFuel;
                if(currentFuel == 0f)
                {
                    GameplayManager.Instance.OnFuelEmpty();
                    //No more exhaust FX whe out of fuel
                    exhaustParticles.Stop();
                    exhaustSource.Stop();
                }
            }
            //Calculate drag based on rocket angle compared to direction of velocity 
            var cpScale = Vector3.Dot(myRigidbody.velocity, transform.right);
            var dragScale = Mathf.Abs(cpScale) * -1f;
            var liftScale = cpScale * 0.25f;

            var cp = transform.TransformPoint(0, centerOfPressure * bodyLength, 0);

            //apply stabilizing force from drag to the center of pressure
            myRigidbody.AddForceAtPosition(myRigidbody.velocity * dragScale, cp);

            //apply lift
            var liftDirection = Vector3.Cross(Vector3.forward, myRigidbody.velocity).normalized;
            myRigidbody.AddForceAtPosition(liftDirection * myRigidbody.velocity.magnitude * liftScale, cp);

            //apply wind
            var windScale = GameplayManager.Instance.Wind * Mathf.Abs(Vector3.Dot(myRigidbody.velocity.normalized, transform.up));
            myRigidbody.AddForceAtPosition(Vector3.right * windScale, cp);

            if(currentFuel<=0f && myRigidbody.velocity.y < 0f)
            {
                GameplayManager.Instance.OnLaunchFinished();
            }


        }
	}


	void Update()
	{
		// Tell GameplayManager about our position and speed
		GameplayManager.Instance.UpdateRocketInfo(myRigidbody.velocity.y, myRigidbody.position.y - startPosition.y);

		// Update fuel in HUD if we're flying
		if (flying)
		{
			UIManager.Instance.ShowFuelRemaining(currentFuel, startFuel);
			windSource.pitch = Mathf.Lerp(windMinPitch, windMaxPitch, myRigidbody.velocity.magnitude / speedForMaxPitch);
			windSource.volume = Mathf.Lerp(windMinVolume, windMaxVolume, myRigidbody.velocity.magnitude / speedForMaxVolume);
			if (GameplayManager.Instance.IsInGame() && !windSource.isPlaying)
			{
				windSource.Play();
			}
		}
	}

	/// <summary>
	/// Call this function to make the rocket start flying.
	/// </summary>
	public void StartFlying()
	{
		flying = true;
		// Turn on physics
		myRigidbody.isKinematic = false;
		// Update mass based on fuel used
		myRigidbody.mass = initialMass + startFuel;
		currentFuel = startFuel;
	}

	/// <summary>
	/// Sets the rocket's mass, minus fuel.
	/// </summary>
	/// <param name="value">Mass.</param>
	public void SetMass(float value)
	{
		initialMass = value;
	}

	/// <summary>
	/// Sets the amount of fuel to use for this rocket launch.
	/// </summary>
	/// <param name="value">Fuel mass.</param>
	public void SetFuelMass(float value)
	{
		startFuel = value;
	}

	/// <summary>
	/// Sets the strength of force applied to the rocket while fuel is used.
	/// </summary>
	/// <param name="value">Force applied to the rocket.</param>
	public void SetImpulse(float value)
	{
		impulse = value;
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (outOfFuel && !collision.gameObject.CompareTag("Player"))
		{
			myRigidbody.velocity = Vector3.zero;
		}
	}
}
