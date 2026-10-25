# AUD7 · 关键词/词条：引擎实现 × 操作层（玩家怎么发动）

> 只读审查 · 2026-10-25。路径约定：下表 `X.cs` 除注明 `Battle/` 者外均在 `D:/4/Unity/MyGame/Assets/RuleEngine/Core/`；`Battle/` = `Assets/CardPresentation/Battle/`。
> 判据优先级：`d:/2/tools/decomp_full/` > `d:/2/新解包资源/assets_full/` > 成品卡图（只量内容）。
> **操作层通用四件套**（下文只写差异）：① 卡面图标 `Core/Badges.cs`（74 张 `Art/traits/*.png`）；② hover 解释 `Core/Tooltip.cs:959` + `Resources/trait_tips.json`（62 条，其中官方 56 / 规则书 5 / 空 1）；③ 场上 7 格状态徽标 `Badges.cs:413 InPlayIconKeys`（47 键，判据 = `trait_collection.json` 的 `hasIcon∧isInPlayIcon∧traitIcon`）；④ 触发时徽标脉冲 `Core/CardView.cs:3965` + 状态框特效 `Core/TraitFrames.cs`（62 trait / 75 条效果）。
> **玩家能「主动按」的入口只有两个**：A = 选中单位后出现的**主动技能格**（`AttackKind.Ability`，`Battle/BattleDriver.cs:13898` 亮 / `:14898` 结算）；B = **点场上残骸格收集灵魂石**（`Battle/BattleDriver.cs:13858`）。ESC 取消选择（`Battle/BattleDriver.cs:11673`）；被拒统一走 `NotifyCantDoAction`（语音+文字，`:7401` → `HintForCode :15089`）。

## 0. 权威清单三源对账（差异本身是结论）

| 来源 | 条数 | 说明 |
|---|---|---|
| 我们 `KeywordTable`（`CardDef.cs:5894-6886` 常量 62 个 + `RuleCore.cs:4593/4632/4665/1230/1235/1242` 6 个 + `Prefixes`/`Implemented` 里的裸字面量） | ≈74 个规范键 | 含 9 个自定/近似词（`ability`/`talent`/`spiritstone`/`faith`/`quest`…） |
| 原版 `CardTrait` 资产真值（`Unity/数据/游戏数据/trait_collection.json`） | 77 | 原版卡面词条的正本；**`huntMark`/`flying`/`armour`/`markerlight`/`ambush` 的 `hasIcon=false`** |
| 官方本地化 `Card_Trait/*`（`官方本地化_en+zh-CN.json`） | 83 | 比 77 多 6 个历史/变体键（`*_old`、`prayAlt`…） |
| 规则书 61 关键词表（中文版 `:161`） | 61 | **粉丝实体版，只当旁证**（文件头写 `Not official`） |
| 差异 | — | 我们有而原版无：`ability`；原版有而我们完全没有：`combo` `support` `relentless` `resolution` `firstStrike` `large` `mekaniak` `daemonicAllegiance` `piercing` `sneak` `counterOffensive` `battleHonour` `courage` `chooseOne` `frontLine` `mesmerize` `notDestroyRemnant` `oathDouble/InAllTurns/TripleActivation` `dontReturnFerocity`；**`penumbra` 是查无此词**（原版全树 0 命中） |

## 1. 主表（一类一行，别名合并；「0 卡」= 卡池 1127 张现读 0 张带它）

