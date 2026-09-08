"""Export only gameplay environment geometry, with portable vertex-color materials.
Run using Blender --background BambooValley.blend --python export_to_unity.py.
The editable master is not overwritten. Foliage is partitioned for Unity culling.
"""
import bpy, os, json, math, re
from mathutils import Vector, noise
from collections import defaultdict

ROOT='/Users/gongyuyang/Documents/37minigame'
OUT=ROOT+'/Assets/Art/Environment/BambooValley'
os.makedirs(OUT,exist_ok=True)
source=bpy.data.scenes['Bamboo Valley | 竹影幽谷']
export=bpy.data.scenes.new('BambooValley_UnityExport')
bpy.context.window.scene=export
materials={}; manifest=[]; stats=[]
def portable_material(mat):
 if mat.name in materials:return materials[mat.name]
 index=len(materials); name='BV_M%02d'%index
 m=bpy.data.materials.new(name);m.diffuse_color=mat.diffuse_color;m.use_nodes=True
 src=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.node_tree else None
 dst=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 color=list(mat.diffuse_color);rough=src.inputs['Roughness'].default_value if src else .7
 emission=src.inputs['Emission Strength'].default_value if src else 0
 dst.inputs['Base Color'].default_value=color;dst.inputs['Roughness'].default_value=rough
 noisy=any(n.type=='TEX_NOISE' for n in mat.node_tree.nodes) if mat.node_tree else False
 manifest.append({'name':name,'source':mat.name,'color':color,'roughness':rough,'emission':emission})
 materials[mat.name]=(m,color,noisy);return materials[mat.name]

for original in source.objects:
 groups=[c.name for c in original.users_collection]
 group=next((g for g in groups if re.match(r'\d\d ',g)),None)
 if not group or int(group[:2])>20 or original.type not in {'MESH','FONT'}:continue
 ob=original.copy();ob.data=original.data.copy();export.collection.objects.link(ob)
 if ob.type=='FONT':
  bpy.context.view_layer.objects.active=ob;ob.select_set(True);bpy.ops.object.convert(target='MESH');ob=bpy.context.object;ob.select_set(False)
 # Thin disconnected leaves dominate the master. Collapse their center fans for gameplay.
 source_material=original.data.materials[0] if original.data.materials else None
 if source_material and source_material.name.startswith('Foliage'):
  bpy.context.view_layer.objects.active=ob
  modifier=ob.modifiers.new('Gameplay leaf simplification','DECIMATE');modifier.ratio=.40
  bpy.ops.object.modifier_apply(modifier=modifier.name)
 mesh=ob.data
 # World-space bake: Unity's importer must not depend on negative object scales.
 mesh.transform(ob.matrix_world);ob.matrix_world.identity()
 mat=original.data.materials[0] if original.data.materials else None
 if not mat:continue
 portable,color,noisy=portable_material(mat)
 partitions=defaultdict(list)
 for p in mesh.polygons:
  if int(group[:2]) in [18,19,20]:
   center=sum((mesh.vertices[i].co for i in p.vertices),Vector())/len(p.vertices)
   cell=(math.floor(center.x/8),math.floor(center.y/8))
  else:cell=(0,0)
  partitions[cell].append(p)
 for part,(cell,polys) in enumerate(partitions.items()):
  name='BV_%s_%03d_%02d'%(group[:2],len(stats),part)
  remap={}; verts=[];faces=[]
  for p in polys:
   face=[]
   for i in p.vertices:
    if i not in remap:remap[i]=len(verts);verts.append(tuple(mesh.vertices[i].co))
    face.append(remap[i])
   faces.append(face)
  me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.materials.append(portable);me.update()
  colors=me.color_attributes.new(name='Color',type='BYTE_COLOR',domain='POINT')
  for i,v in enumerate(verts):
   variation=.84+noise.noise(Vector(v)*1.8)*.20 if noisy else 1
   colors.data[i].color=tuple(min(1,max(0,c*variation)) for c in color[:3])+(1,)
  uv=me.uv_layers.new(name='UVMap')
  for loop in me.loops:
   v=me.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(v.x*.2,v.y*.2)
  obj=bpy.data.objects.new(name,me);export.collection.objects.link(obj)
  obj['source_group']=group;obj['material_source']=mat.name
  stats.append({'name':name,'group':int(group[:2]),'vertices':len(verts),'polygons':len(faces)})
 bpy.data.objects.remove(ob,do_unlink=True)

for name,loc in [('Start',(-20,-16,.55)),('Pavilion',(0,-2,.85)),('Hall',(0,12,.9)),('Boss',(15,12,.96)),('Cave',(20,-3,.5)),('Camp',(-15,10,.5))]:
 ob=bpy.data.objects.new('Anchor_'+name,None);export.collection.objects.link(ob);ob.location=loc
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=OUT+'/BambooValley.fbx',use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
with open(OUT+'/BambooValleyExport.json','w') as f:json.dump({'materials':manifest,'meshes':stats,'coordinate_system':'Imported FBX anchors are (-X,Z,-Y); prefab Y=180 restores (X,Z,Y)','source':'ArtSource/Blender/BambooValley/BambooValley.blend'},f,indent=2)
print('UNITY_EXPORT_OK meshes=%d vertices=%d'%(len(stats),sum(s['vertices'] for s in stats)))
