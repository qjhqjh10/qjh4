# AUD6 攻击与目标选择 审查（只读代理 · 2026-10-25）

**范围**：玩家怎么选近战/远程 → 怎么选敌人 → 过程中看到什么（引擎 → 驱动 → 表现）。
**判据**：`d:/2/tools/decomp_full/`（第一）· `d:/2/新解包资源/assets_full/`（第二）· 卡图（内容尺子）。
**运行时**：`_tmp_view/player/real_shell_1025.log` 那一局**没走到攻击**（只有开场/卡展窗/结束回合；`20/22/23` 三图同）⇒「选目标态」**没有实拍**，下面涉及观感的结论**全部是静态判据**，未在真机验收。

## 一、链路图（玩家视角，逐段）

1. **按住我方场上单位**。轻点（不拖）= 开/关大卡展窗（`BattleDriver.cs:13790` `ToggleUnitCard`）；敌方单位也能开窗、但不能拖（`:13798`；原版 `CardScript.OnTouchDrag` 的 `isPlayer` 闸）。
2. **拖过阈值** → `BoardPress` 判 `dragged` → `OpenCommand`。原版那一刻 = `BattleManager__StartAttackFrom.c`：`SetCardTracked` ＋ 三钮 ＋ `ToggleCrosshair(true)` ＋ `PrepareMovement(拖拽点, card.attackType)` ＋ `ShowPotentialTargets(mode = attackType==4 ? 2 : 0)`。我们有前三件＋预亮（`PreviewTargetsFromDrag`），**准星改前不跟手**（§二 #11，§四② 已修）。
3. **三钮**：近战(1)/远程(2)/主动技能(4)；入场进度 = 累计拖拽 ÷ 0.085 屏高；**悬停即选**（0.1 s 安全窗）；选中 1.3×、0.3 s OutBack；黄圈只亮已选那一格（`AttackSelector.cs:95-101/128/137`）。
4. **定档** → 写 `LastAttackType`、点亮这张卡自己的打法底光、换打法 cue、选择器收起（`CommitCommand:14267-14330`）→ 合法目标换 `ValidTarget` 绿环（`HighlightTargets:14478`）→ 准星换色（红/紫/金）并 punch。
5. **指针移动**：准星跟手、弧线终点 = 射线打「地板/敌兵两个平面」取近（`TargetReticle.ResolveAim`）；压在合法目标上 → 该卡橙环 ×1.05 ＋「这一下会打死它」图标（`SetReticleTarget:14770` / `CardView.SetWillDie:7810`）。
6. **点目标** → `Resolve`→`DoResolve`（`:14868`）→ 引擎 `DeclareAttack`/`UseAbility`；近战走位、命中在 t=0.1；远程回正 0.2 s ＋ 该件 `GetAnimDuration`。
7. **取消**：点空白/点自己人/点非法目标 → `ClearSelection`（`:15116`），非法目标按因给 tip（`DoResolve:14905`）；右键不响应、ESC 开设置窗（`BattleHud__Update.c:27`），两者**都不是取消**。

## 二、逐条差异（表）

