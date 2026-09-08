import bpy, os
root=os.path.dirname(bpy.data.filepath)
s=bpy.data.scenes['Bamboo Valley | 竹影幽谷']
bpy.context.window.scene=s
for name,filename,w,h in [
    ('CAM 01 • Valley overview','BambooValley_Overview.png',1600,1320),
    ('CAM 02 • Layout top','BambooValley_Layout.png',1500,1500),
    ('CAM 03 • Bridge and pavilion','BambooValley_Detail.png',1600,1100),
]:
    s.camera=bpy.data.objects[name]
    s.render.resolution_x=w;s.render.resolution_y=h
    s.render.filepath=os.path.join(root,filename)
    bpy.ops.render.render(write_still=True,scene=s.name)
