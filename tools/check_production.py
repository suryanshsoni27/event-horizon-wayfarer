"""Validate graphics integration, controller recovery, and a genuine v0.3 checkpoint in the player."""
from pathlib import Path
import json,os,re,shutil,subprocess,time

root=Path(__file__).resolve().parents[1];e=root/'evidence';e.mkdir(exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
assert str(exe) not in subprocess.check_output(['ps','-A','-o','comm='],text=True),'Close the normal player first.'
shutil.copyfile(root/'tools/fixtures/v03-campaign.json',e/'v03-campaign-fixture.json')
started=time.time()
with (e/'production-stdout.log').open('w') as log:
 subprocess.run([str(exe),'--voyager-qa','--production-qa','-logFile',str(e/'production-qa.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=log,stderr=subprocess.STDOUT,check=True,timeout=300)
report=e/'production-runtime-checks.json';assert report.stat().st_mtime>=started-.5
checks=json.loads(report.read_text());source=(root/'UnityProject/Assets/Scripts/ProductionPass.cs').read_text()
expected=set(re.findall(r'checks\["([^"]+)"\]',source));assert set(checks)==expected,'Player/source mismatch; rebuild.'
log=(e/'production-qa.log').read_text();assert not re.search(r'NullReferenceException|MissingReferenceException|Shader error|DllNotFoundException',log)
failed=[name for name,result in checks.items() if result is not True]
for line in log.splitlines():
 if line.startswith(('PRODUCTION_METRICS','PRODUCTION_GAIT')):print(line)
assert not failed,failed
print(f'{len(checks)}/{len(checks)} packaged production checks PASS')
