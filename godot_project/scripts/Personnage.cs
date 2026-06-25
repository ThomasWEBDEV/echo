using Godot;

public partial class Personnage : CharacterBody3D
{
	[Export] public float VitesseMarche = 3.0f;
	[Export] public float VitesseCourse = 6.0f;
	[Export] public float Gravite = 9.8f;

	private AnimationTree _animTree;
	private AnimationNodeStateMachinePlayback _sm;

	public override void _Ready()
	{
		_animTree = GetNode<AnimationTree>("ybot/AnimationTree");
		_animTree.Active = true;
		_sm = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
		_sm.Travel("Idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Gravité
		if (!IsOnFloor())
			velocity.Y -= Gravite * (float)delta;

		// Input direction
		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = new Vector3(input.X, 0, input.Y).Normalized();

		bool courir = Input.IsActionPressed("ui_accept"); // Shift plus tard

		if (direction.Length() > 0.1f)
		{
			float vitesse = courir ? VitesseCourse : VitesseMarche;
			velocity.X = direction.X * vitesse;
			velocity.Z = direction.Z * vitesse;

			if (courir)
				_sm.Travel("Running");
			else
				_sm.Travel("Walking");

			// Rotation vers la direction du mouvement
			LookAt(GlobalPosition + new Vector3(direction.X, 0, direction.Z), Vector3.Up);
		}
		else
		{
			velocity.X = 0;
			velocity.Z = 0;
			_sm.Travel("Idle");
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
