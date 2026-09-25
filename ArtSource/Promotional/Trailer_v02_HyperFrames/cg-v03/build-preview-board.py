from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
p=Path('/Users/gongyuyang/Documents/37minigame/ArtSource/Promotional/Trailer_v02_HyperFrames')
font='/Users/gongyuyang/Documents/37minigame/Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.ttf'
f1=ImageFont.truetype(font,42);f2=ImageFont.truetype(font,26);f3=ImageFont.truetype(font,20)
W,H=1376,1560;im=Image.new('RGB',(W,H),'#0E1112');d=ImageDraw.Draw(im)
d.text((36,24),'一炷江湖  /  CG 开场新分镜',font=f1,fill='#E9DFC3')
d.text((36,91),'约 90 秒全片  ·  暂定 36 秒 CG + 46 秒实机 + 8 秒片尾',font=f2,fill='#D1AA5A')
d.text((36,137),'本轮新生成 8 张原画  /  按从左至右、从上至下顺序播放  /  尚未制作动态成片',font=f3,fill='#E9DFC3')
shots=json.loads((p/'shot-manifest.json').read_text())['shots'][:8]
for i,s in enumerate(shots):
 x=36+(i%4)*334;y=190+(i//4)*660
 d.text((x,y),f"{s['id']}  {s['title']}",font=f2,fill='#E9DFC3')
 src=Image.open(p/s['image']).convert('RGB');src.thumbnail((308,548))
 im.paste(src,(x,y+42))
 d.text((x,y+603),f"{s['start']:02d}—{s['end']:02d} 秒",font=f3,fill='#D1AA5A')
d.text((36,1530),'预览用途：看构图与叙事顺序；烟雾、火焰、镜头运动与音效将在合成阶段制作。',font=f3,fill='#E9DFC3')
im.save(p/'cg-v03/cg-preview-board.jpg',quality=94)
# Basic asset and timeline validation.
assert len(shots)==8
for s in json.loads((p/'shot-manifest.json').read_text())['shots']:
 assert (p/s['image']).is_file(),s['image']
print('Board created; all 13 preview references resolve, 8 CG source images verified.')
