"""Run in a fresh Blender file; creates a scene without deleting other scenes.
Repeated runs create another revision scene, so save hand edits separately first.
"""
import bpy
from pathlib import Path
root=Path(__file__).resolve().parent
ns={'__builtins__':__builtins__}
bpy.app.driver_namespace['tutorial_reststop']=ns
for name in ['01_foundation.py','02_landmarks.py','03_dressing.py','04_nature_lighting.py']:
    path=root/name
    ns['__file__']=str(path)
    exec(compile(path.read_text(),str(path),'exec'),ns)
