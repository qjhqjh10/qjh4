# B121-DATA 子报告 — 卡牌数据链（代码已会读、但数据没有 / 恒空 / 生成器不写）

- 会话日期：2026-11-03 · 代理：B121-B（只读普查，卡牌数据链）
- 现读口径：`cards_engine.json` **count = 1,129** · `cards_truth.json` **1,351** · 卡 SO 真值 = `资料/普查产出_1018/allcards_1350/cards_raw.json`（`full_raw` 逐字段）
- 代码面：`Core/CardDef.cs`(9,616) · `Core/CardDefActiveAbility.cs` · `Data/CardDatabase.cs`(5,088) + 4 个 partial · 生成器 `工具/gen_cards_engine.py`(9,218)
- ⚠️ 本文只写子报告；未改任何源码/数据/生成器/账。

## 一、总表

| 编号 | 功能 | 类别 | 代码落点(文件:行) | 缺什么数据 | 当前行为 | 原版判据(有/无 + 出处) | 补数据来源 | 优先级 |
|---|---|---|---|---|---|---|---|---|
| DATA-01 | 治疗量（`AbilityLogic.healCriteria`） | A | `RuleEngine/Core/EffectResolver.cs:3722` `DoHeal`（只读 `EffectText` 正文） | `healCriteria` 的 `minValue`/`maxValue`/`valueOptions` 三格；卡表**没有任何 heal 列** | 只按卡面文字解析「heal N」；结构化数值不进引擎 | **有**：`AbilityLogic.healCriteria // +0x48`（`d:/2/tools/il2cpp_out/dump.cs` AbilityLogic 段）；池内 **84 行**非零（`cards_raw.json` 的 `full_raw`；`cards_truth.json` `abilities[].logic.heal` = 98 行） | 加 `abilityHeal[{ab,lg,min,max,opt}]`，源 = `cards_truth.json` 的 `abilities[].logic.heal`；生成器照 `ability_damage_of()`（`gen_cards_engine.py:1754`）的同形写法 | P0 |
| DATA-02 | 加费（`AbilityLogic.manaBuff`，仅 `chosenAbility==60 buffMana`） | A | 无任何卡表列；`EffectResolver.DoEnergy` 走正文 | `manaAmount`/`createMana`/`refillMana`/`spiritStone`/`faith`/`questPoints` 六格 | 正文「gain N」解析 | **有**：`AbilityLogic.manaBuff // +0x68`；**只有 `ca==60` 才消费**（`decomp_full/AbilityLogic__PlayAbility.c:686`）；池内 **51 行**（全表 128 行里其余 `ca` 行原版不读） | `cards_truth.json` 的 `abilities[].logic.manaBuff`（按 `ca==60` 过滤；别把 128 行全落） | P1 |
| DATA-03 | `AbilityLogic.playersAffected(+0x20)`（普通 ability 路） | A/D | `RuleEngine/Core/CardDef.cs:3403` 只有注释；`EffectResolver` 全树 0 读取口 | 池内 **367 行**非零（Self 331 / Enemy 36） | 由 op 目标 + 正文隐式覆盖（未验证是否等价） | **有**：`AbilityLogic.playersAffected // +0x20`；用法未逐句读（字段名在反编译里已丢，只有偏移） | 先回源确认 `PlayAbility.c` 对 `+0x20` 的分派，再决定是否落列 | P1 |
| DATA-04 | 进攻卡的 `cardAbilities` 旁表已与当前 build 不符 | A（数据陈旧） | `RuleEngine/Core/OffensiveCardData.cs:109` · `Data/OffensiveCardMap.cs` | 表来自 **2022 快照**（`cards_1195_2022时代.json`），写 `trigger==15`；**当前 build** 的 EC64/DA62 是 `trigger==0`、各 1 条 logic | `ThirdCardAbilities` 用旧快照判定 | 当前 build 真值在 `cards_raw.json`（EC64/DA62 各 1 ability / trigger 0） | 重生成 = 换源到 `cards_raw.json`；同时补 `desc`（见详情 3） | P1 |
| DATA-05 | `ai_scoring.json` 的 `untilEndOfTurn` 是 **int**，DTO 是 **bool** | C（产物/契约不一致） | `RuleEngine/Data/AiScoringData.cs:160`（`bool`）· `AiScoring.cs:650` | 118 处 `"untilEndOfTurn":0`（现读）；生成器 `gen_ai_scoring.py:149-150` **已会转 bool**，只是**产物没重生成** | Unity `JsonUtility` 宽容所以线上能跑；`ruleprobe` 的 `System.Text.Json` 会抛 | 原版 `CardEffect.untilEndOfTurn // +0x32` 是 bool（`dump.cs` CardEffect 段） | 重跑 `gen_ai_scoring.py`（产物落 bool） | P1 |
| DATA-06 | `cards_truth.json` 缺 `abilityTriggers` 列 | C（工具真值缺口） | 自检/工具消费者：`Editor/*_Assert.cs`、`工具/criteria_census.py` 等 | 1351 张的 `abilityTriggers` 只存于 `cards_engine.json` | 任何「这张卡有没有某个 trigger」的真值判据只能读 `cards_engine` | 原版 `RawCardScript.cardAbilities[].trigger` | `gen_cards_truth.py` 加一列（它已在读 `cardAbilities`） | P2 |
| DATA-07 | `Cards.abilityTargets` 只有 `trigger`/`ta`，无 `playersAffected`（= DATA-03 的落列形状） | A | `RuleEngine/Data/CardDatabase.cs:396`（`AbilityTargetDto`） | 见 DATA-03 | 见 DATA-03 | 见 DATA-03 | 见 DATA-03 | P1 |
| DATA-08 | `specialAttackScoring` 全 0（1,129/1,129） | B | `SimpleAI.cs:1533` `HasSpecialAttackScoring` | 列存在、恒 0 | 机制恒假 | **原版也全 0**：1,351 张 `specialAttackScoringCriteria` 全 0（`cards_raw.json` `full_raw`；`CardDatabase.cs:63` 同结论） | 无需补；列保留 | — |
| DATA-09 | `eventSpriteName` 现池全空 | B/D | `CardDatabase.cs:278`（`[Obsolete]` 死列） | 1,129/1,129 空 | 无消费者 | **`RawCardScript` 根本没有这个字段**（`cards_raw.json` 的 `full_raw` 116 键里 0 命中）⇒ 这条列是历史误建 | 建议删列或改注为「原版无此字段」 | P3 |
| DATA-10 | `activeAbilityPsychicCost` / `activeAbilityPlasmaCost` 全 0 | B | `CardDatabase.cs:648-649` · `ManaBuffDeadBranch_B116W41.cs` | 1,129/1,129 = 0 | 灵能/等离子付费闸恒不放行 | **原版也全 0**：143 张有 `activeAbility` 的卡 `psychicCost`/`plasmaCost` 全 0（`cards_truth.json`）；且 `psyker`/`reactor` 池内 0 张 | 无需补 | — |
| DATA-11 | `comboCard` / `comboCardFirst`（AI 连招） | B | `AiScoring.cs:976-1020` `ShouldPlayComboCard`（实参传入，DTO 上没有这两列） | 无列 | 常返回 false（`comboCard==null`） | **原版也休眠**：1,351 张 `ScoringCriteria.comboCard.m_PathID=0`、`comboCardFirst=0`（`cards_raw.json` 逐卡扫） | 数据出现后才需要加列 | — |
| DATA-12 | 开局加成三列 `initialHandModifier`/`Mana`/`SpiritStone` | B | `CardDatabase.cs:114-118` · `CardDef.cs:429` | JSON **没有这三个键**（生成器「非 0 才落」） | 读进来 = 0（合法值） | **原版也全 0**：1,351 张全 0 | 无需补 | — |
| DATA-13 | `quest`/`questTimes`/`activeNeedsLoading`/`alternativeNeedsLoading` | B | `CardDatabase.cs:571-595` | JSON 没有这四个键（非 0 才落） | 读进来 = 0；`questTimes>1` 恒假 | **原版也全 0**（`gen_cards_engine.py:9088` 自陈，本次复算 1,351 张全 0） | 无需补 | — |
| DATA-14 | `refNameId` 现池全空 | B | `CardDatabase.cs:256`（`[Obsolete]`） | 1,129/1,129 空 | 无消费者 | 原版有值但**只在 70 张 GUID 卡上**，那些卡没进池（`CardDatabase.cs:254-255`；本次核对 `cards_raw.json` 有该字段） | 无 | — |
| DATA-15 | 技能面板的 `Card_Abilities/<id>` 键 | B/C | `CardPresentation/Battle/SkillPanel.cs:2167-2187` | 132 张有主动能力的池内卡里 **46 张原版就没有这个键**；`-load` 后缀键源表 **0 条** | 落到 🔶 自己设计的「退回裸描述」档（`SkillPanel.cs:2173`，2026-10-27 裁定） | **原版也缺**：源表 `数据/游戏数据/官方本地化_13lang.json` 里 `Card_Abilities/` 只有 120 条，`Card_Abilities/ASH1` 等 46 个键不存在 | 无需补（原版缺键时会印键名，我们不照抄是已记录的 🔶） | — |
| DATA-16 | `AbilityEffectTextRowDto.isHand` 恒 false | ✅ 不是缺口 | `CardDatabase.cs:4946-4982` | JSON 没有 `isHand` 键 | **已由 `at` 推导**（`ResolveIsHand`），38 行全部可读 | 原版 `buffEffect` / `handBuff.cardEffect` 两格 | 无（注释已写死口径） | — |
| DATA-17 | 文档与代码不符：`TriggerValue` 注释说「主路还没写」 | C（文档） | `CardDef.cs:9229` 注释 vs `CardDatabase.cs:4917` 实现 | — | 实际**已映射**（`triggerValue` 1,622 行全部落地） | 原版 `CardAbility.triggerValue // +0x20` | 就地改注释（铁律 5） | P3 |

