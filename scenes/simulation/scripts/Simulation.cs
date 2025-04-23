using Godot;
using System;
using System.Collections.Generic;

public partial class Simulation : Node2D
{
	public static Simulation Instance { get; private set; } // Singleton-instans

	public TileMapLayer worldMap;
	private PackedScene grassScene = (PackedScene)GD.Load("res://scenes/grass/grass.tscn");
	private HashSet<Vector2I> grassCells = new HashSet<Vector2I>(); // Unikt set för gräsceller

	private Timer grassSpreadTimer; // Timer för grässpridning
	private Timer newGrassTimer;   // Timer för ny gräsplacering
	private Random random = new Random();

	public override void _Ready()
	{
		// Singleton-instans
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			GD.PrintErr("Multiple instances of Simulation detected!");
			QueueFree();
			return;
		}

		// Prenumerera på GameSpeedChanged-signalen
		GameUI.Instance.Connect(nameof(GameUI.GameSpeedChanged), new Callable(this, nameof(OnGameSpeedChanged)));

		// Hämta WorldMap
		worldMap = GetNode<TileMapLayer>("WorldMap");
		if (worldMap == null)
		{
			GD.PrintErr("WorldMap node not found!");
			return;
		}

		// Timer för spridning av gräs
		grassSpreadTimer = CreateTimer(GetAdjustedWaitTime(5.0f), SpreadGrass);

		// Timer för att slumpa ny gräsplacering
		newGrassTimer = CreateTimer(GetAdjustedWaitTime(10.0f), TrySpawnNewGrass);

		GD.Print("Simulation started.");
		PlaceGrassAtCell(new Vector2I(20, 20));
		PlaceGrassAtCell(new Vector2I(55, 20));
		
		
		SpawnDeer(new Vector2(200, 200), Sex.Female, 5);
		SpawnDeer(new Vector2(100, 100), Sex.Male, 5);
	}

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

	private float GetAdjustedWaitTime(float baseWaitTime)
	{
		// Justera väntetiden baserat på GameSpeed från GameUI
		float gameSpeed = GameUI.Instance.GameSpeed;
		return baseWaitTime / Math.Max(gameSpeed, 0.1f); // Undvik division med 0
	}

	private void SpreadGrass()
	{
		List<Vector2I> candidatePositions = new List<Vector2I>();

		// Hitta möjliga kandidater runt befintligt gräs
		foreach (var grassCell in grassCells)
		{
			// Kontrollera om den aktuella cellen är mogen
			if (!HasMatureNeighbor(grassCell)) 
			{
				continue; // Hoppa över om cellen inte är mogen
			}

			foreach (var neighbor in GetNeighbors(grassCell))
			{
				if (!grassCells.Contains(neighbor) && IsGrassable(neighbor))
				{
					candidatePositions.Add(neighbor);
				}
			}
		}

		if (candidatePositions.Count > 0)
		{
			var targetCell = candidatePositions[random.Next(candidatePositions.Count)];
			PlaceGrassAtCell(targetCell);
		}
		else
		{
			GD.Print("No candidates found for grass spreading.");
		}

		// Justera timer baserat på aktuell hastighet
		grassSpreadTimer.WaitTime = GetAdjustedWaitTime(10.0f);
	}


	private bool HasMatureNeighbor(Vector2I cell)
	{
		var grass = GetGrassAtCell(cell); // Hämta gräset på den aktuella cellen
		if (grass != null && grass.GetGrowthStage() >= 1) // Kontrollera om det är moget
		{
			return true; // Cellen är mogen för att sprida gräs
		}
		return false; // Cellen är inte mogen
	}


	private Grass GetGrassAtCell(Vector2I cell)
	{
		foreach (Node child in GetChildren())
		{
			if (child is Grass grass && grass.GlobalPosition == worldMap.MapToLocal(cell))
			{
				return grass; // Returnera gräset om det hittas
			}
		}
		return null; // Returnera null om inget gräs hittas
	}

	private void TrySpawnNewGrass()
	{
		GD.Print("Attempting to spawn new grass...");

		// Hämta alla celler som kan innehålla gräs
		var greenCells = worldMap.GetUsedCells();
		List<Vector2I> grassableCells = new List<Vector2I>();

		foreach (var cell in greenCells)
		{
			if (worldMap.GetCellSourceId(cell) == 1 && !grassCells.Contains(cell))
			{
				grassableCells.Add(cell);
			}
		}

		if (grassableCells.Count > 0 && random.Next(0, 50) < 1) // 0.1% chans
		{
			var targetCell = grassableCells[random.Next(grassableCells.Count)];
			PlaceGrassAtCell(targetCell);
			GD.Print($"New grass spawned at {targetCell}");
		}
		else
		{
			GD.Print("No new grass spawned this time.");
		}

		// Justera timer baserat på aktuell hastighet
		newGrassTimer.WaitTime = GetAdjustedWaitTime(10.0f);
	}

	private IEnumerable<Vector2I> GetNeighbors(Vector2I cellPosition)
	{
		return new Vector2I[]
		{
			cellPosition + new Vector2I(-1, 0), // Vänster
			cellPosition + new Vector2I(1, 0),  // Höger
			cellPosition + new Vector2I(0, -1), // Upp
			cellPosition + new Vector2I(0, 1)   // Ner
		};
	}

	private bool IsGrassable(Vector2I cellPosition)
	{
		int cellId = worldMap.GetCellSourceId(cellPosition);
		return cellId == 1; // Kontrollera om cellen är en gräsyta
	}

	private void PlaceGrassAtCell(Vector2I cellPosition)
	{
		var globalPosition = worldMap.MapToLocal(cellPosition);

		GD.Print($"Placing grass at Cell {cellPosition}, Global Position: {globalPosition}");

		var grassInstance = (Node2D)grassScene.Instantiate();
		grassInstance.Position = globalPosition;
		AddChild(grassInstance);

		grassCells.Add(cellPosition); // Lägg till cellpositionen i listan
	}

	private void OnGameSpeedChanged(float newSpeed)
	{
		// Uppdatera timerns väntetid direkt
		if (grassSpreadTimer != null)
		{
			grassSpreadTimer.WaitTime = 5.0f / newSpeed;
			grassSpreadTimer.Start(); // Starta om timern med den nya hastigheten
		}
		GD.Print($"Simulation timer updated for GameSpeed: {newSpeed}");
	}

	public void SpawnDeer(Vector2 position, Sex gender, int age)
	{
		PackedScene deerScene = GD.Load<PackedScene>("res://scenes/animal/deer/Deer.tscn");
		Deer deer = (Deer)deerScene.Instantiate();
		deer.Position = position;
		deer.Gender = gender; // Sätt kön
		deer.Age = age;
		AddChild(deer);
		deer.AddToGroup("Animals");
	}
}
