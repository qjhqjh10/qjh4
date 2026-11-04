# B141_W128_SAN27RowId — 事件按行承载（`B141-SAN27-ROWID`）

> 执行写手 `B141-W43` · 2026-11-04 · 批次 `B141` · 会话第一百二十八。
> 白名单（✅ 已按此执行）：`工具/gen_card_anim_order.py` · `CardPresentation/Battle/CardAnimOrder.cs` ·
> `RuleEngine/Core/BattleEvent.cs` · `CardPresentation/Battle/BattleDriver.cs` ·
> 新建 `RuleEngine/Editor/B141_W43_Assert.cs` · 新建本报告。
> ⛔ 未改：`RuleCore.cs` / `EffectResolver.cs` / 账主表 / 活状态表 / 所有权表 / `CLAUDE.md`；⛔ 未跑 Unity；⛔ 未动 git / `d:/2`。
> 行号一律本文写作时**现读**。

---

## 〇 · 一句话结论

`SAN27-UNION`（「两行都带 `abilityTarget` 拍时每拍播**并集**」）**整条链已闭合**（`B141-W43` 三处 + `B141-W43b` 生产端一处）：
行归属从生成器烘进数据（`CardAnimBeat.li`）→ 事件按行承载的表位（`BattleEvent.AbilityRowTargets`）→
**生产端按行记表**（`RuleCore.TryRowOwnAbilityTargets` / `AppendOneRowAbilityTargetsToEvent`）→
消费端按 `li` 取**那一行**（`BattleDriver.BeatTargetsOf`）—— **四处全部落地并验证**
（typecheck **0/0** · `ruleprobe check` **rc=0** · `play check` **rc=0** 且结算指纹 **`a0e9c63d…` 未变** ·
`play all dump` 改前/改后 **1143 行逐字节相同**）。

> 🔵 **`B141-W43b`（同日续做）**：`RuleCore` 释放后，把原 §五 的三步补丁**原样落地**了
> （`TryRowOwnAbilityTargets` + `MergeRowTargetsUnique` + `ToEvent` 改写 + 一个 `…ForTest` 自检门），
> 并补了一组**端到端**断言（D 组：生产端产出的真事件 → 消费端逐拍取本行）。详见 **§十**。

| # | 处 | 状态 | 落点 |
|---|---|---|---|
| 1 | 生成器 emit 行归属 | ✅ **做完** | `工具/gen_card_anim_order.py:589-596`（+ 注释更新 `:607`）· 产物 `Resources/card_anim_order.json` **重生成** |
| 2 | `CardAnimBeat.li` | ✅ **做完** | `CardPresentation/Battle/CardAnimOrder.cs:75-87` |
| 3 | `BattleEvent` 按行承载的表位 | ✅ **做完** | `RuleEngine/Core/BattleEvent.cs:876-912` |
| 4 | 消费端按 `li` 取本行 | ✅ **做完** | `BattleDriver.cs:19812` / `:19820` / `:19847` / `:19862` + 类尾 `:42297` |
| **5** | **生产端按行记表**（`RuleCore`） | ✅ **做完**（`B141-W43b`） | `RuleCore.cs:18617`（委托）/ `:18633`（事件版）/ 类尾 `:18768` `TryRowOwnAbilityTargets` · `:18787` `MergeRowTargetsUnique` · `:18806` `…ForTest` |

---

## 一 · 改动 1：生成器把行归属烘进瘦身产物

**问题**（`报告/B141_W128_RowAbility.md` §5·1 ④）：中间结构 `_assemble_cards` **早就有** `logicIndex`（`:241-243`），
但 `build()` 的 emit 按 `abilities[]` 拍平成 `{id,name,steps[]}`、把它**丢了**。

**改法**（`工具/gen_card_anim_order.py:589-596`，现读）：

```python
            li = ab.get("logicIndex")
            if li is None:
                li = -1                      # 缺值 = 不知道（⛔ 别拿 0 顶：0 是合法行号）
            for st in sorted(ab.get("steps") or [], key=lambda s: s.get("i", 0)):
                b = slim_beat(st, anims, stats)
                if b is not None:
                    b["li"] = li
                    steps.append(b)
```

`note` 字段同步补了 `li=` 的口径说明（`:607`），**没有**改任何既有字段。

**重生成（🔴 按纪律用显式动作，⛔ 裸跑）**：

```
$ python -I -X utf8 工具/gen_card_anim_order.py --check-slim
[check-slim] 幂等 = False | cards = 93 | steps = 158        ← 预期（多了 li）
$ python -I -X utf8 工具/gen_card_anim_order.py --write-slim
[write-slim] bytes = 55206
[write-slim] stats = {'blank_guid': 64, 'unresolved_guid': 0, 'pure_wait': 0, 'cards': 93, 'steps': 158,
                      'cards_no_steps': 741, 'rows_not_active': 911, 'steps_not_active': 1178,
                      'movers': 13, 'offset_beats': 4, 'not_in_pool': 94, 'renamed': 35, 'poolMissing': 0}
```

**产物 diff 的语义核对**（不是看行数，是**逐键比**）：

