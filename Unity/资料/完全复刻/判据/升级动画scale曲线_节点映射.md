# 升级动画的 `m_ScaleCurves` —— 原版节点 ↔ 我们节点 映射判据

> 代理：`B9_W47SCALE`（2026-10-27）· 只读普查 + 落判据，未改任何 `.cs` / `.py` / 资产。
> 上游：`B9_W37CLIP`（已确认「原版 3 条 clip 的曲线/事件/长度」，并**有意不生成 `m_ScaleCurves`**）。
> 本文件只回答一件事：**那 5 条 scale 曲线，原版绑在谁身上、值是什么量纲、落到我们树上该写什么数**。

**一句话结论**：5 条 scale 曲线里 ——
**3 条（`Rank Up`/`Division Up` 的 `Division Image`）我们已经是正确的，逐字照抄即可，不用动**；
**1 条（`Rank Up` 的 `Rank Icon`）现在写进 `division_clips.json` 的值比正确值大 1.0903292 倍**（我们的静止缩放是 1，原版是 1.0903292，那个因子我们已经烘进矩形了）；
**1 条（`Level Up Card` 的根 `path: ''`，值 250→288.98→250）不能照抄** —— 它的根不是「Upgrade Panel」，是 **`CardUI`（卡视图根）**，而我们的 `Animation` 组件挂在**窗根**上；照抄 = 整扇窗放大 250 倍。等价值 = 在**槽 0 的 `CardView` 根**上做一条 ×1.1559 的脉冲补间。
**最后 1 条（`Level Up Card` 的 `2DCard/Level Up Effect`）我们树上没有那个节点** ⇒ 现在绑不上（不是"原版没有"，是要做）。

---

## §一 原版逐条（path / 时间 / 值 / 目标属性）

**判据源（第一权威 = 解包原文）**：`d:/2/新解包资源/assets_full/{bundle}/AnimationClip/*.json` 的 `m_ScaleCurves`；
交叉核过 AssetRipper 导出的 `.anim`（两边**逐位相同**，见 §附 的脚本）。三条都是 `m_Legacy: true`，
`m_ScaleCurves` 的条目**没有 `attribute` 字段** ⇒ 绑的是 `Transform.localScale`（`classID 4`）。

### 一·1 `Level Up Card`（2 条）

源：`bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`
= `d:/2/tools/assetripper_out/ExportedProject/Assets/AnimationClip/Level Up Card.anim:18-95`

| # | `path` | 关键帧 `time` | 值 `{x, y, z}` | 目标属性 |
|---|---|---|---|---|
| ① | `''`（空 = **动画根自己**） | 0 | 250, 250, 250 | `Transform.localScale`（`classID 4`） |
| | | 1.4333333 | 250, 250, 250 | |
| | | 1.45 | **288.97699, 288.98001, 254.30411** | |
| | | 1.5833334 | 250, 250, 250（`inSlope/outSlope = 72.92988 / 68.22313 / 68.66892`） | |
| | | 1.8333334 | 250, 250, 250 | |
| ② | `2DCard/Level Up Effect` | 1.4333333 | 0.0045454539, 0.0045454539, 0.0045454539 | `Transform.localScale`（同上） |
| | | 1.6333333 | 0.0051881811, 0.0051881811, 0.0051881811 | |

- ① 的形状：**0 → 1.4333 恒 250**；1.4333→1.45（**16.7 ms**）冲到峰值；1.45→1.5833 落回 250，
  因为末键带**正向** `inSlope = 72.93` ⇒ 会**先跌到 249.82 再回到 250**（Hermite 欠冲，比例 **0.99929**，
  = 低 0.07%，肉眼不可见，但照抄要留着 —— 见 §附 的极小值复算）。
- ② 是**近似静止**的一条（0.004545 → 0.005188 只涨 **×1.1414**），时间窗 1.4333→1.6333。

### 一·2 `Rank Up`（2 条）

