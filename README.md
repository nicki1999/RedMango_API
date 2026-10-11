# 🥭 RedMango API

A RESTful backend for **RedMango**, an online restaurant ordering application. Built with **ASP.NET Core 9 Web API**, **Entity Framework Core**, **SQL Server**, **ASP.NET Identity** with **JWT authentication**, and **Azure Blob Storage** for menu item images.

Users can register, log in, browse the menu and manage a shopping cart. Admins can create, update and delete menu items, including uploading dish images.

> This repository contains the **backend API only**. A React frontend consumes this API; until it is connected, the API can be explored and tested through the built-in **Swagger UI**.

---

## 📑 Table of Contents

- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Project Structure](#-project-structure)
- [Architecture & Request Flow](#-architecture--request-flow)
- [Database Schema](#-database-schema)
- [Prerequisites](#-prerequisites)
- [Getting Started](#-getting-started)
  - [1. Clone the repository](#1-clone-the-repository)
  - [2. Install the .NET 9 SDK](#2-install-the-net-9-sdk)
  - [3. Install the EF Core CLI](#3-install-the-ef-core-cli)
  - [4. Start SQL Server](#4-start-sql-server)
  - [5. (Optional) Start Azure Storage emulator](#5-optional-start-the-azure-storage-emulator)
  - [6. Configure appsettings.json](#6-configure-appsettingsjson)
  - [7. Create the database](#7-create-the-database)
  - [8. Run the API](#8-run-the-api)
- [What to Expect](#-what-to-expect)
- [API Reference](#-api-reference)
- [Testing with Swagger (Walkthrough)](#-testing-with-swagger-walkthrough)
- [Response Format](#-response-format)
- [Authentication & Roles](#-authentication--roles)
- [Seed Data](#-seed-data)
- [Running It Again Later](#-running-it-again-later)
- [Troubleshooting](#-troubleshooting)
- [Known Limitations & Roadmap](#-known-limitations--roadmap)
- [Acknowledgements](#-acknowledgements)

---

## ✨ Features

- **User registration & login** using ASP.NET Identity with hashed passwords
- **JWT authentication** with tokens valid for 7 days, carrying the user's id, name, email and role
- **Role-based authorization** with `admin` and `customer` roles, created automatically on first registration
- **Menu management (CRUD)** with image upload to **Azure Blob Storage**
- **Shopping cart** with a single endpoint to add items, change quantities and remove items
- **Database migrations & seeding** with 10 menu items pre-loaded
- **Consistent API response wrapper** (`ApiResponse`) on every endpoint
- **Swagger UI** with JWT "Authorize" support for interactive testing
- **CORS enabled** so a separate frontend (e.g. React) can call the API

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 9 Web API (`net9.0`) |
| ORM | Entity Framework Core 9 (SQL Server provider) |
| Database | Microsoft SQL Server (2019 / 2022, LocalDB, or Docker) |
| Auth | ASP.NET Core Identity + JWT Bearer tokens |
| File storage | Azure Blob Storage (`Azure.Storage.Blobs`) |
| API docs | Swashbuckle / Swagger UI |
| Language | C# |

---

## 📁 Project Structure

```
RedMango_API/
├── RedMango_API.sln                     # Solution file
└── RedMango_API/
    ├── Program.cs                       # App startup: services, auth, Swagger, middleware
    ├── appsettings.json                 # Connection strings & JWT secret
    ├── appsettings.Development.json
    ├── Properties/
    │   └── launchSettings.json          # Ports & launch profiles
    ├── Controllers/
    │   ├── AuthController.cs            # Register & login (issues JWTs)
    │   ├── AuthTestController.cs        # Endpoints to test authentication / admin role
    │   ├── MenuItemController.cs        # Menu CRUD + image upload
    │   ├── ShoppingCartController.cs    # Get cart, add / update / remove items
    │   └── WeatherForecastController.cs # Default template sample
    ├── Data/
    │   └── ApplicationDbContext.cs      # EF Core DbContext + menu seed data
    ├── Model/
    │   ├── ApiResponse.cs               # Standard response wrapper
    │   ├── ApplicationUser.cs           # Identity user with a Name field
    │   ├── MenuItem.cs
    │   ├── ShoppingCart.cs
    │   ├── CartItem.cs
    │   └── Dto/                         # Request / response data transfer objects
    │       ├── LoginRequestDto.cs
    │       ├── LoginResponseDto.cs
    │       ├── RegisterRequestDto.cs
    │       ├── MenuItemCreateDto.cs
    │       └── MenuItemUpdateDto.cs
    ├── Services/
    │   ├── IBlobService.cs              # Blob storage abstraction
    │   └── BlobService.cs               # Upload / get / delete images in Azure Blob Storage
    ├── Utility/
    │   └── SD.cs                        # Static details: role names, storage container name
    └── Migrations/                      # EF Core migrations history
```

---

## 🔄 Architecture & Request Flow

### Application startup (`Program.cs`)

1. **Database:** registers `ApplicationDbContext` with SQL Server using the `DefaultDbConnection` connection string.
2. **Blob storage:** registers a `BlobServiceClient` (from the `StorageAccount` connection string) and `BlobService`.
3. **Identity:** registers ASP.NET Identity for `ApplicationUser` with relaxed password rules (any password of length ≥ 1).
4. **JWT:** configures JWT Bearer authentication, validating tokens signed with `ApiSettings:Secret`.
5. **Swagger:** configures Swagger with a Bearer security definition, so tokens can be entered in the UI.
6. **Middleware pipeline:**
   `HTTPS redirection → CORS (any origin / header / method) → Authentication → Authorization → Controllers`

### Typical user journey

```
 ┌──────────┐   POST /api/auth/register    ┌──────────────┐
 │  Client  │ ───────────────────────────▶ │ AuthController│──▶ Identity creates user + role
 │ (Swagger │   POST /api/auth/login       │              │──▶ returns JWT token
 │ / React) │ ───────────────────────────▶ └──────────────┘
 │          │
 │          │   GET /api/MenuItem          ┌──────────────────┐
 │          │ ───────────────────────────▶ │MenuItemController│──▶ SQL Server (MenuItems)
 │          │   POST/PUT/DELETE (admin)    │                  │──▶ Azure Blob (images)
 │          │ ───────────────────────────▶ └──────────────────┘
 │          │
 │          │   POST /api/ShoppingCart     ┌──────────────────────┐
 │          │ ───────────────────────────▶ │ShoppingCartController│──▶ SQL Server
 │          │   GET  /api/ShoppingCart     │                      │    (ShoppingCarts, CartItems)
 └──────────┘ ───────────────────────────▶ └──────────────────────┘
```

1. **Register:** the user signs up and is assigned the `admin` or `customer` role.
2. **Log in:** the API verifies the credentials and returns a signed **JWT**.
3. **Browse the menu:** the client fetches all menu items (name, price, category, image URL).
4. **Add to cart:** the client sends `userId`, `menuItemId` and a quantity change; the API creates or updates the cart.
5. **View the cart:** the client fetches the user's cart with its items and menu item details.
6. **Admin:** an admin creates, edits and deletes menu items; images are uploaded to Azure Blob Storage and their public URLs are stored in the database.

---

## 🗄 Database Schema

| Table | Description | Key columns |
|---|---|---|
| `AspNetUsers` | Users (via Identity) | `Id`, `UserName`, `Email`, `Name`, `PasswordHash` |
| `AspNetRoles` / `AspNetUserRoles` | Roles and user–role links | `admin`, `customer` |
| `MenuItems` | Dishes on the menu | `Id`, `Name`, `Description`, `SpecialTag`, `Category`, `Price`, `Image` |
| `ShoppingCarts` | One cart per user | `Id`, `UserId` |
| `CartItems` | Lines in a cart | `Id`, `MenuItemId` → `MenuItems`, `Quantity`, `ShoppingCartId` → `ShoppingCarts` |

Relationships:

```
ApplicationUser 1 ──── 1 ShoppingCart 1 ──── * CartItem * ──── 1 MenuItem
```

---

## ✅ Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | **9.0** or later | Required to build and run |
| `dotnet-ef` CLI | 9.0+ | To apply database migrations |
| SQL Server | 2019 / 2022 | Windows: SQL Server Express or LocalDB. macOS / Linux: Docker |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest | Only needed on macOS / Linux (or if you prefer containers) |
| Azure Storage account **or** Azurite | — | Only needed for creating, updating and deleting menu items (image upload) |

Optional: Visual Studio 2022, VS Code with the C# Dev Kit, or JetBrains Rider.

---

## 🚀 Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/nicki1999/RedMango_API.git
cd RedMango_API/RedMango_API
```

### 2. Install the .NET 9 SDK

Download it from https://dotnet.microsoft.com/download/dotnet/9.0, then verify:

```bash
dotnet --list-sdks
# Expect a line starting with 9.0.x
```

### 3. Install the EF Core CLI

```bash
dotnet tool install --global dotnet-ef
dotnet ef --version
```

> If `dotnet ef` is not found, open a new terminal, or add `~/.dotnet/tools` to your `PATH`.

### 4. Start SQL Server

<details>
<summary><b>macOS / Linux (Docker)</b></summary>

1. Install and open **Docker Desktop**.
2. **Apple Silicon (M1/M2/M3/M4) only:** in Docker Desktop, go to *Settings → General* and enable **"Use Rosetta for x86_64/amd64 emulation on Apple Silicon"**, then *Apply & restart*.
3. Start a SQL Server container:

   ```bash
   docker run -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=YourStrong!Passw0rd' \
     -p 1433:1433 --name redmango-sql -d mcr.microsoft.com/mssql/server:2022-latest
   ```

   > Use **single quotes** around the password. In zsh/bash, `!` inside double quotes triggers history expansion (`event not found`).

4. Verify that it is running:

   ```bash
   docker ps
   # redmango-sql should show STATUS "Up ..."
   ```

   Give SQL Server 10–20 seconds to finish starting before the next steps.

> If the terminal says `docker: command not found` even though Docker Desktop is installed, add its CLI to your PATH:
> ```bash
> echo 'export PATH="$HOME/.docker/bin:$PATH"' >> ~/.zshrc && source ~/.zshrc
> ```

</details>

<details>
<summary><b>Windows</b></summary>

Use any of the following:

- **SQL Server Express / Developer edition**, installed locally
- **LocalDB**, which ships with Visual Studio
- **Docker Desktop**, using the same `docker run` command as above (with double quotes in PowerShell)

</details>

### 5. (Optional) Start the Azure Storage emulator

Image upload (creating, updating and deleting menu items) requires blob storage. Choose one:

- **Real Azure:** create a Storage Account in the Azure Portal, add a container named **`redmango`** with *Blob* (anonymous read) access, and copy the account's connection string.
- **Local emulator (Azurite):**

  ```bash
  docker run -d -p 10000:10000 --name redmango-azurite \
    mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0
  ```

  Then create a container named `redmango` (for example with [Azure Storage Explorer](https://azure.microsoft.com/products/storage/storage-explorer/)).

> You can skip this step. Everything except creating, updating and deleting menu items works without blob storage.

### 6. Configure `appsettings.json`

Open `RedMango_API/appsettings.json` and set the connection strings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultDbConnection": "Server=localhost,1433;Database=RedMango;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True",
    "StorageAccount": "UseDevelopmentStorage=true"
  },
  "ApiSettings": {
    "Secret": "This is my custom Secret key for authentication"
  }
}
```

| Key | What to put there |
|---|---|
| `DefaultDbConnection` | Your SQL Server connection string. The example matches the Docker container from step 4. |
| `StorageAccount` | `UseDevelopmentStorage=true` for Azurite, or your Azure Storage connection string. |
| `ApiSettings:Secret` | The key used to sign JWTs. Use a long, random value of at least 32 characters. |

**Other SQL Server setups:**

```text
# LocalDB (Windows)
Server=(localdb)\\MSSQLLocalDB;Database=RedMango;Trusted_Connection=True;TrustServerCertificate=True

# Local SQL Server with Windows authentication
Server=localhost;Database=RedMango;Trusted_Connection=True;TrustServerCertificate=True
```

> ⚠️ **Do not commit real passwords or connection strings.** For local secrets, consider [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):
> ```bash
> dotnet user-secrets init
> dotnet user-secrets set "ConnectionStrings:DefaultDbConnection" "<your connection string>"
> dotnet user-secrets set "ConnectionStrings:StorageAccount" "<your storage connection string>"
> ```

### 7. Create the database

From the `RedMango_API/RedMango_API` folder:

```bash
dotnet restore
dotnet ef database update
```

**Expected result:** EF Core logs a series of `CREATE TABLE` / `INSERT` commands and ends with:

```
Done.
```

The `RedMango` database now contains all tables plus 10 seeded menu items.

### 8. Run the API

```bash
dotnet run --launch-profile http
```

or, with HTTPS:

```bash
dotnet dev-certs https --trust   # first time only
dotnet run --launch-profile https
```

**Expected console output:**

```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5122
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

Leave this terminal open. Press `Ctrl+C` to stop the API.

---

## 👀 What to Expect

| URL | What you will see |
|---|---|
| http://localhost:5122/swagger | **Swagger UI**, an interactive page listing every endpoint |
| https://localhost:7267/swagger | Same, over HTTPS (when using the `https` profile) |
| http://localhost:5122/api/MenuItem | Raw JSON with the 10 seeded menu items |

- There is **no visual storefront** in this repository. The API returns JSON, intended to be consumed by a frontend (React).
- Swagger is only enabled when `ASPNETCORE_ENVIRONMENT=Development`, which both launch profiles set by default.
- Every response uses the [standard response format](#-response-format).

---

## 📚 API Reference

Base URL: `http://localhost:5122`

### Auth: `/api/auth`

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Create a new user |
| `POST` | `/api/auth/login` | Public | Log in and receive a JWT |

**Register: request body**

```json
{
  "userName": "jane@example.com",
  "name": "Jane Doe",
  "password": "123",
  "role": "customer"
}
```

- `role` should be `"admin"` or `"customer"`. Any value other than `admin` is treated as `customer`.
- The email is set to the `userName`.
- Returns `400` with `"User already exists"` if the username is taken.

**Login: request body**

```json
{
  "userName": "jane@example.com",
  "password": "123"
}
```

**Login: response**

```json
{
  "statusCode": 200,
  "isSuccess": true,
  "errorMessages": [],
  "result": {
    "email": "jane@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

The token contains these claims: `fullName`, `id` (the user id, needed for the cart), `email` and `role`. You can decode it at [jwt.io](https://jwt.io) to read the `id`.

---

### Menu items: `/api/MenuItem`

| Method | Endpoint | Body | Description |
|---|---|---|---|
| `GET` | `/api/MenuItem` | — | List all menu items |
| `GET` | `/api/MenuItem/id:int?id={id}` | — | Get one menu item |
| `POST` | `/api/MenuItem` | `multipart/form-data` | Create a menu item with an image |
| `PUT` | `/api/MenuItem/id:int?id={id}` | `multipart/form-data` | Update a menu item (optionally replace the image) |
| `DELETE` | `/api/MenuItem/id:int?id={id}` | — | Delete a menu item and its image |

> ℹ️ The single-item routes are currently declared as `"id:int"` rather than `"{id:int}"`, so the id is passed as a **query string** parameter. See [Known Limitations](#-known-limitations--roadmap).

**Create / update form fields**

| Field | Type | Required | Notes |
|---|---|---|---|
| `Id` | int | Update only | Must match the `id` in the URL |
| `Name` | string | ✅ | |
| `Description` | string | | |
| `SpecialTag` | string | | e.g. `Best Seller`, `Top Rated`, `Chef's Special` |
| `Category` | string | | e.g. `Appetizer`, `Entrée`, `Dessert` |
| `Price` | number | ✅ | Must be ≥ 1 |
| `File` | image file | ✅ | Uploaded to the `redmango` blob container |

**Menu item object**

```json
{
  "id": 1,
  "name": "Spring Roll",
  "description": "Fusc tincidunt maximus leo, sed scelerisque massa auctor sit amet...",
  "specialTag": "",
  "category": "Appetizer",
  "price": 7.99,
  "image": "https://<storage-account>.blob.core.windows.net/redmango/spring roll.jpg"
}
```

---

### Shopping cart: `/api/ShoppingCart`

| Method | Endpoint | Query parameters | Description |
|---|---|---|---|
| `GET` | `/api/ShoppingCart` | `userId` | Get the user's cart with its items |
| `POST` | `/api/ShoppingCart` | `userId`, `menuItemId`, `updateQuantity` | Add, update or remove an item |

**How `POST` behaves:**

| Situation | Result |
|---|---|
| User has no cart and `updateQuantity > 0` | A new cart is created and the item is added |
| Cart exists, item not in cart | Item is added with `updateQuantity` |
| Item already in cart | `updateQuantity` is **added** to the current quantity (use `1` / `-1` for + / −) |
| `updateQuantity = 0`, or the new quantity ≤ 0 | Item is removed from the cart |
| Last item removed | The whole cart is deleted |

Example: add two Spring Rolls for a user:

```
POST /api/ShoppingCart?userId=<user-id-from-token>&menuItemId=1&updateQuantity=2
```

---

### Auth test: `/api/AuthTest`

| Method | Endpoint | Requires | Response |
|---|---|---|---|
| `GET` | `/api/AuthTest` | Any logged-in user | `"You are Authorized"` |
| `GET` | `/api/AuthTest/id:int` | `admin` role | `"You are Authorized with role admin"` |

Without a token these return `401 Unauthorized`; with a non-admin token the admin endpoint returns `403 Forbidden`.

---

## 🧪 Testing with Swagger (Walkthrough)

1. Run the API and open **http://localhost:5122/swagger**.
2. **Browse the menu:** expand `GET /api/MenuItem` → **Try it out** → **Execute**. You should see 10 items.
3. **Register an admin:** `POST /api/auth/register`:
   ```json
   { "userName": "admin@redmango.com", "name": "Admin", "password": "admin", "role": "admin" }
   ```
   Expect `200` with `"isSuccess": true`.
4. **Log in:** `POST /api/auth/login` with the same username and password. Copy the `token` from the response.
5. **Authorize:** click the **Authorize 🔒** button at the top and enter:
   ```
   Bearer <paste your token here>
   ```
   Click **Authorize**, then **Close**.
6. **Check authentication:** run `GET /api/AuthTest` and `GET /api/AuthTest/id:int`. Both should return "You are Authorized…".
7. **Find your user id:** paste the token into [jwt.io](https://jwt.io) and copy the `id` claim.
8. **Add to cart:** `POST /api/ShoppingCart` with `userId=<id>`, `menuItemId=1`, `updateQuantity=2`.
9. **Change the quantity:** repeat with `updateQuantity=-1` (the quantity becomes 1), then `updateQuantity=0` (the item is removed).
10. **(With blob storage configured) Create a menu item:** `POST /api/MenuItem`, fill in the form fields and choose an image file. Expect `201 Created` and the new item with its image URL.

> You can also use the included `RedMango_API.http` file (VS Code REST Client / Visual Studio), Postman, or `curl`:
> ```bash
> curl http://localhost:5122/api/MenuItem
> ```

---

## 📦 Response Format

Every endpoint wraps its data in an `ApiResponse`:

```json
{
  "statusCode": 200,
  "isSuccess": true,
  "errorMessages": [],
  "result": { }
}
```

| Field | Meaning |
|---|---|
| `statusCode` | HTTP status code (e.g. 200, 201, 400, 404) |
| `isSuccess` | `true` if the operation succeeded |
| `errorMessages` | List of error messages when something went wrong |
| `result` | The returned data (object, list, or empty) |

---

## 🔐 Authentication & Roles

- Passwords are hashed and stored by **ASP.NET Identity**.
- Password rules are intentionally relaxed for development (minimum length 1, no digit, uppercase or symbol required). Tighten them in `Program.cs` before production.
- On the first successful registration, the roles **`admin`** and **`customer`** are created.
- Tokens are signed with **HMAC-SHA256** using `ApiSettings:Secret` and expire after **7 days**.
- Send the token on protected requests as:
  ```
  Authorization: Bearer <token>
  ```
- Role names are defined in `Utility/SD.cs`.

---

## 🍽 Seed Data

The initial migration seeds these menu items:

| # | Name | Category | Price | Special tag |
|---|---|---|---|---|
| 1 | Spring Roll | Appetizer | 7.99 | — |
| 2 | Idli | Appetizer | 8.99 | — |
| 3 | Panu Puri | Appetizer | 8.99 | Best Seller |
| 4 | Hakka Noodles | Entrée | 10.99 | — |
| 5 | Malai Kofta | Entrée | 12.99 | Top Rated |
| 6 | Paneer Pizza | Entrée | 11.99 | — |
| 7 | Paneer Tikka | Entrée | 13.99 | Chef's Special |
| 8 | Carrot Love | Dessert | 4.99 | — |
| 9 | Rasmalai | Dessert | 4.99 | Chef's Special |
| 10 | Sweet Rolls | Dessert | 3.99 | Top Rated |

---

## 🔁 Running It Again Later

Once set up, you only need:

```bash
# 1. Make sure Docker Desktop is open, then start the database container
docker start redmango-sql
# (optional) docker start redmango-azurite

# 2. Run the API
cd RedMango_API/RedMango_API
dotnet run --launch-profile http
```

Open http://localhost:5122/swagger.

**Resetting the database** (deletes all data):

```bash
dotnet ef database drop -f
dotnet ef database update
```

---

## 🩺 Troubleshooting

| Problem | Cause / fix |
|---|---|
| `zsh: event not found: Passw0rd` | `!` inside double quotes. Use single quotes around the password in the `docker run` command. |
| `zsh: command not found: docker` | Docker Desktop is not running or its CLI is not on your `PATH`. Open Docker Desktop and add `~/.docker/bin` to `PATH`. |
| SQL Server container exits immediately | On Apple Silicon, enable Rosetta in Docker Desktop. Check `docker logs redmango-sql`. The password must meet SQL Server's complexity rules. |
| `NETSDK1045: The current .NET SDK does not support targeting .NET 9.0` | Install the .NET 9 SDK. |
| `NETSDK1004: Assets file ... project.assets.json not found` | Run `dotnet restore` before `dotnet ef database update`. |
| `dotnet ef` not found | `dotnet tool install --global dotnet-ef` and make sure `~/.dotnet/tools` is on your `PATH`. |
| `A network-related or instance-specific error occurred` | SQL Server is not running or the connection string is wrong. Check `docker ps` and `DefaultDbConnection`. |
| `Login failed for user 'sa'` | The password in the connection string doesn't match `MSSQL_SA_PASSWORD`. |
| `The ConnectionString property has not been initialized` | The key must be named exactly **`DefaultDbConnection`** in `appsettings.json`. |
| `Value cannot be null (Parameter 'connectionString')` when creating a menu item | `StorageAccount` connection string is missing. |
| Creating a menu item fails with a blob error | The blob storage is unreachable, or the `redmango` container doesn't exist. |
| HTTPS certificate warning | Run `dotnet dev-certs https --trust`, or use the `http` profile. |
| `401 Unauthorized` on a protected endpoint | Make sure you entered `Bearer <token>` (with the word *Bearer* and a space) in Authorize. |
| Port already in use | Change `applicationUrl` in `Properties/launchSettings.json`. |

---

## 🧭 Known Limitations & Roadmap

This project is a work in progress. Current known issues and planned improvements:

**Known issues**

- Single-item menu routes use `"id:int"` instead of `"{id:int}"`, so the id is read from the query string rather than the URL path.
- `GET /api/ShoppingCart` loads the cart but does not yet assign it to `result`, so the response body is empty.
- `POST /api/ShoppingCart` fails when a user without a cart sends `updateQuantity ≤ 0`.
- Logging in with a username that doesn't exist returns a server error instead of a `400` "invalid credentials" response.
- `MenuItemUpdateDto.File` is marked `[Required]`, so updating a menu item currently requires uploading a new image.
- Menu and cart endpoints are not yet protected with `[Authorize]`, and any user can register with the `admin` role.
- `DeleteMenuItem` includes an intentional 2-second delay (`Thread.Sleep`).

**Roadmap**

- [ ] Fix the issues listed above
- [ ] Protect admin endpoints with `[Authorize(Roles = "admin")]`
- [ ] Stripe payment integration (`StripePaymentIntentId`, `ClientSecret`, `CartTotal`)
- [ ] Orders and order history
- [ ] React frontend (menu, cart, login / register, admin management)
- [ ] Move secrets to User Secrets / environment variables

---

## 🙏 Acknowledgements

Built while following **Bhrugen Patel's** (DotNetMastery) *React Bootcamp with .NET API (Router, Redux Toolkit, Hooks)* course, which covers building the RedMango restaurant application end to end with ASP.NET Core and React.

---

## 👤 Author

**Niki Najafi**: [GitHub @nicki1999](https://github.com/nicki1999)
