// Technical preparation of AI-generated candidates for an isolated art preview.
// No creative drawing and no Unity asset replacement. Raw files are retained.
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
const root = __dirname;
const repo = path.resolve(root, '../../../../../../');
const generated = '/Users/gongyuyang/.codex/generated_images/01a07188-0191-74c3-8be5-b78ca5ecc039';
const sources = {
  double: 'exec-ec30e6ea-d37c-44ad-a84c-bb34edba0162.png',
  guard: 'exec-7815e977-c6bc-4f9a-9ba0-c29ca7877b49.png',
  slashes: 'exec-eeb9079c-0678-4d73-aeed-2b9c7d2c28e4.png',
  ward: 'exec-22c16c4e-2404-47eb-8ede-a6e2e75d29a8.png'
};
const report = { status: 'Preview prepared; not imported or approved', assets: {} };
fs.mkdirSync(path.join(root, 'raw'), {recursive:true});

async function character(key) {
  const file=path.join(root,'raw',key+'.png');
  const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const w=info.width,h=info.height;
  // The generator baked a near-white neutral checkerboard into RGB character sheets.
  // This key is only for these candidates, not a general-purpose production matte.
  for(let p=0;p<data.length;p+=4){
    const lo=Math.min(data[p],data[p+1],data[p+2]);
    const hi=Math.max(data[p],data[p+1],data[p+2]);
    if(lo>195 && hi-lo<24)data[p+3]=0;
  }
  const seen=new Uint8Array(w*h),components=[];
  for(let start=0;start<w*h;start++){
    if(seen[start] || data[start*4+3]<100)continue;
    const points=[start];seen[start]=1;
    let minx=w,miny=h,maxx=0,maxy=0;
    for(let j=0;j<points.length;j++){
      const id=points[j],x=id%w,y=Math.floor(id/w);
      minx=Math.min(minx,x);maxx=Math.max(maxx,x);miny=Math.min(miny,y);maxy=Math.max(maxy,y);
      for(const n of [x>0?id-1:-1,x<w-1?id+1:-1,y>0?id-w:-1,y<h-1?id+w:-1]){
        if(n>=0&&!seen[n]&&data[n*4+3]>=100){seen[n]=1;points.push(n);}
      }
    }
    if(points.length>600)components.push({points,minx,miny,maxx,maxy});
  }
  components.sort((a,b)=>b.points.length-a.points.length);
  if(components.length<8)throw Error(key+': fewer than eight separate characters');
  const chosen=components.slice(0,8).sort((a,b)=>a.minx-b.minx);
  const maxW=Math.max(...chosen.map(c=>c.maxx-c.minx+1));
  const maxH=Math.max(...chosen.map(c=>c.maxy-c.miny+1));
  const standing=Math.max(chosen[0].maxy-chosen[0].miny+1,chosen[7].maxy-chosen[7].miny+1);
  const scale=Math.min(150/standing,236/maxW,216/maxH);
  const layers=[],bounds=[];
  for(let i=0;i<8;i++){
    const c=chosen[i],cw=c.maxx-c.minx+1,ch=c.maxy-c.miny+1;
    const buf=Buffer.alloc(cw*ch*4);
    let footMin=w,footMax=0;
    for(const id of c.points){
      const x=id%w,y=Math.floor(id/w),out=((y-c.miny)*cw+x-c.minx)*4;
      data.copy(buf,out,id*4,id*4+4);
      if(y>=c.maxy-9){footMin=Math.min(footMin,x);footMax=Math.max(footMax,x);}
    }
    const nw=Math.round(cw*scale),nh=Math.round(ch*scale);
    const foot=(footMin+footMax)/2-c.minx;
    const left=Math.max(4,Math.min(252-nw,Math.round(128-foot*scale)));
    const top=224-nh;
    const png=await sharp(buf,{raw:{width:cw,height:ch,channels:4}}).resize(nw,nh,{kernel:'nearest'}).png().toBuffer();
    layers.push({input:png,left:i*256+left,top});
    bounds.push({source:[c.minx,c.miny,c.maxx+1,c.maxy+1],destination:[left,top,nw,nh],footBaseline:224});
  }
  await sharp({create:{width:2048,height:256,channels:4,background:'#00000000'}}).composite(layers).png().toFile(path.join(root,key+'_preview_strip.png'));
  report.assets[key]={source:[w,h],frames:8,output:[2048,256],scale,bounds,alpha:true,note:'Preview matte; silver edges and pose continuity still need art QA'};
}

