# B9_W47SCALE —— 升级动画 `m_ScaleCurves` 的节点映射（报告）

产出：`资料/完全复刻/判据/升级动画scale曲线_节点映射.md`（本文件的详版；下面只留结论与坐标）。

## 结论

1. **`Level Up Card` 的 `path: ''` 挂的不是 `Upgrade Panel`，是 `CardUI`（卡视图根）。**
   `Animation` 组件在 `CardUI` 上（GO 88，localScale **250**），`CardDisplayLevelUpAnimationController` 才在 `Upgrade Panel`（GO 205）上、它的 `myAnimation` 指向 `CardUI` 那个 `Animation`。
   ⇒ 250 = 「1 卡单位 = 250 画布 px」，不是放大倍数；有效量是比值 **1 → 1.155908 / 1.155920（x/y）· 1.017216（z）→ 1**，峰值在 **1.45 s**、1.5833 s 落回。
2. **照抄会怎样**：我们的 `Animation` 挂在**窗根**，`path: ''` 会绑到窗根 ⇒ 整扇窗放大 250 倍；
   改写成 `Card Display/CardUI` 也有一条硬障碍 —— **1.45 s 的 `ChangeFrame` 会重建整棵卡树（正好是峰值）**。
   ⇒ **等价值 = 代码补间**（先例 `CardFan` 的开窗动画），值 = `CardView.BaseScale × 上面那组比值`，5 键 Hermite 逐键带 `si/so`。
3. **`Rank Up` 的 `Rank Icon`（path = `Division Image/Rank Icon`）现在写进 `division_clips.json` 的值整体大 1.0903292 倍**
   —— 我们的 `Rank Icon` 框（`RankIconR` = 73.51 px）已经把原版的 `localScale 1.0903292` 烘进去了、节点静止缩放是 **1**。
   修法：生成器对该 path 的 x/y/z 三条 **÷ 1.0903291702270508**（1.0903292→1 · 2.2275424→2.043 · 0.77230197→0.70832）。
   **不修 ⇒ `Rank Up` 播完 `Rank Icon` 永久大 9.02%**（末键 1.0903292 且无人复位，原生 + 手动两条驱动都会写）。
4. **`Rank Up`/`Division Up` 的 `Division Image` 那 3 条：现在是正确的**（值 0.8…1.12 是相对自身静止缩放的乘数，两边静止缩放都是 1）⇒ 不动。
5. **`Level Up Card` 的 `2DCard/Level Up Effect`（0.0045454 → 0.0051882）：我们树上没这个节点 ⇒ 现在绑不上。**
   将来建在同一层（`CardUI/2DCard` 下）时**逐字照抄**即可 —— 因为 `250/220` 与我们的 `(250/108)·s·108` 正好让 108 约掉。
   ⛔ 这不是「原版没有」，按铁律 11 属**要做**。

## 证据（文件:行号）

