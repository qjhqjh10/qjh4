# E2 · `A1462`（`non <关键词>` 目标筛选）＋ `A1572`（真打夹具两族过滤口径）

> 写手 **E2**（白名单 = `工具/ruleprobe/**` ＋ 本文件）· 2026-10-11 · 第二十会话。
> ⛔ **没碰** `RuleEngine/**` · `CardPresentation/**` · `数据/**` · 两张正本 · `d:/2/**`（只读）· **没跑 git**。
> 跑过的命令：`ruleprobe.sh check / nonkw / play all / play <id> / seg`（**不跑 Unity**，全走 `dotnet` 离线探针）。

---

## 一、`A1462` 的探针读数（丢没丢 / 丢在哪 / 判据）

### 结论：**丢了 —— 2 处，丢在【解析层】**

| 卡 | op 栏 | 卡面写的 | 解析出来的规格 | 承载处 |
|---|---|---|---|---|
| `GOF85` Unleashed TramplaSquig | `AtTurnOps.Target2`（`forceattack`） | `non Flying` | `Raw=a random non flying enemy · Side=enemy · Kind=any · Count=1 · Random=1` | **`NotKeyword=` 空** |
| `SW12` Hunting Wolf | `Target2`（`forceattack`） | `non Flying` | `Raw=a random non flying enemy troop · Side=enemy · Kind=troop · Count=1 · Random=1` | **`NotKeyword=` 空** |

命令：`bash d:/4/Unity/工具/ruleprobe.sh nonkw`（四节读数；有「没承载」的 ⇒ `rc=1`）。

### 判据（三层，全部是**外部**的，⛔ 不拿我们自己的实现反填）

1. **卡面**（铁律 7 · 本轮**亲读**成品卡图，不是转述）：
   · `d:/2/Warpforge部队卡片/Orks/3部队/Warpforge_11_Unleashed-Tramplasquig.png`
     = `〔Stomp〕 Stomp. At the start of each turn attack a random non 〔Flying〕 Flying enemy`
   · `d:/2/Warpforge部队卡片/Space Wolves/3部队/Warpforge_12_Hunting-Wolf.png`
     = `〔Ferocity〕 Ferocity: Attack a random non 〔Flying〕 Flying enemy troop`
   ⇒ `non Flying` 是**排除条件**（打谁要排除飞行单位）。
2. **原版**（`d:/2/tools/decomp_full/`）：`TargetCriteria.cs` 有
   `public DefinedTrait traitsFilter;` ＋ `[Tooltip("False => Filter units with Trait; True => without trait")] public bool excludeTrait;`；
   消费点 `FilterMethods__MeetsTraitCriteria.c`：`bVar2 = (param_3 ^ 1) & bVar2;`（`param_3` = `excludeTrait`）
   ⇒ **取反是原版真的会生效的那一位**（原版里 `Flying` 是 trait，`RuleCore.cs:3310` 引 `DefinedTrait.flying = 390`）。
3. **我们这一侧**承载「排除」的**唯一**字段 = `EffectTargetSpec.NotKeyword`
   （消费点 `RuleCore.ApplyTargetFilters`，`EffectResolver.cs:1356-1361` 的 `RemoveAll(u => … u.Has(NotKeyword))`）。

### 丢在哪一层（指名）

`EffectText.ParseTarget`（`RuleEngine/Core/EffectText.cs:7608+`）**没有** `non <词>` 这一支 ——
它只认 `with <词>`（`ReTargetWith` → `KeywordFilter`，`EffectText.cs:7771` 一带）+ `highest/lowest` / `adjacent` / `random`。
⇒ `non flying` 两个词**整段丢掉**：`Raw` 留着原文（`Raw` 只是排查用字符串、**不参与筛选**），
`Side`/`Kind`/`Count` 照常解出来 ⇒ **句子报 OK、`partial` 列空、卡面不打 `*`**（完全静默）。

⚠️ **`non-` 在别处【有】实现**（⛔ 别把它读成「全仓没有」）：**卡筛选**那一路 `CreatePool.FilterChoose`
认死两个前缀 `non-legendary` / `non-ephemeral`（`CreatePool.cs:303/305`）—— 同池那 4 处
`Choose a non-Legendary …`（`DA77`/`GSC4`/`UM_Suppressor`/`UM97`）走的就是它（`B19Test` 有断言）。
**缺的只有「目标短语」这一路。**

### 后果（`nonkw` §③ · 两条测量）

