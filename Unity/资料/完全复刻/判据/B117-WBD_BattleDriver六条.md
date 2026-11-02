# B117-WBD · `BattleDriver.cs` 六条 + 跨文件连带（执行代理报告）

- 代理：`B117-WBD`（第一百一十七会话）
- 独占文件：`CardPresentation/Battle/BattleDriver.cs` · `Battle/BattleLogPanel.cs` · `Battle/CardDisplayWindow.cs`
- 新增：`CardPresentation/Editor/B117_WBD_Assert.cs`
- 日期：2026-11-03
- 类型：**执行报告**（⛔ 不写「已完成/未完成」；状态见 `活状态表.md`）

---

## 一、六件活逐条（旧 → 新）

### 1️⃣ `B117-31` · 过期注释

| 落点 | 旧 | 新 |
|---|---|---|
| `BattleDriver.cs:2417` | `GameplayVariables.SkirmishWarlordLifeChange = -10` | `= 0`（附「2026-11-03 现读；原值 −10 已作废」） |

- 现值判据：`GameplayVariables.cs:116`（`SkirmishWarlordLifeChange = 0`），2026-11-03 `B117_F6` 改。
- ⚠️ **另一处不归本代理**：`RuleEngine/Core/RuleCore.cs:645`（「经典 0 ⇒ 这一段一次都不走」）——
  `RuleCore.cs` 有别的写手约束，**本代理未碰**，请调度台转派。
- 断言：`B117_WBD_Assert.SecCommentDrift`（源级 + 两条常量判别式）。

### 2️⃣ `B117-38` · 阻塞窗口 = 整批事件 ⇒ 逐动作 flag

- 原版判据（一手）：`d:/2/tools/decomp_full/BattleManager__WaitingForBlockingAction.c:26-40`
  逐条扫三条动作道（`+0x350 / +0x358 / +0x368`），只有
  `actionType == 0x1b /* transformUnit */` 或 `blockPlayerActions(+0xF4) != 0` 才返回 1；
  字段偏移互证 = `d:/2/tools/il2cpp_out/dump.cs:118994`（`public bool blockPlayerActions; // 0xF4`，
  与反编译里 `local_14` = `local_108[61]` 之后第一个字节 = 244 = 0xF4 逐字节对上）。
- 改法：
  - **新增** `public static bool BattleDriver.BlocksPlayerActions(BattleEvent e)`（逐档映射 + 理由写在 doc 里）；
  - **新增** `public static bool BattleDriver.BatchBlocksPlayerActions(List<BattleEvent> buf)`；
  - `BattleDriver.cs:19727`：`if (_signalBuf.Count > 0) BeginBlockingWindow(t);`
    → `if (BatchBlocksPlayerActions(_signalBuf)) BeginBlockingWindow(t);`
- ⚠️ **时间线推进一个字没动**：`t` 的累加（`DelayBetween` + `DurationOf`）原样，非阻塞事件照常按各自时长播，
  只是**不锁输入**（窗口长度 0）。
- `BlocksPlayerActions` 的档位（左 = 我们，右 = 原版 `BattleActionType`）：
  - **挡**：`Play`(5) · `Deploy`(2/20) · `Attack`(1) · `Hit`(8/9) · `Death`(4) · `Ability`(30) ·
    `Heal`(10) · `Retaliation`(13) · `Return`(26/33) · `DiscardReveal`(28) · `StealReveal`(37/59) ·
    `SabotageCreated`(23 createHandCard —— 原版中央展示段真 `yield`)
  - **`Draw` 例外**：只有 `Effect == "display"` 才挡（原版 `displayFromHand(57)` 真阻塞 2.7 s）。
  - **不挡**：`Trigger` / `GainFaith` / `GainSpirit` / `GainQuest` / `QuestSpent` / `CollectWaystone` /
    `AmbushExit` / `SecretOrder` / `ShowCardsWindow` / `StatusApplied` / `TraitRemoved` / `EnchantmentRemoved`。
- ⚠️ **如实记**：`transformUnit(27)` 在**我们这一层没有对应事件种类**（引擎直调 `RuleCore.TransformUnit`，
  不是一条 `EvtKind`）⇒ 原版那两个 OR 条件里的第一个，在我们这里**无对应项**。
