# YouTube, streaming et poids disque (fonds d’écran / audio)

## Problème

Les liens **https://www.youtube.com/...** ou **https://youtu.be/...** pointent vers une **page applicative** (player + JS), pas vers un **fichier** `.mp4` / `.webm`. Unity `VideoPlayer` et `UnityWebRequest` audio ne peuvent pas « lire YouTube » comme un fichier direct.

Dans le dépôt public, on **ne** versionne **pas** de lourds clips vidéo : la mémoire disque et la bande passante restent modérées ; le runtime peut **lire en flux** des URLs **HTTPS directes** (fichier ou CDN), ou des fichiers **locaux** optionnels côté machine.

## Ce que le code fait

- `StreamingMediaUrlPolicy` : si un champ du JSON (`backgroundVideoUrl`, `musicUrl`, `bottomVideoUrl`, `audioUrl` du blind) contient YouTube, Spotify web, etc., l’URL est **ignorée** (avec log) et le jeu enchaîne sur le **fichier local** `StreamingAssets/Theme/...`, la **musique thème** suivante, ou le **fond 3D / synthé**.
- Les démos en ligne continuent d’utiliser des **fichiers publics** (ex. [MDN flower.mp4](https://interactive-examples.mdn.mozilla.net/media/cc0-videos/flower.mp4), [W3Schools mov_bbb](https://www.w3schools.com/html/mov_bbb.mp4)) déjà présents dans `remote_media.json`.

## Références vidéo (inspiration, hors runtime)

Ces liens servent de **bibliothèque d’inspiration** pour choisir des fonds ou une direction artistique — **ne pas** les coller tels quels dans `backgroundVideoUrl` :

- <https://youtu.be/HeRXfvCmarc>
- <https://youtu.be/c-O-CBMCBW0>
- <https://youtu.be/CDEUUO5d1nw>

## Stratégies sans gonfler le dépôt

1. **Streaming HTTPS (recommandé en prod)**  
   Héberger un **fichier** `.mp4` ou `.webm` (ou audio `.mp3`) sur un **CDN** ou un **hébergeur statique** (S3, Cloudflare R2, GitHub **Releases** asset, site perso) et placer l’**URL directe** dans `remote_media.json`. Aucun gros binaire dans Git ; seule une petite clé de config est versionnée.

2. **Fichier local en dev, ignoré par Git**  
   Télécharger une fois une vidéo (droits vérifiés) dans un dossier listé par `.gitignore` — voir `tools/fetch-youtube-theme.ps1` (facultatif, nécessite [yt-dlp](https://github.com/yt-dlp/yt-dlp)) vers `Theme/_dev_import/`. Le jeu accepte un chemin `StreamingAssets/Theme/<modeId>/background.mp4` ; voir [THEME_BACKGROUNDS.md](THEME_BACKGROUNDS.md).

3. **Réseau obligatoire**  
   Tant qu’on ne met que des URLs de démonstration publiques, le client doit être **en ligne** pour charger le fond. Hors-ligne : copier un clip dans `StreamingAssets/Theme/...` (local) ou s’en tenir au plateau 3D.

## Droit d’auteur

YouTube n’est **pas** une source de redistribution automatique. Pour un produit public, il faut **licences** adaptées, ou contenus **libres** (Commons, tournage propre) ou **streaming** hébergé sur **vos** serveurs avec accord.

## Données mini-jeu (trivia, images)

- `StreamingAssets/Datasets/minigame_blind_extras.json` — questions supplémentaires (fusion avec le C# de base).  
- `StreamingAssets/Datasets/minigame_image_guess_extras.json` — devinettes image (indices + `streamingFileBase` optionnel vers `Theme/ImageGuess/`).  

Ces JSON sont **texte** : peu de poids, versionnables sur Git.