* **(a) 判定层**：同一个规格走引擎自己的筛（`RuleCore.ResolveTargets`；枚举池子时把 `Count` 临时置 0，**只动这一个字段**）。
  摆一个「带 `flying` 的单位 ＋ 不带 `flying` 的单位」⇒
  · 解析出来的规格 ⇒ 候选 = `[FIX_FLYING, FIXWarlord_P1, FIX_PLAIN]` ← 🔴 **含飞行单位**
  · 只把 `NotKeyword` 补成 `"flying"`（探针侧改副本）⇒ 候选 = `[FIXWarlord_P1, FIX_PLAIN]`（`SW12` 那条 `Kind=troop` ⇒ `[FIX_PLAIN]`）
  ⇒ **结算层是好的**，⛔ 别去改 `ApplyTargetFilters`。
* **(b) 真路径**（`GOF85` 走 `EndTurn`→`BeginTurn` 的真实 `turn_start` 触发，8 次抽样）：
  **1/8 空过** —— 随机挑中了飞行单位（`Count=1` ＋ `Random` ⇒ `TryAttackOnce` 只拿到**被挑中的那一个**候选；
  近战打不到飞行、**没有别的候选可退**），而场上**明明有**可打的非飞行敌人。
  引擎自己的话：`打不了 —— 目标不合法（Vanguard / Stealth / Flying 限制）`。

### 探针侧补的两条缝（`A1462` 判不了的两个直接原因）

1. **dump 不打 `Target2`** —— `forceattack` 那一族「打谁」在 `Target2` 里（`Target` 是攻击者、可为空）
   ⇒ `SW12` 在 dump 里**看起来**是 `T={T:null}`。已补 `/{T2:…}` 块（`Program.TargetBlock`）。
2. **`atturn` 的目标在嵌套 op 里**（`AtTurnOps`）—— 顶层 `Target`/`Target2` **都是空**。已补 `NestedText`
   （打在 `seg`/`card` 两个入口；`scan` 不打嵌套 —— 怕基线噪声）。

⛔ **本探针不改引擎**。修法（**解析层**，属 `E1` 的 `Core/`）：在 `ReTargetWith` 那一段**之前**加一支认
`non <词>` ⇒ 写进 `spec.NotKeyword`。**`check` 的哨兵 ⑥ 现在就钉着这一条（红、计入 rc），修好会自己变绿。**

---

## 二、`A1572` 的两条过滤口径（真打夹具「结构上不适用」的两族）

> 账文原写「这两族的『零变化』**必须从候选里排除**」。
> 实测**那句话与本夹具的实际形状不符**（见下面的读数）⇒ 现口径 = **标注 ＋ 单列 ＋ 逐条给原因**，
> ⛔ **不整族删除**（删除会把 3 张**真缺陷**一起盖掉：本仓红线「不许静默失败」＋ 铁律 11）。

| 口径 | **可执行谓词** | 为什么「结构上不适用」（引擎侧判据） |
|---|---|---|
| **① 临时卡** | `CardDef.Has("ephemeral")` | `RuleCore.EndTurn` 调 `SweepEphemeral(ctx, ctx.Active)`（`RuleCore.cs:1341` / 实现 `:1601`）：回合末把**该方手牌**里的临时卡**移出游戏**（规则书 `:229`「**非弃置**」）⇒ 凡「先摆牌库、等它自然上手」的夹具，跨一个来回之后它**必然不在手里** |
| **② 防御卡** | `CardDef.Type == "defence"` | `RuleCore.BuildPlayer`（`RuleCore.cs:506` 分出 / `:553` 交回）把 `defence` 从**牌库**里**分流**（`defenceCardOut`），发牌由调用方按**座位**做、**只发给后手**（`RuleCore.GrantDefenceCard` / `SetDefensiveCard`；`RuleCore.cs:448-473`）⇒ 本夹具 **P0 是先手**，手里永远没有它。⚠️ 补进来之后**座位与原版不同**（它被测量时站在先手这一侧） |

### 实测读数（池 1126 张 · 2026-10-11）

* 两族共 **137 张**（临时卡 **98** ＋ 防御卡 **39**）—— 与写手 `E8` 已写进 README 夹具表的口径一致。
* **137/137 都靠【按引擎形状补手牌】才测到**（`NewInstance`→`Hand.Add`→`SetupCardInHand`）。
* **口径自证**：两族的行与 `injected=1` 的行**互为同一集合**（137 = 137，**逐行核过**：
  `fixtureFit` 非空但 `injected≠1` 的 0 行、反之 0 行）。`play all` 的 ③ 节每次现算并打出来
  —— 哪天不再相等，说明补牌/分流的形状改了，要回来重读（⛔ 不是自动豁免）。
