# Déploiement WebGL (lien de jeu)

Ce dossier sert de **cible de déploiement statique** (Vercel, Netlify, etc.) : on y copie le **dossier entier** produit par Unity (`index.html`, `Build/`, `TemplateData/`, etc.).

## Étapes

1. Dans Unity : **File → Build Settings…** → **WebGL** → **Build** (choisir un dossier temporaire, ex. `C:\temp\CongoWebGL`).

2. Copier le contenu du build dans ce répertoire `webgl-site/` (à la racine : `index.html` + `Build` + `TemplateData`).

   **Important** : `-SourcePath` doit être le **vrai** chemin du dossier choisi à l’étape 1, pas un exemple du style `C:\Builds\CongoWebGL` s’il n’existe pas. Si le script refusera le chemin, crée d’abord le dossier dans l’Explorateur, build Unity **dedans**, puis recopie.

   Sous Windows (PowerShell, depuis la racine du dépôt) :

   ```powershell
   .\webgl-site\copy-into-webgl-site.ps1 -SourcePath "C:\LeCheminExactOùUnityAÉcritLeBuild"
   ```

3. Déployer sur Vercel (CLI installée) :

   ```bash
   cd webgl-site
   npx vercel --prod
   ```

   Ou depuis la racine du dépôt : `npm run webgl:vercel`

4. Ouvrir l’URL HTTPS affichée (téléphone, tablette, PC). Le build WebGL utilise `Resources/CloudEndpoints.json` (Vercel + Railway) pour TTS / WebSocket.

## Fichiers versionnés ici

- `vercel.json` — en-têtes `Content-Type` pour `.wasm` / `.js` (souvent requis).
- `copy-into-webgl-site.ps1` — script de copie.
- `README.md` — ce texte.

Les **fichiers du build** (binaires) ne sont en général **pas** commiter : ajoute-les seulement si tu veux un dépôt autonome (attention à la taille et à Git LFS).

Voir aussi : `docs/WEBGL_LIEN_NAVIGATEUR.md`.
