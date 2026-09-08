def leaf(g,m,a,b,width):
    a,b=Vector(a),Vector(b);d=b-a;u=d.cross(Vector((0,0,1)))
    if u.length<.001:u=Vector((1,0,0))
    u.normalize();mid=a+d*.44
    geo(g,m,[a,mid+u*width,b,mid-u*width,mid+Vector((0,0,.012))],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)])
def bench(g,x,y,z=.15,length=1.15,a=0):
    def xy(xx,yy):return(x+xx*math.cos(a)-yy*math.sin(a),y+xx*math.sin(a)+yy*math.cos(a))
    for yy in [-.10,.10]:
        xx1,yy1=xy(0,yy);box(g,'wood_edge',(xx1,yy1,z+.42),(length,.19,.065),a)
    for xx in [-length*.35,length*.35]:
        for yy in [-.14,.14]:
            xx1,yy1=xy(xx,yy);box(g,'wood',(xx1,yy1,z+.22),(.06,.06,.42),a)
    aa,bb=xy(-length*.38,0),xy(length*.38,0)
    rod(g,'wood',(aa[0],aa[1],z+.2),(bb[0],bb[1],z+.2),.028,n=6)
def table(g,x,y,z=.15,sx=.85,sy=.6):
    for i in range(5):box(g,'wood_edge',(x+(i-2)*sx/5,y,z+.71),(sx*.195,sy,.065))
    for xx in [-sx*.37,sx*.37]:
        for yy in [-sy*.36,sy*.36]:box(g,'wood',(x+xx,y+yy,z+.36),(.065,.065,.71))
    for yy in [-sy*.35,sy*.35]:box(g,'wood',(x,y+yy,z+.60),(sx,.07,.12))
def pot(g,x,y,z,scale=1):
    rows=[(0,.085),(.06,.14),(.20,.17),(.30,.12),(.33,.1)]
    vs=[];n=16
    for zz,r in rows:
        for i in range(n):a=i*math.tau/n;vs.append((x+r*scale*math.cos(a),y+r*scale*math.sin(a),z+zz*scale))
    geo(g,'pottery',vs,[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(rows)-1) for i in range(n)])
    ring(g,'pottery',(x,y,z+.33*scale),.103*scale,.015*scale,n=20)
    rod(g,'ink',(x,y,z+.292*scale),(x,y,z+.293*scale),.09*scale,n=16)

# Ancient leaning pine with tapered limbs, exposed roots, and needle fans.
g='10 Rest • old pine trunk';base=(-.30,.90,.16)
trunk=[base,(-.55,.95,1.1),(-.20,1.07,2.15),(.43,1.18,3.10),(.58,1.30,4.08),(.22,1.48,5.0)]
tube(g,'bark',trunk,[.36,.34,.30,.25,.17,.07],12)
for i in range(8):
    a=i*math.tau/8
    pts=[base,(-.3+.6*math.cos(a),.9+.55*math.sin(a),.24),(-.3+1.0*math.cos(a),.9+.9*math.sin(a),.13)]
    tube(g,'bark',pts,[.17,.08,.015],8)
canopies=[]
for a,z,length in [(0.1,3.25,2.25),(1.2,3.8,2.1),(2.3,3.5,2.25),(3.4,3.2,1.85),(4.35,4.3,1.9),(5.25,4.2,2.0),(.4,4.9,1.3),(2.6,4.95,1.4)]:
    start=Vector((.35,1.2,z));end=start+Vector((math.cos(a)*length,math.sin(a)*length,.8))
    mid=start+(end-start)*.55+Vector((0,0,-.18))
    tube(g,'bark',[start,mid,end],[.135,.075,.018],9)
    for k in [-1,0,1]:
        aa=a+k*.55;tip=end+Vector((math.cos(aa)*.7,math.sin(aa)*.7,.18+random.random()*.18))
        tube(g,'bark',[mid,end,tip],[.06,.024,.004],7);canopies.append(tuple(tip))