| # | 环节 | 原版判据（file:line / 资产字段） | 我们（file:line） | 判定 | 修法 |
|---|---|---|---|---|---|
| 1 | 三钮档位语义 | `AttackTypes.cs`：None0/Melee1/Ranged2/**Spell3**/Active4；第三颗 = 主动技能(4) | `AttackSelector.cs:95-101`（0/1/2/4）；`OpenCommand:13917-13960` | 一致 | — |
| 2 | 三钮显示条件 | `UnitOnBoardAttackTypeSelector__Toggle.c`（近/远程按攻击值、技能那颗按 activeAbility；召唤病+ferocity/oath ⇒ `ToggleJustOne:163`） | 同：`FieldAttack>0` / `HasAbility`+alt+oath（`:13917-13935`）；`:13957` ShowJustOne | 一致 | — |
| 3 | 进入选目标（棋盘） | `CardScript__TryDraggingFromBoard.c` + `BattleManager__StartAttackFrom.c` | `BoardPress:13784` → `OpenCommand:13846` | 一致 | — |
| 4 | 手牌「目标型」卡选敌人 | `_ChooseUnitTarget_d__780`：打出后进 state 9，让玩家点目标 + tooltip + `ShowPotentialTargets(mode=1)`；闸 = `RawCardScript +0x268 targeted` | 我们是「把卡拖到敌方单位上松手」：`:5340-5355` → `RuleCore.PlayCard:3542` → `PlayTactic(slot)`。**没有**打出后点选、没有潜在目标点亮、没有拖回手取消（原版 `OutFromPlayingAreaAnim`） | 部分 | 修法 A |
| 5 | 近战/远程的合法目标 | `IsValidAttackTarget.c:57-86`（两档只差「攻击值>0 / <1」）；距离与行**全无限制**；飞行只挡近战（`:177-181`） | `RuleCore.IsValidTarget:4924` 同；Flying 判据 `:5096` | 一致 | — |
| 6 | 飞行 的先锋豁免 | `:177-181` 第二格：目标同时带 `vanguard(980)` ⇒ 近战也合法 | 只看 `Flying && !attacker.Flying`（`:5096`） | 缺（现池 flying∩vanguard = **0 张**） | `RuleCore.cs:5096` 加 `&& !target.Has(KeywordTable.Vanguard)` |
| 7 | 攻击路缺的六道闸 | `precognition(0x280)` 目标、`summonRift(0x2da)` 目标一律不可打；`coward(0xbe)` 攻方打不了督军；`tainted(0xa0)` 攻方是督军打不了非先锋 tainted；`stun(100)` 攻方；`pindown(0x3ca)` 近战 | `IsValidTarget` 六道**全无**（能力路已有 ward/summonRift/tainted：`CanUseAbilityTarget:10499`；pindown/stun 由 `CanAttackNow:5242` 代偿） | 缺（现池 **0 张**） | `RuleCore.cs` 在 `:5122 return OK` 前按原版顺序补六条 |
| 8 | stealth/camo/ward 分侧 | 攻击：stealth 恒挡（`:134`），camo/ward **不参与**攻击；能力/战术：stealth、ward 只挡异侧；camo **只挡 Tactic(cardType 20) 施放者**（`RawCardScript__IsValidTarget.c:30-40`） | `IsValidTarget` 只挡 stealth ✓；`EffectResolver.AddSide:2600-2612` 三道都挡且 camo 不判施放者卡类（已记「对所有来源都挡」） | 部分（口径偏严） | 行为不改；在 `AddSide` 注释写清「我们这条只有战术来源 ⇒ 与原版等价」，或按施放者卡类加闸 |
| 9 | 准星/弧线参数 | `SpriteRenderer_1754` order 0 白；`LineRenderer_1609` order 5、`m_UseWorldSpace=1`；preset 1/2/3/4（3≡4 金）；colorChangeSpeed 8；punch 0.2×0.5 s | `TargetReticle.cs:210-226/111/119/396/628` | 一致 | — |
| 10 | 状态环 / 会死图标 | `CardHighlight__ChangeState.c`：potTarget 绿 `#00FF21` ×1.05 t=0.1；selTarget 橙 `#FF8400` ×1.05 t=0.1；selected 白 t=0 不缩放。`Minion Death Icon` 1.27×1.8、`m_SortingOrder=2`、clip 0.25 s 淡入＋y 1.71→2.838 | `CardHighlight.cs:107-175`；`CardView.cs:7795-7900` | 一致（图标层排序见顺手①） | — |
| 11 | 拖拽起手期间的准星 | `BattleManager__Update.c:996-1017`（state 6）**每帧 `MoveCrosshair`** ⇒ 还没选打法时准星也跟手 | 改前：`DrivePlayerTurn:13697` 只在 `_command != None` 才调 `UpdateReticle`；`UpdateReticle:14630` 在 `_command==None` 直接 `Hide`；`ShowReticleNow:14724` 读 `_pointerWorld`，而它**只在 `UpdateReticle` 里赋值**（`:14622`）= 开局恒 `(0,0,0)` | **错 → 已修（§四②）** | 修法 B（已按此落地；新增 `★ X8-a` / `★ X8-b`） |
| 12 | 点下去 / 取消 | `Update.c:1020-1080`：点非法目标 ⇒ 分因 tip ＋ `CancelAttack` ＋ `StopTracking`；点空白/自己人**不给 tip** | `DoResolve:14905` 分因 tip ＋ 之后必 `ClearSelection`；空白/自己人静默取消 | 一致 | — |
| 13 | 拖拽阈值 | `Intro.unity:5429` `m_DragThreshold: 5`（全工程**唯一** EventSystem，战场沿用它；`pixelDragThreshold` 反编译 **0 处**读写） | 改前：`AttackSelector.cs:244` `= 10f`（注释写「按 Unity 默认 10，序列化值没搜到」）；`Editor/BattleScene.cs:9009` 也钉 10 | **错 → 已修（§四①）** | 修法 C（已按此落地） |
| 14 | 近战/远程的演出 | 近战 0.4 s、命中 t=0.1；远程回正 0.2 ＋ `AnimInfo.GetAnimDuration`（四字段和）；`MeleeEndPos` 的 dy = `anchor.y − target.y` | `EventTiming` 同（`18540-18560`）；`CardFeel.MeleeEndPos` 带 `tv`（`:23229`） | 一致 | — |
| 15 | 枚举缺 Spell=3 | `AttackTypes.Spell=3`（手牌战术那一档，准星 preset 3≡4 金） | `AttackSelector.AttackKind` 只有 0/1/2/4（`CardView.AttackTypes:142` 有 3） | 部分（今天无 UI 用到） | `AttackSelector.cs:95-101` 加 `Spell = 3`，`TryPresetOf` 归 Gold |

