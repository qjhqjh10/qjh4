# B97_TUT · 报告（执行代理 · 2026-10-29）

> 判据来源 = `资料/完全复刻/判据/B97_可推A七条_审查.md`（独立审查，本代理**不重新论证**）。
> 本件记四件事：① `AiScripted.ExecuteAction` 的 `ResolveCard(140)` 取数口定性 ② `IsScriptedStageAvailable` 四道闸补齐
> ③ `GetAvailableActions` 两档闸改成「整表清空」 ④ 两条钉错口径的断言改正。
> ⛔ 本代理**没有**：跑 Unity 批处理 / 动 git / 改账主表 / 改正本 / 碰 `Editor/RuleEngineTest.cs`。

---

## 〇 总览

| # | 件 | 结论 | 动了哪个文件 |
|---|---|---|---|
| 1 | `AiScripted.ExecuteAction` 的 140 支取数口（账 `:93`） | **只改注释（零行数变化）** —— 残差① 定性为「不是差」，残差②（活跃实例）保留 ⇒ 账仍 B | `RuleEngine/Core/TutorialScript.cs`（注释 3 行就地改写） |
| 2 | `IsScriptedStageAvailable`（账 `:424`） | **四道闸补齐**（原版两个操作数各四道） | `RuleEngine/Core/TutorialScriptPredicates_B94B8.cs` |
| 3 | `GetAvailableActions` 两档闸（账 `:324`） | **改成「整表清空」（含 `endTurn`）** | `RuleEngine/Data/SimpleAI.cs` |
| 4 | 两条钉错口径的断言 | 按原版口径改正（并各补一条判别式断言） | `RuleEngine/Editor/RuleEngineTest_EngineB95WBD.cs` · `CardPresentation/Editor/B94B11_Assert.cs` |

**typecheck**（`TMPDIR=/tmp/wf_tut bash d:/4/Unity/工具/typecheck.sh`，每处改完各跑一次，最后一次在全部改完后）：
**运行时错误数 0 / 编辑器错误数 0**。

**账补丁** = `账/_parts/B97_TUT.tsv`（11 列 · 首行表头 · LF · 与主表同列序；3 行 = 主表 `战斗逻辑驱动.tsv` 物理行 93 / 324 / 424；
`方法` 列逐字从主表复制、均带 `(...)`；写盘后做过「读→写→再读、11 列逐列比对」自检）。

> ⚠️ **状态列 3 行都保持 `B`** —— 理由逐条写在补丁的「差异」列里：每一条都还剩至少一处**未闭合**（分别是 残差② 活跃实例 /
> `allowAutoActions` 那一档 / 两处白名单外残留）。**升 A 是独立审查的事，执行代理不自行升**（审查者 ≠ 作者）。

---

## 一 `RuleEngine/Core/TutorialScript.cs` —— 140 `ResolveCard` 支的取数口

### 审查给的两条路，我走了哪条、为什么

审查 `§一·1`「还差什么（能做，且零风险）」：
1. 把 `trigger 0` 取数口换成「按 `cardAbilities[trigger==0]` 的通用解析」，**或**在账里给出「`desc` 就是 trigger-0 的唯一源」的判据；
2. 账里补一句「取的是卡池 `CardDef` 而不是活跃实例」。

**走的是第 1 条的「或」那一半**（给判据），理由：本仓**没有**那条「通用解析」可取 —— 生成器的收词表把 trigger 0 明确写在「结构性表达不出、没有关键词」那一栏里。

**判据（现读）**
- `d:/4/Unity/工具/gen_cards_engine.py:1282` 原话：「⛔ **没收的那些是【结构性表达不出】，不是漏做**：0 `ThisCardPlayed`（单位 Rally 正文 / 战术卡正文，**走 `ResolveDeploy` / `Desc` 那条路，没有关键词**）」。
  ⇒ 本仓的路由表 `CardDef.TriggerOps(keyword)`（`RuleEngine/Core/CardDef.cs:1811`）按**关键词**取正文，而 trigger 0 **没有关键词** ⇒ `TriggerOps` 里根本不会有 trigger-0 的条目。