canopies.extend([(.2,1.5,5.5),(-.7,1.9,5.25),(.7,.9,5.35)])
for center in canopies:
    for j in range(34):
        a=random.random()*math.tau;r=random.random()**.5*.75
        p=Vector((center[0]+r*math.cos(a),center[1]+r*math.sin(a),center[2]+random.uniform(-.13,.23)))
        tip=p+Vector((math.cos(a)*.25,math.sin(a)*.25,.015))
        rod('11 Rest • pine canopy twigs','bark',p,tip,.008,.003,n=5)
        for k in range(7):
            q=p+(tip-p)*k/7;an=a+random.choice([-1,1])*random.uniform(.6,1.5)
            ll=random.uniform(.12,.23)
            leaf('11 Rest • pine needle fans','leaf'+str(random.randrange(4)),q,q+Vector((math.cos(an)*ll,math.sin(an)*ll,random.uniform(-.01,.08))),.012)
# Thin irregular bark ribbons add close-range trunk relief without dense subdivision.
for i in range(14):
    a=i*math.tau/14
    pts=[(x+r*math.cos(a+.12*j),y+r*math.sin(a+.12*j),z) for j,((x,y,z),r) in enumerate(zip(trunk[:-1],[.357,.34,.3,.25,.17]))]
    line(g,'wood',pts,.012,5)

g='12 Rest • tea tables and stools'
table(g,-.62,-.78,sx=.82,sy=.53)
bench(g,-.62,-1.35,length=1.08)
bench(g,.93,.17,length=1.0,a=math.pi/2)
table(g,-1.15,.25,sx=.47,sy=.46)
pot(g,-.72,-.78,.90,.47)
for x,y in [(-.35,-.69),(-.95,-.75),(-1.15,.2)]:pot(g,x,y,.90,.19)
for x,y in [(1.15,.9),(-1.03,1.1)]:
    rock('12 Rest • mossy seat stones',(x,y,.22),(.27,.24,.21),'stone',True)

# Southwest medicinal plant: one clear red-berry/white-flower cluster, not generic scenery.
g='13 Herb • medicinal plant';cx=-5.55;cy=-2.10
for i in range(13):
    a=i*math.tau/13;h=random.uniform(.36,.64);p=(cx,cy,.14)
    end=(cx+.34*math.cos(a),cy+.34*math.sin(a),h)
    rod(g,'herb_leaf',p,end,.01,.005,5)
    leaf(g,'herb_leaf',(cx,cy,.22),(end[0]+.10*math.cos(a),end[1]+.10*math.sin(a),h*.9),.11)
    if i%2==0:
        rock(g,end,(.048,.048,.05),'berry',False)
    else:
        for j in range(5):
            an=j*math.tau/5
            rock(g,(end[0]+.048*math.cos(an),end[1]+.048*math.sin(an),end[2]),(.04,.025,.016),'flower',False)
for x,y in [(-6,-2.4),(-5.1,-2.6),(-5.9,-1.5)]:rock('13 Herb • moss rock garden',(x,y,.15),(.35,.28,.28),moss=True)
lantern('13 Herb • stone wayside lamp',-6.33,-2.25,1.05,.63)

# Southeast camp: cloth tension, seams, actual poles and pegged ropes.
g='14 Camp • patched canvas shelter';cx=6.2;cy=.50;z=.15
rows=14;cols=13
vs=[]
for j in range(rows+1):
    yy=cy-1.25+j/rows*2.5
    for i in range(cols+1):
        xx=cx-1.3+i/cols*2.6
        h=2.18-abs((xx-cx)/1.3)*1.52-.09*math.sin(j/rows*math.pi)+.025*math.sin(i*2+j*.6)
        vs.append((xx,yy,z+h))
