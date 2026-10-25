# F1 · `RuleEngineTest.Run` 3 条红（执行报告）

> **执行代理 `F1`** · 2026-10-25 · 第七十五会话 · 现场 = `d:/4/_tmp_view/20261025/ruleengine.log`（8425/8428）
> 白名单使用：`MyGame/Assets/RuleEngine/**`（`Core/` + `Editor/RuleEngineTest*`）。
> ⛔ 没碰：`CardPresentation/Battle/**` · `Core/**`（表现层那个）· `Shell/**` · `Editor/BattleScene.cs` ·
> `工具/ruleprobe/**`（**只运行，一个字节没改；没有重钉任何 `Pinned*`**）· 三份正本与任何 `任务总表.md` · git。

---

## 〇 · 一句话结论

3 条红里 **1 条断言过期（α）· 2 条夹具错（α）· 其中 1 条还牵出一个真缺陷（β，已修）** ——
**`DoForceAttack` 的实现是对的**（没有回归），`criteria` 那一侧只有一个真 bug：
`TraitNameOf(1260)` 给的名字是 camelCase `huntMark`，**过不了 `KeywordTable.Normalize`** ⇒
`traitsFilter = 1260` 的 **17 行 / 10 张卡**此前**一格都没生效**（判成「判不了」⇒ 消费端整条不筛）。

验收读数：`ruleprobe check` **rc=0**（解析差异 0 行 · 哨兵失败 0 条）· `ruleprobe play check` **rc=0**
（主档 1127 张 + 电池 586 张逐字节一致 · 结算指纹 `87209677…` 一致）· `typecheck` 运行时 0 / 编辑器 0。
🔴 **`RuleEngineTest.Run` 需要主对话复跑**（本代理不跑 Unity）。

---

## 一 · 逐条

### ① `✗ ★ A1 卡表 version = 25 —— 期望 [25]，实得 [26]`

| 项 | 内容 |
|---|---|
| 现象 | `RuleEngineTest_EngineD4.cs` 的 `D4Shape` 断 `dto.version == 25`，而表已经是 **26** |
| 根因 | 同会话的 **`E2`** 加第三列 `abilityLevelCriteria`（v26）—— 判据 → `资料/普查产出_1025/E2_ability_criteria管线.md` §二·1。这条断言是 `E9` 批写的，**没跟着 bump** |
| 判类 | **α（断言过期）** |
| 改了什么 | `Editor/RuleEngineTest_EngineD4.cs:81` —— 期望改成 **26**，文案写明「v26 = `abilityLevelCriteria`（ability 级 `targetCriteria` 36 格），v25 那两列照旧」；🧨 那一句改成「改回 25 ⇒ 红」 |
| 判据有没有放宽 | **没有**：仍然钉**确切版本号**（不是 `>= 25`）——「加列必须 bump」靠的就是这个等式 |
| 要重跑 | 要（`RuleEngineTest.Run`） |

⚠️ 同文件 §E 的 `D4VersionGuard`（v24 出声 / v25 不出声）**行为仍然成立**（它只数含
`activeAbilityTargets` 的警告，v25 表对那两列 ≥ 25 ⇒ 0 条），所以**没动它的代码**。
唯一顺带说明：它里面的 `curTable` 是 v25，今天已不是「当前表」—— 这不影响断言，本次不改。

---

### ② `✗ ★★ criteria 只筛受害者池：… 打出去 ⇒ 友方那个 3 攻打了一发（敌人 9 → 6）—— 期望 [6]，实得 [9]`

| 项 | 内容 |
|---|---|
| 现象 | `RuleEngineTest_EngineC5.cs` 的 `C5Q4bForceAttackCriteriaSide`：`PlayTactic` 返回 **OK**、攻击者**确实疲劳了**（那条断言绿）⇒ **打了，但没打在那个 foe 身上** |
| 根因 | **夹具**：`EC46` 的 `Target2` 是 `the enemy with highest attack`，而**候选池含敌方督军**（`ruleprobe play EC46` 现读：`（按「+attack」挑：FIXWarlord_EmperorsChildren、FIX_E1、FIX_E2、FIX_E3 → FIX_E2）`）。夹具里 `foe` 是 **0 攻**、夹具督军 `FixtureWarlord` 是 **2 攻** ⇒ 挑中的是**督军**，foe 一滴血不掉 |
| 证据（离线真跑，`/tmp/wf_f1probe`，复刻夹具原样） | `foe hp=9（不变）· 敌方督军 hp=30 → 27 · atk exhausted=True`；日志 `（按「+attack」挑：FixtureWarlord、C5EFoe → FixtureWarlord）` |
| 判类 | **α（夹具错，不是实现错）** —— `Core/EffectResolver.DoForceAttack` 的 `critOnDefender`（`EffectResolver.cs:2734`）**是对的**，同一条路 `ruleprobe play EC46` 也真打出去了 |
| 改了什么 | `Editor/RuleEngineTest_EngineC5.cs:82-111`：① `foe` 改成 **4 攻 / 9 血**（> 督军 2 攻 ⇒ 才是「攻击力最高」那个；它反击 4 点 ⇒ 攻击者 5 血剩 1，不死亡）② 新增**前提断言**「`C5EFoe` 攻击力 > 敌方督军」③ 新增「**敌方督军一点没挨**」断言（挡住「上面那条因为打错人而假绿」） |
| 灭自证仍成立 | 是：把 criteria 套回**攻击者池** ⇒ 友方那个被 `enemyUnits = 1` 淘汰 ⇒ 一个能攻击的都没有 ⇒ foe 仍 9 ⇒ 红（攻击者仍是友方，不受本次改夹具影响） |
| 要重跑 | 要（`RuleEngineTest.Run`） |

