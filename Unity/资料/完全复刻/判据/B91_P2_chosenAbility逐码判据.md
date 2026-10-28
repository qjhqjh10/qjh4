# B91 · 判据代理 `P2` —— `chosenAbility`（`AbilityEffect`）逐码判据总表

> 产出配套：`B91_P2_chosenAbility逐码判据.tsv`（86 行 = 83 个枚举值 + 3 个空档/默认行）
> 时间：2026-10-28 · 判据权威顺序：`d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c` → `cpp2il/out_cpp2il_dev_ilspy/AbilityEffect.cs` → 我们的 `RuleEngine/Core/*`。
> 我在本轮**只读不改**：没动任何 `.cs` / 账主表 / 工具，没跑 Unity、没动 git。

---

## 〇 一句话结论

**86 个码里 52 个已实现（覆盖池内 1,527 / 1,533 条「卡×码」出现 = 99.6%）、2 个只差接线（380 / 480）、0 个真没做、32 个休眠不可达（池内只剩 2 张，且都是 340 的数据门）。**

⇒ **`W2` 报的「86 个码只落了 2 个」是【口径差异】，不是缺陷**：它数的是 `chosenAbility` 的**显式 switch 分支**（我们只有 `370/400` 走 `RunGrantedTriggerRow`）；其余码的**语义**是通过「`CardDef.AttachByAbilityKind` 把 logic 行配到正文 op」＋ `EffectDispatch` 动词表实现的 —— 见下文 §二·1。

---

## 一 口径与可复算命令（别人要复核就照这三条）

