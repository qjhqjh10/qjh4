# B1 · `Editor/BattleScene.cs` 九条红（#1–#9）落地报告

> 写手代理 · 2026-10-14 · **只改了一个文件**：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`
> （⛔ 未跑 Unity · 未动 git · 未改任何正本 / 资料文档 —— 本文件是我唯一写的 .md）
> 判据（唯一来源）：`资料/普查产出_1014/清单_90条红改法.md` §一 第 1 组 + `资料/普查产出_1013/D1013_诊断_块1_Battle与Deck.md` 逐条小节。
> ✅ **秒级类型检查**：`TMPDIR=/tmp/wf_b1 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑过两次，末次在全改完之后）。
> ✅ **行尾**：改前/改后都是**纯 CRLF**（`b'\r\n'` 13260 = `b'\n'` 13260）；`git diff --numstat` = **1348 / 1257**（远小于 13260 ⇒ 没翻行尾）。
> 📌 下面行号 = **改完之后**的现读行号（会漂，按内容定位）。

---

## 一、逐条（#1–#9）

| # | 改了什么（文件:行） | 判据（为什么新期望是对的） | 做完没有 |
|---|---|---|---|
| **1** | `Editor/BattleScene.cs:1561-1575`：在 darkangels 的 **targets 循环**里、`dkMove++` **之前**补一段与 `adds` 循环同形的 `dkAnim` / `dkBad` 计数（`if (t.animation != 0) { …GetComponent<Animation>()… }`）。**没有**动 `dkAnim >= 2`（`:1625` 原样） | ① 计数口径本来就是**三张表之和**：`工具/gen_arena_groups.py:651-653` 的 `_stats.animation = Σ(nodes) + Σ(targets) + Σ(adds)`（**现读该脚本确认**）。② 本轮数据侧把 `Directional Light`（`animation = 1`）从 `adds[]` 挪进 `targets[]` —— 现读 `arenas/battlearenadarkangels/battlearenadarkangels_groups.json`：nodes 17（带 animation **1** 个 = `Battle Arena Dark Angels baked`）· targets 84（带 animation **1** 个 = `Directional Light`）· adds **0** ⇒ 补完 `dkAnim` 恰 **2**、`dkBad` 仍 0。③ **独立实据**：现读 `Resources/ArenaPrefabs/battlearenadarkangels.prefab`（文本 YAML）里 classID `111`（`Animation`）**恰好 2 个**，分别在 `Directional Light` 与 `Battle Arena Dark Angels baked` 上 ⇒ 与旁挂逐条对上，**不是**「改成实现当前值」 | ✅ |
| **2** | `:11044-11047` 加 `int[] wantReused = { 3, 4, 1 };` + `totalReused`；`:11051-11080` arena2：改读 `nodesA + reusedA == wantNodes[0]`（耐改的和式），**另单配一条** `Check(nodesA == 0 && reusedA == wantReused[0])`；文案同步 | 现读 `Resources/EnvBlendables.json` 的 `sceneStandaloneBuild[battlearena2].nodes` = **3 条**（`Scenario` / `Scenario/Battle Arena 2 Particles` / `…/RocketTrail`）；现读 `battlearena2.prefab`：这 3 个叶子名**各恰好 1 份、都在树里** ⇒ 走「复用」支 ⇒ 新建 **0** / 复用 **3**。旧期望「新建 3」描述的是**prefab 还没烘进这些节点**的旧状态（`battle.log:39421` 实得本来就是「新建节点 0 · 复用 3」） | ✅ |
| **3** | `:11045` `wantReused[1] = 4` + `:11122-11132` 循环体里的和式 `nodes + reused == wantNodes[i]` + 单配一条 `nodes == 0 && reused == wantReused[i]` | `sceneStandaloneBuild[battlearena3].nodes` = **4**（`Scenario` / `Particle Effects` / `Lightning_Green` / `Lightning_Green (1)`）；现读 `battlearena3.prefab`：4 个叶子名全在（各 1 份）⇒ 新建 0 / 复用 4（与 `battle.log:39553` 实得「新建节点 0」一致） | ✅ |
| **4** | 同上（循环体一改治两条）：`wantReused[2] = 1` | `sceneStandaloneBuild[battlearenatauviorla].nodes` = **1**（`Scenario/…/Railgun BIG (1)/Big Gun Effect`）；现读 `battlearenatauviorla.prefab`：该叶子在（1 份）⇒ 新建 0 / 复用 1（与 `battle.log:39639` 实得一致） | ✅ |
| **5** | `:11152-11156` 合计那条：`totalNodes == 8` → **`totalNodes == 0 && totalReused == 8`**，文案写明「都烘在 prefab 里」 | 合计 8 = 3 + 4 + 1（#2–#4）；三场的分组节点都已烘进各自 prefab ⇒ 三场新建都是 0（`battle.log:39663` 实得「新建节点 0（8）」） | ✅ |
| **6** | `:11193` 起：**删掉夹具那三行**（`new GameObject("Scenario")` + `SetParent` + `localPosition = (1,2,3)`），改为 `var pre = probeA.transform.Find("Scenario")` + 一条**前提断言**（少了它下面那条等于没验）；`:11207-11217` 期望改 **3 / 0 / 1 / 1 / 5 / 0**；`:11218` 那条判据改成**调用前后 pre 的 `localPosition` 逐位不变**（`preLp` 基线 + `sqrMagnitude < 1e-8f`）；注释块 `:11178-11185` 同步订正 | ① 现读 `battlearena2.prefab`：根级**恰好 1 个**叫 `Scenario` 的子件 ⇒ 没有任何「两份同名」。② 旧夹具摆的那颗与 prefab 自带的**同名同父** ⇒ 会造出**生产路径不会出现**的坏状态（`SceneResolver` 是「名字 + 最近位置」）；`battle.log:39748` 实得「场根底下叫 `Scenario` 的子件 **2**」正是这么来的。③ 「复用不改写」的**语义没变**，只是判据对象换成 prefab 自带的**那一颗**（旧的 `(1,2,3)` 那颗已不存在） | ✅ |
| **7** | `:11267` 检测串 `Contains("已经建过")` → **`Contains("这个战场实例已经建过")`** | 真谎报那句的**原话**在 `Battle/ScenarioBlendables.cs:2256`（`**这个战场实例已经建过** ⇒ 不重复建`）；而「一条都没建」那条警告的**正文自己**含「再进来时谎报「已经建过」」（`:2274-2278`）⇒ 旧串命中的是**说明文字**（自撞）。新串两处可分、且**不弱化**（它钉的仍是「谎报」那条消息本身） | ✅ |
| **8** | ① `:10260-10268` A422 rig 的 `finally` 里补摘静态订阅：`rigCzRef.battleCameraScreenSize.UnregisterResolutionSignal()`（在 `DestroyImmediate(rigGo)` **之前**）；② `:11604` 在「建 rig 之前」记 `int sigBefore463 = ResolutionSignalSubscriberCountForTest`；③ `:11810` 收工断 **`== sigBefore463`**（文案写清「场上还有主战场那台合法订阅者」），**没有**写死 `<= 2` | ① 泄漏是**真**的：`CombatCameraZoom.Initialize()` 会注册那条静态信号（调用点 `Battle/CombatCameraZoom.cs:360` → `BattleCameraSreenSize.cs:172`），而编辑模式**不派** `OnDestroy`（`BattleCameraSreenSize.cs:440` 那句 `OnDestroy` 走不到）⇒ `battle.log:40233` 那条 `…两个都 没接上` 就是它。② `== 0` 的前提**不成立**：主战场那台由 `gameObject.AddComponent` 加在 **`sceneRoot`** 上（`Battle/BattleDriver.cs:6125`），**不随** `DestroyImmediate(driver)`（销毁的是组件）消失 ⇒ 恒 ≥ 1；实得 2 = 它 + A422 泄漏。③ 增量断法**不依赖**「那 2 个订阅者分别是谁」（D1013 §四·2 说逐实例身份没坐实） | ✅ |
| **9** | **整段搬家**：`Editor/BattleScene.cs` 里 A462/A658 那一段（原 `:11247-11725`，**479 行**）整体搬到 `driver.Begin("Ultramarines","Goff",20260915);` + `if (driver.InMulligan) driver.SimulateMulliganDone();` **之后**（新位置 `:10434-10918`，块前加 5 行说明）；搬走处只剩 A463 段收尾。搬法 = python `wb`（用 Edit 逐字对齐 479 行不现实）—— **先在变量里算完、验过才写**（备份留 `%TEMP%/Block1_BattleScene.cs.bak`），并自证：搬过去的块**逐字节相同**（sha256）、总行数守恒、纯 CRLF 未翻 | 根因 = **段落位置**：那一段要的是**活着、HUD 已建**的 driver，而它原来落在 `DestroyImmediate(driver)`（`:10951`）**之后** ⇒ `Object.FindObjectOfType<BattleDriver>()` 恒 null，`battle.log:40342` 只报出「（前提）场上有一台 `BattleDriver`」失败 ⇒ **整段一条都没验**。新落点 = `Begin` + `SimulateMulliganDone` 之后（driver 活着、HUD 已建、开局已走完）。**复核 `hookedBeforeB`**：A462 全段**无任何 `Hook*` / `VFX` 调用**（逐行 grep：只有 `FindObjectOfType` 与 6 处 `RefreshAll()`），而静态钩子的挂点只在 `Begin()` 里（`BattleDriver.cs:2049-2053`）⇒ A388(b) 的 `hookedBeforeB` / A515 的 `hookedBeforeB == hookedBefore388` **读数不受影响** | ✅（改完；**验收要跑 Unity** —— 见下「判不了的」） |

