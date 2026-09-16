using Godot;

/// <summary>
/// NPC drone ennemi pour la salle d'entraînement.
/// Patrouille entre deux points, détecte le joueur à portée et le poursuit.
/// Difficulté progressive : vitesse et cadence augmentent à chaque respawn.
/// Phase d'alerte avant poursuite + tracer de tir visuel.
/// </summary>
public partial class Drone : CharacterBody3D
{
        [Export] public Vector3 PointA = new Vector3(-5f, 1.5f, 0f);
        [Export] public Vector3 PointB = new Vector3( 5f, 1.5f, 0f);
        [Export] public float Vitesse          = 3.0f;
        [Export] public float PointsDeVie      = 50f;
        [Export] public float DegatsParTir     = 25f;
        [Export] public float DelaiDestruction = 1.5f;
        [Export] public float PorteeDetection  = 12f;
        [Export] public float PorteePerte      = 18f;
        [Export] public float DegatsProjectile = 10f;
        [Export] public float CadenceTir       = 2.0f;
        [Export] public bool  AutoRespawn      = true;
        [Export] public float DelaiRespawn     = 8.0f;
        // Difficulté progressive
        [Export] public float BonusVitesseParRespawn   = 0.3f;
        [Export] public float BonusCadenceParRespawn   = 0.15f;
        [Export] public int   RespawnsMax              = 5;
        // Durée de la phase d'alerte avant poursuite
        [Export] public float DureeAlerte = 1.2f;

        private float  _pvActuels;
        private bool   _detruit      = false;
        private bool   _versB        = true;
        private bool   _enPoursuite  = false;
        private bool   _enAlerte     = false;
        private float  _timerAlerte  = 0f;
        private Node3D _joueur       = null;
        private float  _timerTir     = 0f;
        private int    _nbRespawns   = 0;
        private float  _vitesseActuelle;
        private float  _cadenceActuelle;
        private bool   _timerClignotement = false;

        private MeshInstance3D     _mesh;
        private StandardMaterial3D _materiau;

        private static readonly Color CouleurVie      = new Color(0.1f, 0.8f, 0.9f);
        private static readonly Color CouleurMort     = new Color(0.9f, 0.1f, 0.1f);
        private static readonly Color CouleurAlerte   = new Color(1.0f, 0.8f, 0.0f);
        private static readonly Color EmissionNormale = new Color(0.05f, 0.40f, 0.50f);
        private static readonly Color EmissionAlerte  = new Color(0.80f, 0.30f, 0.00f);

        public override void _Ready()
        {
                _pvActuels       = PointsDeVie;
                _vitesseActuelle = Vitesse;
                _cadenceActuelle = CadenceTir;

                _mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
                if (_mesh != null)
                {
                        _materiau = new StandardMaterial3D();
                        _materiau.AlbedoColor              = CouleurVie;
                        _materiau.EmissionEnabled          = true;
                        _materiau.Emission                 = EmissionNormale;
                        _materiau.EmissionEnergyMultiplier = 0.5f;
                        _mesh.SetSurfaceOverrideMaterial(0, _materiau);
                }

                GlobalPosition = GlobalPosition with { Y = PointA.Y };

                var joueurs = GetTree().GetNodesInGroup("joueur");
                if (joueurs.Count > 0)
                        _joueur = joueurs[0] as Node3D;
        }

        public override void _PhysicsProcess(double delta)
        {
                if (_detruit) return;

                _MettreAJourDetection(delta);

                if (_enPoursuite && _joueur != null)
                {
                        _Poursuivre();
                        _timerTir += (float)delta;
                        if (_timerTir >= _cadenceActuelle)
                        {
                                _timerTir = 0f;
                                _TirerSurJoueur();
                        }
                }
                else if (!_enAlerte)
                {
                        _timerTir = 0f;
                        _Patrouiller();
                }

                MoveAndSlide();
        }

        private void _MettreAJourDetection(double delta)
        {
                if (_joueur == null || !GodotObject.IsInstanceValid(_joueur)) return;

                float dist = GlobalPosition.DistanceTo(_joueur.GlobalPosition);

                // Phase d'alerte : joueur détecté mais pas encore en poursuite
                if (!_enPoursuite && !_enAlerte && dist < PorteeDetection && _LigneDeVueLibre())
                {
                        _enAlerte    = true;
                        _timerAlerte = 0f;
                        Velocity     = Vector3.Zero;
                        GD.Print($"[DRONE] {Name} : alerte ! joueur détecté à {dist:F1}m");
                        _DemarrerClignotement();
                }

                // Compte à rebours alerte → poursuite
                if (_enAlerte)
                {
                        _timerAlerte += (float)delta;
                        if (_timerAlerte >= DureeAlerte)
                        {
                                _enAlerte    = false;
                                _enPoursuite = true;
                                GD.Print($"[DRONE] {Name} : passage en poursuite !");
                                if (_materiau != null)
                                        _materiau.Emission = EmissionAlerte;
                        }
                }

                // Perte du joueur
                if (_enPoursuite && (dist > PorteePerte || !_LigneDeVueLibre()))
                {
                        _enPoursuite = false;
                        GD.Print($"[DRONE] {Name} : joueur perdu — retour en patrouille.");
                        if (_materiau != null)
                                _materiau.Emission = EmissionNormale;
                }
        }

