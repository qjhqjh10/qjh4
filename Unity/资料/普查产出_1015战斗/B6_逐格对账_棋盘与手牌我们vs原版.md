# B6 · 逐格对账：**棋盘槽位 / 手牌布局 / 相机** —— 我们 vs 原版

- **日期**：2026-10-15
- **甲方（原版真值 = 尺子）**：`资料/普查产出_1015战斗/B4_战斗相机与棋盘几何.md`（351 行，来源 `assets_full/bundle_scenes_scenes_battlearena2`，B4 §2.4(b) 的槽位是**运行时重建值**）
  ＋ 本次补查两处运行时证据：`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`（1243 行，原版实况 UI dump）· `资料/战斗复刻_正本.md` §2·3/§2·9/§2·10（同日另开的正本）
- **乙方（我们的实现 = 待核对象）**：`Board/BoardLayout.cs` · `Board/ArenaSlots.cs` · `Hand/HandLayout.cs` · `Hand/CardInteraction.cs` · `Battle/CombatAutoZoom.cs` · `Battle/CombatCameraZoom.cs` · `Battle/BattleCameraSreenSize.cs` · `Battle/ScreenAspectRatioController.cs` · `Battle/ArenaSceneState.cs` · `Editor/BattleScene.cs` · `WarpforgeArena1/Editor/ArenaBuilder.cs` · `Core/LayoutSpace.cs` · `RuleEngine/Core/GameplayVariables.cs`
- **规格旁证**：`资料/手牌布局_原版算法与参数.md`（400 行，2026-09-25 实读 + 10-17 订正）· `资料/对战排版_原版数值与改造方案.md` §四（**已过期**：它用的是序列化死值 −3/−6/−9，见下）
- **量纲**：原版 3D 世界 = `MinionArea` 局部系（`BattlePrefab` 链上全是单位变换 ⇒ 局部 = 世界）；原版 UI 世界单位 = **14.835 px**（fov40 / 平面 100）；我方「世界单位」= **108 px**（可见高恒 10 单位 = 1080 px，`LayoutSpace.cs:36`）；两把尺差 **7.28 倍**（`HandLayout.cs:373-377`）。**换算桥 = `×14.835/108`**，⛔ 下文凡跨尺必标。
- **判定口径**：✅ 一致 ｜ ⚠️ 语义一致但**数值不同**（换算/口径差，不是错） ｜ ❌ 不一致 ｜ ➖ 有意挑的（代码里自己标了） ｜ ⏳ 待判

---

## 表一 · 槽位几何逐槽对照

> 🔴 **我们有两条独立的路，别混**：
> ① **3D 落点** = `ArenaSlots.Position`（真机 `use3D = boardCam != null`，`BattleScene.cs:22867`）—— 判定命中、落点、底片投影全走它；
> ② **2D 槽位** = `BoardLayout.SlotPosition/NxOf`（**只在没有 3D 资产时的兜底**那条路上决定落点；平时只剩 `_band` 宽度 `BoardLayout.cs:298` 在用）。
> **格号映射**：`ourSlot = 4 + 原版有符号下标 i`（i<0 = 原版左半场 `left[]`，i>0 = `right[]`；督军位 = 4，`BoardLayout.cs:58` / `ArenaSlots.cs:53`）✅ 与 B4 §2.4(d) 一致。

### 1.1 我方行（y = 0；`MinionArea` z = −6.655）

| ourSlot | 原版 i | **原版 x（运行时，B4 §2.4(b)）** | 原版屏幕偏移 px² | **我们 3D x（`ArenaSlots.cs:98-110`）** | **我们 2D x（`BoardLayout.cs:437-441`）** | 2D 屏幕偏移 px¹ | 判定（3D / 2D） |
|---|---|---|---|---|---|---|---|
| 0 | −4 | **−3.37** | −613.8 | −3.37 ✅ | −5.5296 | −597.2 | ✅ / ⚠️ 差 **+16.6 px** |
| 1 | −3 | **−2.55** | −464.5 | −2.55 ✅ | −4.1472 | −447.9 | ✅ / ⚠️ 差 +16.5 |
| 2 | −2 | **−1.73** | −315.1 | −1.73 ✅ | −2.7648 | −298.6 | ✅ / ⚠️ 差 +16.5 |
| 3 | −1 | **−0.91** | −165.8 | −0.91 ✅ | −1.3824 | −149.3 | ✅ / ⚠️ 差 +16.5 |
| **4** | 督军 | 基准（z 另加 −0.42） | — | (0,0,**−7.075**) ✅ | x=0 | 0 | ✅ / ✅ |
| 5 | +1 | **+0.91** | +165.8 | +0.91 ✅ | +1.3824 | +149.3 | ✅ / ⚠️ |
| 6 | +2 | **+1.73** | +315.1 | +1.73 ✅ | +2.7648 | +298.6 | ✅ / ⚠️ |
| 7 | +3 | **+2.55** | +464.5 | +2.55 ✅ | +4.1472 | +447.9 | ✅ / ⚠️ |
| 8 | +4 | **+3.37** | +613.8 | +3.37 ✅ | +5.5296 | +597.2 | ✅ / ⚠️ |

