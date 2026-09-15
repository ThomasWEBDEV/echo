using Godot;

public partial class Personnage : CharacterBody3D
{
        [Export] public float VitesseCourse = 5.5f;
        [Export] public float VitesseMarche = 2.5f;
        [Export] public float Gravite = 14.0f;
        [Export] public float ForceSaut = 5.0f;
        [Export] public float SensibiliteSouris = 0.002f;
        [Export] public float PitchMin = -50.0f;
        [Export] public float PitchMax = 25.0f;
        [Export] public int   MaxMunitions       = 30;
        [Export] public float TempsRechargement  = 2.0f;
        [Export] public float PointsDeVie        = 100f;
        [Export] public float ForceDash    = 14.0f;
        [Export] public float DureeDash    = 0.18f;
        [Export] public float CooldownDash = 1.5f;

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
        private Label _labelSante;

        private float  _pvActuels;
        private string _etatCourant = "";
        private bool _enSaut = false;
        private bool _etaitEnAir = false; // pour détecter l'atterrissage
        private bool _enLanding = false;
        private float _timerLanding = 0f;
        private const float DUREE_LANDING = 0.4f;

        private bool    _enDash              = false;
        private float   _timerDash           = 0f;
        private float   _cooldownDashRestant = 0f;
        private Vector3 _directionDash       = Vector3.Zero;
        private bool _modeFPS = false;
        private bool _sourisRecaptureeCeFrame = false;
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

                _cameraTPS.MakeCurrent();
                _crosshairH.Visible = false;
                _crosshairV.Visible = false;

                AddToGroup("joueur");
                _pvActuels          = PointsDeVie;
                _munitionsActuelles = MaxMunitions;

