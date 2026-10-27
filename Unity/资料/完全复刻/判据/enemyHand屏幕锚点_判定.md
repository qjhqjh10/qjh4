# enemyHand 屏幕锚点 —— 判定

> 任务：把 `enemyHand` 的**屏幕锚点**算出来（展示位偏移表唯一还缺的一格，也是 `RevealStartPx/EndPx` 至今标 `🔶` 的原因）。
> 出题来源：`资料/完全复刻/调度台日志.md` §12·25 第 4 条 · `资料/完全复刻/报告/B9_W13CDW_展示位与加载态.md:174` · `资料/普查产出_1024b/C4_战斗表现判据.md` §2。
> 代理：**W26ANCHOR** · 2026-10-27 · 只读为主，产出仅本文件 + 报告。
> 数据源（按题给顺序）：① 场景 JSON `d:/2/解包整理/07_场景/battlearena1/`（RectTransform/Transform/GameObject）② 反编译 `d:/2/tools/decomp_full/` ③ 运行期 dump `runtime_ui_dump_drive.tsv` ④ 我们工程 `Hand/HandLayout.cs`。

---

## §一 结论

### 1·1 一句话

**`enemyHand` = `UpperAnchor/EnemyArea/HandArea` 这个 GameObject 的 `Transform`（`Transform_1295`），它的屏幕锚点 = `(960, 50) px` @1080p（x 从左边算、y 从**上**边算、向下为正）。**

- 水平：**正中**（x = 960 = 1920/2）。
- 垂直：**距屏幕顶边 50 px**（`EnemyArea` 的 pivot 落在画布顶下方 50 px；`HandArea` 的 `localPosition = 0`，所以 `enemyHand.transform` 与 `EnemyArea` **同点**）。
- 这个点**与「敌方手牌从上方垂下来」自洽**（手牌卡挂在它下面、往下展开）。
- ⚠️ **`HandArea`（50 px）≠ `HandAnchor`（0.32 px）**：`HandAnchor`（`Transform_1257`，`localPosition.y = 0.46×108 = 49.68`）才是**手牌布局基线**（`HandLayout.EnemyBaselineY` 用的就是它）；但原版 reveal 读的是 `enemyHand.transform` = **`HandArea`**（见 §一·3、§二·3），所以 reveal 锚点要用 **50 px**，⛔ 别拿 `EnemyBaselineY`（0.32 px）来顶（差 49.68 px）。

### 1·2 逐级累加表（`Canvas → … → HandArea`）

约定：画布在 1080p 下 = `1920×1080`（`CanvasScaler` 参考分辨率 1920×1080、`m_MatchWidthOrHeight = 0` 按宽匹配 ⇒ **1080p 时 `scaleFactor = 1`，1 画布单位 = 1 屏幕 px**；判据见 §二·2）。表中「屏幕 px」= 该节点 **pivot 点**的位置（x 从左边、y 从**上**边）。「世界(相对画布中心)」= `(屏幕px − 画布中心) ÷ 14.8364`，即 §二·4 的 px↔世界单位桥。

| 级 | 节点 | 类型 / PathID | anchorMin | anchorMax | pivot | anchoredPosition | sizeDelta | 屏幕 px（pivot 点，y↓） | 世界(相对画布中心) |
|---|---|---|---|---|---|---|---|---|---|
| 0 | `Canvas` | RectTransform `2759` | (0,0) | (0,0) | (0,0) | (0,0) | (0,0) | 运行时驱动；画布 = 全屏 1920×1080 | — |
| 1 | `BackCanvas` | RectTransform `2684` | (0,0) | (1,1) | (0.5,0.5) | (0,0) | (0,0) | 拉伸铺满 ⇒ pivot 在**画布中心** (960,540) | (0, 0) |
| 2 | `Safe area BackCanvas` | RectTransform `3498` | (0,0) | (1,1) | (0.5,0.5) | (0,0) | (0,0) | 拉伸铺满 ⇒ (960,540) | (0, 0) |
| 3 | `UpperAnchor` | RectTransform `2762` | (0.5,1.0) | (0.5,1.0) | (0.5,1.0) | (0,0) | (100,100) | pivot=顶中 ⇒ (960, **0**)（画布上沿） | (0, +36.40) |
| 4 | `EnemyArea` | RectTransform `2798` | (0.5,0.5) | (0.5,0.5) | (0.5,1.0) | (0,0) | (100,100) | pivot=顶中、锚在 `UpperAnchor` 中心（=顶下 50）⇒ (960, **50**) | (0, +33.03) |
| 5 | `HandArea`（= **`enemyHand.transform`**） | **Transform** `1295`（裸 Transform，`localScale=(108,108,108)`、`localPosition=0`） | — | — | — | localPos (0,0,0) | — | **与 `EnemyArea` 同点 ⇒ (960, 50)** | **(0, +33.03)** |
| — | （附）`HandAnchor` | Transform `1257`（`localPos.y=0.46`，×108=49.68 朝上） | — | — | — | localPos (0,0.46,0) | — | (960, **0.32**)（**手牌布局基线**，⛔ 不是 reveal 锚点） | (0, +36.38) |