### 1.2 敌方行（y = 0；`MinionArea` z = +1.043）

| ourSlot | 原版 i | **原版 x（运行时）** | 原版屏幕偏移 px³ | **我们 3D x** | **我们 2D x** | 2D 屏幕偏移 px¹ | 判定（3D / 2D） |
|---|---|---|---|---|---|---|---|
| 0 | −4 | **−6.25** | −538.8 | **+6.25** | +5.5296→镜像 −5.5296 | −597.2 | ⏳ **符号待判** / ⚠️ |
| 1 | −3 | **−4.72** | −406.9 | **+4.72** | −4.1472 | −447.9 | ⏳ / ⚠️ |
| 2 | −2 | **−3.19** | −275.0 | **+3.19** | −2.7648 | −298.6 | ⏳ / ⚠️ |
| 3 | −1 | **−1.66** | −143.1 | **+1.66** | −1.3824 | −149.3 | ⏳ / ⚠️ |
| **4** | 督军 | 基准（z 另加 −0.75） | — | (0,0,**+0.293**) ✅ | x=0 | 0 | ✅ / ✅ |
| 5 | +1 | **+1.66** | +143.1 | **−1.66** | +1.3824 | +149.3 | ⏳ / ⚠️ |
| 6 | +2 | **+3.19** | +275.0 | −3.19 | +2.7648 | +298.6 | ⏳ / ⚠️ |
| 7 | +3 | **+4.72** | +406.9 | −4.72 | +4.1472 | +447.9 | ⏳ / ⚠️ |
| 8 | +4 | **+6.25** | +538.8 | −6.25 | +5.5296 | +597.2 | ⏳ / ⚠️ |

**注 1（px）**：我方行 **182.14 px/世界单位**、敌方行 **86.2 px/世界单位**（两行各自深度的 px/单位；`ArenaSlots.cs:27-29` 与 `资料/3DBody_原版场上卡体规格.md` §四之三）。**全场 x 的集合（±0.91/±1.73/±2.55/±3.37 与 ±1.66/±3.19/±4.72/±6.25）我们 3D 逐值全中**，唯二的差别是 ① 敌方符号（见下）② 上面那 16.5 px。
**注 3（px）**：敌方 px/单位 86.2 是**该深度的透视刻度**，故 1.66 × 86.2 = 143.1 px（不是「同一把尺」的换算）。

### 1.3 表一的三条判定（逐条给依据）

1. ✅ **间距/极值/等距性 100% 复刻**：`ArenaSlots.cs:56-70` 的 `MinionSeparation 0.82/1.53` · `minionExtraDistanceFromHero 0.09/0.13` · `desiredScale 0.36/0.69` · `heroScale 0.40/0.77` · `heroExtraOffset z −0.42/−0.75` 与 B4 §2.4(a) **逐值相同**；`Position()` 的算式 `x = ±(k·Pitch + Extra)`、`y ≡ 0` 与 `MinionManager__Awake` 的运行时重建式**逐项相同**（B4 §2.4(b)）。
2. ⏳ **敌方「镜像」符号 —— 两条判据打架，本轮判不了**：
   · **支持镜像**：B4 §2.4(a) 的**序列化** `leftSlotPosNormal`（敌方 = **+x**）⇒ 我们 `BoardLayout.mirror = true`（`BattleScene.cs:22812-22820`）+ `ArenaSlots.Position` 的 `enemy ? −x : x`（`ArenaSlots.cs:109`）。旧文档 `对战排版_原版数值与改造方案.md:182` 也是照这一条写的。
   · **不支持镜像**：B4 §2.4(b) 的**运行时重建**（`MinionManager__Awake.c:38-96`）对**两个** MinionManager 写的是**同一套** `left[i].x = −(sep·(i+1)) − extra`（取负常量实读于 `GameAssembly.dll` VA `0x1834b2c80`）⇒ 敌方的 `left` 也在**世界 −x**（画面左）。而 B4 自己把这一对字段标成「**序列化值 ≠ 运行时值**、序列化那套是死值」。
   · ⇒ **若运行时那条为准，我们敌方的 9 格整体左右翻了**：`ourSlot 0` 会显示在**画面右**，而原版的 `i = −4` 在**画面左**。影响的是**语义**（「我打你的槽 3」指哪一格、相邻/最外侧类效果落到谁身上），几何集合不变。
   · **怎么结案（便宜）**：进原版实况把敌方 `MinionArea` 的第 4 个 `left` 槽 dump 出来（`SceneJumpShot` 能 dump Transform 位置），比 x 的符号即可。⛔ 在那之前**别照猜改代码**。