| 口径 | 结果 |
|---|---|
| `stats` 逐键 | **逐键相同**（93 卡 / 158 拍 / 13 movers / 4 offset_beats …） |
| `cards` 的**顺序**与每拍的**其它字段** | 剔掉 `li` 与 `note` 后**完全相等**（`strip(a)==strip(b)` = True） |
| 变化的键 | 只有 `note`（1 处）＋ 每拍新增 `li`（158 处） |
| 文本 diff | 新增 158 行 `"li": N`；另有 `"ease": []` → `"ease": [],` 一类**尾逗号**churn（`li` 是最后一个键），以及 1 行 `note` |

**`logicIndex` 的数据形状**（现读产物）：

| 口径 | 数 |
|---|---|
| 拍总数 | **158** |
| `li` 分布 | `0` × **148** · `1` × **9** · `2` × **1** · 其余 0 |
| `li < 0`（不知道） | **0** |
| 有多行的卡 | **9 张**：`AM28 Chimera` `[0,0,1]` · `AM35 Leman Russ Vanquisher` `[0,0,1]` · `DA18 Inner Circle Companion` `[0,1]` · `DA20 Master Lazarus` `[0,1]` · `DA71 Watcher in the Dark` `[0,0,1]` · `DA75 Master Zacharial` `[0,2]` · `SW23 Fenrisian Drop Pod` `[0,1]` · `SW8 Blackmane Reiver` `[0,1]` · `UM105 Dreadnought` `[0,0,1]` |

⚠️ `li` 是**`activeAbility` 那个对象**的 `activeAbilityLogic` 下标（本表只收 `path == 'activeAbility'`），
替代能力（`ab = -2`）那一份**不在**本表里 —— 消费端必须核 `BattleEvent.AbilityRowTargetsObject`（见 §三/§四）。

### 1·3 索引对齐核对（🔴 这条链最容易静默错的地方）

`li` 来自 **2022 快照**（`cards_1195_2022时代.json` 的 `activeAbilityLogic` 枚举序），
而运行期按行取表用的是**当前 build** 的 `activeAbilityLogic[j]` ⇒ 两张表行数/顺序一变，`li` 就会指向**别人那一行**。
逐张核了**全部 9 张多行卡**（现读 `cards_engine.json` 的 `activeAbilityLogic[].ta` 长度 = 当前行数）：

| 卡 | `li` 序列 | 当前卡表行数 | `ab` |
|---|---|---|---|
| `AM28` | 0,0,1 | 2 | `-1` |
| `AM35` | 0,0,1 | 2 | `-1` |
| `DA18` | 0,1 | 3 | `-1` |
| `DA20` | 0,1 | 3 | `-1` |
| `DA71` | 0,0,1 | 2 | `-1` |
| `DA75` | 0,**2** | 3 | `-1` |
| `SW23` | 0,1 | 2 | `-1` |
| `SW8` | 0,1 | 2 | `-1` |
| `UM105` | 0,0,1 | 2 | `-1` |

⇒ **9/9 都满足 `行数 > max(li)`，且全部是 `ab = -1`**（对齐成立）。
⚠️ 这条**不能只靠一次性核对**：断言 `A3c` 把它钉进自检（逐卡比「用到的最大 `li` < 当前行数」），
越界会红；**错位**（行数够但顺序变了）抓不到 ⇒ 仍需人工核，这一点已在断言文案里如实写死。

---

## 二 · 改动 2：`CardAnimBeat.li`

`CardPresentation/Battle/CardAnimOrder.cs:75-87`（新字段 + 判据 doc）：

```csharp
/// <summary>🆕 **行归属** = 这一拍属于原版 `activeAbilityLogic` 的**第几行**（下标；`-1` = 不知道）。
/// ✅ 已复刻（2026-11-04 · B141-W43 · `B141-SAN27-ROWID` · 判据 `AbilityLogic__PlayAbility.c:262/273`）…
public int li;
```

doc 里写死了**为什么不能用 `ca` 反查行**（池内 12 张跨行 `ca` 重复：`AM28` 两行都 70 · `DA18` 都 60 · `SOR13` 都 30）
—— 那条判据来自 `B141_W128_RowAbility.md` §5·2，⛔ 别再有人拿 `ca` 猜行。

**行数变化**：`CardAnimOrder.cs` **425** 行（`git diff --numstat` = **+13 / −0**，本文件在我之前是干净的）。
⚠️ 该文件在账里**没有任何 `文件:行号` 落点**（现扫 `账/*.tsv` = 0 命中）⇒ 插在哪都不漂。

---

## 三 · 改动 3：`BattleEvent` 的「按行承载」表位

`RuleEngine/Core/BattleEvent.cs:876-912`（**追加在 `BattleEvent` 类尾、`Priority` 之后**）：

```csharp
public System.Collections.Generic.List<System.Collections.Generic.List<BattleEventTarget>> AbilityRowTargets;
public int AbilityRowTargetsObject;   // -1 = activeAbility / -2 = alternativeAbility；0 = 未设置（fail-closed）
```

### 3·1 为什么**不**采纳「`BattleEventTarget` 加 `int Row`」那个方案（`B141-W37` §5·1 ③ 的另一个选项）

`AbilityTargetList` 是**去重**的并集（`AppendOneRowAbilityTargets` 按 `InstanceId` / `Player+Slot` 去重）。
**同一张牌可以同时属于两行**（例：`AM28 Chimera` 两行都是 `ta=0 any` ⇒ 两行都会选到同几个单位）。
⇒ 一个目标**一个**行号**表达不了「它属于哪几行」**，只能二选一 ⇒ **必然丢掉一行的播放**。
**按行存列表**才是无损形状，而且与 W37 自己给的第二个选项（`List<List<BattleEventTarget>>`）一致。