| # | 词条（中/英） | 类 | 引擎实现（状态 + file:line） | 玩家怎么发动 | 操作层表现 | 与原版差异 | 修法 |
|---|---|---|---|---|---|---|---|
| 1 | 先锋 vanguard | 常驻 | 完整 · `RuleCore.cs:4952` 目标闸 | 自动 | 卡面图标+tooltip（原版 inplay=false，无徽标） | 一致 | — |
| 2 | 飞行 flying | 常驻 | 完整 · `RuleCore.cs:5085`；`Battle/FlyingCardController.cs:40` | 自动 | 卡面+飞起动画+tooltip | 一致 | — |
| 3 | 潜行 stealth | 常驻(1 回合/攻击后失效) | 完整 · `RuleCore.cs:1961,4945,5821` | 自动 | 徽标+tooltip+日志 | 一致 | — |
| 4 | 护甲 armour X | 常驻 | 完整 · `RuleCore.cs:6962 DamageAfterArmourOne` | 自动 | 卡面+tooltip（无徽标，同原版） | 一致 | — |
| 5 | 护盾 shield | 常驻(一次性) | 完整 · `RuleCore.cs:6805` 挡下 / `:7816` 消耗 | 自动 | 徽标+tooltip | 一致 | — |
| 6 | 闪避 dodge | 常驻(一次性) | 完整 · `RuleCore.cs:6807,7799` | 自动 | **无图标、tooltip 正文空**；0 卡 | 原版 `trait_collection` 无此条目 | 补图标+tooltip 只在有卡时再做，先记账 |
| 7 | 无法攻击 can't attack | 常驻 | 完整 · `RuleCore.cs:4599` | 自动 | 徽标+tooltip | 一致 | — |
| 8 | 远射 long range | 常驻 | 完整 · `CounterAttack.cs:60` | 自动 | 徽标+tooltip | 一致 | — |
| 9 | 潜行攻击 sneak attack | 常驻 | 完整 · `CounterAttack.cs:45`（免反击第①道） | 自动 | **无徽标/无 tooltip**；0 卡 | 原版 inplay=true、有图标 | 加入 `InPlayIconKeys`+tooltip |
| 10 | 恐吓 terror | 常驻 | 完整 · `CounterAttack.cs:51` | 自动 | **无**；0 卡（官方 `Card_Trait/terror` 0 条） | 隐藏 trait | 记账即可 |
| 11 | 狙击 sniper | 常驻 | 完整 · `RuleCore.cs:6487` + `CounterAttack.cs:64` | 自动 | 徽标+tooltip | 一致 | — |
| 12 | 巨人杀手 giant killer | 常驻 | 完整 · `RuleCore.cs:5105` | 自动 | 无图标/tooltip；0 卡 | 原版无 CardTrait 资产 | 有卡时补 tooltip |
| 13 | 无敌 invulnerable | 常驻 | 完整 · `RuleCore.cs:6808` | 自动 | 徽标+tooltip | 一致 | — |
| 14 | 脆弱 vulnerable X | 常驻 | 完整 · `RuleCore.cs:6754` | 自动 | 徽标+tooltip | 一致 | — |
| 15 | 失明 blind | 状态(1 回合) | 完整 · `RuleCore.cs:4547` 远程=0；到期 `:2502` | 自动 | 徽标+tooltip+状态框 | 一致 | — |
| 16 | 眩晕 stun | 状态 | 完整 · `RuleCore.cs:5255` 等五道 `ErrStunned`；到期 `:2502` | 自动 | 徽标+tooltip+状态框 | 一致 | — |
| 17 | 压制 pin down | 常驻 | 完整 · `RuleCore.cs:5265` | 自动 | 徽标+tooltip | 一致 | — |
| 18 | 嗜血 blood thirst | 常驻 | 完整 · `RuleCore.cs:5259` 攻击配额 | 自动（第 2 次攻击照常点） | 徽标+tooltip | 一致 | — |
| 19 | 再生 regeneration X | 触发(回合末) | 完整 · `RuleCore.cs:2351` | 自动 | 徽标+tooltip | 一致 | — |
| 20 | 兽群 pack | 常驻 | 完整 · `RuleCore.cs:4557`（`FieldAttack` 内） | 自动 | 卡面+tooltip（无徽标，同原版） | 一致 | — |
| 21 | 标记光 markerlight X | 常驻 | 完整 · `RuleCore.cs:6548` | 自动 | 卡面文字（原版 hasIcon=false） | 一致 | — |
| 22 | 哨戒 sentry X | 触发(被攻击) | 完整 · `RuleCore.cs:6072` 攻击第 0 步 | 自动 | 徽标+tooltip | 一致 | — |
| 23 | 星镖 shuriken X | 触发(攻击前) | 完整 · `RuleCore.cs:6097` | 自动 | 徽标+tooltip | 一致 | — |
| 24 | 爆裂 blast X | 触发(攻击后) | 完整 · `RuleCore.cs:6411` | 自动 | 徽标+tooltip | 一致 | — |
| 25 | 震荡 concussive/concussion | 触发(攻击命中) | 完整 · `RuleCore.cs:6580` | 自动 | 徽标(concussive)+tooltip | 拼写差异已在 `Badges.cs:250` 归一 | — |
| 26 | 践踏 stomp | 触发(打死目标) | 完整 · `RuleCore.cs:6448` | 自动 | 徽标+tooltip | 一致 | — |
| 27 | 迅捷 fast | 常驻(部署当回合可动) | 完整 · `RuleCore.cs:3208` | 自动 | 卡面+tooltip | 一致 | — |
| 28 | 侧翼 flank | 常驻(部署当回合可动) | 完整 · `RuleCore.cs:3208` | 自动 | 卡面+tooltip | 一致 | — |
| 29 | 临时 ephemeral | 处置(回合末移除) | 完整 · `RuleCore.cs:2689 SweepEphemeral` | 自动 | 卡面+tooltip | 一致 | — |
| 30 | 残骸 remnant | 触发(死亡) | 完整 · `RuleCore.cs:1393` 建 / `:8663 DestroyRemnants` | 自动 | 徽标+残骸美术+`RemnantSfx` | 一致 | — |
| 31 | 路标石 waystone | 触发+**主动** | 完整 · `RuleCore.cs:9073` 生成 / `:11365 CanCollectWaystone` / `CollectWaystone` | **点场上残骸格**（`Battle/BattleDriver.cs:13858`） | 徽标+点击高亮+HUD 灵魂石计数+`VfxMap.CollectWaystone` | 一致（判据 `BattleManager__CanUseWaystone.c`） | — |
| 32 | 灵魂石 spirit stone | 资源 | 完整 · `RuleCore.cs:3789 ResolveSpiritAbility`（付费见 `EffectResolver.cs:8347`） | **自动**：打出时够就扣（无询问） | HUD 计数 `Battle/BattleDriver.cs:10649`+tooltip | 一致（用户 2026-09-19 口径） | — |
| 33 | 不稳定 unstable | 触发(死亡) | 完整 · `RuleCore.cs:9243` | 自动 | 徽标 | 一致 | — |
| 34 | 中毒 poisoned | 触发(自己回合末) | 完整 · `RuleCore.cs:2409` | 自动 | 无；0 卡 | 原版无 CardTrait 资产 | 有卡时补 |
| 35 | 毒素抗性 resistant | 常驻(只豁免毒) | 完整 · `RuleCore.cs:2410` | 自动 | 无；0 卡 | 同上 | 同上 |
| 36 | 幸存者 survivor | 触发(致命伤) | 完整 · `RuleCore.cs:7062` 判死闸 / `:8909` 触发 | 自动 | 无；0 卡 | 同上 | 同上 |
| 37 | 献祭 sacrifice | 触发(幸存者支路) | 完整 · `RuleCore.cs:8809` | 自动 | 无；0 卡 | 同上 | 同上 |
| 38 | 堡垒 bastion | 常驻(整份吃伤) | 完整 · `RuleCore.cs:8061`（`ApplyDamage` 内） | 自动 | 无；0 卡 | 同上 | 同上 |
| 39 | 空降舱 drop pod | 常驻(独立血池) | 完整 · `RuleCore.cs:7924` 结算池 / `:7160` 预览 | 自动 | 无；0 卡 | 同上 | 同上 |
| 40 | 守卫 ward / wardWb | 常驻(异侧不可选) | 完整 · `RuleCore.cs:10513,10547`；`EffectResolver.cs:2608` | 自动 | **无**；0 卡 | **未进 `Implemented`** ⇒ 有卡会白打 `*` | `CardDef.cs:6889` 的 `Implemented` 里补 `Ward`/`WardWb`（常量现只在 `ConditionEval.cs:55-57`） |
| 41 | 污秽 tainted | 常驻(目标闸) | 完整 · `RuleCore.cs:10533`；`ConditionEval.cs:537` | 自动 | 无；0 卡 | 未进 `Implemented` | 同上 |
| 42 | 召唤裂隙 summon rift | 常驻(不可点名) | 完整 · `RuleCore.cs:10521` | 自动 | 无；0 卡 | 未进 `Implemented` | 同上 |
| 43 | 干扰 jam | **部分** | `RuleCore.cs:5602` 替身跳过 / `ConditionEval.cs:501` / `HandEffectExpired:9901` | 自动 | 无；0 卡 | 官方语义「取消所有卡牌能力与主动效果」，我们只做 3 处 | 按 `jam` 全量落点普查后补 |
| 44 | 非战斗人员 noncombatant | 常驻 | 完整 · `RuleCore.cs:4599`（与 cantAttack 同口） | 自动 | 无；0 卡 | 一致 | — |
| 45 | 免晕 unstunnable | 常驻 | 完整 · `RuleCore.cs:4680 StunBlockedByTraits`（消费点 `:6599`） | 自动 | 无；0 卡 | 一致 | — |
| 46 | 不可阻挡 unstoppable | 常驻 | 完整 · `RuleCore.cs:4966` | 自动 | 无图标；池内 1 张（印刷档） | 原版 inplay=true 但无图标资产 | 记账 |
| 47 | 哨兵 sentinel | 触发(回合开始不抽牌) | 完整 · `RuleCore.cs:1230` | 自动 | 无；0 卡 | 一致 | — |
| 48 | 狂暴 berzerk | 触发(回合末强制攻击) | 完整 · `RuleCore.cs:1235` | 自动 | 无；0 卡 | 一致 | — |
| 49 | 维护费 maintenance | 触发(回合开始扣能) | 完整 · `RuleCore.cs:1242` | 自动 | 无；0 卡 | 一致 | — |
| 50 | 集结 rally | 触发 | 完整 · `RuleCore.cs:3816`（从手牌打出） | 自动 | 卡面+tooltip（无徽标） | 一致 | — |
| 51 | 猛击 strike | 触发(攻击后存活) | 完整 · `RuleCore.cs:6236` | 自动 | 徽标+tooltip | 一致 | — |
| 52 | 斩杀 slay | 触发(击杀后存活) | 完整 · `RuleCore.cs:5738` | 自动 | 徽标+tooltip | 一致 | — |
| 53 | 反噬 backlash | 触发(死亡) | 完整 · `RuleCore.cs:9231` | 自动 | 徽标+tooltip | 一致 | — |
| 54 | 忏悔 penitence（=卡面 `Rage`） | 触发(受伤未死) | 完整 · `RuleCore.cs:8317`；`Prefixes` `CardDef.cs:7377` 把 `rage` 归一到这里 | 自动 | 徽标(rage)+tooltip | 一致（官方 `Card_Trait/rage`=`Penitence`） | — |
| 55 | 双重忏悔 doubleRage | 触发(再触发一次) | 完整 · `RuleCore.cs:8358` | 自动 | 无 | 0 卡 | 记账 |
| 56 | 暴群 mob | 触发(友方近战攻击后) | 完整 · `RuleCore.cs:6253` | 自动 | 徽标+tooltip | 一致 | — |
| 57 | 团 regiment | 触发(友方远程攻击后) | 完整 · `RuleCore.cs:6275` | 自动 | 徽标+tooltip | 一致 | — |
| 58 | 残忍 cruelty | 触发(敌方受伤未死) | 完整 · `RuleCore.cs:8411` | 自动 | 徽标+tooltip | 一致 | — |
| 59 | 狂喜 ecstasy X | 触发(生命≤X 未死，一次) | 完整 · `RuleCore.cs:8394` + `CardDef.cs:1530 EcstasyX` | 自动 | 徽标+tooltip | 一致（运行时授予的不触发，同原版） | — |
| 60 | 毁灭者 destroyer（=原版 `frenzied`） | 常驻(目标选择) | 完整 · `RuleCore.cs:4926` | 自动 | 徽标(frenzied)+tooltip | 一致 | — |
| 61 | 巧技 artifice | 触发(每次打出战术) | 完整 · `EffectResolver.cs:2447` | 自动 | 徽标+tooltip | 一致 | — |
| 62 | 激励 stimulation | 触发(被战术选中，结算前) | 完整 · `EffectResolver.cs:2329` | 自动 | 徽标+tooltip | 一致 | — |
| 63 | 突触 synapse | 触发(被友方战术选中) | 完整 · `EffectResolver.cs:2419 RepeatTacticOnAdjacent` | 自动 | 徽标+tooltip | 一致 | — |
| 64 | 圣典 codex | 触发(能量归零) | 完整 · `RuleCore.cs:3854` | 自动（玩家只需正常出牌花光能量） | 徽标+tooltip | 一致 | — |
| 65 | 伏击 ambush | 触发 | 完整 · `RuleCore.cs:3438 PlaysFaceDown`；翻开只在受伤/出手 | **自动**：满足条件即面朝下；**无「是否伏击」选择** | 面朝下图+对手侧整排徽标隐藏（`Badges.cs:14`） | 一致（`W58` 已删「到期自动揭示」） | — |
| 66 | 传送 teleport | 触发(当回合抽到即打出) | 完整 · `RuleCore.cs:3828` | 自动 | 卡面+tooltip | 一致 | — |
| 67 | 起义 uprising | 触发(之后部署部队) | 完整 · `RuleCore.cs:3762` | 自动 | 卡面+tooltip | 一致 | — |
| 68 | 潮涌 tide X | 触发(打出时造复制) | 完整 · `RuleCore.cs:3745` | 自动 | 卡面+tooltip | 一致 | — |
| 69 | 伴生 companion X | 触发(打出时造一张) | 完整 · `RuleCore.cs:3752 PlayCompanions` | 自动（原版也无「玩家选」，`W4` 已订正） | 卡面+tooltip | 一致 | — |
| 70 | 天赋 talent | 触发(回合开始塞一张) | 完整 · `RuleCore.cs:2160` → `:12366 SpawnTalents` | 自动 | 卡面+tooltip | 一致 | — |
| 71 | 虫群 swarm | 触发(打出在同名右侧) | 完整 · `RuleCore.cs:4134 TrySwarmMerge` | 自动（落点即发动） | 徽标+合并动画+合并音（`W` 已修） | 一致 | — |
| 72 | 空降 landing | 触发(开空降舱) | 完整 · `RuleCore.cs:5959` | 自动 | 无；0 卡 | 原版 `landing=770` 卡面词 | 记账 |
| 73 | 出手前 about to attack | 触发(取消这一记) | 完整 · `RuleCore.cs:5864` | 自动 | 无；0 卡 | 同上 | 记账 |
| 74 | 花光法力 spent all mana | 触发 | 完整 · `RuleCore.cs:11966` | 自动 | 无；0 卡 | 同上 | 记账 |
| 75 | 猎杀标记 hunt mark | 触发(被标记单位死亡) | 完整 · `RuleCore.cs:8961`；criteria `TargetCriteriaEval.cs:772 case 1260 → "hunt mark"`（已修） | 自动 | 卡面文字+tooltip(rulebook 兜底)+触发脉冲 | 一致（原版 hasIcon=false，无徽标） | — |
| 76 | **职责 duty** | **主动** | 完整 · `RuleCore.cs:11083` 可判 / `:11148 DutyUsed` / 本体 `UseAlternativeCore:11095` | **A**：选中单位 → 主动技能格 →（正文要目标则点一格） | 按钮文字「职责：<正文>」（`Battle/BattleDriver.cs:14059`）+被拒提示「本局已用过」+ESC 取消 | 一致（`DutyUsed` 判据已收进 `CanUseAlternative`，无硬编码残留） | — |
| 77 | **祈祷 pray** | **主动** | 完整 · `RuleCore.cs:11206 Prayed=true`；清：`:2057` 任何回合开始 / `:8185` 挨伤 / `:2882` 督军疲劳 | **A**，同上 | 同上 + 「正在祈祷」状态可被 `PrayedOnly` 条件读到 | 「+1 信仰」只在 `alternativeTraitLogic≠0` 才发（`EffectResolver.cs:11805`）。**2026-10-25 查实：该值 = LiveOps 事件模式开关，本地无序列化值；0 = Classic 基线，且「下一回合开始 +1 信仰」在原版代码里根本不存在** ⇒ 不是缺陷，详见 §七·二 | 只改注释口径（`EffectResolver.cs:11797` 那句「今天不可达」→「Alt 事件模式专用」），⛔ 不改值 |
| 78 | **狂暴 ferocity** | **主动** | 完整 · `RuleCore.cs:11227` 用后洗回牌库 | **A**，同上 | 同上 + `Ferocity_RecallToDeck` 粒子（`BattleScene.cs:11069` 断言） | 一致 | — |
| 79 | **议程 agenda** | **主动** | 完整 · `RuleCore.cs:11034 AlternativeActions` + `UseAlternativeCore` | **A**，同上 | 同上（正文多为 `Gain 1 Quest Point`） | 一致 | — |
| 80 | 誓言 oath X | **主动** | 完整 · `RuleCore.cs:10845 CanUseOathAbility` / `UseOathAbility`；正文 `CardDef.OathOps` | **A**（优先级 alt → oath → `Ability:`，`Battle/BattleDriver.cs:14898`） | 按钮文字 = `OathOps[0].Source`；付不起时被拒出声 | 一致 | — |
| 81 | 技能 ability（自定） | 主动 | 完整 · `RuleCore.UseAbility` | **A** | 同上 | **我们自定、原版无此词** | 存量卡迁完后删除 |
| 82 | 信仰 faith | 资源+阈值 | 完整 · `EffectResolver.cs:8526 ResolveFaithAbility`（阈值不扣，`:11590`） | 自动（打出时 `faith≥N`） | HUD 信仰计数 `Battle/BattleDriver.cs:10658`+tooltip | 一致 | — |
| 83 | 任务点 quest / questPoints | 资源 | 完整 · `EffectResolver.cs:998,11600` | 自动 | HUD 任务点 `Battle/BattleDriver.cs:26566`+tooltip | 一致 | — |
| 84 | 黑暗契约 dark pact（四变体 markOfKhorne/Nurgle/Slaanesh/Tzeentch） | 触发(施加) | 完整 · `EffectResolver.cs:11192 GrantDarkPact`；计数 `:10507`；识别 `CreatePool.cs:1136` | 自动 | 卡面 `markOfChaos` 徽标+tooltip | 部分：四变体的祝福由**附魔模板**给（`EffectResolver.cs:1807`），与「关键词」不是同一层 | 判据逐条核一次再定 |
| 85 | 破坏 sabotage | 卡类非单位机制 | 按 subtype 归类 `CreatePool.cs:1383`；造牌广播 `EffectResolver.cs:3785` | 自动 | 卡面+tooltip | 一致（原版也是 subtype） | — |
| 86 | 灵能 psychic | **非词条** | subtype `Psychic Power` + 选牌池 `CreatePool.cs:1418`；`initialPsychic` 是死功能（`CardDef.cs:351`） | 自动（选牌 op） | tooltip | 一致 | — |
| 87 | 反击 counterattack | **非词条** | 官方键是 `Card_Trait/counterOffensive`（后手特殊卡）；`CounterAttack.cs` 是**反击伤害**、不是这个词条 | — | — | 该词条我们未建模、池内 0 卡 | 记账（铁律 11） |
| 88 | 预知 precognition | — | `DefinedTrait.precognition=640`；我们 **0 实现**、0 卡、无图标 | — | 无 | 原版有 trait 但无 `CardTrait` 资产 | 记账 |
| 89 | 穿甲 piercing | — | `DefinedTrait.piercing=900`；我们 **0 实现**、0 卡；官方无本地化条目 | — | 无 | 同上 | 记账 |
| 90 | 汲取 syphon | — | `DefinedTrait.syphon=110`；`BattleEvent.cs:644` 只留了接口位，**无结算**、0 卡 | — | 无 | 同上 | 记账 |
| 91 | 连击 cleave / 致命 deadly | **非词条** | 原版无（`cleave` 仅卡包名 1 条、`deadly` 仅卡包名 3 条） | — | — | — | 不适用 |
| 92 | 灵能/等离子能力 psychicAbility·plasmaAbility | 内部 trait | 不是卡面词；`CardDef.cs:351` 明确「不建模、原版死功能」 | — | — | 一致 | — |

