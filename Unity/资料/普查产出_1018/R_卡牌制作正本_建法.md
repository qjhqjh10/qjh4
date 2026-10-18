# R · 卡牌制作正本 —— 建档过程、覆盖范围与没查清项

> 日期：2026-10-18 · 执行代理（只写文档，未改任何 `.cs` / `.py` / `.json` 数据，未动 git）
> 任务：把「卡牌 ID 逆推线」改写成「卡牌制作知识线」，并产出 `资料/卡牌制作_正本.md`。
> 产出文件：
> · `资料/卡牌制作_正本.md`（新建）
> · `资料/任务线/卡牌ID逆推线/任务总表.md`（重写）
> · `资料/任务线/卡牌ID逆推线/准则与做法.md`（重写）
> · `资料/任务线/卡牌ID逆推线/任务/K09_三张表补全.md`（新建）

---

## 一、取材路径（读了什么、读到什么程度）

| 材料 | 读法 | 用途 |
|---|---|---|
| `资料/普查产出_1017/卡牌ID/R_SO与触发代码链_可行性.md` | **全文 386 行** | SO 类/字段/三种本地形态/覆盖率/歧义来源/提取器设计 |
| `资料/普查产出_1017/诊断_第四十八会话/P1_原版效果触发代码地图.md` | **全文 377 行** | 五层结构 / 通用动作表 / 关键词三形态 / 字段偏移 / 未查清清单 |
| `资料/普查产出_1018/allcards_1350/报告.md` | **全文 126 行** | 1,351 张 SO 的提取方法、bundle 坐标、对账结论 |
| `资料/普查产出_1018/allcards_1350/cards_raw.json` | **程序化读取 1,351 条**（未整文件载入上下文） | 字段分布、用量统计、逐卡取值 |
| `资料/普查产出_1017/…/AbilityEffect.cs` · `AbilityTrigger.cs` | **全文** | 枚举全表（83 / 97） |
| `RawCardScript.cs` | **前 240 行**（字段区） | 字段表 + 行号 |
| `PlayerItem.cs` · `CardAbility.cs` · `AbilityLogic.cs` · `ActiveAbility.cs` · `CardAnim.cs` · `CardTraitData.cs` · `AnimInfo.cs` · `TargetCriteria.cs`（头 80 行） | **全文/头部** | 数据块结构 |
| `CardTypeOptions` / `SpellType` / `Keyword` / `CardArmy` / `CardRarity` / `CardExpansionType` / `Initiative` / `CardInventoryOptions` / `ShowInCollectionOptions` / `DamageType` / `TargetsAffected` / `PlayersAffected` / `AbilityContext` / `ActionPriority` / `PositionOption` / `BuffType` / `ArmyFilterOptions` / `StatFilterOptions` / `TargetSelectMethod` | **全文** | 枚举取值表 |
| `BattleManager.cs`（签名桩） | **只 grep `Add*` 方法名 + 行号** | 75 个 Add* 全表 |
| `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c` | **只 grep `BattleManager__Add*`** | 49 个直接被调用的 Add* |
| `资料/普查产出_1018/T14_guid_map重建与VFX重出_报告.md` | **全文 98 行** | GUID 表 / 覆盖率 / 生成器换源 |
| `资料/普查产出_第三十三会话/R10_卡牌打出后演出时间线.md` | **全文 155 行** | 逐卡演出参数样本 |
| `资料/AnimFX_实现与接线.md` | **全文 509 行** | AnimFX 18 类 / 数据链 / 11 钩子 / 声音 |
| `CardPresentation/Core/VfxMap.cs` | **1–130 行 + 350–470 行** | 事件名 / 解析优先级 / 运行时表形状 |
| `RuleEngine/Core/EffectResolver.cs` | **`EffectDispatch` 表 + `ResolveOneCore` 头部 + 方法签名** | 44 个动词、结算入口 |
| `RuleEngine/Core/WhenEvent.cs` · `BattleEvent.cs` | **常量/enum 列表 + 关键注释** | 22 种时机 / 16 种表现事件 |
| `RuleEngine/Core/CardDef.cs` | **`TalentName` / `KeywordTable` / `Implemented` 附近** | 关键词与天赋落点 |
| `数据/游戏数据/card_ids.json` · `官方本地化_en+zh-CN.json` | **程序化统计** | id 映射 / 本地化键族 |
| `工具/gen_cards_engine.py` · `gen_card_vfx_by_card.py` · `import_original_art.py` · `import_original_audio.py` · `_mine_allcards_fields.py` | **文件头** | checklist 的落点与数据流 |
| `CardArt.cs` · `CardText.cs` · `CardIcons.cs` · `Loc.cs` | **grep 关键段** | 立绘命名 / 本地化消费口 |

