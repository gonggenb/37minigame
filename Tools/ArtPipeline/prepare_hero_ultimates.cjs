// Normalize approved-direction generated VFX cores; preserve alpha and pixel edges.
const fs=require('fs'),path=require('path'),sharp=require('sharp');
const root=path.resolve(__dirname,'../..');
const ids=['swift_sword','venom_mist','iron_guard','shadow_moon','blood_domain'];
(async()=>{
 const source=path.join(root,'ArtSource/Raw/VFX/HeroUltimates');
 const output=path.join(root,'Assets/Resources/HeroUltimates');
 const review=path.join(root,'docs/validation/hero_ultimates');
 fs.mkdirSync(output,{recursive:true});fs.mkdirSync(review,{recursive:true});
 const contact=[],manifest=[];
 for(let i=0;i<ids.length;i++){
  const id=ids[i],file=path.join(source,id+'.png');
  const meta=await sharp(file).metadata();
  if(!meta.hasAlpha)throw Error('Source needs genuine alpha: '+id);
  const buffer=await sharp(file).resize(448,448,{fit:'contain',background:'#00000000',kernel:'nearest'}).png().toBuffer();
  const image=await sharp({create:{width:512,height:512,channels:4,background:'#00000000'}})
   .composite([{input:buffer,left:32,top:32}]).png().toBuffer();
  fs.writeFileSync(path.join(output,'vfx_ultimate_'+id+'_v01.png'),image);
  contact.push({input:image,left:i*512,top:0});
  manifest.push({id,sourceSize:[meta.width,meta.height],outputSize:[512,512],safeMargin:32,alpha:true});
 }
 await sharp({create:{width:2560,height:512,channels:4,background:'#1c252b'}})
  .composite(contact).png().toFile(path.join(review,'art_contact_sheet.png'));
 fs.writeFileSync(path.join(review,'normalization.json'),JSON.stringify(manifest,null,2)+'\n');
 console.log(JSON.stringify(manifest));
})().catch(e=>{console.error(e);process.exitCode=1;});
