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
| Passer au lendemain | Start / Options, puis A / Croix | N, puis E, ou bouton Lendemain |
| Tourner la caméra | Maintenir LT / L2 + stick droit | Maintenir Alt + C/V |
| Outils directs | Roue d'outils | 1 à 6 |

Roue : Bêche, Graines, Arrosoir, Panier, Maison, Armoire. Dans les menus, le mouvement du personnage est bloqué.

## Boucle de jeu et inventaire

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

## Temps et état de la terre

**600 secondes réelles = 24 heures en jeu.** Le cycle tourne en continu, y compris pendant l'ouverture de l'inventaire et des autres menus. Le soleil, la lumière ambiante, la lune et les lanternes suivent l'heure.

Le bouton Lendemain demande une validation, avance la simulation jusqu'au jour suivant à 08:00 et applique la croissance pendant les heures écoulées.

Une case bêchée revient à son état naturel :

- après **48 heures de jeu sans plante** (20 minutes réelles) ;
- après **trois récoltes** depuis son dernier bêchage.

Une culture en place protège la terre du retour naturel par inactivité. Repasser la bêche sur une case vide renouvelle sa préparation.

## Rendu 2.5D

Le sol est un maillage 3D légèrement vallonné, avec un MeshCollider. Le personnage se déplace dans le plan X/Z grâce à un CharacterController et rencontre les volumes des bâtiments, arbres et de la mare.

Les illustrations 2D sont montées sur des cartes verticales en 3D. Leur orientation suit la caméra ; la profondeur et l'occultation sont celles du monde 3D. La caméra est en perspective, inclinée à 35°, orientable, et suit le personnage. Les sprites compensent le raccourcissement de cette inclinaison pour conserver leurs proportions. Des ombres de contact douces relient les personnages, bâtiments et plantes au sol, qui s’étend au-delà du cadrage. Un shader URP découpe l'alpha, reçoit l'éclairage dynamique et projette des ombres selon les silhouettes.

Le personnage possède un petit rig natif composé de la tête, du buste et des deux jambes, animé à la marche et pendant les actions. Cette base s'inspire du principe « illustrations 2D dans un monde 3D » demandé. Elle n'intègre pas Spine et ne reproduit pas les animations ou la direction artistique complète de Cult of the Lamb.

L'éclairage utilise les lumières 3D du renderer Universal URP, adaptées à ce monde 3D. Les graphismes sources restent des illustrations 2D.

## Fichiers et réglages

- Assets/Scripts/FarmSimulation.cs : espèces, inventaire, croissance, vente, horloge, durée de préparation de la terre.
- Assets/Scripts/FarmInput.cs : clavier, manette et sélection des secteurs de la roue.
- Assets/Scripts/FarmPoc.cs : boucle de jeu, roue, inventaire, marchand et interface.
- Assets/Scripts/FarmWorld25D.cs : terrain, collisions, caméra, rig, cartes et lumière.
- Assets/Scripts/FarmArt.cs : illustrations et textures générées en code.
- Assets/Shaders/PaperCutout.shader : matériau éclairé des cartes.
- Assets/Editor/FarmProjectSetup.cs : configuration URP et shaders inclus dans les builds.
- Assets/Resources/Art/PROMPTS.md : prompts des illustrations originales.

Les constantes RealSecondsPerDay, SoilIdleHours et HarvestsPerTilling et le tableau Crops, dans FarmSimulation.cs, permettent d'ajuster le rythme.

## Périmètre

L'inventaire et la ferme sont conservés pendant la session. La sauvegarde entre les lancements, les quêtes, les machines et les animations complètes restent à développer. Maison et Armoire proposent des teintes.

Aperçus du décor : output/farm-25d-day.png, output/farm-25d-night.png et output/farm-25d-angle.png. Ces captures URP du monde excluent l'interface OnGUI, visible en mode Play.

## Correctif des commandes et du rendu

Le journal de compilation de l’éditeur utilisait ENABLE_LEGACY_INPUT_MANAGER sans ENABLE_INPUT_SYSTEM, malgré le réglage Both enregistré. La première implémentation lisait uniquement Input System : les menus à la souris fonctionnaient, mais pas les commandes clavier/manette. FarmInput prend désormais en charge les deux configurations compilées. Les axes de manette de compatibilité sont enregistrés dans InputManager.asset ; Input System garde les correspondances propres à chaque famille de manette après redémarrage.

Aperçus corrigés du décor : output/farm-25d-fixed.png et output/farm-25d-fixed-angle.png. Le correctif a été compilé dans une copie Unity isolée. La réponse des périphériques physiques reste à confirmer dans l’onglet Game.
