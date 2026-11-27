"""Staged native rover tour driven by injected gamepad states; not a hardware controller test."""
from pathlib import Path
import os,subprocess,time,json,re
root=Path(__file__).resolve().parents[1];e=root/'evidence';folder=e/'rover-frames';folder.mkdir(exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
assert str(exe) not in subprocess.check_output(['ps','-A','-o','comm='],text=True),'Close the normal player before recording.'
for p in folder.glob('*.png'):
 if p.stem.isdigit():p.unlink()
started=time.time()
with (e/'rover-record-stdout.log').open('w') as output:
 subprocess.run([str(exe),'--rover-record','-logFile',str(e/'rover-record.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),check=True,timeout=600,stdout=output,stderr=subprocess.STDOUT)
report=e/'rover-record-checks.json';assert report.stat().st_mtime>=started-.5,'Stale report'
checks=json.loads(report.read_text())
frames=sorted(p for p in folder.glob('*.png') if p.stem.isdigit());assert len(frames)==400
assert [int(p.stem) for p in frames]==list(range(len(frames)))
subprocess.run(['/opt/homebrew/bin/ffmpeg','-hide_banner','-loglevel','warning','-y','-framerate','12','-i',str(folder/'%04d.png'),'-vf','scale=1280:800:flags=lanczos,format=yuv420p','-c:v','libx264','-preset','fast','-crf','21','-movflags','+faststart',str(e/'dualsense-rover-tour.mp4')],check=True)
print(f'{len(frames)} frames encoded; staged-tour checks: {checks}',flush=True)
assert checks and all(v is True for v in checks.values()),checks
