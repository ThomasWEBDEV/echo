using Godot;
using System;
using System.Threading;

/// <summary>
/// Gestionnaire de statistiques de session (Thread-safe et événementiel).
/// </summary>
public static class ScoreManager
{
    private static int _ciblesDetruites;
    private static int _tirsTires;

    public static int CiblesDetruites => _ciblesDetruites;
    public static int TirsTires => _tirsTires;
    public static float TempsSession { get; private set; }

    /// <summary>
    /// Précision en pourcentage (0–100%).
    /// </summary>
    public static float Precision => 
        _tirsTires > 0 ? (float)_ciblesDetruites / _tirsTires * 100f : 0f;

    // Événements pour mettre à jour l'UI de manière réactive
    public static event Action<int, float> OnScoreUpdated; // (cibles, précision)
    public static event Action OnStatsReset;

    public static void EnregistrerTir()
    {
        Interlocked.Increment(ref _tirsTires);
        OnScoreUpdated?.Invoke(_ciblesDetruites, Precision);
    }

    public static void EnregistrerDestruction()
    {
        Interlocked.Increment(ref _ciblesDetruites);
        OnScoreUpdated?.Invoke(_ciblesDetruites, Precision);
    }

    public static void AjouterTemps(double delta)
    {
        TempsSession += (float)delta;
    }

    public static void Reinitialiser()
    {
        Interlocked.Exchange(ref _ciblesDetruites, 0);
        Interlocked.Exchange(ref _tirsTires, 0);
        TempsSession = 0f;

        OnStatsReset?.Invoke();
        OnScoreUpdated?.Invoke(0, 0f);
    }
}
