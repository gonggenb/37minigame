def lamp(g,x,y,z=0,h=2.8,light=True):
 rod(g,'Wood • aged cedar',(x,y,z),(x,y,z+h),.075,n=8)
 box(g,'Stone • blue limestone',(x,y,z+.14),(.4,.4,.28))
 box(g,'Lantern • amber silk',(x,y,z+h-.34),(.34,.34,.48))
 for dx in [-.2,.2]:
  for dy in [-.2,.2]: rod(g,'Wood • aged cedar',(x+dx,y+dy,z+h-.64),(x+dx,y+dy,z+h-.02),.035,n=4)
 for zz in [z+h-.64,z+h-.04]: box(g,'Wood • honey edges',(x,y,zz),(.5,.5,.08))
 rod(g,'Roof • midnight ceramic',(x,y,z+h+.02),(x,y,z+h+.20),.4,.10,n=4)
 if light:
  data=bpy.data.lights.new('Amber lantern','POINT'); data.energy=65; data.color=(1,.35,.065); data.shadow_soft_size=.65
  ob=bpy.data.objects.new('Lantern glow',data); coll(g).objects.link(ob); ob.location=(x,y,z+h-.3)
def fence(g,points,z=None):
 for a,b in zip(points,points[1:]):
  length=math.dist(a,b); count=max(1,math.ceil(length/1.5))
  posts=[]
  for i in range(count+1):
   t=i/count; x=a[0]+t*(b[0]-a[0]); y=a[1]+t*(b[1]-a[1]); zz=ground(x,y) if z is None else z
   rod(g,'Wood • aged cedar',(x,y,zz),(x,y,zz+1.1),.085)
   rod(g,'Wood • honey edges',(x,y,zz+1.1),(x,y,zz+1.25),.13,.04)
   posts.append((x,y,zz))
  for p,q in zip(posts,posts[1:]):
   for h in [.4,.9]: rod(g,'Wood • aged cedar',(p[0],p[1],p[2]+h),(q[0],q[1],q[2]+h),.06)
def bridge(g,y,width=2.6):
 cx=riverx(y); L=7.2
 def bz(t): return .46+.65*math.sin(t*math.pi)
 for i in range(25):
  t=(i+.5)/25; x=cx-L/2+t*L
  box(g,'Wood • honey edges',(x,y,bz(t)),(L/25-.025,width,.16))
 for side in [-1,1]:
  yy=y+side*(width/2-.05)
  for i in range(9):
   t=i/8; x=cx-L/2+t*L; z=bz(t)
   rod(g,'Wood • aged cedar',(x,yy,z-.35),(x,yy,z+1.1),.10)
   rod(g,'Wood • honey edges',(x,yy,z+1.1),(x,yy,z+1.24),.14,.06)
  for h in [.36,.9,-.25]: line(g,'Wood • aged cedar',[(cx-L/2+i*L/24,yy,bz(i/24)+h) for i in range(25)],.065)
 for x in [cx-L/2,cx+L/2]:
  for yy in [y-width/2-.35,y+width/2+.35]: lamp(g,x,yy,.4,2.1)
bridge('06 Main bridge • arched cedar',-6,2.7)
bridge('07 Camp bridge • narrow crossing',7,1.9)

def hip_roof(g,cx,cy,z,rx,ry,height,levels=10):
 # Curved hipped roof, separate ceramic pans and raised seam ridges.
 def ring(t):
  k=1-t; zz=z+height*(t*t*.78+t*.22)+.30*math.exp(-t*12)
  return [(cx+sx*(rx*k+max(0,rx-ry)*.8*t),cy+sy*ry*k,zz+.28*(k**5)) for sx,sy in [(-1,-1),(1,-1),(1,1),(-1,1)]]
 vs=[p for i in range(levels+1) for p in ring(i/levels)]
 geo(g,'Roof • midnight ceramic',vs,[(i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j) for i in range(levels) for j in range(4)])
 for edge in range(4):
  aa=ring(0)[edge]; bb=ring(0)[(edge+1)%4]; n=max(4,int(math.dist(aa,bb)/.25))
  for j in range(n+1):
   q=j/n; pts=[]
   for i in range(levels+1):
    rr=ring(i/levels); a=rr[edge]; b=rr[(edge+1)%4]
    pts.append(tuple(a[k]*(1-q)+b[k]*q+( .035 if k==2 else 0) for k in range(3)))
   line(g,'Roof • patina ridges',pts,.037,5)
  line(g,'Wood • honey edges',[aa,bb],.11)
 for j in range(4): line(g,'Roof • patina ridges',[ring(i/levels)[j] for i in range(levels+1)],.11)
 ridge=max(0,rx-ry)*.8
 line(g,'Roof • patina ridges',[(cx-ridge,cy,z+height+.03),(cx+ridge,cy,z+height+.03)],.12)
 for xx in [cx-ridge,cx+ridge]: rod(g,'Roof • patina ridges',(xx,cy,z+height),(xx,cy,z+height+.40),.13,.025)

