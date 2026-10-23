# 诊断：`RuleEngineTest.Run` 8 红（第六十七会话同步点，2026-10-23）

> 只读诊断代理。**没有改任何源码 / 测试 / 台账**，没有跑 Unity、没有动 git。
> 日志 = `_tmp_view/t67_sync/ruleengine.log`（汇总行 `:108196` = `=== 结束：6623/6631 通过，8 条失败 ❌ ===`）。
> 唯一额外动作：跑了 **`工具/ruleprobe.sh card <卡名>`**（纯 dotnet 离线探针，不占 Unity 实例、不进 git；
> 产物只落 `工具/ruleprobe/bin|obj`，已被 `.gitignore:214-215` 忽略），用来**现读解析层的分桶结果**。

## 0. 结论速览

| # | 条目 | 断言位置 | 分类 | 一句话 |
|---|---|---|---|---|
| 1 | `[诊断入口过期]` | `RuleEngineTest_Diag.cs:199-223` | **α（工具产物）** | `Run()` 新加了一条用例，生成物没重生成；**不在 8 条计数内**（见 §0·a） |
| 2 | `A1594` 哨兵④-b **孤儿条数** | `RuleEngineTest.cs:10542` | **α（断言已被 D15 作废）** | 本轮 `D15` 让第二条 `Rally:` 正文**真的进 `TriggerOps`** ⇒ 腿 A 收到了 ⇒ 不再是孤儿 |
| 3 | `A1594` 哨兵④-b **档位** | `RuleEngineTest.cs:10545` | ↑ 同一条根因 | 没有孤儿 ⇒ 档位无从判 ⇒ `(没有孤儿)` |
| 4 | `T27⑦ CheckTrue(!queued7)` | `RuleEngineTest_Priority.cs:423` | **δ（夹具前提不成立）** | 夹具用 `u.Health = 0` 冒充「待死」，而我们判 state 5 用的是**待死队列**（`IsPendingDeath`）⇒ 夹具根本没建立「待死」 |
| 5 | `T27⑦` ran 期望 4 | `:425` | ↑ 同一条根因 | ⑦ 入队并结算了 ⇒ `ran` 5 |
| 6 | `T27⑧` ran 期望 5 | `:434` | **δ（被 ⑦ 连累）** | ⑧ 本身**按设计跑了**（日志无「取消」行）；只是基线多了 1 |
| 7 | `T27⑨` ran 期望 5 | `:446` | **δ（被 ⑦ 连累）** | ⑨ 本身**按设计被取消**（日志 `:102633` 有 `[ActionQueue] **取消**…变残骸`）；只是基线多了 1 |
| 8 | `T02·or①` | `RuleEngineTest_T02.cs:1068` | **α（id 是截断串）** | 3 个 8 位短哈希**不是任何卡池版本的 `id`**，只是现真 id 的**前 8 个字符** ⇒ `TryGetValue` 落空 |
| 9 | `T02·or②` | `:1080` | **α（同上）** | 4 个短哈希同因 |

**真回归（必须改实现）＝ 0 条。** 8 条里：**α 4 条**（A1594×2 · T02×2）· **δ 3 条**（T27⑦×2 · ⑧ · ⑨ —— 其中 ⑧⑨ 是 ⑦ 的连累）· **γ 1 条的「来源」但不是「缺陷」**（A1594 确由本批 `D15` 触发，而 `D15` 本身是照原版改的 —— 见 §2）。
另加 1 条**不计数的**工具产物过期（§1）。

### 0·a 为什么日志里有 9 行 `✗`、汇总只说 8 条

`[诊断入口过期]` 那行是 **`[InitializeOnLoadMethod] DiagDriftAlarm()`** 打的
（`RuleEngineTest_Diag.cs:199-223`，源码 `:196-197` 自述「**这里只报警、不碰任何计数**」）
⇒ 它**不进** `_fail` / `_failures`。日志末尾 `:108197-108204` 那 8 行才是 `_failures` 的回显
（`RuleEngineTest.cs:903-908`）。**别把 9 当失败数**。

---

## 1. `[诊断入口过期]` —— α（工具产物）

**失败原文**（`ruleengine.log:609`）：
```
✗ [诊断入口过期] `RuleEngineTest_Diag.cs` 跟不上 `Run()` 的用例表 —— 反射看到 294 个用例方法，
  产物只认 293 个（缺 1 / 多 0）；缺：TestB01AiScorerHook。
  重生成：`PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`（⛔ 别手改那个文件）
```

