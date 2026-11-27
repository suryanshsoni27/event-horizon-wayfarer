"""Exercise destination fauna, real melee combat, foliage and checkpoint restore in the packaged player."""
from pathlib import Path
import json,os,re,subprocess,time
root=Path(__file__).resolve().parents[1];e=root/'evidence';e.mkdir(exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
assert str(exe) not in subprocess.check_output(['ps','-A','-o','comm='],text=True),'Close the normal player first.'
started=time.time()
with (e/'ecology-stdout.log').open('w') as log:
 player=subprocess.Popen([str(exe),'--voyager-qa','--ecology-qa','-screen-width','1600','-screen-height','1000','-screen-fullscreen','0','-logFile',str(e/'ecology-qa.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=log,stderr=subprocess.STDOUT)
 try:
  while player.poll() is None:
   runtime=e/'ecology-qa.log'
   if runtime.exists() and runtime.stat().st_mtime>=started-.5:
    assert not re.search(r'NullReferenceException|MissingReferenceException|Shader error|DllNotFoundException|IndexOutOfRangeException',runtime.read_text()),'Packaged player reported a runtime/render error; inspect ecology-qa.log.'
   assert time.time()-started<300,'Ecology checks timed out.'
   time.sleep(.2)
  assert player.returncode==0,'Ecology player exited unsuccessfully.'
 finally:
  if player.poll() is None:player.terminate();player.wait(timeout=10)
report=e/'ecology-runtime-checks.json';assert report.stat().st_mtime>=started-.5
checks=json.loads(report.read_text())
source=(root/'UnityProject/Assets/Scripts/EcologyPass.cs').read_text()
keys=set(re.findall(r'checks\["([^"\n]+)"\]',source))
for key in re.findall(r'checks\["([^"\n]+)"\+suffix\]',source):
 keys.update(key+suffix for suffix in ('temperate','arid','frozen'))
assert set(checks)==keys,'Player/source mismatch; rebuild.'
log=(e/'ecology-qa.log').read_text()
assert not re.search(r'NullReferenceException|MissingReferenceException|Shader error|DllNotFoundException|IndexOutOfRangeException',log)
failed=[name for name,result in checks.items() if result is not True]
assert not failed,failed
print(f'{len(checks)}/{len(checks)} packaged ecology checks PASS')
