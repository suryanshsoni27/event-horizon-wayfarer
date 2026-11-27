"""Film the opt-in atlas/orbital/mining/workshop inserts; normal play is untouched."""
from pathlib import Path
import json, os, shutil, subprocess, time
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
evidence = root / 'evidence'
folder = evidence / 'feature-insert-frames'
folder.mkdir(parents=True, exist_ok=True)
exe = root / 'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
assert str(exe) not in subprocess.check_output(['ps', '-A', '-o', 'comm='], text=True), 'Close the standalone player first.'
for path in folder.glob('*.png'):
    if path.stem.isdigit():
        path.unlink()
portrait = Image.new('RGB', (512, 512), '#13283e')
draw = ImageDraw.Draw(portrait)
draw.ellipse((62, 30, 450, 418), fill='#eef5ff')
draw.rounded_rectangle((110, 90, 402, 365), radius=80, fill='#33c9cf')
for x in [190, 322]:
    draw.ellipse((x-24, 165, x+24, 213), fill='#13283e')
    draw.ellipse((x-8, 173, x+5, 186), fill='white')
draw.arc((189, 214, 323, 313), 5, 175, fill='#13283e', width=12)
draw.rounded_rectangle((100, 381, 412, 590), radius=65, fill='#eef5ff')
draw.ellipse((225, 421, 287, 483), fill='#ffb75e')
portrait.save(evidence / 'tour-synthetic-portrait.png')
started = time.time()
with (evidence / 'feature-inserts-stdout.log').open('w') as log:
    subprocess.run([str(exe), '--cosmos-record', '--feature-inserts', '-logFile', str(evidence / 'feature-inserts.log')],
                   env=dict(os.environ, BH_EVIDENCE=str(evidence)), stdout=log, stderr=subprocess.STDOUT, check=True, timeout=240)
report = evidence / 'feature-insert-checks.json'
assert report.stat().st_mtime >= started - .5, 'Stale feature report'
checks = json.loads(report.read_text())
frames = sorted(folder.glob('*.png'))
assert [int(p.stem) for p in frames] == list(range(540)), 'Missing filming frames'
ffmpeg = shutil.which('ffmpeg')
assert ffmpeg, 'Install FFmpeg to encode the recording.'
subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-y', '-framerate', '12', '-i', str(folder / '%04d.png'),
                '-c:v', 'libx264', '-preset', 'fast', '-crf', '21', '-pix_fmt', 'yuv420p', '-movflags', '+faststart',
                str(evidence / 'feature-inserts.mp4')], check=True)
assert checks and all(v is True for v in checks.values()), checks
for path in frames:
    path.unlink()
print(f'{len(checks)} feature inserts verified; 45 seconds encoded.')
