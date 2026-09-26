# Maxion Assignment (Unity client)

Unity client for the **Order Backend** (Go). The player signs in as a guest or with email/password, browses products from the server, and buys them through `POST /api/orders`. The backend is a separate project (`Maxion_Assignment_Backend`) and **must be running** before you press Play.

Stack: Unity **6000.3.12f1** · URP 2D · VContainer · UniTask · VitalRouter · DOTween · TextMesh Pro · Input System

---

## Requirements

| Tool | Notes |
|---|---|
| Unity **6000.3.12f1** | Install through Unity Hub. Other versions may re-import or upgrade the project. |
| [Git LFS](https://git-lfs.com) | Images, fonts, and DLLs are stored in LFS. Run `git lfs install` once **before** cloning. |
| Internet on first open | VContainer, UniTask, and NuGetForUnity are UPM git packages that download when the project first opens. |
| The backend | See [Running the backend](#1-start-the-backend). Needs Go 1.25, or Docker for Postgres. |

---

## Quick start

### 1. Start the backend

In the backend folder (full details in its `README.md`):

```bash
cp .env.example .env     # set JWT_SECRET to at least 32 characters
```

Then pick one:

```bash
# A. Postgres (data is kept)
make db-up && make migrate && make run

# B. No database (in-memory, data is lost on restart). Quickest option.
make run-memory
```

Check that it is up: open http://localhost:8080/healthz in a browser.

### 2. Open the project

```bash
git lfs install
git clone https://github.com/Karameet/Maxion_Assignment.git
```

Add the folder in Unity Hub and open it with **6000.3.12f1**. The first import takes a few minutes because it rebuilds `Library/` and downloads packages.

If you got the project as a zip instead, check that images are real images, not small text files. If they are text files, LFS was not pulled. Run `git lfs pull`.

### 3. Play

1. Open **`Assets/Maxion_Assignment/Scenes/Initialize.unity`**.
2. Press Play.

> **Always start from `Initialize`.** It creates the root VContainer scope (API client, session, product catalog) and keeps it across scenes. Pressing Play on `Login` or `Shop` directly fails with a VContainer *parent not found* error.

---

## Scene flow

```
Initialize ──► Login ──► Shop
```

| Scene | What happens |
|---|---|
| **Initialize** | `GET /healthz` (3 attempts, 1 s apart), then `GET /api/products`, then loads `Login`. If the backend is down, it logs `[Initialize] Backend connection failed` in the Console and stays on this scene. |
| **Login** | Guest login (`POST /api/auth/guest`), email login (`POST /api/auth/login`), or register via the popup. Each path ends with `GET /api/me`, then loads `Shop`. |
| **Shop** | Lists the products. Clicking a product opens the purchase popup, and **Buy** sends `POST /api/orders` with an `Idempotency-Key`. |

All three scenes are already in the build scene list in this order.

---

## Configuration

The backend URL is **not** in a config file. It is a serialized field on the `InitializeLifetimeScope` GameObject in `Initialize.unity`:

**Initialize scene → `InitializeLifetimeScope` → Backend Config → Base Url** (default `http://localhost:8080`)

| Where the game runs | Base Url |
|---|---|
| Editor / Windows build, backend on the same PC | `http://localhost:8080` |
| Android emulator | `http://10.0.2.2:8080` |
| Real phone on the same Wi-Fi | `http://<PC's LAN IP>:8080`. Also allow port 8080 through Windows Firewall. |

Change it in the Inspector, not in `BackendConfig.cs`. The scene's value overrides the code default.

Other settings on the same objects:
- `InitializeLifetimeScope`: request timeout, health-check attempts and delay, next scene.
- `LoginLifetimeScope`: scene to load after login.

> The backend serves plain HTTP, so **Player Settings → Other Settings → Allow downloads over HTTP** is already set to *Always allowed*. Keep it that way, or switch the backend to HTTPS.

---

## Project layout

```
Assets/Maxion_Assignment/
├── Scenes/        Initialize, Login, Shop
├── Scripts/
│   ├── Backend/     BackendApiClient (REST wrapper), DTOs, BackendException, PlayerSession, ProductCatalog
│   ├── Initialize/  Root LifetimeScope + boot entry point
│   ├── Login/       Login / Register views and presenters
│   ├── Shop/        Shop list, product item, purchase popup
│   ├── UI/          Shared popup and button animation
│   └── Editor/      Menu tools that generate UI (menu: MaxionAssignment/…)
├── 2D/, Font/     Art and fonts
```

Architecture is **MVP + VContainer**: each scene has a `LifetimeScope` that registers its View (MonoBehaviour) and Presenter (plain C# entry point). Login and Shop scopes use `InitializeLifetimeScope` as their parent, so they share one `BackendApiClient`, `PlayerSession`, and `ProductCatalog`.

`Assets/Scenes/SampleScene.unity` is the unused Unity template scene.

### Behaviour worth knowing

- **Idempotency:** one purchase attempt uses one `Idempotency-Key`. Pressing Buy again after a network error or 5xx **reuses** the key, so the server never creates a duplicate order. Changing quantity, opening another product, a 4xx, or a success starts a new key. See `PurchasePresenter.cs`.
- **Prices come from the server.** The total shown after buying is the server's `total`, not a client calculation.
- **Saved data:** the JWT, its expiry, and a generated device ID are stored in `PlayerPrefs` (keys start with `order_api_`). To start as a brand-new guest, use **Edit → Clear All PlayerPrefs**.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| Stuck on Initialize, Console shows `Backend connection failed` | The backend isn't running, or Base Url is wrong. Check http://localhost:8080/healthz. |
| `VContainerException ... parent` when pressing Play | You started from Login or Shop. Play from `Initialize.unity`. |
| Compile errors about `VContainer`, `Cysharp`, or `VitalRouter` | Packages didn't download. Check your internet connection, then reopen the project or use **Window → Package Manager → Refresh**. For VitalRouter, use **NuGet → Restore Packages**. |
| Pink or missing images, broken fonts | Git LFS files weren't pulled. Run `git lfs install && git lfs pull`. |
| `InvalidOperationException` from `UnityEngine.Input` | Only the new Input System is enabled. Use `UnityEngine.InputSystem` APIs. |
| Login works in the Editor but not on a phone | Wrong Base Url (`localhost` means the phone itself), firewall, or HTTP blocked. See [Configuration](#configuration). |

---

## Backend API reference

Endpoints, request/response shapes, and error codes are in the backend project at `Docs/api-reference.md`.
