occupied=[];landmarks=[]
def reserve(x,y,rx,ry):occupied.append((x,y,rx,ry))
def anchor(name,x,y,z=None):
    o=bpy.data.objects.new('Anchor / '+name,None);coll('80 Layout anchors / no gameplay behavior').objects.link(o)
    o.location=(x,y,height(x,y) if z is None else z);o.empty_display_type='CIRCLE';o.empty_display_size=1.2;o.hide_render=True
    landmarks.append({'name':name,'position_m':list(o.location)})
# Central town. Main street is north/south; shop facades face inward from both sides.
for side in [-1,1]:
    for i in range(7):
        x=-25+side*11.8;y=-26+i*9.6
        w=random.uniform(7.0,8.1);d=random.uniform(6.2,7.2)
        name=('平川客栈' if side==1 and i==3 else '茶肆' if side==-1 and i==1 else '杂货铺' if side==-1 and i==4 else '布庄' if i==5 else '')
        place(house,f'11 Town / shop {side:+d} {i+1:02d} {name}',x,y,-side*math.pi/2,1,w,d,2 if i%3!=2 else 1,name)
        reserve(x,y,5.6,5.0)
# Courtyard lanes and side residences behind the market frontage.
for side in [-1,1]:
    for i in range(5):
        x=-25+side*25;y=-21+i*12
        place(house,f'12 Town / courtyard home {side:+d} {i+1}',x,y,random.choice([0,math.pi]),1,7.3,6.2,1 if i%2 else 2,'')
        reserve(x,y,5,5)
        for k in range(8):slab('12 Town / courtyard stones',x-3+k*.8,y-5,height(x,y)+.04,.75,1.5,.13,'stone_light')
        fence('12 Town / courtyard fences',[(x-4.2,y-5.2),(x-4.2,y+4.8),(x+4.2,y+4.8),(x+4.2,y+1)],height(x,y))
# Rear hall is an architectural destination at the head of the central street.
place(house,'13 Town / northern guild hall',-25,48,0,1.35,10,7,2,'平川会馆');reserve(-25,48,9,7)
place(gate,'14 Town / southern ceremonial gateway',-25,-35,0,1,9)
place(gate,'14 Town / northern ceremonial gateway',-25,37,0,.80,9)
# Cross streets: hand-laid stone links to canal and outer loop.
for y in [-21,8,30]:
    for i in range(68):
        x=-54+i*.86
        if waterdist(x,y)<.25:continue
        for j in [-1,0,1]:slab('10 Town / cross lanes',x,y+j*.72,height(x,y)+.075,.80,.66,.12,'stone_light',random.uniform(-.08,.08))
# Stalls line the street margins without filling the walking corridor.
for i,(x,y,a) in enumerate([(-30,-21,math.pi/2),(-20,-15,-math.pi/2),(-30,-2,math.pi/2),(-20,5,-math.pi/2),(-30,17,math.pi/2),(-20,25,-math.pi/2),(-39,7,0),(-46,8,0),(-15,30,math.pi)]):
    place(stall,f'15 Market / stall {i+1:02d}',x,y,a,1,'indigo' if i%3==0 else 'cloth',i%3!=1)
    reserve(x,y,2.5,2.6)
for x,y,a in [(-47,-30,.3),(-43,34,-.4),(-13,-30,0),(-3,13,.2)]:
    place(cart,'16 Town / merchant carts',x,y,a);reserve(x,y,2,3)
for i,(x,y) in enumerate([(-30,-28),(-20,-9),(-30,10),(-20,27)]):
    place(banner,'16 Town / hanging shop banners',x,y,0,1,0,0,4.4,['茶','酒','杂\n货','平\n川'][i],'cloth',1.1,2.3)
anchor('Town / market street',-25,0)
anchor('Town / inn front',-20,2.8)

# Training plain: oval timber enclosure with four actual entrance gaps.
cx,cy=35,-10
for start,end in [(12,72),(108,162),(198,252),(288,342)]:
    pts=[(cx+18*math.cos(math.radians(a)),cy+20*math.sin(math.radians(a))) for a in range(start,end+1,6)]
    fence('20 Plain / arena split fence',pts)
for i in range(6):
    place(dummy,'21 Plain / straw and timber dummies',25+i*3.5,-1,0,1.15)