---

### ③ `✗ ★★ ① 敌方候选（ta=10 / enemyUnits=1）放行 —— 期望 [True]，实得 [False]`

| 项 | 内容 |
|---|---|
| 现象 | `RuleEngineTest_EngineE2.cs` 的 `E2Case1DataPipelineRealPool` 拿真卡 `SW16 Long Fang` 的 ability 级 criteria 喂 `TargetCriteriaEval.Matches`，期望**放行**，实得 False（= `Matches` 返回 `null`，因为断言写的是 `== true`） |
| 根因（两层） | ① **β（实现缺陷）**：`SW16` 那一行有 `traitsFilter = 1260`（卡面 `When this unit attacks an enemy with Hunt Mark, …`）⇒ 候选**必须带猎杀标记**（原版 `FilterMethods__MeetsTraitCriteria.c`：`excludeTrait == 0 ⇒ 必须 has`）。而 `Core/TargetCriteriaEval.cs` 的 `TraitNameOf(1260)` 原来返回 **camelCase `"huntMark"`** —— `KeywordTable.Prefixes` 里只有 `new[] { "hunt mark", "huntmark" }`（**表头 = 带空格那一条**），小写后的 `huntmark` **前缀匹配不到** ⇒ `Normalize` 返回 `null` ⇒ `TraitPasses` 返回 **`null`（判不了）** ⇒ 消费端（`EffectResolver.ApplyTargetCriteriaFilter` 的 null 支，`EffectResolver.cs:1984-1995`）**整条 criteria 静默不筛**。<br>② **α（夹具与数据自相矛盾）**：夹具摆的是一个**不带标记**的敌人，却断言「放行」 |
| 影响面（现读卡池） | `traitsFilter = 1260` 共 **17 行 / 10 张卡**，**全是 Space Wolves**：`abilityLevelCriteria` 8 行 · `abilityTargetCriteria` 7 行 · `activeAbilityTargets` 2 行（`SW9 / SW14 / SW16 / SW26 / SW29 / SW30 / SW32 / SW33 / SW53 / SW54`）—— 它们此前**一格都没生效** |
| 判类 | **β + α**（先修实现，再修夹具） |
| 改了什么 | ① `Core/TargetCriteriaEval.cs:730`：`case 1260: return "huntMark";` → **`return "hunt mark";`**（+ 一段判据与影响面的注释；⛔ 别改成 `huntmark`：那不是前缀表里的**表头**，只是别名那一半）<br>② `Editor/RuleEngineTest_EngineE2.cs:146-160`：夹具改成 **带 `Hunt Mark` 的敌人** + 新增**「不带标记的敌人 ⇒ 淘汰」**一条（后者才是「这一格真有数据」的判别式），保留「己方候选淘汰」那条 |
| 灭自证 | 修前：`Matches(不带标记) = null` ⇒ 新加那条 `== false` **红**；修后 `false` ⇒ 绿。`Matches(带标记)` 修前 `null` ⇒ 红、修后 `true` ⇒ 绿。三方对拍（带标记 true / 不带 false / 己方 false）结构上排除「恒真」与「恒假」 |
| 离线实据（`/tmp/wf_f1probe`，直接编 `Core/*.cs`） | 修前：`plain -> null`（gap=`traitsFilter=1260 本版认不出`）· `marked -> null`；修后：`plain -> False`（gap 空）· `marked -> True` · `own -> False` |
| 其它 14 个 trait 名 | 逐个过了一遍 `KeywordTable.Normalize`：**只有 1260 这一条坏**（`frenzied → destroyer` 等其余全通）—— 已写进 `TargetCriteriaEval.cs` 注释 |
| 要重跑 | 要（`RuleEngineTest.Run`） |

---

## 二 · 验收读数（本代理实际跑过的）

