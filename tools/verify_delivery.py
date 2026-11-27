"""Tie the current packaged build to its recorded checks and handoff evidence."""
from pathlib import Path
import argparse
import datetime
import hashlib
import json
import re
import subprocess
from zoneinfo import ZoneInfo

root = Path(__file__).resolve().parents[1]
e = root / 'evidence'
app = root / 'Build/Event Horizon.app'
parser = argparse.ArgumentParser()
parser.add_argument('--player-pid', type=int, required=True)
args = parser.parse_args()

def read(name):
    return json.loads((e / name).read_text())

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

build_log = (e / 'unity-build-expanse.log').read_text()
assert 'ORBIT_BUILD_SUCCESS' in build_log
build_time = (e / 'unity-build-expanse.log').stat().st_mtime
checks = {}
for name in ['game-runtime-checks.json', 'planet-runtime-checks.json', 'expanse-runtime-checks.json']:
    values = read(name)
    assert values and all(value is True for value in values.values()), name
    assert (e / name).stat().st_mtime >= build_time, f'{name} predates build'
    checks[name] = {'passed': len(values), 'total': len(values), 'checks': values}

for name in ['expanse-record-milestones.json', 'expanse-record-metrics.json',
             'expanse-record-phases.json', 'expanse-capture-record-checks.json',
             'expanse-combat-stats.json', 'expanse-media-manifest.json', 'crew-chooser-expanse-ui-checks.json']:
    assert (e / name).stat().st_mtime >= build_time, f'{name} predates build'

milestones = read('expanse-record-milestones.json')
assert len(milestones) == 6 and all(value is True for value in milestones.values())
capture = read('expanse-capture-record-checks.json')
assert len(capture) == 3 and all(value is True for value in capture.values())
chooser_ui = read('crew-chooser-expanse-ui-checks.json')
assert len(chooser_ui['observedChecks']) == 11 and all(value is True for value in chooser_ui['observedChecks'].values())
media = read('expanse-media-manifest.json')
for name, video in media['videos'].items():
    assert abs(float(video['duration']) - video['frames'] / 12) < .05
    assert (e / name).stat().st_size == int(video['size'])
    video['sha256'] = sha(e / name)

runtime_errors = {}
pattern = r'(?:\w*Exception:|Shader error|error CS\d+)'
for name in ['game-qa.log', 'planet-qa.log', 'expanse-qa.log', 'expanse-record.log',
             'expanse-capture.log', 'expanse-combat.log', 'crew-chooser-expanse-ui.log', 'normal-expanse.log']:
    found = re.findall(pattern, (e / name).read_text(errors='replace'))
    assert not found, (name, found)
    runtime_errors[name] = len(found)

subprocess.run(['codesign', '--verify', '--deep', '--strict', str(app)], check=True, capture_output=True)
native_source = root / 'UnityProject/Assets/Plugins/macOS/libblackhole.dylib'
native_bundle = next(app.rglob('libblackhole.dylib'))
assert sha(native_source) == sha(native_bundle)
assert (app / 'Contents/Resources/Data/StreamingAssets/THIRD_PARTY_ASSETS.txt').read_bytes() == (root / 'THIRD_PARTY_ASSETS.txt').read_bytes()
for asset in read('surface-asset-manifest.json'):
    assert sha(root / 'UnityProject/Assets/Resources/Surfaces' / asset['file']) == asset['sha256']

source_files = [root / 'README.md', root / 'THIRD_PARTY_ASSETS.txt']
for folder in ['UnityProject/Assets/Scripts', 'UnityProject/Assets/Editor',
               'UnityProject/Assets/Resources', 'UnityProject/ProjectSettings', 'tools']:
    source_files += [p for p in (root / folder).rglob('*') if p.is_file() and p.suffix not in ('.meta', '.pyc')]
