# W · 卡牌飞行族普查（只读）

> 时刻：**2026-10-16**（本机日期）。**行号一律本件现核**（本仓旧文档行号整体漂移是常态）。
> ⛔ 没跑 Unity · ⛔ 没动 git（写）· ⛔ 没改任何 `.cs`/`.unity`/`.json` · ⛔ 没改两张正本 · ⛔ 只写了本文件。
> 判据源：① 原版反编译 `d:/2/tools/decomp_full/`（**引方法体前首行函数名已逐一核过**）② 规格 `资料/3DBody_原版场上卡体规格.md` §四
> ③ 同族诊断 `资料/普查产出_1011/WB3_A337.md` / `WB4_A337_A359.md`（**用结论、行号全部现核**）④ 我们自己的源码。

---

## 〇、一句话

「卡牌飞行」本身**不是一类问题**（它只是「手牌→棋盘」那一拍的位移动画）；
但它**暴露出来**的那一类问题是真的、而且**全仓还有 3 处开着** —— 识别式 = **同一个物理点被两个坐标系各表达一次，而代码在两者之间漏了一次显式换算**
（一侧 = 竞技场 3D 世界 `ArenaSlots.*` / 182.14 px·单位⁻¹；另一侧 = HUD 正交平面 `LayoutSpace.*` / 108 px·单位⁻¹）。
🔑 **原版自己给的判据工具**：`BattleManager.Get2DWorldPosFromBoardPos`（`d:/2/tools/decomp_full/BattleManager__Get2DWorldPosFromBoardPos.c` 亲读）
= `boardCam.WorldToScreenPoint(棋盘点)` → `hudCam.ScreenToWorldPoint(x, y, Canvas.planeDistance)` —— **我们的等价物 = `BoardLayout.DropTargetWorld`**（`Board/BoardLayout.cs:506-516`）。

---

## 一、Q1：「卡牌飞行」这条链逐条

