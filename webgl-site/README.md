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

## Vercel et compression (`.gz`)

Le CDN Vercel **applique mal** les en-têtes `Content-Encoding: gzip` sur des fichiers **déjà** compressés par Unity (`*.js.gz`, etc.), ce qui casse le chargement WebGL. Dans ce dépôt, **Player Settings → WebGL → Publishing Settings → Compression format** est réglé sur **Disabled** (`webGLCompressionFormat: 0`) : le build sort des `.js` / `.wasm` / `.data` **non** suffixés `.gz`, et Vercel les compresse correctement en transit. Après changement de ce réglage : **refaire un build WebGL**, recopier avec `copy-into-webgl-site.ps1`, puis `npm run webgl:vercel`.

Les règles `vercel.json` pour `*.gz` / `*.br` restent utiles si tu réactives Gzip/Brotli et héberges ailleurs (Nginx, S3, etc.).

## Fichiers versionnés ici

- `vercel.json` — `Content-Type` et, si besoin, `Content-Encoding` pour builds compressés hors Vercel.
- `copy-into-webgl-site.ps1` — script de copie.
- `README.md` — ce texte.

Les **fichiers du build** (binaires) ne sont en général **pas** commiter : ajoute-les seulement si tu veux un dépôt autonome (attention à la taille et à Git LFS).

Voir aussi : `docs/WEBGL_LIEN_NAVIGATEUR.md`.
