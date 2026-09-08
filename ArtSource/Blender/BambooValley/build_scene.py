"""Rebuild the authored scene inside Blender; preserve any other scene.
Run in Blender's Python console with exec(compile(open(PATH).read(), PATH, 'exec')).
The directory is intentionally fixed to this project's ArtSource location.
"""
import bpy, os
root='/Users/gongyuyang/Documents/37minigame/ArtSource/Blender/BambooValley'
old=bpy.data.scenes.get('Bamboo Valley | 竹影幽谷')
if old:
    for obj in list(old.objects):
        bpy.data.objects.remove(obj,do_unlink=True)
    for collection in list(old.collection.children):
        if collection.users<=1:
            bpy.data.collections.remove(collection)
    bpy.data.scenes.remove(old)
namespace={'__builtins__':__builtins__}
bpy.app.driver_namespace['bamboo_valley']=namespace
for filename in ['01_foundation.py','02_landmarks.py','03_dressing.py','04_presentation.py']:
    path=os.path.join(root,filename)
    exec(compile(open(path).read(),path,'exec'),namespace)