* **落进候选的只有 7 张**（3 临时 ＋ 4 防御），且**各有别的原因**：

| 卡 | 族 | 引擎自己那句话 | 性质 |
|---|---|---|---|
| `TAU27` Homing Beacon | ephemeral | `Deploy a Crisis Battlesuit` 部署不了 —— 按兵种「crisis」在卡池里一张都没筛到 | 🔴 **真缺陷**（`Crisis Battlesuit`=`TAU15` **在池里**） |
| `TAU67` Tidewall Droneport | defence | `Deploy a Guardian Drone` —— 按兵种「guardian」一张都没筛到 | 🔴 **真缺陷**（`Guardian Drone`=`TAU8` **在池里**） |
| `GSC68` Rusted Vents | defence | `Create a random troop with Ambush in your hand` 造不出牌 | 🔴 **真缺陷**（池里有 **11 张**带 `ambush`） |
| `SAU6` Master Chronomancer | ephemeral | 从牌库选了一张 → 放到牌库**顶** | ⚪ 量具盲区（牌库**顺序**不在指纹里） |
| `GOF60` Extractor Rig | defence | 「vehicle」里的手牌与牌库每张 -2 费，登记 6 张 | ⚪ 量具盲区（**牌库逐张费用**不在指纹里） |
| `SAU26` Reanimate · `SAU67` Resurrection Vault | ephemeral · defence | 「要翻残骸，但场上没有残骸、墓地里也没有单位」 | ⚪ 夹具没有残骸/墓地单位 |

⇒ **7 张里 3 张是真缺陷** —— 整族删除 = 静默盖掉它们。**可执行用法**（已进工具与 README）：
按 TSV 的 `fixtureFit` 列标注、`play all` 的 ③ 节逐条列出并附**引擎自己**那句最能解释「为什么没变化」的话。

---

## 三、改了什么（全部在 `工具/ruleprobe/**`）

| 文件 | 改动 |
|---|---|
| **`Program.cs`** | ① `TargetBlock` 补 `/{T2:…}`（`Target2` 非空才打）· ② 新增 `NestedText(op)`（`seg`/`card` 打 `AtTurnOps`/`RepeatOps`/`BaseOps`）· ③ 注册 `nonkw` 子命令（usage 也加）· ④ **哨兵 ⑥** `[non 排除]`（现为**红**、**计入 rc**、修好自绿）· ⑤ 重钉 `PinnedPoolMd5` / `PinnedBaselineMd5`（**有意**，理由写进源码） |
| **`NonKwProbe.cs`**（新，≈532 行） | `nonkw` 子命令：§① 全池普查（递归 op 树；`desc` ＋ `keywords` 原文）· §② 判据与承载字段 · §③ 行为对照（判定层 ＋ 真路径两跑法）· §④ 结论 · 共享 `Measure()` 供哨兵 ⑥ 用（**判据只写一处**） |
| **`PlayProbe.cs`** | `A1572`：`NotApplicable()` / `FitTag()` / `WhyLine()` ＋ `Reading.Injected`（补手牌标记）· TSV **末尾**加 `fixtureFit` / `injected` 两列 · 汇总新增 **③ 节**（两条口径 ＋ 单列落进候选的那 7 张）· 单卡读数打「夹具适用性」与「补手牌」提示 |
| **`README.md`** | ① dump 列新增「第二目标块」＋ `A1462` 为什么补（含**自证判据**）· ② 新增 `## nonkw` 整节 · ③ `play` 新增「本夹具【结构上不适用】的两族 —— 两条可执行的过滤口径（`A1572`）」整节（含 7 张逐条表、TSV 两列说明、两条 `grep` 示例）· ④ 夹具表「补手牌」那一行改指新节 · ⑤ 基线重建节补「改输出格式的三步」· ⑥ 哨兵表补 ⑥ 那一行 |
| **`ruleprobe.csproj`** | 加 `<Compile Include="NonKwProbe.cs" />`（本工程显式列文件） |
| **`基线/out_baseline.txt`** | **有意重生成**（格式改动）：**8 行**变化（`EC28`/`EC56`/`SAU71`/`GOF92`/`SW12`/`SW36`/`SW63`/`EC24`，全是 `forceattack` 一族），其余 **1112 行逐字节没变** |

