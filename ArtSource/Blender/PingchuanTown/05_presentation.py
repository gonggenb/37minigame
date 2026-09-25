# Daylight/amber balance follows the supplied reference sheets.
world=bpy.data.worlds.new('Pingchuan / cool mountain daylight');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.38,.50,.63,1);bg.inputs['Strength'].default_value=.35;scene.world=world
sun=bpy.data.lights.new('Afternoon sunlight','SUN');sun.energy=2.5;sun.angle=.13;sun.color=(1,.83,.61)
o=bpy.data.objects.new('Afternoon sunlight',sun);coll('90 Lighting').objects.link(o);o.rotation_euler=(math.radians(32),math.radians(-23),math.radians(-32))
area('Soft sky fill',(0,-40,130),(0,0,0),(.65,.79,1),36000,140)
area('Northern rim',(0,110,90),(0,0,8),(.73,.84,1),18000,110)
hero=camera('CAM 01 / full valley overview',(165,-254,212),(-3,9,3),49,ortho=285)
top=camera('CAM 02 / north-up plan',(0,0,300),(0,0,0),ortho=300)
towncam=camera('CAM 03 / town market street',(-9,-43,13),(-25,9,4.1),46)
cavecam=camera('CAM 04 / western cave group',(-49,13,30),(-80,58,3),43)
campcam=camera('CAM 05 / foothill garrison',(103,-119,40),(66,-65,2.6),46)
passcam=camera('CAM 06 / northern pass',(56,53,23),(27,93,4),45)
plaincam=camera('CAM 07 / open training plain',(72,-57,26),(35,-5,2.2),42)
entrycam=camera('CAM 08 / old entry road',(-64,-105,6.2),(-54,-83,3),32)
for o in [hero,top,towncam,cavecam,campcam,passcam,plaincam,entrycam]:o.data.dof.use_dof=False
scene.camera=hero
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.cycles.max_bounces=5;scene.cycles.volume_bounces=0
scene.render.resolution_x=1800;scene.render.resolution_y=1350;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'previews/01_Overview.png')
scene.view_settings.view_transform='AgX';scene.view_settings.exposure=.3
# Packed reference sheets are available in the Image Editor, not used as flat environment geometry.
refs=['4f373c87-db59-4800-a6a5-18e812bc650d','9ec9f29b-6c75-43f4-aecb-8ea3d2548ffc','251be341-08b0-4b9e-9cb5-78a5990c6daa','32523508-27e8-4435-9d64-04ace845aca4','c4951084-f729-4cd5-983e-1e3affb527b4','e3bde50b-fab5-4e05-a335-b809745bcb49']
for i,ref in enumerate(refs):
    im=bpy.data.images.load('/Users/gongyuyang/Downloads/'+ref+'.png',check_existing=True);im.name=f'Pingchuan reference {i+1:02d}';im.use_fake_user=True;im.pack()
font.pack()
# A restrained glow pass for lanterns. Geometry and scene colors remain unchanged.
cg=bpy.data.node_groups.new('Pingchuan / lantern bloom','CompositorNodeTree');cg.interface.new_socket(name='Image',in_out='OUTPUT',socket_type='NodeSocketColor');scene.compositing_node_group=cg
rl=cg.nodes.new('CompositorNodeRLayers');rl.scene=scene
gl=cg.nodes.new('CompositorNodeGlare');gl.inputs['Type'].default_value='Fog Glow';gl.inputs['Quality'].default_value='High';gl.inputs['Threshold'].default_value=2.;gl.inputs['Strength'].default_value=.14;gl.inputs['Size'].default_value=.18
go=cg.nodes.new('NodeGroupOutput');cg.links.new(rl.outputs['Image'],gl.inputs['Image']);cg.links.new(gl.outputs['Image'],go.inputs['Image'])
for ar in bpy.context.screen.areas:
    if ar.type=='VIEW_3D':
        sp=ar.spaces.active;sp.region_3d.view_perspective='CAMERA';sp.region_3d.view_camera_zoom=0
        sp.shading.type='MATERIAL';sp.shading.use_scene_world=True;sp.shading.use_scene_lights=True;sp.overlay.show_overlays=False
scene['purpose']='Editable Blender environment based on six supplied Pingchuan Town references. No Unity gameplay integration.'
scene['status']='Modeled; awaiting render inspection'
scene['regions']='Central market town; cave entrance network; southeast foothill camp; northern mountain pass; east training plain'
scene['source_reference_priority']='Sheet 01 overall arrangement; sheets 02-06 local architecture and props'
scene['water_crossings']=len(bridges);scene['trees']=tree_count;scene['bamboo_culms']=bamboo_count
scene['groundcover_tufts']=grass_count
scene['unity_imported']=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'PingchuanTown_v01.blend'))
print('PINGCHUAN SAVED',bpy.data.filepath,len(scene.objects),flush=True)