| 命令 | rc | 读数 |
|---|---|---|
| `TMPDIR=/tmp/wf_f1b bash 工具/typecheck.sh` | — | 运行时错误 **0** · 编辑器错误 **0**（改完 4 个文件之后跑的） |
| `bash 工具/ruleprobe.sh check` | **0** | 全池 1121 张 · **解析差异 0 行** · 改名 0 · 新增 0 · 消失 0 · **哨兵合计：失败 0 条**（含 ⑤ 基线指纹 ✓ / ⑦ 输出指纹 ✓） |
| `bash 工具/ruleprobe.sh play check` | **0** | 主档 **1127 张逐字节一致** · 事件电池 **586 张逐字节一致** · 结算指纹 `87209677070c8421b8479e3bcc0effab`（与基线一致）· 基线文件哨兵 ✓ |
| `RuleEngineTest.Run` | — | 🔴 **本代理不跑 Unity ⇒ 需要主对话复跑** |

🔴 **`play check` 零差异的解释（别当成「改了没生效」）**：10 张受影响卡里，**只有 `SW30` 的
`deal` op 真的挂上了 `ta=10 / traitsFilter=1260`** 的 logic 级 criteria；而 `SW30` 的**文本层**
（`with Hunt Mark`）在探针夹具里就已经把候选筛成 0（`ruleprobe play SW30` 现读
`（按关键词筛「hunt mark」：4 → 0）`）⇒ 改前改后都是「没有合法目标」。
其余 9 张走的是 **ability 级触发前闸**（`Attack`/`Die` 等），而 `E2` 的报告 §四已记「那 22 张卡的
`Attack`/`Die` 在电池里一次都没被抬起来（`targetCriteria` 告警 0 条）」⇒ 探针**本来就照不到这一层**。
⇒ 这条修复的**行为证据**是上面那份离线探针（`null → true/false` 三档）+ 新的 Unity 断言，**不是** `play check`。

---

## 三 · 顺手发现（⛔ 只报不改，供调度台分流）

1. **日志里第 4 条 `✗`（诊断入口过期，缺 `TestE2AbilityLevelCriteria`）现在复现不了。**
   现读 `Editor/RuleEngineTest_Diag.cs` 里**已经有** `TestE2AbilityLevelCriteria`（`:94` 的
   `RunCaseNames` / `:572` 的 `Safe(...)`），且
   `python -I -X utf8 工具/gen_diag_runner.py --check` → **rc=0 · 334/334 · 与重生成结果逐字节相同（45595 B）**。
   ⇒ 那一行大概是**本会话较早一次跑**留下的（之后有人重生成过）。**不需要动它。**
2. **夹具族的通用坑（值得写进测试夹具的口径）**：卡面写 `the enemy with highest attack` 时，
   候选池**含敌方督军**（`ruleprobe play EC46` 现读候选里就有 `FIXWarlord_…`）。
   ⇒ 任何「按最高攻挑人」的夹具，**目标必须把督军比下去**，否则量的是督军。本次第 ② 条就是这个形状。
3. **`play check` 的边界再确认一次**：它对「ability 级触发前闸」这一层**天生看不见**
   （E2 §四已记）。⇒ 这类改动只能靠 `RuleEngineTest.Run` 与离线探针验收，别拿 `play check` 绿当证据。
4. **卡池现读 `traitsFilter=1260` 的 17 行**里，`SW9` 的两条 ability 级行是 `exT=0` / `exT=1` 成对的
   （「带标记走一条、不带标记走另一条」）—— 修好 `TraitNameOf` 之后这一对**才第一次真的分叉**；
   建议后续给 `SW9` 补一条端到端断言（本批没做，属 `A2` 的 §五·5「歧义档处置」相邻的一块）。

---

## 四 · 改动清单

| 文件 | 改动 | 行尾 |
|---|---|---|
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineD4.cs` | `:81` 版本断言 25 → **26** + 文案（v26 = `abilityLevelCriteria`） | LF 516 / CRLF 0（未翻） |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineC5.cs` | `:82-111` `C5Q4b` 夹具（foe 4 攻）+ 两条前提/判别断言 | LF 867 / CRLF 0（未翻） |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineE2.cs` | `:140-166` `E2Case1` 行为探针（带标记的敌人 + 不带标记的对照） | 新文件（untracked），LF 315 / CRLF 0 |
| `MyGame/Assets/RuleEngine/Core/TargetCriteriaEval.cs` | `:703-730` `TraitNameOf(1260)` → `"hunt mark"` + 判据/影响面注释 | LF 786 / CRLF 0（未翻） |

**没改**：`工具/ruleprobe/**`（含任何 `Pinned*`）· `CardPresentation/**` · 正本 · 任务总表 · git。
**临时产物**：`/tmp/wf_f1probe`（离线探针，**在仓库外**，可随时删）。
