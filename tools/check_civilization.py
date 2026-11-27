"""Exercise civilization progression and controller/world integration in the packaged player."""
from pathlib import Path
import json,os,subprocess,time
root=Path(__file__).resolve().parents[1]
e=root/'evidence';e.mkdir(exist_ok=True)
exe=root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'
assert str(exe) not in subprocess.check_output(['ps','-A','-o','comm='],text=True),'Close the normal player first.'
started=time.time()
with (e/'civilization-qa-stdout.log').open('w') as log:
 subprocess.run([str(exe),'--civilization-qa','-logFile',str(e/'civilization-qa.log')],env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=log,stderr=subprocess.STDOUT,check=True,timeout=180)
report=e/'civilization-runtime-checks.json'
assert report.exists() and report.stat().st_mtime>=started-.5,'No fresh civilization QA report; inspect civilization-qa.log.'
checks=json.loads(report.read_text());failed=[k for k,v in checks.items() if v is not True]
assert checks and not failed,failed
print(f'{len(checks)}/{len(checks)} packaged civilization checks PASS')
