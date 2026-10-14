# E · 引擎落地批 A（`A1155①②` + `A1335①`）

> 会话：第三十一会话（2026-10-14）· 施工卡 = `资料/任务_卡组卡牌战斗.md:31`（`A1155`）与 `:37`（`A1335`）
> 判据第一权威一律 = `d:/2/tools/decomp_full/` 的反编译方法体，**逐行读过**（不是转述账文）。

---

## 一笔一结

### `A1155①` · Codex「只触发一个单位」那半条 —— ✅ 落地

- **结论**：原版**没有**「只触发一个」这回事；我们 `CheckCodex` 循环尾那句 `break` 是**多写的** ⇒ **已删**。
  现在「能量恰好归零」时，**每一个**带 `Codex` 的己方在场单位各触发一次。
- **判据（逐行现读）** `d:/2/tools/decomp_full/BattleManager__ResolveTriggerPlayerCodex.c`（518 行全文）：
  - `:97 lVar16 = *(longlong *)(param_1 + 0x470)` = 扫 `BattleManager.CardsInPlay`（**全场**）；
  - `:285-509` 的 `do { … :509 iVar18 += 1; } while(true)` **循环到 `list.Count` 为止**
    （`:286 if (*(int *)(lVar16 + 0x18) <= iVar18)` 成立才走 `:286-334` 的收尾：
    `:334 FinishResolvingAction` + `return`）⇒ **触发完一张接着下一张，没有早退**；
  - 函数里 `break` 共 **5 处**（`:391` / `:396` / `:444` / `:495` / `:499`），**全部**是
    `X == 0 ⇒ break` 形状的 **null 守卫**（`grep -n "break;"` 逐行数过）；
  - 逐张的两道判据 = `CardScript__IsInPlay` ∧
    `:445 *(char *)(card + 0x40) == *(char *)(local_140 + 0x40)`（**同队**；`+0x40` = `EntityScript._isPlayer`）
    ⇒ `:496 CardScript__TriggerCodex` ＋ `:500 HasCurrentTrait(card, 1000 /*codex*/)` 再广播一次。
- **改了哪几行** `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`：
  - `:8080-8083` `<summary>` 首行「触发**一个**」→「触发**每一个**……**己方**单位」+ 就地订正说明；
  - `:8099-8125` 原来那段「仍未核的一条」→ 换成**已核**的逐行判据（含上面那五处 `break`、
    两个幻影局部量、以及顺序那条如实标着的未验点）；
  - `:8148-8156` 循环体（原 `:7931-7937`）：**删掉 `break;`**，补一段「为什么删 / 别再加回来」的注释；
  - `:8157` 原 `:7936` 的 `break;` 一行 —— **删除**。
- **⚠️ 没查清 / 如实标着的两点**（都写进代码注释了）：
  1. `local_140` / `local_240` 是**幻影局部量**（全文件**从未赋值**，`grep` 逐行数过）——
     Ghidra 没建模出那两个形参。从用法只能确定「`:184-185` 先把某一张自己的 codex 触发一次」
     且「`:388 op_Inequality(local_240, 本项)` 在循环里跳过某一张」⇒ **并集与我们「扫一遍 `p.Board`」相同**，
     但**触发次序**可能不同（原版把那一张排最前、我们按槽号升序）。这一条**未验**。
  2. `AbilityLogic__PlayAbility.c:1694 AddTriggerCodex` 落在一个 `while` 里
     （能力 `0x19a = 410 = FollowUpResolution`）⇒「**强行触发** Codex」那条路**可能也是多目标**
     —— **没逐句核**（账上本来就有这条顺手而记）。
- **自检侧**：有一条**老断言把这个错口径钉死了**，已**翻面**（见下「改了自检」一节）—— 属于账文要求的
  「一律现核」，不是顺手改。

---

### `A1155②` · 祈祷状态 `Prayed` 的时长 —— ✅ 两处都落地

- **结论（真值区间）**：「**祈祷那一刻**」→「**下一个回合开始（不分敌我）**」**或**「**自己挨一次伤害**」，
  **以先到者为准**。账文里那句「一直挂到**本单位控制者的**下个回合开始」**是错的**（我们原来比原版多活一整个回合）。
