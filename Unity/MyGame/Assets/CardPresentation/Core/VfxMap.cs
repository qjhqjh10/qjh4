// VfxMap.cs — **卡牌动作 → 特效名** 的对照表
//
// 原版这张表在 `d:/4/Unity/数据/游戏数据/`：
//   · `card_anim_map.json`  —— 阵营 → {动作名 → 参数 + vfx guid}（13 个阵营）
//   · `card_vfx_tree.json`  —— guid → 那棵特效树（400 条）
//   · `atk_vfx_map.json`    —— 攻击类型名 → 特效名（297 条）
//   · `effect_index.json`   —— 958 个特效，**带还原质量判定**（`对得上`/`偏亮`/`只有原版有内容`…）
//
// 🔴 **2026-10-22（`W64`）就地订正（铁律 5）**：本段原来写「我们的卡是自研的、原版的『卡 → guid』
//    查不到，所以按**事件 + 阵营**兜底 … 以后要逐卡配，把 `ByCard` 填起来即可」—— **早就过期了**：
//    ① 卡池现在是**原版那 1127 张**（`cards_engine.json`）；
//    ② 逐卡那一档 2026-10-14 起就从 `Resources/card_vfx_by_card.json` **载进来了**（`EnsureLoaded`），
//       而且它**排在兜底链最前面**（原版真值优先）；
//    ③ `ByCard` ⛔ **不是手填的** —— 它由 `工具/gen_card_vfx_by_card.py` 生成（手改会被重跑覆盖）。
//    ⇒ 「按事件 + 阵营」那几行只剩 `Hit` / `AttackRanged` / 三个资源 UI 件 / `Deploy` 还在用。
//
// **挑特效的依据是 `effect_index.json` 的判定列**，别凭名字挑 —— 名单里有一批
// `只有原版有内容（导出整个丢了）` / `两边全程空（纯脚本驱动）`，播出来是**空的**。
// 下面这些全是「对得上 + 高置信」的。
using System.Collections.Generic;
using RuleEngine;      // 🆕 2026-10-19（B6）：`CardDef.CardAnimTime` —— 逐候选动画时间（数据侧 E12 已落）。
using UnityEngine;

namespace CardPresentation
{
    public static class VfxMap
    {
        // ---- 事件名 ----
        /// <summary>出牌（手牌打出去的那一刻）。
        /// 🔴 **2026-10-22（`W64` §B1）：原版【没有 play 特效槽】** —— 共享 `CardPrefab` 的 `CardScript`
        /// 字段表里只有 `normalSummon`/`legendarySummon`/`landOnGroundParticles`/`attackHit*`/
        /// `cardDestroyFX`/`toBoardVFXAnchor`/`rangedAttackParticlesSO`，**没有 play**；
        /// `_PlayPlayerCard` / `_PlayEnemyCard` 两个状态机 **0 处 `Instantiate`**。
        /// ⇒ `ByEvent` 里那一行已删，`Resolve(PlayCard)` 回 null 是**正确结果**；**逐卡 `play` 那一档照旧**。</summary>
        public const string PlayCard     = "play";
        public const string Deploy       = "deploy";          // 登场（落到格位上）
        public const string AttackMelee  = "attack_melee";    // 近战攻击
        public const string AttackRanged = "attack_ranged";   // 远程攻击
        public const string Hit          = "hit";             // 挨打（掉血）
        /// <summary>🆕 2026-09-29：**治疗**（原版 `BattleAnims.heal` → `Healing_Circles`）。
        /// 🔴 **2026-10-20（`W58`）就地订正（铁律 5）**：这一行原来写「我们这边没有独立的治疗事件 ——
        /// 它是 `EvtKind.Hit` 且 `Amount &lt; 0`」—— **已过期**：现在有独立的
        /// <see cref="RuleEngine.EvtKind.Heal"/>（`Amount` = 实际治疗量），由
        /// `BattleDriver.GenericVfxFor` 映射到本名。
        /// ⚠️ **Syphon 那一档不播**（`BattleEvent.HealSyphon`）：原版 `CardScript._Heal…:201-225`
        /// 只在 `HealType.Normal(0)` 实例化治疗圈，Syphon(1) **只有飘字**。</summary>
        public const string Heal         = "heal";
        public const string Death        = "death";           // 阵亡
        public const string Ability      = "ability";         // 部队卡在场上发动技能
        public const string Trigger      = "trigger";         // 触发效果
        // 🆕 2026-09-15 补的四个 —— 它们原来**结构上就播不出来**（`PlaySignal` 的 switch
        //    没有它们的 case ⇒ 直接 `default: return`；而且三个资源事件的 `Slot` 本来就是 -1，
        //    就算加了 case 也会被 `if (slot < 0) return;` 挡掉）。见 `BattleDriver.PlaySignal`。
        public const string Return       = "return";          // 离场但不阵亡（回手 / 回牌库）
        public const string GainFaith    = "gain_faith";      // 获得信仰（修女会）
        public const string GainSpirit   = "gain_spirit";     // 收集灵魂石（灵族）
        public const string GainQuest    = "gain_quest";      // 获得任务点（暗黑天使）
        /// <summary>🆕 2026-09-25：**玩家点走一颗灵魂石**（灵族）。⚠️ 和 `GainSpirit` **不是同一件事** ——
        /// 那条是「玩家资源变了」（挂 HUD 计数图标上），这条是「**在那颗石头原来那一格**播收集特效」。 </summary>
        public const string CollectWaystone = "collect_waystone";

        /// <summary>🆕 2026-10-14（`A1816`）：**抽卡**（`EvtKind.Draw`）—— 「卡飞进手牌区那一下」。
        ///
        /// 🔴🔴 **2026-10-22（`W64` §B5）就地改判（铁律 5，覆盖本段原来那一整版）：原版抽卡链【0 粒子】。**
        /// 判据（第一权威，逐句读）：`CardScript__CardDrawn.c` · `BattleManager._ResolveDrawCard_d__432__MoveNext.c`
        /// · `BattleManager__TurnStartCardDraw.c` · `CardScript._PlayPlayerCard…` —— **全部 0 处粒子 `Instantiate`**
        /// （抽卡走 `AnimatorClipInfo` 的**动画**）。
        /// 本段原来写的判据是 `d:/2/Warpforge_tools/data/vfx_wiring_j4.tsv:65` —— 那是**我们自己的 Godot 复刻台账**，
        /// **不是原版判据**（审计 §判据来源末条点名）；而且那两件现读各属别的桶：
        /// `UM_CardDraw` 落在 `UM45`/`UM53` 等 4 行的 **`play`** 桶（authoritative）、
        /// `SAU_CardDraw` **本地没有任何 CardAnim 指向它**。
        /// ⇒ `ByFaction` 里那两行**已删**，抽卡这一拍**留白**（`Resolve(Draw, …)` 恒 null）。
        /// ⏭ 要复刻抽卡表现就去解 `AnimatorClipInfo` 那条**动画**链（审计 §三 U3），⛔ 别拿 play 件顶。
        ///
        /// 🔴 **2026-10-16（账 `A1731`）的那条机制仍然活着**：`Resolve` 走到底（这个 (事件, 阵营) 一行都没配）
        /// 时会 `Debug.LogWarning` 一次（每种 (事件, 阵营) 只说一次，见 `WarnNoFactionVariant`）——
        /// `Draw` 今天**仍然是那个出口唯一的见证件**（⛔ 别把它也加进 `IsOriginalNoGenericEvent`，
        /// 那会让这条警告连一次都发不出来，`A1731` 那条断言就没东西可证了）。
        /// ⇒ 「不带阵营」的默认口径解析出来是 null 是**预期的**，**而且不出声**（没传阵营 = 探测口径，不是播放）。</summary>
        public const string Draw = "draw";

        /// <summary>🆕 2026-10-14（`A1816`）：**伏击翻面**（`EvtKind.AmbushExit` —— `Ambush` 面朝下的
        /// 单位翻开那一刻）。🔴 **2026-10-22（`W64` §B7）：本事件的【通用件入口已撤】—— 见下。**
        ///
        /// **原版的取法**（第一权威，方法体都在）：`BattleManager.GetTraitEffect(trait, FromCode(5), army)`
        /// → `CardTraitCollection.GetTraitEffectObject` → `CardTrait._traitEffects[]` 里按
        /// `EffectTriggerType` 挑那一条、再按阵营取 override（`TraitEffect.GetEffect`）。
        /// ⇒ 也就是说：**「伏击翻开播什么」写在 trait SO 里，不写在卡 SO 里**。
        ///
        /// 🔴 **那张 trait→effect 真值表本地没有**（2026-10-22 现读）：
        /// `assets_full` 全库按 `traitEffects|TraitEffects|effectAnim` 搜 **0 命中**（那批 `CardTrait` SO 没导出）；
        /// `card_vfx_by_card.json` 的 `byKeywordTrigger` 42 个关键词里**没有 ambush**
        /// （它是按 `<关键词>Trait<Trigger>` 的**构词法**从 CardAnim 名推的，而 `Ambush Effect` 不守那个构词法）。
        /// ⇒ 按「⛔ 不许保留孤儿」：`ByEvent` 里 `{ AmbushExit, "AmbushEffect" }` 那一行**已删**。
        ///
        /// ⚠️ **旁证（不足以当判据、但下一个人会想知道）**：`AmbushEffect`
        /// （`Assets/WarpforgeVFX/Prefabs/AmbushEffect.prefab`，`effect_index.json` 判 **对得上/高/0.89**）
        /// 自带的三层全都指向 Ambush 这个天赋 ——
        ///   · 音效 —— `AnimFXController.sounds[0].sound` = cue **`Trait Ambush`**
        ///   · 动画 —— `AnimFXModuleChangeMaterial`：把**卡**的材质换成
        ///     `Card 3d Dissolve Blend Image Ambush`，`_Blend` 0→1（`delay 0.35 + duration 1.0`）
        ///   · 粒子 —— 它自己的 ps（台账组件列写 `-`，全靠 prefab 内部）
        /// 且 `animinfo_lookup["AmbushEffect"]` 的 CardAnim 名就是 `Ambush Effect`（`tm 1.5`）。
        /// 但「它是不是 `FromCode(5)` 那一槽」证不出来（U1）⇒ 先撤、⛔ 不拿它当通用件继续播。
        /// ⏭ 补法：导出 `CardTraitCollection.TraitCollection[]`（`TraitPairs` → `CardTrait` SO），
        ///    查到 `DefinedTrait.ambush = 1200` 的 `FromCode(5)` 条目再接回。
        /// ⚠️ `EventTiming.SourceOf(EvtKind.AmbushExit)` 那段时序判据**仍然成立**（它写的是那件 prefab 自己
        ///    的 1.35 s 补间）—— 只是今天没有件可播，**那条时长仍然是长度闸的上限**（没改它）。</summary>
        public const string AmbushExit = "ambush_exit";

        // ================== 🆕 2026-10-23（`W63` 收口）：**逐卡「技能动画」两桶** ==================
        // 【为什么单立两个事件名】`gen_card_vfx_by_card.py` 的 `TRIGGER_EVENT` 把原版
        //   `AbilityTrigger.UnitAttack(50)` 归到 `attack_melee`、`UnitDamaged(60)` 归到 `hit`
        //   —— 那两桶的**语义是「攻击/受击的特效」**，于是这 17 条被当成「攻击件/受击件」播，
        //   而原版它们是**这张卡的技能动画**（`CardAbility` 触发 → `AbilityLogic.PlayAnim` →
        //   `BattleManager.AddCardAnim` → 动作队列）。两者**播的时刻、锚点、时长**全不一样。
        //   数据侧（生成器）改归口在白名单外 ⇒ 这里在**载入时**把它们整份抄进自己这两个桶，
        //   逐卡表的键仍然只有卡 id ⇒ 生产路径 `ResolveAllTimed` 照原样查得到（⛔ 不需要第二套 join）。
        //
        // 【池内口径（2026-10-23 现读；⚠️「原版几行」与「运行期几件」**不是同一个数**，别混）】
        //   · `UnitAttack(50)`：原版 `cardAnimations[]` **13 行 / 11 张卡**
        //     （判据 = `CardDef.AnimTimes` 里 `tn == "UnitAttack"` 的行：DA22×2 · SAU31×2 · 其余 9 张各 1）。
        //     **运行期有件的是 12 行** —— `SAU31` 的第二行 `Wait_1s` 是**纯计时拍**（`animAdressable` 为空），
        //     生成器写运行期表时按 `n` 为空丢掉它（它那 1.0 s 仍由 `AnimTimes` 管，只是没有 prefab 可播）。
        //     ⚠️ `attack_melee` 里另外 2 条（`EC29` / `SOR8`）是 `inferred-name`（按名字猜的），
        //     原版触发**不是** `UnitAttack` ⇒ **不收**，留在旧桶。
        //   · `UnitDamaged(60)`：原版真值只有 **1 行**（`BL20 Buff_DiabolicStrength`）；
        //     而 `hit` 桶里另有 3 条 `inferred-name`（按名字对上真 CardAnim 的）—— W63 §B 的「4 张」含它们
        //     ⇒ 本桶收 `hit` **整桶 4 行**（这也正是「受击那一档」今天在池内的全部内容）。
        //   ⇒ 两个口径都要记住：**原版行数 13 + 4 = 17**（本件的验收口径）·
        //     **运行期有件 12 + 4 = 16**（`VfxMap.CardTableUnitAnimEntries` 那个数，断言里两条都断）。
        //
        // 【⚠️ 这两桶**没有** `ByEvent` 兜底】原版这两档**只有逐卡**（`cardAbilities[].abilityLogic[].
        //   cardAnimations[]`），没有「通用技能动画」这种东西 ⇒ 没配的卡就是没有
        //   ⇒ 已列进 <see cref="IsOriginalNoGenericEvent"/>（`ResolveFallback` 静默回 null，⛔ 不喊假警告）。
        //   消费端 = `CardAnimClips`（数据）+ `BattleDriver.PlayUnitSkillAnim`（播放）。
        /// <summary>原版 `AbilityTrigger.UnitAttack(50)` 触发的**技能动画**（逐卡；原版 13 行 / 运行期有件 12 条）。
        /// ⛔ **不是**「近战攻击特效」——`ByEvent[AttackMelee]` 已于 2026-10-22（`W63` §B）整行删除。</summary>
        public const string UnitAttack  = "unit_attack_anim";
        /// <summary>原版 `AbilityTrigger.UnitDamaged(60)` 触发的**技能动画**（逐卡；池内 4 条）。
        /// ⛔ **不是**「受击特效」——受击那一拍原版**一件粒子都不播**（`CardScript._ReceiveDamage`），
        /// 见 `BattleDriver.PlaySignal` 里那段注释。</summary>
        public const string UnitDamaged = "unit_damaged_anim";

        /// <summary>按事件兜底。
        ///
        /// ⚠️ **这些是「在战斗场景里实拍过」才选进来的**（`Editor/VfxPicker.cs` 的试片台，
        ///    每个效果拍 3 个时刻）—— `effect_index.json` 判定「对得上」**不等于**在我们场景里也好看：
        ///    实测 `BulletImpactBurst_Blood` 渲成一整块灰方块、`Attack_Stomp` 某一帧是一大块黑菱形，
        ///    换个效果就没这问题。挑特效一定要看实拍。
        ///
        /// 🔴 **2026-09-15：把「原版每个事件接什么」查全了 —— 见下表。**
        ///    以前**没查过** `vfx_wiring_j1..5.tsv`（0824 的「特效名 → 它的原版触发事件 + 接进
        ///    battle.gd 哪个分支」台账，453 行）和 `vfx_wiring_ctx_0824.md`（那份写了**原版的固定触发点**），
        ///    结果是**有 2 个用反了**（下面标 ❌ 的两行）。
        ///
        /// | 我们的事件 | 原版接的是 | 在我们导出索引里能用吗 |
        /// |---|---|---|
        /// | `deploy` | ~~`BlueSummonCircle`（+ 卡专属查名）~~ 🔴 **2026-10-21 作废**：那 4 个候选其实是**某些卡的技能动画**，不是登场法阵（原版没有这一层）⇒ 已删 | —— |
        /// | `death` | `Card 3D Death Explosion`（逐卡 `customDeathParticles` 优先） | ✅ 2026-10-01 已进库；**播放口 = `CardFeel.PickDeathFx`**（原版二选一），⛔ 不进 `ByEvent` |
        /// | `attack_melee` | 按阵营查 `SLASH_VFX`：`Whip Attack EC Normal` / `Cut Axe SW` / `Sword Slash` / `Slash Repeating 2x` / `Rapacious Claw Slash` | 只有 `Cut Axe SW` 干净（对得上/中/1.01）；`Sword Slash`·`Rapacious Claw Slash` **不在索引**，另两个一个「全程空」一个「偏暗 0.19」 |
        /// | `attack_ranged` | 按阵营查 `RANGED_VFX`（`BulletImpact_1shot_trail` 等 ~11 键） | ❌ `BulletImpact_1shot_trail` 判「**两边全程空（完全脚本驱动）**」 |
        /// | `hit` | 攻击结算处的 `AttackHitSmall` / `Actual_Explosion` | ❌ 前者「全程空」、后者**不在索引** |
        /// | `play` / `ability` | 台账里没有对应行 ⇒ 🔴 **2026-10-22 查实：原版也没有这一档**（见 B1/B2） | —— |
        /// | `trigger` | 原版 `ActivateTriggerTraitAnim` 只做徽标/状态框（`Trigger` 那一行是**死条目**） | —— |
        /// | `return` / 三个资源事件 | 见下面 `ByEvent` 里那几行的注释 | ✅ 三件 UI 特效（`return` 已删，B4） |
        ///
        /// 🔴 **2026-10-22（`W63` §B）就地订正（铁律 5）**：上面 `attack_melee` 那一行是 **0824 台账**的
        ///    口径（`SLASH_VFX`）—— **原版代码里没有「通用近战粒子」这一档**：
        ///    `CardScript.ResolveAttackAnimationEffects` 只在**命中帧**播 `AttackHitSmall`。
        ///    ⇒ 我们的 `ByEvent[AttackMelee]` **整行已删**（见下表），`Resolve(AttackMelee)` 回 null 是**正确结果**。
        ///    判据 → `资料/普查产出_1022/W63_飞行与卡ID演出_调查.md` §B。
        ///
        /// 🔴🔴 **2026-10-22（`W64` B1–B8）再订正（铁律 5）：上面「用替代品是有理由的」那句【只对 `Hit`/`AttackRanged` 还成立】。**
        ///    本批把 6 条「我们挑的 / 同名不同物 / 判据只有我们自己的 Godot 台账」的通用件全撤了：
        ///    `play`（B1）· `ability`（B2）· `death`（B3）· `return`（B4）· `draw` 两行（B5）· `ambush_exit`（B7）·
        ///    `collect_waystone`（B8）。逐条原版出处写在 `ByEvent` 那张表对应那一段注释里。
        ///    🔴 **2026-10-22（P6）补一笔**：`collect_waystone` 的真件（`Remnant Aeldari Collect particles`）
        ///    当天已导进效果库、**已接回 `ByEvent`**（不再是缺口）—— 其余五条维持「不播」。
        ///    ⇒ **剩下还挂替代品的只有 `Hit` / `AttackRanged` 两件**（A4 那件其实是原版 SO 第一条，不是发明）。
        ///    ⛔ 别再把「反正是替代品、有总比没有强」当理由 —— 本仓口径是「**原版没有的就不播**」。
        ///
        /// ⚠️ **②「照原版传参数」被 ① 卡住**（2026-09-15 查实）：原版的参数在 `card_anim_map.json`
        ///    （每条带 `startPosOption`/`endPosOption`/`vfxDelayTime`/`timeMoving`/`shouldMoveVFX`/`ease_points` 等 10 个），
        ///    但那张表**按动画 clip 名索引**（`AeldariSummon` / `Atk_BulletImpact_Eldar_DCannon` …），
        ///    而**我们现役这 8 个名字在 `card_anim_map` / `atk_vfx_map` / 台账里一个都查不到**
        ///    ⇒ 想传参数，**得先把特效换成那三张表里有的**。
        /// </summary>
        // ================== 🆕 2026-10-22（`W64`）：原版「登场淡入」的**两档** ==================
        /// <summary>原版 `CardScript.normalSummon` 的 prefab（= 普通档登场淡入）。
        /// 出处：`数据/游戏数据/card_vfx_by_card.json` 的 `generic.byEvent.deploy`（`conf: exact`，4 条之一）。</summary>
        public const string DeployDefault = "Invoke Minion Card Fade Default";
        /// <summary>原版 `CardScript.legendarySummon` 的 prefab（= **传说档**登场淡入）。
        /// 出处：同上（`generic.byEvent.deploy` 的第 2 条 `conf: exact`）。
        /// 🔴 **2026-10-22（`W64` §B9）之前 `VfxMap` 这一条链【没有这一档】** —— 所有卡（含传说）
        /// 都走普通档。⚠️ 就地订正（铁律 5）：审计 §B9 写「整个工程 0 处引用」**已过期** ——
        /// `BattleDriver.MountSpawnParticlesUnderToBoardFx`（`:10128` 的 `SummonVfxLegendary`）已经在用它，
        /// 但那挂的是**卡身上**那一层（原版 `CardSetup.c:305-333` 的 `Spawn Particles`），
        /// 与这里「竞技场空间里那一次根实例化」**是两件不同的事** ⇒ 那条路补不上这一档。</summary>
        public const string DeployLegendary = "Invoke Minion Card Fade Legendary";

