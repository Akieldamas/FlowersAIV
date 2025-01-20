# Application d'Identification et Description de Fleurs

## Description du Projet
 Ce projet a été réalisé dans le cadre d’un challenge d’intelligence artificielle dans mon école. Les groupes avaient pour objectif de créer une application intégrant l’IA de n’importe quelle manière, et ce, en une semaine (4-5 jours).

L’application a été développée avec MAUI en C# et utilise la librairie AForge pour la gestion de la caméra. Cependant, comme AForge est compatible uniquement avec Windows, la version Android de l’application, que nous avons également testée, permet uniquement l’identification des fleurs via une image déjà uploadée, faute de temps pour intégrer la caméra.

Initialement, nous avions prévu d’installer un serveur pour déployer Llama sur Microsoft Azure, mais cela s’est révélé trop coûteux et pas assez performant. L’utilisation de l’API d’OpenAI aurait été une meilleure solution pour ce projet, bien que limitée par les crédits disponibles.

## Fonctionnalités

- Identification d’une fleur via une image uploadée (Windows et Android).
- Analyse en temps réel avec une caméra (Windows uniquement).
- Génération d’une description de la fleur basée sur un modèle d’intelligence artificielle.

## Architecture Technique

- **Frontend** : Développé avec MAUI en C#.
- **Backend** : Utilisation de ML.NET pour les fonctionnalités de reconnaissance et intégration de l’API Ollama pour générer des descriptions.

- **Compatibilité** :
    - Windows : Caméra et images uploadées.
    - Android : Uniquement via des images.

- **Hébergement initialement prévu** :
    - Local pour les tests.
    - Azure pour un déploiement distant (Non accompli).

## Crédits

- Technologies utilisées : MAUI, AForge, ML.NET, Ollama, Microsoft Azure.