**累加算式（可复核，纯加减乘）**
- `Safe area BackCanvas` 的 pivot 已在画布中心 `(0,0)`（画布中心为准）。
- `UpperAnchor`：`localPos = (anchor − 父pivot)×父size + ap = ((0.5,1)−(0.5,0.5))×(1920,1080) + 0 = (0, +540)` ⇒ pivot 落画布上沿（y 从中心 +540 = 距顶 0）。
- `EnemyArea`：`localPos = ((0.5,0.5)−(0.5,1))×(100,100) + 0 = (0, −50)` ⇒ 从画布上沿再下 50 ⇒ **距顶 50 px**。
- `HandArea`：裸 Transform，`localPosition=(0,0,0)` ⇒ 与 `EnemyArea` 同点。

### 1·3 reveal 终点 / 起点（原版公式落到 px）

原版 reveal 的落点（`CardScript.GetOffensiveCardPlayedDisplayPosition`，§二·3）：
`end = enemyHand.transform.position + Vector3.right×(−25) − Vector3.up×(+30)`（**世界单位**；`Vector3.right/up` 是引擎常量 `1`）。
把 `(−25, −30)` 世界位移换 px（×14.8364，见 §二·4）：

| 量 | 推导 | 结果 |
|---|---|---|
| **`RevealEndPx`** | `x = 960 + (−25)×14.8364`；`y = 50 + (+30)×14.8364`（世界 y **↓30** ⇒ 屏幕 y **+30 单位**） | **≈ (589.1, 495.1)** ⇒ 取整 **(589, 495)** |
| `RevealStartPx`（原版 = 世界零 `(0,0,0)`） | 世界零在 **UI 相机**下不在 UI 平面上（z=0，而画布平面在相机前 100）⇒ 投到屏幕约 `(28440, 540)`，**远在屏外右侧** | **不可表示为 1080p 屏内一点**（见 §五） |

> ⚠️ 与 `C4` §2·1 那行「终点 = `HandArea.position + (−25, **+30**, 0)`」的 **y 符号不一致**：逐句读反编译是 `pos.y − up.y×30 = pos.y − 30`（**往下 30**）。若照 `+30` 会算到屏外（距顶 −395 px）。见 §五·顺手发现 1。

---

## §二 证据（逐条可复核）

### 2·1 父链与各节点字段（场景 JSON，现读）

- 目录：`d:/2/解包整理/07_场景/battlearena1/{GameObject,Transform,RectTransform}/`（与 `assets_full/bundle_scenes_scenes_battlearena1/` 同值，任取一份均可）。
- GameObject 的 PathID = 文件名 `_<数字>.json` 的后缀；`RectTransform.m_GameObject.m_PathID` 与 `GameObject.<组件>.m_PathID` 互锁。**实测**：`EnemyArea_317.json` 的组件 = `[2798,4349]`，而 `RectTransform_2798.json` 的 `m_GameObject.m_PathID = 317` ✅。
- 父链（`m_Father` 逐级）：
  - `UpperAnchor`(GO 1105) → RectTransform `2762`，father `3498`；`amin=amax=(0.5,1.0)`、`pivot=(0.5,1.0)`、`ap=(0,0)`、`sd=(100,100)`
  - `EnemyArea`(GO 317) → RectTransform `2798`，father `2762`；`amin=amax=(0.5,0.5)`、`pivot=(0.5,1.0)`、`ap=(0,0)`、`sd=(100,100)`
  - `HandArea`(GO 318) → **Transform** `1295`（**不是 RectTransform**），father `2798`；`m_LocalPosition=(0,0,0)`、`m_LocalScale=(108,108,108)`
  - `Safe area BackCanvas`(GO 128) → RectTransform `3498`，father `2684`；`amin=(0,0)`、`amax=(1,1)`、`pivot=(0.5,0.5)`、`ap=(0,0)`、`sd=(0,0)`
  - `BackCanvas`(GO 1198) → RectTransform `2684`，father `2759`；同上拉伸铺满
  - `Canvas`(GO 983) → RectTransform `2759`，father `1395`（实为 `Canvas` 组件驱动，序列化值全 0）
