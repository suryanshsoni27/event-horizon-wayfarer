"""Record staged chapters showing gait, orbital objects, and both black-hole renderers."""
from pathlib import Path
import os,subprocess,time,json,re
root=Path(__file__).resolve().parents[1];e=root/'evidence';folder=e/'cosmos-frames';folder.mkdir(exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
assert str(exe) not in subprocess.check_output(['ps','-A','-o','comm='],text=True),'Close the normal player before recording.'
for p in folder.glob('*.png'):
 if p.stem.isdigit():p.unlink()
started=time.time()
with (e/'cosmos-record-stdout.log').open('w') as output:subprocess.run([str(exe),'--cosmos-record','-logFile',str(e/'cosmos-record.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),check=True,timeout=600,stdout=output,stderr=subprocess.STDOUT)
report=e/'cosmos-record-checks.json';assert report.stat().st_mtime>=started-.5,'Stale report'
checks=json.loads(report.read_text());assert checks and all(x is True for x in checks.values()),checks
frames=sorted(p for p in folder.glob('*.png') if p.stem.isdigit());assert len(frames)>500;assert [int(p.stem) for p in frames]==list(range(len(frames)))
match=re.search(r'COSMOS_RECORD_FINAL frames=(\d+)',(e/'cosmos-record.log').read_text());assert match and int(match[1])==len(frames)
subprocess.run(['/opt/homebrew/bin/ffmpeg','-hide_banner','-loglevel','warning','-y','-framerate','12','-i',str(folder/'%04d.png'),'-vf','scale=1280:800:flags=lanczos,format=yuv420p','-c:v','libx264','-preset','fast','-crf','21','-movflags','+faststart',str(e/'cosmos-feature-tour.mp4')],check=True)
print(f'{len(checks)} staged-tour checks PASS; {len(frames)} frames encoded.')
