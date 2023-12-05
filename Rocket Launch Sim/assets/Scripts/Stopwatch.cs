using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Stopwatch : MonoBehaviour {
	
	// The static singleton instance of the stopwatch.
	public static Stopwatch Instance { get; private set; }

	float currentTime;
	bool active = false;

	Text stopwatch = null;

	// Use this for initialization
	void Start () {
		currentTime = 0;
	}

	void Awake()
	{
		// Register this script as the singleton instance.
		Instance = this;
	}

	// Update is called once per frame
	void Update () {
		if (active) {
			currentTime += Time.deltaTime;
			TimeSpan timeSpan = TimeSpan.FromSeconds(currentTime);
			UIManager.Instance.getStopwatch().text = "Elapsed: " + timeSpan.Minutes.ToString() + ":" + timeSpan.Seconds.ToString();
		}
	}

	public void startStopwatch()
	{
		active = true;
	}

	public void stopStopwatch()
	{
		active = false;
		currentTime = 0;
	}

	public float getElapsed()
	{
		return currentTime;
	}
}
