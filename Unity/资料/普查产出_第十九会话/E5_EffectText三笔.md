# `E5` · `EffectText.cs` 三笔（`A1408` · `A1428` · `A1472`）—— 执行写手交件

> 2026-10-11 · **动手修**（独占 `RuleEngine/Core/EffectText.cs`）。
> ⛔ 没跑 Unity · ⛔ 没跑 `typecheck.sh` · ⛔ 没动 git / 没改正本 / 没越白名单。
> 唯一产出 = 本文件 + `EffectText.cs` 的改动。
> 离线探针（工程外，只读源码）：`D:/tmp/wf_e5/`（`probe/Esc5Driver.cs` + `base/` = HEAD 快照 + `after/` = 快照＋本笔改动）。
> ⚠️ **为什么用离线探针**：`RuleEngine/Core/EffectResolver.cs` 此刻有别的写手在改（构建报
> `CS0103 CostKindKnown` / `CostPrefixPayable` ⇒ **是别人半成品**，不是本笔）⇒ 直接编工程编不过。
> 做法 = `git show HEAD:<每个 Core/*.cs>` 快照一份（`base/`），只把本笔的 `EffectText.cs` 叠上去（`after/`）。

---

## 一、改动清单（行号 = 改后现读）

| 位置 | 改了什么 | 服务 |
|---|---|---|
| `EffectText.cs:928-943` | 🆕 给 `EffectTargetSpec` 加 `Clone()`（`MemberwiseClone`，与既有 `EffectOp.Clone` 同法） | `A1428` |
| `EffectText.cs:1176-1242` | 🆕 `Parse` 里新增一趟「`instead` 那条的**目标承接**」回填（含 40 行判据注释） | `A1428` |
| `EffectText.cs:2041-2043` | 触发前缀那段注释：把写死的「那 **8** 个」改成「以 `CardDef.RoutableTriggers` 为准」＋留订正痕迹 | `A1408` |
| `EffectText.cs:6540-6556` | `RePaidBare` 的 doc 里记下「无冒号付费段 = 2 条/2 张」＋「探针那一档结构上看不见」 | `A1472` |

`git diff --numstat` = **109 / 1**（LF 没翻；文件 8300+ 行，翻了的数字会是 ~8300）。

---

## 二、每笔的判据 + 🧨 改坏法

### 1. `A1408` —— 纯注释，**没有断言**（如实说）

- 实测（`base` 探针，C 段）：`CardDef.RoutableTriggers.Length` = **22**
  （`rally, strike, slay, backlash, penitence, mob, regiment, cruelty, artifice, duty, pray,
  ferocity, agenda, uprising, stimulation, teleport, ambush, codex, ecstasy, spiritstone,
  survivor, sacrifice`），而那一行原来写「那 **8** 个」。
- 铁律 5：就地订正那一行**本身**，并把数**删掉**、改成指回表（「以 `CardDef.RoutableTriggers` 为准」）。
- 顺手 `grep` 了同一句话有没有被复制到别处：`Unity/MyGame/Assets/**/*.cs` 里只有
  `Editor/RuleEngineTest.cs:1954` 提到 `RoutableTriggers`（讲的是「47 词灌进来会翻 187 张」，
  **另一个数、不是这句的复制**，且那文件不在本笔白名单）。`资料/*.md` 里只有账目那一行本身（不归我改）。
- 🧨 **改坏法：没有**。它是注释 —— **没有任何改动/断言能把它弄红**。
  唯一的「守卫」是那句「别在这儿写个数」的文字本身。⛔ 不许把它说成「有断言」。

### 2. `A1428` —— 跨句承接：**查证了原版怎么表达，照原版那一支做了**

**原版判据（第一权威 = 反编译）—— 原版【真的】有这一档**：

