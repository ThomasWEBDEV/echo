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

	// Dégradé de couleur selon les PV : jaune → orange → rouge
	private static readonly Color CouleurMaxPV = new Color(0.9f, 0.70f, 0.05f);
	private static readonly Color CouleurMiPV  = new Color(0.95f, 0.35f, 0.02f);
	private static readonly Color CouleurMinPV = new Color(0.85f, 0.08f, 0.02f);

	public override void _Ready()
	{
		_pvActuels = PointsDeVie;
		_mesh = GetNode<MeshInstance3D>("MeshInstance3D");

		// Matériau propre par instance pour changer la couleur indépendamment
		_materiau = new StandardMaterial3D();
		_materiau.Metallic = 0.3f;
		_materiau.Roughness = 0.55f;
		_materiau.EmissionEnabled = true;
		_mesh.SetSurfaceOverrideMaterial(0, _materiau);

		_AppliquerCouleurRepos();
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
				_AppliquerCouleurRepos();
		};

		if (_pvActuels <= 0)
			_Detruire();
	}

	// Couleur de repos selon les PV restants : jaune (plein) → orange (50%) → rouge (vide)
	private void _AppliquerCouleurRepos()
	{
		float ratio = Mathf.Clamp(_pvActuels / PointsDeVie, 0f, 1f);
		Color couleur;
		Color emissive;

		if (ratio > 0.5f)
		{
			// Jaune → Orange (100% à 50%)
			float t = (ratio - 0.5f) * 2f;
			couleur  = CouleurMiPV.Lerp(CouleurMaxPV, t);
			emissive = new Color(0.6f, 0.25f * t, 0f);
		}
		else
		{
			// Orange → Rouge (50% à 0%)
			float t = ratio * 2f;
			couleur  = CouleurMinPV.Lerp(CouleurMiPV, t);
			emissive = new Color(0.7f, 0.05f * t, 0f);
		}

		_materiau.AlbedoColor = couleur;
		_materiau.Emission = emissive;
		_materiau.EmissionEnergyMultiplier = 0.3f + (1f - ratio) * 0.4f;
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