源：`bundle_menus_assets_all/AnimationClip/AnimationClip_4418588262184220504.json`
= `.../AnimationClip/Rank Up.anim:18-113`

| # | `path` | 关键帧 `time` | 值 `{x, y, z}` |
|---|---|---|---|
| ③ | `Division Image` | 0.3833333 | 0.8, 0.8, 0.8（`si/so = 1.026864`） |
| | | 0.75 | 1, 1, 1（`si 0.1691275 / so 0.06486487`） |
| | | 2.6 | 1.12, 1.12, 1.12（`si 0.06486487 / so 0.2035436`） |
| | | 2.65 | 0.8979172, **0.9020828, 0.9020828**（x 与 y/z 不同值，原版就这么写） |
| | | 2.8166666 | 1, 1, 1（`si/so = −1.075003`） |
| ④ | `Division Image/Rank Icon` | 0.3833333 | 1.0903292, ×3（`si/so = 0`） |
| | | 2.6 | 2.2275424, ×3（`si/so = 0`） |
| | | 2.65 | 0.77230197, ×3（`si/so = 0`） |
| | | 2.8666666 | 1.0903292, ×3（`si/so = 0`） |

### 一·3 `Division Up`（1 条）

源：`bundle_menus_assets_all/AnimationClip/AnimationClip_6413171015603015678.json`
= `.../AnimationClip/Division Up.anim:18-70`

| # | `path` | 关键帧 `time` | 值 `{x, y, z}` |
|---|---|---|---|
| ⑤ | `Division Image` | 0.4166667 | 0.8, ×3（`si/so = 1.026864`） |
| | | 0.7833334 | 1, ×3（`si 0.1691275 / so 0.06923077`） |
| | | 2.5166667 | 1.12, ×3（`si 0.06923077 / so 0.2035436`） |
| | | 2.5666666 | 0.8979172, 0.9020828, 0.9020828 |
| | | 2.7333333 | 1, ×3（`si/so = −1.075003`） |

> ⑤ 与 ③ **是同一条曲线**，只是整体晚 **0.0333 s**（0.4167 vs 0.3833），且中段 `outSlope` 从 0.06486487 变 0.06923077
> （因为段长变了）。两条都要按各自的时间轴播，⛔ 别合并成一条。

### 一·4 判据链要说准的两处（铁律 5：别混）

1. **`m_StopTime` 有两个字段、不是一个**：
   - `.anim`（AssetRipper 导出）里有 **`m_AnimationClipSettings.m_StopTime: 1`**（`Level Up Card.anim:564`）；
   - `assets_full` 的 **JSON 里没有 `m_AnimationClipSettings` 这个键**（现读 keys 列表），但它有
     **`m_MuscleClip.m_StopTime: 1.0`**（`AnimationClip_10.json`），且 `m_MuscleClipSize: 0`、
     `m_MuscleClip.m_Clip.m_DenseClip.m_FrameCount: 0`（= 纯 legacy，没有肌肉数据）。
   - ⇒ 两条都是 1.0，**都不是 length**：`Level Up Card` 的曲线到 **3.4833 s**、`Rank Up` 到 3.9167、`Division Up` 到 3.8333。
     `B9_W37CLIP` 的结论**成立**，这里只是把「两个字段」点名，避免下一个人以为「JSON 里没有 m_StopTime」。
2. **曲线条的「根」是谁**：见 §二 —— `path: ''` 指的是**挂 `Animation` 组件的那个 GameObject 自己的 Transform**，
   不是「clip 的某个父节点」，也不是「Upgrade Panel」。

---

## §二 原版节点身份与量纲（250 是什么）

### 二·1 `path: ''` 的根 = **`CardUI`**（不是 `Upgrade Panel`）

判据（全部 `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`）：

