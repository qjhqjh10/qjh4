# `ANIM-TRIGGER` 接进 `BattleDriver.PlaySignals` —— **零歧义补丁**（只读产出）

> 执行写手 `B143-W1` · 2026-11-05 · 上游判据 = `判据/B143_四条待判据.md` §1（`B142-ANIM-TRIGGER-COVER`)
> ⛔ 本文件**不改任何源码**（`BattleDriver.cs` 归 `W6`）。落地由调度台转派。
> ✅ 行号 = 2026-11-05 现读；**按符号找**（每节给了 grep）。

---

## 〇 · 现状（比施工单写的时候又前进了一步）

| 件 | 现读 |
|---|---|
| 表 | ✅ **已在盘上**：`Assets/CardPresentation/Resources/card_anim_order_trigger.json`（`CardAnimOrder.TriggerResourcePath = "card_anim_order_trigger"`，`:496`；`EnsureTriggerLoaded()` `:521`） |
| 查询口 | ✅ **已实现**：`CardAnimOrder.TryGetTriggerBeats(string cardIdOrName, int trigger, int abilityIndex, int logicIndex, List<CardAnimBeat> into)`（`:580`；另有 2 个少参重载 `:561` / `:571`）。⛔ **别再新建一份** |
| 引擎侧 | ✅ `EvtKind.Trigger` 的 `Keyword` 已经带 `#<ai>` 后缀：`EffectResolver.cs:8521` `Keyword = BattleEvent.KeywordWithAbility(triggerName, abilityIndex)`；解码**唯一处** = `BattleEvent.SplitKeywordAbility(keyword, out ai)`（`BattleEvent.cs:746`）；另有 `BattleEvent.AbilityIndex`（`:729`） |
| 消费端 | ❌ **仍只接 `EvtKind.Ability`**：`BattleDriver.cs:19689` `if (e.Kind == EvtKind.Ability && ExpandAbilityBeats(e, t))` ⇒ `Trigger` / `Play` 覆盖 **0** |

⇒ **本补丁只做 `EvtKind.Trigger` 这一支**（施工单 §1·5 第 1 条 + 调度台口径：**Trigger 与 Play 分开立项**；`Play` 排在 `Deploy` 之前的重叠问题没在真机看过，**先不开**）。

---

## 1 · 现读锚点与触发链

grep：
```bash
grep -n "if (e.Kind == EvtKind.Ability && ExpandAbilityBeats"   CardPresentation/Battle/BattleDriver.cs
grep -n "bool ExpandAbilityBeats"                               CardPresentation/Battle/BattleDriver.cs
grep -n "readonly List<CardAnimBeat> _beatBuf"                  CardPresentation/Battle/BattleDriver.cs
grep -n "public int AbilityBeatsExpanded"                       CardPresentation/Battle/BattleDriver.cs
```

**谁调 `PlaySignals`**：`RefreshAll()` 的第一句（`BattleDriver.cs:19467` 附近 `PlaySignals();`）；`RefreshAll` 由
`ApplyLoggedAction` / 每帧 `TickReplayPump` 那条链驱动（`:1220` / `:1434` / `:2255` 的注释都在说这条）。

**它做什么**：`Ctx.DrainSignals(_signalBuf)` → 逐个事件算 `t`（时间线上的绝对秒）→ `_timeline.Add(PendingSignal)`。
`t` 的推进 = `t += EventTiming.DurationOf(e)`；`EvtKind.Play` 在 `DurationOf(EvtKind)` 里**没有 case ⇒ 0**，
而 🔴 **`EvtKind.Trigger` 不是 0**：`EventTiming.cs:681` `case EvtKind.Trigger: return TriggerLen;`，`TriggerLen = 0.2f`（`:993`）
—— 那是 `_ResolveTriggerTraitAnim` 那条链的等待（`:1207` 的注释）。
⇒ **展开后不能让 `t` 只加拍时长**（那会把今天那 0.2 吃掉），见 §2·2 的 `Mathf.Max`。

