# B121-DATA · 「已经有代码、但还没有设计对应数据」清单（战斗相关）

> 只读普查 · 2026-11-03 · 代理 `B121-DATA`（主）＋ 三路只读代理 `B121-A`（引擎）/ `B121-B`（卡牌数据链）/ `B121-C`（表现与配置）
> 子报告：`报告/B121_DATA_子报告_引擎.md` · `报告/B121_DATA_子报告_卡牌数据链.md` · `报告/B121_DATA_子报告_表现与配置.md`
> 口径：**功能有代码、但配套数据（卡表/配置表/SO/JSON/本地化/资源）没有设计、没有落列、或落了但全空/无消费端**。
> 本报告**只读**：未改源码/数据/场景，未跑 Unity，未动 git，未写活状态表/账/正本。
> 所有「池内 N 张」均**现读** `Assets/RuleEngine/Resources/cards_engine.json`（v47 · `count=1129` · 2026-11-02）复核；SO 真值读
> `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `full_raw`（⛔ 不用 `cards_raw.json` 的 `cardAbilities` 解析视图，会与 `full_raw` 双份翻倍）。

## 一、一句话结论

战斗域里「代码先行、数据未到」的条目共 **31 条**：**A 真缺口 10 条 · B 原版也休眠 14 条 · C 自设计/产物 6 条 · D 判据查不到 1 条**。
其中**真正今天就在影响对局**的只有三类：① 附魔模板的 `ca` 执行器只覆盖 30/40/50/340（138 行里 75 行无执行器，其中 73 行无人跑；含 39 张进攻环境卡的 44 行）；
② `ta=0 (any)` 有 **224 行 / 209 张卡**，但代码注释误判成「池内 0 张」而**没接线**；③ 卡表缺 `healCriteria`（84 行）与 `manaBuff`（51 行）两列。其余多数是「原版数据本身也是空的」——机制已建、等卡（铁律 11 照做，但今天零可观测）。

## 二、主表

| 编号 | 功能 | 类别 | 代码落点(文件:行) | 缺什么数据 | 当前行为 | 原版判据(有/无 + 出处) | 补数据来源 | 优先级 |
|---|---|---|---|---|---|---|---|---|
| B121-01 | 附魔模板的 `ca` 执行器（`chosenAbility` 分派） | **A** | `Core/EnchantmentExecutor_B115.cs:77-109`（只接 `ca 30/40/50/340`）+ `:109` `NoteEnchantRowUnsupported` | 模板 138 行 logic 里 **75 行无执行器**（其中 2 行由 `Pray`/`DA42` 手写等价物承担，**73 行无人跑**）：`ca 10` 30 行、`ca 0` 12、`ca 60` 10、`ca 130` 4、`ca 20` 4、`ca 70` 2、`ca 13` 2、`ca 100` 2、`ca 236` 2、`185/320/15/140/238` 各 1 | 挂上去的附魔**大部分不结算**（只出声一行 unsupported） | **有**：`decomp_full/AbilityLogic__PlayAbility.c:286` 起的 `chosenAbility` 巨型 if 链，逐行都跑 | 数据已在 `enchantment_templates.json`；补执行器分支即可（判据逐条在 `.c`） | **P0** |
| B121-02 | 进攻环境卡 39 张的效果落地 | **A** | `Battle/BattleDriver.cs:37465`（督军触发）→ `Core/Enchantment.cs:1830` `RunGrantedEnvAbility`；模板链见 B121-01 | 37 个模板 / 71 行 logic 里 **44 行 `ca` 无执行器**（`ca 0` 10 行、`10` 20、`20` 2、`60` 6、`130` 4、`236` 2）；另有 trigger `15/641/280` 是否有广播待核 | 换环境（雾/环境光/prefab）照做，**环境效果不结算** | **有**：`BattleManager__ApplyEnvEffect.c:14-15` → `heroCard.TriggerOtherCardAbility(envCard)`；`CardScript__AddEffect.c:826-828` | 数据齐（`offensive_env_cards.json` 39 张 + 37 模板）；只补执行器 | **P0** |
| B121-03 | `ta = 0 (any)` 多目标表 | **A 判据错** | `Core/RuleCore.cs:17919` 注释称「`0 any` 池内 0 张 ⇒ 不接线」；多目标建池只接 `{10,20,30,110,140,150}` | **实测 224 行 / 209 张卡** 有 `abilityTargets[].ta == [0]` | 这 209 张全部回落文本单目标路（未崩，但理由不成立） | **有**：`decomp_full/AbilityLogic__GetTargets.c:884-891` —— `targetsAffected==0` ⇒ `GetUnitsInPlayList(..., hero 1, minions 1, heroParts 1, weapons 1, remnants 1, player 1, enemy 1, dying 0)` = **双方全场单位** | 先逐 case 复核这 224 行是否真由 `GetTargets` 消费（部分 logic 自带目标）；确认后接 `ta=0` 分支 | **P0 复核** |
| B121-04 | 治疗量 `AbilityLogic.healCriteria` | **A** | `Core/EffectResolver.cs:3722` `DoHeal`（只吃 `EffectOp.Amount`，来源是 `EffectText` 正文正则） | 卡表**没有任何 heal 列**；池内 **84 行**非零（`min!=max` 4 行、`valueOptions!=0` 2 行） | 80 行与正文等价；4 行区间/2 行特殊档会失真 | **有**：`AbilityLogic.healCriteria // +0x48`（`dump.cs`）；`PlayAbility.c` case 20 | 加 `abilityHeal[{ab,lg,min,max,opt}]`，源 = `cards_raw` 的 `full_raw.cardAbilities[].abilityLogic[].healCriteria` | P1 |
| B121-05 | 加费 `AbilityLogic.manaBuff`（仅 `ca==60`） | **A** | 无卡表列；`EffectResolver.DoEnergy` 走正文 | 池内 **51 行**（全表 128 行里其余 77 行原版不读，⛔ 别全落） | 正文「gain N」解析；`createMana/refillMana/spiritStone/faith/questPoints` 五格无载体 | **有**：`PlayAbility.c:686`（`case 0x3c`）；`BattleManager__ProcessManaBuff.c:27-49` | 加列，按 `ca==60` 过滤落 | P1 |
| B121-06 | `TargetCriteria.spellType` / `conditionsFilter` | **A** | `Core/CardCriteria.cs:109,118` 两格已就位；`FromTarget`（`:164-185`）**不赋值** | `abilityTargetCriteria` 里有 `spellType`（10 行非零）但**没进 `CardCriteria`**；`conditionsFilter` **整列不存在**（真值 135 行非零） | 两维恒 `-1` = 不筛 | **有**：`FilterMethods__CheckIfMeetsCriteria.c:286-308` + `MeetsConditionCriteria.c:384-389` | 生成器落两列 + `FromTarget` 赋值 | P1 |
| B121-07 | `AbilityLogic.playersAffected` | **A/D** | `Core/CardDef.cs:3403` 只有注释；`EffectResolver` 全树 0 读取口 | 池内 **367 行**非零（Self 331 / Enemy 36） | 由 op 目标隐式覆盖，未验证等价 | 字段存在（`+0x20`），但用法未逐句读（字段名在反编译里已丢）⇒ **D** | 先读 `PlayAbility.c` 对 `+0x20` 的分派再决定落不落 | P1 |
| B121-08 | 原版音效 cue 覆盖 | **A/D** | `WarpforgeVFX/Runtime/WFSoundBank.cs:43-50`（三表合一） | 610 个 cue 资产里 **73 个不在任何表**；其中 9 条 `Trait *`（7 条对应 trait 有特效 prefab） | 这 73 条不响；调用方未知 | **有**：资产在 `bundle_soundcollection_assets_all/MonoBehaviour`（现数 610） | 按 `m_Script` PathID 反查 `AudioCue` 持有者后再补；⛔ 别凭名字补 | P1 |
| B121-09 | `cantBeRandomlyCreated` 的消费 | **A/B** | `Core/EffectResolver.cs:4767-4795`（`choiceResolution` 全池回落分支） | 池内 **167 张 = 1**；但注入闸 `CardDef.HasSpecificCardChoice` 关着 ⇒ 整段不可达 | 值在、路不通 | **有**：`AbilityLogic.GetTargets` 的 `choiceResolution` 分支 | 放开 `HasSpecificCardChoice` 前先逐张核「选中那张真的被消费」 | P2 |
| B121-10 | 进攻卡旁表 `offensive_card_abilities.json` | **A 数据陈旧** | `Core/OffensiveCardData.cs:109`；消费 `PlayedThirdCardInTurn.cs:64` / `BattleDriver.cs:40845` | 表来自 **2022 快照**（1195 张、`trigger==15`）；当前 build 的 `EC64`/`DA62` 是 `trigger==0`，且两张都不在可玩池 | 第三张牌判定按旧快照 | 当前 build 真值在 `cards_raw.json` | 重生成改源；但两张卡不在池 ⇒ 今天仍休眠 | P2 |
| B121-11 | **战将/督军变身 `CheckForHeroDeckTransform`（E03，用户点名）** | **B** | `Core/RuleCorePartial_B96CB.cs:164-320`（写口）· `:358` `CampaignHeroTransformTargetId`（恒 null）· `:372` `CampaignHeroTransformDeckMeets`（恒 null）· 调用点 `Core/RuleCore.cs:237-238` | 卡表缺两列：`transformedWarlordId`（SO **0/1351 非空**）· `deckCriteria`（**1351/1351 有结构体，但 `countCriteria.targetsAffected` 全 0** = 原版早退「无条件满足」） | **恒不换**（在判据前就 return false，只喊一次日志） | **有**：`BattleManager__CheckForHeroDeckTransform.c`（101 行全文）· `SetupBoardPhase.c:56/66` · 谓词 `hero.uniqueId == deckWarlord.transformedWarlordId` | 原版 SO 本身没有值 ⇒ **不是我们能补的数据**；若做 `E03f` 新卡则数据要自设计（见 B121-25） | **P0（记录）** |
| B121-12 | 23 个已实现/已登记 trait 全池 0 张 | **B** | `Core/CardDef.cs:8058`（`Implemented`）/ `:8686`（`Prefixes`）· `Core/UnitStatePartial_B117DROPPOD.cs`（dropPod）· `Data/SimpleAI.cs:637,654,689` | 按 `keywords`/`cardTraits` 两列 **0 张**：`jam` `transient` `psyker` `reactor` `massive` `poisonous` `sacrifice` `markerlight` `survivor` `resistant` `poisoned` `droppod` `ward` `wardwb` `tainted` `summonrift` `coward` `precognition` `requiem` `giantkiller` `noncombatant` `pindown` `mutation`（`unstoppable`/`oathdouble` 各 1 张） | 闸/结算分支全在，入口 0 ⇒ 零可观测 | **有**：逐词判据在 `CardDef`/`RuleCore` doc；原版 SO 同样 0 | 无（原版也没有）；出现带该词的卡即自动可达 | P1（机制已建） |
| B121-13 | 触发器 155 / 550 / 590 / 640 / 175 | **B** | `Core/RuleCorePartial_B94B21.cs:71,150` · `EffectResolver.cs:18332`(590) · `:8211`(175) · `:16838`(640) | `abilityTriggers` 现读集合里 **无 155 / 550 / 590 / 640 / 175**（640 只存在于附魔模板 17 行） | 发射点/接收点已建，可玩池无监听者 | **有**：`BroadcastTurnSetup.c` · `ResolveUnitSummoned.c:46-60` · `TriggeredEarnSkull.c:7`；原版 SO 也 0 | 无（原版也没有） | P1 |
| B121-14 | `spellType 70 Trap` / `120 Quest` / `15` | **B** | `EffectResolver.cs:17028`(70) · `RuleCorePartial_B94B8.cs:10`(120) · `ActionPump.cs:1856`(15) | `spellType` 现读值域 `{0,90,150,180,190,210,220,230,240,260,265,270}` ⇒ **无 15/70/120/200** | 三条机制恒不触发 | **有**：`BroadcastQuestResolved.c` 等；原版 SO 也 0 | 无 | P2 |
| B121-15 | `buffType 9 setHealth` / `7 skipCardDraw` | **B** | `EffectResolver.cs:15108` · 卡表 `skipDraws` 段 | 现读 `buffType` 值域 `{1,2,3,4,8,12,15,16,17,25,70,90,95,100}` ⇒ **无 7 / 无 9** | 休眠 | **有**：`AddEffect.c:794-820`；原版 SO 也无 | 无 | P2 |
| B121-16 | `chosenAbility 71 / 85 / 350 / 370 / 400` | **B** | `EffectResolverPartial_B88E2.cs:303` · `TransformUnit_B94B12.cs:430` · `EffectResolver.cs:19559` · `:18785` | 现读 `chosenAbility` 集合里**无这五档** | 休眠件，0 调用点 | **有**：各方法 doc；原版 SO 也无 | 无 | P2 |
| B121-17 | 先手判据 `FirstSeatRules.ByInitiative` | **B/?** | `Core/FirstSeatRules.cs:77,87` | `initiative` **1129/1129 = 20**（源表 `card_initiative.json` 自写 `byValue {20:1127}`） | 全池同档 ⇒ 永不产生分叉 | 枚举有 0/10/20/30/40，但当前 build 快照全 20；**抽取字段是否正确未核** | 需 VA 复核 `RawCardScript.initiative` 偏移；不是缺表 | P1 复核 |
| B121-18 | AI 特殊攻击打分 `specialAttackScoring` | **B** | `Data/SimpleAI.cs:660` · `Data/AiScoring.cs:863` | 1129/1129 = 0；`ai_scoring.json` 只有元数据 | 恒 0 | **有字段无真值**：1,351 张全 0 | 无 | P2 |
| B121-19 | AI 连招 `comboCard` / `comboCardFirst` | **B** | `Data/AiScoring.cs:976-1020` | 1,351 张 `m_PathID=0` / 0 | 常返回 false | **有**：字段存在、原版全空 | 无 | P2 |
| B121-20 | 灵能/等离子付费 `psychicCost` / `plasmaCost` | **B** | `Data/CardDatabase.cs:648` · `Core/ManaBuffDeadBranch_B116W41.cs` | 143 张主动能力卡全 0；且 `psyker`/`reactor` 池内 0 张 | 闸恒不放行 | **有**：原版全 0 | 无 | P2 |
| B121-21 | 结算战报三格 `BattleEndPlayerData` | **B/D** | `Battle/EndPanel.cs:640-660` | **宿主 prefab 本地零实例**（`assets_full` 91 包 24.7 万文件按类名+五字段名全搜，只有 `MonoScript_3980.json` 类注册） | 未做；我方副标题是自加 | **有**：`BattleEndPlayerData__Setup.c` 方法体齐（`troopsText/tacticsText/damageText`，值取 `MatchResultData+0x48` 的 `+0x10/14/18`） | 需找到 prefab 或真机；离线不可得 | P2 |
| B121-22 | 战役脚本 `CampaignScriptRunner` | **B** | `Battle/CampaignScriptRunner.cs:1-45` | `WarlordPathData` ⊂ 服务端 LiveOps 下发（远端） | 执行器 + `NullCampaignScriptHost` 出声兜底 | 原版数据在远端（已按 3 个词二进制搜 `D:/2/关服前存档`，0 命中） | 需远端；离线不可得 | P2 |
| B121-23 | 单位聊天引用 `chatRef` / `introRef` | **B** | `Battle/UnitChatPanel.cs:1644` | **0/1351 非空**（亲核 `cards_raw`）；真源是 `customIntroChatData`（57 张 → `card_chat_intro.json`） | 恒等于卡 id（走兜底） | 原版两字段也全空 | 无 | P3 |
| B121-24 | 攻击/阵亡聊天气泡文本 | **B** | `Battle/BattleDriver.cs:7580-7600` | `voice_lines.json` attack 0/56、death 0/56 带 text | 只放音、不弹泡（2026-10-24 已照原版改） | 原版也空 | 无 | — |
| B121-25 | `E03f` 每阵营「变身战术卡」（用户 2026-11-03 裁定要做） | **C** | 复用 `Core/RuleCorePartial_B96CB.cs:380` `SwapWarlordCard`；卡池/加牌闸见 `E03g` | 新卡数据全缺：卡 id / 效果文字 / 本地化 / 插图复用 / 卡种 / 可进池不可进组牌 | 未开始 | 原版**没有**「打出卡变身」⇒ 这是我们的新功能（标 `🔶 用户 2026-11-03 裁定`） | 用户裁定 + 生成器新卡 + 本地化 + 美术 | P1 |
| B121-26 | 卡体动画「一帧裕量」`fps<=0 ⇒ 60` | **C** | `Battle/Card3DAnimationController.cs:409-416` | 无数据缺；**判据冲突待裁**：原版出厂 `GameStaticData.FPSLimit 2 ⇒ targetFrameRate -1` | 裕量按 1/60 s；照抄 -1 会让 **881/1067（82.6%）≤1 s** 的卡体动画当帧收尾 | **有**：`GameStaticData__.cctor.c:212`；`Card3DAnimationController` 的 `FrameSlackExpired` 已逐字照抄公式 | 需调度台裁「照抄 -1 还是保 60」 | P1 待裁 |
| B121-27 | `ai_scoring.json` 的 `untilEndOfTurn` 类型 | **C 产物** | `Data/AiScoringData.cs:160`（bool）· `AiScoring.cs:650` | 现读 **118 处 `"untilEndOfTurn":0`**（int） | Unity `JsonUtility` 宽容可跑；`ruleprobe` 的 `System.Text.Json` 会抛 | 原版 `CardEffect.untilEndOfTurn` 是 bool | 重跑 `gen_ai_scoring.py`（`:149-150` 已会转 bool） | P1 |
| B121-28 | `cards_truth.json` 缺 `abilityTriggers` 列 | **C 工具真值** | 自检/工具消费者 | 1,351 张的 trigger 集合只存于 `cards_engine.json` | 任何「这张卡有没有某 trigger」的真值只能读 `cards_engine` | 原版 `cardAbilities[].trigger` | `gen_cards_truth.py` 加一列 | P2 |
| B121-29 | `misc_clips.json` 11 条 legacy clip / `animDurations` | **C 反向** | `Resources/misc_clips.json` · `animDurations` 全仓 0 引用 | 数据在、**无消费者** | 不播 / 不用 | 原版多为孤儿（全库 0 处 pathID 引用） | 需要时接；`animDurations` 建议删列或标死数据 | P3 |
| B121-30 | 远程序列语音开关 / LiveOps 事件实例 | **C 🔶** | `BattleDriver.cs:35410-35418`（`UnityRemoteConfigData` 本地无）· `:39740-39775` | 远端 RemoteConfig / 事件实例 | 离线等价 `true` / 取不到就出声早退 | 服务器（关服） | 服务器类自设计（铁律 18·0 允许），标 `🔶` | P3 |
| B121-31 | 音效 `Trait *` 9 条 cue 的持有者 | **D** | 表缺 73 条（见 B121-08） | 「cue → 调用方」双向对账未做；`CardTrait`/`TraitEffect` 都无音效字段 | 无法判断原版是否使用 | 资产在；持有者未定位 | 按 `m_Script` PathID 反查 `AudioCue`；⛔ 别凭名字补 | P2 |

