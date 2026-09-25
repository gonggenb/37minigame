# Original modular architecture, local origin at floor center, main facade faces -Y.
# All surfaces remain editable meshes grouped by landmark and material.
def window(g,x,y,z,w=1.3,h=1.65):
    box(g,'ink',(x,y,z),(w,.07,h))
    box(g,'paper',(x,y-.045,z),(w*.89,.018,h*.89))
    for xx in [x-w/2,x+w/2]:box(g,'wood_edge',(xx,y-.09,z),(.10,.13,h+.15))
    for zz in [z-h/2,z+h/2]:box(g,'wood_edge',(x,y-.09,zz),(w+.16,.13,.10))
    for i in range(5):box(g,'wood',(x-w*.4+i*w*.2,y-.115,z),(.036,.035,h))
    for i in range(6):box(g,'wood',(x,y-.12,z-h*.4+i*h*.16),(w,.04,.035))
    # Smaller geometric lattice corner details.
    for xx in [x-w*.32,x+w*.32]:
        for zz in [z-h*.32,z+h*.32]:
            line(g,'wood_edge',[(xx-.13,y-.15,zz),(xx,y-.15,zz+.13),(xx+.13,y-.15,zz),(xx,y-.15,zz-.13),(xx-.13,y-.15,zz)],.024,4)

def house(g,w=8,d=7,floors=2,sign=''):
    h=3.1*floors;floor=.52
    for k in range(3):box(g,'stone',(0,0,.16+k*.14),(w+.65-k*.12,d+.7-k*.12,.2))
    # Uneven masonry skirt.
    for side in [-1,1]:
        for i in range(int(w/.68)):
            slab(g,-w/2+.35+i*.68,side*(d/2+.06),.5,.63,.36,.35,random.choice(['stone','stone_light']))
    box(g,'plaster',(0,0,floor+h/2),(w,d,h))
    for level in range(floors+1):
        zz=floor+level*3.1
        for yy in [-d/2-.06,d/2+.06]:box(g,'wood',(0,yy,zz),(w+.2,.2,.22))
        for xx in [-w/2,w/2]:box(g,'wood',(xx,0,zz),(.2,d,.22))
    for xx in [-w/2,-w/4,0,w/4,w/2]:
        for yy in [-d/2-.09,d/2+.09]:
            box(g,'wood',(xx,yy,floor+h/2),(.22,.24,h+.2))
            box(g,'stone_light',(xx,yy,.64),(.42,.42,.29))
    for side in [-1,1]:
        for yy in [-d*.25,0,d*.25]:box(g,'wood',(side*w/2,yy,floor+h/2),(.20,.18,h))
    # Front lower shop doorway, timber folding panels and thresholds.
    box(g,'ink',(0,-d/2-.13,1.85),(2.2,.05,2.6))
    for xx in [-1.15,1.15]:
        box(g,'wood_edge',(xx,-d/2-.2,1.85),(.16,.2,2.72))
        for k in range(6):box(g,'wood',(xx+math.copysign(.25,xx),-d/2-.22,.88+k*.17),(.42,.08,.12))
    box(g,'wood_edge',(0,-d/2-.18,3.22),(2.5,.22,.22))
    for k in range(3):box(g,'stone_light',(0,-d/2-.85+k*.25,.14+k*.17),(2.8,.34,.19))
    for xx in [-w*.33,w*.33]:window(g,xx,-d/2-.13,2.06,min(1.5,w*.18),1.7)
    # Real balcony deck, guardrails, supports and upper lattice.
    if floors>1:
        for i in range(int(w/.22)):
            box(g,'wood_edge',(-w/2+.11+i*.22,-d/2-.62,3.66),(.21,1.32,.10))
        fence(g,[(-w/2,-d/2-.17),(-w/2,-d/2-1.18),(w/2,-d/2-1.18),(w/2,-d/2-.17)],3.73)
        for xx in [-w*.35,0,w*.35]:
            window(g,xx,-d/2-.13,5.02,w*.22,1.82)
            rod(g,'wood',(xx,-d/2-.2,2.85),(xx,-d/2-1.1,3.63),.095,n=6)
    # Roof structure, dougong brackets, layered ceramic hip roof and eave rafters.
    for xx in [-w/2,0,w/2]:
        for yy in [-d/2,d/2]:
            for k in range(3):box(g,'wood_edge',(xx,yy,floor+h-.22+k*.17),(.72-k*.1,.48+k*.16,.12))
            for side in [-1,1]:rod(g,'wood',(xx,yy,floor+h-.62),(xx+side*.55,yy,floor+h-.06),.075,n=6)
    for i in range(int(w/.36)):
        xx=-w/2+i*.36
        for s in [-1,1]:rod(g,'wood_edge',(xx,s*(d/2-.4),floor+h-.1),(xx,s*(d/2+.9),floor+h-.12),.055,n=8)
    roof(g,0,0,floor+h,w/2+1.0,d/2+.95,2.35 if floors>1 else 1.95)
    if floors>1:roof(g,0,-d/2-.44,3.62,w/2+.30,1.02,.74)
    for xx in [-w*.31,w*.31]:lantern(g,xx,-d/2-.68,2.8,1.45,False)
    if sign:
        box(g,'wood',(0,-d/2-.72,3.24),(3.9,.15,.68))
        # Gold inset frame, physical text rather than flat billboard facade.
        for zz in [2.96,3.53]:box(g,'brass',(0,-d/2-.815,zz),(3.85,.025,.032))
        ob=lettering(g,sign,(0,-d/2-.82,3.03),.48);ob.data.materials.clear();ob.data.materials.append(M['brass'])
    for side in [-1,1]:
        barrel(g,side*(w/2-.55),-d/2-1.3,.03,1.05)
        pot(g,side*(w/2+.45),-d/2-.25,0,2.3)

