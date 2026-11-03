# B124-W10 交回报告 · `BattleScene` 源级断言 17 条 + 3D 挡板夹具 11 条 + 拖拽阈值 3 条新断言

> 执行写手 `B124-W10`（2026-11-03）。**未跑 Unity**（红线）· **未动 git** · **未跑 `--write/--fix`**。
> 只跑过：`TMPDIR=/tmp/wf_b124w10 bash 工具/typecheck.sh`（**运行时 0 / 编辑器 0**）与只读闸 `check_ledger_points.py`（全 20 张表 **漂移 0 / 失败 0**）。

## 〇 一句话

`BattleScene.cs` **12 改 12 删 ⇒ 行数零变化（51,427 行，全 CRLF，裸 CR 0）**；另动 5 个断言/夹具文件（共 +4 行，均加在方法体内单行末尾）。这 5 个文件在账里**只有文件名、没有行号锚点** ⇒ 不产生漂移。新增 `Editor/B124_W10_Assert.cs`（6 条），挂在 `BattleScene.cs:47035` 的**方法尾聚合行**（同一物理行追加 ⇒ 零行数变化）。

---

## A · 源级字面 / 计数断言（报告 #15）

| # | 文件:行号 | 改前 → 改后 | 判据出处 | 行数 |
|---|---|---|---|---|
| A1 | `Editor/BattleScene.cs:1861` | 找 `"PlayHitFrameShake(tgtView);"` → 找 `"PlayHitFrameShake(tgtView, atkView);"` | `Battle/BattleDriver.cs` 现读调用点已是两参（2026-11-02 `B120-W2b` 照原版：震屏方向取**攻击方**）；旧串全库 0 命中。**一行修 3 条**：`:1866`（前提 iShake>0）、`:1869`（震屏→音→粒子→后坐次序）、`:1901`（`shakeLine` 方法体/无早退） | ±0 |
| A2 | `Editor/BattleScene.cs:2233` | `!srcR19.Contains("if (recoilBeat) CardFeel.ShakeCameraForHit(")` → **先 `Regex.Replace(src, @"//[^\r\n]*", "")` 剥注释**再上同一个负判据 | 该串现读**只出现在** `BattleDriver.cs:25467` 的说明注释里（注释在解释「已经删掉了」）⇒ 不剥注释 = 恒红（误命中） | ±0 |
| A3 | `Editor/BattleScene.cs:1311` | 灭自证阈值 `> 0.5f` → `> 0.3f`；文案 `1.3` → `0.75` | `Core/EventTiming.cs:633` `DurationOf(EvtKind.Deploy) = DeploySummonHold(0.45) + DeployLandHold(0.3) = 0.75`（2026-11-02 `B120-W10` 由 1.0→0.45）；`:963` 手牌档 = `0.4` ⇒ 差 **0.35**，原 `> 0.5f` 已不可能成立。**灭自证性质不变**：删掉 `DurationOf(BattleEvent)` 的 Deploy 分支 ⇒ 手牌档变 0.75 = 兜底档 ⇒ 差 0 | ±0 |
| A4 | `Editor/BattleScene.cs:2684-2687` | 期望 `CardFeel.DealDuration / DealDurationFoe`（0.3 / 0.15）→ `EventTiming.DrawBeatSeconds(true) / (false)`（**0.5 / 0.35**） | `Core/EventTiming.cs:759` 的 Draw 分支走 `DrawDealOf`；`:1071-1074` `DrawBeatSeconds = CardTween.RelayoutDuration(0.2) + CardFeel.DealSeconds`；`:1036` 注释的判据 = `BattleManager._ResolveDrawCard_d__432__MoveNext.c:233-249`（`WaitForSeconds(fVar2 + fVar11)`）。**用函数不用字面量** ⇒ 不另造一份判据。**两条一起改** | ±0 |
| A5 | `Editor/BattleScene.cs:16401-16402` | 拖空松手夹具：`offBoard = LayoutSpace.ToWorld(0.5f, 1.6f)`（屏幕上方 11 世界单位）→ `offBoard = mv.transform.position + new Vector3(1.6f, -0.2f, 0f)`（**手牌带内、横向挪开**） | ① `Hand/CardInteraction.cs:2617-2626` `TryGiveUpTooHigh` 三条前置（还在拖 / `!dropOk` / **`IsInPlayArea`** / 高出按下点 > 3.434）—— 手牌带内 `IsInPlayArea`=假（`:2465` 比 `HandLimitY`=0.278，手牌心 0.122）⇒ 不进放弃闸；② `:2349-2353` `PlayCommitGate` 的 ② 要求指针越过 `HandLimitY` ⇒ **永不出牌**，`Release` 把它判 `MissedSlot`；③ `BattleDriver.cs:13300` 只有 `!IsIllegalAction(kind)` 才播 `Battle/Tips/DragToTarget` ⇒ `MissedSlot` 才走提示链（正是本条要测的）；④ 横向 1.6 >> `TapThreshold`(10 px, `:110`) ⇒ 不算轻点 | ±0 |
| A6 | `Editor/B120_W17_Assert.cs:232` | `drain.Contains("Play(p.clip")` → `drain.Contains("(p.clip")` | `Battle/BattleSfx.cs:405` 现读 `PlayLegacyOnMaster(p.clip, p.volume, p.pitch)`（2026-11-03 `B120-W29` · F22 改走原版 `BattleManager.audioSource`）⇒ 再钉死播放器函数名会一直漂。语义（播的是**载荷里的 clip**、`DrainOne` 里**没有** `PickClip`）一条没松 | +1 |
| A7 | `Editor/B120_W21_Assert.cs:456` | `nSide == 9` → `nSide >= 9 && (nSide - 1) % 2 == 0` | 现读 `Hand/CardInteraction.cs` 的 `SideCount(` = **11**（`:1301/1304/1329/1336` 各 2 + `:2572` 2 + `:2424` 1 处定义；`B120-Q04c4b` 之后多了第 5 处解析）。**不钉死数字**：成对出现 + 下限 ⇒ 挡「删任一处（变 7/5/3）」，也不会因新增解析点假红 | +2 |
| A8 | `Editor/B99B1_Assert.cs:339` | `Contains("void Release(Vector3 world)")` → `Contains("void Release(Vector3 world")` | `Hand/CardInteraction.cs:1380` 现签名 = `void Release(Vector3 world, bool byCommitWhileHolding = false)`（`B120-W16`）⇒ 带右括号旧串 0 命中。⑦1-3 三处下标实测全部成立：`iSet < iClear`、`iReset > iClear`、`nTacticAssign == 0`、`nAssignFalse >= 3` | ±0 |
| A9 | `Editor/B120_W13_Assert.cs:124` | 期望串 `… out raySlot); return raySlot; }` → `… out raySlot, out _); return raySlot; }` | `Battle/BattleDriver.cs:33320` 现读 `TryHitSlotByBoardRay(views, world, out raySlot, out _)` | +1 |
| A10 | `RuleEngine/Editor/B118W1_Assert.cs:365/368` | **未做** | 该文件按简报「`B118W*` 归 W7」⇒ 不碰。请调度台转 W7（判据：`BattleDriver.cs:30940-30943` 已改判 `EvtKind.Fatigue`，`:19700 t += EventTiming.DurationOf(e)`） | — |

