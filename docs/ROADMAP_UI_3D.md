# Après le MVP (interface « type PS5 / TikTok live »)

Le client actuel est un **HUD 2D** (texte, grilles) branché WebSocket + TTS. **Étape 1 « vrai 3D » (dans le dépôt)** : `VirtualShowStage3D` — plateau TV **URP** (primitives + lumières) rendu vers une **RenderTexture** sur le `RawImage` de fond quand il n’y a **pas** de vidéo `StreamingAssets/Theme/...` (voir `ThemeBackgroundController`). Option : `PlayerPrefs` **`CongoUseVirtual3D`** (désactiver = repli `SyntheticVideoBackground` 2D), rappel **F9** dans le jeu. Ce n’est pas un bloc PS5 avec personnages HD, mais une **base scène 3D** runtime.

En complément : `Ps5HudParallax` + `Ps5ModeVisualRig` + `Ps5CanvasBackdropRig` (effet « console » sur l’UI 2D).

Un **rendu « jeu console 3D » complet** (personnages, scène 3D) demande **d’autres pipelines** (assets, rigging) :

- **Modèles 3D** (robot, plateau, public), **éclairage**, **post-process** (URP), **caméras** cinématiques.
- **Animations** (Timeline, Animator, VFX Shader Graph), **UI** World Space ou mix **Screen Space** + rendu 3D.
- **Équipe / temps** : rigging, assets, direction artistique (le scope n’est pas « un commit »).

## Piste réaliste par étapes

1. Gardez le flux actuel **Play** + backend ; remplacez progressivement les panneaux par des **prefabs** plus travaillés (même scripts).
2. Variante avancée : **scène Unity** dédiée « show TV » + **Render Texture** pour y afficher le **HUD** actuel (le code actuel fait l’inverse : 3D → texture de **fond**).
3. **Audio** : mixer Unity (déjà possible), stingers par bonne/mauvaise réponse (déjà partiellement là).
4. **TikTok** : gardez **lisibilité** (gros texte, contrastes) — les effets ne doivent pas masquer questions et scores.

Ce document évite de promettre un « PS5 complet » dans le code actuel ; il fixe la direction si vous investissez en contenu 3D plus tard.