**修法 A（手牌目标型卡）**：① `工具/gen_cards_engine.py` 增列 `targeted`（原版 `RawCardScript +0x268`），落进 `cards_engine.json`；② `BattleDriver.BeginPlay:10113` 前若 `card.targeted` ⇒ 不直接 `DoPlay`，先进「选目标」态：复用 `HighlightTargetsCore`（mode=1 语义）+ `HitSlot(_foeUnits)` 点击 → `DoPlay(card, idx, 目标格)`；点空白 = 取消并回手（原版 `OutFromPlayingAreaAnim`）。⚠️ 依赖 `gen_cards_engine.py`（另有代理在改）⇒ 排在那批之后。

**修法 B（准星跟手）**：`BattleDriver.cs:13697` 条件改 `_selectedSlot >= 0`；`UpdateReticle:14630` 的早退只留 `_selectedSlot < 0 || 视图缺失`（去掉 `_command == AttackKind.None`，颜色/punch 由 `ApplyAttackType(None)` 天然不动色）；`ShowReticleNow:14724` 把 `_pointerWorld` 换成 `WorldPointer()` 并写回。`SetReticleTarget:14784` 的 willDie 门**保持** `_command != None`。配断言：`SimulateOpenCommand` 后拖指针 ⇒ `reticle.LastAim` 跟着变（**现在不变**，可当灭自证）。
✅ **已按此落地（§四②）**——实际还多改了两处（选择器那一支必须补 `UpdateReticle`，只放开 ③ 的闸不够；`SimulatePointerAt` 的闸同改）。

**修法 C（阈值）**：`AttackSelector.cs:244` `10f → 5f`；`:229-236` doc 改成「判据 = `Intro.unity:5429 m_DragThreshold: 5`（全工程唯一 EventSystem，战斗沿用它）」；`Editor/BattleScene.cs:9009` 断言 `10f → 5f`、文案 `:9010-9016` 与日志 `:9017` 的「10 px」同改；`BattleDriver.cs:13776-13782` 注释里的「Unity 默认 10 px」同改（用法读常量，无需动）。✅ **已落地（§四①）**。

