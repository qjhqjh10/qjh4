# X6 · 口径乙（`ca` 列 + 按家族认行）执行报告（2026-10-25 · 执行代理 `X6`）

> 判据来源：`资料/待办判据_引擎_1025b.md` §一（已整份读）＋ `普查产出_1025/X3_引擎判据缺口_执行报告.md` §四·2。
> ⛔ 没跑任何 Unity（`-executeMethod`）；⛔ 没动 git 写；⛔ 没碰 `RuleCore.cs` / `BattleScene.cs` / `CollectionScene.cs` /
> `CardPresentation/**`（除 `Editor/RuleEngineTest*`）/ 任何 `任务总表` / `CLAUDE.md` / `项目任务.md` / `ProjectSettings*`。
> ⚠️ 期间 `BattleContext.cs` / `RuleCore.cs` / `TargetCriteriaEval.cs` 有**别的代理**在改 ⇒ typecheck 把它们在飞的内容一起编过，结果 0/0。

---

## 一 · 三处改了什么（file:line）

| # | 件 | 落点 | 做了什么 |
|---|---|---|---|
| 1 | **生成器** | `Unity/工具/gen_cards_engine.py:2000`（族表）· `:2023`（`ability_kind_family_of`）· `:2103`（落 `ca`）· `:4899`（`version: 29`）· `:6248`（读数） | `abilityLevelCriteria` 每行新增 **`ca`**（键序 `ab`→`trigger`→`ca`→`criteria`；**别的列/别的键一字未动**）。值 = 该 ability 的 `abilityLogic[*].chosenAbility` 家族；**族不唯一 / 无 logic ⇒ `""`**。族表 `ABILITY_KIND_FAMILY` 与 C# 的 `CardDef.ActiveAbilityKindFamily` **逐条对齐**（自检有跨语言对账断言兜漂移） |
| 2 | **读列** | `Data/CardDatabase.cs:706`（DTO `ca`）· `:2638`（映射 + **缺键出声**）· `:1312`（`version < 29` 加载告警）· `:1491`（版本位）· `:2801/:2905`（摘要读数） | 哨兵三档：`null` = JSON 缺键（`LogError` + 按 `""` 处置，**不丢行**）· `""` = 家族判不了（合法值）· 族名 = 可判。**独立版本位** `abilityLevelCriteriaFamilyVersioned`（v29），⛔ 不与 v26 的那一位合并 |
| 3 | **消费端** | `Core/CardDef.cs:2421`（`TargetCriteriaRowSpec.Family`）· `:2538`（版本位）· `:2687`（**按家族认行** `AbilityLevelCriteriaForFamily`）· `:2788`（`FamiliesOfVerb`，唯一词表 = `ActiveAbilityFamilyHasVerb`）<br>`Core/ActionPump.cs:922`（`AbilityFamilyTargetAllows`，三态）· `:960`（唯一出声口，同卡同族去重）· `:873/:878`（两个自检计数器）· `:899`（第 ⑧ 格按 `ab` 取）<br>`Core/EffectResolver.cs:342`（`ResolveOps` 新增两形参）· `:450`（调用）· `:237`（`ApplyAbilityFamilyGate`）· `:2475/:2599`（战术正文 + 突触两处接线） | 逐 op 闸：`op.Verb` → 族（`FamiliesOfVerb`；≥2 族 ⇒ 判不了）→ 认那一行 → 拿**点选目标**过 `TargetCriteriaEval.Matches`（含第 ⑧ 格）⇒ **明确不满足** `skip[i]=true`。<br>🔴 **只在「按 trigger 定位不唯一」的卡上启用**（`UniqueAbilityIndexForTrigger` 回 `-1` 且 `ambiguous`）⇒ 其余卡行为**逐字不变**；闸排在 `InsteadSkips` **之后**（配对要在完整 op 表上算） |

**断言**：新建 `Editor/RuleEngineTest_EngineX6.cs`（377 行，LF；`.cs.meta` guid `ec98403e7fb341758be8982264746888`，全仓现查唯一）+
`RuleEngineTest.cs:1171` 一行 `Step(TestEngineX6)`（CRLF 37181→37187，未翻）+ `RuleEngineTest_EngineD4.cs` 的
`version` 断言 **28 → 29**（同会话第五列，就地订正并留痕）+ `RuleEngineTest_Diag.cs` 重生成（**344** 条，`--check` 逐字节相同）。

## 二 · 映射表落地（19 可判 / 4 判不了）

