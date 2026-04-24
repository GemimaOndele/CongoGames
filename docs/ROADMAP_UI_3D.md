# Après le MVP (interface « type PS5 / TikTok live »)

## Ce que le dépôt fournit aujourd’hui (socle technique)

**Exclusions explicites du rendu 3D « show TV » procédural :** pas de mesh d’artiste, pas de salon public 3D, pas d’avatar 3D — uniquement **primitives + lumières** pour poser une base technique et un look plateau TV.

- **HUD 2D** (texte, grilles, chrono, etc.) branché WebSocket + TTS ; le layout central / classement est pensé pour rester lisible en stream (voir `RuntimeBootstrap` + `MiniGamePanelContent`).
- **Fond « show TV » 3D procédural** : [`VirtualShowStage3D.cs`](../UnityProject/Assets/Scripts/Presentation/VirtualShowStage3D.cs) — plateau **URP** (primitives + lumières) → **RenderTexture** sur le `RawImage` de [`ThemeBackgroundController.cs`](../UnityProject/Assets/Scripts/Presentation/ThemeBackgroundController.cs) quand il n’y a **pas** de vidéo dans `StreamingAssets/Theme/...`.
- **Qualité / résolution RT** : [`PresentationConfig.cs`](../UnityProject/Assets/Scripts/Presentation/PresentationConfig.cs) (`CongoPresentationQuality`, `SceneRichness`, taille `VirtualStageWidth` / `Height`).
- **Pas** de promesse de « PS5 AAA complet » dans ce seul code : base **runtime** reconnaissable, **pilotable par `modeId`** (palette / décor), pour **itérer** ensuite.
- Option joueur : `PlayerPrefs` **`CongoUseVirtual3D`** (0 = repli sur `SyntheticVideoBackground` 2D) ; **F9** → case + **Appliquer affichage 3D** (reconstruit le fond sans quitter le Play).
- Compléments UI « console » : `Ps5HudParallax`, `Ps5ModeVisualRig`, `Ps5CanvasBackdropRig`.

## Ce qu’implique un rendu « gros budget PS5 » (hors scope du socle)

Un rendu **persos HD, ciné, assets dédiés** est un **saut de production**, pas un commit de scripts seuls :

| Besoin | Exemple |
|--------|--------|
| Contenu | Modèles **.fbx** (plateau, props, personnages si besoin), textures, direction artistique |
| Animation | **Animator**, **Timeline**, états de caméra |
| Effets | **VFX** (Shader Graph, particules), **post-process** URP (bloom, couleur, etc.) |
| Structure | **Scène `.unity` dédiée** « show TV », éventuellement **Render Texture** vers l’UI (ou l’inverse selon le flux choisi) |
| Méthode | Itérations **par-dessus** le socle actuel (`VirtualShowStage3D` peut être remplacé ou complété par une scène chargée additivement) |

## Piste réaliste par étapes

1. Conserver le flux actuel **Play** + backend ; améliorer les **prefabs** UI en gardant les mêmes scripts.
2. Variante avancée : scène Unity **show TV** + **Render Texture** ; aujourd’hui le code fait **3D → texture de fond** (l’inverse — HUD 3D dans une scène — est une variante d’archi).
3. **Audio** : mixer Unity, stingers déjà partiellement branchés.
4. **TikTok / live** : privilégier **lisibilité** (gros texte, contrastes) — les effets ne doivent pas masquer questions et scores.

## Scène Unity perso dans l’éditeur

Si tu as un objet de scène nommé **VirtualShowStage** (ou un sol coloré fait à la main), ce n’est **pas** automatiquement le même pipeline que **`ThemeBackgroundController` + `VirtualShowStage3D`** généré au runtime. Pour un rendu cohérent avec le jeu :

- soit tu **désactives** le décor perso et tu laisses le **bootstrap** / le `RawImage` du thème porter le rendu 3D procédural ;
- soit tu **importes** tes assets dans une scène dédiée et tu branches le flux **vidéo / RT** documenté ci-dessus (itération plutôt que doublon de deux « fonds »).

## Résumé

- **Inclus** : base 3D **procédurale** + **UI 2D** stable par-dessus, visée **show TV** lisible.
- **À produire à part** : assets AAA (.fbx, anim, VFX, post-process, scène) — **itération** au-dessus de ce socle, pas substitution magique par les seuls scripts.

## Documents liés

| Document | Rôle |
|----------|------|
| [3D_PRODUCTION_ITERATION.md](3D_PRODUCTION_ITERATION.md) | Checklist courte : passage du socle → itérations « vrai » 3D / post-process |
| [THEME_BACKGROUNDS.md](THEME_BACKGROUNDS.md) | **Vidéo par mode** + 3D animé (priorités, noms de fichiers, `remote_media.json`) |
| [AAA_Blockbuster_Specification_CongoGames.md](AAA_Blockbuster_Specification_CongoGames.md) | Vision production long terme (équipe, pipelines, jalons) |
| [UNITY_SCENE_SETUP.md](UNITY_SCENE_SETUP.md) §6 | Scène perso vs `RuntimeBootstrap` / fond 3D |
