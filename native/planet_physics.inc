// Shared deterministic terrain formula is mirrored in PlanetExpedition.Height.
struct Planet {double x,y,z,mu,radius,atmosphere,seed;};
struct Rock{double x,y,z,radius;};static Rock rocks[192];static int rockCount=0;
static Planet planets[8];static int planetCount=0;
static double terrainHash(int x,int y,int z,unsigned seed){uint32_t h=(uint32_t)x*374761393u+(uint32_t)y*668265263u+(uint32_t)z*2246822519u+seed*3266489917u;h=(h^(h>>13))*1274126177u;h^=h>>16;return (h&0xffffff)/16777215.;}
static double terrainNoise(double x,double y,double z,unsigned seed){int ix=(int)std::floor(x),iy=(int)std::floor(y),iz=(int)std::floor(z);x-=ix;y-=iy;z-=iz;x=x*x*(3-2*x);y=y*y*(3-2*y);z=z*z*(3-2*z);double value=0;for(int dx=0;dx<2;dx++)for(int dy=0;dy<2;dy++)for(int dz=0;dz<2;dz++)value+=terrainHash(ix+dx,iy+dy,iz+dz,seed)*(dx?x:1-x)*(dy?y:1-y)*(dz?z:1-z);return value;}
static double terrain(const Planet& p,double x,double y,double z){double r=std::sqrt(x*x+y*y+z*z);if(r<1e-9)return 0;x/=r;y/=r;z/=r;double s=p.seed>=1000?p.seed-1000:p.seed;
 if(p.radius<100)return std::fmax(-.25,.70*std::sin(x*4+s)*std::cos(z*5-s*.5)+.45*std::sin(y*9+z*3)+.20*std::sin(x*21+y*17+z*11)+.08*std::sin(x*57+z*41));
 double c=std::sin(x*3.7+s)*std::cos(z*4.1-s*.4)+.5*std::sin(y*5.2+x*2.3+s*.2)+.12*std::sin(x*17+y*9)*std::cos(z*19+x*5)+.045*std::sin(x*61+z*43)*std::cos(y*53);
 double mask=std::fmax(0.,std::fmin(1.,(c+.15)/.85));mask=mask*mask*(3-2*mask);
 double ridgeNoise=terrainNoise(x*8+s*.7,y*8,z*8,(unsigned)s)*2-1,ridge=std::pow(1-ridgeNoise*ridgeNoise,6);
 double h=(c+.12)*32+mask*(ridge*100+12*(terrainNoise(x*41,y*41,z*41,(unsigned)s+7)-.5)+3*(terrainNoise(x*137,y*137,z*137,(unsigned)s+17)-.5));
 if(p.seed>=1000){double alpine=std::fmax(0.,std::fmin(1.,(h-48)/70));alpine=alpine*alpine*(3-2*alpine);double crest=1-std::abs(terrainNoise(x*53+s*.3,y*53,z*53,(unsigned)s+31)*2-1),cut=terrainNoise(x*151,y*151,z*151,(unsigned)s+53)-.5;h+=alpine*(32*std::pow(crest,4)+8*cut);}
 h+=mask*.32*std::sin(x*1801+y*1511)*std::sin(z*1733+y*997);
 return std::fmax(0.,h*(p.radius/1200));
}
static double density(const Planet& p,double alt){if(alt>=p.atmosphere)return 0;double t=std::fmax(0.,std::fmin(1.,(p.atmosphere-alt)/std::fmax(2.,p.atmosphere*.2)));return std::exp(-std::fmax(0.,alt)/(p.atmosphere*.3))*t*t*(3-2*t);}
