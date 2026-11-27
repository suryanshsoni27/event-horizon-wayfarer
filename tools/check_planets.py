from pathlib import Path
import ctypes as C,json,math,numpy as np
root=Path(__file__).resolve().parents[1]
class S(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','vx','vy','vz')]+[('captured',C.c_int)]
class P(C.Structure):_fields_=[(k,C.c_double) for k in ('x','y','z','mu','radius','atmosphere','seed')]
lib=C.CDLL(str(root/'UnityProject/Assets/Plugins/macOS/libblackhole.dylib'));lib.bh_set_planets.argtypes=[C.POINTER(P),C.c_int];lib.bh_step_world.argtypes=[C.POINTER(S),C.c_double,C.c_double,C.c_double,C.c_double,C.c_int];lib.bh_terrain_height.argtypes=[C.c_int,C.c_double,C.c_double,C.c_double];lib.bh_terrain_height.restype=C.c_double;lib.bh_planet_info.argtypes=[C.POINTER(S),C.POINTER(C.c_double)]
bodies=(P*1)(P(0,0,0,193.6,22,8,4));lib.bh_set_planets(bodies,1)
def step(s,u=(0,0,0),dt=.01,mode=0):lib.bh_step_world(C.byref(s),*u,dt,mode)
def terrain(v):
 x,y,z=v/np.linalg.norm(v);return max(-.25,.7*np.sin(x*4+4)*np.cos(z*5-2)+.45*np.sin(y*9+z*3)+.2*np.sin(x*21+y*17+z*11)+.08*np.sin(x*57+z*41))
rng=np.random.default_rng(2);error=[]
for v in rng.normal(size=(1000,3)):error.append(abs(terrain(v)-lib.bh_terrain_height(0,*v)))
assert max(error)<1e-10
h=lib.bh_terrain_height(0,0,1,0);ground=22+h+.6
s=S(0,40,0,0,0,0,0);step(s);assert s.vy<0
s=S(0,22+h+1,0,10,0,0,0);step(s,dt=.05);assert 0<s.vx<10
fast=S(0,22+h+1,0,500,0,0,0);step(fast,dt=.1);assert 0<fast.vx<500
land=S(0,ground+.05,0,0,-3,0,0)
for _ in range(200):step(land)
alt=np.linalg.norm([land.x,land.y,land.z])-22-terrain(np.array([land.x,land.y,land.z]));assert alt>=.5999 and land.captured==0
walker=S(0,ground+1,0,0,-2,0,0)
for _ in range(200):step(walker,mode=2)
walkalt=np.linalg.norm([walker.x,walker.y,walker.z])-22-terrain(np.array([walker.x,walker.y,walker.z]));assert walkalt>=1.1499
shot=S(0,ground,0,0,-8,0,0)
for _ in range(30):step(shot,mode=1)
assert shot.captured==2
escape=S(0,ground,0,0,0,0,0)
for _ in range(400):step(escape,(0,8,0))
assert escape.y-ground>8 and escape.captured==0
outer=S(0,22+h+8.01,0,10,0,0,0);info=(C.c_double*3)();lib.bh_planet_info(C.byref(outer),info);assert info[1]==0
report={'terrain_native_formula_max_error':max(error),'planet_gravity':'PASS','drag_dissipates_tangential_velocity':'PASS','bounded_drag_no_reversal':'PASS','ship_surface_contact_clearance':alt,'walker_surface_contact_clearance':walkalt,'projectile_surface_collision':'PASS','powered_atmospheric_escape':'PASS','atmosphere_top_density_zero':'PASS','body_ABI_bytes':C.sizeof(P),'scope':'dimensionless Newtonian gravity + simplified exponential atmosphere, exponential bounded drag update and spherical terrain contact; not aerodynamic lift/CFD'}
(root/'evidence/planet-physics-checks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
