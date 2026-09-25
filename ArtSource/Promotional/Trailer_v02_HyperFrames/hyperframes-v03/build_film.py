"""Build the approved 90s HyperFrames composition. All timing is absolute and seek-safe."""
from pathlib import Path
import json
P=Path(__file__).parent
shots=json.loads((P.parent/'shot-manifest.json').read_text())['shots']
parts=[]
for i,s in enumerate(shots[:8]):
    src=Path(s['image']).name
    parts.append(f'''<section class="scene cg" id="s{i}" aria-label="{s['title']}">
      <div class="camera" id="cam{i}"><img class="art" src="assets/{src}" alt="{s['title']}" data-layout-ignore></div>
      <div class="atmosphere" id="air{i}" data-layout-ignore><div class="mist a"></div><div class="mist b"></div></div>
      <svg class="fx" id="fx{i}" viewBox="0 0 1080 1920" data-layout-ignore></svg>
      <div class="label">动画演绎</div></section>''')
for i,(name,title,sub,start,dur) in enumerate([
    ('explore','60 秒探索','每一步，都是选择',36,12),
    ('combat','碰怪即战','自动交锋，时间不停',48,10),
    ('choice','武学搭配','选择你的江湖路',58,10),
    ('boss','挑战九尾妖狐','以本局构筑，迎战强敌',68,14)],8):
    parts.append(f'''<section class="scene game" id="s{i}"><div class="game-heading"><h1>{title}</h1><p>{sub}</p></div>
    <div class="game-frame"><video class="clip" id="{name}" src="assets/{name}.mp4" muted playsinline preload="auto" data-start="{start}" data-duration="{dur}" data-media-start="0" data-track-index="0"></video></div>
    <div class="game-footer"><span>一炷江湖</span><span>实机演示</span></div></section>''')