- 原版曲线原文：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`（`m_ScaleCurves` 2 条）· `bundle_menus_assets_all/AnimationClip/AnimationClip_4418588262184220504.json`（`Rank Up`，2 条）· `..._6413171015603015678.json`（`Division Up`，1 条）。交叉核 AssetRipper `.anim`（逐位相同）：`d:/2/tools/assetripper_out/ExportedProject/Assets/AnimationClip/{Level Up Card,Rank Up,Division Up}.anim`（`Level Up Card.anim:18-95`）。
- 原版 `path: ''` 的根 = `CardUI`：`.../Data/Scenes/MainMenu Warpforge.unity:12139-12155`（`Animation &622` → GO 88）· `:1685-1709`（GO 88 = `CardUI`）· `:47783-47808`（`RectTransform &1516`：`m_LocalScale {250,250,250}` · `m_SizeDelta {2.5437,3.3686}` · `m_Father 1589(Cards)`）· `:47795-47801`（子件含 `2DCard`、`Lighting Rays`）。
- 控制器在 Upgrade Panel：`:84302-84335`（MB `&2522`：`myAnimation → 622`，`upgradePanel → 2117`，`levelUpEffect1/2`）· `:3888`（GO 205 = `Upgrade Panel`）。方法体：`decomp_full/CardDisplayLevelUpAnimationController__{DoAnim,ChangeFrame,AnimationFinished}.c`、`CardDisplayLevelUpAnimationNotifier__{ChangeFrame,AnimationFinished}.c`。
- 原版 `Rank Up`/`Division Up` 的节点真值：`Assets/Ranked Division Change Window.prefab:925-943`（`Animation` 在根 GO）· `Rank Icon` tran `224743393880019028` = `localScale 1.090329` / `sizeDelta 67.4182²` · `Division Image` tran `224722951815217645` = `localScale 1` / `sizeDelta 851.41²`。
- 我们这边：`Core/LayoutSpace.cs:208-211`（108 px/世界单位）· `Core/CardView.cs:292-293`（`Width 2.0927 / Height 3.3313`）· `Core/CardFan.cs:100,112-113`（`SlotName(0)="CardUI"`、`FrontHpx = 3.3313×250 = 832.825`）· `Shell/CardDetailPopup.cs:1019`（`Card Display`）· `:1149-1150`（`SetPose(..., h/(Height·108))` ⇒ 静止 2.314815 = 250/108）· `Core/CardView.cs:4211-4234`（`2DCard` / `Front`）· `:10279-10283`（`ApplyScale = one×(_baseScale×_hlMul)`）· `Shell/CardDetailPopup.cs:3293-3319`（`Animation` 挂**窗根**）· `:997-998`（`Build()` 销毁全部子件）· `:2909-2915`（`ChangeFrame → RebuildKeepingState`）。
- 升段窗的接线现状：`Shell/RankedDivisionChangeWindow.cs:91,96`（`DivisionR` 851.40² / `RankIconR` 73.51²，**localScale 1**）· `:1259-1293`（建节点，名字/父链与原版一致）· `:584-586`（原生 `SetCurve`）· `:864-874`（手动 `ScaleTrack` 直接写 `n.localScale`）。数据：`Resources/division_clips.json` 的 `rankUp`/`divisionUp`（scale 通道现为**原版原值**）。生成器：`工具/gen_division_clips.py:161-170`（`_explode_scale`）· `:419-434`（断言首键 `1.09033`）。
- 「有意不生成」的原话：`工具/gen_anim_clips.py:28-30`（`m_ScaleCurves` 留空）· `:170`（生成器里那一行 `m_ScaleCurves: []`）。

## 没查清的部分

1. **legacy `Animation` 在播放中目标被销毁后的真实行为**（报错？静默停写？把重建出来的**同名**节点当新绑定？）—— 本代理不跑 Unity，**没有实测**，所以按最坏假设（`play() 时解析一次、销毁即失效`）设计。这条决定了 A 方案能不能走数据路；要验只需一次编辑期实验。
2. **`Rank Icon` / `Division Image` 的图**（原版 `m_Sprite = 0`，运行期服务端给；我们 `NoteNoArt`）——所以 ③④⑤ 的缩放**可见部分**目前只有 `Rank Icon Glow`（黄加色块）。判据缺在服务端，不是本机。
3. **`Level Up Effect` 在本窗的承载方式**（是否直接复用 `CardDisplayEffects.Build`）—— 属特效线口径，本代理不定。

## 该怎么用

- **要改 `Division Image/Rank Icon` 就改生成器**（`工具/gen_division_clips.py` 输出时 ÷ 1.0903291702270508，同时把 `:419-434` 断言首键期望改成 `1.0`），然后重跑生成器写两份 JSON。**⛔ 别在 `RankedDivisionChangeWindow.ApplyChannels` 里补除数** —— 那样原生 `SetCurve` 那一半还是错的（两条驱动读同一份 JSON，只有改数据才一次覆盖）。其实**改哪儿都行，但只能有一处**（项目准则：判据只留一份）。
- **要做根脉冲就走代码补间 + `CardView` 的「额外缩放」口**，⛔ 别把 250 写进 `.anim`（会放大整扇窗），⛔ 也别在 `CardDetailPopup` 里直接写 `transform.localScale`（会被下一次 `ApplyScale()` 静默冲掉：`Core/CardView.cs:10283`）。
- 逐条比值 / 关键帧 / 三轴差异 / 欠冲极值 / 施工清单 → 判据文件 §三·2 与 §五。

## 顺手发现

1. `工具/gen_anim_clips.py:29` 写「原版 `Upgrade Panel` 根的真实缩放」—— **根是 `CardUI`，不是 Upgrade Panel**。结论（别照搬）不变，但措辞会让下一个人去 Upgrade Panel 树里找 250（找不到）。
2. `Resources/Animations/Rank Up.anim` 与 `Division Up.anim` 是**无人加载的死资产**：全仓唯一 `Animations/<clip>` 的加载点是 `CardDetailPopup.cs:3289`，只取 `Level Up Card`；升段窗走的是 `Resources/division_clips.json`（`:529` `Resources.Load<TextAsset>`）。`gen_anim_clips.py` 仍在生成它们。
3. `Shell/CardDisplayEffects.cs:11` 的层级注释写 `… ▸ CardUI ▸ Cards ▸ Card Display ▸ 2DCard ▸ {Glow Explosion, Lighting Rays, …}` —— 实测主菜单树是 `Card Displayer Menu For Menu ▸ Card Display ▸ Cards ▸ CardUI ▸ 2DCard`，而且 **`Lighting Rays` 是 `CardUI` 的直接子件**（tran `1574` 的 `m_Father = 1516`），**不在 `2DCard` 下**。注释该订正。
4. 我们 `CardDetailPopup` 的卡树是 `Card Display/CardUI`，**比原版少一层 `Cards`**（原版 `Card Display ▸ Cards ▸ CardUI`；`Core/CardFan.cs:25,58` 的注释描述的是战斗树 `Card Display/Cards/CardUI`）。将来任何「按 legacy path 绑卡」的写法必须按本窗实际结构写，⛔ 别抄 `CardFan` 注释里那条。
5. `CardDetailPopup.SwapToFront`（`:1287`）**没有被 `UpgradeAnimLocked` 闸住**：升级动画 3.48 s 期间点一张相关卡仍会换前台 ⇒ 将来把脉冲做在「槽 0」上时，脉冲会跟着换到另一张卡上（原版那 3.48 s 里 `CardDisplayWindow` 的换位是否被关，本代理没查）。做 A 方案时要顺带定这一条。