def gate(g,w=8):
    for x in [-w/2,w/2]:
        slab(g,x,0,.4,1.05,1.05,.4,'stone')
        box(g,'wood',(x,0,2.9),(.48,.5,5.25))
        for z in [1.0,4.6,5.2]:box(g,'wood_edge',(x,0,z),(.7,.65,.15))
        for side in [-1,1]:rod(g,'wood_edge',(x,0,4.0),(x+side*.85,0,5.0),.14,n=6)
        lantern(g,x,0,3.65,1.5,False)
    for z in [4.7,5.2]:box(g,'wood',(0,0,z),(w+1,.4,.32))
    roof(g,0,0,5.43,w/2+1.05,1.2,1.0)
    box(g,'wood_edge',(0,-.24,4.65),(3.3,.15,.8))
    ob=lettering(g,'平川山镇',(0,-.34,4.36),.55);ob.data.materials.clear();ob.data.materials.append(M['brass'])

def stall(g,m='cloth',goods=True):
    w=3.6;d=2.9
    vs=[];rows=12;cols=12
    for j in range(rows+1):
        for i in range(cols+1):
            x=-w/2+w*i/cols;y=-d/2+d*j/rows
            z=2.62+.5*(1-(x/(w/2))**2)-.16*math.sin(j*math.pi/rows)+.025*math.cos(i*2+j)
            vs.append((x,y,z))
    geo(g,m,vs,[(j*(cols+1)+i,j*(cols+1)+i+1,(j+1)*(cols+1)+i+1,(j+1)*(cols+1)+i) for j in range(rows) for i in range(cols)])
    for x in [-w/2,w/2]:
        for y in [-d/2,d/2]:
            rod(g,'wood',(x,y,0),(x,y,2.85),.065,n=8)
            line(g,'rope',[(x,y,2.65),(x*1.3,y*1.3,.12)],.016,6)
            rod(g,'wood',(x*1.3,y*1.3,0),(x*1.3,y*1.3,.34),.042,n=6)
    for x in [-1.75,0,1.75]:line(g,'rope',[(x,-d/2,2.63+.5*(1-(x/(w/2))**2)),(x,0,2.47+.5*(1-(x/(w/2))**2)),(x,d/2,2.63+.5*(1-(x/(w/2))**2))],.014)
    table(g,0,-.45,0,3.1,1.25)
    if goods:
        for x in [-1.0,0,1.0]:
            box(g,'wood',(x,-.45,.85),(.85,.85,.22))
            for k in range(14):
                px=x+random.uniform(-.30,.30);py=-.45+random.uniform(-.29,.29)
                rock(g,(px,py,1.03+random.random()*.12),(.1,.09,.09),random.choice(['berry','leaf2','herb_leaf']),False)
    else:
        pot(g,-.4,-.45,.75,1.5)
        for x in [.1,.4,.7]:pot(g,x,-.5,.75,.5)
        bench(g,0,-1.6,0,2.1)
    for x in [-1.3,1.3]:crate(g,x,.85,0,.7)
    lantern(g,0,-1.52,2.7,.95,False)

