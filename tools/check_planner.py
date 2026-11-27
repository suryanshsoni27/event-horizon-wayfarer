"""Reference of Unity's 27-action receding-horizon planner against true native transitions."""
from pathlib import Path
import ctypes as C,json,numpy as np
root=Path(__file__).resolve().parents[1];m=json.loads((root/'UnityProject/Assets/Resources/predictor.json').read_text());centers=np.array(m['radialCenters']);sigma=np.array(m['radialSigmas']);w=np.array([m['wx'],m['wy'],m['wz']]).T
class State(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','vx','vy','vz')]+[('captured',C.c_int)]
lib=C.CDLL(str(root/'UnityProject/Assets/Plugins/macOS/libblackhole.dylib'));lib.bh_step.argtypes=[C.POINTER(State),C.c_double,C.c_double,C.c_double,C.c_double]
choices=np.array([[x,y,z] for x in (-.5,0,.5) for y in (-.5,0,.5) for z in (-.5,0,.5)])
def predict(p,u):
 r=np.linalg.norm(p,axis=1);b=np.exp(-((r[:,None]-centers)**2)/(2*sigma*sigma));f=np.column_stack([b*p[:,i,None] for i in range(3)]+[u,np.ones(len(p))]);return f@w
def plan(p,v):
 p=np.tile(p,(27,1));v=np.tile(v,(27,1));cost=.4*(choices**2).sum(1)
 for _ in range(80):
  v+=predict(p,choices)*.03;p+=v*.03;r=np.linalg.norm(p,axis=1);radial=(p*v).sum(1)/r
  cost+=(.8*(r-9)**2+3*radial**2+.5*(np.linalg.norm(v,axis=1)-np.sqrt(60/9))**2)*.03
  cost+=np.where((r<2.4)|(r>16),100000,0)
 cost+=5*(np.linalg.norm(p,axis=1)-9)**2
 i=np.argmin(cost);return choices[i],float(cost[i]),float(cost[13])
speed=np.sqrt(60/11);angle=np.deg2rad(55);s=State(11,0,0,0,speed*np.cos(angle),speed*np.sin(angle),0);coast=State(*[getattr(s,k) for k in ('x','y','z','vx','vy','vz')],0)
selected,selected_cost,idle_cost=plan([s.x,s.y,s.z],[s.vx,s.vy,s.vz]);assert selected_cost<idle_cost
errors=[];coast_errors=[];actions=[]
for i in range(1800):
 if i%30==0:u,_,_=plan([s.x,s.y,s.z],[s.vx,s.vy,s.vz]);actions.append(u.tolist())
 lib.bh_step(C.byref(s),*u,.01);lib.bh_step(C.byref(coast),0,0,0,.01)
 errors.append(abs(np.linalg.norm([s.x,s.y,s.z])-9));coast_errors.append(abs(np.linalg.norm([coast.x,coast.y,coast.z])-9))
assert not s.captured and np.mean(errors)<np.mean(coast_errors)*.8
report={'mode':'MPC using learned xyz predictor; objective coded, policy not trained','candidate_actions':27,'horizon_seconds':2.4,'replan_seconds':.3,'initial_radius':11,'duration_seconds':18,'initial_selected_action':selected.tolist(),'selected_predicted_cost':selected_cost,'zero_action_predicted_cost':idle_cost,'native_mean_absolute_radius_error':float(np.mean(errors)),'coast_mean_absolute_radius_error':float(np.mean(coast_errors)),'final_radius':float(np.linalg.norm([s.x,s.y,s.z])),'capture':bool(s.captured),'scope':'one inclined recovery scenario, not a general safety or planning guarantee'}
(root/'evidence/planner-checks-3d.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
