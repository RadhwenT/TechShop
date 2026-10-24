# TechShop — Gestion de boutique de matériel informatique

Application web **ASP.NET Core MVC (.NET 10)** de gestion d'un magasin de matériel tech : catalogue de produits, stock, ventes et facturation conforme aux règles tunisiennes (TVA 19 % et timbre fiscal).

> Projet réalisé en 2e année de Licence Génie Logiciel et Systèmes d'Information — Institut International de Technologie (IIT, Sfax).

[![CI](https://github.com/RadhwenT/TechShop/actions/workflows/ci.yml/badge.svg)](https://github.com/RadhwenT/TechShop/actions/workflows/ci.yml) ![.NET](https://img.shields.io/badge/.NET-10-512BD4) ![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC-blue) ![EF Core](https://img.shields.io/badge/EF_Core-10-6DB33F) ![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927)

---

## Aperçu

![Facture générée : client moral, TVA 19 %, timbre fiscal et total TTC](docs/captures/facture-totaux.jpg)

| Catalogue des produits | Fiche produit |
| --- | --- |
| ![Liste des produits](docs/captures/produits.jpg) | ![Fiche produit](docs/captures/produit-details.jpg) |
| **Ventes et statut de facturation** | **En-tête de facture** |
| ![Liste des ventes](docs/captures/ventes.jpg) | ![Facture](docs/captures/facture.jpg) |

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
- Règles métier dans le modèle : `Produit.AjouterStock()` / `RetirerStock()` empêchent un stock négatif ; `CalculFacture` centralise le calcul HT / TVA / timbre / TTC et la numérotation
- Interface **Bootstrap** avec barre latérale

```
TechShop/
├── Controllers/        # Produit, Marque, Categorie, Vente
├── Models/             # entités, CalculFacture, DAO (ADO.NET), Data (DbContext, DbInitializer), ViewModels
├── Migrations/         # migrations EF Core
├── Views/              # vues Razor
└── wwwroot/            # CSS, JS, bibliothèques, images de produits
TechShop.Tests/         # tests xUnit (unitaires + intégration sur SQL Server)
```

## Tests

**28 tests xUnit**, exécutés à chaque push par GitHub Actions :

- **unitaires** : règles de stock (`Produit`), calcul de facture (HT, TVA 19 % arrondie au millime, timbre, TTC) et format du numéro `F-AAAA-NNN` ;
- **intégration sur un vrai SQL Server** lancé dans Docker par **Testcontainers** : DAO ADO.NET (recherche, doublons insensibles à la casse, suppression logique et restauration, requêtes paramétrées), ventes (décrément du stock, prix figé, refus du stock insuffisant) et factures (montants, numérotation, matricule fiscal obligatoire pour un client moral, une seule facture par vente, vente annulée non facturable).

```bash
dotnet test     # Docker doit être démarré
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
cd TechShop
dotnet run
```

L'application s'ouvre sur http://localhost:5000.

### Option 2 — SQL Server dans Docker

```bash
docker compose up -d
```

Lancez ensuite **TechShop (SQL Server Docker)** dans VS Code, ou :

```bash
cd TechShop
dotnet run --launch-profile docker
```

La chaîne de connexion peut aussi être fournie par la variable d'environnement `ConnectionStrings__cnx`.

## Auteur

**Radhwen Trabelsi** — étudiant en Génie Logiciel à l'IIT · [LinkedIn](https://www.linkedin.com/in/radhwen-trabelsi) · [GitHub](https://github.com/RadhwenT)
