"""Collect actual 3D native transitions, fit a radial-feature vector regressor, evaluate holdout."""
from pathlib import Path
import ctypes as C, json, numpy as np
ROOT=Path(__file__).resolve().parents[1]
class State(C.Structure):
 _fields_=[(k,C.c_double) for k in ('x','y','z','vx','vy','vz')]+[('captured',C.c_int)]
lib=C.CDLL(str(ROOT/'UnityProject/Assets/Plugins/macOS/libblackhole.dylib'))
lib.bh_step.argtypes=[C.POINTER(State),C.c_double,C.c_double,C.c_double,C.c_double]
lib.bh_energy.argtypes=[C.POINTER(State)];lib.bh_energy.restype=C.c_double
rng=np.random.default_rng(42)
def collect(n):
 direction=rng.normal(size=(n,3));direction/=np.linalg.norm(direction,axis=1,keepdims=True)
 r=np.where(rng.random((n,1))<.75,rng.uniform(2.4,20,(n,1)),rng.uniform(20,300,(n,1)));p=direction*r;a=rng.uniform(-1,1,(n,3));v=rng.uniform(-4,4,(n,3));target=[]
 for pos,u,vel in zip(p,a,v):
  s=State(*pos,*vel,0);lib.bh_step(C.byref(s),*u,.01)
  target.append((np.array([s.vx,s.vy,s.vz])-vel)/.01)
 return p,a,v,np.array(target)
# Radial distances are an inductive bias, not a force law. Independent xyz output weights are fitted.
centers=np.concatenate([np.linspace(2.1,22,64),np.geomspace(24,320,28)]);sigmas=np.concatenate([np.full(64,.48),np.geomspace(24,320,28)*.18])
# Each basis is multiplied by each observed position component; action channels remain independent.
def features(p,a):
 radius=np.linalg.norm(p,axis=1)
 basis=np.exp(-((radius[:,None]-centers)**2)/(2*sigmas*sigmas))
 return np.column_stack([basis*p[:,i,None] for i in range(3)]+[a,np.ones(len(p))])
p,a,v,y=collect(40000);f=features(p,a)
w=np.linalg.solve(f.T@f+np.eye(f.shape[1])*1e-7,f.T@y)
pt,at,vt,yt=collect(8000);yp=features(pt,at)@w
rmse=float(np.sqrt(np.mean((yp-yt)**2)));baseline=float(np.sqrt(np.mean(yt**2)))
np.savez_compressed(ROOT/'evidence/training-data-3d.npz',train_position=p,train_action=a,train_velocity=v,train_next_velocity=v+y*.01,heldout_position=pt,heldout_action=at,heldout_velocity=vt,heldout_next_velocity=vt+yt*.01)
angle=np.deg2rad(55);speed=np.sqrt(60/9)
s=State(9,0,0,0,speed*np.cos(angle),speed*np.sin(angle),0);e0=lib.bh_energy(C.byref(s));radii=[];depth=[]
for _ in range(50000):
 lib.bh_step(C.byref(s),0,0,0,.01);radii.append(np.linalg.norm([s.x,s.y,s.z]));depth.append(s.z)
energy=abs((lib.bh_energy(C.byref(s))-e0)/e0)
assert energy<.001 and max(abs(np.array(radii)-9))<.02 and min(depth)<-7 and max(depth)>7
rollout_errors=[]
for r0 in (6.,9.,12.,15.):
 speed=np.sqrt(60/r0);vel=np.array([0.,speed*np.cos(angle),speed*np.sin(angle)]);pos=np.array([r0,0.,0.]);truth=State(*pos,*vel,0)
 for _ in range(600):
  lib.bh_step(C.byref(truth),0,0,0,.01)
  vel+=(features(pos[None,:],np.zeros((1,3)))@w)[0]*.01;pos+=vel*.01
 rollout_errors.append(float(np.linalg.norm(pos-[truth.x,truth.y,truth.z])))
assert rmse<baseline*.1
model={'dimensions':3,'trainingTransitions':40000,'heldoutRmse':rmse,'radialCenters':centers.tolist(),'radialSigmas':sigmas.tolist(),'wx':w[:,0].tolist(),'wy':w[:,1].tolist(),'wz':w[:,2].tolist()}
(ROOT/'UnityProject/Assets/Resources/predictor.json').write_text(json.dumps(model))
report={'dimensions':3,'seed':42,'training_transitions':40000,'heldout_transitions':8000,'acceleration_component_rmse':rmse,'acceleration_xyz_rmse':np.sqrt(np.mean((yp-yt)**2,axis=0)).tolist(),'zero_acceleration_baseline_rmse':baseline,'velocity_step_component_rmse_dt_001':rmse*.01,'six_second_inclined_rollout_position_errors_r6_r9_r12_r15':rollout_errors,'orbit_steps':50000,'orbit_radius_min':min(radii),'orbit_radius_max':max(radii),'orbit_z_min':min(depth),'orbit_z_max':max(depth),'relative_energy_error_final':energy,'scope':'heldout IID 3D positions radius 2.4..300, random 3-axis actions and velocities; out-of-domain not validated'}
(ROOT/'evidence/validation-3d.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
