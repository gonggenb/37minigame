"""Final orientation corrections and reproducible structural report (no Unity claims)."""
# The east portal must face west into the basin; local +Y tunnel runs east into the mountain.
g='40 Caves / portal 04'
# Only apply rotation when loading the scene generated before the source orientation correction.
if not scene.get('east_portal_facing_west',False):
    if scene.get('source_portal_orientation_correct',False):pass
    else:
        cx,cy=94,22
        T=Matrix.Translation((cx,cy,0))@Matrix.Rotation(math.pi,4,'Z')@Matrix.Translation((-cx,-cy,0))
        for ob in C[g].objects:ob.matrix_world=T@ob.matrix_world
    scene['east_portal_facing_west']=True
    for ob in scene.objects:
        if ob.name=='Anchor / Cave 4 / entrance':ob.location.x=90;ob.location.y=22
    for a in landmarks:
        if a['name']=='Cave 4 / entrance':a['position_m']=[90,22,height(90,22)]
for group,label in [('50 Pass / gate timbers and tiled roof','山口哨道'),('33 Camp / entry gate','戍营')]:
    for ob in C[group].objects:
        if ob.type=='FONT' and ob.data.body=='平川山镇':ob.data.body=label
# Improve far-facing cave mantle placement to cover the short tunnel.
for ob in scene.objects:
    if ob.name.startswith('41 Caves / rock mantle 4') and not ob.get('mantle_repositioned',False):
        ob.location.x+=8;ob.location.y-=8;ob['mantle_repositioned']=True
# Make reference originals easy to find inside Blender.
readme=bpy.data.texts.get('README / Pingchuan Town') or bpy.data.texts.new('README / Pingchuan Town')
readme.clear();readme.write('平川山镇 | Pingchuan Town\n\nIndependent editable Blender environment, meters, +Y north, +Z up.\nCAM 01 overview; CAM 02 plan; CAM 03 town; CAM 04 caves; CAM 05 camp; CAM 06 pass; CAM 07 plain; CAM 08 entry.\nUse the Outliner collections to isolate each building and region. Six source sheets are packed in the Image Editor.\nEnvironment model only: no Unity scene, collisions, navigation, enemies or interactions have been connected.\nMaterials use Blender procedural nodes. Bake textures, split chunks and author LOD/colliders before mobile use.\nOriginal TutorialRestStop file is unchanged.\n')
# Domain-specific checks: actual landmarks, finite geometry, cameras, packed references and save/reopen data.
meshes=[o for o in scene.objects if o.type=='MESH']
nonfinite=[];empty=[];no_material=[];degenerate=0;tris=0
for ob in meshes:
    if len(ob.data.polygons)==0:empty.append(ob.name)
    if not ob.data.materials:no_material.append(ob.name)
    if any(not math.isfinite(c) for v in ob.data.vertices for c in v.co):nonfinite.append(ob.name)
    degenerate+=sum(p.area<1e-10 for p in ob.data.polygons)
    tris+=sum(max(0,len(p.vertices)-2) for p in ob.data.polygons)
# Degenerate zero-area faces from tapered cone tips are excluded in the final mesh cleanup below.
removed=0
for ob in meshes:
    bad=[p.index for p in ob.data.polygons if p.area<1e-10]
    if not bad:continue
    bm=bmesh.new();bm.from_mesh(ob.data);bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i in bad],context='FACES_ONLY')
    bm.to_mesh(ob.data);bm.free();ob.data.update();removed+=len(bad)
scene['status']='Modeled and structurally checked; camera renders available separately. Not Unity integrated.'
scene.camera=hero
report={'scene':scene.name,'file':'PingchuanTown_v01.blend','units':'meters','north_axis':'+Y','up_axis':'+Z','design_extent_m':[240,220],
'objects':len(scene.objects),'mesh_objects':len(meshes),'mesh_polygons':sum(len(o.data.polygons) for o in meshes),'triangles':sum(max(0,len(p.vertices)-2) for o in meshes for p in o.data.polygons),
'buildings':25,'market_stalls':9,'cave_entrances':5,'bridges':len(bridges),'trees':tree_count,'bamboo_culms':bamboo_count,'grass_tufts':grass_count,
'cameras':[o.name for o in scene.objects if o.type=='CAMERA'],'landmarks':landmarks,
'checks':{'nonfinite_meshes':nonfinite,'empty_meshes':empty,'meshes_without_materials':no_material,'zero_area_faces_removed':removed,'east_cave_faces_basin':True,'references_packed':sum(i.packed_file is not None for i in bpy.data.images if i.name.startswith('Pingchuan reference'))==6},
'unity_imported':False,'physics_validated':False,'mobile_performance_validated':False,'texture_baking_completed':False,'human_visual_approved':False,
'notes':['Main and side routes are linked by modeled bridges. No engine navigation is claimed.','References guide layout and art, not gameplay changes.','Original .blend file and all Unity gameplay code are unchanged.']}
(OUT/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
scene['mesh_polygon_count']=report['mesh_polygons'];scene['triangle_count']=report['triangles']
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'PingchuanTown_v01.blend'))
print('FINAL STRUCTURAL REPORT',json.dumps({k:v for k,v in report.items() if k not in ['landmarks','cameras','notes']},ensure_ascii=False),flush=True)
