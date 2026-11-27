"""Capture the nine-species art review. This is staged Unity footage, not an FPS benchmark."""
from pathlib import Path
import io,json,os,re,shutil,subprocess,time
from PIL import Image

root=Path(__file__).resolve().parents[1];e=root/'evidence';frames=e/'cinematic-art-frames';frames.mkdir(parents=True,exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
assert str(exe) not in subprocess.check_output(['ps','-A','-o','comm='],text=True),'Close the normal player first.'
for path in frames.glob('*.png'):
 if path.stem.isdigit():path.unlink()
ffmpeg=shutil.which('ffmpeg');assert ffmpeg,'FFmpeg is required.'
started=time.time();index=0;peak=0;dimensions=None
output=e/'wayfarer-cinematic-art.mp4'
with (e/'cinematic-art-record-stdout.log').open('w') as player_out,(e/'cinematic-art-encode.log').open('w') as encoder_out:
 encoder=subprocess.Popen([ffmpeg,'-hide_banner','-loglevel','error','-y','-f','image2pipe','-framerate','30','-vcodec','png','-i','pipe:0','-an','-c:v','libx264','-preset','fast','-threads','3','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(output)],stdin=subprocess.PIPE,stderr=encoder_out)
 player=subprocess.Popen([str(exe),'--voyager-record','--cinematic-art-record','-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',str(e/'cinematic-art-record.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=player_out,stderr=subprocess.STDOUT)
 try:
  last=time.time();next_budget_check=0
  while True:
   if time.time()>next_budget_check:
    backlog=sum(p.stat().st_size for p in frames.glob('*.png'));peak=max(peak,backlog);next_budget_check=time.time()+1
    if backlog>512*1024**2:raise RuntimeError('Encoder backlog exceeded the 512 MiB budget.')
   path=frames/f'{index:05d}.png'
   if path.exists():
    payload=path.read_bytes()
    if payload.endswith(bytes.fromhex('49454e44ae426082')):
     if dimensions is None:
      with Image.open(io.BytesIO(payload)) as im:dimensions=im.size
     encoder.stdin.write(payload);path.unlink();index+=1;last=time.time()
     if index%360==0:print(f'Encoded {index}/1440 cinematic-art frames.',flush=True)
     continue
   if player.poll() is not None and time.time()-last>2:break
   if encoder.poll() is not None:raise RuntimeError('Encoder stopped; inspect cinematic-art-encode.log.')
   if time.time()-started>600 or time.time()-last>90:raise TimeoutError('Capture stalled; inspect cinematic-art-record.log.')
   time.sleep(.02)
  assert player.returncode==0
  encoder.stdin.close();assert encoder.wait(timeout=60)==0
 except BaseException:
  if player.poll() is None:player.terminate();player.wait(timeout=10)
  if encoder.poll() is None:encoder.terminate();encoder.wait(timeout=10)
  raise
log=(e/'cinematic-art-record.log').read_text()
match=re.search(r'CINEMATIC_ART_FILM_COMPLETE frames=(\d+) width=(\d+) height=(\d+)',log)
assert match and index==int(match[1])==1440 and dimensions==(1920,1080)
assert not re.search(r'NullReferenceException|MissingReferenceException|Shader error|DllNotFoundException',log)
timing=[{'scene':name,'frames':int(count),'simulation_seconds':float(seconds)} for name,count,seconds in re.findall(r'PRODUCTION_FILM_TIMING (\S+) frames=(\d+) simulationSeconds=([\d.]+)',log)]
assert len(timing)==12 and all(abs(t['simulation_seconds']-(6 if t['scene'].startswith('landscape-') else 100/30))<.08 for t in timing)
probe=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_streams','-show_format','-of','json',str(output)],text=True));video=next(s for s in probe['streams'] if s['codec_type']=='video')
assert video['width']==1920 and video['height']==1080 and video['avg_frame_rate']=='30/1' and abs(float(probe['format']['duration'])-48)<.05
manifest={'version':'0.7.0','frames':index,'fps':30,'duration_seconds':48,'width':1920,'height':1080,'quality':'Ultra','anti_aliasing':'SMAA','contact_occlusion':True,'postprocessing':'ACES and color grading','simulation_seconds_per_captured_frame':1/30,'staged_encounters':True,'scripted_cameras':True,'idle_and_attack_pose_review':True,'manual_gameplay':False,'performance_benchmark':False,'audio':False,'camera_capture':False,'peak_pending_png_bytes':peak,'scenes':timing}
(e/'cinematic-art-record-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(f'48-second cinematic art review verified: {output.name} ({output.stat().st_size:,} bytes).')
