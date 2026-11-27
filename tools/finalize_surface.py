"""Validate the saved packaged evidence and bind it to hashes of this delivery."""
from pathlib import Path
import hashlib,json,subprocess,datetime
root=Path(__file__).resolve().parents[1];e=root/'evidence'
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
source={}
for folder in ['UnityProject/Assets','UnityProject/Packages','UnityProject/ProjectSettings','native','tools']:
 for path in sorted((root/folder).rglob('*')):
  if not path.is_file() or any(x in path.parts for x in ['build','__pycache__']):continue
  source[str(path.relative_to(root))]=sha(path)
for path in [root/'README.md',root/'THIRD_PARTY_NOTICES.md']:
 if path.exists():source[str(path.relative_to(root))]=sha(path)
build={str(p.relative_to(root)):sha(p) for p in sorted((root/'Build/Event Horizon.app').rglob('*')) if p.is_file()}
(e/'surface-source-sha256.json').write_text(json.dumps(source,indent=2)+'\n');(e/'surface-build-sha256.json').write_text(json.dumps(build,indent=2)+'\n')
checks={}
for name in ['game-runtime-checks','planet-runtime-checks','expanse-runtime-checks','surface-runtime-checks','surface-loop-checks']:
 data=json.loads((e/(name+'.json')).read_text());assert data and all(v is True for v in data.values()),(name,data);checks[name]={'passed':len(data),'total':len(data),'all_pass':True}
probe=json.loads(subprocess.check_output(['/opt/homebrew/bin/ffprobe','-v','error','-show_entries','format=duration,size','-of','json',str(e/'alien-expedition.mp4')]))
log=(e/'surface-record-final.log').read_text();assert 'SURFACE_RECORD_FINAL' in log;stats=[s for s in log.splitlines() if s.startswith('SURFACE_RECORD_FINAL')][-1]
assert 'outcome=\n' in log or stats.endswith('outcome=')
frames=sorted(p for p in (e/'surface-frames').glob('*.png') if p.stem.isdigit())
assert frames and (e/'surface-loop-checks.json').stat().st_mtime>=frames[-1].stat().st_mtime-3,'Stale playthrough report'
assert abs(float(probe['format']['duration'])-len(frames)/12)<.1,'Video does not match the complete saved sequence'

errors=[]
for name in ['game-qa.log','planet-qa.log','expanse-qa.log','surface-qa.log','surface-record-final.log']:
 for line in (e/name).read_text().splitlines():
  if any(x in line for x in ['NullReferenceException','IndexOutOfRangeException','MissingReferenceException','Shader error','DllNotFoundException']):errors.append(name+': '+line)
assert not errors,errors
report={'built_at_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'artifact':'Build/Event Horizon.app','source_hash_manifest':'surface-source-sha256.json','build_hash_manifest':'surface-build-sha256.json','packaged_checks':checks,'regression_and_surface_checks_total':sum(v['total'] for k,v in checks.items() if k!='surface-loop-checks'),'playthrough':{'video':'alien-expedition.mp4','probe':probe,'summary':stats,'recorded_build_manifest':'surface-playthrough-build-sha256.json','provenance_note':'Recorded before final HUD text shortening; combat, mission and art logic unchanged by that copy edit.','controller':'scripted native walking, targeting, weapon fire and hold interactions; staged valid landing; damage enabled; no health or mission completion overrides','encoding':'sampled screenshots encoded at 12fps; not a frame-rate benchmark or human playtest'},'runtime_exceptions':errors,'new_content':{'creatures':['Rift Stalker','Sporecaster','Brood Warden'],'missions':['Recover flight recorder','Scan three alien specimens','Restart and defend relay','Defeat Warden and recover core'],'extraction':'Actual boarding grants one-time bonus','graphics':['Shared segmented creature meshes and gait animation','Wind-driven streamed ferns, fungi and grass','Wreck, relay, crystals and brood nest','Carbine, energy traces, attack warnings and shockwave','Detailed suit pack and coherent sky dust noise'],'combat_limits':{'living_creatures':10,'retained_creature_records':20,'acid_projectiles':24,'energy_effects':20,'pickups':12}},'limitations':['Procedural prototype art and light steering, without production rigs or a full navmesh','Contracts persist in memory for the current run; no disk save','Full manual hardware mouse playthrough remains unverified'],'rollback_source':'archive/expanse-before-surface-missions.zip'}
(e/'surface-delivery-checks.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'checks':report['regression_and_surface_checks_total'],'video_seconds':probe['format']['duration'],'runtime_exceptions':errors,'playthrough':stats},indent=2))