| # | 是什么 | 我们的实现（文件:行号，现核） | 原版是什么（判据出处） | 差在哪 / 结论 | 同一类吗 | 已修/未修 |
|---|---|---|---|---|---|---|
| 1 | **手牌→棋盘（出牌，单位卡）** | `Hand/CardInteraction.cs:547` `DeploySequence.Play(card, which.DropTargetWorld(land), which.placedScale*LayoutSpace.Scale, …)`；`Board/DeploySequence.cs:44`(总时长 **0.3**) `:47`(抬升占 35%) `:51`(**LiftHeight 0.35**) `:61-62`(两段 DOMove) `:68-69`(rot/scale `Insert(0f,…)`) | 三段：① `PlayMinionFromHand` 换算+挂父 ② `DOLocalMove` 到 **槽 + up×`minionConversionHeight`（我 1.0 / 敌 2.1）**，**0.30 s** ③ 落 y=0 **0.2 s** + 落地前 **0.05 s**（`MinionManager__MoveMinionToConversionPoint.c:90-96` · `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:135-151,186-205`） | 总时长 ✅ / 终点 ✅（A337）。**形状不同**：原版「先飞到槽**上方**再垂直落下」，我们「抬一点再**对角滑**过去」；**`minionConversionHeight` 1.0/2.1 全仓零消费**；**没有落下那一段（0.2 s）** | ✅（终点）· ❌（形状/高度） | 终点**已修**（A337）· 形状/高度**未修** |
| 1b | 落地反馈（扬尘 + 相机抖动） | 只接了**音**：`Battle/BattleDriver.cs:11716` `PlayBattleSfx3D(BattleSfx.LandOnGround, …)` | `CardScript.LandOnGround`（`CardScript__LandOnGround.c:26-41`）：扬尘粒子 + **`shakeWhenLand` 相机抖动** + 音 | **音 ✅ · 扬尘/抖动 ❌**（源码自己标「震动那半**不在本件范围**」`BattleDriver.cs:11713-11716`） | ❌（不是坐标系问题） | 未修 |
| 1c | 交接那一帧（2D 视图 → 3D 层） | `BattleDriver.SyncBoard` → `PoseFor:12531-12549`（3D 时 `ArenaSlots.RootPosition` + `SetLayer(ArenaLayer)` `:12660`） | `BattleManager__GetUnitSizeInPlay.c:21-29` · `UpdateMinionInPlayPosition.c:105-118` | ✅ 逐值一致（缩放 0.36/0.69 · 督军 0.4/0.77 · y=0 + 半卡高补偿） | ✅ | **已修**（2026-09-20 搬进真 3D） |
| 2 | **抽卡飞入手牌（我方）** | `BattleDriver.PlayDealAnimation:13036-13064`；起点 = `LayoutSpace.ToWorld(MyDeckX01, MyDeckY01)`（`:13039`），`CardFeel.DealInPose:1426`，时长 `DealDuration = 0.3`（`CardFeel.cs:1368`） | `VarsGlobal.timeToDrawCard = 0.3`（`CardScript.DrawCard` 用 `DOScale+DORotate+DOMove`） | ✅ 时长与起点对。⚠️ 原版抽牌那一瞬还 `TurnCardAround`（绕 Y 翻 180°）——**我们没做**（`CardFeel.cs:1472` 自述） | ❌ | 主体**已做**；翻转未做 |
| 3 | **抽卡飞入敌方手牌** | `CardFeel.DealInQuad:1473`（卡背 `ImageQuad`），0.15 s = `DealDurationFoe`（`:1375`） | `VarsGlobal.timeToDrawEnemyCard = 0.15`（`VarsGlobal.TimeToDrawCard(bool isPlayer)`） | ✅ 值对（判据只此一处 = `DealSeconds(bool)` `:1378`） | ❌ | 已做 |
| 4 | **换牌期飞进换牌区** | `BattleDriver.PlayDealAnimation:13047-13049` `DealInPose(v, deck, MulliganRowPose(...), …)`；落点判据只此一处 = `MulliganRowPose:12992` | `PlayerHand.MoveCardToMulliganPos.c`（位 = `CardsHorizontalLayout.GetPosition(i,n)`） | ✅ 落点已从「手牌位」改成「换牌行」（A1540，修掉了「牌被拉回屏幕底部」） | ❌ | **已修**（A1540） |
| 5 | **回手 / 召回（`backToHand`）** | `CardFeel.ReturnToHand:1094-1136`（倒放同一时间轴：抬升 `up × localScale.x × 3.0`、**0.208 s 线性** `:1140/:1143`，总长定成 **0.9167**）+ 溶解/卡面淡回；调用点 `BattleDriver.PlayReturnFeel:12462`、`:12483` | `BattleCardUI.PlayBackToHandAnimation` = 把 `Card Hand To Board` **`speed = −1`** 倒放（`_DAT_1834b2bc8 = −1.0`）+ `DelayedResetMaterial(0.55)` | ✅ 时间轴四个时刻全部由正向常量推出、逐值对。⚠️ 取不到原版溶解 shader 时**退「淡出」并出声**（如实标，`CardFeel.cs:1107-1108`） | ❌ | **已做**（2026-09-29） |
| 6 | **战术卡飞向目标** | **复用同一条**：`CardInteraction.cs:547`，`which` = `foeBoard`（`DropReject:443-447` 先试敌半场）→ `foeBoard.DropTargetWorld(slot)` | 原版 spell 那一支的位移参数**本件没核**（未读 `BattleManager._ResolvePlayCardFromHand_d__447` 的 spell 分支） | 机制一致（终点 = 目标格 3D 投影）· 参数**没查** | ✅（终点） | 终点**已修**（A337 顺带）· 参数未核 |
| 7 | **进攻卡用完飞向 HUD 那颗钮** | `BattleDriver.StartRevealFlight:5480-5491`：`DOMove(_offensiveBtn.transform.position, 1.0 s)` + `DOFade`；节奏在 `TickOffensiveReveal:5435-5461`（四段 2.0/0.5+1.0/3.0 s） | `CardScript._DestroyCardOffensive_d__270__MoveNext.c`：`DOMove` 目标是 **`BattleHud.OffensiveButtonTransform()`**（**不是墓地**） | ✅ **两端都在 HUD 平面**（`_revealCard` 建在 `hudRoot`，`:5422`）⇒ 不是坐标系问题。⚠️ 差的两条：① 原版还带 `SetCardStateInCemetery`+`DissolveWholeCard`（我们是临时视图 ⇒ 淡出收掉）② **起点/终点像素是我们挑的**（`RevealStartPx/EndPx:5350-5351`）、`RevealEndScale` 是**推导值**（`:5358`）——~~原版那两个基准**本地解不出来**（已如实标）~~ 🔴 **2026-10-24 订正（铁律 5）：基准【已解出】= `Vector3.one`（绝对 `localScale`，×`globalVars+0x48` = 1.4）**，`RevealEndScale` 已从「推导值」改为读同一原版函数；详见 §五 第 3 条与 `资料/普查产出_1024/D6_进攻卡展示缩放.md` §2·1 | ❌ | 已做；~~基准值为我们挑~~ → 终点现在是原版绝对 **1.4**（起点/落点像素仍是我们挑的） |
| 8 | **进墓地（`GoToCemetery`）** | **没做** —— 阵亡爆散体播完直接 `Kill`（`BattleDriver.cs:12439`） | `CardScript.UnitDeath` 第 ④ 步：`WaitForSeconds(0.5)` → `WaitForSeconds(0.2)` → `GoToCemetery()`（另有两条音效，`CardFeel.cs:1158`） | **整段没做**（全仓 grep `GoToCemetery` ⇒ 只有注释一处命中，无实现） | ❌ | **未修** |
| 9 | **卡面放大窗「开窗」** | **没做动画** —— 直接摆终态 | `Card Display Open`：legacy clip · 60 fps · **0.3167 s / 19 帧**，5 个槽从 `(0,60)/0°/scale 210` 补间到扇形终态 | 整段没做（账在战斗线 `T04` 第 2 条） | ❌ | **未修**（已有账） |
| 10 | 卡面放大窗「相关卡换位」 | `Battle/CardDisplayWindow.cs:738,759` · `Shell/CardDetailPopup.cs:715,736` `CardTween.ToPose(…, CardFan.SwapTime)` | 有换位补间（`CardFan`/legacy clip，判据收在共用件） | ✅ 已做 | ❌ | 已做 |

