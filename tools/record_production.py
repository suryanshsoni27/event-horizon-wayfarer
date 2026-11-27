"""Record the art pass at 30 fps, continuously encoding/deleting PNGs to bound disk use."""
from pathlib import Path
import argparse,io,json,os,re,shutil,subprocess,time,wave
import numpy as np
from PIL import Image

parser=argparse.ArgumentParser();parser.add_argument('--width',type=int,default=1920);parser.add_argument('--height',type=int,default=1080);args=parser.parse_args()
root=Path(__file__).resolve().parents[1];e=root/'evidence';frames=e/'production-frames';frames.mkdir(parents=True,exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon';assert str(exe) not in subprocess.check_output(['ps','-A','-o','comm='],text=True),'Close the normal player first.'
for p in frames.glob('*.png'):
 if p.stem.isdigit():p.unlink()
ffmpeg=shutil.which('ffmpeg');assert ffmpeg,'FFmpeg is required.'
started=time.time();index=0;dimensions=None;pending_peak=0
with (e/'production-capture-stdout.log').open('w') as player_out,(e/'production-encode.log').open('w') as encoder_out:
 encoder=subprocess.Popen([ffmpeg,'-hide_banner','-loglevel','error','-y','-f','image2pipe','-framerate','30','-vcodec','png','-i','pipe:0','-an','-c:v','libx264','-preset','fast','-threads','3','-crf','19','-pix_fmt','yuv420p',str(e/'production-silent.mp4')],stdin=subprocess.PIPE,stderr=encoder_out)
 player=subprocess.Popen([str(exe),'--voyager-record','--production-record','-screen-width',str(args.width),'-screen-height',str(args.height),'-screen-fullscreen','0','-logFile',str(e/'production-record.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=player_out,stderr=subprocess.STDOUT)
 try:
  last_frame=time.time();next_backlog_check=0
  while True:
   if time.time()>=next_backlog_check:
    pending=sum(p.stat().st_size for p in frames.glob('*.png'));pending_peak=max(pending_peak,pending);next_backlog_check=time.time()+1
    if pending>1024**3:raise RuntimeError('Encoder backlog exceeded the 1 GiB disk budget.')
   path=frames/f'{index:05d}.png'
   if path.exists():
    payload=path.read_bytes()
    if payload.endswith(bytes.fromhex('49454e44ae426082')):
     if dimensions is None:
      with Image.open(io.BytesIO(payload)) as im:dimensions=im.size
     encoder.stdin.write(payload);path.unlink();index+=1;last_frame=time.time()
     if index%300==0:print(f'Captured and encoded {index} frames.',flush=True)
     continue
   if player.poll() is not None:
    if time.time()-last_frame>2:break
   if encoder.poll() is not None:raise RuntimeError('Video encoder stopped; inspect production-encode.log.')
   if time.time()-started>600 or time.time()-last_frame>90:raise TimeoutError('Capture stalled; inspect production-record.log.')
   time.sleep(.02)
  assert player.returncode==0
  encoder.stdin.close();assert encoder.wait(timeout=120)==0
 except BaseException:
  if player.poll() is None:player.terminate();player.wait(timeout=10)
  if encoder.poll() is None:encoder.terminate();encoder.wait(timeout=10)
  raise
log=(e/'production-record.log').read_text();match=re.search(r'PRODUCTION_FILM_COMPLETE frames=(\d+) width=(\d+) height=(\d+)',log)
assert match and index==int(match[1])==1500,'Incomplete capture.'
assert dimensions==(int(match[2]),int(match[3]))
assert dimensions==(args.width,args.height),'Requested resolution was not used.'
assert not re.search(r'NullReferenceException|MissingReferenceException|Shader error|DllNotFoundException',log)
chapters=[{'name':name,'start_seconds':int(frame)/30} for name,frame in re.findall(r'PRODUCTION_FILM (\S+) frame=(\d+)',log)]
timings=[{'name':name,'frames':int(count),'simulation_seconds':float(seconds)} for name,count,seconds in re.findall(r'PRODUCTION_FILM_TIMING (\S+) frames=(\d+) simulationSeconds=([\d.]+)',log)]
assert len(timings)==6 and all(abs(t['simulation_seconds']-t['frames']/30)<.08 for t in timings),'Capture changed simulation speed.'
duration=index/30;rate=22050;t=np.arange(round(duration*rate))/rate
chords=np.array([[110,164.8138,220,261.6256],[87.3071,130.8128,174.6141,220],[130.8128,195.9977,261.6256,329.6276],[97.9989,146.8324,195.9977,246.9417]])
left=np.zeros_like(t);right=np.zeros_like(t)
for voice in range(4):
 frequency=chords[(t//4).astype(int)%4,voice];phase=np.cumsum(frequency)*2*np.pi/rate
 left+=np.sin(phase)*.055;right+=np.sin(phase+t*.13*2*np.pi)*.055
envelope=np.sin((t%4)/4*np.pi)*np.minimum(1,t/2)*np.minimum(1,(duration-t)/2)
score=e/'production-original-score.wav'
with wave.open(str(score),'wb') as wav:
 wav.setnchannels(2);wav.setsampwidth(2);wav.setframerate(rate);wav.writeframes((np.clip(np.stack([left*envelope,right*envelope],axis=1),-1,1)*32767).astype('<i2').tobytes())
meta=[';FFMETADATA1','title=Wayfarer - Graphics and production pass','comment=Staged Unity capture at 30 fps with fixed simulation time; seeded resources, scripted input and cameras. Separately assembled original score.']
for i,chapter in enumerate(chapters):
 end=chapters[i+1]['start_seconds'] if i+1<len(chapters) else duration
 meta+=['[CHAPTER]','TIMEBASE=1/1000',f'START={round(chapter["start_seconds"]*1000)}',f'END={round(end*1000)}',f'title={chapter["name"].replace("-"," ").title()}']
metadata=e/'production-chapters.ffmeta';metadata.write_text('\n'.join(meta)+'\n')
output=e/'wayfarer-production-tour.mp4'
subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-i',str(e/'production-silent.mp4'),'-i',str(score),'-i',str(metadata),'-map','0:v:0','-map','1:a:0','-map_metadata','2','-map_chapters','2','-c:v','copy','-c:a','aac','-b:a','128k','-af','loudnorm=I=-18:TP=-1.5:LRA=9','-shortest','-movflags','+faststart',str(output)],check=True)
manifest={'frames':index,'fps':30,'duration_seconds':duration,'width':dimensions[0],'height':dimensions[1],'quality':'Ultra','render_scale':1,'anti_aliasing':'SMAA','fixed_simulation_time_per_frame':True,'wall_clock_performance_benchmark':False,'seeded_resources':True,'scripted_input':True,'staged_cameras':True,'live_game_audio':False,'audio':'Separately synthesized original ambient score.','maximum_observed_pending_png_bytes':pending_peak,'chapters':chapters,'simulation_timing':timings}
(e/'production-record-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
(e/'production-silent.mp4').unlink()
print(f'Finished {duration:.0f}s at {dimensions[0]}x{dimensions[1]}, 30 fps; streamed frames removed.',flush=True)
