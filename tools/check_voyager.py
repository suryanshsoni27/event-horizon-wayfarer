"""Verify the released player, including full checkpoints and the twelve chapter campaign."""
from pathlib import Path
import json,os,subprocess,time,re

root=Path(__file__).resolve().parents[1]
e=root/"evidence";e.mkdir(exist_ok=True)
exe=root/"Build/Event Horizon.app/Contents/MacOS/Event Horizon"
assert str(exe) not in subprocess.check_output(["ps","-A","-o","comm="],text=True),"Close the normal player first."
started=time.time()
with (e/"voyager-qa-stdout.log").open("w") as log:
    subprocess.run([str(exe),"--voyager-qa","-logFile",str(e/"voyager-qa.log")],
                   env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=log,stderr=subprocess.STDOUT,
                   check=True,timeout=240)
report=e/"voyager-runtime-checks.json"
assert report.exists() and report.stat().st_mtime>=started-.5,"No fresh report; inspect voyager-qa.log."
checks=json.loads(report.read_text())
expected=set(re.findall(r'checks\["([^"]+)"\]', (root/"UnityProject/Assets/Scripts/VoyagerChecks.cs").read_text()))
assert set(checks)==expected,"Player/source QA mismatch; rebuild before testing."
failed=[name for name,result in checks.items() if result is not True]
assert checks and not failed,failed
print(f"{len(checks)}/{len(checks)} packaged Voyager checks PASS")
