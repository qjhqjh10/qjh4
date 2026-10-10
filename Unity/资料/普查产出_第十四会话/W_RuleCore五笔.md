# W · `RuleCore.cs` 五笔（`A1352` / `A1353` / `A1354` / `A1355` / `A1356`）

> 第十四会话 · **执行代理（有写权限）**。独占 `RuleEngine/Core/RuleCore.cs` 一个文件。
> ⛔ 未跑 Unity、未动 git、未改正本 / `d:/2` / `CardPresentation/**` / `工具/ruleprobe/**`。
> 判据 = `d:/2/tools/decomp_full/`（反编译）＋ **`GameAssembly.dll` VA 反汇编**（capstone 5.0.7 + pefile，
> ImageBase `0x180000000`）＋ `all_methods.txt`（地址表）。
> 改动唯一文件：`Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`（**+271 / −41**，行尾仍是 **LF**，
> `git diff --numstat` 271/41 —— 与「新增的全是注释」相符，没有翻行尾）。
> 秒级类型检查：`TMPDIR=/tmp/wf_rc bash d:/4/Unity/工具/typecheck.sh` ⇒
> **运行时错误数 0 · 编辑器错误数 0**（每一笔落完各跑一次，共 5 次，最后两次因下面那条自造错重跑）。

---

## 〇、五笔一览

| 账 | 结论 | 代码落点 |
|---|---|---|
| `A1352` | ✅ **做完** | `RuleCore.DeployFree`：出声挪进满场那一支 + 删掉死代码 |
| `A1353` | ⚠️ **判 (γ)「不是缺陷」—— 机制其实早就在**（W4 那半句是错的）；**就地订正注释**（铁律 5） | 只在 `RuleCore.cs` 的注释（`FireTriggerAlways(Sacrifice)` 那一处） |
| `A1354` | ✅ **① 「回合开始自动开舱」+「出手后自动开舱」两支做完**；⚠️ **② 「带 `landing` trait ⇒ 取消攻击 + 发 `Landing`」那一支做不了**（判据已查清，卡在文件白名单） | 新增 `LandingKeyword` + `OpenDropPod`；`BeginTurn` / `DeclareAttack` 各一处调用 |
| `A1355` | ✅ **做完**（预览侧补上「空投舱血池 = 判死循环的第一层」）+ 同批**就地订正两段自相矛盾的 doc** | `WouldKillByEntries` 的循环里；`EnoughPendingDamageToDie` / `WouldKillByEntries` 的 doc |
| `A1356` | ✅ **做完**（两处都收成「事件 + 广播」两半齐） | `TrySwarmMerge` · `EmitBloodThirst`；并订正 `FireTriggerAlways` doc 里那句已过期的「本笔没动」 |

---

## 一、`A1352` —— `CS0162` 死代码 / 「场上没空格了」从不发声

### 结论
**做完。** 判据满足：满场那一支现在**出声**，其后的两句死代码删掉，口径（满场 = 什么都不做、返回 `false`）一个字没变。

### 改动清单
- `RuleCore.cs` `DeployFree(ctx, owner, inst, out slot)` —— `if (dst < 0) return false;` 改成
  `if (dst < 0) { ctx.Log($"{ps.Name} 场上没空格了 —— {card.Name} 部署不了"); return false; }`；
  函数尾部那句 `ctx.Log(...)` + 紧随的 `return false` **删掉**。