- `HandAnchor`(GO 58) → Transform `1257`，father `1295`；`m_LocalPosition=(0,0.46,0)`、scale 1。

> 🔴 与「同一场景多份同类实例」相关的核对：`HandArea` 在本场景有 **GO 318（enemy, `isPlayer=0`）** 与 **GO 898（player, `isPlayer=1`）** 两份。判定用**用途字段**取：`MonoBehaviour_4350`（挂 GO 318）`isPlayer = 0` ⇒ **敌方**；`MonoBehaviour_4952`（挂 GO 898）`isPlayer = 1` ⇒ 我方。两者脚本同一 GUID（`-1233043523015714209` = `PlayerHand`）。**所以 enemyHand = GO 318 = `Transform_1295`**。

### 2·2 Canvas = ScreenSpaceCamera / plane 100 / 参考 1920×1080（⇒ 1080p 下 1 画布单位 = 1 px）

- `Canvas_2572.json`（`m_GameObject.m_PathID = 983`）：`m_RenderMode = 1`（= **ScreenSpaceCamera**）、`m_PlaneDistance = 100`、`m_Camera.m_PathID = 1462`。
- `MonoBehaviour_4731.json`（`m_GameObject = 983`，CanvasScaler）：`m_UiScaleMode = 1`（ScaleWithScreenSize）、`m_ReferenceResolution = (1920,1080)`、`m_MatchWidthOrHeight = 0`、`m_ScaleFactor = 1`。
- 相机 `Camera_1462.json`（GO 350「UI Camera」）：`field of view = 40`、`orthographic = false`；其 Transform `1320` 位置 `(−343, 0, −18.5185)`、旋转 identity（看 +Z）。
- ⇒ `scaleFactor = 2×100×tan20° / 1080 = 0.0674019 = 1/14.8364`（画布被相机缩到 1/14.8364）⇒ **1 画布单位 = 1 屏幕 px @1080p**；且 `1080p / 2 / 100 / tan20° = 14.8364` = **14.835 px/世界单位**（与 `资料/手牌布局_原版算法与参数.md` §八① 同值，那里记 14.835 是四舍五入）。

### 2·3 reveal 落点 = `enemyHand.transform` + 世界位移（反编译逐句）

- `d:/2/tools/decomp_full/CardScript__GetOffensiveCardPlayedDisplayPosition.c`：
  - `lVar5 = *(param_2 + 600)` = CardScript 的 manager（BattleManager）；`*(lVar5 + 0xd8)` = **`enemyHand`**（`dump.cs:30741`：`public PlayerHand enemyHand; // 0xD8`）。
  - `Component.get_transform(enemyHand)` → `Transform.get_position` ⇒ **取的是 `enemyHand` 这个 `PlayerHand` 组件所在 GameObject 的 transform** = `HandArea`（GO 318 / Transform 1295，§2·1）。
  - `fVar8 = globalVars + 0x60 = offensiveCardDisplayXoffset = −25`；`fVar2 = globalVars + 0x5c = offensiveCardDisplayYoffset = 30`（值见 `资料/VarsGlobal_原版数值.md:35-38`）。
  - 返回值（按 Vector3 布局 x/y/z 解包 `CONCAT44`）：`x = pos.x + right.x×(−25) − up.x×30 = pos.x − 25`；`y = pos.y + right.y×(−25) − up.y×30 = pos.y − 30`；`z = pos.z`。
  ⇒ **end = pos + (−25, −30, 0)（世界单位）**。
- `BattleManager__DisplayRevealedCard.c`：`:34` `CreateCard(card, parent = transform(BattleManager+0xd8 /*enemyHand*/), …)`；`:59-63` `transform.position = Vector3.zero`（**起点 = 世界零**）；`:64-65` 起协程 `DisplayOffensiveCardInCenter`。
- 交叉印证（卡确实挂在 `enemyHand` 下、走画布 px 桥）：卡终点缩放 = `localScale 1.4` 挂在 `HandArea`（lossyScale `108×0.06740 = 7.28`）下 ⇒ 世界宽 `2.0927×1.4×7.28 = 21.33` 世界单位 `×14.8364 = 316.5 px`，与 `资料/普查产出_1024/D6_进攻卡展示缩放.md` 的「2.0927×1.4 ≈ 316 设计 px」一致（`108 = 7.28×14.8364` 正是两把尺的桥）。

