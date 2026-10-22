# E-C · 换牌期 8 条 `BattleHud*` 动画 clip —— 解数据 + 接线方案（2026-10-22）

> 执行代理 E-C。**只读**解包 + 生成器；⛔ 未跑 Unity、未改任何运行时 `.cs` / 现有 JSON / 正本。
> 交付物：
>
> - `工具/gen_battlehud_clips.py`（生成器，`--check` 幂等）
> - `数据/游戏数据/battlehud_clips.json`（83,626 B · md5 `39d8f7cfba810d82f495aa55937067b7` · `clipsHash 9a4e484c3dba918b4c513da4e55fe125`）
>
> 复跑：`D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/gen_battlehud_clips.py`
> 核对：同命令加 `--check`（只比对、不写盘；已实测 `✅ 产物与现生成一致`，连跑两遍 md5 相同）。

---

## 一、8 条各是什么（逐条实读）

**结论：8 条全是 legacy 曲线。** 逐条 `m_Legacy=True` · `m_MuscleClipSize=0` · `m_Compressed=False`；
`m_CompressedRotationCurves / m_RotationCurves / m_ScaleCurves / m_PPtrCurves / m_Events` **全空**。
⇒ **曲线本地全有**，运行期可用 `AnimationClip.SetCurve` 重建（同 `Battle/CameraIntro.cs` 那条路），
不需要「运行时取原件」那条退路。

**唯一副本**：`d:/2/新解包资源/assets_full/bundle_battlesharedresources_assets_all/AnimationClip/`。
全库 **`AnimationClip/` 目录**里 grep `"m_Name": "BattleHud` 只命中这 8 条 clip
（另外 3 条同包 clip 是 `Battle Arena 3 Animations` / `SporeArmature|LeviathanBigSpore` / `Recorded`，
与本条线无关；13 个 `battlearena*` 包各自的 `AnimationClip/` 里**没有** BattleHud clip，见 §二）。
13 场 `battlearena*` 的 `BattleHud` 上各一枚 `Animation` 组件，**全部引用这同一套 pid**（见 §二）。

### 1·1 汇总表

| clip | pid | `ToggleWithAnimation` 分支 `(option,useAnimation,animHand)` | 时长 | 曲线 | 键 | 动的节点 |
|---|---|---|---|---|---|---|
| `BattleHudHidden` | −1315085872149960500 | (0, 0, 0) | 0.0 s | 16 | 16 | 9（全部） |
| `BattleHudHiddenNoHand` | 4247708151348801114 | (0, 0, 1) | 0.0 s | 14 | 14 | 7（无 HandArea / EnemyArea） |
| `BattleHudHide` | 1254995653877182794 | (0, 1, 0) | 1.0 s | 16 | 29 | 9（全部） |
| `BattleHudHideNoHand` | 253699984150460926 | (0, 1, 1) | 1.0 s | 14 | 26 | 7（无 HandArea / EnemyArea） |
| `BattleHudShow` | −6573478101177810173 | (1, 1, 0) | 1.0 s | 16 | 30 | 9（全部） |
| `BattleHudShowNoHand` | 2365053043493243489 | (1, 1, 1) | 1.0 s | 14 | 26 | 7（无 HandArea / EnemyArea） |
| `BattleHudShowInstant` | 3477083935848071261 | (1, 0, 0) | 0.0 s | 14 | 14 | 8（有 EnemyArea、无 HandArea / LeftAnchor.y） |
| `BattleHudShowInstantNoHand` | −997326231047120692 | (1, 0, 1) | 0.0 s | 15 | 15 | 8（有 EnemyArea、无 HandArea） |

