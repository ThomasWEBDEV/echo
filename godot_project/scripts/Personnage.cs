using Godot;

public partial class Personnage : CharacterBody3D
{
	// Vitesses de déplacement — réglables dans l'inspecteur
	[Export] public float VitesseCourse = 5.5f;
	[Export] public float VitesseMarche = 2.5f;
	[Export] public float Gravite = 14.0f;
	[Export] public float ForceSaut = 5.0f;
	// Sensibilité de la souris
	[Export] public float SensibiliteSouris = 0.002f;
	// Limites verticales de la caméra en degrés
	[Export] public float PitchMin = -50.0f;
	[Export] public float PitchMax = 25.0f;

	private SpringArm3D _springArm;
	private Node3D _ybot;
	private RayCast3D _rayCast;
	private AnimationTree _animTree;
	private AnimationNodeStateMachinePlayback _sm;
	private string _etatCourant = "";
	private bool _enSaut = false;
	// Empêche de tirer au frame où on recapture la souris
	private bool _sourisRecaptureeCeFrame = false;

	public override void _Ready()
	{
		_springArm = GetNode<SpringArm3D>("SpringArm3D");
		_ybot = GetNode<Node3D>("ybot");
		_rayCast = GetNode<RayCast3D>("SpringArm3D/Camera3D/RayCast3D");
		_animTree = GetNode<AnimationTree>("ybot/AnimationTree");
		_animTree.Active = true;
		_sm = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
		_ChangerEtat("Idle");

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		// Rotation caméra à la souris (TPS — SpringArm3D tourne)
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_springArm.RotateY(-mouseMotion.Relative.X * SensibiliteSouris);

			float pitch = Mathf.Clamp(
				_springArm.RotationDegrees.X - mouseMotion.Relative.Y * Mathf.RadToDeg(SensibiliteSouris),
				PitchMin, PitchMax
			);
			_springArm.RotationDegrees = new Vector3(pitch, _springArm.RotationDegrees.Y, 0);
		}

		// Échap : libérer la souris
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
			Input.MouseMode = Input.MouseModeEnum.Visible;

		// Clic gauche sur fond visible : recapturer sans déclencher un tir
		if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed
			&& mouseBtn.ButtonIndex == MouseButton.Left
			&& Input.MouseMode == Input.MouseModeEnum.Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			_sourisRecaptureeCeFrame = true;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y -= Gravite * (float)delta;

		// Déplacement WASD relatif à la caméra (plan horizontal)
		Vector3 camAvant = -_springArm.GlobalBasis.Z;
		camAvant.Y = 0;
		if (camAvant.LengthSquared() > 0.001f) camAvant = camAvant.Normalized();

		Vector3 camDroite = _springArm.GlobalBasis.X;
		camDroite.Y = 0;
		if (camDroite.LengthSquared() > 0.001f) camDroite = camDroite.Normalized();

		Vector2 inputAxes = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = camAvant * (-inputAxes.Y) + camDroite * inputAxes.X;
		if (direction.LengthSquared() > 0.01f)
			direction = direction.Normalized();

		// Shift = marche lente, défaut = course
		bool marcher = Input.IsPhysicalKeyPressed(Key.Shift);
		float vitesse = marcher ? VitesseMarche : VitesseCourse;

		if (direction.LengthSquared() > 0.01f)
		{
			velocity.X = direction.X * vitesse;
			velocity.Z = direction.Z * vitesse;

			if (IsOnFloor() && !_enSaut)
				_ChangerEtat(marcher ? "Walking" : "Running");

			// Rotation du mesh vers la direction de déplacement
			Vector3 cible = _ybot.GlobalPosition - new Vector3(direction.X, 0, direction.Z);
			_ybot.LookAt(cible, Vector3.Up);
		}
		else
		{
			velocity.X = 0;
			velocity.Z = 0;

			if (IsOnFloor() && !_enSaut)
				_ChangerEtat("Idle");
		}

		// Saut
		if (Input.IsActionJustPressed("sauter") && IsOnFloor())
		{
			velocity.Y = ForceSaut;
			_enSaut = true;
		}

		if (_enSaut && IsOnFloor() && velocity.Y <= 0)
			_enSaut = false;

		// Tir : clic gauche, RayCast depuis le centre de la caméra
		if (Input.IsActionJustPressed("tirer") && Input.MouseMode == Input.MouseModeEnum.Captured && !_sourisRecaptureeCeFrame)
			_Tirer();

		_sourisRecaptureeCeFrame = false;

		Velocity = velocity;
		MoveAndSlide();
	}

	// Détecte l'impact via RayCast — la logique de dégâts sera dans les cibles
	private void _Tirer()
	{
		if (!_rayCast.IsColliding()) return;

		GodotObject collider = _rayCast.GetCollider();
		Vector3 impact = _rayCast.GetCollisionPoint();
		GD.Print($"[TIRER] {(collider as Node)?.Name} — impact : {impact}");

		// TODO : appeler collider.TakeHit() quand les cibles auront un script de santé
		// TODO : instancier particule d'impact à la position impact
	}

	private void _ChangerEtat(string nouvelEtat)
	{
		if (_etatCourant == nouvelEtat) return;
		_etatCourant = nouvelEtat;
		_sm.Travel(nouvelEtat);
	}
}
