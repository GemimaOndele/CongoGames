# Remettre le projet à jour par rapport à Git (sans tout perdre)

Quand `git pull` échoue (fichiers modifiés en local, fichiers non suivis, etc.), vous pouvez utiliser le script à la **racine du dépôt** : **`reset-vers-origin.ps1`**.

## Ce qu’il fait (dans l’ordre)

1. **Sauvegarde** le projet dans le dossier parent (ex. `C:\` → un dossier `Congogame-sauvegarde-AAAA-MM...`), en **excluant** les gros caches (`Library`, `Temp`, `node_modules`) pour aller plus vite.  
2. **Copie** `Backend/.env` dans un petit sous-dossier de sauvegarde, puis (optionnel) la **même** pour un fichier **image** que vous indiquez.  
3. Exécute **`git fetch`**, bascule sur **master** ou **main** (selon ce qui existe sur `origin`), **`reset --hard`**, **`clean -fd`**.  
4. **Recolle** `Backend/.env` et l’**image** au bon endroit.

## Avant

- Fermer **Unity**
- Fermer le terminal où tourne `npm run start-all` (s’il y en a un)

## Commande (PowerShell, dans le dossier du dépôt)

Ouvrez PowerShell, allez **à la racine** (là où se trouve `reset-vers-origin.ps1` et le dossier `Backend`).

Un seul lancement, sans paramètre (garde seulement `.env`) :

```powershell
Set-Location C:\Congogame
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\reset-vers-origin.ps1
```

Avec le chemin d’**une** image à préserver (remplacez par le vrai chemin) :

```powershell
.\reset-vers-origin.ps1 -Image "C:\Congogame\mon_dossier\logo.png"
```

**Simulation** (rien n’est modifié, vous voyez ce qui serait fait) :

```powershell
.\reset-vers-origin.ps1 -WhatIf
```

Sauvegarde lourde désactivée (reset quand même, `.env` / image lus s’ils existent) :

```powershell
.\reset-vers-origin.ps1 -SkipBackup
```

## Après

```powershell
cd Backend
npm install
```

Recréez `Backend/.env` si le script a indiqué qu’il n’en avait pas trouvé (copiez depuis le dossier de sauvegarde `_fichiers-a-garder`).

---

**Important :** l’exécution se fait sur **votre PC** (Cursor ici ne peut pas lancer ce script sur `C:\` à votre place). Copiez-collez les commandes une par une.