分支判据（第一权威）：`d:/2/tools/decomp_full/BattleHud__ToggleWithAnimation.c:19-49` ——
`param_3 = useAnimation`（0 ⇒ 4 条 Instant / 1 ⇒ 4 条带 1 s 补间）、
`param_4 = animHand`（1 ⇒ `NoHand` 变体）、`param_2 = option`（0 = hide / 1 = show）。
三个调用点：`BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:113` = `(1,1,0)` ⇒ `BattleHudShow`；
`BattleManager._CloseBattleDoors_d__393__MoveNext.c:29` = `(0,1,1)` ⇒ `BattleHudHideNoHand`；
`BattleManager._TutorialStartSequence_d__592__MoveNext.c:283` = `(1,1,1)` ⇒ `BattleHudShowNoHand`。
⚠️ **常量 → 名字那一跳（`BattleHud` 的 `.cctor`）本地方法体缺失** ⇒ 上表的分支↔名字对应是
「分支结构 + 两个已知调用点 + Instant/补间语义」推出来的，**没有**从二进制里读出字符串（见 §四·1）。

### 1·2 9 个落点：隐藏值 → 显示值（逐键实读，单位 = 原版画布 px @1920×1080）

| 节点 | 属性 | 隐藏值 | 显示值 | 位移 |
|---|---|---|---|---|
| `LeftArea/PlayerInfo` | `m_AnchoredPosition.x` | −240 | **50** | +290 |
| `LeftArea/PlayerInfo` | `m_AnchoredPosition.y` | −330 | 28 | +358 |
| `LeftArea/EnemyInfo` | `m_AnchoredPosition.x` | −240 | 50 | +290 |
| `LeftArea/EnemyInfo` | `m_AnchoredPosition.y` | 330 | −28 | −358 |
| `RightArea/SettingsBtn` | `m_AnchoredPosition.x` | 310 | **−54** | −364 |
| `RightArea/SettingsBtn` | `m_AnchoredPosition.y` | 311 | −9.19995 | −320.2 |
| `RightArea/PlayerDeck` | `m_AnchoredPosition.x` | −190 | −190 | 0 |
| `RightArea/PlayerDeck` | `m_AnchoredPosition.y` | −461 | 0 | +461 |
| `RightArea/EnemyDeck` | `m_AnchoredPosition.x` | **−190（Hidden）/ −217.7（Hide/Show）** | −217.7 | 见 §二·3 |
| `RightArea/EnemyDeck` | `m_AnchoredPosition.y` | 461 | 0 | −461 |
| `UpperAnchor/EnemyArea` | `m_AnchoredPosition.y` | **402.82998657…** | 0 | −402.83 |
| `RightArea/Right Anchor` | `m_AnchoredPosition.x` | 476 | 0 | −476 |
| `RightArea/Right Anchor` | `m_AnchoredPosition.y` | 0 | 0 | 0 |
| `LeftArea/Left Anchor` | `m_AnchoredPosition.x` | −449 | 0 | +449 |
| `LeftArea/Left Anchor` | `m_AnchoredPosition.y` | 0 | 0 | 0 |
| `BottomAnchor/PlayerArea/HandArea` | **`m_LocalPosition`（纯 `Transform`）** | (0, **−479**, 0) | (0, 0, 0) | +479 |

- 除 `HandArea` 外**全部是 `RectTransform`**（`classID = 224`、`m_AnchoredPosition.x/y`）；
  `HandArea` 是**纯 `Transform`**（`m_LocalPosition` 三轴）⇒ 运行期绑定时类型要用 `Transform`，不是 `RectTransform`。
- 8 条 clip 的**所有关键帧 `inSlope = outSlope = 0`、`weightedMode = 0`（非加权）** ⇒
  两条键之间是 Unity 的 **smoothstep（ease-in/ease-out）**，**不是线性**。
  JSON 逐键保留 `is`/`os`；`new Keyframe(t,v)`（切线 0）与它逐位等价 —— ⛔ 别改成线性补间。
- 全部曲线的 `m_PreInfinity = m_PostInfinity = 2`（**Clamp**）⇒ 1 s clip 结束后停在末键；
  0 s 的 Instant clip 从第 0 帧起就是那个常量。

---

## 二、13 场异同（逐场现读）

**结论：13 场是【同一套】。** 依据三条（互相独立）：