### 3·2 形状契约（消费端逐条依赖，写进了 doc）

| 值 | 语义 |
|---|---|
| 字段 `null` | 生产端**还没填**（旧形状）⇒ 消费端退回并集 ⇒ **与改前逐字节等价** |
| `rows[i] == null` | **第 i 行没记**（白名单外 / 没数据）⇒ 消费端退回并集 **+ 出声** |
| `rows[i]` 是**空表** | 第 i 行**确实没选到目标**（原版 `PlayAnim.c:172-176` = `LogError` + return）⇒ 消费端**不退回并集** |
| `AbilityRowTargetsObject` | 行号属于哪个能力对象；**缺省 0（非法值）⇒ fail-closed** |

### 3·3 落点安全性（铁律 18·b）

- 现扫 `账/*.tsv`：引用 `BattleEvent.cs` 的落点共 **10 条，最深 `:874`**（`特效动画.tsv`，`BattleEvent.Priority`）。
- 我的插入点 = **`:875` 之后（类尾）** ⇒ **`:874` 及之前一行未动** ⇒ 结构上不可能漂。
- `git diff --numstat` = **+38 / −0**；文件 960 → **998** 行；行尾仍**纯 LF**（crlf=0 / bareCR=0）。

---

## 四 · 改动 4：消费端按 `li` 取本行（**零净行数**）

### 4·1 `ExpandAbilityBeats` 里的四处（同一物理行改写，⛔ 不增删行）

| 现读行 | 改前 | 改后 |
|---|---|---|
| `:19812` | `var tgtList = e.AbilityTargetList;` ＋ `int targetCount = tgtList != null ? …` | `var tgtList = …;` ＋ `System.Func<CardAnimBeat, List<BattleEventTarget>> targetsOf = bb => BeatTargetsOf(bb, e, tgtList);` |
| `:19820` | `if (b == null) continue;` | `if (b == null) continue; var bt = targetsOf(b); int targetCount = bt != null ? bt.Count : (target != null ? 1 : 0);` |
| `:19847` | `int nTgt = (usesTgt && tgtList != null && tgtList.Count > 0) ? tgtList.Count : 1;` | 同形，改用 `bt` |
| `:19862` | `var t = tgtList[ti];` | `var t = bt[ti];` |

**零漂证据**（不是我说的，是行号没变）：`bool ExpandAbilityBeats` = **:19800** · `AbilityBeatsExpanded++;` = **:19882** ·
`bool BeatSlotGate(...)` = **:19888** · `static void BeatSoundOff(...)` = **:19937** · `struct PendingSignal` = **:19943**
—— 与我开工前那次 grep **逐个相同**（该文件共有 **837** 条账落点，最深 `:42085`）。

### 4·2 类尾新成员 `BattleDriver.BeatTargetsOf`（`:42297`，`public static`，可被自检直接调）

取表顺序：`AbilityRowTargets[b.li]`（且 `AbilityRowTargetsObject == CardDef.ActiveAbilityObject`）
→ 取不到 ⇒ **退回并集 + `BeatSoundOff` 一次**（`norow:<卡>:<li>` / `norows:<卡>`，`BeatWarnCap = 16` 封顶）。

🔴 两条硬约束写进了 doc：
① **只在 `== -1` 时按行取**（替代能力 `-2` 的行号**也从 0 起**，不核这一位就会把 `-2` 的第 0 行当成 `-1` 的第 0 行）；
② **空表是有效答案**（原版「这一行没选到目标」⇒ `LogError` + return，⛔ 不是「借别人的目标」）。

**行数变化**：`BattleDriver.cs` 42356 → **42405**（**+49**，全部在类尾、最后一个落点 `:42085` 之后）。

### 4·3 同一处 doc 的就地订正（铁律 5）

`ExpandAbilityBeats` 的 `<para>**多目标拍**</para>` 那段（现读 `:19791-19798`，**8 行换 8 行**）原来写着
「我们的引擎一条事件**只带 0 或 1 个目标**…… ⚠️ 原版是整个 `PlayAnim` return，我们**只跳这一拍**」——
**两条都已过期**：`B104_MT` 起事件带 `BattleEvent.AbilityTargetList`（长度 = 目标个数）；`B90_B2` 起是 `break`（剩下的拍一条都不排）。
已就地改写成「按**这一行**的目标表逐目标各播一份」＋「`break`」，并保留「这条是我们改的」那半句（如实标）。
⚠️ **没有新增行**（8 行改 8 行）⇒ 该段以下所有账落点零漂移。
⚠️ 该文件此刻**同时被 `B141-W8` / `B141-W8b` 登记在飞**（`文件所有权_当前.md` §一·〇·g）；我只动了 `ExpandAbilityBeats`
内部 4 个物理行（零行数）与类尾，**任何其它位置一字节未碰**。

---

## 五 · ❗ 还差的一跳：生产端（`RuleCore`，白名单外）—— 可粘贴补丁

> ✅ **2026-11-04 同日更新（`B141-W43b`）：本节的三步补丁已经原样落地**（`RuleCore` 被释放后由我续做），
> 落地细节、行数位移与验证读数见 **§十**。以下保留为**施工判据与理由**（⛔ 别当「还没做」）。

