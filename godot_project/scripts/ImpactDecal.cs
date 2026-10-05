using Godot;

/// <summary>
/// Utilitaire pour créer un impact de balle visuel (trou d'impact / décaleur simple) sur les surfaces sans assets.
/// </summary>
public static class ImpactDecal
{
    public static void Creer(Node hote, Vector3 position, Vector3 normale)
    {
        var decal = new MeshInstance3D();
        var quad = new QuadMesh();
        quad.Size = new Vector2(0.08f, 0.08f);
        decal.Mesh = quad;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.1f, 0.1f, 0.1f); // Noir/Gris fumé
        mat.Roughness = 0.9f;
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        decal.SetSurfaceOverrideMaterial(0, mat);

        hote.GetTree().Root.AddChild(decal);
        decal.GlobalPosition = position + normale * 0.005f; // Légèrement décollé pour éviter le z-fighting

        if (normale != Vector3.Zero)
        {
            decal.LookAt(position + normale, Vector3.Up);
        }

        // Disparition progressive après quelques secondes
        var timer = hote.GetTree().CreateTimer(5.0f);
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(decal))
                decal.QueueFree();
        };
    }
}