geo(g,'cloth',vs,[(j*(cols+1)+i,j*(cols+1)+i+1,(j+1)*(cols+1)+i+1,(j+1)*(cols+1)+i) for j in range(rows) for i in range(cols)])
# Back panel closed, front open with hanging side flaps.
geo(g,'cloth',[(cx-1.3,cy+1.25,z),(cx-1.3,cy+1.25,z+.66),(cx,cy+1.25,z+2.18),(cx+1.3,cy+1.25,z+.66),(cx+1.3,cy+1.25,z)],[(0,1,2,3,4)])
for side in [-1,1]:
    geo(g,'cloth',[(cx+side*1.3,cy-1.25,z+.66),(cx,cy-1.25,z+2.18),(cx+side*.62,cy-1.32,z+.18),(cx+side*1.3,cy-1.25,z+.12)],[(0,1,2,3)])
    for yy in [cy-1.25,cy+1.25]:
        rod(g,'wood',(cx,yy,z),(cx,yy,z+2.36),.045,n=8)
        line(g,'rope',[(cx+side*.75,yy,z+1.35),(cx+side*1.8,yy-.15,z+.13)],.011,5)
        rod(g,'wood',(cx+side*1.8,yy-.15,z),(cx+side*1.83,yy-.15,z+.27),.025,n=5)
line(g,'rope',[(cx,cy-1.35,z+2.19),(cx,cy+1.35,z+2.19)],.013,6)
for yy in [cy-.7,cy+.3,cy+.95]:
    line(g,'rope',[(cx-1.3,yy,z+.66),(cx,yy,z+2.19),(cx+1.3,yy,z+.66)],.008,5)
for x,y in [(5.05,.85),(5.5,1.40),(7.53,.82)]:crate('15 Camp • stacked supplies',x,y,.16,random.uniform(.40,.55))
crate('15 Camp • stacked supplies',5.10,.85,.67,.37)
for x,y in [(7.1,1.75),(7.7,1.6),(5.3,2)]:barrel('15 Camp • stacked supplies',x,y,.15,.75)
bench('15 Camp • workbench',5.65,-.67,length=1.25)
pot('15 Camp • cooking pots',6.95,-1,.15,.9)
for i in range(4):
    x=7.8+i*.13
    rod('15 Camp • weapon rack','wood',(x,-.2,.16),(x-.3,.03,1.9),.025,n=6)
    # Simple sheathed practice weapons are environment props only.
    rod('15 Camp • weapon rack','iron',(x-.3,.03,1.9),(x-.34,.06,2.18),.024,.006,5)
fence('15 Camp • weapon rack',[(7.4,-.1),(8.25,-.1)])

def fire(g,x,y,z,scale=1):
    for i in range(12):
        a=i*math.tau/12
        rock(g,(x+.39*scale*math.cos(a),y+.39*scale*math.sin(a),z+.09*scale),(.14*scale,.12*scale,.09*scale),'stone',False)
    for a in [0,1.1,2.2]:
        rod(g,'bark',(x-.35*scale*math.cos(a),y-.35*scale*math.sin(a),z+.12*scale),(x+.35*scale*math.cos(a),y+.35*scale*math.sin(a),z+.12*scale),.065*scale,n=8)
    for j in range(6):
        xx=x+random.uniform(-.18,.18)*scale;yy=y+random.uniform(-.18,.18)*scale
        h=random.uniform(.34,.68)*scale
        tube(g,'flame',[(xx,yy,z+.13),(xx+.035,yy,z+h*.55),(xx-.10*scale,yy+.04,z+h)],[.08*scale,.048*scale,0],7)
    rod(g,'flame_core',(x,y,z+.15),(x+.04,y,z+.40*scale),.09*scale,0,7)
    pointlight(g,'Fire • warm pool',(x,y,z+.40),(1,.33,.07),70*scale,.45)
fire('16 Camp • cooking fire',5.85,-2.50,.15,.9)
for x in [5.48,6.20]:rod('16 Camp • cooking fire','iron',(x,-2.51,.15),(5.84,-2.51,1.05),.021,n=6)
rod('16 Camp • cooking fire','iron',(5.84,-2.51,1.03),(5.84,-2.51,.71),.015,n=6)
pot('16 Camp • cooking fire',5.84,-2.51,.48,.65)

