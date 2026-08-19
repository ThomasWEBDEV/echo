using Godot;

/// <summary>
/// Nœud de gestion de la session d'entraînement.
/// À attacher à la scène salle_entrainement. Démarre le timer au lancement,
/// affiche les stats dans la console et dans un Label HUD optionnel.
/// </summary>
public partial class SessionStats : Node
{
	// Intervalle en secondes entre deux affichages de stats (0 = désactivé)
	[Export] public float IntervallePrint = 30f;
	// Chemin vers un Label dans le HUD pour afficher le timer en jeu (optionnel)
	[Export] public NodePath CheminLabelTimer;

	private float _timerPrint  = 0f;
	private float _timerLabel  = 0f;
	private Label _labelTimer;

	public override void _Ready()
	{
		ScoreManager.Reinitialiser();
		GD.Print("[SESSION] Entraînement démarré. Bonne chance !");
		GD.Print("[SESSION] Commandes : F = bascule TPS/FPS | R = recharger | Échap = libérer souris");

		// Récupère le Label HUD si un chemin est fourni dans l'inspecteur
		if (CheminLabelTimer != null && !CheminLabelTimer.IsEmpty)
			_labelTimer = GetNodeOrNull<Label>(CheminLabelTimer);
	}

	public override void _Process(double delta)
	{
		ScoreManager.AjouterTemps((float)delta);

		// Mise à jour du Label HUD toutes les secondes (si présent)
		if (_labelTimer != null)
		{
			_timerLabel += (float)delta;
			if (_timerLabel >= 1f)
			{
				_timerLabel = 0f;
				float t   = ScoreManager.TempsSession;
				int   min = (int)(t / 60);
				int   sec = (int)(t % 60);
				_labelTimer.Text = $"{min:00}:{sec:00}  |  {ScoreManager.CiblesDetruites} cibles  |  {ScoreManager.Precision:F0}%";
			}
		}

		if (IntervallePrint <= 0f) return;

		_timerPrint += (float)delta;
		if (_timerPrint >= IntervallePrint)
		{
			_timerPrint = 0f;
			_PrintStats();
		}
	}

	// Affiche les stats de session dans la console Godot
	private void _PrintStats()
	{
		float t   = ScoreManager.TempsSession;
		int   min = (int)(t / 60);
		int   sec = (int)(t % 60);

		GD.Print($"[SESSION] {min:00}:{sec:00} | " +
		         $"Cibles : {ScoreManager.CiblesDetruites} | " +
		         $"Tirs : {ScoreManager.TirsTires} | " +
		         $"Précision : {ScoreManager.Precision:F1}%");
	}
}
