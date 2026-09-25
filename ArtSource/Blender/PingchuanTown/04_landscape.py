# Vegetation and cliff dressing. Reject objects from main routes, rivers and landmark footprints.
def blocked(x,y,margin=0):
    if roaddist(x,y)<margin+.8 or waterdist(x,y)<margin+.5:return True
    return any(((x-cx)/(rx+margin))**2+((y-cy)/(ry+margin))**2<1 for cx,cy,rx,ry in occupied)
def sector(x,y,kind):
    return f'70 Nature / {kind} / {int((x+130)//45):02d}-{int((y+120)//45):02d}'

def cliff(g,x,y,z,rx,ry,h):
    n=9;levels=7;vs=[];rands=[random.uniform(.82,1.17) for _ in range(n)]
    for j in range(levels):
        t=j/(levels-1)
        rr=(1-.48*t)*(.91+.11*math.sin(j*1.7))
        for i in range(n):
            a=i*math.tau/n+.09*math.sin(j);k=rands[i]
            vs.append((x+math.cos(a)*rx*k*rr+math.sin(t*2)*rx*.18,y+math.sin(a)*ry*k*rr,z+h*t+(random.uniform(-.6,.6) if j else 0)))
    fs=[tuple(reversed(range(n)))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(levels-1) for i in range(n)]+[tuple(range((levels-1)*n,levels*n))]
    geo(g,'cliff',vs,fs)
    # Distinct strata and moss shelves, not a field of rounded boulders.
    for j in [1,3,5]:
        for i in range(n):
            if random.random()<.5:
                a=vs[j*n+i];b=vs[j*n+(i+1)%n]
                line(g,'stone',[(a[0]*.999,a[1]*.999,a[2]+.04),(b[0]*.999,b[1]*.999,b[2]+.04)],.12,5)
    top=vs[(levels-1)*n:];geo(g,'moss',[(xx,yy,zz+.025) for xx,yy,zz in top],[tuple(range(n))])

# Layered ring rises towards the north, with real route openings rather than a closed wall.
cliff_count=0
for layer in range(3):
    for i in range(72):
        a=i*math.tau/72+random.uniform(-.022,.022)
        rx=108+layer*10;ry=96+layer*11
        x=rx*math.copysign(abs(math.cos(a))**.63,math.cos(a));y=8+ry*math.copysign(abs(math.sin(a))**.72,math.sin(a))
        if roaddist(x,y)<8:continue
        if any(math.hypot(x-cx,y-cy)<10 for cx,cy in [(-83,58),(-67,75),(0,77),(94,22),(-91,35)]):continue
        h=random.uniform(9,18)+(7 if y>50 else 0)+layer*4
        if y<-60:h*=.40
        cliff(sector(x,y,'mountain cliffs'),x,y,-1,random.uniform(5,9),random.uniform(5,8),h);cliff_count+=1
# Interior outcrops create exploration pockets but keep the plain open.
for x,y,rx,ry,h in [(-67,10,8,7,9),(-63,53,6,8,12),(-14,70,6,8,9),(18,77,5,7,12),(81,47,10,9,13),(93,-20,7,9,10),(-93,-48,6,8,7),(9,32,5,7,8),(-59,-46,5,5,6),(93,-90,6,5,7)]:
    if not blocked(x,y,1):cliff(sector(x,y,'inner outcrops'),x,y,0,rx,ry,h)
    reserve(x,y,rx,ry)
# Atmospheric jagged far mountain silhouettes, behind the playable ground and cliffs.
for layer in range(3):
    ma=f'distant_{layer}';mat(ma,[(.22,.31,.36),(.29,.39,.45),(.37,.47,.53)][layer],rough=1,scale=.12)
    for i in range(24):
        x=-190+i*17+random.uniform(-6,6);y=140+layer*23+random.uniform(-6,6)
        h=random.uniform(30,62)+layer*9
        vs=[(x-18,y-15,0),(x+17,y-12,0),(x+16,y+19,0),(x-15,y+21,0),(x+random.uniform(-7,7),y, h),(x-9,y-7,h*.55),(x+9,y-3,h*.64)]
        geo('79 Backdrop / distant mountain range',ma,vs,[(0,1,6,4,5),(1,2,4,6),(2,3,4),(3,0,5,4)])

