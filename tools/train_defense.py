"""Warm-start and evaluate a logistic danger estimator from observed native projectile encounters."""
from pathlib import Path
import ctypes as C,json,numpy as np
root=Path(__file__).resolve().parents[1];rng=np.random.default_rng(83)
class S(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','vx','vy','vz')]+[('captured',C.c_int)]
class H(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','mu','capture')]
lib=C.CDLL(str(root/'UnityProject/Assets/Plugins/macOS/libblackhole.dylib'));lib.bh_step_multi.argtypes=[C.POINTER(S),C.c_double,C.c_double,C.c_double,C.c_double]
holes=json.loads((root/'UnityProject/Assets/Resources/universe.json').read_text())['holes'];array=(H*len(holes))(*[H(**h) for h in holes]);lib.bh_set_holes(array,len(holes))
def features(p,v):
 d=np.linalg.norm(p);vv=v@v;t=np.clip(-(p@v)/(vv+1e-6),0,1.2);closest=np.linalg.norm(p+v*t);closing=-(p@v)/(d*np.sqrt(vv)+1e-6)
 return np.array([1,d/30,closest/12,t/1.2,closing,np.sqrt(vv)/25])
def collect(n,thrusting=False):
 X=[];Y=[]
 for i in range(n):
  a=np.array([rng.uniform(-12,145),rng.uniform(5,18),rng.uniform(-16,16)])
  v=np.array([rng.uniform(2,4),rng.uniform(-.5,.5),rng.uniform(-.5,.5)])
  direction=rng.normal(size=3);direction/=np.linalg.norm(direction);p=a+direction*rng.uniform(5,25)
  aim=a+v*rng.uniform(.2,1.1)+rng.normal(size=3)*rng.uniform(.1,5);shotv=(aim-p);shotv=shotv/np.linalg.norm(shotv)*18
  X.append(features(p-a,shotv-v));agent=S(*a,*v,0);shot=S(*p,*shotv,0);minimum=np.linalg.norm(p-a);u=rng.uniform(-1,1,3) if thrusting else np.zeros(3)
  for k in range(24):
   lib.bh_step_multi(C.byref(agent),*u,.05);lib.bh_step_multi(C.byref(shot),0,0,0,.05)
   minimum=min(minimum,np.linalg.norm([agent.x-shot.x,agent.y-shot.y,agent.z-shot.z]))
  Y.append(float(minimum<2.4))
 return np.array(X),np.array(Y)
def sigmoid(x):return 1/(1+np.exp(-np.clip(x,-30,30)))
X,y=collect(4000);xt,yt=collect(1000);w=np.zeros(6)
for epoch in range(80):
 order=rng.permutation(len(X))
 for start in range(0,len(X),128):
  b=order[start:start+128];w-=.4*(X[b].T@(sigmoid(X[b]@w)-y[b])/len(b)+.0003*w)
prior=w.copy();p=sigmoid(xt@w);brier=float(np.mean((p-yt)**2));baseline=float(np.mean((yt-y.mean())**2));assert brier<baseline*.8
xs,ys=collect(1000,True);xe,ye=collect(1000,True);before=float(np.mean((sigmoid(xe@w)-ye)**2));
for row,label in zip(xs,ys):w-=.02*(sigmoid(row@w)-label)*row
change=float(np.linalg.norm(w-prior));after=float(np.mean((sigmoid(xe@w)-ye)**2));assert change>.01
(root/'UnityProject/Assets/Resources/defense-risk.json').write_text(json.dumps({'weights':prior.tolist(),'warmSamples':4000,'heldoutBrier':brier}))
report={'features':['bias','distance/30','linear_closest_approach/12','time_to_closest/1.2','closing_cosine','relative_speed/25'],'label':'actual native minimum distance <2.4 during next 1.2 seconds','training_encounters':4000,'heldout_encounters':1000,'heldout_positive_rate':float(yt.mean()),'heldout_brier':brier,'constant_prior_baseline_brier':baseline,'online_shift':'1000 observed encounters with independently thrusting agents','online_weight_l2_change':change,'shifted_holdout_brier_before':before,'shifted_holdout_brier_after':after,'runtime_learning_rate':.02,'limits':'Model estimates danger under observed behavior; labels depend on agent evasive behavior. No future player input. Offline synthetic/native warm start; runtime updates use actual game encounters.'}
(root/'evidence/defense-learning-checks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
