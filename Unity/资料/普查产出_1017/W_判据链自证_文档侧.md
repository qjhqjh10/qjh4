# W-判据链自证（文档侧）· 收口报告

> 2026-10-17 · 写手代理 W · **只改 `资料/**/*.md`（白名单内，禁区一字未碰）**
> 目标 = 把「拿**我们自己的 Godot 复刻** `rule_core.gd` 当原版判据」这条口径订正到全仓文档。
> 措辞照 `RuleEngine/Core/RuleCore.cs` 文件头 2026-10-17 那一处订正（去读了原文再写）。

---

## ① 结论

- **扫描面**（**改前**、开场扫描）：
  - 父任务说「55 个 `.md` 引用 `rule_core.gd`」—— **实测就是 55**（`grep -rl "rule_core\.gd" --include=*.md`）。
    其中**禁区 11 个文件**，**白名单内 44 个文件**。
  - 我改扫**不带 `.gd` 的写法**（`rule_core`）后，**白名单内另有 4 个文件**命中：
    `卡牌数据源对账_0912.md` · `战斗规格/战斗优化证据_0828/README.md` ·
    `普查产出_1010/庚2_A262②_8组合并.md` · `索引与盘点/解包资源列表清单.md`
    —— ⚠️ **按 `rule_core.gd` 扫时它们全被漏掉**（父任务那份 55 的清单也漏）。
  - ⇒ **实际工作集 = 白名单 48 个文件 / 217 处**，下面说的档位都是对这 217 处逐处判的。
- **三档判定**（**逐处判**，不按文件判）：

  | 档 | 处数 | 文件数 | 处理 |
  | --- | --- | --- | --- |
  | **A · 当判据使的**（「原版 `rule_core.gd:N`」「权威源」「判据出处」「语义权威」「**权威**：」「是完整的规格」「改之前先回去看」…） | **24 处** | 15 | **全部改掉**。能查真判据的 → 改指 `d:/2/tools/decomp_full/` 的 `类名__方法名.c`；查不到的 → 写明「**逐行核对还没做 = 待查**」 |
  | **B · 只是交叉引用 / 历史记录**（「参考实现」「照抄」「对齐」「和它一致」） | **138 处** | 34 | **加一句旁证标注**：**20 个文件加在文件头**（覆盖该文件其余全部 B 处）· **6 处加在行内**。⛔ 一处未删、原句全留 |
  | **C · 本来就标了「非权威」的** | **55 处**（**9 个文件整份未动**） | 9 | **一个字都没动**。另在 5 个「已改」文件里还有 **6 处局部 C**（`规则引擎_设计.md:6/:9/:454` · `卡牌基座_进度与交接.md:160` · `卡牌效果管线_计划与交接.md:207` · `战术卡效果_移植方案.md:4/:7` · `规则书_实现指南对账.md:89`）也一字未动 |

  > 24 + 138 + 55 = 217 ✓（三档互斥、穷尽）

- **实际改动**：`git diff --shortstat -- Unity/资料/` = **39 个文件 · 49 insertions(+)/29 deletions(-)**。
  ⛔ **只改出处与措辞** —— **数值、结论、表格内容、判据本身一个都没动**；更正一律写成
  「⚠️ 2026-10-17 更正：原来写 X，实际是 Y，错因是 Z」**保留痕迹**（铁律 5）。
- **顺带那一条也改掉了**：`规则引擎_进度与交接.md:897` 的「关键词的**权威**时机表」
  → 改成「**关键词的时机表（第二来源，不是权威）**」，并补上真判据 —— 与它上一行 `:896` 的
  「**粉丝实体规则书（🔴 非官方）**」**不再同行自相矛盾**。

---

## ② 逐文件表

> 处数 = 改前该文件里 `rule_core` 的**出现次数**（非行数）。**EDIT = 本次动过**；**C = 判成 C，一个字没动**。

