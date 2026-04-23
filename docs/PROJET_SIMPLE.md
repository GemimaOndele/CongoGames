# Une seule règle : la branche `master`

- Travaillez **uniquement** sur **`master`**.  
- C’est la branche contenant le jeu, le backend et la doc à jour.  
- Mettre à jour : `git pull origin master`

Les branches dont le nom commence par `cursor/` viennent d’outils d’intégration. Le contenu a été **fusionné dans `master`**. Vous pouvez supprimer ces branches côté GitHub (*Branches* → corbeille) quand vous voulez alléger l’affichage ; ce n’est **pas** obligatoire pour lancer le jeu.

## Lancer le jeu (rappel court)

1. `Backend/.env` (copie de `Backend/.env.example` + clés)  
2. `npm run start-all` à la racine  
3. Unity **6000.4.x** (voir `UnityProject/ProjectSettings/ProjectVersion.txt`), ouvrir `UnityProject/`, **Play**  
4. (Option) `npm run demo:local` pour simuler le chat

Détail : **`docs/TESTER.md`**