**§一 小结**：
🔴 **「卡牌飞行」的参数缺口 = ① 中段抬升形状是「我们挑的」（原版是 `minionConversionHeight` 1.0/2.1 + 垂直落下）② 落下那一段 0.2 s + 落地前 0.05 s 没做 ③ 落地扬尘/相机抖动没做 ④ 抽牌绕 Y 翻转没做 ⑤ 进墓地整段没做 ⑥ 开窗动画没做。**
🔴 **顺手查出一条【记错的文档】**：`Core/CardFeel.cs:1517-1519` 自己写着
「`timeToLand 0.2` / `timeBeforeLand 0.05` —— ✅ **这一条已经接上了**：见 `DeploySequence`」，
**同一段括号里又自承「等有落地特效时再用」** —— 两说打架；**实据**：全仓 grep `timeToLand|timeBeforeLand|minionConversionHeight|shakeWhenLand` ⇒ **只有注释命中、零消费点**（`DeploySequence` 从头到尾没引用它们）。
⇒ 那三行 **✅ 是假的**，应改成「**未接**（判据在，落点缺）」。

---

## 二、Q2：这一类问题（识别式 + 全仓现存实例）

### 2·1 识别式（**我确认**题面初判，并把它精确化）

> **同一个物理点被两个坐标系各表达一次，而代码在两者之间漏了一次显式换算。**
> 一侧 = **竞技场 3D 世界**（`Board/ArenaSlots.cs`：`Position/RootPosition/CardCenter`，182.14 px·单位⁻¹，场上卡挂在 `ArenaLayer`(8) 由透视相机画）；
> 另一侧 = **HUD 正交平面**（`Core/LayoutSpace.cs`：`ToWorld/FromPixel/ToPixel`，108 px·单位⁻¹，其余所有层含 Default 由正交相机画）。
> ⛔ **精确化（题面初判的半个反例）**：光看「取点走 `SlotPosition`」会**误报** —— 判据不是「这个量从哪来」，而是「**消费方画在哪台相机里**」。
> 实据：`BattleDriver.BoardWorld:11929-11934` 给**音频**喂的是 `cardView.transform.position`（**竞技场**坐标）—— 那里**是对的**；而 `:12037` 给**特效**喂的是 `SlotPosition`（**HUD** 点）—— 那里**是错的**。同一个文件、同一拍、两个量。

### 2·2 全仓现存实例