> ⚠️ **A6/A7/A9 三个文件不在 `文件所有权_当前.md` 的 B124 登记里**（报告 #15 把它们列在本写手名下，简报也写「按 #15 表逐条处理」）⇒ 已改，并在本报告点名落盘。若调度台另有归属请覆盖。

---

## B · δ 夹具：3D 挡板 collider 取不到（11 条）

| 文件:行号 | 改前 → 改后 | 判据出处 |
|---|---|---|
| `Editor/B118_T11_Assert.cs:74` | `CardData.Simple("TapBlockProbe",3,3,4)` → 同一行追加 `data.faction = "Ultramarines";` | 链：`Core/CardView.cs:109-118` `Simple()` **硬写 `faction = null`** ⇒ `Core/CardArt.cs:73` `Frame()` 空阵营**直接 return null** ⇒ `Core/CardView.cs:4323` `if (frameTex != null)` 整段跳过 ⇒ `BuildBody3D` / `EnsureTapBlockCollider` 从没跑 ⇒ `_bodyCollider3D` 恒 null（报告 #7）。补非空阵营即可（`Frame` 只判空、不校验阵营名）；`Resources/Art/cards/frame_ultramarines.png` 现读存在 ⇒ 一定取得到框 |
| `Editor/B120_W13_Assert.cs:167`（B 组 `W13Probe`） | 同上追加 `data.faction = "Ultramarines";` | 同上 —— 修 `B0b` 前提（挡板取不到） |
| `Editor/B120_W13_Assert.cs:245`（C 组 `W13MultiProbe`） | 同上 | 同上 —— 修 `C0` 前提 |

