import bpy, math, random, os, json, bmesh
from mathutils import Vector
from collections import defaultdict
random.seed(220907)
OUT='/Users/gongyuyang/Documents/37minigame/ArtSource/Blender/BambooValley'
scene=bpy.data.scenes.new('Bamboo Valley | 竹影幽谷')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
scene['reference']='abd40112-e408-4289-8cfb-bc5ed03d6a52.png; visual reference only'
scene['purpose']='Editable environment art study; no Unity gameplay integration'
M={}; B={}; collections={}
def material(name,c,rough=.7,noise=0,metal=0,emission=0):
 m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); m.use_nodes=True
 n=m.node_tree.nodes; l=m.node_tree.links; p=next(nod for nod in n if nod.type=='BSDF_PRINCIPLED')
 p.inputs['Base Color'].default_value=(*c,1); p.inputs['Roughness'].default_value=rough; p.inputs['Metallic'].default_value=metal
 if emission: p.inputs['Emission Color'].default_value=(*c,1); p.inputs['Emission Strength'].default_value=emission
 if noise:
  tex=n.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=noise; tex.inputs['Detail'].default_value=3
  ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.2; ramp.color_ramp.elements[0].color=(*(v*.48 for v in c),1)
  ramp.color_ramp.elements[1].position=.8; ramp.color_ramp.elements[1].color=(*(min(1,v*1.35) for v in c),1)
  l.new(tex.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],p.inputs['Base Color'])
  bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.23; bump.inputs['Distance'].default_value=.12
  l.new(tex.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs[0],p.inputs['Normal'])
 M[name]=m; return m
material('Earth • shaded loam',(.19,.225,.105),noise=2)
material('Stone • blue limestone',(.32,.37,.35),noise=3)
material('Stone • pale worn flags',(.48,.46,.35),noise=5)
material('Rock • charcoal cliff',(.20,.25,.235),noise=2.8)
material('Moss • jade olive',(.20,.29,.075),noise=7)
material('Wood • aged cedar',(.235,.108,.045),noise=6)
material('Wood • honey edges',(.42,.245,.10),noise=5)
material('Roof • midnight ceramic',(.065,.125,.15),rough=.38,noise=8)
material('Roof • patina ridges',(.16,.235,.235),rough=.44,noise=6)
material('Bamboo • forest green',(.095,.235,.065),noise=3)
material('Bamboo • golden nodes',(.30,.385,.12))
for i,c in enumerate([(.14,.27,.065),(.25,.36,.085),(.36,.42,.12),(.075,.19,.065)]): material('Foliage '+str(i),c)
material('Fabric • oxblood',(.39,.055,.025),noise=7)
material('Fabric • worn flax',(.48,.40,.245),noise=12)
material('Iron • blackened',(.055,.073,.065),rough=.4,metal=.6)
material('Lantern • amber silk',(1,.40,.065),rough=.4,emission=4)
material('Fire • gold',(1,.15,.008),emission=7)
material('Foam • silver jade',(.49,.77,.72),rough=.3,emission=.12)
water=material('Water • deep turquoise',(.025,.20,.205),rough=.19,noise=4,metal=.38)
next(nod for nod in water.node_tree.nodes if nod.type=='BSDF_PRINCIPLED').inputs['Coat Weight'].default_value=.45
material('Backdrop',(.025,.045,.048),rough=.9)
def coll(name):
 if name not in collections:
  c=bpy.data.collections.new(name); scene.collection.children.link(c); collections[name]=c
 return collections[name]
def geo(group,mat,vs,fs):
 key=(group,mat)
 if key not in B: B[key]=[[],[]]
 v,f=B[key]; offset=len(v); v.extend(vs); f.extend([tuple(offset+i for i in face) for face in fs])