# Two-wheel cart at the front edge of camp, individual spokes and stave bed.
g='17 Camp • wooden handcart';cx=7.1;cy=-3.85;z=.15
for i in range(7):box(g,'wood_edge',(cx+(i-3)*.12,cy,z+.5),(.115,1.28,.055))
for side in [-1,1]:
    xx=cx+side*.6
    ring(g,'wood',(xx,cy,z+.44),.43,.045,axis='X',n=28)
    ring(g,'iron',(xx,cy,z+.44),.46,.016,axis='X',n=28)
    rod(g,'wood_edge',(xx-.085,cy,z+.44),(xx+.085,cy,z+.44),.065,n=10)
    for i in range(12):
        a=i*math.tau/12
        rod(g,'wood_edge',(xx,cy,z+.44),(xx,cy+.39*math.cos(a),z+.44+.39*math.sin(a)),.018,n=5)
    rod(g,'wood',(cx+side*.33,cy+.50,z+.39),(cx+side*.38,cy-1.65,z+.78),.034,n=7)
    for zz in [.72,.92]:box(g,'wood',(cx+side*.46,cy,z+zz),(.055,1.3,.09))
    for yy in [cy-.57,cy,cy+.57]:box(g,'wood_edge',(cx+side*.46,yy,z+.75),(.05,.05,.53))
crate(g,cx,cy+.1,z+.53,.48)

# Cloth signs, calligraphy uses the project's packaged Chinese font and is packed.
fontpath='/Users/gongyuyang/Documents/37minigame/Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.ttf'
font=bpy.data.fonts.load(fontpath)
def lettering(g,body,p,size=.3):
    cu=bpy.data.curves.new('Physical sign lettering','FONT');cu.body=body;cu.font=font;cu.size=size;cu.align_x='CENTER';cu.extrude=.001
    ob=bpy.data.objects.new('Sign • '+body,cu);coll(g).objects.link(ob);ob.location=p;ob.rotation_euler=(math.pi/2,0,0);cu.materials.append(M['ink'])
def banner(g,x,y,z,body,m='cloth',width=.62,height=1.2):
    rod(g,'wood',(x,y,.15),(x,y,z+.27),.043,n=8)
    rod(g,'wood_edge',(x-width*.65,y,z+.15),(x+width*.65,y,z+.15),.035,n=8)
    vs=[];rows=16;cols=8
    for j in range(rows+1):
        for i in range(cols+1):
            xx=x-width/2+i/cols*width;zz=z-j/rows*height
            if j==rows:zz-=.045*(i%2)
            vs.append((xx,y+.045*math.sin(i*.9+j*.6),zz))
    geo(g,m,vs,[(j*(cols+1)+i,j*(cols+1)+i+1,(j+1)*(cols+1)+i+1,(j+1)*(cols+1)+i) for j in range(rows) for i in range(cols)])
    lettering(g,body,(x,y-.065,z-height*.66),width*.7)
banner('18 Signs • tea rest stop',1.00,1.14,2.76,'茶',width=.55,height=1.13)
banner('18 Signs • cave pennant',7.25,5.55,3.15,'山\n洞',m='red_cloth',width=.60,height=1.3)
banner('18 Signs • camp pennant',7.8,.3,2.8,'武',m='red_cloth',width=.55,height=1.1)
g='18 Signs • entry wooden waypost';x=-1.45;y=-4.5
rod(g,'wood',(x,y,.12),(x,y,1.61),.064,n=8)
for i,body in enumerate(['山道','驿亭','山洞']):
    zz=1.45-i*.25;box(g,'wood_edge',(x,y-.045,zz),(1.0,.10,.20))
    lettering(g,body,(x,y-.104,zz-.075),.17)
for g,pts in [('19 Boundaries • roadside rails',[(-3.0,-4.9),(-4.3,-4.5),(-5.6,-4.2)]),
              ('19 Boundaries • roadside rails',[(2.3,-4.8),(3.6,-4.7),(4.35,-4.3)]),
              ('19 Boundaries • cave rails',[(3.4,2.5),(4.35,3.6)]),
              ('19 Boundaries • herb rails',[(-7.4,-3.5),(-7.8,-1.5),(-7.4,.2)])]:fence(g,pts)
for x,y in [(-2.65,-3.3),(2.6,-3.3),(-3.1,2.7),(3.3,2.75),(-6.4,3.1),(4.2,-3.8)]:lantern('19 Boundaries • pathway lanterns',x,y,1.45,.72)
flush()
print('DRESSING_READY',len(scene.objects),flush=True)
