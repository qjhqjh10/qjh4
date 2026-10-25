# R15 · 战斗界面_HUD 遗留 C（第 2 轮）

> 执行代理 `R15` · 2026-10-25（第八十会话 · 批次 2）· 白名单：`CardPresentation/Battle/**` · `Hand/**` · `Board/**`
> 账 `资料/完全复刻/账/战斗界面_HUD.tsv` **只读**（R1 独占写）。本报告里所有「账应该怎么改」都是**建议**，没有动账。
> ⚠️ 本代理中途被进程崩溃打断一次（约 23:03）。恢复后逐条核过盘上状态：改的东西都在、`typecheck` 0/0、**没有半成品**（见 §④）。

---

## ① 结论（数字）

| 项 | 数 |
|---|---|
| **本轮落地** | **5 件 / 7 行账**（P0 ×2 件 = 行 544/542 + 553/551；P2 ×3 件 = 行 395 + 407 + 412/413） |
| **查出「账已过期、其实早做完」** | **5 件**（行 41 · 256 · 365/368/374 · 387/400/413 —— 全是**批次 1 的 A 接线清单 #1～#4** 已落地的） |
| **改判 `D`（原版本身没有）** | **4 行**（148 / 150 / 157 / 158 —— codex / oath 持久框，判据见 §②·五） |
| `typecheck` | **运行时 0 / 编辑器 0** ✅（`TMPDIR=/tmp/wf_r15`） |
| `BattleDriver.cs` 行尾 | **CRLF 32488 / LF 32488 / 裸 CR 0** ✅（纯 CRLF，没翻） |
| `check_ledger_points.py 战斗界面_HUD` | A 394 · 通过 222 · **漂移 172 · 失败 0**（漂移全在 `BattleDriver.cs` 的行号上；**R1 在同步点跑一次 `--fix` 即自愈**） |
| `ledger_progress.py` | 本域仍 **C 39 → 建议改后 30**（见 §②末尾那张「账应怎么改」对照） |

**一句话**：这个域的 **P0/P1 其实已经被批次 1 做掉了大半**（账没跟着更新）；本轮把**唯一还错着**的那件（能量数字弹跳 = 原版没有的设计）**改正并搬到正确的那两颗**，补上 `TryDOVfx` 的「降」半，再顺手收掉三个**玩家看得见**的时钟/回放小件。

---

## ② 判据（逐件，`文件:行号` / 资产路径 + 字段）

### 一、`ManaTypeHolder` 的 `DoTextAnim` / `TryDOVfx`（行 544/542 + 553/551，P0）

**先说结论：批次 1 接的那句「能量数字变化时弹跳」是错的，本轮已删并搬走。**

原版 `SetMana` 的两跳（🥇 `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ManaTypeHolder.cs:66-82`）：

```csharp
this.text.text = value.ToString();
if (!silently) {
    if (useTweenOnValueChange) DoTextAnim();   // ← 弹跳，由序列化标志决定
    TryDOVfx(value);                            // ← 值特效，自己再判一次标志与方向
}
```

**8 个 holder 的两个标志（13 个战场逐份现读，每场同分布）**：

| holder | `useTweenOnValueChange` | `useVfxOnValueChange` | `vfxOnValueUp` / `vfxOnValueDown` |
|---|---:|---:|---|
| `QuestPointsHolder` ×2 | **1** | **1** | `5bbc15aa…` / `2223a4d2…`（**两件都有**） |
| `FaithHolder` ×2 | 0 | **1** | `ae32e60e…` / **空 GUID** |
| `ManaHolder` ×2 | **0** | **0** | 空 / 空 |
| `SpiritStoneHolder` ×2 | 0 | 0 | 空 / 空 |

每场都是 `n = 8 · tween1 = 2 · vfx1 = 4`（13 份全部一致）。

