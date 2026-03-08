# 17 Card Game

A **Unity** client for **17 Card Game** — a 3-player trick-taking card game with betting. Play locally against AI or online with friends via real-time multiplayer.

**Product:** 17 Card Game · **Company:** BVB Apps  
**Website:** [17cardgame.com](https://17cardgame.com/)

---

## Features

- **Local mode** — 1 human vs 2 CPU players (Normal / Hard).
- **Online mode** — Real-time multiplayer over Socket.IO with Firebase authentication.
- **Game rules** — Single, Pair, Run, Suited Run, Set, Paired Run, Bomb. First trick starts with Spade 3; tokens and pot betting.
- **UI** — Main menu, create/join games, waiting rooms (creator/invitee), gameplay (Play/Pass), round/game finished, leaderboard, profile, options (e.g. Auto-Suggest, Auto-Pass), buy tokens (IAP), delete account.
- **Platforms** — Built for **WebGL**, **Windows**, **Android**, and **iOS** (landscape-oriented).

---

## Requirements

- **Unity 2022.3** (tested with 2022.3.62f2)
- **Backend** — Game server and WebSocket server (see [Backend / API](#backend--api))
- **Firebase** — For authentication (email/password; WebGL uses REST, native builds use Firebase SDK)

---

## Project structure

```
17cardgame-frontend/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/          # Card, Play, Rules, Constants, MainThread, etc.
│   │   ├── Managers/      # GameManager, SocketManager, AuthManager, Dealer, AI/Local/Online players
│   │   ├── Services/     # IAP, Stripe, payment factory
│   │   └── UI/            # Scenes UI, CardAnimator, TableAnimator, SelectionManager, dialogs
│   ├── Scenes/            # Splash, Login, Register, MainMenu, GamePlay, Options, etc.
│   ├── Prefabs/
│   ├── Resources/        # Sprites, DOTween settings
│   └── Plugins/           # WebGL Socket.IO bridge, etc.
├── ProjectSettings/
├── Packages/              # manifest.json (Unity packages + Git UPM)
├── lib/                   # External libs (e.g. SocketIO Unity, NativeWebSocket)
└── README.md
```

---

## Getting started

### 1. Clone and open in Unity

```bash
git clone <repo-url> 17cardgame-frontend
cd 17cardgame-frontend
```

Open the project in **Unity Hub** (Unity 2022.3) by adding this folder. Use the existing solution (e.g. `CardGameUnity.sln` or `cardgame-frontend.sln`) if you use an external editor.

### 2. Backend and API

The client expects:

- **REST API** — e.g. `https://www.17-cardgame.com/api/` (production) or `http://localhost:5001/api/` (local).
- **WebSocket (Socket.IO)** — e.g. `https://www.17-cardgame.com/` (production) or `http://localhost:5001` (local).

Server URLs are set in **`Assets/Scripts/Core/Constants.cs`**:

- `SERVER_HTTPS_URL` / `SERVER_WS_URL` — production
- `LOCAL_HTTP_URL` / `LOCAL_WS_URL` — local dev

In **Debug** builds the client uses the local URLs; in **Release** it uses the server URLs.

### 3. Firebase

- Create a Firebase project and enable **Email/Password** sign-in.
- In **Constants.cs**, set `firebaseWebApiKey` to your Firebase Web API key (or use a config that’s not committed).
- WebGL uses Firebase REST (Identity Toolkit). Native builds use the **Firebase SDK** (ensure Firebase is set up under `Assets` for Android/iOS if you use native auth).

### 4. Run in Editor

1. Open scene **Splash** or **MainMenu** (or the scene set as default in Build Settings).
2. Press **Play**.
3. For **online mode**, ensure the game server and Socket.IO server are running and reachable (local or production URLs as above).

---

## Building

1. **File → Build Settings** — Add the scenes you need (e.g. Splash, Login, MainMenu, GamePlay, etc.) in the right order.
2. Choose platform: **WebGL**, **Windows**, **Android**, or **iOS**.
3. **Build** or **Build And Run**.

**WebGL:**  
Socket.IO is handled by a custom WebGL plugin (JavaScript bridge). Ensure the WebGL template and plugin are included in the build.

**Android / iOS:**  
Configure Player Settings (package name, permissions, etc.). For Firebase Auth on device, add and configure the Firebase SDK and config files.

---

## Configuration

- **Constants** — `Assets/Scripts/Core/Constants.cs`: server URLs, Firebase API key, game option keys.
- **Game options** — Stored in `PlayerPrefs` (e.g. `GameOptionsKeys.AutoSuggest`, `GameOptionsKeys.AutoPass`).
- **`.env`** — The repo includes an `.env.example`; copy to `.env` and set any required env vars if your workflow uses them (e.g. for build or tooling). The Unity client itself reads URLs and keys from `Constants.cs`, not from `.env`.

---

## Main dependencies (Unity)

| Package / asset        | Purpose                    |
|------------------------|----------------------------|
| SocketIOUnity          | Socket.IO client (native)  |
| NativeWebSocket        | WebSocket (e.g. fallback)  |
| TextMesh Pro           | UI text                    |
| Newtonsoft Json        | JSON (API / Socket payloads) |
| DOTween (Demigiant)    | Animations                 |
| Unity UI Extensions    | UI components              |
| Unity Rounded Corners  | UI styling                 |
| Unity IAP              | In-app purchases (tokens)  |
| Firebase (native only) | Auth on mobile/PC          |

WebGL uses a custom JS bridge for Socket.IO and Firebase REST for auth.

---

## Script overview

| Area        | Scripts |
|------------|---------|
| **Core**   | `Card`, `Play`, `Rules`, `GameState`, `Constants`, `MainThread` |
| **Managers** | `GameManager`, `SocketManager`, `AuthManager`, `PrepareManager`, `Dealer`, `HumanPlayer`, `AIPlayer`, `RequestManager` |
| **UI**     | `CardAnimator`, `TableAnimator`, `SelectionManager`, `GameFinished`, `RoundFinished`, `MainMenuUI`, `LoginUI`, `NewGame`, etc. |
| **Services** | `IAPService`, `StripeService`, `PaymentServiceFactory` |

---

## License and contributing

See the repository’s license file (if any). For contributing, open an issue or submit a pull request.

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for release notes and changes.
