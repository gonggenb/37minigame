"""Original editable tutorial environment based on the supplied concept board."""
import bpy, bmesh, math, random, os, json
from mathutils import Vector
from pathlib import Path
random.seed(20260908)
OUT=Path('/Users/gongyuyang/Documents/37minigame/ArtSource/Blender/TutorialRestStop')
OUT.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes.new('Level 01 | 山脚古道的小歇脚地')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
scene.unit_settings.length_unit='METERS'
M={};B={};C={}

def mat(name,color,rough=.8,scale=0,stretch=(1,1,1),metal=0,emission=0):
    m=bpy.data.materials.new('RestStop • '+name);m.diffuse_color=(*color,1);m.use_nodes=True
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
mat('loam',(.20,.22,.13),scale=3)
mat('stone',(.30,.35,.34),scale=4)
mat('stone_light',(.43,.43,.34),scale=6)
mat('cliff',(.23,.28,.28),scale=2.4)
mat('moss',(.12,.18,.055),scale=9)
mat('wood',(.20,.105,.046),scale=6,stretch=(2,2,.12))
mat('wood_edge',(.35,.23,.115),scale=5,stretch=(2,.15,2))
mat('bark',(.18,.14,.082),scale=9,stretch=(1,1,.14))
mat('roof',(.065,.10,.125),rough=.64,scale=9)
mat('roof_alt',(.12,.17,.19),rough=.67,scale=7)
mat('iron',(.05,.065,.059),rough=.49,metal=.55)
mat('brass',(.43,.28,.09),rough=.4,metal=.65)
mat('rope',(.38,.30,.15),scale=14)
mat('cloth',(.35,.32,.24),scale=21)
mat('red_cloth',(.30,.045,.02),scale=16)
mat('ink',(.018,.022,.015))
mat('paper',(.92,.47,.11),rough=.5,emission=3.5)
mat('flame',(.99,.23,.018),emission=5)
mat('flame_core',(1,.63,.13),emission=7)
mat('bamboo',(.13,.24,.067),scale=5,stretch=(2,2,.1))
mat('bamboo_node',(.27,.34,.12),rough=.78)
for i,c in enumerate([(.10,.18,.06),(.16,.25,.075),(.23,.29,.09),(.12,.22,.14)]):mat('leaf'+str(i),c,scale=26)
mat('fallen_leaf',(.27,.22,.095),rough=.95)
mat('herb_leaf',(.18,.37,.16),rough=.75)
mat('berry',(.64,.05,.02),rough=.55)
mat('flower',(.81,.82,.58),rough=.8)
mat('water',(.028,.16,.18),rough=.21,scale=5)
mat('foam',(.43,.64,.62),rough=.55)
mat('pottery',(.13,.19,.18),rough=.48,scale=8)
mat('backdrop',(.024,.045,.057),rough=1)

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
bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=3,radius=1)
bm.verts.ensure_lookup_table();IV=[v.co.copy() for v in bm.verts];IF=[tuple(v.index for v in f.verts) for f in bm.faces];bm.free()
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
def ground(x,y):
    return .12+.035*math.sin(x*.9)*math.cos(y*.6)
def stream_y(x):return -7.8+.25*math.sin(x*.65)
def surface(x,y):
    h=ground(x,y);d=abs(y-stream_y(x))
    return h-max(0,1-d/1.4)*.85

# Playable clearing surrounded by real ground extending beyond the 18 x 18 zone.
vs=[];fs=[];N=121
for j in range(N):
    y=-24+j*.4
    for i in range(N):
        x=-24+i*.4;vs.append((x,y,surface(x,y)))
for j in range(N-1):
    for i in range(N-1):k=j*N+i;fs.append((k,k+1,k+1+N,k+N))
geo('01 Terrain • continuous ground','loam',vs,fs)

# Southern stream remains outside the ring; only the short entry bridge crosses it.
vs=[]
for i in range(121):
    x=-12+i*.2;y=stream_y(x)
    vs.extend([(x,y-.78,-.29),(x,y+.78,-.29)])
geo('02 Stream • entry water','water',vs,[(2*i,2*i+2,2*i+3,2*i+1) for i in range(120)])
for _ in range(110):
    x=random.uniform(-11.8,11.8)
    if abs(x)<1.5:continue
    y=stream_y(x)+random.choice([-1,1])*random.uniform(.78,1.25)
    r=random.uniform(.16,.53)
    rock('02 Stream • bank rocks',(x,y,surface(x,y)+.05),(r,r*.8,r*.75))
for _ in range(100):
    x=random.uniform(-11.5,11.5);y=stream_y(x)+random.uniform(-.6,.6)
    line('02 Stream • soft ripples','foam',[(x+k*.06,y+.022*math.sin(k),-.275) for k in range(random.randint(3,9))],.006,4)

# Central ring and four freely selectable spokes from the supplied board.
routes=[([(0,-6.1),(0,-4.4),(0,-2.8)],2.1),
        ([(-2.25,1.3),(-3.4,2.9),(-5,4.7)],1.8),
        ([(2.1,1.4),(3.5,3.3),(5.65,4.9)],1.7),
        ([(-2.7,-.8),(-4,-1.6),(-5.5,-2.2)],1.65),
        ([(2.65,-.9),(4.1,-2.0),(4.9,-2.2)],1.85)]
def dist_seg(x,y,a,b):
    dx,dy=b[0]-a[0],b[1]-a[1];t=max(0,min(1,((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy)))
    return math.hypot(x-a[0]-dx*t,y-a[1]-dy*t)
def path_distance(x,y):
    d=abs(math.hypot(x,y)-2.9)-.98
    for pts,w in routes:
        for a,b in zip(pts,pts[1:]):d=min(d,dist_seg(x,y,a,b)-w/2)
    return d
# Dense but irregular paving avoids the perfectly repeated square-grid look.
for j in range(44):
    y=-6.3+j*.31
    for i in range(53):
        x=-8+i*.31+(j%2)*.155
        if path_distance(x,y)>.05:continue
        if random.random()<.04:continue
        slab('03 Paths • hand laid flagstones',x+random.uniform(-.02,.02),y+random.uniform(-.02,.02),ground(x,y)+.055,
             random.uniform(.25,.30),random.uniform(.25,.30),.09,random.choice(['stone_light','stone','stone_light']),random.uniform(-.12,.12))
# Low circular edging around the rest nook, entry breaks allow access to seating.
for i in range(38):
    a=i*math.tau/38
    if -2.25<a-math.tau<-1.15:continue
    x=1.74*math.cos(a);y=1.74*math.sin(a)
    slab('04 Rest nook • low stone edging',x,y,.28,.32,.25,.19,'stone',a)
flush()
scene['playable_size_m']='18 x 18';scene['north_axis']='+Y';scene['up_axis']='+Z'
scene['reference']='33283148-7eca-40e2-988a-ffc77885bc3a.png'
print('FOUNDATION_READY',len(scene.objects),flush=True)
