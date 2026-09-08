def banner(g,x,y,z=.45):
 rod(g,'Wood • aged cedar',(x,y,z),(x,y,z+4.7),.075)
 rod(g,'Wood • honey edges',(x-.9,y,z+4.35),(x+.9,y,z+4.35),.065)
 vs=[];fs=[]
 for j in range(13):
  for i in range(9):
   xx=x+(i/8-.5)*1.45; zz=z+4.3-j/12*2.55
   if j==12:zz-=.18*(i%2)
   vs.append((xx,y+.12*math.sin(i*.9+j*.42),zz))
 for j in range(12):
  for i in range(8):k=j*9+i;fs.append((k,k+1,k+10,k+9))
 geo(g,'Fabric • oxblood',vs,fs)
 fontpath='/Users/gongyuyang/Documents/37minigame/Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.ttf'
 if os.path.exists(fontpath):
  cu=bpy.data.curves.new('Wuxia banner glyph','FONT');cu.body='武';cu.font=bpy.data.fonts.load(fontpath);cu.size=1.12;cu.align_x='CENTER';cu.extrude=.004
  ob=bpy.data.objects.new('武 • banner calligraphy',cu);coll(g).objects.link(ob);ob.location=(x,y-.18,z+2.73);ob.rotation_euler=(math.pi/2,0,0);cu.materials.append(M['Iron • blackened'])
for x,y in [(-17,12),(5.2,9),(11,15),(19,15),(-15,-7)]:banner('12 Banners • martial red',x,y)

g='13 Camp • fire tents and supplies';cx=-15;cy=12
# Two canvas tents, poles, seams and guy ropes.
for tx,ty in [(-17,13),(-12.6,15.2)]:
 z=ground(tx,ty)
 geo(g,'Fabric • worn flax',[(tx-1.6,ty-1.5,z),(tx,ty-1.5,z+2.65),(tx+1.6,ty-1.5,z),(tx-1.6,ty+1.5,z),(tx,ty+1.5,z+2.65),(tx+1.6,ty+1.5,z)],[(0,1,4,3),(1,2,5,4),(3,4,5)])
 for yy in [ty-1.55,ty+1.55]:rod(g,'Wood • aged cedar',(tx,yy,z),(tx,yy,z+2.9),.07)
 for side in [-1,1]:
  for yy in [ty-1.5,ty+1.5]:
   rod(g,'Fabric • worn flax',(tx+side*.7,yy,z+1.6),(tx+side*2.25,yy-.35,z+.05),.02,n=4)
   rod(g,'Wood • aged cedar',(tx+side*2.25,yy-.35,z),(tx+side*2.25,yy-.35,z+.35),.045,n=5)
fx=-14.7;fy=9.6;zz=ground(fx,fy)
for i in range(11):
 a=i*math.tau/11;rock(g,(fx+.8*math.cos(a),fy+.8*math.sin(a),zz+.12),(.23,.2,.16),'Stone • blue limestone')
for a in [0,1,2]:rod(g,'Wood • aged cedar',(fx-.65*math.cos(a),fy-.65*math.sin(a),zz+.25),(fx+.65*math.cos(a),fy+.65*math.sin(a),zz+.25),.12,n=7)
for i in range(7):
 x=fx+random.uniform(-.35,.35);y=fy+random.uniform(-.35,.35)
 rod(g,'Fire • gold',(x,y,zz+.22),(x+random.uniform(-.18,.18),y,zz+random.uniform(.7,1.5)),.23,0,n=5)
data=bpy.data.lights.new('Campfire pool','POINT');data.energy=500;data.color=(1,.20,.025);data.shadow_soft_size=1.4
ob=bpy.data.objects.new('Campfire pool',data);coll(g).objects.link(ob);ob.location=(fx,fy,zz+1.2)
def crate(g,x,y,z,size=.9):
 box(g,'Wood • honey edges',(x,y,z+size/2),(size,size,size))
 for dx in [-.43,.43]:
  for dy in [-.43,.43]:box(g,'Wood • aged cedar',(x+dx*size,y+dy*size,z+size/2),(.12,.12,size+.05))
 for zz in [z+.08,z+size-.08]:
  for dy in [-.51,.51]:box(g,'Wood • aged cedar',(x,y+dy*size,zz),(size,.08,.1))
 rod(g,'Wood • aged cedar',(x-size*.4,y-size*.51,z+.1),(x+size*.4,y-size*.51,z+size-.1),.055,n=4)