g='08 Central pavilion • six curved eaves'; cx=0;cy=-2
for rad,z in [(3.6,.48),(3.35,.67),(3.1,.84)]: rod(g,'Stone • blue limestone',(cx,cy,z-.15),(cx,cy,z),rad,n=6)
for i in range(6):
 a=i*math.tau/6+math.pi/6; x=cx+2.28*math.cos(a);y=cy+2.28*math.sin(a)
 rod(g,'Stone • pale worn flags',(x,y,.84),(x,y,1.07),.27,n=6)
 rod(g,'Wood • aged cedar',(x,y,1.07),(x,y,4.35),.15,n=10)
 rod(g,'Wood • honey edges',(x,y,3.85),(cx+1.65*math.cos(a),cy+1.65*math.sin(a),4.4),.10)
 if i in [0,1,2]:
  aa=a+math.tau/6;fence(g,[(x,y),(cx+2.28*math.cos(aa),cy+2.28*math.sin(aa))],.87)
 for s in [-1,1]:
  end=(x+.48*math.cos(a+s*math.pi/2),y+.48*math.sin(a+s*math.pi/2),4.23)
  rod(g,'Wood • honey edges',(x,y,3.65),end,.07)
# A true hexagonal swept roof with radiating tile seams.
def hexpoint(a,t):
 r=3.55*(1-t)+.08; z=4.15+2.1*t*t+.40*(1-t)**8
 return (cx+r*math.cos(a),cy+r*math.sin(a),z)
for side in range(6):
 a=side*math.tau/6+math.pi/6; b=a+math.tau/6
 vs=[]; fs=[]; rows=12; cols=14
 for i in range(rows+1):
  t=i/rows; p=hexpoint(a,t);q=hexpoint(b,t)
  for j in range(cols+1):
   f=j/cols; vs.append(tuple(p[k]*(1-f)+q[k]*f+(.08*math.sin(f*math.pi) if k==2 else 0) for k in range(3)))
 for i in range(rows):
  for j in range(cols): k=i*(cols+1)+j;fs.append((k,k+1,k+cols+2,k+cols+1))
 geo(g,'Roof • midnight ceramic',vs,fs)
 for j in range(cols+1): line(g,'Roof • patina ridges',[tuple(vs[i*(cols+1)+j][k]+(.035 if k==2 else 0) for k in range(3)) for i in range(rows+1)],.035,5)
 line(g,'Wood • honey edges',[hexpoint(a,0),hexpoint(b,0)],.11)
 line(g,'Roof • patina ridges',[hexpoint(a,i/20) for i in range(21)],.10)
 rod(g,'Roof • patina ridges',hexpoint(a,0),(cx+3.85*math.cos(a),cy+3.85*math.sin(a),4.9),.10,.025)
rod(g,'Wood • honey edges',(cx,cy,6.2),(cx,cy,7),.15,.035)
rock(g,(cx,cy,6.55),(.22,.22,.18),'Wood • honey edges')
rod(g,'Stone • pale worn flags',(cx,cy,.85),(cx,cy,1.5),.28,n=8)
rod(g,'Stone • pale worn flags',(cx,cy,1.5),(cx,cy,1.65),.9,n=12)
for a in [0,math.pi*.65,math.pi*1.3]: rod(g,'Wood • aged cedar',(cx+1.2*math.cos(a),cy+1.2*math.sin(a),.85),(cx+1.2*math.cos(a),cy+1.2*math.sin(a),1.3),.28,n=8)
for x in [-2.2,2.2]: lamp(g,x,cy-.9,.85,2.55)