**既有展开支（本补丁的模板）**：
```csharp
// BattleDriver.cs:19689 附近（现读原文）
                if (e.Kind == EvtKind.Ability && ExpandAbilityBeats(e, t))
                {
                    t += EventTiming.DurationOf(e);
                    _timelineTailAt = Mathf.Max(_timelineTailAt, t);
                    prev = e;
                    continue;
                }
```
`ExpandAbilityBeats(BattleEvent e, float t0)`（`:19829`）的形状：`_beatBuf.Clear()` → `TryGetBeats` → 逐拍
①`BeatSlotGate` 门 ②多目标拍无目标 ⇒ `break` ③逐目标 `_timeline.Add(PendingSignal{beat=true,…})` ④`wait` 累加 `acc`；
全部被挡 ⇒ **仍排一条空拍**（事件级副作用照响）。

---

## 2 · 补丁（三处，`BattleDriver.cs`）

> 🔴 **必须先读这一节再抄**：`BattleDriver` **不是 `partial`**（`:22` `public class BattleDriver : MonoBehaviour, …`），
> 且账在**它后面那个类**里也有落点（现读最深 `BattleDriver.cs:42085`）⇒ **类尾追加也会漂**。
> ⇒ 下面按本文件一贯的「**同物理行追加**」写（**零行数变化**）；若调度台愿意在同步点跑
> `check_ledger_points --all-A --fix`，也可以按**正常多行**插入（见 §2·4 的 B 案）。

### 2·1 缓冲与计数器（**同行追加**，两处）

改前 `:19795`：
```csharp
        readonly List<CardAnimBeat> _beatBuf = new List<CardAnimBeat>();
```
改后（同一物理行追加；⚠️ 该行**没有** `//` 注释，可直接加）：
```csharp
        readonly List<CardAnimBeat> _beatBuf = new List<CardAnimBeat>(); readonly List<CardAnimBeat> _trigBeatBuf = new List<CardAnimBeat>();
```

改前（三个计数器属性，`AbilityBeatsExpanded` / `AbilityBeatsSlotSkipped` / `AbilityBeatsNoTargetSkipped` 各一行）：
把**三个新计数器**并到 `AbilityBeatsNoTargetSkipped` 那一行的末尾（同一物理行）：
```csharp
        public int AbilityBeatsNoTargetSkipped { get; private set; } public int TriggerBeatsExpanded { get; private set; } public int TriggerBeatsSlotSkipped { get; private set; } public int TriggerBeatsNoTargetSkipped { get; private set; }   // 🔴 2026-11-05（B143 · ANIM-TRIGGER）：触发/出牌能力那一支的读数口（与 Ability 那三个一一对应；自检直接读）
```

### 2·2 `PlaySignals` 里加并列支（**在 `EvtKind.Ability` 那一段【之后】**，⛔ 不改它那道闸）

改前（现读原文，`:19689` 起 7 行）：
```csharp
                if (e.Kind == EvtKind.Ability && ExpandAbilityBeats(e, t))
                {
                    // 整条事件**仍占它自己那段**（`DurationOf` = 该卡技能总时长，见 `AbilityAnimDurations`）——
                    // 展开出来的 N 拍排在这一段里，后面的事件照旧按原来的节拍跟上。
                    t += EventTiming.DurationOf(e);
                    // 🆕 2026-10-19（偏差 3）：展开出来的拍也算进「队尾」（`ExpandAbilityBeats` 里逐拍记过）。
                    _timelineTailAt = Mathf.Max(_timelineTailAt, t);
                    prev = e;
                    continue;
                }
```
改后（**在它后面紧接一段**；A 案 = 整段压在同一行的前后两行都合法，推荐照下面排成 8 行但**不改任何既有行**）：
```csharp
                if (e.Kind == EvtKind.Trigger && ExpandTriggerBeats(e, t, out float trigSpan))
                {
                    // 🔴 2026-11-05（B143 · ANIM-TRIGGER）：触发能力**各播自己那一行 `cardAnimations[]`**
                    //   （判据 `AbilityLogic__PlayAnim.c:45-60` 枚举的是 `AbilityLogic.cardAnimations(+0x18)`；
                    //    `PlayAbility.c:273/1531/2465` 三处调用 ⇒ 每一条命中的 logic 行各播一份）。
                    //   ⚠️ 今天 `EvtKind.Trigger` 的时长**不是 0**：`EventTiming.cs:681` `return TriggerLen` = **0.2 s**
                    //      （`:993`）。⛔ 别用 `trigSpan` 直接顶掉它 —— 取 `Mathf.Max` 保住今天那条 0.2 的下限，
                    //      拍比 0.2 长时才按拍的（这同时是「零回归」的保证）。
                    t += Mathf.Max(EventTiming.DurationOf(e), trigSpan);
                    _timelineTailAt = Mathf.Max(_timelineTailAt, t);
                    prev = e;
                    continue;
                }
```
> ⚠️ 位置敏感：必须在 **`if (e.Kind == EvtKind.Attack) { … _batchAttackSkillAnimAt = … }` 那段之后**、
> 且在 `_timeline.Add` 的**通用出口之前**（照现读顺序 = `Attack` 段之后紧跟 `Ability` 段，把新段插在两者之间或其后都行；
> 建议**紧跟在 Ability 段之后**，语义分组最清楚）。