| 事实 | 出处 |
|---|---|
| `Animation` 组件 `&622` 的 `m_GameObject = {fileID: 88}` | `:12139-12155` |
| GO `88` 的 `m_Name = CardUI` | `:1685-1709` |
| `CardUI` 的 RectTransform `&1516`：**`m_LocalScale = {250, 250, 250}`** · `m_SizeDelta = 2.5437 × 3.3686` · `m_AnchoredPosition = (0, 60)` · `m_Father = 1589(Cards)` | `:47783-47808` |
| `CardUI` 的子件（`m_Children` 序）= `CreatedByText` · **`2DCard`** · `Card Ready for level up` · `New Card Badge` · `Ban Icon` · **`Lighting Rays`** | `:47795-47801`（逐个解 `m_GameObject → m_Name`） |
| 父链：`CardUI ▸ Cards ▸ Card Display ▸ Card Displayer Menu For Menu ▸ 3 - PopUp Holder ▸ Safe area Only Horizontal ▸ MainMenu ▸ Main Canvas ▸ Game UI`，**除 `CardUI` 外每一级 localScale 都是 1** | `:47783` 逐级 `m_Father` |
| `CardDisplayLevelUpAnimationController`（MB `&2522`，挂在 GO `205` = **`Upgrade Panel`**）的 `myAnimation = {fileID: 622}` ⇒ **控制器在 Upgrade Panel、动画在 CardUI** | `:84302-84335`；GO 205 = `Upgrade Panel` 在 `:3888` |
| `2DCard`（tran `&1323`，localScale 1）的子件含 **`Level Up Effect`**（tran `&1126`，**`m_LocalScale = 0.004545454`（= 1/220）**，`m_IsActive = 0`）与 `Glow Explosion`（`&1452`，`active = 0`） | 现读（§附 脚本） |

⇒ **`path: ''` = `CardUI` 自己的 `localScale`**。它那条 250 → 288.98 → 250 就是「卡视图根放大 15.59% 再弹回」
—— 与 `Upgrade Panel` 无关（Upgrade Panel 只是**控制器组件的宿主**，它的 Transform 不在任何一条 scale 曲线里）。

### 二·2 「250」是什么量纲 —— **原版画布像素 / 卡单位**，不是世界单位、不是乘数

- 原版 `CardUI` 的父链 localScale 全是 1、`Main Canvas` 是 UGUI 画布 ⇒ `CardUI` 的 `localScale = 250`
  就是「**1 个卡单位 = 250 画布 px**」。
- 交叉验证：`CardUI` 的子件 `2DCard` 的 `m_SizeDelta = 2.0927 × 3.3313`（卡单位）⇒ 实际画出来
  `2.0927 × 250 = 523.175 px` 宽、`3.3313 × 250 = 832.825 px` 高（**= 我们 `CardFan.FrontWpx/FrontHpx` 那两个数**，
  `Core/CardFan.cs:112-113`）。
- 子件 `Level Up Effect` 的 `1/220`（0.0045454）是**反向补偿**：`250 / 220 = 1.1364 px/卡单位`，
  让它 616.1 × 837.97 卡单位画成 `700.1 × 952.2 px`（≈ 比卡大一圈的光效）。
- ⇒ 所以 250 **不是**「放大 250 倍」的乘数，它是「这个节点静止时就有多大」。
  真正有意义的量是**相对值**：`288.98 / 250 = 1.15591`、`254.30411 / 250 = 1.01722`。

### 二·3 `Rank Up` / `Division Up` 的根与两个目标节点

Judgement file：`Ranked Division Change Window.prefab`（AssetRipper 导出，`Assets/Ranked Division Change Window.prefab`）

| 节点 | 序列化真值 | 出处 |
|---|---|---|
| `Animation` 组件 `&111220653531496868` 挂在根 GO `1709254077376921` = **`Ranked Division Change Window`**，`m_Animations = [Division Up, Rank Up, Division Config]`，`m_PlayAutomatically = 0` | `:925-943`（`:936-937` 两条 guid） |
| 根 `Ranked Division Change Window` 的 `m_LocalScale = {1,1,1}` | 现读（§附 脚本） |
| `Division Image`（tran `224722951815217645`）：**`localScale = {1,1,1}`** · `m_SizeDelta = 851.41 × 851.41` · 父 = 窗根 | 同上 |
| `Division Image/Rank Icon`（tran `224743393880019028`）：**`localScale = {1.090329, 1.090329, 1.090329}`** · `m_SizeDelta = 67.4182 × 67.4182` · 父 = `Division Image` | 同上 |

