"""Exercise controller input in both the original sandbox and the actual campaign."""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import time

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--output", type=Path, default=root / "evidence/controller")
args = parser.parse_args()
output = args.output.resolve()
output.mkdir(parents=True, exist_ok=True)
exe = root / "Build/Event Horizon.app/Contents/MacOS/Event Horizon"
assert str(exe) not in subprocess.check_output(["ps", "-A", "-o", "comm="], text=True), "Save and close the normal game first."

suites = [
    ("controller", ["--controller-qa"], "ControllerChecks.cs", "c", "controller-runtime-checks.json"),
    ("campaign", ["--voyager-qa", "--controller-campaign-qa"], "ControllerCampaignChecks.cs", "checks", "controller-campaign-runtime-checks.json"),
]
total = 0
for name, flags, source, variable, report in suites:
    folder = output / name
    folder.mkdir(exist_ok=True)
    started = time.time()
    logfile = folder / "player.log"
    with (folder / "stdout.log").open("w") as stream:
        subprocess.run([str(exe), *flags, "-logFile", str(logfile)],
                       env=dict(os.environ, BH_EVIDENCE=str(folder)),
                       stdout=stream, stderr=subprocess.STDOUT, check=True, timeout=180)
    path = folder / report
    assert path.is_file() and path.stat().st_mtime >= started - .5, f"Missing fresh {name} report"
    checks = json.loads(path.read_text())
    expected = set(re.findall(rf'{variable}\["([^"]+)"\]', (root / "UnityProject/Assets/Scripts" / source).read_text()))
    assert set(checks) == expected, f"{name}: player/source check mismatch; rebuild first"
    failures = [key for key, passed in checks.items() if passed is not True]
    assert not failures, f"{name}: {failures}"
    assert not re.search(r"(?:NullReference|InvalidOperation|Argument|IndexOutOfRange)Exception|\b(?:Error|Exception):", logfile.read_text()), f"{name}: runtime exception; inspect log"
    total += len(checks)
    print(f"{len(checks)}/{len(checks)} packaged {name} controller checks PASS", flush=True)
print(f"{total}/{total} controller checks PASS. Synthetic devices; physical USB/Bluetooth acceptance still requires a connected controller.")
