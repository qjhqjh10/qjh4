# B9_E1RNG — 随机源改成原版形状（P-04）

> 执行代理 `E1RNG` · 2026-10-27 · 只动 `RuleEngine/Core/`（+ 新文件 `BattleContextRandom.cs`）。
> ⛔ 没碰 `CardPresentation/**`、正本、`账/*.tsv` 主表、`工具/ruleprobe/基线/`。

---

## ① 结论

**引擎随机源已换成原版形状**：开局预烧 **100 个整数**进 `ctx.RandomNumbers`，结算路径
一律 `ctx.GetRandomInt(minIncl, maxExcl)` **按下标取**（游标只前进、按 100 取模回绕）。
结算路径上的 `ctx.Rng.Next` **已清零**（`grep` 只剩注释）；`ctx.Rng` 现在**只当烧表的种子流**。

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_E1RNG bash 工具/typecheck.sh` | **0 / 0** ✅ |
| `bash 工具/ruleprobe.sh check` | **绿**（解析差异 0 · 哨兵失败 0 · rc=0）—— 解析层不受影响 |
| `bash 工具/ruleprobe.sh play check` | **rc=1（预期）**：基线过期，主档 **100 张**、电池 **76 张** 变 |
| 改前结算指纹 | `1b40f42ab010e623e908fcd4a236205f` |
| **改后结算指纹** | **`b671acdb0b60d86b17ffe3a9f9e69c25`**（1714 行 = 主档 1128 + 电池 586） |
| 确定性 | 连跑两次指纹逐位相同（`play all pf` 自身判据自检全过、rc=0） |

**为什么必然变**：旧实现每次调用 `System.Random.Next` **掷一次**；新实现**按游标读一张固定的表**。
同一条动作序列在旧实现里拿到的值与新的**分布形状相同、具体值不同** ⇒ 随机伤害区间、随机挑牌、
洗牌、随机回填位、随机回库位这五族全部换值。**这不是回归**：账 `战斗逻辑驱动.tsv` 早就把
`MatchData.GetRandomInt` / `BattleManager.GetRandomInt` / `MatchData.GenerateRandomNumbers` /
`SupportMethods.GetRandomOrder` 四行判 **C（口径不同）**，本次就是消这条 C。

---

## ② 证据（文件:行号）

### 2.1 原版判据（逐句现读，`d:/2/tools/decomp_full/`）

| 方法 | 文件 | 要点 |
|---|---|---|
| `MatchData.GenerateRandomNumbers` | `MatchData__GenerateRandomNumbers.c:1-40` | 开局 `new int[100]`（`FUN_1803f37c0(…,100)`）→ 逐格 `= UnityEngine.Random.Range(0,1000)` → 游标 `+0x28 = 0` |
| `MatchData.GetRandomInt` | `MatchData__GetRandomInt.c:8-40` | 表空 ⇒ `LogError` + **返回 0**；`max <= min+1` ⇒ **返回 min、不消耗**；否则 `idx = 游标 % Count` → `v = list[idx]` → 游标++ → `return min + v % (max-min)` |
| `BattleManager.GetRandomInt` | `BattleManager__GetRandomInt.c` | **只是转发**到 `MatchData` ⇒ 全引擎**一张表** |
| `SupportMethods.GetRandomOrder` | `SupportMethods__GetRandomOrder.c:1-98` | 洗牌 = **按名次挑选**：逐位置 `r = GetRandomInt(0, 剩余)` → 在「未用表」里数第 r 个空位；末位 `剩余 == 1` ⇒ 早退**不掷**。`stratagemList`（钉位）那一支我们不用 |
| `SupportMethods.ShuffleCardList` | `SupportMethods__ShuffleCardList.c:1-70` | `BattleManager.ShuffleDeck` 走它 → 调 `GetRandomOrder` 得排列再依次取 ⇒ 确认「洗牌 = 同一张表」 |
| `BoardAnalysis.MeetsCriteria`（随机档） | `BoardAnalysis__MeetsCriteria.c:55` + `:89` | `rand = GetRandomInt(0,100)` → `return rand < referenceValue`（原 `:65-99` 支） |

### 2.2 新实现

- **`RuleEngine/Core/BattleContextRandom.cs`（新文件，`partial class BattleContext`）**
  - `:34` `public readonly int[] RandomNumbers = new int[100];`（原版 `+0x20`）
  - `:37` `public int RandomNumbersIndex;`（原版 `+0x28`）
  - `:46` `void InitRandomNumbers()` — 100 格 `= Rng.Next(0,1000)`，游标清 0（🔶 见 §③-a）
  - `:62` `public int GetRandomInt(int minValueIncl, int maxValueExcl)` — 三条边界逐句照抄
- **`BattleContext.cs`**
  - `:317` 类声明加 `partial`（零行数变化）
  - `:681` 构造器**同一条语句**里追加 `InitRandomNumbers();`（零行数变化）
  - `:378` `Rng` 的 doc 改口径（只当烧表种子流）；`:1453` 「退回 `ctx.Rng`」改口
- **`RuleCore.cs`**
  - `:1192` `Shuffle<T>(IList<T> list, BattleContext ctx)` → 委托类尾 `:14986 ShuffleByRandomOrder`（原版按名次挑选）
  - 转换点：`:915` `:7075` `:13403` `:13405`（`1 + Next(3)` → `GetRandomInt(1,4)`）`:13457` `:13503` `:14079`
- **`EffectResolver.cs`**（17 处）：`:1761` `:1864` `:2928` `:3407` `:3471` `:3476` `:3510` `:3928` `:4374`
  `:4420` `:4435` `:4451` `:5952`（洗牌 → `ShuffleByRandomOrder`）`:12207` `:13121` `:15202` `:15329`
  ＋ `:13016` `InsertIntoDeckRandom`（**把独立的 `QuestAux.InsertRng` 并回共用表**，原版走同一个 `BattleManager.GetRandomInt`）
- **`PlayedThirdCardInTurn.cs`**：`:193`（随机目标截断，洗牌 → `ShuffleByRandomOrder`）`:208`（`RandomizedOnResolution(90)` / `DamageType.Random` 的伤害掷点 → `GetRandomInt(min, max+1)`）
- **`CountEval.cs`**：`:318` **补上原版随机档掷法**（`GetRandomInt(0,100) < referenceValue`；旧版是「未实现 ⇒ 返回 null 并出声」）

### 2.3 三族逐条核过（任务验收第 3 条）

| 族 | 原版入口 | 我们的落点 | 结论 |
|---|---|---|---|
| **洗牌** | `SupportMethods.GetRandomOrder` | `RuleCore.Shuffle`（`:1192`）→ `ShuffleByRandomOrder`（`:14986`）；10 个调用点（`RuleCore` 3 · `EffectResolver` 6 · `PlayedThirdCardInTurn` 1） | ✅ 算法从 Fisher–Yates 换成**按名次挑选**；随机源 = 预烧表 |
| **随机部署 / 随机挑牌** | `GetRandomInt(0, pool.Count)` 那一族 | `EffectResolver` `:1761/:1864/:3407/:3476/:3928/:4374/:4420/:4435/:4451/:12207/:13121/:15202/:15329` · `RuleCore` `:915/:7075/:13403/:13457/:13503` | ✅ 全部按下标取 |
| **`RandomizedOnResolution(90)`** | `ChooseValueOptions.RandomizedOnResolution = 90` | `PlayedThirdCardInTurn.RollDamage`（`:208`）；同族区间伤害 `EffectResolver` `:2928/:3471/:3510`、`RuleCore` `:13405` | ✅ 全部 `GetRandomInt(min, max+1)`（上界开区间，与原版一致） |

### 2.4 剩余的可执行 `Rng` 用法（全仓 `Core/` 现扫）

只有一处，且**必须**是 `Rng`：

```
BattleContextRandom.cs:48   RandomNumbers[i] = Rng.Next(0, 1000);   // 烧表
```

其余 `ctx.Rng` 命中**全是注释**（已按铁律 5 就地改口径：原写「走 ctx.Rng」的 12 处 ⇒ 改成 `ctx.GetRandomInt` 并注明 2026-10-27 前是 `ctx.Rng`）。

---

## ③ 没查清 / 没做（明确列出）

### ③-a `🔶 自己设计（非原版）`：**烧表的数源**
原版 `UnityEngine.Random.Range(0,1000)`（**全局、不可由种子复现**）；我们改用 `System.Random(seed)`（`Rng`）。
原因 = 工程红线「同一颗种子跑两次必须得到同一局」。**表/游标/取法三者照原版**，只换数源。
已在 `BattleContextRandom.cs:39-45` 打 `🔶 自己设计` 标记（调度台 2026-10-27 裁定）。

### ③-b **`AiRng` 仍是独立流**（原版 AI 与结算共用一张表）
`SimpleAI` / `AiScoring` / `AiTargets` / `BattleDriver.PickOffensiveForAi` 走 `ctx.AiRng`。
账里多行已把这条记成**有意偏离（回放契约）**、`BattleDriver.cs:9415-9420` 写着「⛔ 别改回去」⇒ **本批不动**。
⇒ 后手：若要彻底对齐，需要一张「AI 掷骰也走 `ctx.GetRandomInt`」的裁定 + 回放契约同步改。**卡点 = 需要调度台裁定**。

### ③-c **`ShowRng` 仍是独立流**（表现层抽「给你看哪 3 张」）
`BattleContext.ShowRng` 只在 `CardPresentation` 用；不进结算。本批不动（不在白名单）。

### ③-d **`QuestAux.InsertRng` / `QuestInsertSeed` / `QuestHashMix` 已成死代码**
为**零行数变化**（`EffectResolver.cs` 有 44 条账落点在 13016 之后）保留在原地并标注「已停用」。
⛔ 别接回去。下一次**允许动行号**（`--fix` 前后）时可以安全删除 —— 需要调度台在同步点做。

### ③-e **`CountEval` 的 `targetSelectMethod = random(0)` 选择链仍未建模**
随机源已经就位，但缺的是**这条选择链自己的逐句判据**（`:976` 已把过期的「随机源尚未落地」改口）。
⇒ 不是随机源问题，另开账。

### ③-f `Data/DeckBuilder.cs:423` 的 `Shuffle(IList<T>, Random)` 没动
那是**开局凑电脑牌组**（不是对局结算），且用的是调用方传入的 `Random`。判断：不属于 P-04 范围。
⚠️ 若调度台认为「凑牌也算原版随机源」，需要另开一条（原版凑牌判据我**没查**）。

### ③-g 探针看不见的量
`PlayProbe.Capture` 不记 `Exhausted` / `AttacksThisTurn` ⇒ 若某张卡只在这两个量上变化，**`play check` 结构上看不见**。
本批 176 行的变化**没有**依赖这两个量（逐行看过差异列，都是 dmg / 手牌 / 牌库 / 能量 / 机制位）。

---

## ④ 改动清单

| 文件 | 行数变化（`git diff --numstat`） | 说明 |
|---|---|---|
| `RuleEngine/Core/BattleContextRandom.cs` | **新增文件**（+`.meta`） | 预烧表 + 游标 + `GetRandomInt`（`partial class`） |
| `RuleEngine/Core/BattleContext.cs` | `4 / 4` | 类加 `partial`；构造器同语句调 `InitRandomNumbers()`；两处 doc 改口径 |
| `RuleEngine/Core/RuleCore.cs` | `64 / 26`（净 +38 = **类尾**） | `Shuffle` 换算法（9 行原地替换，零漂移）＋ 7 处调用换源 ＋ 类尾新增 `ShuffleByRandomOrder` |
| `RuleEngine/Core/EffectResolver.cs` | `53 / 53` | 17 处换源 ＋ 6 处 `Shuffle` 参数换 ctx ＋ `InsertIntoDeckRandom` 并回共用表 ＋ QuestAux 停用标注 |
| `RuleEngine/Core/PlayedThirdCardInTurn.cs` | `7 / 7` | 随机目标截断换 `ShuffleByRandomOrder`；伤害掷点换 `GetRandomInt` |
| `RuleEngine/Core/CountEval.cs` | `11 / 11` | **补上**随机档掷法；三处过期 doc 改口 |

**行号漂移 = 0**（除 `RuleCore.cs` 类尾 +38）。目标：不惊动账的落点。

### 顺手发现（由调度台分流）

1. **`BattleContext.cs` 里 `BattleContext` 类在 `:2035` 就结束了，后面还有 `MatchTypes` 等其它类**
   （`:2081`）。我第一次把新成员追加到「文件尾」⇒ 落进了 `MatchTypes`（静态类 ⇒ `CS0708`）。
   ⇒ 改法 = **新开 `partial` 文件**。**以后谁要往 `BattleContext` 加成员，⛔ 别追加到文件尾。**
2. `EffectResolver.cs:13015` 的 `var aux = QuestAuxOf(ctx);` 现在只用于「一次性提示」记账；
   若日后把 `QuestInsertSeed` 那 20 行删掉，`aux` 那个局部仍需保留（`:13040` 另一处也在用同一个表）。
3. `play check` 的探针历史注释（`PlayProbe.cs:3320-3400`）里有一笔 **DA44 的旧归因**写着
   「派生 RNG 流（`QuestInsertSeed`/`QuestAux.InsertRng`）⇒ filler 序号位移」—— 本批之后
   **那条流已不存在**，重钉时该段说明要跟着改口。
4. `CountEval` 的随机档**今天零影响**（现读全库没有卡的 criteria 用到 `triggerIf = 8`）⇒ 指纹没动（加了它前后指纹都是 `b671acdb…`）。
5. **两处自检的说明文字已过期**（⛔ 我没改，文件不在白名单）：
   - `Editor/RuleEngineTest.cs:20881` 断言文案「（派生随机流可复现；**没有拿 `ctx.Rng`** / 时钟）」——
     现在 `InsertIntoDeckRandom` **就是**走共用表（`ctx.GetRandomInt`）。**断言本身仍然成立**
     （同 seed + 同动作 ⇒ 同游标序列 ⇒ 同插入位），只是**理由要改口**。
   - `Editor/RuleEngineTest.cs:24468` 「换牌走的是 `ctx.Rng`」同理（换牌洗牌现在走 `GetRandomInt`）。
   - ⚠️ 按铁律 12「自检暂停」，本批**没有**跑 `RuleEngineTest.Run` / `_run_8_checks.sh`；
     若有断言把「某个随机抽到的那张卡」写死，会在恢复自检时红 —— 那是**基线过期**，不是回归。
6. `RuleEngineTest_G3.cs:414` 的 `b.Rng.Next()` / `RuleEngineTest.cs:38270` 的 `AiRng` 对比**不受影响**
   （`AiRng` 未动）；`ctx.Rng` 仍存在（只是不再产出结算随机）。

---

## ⑤ 基线重钉用的读数列（调度台用）

```
改前 PinnedPlayMd5 = 1b40f42ab010e623e908fcd4a236205f   （1714 行 = 主档 1128 + 电池 586）
改后 PinnedPlayMd5 = b671acdb0b60d86b17ffe3a9f9e69c25   （1714 行，新增 0 / 消失 0）
解析层（ruleprobe check）= 不变（仍 c9a64798… / 全池 1121 张 · 解析差异 0）
```

### 5.1 变化面（列维度）

| 段 | 变化张数 | 列（变化张数） |
|---|---|---|
| ①-a 主档 | **100** | `changesFull` 100 · `other` 26 · `changes` 25 · `handCostDelta` 21 · `dmg` 15 · `mech` 4 |
| ①-b 事件电池 | **76** | `changes` 72 · `energyDelta` 37 · `handCostDelta` 34 · `other` 27 · `mech` 25 · `expectedPay` 7 |

### 5.2 归因（逐族）

1. **随机伤害区间（`dmg` 15 张，主档）** —— `GetRandomInt(min,max+1)` 换了值（区间不变）：
   `3f38d2e2…` 4→2 · `760bd3af…` 0→1 · `AM53` 10→6 · `ASH49` 24→12 · `GOF41` 7→5 · `GOF44` 9→6 ·
   `GOF95` 4→3 · `SAU50` 1→0 · `SAU51` 12→8 · `SAU56` 3→2 · `TL56` 3→1 · `a84a5b49…` 5→3 ·
   `b4d6d186…` 2→1 · `cf0b346f…` 3→1 · `d3e99cb1…` 6→3。
2. **随机挑牌（造/抽/选「a random X」）** —— 同池不同张：如 `4959cd9e…` `+GSC43`→`+GSC53` ·
   `40667e34…` `+ef596eda…`→`+b14d1db2…` · `AM62` `+AM33`→`+AM18`。
3. **随机抽牌 / 随机回填位**（`changesFull` 里出现 `filler` 的 **21 张**）：
   如 `09bcb1b3…` `+filler24`→`+filler27`；同族含 `RuleCore:14079`（墓地随机回填位）与
   `EffectResolver:13016`（随机回库位）—— 两处都从独立流并回了共用表。
4. **级联聚合**（`handCostDelta` 21 · `energyDelta` 37 · `mech` 4/25 · `other` 26/27 ·
   `expectedPay` 7）—— 都是上面 1~3 的下游：手里的**具体哪几张**变了 ⇒ 手牌总价 / 能量 / 机制位跟着变。
   `expectedPay` = `RuleCore.CostOf(ctx,0,inst)`（`PlayProbe.cs:1098`）⇒ 会随手里其它牌变化。

### 5.3 完整差异卡清单

**①-a 主档 100 张**（`id(类型:变化列)`）：

```
09bcb1b3b8794c4429d82d4b2e68fa1f(tactic:changesFull), 3f38d2e2c83df4b4c9052ef4a510cfbb(tactic:dmg+changesFull), 40667e346ae58a84dbda1033dff70ccb(tactic:other+handCostDelta+changes+changesFull), 4959cd9e99597f74ca9af0bd65ae6307(tactic:changesFull), 4d348cff71208c24d8e39252d913ff9d(tactic:changesFull), 65b2be828179515458a282cf1b027182(tactic:other+handCostDelta+changes+changesFull), 6ed51d59b9f8ea24ab0dc7ceaef7956c(tactic:changesFull), 760bd3afde513774d96d6492f995fd37(tactic:other+dmg+changes+changesFull), 7a01882fe4610244292d16b15f4b873e(tactic:changesFull), 8482077f309b08943b190f18b5874871(tactic:changesFull), 851f73daf747eb642934a16429b391db(tactic:changesFull), 97e959444b53f424ea4285e2ca02c7e6(tactic:changesFull), 9f75a6fe11922eb409ffbad4ecdeb405(tactic:changesFull), AM44(tactic:other+changes+changesFull), AM52(tactic:other+handCostDelta+changes+changesFull), AM53(tactic:dmg+changesFull), AM61(defence:changesFull), AM62(defence:handCostDelta+changesFull), ASH39(tactic:changesFull), ASH41(tactic:changesFull), ASH44(tactic:handCostDelta+changesFull), ASH46(tactic:changesFull), ASH49(tactic:other+dmg+changes+changesFull), ASH57(defence:handCostDelta+changesFull), ASH71(tactic:mech+other+handCostDelta+changes+changesFull), ASH80(unit:changesFull), ASH82(unit:changesFull), BL36(hero:other+changes+changesFull), BL46(tactic:changesFull), BL53(tactic:changesFull), BL55(tactic:changesFull), BL56(defence:handCostDelta+changesFull), BL58(defence:changesFull), DA38(unit:changesFull), DA39(tactic:changesFull), DA42(tactic:changesFull), DA44(tactic:changesFull), DA46(tactic:other+changes+changesFull), DA51(tactic:changesFull), DA54(tactic:changesFull), DA60(defence:changesFull), EC10(unit:changesFull), EC44(tactic:changesFull), EC45(tactic:other+changes+changesFull), EC48(tactic:changesFull), EC52(tactic:changesFull), EC59(defence:changesFull), EC61(defence:changesFull), GOF107(tactic:handCostDelta+changesFull), GOF31(tactic:changesFull), GOF34(tactic:changesFull), GOF40(tactic:changesFull), GOF41(tactic:other+dmg+changes+changesFull), GOF44(tactic:dmg+changesFull), GOF48(tactic:other+changes+changesFull), GOF72(tactic:changesFull), GOF93(unit:changesFull), GOF95(unit:dmg+changesFull), GSC29(unit:changesFull), GSC52(tactic:changesFull), GSC64(defence:other+handCostDelta+changes+changesFull), GSC74(tactic:changesFull), SAU40(tactic:other+changes+changesFull), SAU45(tactic:other+handCostDelta+changes+changesFull), SAU50(tactic:other+dmg+changes+changesFull), SAU51(tactic:dmg+changesFull), SAU56(tactic:dmg+changesFull), SAU74(tactic:changesFull), SOR48(tactic:changesFull), SOR58(tactic:changesFull), SW10(unit:changesFull), SW18(unit:mech+other+handCostDelta+changesFull), SW29(unit:changesFull), SW43(tactic:changesFull), SW48(tactic:mech+other+handCostDelta+changes+changesFull), SW51(tactic:other+changes+changesFull), SW57(tactic:changesFull), TAU48(tactic:changesFull), TAU56(tactic:changesFull), TL20(unit:changesFull), TL49(tactic:changesFull), TL53(tactic:handCostDelta+changesFull), TL56(defence:dmg+changesFull), TL72(tactic:changesFull), UM114(tactic:handCostDelta+changesFull), UM118(tactic:changesFull), UM63(tactic:changesFull), UM75(tactic:handCostDelta+changesFull), a51adc8f5c311584385c3117cfd919e9(tactic:changesFull), a84a5b49f0ca9144585058db6987c2a0(tactic:dmg+changesFull), af98d423c7279a14c84c0d0382c53541(tactic:other+handCostDelta+changes+changesFull), b4d6d186eb95a3f468013c5be649ee3b(tactic:dmg+changesFull), c0ddba6cc45944f46a65f4fd93bcc0a5(tactic:changesFull), c4f5c920744030c4e84428dc5a0f1bf2(tactic:other+handCostDelta+changes+changesFull), cc1400347b15ee3449bc18bbdb9ddec9(tactic:other+changes+changesFull), cf0b346f70e748c4bbfc4dc03ab39ead(tactic:dmg+changesFull), d01a5ba5865ded4418194ea50e333b21(tactic:mech+other+handCostDelta+changes+changesFull), d3e99cb18b8cbba4081486f57f510f75(tactic:other+dmg+changes+changesFull), f2b80d575cbf8f64597b991022beaf35(tactic:other+handCostDelta+changes+changesFull), fd67b47572987b84cb2990ea1ecfe982(tactic:other+handCostDelta+changes+changesFull)
```

**①-b 事件电池 76 张**（前缀 `E|`）：

```
E|AM12(mech+other+energyDelta+handCostDelta+changes), E|AM22(other+energyDelta+handCostDelta+changes), E|AM23(handCostDelta+changes), E|AM30(mech+other+energyDelta+handCostDelta+changes), E|AM32(energyDelta+handCostDelta+changes), E|AM6(mech+energyDelta+handCostDelta+changes), E|AM67(handCostDelta+changes), E|AM69(changes), E|AM70(changes), E|ASH80(changes), E|ASH82(changes), E|BL15(handCostDelta+changes), E|BL23(changes), E|BL24(other+handCostDelta+changes), E|BL25(mech+other+energyDelta+handCostDelta+changes), E|BL30(mech+other+energyDelta+changes), E|BL32(energyDelta+changes), E|DA11(changes), E|DA16(changes), E|DA17(mech+energyDelta+changes), E|DA36(changes), E|DA38(changes), E|DA67(mech+energyDelta+changes), E|DA71(changes), E|EC10(changes), E|EC12(energyDelta+changes), E|EC17(changes), E|EC23(mech+other+changes), E|EC3(mech+other+energyDelta+changes), E|GOF12(other+energyDelta+handCostDelta+changes), E|GOF13(changes), E|GOF22(energyDelta+changes), E|GOF3(other+energyDelta+handCostDelta+changes), E|GOF58(other+expectedPay), E|GOF66(other+expectedPay), E|GOF83(other+energyDelta+handCostDelta+changes), E|GOF84(changes), E|GOF87(mech+other+energyDelta+handCostDelta+changes), E|GOF88(other+expectedPay), E|GOF9(mech+other+energyDelta+handCostDelta+changes), E|GOF90(mech+expectedPay+energyDelta+handCostDelta+changes), E|GOF91(other+expectedPay), E|GOF93(expectedPay+handCostDelta+changes), E|GOF95(mech+changes), E|GOF97(expectedPay+energyDelta+handCostDelta+changes), E|GSC25(mech+other+energyDelta+handCostDelta+changes), E|GSC27(changes), E|GSC29(changes), E|GSC34(handCostDelta+changes), E|GSC5(handCostDelta+changes), E|SAU31(handCostDelta+changes), E|SAU68(mech+energyDelta+changes), E|SOR29(handCostDelta+changes), E|SW10(other+energyDelta+handCostDelta+changes), E|SW18(energyDelta+changes), E|SW19(changes), E|SW21(mech+other+energyDelta+changes), E|SW23(other+energyDelta+handCostDelta+changes), E|SW29(mech+other+energyDelta+handCostDelta+changes), E|SW36(handCostDelta+changes), E|SW5(mech+other+energyDelta+changes), E|SW6(mech+other+energyDelta+handCostDelta+changes), E|TL14(mech+other+energyDelta+handCostDelta+changes), E|TL16(energyDelta+changes), E|TL18(handCostDelta+changes), E|TL20(mech+other+energyDelta+handCostDelta+changes), E|TL24(handCostDelta+changes), E|TL26(other+energyDelta+changes), E|TL30(energyDelta+handCostDelta+changes), E|TL6(handCostDelta+changes), E|UM28(mech+changes), E|UM56(mech+energyDelta+changes), E|UM59(energyDelta+changes), E|UM82(mech+energyDelta+changes), E|UM90(changes), E|UM99(mech+changes)
```

> 复现方式：`bash 工具/ruleprobe.sh play all pf /tmp/x.txt` 后与该文件逐行 diff（19 列）；
> 屏幕上的 `play check` 只列前 60 张，完整清单以本表为准。
