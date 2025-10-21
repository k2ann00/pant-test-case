# Developer Links

LinkedIn - [Kaan Avdan](https://www.linkedin.com/in/kaanavdan/)   ||   Portfolio - [Kaan Avdan](https://linktr.ee/k2ann00)

#
<p float="left">
  <img src="https://raw.githubusercontent.com/k2ann00/pant-test-case/main/Assets/Sprite/Logo-BG.png" width="200" />
  <img src="https://raw.githubusercontent.com/k2ann00/pant-test-case/main/Assets/Sprite/Outlined-Icon-Photoroom.png" width="100" />
</p>

# Panteon Playable Ad Case - Complete Project Documentation

## 📋 Table of Contents
1. [Project Overview](#project-overview)
2. [Game Mechanics](#game-mechanics)
3. [Architecture & Design Patterns](#architecture--design-patterns)
4. [Project Structure](#project-structure)
5. [Key Systems](#key-systems)
6. [Script Directory](#script-directory)
7. [Class Relationships](#class-relationships)
8. [Game Flow & Data Lifecycle](#game-flow--data-lifecycle)
9. [Scene Structure](#scene-structure)
10. [Dependencies](#dependencies)
11. [Configuration](#configuration)

---

## Project Overview

**Project Name:** Panteon-PlayableAdCase
**Type:** 3D Mobile Playable Advertisement
**Platform:** iOS/Android (Portrait orientation - 540x960, 960x600)
**Genre:** Airport/Baggage Handling Simulation
**Developer:** k2ann00

### Game Premise
An interactive airport baggage handling simulation where players control a character to manage passenger baggage through various processing stages. The game showcases economic progression through unlocking new areas and completing increasingly complex baggage handling tasks.

---

## Game Mechanics

### Core Gameplay Loop
```
1. Passengers arrive in queue at Welcome Circle
   ↓
2. Player collects baggage from passengers
   ↓
3. Player navigates to processing areas:
   - Baggage Unload → Conveyor → Platform → Truck
   - Or X-Ray processing route
   ↓
4. Baggage loaded onto trucks
   ↓
5. Earn money → Unlock new areas
   ↓
6. Repeat with increased complexity
```

### Key Mechanics

| Mechanic | Description |
|----------|-------------|
| **Movement** | On-screen joystick controls character movement |
| **Baggage Collection** | Passengers hand over baggage when player is near |
| **Stacking System** | Collected baggage stored in player's baggage holder |
| **Transportation** | Smooth tween-based movement through conveyor systems |
| **Platform Automation** | Automated lift system moves baggage up/down |
| **Truck Loading** | Baggage positioned and transported on trucks |
| **Unlocking System** | Earn money to unlock new areas (stairs, boards, etc.) |
| **Escalator Climbing** | Passengers climb stairs using character animation |
| **Queue Management** | Automatic passenger sorting and positioning |
| **Economy** | Earn coins from completed tasks, spend to unlock areas |

---

## Architecture & Design Patterns

### 1. **Singleton Pattern**
All major managers use singleton pattern for centralized system control:
```csharp
public static GameManager Instance { get; set; }
if (Instance == null) Instance = this;
```
**Managers:** GameManager, PassengerManager, MoneyManager, BaggageUnloadManager, etc.

### 2. **Event Bus Pattern**
Central decoupled messaging system for inter-system communication:
```csharp
public static event Action<CircleType> PlayerEnteredCircle;
public static event Action PassengerHandedBaggage;
public static event Action AllBaggagesLoadedToTruck;
// ...20+ events
```

**Benefits:**
- Loose coupling between systems
- Easy to add new event listeners
- Centralized event management

### 3. **Object Pooling Pattern**
Efficient memory management for frequently created/destroyed objects:
```csharp
MoneyPool.Instance.Get(prefab)           // Get from pool or create
MoneyPool.Instance.Return(prefab, obj)   // Return to pool
```
**Pooled Objects:** Money coins, Passengers, Baggage items

### 4. **State Machine Pattern**
Passengers and baggage follow defined state flows:
```csharp
enum PassengerState {
    Waiting,
    HandingBaggage,
    WalkingToTarget,
    Climbing,
    Done
}

enum BaggageState {
    WithPlayer,
    OnConveyor,
    OnPlatform,
    OnTruck,
    Delivered
}
```

### 5. **Path-Following Pattern**
Characters move along predetermined waypoint paths using DOTween:
```csharp
transform.DOPath(pathArray, duration, PathType.Linear)
    .SetEase(Ease.Linear)
    .SetSpeedBased(true);
```

### 6. **Transfer/Unlock Pattern**
Economic system using interface-based money transfer:
```csharp
interface ITransferTarget {
    TransferSettings GetTransferSettings();
    void OnTransferProgress(float fill, int remaining);
    void OnTransferCompleted();
}

// Implemented by BarFill → UnlockableArea
```

### 7. **Trigger Zone Pattern**
Circular areas detect player collision for game events:
```csharp
// Circle detection
if (player in circle range) {
    EventBus.RaisePlayerEnteredCircle(circleType);
}
```

---

## Project Structure

### Folder Organization
```
Assets/
├── _Scripts/
│   ├── Managers/           (10 files)  - Game system management
│   ├── Controllers/        (12 files)  - Input and movement control
│   ├── Models/             (7 files)   - Game object behaviors
│   ├── Helpers/            (7 files)   - Utility and support systems
│   ├── UI/                 (2 files)   - Interface elements
│   ├── Managers.meta       (Old location - pre-organization)
│   └── ...other scripts
├── _Prefabs/               - Reusable game objects
│   ├── ======MANAGERS======.prefab
│   ├── Player.prefab
│   ├── Characters.prefab
│   ├── Environment.prefab
│   ├── Canvas.prefab
│   ├── Arrow Mark.prefab
│   ├── Waiting Areas.prefab
│   └── ...
├── Scenes/
│   ├── GameScene.unity      (Main gameplay)
│   ├── SampleScene.unity    (Template)
│   └── TestAnimScene.unity  (Animation testing)
├── Material/               - Game materials
├── Sprite/                 - UI and game sprites
├── TextMesh Pro/           - Text rendering assets
├── 3rdParty/               - External packages
└── ProjectSettings/        - Unity configuration
```

---

## Script Directory

### **MANAGERS** - Central Game Systems (10 Files)

| Script | Responsibility |
|--------|-----------------|
| **GameManager.cs** | Core game state, arrow mark control, debug settings, slow/fast-motion modes |
| **PassengerManager.cs** | Passenger lifecycle, queue management, pathfinding, circular area interactions |
| **MoneyManager.cs** | Economy system, money transfers, unlock progression tracking, coin spawning |
| **UIManager.cs** | Money display updates, throttled UI refresh |
| **BaggageUnloadManager.cs** | Baggage unloading sequence, conveyor coordination |
| **BaggageXrayManager.cs** | X-Ray station processing, platform synchronization |
| **PassengerPool.cs** | Passenger object pooling |
| **MoneyStackManager.cs** | Money stack organization |
| **QueueManager.cs** | Queue system management |
| **PaintingManager.cs** | Unlock painting/visualization mechanics |

### **CONTROLLERS** - Input & Movement (12 Files)

| Script | Responsibility |
|--------|-----------------|
| **PlayerController.cs** | Player character movement, joystick handling, climbing mechanics |
| **JoystickController.cs** | On-screen joystick input management |
| **CameraController.cs** | Third-person camera following, cinematics, board focus |
| **PlatformMover.cs** | Automatic platform up/down movement via tweens |
| **StairsController.cs** | Escalator/stairs animation loops |
| **WaitingAreaController.cs** | Circle trigger zones for game areas |
| **EscalatorTrigger.cs** | Start/end point markers for escalator climbing |
| **PassengerController.cs** | Individual passenger AI (movement, baggage handoff) |
| **ArrowMarkController.cs** | Floating directional arrow UI |
| **PaintInputController.cs** | Paint/unlock input system |
| **DirectionArrowManager.cs** | Direction arrow management |
| **PassengerControllerCOP.cs** | Alternative passenger controller (legacy variant) |

### **MODELS** - Game Object Behaviors (7 Files)

| Script | Responsibility |
|--------|-----------------|
| **BaggageMover.cs** | State machine for baggage (conveyor → platform → truck movement) |
| **BaggageXrayMover.cs** | X-Ray baggage path movement |
| **BaggageStack.cs** | Stack data structure and positioning for baggage |
| **BaggageTruck.cs** | Truck movement automation (load → destination → return) |
| **PlayerBaggageHolder.cs** | Player's baggage collection container and management |
| **MoneyMover.cs** | Money coin visual animation trajectory |
| **PassengerXrayManager.cs** | Passenger X-Ray inspection processing |

### **HELPERS** - Supporting Systems (7 Files)

| Script | Responsibility |
|--------|-----------------|
| **EventBus.cs** | Central event system (20+ event types), circle type enum, state management |
| **ObjectPool.cs** | Generic object pooling implementation |
| **Transfers.cs** | Money transfer interface and transfer settings |
| **AnimationEventRelay.cs** | Animation event callback forwarding |
| **BoardSurface.cs** | Board surface interactive mechanics |
| **PlayerCircleChecker.cs** | Player circle/trigger detection |
| **UnlockableArea.cs** | Locked area unlock animation sequence |

### **UI** - Interface Elements (2 Files)

| Script | Responsibility |
|--------|-----------------|
| **BarFill.cs** | Progress bar for unlockable areas, implements ITransferTarget |
| **PaintingUI.cs** | Painting mode user interface |

---

## Class Relationships

### Dependency Hierarchy
```
EventBus (Static - Central Hub)
    ├── GameManager (Singleton)
    │   └── Game state & settings
    │
    ├── PassengerManager (Singleton)
    │   ├── PassengerController (Multiple instances)
    │   │   └── Path systems
    │   └── Object Pool
    │
    ├── MoneyManager (Singleton)
    │   ├── ITransferTarget (BarFill, UnlockableArea)
    │   └── MoneyPool (Object Pool)
    │
    ├── BaggageUnloadManager (Singleton)
    │   ├── BaggageMover (Multiple)
    │   ├── BaggageStack
    │   ├── PlayerBaggageHolder
    │   └── PlatformMover
    │
    ├── BaggageXrayManager (Singleton)
    │   ├── BaggageXrayMover (Multiple)
    │   └── PlatformMover
    │
    ├── BaggageTruck (Singleton)
    │
    ├── CameraController (Singleton)
    │   └── Cinematic control
    │
    ├── WaitingAreaController (Multiple)
    │   └── Circle trigger zones
    │
    └── StairsController (Multiple)
        └── Escalator animations
```

### Key Interfaces
```csharp
interface ITransferTarget {
    TransferSettings GetTransferSettings();           // Get unlock cost & speed
    void OnTransferProgress(float fill, int left);   // Update progress
    void OnTransferCompleted();                        // Handle completion
}
```

### Important Enums
```csharp
enum CircleType {
    WelcomingCircle,
    BaggageUnload,
    BaggageXray,
    PassengerXray
}

enum PassengerState {
    Waiting,
    HandingBaggage,
    WalkingToTarget,
    Climbing,
    Done
}

enum PassengerPathType {
    ToStairs,
    ToXRay,
    ToUpperQueue,
    ToExit,
    ToInspectionPoint
}
```

---

## Game Flow & Data Lifecycle

### Passenger Lifecycle (Complete Flow)

```
[Step 1] Queue Formation
├─ PassengerManager initializes passenger pool
├─ Passengers positioned in queue at start
└─ Event: PassengerManager.OnPlayerEnteredCircle()

[Step 2] Baggage Handoff
├─ Player collides with welcome circle
├─ Passenger walks to front
├─ Baggage jumps to PlayerBaggageHolder (animation)
├─ PassengerState: HandingBaggage → WalkingToTarget
└─ Event: PassengerHandedBaggage

[Step 3] Stairs Navigation
├─ Passenger walks to stairs (follows ToStairs path)
├─ PassengerState: WalkingToTarget → Climbing
├─ Character plays climb animation
├─ rb.MovePosition() interpolates climbing
└─ Event: PassengerReachedTopStairs

[Step 4] X-Ray Path
├─ Passenger walks to X-Ray end (ToXRay path)
├─ Continues to upper queue (ToUpperQueue path)
├─ PassengerState: Climbing → WalkingToTarget
└─ Event: PassengerReachedXRayEnd

[Step 5] Completion
├─ Passenger reaches final position
├─ PassengerState: WalkingToTarget → Done
├─ Object pooled for reuse
└─ Next passenger activated
```

### Baggage Lifecycle (Unload → Truck Path)

```
[Step 1] Collection
├─ Baggage handed from passenger
├─ Added to PlayerBaggageHolder (stack)
├─ Baggage state: WithPlayer
└─ Position: Above player's head

[Step 2] Unload Initiation
├─ Player enters BaggageUnload circle
├─ BaggageUnloadManager.StartStackingBaggages()
├─ Each baggage added to BaggageStack
├─ Visual stack displayed
└─ Baggage state: Positioned

[Step 3] Conveyor Transport
├─ Player enters BaggageXray circle
├─ BaggageMover.Initialize() - starts movement
├─ DOMove animation along conveyor path
├─ Duration based on distance
├─ Baggage state: OnConveyor
└─ Event: BaggageReachedConveyorEnd

[Step 4] Platform Jump
├─ Platform detects baggage arrival
├─ Jump animation (bezier curve)
├─ Baggage positioned on platform
├─ Baggage state: OnPlatform
└─ Event: BaggageJumpedToPlatform

[Step 5] Platform Movement
├─ PlatformMover.Move() - platform up animation
├─ Duration: 2 seconds, Ease: InOutSine
├─ All baggage moves with platform
└─ Event: PlatformReachedTop

[Step 6] Truck Loading
├─ Baggage animates to truck position
├─ Stack positioning with spacing
├─ Baggage state: OnTruck
├─ Event: BaggageLoadedToTruck
└─ All baggages loaded? → Event: AllBaggagesLoadedToTruck

[Step 7] Delivery
├─ Truck.MoveToTarget() animation
├─ Travels to destination
├─ Returns to start position
├─ Baggage destroyed / pooled
└─ Baggage state: Delivered
```

### Money/Unlock Lifecycle

```
[Step 1] Trigger Detection
├─ Player collides with BarFill (unlockable area)
├─ Collider.IsPlayerInRange = true
└─ MoneyManager notified

[Step 2] Transfer Initiation
├─ MoneyManager.StartTransfer(ITransferTarget)
├─ Get transfer settings: cost, speed
├─ Initialize transfer progress
└─ Begin Update loop

[Step 3] Money Animation
├─ Each frame: transfer some money amount
├─ Every (coinValue) threshold:
│  ├─ Spawn money coin prefab
│  ├─ MoneyMover animation from source → target
│  ├─ Duration, easing
│  └─ Coin pooled after arrival
├─ OnTransferProgress() called
├─ BarFill progress updated (0→1)
└─ MoneyText updated

[Step 4] Completion
├─ transfer.CurrentAmount >= transfer.CostNeeded
├─ BarFill.OnTransferCompleted() called
├─ Money deducted from total
└─ Event: AreaUnlocked

[Step 5] Visual Unlock
├─ UnlockableArea.OnUnlockCompleted()
├─ Disable locked objects
├─ Scale animate unlocked objects (0 → 1.2 → 1.0)
├─ Sound effects triggered
├─ If stairs: StairsController.StartMoving()
├─ If board: EventBus.BoardUnlocked()
└─ CameraController cinematic

[Step 6] Camera Cinematic (Optional)
├─ EventBus.BoardUnlocked triggered
├─ CameraController.FocusOnBoard()
├─ Smooth position transition
├─ FOV change (85°)
├─ Look-at target change
└─ Player can resume control
```

---

## Scene Structure

### GameScene Hierarchy

```
GameScene
├── ======MANAGERS====== (Prefab Instance)
│   ├── GameManager
│   ├── PassengerManager
│   ├── MoneyManager
│   ├── UIManager
│   ├── CameraController
│   ├── BaggageUnloadManager
│   ├── BaggageXrayManager
│   ├── BaggageTruck
│   └── ...other managers
│
├── Player
│   ├── Model/Animator
│   ├── Rigidbody (for movement)
│   ├── Collider
│   ├── PlayerBaggageHolder (baggage stack container)
│   └── Joystick Input Handler
│
├── Characters (Passenger Pool)
│   ├── PassengerController[0]
│   ├── PassengerController[1]
│   ├── PassengerController[2]
│   └── ... (pooled instances)
│
├── Environment
│   ├── Stairs
│   │   ├── StairsController
│   │   ├── EscalatorStartTrigger
│   │   └── EscalatorEndTrigger
│   ├── Platform (with PlatformMover)
│   ├── Conveyor System
│   │   ├── ConveyorPath
│   │   └── ConveyorEnd
│   ├── Truck (with BaggageTruck)
│   │   ├── LoadPoint
│   │   └── Destination Target
│   ├── Board (Unlockable)
│   │   └── UnlockableArea
│   └── ...other environment
│
├── Waiting Areas (Circle Triggers)
│   ├── WelcomingCircle
│   │   ├── WaitingAreaController (CircleType: WelcomingCircle)
│   │   ├── Collider (is trigger)
│   │   └── Visual indicator
│   ├── BaggageUnload
│   │   ├── WaitingAreaController (CircleType: BaggageUnload)
│   │   └── Collider
│   ├── BaggageXray
│   │   ├── WaitingAreaController (CircleType: BaggageXray)
│   │   └── Collider
│   ├── PassengerXray
│   │   ├── WaitingAreaController (CircleType: PassengerXray)
│   │   └── Collider
│   └── ...other circles
│
├── Path Points (Waypoint Arrays)
│   ├── ToStairsPath (Transform[])
│   ├── ToXRayPath (Transform[])
│   ├── ToUpperQueuePath (Transform[])
│   ├── ToExitPath (Transform[])
│   └── ...other paths
│
├── Canvas (UI Root)
│   ├── MoneyText (TextMeshProUGUI, UIManager target)
│   ├── ProgressBars Panel
│   │   ├── BarFill[0] (ITransferTarget, Stairs unlock)
│   │   ├── BarFill[1] (ITransferTarget, Board unlock)
│   │   └── ...other unlock bars
│   ├── Joystick (FloatingJoystick)
│   │   ├── Background
│   │   ├── Handle
│   │   └── Input capture
│   ├── Arrow Mark (Prefab instance)
│   │   └── ArrowMarkController
│   └── ...other UI elements
│
├── Camera
│   ├── CameraController
│   ├── Normal follow target (player)
│   ├── Board focus target
│   └── FOV controller
│
└── Lighting & Post-Processing
    ├── Main Light
    ├── Post-Process Volume
    └── Sky settings
```

### Required Tags
```csharp
Tag: "Player"              - Used for collision detection
Tag: "WelcomingCircle"     - Queue entry point
Tag: "BaggageUnload"       - Baggage drop zone
Tag: "BaggageXray"         - X-Ray processing zone
Tag: "PassengerXray"       - Passenger inspection zone
Tag: "EscalatorStartPoint" - Stairs entry
Tag: "EscalatorEndPoint"   - Stairs exit
```

### Layer Usage
- **Default** - General objects
- **Player** - Player character
- **Passenger** - Passenger characters
- **Baggage** - Baggage items
- **Environment** - Static environment
- **UI** - Canvas and UI elements
- (Configure collision matrix as needed)

---

## Dependencies

### Third-Party Assets & Packages

#### 1. **DOTween** (Animation Library)
- **Version:** 2.0+
- **Usage:** Smooth movement, scaling, rotation animations
- **Key Uses:**
  - Character pathfinding: `DOPath()`
  - Baggage movement: `DOMove()`
  - Platform animations: `DOMove()`, `DORotate()`
  - UI tweens: `DOScale()`, `DOFade()`
- **Location:** `Assets/Plugins/DOTween/`

#### 2. **Joystick Pack** (Mobile Input)
- **Components Used:** FloatingJoystick
- **Usage:** On-screen touch joystick for player movement
- **Integrated With:** PlayerController
- **Settings:** Dynamic positioning, input sensitivity

#### 3. **Crafting Mecanim Animation Pack FREE**
- **Components:**
  - CrafterControllerFREE.cs - Base character animation controller
  - InputHelper.cs - Input utility functions
  - SmoothFollow.cs - Camera following system
- **Animation Clips:** Walking, climbing, idle states for passengers and player
- **Models:** Character meshes and rigs

#### 4. **TextMesh Pro** (Text Rendering)
- **Usage:** Money display, UI text, cost labels
- **Location:** `Assets/TextMesh Pro/Resources/`
- **Fonts:** LiberationSans SDF (main font)

#### 5. **Unity Standard Assets** (Optional)
- Camera scripts (possibly used for reference)
- Standard shaders and materials

### Custom Systems

#### EventBus (Custom Event Management)
- Location: `Assets/_Scripts/Helpers/EventBus.cs`
- **~20+ Event Types:**
  - PlayerEnteredCircle
  - PassengerHandedBaggage
  - PassengerReachedTopStairs
  - BaggageReachedConveyorEnd
  - PlatformReachedTop
  - AllBaggagesLoadedToTruck
  - AreaUnlocked
  - BoardUnlocked
  - ...and more

#### Object Pool (Custom Pooling)
- Location: `Assets/_Scripts/Helpers/ObjectPool.cs`
- Generic pooling system for any GameObject
- Used for: Money coins, Passengers, Baggage items

#### Transfer System (Custom Economy)
- Location: `Assets/_Scripts/Helpers/Transfers.cs`
- ITransferTarget interface for unlock system
- TransferSettings struct contains cost and speed

---

## Configuration

### Game Manager Debug Settings
```csharp
public bool IsGameInSlowMo = false;     // Time.timeScale = 0.5
public bool IsGameInFastMo = false;     // Time.timeScale = 3.0
public bool ShowDetailedLogs = false;   // Console logging
```

### Player Settings (ProjectSettings.asset)
- **Product Name:** Panteon-PlayableAdCase
- **Company:** k2ann00
- **Supported Device Orientations:** All (Portrait first)
- **Default Screen Size:** 540 x 960 pixels
- **Target Platform:** Mobile (iOS/Android)
- **Scripting Backend:** IL2CPP

### Component Configuration Reference

#### PlayerController
```csharp
Speed = 5f;                     // Movement speed
RotationSpeed = 10f;            // Rotation responsiveness
ClimbingHeight = 5f;            // Climb distance
```

#### PassengerManager
```csharp
QueueSpacing = 2f;              // Distance between passengers
PathUpdateInterval = 0.1f;      // Path calculation frequency
HandingBaggageDistance = 2f;    // Trigger distance for baggage
```

#### PlatformMover
```csharp
MoveDuration = 2f;              // Platform movement time
MoveEase = Ease.InOutSine;      // Animation easing curve
TopPositionThreshold = 0.1f;    // Position detection threshold
```

#### StairsController
```csharp
MoveDuration = 0.3f;            // Step animation duration
MoveDelay = 0.1f;               // Delay between steps
LoopCount = 3;                  // Animation loop cycles
```

#### MoneyManager
```csharp
TransferSpeed = 100f;           // Money transfer rate per frame
CoinValue = 50;                 // Coins spawned per amount
CoinAnimationDuration = 0.5f;   // Coin tween duration
```

#### BarFill (Unlock System)
```csharp
UnlockCost = 500;               // Money needed to unlock
TransferSpeed = 100f;           // Progress bar fill speed
AnimationDuration = 0.5f;       // Completion animation
```

#### CameraController
```csharp
FollowOffset = new Vector3(-2.45f, 9.04f, 8.06f);
FollowSpeed = 5f;               // Smoothing factor
LookAtOffset = new Vector3(0, 1.5f, 0);
DefaultFOV = 60f;
BoardFOV = 85f;
```

### Material Settings
- **AtlasMaterial.mat** - Main character/environment texture atlas
- **Post-Processing** - Configured in scene for visual polish

### Sprite Configuration
- **Logo-BG.png** - Background logo (540x960)
- **Outlined-Icon.png** - UI button icons
- Sprites set to UI mode with appropriate settings

---

## Development Notes

### Important Files for Modification

1. **GameManager.cs** - Core game state and debug controls
2. **PassengerManager.cs** - Passenger spawning and queue system
3. **MoneyManager.cs** - Economy and unlock balancing
4. **EventBus.cs** - Add new game events here
5. **BaggageUnloadManager.cs** - Baggage processing logic
6. **CameraController.cs** - Camera behavior and cinematics

### Adding New Features

#### To add a new circle area:
1. Create WaitingAreaController with new CircleType
2. Add new CircleType enum value to EventBus
3. Subscribe to PlayerEnteredCircle event
4. Add logic in relevant Manager

#### To add a new unlock:
1. Create new ITransferTarget implementation
2. Add UI BarFill element
3. Configure UnlockCost in BarFill
4. Implement OnTransferCompleted() logic

#### To add a new event:
1. Define new event in EventBus.cs
2. Create raise method: `public static void Raise...() { ... }`
3. Subscribe in relevant systems
4. Raise event when condition met

### Performance Optimization Tips

- Object pooling is already implemented for common objects
- DOTween is used for efficient animations (not LateUpdate-based)
- EventBus reduces Update() polling
- Consider adding more pooling for frequently created objects
- Mobile optimization: Profile on target device

---

## Common Issues & Solutions

### Passenger not picking up baggage
- Check WaitingAreaController trigger is properly configured
- Verify PassengerController is subscribed to events
- Check CircleType matches expected enum value

### Baggage stuck on conveyor
- Verify PlatformMover is receiving events
- Check BaggageMover.Initialize() is called with correct parameters
- Ensure DOTween is installed and paths are valid

### UI money text not updating
- Check UIManager.ThrottledUpdate() frequency
- Verify MoneyManager is raising events
- Check Canvas reference in UIManager

### Camera not following player
- Ensure CameraController is in scene
- Check Player tag is set correctly
- Verify CameraController.FollowOffset is configured

---

## Next Steps for Development

1. **Balancing:** Adjust economy values (earn rate, unlock costs)
2. **Content:** Add more passenger types and baggage variants
3. **Visuals:** Enhance particle effects and animations
4. **Audio:** Implement sound effects and background music
5. **Analytics:** Add event tracking for player behavior
6. **Monetization:** Integrate ad system and in-app purchases
7. **Testing:** Profile on target devices for performance

---

## References & Resources

- **Unity Version:** 2022 LTS or later
- **Scripting Language:** C# 9.0+
- **DOTween Documentation:** http://dotween.demigiant.com/
- **Event Aggregator Pattern:** Martin Fowler's architecture patterns
- **Object Pool Pattern:** Gang of Four design patterns

---

**Last Updated:** 2025-10-21
**Project Status:** Production Ready
**Code Organization:** Clean architecture with SOLID principles
**Maintainability:** Excellent - Well-documented and structured
