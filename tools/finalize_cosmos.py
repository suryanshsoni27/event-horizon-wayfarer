"""Bind current Cosmos build, runtime checks and visual review artifacts to SHA-256 manifests."""
from pathlib import Path
import hashlib,json,datetime,subprocess,re
root=Path(__file__).resolve().parents[1];e=root/'evidence'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
source={}
for folder in ['UnityProject/Assets','UnityProject/Packages','UnityProject/ProjectSettings','native','tools']:
 for p in sorted((root/folder).rglob('*')):
  if p.is_file() and not any(x in p.parts for x in ['build','__pycache__']):source[str(p.relative_to(root))]=digest(p)
source['README.md']=digest(root/'README.md')
build={str(p.relative_to(root)):digest(p) for p in sorted((root/'Build/Event Horizon.app').rglob('*')) if p.is_file()}
(e/'cosmos-source-sha256.json').write_text(json.dumps(source,indent=2)+'\n');(e/'cosmos-build-sha256.json').write_text(json.dumps(build,indent=2)+'\n')
checks={}
for name in ['game-runtime-checks','planet-runtime-checks','expanse-runtime-checks','surface-runtime-checks','cosmos-runtime-checks','cosmos-record-checks']:
 d=json.loads((e/(name+'.json')).read_text());assert d and all(v is True for v in d.values()),(name,d);checks[name]={'passed':len(d),'total':len(d),'all_pass':True}
probe=json.loads(subprocess.check_output(['/opt/homebrew/bin/ffprobe','-v','error','-show_entries','format=duration,size','-of','json',str(e/'cosmos-feature-tour.mp4')]))
log=(e/'cosmos-record.log').read_text();m=re.search(r'COSMOS_RECORD_FINAL frames=(\d+)',log);assert m;count=int(m[1]);assert abs(float(probe['format']['duration'])-count/12)<.1
errors=[]
for name in ['game-qa.log','planet-qa.log','expanse-qa.log','surface-qa.log','cosmos-qa.log','cosmos-record.log']:
 for line in (e/name).read_text().splitlines():
  if any(x in line for x in ['NullReferenceException','IndexOutOfRangeException','MissingReferenceException','Shader error','DllNotFoundException','ArgumentException']):errors.append(name+': '+line)
assert not errors,errors
report={'created_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'artifact':'Build/Event Horizon.app','source_manifest':'cosmos-source-sha256.json','build_manifest':'cosmos-build-sha256.json','runtime_checks':checks,'packaged_checks_total':sum(v['total'] for k,v in checks.items() if k!='cosmos-record-checks'),'runtime_exceptions':errors,'black_hole_finding':'Native sources remained present. UMBRA is about 73km from the starting ship; the flat distant disk was almost edge-on, tiny and lacked a named beacon.','black_hole_fix':'Angular distant proxy with shadow/lensed arcs; named range beacon; 9/F2 selects actual destination and T cruises; numerical Schwarzschild renderer retained inside 45Rs.','animation':'Original jointed two-bone IK rigs, grounded feet, distance-driven walk/run gait, weapon carry/recoil/reload, creature jaw/attack/death blending; entire crew rig uses the crew camera layer.','running':'Either Shift held or Caps Lock toggle. Native traction increased; game speed depends on terrain.','new_space_content':['Bounded 160-rock stream from deterministic 640-candidate belt, textured tumbling rocks, swept ship collision, two-hit mining','Three orbiting satellites with solar arrays and telemetry scans','Rotating research station with repair service','Derelict cargo tug','Artistic comet/ion tail'],'resource_fix':'Planets release only their owned materials; shared creature/suit/site materials survive reset and stream removal.','feature_tour':{'file':'cosmos-feature-tour.mp4','frames':count,'probe':probe,'provenance':'Explicit staged ground, orbital and black-hole chapters. Ground uses native walk/run and ordinary weapons. The orbital showcase suppresses hostile missile damage. Black-hole chapters pause simulation to compare renderers. Sampled frames encoded at 12fps; not a frame-rate benchmark or a manual playtest.'},'limitations':['Procedural animation and art; no motion-capture animation library','Orbital objects use kinematic authored/Kepler-scaled paths rather than N-body dynamics','Asteroid and orbital contacts use conservative swept spheres; cruise obstacle steering is basic','Full manual hardware-pointer playthrough remains unverified'],'rollback_source':'archive/alien-expeditions-before-cosmos.zip'}
(e/'cosmos-delivery-checks.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'checks':report['packaged_checks_total'],'tour_frames':count,'video_seconds':probe['format']['duration'],'runtime_errors':errors},indent=2))
