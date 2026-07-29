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
	[Export] public float PitchMin = -89.0f;
	[Export] public float PitchMax = 89.0f;

	private Node3D _tete;
	private RayCast3D _rayCast;
	private AnimationTree _animTree;
	private AnimationNodeStateMachinePlayback _sm;
	private string _etatCourant = "";
	private bool _enSaut = false;
	// Empêche de tirer au frame où on recapture la souris
	private bool _sourisRecaptureeCeFrame = false;

	public override void _Ready()
	{
		_tete = GetNode<Node3D>("Tete");
		_rayCast = GetNode<RayCast3D>("Tete/Camera3D/RayCast3D");

		// Le mesh ybot reste actif en arrière-plan (utile pour le futur multijoueur)
		_animTree = GetNode<AnimationTree>("ybot/AnimationTree");
		_animTree.Active = true;
		_sm = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
		_ChangerEtat("Idle");

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		// Rotation caméra à la souris
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			// Yaw horizontal — le body entier tourne, ce qui oriente le déplacement WASD
			RotateY(-mouseMotion.Relative.X * SensibiliteSouris);

			// Pitch vertical — seule la tête s'incline, le body reste droit
			float pitch = Mathf.Clamp(
				_tete.RotationDegrees.X - mouseMotion.Relative.Y * Mathf.RadToDeg(SensibiliteSouris),
				PitchMin, PitchMax
			);
			_tete.RotationDegrees = new Vector3(pitch, 0, 0);
		}

		// Échap : libérer la souris (accès menu)
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
			Input.MouseMode = Input.MouseModeEnum.Visible;

		// Clic gauche sur fond visible : recapturer la souris sans tirer
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

		// Déplacement WASD relatif à l'orientation du body (FPS standard)
		Vector2 inputAxes = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		// ui_up (W) donne Y=-1 → on veut avancer → -GlobalBasis.Z
		// -GlobalBasis.Z * -(-1) = -GlobalBasis.Z ✓
		Vector3 direction = (-GlobalBasis.Z * (-inputAxes.Y) + GlobalBasis.X * inputAxes.X);
		if (direction.LengthSquared() > 0.01f)
			direction = direction.Normalized();

		// Shift gauche = marche lente, défaut = course (comme CS2)
		bool marcher = Input.IsPhysicalKeyPressed(Key.Shift);
		float vitesse = marcher ? VitesseMarche : VitesseCourse;

		if (direction.LengthSquared() > 0.01f)
		{
			velocity.X = direction.X * vitesse;
			velocity.Z = direction.Z * vitesse;
		}
		else
		{
			velocity.X = 0;
			velocity.Z = 0;
		}

		// Saut
		if (Input.IsActionJustPressed("sauter") && IsOnFloor())
		{
			velocity.Y = ForceSaut;
			_enSaut = true;
		}

		if (_enSaut && IsOnFloor() && velocity.Y <= 0)
			_enSaut = false;

		// Mise à jour des animations du mesh ybot (invisible en FPS, utile pour le futur)
		if (IsOnFloor() && !_enSaut)
		{
			if (direction.LengthSquared() > 0.01f)
				_ChangerEtat(marcher ? "Walking" : "Running");
			else
				_ChangerEtat("Idle");
		}

		// Tir : clic gauche quand la souris est capturée
		if (Input.IsActionJustPressed("tirer") && Input.MouseMode == Input.MouseModeEnum.Captured && !_sourisRecaptureeCeFrame)
			_Tirer();

		_sourisRecaptureeCeFrame = false;

		Velocity = velocity;
		MoveAndSlide();
	}

	// Détecte l'impact via RayCast et le signale — la logique de dégâts sera dans les cibles
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