### 自证（改输出格式专用的那条判据）

**把新行里的 `/{T2:…}` 块删掉之后必须与旧行【逐字节】相同** —— 实测 **8/8 成立**
（脚本 `re.sub(r'/\{T2:[^;]*', '', 新行) == 旧行`；`新增键 0 · 消失键 0`）。
⛔ 别把判据写成「旧行是新行的前缀」—— 块插在 op 分隔符 `;` **之前**，`;` 会往后挪。

### 现行读数（本轮收口）

```text
bash 工具/ruleprobe.sh check   →  全池 1120 张 · 解析差异 0 行 · 改名 0 · 新增 0 · 消失 0
                                  哨兵合计：失败 1 条（⑥ non 排除 = A1462 已知缺陷的钉子）
                                  ✓ 索引 ✓ 索引·scan 路 ✓ 货币名 ✓ 目标列 ✓ 基线指纹
                                  rc=1（**按 `A1462` 的账与设计**，⛔ 不是探针坏了）
bash 工具/ruleprobe.sh nonkw   →  ① 2 处、都没承载 ⇒ rc=1
bash 工具/ruleprobe.sh play all →  真打成功 1115 · 打不出去 11（全 ErrNoTargetAvailable）· 抛异常 0 ⇒ rc=0
                                  候选 410（A 41 · C 94 · B 275）· 两族 137 张全部 injected、落进候选 7 张
                                  ⚠️ 候选数**随池变**（首跑 411 A41/C96/B274，并行写手改过一次卡池后成了 410）——
                                     「7 张」「137 张」两项两次现算都一样
```

（`play all` 的 ③ 节现在会把那 7 张连同**引擎自己**的解释句逐条打出来 —— 本节第二部分的表就是它的逐字内容。）

---

## 四、没查清

1. **`non X` 到底该落哪个字段** —— 本轮判 `NotKeyword`（唯一承载「排除」的字段、消费点现成）。
   ⚠️ 但原版是 `traitsFilter` **＋** `keywordFilter` **两套**筛各有自己的取反位
   （`excludeTrait` / `excludeRace`）⇒ 「`non Flying` 在原版走的是 `excludeTrait`（flying 是 trait）、
   我们把它写进 `NotKeyword`（关键词表）」这层**表示法映射**是我按语义判的，**没逐条核过原版卡配的数据**。
   ⇒ 若 `E1` 要动解析层，这一点值得顺带核一次（只影响「写哪个字段」，不影响「必须承载」这个结论）。
2. **`A1462` 的完整影响面没盘** —— 今天池里目标短语带 `non X` 的只有 2 张（`GOF85`/`SW12`）；
   但**原版卡池**里同形写法还有没有别的（我们卡表只覆盖 1126 张）**没查**。
3. **顺手发现 #1（`turn_start` 次序）只量到 1 张卡**（`GOF85`）—— 同族「回合开始触发的攻击」还有没有别人
   （`AM33 Hydra` 的 `Regiment` 是攻击时触发，不同族）**没盘**。
4. **顺手发现 #2（`Deploy <二词具名>`）只盘了 4 张**（`TAU27`/`TAU67`/`GSC68`/`BL68`），
   全池同类写法的**总数与影响面没盘**（`ReDeploy`/`CreatePool.Resolve` 的兵种词判定那一支）。
5. **`SAU6` / `GOF60` 那两张量具盲区**（牌库顺序、牌库逐张费用不在指纹里）**没做** ——
   它们是**指纹**该不该扩的问题（改指纹会让全池读数重排），属量具设计，**交给调度台定**。

---

## 五、顺手发现（⛔ 都不是我这两笔的活，如实报；**只报不改**）

1. 🔴 **回合开始触发的强制攻击【永远打不出去】**（`nonkw` §③ 跑法①的副产品，**8/8**）：
   `BeginTurn` 里 `ResolveAtTurn("turn_start")` 在 **`RuleCore.cs:1110`**，而【解疲劳】
   `u.RefreshForNewTurn()` 在 **`:1161`** —— **触发排在解疲劳之前** ⇒ 被测单位此刻仍带着
   `Exhausted`/`CannotAttackThisTurn` ⇒ 引擎报 `ErrExhausted`（「该单位本回合已行动」）。
   **原版是反的**：`d:/2/tools/decomp_full/CardScript__OnTurnStart.c` 里
   `CardScript__ActivateMinion` 在 **`:99` / `:118`**，`RawCardScript__OnTrigger` 在 **`:137` / `:144` / `:151` / `:166` / `:265`**
   ⇒ **先解疲劳、后触发**。
   影响示例：`GOF85`（卡面 `At the start of each turn attack a random non Flying enemy`）**一次都打不出去**。
   ⚠️ 探针要量 `A1462` 只能在跑法②里**替它先调一次引擎自己的【解疲劳】**（已在读数里如实标明是量具干预）。