| 证据 | 位置 |
|---|---|
| `TargetsAffected.previousAbilityTargets = 140` · `previousAbilityFiltered = 150` | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs` |
| 这两个枚举值的**唯一** resolver —— `:849` `if (iVar6 == 0x8c /*140*/)` ⇒ `AddRange(结果, 静态 pooledPossibleTargets)`；`:867` 是 `0x96 /*150*/`（同形，差别只是 150 之后还要再过一遍筛选） | `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c` |
| 那个静态缓冲是**上一条 logic 自己存进去的**（清空 + `AddRange`） | `d:/2/tools/decomp_full/AbilityLogic__SetPreviousAbilityTargets.c`（整条方法体） |
| 调用点 = 每条 logic 结算的**末尾**，存的是 `this + 0x80`（`abilityTargetsToSave`） | `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:2745` |
| 一条 ability = **一个** `targetCriteria`（ability 层）＋ `List<AbilityLogic>`（每 logic 另可各带一份） | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardAbility.cs` · `AbilityLogic.cs` |

⇒ **原版这一档 = 「把上一条 logic 存下的那批目标整批拿过来用」，与「没写目标」是两回事。**
我们照的**就是**这一支：解析期把「被替换那条」的目标**复制**一份贴上去。

**我们这侧为什么原来会含糊**：`SOR6` 第二段那句卡面**没写给谁** ⇒ `TryGive` 的 ④ 支标成
`Subjectless`；而 `Subjectless` 的语义是「有施放者就是它自己、没有就己方全体」
（`EffectResolver.cs:946-983`）。它是**战术卡**（`source == null`）⇒ 落到「己方全体」
（`EffectResolver.cs:979` 的 `AddSide(own)`）。**今天正巧打对**，但那是兜底不是承接 ——
上一句的目标一旦比「己方全体」窄（例：`to a friendly troop`），这一支会**静默打宽**。

**配对判据 = `EffectResolver.ResolveOps` 的同一份**（`Core/EffectResolver.cs:126-140` 的
`insteadBase` 循环），四条逐字相同：从 `i-1` 往前找 · 碰到另一条 `Instead` 就停 ·
跳过得带 `Condition` 的 · 动词要相同。⛔ 没另立一套。

**闸门（收得紧）**：① 只治 `op.Target.Subjectless`；② 被替换那条自己也 `Subjectless`/没目标 ⇒ 不动。

🧨 **改坏法（这一笔是能被弄红的，三条闸各自都能）**：
- 去掉 `!op.Target.Subjectless` 那道闸 ⇒ 全池 6 条 `… damage instead`
  （`ASH31`/`ASH36`/`AM40`/`DA37`/`GSC54`/`GOF94`）会被那条闸放过，目标从「自动选敌方最弱」
  换成被替换那条的 `an enemy`（`Count=1 Auto=0`）⇒ **变成要玩家点目标**，池签名 diff 从
  **1 行**涨到 7 行；
- 去掉 `b.Verb != op.Verb` ⇒ `DA37` 的 `gain`（`instead` + `Subjectless`）会去接前一条 `deal` 的目标；
- 去掉 `bt.Subjectless` ⇒ 会把「前驱自己也说不清」的规格复制过来（今天 0 站点，属防呆）；
- 把 `bt.Clone()` 写回 `bt`（共用同一个对象）⇒ `alias` 档会多报站点（今天 `base` 与 `after` 的别名站点**完全一致**）。
  **判据 = `sig` / `segsig` / `idem` / `alias` 四档对拍**（命令见本文件末 §五）。

### 3. `A1472` —— **裁定：同族**（并且不止 1 条，是 2 条）

**核实结果（解析层实测，1126 张的 `desc` 逐段跑 `EffectText.Split` + `ParseSegment`）**：

| 卡 | 段原文 | 解析层读数 | 有没有冒号 |
|---|---|---|---|
| `SOR72 Adelaide the Serene` | `6 ☀ Gain Flank and Shield` | `kind=Ok paidCost=6 paidKind=faith` | **无** |
| `SOR74 Sacred Rose` | `6 [icon] Give them Flank` | `kind=Ok paidCost=6 paidKind=faith` | **无** |

