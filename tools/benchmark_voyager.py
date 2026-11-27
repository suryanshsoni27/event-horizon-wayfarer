"""Measure staged busy scenes in the player; screenshots are disabled during measurement."""
from pathlib import Path
import json,os,subprocess,time

root=Path(__file__).resolve().parents[1];e=root/"evidence";e.mkdir(exist_ok=True)
exe=root/"Build/Event Horizon.app/Contents/MacOS/Event Horizon"
assert str(exe) not in subprocess.check_output(["ps","-A","-o","comm="],text=True),"Close the normal player first."
started=time.time()
with (e/"voyager-benchmark-stdout.log").open("w") as log:
    subprocess.run([str(exe),"--voyager-benchmark","-logFile",str(e/"voyager-benchmark.log")],
                   env=dict(os.environ,BH_EVIDENCE=str(e)),stdout=log,stderr=subprocess.STDOUT,
                   check=True,timeout=240)
report=e/"voyager-performance.json"
assert report.exists() and report.stat().st_mtime>=started-.5,"No fresh benchmark report."
data=json.loads(report.read_text());assert len(data["phases"])==4
for phase in data["phases"]:
    assert phase["frames"]==360 and phase["averageMs"]>0
    print(f'{phase["name"]}: average {phase["averageMs"]:.1f} ms, p95 {phase["p95Ms"]:.1f} ms, p99 {phase["p99Ms"]:.1f} ms')
print(f'{data["processor"]}, {data["width"]}x{data["height"]}, target {data["targetFps"]} fps. Staged sample, not a long-term stability claim.')