- 同口径 `RuleEngine/Core/CardDef.cs:4534`：「战术卡 / 天赋卡走 `PlayTactic` → `EffectText.Parse(card.Desc)`，**本来就会结算**」。
- ⇒ `EffectText.Parse(card.Desc)` **就是**本仓 `cardAbilities[trigger==0]` 的唯一取数口，**不是残余差**。

**代码改动**：`TutorialScript.cs:1451-1453`（原「残差（如实标）…对其余不等价」三行）**就地改写成**「残差①（定性）+ 残差②（活跃实例）」，**3 行 → 3 行，行数零变化**（`git diff --numstat` = `3 3`；改后 `RunResolveCard` 声明行仍 = `:1454`，`RuleEngine/Core/TutorialScript.cs` 在账里被引的 `:1403 / :1443 / :1528 / :1734 …` 全部未动）。

**顺带闭合审查列的一条「没查清」**（`§三·3`：`RunResolveCard` 用的 `Enchantments.TriggerEnchantment` 是否真连发 0 + 500）：
现读 `RuleEngine/Core/Enchantment.cs:1324-1362` —— ① `ParseEnchantOps(enchantmentCard)` = `EffectText.Parse(card.Desc)`；
② `enchantmentCard.TriggerOps(KeywordTable.TriggerEnchantment)`（= 500）；两次都 `RuleCore.ResolveOps(...)`，**形状与原版 `RawCardScript__TriggerEnchantment.c:7-13` 一致**（`isEnchantment = 1` 那一格在 `RuleCore.ResolveOps` 侧，`.c:8` 的字面量 1 已在 `Enchantment.cs:1294-1296` 的 doc 里记明）。⇒ **该条「未核」闭合**。

**残留（因此账仍 B）**：残差② = 我们按 `data[0].acting.ourId` 从**卡池**查 `CardDef`（`TutorialScript.cs:1467-1470`），原版取 `data[0].actingCard`
那张**活跃 CardScript 实例** ⇒ 实例身份 / 运行时状态（临时改写数值、运行时生成的卡）丢失。今天数据 0 条（6 关 `actionTypeNote` 14 个取值无 130/140）⇒ 休眠。

---

## 二 `RuleEngine/Core/TutorialScriptPredicates_B94B8.cs` —— `IsScriptedStageAvailable` 四道闸

### 原版（两份同形，本轮现读全文）

- `d:/2/tools/decomp_full/BattleManager__IsScriptedStageAvailable.c:1-15`：`IsTutorialStageAvailable() || IsCampaignStageAvailable()`。
- `BattleManager__IsTutorialStageAvailable.c` / `__IsCampaignStageAvailable.c`（同形）四道闸：
  ① `IsTutorialMatch()` / `IsCampaignLike()`；② `PlayerDataManager.get_currentTutorialStage|CampaignStage != null`；
  ③ `ObscuredInt` 的 `turnCounter(+0x3f8) > 0`；④ 末句 `CONCAT31(…, iVar2 + -1 < *(int *)(stage[+0x48] + 0x18))` = **`turn - 1 < turns.Count`**（List `_size` @`+0x18`）。
  `+0x48 == 0` 的两次 `goto LAB_18096e2fa` ⇒ **返 false**。⚠️ 原版**不要求** `scriptedTurn`。

### 改成什么

`TutorialScriptPredicates_B94B8.cs:64-67`（声明行仍是 `:64`，账引的就是 `:64`）：

```
return SingleStageAvailable(ctx.Tutorial != null ? ctx.Tutorial.Stage : null, ctx.Turn)
    || SingleStageAvailable(ctx.CampaignStage, ctx.Turn);
```

新成员**追加到类尾**（`:70-82`，`static bool SingleStageAvailable(TutorialStageData stage, int turn)`，声明行 `:75`）：
`stage == null ⇒ false`（②）· `turn <= 0 ⇒ false`（③）· `stage.turns == null ⇒ false`（原版 `+0x48 == 0` 那两跳）· `(turn - 1) < turns.Length`（④）。
①（match）由 `ctx.Tutorial != null` / `ctx.CampaignStage != null` 承载（判据 = `RuleCore.cs:166` 只在教程局赋 `ctx.Tutorial`；战役半今天恒 null）。
`scriptedTurn` 这一条**已去掉**（原版没有）。