- 其它两个 `BeginBlockingWindow` 调用点（`OnPlayerDeployFlightStart` 的起飞拍、
  `NoteDeployFlightBlock`）**原样保留**（那是「牌已离手」那一段，原版同样是阻塞的）。
- 断言：`SecBlocking`（6 条纯函数 + 3 条源级；其中 ④ 是灭自证）。

### 3️⃣ `B117-48` · 点日志卡名 ⇒ 大卡信息窗（不是小预览卡）

- 原版链路（一手）：`CemeteryManager__ClickCardLink.c` 尾段
  `BattleManager__DisplayCard(manager, card, 1, 0)` + `HideCemeteryLogBtn(...)`；
  `BattleManager__DisplayCard.c` 尾段 `CardDisplayWindow__ShowBattleCard(window, card, matchData.gameplayData.warlordLifeChange, 0)`。
- 改法：
  - `BattleDriver.cs:12535`：`ShowLogCard(key, row);` → `ShowLogCardLarge(key);`
  - **新增** `bool BattleDriver.ShowLogCardLarge(string key)`（类尾）：查卡 → 教程闸
    （`TutorialAllowsCardDisplay`，原版 `DisplayCard.c:44-50` 的 `hideLargeCardDisplay`）→
    在场上就带 `EffectsToShow.Of` + 伏击三合一闸（`!mine && unit.Has(KeywordTable.Ambush)`）→
    `_cardDisplay.ShowBattleCard(cd, def, rows, enemyInPlayAmbush)`。
  - `ClickLogRow` 里**当场 `HideLogCard()`**：原版 `HideCemeteryLogBtn` 收的就是那张 `showingCardUI`；
    我们的大卡窗是另一个类 ⇒ 两张卡同时挂着 = 原版没有的画面。
- **悬停那条路不变**：`TickLogCard` → `ShowLogCard`（小预览卡 226×359.8）照旧。
- 断言：`SecLogClickSource`（4 条源级，含 1 条灭自证）。

### 4️⃣ `B117-49` · 点行命中区 = 行内 TMP 链接

- 原版判据：`CemeteryManager__ClickCardLink.c:56-70` —— 逐行 `TMP_TextUtilities.FindIntersectingLink`，
  未命中扫下一行；10 行全未命中 ⇒ 方法 `return`（不弹卡、不关面板）。
- 改法：
  - `BattleLogPanel` **新增** `int LinkRowAt(Vector3 wp, Camera cam)` ·
    `int LinkRowAtScreen(Vector2 screen, Camera cam)` · `bool LastLinkProbeDegraded` ·
    `bool ForceLinkProbeUnavailableForTest`（类尾）。
  - `OnBgPointerDown`（`BattleLogPanel.cs:625`）：`RowAtScreen` → `LinkRowAtScreen`。
  - `BattleDriver.TryClickLogRow`（`:12504`）：`_logPanel.RowAt(wp)` → `_logPanel.LinkRowAt(wp, cam)`，
    降级时**出声**（`Debug.Log`，⛔ 不静默）。
  - `ClickLogRowForTest` 改喂**链接上的点**（`FindLinkProbe`，扫不到才退回行心 ⇒ 走降级分支）。
- ⚠️ **降级路径（可测）**：`Core/TmpFont.LinkAt` 第一句是 `if (tmp == null || cam == null) return null`
  ⇒ 批处理 / Overlay 子画布取不到 `worldCamera` 时「没命中」与「测不了」同形。
  这一档**降级成整行矩形命中**并置 `LastLinkProbeDegraded`（自检可用 `ForceLinkProbeUnavailableForTest` 摆出来，
  不依赖环境）。
- 断言：`SecLogClickHitTest`（3 条源级 + 4 条运行时，其中 ② 是灭自证、③ 钉降级路径）。
  ⚠️ 运行时那段要**真面板** ⇒ 宿主必须是 `Editor/BattleScene.cs`。

### 5️⃣ `B117-30` · 详情窗显示 SO 原值（比实战少 10）—— 缺的是**接线那一跳**

- 现场：窗那一半（`CardDisplayWindow.SetWarlordLifeChange` / `ApplyWarlordHealthModifier` /
  `BuildFan` 里 `i == 0 && Type == "hero"`）**2026-11-02 已由 `F12` 做完**，
  但**全仓 0 个生产调用点** ⇒ 增量恒 0 ⇒ 详情窗仍显示 SO 原值。
