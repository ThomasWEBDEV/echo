using Godot;

public partial class Personnage : CharacterBody3D
{
	[Export] public float VitesseMarche = 3.0f;
	[Export] public float VitesseCourse = 6.0f;
	[Export] public float Gravite = 9.8f;

	private AnimationTree _animTree;
	private AnimationNodeStateMachinePlayback _sm;
	private string _etatCourant = "";

	public override void _Ready()
	{
		_animTree = GetNode<AnimationTree>("ybot/AnimationTree");
		_animTree.Active = true;
		_sm = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
		_ChangerEtat("Idle");
	}

	private void _ChangerEtat(string nouvelEtat)
	{
		if (_etatCourant == nouvelEtat) return;
		_etatCourant = nouvelEtat;
		_sm.Travel(nouvelEtat);
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y -= Gravite * (float)delta;

		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = new Vector3(input.X, 0, input.Y).Normalized();

		bool courir = Input.IsActionPressed("ui_accept");

		if (direction.Length() > 0.1f)
		{
			float vitesse = courir ? VitesseCourse : VitesseMarche;
			velocity.X = direction.X * vitesse;
			velocity.Z = direction.Z * vitesse;

			if (courir)
				_ChangerEtat("Running");
			else
				_ChangerEtat("Walking");

			LookAt(GlobalPosition + new Vector3(direction.X, 0, direction.Z), Vector3.Up);
		}
		else
		{
			velocity.X = 0;
			velocity.Z = 0;
			_ChangerEtat("Idle");
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
