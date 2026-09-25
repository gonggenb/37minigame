"""Render-driven polish: geological contours, material scale and close-range dressing."""
from mathutils import noise
random.seed(690916)
# Replace first-pass perimeter and distant silhouettes with dense eroded surfaces.
for ob in list(scene.objects):
    if any(s in ob.name for s in ['/ mountain cliffs /','/ inner outcrops /','79 Backdrop / distant mountain range']):
        bpy.data.objects.remove(ob,do_unlink=True)
# Rock shader: broad strata color plus finer mineral relief.
rockmat=M['cliff'];n=rockmat.node_tree.nodes;l=rockmat.node_tree.links
p=next(n for n in n if n.type=='BSDF_PRINCIPLED')
geom=n.new('ShaderNodeNewGeometry');no=n.new('ShaderNodeTexNoise');no.inputs['Scale'].default_value=.43;no.inputs['Detail'].default_value=5;no.inputs['Roughness'].default_value=.72
l.new(geom.outputs['Position'],no.inputs['Vector'])
ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.12,.16,.17,1);ramp.color_ramp.elements[1].color=(.45,.47,.43,1);l.new(no.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color'])
bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.55;bump.inputs['Distance'].default_value=.22;l.new(no.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],p.inputs['Normal'])

def erosion(u,v,seed):
    return noise.noise_vector(Vector((u*2.9+seed,v*2.9-seed,.7)))[0]
def mountain(g,x,y,sx,sy,h,seed,ma='cliff',detail=25):
    vs=[];N=detail
    for j in range(N+1):
        v=-1+2*j/N
        for i in range(N+1):
            u=-1+2*i/N;r=math.sqrt(u*u+v*v)
            e=erosion(u,v,seed);e2=erosion(u*3.1,v*3.1,seed+19)
            contour=max(0,1-r*(1+.12*e))
            hh=h*contour**1.05*(.9+.33*e+.11*e2)
            hh+=max(0,1-r)*h*.12*math.sin(u*11+v*4+e*3)
            vs.append((x+u*sx+.23*e,y+v*sy+.23*e2,-.45+hh))
    fs=[]
    for j in range(N):
        for i in range(N):
            k=j*(N+1)+i
            fs.extend([(k,k+1,k+N+2),(k,k+N+2,k+N+1)])
    geo(g,ma,vs,fs)
    if ma=='cliff':
        # Partial slope vegetation, not flat green summit caps.
        sel=[]
        for f in fs:
            center=sum((Vector(vs[k]) for k in f),Vector())/3
            if center.z>h*.18 and noise.noise_vector(center*.19)[0]>.18:sel.append(f)
        geo(g,'moss',[(xx,yy,zz+.025) for xx,yy,zz in vs],sel)
    return vs
# Broad overlapping masses give a continuous valley wall and an irregular skyline.
for layer in range(2):
    for i in range(60):
        a=i*math.tau/60+random.uniform(-.025,.025)
        x=(111+layer*15)*math.copysign(abs(math.cos(a))**.76,math.cos(a))
        y=8+(100+layer*14)*math.copysign(abs(math.sin(a))**.77,math.sin(a))
        if roaddist(x,y)<7:continue
        if any(math.hypot(x-cx,y-cy)<11 for cx,cy in [(-83,58),(-67,75),(0,77),(94,22),(-91,35)]):continue
        h=random.uniform(15,27)+(6 if y>55 else 0)+layer*5
        if y<-58:h*=.47
        vs=mountain(sector(x,y,'eroded mountain wall'),x,y,random.uniform(10,16),random.uniform(10,16),h,i+layer*90,detail=24)
        for j in range(2):
            pp=Vector(random.choice(vs));
            if pp.z<h*.28:continue
            r=random.uniform(1.8,3.4);rock(sector(x,y,'fractured crags'),tuple(pp),(r,r*.8,r*1.35),'cliff',True)
# Interior granite shelves.
for i,(x,y,rx,ry,h) in enumerate([(-67,10,8,7,7),(-63,53,6,8,10),(-14,70,6,8,7),(18,77,5,7,10),(81,47,10,9,11),(93,-20,7,9,8),(-93,-48,6,8,6),(9,32,5,7,6),(-59,-46,5,5,5),(93,-90,6,5,6)]):
    mountain(sector(x,y,'granite shelves'),x,y,rx,ry,h,400+i,detail=22)
for layer in range(3):
    for i in range(16):
        x=-211+i*29+random.uniform(-8,8);y=156+layer*33+random.uniform(-7,7)
        mountain('79 Backdrop / sculpted distant ridges',x,y,random.uniform(24,40),random.uniform(23,36),random.uniform(28,56)+layer*7,700+layer*50+i,f'distant_{layer}',detail=24)
