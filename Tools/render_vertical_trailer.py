#!/usr/bin/env python3
"""Composite the 30s promotional film from project artwork and captured Play Mode frames.

Requires Pillow, numpy, fontTools and FFmpeg. No gameplay assets are overwritten.
"""
from pathlib import Path
import argparse, json, math, os, shutil, subprocess, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'ArtSource/Promotional/Trailer_v01'
W, H, FPS = 720, 1280, 30
INK = (14, 17, 18)
PAPER = (233, 223, 195)
GOLD = (209, 170, 90)
BG = ROOT / 'Assets/Art/Generated/Backgrounds'
PORTRAIT = ROOT / 'ArtSource/Normalized/OpeningDialogue'
FONT = ROOT / 'Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.ttf'
TEXTS = ['一炷江湖', '六十秒后，强敌来袭', '这一局，你如何破局？', '60 秒探索', '每一步，都是选择',
         '碰怪即战', '自动交锋，时间不停', '武学搭配', '选择你的江湖路', '挑战九尾妖狐', '以本局构筑，迎战强敌',
         '六十秒探索 · 武侠自动战斗', '再来一局，换条路闯江湖', '哈基米组呈献', '实机演示', '动画演绎']

def load(path): return Image.open(path).convert('RGBA')
def cover(im, width, height, zoom=1, pan=0):
    scale = max(width/im.width, height/im.height)*zoom
    im=im.resize((round(im.width*scale), round(im.height*scale)), Image.Resampling.LANCZOS)
    x=round((im.width-width)*(.5+pan));y=(im.height-height)//2
    return im.crop((x,y,x+width,y+height))

fonts={}
def text(im, value, y, size, fill=PAPER, center=True, x=56):
    key=size
    if key not in fonts: fonts[key]=ImageFont.truetype(str(FONT),size)
    font=fonts[key];draw=ImageDraw.Draw(im)
    if center:x=(W-draw.textlength(value,font=font))/2
    draw.text((x,y),value,font=font,fill=fill,stroke_width=1,stroke_fill=INK)

def composite_at(im, layer, x, y, height, opacity=1):
    layer=layer.resize((round(layer.width*height/layer.height),round(height)),Image.Resampling.LANCZOS)
    if opacity<1:layer.putalpha(layer.getchannel('A').point(lambda a:round(a*opacity)))
    im.alpha_composite(layer,(round(x),round(y)))

def shade(im, bottom=True):
    alpha=np.zeros((H,W,4),dtype=np.uint8);alpha[:,:,:3]=INK
    for y in range(H):
        a=max(0,(y-H*.56)/(H*.44))*230 if bottom else max(0,1-y/450)*170
        alpha[y,:,3]=int(a)
    return Image.alpha_composite(im,Image.fromarray(alpha))

