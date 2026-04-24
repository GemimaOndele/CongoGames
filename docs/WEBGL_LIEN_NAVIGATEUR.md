# Jouer dans le navigateur (lien sur téléphone / PC)

**Objectif** : ouvrir **un seul lien HTTPS** (Chrome, Safari, etc.) et lancer le jeu **sans installer d’APK** — utile pour des tests sur n’importe quel appareil, en parallèle du live TikTok Studio.

Ce n’est **pas** la même chose que le **backend** (Vercel / Railway) : le dépôt actuel déploie surtout l’**API Node** sur Vercel. Le **client Unity** dans le navigateur, c’est un **build WebGL** + **fichiers statiques** hébergés à part.

## Mise en place dans ce dépôt (résumé)

| Élément | Rôle |
|--------|------|
| Dossier [`webgl-site/`](../webgl-site/) | Cible de déploiement : copier ici toute la **sortie** d’un build Unity WebGL, puis `npm run webgl:vercel` (Vercel CLI) ou `npx vercel --prod` depuis `webgl-site/`. |
| [`webgl-site/copy-into-webgl-site.ps1`](../webgl-site/copy-into-webgl-site.ps1) | Copie un dossier de build verifié vers `webgl-site/` (sans supprimer `vercel.json` / `README`). |
| `UnityProject/Assets/Resources/CloudEndpoints.json` | En **build WebGL** uniquement : URL **WSS** (Railway) + **HTTPS** TTS (Vercel) lues au démarrage — fini le `localhost` sur téléphone. |
| Scripts npm | `npm run webgl:vercel` à la **racine** du dépôt. |

---

## 1. Principe

1. Dans Unity : **File → Build Settings…** → plateforme **WebGL** → **Switch Platform** (la première fois : import des modules WebGL).
2. Met au moins **une scène** dans *Scenes In Build* (ex. ta scène avec `RuntimeBootstrap`, ou une scène vide qui lance le flux habituel).
3. **Build** vers un dossier local, ex. `Builds/WebGL/` à la racine du dépôt (dossier **à ne pas commiter** si trop lourd — voir `.gitignore`).
4. Ce dossier contient `index.html`, `.js`, `.data`, `.wasm`, etc. → à servir en **HTTPS** sur un hébergeur **statique**.

Ensuite : **https://ton-site.vercel.app** (ou autre) ouvre le jeu comme une page web.

---

## 2. Réglages réseau (indispensable pour le « vrai » cloud)

Dans le navigateur, **`http://127.0.0.1`** n’existe pas sur le téléphone : le build WebGL doit parler à ton **API + WebSocket déjà déployés** :

| Composant | En local (éditeur) | En build WebGL « en ligne » |
|-----------|---------------------|-----------------------------|
| TTS / HTTP | `http://127.0.0.1:3000` | `https://…vercel.app` (ou URL Railway HTTP) |
| WebSocket | `ws://localhost:8080` | `wss://…railway.app` |

À faire **avant** ou **après** le build :

- Sur **Railway** : `PUBLIC_HTTP_BASE=https://ton-api-vercel.vercel.app` (sans slash final) pour que le message système WS donne la bonne base au client.
- Dans **Unity** : sur `LiveEventClient`, `wsUrl` = ton `wss://` Railway ; sur `AIHostManager`, `ttsHttpBase` = ton `https://` Vercel (ou laisser la découverte si le WS envoie déjà `httpApiBase`).

Sinon le jeu se lance « à vide » côté voix / live.

**Contenu mixte** : la page du jeu doit être en **HTTPS** si l’API est en HTTPS (sinon le navigateur peut bloquer les requêtes).

**CORS** : le backend Node (`server.js` et `api/index.js`) envoie `Access-Control-Allow-Origin: *` pour que `UnityWebRequest` (build WebGL) puisse appeler `/tts` depuis un domaine Vercel différent. Redéploie le backend après mise à jour si besoin.

---

## 3. Où héberger les fichiers du build (exemples)

| Option | Idée |
|--------|------|
| **Projet Vercel dédié `webgl-site/`** (recommandé ici) | Après build Unity, lancer le script `copy-into-webgl-site.ps1`, puis `npm run webgl:vercel`. Le `vercel.json` du dossier configure les bons `Content-Type` (`.wasm`, etc.). **Ne mélange pas** avec le déploiement `Backend/` (API) : deux projets / deux domaines, ou un monorepo avec deux cibles. |
| **Netlify** | Glisser-déposer le dossier du build (**Deploy manually**). |
| **GitHub Pages** | Branche `gh-pages` avec le contenu du build, ou action CI qui pousse le build. |
| **Cloudflare Pages** | Pareil : répertoire de build en source. |

Limite Vercel gratuite : **taille** du déploiement ; un gros WebGL peut nécessiter compression Brotli (déjà souvent activée côté Unity) ou hébergeur fichier lourd. Les binaires du build ne sont en général **pas** commiter (voir `webgl-site/.gitignore`).

---

## 4. Test rapide **sans** cloud (LAN)

Sur le PC qui a le build :

```bash
cd Builds/WebGL
npx serve -s .
```

Ouvre `http://IP_DU_PC:3000` depuis le **téléphone** sur le même Wi‑Fi (pas `localhost`). Pour HTTPS + partage public, utiliser un tunnel (**ngrok**, **Cloudflare Tunnel**) pointant vers ce serveur local.

---

## 5. Limites WebGL (à connaître)

- **Premier chargement** : peut être long (téléchargement `.data` / `.wasm`).
- **Vidéo** : `VideoPlayer` + URL externes → souvent soumis au **CORS** du serveur vidéo ; les fonds `remote_media.json` peuvent exiger des en-têtes côté CDN.
- **Qualité / RAM** : URP + effets lourds : viser des réglages WebGL dans l’éditeur (voir `docs/URP_Migration_Checklist_CrossPlatform.md`).
- **TikTok / intégrations natives** : tout ce qui est **plugin desktop** ou **app** peut ne pas exister en Web ; le flux **HTTP + WS** reste le plus portable.

---

## 6. Résumé

| Besoin | Action |
|--------|--------|
| Lien « joue dans le navigateur » | Build **WebGL** + hébergement **statique HTTPS**. |
| Même backend que la prod | `PUBLIC_HTTP_BASE` + `wss` + build client pointant dessus. |
| Ne pas confondre avec l’API actuelle | Le **Backend/** sur Vercel sert l’**API** ; le **jeu** est un autre artefact (dossier WebGL). |

Pour un build **Android / iOS** installable (pas de navigateur), voir la section *Build* de `docs/TESTER.md` — autre pipeline que WebGL.
