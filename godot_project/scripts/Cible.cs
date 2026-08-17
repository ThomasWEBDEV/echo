using Godot;

/// <summary>Cible destructible pour la salle d'entraînement.</summary>
public partial class Cible : StaticBody3D
{
	// Points de vie et dégâts par tir — réglables dans l'inspecteur
	[Export] public float PointsDeVie = 100f;
	[Export] public float DegatsParTir = 25f;

	private MeshInstance3D _mesh;
	private StandardMaterial3D _materiau;
	private float _pvActuels;

	public override void _Ready()
	{
		_pvActuels = PointsDeVie;
		_mesh = GetNode<MeshInstance3D>("MeshInstance3D");

		// Matériau propre par instance pour changer la couleur à l'impact
		_materiau = new StandardMaterial3D();
		_materiau.AlbedoColor = new Color(0.9f, 0.70f, 0.05f);
		_materiau.Metallic = 0.3f;
		_materiau.Roughness = 0.55f;
		_materiau.EmissionEnabled = true;
		_materiau.Emission = new Color(0.6f, 0.4f, 0.0f);
		_materiau.EmissionEnergyMultiplier = 0.3f;
		_mesh.SetSurfaceOverrideMaterial(0, _materiau);
	}

	// Appelé par Personnage._Tirer() lors d'un impact
	public void TakeHit(float degats = -1f)
	{
		float d = degats < 0 ? DegatsParTir : degats;
		_pvActuels -= d;
		GD.Print($"[CIBLE] {Name} : {_pvActuels}/{PointsDeVie} PV");

		// Flash rouge à l'impact
		_materiau.AlbedoColor = new Color(1f, 0.1f, 0.05f);
		_materiau.Emission = new Color(1f, 0.0f, 0.0f);
		_materiau.EmissionEnergyMultiplier = 1.5f;

		GetTree().CreateTimer(0.15).Timeout += () =>
		{
			if (!IsInstanceValid(this)) return;
			if (_pvActuels > 0)
			{
				_materiau.AlbedoColor = new Color(0.9f, 0.70f, 0.05f);
				_materiau.Emission = new Color(0.6f, 0.4f, 0.0f);
				_materiau.EmissionEnergyMultiplier = 0.3f;
			}
		};

		if (_pvActuels <= 0)
			_Detruire();
	}

	private void _Detruire()
	{
		GD.Print($"[CIBLE] {Name} détruite !");

		// Désactiver la collision immédiatement (différé pour éviter les erreurs physiques)
		var col = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
		if (col != null)
			col.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);

		// Assombrir la cible puis la supprimer
		_materiau.AlbedoColor = new Color(0.25f, 0.25f, 0.25f);
		_materiau.EmissionEnergyMultiplier = 0f;

		GetTree().CreateTimer(0.3).Timeout += () =>
		{
			if (IsInstanceValid(this))
				QueueFree();
		};
	}
}