3. ⚠️ **2D 那一路少一个 `minionExtraDistanceFromHero` 偏移 ⇒ 每格偏 16.5 px**：`BoardLayout` 的 x = `(slot−4) × 149.3 px`（等距、以督军为对称中心，`BoardLayout.cs:64/437-441`），而原版是 `(k·0.82 + 0.09) × 182.14` ⇒ **每格比原版近 0.09 世界单位 = 16.4 px**（我方；敌方 0.13 × 86.2 = 11.2 px，且步进应该是 131.9 px 而不是 149.3 px）。**影响面小**（`use3D` 时落点判定与底片都走 3D 投影，`BoardLayout.cs:254-288`），但**没有 3D 资产时**（兜底路）它会决定落点。
4. ➖ **2D 敌行借用了我方步进（149.3 px，应 131.9 px）+ 同尺寸**：`BoardLayout.cs:33-34` **自己标了**「原版敌方那一行是投影收窄的…2D 里不适用 —— 这边同尺寸、只镜像顺序」⇒ 有意挑的，不是漏。
5. ❌ **`origPosReduction` / `AdjustMinionBoardPos` 全仓 0 命中**（我方 `(0.66, 2.75)` · 敌方 `(0.65, 0.275)`，B4 §2.4(d)）：我们只实现了 (b) 的**裸 x**。B4 的措辞是「**算最终落点要用它**，别用裸 x」⇒ 若它在落点那条路上，我方 x 会**偏大约 51%**（1/0.66）。⏳ **待判它是否真在落点路径上**（`origPosReduction` 的 y 分量 2.75 乘在恒 0 的槽位 y 上是空操作、x×0.66 会把棋盘收窄到 ±2.22 —— 与我们已核对的屏幕步距 149.3 px = 0.82×182.14 不符，**旁证倾向「(b) 才是落点」**）。**要给这条找判据**：读 `AdjustMinionBoardPos` 的调用点。
6. ➖ **`slotsPerSide` 同名不同义（已知，不是新发现）**：我们 `RuleEngine/Core/GameplayVariables.cs:125 = 9`（= **每边总格**），原版 = **左/右半场各几格**（序列化 4 / ctor 3，运行时权威值**查不到**）。⚠️ 它**今天不影响**槽位布局（全仓只声明、无消费点）；影响的是「将来接原版 SO」那一脚。

---

## 表二 · 手牌布局逐字段对照（`CardsHorizontalLayout`；原版 我方 5489 / 敌方 4248）

