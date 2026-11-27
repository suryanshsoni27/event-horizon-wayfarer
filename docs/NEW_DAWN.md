# New Dawn: civilization expansion

New Dawn adds a persistent civilization to Event Horizon / Wayfarer. The ship, terrain, space objects, aliens, rover and surface expeditions remain playable. Civilization progress survives restarting a flight or closing the game.

## Download and play

Download `Event-Horizon-New-Dawn-macOS-AppleSilicon.zip` from the [New Dawn release](https://github.com/suryanshsoni27/event-horizon-wayfarer/releases/tag/v0.2.0), unzip it, and open `Event Horizon.app`. The executable is built for Apple Silicon Macs. Connect a DualSense over USB or Bluetooth, or use keyboard and mouse.

This independent prototype is not signed with an Apple Developer distribution certificate. If macOS blocks it, review the downloaded source and release, then use **System Settings → Privacy & Security → Open Anyway** for this app. The release includes a SHA-256 checksum.

## Start a civilization

1. Open the Mac player. The new command screen introduces your campaign. Press Cross/Enter to begin a VTOL descent, or Circle to resume manual exploration.
2. Land, settle, and leave the craft with Triangle/J. Open command with **B** or **D-pad down** while on foot.
3. Choose **City Planner → Pioneer command spire → Place**. Aim at clear, flat ground. **E/R1** confirms; **Q/R or L1** rotates; **Circle/Esc** cancels.
4. Add a foundry, solar farm and observatory. Their construction and production advance while the world is running. Menus and pause freeze the economy.
5. Accept recommendations in **Oracle**. Completing an accepted milestone awards resources once. Already achieved milestones can be claimed retroactively.
6. Build a launch complex, assemble a rocket, then launch. A survey succeeds when its native trajectory reaches 1 km above terrain.
7. Build an orbital shipyard. Research new player hulls, assemble a fleet, explore another world, or negotiate with its civilization.
8. Establish a second colony to reach the interplanetary age. Build twelve stellar collector sectors to complete the Dyson project and reach Type II.

The civilization supply network covers the 32 core systems. Procedural frontier exploration remains available. Rival capitals require a treaty (relations of 70 or higher) or an orbital victory before you can establish a colony there.

## Economy and construction

You begin with 650 alloy, 340 energy and 80 science. Resources come from production, expedition/combat rewards and unique Oracle milestones. Foundries retain limited production during a power shortage, and solar construction requires no stored energy, allowing recovery from a depleted grid.

| District | Purpose |
|---|---|
| Pioneer command spire | Establish a colony; initial housing and command infrastructure |
| Habitat district | Add housing and a residential skyline |
| Regolith foundry | Produce refined alloy |
| Helios solar farm | Generate energy for the shared civilization grid |
| Xenoscience observatory | Produce science while powered |
| Orbital launch complex | Assemble and launch survey rockets |
| Orbital shipyard | Fabricate warships; unlock military hull designs |
| Aegis defense battery | Strengthen friendly fleets defending a raid |

Each world supports one command spire and up to 24 districts. Foundations must be separate, within 1.6 km of the local command spire, and on sufficiently flat ground. Roads fit the underlying terrain. Building cores block crew and ground vehicles; streets remain traversable. Autonomous civilian shuttles fly between the districts using the native solver.

## Shipwright

Four original hull silhouettes can be researched and equipped while the craft is stopped below 3 m/s with the tires retracted. Four coatings change the actual ship and the rotating 3D preview. Existing salvage-fitted engine, hull, weapon and targeting modules remain available through U/R3.

| Hull | Gameplay changes |
|---|---|
| Kestrel Pathfinder | Exploration baseline |
| Atlas hauler | +45 hull; 0.92× thrust |
| Lancer corvette | +85 hull; 1.12× thrust; faster cannon and one additional projectile |
| Sovereign cruiser | +150 hull; 1.20× thrust; heavy cannon and two additional projectiles |

Equipping another design preserves the current hull-health fraction rather than providing a free repair. Blueprints and coatings persist with your civilization.

## Oracle and civilization ages

Oracle is a local utility-based strategy adviser. It ranks milestones using your facilities, resources, progress, fleet readiness and incoming raids, and explains the reason for each recommendation. It requires no API key or external model service. Its recommendations are programmed strategic reasoning, separate from the game's learned NPC motion model.

| Tier | Requirement |
|---|---|
| Pioneer | Starting expedition |
| Settlement | Complete a command spire |
| Planetary | Complete an observatory and a successful survey launch |
| Interplanetary | Own two worlds and complete a survey launch |
| Stellar | Complete four Dyson sectors |
| Type II | Complete all twelve sectors |

The ages and resource rates are gameplay progression, not a quantitative simulation of a real Kardashev civilization.

## Fleets, aliens and diplomacy

Helion Concord, Vanta Synod and the Mycelial Swarm have distinct identities, planetary capitals and diplomatic relations. Envoys improve relations. Hostile civilizations can raid developed colonies; defense batteries reinforce the defending fleet.

A strike requires at least three reserve ships and deploys up to six. Fleet travel times scale with the distance between worlds, allowing the player to reach a distant front before the fleet. They fight on arrival and return surviving ships to reserve. Victories can claim the contested world. Native ship states and ballistic plasma bolts drive nearby battles; distant fronts use a bounded strategic approximation over the same health/outcome state. Player ship weapons can damage hostile fleet ships and earn rewards.

Use **Set course to orbital front** to join an active battle, or visit a rival's capital. Cruise still requires boarding, retracting the rover and clearing the atmosphere. Routes include planet-clearance arcs.

## Dyson project

The stellar project consists of twelve sectors with 56 rendered collector panels per sector, illuminated orbital trusses and a rotating collector shell around the chosen core star. Each completed sector generates 22 energy per gameplay second for all colonies. Construction costs rise with each sector and consume alloy, energy and science. Each sector takes 24 unpaused seconds to assemble.

The structure is a game-scale collector-swarm visualization. Collector orbits, stellar heat, relativistic effects and material engineering are not physically simulated.

## New controls

| Action | Keyboard | DualSense |
|---|---|---|
| Open command while flying/driving | B | L3 |
| Open command on foot | B | D-pad down |
| Change command tabs | Left/right arrows | L1/R1 or D-pad left/right |
| Select a row | Up/down arrows | D-pad up/down |
| Main action | Enter | Cross |
| Return to the world | B/Esc | Circle |
| Cycle ship coating | C | Square |
| Place a selected foundation | Enter in City Planner | Cross in City Planner |
| Confirm foundation / rotate | E / Q,R | R1 / L1 |
| Assemble rocket / build warship / send envoy | G in the relevant tab | Triangle |
| Launch assembled rocket | L in launch-complex row | Square |
| Visit civilization / join front | On-screen course button | Square in Fleet Command |
| Set course to Dyson project | G or on-screen course button | Triangle in Dyson Project |
| Leave rocket-follow camera | Backspace | Circle |

The right stick rotates the live design preview. Controller validation uses injected gamepad input; a physical DualSense has not been connected for verification. The build remains for macOS/Apple Silicon, not a PS5 console package.

## Saves, tests and recording

Campaign data is written to Unity's local persistent-data directory as `wayfarer-civilization-v1.json`, with atomic replacement and a validated backup. The save stores economy, settlements, construction, hulls, missions, diplomacy, fleets, war outcomes and Dyson progress. It does not store local crew photographs, credentials or environment variables. Flight death resets the local expedition and fitted salvage modules; the civilization remains.

After building and closing the normal player:

```sh
python3 tools/check_civilization.py
python3 tools/verify_game.py
python3 tools/record_civilization.py
```

The showcase uses staged camera positions, seeded resources and scripted inputs. It does not overwrite the normal campaign. The verified build passes 257 packaged checks: 63 civilization checks and 194 existing gameplay/controller checks. QA exercises progression, actual native rocket ascent, fleet motion and weapons, player hits, controller navigation and placement, save recovery, pause behavior and floating-origin continuity.

New original geometry is batched by shared material, with metal coatings, lit window facades, emissive accents, terrain-fitted transit surfaces and four ship silhouettes. The renderer uses full render scale with the existing URP shadows and post-processing. This remains a procedural desktop prototype with simplified contact and strategic models; it is not a production-scale RTS or photorealistic asset library.
