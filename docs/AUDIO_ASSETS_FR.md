# Audio / vidéos de thème — ce que le code fait (et ce que vous devez fournir)

## Musique

- Le jeu charge les BGM depuis `Assets/Resources/Audio/BGM/` (`quiz_theme`, `battle_theme`, …) et depuis `StreamingAssets/Theme/<mode>/`.
- **Si plusieurs fichiers ont le même contenu** (même ambiant renommé), vous entendrez **toujours le même son**. Ce n’est pas un bug du lecteur : il faut des **fichiers audio réellement différents** (téléchargés depuis les sites listés dans `FREE_THEME_MEDIA_SOURCES.md`).
- Vérification locale : `pwsh -File tools/Verify-BgmHashes.ps1` — affiche les doublons par hash.

Les liens « gratuits » donnés par Claude ne peuvent pas être téléchargés automatiquement dans votre dépôt depuis cet environnement : **vous devez** importer les pistes choisies dans Unity.

## Vidéo + 3D

- Par défaut, **`CongoMix3DWithVideo = 1`** : les **vidéos** sous `StreamingAssets/Theme/<mode>/` et le **plateau 3D** alternent (F9 pour régler).
- Le mix alterne aussi les variantes **`mode` / `mode_alt`** en 3D tant que les deux existent dans la liste interne.

Si vous ne voyez que la vidéo : désactiviez-vous l’alternance par le passé ; au prochain lancement sans clé enregistrée, le défaut est maintenant **alternance activée**.