## 只有引擎没有操作层

- **`dodge` / `poisoned` / `resistant` / `survivor` / `sacrifice` / `bastion` / `dropPod` / `giantKiller` / `ward` / `wardWb` / `tainted` / `summonRift` / `jam` / `noncombatant` / `unstunnable` / `sentinel` / `berzerk` / `maintenance` / `doublerage` / `landing` / `aboutToAttack` / `spentAllMana`** —— 引擎有读点，但**无图标 / 无 tooltip 正文 / 无徽标**。全部 **0 卡**（`unstoppable` 1 张除外），今天**不可观测**。
- **`ward`/`wardWb`/`tainted`/`summonRift` 还漏了 `Implemented` 登记**（`CardDef.cs:6889`）⇒ 一旦有卡会白打 `*` 且 `UnimplementedKeywords` 多报。**这是修法最明确的一条**。
- **`sneakAttack`/`terror`**：引擎在 `CounterAttack.cs` 读，但原版给它们 `hasIcon/inPlayIcon=true`，我们 `Badges.cs:413 InPlayIconKeys` 里没有 ⇒ 有卡时看不到状态。

## 只有操作层没有引擎

- **`rally` / `flying` / `vanguard` / `armour` / `markerlight` / `ambush` / `huntMark`** 在 `trait_collection.json` 里 `hasIcon=false` 或 `inPlayIcon=false`，但我们的卡面图标/工具提示**照样画**。这类「我们多显示」与引擎不冲突，但**与原版的显隐口径不一致**（原版不在那 7 格显示，我们也不显示 —— 卡面图标由卡图本身决定，属于美术层，非本节缺陷）。
- **`ability`**：`trait_tips.json` 有一条 `ability | none | EMPTY`（标题有、正文空），而引擎侧只有我们自定的 `UseAbility` —— 这是唯一的「有壳无实」条目。

