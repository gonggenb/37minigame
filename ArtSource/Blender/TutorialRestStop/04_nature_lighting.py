def blocked(x,y):
    if path_distance(x,y)<.43:return True
    for cx,cy,rx,ry in [(0,0,3.7,3.7),(-5.2,5,3.0,2.7),(5.65,5.55,2.7,2.6),(-5.55,-2.1,1.4,1.2),(6.2,.3,2.8,2.4),(6.5,-3,2.2,2), (0,-7.9,1.9,2.9)]:
        if ((x-cx)/rx)**2+((y-cy)/ry)**2<1:return True
    return False

# Continuous rocky wooded enclosure, lower in foreground to preserve visibility.
for i in range(86):
    a=i*math.tau/86
    x=10.1*math.copysign(abs(math.cos(a))**.62,math.cos(a))
    y=9.9*math.copysign(abs(math.sin(a))**.62,math.sin(a))+.6
    if y<-6.2 and abs(x)<2:continue
    height=random.uniform(.6,1.5)*( .6 if y<-5 else 1.0)
    rock('20 Nature • perimeter rock shelves',(x,y,.35),(random.uniform(.7,1.25),random.uniform(.6,1.2),height))
for x,y,s in [(-7.7,1.6,.7),(-3.4,6.7,.6),(2.1,7.1,.9),(8.9,2.7,.8),(-7,-.2,.5),(-3.3,-2.8,.35)]:
    rock('20 Nature • interior moss shelves',(x,y,.25),(s,s*.8,s*.8))

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

culms=0
for j in range(230):
    x=random.uniform(-11.6,11.6);y=random.uniform(-10.5,12)
    if abs(y-stream_y(x))<1.25 or blocked(x,y):continue
    if abs(x)<8 and y<8 and random.random()<.65:continue
    h=random.uniform(3.4,6.3)
    if y<-5:h*=.58
    group='21 Bamboo • '+('rear' if y>6 else 'left' if x<0 else 'right')
    for k in range(random.randint(2,4)):
        xx=x+random.uniform(-.2,.2);yy=y+random.uniform(-.2,.2)
        if blocked(xx,yy):continue
        bamboo(group,xx,yy,h*random.uniform(.7,1.05));culms+=1

# Sparse grass, miniature ferns and ground litter stop before the walking strip.
for j in range(1350):
    x=random.uniform(-11,11);y=random.uniform(-10.2,11.3)
    if abs(y-stream_y(x))<1.0 or path_distance(x,y)<.07:continue
    if any(((x-cx)/rx)**2+((y-cy)/ry)**2<1 for cx,cy,rx,ry in [(0,0,1.5,1.5),(-5.2,5,2.3,1.95),(5.65,5.6,1.2,1.7),(6.2,.5,1.5,1.6),(5.8,-2.5,.7,.7)]):continue
    z=ground(x,y)+.01
    for k in range(random.randint(3,6)):
        a=random.random()*math.tau;ll=random.uniform(.08,.28)
        leaf('22 Groundcover • grasses and ferns','leaf'+str(random.randrange(4)),(x,y,z),(x+ll*math.cos(a),y+ll*math.sin(a),z+ll*.7),.023)
    if j%6==0:
        r=random.uniform(.07,.23);rock('22 Groundcover • pebbles',(x,y,z),(r,r*.78,r*.47),'stone',True)
    for k in range(3):
        a=random.random()*math.tau;ll=random.uniform(.05,.10)
        leaf('22 Groundcover • fallen bamboo leaves','fallen_leaf',(x,y,z),(x+ll*math.cos(a),y+ll*math.sin(a),z+.008),.009)
flush()

# Small bevels preserve visible manufactured edges without smoothing rocks or leaves.
for ob in scene.objects:
    if ob.type=='MESH' and ob.name.startswith(('03 Paths','05 Entry','06 Reward','07 Reward','12 Rest','17 Camp')):
        if any(s in ob.name for s in ['/ stone','/ wood','/ brass','/ iron']):
            b=ob.modifiers.new('Soft worn edges','BEVEL');b.width=.009;b.segments=2

# Pack the exact user reference; no text in that board controls the script.
ref=bpy.data.images.load('/Users/gongyuyang/Downloads/33283148-7eca-40e2-988a-ffc77885bc3a.png',check_existing=True)
ref.name='REFERENCE • Level 01 supplied concept';ref.use_fake_user=True;ref.pack()
font.pack()

def area(name,p,target,color,energy,size):
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.color=color;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);coll('90 Lighting • dusk and lanterns').objects.link(o);o.location=p
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o
world=bpy.data.worlds.new('RestStop • blue dusk');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs['Color'].default_value=(.12,.18,.25,1);bg.inputs['Strength'].default_value=.20;scene.world=world
area('Cool skylight • broad fill',(0,6,17),(0,0,0),(.57,.72,1),750,15)
area('Warm rim • through western canopy',(-11,-3,12),(0,1,0),(1,.75,.44),700,9)
area('Rear canopy rim',(2,12,13),(0,1,2),(.62,.78,1),950,8)
area('Pavilion • warm interior',(-5.2,5,3.25),(-5.2,4.5,.7),(1,.5,.15),58,1.8)
sun=bpy.data.lights.new('Moonlit canopy','SUN');sun.energy=.32;sun.angle=.12;sun.color=(.68,.81,1)
ob=bpy.data.objects.new('Moonlit canopy',sun);coll('90 Lighting • dusk and lanterns').objects.link(ob);ob.rotation_euler=(.5,-.45,-.4)

