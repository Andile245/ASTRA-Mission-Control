# ASTRA: Mission Control

ASTRA is a playable Unity 6.6 URP prototype for the NASA Space Apps Challenge Junior Astronaut Mission Trainer concept. Open this project in Unity Editor `6000.6.9f1`. The editor bootstrap automatically generates `Assets/_Project/Scenes/ASTRA_Bootstrap.unity` when the project opens and the scripts finish compiling. If it does not, select **ASTRA > Build First Playable Scene**, then open the scene and press Play.

## Quick play

Open `Prototype/index.html` in a browser for the offline isometric demo. It has the same first-mission loop and runs without a server or downloads. At the welcome screen, choose **Show me how to play** for the optional three-step guide or **I know how to play — skip guide** to start directly. The guide can be replayed from the mission briefing. The Unity welcome screen offers the same guide-or-skip choice. The Unity project and scene builder are under the project root.

## First playable loop

- Explore a stylized 3D lunar outpost in third person.
- Activate communications and a solar array, inspect life support, and collect a virtual sample with the rover.
- Monitor oxygen, water, food, power, and habitat condition.
- Respond to a simulated power shortage and review the mission score and learning feedback.

Controls: **WASD** move, **Shift** sprint, **Space** hop, **right mouse drag** look, **E** interact, **Escape** pause. Walk up to the cyan/green/orange facility beacons until the interaction card appears.

All resource values and event outcomes are invented game rules for learning. The outpost, terrain, and event are fictional and are not real astronaut procedures. See [NASA science notes](Docs/SCIENCE-SOURCES.md) for the source material and limits.

## Project layout

```text
Assets/
  Editor/AstraSceneBuilder.cs        # one-click scene and environment generation
  Settings/                          # Unity 6.6 URP template settings
  _Project/
    Art/Materials/                   # generated scene materials
    Scenes/                          # ASTRA_Bootstrap.unity
    Scripts/Core/                    # mission state and shared types
    Scripts/Player/                  # astronaut control and camera
    Scripts/UI/                      # runtime mission HUD and decisions
    Scripts/World/                   # facility interaction component
```

## Generate / regenerate the scene

In Unity, select **ASTRA > Build First Playable Scene**. This creates the lunar environment, outpost facilities, astronaut, mission manager, HUD and the `ASTRA_Bootstrap` scene, and adds it to the Windows build scene list. If the project is not yet open in Unity, use Unity Hub to add the folder at the project root.

## Phase 1 and prototype verification

The project settings and packages are seeded from the installed Unity 6.6 URP template: URP 17.6.0, Input System 1.20.0 and uGUI 2.6.0. Unity Hub opened this project in Editor `6000.6.9f1`, resolved the license entitlement and packages, and generated `Assets/_Project/Scenes/ASTRA_Bootstrap.unity`. The complete mission loop has now been played through in Unity 6.6 Play Mode: all five objectives completed, the power decision was resolved, the sample was collected, and the mission report appeared. HUD text was enlarged for readability in the Game view. The Windows player was rebuilt after Phases 2–5 and Unity reported a successful 55-second build-and-run.

## Phase 2: Lunar environment and lighting

The saved bootstrap scene now uses a generated lunar regolith mesh with six crater bowls and rims, procedural elevation outside the central outpost, two distant highland silhouettes, and a black airless sky. Warm directional sunlight casts soft shadows against low blue ambient fill; Earth and distant stars are visible from the play area. The outpost's central ground remains level for movement and facility interactions. The mesh terrain uses one collider and generated mesh assets so it remains practical for a junior-focused prototype. This is original fictional geometry inspired by NASA's south-pole environment descriptions, not a NASA terrain dataset or mapped Shackleton site. The updated scene has been regenerated and entered in Play Mode; the welcome screen and tutorial skip are working with the new environment.

## Phase 3: Astronaut character and movement

The astronaut keeps the WASD, Shift, Space, and E controls. A new lightweight `AstraAstronautMotion` component adds idle breathing, alternating arm and leg swings while walking, and a raised-leg hop pose. The suit now has pivoted arms and separate legs and boots. Movement continues to use the CharacterController and the simplified 1.62 m/s² lunar gravity value; these are game mechanics, not suit engineering. Code: `Assets/_Project/Scripts/Player/AstraAstronautMotion.cs`; scene assembly: `Assets/Editor/AstraSceneBuilder.cs`.

## Phase 4: Camera and object interactions

The third-person camera now eases into position and sphere-casts to avoid scenery obscuring the astronaut. Facility range checks use the closest point on each interaction collider and reject interactions blocked by another object. Existing `E` interaction and on-screen nearby-facility prompt remain in place. Code: `Assets/_Project/Scripts/Player/AstraPlayerController.cs`; prompt: `Assets/_Project/Scripts/UI/AstraHUD.cs`.

## Phase 5: Lunar outpost facilities

Added an oxygen processor, water recycler, and research laboratory beside the existing habitat, communications station, solar array, life-support system, greenhouse, and rover. Interacting with the processor/recycler exchanges power for oxygen/water game units; the research lab explains the virtual sample. These interactions run through the existing facility manager and prompt system. The new resource exchange values are fictional game rules. Scene assembly: `Assets/Editor/AstraSceneBuilder.cs`; actions: `Assets/_Project/Scripts/Core/AstraGameManager.cs`.



## Phase 6: Resource management and warning feedback

Added a dedicated resource manager for oxygen, water, food, power, and habitat reserves. It applies steady consumption, solar recharge, life-support efficiency, and shortage effects to habitat condition and facility availability. The HUD now shows persistent, color-coded low and critical alerts with a recovery action, while meter colors also reflect resource severity. These rates and outcomes are fictional learning-game mechanics, not operational life-support guidance. Code: Assets/_Project/Scripts/Core/AstraResourceManager.cs, Assets/_Project/Scripts/Core/AstraGameManager.cs, and Assets/_Project/Scripts/UI/AstraHUD.cs.
