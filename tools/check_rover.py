"""Independent native ABI checks for the rover contact mode, without Unity or a controller."""
from pathlib import Path
import ctypes as C,random,math,json
root=Path(__file__).resolve().parents[1]
class State(C.Structure):
 _fields_=[(n,C.c_double) for n in ('x','y','z','vx','vy','vz')]+[('captured',C.c_int)]
class Planet(C.Structure):
 _fields_=[(n,C.c_double) for n in ('x','y','z','mu','radius','atmosphere','seed')]
class Rock(C.Structure):
 _fields_=[(n,C.c_double) for n in ('x','y','z','radius')]
lib=C.CDLL(str(root/'UnityProject/Assets/Plugins/macOS/libblackhole.dylib'))
lib.bh_set_planets.argtypes=[C.POINTER(Planet),C.c_int]
lib.bh_set_rocks.argtypes=[C.POINTER(Rock),C.c_int]
lib.bh_terrain_height.argtypes=[C.c_int,C.c_double,C.c_double,C.c_double];lib.bh_terrain_height.restype=C.c_double
lib.bh_step_world.argtypes=[C.POINTER(State),C.c_double,C.c_double,C.c_double,C.c_double,C.c_int]
def step(state,mode=3,dt=.01):lib.bh_step_world(C.byref(state),0,0,0,dt,mode)
planet=(Planet*1)(Planet(0,0,0,6000**2*2.4,6000,780,4));assert lib.bh_set_planets(planet,1)==1
rng=random.Random(42);errors=[]
for _ in range(200):
 d=[rng.uniform(-1,1) for _ in range(3)];n=math.sqrt(sum(x*x for x in d));d=[x/n for x in d]
 height=lib.bh_terrain_height(0,*d);s=State(*(x*(6000+height+.2) for x in d),0,0,0,0)
 for i in range(20):step(s)
 r=math.sqrt(s.x*s.x+s.y*s.y+s.z*s.z);h=lib.bh_terrain_height(0,s.x,s.y,s.z);errors.append(abs(r-6000-h-.95))
assert max(errors)<1e-7,max(errors)
flat=(Planet*1)(Planet(0,0,0,0,6000,0,0));assert lib.bh_set_planets(flat,1)==1
rolling=State(0,6000+.8,0,4,0,0,0);ship=State(0,6000+.5,0,4,0,0,0);step(rolling);step(ship,0)
assert rolling.vx>ship.vx>0
rocks=(Rock*1)(Rock(0,6000+.15,0,1.2));assert lib.bh_set_rocks(rocks,1)==1
obstacle=State(0,6002,0,0,-1,0,0);step(obstacle)
assert obstacle.y-(6000+.15)>=1.2+.95-1e-8
unchanged=bytes(obstacle);step(obstacle,dt=0);assert bytes(obstacle)==unchanged
report={'random_rover_contacts':200,'max_rover_clearance_error':max(errors),'rover_contact_clearance_metres':.95,'rover_lower_rolling_resistance_than_ship':True,'boulder_contact_uses_rover_clearance':True,'invalid_dt_is_noop':True,'scope':'Native mode 3 contact ABI only; driving and boarding are separately tested in the packaged Unity player.'}
(root/'evidence/rover-native-checks.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