| 字段（B4 §3.2 真名） | 原版 我 | 原版 敌 | **我们（值 @ 出处）** | 判定 |
|---|---|---|---|---|
| `m_scale` | 0.73 | 0.54 | `DefaultCardScale = 165/(2.0927×108) = 0.7300`（`HandLayout.cs:75`）· 敌 `EnemyCardScale = 0.54f`（`:88`） | ✅ 我方是**算出来的**（与 `CardView.Width=2.0927` 自洽），敌方是字面量 |
| **`m_betweenElementsSpacing`**（⛔ 不叫 `m_spacing`） | 1.45 | 0.60 | `1.45f`（`:119`）· 敌 `0.6f`（`:327`） | ✅ 值同；⚠️ **量纲不同**：我们直接用「我方世界单位」（1 单位 = 108 px，×108 = 156.6 px），原版是「设计单位 × `parentScale 108`」⇒ **折成 px 同为 156.6 / 64.8** ✅ |
| `m_maxLayoutSize` | 0.59 | 0.51 | `0.59f`（`:105`）· 敌 `0.51f`（`:325`） | ✅ |
| `m_maxLayoutSizeSmallScreen` | 0.51 | 0.51 | `0.51f`（`:108`）· 敌同（`:326`） | ✅ |
| `maxLayoutSizeAspectRatioModifier` | (1.70,0)→(2.33,0.1013) | **恒 0** | 我方逐键同（`:112-114`）· 敌建成平线 0（`:339`） | ✅（连切线 `0.160748` 一起） |
| `m_numberOfCardsForMaxHeight` | 12 | 10 | `12`（`:122`）· 敌 `10`（`:328`） | ✅ |
| `m_yAxisCurve` | 5 键（B4 逐键） | 5 键 | 我方 `:126-131` · 敌方 `:341-346`，**time/value/切线逐键与原版相同** | ✅ |
| `m_heightModifierBasedOnTotalCards` | 5 键 | 3 键 | `:138-143` · 敌 `:348-351` | ✅ |
| `m_rotationModifierBasedOnTotalCards` | 4 键 | **恒 1**（直线 y=x） | `:147-151` · 敌 `(0,0)(1,1)`（`:353-355`） | ✅ |
| `m_useRotation` | 1 | 1 | 无开关，恒走 `RotationAt`（`:516-529`） | ✅（两份实例都是 1；⚠️ 选牌/换牌那两份是 **0**，见「顺手发现」5） |
| **`m_invertRotation`** | 0 | **1** | `false`（`:159`）· 敌 `true`（`:330`） | ✅ |
| **`m_verticalOffsetLookTo`** | **−330** | **+74.5** | `45.33`（`:156`）· 敌 `10.23`（`:333`） | ⚠️ **语义一致、数值不同**：原版是**世界单位**（×14.835），我们是自己那把尺（×14.835/108）⇒ 45.33 × 108 = 4896 px = 330 × 14.835 ✅（分母 4896 与 `手牌布局..md` §八之二 实读一致） |
| `m_maxHeight` | 0.70 | **−0.78** | `0.70f`（`:134`）· 敌 `−0.78f`（`:329`） | ✅（敌方负号是「中间往下凹」，`:312-320` 自洽） |
| **`useExtraSpaceOnSelectedCard`** | **1** | **0** | `true`（`:204`）· 敌 `false`（`:331`）；让位量 = 原版 `VarsDevice.extraSpaceOnSelectedCard` 平线 2.0（`:195` + `:389`） | ✅（门闸「**压缩态才让位**」在 `:451-455` `IsCompressed`，2026-10-17 已按 `GetPosition.c:201-214` 订正方向） |
| `Z_OFFSET_BY_POSITION_INDEX` | **0.001** | 0.001 | `zOrderStep = 0.08f`（`:221`） | ➖ **差 80 倍，有意挑的**：`HandLayout.cs:253-275` 逐条论证（原版 0.001 ≈ 0.015 px，**它根本不是分层手段** —— 原版靠嵌套 Canvas 的 `sortingOrder`；我们是世界空间 quad 没 canvas 排序）。🔴 2026-10-12（`A1665`）起分层**已改走 render queue**（`queueTop`），`zOrderStep` 只剩几何纵深 ⇒ **风险已降级** |
| `m_useViewportSize` | 1 | 1 | 无开关，恒走「viewport 那一支」（`:473-478`） | ✅（另一支 `parentScale × m_scale` 我们不做） |
| `allowSelectCardLayoutOverFlow` | 1 | 1 | 恒按 1 处理（`:202-203` 说明） | ✅ |
| `m_inverted` | 0 | 0 | **未实现**（`:161-170`）：13 场 × 4 份 = **52/52 都是 0** | ➖ 例外①（原版数据里触发不了） |
| `renderCamera` | 1559（hudCamera） | 1559 | 我们**读的是 `LayoutSpace.Cam`（正交 HUD 相机）**（`:475`） | ⚠️ 是同一件事的等价物（见表三 item 4） |
| `PlayerHand.smallScreenSizeMultiplier` | **1.365** | **1.2** | **全仓 0 命中**（`SmallScreenUI.Enabled` 有、这个倍数没人用） | ❌ **未实现**（`smallScreenSizeMultiplierBigMobiles` 1.365/1.4 同） |

**幅面口径的两条（不在上表字段里，但决定画面）**

| 项 | 原版 | 我们 | 判定 |
|---|---|---|---|
| 手牌中心行 y | 我方 `HandAnchor` 链 = 961/1080 ⇒ 距底 **119 px**；敌方 `HandAnchor` 挂屏顶 | `baselineY = 0.1204`（= **130 px**，§ 注释自承「取 0.1204 让**卡底**正好压在屏幕下沿上」，`:77-84`）· 敌 `EnemyBaselineY = 0.9102`（`:90-101`，**推导值**：卡顶贴屏幕上沿） | ⚠️ 我方比原版高 **≈11 px**（推导链在注释里，⛔ 不是字段实读） |
| 弧高**代数口径** | `y = 基准 + yAxisCurve(原始值) × maxHeight × hMod` ⇒ **yCurve ∈ [−3.02, −0.0004]，全部 ≤ 0** ⇒ **督军位那一档 = 基准、两端最低** | `arc = ArcHeightFull × Arc01(t) × hMod`（`:414-425/488-496`）—— `Arc01` **归一化到 0..1** ⇒ **两端 = 基准、中间最高** | ⚠️ **形状同、绝对位置差「一整段弧」= 31.35 px**（= 2.113 × 14.835）。已记录（`手牌布局..md` §七 项 4）；`ArcOffsetFromBase`（`:536-541`）留了原版口径给张角用。⏳ 差值能不能被 `baselineY` 完全吸收**本轮没验**（要真机并排） |