def frame(t,assets):
    hero,fox,mist,logo,mountain,temple=assets
    if t<6 or t>=24:
        if t<3:
            q=t/3;im=cover(temple,W,H,1.05+.045*q).convert('RGBA')
            im=Image.blend(im,Image.new('RGBA',(W,H),(20,8,10,255)),.35)
            composite_at(im,fox,-55-18*q,165-15*q,1160+35*q)
            title,sub='六十秒后，强敌来袭','一炷江湖'
        elif t<6:
            q=(t-3)/3;im=cover(mountain,W,H,1.03+.055*q).convert('RGBA')
            im=Image.blend(im,Image.new('RGBA',(W,H),INK+(255,)),.28)
            composite_at(im,hero,-50+18*q,135-22*q,1190+30*q)
            title,sub='这一局，你如何破局？','一炷江湖'
        else:
            q=(t-24)/6;im=cover(mountain,W,H,1.08+.06*q).convert('RGBA')
            im=Image.blend(im,Image.new('RGBA',(W,H),INK+(255,)),.52)
            composite_at(im,fox,260+18*q,315,760,.40)
            composite_at(im,hero,-135-15*q,280,900,.62)
            title,sub='一炷江湖','六十秒探索 · 武侠自动战斗'
        for k in range(2):composite_at(im,mist,-280+110*math.sin(t*.3+k),710+k*220,260,.6)
        im=shade(im);im=shade(im,False)
        # Warm drifting motes, kept behind the typography.
        draw=ImageDraw.Draw(im)
        for k in range(24):
            x=(k*131+t*(8+k%4))%W;y=(k*179-t*(12+k%5))%H
            draw.ellipse((x,y,x+2,y+2),fill=(181,138,70,150))
        if t<6:
            text(im,sub,108,27,GOLD);text(im,title,1010,42)
            text(im,'动画演绎',1180,19,(169,173,163))
            # Single blade sweep at the cut, no repeated flashes.
            if 2.78<t<3 or 5.78<t<6:
                p=((t%3)-2.78)/.22;x=-W+2*W*p
                draw.line((x,930,x+650,300),fill=PAPER,width=6)
                draw.line((x-10,935,x+640,305),fill=GOLD,width=2)
        else:
            text(im,title,730,88,GOLD);text(im,sub,855,28)
            text(im,'再来一局，换条路闯江湖',955,31)
            composite_at(im,logo,302,1090,100,.95)
            text(im,'哈基米组呈献',1195,22)
    else:
        if t<11:name,local,title,sub='explore',t-6,'60 秒探索','每一步，都是选择'
        elif t<14:name,local,title,sub='combat',t-11,'碰怪即战','自动交锋，时间不停'
        elif t<17:name,local,title,sub='choice',t-14,'武学搭配','选择你的江湖路'
        else:name,local,title,sub='boss',t-17,'挑战九尾妖狐','以本局构筑，迎战强敌'
        idx=min(round(local*FPS),len(list((OUT/'capture'/name).glob('*.png')))-1)
        raw=load(OUT/'capture'/name/f'{idx:04}.png')
        # Reserve editorial bands instead of covering the game's own UI.
        im=Image.new('RGBA',(W,H),INK+(255,))
        raw=raw.resize((570,1014),Image.Resampling.LANCZOS)
        im.alpha_composite(raw,(75,145))
        draw=ImageDraw.Draw(im);draw.line((75,143,645,143),fill=GOLD,width=1)
        text(im,title,54,40,GOLD);text(im,sub,1170,27)
        text(im,'实机演示',113,17,(169,173,163))
    # Short fade through ink at each edit; continuous frame pacing.
    cuts=[0,3,6,11,14,17,24,30]
    distance=min(abs(t-c) for c in cuts)
    fade=min(1,distance/.12)
    if fade<1:im=Image.blend(Image.new('RGBA',(W,H),INK+(255,)),im,fade)
    return im.convert('RGB')

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--ffmpeg',default=shutil.which('ffmpeg') or str(Path.home()/'Library/Application Support/bilibili/ffmpeg/ffmpeg'));parser.add_argument('--preview',action='store_true');args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    cmap=TTFont(FONT).getBestCmap();missing=sorted(set(''.join(TEXTS))-set(map(chr,cmap)))
    if missing:raise RuntimeError('Missing font glyphs: '+''.join(missing))
    assets=(load(PORTRAIT/'portrait_hero_v01.png'),load(PORTRAIT/'portrait_fox_v01.png'),load(ROOT/'Assets/Art/Generated/Environment/HD2D/spr_env_hd2d_mist_band_1024x256_v01.png'),load(ROOT/'Assets/Resources/UI/Branding/logo_hakimi_group_v01.png'),load(BG/'bg_mainmenu_misty_mountains_v01.png'),load(BG/'bg_boss_bloodmoon_temple_v01.png'))
    if args.preview:
        for t in [1.5,4.5,8,12,15,20,27]:frame(t,assets).save(OUT/f'preview_{t:g}.png')
        return
    report=json.loads((OUT/'capture/report.json').read_text())
    if not report['success']:raise RuntimeError('Capture did not complete successfully')
    for name,count in [('choice',90),('explore',150),('combat',90),('boss',210)]:
        assert len(list((OUT/'capture'/name).glob('*.png')))==count,(name,count)
    silent=OUT/'silent.mp4'
    cmd=[args.ffmpeg,'-y','-loglevel','error','-f','rawvideo','-pix_fmt','rgb24','-s',f'{W}x{H}','-r',str(FPS),'-i','-','-vf','scale=1080:1920:flags=lanczos','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p',str(silent)]
    proc=subprocess.Popen(cmd,stdin=subprocess.PIPE)
    try:
        for i in range(900):
            proc.stdin.write(frame(i/FPS,assets).tobytes())
            if i%150==0:print(f'Rendered {i}/900 frames',flush=True)
    finally:proc.stdin.close()
    if proc.wait()!=0:raise RuntimeError('FFmpeg encoding failed')
    music=ROOT/'Assets/Resources/Audio/Menu/bgm_menu_wuxia_punk_dj_60s_v02.wav'
    target=OUT/'一炷江湖_竖屏宣传片_v01.mp4'
    swing=ROOT/'Assets/Audio/Generated/Combat/sfx_combat_sword_swing_v01.wav'
    impact=ROOT/'Assets/Audio/Generated/Combat/sfx_combat_impact_critical_v01.wav'
    mix='[1:a]atrim=0:30,afade=t=in:d=0.6,afade=t=out:st=28:d=2,volume=0.7[m];[2:a]asplit=2[s1][s2];[s1]adelay=2780:all=1[a];[s2]adelay=5780:all=1[b];[3:a]asplit=3[i1][i2][i3];[i1]adelay=11000:all=1[c];[i2]adelay=17000:all=1[d];[i3]adelay=24000:all=1[e];[m][a][b][c][d][e]amix=inputs=6:duration=first:normalize=0,alimiter=limit=0.94:level=false[out]'
    subprocess.run([args.ffmpeg,'-y','-loglevel','error','-i',str(silent),'-i',str(music),'-i',str(swing),'-i',str(impact),'-filter_complex',mix,'-map','0:v','-map','[out]','-c:v','copy','-c:a','aac','-b:a','192k','-t','30','-movflags','+faststart',str(target)],check=True)
    print(target)

if __name__=='__main__':main()