| # | 是什么 | 我们的实现（文件:行号，现核） | 原版是什么（判据出处） | 差在哪 / 结论 | 同一类吗 | 已修/未修 |
|---|---|---|---|---|---|---|
| A | **出牌补间终点** | `Hand/CardInteraction.cs:547`（原 `which.SlotPosition(land)`） | 3D 卡心投影 = `Get2DWorldPosFromBoardPos` 的等价 | 3D 下两点差 **0.73 世界单位（≈79 px）**⇒ 交接那一帧整张卡跳一下 | ✅ | **已修 A337** |
| A′ | 它的检测器 | `Editor/BattleScene.cs:3640` `arrivePos = pBoard.DropTargetWorld(freeSlot)`；`:3646/:3654` 灭自证（`DropTargetWorld` 与 `SlotPosition` 在 3D 下可分 + 2D 档**逐位相等**） | —— | 断言靶与实现**同一个口** | ✅ | **已修 A337** |
| B | **伤害飘字基准** | `Battle/BattleDriver.cs:12340` `Vector3 at = layout.DropTargetWorld(e.Slot)` | 原版飘字挂在**受击那张卡**上（`damageCounterParent`） | 3D 下原写法偏低 **51~72 px** | ✅ | **已修 A359** |
| B′ | 它的断言（+灭自证） | `Editor/BattleScene.cs:12391-12407`（容差 0.9 → **0.05**，且断「**不是**落在 2D 行线上」`dLinePop > 0.05`） | —— | —— | ✅ | **已修 A359** |
| C | **攻击位移的「静止位」** | `Editor/BattleScene.cs:12314-12371`（靶 = 卡自己的 `transform.position`；`:12322-12328` 灭自证 `sepLine > 0.02`） | —— | 改前用 `SlotPosition` 当静止位 ⇒ `Vector3.Distance(…) > 0.02f` **恒真**（弱断言） | ✅ | **已修 A360** |
| D | **两条兵线（锥角修正）** | `Battle/BattleDriver.cs:2006-2026` `TryMinionLines`（`boardCam != null` ⇒ `ArenaSlots.Position(...).z`；否则退回正交平面） | 原版那两个量是**战场世界坐标**（`WFModuleScaleByTarget` 的 abs 掩码只取 Z） | 改前只拿 2D 点 ⇒ 量纲错 **3.4 倍**、静默 | ✅ | **已修 A368** |
| **E** | **棋盘事件的特效落点** | `Battle/BattleDriver.cs:12037` `CardEffects.FireEvent(evt, layout.SlotPosition(slot), …)` · `:12048` `CardEffects.Fire(sc, layout.SlotPosition(slot))` | 原版登场/召唤特效在**那一格的卡上** | 🔴 **未修**。根特效 prefab 根 GO 层 = **0（932/962）**⇒ 由 **HUD 正交相机**画（`boardCam.cullingMask = 1<<8`）⇒ 喂 2D 行线 = 特效比卡偏 **51~72 px**（喂竞技场坐标会更远、≈267 px）⇒ **只有 `DropTargetWorld` 是那一档的正解**。账 = **`A367`（挂起·等真 Play）** | ✅ | **未修**（A367 挂起） |
| **F** | 🆕 **攻击方式选择器整条锚点** | `Battle/BattleDriver.cs:8668` `BoardViewAt(slot).transform.position` → `Battle/AttackSelector.cs:469` `_anchorAt` → `:542` `RefreshLayout` 直接当 HUD 局部坐标用 | 🔴 **硬判据**：`UnitOnBoardAttackTypeSelector__Toggle.c:120-128`（亲读）先 `Get2DWorldPosFromBoardPos(卡.transform.position)`、**再** `set_position` | 🔴 **未修·本件新发现**。3D 下喂的是**竞技场坐标**（我方槽 0 x = −3.37）而选择器节点在 **Default 层由 HUD 正交相机画**（`Editor/BattleScene.cs:23228-23230`，`sceneRoot` 在原点）⇒ 我按两台相机逐值推算 **≈267 px 偏右**（见 §四·3 算式，⛔ **没有实跑**） | ✅ | **未修**（无账） |
| **G** | **落点底片 `PlaceMarkers`** | `Board/BoardLayout.cs:293` `LayoutSpace.FromPixel(msp.x, msp.y)`，而 `msp = boardCam.WorldToScreenPoint(...)`（`:290-291`） | `FromPixel` 收**设计像素 1920×1080**（`LayoutSpace.cs:188-189`）；`ScreenToWorld`（另一条路 `DropTargetWorld:513`）收**实际屏幕像素** | 🔴 **未修**。`WorldToScreenPoint` 给的是**实际**像素 ⇒ 只有 1920×1080 下等价（WB3 §六·4 已报，**从未立 A 账**）。批处理下相机 `pixelWidth/Height = 640×480`（`_tmp_view/BattleScene.log` 自打的相机行）⇒ **自检里也是错的** | ✅（量纲/坐标系同族） | **未修·未立账** |
| **H** | **`DropTargetWorld` 没有督军位分支** | `Board/BoardLayout.cs:510` `float sc = ArenaSlots.CardScale(mirror)`（0.36/0.69），而真落位走 `PoseFor` 的 `HeroScale`（0.4/0.77，`BattleDriver.cs:12539`） | —— | 🔴 **未修·未验（读码推的）**：督军位卡心投影可能不准（WB3 §五·3 / WB4 §五·1 都报过、都**没实跑**）。A359 之后影响面**变大**（飘字基准也走它） | ✅ | **未修** |
| I | `BoardLayout._band`（自加的「槽带」） | `Board/BoardLayout.cs:322-329`（位置 = `LayoutSpace.ToWorld(0.5f, lineY)`） | 原版没有这件（是 3D 地板自带分界，`BoardLayout.cs:238-239` 自述） | 3D 下它停在 2D 行线（差 ≈150 px）—— 但 `BandColor` **alpha = 0**（`:213`）⇒ **不可观测** | ⚠️ 形式上是 | 📌 只登记，不构成缺陷 |
| J | 教程提示锚点 `TutorialAnchor` | `Battle/BattleDriver.cs:9611-9668`（`:9667` `center = v.transform.position` ⇒ 3D 下是竞技场坐标） | ✅ **原版也这样**：`TutorialTipScript._ShowTip_d__19__MoveNext.c:29-33` 是 `get_position` → `set_position` **原样转发**，全篇无 `Get2DWorldPosFromBoardPos` | **不是实例**（我们照抄原版）。⚠️ 但那说明「原版有一部分 UI 节点确实吃棋盘坐标」⇒ **别一把梭全改**（见 §三 反例 2） | ❌ | 与原件一致 |
| K | 准星弧线起点 | `Battle/TargetReticle.cs:575-590`（`from` = 卡 `transform.position`）；终点 = `ResolveAim:482-518`（两台相机显式换算） | ✅ `UpdateTrail.c:62-144` + `MoveCrosshair.c:12-23`：anchor **也是原样** `SetPoints`；①→④ 那条链我们逐句照抄 | **不是实例**（原版就是混的；我们与原件一致） | ❌ | 与原件一致 |

