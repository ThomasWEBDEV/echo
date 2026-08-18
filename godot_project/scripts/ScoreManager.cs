/// <summary>
/// Stats de session d'entraînement — classe statique accessible depuis n'importe quel script.
/// Suivre les tirs tirés, les cibles détruites et le temps écoulé.
/// </summary>
public static class ScoreManager
{
	public static int   CiblesDetruites { get; private set; } = 0;
	public static int   TirsTires       { get; private set; } = 0;
	public static float TempsSession    { get; private set; } = 0f;

	// Précision en pourcentage (0 si aucun tir effectué)
	public static float Precision =>
		TirsTires > 0 ? (float)CiblesDetruites / TirsTires * 100f : 0f;

	public static void EnregistrerTir()         => TirsTires++;
	public static void EnregistrerDestruction() => CiblesDetruites++;
	public static void AjouterTemps(float dt)   => TempsSession += dt;

	public static void Reinitialiser()
	{
		CiblesDetruites = 0;
		TirsTires       = 0;
		TempsSession    = 0f;
	}
}
