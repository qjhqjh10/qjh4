# A4 报告 —— `BattleDriver.cs` 攻坚批（第八十七会话 · 2026-10-27）

**独占文件**：`CardPresentation/Battle/BattleDriver.cs`（+ `RuleEngine/Core/CardDef.cs` 只加第 6 件那一个属性）。
⛔ 没碰 `BattleScene.cs` / `RuleCore.cs` / `EffectResolver.cs`。

**例外报备（必须让调度台知道）**：第 6 件要「数据真的流到消费端」，而链上还缺两格，**两格都不在我的白名单里**：

| 文件 | 改了什么 | 行数 |
|---|---|---|
| `RuleEngine/Data/CardDatabase.cs` | ① DTO 加一格 `targetingTextReferenceId`（`:73`）② `def.AttachTargetingTextReference(c.targetingTextReferenceId)`（`:1690`，紧挨 `AttachAlternateArts`） | +4 |

不这么改，「只加属性」就等于加了一个**永远为 null 的死属性**（选目标 tooltip 仍然一律不弹 = 第 6 件没做成）。
该文件在我开工时**没有任何未提交改动**、也不在 `调度层日志` §13·4 的任何代理名下 ⇒ 撞车风险按当时状态为零。

---

## ① 8 件各自改了什么

### 1. 多目标逐目标播放（判据 → `报告/B9_W54ENG2_掉舱端点与多目标事件.md` §六）

| 位置 | 改了什么 |
|---|---|
| `BattleDriver.cs:19875` 起（`PendingSignal` 结构**尾**） | 新增三格：`beatTgtPlayer` / `beatTgtSlot` / `beatTgtInstance`（`-1` = 不覆盖） |
| `:19630` `ExpandAbilityBeats` | ① 目标数改成**优先按 `e.AbilityTargetList`**（`null` = 引擎没填 ⇒ 退回改前的 0/1 口径）；② 位置选项里含 `abilityTarget` 的**每一拍**，按列表**逐目标各排一条** `PendingSignal`（**同一时刻** `t0+acc` ⇒ 原版 `:217-222`「只有最后一个目标带 `waitAnimToApplyEffect`」自然成立：`acc` 只加一次） |
| `:23389` `PlayAbilityBeat` | 新增三参 `beatTgtPlayer/Slot/Instance`；`target` 解析改成「覆盖优先 → 事件 `Target*`」 |
| 类尾 `AbilityTargetView(player, slot, instanceId)` | **先按 `InstanceId` 找视图、找不到再退回 `Player@Slot`**（§六·3 的口径；换位/补位后格号会变） |
| 同上 | 🔴 **新查出并当场修的一处结构性坑**：`beatTail` 不只表示「不是第一拍」，它**还兼着「事件级副作用（语音 / 战斗音效 / 手感 / trait 粒子）要不要重播」那道门**（`PlaySignal` 里 `if (!beatTail)`）⇒ 同一拍的第 2..N 个目标必须一起算 tail，否则**一次技能会响 N 遍**。已改成 `beatTail = (!first 或 ti > 0)`。长度只有 0/1 时这一句与改前等价。 |
| `PlaySignal` 签名 + **两条出口**（`AdvanceTimeline` `:20655`、`StepReplaySignal` `:8447`） | 三个新字段一路带下去（铁律 10 第 5 条：同一对象多入口，状态要带齐 —— 只接主泵会让「单步」时所有拍都瞄事件那个目标） |

### 2. 拖目标型法术的准星（判据 → `报告/R8_Update13分支.md` C-1 · 原版 `BattleManager__Update.c:800-820`）

- 新方法 `UpdateSpellDragReticle(world)`（类尾 `:35852`）：锚点 = **我方督军卡**（`_myUnits[WarlordSlot]`）、
  终点 = 指针 + `moveCardYOffset = 0.2`（世界单位，`VarsDesktop.json` 现读；`VarsMobile 0.7` 是移动档，未接切换）。
- 闸两条：① 拖的确实是**目标型法术**（`CardView.Inst.Card.Targeted == 1 && !IsUnit`）
  ② 指针**过了手牌限位线**（`LayoutSpace.ToNormalized(world).y >= BoardLayout.HandLimitY`，
  与 `CardInteraction.UpdateDrag` 那条「要不要试着打到场上」用的是**同一根线**；对应原版 `:800` 的 `HandLimitArea` 比较）。
- 调用点：`DrivePlayerTurn` 的 `interaction.IsDragging` 分支（`:14821` 起，排在 `CancelCommand()` **之后**）；
  拖拽结束那一帧在非拖拽路径上把准星收掉（`_spellDragReticleOn`，`:14827` 起）。