def pine(g,x,y,s=1,detail=False):
    z=height(x,y);lean=random.uniform(-.8,.8)*s
    trunk=[(x,y,z),(x-.15*s,y,z+2*s),(x+lean,y+.15*s,z+4*s),(x+lean*.8,y+.3*s,z+6*s),(x+lean,y+.3*s,z+7.5*s)]
    tube(g,'bark',trunk,[.34*s,.29*s,.24*s,.14*s,.025*s],9)
    for i in range(5):
        a=i*math.tau/5
        tube(g,'bark',[(x,y,z+.5*s),(x+.65*s*math.cos(a),y+.65*s*math.sin(a),z+.15),(x+1.1*s*math.cos(a),y+1.1*s*math.sin(a),z)],[.16*s,.08*s,.01*s],6)
    for j in range(7):
        a=j*2.399+random.random()*.5;hh=(3.4+j*.55)*s;length=(2.8-j*.22)*s
        p=Vector((x+lean*.6,y, z+hh));q=p+Vector((math.cos(a)*length,math.sin(a)*length,.5*s))
        tube(g,'bark',[p,p+(q-p)*.5+Vector((0,0,-.25*s)),q],[.12*s,.08*s,.022*s],7)
        for k in range(3):
            an=a+(k-1)*.8;center=q+Vector((math.cos(an)*.65*s,math.sin(an)*.65*s,.2*s))
            # Irregular flattened branch pads, with fine needle fans to break the silhouette.
            rock(g,tuple(center),(.96*s,.85*s,.30*s),'leaf'+str((j+k)%4),False)
            for t in range(18 if detail else 7):
                an2=random.random()*math.tau;rr=random.random()*.9*s
                p2=center+Vector((rr*math.cos(an2),rr*math.sin(an2),random.uniform(-.08,.2)*s))
                for side in [-1,1]:
                    tip=p2+Vector((math.cos(an2+side*.55)*.36*s,math.sin(an2+side*.55)*.36*s,.09*s))
                    leaf(g,'leaf'+str(random.randrange(4)),p2,tip,.03*s)
    rock(g,tuple(Vector(trunk[-1])+Vector((0,0,.1*s))),(.75*s,.72*s,.35*s),'leaf1',False)

def broadleaf(g,x,y,s=1):
    z=height(x,y);tube(g,'bark',[(x,y,z),(x+.2*s,y,z+2*s),(x-.1*s,y,z+4.8*s)],[.27*s,.19*s,.04*s],8)
    for j in range(8):
        a=j*2.39;rr=random.uniform(.8,1.9)*s;hh=random.uniform(3.5,5.8)*s
        q=(x+rr*math.cos(a),y+rr*math.sin(a),z+hh)
        rod(g,'bark',(x,y,z+2.7*s),q,.10*s,.025*s,7)
        rock(g,q,(1.4*s,1.15*s,.94*s),'leaf'+str(random.randrange(3)),False)
        for k in range(22):
            aa=random.random()*math.tau;pp=Vector(q)+Vector((math.cos(aa)*1.2*s,math.sin(aa)*1.0*s,random.uniform(-.5,.6)*s))
            leaf(g,'leaf'+str(random.randrange(3)),pp,pp+Vector((math.cos(aa)*.38*s,math.sin(aa)*.38*s,.08*s)),.12*s)

# Designed foreground accents and town garden trees.
tree_count=0
for x,y,s in [(-70,-87,1.4),(-50,-83,1.2),(-71,-66,1.4),(-55,-12,1),(-54,21,1.2),(-2,-6,1.0),(4,28,1.15),(-35,36,1.1),(-83,47,1.25),(7,73,1.2),(84,-56,1.2),(94,-76,1.4),(61,21,1.25),(17,-42,1.25)]:
    if waterdist(x,y)>1.5:pine(sector(x,y,'hero pines'),x,y,s,True);tree_count+=1
for i in range(800):
    x=random.uniform(-114,115);y=random.uniform(-108,110)
    if blocked(x,y,2):continue
    # Cluster tree distribution, wide playable ground between local thickets.
    dense=(abs(x)>82 or y>64 or y<-85)
    if not dense and (math.sin(x*.18)+math.cos(y*.14)<.65 or random.random()<.55):continue
    s=random.uniform(.8,1.6)*( .8 if y<-60 else 1)
    if random.random()<.78:pine(sector(x,y,'pine groves'),x,y,s)
    else:broadleaf(sector(x,y,'broadleaf groves'),x,y,s)
    tree_count+=1
# Bamboo groves at entry and rocky foothills with individual culms, nodes and leaves.
bamboo_count=0
for cx,cy in [(-72,-90),(-50,-95),(-93,-17),(-57,58),(5,64),(88,39),(97,-42),(73,-95)]:
    for j in range(20):
        x=cx+random.uniform(-4,4);y=cy+random.uniform(-4,4)
        if blocked(x,y,.3):continue
        place(bamboo,sector(x,y,'bamboo'),x,y,0,1,0,0,random.uniform(5.8,10.2));bamboo_count+=1
