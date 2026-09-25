"""Original 90-second cinematic wuxia score and timed SFX; deterministic synthesis."""
from pathlib import Path
import sys, wave, json
import numpy as np
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'Tools/Audio'))
from generate_menu_wuxia_music import pluck,xiao
from generate_menu_wuxia_punk_dj import bass, electronic_kick,electronic_snare,hi_hat
R=44100; N=R*90
music=np.zeros((N,2)); effects=np.zeros_like(music)
def add(bus,sound,start,gain=.2,pan=0):
    k=round(start*R); size=min(len(sound),N-k)
    if size<=0:return
    bus[k:k+size]+=sound[:size,None]*gain*np.array([np.cos((pan+1)*np.pi/4),np.sin((pan+1)*np.pi/4)])
def boom(d=2,seed=4):
    t=np.arange(round(d*R))/R; rng=np.random.default_rng(seed)
    n=rng.normal(0,1,len(t)); n=np.convolve(n,np.ones(55)/55,'same')
    return (np.sin(2*np.pi*(47*t+24*(1-np.exp(-t*12))/12))*np.exp(-t*3.5)+n*.5*np.exp(-t*5))*(1-np.exp(-t*150))
def wind(d=1,seed=4):
    t=np.arange(round(d*R))/R; rng=np.random.default_rng(seed)
    n=rng.normal(0,1,len(t));n=np.convolve(n,np.ones(17)/17,'same')
    return n*np.sin(np.pi*t/d)**2
def pad(note,d):
    t=np.arange(round(d*R))/R; f=440*2**((note-69)/12)
    return (np.sin(2*np.pi*f*t)+.35*np.sin(2*np.pi*f*1.002*t)+.18*np.sin(2*np.pi*f*2*t))*np.minimum(t/.6,1)*np.minimum((d-t)/1.5,1)
# Two-second bars at 120 BPM line up with the CG/gameplay handoff at 36 s.
notes=[62,65,69,67,65,62,60,57]
for bar in range(45):
    s=bar*2; root=[38,38,41,41,36,36,38,38][bar%8]
    add(music,pad(root,3.4),s,.068)
    for j in range(4):
        add(music,pluck(notes[(bar*2+j)%8],2.4),s+j*.5,.105 if s<12 or s>=82 else .073,(-1)**j*.32)
    if 12<=s<17 or 22<=s<82:
        strength=.42 if s>=36 else .32
        for j in range(4):
            add(music,electronic_kick(),s+j*.5,strength)
            if j in [1,3]:add(music,electronic_snare(),s+j*.5,.16)
            add(music,hi_hat(),s+j*.5+.25,.07,.3)
            add(music,bass(root,.35),s+j*.5+.25,.16,-.08)
    if bar in [2,5,12,19,23,27,31,35,39,42]:
        for j,n in enumerate([69,67,65,62]):add(music,xiao(n,.85,bar*7+j),s+j*.9,.11,.12)
for s in [4,12,21,26,30,33.2,36,48,58,68,82]:
    add(effects,boom(seed=int(s*10)),s,.38 if s in [33.2,36,68,82] else .2)
    if s>0:add(effects,wind(.65,seed=int(s)),s-.65,.48,-.25)
for s in [9.5,11,15.5,26.3,27.4,28.5,32.4,34.7,35.3]:
    add(effects,wind(.75,int(s*10)),s,.65,.25)
# Subtle rain/air through the opening, finite room tails, no wraparound loops.
add(effects,wind(36,998),0,.11)
dry=music.copy()
for d,g in [(.173,.09),(.337,.07),(.619,.05),(.911,.03)]:
    k=round(d*R);music[k:]+=dry[:-k,::-1]*g
t=np.arange(N)/R
env=np.interp(t,[0,.8,17,18,20.5,21.5,32.5,33,35.5,36,81.9,82.5,88,90],[.15,1,1,.48,.48,1,1,.4,.6,1,1,.8,.8,0])
music*=env[:,None]
mix=music+effects
gain=10**(-1.5/20)/np.max(np.abs(mix));music*=gain;effects*=gain
out=Path(__file__).parent/'assets'
for name,data in [('score.wav',music),('effects.wav',effects)]:
    with wave.open(str(out/name),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(R);w.writeframes(np.rint(np.clip(data,-1,1)*32767).astype('<i2').tobytes())
mix=music+effects
report={'duration':90,'sample_rate':R,'bpm':120,'provenance':'original deterministic synthesis; no external recordings','peak_dbfs':float(20*np.log10(np.max(np.abs(mix)))),'rms_dbfs':float(20*np.log10(np.sqrt(np.mean(mix**2)))),'clipped_samples':int((np.abs(mix)>=1).sum())}
(out/'audio-report.json').write_text(json.dumps(report,indent=2));print(report)
