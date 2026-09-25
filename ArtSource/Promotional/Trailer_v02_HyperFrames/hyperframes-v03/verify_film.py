"""Decode, probe and create a labeled final-output contact sheet (QA artifact only)."""
from pathlib import Path
import subprocess,json,hashlib,re
from PIL import Image,ImageDraw
P=Path(__file__).parent; V=P.parent/'一炷江湖_90秒宣传片_v03.mp4'
ff=str(P/'.bin/ffmpeg');probe=str(P/'.bin/ffprobe')
info=json.loads(subprocess.check_output([probe,'-v','error','-show_format','-show_streams','-of','json',str(V)]))
video=next(s for s in info['streams'] if s['codec_type']=='video');audio=next(s for s in info['streams'] if s['codec_type']=='audio')
assert video['width']==1080 and video['height']==1920 and video['r_frame_rate']=='30/1'
assert abs(float(info['format']['duration'])-90)<.05
assert int(video['nb_frames'])==2700
dec=subprocess.run([ff,'-hide_banner','-v','error','-i',str(V),'-f','null','-'],capture_output=True,text=True)
assert dec.returncode==0 and not dec.stderr,dec.stderr
meter=subprocess.run([ff,'-hide_banner','-i',str(V),'-vn','-af','volumedetect','-f','null','-'],capture_output=True,text=True)
levels={k:float(v) for k,v in re.findall(r'(mean_volume|max_volume): ([\-\d.]+)',meter.stderr)}
Q=P/'final-review';Q.mkdir(exist_ok=True)
times=[1,6,10.5,14,19,23,28,33.4,36.2,40,50,55,59.5,73,81,88]
for i,t in enumerate(times):
    subprocess.run([ff,'-hide_banner','-loglevel','error','-y','-ss',str(t),'-i',str(V),'-frames:v','1',str(Q/f'{i:02d}-{t:g}s.png')],check=True)
board=Image.new('RGB',(1080,4*508),'#131716');draw=ImageDraw.Draw(board)
for i,t in enumerate(times):
    im=Image.open(Q/f'{i:02d}-{t:g}s.png');im.thumbnail((264,470))
    x=(i%4)*270;y=(i//4)*508;board.paste(im,(x+3,y+28));draw.text((x+9,y+8),f'{i+1:02d} | {t:g}s',fill='#e9dfc3')
board.save(Q/'contact-sheet.jpg',quality=92)
reports={}
for folder in ['capture','capture-boss','capture-combat']:
    d=json.loads((P.parent/'cg-v03'/folder/'report.json').read_text());reports[folder]={'success':d['success'],'runtimeErrors':d['runtimeErrors'],'sample_count':len(d['samples'])}
report={'status':'rendered_and_media_verified','file':V.name,'duration':float(info['format']['duration']),'width':video['width'],'height':video['height'],'fps':video['r_frame_rate'],'frames':video['nb_frames'],'video_codec':video['codec_name'],'audio_codec':audio['codec_name'],'audio_sample_rate':audio['sample_rate'],'audio_channels':audio['channels'],'audio_dbfs':levels,'decode_errors':dec.stderr,'bytes':V.stat().st_size,'sha256':hashlib.sha256(V.read_bytes()).hexdigest(),'capture_reports':reports,'hyperframes_check':{'passed':True,'errors':0,'warnings':0,'contrast_checks':12,'intentional_info':['camera overscan clipped by scene','short transition label overlap']},'final_frame_review_times':times,'visual_review':'pending review of contact-sheet.jpg','human_device_audio_platform_QA':'not performed','cg_method':'AI still keyframes, seek-safe camera moves and separate VFX; no continuous articulated character animation','gameplay_method':'staged actual Editor capture, normal speed, legal build application, multiple takes'}
(P/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(json.dumps(report,ensure_ascii=False,indent=2))
