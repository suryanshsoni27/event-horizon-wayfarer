"""Capture the opt-in New Dawn showcase; seeded resources and scripted staging are explicit."""
from pathlib import Path
import os,subprocess,shutil,time,json,re
root=Path(__file__).resolve().parents[1];e=root/'evidence';folder=e/'civilization-frames';folder.mkdir(parents=True,exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
assert str(exe) not in subprocess.check_output(['ps','-A','-o','comm='],text=True),'Close the standalone player first.'
for p in folder.glob('*.png'):
 if p.stem.isdigit():p.unlink()
started=time.time()
with (e/'civilization-record-stdout.log').open('w') as output:
 subprocess.run([str(exe),'--civilization-record','-logFile',str(e/'civilization-record.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=output,stderr=subprocess.STDOUT,check=True,timeout=480)
report=e/'civilization-record-checks.json';assert report.exists() and report.stat().st_mtime>=started-.5,'No fresh showcase report'
checks=json.loads(report.read_text());assert checks and all(x is True for x in checks.values()),checks
frames=sorted(p for p in folder.glob('*.png') if p.stem.isdigit());assert len(frames)>1000;assert [int(p.stem) for p in frames]==list(range(len(frames)))
log=(e/'civilization-record.log').read_text();match=re.search(r'CIVILIZATION_RECORD_FINAL frames=(\d+)',log);assert match and len(frames)==int(match[1])
chapters=[{'name':name,'source_start_seconds':int(frame)/12} for name,frame in re.findall(r'CIVILIZATION_FILM (\S+) frame=(\d+)',log)]
(e/'civilization-record-phases.json').write_text(json.dumps(chapters,indent=2)+'\n')
metadata=[';FFMETADATA1','title=Wayfarer - New Dawn civilization showcase','comment=Recorded Unity gameplay; seeded resources, staged cameras and scripted input.']
for i,chapter in enumerate(chapters):
 end=chapters[i+1]['source_start_seconds'] if i+1<len(chapters) else len(frames)/12
 metadata.extend(['[CHAPTER]','TIMEBASE=1/1000',f"START={round(chapter['source_start_seconds']*1000)}",f'END={round(end*1000)}',f"title={chapter['name'].replace('-',' ').title()}"])
meta=e/'civilization-chapters.ffmeta';meta.write_text('\n'.join(metadata)+'\n')
ffmpeg=shutil.which('ffmpeg');assert ffmpeg
subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-framerate','12','-i',str(folder/'%04d.png'),'-i',str(meta),'-map','0:v:0','-map_metadata','1','-map_chapters','1','-c:v','libx264','-preset','fast','-crf','21','-pix_fmt','yuv420p','-movflags','+faststart',str(e/'new-dawn-civilization-tour.mp4')],check=True)
for p in frames:p.unlink()
print(f'{len(checks)} showcase checks PASS; {len(frames)} frames encoded and cleared.')