- **字段出处**：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/MonoBehaviour/MonoBehaviour_<pid>.json`
  的 `useTweenOnValueChange` / `useVfxOnValueChange` 两列；父链解到 holder 名（arena1 那 8 颗 pid =
  `4153/4223`=SpiritStone · `4525/4703`=QuestPoints · `4868/5227`=Mana · `5031/5237`=Faith）。
- **GUID → 资产名**：`资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` 的 `name` 列
  ⇒ `5bbc15aa…`=**`Quest UI Gain`** · `2223a4d2…`=**`Quest UI Trigger`** · `ae32e60e…`=**`Faith UI Gain`**。
  三件 prefab **都在我们工程里**（`WarpforgeVFX/Prefabs/*.prefab`），且**已进**
  `Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 清单 ⇒ `CardEffects.Fire(名字)` 取得到。

**`DoTextAnim` 的四个参数**（🥈 `decomp_full/ManaTypeHolder__DoTextAnim.c` 逐句 + `.rdata` 现读）：

| 参数 | 值 | 出处 |
|---|---|---|
| punch 向量 | `Vector3.one × 0.2` | `0x1834b2bb0` = **0.2f**（`工具/read_literal.py` 4B 档；8B 档是 3e-5，按 `DOPunchScale` 的形参类型取 4B） |
| duration | **0.5 s** | `0x1834b2bb4` = **0.5f** |
| vibrato | **10** | 调用点立即数（`:103`） |
| elasticity | **1.0** | `0x1834b2bb8` = **1.0f** |
| 前置 | 先 `localScale = originalScale`（原版在 `originalScale == 0` 时记一次） | `:86-95` |

> 「原始缩放」这一格：原版记的是 `text.transform.localScale`；我们每颗 `Label` 的根节点**从不被写 `localScale`**
> （`Battle/Label.cs` 的 `scale` 形参落在 `l.scale` 字段＝字号档，不是 transform）⇒ 恒 `(1,1,1)`，
> 直接复位 `Vector3.one` 与原版等价。

**`TryDOVfx` 的逻辑**（🥇 `ManaTypeHolder.cs:144-170`，`decomp_full/ManaTypeHolder__TryDOVfx.c` 交叉）：

```
if (!useVfxOnValueChange) return;            // Mana / SpiritStone 在这里就出去了
if (target == current) return;               // 值没变 ⇒ 什么也不做（current 也不更新）
aref = (target > current) ? up : down;
if (aref.RuntimeKeyIsValid()) Instantiate(aref.Load(), 本组件 transform 的 position, prefab 自己的 rotation);  // ⛔ 不带父节点
current = target;                            // 无论有没有播都记
```

**⇒ 我们这边的落法（两半分开）**：

- **升**那一半**已经在生产里**（不重复造）：
  `EvtKind.GainQuest / GainFaith → BattleDriver.GenericVfxFor → VfxMap.GainQuest / GainFaith`
  （`BattleDriver.cs` 的 `GenericVfxFor` 里 `case EvtKind.GainQuest / GainFaith` 那几行 · `Core/VfxMap.cs:429-431`），
  落点 = 资源图标（`IsResourceUiEvent` / `ResourceIcon01`）。事件本身由引擎发
  （`RuleEngine/Core/EffectResolver.cs:12314` / `:12398` / `:12404` / `:12467`）。
  ⇒ 再接一次 = **原版没有的第二下**。
- **降**那一半**原来一处都没有**（引擎只为「获得」发事件，花掉/被扣没有事件）⇒ **本轮补的就是它**。

### 二、`ClockManager.StopClock` 的「收倒计时」（行 412 / 413，P2）

🥇 `cpp2il/out_cpp2il_dev_ilspy/ClockManager.cs:276-298`（`StopClock`）· `:424-440`（`StopClockUi`）· `:782-800`（`EndCountdown`）：
三处**都做同样的三件**：`Destroy(countdownAnim)` / `SetEndTurnText(isPlayer:false)` / `clockCountdownFlag = false`。

我们的对应物：
- `countdownAnim` = `PlayCountdownAnim()` 播的那一件（`TurnTimerTickPulse`，`destroyTime = 17 s` 现读）
  —— 17 s **比一回合剩下的时间长** ⇒ 不收就演到对手回合。
- `clockCountdownFlag` = `_clockInCountdown`。⚠️ 它原来**只在 `ResetClock()`（下一回合开始）才清**
  ⇒ 「倒计时里结束回合」之后，`UpdateClockLabel` 会把**冻住的秒数**一直印在结束回合钮上直到对手回合走完。

### 三、`ScenarioVariables.clockCountdownSec`（行 395，P2）

- 取值链：🥇 `cpp2il …/BattleManager.cs:14461-14476` 的 `scenarioVariables` getter =
  **`IsTutorialMatch()` ⇒ `tutorialScenarioVars`**，否则 `defaultScenarioVars`（编辑期走 `editorScenarioVars`）。
- 三份资产现读（`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/{Default,Editor,Tutorial}Scenario.json`）：
  `clockCountdownSec = 15 / 15 / **10**`。
- ⇒ 只有**教程局**不同（10 s）。⛔ 别按别的 `matchType` 猜档。

### 四、回放局的结束回合钮文案（行 407，P2）

🥇 `cpp2il …/ClockManager.cs:118-131`（`Start()` 里）：`matchType == MatchType.Replay` ⇒
`clockButton.interactable = false` + `clockText.text = GetTranslation("Battle/HUD/Replay").ToUpper()`；
且 `SetEndTurnText` **开头就为回放局 early-return**（`:144-152`）⇒ 之后任何刷字都盖不掉。
词条两列 =「重播」/「Replay」（`Core/Loc.cs:1271`，此前**零消费点**）。
我们的等价物 = `_replaySession`（`PlayReplay` 置、`Begin` 清；原版那一档是 `MatchType.Replay = 160`，我们的 `Ctx.MatchType` 不跟着改）⇒ 两条都认。

### 五、`ShowCodex` / `ShowOath`（行 148 / 150 / 157 / 158）应改判 **D**

🥇 `cpp2il …/BattleCardUI.cs:2501-2570`：两个方法第一句都是

```csharp
CardTrait trait = CardTraitCollection.GetTrait(DefinedTrait.codex /*or oath*/);
UnityEngine.Object customObject = trait.CustomObject;
if ((bool)codexFrame || !customObject) return;      // ← customObject 为空 ⇒ 直接 return
```

而 69 个 `CardTrait` SO 的 `customObject` 现读（`数据/游戏数据/trait_collection.json`）：

- **非空的只有 7 个**：`Ambush / BloodThirst / Cruelty / Ecstasy / Sentry / Shuriken / Swarm`；
- **`codex` 与 `oath` 的 `customObject` 是 `{"guid": "", "name": null}`** ⇒ 原版这两位**永远 early-return**。

⇒ 这是**铁律 11 例外①（原版本身就没有）**，不是「欠账」。⛔ 做出来反而是「原版没有的设计」。
（同族先例：`Bastion` 的 `bastionText` 全 null 已判 D。）

### 六、批次 1 已经做掉、账还挂 C 的五件（复核证据）

| 账行 | 账说「还差」 | 现读证据（本代理亲核） | 判 |
|---|---|---|---|
| 41 `BasicCardUI.OnEnable` | `OnCardTapped` 里加 `CardDisplaySuppressedFor` | `BattleDriver.cs:12665` 那一句**已在**（注释 = 待A接线清单 #2） | ✅ A |
| 256 `BattleSettingsWindow.Open` | `Create(..., Ctx.MatchType)` + `OnSkipTutorial` | `BattleDriver.cs:28023-28032` 两样**都已接**（待A接线清单 #4） | ✅ A |
| 365/368/374 `ChooseCardMenu` 倒计时 | `TickClock` 里喂 `TickOpenDelay/CountdownTrigger` | `BattleDriver.cs:14085-14100` **已接**（待A接线清单 #1） | ✅ A |
| 387/400/413 时钟 for-anim | `_clockInCountdown && (depth>0 \|\| WaitingForBlockingAction)` | `BattleDriver.cs:14018` 那一句 + `:2888-2896` 的字段/公开口**都在** | ✅ A |

---

## ③ 改动清单（全部在 `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`）

> 一条文件（本代理独占）· 新成员**全部追加在类尾**（`:32340` 之后那一段）· 行尾 CRLF 未翻。

| # | 行号 | 改了什么 |
|---|---|---|
| 1 | `:30700-30707` | **删**「能量数字变化时弹跳」那一段（含 `_energyShownLast` 的闸），原位留一段更正注释（为什么删、搬到哪去） |
| 2 | `:1872-1875` | **删**字段 `_energyShownLast`（原 `:1874`），原位留更正注释 |
| 3 | `:30809-30816` | **加** 任务点数字那两行之后的 `TickManaHolderValue(me/foe.QuestPoints, …)`（弹 + 降向特效） |
| 4 | `:13933-13939` | `PlayCountdownAnim` 播完按 `childCount` 差**记下实例** `_countdownAnimGo`（= 原版 `countdownAnim +0x98`） |
| 5 | `:14052` | 倒计时起点：`countdownSeconds` → **`CountdownSecondsForThisMatch`**（教程 10 s） |
| 6 | `:14160-14173` | `UpdateClockLabel` 开头**加回放局分支**：`Battle/HUD/Replay`.ToUpper() + 纯白 + return |
| 7 | `:15984-15986` | `EndPlayerTurn` 在 **`BeginTurn` 之后 / `ReportUnaskedChoices` 之前**调 `StopClockVisuals()` |
| 8 | `:32340-32488`（类尾） | 新成员：`_qpShownLastMe/_qpShownLastFoe` · `QuestDownVfxName` · `TickManaHolderValue` · `CountdownSecondsForThisMatch` · `TutorialCountdownSeconds` · `ReplayButtonTerm` · `IsReplayMatch` · `_countdownAnimGo` · `StopClockVisuals` |

**`StopClockVisuals()` 做三件**（与 `ClockManager.StopClock` 对齐）：`Kill(_countdownAnimGo)`（批处理走 `DestroyImmediate`）· `_clockInCountdown = false` · `UpdateClockLabel()`。

### 账应该怎么改（**建议，R1 执行**）

| 账行 | 建议 |
|---|---|
| 44/542 `ManaTypeHolder.DoTextAnim` | **C → A**，落点 `Battle/BattleDriver.cs:32412 TickManaHolderValue` / `:30815` 调用点；备注补「生效面 = 每场 2 颗 `QuestPointsHolder`（tween=1）；`ManaHolder` tween=0 ⇒ 能量数字不弹」 |
| 553/551 `ManaTypeHolder.TryDOVfx` | **C → A**，落点 `:32412` + `:32396 QuestDownVfxName`；备注补「升半由 `EvtKind.GainQuest/GainFaith` 事件链播，本轮只补降半 `Quest UI Trigger`」 |
| 412/410 · 413/411 `StopClock*` | **C → A**，落点 `:32481 StopClockVisuals` + `:15986` 调用点 |
| 395 `ClockManager.Initialize` | **C → A**，落点 `:32441 CountdownSecondsForThisMatch` |
| 407 `ClockManager.Start` | **C → A**，落点 `:32455 ReplayButtonTerm` + `:14166` |
| 41 · 256 · 365 · 368 · 374 · 387 · 400 · 413 | **C → A**（§②·六 的证据；批次 1 已落地，只是账没更新） |
| 148 · 150 · 157 · 158 | **C → D**（§②·五 的判据：`customObject` 空 ⇒ 原版 early-return） |

---

## ④ 断言 / 读数

**typecheck**（崩溃恢复后重跑两次，最后一次在全部改动之后）：
```
TMPDIR=/tmp/wf_r15 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