**没有重新挖全量反编译**（按简报红线）：只对枚举、`Add*` 清单做了 grep 级复核；
`PlayAbility` / `ResolveAction` 的逐条语义沿用 `P1`。

---

## 二、正本覆盖了什么

### 2·1 六项必答

| 简报要求 | 正本位置 | 覆盖情况 |
|---|---|---|
| ① id 怎么写 | §一 | ✅ 字段本体 / 两种形态（1,040 + 311）/ 13 前缀→阵营表（带张数与号段）/ 唯一性 / 新卡取号 5 条（**标明「我们的做法」**）/ `card_ids.json` 用法 / `cardShortId` |
| ② SO 怎么写 | §二 | ✅ 约 70 个字段逐条（分 A–E 五组，带「我们引擎读不读」与行号）；其余（评分/聊天/编排）按组说明；10 个枚举取值表 + 当前 build 分布；`CardAbility`/`AbilityLogic`/`TargetCriteria`/`CardTraitData`/`ActiveAbility` 结构块；5 个易错点 |
| ③ 引擎代码怎么接 | §三 | ✅ 五层总图；`AbilityTrigger` 97 值（60 个逐个给落点 + 37 个未用清单）；`AbilityEffect` 83 值（57 个逐个给「分发行 → Add* → 我们 verb」+ 26 个未用清单）；`BattleManager.Add*` 75 个族级表；基础通用动作「两面照」；关键词三形态；三条扩展路径 |
| ④ 动画/特效怎么连 | §四 | ✅ 原版链（SO → CardAnim → animInfo.animAdressable → prefab）；四个逐卡字段 + `cardAnimations` + 使用条数；两张 GUID 索引；16 个我们侧事件名；`VfxMap` 解析优先级 + 运行时表形状；AnimFX 模块层；接新卡 6 步；**§4·7 一张卡（SOR36）的全链实例** |
| ⑤ 本地化 | §五 | ✅ 10 个键族（含 `Card_Name` 1,198 / `Card_Description` 1,258 / `Warlord_Lore` 1,157 / `Chat_Messages` 1,736）；6 个覆盖字段（`refNameId` 等）+ 现读非空数；已验证的覆盖行为；4 种卡面记号；工程落点 |
| ⑥ 新卡 checklist | §六 | ✅ 10 步，每步写落点文件/工具；末尾说明「需要新行为时先按 §3·7 加动词/关键词/事件」 |

### 2·2 统计口径（全部现算，来源 `cards_raw.json`）

- 卡数 **1,351**；`cardAbilities` 非空 1,205；`abilityLogic` 总数 **2,077**。
- `AbilityTrigger` 用到 **60 / 97**；`AbilityEffect` 用到 **57 / 83**。
- `BattleManager.Add*` **75 个**；`PlayAbility` 直接调用 **49 个**。
- `cardAnimations` 引用：`cardAbilities[]` 内 **1,482 条**（1,468 有 GUID）+ `activeAbilityLogic[]` **249 条**（161 有 GUID）。
- 前缀号 **1,040** / hex **311** / 其它 **0**；前缀 13 个，号段见正本 §1·2。
- 表现字段非空张数：`cardImageReference` 1,328 · `summonAudio` 602 · `customRangedAttackParticles` 540 ·
  `attackAudio` 66 · `deathAudio` 68 · `customDeathParticles` 72 · `remnantBodyPrefab` 74 ·
  `whileInPlayAnim` 41 · `summonAnimDelayed` 1 · `targetedSpellAnim`/`nonTargetedSpellAnim` 0/0。

---

## 三、没查清的部分（逐条）

> 与正本 §七·2 同源；这里补充「还差什么」。

1. **`ResolveAction` 92 个 case 只核了约 15 个** —— 差：逐 case 读 `BattleManager__ResolveAction.c`（9,300 行，建议按需读）。
2. **`PlayAbility` 83 个枚举没有逐条核完** —— 差：正本 §3·3 标 ⚠️/❌ 的那些值逐条读分支。
3. **`CardEffect` 完整枚举没在签名桩里找到** —— 差：可能嵌套在 `CardEffect` 内部；需从 `dump.cs` 的类型段或方法体反推。
4. **`CardTraitCollection` 资产本体没在本地找到** —— 差：「trait → 图标/动画」的实际绑定值。
5. **128 个 `CardTrait` SO 资产没盘** —— 差：`_hasInPlayAnim` / `_stackable` / `_traitIcon` 实际值。
6. **`activeAbilityTextReferenceId` 的语义** —— 3 条样本（`SOR30_new` / `AM68` / `SOR25_new`）
   **同时命中 `Card_Description/*` 与 `Card_Abilities/*`**（最初误判为「查不到」，已订正）；
   「它覆盖哪一段文本」**未坐实**。另：`cardDescriptionTextReferenceId` 44 条里 **`UM6` 在表里查不到**
   （教程卡），原因未查清。