| 文件 | 处数 | 判成哪档 | 改成了什么（一句话） |
| --- | --- | --- | --- |
| `战术卡47条_语义查证.md` | 39 | **C**（文件级） | 一字未动 —— 顶部 `:14` + 文末 `:169` **两处**已明写「我们自己复刻的，**从不当权威**」「我们的规格书、**不是原版**」，正文全是逐条旁证行号 |
| `索引与盘点/解包资源使用地图.md` | 32 | **B**（文件头标注） | 加一句旁证标注（覆盖全篇 10 处「rule_core 实现」类描述） |
| `规则引擎_设计.md` | 19 | A `:231` + **C**（`:6/:9/:454`） | `:231`「**改之前先回去看 `rule_core.gd`**」→ 改指 `d:/2/tools/decomp_full/`，并注明 §六 各小节标题里的 `rule_core.begin_turn` 等**按旁证读**；`:6/:9/:454` 原本就标了，未动 |
| `索引与盘点/_资产盘点_0910.md` | 12 | **B**（文件头标注） | 该文件是 2026-09-10 的**旧 Godot 项目盘点**，12 处全是「盘点它里面有什么」⇒ 加一句旁证标注，原文全留 |
| `战斗规格/战斗优化方案_0828.md` | 9 | **B**（文件头标注） | 2026-08-28 的旧 Godot 施工文档 ⇒ 加旁证标注 |
| `战术卡效果_移植方案.md` | 8 | **A ×3** | `:90`「**原版**把…收敛在一处」· `:238`「**原版** `rule_core.gd:2635`」· `:295`「**权威语义**」⇒ 三处各改为「降级为旁证 + 真判据 = `CardScript__AddEffect.c` 等」 |
| `战斗规格/战斗重建_0827/战斗重建方案_0827.md` | 7 | **B**（文件头标注） | 旧 Godot 重建施工稿 ⇒ 加旁证标注 |
| `战斗规格/战斗缺口清单_0827.md` | 6 | **B**（文件头标注） | 同上 |
| `规则引擎_进度与交接.md` | 6 | **A ×5** | `:897` **权威时机表 → 第二来源**（本次点名的自相矛盾）· `:809` handler 顺序「照抄 `_resolve_text`」→ 真判据 3 个 `.c` · `:829`「**规格书**在哪」→「旁证在哪」· `:349` 能量修正记录 · `:352` 出牌校验顺序 —— 后四条各补「真判据 + 待查」 |
| `选牌_数据与规格.md` | 6 | **A ×1 + B ×1** | `:48`「**权威源**：`rule_core.gd`」→「降级为旁证 + 真判据 = `BattleManager__GetChoiceOptions.c` 等」；`:46` 标题加「（旁证，见下）」 |
| `阵营推进_清单与交接.md` | 5 | **A ×2** | `:216` 两个「判据出处」（`ecstasy` / `destroyer`）→ 改指 `CardScript__ShouldTriggerEcastasy.c` · `BattleManager__CanAttackCard.c` · `EntityScript__HasVanguardTrait.c` |
| `加时与冲突模式_原版规格.md` | 4 | **C** | 一字未动 —— `:111-112` 已用删除线 + 「`rule_core.gd` 是我们自己的 Godot 复刻（铁律 2 明写「不是权威」）」订正过；`:160/:886` 用的是「**参考实现**」+「15 是它挑的，**不是原版值**」 |
| `常驻效果_数据与设计.md` | 4 | **B**（文件头标注） | 加旁证标注 |
| `战斗规则与数值_出处.md` | 4 | **A ×2** | 🔴 **这份最要紧**：`:16` 五层表第 3 行原来标题就写「（**实现**）」、把 `rule_core.gd` 摆成第 3 层判据 → 改标「**我们自己的上一版 Godot 复刻、不是原版产物**」+ 该格换成真判据清单；`:268` 「**要具体数值 → `rule_core.gd`**」→ 改指第 5 层反编译；`:33` 加注 |
| `卡牌基座_进度与交接.md` | 3 | **A ×1（行内）** | `:15` 「规则书 / `rule_core.gd:44` / 原版 `MinionArea` 三处一致」→ 给 rule_core 那处加「我们自己复刻、非权威」，并点明**原版侧真判据** |
| `卡牌效果管线_计划与交接.md` | 3 | **A ×1** | `:222`「**原版** `rule_core.gd:2408` 那张表」→ 改指 `EntityScript__HasCurrentTrait.c` · `CardScript__AddTraitSilently.c` |
| `可并行任务清单.md` | 3 | **C** | 一字未动 —— `:55/:89` 本来就写着「⚠️ **我们自己的 Godot 复刻，不是原版**」并把它排在反编译之后 |
| `已知的坑.md` | 3 | **C** | 一字未动 —— `:77/:79/:80` 三条都已有「⚠️ 2026-09-19 更正：原出处写的 `rule_core.gd` **不是权威**」 |
| `战斗规格/战斗重建_0827/RuleCore对战API手册_0827.md` | 3 | **B**（文件头标注） | 旧 Godot API 手册 ⇒ 加旁证标注 |
| `战斗规格/战斗重建_0827/VFX迁移规格_0828.md` | 3 | **B**（文件头标注） | 同上 |
| `普查产出_0916/待修_Helbrute与Litany.md` | 3 | **B**（文件头标注） | 加旁证标注 |
| `事件层_数据与设计.md` | 2 | **B**（文件头标注） | 加旁证标注 |
| `战斗规格/战斗重建_0827/子代理读报_引擎映射_0827.md` | 2 | **B**（文件头标注） | 加旁证标注 |
| `普查产出_0916/吞句候选裁定_块1.md` | 2 | **A ×1** | `:5`「**语义权威** `d:/warpforge/scripts/rule_core.gd`」→ 全篇改为「只作**旁证**；真判据 = `d:/2/tools/decomp_full/`」 |
| `普查产出_0916/待修_结算层三件.md` | 2 | **A ×1** | `:99`「**权威**：`rule_core.gd`（`_apply_gain:3266`…）」→ 改指 `CardScript__AddEffect.c` · `ChangeBaseAttack.c` · `ClearEndOfTurnEffects.c` |
| `普查产出_0918/11⑤_ResolveTargets退化支_普查.md` | 2 | **A ×1** | `:150`「**原版语义在参考实现里查得到**」→ 改为「**旁证**里也写着同一条」，真判据指回本节上面已核实的 `BattleManager__GetLowestHealthUnit.c:153` |
| `普查产出_1008/审计_资料目录清理.md` | 2 | **C** | 一字未动 —— 它本身就是**记录这个缺陷**的审计报告 |
| `规则书_实现指南对账.md` | 2 | **A ×1 + C ×1** | `:52`「`rule_core.gd:4696` 也**明写**「防御卡=计策类」」→ 改标旁证 + 点明真判据是原版 `spellType = DefensiveCard`；`:89` 那处在**删除线里**（已订正过）⇒ 未动 |
| `资源使用手册.md` | 2 | **A ×1 + B ×1** | `:505`「**具体数值**去 `d:/warpforge/scripts/rule_core.gd`」→ 改指反编译，并把「唯一**权威**文字规则」改成「唯一**成文**的文字来源（**不是权威**）」；`:485` 旧 Godot 项目行 → 加行内旁证标注 |
| 其余 **19 个文件**（各 1–2 处） | 19 | 见下 | `临时卡Ephemeral_设计与实现计划.md` · `关键词三列对账.md`(A) · `单位卡desc与光环_批次划分.md` · `卡牌数据源对账_0912.md` · `卡表核对_卡图提取/_裁定_图标丢失.md` · `待办判据_战场与战斗视图.md` · `战斗规格/战斗优化证据_0828/README.md` · `战斗规格/战斗重建_0827/README.md` · `普查产出_0916/吞句候选裁定_汇总.md` · `普查产出_0916/静默桩家族_0916.md` · `普查产出_1007/波8_A77_11_文档订正.md` · `查证_裸写触发点_Leviathan.md` · `查证_裸写触发点_四批.md` · `第三方复刻对照_CardboardConsole.md`(A) · `索引与盘点/解包资源列表清单.md` —— 除标 (A) 的两处外**一律加旁证标注**；未动的三个 C：`文档总入口.md`（`:120` 已标）· `普查产出_1005/块11_顺手发现三条.md`（引的是**已带「非权威」的代码注释**）· `普查产出_1009/写手W3_A262资料目录清理.md`（记录别人已订正）· `普查产出_1010/庚2_A262②_8组合并.md`（合并索引行） |

