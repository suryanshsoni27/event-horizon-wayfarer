using UnityEngine;

// Continuous, UV-mapped surfaces. Profiles are authored in metres and keep the existing IK pivots.
public sealed partial class SurfaceArt {
    public sealed partial class Geometry {
        public readonly System.Collections.Generic.List<Vector2> uv = new System.Collections.Generic.List<Vector2>();
        static Vector4 Curve(Vector4 a,Vector4 b,Vector4 c,Vector4 d,float t) {
            return .5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
        }
        public void Profile(Vector4[] shape,Color skin,Color armor,int sides=40,int rings=64,float sculpt=.025f) {
            int first=vertices.Count;
            for(int row=0;row<=rings;row++) {
                float f=row/(float)rings*(shape.Length-1);int k=Mathf.Min(shape.Length-2,(int)f);float t=f-k;
                Vector4 q=Curve(shape[Mathf.Max(0,k-1)],shape[k],shape[k+1],shape[Mathf.Min(shape.Length-1,k+2)],t);
                for(int col=0;col<=sides;col++) {
                    float a=col*Mathf.PI*2/sides,x=Mathf.Cos(a),y=Mathf.Sin(a);
                    float ridge=1+sculpt*Mathf.Sin(q.x*31+Mathf.Cos(a*3)*1.1f)*Mathf.Pow(Mathf.Abs(x),2);
                    float cheek=1+sculpt*.7f*Mathf.Cos(a*8)*Mathf.Sin(row/(float)rings*Mathf.PI);
                    vertices.Add(new Vector3(x*Mathf.Max(.003f,q.y)*ridge,q.w+y*Mathf.Max(.003f,q.z)*cheek,q.x));
                    colors.Add(Color.Lerp(skin,armor,Mathf.SmoothStep(0,1,(y+.25f)*.95f)));uv.Add(new Vector2(col/(float)sides*2,row/(float)rings*2.6f));normals.Add(Vector3.zero);
                    if(row<rings&&col<sides){int i=first+row*(sides+1)+col;triangles.AddRange(new[]{i,i+1,i+sides+1,i+1,i+sides+2,i+sides+1});}
                }
            }
            SmoothGrid(first,sides,rings,true);
        }
        public void Tube(Vector3 start,Vector3 end,float from,float to,Vector3 bend,Color c,int sides=18,int rings=12) {
            int first=vertices.Count;Vector3 axis=(end-start).normalized,right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized,up=Vector3.Cross(axis,right);
            for(int r=0;r<=rings;r++)for(int col=0;col<=sides;col++) {
                float t=r/(float)rings,a=col*Mathf.PI*2/sides;
                float radius=Mathf.Lerp(from,to,t)*(1+.06f*Mathf.Sin(t*Mathf.PI*8));
                vertices.Add(Vector3.Lerp(start,end,t)+bend*Mathf.Sin(t*Mathf.PI)+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius);colors.Add(c);uv.Add(new Vector2(col/(float)sides,t*1.5f));normals.Add(Vector3.zero);
                if(r<rings&&col<sides){int i=first+r*(sides+1)+col;triangles.AddRange(new[]{i,i+1,i+sides+1,i+1,i+sides+2,i+sides+1});}
            }
            SmoothGrid(first,sides,rings,true);
        }
        public void ArmorPlate(float z,float width,float height,float length,Color c) {
            int first=vertices.Count;const int sides=24,rows=8;
            for(int r=0;r<=rows;r++)for(int col=0;col<=sides;col++) {
                float t=r/(float)rows,a=Mathf.Lerp(-.10f,Mathf.PI+.10f,col/(float)sides),taper=.68f+.32f*Mathf.Sin(t*Mathf.PI);
                vertices.Add(new Vector3(Mathf.Cos(a)*width*taper,height+Mathf.Sin(a)*width*.34f-Mathf.Pow(t,3)*.13f,z+(t-.5f)*length));
                colors.Add(c);uv.Add(new Vector2(col/(float)sides,t*.45f));normals.Add(Vector3.zero);
                if(r<rows&&col<sides){int i=first+r*(sides+1)+col;triangles.AddRange(new[]{i,i+1,i+sides+1,i+1,i+sides+2,i+sides+1});}
            }
            SmoothGrid(first,sides,rows,false);
        }
        void SmoothGrid(int first,int sides,int rows,bool seam) {
            // Derive normals from the actual sculpt, rather than a spherical approximation.
            for(int r=0;r<=rows;r++)for(int c=0;c<=sides;c++) {
                int i=first+r*(sides+1)+c;
                Vector3 du=vertices[first+r*(sides+1)+(seam?(c+1)%sides:Mathf.Min(sides,c+1))]-vertices[first+r*(sides+1)+(seam?(c+sides-1)%sides:Mathf.Max(0,c-1))];
                Vector3 dv=vertices[first+Mathf.Min(rows,r+1)*(sides+1)+c]-vertices[first+Mathf.Max(0,r-1)*(sides+1)+c];
                Vector3 n=Vector3.Cross(du,dv).normalized;
                // Match the outward winding of the generated surface triangles.
                normals[i]=n;
            }
        }
    }
}