> 说明：**B 类不是「不用做」** —— 铁律 11 的判据是「机制已建、数据一变自动可达」，列出是为了让复刻百分比别把它们算成「没做」。
> 其中 `B121-11`（E03）是用户点名那条，机制与写口**已完整落地**，缺的只有原版 SO 上那两列值。

## 三、重点条目详情

### B121-01 / B121-02：附魔模板的 `ca` 执行器只覆盖 1/4（今天影响对局的 A 缺口）
- 链：原版 `ApplyEnvEffect`（`BattleManager__ApplyEnvEffect.c:14-15`）让督军按 `ThisCardPlayed` 跑环境卡能力 →
  `CardScript__AddEffect.c:826-828` 把模板挂到载体 → 之后每次触发由 `RawCardScript__TriggerAbilities.c:49-197` 逐件派发 →
  `AbilityLogic__PlayAbility.c:286` 起按 `chosenAbility` 分派**每一行 logic**。
- 我们：挂载已通（`Enchantment.cs:1830 RunGrantedEnvAbility`，载体 120 友方督军 / 130 敌方督军 / 20 thisCard 三档已对齐），
  派发已通（`EffectResolverPartial_B109E6.cs:69` + `RuleCore.BroadcastEarnedSkull`），**但最后一步执行器只接了 `ca 30/40/50/340`**
  （`EnchantmentExecutor_B115.cs:77-109`）。