**为什么 `ctx.Turn` 可以顶 `+0x3f8`**：`RuleCore.cs:274` 建场写 `ctx.Turn = 0`、`RuleCore.cs:1710` 的 `BeginTurn` 开头 `ctx.Turn++` ⇒ 第 1 回合时 `Turn == 1`；`TutorialScript.Turn` 的 doc 也写明「1 起；= 原版 `BattleManager.turnCounter +0x3F8` 的口径」。

### 断言同步（`CardPresentation/Editor/B94B11_Assert.cs` C 段，`:187` 起）

原来 C3/C4 断言的是「stage 非空即 true」= **原版返回 false 的格**（审查 `§一·3` 点名的错口径）。改成：

| 断言 | 输入 | 期望 | 判据 |
|---|---|---|---|
| C3 | `Stage.turns == null`，`Turn = 1` | **false** | 原版 ④（`+0x48 == 0` 直接返 false） |
| C3b | `turns.Length = 1`，`Turn = 1` | **true** | 四道闸全过 |
| C3c | 同上但 `Turn = 0` | **false** | 原版 ③ `turnCounter > 0` |
| C3d | `Turn = 2`，`turns.Length = 1` | **false** | 原版 ④ `turn-1 < Count` 不成立 |
| C4 | 战役半 `turns == null` | **false** | 战役半同一套四道闸 |
| C5 | 战役半 `turns.Length = 1`，`Turn = 1` | **true** | `IsCampaignStageAvailable()` 那一半 |

（C0/C1/C1b/C2/C5b 未动。）

---

## 三 `RuleEngine/Data/SimpleAI.cs` —— `EnumerateActions` 的两档闸

### 原版（`decomp_full/BattleManager__GetAvailableActions.c`，本轮现读 `:35-140`）

`:35-59` 清三张复用表 → `:60-77` 两档闸 →
```
if (param_3 == 0 /* !allowAutoActions */) {
    if (param_2 == 0 /* !isPlayer */) { if (+0x246 == 0) goto LAB_180964050; hand = +0xd8; }
    else                              { if (+0x244 == 0) { LAB_180964050: return *(param_1 + 0x560); } hand = +0xd0; }
} else { hand = (param_2 == 0 ? +0xd8 : +0xd0); }
```
⇒ **闸不满足 = 立刻返回那张已被清空的表**；`:69` 在棋盘段（`:131` 起）与 `endTurn`（`:339-341`）**之前**。
⇒ 我们原来在 `:326` 只把手牌段的循环上界压成 0（「只挡手牌那一半」）是**读错 `.c`**。

### 改了哪三处（行数全部零变化）

1. `SimpleAI.cs:326`：`if (me != ctx.Active) return list; for (int i = 0; i < mine.Hand.Count; i++)`
   —— 闸与手牌循环**写在同一行**，替换掉原来那一行循环（原来那句三元上界 `me == ctx.Active ? … : 0` 随之作废）
   ⇒ **净 0 行**（`git diff --numstat` = `4 4`）。⚠️ 为什么挤一行：`SimpleAI.cs` 被账按**行号**引了十几处
   （`:485 / :584 / :786 / :826 / :966 / :1421 / :1436 / :1506 / :2095` …），插一行会让它们**全部 +1 漂移**。
   改后现读：`:316` = 声明 · `:326` = 闸+循环 · `:442` = 路标石循环 · `:449` = `endTurn` 追加（**全部原位**）。
