# 大地图美术资源候选

## 当前项目状态

- 当前 `MainPrototype` 已经导入 KayKit Medieval Builder Pack 的一组低多边形模型。
- 已有可复用内容包括房屋、集市、矿洞、城门、城墙、瞭望塔、桥、井、树和岩石。
- `Walkable Ground` 与全部主地图道路已经使用同一套世界坐标平铺哑光 Shader，缩放时不会拉伸纹理。
- 草地使用 `mat_mainmap_grass`，道路使用 `mat_mainmap_dirt`，并统一为灰绿草地、暖灰褐道路与柔和暖光。
- 地图正式采用 [2.5D 武侠绘本美术规范](art_style_guide.md)：3D 低多边形手绘场景与 2D 像素角色的有意混合风格。

## 推荐候选

| 优先级 | 资源 | 用途 | 页面标注授权 | 当前状态 |
| --- | --- | --- | --- | --- |
| 1 | [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) | 树木、石头、植被、地貌和自然区域补充 | CC0 | 已找到，未下载、未导入 |
| 2 | [Kenney Castle Kit](https://www.kenney.nl/assets/castle-kit) | 城墙、城门、塔楼和边关区域 | CC0 | 已找到，未下载、未导入 |
| 3 | [Quaternius Medieval Village MegaKit](https://quaternius.com/packs/medievalvillagemegakit.html) | 更丰富的中世纪村庄建筑与模块化场景 | CC0；页面提供免费内容，完整源文件有会员版本 | 已找到，未下载、未导入 |
| 4 | [KayKit Medieval Builder Pack](https://kaylousberg.itch.io/kaykit-medieval-builder-pack) | 与当前地图风格最接近的建筑和场景延续 | CC0；页面为自由定价 | 项目已有精选模型 |

## 建议接入顺序

1. 先用 Kenney Nature Kit 补足森林、岩地和道路两侧的自然装饰。
2. 再用 Kenney Castle Kit 加强北门、南关和地图边界的视觉识别。
3. 如果需要把村落做得更丰富，再选择性接入 Quaternius Medieval Village MegaKit。
4. 下载后保留资源包自带的授权文件，并先做一个小区域风格测试，再批量替换当前原型资源。

## 约束

- 不在用户确认前购买付费资源或会员内容。
- “风格匹配”“授权清晰”“已经导入”分别记录，不把找到资源写成已经接入。
- 优先保持同一区域内模型比例、材质色调和轮廓语言一致，避免混用过多不同作者的资源。