---

## 表三 · 相机与挡板

### 3.1 两台相机

| 项 | 原版 `boardCamera`（arena2 = 1560） | **我们 `boardCam`** | 原版 `hudCamera`（1559） | **我们 HUD 相机 `cam`** |
|---|---|---|---|---|
| 投影 | 透视 | ✅ 透视（`ArenaBuilder.cs:3449`） | **透视**（`fov 40`） | ➖ **正交**（`LayoutSpace.cs:52-53`） |
| fov / focal / sensor | 46.397182 / 28 / (41.5, 24) | ✅ 46.397182（+ 逐场 `sensorSize`）/ 28 / 逐场 | **40** / 50 / (36, 24) | ❌ **未设**（正交不吃 fov；focal/sensor 也未设） |
| gateFit | 2 (Horizontal) | ✅ `Camera.GateFitMode.Horizontal`（`ArenaBuilder.cs:3457`） | 2 | 未设（默认 Vertical） |
| lensShift | **(0, −0.205)** 序列化，运行时由 `CalculateFraming` 重算 | ✅ `lensShift = (0, LensShiftY(aspect, zoom))`（`ArenaBuilder.cs:3463` + `CombatCameraZoom.cs:840-844`），初值 = 烘场景那一档（`CombatAutoZoom.cs:293-295`） | (0, 0) | 未设（正交下无效） |
| near / far | 0.3 / 300 | ✅ | 0.3 / 300 | 0.01 / 200（`LayoutSpace.cs:56-57`） |
| clearFlags / depth | 2 (SolidColor) / **−1** | ✅ / ✅（`BattleScene.cs:22719`） | **4 (Nothing)** / 0 | SolidColor / 0（**URP base+overlay**：HUD 是 **Overlay**（`BattleScene.cs:22797-22802`）—— 与「Nothing + depth 0」是同一意图的不同载体） |
| cullingMask | **1137**（bit 0/4/5/6/10） | ➖ `1 << 8`（只画 3D 战场那一层，`BattleScene.cs:22720`） | **32**（layer 5 = UI） | ➖ 全层**去掉** 8（`BattleScene.cs:22797`） |
| HDR | true | 未设（吃默认 true） | false | 未设 |
| postFX / 阴影 / volume 层 | `RenderPostProcessing 1` · `m_RenderShadows 1` · `VolumeLayerMask {1}` | ✅ 三项都设（`ArenaBuilder.cs:3472-3475`） | 无 | — |
| 位置 | `(100, 2.222075, −13.57198)`、朝向单位四元数（朝 +Z） | ✅ 同值 **减 `ArenaOriginX`**（`BattleScene.cs:22725-22728`） | canvas `planeDistance 100` | 相机 `(0,0,−20)`（`LayoutSpace.cs:54`） |

### 3.2 挡板（原版 `CardLowBoardLimit`）

| 项 | 原版 | 我们 | 判定 |
|---|---|---|---|
| 实体 | `BattleBoardElements/Colliders/CardBoardLimits/CardLowBoardLimit`：`BoxCollider` **size (1,1,1) · center (0,0,0) · IsTrigger false · layer 11**，Transform 缩放 **81.16108 × 0.8291501 × 41.907936**（局部 `(0, 0.5, −13.14)`，父 `(0, −1.09, 16.8)`）⇒ 换算到 `Colliders` 原点：中心 `(0, −0.59, 3.66)`、**顶面 y = −0.1754**、x ∈ ±40.58、z ∈ [−17.29, 24.61] | **没有对应物**：全仓 **无 `BoxCollider` 挡板**、**无 layer 11**（grep `1 << 11` / `layer = 11` = 0 命中） | ❌ **缺** |
| 「出牌边界」怎么实现 | 拖拽射线**打到那块 Box** ⇒ 卡被物理挡住 | ① `BoardLayout.lineY`（我 **0.3444** / 敌 **0.5685**，`BattleScene.cs:121-122`）+ `snapToleranceY 0.09`（`BoardLayout.cs:76/465`）；② 3D 时落点判据 = 9 格卡心**投影**取最近、容差 = 两行投影间距的一半（`ArenaSlots.cs:147-169`）；③ 拖拽**每帧不钳制**（`t.position = Lerp(t.position, 指针+抓取偏移, …)`，`CardInteraction.cs:245-252`）—— **松手才判合法性、不合法就回弹** | ➖ **口径不同**（原版是「拖到那儿就被挡住」，我们是「松手判不合法则回弹」） |
| 那条 y 线还被谁用 | 原版那条 y 判据守的是「**要不要试着把手牌打到场上**」（`BattleManager__Update.c:195/249/291`，比较量取**指针**） | 我们也用它当**手牌让位**的守卫（`CardInteraction.cs:285`，量已按 10-17 改成**指针** ✅） | ⚠️ 我们的守卫**本身在原版没有对应物**（`CardInteraction.cs:277-284` 自己标了），但阈值那一格**换算没验过** ⇒ 本次补验见「顺手发现」2 |

