# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Tower Game is a 2D tower/office building management simulator inspired by "The Tower II". Players build and manage office buildings, observing employee commute cycles throughout the day.

- **Engine**: Unity 6.0.3.2f1
- **Language**: C#
- **Project Type**: 2D Simulation (single-scene gameplay using URP)
- **Primary Scene**: `Assets/Scenes/SampleScene.unity`

## Architecture

### Namespace Structure

```
TowerGame.Core      → GameManager, GameTimeManager (singletons)
TowerGame.Building  → OfficeBuilding, BuildingPlacer, FloorSystemManager, FloorStructure, Lobby
TowerGame.Building  → Elevator/: ElevatorManager, ElevatorShaft, ElevatorCar
TowerGame.People    → Person (abstract), Employee, PersonSpawner, state enums
TowerGame.Grid      → GridManager, GridConfig (ScriptableObject)
TowerGame.UI        → BuildModeUI, TimeDisplayUI
TowerGame.Editor    → TowerGameSceneSetup (editor-only)
```

### Key Patterns

- **Singleton Pattern**: `GameManager`, `GameTimeManager`, `GridManager`, `BuildingPlacer`, `FloorSystemManager`, `ElevatorManager` all use `Instance` property
- **Auto-Discovery**: Components attempt to find references via `GetComponentInChildren<T>()` then `FindObjectOfType<T>()` if not serialized
- **Event-Driven Time**: `GameTimeManager` fires `OnHourChanged`, `OnDayChanged`, `OnTimeUpdated` events that employees subscribe to
- **State Machine**: Employee uses `EmployeeState` enum (AtHome → CommutingToWork → EnteringBuilding → Working → LeavingBuilding → CommutingHome)

### Core Systems Interaction

```
GameManager
    ├── GameTimeManager (time simulation: 10 sec = 1 game hour)
    ├── PersonSpawner → Employee[] (3 employees by default)
    └── OfficeBuilding (capacity: 10, work hours: 8:00-18:00)

FloorSystemManager (singleton)
    ├── TowerData[] (supports twin towers via towerId)
    ├── Lobby (ground floor, required first)
    ├── FloorStructure[] (floor templates per tower)
    └── Placement validation (support requirements, overlap checks)

ElevatorManager (singleton)
    ├── ElevatorShaft[] (vertical shaft spanning floors)
    │   └── ElevatorCar (moves within shaft, handles passenger transport)
    └── Events: OnShaftCreated, OnCarArrived

BuildingPlacer ←→ GridManager ←→ FloorSystemManager
    └── OnBuildingPlaced event → creates buildings via FloorSystemManager

Employee subscribes to GameTimeManager.OnHourChanged
    → StartCommute() at 8:00 AM
    → LeaveWork() at 18:00
```

## Development Workflow

### Opening the Project
1. Unity Hub → Add Project → Select this folder
2. Open with Unity 6.0.3.2f1
3. Open `Assets/Scenes/SampleScene.unity`

### Running the Game
Press Play in Unity Editor. Game starts at 6:00 AM with employees spawned at home position.

### Keyboard Controls (in Play Mode)
- `Space` → Toggle pause
- `1` / `2` / `4` → Set game speed (1x, 2x, 4x)
- Mouse click → Place buildings (when in build mode)

### Input System
Uses Unity's **new Input System** (`UnityEngine.InputSystem`). Access keyboard via:
```csharp
Keyboard keyboard = Keyboard.current;
if (keyboard.spaceKey.wasPressedThisFrame) { ... }
```

## Coordinate System

| Element | Value | Notes |
|---------|-------|-------|
| Ground Level | Y = -3.0 | World coordinate |
| Building Size | 8×5 units | Width × Height |
| Floor Height | 5 units | For multi-floor support |
| Grid Segment | 1 unit | Grid snapping unit |
| Spawn Point | (-18, -3) | Employee home position |

Building placement Y-coordinate: `-3.0 + (buildingHeight / 2) = -0.5` (pivot at center)

### Floor System (The Tower II Style)
- 0-indexed: `floor 0 = 1F`, `floor 1 = 2F`
- **Building Hierarchy**: Lobby (ground) → FloorStructure → Tenants (Office, Shop, etc.)
- **Placement Rules**: 2F+ requires 70% support from floor below
- **Twin Towers**: Supported via `towerId` parameter in FloorSystemManager
- **Elevator Access**: ElevatorShaft spans multiple floors, ElevatorCar transports passengers

## Time System

```
Real Time → Game Time: 10 seconds = 1 hour
Default Start: 6:00 AM
Work Hours: 8:00 AM - 6:00 PM (18:00)
```

Key methods on `GameTimeManager`:
- `IsWorkingHours()` → 8:00-18:00
- `IsCommutingTime()` → 7:00-8:30
- `IsLeavingTime()` → 17:30-19:00

## Code Conventions

- **Logging**: Use `Debug.Log($"[ClassName] message")` prefix pattern
- **XML Docs**: Public methods have `<summary>` documentation
- **SerializeField**: Private fields exposed to Inspector use `[SerializeField]`
- **Header Attributes**: Group Inspector fields with `[Header("Section Name")]`

## Project Documentation

Detailed design documents in `claudedocs/`:
- `GAME_SPECIFICATION.md` - Full game spec with class diagrams (Japanese)
- `GRID_SYSTEM_DESIGN.md` - Grid/floor system architecture
- `FLOOR_SYSTEM_DESIGN.md` - The Tower II style floor/building hierarchy system (Japanese)

## Current Implementation Status

**Implemented**: Core loop, time system, office building, employee commuting, building placement with grid snapping, multi-floor placement structure, UI, floor system manager (The Tower II style hierarchy), elevator system (ElevatorManager, ElevatorShaft, ElevatorCar), lobby building type, twin tower support

**Not Implemented**: Economic system, additional building types (restaurant, shop), save/load, elevator passenger dispatch logic integration with employee AI

## Unity MCP Integration

Project includes Unity MCP package (`jp.shiranui-isuzu.unity-mcp`) for Claude AI integration. Sample handlers in `Assets/Samples/`.
