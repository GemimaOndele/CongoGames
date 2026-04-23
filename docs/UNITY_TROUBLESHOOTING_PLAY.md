# Play bloqué : « All compiler errors have to be fixed… » (Console 0 erreur)

Ce document regroupe les causes fréquentes quand le bouton **Play** reste inactif alors que la **Console** n’afficre qu’**un avertissement** (souvent *Input Manager is deprecated* — **sans rapport**, ce n’est pas une erreur de compilation).

## 1) Version d’éditeur Unity (critique)

Le dépôt fixe l’intention ici : [`UnityProject/ProjectSettings/ProjectVersion.txt`](../UnityProject/ProjectSettings/ProjectVersion.txt) (ex. **6000.4.3f1**).

- **Utilisez Unity Hub** pour installer **la même version** (ou la même **ligne mineure** 6000.4.x) et ouvrez le projet avec celle-là.  
- Si vous ouvrez le projet avec une autre (ex. **6000.0.3f1** alors que le projet vise 6000.4.x), Unity peut **résoudre les packages différemment** (`Library` / `packages-lock.json`), générer des scripts package obsolètes et laisser le compilateur en état **d’erreur non affiché** dans la Console.
- **Après** avoir choisi la bonne version : menu **File → Open Project** sur `UnityProject/`, laissez l’import finir, puis regardez la Console (filtre erreurs) et le pas suivant.

## 2) Vérifier le log compilation

Avec **Unity fermé** :

- **Windows** : ouvrez `%LOCALAPPDATA%\Unity\Editor\Editor.log` (ou *Help → Open log folder* si disponible).  
- Cherchez **`error CS`**, **`Failed to`**, **`Bee`**, **`Exception`**.

La vraie erreur s’y trouve souvent quand l’UI affiche 0 erreur en rouge.

## 3) Fichier `Assets/csc.rsp`

`Assets/csc.rsp` ne doit **pas** contenir de bribes d’options invalides. Le dépôt le garde **vide** (ou uniquement des options Roslyn valides documentées par Unity, ex. des `-define:…`).

## 4) Cache : `Library` et `Temp`

1. Fermer Unity.  
2. Supprimer le dossier **`UnityProject/Library`** et **`UnityProject/Temp`**.  
3. Exécuter à la racine du dépôt (Unity **toujours fermé**) : `.\prepare-unity.ps1` ou `npm run unity:prepare`.  
4. Rouvrir le projet et attendre la fin de l’import + compilation.

*(Chemins de projet contenant des **emojis** caractère unique, **antivirus** agressif sur `Library\Bee`, manque d’espace disque : voir [TESTER.md](TESTER.md), tableau dépannage.)*

## 5) Assemblies du dépôt

- Le code sous **`Assets/Scripts`** est compilé en **`Assembly-CSharp`** (fichier unique par défaut) : toutes les références `Unity.*` héritent des modules du projet — **inutile** d’ajouter un `.asmdef` runtime ici, au risque d’oublier un module (WebRequest, Video, UGUI…) et de provoquer des **erreurs CS invisibles** ou des Play bloqués.  
- Le menu URP **CongoGames** est dans l’assembly éditeur **`CongoGames.Editor.URP`** (`Assets/Editor/RenderingSetup/`). En cas d’erreurs **uniquement** côté menu outil, vérifiez ce dossier + le package URP.

## 6) Dernière piste

**Assets → Reimport All** (long) ou vérification **Project Settings → Player** : **Active Input Handling = Input Manager** si le manifest ne contient pas `com.unity.inputsystem` (c’est le cas par défaut du dépôt).

---

Références : [TESTER.md](TESTER.md) (section Unity Play, `csc.rsp`, Bee), [UNITY_LIBRARY_AND_LAUNCH.md](UNITY_LIBRARY_AND_LAUNCH.md).