**A 档 15 个文件一览**：`战斗规则与数值_出处.md` · `规则引擎_进度与交接.md` · `规则引擎_设计.md` ·
`战术卡效果_移植方案.md` · `选牌_数据与规格.md` · `资源使用手册.md` · `关键词三列对账.md` ·
`卡牌效果管线_计划与交接.md` · `卡牌基座_进度与交接.md` · `阵营推进_清单与交接.md` ·
`第三方复刻对照_CardboardConsole.md` · `规则书_实现指南对账.md` ·
`普查产出_0916/待修_结算层三件.md` · `普查产出_0916/吞句候选裁定_块1.md` · `普查产出_0918/11⑤_…_普查.md`。

## ③ 我改指了哪些真判据（全部**已核文件存在**，`d:/2/tools/decomp_full/`）

| 原来的 `rule_core.gd` 处 | 改指的原版方法体 |
| --- | --- |
| 棋盘 9 格 / 督军槽 | `MinionManager__GetSlots.c` · `MinionManager__AvailableSlotsCount.c` |
| 回合 / 相位次序 | `BattleManager__NextTurn.c` · `PlayerManager__TurnStart.c` · `BattleManagerSupport__BroadcastTurn{Setup,Start,Started,End,BeforeEnd}.c` · `CardScript__OnTurn{Setup,Start,Started,BeforeEnd,End}.c` |
| **关键词时机表**（`:897` 那条） | `BattleManager__AddTrigger{Slay,Ambush,Codex,Mob,Pray,Regiment,Relentless,Requiem,Sacrifice,SpiritStone,Synapse,Teleport,Uprising}.c` + `BattleManager__ResolveTrigger*.c` |
| 攻击/伤害管线 | `BattleManager__AddAttackAction.c` · `EntityScript__GetCombatDamage.c` · `BattleManager__CheckIfAttackKillsTarget.c` · `EntityScript__DamageKillsTarget.c` |
| 目标合法性（Vanguard / Destroyer 硬约束） | `BattleManager__CanAttackCard.c` · `BattleManager__IsValidAttackTarget.c` · `EntityScript__HasVanguardTrait.c` · `CardScript__GetTargetsAvailable.c` |
| 出牌校验顺序 / 打出结算 | `BattleManager__CanPlayCard.c` · `BattleManager__ResolvePlayCardFromHand.c` · `BattleManager__ApplyOffensiveAndDefensiveEffects.c` |
| **选牌**（`_resolve_choose` 那一族） | `BattleManager__GetChoiceOptions.c` · `BattleManager__GetChoiceFullPool.c` · `BattleManager__GetChoiceIndexOfCard.c` · `BattleManager._ChooseCardMethod_d__449__MoveNext.c` · `BattleManager._ChooseUnitTarget_d__780__MoveNext.c` |
| **效果文本解析**（`_resolve_text`） | `BattleManager__ResolvePlayCardFromHand.c` · `BattleManager__ApplyOffensiveAndDefensiveEffects.c` · `BattleManager__GetChoiceOptions.c` |
| **给单位加东西**（`_apply_gain`） | `CardScript__AddEffect.c`（974 行，加效果总入口）· `CardScript__ChangeBaseAttack.c` · `CardScript__ChangeMaxHealth.c` · `BattleManager__AddEffect.c` |
| 关键词增减（`GiveKw` 表） | `EntityScript__HasCurrentTrait.c` · `CardScript__AddTraitSilently.c` |
| 回合末清理 / 限时增益到期 | `CardScript__ClearEndOfTurnEffects.c` |
| **ecstasy** 关键词 | `CardScript__ShouldTriggerEcastasy.c` |
| 裸 `Deal N damage` 自动挑最弱 | `BattleManager__GetLowestHealthUnit.c` · `AbilityLogic__GetTargets.c`（这两条是 `普查产出_0918/11⑤` 里**已经核实过**的，我只把 `:150` 那条残余改写指回它） |