**硬前提写法**：三个夹具原本就是「先 `Check(前提)`、再 `Check(结论)`」两条独立断言（`if (col == null) return;` 之前那句 `Check` 已经红过）⇒ **不存在 `if (前提) Check(结论)` 的静默跳过**，本次未再改（新增的 `B124_W10_Assert` 也照这条写，见 §C）。

---

## C · 新增：`pixelDragThreshold = 5`（`B120-A13`）—— `Editor/B124_W10_Assert.cs`（新文件，120 行）

**为什么必须加**：`B124-W4` 已在 `Ensure()` 的**三条路**（① 收养 `EventSystem.current` / ② 收养场景里那只 / ③ 现建）各补了 `pixelDragThreshold = (int)AttackSelector.PopDragThresholdPx`，但**没有任何断言验过它** —— 不动这条，2/3 的修复（两条收养路）永远没被走过。

**判据**：① 原版 `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity:5429` `m_DragThreshold: 5`（`decomp_full` 0 处读写 ⇒ 奔跑值就是 5）；② uGUI 默认 **10**：本机包源码 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventSystem.cs:68` `private int m_DragThreshold = 10;`；③ 唯一出处 `Battle/AttackSelector.cs:252` `PopDragThresholdPx = 5f`。

| ID | 断言 | 🧨 灭自证 |
|---|---|---|
| A0 | （硬前提）`Ensure()` 现建出那只 `EventSystem` | — |
| A | `Ensure()` 之后 `Instance.pixelDragThreshold == 5` | 删掉 ③ 那句赋值 ⇒ 得 uGUI 默认 10 ⇒ 红 |
| A2 | `pixelDragThreshold < 10` | 与 A **结构上不可能同时被写反满足**：写 `=10` ⇒ A 红；写 `=12` ⇒ A2 红；三处全删 ⇒ A 红 |
| B0 | （硬前提）外部现建那只此刻 **== 10** | 前提不成立 ⇒ B 没有观测面（拆成独立 `Check`，⛔ 不用 `if(前提)Check(结论)`） |
| B0b | （硬前提）它此刻就是 `EventSystem.current` | 否则 `Ensure()` 走 ②/③ 而不是 ① 收养 |
| B | **收养路**：`Ensure()` 收养了它、且把它从 10 **改成 5**；`ReferenceEquals(Instance, 它)` | 删掉 `Ensure()` ①/② 那两句赋值 ⇒ 仍旧 10 ⇒ 红 |

**零前提**：自带临时 `EventSystem`（`ResetForTest()` → `DestroyImmediate`），开跑前先清掉场景里**残留的**任何 `EventSystem`（会出声，⛔ 不静默 —— 否则 `Ensure()` 先收养它，"现建/收养"两条路的观测面都不成立）。
**接线**：`BattleScene.cs:47035` 的方法尾聚合行（`B123_C4_Assert` 之后、`Debug.Log(... 合计 ...)` 之前）**同一物理行追加** ⇒ 行数零变化。

---

## 行数变化 / 校验

| 文件 | numstat（+/−） | 净 |
|---|---|---|
| `Editor/BattleScene.cs` | 32 / 32 | **0**（51,427 行；CRLF 51,427 / 裸 CR 0） |
| `Editor/B118_T11_Assert.cs` | 1 / 1 | 0 |
| `Editor/B99B1_Assert.cs` | 1 / 1 | 0 |
| `Editor/B120_W13_Assert.cs` | 4 / 3 | +1 |
| `Editor/B120_W17_Assert.cs` | 4 / 3 | +1 |
| `Editor/B120_W21_Assert.cs` | 5 / 3 | +2 |
| `Editor/B124_W10_Assert.cs` | 新文件 120 行 | — |

**闸**：`check_ledger_points.py`（只读、不带 `--write/--fix`）⇒ 20 张表 **通过 100% · 漂移 0 · 失败 0**。
**typecheck**：`TMPDIR=/tmp/wf_b124w10 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑了两次，第二次在全部改完之后）。
**未跑 Unity** ⇒ 上述 28 条的**绿红待同步点复跑确认**；A5 那条夹具是行为面，只能靠 `BattleScene.Run` 现读 `[CardPresentation] 落点不合法（…MissedSlot…）` 那一行来证。