parts.append('''<section class="scene end" id="s12"><div id="end-art"><img class="art" src="assets/endcard-art.png" alt="剑客与九尾妖狐交锋" data-layout-ignore></div><div class="end-panel" data-layout-ignore></div><div class="end-copy"><div id="end-rule"></div><h1 id="end-title">一炷江湖</h1><p id="end-sub">六十秒探索 · 武侠自动战斗</p><p id="end-cta">再来一局，换条路闯江湖</p><p id="end-studio">哈基米组呈献</p></div></section>''')
html='''<!doctype html><html lang="zh-CN" data-resolution="portrait"><head><meta charset="utf-8"><meta name="viewport" content="width=1080,height=1920"><title>一炷江湖 · 90秒宣传片</title><script src="assets/gsap.min.js"></script><style>
@font-face{font-family:Wuxia;src:url('assets/chinese.ttf')}*{box-sizing:border-box;margin:0;padding:0}html,body{width:1080px;height:1920px;overflow:hidden;background:#0e1112;color:#e9dfc3;font-family:Wuxia,sans-serif}#root{width:100%;height:100%;position:relative;overflow:hidden}.scene{position:absolute;inset:0;width:1080px;height:1920px;overflow:hidden;opacity:0;background:#0e1112}#s0{opacity:1}.camera,.art{position:absolute;inset:0;width:1080px;height:1920px}.art{object-fit:cover}.camera{will-change:transform}.label{position:absolute;top:138px;left:76px;font-size:24px;letter-spacing:4px;padding:10px 18px;background:#0e1112dd;border-left:2px solid #d1aa5a;color:#e9dfc3}.atmosphere,.fx{position:absolute;inset:0;width:1080px;height:1920px;pointer-events:none}.mist{position:absolute;left:-450px;width:1800px;height:390px;background:radial-gradient(ellipse at center,rgba(174,187,187,.20),rgba(138,158,166,.06) 38%,transparent 67%);transform:rotate(-12deg)}.mist.a{top:1200px}.mist.b{top:550px;opacity:.45}.game-heading{position:absolute;top:110px;left:120px;right:120px;text-align:center}.game-heading h1{font-size:60px;letter-spacing:8px;line-height:1.2;color:#e9dfc3}.game-heading p{font-size:30px;margin-top:20px;color:#d1aa5a;letter-spacing:3px}.game-frame{position:absolute;left:135px;top:260px;width:810px;height:1440px;border:1px solid #736347;overflow:hidden;background:#101515}.game-frame video{width:810px;height:1440px;object-fit:contain;display:block}.game-footer{position:absolute;left:138px;right:138px;top:1734px;display:flex;justify-content:space-between;font-size:23px;color:#b5b09f;letter-spacing:3px}.end #end-art{position:absolute;inset:0;transform-origin:60% 30%}.end-panel{position:absolute;inset:0;background:linear-gradient(to bottom,transparent 24%,rgba(14,17,18,.22) 38%,#0e1112 63%)}.end-copy{position:absolute;left:90px;right:90px;top:1120px;text-align:center}#end-rule{height:2px;width:150px;background:#d1aa5a;margin:0 auto 35px}#end-title{font-size:128px;letter-spacing:12px;line-height:1.2;color:#e9dfc3}#end-sub{font-size:32px;letter-spacing:4px;margin-top:34px;color:#d1aa5a}#end-cta{font-size:38px;margin-top:104px;letter-spacing:3px}#end-studio{font-size:26px;margin-top:70px;color:#b5b09f;letter-spacing:7px}#handoff{position:absolute;inset:0;z-index:99;pointer-events:none;overflow:hidden}#sword-wipe{position:absolute;left:-1650px;top:700px;width:3100px;height:160px;background:linear-gradient(0deg,transparent,#f1deb18a 42%,#fff4d9 48%,#fffef5 52%,#f1deb166 59%,transparent);transform:rotate(-32deg);opacity:0}
</style></head><body><div id="root" data-composition-id="main" data-start="0" data-duration="90" data-width="1080" data-height="1920">'''+''.join(parts)+'''
<div id="handoff" data-layout-ignore><div id="sword-wipe"></div></div><audio id="music-score" src="assets/score.wav" data-start="0" data-duration="90" data-track-index="1" data-volume="1" data-audio-group="music"></audio><audio id="cinematic-sfx" src="assets/effects.wav" data-start="0" data-duration="90" data-track-index="2" data-volume="1" data-audio-group="sfx"></audio></div><script>
const tl=gsap.timeline({paused:true});
const cuts=[0,4,9,12,17,21,26,30,36,48,58,68,82,90];
// Scene handoffs own the exit. No element fades out ahead of its transition.
for(let i=1;i<13;i++){
  let t=cuts[i], d=i<8?([2,3,6,7].includes(i)?.18:.5):.28;
  tl.fromTo('#s'+i,{opacity:0},{opacity:1,duration:d,ease:'power2.inOut'},t);
  tl.to('#s'+(i-1),{opacity:0,duration:d,ease:'power2.inOut'},t);
}
function cam(i,poses){for(const [at,dur,from,to,ease] of poses)tl.fromTo('#cam'+i,from,{...to,duration:dur,ease:ease||'sine.inOut',immediateRender:false},cuts[i]+at);}
cam(0,[[0,4.5,{scale:1.10,x:12,y:35},{scale:1.22,x:35,y:-35}]]);
cam(1,[[0,2.6,{scale:1.22,x:30,y:-100},{scale:1.08,x:0,y:18},'power2.out'],[2.6,2.6,{scale:1.08,x:0,y:18},{scale:1.17,x:-25,y:92},'power2.in']]);
cam(2,[[0,3.3,{scale:1.15,x:28,y:0},{scale:1.23,x:-18,y:-18},'power1.in']]);
cam(3,[[0,1.25,{scale:1.28,x:35,y:-70},{scale:1.11,x:0,y:5},'power3.out'],[1.25,4,{scale:1.11,x:0,y:5},{scale:1.23,x:-45,y:48},'power1.in']]);
cam(4,[[0,4.5,{scale:1.06,x:0,y:0},{scale:1.16,x:35,y:-20},'sine.inOut']]);
cam(5,[[0,3,{scale:1.28,x:0,y:-140},{scale:1.1,x:0,y:32},'power2.out'],[3,2.3,{scale:1.1,x:0,y:32},{scale:1.15,x:0,y:85},'sine.inOut']]);
cam(6,[[0,4.3,{scale:1.12,x:-20,y:5,rotation:-2},{scale:1.2,x:22,y:12,rotation:1},'power1.in']]);
cam(7,[[0,2.9,{scale:1.05,x:0,y:0},{scale:1.13,x:-20,y:25},'power2.in'],[2.9,.32,{scale:1.13,x:-20,y:25},{scale:1.24,x:-55,y:70},'power4.out'],[3.22,3.2,{scale:1.24,x:-55,y:70},{scale:1.1,x:-10,y:15},'power2.out']]);
const NS='http://www.w3.org/2000/svg';
function el(parent,tag,attrs){const e=document.createElementNS(NS,tag);for(const k in attrs)e.setAttribute(k,attrs[k]);parent.appendChild(e);return e;}
for(let i=0;i<8;i++){
 const st=cuts[i],dur=cuts[i+1]-st+.5,fx=document.querySelector('#fx'+i);
 tl.fromTo('#s'+i+' .label',{opacity:0,x:-12},{opacity:1,x:0,duration:.6,immediateRender:false},st+.25);
 tl.fromTo('#air'+i,{x:-120,y:25},{x:115,y:-40,duration:dur,ease:'none',immediateRender:false},st);
 // Deterministic near-field embers/rain. Each particle has its own depth/speed.
 for(let j=0;j<26;j++){
   const x=(j*277+i*109)%1200-60,y=(j*491+i*137)%2050-70;
   const rain=i===3||i===4; const e=rain?el(fx,'line',{x1:x,y1:y,x2:x-8,y2:y+45,stroke:'#d0dbe3','stroke-width':1+(j%2),opacity:.2}):el(fx,'ellipse',{cx:x,cy:y,rx:1.2+j%3,ry:2+j%4,fill:i===0?'#edaa58':'#da7546',opacity:.15+(j%5)*.07});
   tl.fromTo(e,{x:0,y:0},{x:rain?-160:80+(j%4)*32,y:rain?900:-280-(j%4)*80,duration:dur,ease:'none',immediateRender:false},st);
 }
 if(i===1||i===3||i===5){for(let j=0;j<7;j++){
   let x=(j*197+74)%1100,y=(j*347+130)%1900;
   let p=el(fx,'path',{d:`M${x} ${y}l28 8 -9 34 -17 -9z`,fill:i===5?'#933e33':'#c5bda1',opacity:.42});
   tl.fromTo(p,{x:-120,y:130,rotation:-18,transformOrigin:'50% 50%'},{x:190,y:-230,rotation:50,duration:dur,ease:'none',immediateRender:false},st);
 }}
}
// Incense wisps: flowing narrow lines, distinct from the slower atmospheric plane.
for(let j=0;j<4;j++){
 const p=el(document.querySelector('#fx0'),'path',{d:`M310 840 C${240+j*20} 610 ${490-j*25} 430 320 150`,fill:'none',stroke:'#d9ded9','stroke-width':1+j,opacity:.14});
 tl.fromTo(p,{x:-8,y:40},{x:25+j*7,y:-70,opacity:.04,duration:4.4,ease:'sine.inOut'},0);
}
function strike(fx,d,t,dur,color,width){const p=el(document.querySelector(fx),'path',{d,fill:'none',stroke:color,'stroke-width':width,'stroke-linecap':'round',pathLength:1,'stroke-dasharray':1,'stroke-dashoffset':1,opacity:0});tl.fromTo(p,{strokeDashoffset:1,opacity:0},{strokeDashoffset:0,opacity:.85,duration:dur,ease:'power2.in'},t);tl.to(p,{opacity:0,duration:.5},t+dur);return p;}
strike('#fx2','M565 795 L860 960',9.65,.3,'#fff3d4',4);
strike('#fx3','M730 780 L1070 1030',15.1,.4,'#e3eddf',3);
// Three foxfire attacks advance along the spatial directions in the artwork.
['M735 425 C565 590 330 740 100 985','M758 430 C585 725 268 985 400 1260','M781 438 C638 745 677 904 796 1015'].forEach((d,j)=>{strike('#fx6',d,26.4+j*.78,.8,'#e75836',14);strike('#fx6',d,26.44+j*.78,.76,'#ffe1ae',4);});
strike('#fx7','M240 1220 Q590 980 694 800',32.45,.62,'#ffe9b7',10);
strike('#fx7','M275 1330 Q900 1280 690 800',32.75,.45,'#fff3d8',5);
const ring=el(document.querySelector('#fx7'),'circle',{cx:690,cy:820,r:80,fill:'none',stroke:'#f4d79d','stroke-width':4,opacity:0});
tl.fromTo(ring,{scale:.15,opacity:0,transformOrigin:'50% 50%'},{scale:3.7,opacity:.5,duration:.35,ease:'power3.out'},33.15).to(ring,{scale:5,opacity:0,duration:.55,ease:'power2.out'},33.5);
for(let j=0;j<28;j++){
 let a=j*2.399,rx=Math.cos(a)*(250+j*16),ry=Math.sin(a)*(350+j*20);
 let p=el(document.querySelector('#fx7'),'path',{d:'M690 820l7 -12 4 19z',fill:j%3?'#edc27b':'#393633',opacity:0});
 tl.fromTo(p,{x:0,y:0,opacity:0},{x:rx,y:ry,opacity:.8,duration:.65+(j%5)*.12,ease:'power3.out'},33.2);
 tl.to(p,{opacity:0,duration:.65},34.1+(j%5)*.06);
}
tl.fromTo('#sword-wipe',{x:-200,y:700,opacity:0},{x:1800,y:-500,opacity:1,duration:.42,ease:'power2.in'},35.66);
tl.to('#sword-wipe',{opacity:0,duration:.25},36.08);
for(let i=8;i<12;i++){
 tl.fromTo('#s'+i+' .game-heading',{y:22,opacity:0},{y:0,opacity:1,duration:.55,ease:'power2.out',immediateRender:false},cuts[i]+.2);
 tl.fromTo('#s'+i+' .game-footer',{opacity:0},{opacity:1,duration:.65,immediateRender:false},cuts[i]+.55);
}
tl.fromTo('#end-art',{scale:1.10,y:0},{scale:1.18,y:-60,duration:8,ease:'sine.inOut'},82);
tl.fromTo('#end-rule',{scaleX:0},{scaleX:1,duration:.85,ease:'power2.out'},82.4);
tl.fromTo('#end-title',{opacity:0,y:35,scale:1.05},{opacity:1,y:0,scale:1,duration:1.1,ease:'power3.out'},82.65);
tl.fromTo('#end-sub',{opacity:0,y:15},{opacity:1,y:0,duration:.8},83.25);
tl.fromTo('#end-cta',{opacity:0,y:22},{opacity:1,y:0,duration:.85,ease:'power2.out'},86);
tl.fromTo('#end-studio',{opacity:0},{opacity:1,duration:.8},86.7);
tl.to({}, {duration:.01},89.99);
window.__timelines=window.__timelines||{};window.__timelines.main=tl;
</script></body></html>'''
(P/'index.html').write_text(html)
print('Built',P/'index.html')
