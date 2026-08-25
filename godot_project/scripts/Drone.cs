using Godot;

/// <summary>
/// NPC drone ennemi pour la salle d'entraînement.
/// Patrouille entre deux points, détecte le joueur à portée et le poursuit.
/// Peut recevoir des dégâts et se détruire (enregistre une destruction dans ScoreManager).
/// </summary>
public partial class Drone : CharacterBody3D
{
	// Points de patrouille — à régler dans l'inspecteur
	[Export] public Vector3 PointA = new Vector3(-5f, 1.5f, 0f);
	[Export] public Vector3 PointB = new Vector3( 5f, 1.5f, 0f);
	// Paramètres de déplacement et de vie
	[Export] public float Vitesse          = 3.0f;
	[Export] public float PointsDeVie      = 50f;
	[Export] public float DegatsParTir     = 25f;
	[Export] public float DelaiDestruction = 1.5f;
	// Portées de détection du joueur
	[Export] public float PorteeDetection  = 12f;
	[Export] public float PorteePerte      = 18f;

	private float  _pvActuels;
	private bool   _detruit     = false;
	private bool   _versB       = true;
	private bool   _enPoursuite = false;
	private Node3D _joueur      = null;

	private MeshInstance3D     _mesh;
	private StandardMaterial3D _materiau;

	private static readonly Color CouleurVie      = new Color(0.1f, 0.8f, 0.9f);
	private static readonly Color CouleurMort     = new Color(0.9f, 0.1f, 0.1f);
	private static readonly Color EmissionNormale = new Color(0.05f, 0.40f, 0.50f);
	private static readonly Color EmissionAlerte  = new Color(0.80f, 0.30f, 0.00f);

	public override void _Ready()
	{
		_pvActuels = PointsDeVie;
		_mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");

		if (_mesh != null)
		{
			_materiau = new StandardMaterial3D();
			_materiau.AlbedoColor            = CouleurVie;
			_materiau.EmissionEnabled        = true;
			_materiau.Emission               = EmissionNormale;
			_materiau.EmissionEnergyMultiplier = 0.5f;
			_mesh.SetSurfaceOverrideMaterial(0, _materiau);
		}

		GlobalPosition = GlobalPosition with { Y = PointA.Y };

		// Récupérer le joueur via le groupe "joueur" (ajouté dans Personnage._Ready)
		var joueurs = GetTree().GetNodesInGroup("joueur");
		if (joueurs.Count > 0)
			_joueur = joueurs[0] as Node3D;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_detruit) return;

		_MettreAJourDetection();

		if (_enPoursuite && _joueur != null)
			_Poursuivre();
		else
			_Patrouiller();

		MoveAndSlide();
	}

	// Gère les transitions entre patrouille et poursuite selon la distance au joueur
	private void _MettreAJourDetection()
	{
		if (_joueur == null || !GodotObject.IsInstanceValid(_joueur)) return;

		float dist = GlobalPosition.DistanceTo(_joueur.GlobalPosition);

		if (!_enPoursuite && dist < PorteeDetection)
		{
			_enPoursuite = true;
			GD.Print($"[DRONE] {Name} : joueur détecté à {dist:F1}m — passage en poursuite !");
			if (_materiau != null)
				_materiau.Emission = EmissionAlerte;
		}
		else if (_enPoursuite && dist > PorteePerte)
		{
			_enPoursuite = false;
			GD.Print($"[DRONE] {Name} : joueur perdu — retour en patrouille.");
			if (_materiau != null)
				_materiau.Emission = EmissionNormale;
		}
	}

	private void _Patrouiller()
	{
		Vector3 cible = _versB ? PointB : PointA;
		Vector3 vers  = cible - GlobalPosition;

		if (vers.Length() < 0.15f)
		{
			_versB = !_versB;
		}
		else
		{
			Velocity = vers.Normalized() * Vitesse;
			LookAt(GlobalPosition + new Vector3(vers.X, 0f, vers.Z).Normalized(), Vector3.Up);
		}
	}

	private void _Poursuivre()
	{
		Vector3 vers = _joueur.GlobalPosition - GlobalPosition;
		Velocity = vers.Normalized() * Vitesse * 1.5f;

		Vector3 versH = new Vector3(vers.X, 0f, vers.Z);
		if (versH.LengthSquared() > 0.01f)
			LookAt(GlobalPosition + versH.Normalized(), Vector3.Up);
	}

	// Appelé par Personnage._Tirer() si le raycast touche ce nœud
	public void TakeHit(float degats = -1f)
	{
		if (_detruit) return;

		float d = degats < 0 ? DegatsParTir : degats;
		_pvActuels -= d;
		GD.Print($"[DRONE] {Name} : {_pvActuels}/{PointsDeVie} PV");

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

	// Dégradé bleu cyan (plein PV) → rouge (critique)
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

		var col = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
		if (col != null)
			col.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);

		if (_materiau != null)
		{
			_materiau.AlbedoColor              = new Color(0.15f, 0.15f, 0.15f);
			_materiau.EmissionEnergyMultiplier  = 0f;
		}

		GetTree().CreateTimer(DelaiDestruction).Timeout += () =>
		{
			if (IsInstanceValid(this))
				QueueFree();
		};
	}
}
