# Correctif — commandes et 2.5D

- Cause observée : la compilation de l’éditeur n’activait que ENABLE_LEGACY_INPUT_MANAGER, tandis que FarmInput interrogeait uniquement Input System.
- Correction : branches explicites pour les deux systèmes, clavier complet et axes/manette Windows en mode legacy.
- Rendu : caméra en perspective à 35°, proportions des sprites compensées, terrain étendu, suivi du personnage et ombres de contact.
- Compilation Unity 6000.3.0f1 : terminée sans erreur C# ni shader.
- Captures du décor excluant l’interface : farm-25d-fixed.png et farm-25d-fixed-angle.png.
- Aucun test avec clavier ou manette physique exécuté par l’assistant.
