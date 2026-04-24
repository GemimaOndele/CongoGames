# Politique de contenu (CongoGames)

## Objectif

Enrichir le jeu avec **images, audio et métadonnées** stockés **en local** et versionnables sur Git, **sans exposer le projet** à des réclamations de droits d’auteur (stores, GitHub, TikTok, monétisation).

## Ce qu’il faut éviter

- **Télécharger de la musique / vidéos depuis YouTube, Spotify, Deezer, etc.** pour les redistribuer dans le jeu : le droit d’écoute en streaming **n’est pas** un droit de réutilisation commerciale dans un binaire.
- **« Récupérer tout Internet »** sans licence explicite : instable légalement et pour les plateformes.
- **Faire labeliser automatiquement** une langue de chant (Kongo, Mbochi, Téké…) **à partir d’un audio seul** : ce n’est pas fiable ; la vérité terrain vient d’**experts, labels, chercheurs, ou de ta propre fiche** par piste.

## Pistes sûres (départ)

| Type | Où chercher (indicatif) | Condition |
|------|-------------------------|-----------|
| Images | [Wikimedia Commons](https://commons.wikimedia.org) (API), Openverse, photos dont **tu** détiens les droits | Conserver auteur + licence (souvent CC BY / BY-SA) |
| Musique / sons | [Wikimedia Commons](https://commons.wikimedia.org/wiki/Category:Ogg_files), [FMA (Free Music Archive)](https://freemusicarchive.org) selon filtre licence, [Openverse audio](https://openverse.org) | Même règle : licence affichée + attribution dans `ATTRIBUTION.md` |
| Données structurées | Questionnaires **rédigés par toi**, communauté (avec cession de droits claire), texte sous licence ouverte, **pas** reprise de banques commerciales protégées | Traçabilité |
| Génération IA (images, texte) | Selon **conditions d’utilisation** du modèle (usage commercial autorisé ou non) | Documenter l’origine (modèle, date) |

## Ce que le dépôt fournit

- Schémas JSON (`Datasets/schemas/`) pour lier **fichier + licence + questions**.
- Exemples (`Datasets/content/examples/`) : à dupliquer et compléter.
- Script d’aide `scripts/fetch-commons-assets.mjs` : ne télécharge que depuis **Commons** avec métadonnées (pas de YouTube).

## Rôle des langues (Kongo/Lari, Mbochi, Téké, Lingala, Kituba…)

Pour un blind test du type *« dans quelle langue est ce chant ? »* :

1. Tu places un fichier audio **sous licence claire** (fichier `.ogg` / `.mp3` autorisé).
2. Tu remplis la **fiche** (`audioFile`, `attribution`, `languageAnswer` validée par écoute + source **humaine** : musicien, fiche de disque, enquête terrain).
3. L’**IA** peut aider à **rédiger** des formulations de question ou des **faux bons** plausibles, **pas** à inventer la langue « à l’oreille » sans contrôle.

---

*Ce document vise à cadrer la production ; il ne constitue pas un avis juridique : en cas de doute, consulter un professionnel du droit des médias dans ta juridiction.*