### 2·3 新方法 `ExpandTriggerBeats`（**逐行照抄** `ExpandAbilityBeats`，只换「查哪张表 / 取哪个 trigger」）

> 零漂移落地方式：把下面**整个方法压成一条物理行**，追加在 `ExpandAbilityBeats` 的**收尾 `}`** 那一行之后
> （那一行 `}` 后面现在什么都没有 ⇒ `} bool ExpandTriggerBeats(...) { … }` 合法）。这与本文件既有风格一致
> （文件里有 3000+ 字符的行）。

```csharp
        bool ExpandTriggerBeats(BattleEvent e, float t0, out float span)
        {
            span = 0f;
            string tn = BattleEvent.SplitKeywordAbility(e.Keyword, out int aiSuffix);   // ⛔ 解码只此一处
            int ai = aiSuffix >= 0 ? aiSuffix : e.AbilityIndex;
            int trig = CardDef.AbilityTriggerNumber(tn);
            if (trig < 0) return false;                       // 认不出 trigger 名 ⇒ 走原样（⛔ 不猜）
            // ⛔ 排除**已经专用化**的三族（否则双播）：UnitAttack(50) `PlayUnitSkillAnim` ·
            //    UnitDamaged(60) `PlayDamaged*` · 死亡族 80/180 `PlayDeathAbilityAnims`。
            if (trig == 50 || trig == 60 || trig == 80 || trig == 180) return false;

            _trigBeatBuf.Clear();                             // 🔴 **复用缓冲**：不 new、不清 GC
            int n = CardAnimOrder.TryGetTriggerBeats(e.CardId, trig, ai, -1, _trigBeatBuf);
            if (n <= 0) return false;                         // 表里没有这一行 ⇒ 走原样（`TryGetTriggerBeats` 已出声一次）

            CardView self = ViewAt(e.Player, e.Slot);
            CardView target = (e.TargetPlayer >= 0 && e.TargetSlot >= 0) ? ViewAt(e.TargetPlayer, e.TargetSlot) : null;
            var tgtList = e.AbilityTargetList;
            System.Func<CardAnimBeat, List<BattleEventTarget>> targetsOf = bb => BeatTargetsOf(bb, e, tgtList);
            int optAbilityTarget = (int)PositionOption.abilityTarget;

            float acc = 0f; bool first = true;
            for (int i = 0; i < n; i++)
            {
                var b = _trigBeatBuf[i];
                if (b == null) continue;
                var bt = targetsOf(b); int targetCount = bt != null ? bt.Count : (target != null ? 1 : 0);
                if (!BeatSlotGate(b.sp, b.ep, e.Player)) { TriggerBeatsSlotSkipped++; continue; }
                if ((b.sp == optAbilityTarget || b.ep == optAbilityTarget || b.pt == optAbilityTarget) && targetCount == 0)
                {
                    TriggerBeatsNoTargetSkipped++;
                    BeatSoundOff($"notarget:{e.CardId}:{b.ca}",
                        $"`{e.CardId}` 的 trigger {trig} 有一拍是**多目标拍**且这条事件没带目标 ⇒ 原版 `PlayAnim.c:172-176` 是 LogError + **整段 return**（我们 `break`，同 `ExpandAbilityBeats`）");
                    break;
                }
                bool usesTgt = b.sp == optAbilityTarget || b.ep == optAbilityTarget || b.pt == optAbilityTarget;
                int nTgt = (usesTgt && bt != null && bt.Count > 0) ? bt.Count : 1;
                for (int ti = 0; ti < nTgt; ti++)
                {
                    var sig = new PendingSignal
                    {
                        evt = e, at = t0 + acc, drawFromEnd = -1, points = null, pointIndex = -1,
                        beat = true, beatVfx = b.vfx, beatSp = b.sp, beatEp = b.ep, beatPt = b.pt,
                        beatTail = !first || ti > 0, beatMove = b.mv != 0, beatOtv = b.otv != 0,
                        beatTs = b.ts, beatTm = b.tm, beatEase = b.ease, beatData = b,
                        beatTgtPlayer = -1, beatTgtSlot = -1, lane = e.Priority,
                    };
                    if (usesTgt && bt != null && bt.Count > 0)
                    { var t = bt[ti]; sig.beatTgtPlayer = t.Player; sig.beatTgtSlot = t.Slot; sig.beatTgtInstance = t.InstanceId; }
                    _timeline.Add(sig);
                }
                _timelineTailAt = Mathf.Max(_timelineTailAt, t0 + acc + Mathf.Max(0f, b.Duration));
                first = false;
                if (b.Waits) acc += Mathf.Max(0f, b.Duration);
            }
            if (first)   // 全被门挡掉 ⇒ **仍排一条空拍**：语音/音效/手感照响一次（否则这条 Trigger 整条静默）
                _timeline.Add(new PendingSignal { evt = e, at = t0, drawFromEnd = -1, points = null, pointIndex = -1,
                                                  beat = true, beatVfx = null, beatTail = false, lane = e.Priority });
            TriggerBeatsExpanded++;
            span = acc;
            return true;
        }
```
**四个要点（缺一个就是错）**
1. **`span` 只累计 `Waits` 的拍**（与 `ExpandAbilityBeats` 的 `acc` 同一份口径）；推进 `t` 时用
   `Mathf.Max(EventTiming.DurationOf(e), span)` —— **`Trigger` 今天有 0.2 s 的 `TriggerLen`**，⛔ 别把它吞掉。