## 三、玩家实际看到的样子（按时间顺序，一步一句）

1. 按住自己的兵**不动** → 只弹卡展窗，没有任何攻击 UI。
2. 往外拖 → 棋子之间那条带里浮出三颗钮（近战左／技能中／远程右），红色准星出现在指针处，能打的敌方单位亮**绿环**。（原版 5 px 就浮出；我们改前要 10 px，§四① 已改回 5 px。）
3. 继续拖或悬停到某颗钮 → 它放大 1.3×、外面亮黄圈，准星弹一下；换一颗 → 准星与弧线换成该打法配色（红/紫/金）。
4. 定档后目标全员绿环；指针压着的那一个变**橙**并放大 5%；若这一下打得死它，它上方浮出骷髅图标并往上飘 0.25 s。
5. 准星跟着指针走；弧线从单位拱到落点（近战拱得高、远程几乎贴地）。（我们改前**这一拍不跟手**，§四② 已修。）
6. 点下去 → 近战：单位走过去、0.1 s 命中；远程：原地一缩一放 0.2 s 再出弹道。命中后选择全收、系统光标回来。
7. 点空白／点自己人／点一个打不了的目标 → 全部收起（打不了的那个会在提示条报**具体原因**）。
8. 右键什么都不发生；ESC 开设置窗（不是取消）。

## 顺手发现（本条 ① 仍⛔未改、② 已在 §四③ 做掉；③④⑤ 未改）

1. 「会死」图标层（`CardView.cs:7848`）不进 `StampQueue`、也不写 `sortingOrder`（原版 `SpriteRenderer.m_SortingOrder=2`）⇒ 一旦卡的层队列被抬到 ≥3000，它会从「压在卡上」变成「被卡压住」。建层时补 `_willDieMr.sortingOrder = 2` 并在 `SetQueue` 那一跳盖章。**（做不了：`Core/CardView.cs` 不在本轮授权文件内）**
2. `ClearSelection:15135` 只把 `_foeUnits` 刷回 Normal；一旦 `AbilityPickOwner` 开放友方档，我方被点亮的目标会**残留绿环**。**✅ 已修（§四③）**
3. `AttackSelector.ContainsBar`（槽外取消）是自造；uGUI 档悬停区已换成 `Highlight` 的 176×176 矩形，而 `ContainsBar` 仍用手搓矩形 ⇒ 底板边缘会出现「原版会吃、我们会取消」。
4. `HitCommandTargetSlot:14226` 不看 `LegalTargetCode`，靠引擎拒 ⇒ 每次点非法目标多跑一遍 `CanAttackCard`/`DeclareAttack`（不记录像：`LocalAct:309` 只在 OK 时记）。无正确性问题。
5. `reticle.Show` 藏光标（`:628`）、`Hide` 恢复（`:469`）——若将来有 `ClearSelection` 之外的路收掉选择（如重建视图），会留下「光标一直藏着」。建议 `OnDisable` 兜底恢复。

## 四、X8 执行报告（2026-10-25 · 授权修的两件确凿的错 + 顺手②；顺手① 因文件在授权外未做）

