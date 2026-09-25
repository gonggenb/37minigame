// Technical sprite-sheet slicing only: shared scale, reviewed cell seams and
// boot anchors, preserved source alpha, reference idle at entry and exit.
const fs=require('fs'),path=require('path'),sharp=require('sharp');
const root=path.resolve(__dirname,'../..');
const source=path.join(root,'ArtSource/Raw/Characters/HeroUltimates');
const output=path.join(root,'Assets/Resources/Characters/HeroAttacks');
const review=path.join(root,'docs/validation/hero_ultimate_poses');
const specs=[
 {id:'swift_sword',sourceBodyHeight:210,seams:[0,205,538,716,1146,1485,1700,1935,2172],anchors:[136,401,619,872,1226,1560,1810,2080],feet:[523,523,523,523,523,523,523,523]},
 {id:'venom_mist',sourceBodyHeight:234,seams:[0,213,510,750,1167,1430,1680,1910,2172],anchors:[130,383,648,882,1242,1540,1820,2050],feet:[530,530,530,530,535,535,530,530]},
 {id:'iron_guard',sourceBodyHeight:243,seams:[0,225,550,800,1054,1420,1690,1910,2172],anchors:[144,398,661,921,1230,1536,1793,2050],feet:[558,558,558,558,562,558,558,558]},
 {id:'shadow_moon',sourceBodyHeight:190,seams:[0,210,468,742,1090,1415,1745,1950,2172],anchors:[127,377,635,871,1260,1568,1840,2072],feet:[480,480,480,480,484,484,480,480]},
 {id:'blood_domain',sourceBodyHeight:226,seams:[0,227,539,773,1145,1405,1665,1902,2172],anchors:[148,391,659,915,1263,1532,1780,2070],feet:[531,531,531,531,531,531,531,531]}
];
function seam(spec,n,y){
 if(spec.id==='swift_sword' && n===4 && y>=440)return 1108;
 if(spec.id==='swift_sword' && n===5 && y<320)return 1440;
 if(spec.id==='venom_mist' && n===4 && (y>=420 || y<320))return 1105;
 if(spec.id==='shadow_moon' && n===6 && y>=360 && y<415)return 1795;
 return spec.seams[n];
}
const canvas=(w,h,bg='#00000000')=>sharp({create:{width:w,height:h,channels:4,background:bg}});
function bounds(data,w,h){let l=w,r=-1,t=h,b=-1;for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(data[(y*w+x)*4+3]>0){l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y);}return {left:l,top:t,width:r-l+1,height:b-t+1};}
async function pack(data,w,h,anchor,foot,scale){const b=bounds(data,w,h);const width=Math.max(1,Math.round(b.width*scale)),height=Math.max(1,Math.round(b.height*scale));const left=Math.round(128-(anchor-b.left)*scale),top=Math.round(223-(foot-b.top)*scale);
 if(left<2||top<2||left+width>254||top+height>252)throw Error('Clipping '+JSON.stringify({b,left,top,width,height}));
 const png=await sharp(data,{raw:{width:w,height:h,channels:4}}).extract(b).resize(width,height,{kernel:'nearest'}).png().toBuffer();
 return {png:await canvas(256,256).composite([{input:png,left,top}]).png().toBuffer(),sourceBounds:b,placedBounds:{left,top,width,height}};
}
(async()=>{
 fs.mkdirSync(review,{recursive:true});
 const seed=await sharp(path.join(output,'spr_hero_attack_basic_right_8f_v01.png')).extract({left:0,top:0,width:256,height:256}).ensureAlpha().raw().toBuffer();
 const contact=[],cycles=Array.from({length:8},()=>[]),manifest={frameSize:256,pivot:[.5,.125],ppu:160,alphaNoiseThreshold:8,clips:[]};
 for(let v=0;v<specs.length;v++){
  const s=specs[v];const {data,info}=await sharp(path.join(source,s.id+'.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  if(info.width!==2172||info.height!==724)throw Error('Unexpected raw strip dimensions');
  for(let p=3;p<data.length;p+=4)if(data[p]<=8)data[p]=0;
  const cells=[];let scale=136/s.sourceBodyHeight;
  for(let f=1;f<7;f++){
   const pixels=Buffer.alloc(data.length);for(let y=0;y<info.height;y++){const a=seam(s,f,y),b=seam(s,f+1,y);data.copy(pixels,(y*info.width+a)*4,(y*info.width+a)*4,(y*info.width+b)*4);}
   const b=bounds(pixels,info.width,info.height),a=s.anchors[f],foot=s.feet[f];
   scale=Math.min(scale,124/Math.max(1,a-b.left),124/Math.max(1,b.left+b.width-a),219/Math.max(1,foot-b.top),27/Math.max(1,b.top+b.height-foot));
   cells[f]=pixels;
  }
  const bodyHeight=Math.floor(s.sourceBodyHeight*scale);scale=bodyHeight/s.sourceBodyHeight;
  const frames=[],records=[];
  for(let f=0;f<8;f++){
   const packed=(f===0||f===7)?await pack(seed,256,256,128,223,bodyHeight/136):await pack(cells[f],info.width,info.height,s.anchors[f],s.feet[f],scale);
   frames.push({input:packed.png,left:f*256,top:0});contact.push({input:packed.png,left:f*256,top:v*256});
   const displaySize=Math.round(256*136/bodyHeight);
   cycles[f].push({input:await sharp(packed.png).resize(displaySize,displaySize,{kernel:'nearest'}).png().toBuffer(),left:v*384+192-Math.round(displaySize/2),top:350-Math.round(displaySize*.875)});
   records.push({frame:f,sourceBounds:packed.sourceBounds,placedBounds:packed.placedBounds});
  }
  const filename='spr_hero_ultimate_'+s.id+'_right_8f_v01.png';
  await canvas(2048,256).composite(frames).png().toFile(path.join(output,filename));
  manifest.clips.push({...s,scale,bodyHeight,displayScale:136/bodyHeight,filename,frames:records});
 }
 await canvas(2048,1280,'#202e32').composite(contact).png().toFile(path.join(review,'contact_sheet.png'));
 const pages=[];for(let f=0;f<8;f++)pages.push(await canvas(1920,400,'#202e32').composite(cycles[f]).raw().toBuffer());
 for(const fps of [8,12])await sharp(Buffer.concat(pages),{raw:{width:1920,height:3200,channels:4,pageHeight:400}}).gif({loop:0,delay:Math.round(1000/fps)}).toFile(path.join(review,'poses_'+fps+'fps.gif'));
 await sharp(Buffer.concat(pages),{raw:{width:1920,height:3200,channels:4,pageHeight:400}}).gif({loop:0,delay:[70,140,130,160,210,240,220,630]}).toFile(path.join(review,'cast_preview.gif'));
 const compact=[];
 for(let f=0;f<8;f++){
  const frame=sharp(pages[f],{raw:{width:1920,height:400,channels:4}}),parts=[];
  for(let v=0;v<5;v++)parts.push({input:await frame.clone().extract({left:v*384,top:80,width:384,height:320}).png().toBuffer(),left:(v%3)*384,top:Math.floor(v/3)*320});
  compact.push(await canvas(1152,640,'#202e32').composite(parts).raw().toBuffer());
 }
 await sharp(Buffer.concat(compact),{raw:{width:1152,height:5120,channels:4,pageHeight:640}}).gif({loop:0,delay:[70,140,130,160,210,240,220,630]}).toFile(path.join(review,'baked_effects_preview.gif'));
 await canvas(1920,400,'#202e32').composite(cycles[4]).png().toFile(path.join(review,'cast_impact.png'));
 await canvas(1920,400,'#202e32').composite(cycles[4]).resize(7680,1600,{kernel:'nearest'}).png().toFile(path.join(review,'cast_impact_4x.png'));
 fs.writeFileSync(path.join(review,'normalization.json'),JSON.stringify(manifest,null,2)+'\n');
 console.log(JSON.stringify(manifest.clips.map(c=>({id:c.id,bodyHeight:c.bodyHeight,displayScale:c.displayScale}))));
})().catch(e=>{console.error(e);process.exitCode=1;});
