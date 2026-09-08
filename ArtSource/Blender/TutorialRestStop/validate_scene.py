"""Read-only saved-scene structural audit. This is not Unity gameplay validation."""
import bpy, math, json
from pathlib import Path
root=Path(__file__).resolve().parent
s=next(s for s in bpy.data.scenes if s.name.startswith('Level 01 |'))
meshes=[o for o in s.objects if o.type=='MESH']
checks={
    'metric_units':s.unit_settings.system=='METRIC',
    'all_six_layout_anchors':all('Anchor_'+n in s.objects for n in ['Spawn','Chest','Cave','Herb','Enemy','Rest']),
    'three_cameras':sum(o.type=='CAMERA' for o in s.objects)==3,
    'finite_mesh_vertices':all(math.isfinite(c) for o in meshes for v in o.data.vertices for c in v.co),
    'meshes_nonempty':all(len(o.data.vertices)>0 and len(o.data.polygons)>0 for o in meshes),
    'all_environment_meshes_have_materials':all(len(o.data.materials)>0 for o in meshes),
    'reference_packed':any(i.name.startswith('REFERENCE • Level 01') and i.packed_file for i in bpy.data.images),
    'font_packed':any(f.packed_file for f in bpy.data.fonts if f.name!='Bfont'),
    'no_missing_external_images':not any(i.source=='FILE' and i.filepath and not i.packed_file and not Path(bpy.path.abspath(i.filepath)).exists() for i in bpy.data.images),
    'compositor_connected':bool(s.compositing_node_group and len(s.compositing_node_group.links)>=2),
    'new_reference_layout':s.objects['Anchor_Chest'].location.x<0 and s.objects['Anchor_Chest'].location.y>0 and s.objects['Anchor_Cave'].location.x>0 and s.objects['Anchor_Cave'].location.y>0 and s.objects['Anchor_Herb'].location.x<0 and s.objects['Anchor_Enemy'].location.x>0,
    'enemy_away_from_cooking_fire':math.hypot(s.objects['Anchor_Enemy'].location.x-5.85,s.objects['Anchor_Enemy'].location.y+2.50)>.9,
}
report={'checks':checks,'passed':all(checks.values()),'scene':s.name,'blend':bpy.data.filepath,
        'mesh_objects':len(meshes),'polygons':sum(len(o.data.polygons) for o in meshes),
        'scope':'Blender structure and packed dependencies only. No Unity collision, timing or device tests.'}
(root/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
print(json.dumps(report,ensure_ascii=False))
assert report['passed'],'Blender structural audit failed'