**行尾**（必须二进制读）：
```
io.open('Battle/BattleDriver.cs','rb') ⇒ CRLF 32488 / LF 32488 / 裸 CR 0   ✅
```

**静态读数（本轮的「灭自证」口）**：

| 读数 | 现值 | 为什么它能挡「改回去」 |
|---|---|---|
| `grep -c "_energyShownLast"` | **1**（只剩那句「原来这里有一格…」的更正注释） | 若有人把弹跳接回能量数字，这个计数或 `_energyLabel.transform` 会重新非零 |
| `grep -c "_energyLabel.transform"` | **0** | 同上（能量标签现在**不在任何 punch 路径上**） |
| `grep -n "DOPunchScale"` | 两处调用：`:6139`（镜头复位按钮，另一件）· **`:32422`（本轮的 `TickManaHolderValue`，唯一一处 HUD 数字弹跳）** | 弹跳只剩一条路，且它的两个调用点都在任务点那两行（`:30815/:30816`） |
| `grep -c "TickManaHolderValue"` | 5（1 声明 + 2 调用 + 2 注释） | 调用点只有任务点那一处 |
| `grep -c "StopClockVisuals"` | 2（1 声明 + 1 调用） | 晚于 `BeginTurn`、早于 `RefreshAll` |
| `grep -n "_clockInCountdown = false"` | 3 处：`ResetClock:13898` · 战后收尾 `:30481` · **本轮 `:32484`** | 原来「回合结束」那条路**没有**这一句 |
| `grep -rn "Battle/HUD/Replay"` | `Loc.cs:1271`（词条）＋ `BattleDriver.cs:14161/32453/32455`（本轮消费点） | 词条从「0 消费点」变成「有消费点」，且只在 `IsReplayMatch` 那一支 |

