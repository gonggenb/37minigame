# Unity 6 美术 Pilot 人工验收说明

阶段 9 的推荐验收输出位于：

```text
Tools/AiArtAgentPlatform/pilot-output/wuxia-stage-9-r2
```

`pilot-output/wuxia-stage-9/` 是保留的第一轮返工证据：死亡动作在通用 4 px 中心漂移和 8% 尺寸漂移阈值下失败，但脚底基线漂移为 0 px。第二轮没有放宽全局动画预设，只为死亡动作设置 16 px 中心漂移、20% 尺寸漂移和 2 px 基线漂移覆盖；待机、移动、攻击和受击仍使用原有严格阈值。

本轮属于脚本开发任务，没有调用 Unity MCP，没有修改 Scene、Prefab、GameObject、Component 或 Inspector，也没有进入 Play Mode。以下步骤必须由用户在独立的 Editor 工作任务和测试任务中执行。

## Editor 工作任务

1. 在 Unity 项目中创建临时目录 `Assets/Art/Pilot/wuxia-stage-9-r2/`。
2. 从 Pilot 的 `outputs/` 复制角色、场景、物品、UI、动画和特效输出；不要复制 `references/`。
3. 角色、物品、UI PNG：设为 `Sprite (2D and UI)`，开启 `Alpha Is Transparency`，首轮验收关闭压缩。
4. 场景 PNG：保持 1920×1080，按项目当前相机与渲染路径选择 `Default` 或 `Sprite`。
5. 五动作 Sprite Sheet：设为 `Multiple`，按 256×256 网格切分，Pivot 使用 `Bottom Center`。
6. 特效 Sprite Sheet：按 256×256、4×4 网格切分，Pivot 使用 `Center`；材质分别人工比较 Alpha 与 Additive。
7. 创建临时 Animation Clip；待机/移动按 manifest 循环，攻击/受击/死亡不循环，FPS 参考各动作目录中的预览。

完成 Editor 配置后停止，不进入 Play Mode；下一轮单独发起测试任务。

## 测试任务

1. 在独立测试场景放置静态角色、场景、物品和 UI。
2. 播放待机、移动、攻击、受击和死亡五种动作，检查脚底基线、尺寸、身份和节奏。
3. 检查透明边缘是否有白边、黑边、色溢或不透明底色。
4. 在 64–128 px 显示尺寸检查物品/UI 可读性。
5. 检查特效单帧边界、首尾变化和混合模式。
6. 判断六类素材是否属于同一 Q 版水墨武侠体系。
7. 把问题写入 Pilot 输出目录的 `pilot-rework-log.md`。

## 当前已知限制

- 当前工作区没有 `.env`，未执行真实 OpenAI 生成或视觉评审。
- 受击动作是基于已批准基准帧的确定性位移/红闪代理，仅验证生产与导入管线。
- 素材库原始动作帧数不足时，离线试点使用循环补帧或末帧保持；正式生产仍应使用整条动作模型生成结果。
- Unity 尺寸、Alpha、动画节奏、混合模式和整体风格尚未人工确认，不能标记为最终通过。
