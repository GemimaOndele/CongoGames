# Check-list Graphique Express (Windows)

1. Dans `Project Settings > Player > Other Settings`, décoche `Auto Graphics API for Windows`.
2. Dans `Graphics APIs for Windows`, garde `Direct3D11` en premier et retire temporairement les autres API pour le test local.
3. Redémarre Unity et vérifie que la barre titre affiche bien `DX11`.
4. Mets à jour le pilote GPU et coupe les overlays (Xbox Game Bar, Discord, GeForce overlay).
5. Si le warning persiste : ferme Unity, supprime `UnityProject/Library` puis relance le projet.

