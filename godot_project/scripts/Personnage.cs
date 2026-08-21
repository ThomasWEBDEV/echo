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
	// Munitions — réglables dans l'inspecteur
	[Export] public int   MaxMunitions       = 30;
	[Export] public float TempsRechargement  = 2.0f;

	private SpringArm3D _springArm;
	private Node3D _tete;
	private Camera3D _cameraTPS;
	private Camera3D _cameraFPS;
	private Node3D _ybot;
	private AnimationTree _animTree;
	private AnimationNodeStateMachinePlayback _sm;
	private ColorRect _crosshairH;
	private ColorRect _crosshairV;
	private Label _labelMunitions;
	private Label _labelScore;
	private AudioStreamPlayer3D _sonTir;

	private string _etatCourant = "";
	private bool _enSaut = false;
	// false = TPS (défaut), true = FPS
	private bool _modeFPS = false;
	// Empêche de tirer au frame où on recapture la souris
	private bool _sourisRecaptureeCeFrame = false;

	// Munitions
	private int  _munitionsActuelles;
	private bool _enRechargement = false;

	public override void _Ready()
	{
		_springArm  = GetNode<SpringArm3D>("SpringArm3D");
		_tete       = GetNode<Node3D>("Tete");
		_cameraTPS  = GetNode<Camera3D>("SpringArm3D/CameraTPS");
		_cameraFPS  = GetNode<Camera3D>("Tete/CameraFPS");
_ybot       = GetNode<Node3D>("ybot");
		_animTree   = GetNode<AnimationTree>("ybot/AnimationTree");
		_animTree.Active = true;
		_sm = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
		_ChangerEtat("Idle");

		_crosshairH = GetNode<ColorRect>("HUD/CrosshairH");
		_crosshairV = GetNode<ColorRect>("HUD/CrosshairV");

		// TPS par défaut : caméra TPS active, ybot visible, viseur caché
		_cameraTPS.MakeCurrent();
		_crosshairH.Visible = false;
		_crosshairV.Visible = false;

		_munitionsActuelles = MaxMunitions;

		_labelMunitions = GetNodeOrNull<Label>("HUD/LabelMunitions");
		_labelScore     = GetNodeOrNull<Label>("HUD/LabelScore");
		_sonTir         = GetNodeOrNull<AudioStreamPlayer3D>("SonTir");
		// Cachés au démarrage (mode TPS par défaut)
		if (_labelMunitions != null) _labelMunitions.Visible = false;
		if (_labelScore     != null) _labelScore.Visible     = false;

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		// Rotation caméra à la souris
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			if (_modeFPS)
			{
				// FPS : la tête tourne en Y, la caméra FPS pitch en X
				_tete.RotateY(-mouseMotion.Relative.X * SensibiliteSouris);
				float pitch = Mathf.Clamp(
					_cameraFPS.RotationDegrees.X - mouseMotion.Relative.Y * Mathf.RadToDeg(SensibiliteSouris),
					PitchMin, PitchMax
				);
				_cameraFPS.RotationDegrees = new Vector3(pitch, 0, 0);
			}
			else
			{
				// TPS : le SpringArm3D tourne
				_springArm.RotateY(-mouseMotion.Relative.X * SensibiliteSouris);
				float pitch = Mathf.Clamp(
					_springArm.RotationDegrees.X - mouseMotion.Relative.Y * Mathf.RadToDeg(SensibiliteSouris),
					PitchMin, PitchMax
				);
				_springArm.RotationDegrees = new Vector3(pitch, _springArm.RotationDegrees.Y, 0);
			}
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			// F : basculer entre TPS et FPS
			if (keyEvent.Keycode == Key.F)
				_BasculerMode();
			// Échap : libérer la souris
			else if (keyEvent.Keycode == Key.Escape)
				Input.MouseMode = Input.MouseModeEnum.Visible;
			// R : rechargement manuel
			else if (keyEvent.Keycode == Key.R && _modeFPS && !_enRechargement
					 && _munitionsActuelles < MaxMunitions)
				_Recharger();
		}

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

		// Axes de déplacement selon le mode actif
		Node3D referenceCamera = _modeFPS ? _tete : _springArm;
		Vector3 camAvant = -referenceCamera.GlobalBasis.Z;
		camAvant.Y = 0;
		if (camAvant.LengthSquared() > 0.001f) camAvant = camAvant.Normalized();

		Vector3 camDroite = referenceCamera.GlobalBasis.X;
		camDroite.Y = 0;
		if (camDroite.LengthSquared() > 0.001f) camDroite = camDroite.Normalized();

		Vector2 inputAxes = Input.GetVector("aller_gauche", "aller_droite", "avancer", "reculer");
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

		// Tir : seulement en mode FPS, avec munitions disponibles
		if (_modeFPS && Input.IsActionJustPressed("tirer")
			&& Input.MouseMode == Input.MouseModeEnum.Captured
			&& !_sourisRecaptureeCeFrame)
		{
			if (_munitionsActuelles > 0 && !_enRechargement)
				_Tirer();
			else if (!_enRechargement)
				_Recharger(); // rechargement auto si à court
		}

		_sourisRecaptureeCeFrame = false;
		_ActualiserHUD();

		Velocity = velocity;
		MoveAndSlide();
	}

	// Bascule entre le mode TPS et le mode FPS
	private void _BasculerMode()
	{
		_modeFPS = !_modeFPS;

		if (_modeFPS)
		{
			// Aligner la tête sur la rotation du SpringArm pour éviter un saut de caméra
			_tete.GlobalRotation = new Vector3(0, _springArm.GlobalRotation.Y, 0);
			_cameraFPS.RotationDegrees = new Vector3(_springArm.RotationDegrees.X, 0, 0);
			_cameraFPS.MakeCurrent();
			_ybot.Visible = false;
			_crosshairH.Visible = true;
			_crosshairV.Visible = true;
			if (_labelMunitions != null) _labelMunitions.Visible = true;
			if (_labelScore     != null) _labelScore.Visible     = true;
		}
		else
		{
			_cameraTPS.MakeCurrent();
			_ybot.Visible = true;
			_crosshairH.Visible = false;
			_crosshairV.Visible = false;
			if (_labelMunitions != null) _labelMunitions.Visible = false;
			if (_labelScore     != null) _labelScore.Visible     = false;
		}
	}

	// Détecte l'impact via raycast physique (sans nœud RayCast3D pour éviter tout rendu)
	private void _Tirer()
	{
		_munitionsActuelles--;
		ScoreManager.EnregistrerTir();
		_sonTir?.Play();
		_ActualiserHUD();
		GD.Print($"[TIRER] Munitions : {_munitionsActuelles}/{MaxMunitions}");

		var espace = GetWorld3D().DirectSpaceState;
		var origine = _cameraFPS.GlobalPosition;
		var direction = -_cameraFPS.GlobalBasis.Z;
		var query = PhysicsRayQueryParameters3D.Create(origine, origine + direction * 100f);
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		var result = espace.IntersectRay(query);

		if (result.Count == 0) return;

		var collider = result["collider"].As<GodotObject>();
		Vector3 impact = result["position"].As<Vector3>();
		GD.Print($"[TIRER] {(collider as Node)?.Name} — impact : {impact}");

		// Appel TakeHit si le collider est une cible
		if (collider is Cible cible)
			cible.TakeHit();
		else if (collider is Drone drone)
			drone.TakeHit();
	}

	// Rechargement avec délai (auto ou manuel avec R)
	private void _Recharger()
	{
		_enRechargement = true;
		GD.Print($"[RECHARGER] Rechargement en cours ({TempsRechargement}s)...");

		GetTree().CreateTimer(TempsRechargement).Timeout += () =>
		{
			_munitionsActuelles = MaxMunitions;
			_enRechargement = false;
			_ActualiserHUD();
			GD.Print($"[RECHARGER] Prêt — {_munitionsActuelles}/{MaxMunitions}");
		};
	}

	// Met à jour les labels HUD munitions et score
	private void _ActualiserHUD()
	{
		if (_labelMunitions != null)
			_labelMunitions.Text = _enRechargement
				? "RECHARGEMENT..."
				: $"{_munitionsActuelles}/{MaxMunitions}";
		if (_labelScore != null)
			_labelScore.Text = $"Cibles : {ScoreManager.CiblesDetruites}";
	}

	private void _ChangerEtat(string nouvelEtat)
	{
		if (_etatCourant == nouvelEtat) return;
		_etatCourant = nouvelEtat;
		_sm.Travel(nouvelEtat);
	}
}