## 两边都没有

以下原版词条我们**完全没有实现、池内也 0 卡**（`Card_Trait/*` 有键、`cards_engine.json` 三列全 0 命中）：`combo` `support` `relentless` `resolution` `firstStrike` `large` `mekaniak` `daemonicAllegiance` `piercing` `sneak` `counterOffensive` `battleHonour` `courage` `chooseOne` `frontLine` `mesmerize` `corrupt` `poisonous` `coward` `notDestroyRemnant` `oathDouble` `oathInAllTurns` `oathTripleActivation` `dontReturnFerocity` `syphon` `precognition` `psyker` `reactor` `spikes` `horns` `daemonhost` `requiem` `mutation` `transient` `reload` `secret` `secretOrder` `trap` `fear` `meltdown` `massive` `notCollectable`。
**处理口径**：按铁律 11 全部要记账、只有先后；**今天不需动手**（0 卡、0 判据缺口），但要写进引擎线 `任务总表.md` 的欠账，避免下次又当「查无此词」。

## 顺手发现

1. ✅ **`pray` 的 +1 信仰 —— 已查实「不是缺陷」**（2026-10-25 复核，全过程见 §七·二）：`alternativeTraitLogic` 是**原版 `bool`**（`dump.cs:130190`，偏移 `0x4B`，住在 LiveOps 的 `PlayEventData.gameplayVariables` 里、**服务端下发**）⇒ 本地**查不到真值**；但 `0` 有正面证据（第二个消费点是任务点阈值 3 vs 4，官方 base 文案写 3）⇒ **0 = Classic 基线**。而「下一回合开始 +1 信仰」在原版**根本没有任何代码**（`AddFaithMana` 全库只有 2 个调用者）。**结论：不改值，只改注释口径。**
2. 🔴 **`jam` 只做了 3 处**（替身跳过 / 任务条件 / 手牌效果摘除），官方语义是「取消所有卡牌能力与主动效果」。0 卡所以不可观测，但语义差得远。
3. 🟡 **`AlternativeActionName`（`RuleCore.cs:11295`）是硬编码中文**（"职责"/"祈祷"/"狂暴"/"议程"），没走 `Loc`/`CardText` ⇒ 英文语档下按钮仍是中文。与「词条一律走本地化」的口径不一致。
4. 🟡 **`penumbra` 在原版全树 0 命中**（`Warpforge_code` + `decomp_full` + `trait_collection` 三处都查过）—— 之前简报里把它和 `duty` 并称「那一族」，**这是错的**，建议从待办里删掉或改指 `precognition`。
5. 🟡 **`Badges.InPlayIconKeys` 含 `relentless`/`resolution`/`rage`/`frenzied`/`markof*`，但引擎对 `relentless`/`resolution` 零实现** ⇒ 有卡时会画出「我们根本不结算」的徽标（静默误导）。今天 0 卡，先记账。
6. 🟢 **`TraitNameOf(1260)` 已返回 `"hunt mark"`（带空格）** —— `TargetCriteriaEval.cs:772`，2026-10-25 `F1` 已修，实测注释里点名「17 行 / 10 张卡此前一格都没生效」。本项**无需再改**。
7. 🟢 **替代行动四词的全池张数**（`RuleCore.cs:11040` 现读）：`duty 23 / pray 23 / ferocity 18 / agenda 26`（`keywords`+`cardTraits` 合计），**两两不重叠** ⇒ 「一格按钮」的假设成立。

