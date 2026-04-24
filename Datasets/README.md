# Datasets — images, audio, questions (Congo)

## Organisation recommandée

```text
Datasets/
  CONTENT_POLICY.md     # droits, TikTok, GitHub, pas YouTube
  README.md            # ce fichier
  schemas/             # JSON Schema (validation optionnelle)
  content/
    examples/          # échantillons versionnables (petits JSON)
  harvest/
    staging/          # généré par le script, non versionné (téléchargements)
```

Les **binaires lourds** (packs d’images / musiques) : préférer **Git LFS** ou hébergeur externe + URL dans JSON, selon la taille du dépôt.

## Lier au projet Unity

- **Images (devine l’image)** : copie les images renommées vers  
  `UnityProject/Assets/StreamingAssets/Theme/ImageGuess/`  
  (voir `Theme/ImageGuess/NOMS_FICHIERS.txt` et la banque C# `MiniGameDemoBanks`).
- **Blind test** : pistes dans  
  `UnityProject/Assets/StreamingAssets/Theme/BlindTest/` ou `Theme/blind-test/`  
  + entrées JSON ou banque C# alignées sur les **mêmes noms de fichiers**.
- **Attribution** : compléter `UnityProject/Assets/StreamingAssets/Theme/ATTRIBUTION.md` ou un fichier `Datasets/ATTRIBUTION.congo.json`.

## Remplir l’échantillon (workflow)

1. Lire `CONTENT_POLICY.md`.
2. Partir de `content/examples/*.json` ; dupliquer en `content/production/` (dossier optionnel, gitignore possible).
3. Utiliser le script (voir section Scripts) **ou** imports manuels depuis Commons.
4. Faire relire / valider des questions **langues de chants** par une personne fiable (pas l’oreille d’un script seul).

## Scripts (racine du dépôt)

```bash
node scripts/fetch-commons-assets.mjs --query "Brazzaville" --limit 3
```

Télécharge vers `Datasets/harvest/staging/` + écrit un manifeste JSON (URL, auteur, licence). **Ne pas** committer le dossier `staging/` s’il gonfle trop.

## Liens utiles (sourcing)

- Moteur de recherche Commons : <https://commons.wikimedia.org>
- Aide API : <https://commons.wikimedia.org/wiki/Commons:API>
- Openverse (images/audio licences ouvertes) : <https://openverse.org>
- Free Music Archive (vérifier la licence par piste) : <https://freemusicarchive.org>