### 3.3 两张 aspect 曲线（简报第 5 项）

| 曲线 | 原版逐键（B4 §1.4） | 我们 | 判定 |
|---|---|---|---|
| `CameraVerticalFramer.verticalPaddingModifierByAspectRatio` | 1.0 / 1.0 / 1.0 / 0.6518510580062866 / 0.6508162021636963（keyTime 1.333/1.6/1.77/2.333/2.44） | `CombatAutoZoom.cs:608-614` `PadModByAspect` **逐键 + 四条切线**（含 `−0.6183817982673645` 那个**曾经写反号**的） | ✅ |
| `CombatCameraZoom.maxZoomForSmallScreenDevicesByAspectRatio` | 0.5739306211471558 / 0.5352425575256348 / 0.3050706088542938 / 0.3084440529346466 / 0.3074091374874115 | `CombatCameraZoom.cs:176` + `CombatAutoZoom.cs:496-504`（`OriginalMaxZoomByAspect`）· 消费点 `CombatCameraZoom.cs:534-540`（`SmallScreenUI.Enabled` 才夹） | ✅ |
| 同族的另三条 | `cameraSizeXTable`(6 键) · `viewShiftModifier`(7 键) · `verticalPaddingByZoom`(2 键) · `maxVerticalSizeInViewPort = 7.0` | `CombatAutoZoom.cs:587-603 / 657-675` ✅ **逐键 + 切线**（2026-10-12 `A444` 把当年的抄错整批换成了原版字面量） | ✅ |
| `CombatAutoZoom.unitsZoomCurve` | (0,0)(1,0)(2,0)(3,0.1052578)(4,1) | `CombatAutoZoom.cs:481-489` | ✅ |

⇒ **简报第 5 项的答案：两张曲线我们都有对应实现，且逐键（含切线）相同**；`CombatAutoZoom` 那族**正是**在扮演它们（`CombatAutoZoom` = 消费者 + 取景器，`CombatCameraZoom` = 原版那件 zoom 组件，2026-10-12 `A421/A422/A423` 接齐）。

---

## 不一致清单（按严重度；真错在前）