---

## ④ 还没做的文件清单

- **白名单内：无** —— 48 个文件（含 4 个新发现的）**全部判过**：39 个已改、9 个判 C 未动。
  **没有留下「改一半」的文件**（一个文件要么改完、要么没动）。
- **白名单外（禁区，我按规矩一字未碰 —— 列出来让主对话决定要不要另派）**：

  | 禁区 | 文件 | 处 | 说明 |
  | --- | --- | --- | --- |
  | `资料/说明书/` | 1 | 6 | ⚠️ **这个目录被 `.gitignore` 忽略**（`.gitignore:15`）⇒ **任何按 `git ls-files` 做的扫描都会漏掉它**（我第一遍就是这么漏的，第二遍改用文件系统 `os.walk` 才看见）。文件 = `01_战斗_对战/战斗规则摘要.md`（原版说明书摘录，禁区） |
  | `资料/普查产出_1017/` | 3 | 37 | **本轮别的写手正在写的产出**（跑这次任务时 `对账_卡组编辑部分_差异与待办.md` 还在被另一路写手追加 +36 行）⇒ 不该由我改 |
  | `资料/历史/` | 9 | 45 | 历史归档 —— 改了会毁掉「当时是什么样」的记录（铁律 6） |
  | `资料/规则书/` | 0 | 0 | 规则书**原文**，零命中 |
  | **合计** | **13** | **88** | 全部按白名单**一字未碰** |

