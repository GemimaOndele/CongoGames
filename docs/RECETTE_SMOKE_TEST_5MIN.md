# Recette smoke test (~5 min)

**Objectif** : après un `git pull` ou un gros changement, valider rapidement que l’UI, les mini-jeux et le thème se comportent sans régression. Pas un test unitaire : **parcours manuel** dans l’éditeur.

**Prérequis** : Unity **6000.4.x** (voir [`ProjectVersion.txt`](../UnityProject/ProjectSettings/ProjectVersion.txt)), scène de jeu (souvent vide + [`RuntimeBootstrap`](../UnityProject/Assets/Scripts/Core/RuntimeBootstrap.cs) qui monte le canvas). Détails cache / 1ʳᵉ install : [UNITY_LIBRARY_AND_LAUNCH.md](UNITY_LIBRARY_AND_LAUNCH.md). Boucle complète backend + TTS (optionnel ici) : [TESTER.md](TESTER.md).

---

## Avant **Play** (1 min)

| # | Action | OK |
|---|--------|-----|
| 1 | Ouvrir `UnityProject/`, fin import + compilation, **Console** : **0 erreur** rouge | [ ] |
| 2 | (Optionnel) `npm run start-all` à la racine — pour Lia / WS / TTS en même temps | [ ] |

---

## **Play** — navigation globale (1 min)

| # | Action | Résultat attendu | OK |
|---|--------|------------------|-----|
| 1 | Lancer **Play** | Le HUD type flux TV s’affiche (mode, chrono, classement si prévu) | [ ] |
| 2 | Lire l’entête de mode (ex. « Mode : … ») | Un libellé de mini-jeu apparaît | [ ] |
| 3 | Hors champ **InputField** (cliquer le fond) : **1** puis **2**… jusqu’à **9** (pavé ou ligne) | Chaque touche **change** de mini-jeu ([`GameModeManager`](../UnityProject/Assets/Scripts/Core/GameModeManager.cs) — 9 modes max en démo) | [ ] |
| 4 | **F9** (panneau debug PlayerPrefs) | Bascule fond 3D (si dispo) ; ne doit pas crasher | [ ] |
| 5 | **F10** (si barre d’URL thème) | Surcharge d’URL session ; fermer / réinitialiser sans bloquer l’écran | [ ] |

---

## Mini-jeux ciblés (2–3 min)

Parcourir au moins **une fois** chaque type critique (touches **1–9** pour y accéder plus vite qu’en attendant la rotation).

| # | Mode (indicatif) | Vérification | OK |
|---|------------------|-------------|-----|
| 1 | **Blind test** | Phase **écoute** : anneau / secondes = temps d’écoute restant ; **bip** chaque seconde. Puis A–D ; **bon / faux** : son + transition **~1 s** (pas d’enchaînement instantané). | [ ] |
| 2 | **Devine l’image** | Pendant la **révélation** (image floue → net) : compte à rebours + bips. Saisie une réponse, valider. | [ ] |
| 3 | **Chrono vitesse** | Compte 3-2-1 puis **GO** ; touches **1–4** ; fin de session = message puis enchaînement. Le **petit** chrono rond en bas est **masqué** pendant ce mode (gros compte en panneau). | [ ] |
| 4 | Un mode **mots** (mélangés / cachés / quiz…) | Saisir une réponse ou compléter une action ; pas d’**erreur** console au clic. | [ ] |
| 5 | **Faux** volontaire (mauvais choix) | **Shake** de l’UI adouci (pas de dislocation durable) ; l’écran redevient lisible avant le prochain mode. | [ ] |

**Données** : manches supplémentaires blind / image si présentes :  
`StreamingAssets/Datasets/minigame_blind_extras.json` et `minigame_image_guess_extras.json` ([chargées au runtime](../UnityProject/Assets/Scripts/Core/MiniGameDemoBanks.cs)).

---

## Thèmes & médias (30 s)

| # | Action | Résultat attendu | OK |
|---|--------|------------------|-----|
| 1 | Fond : **URL** `remote_media.json` (démo MDN / W3C) | Vidéo de fond **ou** repli 3D / synthé **sans** erreur rouge (réseau requis si URL). | [ ] |
| 2 | Si tu testes une **URL YouTube** dans le JSON (volontaire) | **Avertissement** en console, **ignoré** par le `VideoPlayer` — repli local / 3D ([`StreamingMediaUrlPolicy`](../UnityProject/Assets/Scripts/Presentation/StreamingMediaUrlPolicy.cs)). | [ ] |

Détail des priorités (vidéo / 3D / JSON) : [THEME_BACKGROUNDS.md](THEME_BACKGROUNDS.md), [THEME_YOUTUBE_AND_STREAMING.md](THEME_YOUTUBE_AND_STREAMING.md).

---

## Fin de recette (30 s)

| # | Action | OK |
|---|--------|-----|
| 1 | **Stop** Play : pas d’exception dans la **Console** | [ ] |
| 2 | Noter le **mode** / la **ligne** si anomalie (copier le message d’erreur) | [ ] |

**Si un point échoue** : [TESTER.md](TESTER.md) (boucle API), [UNITY_TROUBLESHOOTING_PLAY.md](UNITY_TROUBLESHOOTING_PLAY.md) (Play grisé), [UNITY_LIBRARY_AND_LAUNCH.md](UNITY_LIBRARY_AND_LAUNCH.md) (cache `Library`).

---

*Dernière mise à jour : recette ciblant les comportements documentés (HUD, mini-jeux, thème, Datasets). Ajuster la liste si tu ajoutes des modes ou des raccourcis.*