⇒ `Rank Icon` 的**视觉尺寸** = `67.4182 × 1.0903292 = 73.51 px`（**静止缩放是 1.0903292，不是 1**）。

---

## §三 我们的映射表

### 三·1 我们的树长什么样（现读）

| 我们这边 | 实值 / 出处 |
|---|---|
| 世界基准 | **1 世界单位 = 108 设计 px**（`Core/LayoutSpace.cs:208-211`，`Px(px)=px/108`） |
| `CardView` 的单位 | `Width = 2.0927` · `Height = 3.3313`（`Core/CardView.cs:292-293`）—— **与原版 `2DCard.m_SizeDelta` 逐位相同** |
| 卡视图根（槽 0）的缩放 | `CardDetailPopup.cs:1149-1150`：`SetPose(..., h / (CardView.Height * 108f))`，`h = CardFan.Hpx(0) = 3.3313 × 250` ⇒ **2.3148148 = 250 / 108** |
| 卡树路径（本窗实际） | 窗根 ▸ **`Card Display`**（`CardDetailPopup.cs:1019`）▸ **`CardUI`**（`SlotName(0)`，`Core/CardFan.cs:100`）⇒ 相对路径 **`Card Display/CardUI`**（⚠️ 比原版少一层 `Cards`，原版是 `Card Display▸Cards▸CardUI`） |
| `CardView` 内部的卡单位层 | `2DCard`（localScale 1，`Core/CardView.cs:4211-4216`）▸ `Front`（`Core/CardView.cs:4230-4234`）—— **与原版同名同层** |
| 缩放写入口 | `ApplyScale()` = `localScale = one × (_baseScale × _hlMul)`（`Core/CardView.cs:10279-10283`）；静止时 `_hlMul = 1` |
| 升级动画的 `Animation` 宿主 | **窗根**（`CardDetailPopup.cs:3298`：`GetComponent<Animation>()`，缺则 `gameObject.AddComponent<Animation>()`）；窗根 localScale = 1（`WindowHolder` 下、`CardDetailPopup.cs:2877-2878` 的注释 + 全仓无写它 localScale 的地方） |
| 升段窗的节点 | `Division Image`（`RankedDivisionChangeWindow.cs:91` `DivisionR` = 851.40²，**localScale 1**）· `Division Image/Rank Icon`（`:96` `RankIconR` = 73.51²，**localScale 1**，`1.0903292` **已经烘进这个框**）；`BuildDivisionImage/BuildRankIcon` 在 `:1259-1293` 建的就是这两个名字、这条父链 |
| 升段窗的 scale 驱动 | 两条都在：① 原生 `clip.SetCurve(path, typeof(Transform), "localScale.x/y/z")`（`:584-586`）；② 手动通道 `ApplyChannels` 的 `ScaleTrack`（`:864-874`）**把 `Evaluate(t)` 直接写进 `n.localScale`** |

### 三·2 逐条映射