- **判据**：`EntityScript.isPraying`（`+0x67`）在**全量反编译里只有 3 个写点**
  （`grep -rn "param_1 + 0x67)" decomp_full/` 逐行数过）：

  | # | 出处 | 内容 | 我们落在哪 |
  |---|---|---|---|
  | 置真 | `BattleManager__ResolveTriggerPray.c:266` | `set_isPraying(target, 1)` | `RuleCore.UseAlternative`（已对） |
  | 置假① | `CardScript__OnTurnStart.c:100` | `*(undefined1 *)(card + 0x67) = 0;` —— **无条件**，且对**全场**每张在场卡各跑一次（`BattleManagerSupport__BroadcastTurnStart.c:20-26` 扫 `CardsInPlay`；`:35-51` 再把双方手牌各扫一遍） | **新增** `RuleCore.BeginTurn`（**不分敌我**） |
  | 置假② | `CardScript__ResolveDamageDealt.c:168` | 闸 = `:166 op_Equality(param_2 /*damagedCard*/, param_1 /*this*/)` ⇒「受伤的就是自己」；`BattleManagerSupport__BroadcastUnitDamaged.c:19` 广播时先给受伤那张卡自己调一次 | `RuleCore.ApplyDamage`（**新增**）+ 疲劳那一处（**新增**） |

- **⚠️ 这里有一条我第一版写错、现核后推翻的地方（写下来免得下轮踩）**：
  `ResolveDamageDealt` 在 `:168` **之前**还有**两道早退闸**：
  - `:31 if (damageInfo.damageAbsorbed != 0) return;`（字段布局：`DamageInfo` 存根字段序 = `actingCard(+0x10)`
    `damageValue(+0x18)` `damageType(+0x1c)` `secondaryValue(+0x20)` `damageAbsorbed(+0x24)`；
    与 `_ReceiveDamage` 里 `+0x18`/`+0x1c==4` 那几处读法**互相印证**）
    ⇒ **被盾 / 闪避 / 无敌 / 空投舱（整条支路）/ 堡垒整份吃下 的那几档【不算挨到伤害】、不清**。
    （`CardScript__GetAdjustedDamage.c:36-60` 把 `dropPod(0xe6)`/`dodge(0xf0)`/`invulnerable(0x136)`/`shield(0x50)`
     任一存在 ⇒ `absorbed = 1`；外加 isCombat 且 `damage <= CurrentBastion` ⇒ 1。
     它在 `BattleManager__ResolveAttackDamage.c:28-29` 里排在 `ReceiveDamage` **之前**算好
     ⇒ 空投舱「池子被这一下打空」也照样 `absorbed = 1`。）
  - `:34 if (damageInfo.damageValue == 0) return;` ⇒ **这一下本来就是 0 点**时不清。
- **改了哪几行**：
  - `RuleCore.cs:1284-1316`（`BeginTurn`，逐卡那圈之后、`ResolveAtTurn("turn_start")` 之前）：
    **新增**「不分敌我、扫双方 `Board`、清 `Prayed`」那一圈（原版先清 `:100` 再跑 `:137+` 的 `TurnStart` 触发，同序）。
  - `RuleCore.cs:5835-5863`（`ApplyDamage`，`u.Health -= actual;` 之前那条**共用尾巴**）：
    **新增** `if (!absorbed && dmg != 0) u.Prayed = false;`（两条早退闸各一个条件）。
  - `RuleCore.cs:5656-5672`：新增局部量 `hit` / `alreadyVulnerableHit` / `absorbed` 的说明与声明
    （`hit` 与 `absorbed` 是为 `A1335①` / `A1155②` 各加的，见下）。
  - `RuleCore.cs:5682-5686`：空投舱那一段开头 `absorbed = true;`（整条支路都是「被挡下」）。
  - `RuleCore.cs:5777-5783`：堡垒「整份吃下」那一支补一句说明（它直接 `return`，天然同形，⛔ 别补清）。
  - `RuleCore.cs:1908-1918`（`MoveDeckTopToHand` 的疲劳分支）：**新增** `ps.Warlord.Prayed = false;`
    —— 原版疲劳走 `_ResolveFatigue_d__587__MoveNext.c:125-137 DealMultiDamage` →
    `_ResolveMultiDamage_d__456__MoveNext.c:141 BroadcastUnitDamaged`，**同样落进**这一口；
    而我们那一支是**直扣血 + 手发 `EvtKind.Hit`**（绕开了 `ApplyDamage`）⇒ 必须单独补。
  - `UnitState.cs:204-216` 字段 doc（时长**已核**，⛔ 别再写「按回合重置」）；
    `UnitState.cs:1218-1228` `RefreshForNewTurn`：**删掉 `Prayed = false;`**，留指针注释说明搬去哪、为什么别加回来。
  - `RuleCore.cs:7667-7684`（`UseAlternative` 里 `u.Prayed = true` 那段）：把「仍未核」的 doc 换成**已核**的逐行判据。