**为什么本件不能做**：① `RuleCore.cs` **不在**本件白名单；② 它此刻被 **`B141-W22` / `B141-W37` 两个在飞写手**独占
（`文件所有权_当前.md` §一·〇·g；现读 mtime `19:23`）。
③ 按纪律「一个文件同一时刻只有一个写手」⇒ 我**只交补丁、不硬改**。

**要改的是 `AppendOneRowAbilityTargetsToEvent`**（现读 `RuleCore.cs:18647-18656`）—— 它已经是「每行一次」的唯一调用点，
所以补丁**不需要动行循环**（`:15632` 那一行原样）。

> ⚠️ **行号是 19:31 现读的**，而 `RuleCore.cs` 此刻仍有别的写手在改（mtime 19:23）⇒ **落地前请按方法名重新定位**
> （`grep -n "static void AppendOneRowAbilityTargets\|static void AppendOneRowAbilityTargetsToEvent"`），
> ⛔ 别拿本文的行号当插入点。

### 5·1 三步（`AppendOneRowAbilityTargets` 改前现读 `:18615-18644`；⚠️ 落地后行号已移，见 §十）

**① 把「白名单 + `sub`」抽成一个新方法**（新方法放**类尾**，与 `PublishRowAbilityTargetTableForTest` 同一个纪律）：

```csharp
        /// <summary>【B141-W43 补丁】这一行**自己的**目标表（未并进并集、未去重）。
        /// 返回 false = 这一行**不归按行承载管**（白名单外 / 没 36 格数据 / `logic < 0`）⇒ 消费端退回并集。
        /// 🔴 白名单与 `PublishRowAbilityTargetTable` 同一条（`ta ∈ {0,10,110,140,150}` ∧ `MaxTargets < 1`，
        /// 判据 `GetTargets.c:884-891/:972-1060` = 建完池直接 return、不消费 `ctx.GetRandomInt`）。</summary>
        static bool TryRowOwnAbilityTargets(BattleContext ctx, int owner, UnitState source, UnitState chosen,
                                            int[] step, out List<BattleEventTarget> own)
        {
            own = null;
            if (ctx == null || source == null || source.Card == null || step == null || step.Length < 4) return false;
            int logic = step[2], abilityObject = step[3];
            if (logic < 0) return false;
            var set = source.Card.ActiveAbilityTargetCriteriaAt(abilityObject, logic);
            if (set == null) return false;
            int ta = set.TargetsAffected;
            if (ta != 0 && ta != 10 && ta != 110 && ta != 140 && ta != 150) return false;
            if (ta != 0 && set.MaxTargets >= 1) return false;
            own = AbilityTargetListFromCriteria(ctx, owner, set, source, chosen);
            return own != null;
        }

        /// <summary>【B141-W43 补丁】把 `sub` 按 `InstanceId` / `Player+Slot` 去重并进 `into`
        /// （**逐字搬**自 `AppendOneRowAbilityTargets` 原来的那个 for 循环，⛔ 别再写第二套去重规则）。</summary>
        static void MergeRowTargetsUnique(List<BattleEventTarget> sub, List<BattleEventTarget> into)
        {
            if (sub == null || into == null) return;
            for (int i = 0; i < sub.Count; i++)
            {
                var t = sub[i];
                bool has = false;
                for (int j = 0; j < into.Count; j++)
                {
                    var x = into[j];
                    if (t.InstanceId != 0 && x.InstanceId != 0) { if (x.InstanceId == t.InstanceId) { has = true; break; } }
                    else if (x.Player == t.Player && x.Slot == t.Slot) { has = true; break; }
                }
                if (!has) into.Add(t);
            }
        }
```

**② `AppendOneRowAbilityTargets` 的方法体整段换成**（**签名不变** ⇒ 另外两个调用点 `:18655` / `:18663` 零改动）：

```csharp
        static void AppendOneRowAbilityTargets(BattleContext ctx, int owner, UnitState source, UnitState chosen,
                                               int[] step, List<BattleEventTarget> into)
        {
            if (into == null) return;
            List<BattleEventTarget> sub;
            if (!TryRowOwnAbilityTargets(ctx, owner, source, chosen, step, out sub)) return;
            MergeRowTargetsUnique(sub, into);
        }
```
（原来那一串 `logic < 0` / `set == null` / `ta` 白名单 / `MaxTargets` / `sub == null` 判据**整体搬进** `TryRowOwnAbilityTargets`，**口径逐字不变**。）

**③ `AppendOneRowAbilityTargetsToEvent` 的方法体换成**：

```csharp
        static void AppendOneRowAbilityTargetsToEvent(BattleContext ctx, int owner, UnitState source, UnitState chosen, int[] step)
        {
            if (ctx == null) return;
            var sig = ctx.Signals;
            if (sig == null || sig.Count == 0) return;
            var e = sig[sig.Count - 1];
            if (e == null || e.Kind != EvtKind.Ability) return;
            // 🆕 2026-11-04（`B141-W43` · `B141-SAN27-ROWID`）：**按行承载**（原版每行各一次 `GetTargets + PlayAnim`，
            //   `AbilityLogic__PlayAbility.c:262/273`）。并集（`AbilityTargetList`）口径**保持不变**（零回归的兜底）。
            List<BattleEventTarget> own;
            if (TryRowOwnAbilityTargets(ctx, owner, source, chosen, step, out own))
            {
                if (e.AbilityRowTargets == null)
                {
                    e.AbilityRowTargets = new List<List<BattleEventTarget>>();
                    e.AbilityRowTargetsObject = step[3];          // -1 activeAbility / -2 alternativeAbility
                }
                if (e.AbilityRowTargetsObject == step[3])         // 两个对象的行号各自从 0 起 ⇒ 不混
                {
                    while (e.AbilityRowTargets.Count <= step[2]) e.AbilityRowTargets.Add(null);
                    e.AbilityRowTargets[step[2]] = own;            // ⚠️ **空表也是有效答案**（这一行确实没选到目标）
                }
                MergeRowTargetsUnique(own, e.AbilityTargetList);   // 并集照旧（改前口径）
            }
            if (e.AbilityTargetList == null) e.AbilityTargetList = new List<BattleEventTarget>(4);
            // ⚠️ 白名单外那一行**故意不记**（`rows[logic]` 保持 null）⇒ 消费端退回并集 + 出声，⛔ 别记空表
            //    （空表 = 「这一行没选到目标」= 消费端会让这一拍的拍序直接 return，那是另一回事）。
        }
```

