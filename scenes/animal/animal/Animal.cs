using Godot;
using System;
using System.Collections.Generic;

public enum Sex { Male, Female }

public partial class Animal : CharacterBody2D
{	
	public bool isDead { get; set; } = false;
	public bool hasDecayed {get; set; } = false;

	public Vector2 targetPosition = Vector2.Zero;

	public bool isMovingToFood = false;
	public bool isMovingToWater = false;

	public bool isEating = false;
	public float eatTimer = 0.0f;
	public const float eatDuration = 3.0f;

	public bool goingForDrink = false;
	public bool isDrinking = false;
	public float drinkTimer = 1.0f;
	public const float drinkCooldown = 1.0f;

	public float statusPrintTimer = 5.0f;
	public const float statusPrintCooldown = 5.0f;
	
	// Reproduktionsrelaterat
	public bool IsPregnant { get; private set; } = false;
	public double PregnancyProgress { get; private set; } = 0.0;
	public Animal MateTarget { get; private set; } = null;
	public List<Animal> Children { get; private set; } = new();
	public Animal Mother { get; private set; } = null;
	public Animal Father { get; private set; } = null;

	public Label eatingLabel;
	public AnimatedSprite2D sprite;
	public CollisionPolygon2D collider;

	public float currentRotation = 0.0f;

	public Timer thirstTimer;
	public Timer hungerTimer;
	public Timer energyTimer;
	public Timer decayTimer;
	
	//
	public string Species { get; set; }
	public Sex Gender { get; set; }
	public string AnimalName { get; set; }
	public int Age { get; set; }
	public float Health { get; set; }
	public float Thirst { get; set; }
	public float Hunger { get; set; }
	public float Energy { get; set; }
	public float baseSize { get; set; }
	public float baseSpeed { get; set; }
	public float Bravery { get; set; } // Curage to fight back
	public float Aggression { get; set; } // Will to attack
	public float Awareness { get; set; } // Liklyness to spot predators, find food and so on
	public float Social { get; set; } // Pack-behaviour
	public float meatAtDeath { get; set; }
	public float Speed { get; set; }

	[Export] public int PregnancyAge;
	[Export] public Season PregnancySeason;
	[Export] public int MinChildren;
	[Export] public int MaxChildren;
	[Export] public float PregnancyDuration;

	public override void _Ready()
	{
		base._Ready();
		collider = GetNodeOrNull<CollisionPolygon2D>("CollisionPolygon2D");
	}

	public Timer CreateTimer(float waitTime, Action timeoutAction)
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

	// Adjust timers at game speed change
	public void AdjustTimers(float newSpeed)
	{
		float adjustedHungerWait = GetAdjustedWaitTime(5.0f);
		float adjustedThirstWait = GetAdjustedWaitTime(2.0f);
		float adjustedEnergyWait = GetAdjustedWaitTime(1.0f);

		hungerTimer.WaitTime = adjustedHungerWait;
		thirstTimer.WaitTime = adjustedThirstWait;
		energyTimer.WaitTime = adjustedEnergyWait;

		hungerTimer.Start();
		thirstTimer.Start();
		energyTimer.Start();
	}

	public void SetGenderSprite()
	{
		if (Gender == Sex.Male)
		{
			sprite.Frame = 0; // Male-sprite
		}
		else if (Gender == Sex.Female)
		{
			sprite.Frame = 1; // Female-sprite
		}
	}

	public virtual void ShowInfo()
	{
		GD.Print($"Name: {Name}, Species: {Species}, Health: {Health}, Hunger: {Hunger}, Thirst: {Thirst}, Speed: {baseSpeed}, Bravery: {Bravery}");
	}

	// Function for sprite to look in the direction the animal is moving
	public void UpdateDirection(Vector2 direction, float delta)
	{
		if (direction == Vector2.Zero) return;

		float targetRotation = Mathf.Atan2(direction.Y, direction.X);
		currentRotation = Mathf.LerpAngle(currentRotation, targetRotation, 5.0f * delta);
		sprite.Rotation = currentRotation;
	}
	
	//Validates if the desired goal position is approved
	public bool IsValidTarget(Vector2I currentCell, Vector2I targetCell, int maxDistance)
	{
		int distance = (int)currentCell.DistanceTo(targetCell);
		if (distance > maxDistance)
			return false;

		var mapBounds = Simulation.Instance.worldMap.GetUsedRect();
		if (!mapBounds.HasPoint(targetCell))
			return false;

		int cellId = Simulation.Instance.worldMap.GetCellSourceId(targetCell);
		return cellId != 2;
	}

