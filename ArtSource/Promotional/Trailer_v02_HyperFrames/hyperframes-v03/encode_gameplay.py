"""Encode real Editor captures. No interpolation or gameplay speed changes."""
from pathlib import Path
import subprocess,json
P=Path(__file__).parent
FF=str(P/'.bin/ffmpeg')
C=P.parent/'cg-v03/capture'
def encode(name,source,frames,start=0):
    out=P/'assets'/f'{name}.mp4'
    subprocess.run([FF,'-hide_banner','-loglevel','error','-y','-framerate','30','-start_number',str(start),'-i',str(source/'%04d.png'),'-frames:v',str(frames),'-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(out)],check=True)
    print(name,frames/30,flush=True)
if __name__=='__main__':
    import sys
    target=sys.argv[1] if len(sys.argv)>1 else 'base'
    if target=='base':
        encode('explore',C/'explore',360)
        encode('choice',C/'choice',300)
    elif target=='boss':encode('boss',P.parent/'cg-v03/capture-boss/boss',420)
    elif target=='combat':
        encode('combat-a',C/'combat',165)
        encode('combat-b',P.parent/'cg-v03/capture-combat/combat',135)
        listing=P/'combat-concat.txt';listing.write_text("file 'assets/combat-a.mp4'\nfile 'assets/combat-b.mp4'\n")
        subprocess.run([FF,'-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i',str(listing),'-c','copy','-movflags','+faststart',str(P/'assets/combat.mp4')],check=True)
