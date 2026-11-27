# Event Horizon / Wayfarer

A Unity space-adventure and civilization prototype with a custom C++ physics core, locally trained NPC motion predictions, whole planets, alien expeditions, a crew rover, ship design, city construction, native rocket launches, planetary wars and a Dyson collector project. Built while learning Unity and experimenting with AI-assisted development.

[![Graphics and production pass](docs/media/production-city-after.jpg)](docs/media/wayfarer-production-tour.mp4)

**[Download the Mac game — v0.4.0](https://github.com/suryanshsoni27/event-horizon-wayfarer/releases/tag/v0.4.0)** · **[Watch the new graphics tour](docs/media/wayfarer-production-tour.mp4)** · **[Player guide](docs/TWELVE_HORIZONS.md)**

macOS / Apple Silicon · Keyboard/mouse or DualSense · Local play with no API key

## v0.4.0: graphics and production fixes

Terrain now stitches between detail levels, colony platforms meet graded ground, and cloud layers clear mountain peaks. Physically based ship and suit materials use licensed texture maps, an original HDR reflection environment, closed pressure hulls and beveled armor. Alien warships have their own crescent silhouette. Walk and sprint animation adds slope-aware feet and pelvis adjustment.

The fourth settings page controls anti-aliasing, bloom, field of view and shadows. Earlier preferences migrate while preserving controls, and a checkpoint created by the actual v0.3 release loads in this version. Defeat now releases the mouse and lets a controller return to save slots. See the [before/after captures, measured results and remaining production work](docs/PRODUCTION_PASS.md).

## Twelve Horizons: an expedition becomes a civilization

- **Twelve sequential chapters:** land, uncover an alien expedition, build a city, launch a survey rocket, customize a ship, settle new worlds, repel raids, fight planetary wars and confront an alien flagship. Every successive chapter increases actual enemy health and damage; later defenses add waves.
- **Real ship choices:** four original hulls, four coatings and twelve modules across propulsion, weapon, defense and cargo categories. Reactor budgets, costs and tradeoffs affect thrust, speed, hull, shields, heat, projectile behavior and cargo capacity. A live 3D preview shows your design.
- **Seven contract archetypes:** rescue, three-point survey, salvage, elite hunt, vulnerable orbital escort, intercolony courier and colony defense. The local adviser ranks jobs by your situation and explains its recommendation. Physical interactions and actual combat complete them.
- **Build and supply worlds:** eight district types, native-physics civilian shuttles, construction, shared energy, science and alloy production. Food, morale, colony policies, supply routes and timed crises give settlements needs beyond their skyline.
- **A contested frontier:** three rival civilizations develop industry, research and territory. Negotiate, fabricate reserve warships and join nearby native-physics fleet battles. Distant battles use a strategic approximation over the same health and outcome state.
- **Reach the star:** launch survey rockets through the native solver, progress through six civilization tiers and build twelve Dyson collector sectors with 672 panels. The final chapter combines three worlds, the completed project and an alien flagship battle.
- **Keep your expedition:** three independent campaign slots, checksums, atomic saves and validated backups restore the ship, crew, rover, world catalogue, mission progress, routes and civilization. Continue an existing slot or import the earlier New Dawn civilization.
- **Improve the feel:** remappable keyboard and digital controller actions, deadzone and sensitivity controls, aim assistance, toggle interaction, contrast and text-size options, three graphics presets, frame-rate targets, synthesized music and spatial sound, revised materials, stellar coronas, pooled impacts and ground dust.

The [Twelve Horizons guide](docs/TWELVE_HORIZONS.md) lists every chapter, controls, fitting rules, survival systems and checkpoint limits. [New Dawn's guide](docs/NEW_DAWN.md) explains the underlying city, shipwright, diplomacy and Dyson systems.

## Explore, land, drive

- Travel through 32 systems and 128 catalogue destinations, with additional procedural frontier systems.
- Approach black holes with a distant accretion-disk view and a nearby numerical Schwarzschild lensing shader.
- Fly among asteroids, satellites, a research station, wrecks and comet effects.
- Land on complete procedural planets, deploy six tires and drive with four astronauts aboard.
- Disembark, walk or run, fight alien creatures, recover a recorder, scan specimens, defend a relay and defeat a guardian.
- Reboard the parked rover, retract the tires and return to flight.
- Play using keyboard/mouse or a PS5 DualSense controller.

This remains prototype gameplay and procedural art. Hand-authored environments and characters, a full manual campaign balance pass and broader hardware testing remain production work. Galactic distances are compressed, planets are small gameplay worlds, and vehicle/contact behavior is simplified.

## Physics and NPC architecture

**C++ simulation, C# gameplay.** A native library handles 3D Newtonian gravity, bounded thrust, atmospheric drag, terrain/boulder contact and projectile motion. Unity's C# layer handles gameplay, encounters, controls, cameras and presentation. Rover driving adds tangent traction, side grip, steering, braking, a static parking brake and terrain alignment. The solver runs in 0.01-second gameplay steps.

**A learned dynamics model.** `tools/train.py` collects 40,000 state-action transitions from the native simulator and fits a radial-feature regression model to acceleration. It evaluates an independent 8,000-transition holdout and exports weights into Unity resources. Trained weights are included, so retraining is optional.

**Prediction feeds planning.** Each space NPC has its own physical state and programmed goals. Its planner evaluates 15 candidate thrust vectors over a 2.4-second horizon, scores progress and hazards, applies the selected action, and replans. Free-space predictions use the learned model; atmosphere and terrain/contact predictions use the native solver. Larger gravity sources use normalized coordinates and force scaling.

**A separate danger learner.** A six-feature logistic model starts with 4,000 simulated projectile encounters and updates from delayed encounter outcomes during gameplay. Its risk estimates influence planning costs. Online updates change the model; they do not guarantee improved play. Surface aliens use programmed steering and attack behaviors.

The NPC architecture combines learned motion prediction with programmed planning. Black-hole gravity uses a simplified Newtonian gameplay field; the lensing shader is separate from that solver. This is not a full relativistic, N-body, aerodynamic or multi-body tire simulation.

![Black-hole rendering](docs/media/cosmos-near-black-hole.png)

## Build on macOS / Apple Silicon

Verified editor: **Unity 6000.6.10f1**. The native photo chooser uses macOS frameworks; other desktop platforms have not been validated.

Requirements: Unity with macOS build support, Apple Command Line Tools, CMake, and Python 3 with the packages in `requirements.txt`. FFmpeg is optional for recording tools. No API keys or `.env` file are needed.

```sh
python3 -m venv .venv
source .venv/bin/activate
python3 -m pip install -r requirements.txt
mkdir -p evidence
cmake -S native -B native/build
cmake --build native/build
```

The native build generates libraries under `UnityProject/Assets/Plugins/macOS`. Generated libraries, Unity caches and player builds are ignored by Git.

Open `UnityProject` in Unity. To create the standalone app with the verified Unity Hub installation:

```sh
'/Applications/Unity/Hub/Editor/6000.6.10f1-arm64/Unity.app/Contents/MacOS/Unity' \
  -batchmode -nographics -projectPath "$PWD/UnityProject" \
  -executeMethod BuildDemo.Build -quit \
  -logFile "$PWD/evidence/unity-build.log"
open 'Build/Event Horizon.app'
```

`BuildDemo.Build` generates the gameplay scene and builds the app. To also regenerate model weights and run the original native/training checks, use `zsh tools/build.sh` after activating the Python environment.

## Controls

Connect the DualSense through USB or [pair it with macOS Bluetooth](https://www.playstation.com/en-us/support/hardware/pair-dualsense-controller-bluetooth/). Release all controls after connecting or resuming. The pause screen displays the detected device. Controller disconnection pauses the game.

| Action | Keyboard / mouse | DualSense |
|---|---|---|
| Confirm menu / pause | Enter; Esc or F1 | Cross; Options |
| Save / checkpoint menu / contracts | F5 / F9 / Tab | Through pause menu |
| Flight look | Mouse / arrows | Right stick |
| Flight thrust / brake | W/S throttle; Space brake | R2 thrust; L2 brake |
| Flight strafe / vertical thrust | A/D; Z/X | Left stick |
| Roll / boost / ship weapon | Q/E; Shift; left click | L1/R1; hold Cross; Square |
| VTOL descent | K | D-pad up |
| Deploy / retract tires while stopped | 6 | D-pad down |
| Rover drive / reverse; steer | W/S; A/D | R2/L2; left stick |
| Rover handbrake / camera | Space; mouse / arrows | Cross; right stick |
| Exit / board while parked | J | Triangle |
| Walk / look | WASD; mouse / arrows | Left / right sticks |
| Run / hop | Shift or Caps Lock; Space | L3 toggle; Cross |
| Carbine fire / aim / reload | Left / right click; R | R2 / L2; Square |
| Interact / suit pulse / journal | Hold E; Q; O | Hold R1; Circle; L1 |
| Stellar atlas | M | Create / Share |
| Cruise | T engages / cancels | Cross in atlas; L2 cancels |
| Select black hole / orbital service | 9 / 0 | D-pad left / right |
| Salvage workshop | U; 1–4 fit | R3; D-pad selects, Cross fits |
| Session crew-photo manager | H | Native chooser uses mouse / keyboard |

In the atlas, left/right or L1/R1 changes systems, up/down selects a destination, Cross engages cruise and Circle returns. Circle also closes the journal and workshop. After defeat, restore the campaign checkpoint from the defeat screen.

Land and settle below 1.5 m/s before deploying tires or leaving the craft. The rover parks while the crew explores. Return within 6 m to board, then retract the tires before flight. Cruise requires clearing the atmosphere.

Controller tests use injected DualSense HID reports and a virtual gamepad. A physical DualSense was not connected during verification; USB/Bluetooth feel, connection quality and device rumble remain unverified. Basic rumble is implemented through Unity controller support; adaptive triggers and gyro controls are not implemented. Keyboard and fourteen digital controller actions can be remapped in settings; analog trigger roles remain fixed.

![Exploring a planet on foot](docs/media/rover-crew-running.png)

## Verification

The v0.4.0 packaged player passed **378 automated checks**: 33 graphics/production, 87 campaign, 64 civilization and the original 194 checks. The original suites cover 29 gameplay, 20 crew/planet, 33 large-world, 33 surface mission, 28 space/animation and 51 controller/rover checks. Production QA measures terrain continuity and platform contact, exercises animation and live graphics controls, migrates preferences and restores an actual v0.3 checkpoint. Campaign and civilization QA cover chapter gates, increasing difficulty, native landing, rover transitions, physical contracts, fitting, economy, native rockets and fleet combat. Reviewed results live in [docs/validation](docs/validation/production-validation.json).

After building and closing the normal player:

```sh
python3 tools/check_production.py
python3 tools/check_voyager.py
python3 tools/check_civilization.py
python3 tools/verify_game.py
python3 tools/benchmark_voyager.py
python3 tools/benchmark_voyager.py --ultra --width 1920 --height 1080
python3 tools/record_production.py
```

The [Balanced report](docs/validation/production-performance-balanced.json) and [Ultra report](docs/validation/production-performance-ultra.json) sample flight, a city, native fleet combat and the Dyson project on Apple M4 hardware with a 60 FPS target. Each records 360 frames per scene after warmup, actual resolution and render scale, anti-aliasing, frame-time percentiles, available GPU timing and Unity allocated memory. These are short staged local samples; traversal, long-term stability and other machines need separate testing. Automated progression checks do not replace a full manual balance playthrough.

The new [50-second graphics tour](docs/media/wayfarer-production-tour.mp4) shows ship designs, graded terrain, walking and sprinting, alien fleet combat, planetary atmosphere and graphics settings at 1920×1080 and 30 FPS. It uses seeded resources, scripted inputs and staged cameras, labeled on screen. Simulation advances at a fixed 1/30 second per captured frame. The original ambient score is assembled separately. It is not a performance benchmark or a live-audio recording. The [manifest](docs/validation/production-record-manifest.json) includes measured simulation timing for each segment.

The [72-second Twelve Horizons walkthrough](docs/media/twelve-horizons-tour.mp4) shows chapters, city infrastructure, native rocket ascent, ship outfitting, contracts, settings, fleet combat and the Dyson structure. Seeded resources, scripted inputs and staged cameras are labeled on screen. Capture is 1280×800 at 12 FPS, and the original ambient score is assembled separately. It is not an FPS benchmark, live-audio capture or uninterrupted manual campaign. The [manifest](docs/validation/voyager-record-manifest.json) records scene timings. Recording and QA protect normal campaign files.

Earlier showcases remain available: [1:54 New Dawn](docs/media/new-dawn-civilization-tour.mp4) and the [3:23 exploration tour](docs/media/wayfarer-full-feature-tour.mp4).

Additional native and model checks live under `tools/`. Player QA and recording commands write to the local, ignored `evidence/` directory. Reproduce the rover tour with `python3 tools/record_rover.py` and FFmpeg installed.

The original exploration tour covers atlas navigation, continuous flight and landing, tire deployment, driving and crew exploration, all linked surface missions, extraction and ascent, satellite scanning, station repair, derelicts, native projectile mining, upgrades, comets, both black-hole renderers, capture, space NPC combat, physics/model architecture and local crew portraits. Captions identify staged scenes and accelerated sections. Gameplay sources were captured at 12 fps and edited at 24 fps in a 1920×1080 layout, with an original locally synthesized ambient score. The portrait insert uses a generated synthetic robot in the current local crew manager. The cut joins separate sessions and is not an uninterrupted manual playtest.

After building, close the normal player and run `python3 tools/make_feature_tour.py --record` to capture and assemble the tour. Without `--record`, the tool reuses recordings under `evidence/`. The optional `--cosmos-record --feature-inserts` player flags stage the atlas, orbital, mining and workshop close-ups; ordinary gameplay is unchanged. Timing varies with recording conditions, so review the mission cuts after recapturing.

The included rover demo starts from a staged valid landing, uses injected gamepad input and native driving, and protects crew health after clearing the initial patrol. It demonstrates vehicle transitions rather than a physical controller playtest, an uninterrupted landing journey or an FPS benchmark. The black-hole still comes from an earlier staged rendering showcase.

## Source layout

| Path | Contents |
|---|---|
| `native/` | C++ physics, terrain/contact and macOS photo chooser |
| `UnityProject/Assets/Scripts/` | Gameplay, NPC planning, rover, controller input and procedural animation |
| `UnityProject/Assets/Resources/` | Shaders, trained model weights, world configuration and surface textures |
| `UnityProject/Assets/Editor/BuildDemo.cs` | Scene generation and macOS player build |
| `tools/` | Training, native validation, packaged QA and recording scripts |
| `docs/` | Reviewed public media and numerical results |

Surface texture credits and CC0 links are in [THIRD_PARTY_ASSETS.txt](THIRD_PARTY_ASSETS.txt). Other geometry, gameplay and procedural art are project code. See [SECURITY.md](SECURITY.md) for the boundary around credentials, local photos and developer logs.
