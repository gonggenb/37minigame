// Asset normalization only; basic poses come from the user, variant poses from imagegen.
// Cutout/foot-anchor helper follows prepare_hero_eight_directions.cjs.
const fs=require('fs'), path=require('path'), sharp=require('sharp');
const root=path.resolve(__dirname,'../..');
const median = a => a.sort((a,b)=>a-b)[Math.floor(a.length/2)];

// Remove only bright neutral background connected to the cell boundary.
// White clothing enclosed by the dark outline remains opaque.
async function cut(image, rect, mask, allowEdge=false) {
  const original=await sharp(image).extract(rect).ensureAlpha().raw().toBuffer();
  const {data, info} = await sharp(image).extract(rect).flatten({background:'#ffffff'}).removeAlpha().raw().toBuffer({resolveWithObject:true});
  const hasAlpha=original.some((v,i)=>i%4===3&&v<255);
  const w=info.width, h=info.height, count=w*h, seen=new Uint8Array(count), queue=new Int32Array(count);
  // Source-local separation at a touching sword-tip/neighbor boot, reviewed in the raw sheet.
  if(mask)for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(mask(x+rect.left,y+rect.top))
    data.fill(255,(y*w+x)*3,(y*w+x)*3+3);
  let head=0, tail=0;
  function visit(i) {
    if(seen[i]) return;
    const p=i*3, lo=Math.min(data[p],data[p+1],data[p+2]), hi=Math.max(data[p],data[p+1],data[p+2]);
    if(lo>=190 && hi-lo<=27) { seen[i]=1; queue[tail++]=i; }
  }
  if(hasAlpha)for(let i=0;i<count;i++)if(original[i*4+3]<20)seen[i]=1;
  for(let x=0;x<w;x++){ visit(x); visit((h-1)*w+x); }
  for(let y=0;y<h;y++){ visit(y*w); visit(y*w+w-1); }
  while(head<tail){const i=queue[head++],x=i%w,y=Math.floor(i/w); if(x)visit(i-1);if(x<w-1)visit(i+1);if(y)visit(i-w);if(y<h-1)visit(i+w);}
  // Keep the principal connected silhouette, removing isolated background specks.
  const labels=new Int32Array(count); let best=0,bestSize=0,label=0;
  for(let i=0;i<count;i++) if(!seen[i]&&!labels[i]) {
    label++;head=0;tail=1;queue[0]=i;labels[i]=label;
    while(head<tail){ const p=queue[head++],x=p%w,y=Math.floor(p/w);
      for(const n of [x?p-1:-1,x<w-1?p+1:-1,y?p-w:-1,y<h-1?p+w:-1])
        if(n>=0&&!seen[n]&&!labels[n]){labels[n]=label;queue[tail++]=n;}
    }
    if(tail>bestSize){bestSize=tail;best=label;}
  }
  let x0=w,y0=h,x1=0,y1=0; const rgba=Buffer.alloc(count*4);
  for(let i=0;i<count;i++) if(labels[i]===best){
    const x=i%w,y=Math.floor(i/w);x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y);
    rgba[i*4]=hasAlpha?original[i*4]:data[i*3];rgba[i*4+1]=hasAlpha?original[i*4+1]:data[i*3+1];rgba[i*4+2]=hasAlpha?original[i*4+2]:data[i*3+2];rgba[i*4+3]=hasAlpha?original[i*4+3]:255;
  }
  if((!allowEdge&&(x0===0||x1===w-1||y0===0||y1===h-1))||bestSize<1000) throw Error('Clipped or empty sprite '+JSON.stringify({image,rect,box:[x0,y0,x1,y1],bestSize}));
  // Waist center is more stable than hair/stride silhouette center.
  const centers=[];
  for(let y=Math.round(y0+(y1-y0)*.50);y<=Math.round(y0+(y1-y0)*.65);y++){
    const xs=[];for(let x=x0;x<=x1;x++)if(rgba[(y*w+x)*4+3])xs.push(x);
    if(xs.length)centers.push(median(xs));
  }
  const anchorX=median(centers);
  // Enclosed checkerboard islands inside ponytail loops are also background.
  // Restrict this to the upper silhouette outside the head/torso center;
  // the white lapels, cuffs, boot bands and tiny eye glints stay intact.
  const checked=new Uint8Array(count);
  function pale(i){const p=i*4;return rgba[p+3]&&Math.min(rgba[p],rgba[p+1],rgba[p+2])>=210&&Math.max(rgba[p],rgba[p+1],rgba[p+2])-Math.min(rgba[p],rgba[p+1],rgba[p+2])<20;}
  for(let i=0;i<count;i++)if(!checked[i]&&pale(i)){
    head=0;tail=1;queue[0]=i;checked[i]=1;let sx=0,sy=0;
    while(head<tail){const p=queue[head++],x=p%w,y=Math.floor(p/w);sx+=x;sy+=y;
      for(const n of [x?p-1:-1,x<w-1?p+1:-1,y?p-w:-1,y<h-1?p+w:-1])if(n>=0&&!checked[n]&&pale(n)){checked[n]=1;queue[tail++]=n;}
    }
    if((tail>18&&sy/tail<y0+(y1-y0)*.48&&Math.abs(sx/tail-anchorX)>(y1-y0)*.08) ||
       (!hasAlpha&&tail>400&&sy/tail>y0+(y1-y0)*.35&&sx/tail>anchorX+(y1-y0)*.12))
      for(let n=0;n<tail;n++)rgba[queue[n]*4+3]=0;
  }
  return {rgba,w,h,box:{left:x0,top:y0,width:x1-x0+1,height:y1-y0+1},anchorX};
}
async function pack(c, scale){
  const width=Math.round(c.box.width*scale),height=Math.round(c.box.height*scale);
  const left=Math.round(128-(c.anchorX-c.box.left)*scale);
  let top=224-height;
  if(left<4||left+width>252||top<4) throw Error('Insufficient frame padding '+JSON.stringify({left,width,top,scale}));
  const input=await sharp(c.rgba,{raw:{width:c.w,height:c.h,channels:4}}).extract(c.box).resize(width,height,{kernel:'nearest'}).png().toBuffer();
  const pixels=await sharp(input).raw().toBuffer();let lastY=height-1;
  while(lastY>0&&!pixels.subarray(lastY*width*4,(lastY+1)*width*4).some((v,i)=>i%4===3&&v>0))lastY--;
  top=223-(c.footY===undefined?lastY:Math.round((c.footY-c.box.top)*scale));
  return sharp({create:{width:256,height:256,channels:4,background:'#00000000'}}).composite([{input,left,top}]).png().toBuffer();
}

