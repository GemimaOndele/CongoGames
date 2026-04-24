# Vercel : un dépôt GitHub, deux projets (recommandé)

## Pourquoi deux projets Vercel ?

Ce n’est **pas** un doublon d’erreur : c’est la configuration **normale** pour un monorepo.

| Projet Vercel              | Rôle | Dossier racine (Root Directory) | URL typique        |
|---------------------------|------|----------------------------------|--------------------|
| **congogames**            | WebGL + **proxy** (rewrites) vers l’API ; le site **n’exécute** pas le Node | Voir ci‑dessous | `https://congogames.vercel.app` |
| **congogames-backend-cg** | **Vrai** serveur API (Node / Express) : TTS, `/health`, etc. | `Backend` | ex. `…-nine.vercel.app` (cible des rewrites) |

`congogames` **ne** « surcharge » **pas** le backend : il sert le jeu et **redirige** les chemins d’API vers le déploiement `congogames-backend-cg`. **Sans** ce second projet, il n’y a **rien** d’hébergé côté serveur (plus de TTS, plus d’API). **Ne pas le supprimer** sauf si tu déplaces toute l’API dans un autre hébergeur et tu mets à jour `vercel.json` (rewrites) en conséquence.

**Une seule source Git** : le dépôt **`https://github.com/GemimaOndele/CongoGames`**, branche de production **`master`** (c’est la branche utilisée dans ce dépôt).

Ne pas maintenir un **second** dépôt GitHub « backend seul » pour le même code : tout est déjà sous `Backend/` dans `CongoGames`. Si un ancien projet Vercel pointait vers un autre repo, il faut **le basculer** sur `CongoGames` (voir ci‑dessous).

---

## Règle d’or

- **Un** dépôt GitHub : `GemimaOndele/CongoGames`
- **Deux** projets Vercel, chacun avec un **Root Directory** différent
- **Branche de production** : **`master`** sur les deux (alignée sur GitHub)

Si Vercel est réglé sur **`main`** alors que le dépôt ne pousse que sur **`master`**, tu verras **« No Production Deployment »** sur le backend et aucun déploiement prod sur les pushes.

---

## 1. Projet `congogames` (Git + `congogames.vercel.app`)

**Connexion dépôt (CLI, déjà faisable en local) :** depuis le dossier `webgl-site` (projet lié `congogames`) :

```bash
npx vercel@latest git connect https://github.com/GemimaOndele/CongoGames
```

Puis, dans le **dashboard** Vercel → **congogames** → **Settings** → **Git** :

- **Production Branch** : `master` (le dépôt utilise `master`, pas seulement `main`).

**Root Directory** — deux modèles valides (choisir **un** et s’y tenir) :

1. **Racine du dépôt (`.`)** — le fichier **`vercel.json` à la racine** du repo sert le dossier `webgl-site` (`"outputDirectory": "webgl-site"`) : adapté quand Vercel est branché sur la racine par défaut.
2. **`webgl-site` uniquement** — alors c’est le fichier **`webgl-site/vercel.json`** qui s’applique (pas celui de la racine).

Si tu modifies les **rewrites** (URL de l’API) et que les deux `vercel.json` coexistent, **mets-les à jour en parallèle** (`/` et `webgl-site/`) pour éviter la dérive, **ou** ne garde qu’une seule config en fixant le Root Directory côté Vercel.

**Déploiement manuel WebGL** : à la **racine** du clone (`npm run webgl:vercel`). Ne pas lancer `vercel` **depuis** le seul dossier `webgl-site` si le réglage Vercel du projet indique déjà **Root Directory = `webgl-site`** (sinon erreur `…\webgl-site\webgl-site`). Le lien `.vercel` à la racine du dépôt cible le projet **congogames** ; le `vercel.json` à la racine sert le build avec `"outputDirectory": "webgl-site"`.

---

## 2. Projet `congogames-backend-cg` (« No Production Deployment »)

1. **Settings** → **Git** : le dépôt doit être **`GemimaOndele/CongoGames`** (si un autre repo apparaît, *Disconnect* puis reconnecte le bon).
2. **Root Directory** : **`Backend`** (obligatoire).
3. **Production Branch** : **`master`**.
4. **Settings** → **Environment Variables** : reprendre les clés (TTS, OpenAI, etc.) déjà utilisées en déploiement CLI.
5. Lance un déploiement : push sur `master` ou **Deployments** → *Redeploy*.

Si le build échoue, ouvre le log du déploiement ; souvent : mauvais dossier racine, ou branche qui ne correspond pas.

**Déploiement manuel** (sans Git) : depuis la machine, à la racine du clone : `npm run backend:vercel` (déjà configuré pour lancer Vercel dans `Backend/`).

---

## 3. Que mettre dans Unity / scripts

- **TTS / HTTP** : `https://congogames.vercel.app` (rewrites / proxy vers l’URL **directe** de l’API, déployée sur **congogames-backend-cg**).
- Après changement d’URL de l’API **directe** : mettre à jour les `destination` dans le `vercel.json` **efficace** (racine et/ou `webgl-site/`, voir ci‑dessus), puis redéployer **congogames**.

---

## 4. Résumé « meilleure option »

| Option | Verdict |
|--------|--------|
| Un repo `CongoGames` + deux projets Vercel (`webgl-site` / `Backend`) | **Recommandé** : clair, CI sur chaque push, pas de duplication Git. |
| Deux repos GitHub pour le même backend | **À éviter** : confusion, doubles PR, risque de désalignement. |
| Tout fusionner en un seul projet Vercel | Possible mais moins propre (builds mélangés, config plus lourde) ; le modèle actuel à deux projets est volontaire. |

En bref : **ne supprime pas** un des deux projets Vercel sans raison ; **aligne** les deux sur **le même repo** `CongoGames`, **branche `master`**, et les bons **Root Directory**.