        // ============ 🆕 2026-10-22（`W64`）：**`ByEvent` 里该留空的两族** ============
        // 这一格是**判据**，不是备注：`ResolveFallback` 与自检的白名单都读它（⛔ 别在两处各写一份）。
        // 两种**不同**的空，别混：
        //   ① <see cref="IsOriginalNoGenericEvent"/> —— **不该由本表兜底**（静默回 null；
        //      其中 `death` 是「原版有真件、但播放口在 `CardFeel`」，不是「原版没有」）；
        //   ② <see cref="IsKnownGapEvent"/>          —— **有真件、我们还没接上**（回 null 但**出声**）。
        /// <summary>① 这些事件**不该由 `ByEvent` 兜底**（`ResolveFallback` 对它们**静默**回 `null`）。
        /// 两种都算：**原版没有这一档**（`play` / `ability` / `return` / `attack_melee`），
        /// 以及**原版有、但播放口不在本表**（`death`：真件由 `CardFeel.PickDeathFx` 那条二选一放）。
        /// ⚠️ 所以这个名字说的是「`ByEvent` 里留空是对的」，⛔ **不是**「原版没有这件东西」。</summary>
        public static bool IsOriginalNoGenericEvent(string evt)
        {
            return evt == PlayCard     // B1：`CardScript` 没有 play 特效槽 + `_Play*Card` 0 处 Instantiate
                || evt == Ability      // B2：技能动画一律来自逐卡 `abilityLogic[].cardAnimations[]`
                || evt == Death        // B3：真件 = `Card 3D Death Explosion`，播放口在 `CardFeel`（不进 ByEvent）
                || evt == Return       // B4：`_ResolveRecallToHand` 0 处 Instantiate（回手是 Animation、不是粒子）
                || evt == AttackMelee  // A2：1,195 张卡只有 2 张有 attackAnim、且都是空引用
                // 🆕 2026-10-23（`W63` 收口）：**这两桶只有逐卡，原版没有「通用技能动画」**
                //   （`UnitAttack(50)` / `UnitDamaged(60)` 都写在卡的 `cardAbilities[]` 里）
                //   ⇒ 没这张卡的候选就该静默回 null（⛔ 别让 `WarnNoFactionVariant` 把它当漏配喊出来）。
                || evt == UnitAttack
                || evt == UnitDamaged;
        }

        /// <summary>② **有真件、但本机还接不上**的事件（回 `null` **且出声一次**，⛔ 绝不拿替代品顶）。
        /// <para>`AmbushExit`（B7）：真件在 `CardTrait._traitEffects[]` 的 `FromCode(5)` 槽里，
        /// 而**那张 trait→effect 真值表本地没有导出**（见 `ByEvent` 里那段注释）。</para>
        /// <para>🔴 **2026-10-22（P6 · `W64` B8 收口）：`CollectWaystone` 已从这里【移出】** ——
        /// 真件 `Remnant Aeldari Collect particles` 当天已导进效果库、映射见 `ByEvent` 那一行
        /// ⇒ 它现在**解析得出**，不再是缺口。⚠️ 把它留在这个名单里会**劫持** `ResolveFallback`：
        /// 那个分支排在 `ByEvent.TryGetValue` **之前**（见 `ResolveFallback`），留着 ⇒ 那一行永远读不到。</para></summary>
        public static bool IsKnownGapEvent(string evt)
        {
            return evt == AmbushExit;
        }

        static readonly Dictionary<string, string> ByEvent = new Dictionary<string, string>
        {
            // 🔴 **2026-10-22（`W64` §B1）：`play` 这一行【已删】—— 原版【没有 play 特效槽】。**
            //   判据（第一权威）= 共享 `CardPrefab` 的 `CardScript` 字段表
            //   （`assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/
            //     MonoBehaviour_1744609728290659264.json`）里**根本没有 play 那一格**：只有
            //   `normalSummon` / `legendarySummon` / `landOnGroundParticles(+Legendary)` /
            //   `attackHitSmall/BigParticles` / `cardDestroyFX` / `toBoardVFXAnchor` /
            //   `rangedAttackParticlesSO`；且 `CardScript._PlayPlayerCard_d__259__MoveNext.c` 与
            //   `_PlayEnemyCard_d__258__MoveNext.c` **0 处 `Instantiate`/粒子**。
            //   原来挂的 `Tap Blue Glow` 是 2026-09-12 **我们挑的「轻一下」**（`decomp_full` 全文 0 命中、
            //   `anim_address_map` 无任何 CardAnim 指向它）⇒ 原版没有的第二下，**已删**。
            //   ⚠️ 删了以后 `Resolve(PlayCard)` 回 `null` 是**正确结果**（不是漏配）——
            //     `ResolveFallback` 里 `IsOriginalNoGenericEvent` 对它有显式分支、**不出声**。
            //   ⚠️ **逐卡 `play` 那一档照旧**（现读 534 行，原版 `cardAbilities[].abilityLogic[].
            //     cardAnimations[]` 解出来的）—— 删的只是「通用件」。
            // 🆕 2026-09-29 **换成原版那件**（判据齐）：原版每张卡登场走的是 `CardScript.normalSummon`
            //   （共享 `CardPrefab` 上的 CardAnim），它的 `animInfo.animAdressable` →
            //   prefab **`Invoke Minion Card Fade Default`**（传说档是 `… Legendary`）。
            //   出处：`数据/游戏数据/card_vfx_by_card.json` 的 `generic.byEvent.deploy`（`conf: exact`）。
            //   ⚠️ 原来那件 `Sororitas Summon Basic` 是**我们挑的替代品**（2026-09-12 挑的）。
            //   🆕 **2026-10-22（`W64` §B9）：稀有度分档** —— 传说档是同一份 `CardPrefab` 上的
            //   `legendarySummon`（`generic.byEvent.deploy` 的**第 2 条 `conf: exact`**，
            //   → prefab <see cref="DeployLegendary"/>）。挑档走 <see cref="DeployTierFor"/>
            //   （判据 = 原版 `cardRarity == 4`，与 `BattleDriver.GetInvokeCue(cardRarity)` 同一套规则）。
            { Deploy,       DeployDefault },
            // 🔴 **2026-10-22（`W63` §B）：`attack_melee` 这一行【已删】—— 原版【没有】通用近战件。**
            //   原版近战只在**命中帧**播 `AttackHitSmall`（`CardScript.ResolveAttackAnimationEffects`），
            //   没有「通用近战粒子」这一档（`decomp_full` 里没有；逐卡 `AlternateArtCard.attackAnim`
            //   1,195 张里只有 2 张、且都是空引用 ⇒ 判据为空）。
            //   逐卡 `attack_melee` 现读只有 **13 行 / 1127 张卡**，且那 13 条其实是 `UnitAttack` 触发的
            //   **技能动画**（语义归口另案，见报告 §B/§D）⇒ 631 个近战攻击者里 618 个原本落到这件。
            //   ⇒ 原来这里挂的 `BulletImpactBurst_crowd` 是**我们挑的替代品** = 原版没有的第二下，已删。
            //   ⚠️ 删了以后 `Resolve(AttackMelee)` 回 `null` 是**正确结果**（不是漏配）——
            //     `ResolveFallback` 里对它有显式分支、**不出声**（见那一处）。
            //   判据 → `资料/普查产出_1022/W63_飞行与卡ID演出_调查.md` §B。
            //
            // 🆕 2026-09-29 **换成原版那件**（判据齐）：`CardScript.attackHitSmallParticles` →
            //   prefab `AttackHitSmall`（原版 `attackHitSmallParticles` 与 `attackHitBigParticles`
            //   是**同一个 pathId**，两档同对象）。
            // 🔴 **2026-10-22（`W63` §B）：远程兜底件换成原版那一件** —— 原版
            //   `RangedAttackParticlesByClanSO__GetRangedClanParticles.c` 查不到时**返回 SO 第一条**
            //   = Ultra 的 **`BulletImpact_2shot_trail`**；原来这里挂的是 `BulletImpact_2shot`
            //   （少了 `_trail`）⇒ 我们播的**不是同一件**。现读 37 张没有
            //   `customRangedAttackParticles` 的卡走这一行。判据 → 同上报告 §B。
            { AttackRanged, "BulletImpact_2shot_trail" },   // 远程：原版查不到时的第一条（Ultra）
            // 🔴 **这件仍是替代品**：原版攻击结算处接的是 `AttackHitSmall` / `Actual_Explosion`，
            //    **现在 `Hit` 已经改成原版那件**（见下），这一行留着是为了说明当时的取舍。
            // ⚠️ 2026-09-15 实拍复核过（`_tmp_view/pick_hit.png`）：五个候选里这件最好看。
            { Hit,          "AttackHitSmall" },
            // 🔴 **2026-10-22（`W64` §B3）：`death` 这一行【已删】—— 阵亡那件**不走本表**。**
            //   判据（第一权威）= `CardScript._UnitDeath_d__446__MoveNext.c`：整个协程**只有两处
            //   `Instantiate`** —— `:233` 逐卡 `customDeathParticles`、`:262` 默认 `cardDestroyFX`
            //   （= GameObject **`Card 3D Death Explosion`**，`generic.byEvent.death` 里也是它、`conf: exact`），
            //   判别式在 `:240`「逐卡取不到才用默认件」。
            //   ⚠️ **就地订正（铁律 5）**：本段初稿写「原版没有通用阵亡件」—— **不准确**：真件就是
            //   `Card 3D Death Explosion`；错的只是**入口** —— 原版走 `CardFeel.PickDeathFx` 那条二选一
            //   （我们早就实现了），而不是「一张通用表」。所以本表留空、由那条路播。
            //   原来这里挂的 `Explosion Fenrisian Monstrosities` 现读是 **`SW54` 的 `play` 逐卡件**
            //   （`card_vfx_by_card.json`，`conf: authoritative`）—— 拿一张卡的出场动画当「通用阵亡」，
            //   那是 2026-09-12 的替代品 ⇒ **已删**。
            //   ⇒ 真件取不到时**出声、不播替代品**（`CardFeel.PickDeathFx` 已经出过声）。
            //   🔴 **下游待改（不在本文件）**：`CardFeel.DeathExplosion` 的退回路原来在这里取替代品名字
            //      （`VfxMap.Resolve(VfxMap.Death)`）⇒ 现在会拿到 null，`WarpforgeEffectPlayer.Play(null, …)`
            //      会在 `Dictionary.TryGetValue(null)` 上**抛异常**。那条路必须改成「只出声、不播」。
            //      判据 → `资料/普查产出_1022/W64_兜底审计_特效.md` §一 B3。
            // 🆕 2026-09-29 **新增 `Heal`**（判据齐）：原版 `BattleAnims.heal` → prefab **`Healing_Circles`**
            //   （那张表其余 12 个槽在本地**全是空引用**）。
            // 🔴 **2026-10-20（`W58`）就地订正（铁律 5）**：这里原来写「我们这边『治疗』= `EvtKind.Hit`
            //   且 `Amount < 0`」—— **已过期**。现在 = <see cref="RuleEngine.EvtKind.Heal"/>；
            //   **Syphon 那一档在 `GenericVfxFor` 里直接回 `null`**（原版只在 `HealType.Normal` 播治疗圈）。
            { Heal,         "Healing_Circles" },
            // 🔴 **2026-10-22（`W64` §B2）：`ability` 这一行【已删】—— 原版【没有通用技能粒子】。**
            //   判据：技能动画一律来自该卡自己的 `cardAbilities[].abilityLogic[].cardAnimations[]`
            //   （逐卡表里 `ability` 现读 **440 张 / 612 条权威**，没覆盖的卡原版就是没有）；
            //   而 `Buff_Blue_SW` 现读是**太空野狼逐卡件** —— `card_vfx_by_card.json` 里 `SW11/18/27/29/
            //   32/34/36/37/40/43/47/54/57/61` 共 **17 条 `conf=authoritative`** 的 `play`/`ability`/
            //   `trigger` 行；`anim_address_map` 里它是 `Buff_Blue_SW_self/_target/_warlord` 三条
            //   CardAnim 的 target（`cardanimsgeneral`）。⇒ 通用那一条（2026-09-12 挑的「蓝色光柱」）**已删**。
            //   ⚠️ `Resolve(Ability)` 回 `null` 是**正确结果**（`IsOriginalNoGenericEvent` 静默挡掉）。
            //   判据 → `资料/普查产出_1022/W64_兜底审计_特效.md` §一 B2。
            // 🔴 **2026-10-19（B2 · 偏差 7 落地后 · 本行现在是【死条目】）**：
            //   `BattleDriver.GenericVfxFor(Trigger)` 自 2026-10-19 起回 **`null`** —— 原版
            //   `CardScript.ActivateTriggerTraitAnim` 只做徽标脉冲 + 状态框，**没有**通用特效
            //   （判据 → `资料/普查产出_1018/R_战斗表现链路对拍.md` §4 偏差 7）；触发那一拍的
            //   反馈现在走 `PlayFeel → PlayTraitPulse` / `PlayTraitParticles`，**不读这一行**。
            //   ⚠️ **为什么不删**（两个理由，都现读）：
            //     ① `VfxMap.Trigger` 这个**常量**还有消费者 —— `Editor/BattleScene.cs` 的
            //        「Trigger 不再播通用特效」那条断言要用它取名字；
            //     ② 删掉这一行会让 `Resolve(Trigger)` 落进「只有按阵营配」的兜底出口
            //        （`WarnNoFactionVariant`）⇒ 探针/自检会**凭空多出一条「留白」警告**（假信号）。
            //   ⇒ **留着、并明确标注无生产消费者**。⛔ 别把 `GenericVfxFor` 的 `Trigger` 改回非 null。
            //   ✅ **2026-10-19（W）现核「确实是死条目」**：生产侧取通用特效的**唯一**入口是
            //   `BattleDriver.GenericVfxFor` —— 它对 `EvtKind.Trigger` **显式 `return null`**
            //   （`BattleDriver.cs:14188`）；`PlaySignal` 在 `evt == null` 时于 `FireEventAll` **之前**返回
            //   （`:14346` / `:14358`）⇒ 这一行**没有生产消费者**。`VfxMap.Resolve(Trigger)` 的其余
            //   调用点只有 `Editor/BattleScene.cs:7852` 那条断言（它要的正是这个名字）和 `:24928`
            //   的「名字在库里」清单 ⇒ **删了会断两项自检的名字来源 + 多一条假留白警告**，维持保留。
            //   🆕 **同日（W）逐卡 Trigger 接上生产**（`ResolveAllTimed(..., perCardOnly: true)`）——
            //   它走的是**只查逐卡表**的出口，**仍然不会读到这一行** ⇒ 本条依旧死、保留理由不变。
            { Trigger,      "Faith_trigger_unit" },         // ⚠️ 死条目（无生产消费者，见上）
            // 🆕 2026-09-15：**这一组是按原版台账挑的，不是自己拍的**。
            //    台账 `d:/2/Warpforge_tools/data/vfx_wiring_j1..5.tsv`（453 行，0824 的独立
            //    「特效名 → 它的原版触发事件 + 接进 battle.gd 哪个分支」）里，这三件**明明白白写着是 `UI` 类**：
            //      · `Faith UI Gain`    —— 「信仰值(☀)获得时**计数 UI 闪光**」，接在信仰计数 UI 更新处
            //      · `Quest UI Gain`    —— 「任务点获得时计数 UI 闪光」，接在 QP UI 更新处
            //      · `Waystone UI Gain` —— 「灵族路标石(灵魂石)收集计数 UI 闪光」，接在灵魂石收集结算处
            //    ⇒ 它们挂的是 **HUD 上那个资源图标**，**不是棋盘格位**。位置见 `BattleDriver.PlaySignal`。
            //    `effect_index.json` 的判定：三件都是 **对得上 / 高置信**（ratio 0.99 / 1.00 / 0.98）。
            { GainFaith,    "Faith UI Gain" },
            { GainSpirit,   "Waystone UI Gain" },
            { GainQuest,    "Quest UI Gain" },
            // 🔴 **2026-10-22（`W64` §B4）：`return` 这一行【已删】—— 原版回手链【0 粒子】。**
            //   判据（第一权威）= `BattleManager._ResolveRecallToHand_d__542__MoveNext.c` 逐句读：
            //   **0 处 `Instantiate` / 粒子**；`BattleCardUI__PlayBackToHandAnimation.c` 只 `Animation.Play`
            //   （那是一段**动画**，不是粒子）。原来挂的 `GSC RecallToHand` 现读是 **`GSC39` 的 `play`
            //   逐卡件**（`card_vfx_by_card.json`，`authoritative`），且该名在 `decomp_full` 全文 0 命中。
            //   ⇒ **已删**（2026-09-12 那次「三选一挑通用性最好的」本身就不是判据问题、是**事件有没有粒子**的问题）。
            //   ⚠️ 真要复刻回手表现，去解 `PlayBackToHandAnimation` 播的那条 clip（不是粒子）—— 那是另一笔账。
            //   判据 → `资料/普查产出_1022/W64_兜底审计_特效.md` §一 B4。
            // 🆕 **2026-10-22（P6 · `W64` §B8 收口）：`collect_waystone` 这一行【已接上】。**
            //   判据（第一权威）= `RemnantAeldari__CollectWaystoneEffect.c` 逐句读：先 `AudioCue.Play3D`，
            //   再 `Instantiate(collectParticles = +0xf, GetDeathParticlePosition(),
            //   该 prefab 自己 transform 的 rotation)` —— `+0xf` 是**残骸 prefab 自己序列化的
            //   `collectParticles`**（`RemnantBody3D Aeldari.waystoneCollectVFX` ⇒ 本件），
            //   随后 `AnimFXController.SetData(card)` + `Destroy(残骸体)`。
            //   **落点**：`RemnantAeldari__GetDeathParticlePosition.c` 取 `this + 0x68` 那个 Transform 的
            //   `position` —— P5 实读那一位 = **`wayStoneIdle`**，落到 prefab 节点
            //   `RemnantBody3D Aeldari / Idle / Spirit Stone Idle`（localPosition `(0, 1.203, 0)`）。
            //   我们的等价实现 = `BattleDriver.PlaySignal` 里那一支（**只此一处判据**）：
            //   `CardView.TryGetRemnantNode(CardView.RemnantIdleNodePath)` 取世界坐标，
            //   取不到才退回 `DropTargetWorld`（= 格心）**并出声**。
            //   ✅ **它今天在库里**：`效果库报告.tsv` 有 `Remnant Aeldari Collect particles`
            //   （自带 `AnimFXModuleMoveParticlesToTarget` = 「灵魂飞向目标」，语义对得上）。
            //   ⚠️ 原来挂的 `WaystoneCollect`（`assets_full` 里同名那件）**不是** collectParticles：
            //      `anim_address_map` 里它的 CardAnim 是 `WaystoneCollectAnim`（`cardanimsgeneral`），
            //      而**全池没有任何卡 SO 引用它**（`card_vfx_by_card.json` 0 命中）⇒ 同名不同物，⛔ 别再挂回去。
            //   ⏭ 历史（2026-10-22 之前）：这一行曾因「真件没导入」留白 + 出声；真件由
            //      `工具/extract_missing_shaders.py --prefabs` → `EffectExporter` → `EffectLibraryBuilder`
            //      导进来之后**本行才接上**。判据 → `资料/普查产出_1022/W64_兜底审计_特效.md` §一 B8。
            { CollectWaystone,  "Remnant Aeldari Collect particles" },
            // 🆕 2026-10-14（`A1816`）**伏击翻面**（`ambush_exit`）—— 🔴 **2026-10-22（`W64` §B7）也留空**：
            //   原版 trait 特效的取法 = `BattleManager.GetTraitEffect(trait, FromCode(5), army)`
            //   （`CardTraitCollection__GetTraitEffectObject.c` → `CardTrait._traitEffects[]` 里按
            //   `EffectTriggerType` 挑，再按阵营取 override）；而**那张 trait→effect 真值表本地没有**：
            //   `assets_full` 全库按 `traitEffects|TraitEffects|effectAnim` 搜 **0 命中**；
            //   `card_vfx_by_card.json` 的 `byKeywordTrigger` 42 个关键词里**没有 ambush**
            //   （它是由 `<关键词>Trait<Trigger>` 的**构词法**推的，而 `Ambush Effect` 不守那个构词法）。
            //   ⇒ 按「⛔ 不许保留孤儿」：**入口撤掉**（原来那件 `AmbushEffect` 的 GUID 在本机**找不到任何
            //   引用方**：卡 SO / CardAnim 的消费者 / trait SO 都判不了，见报告 U1）。
            //   ⚠️ **旁证（不足以当判据，但下一个人会想知道）**：`AmbushEffect` 的 AnimFX 音效 cue 是
            //   **`Trait Ambush`**、它换的材质是 **`Card 3d Dissolve Blend Image Ambush`**、
            //   `animinfo_lookup["AmbushEffect"]` 的 CardAnim 名就是 `Ambush Effect`（`tm 1.5`）——
            //   它**很可能就是** ambush 天赋那件，只是「哪一个 `EffectTriggerType` 槽」证不出来。
            //   ⏭ 补法：把 `CardTraitCollection.TraitCollection[]`（`TraitPairs` → `CardTrait` SO）导出来，
            //   查到 `DefinedTrait.ambush = 1200` 的 `FromCode(5)` 条目再接回（**需要那批 SO，本地没有**）。
            //   判据 → `资料/普查产出_1022/W64_兜底审计_特效.md` §一 B7/B8 + §三 U1。
        };