2. `SimpleAI.cs:325` 的分段注释改成「不满足 ⇒ 整张表清空：`.c:62/67` 的 `goto` 落在 `:69 …`，`endTurn` 在 `:339-341`（返回点之后）」。
3. `SimpleAI.cs:314` 那段总 doc 的「我们的等价物 = 调用点 `BattleDriver.cs:14466` 的 …」**就地订正**为 `if (me != ctx.Active) return list;`
   （并注明 `allowAutoActions` 那一档我们没有等价物、⛔ 不造无人能置真的形参）；`:316` 的签名尾注同步改。
   ⇒ `SimpleAI.cs:316` 的声明行与 `:449` 的 `endTurn` 追加行**都还在原位**（改后现读：`:316` = 声明、`:326` = 闸、`:449` = `endTurn`）。

**残留**：原版 `allowAutoActions == true` 时**旁路整道闸**；我们 `EnumerateActions` 没有这个形参（AI 只枚举自己那侧、六处调用（`:1585/:1637/:1729/:1816/:1856` 等）一律不给 `forSide`）⇒ 生产里 `me == ctx.Active` 恒真、闸恒过。这一点写进了 `SimpleAI.cs:314` 的 doc，**没有**造成死形参。

---

## 四 断言修正

### 4a `RuleEngine/Editor/RuleEngineTest_EngineB95WBD.cs`（⚠️ 简报里写的是 `CardPresentation/Editor/…`，实际路径是 `RuleEngine/Editor/…`，全仓 `find` 只有这一份）

| 位置 | 原来（错口径） | 现在（原版口径） |
|---|---|---|
| 文件头 `:16-19` | 「**只给「手牌段」加闸**…`endTurn` 一律不受影响」 | 「闸不满足 ⇒ **整张表为空**（连 `endTurn` 都没有）」＋ 就地标注「原来写只挡手牌段，是读错 `.c`」 |
| 方法 doc `:43` | 「手牌候选 0 条，但 **`endTurn` 仍在**」 | 「**整张表为空**（手牌 0 ∧ 连 `endTurn` 也没有；判据 `.c:62/67 → :69`）」 |
| 断言 `:89` | `CheckTrue(foeHasEndTurn, "`endTurn` 仍在 ……")` | `Check(foeList.Count, 0, "★★ 非行动方那侧整张表为空 …")` ＋ 新增 `CheckTrue(!foeHasEndTurn, "★★ `endTurn` 也不在 …")` |

（`Check(handMine, 1 …)` / `Check(handMineExplicit, 1 …)` / `Check(handFoe, 0 …)` 三条未动 —— 它们在原版口径下仍然成立。）

### 4b `CardPresentation/Editor/B94B11_Assert.cs` C 段

见上文 §二 那张表（C3 / C3b / C3c / C3d / C4 / C5）。

---

## 五 ⚠️ 白名单外的一处**已知会红**的断言 —— 请调度台派给 `Editor/RuleEngineTest.cs` 那一族的写手

`RuleEngine/Editor/RuleEngineTest_EngineB94B8.cs:106-108`（`B94B8T2TutorialPredicates`，由 `RuleEngineTest.cs:1352` 的
`{ … CheckB94B8Engine(); … }` 块调用 ⇒ **会真的跑到**）仍然钉着旧口径：

```csharp
                ctx.CampaignStage = new TutorialStageData { stage = 1 };            // :106  turns == null
                CheckTrue(TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
                          "战役半非空 ⇒ `IsScriptedStageAvailable` = true（原版 `IsCampaignStageAvailable()` 那一跳）");   // :107-108
```

按原版（`BattleManager__IsCampaignStageAvailable.c` ④ `turn-1 < turns.Count`）这一格应返回 **false** ⇒ **我这批改完它会红**。
可粘贴补丁（把 `:106-108` 三行换成下面七行；`ctx` 来自 `Battle(...)`，其 `Turn` 在 `RuleCore.cs:274` 被置 0 ⇒ 必须显式设 1）：

```csharp
                ctx.Turn = 1;
                ctx.CampaignStage = new TutorialStageData { stage = 1 };
                CheckTrue(!TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
                          "🧨 战役半非空但 `turns == null` ⇒ false（原版 ④ `turn-1 < stage[+0x48].Count`；`decomp_full/BattleManager__IsCampaignStageAvailable.c`。2026-10-29 `B97_TUT` 订正：原来这条期望 true，钉的是「两条 null 判断」的旧口径）");
                ctx.CampaignStage = new TutorialStageData { stage = 1, turns = new[] { new TutorialTurn() } };
                CheckTrue(TutorialScriptPredicatesB94B8.IsScriptedStageAvailable(ctx),
                          "战役半 `turns.Length = 1` ∧ `Turn = 1` ⇒ true（原版 `IsCampaignStageAvailable()` 那一跳）");
```

