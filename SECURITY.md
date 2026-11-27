# Credentials and local data

The game, native simulation, model training and NPC inference run locally. No API keys or environment credentials are required.

- Keep real environment files, signing keys, Unity caches, local logs, private captures and backup archives out of Git. The repository ignore rules cover these locations.
- Optional crew photos are selected locally and used in memory for that session. They are not uploaded or included in this repository. Included screenshots show the default generated crew.
- Campaign slots and settings remain in Unity's local persistent-data directory. They store gameplay state, not environment variables, credentials or portrait pixels. Automated campaign checks and recording use isolated evidence profiles.
- Optional face-flight capture starts only from its in-game camera control. AVFoundation supplies video and Apple Vision analyzes pose/landmarks locally. The app does not record video, upload frames or identify people. Frames and neutral/expression calibration are session-only and cleared on camera stop; capture also stops on focus loss and quit. `wayfarer-head-pilot-v1.json` stores input preferences, not images or calibration. Automated QA/recording cannot start the camera.
- The offline face detector fixture under `tools/fixtures/` is an unmodified public-domain NASA photograph with a source/hash record. It is not a private user photo or a live-camera demonstration.
- Build logs and developer evidence stay in the ignored `evidence/` directory. Reviewed public screenshots and numerical results live under `docs/`.

Before publishing changes, stage the intended files, run `python3 tools/check_publication.py`, then scan the staged snapshot and Git history with Gitleaks using redacted output. Never include authentication tokens in repository URLs, scripts, command examples or reports.
