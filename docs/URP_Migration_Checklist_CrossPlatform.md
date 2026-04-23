# Checklist migration URP — CongoGames (PC, mobile, WebGL, consoles)

Objectif : passer du **Built-in Render Pipeline** à **URP** sans casser l’UI ni les matériaux, avec un profil **compatible toutes plateformes** (pas HDRP).

**Prérequis** : Unity **6000.4.x** (Unity 6.4) aligné avec le `ProjectVersion.txt` du dépôt.

---

## 1. Packages et projet

- [ ] Ouvrir le projet : Unity résout `com.unity.render-pipelines.universal` (voir `Packages/manifest.json`).
- [ ] Si erreur de version : **Window > Package Manager** → *Universal RP* → version recommandée par l’éditeur pour votre patch exact (remplacer `17.5.0` dans le manifest si nécessaire, puis réouvrir).
- [ ] Lire les éventuels messages de migration Unity 6 (compatibilité packages).

---

## 2. Créer et assigner le pipeline (recommandé)

- [ ] Menu **CongoGames > Rendering > Créer et assigner URP (multi-plateforme)**, ou **Window > CongoGames > Créer et assigner URP**, ou **Tools > CongoGames > Assigner le pipeline URP** (même action).
- [ ] Vérifier **Edit > Project Settings > Graphics** : *Scriptable Render Pipeline Settings* pointe vers `Assets/Settings/URP/CongoGames_UniversalRP.asset`.
- [ ] Optionnel : **CongoGames > Rendering > Activer espace couleur Linear** (recommandé pour PBR ; tester **WebGL** après changement).

---

## 3. Matériaux et shaders (éviter le « rose »)

- [ ] **Edit > Rendering > Materials** : convertir les matériaux du projet (ou **Upgrade Project Materials to URP** selon version d’éditeur).
- [ ] Pour les assets tiers : importer les **URP** ou **Shader Graph** fournis par l’éditeur d’asset ; sinon recréer les matériaux avec **Lit** / **Unlit** URP.
- [ ] **Sprites / UI** : l’UI uGUI utilise en général des shaders déjà compatibles ; si un élément devient rose, réassigner un shader **UI/Default** ou **Sprite-Lit-Default** URP.
- [ ] Ne pas mélanger **HDRP** et **URP** dans le même projet.

---

## 4. Éclairage et qualité

- [ ] Vérifier les **Lights** : ombres et intensités (URP gère différemment le realtime baked mix selon scène).
- [ ] **Project Settings > Quality** : le projet peut garder `customRenderPipeline` vide sur chaque niveau pour hériter du pipeline global ; ou assigner le même URP Asset par niveau si vous utilisez des profils différents.
- [ ] **PC / console** : MSAA 2× ou 4× sur l’URP Asset si la cible suit ; **mobile / WebGL** : commencer sans MSAA ou 2×, `Render Scale` ≤ 1 si perf faible.

---

## 5. Plateformes spécifiques

| Plateforme | Points de vigilance |
|------------|---------------------|
| **Standalone (PC)** | Cible principale live + OBS ; tester résolution et fullscreen. |
| **Android / iOS** | Compression textures (ASTC), `Render Scale`, limiter post-process lourd. |
| **WebGL** | WebGL2 + URP possible ; build long ; limiter effets, tester Linear si activé. |
| **Consoles** (si plus tard) | Profils qualité dédiés, certification plateforme. |

---

## 6. Validation rapide

- [ ] Scène principale : pas de matériaux roses, UI lisible.
- [ ] Build **Standalone** + smoke test.
- [ ] Si vous ciblez **mobile** ou **WebGL** : un build réel sur appareil cible (pas seulement l’éditeur).

---

## 7. Références Unity

