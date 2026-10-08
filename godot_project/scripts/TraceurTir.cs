using Godot;

public static class TraceurTir
{
    public static void Afficher(Node parent, Vector3 depart, Vector3 arrivee)
    {
        var meshInstance = new MeshInstance3D();
        var immediateMesh = new ImmediateMesh();
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1.0f, 0.85f, 0.4f, 0.8f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };

        immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
        immediateMesh.SurfaceAddVertex(depart);
        immediateMesh.SurfaceAddVertex(arrivee);
        immediateMesh.SurfaceEnd();

        meshInstance.Mesh = immediateMesh;
        parent.GetTree().Root.AddChild(meshInstance);

        // Flash d'éclairage au départ du tir (Muzzle Flash)
        var lumiereFlash = new OmniLight3D
        {
            GlobalPosition = depart,
            LightColor = new Color(1.0f, 0.75f, 0.3f),
            LightEnergy = 4.0f,
            OmniRange = 5.0f
        };
        parent.GetTree().Root.AddChild(lumiereFlash);

        var timer = parent.GetTree().CreateTimer(0.04f);
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(meshInstance)) meshInstance.QueueFree();
            if (GodotObject.IsInstanceValid(lumiereFlash)) lumiereFlash.QueueFree();
        };
    }
}