## 二、重点条目详情

**1) DATA-01 治疗量（最值得补的一条）**
- 原版 `AbilityLogic__PlayAbility.c` 在 `case 20 (doHeal)` 读 `+0x48` 的 `healCriteria`（`minValue`/`maxValue`/`valueOptions`）。
- 池内 **84 行**非零，例：`UM23 Bladeguard Lieutenant`「Codex: Heals 3」`heal 3/3`、`TL40 Infestation Node` `heal 2/2`、`SAU37 Orikan the Diviner`（trigger 570）`heal 1/1`。
- 我们的 `DoHeal` 只吃 `EffectOp.Amount/AmountMax`，那两个值来自 `Core/EffectText.cs` 的正则解析。**行为可能对，但依据是文本不是数据**，且 `valueOptions`（随机/选择档）根本没有载体。

**2) DATA-02 加费（`ca==60`）**
- 关键坑：`manaBuff` 在 `full_raw` 里**每个 logic 行都有一份**，但原版**只在 `ca==60` 时读**（`PlayAbility.c:686` 的 `case 0x3c`）。全表 128 行非零，过滤到 `ca==60` 后 **51 行**。⛔ 别全落 —— 那会给 77 行「伤害/抽牌」凭空加出一次加费。
- 六格语义：`createMana` / `refillMana` / `spiritStone` / `faith` / `questPoints` 是 `BattleManager__ProcessManaBuff.c:27-49` 的五个分派口；`psychicMana`/`plasmaMana` 两个 flag 原版**不读**（`ManaBuffDeadBranch_B116W41.cs` 已照抄成死支）。

