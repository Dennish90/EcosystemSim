using Godot;
using System;

public enum Season { Winter, Spring, Summer, Autumn };

public partial class TimeManager : Node
{
	public static TimeManager Instance { get; private set; }

	public int Year { get; private set; } = 1;
	public int Day { get; private set; } = 1;

	public Season CurrentSeason { get; private set; } = Season.Summer;

	private double timeSpeed = 1.0;
	private const double dayLengthSeconds = 240.0;
	private Timer dayTimer;

	[Signal]
	public delegate void TimeUpdatedEventHandler(int year, int day, string season); // ✅ string

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
			GD.Print("TimeManager: Instance set successfully!");
		}
		else
		{
			GD.PrintErr("Multiple instances of TimeManager detected! Deleting this one.");
			QueueFree();
			return;
		}
		
		GetTree().CreateTimer(0.1f).Timeout += () =>
		{
			if (GameUI.Instance != null)
			{
				GameUI.Instance.Connect("GameSpeedChanged", new Callable(this, nameof(OnGameSpeedChanged)));
				GD.Print("TimeManager: Connected to GameSpeedChanged!");
			}
			else
			{
				GD.PrintErr("TimeManager: GameUI.Instance is NULL!");
			}
		};

		dayTimer = new Timer();
		AddChild(dayTimer);
		dayTimer.OneShot = true;
		dayTimer.Timeout += OnDayEnd;

		StartNewDay();
	}

	private void StartNewDay()
	{
		dayTimer.WaitTime = dayLengthSeconds / timeSpeed;
		dayTimer.Start();
		GD.Print($"StartNewDay: Timer started for {dayTimer.WaitTime} seconds at speed {timeSpeed}");
	}

	private void OnDayEnd()
	{
		Day++;
		if (Day > 10)
		{
			Day = 1;
			SwitchSeason();
		}

		EmitSignal(nameof(TimeUpdated), Year, Day, CurrentSeason.ToString());
		GD.Print($"TimeUpdated: Year {Year}, Day {Day}, Season {CurrentSeason}");

		StartNewDay();
	}

	private void SwitchSeason()
	{
		CurrentSeason = (Season)(((int)CurrentSeason + 1) % 4);
		if (CurrentSeason == Season.Spring)
			Year++;
	}

	private void OnGameSpeedChanged(float newSpeed)
	{
		double elapsedTime = dayLengthSeconds - dayTimer.TimeLeft;
		timeSpeed = newSpeed;
		dayTimer.Stop();
		double remainingTime = (dayLengthSeconds - elapsedTime) / timeSpeed;
		dayTimer.WaitTime = remainingTime;
		dayTimer.Start();
	}
	
	public String GetCurrentDate()
	{
		return $"Year: {Year} | Day: {Day} | Season: {CurrentSeason}";
	}
}