### 5·2 补丁落地的验收（建议顺序）

1. `TMPDIR=/tmp/wf_x bash 工具/typecheck.sh` ⇒ 0/0。
2. `bash 工具/ruleprobe.sh play check` ⇒ **rc=0 · 指纹应当仍是 `a0e9c63d…`**（这条链只产**表现层**的表，结算层不读它）。
3. `bash 工具/ruleprobe.sh play all dump <out>` ⇒ **逐字节不变**（同理由；`play all dump` 看不见表现层）。
4. 跑 `RuleEngineTest`（`B141_W43_Assert` 接线后）+ `BattleScene.Run`（`ExpandAbilityBeats` 的真实宿主）。
5. 真机：多行卡（`AM28`/`AM35`/`DA18`/`DA20`/`DA71`/`DA75`/`SW23`/`SW8`/`UM105`）打一次主动能力，看**两拍的落点不同**。

### 5·3 `RuleCore` 补丁的行数影响

- `AppendOneRowAbilityTargets` / `…ToEvent` 两处**方法体**改写（净减少约 16 行）；
- 类尾**新增两个方法**（约 35 行）；
- ✅ **实测零漂移（2026-11-04 `B141-W43b` 落地后现核）**：原来这里预估「`:18615` 之后的 3 条账落点会漂」——
  **不成立**。现扫 `账/*.tsv`：引用 `RuleCore.cs` 的落点共 **700 条，最深 `:18464`** ⇒ **改动起点（`:18617`）之后一条都没有**
  ⇒ **`check_ledger_points` 不需要 `--fix`、也不该出现新漂移**。⚠️ 若以后有人把新成员插到 `:18464` **之前**，这条前提就废了。

---

## 六 · 断言（`RuleEngine/Editor/B141_W43_Assert.cs`，305 行）

| 断言 | 盯什么 | 灭自证（怎么才能红） |
|---|---|---|
| A0 / A1 | 表载入；`AM28 Chimera` = **3 拍** | —— 前提 |
| **A1b** | 三拍 `li` = `0,0,1` | 生成器删掉 `b["li"] = li` 并重生成 ⇒ 全 0 ⇒ **红** |
| A2 / A2b | 158 拍；`li < 0` 的 = **0** | 同上 ⇒ 全 -1/全 0 ⇒ 红 |
| **A2c** | **9 张卡有不止一行** | `logicIndex` 被摊平 ⇒ 0 张 ⇒ 红（那时 over-play 就再也修不了） |
| A2d | `li` 分布 `148/9/1/0` | 数据变了 ⇒ 红（数据断言，跟着生成器走） |
| A3 / A3b | 卡表载入；可比的卡数 > 0 | —— 前提 |
| **A3c** ★ | **索引对齐**：逐卡比「用到的最大 `li` < 当前卡表行数」（现读 `9/9` 成立） | 2022 快照与当前 build 的 `activeAbilityLogic` 行数缩了 ⇒ 红。⚠️ **错位（行数够、顺序变了）抓不到**，如实写在文案里 |
| **B1** ★ | 拍 0/1（`li=0`）→ 行 0 · 拍 2（`li=1`）→ 行 1 | **把 `BeatTargetsOf` 改回 `return union`** ⇒ 三拍都 3 项 ⇒ **红** |
| B1b/B1c | 拍 0 = **2** 项、拍 2 = **1** 项（并集是 3 ⇒ 多出来那张就是改前的 over-play） | 「按 `rows[0]` 取」⇒ 拍 2 变 2 项 ⇒ 红 |
| B1d / B2 | 拍 2 播的是**敌方那一张**；同一张卡的拍 0 与拍 2 取到**不同的**表 | 「一卡一表」⇒ 红 |
| **C1** ★ | `rows[1]` 是空表 ⇒ 拍 2 拿到**空表**（⛔ 不退回并集） | 写成「非空才用」⇒ 变 2 项 ⇒ 红 |
| C2 | `AbilityRowTargets == null` ⇒ 退回并集（**旧形状零回归**） | —— 今天生产端就是这个状态 |
| C3 / C5 | `li < 0` / `li` 越界 ⇒ 退回并集 + 出声 | —— |
| **C4** ★ | `AbilityRowTargetsObject = -2`（替代能力）⇒ 退回并集 | 去掉 `== ActiveAbilityObject` 那道闸 ⇒ 拿到行 0 ⇒ 红 |
| C6 | `AbilityTargetList == null` ⇒ 返回 `null`（`null` ≠ 空表） | —— |

