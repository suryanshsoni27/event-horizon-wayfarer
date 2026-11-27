"""Run deterministic checks in the actual packaged Unity player and fail on any false result."""
from pathlib import Path
import subprocess,os,json,time
root=Path(__file__).resolve().parents[1];env=dict(os.environ,BH_EVIDENCE=str(root/'evidence'))
for flag,filename in [('--game-qa','game-runtime-checks.json'),('--planet-qa','planet-runtime-checks.json'),('--expanse-qa','expanse-runtime-checks.json'),('--surface-qa','surface-runtime-checks.json'),('--cosmos-qa','cosmos-runtime-checks.json'),('--controller-qa','controller-runtime-checks.json')]:
 started=time.time()
 subprocess.run([str(root/'Build/Event Horizon.app/Contents/MacOS/Event Horizon'),flag,'-logFile',str(root/('evidence/'+flag[2:]+'.log'))],env=env,check=True,timeout=180,stdout=subprocess.DEVNULL)
 assert (root/'evidence'/filename).stat().st_mtime>=started-.5, 'Stale packaged check output'
 results=json.loads((root/'evidence'/filename).read_text());failed=[k for k,v in results.items() if v is not True]
 assert not failed,'Failed packaged checks: '+str(failed)
 print(f'{len(results)}/{len(results)} {flag} checks PASS')