for i in range(5):place(target,'21 Plain / archery targets',25+i*4.3,4,0,1)
for x,y in [(20,-16),(51,-13),(42,5)]:place(weapons,'21 Plain / weapon racks',x,y)
place(tower,'22 Plain / watchtower',15,12,0,.82);reserve(15,12,4,4)
place(stall,'22 Plain / shaded tea rest',56,-5,math.pi/2,1.4,'cloth',False);reserve(56,-5,5,4)
place(cart,'22 Plain / supply wagon',59,4,-.3)
for x,y in [(17,-26),(52,-29),(18,7),(52,7)]:place(banner,'23 Plain / martial standards',x,y,0,1,0,0,6.4,'武','red_cloth',1.5,3.6)
# Low raised sparring platform, planked surface, stairs, no obstruction of main route.
for j in range(22):box('24 Plain / sparring platform','wood_edge',(40,-20+j*.30,height(40,-20)+.55),(8,.285,.13))
for x in [36.1,43.9]:
 for y in [-20,-13.7]:box('24 Plain / sparring platform','wood',(x,y,.55),(.3,.3,1))
for i in range(3):box('24 Plain / platform steps','wood',(40,-21+i*.3,.12+i*.16),(3.2,.31,.17))
reserve(35,-10,24,25);anchor('Plain / training arena',35,-10)

# Southeastern outpost: center lane, large command tent, staggered quarters and supplies.
place(tent,'30 Camp / command tent',67,-56,0,1,9,8,5.3);reserve(67,-56,7,6)
for i,(x,y,a) in enumerate([(49,-72,math.pi/2),(49,-59,math.pi/2),(79,-76,-math.pi/2),(80,-65,-math.pi/2),(61,-83,0),(76,-49,0)]):
    place(tent,f'31 Camp / quarter tent {i+1}',x,y,a,1,5.3,5.8,3.6);reserve(x,y,4,4)