**`check_ledger_points.py 战斗界面_HUD`**（只读跑，**没有 `--fix`**）：
`A 394 条 · 通过 222（告警 8）· 漂移 172 · 失败 0`。
⇒ 漂移**全部**是 `BattleDriver.cs` 的行号（本代理在 `:1872` 与 `:13800-16000` 之间加了行，下游整体后移）；
**失败 0** 说明没有「落点压根不存在」的。R1 在同步点 `--fix` 一次即可自愈。

**没跑的**（按简报禁令）：Unity 批处理 · `_run_8_checks.sh` · 任何宿主自检。

---

## ⑤ 没查清 / 没做的

1. **`ManaTypeHolder.SetSavedMana` / `ToggleManaAccumulation`（账 535 / 546，P2-23）整条「存档能量/能量积攒」显示路径没做**。
   判据其实已经读全（`cpp2il …/ManaTypeHolder.cs:186-260` + `decomp_full/ManaTypeHolder__SetSavedMana.c`）：
   ① 按 `savedMana ≥ 1` 切 `manaAccumulationVFXON/OFF` 两个 GO；② 累积期数字显示 `unUsedMana − 1`；
   ③ `manaAccumulationImageOn` 的 `DOFade(option?1:0, 1f)` **带 0.5 s 延迟**；④ 关灯后 `DOVirtual.DelayedCall(1f)` 才把数字写回；
   ⑤ 音效 `manaAccumulated / manaAccumulatedUsed` 按 `Time.frameCount` 去重；⑥ `lastFrameAccumulatedSound` 是**静态**。
   **没做的原因 = 工作量**（一件 6 步的状态机 + 两件 VFX + 两条 audio cue），不是判据缺 —— 下一轮建议整件做。
   注：13 场现读 `ManaHolder ×2 / 8` 的 `manaAccumulationObj` 是**非空**（`749 / 1036`），所以这件**是活的**。
