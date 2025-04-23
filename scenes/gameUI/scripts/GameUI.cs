using Godot;

public partial class GameUI : Control
{
	public static GameUI Instance { get; private set; }
	public float GameSpeed { get; private set; } = 1.0f;

	private Mutex _mutex = new();
	private Label speedLabel;
	private Label clockLabel;

	[Signal]
	public delegate void GameSpeedChangedEventHandler(float newSpeed);

	public override void _Ready()
	{
		ZIndex = 10;

		speedLabel = GetNode<Label>("SpeedLabel");
		clockLabel = GetNode<Label>("ClockLabel");

		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			GD.PrintErr("Multiple instances of GameUI detected!");
			QueueFree();
			return;
		}

		// ✅ Vänta lite med att koppla signal tills vi vet att TimeManager är färdig
		GetTree().CreateTimer(0.1f).Timeout += () =>
		{
			if (TimeManager.Instance != null)
			{
				TimeManager.Instance.Connect("TimeUpdated", new Callable(this, nameof(OnTimeUpdated)));
				GD.Print("GameUI: Connected to TimeUpdated signal!");
			}
			else
			{
				GD.PrintErr("GameUI: TimeManager.Instance is still NULL after delay.");
			}
		};

		// Knappar för att ändra spelhastighet
		GetNode<Button>("HBoxContainer/Speed1x").Pressed += () => TrySetGameSpeed(1.0f);
		GetNode<Button>("HBoxContainer/Speed2x").Pressed += () => TrySetGameSpeed(2.0f);
		GetNode<Button>("HBoxContainer/Speed4x").Pressed += () => TrySetGameSpeed(4.0f);
		GetNode<Button>("HBoxContainer/Speed10x").Pressed += () => TrySetGameSpeed(10.0f);
		GetNode<Button>("HBoxContainer/Speed20x").Pressed += () => TrySetGameSpeed(20.0f);

		UpdateSpeedLabel(GameSpeed);
		clockLabel.Text = TimeManager.Instance.GetCurrentDate();
	}

	private void SetGameSpeed(float speed)
	{
		_mutex.Lock();
		GameSpeed = speed;
		UpdateSpeedLabel(speed);
		EmitSignal(nameof(GameSpeedChanged), speed);
		_mutex.Unlock();
	}

	private void UpdateSpeedLabel(float speed)
	{
		speedLabel.Text = $"Speed: {speed}x";
	}

	private void TrySetGameSpeed(float newSpeed)
	{
		if (GameSpeed != newSpeed)
			SetGameSpeed(newSpeed);
	}

	private void OnTimeUpdated(int year, int day, string season)
	{
		clockLabel.Text = $"Year: {year} | Day: {day} | Season: {season}";
		GD.Print("GameUI: Time label updated.");
	}
}