**3) DATA-04 进攻卡旁表陈旧（并顺手发现一条过期注释）**
- `offensive_card_abilities.json` 的 note 写「卡面正文不在本地 ⇒ 本表没有 desc」——**已过期**：官方 13 语表里 `Card_Description/DA62`、`Card_Description/EC64` **都在**（en 值 = "When a player plays a third card…"）。
- 同一份表写「trigger==15 全快照只有 2 张」，当前 build 的 EC64/DA62 是 `trigger==0` ⇒ **源换了、表没换**。消费者 `RuleEngine/Core/PlayedThirdCardInTurn.cs:64` / `BattleDriver.cs:40845` 会按旧快照判定。

**4) DATA-05 `untilEndOfTurn` 类型不一致**
- `ai_scoring.json` 现读 **118 处 `"untilEndOfTurn":0`**（仍是 int）；`AiScoringData.UntilEndOfTurn` 是 `bool`。
- 生成器 `gen_ai_scoring.py:149-150` 已经有 `ce['untilEndOfTurn'] = bool(...)` ⇒ **只差重跑**。Unity 的 `JsonUtility` 宽容（所以 `AiScoring.cs:650` 的 `!ce.untilEndOfTurn` 今天行为正确），但 `ruleprobe` 的 `System.Text.Json` 会直接抛。

