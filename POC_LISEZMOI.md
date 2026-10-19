# Ferme du petit matin — POC Unity 2.5D

Unity 6000.3.0f1 / URP 17.3.0 / Input System 1.14.2.

## Lancer cette version

1. Arrête le mode Play, laisse Unity compiler, puis relance Play. Le clavier fonctionne aussi lorsque l’éditeur utilise encore Input Manager.
   Un redémarrage de Unity active la prise en charge complète des manettes par Input System.
2. Laisse les packages, images et scripts finir leur import.
3. Lance Play et ouvre l'onglet Game. La ferme est créée automatiquement, même depuis une scène vide.

Le projet utilise **Both** dans Project Settings > Player > Active Input Handling. Les commandes utilisent le système d’entrée réellement actif : Input System, ou Input Manager pour le clavier et les manettes Windows/XInput. L’interface IMGUI conserve le clic de souris.

Le pipeline URP est fourni dans Assets/Resources/Rendering. Il est affecté automatiquement à l'import et au lancement. Le menu **Farm POC > Configurer le rendu 2.5D URP** permet de réappliquer cette configuration.

## Commandes

### Jouer entièrement à la souris

- **Clic gauche sur le sol** : le personnage rejoint l'endroit choisi en contournant les collisions (mare, arbres, bâtiments, meubles).
- **Clic droit sur une case** : utilise l'outil équipé. Si la case est loin, le personnage s'approche puis agit une seule fois, sur cette case précise.
- **Roue** : clique sur ce bouton puis sur la bêche, les graines, l'arrosoir ou le panier. L'inventaire permet de sélectionner l'espèce de graines ou le produit à vendre.
- **Clic sur la maison, le marchand, l'armoire, le coffre ou le tapis de sortie** : le personnage s'approche puis interagit. Le bouton contextuel à gauche propose aussi ces interactions.
- **Parcelles** : ouvre les achats de terrain. Les étiquettes et les zones verrouillées sont également cliquables.
- **Pause, Inventaire, Lendemain** et tous les menus ont des boutons de souris. Le clic droit ferme un menu ou reprend le jeu depuis la pause. La roue dispose aussi d'un bouton Fermer.
- **Bouton central maintenu et déplacement horizontal de la souris**, ou boutons Caméra ‹/› : tourne la caméra extérieure.

Un nouveau clic remplace le trajet en cours. Le clavier, la manette, l'ouverture d'un menu et la pause annulent le déplacement et l'action en attente. Un clic dans l'interface ne commande pas d'action dans le monde situé derrière.

### Clavier et manette

Les boutons sont indiqués par position : A/Croix en bas, B/Rond à droite, X/Carré à gauche et Y/Triangle en haut.

| Action | Manette | Clavier / souris |
| --- | --- | --- |
| Déplacement | Stick gauche | ZQSD, WASD ou flèches |
| Agir / valider | A / Croix | E, Espace ou Entrée |
| Ouvrir la roue d'outils | X / Carré | Tab ou bouton Roue |
| Choisir l'outil dans la roue | Stick droit ou gauche | Flèches ou souris |
| Équiper l'outil choisi | A / Croix, ou X / Carré | E ou Tab |
| Annuler / fermer | B / Rond | Échap |
| Inventaire | Y / Triangle | I ou bouton Inventaire |
| Naviguer dans un menu | Stick gauche ou croix directionnelle | Flèches ou souris |
| Changer d'onglet | LB/RB ou L1/R1 | Page précédente / suivante |
| Modifier la quantité | LT/RT ou L2/R2 | −/+ ou boutons du menu |
| Marchand, à proximité | RB / R1 | B |
| Pause / reprendre | Start / Options | Échap depuis le jeu, ou bouton Pause |
| Passer au lendemain | Menu pause > Passer au lendemain, puis A / Croix | N, puis E, ou bouton Lendemain |
| Entrer / sortir de la maison | A / Croix près de la porte ou du seuil | E près de la porte ou du seuil |
| Décorer / ouvrir l'armoire | A / Croix près du coffre / de l'armoire | E près du coffre / de l'armoire |
| Tourner la caméra | Maintenir LT / L2 + stick droit | Maintenir Alt + C/V |
| Outils directs | Roue d'outils | 1 à 6 |