- [Universal Render Pipeline pour Unity 6.4](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/introduction-to-urp.html)  
- [Exigences et compatibilité URP](https://docs.unity3d.com/6000.4/Documentation/Manual/urp/requirements.html)  
- Spécification projet : `docs/AAA_Blockbuster_Specification_CongoGames.md` (annexe broadcast).

---

## Entrées : Input Manager (recommandé CongoGames)

- **Pas de** `com.unity.inputsystem` dans `Packages/manifest.json` : le package provoquait chez nous des erreurs d’import (`TypeLoadException` / `InputActionAsset` dans des UXML du paquet) et des blocages **Play** tant que l’éditeur ne compile pas proprement.
- **Player** : **Project Settings > Active Input Handling** = **Input Manager (Old)** (**0**). Unity peut afficher un avertissement de dépréciation : ignorable pour ce dépôt tant que l’on reste sur ce mode.
- **Touches démo** : **1–9** et **F10** via `UnityEngine.Input` (`GameModeManager`, `ThemeUrlDebugBar`).
- **UI** : `RuntimeBootstrap` utilise **`StandaloneInputModule`** sur l’`EventSystem`.

**Option avancée — réactiver le package Input System** : ajouter `com.unity.inputsystem` (ex. **1.19.0** pour Unity 6.4), régler le Player sur **Input System Package**, migrer le code (`Keyboard.current`, …) et l’UI (`InputSystemUIInputModule`), puis avec Unity **fermé** lancer **`npm run unity:patch-input-uxml`** une fois `Library/PackageCache` peuplé (voir `UnityProject/tools/PatchInputSystemUxml.ps1`) pour éviter l’erreur sur `InputActionsProjectSettings.uxml`.

### Message « On demand scheduler requested to import while stopped »

Souvent déclenché par un **`AssetDatabase.Refresh()`** au mauvais moment. Les scripts Éditeur CongoGames **n’appellent pas** `Refresh` après la création URP ; si le message vient d’une autre extension, évitez les `Refresh` dans `InitializeOnLoad` / `delayCall` trop tôt.

### Safe Mode + « ValidateMenuItem … Assets/Create/Scripting/C# Script »

Sous **Unity 6.4**, le menu **`Assets > Create > Scripting > C# Script`** n’existe plus (remplacé par MonoBehaviour / ScriptableObject / Blank script). Le package **URP** ajoutait des entrées sous **`Assets/Create/Scripting/URP …`**, ce qui peut déclencher cette erreur et bloquer l’éditeur en **Safe Mode**.

**Correctif CongoGames** (avec Unity fermé) : `.\prepare-unity.ps1` exécute aussi **`PatchUrpScriptingMenu.ps1`**, qui déplace ces entrées vers **`Assets/Create/Rendering/URP …`**. Ou : `npm run unity:patch-urp-menus`. Ensuite rouvrir Unity ; quitter le Safe Mode une fois la compilation OK.

Les gabarits URP (Renderer Feature, Post-process) se retrouvent alors sous **Assets > Create > Rendering** avec les autres assets URP.

---

## Dépannage : `FSBTool` / FMOD sur des `.ogg`

Messages du type `FSBTool ERROR: Internal error from FMOD sub-system` à l’import d’un **AudioClip** : le fichier **OGG** est souvent dans un format que l’outil d’import n’aime pas (fichier renommé, encodage exotique, flux corrompu).

**Correctif appliqué dans le dépôt** : réencodage **Vorbis** propre avec **ffmpeg** pour `sfx_cheer.ogg`, `sfx_laugh.ogg`, `sfx_wrong.ogg` sous `Assets/Resources/Audio/`.

Pour refaire localement :

```bash
ffmpeg -y -i sfx_cheer.ogg -c:a libvorbis -q:a 5 sfx_cheer_fixed.ogg
```

Puis remplacer l’original. Alternative : exporter en **`.wav`** (PCM) et mettre à jour les références si vous retirez l’OGG.

---

*Les assets URP générés par le menu Éditeur vivent sous `Assets/Settings/URP/` et peuvent être versionnés une fois créés.*
