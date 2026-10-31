using Godot;

/// <summary>
/// Générateur procédural d'une troisième carte tactique : 'Chantier / Hangar Désertique' (Style Dust2 / Mirage CS).
/// Combine des conteneurs empilés, de la caisse en bois, du relief et des angles d'assaut à découvert.
/// </summary>
public partial class GenerateurCarteChantier : Node3D
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize();
        _GenererMapChantier();
    }

    private void _GenererMapChantier()
    {
        GD.Print("[CARTE 3] Génération de la carte 'Chantier Désertique'...");

        var matConteneurJaune = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.75f, 0.55f, 0.15f),
            Roughness = 0.5f,
            Metallic = 0.6f
        };

        var matConteneurBleu = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.12f, 0.35f, 0.55f),
            Roughness = 0.4f,
            Metallic = 0.7f
        };

        var matCaisseBois = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.45f, 0.32f, 0.18f),
            Roughness = 0.85f,
            Metallic = 0.0f
        };

        // Zone 1 : Conteneurs de fret empilés (ligne de vue haute/basse)
        _CreerBloc(new Vector3(-9f, 1.3f, -10f), new Vector3(2.6f, 2.6f, 6.0f), matConteneurJaune);
        _CreerBloc(new Vector3(-9f, 3.9f, -10f), new Vector3(2.6f, 2.6f, 6.0f), matConteneurBleu);

        _CreerBloc(new Vector3(9f, 1.3f, -12f), new Vector3(2.6f, 2.6f, 6.0f), matConteneurBleu);
        _CreerBloc(new Vector3(6.4f, 1.3f, -12f), new Vector3(2.6f, 2.6f, 4.0f), matConteneurJaune);

        // Zone 2 : Mid / Couloir de caisses tactiques
        _CreerBloc(new Vector3(0f, 0.8f, -6f), new Vector3(1.6f, 1.6f, 1.6f), matCaisseBois);
        _CreerBloc(new Vector3(-1.7f, 0.8f, -6f), new Vector3(1.6f, 1.6f, 1.6f), matCaisseBois);
        _CreerBloc(new Vector3(-0.8f, 2.4f, -6f), new Vector3(1.6f, 1.6f, 1.6f), matCaisseBois); // Caisse supérieure pour saut

        // Zone 3 : Obstacles de flanc et abris d'angle
        _CreerBloc(new Vector3(-4f, 1.0f, -15f), new Vector3(3.5f, 2.0f, 0.8f), matConteneurJaune);
        _CreerBloc(new Vector3(4f, 1.0f, -15f), new Vector3(3.5f, 2.0f, 0.8f), matConteneurBleu);

        GD.Print("[CARTE 3] Carte 'Chantier Désertique' générée avec succès.");
    }

    private void _CreerBloc(Vector3 position, Vector3 taille, StandardMaterial3D material)
    {
        var objet = new StaticBody3D();
        AddChild(objet);
        objet.GlobalPosition = position;

        var meshInstance = new MeshInstance3D();
        var boxMesh = new BoxMesh { Size = taille };
        meshInstance.Mesh = boxMesh;
        meshInstance.SetSurfaceOverrideMaterial(0, material);
        objet.AddChild(meshInstance);

        var colShape = new CollisionShape3D();
        var boxShape = new BoxShape3D { Size = taille };
        colShape.Shape = boxShape;
        objet.AddChild(colShape);
    }
}