> ⚠️ 先说清一件事：这 8 条 clip **本地只有一份**（在 `battlesharedresources_assets_all` 里），
> 13 场是**引用**它、不是各自存一份拷贝 ⇒ 「逐场读关键帧」在物理上退化为「逐场核引用」：
> 只要某一场指向了别的 pid/CAB 就会不同。逐场核过 —— **13 场没有任何一场不同**（下表）。
> 真正的「逐键比」在副本层面不适用；逐键比做在**同一份 clip 的 8 条之间**（见 §一·2 与 `differences`）。

1. **8 个 `m_PathID` 逐一相同、顺序相同**（每场 `Animation.m_Animations` 都是
   `Hidden, Hide, Show, ShowInstant, HiddenNoHand, HideNoHand, ShowNoHand, ShowInstantNoHand`）。
2. **都解析到同一个外部 CAB**：`CAB-68cc81d35f88d5ad277f2717d6e1ac25`（= `battlesharedresources_assets_all` 那一份）。
   各场 `m_FileID` 数值不同（2 / 3 / 4）只是**每个包自己的 external 表下标不同**，不是不同资产。
3. **每场 `m_Animation`（默认 clip）都是 `BattleHudHiddenNoHand`**、`m_PlayAutomatically = True`（13/13）。

| 场 | `Animation` 文件 | Animation pid | GameObject pid | `m_FileID` | 默认 clip | clip hash |
|---|---|---|---|---|---|---|
| battlearena1 | Animation_1606 | 1606 | 239 | 4 | HiddenNoHand | 9a4e484c… |
| battlearena2 | Animation_1774 | 1774 | 230 | 4 | HiddenNoHand | 9a4e484c… |
| battlearena3 | Animation_1701 | 1701 | 247 | 3 | HiddenNoHand | 9a4e484c… |
| battlearenaaeldari | Animation_1593 | 1593 | 234 | 2 | HiddenNoHand | 9a4e484c… |
| battlearenaastramilitarum | Animation_1786 | 1786 | 255 | 4 | HiddenNoHand | 9a4e484c… |
| battlearenablacklegion | Animation_1675 | 1675 | 226 | 4 | HiddenNoHand | 9a4e484c… |
| battlearenadarkangels | Animation_1663 | 1663 | 237 | 4 | HiddenNoHand | 9a4e484c… |
| battlearenaemperorschildren | Animation_1709 | 1709 | 234 | 4 | HiddenNoHand | 9a4e484c… |
| battlearenagenestealers | Animation_1627 | 1627 | 240 | 4 | HiddenNoHand | 9a4e484c… |
| battlearenaleviathan | Animation_1751 | 1751 | 251 | 3 | HiddenNoHand | 9a4e484c… |
| battlearenasororitas | Animation_1800 | 1800 | 269 | 2 | HiddenNoHand | 9a4e484c… |
| battlearenaspacewolves | Animation_1638 | 1638 | 239 | 3 | HiddenNoHand | 9a4e484c… |
| battlearenatauviorla | Animation_1939 | 1939 | 281 | 4 | HiddenNoHand | 9a4e484c… |

（逐场完整 pid 列表与 hash 见 JSON `perArena`；13 场的 `clipPids` / `clipNames` / `hash` 全等。）

### 2·1 场景里那 9 个节点的**静态 rest 值**（13 场逐场读，UnityPy 直读 `scenes_scenes_battlearena*.bundle`）

13 场**完全一致**（只有 float32 末位噪声）：

| 节点 | 13 场 rest |
|---|---|
| `LeftArea/PlayerInfo` | **(31.7002, 28.0)** |
| `LeftArea/EnemyInfo` | (50.0, −28.0) |
| `RightArea/SettingsBtn` | **(−48.1001, −9.2000)**（3 场写 −48.1，差 1e−4） |
| `RightArea/PlayerDeck` | (−190.0, 0.0) |
| `RightArea/EnemyDeck` | (−217.7002, 0.0) |
| `UpperAnchor/EnemyArea` | (0.0, 0.0) |
| `RightArea/Right Anchor` | (0.0, 0.0) |
| `LeftArea/Left Anchor` | (−0.0001, 0.0)（3 场写 0.0） |
| `BottomAnchor/PlayerArea/HandArea` | `m_LocalPosition` (0, 0, 0) |

