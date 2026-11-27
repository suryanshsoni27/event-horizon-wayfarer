"""Validate composed learned fields against actual multi-source native xyz transitions."""
from pathlib import Path
import json,ctypes as C,numpy as np
r=Path(__file__).resolve().parents[1];m=json.loads((r/'UnityProject/Assets/Resources/predictor.json').read_text());centers=np.array(m['radialCenters']);sigmas=np.array(m['radialSigmas']);W=np.array([m['wx'],m['wy'],m['wz']]).T
class S(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','vx','vy','vz')]+[('captured',C.c_int)]
class H(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','mu','capture')]
lib=C.CDLL(str(r/'UnityProject/Assets/Plugins/macOS/libblackhole.dylib'));lib.bh_step_multi.argtypes=[C.POINTER(S),C.c_double,C.c_double,C.c_double,C.c_double];lib.bh_set_holes.argtypes=[C.POINTER(H),C.c_int];lib.bh_set_gravity_window.argtypes=[C.c_double,C.c_double]
holes=[H(x,y,z,20,1.8) for x,y,z in ((0,0,0),(45,8,10),(88,-6,-12),(130,5,16),(-65,35,20),(0,-70,-45),(60,70,0))];array=(H*len(holes))(*holes);lib.bh_set_holes(array,len(holes));lib.bh_set_gravity_window(60,100)
def field(p,u):
 radius=np.linalg.norm(p,axis=1);b=np.exp(-((radius[:,None]-centers)**2)/(2*sigmas**2));b[np.abs(radius[:,None]-centers)>5*sigmas]=0
 return np.column_stack([b*p[:,i,None] for i in range(3)]+[u,np.ones(len(p))])@W
rng=np.random.default_rng(995);p=rng.uniform([-85,-80,-80],[180,100,100],(6000,3));v=rng.uniform(-8,8,(6000,3));u=rng.uniform(-14,14,(6000,3));truth=[];pred=[]
for point,velocity,action in zip(p,v,u):
 if min(np.linalg.norm(point-np.array([h.x,h.y,h.z])) for h in holes)<2.4:continue
 s=S(*point,*velocity,0);lib.bh_step_multi(C.byref(s),*action,.01);truth.append((np.array([s.vx,s.vy,s.vz])-velocity)/.01)
 prediction=(field(np.zeros((1,3)),action[None,:])-field(np.zeros((1,3)),np.zeros((1,3))))[0]
 for h in holes:
  d=point-np.array([h.x,h.y,h.z]);t=np.clip((np.linalg.norm(d)-60)/40,0,1);fade=1-t*t*(3-2*t)
  prediction+=field(d[None,:],np.zeros((1,3)))[0]*(h.mu/60)*fade
 pred.append(prediction)
truth=np.array(truth);pred=np.array(pred);rmse=float(np.sqrt(np.mean((truth-pred)**2)));assert rmse<.003
# Independent states, 3-axis boost/upgraded-thrust response, capture and a smooth zero streaming shell.
a=S(12,10,8,1,2,3,0);b=S(12,10,8,1,2,3,0);lib.bh_step_multi(C.byref(a),0,0,0,.01);lib.bh_step_multi(C.byref(b),14,-14,14,.01);assert np.allclose(np.array([b.vx-a.vx,b.vy-a.vy,b.vz-a.vz]),[.126,-.126,.126])
solo=(H*1)(H(0,0,0,20,1.8));lib.bh_set_holes(solo,1)
def acceleration(distance):
 s=S(distance,0,0,0,0,0,0);lib.bh_step_multi(C.byref(s),0,0,0,.01);return s.vx/.01
assert acceleration(100)==0 and acceleration(100.01)==0 and abs(acceleration(99.99))<1e-8
for axis in range(3):
 p=np.zeros(3);p[axis]=1.81;vv=np.zeros(3);vv[axis]=-2;s=S(*p,*vv,0);lib.bh_step_multi(C.byref(s),0,0,0,.01);assert s.captured
report={'heldout_multi_source_transitions':len(truth),'sources':len(holes),'learned_composed_component_acceleration_rmse':rmse,'composition':'sum learned mu=60 field scaled by source mu/60; learned action term applied once','native_and_model_window':'1-smoothstep(60,100,r); zero beyond 100; sectors enter >112/leave >123','three_axis_upgraded_thrust':'PASS','independent_state_byref':'PASS','spherical_capture_xyz':'PASS','continuous_streaming_force_cutoff':'PASS','scope':'6000 seeded states across seven sources, actions up to ±14; no interacting multi-hole GR'}
(r/'evidence/multi-source-checks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))

# The same learned point field must also compose scaled planet gravity in SPACE.
class P(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','mu','radius','atmosphere','seed')]
lib.bh_set_planets.argtypes=[C.POINTER(P),C.c_int]
planets=[P(-14,-34,42,193.6,22,8,4),P(120,-30,40,160,20,7,8)]
lib.bh_set_planets((P*2)(*planets),2);lib.bh_set_holes(array,len(holes))
rng=np.random.default_rng(996);p=rng.uniform([-85,-80,-80],[180,100,100],(6000,3));v=rng.uniform(-8,8,(6000,3));u=rng.uniform(-14,14,(6000,3))
errors=[]
for point,velocity,action in zip(p,v,u):
 if min(np.linalg.norm(point-np.array([h.x,h.y,h.z])) for h in holes)<2.4:continue
 if any(np.linalg.norm(point-np.array([b.x,b.y,b.z]))<b.radius+b.atmosphere+3 for b in planets):continue
 state=S(*point,*velocity,0);lib.bh_step_multi(C.byref(state),*action,.01)
 prediction=(field(np.zeros((1,3)),action[None,:])-field(np.zeros((1,3)),np.zeros((1,3))))[0]
 for source in holes+planets:
  d=point-np.array([source.x,source.y,source.z]);t=np.clip((np.linalg.norm(d)-60)/40,0,1)
  prediction+=field(d[None,:],np.zeros((1,3)))[0]*(source.mu/60)*(1-t*t*(3-2*t))
 errors.append((np.array([state.vx,state.vy,state.vz])-velocity)/.01-prediction)
space_rmse=float(np.sqrt(np.mean(np.array(errors)**2)));assert space_rmse<.003
report.update(planet_space_transitions=len(errors),planet_sources=2,planet_and_hole_space_component_rmse=space_rmse,planet_scope='space only; excludes atmosphere and contact, which use native rollout fallback')
(r/'evidence/multi-source-checks.json').write_text(json.dumps(report,indent=2));print('Planet + hole space RMSE:',space_rmse,'over',len(errors),'transitions')
