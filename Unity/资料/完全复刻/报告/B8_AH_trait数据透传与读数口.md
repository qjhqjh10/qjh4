# B8_AH · `trait / keyword` 链的四个前置（数据透传 + 读数口）

> 执行代理 **B8_AH**（批次 8）· 2026-10-26
> 白名单：`RuleEngine/Data/CardDatabase.cs` · `RuleEngine/Core/ConditionEval.cs` · `工具/ruleprobe/**` ·
> `工具/gen_cards_engine.py` · 账补丁 `账/_parts/B8_AH.tsv` · 本报告。
> **验证**：`typecheck` **0 / 0** · `ruleprobe dbparse` rc=0 · `ruleprobe traits` 系列 rc=0 ·
> `verify_annot` **缺注解 0** · `check_a_vs_diff`（主表 29 条弱候选，**没有一条**是我这 6 行的）·
> 补丁自查（同规则）**弱候选 0 · 缺注解 0** · 合并 dry-run（`--only B8_AH`）**更新 6 / 新增 0** ·
> `check_patch_conflicts` **无跨补丁重复键** · 合并副本上 `check_ledger_points` 我这 6 行**全部 `OK`**。

---

## ① 结论

| # | 任务 | 做到什么 |
|---|---|---|
| **R1** | `cardTraits` 透传 | ✅ **已接**。`CardDatabase.Parse` 现在每张卡构造后调 `def.AttachCardTraits(c.cardTraits)`（`CardDatabase.cs:1610`）＋ `def.AttachKeywordId(c.keyword)`（`:1611`）。⛔ **没有动 `CardDef` 的构造函数**（B8_Q 的类尾挂载口就是为此建的，`CardDef.cs` 40 处账落点零位移）。实测 **`CardTraits` 已挂 1128/1128 张 · `keyword` 已挂 1128/1128 张**（`traits` 横幅）。<br>⇒ **trait 家族 5 条 C 行全部升 A**（判据/观测量见 §②、补丁 6 行）。 |
| **R2** | 生成器落 `keyword` 列 | ✅ **已落**。`gen_cards_engine.py` 主循环 + 隐藏附魔那一支各加 `"keyword": truth_int(…, "keyword", -1)`；**卡表 version 30 → 31**；加了一条**完整性守卫**（「真值里有这一行却落 -1」⇒ `sys.exit`，不落盘）。<br>产物：`cards_engine.json` **version=31 · 1128 张 100% 有 `keyword`**，分布 `0×539 · 5×399 · 10×135 · 15×18 · 25×8 · 30×7 · 35×15 · 40×7`（= 1,128）。<br>🔴 **除 `keyword` 与 `version` 外，全表与 HEAD 逐列相同**（脚本逐卡逐列比对：`differing columns vs HEAD = {}`）。 |
| **R3** | `CardDef` 侧接线 | ✅ `CardDto.keyword`（缺值 **`-1` = 未知**，`CardDatabase.cs:590-600`）＋ `< 31` version 守卫（`:1358-1363`，⛔ 不许把旧表读成 `none(0)`）＋ `AttachKeywordId`（`:1611`）。 |
| **R4** | 条件 275/276/277 走 `IsKeyword` | ✅ **已改**（`ConditionEval.cs:428 / :434 / :440` → `KeywordOrSubtype(card, 5/10/25/35, "…")`；兜底与出声在 `:641-665`）。⛔ 没有删 `Subtype` 那一档 —— 它是「`keyword` 列没喂」时的**退回**口径并**出声**。**池内受影响卡 = 4 张**，实测差集 = 4 张（见 §②.3）。 |
| **R6** | `ruleprobe traits` 只读模式 | ✅ **已建**（`工具/ruleprobe/TraitsProbe.cs`，244 行；`Program.cs` 挂 `traits`）。四种用法：`traits`（全池一行一卡）· `traits <id|卡名>`（单卡详表 + 275/276/277 的**产品代码**返回值）· `traits q=<trait,…>`（`HasDefaultTrait`/`GetDefaultTraitValue` 汇总）· `traits cond=<n,…>`（跑 `ConditionEval.MeetsCard`，列候选集 + 与旧 `Subtype` 等价式的**差集**）。<br>🔴 它走**真实装载链** `CardDatabase.Parse`（与 `dbparse` 同一条路）⇒ 量得到「两列到底透传了没有」；⛔ 不用 `Program.PoolList`（那是探针自己的手读，看不到接线）。 |