另一处**不是断言、但同源**的残留（也在白名单外）：`CardPresentation/Battle/BattleDriver.cs:36920` 的消费者仍写着
`!…IsScriptedStageAvailable(Ctx) || tut == null || tut.CurrentTurn == null`，而 `TutorialScript.CurrentTurn`（`TutorialScript.cs:674-686`）
**额外要求 `turns[i].scriptedTurn == true`** —— 原版四道闸里**没有**这一条（审查 `§一·3` 的那格「不等价（反向）」）。
它属表现层的前置（下面 `TutorialRules.IsWaitingUseAbility` 要 `tut.CurrentTurn`），本代理白名单外 ⇒ **未动**，记在这里。

---

## 六 没查清 / 留给下一批

1. **`allowAutoActions == true` 那一档**（§三残留）——原版是「旁路两档闸」，我们没有任何等价物，也没有调用方需要它。要不要造这个形参，留给调度台按「没有调用方 = 死代码」那条纪律判。
2. **原版 `GetAvailableActions` 的 ④ 档（Aeldari 残骸 + `CanUseWaystone`）与 ⑤ `endTurn` 的先后**：本轮只核了「闸在两者之前返回」，**没有**重核 ④/⑤ 与 ③ 的相对次序（审查 `§一·2` 的五段表已给，我按它就够，未独立复核 `:307-341` 的每一行）。
3. **`RuleCore.DeployFreeCore` 是否该补 `Tally.AddCardPlayed`**（审查 `§三·2` 的存疑，属账 `:500`，**不在本代理四件里**）——没查，仍在 `:500` 的账上挂着。
4. **`BattleCardUI +0x260 (summonSicknessFrame)` 是否恒 null**（审查 `§三·1`，属账 `:930`）——没查，同上。
5. **Unity 自检未跑**（红线：子代理不跑批处理）⇒ `RuleEngineTest.Run` 里 `B95WBD` 那套新断言、以及 `B94B11_Assert` C 段，**都还没有被执行过**；`RuleEngine/RuleEngineTest_EngineB94B8.cs:107` 的那条已知会红（见 §五）也需要先按补丁改掉再跑，否则整条 `CheckB94B8Engine()` 会带着一条红。

---

## 七 改动清单（全部落在白名单内）

| 文件 | 改了什么 | 行数变化 |
|---|---|---|
| `RuleEngine/Core/TutorialScript.cs` | `:1451-1453` 注释就地改写（残差① 定性 + 残差② 记明） | **0**（`git diff --numstat` = `3 3`） |
| `RuleEngine/Core/TutorialScriptPredicates_B94B8.cs` | `:56-67` doc + 四道闸调用；类尾新增 `SingleStageAvailable`（`:70-82`） | +14（`git diff --numstat` = `21 7`；全部落在 `:68` 之后，`:64`/`:67` 未动） |
| `RuleEngine/Data/SimpleAI.cs` | `:314`/`:316`/`:325` 注释就地订正 · `:326` = 闸 + 手牌循环（同一行） | **0**（`git diff --numstat` = `4 4`） |
| `RuleEngine/Editor/RuleEngineTest_EngineB95WBD.cs` | 文件头 `:16-19` · 方法 doc `:43` · 断言块 `:89-92` → `:89-96` | +3（`git diff --numstat` = `11 8`） |
| `CardPresentation/Editor/B94B11_Assert.cs` | C3/C4 改正为 C3/C3b/C3c/C3d/C4/C5 | +8（`git diff --numstat` = `22 14`） |
| `账/_parts/B97_TUT.tsv` | 新建（3 行补丁：主表 93 / 324 / 424） | 新文件 |
| `报告/B97_TUT.md` | 本件 | 新文件 |