source_files += [p for p in (root / 'native').iterdir() if p.is_file()]
source_manifest = {str(p.relative_to(root)): sha(p) for p in sorted(set(source_files))}
(e / 'expanse-source-sha256.json').write_text(json.dumps(source_manifest, indent=2) + '\n')
bundle_manifest = {str(p.relative_to(app)): sha(p) for p in sorted(app.rglob('*')) if p.is_file()}
(e / 'expanse-build-sha256.json').write_text(json.dumps(bundle_manifest, indent=2) + '\n')

processes = subprocess.check_output(['ps', '-A', '-o', 'pid=,comm='], text=True).splitlines()
players = [int(row.split(None, 1)[0]) for row in processes if 'black-hole-inspiration/Build/Event Horizon.app/Contents/MacOS/Event Horizon' in row]
assert players == [args.player_pid], players
mesh_samples = re.findall(r'MESH_CONTACT .*?error=([\d.Ee+-]+) samples=(\d+) nodes=(\d+) level=(\d+)', (e / 'expanse-qa.log').read_text())
assert len(mesh_samples) == 6
report = {
    'createdAt': datetime.datetime.now(ZoneInfo('America/New_York')).isoformat(),
    'status': 'Larger playable milestone; requested GTA 6 production fidelity remains unachieved',
    'app': str(app),
    'build': {'bytes': sum(p.stat().st_size for p in app.rglob('*') if p.is_file()),
              'executableSHA256': sha(app / 'Contents/MacOS/Event Horizon'),
              'nativeBundleMatchesSourcePlugin': True, 'codeSignatureVerified': True,
              'sourceManifest': 'expanse-source-sha256.json', 'bundleManifest': 'expanse-build-sha256.json'},
    'packagedChecks': checks,
    'nativeLargeWorld': read('expanse-native-checks.json'),
    'scaleAndTravel': read('expanse-scale-metrics.json'),
    'meshContact': {'cardinalLocations': len(mesh_samples),
                    'maximumSampledTriangleErrorMetres': max(float(x[0]) for x in mesh_samples),
                    'triangleSamples': sum(int(x[1]) for x in mesh_samples),
                    'maximumTestedNodes': max(int(x[2]) for x in mesh_samples)},
    'expeditionMilestones': milestones,
    'expeditionMetrics': read('expanse-record-metrics.json'),
    'capture': capture,
    'combat': read('expanse-combat-stats.json'),
    'media': media,
    'runtimeExceptionMatches': runtime_errors,
    'desktop': read('expanse-desktop-status.json'),
    'nativeChooserUI': chooser_ui,
    'manualLimitations': ['Full manual mouse flight is unverified; prior CUA injection did not update Unity hardware-pointer state'],
    'recordingScope': 'Packaged scripted controllers/native dynamics; exploration and capture disable combat damage, combat keeps damage enabled. Globe/mountain/night/atlas screenshots are staged camera fixtures.',
    'performanceScope': 'This Mac with capture overhead; Unity allocated bytes are not process RSS; no cross-machine performance guarantee',
    'assetScope': 'Original procedural geometry/shaders/world code with four credited Poly Haven CC0 surface texture files; simple procedural characters and visor photo textures',
    'normalLaunch': read('normal-expanse-launch.json'),
    'originalProjectsTargetedForChanges': False,
    'originalLearningEditor41239StillRunning': any(row.split(None, 1)[0] == '41239' for row in processes),
    'remainingProductionWork': ['Production assets and vegetation', 'Settlements, authored activities and richer interactions',
                                'Character rigs, animation and sound', 'Volumetric weather', 'Advanced route avoidance',
                                'Long manual playtests, accessibility and input tuning', 'Performance/content QA across machines'],
}
(e / 'expanse-delivery-checks.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps({'packagedChecksPassed': sum(x['passed'] for x in checks.values()),
                  'meshMaximumErrorMetres': report['meshContact']['maximumSampledTriangleErrorMetres'],
                  'normalPlayerPID': args.player_pid, 'report': str(e / 'expanse-delivery-checks.json')}, indent=2))
