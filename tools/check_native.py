"""3D ABI and physical invariants, independent of the learned model."""
from pathlib import Path
import ctypes as C,json,numpy as np
root=Path(__file__).resolve().parents[1]
class State(C.Structure):
 _fields_=[(k,C.c_double) for k in ('x','y','z','vx','vy','vz')]+[('captured',C.c_int)]
lib=C.CDLL(str(root/'UnityProject/Assets/Plugins/macOS/libblackhole.dylib'))
lib.bh_step.argtypes=[C.POINTER(State),C.c_double,C.c_double,C.c_double,C.c_double]
def step(s,a=(0,0,0),dt=.01):lib.bh_step(C.byref(s),*a,dt)
def velocity(s):return np.array([s.vx,s.vy,s.vz])
base=State(7,3,4,0,0,0,0);step(base)
for i in range(3):
 u=np.eye(3)[i];s=State(7,3,4,0,0,0,0);step(s,u);assert np.allclose(velocity(s)-velocity(base),u*.009,atol=1e-12)
# General axis-angle rotation, not only coordinate permutation.
axis=np.array([1.,2.,3.]);axis/=np.linalg.norm(axis);angle=.73
K=np.array([[0,-axis[2],axis[1]],[axis[2],0,-axis[0]],[-axis[1],axis[0],0]])
R=np.eye(3)*np.cos(angle)+(1-np.cos(angle))*np.outer(axis,axis)+np.sin(angle)*K
p=np.array([7.,3.,4.]);v=np.array([.3,1.,-.5]);u=np.array([.2,-.1,.3]);s=State(*p,*v,0);t=State(*(R@p),*(R@v),0);step(s,u);step(t,R@u)
assert np.allclose([t.x,t.y,t.z],R@np.array([s.x,s.y,s.z]),atol=1e-12)
assert np.allclose(velocity(t),R@velocity(s),atol=1e-12)
for dt in (0,-.01,.11):
 s=State(7,3,4,1,2,3,0);before=bytes(s);step(s,dt=dt);assert bytes(s)==before
s=State(0,0,1.81,0,0,-2,0);step(s);assert s.captured==1
before=bytes(s);step(s,(1,1,1));assert bytes(s)==before
s=State(9,0,0,0,1.5,2.1,0);step(s);assert s.z>0
result={'dimensions':3,'ABI_state_bytes':C.sizeof(State),'three_axis_thrust':'PASS','arbitrary_rotation_equivariance':'PASS','invalid_dt_noop':'PASS','3d_capture_freeze':'PASS','nonzero_z_transition':'PASS'}
(root/'evidence/native-checks-3d.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