def barrel(g,x,y,z):
 vs=[]
 for zz,rr in [(0,.36),(.15,.43),(.65,.47),(1.05,.4),(1.16,.35)]:
  for i in range(12):a=i*math.tau/12;vs.append((x+rr*math.cos(a),y+rr*math.sin(a),z+zz))
 geo(g,'Wood • honey edges',vs,[(j*12+i,j*12+(i+1)%12,(j+1)*12+(i+1)%12,(j+1)*12+i) for j in range(4) for i in range(12)]+[tuple(range(48,60))])
 for zz,rr in [(.18,.435),(.87,.43)]:line(g,'Iron • blackened',[(x+rr*math.cos(i*math.tau/24),y+rr*math.sin(i*math.tau/24),z+zz) for i in range(25)],.038,5)
for x,y in [(-19,10),(-18.8,9),(-11.5,12),(5.1,12),(6.1,12),(5.3,13.2)]:crate('14 Props • crates barrels cart',x,y,ground(x,y),random.uniform(.7,1))
for x,y in [(-18,10),(-11.5,13.3),(5.5,14.5),(6.5,14.3)]:barrel('14 Props • crates barrels cart',x,y,ground(x,y))
# Abandoned two-wheel handcart east of the main path.
g='14 Props • crates barrels cart';cx=9;cy=-9;z=ground(cx,cy)
for i in range(7):box(g,'Wood • honey edges',(cx-.8+i*.26,cy,z+.85),(.23,2,.12))
for side in [-1,1]:
 x=cx+side*1.15
 line(g,'Wood • aged cedar',[(x,cy+math.cos(i*math.tau/32)*.8,z+.8+math.sin(i*math.tau/32)*.8) for i in range(33)],.085)
 for i in range(10):a=i*math.tau/10;rod(g,'Wood • honey edges',(x,cy,z+.8),(x,cy+math.cos(a)*.75,z+.8+math.sin(a)*.75),.04,n=5)
 rod(g,'Wood • aged cedar',(cx+side*.65,cy+.7,z+.65),(cx+side*.65,cy-3.2,z+1.05),.065)
 fence(g,[(cx+side*.85,cy-1),(cx+side*.85,cy+1)],z+.85)
barrel(g,cx,cy+.3,z+.9)

# Entrance ceremonial gate.
g='15 Entrance • timber paifang';cx=-17;cy=-11.5;z=ground(cx,cy)
for x in [cx-1.7,cx+1.7]:
 box(g,'Stone • blue limestone',(x,cy,z+.25),(.7,.7,.5));rod(g,'Wood • aged cedar',(x,cy,z+.25),(x,cy,z+4.25),.19,n=10)
box(g,'Wood • aged cedar',(cx,cy,z+3.5),(4.4,.35,.38))
box(g,'Wood • honey edges',(cx,cy-.23,z+3.5),(1.8,.10,.52))
hip_roof(g,cx,cy,z+3.85,2.6,.8,.85,7)
for x in [cx-2.1,cx+2.1]:lamp(g,x,cy-.4,z,2.4)
for pts in [[(-19,-14),(-20,-10),(-17,-6)],[(-10,-9),(-6,-10),(-2,-10)],[(4,0),(6,1),(7,4)],[(-18,7),(-19,3),(-18,-1)],[(8,15),(8,18),(18,18)]]:fence('16 Wayside • split rail fences',pts)
for x,y in [(-19,-15),(-15,-9),(-12,-4),(-15,2),(-16,6),(-3,-8),(4,-5),(5,2),(2,6),(8,6),(13,6),(17,-7),(11,-7)]:lamp('17 Wayside • amber lanterns',x,y,ground(x,y),random.uniform(2.1,2.8))
flush()

def leaf(g,base,tip,width,mat):
 a=Vector(base);b=Vector(tip);d=b-a;u=d.cross(Vector((0,0,1)))
 if u.length<.001:u=Vector((1,0,0))
 u.normalize();mid=a+d*.43
 geo(g,mat,[tuple(a),tuple(mid+u*width),tuple(b),tuple(mid-u*width),tuple(mid+Vector((0,0,.035)))],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)])
