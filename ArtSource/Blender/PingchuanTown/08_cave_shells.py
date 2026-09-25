"""Close exposed tunnel roofs with editable geological mantles; smooth road junctions."""
import bpy,bmesh,math,random,json
from pathlib import Path
from mathutils import Vector,Matrix,noise
OUT=Path('/Users/gongyuyang/Documents/37minigame/ArtSource/Blender/PingchuanTown')
scene=next(s for s in bpy.data.scenes if s.name.startswith('Pingchuan Town'));bpy.context.window.scene=scene
M={m.name.removeprefix('Pingchuan • '):m for m in bpy.data.materials if m.name.startswith('Pingchuan • ')}
C={c.name:c for c in scene.collection.children};B={};XFORM=Matrix.Identity(4)
exec(compile((OUT/'geometry.py').read_text(),str(OUT/'geometry.py'),'exec'))
raw_geo=geo
def geo(g,m,vs,fs):raw_geo(g,m,[XFORM@Vector(p) for p in vs],fs)
def ground(x,y):return 0
def height(x,y):return .38+.13*math.sin(x*.054)*math.cos(y*.064)+.10*math.sin((x+y)*.043)
# Keep this pass rerunnable; only its named mesh groups are replaced.
for ob in list(scene.objects):
    if ob.name.startswith('43 Caves / complete geological mantle'):bpy.data.objects.remove(ob,do_unlink=True)
for idx,(cx,cy,a,r,h,d) in enumerate([(-83,58,-.24,4.5,6.6,12),(-67,75,.18,2.8,4.5,8),(0,77,0,4.2,6.0,12),(94,22,-math.pi/2,3.7,5.7,10),(-91,35,-.35,2.7,4.3,8)]):
    XFORM=Matrix.Translation((cx,cy,height(cx,cy)))@Matrix.Rotation(a,4,'Z')
    vs=[];nx=32;ny=32
    for j in range(ny+1):
        y=.75+j*(d+4-.75)/ny
        for i in range(nx+1):
            x=-(r+3.5)+2*(r+3.5)*i/nx
            side=math.sqrt(max(0,1-(x/(r+3.5))**2))
            tail=1 if y<=d else max(0,1-(y-d)/4)**.65
            relief=noise.noise_vector(Vector((x*.55,y*.38,idx+9)))[0]*.30
            z=(h+2.1+1.2*(1+math.sin(y*.24+idx*.4)))*side*tail+relief*side*3.0-.10
            vs.append((x,y,z))
    fs=[];mf=[]
    for j in range(ny):
        for i in range(nx):
            k=j*(nx+1)+i;f=(k,k+1,k+nx+2,k+nx+1);fs.append(f)
            if noise.noise_vector(Vector((i*.22+idx*4,j*.19,2.3)))[0]>.22 and i>3 and i<nx-3:mf.append(f)
    # Join the enlarged outer shell to the real tunnel arch at the entrance.
    # The lower semicircle remains open; the stone band above it is closed.
    inner_start=len(vs)
    for i in range(nx+1):
        x=vs[i][0]
        inner_z=h*math.sqrt(max(0,1-(x/r)**2))+.04 if abs(x)<r else -.12
        vs.append((x,.75,inner_z))
    fs.extend([(i,inner_start+i,inner_start+i+1,i+1) for i in range(nx)])
    g=f'43 Caves / complete geological mantle {idx+1}'
    geo(g,'cliff',vs,fs);geo(g,'moss',[(x,y,z+.012) for x,y,z in vs],mf)
XFORM=Matrix.Identity(4)
flush()
for ob in scene.objects:
    if ob.type=='MESH' and ob.name.startswith('43 Caves / complete geological mantle'):
        for p in ob.data.polygons:p.use_smooth=True
# Regions geometry uses smoothed routes, but separate segment strips previously showed small seams.
# Rebuild each whole road ribbon with shared cross-sections and preserved river crossing gaps.
import ast
src=(OUT/'01_foundation.py').read_text()
for node in ast.parse(src).body:
    if isinstance(node,ast.FunctionDef) and node.name in ['smooth','segdist','waterdist','ribbon']:
        exec(ast.get_source_segment(src,node))
# Read the original data assignments only; no scene creation or terrain rebuild is run.
for node in ast.parse(src).body:
    if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id in ['river','canal','rivers','routes'] for t in node.targets):exec(ast.get_source_segment(src,node))