| # | 项 | 原版值（出处） | 我们值（出处） | 判定与影响 |
|---|---|---|---|---|
| 1 | **挡板 `CardLowBoardLimit` 完全没有对应物** | BoxCollider · layer **11** · 顶面 y = −0.1754（B4 §2.1） | 无 collider / 无 layer 11 / 拖拽不钳制（`CardInteraction.cs:245-252`） | ❌ **真缺**（铁律 11 ⇒ 要做）。影响：原版「拖到棋盘下沿就被挡住」，我们**能拖到任意位置**、只在松手时回弹 —— 手感/可达范围不同 |
| 2 | **`origPosReduction` / `AdjustMinionBoardPos` 未实现** | 我 (0.66, 2.75) · 敌 (0.65, 0.275)（B4 §2.4(d)） | 全仓 0 命中；只实现裸 x | ⏳ **路径待判**；若在落点路上则我方 x **偏大 51%**。**先读 `AdjustMinionBoardPos` 的调用点**（廉价），判完再决定做不做 |
| 3 | **敌方槽位符号（镜像 or 不镜像）** | 序列化 left=+x ↔ 运行时重建 left=−x（B4 §2.4 a vs b，**自相矛盾**） | 镜像（`ArenaSlots.cs:109` · `BoardLayout.mirror`） | ⏳ **待判**（两条判据都硬）。影响**语义**（哪个物理格被点名）⇒ 联机/效果会放大。**结案法 = 原版实况 dump 一个敌方 left 槽的 x 符号** |
| 4 | **2D 槽位缺 `minionExtraDistanceFromHero` 偏移** | `(0.82k + 0.09) × 182.14` ⇒ 每格 +16.4 px（B4 §2.4(b)） | `(slot−4) × 149.3 px`（`BoardLayout.cs:64`） | ❌ 真差（16.5 px/格）。**影响面**：仅无 3D 资产的兜底路（`use3D` 时走 3D 投影） |
| 5 | **`PlayerHand.smallScreenSizeMultiplier` 未实现** | 1.365（我）/ 1.2（敌）· BigMobiles 1.365/1.4（B4 §3.3） | 0 命中（只有 `SmallScreenUI.Enabled`） | ❌ 缺（小屏档手牌尺寸会小一档）。⏳ 它乘在「卡尺寸」还是「布局」未核 |
| 6 | **HUD 相机是正交**（简报第 4 项） | `hudCamera` **透视** fov 40 / 平面 100 / focal 50 / sensor 36×24（B4 §1.1） | `LayoutSpace.Apply` 写死正交、可见高恒 10 单位（`LayoutSpace.cs:49-58/80`） | ➖ **有意挑的**（`LayoutSpace.cs:1-18` 明确写了为什么不做 uGUI/CanvasScaler；`战斗复刻_正本.md` §三/§3·1c 记了 2026-10-15 试改 Expand **当天回滚**）。**后果**：① HUD 没有透视（无近大远小）；② 手牌 `cap` 走「可见宽 = 10×aspect」而不是原版的 `2·dist·tan(fov/2)·aspect`（16:9 下等价，非 16:9 不等价）；③ **画布模型不同 ⇒ 非 16:9 下的相对尺寸不同**（正本 §3·1b：4:3 下 = 原版 ÷ 1.333 尺度）。**手牌这一格本轮算过（⛔ 别照旧结论猜）**：`LayoutSpace.Scale` 那一乘恰好把间距也缩了 ⇒ · **宽 ≤ 1920（4:3 / 16:10）两者逐值相同**（4:3：间距都是 117.4 px、`cap` 都是 867.7 px ⇒ **压缩阈值都是 n ≥ 8** ✅，`HandLayout.cs:449` 那个数在 4:3 下照样成立）；· **宽 > 1920（21:9）时分叉**：我们被 `Scale = min(1, ·)` **钉在 156.6 px**，而原版参考宽**恒 1920 单位**、屏幕越宽元素越大（2560 宽 → 208.8 px）⇒ `cap` 同为 1542.7 px 时 **阈值 = n ≥ 10（我们）vs n ≥ 8（原版）**；· 5:4 那类**比 16:9 高**的屏：我们按**宽高比**缩（0.703）、原版按**绝对宽**缩（0.667）⇒ 我们约大 5%。⚠️ 这三条是**推算**（`HandLayout.cs:435-455/473-478` + `LayoutSpace.cs:80-98` × 原版 `CanvasScaler{match=0, ref 1920×1080}` + 正本 §3·1b），**没跑真机**。**落地 = 账 `A2098`**（换 uGUI 锚点体系），⛔ 不是调相机 |
| 7 | **手牌弧高的代数口径**（多抬「一整段弧」） | y = 基准 + `yAxisCurve(原始值)`×0.70 ⇒ 督军位在基准、两端 −31.35 px | `Arc01` 归一化 ⇒ 两端在基准、中间 +31.35 px（`HandLayout.cs:414-425`） | ⚠️ 已记录（`手牌布局..md` §七 项 4）。⏳ 「`baselineY` 能不能完全吸收」**没验**（要真机并排） |
| 8 | **手牌中心行 y** | `HandAnchor` 链 = 961/1080 ⇒ 距底 **119 px** | `baselineY = 0.1204` = **130 px**（`HandLayout.cs:84`） | ⚠️ 差 **≈11 px**（且注释自承是为「卡底贴屏幕下沿」而抬的）—— 推导 vs 推导，非字段对字段 |
| 9 | **2D 敌行用了我方步进/尺寸** | 敌行步距 131.9 px、卡 124.5 px（B4 §2.4 + `ArenaSlots.cs:27-29`） | 149.3 px / 同我方尺寸 | ➖ **有意挑的**（`BoardLayout.cs:33-34` 自标「2D 里不适用」）。3D 那一路是对的 |
| 10 | **`zOrderStep` 0.08 vs `Z_OFFSET` 0.001** | 0.001（`..cctor` `0x3a83126f`） | 0.08（`HandLayout.cs:221`） | ➖ 有意挑的（差 80 倍，理由逐条在 `:253-275`）；分层已改走 render queue ⇒ 风险降级 |
| 11 | `m_inverted` 未实现 | 0（52/52 实例） | 未实现 | ➖ 例外①（原版数据触发不了） |
| 12 | `boardCam.cullingMask` / `hudCam.cullingMask` 与 `clearFlags` | 1137 / 32 / 2 & 4 | `1<<8` / 全层减 8 / SolidColor & Overlay | ➖ 载体不同（URP base+overlay + 我们只有两层）；**层数少的根因是我们自建了 `ArenaLayer 8`**，不是漏 |

---

## 顺手发现