# Real thin atmosphere. Kept behind the playable ground so paths remain clear.
fogmat=bpy.data.materials.new('RestStop • rear canopy mist');fogmat.use_nodes=True
nn=fogmat.node_tree.nodes;nn.clear();lk=fogmat.node_tree.links
output=nn.new('ShaderNodeOutputMaterial');vol=nn.new('ShaderNodeVolumePrincipled')
vol.inputs['Color'].default_value=(.57,.66,.73,1);vol.inputs['Density'].default_value=.008
vol.inputs['Anisotropy'].default_value=.25;lk.new(vol.outputs['Volume'],output.inputs['Volume'])
bpy.ops.mesh.primitive_cube_add(size=2,location=(0,9,5))
fog=bpy.context.object;fog.name='Atmosphere • rear mist only';fog.scale=(15,5,7)
for c in list(fog.users_collection):c.objects.unlink(fog)
coll('91 Atmosphere • exclude from mesh export').objects.link(fog);fog.data.materials.append(fogmat);fog.display_type='WIRE'
fog['asset_role']='render_only_volume';fog['export_to_unity']=False

# Long lens gameplay-like view plus true orthographic plan and optional close view.
def camera(name,loc,target,lens=45,ortho=None):
    d=bpy.data.cameras.new(name);d.lens=lens;d.clip_end=300
    o=bpy.data.objects.new(name,d);coll('92 Cameras').objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    if ortho:d.type='ORTHO';d.ortho_scale=ortho
    else:
        f=bpy.data.objects.new(name+' focus',None);coll('92 Cameras').objects.link(f);f.location=target
        d.dof.use_dof=True;d.dof.focus_object=f;d.dof.aperture_fstop=8
    return o
hero=camera('CAM 01 • whole rest stop',(13.8,-24.5,20),(0,.8,1.4),48)
top=camera('CAM 02 • plan',(0,0,40),(0,0,0),ortho=25)
detail=camera('CAM 03 • tea pine and pavilion',(8,-14,10),(-.7,1.0,1.7),44)
scene.camera=hero
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.cycles.max_bounces=6;scene.cycles.volume_bounces=1
scene.render.resolution_x=1600;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'RestStop_Overview.png')
scene.view_settings.view_transform='AgX';scene.view_settings.exposure=.45

# Glare socket API for Blender 5.2; subtle highlight halation, no full-scene blur.
cg=bpy.data.node_groups.new('RestStop • HD2D warm halation','CompositorNodeTree')
cg.interface.new_socket(name='Image',in_out='OUTPUT',socket_type='NodeSocketColor')
scene.compositing_node_group=cg
rl=cg.nodes.new('CompositorNodeRLayers');rl.scene=scene
gl=cg.nodes.new('CompositorNodeGlare');gl.inputs['Type'].default_value='Fog Glow';gl.inputs['Quality'].default_value='High'
gl.inputs['Threshold'].default_value=1.6;gl.inputs['Strength'].default_value=.18;gl.inputs['Size'].default_value=.22
go=cg.nodes.new('NodeGroupOutput');cg.links.new(rl.outputs['Image'],gl.inputs['Image']);cg.links.new(gl.outputs['Image'],go.inputs['Image'])

for area_ in bpy.context.screen.areas:
    if area_.type=='VIEW_3D':
        sp=area_.spaces.active;sp.region_3d.view_perspective='CAMERA';sp.region_3d.view_camera_zoom=0
        sp.shading.type='MATERIAL';sp.shading.use_scene_world=True;sp.shading.use_scene_lights=True;sp.overlay.show_overlays=False
scene['bamboo_culms']=culms
scene['purpose']='Blender environment art; new reference layout, no changes to Unity gameplay or timing'
scene['landmarks']='NW pavilion/chest; NE cave; SW herb; SE camp/enemy; central old pine; south entry bridge'
scene['status']='Modeled; pending actual render inspection'
polish_path=OUT/'05_refinement.py'
exec(compile(polish_path.read_text(),str(polish_path),'exec'),globals())
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'TutorialRestStop_v01.blend'))
stats={'file':bpy.data.filepath,'scene':scene.name,'objects':len(scene.objects),
       'mesh_objects':sum(o.type=='MESH' for o in scene.objects),
       'polygons':sum(len(o.data.polygons) for o in scene.objects if o.type=='MESH'),
       'bamboo_culms':culms,'lights':sum(o.type=='LIGHT' for o in scene.objects),
       'playable_size_m':[18,18],'units':'meters','north':'+Y','status':scene['status'],
       'unity_imported':False,'original_user_scene_preserved':True}
(OUT/'manifest.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2))
print('RESTSTOP_SAVED',json.dumps(stats,ensure_ascii=False),flush=True)