### 2·4 px↔世界单位桥

- `14.8364 px/世界单位 @1080p`（`1080/(2×100×tan20°)`）。
- 同值在 `资料/手牌布局_原版算法与参数.md` §四/§八① 已作正本（记 14.835），本文件与之一致。

### 2·5 运行期 dump（旁证，非主判据）

- `d:/4/Unity/资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive.tsv`：
  - `:333 UpperAnchor`、`:334 EnemyArea` 有 `RT` 行（dump 头「rect/pos/pivot」列名**错位**，真身依次是 **anchoredPosition / sizeDelta / anchorMin / anchorMax / pivot**，判据 = mod 源 `d:/2/Warpforge_tools/scenejumpshot/SceneJumpShot.cs:41-49`）。读出的值与 §2·1 序列化一致（`UpperAnchor ap=(0,0) sd=(100,100) amin=amax=(0.5,1) pivot=(0.5,1)`；`EnemyArea ap=(0,0) sd=(100,100) amin=amax=(0.5,0.5) pivot=(0.5,1)`）。
  - `:335 HandArea`、`:338 HandAnchor` 都是 `TF`（裸 Transform）⇒ 这正是「运行时 dump 拿不到锚点」的原因；但**父链字段在场景 JSON 里是齐的**（§2·1），所以能算。
- 旁证：`:341 BottomAnchor/PlayerArea/HandArea`（TF）、`:346 …/HandAnchor`（TF），与 `资料/手牌布局…` §八① 的玩家链一致。

### 2·6 可复现脚本（现读三份 JSON + 累加）

```python
import io, json, os, glob
base = r'd:/2/解包整理/07_场景/battlearena1'
def rt(pid): return json.load(io.open(os.path.join(base,'RectTransform','RectTransform_%d.json'%pid),'rb'))
def tf(pid): return json.load(io.open(os.path.join(base,'Transform','Transform_%d.json'%pid),'rb'))
# 读出的值（本文件 §2·1 即由此产出）
for pid in (2759,2684,3498,2762,2798):
    o=rt(pid); print(pid,o['m_AnchorMin'],o['m_AnchorMax'],o['m_Pivot'],o['m_AnchoredPosition'],o['m_SizeDelta'],'father',o['m_Father']['m_PathID'])
o=tf(1295); print('HandArea',o['m_LocalPosition'],o['m_LocalScale'],'father',o['m_Father']['m_PathID'])
o=tf(1257); print('HandAnchor',o['m_LocalPosition'])
# 累加（父 pivot 在画布中心；画布 1920x1080，y 取「距顶」）
# UpperAnchor: ((0.5,1)-(0.5,0.5))*(1920,1080) = (0,540)  -> 距顶 0
# EnemyArea:   ((0.5,0.5)-(0.5,1))*(100,100) = (0,-50)     -> 距顶 50
# HandArea localPos 0                                        -> 距顶 50  (x=960)
pxu = 1080/(2*100*__import__('math').tan(__import__('math').radians(20)))  # 14.8364
print('enemyHand=(960,50)  EndPx=(%.2f, %.2f)'%(960+(-25)*pxu, 50+(30)*pxu))
```

---

## §三 与我们现状对照

| 项 | 原版（本文件判定） | 我们现状 | 差 |
|---|---|---|---|
| 敌方手牌**布局基线** | `HandAnchor` = 距顶 **0.32 px**（`Transform_1257.y = 0.46×108`） | `HandLayout.EnemyBaselineY = 1 − 0.32/1080 = 0.999704`（`Hand/HandLayout.cs:154`，`ConfigureForEnemy` 里 `baselineY = EnemyBaselineY` `:414`） | ✅ **一致**（这条早就算对了，但它是 **HandAnchor**、不是 reveal 用的 **HandArea**） |
| 敌方手牌**载体** | 世界物件（卡本体，挂在 `enemyHand` 下，UI 层） | 世界空间 quad（`_foeHandViews`，`SyncFoeHand` 建，不挂 `HudCanvas`），位置走 `HandLayout.SlotPosition` | 载体不同（我们世界 quad）；基线与原版一致 |
| `UpperAnchor/EnemyArea` 节点 | `EnemyArea` pivot 距顶 **50 px** | 我们**没有真的 EnemyArea RectTransform**；只有 `BattleHudAnim` 的**虚拟 rig 名** `"UpperAnchor/EnemyArea"`（`Battle/BattleHudAnim.cs:126`），仅用于换牌期滑出（Δy=402.83） | 我们无对应世界几何件；reveal 用常量 `RevealEndPx` 直接给像素 |
| `RevealEndPx` | **(589, 495)**（§一·3） | `(960, 520)`（`Battle/BattleDriver.cs:9860`，头注「🔴 我们挑的」） | **x −371 px、y −25 px**；这就是「`RevealStartPx/EndPx` 是我们挑的」那处 |
| `RevealStartPx` | 世界零 ⇒ 屏外（不可表示，§五） | `(960, 660)`（`BattleDriver.cs:9859`，头注「🔴 我们挑的」） | 原版起点是**屏外+极近相机+0.01 缩放**（≈不可见），我们给了屏内一点；见 §五 |
| `Reveal` 渲染相机 | **UI Camera（`Camera_1462`，mask=layer 5）** | 我们建在 `hudRoot`、正交 HUD 画（`LayoutSpace`，高 10 世界单位） | 原版透视图层（plane 100、14.8364 px/单位）；我们正交无透视 ⇒ 只能近似（起点那半步无法还原） |

