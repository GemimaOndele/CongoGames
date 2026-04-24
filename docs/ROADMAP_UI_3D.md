# Après le MVP (interface « type PS5 / TikTok live »)

Le client actuel est un **HUD 2D** (texte, grilles, fond animé `SyntheticVideoBackground`, révélation d’image en shader léger) branché WebSocket + TTS — c’est volontaire pour valider la boucle live. En complément (sans scène 3D), le dépôt ajoute un **ressenti « console »** léger : `Ps5HudParallax` + `Ps5ModeVisualRig` + `Ps5CanvasBackdropRig` sur le bootstrap (mouvement 2,5D, légère variation d’échelle par `modeId`, vignette animée). Ce n’est **pas** un remplacement d’une vraie scène 3D.

Un **rendu « jeu console 3D » complet** (personnages, scène 3D) demande **d’autres pipelines** (assets, rigging) :

- **Modèles 3D** (robot, plateau, public), **éclairage**, **post-process** (URP), **caméras** cinématiques.
- **Animations** (Timeline, Animator, VFX Shader Graph), **UI** World Space ou mix **Screen Space** + rendu 3D.
- **Équipe / temps** : rigging, assets, direction artistique (le scope n’est pas « un commit »).

## Piste réaliste par étapes

1. Gardez le flux actuel **Play** + backend ; remplacez progressivement les panneaux par des **prefabs** plus travaillés (même scripts).
2. Ajoutez une **scène** dédiée « show TV » : un plan 3D simple (sol, écran géant) + **Render Texture** pour y afficher le HUD actuel.
3. **Audio** : mixer Unity (déjà possible), stingers par bonne/mauvaise réponse (déjà partiellement là).
4. **TikTok** : gardez **lisibilité** (gros texte, contrastes) — les effets ne doivent pas masquer questions et scores.

Ce document évite de promettre un « PS5 complet » dans le code actuel ; il fixe la direction si vous investissez en contenu 3D plus tard.
