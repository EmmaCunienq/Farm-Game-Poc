# Contexte du POC — jeu de ferme 2.5D

## Concept

Jeu de ferme en 2.5D sur Unity : illustrations 2D dans un monde 3D. Références d’ambiance : le rendu papier avec une impression de profondeur de *Cult of the Lamb*, et le ton calme et mignon d’*Animal Crossing* et de *Stardew Valley*.

Le projet est limité à la **version 1**, centrée sur le système de ferme. Le but du POC est d’obtenir un aperçu jouable, de repérer les éléments de gameplay ou de conception qui manquent et d’identifier les assets nécessaires.

## Périmètre souhaité

Maximiser le nombre de boucles jouables, même avec des systèmes simplifiés et des visuels provisoires.

### Boucle de ferme

Obtenir des graines (achat, don, échange ou récompense) → labourer des champs → planter → arroser ou fertiliser (au plus une fois par jour, pour accélérer ou améliorer la pousse) → laisser le temps passer → récolter → éventuellement transformer les produits avec des machines → vendre et recevoir de l’argent.

### Achats et progression

- **Terrain** : acheter une parcelle avec de l’argent et une quête ; la relation peut faire évoluer son prix.
- **Graines** : acheter auprès d’un marchand, les faire pousser puis vendre la récolte.
- **Maison** : acheter auprès d’un marchand puis entrer en mode décoration.
- **Personnage** : acheter des éléments auprès d’un marchand ou d’un coiffeur, puis les sélectionner dans une armoire.

### Personnalisation du personnage

Depuis l’armoire, ouvrir les catégories (cheveux, yeux, bouche, haut, bas, chaussures), choisir un élément, l’appliquer au personnage (en remplaçant l’ancien selon la catégorie), puis valider.

### Décoration de la maison

Depuis un coffre, choisir une catégorie (extérieur, toit, fenêtres et portes, murs intérieurs, mobilier, électricité, etc.), sélectionner un élément, le placer sur une grille, puis recommencer ou valider.

## Choix déjà faits

- **Plateforme du POC** : PC.
- **Contenu** : inclure aussi des versions simples de la personnalisation du personnage et de la décoration de la maison.
- **Visuels** : illustrations originales en papier, sol 3D, caméra en perspective et éclairage dynamique URP.
- **Contrôles** : clavier et manette.
- **Caméra** : caméra qui suit le personnage, choix proposé pour l’exploration de la ferme.
- **Version Unity** : 6000.3.0f1 (Unity 6.3), URP 17.3.0, Input System 1.14.2.

## État du POC

Le workspace est accessible et les fichiers du POC sont intégrés au projet existant. Le prototype comprend déplacement 3D à la manette ou au clavier, roue d’outils, inventaire, achat et vente par espèce et quantité, carottes/radis/citrouilles, arrosage, croissance continue, retour de la terre naturelle après 48 heures sans plante ou trois récoltes, cycle jour/nuit de dix minutes réelles et passage manuel au lendemain.

La nouvelle direction utilise des cartes illustrées verticales sur un terrain 3D avec collisions, caméra en perspective orientable, rig natif simple du fermier et éclairage URP dynamique. Les réglages d’entrée nécessitent un redémarrage de Unity après cette mise à jour.

Le détail des commandes et les limites du prototype sont dans POC_LISEZMOI.md. Les rendus du décor et le bilan des contrôles Unity de cette version sont dans output. La manette a été simulée via Input System ; aucun contrôle sur matériel physique n’a été effectué.

## Correction après retour utilisateur

Le blocage des commandes venait de la configuration d’entrée active dans l’éditeur : compilation legacy uniquement alors que le code lisait Input System. Un chemin clavier et manette Windows/XInput compatible Input Manager est ajouté. Les sprites compensent désormais l’inclinaison de la caméra pour garder leurs proportions, le terrain dépasse le cadrage, la caméra suit davantage le personnage et les éléments ont des ombres de contact. Les correctifs sont dans les scripts du projet ; arrêter puis relancer Play permet leur initialisation.
