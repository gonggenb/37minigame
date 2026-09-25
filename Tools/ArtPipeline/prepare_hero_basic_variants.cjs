// Technical slicing/normalization of the three user-selected imagegen strips.
// No repainting: remove almost-transparent generation noise, one scale per strip,
// shared entry/exit pose; preserve the remaining source alpha.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../..');
const source = path.join(root, 'ArtSource/Previews/HeroAttacks/2026-09-21_basic_vfx_v01');
const output = path.join(root, 'Assets/Resources/Characters/HeroAttacks');
const review = path.join(root, 'docs/validation/hero_basic_variants');
const specs = [
  { id: 'jade_crescent', height: 112, sourceHeight: 204, foot: 503,
    bounds: [0,245,530,785,1100,1402,1655,1916,2172],
    anchors: [137,405,678,908,1186,1481,1764,2052] },
  { id: 'golden_ember', height: 112, sourceHeight: 214, foot: 517,
    bounds: [0,250,533,770,1088,1378,1671,1907,2172],
    anchors: [145,401,673,925,1172,1457,1757,2046] },
  { id: 'ink_afterimage', height: 94, sourceHeight: 191, foot: 494,
    bounds: [0,247,530,780,1088,1454,1737,1940,2172],
    anchors: [140,398,651,907,1212,1533,1810,2073] }
];
const canvas = (w,h,bg='#00000000') => sharp({create:{width:w,height:h,channels:4,background:bg}});
function boxOf(data,w,h) {
  let left=w,top=h,right=-1,bottom=-1;
  for(let y=0;y<h;y++) for(let x=0;x<w;x++) if(data[(y*w+x)*4+3]>0) {
    left=Math.min(left,x);right=Math.max(right,x);top=Math.min(top,y);bottom=Math.max(bottom,y);
  }
  if(right<left) throw Error('Empty frame');
  return {left,top,width:right-left+1,height:bottom-top+1};
}
async function pack(data,w,h,anchor,foot,scale) {
  const box=boxOf(data,w,h);
  const width=Math.round(box.width*scale),height=Math.round(box.height*scale);
  const left=Math.round(128-(anchor-box.left)*scale);
  const top=223-Math.round((foot-box.top)*scale);
  if(left<3||left+width>253||top<3||top+height>225)
    throw Error('Frame clips its safe area: '+JSON.stringify({box,left,top,width,height}));
  const sprite=await sharp(data,{raw:{width:w,height:h,channels:4}}).extract(box)
    .resize(width,height,{kernel:'nearest'}).png().toBuffer();
  return {png:await canvas(256,256).composite([{input:sprite,left,top}]).png().toBuffer(),
    sourceBounds:box,placedBounds:{left,top,width,height}};
}
(async()=>{
  fs.mkdirSync(review,{recursive:true});
  const seed=await sharp(path.join(output,'spr_hero_attack_basic_right_8f_v01.png'))
    .extract({left:0,top:0,width:256,height:256}).ensureAlpha().raw().toBuffer();
  const manifest={frameSize:256,footPixelY:223,ppu:160,pivot:[0.5,0.125],variants:[]};
  const contact=[],cycles=Array.from({length:8},()=>[]),displayCycles=Array.from({length:8},()=>[]);
  for(let v=0;v<specs.length;v++) {
    const spec=specs[v],file=path.join(source,`spr_hero_attack_basic_${spec.id}_right_8f_concept_v01.png`);
    const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    if(info.width!==2172||info.height!==724) throw Error('Unexpected source dimensions');
    for(let p=3;p<data.length;p+=4) if(data[p]<=25) data[p]=0;
    const scale=spec.height/spec.sourceHeight,frames=[],records=[];
    for(let f=0;f<8;f++) {
      let packed;
      if(f===0||f===7) packed=await pack(seed,256,256,128,223,spec.height/136);
      else {
        // Golden frame five's upper sparks extend over the next pose's left boot.
        // The reviewed boundary changes above the boots, preserving both silhouettes.
        const pixels=Buffer.alloc(data.length);
        for(let y=0;y<info.height;y++) {
          let a=spec.bounds[f],b=spec.bounds[f+1];
          if(spec.id==='golden_ember'&&y<345) {if(f===4)b=1400;if(f===5)a=1400;}
          data.copy(pixels,(y*info.width+a)*4,(y*info.width+a)*4,(y*info.width+b)*4);
        }
        packed=await pack(pixels,info.width,info.height,spec.anchors[f],spec.foot,scale);
      }
      frames.push({input:packed.png,left:f*256,top:0});
      contact.push({input:packed.png,left:f*256,top:v*256});
      cycles[f].push({input:packed.png,left:v*256,top:0});
      const displaySize=Math.round(256*136/spec.height);
      displayCycles[f].push({
        input:await sharp(packed.png).resize(displaySize,displaySize,{kernel:'nearest'}).png().toBuffer(),
        left:v*384+192-Math.round(displaySize/2),top:336-Math.round(displaySize*0.875)
      });
      records.push({frame:f,sourceBounds:packed.sourceBounds,placedBounds:packed.placedBounds});
    }
    const filename=`spr_hero_attack_basic_${spec.id}_right_8f_v01.png`;
    await canvas(2048,256).composite(frames).png().toFile(path.join(output,filename));
    manifest.variants.push({...spec,scale,displayScale:136/spec.height,filename,frames:records});
  }
  await canvas(2048,768,'#202e32').composite(contact).png().toFile(path.join(review,'contact_sheet.png'));
  const pages=[];
  for(let f=0;f<8;f++) pages.push(await canvas(768,256,'#202e32').composite(cycles[f]).raw().toBuffer());
  for(const fps of [8,12]) await sharp(Buffer.concat(pages),{raw:{width:768,height:2048,channels:4,pageHeight:256}})
    .gif({loop:0,delay:Math.round(1000/fps)}).toFile(path.join(review,`attacks_${fps}fps.gif`));
  const displayPages=[];
  for(let f=0;f<8;f++) displayPages.push(await canvas(1152,384,'#202e32').composite(displayCycles[f]).raw().toBuffer());
  await sharp(Buffer.concat(displayPages),{raw:{width:1152,height:3072,channels:4,pageHeight:384}})
    .gif({loop:0,delay:[60,60,60,60,60,60,60,480]}).toFile(path.join(review,'display_scale_preview.gif'));
  await canvas(768,256,'#202e32').composite(cycles[4]).resize(3072,1024,{kernel:'nearest'})
    .png().toFile(path.join(review,'impact_4x.png'));
  fs.writeFileSync(path.join(review,'normalization.json'),JSON.stringify(manifest,null,2)+'\n');
  console.log(JSON.stringify(manifest.variants.map(v=>({id:v.id,scale:v.scale,displayScale:v.displayScale}))));
})().catch(e=>{console.error(e);process.exitCode=1;});