**改完的 9 条之外，同文件里还有两处「就地订正」**（都属于**我改的那两段的同一处注释**，不改就会与我刚写的新期望**自相矛盾** —— 铁律 5）：

- `:10996-10999` A431 段头：删掉「跑完 A418 那三步 ⇒ `battlearena2` 的 `reparent[]` 会 **5 → 6**，否则 A431 会因为**另一个原因**再红一次」那段**已失效的预告**（现读 `EnvBlendables.json`：三场 `reparent` 仍是 **5 / 0 / 0**），换成本轮**真实**的原因（prefab 重建 ⇒ `nodes[]` 走复用支）；并把「新建几个节点」那格的说法改成「`nodes[]` 几条」。**出处**：D1013 §五·3 明确建议「连同第 2~5 条的期望值一起订正」。
- `:11002-11005`：按 D1013 §五·5 / 清单 §二 A806 附带 ② 补了一句**注释纪律**「本轮收尾这几段（A431 / A514 / A463）不许依赖 `driver`」——它就在 `DestroyImmediate(driver)`（`:10951`）那一段的后面，正是 #9 踩的坑。（纯注释，零行为影响；该条本来归 A806 的「附带」，但 A806 在白名单外、⛔ 碰不了本文件。）

---

## 二、顺手发现（⛔ 没改）