---

## ② 逐条证据

### 2.1 判据（原版侧，全部现读）

| 判据 | 出处 |
|---|---|
| `RawCardScript.keyword` = `*(int*)(raw+0x98)` | `decomp_full/RawCardScript__IsKeyword.c:5-7`（全文 7 行） |
| `CheckIfTraitIsInList(list, trait)`：遍历 `list`、`item(+0x18) == trait` ⇒ 1；**`list == null` / 元素 null ⇒ `FUN_1803f47a0()`（不返回=抛）** | `decomp_full/RawCardScript__CheckIfTraitIsInList.c:10-32` |
| `FindTraitInList`：同一次线性扫，返回**命中元素**；`list == null` ⇒ 抛 | `decomp_full/RawCardScript__FindTraitInList.c:10-32` |
| `CountTraits()` = `*(list+0x18)`（= `List.Count`）；`+0x230 == null` ⇒ **0** | `decomp_full/RawCardScript__CountTraits.c:9-13` |
| 条件 275 / 276 / 277 | `decomp_full/FilterMethods__MeetsConditionCriteria.c:183-190 / :191-203 / :204-216`（`IsKeyword` 直调 `RawCardScript__IsKeyword`） |
| `Keyword` 枚举 9 档（无 `Warlord`） | `il2cpp_out/dump.cs:45899-45912` |

### 2.2 我们侧落点（行号范围）

| 我们 | 行号 |
|---|---|
| `CardDto.keyword`（`-1` = 未知） | `RuleEngine/Data/CardDatabase.cs:590-600` |
| `< 31` version 守卫 | `RuleEngine/Data/CardDatabase.cs:1358-1363` |
| 构造后挂载（两个） | `RuleEngine/Data/CardDatabase.cs:1590-1612`（`def.AttachCardTraits` `:1610` · `def.AttachKeywordId` `:1611` · `list.Add(def)` `:1612`） |
| trait 家族四个查询 / `CardTraitsKnown` / `CardTraits` | `RuleEngine/Core/CardDef.cs:6393-6475`（`CountTraits` `:6430-6435` · `HasDefaultTrait` `:6442-6451` · `GetDefaultTraitValue` `:6458-6462` · `FindDefaultTrait` `:6466-6475`） |
| `IsKeyword` / `AttachKeywordId` / `KeywordId` | `RuleEngine/Core/CardDef.cs:6519-6528` / `:6508` / `:6505` |
| 条件 275/276/277 的三个返回行 | `RuleEngine/Core/ConditionEval.cs:428` / `:434` / `:440`（**行号与改前完全一致**） |
| 兜底 `KeywordOrSubtype` + 出声 + 四个枚举常量 | `RuleEngine/Core/ConditionEval.cs:641-665`（类尾新增） |
| 生成器 `keyword` 列 + 完整性守卫 | `工具/gen_cards_engine.py`（主循环 `entry` / 隐藏附魔 `entry` / `build()` 末尾守卫 / `"version": 31`） |
| 读数口 | `工具/ruleprobe/TraitsProbe.cs`（新文件）· `工具/ruleprobe/Program.cs`（`traits` 分派）· `ruleprobe.csproj`（`<Compile Include="TraitsProbe.cs" />`） |

### 2.3 可观察量（G3）—— 两侧数值都是**现跑**的

全部命令都是 `bash d:/4/Unity/工具/ruleprobe.sh …`。

**① trait 家族（5 行）**

```text
traits  横幅：`CardTraits` 已挂 1128/1128 张 · `keyword` 已挂 1128/1128 张
traits ASH34（Avatar of Khaine）
  CountTraits = 1                    ← 原版 CardTraits=[talent] ⇒ 1；旧映射 kwKeys={armour:2,talent:1} ⇒ 2
  CardTraits  = [Talent]  name=talent value=0    ← 原版 _traitValue=0（没印数字）
  HasDefaultTrait(talent)  = 真 · GetDefaultTraitValue = 0   ← 旧 KwValue(talent)=1
  HasDefaultTrait(armour)  = 假                              ← 旧 Has("armour")=真（armour 是字段，不在 +0x230）
  ArmourFromPool / FromKeywords = 2 / 2                      ⇒ 护甲来自字段那一份，与 trait 清单无关
traits q=armour   ⇒ HasDefaultTrait(armour) = 真 5 张：BL27(1) GSC32(1) GSC28(1) GOF61(1) TL70(1)
                    原版侧（cards_truth.json 现扫）：_trait == 1020 的 = 同样这 5 张、_traitValue 全 1 ⇒ 逐张相同
                    旧映射 Has("armour") = 81 张（Armour N 关键词 81 张）⇒ 76 张假阳已消除
```

