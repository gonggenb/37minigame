"""Export optimized, spatially culled vertex-colored meshes. Never overwrite master."""
import bpy,sys,os,json,math,re
from mathutils import Vector,noise
from collections import defaultdict
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../../..'))
sys.path.insert(0,os.path.dirname(__file__))
from gameplay_layout import build,SCALE
OUT=ROOT+'/Assets/Art/Environment/PingchuanTown';os.makedirs(OUT,exist_ok=True)
source=bpy.data.scenes['Pingchuan Town | 平川山镇 · 大地图']
export=bpy.data.scenes.new('Pingchuan_UnityExport');bpy.context.window.scene=export
materials={};manifest=[];batches={};stats=[]
def portable(mat):
 if mat.name in materials:return materials[mat.name]
 name='PC_M%02d'%len(materials);m=bpy.data.materials.new(name);m.use_nodes=True
 src=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.node_tree else None
 color=list(mat.diffuse_color);rough=src.inputs['Roughness'].default_value if src else .8
 emit=src.inputs['Emission Strength'].default_value if src else 0
 m.diffuse_color=color;bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=color
 manifest.append(dict(name=name,source=mat.name.split('•')[-1].strip(),color=color,roughness=rough,emission=emit))
 materials[mat.name]=(m,color);return materials[mat.name]
for ix,original in enumerate(list(source.objects)):
 if original.type not in {'MESH','FONT'}:continue
 groups=[c.name for c in original.users_collection];group=next((g for g in groups if re.match(r'\d\d ',g)),None)
 if not group or int(group[:2])>=80:continue
 ob=original.copy();ob.data=original.data.copy();export.collection.objects.link(ob)
 bpy.context.view_layer.objects.active=ob;ob.select_set(True)
 if ob.type=='FONT':bpy.ops.object.convert(target='MESH');ob=bpy.context.object
 ob.select_set(False)
 mat=ob.data.materials[0] if ob.data.materials else None
 if mat is None:bpy.data.objects.remove(ob,do_unlink=True);continue
 # Keep thin sheets and signs; collapse high-density needle pads, stones and roof tiles.
 ratio=.45
 material_key=mat.name.split('•')[-1].strip()
 if material_key in ['roof','roof_alt']:ratio=.16
 if material_key in ['rope','iron','brass']:ratio=.06
 if group.startswith('70') or group.startswith('79'):ratio=.18 if material_key.startswith(('leaf','herb','bark','fallen')) else .3
 if group.startswith(('00','01','02','60','61')) or original.type=='FONT':ratio=1
 if len(ob.data.polygons)>100 and ratio<1:
  mod=ob.modifiers.new('Gameplay simplification','DECIMATE');mod.ratio=ratio
  bpy.ops.object.modifier_apply(modifier=mod.name)
 me=ob.data;me.transform(ob.matrix_world);ob.matrix_world.identity()
 pm,color=portable(mat)
 for p in me.polygons:
  center=p.center
  # Separate buildings for culling, nature in 12.8m cells, all baked in scene coordinates.
  cell=(math.floor(center.x/32),math.floor(center.y/32))
  key=(cell,pm.name)
  if key not in batches:batches[key]=[[],[],[],pm,group]
  vs,faces,cols,_,_=batches[key];face=[]
  for vi in p.vertices:
   v=me.vertices[vi].co;face.append(len(vs));vs.append(tuple(v*SCALE))
   factor=.88+noise.noise(v*1.6)*.16
   cols.append(tuple(max(0,min(1,c*factor)) for c in color[:3])+(1,))
  faces.append(face)
 bpy.data.objects.remove(ob,do_unlink=True)
 if ix%150==0:print('EXPORT progress',ix,flush=True)
for (cell,mname),(vs,faces,cols,pm,group) in batches.items():
 name='PC_%s_%s_%s'%(cell[0],cell[1],mname);me=bpy.data.meshes.new(name);me.from_pydata(vs,[],faces);me.materials.append(pm);me.update()
 attr=me.color_attributes.new(name='Color',type='BYTE_COLOR',domain='POINT')
 for i,c in enumerate(cols):attr.data[i].color=c
 # Merge position duplicates only after color bake. FBX maintains per-corner discontinuities.
 ob=bpy.data.objects.new(name,me);export.collection.objects.link(ob)
 stats.append(dict(name=name,vertices=len(vs),triangles=sum(len(f)-2 for f in faces)))
for name,p in [('Start',(-64,-103,0)),('Town',(-25,0,0)),('Pass',(27,95,0))]:
 ob=bpy.data.objects.new('Anchor_'+name,None);export.collection.objects.link(ob);ob.location=Vector(p)*SCALE
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=OUT+'/PingchuanTown.fbx',use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
with open(OUT+'/PingchuanTownExport.json','w') as f:json.dump(dict(materials=manifest,meshes=stats,triangles=sum(s['triangles'] for s in stats),scale=SCALE,source='ArtSource/Blender/PingchuanTown/PingchuanTown_v01.blend'),f,indent=2)
os.makedirs(ROOT+'/Assets/Resources',exist_ok=True)
with open(ROOT+'/Assets/Resources/PingchuanTownLayout.json','w') as f:json.dump(build(),f,indent=2)
print('PINGCHUAN_EXPORT_OK',len(stats),sum(s['triangles'] for s in stats),flush=True)