**根因**：本轮给 `Run()` 加了 `Step(TestB01AiScorerHook);`（`RuleEngineTest.cs:289-290`，B-01），
但生成物 `RuleEngineTest_Diag.cs` 的 `RunCaseNames` / `RunSafe` 没有跟着重生成
（该文件里 `TestT02OrFamilyBuckets` **在**，`TestB01AiScorerHook` **不在** ⇒ 只差这一条）。

**证据（现读，非猜）**：
```
$ python -I -X utf8 Unity/工具/gen_diag_runner.py --check
`Run()` 用例表 294 条 · 用例方法（口径）294 个
🔴 --check：第 52 行起不一样 ——
    盘上：   "TestPlayCostRejected", …
    重生成：  "TestB01AiScorerHook", "TestPlayCostRejected", …
🔴 --check：产物**过期**
```
⇒ 源头（`Run()` 与类里的方法）**两边都是 294、自洽**，只是生成物停在 293。

**最小修法**：`PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`（**重生成，⛔ 别手改产物**）。
重生成后 `--check` 应打「一致」。

**风险**：无。纯生成物；跑完顺手 `--check` 复核一次即可。

---

## 2. `A1594` 哨兵④-b（2 条）—— α（断言已被本轮 `D15` 作废）；来源是 γ，但 `D15` 是对的

**失败原文**（`ruleengine.log:21822` / `:21836`）：
```
✗ ★ `A1594` 哨兵④-b：夹具「Rally: Deal 1 damage. Rally: Deal 3 damage」—— 第二条 `Rally:` 的正文
   在腿 A 里没有同签名 op ⇒ 孤儿 **1 条**（实得 0 条） —— 期望 [1]，实得 [0]
✗ ★ ……档位 = **同头两写**（⛔ 不是「收到了」…） —— 期望 [同头两写]，实得 [(没有孤儿)]
```

**是不是本批 `D15` 引起的？是。** 上一轮（第六十五会话，`_tmp_view/w65f_ruleengine.log:21673`）这条是 **✓（实得 1 条）**。

**根因链（逐跳有出处）**：
1. 本轮 `D15`：`CardDef.AddTriggerOp` 从「先到先得」改成「同文去重 + **异文追加**」——
   `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs:3178-3199`（关键行 `:3194` `have.AddRange(ops);`）。
2. `EffectText.WillRunOps` 逐 `TriggerTexts` 键读 `TriggerOps(key)`
   → `Core/EffectText.cs:2330-2336` ⇒ **拿到的已经是合并后的两条 op**（`deal 1` + `deal 3`）。
3. 腿 B 的孤儿判据 = 段内任一 op 的 `WillRunSig` 是否在腿 A 的签名集合里
   （`RuleEngineTest.cs:10744-10752`，签名 = verb/amount/payload/target，`:10689-10693`）
   ⇒ `Deal 3 damage` 现在**同签名命中** ⇒ 不再算孤儿 ⇒ `Orphans.Count = 0`。

**`D15` 对不对？对（第一权威）**：`d:/2/tools/decomp_full/RawCardScript__TriggerAbilities.c`
遍历的是**`cardAbilities` 列表的每一个元素**（`:112-120` 循环内 `CardAbility__CanTriggerAbility`，
`:204-215` 命中后逐个 `RawCardScript__TriggerAbility`）⇒ **同一 trigger 词挂两条就是两条都触发**，
原版没有「先到先得」。⇒ **不是 β，是断言过期（α）**。

**最小修法（改断言，⛔ 别回退 `D15`）**：把 ④-b 改成钉**新真值**，并且**保住灭自证**：
```csharp
// 期望：两条正文都进腿 A；孤儿 0；档位无从判
Check(wfTwiceRow.RunOps.Count, 2, "★ …④-b：`Rally:` 两份正文**都**进腿 A（`D15` 异文追加；改回先到先得 ⇒ 本条红）");
Check(wfTwiceRow.Orphans.Count, 0, "★ ……两条都被腿 A 收到 ⇒ 孤儿 0");
```
（外加一条同签名的判别：`RunOps` 里同时出现 `deal 1` 与 `deal 3`，防「只收到一条也报 2」。）
⚠️ 原来那两行 `Check(... WRSegTwice)` **必须删掉**：见下一条「顺手发现①」——这个档位在 `D15` 之后
对「同头异文」这一形状**结构上不可达**了。

