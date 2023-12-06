using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// RocketController - Script that controls rocket movement and physics.
/// </summary>
public class RocketController : MonoBehaviour
{
	Rigidbody myRigidbody = null;
	Component firstPart = null;

	[SerializeField]
	float enginePower = 1f;

	[SerializeField]
	float angle = 0;

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
	float startFuelFirst;
	float startFuelSecond;
	float currentFuel;
	float currentFuelFirst;
	float currentFuelSecond;

	float initialMass;
	float secondPartMass;
	float firstPartMass;

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
	bool outOfFuelFirst = false;

	Vector3 startPosition;
	Vector3 startPositionFirst;

	void Awake()
	{
		startFuel = 1f;
		startFuelFirst = 1f;
		initialMass = 1f;
		impulse = 1f;
	}

	void Start()
	{
		exhaustSource = AudioHelper.CreateAudioSource(gameObject, exhaustAudio);
		windSource = AudioHelper.CreateAudioSource(gameObject, windAudio);

		myRigidbody = GetComponent<Rigidbody>();
		startPosition = myRigidbody.position;
		firstPart = GameplayManager.Instance.GetFirstPart().GetComponent<Component>();
		startPositionFirst = firstPart.transform.position;


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
		outOfFuelFirst = false;
		lastEnginePower = -1f;
		timeToSubtract = 0f;
		r = 0;
		startMass = 0;
		removedMass = 0;
		previousMass = 0;
		windSource.Stop();
	}
	bool dontupdatemaxspeed = false;
	Vector3 velocityToAdd;
	float lastEnginePower = -1f;
	float timeToSubtract = 0f;
	float startMass;
	float removedMass;
	float previousMass;
	float r;

	void FixedUpdate()
	{
		enginePower = GameplayManager.Instance.GetCurrentEnginePower();
		angle = Mathf.SmoothDampAngle(transform.eulerAngles.z, (float)GameplayManager.Instance.GetCurrentAngle (), ref r, 1f);
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

				// m = m0 * exp(-alpha*t)

				float timeElapsed = Stopwatch.Instance.getElapsed();
				if (lastEnginePower != enginePower)
				{
					startMass = myRigidbody.mass;
					timeToSubtract = timeElapsed;
				}

				//Vector3 impulseToAdd = (previousMass - initialMass + currentFuelSecond + currentFuelFirst) * myRigidbody.velocity;

				//myRigidbody.AddRelativeForce(impulseToAdd, ForceMode.Impulse);
				
				myRigidbody.mass = initialMass + currentFuelSecond + currentFuelFirst;

				float massToRemain = (startMass) * Mathf.Exp(-(enginePower * 9.81f) * (timeElapsed - timeToSubtract) / impulse) - removedMass;
				float massToSubtract = (myRigidbody.mass - massToRemain);

				myRigidbody.transform.rotation = Quaternion.AngleAxis(angle, new Vector3(0,0,1));
				myRigidbody.AddRelativeForce(Vector3.up * massToSubtract * impulse, ForceMode.Force);
				myRigidbody.AddRelativeForce(Vector3.up * enginePower * 9.81f, ForceMode.Acceleration);

                //Substract fuel
				if (currentFuelFirst > 0f) {
					currentFuelFirst = Mathf.Max(0f, currentFuelFirst - massToSubtract);
				}
				else if (currentFuelSecond > 0f) {
					currentFuelSecond = Mathf.Max(0f, currentFuelSecond - massToSubtract);
				}

				currentFuel = currentFuelFirst + currentFuelSecond;

				if (dontupdatemaxspeed) {
					dontupdatemaxspeed = false;
					
					myRigidbody.AddForce(velocityToAdd, ForceMode.VelocityChange);
					myRigidbody.freezeRotation = false;
				}
                //Update mas based on remaing fuel
				if (currentFuelFirst == 0f && !outOfFuelFirst) {
					outOfFuelFirst = true;
					dontupdatemaxspeed = true;
					float velocityChangeFactor = (myRigidbody.mass) / (myRigidbody.mass - firstPartMass); // Фактор изменения скорости
					velocityToAdd = myRigidbody.velocity * velocityChangeFactor;
					myRigidbody.AddForce(velocityToAdd, ForceMode.VelocityChange);
					myRigidbody.freezeRotation = true;
					DetachFirstPartFromRocket();
					initialMass -= firstPartMass;
					removedMass += firstPartMass;
				}
                if(currentFuel == 0f)
                {
                    GameplayManager.Instance.OnFuelEmpty();
                    //No more exhaust FX whe out of fuel
                    exhaustParticles.Stop();
                    exhaustSource.Stop();
                }
            }