# River edge boulders and glinting broken ripples.
for pts,w in rivers:
    for i,(x,y) in enumerate(pts):
        a=pts[max(0,i-1)];b=pts[min(len(pts)-1,i+1)];dx=b[0]-a[0];dy=b[1]-a[1];L=math.hypot(dx,dy)
        for side in [-1,1]:
            for j in range(3):
                xx=x-side*dy/L*(w/2+.6)+random.uniform(-1.1,1.1);yy=y+side*dx/L*(w/2+.6)+random.uniform(-1.1,1.1)
                if any(math.hypot(xx-bx,yy-by)<4.6 for bx,by,_ in bridges):continue
                r=random.uniform(.45,1.45);rock(sector(xx,yy,'river bank'),(xx,yy,-.2),(r,r*.8,r*.65),'stone',True)
        for j in range(3):
            xx=x+random.uniform(-w*.35,w*.35);yy=y+random.uniform(-w*.35,w*.35)
            line('01 Water / pale flow crests','foam',[(xx+k*.15,yy+.12*math.sin(k*.4),-.445) for k in range(random.randint(3,10))],.014,4)
# Groundcover patches, flowers and leaf litter at several scales.
grass_count=0
for i in range(26000):
    x=random.uniform(-115,116);y=random.uniform(-112,110)
    if waterdist(x,y)<.4 or roaddist(x,y)<.08:continue
    if any(((x-cx)/(rx*.94))**2+((y-cy)/(ry*.94))**2<1 for cx,cy,rx,ry in occupied):continue
    if math.sin(x*.35)*math.cos(y*.28)<-.48:continue
    z=height(x,y)+.035;g=sector(x,y,'meadow tufts')
    for j in range(random.randint(3,6)):
        a=random.random()*math.tau;h=random.uniform(.16,.58);w=random.uniform(.026,.054)
        leaf(g,random.choice(['leaf1','leaf2','leaf2','leaf0']),(x,y,z),(x+math.cos(a)*h*.5,y+math.sin(a)*h*.5,z+h),w)
    if i%9==0:
        h=random.uniform(.35,.6)
        rod(g,'herb_leaf',(x,y,z),(x+.06,y,z+h),.014,.007,5)
        for k in range(5):
            a=k*math.tau/5
            leaf(g,'flower',(x+.06,y,z+h),(x+.06+.10*math.cos(a),y+.10*math.sin(a),z+h+.025),.055)
    if i%21==0:
        r=random.uniform(.10,.40);rock(sector(x,y,'scattered stones'),(x,y,z),(r,r*.73,r*.38),'stone',True)
    grass_count+=1
# Road-edge dressing without narrowing the walking path.
for n,pts,w in routes:
    for i,(x,y) in enumerate(pts):
        if i%3:continue
        a=pts[max(0,i-1)];b=pts[min(len(pts)-1,i+1)];dx=b[0]-a[0];dy=b[1]-a[1];L=math.hypot(dx,dy)
        for side in [-1,1]:
            xx=x-side*dy/L*(w/2+.45);yy=y+side*dx/L*(w/2+.45)
            if waterdist(xx,yy)<.5:continue
            for j in range(4):
                px=xx+random.uniform(-.5,.5);py=yy+random.uniform(-.5,.5);r=random.uniform(.14,.44)
                rock(sector(px,py,'road shoulders'),(px,py,height(px,py)),(r,r*.7,r*.3),'stone_light',False)
        if i%12==0 and waterdist(x,y)>3 and not (-58<x<3 and -36<y<49):
            xx=x-dy/L*(w/2+1.4);yy=y+dx/L*(w/2+1.4)
            place(lantern,'71 Wayfinding / amber roadside lanterns',xx,yy,0,1,0,0,2.5,1.4,True)
# Ground moss patches at roots of houses, to soften architecture/ground seams.
for i in range(900):
    x=random.uniform(-59,4);y=random.uniform(-34,47)
    if waterdist(x,y)<.2 or (-30<x<-20):continue
    if any(abs(x-cx)<rx*.8 and abs(y-cy)<ry*.8 for cx,cy,rx,ry in occupied):continue
    z=height(x,y)
    for j in range(5):
        a=random.random()*math.tau
        leaf('16 Town / foundation weeds','leaf1',(x,y,z),(x+.24*math.cos(a),y+.24*math.sin(a),z+.24),.035)
flush()
print('LANDSCAPE READY',tree_count,'TREES',bamboo_count,'BAMBOO',grass_count,'TUFTS',cliff_count,'CLIFFS',len(scene.objects),flush=True)