2. **`EndPlayerTurn` 里「对手掉线」那一支没有单独收倒计时**（原版 `StopClockUi` 的触发点在 `SetEnemyTurnUI`）。
   我们这一条只挂在「我结束回合」上；若将来有「对手掉线时表要停」的需求（现在 `NetClockPaused` 已管停表），
   要不要连带收特效 —— **没查**（不知道原版 `StopClockUi` 在掉线那一下有没有被调）。
3. **`Quest UI Trigger` 的位置口径**：原版 `Instantiate(prefab, 本 holder transform.position, …)` ⇒ 我们取
   `label.transform.position`（`QPText` 那一颗）。⚠️ **没有跑到实况核**这把「holder 中心 vs 文字中心」
   在屏幕上的差异（差几个像素，**没量**）。
4. **`CountdownSecondsForThisMatch` 没跑过教程局**（本工程教程局怎么进、`Ctx.MatchType` 是不是 100 —— 只从
   `RuleEngine/Core/BattleContext.cs:2012` 的枚举与 `BattleDriver.cs:17311` 的用法确认口径，**没有实跑**）。
5. **`_countdownAnimGo` 的「取最后一个新子件」是外推**：依赖 `WarpforgeEffectPlayer.Play` 的
   `Instantiate(entry.prefab, parent)`（`WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:292`）**同步**建一个子件。
   ✓ 前提已在代码里读到（`CardEffects.StampNewChildren` 用的是同一个前提）；⚠️ 若那件 prefab 内部再挂子件、
   或播放器改成异步加载 ⇒ 取到的可能不是它。**成本低的加固**：给 `CardEffects.Fire` 加一个「返回新实例」的窄口
   （`Core/` 不在本轮白名单，**没动**）。