> 📌 **行号口径**：§一 / §二 里引的是**改动前**的行号（审查当时的读数）；**改动后**的新行号只在 §四 里给。
> 本轮只碰了 `Battle/AttackSelector.cs` · `Battle/BattleDriver.cs` · `Editor/BattleScene.cs`。
> 类型检查 `TMPDIR=/tmp/wf_atk bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> ⛔ 未跑 Unity 批处理、未碰 git 写操作、未改数据与正本。
> ⚠️ 第一次跑类型检查时编辑器侧有 1 条红，在 `RuleEngine/Editor/RuleEngineTest_EngineX6.cs(100)`
> （缺 `using UnityEngine`，**别的代理当刻正在写的文件**，与我三处无关）；90 s 后复跑已 **0/0**。

### ① 拖拽阈值 10 px → 5 px

- 改：`AttackSelector.cs:252` `PopDragThresholdPx = 5f`；`:222-243`（`DragThreshold01` 的判据段）与
  `:246-251`（`PopDragThresholdPx` 的 doc）换成新判据
  （`Intro.unity:5429 m_DragThreshold: 5` · 全工程唯一 EventSystem · 战场沿用它 · `pixelDragThreshold`
  反编译 0 处读写 · 并写清旧结论「按 Unity 默认 10」的错因 = 只搜了 bundle、没搜 AssetRipper 导出的场景）；
  `BattleDriver.cs:13786-13798`（`BoardPress`）注释同步；`Editor/BattleScene.cs:9012-9021` 断言 `10f → 5f`、
  文案与日志同改。`PopDragThresholdWorld` 自动跟随（未手改第二处）。
- **原来抓不住什么**：断言把 10 钉死 ⇒ 「我们比原版多要求一倍的拖拽距离」永远绿。**现在抓住什么**：
  常量一改回 10（或改回 0.085 屏高）⇒ `★ 弹出选择器的拖拽阈值 = 5 px` 立刻红。

### ② 起手（还没定打法）准星不跟手 + 首帧瞄准点用未初始化 `_pointerWorld`

- 改（三处，按授权）：
  - `BattleDriver.cs:13666`：`DrivePlayerTurn` 选择器那一支里**补一句** `if (_selectedSlot >= 0) UpdateReticle(world);`
    —— 原来这一支处理完就 `return`，③ 永远轮不到，所以只放开 ③ 的闸**不够**。
  - `BattleDriver.cs:13709`：③ 的闸 `_selectedSlot>=0 && _command!=None` → **只留 `_selectedSlot>=0`**。
  - `BattleDriver.cs:14646`：`UpdateReticle` 的早退去掉 `_command == AttackKind.None`（只留「没选人 / 没视图」）。
  - `BattleDriver.cs:14749`：`ShowReticleNow` 由读 `_pointerWorld` 改成 `_pointerWorld = WorldPointer();`
    再喂 —— 那个字段只在 `UpdateReticle` 里赋值，原来一局第一次起手时它是 `(0,0,0)`。
  - 配套（铁律 6，同一份判据）：`BattleDriver.cs:30916` `SimulatePointerAt` 的闸也改成 `_selectedSlot>=0`
    （它是生产 ③ 的自检镜像；只改一处 = 两条口径分叉）。
  - ⚠️ **`SetReticleTarget` 里 willDie 的门仍是 `_command != None`**（授权要求，语义不同：没打法就没有伤害条目）。
- 断言（新增，`Editor/BattleScene.cs:9020-9068`）：
  - `★ X8-a`：钉死指针 A → 起手；换指针 B → 跑**生产那一帧** `DrivePlayerTurnForTest()`；`reticle.LastAim` 必须变。
  - `★ X8-b`：起手那一下的 `LastAim` 必须 ≠ `ResolveAim(Vector3.zero)`（`ResolveAim` 有副作用 ⇒ 放最后调）。
- **原来抓不住什么**：这段链**一条断言都没有** ⇒ 「准星冻在起手那一刻」与「首帧瞄到世界原点」两种状态都全绿
  （后者只在真机上表现为「弧线第一帧指向莫名其妙的方向」，一选打法就自愈 ⇒ 极难目视复现）。
  **现在抓住什么**：删掉 13666 那一句、或把 13709/14646 的闸改回 `_command != None` ⇒ X8-a 红；
  把 14749 改回 `_pointerWorld` ⇒ X8-b 红。

### ③ 顺手②（授权 · 已做）：`ClearSelection` 只清敌方 ⇒ 友方目标残留绿环

- 改：`BattleDriver.cs:15166-15178` 新增一段 `foreach (var kv in _myUnits)`，两侧一起刷 `Normal` +
  按**各自**打法刷底光（`ApplyOwnAttackType(_me, …)`）。判据补在注释里（`CancelActionStates` →
  `StopShowingPotentialTargets` 是整场收）。
- **原来抓不住什么**：今天没有一条路会把**我方**单位点亮（友方候选档在表现层被 `AbilityPickOwner` 退回敌方），
  所以症状**现在不可见**、断言也测不出来；一旦那一档打开，症状是「选择收起了、自己人还亮着绿环」。
- **现在抓住什么**：不新增断言（今天触发不了 ⇒ 写了也是假绿）；改为**代码结构上就不分侧**。

### ④ 顺手①（**未做** —— 文件在授权白名单之外）

- 「会死」图标层不进 `StampQueue`、也不写 `sortingOrder`（原版 `SpriteRenderer.m_SortingOrder = 2`）
  —— 修点在 **`Core/CardView.cs:7848` 的 `BuildWillDie()`**，**不在**本轮白名单（`AttackSelector.cs` /
  `BattleDriver.cs` / `BattleScene.cs`）⇒ **按纪律停手，未改**。
- 精确补丁（谁拿到 `CardView.cs` 就照这两行做）：
  1. `BuildWillDie()` 里建完 `_willDieMr` 之后加：`_willDieMr.sortingOrder = 2;`（原版 `SpriteRenderer.m_SortingOrder`）。
  2. `SetQueue(int q)`（或它调的那一圈盖章循环）里补一句 `if (_willDieMr != null) StampQueue(_willDieMr);`
     —— 否则卡层队列被抬到 ≥3000 时，这层（材质队列停在默认 3000）会被卡压住。
  3. 配一条断言：`hv.SetQueue(3005)` 之后 `_willDie` 的渲染队列 == 3005（现读 `WillDieAlpha` 那条同族探针旁边）。

### ⑤ 新记账（本轮**未授权**、未改）

- `TickTargetingLean`（`BattleDriver.cs:14692` 的 `aiming`）仍用 `_command != None` 当闸，
  而原版 `CardScript.Update` 的 `OrientToTargetingDirection` 只在 `cardState == inPlayAminingAttack(0x11)`
  时跑，而 0x11 是 **`TryDraggingFromBoard` 那一刻**就置上的 ⇒ **原版的「瞄准抬手」从起手就开始，我们现在要等选完打法**。
  与 ② 同族，属行为变化 ⇒ 等裁。判据：`CardScript__Update.c:6` + `CardScript__TryDraggingFromBoard.c:56`。
- `AttackSelector.ContainsBar`（槽外取消）是自造：uGUI 档的悬停已经按 **`Highlight` 176×176 矩形**判，
  而它仍是手搓矩形 ⇒ 底板边缘会出现「原版会吃、我们会取消」。属「原版没有等价物」那一类，等裁。

### ⑥ 未验（如实记）

- 两条新断言（`★ X8-a` / `★ X8-b`）**一次都没跑过** —— 本轮明令不许跑 Unity 批处理。
  下一次 `BattleScene.Run` 才见真章；若 `X8-a` 红，先查三处前提：
  ① `driver.cam` 在 `RunCore` 里由 `BuildScene` 赋值（`BattleScene.cs:44954`）⇒ `DrivePlayerTurn` 不会在 `cam == null` 早退；
  ② `interaction.IsDragging` 必须是假（假 → 会走 `CancelCommand()` 把选择收掉）；
  ③ 钉死的两个指针点（`±3.2, 4.5, -1.5`）**必须在选择器三钮之外**（世界档走手搓圆判定；uGUI 档不看坐标）。
- 阈值那条断言（`★ 5 px`）是纯常量比较，必绿；但它**只证明常量对**，不证明手感 —— 手感仍需真机拖一次（5 px 在 1080p 下约 5 个像素，肉眼几乎「一碰就出」）。

## 五、X8 第二批（2026-10-25 · 剩下三件：顺手① · 抬手闸 · ContainsBar 同源）

> 本轮可改范围：`Core/CardView.cs` · `Battle/BattleDriver.cs` · `Battle/AttackSelector.cs` · `Editor/BattleScene.cs`。
> 类型检查 `TMPDIR=/tmp/wf_atk2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（连跑两次都 0）。
> **卡池 md5 未变**：`ef431e8054822f359162862fdf51dd5b`（只动表现层）。行尾未翻（四文件 CRLF 数 == LF 数）。
> ⛔ 未跑 Unity 批处理 / 未碰 git 写操作 / 未碰 `RuleCore.cs` · `EffectResolver.cs` · `CardInteraction.cs` · 三份正本。