* 池内 `abilityLevelCriteria` **85 行 / 75 张**；`ca` 可判 **80 行**，空串 **5 行**（3 行混族 + 2 行无 `abilityLogic`）。
* **按 trigger 重复（= 闸真会遇到）的卡 24 张**：`ThisCardPlayed` 那一族 **23 张 ⇒ 可判 19 / 判不了 4**（与判据文档逐张一致）；
  另一张是 **`SW9`**（歧义在 `trigger 410`，判据已定「原版本身不触发」⇒ 不在 32 行内，落 `""` 也不会被这条链消费）。
* 4 张判不了的**真实歧义族**：`ASH30` damage（两行）· `DA33` damage（ab1 混 damage+resource ⇒ `ca=""`）·
  `GSC46` damage（两行）· **`SOR31` resource**（两行；⚠️ 用 `damage` 去问会得到「明确无条件」= 放行但**不出声**，别混）。
* 断言逐条钉了 19 张的「卡×族 → ab」（24 个组合），并**逐张钉灭自证**：这 19 张按 trigger 定位**一律不唯一** ⇒ 没有家族层就是空转/整条丢。

## 三 · 断言与灭自证

| 组 | 正例 | 灭自证（回退即红） |
|---|---|---|
| `X6T1` 数据层 | 85 行 / 80 可判 / 5 空串 / 10 个族名；**跨语言**：落出去的 `ca` 全在 `ActiveAbilityKindFamilyNames` | 族表漂了 / 版本位没 bump ⇒ 红 |
| `X6T2` 19 张 | 逐卡逐族 `ab` 对得上；认出来的 36 格**就是** `AbilityLevelCriteriaAt(ab)` 同一对象 | 每张都断「按 trigger 定位不唯一」⇒ 谁敢改成「猜第一条」就红；家族路径另建 criteria ⇒ 红 |
| `X6T3` 4 张 | 判不了（`why` 非空）· **不出 false**（放行）· **各出声一次**（计数 +4）· 第二次不再出声 | 「判不了当不满足」⇒ 正文被掐死 ⇒ 红；出声没去重 ⇒ >4 ⇒ 红 |
| `X6T4` 真筛 | `SW43` 友方部队 true / **敌方 false** / 友方督军 false；`EC60` 两族互补；`AM75`/`TAU58` 第 ⑧ 格 `cond 220 dealtKillDmg` 的 `ex=0/1` 两边都钉 | 同一候选换到**没有行**的族 ⇒ true（闸恒 false 会红）；拿掉第 ⑧ 格 ⇒ `AM75` 敌方变 true ⇒ 红；`ex` 不取反 ⇒ 红 |
| `X6T5` 生产路径 | 真 `EC60` criteria + 自造 op `Draw a card`：友方候选下**带闸不抽（+0，skip 计数=1）** | **不传闸同一局面照抽（+1）** ⇒ 闸没接上/恒真/恒假三条都会红 |

## 四 · 读数

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_ca bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（每个 `.cs` 改完都跑；最后一次在全部改动之后） |
| `ruleprobe.sh check` | rc=0 · **哨兵失败 0 条** · **解析差异 0 行** · 输出指纹 **af836aa0…** 一致 |
| `ruleprobe.sh play check` | **rc=0** · ①-a 主档逐卡差异**空** · ①-b 电池差异**空** · 基线指纹 **fc591fb3…** 未变 |
| `gen_cards_engine.py --check` | **一致 ✅** |
| `gen_diag_runner.py` / `--check` | **344** 条；`--check` **逐字节相同**（46,552 B） |
| 离屏探针（`D:/tmp/x6probe`，不进工程） | **pass 81 / fail 0**（复刻本文件全部断言 + 生产路径的带闸/不带闸对照） |

## 五 · 新池 md5 + 逐列 diff 摘要（🔴 重钉请主对话做，我没改 `PinnedPoolMd5`）

* **新卡池 md5 = `ef431e8054822f359162862fdf51dd5b`**（旧 `0397005df1c7de826f56e651c4ca7b01`）。
* **逐列 diff（结构级，before/after 全量对比）：总 86 处 = `version` 28→29（1 处）+ `/cards[]/abilityLevelCriteria[]/ca` 新增 85 处**，
  其余**零差异**（`count` 1127 不变；任何卡的任何其它字段/键序都没动）。族别分布（行数）：`buff 31 · damage 17 · destroy 7 · heal 7 · resource 7 · summon 5 · create 2 · draw 2 · stun 1 · trigger 1`。
