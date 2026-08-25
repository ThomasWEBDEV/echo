using Godot;

/// <summary>
/// Utilitaire de rendu — affiche un trait lumineux temporaire entre deux points
/// pour visualiser la trajectoire d'un tir (feedback joueur essentiel).
/// </summary>
public static class TraceurTir
{
	// Affiche un rayon lumineux de debut à fin pendant duree secondes
	public static void Afficher(Node hote, Vector3 debut, Vector3 fin, float duree = 0.06f)
	{
		float longueur = debut.DistanceTo(fin);
		if (longueur < 0.01f) return;

		var mesh  = new MeshInstance3D();
		var boite = new BoxMesh();
		// La boite s'étend le long de l'axe Z local : LookAt orientera -Z vers fin
		boite.Size = new Vector3(0.015f, 0.015f, longueur);
		mesh.Mesh  = boite;

		var mat = new StandardMaterial3D();
		mat.AlbedoColor              = new Color(1f, 0.85f, 0.3f);
		mat.EmissionEnabled          = true;
		mat.Emission                 = new Color(1f, 0.7f, 0.0f);
		mat.EmissionEnergyMultiplier = 4f;
		mat.ShadingMode              = BaseMaterial3D.ShadingModeEnum.Unshaded;
		mesh.SetSurfaceOverrideMaterial(0, mat);

		// Ajouter à la racine de scène pour que le tracer ne suive pas le joueur
		hote.GetTree().Root.AddChild(mesh);

		// Centrer entre debut et fin, orienter -Z vers fin
		mesh.GlobalPosition = (debut + fin) * 0.5f;
		Vector3 dir  = (fin - debut).Normalized();
		Vector3 haut = Mathf.Abs(dir.Dot(Vector3.Up)) < 0.9f ? Vector3.Up : Vector3.Right;
		mesh.LookAt(fin, haut);

		hote.GetTree().CreateTimer(duree).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(mesh))
				mesh.QueueFree();
		};
	}
}
