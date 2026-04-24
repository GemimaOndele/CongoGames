# Itération 3D : du socle procédural vers un rendu « production »

Ce fichier **exécute** la séparation annoncée dans le code et [ROADMAP_UI_3D.md](ROADMAP_UI_3D.md) :

- **Aujourd’hui (dépôt)** : primitives + lumières → `VirtualShowStage3D` → RenderTexture sur le fond **2D** ; **pas** de mesh d’artiste, **pas** de public 3D, **pas** d’avatar dans ce pipeline.

- **Demain (hors scripts seuls)** : un rendu type **gros budget** (persos HD, ciné, assets dédiés) = **art + modèles + rig + post-process** en plus, souvent une **scène `.unity` dédiée**.



La vision long terme (organisation studio, budgets) est dans [AAA_Blockbuster_Specification_CongoGames.md](AAA_Blockbuster_Specification_CongoGames.md). Ici : **checklist technique** pour ne pas confondre socle et prod.



---



## Phase A — Socle actuel (déjà en place)



| Élément | Fichier / emplacement |

|---------|------------------------|

| Plateau procédural URP | `UnityProject/Assets/Scripts/Presentation/VirtualShowStage3D.cs` |

| Choix vidéo / 3D / synthé | `ThemeBackgroundController.cs` |

| Préférences qualité & 3D | `PresentationConfig.cs` ; `CongoUseVirtual3D`, `CongoPresentationQuality` |

| UI stream (canvas) | `RuntimeBootstrap.cs`, `MiniGamePanelContent.cs` |



**À valider en Play :** fond 3D cohérent, `CongoUseVirtual3D=1`, pas de second « sol » maison en parallèle (voir [UNITY_SCENE_SETUP.md](UNITY_SCENE_SETUP.md) §6).



---



## Phase B — Première itération « vrai contenu 3D » (équipe art + dev)



Ordre indicatif ; chaque point peut être une PR / tâche.



1. **Importer** des `.fbx` (plateau, props) + textures ; matériaux **URP Lit** ; tests **une** scène de test avant intégration jeu.

2. **Remplacer ou enrichir** `VirtualShowStage3D.BuildGeometry` : soit instancier des prefabs assignables (SerializeField), soit **charger additivement** une sous-scène « ShowStage » et y placer la caméra RT (même contrat : une caméra → `RenderTexture` → `RawImage` thème).

3. **Animator / Timeline** : intro courte, lumières, caméra (pas besoin de personnage au début).

4. **Post-process URP** : Volume (color grading, bloom léger) — testé en **stream** (lisibilité du texte UI).

5. **VFX** : particules ou Shader Graph **derrière** la zone de texte (zones sûres documentées dans la roadmap live).



---



## Phase C — Ambition « PS5 / ciné » (saut de production)



Nécessite typiquement : direction artistique, rig, personnages si besoin, polish lumière, profiling (build device cible), itérations QA stream.



- Référence structurante : [AAA_Blockbuster_Specification_CongoGames.md](AAA_Blockbuster_Specification_CongoGames.md) (périmètre, pipelines, jalons).

- Ne **pas** attendre tout cela du seul script `VirtualShowStage3D` : le remplacer ou le brancher sur des **assets versionnés** est l’objectif.



---



## Rappel anti-confusion



| Symptôme | Cause fréquente |

|----------|------------------|

| Sol « rose » ou décor incohérent | Autre objet de scène / matériau perso **en plus** du RT du thème ; aligner ou désactiver le doublon. |

| « Rien ne change » côté 3D | `CongoUseVirtual3D=0` ou vidéo `StreamingAssets/Theme/...` qui prend la priorité. |

| Attente d’un avatar HD | Hors socle : importer + intégrer (Phase B–C). |



Pour le détail du flux fond + UI, [ROADMAP_UI_3D.md](ROADMAP_UI_3D.md) ; pour **fonds vidéo par jeu + 3D de secours**, [THEME_BACKGROUNDS.md](THEME_BACKGROUNDS.md).