- 数据面（现读 `enchantment_templates.json`，86 张 / 138 行）：`ca 10` 30 行、`ca 0` 12、`ca 60` 10、`ca 130` 4、`ca 20` 4、
  `ca 70` 2、`ca 13` 2、`ca 100` 2、`ca 236` 2、`185/320/15/140/238` 各 1 ⇒ **75 行无执行器**
  （其中 2 行是 `Pray`/`DA42`，由手写等价物承担；其余 **73 行无人跑**）。
  进攻环境卡那 37 个模板 / 71 行里 **44 行**无执行器（`ca 0` 10 行、`10` 20、`20` 2、`60` 6、`130` 4、`236` 2），只有 27 行落在已接的三档。
- 后果：换环境（雾/环境光/prefab）是**看得见**的，但「每回合 10% 全体 3 伤」这类**规则效果不结算**。
  `offensiveCardEnabled` 现读默认 `true`（`BattleDriver.cs:9244`）⇒ 玩家会选中一张环境卡却只看到换天。
- 补法：按同一份 `.c` 把 `ca 0/10/20/60/70/130/236` 等分支补进执行器（⛔ 别另开第二套 DTO，行数据已在 `abilityBuffs` 旁表）。

### B121-03：`ta=0 (any)` —— 注释写「池内 0 张」，实测 224 行 / 209 张
- 注释原文 `RuleCore.cs:17919`：「其余 `ta`（含 `0 any` —— **池内 0 张**、原版休眠，裁定 ④ 不接线）」。
- 现读 `cards_engine.json`：`abilityTargets[].ta == [0]` **224 行 / 209 张**（样例 `AM1` `AM2` `AM3` `AM10` `AM15` `ASH55` … 多为督军 Talent/Duty 与战术卡）。
- 原版：`AbilityLogic__GetTargets.c:884-891` 的 `if (targetsAffected == 0)` ⇒
  `GetUnitsInPlayList(ref list, clear 0, inclHero 1, inclMinions 1, inclHeroParts 1, inclWeapons 1, inclRemnants 1, inclPlayer 1, inclEnemy 1, includeDying 0)`
  —— 语义 = **双方全场的单位**（不是「不可达」）。