- ⚠️ 与 `Show/Aim` 的关系：`Show` 只在第一次，之后走 `Aim`（原版 `MoveCrosshair` 也是「挪准星 + UpdateTrail」两条）。

### 3. ESC 的第二个消费者（原版 `BattleManager__Update.c:1303-1335`，掩码 `0x402C`）

- 新 `HandleChatHotkeyEsc()`（类尾 `:35909`）+ `ChatHotkeyUiState()`（`:35891`，把我们的状态映射到原版 15 档的
  **近似档**，口径与本文件既有的 `BattleUiIsNormalBattle` 同一路推导方式）。
- 逐档照抄：`8(postBattle)` ⇒ 直接不认；`0/7/11/12` ⇒ **只打一条日志**（原版 `"ClickChat but not during battle"`
  + `state.ToString()`）；`2/3/5/14`（掩码位）与 `4` ⇒ 什么都不做；`1/6/9/10/13` ⇒ `ClickChatHud()`
  （**复用聊天钮那一个落点**，⛔ 没另写开气泡）。
- 排位：`Update` 里 `HandleSettingsEsc()` 之后、`HandleChatPopup()`（ESC 关气泡）之前 —— 气泡开着时
  `ClickChatHud` 自己会被 `HudClickBlocked` 挡回去，不会重复开。
- ⚠️ 两条原版闸如实标未接（本机无等价物）：`blockingOverlay.isSpinning`（我们没那种转圈遮罩，恒假）、
  `voiceLinesController.IsPopUpActiveAndEnabled`（我们的 `ChatPopup` 常驻建着，恒真）。

### 4. 回放局键位（原版 `BattleManager__Update.c:1336-1413`）

`HandleTimeControlKeys` 重写（`:8684` 起）：

| 键 | 原版 | 我们 |
|---|---|---|
| `A` / `←` | `SceneManager.LoadScene(当前场景)` | `Restart()`（回放局里 = `PlayReplay(_replayRec)` 重演同一份）——**只在 `_replaySession`** 认 |
| `D` / `→` | **`Time.timeScale = 15`**（`DAT_1834b3164` = 15.0）+ 排下一条回放动作 | 新 `SetReplaySpeed(15f)`（`:35947`，`Time.timeScale` 在本仓的**唯一写点**）；「排下一条」由 `TickReplayPump` 每帧做 |
| `Space` | **自动播放开关**（`+0x521` 取反 + Start/StopCoroutine） | `_replayPaused` 取反（语义同原版；普通对局里这条仍是既有功能、原版无对应键） |
| `.` | **不存在** | **已删**（自设计键；单步留在回放条那颗钮 `ApplyPendingReplay → Btn.Step`，原版本来就是钮） |

- 复位：`Begin()`（`:4934` 起）与 `LeaveBattle()`（`:12709`）都调 `ResetReplaySpeed()` —— 15× 是**回放专用**的
  全局 `timeScale`，带出去会毁掉主菜单/下一局。
- ⚠️ `R`（重开一局）**没动**：它在 `Ctx.IsOver` 那一支、且明确是我们自己的调试键（原版无「再来一局」）。

### 5. `OfficialFaceTextWhitelist` 摘 5 条（裁定 → `调度台日志.md` §12·94）

- `:28101` 现在只剩 `{ "AM72" }`；6 段 doc 逐条就地改写成「已摘 / 为什么」，并写明**代价**（5 张卡措辞会变 +
  `BattleScene.cs` 那三处断言要一起改 + 「其余 10 种语言」那一路的绕过点也随之对齐）。
- **零行数变化**（那一块净 0 行）—— 下游账落点不受影响。
- 顺手把 `FaceTextFull` 里那条**已过期**的行内注释也就地订正了（`BattleDriver.cs:28131` 起那两行原写
  「`ASH48` 官方后半句截断 · `SOR25` 内容分歧待 PnP 裁」—— 现在只剩 `AM72`）。**同样是零行数改写**。

### 6. `targetingTextReferenceId` 落地（数据 → 卡定义 → 消费端）