        /// <summary>阵营覆盖：**只剩远程攻击**按阵营换（🔴 2026-10-22 `W64` §B5：抽卡那两行已删 ——
        ///   原版抽卡链 0 粒子，见下面表里那段注释）。
        /// 🔴 **2026-10-21（W61 第 2 批 · 第 2 条）：登场那条【没有】按阵营的第二件了** ——
        ///   我们原来在登场拍另补一发「阵营召唤法阵」（`SummonCircleByFaction` 4 个候选：
        ///   Ember/Tide 是**我们挑的**、SaimHann/TauEmpire 是**按名字前缀推断的**），
        ///   而原版**没有「按阵营的登场法阵」这一层**（`decomp_full` 按 `SummonCircle` /
        ///   `summonParticles` / `SummonAnim` 搜 **0 命中**；`assets_full` 的 `*ByArmy*` 只有
        ///   `RangedAttackParticlesByArmy` / `CardFramesByArmy` 两条）⇒ **整层已删**。
        ///   登场那一拍有判据的只有：卡的淡入（`CardPrefab.normalSummon` → `VfxMap.Deploy`）+
        ///   逐卡 `summonAnimDelayed` + 落地扬尘。判据 → `资料/普查产出_1021/W61_战斗线_演出链对账.md` §2·1 #10。
        /// ⚠️ **没配到的阵营一律留白**（回 null ⇒ 不播）；🔴 **2026-10-16（账 `A1731`）：
        /// 留白不再静默** —— `Resolve` 走到底回 null 之前会 `Debug.LogWarning` 一次
        /// （每种 (事件, 阵营) 只说一次，见 `WarnNoFactionVariant`）。
        /// ⚠️ `AttackRanged` **不属于这一档**：它在 `ByEvent` 里有一件通用替代品 ⇒ 没配专属粒子的阵营走它，
        /// 不构成留白（`Resolve` 只在「`ByEvent` 里根本没有这个事件」时才出声）。
        /// 🆕 **2026-10-22（`W64`）现读：这张表里唯一还在用的事件是 `AttackRanged`** ——
        ///   `Draw` 两行已删（B5）。「按阵营配、这个阵营没有 ⇒ 出声」那条机制**仍然活着**，
        ///   自检的见证件由 `Draw` 换成…… **没有换**：直接拿一个**合成阵营**去问 `AttackRanged` 也行，
        ///   但 `AttackRanged` 在 `ByEvent` 里有通用件 ⇒ **它永远走不到那个出口**。
        ///   ⇒ `WarnNoFactionVariant` 今天**只剩 `Draw` 这一个可达见证**（`ByEvent` 里没有 `draw`、
        ///   `ByFaction` 里也没有）—— 见 `ResolveFallback` 的注释，⛔ 别把 `Draw` 也加进「原版无此槽」那张表。</summary>
        static readonly Dictionary<string, Dictionary<string, string>> ByFaction =
            new Dictionary<string, Dictionary<string, string>>
        {
            // 🆕 2026-09-29 **远程攻击按阵营**（判据齐）：原版 `RangedAttackParticlesByArmy.rangedParticlesByClan`
            //   只有 **4 个阵营**有专属粒子，其余走逐卡的 `customRangedAttackParticles`
            //   （**在远端包、本地判不了** ⇒ 那些阵营仍用 `ByEvent[AttackRanged]` 那件替代品）。
            //   键 = 我们的阵营名（与 `offensive_cards.json` 的 armyId 一一对应：
            //   10 Ultramarines / 20 Goff / 40 Sautekh / 50 BlackLegion）。
            //   出处：`数据/游戏数据/card_vfx_by_card.json` 的 `generic.attackRangedByArmy`（`conf: exact`），
            //   四件的 prefab 都在效果库里 ✓。
            //   🔴 **2026-10-22（`W64` §B5）：`draw` 这两行【已删】—— 原版抽卡链【0 粒子】。**
            //   判据（第一权威）＝`CardScript__CardDrawn.c` / `BattleManager._ResolveDrawCard_d__432__MoveNext.c`
            //   / `BattleManager__TurnStartCardDraw.c` / `CardScript._PlayPlayerCard…` 逐句读：
            //   **0 处粒子 `Instantiate`**（抽卡走 `AnimatorClipInfo` 的**动画**，不是粒子）。
            //   原来那两件 `UM_CardDraw` / `SAU_CardDraw` 的判据只有 **`vfx_wiring_j4.tsv:65`**
            //   —— 那是**我们自己的 Godot 复刻台账**，不是原版判据（见审计 §判据来源末条）。
            //   而且 `card_vfx_by_card.json` 现读：`UM_CardDraw` 落在 `UM45`/`UM53` 等 4 行的 **`play`** 桶
            //   （authoritative）、`SAU_CardDraw` **本地没有任何 CardAnim 指向它** ⇒ 两件各属别的桶。
            //   ⇒ 抽卡这一拍**留白**（`Resolve(Draw, …)` 恒 null；`ResolveFallback` 走 `WarnNoFactionVariant`
            //   出声 —— 那条 `A1731` 的机制没动，仍然由 `Draw` 当见证）。
            //   ⏭ 要复刻抽卡表现就去解 `AnimatorClipInfo` 那条**动画**链（U3），别拿 play 件顶。
            //   判据 → `资料/普查产出_1022/W64_兜底审计_特效.md` §一 B5。
            { "Ultramarines", new Dictionary<string, string> {
                  { AttackRanged, "BulletImpact_2shot_trail" } } },
            { "Goff",         new Dictionary<string, string> {
                  { AttackRanged, "BulletImpact_2shot_trail_ork_NEW" } } },
            { "Sautekh",      new Dictionary<string, string> {
                  { AttackRanged, "NecronGauss" } } },
            { "BlackLegion",  new Dictionary<string, string> {
                  { AttackRanged, "BulletImpact_2shot_trail_chaos" } } },
        };

        // 🔴 **2026-10-21（W61 第 2 批 · 第 2 条）：`SummonCircleOf` / `SummonCircleByFaction` 整层已删。**
        //   它们原来给「登场」补一发**按阵营的召唤法阵**（Ember/Tide 是我们挑的、SaimHann/TauEmpire 按名字前缀推），
        //   而原版**没有这一层**：`decomp_full` 按 `SummonCircle` / `summonParticles` / `SummonAnim` 搜 0 命中；
        //   `assets_full` 的 `*ByArmy*` 只有 `RangedAttackParticlesByArmy` / `CardFramesByArmy` 两条；
        //   本地那 4 个候选其实是**某些卡的技能动画**（逐卡 `cardAbilities[].abilityLogic[].cardAnimations[]`）。
        //   ⇒ 登场那一拍只保留有判据的三件：卡淡入（`normalSummon` → `VfxMap.Deploy`）· 逐卡
        //   `summonAnimDelayed` · 落地扬尘。判据 → `资料/普查产出_1021/W61_战斗线_演出链对账.md` §2·1 #10。
        //   ⛔ 别再给它补一件「看起来像法阵」的替代品（那是原版没有的额外段）。

        /// <summary>逐卡覆盖（键 = **卡 id**；事件名同 <see cref="Resolve"/>）。
        /// **2026-10-14 起它由 `Resources/card_vfx_by_card.json` 载入**（见 <see cref="EnsureLoaded"/>），
        /// ⛔ 不再是写死的空字典。
        /// <para>值 = 该 (卡, 事件) 下**要播的那些候选**（按原版 `abilityLogic` 数组顺序）。
        /// 🆕 2026-10-19（B6）：元素从裸 `string` 变成 <see cref="CardPick"/>（`N` = prefab 名 ·
        /// `A` = `CardAnim` 名）—— 播放名仍是 `N`，加 `A` 只为了把候选钉到它自己那条 `AnimInfo`
        /// （逐候选时间，见 <see cref="ResolveAllTimed"/>）。</para>
        /// <para>🔴 **2026-10-19（B3 · `T13`）就地改型：`string` → `List&lt;string&gt;`** ——
        /// 判据（第一权威）＝原版 `CardAbility.cs:27-29` 的字段注释：
        /// 「`randomAbilityChoice` 为假 ⇒ **all are triggered in order**」；现读 **1,495/1,496** 个
        /// `CardAbility` 是假 ⇒ 同一 (卡, 事件) 下的**多条** `abilityLogic` 本来就应该**全播**。
        /// 原来这张表把每个 (卡,事件) 压成一个 string（按 `confPriority` 取第一个）⇒ **丢了 508 个
        /// 权威 prefab / 375 张卡**（判据与统计 → `资料/普查产出_1019/V2_报告.md` §3·2/§3·3）。
        /// <para>**表里存什么**（三条都重要，⛔ 别自己再收一遍）：
        /// · 只留**最优置信度那一档**（`confPriority` 里 rank 最小的）；
        /// · **只有该档是 `authoritative` 时才保留同档的全部候选**；我们自己的推断档
        ///   （`inferred-*`）**保持原行为只留第一条** —— V2 §3·3 最后那条：
        ///   「别把推断档也全播」；
        /// · 同档里**同名重复也保留**（现读 59 行）—— 那是两条 `abilityLogic` 指向同一件 prefab，
        ///   原版 `AbilityLogic.PlayAnim` 会各播一次；⛔ 别自作主张去重（去重 = 少播）。
        /// 顺序 = JSON 里 `es` 的数组顺序 = **原版 `abilityLogic` 的索引顺序**
        /// （V2 现读：436 行多候选里 **435 行逐位相同**，唯一 1 行是 `remnantBodyPrefab` 分键问题）。
        /// </para>
        /// <para>🔴 **2026-10-15 就地订正（铁律 5 · 账 `A2082`）**：这一行原来写着
        /// 「键 = 卡 id，**与 `CardDef.Id` / `BattleEvent.CardId` 同一套**」—— **后半句是错的**，
        /// 而且正是它掩盖了那个缺陷：`BattleEvent.CardId` 里装的是 **`CardDef.Name`（卡名）**，
        /// 于是调用点传卡名来查这张 id 键的表 ⇒ **恒不命中**。
        /// ⇒ 现在**这半句已作废**：查得到 id 走 id，查不到就按卡名换算（<see cref="ResolveCardIdOf"/>）。
        /// ⛔ **别再把这句写回来** —— 铁律 5 记的就是这类「听着对、实际错」的注释。</para></summary>
        /// <summary>🆕 2026-10-19（B6）：表里一条候选的**两个 join 键**。
        /// <para>`N` = prefab 名（= JSON 的 `es[].n`，也是今天真正拿去 `CardEffects.Fire` 的名字）；
        /// `A` = 原版 `CardAnim` 资产名（= `es[].a`）。**`A` 是和 `CardDef.CardAnimTime.Anim`
        /// 精确对上的那一把键**（`资料/普查产出_1019/E12_报告.md` §2·2/§5·1）——
        /// 多候选按 `t` 错开时必须靠它把候选钉到**它自己那条** `AnimInfo` 上，⛔ 不能只按 prefab 名
        /// （一张卡里同一个 `CardAnim` 可以出现多次，现读 59 行；只按名会把时间配错一条）。</para>
        /// <para>`A` 为空 = 这条候选不属于 `cardAnimations[]`（来自卡 SO 的顶层字段 / 我们的推断档）
        /// ⇒ 它**没有逐候选时间**，那时退回按 `N` 对（E12 §5·1 的第 2 顺位）。</para>
        /// <para>🆕 **2026-10-19（W · 多候选 ③）`rc/ai/gj/gn`**：`randomAbilityChoice` 那条路的四个数
        /// （JSON 的 `es[].rc/ai/gj/gn`，只有 `rc == 1` 才写）。
        /// 判据（第一权威）= `RawCardScript__TriggerAbility.c:75-97` 逐句读：
        /// `CardAbility.randomAbilityChoice`(0x48) 为真 且 `abilityLogic`(0x50) 的 `Count &gt; 1` 时，
        /// 原版用 `BattleManager.GetRandomInt(0, Count)` **抽一个 `abilityLogic` 组**播；
        /// 为假（或 Count ≤ 1）⇒ **按序全播**。
        /// · `rc` = 1 = 这条候选所属的 `CardAbility` 是「随机抽一组」；
        /// · `ai` = `cardAbilities[i]` 的下标（同一事件里可能混着多条 CardAbility，**每条各抽各的**）；
        /// · `gj` = `abilityLogic[j]` 组号（这条候选属于哪一组）；
        /// · `gn` = `abilityLogic` 的**完整组数**（⚠️ 不是「有动画的组数」—— 原版抽中没动画的组
        ///   也是合法结果，等于这一下没有特效）。
        /// 消费 = <see cref="ResolveAllTimed"/> 的 `rng` 参数（只有传了 rng 才真抽；不传 = 旧行为全播）。</para></summary>
        public struct CardPick
        {
            public string N;
            public string A;
            public int rc, ai, gj, gn;
        }

        static readonly Dictionary<string, Dictionary<string, List<CardPick>>> ByCard =
            new Dictionary<string, Dictionary<string, List<CardPick>>>();

        /// <summary>🆕 2026-10-15（账 `A2082`）：**卡名 → 卡 id**（一个名字可能落在多张卡上，
        /// 全池 3 组重名 ⇒ 值是**列表**）。与 <see cref="ByCard"/> **同一次载入**建立。
        /// 用途见 <see cref="ResolveCardIdOf"/>。</summary>
        static readonly Dictionary<string, List<string>> _byName = new Dictionary<string, List<string>>();

        /// <summary>卡 id → 阵营（重名时用它把名字钉到唯一那张卡上）。
        /// ⚠️ 与 <see cref="ByCard"/> 是**同一次载入**的产物，⛔ 别当成两张可以各自刷新的表。</summary>
        static readonly Dictionary<string, string> _factionOfId = new Dictionary<string, string>();

        /// <summary>「名字重名、阵营也判不出唯一」的**出声去重**（每张卡只说一次，别每帧刷屏）。</summary>
        static readonly HashSet<string> _warnedAmbiguous = new HashSet<string>();
        const int AmbiguousWarnCap = 20;

        // ============================ 🔴 2026-10-16（账 `A1731`）：**按阵营配的东西，这个阵营没有 ⇒ 出声** ============================
        //
        // **背景（本仓红线：不许静默失败）**：`ByFaction`（远程攻击 / 抽卡）是**按阵营配**的表 ——
        // 没配到的阵营解析出来是 `null`，而
        // `CardEffects.Fire(null, …)` 的第 1 行就是 `if (string.IsNullOrEmpty(effectName)) return;`
        // （那一行是给「通用兜底也没配」用的）⇒ 玩家看到「这一拍什么都没发生」，
        // 而日志里**一个字都没有**，「原版这一档我们没有」与「表里漏抄了」分不出来。
        // ⇒ 本类的两个**按阵营**的出口在回 `null` 之前一律出声（每次会话每种 (东西, 阵营) 只说一次）。
        //
        // ⚠️ **只在「播放口径」出声**：`Resolve(evt)` 不带阵营（= 探测口径，见 `Unmapped`）、以及
        //    `ResolvesForSomeFaction` / `Describe` 这两个**枚举探针**（它们问的是「哪一档配过」，
        //    不是「现在播什么」）⇒ 都不出声（探针期间 `_probeDepth > 0`，见那两处的 `try/finally`）。
        // ⚠️ **不硬凑一件替代品** —— 出声只说明「这里留白」，替代品是**判据**问题，不在这条账里。
        static readonly HashSet<string> _warnedNoFaction = new HashSet<string>();
        const int NoFactionWarnCap = 32;
        static int _probeDepth;

        /// <summary>自检用：账 `A1731` 那条「留白要出声」已经报过几种 (东西, 阵营)。
        /// 同族先例 = `DeckRuntime.UiCardbackArtWarnCount`（同样是「去重之后的警告计数」）。
        /// ⚠️ 只增不减（没有 reset）—— 自检要断的是「报了没有」，不是「报了几次」。</summary>
        public static int NoFactionVariantWarns { get { return _warnedNoFaction.Count; } }

        /// <summary>某件**按阵营**配的东西，这个阵营没有候选 ⇒ 出声（⛔ 不静默留白，也不硬凑一件替代品）。
        /// `what` = 那件东西的名字（事件名 / `阵营召唤法阵（deploy）`），同时做去重键与日志里的主语。</summary>
        static void WarnNoFactionVariant(string what, string faction, string detail)
        {
            if (string.IsNullOrEmpty(what) || string.IsNullOrEmpty(faction)) return;
            if (_probeDepth > 0) return;                                    // 枚举探针不掺进播放日志
            if (_warnedNoFaction.Count >= NoFactionWarnCap) return;         // 上限（别刷屏）
            if (!_warnedNoFaction.Add(what + "|" + faction)) return;        // 每种 (东西, 阵营) 只说一次
            Debug.LogWarning($"[Vfx] `{what}` 在阵营 `{faction}` 上**没有候选** ⇒ 这一拍**留白**"
                           + "（⛔ 不拿别的阵营的顶上、也不硬凑一件）。" + detail);
        }

        /// <summary>运行时那份逐卡表的 Resources 路径（**没有扩展名**）。
        /// 由 `工具/gen_card_vfx_by_card.py` 与 `数据/游戏数据/card_vfx_by_card.json` **同一次运行**产出。</summary>
        public const string CardTableResourcePath = "card_vfx_by_card";

        /// <summary>🆕 2026-10-19（B3 · `T13`）：**唯一允许「同档全播」的置信度名**。
        /// 与生成器写进 `confPriority[0]` 的那个字符串逐字相同（`工具/gen_card_vfx_by_card.py`），
        /// ⛔ 别在别处再写一遍字面量（判据只此一处）。</summary>
        public const string ConfAuthoritative = "authoritative";

        static bool _triedCardTable;
        /// <summary>`Resources` 里那份表载进来几张卡（0 = 没载到/空表 —— 那时逐卡那一档**恒不命中**，
        /// 退回 <see cref="ByFaction"/> / <see cref="ByEvent"/> 的老行为，**并已经出过声**）。</summary>
        public static int CardTableCards { get { EnsureLoaded(); return ByCard.Count; } }
        public static int CardTableEntries { get; private set; }

        // ================== 🆕 2026-10-23（`W63` 收口）：两个「技能动画」桶的账 ==================
        //  语义与口径见 <see cref="UnitAttack"/> 那一段。⚠️ 它们**不计入** `CardTableEntries`
        //  （那一个是「原版逐卡真值行数」的口径，重复计会让历史断言漂）。
        /// <summary>`UnitAttack` 桶里的**行数**（不是卡数；一张卡可以有两拍 —— DA22 / SAU31 各 2 条）。
        /// 池内现读 **13**。</summary>
        public static int CardTableUnitAttackEntries { get; private set; }
        /// <summary>`UnitDamaged` 桶里的行数。池内现读 **4**。</summary>
        public static int CardTableUnitDamagedEntries { get; private set; }
        /// <summary>两个桶的行数合计（自检里那个 **17**）。</summary>
        public static int CardTableUnitAnimEntries { get { return CardTableUnitAttackEntries + CardTableUnitDamagedEntries; } }
        static readonly Dictionary<string, List<string>> _unitAnimCards = new Dictionary<string, List<string>>();
        /// <summary>某个桶里**有候选的卡 id**（拷贝；自检逐卡查命中用）。表里没有 ⇒ 空数组。</summary>
        public static string[] UnitAnimCardIds(string evt)
        {
            EnsureLoaded();
            List<string> l;
            return _unitAnimCards.TryGetValue(evt ?? "", out l) ? l.ToArray() : new string[0];
        }
        /// <summary>🆕 2026-10-19（B3 · `T13`）：**会一次播多件**的 (卡, 事件) 行数
        /// （= `ByCard` 里值列表长度 &gt; 1 的行）。🔴 **2026-10-23 订正（铁律 5）**：现读**全表 500 行**
        /// （633 条 `inferred-keyword` 升 `authoritative` 后；旧读数 382 当时含推断档）。
        /// 数据会随生成器重跑变，断言里别写死。</summary>
        public static int CardTableMultiEntries { get; private set; }
        /// <summary>🆕 同上：表里**要播的候选总数**（= Σ 每个 (卡,事件) 行列表长度）。
        /// 它 − <see cref="CardTableEntries"/> = 「全播比原来多播出去的件数」。</summary>
        public static int CardTableCandidates { get; private set; }
        /// <summary>🆕 同上：**过滤前**有多少行是「同档多候选」（含我们自己的 `inferred-*` 平票）。
        /// 🔴 **2026-10-23 订正（铁律 5）**：现读 **500 行**，与 <see cref="CardTableMultiEntries"/> **相等**
        /// —— 因为 633 条 `inferred-keyword` 已升权威、运行时表里**非权威平票行 = 0**（旧读数 505/382）。
        /// 自检现在用「同一份 JSON 独立重算」+ `CardTableMultiEntriesAll == MultiEntries + 非权威平票行数`
        /// （B3·T13 换靶子，见 `普查产出_1023b/B3T13换靶子_执行报告.md`）。</summary>
        public static int CardTableMultiEntriesAll { get; private set; }
        /// <summary>🆕 `A2082`：卡名索引里有几个名字（自检用 —— 它 = 0 就说明生产路径那一档查不动）。</summary>
        public static int CardTableNames { get { EnsureLoaded(); return _byName.Count; } }

