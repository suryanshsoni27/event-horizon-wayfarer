# Living planets: graphics and fauna

[Temperate](media/ecology-temperate.png) · [Arid](media/ecology-arid.png) · [Frozen](media/ecology-frozen.png)

Version 0.6 gives the three planet climates their own fauna, ground cover, minerals and guardian nests. These are original procedural meshes and articulated animation, rendered in the actual Unity player.

| Habitat | Hunter | Ranged creature | Guardian |
| --- | --- | --- | --- |
| Temperate, including Pelagos | Moss Stalker: four legs, antennae, counterbalance tail; circles prey | Sporeback: six legs, dorsal fungal organs; retreats and launches spores | Thorn Matriarch: six legs, broad carapace, crown and exposed throat |
| Arid, including Cinder | Dune Scorpion: eight legs, closing pincers, articulated stinger; fast straight approach | Glassback: six legs, mineral armor and dorsal vents; faster, longer-range projectiles | Sunbreaker: heavy shell, large pincers and forward tusks; broad, slow attacks |
| Frozen, including Nivalis | Rime Maw: thick limbs, insulating collar and tusks; slower and tougher | Rimecaller: tall rime spines; slower, heavier ice projectiles | Glacier Titan: broad shoulders, branching horns and large feet; high health and long windup |

The destination key selects the same climate for terrain, flora, enemies, specimens and nests. Every temperate planet uses the temperate family, and likewise for arid and frozen planets. This is nine species across three climate families, rather than a unique species for every named destination. The field journal (**O / L1**) identifies the local fauna and gives attack advice. Enemy labels identify nearby species.

All attacks still use real encounter state: anticipation, a visible warning ring, range checks, recovery time, native movement and native ballistic projectiles. Move outside a melee ring before the strike. Ranged species retreat when approached. Desert scorpions accelerate on a straight approach. Heavy guardians become faster below 40% health. Exposed organs retain the carbine weak-point bonus; the suit pulse staggers nearby creatures. Chapter and difficulty scaling still apply.

## Graphics changes

- Organic meshes have smooth analytical normals, overlapping armor with deliberate clearance, recessed eyes, individual teeth and articulated jaws, tails and pincers. Existing terrain-aware leg IK plants their feet against the same contact surface used by the crew.
- Creature and foliage shaders now use URP physically based lighting, environment reflections and restrained emission. Depth and shadow passes match animated foliage and dissolving corpses. Damage produces a brief tint rather than a solid glowing silhouette.
- The build retains URP global resources, and the player activates them before the first scene using [Unity's runtime settings API](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/rendering/graphicssettings/trysetcurrentrenderpipelineglobalsettings).
- The renderer now references URP's post-processing data. ACES, color adjustments, bloom and vignette can reach the rendered image. Screen-space contact occlusion adds depth to intersections, armor and grounded objects. Balanced and Ultra enable it; Low disables it.
- Temperate ground cover uses rooted grass clumps and curved fern leaflets. Arid worlds use sparse waxy succulents and dry tufts. Frozen worlds use rigid rime shards and low lichen. Stable planet-relative cells preserve placement when the streaming window moves.
- Arid basins render as dry mineral soil rather than liquid seas. Frozen basins render as cracked glacial ice. Temperate coasts retain water, damp shore transitions and smaller water ripples. Triplanar rock materials remove stretched sphere UVs, desaturate frozen stone and accumulate snow on upward faces. Sand detail, snow roughness, rock normals, atmospheric scattering and cloud coverage vary with climate.
- Creature spawning resolves local boulder clearance. Rock streaming detects a replacement planet after checkpoint loading, rebuilding both its visible field and native contacts against the live planet.

## Checkpoints and validation

Existing enemy `kind` values remain hunter, ranged and guardian roles. Their species is derived from the saved destination key on attachment. No save format change is required. Existing foe health, mission flags, bounty IDs and rewards are preserved, rather than resetting a planet on upgrade. Newly spawned enemies use the new habitat tuning.

Build normally, close the player, then run:

```sh
python3 tools/check_ecology.py
python3 tools/check_head_pilot.py
python3 tools/check_production.py
python3 tools/check_voyager.py
python3 tools/check_civilization.py
python3 tools/verify_game.py
```

The ecology suite checks all nine actual spawned species, geometry and normals, grounded animation, native melee/projectile execution, dodging, climate-specific foliage, save/load, floating origins, live rock-field ownership, quality settings and rendered-pixel response to color grading. The production suite also loads a checkpoint created by the real v0.3 executable.

The [36-second art review](media/wayfarer-planet-ecology.mp4) shows each species on its native planet at 1920 × 1080 and 30 FPS. Encounters, idle/attack poses and cameras are staged; captions identify this. Simulation uses fixed capture time. The silent video is an art review, not a manual combat playthrough or performance benchmark. [Capture manifest](validation/ecology-record-manifest.json).

This remains a procedural indie prototype. Photorealistic hero characters, authored skeletal locomotion, full production animation and a manual campaign balance pass are still needed to reach a large commercial game's presentation. Physical DualSense and live face-flight hardware validation also remain separate work. Earlier v0.4 frame-time reports do not benchmark this ecology pass.
