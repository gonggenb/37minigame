"""Refinement after the first actual render: silhouettes, natural enclosure, ground breakup."""
# Broad, layered pine bough silhouettes with needles visible on the perimeter.
for ix,center in enumerate(canopies):
    for j in range(3):
        p=(center[0]+random.uniform(-.32,.32),center[1]+random.uniform(-.3,.3),center[2]+random.uniform(-.10,.08))
        rock('11 Rest • dense pine boughs',p,(random.uniform(.45,.72),random.uniform(.4,.65),random.uniform(.13,.23)),
             'leaf'+str((ix+j)%3),False)
        for k in range(64):
            an=random.random()*math.tau;r=random.uniform(.25,.65)
            start=Vector((p[0]+math.cos(an)*r,p[1]+math.sin(an)*r,p[2]+random.uniform(-.05,.15)))
            for side in [-1,0,1]:
                a=an+side*.48;ll=random.uniform(.15,.30)
                leaf('11 Rest • dense pine bough needles','leaf'+str(random.randrange(3)),start,
                     start+Vector((math.cos(a)*ll,math.sin(a)*ll,random.uniform(-.02,.05))),.021)

# Rear woodland curtain sits outside the playable zone and avoids a flat map cut edge.
for i in range(145):
    x=random.uniform(-17,17);y=random.uniform(-12,18)
    if abs(x)<10.7 and y<10.3:continue
    if abs(y-stream_y(x))<1.1:continue
    if y<-7 and abs(x)<8:continue
    h=random.uniform(4,7.5)*( .65 if y<0 else 1)
    for j in range(3):
        bamboo('23 Scenic • outer bamboo curtain',x+random.uniform(-.3,.3),y+random.uniform(-.3,.3),h*random.uniform(.75,1.05))
        culms+=1
for i in range(38):
    x=random.uniform(-20,20);y=random.uniform(12.7,20)
    rock('23 Scenic • distant wooded ridge',(x,y,.5),(random.uniform(1.5,3),random.uniform(1.2,2.5),random.uniform(1.7,3.5)),'cliff',True)

# Sculpted broad-leaf shrubs and ferns cover sparse ground between routes.
for x,y in [(-7.2,1.3),(-3.3,6.6),(2.2,6.7),(8.7,2.8),(-7.2,-.1),(-3.7,-3.0),
            (2.8,-5.5),(-4.1,-5.7),(8.7,-4.8),(-8.7,5.5),(2.3,8.8),(-8.2,-5.1)]:
    for i in range(45):
        a=random.random()*math.tau;r=random.uniform(.1,.55)
        p=Vector((x,y,ground(x,y)+.07))
        end=p+Vector((math.cos(a)*r,math.sin(a)*r,random.uniform(.18,.48)))
        rod('24 Nature • broadleaf shrubs','wood',p,end,.006,.002,4)
        for side in [-1,1]:
            aa=a+side*.6
            leaf('24 Nature • broadleaf shrubs','leaf'+str(random.randrange(4)),end,
                 end+Vector((math.cos(aa)*.22,math.sin(aa)*.22,.05)),.075)
for j in range(60):
    x=random.uniform(-9,9);y=random.uniform(-6,9)
    if blocked(x,y):continue
    z=ground(x,y)
    for i in range(7):
        a=i*math.tau/7;l=random.uniform(.3,.6)
        base=Vector((x,y,z));tip=base+Vector((math.cos(a)*l,math.sin(a)*l,.18))
        rod('24 Nature • fine fern fronds','bamboo',base,tip,.006,.002,5)
        for k in range(7):
            t=(k+1)/8;p=base+(tip-base)*t;p.z+=.13*math.sin(t*math.pi)
            for side in [-1,1]:
                aa=a+side*.8;length=.11*(1-t*.7)
                leaf('24 Nature • fine fern fronds','leaf1',p,p+Vector((math.cos(aa)*length,math.sin(aa)*length,0)),.023)

# Additional low mossy stones merge cave tunnel into the surrounding hillside.
for x,y,z,s in [(5.7,7.8,2.3,.75),(4.3,7.9,1.9,.65),(7.0,7.9,1.9,.7)]:
    rock('08 Cave • rear hill integration',(x,y,z),(s,s*.8,s*.7))

# Raised rolled tile end caps and join bands add scale detail to the pavilion eave.
for i in range(30):
    x=-5.2-2.30+i*.158;y=5.0-1.94
    pts=[(x+.044*math.cos(a*math.pi/8),y-.014,3.74+.044*math.sin(a*math.pi/8)) for a in range(9)]
    line('06 Reward • ceramic tile end caps','roof_alt',pts,.011,5)
flush()
scene['bamboo_culms']=culms
print('REFINEMENT_READY',len(scene.objects),flush=True)