        // ---- 运行时那份 JSON 的形状（**数组，不是字典** —— `JsonUtility` 不支持字典）----
        /// <summary>🆕 2026-10-19（W · 多候选 ③）：`rc/ai/gj/gn` 只在 `rc == 1` 的候选上出现
        /// （= 该卡那条 `CardAbility.randomAbilityChoice` 为真）—— 见 <see cref="CardPick"/> 的 doc。
        /// 其余条目**不写这四个键**，`JsonUtility` 缺字段给默认 0。</summary>
        [System.Serializable] public class RtEntry
        {
            public string n; public string c; public string a;
            public int rc; public int ai; public int gj; public int gn;
        }
        [System.Serializable] public class RtEvent { public string ev; public RtEntry[] es; }
        /// <summary>🆕 2026-10-15（账 `A2082`）补 `nm` / `fa`：**卡名 + 阵营**。
        ///
        /// 为什么必须带卡名：<see cref="Resolve"/> 的 `cardId` 参数在**生产路径上装的是卡名**
        /// （`BattleEvent.CardId` 的 doc = 「卡名」·`RuleEngine/Core/BattleEvent.cs:201`；
        /// 发射点 `RuleCore.UseAbility` 传的是 `u.Name`），而本表的主键是 **id**
        /// ⇒ 在这之前**逐卡这一档恒不命中**（2026-10-15 实测：1017/1017 命中 id、0/1017 命中卡名）。
        ///
        /// ⚠️ **`fa`（阵营）不是装饰**：全池有 **3 组重名**（`Terminator` · `Terminator Champion` ·
        /// `Maulerfiend`，各跨两个阵营）⇒ 只有 `卡名 + 阵营` 唯一。少了它就只能猜 —— 本件**不猜**，
        /// 判不了时**出声并返回 null**（见 <see cref="ResolveCardIdOf"/>）。
        ///
        /// ⚠️ **两个键都可能缺**（原版那张卡查不到卡名时生成器不写）⇒ 取用时一律判 null。</summary>
        [System.Serializable] public class RtCard { public string id; public string nm; public string fa; public RtEvent[] events; }
        [System.Serializable] public class RtFile
        {
            public string[] confPriority;      // 权威优先；不认识的一律排最后
            public RtCard[] cards;
            public int cardCount;
            public int entryCount;
        }

        /// <summary>🔴 **2026-10-14（第二十七会话）：把逐卡表从 `Resources` 载进来（懒加载、只做一次）。**
        ///
        /// **为什么要它**：在这之前 <see cref="ByCard"/> 是一句
        /// `new Dictionary<…>()` 写死的空表（注释写着「留给以后配」），而 `工具/gen_card_vfx_by_card.py`
        /// 早在造一份 2.2 MB 的逐卡表 —— **全仓没有一行代码读过它**（`grep card_vfx_by_card` 只有 4 处注释）。
        /// ⇒ 原版真值（卡 SO 的 `customRangedAttackParticles` / `customDeathParticles` /
        /// `cardAbilities[].abilityLogic[].cardAnimations[]` 解出来的 **3326 条**）**一条都没进游戏**。
        ///
        /// **数据**：`Resources/card_vfx_by_card.json`（**2026-10-16 现读**：**350,789 B · `cardCount` 1017 ·
        /// `entryCount` **2402** · `audioCount` 621 · `noPrefabCount` 22 · `generatedAt` 2026-10-15 18:50:58**），
        /// 🔴 **2026-10-16 就地订正（铁律 5）**：本行原来写「355,451 B · `entryCount` **2473** · `generatedAt` 18:27」——
        ///   那是**同一天更早那一版**的读数（生成器不幂等，`guid_map.tsv` 更新过就会变）。
        ///   ⇒ **这一行里的数一律现读**（`json.load(...)["entryCount"]`），⛔ 别把哪个具体数写死在这儿当判据。
        /// 与 `数据/游戏数据/card_vfx_by_card.json`（全量、给工具看）**由同一次运行产出** ⇒ 不会分叉。
        /// ⚠️ **2026-10-14 就地订正（铁律 5 · 账 `A1833`）**：这一行原来写「**240 KB · 979 张卡**」——
        ///    那是**当天上午那一版**的读数（246,036 B），下午 `A1817`（逐卡音效）重跑生成器之后就变了
        ///    （一度是 295,821 B / 989 卡 / 2323 条 / `generatedAt` 2026-10-14 12:24）。
        /// 🔴 **2026-10-15 再订正（账 `A2082` + `A2095`）**：上面的数又变了两轮 ——
        ///    ① `02:08` 更新过 `资料/普查产出_第二十七会话/guid_map.tsv`，08:28 那一版还是按旧表算的；
        ///       本会话重跑生成器后 `entryCount` **2417 → 2473（+56）**，**全部落在我们自己的
        ///       `inferred-keyword` 推断档**（556 → 612），真值档 `authoritative` **1844 → 1844 一条没动**
        ///       （判据 = 逐 (卡,事件) 行对比，见 `资料/待办判据_*` 里 `A2082` 一节）；
        ///    ② 本会话给运行时那份**加了 `nm`（卡名）/ `fa`（阵营）两个键**（逐卡按名查要用，见 <see cref="RtCard"/>）。
        /// ⛔ **别再把这几个数当成手写常量** —— 要看真值就读文件头那几个字段（本行的数**也会再变**）。
        /// 🔑 **判幂等**：生成器连跑两次，除 `generatedAt` 外**逐字节一致**（本会话实测）。
        ///
        /// **取哪一条**：同一条 (卡, 事件) 可能有多个候选（原版真值 + 我们早先按名/按关键词推的），
        /// 按 JSON 自带的 `confPriority`（`authoritative` 最前）**只留最优那一档**。
        /// 🆕 **2026-10-19（B3 · `T13`）**：最优档是 `authoritative` 且同档有多个 ⇒ **全都留**（按序全播，
        /// 判据 = 原版 `CardAbility.cs:27-29` 的 `randomAbilityChoice` 注释）；推断档之间平票仍只留第一条。
        /// ⚠️ 这与 `gen_card_vfx_by_card.py` 打印的优先级**是同一份**（数据自带，⛔ 别在这儿再写一张表）。
        ///
        /// **载不到怎么办**：**出声 + 退回老行为**（`ByFaction` → `ByEvent`），⛔ **不静默**。
        /// ⚠️ 效果名**不在这里校验是否在效果库里** —— 那是 `BattleDriver` 那一侧的事
        /// （缺的那些会走它自己的「找不到就跳过」并出声）。
        /// </summary>
        public static void EnsureLoaded()
        {
            if (_triedCardTable) return;
            _triedCardTable = true;                       // 失败也只试一次（别每帧重试）
            var asset = Resources.Load<TextAsset>(CardTableResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[Vfx] `Resources/{CardTableResourcePath}.json` **没载到** ⇒ "
                               + "逐卡特效这一档**不起作用**，退回「按阵营 + 按事件」的老兜底。"
                               + "重跑：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_card_vfx_by_card.py");
                return;
            }
            RtFile dto;
            try { dto = JsonUtility.FromJson<RtFile>(asset.text); }
            catch (System.Exception e)
            {
                Debug.LogError($"[Vfx] `Resources/{CardTableResourcePath}.json` 解析失败：{e.Message} ⇒ "
                             + "逐卡特效这一档**不起作用**（退回老兜底）");
                return;
            }
            if (dto == null || dto.cards == null)
            {
                Debug.LogError($"[Vfx] `Resources/{CardTableResourcePath}.json` 是空的 / 形状不对 ⇒ 逐卡特效不起作用");
                return;
            }
            // `conf` → 优先级序数（不认识的一律排最后；⛔ 不猜它比谁高）
            var prio = new Dictionary<string, int>();
            if (dto.confPriority != null)
                for (int i = 0; i < dto.confPriority.Length; i++)
                    if (!string.IsNullOrEmpty(dto.confPriority[i])) prio[dto.confPriority[i]] = i;
            int worst = prio.Count;

            foreach (var c in dto.cards)
            {
                if (c == null || string.IsNullOrEmpty(c.id)) continue;
                // 🆕 `A2082`：**卡名索引先建** —— 不看 `events` 有没有（只有语音的卡也要能被名字查到，
                //   否则「按名查得到、按 id 查不到」这种半通状态会再长出来）。
                if (!string.IsNullOrEmpty(c.nm))
                {
                    List<string> lst;
                    if (!_byName.TryGetValue(c.nm, out lst)) { lst = new List<string>(); _byName[c.nm] = lst; }
                    if (!lst.Contains(c.id)) lst.Add(c.id);      // 同一个 id 别重复加
                }
                if (!string.IsNullOrEmpty(c.fa)) _factionOfId[c.id] = c.fa;
                if (c.events == null) continue;
                Dictionary<string, List<CardPick>> m;
                if (!ByCard.TryGetValue(c.id, out m)) { m = new Dictionary<string, List<CardPick>>(); ByCard[c.id] = m; }
                foreach (var ev in c.events)
                {
                    if (ev == null || string.IsNullOrEmpty(ev.ev) || ev.es == null) continue;
                    // 🆕 2026-10-19（B3 · `T13`）：**同档平票全收**（判据见 `ByCard` 的 doc）。
                    //   `picks` 的语义 = 「最优档、按数组顺序」；遇到更高档就整个换掉（低档的丢掉）。
                    // 🆕 2026-10-19（B6）：值改成 `CardPick`（`n` + `a`）—— `a` 是逐候选时间的 join 键
                    //   （见 `CardPick` 的 doc）；播放用的名字仍是 `N`，对外 API 不变。
                    string best = null; int bestRank = int.MaxValue;
                    var picks = new List<CardPick>();
                    foreach (var e in ev.es)
                    {
                        if (e == null || string.IsNullOrEmpty(e.n)) continue;
                        int rank;
                        if (!prio.TryGetValue(e.c ?? "", out rank)) rank = worst;
                        if (rank < bestRank)
                        {
                            bestRank = rank; best = e.n; picks.Clear();
                            picks.Add(ToPick(e));
                        }
                        else if (rank == bestRank) picks.Add(ToPick(e));
                    }
                    if (best == null) continue;
                    // 🔴 **只播最优档那一档，且只播 `authoritative` 时全播**（V2 §3·3 末条）：
                    //   我们自己的推断档（`inferred-*`）之间平票**不扩**—— 那不是原版真值，
                    //   全播只会多放我们猜出来的东西。
                    bool authoritative = dto.confPriority != null
                                      && bestRank >= 0 && bestRank < dto.confPriority.Length
                                      && dto.confPriority[bestRank] == ConfAuthoritative;
                    if (picks.Count > 1) CardTableMultiEntriesAll++;      // 过滤前（含推断档平票）
                    if (!authoritative && picks.Count > 1) picks.RemoveRange(1, picks.Count - 1);
                    // 🆕 2026-10-23（`W63` 收口）：**这两族的行【改归】两个技能动画桶，⛔ 不再写回旧桶**
                    //   （判据/口径见 <see cref="UnitAttack"/> 那一段）。🔴 **必须真的「搬」而不是「抄」**：
                    //   生产路径 `ScheduleAttackPresentation` 会用 `attack_melee` 逐卡解一次并在
                    //   **命中帧**播出去（`ResolveAllTimed`）—— 只加新桶不动旧桶的话，同一件
                    //   `Hammer_Slam_SW` 会**当头一击的粒子播一次、再当技能动画播一次**（原版没有的第二下）。
                    //   `hit` 那 4 条同理（受击那一拍原版一件粒子都不播，见 `PlaySignal` 那段注释）。
                    //   ⚠️ **`attack_melee` 里另外 2 条 `inferred-name`（EC29/SOR8）留在旧桶** ——
                    //   它们**不是** `UnitAttack(50)` 触发的（按名字猜的），不在本件口径内。
                    bool movedToUnitAnim = false;
                    if (ev.ev == AttackMelee && authoritative)
                    {
                        m[UnitAttack] = picks;
                        CardTableUnitAttackEntries += picks.Count;
                        if (!_unitAnimCards.TryGetValue(UnitAttack, out var ua))
                            _unitAnimCards[UnitAttack] = ua = new List<string>();
                        if (!ua.Contains(c.id)) ua.Add(c.id);
                        movedToUnitAnim = true;
                    }
                    else if (ev.ev == Hit)
                    {
                        m[UnitDamaged] = picks;
                        CardTableUnitDamagedEntries += picks.Count;
                        if (!_unitAnimCards.TryGetValue(UnitDamaged, out var ud))
                            _unitAnimCards[UnitDamaged] = ud = new List<string>();
                        if (!ud.Contains(c.id)) ud.Add(c.id);
                        movedToUnitAnim = true;
                    }
                    if (!movedToUnitAnim) m[ev.ev] = picks;
                    CardTableEntries++;
                    CardTableCandidates += picks.Count;
                    if (picks.Count > 1) CardTableMultiEntries++;
                }
            }
            Debug.Log($"[Vfx] 逐卡特效表已载入：{ByCard.Count} 张卡 / {CardTableEntries} 条"
                    + $"（`Resources/{CardTableResourcePath}.json`，按 `confPriority` 取权威优先）"
                    + $"；卡名索引 {_byName.Count} 个名字（🆕 `A2082`：生产路径传的是**卡名**，"
                    + "没有这个索引逐卡那一档就恒不命中）"
                    + $"；🆕 `T13` 全播：{CardTableMultiEntries} 条会播多件 / 候选共 {CardTableCandidates} 件"
                    + "（= 最优档 `authoritative` 的同档平票，按原版 `abilityLogic` 顺序）");
        }

        /// <summary>🆕 2026-10-19（W）：运行时表的一条 → <see cref="CardPick"/>
        /// （`rc/ai/gj/gn` 只有 `randomAbilityChoice` 那条 CardAbility 的候选才非零；
        /// 见 <see cref="CardPick"/> 的 doc）。**只此一处**做这个映射 —— `EnsureLoaded` 的两个分支都用它。</summary>
        static CardPick ToPick(RtEntry e)
        {
            return new CardPick { N = e.n, A = e.a, rc = e.rc, ai = e.ai, gj = e.gj, gn = e.gn };
        }

        // ================== 🆕 2026-10-22（`W64` §B9）：登场淡入的**稀有度分档** ==================
        // 判据（原版）：`CardPrefab` 上 `normalSummon` / `legendarySummon` **成对存在**
        //   （`数据/游戏数据/card_vfx_by_card.json` 的 `generic.byEvent.deploy`：4 条 `conf: exact`，
        //    前两条就是这两个 prefab）；挑选规则 = **传说档（`cardRarity == 4`）** —— 与
        //   `BattleDriver.GetInvokeCue(cardRarity)`（`invokeCUE` / `invokeCUELegendary` 那一对）**同一套规则**，
        //   也与 `BattleDriver.MountSpawnParticlesUnderToBoardFx` 同一个形状（`def.rarity == "legendary"`）。
        // ⚠️ 原版那一支的**完整**条件是 `HasCurrentTrait(0x4b0) || cardRarity == 4`
        //   （`CardSetup.c:286/290`；`0x4b0` = `DefinedTrait.ambush = 1200`，见 `d:/2/tools/il2cpp_out/dump.cs`）
        //   —— **伏击那半个条件本节【没接】**：审计 §三 U4 明说「`CardScript.CardToMinionAnim` 那个 switch
        //   的方法体没逐句跟到」⇒ ⛔ 不猜，只按**稀有度**分档（传说以外的卡行为一个字没变）。
        // ⚠️ 稀有度从哪来：`cardIdOrName` 就是 `BattleEvent.CardId`（= **卡名**）⇒ 用
        //   `RuleEngine.CardDatabase`（进程级缓存；战斗开局 `BattleDriver._pool` 已经载过那一份 ⇒ 现成）
        //   查 `CardDef.Rarity`。查不到那张卡 ⇒ **不缓存、也不猜**，直接走普通档。
        static readonly Dictionary<string, bool> _legendaryOfCard =
            new Dictionary<string, bool>(System.StringComparer.Ordinal);

        /// <summary>这张卡是不是**传说档**（`CardDef.Rarity == "legendary"`，忽略大小写）。
        /// 查不到这张卡 ⇒ `false`（= 普通档；⛔ 不猜、也**不缓存**，补上卡表之后还能再查）。</summary>
        static bool IsLegendaryCard(string cardIdOrName, string faction)
        {
            if (string.IsNullOrEmpty(cardIdOrName)) return false;
            string key = cardIdOrName + "|" + (faction ?? "");
            bool legendary;
            if (_legendaryOfCard.TryGetValue(key, out legendary)) return legendary;
            var pool = CardDatabase.Load();
            var card = CardDatabase.Find(pool, cardIdOrName, faction);
            if (card == null) card = CardDatabase.FindById(pool, cardIdOrName);   // 也收 id（自检会按 id 调）
            if (card == null) return false;
            legendary = string.Equals(card.Rarity, "legendary", System.StringComparison.OrdinalIgnoreCase);
            _legendaryOfCard[key] = legendary;
            return legendary;
        }

        /// <summary>登场淡入按稀有度挑档：**传说 ⇒ <see cref="DeployLegendary"/>**；其余 / 判不出 ⇒ `null`
        /// （= 交回 `ByEvent` 的普通档 <see cref="DeployDefault"/>，行为不变）。⛔ 只对 `Deploy` 生效。</summary>
        static string DeployTierFor(string evt, string faction, string cardIdOrName)
        {
            if (evt != Deploy) return null;
            return IsLegendaryCard(cardIdOrName, faction) ? DeployLegendary : null;
        }

        /// <summary>查一个事件该播什么。查不到返回 null（调用处会跳过）。
        ///
        /// <para>🔴 <b>2026-10-15（账 `A2082`）：`cardId` 这个参数<b>两把键都收</b> —— 卡 id <b>或</b> 卡名。</b>
        /// 修的是这么一个缺陷：本表的主键一直是**卡 id**，而<b>生产路径上唯一那个调用点传的是卡名</b>
        /// （`BattleDriver.PlaySignal` → `CardEffects.FireEvent(evt, pos, faction, e.CardId)`，
        /// 🆕 2026-10-19（B3）起生产路径改走 `BattleDriver.FireEventAll` → 本类的 `ResolveAll`；
        /// 而 `BattleEvent.CardId` 装的是 `CardDef.Name`）⇒ 在修之前**逐卡这一档恒不命中**，
        /// 1,009 张卡 / 3,498 条真值**一条都没进过画面**，一律掉到按阵营/按事件的兜底。
        /// （实测：运行时表 1017 个键 **1017/1017 命中卡池 `id`、0/1017 命中卡名**。）
        ///
        /// ⚠️ 为什么在**这里**收口、而不是在调用点把卡名换成 id：调用点有 3 处
        /// （`BattleDriver.cs:11384/11386/11404`），换一处漏一处就是同一个 bug 再长一遍；
        /// 而这件是**所有人都会经过的唯一一道门**。同族先例 = `AbilityAnimDurations.TryGet`
        /// （它自 2026-10-13 起就是「先查 id、再查卡名」——两条路各写一套才是真隐患）。
        ///
        /// ⚠️ **重名不猜**：3 组重名（见 <see cref="RtCard"/> 的 doc）用 `faction` 钉到唯一那张卡；
        /// 钉不出来 ⇒ **出声 + 返回 null**（宁可不播，也不给错特效）。</summary>
        public static string Resolve(string evt, string faction = null, string cardId = null)
        {
            if (string.IsNullOrEmpty(evt)) return null;

            // ⚠️ 逐卡那一档**要先载表**（2026-10-14 起它不再是空字典 —— 见 `EnsureLoaded` 的说明）。
            //    它排在最前面：**原版真值优先于我们按阵营/按事件的兜底**。
            //    （查询本身收口在 `TryResolvePerCard` —— 与 `ResolvePerCard` **共用一份实现**，
            //      ⛔ 别在这儿再抄一遍那个「id 或卡名」的换算。）
            string perCard;
            if (TryResolvePerCard(evt, faction, cardId, out perCard)) return perCard;
            // 🆕 2026-10-22（`W64` §B9）：`Deploy` 还要问一次**稀有度档**（传说 ⇒ `… Legendary`）。
            //   ⚠️ 排在兜底链**之前**、逐卡那一档**之后**：逐卡的 `summonAnimDelayed` 是**另一件**
            //      （原版是叠加的第二件），不能在它命中时抢走它。
            string tier = DeployTierFor(evt, faction, cardId);
            if (tier != null) return tier;
            // 🆕 2026-10-19（B3）：兜底那一段拆成 `ResolveFallback` —— `ResolveAll` 也要用它，
            //   ⛔ 别在两处各写一遍「ByFaction → ByEvent → 留白出声」。
            return ResolveFallback(evt, faction);
        }