| # | 原版 path | 我们对应的节点 | 换算规则 | 现在能不能绑 |
|---|---|---|---|---|
| ① | `''`（= 原版 `CardUI`） | **`Card Display/CardUI`**（槽 0 的 `CardView` 根） | **÷108**（`250/108 = 2.314815`）—— 因为我们的卡视图根静止就是 `250/108`，原版是 `250`，两者差一个 108（= px/世界单位） | ⚠️ **不能按原样写进 clip**：我们的 `Animation` 在**窗根**，`path: ''` 会绑到**窗根**（整扇窗放大 250 倍）；写成 `Card Display/CardUI` 又会在 **1.45 s（正好是峰值）被 `ChangeFrame` 的重建销毁**。⇒ 走代码补间（§五·A） |
| ② | `2DCard/Level Up Effect` | **我们没有这个节点**（本窗 `CardUI/2DCard` 下只有 `Front` + 卡面各层） | 若将来建在同一层：**逐字照抄 0.0045454 / 0.0051882**（推导见下） | ❌ 绑不上（路径不存在） |
| ③ | `Division Image`（`Rank Up`） | `Division Image`（本窗直挂窗根） | **逐字照抄**（值 0.8…1.12 是「相对自身静止缩放」的乘数，我们的静止缩放也是 **1**，原版也是 **1**） | ✅ 已绑上（原生 + 手动两条）· **值正确，不用改** |
| ④ | `Division Image/Rank Icon` | `Division Image/Rank Icon`（`RankedDivisionChangeWindow.cs:96` 那颗，localScale 1） | **÷1.0903292**（我们的静止缩放 = 1，原版 = 1.0903292，那个因子已经烘进 `RankIconR` 的 73.51 px 视觉框） | ✅ 已绑上，**但值偏大 1.0903292 倍** ⇒ 见 §五·B |
| ⑤ | `Division Image`（`Division Up`） | 同 ③ | 同 ③ | ✅ 已绑上 · **值正确，不用改** |

#### ② 的「逐字照抄」推导（为什么下面那一层不用除 108）

- 原版：`CardUI(250) × 2DCard(1) × LevelUpEffect(1/220)` ⇒ **1.13636 px / 卡单位**。
- 我们（若建在 `CardUI/2DCard` 下、localScale = s）：`CardUI(250/108) × 2DCard(1) × Effect(s)` ⇒ 世界单位 `= 2.3148·s`，
  换成 px 再 `×108` ⇒ **`250·s` px / 卡单位**。
- 令 `250·s = 1.13636` ⇒ **`s = 0.0045454`（与原版逐位相同）**。
- ⇒ **判据规则（一句话）**：**`CardUI` 这一级（含）以上要 ÷108；`2DCard` 以下（不含）逐字照抄。**
  因为我们的卡树子树沿用原版「卡单位」，只把这一个 108 倍差压在卡视图根上。

#### ④ 的换算（数值）

| 原版值 | 我们的值（÷1.0903291702270508） |
|---|---|
| 1.0903292（静止） | **1** |
| 2.2275424（峰值） | **2.043**（2.042999913） |
| 0.77230197（回弹底） | **0.70832**（0.708320013） |
| 1.0903292（回到静止） | **1** |

**不修的后果（可量）**：`Rank Up` 播完之后 `Rank Icon` 会**永久停在 9.02% 偏大**的尺寸上
（末键 = 1.0903292、而我们的框已经含这个因子；两条驱动都会写，没有任何地方复位），
子件 `Rank Icon Glow` 跟着一起大 9.02%。峰值那一下也不是 ×2.043 而是 ×2.228（多放大 9%）。

---

## §四 做/不做的裁定依据（铁律 11 口径）

> 铁律 11：与原版不符 / 复刻有缺漏 —— **全部都要做**，只有先后之分；
> 只有两种情况可以不做：① **原版本身就没有 / 判据是空的**；② **用户明确拍板不做**。
> ⛔ 「影响小 / 成本高」不能当理由。

