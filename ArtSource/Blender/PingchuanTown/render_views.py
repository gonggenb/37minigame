"""Render supplied camera names; run with Blender background against saved .blend."""
import bpy,sys
from pathlib import Path
root=Path(__file__).resolve().parent
scene=next(s for s in bpy.data.scenes if s.name.startswith('Pingchuan Town'))
bpy.context.window.scene=scene
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['01','03','04','05','06','07']
scene.render.resolution_x=1500;scene.render.resolution_y=1050
scene.cycles.samples=24
for code in args:
    cam=next(o for o in scene.objects if o.type=='CAMERA' and o.name.startswith('CAM '+code+' /'))
    scene.render.resolution_x=1400 if code=='02' else 1500;scene.render.resolution_y=1400 if code=='02' else 1050
    scene.camera=cam;scene.render.filepath=str(root/'previews'/f'{code}_Preview.png')
    print('RENDER_START',code,flush=True);bpy.ops.render.render(write_still=True);print('RENDER_DONE',code,flush=True)