        /// <summary>🆕 2026-10-19（B3 · `T13`）：**这条 (卡, 事件) 要播的【全部】prefab**（按原版
        /// `abilityLogic` 顺序）。逐卡那一档没有命中 ⇒ 退回「按阵营 / 按事件」那**一件**（数组长度 1）。
        ///
        /// <para>**判据**（第一权威）＝原版 `CardAbility.cs:27-29`：
        /// 「If true, only one ability triggered, chosen randomly. Otherwise, **all are triggered in order**」
        /// —— 现读 1,495/1,496 个 `CardAbility` 的 `randomAbilityChoice` 为假 ⇒ **全播**。
        /// 我们这张表里「哪些行该全播」的筛选口径写在 <see cref="ByCard"/> 的 doc（只播最优档、
        /// 只播 `authoritative` 时全播、同名重复保留）。</para>
        ///
        /// <para>⚠️ **调用方必须按顺序逐个播**（顺序 = 原版 `abilityLogic` 的索引顺序）；本方法只负责
        /// 「播哪些、什么顺序」，不碰 `CardEffects`（那个钩子归调用点）。</para>
        /// <para>⚠️ **返回的是拷贝**（调用方改它不会污染表）；没有可播的 ⇒ **空数组**（⛔ 不是 null ——
        /// 调用方直接 for 循环即可）。</para>
        /// <para>🆕 **2026-10-19（W · 多候选 ③）就地订正（铁律 5）**：本段原来写「`randomAbilityChoice = true`
        /// 的『按组随机抽一组』**还没做**，数据侧还没有 `g`/`rc` 位」—— **不再成立**：
        /// 数据侧已由 `gen_card_vfx_by_card.py` 写 `es[].rc/ai/gj/gn`（现读全池唯一 `rc == 1`
        /// 的卡仍是 `GOF12 Spanner`），消费在 <see cref="ResolveAllTimed"/>（**传 `rng` 才抽**）。
        /// 👉 **本方法（不带时间的老口）仍然全播** —— 它没有 rng 参数，生产也不用它。</para>
        /// <para>✅ **2026-10-19（B6）就地订正（铁律 5）**：本段原来还有「② 同一组内多拍的时间错开 —— 数据侧
        /// 还没有逐候选的时间」—— **两半都不再成立**：数据 = `CardDef.AnimTimes`（E12 已落，卡池 v12），
        /// 消费 = <see cref="ResolveAllTimed"/>（本类）。本方法（只回名字）保留为**不带时间**的老口。</para></summary>
        public static string[] ResolveAll(string evt, string faction = null, string cardIdOrName = null)
        {
            if (string.IsNullOrEmpty(evt)) return NoEffectNames;
            List<CardPick> picks;
            if (TryResolvePerCardPicks(evt, faction, cardIdOrName, out picks) && picks != null && picks.Count > 0)
            {
                var names = new string[picks.Count];          // 拷贝：⛔ 别把表里的 List 交给调用方
                for (int i = 0; i < picks.Count; i++) names[i] = picks[i].N;
                return names;
            }
            // 🆕 2026-10-22（`W64` §B9）：与 `Resolve` 同一处判据（稀有度档）—— ⛔ 别只改一个口。
            string tier = DeployTierFor(evt, faction, cardIdOrName);
            if (tier != null) return new[] { tier };
            string one = ResolveFallback(evt, faction);
            return string.IsNullOrEmpty(one) ? NoEffectNames : new[] { one };
        }

        /// <summary>「没有可播的」那一个共享空数组（省掉每帧的分配；只读，别往里面写）。</summary>
        static readonly string[] NoEffectNames = new string[0];

        /// <summary>🆕 2026-10-19（B6）：**一条要播的候选 + 它自己的起点偏移**。</summary>
        public struct ResolvedVfx
        {
            /// <summary>prefab 名（= 原来 `ResolveAll` 给的那个字符串）。</summary>
            public string Name;
            /// <summary>**起点偏移（秒，相对这一事件的起播时刻）** = 原版 `CardDef.CardAnimTime.Offset`
            /// （= 同 `(卡, 原版 trigger)` 组内前面各候选的 `Duration` 之和）。`0` = 与事件同一拍。</summary>
            public float T;
            /// <summary>🆕 2026-10-23（`W63` 收口 ①）：**这条候选的 `CardAnim` 资产名**
            /// （= `CardPick.A`；空串 = 那条候选不来自 `cardAnimations[]`）。
            /// 🔴 **它是 `AnimInfo` 参数的精确键**：同一个 prefab 名可以被多张
            /// `CardAnim` 共用、参数却不同（现读 **215 组**）⇒ 按 prefab 名查 `AnimInfo` 会拿错。</summary>
            public string Anim;
            /// <summary>🆕 这条候选**自己的时长**（= `AnimInfo.GetAnimDuration()`，`CardAnimTime.Duration`）。
            /// 消费端用它把「演出闸 / 队尾」推到这一拍的收尾（原版队列要等它演完才放行下一条动作）。
            /// `0` = 没有逐候选时间（同一档兜底 / 单候选）—— 那时收尾由事件自己的时长负责。</summary>
            public float Duration;
        }

        /// <summary>🆕 2026-10-19（B6）：<see cref="ResolveAll"/> 的**带时间**版本 —— 多候选按原版
        /// `cardAnimations[]` 的串行时长**错开**。
        ///
        /// <para>**判据（第一权威）**：原版 `AbilityLogic.PlayAnim` 把每条候选压成一条
        /// `playAnim` 动作，队列**一次只弹一条**、每条等完 `AnimInfo.GetAnimDuration()` 才结束
        /// （`BattleManager__ResolveActionFromQueue.c:44-76` +
        /// `BattleManager._ResolveAnim_d__466__MoveNext.c:200-210`）
        /// ⇒ **第 n 条的起点 = 前面各条时长之和**。E12 把这个和算好落成 `CardAnimTime.Offset`（`t`）；
        /// 本方法只负责「把候选钉到它自己那条 `AnimInfo` 上、把 `t` 交出去」。
        /// ⛔ **不要在这里发明任何固定间隔**（那正是 B4 报告里被推翻的口径）。</para>
        ///
        /// <para>**怎么钉**（判据 → `资料/普查产出_1019/E12_报告.md` §5·1）：
        /// ① 首选 `CardAnimTime.Anim == 表里的 es[].a`（<see cref="CardPick.A"/>）—— 这是**精确 join 键**；
        /// ② `A` 为空（那条候选来自卡 SO 的顶层字段 / 推断档，不在 `cardAnimations[]` 里）
        ///    ⇒ 用 `CardAnimTime.Prefab == es[].n`；
        /// ③ 同一张卡里同名 `CardAnim` 可以出现多次（现读 59 行）⇒ **按数组顺序逐次消费**
        ///    （游标只前进不后退），⛔ 不去重、也⛔ 不回头拿前面那条；
        /// ④ 钉不上（这张卡没有 `animTimes` / 真值快照里就没有这一条）⇒ `T = 0`
        ///    —— **等于今天的行为**（同一拍全播），⛔ 不拿别的候选的时长凑一个。</para>
        ///
        /// <para>⚠️ **单候选 ⇒ `T` 恒为 0**（回归口径）：错开是给「同组多候选」用的；单候选多出来的
        /// 等待会把「这一拍什么也没发生」放大，而那不是本件要解决的问题（现读有 `animTimes` 且有命中的
        /// 1,008 行里，993 行首候选 `t = 0`；会落到「首候选 `t &gt; 0`」的 15 行，原因都是
        /// **它前面还有一条被本表过滤掉的纯计时拍 / 别的触发拍**）。</para>
        ///
        /// <para>🆕 **2026-10-19（W）`vfxDelayTime`**：`T` = **组偏移 + 这一条自己的 `vfxDelayTime`**。
        /// 判据 = 原版 `BattleManager._DelayedVFXPlay_d__469__MoveNext.c:17-27` —— 生成 VFX 之前
        /// `WaitForSeconds(animInfo.vfxDelayTime)`（`0x24`）。⚠️ 它**不受「单候选 ⇒ 组偏移 0」影响**
        /// （那说的是组偏移 `t`；自身延迟是真的要等）。现读 1,514 条钉上的候选里只有 **1 条** `d &gt; 0`
        /// （`EC29 Heldrake` 的 `play`，0.4 s）。</para>
        ///
        /// <para>🆕 **2026-10-19（W）跨 trigger（`ability` 专用）**：见 <see cref="CrossTriggerDrops"/> ——
        /// `ability` 行里混着「收集类」trigger（610/665）时丢掉那几条（现读 2 行 / 3 条）。</para>
        ///
        /// <para>⚠️ **跨 trigger 组**：`t` 的分组键是 `(卡, 原版 trigger)`，而本表的事件键更粗
        /// （`Strike`/`Slay` 都归 `trigger`，多条 trigger 都归 `ability` —— 见 `工具/gen_card_vfx_by_card.py`
        /// 的 `TRIGGER_EVENT`）。现读 **23 行**的多候选里混着不同 trigger 组 ⇒ 那些候选的 `t`
        /// **各自从 0 起**（数据本来如此），可能同时播。**如实保留**：⛔ 不把后一组的 `t` 累加到前一组上
        /// （那等于编一个间隔）。</para>
        ///
        /// <para>🆕 **2026-10-19（W · 多候选 ③）`rng` 参数**：传了才做 `randomAbilityChoice`
        /// 的「按组随机抽一组」（判据见 <see cref="CardPick"/> 的 `rc/ai/gj/gn` 段）；
        /// **抽中的那一组是单独进原版动作队列的** ⇒ 组内 `T` **从 0 重新累计**
        /// （数据里的 `t` 是「全部 `abilityLogic` 按序全播」的累计，直接用会把第 n 组延后）。
        /// ⚠️ 现读唯一 `rc == 1` 的卡是 `GOF12 Spanner`（5 组 × 1 件、全是 `Buff_Ork_Mek`），
        /// 且它只有这一条 `CardAbility` ⇒ 没有「随机组后面还跟着别的 CardAbility」的样例；
        /// 那种情况下后续候选的 `t` 还没按抽组重算 —— 如实记，⛔ 别拿今天的单一样例当通则。</para>
        ///
        /// <para>⚠️ **返回的是拷贝**；没有可播的 ⇒ **空数组**（调用方直接 for）。</para></summary>
        /// <param name="times">这张卡的 `CardDef.AnimTimes`（null / 空 = 没有逐候选时间 ⇒ 全 `T=0`）。</param>
        /// <param name="rng">🆕 2026-10-19（W · 多候选 ③）：**`randomAbilityChoice` 的随机源**。
        /// 传了 ⇒ 对 `rc == 1` 的那条 `CardAbility` 抽一个 `abilityLogic` 组（判据见
        /// <see cref="CardPick"/> 的 `rc/ai/gj/gn` 段）；**`null`（默认）= 旧行为：按序全播**。
        /// 生产调用方 = `BattleDriver.FireEventAll`（传 `_vfxOffsetRng`，开局种子派生的独立随机源）。
        /// 现读全池唯一 `rc == 1` 的卡是 `GOF12 Spanner`，它 5 个组的 prefab **全是 `Buff_Ork_Mek`**。</param>
        /// <param name="perCardOnly">🆕 2026-10-19（W · 触发逐卡）：**只查逐卡那一档、不许掉兜底**
        /// （`Trigger` 用途要它 —— `ByEvent[Trigger]` 是死条目，掉兜底会播出一个原版没有的通用特效）。
        /// 逐卡表里没有 ⇒ 返回空数组。</param>
        /// <param name="keyword">🆕 2026-10-19（W · `T21` 缺口 B/G）：**这一拍是哪个原版 trigger**
        /// （`BattleEvent.Keyword`，trait 规范键或 `AbilityTrigger` 枚举名）。
        /// 只有 `evt == Trigger` 时起作用，且**只对 <see cref="IsRoutedTriggerName"/> 认的那一批**
        /// （`Ambush` / `TriggerEnchantment` + 缺口 A/E/F 那两条发射口拍出来的名字）生效
        /// —— 见 <see cref="TryResolveTriggerPicks"/>。
        /// ✅ **2026-10-19（战斗线 `T21`）就地更新**：生产路径**已经接上** ——
        /// `BattleDriver.PlaySignal` 把 `e.Kind == Trigger ? e.Keyword : null` 传下来
        /// （`FireEventAll` 的 `keyword` 形参）。原来那两句「生产路径今天传的是 `null` /
        /// 把 `e.Keyword` 接进来是接口需求」**已经不成立**（铁律 5）。
        /// 剩下的保守档只对「`keyword` 为空」的旧事件有效。
        /// <para>🆕 **2026-10-20（`FACTION-DOUBLE605`）**：`keyword` 可能带 **`#&lt;i&gt;` 后缀** ——
        /// `BattleEvent.KeywordWithAbility` 拼的「能力序号传输形态」（`BattleEvent.AbilityIndex` 的 doc）。
        /// 本方法**先拆后缀**再当 trigger 名用（<see cref="BattleEvent.SplitKeywordAbility"/>，
        /// 拆法与拼法同一处）；`abilityIndex` 形参给了（≥0）就以形参为准、不再看后缀。</para></param>
        /// <param name="abilityIndex">🆕 2026-10-20（`FACTION-DOUBLE605`）：**只看这一条原版
        /// `cardAbilities[i]` 的动画行**（0 基；`-1` = 不筛 = 改前行为）。
        /// 判据 = `CardDef.CardAnimTime.Order` 的第一段（原版字段路径 `i.j.k`）；
        /// 钉行用 <see cref="FindAnimTimeIndex"/> 的**顺序消费**（同一张卡里同名 `CardAnim`
        /// 可以出现多次 —— 今天这 3 张卡里 `Faith_trigger_*` 就各出现两次，⛔ 不能按名字取第一条）。
        /// 解析不到 `Order` 的候选**不算**这一条能力（`AbilityIndex &gt;= 0` 时宁可留白，⛔ 不多播）。</param>
        public static ResolvedVfx[] ResolveAllTimed(string evt, string faction, string cardIdOrName,
                                                   IReadOnlyList<CardDef.CardAnimTime> times,
                                                   System.Random rng = null,
                                                   bool perCardOnly = false,
                                                   string keyword = null,
                                                   int abilityIndex = -1)
        {
            if (string.IsNullOrEmpty(evt)) return NoEffectTimes;

            // 🆕 2026-10-20（`FACTION-DOUBLE605`）：`BattleDriver` 只转 `Keyword`（不转
            //   `BattleEvent.AbilityIndex`）⇒ 生产路径的能力序号在 `Keyword` 的 `#i` 后缀里。
            //   显式给了 `abilityIndex`（≥0）的调用方以形参为准（keyword 仍会剥后缀，防御重复拼）。
            int evtAbility = abilityIndex;
            string triggerName = keyword;
            if (triggerName != null
                && (evtAbility < 0 || triggerName.IndexOf(BattleEvent.AbilityKeywordSeparator) >= 0))
            {
                int parsed;
                triggerName = BattleEvent.SplitKeywordAbility(triggerName, out parsed);
                if (evtAbility < 0) evtAbility = parsed;    // 形参优先（见 param doc）
            }

            List<CardPick> picks;
            // 🆕 2026-10-19（W · `T21` 缺口 B/G）：`Trigger` 那一档要**按原版 trigger 名归口**
            //   （`Ambush` / `TriggerEnchantment` 的动画行落在 `ability` 桶里 —— 见 `TryResolveTriggerPicks`）。
            //   其余事件仍然只查自己那个桶（行为不变）。
            bool hit = evt == Trigger
                     ? TryResolveTriggerPicks(faction, cardIdOrName, times, triggerName, evtAbility, out picks)
                     : TryResolvePerCardPicks(evt, faction, cardIdOrName, out picks);
            if (!hit || picks == null || picks.Count == 0)
            {
                // 🆕 2026-10-19（W · 触发逐卡）：`perCardOnly` = **只查逐卡那一档、不许掉兜底**。
                //   `Trigger` 用途必须这样：`ByEvent` 里那条 `Faith_trigger_unit` 是**死条目**
                //   （原版触发没有通用特效，B2 · 偏差 7）—— 掉兜底会把它当通用特效播出来。
                if (perCardOnly) return NoEffectTimes;
                // 🆕 2026-10-22（`W64` §B9）：**生产路径就是这里**（`BattleDriver.FireEventAll` →
                //   `ResolveAllTimed`）—— 登场那一拍按稀有度挑档，判据与 `Resolve` / `ResolveAll` 同源
                //   （<see cref="DeployTierFor"/>）。⛔ 别只改那两处、漏了这一处（那才是真正在播的口）。
                string tier = DeployTierFor(evt, faction, cardIdOrName);
                if (tier != null) return new[] { new ResolvedVfx { Name = tier, T = 0f } };
                // 兜底那一档（按阵营 / 按事件）永远只有**一件** ⇒ `T = 0`（与 `ResolveAll` 同源）。
                string one = ResolveFallback(evt, faction);
                return string.IsNullOrEmpty(one)
                     ? NoEffectTimes
                     : new[] { new ResolvedVfx { Name = one, T = 0f } };
            }

            // 🆕 2026-10-19（W · 多候选 ③）：`randomAbilityChoice` 的「按组随机抽一组」。
            //   ⚠️ `TryResolvePerCardPicks` 给的是**表里那一份**（约定只读）⇒ 过滤产出**新 list**，
            //       ⛔ 别原地 `Remove`（那会把下一条事件的候选也删掉）。
            bool filtered = false;
            // 🔴 **2026-10-19（W57 · FX-3）就地订正（铁律 5）：闸不能写成 `picks.Count > 1`。**
            //   判据 = 原版 `RawCardScript__TriggerAbility.c:82-93`：`randomAbilityChoice` 为真且
            //   `abilityLogic.Count > 1` ⇒ **一定** `GetRandomInt(0, Count)` 抽一组，**哪怕只有一组
            //   带 `cardAnimations[]`**（抽中没动画的那组 = 这一下没有特效也是合法结果）。
            //   我们的表只收「有 prefab 的候选」⇒ 那种卡折叠后 `picks.Count == 1`；旧闸会让它
            //   **100% 播**（原版是 1/gn）。现读全池唯一 `rc == 1` 的 `GOF12 Spanner` 五组全有件，
            //   所以今天**没有任何可观测变化** —— 这是给数据侧将来出现「部分组无动画」时兜底。
            //   ⚠️ 没有 `rc == 1` 的候选时 `FilterRandomAbilityChoice` 原样返回**同一个引用** ⇒
            //   `filtered` 仍为 false，「单候选 ⇒ T=0」的回归口径（B6 §三·2）一个字不变。
            if (rng != null)
            {
                var fp = FilterRandomAbilityChoice(picks, rng);
                // ⚠️ 只有**真的抽过**（返回了新 list）才改口径 —— 没有 `rc == 1` 的候选时
                //    `FilterRandomAbilityChoice` 原样返回**同一个引用**，此时「单候选 ⇒ T=0」
                //    的回归口径必须继续成立（B6 §三·2：单候选按绝对 `t` 会凭空多出一段空等）。
                filtered = !object.ReferenceEquals(fp, picks);
                picks = fp;
            }

            int n = picks.Count;
            // 没有时间表 ⇒ 全部 T=0（= 改前的行为）。
            if (times == null || times.Count == 0)
            {
                var flat = new ResolvedVfx[n];
                for (int i = 0; i < n; i++) flat[i] = new ResolvedVfx { Name = picks[i].N, Anim = picks[i].A, T = 0f, Duration = 0f };
                return flat;
            }

            // ① **先钉**：每条候选贪心钉到它自己那条 `AnimInfo`（游标只前进；规则见方法 doc ②③）。
            int[] pin = new int[n];
            int cursor = 0;
            for (int i = 0; i < n; i++)
            {
                pin[i] = -1;
                int idx = FindAnimTimeIndex(times, cursor, picks[i]);
                if (idx < 0) continue;              // 钉不上 ⇒ 该条 T 只有自身 `vfxDelayTime`
                pin[i] = idx;
                cursor = idx + 1;                   // ③ 游标只前进：同名重复按顺序逐次消费
            }

            // ①·bis 🆕 2026-10-20（`W58-REFIX` · 铁律 5 就地订正）：**按能力序号筛行放在钉行【之后】**，
            //   复用同一组 `pin[]`。原来这一步在 `TryResolveTriggerPicks` 里（钉行之前），
            //   而 `CardPick` 不带行号、`pin` 的游标每次从 0 起 ⇒ 第 2 条能力被重新钉回**第 1 次出现的
            //   同名行**上，拿到的是第 1 条能力的 `Offset`（SOR56 `#2` 实得 0/1、SOR23 `#1` 实得 0、
            //   SOR67 `#2` 实得 0 —— 三条断言一起红）。诊断 → `资料/普查产出_1020/W58_诊断_RuleEngine14条.md` §11。
            //   判据 = `CardDef.CardAnimTime.Order` 第一段（原版字段路径 `i.j.k` 的 `i`）。
            //   ⚠️ 钉不上 / `Order` 解不出 ⇒ **不算这一条能力**（`evtAbility >= 0` 时宁可留白，⛔ 不多播）。
            //   ⚠️ 只对 `Trigger` 那一档生效（改前也只有 `TryResolveTriggerPicks` 做过这件事）。
            if (evt == Trigger && evtAbility >= 0)
            {
                int kept = 0;
                for (int i = 0; i < n; i++)
                {
                    int ai;
                    if (pin[i] >= 0 && CardDef.TryParseAbilityIndexOfOrder(times[pin[i]].Order, out ai)
                        && ai == evtAbility) kept++;
                }
                if (kept == 0)
                {
                    // 与改前同口径（原来是 `TryResolveTriggerPicks` 回 false 走 `!hit` 那一支）：
                    //   只查逐卡的不许掉兜底；允许兜底的老口径原样保留。
                    if (perCardOnly) return NoEffectTimes;
                    string fb = ResolveFallback(evt, faction);
                    return string.IsNullOrEmpty(fb)
                         ? NoEffectTimes
                         : new[] { new ResolvedVfx { Name = fb, T = 0f } };
                }
                if (kept < n)
                {
                    var kp = new List<CardPick>(kept);
                    var kpin = new int[kept];
                    int q = 0;
                    for (int i = 0; i < n; i++)
                    {
                        int ai;
                        if (pin[i] < 0 || !CardDef.TryParseAbilityIndexOfOrder(times[pin[i]].Order, out ai)
                            || ai != evtAbility) continue;
                        kp.Add(picks[i]);
                        kpin[q++] = pin[i];
                    }
                    picks = kp;
                    pin = kpin;
                    n = kept;
                }
            }

            // ② 跨 trigger：`ability` 事件里「收集类」那一组是另一个时刻（判据 → `CrossTriggerDrops`）。
            bool[] drop = (!perCardOnly && evt == Ability) ? CrossTriggerDrops(pin, times) : null;

            // ③ 组装：**单候选 ⇒ 组偏移按 0**（回归口径，B6 §三·2）；
            //    🆕 2026-10-19（W）：但 **`vfxDelayTime` 要加**（原版 `DelayedVFXPlay` 在生成 VFX 前
            //    先 `WaitForSeconds(animInfo.vfxDelayTime)` —— `BattleManager._DelayedVFXPlay_d__469__MoveNext.c:17-27`）。
            bool onlyOne = n <= 1;                  // 组偏移为 0 的那一档
            bool regression = onlyOne && !filtered; // `Duration` 也保持 0 的那一档（队尾由事件自己负责）
            // 🆕 2026-10-20（`FACTION-DOUBLE605`）：**按能力序号发的事件里，单候选也要保留 `Offset`**。
            //   理由：那时 `Offset` 是「这条能力在整张卡动画序列里的绝对位置」（前一条能力的行演完
            //   才轮到它）—— 同一张卡的第二条能力往往只有 1 条动画行（SOR67 能力 2、SOR23 能力 1），
            //   掉回 0 会跟第一条**同时播**（原版是接着演）。⚠️ 只影响 `abilityIndex >= 0` 那一档；
            //   `regression`（`Duration = 0`）**不动** —— 队尾口径保持原样。
            bool keepOffset = evtAbility >= 0;
            int keep = 0;
            for (int i = 0; i < n; i++) if (drop == null || !drop[i]) keep++;
            var res = new ResolvedVfx[keep];
            int k = 0;
            Dictionary<long, float> groupAcc = null;    // (ai,gj) → 组内已累计时长
            for (int i = 0; i < n; i++)
            {
                if (drop != null && drop[i]) continue;
                int idx = pin[i];
                float delay = 0f, dur = 0f, t = 0f;
                if (idx >= 0)
                {
                    delay = times[idx].Delay > 0f ? times[idx].Delay : 0f;
                    dur = times[idx].Duration > 0f ? times[idx].Duration : 0f;
                    t = (onlyOne && !keepOffset) ? 0f : (times[idx].Offset > 0f ? times[idx].Offset : 0f);
                }
                if (picks[i].rc != 0 && filtered)
                {
                    // 🔴 **抽中的那一组是【单独】进队列的** ⇒ 组内从 0 重新累计。
                    //   数据里的 `t` 是「全部 `abilityLogic` 按序全播」的累计
                    //   （`anim_times_of` 的 cursor 不分组）⇒ 直接用它会把抽中的第 4 组延到 0.9 s。
                    //   ⚠️ **`filtered` 这个条件要留**（2026-10-19 · W57 · FX-3）：没有传 `rng`
                    //   （= 探测 / 自检口径，生产恒传）时**没有抽组**，此时应保持「按序全播」的
                    //   原时间线 —— 直接用数据里的 `t`（0 / 0.3 / 0.6 / …）。少了它，5 组会
                    //   各自从 0 累计 ⇒ **同时播**（`ResolveAllTimed` 的 doc 写的「不传 = 旧行为：
                    //   按序全播」就不成立了）。现读唯一 `rc == 1` 的 `GOF12 Spanner` 5 组同 prefab，
                    //   生产（传 rng）只播抽中的 1 组 ⇒ 这个条件对生产**零影响**。
                    if (groupAcc == null) groupAcc = new Dictionary<long, float>();
                    long gk = ((long)picks[i].ai << 32) ^ (uint)picks[i].gj;
                    float at;
                    groupAcc.TryGetValue(gk, out at);
                    res[k] = new ResolvedVfx { Name = picks[i].N, Anim = picks[i].A, T = at + delay, Duration = dur };
                    groupAcc[gk] = at + dur;        // 下一件的组内起点 = 这一件动作起点 + 它的完整时长
                    k++;
                    continue;
                }
                res[k] = new ResolvedVfx
                {
                    Name = picks[i].N,
                    Anim = picks[i].A,
                    T = t + delay,
                    Duration = regression ? 0f : dur,
                };
                k++;
            }
            return res;
        }