| # | 照抄会怎样 | 等价做法 | 裁定 |
|---|---|---|---|
| ① | **灾难**：`Animation` 挂在窗根，`path: ''` 绑到窗根 ⇒ 整扇弹窗（遮罩 + 卡 + 文字 + 按钮）放大 250 倍，一帧就看不见了 | **代码补间**：在槽 0 的 `CardView` 根上做「静止缩放 × 比值」的脉冲（比值 = 1 → 1.155908/1.155920/1.017216 → 1，5 键 Hermite 逐键带 `si/so`）。**先例 = `CardFan` 的开窗动画**：原版那条 clip 我们也没搬（`Core/CardFan.cs:72-74`「本工程不建那颗 Animation 组件 ⇒ 等价物是代码补间」） | **要做**（原版的升级动画必须有这一下）。**不属铁律 11 的例外** —— 这是「等价实现」，不是「不做」 |
| ② | 绑不上（路径不存在）⇒ 静默无效 | 先建 `Level Up Effect` / `Level Up Effect Down`（本窗的 `CardUI/2DCard` 下），再在同一支补间里照抄 1/220 → 0.0051882 | **要做**。⛔ 不能判「不做」—— `Level Up Effect` 是**原版有、我们没建**（不是原版没有）。卡的其它 8 条曲线（`m_Color.a` / `floatProperty1` / `m_IsActive`）同理，全部指着这个节点 |
| ③ | 现在是正确的（值 + 静止缩放都对上） | —— | **保持**（不属待办；改它反而错） |
| ④ | 静止多 9.02%、峰值多 9%（且**播完不复位**） | 生成器输出时 ÷1.0903291702270508；断言首键期望从 `1.09033` 改 `1` | **要做**（真缺陷，见 §五·B） |
| ⑤ | 同 ③ | —— | **保持** |

> ⚠️ 另外如实记一条**判据不足**（不是「不做」）：原版 `Level Up Effect` 的图/材质在 `CardDisplayEffects`
> 那套 JSON 里有、我们只在**战斗卡的展示窗**建了（`Battle/CardDisplayWindow.cs:646` 调 `CardDisplayEffects.Build`），
> **本窗（`Shell/CardDetailPopup`）没调**。要不要在本窗复用它 → 属 `特效动画线`，本文件只指出「不建 ⇒ ② 那条永远绑不上」。

---

## §五 可施工清单 + 还差什么

### A. `Level Up Card` 的根脉冲（#①）—— 代码补间（⛔ 不要写进 clip）

**为什么不能写进 clip（三条，任一都够）**
1. 我们的 `Animation` 组件在**窗根**（`CardDetailPopup.cs:3298`）；`path: ''` 绑的是「组件所在 GO」⇒ 会绑到窗根。
2. 写成 `path: 'Card Display/CardUI'` 可以绑到卡上，但 **`ChangeFrame`（1.45 s）会 `RebuildKeepingState()`**
   （`CardDetailPopup.cs:2909-2915` → `Build()` 首句把窗根所有子件销毁重建，`:997-998`）
   —— 而 **1.45 s 正好是这条曲线的峰值**。legacy `Animation` 在 `Play()` 时解析一次绑定，
   目标被销毁后**没有任何机制把绑定换到新节点上** ⇒ 脉冲停在峰值（卡永久大 15.6%）。
   ⚠️ 这条「销毁后绑定的行为」**本代理没跑 Unity、不能断言**（不跑就不说死）；但**按最坏假设设计**是唯一安全的。
3. 就算 2 不成立，我们的**手动通道**（本轮 `CardDisplayLevelUp` 没有手动通道）也不会去写它。

**补间的规格（照抄要的每一个数）**

- 目标节点：`Card Display/CardUI`（槽 0）。**每次 `RebuildKeepingState()` 之后要重新取**（`OnLevelUpChangeFrame` 里 `RewireLevelUpTargets()` 旁边）。
- 基准值：`CardView.BaseScale`（槽 0 ⇒ `2.3148148`，⛔ 别写死，`SetPose` 切分辨率/换位会改）。
- 比值曲线（= 原版值 ÷ 250，**照抄这个**，与我们的绝对缩放无关）：

| x | y | z | 说明 |
|---|---|---|---|
| 1 | 1 | 1 | t = 0 |
| 1 | 1 | 1 | t = 1.4333333 |
| **1.155907959** | **1.155920044** | **1.017216431** | t = 1.45 |
| 1 | 1 | 1 | t = 1.5833334（`si/so(x) = 72.92988` · `(y) = 68.22313` · `(z) = 68.66892`） |
| 1 | 1 | 1 | t = 1.8333334 |