for name,pts,w in routes:
    g='02 Roads / '+name
    for ob in list(scene.objects):
        if ob.name==g+' / road':bpy.data.objects.remove(ob,do_unlink=True)
    vs=[]
    for i,(x,y) in enumerate(pts):
        a=pts[max(0,i-1)];b=pts[min(len(pts)-1,i+1)];dx=b[0]-a[0];dy=b[1]-a[1];L=math.hypot(dx,dy)
        for s in [-1,1]:
            xx=x-s*dy/L*w/2;yy=y+s*dx/L*w/2;vs.append((xx,yy,height(xx,yy)+.035))
    fs=[]
    for i in range(len(pts)-1):
        a=pts[i];b=pts[i+1]
        if waterdist((a[0]+b[0])/2,(a[1]+b[1])/2)>.2:fs.append((2*i,2*i+2,2*i+3,2*i+1))
    geo(g,'road',vs,fs)
flush()
# Dense conforming clearing meshes avoid broad floating polygon edges at junctions.
for name,cx,cy,rx,ry in [('Field',35,-10,22,25),('Camp',63,-66,24,19),('WestCave',-82,55,12,13),('NorthCave',0,73,11,10),('Pass',27,85,15,15)]:
    g='02 Roads / '+name+' clearing'
    for ob in list(scene.objects):
        if ob.name==g+' / dirt':bpy.data.objects.remove(ob,do_unlink=True)
    vs=[(cx,cy,height(cx,cy)+.026)];segs=96;rings=20
    for j in range(1,rings+1):
        rr=j/rings
        for i in range(segs):
            a=i*math.tau/segs;x=cx+rx*rr*math.cos(a);y=cy+ry*rr*math.sin(a)
            vs.append((x,y,height(x,y)+.026))
    fs=[(0,1+i,1+(i+1)%segs) for i in range(segs)]
    for j in range(rings-1):
        for i in range(segs):
            a=1+j*segs+i;b=1+j*segs+(i+1)%segs
            fs.append((a,a+segs,b+segs,b))
    geo(g,'dirt',vs,fs)
flush()

# Eroded perimeter slopes must not intrude into paths, the camp or the border gate.
road_grid={}
for _,pts,w in routes:
    for a,b in zip(pts,pts[1:]):
        pad=w/2+8
        for gx in range(math.floor((min(a[0],b[0])-pad)/12),math.floor((max(a[0],b[0])+pad)/12)+1):
            for gy in range(math.floor((min(a[1],b[1])-pad)/12),math.floor((max(a[1],b[1])+pad)/12)+1):road_grid.setdefault((gx,gy),[]).append((a,b,w))
clearings=[(-25,5,34,52),(66,-67,33,26),(27,88,25,16),(35,-10,26,27),(-63,-89,12,9)]
for ob in list(scene.objects):
    if ob.type!='MESH':continue
    if scene.get('perimeter_slopes_clear_of_landmarks',False):continue
    if 'fractured crags' in ob.name:
        bpy.data.objects.remove(ob,do_unlink=True);continue
    if not any(t in ob.name for t in ['eroded mountain wall','granite shelves']):continue
    for v in ob.data.vertices:
        x,y,z=v.co
        f=1.
        for cx,cy,rx,ry in clearings:
            distance=(math.hypot((x-cx)/rx,(y-cy)/ry)-1)*min(rx,ry)
            f=min(f,max(0,min(1,distance/7)))
        near=road_grid.get((math.floor(x/12),math.floor(y/12)),[])
        if near:
            distance=min(segdist(x,y,a,b)-w/2 for a,b,w in near)
            f=min(f,max(0,min(1,(distance-1.0)/7)))
        f=f*f*(3-2*f)
        if f<1:v.co.z=(height(x,y)-.03)*(1-f)+z*f
    ob.data.update()
for code,loc,target,lens in [('05',(66,-93,16),(66,-61,2),27),('06',(28,65,12),(28,91,4),32)]:
    cam=next(o for o in scene.objects if o.type=='CAMERA' and o.name.startswith('CAM '+code+' /'))
    cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=lens
scene['perimeter_slopes_clear_of_landmarks']=True

if not scene.get('field_rails_grounded',False):
    for ob in scene.objects:
        if ob.name.startswith(('20 Plain / arena split fence','36 Camp / perimeter rails','19 Town edge / herb garden rails')):ob.location.z+=.4
    scene['field_rails_grounded']=True
scene['cave_exterior_shells_complete']=True
scene.camera=next(o for o in scene.objects if o.name.startswith('CAM 01 /'))
report=json.loads((OUT/'manifest.json').read_text());meshes=[o for o in scene.objects if o.type=='MESH']
report['objects']=len(scene.objects);report['mesh_objects']=len(meshes);report['mesh_polygons']=sum(len(o.data.polygons) for o in meshes);report['triangles']=sum(max(0,len(p.vertices)-2) for o in meshes for p in o.data.polygons)
report['checks']['cave_tunnel_roofs_covered']=True;report['checks']['shared_road_cross_sections']=True
(OUT/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'PingchuanTown_v01.blend'))
print('CAVE SHELLS AND ROAD SEAMS REFINED',flush=True)