        // ==================================================================
        //  🆕 2026-10-21（W61 战斗线「新账-65」第 1 批 · 第 2/3 条）：
        //  **这条事件在原版动作队列里「演多久」** —— 远程攻击的伤害就在那一刻结算
        // ==================================================================
        //  判据：原版 `cardAnimations[]` 的每条候选各占一条 `playAnim` 动作，队列一次弹一条、
        //  每条等完 `AnimInfo.GetAnimDuration()` 才结束 ⇒ 整条链的收尾 = `max(起点 + 时长)`
        //  （`BattleManager__ResolveActionFromQueue.c:44-76` + `_ResolveAnim_d__466__MoveNext.c:200-210`；
        //   远程那一支另外还有 `_ResolveAnimInfoRangeAttack…:111-124` 的 `WaitForSeconds(GetAnimDuration())`）。
        //
        //  🔴 **为什么不能用 `ResolveAllTimed` 直接取**：它对**单候选**故意把 `Duration` 压成 0
        //  （「回归口径」，B6 §三·2 —— 队尾由事件自己负责），而**逐卡攻击动画恰好几乎都是单候选**
        //  ⇒ 直接读会拿到 0（那就等于退回写死常数了）。这里在同一次钉行结果上补一次真值。
        // ------------------------------------------------------------------

        /// <summary>🆕 **2026-10-22（`W64`）：远程攻击件那条【原版三层链】的唯一一个口**
        /// （普通攻击与**反击**共用；两者在原版是**同一个协程**）。</summary>
        ///
        /// <para>**判据（第一权威）= `CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c:142-163`**：</para>
        /// <code>
        /// if (RawCardScript__HasCustomRangedAttackParticles(自) == 0) {
        ///     lVar7 = RangedAttackParticlesByClanSO__GetRangedClanParticles(自.clanSO, 自.clan);  // :155
        ///     uVar5 = *(long8 *)(lVar7 + 0x18);                                                   // 取那一条
        /// } else uVar5 = RawCardScript__GetRangedAttackParticles(自);                              // :162
        /// </code>
        /// <para>三层，**一层都不许省、也不许再加一层**：</para>
        /// <list type="number">
        /// <item>**逐卡** `customRangedAttackParticles`（卡 SO 顶层字段）—— 命中就用它；</item>
        /// <item>**阵营** `RangedAttackParticlesByClan`（原版 SO 里只有 **4 家**有专属件：
        ///   Ultramarines / Goff / Sautekh / BlackLegion）—— 我们对应 <see cref="ByFaction"/> 的
        ///   `attackRanged` 那 4 行（`card_vfx_by_card.json` 的 `generic.attackRangedByArmy`，`conf: exact`）；</item>
        /// <item>**SO 第一条** —— 原版 `GetRangedClanParticles` **查不到 clan 就返回 SO 第一条**
        ///   （= Ultra 的 `BulletImpact_2shot_trail`，判据 → `资料/普查产出_1022/W63_飞行与卡ID演出_调查.md` §B）
        ///   ⇒ 我们对应 <see cref="ByEvent"/> 的 `attackRanged` 那一行（2026-10-22 `W63` 已把它的值
        ///   从 `BulletImpact_2shot` 换成 `BulletImpact_2shot_trail` = 同一件）。
        ///   ⚠️ **这一层【不是】「我们挑的通用兜底」**，它就是原版那条链的最后一档；
        ///   ⛔ 别再新造一件替代品、也⛔ 别把这一档删掉让结果变成「空」。</item>
        /// </list>
        ///
        /// <para>⚠️ 实现走 <see cref="ResolveAllTimed"/> 的 **`perCardOnly: false`** 那一档 ——
        /// 它的兜底顺序 = 逐卡 → `ByFaction[evt][faction]` → `ByEvent`，与本方法 doc 的三层**逐字对应**
        /// （`ResolveFallback`）。⛔ 别在这里另写一份 join。</para>
        /// <param name="faction">**施法者**的阵营（攻击 = 攻方、反击 = **守方**）——
        /// 原版读的是「施法者自己的 clan」（`:156` 的 `自.clan`）。`null` = 判不出 ⇒ 只用
        /// `ByEvent` 那条（探针口径，⛔ 不是生产值）。</param>
        /// <param name="cardIdOrName">施法者那张卡（卡 id 或卡名；重名才靠 `faction` 钉）。</param>
        /// <param name="times">那张卡的 `CardDef.AnimTimes`（`null` = 没有逐候选时长 ⇒ 全 `T=0`）。</param>
        /// <param name="rng">`randomAbilityChoice` 的随机源（与 `ResolveAllTimed` 同一个形参；`null` = 按序全播）。</param>
        public static ResolvedVfx[] ResolveRangedAttackPieces(string faction, string cardIdOrName,
                                                             IReadOnlyList<CardDef.CardAnimTime> times,
                                                             System.Random rng = null)
        {
            return ResolveAllTimed(AttackRanged, faction, cardIdOrName, times, rng, perCardOnly: false);
        }

        /// <summary>🆕 2026-10-21（W61 · 第 2 条）：**这条事件演多久**（秒，相对事件起播）。
        /// 逐卡候选有 `CardAnimTime` 真值时 = 「最后收尾那一条的 `起点 + 时长`」；
        /// 只有共享件（没有逐卡时间）时 = 那件 prefab 自己的 `AnimInfo` 时长（<see cref="AnimDurationOfPrefab"/>）。
        /// <para>⚠️ 返回 **0** = 既没有逐卡时间、也不在共享件表里（调用方按「不等」处理，并已由
        /// <see cref="AnimDurationOfPrefab"/> 出过声）。</para></summary>
        public static float ChainDuration(string evt, string faction, string cardIdOrName,
                                          IReadOnlyList<CardDef.CardAnimTime> times)
        {
            if (string.IsNullOrEmpty(evt)) return 0f;

            // ① 先按常规解（多候选时它给的是**真的** `T + Duration`）
            var items = ResolveAllTimed(evt, faction, cardIdOrName, times);
            float end = 0f;
            for (int i = 0; i < items.Length; i++)
                if (items[i].T + items[i].Duration > end) end = items[i].T + items[i].Duration;
            if (end > 0f) return end;

            // ② 全 0（单候选回归口径 / 没有逐卡时间表）⇒ 在**同一次钉行**上补真值。
            //    钉行规则与 `ResolveAllTimed` **逐条相同**（`FindAnimTimeIndex` + 游标只前进），
            //    ⛔ 不另写一份 join。
            List<CardPick> picks;
            if (times != null && times.Count > 0
                && TryResolvePerCardPicks(evt, faction, cardIdOrName, out picks)
                && picks != null && picks.Count > 0)
            {
                int cursor = 0;
                for (int i = 0; i < picks.Count; i++)
                {
                    int idx = FindAnimTimeIndex(times, cursor, picks[i]);
                    if (idx < 0) continue;
                    cursor = idx + 1;
                    var t = times[idx];
                    // 单候选 ⇒ 组偏移按 0（与 `ResolveAllTimed` 的 `onlyOne` 同口径）
                    float at = (picks.Count <= 1 ? 0f : (t.Offset > 0f ? t.Offset : 0f))
                             + (t.Delay > 0f ? t.Delay : 0f);
                    float e2 = at + (t.Duration > 0f ? t.Duration : 0f);
                    if (e2 > end) end = e2;
                }
                if (end > 0f) return end;
            }

            // ③ 共享件（按阵营 / 按事件兜底）：读它自己的 AnimInfo 时长
            for (int i = 0; i < items.Length; i++)
            {
                float d = AnimDurationOfPrefab(items[i].Name);
                if (d > end) end = d;
            }
            return end;
        }

        /// <summary>🆕 2026-10-21（W61）：**共享（非逐卡）攻击件的 `AnimInfo` 时长表** —— 秒。
        /// <para>判据 = `数据/游戏数据/animinfo_lookup.json`（键 = prefab 名，值 = `m` 那条 CardAnim；
        /// 时长 = `ts + tm + te` = `timeAtStartPos + timeMoving + timeAtEndPos`，
        /// 与 `AnimInfo.GetAnimDuration()` 同口径 —— 那 418 条里**没有一条** `vfxDelay ≠ 0`，
        /// 所以这里不加 `Delay`）。</para>
        /// <para>⚠️ **只收「共享件」**（`ByFaction[AttackRanged]` 4 条 + `ByEvent[AttackRanged]` 1 条 +
        /// 1-shot 那一档 + 近战那件 1 条）。逐卡件**不进来** —— 它们的时长在 `CardDef.AnimTimes` 里
        /// （逐卡真值优先）。🔴 **2026-10-22（`W63` §B）**：近战那一条**已经不是兜底件**（原版无通用近战件，
        /// `ByEvent[AttackMelee]` 已删），见下面那一段的注释。</para>
        /// <para>🔴🔴 **2026-10-23（`W64` 收口 · 同一 prefab 名多 CardAnim 的歧义现核）**：
        /// 本表按 **prefab 名**取值，而 `bundle_*cardanims*` 里 **215 个 prefab 名被 ≥2 张 CardAnim 共用、
        /// 参数/时长不同**。逐条核过本表这 7 个名字（判据 = `Resources/cardanim_clips.json` 的 `vd+ts+tm+te`）：
        /// · **无歧义 5 条**：`BulletImpact_2shot_trail`(0.75) · `…_ork_NEW`(0.75) · `NecronGauss`(0.75) ·
        ///   `BulletImpact_2shot`(0.4) · `BulletImpactBurst_crowd`(0.4)；
        /// · ⚠️ **有歧义 2 条**（`_quick` 变体是**另一张 CardAnim**，不是我们这条）：
        ///   `BulletImpact_2shot_trail_chaos` = `Atk_BulletImpact_2shot_chaos` **0.75**
        ///   （`…_chaos_quick` 是 0.3）· `BulletImpact_1shot_trail` = `Atk_BulletImpact_1shot` **0.3**
        ///   （另有 `_nurgle` 0.75 / `_quick` 0.21）。
        ///   ⇒ 本表取的**就是正常那一张**（原版 `RangedAttackParticlesByClan` 挂的也是它），值**不用改**；
        ///   自检里有一条拿 `CardAnimClips.DurationOf("Atk_BulletImpact_2shot_chaos" / "Atk_BulletImpact_1shot")`
        ///   逐值对过 —— 那次核对**就是**「防将来有人按 prefab 名换值换错」的锁。</para>
        /// <para>🔴 现读值（2026-10-22）：`BulletImpact_1shot_trail` **0.3** · `BulletImpact_2shot` **0.4**
        /// （**不再是兜底件**）· 4 个阵营的 `2shot*` / `NecronGauss` 全部 **0.75**（`ByEvent[AttackRanged]`
        /// 现在也取 `BulletImpact_2shot_trail` ⇒ **兜底档同样是 0.75**，与 `EventTiming.RangedVfxFallback` 一致）。
        /// 原版对这五档的 `sp/ep` 都是 `0/2`（起点 = 攻方 `effectsAnchor`、终点 = `abilityTarget`）。</para></summary>
        static readonly Dictionary<string, float> SharedRangedVfxDuration = new Dictionary<string, float>
        {
            // ===== `ByFaction[AttackRanged]` 那 4 条（`card_vfx_by_card.json` generic.attackRangedByArmy）=====
            { "BulletImpact_2shot_trail",             0.75f },   // Atk_BulletImpact_2shot（Ultramarines）
            { "BulletImpact_2shot_trail_ork_NEW",     0.75f },   // Atk_BulletImpact_2shot_ork（Goff）
            { "NecronGauss",                          0.75f },   // Atk_Gauss_Basic（Sautekh）
            { "BulletImpact_2shot_trail_chaos",       0.75f },   // Atk_BulletImpact_2shot_chaos（BlackLegion）
            // ===== `ByEvent[AttackRanged]`（没配专属粒子的阵营走它）=====
            //   🆕 **2026-10-22（`W63` §B）**：兜底名已改成 `BulletImpact_2shot_trail`（上面第一行 0.75）
            //   —— 原版 `RangedAttackParticlesByClanSO__GetRangedClanParticles` 查不到时返回 SO 第一条
            //   = Ultra 那件。`BulletImpact_2shot`（0.4）**不再是兜底件**，今天只剩
            //   `UM34 Point-Blank Shot` 的逐卡 `play` 行还引用它（逐卡时长走 `CardDef.AnimTimes`、
            //   不读这张表）⇒ **保留这一行**只为「谁播到它都查得到时长」，⛔ 别再挂回 `ByEvent`。
            { "BulletImpact_2shot",                   0.40f },   // `BulletImpact_2shot`（同名 CardAnim · 仅逐卡引用）
            // ===== 1-shot 那一档（`RangedAttackParticlesByArmy` 的 `BulletImpact_1shot_trail`）=====
            //   ⚠️ 今天 `ByFaction` 没配它（4 个阵营全是 2shot）—— 留在这里是因为它是**同名 prefab 的
            //   原版时长**，`ChainDuration` 的断言要拿它当**第三档**（0.3 ≠ 0.75 ≠ 1.0）判「没有写死一个数」。
            { "BulletImpact_1shot_trail",             0.30f },   // Atk_BulletImpact_1shot
            // ===== 近战那一件（🔴 **2026-10-22（`W63` §B）：兜底件已从 `ByEvent` 删除**）=====
            //   原版近战**没有**通用件（`attack_melee` 逐卡只 13/1127 张、且那 13 条其实是 `UnitAttack`
            //   技能动画）⇒ **今天没有任何生产路径会播它**。保留这一行只为「试片台/历史引用播到它时
            //   查得到时长」，⛔ 别再把它挂回 `ByEvent[AttackMelee]`（那是原版没有的第二下）。
            { "BulletImpactBurst_crowd",              0.40f },   // `BulletImpactBurst_ability_unit`
        };