**② `keyword` / 条件 275-277（1 行）**

```text
traits BL1（Chaos Spawn，原版自身印刷与数据矛盾的那张）
  KeywordId(+0x98) = 40 Daemon
  Subtype(卡面橙字·显示用) = Infantry      ← ⛔ 不是 IsKeyword 的判据
  IsKeyword(5 Infantry) = 假 · IsKeyword(40 Daemon) = 真      ← 旧口径（Subtype）会答「Infantry=真」
  ConditionEval.MeetsCard(277) = 假                          ← 旧实现在这张上是「真」

traits cond=275,276,277
  cond 275：真 191 · 与旧 Subtype 等价式差集 0
  cond 276：真 143 · 差集 0
  cond 277：真 414 · 差集 4 张：BL1 真→假 · EC55 假→真 · EC56 假→真 · EC57 假→真
           （旧等价式 410 = 414 − 3 + 1）

traits d19e8b68…（Speshul Dose，用条件 277）
  CardTargetFallback：Targeted=1 · op=1 条
     op[0] `give` TargetCondition = cond=277 ex=False role=`cardtarget`   ⇒ 条件真的挂到了 op 上
traits 1bfe08…（Path of the Warrior，同条件但 Targeted=0）
  CardTargetFallback：Targeted=0 · op[0] TargetCondition = null           ⇒ 闸生效（正文根本不选目标）
```

### 2.4 结算层控制（证明这批改动**没有**改掉结算）

- `play check` 在我**引擎侧改动全部在位之后**（07:40:01）读数 = **`350c16b2761e9a662b1f29ebaf69500e`**（2 张 / 5 张差异），
  与我开工前的读数（07:33:51，`350c16b2…`，同样 2/5）**逐字节相同**。
- 原因（结构性的，不是巧合）：`play` 的夹具单位是 `new CardDef(...)` 现造的（**不带 `keyword`**）⇒
  条件 275/276/277 走 `Subtype` **退回支**（`ConditionEval.cs:650-665`）⇒ 与改前同判。
- ⚠️ **07:45 再跑变成 `e11aa1c263e933dbdd67718133c8e5b2`（7 张 / 6 张）** —— 那是**别人的**改动：
  `EffectResolver.cs`(07:42:27) · `EffectText.cs`(07:44:34) · `CreatePool.cs`(07:44:43) 在我最后一次 play 读数之后被改过；
  变化的卡 `1688c614…` / `GOF105` / `GOF97` / `SOR60` 全是**随机召唤/群体加值**那几张，与 trait/keyword 无关。
  ⇒ **新指纹请调度台在同步点统一重算**（见 §⑥）。

---

## ③ 改了哪些文件哪些行（新增行数）

| 文件 | 改动 | 行数 |
|---|---|---|
| `RuleEngine/Data/CardDatabase.cs` | ① `CardDto.keyword`（**类尾**追加，`:590-600`）② `< 31` version 守卫（`:1358-1363`）③ `list.Add(new CardDef(…))` → `var def = new CardDef(…)` + **构造后挂载 3 行**（`:1590-1612`，含判据注释） | **3200 → 3253（+53）** |
| `RuleEngine/Core/ConditionEval.cs` | ① 三格返回行**就地改写**（`:428/:434/:440`，行号不动）② 类尾新增 `KeywordOrSubtype` / `NoteKeywordFallback` / 四个 `Kw*` 常量 / 判据块注释（`:630-665`） | **623 → 669（+46，全部在最后一个账落点 `:543` 之后 ⇒ 零位移）** |
| `工具/gen_cards_engine.py` | `keyword` 列（主循环 + 隐藏附魔）· 完整性守卫 + 分布打印 · `"version": 31` | **6882 → 6923（+41）** |
| `RuleEngine/Resources/cards_engine.json` | **生成器产物**（非手改）：version 30→31，每卡多 `keyword` 一格 | +1 列 × 1128 |
| `工具/ruleprobe/TraitsProbe.cs` | **新文件**（`traits` 读数口） | 244（新） |
| `工具/ruleprobe/Program.cs` | `traits` 分派 + 用法串 + 注释 | +17 |
| `工具/ruleprobe/PlayProbe.cs` | `preset sub=<卡id\|卡名>`（换掉夹具 `FIX_A`）· `preset legal=1`（目标格改由 `RuleCore.IsLegalPlayTarget` 挑 + 打合法池 + 打 `op.TargetCondition` 诊断）· 扣血跳过被换进来的真卡 | ≈ +60（全部在 `preset` 非空时生效，`play all` 走 `Measure(pool, c)` 不传 preset ⇒ 指纹不受影响） |
| `工具/ruleprobe/ruleprobe.csproj` | `<Compile Include="TraitsProbe.cs" />`（本 csproj 关了 `EnableDefaultCompileItems`，漏了就是构建期 `CS0103`） | +5 |
| `账/_parts/B8_AH.tsv` | 6 行（11 列 / LF / 无裸 CR）：trait 家族 5 行 **C→A** ＋ `IsKeyword` 1 行 **C→A** | 新建 |

