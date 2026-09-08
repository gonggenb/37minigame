"""Create a separate HD-2D lighting/model revision from the existing mother scene.

Run with Blender --background BambooValley.blend --python this_file.py.
Never writes the source blend or Unity assets. Procedural materials are Blender-only.
"""
import bpy, math, random, json, os
from mathutils import Vector
from pathlib import Path

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'HD2D_v02'
OUT.mkdir(exist_ok=True)
random.seed(220908)
scene = bpy.data.scenes['Bamboo Valley | 竹影幽谷']
bpy.context.window.scene = scene
scene.name = 'Bamboo Valley HD2D v02'

# Reuse the authored geometry helpers, without executing the scene generator.
src = (ROOT / '01_foundation.py').read_text()
ns = dict(bpy=bpy, math=math, random=random, Vector=Vector, scene=scene)
import bmesh
ns.update(bmesh=bmesh, M={m.name:m for m in bpy.data.materials}, B={},
          collections={c.name:c for c in scene.collection.children})
for obj in scene.objects:
    if obj.type=='MESH' and ' / ' in obj.name and len(obj.data.materials):
        ns['M'][obj.name.split(' / ',1)[1]]=obj.data.materials[0]
exec(src[src.index('def coll(name):'):src.index("g='01 Terrain")], ns)
box, rock, geo, flush = (ns[n] for n in ['box','rock','geo','flush'])
ground, riverx = ns['ground'], ns['riverx']

# World-space material scale avoids stretching noise across merged geometry.
for mat in bpy.data.materials:
    if not mat.use_nodes:
        continue
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    p = next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None)
    if p is None:
        continue
    noises = [n for n in nodes if n.type=='TEX_NOISE']
    if noises:
        coord = nodes.new('ShaderNodeNewGeometry')
        coord.label = 'Meter-scaled detail for merged modular meshes'
        for noise in noises:
            links.new(coord.outputs['Position'],noise.inputs['Vector'])
            noise.inputs['Scale'].default_value = 2.5 if 'Rock' in mat.name else 4
    if 'Stone' in mat.name or 'Rock' in mat.name:
        p.inputs['Roughness'].default_value=.86
        for n in nodes:
            if n.type=='BUMP':
                n.inputs['Distance'].default_value=.055
                n.inputs['Strength'].default_value=.3
    if 'Wood' in mat.name:
        mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY'
        mapping.inputs[1].default_value=(2.2,2.2,.16)
        coord=nodes.new('ShaderNodeNewGeometry')
        links.new(coord.outputs['Position'],mapping.inputs[0])
        for n in noises:
            links.new(mapping.outputs[0],n.inputs['Vector'])
        p.inputs['Roughness'].default_value=.76
    if 'Roof' in mat.name:
        p.inputs['Roughness'].default_value=.62
    if 'Lantern' in mat.name:
        p.inputs['Emission Color'].default_value=(1,.48,.13,1)
        p.inputs['Emission Strength'].default_value=5
    if 'Water' in mat.name:
        p.inputs['Metallic'].default_value=.05
        p.inputs['Roughness'].default_value=.26
        p.inputs['Coat Weight'].default_value=.3
        for n in nodes:
            if n.type=='BUMP':
                n.inputs['Distance'].default_value=.035
                n.inputs['Strength'].default_value=.2

# Beveled slab silhouettes catch warm light; mesh-level authored layout is unchanged.
for obj in scene.objects:
    if obj.type=='MESH' and obj.name.startswith('05 Routes'):
        bevel=obj.modifiers.new('Worn stone corners • 25 mm','BEVEL')
        bevel.width=.025;bevel.segments=2
    if obj.type=='MESH' and obj.name.startswith(('06 Main bridge','07 Camp bridge')):
        bevel=obj.modifiers.new('Cedar edge wear • 12 mm','BEVEL')
        bevel.width=.012;bevel.segments=1

# Sparse fragments at path shoulders, leaving the whole walking strip clear.
routes=[([(-20,-16),(-17,-12),(-14,-8),(-11,-6)],2.7),
        ([(-4.5,-6),(0,-5),(3,-2),(3,4),(1,8)],3.2),
        ([(3,3),(8,6),(12,7),(15,10)],2.8),
        ([(5,-3),(10,-6),(16,-7),(20,-3)],2.1),
        ([(-14,-7),(-16,0),(-15,6),(-15,11)],2.2)]