**风险**：低。仅动一条哨兵；`A1594` 其余五条（②/④收漏/④付费段/⑤/④-a/④-c）本轮全 ✓，不受影响。
**⚠️ 同批要改的注释（铁律 5，二处）**：
- `RuleEngineTest.cs:10916` 那句「`WillRunOps` 读的是 `TriggerTexts`（= **第一条**正文）⇒ **这一档的分档结果不变**」——
  **不成立**：`WillRunOps` 读的是 `TriggerOps`（合并表）；分档结果**会变**（本条就是证据）。
- 同文件 `:10924` 的文案「但 `TriggerTexts` 只留第一条 ⇒ 这一份不在 `WillRunOps` 的通道账里」——同一句话，同样不成立。

---

## 3. `T27⑦`（2 条）+ `T27⑧` + `T27⑨` —— δ（夹具前提不成立）；⑧⑨ 是被 ⑦ 连累

**失败原文**（`ruleengine.log:102587 / :102603 / :102618 / :102648`）：
```
✗ ★ T27⑦ `InPlayOrCemetery(50)` 的载体**待死不算在场**（`:66-72` 不含状态 5）… 期望 [True]，实得 [False]
✗ ★ T27⑦ 这条同样没结算 —— 期望 [4]，实得 [5]
✗ ★ T27⑧ 入队时在场、**出队前离场** ⇒ 这条**照跑**… —— 期望 [5]，实得 [6]
✗ ★ T27⑨ 载体**变成残骸**… ⇒ 这条**被取消**… —— 期望 [5]，实得 [6]
```

### 3·1 先判「⑦ 的期望对不对」——**期望与原文一致，实现也一致**

原版（第一权威，逐行读过）：
- `CardAbility__IsRightContext.c:66-72`（`case 0x32 = 50`）：`IsInCemetery(card) || IsInPlay(card)`；
- `CardScript__IsInPlay.c:7-13`：状态 ∈ **{2, 15(0xf), 3, 17(0x11)}** —— **不含 5**；
- `CardScript__IsInPlayOrDying.c:8-13`：上面四档 **∪ {5}**（这才叫 40）；
- `CardScript__CheckIfDead.c:108-124`：`health < 1` ∧ 状态∈{2,15,3,17} ∧ `CurrentSurvivor < 1` ⇒ **`*(+0x228) = 5`**。

⇒ **状态 5 = 血归零且没幸存者 ⇒ 对 50 而言「不算在场」**。断言 ⑦ 的**语义**没错。
我们实现（`Core/ActionPump.cs:556-558`）：`(cell != null && !ctx.IsPendingDeath(cell)) || InZone(discard)`，
`IsPendingDeath` = **在 `_pendingDeaths` 队列里**（`Core/BattleContext.cs:1565-1571`，
其 doc 自述「口径 = 原版 `waitingToDie = 5`」）。⇒ **语义映射也对**。

### 3·2 那为什么红？——夹具把「待死」写成 `u.Health = 0`，而这在本引擎里**不是**待死

夹具（`RuleEngineTest_Priority.cs:402-425`）：
```csharp
int hp0 = u.Health; u.Health = 0;                     // ← 只改血，不进 _pendingDeaths
bool queued7 = RuleCore.EnqueueAbility(..., actor: u, abilityContext: CardDef.AbilityContextInPlayOrCemetery);
```
`_pendingDeaths` **只在「同时伤害批」那一路被填**（`Core/RuleCore.cs:8440-8448`，
唯一入口 `AddPendingDeath` 在 `Core/BattleContext.cs:1544`），而**所有**把血写成 ≤0 的引擎路径
都**当场**跟一句 `CleanupDeaths`（`RuleCore.cs:2299-2301` 毒· `:10455-10456` 路标石收集 ·
`Core/EffectResolver.cs:3213-3219` 摧毁）⇒ 真对局里「**站在棋盘上 + 血≤0 + 不在待死队列**」
这个状态**不存在**（要么进了 `_pendingDeaths`，要么已经离场/进弃牌堆）。
⇒ 夹具造的是一个引擎产不出来的状态 ⇒ **δ**。

**旁证（日志）**：⑦ **没有任何** `[ActionQueue] **不触发**` 行（⑤ 有：`:102526`），
说明入队闸**放行**了 = `IsPendingDeath` 为假 ⇒ 与上面推断逐字吻合。