g='09 Martial hall • old timber courtyard';cx=0;cy=12
for i in range(3): box(g,'Stone • blue limestone',(cx,cy,.5+i*.2),(9.6-i*.4,6.6-i*.3,.2))
box(g,'Fabric • worn flax',(cx,cy,2.5),(8.6,5.6,3.2))
for x in [-4.3,-2.2,0,2.2,4.3]:
 for y in [9.15,14.85]: rod(g,'Wood • aged cedar',(x,y,.9),(x,y,4.5),.16,n=8)
for z in [1.1,3.8,4.25]: box(g,'Wood • aged cedar',(0,9.05,z),(9,.18,.18))
for x in [-3.15,3.15]:
 box(g,'Iron • blackened',(x,9.08,2.6),(1.55,.10,1.7))
 for dx in [-.68,-.34,0,.34,.68]: box(g,'Wood • honey edges',(x+dx,8.98,2.6),(.05,.09,1.68))
 for z in [1.9,2.25,2.6,2.95,3.3]: box(g,'Wood • honey edges',(x,8.97,z),(1.48,.09,.045))
for x in [-.52,.52]:
 box(g,'Wood • aged cedar',(x,8.98,2.1),(.99,.16,2.4))
 for dx in [-.33,0,.33]: box(g,'Wood • honey edges',(x+dx,8.87,2.1),(.045,.06,2.25))
 rock(g,(x+(.25 if x<0 else -.25),8.82,2.15),(.06,.04,.08),'Iron • blackened')
hip_roof(g,0,12,4.3,5.3,3.8,2.8)
hip_roof(g,0,8.5,3.65,2.5,1.1,.7,7)
for x in [-4,4]:lamp(g,x,8,.75,2.5)
for i in range(4): box(g,'Stone • pale worn flags',(0,7.75-i*.45,.85-i*.13),(3.8,.49,.2))
for x in [-5.5,5.5]: fence(g,[(x,8),(x,15),(x*.85,16)],.45)

g='10 Boss arena • octagonal dueling court';cx=15;cy=12
for r,z in [(5.7,.55),(5.4,.75),(5.1,.91)]:rod(g,'Stone • blue limestone',(cx,cy,z-.2),(cx,cy,z),r,n=12)
rod(g,'Stone • pale worn flags',(cx,cy,.92),(cx,cy,.96),4.7,n=64)
for r in [4.6,3.65,1.6]:line(g,'Stone • blue limestone',[(cx+r*math.cos(i*math.tau/96),cy+r*math.sin(i*math.tau/96),.975) for i in range(97)],.045,5)
for i in range(12):
 a=i*math.tau/12
 line(g,'Stone • blue limestone',[(cx+r*math.cos(a),cy+r*math.sin(a),.98) for r in [1.6,4.7]],.025,4)
 if i not in [7,8,9]:
  x=cx+5.1*math.cos(a);y=cy+5.1*math.sin(a)
  box(g,'Stone • blue limestone',(x,y,1.35),(.42,.42,.85));rod(g,'Stone • pale worn flags',(x,y,1.76),(x,y,1.87),.33,.1,n=4)
for i in range(4):box(g,'Stone • pale worn flags',(15,6.9-i*.42,.83-i*.12),(3.5,.48,.22))

g='11 Hidden cave • open rock arch';cx=20;cy=-1
# Dark interior behind an open arch, with a short modeled tunnel.
vs=[]
for yy in [cy-1,cy+3.5]:
 for j in range(17):
  a=j*math.pi/16;vs.append((cx+2.1*math.cos(a),yy,.45+3.1*math.sin(a)))
geo(g,'Rock • charcoal cliff',vs,[(j,j+1,j+18,j+17) for j in range(16)])
box(g,'Iron • blackened',(cx,cy+3.5,1.8),(4.3,.1,3.8))
for j in range(11):
 a=j*math.pi/10
 rock(g,(cx+2.9*math.cos(a),cy,.5+3.45*math.sin(a)),(.85,1.45,1.02),moss=True)
for i in range(22):
 x=random.uniform(17,23);y=random.uniform(1,4)
 rock(g,(x,y,1.4),(random.uniform(.9,1.8),random.uniform(.7,1.7),random.uniform(1.5,2.8)),moss=True)
for x in [17.7,22.3]:lamp(g,x,-2.3,.45,2.2)
for i in range(7):box(g,'Stone • pale worn flags',(20+random.uniform(-.3,.3),-3+i*.7,.48),(1.8,.6,.13),random.uniform(-.1,.1))
flush()
print('Landmarks ready',len(scene.objects))