- 我们：多目标建池只接 `{10,20,30,110,140,150}`，`ta=0` 返回 `null` ⇒ 调用方退回文本单目标路。
- 待复核点：这 224 行是否每一行都真的由 `GetTargets` 消费（有些 logic 自带 `targetCard`）。**不能直接判「209 张全错」**，
  但「不接线的理由」必须重写；这是本清单里**唯一一条判据被现读数直接推翻**的条目。

### B121-11：E03 战将变身的准确状态（用户点名）
- 代码做了什么：`CheckForHeroDeckTransform` 按原版四跳（`deckWarlord` 空 → `DeckMeetsCriteria` → 按 `transformedWarlordId` 找英雄 →
  记录原督军 + 换督军）实现，**只换督军、一张牌库都不动**（`B120-W30` 已订正旧语义）；写口 `SwapWarlordCard`（`:380`）会重建督军单位并补 `Exhausted/PlayOrder/生命修正`。
- 缺哪一列：`CardDef` **没有** `TransformedWarlordId` / `DeckCriteria` 两个字段，`cards_engine.json` 也没有这两列；
  SO 真值里 `transformedWarlordId` **0/1351 非空**、`deckCriteria` **1351/1351 有结构体但计数全 0**。
- 补上后会怎样：两个取值口（`:358` / `:372`）一返回真值，整条链**不用再改**就会跑。
- 不补的后果：**所有对局都不会变身**——但这也正是原版当前 build 的状态（原版同样休眠），所以是 B 不是 A。
- 注意：`deckCriteria` **不是空**，是「默认档 = 无条件满足」；下个会话别去「补 criteria」，要补的是 **id**。
- 表现侧 `E03c`：`BattleDriver.SetTransformedWarlordNewChats`（`:35422`）+ `ChatPopupPanel.SetTransformedWarlordNewChats`（`:1455`）
  **方法体与夹具都在**，但生产调用者 0 —— 因为上面恒不成功。E03 数据一旦有值，这里自动接上。