### 2·3 顺带核到的两个「正解样板」（以后照这两个写）

1. **音频**：`BattleDriver.BoardWorld:11929-11934` —— 音频要的是**世界点**，所以直接给 `cardView.transform.position`（竞技场），**不需要换算** ✅。
2. **准星的求交**：`TargetReticle.ResolveAim:493-517` —— 「`boardCam.WorldToScreenPoint` → `hudCam.ScreenPointToRay` → 打两个平面取近的那个」，**两台相机各用各的**、一处不省 ✅（判据 = `TargetReticleController__UpdateTrail.c`）。

---

## 三、判据：以后新写「取场上某个点」的代码，怎么一眼判该用哪个口

**先答一句：这段代码画出来的东西，归哪台相机？**
（判法：看它的层。层 = `ArenaSlots.ArenaLayer`(8) ⇒ 透视 `BoardCamera`；**其余所有层（含 Default）⇒ HUD 正交相机** —— `boardCam.cullingMask = 1<<8`，HUD 相机 `&= ~(1<<8)`，`Editor/BattleScene.cs:23065 / 23142`）

| 消费方归谁 | 该喂什么 | 口 |
|---|---|---|
| **透视 BoardCamera**（3D 战场物件、卡自己的子物体、残骸体） | **竞技场世界坐标** | `ArenaSlots.Position / RootPosition / CardCenter`，或直接 `cardView.transform.position` |
| **HUD 正交相机**（HUD、窗口、飘字、面板、根特效） | **HUD 平面点**；若这个点是「场上某张卡」⇒ **必须投影** | `BoardLayout.DropTargetWorld(slot)`（**唯一一处投影**；非 3D 时逐位 = `SlotPosition`） |
| **音频** | 世界点（**原样**，不换算） | `cardView.transform.position` |

**三条反例（就是上面那三笔现存的账）**：