---

## ④ 没查清的部分（如实）

1. **`CheckIfTraitIsInList` / `FindTraitInList` 的「外部表」那一档没有对等物**：原版形参可以是**任意** `List<CardTraitData>`（`ExtraTraits` 这类），
   我们只有「这张卡自己的清单」这一支。**全库 0 调用方**（B8_Q 已 grep 过；本轮没有再查一遍原版调用点）⇒ 今天零可观察差异，但**这条差异是真的**，已写进补丁的「差异」列。
2. **`CountTraits()` 在已恢复的原版方法体里 0 个调用方**（B8_Q 报告 §⑤-4）⇒ 它升 A 是「模型完整性」层面的对齐，**没有可观察行为**（观测量只能给数据两侧值）。
3. **`AttachCardTraits` 存的是**卡面显示名**（`Destroyer`/`Penitence`），不是真值枚举名（`frenzied`/`rage`）。
   我核过这是**非缺陷**（`CardDef.KeywordTable.Prefixes` 里 `new[] { "frenzied", Destroyer }` 显式登记了同一 trait 的两种拼法 ⇒
   `HasDefaultTrait("frenzied")` 与 `HasDefaultTrait("destroyer")` 都是真，实测 10 张 / 5 张逐张相同）。
   但**「靠别名表兜住」这件事本身没有断言钉住** —— 将来谁往 `Prefixes` 里删一条，trait 家族会**静默**答错。
4. **`play` 那条路看不到卡级 `cardtarget` 条件**（见 §⑤-1），所以 R4 的观测量只能走 `traits`（引擎装载链）+ 数据侧两侧值，
   **没有**「点一下看到效果落到谁身上」的实机读数。
5. **没跑任何 Unity 宿主/自检**（红线）⇒ `RuleEngineTest.Run` 里那些与卡表/关键词有关的断言（如「`cardTraits` 里不许出现 Frenzied/Rage」）**未验**。

---

## ⑤ 顺手发现（请调度台分流）

1. 🔴 **`ruleprobe` 的 `LoadPool` 没传 `targeted` ⇒ `play` 这条路结构上看不到卡级 `cardtarget` 条件**。
   病灶：`CardDef.Targeted` 默认 `-1`，而 `EffectText.AssignCardTargetFallback` 的闸是 `Targeted == 1`
   ⇒ 探针池里**所有卡**都不挂 `TargetCondition`（实测 `[目标条件] op[0] … TargetCondition = null`）。
   **这与 `A1911` / `A1959` 是同一族**（探针与引擎口径不同 ⇒ 拿探针量会静默得出「原版没有这一格」）。
   修法 = 在 `Program.LoadPool` 的 `new CardDef(...)` 里补 `targeted:`（`CardDatabase.cs` 早就在传了）。
   ⚠️ **代价**：挂上之后 4 张卡的 op 多一格 ⇒ `scan` 的解析输出与 `play all` 的候选池会变
   ⇒ **`check` 的解析差异会 > 1 行、`play check` 指纹会再变、三个 md5 要重钉**。
   ⇒ 我**没有擅自动**（基线重钉是调度台的活）。需要的话给一个「无 `.cs` 写手」的窗口，改一行 + 重生成基线。
