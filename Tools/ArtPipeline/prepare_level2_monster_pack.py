#!/usr/bin/env python3
"""Pack already transparent generated art; do not paint, key or synthesize pixels.

Run: uv run --with pillow --with numpy --with scipy python Tools/ArtPipeline/prepare_level2_monster_pack.py
Input metadata: ArtSource/Raw/Monsters/LevelTwoPack/manifest.json
"""
import json
import argparse
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from scipy.ndimage import label, find_objects

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / 'ArtSource/Raw/Monsters/LevelTwoPack'
OUT = ROOT / 'Assets/Art/Generated/Characters/Enemies/LevelTwoPack'
PREVIEW = ROOT / 'ArtSource/Previews/Monsters/LevelTwoPack'


def extract(path, projectile_frames=(), floating=False):
    image = Image.open(path)
    if image.mode != 'RGBA' or image.getchannel('A').getextrema()[0] != 0:
        raise ValueError(f'Expected actual transparent RGBA input: {path}')
    data = np.array(image)
    labels, count = label(data[:, :, 3] > 24)
    counts = np.bincount(labels.ravel())
    boxes = find_objects(labels)
    bodies = sorted(range(1, count+1), key=lambda i: counts[i], reverse=True)[:8]
    if len(bodies) != 8 or min(counts[bodies]) < max(counts[bodies]) * .3:
        raise ValueError(f'Expected eight disconnected full characters: {path}')
    bodies.sort(key=lambda i: boxes[i-1][1].start)
    frames = []
    for index, ident in enumerate(bodies):
        ys, xs = boxes[ident-1]
        body = labels == ident
        # Bottom contact band anchors feet independently of weapon/attack reach.
        foot_x = np.where(body[max(ys.start, ys.stop-8):ys.stop])[1]
        anchor_x = (float(foot_x.min()) + float(foot_x.max())) / 2
        if floating:
            mid_y = ys.start + round((ys.stop-ys.start)*.4)
            core_x = np.where(body[mid_y:mid_y+8])[1]
            anchor_x = (float(core_x.min())+float(core_x.max()))/2
        anchor_y = ys.stop
        mask = body.copy()
        if index in projectile_frames:
            for extra in range(1, count+1):
                if extra in bodies or counts[extra] < 200:
                    continue
                ey, ex = boxes[extra-1]
                cx = (ex.start+ex.stop)/2
                # Detached rightward flame or venom belongs to the preceding body.
                centers = [(boxes[b-1][1].start+boxes[b-1][1].stop)/2 for b in bodies]
                owner = max((n for n,c in enumerate(centers) if c < cx), default=-1)
                if owner == index and cx-centers[index] < image.width/8:
                    mask |= labels == extra
        pixels = np.zeros_like(data)
        pixels[mask] = data[mask]
        sprite = Image.fromarray(pixels)
        box = sprite.getbbox()
        frames.append((sprite.crop(box), anchor_x-box[0], anchor_y-box[1]))
    return frames


def pack(idle, attack):
    # Generation requests may return different source scales. Match neutral-frame height,
    # then use ONE shared scale for every pose within each source strip, never per-frame fit.
    ratio = idle[0][0].height / attack[0][0].height
    sources = [(idle, 1.0), (attack, ratio)]
    left = right = top = 0
    for frames, factor in sources:
        for image, x, y in frames:
            left = max(left, x*factor); right = max(right, (image.width-x)*factor)
            top = max(top, y*factor)
    scale = min(116/max(left,1),116/max(right,1),216/max(top,1),184/idle[0][0].height)
    output = []
    for frames, factor in sources:
        target = []
        for image, x, y in frames:
            s = scale*factor
            resized = image.resize((max(1,round(image.width*s)),max(1,round(image.height*s))),Image.Resampling.NEAREST)
            canvas = Image.new('RGBA',(256,256))
            canvas.alpha_composite(resized,(128-round(x*s),224-round(y*s)))
            box = canvas.getbbox()
            assert box and box[0]>=8 and box[1]>=7 and box[2]<=248 and box[3]<=225,box
            target.append(canvas)
        output.append(target)
    output[0][-1] = output[0][0].copy()
    output[1][0] = output[0][0].copy()
    output[1][-1] = output[0][0].copy()
    return output, scale, ratio


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--only',help='Normalize one species during asset inspection.')
    args=parser.parse_args()
    rows = json.loads((RAW/'manifest.json').read_text())
    if args.only: rows=[r for r in rows if r['id']==args.only]
    PREVIEW.mkdir(parents=True,exist_ok=True)
    report=[]; previews=[]
    for row in rows:
        ident=row['id']
        floating=ident=='lantern_wraith'
        attack=extract(ROOT/row['attack'],(4,5) if floating else ((5,) if ident=='scarlet_viper' else ()),floating)
        if row.get('idle_mode')=='neutral_pair':
            # Reuse approved neutral endpoints instead of shipping a mismatched generated costume.
            # This is an eight-slot, two-pose idle hold, not eight unique breathing drawings.
            idle=[attack[i] for i in [0,0,7,7,7,7,0,0]]
        else:
            idle=extract(ROOT/row['idle'],floating=floating)
        (idle,attack),scale,ratio=pack(idle,attack)
        folder=OUT/ident;folder.mkdir(parents=True,exist_ok=True)
        for action,frames in [('idle',idle),('attack',attack)]:
            sheet=Image.new('RGBA',(2048,256))
            for i,frame in enumerate(frames):sheet.alpha_composite(frame,(i*256,0))
            sheet.save(folder/f'spr_enemy_{ident}_{action}_right_8f_v01.png')
        board=Image.new('RGB',(1024,280),(30,34,39));draw=ImageDraw.Draw(board);draw.text((8,5),ident,fill='white')
        for r,frames in enumerate([idle,attack]):
            for c,im in enumerate(frames):
                im=im.resize((128,128),Image.Resampling.NEAREST);board.paste(im,(c*128,24+r*128),im)
        board.save(PREVIEW/f'{ident}_sheet.png');previews.append(board)
        anim=[]
        for frame in idle+attack+idle[:4]:
            bg=Image.new('RGB',(256,256),(45,49,54));bg.paste(frame,(0,0),frame);anim.append(bg)
        anim[0].save(PREVIEW/f'{ident}.gif',save_all=True,append_images=anim[1:],duration=[125]*8+[83]*8+[125]*4,loop=0,disposal=2)
        report.append(dict(id=ident,idle_mode=row.get('idle_mode','eight_pose'),shared_scale=scale,attack_source_scale_ratio=ratio,frames=16,size=[2048,256],status='Normalized',alpha='RGBA',anchor=[128,224]))
        print(ident,'normalized',round(scale,4),round(ratio,4))
    combined=Image.new('RGB',(1024,len(previews)*280))
    for i,p in enumerate(previews):combined.paste(p,(0,i*280))
    combined.save(PREVIEW/'all_monsters.png')
    (PREVIEW/'normalization_report.json').write_text(json.dumps(report,indent=2)+'\n')


if __name__=='__main__':main()