- **⚠️ 没查清 / 近似（如实标着）**：
  1. **「逐卡 vs 整段」的近似**：原版是**逐卡**「解疲劳 → 清 `+0x67` → 开舱…」；
     我们拆成「逐卡（只己方）」＋「整段清 `Prayed`（双方）」两步。那一刻**没人读** `Prayed`
     （`OpenDropPod` / stealth 到期 / 晕瞎闸门都不读它）⇒ 今天不可观测，但结构上确实不是同一次遍历。
  2. **堡垒那一格没区分 isCombat**：`GetAdjustedDamage.c:52-60` 只在 `isCombat` 时把堡垒折进 `absorbed`；
     我们引擎不携带 `isCombat`，一律当「堡垒整份吃下 ⇒ 被挡下」。今天 0 张卡带 `bastion` ⇒ 不可达。
  3. **没跑 Unity 对局验证**（本批不动 Unity 批处理）—— 上面两条是读出来的，**不是跑出来的**。

---

### `A1335①` · 易伤那一份（两档）—— ✅ 落地

- **结论**：原版的易伤是**另一条独立的 `DealMultiDamage`**（`_ReceiveDamage…:252-254`），
  它**自己再走一遍**整条管线。我们把它**折进同一次调用**（不另发事件），照施工卡补两档：
  - **② 堡垒被打穿**：原来 `actual = -remaining`（溢出量）**覆盖**掉了含易伤的那个值 ⇒ **少补一份**。
    现在 = `溢出量 + max(1, 易伤 − 护甲)`，闸 = **打穿** ∧ `hit > 0` ∧ **溢出后血还 > 0** ∧ **不是残骸**。
    判别式：堡垒 3 / 这一下 5 / 易伤 2 / 护甲 1 ⇒ 原版掉 **3**，改前我们只掉 **2**。
  - **① 有空投舱**：池子**还在**（`after >= 1`）⇒ 第二发**也落进池子**（不减护甲、生命一点不动）；
    池子**已被这一发打碎**（`after < 1`）⇒ 第二发走**普通路**（`dropPod` 已摘 ⇒ 落到下面堡垒/血那段共用尾巴）。
- **判据（逐行现读 `CardScript._ReceiveDamage_d__381__MoveNext.c`）**：
  `:56/:58-60` 挡下三连（dodge/invuln/shield）→ `:60 HasCurrentTrait(0xe6)` 空投舱优先 →
  `:63 (bastion < 1 || deathType == 10)` → `:64 notRemnant` → **`:93 Math.Max(1, dmg − armour)`、`:94` 原地写回
  `DamageInfo+0x18`** → `:128 RemoveBastionDamage`、**`:130 bVar17 = (iVar8 >= 0)`** → `:145-153 iVar8 < 1` 时
  `health += iVar8`（负 = 溢出进血）；易伤第二发 `:188-191` 触发闸（含 **`!bVar17`**）· `:197 IsVulnerableDamage`
  · `:200` 进伤 > 0 · `:210-211` **血还 > 0 ∧ 不是残骸** · `:226 DamageInfo___ctor` ·
  `:252-254 BattleManager__DealMultiDamage(…, priority 2, …)`。
- **改了哪几行** `RuleCore.cs`：
  - `:5550`（`ApplyDamage` 顶部）新增局部量 `hit = dmg`（「这一发打进管线里的量」，唯一改写点 = 池已碎那档）；
  - `:5615` / `:5618`：`VulnerableDamagePart` 那段 doc 里「三条今天都不可达 ⇒ 不做」的过期说法**就地订正**
    （现在①②已做、只剩③）；
  - `:5620-5790`：那一段「为什么不做（ⓐⓑⓒ）」整体改写为「①②已落地 / ③仍开着」；
  - `:5706-5769`：空投舱那一段 —— 新增第二发路由（池还在 / 池已碎两支）+ 触发闸 + 逐行判据；
  - `:5771-5810`：堡垒那一段 —— `dmg` → `hit`，打穿支改为 `overflow + vuln`（带上面那三道闸）；
  - `CardDef.cs:3113-3129` `Bastion` 的 doc：「打穿且血还 >0 那一档我们**没补**」**就地订正为「已补」**；
  - `CardDef.cs:3188-3193` `DropPod` 的 doc：补一段「池子还吃易伤第二发」。
