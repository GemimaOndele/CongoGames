# Prompts IA — rédaction assistée (pas scraping YouTube)

Utilise ces prompts **dans un outil IA** (ChatGPT, Cursor, etc.) pour **produire du texte** (questions, faux bons, indices), **après** avoir choisi la **bonne réponse** toi-même (fiche audio / image validée).

## Règle d’or

> L’IA ne doit **pas** inventer la « langue exacte » d’un extrait sans fichier et sans source humaine. Elle peut **formuler** des QCM à partir d’une langue **que tu as déjà décidée**.

---

## Prompt 1 — QCM langue du chant (4 choix)

```text
Tu es rédacteur culturel pour la République du Congo (capitale Brazzaville), pas la RDC.

Contexte fourni par l’humain :
- Langue correcte du couplet (validée) : [Kongo/Lari | Mbochi | Téké | Lingala | Kituba | …]
- Style musical (indicatif) : [traditionnel | rumba | gospel | …]

Génère UNE question en français pour un blind test TikTok, 4 choix A–D, une seule bonne réponse.
Évite toute mention de Kinshasa comme capitale du même pays que Brazzaville dans la bonne réponse.
Format JSON : { "question", "choices": [4 strings], "correctIndex": 0-3, "hintCourt" }
```

---

## Prompt 2 — Faux bons plausibles (sans copier de texte protégé)

```text
Pour une question sur le Congo (Brazzaville), génère 3 mauvaises réponses plausibles (géographie, culture) pour une QCM dont la bonne réponse est : [TEXTE].
Les 3 distractors doivent être factuellement fausses mais crédibles pour un joueur.
Pas de noms d’artistes ou de titres d’album récents sans source (évite le copyright).
```

---

## Prompt 3 — Indices (image)

```text
Image : [lieu ou sujet validé]. Rédige 2 indices courts (max 120 caractères chacun) pour un jeu devine-image, ton léger, public TikTok, sans promettre de reconnaissance faciale d’individus réels.
```

---

## Ce qu’il ne faut pas demander à l’IA

- « Télécharge la musique de [artiste] sur YouTube »
- « Devine la langue de ce MP3 » sans fournir le fichier et une référence humaine