### B121-12：dropPod 0xe6 与 23 个 trait（机制已建、池内 0 张）
- 原版 `ActivateTraitsOnSummonOrEnchantment.c:102-135` 的 `0xe6` 支已逐句复刻到 `UnitStatePartial_B117DROPPOD.cs`（`ApplyDropPodOnSummon`），
  4 条接线零行数落地，+28 断言。
- 现读 `cards_engine.json` 的 `keywords` + `cardTraits`：`droppod` **0 张**（`desc` 里也没有）；同族 23 个词里
  `markerlight` 只在 15 张卡的 `desc` 文本里作为**引用词**出现（不是 trait），`poisoned` 1 张同理 ⇒ 按 trait 列判仍是 0。
- 结论：不是「没做」，是「等卡」。⛔ 别把 `desc` 里的引用词当成 trait 已落地。

### B121-21：结算战报三格（`BattleEndPlayerData`）
- 原版判据齐：`BattleEndPlayerData__Setup.c` —— `GetTranslation(词条) + ": " + 数值`，顺序 TroopsDead → Tactics → DamageDone，
  值取 `MatchResultData + 0x48` 的 `+0x10/+0x14/+0x18`。
- 缺的是**宿主 prefab**：91 个包 / 24.7 万文件按类名与 `troopsText/tacticsText/damageText/quoteText/cantLoseRankText` 全搜，
  只命中 `globalgamemanagers/MonoScript/MonoScript_3980.json` 的类注册；AssetRipper 导出工程按脚本 guid 反查 prefab/scene 也 0 引用。