⚠️ **如实标**：本件**不执行** `ExpandAbilityBeats` 本体（它要活的 `BattleDriver` + 视图 + 时间线，批处理里造不出来）
⇒ 「循环里确实用了这张表」那一跳靠**结构**（`targetsOf` 是该方法的**唯一**取表口，`nTgt` / `sig.beatTgt*` 两处都读它）
＋ 上面那 4 处零净行数的改写。**端到端（真打一局看画面）仍需 Unity / 真机**，⛔ 别把本件绿当画面绿。

**接线（paste-ready，调度台在 `RuleEngineTest.cs` 聚合行同一物理行、第一个 `//` 之前追加）**：

```
Guard("B141W43_Assert", B141_W43_Assert.Run); _pass += B141_W43_Assert.Pass; _fail += B141_W43_Assert.Fail;
```

---

## 七 · 验证原始读数

```
$ TMPDIR=/tmp/wf_b141_w43 bash 工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0

$ bash 工具/ruleprobe.sh check
=== 19:42:54 结束（rc=0）===

$ bash 工具/ruleprobe.sh play check
  结算指纹：当次现算 = **a0e9c63d7bfa33735f5dad8230a26edd** · 基线文件 = a0e9c63d7bfa33735f5dad8230a26edd（一致）
  --- ①-a 逐卡差异（主档 · 打出去那一拍）---（空）
  --- ①-b 逐卡差异（事件电池）---（空）
  ✓ 基线文件的内容与钉住的一致（a0e9c63d…）
=== 19:43:33 结束（rc=0）===

$ bash 工具/ruleprobe.sh play all dump  <改前>   # 19:40:17  rc=0 · 1143 行
$ bash 工具/ruleprobe.sh play all dump  <改后>   # 19:44:35  rc=0 · 1143 行
$ diff before.tsv after.tsv  ⇒ **IDENTICAL（逐字节相同）**
```

**为什么指纹/逐卡读数**必然**不变**（逐条归因，⛔ 不是「改动没生效」）：
① 本件三处代码改动全在**表现层**（`CardAnimOrder` 数据字段 + `BattleDriver` 的展开逻辑），**引擎结算路径一字节未动**；
② `RuleEngine` 侧只**新增**了 `BattleEvent` 的两个**纯表现提示位**（doc 里写明「不进录像 / 联机」），且**今天没有任何代码写它**；
③ `play all dump` 是**结算层** sweep，结构上看不见拍序/目标展开（同 `B141_W37` 的结论）。

**账闸（只读，未 `--fix`）**：

```
$ python -I -X utf8 工具/check_ledger_points.py
合计：A 5966 条 · 通过 5370（其中告警 951 条）· 漂移 536 · 失败 62
```

⚠️ **这组数不是本件的读数**，而且**比本件开工时（W37 报告记的 A 5966 / 漂移 347 / 失败 51）更差** ——
那是**别的写手同时在改 `RuleCore.cs` / `EffectResolver.cs` / `BattleDriver.cs`** 带来的涨落（`git status` 里这些文件都是 `M`）。
本件对账落点的**结构证据**：
· `BattleEvent.cs` 最深落点 `:874`，我的插入在 `:875` 之后 ⇒ 不漂；
· `CardAnimOrder.cs` 账落点 **0 条** ⇒ 不漂；
· `BattleDriver.cs` 我开工前记下的 5 个行号（`:19800/:19882/:19888/:19937/:19943`）**改后逐个相同** ⇒ 不漂；
· 新增的 49 行全在**最后一个落点 `:42085` 之后**。

---

## 八 · 行数变化总表

| 文件 | 改前 | 改后 | `git diff --numstat` | 说明 |
|---|---|---|---|---|
| `工具/gen_card_anim_order.py` | — | 759 | **+15 / −1** | `li` 块 + `note` 一行改写 |
| `Resources/card_anim_order.json` | 3170 行 | **3328** | **+317 / −159** | 净 **+158** = 每拍一行 `"li"`；159 条 `−` 是**尾逗号**churn（`li` 是最后一个键） |
| `CardPresentation/Battle/CardAnimOrder.cs` | — | 425 | **+13 / −0** | 新字段 + doc；该文件账落点 **0** 条 |
| `RuleEngine/Core/BattleEvent.cs` | 960 | **998** | **+38 / −0** | 类尾追加；最深落点 `:874` 不受影响 |
| `CardPresentation/Battle/BattleDriver.cs` | — | **42405** | （该文件本来就有大量未提交改动，`numstat` 不可用） | **正文段零净行数**（5 个锚点行号逐个相同）· 新成员 **+49** 全在 `:42085` 之后 |
| `RuleEngine/Editor/B141_W43_Assert.cs` | —— | **305** | 新增 | 类名 == 文件名，`Editor/` 下（编辑器程序集已编过：typecheck 0/0） |

行尾：所有改动文件**行尾未翻**（`BattleDriver.cs` 仍全 CRLF：`crlf=42405 == lines`；其余全 LF；`bareCR` 全 0）。

---

## 九 · ❓ 未闭合 / 顺手发现（⛔ 只报不改）

1. 🔴 **整条链还差生产端那一跳**（§五）—— 在补丁落地之前，本件**行为与改前逐字节等价**。
   ⛔ 别把「三处代码已就位 + typecheck 绿」当成「over-play 已经修好了」。