### ① 顺手①（已做）：`BuildWillDie` 的排序号与队列 —— `Core/CardView.cs:7878-7879`
- 改：建完 `_willDieMr` 后 `sortingOrder = 2`（原版 `SpriteRenderer.m_SortingOrder`）＋ `StampQueue(_willDieMr)`。
- 🔴 **与你给的补丁有一处刻意不同（请复核）**：`SetQueue(int q)` 那一圈**本来就扫得到它** ——
  它走的是 `GetComponentsInChildren<MeshRenderer>(true)`（`CardView.cs:1597-1603`），
  所以「层已建好之后再 `SetQueue`」这一档**没有缺**。真正缺的只有**晚建**那一档：
  `_willDie` 是 `SetWillDie` 第一次调用才懒建的，而 `SetQueue` 通常在那之前就跑完了
  ⇒ 再补一句 `if (_willDieMr != null) StampQueue(_willDieMr)` 是**同一条判据的第二份写法**（铁律 6 说的那种），
  所以我改成**建层当场自己盖一次**（同族先例：徽标 `:3819` / 状态环 `:4465` / 数值层 `:12380` 都是「晚建 ⇒ 当场补号」）。
  ⇒ 若你仍要那句显式调用，我可以加（它是幂等的、不会坏事），只是我认为那是冗余。