7. **`customIntroReferenceId` 键族** —— 候选 `Chat_Messages/<id>-Chat0`；未坐实。
8. **`cardShortId` 运行时用途** —— 861 个不同值、436 张为 0；反编译里没找到按它分发的证据。
9. **37 个未用 trigger 的落点** —— 正本只给了清单，没给每个的发射点。
10. **`alternativeAbility` 在规则里的实际执行链** —— 字段存在（`RawCardScript.cs:203`），样本与消费点未核。
11. **AI 评分字段公式** —— 属对手 AI 线，本正本只标「我们引擎不读」。
12. **`ShowTraits` 与 `CardTraits` 的差异** —— 「仅展示」是推断，未读消费点。
13. **`DefinedTrait` 134 个值只列了 ~24 个常用值** —— 全表在 `DefinedTrait.cs`，做关键词手册（K04）时要补全。

---

## 四、顺手发现（不属于本件，建议分流）

1. 🔴 **`资料/任务线/README.md:95` 这一行已过期** —— 它还写着本线「K06 池内 114 张旧 id 重编号，大迁移待拍板」、
   「K01/K03 收口 ⇒ 剩 K06 + K07」，而 **K06/K07 2026-10-18 已完成**，且本线目标已改成知识线。
   ⚠️ 该文件**不在本代理白名单**，请主对话更新（或由用户决定）。
2. **`资料/普查产出_1017/卡牌ID/R_SO与触发代码链_可行性.md:18` 仍写「49 个 `BattleManager__Add*`」** ——
   那是历史报告，`P1:341` 已订正为 75。**别回改历史报告**（它会丢当时的上下文）；
   正本已按 75 写，并在 §3·4 注明「49 是 `PlayAbility` 直接调用的子集」。
3. **`工具/_mine_allcards_fields.py` 的文件头仍指向旧快照**（`cards_1195_2022时代.json`、1195 张）——
   它现在被 K02（SO 导出器固化）沿用的话，需要换到 `cards_raw.json` 的 116 字段形状。
4. **`工具/gen_card_vfx_by_card.py:11` 附近写「我们的卡池（1126 张」** —— 现读 1,127；属注释级过期（不影响运行）。
5. **`cardShortId` 不是全 0**：861 个不同值、436 张为 0（初看单张卡容易误判成「全 0」）。
   正本已按现读写，未赋予语义。
6. **`Card_Description`（1,258）比 `Card_Name`（1,198）多 60 条** —— 可能是变体/能力文案，未追。
7. **本线文件夹名仍是 `卡牌ID逆推线`**，但线名已改「卡牌制作知识线」——
   重命名会牵动 `README.md`/其它线指针；**本代理未动**，建议主对话统一（或保留旧目录名 + 标题改新名）。
8. **`CLAUDE.md` §四 与本线有关的指针**（若有）也会随目标变化需要订正 —— 不在白名单，未动。

---

## 五、改动清单（只列本代理写过的文件）

| 文件 | 动作 |
|---|---|
| `资料/卡牌制作_正本.md` | 新建（约 870 行） |
| `资料/任务线/卡牌ID逆推线/任务总表.md` | 重写（目标/状态/未完成/常驻口径/已完成/规矩） |
| `资料/任务线/卡牌ID逆推线/准则与做法.md` | 重写（线定义/判据入口/六步流程/别推翻/坑/形成文件/边界） |
| `资料/任务线/卡牌ID逆推线/任务/K09_三张表补全.md` | 新建（K09 的「还差什么、怎么核」） |
| `资料/普查产出_1018/R_卡牌制作正本_建法.md` | 本报告 |

**确认未碰**：任何 `.cs` / `.py` / `.json` 数据、`CLAUDE.md`、`项目任务.md`、其它线的任务文件、`d:/2/**`、git。

---

## 六、建议的下一步

1. 主对话更新 `资料/任务线/README.md:95` 与 `CLAUDE.md` 里本线的指针（本代理无权限）。
2. 按 `任务总表.md` §一 排 K04（参数 schema）→ K09（三张表）→ K10（校验器）→ K11（样板库）→ K12（未查清清零）。
3. 做新卡时**先走正本 §六 checklist**；遇到「正本没写」的字段，先补正本再写卡（铁律 5·b）。