def box(g,m,p,s,rz=0):
 x,y,z=p; a,b,c=(v/2 for v in s); co=math.cos(rz); si=math.sin(rz)
 vs=[(x+u*co-v*si,y+u*si+v*co,z+w) for u,v,w in [(-a,-b,-c),(a,-b,-c),(a,b,-c),(-a,b,-c),(-a,-b,c),(a,-b,c),(a,b,c),(-a,b,c)]]
 geo(g,m,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
def rod(g,m,a,b,r,r2=None,n=8):
 a=Vector(a); b=Vector(b); w=(b-a).normalized(); u=w.cross(Vector((0,0,1)))
 if u.length<.01: u=w.cross(Vector((0,1,0)))
 u.normalize(); v=w.cross(u); r2=r if r2 is None else r2
 vs=[tuple(p+(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))*rr) for p,rr in [(a,r),(b,r2)] for i in range(n)]
 fs=[tuple(reversed(range(n))),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 geo(g,m,vs,fs)
def line(g,m,pts,r=.04,n=6):
 for a,b in zip(pts,pts[1:]): rod(g,m,a,b,r,n=n)
bm=bmesh.new(); bmesh.ops.create_icosphere(bm,subdivisions=2,radius=1)
bm.verts.ensure_lookup_table(); IV=[v.co.copy() for v in bm.verts]; IF=[tuple(v.index for v in f.verts) for f in bm.faces]; bm.free()
def rock(g,p,s,mat='Rock • charcoal cliff',moss=False):
 vs=[(p[0]+v.x*s[0]*random.uniform(.85,1.15),p[1]+v.y*s[1]*random.uniform(.85,1.15),p[2]+v.z*s[2]*random.uniform(.87,1.13)) for v in IV]
 geo(g,mat,vs,IF)
 if moss:
  top=[f for f in IF if sum(IV[i].z for i in f)/len(f)>.45 and random.random()<.65]
  geo(g,'Moss • jade olive',[(x,y,z+.015) for x,y,z in vs],top)
def flush():
 for (g,m),(v,f) in B.items():
  me=bpy.data.meshes.new(g+' / '+m); me.from_pydata(v,[],f); me.materials.append(M[m]); me.update()
  ob=bpy.data.objects.new(g+' / '+m,me); coll(g).objects.link(ob)
 B.clear()
def riverx(y): return -7+2.1*math.sin(y*.16)+max(0,-y-4)*.48
def ground(x,y):
 base=.40+.12*math.sin(x*.36)*math.cos(y*.32)
 d=abs(x-riverx(y)); base-=max(0,1-d/2.65)*1.05
 edge=max(abs(x)/25,abs(y)/22)
 return base+max(0,edge-.74)*2.4
def inside(x,y): return (abs(x)/25)**4+(abs(y)/22)**4<1
g='01 Terrain • continuous valley floor'
vs=[]; fs=[]; ids={}
for j in range(89):
 y=-22+j*.5
 for i in range(101):
  x=-25+i*.5
  if inside(x,y): ids[i,j]=len(vs); vs.append((x,y,ground(x,y)))
for i,j in ids:
 if all(k in ids for k in [(i+1,j),(i+1,j+1),(i,j+1)]): fs.append(tuple(ids[k] for k in [(i,j),(i+1,j),(i+1,j+1),(i,j+1)]))
geo(g,'Earth • shaded loam',vs,fs)
# Rock mass beneath continuous ground, with an irregular silhouette.
for i in range(82):
 a=i*math.tau/82; x=24.4*math.copysign(abs(math.cos(a))**.5,math.cos(a)); y=21.4*math.copysign(abs(math.sin(a))**.5,math.sin(a))
 rock('02 Perimeter • moss cliffs',(x,y,-.5),(random.uniform(1.3,2.6),random.uniform(1.3,2.4),random.uniform(1.7,2.7)),moss=True)
# The water surface follows the cut channel.
vs=[]
for i in range(111):
 y=-23+i*.42; x=riverx(y); w=2.15+.22*math.sin(y*.7)
 vs.extend([(x-w,y,-.06),(x+w,y,-.06)])
geo('03 Stream • jade water','Water • deep turquoise',vs,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(110)])
for i in range(165):
 y=random.uniform(-21,21); x=riverx(y)+random.choice([-1,1])*random.uniform(2,3.1)
 rock('04 Stream banks • boulders',(x,y,ground(x,y)+.12),(random.uniform(.3,1),random.uniform(.35,1.2),random.uniform(.3,.9)),moss=True)
for i in range(210):
 y=random.uniform(-21,21); x=riverx(y)+random.uniform(-1.8,1.8)
 line('03 Stream • jade water','Foam • silver jade',[(x+.06*math.sin(k),y+k*.13,-.035) for k in range(random.randint(2,6))],random.uniform(.008,.022),4)
# Curving routes are encoded once for paving and vegetation exclusion.
routes=[([(-20,-16),(-17,-12),(-14,-8),(-11,-6)],2.7),([(-4.5,-6),(0,-5),(3,-2),(3,4),(1,8)],3.2), ([(3,3),(8,6),(12,7),(15,10)],2.8), ([(5,-3),(10,-6),(16,-7),(20,-3)],2.1), ([(-14,-7),(-16,0),(-15,6),(-15,11)],2.2), ([(-14,10),(-10,8),(-7,7),(-2,8),(1,8)],2.2)]
def segments():
 for pts,width in routes:
  for a,b in zip(pts,pts[1:]): yield a,b,width
def distance_seg(x,y,a,b):
 dx=b[0]-a[0];dy=b[1]-a[1]; t=max(0,min(1,((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy)))
 return math.hypot(x-a[0]-t*dx,y-a[1]-t*dy)
for a,b,width in segments():
 length=math.dist(a,b); ang=math.atan2(b[1]-a[1],b[0]-a[0]); count=int(length/.72)+1
 for i in range(count):
  t=i/count; x=a[0]+t*(b[0]-a[0]); y=a[1]+t*(b[1]-a[1])
  if abs(x-riverx(y))<2.3: continue
  for j in range(3):
   o=(j-1)*width*.29; xx=x-math.sin(ang)*o; yy=y+math.cos(ang)*o
   box('05 Routes • worn stone paving','Stone • pale worn flags',(xx,yy,ground(xx,yy)+.07),(.62,width*.275,.12),ang+random.uniform(-.09,.09))
flush()
print('Foundation ready',len(scene.objects))