## 七、2026-10-25 修法执行记录（授权后追加 · 本轮只改了 `RuleEngine/Core/RuleCore.cs`）

### 七·一 ① `AlternativeActionName` 硬编码中文 —— ✅ 已改

**官方键：四个全部有**（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 现读）：

| 词 | 官方键 | 官方 en | 官方 zh |
|---|---|---|---|
| 职责 | `Card_Trait/duty` | Duty | 职责 |
| 祈祷 | `Card_Trait/pray` | Pray | 祈祷 |
| 狂暴 | `Card_Trait/ferocity` | Ferocity | **凶残** |
| 议程 | `Card_Trait/agenda` | Agenda | 议程 |

**走的哪条路：不是直接 `Loc.T`，而是「引擎出键 + 表现层出字」**。理由（两条都是硬的）：
- `RuleCore.cs` 文件头第 16 行明写「**本文件属于 `Core/` —— 不允许依赖 UnityEngine**」，而 `Core/Loc.cs:143` 有 `using UnityEngine`（`Debug.LogWarning`）；
- 引擎线的快内环 `工具/ruleprobe` 是**纯 dotnet 编译引擎源码** ⇒ 引擎引 `Loc` 会**编不过 / 破内环**。
- 仓里既有分工就是这么做的：`RuleCodes.TermKey`（引擎给键）+ `BattleDriver.HintForCode`（表现层 `Loc.T`）。

