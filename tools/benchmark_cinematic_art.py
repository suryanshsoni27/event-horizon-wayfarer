"""Measure cinematic art scenes in the player; screenshots are disabled during measurement."""
from pathlib import Path
import argparse,json,os,subprocess,time

parser=argparse.ArgumentParser();parser.add_argument('--ultra',action='store_true');parser.add_argument('--width',type=int,default=1280);parser.add_argument('--height',type=int,default=800);args=parser.parse_args()

root=Path(__file__).resolve().parents[1];e=root/"evidence";e.mkdir(exist_ok=True)
exe=root/"Build/Event Horizon.app/Contents/MacOS/Event Horizon"
assert str(exe) not in subprocess.check_output(["ps","-A","-o","comm="],text=True),"Close the normal player first."
started=time.time();suffix='ultra' if args.ultra else 'balanced'
command=[str(exe),'--voyager-benchmark','--cinematic-art-benchmark','-screen-width',str(args.width),'-screen-height',str(args.height),'-screen-fullscreen','0','-logFile',str(e/f'cinematic-art-benchmark-{suffix}.log')]
if args.ultra:command.append('--production-ultra')
with (e/f"cinematic-art-benchmark-{suffix}-stdout.log").open("w") as log:
    subprocess.run(command,
                   env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=log,stderr=subprocess.STDOUT,
                   check=True,timeout=240)
report=e/"cinematic-art-performance.json"
assert report.exists() and report.stat().st_mtime>=started-.5,"No fresh benchmark report."
data=json.loads(report.read_text());assert len(data["phases"])==6
assert data['width']==args.width and data['height']==args.height,'Requested resolution was not used.'
assert data['quality']==('Ultra' if args.ultra else 'Balanced')
(e/f'cinematic-art-performance-{suffix}.json').write_text(json.dumps(data,indent=2)+'\n')
for phase in data["phases"]:
    assert phase["frames"]==360 and phase["averageMs"]>0
    print(f'{phase["name"]}: average {phase["averageMs"]:.1f} ms, p95 {phase["p95Ms"]:.1f} ms, p99 {phase["p99Ms"]:.1f} ms')
print(f'{data["processor"]}, {data["width"]}x{data["height"]}, {data["quality"]}, {data["renderScale"]:.0%} scale, target {data["targetFps"]} fps. Staged sample, not a long-term stability claim.')