- 🧨 **反例 1 —— 拿 `BoardLayout.SlotPosition(slot)` 当「卡的位置」**：它是 **2D 兜底布局的行线点**（`BoardLayout.cs:458-461`），3D 下既不精确等于卡的项目位置、也不等于 HUD 投影点 ⇒ 低 **51~72 px**。**A337 / A359 / E 三笔全是它。**
- 🧨 **反例 2 —— 拿 `cardView.transform.position` 当「HUD 坐标」**：3D 下它是**竞技场**坐标（我方槽 0 x = **−3.37**、z = −6.655），当 HUD 坐标使 ⇒ 偏 **≈267 px**（§四·3 算式）。**F 那一笔就是它。**
  ⚠️ **但同一个量在别处是对的**（`BoardWorld` 音频 / `TargetReticle` 弧线起点 / 原版教程提示）⇒ **判据永远是「消费方在哪台相机里」，不是「这个量从哪来」。**
- 🧨 **反例 3 —— 换算函数的【入参量纲】混用**：`LayoutSpace.FromPixel/ToPixel` 收 **设计像素 1920×1080**；`LayoutSpace.ScreenToWorld` 收 **实际屏幕像素**。`Camera.WorldToScreenPoint` 给的是**实际**像素 ⇒ **只能喂 `ScreenToWorld`**（`DropTargetWorld` 就是这么做的 ✅）。**G 那一笔就是喂错了。**
  ⚠️ 这条**只在 1920×1080 下看不见**（两个量纲重合）⇒ 自检全绿、真机换分辨率才露。

---

## 四、还差什么（可执行清单）

1. **E · 特效落点（`BattleDriver.cs:12037 / :12048`）** —— 改法：`layout.SlotPosition(slot)` → `layout.DropTargetWorld(slot)`（与 A337/A359 同口，铁律 6）。⚠️ 账已在 **`A367`（挂起·等真 Play）**，但**静态上「2D 行线」一定是错的**（根特效 932/962 在 layer 0 ⇒ HUD 相机画 ⇒ 只有投影点落在卡上）⇒ 可以先把账从「挂起」拆出「改法已定」那一半。
2. **F · 攻击选择器锚点（`BattleDriver.cs:8668` / `AttackSelector.cs:542`）** —— 改法：`BoardViewAt(slot).transform.position` → `playerBoard/enemyBoard.DropTargetWorld(slot)`（按 `side` 选棋盘）。判据 = `UnitOnBoardAttackTypeSelector__Toggle.c:120-128` 亲读。⚠️ 连带：`Editor/BattleScene.cs:5472-5476` 那条断言（`sel.AnchorWorld == selUnit.transform.position`）**把错口径钉死了**，要同批改成「`AnchorWorld == DropTargetWorld(slot)`」+ 补一条**灭自证**（3D 下两者可分）。
3. **G · `PlaceMarkers` 量纲（`BoardLayout.cs:293`）** —— 改法：`LayoutSpace.FromPixel(msp.x, msp.y)` → `LayoutSpace.ScreenToWorld(new Vector2(msp.x, msp.y))`（与 `DropTargetWorld:513` 同一支）。⚠️ `Editor/*` 里若有断言比 `SlotMarkerWorldPos` 的**绝对值**，同批核。**本件没跑，改完要复核截图**。
4. **H · `DropTargetWorld` 督军分支** —— 要真 Play/实拍定「督军位该对齐 HeroScale 还是 CardScale」，别照猜改（WB3 §五·3 明确要求裁）。
5. **1 / 1b · 出牌飞行的形状** —— ① 中段抬升照原版 `minionConversionHeight`（我 1.0 / 敌 2.1、**竞技场单位**，换算见 §三 反例 2）② 补**落下 0.2 s + 落地前 0.05 s** ③ 补 `shakeWhenLand`。⚠️ `DeploySequence` 现在只有两段；加第三段要**同时**改 `Editor/BattleScene.cs:12087` 那条「序列真实长度 == 0.3」的断言（否则它先红）。
6. **8 / 9 · 进墓地、开窗动画** —— 各是一条独立账（后者已在 `T04`）。
7. **文档订正（铁律 5）** —— `Core/CardFeel.cs:1517-1519` 那句「✅ 这一条已经接上了」与实据相反（全仓零消费），改成「**未接**」。

---

## 五、没查清 / 判不了的（⛔ 不猜）