1. **码表**：`cpp2il/out_cpp2il_dev_ilspy/AbilityEffect.cs` = **83 个枚举值**（0 … 480）。本表 86 行 = 83 + 附录索引里的 3 个非码行（`73/74/76/78/79` 空档 · `463/464/469/471-474` 空档 · `default`）。
2. **池内张数**（TSV 第 3 列）：读 `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`count = 1129`），逐卡并 `chosenAbility[]` 与 `activeAbilityLogic[].ca[]` 取并集。
   - ⛔ 不要用 `cards_raw.json`（每卡两份同构技能树 ⇒ 恰好翻倍）。
   - ⚠️ `cards_truth.json`（1,351 张 SO）比池大：多出来的主要是**附魔模板 / 池外卡**。本表按**池**计；池外那部分写进 `依赖` 列（例：`340` SO 7 条 / 池内 2 条、`72`/`160`/`185` SO 各 1 条但池内 0）。
   - 复算片段（只读，`-I` 跑）：
     ```
     python -I -X utf8 -c "import json,io,collections;d=json.load(io.open(r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json','r',encoding='utf-8'));c=collections.Counter();[[c.update(set((x.get('chosenAbility') or [])+[v for g in (x.get('activeAbilityLogic') or []) for v in (g.get('ca') or [])])) for x in d['cards']];print(sorted(c.items()))"
     ```
3. **原版行为**：逐条读 `decomp_full/AbilityLogic__PlayAbility.c`（2,878 行）的 `switch(iVar8=chosenAbility)` 分支，`判据出处` 列给行号区间；方法名 + 实参取自分支体。

---

## 二 三处口径订正（旧索引 / 旧报告里【过期】的）

### 1. 「86 个码只落了 2 个」的正确读法
- 我们**没有**逐族 `switch (chosenAbility)`（原版有）。
- 我们的形状是：`CardDef.AttachByAbilityKind`（`Core/CardDef.cs:3413`）按 `chosenAbility` 的**族名**把 `activeAbilityLogic` 行配到该卡**正文解析出来的 op** 上，再由 `EffectDispatch`（`Core/EffectResolver.cs:1061` 起的 50 键表）执行。
- 所以「码有没有实现」= **该码对应的动词在不在 `EffectDispatch` / `RuleCore` 里** + **池内卡的正文解不解得出它**。
- 现存的**显式** chosenAbility 分派只有两处：`ca == 370 / 400` ⇒ `RunGrantedTriggerRow`（`Core/EffectResolver.cs:18490`）、其余 ⇒ `RunActiveRowOps`（`RuleEngine/Core/RuleCorePartial_B88E1.cs:224`）。
- 主动能力行只有 **132 张卡 / 193 行 / 19 个码**（0,10,20,30,40,60,70,75,100,130,140,238,300,320,325,420,462,467,468）；**其余码全部落在被动/触发能力行**上，走正文 op。

### 2. 旧索引 `PlayAbility分支索引.tsv` 的 **C 判**有 4 个是错的（不止 W2 订正的那 5 行）
| 码 | 旧索引 | 本轮实测 | 证据 |
|---|---|---|---|
| **420 setCardDeckToTop** | C「SetCardToTop 0 命中」 | **已实现** | `EffectResolver.cs:5009` `DoChooseCard` 的 `case "decktop"`；池内 7 张（DA1/DA11/DA16/DA40/DA72/SW1/Master Chronomancer）正文都是 "put … at the top of your deck" ⇒ `EffectText.cs:6619` 产 `ChooseAct="decktop"` |
| **270 createCardInDeck** | C「AddNewCardToDeck 0 命中」 | **已实现** | 同一张表的 `case "todeck":5017`（DA44 实测把 Secret 随机插入牌库）＋ `decktop`（DA72 实测复制进牌库） |
| **90 transformUnit** | C「我们无变身机制」 | **已实现** | `DoBecome`（`EffectResolver.cs:3491`）+ `EffectText.cs:3909` `TryBecome`；`play SW18` 实测手牌 Stratagem → `SW5 Hunting Wolf` |
| **360 playChosenCardAbility** | C「战术卡 oath 整支无出口」 | **已实现** | `DoChooseOne`（`EffectResolver.cs:4557`）＋ Oath 链；`play ASH71`（Choose one）/ `play UM109`（Oath 1）都能结算 |

> 错因（与 W2 那 5 行同源）：**索引写在这些能力落地之前，之后没回来改**。⇒ 本表取代旧索引的「判定 / 缺什么」两列；旧索引的「原版行号区间 / 语义一句话」仍可用（本轮抽查吻合）。

### 3. 池内数据与旧文档不一致的两格
- **120 jam**：旧索引判 A，但**池内 0 张**（`cards_engine` 逐卡扫 `chosenAbility` = 0；`desc` 匹配 `\bjam` = 0 命中）⇒ 本表判**休眠/不可达**（`DoJam` 已建，等数据）。
- **230 forceAttack**：旧索引判 A，但**池内 0 张**（SO 也 0）⇒ 休眠；真正可达的是 **231/235/236/237/238**（分别 2/2/1/1/6 张）。

---

## 三 结论分布（口径：TSV 第 7 列）

| 结论 | 行数 | 卡×码 出现次数 |
|---|---|---|
| **已实现** | **52** | **1,527** |
| **只差接线** | **2** | 4 |
| **真没做** | **0** | 0 |
| **休眠/不可达** | **32** | 2 |
| 合计 | 86 | 1,533 |

---

## 四 施工顺序（按「池内张数」降序；张数多的先做）

### ① 现在就能施工的只有 2 条（其余全是「机制已在、等数据」）

| 顺序 | 码 | 枚举名 | 池内张数 | 要做的事 | 判据 |
|---|---|---|---|---|---|
| 1 | **380** | setReferenceCard | **3**（AM54 / GOF93 / GOF104） | 把 `SetReferenceCards` 接上生产调用点：`chosenAbility 380` 那一行要把「本条目标」写进 `actingCard.referencedCards`，之后 `it` 指代读它而不是 `ctx.LastTarget` | 原版 `PlayAbility.c:2128-2160`；`EffectResolver.cs:1366` 现有结构 + 其 doc 自述「无生产调用点」 |
| 2 | **480** | highlightBloodThirst | **1**（SW34 Murderfang） | Slay 正文（Heal 2 + gain Blood Thirst）结算后补一条「亮一下」 | 原版 `PlayAbility.c:1960-1972` → `SendHighlightBloodThirstAction`；我们已有 `RuleCore.EmitBloodThirst`（`RuleCore.cs:12441`）与表现侧 `BattleDriver.cs:24879`，只需在 480 那条被动行/对应时点补发 |

> 这两条**都在我白名单之外**（要改 `Core/EffectResolver.cs` / `Core/RuleCore.cs`）—— 转岗施工时**先更新文件所有权**，且按铁律「新成员追加到类尾」避免账漂移。

### ② 休眠档按「SO 条数」降序（等数据；机制多半已建）

| 序 | 码 | 枚举名 | 池 / SO | 机制状态 | 「回来」的条件 |
|---|---|---|---|---|---|
| 3 | **340** | removeThisEnchantment | 2 / 7 | 附魔摘除机制在（`Enchantment.cs:957`），但唯一可达路要求 `cardEffect.spellType == 15`，**池内 0 张** | 池内出现 `spellType=15` 的附魔 → 立刻可达（`ActionPump.cs` §七 已写全判据） |
| 4 | **13** | doDamageFast | 0 / 2 | `DoDeal` 无 `isFast` 位 | 池内出现该码；补 `isFast` 第 8 参语义（原版 `PlayAbility.c:1510`） |
| 5 | **15** | doDamageAndDestroySelf | 0 / 1 | 「打完自毁」未建模 | 同上；补自毁那半（`PlayAbility.c:1629-1674`） |
| 6 | **72** | summonUnitAndTriggerEffect | 0 / 1 | 召唤链整体在（`DoDeploy`） | 池内出现该码 |
| 7 | **160** | discardFromHand | 0 / 1 | **无 discard 动词** | 池内出现该码 → 需先建 `discard` 动词（原版 `PlayAbility.c:588-603`） |
| 8 | **185** | stealMinionSelf | 0 / 1 | `DoTakeControl` 只建模「本回合临时」 | 池内出现该码 → 补「永久偷」那一档 |
| 9-32 | 其余 24 个码 | — | **0 / 0** | 多数机制已建（120 jam · 170/175/190/200/210 休眠动词 · 280 revealOrder · 290 missAction · 370/400 已建） | **SO 里 0 条** ⇒ 等卡池扩充；⛔ 不要凭空造数据（铁律 2） |

> ⚠️ **休眠 ≠ 不做**（铁律 11）：本表把它们记成「等数据」，不是「砍掉」。其中 `170/175/190/200/210` 的 verb 已在 2026-10-27 那批落下，缺的确实只是**解析层生产者**（`EffectText` 不产 `stealhand`/`stealdeck`/`exchange`/`copycemetery`，因为池内卡面一句都没有）。

---

## 五 「我自己就能做完」的

- **380 / 480** —— 判据已齐、改动都在 `Core/EffectResolver.cs` 与 `Core/RuleCore.cs` 两个文件里，改完 `bash 工具/typecheck.sh` 就能收（属铁律 14 的「迅速」三条全中）。
- **340 的数据门再核一遍** —— 只需读 `cards_engine.json` 的 `spellType` 分布复证「15 = 0 张」，纯只读，可顺手做（本轮已核：池内 `spellType` 无 15）。
- ⛔ **NOT in my scope**：其余 50 条已实现码**不需要动**；休眠 24 条**没有数据可做**。⇒ 本批不做「为了填表而补代码」的动作。

---

## 六 判「休眠/不可达」的证据分类（按 §五 的 D 口径）

| D 类 | 码 | 证据 |
|---|---|---|
| **(b) 数据恒 0（池内 0 张）** | 13 · 15 · 71 · 72 · 80 · 85 · 120 · 135 · 160 · 162 · 165 · 170 · 175 · 185 · 190 · 200 · 210 · 220 · 230 · 233 · 240 · 280 · 290 · 310 · 330 · 345 · 350 · 370 · 375 · 390 · 400 | `cards_engine.json` 1129 张逐卡扫 `chosenAbility`/`activeAbilityLogic.ca` 该值出现 **0** 次 |
| **(b) 数据恒 0（唯一可达路的前置位为 0）** | 340 | 原版唯一调用方双闸 = `abilityLogic==0x154(340)` ∧ `cardEffect.spellType==0x0f(15)`；池内 `spellType==15` 的 **0 张**（判据全文 → `Core/ActionPump.cs` §七·账 166） |

⛔ 本表**没有**任何一条按 (a) 空桩 / (c) 四源负死代码 / (d) 架构不适用 判 D —— 也就是说：**没有哪个码是「原版本身就没有」**，它们全部是「原版有、我们数据里没有」。

---

## 七 边界（本表覆盖了什么 / 没覆盖什么）

1. **本表判的是「语义有没有实现」，不是「每一张卡都跑对了」**。机制层用「动词在不在 + 池内正文解不解得出」判；**逐卡**只做了 15 张代表卡的实打对拍：
   `AM44`(12) · `BL55`(250) · `SAU74`(101) · `SW18`(90) · `AM47`(475) · `GSC71`(465) · `DA20`(467/468) · `ASH46`(260) · `ASH73`(325) · `DA72`/`DA44`(270) · `DA51`(301) · `ASH71`/`UM109`(360) · `GSC55`(470)。
   **⇒ 残余风险 = 「动词对、但个别卡正文解析错」**，那属「卡面正文到达率」那条更大的账，不在 86 码这一层。
2. **主动能力行的实际执行率没有量化**：`CardDef.ActiveAbilityMap` 只在运行期可得（`attached` / `gap:*`）。本表只判「该码的语义能不能被执行」，**没有**统计 193 行里有多少行真的配到了 op。要量化需一次 `ActiveAbilityMap` 全池 dump（属另一批）。
3. **`FollowUpSchedulerOf` / `ShowCardsWindowOf` 是硬编码卡 id 表**（`EffectResolver.cs:7796` / `:7846`）：卡池换 id 或改能力**必须重生成**，否则静默少做（表内已标在 `依赖` 列）。
4. **池外卡未计**：`cards_truth.json` 1,351 张比池多 222 张（附魔模板等）；`72/160/185/340` 的「SO 有、池内没有」就属这一类。它们**不影响本表的施工顺序**（打不出去的码先不做）。
5. **`chosenAbility` 不是我们引擎的运行期驱动位**：我们靠正文 op 驱动，`chosenAbility` 只在「主动能力行配对」那一处被读（`CardDef.AttachByAbilityKind`）。⇒ 若将来切到「按 `chosenAbility` 直驱」，本表的「已实现」会退化成「语义有、分派要重写」——这一点写在这里，免得下个会话误读。

---

## 八 转岗施工（2026-10-28 · 调度台授权后追加）

调度台在收工前授权把「只差接线」那 2 条 **真做掉** + 订正索引。本轮结论：**索引与账已改；两条代码没有落，原因是有硬阻断，不是时间问题。**

### 8·1 `480 highlightBloodThirst` —— 阻断：唯一可用的载体不能当等价物

- 原版 `AbilityLogic__PlayAbility.c:1960-1972`（`case 0x1e0`）：对目标列表里**第一个**调 `SendHighlightBloodThirstAction(target)`，**四道闸一道都没有**。
- 我们能顶替的只有 `RuleCore.EmitBloodThirst`（`Core/RuleCore.cs:12441`），它带原版 `CardScript.ActivateBloodThirst` 的四道闸：
  `Has(bloodThirst)` ∧ `AttacksThisTurn == 1` ∧ `p == ctx.Active` ∧ `!IsPendingDeath`。
  ⇒ SW34 的 Slay 发生在正文结算之后、`AttacksThisTurn` 多半不是 1 ⇒ **会直接早退**，补上去等于没补。
- 想绕开就得自己 `ctx.Emit(EvtKind.Trigger, keyword=bloodthirst)`，但 `BattleContext.Emit`（`Core/BattleContext.cs:1866`）会
  ① 调 `CountTriggerForAchievements`（**成就 `Ach_*` 的触发计数会变**）；② `Signals.Add` ⇒ 进 `NetProtocol.Fingerprint`（**动 `play check` 钉住的基线**）。
  而原版 480 那一跳**只演出、不计数、不广播**（`EmitBloodThirst` 的 doc 已自证「原版没有 `BroadcastUnitBloodThirst`」）。
- **正确做法（下一批）**：新开一个专用 `EvtKind`（`BattleEvent.cs`）+ `BattleDriver` 一个分支，或给 `Emit` 加一条「不计成就」的旁路。
  两者都在本批白名单外（`BattleEvent.cs` / `BattleDriver.cs` 均未授权）⇒ 移交。

### 8·2 `380 setReferenceCard` —— 阻断：行级目标拿不到，且两个读者里只有一个还有意义

- 写侧语义已定（`SetReferenceCards`，`EffectResolver.cs:1366`）：覆盖式写入 `actingCard.ReferencedCards`，来源 = **本条 logic 自己的目标表**。
- 但 380 行的 `ta = 150 (previousAbilityFiltered)` **在运行期拿不到**：`cards_engine.json` 没有 `chosenAbility` 列，
  `abilityTargets[].ta` 是**整条 ability 的数组**（GOF93 = `[50,150,20]`），无法定位哪一格属于 380 行
  ⇒ 任何调用点都只能靠猜 —— 正是 `SetReferenceCards` 的 doc 明令禁止的那种。
- 三个 380 卡的现状（本轮实测/代码核对）：
  | 卡 | 380 的 ta | 读者 | 现状 |
  |---|---|---|---|
  | **GOF93 Hunta Rig** | 150 | ab1 `ca=235` + **ta=201 referencedCardsInPlay** | **已经正确**：`play GOF93` 实测「敌方手牌那张被部署、被 Hunta Rig 打死」⇒ 走 `prev`/`LastTarget` 那条路，`ReferencedCards` 本来就不参与 |
  | **AM54 At All Costs** | 150 | ab1 `ca=30` + **ta=200 referencedCards** | **结构上不可达**：它是**战术卡**，`UnitState` 载体不存在（`EffectResolver.cs:1507` 已如实出声「手牌卡这条载体本批没做」） |
  | **GOF104 Drag it Down** | 30 | 无 200/201 读者 | 写了也没人读 |
- ⇒ 补调用点**不会改变任何可观测结果**，却要在 `EffectResolver.cs` 中间加行（下游账行漂移）；若强行让 `it` 改读 `ReferencedCards`，那是**换一条指代主链**，属于大改。
  **建议**：等「手牌卡载体」那条结构件落地后一起做；在那之前保持 B。

### 8·3 本轮实际落地的两类改动

**① `判据/PlayAbility分支索引.tsv` 就地更正 8 行**（表头留更正痕迹 + 判定列图例）：

| 分支 | 码 | 原判 | 新判 | 依据 |
|---|---|---|---|---|
| B21 | 90 transformUnit | C | **已实现** | `DoBecome:3491` + `TryBecome:3909`；`play SW18` 实测 |
| B51 | 270 createCardInDeck | C | **已实现** | `DoChooseCard` 的 `todeck:5017` / `decktop:5009` / `copies:5115`；`play DA44`/`DA72` 实测 |
| B63 | 360 playChosenCardAbility | C | **已实现** | `DoChooseOne:4557` + Oath；`play ASH71`/`UM109` 实测 |
| B71 | 420 setCardDeckToTop | C | **已实现** | `DoChooseCard` `case "decktop":5009` |
| B84 | 475 resetDuty | C | **已实现** | `DoReloadDuty:15196`（**调度台未点名的额外一条**，同样过期） |
| B25 | 120 jam | A | **休眠** | 池内 0 张（`DoJam:15998` 机制在） |
| B41 | 230 forceAttack | A | **休眠** | 池内 0 张（`DoForceAttack:3207` 机制在） |
| B85 | 480 highlightBloodThirst | C | **B（只差生产点）** | 表现链 A（账 `战斗界面_HUD:759/766`），生产点见 §8·1（**额外一条**） |

**② `账/_parts/B91_P2.tsv`**（11 列 · LF · 2 行，其余列照抄主表现值）：
- `战斗逻辑驱动` / `AbilityLogic.PlayAbility(...)`：在 `差异` 里**就地更正**「C 的 19 支是真缺口」那段（把 90/270/360/420/475 五项划掉），并写入 52/2/0/32 的逐码结论与池内覆盖 99.6%。
- `战斗界面_HUD` / `CardScript.SendHighlightBloodThirstAction()`：A 不改（它说的是**表现等价**），但把「480 行没有生产点、`EmitBloodThirst` 四闸不等价、⛔ 别用 `EvtKind.Trigger` 直接补」写进 `差异`/`备注`。

**③ 门禁**：`TMPDIR=/tmp/wf_p2 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（本轮没改任何 `.cs`）。

### 8·4 给下一批的最小施工单（两条，判据已齐）

1. `480`：`BattleEvent.cs` 加 `EvtKind.BloodThirstHighlight`（**不进 `CountTriggerForAchievements`**）+ `BattleDriver` 里按 `PlayTraitPulse` 同一份演出；
   生产点 = `RuleCore` 里 SW34 的 Slay 正文结算之后（硬编码 1 张表，与 `FollowUpSchedulerOf` / `ShowCardsWindowOf` 同形）。
   ⚠️ 加行必然漂移 ⇒ 放在**重同步点**做，随后 `check_ledger_points --all-A --fix`。
2. `380`：先做「手牌卡引用表载体」（`UnitState.ReferencedCards` 之外的那一份），再把 AM54 的 `ta=200` 读通；
   GOF93 那条**不用动**（已正确）。同样放在重同步点。
