import bpy,bmesh,math,random,json,os
from pathlib import Path
from mathutils import Vector,Matrix
random.seed(20260909)
OUT=Path('/Users/gongyuyang/Documents/37minigame/ArtSource/Blender/PingchuanTown')
scene=bpy.data.scenes.new('Pingchuan Town | 平川山镇 · 大地图')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC';scene.unit_settings.length_unit='METERS'
M={};B={};C={};XFORM=Matrix.Identity(4)
exec(compile((OUT/'geometry.py').read_text(),str(OUT/'geometry.py'),'exec'))
raw_geo=geo
# Transform each reusable module into world space, then batch by region and material.
def geo(g,m,vs,fs):raw_geo(g,m,[XFORM@Vector(p) for p in vs],fs)
def ground(x,y):return 0
raw_pointlight=pointlight
def pointlight(g,name,p,color=(1,.43,.12),energy=28,r=.22):
    return raw_pointlight(g,name,tuple(XFORM@Vector(p)),color,energy,r)
def lettering(g,body,p,size=.3):
    cu=bpy.data.curves.new('Physical sign lettering','FONT');cu.body=body;cu.font=font;cu.size=size;cu.align_x='CENTER';cu.extrude=.003
    ob=bpy.data.objects.new('Sign / '+body,cu);coll(g).objects.link(ob)
    ob.matrix_world=XFORM@Matrix.Translation(p)@Matrix.Rotation(math.pi/2,4,'X');cu.materials.append(M['ink'])
    return ob

def place(fn,g,x,y,a=0,s=1,*args):
    global XFORM
    prev=XFORM.copy();XFORM=Matrix.Translation((x,y,height(x,y)))@Matrix.Rotation(a,4,'Z')@Matrix.Scale(s,4)
    fn(g,*args);XFORM=prev

def height(x,y):return .38+.13*math.sin(x*.054)*math.cos(y*.064)+.10*math.sin((x+y)*.043)
colors={'loam':(.25,.29,.135),'stone':(.35,.39,.37),'stone_light':(.49,.46,.37),'cliff':(.29,.33,.34),'moss':(.16,.24,.066),'wood':(.18,.084,.035),'wood_edge':(.37,.22,.10),'bark':(.14,.10,.052),'roof':(.052,.092,.115),'roof_alt':(.11,.17,.19),'iron':(.045,.054,.055),'brass':(.48,.30,.08),'rope':(.48,.36,.18),'cloth':(.64,.55,.37),'red_cloth':(.43,.052,.025),'ink':(.022,.023,.019),'bamboo':(.17,.29,.06),'bamboo_node':(.36,.39,.13),'leaf0':(.11,.22,.064),'leaf1':(.20,.29,.074),'leaf2':(.31,.36,.093),'leaf3':(.105,.23,.15),'fallen_leaf':(.40,.28,.095),'herb_leaf':(.19,.40,.13),'berry':(.68,.09,.027),'flower':(.87,.80,.46),'pottery':(.13,.23,.23),'backdrop':(.044,.072,.081),'plaster':(.69,.60,.43),'road':(.47,.37,.215),'dirt':(.34,.265,.135),'indigo':(.07,.16,.23)}
for name,c in colors.items():mat(name,c,rough=.85,scale=5 if name not in ['ink','iron','brass'] else 0,stretch=(2,2,.15) if name in ['wood','wood_edge','bark'] else (1,1,1),metal=.65 if name in ['iron','brass'] else 0)
mat('water',(.026,.245,.27),rough=.19,scale=2,stretch=(.15,3,1),metal=.25)
mat('foam',(.49,.70,.66),rough=.6)
mat('paper',(1,.44,.08),rough=.5,emission=3)
mat('flame',(1,.18,.014),emission=5);mat('flame_core',(1,.63,.11),emission=7)
bm=bmesh.new();bmesh.ops.create_icosphere(bm,subdivisions=2,radius=1);bm.verts.ensure_lookup_table();IV=[v.co.copy() for v in bm.verts];IF=[tuple(v.index for v in f.verts) for f in bm.faces];bm.free()
font=bpy.data.fonts.load('/Users/gongyuyang/Documents/37minigame/Library/FontToolsCache/NotoSansCJKsc-Bold.otf');font.pack()

def smooth(pts,steps=10):
    pts=[Vector(p) for p in pts];result=[]
    for i in range(len(pts)-1):
        p0=pts[max(0,i-1)];p1=pts[i];p2=pts[i+1];p3=pts[min(len(pts)-1,i+2)]
        for j in range(steps):
            t=j/steps;result.append(tuple(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)))
    result.append(tuple(pts[-1]));return result

