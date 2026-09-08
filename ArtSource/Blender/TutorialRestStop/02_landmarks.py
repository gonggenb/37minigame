def pointlight(g,name,p,color=(1,.43,.12),energy=28,r=.22):
    d=bpy.data.lights.new(name,'POINT');d.energy=energy;d.color=color;d.shadow_soft_size=r
    ob=bpy.data.objects.new(name,d);coll(g).objects.link(ob);ob.location=p
    return ob
def lantern(g,x,y,z,scale=1,post=True):
    # Octagonal silk body with actual eight ribs and two pierced-looking caps.
    if post:
        rod(g,'wood',(x,y,ground(x,y)),(x,y,z+.42*scale),.055*scale,n=8)
        slab(g,x,y,ground(x,y)+.17,.33,.33,.18,'stone')
    rr=.17*scale;h=.5*scale
    rod(g,'paper',(x,y,z-h*.5),(x,y,z+h*.5),rr,n=8)
    for i in range(8):
        a=i*math.tau/8
        rod(g,'iron',(x+rr*math.cos(a),y+rr*math.sin(a),z-h*.54),(x+rr*math.cos(a),y+rr*math.sin(a),z+h*.54),.012*scale,n=5)
    for zz in [z-h*.56,z+h*.56]:
        rod(g,'wood_edge',(x,y,zz-.026),(x,y,zz+.026),rr*1.24,n=8)
    rod(g,'roof',(x,y,z+h*.58),(x,y,z+h*.82),rr*1.48,rr*.25,n=8)
    rod(g,'brass',(x,y,z+h*.82),(x,y,z+h*.96),.04*scale,.012*scale,n=8)
    ring(g,'iron',(x,y,z+h*1.02),.05*scale,.008*scale,axis='X',n=12)
    pointlight(g,'Lantern • amber pool',(x,y,z),(1,.47,.14),45*scale,.22*scale)
def fence(g,pts,z=None):
    for a,b in zip(pts,pts[1:]):
        n=max(1,math.ceil(math.dist(a,b)/.9));ps=[]
        for i in range(n+1):
            t=i/n;x=a[0]+(b[0]-a[0])*t;y=a[1]+(b[1]-a[1])*t;h=ground(x,y) if z is None else z
            ps.append((x,y,h))
            box(g,'wood',(x,y,h+.43),(.085,.085,.86))
            box(g,'wood_edge',(x,y,h+.86),(.13,.13,.07))
            for zz in [.3,.65]:
                for k in range(3):ring(g,'rope',(x,y,h+zz+k*.015),.062,.009,n=8)
        for p,q in zip(ps,ps[1:]):
            for h in [.27,.66]:rod(g,'wood',(p[0],p[1],p[2]+h),(q[0],q[1],q[2]+h),.036,n=7)
def timber_beam(g,a,b,width=.12):
    rod(g,'wood',a,b,width,n=4)
    for p in [a,b]:rock(g,p,(.025,.025,.025),'iron',False)
def roof(g,cx,cy,z,rx,ry,height):
    def pt(edge,u,t):
        k=1-t;zz=z+height*(t*t*.65+t*.35)+.19*k**9
        rr=[(cx+sx*(rx*k+max(0,rx-ry)*t*.85),cy+sy*ry*k,zz+.09*k**5) for sx,sy in [(-1,-1),(1,-1),(1,1),(-1,1)]]
        a,b=rr[edge],rr[(edge+1)%4]
        return tuple(a[i]*(1-u)+b[i]*u for i in range(3))
    for edge in range(4):
        width=math.dist(pt(edge,0,0),pt(edge,1,0));cols=max(8,int(width/.15));rows=13
        for row in range(rows):
            t0=row/rows*.98;t1=(row+1)/rows*.98+.008
            for col in range(cols):
                # Each overlapping roof tile has a curved pan and a real edge thickness.
                u0=col/cols+.003;u1=(col+1)/cols-.003
                vs=[]
                for t in [t0,t1]:
                    for u in [u0,(u0+u1)/2,u1]:
                        p=pt(edge,u,t);vs.append((p[0],p[1],p[2]+.026*(u!=(u0+u1)/2)))
                vs+= [(x,y,zz-.025) for x,y,zz in vs]
                faces=[(0,1,4,3),(1,2,5,4),(0,6,7,1),(1,7,8,2),(2,8,11,5),(5,11,10,4),(4,10,9,3),(3,9,6,0)]
                geo(g,random.choice(['roof','roof','roof','roof_alt']),vs,faces)
        line(g,'wood_edge',[pt(edge,0,0),pt(edge,1,0)],.075,8)
        line(g,'roof_alt',[tuple(v+( .055 if k==2 else 0) for k,v in enumerate(pt(edge,0,i/25))) for i in range(26)],.065,8)
        x,y,zz=pt(edge,0,0)
        rod(g,'roof_alt',(x,y,zz),(cx+(x-cx)*1.1,cy+(y-cy)*1.1,zz+.28),.065,.018,8)
    r=max(0,rx-ry)*.85
    line(g,'roof_alt',[(cx-r-.15,cy,z+height+.06),(cx+r+.15,cy,z+height+.06)],.10,10)
    for x in [cx-r-.15,cx+r+.15]:
        tube(g,'roof_alt',[(x,cy,z+height+.04),(x,cy,z+height+.24),(x+math.copysign(.16,x-cx or 1),cy,z+height+.37)],[.09,.06,.015])