	// Animals default action to roam around
	public void Wander(float delta)
	{
		Vector2I currentCell = Simulation.Instance.worldMap.LocalToMap(GlobalPosition);
		Vector2I targetCell;

		int attempts = 0;
		do
		{
			if (attempts > 20)
			{
				//GD.Print($"{Name} could not find a valid target. Staying in place.");
				targetPosition = GlobalPosition;
				return;
			}

			// Destination max 10 grids away
			int offsetX = (int)(GD.Randi() % 21) - 10; // -10 till +10
			int offsetY = (int)(GD.Randi() % 21) - 10;
			targetCell = currentCell + new Vector2I(offsetX, offsetY);
			attempts++;
		}
		while (!IsValidTarget(currentCell, targetCell, 10));

		targetPosition = Simulation.Instance.worldMap.MapToLocal(targetCell);
		//GD.Print($"{Name} is wandering to {targetPosition}");
	}
	
	//Locate the nearest water tile animal can drink from when thirsty
	public Vector2 FindNearestWater()
	{
		Vector2 nearestWaterPosition = Vector2.Zero;
		float nearestDistance = float.MaxValue;

		// Get current location
		Vector2I currentCell = Simulation.Instance.worldMap.LocalToMap(GlobalPosition);

		foreach (var cell in Simulation.Instance.worldMap.GetUsedCells())
		{
			// Check if tile is water
			if (Simulation.Instance.worldMap.GetCellSourceId(cell) != 2) // 2 = water sprite
				continue;

			// Find walkable tile next to desired water tile
			foreach (var neighbor in GetNeighbors(cell))
			{
				// Kontrollera att cellen är gångbar och giltig
				if (Simulation.Instance.worldMap.GetCellSourceId(neighbor) == 1) // Exempel: "0" är gångbar terräng
				{
					Vector2 neighborPosition = Simulation.Instance.worldMap.MapToLocal(neighbor);
					float distance = GlobalPosition.DistanceTo(neighborPosition);
					// Prioritera närmaste gångbara cell
					if (distance < nearestDistance)
					{
						nearestDistance = distance;
						nearestWaterPosition = neighborPosition;
					}
				}
			}
		}

		/*if (nearestWaterPosition == Vector2.Zero)
		{
			GD.Print($"{Name} could not find accessible water.");
		}
		else
		{
			GD.Print($"{Name} found nearest accessible water at {nearestWaterPosition}.");
		}
		return nearestWaterPosition;*/
	}

	public bool IsWaterTile(Vector2 position)
	{
		Vector2I cell = Simulation.Instance.worldMap.LocalToMap(position);
		int cellId = Simulation.Instance.worldMap.GetCellSourceId(cell);
		return cellId == 2; // cellId 2 = water
	}
	
	// check neighboring tiles to be able to process further
	public IEnumerable<Vector2I> GetNeighbors(Vector2I cell)
	{
		return new List<Vector2I>
		{
			cell + new Vector2I(1, 0),  // Right
			cell + new Vector2I(-1, 0), // Left
			cell + new Vector2I(0, 1),  // Down
			cell + new Vector2I(0, -1)  // Up
		};
	}

	public void HandleDrinking(float delta)
	{
		drinkTimer -= delta;
		if (drinkTimer <= 0)
		{
			drinkTimer = drinkCooldown;
			Thirst -= 10;
			if (Thirst <= 0)
			{
				Thirst = 0;
				StopDrinking();
			}
		}
	}

	public void StartDrinking()
	{
		isDrinking = true;
		goingForDrink = false; // Stops moving for water
		isMovingToWater = false;
		Velocity = Vector2.Zero; // Stop motion
		Speed = 0; // Stop at water

		eatingLabel.Modulate = new Color(0, 0, 1); // Blå färg
		eatingLabel.Text = "*Drinking*";
		eatingLabel.Visible = true;

		//GD.Print($"{Name} starts drinking water.");
	}

	public void StopDrinking()
	{
		isDrinking = false;
		Speed = 20.0f;
		eatingLabel.Text = "";
		eatingLabel.Visible = false;
		//GD.Print($"{Name} finished drinking.");
	}

