# Controller repairs / v0.9.2

[Download for Apple Silicon Mac](https://github.com/suryanshsoni27/event-horizon-wayfarer/releases/tag/v0.9.2) · [Default controls](../README.md#controls)

The controller regression suite previously bypassed the campaign's menus. A separate campaign suite reproduced fourteen failing cases in the pre-fix player. Those cases are retained alongside new coverage for input transitions and raw DualSense reports.

## Repairs

| Problem | Updated behavior |
|---|---|
| Moving the left stick had no effect in menus | Left stick and D-pad navigate campaign, settings, civilization, atlas and workshop menus. Holding a direction repeats after a delay; small stick movements do not move the selection. |
| One held stick or trigger blocked every button after reconnect/resume | Each held control waits for its own release. Other buttons work immediately. A held Cross still cannot resume automatically on reconnection. |
| VTOL landing overrode manual flight commands | Deliberate stick input, R2, L2, boost, shoulder roll or Circle leveling takes over landing assist. D-pad up re-enables it. Cruise takeover first slows the craft when needed. |
| Atlas buttons also operated the world underneath | The atlas consumes navigation and confirmation. Its shoulders cannot open the journal, and D-pad down cannot open civilization command. |
| Rebinding could trap controller users | Options or Circle cancels keyboard capture. Hold the physical Options button to cancel digital controller capture; a short Options press can still be assigned. Binding runs in the input update, independent of GUI repaint timing. |
| Remapped D-pad hints and mapping labels were misleading | D-pad action hints resolve to their assigned buttons. Binding rows and the live test display retain physical button names. |
| Disconnects inside a menu skipped the safety handler | Disconnects clear movement/thrust and show the campaign pause menu. Focus loss does the same during exploration. |
| Holding fire while boarding could become ship thrust | Controls held across boarding wait for release before their new vehicle action becomes active. |
| Some actions required a mouse | Touchpad click opens the atlas. Face-flight setup is selectable in the pause menu. Camera use remains optional and requires its existing macOS permission. |

## Live controller test

1. Connect your DualSense to the Mac using USB or Bluetooth.
2. Open **Settings & Accessibility** from Home or the Options pause menu.
3. Press **Triangle** until **Controller Actions** appears.
4. Check the detected device/layout, both sticks, L2/R2 and the physical buttons shown beneath the mappings. Adjust the deadzone under **Feel & Audio** if a released stick drifts.
5. **Square** resets the current binding page to defaults. **Circle** returns.

The input display is local. No controller reports, video or photos are uploaded. If it says **No controller connected**, Unity is not receiving a gamepad from macOS; gameplay remapping cannot repair that connection.

## Verification and limits

Build the player, then run `python3 tools/check_controller.py`. This checks both the original controller/native rover suite and the campaign suite, verifies fresh reports against the source's expected checks and rejects runtime exceptions. Test campaigns use isolated evidence folders; the suite checks that personal save/settings files remain untouched.

Transport fixtures exercise Sony VID/PID matching, DualSense Edge layout selection, USB reports, full/minimal Bluetooth reports, sticks, analog triggers, buttons and touchpad click through Unity Input System's HID processing. Campaign tests cover menus, rebinding, VTOL takeover, focus loss and reconnect behavior. Original tests retain native walking, sprinting, weapons, tire deployment, rover driving, braking, seating and reboarding coverage.

Results are in [controller-fix-validation.json](validation/controller-fix-validation.json). These are automated software checks. No physical DualSense was connected during this run, so physical connection reliability, input latency and rumble still require hands-on acceptance. Adaptive triggers, gyro steering, PS-button actions and microphone-button gameplay actions are not implemented. This remains a Mac build, not a PS5 console package. Existing campaign checkpoints and settings formats are retained.