def crate(g,x,y,z,size=.52):
    # Plank faces, corner stiles, diagonal brace, nail heads.
    for i in range(5):
        off=(i-2)*size/5
        for yy in [y-size/2,y+size/2]:box(g,'wood_edge',(x+off,yy,z+size/2),(size*.19,.045,size))
        for xx in [x-size/2,x+size/2]:box(g,'wood',(xx,y+off,z+size/2),(.045,size*.19,size))
        box(g,'wood_edge',(x+off,y,z+size),(size*.19,size,.045))
    for zz in [z+.06,z+size-.06]:
        for yy in [y-size*.52,y+size*.52]:
            box(g,'wood',(x,yy,zz),(size+.07,.05,.08))
            for xx in [x-size*.4,x+size*.4]:rod(g,'iron',(xx,yy-.031,zz),(xx,yy-.04,zz),.014,n=6)
    rod(g,'wood',(x-size*.4,y-size*.56,z+.1),(x+size*.4,y-size*.56,z+size-.1),.035,n=4)
def barrel(g,x,y,z,scale=1):
    rows=[(0,.24),(.1,.27),(.45,.31),(.75,.28),(.83,.24)]
    for i in range(14):
        a=i*math.tau/14+.008;b=(i+1)*math.tau/14-.008;vs=[]
        for zz,r in rows:
            for an in [a,b]:vs.append((x+r*scale*math.cos(an),y+r*scale*math.sin(an),z+zz*scale))
        geo(g,'wood_edge' if i%3 else 'wood',vs,[(2*j,2*j+1,2*j+3,2*j+2) for j in range(4)])
    for zz,r in [(.11,.274),(.65,.296),(.77,.274)]:
        for k in [-1,1]:ring(g,'iron',(x,y,z+(zz+k*.016)*scale),r*scale,.014*scale,n=28)
    rod(g,'wood',(x,y,z+.806*scale),(x,y,z+.83*scale),.242*scale,n=14)
    for i in [-2,-1,0,1,2]:
        yy=i*.084;dx=math.sqrt(max(0,.235**2-yy**2));line(g,'iron',[(x-dx*scale,y+yy*scale,z+.832*scale),(x+dx*scale,y+yy*scale,z+.832*scale)],.003,4)

# Short low stone bridge at the single stream crossing. Bridge is a model, not a teleporter.
g='05 Entry • stone footbridge'
for i in range(12):
    y=-9.25+i*.255;zz=.18+.22*math.sin(i/11*math.pi)
    for side in [-1,1]:slab(g,side*.48,y,zz,.95,.25,.22,'stone',random.uniform(-.01,.01))
for side in [-1,1]:
    x=side*1.05
    for y in [-9.1,-8.2,-7.3,-6.45]:
        box(g,'stone',(x,y,.52),(.18,.18,.72));rod(g,'stone_light',(x,y,.9),(x,y,1),.15,.06,n=4)
    line(g,'stone_light',[(x,y,.68) for y in [-9.15,-8.2,-7.3,-6.4]],.055,6)
for x in [-1.35,1.35]:lantern(g,x,-6.35,1.55,.85)

# Old roadside pavilion at the northwest reward pocket.
g='06 Reward • old roadside pavilion';cx=-5.2;cy=5.0
for i in range(3):
    box(g,'stone',(cx,cy,.21+i*.18),(4.4-i*.16,3.7-i*.14,.2))
for j in range(6):
    for i in range(8):slab(g,cx-1.85+i*.52,cy-1.47+j*.52,.65,.49,.49,.08,'stone_light')
for j in range(4):
    box(g,'stone_light',(cx,cy-2.55+j*.27,.16+j*.13),(1.8,.3,.17))
for x in [cx-1.65,cx+1.65]:
    for y in [cy-1.32,cy+1.32]:
        rod(g,'stone_light',(x,y,.65),(x,y,.9),.21,n=8)
        rod(g,'wood',(x,y,.9),(x,y,3.48),.12,n=12)
        for dx,dy in [(0,.48),(0,-.48),(.48,0),(-.48,0)]:
            rod(g,'wood_edge',(x,y,2.98),(x+dx,y+dy,3.46),.065,n=6)
        for z in [3.30,3.48]:box(g,'wood_edge',(x,y,z),(.42,.34,.10))
for y in [cy-1.35,cy+1.35]:box(g,'wood',(cx,y,3.43),(3.8,.16,.19))
for x in [cx-1.67,cx+1.67]:box(g,'wood',(x,cy,3.43),(.16,3.1,.19))
# Visible rafters beneath the overhanging roof.
for i in range(11):
    x=cx-1.5+i*.3
    line(g,'wood_edge',[(x,cy-1.65,3.32),(x,cy-1.05,3.48)],.034,6)
    line(g,'wood_edge',[(x,cy+1.65,3.32),(x,cy+1.05,3.48)],.034,6)
