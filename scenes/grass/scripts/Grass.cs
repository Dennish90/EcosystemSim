//TODO: Structure code into parent and child-classes. Plants > Trees / Bushes / grasses > grass

using Godot;
using System;

public partial class Grass : Node2D
{
	// Variables for growth-stages
	private int growthStage = 0;
	private const int MaxGrowthStage = 3;

	private Timer growthTimer;
	private float baseGrowthTime = 5.0f; // Default growth time (Will include random event)

	// Object for handling Graphics
	private AnimatedSprite2D grassAnimation;

	// Random generator
	private Random random = new Random();

	public override void _Ready()
	{
		ZIndex = 1;
		// Create and configure growth-timer
		growthTimer = CreateTimer(GetAdjustedWaitTime(baseGrowthTime), OnGrowthTimeout);

		// Fetch referance to graphic-sprites
		grassAnimation = GetNode<AnimatedSprite2D>("GrassAnimation");

		// Set the first growth-stage
		UpdateGrowth();

		// Link with gamespeed inside GameUI
		if (GameUI.Instance != null)
		{
			GameUI.Instance.Connect(nameof(GameUI.GameSpeedChanged), new Callable(this, nameof(OnGameSpeedChanged)));
		}
		else
		{
			//GD.PrintErr("GameUI instance not found! Grass will not adjust its growth speed.");
		}
	}

	// Function for creating a timer
	private Timer CreateTimer(float waitTime, Action timeoutAction)
	{
		var timer = new Timer
		{
			WaitTime = waitTime,
			OneShot = false
		};
		timer.Timeout += timeoutAction;
		AddChild(timer);
		timer.Start();
		return timer;
	}

	// Adjusts growth rate and grass spread with game speed
	private float GetAdjustedWaitTime(float baseWaitTime)
	{
		float gameSpeed = GameUI.Instance?.GameSpeed ?? 1.0f; // Default 1 if gamespeed is not found
		return baseWaitTime / Math.Max(gameSpeed, 0.1f); // Avoid division with 0 (!)
	}

	// Triggers at growth-timer timeout
	private void OnGrowthTimeout()
	{	// Chans to grow if not at last growth stage
		if (growthStage < MaxGrowthStage && random.Next(0, 50) < 1) // 2% chanse
		{
			growthStage++;
			UpdateGrowth();
		}
		// Adjust timer to represent game speed
		growthTimer.WaitTime = GetAdjustedWaitTime(baseGrowthTime);
	}
	//Update graphics sprite at growth
	private void UpdateGrowth()
	{
		grassAnimation.Frame = growthStage;
	}
	//Reset growth. Used when grass has been eaten by herbivore
	public void ResetGrowth()
	{
		growthStage = 0;
		UpdateGrowth();
		growthTimer.WaitTime = GetAdjustedWaitTime(baseGrowthTime);
		growthTimer.Start();
	}

	private float lastLoggedSpeed = -1.0f; // Keeps track of last game speed
	// adjust growth timer at game speed change
	private void OnGameSpeedChanged(float newSpeed)
	{
		// Justera timerns väntetid direkt
		growthTimer.Stop();
		growthTimer.WaitTime = GetAdjustedWaitTime(baseGrowthTime);
		growthTimer.Start();
		if (Math.Abs(newSpeed - lastLoggedSpeed) > 0.5f)
		{
			lastLoggedSpeed = newSpeed;
		}
	}
	
	public int GetGrowthStage()
	{
		return growthStage;
	}

	public void SetGrowthStage(int stage)
	{
		growthStage = Math.Min(stage, MaxGrowthStage); // Limit at max grow stage
	}

	public void Grow()
	{
		if (growthStage < MaxGrowthStage)
		{
			growthStage++;
		}
	}
}
