// Static approval board only. Does not initialize or render a HyperFrames video.
const fs = require('fs');
const path = require('path');
const { pathToFileURL } = require('url');
const { spawnSync } = require('child_process');
const base = __dirname;
const root = path.resolve(base, '../../..');
const ffmpeg = process.env.FFMPEG_PATH || path.join(process.env.HOME, 'Library/Application Support/bilibili/ffmpeg/ffmpeg');
const source = path.resolve(base, '../Trailer_v01/一炷江湖_竖屏宣传片_v01.mp4');
const url = p => pathToFileURL(p).href;
const shots = [
  {id:'01',start:0,end:3,title:'血月妖狐',caption:'六十秒后，强敌来袭',image:'assets/fox_bloodmoon.png',motion:'古刹与血月先进入视野，镜头缓推妖狐，雾层横移，狐火微亮；暗雾溶接下一镜。',meaning:'建立最终强敌与六十秒成长压力。',risk:'新构图与推进方式待批准；只是动态分镜，不是连续人物表演。'},
  {id:'02',start:3,end:6,title:'拔剑入局',caption:'这一局，你如何破局？',image:'assets/hero_draws_sword.png',motion:'主角持剑构图进入，镜头推进，剑刃扫过高光；单次斜向剑光揭开已有实机。',meaning:'把强敌压力转换成玩家的迎战意愿。',risk:'剑光揭幕后必须直接接现有实机；不增加对白或肢体动画承诺。'},
  {id:'03',start:6,end:11,title:'探索与碰怪',caption:'60 秒探索／每一步，都是选择',time:7.4,motion:'沿用实机道路移动和自然碰怪交锋，保留 HUD 与原剪辑转场。',meaning:'说明探索、遭遇和时间压力确实存在。',risk:'沿用已录制画面；不是新一次自然通关。'},
  {id:'04',start:11,end:14,title:'精英交锋',caption:'碰怪即战／自动交锋，时间不停',time:12.5,motion:'沿用精英自动战斗、技能与伤害反馈及原转场。',meaning:'展示自动战斗以及普通地图战斗继续倒计时。',risk:'沿用受控遭遇录制，不改变数值。'},
  {id:'05',start:14,end:17,title:'武学构筑',caption:'武学搭配／选择你的江湖路',time:15.5,motion:'沿用真实武学选择页和融合路线预告，不添加虚构选项。',meaning:'让观众看到选择武学和搭配流派。',risk:'保留现有页面；不把可解锁预告当成已经获得。'},
  {id:'06',start:17,end:24,title:'九尾妖狐决战',caption:'挑战九尾妖狐／以本局构筑，迎战强敌',time:21.5,motion:'沿用七秒连续实机攻击、独立计时与阶段反馈。',meaning:'回收开场强敌，展示构筑面对 Boss 的检验。',risk:'七秒用于看清攻击循环与 Boss 状态，不是完整胜利录像。'},
  {id:'07',start:24,end:30,title:'片名与重玩邀请',caption:'一炷江湖／再来一局，换条路闯江湖',time:27.5,motion:'沿用双角色片尾、片名与工作室标识，最后自然淡出。',meaning:'留出读片名、重玩邀请与制作组名称的时间。',risk:'沿用已有片尾，不增加上线时间或下载地址。'}
];
for(const s of shots.filter(s=>s.time!==undefined)) {
  const target = path.join(base, 'keyframes',`scene-${s.id}.png`);
  const r = spawnSync(ffmpeg,['-y','-loglevel','error','-ss',String(s.time),'-i',source,'-frames:v','1',target],{encoding:'utf8'});
  if(r.status!==0)throw new Error(r.stderr);
}
const font=url(path.join(root,'Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.ttf'));
const html=`<!doctype html><html lang="zh-CN"><meta charset="UTF-8"><title>一炷江湖 · 静态关键帧审批</title><style>
@font-face{font-family:Wuxia;src:url('${font}');font-weight:700}*{box-sizing:border-box}body{margin:0;background:#0e1112;color:#e9dfc3;font-family:Wuxia,sans-serif}main{padding:40px}h1{font-size:30px;margin:0 0 12px}p{font-size:18px;color:#a9ada3;margin:0 0 28px}.row{display:flex;gap:24px}.card{width:320px}.meta{font-size:21px;margin:0 0 12px;color:#d1aa5a}.frame{width:1080px;height:1920px;position:relative;overflow:hidden;background:#0e1112}.frame img{position:absolute;width:100%;height:100%;object-fit:cover}.shade{position:absolute;inset:0;background:linear-gradient(to top,rgba(14,17,18,.96) 0%,rgba(14,17,18,.75) 17%,rgba(14,17,18,0) 38%,rgba(14,17,18,0) 85%,rgba(14,17,18,.5) 100%)}.content{width:100%;height:100%;padding:105px 90px 265px;display:flex;flex-direction:column;align-items:center;gap:24px;position:relative}.brand{font-size:42px;color:#d1aa5a;letter-spacing:12px;text-shadow:0 2px 5px #0e1112}.spacer{flex:1}.caption{font-size:64px;white-space:nowrap;color:#e9dfc3;text-shadow:0 3px 8px #0e1112}.tag{font-size:27px;color:#b8bcb3;letter-spacing:6px}.scale{width:320px;height:569px;overflow:hidden}.scale>.frame{transform:scale(.2962962963);transform-origin:top left}.description{font-size:17px;line-height:1.65;color:#d8cba5;margin-top:16px}.all{display:grid;grid-template-columns:repeat(4,240px);gap:20px;margin-top:40px}.all img{width:240px;height:426px;object-fit:cover}
</style><main><h1>一炷江湖 · 动画开场接实机</h1><p>静态关键帧审批 · 0—6 秒新动画方向，6 秒后沿用实机剪辑 · 尚未渲染新视频</p><section class="row">${shots.slice(0,3).map(s=>`<article class="card"><div class="meta">${s.id} / ${s.start}—${s.end} 秒 · ${s.title}</div><div class="scale">${s.image?`<div class="frame" id="scene-${s.id}"><img src="${s.image}"><div class="shade"></div><div class="content"><div class="brand">一炷江湖</div><div class="spacer"></div><div class="caption">${s.caption}</div><div class="tag">动画演绎</div></div></div>`:`<div class="frame"><img src="keyframes/scene-${s.id}.png"></div>`}</div><div class="description">${s.motion}</div></article>`).join('')}</section><section class="all">${shots.slice(3).map(s=>`<article><div class="meta">${s.id} / ${s.start}—${s.end} 秒</div><img src="keyframes/scene-${s.id}.png"><div>${s.title} · 沿用实机段与片尾</div></article>`).join('')}</section></main></html>`;
fs.writeFileSync(path.join(base,'preview.html'),html);
let spec=`请你按照以下 script，帮我生成一条视频。以下是这条视频的 script 和详细讲解。

状态：待用户批准关键帧。此目录是独立的视频项目根目录。当前只准备图片与规范，尚未创建或渲染 HyperFrames 时间轴。

## 1. 视频基本盘
- 标题：一炷江湖 · 动画开场＋实机宣传片 v02。
- 目的：用户明确要求游戏宣传片，前面 AI 动画接已有实机。
- 受众：[待用户确认] 在抖音／视频号刷到游戏、对武侠或短局构筑感兴趣的新玩家；不假定其熟悉项目。
- 观众熟悉度：[待用户确认] 不要求理解 Roguelite 术语，直接展示探索、武学选择和强敌。
- 平台与时长：抖音／视频号，9:16 已确认；沿用已制作的 30.0 秒方案。
- 画面规格：1080 × 1920，30 FPS，字幕保证无声也能理解；原实机为 720 × 1280。
- 输出：MP4 H.264 + AAC；首次 HyperFrames 审片 standard，确认后 high。
- 信息密度：前六秒两镜建立冲突，此后保留已有玩法展示和片尾。
- 语气基调：继承本项目现有主角、妖狐立绘和 v01；角色对峙，不添加剧情对白。

## 2. 叙事结构
- 叙事节拍：0.0–3.0 强敌压场；3.0–6.0 主角迎战；6.0–24.0 已有玩法；24.0–30.0 原片尾。
- 情绪曲线：[待用户确认] 压迫 → 决心 → 玩法验证与再战邀请。
- 音画关系：沿用 v01 音轨；2.78 秒剑风先于主角画面出现，5.78 秒剑风引出实机，形成声音先行。不开麦，不新增 TTS。
- 同质化反例：不用现代促销卡片；不用虚假战力或下载承诺；不用连续频闪和长时间空白。

## 3. 表达手段
- 场景类型组合：AI 电影插画动态分镜 → Unity 实机录制 → 武学选择 UI → Boss 实机 → 原品牌片尾。
- 字幕呈现：沿用 v01 整句字幕；没有口播，不凭空增加逐词卡拉 OK。
- 关键词强调：片名暗金，正文字米白；不增加手绘圆圈和促销贴纸。
- 文字动效：[待用户确认] 开场文字随场景依次淡入；实机段原字幕不变。
- 3D：不新增模型或 3D 片段；实机中的 Unity 地图是原游戏画面。
- 转场风格：开场暗雾溶接，剑光揭幕到实机；6 秒之后原转场不变。
- 特殊视觉：独立雾层、狐火轻光、剑光路径；不做重型全屏抖动或节拍缩放。
- 节奏基准：前两镜各 3.0 秒；后三秒以上镜头各有玩法演示或阅读任务，见分镜表。

## 4. 视觉规范
- 视觉主题：design.md（本视频项目根目录），继承仓库 UI_STYLE_GUIDE 与现有角色身份。
- accent 色：#D1AA5A 暗金，#963A31 暗朱红；不是全局 UI 风格变更。
- 装饰密度：[待用户确认] 中等，仅雾、狐火、剑光；实机不加遮挡型装饰。
- 组件取舍：不使用数据图表、现代 App 卡片或浏览器外壳。目录缺少游戏录像／电影分镜组件，对应镜头暂用规定的 placeholder 分类，实际素材和直接媒体实现已明确。

## 5. 素材清单
### 已有素材
- assets/fox_bloodmoon.png：本轮内置 imagegen 生成，妖狐身份参考来自 ../../../ArtSource/Normalized/OpeningDialogue/portrait_fox_v01.png。
- assets/hero_draws_sword.png：本轮内置 imagegen 生成，主角身份参考来自 ../../../ArtSource/Normalized/OpeningDialogue/portrait_hero_v01.png。
- ../Trailer_v01/一炷江湖_竖屏宣传片_v01.mp4：视频仅使用 6.0–30.0 秒；现有混音音轨可独立使用 0.0–30.0 秒。
- ../../../Assets/Art/Generated/Environment/HD2D/spr_env_hd2d_mist_band_1024x256_v01.png：可复用雾层。
- ../../../Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.ttf：随包字体。
- ../Trailer_v01/capture/report.json：实机来源与范围；受控输入和可获得武学组合，不是完整自然通关。
- image-prompts.json：两幅新图完整提示词与身份约束。
### 待生成素材
- 用户批准后：HyperFrames index.html 及可定位时间轴；新的 30 秒合成 MP4。
- 关键帧 keyframes/scene-01.png 至 scene-07.png 已由静态 HTML 截图／原片提帧准备。
- 不需要真人、配音、新 3D 模型、外部素材或额外 Unity 录制。
### 待搜索素材
无搜索素材需求。

## 6. 分镜表
`;
for(const s of shots){
const comp=['01','02','07'].includes(s.id)?'broll-hero.big-type':'broll-abstract.placeholder';
spec+=`\n### Scene ${s.id} · ${s.start.toFixed(1)}–${s.end.toFixed(1)} · ${s.title}\n\n- 类型：B-roll。\n- 组件：${comp}${comp.includes('placeholder')?'（目录不含游戏录像组件；直接嵌入已存在的视频，不生成灰盒占位图）':'（角色场景配一句主标题，使用本项目美术）'}。\n- 旁白文案：无。\n- 屏显文案：${s.caption}。\n- 期待内容：${s.meaning}\n- 期待效果：${s.id==='01'?'认出强敌并产生紧迫感。':s.id==='02'?'从角色迎战转向想看真实玩法。':s.id==='07'?'记住片名与再玩一局的邀请。':'看到与宣传文案对应的实际功能。'}\n- 画面描述：9:16。${s.image?'新 AI 插画铺满画面，片名上置，主文案在下方暗区，脸与手不被遮挡；不新增 3D。':'保留 v01 原竖屏布局与字幕，保持 HUD 和关键操作可读；无二次拉伸裁剪。'}\n- 动效要点：${s.motion}\n- 音效描述：${s.id==='01'?'2.78 秒剑风，声音先行。':s.id==='02'?'5.78 秒剑风接实机。':s.id==='04'?'11.0 秒原片转场重击。':s.id==='06'?'17.0 秒原片重击，保留后续背景音乐。':s.id==='07'?'24.0 秒原片重击，28.0–30.0 秒音乐渐弱。':'无新增音效，沿用原片混音。'}\n- 转场进入：${s.id==='01'?'背景与主体依次淡入。':s.id==='02'?'暗雾溶接。':s.id==='03'?'斜向剑光揭幕。':'沿用 v01 已有转场。'}\n- 转场离开：${s.id==='01'?'暗雾溶接，转场前主体保持可见。':s.id==='02'?'斜向剑光揭幕到实机。':s.id==='07'?'片尾淡出结束。':'沿用 v01 原转场。'}\n- 素材依赖：${s.image||'../Trailer_v01/一炷江湖_竖屏宣传片_v01.mp4，对应 '+s.start.toFixed(1)+'–'+s.end.toFixed(1)+' 秒'}；keyframes/scene-${s.id}.png。\n`;
}
spec+=`\n## 7. 音频时间轴
- 旁白：无，保留已有无配音方案。
- 背景音乐与音效：复用 ../Trailer_v01/一炷江湖_竖屏宣传片_v01.mp4 的完整 0.0–30.0 秒已混音音轨，音量 1.0，不重复叠加 BGM。
- 独立 audio 元素承载声音；video 元素 muted playsinline。前后视频片段不带第二条声音。
- 音效节点：2.78、5.78、11.0、17.0、24.0 秒，对应上方分镜；28.0–30.0 秒原混音已淡出。

## 8. 参考与反例
- 正向参考：本项目 v01 前六秒与主角／妖狐现有立绘；差异是新增有场景、有动作姿势的 AI 原画，保留既有身份。
- 静态参考：两张原立绘锁定角色面貌、服装、配色；新生成图不进入 Unity 资源目录。
- 反例：不再只是旧立绘覆盖背景的小幅缩放；不把连续人物动画或虚构技能伪装成实机；不引入与武侠题材不一致的科技模板。

## 9. 开放问题
- Scene 01 / 画面描述、Scene 02 / 画面描述：两张新图和六秒衔接提案待用户批准。
- 视频基本盘 / 受众、叙事结构 / 情绪曲线：属于根据游戏与平台提出的制作建议，不冒充用户原话；随预览一起确认。
- Scene 03–06 / 组件：目录分类占位，实际画面素材已齐备，拟直接复用原视频；批准后解除此分类待确认，不添加无关 UI 组件。
- 不需要重新确认已明确的竖屏、抖音／视频号、动态分镜、已有实机部分。
- 批准前只提供关键帧与规范，不启动 HyperFrames 生成视频。
`;
fs.writeFileSync(path.join(base,'video-spec.md'),spec);
const board=`# Preview Board · 渲染前关键帧预览表

- 对应脚本：[video-spec.md](video-spec.md)
- 审批状态：待用户批准
- 本轮范围：前六秒换成新 AI 场景动态分镜，随后接已有实机和原片尾。总长仍为 30 秒。
- 视觉主题：[design.md](design.md)；新图用于动画演绎，不是实机。
- 关键帧为静态画面，供确认构图、文字和衔接；还没有渲染新的 HyperFrames 视频。

| Scene | 时间 | 关键帧预览 | 简单介绍 | 预览图计划 | 动态效果计划 | 风险 / 待确认 | 用户意见 |
|---|---:|---|---|---|---|---|---|
${shots.map(s=>`| ${s.id} | ${s.start.toFixed(1)}–${s.end.toFixed(1)} 秒 | ![Scene ${s.id}](keyframes/scene-${s.id}.png) | ${s.meaning} | ${s.image?s.title+'新原画，顶部片名，下方短句，保持面部与手部无遮挡。':'沿用原片对应镜头、字幕与 HUD。'} | ${s.motion} | ${s.risk}${['03','04','05','06'].includes(s.id)?' 组件目录占位，渲染前确认直接采用已有视频画面。':''} | 待审 |
`).join('')}
## 审批口径
- 通过：回复“按这版生成”，进入 HyperFrames 制作与合成。
- 修改：指出镜头编号以及画面、字幕或动态效果要改哪里。
- 本轮只新增此视频项目目录的原画、关键帧和文档；没有修改 Unity 场景、脚本或 v01 成片。
`;
fs.writeFileSync(path.join(base,'preview-board.md'),board);
fs.writeFileSync(path.join(base,'shot-manifest.json'),JSON.stringify({approval:'pending',duration:30,width:1080,height:1920,fps:30,shots},null,2));