落地（全在 `RuleCore.cs`）：
| 新增/改动 | 位置 | 作用 |
|---|---|---|
| `AlternativeActionTermKey(keyword)` | `:11303` | → `Card_Trait/{duty,pray,ferocity,agenda}`；其它词 `null` |
| `public static Func<string,string> TermResolver` | `:11321` | 表现层启动时挂 `CardPresentation.Loc.T` |
| `UnresolvedActionNameKeys` + 记账 | `:11329` | 取不到词条的键（去重累加，形状同 `KeywordTable.Dropped`）|
| `AlternativeActionNameFallback` | `:11339` | 四个中文字面量，**逐字等于改前** ⇒ 无解析器时行为零变化 |
| `AlternativeActionName` | `:11359` | 先 `TermResolver(key)`；回空 / 回键名本身 / 无解析器 ⇒ 兜底 + 记账 |
| `AlternativeActionNameForLog` | `:11375` | **每个键每进程往战斗日志出声一次**，两种原因分开说 |
| 日志调用点 | `:11216` | `AlternativeActionName` → `AlternativeActionNameForLog(ctx, keyword)` |

🔴 **还差一行（表现层，本轮禁区）** —— 不接上，「英语档按钮仍是中文」这个**症状**不会消失（只是引擎不再硬编码、且会出声）：
- 推荐 **(a)** 在壳/战斗启动处加一行 `RuleCore.TermResolver = CardPresentation.Loc.T;`；
- 或 **(b)** 把 `Battle/BattleDriver.cs:14059` 改成先 `Loc.HasEntry(key)` 再 `Loc.T(key)`。
⚠️ 接上后**中文档 `ferocity` 会显示「凶残」**（官方列），与卡面/`trait_tips.json` 的「狂暴」不一致 —— 二选一：接受官方口径（并同步 `trait_tips.json`，属 UI 线），或改 `Core/Loc.cs:2701` 那条。⛔ 别在引擎里再写第三份。

### 七·二 ② `pray` 的 +1 信仰 / `alternativeTraitLogic` —— 查到底，**判定不改代码**

证据链（六条，全部现读）：
1. **类型**：原版是 `public bool alternativeTraitLogic; // 0x4B`（`d:/2/tools/il2cpp_out/dump.cs:130190`）。我们的 `GameplayVariables.alternativeTraitLogic` 是 **`int`**（`GameplayVariables.cs:324`）—— 类型建模不一致。
2. **唯一读点**：`CardScript__FinishActiveAbility.c` —— `if (*(char*)(*(long*)(lVar2+0xa8) + 0x4b) != '\0')` → `RawCardScript__HasDefaultTrait(card, 0x4a1 /*pray=1185*/)` → `PlayerManager__AddFaithMana(pm,1)` + `BroadcastCollectedFaith`。与我方 `EffectResolver.cs:11805` 逐句一致。
3. **值从哪来**：`matchData.GameplayData`(+0xA8) ← `PlayEventData.gameplayVariables`(+0x80)；`PlayEventData` 是 `[Serializable]` 的 **LiveOps 事件载荷**（`IAfterRead` / `JsonIgnoreIfEmpty`）⇒ **服务端下发**。
4. **本地无真值**：`d:/2` 全树搜 `alternativeTraitLogic`（`*.json/*.txt/*.c/*.cs/*.tsv`）只命中 4 个**代码定义**文件（assetripper / `dump.cs` / stubs / full_decomp），**零资产**；`assets_full` 无 `*GameplayVariables*` 资产；`关服前存档` 的 json/dat/bin/txt 零命中。⇒ **原版值：查不到**（不是「查到是 0」）。
5. **0 有正面证据**：第二个消费点 = `PlayerManager__AddQuestPoints.c:28-41`，阈值 **3（=0 档）vs 4（≠0 档）**；官方 base `CardTraitDescription/questPoints` 写「每 **3** 点」、`questPointsAlt` 写「每 **4** 点」⇒ **0 = 基线/经典，非 0 = Alt 事件模式**。我方 `EffectResolver.cs:11858` 已按此实现，且 `:11994` **已经在出声**（「真值不在本地」）。
6. **基线的「下一回合开始 +1 信仰」在原版不存在**：`PlayerManager__AddFaithMana` 全库只有 **2 个调用者**（`ProcessManaBuff` = 卡面「Gain N Faith」；`FinishActiveAbility` = 上面那条）。`BattleManager__ResolveTriggerPray.c` 全文只做 `RawCardScript__OnTrigger(0x28f /*Pray*/)` + `EntityScript__set_isPraying(1)`，**没有任何 AddFaithMana**。⇒ 官方 `CardTraitDescription/pray`「Gain 1 Faith at the start of your next turn」**与原版代码不符**；`prayAlt`（立刻 +1，再在下回合 +1）才对应实现，且靠 Alt 开关。

**判定**：`alternativeTraitLogic` 保持 **0**（Classic 基线，与官方 base 的 3 点阈值自洽）；`FinishActiveAbility` 那段**不是缺陷**，是 **Alt 事件模式专用分支**。⛔ 不改值、⛔ 不给预设填非 0。