def segdist(x,y,a,b):
    dx=b[0]-a[0];dy=b[1]-a[1];t=max(0,min(1,((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy)))
    return math.hypot(x-a[0]-t*dx,y-a[1]-t*dy)
river=smooth([(-125,17),(-101,1),(-76,-30),(-40,-45),(-12,-53),(23,-65),(42,-91),(80,-107),(124,-93)],8)
canal=smooth([(-12,-53),(-7,-26),(-6,0),(-4,29),(8,53),(41,61),(63,85),(75,118)],7)
rivers=[(river,5.7),(canal,2.7)]
routes=[('Entry',[(-64,-108),(-56,-81),(-49,-63),(-28,-33),(-26,-12),(-25,19),(-25,36)],6.5),
('North',[(-25,36),(-19,50),(0,77),(25,87),(31,111)],5.5),
('West',[(-25,15),(-49,23),(-69,46),(-83,58)],5),
('East',[(-25,-12),(0,-15),(28,-14),(52,-18),(78,2),(94,22)],6),
('Camp',[(-27,-30),(0,-38),(23,-50),(52,-66),(66,-70),(94,-51),(114,-37)],5.5),
('FieldCamp',[(52,-18),(50,-35),(52,-66)],4.5),
('NorthField',[(28,-14),(36,20),(37,45),(25,87)],4.5),
('WestLoop',[(-49,-63),(-77,-32),(-84,2),(-69,46)],3.5),
('CaveLoop',[(-83,58),(-69,71),(-43,70),(-19,50)],3.2)]
routes=[(n,smooth(p,8),w) for n,p,w in routes]
def waterdist(x,y):return min(segdist(x,y,a,b)-w/2 for pts,w in rivers for a,b in zip(pts,pts[1:]))
def roaddist(x,y):return min(segdist(x,y,a,b)-w/2 for _,pts,w in routes for a,b in zip(pts,pts[1:]))
# Flat playable basin, physically recessed beds, continuous terrain under mountain ring.
vs=[];fs=[];N=151
for j in range(N):
 y=-125+j*250/(N-1)
 for i in range(N):
  x=-132+i*264/(N-1);d=waterdist(x,y)
  vs.append((x,y,height(x,y)-max(0,1-d/1.6)*1.35 if d<1.6 else height(x,y)))
for j in range(N-1):
 for i in range(N-1):k=j*N+i;fs.append((k,k+1,k+1+N,k+N))
geo('00 Terrain / continuous basin','loam',vs,fs)

def ribbon(g,m,pts,w,z=None):
    vs=[]
    for i,(x,y) in enumerate(pts):
        a=pts[max(0,i-1)];b=pts[min(len(pts)-1,i+1)];dx=b[0]-a[0];dy=b[1]-a[1];L=math.hypot(dx,dy)
        for side in [-1,1]:
            xx=x-side*dy/L*w/2;yy=y+side*dx/L*w/2
            vs.append((xx,yy,height(xx,yy)+.045 if z is None else z))
    geo(g,m,vs,[(2*i,2*i+2,2*i+3,2*i+1) for i in range(len(pts)-1)])
for pts,w in rivers:ribbon('01 Water / flowing river and town canal','water',pts,w,-.46)
# Roads stop at water banks; bridge spans are added at every actual path crossing.
for n,pts,w in routes:
 for a,b in zip(pts,pts[1:]):
  if waterdist((a[0]+b[0])/2,(a[1]+b[1])/2)<.2:continue
  ribbon('02 Roads / '+n,'road',[a,b],w)
# Town main street paving laid stone-by-stone.
for j in range(98):
 y=-33+j*.72
 for i in range(8):
  x=-28.8+i*.78+(j%2)*.36
  if waterdist(x,y)<.4:continue
  slab('10 Town / main street paving',x,y,height(x,y)+.09,.72,.65,.13,random.choice(['stone','stone_light','stone_light']),random.uniform(-.08,.08))
# Broad rehearsal arena, main camp and cave plazas blend into surrounding ground.
for name,x,y,rx,ry in [('Field',35,-10,22,25),('Camp',63,-66,24,19),('WestCave',-82,55,12,13),('NorthCave',0,73,11,10),('Pass',27,85,15,15)]:
 vs=[(x,y,height(x,y)+.026)]+[(x+rx*math.cos(i*math.tau/64),y+ry*math.sin(i*math.tau/64),height(x+rx*math.cos(i*math.tau/64),y+ry*math.sin(i*math.tau/64))+.027) for i in range(65)]
 geo('02 Roads / '+name+' clearing','dirt',vs,[(0,i,i+1) for i in range(1,65)])
flush()
scene['extent_m']='240 x 220';scene['north_axis']='+Y';scene['up_axis']='+Z'
print('PINGCHUAN FOUNDATION READY',len(scene.objects),flush=True)