def bamboo(x,y,h,g):
 z=ground(x,y);leanx=random.uniform(-.7,.7);leany=random.uniform(-.6,.6);radius=random.uniform(.045,.095)
 segments=max(5,int(h/.7))
 for i in range(segments):
  t=i/segments;t1=(i+1)/segments;a=(x+leanx*t,y+leany*t,z+h*t);b=(x+leanx*t1,y+leany*t1,z+h*t1)
  rod(g,'Bamboo • forest green',a,b,radius*(1-t*.6),radius*(1-t1*.6),7)
  rod(g,'Bamboo • golden nodes',(a[0],a[1],a[2]-.025),(a[0],a[1],a[2]+.026),radius*(1-t*.6)*1.2,n=7)
 for j in range(random.randint(5,9)):
  t=random.uniform(.48,.98);a=Vector((x+leanx*t,y+leany*t,z+h*t));ang=random.uniform(0,math.tau);length=random.uniform(.7,1.6)
  b=a+Vector((math.cos(ang)*length,math.sin(ang)*length,random.uniform(.0,.5)))
  rod(g,'Bamboo • forest green',a,b,.018,.004,5)
  for k in range(5):
   q=.2+k*.17;center=a+(b-a)*q
   for side in [-1,1]:
    an=ang+side*random.uniform(.4,1.1);l=random.uniform(.35,.75)
    end=center+Vector((math.cos(an)*l,math.sin(an)*l,random.uniform(-.25,.2)))
    leaf(g,center,end,random.uniform(.055,.095),'Foliage '+str(random.randrange(4)))
def blocked(x,y):
 if abs(x-riverx(y))<3.2:return True
 if any(distance_seg(x,y,a,b)<w*.65+.55 for a,b,w in segments()):return True
 for cx,cy,rx,ry in [(0,-2,5.3,5.3),(0,-7,4.2,4.5),(0,12,6.7,5.7),(15,12,7.3,7.3),(20,-1,4.6,5.5),(20,-6,4.8,6),(-15,12,5,5),(-17,-11.5,3,2)]:
  if ((x-cx)/rx)**2+((y-cy)/ry)**2<1:return True
 return False
count=0
for i in range(1250):
 x=random.uniform(-24,24);y=random.uniform(-21,21)
 if not inside(x,y) or blocked(x,y):continue
 edge=max(abs(x)/25,abs(y)/22)
 density=.83 if edge>.72 else .22
 if random.random()>density:continue
 # Foreground bamboo is shorter to preserve the route silhouette.
 h=random.uniform(4.8,8.8)*( .66 if y<-11 else 1)
 g='18 Bamboo groves • '+('north' if y>10 else 'west' if x<-10 else 'east' if x>9 else 'south')
 for k in range(random.randint(2,4)):bamboo(x+random.uniform(-.35,.35),y+random.uniform(-.35,.35),h*random.uniform(.7,1.1),g);count+=1
for i in range(800):
 x=random.uniform(-24,24);y=random.uniform(-21,21)
 if not inside(x,y) or abs(x-riverx(y))<2.6:continue
 if any(distance_seg(x,y,a,b)<w*.46 for a,b,w in segments()):continue
 if any(((x-cx)/rx)**2+((y-cy)/ry)**2<1 for cx,cy,rx,ry in [(0,-2,3.7,3.7),(0,12,5.5,4.5),(15,12,5.9,5.9),(20,-1,2.5,3),(-15,12,4,4)]):continue
 z=ground(x,y)
 for j in range(random.randint(4,8)):
  a=random.uniform(0,math.tau);l=random.uniform(.25,.7)
  leaf('19 Undergrowth • fern grass',(x,y,z+.02),(x+math.cos(a)*l,y+math.sin(a)*l,z+random.uniform(.15,.55)),.065,'Foliage '+str(random.randrange(4)))
 if i%5==0:rock('20 Forest floor • scattered moss stones',(x,y,z+.04),(random.uniform(.2,.7),random.uniform(.2,.65),random.uniform(.15,.45)),moss=True)
flush()
scene['bamboo_culm_count']=count
print('Dressing ready',len(scene.objects),'bamboo culms',count)