### 证据
- 改前：`internal` 编译器在 `:2750` 报 `CS0162 无法访问的代码`（`W5d` 的构建告警也是它）。
- 改后：`TMPDIR=/tmp/wf_rc bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0**（`CS0162` 属于**编译告警**，
  不是错误 —— 所以判据是「那两行不再存在」而不是「错误数变化」，`git diff` 里能看到它们被删掉）。
- 口径没动：这一支仍然是**无条件 `return false`**、不写棋盘、不发事件；只是多一行日志（原版
  「静默失败」红线的要求）。`ps` / `card` 两个变量在 `:2702-2703` 就声明了，作用域够。

### 我做的判断
原账给了两条路（挪进那一支 / 说明为什么该静默）。选**挪进去**：本仓红线是「**不许静默失败**」，
而这句话（场上没空格）是效果免费部署被吞掉时**唯一的**线索，静默没有任何理由。

### 还差什么
无。

---

## 二、`A1353` —— `OtherCardSacrifice`（225）广播

### 结论
⚠️ **判 (γ)：机制【不是没做】，而是 W4 那半句判据写错了** —— 它**早就在**，落在
`FireTriggerAlways` 免费带的那条 `WhenEventKind.Triggers("<kw>")` 广播里。
本笔**只改注释**（铁律 5：把错的记录就地订正），**没有**新开 `WhenEventKind`。

### 改动清单
- `RuleCore.cs` `CleanupDeaths` 幸存者支路里 `FireTriggerAlways(ctx, u, KeywordTable.Sacrifice, p, slot)`
  上方那段注释 —— 原来写「原版 `BroadcastUnitUsedSacrifice` 那是**独立的一条广播**、**不是**这个口免费带的
  `triggers:<kw>` 那一族 ⇒ 我们**没做**」，整段换成：**已做**＋三条实证＋为什么**不**另开 Kind。

### 证据（都是现读原文；这也是推翻 W4 那半句的东西）
1. **原版「别的卡因某关键词被触发而反应」那一族，落的 id 就是「本卡自己的那个词」在别的卡上那一份**：
   - `BattleManagerSupport__BroadcastUnitSwarm.c`：对**场上所有卡 + 当前回合方手牌 + 对手手牌**逐个
     `CardScript__TriggeredSwarm` → `CardScript__TriggeredSwarm.c:9-11`
     `RawCardScript.OnTrigger(raw, 0x26c, …)`，`0x26c = 620 = AbilityTrigger.TriggeredSwarm`
     ⇒ **与我们的 `triggers:swarm` 同一条**（`TL79 Termagant Brood` / `TL22 Tyranid Prime` 卡面写的
     就是 `When a friendly unit triggers Swarm, …`，靠的正是它；这两张卡在 `cards_engine.json` 里都在）。
   - `CardScript__TriggerOtherCardCodex.c:11-13` → `OnTrigger(0x28a = 650 = TriggeredCodex)`。
   - `CardScript__TriggerOtherCardMob.c:11-13` → `OnTrigger(0x285 = OtherCardMob)`。
   ⇒ 合成结论：原版这一族**id 本身就不统一**（Swarm/Codex 借 `Triggered<X>`，Mob/Sacrifice 用 `OtherCard<X>`），
   但**角色**是同一个 —— `OtherCardSacrifice = 225` 就是那个角色的 Sacrifice 版。
2. **我们的 `triggers:<kw>` 就是这个角色**：`FireTriggerAt` 尾部（有正文那条路）与
   `FireTriggerAlways`（没正文那条路）**都** `BroadcastWhen(WhenEventKind.Triggers(kw), …)`；
   而 `BroadcastWhen`（`EffectResolver.cs:5270+`）覆盖 **双方棋盘快照 + 两个方向的手牌**两跳
   —— 与 `BroadcastUnitUsedSacrifice`（场上 `+0x470` → 当前回合方手牌 → 对手手牌）的受众逐格相同。
   ⇒ 牺牲那一刻我们**已经**把这一族广播发出去了，而且**有没有 `Sacrifice:` 正文都会发**（两条路都覆盖）。
3. **接得通**（不是「发出去没人听」）：`sacrifice` 在 `CardDef.Implemented`（`CardDef.cs:2621`）里、
   `HasTriggerMoment("sacrifice")` 为真（`CardDef.cs:2356`）⇒ 卡面写 `When another unit uses Sacrifice, …`
   会被 `WhenEvent.TryParseKeywordTrigger`（`… triggers / uses <词>` 那一支，`WhenEvent.cs:981-1017`）
   收成 `triggers:sacrifice` 监听器，并在 `FireTriggerAlways` 那一跳被叫醒。**全链路已通。**

### 我做的判断（本笔唯一「判据没定」的那一处）
**不另开 `WhenEventKind`**，理由（写在代码注释里）：
- 新 Kind 要能用，就得配一条**卡面短语**；而 `… triggers / uses <词>` 这一支**已经吃掉所有这类写法**
  ⇒ 新 Kind 只有两种下场：**没有短语 = 死代码**，或者**与现有短语撞车 = 一次牺牲把监听器叫醒两遍**。
- 新短语本身**没有判据**：卡池 **1126 张**整条记录文本扫 `sacrific` —— 唯一命中是**卡名**
  `TAU65 Valued Sacrifice`，**0 张**卡带这个词条/正文；而数字版卡面整句在**远端词条表**
  （`RawCardScript__GetLocalizedCardDesc` → `Card_Description/<term>`）⇒ 英文措辞**拿不到**
  ⇒ 编一条短语就是**自己发明口径**（本仓红线）。
- 所以本笔**不动 `WhenEvent.cs`**（也就不需要白名单里那个文件）。

### 还差什么
- ⚠️ **一处未核（0 张卡可达）**：原版 `TriggerOtherCard*` 那一族里，「**被触发那张卡自己**要不要排除」是
  **不一致**的 —— `CardScript__TriggerOtherCardLanding/Mob/Codex` 里都有 `Object.op_Inequality(param_2, param_1)`
  **自排除**，而 `ReactToUnitUsedSacrifice` 的**棋盘那一跳**传的是**空**（`ReactToUnitUsedSacrifice(lVar4)`，
  只有 1 个实参）⇒ 棋盘上的卡**没有**自排除，只有手牌那一跳有。我们这边「自己会不会被自己叫醒」
  与它是否同形，**没核**。
- 全池 **0 张**卡能碰到这条链 ⇒ **不可观测**（按铁律 11「原版有、我们缺」才做；这里判成「我们没缺」）。

---

## 三、`A1354` —— `dropPod` 的「开舱」那一半

### 结论
✅ **两支做完**：**回合开始自动开舱**（`BeginTurn`）＋ **出手后自动开舱**（`DeclareAttack`），
两支都发 `Landing`（440）。
⚠️ **但原版「出手后」那一处还有第二支本笔做不了**（卡在文件白名单，理由见下）——
**并且本笔回头把原版那一段读透了，结论与原账/文档的说法不同**，见「我做的判断」。

### 改动清单
1. `RuleCore.cs` 新增 `const string LandingKeyword = "landing";`（带 doc：本该住 `KeywordTable`）。
2. `RuleCore.cs` 新增 `static void OpenDropPod(BattleContext ctx, int p, int slot, UnitState u)`
   —— `u.RemoveAll(DropPod)` + 日志 + `FireTriggerAlways(ctx, u, LandingKeyword, p, slot)`。
3. `BeginTurn` 的 `RefreshForNewTurn` 那一圈里，`u.RefreshForNewTurn();` **之后**加
   `OpenDropPod(ctx, ctx.Active, s, u);`。
4. `DeclareAttack` 的「伪装摘除」之后、`var target = …` **之前**加 `OpenDropPod(ctx, p, atkSlot, attacker);`。
5. `ApplyDamage` 里那句「原版只在**另外两处**发它…那两处**没做**」→ 改成「✅ **已做**，落点 = `OpenDropPod`」。

### 证据
**① 回合开始那一处**（判据 = `CardScript__OnTurnStart.c:102-108 / 263-266`）：
- `:102 bVar3 = false;` → `:103 HasCurrentTrait(param_1, 0xe6)` ∧ `IsInPlay` ∧ `param_2 == card.isPlayer`
  → `:107 RemoveDropPod(param_1,0)` + `:108 bVar3 = true;` → `:263-266 if (bVar3) OnTrigger(0x1b8 = Landing)`。
- 🔴 **`bVar3` 的语义没有歧义**：`grep -n bVar3 CardScript__OnTurnStart.c` ⇒ 整支函数里只有 **4** 处：
  `:7` 声明、`:102` 置假、`:108` 置真、`:263` 读 ⇒ 它唯一的意思就是「这一趟开过舱」。
  `.c` 里 `:263` 那段**被 Ghidra 套进了 `stun`/`psionics` 那串 if 里面**（渲染嵌套不可信）——
  **按 `bVar3` 读**才是它本来的位置。
- 该文件里**没有** `0x302`（= `DefinedTrait.landing = 770`）⇒ 回合开始那一支**不**带 landing 门。

**② 出手那一处**（判据 = `BattleManager._ResolveAttack_d__438__MoveNext.c:339-379`，
🔴 **本笔回 VA 读清的** —— 入口 RVA **10145552**（`all_methods.txt`）/ VA `0x1809ACE10`，ImageBase `0x180000000`）：

| 地址 | 指令 | 读作 |
|---|---|---|
| `0x1809ad4c8` | `mov edx,0xe6` → `0x1809ad4cd call`(HasCurrentTrait) | 判 `dropPod` |
| `0x1809ad4d4` | `je 0x1809ad847` | **没有 `dropPod` ⇒ 跳到「正常继续」** |
| `0x1809ad4ec` | `call 0x1805f6cb0` | `RemoveDropPod(card, 0)` |
| `0x1809ad50e` | `mov edx,0x302` → `call`(HasDefaultTrait) | 判 **`DefinedTrait.landing = 770`** |
| `0x1809ad51a` | `jne 0x1809ad55b` | **有 `landing` ⇒ 跳去取消攻击那一支** |
| `0x1809ad54f` / `0x1809ad556` | `mov dword[rdi+0x10],1` / `jmp` | 没有 ⇒ 装配 `WaitForSeconds`、**协程状态 = 1**、挂起 |
| — | 跳转表 `0x1809af80c`（27 项，`lea rdx,[rip-0x9ad0f1]` ⇒ 基址 = ImageBase）⇒ **state 1 → `0x1809ad7e8`** | —— |
| `0x1809ad7e8…0x1809ad841` | 两次 `op_Inequality` → **落到 `0x1809ad847`** | **与「没有 `dropPod`」那一支同一个出口 ⇒ 攻击照常打下去** |
| `0x1809ad56d` / `0x1805ea740`×2 / `0x1809ad5c1` | `CancelAttack` · `ClearPendingDamage`（双方） · `mov edx,0x1b8` → `call`(OnTrigger) | **有 `landing` 那一支：取消攻击 + 发 `Landing`** |
| `0x1809ad643` | `jmp 0x1809ad373` | `FinishResolvingAction` ⇒ **这一下不打了** |

**③ 类型检查**：`TMPDIR=/tmp/wf_rc bash d:/4/Unity/工具/typecheck.sh` ⇒ 运行时 0 · 编辑器 0。

### 我做的判断（两处，都写进了代码注释）
1. 🔴 **原版那一段的语义与原账/文档的说法不同**（本笔据 VA 订正）：
   - 「出手后开舱」= `RemoveDropPod` **无条件**（只要带 `dropPod`）；
   - **`Landing` 只在卡上带 `landing`（770）那个 trait 时才发**，而且**那一下会取消这次攻击**；
   - 不带 `landing` 的 ⇒ 开舱 + **等一帧** + **照常攻击**（不是「发 Landing」）。
   原账与 `CardDef.DropPod` 的 doc 都写成「两处**都**发 `Landing`」—— 那是照 `.c` 的表面读的，**出手那一处要加 `landing` 门**。
2. **把两支开舱收成一个 `OpenDropPod`**（不各写一份）：两个入口、同一件事 ⇒ 判据一处
   （铁律 10 第 5 条：一个对象有多个入口时每个入口要显式设置 —— 这里反过来，是「同一件事只有一个实现」）。
3. **`OpenDropPod` 里不调 `Auras.Recompose`**（本仓「棋盘写入点必须挂钩子」那条纪律的边界）：本函数
   **不改棋盘**（只摘一个词），而 `dropPod` **既不是任何光环的来源、也不会被任何光环授予** ⇒ 光环重算
   没有输入变化。注释里写死了「将来若某个光环读 `dropPod`，这里必须补一次」。

### 还差什么（如实）
- 🔴 **「出手后那一处」的第二支没做**：卡上带 `landing`（`DefinedTrait.landing = 770`）时
  ⇒ **取消这次攻击** + 发 `Landing`。**两条理由**（都不是「影响小」）：
  ① **`landing` 这个 trait 我们引擎里根本没有** —— 要判它得往 `KeywordTable` 登记（`CardDef.cs`），
     而**那个文件不在本笔白名单**；硬写 `u.Has("landing")` 只会得到**永远为假**的分支（死代码 + 魔法字符串）。
  ② **「取消这次攻击」在我们的形状里没有对应物** —— 原版那一下发生在**协程**里
     （`CancelAttack` + `ClearPendingDamage` 在 `yield` 之后、伤害之前取消一个**已声明未结算**的攻击）；
     我们的 `DeclareAttack` 是**同步一把跑完**，等价表达要在入口先预判一次 ⇒ 另一处改动、另一套断言。
  ⇒ **建议调度台单开一笔**（要动 `CardDef.cs` 的写手）。
- ⚠️ **`LandingKeyword` 住在 `RuleCore` 而不是 `KeywordTable`**（同一个白名单问题）。今天卡池 **0 张**卡提到
  `landing`，所以「没登记」不造成差异；**登记那一天必须挪**（否则卡面写 `Landing: …` 会被 `Normalize`
  静默丢掉 —— 那正是本仓「不许静默失败」那一族）。
- ⚠️ **一处次序近似**：原版 `OnTurnStart` 里 stealth 到期（`:110-118`）排在**开舱之后**，
  我们这边 stealth 到期在 `BeginTurn` **更早** ⇒ 这一格相对次序与原版相反；今天 0 张卡同时带两个词 ⇒ 不可观测。
- ⚠️ 原账里说的「池被伤害打空那条路**不发** `Landing`」—— 我们本来就是那样（照原版），**不动**。

---

## 四、`A1355` —— 预览侧「空投舱也是判死的一层」

### 结论
**做完。** 判据满足：`WouldKillByEntries` 现在把 `dropPod` 血池当**循环里的第一层**，且是 `if/else`
（走池子那一轮**不**落进幸存者/堡垒）；同批把两段自相矛盾的 doc 就地订正（铁律 5）。

### 改动清单
1. `RuleCore.WouldKillByEntries`：循环前取 `int podPool = u.KwValue(KeywordTable.DropPod);` +
   `bool podSpent = false;`；循环体内 `acc += DamageAfterReductionOne(...)` **之后**加
   ```csharp
   if (u.Has(KeywordTable.DropPod) && !podSpent)
   {
       if (acc < podPool) { podPool -= acc; acc = 0; }
       else { acc = 0; podSpent = true; }
       continue;
   }
   ```
2. `EnoughPendingDamageToDie` 的 doc：原来写「预览侧**照原版不读** ⇒ 本函数一个字都不改」→
   改成「✅ **2026-10-21（`A1355`）已补**」+ 排除「假象」那一步怎么做的 + 出处。
3. `WouldKillByEntries` 的 doc：删掉「`dropPod` 不在这一口里…本函数一个字都不改」那段，
   换成四条实的（VA 逐条）+ 「同一批要一并改」的说明；bullet 表**新增一条** `:255-264` 的第一层。
4. `ApplyDamage` 那条「被打空 ⇒ 不发 Landing」的注释：顺手改成「这一半是**照原版**的；另外两处已做（`A1354`）」。

### 证据
🔴 **本笔自己回 VA 复核过一遍**（不是照抄那份查证报告）——
`CardScript$$EnoughPendingDamageToDieWithDamageValues`，RVA `0x5EBE00` / VA `0x1805EBE00`：

```
0x1805ec228  mov   edx, 0xe6                 ; DefinedTrait.dropPod = 230
0x1805ec230  add   esi, ebx                  ; acc += 本条伤害（ebx = DamageAfterReductionOne）
0x1805ec232  call  0x180930810               ; EntityScript$$HasCurrentTrait
0x1805ec239  je    0x1805ec26b               ; 没 dropPod ⇒ 走幸存者/堡垒
0x1805ec23b  cmp   byte ptr [rbp+0x60], 0    ; 原版 bVar3 =「池已花掉」
0x1805ec23f  jne   0x1805ec26b               ; 花掉了 ⇒ 也走幸存者/堡垒
0x1805ec245  cmp   esi, r13d                 ; 累计 vs 池（r13d = iVar6）
0x1805ec248  jge   0x1805ec25a
0x1805ec24a  sub   r13d, esi                 ;   <  ⇒ 池 -= 累计
0x1805ec24d  xor   esi, esi                  ;        累计 = 0
0x1805ec255  jmp   0x1805ec100               ;        回循环顶
0x1805ec25a  xor   esi, esi                  ;   >= ⇒ 累计 = 0（整份丢给池）
0x1805ec25c  mov   byte ptr [rbp+0x60], 1    ;        bVar3 = true
0x1805ec266  jmp   0x1805ec100               ;        回循环顶
0x1805ec26b  ...                            ; ← 幸存者/堡垒只从上面两个 `je/jne` 进得来
```
⇒ **两支都以显式 `jmp 0x1805ec100` 回循环顶**、**谁也不落进 `0x1805ec26b`** ⇒ `if/else` 成立。
⇒ `0x1805ec25a xor esi,esi` 是**机器码里的真指令**（不是 Ghidra 渲染假象）；「`.c` 块顺序 ≠ 地址顺序」
这一次**没有**把语义弄反（真尾块在 `0x1805EC225–266` 这座小岛上，按地址顺序重读两支体与 `.c` 逐条相同）。
与 `.c` 的读法也一致（`CardScript__EnoughPendingDamageToDieWithDamageValues.c` 的
`do { while(true){…} if (iVar5 < iVar6) {…} else {…} } while(true);`）。

**断言的判据（本笔**没有**写断言 —— `RuleEngineTest.cs` 不在白名单，见「还差什么」）**：
- 夹具：**手工构造的 `CardDef`**（⚠️ **不是真卡** —— 全池 1126 张**0 张**声明 `dropPod`；
  唯一含这个词的是 **`SW31 Fenrisian Drop Pod` 的卡名**，它 `keywords` 只有 `Flying` / `Armour 1`）
  + `u.AddKeyword(KeywordTable.DropPod, 4)`。
- ① 池没穿：`WouldKillByEntries(u, {3})` ⇒ **假**（生命 5、池 1 吃掉这 3）；
- ② 打穿那一击**整份计入池**：`WouldKillByEntries(u, {3, 3})` ⇒ 累计到第二条时 `acc(3) >= pool(1)`
  ⇒ 池标记花掉、累计清零 ⇒ 该判据**仍为假**（除非血量那条被触发），且**第三条的伤害落回血**；
- **反向沙包（灭自证）**：把这一层退回旧写法（删掉那 6 行 `if`）⇒ ① 仍绿、**② 变红** ⇒ 两格结构上
  不可能被同一种错实现同时满足。🔴 注意 **必须同时断「池被清空 = `podSpent` 生效」与「池那一轮不落进幸存者/堡垒」**
  —— 只插一段 `if`（不 `continue`）会让**两边都算**，而只断①看不出来。

### 我做的判断
- 池的取法**沿用 `ApplyDamage` 同一口**（`u.Has(KeywordTable.DropPod)` / `u.KwValue(...)`），⛔ 没另开算式。
- 原版是**逐条**调 `HasCurrentTrait`（`0x1805ec232` 在循环体里）⇒ 我把它写在循环体内（不是循环外算一次），
  ⛔ 不为「省一次查表」偏离原版形状。
- 池为 0 但词还在（`Has` 真、`KwValue` 0）这一格：原版 `cmp esi,r13d`（r13d = 0）⇒ `esi >= 0` 恒真 ⇒ 走 else
  ⇒ 我的写法**逐字同形**（`acc < 0` 恒假 ⇒ else）。

### 还差什么
- ⛔ **本笔没写断言**：`RuleEngine/Editor/RuleEngineTest.cs` 与新增的 `RuleEngineTest_S14.cs`
  **都不在本笔白名单**（那是断言写手的文件）⇒ 上面那套夹具/判别式**只写在报告里**，
  需要调度台派给断言写手（**建议**：放进 `A1355` 的断言组，并注明「手工夹具、不是真卡」）。
- 池的**初值口径**仍是我们自定的（`KwValue(DropPod)`）—— 原版取自 `GameStaticData` 的全局常量
  （`ActivateTraitsOnSummonOrEnchantment.c:105-120` 读 `+0x1fc`），那份表本地没有。`A1335` 已如实标着，本笔不动。

---

## 五、`A1356` —— `EmitBloodThirst` / `TrySwarmMerge` 各缺「没正文那半边」

### 结论
**做完**（两处都补成「事件 + 广播」两半齐）。⚠️ 其中 `TrySwarmMerge` 那一半**其实 2026-09-30 就补过**
（W4 那句「有广播、没事件」**已过期**）—— 本笔把它**收成一次 `FireTriggerAlways` 调用**（同一个口）；
`EmitBloodThirst` 那一半是**真缺**，本笔补上。
另：`FireTriggerAlways` 的 doc 里那句「这两处本笔【没动】」也一并订正（铁律 5）。

### 改动清单
1. `RuleCore.TrySwarmMerge`：删掉手写的那两条跳
   （`BroadcastKeywordEvent(ctx, WhenEventKind.Triggers(Swarm), host)` + 一段自己拼的 `ctx.Emit(EvtKind.Trigger…)`），
   换成**一次** `FireTriggerAlways(ctx, host, KeywordTable.Swarm, p, hostSlot);`
   （`hostSlot` 那两行「按身份现查」保留 —— 那是连续棋盘补位那条判据，和有/无事件无关）。
2. `RuleCore.EmitBloodThirst`：在 `ctx.Emit(EvtKind.Trigger…bloodthirst…)` **之后**补
   `if (u.Card != null) BroadcastWhen(ctx, WhenEventKind.Triggers(KeywordTable.BloodThirst), p, u.Card, u);`
3. `RuleCore.FireTriggerAlways` 的 doc：把「已经这么犯过的两处…本笔没动」改成 ✅ 两处都已收口
   （并点明 `TrySwarmMerge` 旧说法过期、`RuleEngineTest` 有断言盯着它）。

### 证据
- **`TrySwarmMerge` 那一半已存在**：`RuleEngineTest.cs:12918-12930` 的 `TestSwarm` 第 ① 组就是
  「★ 虫群合并发一条 `EvtKind.Trigger`（keyword = swarm）」＋「★ 那条事件的格位 = 合并后还活着的右边那格」
  —— 该断言自 2026-09-30 起就在。W4 读到的是 `BroadcastKeywordEvent` **自身**体内没有 `ctx.Emit`
  （那是对的），但**没看到函数尾部那一句**。
- **收成一次调用是等价改写**：`swarm` **不在** `CardDef.RoutableTriggers`（`CardDef.cs:172-205`，
  那张表我逐项读过）⇒ `u.FxOps("swarm")` 与 `u.Effect("swarm")` **恒 null** ⇒ `FireTriggerAlways`
  **必走「没正文」那一支** = `ctx.Emit(EvtKind.Trigger, owner, slot, u.Name, keyword, effect:null, amount:0)`
  + `BroadcastWhen(Triggers(kw), owner, u.Card, u)` —— 与我删掉的两条手写跳**逐参数相同**。
  唯一变化是**次序**：原来是「先广播、后发事件」，现在是「先发事件、后广播」——而这正是原版的次序
  （`BattleManagerSupport__BroadcastUnitSwarm.c` 先 `DamageSignal.Raise`、再逐个 `TriggeredSwarm`）⇒ **更贴原版**。
- **`EmitBloodThirst` 那一半是真缺**：`u.Card.WhenTriggers` 里写 `When … triggers Blood Thirst, …` 的卡
  才需要这条广播 —— 全池 **0 张**（`cards_engine.json` 1126 张扫 `bloodthirst` 10 命中，全是
  `Blood Thirst` 词条/`Gain Blood Thirst` 载荷，**没有一条是监听器**）⇒ 今天不可观测、也不改任何现有行为。
- **判据「原版那几跳是不是都无条件」的答案**（原账留的那一问）：
  · **Swarm：是**。`BattleManagerSupport__BroadcastUnitSwarm.c` 函数体里，「发信号」与「逐个
    `TriggeredSwarm`」两件事**都在同一个无条件函数里**，中间没有任何 `if` 挡着 ⇒ 事件与广播**都要发**。
  · **Bloodthirst：`ActivateBloodThirst` → `SendHighlightBloodThirstAction` 那一跳也是无条件的**
    （`CardScript__ActivateBloodThirst.c` 三道门通过之后直接转调，`CardScript__FinishAfterAttack.c:83-99`
    与 `CardScript__UsedActiveAbility.c:21-23` 是同一个判据）；但原版那一跳**只是演出**，见下。
- **类型检查**：`TMPDIR=/tmp/wf_rc bash d:/4/Unity/工具/typecheck.sh` ⇒ 运行时 0 · 编辑器 0。

### 我做的判断
- `TrySwarmMerge` 用 **`FireTriggerAlways`**（而不是「照它的形状手写」）：这一跳在原版里
  **确实会跑宿主自己的 `Swarm:` 能力**（`OnTrigger(620)` 是在「场上所有卡」那个循环里对**包括宿主在内**的
  每一张发的）⇒ 走那个口**是照原版**。
- ⛔ **`EmitBloodThirst` 反过来**：**照形状手写、不调 `FireTriggerAlways`** —— 原版那一跳
  （`HighlightBloodThirst` = `BattleCardUI.HighlightBloodThirst` + `DisplayTriggerAnim`）
  **只演出、不结算**，调那个口会在「卡上真有 `BloodThirst:` 正文」那天**顺手把正文结算掉**（多算一件事）。
  今天没有这种卡（`bloodthirst` 也不在 `RoutableTriggers`），但这个口是移动靶。
- ⚠️ **如实标着**：原版**没有** `BattleManagerSupport__BroadcastUnitBloodThirst`
  （`BattleManager.*Broadcast*` 全表逐项数过）⇒ `EmitBloodThirst` 那一句广播**在原版没有直接对应物**；
  它是把 `A1336` 立的「触发了两半要一起发」**一致地**套到这个触发点上。0 张卡能注册 ⇒ 不可观测。

### 还差什么
- 同上：**本笔没写断言**（测试文件不在白名单）。建议断言（手工夹具）：
  · `TrySwarmMerge`：**已有的** `TestSwarm` 第 ① 组就盯住了事件那半；**要补的是广播那半** ——
    夹具 = 场上一张写 `When a friendly unit triggers Swarm, …` 的监听卡，期望合并时它被叫醒。
    **灭自证**：把 `FireTriggerAlways` 换回「手写 emit、不广播」⇒ 本格必红、而格位那一格仍绿。
  · `EmitBloodThirst`：夹具 = `bloodthirst` 单位 + 一张写 `When a friendly unit triggers Blood Thirst, …`
    的监听卡；打第一次 ⇒ 期望 `EvtKind.Trigger{bloodthirst}` **与**监听器都被叫醒。
    **灭自证**：删掉那一句 `BroadcastWhen` ⇒ 本格必红。

---

## 六、顺手发现（⛔ 只报不改；一行一条 + 出处）

1. 🔴 **`CardDef.cs:2640-2642` 的那段注释现在过期了**：它写「广播走 `BroadcastKeywordEvent`
   （体无正文也能发），**不是** `FireTriggerAt`」——`TrySwarmMerge` 已改成 `FireTriggerAlways`
   （`RuleCore.cs` 本笔那一处）。⛔ 我**没改**（`CardDef.cs` 不在白名单），请调度台派给能改那个文件的写手。
2. 🔴 **`CardDef.cs:2161-2167`（`KeywordTable.DropPod` 的 doc）有两处要改**，本笔已把判据查清：
   · 它写「`_ResolveAttack…:359-379`（本单位出手 → `RemoveDropPod` → `Landing`）」——
     **出手那一处要加 `landing`（`DefinedTrait.landing = 770`）这道门**，而且那一支会**取消这次攻击**；
     不带 `landing` 的只是开舱 + 等一帧 + 照常打（VA 证据见本报告 §三）。
   · 「**没有做的两半**」那条已过期：两支开舱时机**本笔已做**（`OpenDropPod`），
     只剩「带 `landing` ⇒ 取消攻击」那一支没做。
3. ⚠️ **`landing`（`DefinedTrait.landing = 770`）这个 trait 在我方引擎里完全没有**
   （`KeywordTable` 里没有常量、`Prefixes` 里没有条目、卡池 0 张提到）—— 与 `AbilityTrigger.Landing = 440`
   **是两个不同的东西、数值也不同**（440 vs 770），别混。
4. ℹ️ **`RuleEngine/Editor/RuleEngineTest_S14.cs` 是本会话另一个写手新建的未跟踪文件**
   （`git status` 里 `??`），**不是我建的**，我一行没碰。
5. ℹ️ **`CardDef.RoutableTriggers`（`CardDef.cs:172-205`）与 `Implemented`（`:2621` 一带）是两张不同的表**：
   `swarm` / `bloodthirst` / `survivor` / `sacrifice` / `droppod` / `bastion` 都在 `Implemented` 里，
   但**都不在** `RoutableTriggers` 里 ⇒ `FxOps`/`Effect` 对它们恒 null、`FireTriggerAt` 对它们
   永远只发事件与广播、不结算正文。判「某个词有没有正文」时要分清读的是哪一张表。
6. ⚠️ **`ReactToUnitUsedSacrifice` 的棋盘那一跳没有「自排除」**，而 `TriggerOtherCardLanding/Mob/Codex`
   都有（`Object.op_Inequality(param_2, param_1)`）—— 原版这一族**自身不一致**；我们这边是统一的
   `triggers:<kw>` 广播（不自排除）。0 张卡可达，如实记着。
7. ℹ️ **`grep` 出来的地址表用法**（可复用）：`d:/2/tools/all_methods.txt` 第二列是**十进制 RVA**
   （不是 VA），`VA = 0x180000000 + RVA`；`.c` 的块顺序不可信，**回 VA 一遍**常常能一次读通
   （本笔 `A1355` / `A1354` 两处都靠它）。IL2CPP 协程的 `yield` 恢复点可以**解跳转表**拿到
   （`lea rdx,[rip-…]` 的基址就是 ImageBase，`mov ecx,[rdx+rax*4+disp]` 里那个 `disp` 就是表偏移）。