        // Clignotement jaune pendant la phase d'alerte
        private async void _DemarrerClignotement()
        {
                if (_timerClignotement) return;
                _timerClignotement = true;

                int cycles = Mathf.CeilToInt(DureeAlerte / 0.2f);
                for (int i = 0; i < cycles && _enAlerte; i++)
                {
                        if (_materiau != null)
                                _materiau.AlbedoColor = (i % 2 == 0) ? CouleurAlerte : CouleurVie;
                        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
                }

                _timerClignotement = false;
                if (!_detruit && _materiau != null)
                        _AppliquerCouleurVie();
        }

        private bool _LigneDeVueLibre()
        {
                var espace = GetWorld3D().DirectSpaceState;
                var query  = PhysicsRayQueryParameters3D.Create(GlobalPosition, _joueur.GlobalPosition);
                query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
                var result = espace.IntersectRay(query);
                if (result.Count == 0) return false;
                return result["collider"].As<GodotObject>() is Personnage;
        }

        private void _Patrouiller()
        {
                Vector3 cible = _versB ? PointB : PointA;
                Vector3 vers  = cible - GlobalPosition;

                if (vers.Length() < 0.15f)
                {
                        _versB   = !_versB;
                        Velocity = Vector3.Zero;
                }
                else
                {
                        Velocity = vers.Normalized() * _vitesseActuelle;
                        LookAt(GlobalPosition + new Vector3(vers.X, 0f, vers.Z).Normalized(), Vector3.Up);
                }
        }

        private void _Poursuivre()
        {
                Vector3 vers = _joueur.GlobalPosition - GlobalPosition;
                Velocity = vers.Normalized() * _vitesseActuelle * 1.5f;

                Vector3 versH = new Vector3(vers.X, 0f, vers.Z);
                if (versH.LengthSquared() > 0.01f)
                        LookAt(GlobalPosition + versH.Normalized(), Vector3.Up);
        }

        private void _TirerSurJoueur()
        {
                if (_joueur == null || !GodotObject.IsInstanceValid(_joueur)) return;

                var espace = GetWorld3D().DirectSpaceState;
                var query  = PhysicsRayQueryParameters3D.Create(GlobalPosition, _joueur.GlobalPosition);
                query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
                var result = espace.IntersectRay(query);
                if (result.Count == 0) return;

                if (result["collider"].As<GodotObject>() is Personnage joueur)
                {
                        GD.Print($"[DRONE] {Name} touche le joueur — {DegatsProjectile} dégâts !");
                        joueur.TakeHit(DegatsProjectile);
                        // Tracer de tir visuel drone → joueur
                        TraceurTir.Afficher(this, GlobalPosition, _joueur.GlobalPosition);
                }
        }

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

        private void _AppliquerCouleurVie()
        {
                if (_materiau == null) return;
                float t = Mathf.Clamp(_pvActuels / PointsDeVie, 0f, 1f);
                _materiau.AlbedoColor = CouleurMort.Lerp(CouleurVie, t);
        }

        private void _Detruire()
        {
                _detruit = true;
                Velocity = Vector3.Zero;
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

                if (AutoRespawn && _nbRespawns < RespawnsMax)
                {
                        GD.Print($"[DRONE] {Name} réapparaît dans {DelaiRespawn}s... (respawn {_nbRespawns + 1}/{RespawnsMax})");
                        GetTree().CreateTimer(DelaiRespawn).Timeout += _Reinitialiser;
                }
                else
                {
                        GetTree().CreateTimer(DelaiDestruction).Timeout += () =>
                        {
                                if (IsInstanceValid(this)) QueueFree();
                        };
                }
        }

        private void _Reinitialiser()
        {
                if (!IsInstanceValid(this)) return;

                _nbRespawns++;
                // Difficulté progressive
                _vitesseActuelle = Vitesse  + BonusVitesseParRespawn  * _nbRespawns;
                _cadenceActuelle = Mathf.Max(0.5f, CadenceTir - BonusCadenceParRespawn * _nbRespawns);

                _pvActuels   = PointsDeVie;
                _detruit     = false;
                _enPoursuite = false;
                _enAlerte    = false;
                _timerTir    = 0f;
                GlobalPosition = PointA;

                var col = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
                if (col != null)
                        col.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);

                if (_materiau != null)
                {
                        _materiau.AlbedoColor              = new Color(1f, 1f, 1f);
                        _materiau.EmissionEnergyMultiplier  = 2.0f;
                        _materiau.Emission                 = EmissionNormale;

                        GetTree().CreateTimer(0.25).Timeout += () =>
                        {
                                if (IsInstanceValid(this) && !_detruit)
                                        _AppliquerCouleurVie();
                        };
                }

                GD.Print($"[DRONE] {Name} respawn {_nbRespawns} — vitesse {_vitesseActuelle:F1} | cadence {_cadenceActuelle:F1}s");
        }
}
