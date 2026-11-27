"""Record a complete scripted ground expedition in the packaged player.
Close the normal game before running. This mode stages a valid landing; it is not a manual test.
"""
from pathlib import Path
import os,subprocess,time,json
root=Path(__file__).resolve().parents[1];e=root/'evidence';folder=e/'surface-frames';folder.mkdir(exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
processes=subprocess.check_output(['ps','-A','-o','comm='],text=True)
assert str(exe) not in processes,'Close the normal Event Horizon player before recording.'
for path in folder.glob('*.png'):
 if path.stem.isdigit():path.unlink()
started=time.time()
with (e/'surface-record-stdout.log').open('w') as output:
 subprocess.run([str(exe),'--surface-record','-logFile',str(e/'surface-record-final.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),check=True,timeout=900,stdout=output,stderr=subprocess.STDOUT)
report=e/'surface-loop-checks.json';assert report.stat().st_mtime>=started-.5,'Stale recording report'
checks=json.loads(report.read_text());assert all(v is True for v in checks.values()),checks
frames=sorted(p for p in folder.glob('*.png') if p.stem.isdigit());assert len(frames)>500
assert [int(p.stem) for p in frames]==list(range(len(frames))),'Missing or stale frame indices'
subprocess.run(['/opt/homebrew/bin/ffmpeg','-hide_banner','-loglevel','warning','-y','-framerate','12','-i',str(folder/'%04d.png'),'-vf','scale=1280:800:flags=lanczos,format=yuv420p','-c:v','libx264','-preset','fast','-crf','21','-movflags','+faststart',str(e/'alien-expedition.mp4')],check=True)
print(f'PASS: {len(checks)} playthrough milestones, {len(frames)} frames; alien-expedition.mp4 saved.')