---

## D · R4 已判死的 8 条「提交后未结算」夹具（补派 · 2026-11-03）

**根因（R4 + 调度台定）**：夹具先 `SimulateHover` 再读 `view.transform.position` 当按下点；而**悬停抬牌是瞬时的**（`HandLayout.animateRelayout = false`），抬到 y ≈ **−0.88**，我方棋盘行也在 −0.86~−0.89 ⇒ 这一拖只抬高 **0.02 < `CardInteraction.PlayLiftWorld` (0.0412)** ⇒ `PlayCommitGate` ① 不过 ⇒ `Release` 判 **`MissedSlot`**。

| 落点 | 改法 | 判据出处 | 行数 |
|---|---|---|---|
| `BattleScene.cs:16321/16325`（§7 单位拖上场） | 取 `pressPos = view.transform.position` **在 hover 之前**，`SimulateHover/SimulatePress` 都改用它 | `Hand/CardInteraction.cs:2349-2353`（`PlayCommitGate` ① = 抬高 ≥ `PlayLiftWorld` 0.3 原版世界单位 = 4.45 px）· `:2345` `PlayLiftWorld` | ±0（并把 `pressPos` 并到上一行的行尾） |
| `BattleScene.cs:19127/19131-19132`（§10 战术卡） | 同上 | 同上 | ±0 |
| `BattleScene.cs:48817/48820-48821`（`DriveTacticAndCheck` —— 造牌/部署那 4 条都走它） | 同上 | 同上 | ±0 |
| `BattleScene.cs:19134`（§10 结算前） | 释放后 **先等 `AwaitingPlayTarget` 成立**再 `driver.SimulatePlayTargetPickForTest(1, tgt)`（目标点定 = 卡面要打的那一格）；没人点 = 那四条永远等不到结算 | 落点解析走的是**部署候选格**（`GetClosestAvailableSlot` 候选集**不含督军位**）⇒ 打敌方督军时解析成 5 而目标是 4 ⇒ 驱动层按原版 `UIstate == 9` 进「等点目标」态（`BattleDriver.cs:8035` / `:8419` `SimulatePlayTargetPickForTest`） | ±0（并到 `SimulateRelease` 那一物理行） |
| `BattleScene.cs:19145` | **新增断言「落位槽 == tgt」**：`!driver.AwaitingPlayTarget && tgtUnit.Health < hpBefore` —— 点定之后退出等目标态、且效果真的落在 `tgt` 那一格；⛔ 谁把「点定的那一格」退回「落点那一格」⇒ 本条与「目标真的挨了打」同时红 | 同上（4→5 那个漂移的**专用探测条**） | ±0（并到上一条 `Check` 的同一物理行） |