- 逐键**必须写 `inTangent/outTangent`**（`new Keyframe(t, v)` 会把段变成 ease-in/out，端点仍对 ⇒ 端点断言抓不到）。
  原版 `weightedMode` 全 **0**（= `WeightedMode.None`，权重被忽略）⇒ 运行期用 `new Keyframe(t, v, inTangent, outTangent)` 即**逐位等价**。
- x 与 y 差 0.000012（`288.977` vs `288.98`）、z 是另一条（1.0172）：**三轴各一条曲线，照抄**。
- 欠冲：末段 Hermite 会跌到 **0.99929**（250 侧是 249.822）再回 1，照抄即可。
- 收尾：`AnimationFinished`（3.4833 s）与 `SettleLevelUpAnimation`（`CardDetailPopup.cs:2950-2958` 的 `Tick(1000)`）都要把比值归 **1**；
  ⛔ 别只归 x/y 忘了 z。
- **写入口必须走 `CardView`**：`ApplyScale()` 是 `Vector3.one × (_baseScale × _hlMul)`（`Core/CardView.cs:10283`），
  任何 `SetPose` / `SetHighlight` / `SetBaseScale` 都会把它冲掉 ⇒ 需要给 `CardView` 加一个「额外缩放 `Vector3`」字段，
  并让 `ApplyScale()` 乘进去（与 `_hlMul` 同一个位置）。⛔ 不要在 `CardDetailPopup` 里直接写 `transform.localScale`（会被下一次 `ApplyScale` 静默冲掉）。

### B. `Rank Icon` 的比值修正（#④）—— 改生成器 + 重跑

1. `工具/gen_division_clips.py` 的 `_explode_scale()`（`:161-170`）之后，对 **path == `"Division Image/Rank Icon"`** 的**三条（x/y/z）**统一 **÷ `1.0903291702270508`**；
   把这个除数写成带出处的常量（本文件 §二·3），并在文件头加一行「映射判据 → `资料/完全复刻/判据/升级动画scale曲线_节点映射.md` §三·2 ④」。
2. 同步改 `:419-434` 那段自检：`("Division Image/Rank Icon", 1.09033)` → `("…", 1.0)`，并把断言注释改成「**映射后的值**（原版 1.09033 ÷ 静止缩放 1.0903292）」。
3. 重跑 `python -I -X utf8 d:/4/Unity/工具/gen_division_clips.py`（会写两份：`数据/游戏数据/division_clips.json` + `MyGame/Assets/CardPresentation/Resources/division_clips.json`）。
4. 验收（改了就能测，不需要 Unity）：读回 `Resources/division_clips.json`，断言
   `rankUp` 的 `Division Image/Rank Icon` 三条 x 首键 == `1.0`、`2.6` 键 == `2.043`、`2.65` 键 == `0.70832`、末键 == `1.0`；
   `Division Image` 那三条（③⑤）**逐位不变**。

### C. `2DCard/Level Up Effect`（#②）—— 依赖建节点

- 前置：把 `Level Up Effect` / `Level Up Effect Down` 挂到**本窗**的卡树（`CardUI/2DCard` 下）；
  现成资产 = `Shell/CardDisplayEffects.cs`（它的 json 里就有这两条，`parent: "2DCard"` / `"Level Up Effect"`），
  但它现在只被 `Battle/CardDisplayWindow.cs:646` 调用。
- 建好之后：scale 曲线**逐字照抄** 0.0045454 → 0.0051882（推导见 §三·2 ②），走同一支代码补间（同样要处理 1.45 s 重建）。
- ⚠️ 顺带：`CardDetailPopup.RewireLevelUpTargets()`（`:2918-2928`）现在按名找不到 `Level Up Effect` / `Level Up Effect Down`
  ⇒ `levelUpEffect1/2` 是 `Valid == false`（`FadeImage` 出声一次）。建了节点这条会自己接上，**不用改那段代码**。

### 还差什么（如实记）

