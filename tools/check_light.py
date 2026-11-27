"""CPU reference for the shader's affine null-ray ODE, without the emitting disk."""
from pathlib import Path
import math,json,numpy as np
root=Path(__file__).resolve().parents[1]
def ray(b,scale=.045):
 p=np.array([0.,0.,-100.]);alpha=math.asin(b*math.sqrt(.99)/100);d=np.array([math.sin(alpha),0.,math.cos(alpha)])
 radial=p/100;rv=d@radial;v=rv*radial+(d-rv*radial)/math.sqrt(.99);initial=v.copy();L2=np.dot(np.cross(p,v),np.cross(p,v));max_constraint=0
 def acc(q):
  r2=q@q;return -1.5*L2*q/(r2*r2*math.sqrt(r2))
 for i in range(320):
  r=np.linalg.norm(p)
  if r<1.002:return True,v,initial,i,max_constraint
  if r>112 and p@v>0:return False,v,initial,i,max_constraint
  constraint=(p@v/r)**2+(1-1/r)*L2/r**2;max_constraint=max(max_constraint,abs(constraint-1))
  h=np.clip(r*scale,.025,2.4);a=acc(p);p+=v*h+.5*a*h*h;v+=.5*(a+acc(p))*h
 raise AssertionError('Unresolved ray '+str(b))
# Avoid testing exactly the unstable critical orbit, which may exhaust any finite budget.
assert ray(2.58)[0] and not ray(2.62)[0]
_,v,initial,steps,residual=ray(20)
deflection=math.acos(np.clip((v@initial)/(np.linalg.norm(v)*np.linalg.norm(initial)),-1,1))
assert .09<deflection<.13
report={'shader_ODE':'Schwarzschild affine null geodesic, Rs=1','analytic_critical_impact_parameter':3*math.sqrt(3)/2,'b_2_58_capture':'PASS','b_2_62_escape':'PASS','b_20_deflection_radians':deflection,'weak_field_leading_term_2Rs_over_b':.1,'b20_max_null_constraint_residual':residual,'limitations':'CPU double-precision reference of same integration; GPU image validated visually, finite-budget near-critical rays unresolved/dark'}
(root/'evidence/light-checks-3d.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