### 2·2 🔴 rest ≠ clip 显示终值的两处（**我们 HUD 的一处真偏离，本批未修**）

| 节点 | 场景 rest | `BattleHudShow` 末键 | 差 |
|---|---|---|---|
| `LeftArea/PlayerInfo` x | 31.7002 | **50** | **+18.2998 px** |
| `RightArea/SettingsBtn` x | −48.1001 | **−54** | **−5.8999 px** |
| 其余 7 个节点的 x/y | — | — | 逐位一致（≤1e−4） |

**为什么这不是「clip 写错了」**：原版 `m_PlayAutomatically = True` + 默认 clip `BattleHudHiddenNoHand`
⇒ **场景一加载就播 HiddenNoHand**，那 9 个节点的 `m_AnchoredPosition` 立刻被 clip 覆盖；
换牌结束再播 `BattleHudShow`，末键停在 (50, 28) / (−54, −9.19995)。
⇒ **原版运行时的「显示态」= clip 末键，不是 prefab 的序列化 rest**（rest 只在 clip 跑之前存在一瞬）。
运行期 dump 两条（`runtime_ui_dump_drive_0912.tsv` / `runtime_ui_dump_Battle_Arena_1.tsv`）都是在**换牌期**抓的，
里面 `PlayerInfo = (−240, −330)`、`Left Anchor = (−449, 0)`、`SettingsBtn = (310, 311)` —— **正是 clip 的隐藏值**，
反证了「clip 覆盖 rest」这件事。
⇒ **我们按 prefab rest 摆出来的 HUD，在显示态比原版偏右 18.3 px（玩家名牌）/ 5.9 px（设置钮）**。
这一条按铁律 5·b / 11 **记在这里**（本轮不改代码）；修法 = 把这两个件的基准 x 改成 clip 末键（或直接由动画驱动，不再用 rest）。

### 2·3 `NoHand` 规则的边界（原版素材自带的不对称，生成器已写进 `differences`）

| id | 现象 |
|---|---|
| `enemy_deck_x_hidden_value_split` | `Hidden`/`HiddenNoHand` 的隐藏 x = **−190**；`Hide`/`Show`/`ShowInstant*` 用 **−217.7**。场景 rest = −217.7 ⇒ −190 只出现在两条 Instant 的隐藏档。 |
| `enemy_deck_x_single_key_in_animated` | `Show`/`ShowNoHand` 的 EnemyDeck.x 只有 **t=1.0 一个键**（开局瞬跳）；`Hide`/`HideNoHand` 只有 **t=0.0 一个键**。这条 x **没有补间**。 |
| `hide_nohand_settingsbtn_start_split` | `HideNoHand` 的 SettingsBtn 起点 = **(−6.90002, −4.79999)**；`Hide` 起点 = (−54, −9.19995)。 |
| `left_anchor_y_17_only_in_showinstantnohand` | 只有 `ShowInstantNoHand` 的 LeftAnchor.y = **17.0**；其余 7 条都是 0.0（`ShowInstant` 没有这条曲线）。场景 rest y = 0.0。 |
| `left_anchor_y_missing_in_showinstant` | `ShowInstant` 只有 14 条 float 曲线 —— 唯独缺 `Left Anchor.y`。 |
| `nohand_enemyarea_presence` | `Hidden/Hide/Show` 的 NoHand 变体都**去掉** `EnemyArea.y`；但 **`ShowInstantNoHand` 保留了它** ⇒ 「NoHand = 只去掉 HandArea」在 EnemyArea 上 3/4 条按、1 条不按。 |
| `show_playerdeck_x_constant` | `Show` 的 PlayerDeck.x 两个键都是 −190（空操作）。 |
| `single_key_curves` | 带 1 s 补间的 4 条里也有零星单键曲线（见 JSON），它们在整段内是常量、不是补间。 |

---

## 三、怎么接（**只写方案，不改代码**）

### 3·0 先讲清一条：**必须用「增量」，不能直接写 clip 的绝对值**