async function vfx(key){
  const {data,info}=await sharp(path.join(root,'raw',key+'.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const {width:w,height:h}=info;
  const cells=[];
  for(let i=0;i<6;i++){
    const x0=Math.round(i*w/6),x1=Math.round((i+1)*w/6);
    let minx=x1,miny=h,maxx=x0,maxy=0;
    for(let y=0;y<h;y++)for(let x=x0;x<x1;x++)if(data[(y*w+x)*4+3]>40){minx=Math.min(x,minx);maxx=Math.max(x,maxx);miny=Math.min(y,miny);maxy=Math.max(y,maxy);}
    cells.push({x0,x1,minx,miny,maxx,maxy});
  }
  // One scale and one vertical origin per strip preserve growth/dissipation.
  const top=Math.min(...cells.map(c=>c.miny)),bottom=Math.max(...cells.map(c=>c.maxy));
  const scale=Math.min(220/(w/6),220/(bottom-top+1));
  const layers=[];
  for(let i=0;i<6;i++){
    const c=cells[i];
    const png=await sharp(path.join(root,'raw',key+'.png')).extract({left:c.x0,top,width:c.x1-c.x0,height:bottom-top+1}).resize(Math.round((c.x1-c.x0)*scale),Math.round((bottom-top+1)*scale),{kernel:'nearest'}).png().toBuffer();
    layers.push({input:png,left:i*256+Math.round((256-(c.x1-c.x0)*scale)/2),top:Math.round((256-(bottom-top+1)*scale)/2)});
  }
  await sharp({create:{width:1536,height:256,channels:4,background:'#00000000'}}).composite(layers).png().toFile(path.join(root,key+'_preview_strip.png'));
  report.assets[key]={source:[w,h],frames:6,output:[1536,256],scale,alpha:true};
}

(async()=>{
  for(const [key,name]of Object.entries(sources)){
    const dest=path.join(root,'raw',key+'.png');
    if(!fs.existsSync(dest))fs.copyFileSync(path.join(generated,name),dest);
  }
  await character('double');await character('guard');await vfx('slashes');await vfx('ward');
  const assets={
    'idle.png':'Assets/Art/Generated/Characters/Bosses/XuanjiaGateWarden/spr_boss_xuanjia_gate_warden_idle_left_1f_v01.png',
    'mountain.png':'Assets/Art/Generated/Characters/Bosses/XuanjiaGateWarden/spr_boss_xuanjia_gate_warden_skill_mountain_breaker_left_8f_v01.png',
    'mountain_vfx.png':'Assets/Art/Generated/Effects/XuanjiaGateWarden/spr_vfx_midboss_mountain_breaker_6f_v01.png',
    'stage.png':'Assets/Art/Generated/Backgrounds/bg_battle_north_pass_v01.png',
    'font.ttf':'Assets/Resources/Fonts/NotoSansCJKsc-Regular-Subset.ttf',
    'OFL-NotoSansCJK.txt':'Assets/Resources/Fonts/OFL-NotoSansCJK.txt'
  };
  for(const [name,src]of Object.entries(assets))fs.copyFileSync(path.join(repo,src),path.join(root,name));
  fs.writeFileSync(path.join(root,'qa.json'),JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify(report,null,2));
})().catch(e=>{console.error(e);process.exit(1)});
