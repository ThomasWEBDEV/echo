using Godot;

public partial class Personnage : CharacterBody3D
{
		[Export] public float VitesseMarche = 3.0f;
		[Export] public float VitesseCourse = 6.0f;
		[Export] public float Gravite = 9.8f;
		[Export] public float ForceSaut = 8.0f;
		// Sensibilité de la souris (réglable dans l'inspecteur)
		[Export] public float SensibiliteSouris = 0.003f;
		// Limites verticales de la caméra en degrés
		[Export] public float PitchMin = -50.0f;
		[Export] public float PitchMax = 25.0f;

		private AnimationTree _animTree;
		private AnimationNodeStateMachinePlayback _sm;
		private string _etatCourant = "";
		private SpringArm3D _springArm;
		private Node3D _ybot;

		public override void _Ready()
		{
				_animTree = GetNode<AnimationTree>("ybot/AnimationTree");
				_animTree.Active = true;
				_sm = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
				_springArm = GetNode<SpringArm3D>("SpringArm3D");
				_ybot = GetNode<Node3D>("ybot");
				_ChangerEtat("Idle");

				// Jump en loop pour éviter le gel sur la dernière frame si le saut est plus long que l'animation
				var animPlayer = GetNode<AnimationPlayer>("ybot/AnimationPlayer");
				var jumpAnim = animPlayer.GetAnimation("Jump");
				if (jumpAnim != null)
						jumpAnim.LoopMode = Animation.LoopModeEnum.Linear;

				// Capturer la souris au démarrage
				Input.MouseMode = Input.MouseModeEnum.Captured;
		}

		public override void _Input(InputEvent @event)
		{
				// Rotation caméra à la souris
				if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
				{
						// Yaw horizontal — rotation du bras caméra autour de Y
						_springArm.RotateY(-mouseMotion.Relative.X * SensibiliteSouris);

						// Pitch vertical — inclinaison haut/bas clampée
						float pitch = Mathf.Clamp(
								_springArm.RotationDegrees.X - mouseMotion.Relative.Y * Mathf.RadToDeg(SensibiliteSouris),
								PitchMin, PitchMax
						);
						_springArm.RotationDegrees = new Vector3(pitch, _springArm.RotationDegrees.Y, 0);
				}

				// Échap : libérer la souris
				if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
						Input.MouseMode = Input.MouseModeEnum.Visible;

				// Clic gauche : recapturer la souris
				if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && Input.MouseMode == Input.MouseModeEnum.Visible)
						Input.MouseMode = Input.MouseModeEnum.Captured;
		}

		public override void _PhysicsProcess(double delta)
		{
				Vector3 velocity = Velocity;
				if (!IsOnFloor())
						velocity.Y -= Gravite * (float)delta;

				// Saut — vientDeSauter empêche Idle/Walk d'écraser Jump dans la même frame
				bool vientDeSauter = false;
				if (Input.IsActionJustPressed("sauter") && IsOnFloor())
				{
						velocity.Y = ForceSaut;
						vientDeSauter = true;
						_ChangerEtat("Jump");
				}

				// Axes de déplacement relatifs à la caméra (plan horizontal uniquement)
				Vector3 camAvant = -_springArm.GlobalBasis.Z;
				camAvant.Y = 0;
				camAvant = camAvant.Normalized();

				Vector3 camDroite = _springArm.GlobalBasis.X;
				camDroite.Y = 0;
				camDroite = camDroite.Normalized();

				Vector2 inputAxes = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
				// ui_up (Y négatif) = avancer → on inverse Y pour obtenir +1 vers l'avant
				Vector3 direction = camAvant * (-inputAxes.Y) + camDroite * inputAxes.X;
				if (direction.Length() > 0.1f)
						direction = direction.Normalized();

				bool courir = Input.IsActionPressed("ui_accept");

				if (direction.Length() > 0.1f)
				{
						float vitesse = courir ? VitesseCourse : VitesseMarche;
						velocity.X = direction.X * vitesse;
						velocity.Z = direction.Z * vitesse;

						if (IsOnFloor() && !vientDeSauter)
						{
								if (courir)
										_ChangerEtat("Running");
								else
										_ChangerEtat("Walking");
						}

						// Rotation du mesh ybot vers la direction de déplacement
						// (le CharacterBody3D ne tourne pas, pour ne pas entraîner la caméra)
						// Inversion : le modèle Mixamo fait face au +Z, LookAt oriente le -Z → on inverse
						Vector3 cible = _ybot.GlobalPosition - new Vector3(direction.X, 0, direction.Z);
						_ybot.LookAt(cible, Vector3.Up);
				}
				else
				{
						velocity.X = 0;
						velocity.Z = 0;

						if (IsOnFloor() && !vientDeSauter)
								_ChangerEtat("Idle");
				}

				Velocity = velocity;
				MoveAndSlide();
		}

		private void _ChangerEtat(string nouvelEtat)
		{
				if (_etatCourant == nouvelEtat) return;
				_etatCourant = nouvelEtat;
				_sm.Travel(nouvelEtat);
		}
}