**下一轮补丁（都在本轮禁区，已写成可直接落的样子）**：
- `EffectResolver.cs:11797-11799`：把「⚠️ **今天不可达** … 按判据补齐、当前静默不生效的死路」改为「⚠️ **Alt 事件模式专用分支**：开关 = `GameplayVariablesData.alternativeTraitLogic`（原版 `bool`、`+0x4B`、**LiveOps 服务端下发、本地无序列化值**）。Classic/Skirmish 取 0 ⇒ 本支不跑。**这与官方 base 文案「下一回合开始 +1 信仰」无关 —— 那句话在原版代码里不存在**（`AddFaithMana` 只有两个调用者）。」
- `GameplayVariables.cs:323-324`：`int alternativeTraitLogic = 0;` → `bool alternativeTraitLogic = false;`（doc 写清「原版 bool / +0x4B / LiveOps 下发 / 0 = Classic 基线」），并把 `EffectResolver.cs:11805 / :11858 / :11986` 三处的 `== 0` / `!= 0` 改成 `!` / 裸判断。
- 建议补断言（`RuleEngineTest`）：Classic 预设 `Vars.alternativeTraitLogic == false`；`FinishActiveAbility` 在 false 时**不加**信仰、true 时 +1。

### 七·三 ③ 记账（本轮不改代码）

- `penumbra` **查无此词** ⇒ 从待办删除（`Warpforge_code/`、`decomp_full/`、`trait_collection.json` 三处 0 命中，`DefinedTrait` 也无此成员）。
- `jam` 只做 3 处（替身跳过 / 任务条件 / 手牌效果摘除）vs 官方「取消所有卡牌能力和主动效果」⇒ 0 卡，记账。
- 约 23 个「只有引擎没有操作层」的词条 ⇒ 0 卡，按铁律 11 记账，本轮不动。
- **追加两条**：① `GameplayVariables.alternativeTraitLogic` 类型应为 `bool`（见 §七·二）；② **官方 `CardTraitDescription/pray` 的 base 文案与原版代码不符**（「下一回合 +1 信仰」查无实现）—— 建议进「本地化文案 vs 反编译」差异清单，免得下次照文案去补机制。

### 七·四 自检

