"""Edit actual recorded gameplay into a captioned 1080p feature tour.

Requires the recording tools' MP4s in evidence/. Pillow draws the edit graphics;
NumPy synthesizes an original quiet ambient score. No remote assets or services.
Use --record to recapture every source first (close the standalone player).
"""
from pathlib import Path
import argparse, datetime, hashlib, json, math, shutil, subprocess, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
E = ROOT / 'evidence'
WORK = E / 'full-feature-tour-edit'
OUT = ROOT / 'docs/media/wayfarer-full-feature-tour.mp4'
FF = shutil.which('ffmpeg')
PROBE = shutil.which('ffprobe')
W, H, FPS = 1920, 1080, 24
BG, INK, MUTED, CYAN = '#08111e', '#eef5ff', '#9bb0c9', '#55e8d0'

def run(args):
    subprocess.run([str(a) for a in args], check=True)

def probe(path):
    return json.loads(subprocess.check_output([PROBE, '-v', 'error', '-show_format', '-show_streams', '-of', 'json', str(path)]))

def cleanup_frames(folder):
    for p in (E / folder).glob('*.png'):
        if p.stem.isdigit():
            p.unlink()

def record():
    jobs = [('record_surface.py', [], 'surface-frames'), ('record_rover.py', [], 'rover-frames'),
            ('record_feature_inserts.py', [], 'feature-insert-frames'), ('record_cosmos.py', [], 'cosmos-frames')]
    for script, args, folder in jobs:
        run(['python3', ROOT / 'tools' / script, *args])
        cleanup_frames(folder)
    for mode, folder, digits, name in [('combat', 'expanse-combat-frames', 3, 'expanse-combat'),
                                       ('expedition', 'expanse-frames', 4, 'expanse-expedition'),
                                       ('capture', 'expanse-hole-frames', 4, 'expanse-black-hole')]:
        run(['python3', ROOT / 'tools/record_expanse.py', '--mode', mode])
        run([FF, '-hide_banner', '-loglevel', 'error', '-y', '-framerate', '12', '-i', E / folder / ('%0' + str(digits) + 'd.png'),
             '-c:v', 'libx264', '-preset', 'fast', '-crf', '21', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', E / (name + '.mp4')])
        assert float(probe(E / (name + '.mp4'))['format']['duration']) > 10
        cleanup_frames(folder)

def font(size, bold=False, mono=False):
    candidates = [('/System/Library/Fonts/Menlo.ttc', 0)] if mono else [('/System/Library/Fonts/Avenir Next.ttc', 0 if bold else 5)]
    candidates += [('/usr/share/fonts/truetype/dejavu/DejaVuSans' + ('-Bold' if bold else '') + '.ttf', 0)]
    for path, index in candidates:
        if Path(path).exists():
            return ImageFont.truetype(path, size, index=index)
    return ImageFont.load_default(size=size)

def wrap(draw, text, f, width):
    lines, line = [], ''
    for word in text.split():
        candidate = (line + ' ' + word).strip()
        if draw.textlength(candidate, font=f) > width and line:
            lines.append(line); line = word
        else:
            line = candidate
    if line:
        lines.append(line)
    return lines

def text_block(draw, text, xy, size, width, color=INK, bold=False, spacing=1.3):
    f = font(size, bold); x, y = xy
    for line in wrap(draw, text, f, width):
        draw.text((x, y), line, font=f, fill=color)
        y += size * spacing
    return y

def overlay(segment, index, total):
    im = Image.new('RGBA', (W, H), BG); d = ImageDraw.Draw(im)
    d.rectangle((24, 96, 1464, 996), fill=(0, 0, 0, 0))
    d.line((24, 79, 1895, 79), fill='#22364d', width=2)
    d.text((29, 19), 'EVENT HORIZON / WAYFARER', font=font(34, True), fill=INK)
    d.text((1470, 29), 'THE COMPLETE FEATURE TOUR', font=font(18, True), fill=CYAN)
    d.text((1500, 118), segment['chapter'], font=font(22, True), fill=CYAN)
    d.text((1496, 165), segment['number'], font=font(86, True), fill='#233d55')
    y = text_block(d, segment['title'], (1500, 285), 43, 380, bold=True)
    d.line((1500, int(y + 25), 1880, int(y + 25)), fill=CYAN, width=3)
    y += 60
    for item in segment['details']:
        d.ellipse((1501, int(y + 12), 1507, int(y + 18)), fill=CYAN)
        y = text_block(d, item, (1523, y), 24, 355, MUTED) + 25
    if segment.get('control'):
        d.rounded_rectangle((1495, 847, 1894, 973), radius=12, fill='#10283a')
        text_block(d, segment['control'], (1517, 866), 21, 355, CYAN)
    d.text((28, 1016), segment.get('note', 'SCRIPTED GAMEPLAY  /  1x SPEED'), font=font(18, True), fill=MUTED)
    d.text((1080, 1016), 'CAPTURE: 12 FPS  /  EDIT: 1080p', font=font(17), fill=MUTED)
    d.text((1700, 1015), f'{index + 1:02d} / {total:02d}', font=font(20, True), fill=INK)
    d.rectangle((24, 1060, 1896, 1064), fill='#22364d')
    d.rectangle((24, 1060, int(24 + 1872 * (index + 1) / total), 1064), fill=CYAN)
    path = WORK / f'overlay-{index:02d}.png'; im.save(path)
    return path

def card(kind):
    im = Image.new('RGB', (W, H), BG); d = ImageDraw.Draw(im)
    # Project-native orbital motifs; no stock or AI-generated gameplay.
    for r in range(170, 790, 110):
        d.ellipse((1470-r, 500-r, 1470+r, 500+r), outline='#17344b', width=2)
    d.ellipse((1390, 420, 1550, 580), fill='#02060c', outline=CYAN, width=4)
    if kind == 'intro':
        d.text((105, 170), 'A HOLIDAY PROJECT BY SURYANSH SONI', font=font(26, True), fill=CYAN)
        d.text((100, 265), 'EVENT HORIZON', font=font(92, True), fill=INK)
        d.text((105, 380), 'WAYFARER', font=font(68, True), fill=INK)
        text_block(d, 'From spaceflight to a whole alien expedition.', (110, 525), 40, 1060, MUTED)
        d.text((110, 670), 'UNITY  +  C++ PHYSICS  +  LEARNED NPC DYNAMICS', font=font(27, True), fill=CYAN)
        d.text((110, 900), 'Actual prototype footage / scripted demonstrations / chaptered edit', font=font(24), fill=MUTED)
    elif kind == 'physics':
        d.text((100, 100), 'HOW THE NPCs DECIDE', font=font(66, True), fill=INK)
        d.text((105, 200), 'Learned motion predictions + a programmed planner', font=font(32), fill=CYAN)
        labels = [('C++ SIMULATOR', '40,000 transitions\n8,000 held-out samples'),
                  ('DYNAMICS MODEL', 'Predict acceleration\nfor candidate actions'),
                  ('NPC PLANNER', '15 thrust candidates\n2.4-second horizon')]
        for i, (heading, body) in enumerate(labels):
            x = 105 + i * 580
            d.rounded_rectangle((x, 345, x + 525, 635), radius=18, fill='#10283a', outline='#284961', width=2)
            d.text((x + 28, 380), heading, font=font(27, True), fill=CYAN)
            for j, line in enumerate(body.split('\n')):
                d.text((x + 28, 450 + j * 48), line, font=font(28), fill=INK)
            if i < 2:
                d.text((x + 540, 450), '>', font=font(48, True), fill=CYAN)
        text_block(d, 'Native physics handles gravity, thrust, drag, contacts and projectiles. A separate danger model updates from encounters during play.', (110, 720), 32, 1670, MUTED)
        d.text((110, 940), 'Programmed goals and planning / surface aliens use steering and attack logic', font=font(26), fill=CYAN)
    else:
        d.text((105, 170), 'BUILT WHILE LEARNING UNITY', font=font(28, True), fill=CYAN)
        d.text((100, 275), 'NEVER STOP', font=font(90, True), fill=INK)
        d.text((100, 380), 'BEING A BEGINNER.', font=font(90, True), fill=INK)
        d.text((110, 590), 'Explore. Land. Drive. Discover. Survive.', font=font(39), fill=MUTED)
        d.text((110, 780), 'PUBLIC SOURCE CODE', font=font(23, True), fill=CYAN)
        d.text((110, 829), 'github.com/suryanshsoni27/event-horizon-wayfarer', font=font(28), fill=INK)
        d.text((110, 960), 'Gameplay prototype / original procedural score / scripted controller input', font=font(23), fill=MUTED)
    path = WORK / (kind + '.png'); im.save(path); return path

def segments():
    phases = json.loads((E / 'expanse-record-phases.json').read_text())
    p = {int(k): v / 12 for k, v in phases.items()}
    rows = []
    def add(source, start, end, chapter, number, title, details, control='', speed=1, note=None):
        rows.append(dict(source=source, start=start, end=end, speed=speed, chapter=chapter, number=number,
                         title=title, details=details, control=control, note=note or f'SCRIPTED GAMEPLAY  /  {speed:g}x SPEED'))
    add('feature-inserts.mp4',0,4,'EXPLORE','01','Chart your next destination',['32 systems. 128 catalogue destinations.','Select a planet or black-hole approach.'],'Create / Share: atlas\nD-pad: select\nCross: cruise')
    add('expanse-expedition.mp4',max(p[5],p[6]-36),min(float(probe(E/'expanse-expedition.mp4')['format']['duration']),p[6]+3),'EXPLORE','01','Cross the system',['Continuous interplanetary travel.','Floating origin keeps the local world stable.'],speed=3,note='SCRIPTED CONTINUOUS JOURNEY / 3x SPEED')
    add('expanse-expedition.mp4',p[1],p[2],'LAND','02','From orbit to the surface',['Atmosphere, clouds and procedural terrain.','Native gravity, drag and terrain contact.'],'D-pad up: VTOL descent',speed=4,note='SCRIPTED CONTINUOUS LANDING / 4x SPEED')
    add('dualsense-rover-tour.mp4',0,5,'DRIVE','03','Deploy six tires',['Landing gear unfolds into a crew rover.','Four astronauts ride aboard.'],'D-pad down: deploy tires',note='STAGED LANDING / INJECTED GAMEPAD / HEALTH PROTECTED')
    add('dualsense-rover-tour.mp4',5,14,'DRIVE','03','Drive across the planet',['Steering, braking, reverse and terrain alignment.','Tires rotate; the suspension follows the ground.'],'R2: drive / L2: reverse\nLeft stick: steer\nCross: handbrake',note='NATIVE ROVER DRIVING / INJECTED GAMEPAD')
    add('dualsense-rover-tour.mp4',17,22,'EXPLORE ON FOOT','04','Park. Disembark. Explore.',['The rover parks while the crew leaves.','Astronaut companions roam alongside you.'],'Triangle: exit / board',note='INJECTED GAMEPAD / INITIAL PATROL CLEARED')
    add('dualsense-rover-tour.mp4',22,28,'EXPLORE ON FOOT','04','Walk, run and regroup',['Articulated legs, knees and carried equipment.','A distinct run animation with faster movement.'],'Left stick: move\nL3: toggle run\nCross: hop',note='NATIVE CREW MOVEMENT / INJECTED GAMEPAD')
    add('alien-expedition.mp4',2,7,'PLANET MISSIONS','05','Recover the recorder',['Reach the site and hold interact.','Contracts begin when you land and disembark.'],'Hold R1: interact',note='FRESH SCRIPTED PLAYTHROUGH / DAMAGE ENABLED')
    add('alien-expedition.mp4',8,21,'PLANET MISSIONS','05','Scan three specimens',['Move between research sites.','Complete all scans to unlock relay defense.'],'L1: mission journal',speed=1.5,note='FRESH SCRIPTED PLAYTHROUGH / 1.5x SPEED')
    add('alien-expedition.mp4',31,47,'ALIEN ENCOUNTERS','06','Defend the relay',['Survive waves of hostile creatures.','Aim, reload and use the suit shockwave.'],'R2: fire / L2: aim\nSquare: reload\nCircle: suit pulse',speed=2,note='RELAY DEFENSE / DAMAGE ENABLED / 2x SPEED')
    add('alien-expedition.mp4',52,62,'ALIEN ENCOUNTERS','06','Face the brood warden',['Attack telegraphs, weak points and creature animation.','Defeat the guardian and recover its core.'],'Keep moving. Watch the attack rings.',note='GUARDIAN ENCOUNTER / DAMAGE ENABLED')
    add('alien-expedition.mp4',78,1013/12,'EXTRACT','07','Bring the expedition home',['Return to the ship and board.','Completed extraction awards a one-time bonus.'],'Triangle: board',note='SAME PLAYTHROUGH / ALL MISSION CHECKS PASSED')
    add('dualsense-rover-tour.mp4',28,400/12,'RETURN TO FLIGHT','07','Reboard and retract',['Gather the crew and fold the tires away.','The rover becomes a spacecraft again.'],'Triangle: board\nD-pad down: retract',note='STAGED VEHICLE TRANSITION / INJECTED GAMEPAD')
    add('expanse-expedition.mp4',p[4]+3,min(p[5],p[4]+27),'RETURN TO FLIGHT','07','Power out of the atmosphere',['Thrust against the planet\'s gravity.','Clear the atmosphere before engaging cruise.'],'Cross: boost',speed=3,note='SCRIPTED POWERED ASCENT / 3x SPEED')
    add('feature-inserts.mp4',12,17,'ORBITAL ACTIVITIES','08','Scan a satellite',['Solar arrays and orbiting instruments.','Download telemetry to earn score and parts.'],'D-pad right: orbital service',note='STAGED ORBITAL CLOSE-UP / REAL SCAN REWARD')
    add('feature-inserts.mp4',6,12,'ORBITAL ACTIVITIES','08','Repair at the research station',['A rotating habitat and docking structures.','Approach slowly to restore hull health.'],'Service within 160 m\nSpeed below 15 m/s',note='STAGED STATION APPROACH / REAL HULL REPAIR')
    add('feature-inserts.mp4',17,21,'ORBITAL ACTIVITIES','08','Find a derelict freighter',['Cargo pods and damaged spacecraft.','Explore the objects scattered around the planet.'])
    add('feature-inserts.mp4',21,29,'MINE AND UPGRADE','09','Break apart an asteroid',['Tumbling, textured rubble surrounds the planet.','Projectile hits mine a rock and award parts.'],'Square: ship weapon',note='STAGED MINING / NATIVE PROJECTILE HITS / TRAFFIC CLEARED')
    add('feature-inserts.mp4',29,34,'MINE AND UPGRADE','09','Fit the parts you earn',['Upgrade engine, hull, weapons or targeting.','Fitted modules change the ship\'s stats.'],'R3: workshop\nD-pad: select / Cross: fit',note='ACTUAL WORKSHOP / EARNED PARTS CONSUMED')
    add('feature-inserts.mp4',34,39,'SPACE DETAILS','10','Follow a passing comet',['An icy body and a long ion-tail effect.','Orbital objects make the sky feel inhabited.'],note='STAGED CLOSE-UP / ILLUSTRATIVE COMET TAIL')
    add('cosmos-feature-tour.mp4',460/12,530/12,'BLACK HOLES','11','The black holes are back',['A distant accretion disk marks the approach.','Select UMBRA and cruise toward it.'],'D-pad left: black hole',note='STAGED RENDERING SHOWCASE / PHYSICS PAUSED')
    add('cosmos-feature-tour.mp4',530/12,630/12,'BLACK HOLES','11','See spacetime bend light',['Nearby numerical Schwarzschild lensing.','The light-ray shader is separate from gameplay gravity.'],note='NUMERICAL LENSING SHOWCASE / PHYSICS PAUSED')
    duration = float(probe(E / 'expanse-black-hole.mp4')['format']['duration'])
    add('expanse-black-hole.mp4',max(0,duration-7),duration,'BLACK HOLES','11','Cross the capture boundary',['Native capture triggers the loss sequence.','The ship stretches into an illustrative tidal effect.'],note='SCRIPTED CAPTURE / STRETCH IS AN ARTISTIC EFFECT')
    add('expanse-combat.mp4',1,15,'SPACE NPCs','12','Fight independent space agents',['Each NPC has its own physical state and goal.','Predictions guide repeated action planning.'],'Square: fire\nR2: thrust / Cross: boost',note='SCRIPTED COMBAT PILOT / DAMAGE ENABLED')
    add('feature-inserts.mp4',39,45,'CUSTOM CREW','13','Bring your crew aboard',['Use local images as helmet portraits.','Session-only roster, crop and selection tools.'],'H: local crew-photo manager',note='CURRENT CREW MANAGER / SYNTHETIC PORTRAIT / SESSION ONLY')
    return rows

def score(duration):
    sr = 48000; result = np.zeros(int((duration+.2)*sr), dtype=np.float64)
    chords = [(130.8128,164.8138,195.9977),(110,130.8128,164.8138),(87.3071,110,130.8128),(97.9989,123.4708,146.8324)]
    for bar, start in enumerate(np.arange(0,duration,6)):
        n=min(int(6.5*sr),len(result)-int(start*sr));t=np.arange(n)/sr
        env=(1-np.exp(-t*1.5))*np.exp(-t*.42)
        for f in chords[bar%4]:
            tone=np.sin(2*np.pi*f*t)+.25*np.sin(2*np.pi*f*2*t)
            result[int(start*sr):int(start*sr)+n]+=.023*tone*env
        for step in range(6):
            offset=int((start+step)*sr)
            if offset>=len(result):break
            k=min(int(1.8*sr),len(result)-offset);tt=np.arange(k)/sr
            f=chords[bar%4][step%3]*4
            result[offset:offset+k]+=.014*np.sin(2*np.pi*f*tt)*np.exp(-tt*3)*(1-np.exp(-tt*50))
    t=np.arange(len(result))/sr
    result*=np.minimum(1,t/3)*np.minimum(1,np.maximum(0,(duration-t)/4))
    stereo=np.column_stack((result,result*.96));pcm=(np.clip(stereo,-1,1)*32767).astype('<i2')
    path=WORK/'original-ambient-score.wav'
    with wave.open(str(path),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(sr);w.writeframes(pcm.tobytes())
    return path

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--record',action='store_true');args=parser.parse_args()
    assert FF and PROBE, 'FFmpeg and ffprobe are required.'
    WORK.mkdir(parents=True,exist_ok=True);OUT.parent.mkdir(parents=True,exist_ok=True)
    if args.record:record()
    for name in ['surface-loop-checks.json','rover-record-checks.json','cosmos-record-checks.json','feature-insert-checks.json','expanse-record-milestones.json','expanse-capture-record-checks.json']:
        checks=json.loads((E/name).read_text());assert checks and all(v is True for v in checks.values()),name
    rows=segments();parts=[];timeline=[];cursor=0
    base=[FF,'-hide_banner','-loglevel','error','-y']
    enc=['-an','-c:v','libx264','-preset','veryfast','-crf','23','-pix_fmt','yuv420p','-r',str(FPS),'-threads','2','-video_track_timescale','24000']
    def add_card(kind,seconds):
        nonlocal cursor
        p=WORK/f'{len(parts):02d}-{kind}.mp4'
        run(base+['-loop','1','-framerate',str(FPS),'-i',card(kind),'-t',str(seconds),*enc,p]);parts.append(p)
        timeline.append(dict(at=round(cursor,3),duration=seconds,card=kind));cursor+=seconds
    add_card('intro',6)
    for i,s in enumerate(rows):
        if s['chapter']=='CUSTOM CREW':add_card('physics',11)
        hold = 2 if s['chapter'] == 'EXTRACT' else 0
        duration=round(((s['end']-s['start'])/s['speed'] + hold)*FPS)/FPS
        assert s['end'] <= float(probe(E/s['source'])['format']['duration'])+.05,s
        p=WORK/f'{len(parts):02d}-gameplay.mp4';graphic=overlay(s,i,len(rows))
        vf=f'[0:v]setpts=(PTS-STARTPTS)/{s["speed"]},fps={FPS},tpad=stop_mode=clone:stop_duration={hold},scale=1440:900:flags=lanczos,pad=1920:1080:24:96:color={BG}[game];[game][1:v]overlay=0:0:shortest=1,fade=t=in:st=0:d=0.18,fade=t=out:st={max(0,duration-.18)}:d=0.18,scale=1920:1080,setsar=1,format=yuv420p[v]'
        run(base+['-ss',str(s['start']),'-t',str(s['end']-s['start']),'-i',E/s['source'],'-loop','1','-i',graphic,
                  '-filter_complex',vf,'-map','[v]','-t',str(duration),*enc,p]);parts.append(p)
        timeline.append(dict(at=round(cursor,3),duration=duration,hold_final_frame=hold,**s));cursor+=duration
        print(f'{i+1}/{len(rows)}: {s["title"]}',flush=True)
    add_card('outro',6)
    concat=WORK/'parts.txt';concat.write_text(''.join("file '"+p.name+"'\n" for p in parts))
    silent=WORK/'picture.mp4';run(base+['-f','concat','-safe','0','-i',concat,'-c','copy',silent])
    duration=float(probe(silent)['format']['duration']);music=score(duration)
    chapter_file=WORK/'chapters.ffmeta';chapter_lines=[';FFMETADATA1'];last=None
    for entry in timeline:
        label=entry.get('chapter',entry.get('card','').upper())
        if label!=last:
            chapter_lines += ['[CHAPTER]','TIMEBASE=1/1000','START='+str(round(entry['at']*1000)),'END='+str(round(duration*1000)),'title='+label]
            last=label
    # Bound each chapter at the next chapter start.
    starts=[i for i,line in enumerate(chapter_lines) if line.startswith('START=')]
    for a,b in zip(starts,starts[1:]):chapter_lines[a+1]='END='+chapter_lines[b].split('=',1)[1]
    chapter_file.write_text('\n'.join(chapter_lines)+'\n')
    run(base+['-i',silent,'-i',music,'-f','ffmetadata','-i',chapter_file,'-map','0:v:0','-map','1:a:0','-c:v','copy','-c:a','aac','-b:a','160k',
              '-af','loudnorm=I=-22:TP=-3:LRA=7','-shortest','-map_chapters','2','-metadata','title=Event Horizon / Wayfarer - Full Feature Tour',
              '-metadata','comment=Actual prototype gameplay; scripted demonstrations. Original procedural ambient score. Capture 12 fps; edit 24 fps.',
              '-movflags','+faststart',OUT])
    sources=sorted({s['source'] for s in rows})
    manifest=dict(created_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),resolution=[W,H],edit_fps=FPS,capture_fps=12,
                  duration=duration,gameplay_provenance='Recorded Unity prototype. Scripted input and staged views; no physical DualSense connected. Cuts join separate sessions.',
                  music='Original locally synthesized ambient score; no third-party music.',
                  capture_base_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
                  capture_code_sha256={name:hashlib.sha256((ROOT/'UnityProject/Assets/Scripts'/name).read_bytes()).hexdigest() for name in ['FeatureInserts.cs','CosmosGame.cs','HelmetPhotoUI.cs']},
                  sources=[dict(file='evidence/'+n,sha256=hashlib.sha256((E/n).read_bytes()).hexdigest()) for n in sources],timeline=timeline,
                  output_sha256=hashlib.sha256(OUT.read_bytes()).hexdigest())
    (ROOT/'docs/validation/feature-tour-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    for name in ['surface-loop-checks','rover-record-checks','cosmos-record-checks','feature-insert-checks','expanse-record-milestones','expanse-capture-record-checks']:
        shutil.copy2(E/(name+'.json'),ROOT/'docs/validation'/(name+'.json'))
    print(f'Saved {OUT.name}: {duration:.2f}s, {OUT.stat().st_size/1024**2:.1f} MiB',flush=True)

if __name__=='__main__':main()