- 探针（新）：`WillDieRenderQueue`（`:7913`）· `WillDieSortingOrder`（`:7922`）。
- 断言（`Editor/BattleScene.cs:9646-9670`，用一张**现造的探针卡**，避开「这层早就建好了」）：
  先 `SetQueue(3005)` → 断言「层还没建」→ 第一次 `SetWillDie(true)` → 读队列**必须 3005**、排序号**必须 2**。
- **原来抓不住什么**：队列那条从来没被量过 ⇒ 「晚建层停在材质默认 3000、被卡面盖住」是**静默**的
  （画面上只是图标不见了，和「预览伤害不够致死」长得一模一样）。排序号同理（`MeshRenderer` 默认 0，与卡面若干层同号）。
- **现在抓住什么**：删 `StampQueue(_willDieMr)` ⇒ 队列退回 3000 ≠ 3005 ⇒ 红；删 `sortingOrder = 2` ⇒ 红。

### ② 抬手闸（已做）：`BattleDriver.cs:14702` `bool aiming = _selectedSlot >= 0;`
- 判据：`CardScript__Update.c:6` 跑的是 `cardState == 0x11`，而 0x11 在 `CardScript__TryDraggingFromBoard.c:56`
  就置上（`:58` 才 `StartAttackFrom`，那时 `attackType` 可以是 0）。
- **「会不会叠两次」的核**（你点名要确认的）：**不会**。这一路是
  `localPosition = Lerp(当前, _leanHome + LeanOffset(true), clamp01(dt/ChargeTime))` —— **绝对目标、不是增量**，
  目标点恒为「基准 + 一个常量位移」⇒ 推多少帧都收敛到同一个点，不可能叠加。
  出手时 `LeanHomeOf`（`:23310` 附近）会把 `_leanActive` 清掉、并把**抬手之前**的 `_leanHome` 交给攻击序列
  ⇒ 攻击那几段也不会带着这一位移再叠一遍。
