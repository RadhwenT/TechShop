# TechShop — Gestion de boutique de matériel informatique

Application web **ASP.NET Core MVC (.NET 10)** de gestion d'un magasin de matériel tech : catalogue de produits, stock, ventes et facturation conforme aux règles tunisiennes (TVA 19 % et timbre fiscal).

> Projet réalisé en 2e année de Licence Génie Logiciel et Systèmes d'Information — Institut International de Technologie (IIT, Sfax).

![.NET](https://img.shields.io/badge/.NET-10-512BD4) ![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC-blue) ![EF Core](https://img.shields.io/badge/EF_Core-10-6DB33F) ![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927)

---

## Fonctionnalités

| Module | Fonctionnalités |
| --- | --- |
| **Produits** | Catalogue avec recherche et filtres (marque, catégorie), fiche détaillée, upload d'image, référence unique, suppression logique et restauration |
| **Stock** | Entrées et sorties de stock par produit, contrôle du stock insuffisant |
| **Marques / Catégories** | CRUD complet, recherche, contrôle des doublons, corbeille avec restauration |
| **Ventes** | Création d'une vente multi-produits avec décrément automatique du stock, prix figé au moment de la vente, filtres par date (année / mois / jour) et par statut |
| **Facturation** | Facture liée à une vente (relation 1-1), numérotation automatique `F-AAAA-NNN`, client physique ou moral (matricule fiscal obligatoire), calcul HT, TVA 19 %, timbre 1 DT et TTC, affichage de la facture |

## Architecture technique

- **ASP.NET Core MVC** : contrôleurs, vues Razor, view models
- **Deux approches d'accès aux données**, utilisées volontairement :
  - **Entity Framework Core** (code-first, migrations) pour les produits, ventes, lignes de vente et factures ;
  - **ADO.NET** (`SqlConnection`, requêtes paramétrées) via le pattern **DAO** pour les marques et catégories.
- **SQL Server** (LocalDB par défaut)
- Règles métier dans le modèle : `Produit.AjouterStock()` / `RetirerStock()` empêchent un stock négatif
- Interface **Bootstrap** avec barre latérale

```
PROJET/
├── Controllers/        # Produit, Marque, Categorie, Vente
├── Models/             # entités, DAO (ADO.NET), Data (DbContext, DbInitializer), ViewModels
├── Migrations/         # migrations EF Core
├── Views/              # vues Razor
└── wwwroot/            # CSS, JS, bibliothèques, images de produits
```

## Lancer le projet

### Prérequis

- **SDK .NET 10**
- **VS Code** avec l'extension **C# Dev Kit** (proposée automatiquement à l'ouverture du dossier)
- Une base **SQL Server** :
  - **SQL Server LocalDB**, installé avec Visual Studio ou SQL Server Express (Windows) ;
  - ou **Docker**, quel que soit le système.

Au premier lancement, l'application **crée automatiquement la base** et ajoute quelques marques et catégories de démonstration.

### Option 1 — LocalDB (Windows)

Dans VS Code, ouvrez l'onglet *Run and Debug* et lancez **TechShop (LocalDB)**. Ou en ligne de commande :

```bash
cd PROJET
dotnet run
```

L'application s'ouvre sur http://localhost:5000.

### Option 2 — SQL Server dans Docker

```bash
docker compose up -d
```

Lancez ensuite **TechShop (SQL Server Docker)** dans VS Code, ou :

```bash
cd PROJET
dotnet run --launch-profile docker
```

La chaîne de connexion peut aussi être fournie par la variable d'environnement `ConnectionStrings__cnx`.

## Auteur

**Radhwen Trabelsi** — étudiant en Génie Logiciel à l'IIT · [LinkedIn](https://www.linkedin.com/in/radhwen-trabelsi) · [GitHub](https://github.com/RadhwenT)