	public float GetAdjustedWaitTime(float baseWaitTime)
	{
		// adjust wait time to match game speed
		float gameSpeed = GameUI.Instance?.GameSpeed ?? 1.0f; // default 1 if game speed is not found
		return baseWaitTime / Math.Max(gameSpeed, 0.01f); // no division with 0 please
	}

	public void OnHungerTimeout()
	{
		Hunger += 1 * GameUI.Instance.GameSpeed;; // increase hunger
		if(Hunger > 100) Hunger = 100;
		//GD.Print($"{Name}'s hunger increased to {Hunger}.");

		if (Hunger >= 100)
		{
			Energy -= 5 * GameUI.Instance.GameSpeed;
			if(Energy > 100) Energy = 100;
			//GD.Print($"{Name} is starving! Health: {Health}");
		}
	}

	public void OnEnergyTimeout()
	{
		Speed = (baseSpeed * (Energy / 100.0f));
		if(Hunger >= 100) 
		{
			Hunger = 100;
			Energy += 1 * GameUI.Instance.GameSpeed;
			if(Energy < 0) Energy = 0;
			//GD.Print($"{Name} is starving! Energy decreased to {Energy}");
		}
		if(Thirst >= 100) 
		{
			Thirst = 100;
			Energy -= 1 * GameUI.Instance.GameSpeed;
			if(Energy < 0) Energy = 0;
			//GD.Print($"{Name} is dehydrated! Energy decreased to {Energy}");
		}
		
		if(Energy <= 0) 
		{
			Health -= 1 * GameUI.Instance.GameSpeed;
			if(Health < 0) Health = 0;
		}
		if(Thirst <= 30 && Hunger <= 30)
		{
			Energy += 1 * GameUI.Instance.GameSpeed;
			if (Energy > 100.0f) Energy = 100.0f;
			//if (Energy < 100.0f) GD.Print($"{Name} is recovering! Energy increased to {Energy}");
		}
		
		if(Energy < 30) Speed = baseSpeed * 0.2f;
		
		if(Energy >= 30)
		{
			Health += 1 * GameUI.Instance.GameSpeed;
			if(Health > 100) Health = 100;
			//if(Health < 100) GD.Print($"{Name} is recovering! Health increased to {Health}");
		}
	}

	public void OnThirstTimeout()
	{
		Thirst += 1; // increase thirst
		//GD.Print($"{Name}'s thirst increased to {Thirst}.");

		if (Thirst >= 100)
		{
			Energy -= 5 * GameUI.Instance.GameSpeed;
			//GD.Print($"{Name} is dehydrated! Health: {Health}");
		}
	}

	public virtual void Drink()
	{
		Thirst -= 20; // reqover thirst
		if (Thirst < 0) Thirst = 0;
		//GD.Print($"{Name} drank water. Thirst: {Thirst}");
	}

	// Child classes have their own Eat-function
	public virtual void Eat()
	{
		GD.PrintErr($"{Name} tried to call base Eat() – should be overridden!");
	}

	public virtual void Move(float delta)
	{
		Position += new Vector2(Speed * delta, 0); // Move to position
	}

	// Enables animals to not collide during mating
	public void SetCollisionEnabled(bool enabled)
	{
		if (collider != null)
			collider.Disabled = !enabled;
	}

	public void HandleAnimalCollision(Animal otherAnimal, float delta)
	{
		//GD.Print($"{Name} and {otherAnimal.Name} are trying to resolve collision.");
		if (MateTarget == otherAnimal && otherAnimal.MateTarget == this)
		{
			// Female turns away from male while mating
			currentRotation = otherAnimal.currentRotation;
			sprite.Rotation = currentRotation;

			return;
		}

		// if both animals stand still, choose new target location
		if (Velocity == Vector2.Zero && otherAnimal.Velocity == Vector2.Zero)
		{
			//GD.Print($"{Name} and {otherAnimal.Name} are stuck. Choosing new targets.");
			targetPosition = Vector2.Zero;
			isMovingToWater = false;
			isMovingToFood = false;
			Wander(delta);
			return;
		}

		// If only this animal is moving, move to the side
		Vector2 sideStep = new Vector2(-Velocity.Y, Velocity.X).Normalized();
		targetPosition = GlobalPosition + sideStep * 10; // Move to side
		//GD.Print($"{Name} is sidestepping to avoid collision.");
	}
	
