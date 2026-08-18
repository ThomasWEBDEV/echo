using Godot;

/// <summary>
/// Nœud de gestion de la session d'entraînement.
/// À attacher à la scène salle_entrainement. Démarre le timer au lancement,
/// affiche les stats dans la console à intervalle régulier.
/// </summary>
public partial class SessionStats : Node
{
	// Intervalle en secondes entre deux affichages de stats (0 = désactivé)
	[Export] public float IntervallePrint = 30f;

	private float _timerPrint = 0f;

	public override void _Ready()
	{
		ScoreManager.Reinitialiser();
		GD.Print("[SESSION] Entraînement démarré. Bonne chance !");
		GD.Print("[SESSION] Commandes : F = bascule TPS/FPS | R = recharger | Échap = libérer souris");
	}

	public override void _Process(double delta)
	{
		ScoreManager.AjouterTemps((float)delta);

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
