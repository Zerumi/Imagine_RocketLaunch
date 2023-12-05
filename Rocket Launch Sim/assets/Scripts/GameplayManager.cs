using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The gameplay manager is responsible for controlling the overall flow of the game. The
/// game is divided into three main states: Tutorial, InGame, and GameOver. The user interface
/// and input controls are different depending on the current game state. The gameplay
/// manager tracks the player progress and switches between the game states based on
/// the results as well as the user input. The gameplay manager is a singleton and can be
/// accessed in any script using the GameplayManager.Instance syntax.
/// </summary>
public class GameplayManager : MonoBehaviour
{
	// The static singleton instance of the gameplay manager.
	public static GameplayManager Instance { get; private set; }

	// Enumeration for the different game states. The default starting
	// state is the tutorial.
	enum GameState
	{
		Tutorial,	// Show player the game instructions.
		Planning,
		InGame,		// Player can start shooting with the left mouse button.
		GameOver,	// Game ended, player input is blocked.
	};
	GameState state = GameState.Tutorial;

	float maxHeight;
	float maxSpeed;

	[SerializeField]
	RocketController rocket = null;

	[SerializeField]
	int budget = 400000;

	[SerializeField]
	List<RocketMaterial> rocketMaterials;

	[SerializeField]
	List<RocketFuel> rocketFuels;

	[SerializeField]
	int maxFuel = 10;

	[SerializeField]
	float wind = 1f;

	[SerializeField]
	GameObject rocket_obj; // Ссылка на объект Rocket
	[SerializeField]
	GameObject firstPart_obj; // Ссылка на объект FirstPart

	public float Wind
	{
		get
		{
			return wind;
		}
	}

	int currentMaterialIndex = 0;
	int currentFuelIndex = 0;
	float currentFuelMass = 1f;
	float currentFuelFirstMass = 1f;
	float currentFuelSecondMass = 1f;
	int currentAngle = 0;
	float currentEnginePower = 1f;

	void Awake()
	{
		// Register this script as the singleton instance.
		Instance = this;
	}

	void Start()
	{
		UIManager.Instance.SetupControlPanel(rocketFuels, maxFuel);
		state = GameState.Tutorial;
		UIManager.Instance.OnTutorial();
		// Refresh the HUD and show the tutorial screen.
		UIManager.Instance.ShowHUD(false);
		UIManager.Instance.ShowScreen("Tutorial");
		rocket.Reset();
	}

	/// <summary>
	/// Call this function to start the gameplay.
	/// </summary>
	public void OnStartGame()
	{
		maxHeight = 0f;
		maxSpeed = 0f;
		state = GameState.Planning;
		CameraController.Instance.Reset();
		UIManager.Instance.OnReset();
		UIManager.Instance.ShowHUD(true);
		UIManager.Instance.ShowScreen("");
	}

	/// <summary>
	/// Call this function to launch the rocket.
	/// </summary>
	public void OnLaunch()
	{
		state = GameState.InGame;
		UIManager.Instance.OnStartFlying();
		CameraController.Instance.OnFlyingStarted();
		Stopwatch.Instance.startStopwatch();
		rocket.StartFlying();
	}

	/// <summary>
	/// Call this function when the rocket finishes flying.
	/// </summary>
	public void OnLaunchFinished()
	{
		CameraController.Instance.OnFlyingEnded();
		state = GameState.GameOver;
		Stopwatch.Instance.stopStopwatch();
		UIManager.Instance.ShowScreen("Game Over");
	}

	/// <summary>
	/// Call this function to reload the current level. The player progress will be reset.
	/// </summary>
	public void OnRetryLevel()
	{
		rocket.Reset();
		rocket.AttachFirstPartToRocket();
		UIManager.Instance.ShowScreen("");
		Invoke("OnStartGame", 0.5f);
	}

	public void SetRocketFirstMass(float value) {
		rocket.SetFirstMass(value);
	}

	/// <summary>
	/// Call this function to change the material the rocket is made of.
	/// Different materials affect the mass of the rocket and cost of the rocket.
	/// </summary>
	/// <param name="value">The index of the rocket material to use.</param>
	public void SetRocketSecondMass(float value)
	{
		rocket.SetSecondMass(value);
	}
	
	/// <summary>
	/// Call this function to change the fuel the rocket uses.
	/// Different fuels affect the impulse provided and cost of the rocket.
	/// </summary>
	/// <param name="value">The index of the fuel type to use.</param>
	public void SetFuelType(int value)
	{
		currentFuelIndex = value;
		UIManager.Instance.UpdateFuelType(rocketFuels[value]);
		rocket.SetImpulse(rocketFuels[value].impulsePerWeight);
	}

	/// <summary>
	/// Call this function to change the amount of fuel the rocket uses.
	/// </summary>
	/// <param name="value">The amount of fuel to use.</param>
	public void SetFuelSecondAmount(float value)
	{
		currentFuelSecondMass = value;
		rocket.SetSecondFuelMass(value);
	}

	public void SetFuelFirstAmount(float value)
	{
		currentFuelFirstMass = value;
		rocket.SetFuelFirstMass(value);
	}

	void SetCurrentFuel()
	{
		currentFuelMass = currentFuelFirstMass + currentFuelSecondMass;
	}

	public float GetCurrentEnginePower() {
		return currentEnginePower;
	}

	public int GetCurrentAngle() {
		return -currentAngle;
	}
	
	public void SetAngle(int value)
	{
		currentAngle = value;
		UIManager.Instance.UpdateAngle(value);
	}
	
	public void SetEnginePower(float value)
	{
		currentEnginePower = value;
		UIManager.Instance.UpdateEnginePower(value);
	}

	// marked for removal
	/// <summary>
	/// Call this function to recalculate the budget used on the rocket.
	/// </summary>
	void UpdateBudget()
	{
        RocketMaterial material = rocketMaterials[currentMaterialIndex];
        RocketFuel fuel = rocketFuels[currentFuelIndex];
        int currentExpenses = material.cost;

        currentExpenses += fuel.costPerWeight * Mathf.RoundToInt(currentFuelMass);
        UIManager.Instance.UpdateBudget(currentExpenses, budget);
	}

	public void OnFuelEmpty()
	{
		CameraController.Instance.OnFuelEmpty();
	}

	/// <summary>
	/// Update speed and height stats.
	/// </summary>
	/// <param name="speed">Current rocket vertical speed.</param>
	/// <param name="height">Current rocket height.</param>
	public void UpdateRocketInfo(float speed, float height)
	{
		maxHeight = Mathf.Max(height, maxHeight);
		maxSpeed = Mathf.Max(speed, maxSpeed);
		UIManager.Instance.UpdateHUD(speed, maxSpeed, height, maxHeight);
	}

	public GameObject GetRocketObj() {
		return rocket_obj;
	}

	public GameObject GetFirstPart() {
		return firstPart_obj;
	}

	public bool IsInGame()
	{
		return state == GameState.InGame;
	}

	public void OnLanguageChanged()
	{
		UIManager.Instance.OnLanguageChanged();
	}
}