(async()=>{
  let pw;try{pw=require('playwright')}catch{pw=require('/Users/gongyuyang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright')}
  const browser=await pw.chromium.launch({executablePath:process.env.CHROME_PATH||'/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',headless:true});
  const page=await browser.newPage({viewport:{width:1080,height:1000},deviceScaleFactor:1});
  await page.goto(url(path.join(base,'preview.html')));await page.evaluate(()=>document.fonts.ready);
  await page.evaluate(()=>Promise.all([...document.images].map(img=>img.decode())));
  // Temporarily remove the thumbnail scale for each full-resolution static frame.
  for(const s of shots.slice(0,2)){
    await page.addStyleTag({content:'.scale{width:1080px;height:1920px}.scale>.frame{transform:none}'});
    await page.locator(`#scene-${s.id}`).screenshot({path:path.join(base,'keyframes',`scene-${s.id}.png`)});
  }
  await page.reload();await page.evaluate(()=>document.fonts.ready);await page.evaluate(()=>Promise.all([...document.images].map(img=>img.decode())));
  await page.addStyleTag({content:'.all{display:none}'});
  await page.evaluate(()=>window.scrollTo(0,0));
  await page.screenshot({path:path.join(base,'opening-preview.png'),fullPage:true});
  await browser.close();console.log('Prepared 7 keyframes, static board, design and video spec. No HyperFrames render started.');
})().catch(e=>{console.error(e);process.exit(1)});
