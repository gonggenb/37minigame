// Capture the preview renderer at fixed timestamps as shareable animations.
const {chromium}=require('playwright');
const sharp=require('sharp');
const path=require('node:path');
(async()=>{
 const browser=await chromium.launch({headless:true,channel:'chrome'});
 const page=await browser.newPage();await page.goto('http://127.0.0.1:8766');
 await page.waitForFunction(()=>window.previewReady===true);
 for(const [key,duration]of [['double',2.4],['guard',4.5]]){
  const frames=[],count=Math.ceil(duration*12);
  for(let i=0;i<count;i++){
   const data=await page.evaluate(({key,time})=>{window.setPreview(key,time);return document.getElementById('stage').toDataURL('image/png').split(',')[1];},{key,time:i/12});
   frames.push(await sharp(Buffer.from(data,'base64')).resize(640,360,{kernel:'nearest'}).removeAlpha().raw().toBuffer());
  }
  await sharp(Buffer.concat(frames),{raw:{width:640,height:360*count,channels:3,pageHeight:360}}).gif({loop:0,delay:Array.from({length:count},(_,i)=>i%3===2?90:80),colours:128,dither:0.3}).toFile(path.join(__dirname,key+'_preview.gif'));
  console.log(key+': '+count+' frames exported');
 }
 await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
