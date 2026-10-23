using Godot;

public static class ImpactDecal
{
    public static void Creer(Node hote, Vector3 position, Vector3 normale)
    {
        Vector3 direction = normale == Vector3.Zero ? Vector3.Up : normale.Normalized();
        var nœudImpact = new Node3D();
        hote.GetTree().Root.AddChild(nœudImpact);
        nœudImpact.GlobalPosition = position + direction * 0.012f;
        nœudImpact.GlobalBasis = new Basis(new Quaternion(Vector3.Up, direction));

        var matériauAnneau = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.12f, 0.85f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.05f, 0.65f, 1.0f),
            EmissionEnergyMultiplier = 2.5f
        };

        var anneau = new MeshInstance3D
        {
            Name = "AnneauImpact",
            Mesh = new TorusMesh
            {
                InnerRadius = 0.059f,
                OuterRadius = 0.065f,
                Rings = 24,
                RingSegments = 6
            },
            Scale = Vector3.One * 0.15f
        };
        anneau.SetSurfaceOverrideMaterial(0, matériauAnneau);
        nœudImpact.AddChild(anneau);

        var matériauParticules = new ParticleProcessMaterial
        {
            Direction = Vector3.Up,
            Spread = 50.0f,
            InitialVelocityMin = 1.2f,
            InitialVelocityMax = 2.8f,
            Gravity = Vector3.Zero,
            ScaleMin = 0.025f,
            ScaleMax = 0.045f
        };

        var matériauÉtincelles = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1.0f, 0.55f, 0.12f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.3f, 0.04f),
            EmissionEnergyMultiplier = 2.0f
        };
        var meshÉtincelle = new SphereMesh
        {
            Radius = 0.025f,
            Height = 0.05f
        };
        meshÉtincelle.SurfaceSetMaterial(0, matériauÉtincelles);

        var particules = new GpuParticles3D();
        particules.ProcessMaterial = matériauParticules;
        particules.DrawPass1 = meshÉtincelle;
        particules.Amount = 8;
        particules.Lifetime = 0.35f;
        particules.OneShot = true;
        particules.Explosiveness = 1.0f;
        nœudImpact.AddChild(particules);
        particules.Emitting = true;

        hote.CreateTween()
            .TweenProperty(anneau, "scale", Vector3.One, 0.18f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        var timer = hote.GetTree().CreateTimer(1.2f);
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(nœudImpact))
                nœudImpact.QueueFree();
        };
    }
}