clip 里的 `m_AnchoredPosition` 是**原版那棵树里「组节点」的绝对坐标**；
我们的 HUD 是**摊平**的（`_hudLayer` 直子 + 归一化锚点，`HudCanvas` 一个单位 = 一个设计 px，
参考分辨率 1920×1080，见 `Battle/HudCanvas.cs:57`），每个件的原点是**画布**、不是原版那个组节点
（例：`_myPlate` 的锚点是**左缘** 26.7561 px，见 `Battle/BattleDriver.cs:20846` 的 doc；
它对应的是原版 `PlayerInfo/PlayerName/NameBackground` 的**画心**，不是 `PlayerInfo` 节点本身）。
⇒ 直接把 clip 的绝对值写给我们的件，会把整块 HUD 搬到原版那棵树的位置上（整体错位）。
**做法**：对每条曲线算 **`delta(t) = key(t) − reference.shown`**（JSON 新增的 `reference` 块已把
每个 `(node, property)` 的 `hidden` / `shown` 及来源 clip 都列好），把 `delta` 施加到 rig 节点。

### 3·1 数据落点 / 重建 clip

1. **把 JSON 放进 Resources**：`MyGame/Assets/CardPresentation/Resources/battlehud_clips.json`
   （生成器按简报只写 `数据/游戏数据/` 那一份；接线那一批要么拷一份，要么把生成器扩成两处写 —— 后者是
   `工具/gen_camera_intro_clips.py:69-70` 的既有做法）。运行期加载照 `Battle/CameraIntro.cs:121,180-190`
   （`Resources.Load<TextAsset>` + `JsonUtility.FromJson`）。
2. **重建 clip 用 `SetCurve`**（判据/先例 = `Battle/CameraIntro.cs:387-444`）：
   - `RectTransform` 曲线：`clip.SetCurve(node, typeof(RectTransform), "m_AnchoredPosition.x", curve)`；
   - `HandArea` 的 `m_LocalPosition`：`clip.SetCurve(node, typeof(Transform), "m_LocalPosition.x/y/z", curve)`；
   - 逐键写回 `new Keyframe(t, v, inTangent, outTangent)`（`CameraIntro.cs:430-435` 的写法）。
   - 采样照 `CameraIntro.cs:367`（`clip.SampleAnimation(go, t)`）—— 我们的驱动是 `Update` 泵，**不要依赖
     `Animation.Play` 自己跑**（批处理下没有帧循环，且补间时序要可断言）。

### 3·2 rig 节点（几何零位移的加法）

在 `_hudLayer`（`BattleDriver.cs:20703-20733` 建的那一层）下建 **9 个全屏 stretch `RectTransform`**
（`anchorMin=0,0` / `anchorMax=1,1` / `sizeDelta=0` / `anchoredPosition=0` / `pivot=0.5`），
名字就用 JSON 的 `node`；把现有件**搬进去**。因为 rig 是全屏 + 单位变换，**搬家本身一个像素都不动**；
之后给 rig 的 `anchoredPosition` 写 `delta` 就等价于原版那个组节点的位移。

| rig（JSON `node`） | 现有件（`BattleDriver.cs` 行号） |
|---|---|
| `LeftArea/PlayerInfo` | `_myPlate`(20846) · `_myText`(20854) · 里程碑/语音钮(20857+) |
| `LeftArea/EnemyInfo` | `_enemyPlate`(20828) · `_enemyText`(20841) · `_cemeteryBtn`(997) |
| `RightArea/SettingsBtn` | `_settingsBtn`(21424) |
| `RightArea/PlayerDeck` | `_myDeckPlate`(21200) · `_myPile`(21222) · `_myDeckSizePlate`(1507) · `_myDeckLight`(1506) |
| `RightArea/EnemyDeck` | `_foeDeckPlate`(21204) · `_foePile`(21224) · `_foeDeckSizePlate` · `_foeDeckLight` |
| `RightArea/Right Anchor` | `_endTurnBg`(21129) + 能量那一竖排（常量/件见 `1687-1770`、`_myEnergyPlate`(1499)） |
| `LeftArea/Left Anchor` | `_offensiveBtn`(22246) · `_cameraResetBtn`(22212) |
| `UpperAnchor/EnemyArea` | **不挂件**：敌方手牌是世界空间 quad，改驱动 `foeHand.baselineY`（见 3·3） |
| `BottomAnchor/PlayerArea/HandArea` | **不挂件**：手牌由 `HandLayout` 世界定位，改驱动 `HandLayout.baselineY`（见 3·3） |

