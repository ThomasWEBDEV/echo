using Godot;
using System.Collections.Generic;

/// <summary>
/// Générateur procédural de cartes tactiques modulaires (style arène compétitive / CS).
/// Assemble des blocs de murs, obstacles et zones de spawn de manière aléatoire ou semi- procédurale.
/// </summary>
public partial class GenerateurCarte : Node3D
{
    [Export] public int LargeurGrille { get; set; } = 5;
    [Export] public int LongueurGrille { get; set; } = 8;
    [Export] public float TailleCellule { get; set; } = 6.0f;

    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize();
        _GenererNouvelleArene();
    }

    private void _GenererNouvelleArene()
    {
        GD.Print("[CARTE] Génération d'une nouvelle arène tactique modulaire...");

        var matMur = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.18f, 0.20f, 0.25f),
            Roughness = 0.5f,
            Metallic = 0.7f
        };

        var matObstacle = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.25f, 0.22f, 0.18f),
            Roughness = 0.6f,
            Metallic = 0.3f
        };

        // Création des blocs intérieurs aléatoires (couvertures / murets)
        for (int x = -LargeurGrille; x <= LargeurGrille; x += 2)
        {
            for (int z = -LongueurGrille; z <= LongueurGrille; z += 3)
            {
                // Eviter de spawner sur le joueur au centre (0,0)
                if (Mathf.Abs(x) <= 2 && Mathf.Abs(z) <= 3) continue;

                if (_rng.Randf() > 0.4f)
                {
                    Vector3 position = new Vector3(x * (TailleCellule / 2f), 0.75f, z * (TailleCellule / 2f));
                    Vector3 taille = new Vector3(_rng.RandfRange(1.2f, 2.5f), 1.5f, _rng.RandfRange(1.2f, 2.5f));
                    
                    _CreerBlocObstacle(position, taille, matObstacle);
                }
            }
        }

        GD.Print("[CARTE] Arène générée avec succès ! Prêt pour le combat.");
    }

    private void _CreerBlocObstacle(Vector3 position, Vector3 taille, StandardMaterial3D material)
    {
        var obstacle = new StaticBody3D();
        AddChild(obstacle);
        obstacle.GlobalPosition = position;

        var meshInstance = new MeshInstance3D();
        var boxMesh = new BoxMesh { Size = taille };
        meshInstance.Mesh = boxMesh;
        meshInstance.SetSurfaceOverrideMaterial(0, material);
        obstacle.AddChild(meshInstance);

        var colShape = new CollisionShape3D();
        var boxShape = new BoxShape3D { Size = taille };
        colShape.Shape = boxShape;
        obstacle.AddChild(colShape);
    }
}
