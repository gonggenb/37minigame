def camera(name,loc,target,scale):
 d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=scale;d.lens=50;d.clip_end=500
 ob=bpy.data.objects.new(name,d);coll('90 Cameras').objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler();return ob
hero=camera('CAM 01 • Valley overview',(19,-58,52),(0,1,1.5),65)
top=camera('CAM 02 • Layout top',(0,0,70),(0,0,0),56)
detail=camera('CAM 03 • Bridge and pavilion',(17,-29,18),(-2,0,2),32)
scene.camera=hero
def area(name,loc,target,color,energy,size):
 d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.color=color;d.shape='DISK';d.size=size
 ob=bpy.data.objects.new(name,d);coll('91 Lighting').objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
area('Moon • broad cool fill',(-8,8,35),(0,0,0),(.51,.70,1),2800,25)
area('Evening • warm canopy edge',(-25,-15,25),(0,0,0),(1,.73,.40),3600,20)
d=bpy.data.lights.new('Sun • late valley light','SUN');d.energy=2.1;d.angle=.24;d.color=(1,.86,.64)
ob=bpy.data.objects.new('Sun • late valley light',d);coll('91 Lighting').objects.link(ob);ob.rotation_euler=(.42,-.52,-.45)
world=bpy.data.worlds.new('Valley dusk');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.11,.18,.22,1);bg.inputs['Strength'].default_value=.35;scene.world=world
box('92 Presentation • backdrop','Backdrop',(0,0,-3.7),(200,200,.3));flush()
scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=True
scene.render.resolution_x=1600;scene.render.resolution_y=1320;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=OUT+'/BambooValley_Overview.png'
scene.view_settings.view_transform='AgX'
refpath='/Users/gongyuyang/Downloads/abd40112-e408-4289-8cfb-bc5ed03d6a52.png'
if os.path.exists(refpath):
 img=bpy.data.images.load(refpath,check_existing=True);img.name='REFERENCE • supplied Bamboo Valley board';img.pack()
for f in bpy.data.fonts:
 if f.filepath and f.name!='Bfont':
  try:f.pack()
  except Exception:pass
for area_ in bpy.context.screen.areas:
 if area_.type=='VIEW_3D':
  area_.spaces.active.region_3d.view_perspective='CAMERA'
  area_.spaces.active.overlay.show_overlays=False
  area_.spaces.active.shading.type='MATERIAL'
  area_.spaces.active.shading.use_scene_world=False
  area_.spaces.active.region_3d.view_camera_zoom=0
scene['landmarks']='SW entrance; west stream bridges; center hexagonal pavilion; north martial hall; NW camp; NE boss arena; east cave'
stats={'scene':scene.name,'objects':len(scene.objects),'mesh_objects':sum(o.type=='MESH' for o in scene.objects),'vertices':sum(len(o.data.vertices) for o in scene.objects if o.type=='MESH'),'polygons':sum(len(o.data.polygons) for o in scene.objects if o.type=='MESH'),'bamboo_culms':scene['bamboo_culm_count'],'cameras':[o.name for o in scene.objects if o.type=='CAMERA'],'units':'meters','bounds_approx_m':[50,44],'validation':'Blender modeling and render inspection only. Not integrated or playtested in Unity.'}
with open(OUT+'/scene_manifest.json','w') as f:json.dump(stats,f,ensure_ascii=False,indent=2)
text=bpy.data.texts.new('README • Bamboo Valley')
text.write('BAMBOO VALLEY / 竹影幽谷\nReference-based editable environment model.\nCameras: 01 overview, 02 orthographic layout, 03 bridge/pavilion.\nCollections 01–20 contain themed mesh parts; material-separated meshes can be separated by loose parts in Edit Mode.\nMetric units; ~50 × 44 m. North is +Y, up is +Z.\nPacked reference and banner font; procedural materials need no external textures.\nOriginal default Scene preserved. New environment scene is active.\nNo gameplay, collisions, navigation or Unity material conversion are included.\n')
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/BambooValley.blend')
print(json.dumps(stats,ensure_ascii=False))
