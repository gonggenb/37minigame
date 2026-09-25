"""Run in Blender's Python console or with blender --background --python build_scene.py.
Creates a new scene without deleting existing scenes. Saves PingchuanTown_v01.blend.
Save hand edits separately before rebuilding; use a fresh file for repeat builds.
"""
import bpy
from pathlib import Path
root=Path(__file__).resolve().parent
ns={'__builtins__':__builtins__}
bpy.app.driver_namespace['pingchuan']=ns
for name in ['01_foundation.py','02_modules.py','03_regions.py','04_landscape.py','05_presentation.py','06_refinement.py','07_final_checks.py','08_cave_shells.py']:
    p=root/name;exec(compile(p.read_text(),str(p),'exec'),ns)