def tent(g,w=5,d=6,h=3.6):
    vs=[];rows=18;cols=16
    for j in range(rows+1):
        y=-d/2+j*d/rows
        for i in range(cols+1):
            x=-w/2+i*w/cols
            zz=h-abs(x/(w/2))*(h-.65)-.14*math.sin(j*math.pi/rows)+.045*math.sin(i*.9+j*.8)
            vs.append((x,y,zz))
    geo(g,'cloth',vs,[(j*(cols+1)+i,j*(cols+1)+i+1,(j+1)*(cols+1)+i+1,(j+1)*(cols+1)+i) for j in range(rows) for i in range(cols)])
    geo(g,'cloth',[(-w/2,d/2,0),(-w/2,d/2,.65),(0,d/2,h),(w/2,d/2,.65),(w/2,d/2,0)],[(0,1,2,3,4)])
    for side in [-1,1]:
        geo(g,'cloth',[(0,-d/2,h),(side*w/2,-d/2,.65),(side*w/2,-d/2,0),(side*w*.25,-d/2-.1,.25)],[(0,1,2,3)])
        for yy in [-d/2,0,d/2]:
            line(g,'rope',[(side*w/2,yy,.7),(side*(w/2+1.2),yy-.3,.1)],.025)
            rod(g,'wood',(side*(w/2+1.2),yy-.3,0),(side*(w/2+1.2),yy-.3,.45),.055,n=7)
    for yy in [-d/2,d/2]:rod(g,'wood',(0,yy,0),(0,yy,h+.25),.09,n=10)
    for yy in [-d*.33,0,d*.33]:line(g,'rope',[(-w/2,yy,.69),(0,yy,h-.08),(w/2,yy,.69)],.016)
    for xx in [-w*.33,w*.33]:crate(g,xx,d*.28,0,.8)
    lantern(g,-w/2-.3,-d/2,1.7,1.3)

def tower(g):
    h=8.0;r=1.8
    for x in [-r,r]:
        for y in [-r,r]:
            slab(g,x,y,.4,.85,.85,.4,'stone')
            rod(g,'wood',(x*1.25,y*1.25,.3),(x,y,h+2.8),.20,.13,n=10)
    for z in [1,3.9,7.5,8]:
        for side in [-1,1]:
            box(g,'wood',(0,side*r,z),(4.2,.25,.27))
            box(g,'wood',(side*r,0,z),(.25,4.2,.27))
    for z in [1,4]:
        for side in [-1,1]:
            rod(g,'wood_edge',(-r,side*r,z),(r,side*r,z+3.4),.10,n=7)
            rod(g,'wood_edge',(r,side*r,z),(-r,side*r,z+3.4),.10,n=7)
            rod(g,'wood_edge',(side*r,-r,z),(side*r,r,z+3.4),.10,n=7)
    for i in range(19):box(g,'wood_edge',(-2.0+i*.22,0,h),(.21,4.25,.12))
    fence(g,[(-1.8,-1.8),(-1.8,1.8),(1.8,1.8),(1.8,-1.8),(.55,-1.8)],h+.08)
    for xx in [-.45,.45]:rod(g,'wood',(xx,-2.3,0),(xx,-1.6,h),.065,n=8)
    for i in range(23):rod(g,'wood_edge',(-.46,-2.3+i*.7/23,.3+i*.33),(.46,-2.3+i*.7/23,.3+i*.33),.052,n=8)
    roof(g,0,0,h+2.9,2.6,2.6,1.75)
    banner(g,0,-1.92,h+2.7,'戍','red_cloth',1.5,3.6)

def cart(g):
    for i in range(10):box(g,'wood_edge',(-.85+i*.19,0,1.0),(.18,2.6,.12))
    for side in [-1,1]:
        x=side*1.18
        ring(g,'wood',(x,0,.90),.87,.075,'X',36);ring(g,'iron',(x,0,.90),.93,.026,'X',36)
        rod(g,'wood_edge',(x-.14,0,.9),(x+.14,0,.9),.13,n=12)
        for i in range(14):
            a=i*math.tau/14;rod(g,'wood_edge',(x,0,.9),(x,.83*math.cos(a),.9+.83*math.sin(a)),.038,n=6)
        for z in [1.3,1.64]:box(g,'wood',(side*.94,0,z),(.10,2.65,.17))
        for y in [-1.2,0,1.2]:box(g,'wood_edge',(side*.94,y,1.38),(.12,.12,.94))
        rod(g,'wood',(side*.7,.5,.85),(side*.75,-3.3,1.2),.075,n=8)
    rod(g,'iron',(-1.35,0,.9),(1.35,0,.9),.095,n=10)
    barrel(g,0,.5,1.1,1.2);crate(g,0,-.55,1.1,.8)