- **🔴 仍然没做的那一档（③，如实标着，账面另有）**：`:210-211` 那道闸里的
  「**主伤害之后血还 > 0**」—— 我们在**普通档**（既无空投舱也无堡垒）仍是**先把两份加起来再判血**
  ⇒ 「主伤害本来就能打死它」时**会多算这一份**（影响 `ApplyDamage` 的返回值 = 事件 `Amount` / 践踏溢出量；
  血本身由 `CleanupDeaths` 兜住）。
  ⚠️ **它与①②两个模型无关、是纯顺序问题，而且 `vulnerable` 是【可达的】**（全池 0 张卡带 `dropPod`/`bastion`
  ⇒ ①②今天不可达，这一档可达）⇒ **建议主对话立刻立账**。
  修它要把普通档也拆成「两次结算」⇒ 要动 `DamageAfterReduction`（被 `RuleEngineTest_S14.cs:633` 钉着）
  与预览侧（`DamageAfterReductionOne` / `WouldKillByEntries`，**必须同式**）⇒ 另立一笔。
- **⚠️ 没查清的（如实标着）**：
  - `priority = 2` 在动作队列里插到哪儿（账上就标着**未查**）⇒「池子被打碎的**同一时刻**第二发是否已经看到
    `dropPod` 被摘」这一格我们按「摘了」处理（判据 = `CheckIfDead.c:94` 在 `_ResolveMultiDamage` 收尾跑、
    第二发是**另一条 action**）。
  - `DealMultiDamageVariable` / `DealMultiDamageAndDestroySelf` 有没有同样的第二发 —— **本批没读**。
  - **事件形状是近似**：原版两发 = **两条** `BroadcastUnitDamaged`；我们折进同一次调用 ⇒ 只发**一条**
    `EmitHit`（「池还在」那档连血都不动 ⇒ 无差异；「池已碎」那档真掉血、走共用尾巴发一条）。
  - 「池已碎 + 易伤 + 同时带 `bastion`」这个**三重组合**走的是「第二发重新磕堡垒」那条路（`hit = podVuln`
    + `alreadyVulnerableHit` 挡住第三发）—— 判据是推出来的，**没跑对局验过**。
- **零变化论证（今天）**：全池 1126 张里 `dropPod` / `bastion` **各 0 命中** ⇒ 新增的两支第一个条件即假、
  逐字节跳过；档案面上「普通档」一行未动（`DamageAfterReduction` 没改）。

---

## 改了自检（都是「老断言把错口径钉死了」，不是顺手改）

`Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`：

1. **`:12153-12196` `④` —— 翻面**：原来断的是「**一次只触发第一个**（原版那条 `break`，
   别改成「全体各来一次」）」，判据是**我们上一版 Godot 复刻** `rule_core.gd:2397` 的 `break`（**旁证、非权威**）。
   ⇒ 改成「**两个都触发**（3 号格 2 → 3）」，并写上判别式：
   **把 `CheckCodex` 循环尾那句 `break` 加回去 ⇒ 本行必红**。
   ⚠️ 这是「**自检绿 ≠ 口径对**」的活例 —— 它绿了很久，只是把我们的错钉住了。
2. `:12078-12086` 同一节的文件头注释：`rule_core.gd:2397 _check_codex` 这个判据来源 → 换成原版反编译。
3. `:1546-1548` + `:1583-1618`（`Prayed` 那一节）：
   - 注释就地订正（原来写「要**两次** `PassTurn`：`RefreshForNewTurn` 只刷当前行动方」—— 那是改前口径）；
   - 断言加强：**一次 `PassTurn` 就掉**（原来那一句 `PassTurn; PassTurn;` 已被切成两句、各带一条断言）；
   - **新增两格**：③ 自己挨一次伤害 ⇒ 掉（判别式：删掉 `ApplyDamage` 里那句 ⇒ 红）；
     ④ **被 `Shield` 全挡下 ⇒ 不算挨到伤害、`Prayed` 仍在**（判别式：把那句挪到函数最前面 ⇒ 红）。