* **为什么这个变化是预期的**：只加了一列数据、没有改卡的内容 ⇒ 解析层输入里的「文本/字段」逐字节没变 ⇒
  `PinnedParseMd5` 现跑仍 **af836aa0…**、基线文件没重生成 ⇒ `PinnedBaselineMd5` **不用动**。⇒ **只需重钉 `PinnedPoolMd5`** 这一颗。
* `ruleprobe check` 的 `解析差异` = **0 行**。

## 六 · 没做的 + 还差什么

1. **`ca` 只落在 `abilityLevelCriteria`**（其余 criteria 列不落）：那几列的行按 `(ab, lg)` 就能从
   `activeAbilityLogic[].ca` 直接推族，且**当前没有消费者**；多落一列就是没人读的数据 + 更大的池 md5 面。要做时按同一张 `ABILITY_KIND_FAMILY` 派生即可。
2. 🔴 **单位卡的 trigger-0 那一跳没接**（`BL33`/`DA38` 可判 · `ASH30`/`DA33`/`SOR31` 判不了）：落点在
   `RuleCore.cs` 的 `FireTriggerAt` → `EnqueueAbility`（**白名单外**，⛔ 没碰）。⇒ 这 5 张的单位 Rally 正文**今天不过**家族闸
   （读列/认行/闸函数都已就绪，接上去就是给那一跳补 `abilityTrigger:"ThisCardPlayed"` + 一个族名）。
3. **`chooseone` 折成的单条 op 不在任何族**（实测 `SW43` / `SAU40` 等 `Give A or B` 的正文是**一条** `chooseone`）
   ⇒ 生产路径上族闸碰不到它们的分支。要真筛得先把 `chooseone` 拆到分支层（那是另一笔活，已进报告不进代码）。
4. `AbilityLevelCriteriaForFamily` 目前只有 `ThisCardPlayed` 一个调用点；别的 trigger（例如 `SW9` 的 410）同样可用但**没有调用点**。
5. **本轮没跑 Unity**（纪律）⇒ `RuleEngineTest.Run` 的 **344 条**由主对话在同步点跑；等价逻辑已在离屏探针跑过 81 条全绿。
## 七 · 顺手发现（⛔ 一个字都没改）

1. `Core/EffectResolver.cs` 里的类型**不是** `EffectResolver` —— 它和 `ActionPump.cs` 都是 **`RuleCore` 的 partial**；
   照文件名写 `EffectResolver.` 编不过（`RuleCore.cs:10905` 早就记过这条）。
2. `Editor/RuleEngineTest_EngineX1.cs.meta` 的 `guid` 是 **64 hex**（两段 uuid 拼在一起），全仓唯一但不是标准形状；
   新文件按标准 32 hex 生成。将来若有人按「32 hex」正则扫 meta，会漏掉它。
3. `AM75` / `SAU59` 的 `ab=1` 带 `unitConditions` 的 `role="target"` `cond=220 dealtKillDmg`（`TAU58` 是 `ex=1`）——
   说明 `E9` 那道第 ⑧ 格拼接口在**家族路径**上同样生效（已钉成断言，`ex` 两个方向都有）。
4. `RuleEngineTest_EngineD4.cs` 的 `version` 断言原来停在 28 ⇒ 已同步到 29 并写了归因行；下一位加列时**记得同步它**。

---

## 七 · 第二批（主对话授权）：**单位 trigger 那一跳接上族闸**

> 白名单：`Core/RuleCore.cs` + 本批测试文件（`Core/EffectResolver.cs` 当时有别的写手 ⇒ 一点没碰）。
> 🔴 **卡池一个字节没动**：重钉后的 `PinnedPoolMd5` = `ef431e8054822f359162862fdf51dd5b` 现算**逐字一致**。