⇒ **两条都是「付费段」**（`ck=faith`，与 `A1436`/`A1452` 那 11 条/9 张**同一条解析路径**），
只是卡面**印得下就不印冒号**。**卡面实据（铁律 7，亲读 PnP 成品图）**：
`d:/2/Warpforge部队卡片/Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png` 印的是
`6 ☀ Gain ⤴Flank and 👁Shield.`（**无冒号**）接 `⚡Rally: …`（有冒号）；
`…/4计策/Warpforge_10_Sacred-Rose.png` 印的是 `Deploy two Sacred Rose Sister. 6 ☀ Give them ⤴Flank`（**无冒号**）。
⇒ **不是 OCR 抄丢了冒号，卡面本来就不印。**

⚠️ **账目那一行说「同族可能还有第 12 条段 / 第 10 张卡」—— 实测是 2 条**：
`SOR72` 进来是第 12 条，**`SOR74` 是第 13 条**，而 `SOR74` 在三张名单里**一处都没有**
（`W_A1436付费能力.md:166-181` 的 11 条/9 张没有它 · `A1458` 的「无消费点 12 张」也没有它）。

**探针那一档为什么看不见（探针侧，⛔ 本笔没动）**：`工具/ruleprobe/WillRun.cs` 的
`Classify` ② 档判据是 `b.Head`（= `AddFaceBody` 里 `s.Substring(0, s.IndexOf(':'))`）
⇒ **没有冒号的段进不了「付费段」那一档**，会被 ⑤「无头」档收走
（`SOR72` 就落在 `A1458` 的「无消费点」名单里 —— **同一张卡被两本账按不同名目各记一次**）。

⇒ **本笔在解析层做的事（能做的全部）**：把「要数这一族**必须走 `ParseSegment`**」这条判据
连同 2 条/2 张的实测数写进 `RePaidBare` 的 doc（`EffectText.cs:6540-6556`），
并写明「⛔ 别按 `seg.Contains(':')` 数付费段」。**改探针 / 把两条并进 `A1452` 的实施
都不在本笔白名单**（`工具/ruleprobe/**` 与 `CardDef.cs`），**只报不做**。

🧨 **改坏法：没有运行时断言**。它是 doc ＋ 一个**可复现的读数**：若哪天 `PaidVerbs` / `RePaidBare`
被改窄到认不出 `SOR72`/`SOR74`，按注释里那条命令重跑会得到 **0 条**（`base`/`after` 实测都是 2 条）
⇒ **读数会红，注释会过期**。⛔ 不假装它有断言。

---

## 三、实测对照（**改前 → 改后**，全部离线探针；Unity 侧读数需主对话跑）

| 项 | 改前（`base/` = HEAD） | 改后（`after/`） |
|---|---|---|
| **`SOR6` 付费段的 `Target`** | `side=own kind=unit n=0 **subj=1 auto=1** raw=(未写目标：有施放者就是施放者自己，否则己方全体)` | `side=own kind=unit n=0 **subj=0 auto=0** raw=your units` |
| `SOR6` 第一段（被替换那条） | `side=own kind=unit n=0 raw=your units`（**未变**） | 同左 |
| `SOR6` `Parse` 的 op 数 / `unparsed` / `partial` | 3 / 0 / 0 | 3 / 0 / 0 |
| **全池 op 签名**（1126 张 `desc`＋全部 `keywords` 原文与冒号后正文，**1262 行**） | —— | **diff = 恰好 1 行**（只有 `SOR6\|desc\|1` 那一条） |
| **全池「单段单独 Parse」签名**（`CardDef.AddWhenTrigger`/`AddTriggerOp` 那条路，**2762 行**） | —— | **diff = 0 行**（`ParseSegment` 无跨段上下文 ⇒ 触发正文一个字没动） |
| **全池 `Parse` 覆盖**（逐卡 `ops` / `unparsed` / `partial`，1126 张） | 合计 `ops=1241 unparsed=184 partial=11` | **逐卡 diff = 0 行**，合计同上 |
| **幂等性**（同一 `desc` 连调两次 `Parse`，desc＋keywords 全池） | 破 0 处 | 破 0 处 |
| **目标规格别名**（两条 op 共用同一个 `EffectTargetSpec` 对象） | 2 站点（`UM44`、`UM_Codex_Discipline`，**既有**） | **完全一致**（本笔没新增别名） |
| `CardDef.RoutableTriggers.Length` | **22**（注释写 8） | 22（注释不再写数） |
| `ParseSegment` 认得成付费段的段 | 带冒号 **75 条/73 张** · **无冒号 2 条/2 张** | **同左**（本笔不动 RePaidBare 的行为） |
| 其中 `ck=faith` 的付费段 | **19 条 / 17 张** | 同左 |
| 全池 `Instead` op 总数 | **12 条** | 12 条（只有 `SOR6` 那 1 条的目标被改） |
| 编译 | —— | **离线 net8 编译 0 错 0 警告**（本笔的 `EffectText.cs` ＋ HEAD 的其余 `Core/*.cs`）；⛔ **Unity 侧编译未跑**（主对话跑） |