def bridge(g,length=9,width=5,stone=False):
    m='stone_light' if stone else 'wood_edge'
    count=int(length/.32)
    for i in range(count+1):
        y=-length/2+i*length/count;z=.14+.72*math.sin(i/count*math.pi)
        slab(g,0,y,z,width,.31,.25,m) if stone else box(g,m,(0,y,z),(width,.30,.18))
    for side in [-1,1]:
        x=side*(width/2+.08)
        pts=[]
        for i in range(7):
            y=-length/2+i*length/6;z=.14+.72*math.sin(i/6*math.pi)
            box(g,'stone' if stone else 'wood',(x,y,z+.67),(.25,.25,1.45))
            if stone:rod(g,'stone_light',(x,y,z+1.4),(x,y,z+1.58),.23,.05,n=6)
            pts.append((x,y,z+1.25))
        line(g,m,pts,.095,8)
        line(g,m,[(x,y,z-.5) for x,y,z in pts],.065,8)
        if not stone:
            for y in [-length*.3,length*.3]:rod(g,'wood',(x,y,-1.8),(x,y,.8),.19,n=8)
    if stone:
        # Actual arch voussoirs with a hollow opening below the walkway.
        for side in [-1,1]:
            for i in range(18):
                a=i*math.pi/18;b=(i+1)*math.pi/18
                vs=[(side*width/2,y,z) for rr in [1,1.16] for t in [a,b] for y,z in [(length*.48*math.cos(t),-1.0+rr*1.5*math.sin(t))]]
                geo(g,'stone',vs,[(0,1,3,2)])

def cave(g,r=4,h=5.8,depth=10):
    vs=[];N=28
    for j in range(5):
        for i in range(N+1):
            a=i*math.pi/N;rr=1-.12*math.sin(j*.7)
            vs.append((r*rr*math.cos(a),j*depth/4,.05+h*rr*math.sin(a)))
    geo(g,'cliff',vs,[(j*(N+1)+i,j*(N+1)+i+1,(j+1)*(N+1)+i+1,(j+1)*(N+1)+i) for j in range(4) for i in range(N)])
    geo(g,'ink',[(0,depth,0)]+[(r*math.cos(i*math.pi/N),depth,h*math.sin(i*math.pi/N)) for i in range(N+1)],[(0,i,i+1) for i in range(1,N+1)])
    for layer in [0,1]:
        for i in range(14):
            a=i*math.pi/13
            rock(g,((r+1+layer*.65)*math.cos(a),layer*1.45,h*math.sin(a)+.3),(random.uniform(1,1.55),1.55,random.uniform(1,1.55)))
    for side in [-1,1]:
        box(g,'wood',(side*(r-.35),-.4,h*.39),(.40,.42,h*.78))
        rod(g,'wood',(side*(r-.3),-.44,h*.47),(side*(r-1.4),-.44,h*.80),.18,n=6)
        lantern(g,side*(r+1),-1.55,2.7,1.8)
        crate(g,side*(r+1.1),-2.4,0,1.0);barrel(g,side*(r+1.3),-.2,0,1.3)
    box(g,'wood',(0,-.4,h*.84),(2*r-.2,.45,.4))
    for j in range(11):
        for i in range(5):slab(g,(i-2)*1.1,-3+j*.9,.04,1.04,.85,.14,'stone')
    pointlight(g,'Cave recessed amber',(0,3.5,2),(1,.30,.06),90,1)
    banner(g,r+2.1,0,h+1.2,'戒','red_cloth',1.7,3.7)

def wall(g,length=12,h=4):
    for row in range(int(h/.55)):
        for i in range(int(length/.94)):
            x=-length/2+(i+.5)*.94+(row%2)*.45
            if row>h/.55-2 and random.random()<.28:continue
            box(g,random.choice(['stone','stone','stone_light']),(x,0,.29+row*.55),(.90,.85+random.random()*.12,.51),random.uniform(-.025,.025))
    for i in range(int(length/1.85)):
        box(g,'stone',(-length/2+.6+i*1.85,0,h+.26),(.90,.92,.54))

def weapons(g):
    fence(g,[(-1.6,0),(1.6,0)],0)
    for i in range(7):
        x=-1.3+i*.43
        rod(g,'wood',(x,-.25,0),(x,.1,2.55),.033,n=7)
        rod(g,'iron',(x,.1,2.55),(x,.13,3.06),.095,0,n=4)

def dummy(g):
    rod(g,'wood',(0,0,0),(0,0,2.0),.12,n=10)
    rod(g,'rope',(0,0,.8),(0,0,1.65),.25,.22,n=12)
    rock(g,(0,0,1.98),(.21,.21,.24),'rope',False)
    rod(g,'wood',(-.70,0,1.42),(.70,0,1.42),.08,n=8)
    for z in [ .86,1.0,1.14,1.28,1.42,1.57]:ring(g,'wood_edge',(0,0,z),.252,.02,n=16)

def target(g):
    for side in [-1,1]:rod(g,'wood',(side*.55,0,0),(side*.3,0,1.8),.075,n=8)
    # Circular vertical target facing -Y.
    for rr,ma in [( .70,'cloth'),(.53,'red_cloth'),(.37,'cloth'),(.20,'red_cloth')]:
        rod(g,ma,(0,-.1-(.7-rr)*.04,1.9),(0,-.16-(.7-rr)*.04,1.9),rr,n=32)
print('MODULES READY')