| 环节 | 落点 |
|---|---|
| 卡表 | `cards_engine.json` **v35** 已有该列（现读 1,129 张**全有该键**、51 张非空）—— 生成器那半已由 `G1` 做完，本代理没动 |
| DTO | `CardDatabase.cs:73` `public string targetingTextReferenceId;` |
| 卡定义 | `CardDef.cs:6743` `public string TargetingTextReferenceId { get; private set; }` + `:6747` `AttachTargetingTextReference(string)`（**构造后挂载**，同 `AttachAlternateArts` 那一族 —— ⛔ 没新增构造参数：那 40 处是账的落点） |
| 挂载 | `CardDatabase.cs:1690`（紧挨 `AttachAlternateArts`） |
| 消费 | `BattleDriver.cs:35777` `ShowPlayTargetPrompt` **直读**；`TargetingTextReferenceIdOf` 现在只剩一句（`:35750` 附近），**反射 + 缓存 + 「没到就出声不弹」整段删掉** |

- 🔴 **语义变了（这也是第 6 件真正要修的那条）**：`null`（列缺席 / 夹具直建）现在按原版 `RawCardScript.cs:2450`
  的三元**回退 `uniqueId`**，⛔ 不再「不弹」；缺键仍由 `raw == key` 拦（不把键名印上屏）。
- 三态（`null` / `""` / 有值）在 `CardDef` 那一段 doc 里写清，`AttachTargetingTextReference` **不做 null→"" 归一**。

### 7. 收局摧场（原版 `DestroyUnitsAfterBattleEnd` + `BasicBattleEndSequence`）

- 新 `BuildEndBattleDestroySequence()`（类尾 `:35999`）+ `EndBattleDestroySequencePending()`（`:36034`）：
  等 **1.0 s** → **我方** hero（督军格）→ 等 `0.5 + 0.2` → 逐单位**从末格往回**各等 0.2 → 收尾等 0.2 →
  **对面**同法 → 再等 **0.5 s** → 放行结算。
- 两个 `VarsGlobal` 偏移按 `资料/VarsGlobal_原版数值.md` 的基址 **0x18** 换算出字段：
  `+0x34 → deathTimeWarlordDuration = 0.5` · `+0x8c → minionDeathTime = 0.2`
  （同表 `targettingAnimTime(+0x88)=0.5` / `minionInPlayScale(+0x68)=0.75` 三条互证）。
- 演出口**复用** `PlayDeathFeel`（⛔ 没另写一份消散）。
- 新 `_endSeqDead`（`HashSet<UnitState>`）：`SyncBoard` 遇到「已送走」的单位**不再建视图回来**
  （否则画面上「倒下 → 又站起来」，而且零报错）。
- 闸门：`ShowEndPanelAndFreezeInput`（`:32105`）**第一句**先过它，没演完就 `_endPanelPending = true`（由 `Update` 每帧重试）。
- 🔴 **批处理不跑**（`Application.isPlaying` 第一道闸，与 `_endPanelWaitLeft` 同一条口径）⇒ `BattleScene.Run`
  那条「打完之后结算面板是显示的」读到的仍是**当场**，与改前逐字一致。

### 8. 结转能量桥（引擎 `PlayerState.SetSavedMana` → HUD 六步状态机）

- 新 `SyncSavedManaToHud()`（类尾 `:36076`）：**边沿触发**（`GetSavedMana()` / `Energy` / `MaxEnergy` 三量一变就喂一次），
  落点 = `SetSavedMana(side, carry, max(carry, energy), max, useNumberChange: true)`；
  调用点 = `UpdateHud` 里能量数字那一行之前（`:31793`）。
- 新 `EnergyShownOf(side)`（`:36102`）= `max(0, Energy − 结转)`，替掉原来两处内联算式；
  `SetManaDisplay` 也夹了 0（🔶 自己设计：`EndTurnCore` 把 `Energy` 直接写 0，原版那一刻 `unusedMana` 还留着
  清零前的余额 ⇒ 不夹的话对手回合的 HUD 会印 `-1/3`；判据在引擎侧拿不到）。
- `Begin()` 里把那三个影子值一起清（否则新局 `carry/energy/max` 恰好与上一局相同 ⇒ **不会再喂**，灯停在「未知」档）。

---

