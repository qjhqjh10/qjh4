# 分层卡演示_0827/ — 分层卡面定稿演示图(2026-08-27,勿删)

> 🔴 **本文件是【Godot 时代的账】（2026-08 及更早）—— ⛔ 已作废，只作历史。**
> 本项目早期用 **Godot 4.7.2** 做过一版复刻（`d:/warpforge/scripts/`，70 个 `.gd`），本文件是那一期的产物或其派生物。
> **现在唯一权威 = 原版解包资源** `d:/2/新解包资源/assets_full/` + 反编译 `d:/2/tools/decomp_full/`。
> ⚠️ 本文件里的 **y 向下坐标 / `unity_scene_to_godot.py` / gdshader / Z 反射 / `chain_rect` 的「Godot 绝对坐标」** 一律是历史管线遗留。
> 🔴 **2026-10-15 用户口径**：「**我们直接使用解包资料，不要被 Godot 时代的账这些干扰**」。
> ⚠️ 保留本文件**只是留痕**（当时怎么做的 / 为什么那么做），⛔ **别拿它当判据**。

---

> 背景:用户裁决卡面结构=**最底完整卡面(画) → 卡框(整贴图含透明外沿)盖其上 → 画≤框环不超透明层;素材 1024² 方形等比显示(不变形)**;Unity JSON 权威(CardImage 2.7484²@(0,-0.044)/CardFrame 2.2452×3.2572@(0,-0.03))。
> 代码:scripts/sd_card.gd build() 分层(force_layered=true 跳过 PnP face;GameData.force_frame_tier4=true=全卡框最高级 tier4)。

| 文件 | 内容 |
|---|---|
| 分层卡渲染_荷尔马根_1024.png | **定稿**:1024×1024 输出,画≤框环(786-1126⊂768-1155),数值无彩圈只白字,方形等比无变形 |
| 分层卡渲染_荷尔马根_整屏.png | 同版 1920×1080 整屏(游戏内视角) |

**素材链**(新会话用):
- 用户指定卡面:`D:/2/新解包资源/assets_full/bundle_tyranidsleviathancardassets_assets_all/Texture2D/Tyranid_Leviathan_inf_Hormagaunt.png`(1024²;alpha 提取损坏→已修复版 `d:/warpforge/assets/cards/art/Tyranids_泰伦虫族/_demo_Hormagaunt_full1024.png`:按内容 bbox(180,0,844,1024)补 alpha=255,padding 保持透明);
- 卡框=新包 40k_Cardframe_{troop,stratagem}_{阵营}_tier{1-4}.png(非 SDF 1024²);**104 张已入库**项目 assets/cards/frames/<阵营>/;两套命名族(9 新式 Cardframe/4 旧式 Cardframes);
- 演示工具:d:/warpforge/tools_dev/shot_card_layered.gd(--script 运行,零写入)。

**已修 sd_card.gd 关键坑**(勿再踩):TextureRect "先赋 texture 再赋 size"会被钳回贴图尺寸(须先 size 后 texture 或 deferred);EXPAND_IGNORE_SIZE=最小尺寸 0(默认 KEEP_SIZE=贴图尺寸=钳制源);帧节点原先从未 add_child(8-27 前分层卡从未真正显示的原因)。