            if(currentFuel<=0f && myRigidbody.velocity.y < 0f)
            {
                GameplayManager.Instance.OnLaunchFinished();
            }

			previousMass = myRigidbody.mass;
        }
		lastEnginePower = enginePower;
	}


	void Update()
	{
		// Tell GameplayManager about our position and speed
		if (dontupdatemaxspeed)
			GameplayManager.Instance.UpdateRocketInfo (0, myRigidbody.position.y - startPosition.y);
		else
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
		myRigidbody.mass = initialMass + startFuel + startFuelFirst;
		currentFuel = startFuel;
		currentFuelFirst = startFuelFirst;
		currentFuelSecond = startFuelSecond;
		startMass = startFuel + initialMass;
		previousMass = startMass;
	}

	public void SetFirstMass(float value)
	{
		firstPartMass = value;
		SetInitMass();
	}

	/// <summary>
	/// Sets the rocket's mass, minus fuel.
	/// </summary>
	/// <param name="value">Mass.</param>
	public void SetSecondMass(float value)
	{
		secondPartMass = value;
		SetInitMass();
	}

	void SetInitMass() {
		initialMass = firstPartMass + secondPartMass;
	}

	public void SetFuelFirstMass(float value)
	{
		startFuelFirst = value;
		setInitFuel();
	}

	/// <summary>
	/// Sets the amount of fuel to use for this rocket launch.
	/// </summary>
	/// <param name="value">Fuel mass.</param>
	public void SetSecondFuelMass(float value)
	{
		startFuelSecond = value;
		setInitFuel();
	}

	void setInitFuel()
	{
		startFuel = startFuelFirst + startFuelSecond;
	}

	/// <summary>
	/// Sets the strength of force applied to the rocket while fuel is used.
	/// </summary>
	/// <param name="value">Force applied to the rocket.</param>
	public void SetImpulse(float value)
	{
		impulse = value;
	}

	public GameObject FindByName(GameObject parent, string name)
	{
		if (parent.name == name) return parent;
		foreach (Transform child in parent.transform)
		{
			GameObject result = FindByName(child.gameObject, name);
			if (result != null) return result;
		}
		return null;
	}

	public void DetachFirstPartFromRocket()
	{
		// Получаем компоненты FirstPart и Rocket
		Component firstPartComponent = GameplayManager.Instance.GetFirstPart().GetComponent<Component>();
		Component rocketComponent = GameplayManager.Instance.GetRocketObj().GetComponent<Component>();
		
		if (firstPartComponent != null && rocketComponent != null)
		{
			// Отсоединяем FirstPart от Rocket
			//Rigidbody rocketPhys = rocketComponent.GetComponent<Rigidbody>();
			//rocketPhys.isKinematic = false;
			firstPartComponent.transform.parent = null;
			//rocketPhys.isKinematic = true;
		}
	}

	public void AttachFirstPartToRocket()
	{
		// Получаем компоненты FirstPart и Rocket
		Component firstPartComponent = GameplayManager.Instance.GetFirstPart().GetComponent<Component>();
		Component rocketComponent = GameplayManager.Instance.GetRocketObj().GetComponent<Component>();
		
		if (firstPartComponent != null && rocketComponent != null)
		{
			// Отсоединяем FirstPart от Rocket
			firstPartComponent.transform.parent = rocketComponent.transform;
			
			firstPartComponent.transform.position = startPositionFirst;
			firstPartComponent.transform.rotation = Quaternion.identity;
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if (outOfFuel && !collision.gameObject.CompareTag("Player"))
		{
			myRigidbody.velocity = Vector3.zero;
		}
	}
}
