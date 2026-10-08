using Godot;

public static class ImpactDecal
{
    public static void Creer(Node hote, Vector3 position, Vector3 normale)
    {
        var nœudImpact = new Node3D();
        hote.GetTree().Root.AddChild(nœudImpact);
        nœudImpact.GlobalPosition = position;

        if (normale != Vector3.Zero && normale != Vector3.Up)
        {
            nœudImpact.LookAt(position + normale, Vector3.Up);
        }

        // Création du Decal Godot 3D
        var decal = new Decal();
        decal.Size = new Vector3(0.12f, 0.12f, 0.12f);
        nœudImpact.AddChild(decal);

        // Particules d'éclats à l'impact
        var particules = new GpuParticles3D();
        var matParticle = new ParticleProcessMaterial
        {
            Direction = Vector3.Back, // Remplace Vector3.ModelZ
            Spread = 45.0f,
            InitialVelocityMin = 2.0f,
            InitialVelocityMax = 5.0f,
            Gravity = new Vector3(0, -9.8f, 0),
            ScaleMin = 0.02f,
            ScaleMax = 0.06f
        };

        particules.ProcessMaterial = matParticle;
        particules.Amount = 10;
        particules.Lifetime = 0.25f;
        particules.OneShot = true;
        particules.Explosiveness = 0.9f;
        
        nœudImpact.AddChild(particules);
        particules.Emitting = true;

        var timer = hote.GetTree().CreateTimer(8.0f);
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(nœudImpact))
                nœudImpact.QueueFree();
        };
    }
}
