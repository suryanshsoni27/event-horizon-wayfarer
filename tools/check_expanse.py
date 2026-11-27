"""Independent native terrain/contact validation on a 12 km complete planet."""
from pathlib import Path
import ctypes as C,json,numpy as np
r=Path(__file__).resolve().parents[1]
class S(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','vx','vy','vz')]+[('captured',C.c_int)]
class P(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','mu','radius','atmosphere','seed')]
lib=C.CDLL(str(r/'UnityProject/Assets/Plugins/macOS/libblackhole.dylib'));lib.bh_set_planets.argtypes=[C.POINTER(P),C.c_int];lib.bh_step_world.argtypes=[C.POINTER(S),C.c_double,C.c_double,C.c_double,C.c_double,C.c_int];lib.bh_terrain_height.argtypes=[C.c_int,C.c_double,C.c_double,C.c_double];lib.bh_terrain_height.restype=C.c_double
R=6000.;body=P(0,-6000,0,R*R*2.4,R,R*.13,4);lib.bh_set_planets((P*1)(body),1);center=np.array([0,-R,0]);rng=np.random.default_rng(61);directions=rng.normal(size=(3000,3));directions/=np.linalg.norm(directions,axis=1)[:,None]
def terrain_hash(x,y,z,seed):
 h=(x*374761393+y*668265263+z*2246822519+seed*3266489917)&0xffffffff;h=((h^(h>>13))*1274126177)&0xffffffff;h^=h>>16;return (h&0xffffff)/16777215.
def terrain_noise(x,y,z,seed):
 import math
 ix,iy,iz=math.floor(x),math.floor(y),math.floor(z);x-=ix;y-=iy;z-=iz;x=x*x*(3-2*x);y=y*y*(3-2*y);z=z*z*(3-2*z)
 return sum(terrain_hash(ix+dx,iy+dy,iz+dz,seed)*(x if dx else 1-x)*(y if dy else 1-y)*(z if dz else 1-z) for dx in range(2) for dy in range(2) for dz in range(2))
def height(d):
 x,y,z=d;s=4;c=np.sin(x*3.7+s)*np.cos(z*4.1-s*.4)+.5*np.sin(y*5.2+x*2.3+s*.2)+.12*np.sin(x*17+y*9)*np.cos(z*19+x*5)+.045*np.sin(x*61+z*43)*np.cos(y*53);m=np.clip((c+.15)/.85,0,1);m=m*m*(3-2*m);ridge_noise=terrain_noise(x*8+s*.7,y*8,z*8,s)*2-1;ridge=(1-ridge_noise*ridge_noise)**6;return R/1200*max(0,(c+.12)*32+m*(ridge*100+12*(terrain_noise(x*41,y*41,z*41,s+7)-.5)+3*(terrain_noise(x*137,y*137,z*137,s+17)-.5))+m*.32*np.sin(x*1801+y*1511)*np.sin(z*1733+y*997))
errors=[abs(height(d)-lib.bh_terrain_height(0,*d)) for d in directions];assert max(errors)<1e-8
heights=np.array([height(d) for d in directions]);assert heights.max()>500
contact_error=[]
for d in directions[:200]:
 h=height(d);s=S(*(center+d*(R+h+1)),*(-d*2),0)
 for _ in range(200):lib.bh_step_world(C.byref(s),0,0,0,.01,2)
 delta=np.array([s.x,s.y,s.z])-center;alt=np.linalg.norm(delta)-R-height(delta/np.linalg.norm(delta));contact_error.append(alt-1.15)
assert min(contact_error)>-1e-6 and max(contact_error)<.02
# Cube-face coordinates are shared along boundaries: the formula is global and periodic in direction.
seam_errors=[]
def face_direction(face,u,v):
 n=np.eye(3)[face//2]*(1 if face%2==0 else -1);a=np.array([n[1],n[2],n[0]]);b=np.cross(n,a);d=n+a*u+b*v;return d/np.linalg.norm(d)
for t in np.linspace(-1,1,41):
 a=face_direction(0,1,-t);b=face_direction(4,t,-1);assert np.linalg.norm(a-b)<1e-10;seam_errors.append(abs(height(a)-height(b)))
report={'diameter_metres':R*2,'circumference_metres':2*np.pi*R,'surface_area_square_km':4*np.pi*R*R/1e6,'terrain_samples':3000,'analytic_terrain_max_error':max(errors),'sampled_height_range_metres':[float(heights.min()),float(heights.max())],'random_spherical_contact_locations':200,'walker_contact_clearance_error_range':list(map(float,[min(contact_error),max(contact_error)])),'shared_edge_height_error':max(seam_errors),'scope':'native C++ independent CPU checks; terrain field covers the complete sphere, including poles; Unity LOD coverage/contact/travel is separately packaged-tested'}
(r/'evidence/expanse-native-checks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
# Decorative boulders have independent native sphere contact, not just visual props.
class Rock(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','radius')]
lib.bh_set_rocks.argtypes=[C.POINTER(Rock),C.c_int]
h=height(np.array([0.,1.,0.]));rock=Rock(0,h+1,0,2);assert lib.bh_set_rocks((Rock*1)(rock),1)==1
walker=S(0,h+6,0,0,-4,0,0)
for _ in range(400):lib.bh_step_world(C.byref(walker),0,0,0,.01,2)
assert walker.y-rock.y>=3.1499
shot=S(0,h+6,0,0,-9,0,0)
for _ in range(80):lib.bh_step_world(C.byref(shot),0,0,0,.01,1)
assert shot.captured==2
lib.bh_set_rocks((Rock*1)(rock),0)
# Large source prediction uses normalized coordinates, keeping the existing learned domain.
class Hole(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','mu','capture')]
lib.bh_set_holes.argtypes=[C.POINTER(Hole),C.c_int];lib.bh_set_gravity_window.argtypes=[C.c_double,C.c_double]
holes=[Hole(47500,7500,55000,800000,360),Hole(-55000,2500,65000,800000,360)]
lib.bh_set_holes((Hole*2)(*holes),2);lib.bh_set_gravity_window(60,100)
m=json.loads((r/'UnityProject/Assets/Resources/predictor.json').read_text());centers=np.array(m['radialCenters']);sigmas=np.array(m['radialSigmas']);W=np.array([m['wx'],m['wy'],m['wz']]).T

def field(p,u):
 q=np.linalg.norm(p);b=np.exp(-(q-centers)**2/(2*sigmas**2));b[np.abs(q-centers)>5*sigmas]=0
 return np.concatenate([b*p[0],b*p[1],b*p[2],u,[1]])@W
errors=[]
for d in directions[:2000]:
 point=center+d*rng.uniform(R*1.13+200,R*8);velocity=rng.uniform(-10,10,3);action=rng.uniform(-14,14,3);s=S(*point,*velocity,0);lib.bh_step_world(C.byref(s),*action,.01,0)
 pred=field(np.zeros(3),action)-field(np.zeros(3),np.zeros(3))
 for loc,mu,scale,inner,outer in [(center,R*R*2.4,R/22,R*5,R*8)]+[(np.array([h.x,h.y,h.z]),h.mu,h.capture/1.8,60*h.capture/1.8,100*h.capture/1.8) for h in holes]:
  delta=point-loc;t=np.clip((np.linalg.norm(delta)-inner)/(outer-inner),0,1);pred+=field(delta/scale,np.zeros(3))*(mu/(60*scale*scale))*(1-t*t*(3-2*t))
 errors.append((np.array([s.vx,s.vy,s.vz])-velocity)/.01-pred)
rmse=float(np.sqrt(np.mean(np.array(errors)**2)));assert rmse<.02,rmse
report.update(native_boulder_walker_contact='PASS',native_boulder_projectile_contact='PASS',normalized_large_source_transitions=2000,normalized_large_source_component_acceleration_rmse=rmse)
(r/'evidence/expanse-native-checks.json').write_text(json.dumps(report,indent=2));print('Boulder contacts PASS; normalized large-world acceleration RMSE',rmse)