---

## ⑤ 没查清的部分 / 留给定盘的三条

1. **改指的判据是「路径已核存在」，不是「已逐行读懂」** —— 我在 `d:/2/tools/decomp_full/` 里
   **逐个确认了文件存在**（31 个直接引用的 + 23 个花括号展开项，**全部 OK**），
   并读过 `BattleManager__GetChoiceOptions.c` / `BattleManager__CanAttackCard.c` 的开头确认主题对得上；
   但**没有**把每一条都读到「原版这一行就是那个语义」。
   ⇒ 所以我在正文里**一律写「逐行核对还没做 = 待查」**，没有把「改指」写成「已核实」。
2. 🔴 **顺带发现（不在本次白名单口径内，故未改，请主对话定盘）**：同一条「判据链」毛病还有**另一半 = 把粉丝规则书当权威**，
   我在扫的过程中还看到 **≥3 处**（本次只改了父任务点名的 `:897`）：
   - `索引与盘点/解包资源使用地图.md:953` —— 「**权威**：规则书回合结构 7 步」
   - `阵营推进_清单与交接.md:137` —— 「每个卡点先定语义，顺序是：**① 规则书** · ② 原版反编译方法名」
     （权威顺序应反过来：反编译在前，见 `CLAUDE.md` 铁律 2）
   - `资源使用手册.md:505` —— 本次已顺手改掉（「唯一权威文字规则」→「唯一**成文**的文字来源（不是权威）」）
3. **另外两条「同行自相矛盾」式的候选**（我没动，因为不在本次口径内、且改的是别人的活）：
   - `规则引擎_进度与交接.md:901` 把 `rule_test.gd` 列进「另外三份来源（都**不是**权威）」——
     这条**正确**，但同一行里「权威来源的正面判据见上面那一行」指的是 `:896`，读起来绕；
   - `索引与盘点/解包资源使用地图.md` 是一份 **2026-08-27 的 Godot 侧快照**，里面 32 处 `rule_core`
     讲的全是旧工程；我只加了文件头标注，**没有逐处改**（那份文件的性质是历史快照，逐处改会把
     「当时是什么样」改掉 —— 铁律 6）。

> **本机并发提示**：本次运行期间 `git status` 里出现了 `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` ·
> `EffectResolver.cs` · `项目任务.md` 的改动 —— **那是本轮别的写手的活，我一个字节都没碰**（我的写盘脚本
> 只写 `资料/` 下的白名单文件，40 个目标路径逐一硬编码在脚本里）。