	// Look to find partner to mate
	public virtual void TryFindMate()
	{
		if (isDead || Gender != Sex.Male || MateTarget != null || Age < PregnancyAge) return;

		float maxDistance = 200.0f;

		// Loop through and decide if animal is possible mating partner
		foreach (Animal other in GetTree().GetNodesInGroup("Animals"))
		{
			if (other == this) continue;
			if (other.isDead) continue;
			if (other.Species != Species) continue;
			if (other.MateTarget != null) continue;
			if (other.Age < other.PregnancyAge) continue;
			if (other.Gender != Sex.Female) continue;
			if (other.IsPregnant) continue;

			// Make sure found mating partner is not too far away
			float distance = GlobalPosition.DistanceTo(other.GlobalPosition);
			if (distance > maxDistance) continue;

			// Match!
			MateTarget = other;
			other.MateTarget = this;

			targetPosition = other.GlobalPosition;
			GD.Print($"{Name} (male) found {other.Name} (female) and is moving to mate.");
			return;
		}
	}

	public virtual void PerformMating()
	{
		if (MateTarget == null || MateTarget.isDead) return; // Does partner exist? Is partner alive?

		if (MateTarget.Gender == Sex.Female && !MateTarget.IsPregnant) // Is parner female? Not already pregnant?
		{
			MateTarget.IsPregnant = true;
			MateTarget.PregnancyProgress = 0;
			MateTarget.Mother = MateTarget; // Keep track of childs parents for statistics and evolutionary heritage
			MateTarget.Father = this;

			GD.Print($"{Name} mated with {MateTarget.Name}. She is now pregnant.");

			// Print statusmessage
			eatingLabel.Text = "*Mating*";
			eatingLabel.Modulate = new Color(1, 0, 1); // Lila färg
			eatingLabel.Visible = true;

			// Save partner before resetting MateTarget
			Animal partner = MateTarget;

			// Timer for hiding lable after a short while
			var matingTimer = new Timer
			{
				OneShot = true,
				WaitTime = 2.0f
			};
			matingTimer.Timeout += () =>
			{
				eatingLabel.Text = "";
				eatingLabel.Visible = false;
				matingTimer.QueueFree();
			};
			AddChild(matingTimer);
			matingTimer.Start();

			// Timer for reinstating the animal collision again after mating
			var reenableTimer = new Timer
			{
				OneShot = true,
				WaitTime = 2.0f
			};
			reenableTimer.Timeout += () =>
			{
				SetCollisionEnabled(true);
				partner?.SetCollisionEnabled(true);

				if (partner != null)
				{
					partner.MateTarget = null;
				}
				MateTarget = null;

				reenableTimer.QueueFree();
			};
			AddChild(reenableTimer);
			reenableTimer.Start();
		}
	}

	
	public void MoveTowardsMatingTarget(float delta)
	{
		if (MateTarget == null) return;

		// Dynamic position - Male will activly follow mating partner
		Vector2 direction = (MateTarget.GlobalPosition - GlobalPosition).Normalized();

		Velocity = direction * Speed * GameUI.Instance.GameSpeed;
		MoveAndSlide();
		UpdateDirection(direction, delta);

		// When male is close enough to female
		if (GlobalPosition.DistanceTo(MateTarget.GlobalPosition) < 5f)
		{
			Velocity = Vector2.Zero;
			PerformMating();
		}
	}
	
	// Handle decay of dead animals. meatAtDeath is food for carnivores
	public void HandleDecay(float delta)
	{
		if (hasDecayed) return;

		if(meatAtDeath > 0 && isDead)
		{
			meatAtDeath--;
			if(meatAtDeath < 0) meatAtDeath = 0;
		}

		if (meatAtDeath <= 0 && sprite.Frame != 3)
		{
			if(meatAtDeath < 0) meatAtDeath = 0;
			sprite.Frame = 3; // Sceletal remains at last stage of decay
			//GD.Print($"{Name}'s carcass has decayed into remains.");
			decayTimer = CreateTimer(GetAdjustedWaitTime(300.0f), DeleteRemains);
		}
	}

	public void DeleteRemains()
	{
		QueueFree(); // Delete animal from world
		//GD.Print($"{Name}'s remains have fully decayed.");
	}

	// Rest in peace
	public void Die()
	{
		eatingLabel.Text = "*DEAD*";
		isDead = true;
		Speed = 0;
		sprite.Frame = 2; // Death sprite
		//GD.Print($"{Name} has died and is now a carcass.");
	}
}