### 3·3 ⑧ / ⑨ 是连累，不是独立缺陷

- ⑧：日志里**没有**「取消」行 ⇒ 它按设计**跑了**（`ran` +1）。它的期望 5 是按「⑦ 不涨」写的 ⇒ 实得 6。
- ⑨：日志 `:102633` 有 `[ActionQueue] **取消**一条待结算动作「T27⑨-变残骸」` ⇒ 它按设计**被取消**（`ran` 不涨），
  实得 6 同样只是基线多了 1。
⇒ **⑦ 一修，⑧⑨ 自动转绿**。

### 3·4 最小修法（二选一；推荐 ①）

**① 改夹具（零引擎风险，推荐）**：⑦ 用**另一个载体**造待死，`u` 留给 ⑧⑨：
```csharp
// ⑦ InPlayOrCemetery(50)：待死（= 原版 state 5）不算在场
var dying = Place(ctx, 0, 5, Unit("Dying", 1, 1, 1));   // 不能动 u —— ⑧⑨ 还要用它
dying.Health = 0;
ctx.AddPendingDeath(0, 5, -1);          // ← 唯一公开入口（BattleContext.cs:1544），这才叫 state 5
bool queued7 = RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "T27⑦-待死50", () => ran++,
                                       actor: dying, abilityContext: CardDef.AbilityContextInPlayOrCemetery);
RuleCore.PumpActionQueues(ctx);
CheckTrue(!queued7, "…");
Check(ran, 4, "…");
ctx.Players[0].Board[5] = null;         // 清场
```
⚠️ `Place` 是**直写** `Board[slot]`（`RuleEngineTest.cs:2209-2234`），摆之前**先确认 5 号格是空的**
（本夹具 `PlayCard(..., 2, out slot)` 落点可能是 2 或 3，5 号格通常空；若被占就换个空格号，两处一起改）。
⚠️ **不要**想着「用完再把待死撤掉」：清队列的那个口 `TakePendingDeaths()` 是 **`internal`**
（`BattleContext.cs:1590`），Editor 测试够不着 —— 全仓 **0 个 `.asmdef`、0 处 `InternalsVisibleTo`**
⇒ `Assets/…/Editor/` 编进 `Assembly-CSharp-Editor`，看不到 `Assembly-CSharp` 的 `internal`
（**现读核过**：Editor 里出现的 `TryParseWhenSentence` / `ExtractBareTalentName` / `PeelInstead`
全是**注释**，没有一处真调用）。这也是推荐「另起一个载体」的原因。

**② 改实现一行（更贴 `CheckIfDead` 字面，可选加固）**：
```csharp
// Core/ActionPump.cs:557
return (cell != null && !ctx.IsPendingDeath(cell)
        && !RuleCore.EnoughPendingDamageToDie(cell))      // ← 补这一条
    || InZone(ctx, owner, inst, Zone.Discard);
```
`EnoughPendingDamageToDie(u) = u.Health <= 0 && u.Survivor <= 0`（`RuleCore.cs:6721-6726`），
正是 `CheckIfDead.c:108-124` 那个闸；补上之后**按夹具原样**也能过。
**今天零行为变化**（真对局里两者恒等，见 §3·2），但**别忘**它改的是 `Core/`（要跑 `RuleEngineTest.Run` 全条）。
👉 **建议只做 ①**，把 ② 记成「可选加固」，避免为一条夹具去动引擎。

**风险**：① 极低（纯夹具）；② 低但属引擎改动，且要多跑一轮。

### 3·5 顺手指出（不是失败，但文档要钉）

`T27⑧` 的断言文案说「🧨 把 context 复核搬回泵点 ⇒ 本条红」——这条**确实**有效（⑧ 真跑了）。
但它**判不出**「门在入队不在出队」的**另一半**：⑨（变残骸取消）仍由泵点负责
（`Core/ActionPump.cs:646-650` 的 `CarrierReplacedByRemnant`）。两句不矛盾，只是**别把它们当同一件事**。

---

## 4. `T02·or①` / `T02·or②` —— α（id 是**截断串**，不是卡池 id）

**失败原文**（`ruleengine.log:105587` / `:105601`）：
```
✗ ★ T02·or① … 不成立：6ed51d59(卡池里没有) d01a5ba5(卡池里没有) 4d348cff(卡池里没有) —— 期望 [0]，实得 [3]
✗ ★ T02·or② … 不成立：9f75a6fe(卡池里没有) 760bd3af(卡池里没有) 97e95944(卡池里没有) 8482077f(卡池里没有) —— 期望 [0]，实得 [4]
```

