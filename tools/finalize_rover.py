"""Bind the delivered controller/rover source, app, checks and reviewed media."""
from pathlib import Path
import hashlib,json,datetime,subprocess,re,zipfile,shutil
root=Path(__file__).resolve().parents[1];e=root/'evidence'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
checks={};evidence={}
for name in ['game','planet','expanse','surface','cosmos','controller']:
 p=e/(name+'-runtime-checks.json');d=json.loads(p.read_text());assert d and all(v is True for v in d.values()),(name,d)
 checks[name]={'passed':len(d),'total':len(d)};evidence[p.name]=digest(p)
 assert 'Exception:' not in (e/(name+'-qa.log')).read_text(),name
record=json.loads((e/'rover-record-checks.json').read_text());assert record and all(v is True for v in record.values())
for name in ['rover-record.log','controller-normal.log']:
 for line in (e/name).read_text().splitlines():
  assert not any(x in line for x in ['Exception:','Shader error','DllNotFoundException','MissingReferenceException']),(name,line)
log=(e/'unity-build-rover-delivery.log').read_text();assert 'ORBIT_BUILD_SUCCESS' in log
probe=json.loads(subprocess.check_output(['/opt/homebrew/bin/ffprobe','-v','error','-show_entries','format=duration,size:stream=width,height,nb_frames','-of','json',str(e/'dualsense-rover-tour.mp4')]))
assert abs(float(probe['format']['duration'])-400/12)<.1
assert probe['streams'][0]['width']==1280 and probe['streams'][0]['height']==800
assert int(probe['streams'][0]['nb_frames'])==400
stills={'rover-landed-options.png':8,'rover-tires-deploying.png':32,'rover-four-crew-seated.png':64,'rover-driving.png':120,'rover-crew-running.png':238,'rover-reboarded.png':320,'rover-flight-ready.png':380}
for name,frame in stills.items():shutil.copyfile(e/'rover-frames'/f'{frame:04}.png',e/name)
for p in [e/'dualsense-rover-tour.mp4',e/'dualsense-pause-guide.png',e/'rover-record-checks.json',e/'rover-native-checks.json',e/'controller-packaged-verification.log',e/'unity-build-rover-delivery.log']+[e/n for n in stills]:evidence[p.name]=digest(p)
source={}
for folder in ['UnityProject/Assets','UnityProject/Packages','UnityProject/ProjectSettings','native','tools']:
 for p in sorted((root/folder).rglob('*')):
  if p.is_file() and not any(x in p.parts for x in ['build','__pycache__']):source[str(p.relative_to(root))]=digest(p)
source['README.md']=digest(root/'README.md')
build={str(p.relative_to(root)):digest(p) for p in sorted((root/'Build/Event Horizon.app').rglob('*')) if p.is_file()}
(e/'controller-source-sha256.json').write_text(json.dumps(source,indent=2)+'\n');(e/'controller-build-sha256.json').write_text(json.dumps(build,indent=2)+'\n')
rollback=root/'archive/cosmos-before-controller-rover.zip'
with zipfile.ZipFile(rollback) as z:assert z.testzip() is None
report={'created_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'app':'Build/Event Horizon.app','source_manifest':'controller-source-sha256.json','build_manifest':'controller-build-sha256.json','packaged_checks':checks,'total_packaged_checks':sum(x['total'] for x in checks.values()),'record_checks':record,'native_rover_checks':json.loads((e/'rover-native-checks.json').read_text()),'input_system':'Unity Input System 1.20.1; DualSense HID layout; both input backends enabled','controller_testing':{'physical_dualsense_connected':False,'injected_dualsense_hid_reports':True,'virtual_gamepad_gameplay_and_menu_flow':True,'disconnect_and_reconnect_pause':True,'hardware_haptics_gyro_adaptive_triggers':False},'features':['Six animated deployable tires, wheel tread/rotation, front/rear steering, suspension and four riding crew','Native rover contact mode, traction/brake/reverse, slope limiting and conservative boulder clearance','Automatic static parking brake while astronauts explore','Disembark, walk/run/combat/missions, reboard, retract tires and return to flight','Controller atlas/workshop/journal/pause/death flow, hotplug/neutral gating/deadzones, device-aware HUD prompts'],'visual_review':{'method':'Game-captured frames reviewed from disk; Mac locked during native UI inspection','reviewed_frames':[32,64,120,238,320,380],'pause_guide_reviewed':True},'video':{'file':'dualsense-rover-tour.mp4','probe':probe,'scope':'Staged valid landing and native driving with injected gamepad states; initial patrol cleared and health protected; not a continuous landing trip, physical-controller playtest, combat test or FPS benchmark'},'runtime_exceptions':[],'limitations':['Physical USB/Bluetooth controller feel and connection remain unverified; no DualSense connected','Optional native macOS photo file chooser still uses keyboard/mouse','Vehicle is a simplified tangent-force/contact model, not a full multi-body tire simulation','Procedural graphics/animations remain prototype art'],'rollback':str(rollback.relative_to(root)),'rollback_sha256':digest(rollback),'evidence_sha256':evidence}
(e/'controller-delivery-checks.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'packaged_checks':report['total_packaged_checks'],'record_checks':len(record),'video_seconds':probe['format']['duration'],'runtime_exceptions':0},indent=2))
