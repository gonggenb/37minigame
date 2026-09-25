# Geometry helpers adapted from the project TutorialRestStop model.

def mat(name,color,rough=.8,scale=0,stretch=(1,1,1),metal=0,emission=0):
    m=bpy.data.materials.new('Pingchuan • '+name);m.diffuse_color=(*color,1);m.use_nodes=True
    n=m.node_tree.nodes;l=m.node_tree.links
    p=next(x for x in n if x.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    if emission:
        p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
    if scale:
        coord=n.new('ShaderNodeNewGeometry')
        mul=n.new('ShaderNodeVectorMath');mul.operation='MULTIPLY';mul.inputs[1].default_value=stretch
        noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=scale;noise.inputs['Detail'].default_value=3
        ramp=n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position=.18;ramp.color_ramp.elements[0].color=(*(c*.64 for c in color),1)
        ramp.color_ramp.elements[1].position=.82;ramp.color_ramp.elements[1].color=(*(min(1,c*1.18) for c in color),1)
        bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.27;bump.inputs['Distance'].default_value=.045
        l.new(coord.outputs['Position'],mul.inputs[0]);l.new(mul.outputs[0],noise.inputs['Vector'])
        l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color'])
        l.new(noise.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],p.inputs['Normal'])
    M[name]=m;return m

def coll(name):
    if name not in C:
        c=bpy.data.collections.new(name);scene.collection.children.link(c);C[name]=c
    return C[name]

def geo(g,m,vs,fs):
    v,f=B.setdefault((g,m),([],[]));offset=len(v)
    v.extend(tuple(p) for p in vs);f.extend(tuple(offset+i for i in face) for face in fs)

def box(g,m,p,s,angle=0):
    x,y,z=p;a,b,c=[v*.5 for v in s];co,si=math.cos(angle),math.sin(angle)
    vs=[(x+u*co-v*si,y+u*si+v*co,z+w) for u,v,w in [(-a,-b,-c),(a,-b,-c),(a,b,-c),(-a,b,-c),(-a,-b,c),(a,-b,c),(a,b,c),(-a,b,c)]]
    geo(g,m,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])

def rod(g,m,a,b,r,r2=None,n=8):
    a,b=Vector(a),Vector(b);w=(b-a).normalized();u=w.cross(Vector((0,0,1)))
    if u.length<.01:u=w.cross(Vector((0,1,0)))
    u.normalize();v=w.cross(u);r2=r if r2 is None else r2
    vs=[tuple(p+(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))*rr) for p,rr in [(a,r),(b,r2)] for i in range(n)]
    geo(g,m,vs,[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)])

def line(g,m,pts,r=.025,n=6):
    for a,b in zip(pts,pts[1:]):rod(g,m,a,b,r,n=n)

def tube(g,m,pts,radii,n=10):
    for i in range(len(pts)-1):rod(g,m,pts[i],pts[i+1],radii[i],radii[i+1],n)

def ring(g,m,center,r,tube_r=.025,axis='Z',n=24):
    x,y,z=center
    if axis=='X':pts=[(x,y+r*math.cos(i*math.tau/n),z+r*math.sin(i*math.tau/n)) for i in range(n+1)]
    else:pts=[(x+r*math.cos(i*math.tau/n),y+r*math.sin(i*math.tau/n),z) for i in range(n+1)]
    line(g,m,pts,tube_r)

def rock(g,p,s,m='cliff',moss=True):
    a=random.random()*math.tau;co,si=math.cos(a),math.sin(a)
    vs=[]
    for v in IV:
        k=random.uniform(.96,1.04);xx=v.x*s[0]*k;yy=v.y*s[1]*k
        vs.append((p[0]+co*xx-si*yy,p[1]+si*xx+co*yy,p[2]+v.z*s[2]*random.uniform(.95,1.05)))
    geo(g,m,vs,IF)
    if moss:
        fs=[f for f in IF if sum(IV[i].z for i in f)/len(f)>.48 and random.random()<.7]
        geo(g,'moss',[(x,y,z+.013) for x,y,z in vs],fs)