        /// <summary>🆕 2026-10-21（W61）：共享攻击件 prefab → `AnimInfo` 时长（秒）。
        /// 查不到 ⇒ **出声**（上限 32 条，别刷屏）并返回 0（调用方按「不等 / 走兜底常数」处理，⛔ 不拿别的数凑）。</summary>
        public static float AnimDurationOfPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return 0f;
            float d;
            if (SharedRangedVfxDuration.TryGetValue(prefabName, out d)) return d;
            if (_warnedNoAnimDuration.Count < 32 && _warnedNoAnimDuration.Add(prefabName))
                Debug.LogWarning($"[VfxMap] 共享特效 `{prefabName}` 没有登记 `AnimInfo` 时长 "
                               + "⇒ 时间线按「不等」处理（远程攻击那一档还会退回 `EventTiming.RangedVfxFallback` 0.75）。"
                               + "补它：`数据/游戏数据/animinfo_lookup.json` 里 `ts + tm + te`，"
                               + "追加到 `SharedRangedVfxDuration`");
            return 0f;
        }
        static readonly HashSet<string> _warnedNoAnimDuration = new HashSet<string>();

        /// <summary>🆕 2026-10-19（W · 多候选 ①）：`ability` 事件里的**跨 trigger** 那一档 ——
        /// 「收集类」候选要丢。
        ///
        /// <para>**判据**：我们的 `EvtKind.Ability` 对应原版这一族 ——
        /// `activeAbility`（`tg = -1`）· `ThisUnitAbilityUsed`(400) · `UseSpiritStone`(600) ·
        /// `UseFaith`(605)（枚举值 → `dump.cs` 的 `AbilityTrigger`；
        /// `RawCardScript__TriggerAbility.c` 是这些 trigger 的演出出口）。
        /// 同一行里还混着 **`OnCollectSpiritStone`(610) / `OnCollectFaith`(665)** 时，
        /// 那是「**收集**那一刻」（`anim_times_of` 的分组键 = `(卡, trigger)`，两组各自从 0 起），
        /// **不是**技能 —— 一起播就是 `ASH80` / `SOR38` 那两条**多播一组**的缺陷。</para>
        ///
        /// <para>⚠️ **只在「行里至少有一条 ability 类」时才丢**（否则无从判断 ⇒ 原样保留，
        /// 不把「我们还没归口的 trigger」当垃圾清掉）；⚠️ **`FollowUpResolution`(410) 不丢** ——
        /// `BattleManager__ResolveAction.c:4746` 在**动作结算尾部**触发它，与能力在同一窗口，
        /// 不能证伪（现读混在 `ability` 行里的 410 有 6 条）。</para>
        ///
        /// <para>现读命中：**2 行 / 3 条** —— `ASH80`（610×1）· `SOR38`（665×2）。</para></summary>
        static bool[] CrossTriggerDrops(int[] pin, IReadOnlyList<CardDef.CardAnimTime> times)
        {
            int n = pin.Length;
            bool hasAbility = false, hasCollect = false;
            for (int i = 0; i < n; i++)
            {
                int tg = TriggerOf(pin[i], times);
                if (IsAbilityTrigger(tg)) hasAbility = true;
                else if (IsCollectTrigger(tg)) hasCollect = true;
            }
            if (!hasAbility || !hasCollect) return null;      // 无从判断 ⇒ 原样
            var drop = new bool[n];
            for (int i = 0; i < n; i++) drop[i] = IsCollectTrigger(TriggerOf(pin[i], times));
            return drop;
        }

        /// <summary>钉上的那条 `CardAnimTime.Trigger`；没钉上 ⇒ <see cref="int.MinValue"/>（≠ 任何枚举值）。</summary>
        static int TriggerOf(int pin, IReadOnlyList<CardDef.CardAnimTime> times)
        {
            if (pin < 0 || times == null || pin >= times.Count) return int.MinValue;
            var at = times[pin];
            return at != null ? at.Trigger : int.MinValue;
        }

        /// <summary>`ability` 类 trigger（见 <see cref="CrossTriggerDrops"/>）。</summary>
        static bool IsAbilityTrigger(int tg)
        {
            return tg == -1                  // activeAbility
                || tg == 400                 // ThisUnitAbilityUsed
                || tg == 600                 // UseSpiritStone
                || tg == 605;                // UseFaith
        }

        /// <summary>收集类 trigger（见 <see cref="CrossTriggerDrops"/>）。</summary>
        static bool IsCollectTrigger(int tg)
        {
            return tg == 610                 // OnCollectSpiritStone
                || tg == 665;                // OnCollectFaith
        }

        /// <summary>「没有可播的」那一个共享空数组（只读，别往里面写）。</summary>
        static readonly ResolvedVfx[] NoEffectTimes = new ResolvedVfx[0];

        /// <summary>🆕 2026-10-19（W · 多候选 ③）：`randomAbilityChoice` 的**按组随机抽一组**。
        ///
        /// <para>**判据（第一权威）** → `RawCardScript__TriggerAbility.c:75-97` 逐句读：
        /// `CardAbility.randomAbilityChoice`(0x48) 为真、且它的 `abilityLogic`(0x50) 的
        /// `Count &gt; 1` 时 —— `BattleManager.GetRandomInt(0, Count)` 抽**一个** logic 组
        /// （`GetRandomInt` 的区间是**左闭右开**：`MatchData__GetRandomInt.c` 的
        /// `param_2 + 值 % (param_3 − param_2)`），然后只播那一组；否则**按序全播**。
        /// ⚠️ 原版的随机**在玩法层**（`AbilityLogic.PlayAbility` 是效果+演出一起），我们这边引擎的
        /// 效果结算不走 `abilityLogic` ⇒ 这里只能做**表现层**的近似（如实记，铁律 3）。</para>
        ///
        /// <para>**怎么抽**：按 `ai`（= `cardAbilities[i]`）分组，**每条各抽各的**；
        /// 每条的 `gn` 取该组所有候选里的**最大值**（数据里本应一致；取大是防御 ——
        /// 原版抽中「没有动画的组」也是合法结果，等于这一下没特效）。
        /// 抽取顺序 = **候选数组里 `ai` 第一次出现的顺序**（`List` 顺序确定 ⇒ 同一 rng 序列可复现）。
        /// 没有 `rc == 1` 的候选 ⇒ **原样返回表里那一份**（不分配新 list）。</para>
        ///
        /// <para>⚠️ **它不碰引擎的 `Ctx.Rng`** —— 随机源由调用方给（生产建议 `_vfxOffsetRng`，
        /// 与逐轴偏移同一口径：开局种子派生、两局不同、同一局重放一致）。</para></summary>
        static List<CardPick> FilterRandomAbilityChoice(List<CardPick> picks, System.Random rng)
        {
            // ① 按 `ai` 收集：组数（取最大）+ 第一次出现的顺序
            List<int> order = null; Dictionary<int, int> gnOf = null;
            for (int i = 0; i < picks.Count; i++)
            {
                var p = picks[i];
                if (p.rc == 0 || p.gn <= 1) continue;          // 不需要抽（原版 Count ≤ 1 ⇒ 播那唯一一组）
                if (order == null) { order = new List<int>(); gnOf = new Dictionary<int, int>(); }
                int cur;
                if (!gnOf.TryGetValue(p.ai, out cur)) { order.Add(p.ai); gnOf[p.ai] = p.gn; }
                else if (p.gn > cur) gnOf[p.ai] = p.gn;
            }
            if (order == null) return picks;                   // 没有可抽的 ⇒ 原样

            // ② 每条 CardAbility 抽一组（顺序固定 = 上面的 `order`）
            var chosen = new Dictionary<int, int>();
            for (int i = 0; i < order.Count; i++) chosen[order[i]] = rng.Next(gnOf[order[i]]);

            // ③ 只留抽中的组；`rc == 0` 的候选不受影响（它们不在「随机抽一组」那条 CardAbility 里）
            var kept = new List<CardPick>(picks.Count);
            for (int i = 0; i < picks.Count; i++)
            {
                var p = picks[i];
                if (p.rc != 0)
                {
                    int c;
                    if (chosen.TryGetValue(p.ai, out c) && p.gj != c) continue;
                }
                kept.Add(p);
            }
            return kept;
        }

        /// <summary>🆕 2026-10-19（B6）：在 `times` 里从 `from` 起找这条候选自己那条 `AnimInfo`
        /// （规则见 <see cref="ResolveAllTimed"/> 的 doc ②③）。找不到 ⇒ `-1`。
        /// 🆕 2026-10-19（W）：`rc != 0`（= `randomAbilityChoice` 的候选）**先按组号钉** ——
        /// `CardAnimTime.Order` 的 `i.j.k` 里的 `i.j` 必须等于 `ai.gj`（判据 = 生成器写的是
        /// `cardAbilities[i].abilityLogic[j]`；`anim_times_of` 的 `o` 是同一个三元组）。
        /// 不这么做会：① 抽中的组被贪心游标钉到**别的组**同名的 `CardAnim` 上；② 组内时长算错。</summary>
        static int FindAnimTimeIndex(IReadOnlyList<CardDef.CardAnimTime> times, int from, CardPick p)
        {
            if (times == null) return -1;
            bool byAnim = !string.IsNullOrEmpty(p.A);
            string groupPrefix = p.rc != 0 ? (p.ai + "." + p.gj + ".") : null;
            for (int i = Mathf.Max(0, from); i < times.Count; i++)
            {
                var at = times[i];
                if (at == null) continue;
                if (groupPrefix != null
                    && (at.Order == null || !at.Order.StartsWith(groupPrefix, System.StringComparison.Ordinal)))
                    continue;
                if (byAnim)
                {
                    if (at.Anim == p.A) return i;      // ① 精确 join 键
                }
                else if (!string.IsNullOrEmpty(p.N) && at.Prefab == p.N)
                {
                    return i;                          // ② `a` 为空 ⇒ 退回 prefab 名
                }
            }
            return -1;
        }

        /// <summary>`Resolve` / `ResolveAll` 共用的**兜底链**（逐卡那一档没有命中之后）：
        /// `ByFaction` → （原版无此槽？）→ （已知缺口？）→ `ByEvent` → 留白出声。
        /// 🔴 **2026-10-22（`W64`，判据只在 <see cref="IsOriginalNoGenericEvent"/> / <see cref="IsKnownGapEvent"/> 两处）**：
        /// `play` / `ability` / `death` / `return` / `attack_melee` **回 null 且不出声**
        /// （原版没有这一档；`death` 是**真件另有播放口**，见 <see cref="IsOriginalNoGenericEvent"/>）；
        /// `ambush_exit` **回 null 且出声**（有真件、本机接不上）。
        /// 🔴 **2026-10-22（P6）：`collect_waystone` 已从这一族移出** —— 真件导进库、`ByEvent` 那行接上了
        /// （判据见那一行），它现在**解析得出**。
        /// ⛔ 别为了「让它有件」往 `ByEvent` 里塞替代品 —— 那正是 `W64` 销掉的那批账。</summary>
        static string ResolveFallback(string evt, string faction)
        {
            Dictionary<string, string> m;
            if (!string.IsNullOrEmpty(faction) && ByFaction.TryGetValue(faction, out m))
            {
                string s;
                if (m.TryGetValue(evt, out s)) return s;
            }
            string fallback;
            // 🔴 **2026-10-22（`W64`）：两张「空表」在这里分开处理（判据见方法 <see cref="IsOriginalNoGenericEvent"/>）。**
            //   原来这里只有一条 `if (evt == AttackMelee) return null;`（`W63` §B）—— 现在推广成两族：
            //   ① **原版的形状**（`play` / `ability` / `death` / `return` / `attack_melee`）⇒ 回 null、
            //      **不出声**：下面 `WarnNoFactionVariant` 那条警告是给「按阵营配、这个阵营漏配」的
            //      **缺陷**用的；这几档不是缺陷，喊出来只会变成假信号（`Describe()` 里会如实打成 `xx=—`）。
            //   ② **有真件、本机接不上**（今天只剩 `ambush_exit`）⇒ 回 null、**出声一次**：
            //      这是已知缺口，⛔ 不拿替代品顶、也⛔ 不静默。
            //      （🔴 2026-10-22（P6）：`collect_waystone` 已接上真件、**不在这条路上**了。）
            //   ⚠️ `ByFaction` 的查询在**上面**（真有阵营想给这些事件配专属件时仍然生效），
            //      别把这两条挪到它前面。
            //   判据 → `资料/普查产出_1022/W64_兜底审计_特效.md` §一 B1–B8（逐条原版出处）
            //        + `资料/普查产出_1022/W63_飞行与卡ID演出_调查.md` §B（近战那一条）。
            if (IsOriginalNoGenericEvent(evt)) return null;
            if (IsKnownGapEvent(evt))
            {
                // ⚠️ 只在**播放口径**出声：`faction` 为空 = `Unmapped()` / `Describe()` 那种**枚举探针**，
                //    与 `WarnNoFactionVariant` 自己的口径一致（它也挡空 faction），别在枚举时刷日志。
                WarnNoFactionVariant(evt, faction,
                    "这一档**有原版真件、但本机还没接上**（`VfxMap.IsKnownGapEvent`）⇒ 这一拍留白。"
                    + "真件与缺的那张表写在 `ByEvent` 那张表对应那一段注释里。");
                return null;
            }
            if (ByEvent.TryGetValue(evt, out fallback)) return fallback;
            // 🔴 2026-10-16（账 `A1731`）：**走到这里 = 这个 (事件, 阵营) 一行都没配** ⇒ **出声**再回 null。
            //    原来这里是静默 `return null`：玩家看到「抽卡这一拍什么都没发生」，日志里却一个字都没有
            //    （本仓红线：不许静默失败）。
            //    🔴 **2026-10-22（`W64` §B5）就地订正（铁律 5）**：本段原来写「走到这里 = 这个事件只能按阵营配 …
            //      现读只有 `Draw` 是这种，配过的见 `ByFaction` 那张表」—— **前半句不再成立**：`ByFaction`
            //      里 `Draw` 那两行已删（原版抽卡 0 粒子）⇒ 今天走到这里的**只有 `Draw`**、而它
            //      **两边都没配**。这条警告的**语义没变**（留白要出声），只是「配过的」不再存在。
            //    ⚠️ `faction` 为空（`Unmapped()` 那种探测口径，`Draw` 不带阵营本来就该是 null）由
            //       `WarnNoFactionVariant` 自己挡掉；枚举探针由 `_probeDepth` 挡。
            WarnNoFactionVariant(evt, faction,
                "这个事件在 `ByEvent` / `ByFaction` 里都**没有这个阵营的候选** ⇒ 这一拍留白。");
            return null;
        }

        /// <summary>🆕 2026-10-15（账 `A2083`）：**只查逐卡那一档**，⛔ 不回落到按阵营 / 按事件。
        ///
        /// 为什么单开一个口：<see cref="Resolve"/> 的兜底链会把同族事件解析成**按阵营 / 按事件**那件，
        /// 而**阵亡那一拍已经有原版真件**（`CardFeel.DeathExplosion` 播的 `Card 3D Death Explosion`）
        /// ⇒ 把兜底也算进来就是**原版没有的第二下**。
        /// 🔴 **2026-10-22（`W64` §B3）就地订正（铁律 5）**：本段原来举例「`ByEvent[death]` = 我们挑的替代品
        /// `Explosion Fenrisian Monstrosities`」—— **那一行已删**，`Resolve(Death)` 现在也回 null。
        /// 但这道口**仍然必要**：它问的是「**这张卡自己**有没有逐卡覆盖」，而 `Resolve` 还带着
        /// 按阵营 / 按事件两档（别的族仍在用）⇒ 两者语义不同，⛔ 别合并。
        /// ⚠️ 首个消费者 = `CardFeel.SpawnDeathBody`（原版 `CardScript.UnitDeath` 的「二选一」判别式）。
        ///
        /// 与 <see cref="Resolve"/> **共用同一套键解析**（id 或卡名 + 重名靠阵营钉，见 <see cref="ResolveCardIdOf"/>）。</summary>
        public static string ResolvePerCard(string evt, string faction = null, string cardIdOrName = null)
        {
            string s;
            return TryResolvePerCard(evt, faction, cardIdOrName, out s) ? s : null;
        }

        /// <summary><see cref="Resolve"/> 与 <see cref="ResolvePerCard"/> 共用的那一份逐卡查询。
        /// 返回 true ⇔ 命中了**逐卡那一档**（`hit` 是该 (卡,事件) 下的**第一条** prefab 名）。</summary>
        static bool TryResolvePerCard(string evt, string faction, string cardIdOrName, out string hit)
        {
            List<CardPick> picks;
            bool ok = TryResolvePerCardPicks(evt, faction, cardIdOrName, out picks);
            hit = (ok && picks != null && picks.Count > 0) ? picks[0].N : null;
            return ok && hit != null;
        }

        /// <summary>🆕 2026-10-19（B3 · `T13`）：<see cref="TryResolvePerCard"/> 的**全剂量**版本 ——
        /// 交出该 (卡,事件) 下**最优档的全部候选**（按原版 `abilityLogic` 顺序，见 `ByCard`）。
        /// 🆕 B6：元素是 <see cref="CardPick"/>（`N` = prefab 名 · `A` = CardAnim 名，逐候选时间的 join 键）。
        /// ⚠️ 返回的 `List` **就是表里那一份**，调用方**只读**（`ResolveAll` 会自己拷贝）。</summary>
        static bool TryResolvePerCardPicks(string evt, string faction, string cardIdOrName,
                                           out List<CardPick> picks)
        {
            picks = null;
            if (string.IsNullOrEmpty(evt)) return false;
            var m = FindCardRow(cardIdOrName, faction);
            return m != null && m.TryGetValue(evt, out picks) && picks != null && picks.Count > 0;
        }

        /// <summary>卡 id / 卡名 → 那张卡的逐卡行表（`A2082` 的「id 或卡名」换算**只此一份**）。
        /// 没有这张卡 ⇒ `null`。⚠️ 会触发 <see cref="EnsureLoaded"/>。</summary>
        static Dictionary<string, List<CardPick>> FindCardRow(string cardIdOrName, string faction)
        {
            if (string.IsNullOrEmpty(cardIdOrName)) return null;
            EnsureLoaded();
            Dictionary<string, List<CardPick>> m;
            if (ByCard.TryGetValue(cardIdOrName, out m))
                return m;                             // 原样就查得到 ⇒ 它**就是 id**
            string id = ResolveCardIdOf(cardIdOrName, faction);
            if (id != null && ByCard.TryGetValue(id, out m)) return m;
            return null;
        }

        // ==================================================================
        //  🆕 2026-10-19（W · `T21` 缺口 B/G）：`EvtKind.Trigger` 的**按原版 trigger 名归口**
        //
        //  判据 → `资料/普查产出_1019/W_战斗线_T21现核_1019b.md` §三 B / §三 G：
        //    · 我们的运行时表把「原版 trigger → 行」压成两个**粗桶**：
        //      `Strike` / `Slay` → `trigger`；其余 trigger（`Ambush` / `TriggerEnchantment` …）→ `ability`
        //      （`工具/gen_card_vfx_by_card.py` 的 `TRIGGER_EVENT`）。
        //    · 引擎发 `EvtKind.Trigger` 时**只有卡名没有关键词**送到表现层
        //      （`BattleDriver.PlaySignal` 的 `FireEventAll(..., e.CardId)`）⇒ 直接按粗桶播会**多播**
        //      （`GSC16` 的 `Strike` 行动画会在伏击那一拍也播）。
        //    · 唯一能分清「这一条候选属于哪个原版 trigger」的键 = `CardDef.CardAnimTime.TriggerName`
        //      （E12 落的 `tn`，逐候选、来自 `cardAnimations[]` 的 outer `CardAbility.trigger`）。
        //  ==================================================================

        /// <summary>🆕 2026-10-19（W · `T21`）：**允许归口的原版 trigger 名**。
        ///
        /// <para>`Ambush`(670)：引擎在伏击翻开发 `EvtKind.Trigger`（`RuleCore.ApplyDamage` 的伤害路 /
        /// `RuleCore.DeclareAttack` 的攻击取消路，两处都调 `FireTriggerAt(..., KeywordTable.Ambush, ...)`；
        /// 🔴 2026-10-20（`W58`）订正：原文写的 `RuleCore.RevealAmbush` **已删除**）。</para>
        /// <para>`TriggerEnchantment`(500)：`RuleCore.GrantDarkPact` 照原版
        /// `RawCardScript__TriggerEnchantment.c:7-13` 发的就是它。</para>
        ///
        /// <para>🆕 **2026-10-19（战斗线 `T21` · 引擎事件缺口 A/E/F）：再放行下面这一组。**
        /// 判据 = **引擎现在会带着一个明确的 `BattleEvent.Keyword` 发出来**（不是推断的）：
        ///   · A：`FollowUpResolution`(410) —— `CardScript__FollowUpResolutionPlayed.c:7`
        ///     （`OnTrigger(0x19a)`）；发射口 `EffectResolver.EmitFollowUpResolution`。
        ///   · E：`OtherCardPlayed`(10) · `TriggeredFerocity`(726) · `UnitReceivesMarkOfChaos`(510)
        ///     —— `RawCardScript__OnCardPlayedWithTarget.c:96` / `CardScript__TriggeredFerocity.c:7` /
        ///     `CardScript__ReactToUnitMarked.c:7`；其余几个（`OtherUnitSummoned` / `OtherUnitDeath` /
        ///     `UnitAttack` / `UnitDamaged` / `OtherCardDrawn` / `Reanimated` / `TriggeredPray` /
        ///     `TriggeredDuty` / `TriggeredSwarm` / `OtherCardSpiritStone`）= `AbilityTrigger` 枚举名，
        ///     发射口 `EffectResolver.EmitListenerTrigger`。
        ///   · F：`TurnStart`(30) · `TurnStartPlayer`(31) · `TurnEnd`(40) · `TurnBeforeEnd`(41)
        ///     —— 发射口 `EffectResolver.EmitTurnTrigger`。
        /// ⚠️ **只放行「引擎按名字发出来的那一批」**；`Strike` / `Slay` / `Mob` / `Regiment` 这些
        /// 走旧路的**一律不动**（它们今天怎么播就怎么播）—— 报告 §五·6 的「多播」正是
        /// 「把引擎已会发的 trigger 全塞进同一个桶」造成的。新增一条必须**有一个真正的发射口**。</para></summary>
        static bool IsRoutedTriggerName(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) return false;
            if (string.Equals(triggerName, KeywordTable.Ambush, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(triggerName, KeywordTable.TriggerEnchantment, System.StringComparison.OrdinalIgnoreCase))
                return true;
            switch (triggerName)
            {
                // ---- A · followAfterResolution ----
                case "FollowUpResolution":
                // ---- E · 监听者 / 自发反应 ----
                case "OtherCardPlayed":
                case "OtherCardDrawn":
                case "OtherCardSpiritStone":
                case "OtherUnitSummoned":
                case "OtherUnitDeath":
                case "UnitAttack":
                case "UnitDamaged":
                case "UnitReceivesMarkOfChaos":
                case "Reanimated":
                case "TriggeredFerocity":
                case "TriggeredPray":
                case "TriggeredDuty":
                case "TriggeredSwarm":
                // ---- F · 回合起止 ----
                case "TurnStart":
                case "TurnStartPlayer":
                case "TurnEnd":
                case "TurnBeforeEnd":
                // ---- 🆕 2026-10-19（第五十七会话 `W57` · T21 C/D）：阵营能力自己那一拍 ----
                //   `AbilityTrigger.UseFaith = 605` / `UseSpiritStone = 600`。
                //   发射口 = `EffectResolver.ResolveFaithAbility` / `ResolveSpiritAbility`
                //   （经 `EmitAbilityUseTrigger`；闸 = `CardDef.HasAnimTrigger`）。
                //   ⚠️ 这一族以前**没有引擎事件**，`card_vfx_by_card.json` 的
                //   `abilityEventWhy` 逐行写着「未归口（保持 `ability`）」—— 加进来之后
                //   `TryResolveTriggerPicks` 就能按名字从两张桶里挑出这三条候选。
                case "UseFaith":
                case "UseSpiritStone":
                    return true;
            }
            return false;
        }

        /// <summary>🆕 2026-10-19（W · `T21`）：一条候选**自己那条** `CardAnimTime` 的原版 trigger 名
        /// （`CardAnimTime.TriggerName`，如 `Ambush` / `TriggerEnchantment` / `Strike`）。
        ///
        /// <para>钉法同 <see cref="ResolveAllTimed"/>：**先 `A`**（CardAnim 精确 join 键）、**后 `N`**
        /// （prefab 名；`A` 为空的那几条来自卡 SO 顶层字段 / 我们自己的推断档）。
        /// ⚠️ **它是分类器、不是计时器** —— 只回名字，计时仍由 <see cref="FindAnimTimeIndex"/> 的游标做
        /// （同名重复按顺序逐次消费那一条规则只属于计时）。</para>
        /// <para>钉不上 ⇒ `null`（= 这条候选不是来自 `cardAnimations[]`）。</para></summary>
        static string TriggerNameOf(IReadOnlyList<CardDef.CardAnimTime> times, CardPick p)
        {
            if (times == null || times.Count == 0) return null;
            // `A` 非空 ⇒ **只认 `A`**（它是 `cardAnimations[]` 的精确 join 键）。
            // ⚠️ **不许在 `A` 查不到时退回 `N`**：`A` 有值但 `times` 里没有 = 这条候选来自
            //    别的数据源（典型 = 我们自己的 `inferred-keyword` 行，`GSC32` 的
            //    `ArmourTraitStart`/`ConcussiveTraitTrigger`）。退回按 prefab 名匹配会把它
            //    错标成同名的 `Ambush` 候选（`GSC32` 的 `ArmourEffect` 两边同名）⇒ **多播**。
            if (!string.IsNullOrEmpty(p.A))
            {
                for (int i = 0; i < times.Count; i++)
                    if (times[i] != null && times[i].Anim == p.A) return times[i].TriggerName;
                return null;
            }
            if (!string.IsNullOrEmpty(p.N))
                for (int i = 0; i < times.Count; i++)
                    if (times[i] != null && times[i].Prefab == p.N) return times[i].TriggerName;
            return null;
        }

        /// <summary>🆕 2026-10-19（W · `T21` 缺口 B/G）：`EvtKind.Trigger` 的逐卡取数 —— **按原版
        /// trigger 名把两张桶（`trigger` + `ability`）合成这一次该播的候选**。
        ///
        /// <para>**四种结果**（按优先级）：
        /// ① 这张卡在表里一条都没有 ⇒ `false`（`perCardOnly` 那一层回空数组）；
        /// ② **没有逐候选时间**（`times` 空）⇒ 分不出 trigger 组 ⇒ **原样回 `trigger` 桶**
        ///    （= 改前的行为，⛔ 不猜）；
        /// ③ 目标 trigger 名定得出来 ⇒ **两张桶一起、只留标到那个名字的候选**。这正是原版
        ///    `RawCardScript.TriggerAbilities(triggerId)` 的语义（按 triggerId 取 ability）；
        /// ④ 定不出来（同一张卡混着多种 trigger 动画，典型 = `GSC16` 的 `Ambush` + `Strike`）⇒
        ///    **原样回 `trigger` 桶**（= 今天的行为）并如实留白 —— ⛔ 不猜（多播比不播更糟）。</para>
        ///
        /// <para>**目标怎么定**：`keyword` 非空且是 <see cref="IsRoutedTriggerName"/> 认的那一批 ⇒ 用它；
        /// 否则**只在「整张卡只标出一种 trigger 名、且它是那一批之一」时**推断。
        /// ✅ **2026-10-19（战斗线 `T21`）就地更新**：`keyword` **生产路径已经传得进来了**
        /// （`BattleDriver.PlaySignal` → `FireEventAll(keyword:)`）。保守推断档只剩给
        /// 「`keyword` 为空」的旧事件 / 自检直调。</para>
        ///
        /// <para>⚠️ **未标名的候选**（`TriggerNameOf` 回 null：顶层字段 / 我们的 `inferred-*` 档）：
        /// 归口生效时**两张桶都丢掉**（它们证明不了自己属于这个 trigger）。今天受影响的只有
        /// `GSC32`/`GSC28`/`GSC37` 那几条 `inferred-keyword` 行（Concussive/Armour/Talent —— 这几个
        /// 关键词在我们引擎里**根本不触发**，报告 §3·2），丢掉它们 = 不再在伏击那一拍乱播。</para>
        ///
        /// <para>🆕 **2026-10-20（`FACTION-DOUBLE605`）`abilityIndex`**（≥0 = 原版 `cardAbilities[i]`
        /// 的 0 基下标；`-1` = 不筛 = 改前行为）：本方法**不再筛**（见 ③ 的注释）——
        /// 筛选移到 `ResolveAllTimed` 的「①·bis」，复用**同一组 `pin[]`** 读 `CardAnimTime.Order`
        /// 第一段。带序号时**判不出目标 trigger（第 ②/④ 档）⇒ 留白**（⛔ 不掉回粗桶：那会把整批候选再播一遍）。
        /// ⚠️ **它依赖「候选顺序 == `times` 里 `cardAnimations` 的顺序」**（钉行用顺序消费）：
        /// 现读这 3 张卡的全部 `UseFaith` 行都落在**同一张桶**（`ability`）里 = 成立；
        /// 将来若出现「跨两张桶、且同名重复」的卡，钉行会错位 ⇒ 那时要按 `Order` 落数据（见报告「没查清」）。</para></summary>
        static bool TryResolveTriggerPicks(string faction, string cardIdOrName,
                                           IReadOnlyList<CardDef.CardAnimTime> times,
                                           string keyword, int abilityIndex, out List<CardPick> picks)
        {
            picks = null;
            var m = FindCardRow(cardIdOrName, faction);
            if (m == null) return false;
            List<CardPick> trig, abil;
            m.TryGetValue(Trigger, out trig);
            m.TryGetValue(Ability, out abil);
            bool hasTrig = trig != null && trig.Count > 0;
            bool hasAbil = abil != null && abil.Count > 0;
            if (!hasTrig && !hasAbil) return false;

            // ② 没有逐候选时间 ⇒ 分不出 trigger 组 ⇒ 保持改前的行为（只 `trigger` 桶）
            if (times == null || times.Count == 0)
            {
                // 🆕 2026-10-20（`FACTION-DOUBLE605`）：带能力序号的拍**证明不了**是哪几条动画
                //   ⇒ 宁可留白（多发一条事件时「全播」= 两遍，那正是本件要修的缺陷）。
                if (abilityIndex >= 0) return false;
                picks = trig;
                return hasTrig;
            }

            // ① 标名（两张桶一起）
            var names = new List<string>();
            if (hasTrig) foreach (var p in trig) AddTriggerName(names, TriggerNameOf(times, p));
            if (hasAbil) foreach (var p in abil) AddTriggerName(names, TriggerNameOf(times, p));

            // ② 定目标
            string target = null;
            if (!string.IsNullOrEmpty(keyword) && IsRoutedTriggerName(keyword)) target = keyword;
            else if (string.IsNullOrEmpty(keyword) && names.Count == 1 && IsRoutedTriggerName(names[0]))
                target = names[0];
            if (target == null)
            {
                // 🆕 2026-10-20（`FACTION-DOUBLE605`）：带能力序号的拍**判不出目标 trigger 就留白**
                //   —— 掉回 `trigger` 粗桶会把整批候选再播一遍（同一张卡发两条事件时 = 播两遍）。
                if (abilityIndex >= 0) return false;
                picks = trig;                     // ④ 判不出 ⇒ 原样（今天的行为）
                return hasTrig;
            }

            // ③ 两张桶一起、只留标到目标的候选
            var merged = new List<CardPick>();
            if (hasTrig) foreach (var p in trig) if (NameIs(TriggerNameOf(times, p), target)) merged.Add(p);
            if (hasAbil) foreach (var p in abil) if (NameIs(TriggerNameOf(times, p), target)) merged.Add(p);
            // 🆕 2026-10-20（`W58-REFIX` · 铁律 5 就地订正）：**能力序号筛选不在这里做** ——
            //   原来这里调 `FilterPicksByAbilityIndex`（它自己从游标 0 重钉一遍），随后
            //   `ResolveAllTimed` 又用**另一组** `pin[]` 从游标 0 再钉一次 ⇒ 第 2 条能力被钉回
            //   第 1 次出现的同名行（详见 `ResolveAllTimed` 的「①·bis」那段）。现在只回**合并后的
            //   完整候选**，筛选由 `ResolveAllTimed` 在**同一组** `pin[]` 上做。
            picks = merged;
            return merged.Count > 0;
        }

        /// <summary>`names` 里加一个（大小写不敏感去重）。<see cref="TryResolveTriggerPicks"/> 专用。</summary>
        static void AddTriggerName(List<string> names, string tn)
        {
            if (string.IsNullOrEmpty(tn)) return;
            for (int i = 0; i < names.Count; i++)
                if (string.Equals(names[i], tn, System.StringComparison.OrdinalIgnoreCase)) return;
            names.Add(tn);
        }

        /// <summary>大小写不敏感的相等（`ambush` ↔ `Ambush`）。</summary>
        static bool NameIs(string a, string b)
        {
            return !string.IsNullOrEmpty(a) && string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>🆕 2026-10-15（账 `A2082`）：**把一个「卡名」钉到唯一那张卡的 id 上**。
        ///
        /// 三种结果，各自都出声或不吭声的理由写清楚：
        ///   · 名字不在索引里 ⇒ 返回 null，**不出声** —— 这不是错，是「这张卡本来就没有逐卡条目」
        ///     （新卡不在 2022 快照 / 原版这张卡没有动画字段），调用处掉兜底是**设计如此**。
        ///   · 名字只对上一张卡 ⇒ 返回它（绝大多数情况）。
        ///   · 名字对上多张（全池只有 3 组）⇒ 用 `faction` 钉；**钉不出唯一就出声 + 返回 null**。
        ///     ⛔ **不拿「先到的那个」充数** —— 那是静默给错特效（红线）。
        ///     ⚠️ 出声有上限（<see cref="AmbiguousWarnCap"/> 条，按卡名去重），别每帧刷屏。</summary>
        static string ResolveCardIdOf(string keyOrName, string faction)
        {
            List<string> ids;
            if (!_byName.TryGetValue(keyOrName, out ids) || ids == null || ids.Count == 0) return null;
            if (ids.Count == 1) return ids[0];

            string hit = null; int n = 0;
            if (!string.IsNullOrEmpty(faction))
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    string f;
                    if (_factionOfId.TryGetValue(ids[i], out f) && f == faction) { hit = ids[i]; n++; }
                }
                if (n == 1) return hit;                 // 阵营钉住了唯一那张
            }
            if (_warnedAmbiguous.Count < AmbiguousWarnCap && _warnedAmbiguous.Add(keyOrName))
                Debug.LogWarning($"[Vfx] 卡名 `{keyOrName}` 对应 **{ids.Count} 张卡**"
                               + $"（{string.Join(" / ", ids.ToArray())}），而阵营 `{faction ?? "(没传)"}` "
                               + "钉不出唯一那张 ⇒ **逐卡特效这一档不生效**（掉按阵营/按事件的兜底）。"
                               + "⛔ 这里不猜 —— 猜错就是给错特效。判据 → `VfxMap.ResolveCardIdOf` 的 doc。");
            return null;
        }

        /// <summary>自检用：把表里**所有**名字列一遍（BattleScene 那条「名字在库里都找得到」断言吃它）。
        /// 🔴 **2026-10-16（`A2080` 顺手查出的覆盖洞）修两处**：
        ///   ① **补 `EnsureLoaded()`** —— 原来这里**不触发载表**，而逐卡那一档（`ByCard`）是
        ///      2026-10-14 起才**懒载入**的 ⇒ 那条断言跑在第一次 `Resolve` **之前**时，
        ///      `ByCard` 还是空的、**逐卡那一档整块没被查** —— 暗黑天使 `DA48` 那条
        ///      「值在表里、库里没有」就是这么**静默**挂着的（判据 → `任务/T05` §2·1 ②）。
        ///   ② 当年还补过 `SummonCircleByFaction`（阵营召唤法阵）—— 🔴 **2026-10-21 那张表整层已删**（原版没有），
        ///      本条不再枚举它。`ResolvePerCard` 那一档也已经由 `ByCard` 覆盖。</summary>
        public static IEnumerable<string> AllNames()
        {
            EnsureLoaded();                                   // ① 见上面 doc：不载表 ⇒ 逐卡那一档整块漏查
            foreach (var kv in ByEvent) yield return kv.Value;
            foreach (var f in ByFaction) foreach (var kv in f.Value) yield return kv.Value;
            foreach (var c in ByCard)
                foreach (var kv in c.Value)
                    foreach (var nm in kv.Value) yield return nm.N;   // 🆕 B3：一行可能有多件（全播）；B6：`N` = prefab 名
        }

        /// <summary>所有事件名 —— **唯一一份**。`Describe` / `Unmapped` / 自检都用它，
        /// ⚠️ 别在别处再抄一份：「加了新事件、那份列表没跟上」正是 2026-09-15 那个 bug 的形状
        /// （`PlaySignal` 的 switch 漏了 4 个 kind ⇒ 那四个事件**结构上永远不播**）。</summary>
        public static readonly string[] Events =
        {
            PlayCard, Deploy, AttackMelee, AttackRanged, Hit, Heal, Death, Ability, Trigger,
            Return, GainFaith, GainSpirit, GainQuest,
            // 🔴 **2026-10-14（第二十七会话）补 `CollectWaystone`** —— 常量早就在 `:43`、
            //    `BattleDriver.PlaySignal` 也有它的 case，**就是没进这张表** ⇒
            //    `Unmapped()` / `Describe()` 两个自检口**覆盖不到它**（本表自己的注释就写着
            //    「加了新事件、列表没跟上，正是 2026-09-15 那个 bug 的形状」—— 同一形状又犯了一次）。
            CollectWaystone,
            // 🔴 **2026-10-14（`A1816`）补 `Draw` / `AmbushExit`** —— 两条都早就在 `EvtKind` 里、
            //    引擎也在发（`RuleCore.Draw` / `ApplyDamage` 的伤害路；🔴 2026-10-20（`W58`）订正：
            //    原文这里还写着 `RevealAmbush` 那一处 —— **那个方法已删除**，伏击翻面的第二个发送点
            //    现在是 `RuleCore.DeclareAttack` 的攻击取消路），
            //    **就是没进这张表**，而且 `BattleDriver.PlaySignal` 的 switch 里也没有它们的 case
            //    ⇒ 掉 `default: return`（**连 `PlayFeel` 都不进**）。同一形状第三次了（`CollectWaystone` 那次是第二次）。
            //    ⚠️ 🔴 **2026-10-22（`W64` §B5）**：本段原来写「`Draw` 是**按阵营**配的 ⇒ 默认口径解析不出
            //       是预期的，判断时要配 `ResolvesForSomeFaction`」—— **已过期**：`ByFaction` 里
            //       `Draw` 那两行已删（原版抽卡 0 粒子）⇒ 今天**任何一个阵营都解析不出**。
            //       `Unmapped()` 里 `Draw` / `AmbushExit` 会一直出现，那是**预期的**
            //       （由 `IsOriginalNoGenericEvent` / `IsKnownGapEvent` 解释，不是漏配）。
            //       🔴 **2026-10-22（P6）**：`CollectWaystone` **不再出现在那个名单里** ——
            //       它已接上 `Remnant Aeldari Collect particles`（见 `ByEvent` 那一行）。
            Draw, AmbushExit,
            // 🆕 **2026-10-23（`W63` 收口）补 `UnitAttack` / `UnitDamaged`** —— 同一形状**第四次**
            //   （前三次：`CollectWaystone` / `Draw`+`AmbushExit` / …）。它们**只有逐卡**，
            //   `Resolve(evt)`（不带卡）恒 null 是**正确结果** ⇒ 已列进 `IsOriginalNoGenericEvent`，
            //   `Unmapped()` 里会一直看到它们（预期，不是漏配）。
            //   ⚠️ 加进来会让 `BattleScene` 那组「原版没有通用件」的**计数**从 5 变 7 ——
            //   那条断言已同步改成 7（判据本来就是「恰好是那一族」，不是「恰好 5 个」）。
            UnitAttack, UnitDamaged,
        };

        /// <summary>自检用：这个事件**至少有一个已注册阵营**配得上 —— <see cref="Unmapped"/> 的搭档。
        ///
        /// **为什么要有它**：**按阵营**配的事件（`ByFaction` 那 4 个 `AttackRanged`）在
        /// 「不带阵营」的默认口径下**必然**解析成 null，但**那不是「结构上永远不播」** ——
        /// 对配了的阵营它是播的。⛔ **别拿它放宽判据**：它只在 `ByFaction` 里找，
        /// 「一个阵营都没有」仍然算没配。
        /// <para>🔴 **2026-10-22（`W64` §B5）就地订正（铁律 5）**：本段原来拿 `Draw` 当例子
        /// （「本地只有 2 个阵营有那件 prefab」）—— **那两行已删**，`Draw` 今天**一个阵营都没有**
        /// ⇒ `ResolvesForSomeFaction(Draw)` 现在**恒 false**（这是正确结果，⛔ 别拿它判「漏配」）。</para>
        /// ⚠️ 它是**只读**的（不碰 `ByCard`、不触发 `EnsureLoaded`）—— 逐卡那一档
        /// 按设计就**不该**救一个事件名（逐卡表是「这张卡的这个动作」的覆盖，不是「事件有没有配」的兜底）。</summary>
        public static bool ResolvesForSomeFaction(string evt)
        {
            if (string.IsNullOrEmpty(evt)) return false;
            // 🆕 2026-10-16（账 `A1731`）：这是**枚举探针**（问「哪一档配过」），不是播放 ⇒ 不出声。
            //    不挡的话它每次都会替 `Goff` / `BlackLegion` 报一句「抽卡没配」——真话，但不是「现在播什么」。
            _probeDepth++;
            try
            {
                foreach (var f in ByFaction.Keys)
                    if (Resolve(evt, f) != null) return true;
            }
            finally { _probeDepth--; }
            return false;
        }

        /// <summary>自检用：**解析不出特效名**的事件。见 <see cref="Events"/>。
        /// <para>🔴 **2026-10-22（`W63` §B + `W64` B1–B8）就地订正（铁律 5）**：原来这里写「正常应当为空」——
        /// **不再成立**。今天**必然**出现在这个列表里的有三族，而且「回 null」都是**正确结果**：
        ///   ① 原版本来就没有通用件 ⇒ <see cref="IsOriginalNoGenericEvent"/>
        ///      （`play` / `ability` / `death` / `return` / `attack_melee`）；
        ///   ② 有真件、本机接不上 ⇒ <see cref="IsKnownGapEvent"/>
        ///      （今天只有 `ambush_exit`；🔴 2026-10-22（P6）`collect_waystone` 已接上、不在此列）；
        ///   ③ 只能按阵营配、而今天一个阵营都没配（`draw` —— 🔴 2026-10-22 B5 之后连 `ByFaction` 也没了）。
        /// ⇒ 断言批改时这三族要**显式排除**（⛔ 别为了让它绿而往表里塞一件替代品 —— 那正是本批销掉的账）。
        /// 判据 → `资料/普查产出_1022/W63_飞行与卡ID演出_调查.md` §B + `W64_兜底审计_特效.md` §一。</para></summary>
        public static IEnumerable<string> Unmapped()
        {
            foreach (var e in Events)
                if (Resolve(e) == null) yield return e;
        }

        /// <summary>自检用：事件 → 最终会播的名字（带阵营/卡覆盖）</summary>
        public static string Describe(string faction = null)
        {
            var parts = new List<string>();
            // 🆕 2026-10-16（账 `A1731`）：这是「把表打出来看」的口、**不是播放** ⇒ 走探针口径不出声
            //    （否则自检日志里会混进一串「某阵营没配」——那是工具在想看表，不是这一拍真要播）。
            _probeDepth++;
            try
            {
                foreach (var e in Events) parts.Add($"{e}={Resolve(e, faction) ?? "—"}");
            }
            finally { _probeDepth--; }
            return (faction != null ? $"[{faction}] " : "") + string.Join("  ", parts.ToArray());
        }
    }
}