> 🔴 **别混 `HandArea` 与 `HandAnchor`**：我们 `EnemyBaselineY`（0.32 px）是给**手牌布局**的；原版 **reveal 读的是 `enemyHand.transform` = `HandArea`（50 px）**。若拿 `EnemyBaselineY` 去算 reveal，会**高 49.68 px**（`EndPx.y` 会变成 `0.32+445≈445`，而正确是 `495`）。

---

## §四 该怎么用（`RevealStartPx/EndPx` 该填什么）

1. **`RevealEndPx`：可撤 `🔶` → 填 `(589, 495)`**（`BattleDriver.cs:9860`）。
   式：`( 960 + (−25)×14.8364 , 50 + (+30)×14.8364 )` = `(589.09, 495.09)`。
   - 出处：`(960,50)` = `enemyHand` 屏幕锚点（§一·2）；`−25 / +30` = `offensiveCardDisplayXoffset / Yoffset`（`资料/VarsGlobal_原版数值.md:35-38`）；`×14.8364` = px 桥（§二·4）；**y 是加** 因为世界 `−30`（向下 30 单位）在屏幕 y↓ 里就是 `+30×pxu`。
   - 配套：`RevealEndScale = 1.4`（已由 `D6` 定为原版绝对 localScale，别动）。
   - ⚠️ 改这个常量会让 reveal 落点从「正中偏下」移到「**偏左、垂直近中**」——这是**原版真实落点**（`enemyCardDisplay` 姊妹档 X=`−26` 同理也偏左），不是 bug。

2. **`RevealStartPx`：暂时**保留 `🔶`（不建议撤标）**。**理由：原版起点 = 世界 `(0,0,0)`，在 **UI 相机**下 z=0 而画布平面在相机前 100 ⇒ 它**不在 UI 平面上**，投影到屏幕约 `(28440, 540)`（**屏外极右**）+ 起手缩放 0.01（≈不可见）。我们的 `LayoutSpace`（正交、高 10、无透视）**根本表达不了这个「近相机 + 屏外」的点**。⇒
   - 若要「行为近似」：可把 `RevealStartPx` 设成与 `EndPx` 同点或略上方任意点——因为起手 `RevealStartScale = 0.01`（≈2 px），**肉眼不可见**，起点像素对观感几乎无影响。
   - 若要「逐字忠实」：得在 reveal 里单独做一条「从屏外右侧飞入」的路径（超出 `FromPixel` 能力），属另一件事。
   - 结论：**`EndPx` 去 `🔶`、`StartPx` 留 `🔶` 并在注释里改成「原版是世界零、不在 UI 平面 ⇒ 我们的正交布局不可表示；起手 0.01 缩放故观感无关」**。

3. **落点位置的「世界零」那半句也顺带澄清**：`C4`/`D6` 里「起点 = 世界零」是对的，但**不能**直接当屏幕像素用（它不是 UI 平面上的点）。

---

## §五 还差什么 / 没查清

