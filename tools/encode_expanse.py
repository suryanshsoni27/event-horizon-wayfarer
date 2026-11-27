"""Encode current packaged recordings and build an honest visual evidence sheet."""
from pathlib import Path
import argparse,subprocess,json,shutil,datetime,re
from zoneinfo import ZoneInfo
from PIL import Image,ImageOps,ImageDraw,ImageFont
r=Path(__file__).resolve().parents[1];e=r/'evidence';args=argparse.ArgumentParser();args.add_argument('--crew-frame',type=int,default=-1);opt=args.parse_args()
expected={'space_to_atmosphere_continuous','large_planet_native_landing','surface_disembark','walk_and_reboard','powered_escape_large_atmosphere','continuous_interplanetary_arrival'}
milestones=json.loads((e/'expanse-record-milestones.json').read_text());assert expected<=milestones.keys() and all(milestones[k] is True for k in expected)
capture=json.loads((e/'expanse-capture-record-checks.json').read_text());assert all(capture.values())
media={}
for folder,pattern,out in [('expanse-frames','%04d.png','expanse-expedition.mp4'),('expanse-hole-frames','%04d.png','expanse-black-hole.mp4'),('expanse-combat-frames','%03d.png','expanse-combat.mp4')]:
 frames=sorted((e/folder).glob('[0-9]'*(4 if '04' in pattern else 3)+'.png'));assert frames and [int(x.stem) for x in frames]==list(range(len(frames))),folder
 subprocess.run(['/opt/homebrew/bin/ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','12','-i',str(e/folder/pattern),'-vf','scale=1280:800:flags=lanczos','-c:v','libx264','-threads','2','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(e/out)],check=True)
 meta=json.loads(subprocess.check_output(['/opt/homebrew/bin/ffprobe','-v','error','-show_entries','format=duration,size:stream=width,height,r_frame_rate,codec_name','-of','json',str(e/out)]));media[out]={'frames':len(frames),**meta['format'],'streams':meta['streams']}
crew_frame=opt.crew_frame if opt.crew_frame>=0 else int(json.loads((e/'expanse-record-phases.json').read_text())['2'])+14
shutil.copyfile(e/'expanse-frames'/f'{crew_frame:04}.png',e/'expanse-surface-crew.png')
canvas=Image.new('RGB',(1600,1100),(9,17,26));draw=ImageDraw.Draw(canvas);font=ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc',23)
for x,y,file,label in [(0,0,'wayfarer-surface-crew.png','EARLIER / WAYFARER'),(800,0,'expanse-surface-crew.png','EXPANSE / SURFACE EXPEDITION'),(0,550,'expanse-whole-planet.png','12 KM DIAMETER / COMPLETE PLANET'),(800,550,'expanse-mountain-valley.png','3D RELIEF / ADAPTIVE TERRAIN')]:
 im=ImageOps.contain(Image.open(e/file).convert('RGB'),(790,495));canvas.paste(im,(x+(800-im.width)//2,y+45));draw.text((x+20,y+13),label,font=font,fill=(210,230,243))
canvas.save(e/'expanse-before-after.png')
(e/'expanse-media-manifest.json').write_text(json.dumps({'createdAt':datetime.datetime.now(ZoneInfo('America/New_York')).isoformat(),'videos':media,'note':'Scripted packaged player demonstrations; exploration/capture disable combat damage, combat keeps it enabled; staged globe/mountain/night/atlas views are separate screenshots.'},indent=2)+'\n')
print(json.dumps(media,indent=2))
