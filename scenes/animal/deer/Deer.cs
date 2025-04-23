using Godot;
using System;

public partial class Deer : Herbivore
{	
	public Deer()
	{
		// Standardvärden
		Species = "Deer";
		Name = "Unnamed Deer";
		Age = 0;
		Energy = 50;
		Health = 100;
		Hunger = 0;
		Thirst = 0;
		baseSize = 1.0f;
		baseSpeed = 20.0f;
		Bravery = 20.0f;
		Aggression = 10.0f;
		Awareness = 70.0f;
		Social = 50.0f;
		meatAtDeath = 750.0f * baseSize;
		
		PregnancySeason = Season.Summer;
		PregnancyAge = 1;
		MinChildren = 1;
		MaxChildren = 1;
		PregnancyDuration = 30; // i dagar
	}

	public override void _Ready()
	{
		ZIndex = 5;

		thirstTimer = CreateTimer(GetAdjustedWaitTime(2.0f), OnThirstTimeout);
		hungerTimer = CreateTimer(GetAdjustedWaitTime(5.0f), OnHungerTimeout);
		energyTimer = CreateTimer(GetAdjustedWaitTime(1.0f), OnEnergyTimeout);

		sprite = GetNode<AnimatedSprite2D>("DeerSprite");
		eatingLabel = GetNode<Label>("EatingLabel");

		SetGenderSprite();
	}
	
	public override void _PhysicsProcess(double delta)
	{
		float adjustedDelta = (float)(delta * GameUI.Instance.GameSpeed);

		// Statusutskrift med intervall
		statusPrintTimer -= adjustedDelta;
		if (statusPrintTimer <= 0)
		{
			GD.Print($"{Gender} {Name}: Hunger = {Hunger:F2}, Thirst = {Thirst:F2}, Energy = {Energy:F2}, Health = {Health:F2}");
			statusPrintTimer = statusPrintCooldown;
		}

		// Dödshantering
		if (Health <= 0)
			isDead = true;

		if (isDead)
		{
			HandleDecay(adjustedDelta);
			return;
		}

		// Äter?
		if (isEating)
		{
			HandleEating(adjustedDelta);
			return;
		}

		// Dricker?
		if (isDrinking)
		{
			HandleDrinking(adjustedDelta);
			return;
		}

		if (Gender == Sex.Female)
		{
			foreach (Animal other in GetTree().GetNodesInGroup("Animals"))
			{
				if (other.MateTarget == this && other.Gender == Sex.Male)
				{
					float distance = GlobalPosition.DistanceTo(other.GlobalPosition);
					if (distance < 20.0f)
					{
						Velocity = Vector2.Zero;
						targetPosition = GlobalPosition; // Så hon inte vandrar iväg
						SetCollisionEnabled(false);
						MateTarget?.SetCollisionEnabled(false);

						// Vänd henne bort från hanen
						Vector2 awayDirection = (GlobalPosition - other.GlobalPosition).Normalized();
						UpdateDirection(awayDirection, (float)(delta * GameUI.Instance.GameSpeed));

						return;
					}
				}
			}
		}

		// 🔥 Hantera parning först – så inget annat stör
		if (Gender == Sex.Male && MateTarget != null && !IsPregnant)
		{
			MoveTowardsMatingTarget(adjustedDelta);

			float distance = GlobalPosition.DistanceTo(MateTarget.GlobalPosition);
			if (distance < 20.0f)
			{
				Velocity = Vector2.Zero;
				targetPosition = GlobalPosition;
				PerformMating();
			}

			return; // Viktigt att vi hoppar över vanlig rörelse
		}

		// 🔎 Försök hitta en partner – om det är rätt säsong
		if (Gender == Sex.Male && MateTarget == null && PregnancySeason == TimeManager.Instance.CurrentSeason)
		{
			TryFindMate();
		}

		// 🚶 Vanlig rörelse (mat, vatten, wander)
		if (targetPosition != Vector2.Zero)
		{
			MoveTowardsTarget(adjustedDelta);
		}
		else
		{
			DecideNextAction(adjustedDelta);
		}
	}
}
