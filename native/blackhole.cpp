#include <cmath>
#include <cstdint>
extern "C" {
struct State { double x,y,z,vx,vy,vz; int captured; };
// Dimensionless 3D Newtonian dynamics; radius 1.8 is an artificial capture boundary.
void bh_step(State* s,double ax,double ay,double az,double dt) {
 if(!s || s->captured || dt<=0 || dt>0.1) return;
 const double r=std::sqrt(s->x*s->x+s->y*s->y+s->z*s->z);
 if(r<=1.8){s->captured=1; return;}
 const double g=-60.0/(r*r*r);
 s->vx+=(g*s->x+ax*0.9)*dt; s->vy+=(g*s->y+ay*0.9)*dt; s->vz+=(g*s->z+az*0.9)*dt;
 s->x+=s->vx*dt; s->y+=s->vy*dt; s->z+=s->vz*dt;
 if(s->x*s->x+s->y*s->y+s->z*s->z<=1.8*1.8) s->captured=1;
}
double bh_energy(const State* s){return .5*(s->vx*s->vx+s->vy*s->vy+s->vz*s->vz)-60/std::sqrt(s->x*s->x+s->y*s->y+s->z*s->z);}
}
extern "C" {
struct Hole {double x,y,z,mu,capture;};
#include "planet_physics.inc"
int bh_set_rocks(const Rock* data,int count){if(count<0||count>192||(!data&&count>0))return 0;for(int i=0;i<count;i++)rocks[i]=data[i];rockCount=count;return 1;}
int bh_set_planets(const Planet* data,int count){if(count<0||count>8||(!data&&count>0))return 0;for(int i=0;i<count;i++)planets[i]=data[i];planetCount=count;return 1;}
double bh_terrain_height(int index,double x,double y,double z){if(index<0||index>=planetCount)return 0;return terrain(planets[index],x,y,z);}
int bh_planet_info(const State* s,double* out){if(!s||!out||planetCount==0)return -1;int nearest=-1;double best=1e9;for(int i=0;i<planetCount;i++){const auto&p=planets[i];double x=s->x-p.x,y=s->y-p.y,z=s->z-p.z;double r=std::sqrt(x*x+y*y+z*z),alt=r-p.radius-terrain(p,x,y,z);if(alt<best){nearest=i;best=alt;out[0]=alt;out[1]=density(p,p.radius>100?r-p.radius:alt);out[2]=(s->vx*x+s->vy*y+s->vz*z)/(r+1e-9);}}return nearest;}
static Hole holes[128];static int holeCount=0;static double innerWindow=0,outerWindow=0;
void bh_set_gravity_window(double inner,double outer){innerWindow=inner;outerWindow=outer;}
int bh_set_holes(const Hole* data,int count){if(!data||count<1||count>128)return 0;for(int i=0;i<count;i++)holes[i]=data[i];holeCount=count;return 1;}
void bh_step_world(State* s,double ax,double ay,double az,double dt,int mode){
 if(!s||s->captured||dt<=0||dt>.1)return;
 double gx=0,gy=0,gz=0;
 for(int i=0;i<holeCount;i++){double x=s->x-holes[i].x,y=s->y-holes[i].y,z=s->z-holes[i].z,r2=x*x+y*y+z*z;
  if(r2<=holes[i].capture*holes[i].capture){s->captured=1;return;}
  double r=std::sqrt(r2),fade=1;if(outerWindow>innerWindow&&innerWindow>0){double scale=std::fmax(1.,holes[i].capture/1.8);double t=(r-innerWindow*scale)/((outerWindow-innerWindow)*scale);t=t<0?0:t>1?1:t;fade=1-t*t*(3-2*t);}
  double g=-holes[i].mu*fade/(r2*r);gx+=g*x;gy+=g*y;gz+=g*z;
 }
 double drag=0;
 for(int i=0;i<planetCount;i++){const auto&p=planets[i];double x=s->x-p.x,y=s->y-p.y,z=s->z-p.z,r=std::sqrt(x*x+y*y+z*z),rr=std::fmax(r,p.radius*.5);double fade=1;if(outerWindow>innerWindow&&innerWindow>0){double inner=p.radius>100?p.radius*5:innerWindow,outer=p.radius>100?p.radius*8:outerWindow;double t=std::fmax(0.,std::fmin(1.,(r-inner)/(outer-inner)));fade=1-t*t*(3-2*t);}double g=-p.mu*fade/(rr*rr*rr);gx+=g*x;gy+=g*y;gz+=g*z;double alt=r-p.radius;if(p.radius<100)alt-=terrain(p,x,y,z);double speed=std::sqrt(s->vx*s->vx+s->vy*s->vy+s->vz*s->vz);drag+=std::fmin(1.8,.025*density(p,alt)*speed);}
 s->vx+=(gx+.9*ax)*dt;s->vy+=(gy+.9*ay)*dt;s->vz+=(gz+.9*az)*dt;
 double damping=std::exp(-drag*dt);s->vx*=damping;s->vy*=damping;s->vz*=damping;
 s->x+=s->vx*dt;s->y+=s->vy*dt;s->z+=s->vz*dt;
 for(int i=0;i<planetCount;i++){const auto&p=planets[i];double x=s->x-p.x,y=s->y-p.y,z=s->z-p.z,r=std::sqrt(x*x+y*y+z*z);if(r<1e-8){x=1;y=z=0;r=1;}if(r>p.radius+p.radius*.2+5)continue;double clearance=mode==3?.95:mode==2?1.15:mode==1?.12:.6;double surface=p.radius+terrain(p,x,y,z)+clearance;
  if(r<surface){if(mode==1){s->captured=2;return;}double nx=x/r,ny=y/r,nz=z/r;const double e=p.radius>100?.03/p.radius:.001;double hx=(terrain(p,nx+e,ny,nz)-terrain(p,nx-e,ny,nz))/(2*e),hy=(terrain(p,nx,ny+e,nz)-terrain(p,nx,ny-e,nz))/(2*e),hz=(terrain(p,nx,ny,nz+e)-terrain(p,nx,ny,nz-e))/(2*e);double projection=hx*nx+hy*ny+hz*nz;double ux=nx-(hx-projection*nx)/p.radius,uy=ny-(hy-projection*ny)/p.radius,uz=nz-(hz-projection*nz)/p.radius;double norm=std::sqrt(ux*ux+uy*uy+uz*uz);ux/=norm;uy/=norm;uz/=norm;
   s->x=p.x+nx*surface;s->y=p.y+ny*surface;s->z=p.z+nz*surface;double vn=s->vx*ux+s->vy*uy+s->vz*uz;if(vn<0){s->vx-=vn*ux;s->vy-=vn*uy;s->vz-=vn*uz;}double friction=std::exp(-(mode==3?.15:mode==2?2.5:.7)*dt);s->vx*=friction;s->vy*=friction;s->vz*=friction;
  }
 }
 for(int i=0;i<rockCount;i++){const auto& rock=rocks[i];double x=s->x-rock.x,y=s->y-rock.y,z=s->z-rock.z,rr=std::sqrt(x*x+y*y+z*z),clearance=mode==3?.95:mode==2?1.15:mode==1?.12:.6,surface=rock.radius+clearance;if(rr>=surface||rr<1e-8)continue;if(mode==1){s->captured=2;return;}x/=rr;y/=rr;z/=rr;s->x=rock.x+x*surface;s->y=rock.y+y*surface;s->z=rock.z+z*surface;double vn=s->vx*x+s->vy*y+s->vz*z;if(vn<0){s->vx-=vn*x;s->vy-=vn*y;s->vz-=vn*z;}}
 if(rockCount>0&&mode!=1)for(int i=0;i<planetCount;i++){const auto&p=planets[i];double x=s->x-p.x,y=s->y-p.y,z=s->z-p.z,r=std::sqrt(x*x+y*y+z*z);if(r<1e-8||r>p.radius+p.radius*.2+5)continue;double surface=p.radius+terrain(p,x,y,z)+(mode==3?.95:mode==2?1.15:.6);if(r<surface){s->x=p.x+x/r*surface;s->y=p.y+y/r*surface;s->z=p.z+z/r*surface;}}
 for(int i=0;i<holeCount;i++){double x=s->x-holes[i].x,y=s->y-holes[i].y,z=s->z-holes[i].z;if(x*x+y*y+z*z<=holes[i].capture*holes[i].capture)s->captured=1;}
}
void bh_gravity(double px,double py,double pz,double* ox,double* oy,double* oz){double gx=0,gy=0,gz=0;
 for(int i=0;i<holeCount;i++){double x=px-holes[i].x,y=py-holes[i].y,z=pz-holes[i].z,r=std::fmax(.001,std::sqrt(x*x+y*y+z*z)),fade=1;if(outerWindow>innerWindow&&innerWindow>0){double scale=std::fmax(1.,holes[i].capture/1.8);double t=std::fmax(0.,std::fmin(1.,(r-innerWindow*scale)/((outerWindow-innerWindow)*scale)));fade=1-t*t*(3-2*t);}double k=-holes[i].mu*fade/(r*r*r);gx+=k*x;gy+=k*y;gz+=k*z;}
 for(int i=0;i<planetCount;i++){double x=px-planets[i].x,y=py-planets[i].y,z=pz-planets[i].z,r=std::sqrt(x*x+y*y+z*z),rr=std::fmax(r,planets[i].radius*.5),fade=1;if(outerWindow>innerWindow&&innerWindow>0){double inner=planets[i].radius>100?planets[i].radius*5:innerWindow,outer=planets[i].radius>100?planets[i].radius*8:outerWindow;double t=std::fmax(0.,std::fmin(1.,(r-inner)/(outer-inner)));fade=1-t*t*(3-2*t);}double k=-planets[i].mu*fade/(rr*rr*rr);gx+=k*x;gy+=k*y;gz+=k*z;}
 if(ox)*ox=gx;if(oy)*oy=gy;if(oz)*oz=gz;
}
void bh_step_multi(State* s,double ax,double ay,double az,double dt){bh_step_world(s,ax,ay,az,dt,0);}
}
