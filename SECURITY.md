# Credentials and local data

The game, native simulation, model training and NPC inference run locally. No API keys or environment credentials are required.

- Keep real environment files, signing keys, Unity caches, local logs, private captures and backup archives out of Git. The repository ignore rules cover these locations.
- Optional crew photos are selected locally and used in memory for that session. They are not uploaded or included in this repository. Included screenshots show the default generated crew.
- Build logs and developer evidence stay in the ignored `evidence/` directory. Reviewed public screenshots and numerical results live under `docs/`.

Before publishing changes, stage the intended files, run `python3 tools/check_publication.py`, then scan the staged snapshot and Git history with Gitleaks using redacted output. Never include authentication tokens in repository URLs, scripts, command examples or reports.