> 原版 `Right Anchor` 里装的是 `Energy And turn holder`（结束回合 + 双方能量），
> `Left Anchor` 里装的是 `CenterCameraButton` / `OffensiveButton` —— 与我们上表的对应物一致。

### 3·3 两个「不是 uGUI 件」的落点

- **我方手牌**：`Hand/HandLayout.cs:236` 的 `baselineY`，`SlotPosition` 在 `:586-591`
  用 `LayoutSpace.ToWorld(x, baselineY + arc)`。⇒ 给 `HandLayout` 加一个 `handAreaOffsetY`
  （归一化，默认 0），在 `baselineY + arc` 上叠加；`HandArea` 的 Δ = **+479 px = 479/1080**。
  我方与敌方各一份 `HandLayout`（`BattleDriver.cs:38` / `:20102`）。
- **敌方手牌排**：`SyncFoeHand` 在 `BattleDriver.cs:20191-20206` 写 `foeHand.SlotPosition(i,n)`；
  同一份 `baselineY` 偏移即可（`UpperAnchor/EnemyArea` 的 Δ = **−402.82998657 px = −402.82998657/1080**）。
  ⚠️ `BattleHudShow` 的 EnemyArea 首键就是 402.83 ⇒ **若播放前不在那个位置，t=0 会先瞬跳上去再滑下来**；
  要么在换牌期先把它置到 402.83，要么接受这一帧瞬跳（本来就被换牌面板挡着）。

### 3·4 触发点（替换现在的硬切）

现在：`ApplyMulliganHudVisibility()`（`BattleDriver.cs:23856-23879`，`UpdateHud` 在 `:23900` 调）
用 `_hudGroup.alpha = 0` 硬切 + 对 `_foeHandViews` 逐个 `SetActive(false)`。

改法：
1. **换牌期**：改播 `BattleHudHiddenNoHand`（Instant）。这条**不含 EnemyArea / HandArea** ⇒
   敌方手牌那排要保留现在的 `SetActive(false)`（`23873-23878`），或把它预置到 402.83（见 3·3）。
   同时保留 `blocksRaycasts/interactable = false`；`alpha` 只在 rig 建失败时作兜底（别两条都走）。
2. **换牌结束**：改播 `BattleHudShow`（1 s）。原版触发点 = `_FinishMulliganFinalPhase...c:113`；
   我们的等价边沿 = `InMulligan`（`:5808`）翻 false（`OnMulliganDone` `:5925` → `BeginBattleAfterSetup` `:5962`）。
   注意 `ApplyMulliganHudVisibility` 现在每帧算 `show = !InMulligan`，要在**翻真那一帧**起播，不能每帧重置。
3. **战斗结束**：改播 `BattleHudHideNoHand`（1 s）。我们**目前没有这个动作** ——
   `BattleDriver.cs:11781-11789` 自己写着「我们没有『收整条 HUD』这个动作」；
   落点 = `CloseBattleDoors` 等价物（`24053` / `24125-24129` 那一段）。
4. **教程**：`BattleHudShowNoHand` / `HideNoHand` 是教程那条（`_TutorialStartSequence...c:283`），
   数据已在 JSON，接线时按 `ApplyTutorialVisibility`（`:23904`）那一档决定用哪套。

### 3·5 断言（接线时必须补）

- `HudAnimForTest`：暴露「当前播的 clip 名 / 归一化时间 t / 每个 rig 的 `anchoredPosition`」。
- **端点**：`t=0` 播 Hidden 时，9 个 rig 的 `delta` = `reference.hidden − reference.shown`
  （如 PlayerInfo = (−290, −358)、RightAnchor.x = +476、HandArea.y = +479）；
  `t=1` 播 Show 时 **全部 delta = 0**（= 回到我们的显示基准）。