| 缺什么 | 影响哪条 | 怎么补 |
|---|---|---|
| **没跑 Unity**：`Animation` 在播放中被销毁目标后的实际行为（是否报错 / 是否停写 / 是否把新同名节点当绑定） | 决定 A 能不能走数据路 | 进一次 player 或写一条编辑期实验（挂 clip→播→中途换节点→读 `localScale`）。**在此之前按最坏假设**（走代码补间） |
| `Rank Icon` 的**图**（`m_Sprite = 0`，运行期由服务端给） | ④③⑤ 的**可观性**：图不画出来，缩放只影响 `Rank Icon Glow`（黄加色块） | 判据缺（服务端 `RankedDivisions.GetRankData`），已记在 `RankedDivisionChangeWindow.cs:296` 的 `NoteNoArt` |
| `Level Up Effect` 在本窗的承载 | ② | 见 C |

---

## §附 可复现脚本

```bash
# ① 三条 clip 的 m_ScaleCurves 全量（JSON 侧）
python -I -X utf8 - <<'EOF'
import json,io
F={'Level Up Card':'d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json',
   'Rank Up':'d:/2/新解包资源/assets_full/bundle_menus_assets_all/AnimationClip/AnimationClip_4418588262184220504.json',
   'Division Up':'d:/2/新解包资源/assets_full/bundle_menus_assets_all/AnimationClip/AnimationClip_6413171015603015678.json'}
for n,p in F.items():
    d=json.load(io.open(p,encoding='utf-8'))
    print('##',n,'legacy',d['m_Legacy'],'muscleStop',d['m_MuscleClip']['m_StopTime'],'muscleSize',d['m_MuscleClipSize'])
    for c in d['m_ScaleCurves']:
        print(' path=%r'%c.get('path'))
        for k in c['curve']['m_Curve']:
            print('   t=%r v=%r si=%r so=%r'%(k['time'],k['value'],k['inSlope'],k['outSlope']))
EOF

# ② 原版 CardUI 的 localScale / 父链 / 2DCard 子件（MainMenu 场景）
python -I -X utf8 - <<'EOF'
import io,re
p=r"d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity"
L=io.open(p,'rb').read().decode('utf-8','replace').split('\n')
idx={}
for i,l in enumerate(L):
    m=re.match(r'^--- !u!(\d+) &(-?\d+)',l)
    if m: idx[m.group(2)]=(int(m.group(1)),i)
def get(f,n):
    c,i=idx[f]
    for j in range(i,min(i+300,len(L))):
        if L[j].startswith('  '+n+':'): return L[j].split(':',1)[1].strip()
    return None
def goname(t):
    g=re.search(r'fileID: (-?\d+)',get(t,'m_GameObject')).group(1)
    return g,get(g,'m_Name')
for t in ['1516']:
    print(t,goname(t),'scale',get(t,'m_LocalScale'),'sizeDelta',get(t,'m_SizeDelta'))
EOF

# ③ 我们这边的静止缩放（不改代码，只算）
python -I -X utf8 - <<'EOF'
W,H=2.0927,3.3313           # CardView.Width/Height
hpx=H*250                   # CardFan.Hpx(0) = 3.3313*250 = 832.825
print('card root rest scale =',hpx/(H*108),' == 250/108 ->',250/108)
print('root peak  mapped   =',288.97698974609375/108,' / ',288.9800109863281/108,' / ',254.30410766601562/108)
print('RankIcon rest factor baked in rect =',67.4182*1.0903291702270508)   # 73.508 == RankIconR 的 73.51
for v in (1.0903291702270508,2.2275424003601074,0.772301971912384):
    print('  RankIcon mapped:',round(v/1.0903291702270508,7))
EOF
```

现读数值（本代理实跑，2026-10-27）：
`CardUI.localScale = {250,250,250}` · `sizeDelta = {2.5437,3.3686}` · 父链除它自己外全 1 ·
`Level Up Effect.localScale = 0.004545454` · `Rank Icon.localScale = 1.090329` · `Division Image.localScale = {1,1,1}` ·
根脉冲欠冲极小值 **249.822474（比例 0.99929）@ t = 1.57841**。
