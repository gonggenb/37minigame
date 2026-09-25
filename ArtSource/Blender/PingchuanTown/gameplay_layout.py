"""Shared authored coordinates used by the Blender export and Unity layout manifest."""
import math
SCALE=.4
RIVER=[(-125,17),(-101,1),(-76,-30),(-40,-45),(-12,-53),(23,-65),(42,-91),(80,-107),(124,-93)]
CANAL=[(-12,-53),(-7,-26),(-6,0),(-4,29),(8,53),(41,61),(63,85),(75,118)]
ROUTES=[('Entry',[(-64,-108),(-56,-81),(-49,-63),(-28,-33),(-26,-12),(-25,19),(-25,36)],6.5),('North',[(-25,36),(-19,50),(0,77),(25,87),(31,111)],5.5),('West',[(-25,15),(-49,23),(-69,46),(-83,58)],5),('East',[(-25,-12),(0,-15),(28,-14),(52,-18),(78,2),(94,22)],6),('Camp',[(-27,-30),(0,-38),(23,-50),(52,-66),(66,-70),(94,-51),(114,-37)],5.5),('FieldCamp',[(52,-18),(50,-35),(52,-66)],4.5),('NorthField',[(28,-14),(36,20),(37,45),(25,87)],4.5),('WestLoop',[(-49,-63),(-77,-32),(-84,2),(-69,46)],3.5),('CaveLoop',[(-83,58),(-69,71),(-43,70),(-19,50)],3.2)]
def smooth(p,n):
 out=[]
 for i in range(len(p)-1):
  a,b,c,d=p[max(0,i-1)],p[i],p[i+1],p[min(len(p)-1,i+2)]
  for j in range(n):
   t=j/n;out.append(tuple(.5*(2*b[k]+(-a[k]+c[k])*t+(2*a[k]-5*b[k]+4*c[k]-d[k])*t*t+(-a[k]+3*b[k]-3*c[k]+d[k])*t*t*t) for k in [0,1]))
 return out+[p[-1]]
def point(p):return dict(x=p[0]*SCALE,y=p[1]*SCALE)
def build():
 rivers=[(smooth(RIVER,8),5.7),(smooth(CANAL,7),2.7)];roads=[(n,smooth(p,8),w) for n,p,w in ROUTES];bridges=[]
 for name,pts,w in roads:
  for a,b in zip(pts,pts[1:]):
   for rp,rw in rivers:
    for c,d in zip(rp,rp[1:]):
     ax,ay=b[0]-a[0],b[1]-a[1];bx,by=d[0]-c[0],d[1]-c[1];den=ax*by-ay*bx
     if abs(den)<1e-9:continue
     t=((c[0]-a[0])*by-(c[1]-a[1])*bx)/den;u=((c[0]-a[0])*ay-(c[1]-a[1])*ax)/den
     if not(0<=t<=1 and 0<=u<=1):continue
     x,y=a[0]+t*ax,a[1]+t*ay
     if any(math.dist((x*SCALE,y*SCALE),(v['center']['x'],v['center']['y']))<5*SCALE for v in bridges):continue
     si=abs(den)/(math.hypot(ax,ay)*math.hypot(bx,by));ang=math.atan2(ay,ax)-math.pi/2
     bridges.append(dict(name=name,center=point((x,y)),angle=-ang*180/math.pi,length=(rw+3.7)/max(.32,si)*SCALE,width=(w+.5)*SCALE,stone=False))
 for y in [-21,8,30]:
  for a,b in zip(rivers[1][0],rivers[1][0][1:]):
   if min(a[1],b[1])<=y<max(a[1],b[1]):x=a[0]+(b[0]-a[0])*(y-a[1])/(b[1]-a[1]);break
  bridges.append(dict(name='Canal '+str(y),center=point((x,y)),angle=-90,length=7*SCALE,width=3.6*SCALE,stone=True))
 return dict(scale=SCALE,rivers=[dict(points=[point(p) for p in r],width=w*SCALE) for r,w in rivers],roads=[dict(name=n,points=[point(p) for p in p],width=w*SCALE) for n,p,w in roads],bridges=bridges)
