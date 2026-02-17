# Marching Band: Halftime Show - Project Setup Guide

## Prerequisites

1. **Unity Hub** - Download from https://unity.com/download
2. **Unity 6 LTS** (6000.0.x) - Install via Unity Hub
   - In Unity Hub > Installs > Install Editor
   - Select **Unity 6 (6000.0 LTS)**
   - Modules to include:
     - **WebGL Build Support** (required for web target)
     - **Windows Build Support** (if on Mac, for cross-platform)
     - **Visual Studio** or **VS Code** integration

## Project Creation

Since the codebase (scripts, folder structure) already exists in this repository,
you need to create the Unity project around it:

### Option A: Create project via Unity Hub (Recommended)

1. Open Unity Hub
2. Click **New Project**
3. Select **Unity 6 (6000.0 LTS)** as the editor version
4. Choose the **3D (URP)** template
5. Set project name to `mbhs-game`
6. Set location to the **parent** of this directory
   - Unity Hub will create `mbhs-game/` with ProjectSettings, Packages, etc.
   - If the directory already exists, Unity will initialize the project within it
7. Click **Create Project**

### Option B: Initialize in existing directory

1. Open Unity Hub
2. Click **Add** > **Add project from disk**
3. Navigate to this directory
4. If Unity doesn't recognize it as a project, create a new project (Option A)
   and then copy the `Assets/_Project/` folder into the new project

## After Project Creation

### 1. Install Required Packages

Open **Window > Package Manager** and install:

Already included with URP template:
- Universal RP (should be pre-installed)
- Shader Graph (comes with URP)

Install via Package Manager:
- **Cinemachine** - Search "Cinemachine" and install
- **Addressables** - Search "Addressables" and install
- **Input System** - Search "Input System" and install
  - When prompted, click "Yes" to enable the new Input System
- **TextMeshPro** - Should auto-install; if not, search and install
- **Animation Rigging** - Search "Animation Rigging" and install
- **Timeline** - Search "Timeline" and install
- **ProBuilder** - Search "ProBuilder" and install (optional, for prototyping)

### 2. Configure Project Settings

#### Player Settings (Edit > Project Settings > Player)
- Company Name: `MBHS Game`
- Product Name: `Marching Band Halftime Show`
- Color Space: **Linear** (should be default with URP)
- Scripting Backend: **IL2CPP** (for PC builds)

#### Quality Settings (Edit > Project Settings > Quality)
- Create 3 quality levels: Low, Medium, High
- Assign appropriate URP Pipeline Assets to each

#### Input System
- If using the new Input System, go to Edit > Project Settings > Player
- Set Active Input Handling to **Both** (supports old and new)

### 3. Create Scenes

Create the following scenes in `Assets/_Project/Scenes/`:
1. **Boot.unity** - The initialization scene
   - Create an empty GameObject called "GameBootstrapper"
   - Add the `GameBootstrapper` component to it
   - Set this as Scene 0 in Build Settings
2. **MainMenu.unity** - Main menu
3. **BandManagement.unity** - Team management screen
4. **FormationEditor.unity** - Drill chart editor
5. **ShowSimulation.unity** - Performance playback

### 4. Set Build Settings

Go to **File > Build Settings**:
1. Add scenes in order: Boot, MainMenu, BandManagement, FormationEditor, ShowSimulation
2. Boot.unity must be at index 0
3. For WebGL testing: Switch platform to WebGL

### 5. URP Configuration

The URP template should set this up, but verify:
1. Check `Assets/_Project/Settings/URP/` for pipeline assets
2. In Edit > Project Settings > Graphics, ensure a URP Pipeline Asset is assigned
3. In Edit > Project Settings > Quality, ensure each tier has a pipeline asset

## Project Structure

```
Assets/_Project/
├── Scripts/          # All C# code organized by system
│   ├── Core/         # ServiceLocator, EventBus, StateMachine
│   ├── Data/         # Models and ScriptableObjects
│   ├── Systems/      # Game systems (Band, Formation, Music, etc.)
│   ├── UI/           # UI controllers and styles
│   └── Editor/       # Unity Editor extensions
├── Art/              # Visual assets (models, textures, sprites)
├── Audio/            # Music, SFX, beat maps
├── Animation/        # Animator controllers and clips
├── Data/             # ScriptableObject instances
├── Prefabs/          # Reusable prefab assets
├── Scenes/           # Unity scenes
├── Settings/         # URP, Input, Addressables config
├── Tests/            # Unit and integration tests
└── UI/               # UI Toolkit documents and styles
```

## Running Tests

1. Open **Window > General > Test Runner**
2. Select **EditMode** tab
3. Click **Run All** to run unit tests
4. Tests cover: FormationSystem, ScoringEngine

## Next Steps

After setup, the Phase 1 development priorities are:
1. Verify all scripts compile without errors
2. Create the Boot scene with GameBootstrapper
3. Start building the Formation Editor UI
4. Create placeholder ScriptableObject instances for instruments and schools
