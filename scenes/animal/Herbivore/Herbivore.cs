using Godot;
using System;

public partial class Herbivore : Animal
{
	Grass currentGrass;
	
	public void DecideNextAction(float delta)
	{
		if (!isMovingToFood && !isMovingToWater && !goingForDrink)
		{
			if (Thirst > 20)
			{
				var water = FindNearestWater();
				if (water != Vector2.Zero)
				{
					targetPosition = water;
					isMovingToWater = true;
					goingForDrink = true; // Markera att vi aktivt går för att dricka
					//GD.Print($"{Name} is moving to water: {targetPosition}");
				}
			}
			else if (Hunger > 20)
			{
				var food = FindNearestFood();
				if (food != null)
				{
					targetPosition = food.GlobalPosition;
					isMovingToFood = true;
					//GD.Print($"{Name} is moving to food: {targetPosition}");
				}
			}
			else
			{
				Wander(delta);
			}
		}
	}

	public Grass FindNearestFood()
	{
		Grass nearestGrass = null;
		float nearestDistance = float.MaxValue;

		foreach (var child in GetTree().GetNodesInGroup("Grass"))
		{
			if (child is Grass grass && grass.GetGrowthStage() >= 1)
			{
				float distance = GlobalPosition.DistanceTo(grass.GlobalPosition);
				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearestGrass = grass;
				}
			}
		}
		return nearestGrass;
	}

	public void MoveTowardsTarget(float delta)
	{
		Vector2 direction = (targetPosition - GlobalPosition).Normalized();

		if (direction == Vector2.Zero)
		{
			//GD.PrintErr($"{Name} Direction is zero. Choosing a new target.");
			targetPosition = Vector2.Zero;
			Wander(delta);
			return;
		}

		// Uppdatera Velocity och flytta
		Velocity = direction * Speed * GameUI.Instance.GameSpeed;
		var collision = MoveAndSlide();

		// Kontrollera kollisioner
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var slideCollision = GetSlideCollision(i);
			if (slideCollision != null)
			{
				Node collider = slideCollision.GetCollider() as Node;

				if (collider != null)
				{
					//GD.Print($"{Name} collided with {collider.Name}");

					// Kontrollera om rådjuret kolliderar med vatten
					if (IsWaterTile(slideCollision.GetPosition()))
					{
						if (goingForDrink)
						{
							//GD.Print($"{Name} reached water and starts drinking.");
							StartDrinking();
							return;
						}
						else
						{
							//GD.Print($"{Name} hit water accidentally. Choosing a new target.");
							targetPosition = Vector2.Zero;
							isMovingToFood = false;
							Wander(delta);
							return;
						}
					}

					// Om rådjuret kolliderar med annat, välj ett nytt mål
					//GD.Print($"{Name} collided with {collider.Name}. Choosing a new target.");
					targetPosition = Vector2.Zero;
					Wander(delta);
					return;
				}
			}
		}

		// Uppdatera riktning visuellt
		UpdateDirection(direction, delta);

		// Kontrollera om målet är nått
		if (GlobalPosition.DistanceTo(targetPosition) < 5)
		{
			if (isMovingToWater)
			{
				StartDrinking();
			}
			else if (isMovingToFood)
			{
				EatGrassAt(targetPosition);
			}
			else
			{
				targetPosition = Vector2.Zero;
			}
		}
	}
	
	public void HandleEating(float delta)
	{
		eatTimer -= delta * GameUI.Instance.GameSpeed;
		if (eatTimer <= 0)
		{
			FinishEating(); // Avsluta äthandlingen
		}
	}

	public Grass GetGrassAt(Vector2 position)
	{
		foreach (var child in GetTree().GetNodesInGroup("Grass"))
		{
			if (child is Grass grass && grass.GlobalPosition.DistanceTo(position) < 5)
			{
				return grass;
			}
		}
		return null;
	}

	public void EatGrassAt(Vector2 position)
	{
		var grass = GetGrassAt(position);
		if (grass != null)
		{
			isEating = true;
			isMovingToFood = false; // Stoppa rörelse mot mat
			Speed = 0; // Stanna medan det äter
			eatTimer = eatDuration / GameUI.Instance.GameSpeed; // Starta ättimer
			eatingLabel.Modulate = new Color(1, 0, 0); // Röd färg
			eatingLabel.Text = "*Eating*"; // Visa text
			eatingLabel.Visible = true;
			currentGrass = grass;

			//GD.Print($"{Name} started eating grass at {position}");
		}
		else
		{
			//GD.Print($"{Name} couldn't find grass at {position}. Choosing a new target.");
			targetPosition = Vector2.Zero;
			Wander(0); // Leta efter ett nytt mål
		}
	}

	public void FinishEating()
	{
		isEating = false;
		Speed = 20.0f;
		eatingLabel.Text = ""; // Ta bort text
		eatingLabel.Visible = false;

		if (currentGrass != null)
		{
			int growthStage = currentGrass.GetGrowthStage();
			Hunger -= growthStage switch
			{
				1 => 5,
				2 => 10,
				3 => 20,
				_ => 0
			};

			Hunger = Mathf.Max(Hunger, 0); // Säkerställ att Hunger inte går under 0
			currentGrass.ResetGrowth();
			currentGrass = null;

			//GD.Print($"{Name} finished eating. Hunger now at {Hunger}");
		}
	}
	
}
