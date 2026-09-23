using Godot;

/// <summary>
/// Nœud de gestion de la session d'entraînement.
/// Tab = affiche/cache le panneau de stats en overlay.
/// </summary>
public partial class SessionStats : Node
{
        [Export] public float IntervallePrint = 30f;
        [Export] public NodePath CheminLabelTimer;

        private float _timerPrint  = 0f;
        private float _timerLabel  = 0f;
        private Label _labelTimer;

        // Panneau stats overlay
        private CanvasLayer _overlay;
        private Label       _labelStats;
        private bool        _overlayVisible = false;

        public override void _Ready()
        {
                ScoreManager.Reinitialiser();
                GD.Print("[SESSION] Entraînement démarré. Bonne chance !");
                GD.Print("[SESSION] Commandes : F = bascule TPS/FPS | R = recharger | Tab = stats | Échap = libérer souris");

                if (CheminLabelTimer != null && !CheminLabelTimer.IsEmpty)
                        _labelTimer = GetNodeOrNull<Label>(CheminLabelTimer);

                _CreerOverlay();
        }

        private void _CreerOverlay()
        {
                _overlay = new CanvasLayer();
                _overlay.Layer = 10;

                var fond = new ColorRect();
                fond.Color = new Color(0f, 0f, 0f, 0.75f);
                fond.AnchorLeft   = 0.2f;
                fond.AnchorTop    = 0.2f;
                fond.AnchorRight  = 0.8f;
                fond.AnchorBottom = 0.8f;
                _overlay.AddChild(fond);

                _labelStats = new Label();
                _labelStats.AnchorLeft   = 0.2f;
                _labelStats.AnchorTop    = 0.2f;
                _labelStats.AnchorRight  = 0.8f;
                _labelStats.AnchorBottom = 0.8f;
                _labelStats.HorizontalAlignment = HorizontalAlignment.Center;
                _labelStats.VerticalAlignment   = VerticalAlignment.Center;
                _labelStats.AddThemeColorOverride("font_color", new Color(0.2f, 1f, 0.9f));
                _labelStats.AddThemeFontSizeOverride("font_size", 22);
                _overlay.AddChild(_labelStats);

                AddChild(_overlay);
                _overlay.Visible = false;
        }

        public override void _Input(InputEvent @event)
        {
                if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Tab)
                {
                        _overlayVisible = !_overlayVisible;
                        _overlay.Visible = _overlayVisible;

                        if (_overlayVisible)
                        {
                                float t   = ScoreManager.TempsSession;
                                int   min = (int)(t / 60);
                                int   sec = (int)(t % 60);
                                _labelStats.Text =
                                        $"── STATS SESSION ──\n\n" +
                                        $"Temps         {min:00}:{sec:00}\n" +
                                        $"Cibles        {ScoreManager.CiblesDetruites}\n" +
                                        $"Tirs tirés    {ScoreManager.TirsTires}\n" +
                                        $"Précision     {ScoreManager.Precision:F1} %\n\n" +
                                        $"[Tab] pour fermer";
                        }
                }
        }

        public override void _Process(double delta)
        {
                ScoreManager.AjouterTemps((float)delta);

                if (_labelTimer != null)
                {
                        _timerLabel += (float)delta;
                        if (_timerLabel >= 1f)
                        {
                                _timerLabel = 0f;
                                float t   = ScoreManager.TempsSession;
                                int   min = (int)(t / 60);
                                int   sec = (int)(t % 60);
                                _labelTimer.Text = $"{min:00}:{sec:00}  |  {ScoreManager.CiblesDetruites} cibles  |  {ScoreManager.Precision:F0}%";
                        }
                }

                if (IntervallePrint <= 0f) return;

                _timerPrint += (float)delta;
                if (_timerPrint >= IntervallePrint)
                {
                        _timerPrint = 0f;
                        _PrintStats();
                }
        }

        private void _PrintStats()
        {
                float t   = ScoreManager.TempsSession;
                int   min = (int)(t / 60);
                int   sec = (int)(t % 60);
                GD.Print($"[SESSION] {min:00}:{sec:00} | " +
                         $"Cibles : {ScoreManager.CiblesDetruites} | " +
                         $"Tirs : {ScoreManager.TirsTires} | " +
                         $"Précision : {ScoreManager.Precision:F1}%");
        }
}