- 现在结算面板的副标题（`N 回合 敌方战将最低生命 X`）**是我们自加的**，原版没有这个形状。
- 补上后会怎样：结算窗多三行统计；不补的后果 = 少三行。

### B121-26：帧率默认档（唯一「照抄原版会坏」的条目）
- 原版：`GameStaticData.FPSLimit` 出厂默认 **2 ⇒ `Application.targetFrameRate = -1`**（`GameStaticData__.cctor.c:212`）。
- 我们：`fps <= 0 ⇒ 60`（`Card3DAnimationController.cs:415`，标 `🔶 自己设计`）。
- 冲突：`FrameSlackExpired` 的公式已逐字照抄（`1/fps + remaining < 0`）；fps=-1 时它=「remaining<1s 即收尾」，
  而 `cardanim_clips.json` 1067 条里 **881 条 ≤1.0 s** ⇒ 82.6% 的卡体动画会当帧结束。
- 需要调度台裁：照抄 -1（视觉上大量动画被吃掉）还是保留 60（与公式原文不一致）。`B120-Q07b` 真机探针可给运行值。

## 四、查过但【不是缺口】（防止下个会话重复劳动）

| 疑似缺口 | 现读结论 |
|---|---|
| 附魔 `enchantment` 列只有 54 张、真值 63 张（代理初判「缺 9 张」） | **不是缺口**：那 9 张（`ASH43 ASH46 BL49 GOF104 GOF31 GOF41 GOF90 SAU52 UM82`）在真值里 `buffEffect.buffType` 是 `None`/`3`，而原版 `CardScript__AddEffect.c` 只有 `case 0xc/0xf` 进 `AddEnchantment` ⇒ 生成器剔除是**对的**（全表 15 行非挂载，池内那 9 张的 9 行都在其中） |
| `normalAbilityBoardCriteria` / `hasAlternativeAbility` 没落列 | **不是缺口**：稀疏列（只落非默认值）。池内 `hasAlternativeAbility=1` 只有 `SW21` 一张，`normalAbilityBoardCriteria` 非默认 2 张 —— 与真值完全一致 |
| `deckCriteria` 全空 | **措辞错**：1351/1351 有结构体，只是计数全 0（= 原版早退「满足」）。要补的是 `transformedWarlordId` |
| `Carnifex` / `TL20` 选项卡（`B-329` / `ENG-12`） | **已闭合**：`choice_option_cards.json` 57 张已落，`TL20.choicesSpecificCardIds` 3 个 id + `ta=180` + `maxTargets=3` 现读齐；执行器 `ChoiceOptionCards_B116W32.cs` 已接 |
| 音效表 508/610（`B120_F:27` / `B120_W5:144`） | **旧数过期**：加上 `battle_sounds.json` 的 24 条后现读 **537/610，缺 73** |
| `Rank_Skull_0` 本地没有 / `ShowJustOne` 没传 `army` | **注释过期**：`Resources/Art/ui_menu/Rank_Skull_0.png` 已导入（`EndPanel.cs:1419` 在用）；`BattleDriver.cs:15362` 已传 army |
| `card_icon_plan.json` 只 876 张 | **不是缺**：只收含方括号 token 的卡；其余 253 张 `desc` 里 `[` 出现 0 次 |
| 本地化卡名/描述缺 | **无实际缺口**：池内缺 `Card_Name` 2（附魔模板）、`Card_Description` 4（本就无 desc）；`Card_Trait` 那 7 个是 camelCase 键名匹配问题，实际都在 |
| `cards_truth` 的 `isHand` 恒 false | **设计如此**：由 `at` 经 `ResolveIsHand` 推导，38 行全可读 |
| `specialAttackScoring` / `comboCard` / `initial*` / `quest*` / `psychic+plasma` | **原版也全 0/全空**（见 B 类）；列保留即可 |
| `animDurations` | 全仓 0 引用 ⇒ **死数据**，不是「缺消费端」的待办 |