> ⚠️ **落位槽的语义**（如实标）：`Release` 解出来的「落点」是**部署候选格**（不含督军位），它**不是**结算目标；结算目标由「点定」那一步决定。所以「落位槽 == tgt」断的是**结算落点**（`tgtUnit` 那一格），不是 `DropReject` 的返回值 —— 后者在打督军时**结构性**等于 5（`GetClosestAvailableSlot` 的候选集定义）。若调度台要的是后者，请告知，那条会**恒红**直到候选集口径改。

## E · W9 的两段补丁（`报告/B124_W9_落点坐标系.md` §「给 BattleScene.cs 的补丁」）

| 段 | 落点 | 改动 | 行数 |
|---|---|---|---|
| **P2**（飞牌部署闸 B2，与 D 组同形状） | `BattleScene.cs:41412-41415` | `b2press = b2view.transform.position` **在 hover 之前**取；`SimulateHover/SimulatePress` 改用它；并补**两条硬前提**：① `ToNormalized(b2press).y < BoardLayout.HandLimitY`（按下点在手牌带）② `b2drop.y - b2press.y >= CardInteraction.PlayLiftWorld`（这一拖抬高 ≥ 4.45 px）—— 前提不成立时下面那一串红得没有观测面 | **±0**（4 行 → 4 行：`ResetCounters` 与取 `b2press` 并到一行，两条前提各一行，hover/press 并到一行） |
| **P3a** | `BattleScene.cs:6697` | `pBoard.transform.TransformPoint(pBoard.DropTargetWorld(probeSlot))` → `pBoard.DropTargetWorld(probeSlot)` | ±0 |
| **P3b** | `BattleScene.cs:12647-12649` | 删 `localEmpty` + `boardRoot != null ? boardRoot.TransformPoint(...)` ⇒ `Vector3 worldEmpty = driver.playerBoard.DropTargetWorld(s);`（旧写法留在行尾注释里，铁律 5） | **±0**（3 行 → 3 行：2 行说明 + 1 行代码） |

> P3 的判据 = W9 已把 `BoardLayout.DropTargetWorld` 统一成**世界点**（内部那次 `InverseTransformPoint` 删了）⇒ 外面再来一次 `TransformPoint` 就是**双重变换**；今天板根 ≈ 单位矩阵 ⇒ 恒等、行为不变，但一改父级缩放就全偏。

## §顺手发现（不作为本批改动）
1. **`B120_W13_Assert` 的 A4 可能已过期（未改）**：它断 `TryHitSlotByBoardRay` 方法体里 **`distance` 出现 0 次**。但 `B120-W27` 已把方法体改回**字面 `_boardRayHits[0]`**、并删掉「缓冲里第一条卡」的变通 ⇒ 若新实现里出现了 `distance` 字样（例如注释或 `hit.distance`），A4 会假红 —— 复跑时看 `[B120W13]` 前缀。
2. **`BattleScene.cs:1289-1292` 的文案仍写「老口径」但没写新值**：它断 `DurationOf(EvtKind.Deploy) == DeploySummonHold + DeployLandHold`（0.75），文案只写「⛔ 生产路径不许走它」。建议下次把 0.75 写进文案（本次为守零行数变化没动）。
3. **`EventTiming.DrawDeal`（`:1036`）与 `DrawBeatSeconds`（`:1071`）现在只差一点语义**（前者只覆盖我方档，后者按座位）——`DurationOf(EvtKind.Draw)` 仍走 `DrawDeal`，`DurationOf(BattleEvent)` 走 `DrawDealOf`。两处并存是**判据**（前者给探针/旧路径），但容易被下一个会话当成重复件合并掉，建议在 `DrawDeal` 的 doc 再点一句「⛔ 别删，它是 `EvtKind` 重载的唯一出口」。
4. **`B118_T11_Assert` / `B120_W13_Assert` 的夹具用 `CardData.Simple` 是通用坑**：那份 `Simple()` 自带 `faction = null`，任何「要 3D 卡体」的夹具都得自己补阵营。若再有新夹具，建议直接在 `CardView.Simple` 上加一个可选阵营形参（属产品文件，**未动**，留给调度台裁定）。
