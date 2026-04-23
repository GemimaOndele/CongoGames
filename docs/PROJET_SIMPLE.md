# Une seule règle : la branche `master`

- Il ne reste **que `master`** sur le dépôt (les branches `cursor/...` ont été retirées du serveur, tout le code utile y est).  
- Mettre à jour : `git pull origin master`  
- Si votre machine liste encore d’anciennes branches : `git fetch --prune` puis `git branch -D` le nom d’une branche locale obsolète, ou ne touchez à rien et restez sur `master`.

## Lancer le jeu (rappel court)

1. `Backend/.env` (copie de `Backend/.env.example` + clés)  
2. `npm run start-all` à la racine  
3. Unity **6000.4.x** (voir `UnityProject/ProjectSettings/ProjectVersion.txt`), ouvrir `UnityProject/`, **Play**  
4. (Option) `npm run demo:local` pour simuler le chat

Détail : **`docs/TESTER.md`**