# Larger meadow color variation on world space, plus fine blades already modeled.
n=M['loam'].node_tree.nodes;l=M['loam'].node_tree.links;p=next(n for n in n if n.type=='BSDF_PRINCIPLED')
geom=n.new('ShaderNodeNewGeometry');no=n.new('ShaderNodeTexNoise');no.inputs['Scale'].default_value=.16;no.inputs['Detail'].default_value=4
ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.17;ramp.color_ramp.elements[0].color=(.16,.22,.065,1);ramp.color_ramp.elements[1].position=.84;ramp.color_ramp.elements[1].color=(.40,.39,.15,1)
l.new(geom.outputs['Position'],no.inputs['Vector']);l.new(no.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color'])
# Readable warm paper windows, stronger small lantern highlights.
mat('window_paper',(.62,.40,.18),rough=.9,scale=22,emission=.18)
for ob in scene.objects:
    if ob.type=='MESH' and ob.name.startswith(('11 Town','12 Town','13 Town')) and ob.name.endswith('/ paper'):
        ob.data.materials.append(M['window_paper'])
        for f in ob.data.polygons:
            if f.area>.5:f.material_index=1
# Quietly reinforce close-range side facades with lower wooden paneling and beam joints.
for side in [-1,1]:
    for i in range(7):
        x=-25+side*11.8;y=-26+i*9.6
        def paneldetail(g):
            for sx in [-1,1]:
                for j in range(20):
                    box(g,'wood_edge',(sx*3.72,-2.82+j*.29,1.05),(.065,.27,1.12))
                for yy in [-2.6,0,2.6]:
                    rod(g,'wood',(sx*3.8,yy,3.0),(sx*3.8,yy+.6,3.5),.055,n=6)
        place(paneldetail,'17 Town / timber side panels',x,y,-side*math.pi/2)
# Town canals have short stone embankments and occasional water steps.
for yy in range(-27,35,2):
    for a,b in zip(canal,canal[1:]):
        if min(a[1],b[1])<=yy<max(a[1],b[1]):xx=a[0]+(b[0]-a[0])*(yy-a[1])/(b[1]-a[1]);break
    if any(math.hypot(xx-bx,yy-by)<3.2 for bx,by,_ in bridges):continue
    for side in [-1,1]:
        for k in range(3):
            slab('18 Town / canal embankment masonry',xx+side*1.9,yy,-.1+k*.25,.85,1.9,.26,'stone')
        if yy%6==1:
            box('18 Town / canal embankment masonry','stone_light',(xx+side*2,yy,.8),(.28,.28,1.1))
# Stitched canvas patches on the command tent: visible at camp camera distance.
for x,y in [(65,-56),(68,-54),(63.5,-55.2)]:
    zz=height(x,y)+5.3-abs((x-67)/4.5)*(5.3-.65)-.05
    geo('38 Camp / patched command canvas','rope',[(x-.34,y-.4,zz+.25),(x+.34,y-.4,zz-.25),(x+.34,y+.4,zz-.25),(x-.34,y+.4,zz+.25)],[(0,1,2,3)])
# Contextual scattered objects: tea sets, stacked timber, local garden plots.
for x,y in [(-44,-31),(-44,29),(-4,-30)]:
    place(table,'18 Town / tea courtyard furnishings',x,y,0,1.3,0,0,0,1.6,1.1)
    place(bench,'18 Town / tea courtyard furnishings',x,y-1.1,0,1.3,0,0,0,1.8)
    for k in range(3):place(pot,'18 Town / tea courtyard furnishings',x+(k-1)*.4,y,0,1,0,0,.95,.55)
for cx,cy in [(-65,-24),(-66,-9),(-62,31)]:
    if waterdist(cx,cy)<4:continue
    vs=[(cx-5,cy-3,height(cx,cy)+.03),(cx+5,cy-3,height(cx,cy)+.03),(cx+5,cy+3,height(cx,cy)+.03),(cx-5,cy+3,height(cx,cy)+.03)]
    geo('19 Town edge / herb gardens','dirt',vs,[(0,1,2,3)])
    for j in range(6):
        y=cy-2.3+j*.85
        line('19 Town edge / herb gardens','road',[(cx-4.6,y,height(cx,cy)+.06),(cx+4.6,y,height(cx,cy)+.06)],.15,6)
        for i in range(18):
            x=cx-4.4+i*.5
            for k in range(4):
                an=k*math.tau/4;leaf('19 Town edge / herb gardens','herb_leaf',(x,y,height(x,y)+.1),(x+.22*math.cos(an),y+.22*math.sin(an),height(x,y)+.38),.09)
    fence('19 Town edge / herb garden rails',[(cx-5.5,cy-3.5),(cx-5.5,cy+3.5),(cx+5.5,cy+3.5)])
# Natural blue sky gives real horizon variation in the street cameras.
n=world.node_tree.nodes;l=world.node_tree.links
sky=n.new('ShaderNodeTexSky');sky.sky_type='MULTIPLE_SCATTERING';sky.sun_elevation=.43;sky.sun_rotation=2.2;sky.altitude=.1;sky.air_density=1.1;sky.aerosol_density=2.0
bg.inputs['Color'].default_value=(.38,.50,.63,1);bg.inputs['Strength'].default_value=.35
# Camera views stay inside the valley. The overview crops distant set extensions.
towncam.location=(-25,-29,5.0);targetp=Vector((-25,26,3.5));towncam.rotation_euler=(targetp-towncam.location).to_track_quat('-Z','Y').to_euler();towncam.data.lens=29
hero.location=(148,-241,240);hero.rotation_euler=(Vector((-2,1,3))-hero.location).to_track_quat('-Z','Y').to_euler();hero.data.ortho_scale=274
flush()
# Smooth eroded rock shading, while keeping manufactured roofs and walls sharply legible.
for ob in scene.objects:
    if ob.type=='MESH' and any(t in ob.name for t in ['eroded mountain wall','sculpted distant ridges','granite shelves']):
        for p in ob.data.polygons:p.use_smooth=True
scene.camera=hero
scene['status']='Refined after first overview and street inspection; final view check pending'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'PingchuanTown_v01.blend'))
print('REFINEMENT SAVED',len(scene.objects),flush=True)
