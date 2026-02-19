# Visual Studio Solution * Finder *

**Finder** est une application CLI (Command Line Interface) permettant de rechercher, lister et ouvrir des solutions Visual Studio (`.sln` ou `.slnx`) à partir d'un chemin racine configurable. 
Elle inclut également des fonctionnalités de gestion de cache pour accélérer les recherches.

---

## 🚀 Fonctionnalités

- **Recherche rapide** : Recherchez des solutions Visual Studio (sln ou slnx) par nom ou masque.
- **Ouverture directe** : Ouvrez une solution dans Visual Studio ou accédez au dossier contenant la solution.
- **Gestion de cache** : Accélère les recherches en utilisant un cache local.
- **Configuration personnalisée** : Définissez un chemin racine pour vos recherches.
- **Interface utilisateur interactive** : Sélectionnez facilement une solution ou une action via un menu interactif.

---

## 🛠️ Installation

1. Clonez le dépôt
2. Compilez le projet
3. Exécutez l'application


---

## 📖 Utilisation

### Commandes disponibles

| Commande                          | Description                                      | Exemple                                   |
|-----------------------------------|--------------------------------------------------|-------------------------------------------|
| `finder.exe <mask>`               | Recherche et ouvre une solution Visual Studio.   | `finder.exe MonProjet`                    |
| `finder.exe refresh`              | Reconstruit le cache des solutions.              | `finder.exe refresh`                      |
| `finder.exe config [chemin]`      | Configure ou affiche le chemin racine.           | `finder.exe config "D:\sources\Projets"` |

---

### Exemple d'utilisation

#### Rechercher une solution

- Affiche une liste des solutions correspondant au masque `MonProjet`.
- Permet de sélectionner une solution ou d'ouvrir le dossier contenant la solution.

#### Reconstruire le cache

- Effectue un scan complet du chemin racine et met à jour le cache.

#### Configurer le chemin racine

- Définit le chemin racine pour les recherches.
- Propose de lancer un scan complet après configuration.

---

## 🗂️ Structure du projet

- **`FindSolutionCommand.cs`** : Commande principale pour rechercher et ouvrir des solutions.
- **`RefreshCacheCommand.cs`** : Commande pour reconstruire le cache.
- **`ConfigCommand.cs`** : Commande pour configurer le chemin racine.
- **`CacheManager.cs`** : Gestion du cache des solutions.
- **`appsettings.json`** : Fichier de configuration pour le chemin racine.

---

## 🛡️ Licence

Ce projet est sous licence [MIT](LICENSE).

---

## 🤝 Contribuer

Les contributions sont les bienvenues ! N'hésitez pas à ouvrir une issue ou une pull request pour proposer des améliorations.

---

## 📧 Contact

Créé par [Mickaël François](https://github.com/mickaelfrancois). Pour toute question, contactez-moi via GitHub.