6. **codex/oath 改判 D 只核了「数据 + C# 早退」两条**，没去跑 `ShowCodex` 的调用点看有没有别的入口给它塞过
   `CustomObject`（运行时改 SO）。按 `ShowCodex` 的签名（无参、只读 SO）判断**不可能**，但没有穷举调用点。

---

## ⑥ 顺手发现（⛔ 都不在本轮可改范围，记账）

1. 🔴 **`Core/VfxMap.cs` 缺「降」方向的两行**：表里只有升方向的 `GainFaith/GainSpirit/GainQuest`；
   原版 `vfxOnValueDown` 那一格是**另一件资产**（任务点 = `Quest UI Trigger`；信仰 = 空；能量/灵魂石 = 不播）。
   本轮把名字常量落在 `BattleDriver.QuestDownVfxName`（`Core/` 不在白名单）——
   **建议**后续并进 `VfxMap`，与那三行同表（否则以后做「灵魂石减少」又会另起一份）。
2. 🔴 **能量数字在原版里【不弹不闪】这件事，账里两说并存，容易被下一个代理读成「欠账」**：
   账 44/542 的备注已经写了「每场 8 个里只有 2 个 `useTweenOnValueChange=1`」，但**没有一句说「那 2 个是任务点、
   不是能量」**。本轮把这一句写进了代码注释（`:32350-32375` 那张表）与 §②·一；**建议账备注也补上**。
3. 🟡 **`BattleDriver` 里已经有一个文档条目也叫 `IsReplayMatch`**（`:17586` / `:17600` 是**注释**里的
   原版名字，不是成员）。本轮新增的 `IsReplayMatch` 属性（`:32461`）与它语义一致（回放局判据），
   但**没有把那两处注释改成「见 `IsReplayMatch`」** —— 因为改注释会再推一次行号，留给下一轮或 R1 合并时顺手做。
4. 🟡 **`ManaTypeQPHolder.SetMana` 还额外驱动 `questObjects[]` 那一排任务点灯**（`cpp2il …/ManaTypeQPHolder.cs:40-80`：
   先全灭、再按 `value` 逐颗 `ToggleCounter(true, 对象, silently)`）。账里 `ManaTypeQPHolder.ToggleCounter`
   已判 **A**，但我们这边到底是哪一处实现、是否也按「`value` 之前的都点亮」—— 本轮**没核**（不在我这次的入参里）。