## ② 验收读数

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_a4 bash 工具/typecheck.sh` | **本批一共跑了 10 次**：前 **3 次 0/0**；此后 7 次（跨约 25 分钟）红在**别人的文件**上，而**每一次读数里我的三个文件都是 0 条**。收尾那次 = **6 条错 / 4 个文件**，全是别人的：`Core/CardView.cs`(1) · `Shell/ChatTab.cs`(2) · `Shell/ExpansionPassEventWindow.cs`(2) · `Shell/MainMenuRuntime.cs`(1)（都是另一批代理正在写的活；`git status` 里 `ScoringEvent.cs` 还是 `??` 新文件）。✅ **交叉验证（不改别人的文件）**：把 `ScoringEvent.cs` 换成临时目录里一份**只挪了 `using` 别名位置**的探针副本、并排除当时正在报语法错的 `ExpansionPassEventWindow.cs`，用 `csc` 直编整份运行时程序集 ⇒ **唯一的报错就是「排除那个文件」带来的级联**（`WindowsManager.cs:1108` 找不到 `ExpansionPassEventWindow`），其余 776 个源文件**一条错都没有（含我的三个）**。⇒ 我的改动编译干净；⚠️ **调度台在重同步点必须复跑一次 typecheck 拿真绿**。 |
| `git diff --numstat`（相对 HEAD，这三个文件开工前**无未提交改动** ⇒ 数字就是本批的） | `BattleDriver.cs` **+492 / −77** · `CardDef.cs` **+25 / −0** · `CardDatabase.cs` **+4 / −0** |
| 行数 | `BattleDriver.cs` **35,695 → 36,110**（净 **+415**）· `CardDef.cs` 8,812 → 8,837（+25）· `CardDatabase.cs` 3,360 → 3,364（+4） |
| 行尾（**必须二进制读**） | `BattleDriver.cs` `\n` 36,110 / `\r\n` 36,110 / 裸 CR **0**（HEAD 也是 CRLF 35,695 ⇒ **没翻**）· `CardDef.cs` LF 8,837 / 裸 CR 0 · `CardDatabase.cs` LF 3,364 / 裸 CR 0 |
| 账补丁 | `账/_parts/B87_A4_BattleDriver.tsv`：11 列 / 带表头 / **6 行** / LF / `方法` 列都是完整签名（带 `(...)`） |
| `check_patch_conflicts.py` | 🔴 **报 1 条跨补丁重复键**：`ManaManager.SetManaSaved(...)` 同时出现在 `B87_A2_B04.tsv:12`（C）与我的 `:2`（B）。按字典序 `B87_A4` **后合 ⇒ 我的赢**（正是想要的：A2 那一行是「桥还没有」的旧结论）。**给调度台：合并顺序必须 A2 先、A4 后**（或从 A2 删掉那一行）。 |
| 没跑 Unity | ⛔ 本代理不跑（简报红线）。第 5 件会让 `BattleScene.cs` 那三处白名单断言变红 —— 那是**同批该改的**（简报里点明由另一个代理改），⛔ 不是为了绿而保留白名单。 |

### 🔴 行号漂移表（给调度台在重同步点跑 `check_ledger_points --all-A --fix`）

`BattleDriver.cs`：**35,695 → 36,110（净 +415）**。按**改前（HEAD）行号**：

| 改前行号起点 | 累计漂移 | 触发点 |
|---|---:|---|
| ≤ 4,933 | **0** | —— |
| ≥ 4,934 | **+15** | `Begin` 里的三件复位（回放速度 / 收局账 / 结转影子值） |
| ≥ 8,432 | +18 | `StepReplaySignal` 那条出口补三个 beat 目标实参 |
| ≥ 8,666 | +28 | `HandleTimeControlKeys` 重写（`A`/`D`/`Space`） |
| ≥ 8,679 | +34 | 同上 |
| ≥ 8,687 | +35 | 同上 |
| ≥ 12,674 | +37 | `LeaveBattle` 复位回放速度 |
| ≥ 14,278 | +43 | `Update` 里 ESC ⇒ 聊天钮那一句 |
| ≥ 14,778 | +48 | `DrivePlayerTurn` 拖拽分支喂准星 |
| ≥ 14,779 | +55 | 同上（拖拽结束收准星） |
| ≥ 19,583 | +59 | `ExpandAbilityBeats` 目标数改按列表 |
| ≥ 19,608 | +65 | 同上（逐目标排拍） |
| ≥ 19,610 | +77 | 同上 |
| ≥ 19,800 | +88 | `PendingSignal` 尾部三格 |
| ≥ 20,569 | +90 | `AdvanceTimeline` 那条出口补实参 |
| ≥ 22,742 | +93 | `PlaySignal` 签名三参 |
| ≥ 22,818 | +94 | beat 分支透传 |
| ≥ 23,311 | +97 | `PlayAbilityBeat` 签名三参 |
| ≥ 23,317 | +99 | 目标解析改成「覆盖优先」 |
| ≥ 26,226 | +114 | `SyncBoard` 的 `_endSeqDead` 闸 |
| ≥ 27,967 / 27,981 / 27,987 | +113 / +111 / +114 | 白名单那一段就地改写（净 0） |
| ≥ 31,690 | +116 | 能量数字那一行（喂桥 + `EnergyShownOf`） |
| ≥ 31,993 | +123 | `ShowEndPanelAndFreezeInput` 首道闸 |
| ≥ 33,777 | +126 | `SetManaDisplay` 夹 0 |
| ≥ 35,626 / 35,634 / 35,644 / 35,647 | +126 / +121 / +122 / +106 | 反射桥 → 直读（删了 21 行） |
| ≥ 35,676 / 35,677 | +108 / +107 | `ShowPlayTargetPrompt` 里那条 `null` 早退删掉 |
| ≥ 35,694 | **+415** | 类尾新成员块 |

**账里 381 处 `BattleDriver.cs:NNNN` 引用，其中 335 处会漂**（清单可直接 `--all-A --fix` 重钉）。

`CardDef.cs`（8,812 → 8,837，**+25**）：新块插在 `CardDef` 类尾（原 `:6718` 之后、`TraitReachability` 之前）。
⇒ 账里引用本文件的行**大部分不漂**（`:84`–`:6552` 共 55 处），但**`:6718` 之后的 5 处漂 +25**：
`CardDef.cs:7066` · `:7663` · `:7864` · `:8505` · `:8520`。

`CardDatabase.cs`（3,360 → 3,364，**+4**）：两处插入（`:73` DTO **+1**、`:1687` 起挂载 **+4 累计**）
⇒ `:1040` / `:1610` / `:1611` 漂 **+1**；`:1690` / `:1799` / `:2303` / `:2449` / `:3207` 漂 **+4**。

---

## ③ 没做的（如实列）

1. **第 5 件的那三处 `BattleScene.cs` 断言没改**（不在白名单）：`BattleScene.cs:25372` / `:25401` 现在会
   用只剩 `AM72` 的白名单去判 5 张卡 —— 那三处期望值要同批改（简报说另一个代理负责）。
2. **第 7 件的两条原版闸没建模**：`battleWinner == 10` / `resultType == 0x14` 的「整侧不摧」
   （值来自服务端 `RankedMatchResult`，本机拿不到同一语义 ⇒ 两侧都摧）；`skipDeathSound`
   （`PlayDeathFeel` 本身不播死亡音、音效走事件那一拍 ⇒ 无观察量）。
3. **第 8 件的数字显示口径是 🔶 近似**（见上）：引擎不保留「清零前的 unused」。
4. **一条 Unity 都没跑**（简报红线）⇒ 第 1 件的「逐目标」只有夹具级证据（池内多目标档今天 0 张可达）、
   第 2/3/4/7/8 的真机行为**全部未验**。

---

## ④ 顺手发现（只报、⛔ 没改）

1. 🔴 **`BattleDriver.DefOf(CardView)` 在「这张卡不在手牌里」时每调一次就打一条 `Debug.Log`**（`:13200` 附近）。
   它是「视图 → CardDef」的唯一现成口，但任何**每帧**调用点都会刷屏。我这批**没有**用它（用的是
   `CardView.Inst.Card`，零查找、零日志），但将来谁要按 `DefOf` 做逐帧判定，先加一个静默重载。
2. 🔴 **`BattleUiIsNormalBattle` 与我这批新加的 `ChatHotkeyUiState` 是「同一套状态、两套推导」**——
   按铁律 6（同一件事只留一处）这里已经有分叉风险。本轮我按「不新造状态机、只在既有推导上加一档」写，
   并把它标成**近似档**；真要收口，应该抽一个「`UIstate` 近似值」的单一返回口、让两者都读它。
   但那会动到 `BattleUiIsNormalBattle` 的既有语义与它的断言 ⇒ 留给调度台判。
3. ⚠️ **`CardInteraction.UpdateDrag` 把「手牌让位」那道守卫写在自己的文件里**（`:1025` 那行 `HandLimitY` 比较），
   我这批又在 `BattleDriver` 里写了同一根线的一次比较（准星那道闸）。两处**读的是同一个常量**
   （`BoardLayout.HandLimitY`）所以不会分叉，但「哪条线算过线」这件事现在有两个消费者 —— 记一笔。
4. ⚠️ **`WorldPointerCallsForTest` 的计数在拖牌期间会 +1**（我为了喂准星在拖拽分支调了一次 `WorldPointer()`；
   原来那一档是早退、不调）。若有断言在「拖牌期间」比值，需要知道这条。
5. ℹ️ `BattleScene.cs:25409-25416` 里 `GOF27` 那条注释说「真理源在 `BattleDriver.OfficialFaceTextWhitelist`
   （生产文件、由另一代理改）」—— **那个「另一代理」就是我**，本轮已改成只剩 `AM72`；
   那三处断言按上面的清单改即可。