Roue : Bêche, Graines, Arrosoir, Panier, Maison, Armoire. Dans les menus, le mouvement du personnage est bloqué. Échap / B ferme d'abord le menu ouvert ; Échap depuis le jeu ouvre la pause.

## Maison et décoration

Approche-toi de la porte de la maison, en haut à gauche, puis appuie sur E / A. Tu arrives dans une pièce de neuf cases sur sept. Pour sortir, rejoins le seuil vert au centre du bord inférieur et appuie sur E / A.

Le coffre, dans le coin droit du fond, ouvre la décoration. L'outil Maison (5), la touche G ou le bouton Construire y donnent aussi accès depuis l'intérieur. Les meubles du catalogue sont gratuits dans ce POC : lit, table, chaise, canapé, bibliothèque, plante, lampe et tapis.

- Le catalogue affiche les meubles illustrés. Choisis-en un (clic, ou flèches puis E / A) : le panneau glisse sur le côté, le voile disparaît et un aperçu du meuble suit le curseur dans la pièce, vert si la place est libre, rouge sinon.
- Clic gauche ou E / A pose le meuble ; tu peux en poser plusieurs à la suite. Les flèches / le stick déplacent l'aperçu ; R / Y le tourne ; −/+ ou LT/RT changent de meuble.
- Tab / X passe en mode déplacement : vise un meuble existant et valide, puis choisis sa nouvelle place. Le meuble conserve son ancien emplacement si la nouvelle pose est invalide ou si tu quittes la pose.
- Suppr / R3 ou Retirer range le meuble visé dans le catalogue.
- Clic droit, Échap / B ou le bouton Catalogue ramènent le panneau au centre.
- Le panneau indique en direct l'état de la case visée, et une bulle sous l'aperçu donne la raison d'un refus. Au clavier et à la manette, l'aperçu démarre sur la place libre la plus proche. Juste après une pose, l'aperçu s'efface pour laisser voir l'objet posé.
- Les meubles doivent rester dans la pièce et ne peuvent pas se chevaucher. Les tapis peuvent passer sous le mobilier. L'entrée, le coffre et l'armoire gardent des cases réservées. La pose sur le personnage est refusée.
- Les onglets Papier peint et Sol proposent chacun trois revêtements. Page↑/↓ ou LB/RB change l'onglet ; gauche/droite change le revêtement.
- Terminer ou Échap / B conserve les modifications de décoration et tente une sauvegarde automatique. La décoration est appliquée immédiatement, et n'a pas de bouton d'annulation globale.

### Décorations de la ferme

Dehors, la touche G, le bouton Construire ou l'outil Maison ouvrent le même menu avec les décorations fabriquées à l'établi : caisse de marché, lanterne citrouille et épouvantail. L'onglet Objets de l'inventaire propose aussi « Placer dans la ferme ». Après le choix, le panneau se range et la caméra montre toute la ferme (à la souris) ou suit l'aperçu (clavier et manette). Les décorations se posent hors des champs et des parcelles, sans chevaucher le décor ni le personnage. Poser consomme l'objet ; retirer le rend à l'inventaire. Les lanternes s'allument la nuit. Les décorations sont sauvegardées.

## Personnalisation du personnage

L'armoire, dans le coin gauche du fond, s'ouvre avec E / A. L'outil Armoire (6) ouvre aussi le menu depuis l'intérieur.

Les catégories sont Peau, Coiffure, Couleur des cheveux, Yeux, Bouche, Haut, Bas et Chaussures. Haut/bas sélectionne une catégorie, gauche/droite change sa variante. La souris permet aussi de tout choisir. L'aperçu et le personnage utilisent les mêmes cartes illustrées, avec des traits et des vêtements distincts.

Valider (E / A) applique les choix et tente une sauvegarde automatique. Annuler (Échap / B) rétablit l'apparence précédente. Les vêtements sont disponibles gratuitement dans cette version.

## Pause et sauvegarde

Échap depuis le jeu, Start / Options ou le bouton Pause ouvre le menu. Il arrête réellement l'horloge, la pousse, le déplacement et les animations. Start / Options ou Reprendre revient à l'écran précédent. Le menu permet de sauvegarder, charger, restaurer la copie de secours, passer au lendemain ou sauvegarder puis quitter. Charger demande confirmation avant de remplacer la partie en cours.