for pts,width in routes:
    for a,b in zip(pts,pts[1:]):
        ang=math.atan2(b[1]-a[1],b[0]-a[0])
        for _ in range(int(math.dist(a,b)*3)):
            t=random.random(); side=random.choice([-1,1])
            off=side*(width*.5+random.uniform(.18,.65))
            x=a[0]+(b[0]-a[0])*t-math.sin(ang)*off
            y=a[1]+(b[1]-a[1])*t+math.cos(ang)*off
            if abs(x-riverx(y))<2.6:
                continue
            r=random.uniform(.055,.19)
            rock('21 HD2D • path shoulder chips',(x,y,ground(x,y)+r*.3),
                 (r,r*.8,r*.5),'Stone • blue limestone',moss=True)

# Fine fallen leaves use a single lightweight mesh and never cover interaction points.
leafmat=bpy.data.materials.new('HD2D • dry bamboo litter')
leafmat.diffuse_color=(.27,.20,.085,1);leafmat.use_nodes=True
lp=next(n for n in leafmat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
lp.inputs['Base Color'].default_value=leafmat.diffuse_color
lp.inputs['Roughness'].default_value=.95
ns['M'][leafmat.name]=leafmat
for _ in range(1300):
    x=random.uniform(-23,23);y=random.uniform(-20,20)
    if abs(x-riverx(y))<2.8 or not ns['inside'](x,y):
        continue
    if any(math.hypot(x-cx,y-cy)<r for cx,cy,r in [(0,-2,3.7),(0,12,6),(15,12,6),(20,-1,3.5)]):
        continue
    z=ground(x,y)+.024;a=random.random()*math.tau;l=random.uniform(.06,.16);w=l*.23
    ux,uy=math.cos(a),math.sin(a)
    vs=[(x-ux*l,y-uy*l,z),(x-uy*w,y+ux*w,z+.01),
        (x+ux*l,y+uy*l,z+.018),(x+uy*w,y-ux*w,z)]
    geo('22 HD2D • bamboo leaf litter',leafmat.name,vs,[(0,1,2,3)])

# Warm translucent-looking window panels behind the existing timber lattice.
window=bpy.data.materials.new('HD2D • warm paper windows');window.use_nodes=True
wp=next(n for n in window.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
wp.inputs['Base Color'].default_value=(.64,.29,.08,1)
wp.inputs['Emission Color'].default_value=(1,.36,.065,1)
wp.inputs['Emission Strength'].default_value=1.8
ns['M'][window.name]=window
for x in [-3.15,3.15]:
    box('23 HD2D • lit hall windows',window.name,(x,9.018,2.6),(1.47,.015,1.61))
flush()

lighting=bpy.data.collections.new('93 HD2D • atmospheric lighting')
scene.collection.children.link(lighting)
def lamp(name,kind,loc,color,power,size,target=None):
    d=bpy.data.lights.new(name,kind);d.energy=power;d.color=color
    if kind=='AREA':
        d.shape='DISK';d.size=size
    else:
        d.shadow_soft_size=size
    o=bpy.data.objects.new(name,d);lighting.objects.link(o);o.location=loc
    if target is not None:
        o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o

for obj in scene.objects:
    if obj.type!='LIGHT':continue
    d=obj.data
    if d.type=='SUN':
        d.energy=.72;d.color=(.78,.86,1);d.angle=.11
    elif obj.name.startswith('Moon'):
        d.energy=3000;d.color=(.43,.64,1);d.size=22
    elif obj.name.startswith('Evening'):
        d.energy=1550;d.color=(1,.74,.43);d.size=15
    elif obj.name.startswith('Lantern glow'):
        d.energy=145;d.color=(1,.40,.10);d.shadow_soft_size=.42
    elif obj.name.startswith('Campfire'):
        d.energy=720;d.color=(1,.25,.045)
bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs['Color'].default_value=(.075,.12,.19,1)
bg.inputs['Strength'].default_value=.24
lamp('HD2D • pavilion warm interior','AREA',(0,-2,3.7),(1,.52,.18),180,2,(0,-2,.8))
for x in [-3.15,3.15]:
    lamp('HD2D • window spill','AREA',(x,8.8,2.6),(1,.47,.14),45,1.2,(x,6.8,.7))
lamp('HD2D • cave invitation','POINT',(20,-.5,1.4),(1,.36,.08),100,.55)
lamp('HD2D • pale canopy rim','AREA',(-18,17,21),(.66,.79,1),2300,9,(0,2,0))

# True participating atmosphere, with denser wisps high in the rear canopy.
fogmat=bpy.data.materials.new('HD2D • subtle spatial mist');fogmat.use_nodes=True
n=fogmat.node_tree.nodes;n.clear();lk=fogmat.node_tree.links
out=n.new('ShaderNodeOutputMaterial');v=n.new('ShaderNodeVolumePrincipled')
v.inputs['Color'].default_value=(.49,.61,.70,1);v.inputs['Anisotropy'].default_value=.25
tex=n.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=.12;tex.inputs['Detail'].default_value=2
pos=n.new('ShaderNodeNewGeometry');lk.new(pos.outputs['Position'],tex.inputs['Vector'])
mul=n.new('ShaderNodeMath');mul.operation='MULTIPLY_ADD'
mul.inputs[1].default_value=.012;mul.inputs[2].default_value=.0015
lk.new(tex.outputs['Fac'],mul.inputs[0]);lk.new(mul.outputs[0],v.inputs['Density'])
lk.new(v.outputs['Volume'],out.inputs['Volume'])
bpy.ops.mesh.primitive_cube_add(size=2,location=(0,4,8))
fog=bpy.context.object;fog.name='HD2D • canopy mist volume';fog.scale=(32,30,12)
for c in list(fog.users_collection):c.objects.unlink(fog)
lighting.objects.link(fog);fog.data.materials.append(fogmat);fog.display_type='WIRE'
fog['export_to_unity']=False

# Independent presentation camera; overview and top views remain available for editing.
cams=bpy.data.collections['90 Cameras']
def camera(name,loc,target,lens):
    d=bpy.data.cameras.new(name);d.type='PERSP';d.lens=lens;d.clip_end=300
    o=bpy.data.objects.new(name,d);cams.objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    focus=bpy.data.objects.new(name+' focus',None);cams.objects.link(focus);focus.location=target
    d.dof.use_dof=True;d.dof.focus_object=focus;d.dof.aperture_fstop=7.1
    return o
hero=camera('CAM 04 • HD2D bridge courtyard',(19,-33,22),(-1,1.5,2),46)
scene.camera=hero
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.cycles.max_bounces=7;scene.cycles.volume_bounces=1
prefs=bpy.context.preferences.addons['cycles'].preferences
try:
    prefs.compute_device_type='METAL';prefs.get_devices()
    for d in prefs.devices:d.use=d.type=='METAL'
    scene.cycles.device='GPU'
except Exception:
    scene.cycles.device='CPU'
scene.render.resolution_x=1600;scene.render.resolution_y=1000
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(OUT/'BambooValley_HD2D_Hero.png')
scene.view_settings.view_transform='AgX';scene.view_settings.exposure=.3

# Blender 5 compositor. Glare is limited to emissive lamps and windows.
group=bpy.data.node_groups.new('HD2D • restrained amber halation','CompositorNodeTree')
group.interface.new_socket(name='Image',in_out='OUTPUT',socket_type='NodeSocketColor')
scene.compositing_node_group=group
rl=group.nodes.new('CompositorNodeRLayers');rl.scene=scene
gl=group.nodes.new('CompositorNodeGlare')
gl.inputs['Type'].default_value='Fog Glow'
gl.inputs['Quality'].default_value='High'
if 'Threshold' in gl.inputs:gl.inputs['Threshold'].default_value=1.5
if 'Strength' in gl.inputs:gl.inputs['Strength'].default_value=.22
if 'Size' in gl.inputs:gl.inputs['Size'].default_value=.25
output=group.nodes.new('NodeGroupOutput')
group.links.new(rl.outputs['Image'],gl.inputs['Image']);group.links.new(gl.outputs['Image'],output.inputs['Image'])

scene['revision']='v02: weathered paths, leaf litter, paper windows, cool/warm HD2D lighting, spatial mist, focused camera'
scene['purpose']='Editable Blender environment revision; original Unity level and timing unchanged'
scene['reference_image']='abd40112-e408-4289-8cfb-bc5ed03d6a52.png'
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.shading.type='MATERIAL'
        area.spaces.active.shading.use_scene_world=True
        area.spaces.active.shading.use_scene_lights=True
        area.spaces.active.overlay.show_overlays=False
        area.spaces.active.region_3d.view_camera_zoom=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BambooValley_HD2D_v02.blend'))
stats={'source':str(ROOT/'BambooValley.blend'),'output':bpy.data.filepath,
       'mesh_objects':sum(o.type=='MESH' for o in scene.objects),
       'polygons':sum(len(o.data.polygons) for o in scene.objects if o.type=='MESH'),
       'lights':sum(o.type=='LIGHT' for o in scene.objects),
       'units':'meters','bounds':[50,44],
       'unity_integrated':False,'validation':'Pending render inspection'}
(OUT/'manifest.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2))
print('HD2D_V02_SAVED',json.dumps(stats),flush=True)
if os.environ.get('BAMBOO_RENDER','1')=='1':
    bpy.ops.render.render(write_still=True,scene=scene.name)
    print('HD2D_HERO_RENDERED',flush=True)