2. 🔴 **白名单外的行仍然退回并集**：`ta = 20 self` / `30 target` / `maxTargets >= 1` 的行**不记**按行表
   ⇒ 那些行的拍仍播**并集**（现有近似，与 `B141-W37` §六·1/§六·2 是同一件事，需一次裁定：
   非白名单行是「清表」还是「留上一行」、以及 `ta=20/30` 能不能进白名单 —— 那会改 `B140_WA` C4 的口径）。
3. ⚠️ **`norows:` 出声会在补丁落地前每张卡响一次**（`BeatWarnCap = 16` 封顶）。
   这是**故意的**：它把「按行承载还没接线」变成可观测，⛔ 别把它当噪声删掉 —— 补丁一落地它就消失。
4. ℹ️ **`CardAnimOrder` 的 158 拍里 `li=0` 有 148 拍**：不是「只有 1 行」，而是**绝大多数主动能力只有一行**
   （多行 = 9 张卡 / 10 拍）。⛔ 别拿分布当「行数」。
5. ℹ️ 生成器 `--write-slim` 的产物 `stats` **不含 `li` 的口径统计**（我没加）—— 若以后要盯它会漂，
   加 `stats["li_missing"]` 之类的**必须同时**改 `note`，否则两份说法打架（铁律 6）。
6. ⚠️ **`BattleDriver.cs` 冲突风险**：该文件在 `文件所有权_当前.md` 里同时登记 `B141-W8` / `B141-W8b`（在飞）。
   本件只动「`ExpandAbilityBeats` 内 4 个物理行 + 类尾」，**任何其它位置未碰**；若两位写手也动了这两处，合并时需要一次人工对读。

---

## 十 · 🔵 续做 `B141-W43b`：生产端落地（`RuleCore`，同日 20:00）

> 触发：调度台通知 **`RuleCore` 已释放**（`B141-W22` / `B141-W37` 交回）。
> 白名单（本续做）：`RuleEngine/Core/RuleCore.cs` · `RuleEngine/Editor/B141_W43_Assert.cs` · 本报告。⛔ 未碰其他文件。
> 行号一律**落地后现读**（20:00）。

### 10·1 三步（按 §五 的可粘贴补丁，一处未改口径）

| 步 | 做了什么 | 现读落点 |
|---|---|---|
| ① 抽 `TryRowOwnAbilityTargets` | 把「`logic < 0` / 36 格数据 / `ta` 白名单 / `MaxTargets` / `sub == null`」整套判据**整体搬**成一个 `static bool …(out List<BattleEventTarget> own)` | `RuleCore.cs:18768`（**类尾新增**） |
| ② 抽 `MergeRowTargetsUnique` | 把原来那个「按 `InstanceId` / `Player+Slot` 去重并进 `into`」的 for 循环**逐字搬**出来共用 | `RuleCore.cs:18787`（**类尾新增**） |
| ③ 改写 `AppendOneRowAbilityTargets` / `…ToEvent` | 前者退化成 3 行（取本行表 → 去重并集）；后者把本行的表**按行号记进 `e.AbilityRowTargets`**（并集照旧） | `RuleCore.cs:18617`（方法签名**未变**）/ `:18633` |
| ④（补）自检门 `AppendOneRowAbilityTargetsToEventForTest` | 让 `RuleEngine/Editor` 能驱动**生产端**（否则断言只能测消费端一半） | `RuleCore.cs:18806` |

**关键语义（写进了代码注释，⛔ 别推翻）**：
- **白名单外的行故意不记**（`rows[logic]` 保持 `null`）⇒ 消费端**退回并集**；**记了空表**才表示「这一行确实没选到目标」
  （原版 `PlayAnim.c:172-176` = `LogError` + return）。两者**必须分开**，否则白名单外的行会静默不排。
- **`AbilityRowTargetsObject` 只在第一次写时定**，之后只接受**同一个能力对象**的行（`-1` / `-2` 的行号各自从 0 起）。
- **并集 `AbilityTargetList` 口径一字未变**（还是同一处 `MergeRowTargetsUnique`，还是同一个顺序）。

### 10·2 行数变化 / 账漂移

| 文件 | 改前 | 改后 | 说明 |
|---|---|---|---|
| `RuleEngine/Core/RuleCore.cs` | 18,748 | **18,812**（+64） | 全部落在 **`:18617` 及之后** |
| `RuleEngine/Editor/B141_W43_Assert.cs` | 305 | **480**（+175） | 新增 D 组（端到端）＋夹具 `MultiRowCard` 等 |

🔴 **零账漂移的结构证据**（不是「跑完闸看着没事」）：
现扫 `账/*.tsv` 里引用 `RuleCore.cs` 的落点共 **700 条，最深 `:18464`** ⇒ **`:18465` 之后一条都没有**，
而我的全部改动在 **`:18617` 之后**（含类尾新增）⇒ **结构上不可能漂**。
（现读闸：`A 5966 · 漂移 532 · 失败 66` —— 与 19:47 那次的 `536 / 62` 有涨有落，那是**同期其他写手**在改
`EffectResolver` / `BattleDriver` / `Shell/*` 等文件的涨落；本件的改动**全在最后一个落点之后**，不贡献漂移。）
（这与 `B141-W37` 当时把改动写在 `:18577` 是同一个理由：那一节本来就长在最后一个落点之后。）

### 10·3 验证读数（改前 / 改后）