5. 🟢 **`ClockManager.StopClock` 里还有两件我们没做、但判据为空的事**（不必做）：
   `CreateInOutAnim(inOutAnimPrefab)` 与 `noActionsAnim.SetActive(false)` —— 前者已由前人核过「13/13 场 prefab 全 null」，
   后者我们根本没有 `noActionsAnim` 这一件。**⛔ 别当欠账**。
6. 🟡 **`cpp2il` 的 `ClockManager.Start()` 里那句 `SetEndTurnText(isPlayer:true)` 与我读到的回放分支顺序**
   值得写进「判据要按语序读」的例子：先写普通文案、**再**覆盖成 Replay ⇒ 我们照这个顺序写在
   `UpdateClockLabel` 顶上（回放分支先 return），语义等价且更省。

---

## 交接给下一个代理

**做到哪**：本域 **P0 全部清完**（2 件已落地）；**P1 实际只剩 0 件**（4 件是批次 1 已做、账没更新；4 行是 codex/oath，
判据证明应改 D）。P2 落地 3 件（395 / 407 / 410+411）。`typecheck` 0/0，行尾纯 CRLF。

**剩下什么（按建议顺序）**：

1. 🔴 **账要让 R1 更新**（§③ 末尾那张对照表）：8 行 C→A、4 行 C→D。⛔ 我不动账（R1 独占）。
   ⚠️ 更新前后都跑一次 `python -I -X utf8 工具/check_ledger_points.py 战斗界面_HUD --fix`（现读漂移 172 / 失败 0）。
2. **下一件真活 = 账 535 / 546（`SetSavedMana` + `ToggleManaAccumulation`，P2-23）** —— 判据 §⑤·1 已经读全，
   只剩照做（6 步状态机 + 2 件 VFX + 2 条 audio cue）。它影响**每场那 2 颗 ManaHolder**（`manaAccumulationObj` 非空）⇒ 玩家看得见。
3. 再往下的可做项（都在 `BattleDriver.cs`，判据见账的 N4-triage 备注）：
   `241` 分数动画（要先读 `BattleScoreUiManager.ScoreUpAnimation` 的方法体参数）· `397` 切后台补时 ·
   `362/376` 回放自动选牌 · `187` 局内换语言重刷整棵 HUD（要 `Core/Loc` + `SettingsPanel`，**跨域**）。
4. `148/150/157/158` **不要做**（改判 D 的判据在 §②·五）；`167 bastion` 早已 D。

**踩过的坑（本轮新增，建议进 `CLAUDE.md` §三 或 `资料/已知的坑.md`）**：

- 🔴 **「序列化标志决定要不要播」**：同一段代码（`SetMana` 的两跳）在 4 个实例上**只有 2 个会动** ——
  读方法体时必须**同时读实例的序列化字段**，否则会把「原版本来不动」的地方做成「多出来的一下」。
  （本轮实证：批次 1 就是把弹跳接到了 `tween = 0` 的能量数字上。）
- 🔴 **`DOPunchScale` 那类「看起来是参数」的常量，`.rdata` 读出来要按形参类型选宽度**：
  `0x1834b2bb0` 4B = `0.2f`（对）、8B = `3.05e-05`（错）—— `read_literal.py` 会两档都打，**要自己按上下文选**。
- 🔴 **`CardEffects.Fire` 播完的东西要收，只能靠「播前后 `childCount` 差」**（`Play` 是 `void`，没有返回值）。
  这个手法前提是 `WarpforgeEffectPlayer.Play` 同步 `Instantiate`（`Runtime/WarpforgeEffectPlayer.cs:292`）——
  **前提若变，这段静默失效**（不回滚也不报错），要留意。

**文件归属**：本轮**只动** `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`。
`Hand/**`、`Board/**`、账、正本、`Editor/**` 一个字都没动。