2. **同卡同 trigger 多行必须靠 `(ai, li)` 区分** —— 表里 `ai` = `cardAbilities` 下标、`li` = `abilityLogic` 下标。
   本补丁只传 `ai`（`logicIndex = -1` ⇒ 该 ability 下**所有 logic 行**按表序拼，= 原版「遍历全部命中行」）；
   ⛔ **不能靠 `ca` 反查**（`CardAnimOrder.cs:568-578` 的 doc 已经写明跨行会重复）。
3. **缓冲复用**：`_trigBeatBuf` 只在声明处 `new` 一次，方法里 `Clear()` + `AddRange`（`TryGetTriggerBeats` 内部是 `into.AddRange`）。
   ⚠️ **不要**在 `PlaySignals` 的循环里 `new List<…>` —— 那是热路径（一局上百条 Trigger）。
4. `span = acc`（只累计 `Waits` 的拍）—— 与 `ExpandAbilityBeats` 里 `acc` 的口径**同一份**。

### 2·4 落地方式二选一

| 案 | 做法 | 代价 |
|---|---|---|
| **A（推荐，零漂移）** | §2·1 / §2·2 / §2·3 全部按上面写的**同物理行 / 单行方法**追加 | 方法是一条很长的行（可读性差，但本文件已有先例）；**账零漂移**，同步点不用 `--fix` |
| **B（可读）** | 正常多行插入（`ExpandTriggerBeats` ≈ +65 行，插在 `:19917` 一带） | `BattleDriver.cs` 现有 **全部 >19917 的账落点整体 +65**（现读最深 `:42085`）⇒ **必须**由调度台在「没有 `.cs` 写手」时跑 `check_ledger_points --all-A --fix`，否则账闸一片红 |

---

## 3 · 可观测性（「怎么知道它真的播了」）

三层，缺一不可 —— **只看其中一层都会假绿**：

