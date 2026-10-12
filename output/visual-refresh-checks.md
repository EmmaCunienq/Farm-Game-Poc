# Contrôle du nouveau rendu

Vérification avec Unity 6000.3.0f1 dans une copie isolée du projet.

- Scripts du jeu et importeur : compilation réussie.
- Atlas : RGBA 1254 × 1254, transparence réelle ; neuf sujets découpés selon leurs bornes.
- Rendu Unity : 138 SpriteRenderer, caméra orthographique, capture `visual-refresh.png`.
- Boucle vérifiée : récolte d'une culture mûre, vente pour huit pièces, achat de cinq graines pour dix pièces, passage au jour suivant.
- Le rendu de contrôle montre le décor sans l'interface OnGUI, qui s'affiche en mode Play.
- Contrôle terminé sans erreur de compilation ou de rendu ; Unity sort avec le code 0.

Logs complets : `tmp/art-check/unity-preview.log`.
