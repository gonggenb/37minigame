// Format-only normalization of selected imagegen animation sheets: preserve RGBA,
// common cell center and one scale, then pack Unity strips and review frames.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../..');
const source = path.join(root, 'ArtSource/MartialProcVfx/raw');
const dest = path.join(root, 'Assets/Resources/Effects/MartialProcs');
const review = path.join(root, 'docs/validation/martial_proc_vfx');
const ids = ['armor_break', 'swift_combo', 'retaliation', 'life_drain'];

(async () => {
  fs.mkdirSync(dest, {recursive: true});
  fs.mkdirSync(review, {recursive: true});
  const report = {frameSize: 256, framesPerClip: 6, pivot: [.5, .5], ppu: 256,
    normalization: '3x2 equal cells; common 224px scale centered in 256px; nearest neighbor; alpha preserved', clips: []};
  const contact = [], cycles = Array.from({length: 6}, () => []);
  for (let row = 0; row < ids.length; row++) {
    const id = ids[row], file = path.join(source, id + '.png');
    const metadata = await sharp(file).metadata();
    if (!metadata.hasAlpha || metadata.width % 3 || metadata.height % 2 || metadata.width / 3 !== metadata.height / 2)
      throw new Error('Expected RGBA 3x2 square-cell sheet: ' + id);
    const cell = metadata.width / 3, frames = [], stats = [];
    for (let i = 0; i < 6; i++) {
      const crop = {left: (i % 3) * cell, top: Math.floor(i / 3) * cell, width: cell, height: cell};
      const {data} = await sharp(file).extract(crop).ensureAlpha().raw().toBuffer({resolveWithObject: true});
      let transparent = 0, visible = 0, edge = 0;
      for (let pixel = 0; pixel < cell * cell; pixel++) {
        const alpha = data[pixel * 4 + 3], x = pixel % cell, y = Math.floor(pixel / cell);
        if (alpha === 0) transparent++;
        if (alpha > 32) {
          visible++;
          if (x < 2 || y < 2 || x >= cell - 2 || y >= cell - 2) edge++;
        }
      }
      if (transparent < cell * cell * .15 || visible < 100 || edge > 0)
        throw new Error('Opaque background, empty frame or clipped effect: ' + id + '/' + i + ' ' + JSON.stringify({transparent, visible, edge}));
      const reduced = await sharp(data, {raw: {width: cell, height: cell, channels: 4}})
        .resize(224, 224, {kernel: 'nearest'}).png().toBuffer();
      const frame = await sharp({create: {width: 256, height: 256, channels: 4, background: '#00000000'}})
        .composite([{input: reduced, left: 16, top: 16}]).png().toBuffer();
      const frameFolder = path.join(review, 'frames', id);
      fs.mkdirSync(frameFolder, {recursive: true});
      fs.writeFileSync(path.join(frameFolder, 'frame_' + String(i).padStart(2, '0') + '.png'), frame);
      frames.push({input: frame, left: i * 256, top: 0});
      contact.push({input: frame, left: i * 256, top: row * 256});
      cycles[i].push({input: frame, left: row * 256, top: 0});
      stats.push({frame: i, transparent, visible, clippedEdgePixels: edge,
        sha256: crypto.createHash('sha256').update(frame).digest('hex')});
    }
    if (new Set(stats.map(s => s.sha256)).size !== 6) throw new Error('Duplicate animation frames: ' + id);
    const name = 'spr_vfx_proc_' + id + '_6f_v01.png';
    await sharp({create: {width: 1536, height: 256, channels: 4, background: '#00000000'}})
      .composite(frames).png().toFile(path.join(dest, name));
    report.clips.push({id, source: 'ArtSource/MartialProcVfx/raw/' + id + '.png', output: name, frames: stats});
  }
  for (const [name, color] of [['contact_dark', '#171e22'], ['contact_light', '#d3c8b2']])
    await sharp({create: {width: 1536, height: 1024, channels: 4, background: color}})
      .composite(contact).png().toFile(path.join(review, name + '.png'));
  for (let i = 0; i < cycles.length; i++)
    await sharp({create: {width: 1024, height: 256, channels: 4, background: '#171e22'}})
      .composite(cycles[i]).png().toFile(path.join(review, 'cycle_' + i + '.png'));
  const animation = [];
  for (let i = 0; i < 6; i++)
    animation.push(await sharp(path.join(review, 'cycle_' + i + '.png')).ensureAlpha().raw().toBuffer());
  animation.push(await sharp({create: {width: 1024, height: 256, channels: 4, background: '#171e22'}}).raw().toBuffer());
  await sharp(Buffer.concat(animation), {raw: {width: 1024, height: 256 * 7, channels: 4, pageHeight: 256}})
    .gif({loop: 0, delay: [83, 83, 83, 83, 83, 83, 350]})
    .toFile(path.join(review, 'animation_preview.gif'));
  fs.writeFileSync(path.join(review, 'asset_report.json'), JSON.stringify(report, null, 2) + '\n');
  console.log('Packed four transparent six-frame strips; common center/scale, no clipping, no duplicate frames.');
})().catch(error => {console.error(error); process.exitCode = 1;});