def slab(g,x,y,z,sx,sy,h=.11,m='stone_light',a=0):
    # Irregular eight-corner flagstone with clipped edges.
    corners=[(-.5,-.34),(-.35,-.5),(.34,-.5),(.5,-.34),(.5,.35),(.32,.5),(-.35,.5),(-.5,.33)]
    co,si=math.cos(a),math.sin(a);pts=[]
    for u,v in corners:
        u*=sx*random.uniform(.96,1.04);v*=sy*random.uniform(.96,1.04)
        pts.append((x+co*u-si*v,y+si*u+co*v))
    vs=[(xx,yy,zz) for zz in [z-h,z] for xx,yy in pts]
    geo(g,m,vs,[tuple(reversed(range(8))),tuple(range(8,16))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)])

def flush():
    for (g,m),(vs,fs) in B.items():
        if not vs:continue
        me=bpy.data.meshes.new(g+' / '+m);me.from_pydata(vs,[],fs);me.materials.append(M[m]);me.update()
        ob=bpy.data.objects.new(g+' / '+m,me);coll(g).objects.link(ob)
        # UV layer is usable as a starting point, procedural master uses world coordinates.
        uv=me.uv_layers.new(name='WorldPlanar')
        for poly in me.polygons:
            for li in poly.loop_indices:
                p=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(p.x*.25,p.y*.25)
        ob['asset_role']='environment_mesh'
    B.clear()

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
    # Continuous sheathing under individually modeled overlapping ceramic tiles.
    # This prevents light leaks at pan-tile seams in close views.
    for edge in range(4):
        vs=[tuple(c-(.038 if k==2 else 0) for k,c in enumerate(pt(edge,u,j/16))) for j in range(17) for u in [0,1]]
        geo(g,'roof',vs,[(2*j,2*j+1,2*j+3,2*j+2) for j in range(16)])
    for edge in range(4):
        width=math.dist(pt(edge,0,0),pt(edge,1,0));cols=max(8,int(width/.15));rows=13
        for row in range(rows):
            t0=row/rows*.98;t1=(row+1)/rows*.98+.008
            for col in range(cols):
                # Each overlapping roof tile has a curved pan and a real edge thickness.
                u0=(col+.018)/cols;u1=(col+.982)/cols
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

def bamboo(g,x,y,h):
    z=ground(x,y);lean=(random.uniform(-.33,.33),random.uniform(-.3,.3));r=random.uniform(.028,.052)
    steps=max(6,int(h/.46))
    for i in range(steps):
        t=i/steps;tt=(i+1)/steps
        a=(x+lean[0]*t,y+lean[1]*t,z+h*t);b=(x+lean[0]*tt,y+lean[1]*tt,z+h*tt)
        rod(g,'bamboo',a,b,r*(1-t*.55),r*(1-tt*.55),7)
        rod(g,'bamboo_node',(a[0],a[1],a[2]-.012),(a[0],a[1],a[2]+.012),r*(1-t*.55)*1.17,n=7)
    for j in range(4):
        t=random.uniform(.53,.95);a=Vector((x+lean[0]*t,y+lean[1]*t,z+h*t));an=random.random()*math.tau
        b=a+Vector((math.cos(an)*.65,math.sin(an)*.65,.14))
        rod(g,'bamboo',a,b,.011,.003,5)
        for k in range(4):
            p=a+(b-a)*(.22+k*.22)
            for side in [-1,1]:
                aa=an+side*.72;ll=random.uniform(.23,.43)
                leaf(g,'leaf'+str(random.randrange(4)),p,p+Vector((math.cos(aa)*ll,math.sin(aa)*ll,random.uniform(-.1,.08))),.038)

def area(name,p,target,color,energy,size):
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.color=color;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);coll('90 Lighting • dusk and lanterns').objects.link(o);o.location=p
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o

def camera(name,loc,target,lens=45,ortho=None):
    d=bpy.data.cameras.new(name);d.lens=lens;d.clip_end=1200
    o=bpy.data.objects.new(name,d);coll('92 Cameras').objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    if ortho:d.type='ORTHO';d.ortho_scale=ortho
    else:
        f=bpy.data.objects.new(name+' focus',None);coll('92 Cameras').objects.link(f);f.location=target
        d.dof.use_dof=True;d.dof.focus_object=f;d.dof.aperture_fstop=8
    return o
