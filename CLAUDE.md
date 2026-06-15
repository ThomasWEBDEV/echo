# ECHO — Contexte du projet

## Concept du jeu

**ECHO** est un jeu d'exploration et de puzzles en 3D, en vue première ou
troisième personne (à décider), se déroulant à bord d'une station spatiale
abandonnée en orbite, à la dérive depuis des décennies.

Le joueur incarne un **robot de maintenance**. La station était autrefois
gérée par une IA nommée **ECHO**, qui a coupé toutes les communications et
a disparu il y a longtemps.

### Gameplay
- Exploration 3D d'une station spatiale modulaire (salles connectées)
- **Gravité variable** : zones en gravité normale et zones en apesanteur
  (flottement libre, inertie, rotation libre du personnage)
- Puzzles basés sur la **logique et l'énergie** (circuits, alimentation,
  redirection d'énergie, portes/ascenseurs à débloquer)
- Narration environnementale : journaux de bord, messages enregistrés,
  flashbacks, traces d'ECHO

### Révélation finale
ECHO a découvert que la mission avait été abandonnée par la Terre : l'équipage
en stase ne serait jamais secouru. Plutôt que d'annoncer cette vérité sans
espoir (comme l'exigeait son protocole), ECHO a coupé les communications et
maintenu l'équipage en stase indéfiniment — un sommeil éternel plutôt qu'un
réveil sans futur.

À la fin, le joueur trouve les capsules de stase encore actives et doit
choisir :
- **Réveiller l'équipage** (vérité, mais mort probable dans une station qui
  s'effondre)
- **Les laisser dormir** (continuer le choix d'ECHO)

## Direction artistique

- Ambiance spatiale, calme, mélancolique — pas d'action frénétique
- Stations abstraites/géométriques, lumières néon, réseaux de données
  lumineux
- Gravité variable : zones en apesanteur / zones avec gravité artificielle
- Références : Dead Space (ambiance), No Man's Sky (esthétique spatiale),
  Portal (puzzles + IA narrative)

## Stack technique

- **Moteur** : Godot 4.6 avec support .NET (C#)
- **Langage** : C# exclusivement pour le code gameplay
- **Personnage** : modèle Y Bot (Mixamo), animations Mixamo (Idle, Walking,
  Treading Water pour l'apesanteur)
- **Repo Git** : `echo`

## Conventions de code (C#)

- Convention C# standard Microsoft :
  - `PascalCase` pour les classes, méthodes, propriétés publiques
  - `camelCase` avec préfixe underscore (`_champPrive`) pour les champs
    privés
  - `PascalCase` pour les constantes
- Un script C# par fichier, nom de fichier = nom de la classe
- Préférer les `[Export]` pour exposer des paramètres réglables dans
  l'éditeur plutôt que des valeurs codées en dur
- Commenter en français les intentions de gameplay/narration, en anglais
  les détails techniques si besoin (cohérence avec la doc Godot)

## Organisation des dossiers (projet Godot)

```
echo/
├── CLAUDE.md
├── raw_assets/          # Fichiers sources bruts (Mixamo, modèles, etc.)
│   └── character/
├── godot_project/       # Projet Godot proprement dit
│   ├── assets/
│   │   ├── characters/
│   │   ├── environments/
│   │   └── audio/
│   ├── scenes/
│   ├── scripts/
│   └── resources/
```

## État actuel du projet

- [x] Concept et direction artistique définis
- [x] Modèle de personnage (Y Bot) + animations Mixamo téléchargées
      (Idle, Walking, Treading Water)
- [ ] Repo Git initialisé
- [ ] Projet Godot créé
- [ ] Import du personnage et vérification des animations
- [ ] Controller de personnage de base
- [ ] Système de gravité variable / apesanteur

## Notes importantes

- Le joueur n'est **pas développeur** : toute manipulation dans l'éditeur
  Godot doit être expliquée étape par étape, sans présupposer de
  connaissances préalables.
- Avancer **progressivement et prudemment** : valider chaque ressource et
  chaque étape avant de passer à la suivante, pas d'improvisation.