**它们是什么？**——**当前真 id 的前 8 个字符**，不是任何一版 `cards_engine.json` 的 `id`：
- 现池（`cards_engine.json`，`count=1127`）里 7 个前缀**各自唯一命中 1 张**（现读）；
- 历史：`_tmp_view/cardid_bak_1013` 里是 `TAU7`/`TL2`，`_tmp_view/p1_1014/bak` 里是
  `TAU_Hero_of_the_Empire`，`git show 3251df05^`（`K06` 之前）里已经是**全长 GUID** 或 `UM_Catechism_of_Death`
  ⇒ **8 位短串从来不是池 id**，是**普查文档里的展示写法**（`资料/普查产出_1022/W63_T02_or与落位_普查.md:15-16`
  就是用这 7 个短串点名的），本轮写断言时**照抄**过来了。
- ⚠️ 所以不是「`K06` 重编号前的旧 id」，而是「**把人读用的短哈希当成了 `id`**」——分类仍是 **α**。
- 另外两个短串 `4d348cff` / `8482077f` 在别的卡 `descZh` 里也出现过（`$$<guid>$$` 卡牌链接），
  那是**巧合子串**，与本题无关。
- `RuleEngineTest_T02.cs:1064` / `:1076` 的 `byId.TryGetValue(id)` 是**精确匹配** ⇒ 全落空。

**最小修法**：把 `RuleEngineTest_T02.cs:1032-1035` 的 7 个短串换成全长 id（现读，唯一命中）：

| 短串 | 全长 id | 卡 |
|---|---|---|
| `6ed51d59` | `6ed51d59b9f8ea24ab0dc7ceaef7956c` | Duelist's Hubris |
| `d01a5ba5` | `d01a5ba5865ded4418194ea50e333b21` | Inscrutable Cunning |
| `4d348cff` | `4d348cff71208c24d8e39252d913ff9d` | Hymn of Battle |
| `9f75a6fe` | `9f75a6fe11922eb409ffbad4ecdeb405` | Hero of the Empire |
| `760bd3af` | `760bd3afde513774d96d6492f995fd37` | Spirit Leech |
| `97e95944` | `97e959444b53f424ea4285e2ca02c7e6` | Chosen of the Four |
| `8482077f` | `8482077f309b08943b190f18b5874871` | Catechism of Death |

⛔ **别在测试里加「前缀匹配」兜底** —— 那是给判据开第二种口径（本仓红线：一处规则只写一份）。

**改完之后真的会绿吗？—— 现读验证过（`工具/ruleprobe.sh card <卡名>`，同一份 `EffectText.Parse`）**：

| 卡 | `chooseone`? | `randomPick` | 落哪个桶 | 断言期望 |
|---|---|---|---|---|
| Duelist's Hubris | 是（2 选项） | **0** | 玩家选 | ① ✔ |
| Inscrutable Cunning | 是（3 选项） | **0** | 玩家选 | ① ✔ |
| Hymn of Battle | 是（3 选项） | **0** | 玩家选 | ① ✔ |
| Hero of the Empire | 是（2 选项） | **1** | 随机 | ② ✔ |
| Spirit Leech | 是（2 选项） | **1** | 随机 | ② ✔ |
| Chosen of the Four | 是（2 选项） | **1** | 随机 | ② ✔ |
| Catechism of Death | 是（2 选项） | **1** | 随机 | ② ✔（另有 1 条 `give` op 的 `randomPick` 缺省 0，不影响分桶） |

**风险**：极低。改的是字面量；`or③`（4 张条件换数值）本轮本来就 ✓，别动。

---

## 5. 建议修复顺序

1. **`gen_diag_runner.py` 重生成**（§1）—— 1 秒，零风险，先把「计数外的那条红」清掉。
2. **`T02` 七个 id 换全长**（§4）—— 纯字面量，改完 `T02` 两条必绿（解析结果已现读验证）。
3. **`T27⑦` 夹具改成真「待死」**（§3·4 ①）—— ⑦ 两条 + ⑧ + ⑨ **一并转绿**。
4. **`A1594` ④-b 换期望 + 顺手订正两处注释**（§2）—— 必须与第 1~3 条**同一批**跑，否则再来一轮 8 红。
5. 落盘后跑 **`RuleEngineTest.Run` 一条**（只动 `RuleEngine/` 与它的 Editor 测试 ⇒
   按铁律 12「只动一个自检宿主就只跑那一条」；⚠️ 但 `Core/ActionPump.cs` 若走 §3·4 ② 则同属一个宿主，仍只跑这一条）。

