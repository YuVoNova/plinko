# Progressive Plinko

A Plinko game prototype built for Midas Games case study. The focus is on system architecture, server-authoritative design, and clean code rather than visual polish.

## Gameplay

Players hold the screen to drop balls onto a Plinko board. Balls bounce off pegs and land in buckets, each with a reward multiplier. Dropping all balls for a level advances to the next level with better rewards. Every 15 minutes, the session resets (balls and level go back to start), but the wallet balance is preserved.

## Architecture

The project follows an event-driven architecture where the UI layer only displays data and never makes game logic decisions. All reward calculations happen on the server side (simulated via MockServerService), and the client simply reports which bucket each ball landed in.

### Core Components

**PlinkoGameManager** acts as the orchestrator. It coordinates between controllers, manages game state, and broadcasts events to the UI. State management is minimal and kept here intentionally since it only involves a few transitions.

**SpawnController** handles ball spawning, counting, and refunds when balls go out of bounds. It doesn't know about levels or rewards, only about spawning balls and tracking counts.

**LevelController** manages level loading, progression checks, and the level-up flow. It communicates with the backend to validate when a level up should happen.

**MockServerService** simulates a real backend with network latency. It implements IBackendService interface for easy swapping with a real server later. The server is the source of truth for all reward calculations.

**PlayerDataStore** handles all persistence through PlayerPrefs. Session state, wallet balance, and reward history are managed here.

**PlinkoBatchProcessor** solves the network strategy problem. Instead of making a server call for every ball landing (which would spam requests), it batches landings together. A batch is sent when either 5 balls have landed or 2 seconds have passed since the last landing.

**PlinkoSessionManager** monitors the 15-minute reset timer by polling the server periodically and triggers a reset when needed.

### Design Patterns Used

- **Object Pooling** for balls to minimize garbage collection during gameplay
- **Observer Pattern** via C# Actions for event-driven communication between systems
- **Facade Pattern** in MockServerService which provides a simple API while coordinating multiple validators and data stores internally
- **Single Source of Truth** where the server holds authoritative game state and the client is just a view

### Data Flow

When a ball lands, the spawner fires an event with the bucket index. GameManager passes this to the BatchProcessor which collects landings. Once a batch is ready, it sends the bucket indices to MockServerService. The server calculates rewards using its own config data (not trusting client values), updates the wallet, and returns the new balance. GameManager then fires OnWalletUpdated which the UI picks up to update the display.

## Room for Improvement

There are several areas I'd improve given more time.

**Architecture Issues**

GameManager still has multiple responsibilities. While I extracted SpawnController and LevelController, the reset logic and some coordination code could be further separated. MockServerService similarly handles both API orchestration and some session logic that could be extracted.

Some UI scripts like BatchProcessingIndicator contain logic about when to show or hide themselves. Ideally, they should just receive a "show" or "hide" call and let the game logic decide when. The decision making should stay in PlinkoBatchProcessor or GameManager.

**Missing Features**

ScriptableObject configs would be better than hardcoded values for ball physics, batch settings, and level data. Currently these are marked with TODOs in the code.

The board currently uses fixed peg rows and bucket counts. A better approach would be to store different board layouts per level, allowing levels to have varying numbers of peg rows and buckets. The camera orthographic size could then scale based on the row count to ensure the board always fits the screen properly.

Visual feedback like peg hit animations, bucket reward popups, and sound effects would make the game feel more satisfying. The case study mentioned this isn't the focus, but it would help the overall feel.

Unit tests for the validators and controllers would ensure the core logic is correct and make refactoring safer.

## Project Structure
```
Assets/Scripts/
├── Backend/        Server simulation, validators, persistence
├── Gameplay/       Core game logic, controllers, board
├── UI/             Display components
├── Data/           Data models and configs
├── Core/           Shared types like GameState enum
└── Utils/          Generic utilities like ObjectPool
```

## Setup

The project was built with Unity 6000.0.33f1. Open the project, load the Game scene from Assets/Scenes, and press Play. For Android testing, build and run normally.

Dev tools are available in the bottom-right corner for testing manual resets and adding balance.