- 改法：`BattleDriver.cs:5080`（`Begin` 里 `_vars = vars ?? GameplayVariables.Classic;` 那一行）
  同一物理行追加 `CardDisplayWindow.SetWarlordLifeChange(_vars != null ? _vars.warlordLifeChange : 0);`
  （⇒ **零行数变化**）。
- 判据：`BattleManager__DisplayCard.c` 把 `matchData(+0xA8).warlordLifeChange(+0x20)` 当第 3 实参传给
  `ShowBattleCard`；`GameplayVariablesData +0x20 = warlordLifeChange`（`dump.cs:130170+`）。
- 断言：`SecWarlordWiring`（源级 + 写口/读口往返）。**窗那一半的行为断言已由 `B117F12_Assert.Run` 覆盖**（勿重复）。

### 6️⃣ `B117-40` · 回放加速 + 暂停 = 跳过动画（**只做 BattleDriver 这一半**）

- 原版判据（一手）：
  - `BattleManager__ReplaySkipAnims.c`：`matchType(+0x1c) != 0xA0 ⇒ 0`，否则返回 `+0x521`；
  - `ClickPauseReplay.c:10` 写 `+0x521 = 1`、`ClickPlayReplay.c:10` 清 0 ⇒ **暂停 = 打开跳过动画**；
  - `CardScript__get_AttackStepTime.c`：为真时 `attackStepTime / DAT_1834b2e8c`，常量实读 **3.0**
    （= 现值 `EventTiming.ReplaySkipAnimDivisor`）。
- 改法（全部在 `BattleDriver.cs`）：
  - **新增** `public bool ReplaySkipAnimsNow => _replaySession && _replayPaused;`（类尾）；
  - **新增** `float ReplayScaledAttackStep(float step)` · `Tween NoteAttackTween(Tween)` ·
    `void ApplyReplaySkipToLiveAttackTweens(bool)` · `readonly List<Tween> _attackTweens`（类尾）；
  - `SetReplayPaused`（`:8457`）追加 `ApplyReplaySkipToLiveAttackTweens(on);`（同一物理行）；
  - `AttackMotionDurationOf` 改实例口 + 走 `ReplayScaledAttackStep(CardFeel.AttackStepTime)`；
  - 三处攻击补间登记：`CardFeel.MeleeAttack(...)` / `CardFeel.AttackRecover(...)`（`:22767` 守方那支、
    `:25280` 攻方远程、`:25301` 攻方近战）⇒ 包一层 `NoteAttackTween(...)`；
  - `float vfxAt = ... AttackStepRanged : MeleeDamageLag` → `ReplayScaledAttackStep(...)`；
    `plan.At = AttackStepRanged` → `ReplayScaledAttackStep(...)`；
  - `PlayBucketOffsetOf` 的准星 ÷3 门：`_replaySession` → `ReplaySkipAnimsNow`
    （原来只取了 `ReplaySkipAnims` 的一半 —— 正常回放也会被 ÷3）。
- ⚠️ **如实记（未根治的残差）**：`Core/CardFeel.cs` 里 `MeleeAttack` / `AttackRecover` 的**整条序列**
  共用 `AttackStepTime` / `AttackReturnTime` 两个常量，而 `CardFeel.cs` **不在本代理可改范围**
  ⇒ 我们只能对**整条补间**打 `t.timeScale = 3`：近战 0.4 → **0.133**，原版跳过档是
  `0.0333 + 0.1(段3 的 WaitForSeconds 不除) + 0.0667` = **0.2** ⇒ 残余 **0.067 s**（远程同理 0.133）。
  要根治：让 `CardFeel.MeleeAttack/AttackRecover` 收一个 `stepScale` 形参（`1` = 现状），
  序列里两处 `AttackStepTime` 用它、`AttackHoldTime` 不用。
- 断言：`SecReplaySkip`（4 条运行时状态机，其中 ② 是灭自证 + 5 条源级）。

---

## 二、给 `WBT` 的 `EventTiming` 可粘贴补丁（⛔ 本代理没碰 `EventTiming.cs`）

> 现状：`EventTiming.cs:742-743` 的 `if (e.Kind == EvtKind.Attack) return e.Ranged ? AttackStepRanged : AttackStepMelee;`
> **不除**；而 `BattleDriver` 那一半已经按 ÷3 走了 ⇒ 时间线与画面会差 0.067–0.133 s/刀。
> 做法 = 加一个**实时 hook**（与本文件既有的 `HitIsVulnerable` / `ReturnDestOf` 同一种写法），
> 驱动只需在 `Begin` 里赋一次值（`ReplaySkipAnimsNow` 自己读两个实时字段）。

