"""Run native Vision + actual ship input checks without opening or recording a webcam."""
from pathlib import Path
import hashlib,json,os,re,subprocess,time
from PIL import Image

root=Path(__file__).resolve().parents[1];e=root/'evidence';e.mkdir(exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
assert str(exe) not in subprocess.check_output(['ps','-A','-o','comm='],text=True),'Close the normal player first.'
fixture=root/'tools/fixtures/head-pilot-astronaut.png';source=json.loads((root/'tools/fixtures/head-pilot-astronaut-source.json').read_text())
assert hashlib.sha256(fixture.read_bytes()).hexdigest()==source['sha256']
with Image.open(fixture) as im:
    assert im.size==(512,512)
    (e/'head-pilot-face.bgra').write_bytes(im.convert('RGBA').tobytes('raw','BGRA'))
started=time.time()
with (e/'head-pilot-stdout.log').open('w') as log:
    subprocess.run([str(exe),'--voyager-qa','--head-pilot-qa','-logFile',str(e/'head-pilot-qa.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=log,stderr=subprocess.STDOUT,check=True,timeout=240)
report=e/'head-pilot-runtime-checks.json';assert report.stat().st_mtime>=started-.5
checks=json.loads(report.read_text());expected=set(re.findall(r'checks\["([^"]+)"\]',(root/'UnityProject/Assets/Scripts/HeadPilotChecks.cs').read_text()))
assert set(checks)==expected,'Player/source mismatch; rebuild.'
log=(e/'head-pilot-qa.log').read_text();assert not re.search(r'NullReferenceException|MissingReferenceException|DllNotFoundException|Shader error|EntryPointNotFoundException',log)
failed=[k for k,v in checks.items() if v is not True];assert not failed,failed
print(f'{len(checks)}/{len(checks)} head-pilot checks PASS. Native detector used an offline public image; no webcam opened.')