| 件 | 落点 | 做了什么 |
|---|---|---|
| 闸本体 | `Core/RuleCore.cs` 的 `AbilityFamilyGateOpsForTrigger` | 逐 op：`op.Verb` → `CardDef.FamiliesOfVerb` → `CardDef.AbilityLevelCriteriaForFamily`（**复用已建好的读列/认行**）→ `AbilityFamilyTargetAllows` ⇒ **明确不满足**的 op 从这一批里剔掉（钱照收）。<br>🔴 三道窄闸：`only == null`（附魔不算本卡 ability）· **只有「按 trigger 定位不唯一」才启用**（其余卡原样返回）· **要剔的 op 参与 `instead` 配对 ⇒ 整条不剔 + 出声**（否则「谁替换谁」配不上） |
| 接线 | `RuleCore.cs` 的 `FireTriggerAt` | `_carrierCard` / `_kwTriggerName` **上提到 `body` 闭包之前**（闸要在**入队前**判 —— 原版 `TriggerAbilities.c:207` 就是派发那一刻同步判），`FireTriggerDeferred` 收到的是 `opsGated`；`abilityContext` 那段判据**一字未改**。 |
| 自检开门 | `RuleCore.cs` 的 `FireTriggerAtForTest` | **只为自检转发**（纪律同 `ActiveAbilityPickCriteriaWarningsForTest`：不改任何行为）。`FireTriggerAt` 本来就是 private。 |
| 判不了的去重口径 | `ActionPump.cs` 的 `EmitAbilityFamilyGateNotice` | 键分两档：**整卡 `ca` 有空串**（`DA33`）⇒ 键不含族名（同卡同 trigger **只喊一次**）；其余（`SOR31` 那种「同族 ≥2 行」）⇒ 键含族名。 |

**新断言**：`Editor/RuleEngineTest_EngineX6b.cs`（155 行 LF；`.cs.meta` guid `17fae971e1484ce289af0716c4ecc8be`，全仓唯一）＋
`RuleEngineTest.cs` 一行 `Step(TestEngineX6b)`（CRLF 37187→37219，未翻）。
**14 个 `Check*` 调用点 ⇒ 16 条运行时断言**（其中一条在 3 张卡的循环里）。覆盖：

* **可判 `BL33`**：非载具敌方目标 ⇒ `destroy` 族被剔（`skip` 计数 = **1**、`warn` = 0、结算后目标只吃 `Deal 5` 仍活着）；
  **灭自证** = 同一个载体 + **载具**目标（真卡 `ASH61 Weapons Platform`，`subtype=Vehicle`）⇒ `skip` = **0**。
* **可判 `DA38`**：友方目标 ⇒ `resource` 族（`enemyUnits=1`）被剔；敌方目标 ⇒ **仍被剔**，因为 `ab=1` 带着
  `unitConditions role="target" cond=220 dealtKillDmg ex=0`（第 ⑧ 格拼进来了）—— 两条方向把 criteria 的**两半**都钉住，
  「恒剔」由 `BL33` 的载具那条打红。
* **判不了 `ASH30`/`DA33`/`SOR31`**：仍入队（放行）· `skip` = 0 · **各出声一次（+3）** · 同卡第二次不再出声 ·
  `ASH30` 的正文**真的结算了**（目标 6 → 3，= 放行的观测证据）。
* **离屏探针**（`D:/tmp/x6probe`，含本批）：**pass 89 / fail 0**。

**读数（第二批之后）**：`TMPDIR=/tmp/wf_ca2 bash 工具/typecheck.sh` **0 / 0** · `ruleprobe check` rc=0 / **解析差异 0** / 哨兵 0 /
输出指纹仍 `af836aa0…` · `ruleprobe play check` **rc=0**（①-a / ①-b 两档差异**都空**）· `gen_diag_runner` **345 条**、`--check` 逐字节相同（46,623 B）·
**卡池 md5 = `ef431e80…` 未变 ⇒ 不需要再重钉**。

**如实标着的边界（⛔ 别当成已做完）**：`Rally` 那一跳是 `FireTriggerOnBoard → FireTriggerAt(…, owner, slot)`，**不传 `chosen`**
⇒ 生产环境里这 5 张**一律「判不了 ⇒ 出声 + 放行」**（= 改前行为，只是不再静默）。**真筛只在传 `chosen` 的调用形状上生效**
（`AboutToAttack` / 替代行动那两族在传；断言就是显式传 `chosen` 钉的判据本身）。要让 `Rally` 也真筛，得先有
「**先取目标、再判 criteria**」的次序（原版 `GetTargets` 在 `CanTriggerAbility` 之后/之前的那道链仍未逐句核）⇒ **另一笔，已记**。

**顺手发现（第二批）**：① `DA38 ab=1` 带 `cond 220 dealtKillDmg ex=0` ⇒ 「敌方目标也过不了」，别拿它当「敌方 ⇒ 通过」的靶子；
② **`KeywordTable.Normalize("Vehicle")` 认不出**（`Vehicle` 是 **subtype**、不是关键词）⇒ 造「载具靶子」必须给 `CardDef` 的 `subtype`，
拿合成 keyword 无效（探针先踩过，改用真卡 `ASH61`）；③ `EffectOp.FindInsteadBaseIndex` 是 `public`，本批用它做「别剔配对的一半」那道闸。