`TMPDIR=/tmp/wf_kw bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误 0 / 编辑器错误 0**。未跑 Unity 批处理、未动 git、未碰 `CardDef.cs` / `CardDatabase.cs` / `ActionPump.cs` / `gen_cards_engine.py` / `cards_engine.json`。

## 八、2026-10-25 第二轮执行记录（`alternativeTraitLogic` int→bool ＋ 注释重写 ＋ 两条断言）

### 八·一 改了哪几个文件（⚠️ 第三个是「不写就编不过」）

| 文件 | 改动 | 为什么必须 |
|---|---|---|
| `RuleEngine/Core/GameplayVariables.cs` | `public int alternativeTraitLogic = 0;` → **`public bool alternativeTraitLogic = false;`** + 字段 doc 全部重写；类头那句「我们引擎不读 `alternativeTraitLogic`」一并订正 | 授权范围 |
| `RuleEngine/Core/EffectResolver.cs` | `:11818` `== 0` → `!v...` · `:11822` 日志文案 · `:11872` `!= 0` → 裸 bool · `:12000` `int alt` → `bool alt` · `:11798-11810` 整段 doc 重写（见八·二） | 授权范围 |
| `RuleEngine/Editor/RuleEngineTest.cs` | `:20612` `= 1;` → `= true;`（**不改就编不过**）；新增 `④·b` 两条断言；`:20601` 标题注释同步 | ⚠️ **第三个 `.cs`**：类型改了之后 `= 1` 是编译错误；且你这轮要的两条断言必须有宿主。已经是最小面（没开新文件、没碰 `Run()`、没重生成 `RuleEngineTest_Diag.cs`——断言加在**已有方法内部**，`RunCaseNames` 对账不受影响） |

⛔ **没碰**：`RuleCore.cs`（X6 在飞）· `BattleDriver.cs` · `CardInteraction.cs` · `AttackSelector.cs` · `Editor/BattleScene.cs` · `CardDef.cs` · `CardDatabase.cs` · `ActionPump.cs` · `gen_cards_engine.py` · `cards_engine.json`。

### 八·二 注释重写（`EffectResolver.cs:11798-11810`，已按裁定口径）

写进去的四条：① 原版是 **`bool`**（`dump.cs:130190`，`+0x4B`）、住在 LiveOps `PlayEventData.gameplayVariables`、**服务端下发、本地无序列化真值**；② 我们取 `false` = **经典基线**（第二个消费点 = 任务点阈值 3 vs 4，官方 base 写「每 3 点」⇒ 自洽）；③ **这是 Alt 事件模式专用分支**，不是死路；④ ⚠️ **与官方 base `CardTraitDescription/pray` 那句「下回合开始 +1 信仰」无关 —— 那句在原版代码里不存在**（`AddFaithMana` 全库只有 2 个调用者，`ResolveTriggerPray` 一次都不调）。「死路 / 今天不可达」四个字已从该段删净。

### 八·三 序列化 / 加载路径 —— 已核，无「把 int 当数字读」的地方

- 全仓 `alternativeTraitLogic` 的**生产读点**只有 3 处：`EffectResolver.cs:11818`（`FinishActiveAbility` 闸）、`:11872`（`QuestPointStepFor`）、`:12000`（阈值日志）；其余全是注释/测试。
- **没有 JSON 读**：`GameplayVariables` 从不进 `JsonUtility`（本文件 `:153` 自己就写着「别把本类型塞进 `JsonUtility`」）；`Clone()` = `MemberwiseClone()`（类型无关），`BattleContext` 是 `GameplayVariables.Classic.Clone()`、`RuleCore:176` 是 `(vars ?? Classic).Clone()`。
- 预设 `Classic` / `Skirmish` / 轮抽那份都走 `new GameplayVariables()` + 字段初始化器 ⇒ 三处都是 `false`，**无需逐处赋值**。
- ⚠️ 只有一处需要留意：`CardPresentation/Battle/BattleDriver.cs:29840` 的**注释**还写着旧口径（文件本轮禁区，不改；不是代码）。

### 八·四 两条断言（`RuleEngineTest.cs` `④·b`）

1. `CheckTrue(!GameplayVariables.Classic.alternativeTraitLogic, …)` —— **Classic 预设 = `false`**（🧨 给 `Classic` 填 `true` ⇒ 红）。
2. 同一夹具里：`false` ⇒ 调 `RuleCore.FinishActiveAbility(ctx, 0, warlord, KeywordTable.Pray)` 后 **信仰不变**（🧨 删掉 `!v.alternativeTraitLogic` 那道闸 ⇒ 红）；`ctx.Vars.alternativeTraitLogic = true`（**合成**，不依赖真实下发）再调一次 ⇒ **信仰 +1**（🧨 让方法恒 `return` ⇒ 红）。

📌 **一个必须记下的坑（本轮实测）**：`EffectResolver.cs` 是**文件名，不是类型名** —— 该文件里是 `namespace RuleEngine { public static partial class RuleCore }`（`EffectResolver.cs:27`）。所以正确写法是 **`RuleCore.FinishActiveAbility`**，写成 `EffectResolver.FinishActiveAbility` 会 `CS0103`。⚠️ 本报告 §一 / §七 里凡写 `EffectResolver.cs:NNNN` 的都是**文件:行号**（读数正确），但**类型一律是 `RuleCore`**，别再照字面去调 `EffectResolver.*`。

### 八·五 验证读数

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_kw2 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** ✅ |
| `bash 工具/ruleprobe.sh check` | `哨兵合计：失败 0 条` · `解析差异 0 行` · **rc=0** ✅ |
| `bash 工具/ruleprobe.sh play check` | **rc=0** ✅（①-a/①-b 逐卡全等 · ② 基线指纹 `fc591fb3…` 过） |
| 卡池 md5 | `ef431e8054822f359162862fdf51dd5b` —— **改前改后一致**（只动引擎） ✅ |

⚠️ **第一次 typecheck 是红的，但不是我的**：`RuleCore.cs(11740) CS0103 AbilityFamilyGateOpsForTrigger` —— X6 当时正写到一半（调用先落地、定义后落地），编辑器那条 `CS0006 WFCheck.dll 未找到` 是它的级联。等约 45 秒后重跑即 0/0。**按铁律 13·3：错误全落在别人正在写的文件里 ⇒ 不是自己的问题，记下来别去改**。

### 八·六 「官方文案 vs 原版代码 对不上」清单

**A. 已由反编译坐实（第一手，本轮亲读）**

| # | 官方文案 | 原版代码真值 | 判据 |
|---|---|---|---|
| 1 | `CardTraitDescription/pray`：「Gain 1 Faith **at the start of your next turn**, unless interrupted by damage」 | **没有这回事**。`PlayerManager__AddFaithMana` 全库只有 2 个调用者（`ProcessManaBuff` = 卡面「Gain N Faith」、`FinishActiveAbility` = Alt 档 +1）；`BattleManager__ResolveTriggerPray.c` 全文只有 `OnTrigger(0x28f=Pray)` + `set_isPraying(1)` | 本轮亲读两份方法体 + 全库 `AddFaithMana` 调用者普查 |
| 2 | `CardTraitDescription/prayAlt`：「Gain 1 Faith. Gain another 1 Faith **at the start of your next turn**」 | **前半对、后半没有**：`FinishActiveAbility` 在 Alt 档（`+0x4B`）下确实 +1，但**没有任何「下回合开始再 +1」的代码** | 同 #1 |

**B. 待核（文案与实现方向不同，但本轮判据不足 —— ⛔ 别当缺陷直接改）**

| # | 官方文案 | 我们 / 疑点 | 差什么判据 |
|---|---|---|---|
| 3 | `CardTraitDescription/companion`：「you **can** play up to X copies … this same turn」（玩家选） | 我们**无条件造一张**（手牌满就送墓地且仍广播），无「玩家选」这一步；`CardDef.cs` 的 `Companion` doc 记着前轮逐条引 `CardScript__ProcessCompanion.c:26/29/35/70/74/85/88-89` + `ShouldTriggerCompanion.c:12-29` | 本轮**没有**重读这两份方法体（前轮结论，置信度「已记未复核」）→ 复核一次即可定案 |
| 4 | `CardTraitDescription/swarm`：「played on the **left** side of a troop with the same name」（规则书 `:216` 也写左侧） | 我们 `RuleCore.cs:4138 / :4164` 合并的是 **`slot + 1`**（+1 = 屏幕上更靠中央的那一侧）。**但我们的坐标系左右两侧的 `+1` 屏幕方向相反**（左区 0..3 递增 = 朝督军/屏幕右；右区 5..8 递增 = 朝外/屏幕右） | 需要**逐侧**确认「`slot+1` 在左区和右区分别对应原版的哪一侧」；`BattleManager__AddExecuteSwarm.c` 里到底判的是 ±1 还是别的。**这条可能是真缺陷，也可能是坐标系镜像的假警报** |
| 5 | `CardTraitDescription/ephemeral`：「**Discard** this card from your hand at the end of your turn」 | 我们去向是 `ctx.Removed`（**移出游戏**、不进弃牌堆），`RuleCore.cs:2729`；`SweepEphemeral` 的 doc 引的是**粉丝规则书**「非弃置」 | **缺反编译判据**（`ctx.Removed` 对应的原版动作槽/函数未定位）⇒ 先找 `CardScript__OnTurnEnd` / `ProcessEphemeral` 那一支再定 |

**处理建议**：A 组两条建议进「本地化文案 vs 反编译」差异表并**以代码为准**（⛔ 别照文案补机制）；B 组三条各派一次只读判据（都是 10 分钟级），定案后再谈改不改。

### 八·七 本轮没做 / 留给下一轮

- `RuleCore.cs:11281-11283` 的注释还写 `alternativeTraitLogic (+0x4B) != 0`（**代码正确、只是注释旧口径**）—— 文件本轮禁区（X6 在飞），下一轮顺手改。
- `BattleDriver.cs:29840` 同上一类（注释），文件在别人手里。
- `TermResolver` 的一行接线（`RuleCore.TermResolver = CardPresentation.Loc.T;`）—— 等 `BattleDriver.cs` 空出来。
- `ferocity` 的「凶残 vs 狂暴」—— 已裁定按官方**凶残**，但要连 `trait_tips.json` + 卡面一起改，属下一批。