1. 🔴 **tauviorla 那一半的 `Animation` 计数仍是两张表**（D1013 §1 末尾 + §五·4 建议统一口径）：`tvAnim` 只数 nodes（2：`Railgun Turret 1/2`）+ adds（0），**不数 targets**（1：`Railgun BIG (1)`）。
   **我现读 prefab 已把它证死**：`battlearenatauviorla.prefab` 里 classID `111` **恰好 3 个** —— 在 `Railgun BIG (1)` / `Railgun Turret 1` / `Railgun Turret 2` 上 ⇒ 补上 targets 计数后 `tvAnim` **2 → 3**（断言是 `>= 2`，仍绿）、`tvBad` 仍 **0**、`tvNode` 不变 ⇒ **安全**。
   我**没做**：清单 #1 的最小改法只点名了 `dkAnim`/`dkBad`（darkangels 半边），tau 那半边属超出指派面。⚠️ 若调度台决定补，**文案那半句「= 两个炮塔节点」（`:1698`）要一起改成「= 两个炮塔节点 + `Railgun BIG (1)`」**，否则文案与数字又对不上。
2. 🔴 **A462/A658 那 ~40 条断言本轮是「第一次真的执行」**：搬之前 driver 恒 null ⇒ 整段被 `if (wb4 != null)` 跳过，只有那条前提断言报红。搬完它们才第一次跑起来（含 `FreeSlot` / `CardByName(StarterCards.Tide(), "Ballista")` / `SimulateOpenCommand` 这些**依赖真 `ctx`** 的路）⇒ **重跑 `BattleScene.Run` 才知道它们绿不绿**，不能从旧日志外推。
3. ⚠️ **过期注释（未改）**：`:11815` 那句 `Debug.Log(P + "--- A463 段结束（下面还有别的段）---")` —— 它下面已经没有别的段了（A462 已搬走，紧跟的是外层块的 `}`）。纯日志文案，改不改都不影响断言。
4. ℹ️ **A514 ① 的「复用不改写」判据**按清单换成「调用前后 prefab 那颗的位姿逐位不变」后，**有一格变弱**（已在代码注释里如实标）：prefab 那颗 `Scenario` 的 `localPosition` 与旁挂值**本来就一致** ⇒ 若实现错误地把旁挂值**写回**，这条**也会绿**（旧夹具的 `(1,2,3)` 特意与旁挂不同，正是为了挡这一格）。要挡回来得让「树里的值 ≠ 旁挂值」——那要么再造一份同名（清单明确不推荐），要么挪动 prefab 那颗（有改变 `SceneResolver`「最近位置」命中的风险）⇒ **我没动**，如实报。
5. ℹ️ `BattleCameraSreenSize` **自己是有 `OnDestroy → UnregisterResolutionSignal()` 的**（`BattleCameraSreenSize.cs:440`），泄漏能长期存在只因**编辑模式不派生命周期消息**（本仓既有环境事实）⇒ 显式摘（#8 ①）才是唯一有效手段。
6. ℹ️ D1013 §四·1 那条「`Embers` 到底还是不是被闸门挡着」我**没有新证据**：只读到 `EnvBlendables.json` 里 `battlearena2.reparent` 仍是 **5**（与 D1013 一致），没去重跑 `gen_env_blendables.py`。

---

## 三、没做完的 / 判不了的（如实列，⛔ 没猜）

1. **#9 的验收判不了**（要跑 Unity）：搬家本身已自证（块逐字节相同、类型检查 0 错、结构自洽 —— `driver.Begin` → 本段 → `hookedBeforeB` → A515 → `DestroyImmediate(driver)` → A431 → A514 → A463 → 收尾），但**那 ~40 条断言实际绿不绿只能靠 `BattleScene.Run` 收口跑**。
2. **#8 的「那 2 个订阅者的逐实例身份」没坐实**（D1013 §四·2 同）：我用的是**增量**断法（进段前 vs 收工），**不依赖**「谁是泄漏的那一个」⇒ 不影响本条落地。
3. **A514 ① 那一格变弱**（见「顺手发现 4」）：清单给的判据本身如此，未加新夹具；**如实标，未猜**。
4. **没有跑 Unity、没有跑 `BattleScene.Run`**（白名单外）；**没有复核**任何断言在批处理下的实得值 —— 本报告里所有「实得」都引自 `d:/4/_tmp_view/battle.log`（诊断用过的同一份）。
