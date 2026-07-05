# HelperKH

**A comprehensive Unity utilities package for game development**

HelperKH is a robust collection of systems and extensions that I designed throughout my journey with Unity to accelerate game development. It provides battle-tested solutions for health management, data persistence, animation, performance optimization, and editor workflows.

---

## 📦 What's Inside

### 🔥 Core Systems

| System | Description |
|--------|-------------|
| **Health Controller** | Full-featured health management with damage, healing, death/revive events, and Damage Over Time (DOT) support |
| **Save System** | Secure save/load with AES-256 encryption, backup recovery, and version migration support |
| **Time Manager** | Global time scale control with debugging multiplier for testing slow-motion or speed-up |
| **System Hub** | Centralized update manager for better performance and controlled execution order |

### 🎨 Animation & UI

| System | Description |
|--------|-------------|
| **Sprite Animator** | Frame-based sprite animation with loop control and frame callbacks |
| **Sprite Size Matcher** | Auto-resizes UI images to match sprite dimensions with preserveAspect enforcement |
| **Hierarchy Color Header** | Visual hierarchy organization with custom-styled header objects |

### 🛠️ Editor Tools

| Tool | Description |
|------|-------------|
| **Time Scale Window** | Editor window for quick time scale adjustments (Tools > KH) |
| **UI Animator Editor** | Custom inspector for UI animation settings |
| **List Utilities** | Prevent duplicates, match list counts, auto-fill from asset database |

### 🔧 Extensions & Utilities

| Category | Features |
|----------|----------|
| **Collection Extensions** | ForEach, Find, FindAll, PickRandom, IsEmpty with index support |
| **Transform Utilities** | Direction/angle calculations, move towards, destroy children |
| **Distance Utilities** | Optimized squared distance checks with threshold comparisons |
| **Debug Logging** | Colored console logs with caller info (file, method, line number) |
| **Timer System** | Accurate timer with formatted output (seconds, minutes, hours) |
| **Cursor Management** | Custom cursor texture with hotspot alignment |
| **Mouse Utilities** | Get mouse world position, detect UI hover |
| **Sorting Order** | Auto-update sprite sorting order based on Y position |

### 📊 Data Management

| Feature | Description |
|---------|-------------|
| **Save Migration** | Version-based data migration for save files |
| **Serializable Structs** | `KHDamage` struct with operator overloading for damage calculations |
| **List Management** | Extension methods for list manipulation and cleaning |

---

## 🎯 Key Use Cases

- **RPG/Action Games**: Health, damage, and save systems
- **Mobile Games**: Performance-optimized utilities
- **UI-Heavy Applications**: Animation tools and sprite management
- **Editor Tool Development**: Custom inspectors and utilities
- **Cross-Platform**: Works with Unity's new Input System

---

## 💡 Quick Examples

```csharp
// Health & Damage
var health = new KHHealthController(owner, 100, 100);
health.ApplyDamage(new KHDamage { 
    damage = 25, 
    overTimeDamagePercent = 0.2f, 
    duration = 3f 
});

// Save & Load
KHSaveSystem.Save(gameData);
var loadedData = KHSaveSystem.Load<GameData>();

// Animation
animator.PlaySpriteAnimation(frames, loopCount: -1, onFrame: i => UpdateFrame(i));

// Collection Operations
list.KHForEach((item, index) => ProcessItem(item, index));
var randomItem = list.KHPickRandom();

// Timer
if (timer.DidExceed(5.0)) { /* 5 seconds passed */ }

// Debug Logging
KHDebug.Log("Player teleported", XMLC.Green);
KHDebug.LogWarning("Low health warning", XMLC.Yellow);
KHDebug.LogError("Critical error", XMLC.Red);

// Performance
if (Kh.SqrDistanceIsLessThan(player, enemy, 10f)) { 
    /* Within 10 units */ 
}
```

---

## 🚀 Performance Optimizations
Squared Distance Checks - Avoid expensive square root calculations

Batched Operations - Process large loops in batches to avoid frame drops

Pooled Event Listeners - Efficient listener management with safe iteration

Sorting Order Caching - Only update when Y position significantly changes

---

## 📋 Summary
HelperKH is your complete Unity development toolkit, providing:

- ✅ 8+ Core Systems - Health, Save, Time, Animation, Timer, and more
- ✅ 50+ Extension Methods - Collections, Transform, Debug, and utilities
- ✅ 3 Editor Windows/Tools - Time Scale, UI Animator, Hierarchy Headers
- ✅ Performance Optimized - Squared distances, batched operations
- ✅ Production Ready - Error handling, backup recovery, migration support

---

## 📦 Installation

1. Copy the following URL: https://github.com/Kh4ht/HelperKH.git
2. In unity, select **Window** > **Package Manager**
3. Click the **Add (+)** icon button in the top-left corner of the Package Manager window.
4. Select **Add package from git URL...** (or "Install package from git URL") from the drop-down menu.
5. Paste the GitHub URL into the text box and click **Add** (or Install).

---

## 📝 Requirements
Unity 2021.3 or higher

Input System package (for mouse position utilities)

.NET 4.x runtime

---

## 📄 License
MIT License - See LICENSE file for details

Made with ❤️ for Unity developers

