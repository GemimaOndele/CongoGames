# Workflow local (après `git pull`)

Ordre recommandé pour tester la boucle complète sur **Windows** (`C:\Congogame`) ou clone Unix.

1. **`git pull origin master`** — aligne les scripts, banques et (si versionnés) les pistes `Theme/blind-test/`.
2. **Backend** : à la racine du dépôt, `npm run start-all` (ou `.\start-all.ps1`). Laisse ce terminal ouvert.
3. **Unity** : ouvre `UnityProject` avec la version indiquée dans `UnityProject/ProjectSettings/ProjectVersion.txt`, puis **Play** (scène vide OK si `RuntimeBootstrap` est actif).

## Audio (playlist blind / ambiance)

- Dossier lu en priorité : `UnityProject/Assets/StreamingAssets/Theme/blind-test/` (`track01.ogg`, `track02.ogg`, …).
- **Crédits** : `StreamingAssets/Theme/ATTRIBUTION.md` (Commons CC BY-SA).
- Si tu n’as pas les binaires après un clone : télécharge depuis les pages Commons liées dans `ATTRIBUTION.md` (bouton *Original file*) ou relance le pull une fois le dépôt mis à jour.

## Images (devine l’image)

- Répertoire : `UnityProject/Assets/StreamingAssets/Theme/ImageGuess/`
- Noms et indices : `ImageGuess/NOMS_FICHIERS.txt`
- Fichier optionnel testé en jeu : ex. `kintele.png` (stade) si tu l’ajoutes toi-même (licence vérifiée).

## Rappel 3D « type PS5 »

- Pas dans ce dépôt seul : scènes, modèles, éclairage URP, prefabs (voir `docs/AAA_Blockbuster_Specification_CongoGames.md` et `ROADMAP_UI_3D.md`).

## Datasets (images / audio)

- Guide : **`Datasets/README.md`** — politique : **`Datasets/CONTENT_POLICY.md`** (pas YouTube ; Commons / licences).
- Rédaction IA (QCM, pas scraping) : **`Datasets/PROMPTS_IA_REDACTION.md`**.
- Récupération d’exemples Commons (local, manifeste JSON) :

```bash
npm run dataset:commons -- --query "Brazzaville" --limit 3
```

Les fichiers vont dans `Datasets/harvest/staging/` (ignoré par git par défaut ; copie manuelle vers `StreamingAssets` si tu veux les versionner avec attributions).