## 五、顺手发现（只报不改）

1. **代理初判的「附魔列缺 9 张」是假缺口**（见上表第一行）。判断依据：生成器 `工具/gen_cards_engine.py:4478` 的 `bt not in ENCHANTMENT_ATTACH_BUFFTYPES` 闸与原版 `AddEffect.c` 的 `case 0xc/0xf` 同语义。**下个会话别去补这 9 张。**
2. `RuleEngine/Core/Enchantment.cs:1826-1831` 那两段 doc 已过期：它写「`LoadEnchantmentTemplates` 没把 abilities 灌进 CardDef、`TriggerEnchantment` 只跑 0/500」——
   实际 `CardDatabase.cs:3729`（`B115_S5`，2026-11-01）已 `AttachAbilityTriggers` + `RecordEnchantmentAbilityBuffs`，`RunEnchantmentLogicRows` 也已存在。**真缺口是 `ca` 分支覆盖不全（B121-01），不是「没人跑」。**
3. `RuleEngine/Data/CardDatabase.cs:3977` 写「本表（进攻卡）今天只有数据、还没有消费者」—— 现读 `offensiveCardEnabled = true`、`ChosenOffensiveCardDef()` 已在生产调用（`BattleDriver.cs:37479`）⇒ **该句过期**。
4. `initiative` 全池 = 20（含源表自写 `byValue {20:1127}`）**可疑**：原版枚举 5 档、池内 1,129 张全同值，更像抽取错列而不是真值。建议 VA 核 `RawCardScript.initiative` 偏移（`B121-17`）。
5. `cards_engine.json` 没有 `alternativeAbility` 这一列（只有 `hasAlternativeAbility`），而 `activeAbility*` 有整套 —— 若将来要跑替代能力，是**整列**缺，不是一两格（今天 `SW21` 走 `alternativeAbility` 的动画支已有注释态，见 `AbilityAnimDurations.cs:62-72`）。
6. `EffectResolver.cs:16838` 的「17 张带 trigger 640 的卡」**是对的**：`cards_truth` 实测 17 行 / 12 张，全在环境卡（不在可玩池），且已在 `enchantment_templates.json` 里；缺的仍是 `ca` 执行器。
7. 池内 `type="enchantment"` 只有 **2** 张卡，但 `enchantment_templates.json` 有 **86** 张模板 —— 模板走旁表而不是池，这是**设计**（`CardDef` doc 已写死），别当缺口。