roof(g,cx,cy,3.47,2.42,1.94,1.35)
fence(g,[(cx-1.63,cy+.2),(cx-1.63,cy+1.3),(cx+1.63,cy+1.3),(cx+1.63,cy+.2)],.68)
for x in [cx-1.25,cx+1.25]:lantern(g,x,cy-1.27,2.86,.82,False)
for x in [cx-1.25,cx+1.25]:barrel('07 Reward • chest and supplies',x,cy+.66,.68,.65)

# Detailed treasure chest with curved segmented lid, bands, hinge and rivets.
g='07 Reward • chest and supplies';x=cx;y=cy-.67;z=.67
for i in range(7):
    xx=x+(i-3)*.15
    box(g,'wood_edge',(xx,y,z+.28),(.145,.66,.54))
for side in [-1,1]:
    box(g,'iron',(x+side*.54,y,z+.27),(.065,.7,.55))
for yy in [y-.35,y+.35]:box(g,'iron',(x,yy,z+.07),(1.15,.035,.08))
for i in range(12):
    a=i*math.pi/12;b=(i+1)*math.pi/12
    vs=[(xx,y+.36*math.cos(t),z+.54+.28*math.sin(t)) for xx in [x-.56,x+.56] for t in [a,b]]
    geo(g,'wood' if i%3==0 else 'wood_edge',vs,[(0,2,3,1)])
for side in [-1,1]:
    xx=x+side*.561
    cap=[(xx,y,z+.54)]+[(xx,y+.36*math.cos(i*math.pi/16),z+.54+.28*math.sin(i*math.pi/16)) for i in range(17)]
    geo(g,'wood_edge',cap,[(0,i,i+1) if side>0 else (0,i+1,i) for i in range(1,17)])
for xx in [x-.48,x+.48]:
    line(g,'brass',[(xx,y+.373*math.cos(i*math.pi/24),z+.54+.292*math.sin(i*math.pi/24)) for i in range(25)],.031,6)
    for yy in [y-.37,y+.37]:
        box(g,'brass',(xx,yy,z+.30),(.06,.025,.46))
        for zz in [.13,.3,.49]:rock(g,(xx,yy-.021,z+zz),(.018,.018,.018),'iron',False)
box(g,'brass',(x,y-.385,z+.47),(.18,.035,.24))
ring(g,'iron',(x,y-.42,z+.39),.046,.01,axis='X',n=14)
for xx in [x-.35,x+.35]:box(g,'iron',(xx,y+.37,z+.54),(.16,.08,.07))

# Northeast cave, open mouth and genuinely recessed short tunnel.
g='08 Cave • rock mouth and tunnel';cx=5.65;cy=5.55
vs=[]
for yy in [cy-.22,cy+2.6]:
    for i in range(21):
        a=i*math.pi/20;vs.append((cx+1.12*math.cos(a),yy,.28+2.02*math.sin(a)))
geo(g,'cliff',vs,[(i,i+1,i+22,i+21) for i in range(20)])
cap=[(cx,cy+2.6,.28)]+[(cx+1.12*math.cos(i*math.pi/20),cy+2.6,.28+2.02*math.sin(i*math.pi/20)) for i in range(21)]
geo(g,'ink',cap,[(0,i,i+1) for i in range(1,21)])
for i in range(11):
    a=i*math.pi/10
    rock(g,(cx+1.57*math.cos(a),cy,.25+2.28*math.sin(a)),(.52,.77,.58),moss=True)
for i in range(28):
    x=random.uniform(3.1,8.7);y=random.uniform(6.7,8.9)
    rock(g,(x,y,.8),(random.uniform(.55,1.05),random.uniform(.5,.95),random.uniform(.6,1.5)))
for i in range(5):slab(g,cx,cy-1.67+i*.31,.17+i*.028,1.65,.30,.13,'stone_light')
for x in [cx-1.48,cx+1.48]:lantern(g,x,cy-.69,1.38,.8)
pointlight(g,'Cave • inner warm depth',(cx,cy+.7,1.08),(1,.35,.065),24,.38)
crate('09 Cave • entrance supplies',cx+1.73,cy-.92,.15,.5)
barrel('09 Cave • entrance supplies',cx+1.75,cy-.15,.18,.68)

# Editable gameplay-position markers are not visible in renders and have no behavior.
for name,loc in [('Spawn',(0,-9.25,.25)),('Chest',(-5.2,4.25,.68)),('Cave',(5.65,4.1,.24)),('Herb',(-5.5,-2.2,.17)),('Enemy',(4.75,-2.15,.15)),('Rest',(0,0,.15))]:
    o=bpy.data.objects.new('Anchor_'+name,None);coll('80 Anchors • layout only').objects.link(o);o.location=loc
    o.empty_display_type='PLAIN_AXES';o.empty_display_size=.3;o.hide_render=True
flush()
print('LANDMARKS_READY',len(scene.objects),flush=True)