| 层 | 判据 | 怎么读 |
|---|---|---|
| ① **表命中** | `CardAnimOrder.TriggerFallbackHits` **不涨**；`TriggerLastFallback` 为空 | 现成的诊断计数器（`:603-607`），命中一次就 `Debug.Log` 一次（`WarnCap` 上限 16） |
| ② **真的排进时间线** | `driver.TriggerBeatsExpanded` **+N**；`TriggerBeatsSlotSkipped` / `TriggerBeatsNoTargetSkipped` 各记各的 | 本补丁新增的三个 `public … { get; private set; }`（自检直接读，和 `AbilityBeatsExpanded` 同族） |
| ③ **真的播到画面** | **`AnimTrace` 里出现 `kind = "Trigger"` + `cueKind = "beat"` + `piece = beatVfx` + `first = true` 的那一行** | `PendingSignalQueue.Trace`（`:39299`）在每个 `PendingSignal` 入队时写；`piece` 取 `beatVfx`、`hold` 取 `beatData.Duration` ⇒ **这一行存在 = 那一拍真的进了播放队列、带着自己的 prefab 与时长** |

**做真机验收时的最小读数**：打一张 `ThisCardPlayed` 有动画的卡（`EC29 Heldrake` = 1 拍），看
`AnimTrace` 里多出 `EC29 / Trigger / <prefab 名> / beat` 一行，且 `TriggerBeatsExpanded` = 1。
**灭自证**：把 `CardAnimOrder.TryGetTriggerBeats` 的 `trigger` 实参改成 `-999` ⇒ 三层**同时**该有反应
（① `TriggerFallbackHits`+1 ② `TriggerBeatsExpanded` 不动 ③ `AnimTrace` 没有 beat 行）——三条都在 = 观测链是活的。

---

## 4 · 风险 / 落地前要瞄一眼

1. **`Play` 这一支没做**（有意）：单位出牌会先发 `Play`（`RuleCore.cs:4092`）再发 `Deploy`（`:4196`）⇒
   展开的 ThisCardPlayed 拍会与部署飞行**重叠**。施工单 §1·5 第 1 条建议先只在真机看一张，再单独立项。
2. **三族排除**（`50 / 60 / 80 / 180`）必须真排除，否则与 `PlayUnitSkillAnim`（`:23326`）、
   `PlayDamaged*`（`:22649` / `:40179`）、`PlayDeathAbilityAnims`（`:23241`）**双播**（同一事件两张动画叠着播）。
3. **`_batchAttackSkillAnimAt` 回填**（`:19752-19762` 那段 for）会把 `skillAnimAt` 贴到**所有 `Kind == Trigger` 的
   `PendingSignal`** 上 —— 包括本补丁展开出来的拍。落地时**现读确认 `PlayAbilityBeat` 不读 `skillAnimAt`**
   （`PlaySignal` 的事件拍才读）⇒ 若不读则无影响；若读，就在那段 for 里加一句 `if (ps.beat) continue;`
   （现在没有这句，是**特意留给落地写手现读**的）。
4. **`TriggerRowCount` 为 0 但表里"有卡"** 的三种正常原因（无该 trigger / 每拍 `ca` 空 / 不在表里）——
   `TryGetTriggerBeats` 的 fallback 日志已经写清，落地后**别把那条日志当报错**（它是「走原样」的正常路径）。
5. 施工单 §1·4 ④ 的断言（`cards=726 / rows=911 / beats=1178`）与 §1·5 第 2 条（`CardAnimOrder:356-360` 那句注释要改）
   **不在本补丁范围**，但落地那批应一起收。
6. 🔴 **`Trigger` 的时长不是 0**（`TriggerLen = 0.2 s`，`EventTiming.cs:681` / `:993`）⇒ 展开后**别让 `t` 只加拍时长**：
   本补丁写的是 `Mathf.Max(EventTiming.DurationOf(e), trigSpan)`。这一条是**现读订正**（施工单 §1·4 ③ 第 2 条写的是
   「`Trigger`/`Play` 在 `EventTiming` 里是 0 时长」——**对 `Play` 成立、对 `Trigger` 不成立**）。
   落地时若要「逐字照施工单」，那 0.2 会被吞掉，整批 Trigger 的后续事件会提前 —— 表现为「动画还没播完下一件事就来了」，**且不报错**。

---

## 5 · 一句话交付

改 **3 处**（缓冲/计数器同行追加 · `PlaySignals` 加 8 行并列支 · 新增 `ExpandTriggerBeats` 一条单行方法），
**只接 `EvtKind.Trigger`**、**排除 50/60/80/180**、**复用 `_trigBeatBuf`**、**用 `AnimTrace` + 三个计数器做可观测性**；
零漂移（A 案）或 +65 行 + 一次 `--fix`（B 案）。