                _labelMunitions = GetNodeOrNull<Label>("HUD/LabelMunitions");
                _labelScore     = GetNodeOrNull<Label>("HUD/LabelScore");
                _labelSante     = GetNodeOrNull<Label>("HUD/LabelSante");
                _sonTir         = GetNodeOrNull<AudioStreamPlayer3D>("SonTir");
                if (_labelMunitions != null) _labelMunitions.Visible = false;
                if (_labelScore     != null) _labelScore.Visible     = false;

                Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        public override void _Input(InputEvent @event)
        {
                if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
                {
                        if (_modeFPS)
                        {
                                _tete.RotateY(-mouseMotion.Relative.X * SensibiliteSouris);
                                float pitch = Mathf.Clamp(
                                        _cameraFPS.RotationDegrees.X - mouseMotion.Relative.Y * Mathf.RadToDeg(SensibiliteSouris),
                                        PitchMin, PitchMax
                                );
                                _cameraFPS.RotationDegrees = new Vector3(pitch, 0, 0);
                        }
                        else
                        {
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
                        if (keyEvent.Keycode == Key.F)
                                _BasculerMode();
                        else if (keyEvent.Keycode == Key.Escape)
                                Input.MouseMode = Input.MouseModeEnum.Visible;
                        else if (keyEvent.Keycode == Key.R && _modeFPS && !_enRechargement
                                         && _munitionsActuelles < MaxMunitions)
                                _Recharger();
                }

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
                bool surSol = IsOnFloor();

                if (!surSol)
                        velocity.Y -= Gravite * (float)delta;

                // Détection atterrissage
                if (_etaitEnAir && surSol)
                {
                        _enLanding = true;
                        _timerLanding = 0f;
                        _enSaut = false;
                        _ChangerEtat("Landing");
                }
                _etaitEnAir = !surSol;

                // Timer landing
                if (_enLanding)
                {
                        _timerLanding += (float)delta;
                        if (_timerLanding >= DUREE_LANDING)
                                _enLanding = false;
                }

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

                bool marcher = Input.IsPhysicalKeyPressed(Key.Shift);
                float vitesse = marcher ? VitesseMarche : VitesseCourse;

                if (direction.LengthSquared() > 0.01f)
                {
                        velocity.X = direction.X * vitesse;
                        velocity.Z = direction.Z * vitesse;

                        if (surSol && !_enSaut && !_enLanding)
                                _ChangerEtat(marcher ? "Walking" : "Running");

                        Vector3 cible = _ybot.GlobalPosition - new Vector3(direction.X, 0, direction.Z);
                        _ybot.LookAt(cible, Vector3.Up);
                }
                else
                {
                        velocity.X = 0;
                        velocity.Z = 0;

                        if (surSol && !_enSaut && !_enLanding)
                                _ChangerEtat("Idle");
                }

                // Animation en l'air
                if (!surSol && _enSaut)
                        _ChangerEtat("FallingIdle");

                // Dash
                if (_cooldownDashRestant > 0f)
                        _cooldownDashRestant -= (float)delta;

                if (Input.IsActionJustPressed("dasher") && surSol && !_enDash && _cooldownDashRestant <= 0f)
                {
                        if (direction.LengthSquared() > 0.01f)
                                _directionDash = direction;
                        else
                        {
                                Vector3 fwd = new Vector3(-referenceCamera.GlobalBasis.Z.X, 0f, -referenceCamera.GlobalBasis.Z.Z);
                                _directionDash = fwd.LengthSquared() > 0.001f ? fwd.Normalized() : Vector3.Forward;
                        }
                        _enDash    = true;
                        _timerDash = 0f;
                }

                if (_enDash)
                {
                        _timerDash += (float)delta;
                        velocity.X  = _directionDash.X * ForceDash;
                        velocity.Z  = _directionDash.Z * ForceDash;
                        if (_timerDash >= DureeDash)
                        {
                                _enDash              = false;
                                _cooldownDashRestant = CooldownDash;
                        }
                }

                // Saut
                if (Input.IsActionJustPressed("sauter") && surSol && !_enLanding)
                {
                        bool enCourse = _etatCourant == "Running";
                        velocity.Y = ForceSaut;
                        _enSaut = true;
                        _ChangerEtat(enCourse ? "RunningJump" : "Jump");
                }

                // Tir
                if (_modeFPS && Input.IsActionJustPressed("tirer")
                        && Input.MouseMode == Input.MouseModeEnum.Captured
                        && !_sourisRecaptureeCeFrame)
                {
                        if (_munitionsActuelles > 0 && !_enRechargement)
                                _Tirer();
                        else if (!_enRechargement)
                                _Recharger();
                }

                _sourisRecaptureeCeFrame = false;
                _ActualiserHUD();

                Velocity = velocity;
                MoveAndSlide();
        }

        private void _BasculerMode()
        {
                _modeFPS = !_modeFPS;

                if (_modeFPS)
                {
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

        private void _Tirer()
        {
                _munitionsActuelles--;
                ScoreManager.EnregistrerTir();
                _sonTir?.Play();
                _ActualiserHUD();

                var espace    = GetWorld3D().DirectSpaceState;
                var origine   = _cameraFPS.GlobalPosition;
                var direction = -_cameraFPS.GlobalBasis.Z;
                var query     = PhysicsRayQueryParameters3D.Create(origine, origine + direction * 100f);
                query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
                var result    = espace.IntersectRay(query);

                Vector3 impact = result.Count > 0
                        ? result["position"].As<Vector3>()
                        : origine + direction * 100f;
                TraceurTir.Afficher(this, origine, impact);

                if (result.Count == 0) return;

                var collider = result["collider"].As<GodotObject>();

                if (collider is Cible cible)
                        cible.TakeHit();
                else if (collider is Drone drone)
                        drone.TakeHit();
        }

        private void _Recharger()
        {
                _enRechargement = true;
                GetTree().CreateTimer(TempsRechargement).Timeout += () =>
                {
                        _munitionsActuelles = MaxMunitions;
                        _enRechargement = false;
                        _ActualiserHUD();
                };
        }

        public void TakeHit(float degats)
        {
                _pvActuels = Mathf.Max(0f, _pvActuels - degats);
                _ActualiserHUD();
                if (_pvActuels <= 0f)
                        _Mourir();
        }

        private void _Mourir()
        {
                GetTree().ReloadCurrentScene();
        }

        private void _ActualiserHUD()
        {
                if (_labelMunitions != null)
                        _labelMunitions.Text = _enRechargement
                                ? "RECHARGEMENT..."
                                : $"{_munitionsActuelles}/{MaxMunitions}";
                if (_labelScore != null)
                        _labelScore.Text = $"Cibles : {ScoreManager.CiblesDetruites}";
                if (_labelSante != null)
                {
                        _labelSante.Text = $"PV : {(int)_pvActuels}/{(int)PointsDeVie}";
                        float ratio = Mathf.Clamp(_pvActuels / PointsDeVie, 0f, 1f);
                        Color couleurSante = ratio > 0.5f
                                ? new Color(0.2f, 1f, 0.2f).Lerp(new Color(1f, 1f, 0.1f), (1f - ratio) * 2f)
                                : new Color(1f, 1f, 0.1f).Lerp(new Color(1f, 0.15f, 0.1f), (0.5f - ratio) * 2f);
                        _labelSante.AddThemeColorOverride("font_color", couleurSante);
                }
        }

        private void _ChangerEtat(string nouvelEtat)
        {
                if (_etatCourant == nouvelEtat) return;
                _etatCourant = nouvelEtat;
                _sm.Travel(nouvelEtat);
        }
}

// [AJOUT AUTOMATIQUE COMMIT 1]
// Variables et logique de stamina
[Export] public float MaxStamina { get; set; } = 100.0f;
[Export] public float StaminaDrainRate { get; set; } = 30.0f;
[Export] public float StaminaRegenRate { get; set; } = 20.0f;
private float _currentStamina;
private bool _isExhausted = false;

// [AJOUT AUTOMATIQUE COMMIT 2]
// Gestion de la vitesse et de l'épuisement
// (À intégrer dans votre boucle _PhysicsProcess)

// [AJOUT AUTOMATIQUE COMMIT 3]
[Export] private NodePath staminaLabelPath;
private Label _staminaLabel;