2. 🔴 **`Deploy <二词具名>` 被当兵种词筛 ⇒ 一条都筛不到**（同族有 4 处，本轮实测）：
   `TAU27`（`Crisis Battlesuit`=`TAU15` **在池里**）· `TAU67`（`Guardian Drone`=`TAU8` **在池里**）·
   `GSC68`（`troop with Ambush`，池里 **11 张**带 `ambush`）· `BL68`（`4 random troops from your deck`，README 已记）。
   症状是**静默偏一半**：句子解析得好好的（`payload=[crisis battlesuit]`），只有引擎日志里那句
   「按兵种「crisis」在卡池里一张都没筛到（**这条没生效**）」。
   ⚠️ 落点看 `RuleCore.CreatePool.Resolve` 的 `MatchKindWord` 分支（`CreatePool.cs:150+`）——
   **我只读到那一层、没往下追**（属 `Core/`，不在我白名单）。
3. ⚠️ **`SW16 Fenrisian Wolf` 的英文 `desc` 丢了一整句**（→ 这是 `A1462` 名单里第三张的同族，但**病更重**）：
   · 卡面（**亲读** `d:/2/Warpforge部队卡片/Space Wolves/3部队/Warpforge_16_Fenrisian-Wolf.png`）
     = `〔Tide 1〕 Tide 1. 〔Ferocity〕 Ferocity: Attack a non 〔Flying〕 Flying enemy troop. If it has a Hunt Mark, gain +1〔粉拳〕`
   · 源数据 `数据/游戏数据/card_stats.json`（同一张卡的 desc）= `Attack a non-Flying enemy troop. If it has a Hunt Mark, gain +1 [armor].`
   · 卡池 `cards_engine.json` 的 `desc` = **只剩** `If it has a Hunt Mark, gain +1 [Attack]`
   ⇒ **第一句（含 `non Flying`）在 `cardface_fixes.json` 的 `desc` 列被截掉了**（那是**英文 desc** 那一列，
   手工维护）。后果比 `A1462` 更重：`CardDef.CollectBareKeywordBody` 把**整条 desc** 当成 `Ferocity` 的正文
   ⇒ **`Fenrisian Wolf` 的 Ferocity 根本不会攻击**（只会在有猎杀标记时 +1）。它的 `descZh` 是**全的**
   （「攻击 1 个非飞行敌方部队。若其带猎杀标记，获得 +1 攻击。」）⇒ 中文玩家看得见的那半句英文侧没有。
   ⇒ **要做**（数据侧，属 `C1` 的 `数据/**`）：把 `desc` 补回
   `Ferocity: Attack a non Flying enemy troop. If it has a Hunt Mark, gain +1 [Attack]`（写法照同族 `SW12`）。
4. ⚠️ **棋盘落点 ≠ 请求格**：夹具里请求槽 6、实测落在槽 5（本仓棋盘是「**连续无洞**」模型）。
   ⇒ 探针夹具**不能按槽号认身份**（已改成按卡模板回找，并在 `NonKwProbe.BuildPair` 里写了纪律）。
5. ⚠️ **`EffectResolver.X` 这个名字不存在** —— `EffectResolver.cs` 里其实是 `partial class RuleCore`
   （编译期就撞到了：`CS0103`）。写探针/断言时一律走 `RuleCore.*`。
6. ⚠️ **卡池文件在本会话被并行写手改过**（md5 `cfc13db1…` → `b2ae84b2…`）⇒ 我按哨兵 ⑤ 的流程
   **把两个指纹一起重钉**（只钉一个会让「基线被静默改写」那条守卫失效）。⚠️ 卡池还在别人手里改，
   它再变一次、哨兵 ⑤ 就回到「此时不判」那一支（那是**已知限制**，不是失败）。
7. ℹ️ **`check` 的哨兵 ⑥ 现在是【红】的**（`A1462`），**这是有意的**（用户口径 ④：会红的断言要进哨兵组、计入 rc）。
   读法：那句 `✗` 逐条列出「哪张卡 · 哪个 op 栏 · 卡面写了什么」并指名修法；**修好解析层它会自己变绿**。
