using Godot;

/// <summary>
/// Générateur procédural d'une deuxième carte tactique asymétrique (style map de type labyrinthe industriel / usine).
/// Intègre des couloirs étroits, des plateformes surélevées et des zones de cover multiples.
/// </summary>
public partial class GenerateurCarteComplexe : Node3D
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize();
        _GenererMapUsine();
    }

    private void _GenererMapUsine()
    {
        GD.Print("[CARTE 2] Génération de la carte 'Usine / Complexe'...");

        var matMetalSombre = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.10f, 0.12f, 0.16f),
            Metallic = 0.85f,
            Roughness = 0.35f,
            EmissionEnabled = true,
            Emission = new Color(0.0f, 0.4f, 0.8f),
            EmissionEnergyMultiplier = 0.1f
        };

        var matBriqueRouge = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.35f, 0.18f, 0.14f),
            Roughness = 0.7f,
            Metallic = 0.1f
        };

        // Construction de couloirs et de structures asymétriques
        for (int i = -3; i <= 3; i++)
        {
            float posX = i * 5.0f;
            float posZ = (i % 2 == 0) ? -12.0f : -18.0f;

            _CreerElementMap(new Vector3(posX, 1.25f, posZ), new Vector3(2.5f, 2.5f, 1.2f), matBriqueRouge);
            _CreerElementMap(new Vector3(posX * 0.8f, 2.0f, posZ + 8.0f), new Vector3(3.0f, 0.4f, 3.0f), matMetalSombre);
        }

        // Ajout de grands piliers centraux bloquant les lignes de vue directes (style Cover / Peek)
        _CreerElementMap(new Vector3(-7f, 3f, -5f), new Vector3(1.5f, 6.0f, 1.5f), matMetalSombre);
        _CreerElementMap(new Vector3(7f, 3f, -5f), new Vector3(1.5f, 6.0f, 1.5f), matMetalSombre);
        _CreerElementMap(new Vector3(0f, 3f, -14f), new Vector3(4.0f, 6.0f, 1.2f), matMetalSombre);

        GD.Print("[CARTE 2] Carte 'Usine' générée avec succès ! Asymétrie et angles morts appliqués.");
    }

    private void _CreerElementMap(Vector3 position, Vector3 taille, StandardMaterial3D material)
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
