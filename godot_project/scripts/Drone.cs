using Godot;

/// <summary>
/// NPC drone ennemi basique pour la salle d'entraînement.
/// Patrouille entre deux points (PointA et PointB) et peut recevoir des dégâts.
/// Lorsque ses PV tombent à zéro, il se désintègre et enregistre une destruction.
/// </summary>
public partial class Drone : CharacterBody3D
{
	// Points de patrouille — à régler dans l'inspecteur ou via la scène
	[Export] public Vector3 PointA = new Vector3(-5f, 1.5f, 0f);
	[Export] public Vector3 PointB = new Vector3( 5f, 1.5f, 0f);
	// Paramètres de mouvement et de vie
	[Export] public float Vitesse         = 3.0f;
	[Export] public float PointsDeVie     = 50f;
	[Export] public float DegatsParTir    = 25f;
	[Export] public float DelaiDestruction = 1.5f;

	private float _pvActuels;
	private bool  _detruit = false;
	private bool  _versB   = true;  // direction courante : true = vers B, false = vers A

	private MeshInstance3D     _mesh;
	private StandardMaterial3D _materiau;

	// Couleurs de vie : vert (plein) → rouge (critique)
	private static readonly Color CouleurVie  = new Color(0.1f, 0.8f, 0.9f);
	private static readonly Color CouleurMort = new Color(0.9f, 0.1f, 0.1f);

	public override void _Ready()
	{
		_pvActuels = PointsDeVie;
		_mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");

		if (_mesh != null)
		{
			_materiau = new StandardMaterial3D();
			_materiau.AlbedoColor = CouleurVie;
			_materiau.EmissionEnabled = true;
			_materiau.Emission = new Color(0.05f, 0.4f, 0.5f);
			_materiau.EmissionEnergyMultiplier = 0.5f;
			_mesh.SetSurfaceOverrideMaterial(0, _materiau);
		}

		// Démarrer à la position du point A
		GlobalPosition = GlobalPosition with { Y = PointA.Y };
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_detruit) return;

		// Cible courante de patrouille
		Vector3 cible  = _versB ? PointB : PointA;
		Vector3 vers   = (cible - GlobalPosition);
		float   dist   = vers.Length();

		if (dist < 0.15f)
		{
			// Atteint la cible : faire demi-tour
			_versB = !_versB;
		}
		else
		{
			Velocity = vers.Normalized() * Vitesse;
			LookAt(GlobalPosition + new Vector3(vers.X, 0f, vers.Z).Normalized(), Vector3.Up);
		}

		MoveAndSlide();
	}

	// Appelé par Personnage._Tirer() si le raycast touche ce nœud
	public void TakeHit(float degats = -1f)
	{
		if (_detruit) return;

		float d = degats < 0 ? DegatsParTir : degats;
		_pvActuels -= d;
		GD.Print($"[DRONE] {Name} : {_pvActuels}/{PointsDeVie} PV");

		// Flash rouge à l'impact
		if (_materiau != null)
		{
			_materiau.AlbedoColor = new Color(1f, 0.1f, 0.05f);
			GetTree().CreateTimer(0.12).Timeout += () =>
			{
				if (IsInstanceValid(this) && !_detruit)
					_AppliquerCouleurVie();
			};
		}

		if (_pvActuels <= 0)
			_Detruire();
	}

	// Couleur selon les PV restants (bleu cyan → rouge)
	private void _AppliquerCouleurVie()
	{
		if (_materiau == null) return;
		float t = Mathf.Clamp(_pvActuels / PointsDeVie, 0f, 1f);
		_materiau.AlbedoColor = CouleurMort.Lerp(CouleurVie, t);
	}

	private void _Detruire()
	{
		_detruit = true;
		ScoreManager.EnregistrerDestruction();
		GD.Print($"[DRONE] {Name} détruit ! (score : {ScoreManager.CiblesDetruites})");

		// Désactiver la collision (différé pour éviter les erreurs physiques)
		var col = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
		if (col != null)
			col.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);

		// Assombrir le drone
		if (_materiau != null)
		{
			_materiau.AlbedoColor = new Color(0.15f, 0.15f, 0.15f);
			_materiau.EmissionEnergyMultiplier = 0f;
		}

		// Supprimer le nœud après un bref délai
		GetTree().CreateTimer(DelaiDestruction).Timeout += () =>
		{
			if (IsInstanceValid(this))
				QueueFree();
		};
	}
}