1. **reveal 卡实际被哪台相机画（唯一残余假设）**：
   - 逻辑上**必然是 UI Camera**（`Camera_1462`，`m_CullingMask = 32 = layer 5`）：`enemyHand` 处在 **UI 画布世界空间**（画布中心在 UI 相机 `(−343,0,−18.5)` 前 100 ⇒ `(−343,0,81.5)`），若由 `BoardCamera`（`GO 348`，`(0,2.22,−13.57)`，mask 1137）画，`enemyHand` 会落在**屏外极左**（x 投出约 −4550 px）⇒ 不可能。故必须是 UI Camera。
   - **未逐字节确认**：`BattleCardManager.CreateCard` 的第 5 实参（layer）在 `DisplayRevealedCard` 里取自 `*(lVar4+0x2c)` 一处间接对象字段，**没解到具体数值**。这条**只影响「确认」不影响「数值」**（已用「enemyHand 必须在 UI 空间才可见」反证）。
   - 建议：真机看一次 reveal 卡落点是不是「偏左、垂直近中」（≈(589,495)/1920×1080）。若明显不是，再回来查 layer / 相机。
2. **`RevealStartPx` 不可表示**（§四·2）—— 不是「没查到」，是「原版那个点不在 UI 平面上」，属结构性问题。
3. **非 1080p 档**：本文件全部按 **1080p 基准**算（`scaleFactor=1`、1 画布单位=1 px）。其它分辨率下 `CanvasScaler`（按宽匹配）会让 1 画布单位 ≠ 1 px（4:3 下画布变高）⇒ **y 的 px 会变**。我们 `LayoutSpace` 走「全局映射」，这条不是新问题（见 `LayoutSpace.cs` 的 `A2098` 说明），本文件不展开。
4. **`HandAnchor` 的用途确认**：`PlayerHand` 的 `+0x50` 在 `dump.cs` 是 `List<CardScript> currentHand`（**不是** Transform），所以 `BattleManager.GetEnemyHandRef`（返回 `*(enemyHand+0x50)`）取的是**手牌列表**、不是锚点 —— 与 reveal 无关（reveal 走 `Component.get_transform(enemyHand)`）。已核，记此备查。
5. **四舍五入**：本文件 pxu 用精确 `14.8364`；项目正本记 `14.835`。二者差 0.01%，`EndPx` 差 <0.1 px，取整后同为 `(589,495)`。

---

## 顺手发现（只报，未改任何白名单外文件）

1. **`C4` §2·1 的终点 y 符号疑似写反**：`资料/普查产出_1024b/C4_战斗表现判据.md:112` 写「终点 = `HandArea.position + (−25, +30, 0)`」；逐句读反编译应为 `(−25, **−30**, 0)`（`pos.y − up.y×30`）。照 `+30` 会算到**屏外顶部**（≈距顶 −395 px），照 `−30` 才是屏内 `(589,495)`。请主对话按铁律 5 处置（不在本代理白名单）。
2. **`C4` §2·2 / `D6` §五·1 的「还差什么」现已补齐**：它们写「enemyHand 世界坐标→屏幕像素没算」「−25/30 落到哪个 px 没解出来」。本文件已给出：`(960,50)` 与 `(589,495)`。可据此把 `D6` §五·1、`W_卡牌飞行族普查.md:118-119` 的「仍未解」改成「已解（判据 → 本文件）」。
3. **`BattleDriver.cs:9805-9807` / `:9860` 的头注**「三处本地都没解出来」对 **`RevealEndPx`** 已过期（现可算）；对 **`RevealStartPx`** 仍成立（世界零不在 UI 平面）。改注释时按此区分，别一刀切。
4. **`runtime_ui_dump` 表头列名错位**是全表通用坑（`rect/pos/pivot` 实际是 `anchoredPosition/sizeDelta/anchorMin/anchorMax/pivot`），凡按该表读锚点的人都会踩；判据 = `SceneJumpShot.cs:41-49`。已在本文件 §2·5 标注。

---

## 交接给下一个代理（若需继续）

- 本件已闭合「`enemyHand` 屏幕锚点 = (960,50)」与「`RevealEndPx = (589,495)」两格。
- 若要往下推：① 真机确认 reveal 落点（§五·1，需 `-wfscene`/`resource` 起 player，超出只读范围）；② 让执行代理把 `BattleDriver.cs:9860` 的 `RevealEndPx` 改成 `(589,495)` 并删 `🔶`、把 `:9859` `RevealStartPx` 的注释按 §四·2 改写（注意 `:9802-9807` 那段头注里「EndPx 解不出」要同步订正为「已解」，`StartPx` 保留未解口径）；③ 按顺手发现 1/2/3 订正 `C4`/`D6`/`W_卡牌飞行族普查.md`/`BattleDriver` 注释。
- 文件所有权提醒：本代理只写了本文件 + 报告，**未动任何 `.cs`/`.tsv`/正本/资产**。