- **灭自证**：再断一条「clip 数据里的原始绝对值」—— 例：`BattleHudShow` 的末键 PlayerInfo = (50, 28)、
  `BattleHudHidden` 的 EnemyArea = 402.82998657；这样「把生成器与运行时一起改回错误口径」不会两边同时绿。
- **时长**：`BattleHudShow` 必须 1.000 s，Instant 那 4 条必须 0.000 s。

---

## 四、没查清

1. **`BattleHud` 的 `.cctor` 不在 `d:/2/tools/decomp_full/`**（全目录只有 `ToggleWithAnimation` 等 30 余个方法，
   **没有** `BattleHud__cctor.c`）⇒ 8 个 `DAT_18428b*` 常量到底指向哪条 clip 名，**没有从二进制里读出来**。
   本报告的分支↔名字对应是推的（依据见 §一·1）；**要用得更硬就得补那个 `.cctor` 的方法体**。
2. `BattleHud__ToggleWithAnimation.c` 反编译出 **4 个形参**，但三个调用点都传 **5 个实参**（末尾恒 0）。
   函数体只读 `param_2/param_3/param_4`，那个第 5 参数**未使用**、语义不明。
3. **rest ≠ clip 末键**的两处（PlayerInfo.x 31.7002 vs 50 · SettingsBtn.x −48.1001 vs −54）**原因没查清**。
   现在的解释「animation 自加载起就覆盖 rest、所以 rest 是陈值」能解释现象，但**没有**运行期「换牌之后」的 dump 佐证
   （手上两条 dump 都是换牌期）。需要一次换牌结束后的 runtime UI dump，或反编译里找谁在 Show 之后又改这两个件。
4. `BattleHudHideNoHand` 的 SettingsBtn 起点 (−6.90002, −4.79999) 与任何其它 clip 都对不上，**不知道它是哪个状态的残值**。
5. `BattleHudShowInstantNoHand` 的 LeftAnchor.y = 17.0 是孤例，**没有第二个来源能解释 17**。
6. `animHand` 的语义（1 = 「不动手牌区」还是「此时手牌为空」）是**按调用点推的**，没有字段/字符串直接写。
7. **没有跑 Unity**：`RectTransform` 的 `m_AnchoredPosition` 在 `AnimationClip.SetCurve` 里的绑定
   **本仓没有先例**（`CameraIntro.cs` 只绑过 `Transform` 的 position/rotation/float）；
   资产侧 `classID=224` 是标准绑定，但没在本工程实机 probe 过。
8. 原版 `BattleHudHidden*` 那两条 Instant clip 由谁触发**没查到**（三个 `ToggleWithAnimation` 调用点全是 `useAnimation=1`）
   —— 最可能是 `m_PlayAutomatically` 的默认 clip（`HiddenNoHand`），但 `Hidden`（带 Hand 那版）的触发点未知。

---

## 五、证据与可复现

- clip 原始 JSON：`d:/2/新解包资源/assets_full/bundle_battlesharedresources_assets_all/AnimationClip/*.json`
- 场侧 `Animation` 组件：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/Animation/Animation_*.json`
- 场侧 rest 值 / 外部 CAB：UnityPy 直读
  `d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/scenes_scenes_battlearena*.bundle`
  （只读；每场读完即 `env.file` 释放，未一次性全 load）
- 反编译：`d:/2/tools/decomp_full/BattleHud__ToggleWithAnimation.c` ·
  `BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:113` ·
  `BattleManager._CloseBattleDoors_d__393__MoveNext.c:29` ·
  `BattleManager._TutorialStartSequence_d__592__MoveNext.c:283`
- 生成器自检包含：8 条 legacy 硬前提 / 16 条曲线名逐条核对 / 9 个落点两端值逐条核对 /
  斜率全 0 / 13 场 pid+hash 一致 / 抽 3 条与原 JSON 逐键对读 / 8 条 `differences` 覆盖。

**红线确认：未跑 Unity、未改运行时 `.cs`、未改现有 JSON/正本、未动 `d:/2/`、未派子代理、未动 git。**