**5) DATA-03 `playersAffected` —— 诚实标注「未查透」**
- 池内 367 行非零；`CardDef.cs:3403` 明确写「它不参与主动能力的行↔op 配对」。
- 但**普通 ability 路**有没有读它：`grep -rn "playersAffected" RuleEngine/Core` 只命中注释与 `ActiveAbilityLogicSpec.PlayersAffected` 一处，`EffectResolver` 0 命中 ⇒ 今天**没消费**。
- ⛔ 不能直接判「原版也不读」：字段在 `dump.cs` 确实存在（`+0x20`），只是反编译里字段名丢了。⇒ 判 **D**：要读 `PlayAbility.c` 里 `+0x20` 的分派才能定。

## 三、其他核实结论（都不算缺口）

- `abilityEffectTexts`：原版池内 31 张有 `effectText`，我们落 31 张 —— **齐**。`effectName` 池内只有 `TL47`/`TAU51` 两张且值就是卡 id，真正的键在 `effectText` ⇒ **不缺**。
- `relatedCard1..4`：原版 212/36/12/6 条；`related_cards.json` 191 张卡 / 239 条边 / skipped 14 → 缺口只在池外卡，**正常**。
- `startingCardInHand` 3 → 我们 2；`goSecondCardInHand` 64 → 我们 56：差额全是池外卡（命中率随池子），**不是缺列**。
- `animTimes`：真值 919 张 / 1,482 条 → 我们 875 张 / 1,469 条，差额为池外卡。
- `unitConditions`：442 行 / 10 个 role，`count` 99 行**全部带 `countSet`**（v16 缺口已闭合）。`mult`(31) 与 `multi`(9) 也已落地；只有 `multiCountCriteria` 的 `countOptions/triggerIf/referenceValue` 三格没落（全池 1 张 `UM77`，生成器 `:2665` 已记账）。
- `specialAttackScoring` / `comboCard` / `initial*Modifier` / `quest*` / `chanceOfHappening`（2 张且都在池外）/`psychicCost`/`plasmaCost`：**原版全 0 / 全空**。
- 本地化：池内 1,129 张缺 `Card_Name` **2**（两个附魔模板）、缺 `Card_Description` **4**（`ASH61`/`TL61` 的 `desc` 本身为空 + 两个附魔模板）⇒ **无实际缺口**。`Card_Trait` 那 7 个「缺失」是 camelCase 键名（`longrange`/`frenzied`/`rage`/`bloodThirst`/`cantAttack`…）匹配问题，**实际都在**。
- `transformedWarlordId` / `deckCriteria`：`RuleCorePartial_B96CB.cs:206-275` 已在读、两列 1,351 张全空 ⇒ **归 B121-DATA 的 E03，本报告不展开**。
- `animDurations`：全仓 0 引用，**没有代码读它**。

---

## 四、主代理 `B121-DATA` 回源订正（2026-11-03）

1. **DATA-01（治疗量）降为 P1**：现读池内 84 行非零里，`minValue == maxValue` 的有 **80 行**、`valueOptions == 0` 的有 **82 行**
   ⇒ 正文「heal N」与结构化值**大概率等价**；真正会失真的只有 4 行区间 + 2 行特殊档。仍是 A（该补），但不是 P0。
2. **DATA-04 的「两张卡」现读都不在可玩池**：`EC64` / `DA62` 均不在 `cards_engine.json` 的 1,129 张里
   ⇒ 旧旁表即使重生成，这条链今天仍是**休眠**（主清单判 B/C，不判「今天影响对局」）。
3. **DATA-03（`playersAffected`）保持 D**：主代理复核 `grep playersAffected RuleEngine/Core` 只命中注释与
   `ActiveAbilityLogicSpec.PlayersAffected`，普通 ability 路确实 0 消费口；原版 `+0x20` 用法未读到 ⇒ 先读 `.c` 再定。
4. **`enchantment` 列的「缺 9 张」不成立**（本报告未这样写，但引擎子报告写了）：现读全表 130 行 PPtr 里
   只有 115 行 `buffType ∈ {12,15,16}`，那 9 张全在剩下 15 行里 ⇒ 生成器剔除正确。⛔ 别补。
