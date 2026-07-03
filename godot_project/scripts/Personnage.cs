using Godot;

public partial class Personnage : CharacterBody3D
{
        [Export] public float VitesseMarche = 3.0f;
        [Export] public float VitesseCourse = 6.0f;
        [Export] public float Gravite = 9.8f;
        [Export] public float ForceSaut = 5.0f;
        [Export] public float SensibiliteSouris = 0.005f;

        private AnimationTree _animTree;
        private AnimationNodeStateMachinePlayback _sm;
        private string _etatCourant = "";
        private SpringArm3D _springArm;

        public override void _Ready()
        {
                _animTree = GetNode<AnimationTree>("ybot/AnimationTree");
                _animTree.Active = true;
                _sm = (AnimationNodeStateMachinePlayback)_animTree.Get("parameters/playback");
                _ChangerEtat("Idle");

                _springArm = GetNode<SpringArm3D>("SpringArm3D");
                Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
                if (@event is InputEventMouseMotion mouseMotion)
                {
                        RotateY(-mouseMotion.Relative.X * SensibiliteSouris);
                        _springArm.RotateX(-mouseMotion.Relative.Y * SensibiliteSouris);

                        Vector3 rot = _springArm.Rotation;
                        rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-60), Mathf.DegToRad(10));
                        _springArm.Rotation = rot;
                }

                if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
                {
                        Input.MouseMode = Input.MouseModeEnum.Visible;
                }
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

                // Gravité
                if (!IsOnFloor())
                        velocity.Y -= Gravite * (float)delta;

                // Saut sur Espace uniquement si au sol
                if (IsOnFloor() && Input.IsKeyPressed(Key.Space))
                {
                        velocity.Y = ForceSaut;
                        _ChangerEtat("Jump");
                }

                Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
                Vector3 direction = new Vector3(input.X, 0, input.Y).Normalized();

                // Shift = marcher, par défaut on court
                bool marcher = Input.IsKeyPressed(Key.Shift);

                if (direction.Length() > 0.1f)
                {
                        float vitesse = marcher ? VitesseMarche : VitesseCourse;
                        velocity.X = direction.X * vitesse;
                        velocity.Z = direction.Z * vitesse;

                        // Animation uniquement si au sol
                        if (IsOnFloor())
                        {
                                if (marcher)
                                        _ChangerEtat("Walking");
                                else
                                        _ChangerEtat("Running");
                        }

                        LookAt(GlobalPosition + new Vector3(direction.X, 0, direction.Z), Vector3.Up);
                }
                else
                {
                        velocity.X = 0;
                        velocity.Z = 0;

                        if (IsOnFloor())
                                _ChangerEtat("Idle");
                }

                Velocity = velocity;
                MoveAndSlide();
        }
}