4. `RuleEngineTest_S14.cs:616-676`（`TestS14VulnerableWithBastion`）：
   - 方法头那一段「⛔ **别把『打穿之后会少算』写成期望**（那会把我方缺口固化成断言）」**就地订正** ——
     那一档**已经补上了** ⇒ 现在**必须**断它；
   - `#20` 那一格的 `⛔ 别补一条…` 也订正；
   - **新增 `#20-bis`**：堡垒 3 / 易伤 2 / 5 血 / 打 5（打穿、溢出 2、血还 3 > 0）⇒
     `ret == 4`（溢出 2 ＋ `max(1, 2 − 0)` = 2）、`Health == 1`、堡垒摘掉、`Hit{Amount = 4}` 一条。
     判别式：把堡垒那段的 `actual = vulnSecond ? overflow + vuln : overflow;` 改回 `actual = -remaining;`
     ⇒ **本格前两行都红**。
5. `:3113-3129` / `:3188-3193`（`CardDef.cs`）只是 doc，无断言。

**没动的自检**（我逐个核过、它们本来就绿）：`TestS14BastionAbsorbsWholeHit`（#15/#16/#16 边界）、
`TestS14CounterIgnoresBastion`（#17）、`TestS14DropPodPool`（#18）、
`TestS14DropPodTakesPrecedenceOverBastion`（#19）、`TestS14VulnerableWithBastion`（#20）——
它们都**不带 `vulnerable`** 或取的是「整份吃下」那一档，`hit == dmg` ⇒ 日志串与返回值**逐字节相同**。

---

## 自检 / 验证（本批跑过的）

| 项 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_engine1 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 错 · 编辑器 0 错**（末次） |
| 纯 dotnet 快内环 | `bash d:/4/Unity/工具/ruleprobe.sh check` | **rc=0 全绿**（解析差异 0 行 · 哨兵失败 0 条 ⇒ **不需要重钉指纹**） |
| 行尾 | `python -I` 数 CRLF/LF | `RuleCore.cs`/`UnitState.cs`/`CardDef.cs` = **纯 LF**（未翻）；`RuleEngineTest.cs` = **CRLF 28931/28931**（未翻） |
| `git diff --numstat` | —— | `RuleCore.cs 284/63` · `UnitState.cs 22/6` · `CardDef.cs 15/5` · `RuleEngineTest.cs 66/12` |

⛔ **没跑 Unity 批处理**（按简报）· ⛔ **没动 git** · ⛔ **没改正本**。

**中途一次假红（如实记）**：第二次 typecheck 报 `Assets/CardPresentation/Core/CardIcons.cs(227,…)`
三个 `CS0103`（`HasOfficialMarks` / `RewriteMarks` / `DescZhField`）—— **不是我负责的文件**
（`CardPresentation/`，我在 `RuleEngine/`），隔一会儿重跑即 0 错 ⇒ 判为**别人的半成品**，
**没有去改**（按简报与铁律 13·3）。

---

## 顺手发现（照报，⛔ 没顺手改）

1. 🔴 **`A1335①` 的第三档（普通档 `:210-211` 闸）是【可达】的、仍开着** —— 详见上面那一节末尾。
   建议主对话立账（我按简报「只做两档」的范围没动它）。
2. **`decomp_full/CardScript__SetPray.c` 的内容不是 `SetPray`** —— 现读整文件 =
   `EntityScript__set_isPraying(longlong param_1, undefined1 param_2) { *(undefined1 *)(param_1 + 0x67) = param_2; }`
   ⇒ 「文件名与内容对不上」**又一例**（账上已并入 `A1962`，此处只是**新增实证**）。
3. **`ResolveTriggerPlayerCodex.c` 里有两个【从未赋值】的幻影局部量**（`local_140` / `local_240`）——
   Ghidra 没建模出的形参。凡是要按这个文件逐句核的行为（比如「强行触发 Codex 是不是多目标」），
   **都要先把这两格认出来**，否则会把 `!: X == 0` 那样的守卫读成业务逻辑。
4. `FilterMethods__MeetsConditionCriteria.c:296 case 0xf3` 读的也是 `+0x67`（= `isPraying`）
   ⇒ 那是原版「正在祈祷」的**目标筛**，与我们的 `EffectTargetSpec.PrayedOnly` **同源**（旁证，不是新账）。
5. `RuleCore.cs` 里 `DamageAfterReduction` 的护甲那一段**缺原版 `:77` 的 `damageValue < 1` 守卫**
   （原版 `dmg < 1` 时**不扣护甲**；我们 `if (u.Armor > 0) actual = Max(1, actual - u.Armor);`
   会把 `dmg = 0` 变成 `1`）。**本批没动**（超出范围，且要连带预览侧一起改才同式）—— 记为疑点。