1. **F 那一笔没实跑**。它是**读码 + 反编译 + 逐值推算**得的（原版那一处**显式换算**是硬判据；我们这一步的像素偏差 ≈267 px 是**算术**）。**一行可验**：进游戏点自己一个场上单位 ⇒ 看那条选择器条**是不是挂在那张卡上**（`资料/真Play待验清单.md` D20/D24 那一族）。⚠️ 若实机看着是对的，**先量两台相机**再改结论，别直接把这一笔当已证。
2. **战术卡那一档的位移参数没查**（§一 #6）—— 未读 `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c` 的 spell 分支。
3. ~~**`RevealStartPx/EndPx`、`RevealEndScale`**（`BattleDriver.cs:5347-5358`）—— 原版基准来自 `GetOffensiveCardPlayedDisplayPosition` / `GetEnemyCardPlayedDisplayScale` 依赖的**两个静态对象**与 `BattleManager+0xd8`，**本地解不出**（已如实标，本件未复核）。~~
   🔴 **2026-10-24 订正（铁律 5）：实际是【已解出】—— `RevealEndScale` 的基准 = `UnityEngine.Vector3.one`，返回值是【绝对 localScale】**（不是「场上值 × 某倍率」）。`DAT_1842da2a8` **就是 `UnityEngine.Vector3` 的 `Il2CppClass`**，`static_fields + 0xC` = `oneVector`（静态字段表 `zero 0x0 / one 0xC / up 0x18 / …`；类身份三条独立反证 → `资料/普查产出_1011/审查_W1.md:113`）；同一函数再 × `globalVars+0x48` = **1.4**（`CardScript__GetEnemyCardPlayedDisplayScale.c:26-31`）。同一条链的起手那半也在：原版 `BattleManager__DisplayRevealedCard.c:47-53` 建完卡立刻 `set_localScale(基准 × 0.01)`，终点 = `DOScale(GetEnemyCardPlayedDisplayScale(), 0.3)` + `OutSine`（`CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c:58-61`）⇒ **起手 = `Vector3.one × 0.01`、终点 = 绝对 1.4**，一始一终同量纲。**错因**：把 `DAT_1842da2a8` 当成「某个依赖静态对象 / `BattleManager` 上的字段」而没解它的类身份 —— 它其实是 `UnityEngine.Vector3` 本身。**仍未解的只剩** `RevealStartPx/EndPx`（那 −25/30 与 `enemyHand` 落到我们布局哪个像素）与落点位置（原版世界零落屏哪一处）。判据 → `资料/普查产出_1024/D6_进攻卡展示缩放.md` §一 / §2·1 / §2·4 / §2·5。
4. **敌行的投影差幅值没量到**（WB4 §五·2 同一条仍成立）：我引的 51~72 px / 0.73 世界单位全是**玩家行**的实测。
5. **`_band`（I）在非 16:9 下**是否也可能露出来（alpha=0 ⇒ 理论上不能）—— 没跑、没验，只登记。
6. **本件搜过的词/目录（自证不是「没找到」）**：`SlotPosition(` · `DropTargetWorld` · `LayoutSpace.ToWorld|FromPixel|ScreenToWorld|ToPixel|ToDesignPixel` · `ArenaSlots.CardCenter|RootPosition|Position` · `BoardViewAt(` · `transform.position`（非 Editor 全部）· `DOMove|DOLocalMove|ToPose(` · `CardEffects.Fire|FireEvent` · `WorldToScreenPoint|ScreenToWorldPoint|ScreenPointToRay` ·
   反编译目录 `d:/2/tools/decomp_full/` 的 `Get2DWorldPosFromBoardPos` / `MoveCrosshair` / `PrepareMovement` / `UpdateTrail` / `UnitOnBoardAttackTypeSelector__Toggle` / `TutorialTipScript._ShowTip` / `AiScripted__GetTipPosition`（每个引用前**首行函数名逐份核过**）·
   文档 `资料/普查产出_1011/WB3_A337.md` · `WB4_A337_A359.md` · `资料/普查产出_1013/A表现核_块3.md` · `资料/已知的坑.md` · `资料/战斗复刻_正本.md`（grep 命中处）· `资料/任务线/战斗线/任务总表.md`（T02/T04/T05/T15 行）。
7. **本件没查的**：`A367` 的 `_tmp_view` 实拍、`Battle.unity` 里两台相机的**实际** `pixelRect`、非 16:9 下的 `BoardFramer` 行为。