place(tower,'32 Camp / southwest watchtower',43,-83,0,.95);reserve(43,-83,4,4)
place(tower,'32 Camp / northeast watchtower',87,-51,0,1);reserve(87,-51,4,4)
place(gate,'33 Camp / entry gate',54,-88,0,.95,7)
place(fire,'34 Camp / central fire',65,-72,0,2.5,0,0,0,1)
for x,y in [(61,-74),(69,-74),(65,-68)]:place(bench,'34 Camp / fire benches',x,y,0,1.8,0,0,0,1.5)
for x,y in [(84,-81),(87,-74)]:place(cart,'35 Camp / wagon supply yard',x,y,-.2)
for i in range(18):
 x=88+(i%3)*1.5;y=-66+(i//3)*1.25
 place(crate,'35 Camp / stacked provisions',x,y,0,1,0,0,1.05 if i%4==0 else 0,1.0)
for i in range(8):place(barrel,'35 Camp / stacked provisions',78+i%4*1.3,-84+i//4*1.1,0,1,0,0,0,1.4)
for x,y in [(57,-58),(77,-59),(47,-81)]:place(weapons,'35 Camp / spear racks',x,y)
for pts in [[(40,-86),(40,-70),(42,-54),(51,-48)],[(59,-89),(75,-89),(92,-84),(95,-66)],[(58,-47),(71,-43),(84,-44)]]:
 fence('36 Camp / perimeter rails',pts)
 # Tall pointed palisade at selected defensible edges.
 for a,b in zip(pts,pts[1:]):
  n=int(math.dist(a,b)/.66)
  for i in range(n):
   x=a[0]+(b[0]-a[0])*i/n;y=a[1]+(b[1]-a[1])*i/n;z=height(x,y)
   rod('36 Camp / palisade','wood',(x,y,z),(x,y,z+2.65),.15,.11,7)
   rod('36 Camp / palisade','wood_edge',(x,y,z+2.65),(x,y,z+3.12),.11,0,7)
for x,y in [(48,-87),(60,-86),(72,-52),(87,-63)]:place(banner,'37 Camp / garrison standards',x,y,0,1,0,0,6.8,'戍','red_cloth',1.7,3.8)
reserve(66,-67,31,24);anchor('Camp / command tent',67,-61)

# Cave region: large open main mouth, smaller surrounding entrances and ruined approach.
for i,(x,y,a,r,h,d) in enumerate([(-83,58,-.24,4.5,6.6,12),(-67,75,.18,2.8,4.5,8),(0,77,0,4.2,6.0,12),(94,22,-math.pi/2,3.7,5.7,10),(-91,35,-.35,2.7,4.3,8)]):
    place(cave,f'40 Caves / portal {i+1:02d}',x,y,a,1,r,h,d);reserve(x,y,8,10)
    anchor(f'Cave {i+1} / entrance',x+math.sin(a)*4,y-math.cos(a)*4)
    for j in range(7):
        xx=x+random.uniform(-11,11);yy=y+random.uniform(6,15)
        rock(f'41 Caves / rock mantle {i+1}',(xx,yy,2.5),(random.uniform(3,6),random.uniform(3,5),random.uniform(3,7)))
place(wall,'42 Caves / broken exploration ruins',-74,43,.3,1,10,2.8)
place(wall,'42 Caves / broken exploration ruins',-89,42,1.2,1,7,3.6)
place(cart,'42 Caves / abandoned cart',-74,39,.9)
for i in range(14):
 x=random.uniform(-90,-69);y=random.uniform(37,47)
 if roaddist(x,y)>0:rock('42 Caves / ruined stone debris',(x,y,.5),(.6,.45,.45),'stone',True)

# Northern mountain pass, roofed stone gate and defensive wings with an open passage.
place(gate,'50 Pass / gate timbers and tiled roof',27,89,0,1.1,9)
for x in [19.5,34.5]:place(wall,'51 Pass / masonry gate piers',x,89,0,1,5,6)
for x in [11.5,42.5]:place(wall,'51 Pass / broken wall wings',x,90,0,1,9,3.8)
place(tower,'52 Pass / border watchtower',44,95,0,1.2);reserve(44,95,4,4)
place(tent,'53 Pass / guard tent',38,79,0,1,5.2,5.5,3.5)
place(cart,'53 Pass / border supplies',47,83,.2)
for x in [20,34]:place(banner,'54 Pass / border standards',x,87,0,1,0,0,7.5,'戍','red_cloth',1.6,4.4)
# Carved boundary stele with inset inscription.
slab('54 Pass / north border stele',14,82,2.0,1.25,.65,2.0,'stone_light',.04)
lettering('54 Pass / north border stele','北\n境\n之\n门',(14,81.63,.65),.34)
for x,y in [(16,86),(38,86),(17,80)]:place(weapons,'54 Pass / chevaux de frise',x,y,0,1.1)
reserve(27,88,23,13);anchor('Pass / north exit',27,95)

# Bridges placed at exact path/river intersections. Local bridge axis is Y.
def intersect(a,b,c,d):
    ax=b[0]-a[0];ay=b[1]-a[1];bx=d[0]-c[0];by=d[1]-c[1];den=ax*by-ay*bx
    if abs(den)<1e-9:return None
    t=((c[0]-a[0])*by-(c[1]-a[1])*bx)/den;u=((c[0]-a[0])*ay-(c[1]-a[1])*ax)/den
    if 0<=t<=1 and 0<=u<=1:return (a[0]+t*ax,a[1]+t*ay,math.atan2(ay,ax)-math.pi/2,abs(den)/(math.hypot(ax,ay)*math.hypot(bx,by)))
bridges=[]
for n,pts,w in routes:
 for a,b in zip(pts,pts[1:]):
  for rpts,rw in rivers:
   for c,d in zip(rpts,rpts[1:]):
    hit=intersect(a,b,c,d)
    if hit and all(math.hypot(hit[0]-v[0],hit[1]-v[1])>5 for v in bridges):
     x,y,an,si=hit;length=(rw+3.7)/max(.32,si)
     place(bridge,'60 Bridges / '+n,x,y,an,1,length,w+.5,False)
     bridges.append((x,y,length));reserve(x,y,4,4);anchor('Bridge / '+n,x,y)
# Three small arched stone crossings link market cross-lanes to the eastern canal bank.
for yy in [-21,8,30]:
    # Canal x at requested y from linear interpolation.
    for a,b in zip(canal,canal[1:]):
        if min(a[1],b[1])<=yy<max(a[1],b[1]):
            xx=a[0]+(b[0]-a[0])*(yy-a[1])/(b[1]-a[1]);break
    place(bridge,'61 Town / arched canal bridge',xx,yy,math.pi/2,1,7,3.6,True)
    reserve(xx,yy,5,3);bridges.append((xx,yy,7))
# Southwest entry path, smaller resting pavilion and bamboo gateway.
place(gate,'62 Entry / mountain road gateway',-58,-91,-.18,1,8)
place(stall,'62 Entry / roadside tea rest',-66,-88,.15,1.3,'cloth',False);reserve(-66,-88,5,4)
anchor('Spawn / southwest old road',-64,-103)
flush()
print('REGIONS READY',len(scene.objects),'BRIDGES',len(bridges),flush=True)

scene["source_portal_orientation_correct"]=True