```csharp
// ── ① 在 `HitIsVulnerable`（EventTiming.cs:113）那一组 hook 旁边追加：
/// <summary>回放「跳过动画」档 = 原版 `BattleManager.ReplaySkipAnims()`
/// （`matchType == 0xA0` ∧ `+0x521`；`ClickPauseReplay` 置 1、`ClickPlayReplay` 清 0）。
/// 真时「攻击前摇」要 ÷ <see cref="ReplaySkipAnimDivisor"/>（`CardScript.get_AttackStepTime`）。</summary>
public static System.Func<bool> ReplaySkipAnims;

// ── ② 在 `ReplaySkipAnimDivisor`（EventTiming.cs:328）后面追加：
/// <summary>跳过档生效时把一段「攻击前摇」÷3；未生效时**原样返回**（逐位不变）。</summary>
static float SkipScaled(float seconds)
{
    return (ReplaySkipAnims != null && ReplaySkipAnims()) ? seconds / ReplaySkipAnimDivisor : seconds;
}

// ── ③ `DurationOf(BattleEvent)`（EventTiming.cs:742-743）：
            if (e.Kind == EvtKind.Attack)
                return SkipScaled(e.Ranged ? AttackStepRanged : AttackStepMelee);

// ── ④ `DurationOf(EvtKind)`（EventTiming.cs:638）：
                case EvtKind.Attack: return SkipScaled(AttackStepMelee);

// ── ⑤ `DelayBetween`（EventTiming.cs:405）—— 近战「剩下那一截」也要跟着缩：
                    if (!prev.Ranged) return SkipScaled(MeleeDamageLag) - SkipScaled(AttackStepMelee);

// ── ⑥ 同族两处（要做就一起做，否则会出现「Attack 缩了、Hit 没缩」的错位）：
//     · EventTiming.cs:707  `case EvtKind.Retaliation: return SkipScaled(AttackStepRanged) + RangedVfxFallback;`
//     · EventTiming.cs:802  `HitFrameLag(e)` 里那一族 `AttackStep*` 同上
//     · EventTiming.cs:937-938 `DeployHoldOf` 的 `HandPlayed / AutoPlayFromHand` 两档（用的是 `AttackStepMelee`）
```

```csharp
// ── ⑦ 驱动侧接线（一行，放 `BattleDriver.Begin`，也可放在 `Setup` 的静态初始化那里）：
EventTiming.ReplaySkipAnims = () => ReplaySkipAnimsNow;
```