1. 🔴 **`boardCamera.sensorSize` 是「逐场」的 —— B4 只给了 arena2 的 41.5**：`ArenaBuilder.cs:3452-3456` 记着「**三场是 37.2、十场是 41.5**，写死会把那三场等比放大 10.36%」。⇒ **拿 B4 当尺子逐场验收时，先确认是哪一场**（B4 的 arena2 = 41.5 那一档）。
2. ✅ **补上了「`HandLimitArea` 到底在哪一根线上」这个悬案**（`CardInteraction.cs:283-284` 自承「换算没验过」）：
   · 原版实况 dump `runtime_ui_dump_Battle_Arena_1.tsv:332` —— `…/Safe area BackCanvas/HandLimitArea`：`RT` · pos **`(0.0, 300.0)`** · size `1×1` · anchorMin = anchorMax = pivot = **`(0.5, 0)`**（锚在**底边**、往上是 +y）⇒ **底边上方 300 单位**。
   · 同一份 dump 的根 `Canvas`（`:95`）sizeDelta = **1920×1080** ⇒ **这 300 就是 1920×1080 参考空间里的设计 px**；而且 `1080 − 300 = 780` ≈ 旧记录里的 `HandLimitArea [-0, 779 1x1]` ⇒ **两处读数其实同一根线**（779 是「距顶」，300 是「距底」）。
   · 我们 `BoardLayout.lineY`(我方) = **0.3444 = 372 px** ⇒ **比原版那条线高 72 px**。
   · ⚠️ 这条**不是「改 0.3444」的理由**：`lineY` 同时是 2D 棋盘行心（用的是 `battle.gd` 时代 708/1080 的旧校准，`ArenaSlots.cs:37` 已自承它「**不是原版字段**」）。⇒ 正解是**把两个用途拆开**（棋盘行心 vs 手牌线），⛔ 不是挪一个数。
3. 🔴 **我们的 2D 棋盘行心（`PlayerLineY = 0.3444` = 距底 372 px）与 3D 实测行位（我方 **58.9% = 636 px**）差 264 px**：`ArenaSlots.cs:31-40` 记的是「实测我方行差 ≈150 px」并已把命中判定改走透视投影 ⇒ **2D 那份行心今天只剩兜底与 `_band`**；但**两套值并存本身是隐患**（正本 §三 记的是同一族病）。⇒ 建议记一条账：**把 `lineY` 的「棋盘行心」用途也收进 3D 投影**（判据已现成）。
4. ⚠️ **旧文档 `资料/对战排版_原版数值与改造方案.md:172-190` 已过期**：它用的是**序列化**的 `leftSlotPosNormal = ±3/±6/±9`（间距 3.0）与「敌方镜像」，而 B4 §2.4 已定「序列化值 ≠ 运行时值、那套是死值」。**别拿它当尺子**（铁律 5：建议就地加一行更正）。
5. ⏳ **选牌 / 换牌两份 `CardsHorizontalLayout` 我们没核**：原版 `m_useRotation = 0` · `m_yAxisCurve` 平线 0（**没有弧**）· `m_scale 183.41` · `m_betweenElementsSpacing 502.2`（B4 §3.2 + `手牌布局..md` §五）。我们的换牌面板（`Battle/MulliganPanel.cs` 用了 `HandLayout.Place`）**是否套了手牌的弧+张角** —— 本轮**没核**（不在简报清单内），⚠️ 看起来是条真差异，建议另开一格查。
6. ℹ️ `BattleBoardElements` 的两对 `Left/RightMinionArea` **`activeSelf = false`**（B4 §2.2）—— 它们**不是**左右半场的容器（左右半场是两条 `List`）⇒ 我们没建它们是对的。

## 没查清的（⛔ 不猜）

1. **原版敌方槽位的运行时符号**（表一 §1.3-2）—— 两条判据打架，只有实况能结。
2. **`origPosReduction` 在不在落点路径上**（表一 §1.3-5）—— 差 `AdjustMinionBoardPos` 的调用点。
3. **`smallScreenSizeMultiplier`（1.365/1.2）乘在哪**（表二末行）—— 是卡尺寸还是布局，没读它的消费点。
4. **`aspectRatioModifier` 曲线在 aspect < 1.70 时的取值**（外推给负值，还是夹到 0）—— ⚠️ **两端套的是同一条曲线**（我方 `HandLayout.cs:112-114` ↔ 原版 `maxLayoutSizeAspectRatioModifier`）⇒ **不影响对账结论**，只影响绝对值；4:3 档要不要夹**没核**（本轮只按 16:9 的 +0.0126 复算过 n ≥ 8）。
5. **手牌 `baselineY` 的 11 px 与弧高口径的 31 px 是否互相抵消**（要真机并排才能定）—— 本轮只做静态对账，**没跑实况**。
6. **`HandLimitArea` 那条线在原版守的是「要不要打到场上」**，我们拿它守「手牌让位」⇒ 阈值该不该用 300 那根线，**属设计口径、不是复刻判据**，本轮不裁。
