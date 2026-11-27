"""Record developer-controlled demonstrations from the packaged large world."""
from pathlib import Path
import argparse
import json
import os
import re
import subprocess
import time

root = Path(__file__).resolve().parents[1]
evidence = root / 'evidence'
parser = argparse.ArgumentParser()
parser.add_argument('--mode', choices=['all', 'expedition', 'capture', 'combat'], default='all')
args = parser.parse_args()
jobs = [
    ('expedition', '--expanse-record', 'expanse-frames', 4, 420,
     ['expanse-record-milestones.json', 'expanse-record-metrics.json', 'expanse-record-phases.json']),
    ('capture', '--expanse-capture', 'expanse-hole-frames', 4, 180,
     ['expanse-capture-record-checks.json']),
    ('combat', '--expanse-combat', 'expanse-combat-frames', 3, 180,
     ['expanse-combat-stats.json']),
]
milestones = {
    'space_to_atmosphere_continuous', 'large_planet_native_landing',
    'surface_disembark', 'walk_and_reboard',
    'powered_escape_large_atmosphere', 'continuous_interplanetary_arrival',
}
for mode, flag, folder_name, digits, timeout, reports in jobs:
    if args.mode not in ('all', mode):
        continue
    processes = subprocess.check_output(['ps', '-A', '-o', 'pid=,comm='], text=True)
    assert 'black-hole-inspiration/Build/Event Horizon.app/Contents/MacOS/Event Horizon' not in processes, 'An Event Horizon player is already running'
    folder = evidence / folder_name
    folder.mkdir(exist_ok=True)
    for path in folder.iterdir():
        if re.fullmatch(r'\d{' + str(digits) + r'}\.png', path.name):
            path.unlink()
    for report in reports:
        (evidence / report).unlink(missing_ok=True)
    started = time.time()
    print(f'Recording {mode}...', flush=True)
    subprocess.run(
        [str(root / 'Build/Event Horizon.app/Contents/MacOS/Event Horizon'), flag,
         '-logFile', str(evidence / (flag[2:] + '.log'))],
        env=dict(os.environ, BH_EVIDENCE=str(evidence)), check=True,
        timeout=timeout, stdout=subprocess.DEVNULL,
    )
    for report in reports:
        path = evidence / report
        assert path.stat().st_mtime >= started - .5, f'Stale report: {report}'
    if mode == 'expedition':
        results = json.loads((evidence / reports[0]).read_text())
        assert milestones <= results.keys() and all(results[k] is True for k in milestones), results
    elif mode == 'capture':
        assert all(json.loads((evidence / reports[0]).read_text()).values())
    print(f'{mode}: {len(list(folder.glob("*.png")))} frames; reports fresh', flush=True)