⚠️ 引用 `EventTiming.AttackStep*` 的地方**不止上面这些**（例如 `CardFeel` 自己也用 `AttackStepTime`），
`⛔ 别用「全局乘 1/3」的做法** —— 原版只除 `attackStepTime` 这一个字段读出来**的那几处**。

---

## 三、跨文件连带（调度台 2026-11-03 中途追加）：AI 登场 clip 的起播相位

- 症状：`WBT` 把登场序列改成交接 0.95 / 落地 1.20 / 收尾 1.30 之后，
  `StartHandToBoardClip(v)` 写在 `Handoff.Apply` 里 ⇒ clip **0.95 才起播、跑到 1.867**，
  内部相位整体后错 0.65 s（比整条序列还长）。
- 选择：**(b) 给 clip 留一个「从 0.65 偏移起播」的窄口**（⛔ 不是 (a)）——
  理由：`Apply` 那一刻才 `RestoreBoardViewVisibility` + `SetPose`，0.3 s 时**场上视图还是藏着的**
  （先起播就是「整条 clip 演给一个隐藏对象看」）。
- 实现（全部在 `BattleDriver.cs`）：
  - `void StartHandToBoardClip(CardView card, float seekSeconds = 0f)`（`:13926`）；
  - `:13946` 同一物理行追加 `else if (tw != null && seekSeconds > 0f) tw.GotoWithCallbacks(seekSeconds);`
    —— **必须是 `GotoWithCallbacks`**：`Goto` 会跳过 clip 在 0.55 的那条
    「`SetDissolveAmount(0)` / `RestoreOriginalMaterial()`」回调 ⇒ 溶解材质停在半途（静默）；
  - 三个 `Handoff.Apply` 调用点（`:25928` / `:26023` / `:37924`）传
    `DeploySequence.HandoffTime - DeploySequence.MoveTime`（= 0.65，**现算、不写死**）；
  - 玩家那条路（`:13842` 的 `ScheduleAction(DeploySequence.MoveTime, ...)`）**一字未动**。
- 注释漂移一并订正（**逐行替换、行数守恒**，全文共 **30 行**；见下面的清单）：
  - 0.4 s → **0.95 s**：`:5737` `:13846` `:13851` `:13855` `:13860` `:13884` `:13900` `:13910` `:14187`
    `:20195` `:20654` `:25907` `:26009` `:37885`
  - 0.75 s → **1.30 s**：`:13802` `:13808` `:13867` `:13902` `:18619` `:20930` `:21476` `:24482`
    `:25904` `:26535` `:38693`
  - 数值拆解逐项订正：`:13850`（`0.3 + HandToBoardDone 0.55 + 0.1 = 0.95`）、
    `:21477`（`MoveTime 0.3 + HandToBoardDone 0.55 + TimeToScaleIntoMoving 0.1 + HoldTime 0.05 +
    LandTime 0.2 + LandTailTime 0.1 = 1.30`）
  - 「这一格在这 0.4 s 里被换掉了」→ 0.95 s：**三条 `Handoff.Apply` 各一处**（`:25926` / `:26021` / `:37921` 一带）
- 断言：`SecDeployClipSeek`（4 条源级，含「三条都传了偏移」「用 `GotoWithCallbacks` 而不是 `Goto`」
  「玩家那条路不传偏移」）。

---

## 四、闸

| 闸 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_wbd bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `python -I -X utf8 工具/check_aggregate_lines.py`（三份文件） | 聚合行 0 · 调用 0 · 违规 0 ⇒ **PASS** |
| `python -I -X utf8 工具/check_ledger_points.py` | 输出里 **0 处**提到 `BattleDriver.cs` ⇒ 无漂移/无失败 |
| 行尾 | `BattleDriver.cs` = 40,718 行，CRLF 40,718 / LF 40,718 / **裸 CR 0** |

---

## 五、⚠️ 需要调度台处理的三件事

1. **`Editor/BattleScene.cs:7203` 会变红（预期）**：那里断言
   `drv.LogHoverCardKey == wantKey`（「点行之后**小预览卡**还弹着」）——
   按 `B117-48` 的判据，点行现在弹**大卡窗**、小预览卡当场收掉。
   建议改成断 `drv.CardDisplay.Visible` + `drv.CardDisplay.ShownTitle`（卡名）非空。
   ⛔ 本代理按红线**未改任何 `Editor/*Scene.cs`**。
2. **`Editor/BattleScene.cs:7176-7210` 那一节的注释**（「命中区 = 整行底板矩形，比原版宽」）**已过期** ——
   `B117-49` 之后命中区就是链接（带降级）。同样未改（红线）。
3. **`RuleCore.cs:645`** 那处过期注释（`SkirmishWarlordLifeChange` 的「经典 0」说法）—— 不归本代理。

---

## 六、没做成 / 判据不足（如实记）

1. **`transformUnit(27)` 在我们这一层无对应项**（不是漏做）：原版 `WaitingForBlockingAction` 的第一个 OR 条件
   在我们这里没有事件载体（引擎直调 `RuleCore.TransformUnit`）。
2. **整条攻击补间 ÷3 的残差 0.067 s**（见 §一·6 末段）：要根治必须让 `Core/CardFeel.cs` 收 `stepScale` 形参，
   而它不在本代理白名单里。
3. **`BattleLogPanel` 降级判据只覆盖了「无相机」这一半**：`TmpFont.LinkAt` 在 `_tmp == null` /
   未生成字形时也返回 null，但从 `BattleLogPanel` 侧**拿不到** `Label._tmp`（`Label.cs` 不在白名单，
   没有公开读口）⇒ 那一半只能靠 `ForceLinkProbeUnavailableForTest` 模拟，**不是**真环境探测。
4. **未跑任何 Unity 自检**（红线：本代理不跑批处理）；断言**尚未接线**（由调度台统一接，
   `SecLogClickHitTest` 那段要求宿主 = `Editor/BattleScene.cs`）。
