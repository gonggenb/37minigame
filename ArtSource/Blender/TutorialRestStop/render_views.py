"""Render saved environment without modifying the .blend. CPU avoids Metal kernel warmup."""
import bpy, os
from pathlib import Path
root=Path(__file__).resolve().parent
s=next(s for s in bpy.data.scenes if s.name.startswith('Level 01 |'))
bpy.context.window.scene=s
s.cycles.device='CPU'
preview=os.environ.get('RESTSTOP_PREVIEW','0')=='1'
views=[('CAM 01 • whole rest stop','RestStop_Preview.png',960,720,16)] if preview else [
    ('CAM 01 • whole rest stop','RestStop_Overview.png',1600,1200,48),
    ('CAM 02 • plan','RestStop_Layout.png',1300,1300,24),
    ('CAM 03 • tea pine and pavilion','RestStop_Detail.png',1600,1100,48)]
for name,filename,w,h,samples in views:
    s.camera=bpy.data.objects[name];s.render.resolution_x=w;s.render.resolution_y=h
    s.render.resolution_percentage=100;s.cycles.samples=samples;s.render.filepath=str(root/filename)
    print('RENDER_START',filename,flush=True)
    bpy.ops.render.render(write_still=True,scene=s.name)
    print('RENDER_DONE',filename,flush=True)
