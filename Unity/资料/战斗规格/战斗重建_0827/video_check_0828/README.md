> 🔴 **本文件是【Godot 时代的账】（2026-08 及更早）—— ⛔ 已作废，只作历史。**
> 本项目早期用 **Godot 4.7.2** 做过一版复刻（`d:/warpforge/scripts/`，70 个 `.gd`），本文件是那一期的产物或其派生物。
> **现在唯一权威 = 原版解包资源** `d:/2/新解包资源/assets_full/` + 反编译 `d:/2/tools/decomp_full/`。
> ⚠️ 本文件里的 **y 向下坐标 / `unity_scene_to_godot.py` / gdshader / Z 反射 / `chain_rect` 的「Godot 绝对坐标」** 一律是历史管线遗留。
> 🔴 **2026-10-15 用户口径**：「**我们直接使用解包资料，不要被 Godot 时代的账这些干扰**」。
> ⚠️ 保留本文件**只是留痕**（当时怎么做的 / 为什么那么做），⛔ **别拿它当判据**。

# 结算视频检查归档（2026-08-28 会话产物）

> 归档用途：结算视频结构证据 + 战斗 UI 验收放大图（后续对照/复检可复用）。临时目录 tmp_video_0828 已删，内容迁至本目录。

## 一、结算视频结构结论（权威，勿重查）

- 源：`解包整理/05_视频/videos/{Victory,Draw,Defeat} Video.mp4`
- **3840×1080 = 左右双屏拼接**：左半=彩色结算徽章动画（含 VICTORY/DEFEAT/DRAW 字样=正片）；右半=同徽章**白色剪影遮罩层**（原版 VFX 合成用，用户曾疑为"白底"）
- 时长：Victory 3.71s / Draw 3.4s / Defeat 6.79s，24-25fps，VP8
- **使用**：项目 `assets/videos/{victory,draw,defeat}.webm`=左半裁切（crop=1920:1080:0:0，vp8 重编码）；`*_w.webm`=右半白剪影（crop x=1920）→ BattleDoors 双层播放（第二层 CanvasItemMaterial.BLEND_MODE_ADD=闪白合成）
- 转换工具：`Warpforge_tools/py312/python.exe` + imageio_ffmpeg 内置 ffmpeg（`get_ffmpeg_exe()`）；注意 filter+copy 不可共用（须重编码）

## 二、frames/（9 张）

- Victory_0_2/1_8/3_5.png、Draw_0_2/1_6/3_1.png、Defeat_0_3/3_3/6_5.png
- 每张=全幅缩 1920×540（左右并排可见），vision 分析结论：左=彩色徽章/右=白剪影（见上）

## 三、验收放大图（5 张）

- eh_zoom.png / eh_zoom2.png = 敌手牌卡背阵列顶部放大（证明阵列渲染+右上固定位置）
- quest_zone.png / replay_zone.png = 任务点区/回放钮区裁剪（证明已隐藏）
- quest_zoom.png = 任务点区（非 DA 局=无图标）

## 四、相关（本项目文件）

- 战斗重建_0827/VFX迁移规格_0828.md（事件→VFX 全链规格）
- 战斗重建_0827/子代理读报_front弹层_0827.md（BattleDoors/卡展窗/设置面板规格）