2. ⚠️ **`CardDef.cs` 里两处 doc/提示串已过期**（不在我白名单，请派给 `CardDef` 的写手）：
   - `CardDef.cs:6477-6479` 的 `CardTraitsWhy` 仍写「`CardDatabase` 的 `new CardDef(...)` **还没传** `cardTraits:`」——
     现在**已经接了**（`CardDatabase.cs:1610`）；这句话会误导下一个会话。
   - 类尾那段「接线不属于本代理白名单」的说明（`CardDef.cs:6364-6370` 附近）同上。
3. ⚠️ **两处账的 `CardDatabase.cs` 行号因我这次 +53 行而漂**（都不是 A 档落点，机检扫不到）：
   - `战斗逻辑驱动.tsv:1465`（`SupportMethods.GetCardListFromIds`，B 档）落点写 `CardDatabase.cs:3154` ⇒ 现读 `DeckLookup` 在 **`:3207`**。
   - `战斗逻辑驱动.tsv:1440`（`ShouldShowSpecialMiniDescCounter`，A 档）**判据要点里的散文引用** `CardDatabase.cs:2449` ⇒ 现读对应行是 **`:2502`**（A 档机检只看落点列，所以没报）。
4. 📌 **`CardTraits` 的 5 张 `armour` 与 `armour > 0` 的 81 张**这一对数字本轮**双向复核过**：
   真值 `_trait == 1020` = 5 张（`BL27/GOF61/GSC28/GSC32/TL70`，值全 1），
   我们 `traits q=armour` = **同样 5 张 / 同值** ⇒ R1 的数据透传**没有丢也没有多**。
5. 📌 **生成器的 `keyword` 分布可与真值独立对账**：池内 `0×539 · 5×399 · 10×135 · 15×18 · 25×8 · 30×7 · 35×15 · 40×7`
   （1,128 张）；真值 1,351 张 = `0×738 · 5×416 · 10×137 · 15×21 · 35×17 · 25×8 · 40×7 · 30×7` ——
   差额全部落在「真值有、池里没有」的 223 张上（池 ⊂ 真值），**没有一张池内卡拿到 -1**。

---

## ⑥ 给调度台的请求

1. **新指纹（请在同步点统一重算/重钉）**：
   - `play check`：本代理自己的净影响 = **0**（证据见 §2.4：07:40:01 与 07:33:51 同为 `350c16b2…`）；
     现在盘上是 **`e11aa1c2…`（7 张 / 6 张）**，来自**别人** 07:42–07:44 改的 `EffectResolver/EffectText/CreatePool`。
   - `check`：**`解析差异 1 行`** = `f8260229254a09746af6d9ff7a9b0b54|Ethereal Supreme`。
     🔴 **不是我造成的**：用**旧卡池**（HEAD 那份、`version=30`、没有 `keyword` 列）跑同一条命令，**同样 1 行** ⇒
     来自别人 07:35:49 改的 `EffectText.cs`。哨兵⑤「卡池 md5 不同 ⇒ 此时不判」（现值 `e60982b7…`）＋⑦ 输出指纹红是同一件事。
   - `CardDatabase.cs` 的卡池 md5 变了（我重生成过卡表）⇒ 三个 Pinned md5 都要重钉。
2. **哪些 C 能收口**：`账/_parts/B8_AH.tsv` 已把 **6 行 C → A**：
   `CheckIfTraitIsInList` · `CountTraits` · `FindTraitInList` · `GetDefaultTraitValue` · `HasDefaultTrait` · `IsKeyword`。
   合并校验（dry-run）：**更新 6 / 新增 0**；`check_patch_conflicts` 无重复键；合并副本上 `check_ledger_points` 这 6 行**全部 OK**。
3. **建议跑的自检**（铁律 12 三条判据：动了引擎 `RuleEngine/` + 卡表 ⇒ 至少这两条）：
   - **`RuleEngineTest.Run`**（卡表/关键词/`cardTraits` 那一批断言；⚠️ 新增了 `keyword` 列 + version 31，值得先跑它）
   - **`BattleScene.Run`**（条件 275/276/277 的消费面在战斗侧；我这边只能给离线读数）
   - ⛔ 不需要 `Net*` / `Shell*` / `Shop*` 等宿主（这批没碰它们）。
4. **`CardDef.cs` 的两处过期 doc**（§⑤-2）请派给 `CardDef` 写手；**`LoadPool` 补 `targeted`**（§⑤-1）请单列一个「需重钉基线」的窗口。