- 探针（新）：`LeanActiveForTest`（`BattleDriver.cs:14735`）。
- 断言（`Editor/BattleScene.cs:9084-9110`）：起手（**还没定打法**）`Step(0.5 s)` 后
  ① `_leanActive` 为真；② 卡体真的离开静止位；③ 位移幅度 **= 一个 `LeanOffset`**；④ 选完打法再推 0.2 s **幅度不变**（不是继续长/翻倍）。
  另加前提：那一步之前先 `Step(1.5 s)` 把落场补间跑完（`TickTargetingLean` 见 `IsTweening` 会让开，不先收干净会静默跳过）。
- **原来抓不住什么**：整条抬手链**一条断言都没有** ⇒「拖起来到选完打法这段时间卡是僵的」全绿。
- **现在抓住什么**：闸改回 `_command != None` ⇒ 位移 0 ⇒ X8-d 三条红；改成增量写法 ⇒ 第 ④ 条红。

### ③ `ContainsBar` 与悬停同源（已做）
- 改：把 `UpdatePointer` 里那段命中循环提成 **`KindAtPoint(world)`**（`AttackSelector.cs:1656`，含 `HitPiece:1674`
  的**单件判定**），`ContainsBar`（`:1818`）改成 `KindAtPoint(world) != None`。⛔ 不再有第二份手搓矩形。
- 两档的单位在 `KindAtPoint` 里**一次换好**：uGUI 档 `_frame.InverseTransformPoint(world)` → 帧局部 px（与件自己的
  `localPosition` 同空间）；世界档直接用世界点走 `HitRect`。
- ⚠️ **行为变化（如实记，请你裁）**：现在「点在选择器**底板**上、但不在任何一颗钮上」会走**取消**
  （旧的手搓大矩形把它算作「在里面」）。这是「与悬停同源」的必然结果 —— 那一片本来就没有动作
  （原版 `Select Attack Background` 是 `raycastTarget=1` 的纯色靶、点了什么也不发生），而「点槽外取消」本来就是我们自造的。
  另一个同源副作用：**不可选的钮**（`pindown` 的近战那颗 / 教程关掉的那颗）不算受体 ⇒ 点它会取消（旧实现不取消）。
- 断言（`Editor/BattleScene.cs:8970-8983`）：钮中心「悬停档 = 远程、取消档 = true」；**钮外 60 px**
  （越过钮半宽 59.186 与圈半宽 88.1，但仍在旧手搓矩形的 `BarW/2 = 310.85` 之内）两档**都答「不在」**。
- **原来抓不住什么**：两条判定各写一套几何，没有任何断言把两者放一起比 ⇒ 边缘那圈「悬停说不在、取消说在」永远绿。
- **现在抓住什么**：把 `ContainsBar` 改回手搓大矩形 ⇒ 「钮外 60 px」那一点又变 true ⇒ 红。

### 新发现（本轮没动，先记账）
1. **同族（抬手/准星那一族）**：`HighlightTargetsCore` 只刷 `other` 那一侧的「自己的打法」底光；
   若 `side == _me`（友方能力候选那一档，今天被 `AbilityPickOwner` 退回敌方所以走不到），
   **我方**那排的底光在选目标期间不会被刷新（与 §四③ 的「只清敌方」同源）。
2. `KindAtPoint` 的 uGUI 档依赖 `_frame.InverseTransformPoint`；`WorldOfPxForTest` 里对
   「画布 `lossyScale` 还没算出来」只做了兜底（退回 px），若真发生会让 X8-c 红而不是静默错 —— 记一笔。
3. `TickTargetingLean` 的「见 `IsTweening` 就让开」这道闸会让**起手瞬间正有落场补间**的单位**这一手完全不抬手**，
   直到下一次「开始选目标」（原版 `OrientToTargetingDirection` 是每帧无条件跑的）—— 今天无害（落场补间很短），
   但它是「同一类：状态在入口上漏设」的潜在同类，记一笔。