## 六、核实方式与搜索边界（供复核）

- 代码面：`Assets/RuleEngine/{Core,Data}/*.cs`、`Assets/CardPresentation/{Battle,Core,Hand,Board}/*.cs` 全量 `grep`：
  `池内 0` / `0 张可达` / `休眠` / `等数据` / `数据未接` / `恒 0` / `没有生产者` / `未接线` / `🔶 自己设计` / `查不到`。
- 数据面（全部现读）：`cards_engine.json`（v47 · 1129）· `cards_truth.json`（1351）· `cards_raw.json` 的 `full_raw`（1351）·
  `enchantment_templates.json`（86 模板 / 138 logic 行）· `offensive_env_cards.json`（39 张 / 41 logic 行）·
  `choice_option_cards.json`（57）· `ai_scoring.json` · `tutorial_stages.json` · `tutorial_decks.json` · `prebuilt_decks.json`。
- 原版面：`d:/2/tools/decomp_full/`（`AbilityLogic__GetTargets.c` · `AbilityLogic__PlayAbility.c` · `CardScript__AddEffect.c` ·
  `BattleManager__CheckForHeroDeckTransform.c` · `BoardAnalysis__DeckMeetsCriteria.c` · `BattleManager__ApplyEnvEffect.c` ·
  `RawCardScript__TriggerAbilities.c` · `BattleManager__BroadcastEarnedSkull.c`）· `d:/2/tools/out_cpp2il_dev_ilspy/` ·
  `d:/2/tools/out_il2csharp/` · `dump.cs` · `d:/2/新解包资源/assets_full/` · `d:/2/tools/assetripper_out/ExportedProject` ·
  `D:/2/关服前存档/安卓版本warpforge，包含远端资料/`。
- 资产计数：`assets_full/bundle_soundcollection_assets_all/MonoBehaviour` = **610** 个 cue（现数）。
- ⛔ 没做：未改任何源码/数据/场景/账；未跑 Unity；未动 git；未写活状态表。