🔴 **必须如实说清的一条**：`SOR6` 的**结算结果今天不变**。`Subjectless` 在战术卡上落到
「己方全体」（`EffectResolver.cs:979` 的 `AddSide(players[owner], false, kind=="troop", -1)`），
而 `your units`（`own/unit/count=0`）走的是**同一个** `AddSide`（`EffectResolver.cs:1038`）
⇒ 两次都收**同一批人**（含督军：`troopOnly` 两边都是 `false`）。
**所以这一笔不是「修了一个今天看得见的错」，是**：① 把「靠兜底打对」改成「按原版机制承接」；
② 去掉一条**潜在的静默打宽**（上一句目标更窄时）。

---

## 四、没查清 / 没做 / 顺手发现

**没做（都不在本笔白名单，逐条给出该谁做）**：

1. 🔴 **`A1452` 的射程要加 2 条/2 张**（`SOR72` ＋ **`SOR74`**）—— 本笔只把事实查实并落进解析层注释；
   `CollectFaithOps`（`CardDef.cs`）/ `ResolveFaithAbility`（`EffectResolver.cs`）/ `WillRunOps` 的来源
   （`EffectText.cs:1667` 那一档，**属本文件但属 `A1452` 的实施单、不是本笔三笔**）**都没做**。
2. **探针 ④ 档的判据没改**（要 `:` 那一处，`工具/ruleprobe/WillRun.cs` 的 `Classify`）⇒
   `willrun` 今天**仍然看不见** `SOR72`/`SOR74`，rc 照旧。**这是探针侧的缝**。
3. **`A1428` 的行为面没有自检覆盖**。要真看「承接生效」，得造一张**上一句目标更窄**的夹具卡
   （例：`Give +1 [Attack] to a friendly troop. 2 Energy: Give +2 [Attack] instead`）跑 `ResolveOps`——
   但夹具宿主是 `RuleEngine/Editor/RuleEngineTest.cs`（**别的写手正在改**）⇒ **没做，报给主对话**。
   今天可用的判据只有本笔那四档离线对拍。

**没查清**：

4. ⚠️ **「faith 付费段」到底有多少条，我给出的数（解析层 19 条/17 张）与 `A1452` 的射程
   （探针口径 11 条/9 张）不是同一把尺子，我**没有**把它们对上。**
   两个数的分母不同：探针那一档只收「**腿 A 一条都没收到**」且 `HandledByOtherLayer` 为空、
   且**冒号后正文解得开 op** 的段（`WillRun.cs` 的 `AddFaceBody`）。举例：
   `SOR28`（`2 ☀: Deal 1 additional damage`，unit）在解析层是 `ck=faith`，却**不在**探针那 11 条里。
   ⇒ **「射程到底该按哪把尺子算」需要现查**（先查 `HandledByOtherLayer` / body 解不解得开），
   **别拿我这 17 当射程**，也别拿 9 当射程。