(async()=>{
  const source=path.join(root,'ArtSource/HeroAttacks');
  const dest=path.join(root,'Assets/Resources/Characters/HeroAttacks');
  const review=path.join(root,'docs/validation/hero_attacks');
  fs.mkdirSync(dest,{recursive:true});fs.mkdirSync(review,{recursive:true});
  const ids=['basic','sword_qi','venom_palm','blood_cleave'];
  const manifest={frameSize:256,ppu:160,pivot:[.5,.125],footPixelY:223,clips:[]};
  const contact=[], cycles=Array.from({length:8},()=>[]);
  let seed, sharedHeight=136;
  for(let d=0;d<ids.length;d++){
    const id=ids[d],cuts=[];
    const raw=path.join(source,id==='basic'?'reference.jpg':'raw/'+id+'.png');
    if(!fs.existsSync(raw))throw Error('Required attack source missing: '+id);
    const m=await sharp(raw).metadata();
    if(id==='basic'){
      // Original photo is NOT a uniform grid: the two long sword poses overlap cell columns.
      // Overlapping crops plus principal-component isolation retain the user's exact eight poses.
      for(const r of [[0,0,310,426],[310,0,327,426],[637,0,302,426],[939,0,341,426],
                      [0,426,445,427],[405,426,317,427],[719,426,222,427],[942,426,338,427]])
        cuts.push(await cut(raw,{left:r[0],top:r[1],width:r[2],height:r[3]}));
    }else{
      // Explicit per-source cell boundaries are reviewed against the generated sheet.
      const bounds=JSON.parse(fs.readFileSync(path.join(source,'cells.json'),'utf8'))[id];
      if(!bounds||bounds.length!==8)throw Error('Missing eight reviewed source cells for '+id);
      for(let i=0;i<8;i++){
        const mask=id==='blood_cleave'&&i===5 ? (x,y)=>x>=1746 :
                   id==='blood_cleave'&&i===6 ? (x,y)=>y>=570&&x<1749 : null;
        cuts.push(await cut(raw,{left:bounds[i][0],top:0,width:bounds[i][1]-bounds[i][0],height:m.height},mask,id==='sword_qi'&&(i===0||i===7)));
        if(id==='blood_cleave')cuts[i].footY=618;
      }
    }
    // ONE scale per strip; reserve room for the long thrust and raised sword in 256px cells.
    const heightScale=sharedHeight/cuts[0].box.height;
    const scale=Math.min(heightScale,...cuts.map(c=>Math.min(120/(c.anchorX-c.box.left),120/(c.box.left+c.box.width-c.anchorX),216/c.box.height)));
    if(id==='basic')sharedHeight=cuts[0].box.height*scale;
    const frames=[];
    for(let f=0;f<8;f++){
      let input=await pack(cuts[f],scale);
      if(id==='basic'&&f===0)seed=input;
      // Share neutral entry/exit poses so switching skills cannot change idle costume or anchor.
      if(seed&&(f===0||f===7))input=seed;
      frames.push({input,left:f*256,top:0});contact.push({input,left:f*256,top:d*256});
      cycles[f].push({input,left:d*256,top:0});
    }
    await sharp({create:{width:2048,height:256,channels:4,background:'#00000000'}}).composite(frames).png()
      .toFile(path.join(dest,'spr_hero_attack_'+id+'_right_8f_v01.png'));
    manifest.clips.push({id,scale,sourceHeight:cuts[0].box.height,normalizedHeight:cuts[0].box.height*scale,
      frames:8,source:raw.replace(root+'/',''),bounds:cuts.map(c=>c.box)});
  }
  await sharp({create:{width:2048,height:1024,channels:4,background:'#202e32'}}).composite(contact).png().toFile(path.join(review,'contact_sheet.png'));
  const animation=[];
  for(let f=0;f<8;f++)animation.push(await sharp({create:{width:1024,height:256,channels:4,background:'#202e32'}}).composite(cycles[f]).raw().toBuffer());
  await sharp(animation[4],{raw:{width:1024,height:256,channels:4}}).resize(4096,1024,{kernel:'nearest'}).png()
    .toFile(path.join(review,'impact_4x.png'));
  for(const fps of [8,12])await sharp(Buffer.concat(animation),{raw:{width:1024,height:2048,channels:4,pageHeight:256}})
    .gif({loop:0,delay:Math.round(1000/fps)}).toFile(path.join(review,'attacks_'+fps+'fps.gif'));
  fs.writeFileSync(path.join(source,'normalization.json'),JSON.stringify(manifest,null,2)+'\n');
  console.log(JSON.stringify(manifest.clips.map(c=>({id:c.id,scale:c.scale,height:c.normalizedHeight}))));
})();