Le fichier **farm-save.json** est créé dans `Application.persistentDataPath`. Avec les réglages Windows actuels, le dossier est `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Farm Game\`. Le fichier contient les cultures et leur arrosage, la terre, l'inventaire, les pièces, les sélections, l'heure, la position, l'intérieur/extérieur, les meubles, les revêtements et le personnage.

La sauvegarde se charge automatiquement au lancement. Une sauvegarde automatique a lieu chaque minute en exploration, à l'entrée/sortie de la maison, après validation du personnage, après décoration, au passage au lendemain et à la fermeture du jeu. La pause suspend ces sauvegardes périodiques. Dans l'éditeur, Sauvegarder et quitter arrête Play.

L'écriture utilise un fichier temporaire puis un remplacement atomique. Une version précédente valide est conservée dans **farm-save.json.bak**. Un fichier incompatible ou endommagé n'est pas appliqué ; au lancement, il suspend la sauvegarde automatique afin de permettre une récupération depuis le menu pause. Une erreur d'écriture est affichée et empêche l'action Sauvegarder et quitter de fermer le jeu.

## Boucle de jeu et inventaire

### Débloquer de nouvelles parcelles

Le champ de départ comprend 35 cases (x=3 à 9, z=2 à 6). Les contours dorés signalent trois extensions achetables depuis **Parcelles**, une étiquette ou un clic sur leur terrain. Les contours passent au vert après achat.

| Extension | Prix | Cases supplémentaires |
| --- | --- | --- |
| Prairie du sud | 30 pièces | 14 |
| Jardin de l'est | 60 pièces | 8 |
| Lisière du nord | 90 pièces | 12 |

Les achats peuvent se faire dans n'importe quel ordre, avec suffisamment de pièces. Une parcelle ne peut être achetée qu'une fois. Acheter conserve la terre naturelle : il faut ensuite la labourer avec la bêche. On peut alors y planter, arroser et récolter comme sur le champ initial. Les limites de la ferme et les terrains non cultivables restent inchangés. Le bouton Aller à la parcelle permet de la rejoindre après achat.

La propriété des terrains et leur culture sont sauvegardées dans le fichier JSON. Les sauvegardes antérieures restent compatibles et commencent avec les trois extensions verrouillées.

### Cultiver et vendre

- Bêche une case au centre de la ferme, puis choisis une graine dans l'inventaire et plante devant le personnage.
- L'anneau au sol indique la case visée.
- Arrose une fois par journée pour accélérer la croissance de 35 % jusqu'à minuit. Les plantes poussent aussi sans arrosage.
- Récolte une plante mûre avec le panier. Chaque espèce a son propre stock.
- L'inventaire propose **Graines**, **Récoltes** et **Tout**, avec les quantités et les caractéristiques.
- Depuis Récoltes, choisis le produit et la quantité à vendre. Rejoins le marchand en haut à droite, puis utilise le panier, ou valide la vente depuis l'inventaire à proximité.
- Chez le marchand, les onglets permettent d'acheter une espèce de graines ou de vendre une espèce de récoltes avec une quantité choisie.

| Culture | Croissance sans arrosage | Récolte | Prix d'une graine | Vente par produit |
| --- | --- | --- | --- | --- |
| Carotte | 36 h en jeu / 15 min réelles | 2 | 2 pièces | 8 pièces |
| Radis | 24 h en jeu / 10 min réelles | 3 | 1 pièce | 5 pièces |
| Citrouille | 60 h en jeu / 25 min réelles | 1 | 5 pièces | 18 pièces |

On commence avec 45 pièces, 5 graines de carotte, 3 de radis et 2 de citrouille. Trois cultures mûres permettent de récolter immédiatement.

## Établi et fabrication

L'établi se trouve à gauche des champs. Approche-toi puis agis (E / A, ou clic gauche sur l'établi) pour ouvrir le catalogue. Chaque objet indique les récoltes et les pièces nécessaires, avec ce que tu possèdes ; la fabrication n'est possible que si tout est réuni. Une animation de 2,4 secondes suit : coups d'outil sur l'établi, copeaux et ingrédients qui rejoignent l'objet. Les ingrédients ne sont consommés qu'à la fin ; Échap / B interrompt sans perte.

| Objet | Catégorie | Ingrédients | Vente |
| --- | --- | --- | --- |
| Caisse de marché | Objet | 2 radis + 3 pièces | 16 pièces |
| Lanterne citrouille | Décoration | 1 citrouille + 1 radis | 30 pièces |
| Couronne fleurie | Accessoire | 2 carottes + 6 pièces | 28 pièces |
| Engrais maison | Objet | 1 carotte + 2 radis | 22 pièces |
| Chapeau de roseaux | Accessoire | 3 radis + 5 pièces | 26 pièces |
| Épouvantail | Décoration | 2 carottes + 1 citrouille + 6 pièces | 52 pièces |

Les objets fabriqués apparaissent dans l'onglet **Objets** de l'inventaire et se vendent chez le marchand (onglet **Vendre les objets**). Les décorations se posent dans la ferme (voir Décorations de la ferme). Ils sont conservés dans la sauvegarde. Les recettes se règlent dans le tableau Recipes de FarmCrafting.cs. L'établi utilise provisoirement l'illustration de la table ; les accessoires et l'engrais n'ont pas encore d'usage en jeu.

## Temps et état de la terre

**600 secondes réelles = 24 heures en jeu.** Le cycle tourne en continu dans l'inventaire, la décoration et l'armoire. Le menu pause l'arrête. Le soleil, la lumière ambiante, la lune et les lanternes suivent l'heure.

Le bouton Lendemain demande une validation, avance la simulation jusqu'au jour suivant à 08:00 et applique la croissance pendant les heures écoulées.

Une case bêchée revient à son état naturel :

- après **48 heures de jeu sans plante** (20 minutes réelles) ;
- après **trois récoltes** depuis son dernier bêchage.

Une culture en place protège la terre du retour naturel par inactivité. Repasser la bêche sur une case vide renouvelle sa préparation.

## Rendu 2.5D

Le sol est un maillage 3D légèrement vallonné, avec un MeshCollider. Le personnage se déplace dans le plan X/Z grâce à un CharacterController et rencontre les volumes des bâtiments, arbres et de la mare.

Les illustrations 2D sont montées sur des cartes verticales en 3D. Leur orientation suit la caméra ; la profondeur et l'occultation sont celles du monde 3D. La caméra est en perspective, inclinée à 35°, orientable, et suit le personnage. Les sprites compensent le raccourcissement de cette inclinaison pour conserver leurs proportions. Des ombres de contact douces relient les personnages, bâtiments et plantes au sol, qui s’étend au-delà du cadrage. Un shader URP découpe l'alpha, reçoit l'éclairage dynamique et projette des ombres selon les silhouettes.

Le personnage utilise désormais un atlas de pièces peintes : coiffures, visage, trois hauts, trois bas et bottes. Le rig natif aligne la tête, le buste et les jambes, avec mouvement des pieds, balancement et respiration discrète. L'armoire montre les mêmes pièces et proportions que dans le monde. Les choix de peau, cheveux, yeux et chaussures conservent la texture de l'illustration. Cette base n'intègre pas Spine et ne reproduit pas les animations complètes de Cult of the Lamb.

L'intérieur respecte le même rendu 2.5D : mobilier, armoire, coffre, fenêtre et poutres sont des cartes illustrées avec alpha. Les murs et le sol sont des plans avec textures peintes. Des volumes de collision invisibles conservent la profondeur physique. La rotation choisit une vue de face ou de côté, avec miroir pour les orientations opposées ; les silhouettes du mobilier restent verticales. Le tapis tourne dans le plan du sol.

L'éclairage utilise les lumières 3D du renderer Universal URP, adaptées à ce monde 3D. Les graphismes sources restent des illustrations 2D.

## Fichiers et réglages

- Assets/Scripts/FarmSimulation.cs : espèces, inventaire, croissance, vente, horloge, durée de préparation de la terre.
- Assets/Scripts/FarmLand.cs : extensions, prix et limites des terrains cultivables.
- Assets/Scripts/FarmNavigation.cs : calcul et lissage des trajets autour des obstacles.
- Assets/Scripts/FarmWorldMouse.cs : visée à la souris, sélection des illustrations, collisions de navigation et contours des lots.
- Assets/Scripts/FarmPocMouse.cs : clics, déplacement automatique, actions après approche et menu d'achat des parcelles.
- Assets/Scripts/FarmInput.cs : clavier, manette et sélection des secteurs de la roue.
- Assets/Scripts/FarmPoc.cs : boucle de jeu, roue, inventaire, marchand et interface.
- Assets/Scripts/FarmWorld25D.cs : terrain, collisions, caméra, rig, cartes et lumière.
- Assets/Scripts/FarmArt.cs : illustrations et textures générées en code.
- Assets/Scripts/FarmHome.cs : catalogue, grille de placement et choix de personnage.
- Assets/Scripts/FarmInterior25D.cs : pièce, mobilier, collisions et revêtements.
- Assets/Scripts/FarmCharacterArt.cs : composition et teintes des pièces peintes du personnage.
- Assets/Scripts/FarmHomeArt.cs : découpage des atlas de mobilier, revêtements et proportions du personnage.
- Assets/Scripts/FarmPocHome.cs : interactions, décoration, armoire, pause et sauvegarde.
- Assets/Scripts/FarmSave.cs : fichier JSON, validation, écriture atomique et copie de secours.
- Assets/Scripts/FarmCrafting.cs : recettes, fabrication et vente des objets.
- Assets/Scripts/FarmPocCrafting.cs : catalogue de l'établi et animation de l'interface.
- Assets/Scripts/FarmWorldCrafting.cs : établi dans le monde et animation de fabrication.
- Assets/Scripts/FarmPocBuild.cs : menu de construction (catalogue, panneau latéral, pose) pour la maison et la ferme.
- Assets/Scripts/FarmWorldPlacement.cs : décorations posées, aperçu de pose et caméra de construction.
- Assets/Editor/FarmCraftChecks.cs : contrôles de l'établi dans une copie isolée.
- Assets/Editor/FarmHomeChecks.cs : contrôles fonctionnels à exécuter dans une copie isolée du projet.
- Assets/Shaders/PaperCutout.shader : matériau éclairé des cartes.
- Assets/Editor/FarmProjectSetup.cs : configuration URP et shaders inclus dans les builds.
- Assets/Resources/Art/PROMPTS.md : prompts des illustrations originales.
- Assets/Resources/Art/PAPER_REDESIGN_PROMPTS.md : prompts des nouveaux atlas de la maison et du personnage, générés avec l'outil intégré imagegen.
- Assets/Editor/FarmPaperChecks.cs : contrôles des cartes 2D et des rotations, rendus de la refonte.
- Assets/Editor/FarmMouseChecks.cs : contrôles des achats, sauvegardes, trajets et actions souris dans une copie isolée.

Les constantes RealSecondsPerDay, SoilIdleHours et HarvestsPerTilling et le tableau Crops, dans FarmSimulation.cs, permettent d'ajuster le rythme.

## Périmètre

La ferme, la maison et le personnage sont conservés entre les lancements dans un fichier JSON local, avec un seul emplacement de sauvegarde. La maison comprend une pièce et un catalogue gratuit de mobilier ; les achats de meubles, les quêtes, les machines et les animations complètes restent à développer.

Captures de l'interface de pose : output/farm-build-ui-ferme.png et output/farm-build-ui-maison.png.

Aperçus du décor : output/farm-25d-day.png, output/farm-25d-night.png et output/farm-25d-angle.png. Ces captures URP du monde excluent l'interface OnGUI, visible en mode Play.

## Correctif des commandes et du rendu

Le journal de compilation de l’éditeur utilisait ENABLE_LEGACY_INPUT_MANAGER sans ENABLE_INPUT_SYSTEM, malgré le réglage Both enregistré. La première implémentation lisait uniquement Input System : les menus à la souris fonctionnaient, mais pas les commandes clavier/manette. FarmInput prend désormais en charge les deux configurations compilées. Les axes de manette de compatibilité sont enregistrés dans InputManager.asset ; Input System garde les correspondances propres à chaque famille de manette après redémarrage.

Aperçus corrigés du décor : output/farm-25d-fixed.png et output/farm-25d-fixed-angle.png. Le correctif a été compilé dans une copie Unity isolée. La réponse des périphériques physiques reste à confirmer dans l’onglet Game.