**顺手发现（一条一句 + 出处）**：

5. ⚠️ **`SOR74` 在三张名单里一处都没有**（`W_A1436付费能力.md:166-181` · `A1458` 的 12 张 ·
   `A1472` 的 1 条）⇒ 只按「名单」做的账会**整张卡漏掉**。出处 = 本文件 §二·3 的扫描。
6. ⚠️ **同一张 `SOR72` 被两本账按不同名目各记一次**：`A1458` 记「无消费点」（⑤ 无头档的 `KHNone`）、
   `A1472` 记「付费段看不见」。两本都不是错的，但**口径不同 → 数会重复计**。
   出处 = `资料/任务_卡组卡牌战斗.md` 的 `A1458` / `A1472` 两行 ＋ `WillRun.cs` 的 `AddHeadless`/`Classify`。
7. ⚠️ **全池有 2 处「两条 op 共用同一个 `EffectTargetSpec` 对象」**（改一个两边一起变，而且**静默**）：
   `UM44`（`desc` #0 与 #1，`raw=(未写主语：…)`）· `UM_Codex_Discipline`（`desc` #0 与 #1，`raw=your troops,`）。
   **本笔前后完全一致**（不是本笔造成的），出处 = `alias` 档扫描（命令见 §五）。
8. ✅ **`EffectTargetSpec` 的引用型字段只有 `PrevAntecedent` 一个**（其余引用型全是 `string`）
   ⇒ 本笔 `Clone()` 的浅拷**实际上只共享它**，而 `LinkPrevAntecedent` 只回填 `Kind == "prev"` 的目标
   ⇒ 本笔这条新路径**不会**共享它（`SOR6` 那条克隆时两边都是 `null`）。出处 = 本文件字段清点 ＋
   `EffectText.cs` 的 `LinkPrevAntecedent`。

---

## 五、复现命令（全部在工程外，只读源码）

```bash
# 1) 快照（HEAD 的 Core/*.cs）—— base/
cd /d/4 && for f in $(git ls-files Unity/MyGame/Assets/RuleEngine/Core/ | grep '\.cs$'); do
  git show "HEAD:$f" > "/d/tmp/wf_e5/base/$(basename $f)"; done

# 2) 卡片文本 TSV（id \t type \t desc \t keywords）
PYTHONIOENCODING=utf-8 python -I <脚本>   # 见本笔工作目录，读 cards_engine.json（1126 张）

# 3) 两档构建（after/ = base 的拷贝 ＋ 本笔的 EffectText.cs）
cd /d/tmp/wf_e5/base  && dotnet build -c Debug -v q --nologo && ./bin/Debug/net8.0/esc5b.exe D:/tmp/wf_e5/texts.tsv sig    > /d/tmp/wf_e5/sig_base.txt
cd /d/tmp/wf_e5/after && dotnet build -c Debug -v q --nologo && ./bin/Debug/net8.0/esc5a.exe D:/tmp/wf_e5/texts.tsv sig    > /d/tmp/wf_e5/sig_after.txt
# 其余档：cards / instead / cov / segsig / idem / alias

# 4) 对拍（期望值写在 §三 那张表里）
diff /d/tmp/wf_e5/sig_base.txt /d/tmp/wf_e5/sig_after.txt      # 期望 = 1 行（SOR6）
diff /d/tmp/wf_e5/segsig_base.txt /d/tmp/wf_e5/segsig_after.txt # 期望 = 0 行
diff /d/tmp/wf_e5/cov_base.txt /d/tmp/wf_e5/cov_after.txt       # 期望 = 0 行
```

⚠️ 探针的 `base/` 是 **HEAD 快照**，**不是**工作区 —— 因为 `EffectResolver.cs` 此刻有别人半成品的
`CS0103`。`after/` = 同一份快照 ＋ **只有** `EffectText.cs` 换成工作区版 ⇒ **一次只动一个变量**。
