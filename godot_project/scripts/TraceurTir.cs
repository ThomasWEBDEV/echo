using Godot;

/// <summary>
/// Utilitaire de rendu — trait lumineux + étincelles à l'impact.
/// </summary>
public static class TraceurTir
{
        public static void Afficher(Node hote, Vector3 debut, Vector3 fin, float duree = 0.06f)
        {
                float longueur = debut.DistanceTo(fin);
                if (longueur < 0.01f) return;

                // ── Trait de tir ──────────────────────────────────────────
                var mesh  = new MeshInstance3D();
                var boite = new BoxMesh();
                boite.Size = new Vector3(0.015f, 0.015f, longueur);
                mesh.Mesh  = boite;

                var mat = new StandardMaterial3D();
                mat.AlbedoColor              = new Color(1f, 0.85f, 0.3f);
                mat.EmissionEnabled          = true;
                mat.Emission                 = new Color(1f, 0.7f, 0.0f);
                mat.EmissionEnergyMultiplier = 4f;
                mat.ShadingMode              = BaseMaterial3D.ShadingModeEnum.Unshaded;
                mesh.SetSurfaceOverrideMaterial(0, mat);

                hote.GetTree().Root.AddChild(mesh);
                mesh.GlobalPosition = (debut + fin) * 0.5f;
                Vector3 dir  = (fin - debut).Normalized();
                Vector3 haut = Mathf.Abs(dir.Dot(Vector3.Up)) < 0.9f ? Vector3.Up : Vector3.Right;
                mesh.LookAt(fin, haut);

                hote.GetTree().CreateTimer(duree).Timeout += () =>
                {
                        if (GodotObject.IsInstanceValid(mesh))
                                mesh.QueueFree();
                };

                // ── Étincelles à l'impact ─────────────────────────────────
                _AfficherEtincelles(hote, fin, dir);
        }

        private static void _AfficherEtincelles(Node hote, Vector3 position, Vector3 directionTir)
        {
                var rng = new RandomNumberGenerator();
                rng.Randomize();

                int nbEtincelles = 8;
                for (int i = 0; i < nbEtincelles; i++)
                {
                        var etincelle = new MeshInstance3D();
                        var sphere    = new SphereMesh();
                        float taille  = rng.RandfRange(0.02f, 0.06f);
                        sphere.Radius = taille;
                        sphere.Height = taille * 2f;
                        etincelle.Mesh = sphere;

                        var matE = new StandardMaterial3D();
                        matE.AlbedoColor              = new Color(1f, rng.RandfRange(0.4f, 0.9f), 0f);
                        matE.EmissionEnabled          = true;
                        matE.Emission                 = matE.AlbedoColor;
                        matE.EmissionEnergyMultiplier = rng.RandfRange(3f, 6f);
                        matE.ShadingMode              = BaseMaterial3D.ShadingModeEnum.Unshaded;
                        etincelle.SetSurfaceOverrideMaterial(0, matE);

                        hote.GetTree().Root.AddChild(etincelle);
                        etincelle.GlobalPosition = position;

                        // Direction aléatoire en cone autour du point d'impact
                        Vector3 dispersion = new Vector3(
                                rng.RandfRange(-1f, 1f),
                                rng.RandfRange( 0f, 1f),
                                rng.RandfRange(-1f, 1f)
                        ).Normalized() * rng.RandfRange(0.05f, 0.3f);

                        // Animation de déplacement via Tween
                        var tween = hote.GetTree().CreateTween();
                        float dureeEtincelle = rng.RandfRange(0.08f, 0.2f);
                        tween.TweenProperty(etincelle, "global_position",
                                position + dispersion, dureeEtincelle);

                        float delai = dureeEtincelle;
                        hote.GetTree().CreateTimer(delai).Timeout += () =>
                        {
                                if (GodotObject.IsInstanceValid(etincelle))
                                        etincelle.QueueFree();
                        };
                }

                // Flash blanc au point d'impact
                var flash = new MeshInstance3D();
                var flashMesh = new SphereMesh();
                flashMesh.Radius = 0.12f;
                flashMesh.Height = 0.24f;
                flash.Mesh = flashMesh;

                var matF = new StandardMaterial3D();
                matF.AlbedoColor              = new Color(1f, 1f, 0.8f);
                matF.EmissionEnabled          = true;
                matF.Emission                 = new Color(1f, 1f, 0.8f);
                matF.EmissionEnergyMultiplier = 8f;
                matF.ShadingMode              = BaseMaterial3D.ShadingModeEnum.Unshaded;
                flash.SetSurfaceOverrideMaterial(0, matF);

                hote.GetTree().Root.AddChild(flash);
                flash.GlobalPosition = position;

                hote.GetTree().CreateTimer(0.05f).Timeout += () =>
                {
                        if (GodotObject.IsInstanceValid(flash))
                                flash.QueueFree();
                };
        }
}