```
改前（19:51–19:53）
$ bash 工具/ruleprobe.sh check                       ⇒ rc=0（19:51:41）
$ bash 工具/ruleprobe.sh play check                  ⇒ rc=0 · 指纹 a0e9c63d7bfa33735f5dad8230a26edd（19:52:22）
$ bash 工具/ruleprobe.sh play all dump all_before2.tsv ⇒ rc=0 · 1143 行（19:53:28）

改后（19:58–20:00）
$ TMPDIR=/tmp/wf_b141_w43 bash 工具/typecheck.sh     ⇒ 运行时 0 / 编辑器 0
$ bash 工具/ruleprobe.sh check                       ⇒ 全池 1134 张 · 解析差异 0 行 · rc=0（19:59:24）
$ bash 工具/ruleprobe.sh play check                  ⇒ rc=0 · 指纹 a0e9c63d…（**与基线一致**）
                                                       ①-a 1142 张逐字节一致 · ①-b 1086 张逐字节一致（20:00:03）
$ bash 工具/ruleprobe.sh play all dump all_after2.tsv ⇒ rc=0 · 1143 行 · diff 前/后 = **IDENTICAL**（19:56:14）
```

**逐张归因：为什么「可能变」却**一点没变**（⛔ 不是改动没生效）**：
① 本次改动只**多写** `BattleEvent.AbilityRowTargets`（纯表现提示位，`BattleEvent` 不进 `.json`、不进录像/联机）；
② `play check` / `play all dump` 都是**结算层** sweep，**不消费**这个字段（引擎里 0 个读点：`grep AbilityRowTargets` 只命中
   `BattleEvent.cs` 定义 + `RuleCore` 写入 + `BattleDriver` 读取）；
③ `AbilityTargetList`（并集）的口径**逐字未变** ⇒ 即便有消费者，读数也该一样。
⇒ **预期「多行卡会变」在结算层不会发生**；它真正的可观测面是**画面**（同一张卡两拍的落点不同），那要 Unity / 真机。

### 10·4 断言：新增 D 组（端到端 8 条）

| 断言 | 盯什么 | 灭自证 |
|---|---|---|
| D0 / D0b / D0c | 夹具 3 个己方部队；发得出一条 `EvtKind.Ability`；**两行各自的目标表不一样** | —— 前提（没有鉴别力就没有意义） |
| **D1 / D1b / D1b2** | 行 0（`ta=10`）记进去了，且**与引擎自己那一行的表逐项相同**（oracle = `AbilityTargetListFromCriteriaForTest`） | 生产端不写字段 ⇒ `rows == null` ⇒ 红 |
| **D2 / D2b / D2c / D2d** | 行 1（`ta=0`）也记进来（下标 = 行号）、含敌方、`AbilityRowTargetsObject == -1` | 只记第一行 ⇒ `Count==1` ⇒ 红 |
| **D3 / D3b** | 并集口径没变（含敌方；= 两行并） | 把并集改成按行 ⇒ 红 |
| **D4** ★ | 白名单外的行（`ta=20 self`）**不记**（`Count` 仍 2） | 记成空表 ⇒ 红（那会让白名单外的行静默不排） |
| **D5** ★ | **行 0 不含敌方** | 生产端偷懒把并集抄进每一行 ⇒ 含敌方 ⇒ **红** |
| **D6** ★ | **端到端**：生产端产的真事件 + `AM28` 的 `li=[0,0,1]` ⇒ 拍 0 → 行 0、拍 2 → 行 1 | 任一端退回并集 ⇒ 红 |
| **D7** | `rows[2]` 没记 ⇒ 消费端**退回并集**（与「记了空表」是两档） | 把「没记」也当空表 ⇒ 红 |

⚠️ 期望值**不手算**：行 0/行 1 的期望表直接取引擎自己的 `RuleCore.AbilityTargetListFromCriteriaForTest(...)` 作 oracle
⇒ 不会因为「我数错了棋盘」而假红，同时仍然能抓住「记成了并集 / 记成了别人的行」。
⚠️ 仍如实标：**D 组是 `RuleEngineTest` 里的编辑器内自检**（不跑画面）；画面侧的「两拍落点不同」要真机。

### 10·5 续做后仍未闭合（❓）

1. **`ta = 20 / 30 / maxTargets >= 1` 的行仍退回并集** —— 白名单是**故意的**（那些档要么烧随机数、要么是另一层语义），
   与 `B141-W37` §六·1/§六·2 是同一件待裁定的事（会撞 `B140_WA` C4）。今天多行卡里没有这种行，**零可观测**。
2. **`norows:` 出声会消失**：生产端已接线 ⇒ 那条「没接线」的告警**不该再出现**；若还出现，说明某个发点没走
   `UseActiveAbilityCore` 的行循环（⛔ 别当噪声静音，那正是本件的检测口）。
3. **端到端画面未验**：`AM28`/`AM35`/`DA18`/`DA20`/`DA71`/`DA75`/`SW23`/`SW8`/`UM105` 打一次主动能力，
   看两拍的落点是否**分开**（需要 Unity / 真机 — 本件按红线未跑）。
4. ⚠️ **`RuleCore.cs` 我改完之后**若再有写手动这个文件，请按**方法名**定位（`TryRowOwnAbilityTargets` /
   `MergeRowTargetsUnique` / `AppendOneRowAbilityTargetsToEvent` / `…ForTest`），⛔ 别用本报告的行号。