---

## 6. 顺手发现（找几条报几条）

1. 🔴 **`WRSegTwice`（同头两写）档位在 `D15` 之后对「同头异文」结构上不可达** ——
   异文追加 ⇒ 第二份正文的 op 一定在 `TriggerOps` 里 ⇒ 一定不是孤儿 ⇒ `ClassifyWillRunFace`
   （`RuleEngineTest.cs:10916-10926`）那个分支永远走不到。**现读全池它本来就是 0**
   （`:21600` 与上一轮 `w65f:21600` 都是「同头两写 0」，因为真卡那两例 `ASH69`/`DA7` 的
   两份正文**签名相同**，靠签名相等就已经「收到了」）⇒ **今天读数不变**，但**分支是死代码**。
   要么删掉该档并同步报告行（`RuleEngineTest.cs:10369` 那句文案也要跟着改），要么在注释里写明「D15 之后不可达」。
2. ⚠️ **`IsRightContext(Play)` 用 `IsAlive`（`Health > 0`）近似原版 `IsInPlay`**（`ActionPump.cs:553`）。
   原版 `Play(0)` 的闸是 `IsInPlay && !EnoughPendingDamageToDie && !isRemnant`
   （`CardAbility__IsRightContext.c:18-32`）⇒ 对「`Health ≤ 0` 且 `Survivor > 0`」这一格
   原版**放行**、我们**拦下**。现读**不可达**（幸存者那一支在 `CleanupDeaths` 里就把血救回来了，
   `RuleCore.cs:8320-8432`），但**口径不完全等价** —— 建议并入 §3·4 ② 那一笔一起想，别当两件事。
3. ℹ️ `T27` 的 ④～⑩ 是**本轮新增**、从未绿过（`RuleEngineTest_Priority.cs:382-466`）。
   本次只报出 ⑦ 一条真问题；④/⑤/⑥/⑩ 全 ✓（⑤ 的 `[ActionQueue] **不触发**` 行在 `:102526`）。

---

## 7. 判据出处清单（我实际读过的）

| 判据 | 位置 |
|---|---|
| `IsRightContext` 六档（0/10/20/30/40/50） | `d:/2/tools/decomp_full/CardAbility__IsRightContext.c:18-72` |
| `IsInPlay`（四档，无 5） | `d:/2/tools/decomp_full/CardScript__IsInPlay.c:7-13` |
| `IsInPlayOrDying`（四档 ∪ 5） | `d:/2/tools/decomp_full/CardScript__IsInPlayOrDying.c:8-13` |
| 状态 5 何时置位 | `d:/2/tools/decomp_full/CardScript__CheckIfDead.c:108-124` |
| 同一 trigger 多条 ability 各发一次 | `d:/2/tools/decomp_full/RawCardScript__TriggerAbilities.c:112-120, 204-215` |
| 待死队列（state 5 的等价物） | `Unity/MyGame/Assets/RuleEngine/Core/BattleContext.cs:1544, 1565-1571, 1590` |
| 判死口径 | `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs:6721-6726`；入队 defer 在 `:8440-8448` |
| `IsRightContext` 我们的实现 | `Unity/MyGame/Assets/RuleEngine/Core/ActionPump.cs:536-568`（50 档 `:556-558`） |
| 入队闸 | `Unity/MyGame/Assets/RuleEngine/Core/ActionPump.cs:197-210` |
| `D15` 异文追加 | `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs:3178-3199` |
| `WillRunOps` 读 `TriggerOps` | `Unity/MyGame/Assets/RuleEngine/Core/EffectText.cs:2330-2336` |
| 孤儿判定 / 签名 | `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs:10689-10693, 10744-10752` |
| 分档（WRSegTwice） | `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs:10905-10926` |
| ④-b 断言 | `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs:10538-10547` |
| T27 ④～⑨ | `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_Priority.cs:382-466` |
| T02·or 分桶 | `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_T02.cs:1029-1096` |
| 诊断入口过期警报 | `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_Diag.cs:196-223` |
| 生成器 | `Unity/工具/gen_diag_runner.py`（`--check` 只读对账） |
