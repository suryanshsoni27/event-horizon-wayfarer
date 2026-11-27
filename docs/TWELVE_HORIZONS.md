# Twelve Horizons — player guide

Twelve Horizons adds a twelve-chapter campaign to Event Horizon / Wayfarer: your first landing becomes a network of cities, fleets and stellar collectors. Chapters advance when their objectives are complete, reward resources once, and save a checkpoint. You can keep exploring after the finale.

## Download and play

Download `Event-Horizon-v0.5-macOS-AppleSilicon.zip` from the [v0.5.0 release](https://github.com/suryanshsoni27/event-horizon-wayfarer/releases/tag/v0.5.0), unzip it, and open `Event Horizon.app`. This build runs on Apple Silicon Macs. It is not a PS5 console package; a DualSense controls the Mac game over USB or Bluetooth. The [v0.4 graphics pass](PRODUCTION_PASS.md) and [v0.5 face-flight update](FACE_FLIGHT.md) preserve the twelve-chapter campaign and existing checkpoints.

The Mac app has a local ad-hoc signature; it is not Developer ID signed or notarized. If macOS blocks it, review the source and release, then use **System Settings → Privacy & Security → Open Anyway** for this app. The release includes a SHA-256 checksum. No account, API key, environment file or online model service is needed to play.

## Your first expedition

1. Select one of three save slots, then **New campaign**. An occupied slot requires a second confirmation before replacement. **Continue** restores that slot's checkpoint. An empty slot can import a previous New Dawn civilization when one is found; the old file is preserved.
2. The craft begins an assisted VTOL descent. Follow the current-step hint, settle below 1.5 m/s, and deploy the six tires with **6 / D-pad down**.
3. Exit the parked rover with **J / Triangle**. Walk with WASD / left stick, look with the mouse / right stick, and sprint with Shift or the L3 run toggle.
4. Follow the recorder marker. Hold **E / R1** to interact. The journal explains the linked specimen, relay and Warden expedition.
5. Return within 6 m of the rover to board. Retract the tires before flying. Build on foot using **B / D-pad down → City Planner**; the [construction guide](NEW_DAWN.md#economy-and-construction) covers district recipes and placement.

**Escape / F1 / Options** opens the campaign pause menu. **Tab** opens frontier contracts, **F5** saves, and **F9** opens the checkpoint menu. Pause gives controller access to contracts, outfitting, colonies, chapters and settings. **B / L3** opens civilization command while flying or driving. The atlas remains **M / Share**.

## The twelve chapters

Objectives accumulate across the campaign. For example, earlier contracts and defensive victories count when a later chapter asks for them. District requirements count completed construction.

| Chapter | Objective |
|---|---|
| 1. First Light | Land, deploy tires, disembark and recover the flight recorder. |
| 2. Echoes Below | Scan specimens, defend the relay, defeat the Warden and extract to the ship. |
| 3. A Home Among Stars | Complete a command spire, foundry, solar farm and observatory. |
| 4. Torch to Orbit | Complete a launch complex and launch an Aurora survey rocket above 1 km. |
| 5. The Vanguard | Complete a shipyard, fit a nonstandard ship module and assemble three reserve warships. |
| 6. Across the Black | Own two worlds, visit two worlds and finish three frontier contracts. |
| 7. Broken Accord | Win a planetary war and personally destroy two hostile warships. |
| 8. The Siege | Win two defensive raids, complete two defense batteries and enable a supply route. |
| 9. Star Harvest | Complete four Dyson sectors and six frontier contracts. |
| 10. War of Three Suns | Own three worlds, win three wars and personally destroy six hostile warships. |
| 11. The Long Blackout | Complete eight Dyson sectors, resolve two crises and complete five supply deliveries. |
| 12. Light of Civilization | Complete all twelve Dyson sectors, own three worlds and defeat the alien flagship. |

Every new chapter raises actual hostile health by 7.5% of the starting baseline and damage by 5.5%. Relative to chapter 1, chapter 12 enemies have 1.825× health and 1.605× damage at the same difficulty. Later relay defenses add waves. The finale introduces a larger, heavily armored alien flagship with faster attack phases; launch its assault from the chapter screen with at least eight collector sectors and four reserve warships.

Choose **Explorer, Wayfarer or Veteran** in settings. These modify enemy health and damage while preserving the chapter order. On the title screen, **G / Triangle** cycles Frontier, Conflict and Scarcity starts. Conflict begins with fewer alloys and worse relations; Scarcity reduces alloy and energy reserves.

## Make the ship your own

Civilization command's Shipwright retains four hull silhouettes and four coatings. **Pause → Ship outfitter** adds four module categories, each with three choices. The live preview shows the hull, coating and fitted equipment.

| Category | Choices and tradeoffs |
|---|---|
| Drive | Vector baseline; Ion sprint adds thrust and speed with more heat; Armored freight adds hull while reducing thrust. |
| Weapon | Pulse repeater; a fast, heavy single-shot rail accelerator; a five-pellet short-range scatter battery. |
| Defense | Balanced regenerating shield; a larger shield with reduced thrust; reactive armor with a smaller shield and lower speed. |
| Hold | Survey hold; expanded freight capacity with reduced thrust; a smaller research hold that raises contract science rewards. |

Each fitting consumes reactor capacity. Larger hulls provide more reactor power. The outfitter checks power, cargo capacity and purchase costs; it will not let you reduce the hold below the cargo aboard. Fit equipment while stopped with tires retracted. Hull changes preserve the current health fraction. Shields absorb incoming damage and regenerate after a delay; excessive weapon heat prevents firing until the ship cools.

Salvage upgrades remain available through **U / R3**. Station repair restores the upgraded hull up to its actual maximum.

## Frontier contracts and the local adviser

The board offers six jobs at a time, allows three active contracts, and explains why it recommends each one. Recommendations respond to science, alloy, diplomatic relations, food shortages and raids. This is a local utility-ranked system, separate from the learned motion model used by space NPC planners.

- **Rescue:** assist a stranded surveyor, reserve cargo space, then return to your ship.
- **Geological survey:** reach and scan three separate surface signals.
- **Salvage:** retrieve a guarded cache and return the cargo to your ship.
- **Elite hunt:** find and defeat the marked alien target.
- **Escort:** stay within 220 m of a vulnerable shuttle through three orbital waypoints. Hostile projectiles can destroy it.
- **Courier:** collect food at one colony, fly to another and deliver it. The receiving colony gains provisions.
- **Defense:** repel an actual raid on a colony.

Escort contracts become available after the early surface chapters; intercolony deliveries need a second colony. You can have one active escort at a time. Contract rewards include alloy, science, influence and faction reputation, and cannot be claimed twice. Accepted jobs survive checkpoint restoration.

## Keep the civilization alive

In **Pause → Colonies**, choose Balanced, Industry, Research or Provisioning for each world. Policies change output. Habitats contribute food production; population consumes food. Shortages lower morale, reduce industrial output and eventually reduce population. Provisioning increases food production at an industrial cost.

Create supply routes with **G / Triangle** in the colony screen. A successful 90-second route transfers 15 food from a sufficiently stocked source, records a delivery, and awards alloy. Manual courier contracts also replenish the destination. Use emergency provisions when a colony needs immediate relief.

The three rival civilizations grow their industry, research and territorial holdings while time runs. Relations, envoys, ceasefires, defenses and your own fleet influence the frontier. From the interplanetary age, periodic stellar, food or diplomatic crises require relief within three minutes. Failure has bounded resource and morale consequences. The chapter screen shows your current objectives and progress.

Explore alternative legacies alongside the story: Pathfinder rewards exploration and contracts, Concord rewards diplomacy and deliveries, Guardian rewards successful wars and defense, and Starforger rewards a completed Dyson project across three worlds. Story completion awards Wayfarer.

## Controls, comfort and graphics

**F7 / Settings → Face Flight** opens optional webcam steering. Turn, nod and tilt your head to steer. After calibrating your neutral face and two expressions, hold your mouth open to add throttle or smile broadly to brake. **F6** stops capture and returns to manual controls after pause; **F8** recalibrates. The camera starts only when enabled in the setup screen. Read the [calibration and privacy guide](FACE_FLIGHT.md) before using this experimental input mode.

Settings include mouse and stick sensitivity, deadzone, axis inversion, aim assistance, toggle interaction, toggle aim, rumble, camera motion, high contrast and text scale. Keyboard actions and fourteen digital gamepad actions can be reassigned; conflicting bindings swap. Analog trigger roles remain fixed. Reset a binding page with **Delete / Square**. The menu displays reassigned controller action names.

Settings have four pages: **Feel & Audio**, **Keyboard Actions**, **Controller Actions** and **Graphics & Camera**. Use the tabs or **G / Triangle** to change pages. Graphics presets are **Performance, Balanced and Ultra**, using 75%, 90% and 100% render scale with increasing shadow distance. Frame-rate targets are 30, 60 or 120 FPS. Anti-aliasing offers Off, FXAA and SMAA; shadows offer Off, Hard and Soft. Bloom ranges from 0 to 0.8 and base field of view from 55° to 90°, with aim/run camera adjustments. Graphics reset preserves keyboard/controller bindings. Master, music and effects volumes are separate. Original synthesized music, spatial effects, engines, footsteps, weapon impacts and chapter cues play locally.

The presentation adds physically based materials, textured suits, beveled ship armor, original HDR reflections, distinct alien ships, stitched terrain and graded colony foundations. Walk and sprint use distance-driven animation with slope-aware foot placement and pelvis adjustment. Hand-authored environments, characters and animations remain future production work; this is not a finished AAA production. Rumble is implemented through Unity's controller support; a physical DualSense was not connected for verification. USB/Bluetooth feel and device rumble remain unverified. Adaptive triggers and gyro controls are unsupported.

## Checkpoints and privacy

Progress auto-saves after chapter transitions and during exploration, and can be saved from pause or F5. Defeat lets you restore the last valid checkpoint. Three independent local slots use checksummed JSON, atomic replacement and validated backups. Settings are stored separately.

Checkpoints include civilization, chapters, contracts, cargo, modules, the world catalogue and floating origin, ship and crew positions, rover tires, surface mission progress, orbital discoveries, cruise routes, VTOL assistance and active survey rockets. Transient bullets and effects are rebuilt. Ordinary space patrols respawn; strategic war health and outcomes persist. This is checkpoint restoration rather than a replay of every transient scene object.

Files are stored in Unity's local persistent-data directory as `wayfarer-campaign-slot-N.json` and `wayfarer-settings-v1.json`. The previous `wayfarer-civilization-v1.json` is retained for import. No credentials, environment variables or crew photograph pixels enter these saves, the public source or the release archive. Crew portraits are session-local.

The settings payload is version 2 inside the existing filename and checksum envelope. Version 1 preferences migrate automatically while retaining bindings, sensitivity, audio and difficulty. Campaign format remains compatible with v0.3. Defeat unlocks the pointer; select **Restore checkpoint** or **Save slots / Home** with keyboard, mouse or controller.

## Reproduce the validation and showcase

Build the Mac player using the [repository instructions](../README.md#build-on-macos--apple-silicon), close any normal player, then run:

```sh
python3 tools/check_production.py
python3 tools/check_voyager.py
python3 tools/check_civilization.py
python3 tools/verify_game.py
python3 tools/benchmark_voyager.py
python3 tools/benchmark_voyager.py --ultra --width 1920 --height 1080
python3 tools/record_production.py
```

QA and recording use isolated files under the ignored `evidence/` directory. The campaign suite checks all chapter gates and monotonic difficulty, actual native landing and rover transitions, physical contract interactions, native escort motion and damage, modules, shields, remapping and full checkpoint recovery. Automated checks do not replace a complete manual twelve-chapter balance playthrough.

The [72-second walkthrough](media/twelve-horizons-tour.mp4) uses seeded resources, scripted inputs and staged cameras, labeled on screen. It shows the chapter menu, a city, native rocket ascent, outfitting, contracts, settings, fleet combat and Dyson collectors. Its original ambient score is assembled separately; it is not a recording of live game audio. Capture runs at 1280×800 and 12 FPS and is not a performance benchmark. The [capture manifest](validation/voyager-record-manifest.json) records its chapter timings.

The benchmark separately samples 360 frames in each of four staged scenes after a warmup. It records frame-time percentiles, GPU timing when available, and Unity allocated memory. Read [the measured report](validation/voyager-performance.json) with its hardware, resolution and frame-rate target. It is a short local sample, not a guarantee across machines or a long-term stability test.
