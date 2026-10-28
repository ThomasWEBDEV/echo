using Godot;

public partial class MenuPrincipal : Control
{
    public override void _Ready()
    {
        // S'assure que la souris est libre et visible dans le menu
        Input.MouseMode = Input.MouseModeEnum.Visible;

        var panel = new ColorRect();
        panel.Color = new Color(0.04f, 0.05f, 0.08f, 1f);
        panel.AnchorRight = 1f;
        panel.AnchorBottom = 1f;
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AnchorLeft = 0.5f;
        vbox.AnchorTop = 0.5f;
        vbox.AnchorRight = 0.5f;
        vbox.AnchorBottom = 0.5f;
        vbox.GrowHorizontal = Control.GrowDirection.Both;
        vbox.GrowVertical = Control.GrowDirection.Both;
        vbox.OffsetLeft = -150f;
        vbox.OffsetTop = -100f;
        vbox.OffsetRight = 150f;
        vbox.OffsetBottom = 100f;
        vbox.AddThemeConstantOverride("separation", 20);
        AddChild(vbox);

        var titre = new Label();
        titre.Text = "ECHO // TRAINING RANGE";
        titre.HorizontalAlignment = HorizontalAlignment.Center;
        titre.AddThemeFontSizeOverride("font_size", 32);
        titre.AddThemeColorOverride("font_color", new Color(0f, 0.8f, 1f));
        vbox.AddChild(titre);

        var btnJouer = new Button();
        btnJouer.Text = "LOUER L'ENTRAÎNEMENT";
        btnJouer.CustomMinimumSize = new Vector2(300, 50);
        btnJouer.Pressed += OnJouerPressed;
        vbox.AddChild(btnJouer);

        var btnQuitter = new Button();
        btnQuitter.Text = "QUITTER";
        btnQuitter.CustomMinimumSize = new Vector2(300, 50);
        btnQuitter.Pressed += OnQuitterPressed;
        vbox.AddChild(btnQuitter);

        GD.Print("[MENU] Menu principal initialisé.");
    }

    private void OnJouerPressed()
    {
        GD.Print("[MENU] Lancement de la salle d'entraînement...");
        // Change vers ta scène de salle d'entraînement
        GetTree().ChangeSceneToFile("res://scenes/salle_entrainement.tscn");
    }

    private void OnQuitterPressed()
    {
        GD.Print("[MENU] Fermeture du jeu.");
        GetTree().Quit();
    }
}
