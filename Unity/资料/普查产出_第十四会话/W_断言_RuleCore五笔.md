# W_断言_RuleCore五笔 —— `A1396`（`A1355` / `A1354` / `A1356` 三笔补断言）

> 第十四会话（2026-10-21）· **执行代理（有写权限）**报告。
> **产出**：只改一个文件 —— `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_S14.cs`
> （**1678 → 2293 行 · 行尾仍是 LF** · 34 个方法 · 新增 5 个方法 / **75 处断言（源级）**）。
> ⛔ `RuleEngineTest.cs` **一个字都没动**（`Run()` 里那几行 `Section`/`Step` 由调度台挂）、
> `RuleEngineTest_Diag.cs` 没动、**任何 `Core/**` 一个字没动**、`CardPresentation/**` / `工具/**` / 两张正本都没碰、未跑 Unity、未动 git。
>
> **验收三件**（都做齐了）：
> 1. `TMPDIR=/tmp/wf_as14b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（本笔共跑 6 次，最后一次在全部改动落定之后）。
> 2. 🔴 **离线探针真跑**（scratch `csproj`：只编 `RuleEngine/Core/*.cs` + `Data/SimpleAI.cs` + 本文件 + 一个壳，
>    **不改任何工程文件、不跑 Unity**）⇒ **34 个方法 / 361 通过 / 0 失败**。做法同 `W_断言_引擎.md` §六，
>    本笔的产物在 `D:/4/_tmp_as14b/probe/`（`as14bprobe.csproj` · `S14HarnessB.cs` · `result.txt`）。
>    ⚠️ **它只是旁证**：断言的真宿主仍是 Unity 下的 `RuleEngineTest.Run`（那一条由调度台在挂完 `Step()` 之后跑）。
> 3. 🔴 **灭自证 = 14 条负向对照**：把「写坏了」的改动打进 **`Core` 的副本**（`D:/4/_tmp_as14b/neg/`，
>    ⛔ **仓库里的引擎源码一个字没动**），重编重跑，**每一条都红了预期的那个方法** —— 见 §三·2。
>
> ⚠️ **两条夹具口径（本笔全程如此）**：
> · 凡**直接改棋盘 / 直接调 `RuleCore.*`** 的都是**夹具直调**，不是走 `BattleDriver.LocalAct` /
>   `SimpleAI.ExecuteAction` 那两个记账口 ⇒ **不会被录进本地录像**（本文件不是对局，不逐条标注）。
> · `A1355` 与 `A1354` 的夹具是**手工构造的 `CardDef`**（**⚠️ 不是真卡**），
>   断言的消息串里都写着这一句 —— 因为**全池 1126 张卡 0 张声明 `dropPod`**。
>
> ⚠️ **一次「假红」如实记**：本笔倒数第二次跑类型检查时报 **15 条编辑器错误，全部集中在
> `Assets/CardPresentation/Editor/IconSetup.cs`**（**另一个写手**的文件，当时正在写；本笔一次都没碰它）；
> 隔一会儿重跑即 **运行时 0 / 编辑器 0**。判据 = 本仓那条「**错全部集中在某个不是你负责的文件上 ⇒ 那不是你的问题**」。
> ⛔ 本笔**没有**去改那个文件。

---

## 一、方法名 → 断的是哪一笔的哪个行为

| 方法名 | 笔 | 断的是哪个行为（一句话） | 断言数（源级） |
|---|---|---|---|
| `TestS14WouldKillByEntriesDropPodLayer` | **`A1355`** | `WouldKillByEntries` 里 **`dropPod` 血池 = 判死循环的第一层**：整份吃下一击、花掉后不再吃、`>=` 的方向、池 0 但词还在那一格、以及「没有池的对照」 | 14 |
| `TestS14DropPodOpensAtTurnStart` | **`A1354` 支①** | **回合开始开舱**：本轮行动方**开**（摘词 + 一条 `Trigger{landing}`，格位正确）· 非行动方**不开** · 换边后对手那个**才开** · 转回来**不重复开** | 18 |
| `TestS14DropPodOpensAfterAttack` | **`A1354` 支②** | **出手后开舱**：攻击者**开** · **被打的那个不开**（它的池只按伤害扣） · 攻击者**没有** `dropPod` ⇒ **一条 `landing` 都不发** | 13 |
| `TestS14SwarmTriggerBothHalves` | **`A1356` 刀①** | `TrySwarmMerge` 那一路的「**发什么**」：`Trigger{swarm}` 恰一条且格位 = 合并后还活着那格 · 广播的监听者被叫醒 · **次序判别式**（先事件、后广播） | 9 |
| `TestS14BloodThirstTriggerBothHalves` | **`A1356` 刀②** | `EmitBloodThirst` 补的那半边：第一次出手**事件 + 广播两半齐** · 第二次出手**一条都不发** · 没 `bloodthirst` 的**一条都不发** · **只演出、不结算** | 21 |

> 调度台挂 `Step()` 时按这 5 行一对一对上即可（方法与行为是 1:1，没有一格跨两笔）。

---

## 二、逐条清单

| 笔 | 断言该断什么（逐格） | 灭自证那一半怎么做的 | 期望值的判据出处 |
|---|---|---|---|
| **`A1355`** | ① 池 4 吃掉一条 **5**（本该致死）⇒ **不判死**<br>② `{5,5,5}` ⇒ **判死**（池花掉之后后面的伤害真的落回生命）<br>③ `{3,3}` ⇒ **不判死**（第二条把池打穿并**整份丢给池**）<br>④ `{2,2,2}` ⇒ 不判死（同形状、换数值）<br>⑤ 池 **0** 但**词还在** ⇒ 第一击**整份被吞**、不判死<br>⑥ 对照：同样 5 血、**没有** `dropPod` ⇒ 一条 5 就判死<br>⑦ `{4,5}` ⇒ **判死**（钉 `jge` 的**方向**：`>=` 而不是 `>`）<br>⑧ 无：解析/枚举前提（生命 5、池 4、无堡垒无幸存者） | 🔴 **① 与 ② 结构上不可能同时绿**：<br>· 删掉那一层（退回旧写法）⇒ **① 实得 `true`**；<br>· 把池写成「每轮重新装满」（`podSpent` 不生效）⇒ **② 实得 `false`**。<br>⇒ 「永远判死」/「永远不判死」两种错法各让其中一格红。<br>**⑦ 单独钉方向**：① / ④ 都量不出 `>=` vs `>`（N3 实测只 ⑦ 那一格红）。<br>（负向对照 N1 / N2 / N3） | 🔴 **原版反编译方法体**（本仓第一权威）：<br>`CardScript$$EnoughPendingDamageToDieWithDamageValues`，入口 RVA `0x5EBE00` / VA `0x1805EBE00`：<br>· `0x1805ec228 mov edx,0xe6`（`DefinedTrait.dropPod` = 230）→ `call` → `je 0x1805ec26b`（没词走幸存者/堡垒）<br>· `0x1805ec245 cmp esi,r13d` + `0x1805ec248 jge`：`<` ⇒ `sub r13d,esi` + `xor esi,esi`；`>=` ⇒ `xor esi,esi` + `mov byte[rbp+0x60],1`（= 池标记花掉）<br>· 两支**都以 `jmp 0x1805ec100` 回循环顶**（`0x1805ec255` / `0x1805ec266`）⇒ **`if / else`** 成立<br>（同一段 VA 逐条抄在 `RuleCore.cs` 的 doc 里；另见 `W_RuleCore五笔.md` §四） |
| **`A1354` 支①** | ① 本方 `BeginTurn` ⇒ 舱**开**（`Has` 假、`KwValue` 0）+ 恰一条 `Trigger{landing}`、`Player` = 行动方、`Slot` = 3<br>② **非行动方**的单位 ⇒ **没开**（词还在）<br>③ 换边后 ⇒ **对手那个这时才开**（`Player`/`Slot` 都对）<br>④ 再转回本方 ⇒ **一条 `landing` 都不发** | 删掉 `BeginTurn` 里那句 `OpenDropPod` ⇒ **本方法红 9 条**，而**出手那一支 0 红**（N4）；<br>`OpenDropPod` 开头那句 `u.Has(DropPod)` 守卫由 **④** 单独盯着（N6 里 ③/④ 那几条红）。<br>**② 挡「两边都扫」那种改法。** | `d:/2/tools/decomp_full/CardScript__OnTurnStart.c:102-108 / 263-266`：<br>`:102 bVar3 = false;` → `:103 HasCurrentTrait(param_1, 0xe6)` ∧ `IsInPlay` ∧ **`param_2 == card.isPlayer`** ⇒ `:107 RemoveDropPod` + `:108 bVar3 = true` → `:263-266 if (bVar3) OnTrigger(0x1b8)`。<br>🔴 `bVar3` 整支函数里只被写两次（`grep -n bVar3` 数过）⇒ 语义无歧义。 |
| **`A1354` 支②** | ① 出手后**攻击者**开舱（摘词 + 一条 `landing`、`Player`/`Slot` 对）<br>② **被打的那个不开**：词还在、池只按**伤害**扣（5 − 3 = 2）<br>③ 攻击者**没有** `dropPod` ⇒ **0 条 `landing`** | 删掉 `DeclareAttack` 里那句 `OpenDropPod` ⇒ **本方法红 5 条**，而**回合开始那一支 0 红**（N5）；<br>② 挡「攻守双方都开舱」（那一句写成两处 ⇒ ② 红）；③ 挡「删掉 `Has` 守卫」（N6）。 | `BattleManager._ResolveAttack_d__438__MoveNext.c:339-379`（**写手回 VA 逐条读过**，入口 RVA `10145552` / VA `0x1809ACE10`）：<br>`0x1809ad4c8 mov edx,0xe6` → `0x1809ad4d4 je 0x1809ad847`（没 `dropPod` ⇒ 正常继续）；有 ⇒ `0x1809ad4ec call 0x1805f6cb0`（`RemoveDropPod`，**无条件**）；随后才判 `0x302`（`DefinedTrait.landing = 770`）。<br>⚠️「带 `landing` ⇒ 取消这次攻击」那一支**我们没做**（= 已另立的 `A1395`）。 |
| **`A1356` 刀①（swarm）** | ① 合并 ⇒ 恰一条 `Trigger{swarm}`，**格位 = 合并后还活着的那一格**<br>② 监听卡（`When a friendly unit triggers Swarm, gain +2 Attack`）被叫醒（1 → 3 攻）<br>③ **次序判别式**：`Trigger{swarm}` 的下标 **<** 第一条 `EvtKind.Hit`（监听卡的正文是 `deal 1 damage` ⇒ 会再发一条信号） | 换成「只 `ctx.Emit`、不广播」⇒ ① 的**广播**那行红、**事件**那行仍绿（两半各一条）；<br>把「事件 ↔ 广播」次序对调 ⇒ **只有次序那一格红**（N10）。 | 原版 `BattleManagerSupport__BroadcastUnitSwarm.c`：`:16-18` 先 `DamageSignal.Raise`（= 事件那一格）、`:20-70` 再对「场上所有卡 + 当前回合方手牌 + 对手手牌」逐个 `TriggeredSwarm` → `OnTrigger(0x26c)` —— **两件事都在同一个无条件函数里**。<br>⚠️ **「事件那一半不是本笔新加的」**：它 **2026-09-30** 就补过，`RuleEngineTest.cs` 的 `TestSwarm` ① 一直盯着 —— ⛔ 别读成新增行为。 |
| **`A1356` 刀②（bloodthirst）** | ① 第一次出手 ⇒ 恰一条 `Trigger{bloodthirst}`（`Player`/`Slot` 对）**且**监听卡被叫醒（1 → 3 攻）<br>② 第二次出手 ⇒ **0 条**、监听者不再醒<br>③ 没有 `bloodthirst` 的单位 ⇒ 0 条、监听者不醒<br>④ **只演出、不结算**：先用 `UnitState.GrantOps` 给账上挂一份 `bloodthirst` 正文（`Gain +3 Attack`），出手时**攻一点没涨** | 删掉尾部那句 `BroadcastWhen` ⇒ ① 的**广播**那行红、**事件**那行仍绿（N7）；<br>`AttacksThisTurn != 1` 放宽 ⇒ ② 红（N8）；<br>尾部改成调 `FireTriggerAlways` ⇒ **④ 红**（N9）。<br>🔴 **④ 的夹具用 `GrantOps` 而不是靠 `RoutableTriggers`** —— 它与「`bloodthirst` 在不在那张表里」**无关**，所以量的是「**这一跳有没有走 `FireTriggerAlways`**」本身，不是数据层。 | 原版 `CardScript__ActivateBloodThirst` → `SendHighlightBloodThirstAction` → `CardScript__HighlightBloodThirst` → `BattleCardUI.HighlightBloodThirst` + `DisplayTriggerAnim` —— **全长没有 `OnTrigger`** ⇒ **只演出**。<br>`+0x48 == 1`（第 1 次之后才亮）⇒ ② 那条「第二次不发」。 |

---

## 三、离线探针读数

### 1. 正跑（scratch csproj，只编 `Core` + `Data/SimpleAI.cs` + 本文件）

```
D:/4/_tmp_as14b/probe  ->  dotnet build ≈ 0.9 s · dotnet run ≈ 0.7 s
=== TOTAL: pass 361 / fail 0        （34 个方法，逐方法 FAILS=0）
```

* 本笔新增的 5 个方法 = **75 处断言（源级）**；其余 29 个（上一批 + 本笔接单改的那几格）一起跑，**全绿**。
* ⚠️ **它不替代** Unity 下 `RuleEngineTest.Run` 那次实跑 —— 它只证明「断言编得过、期望值没写错、改动真的会把它打红」。

### 2. 负向对照（**灭自证**）—— 14 条，每条都红了预期的那个方法

> 做法：把 `Core/*.cs` **复制**到 `D:/4/_tmp_as14b/neg/Core/`（⛔ 仓库里的源码一个字没动），
> 逐条打补丁 → `dotnet build` → 跑 → 读 `### FAILS=<n> <方法名>` → **还原**。
> 脚本 `D:/4/_tmp_as14b/neg/negctl.py`，逐条读数在 `neg/m_N*.txt`。

| # | 打进副本的「写坏了」 | 变红的方法（失败断言数） | 别的格子 |
|---|---|---|---|
| N1 | `WouldKillByEntries` 里**删掉** `dropPod` 那一层（6 行） | `…WouldKillByEntriesDropPodLayer` (4) | 其余 0 |
| N2 | 池花掉后**不置** `podSpent`（= 池每轮重新装满） | 同上 (2) | 其余 0 |
| N3 | `if (acc < podPool)` 读成 **`<=`**（= `jge` 方向读反） | 同上 (1) | 其余 0 |
| N4 | **删掉** `BeginTurn` 里那次 `OpenDropPod` | `…DropPodOpensAtTurnStart` (9) | **出手那一支 0 红** |
| N5 | **删掉** `DeclareAttack` 里那次 `OpenDropPod` | `…DropPodOpensAfterAttack` (5) | **回合开始那一支 0 红** |
| N6 | 删掉 `OpenDropPod` 开头的 `u.Has(DropPod)` 守卫 | turn (3) + attack (1) | ——（共用件，两处都该红） |
| N7 | 删掉 `EmitBloodThirst` 尾部那句 `BroadcastWhen` | `…BloodThirstTriggerBothHalves` (2) | 其余 0 |
| N8 | `u.AttacksThisTurn != 1` 放宽成 `< 1` | 同上 (2) | 其余 0 |
| N9 | `EmitBloodThirst` 尾部改成调 `FireTriggerAlways` | 同上 (3)（含 ④） | 其余 0 |
| N10 | `FireTriggerAlways` 里「事件 / 广播」**次序对调** | `…SwarmTriggerBothHalves` (1) | 其余 0 |
| N11 | `TrySwarmMerge` 改成只 `ctx.Emit`、**不广播** | 同上 (3) | 其余 0 |
| N12 | `CardDef` 两张表末尾的 `Survivor, Sacrifice` **都去掉** | `#21`(2) + `#24`(6) | 其余 0 |
| N13 | **只去掉 `RoutableTriggers`** 那半边 | `#21`(2) + `#24`(4) | 其余 0 |
| N14 | **只去掉 `BodyKeywords`** 那半边 | `#24`(2) —— 正是 `#24c` 那一对 | 其余 0 |

🔑 **N4 / N5 是 A1354 两支「互不遮蔽」的证据**：删一支只红一支。
🔑 **N12 / N13 / N14 是接单那三格的证据**：新增的数值侧（`#24b`）与断句侧（`#24c`）断言**真的依赖** `A1386`/`A1389` 那次落地的**两张表**；只补一张表 ⇒ N13/N14 各自红一半。

### 3. 期望值来源（⛔ 没有一条是从我们自己的实现里读回来的）

* `A1355` —— 原版方法体 VA（上表 §二）。
* `A1354` —— 原版 `.c` + 写手回 VA 复核的跳转表（同一处把两笔的判据都钉着）。
* `A1356` —— 原版 `BattleManagerSupport__BroadcastUnitSwarm.c` / `CardScript__ActivateBloodThirst` 一族。
* 接单那一批 —— `A1386`/`A1389` **落地后当场量的读数**（`TriggerOps("sacrifice")` = 1 op / `payload "+1 attack"`；
  `Survivor:` 端到端 `Attack` 2 → 4；`Parse` 两种顺序都是 3；`Rally` 的正文不再吞掉后半句）。

---

## 四、待调度台定

1. 🔴 **`landing` 不在 `CardDef.Implemented` 里 ⇒ `When a friendly unit triggers Landing, …` 这种监听卡【注册不上】**
   （`WhenEvent.cs:948` 那道闸要求 `Implemented.Contains(kwName)` ∧ `HasTriggerMoment`）。
   今天卡池 **0 张**提到它 ⇒ 不可观测；但 `AbilityTrigger.Landing = 440` 是**原版真有的触发 id**
   ⇒ 按**铁律 11** 该登记。**要动 `CardDef.cs`（⛔ 不在本笔白名单）** ⇒ 请另立一笔。
   （这也是本笔**没有**给 `landing` 配监听卡断言的原因 —— 配了会是一条**永远不响**的监听器。）
2. 🔴 **`Sacrifice:` 正文里那条 `gain +1 attack` 的【落点】今天不稳** ——
   本轮离线探针**两次读数就不同**：一次落到**另一个己方单位**（督军那一格 2 → 3），一次**哪一格都没涨**；
   而 `Core/EffectResolver.cs` **此刻正被另一个写手改**（mtime 20:28，晚于本笔第一次读数）。
   ⇒ 本笔**只钉稳的两件**：解析（`TriggerOps` 非空 + 载荷）+ 「正文真的被跑起来了」（日志）；
   **落点**那半如实留给本报告 §五·2，⛔ 不写成期望（本工程红线：不许把一件**正在变**的事固化成期望）。
   **判据方向**（如果调度台要单开一笔）：原版 `UseSurvivor.c:73-75` 那一族的 ops 是按「**触发者自己**」结算的，
   但 `TriggerSacrifice` 那一处**没核**；另外「触发时它生命还 ≤ 0（`UseSurvivor` 排在 `TriggerSacrifice` 之后）」
   会不会让目标挑法跳过它 —— **未核**。
3. ⚠️ **`A1355` 的池初值口径**仍是我们自定的（`u.KwValue(KeywordTable.DropPod)`），原版取自
   `GameStaticData` 的全局常量（`ActivateTraitsOnSummonOrEnchantment.c:105-120` 读 `+0x1fc`），那份表本地没有。
   `A1335` 已如实标着，本笔**不动**。
4. ⚠️ **另立一笔的候选**（本笔只报不做）：`A1354` 支②的第二支（带 `landing` ⇒ 取消这次攻击）= 已立的 **`A1395`**；
   本笔的断言**不含**它，别把它读成「已断」。

---

## 五、顺手发现（⛔ 一条一句话 + 出处；**一条都没改别的文件**）

1. 🔴 **`landing` 不在 `CardDef.Implemented`**（现读：`Implemented` 那一块里 `landing` 0 命中、
   `bloodthirst` 命中）⇒ `When … triggers Landing` 那族**注册不上**。今天 0 张卡可达。
   出处：`Core/CardDef.cs` 的 `Implemented` + `Core/WhenEvent.cs:948`；与本笔 §四·1 同一条。
2. 🔴 **`Sacrifice:` 正文的落点不稳**（两次读数：落到另一个己方单位 / 哪都没落），
   而 `Core/EffectResolver.cs` 当时正被另一个写手改。出处：`D:/4/_tmp_as14b/probe/measure.txt` 的两轮读数 +
   `Core/EffectResolver.cs` mtime `2026-10-10 20:28`。**未核**：原版那一跳的 ops 按谁结算、挑法是否按 `IsAlive` 过滤。
3. ✅ **`RuleEngineTest_S14.cs` 里 `#24` 夹具那段「顺序是刻意的」注释已过期 —— 本笔就地订正了（铁律 5）**：
   它原来写「`survivor 3` 必须排在最后，否则 `Survivor: Gain +2 Attack` 的头会被 `HeadOf` 取成 `Survivor`、
   兜底值 1 把 `survivor` 冲成 1（实测救回后生命是 1）」。另一个写手当天给 `Parse` 加了 `numSeen`
   （同名里印了数字的那一条压过没印数字的）⇒ **两种摆法现在都是 3**（本轮两种顺序各量过一遍，
   `D:/4/_tmp_as14b/probe/m2.txt`）。摆法保留（无害），但那句「必须」不再成立。
4. ⚠️ **`W_断言_引擎.md` §四① 也过期**（同一件事：`Survivor:` / `Sacrifice:` 的正文现已收得下来）——
   ⛔ **不在本笔白名单，未改**，留给调度台。
5. ℹ️ **探针壳的失败行是最后统一打印的** ⇒ 「哪一格红了」要靠消息文本猜。本笔给壳加了一行
   `### FAILS=<n> <方法名>`（`D:/4/_tmp_as14b/probe/S14HarnessB.cs`），**下一批直接复用**就能按方法归属。

---

## 六、接单追加（调度台中途下的三件 —— 都做了）

### ① `:677-679` 那条断言已【翻过来】，判别力保留

* 现在断的是 `sacCard.TriggerOps(KeywordTable.Sacrifice)` **非空且恰好 1 条 op**，并把载荷也钉住
  （`payload == "+1 attack"`，实测值）。
* 🔴 **判别力没丢**：`TestS14SurvivorTriggerAlwaysFires` 的 `#25` 那一格**没动** ——
  那张只印 `survivor 3`（**无正文档**）的卡 `TriggerOps("survivor")` **仍是 `null`**（本轮实测），
  它正是「**有没有正文**」的判别式。

### ② 「攻 +1 / +2」那半补上了，但拆成【能钉的两半】

| 落点 | 断言 | 读数 |
|---|---|---|
| `#21`（`Sacrifice:` + `survivor 3`） | 解析非空 + 载荷 `"+1 attack"`；**且** `S14Log(ctx,"触发 SACRIFICE")`（= 那一跳走的是 `FireTriggerAt` 那一支，**正文真被跑起来了**） | 探针通过；改前「没有正文」那一支**不打日志** ⇒ 这两条在 N12/N13 下都红 |
| `#22`（敌方回合） | 本方棋盘**总攻 +0**（那道闸把**事件与正文一起**挡掉） | 实测 0（稳） |
| `#24`（两段都有） | `S14Log(ctx,"触发 SACRIFICE")` **且** `S14Log(ctx,"触发 SURVIVOR")` | 探针通过 |
| **`#24b`（新增）** | 只印 `Survivor: Gain +2 Attack` + `survivor 3` 的卡 ⇒ **那个单位自己 +2**（攻 2 → **4**） | 实测 4（两版引擎一致，**稳**） |
| **`#24c`（新增）** | `desc = "Rally: … . Survivor: …"` ⇒ `Rally` 只收 **1** 条 op、`TriggerText("rally") == "Deal 3 damage to an enemy"`、后半句归 `Survivor` | 实测；N14（只去掉 `BodyKeywords`）**专红这一对** |
| `#25`（无正文） | 攻**照旧 2**（数值侧对照） | 实测 2（稳） |

🔴 **「那 +1 落到谁身上」这半【故意没断】** —— 理由与读数见 §四·2 / §五·2（落点今天不稳、且引擎正在被改）。

### ③ 过期头注就地订正（铁律 5）

* `TestS14SacrificeOnOwnTurnRescue` 头注（原 `:660-670`）：把「正文收不下来」整段换成
  「已收得下来 + 落成两条断言 + 为什么不断落点」。
* `TestS14SurvivorTriggerAlwaysFires` 头注（原 `:795-797`）：把「那半句到不了」换成
  「已可达、已补断言（`#24b` / `#24c`）」。
* `#24` 夹具里那段「顺序是刻意的」注释（见 §五·3）。

### ④ 读数（按调度台要求写成一条）

* **改前**（上一批 29 个方法，写手自己最后一次快照 `_tmp_as14/probe/s14_result.txt`）：**272 通过 / 0 失败**。
  ⚠️ 它目录里还留着一份**更早**的中途快照 `run_out.txt` = **271 通过 / 1 失败**（那唯一一条红在
  `TestS14SurvivorTriggerAlwaysFires` 里，**写手在最终那次重跑前已自行改掉** ⇒ 不是本笔修的）。
* **改后**（本次 34 个方法，含本笔 5 个新方法与本笔接单改的那几格）：**361 通过 / 0 失败**。
* **这两次之间新增的断言，靠 N12 / N13 / N14 三条负向对照证明它们【真的依赖】`A1386`/`A1389` 那次落地**
  （去掉那两张表的词 ⇒ 它们立刻红）—— ⛔ 不是「写的时候就绿、写不写都一样」。
