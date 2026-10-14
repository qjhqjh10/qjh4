// VfxMap.cs — **卡牌动作 → 特效名** 的对照表
//
// 原版这张表在 `d:/4/Unity/数据/游戏数据/`：
//   · `card_anim_map.json`  —— 阵营 → {动作名 → 参数 + vfx guid}（13 个阵营）
//   · `card_vfx_tree.json`  —— guid → 那棵特效树（400 条）
//   · `atk_vfx_map.json`    —— 攻击类型名 → 特效名（297 条）
//   · `effect_index.json`   —— 958 个特效，**带还原质量判定**（`对得上`/`偏亮`/`只有原版有内容`…）
//
// ⚠️ 我们的卡是自研的（Scavenger / Bulwark…），原版的「卡 → guid」查不到，
//    所以这里按**事件 + 阵营**兜底，先保证「有特效、且是还原得对的那个」。
//    以后要逐卡配，把 `ByCard` 填起来即可（键用卡名）。
//
// **挑特效的依据是 `effect_index.json` 的判定列**，别凭名字挑 —— 名单里有一批
// `只有原版有内容（导出整个丢了）` / `两边全程空（纯脚本驱动）`，播出来是**空的**。
// 下面这些全是「对得上 + 高置信」的。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class VfxMap
    {
        // ---- 事件名 ----
        public const string PlayCard     = "play";            // 出牌（手牌打出去的那一刻）
        public const string Deploy       = "deploy";          // 登场（落到格位上）
        public const string AttackMelee  = "attack_melee";    // 近战攻击
        public const string AttackRanged = "attack_ranged";   // 远程攻击
        public const string Hit          = "hit";             // 挨打（掉血）
        /// <summary>🆕 2026-09-29：**治疗**（原版 `BattleAnims.heal` → `Healing_Circles`）。
        /// 我们这边没有独立的治疗事件 —— 它是 `EvtKind.Hit` 且 `Amount &lt; 0`（见 `BattleDriver.PlaySignal`）。</summary>
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
        /// **判据 = 原版那件东西真的叫这个名**：原版抽牌走 `PlayerHand._AddDrawnCardToHand`
        /// （`d:/2/tools/decomp_full/PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c:43-69`：
        /// `BattleHud.GetCardDrawPosition` → `CardScript.DrawCard`），而**逐阵营**那件特效
        /// 由旧台账 `d:/2/Warpforge_tools/data/vfx_wiring_j4.tsv:65` 记着：
        /// `UM_CardDraw` 判 `UI`（「极限战士抽卡特效(**卡飞入手牌区闪光**)」）、挂点 =
        /// 「`_fx_from_event` 抽卡事件(draw) UM 阵营处 / 手牌接收动画」，证据 = `anim SAU_CardDraw`。
        ///
        /// 🔴 **它走「按阵营」那一档，⛔ 不在 `ByEvent` 里** —— 本地**只有 2 个阵营**有这件
        /// （`UM_CardDraw` / `SAU_CardDraw`，都在效果库里 ✓），其余 11 个阵营**留白**
        /// （`Resolve` 回 null ⇒ 不播）。⚠️ **同 `SummonCircleOf` 的先例**：宁可留白，⛔ 不硬凑一件。
        /// ⇒ 「不带阵营」的默认口径解析出来是 null 是**预期的**，不是「这个事件没配」
        /// （见 <see cref="ResolvesForSomeFaction"/>）。</summary>
        public const string Draw = "draw";

        /// <summary>🆕 2026-10-14（`A1816`）：**伏击翻面**（`EvtKind.AmbushExit` —— `Ambush` 面朝下的
        /// 单位翻开那一刻）。
        ///
        /// **判据 = 原版**：旧台账 `d:/2/Warpforge_tools/data/vfx_wiring_j1.tsv:3` 记着
        /// `AmbushEffect` 判 `TRAIT`（「伏击单位翻开瞬间」）、挂点 = 「`_fx_from_event`
        /// "flipped on damage (Ambush no effect)" 分支 + Ambush 翻开（at-turn 翻面）处」。
        /// 这件 prefab **在我们工程里**（`Assets/WarpforgeVFX/Prefabs/AmbushEffect.prefab`，
        /// `effect_index.json` 判定 **对得上 / 高 / ratio 0.89**），而且它自己把三层都带齐了：
        ///   · 音效 —— `AnimFXController.sounds[0].sound` = cue **`Trait Ambush`**（→ clip `Ambush`）
        ///   · 动画 —— `AnimFXModuleChangeMaterial`：把**卡**的材质换成
        ///     `Card 3d Dissolve Blend Image Ambush`，`_Blend` 0→1（`delay 0.35 + duration 1.0`）
        ///   · 粒子 —— 它自己的 ps（台账组件列写 `-`，即没有额外挂点，全靠 prefab 内部）
        /// ⚠️ 逐字段实读（那条 1.35 s 补间的原件路径与 `RefIds`）→ `EventTiming.SourceOf(EvtKind.AmbushExit)`
        ///    的文本 + `DurationOf` 里那个 case 的注释（**那里同时写了为什么它占 0**）。</summary>
        public const string AmbushExit = "ambush_exit";

        /// <summary>
        /// 按事件兜底。
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
        /// | `deploy` | `BlueSummonCircle`（+ 卡专属查名） | ⚠️ 在，但**偏亮 2.7×** |
        /// | `death` | `Card 3D Death Explosion` | ❌ **不在导出索引**（脚本驱动的 3D 效果） |
        /// | `attack_melee` | 按阵营查 `SLASH_VFX`：`Whip Attack EC Normal` / `Cut Axe SW` / `Sword Slash` / `Slash Repeating 2x` / `Rapacious Claw Slash` | 只有 `Cut Axe SW` 干净（对得上/中/1.01）；`Sword Slash`·`Rapacious Claw Slash` **不在索引**，另两个一个「全程空」一个「偏暗 0.19」 |
        /// | `attack_ranged` | 按阵营查 `RANGED_VFX`（`BulletImpact_1shot_trail` 等 ~11 键） | ❌ `BulletImpact_1shot_trail` 判「**两边全程空（完全脚本驱动）**」 |
        /// | `hit` | 攻击结算处的 `AttackHitSmall` / `Actual_Explosion` | ❌ 前者「全程空」、后者**不在索引** |
        /// | `play` / `ability` / `trigger` | 台账里没有对应行 | — |
        /// | `return` / 三个资源事件 | 见下面 `ByEvent` 里那四行的注释 | ✅ 三件 UI 特效 + 一件 MECH 都能用 |
        ///
        /// ⇒ **原版那条链在我们管线里大部分跑不通**（脚本驱动 / 没导出 / 偏亮），
        ///    所以下面用替代品是**有理由的**、不是随手挑的。**但替代品必须是同一语义族** ——
        ///    2026-09-15 查出台账把这两件判成了别的东西、和我们的用法**对不上**：
        ///      · `Tap Firepit` 台账判 **`TAP_SKIP`** = 3D 战场火盆的**点击反馈**（不是「挨打」）
        ///      · `Explosion_Possession` 台账判 **`ATK_EVENT`** = BL 战术卡 `Rites of Possession` 的**命中**（不是「通用阵亡」）
        ///
        /// ⚠️ **②「照原版传参数」被 ① 卡住**（2026-09-15 查实）：原版的参数在 `card_anim_map.json`
        ///    （每条带 `startPosOption`/`endPosOption`/`vfxDelayTime`/`timeMoving`/`shouldMoveVFX`/`ease_points` 等 10 个），
        ///    但那张表**按动画 clip 名索引**（`AeldariSummon` / `Atk_BulletImpact_Eldar_DCannon` …），
        ///    而**我们现役这 8 个名字在 `card_anim_map` / `atk_vfx_map` / 台账里一个都查不到**
        ///    ⇒ 想传参数，**得先把特效换成那三张表里有的**。
        /// </summary>
        static readonly Dictionary<string, string> ByEvent = new Dictionary<string, string>
        {
            { PlayCard,     "Tap Blue Glow" },              // 手牌打出去的一下（轻）
            // 🆕 2026-09-29 **换成原版那件**（判据齐）：原版每张卡登场走的是 `CardScript.normalSummon`
            //   （共享 `CardPrefab` 上的 CardAnim），它的 `animInfo.animAdressable` →
            //   prefab **`Invoke Minion Card Fade Default`**（传说档是 `… Legendary`）。
            //   出处：`数据/游戏数据/card_vfx_by_card.json` 的 `generic.byEvent.deploy`（`conf: exact`）。
            //   ⚠️ 原来那件 `Sororitas Summon Basic` 是**我们挑的替代品**（2026-09-12 挑的）。
            { Deploy,       "Invoke Minion Card Fade Default" },
            { AttackMelee,  "BulletImpactBurst_crowd" },    // 近战：碎屑迸溅
            // 🆕 2026-09-29 **换成原版那件**（判据齐）：`CardScript.attackHitSmallParticles` →
            //   prefab `AttackHitSmall`（原版 `attackHitSmallParticles` 与 `attackHitBigParticles`
            //   是**同一个 pathId**，两档同对象）。
            { AttackRanged, "BulletImpact_2shot" },         // 远程：弹着点
            // 🔴 **这件仍是替代品**：原版攻击结算处接的是 `AttackHitSmall` / `Actual_Explosion`，
            //    **现在 `Hit` 已经改成原版那件**（见下），这一行留着是为了说明当时的取舍。
            // ⚠️ 2026-09-15 实拍复核过（`_tmp_view/pick_hit.png`）：五个候选里这件最好看。
            { Hit,          "AttackHitSmall" },
            // 🔴 **这件是替代品**：原版的通用死亡爆散是 **`Card 3D Death Explosion`**。
            //    ✅ **2026-10-01：那件真件已经进库了**（原来它**不是 addressable**，`GetAllAssetNames` /
            //    `LoadAllAssets` 两条路都拿不到 ⇒ 另开了一条导入路：`工具/extract_missing_shaders.py --prefabs`
            //    重打包 → `EffectExporter.RunListed` → `EffectLibraryBuilder.Run`；判据 → `资料/已知的坑.md`）。
            //    ⇒ 阵亡现在由 `CardFeel.DeathExplosion` 在**卡位生成那件真件**；
            //      这个名字**只在「效果库里真没有那件」的退回路**上才被播一次
            //      （见 `CardFeel.DeathExplosion`；`BattleDriver.PlaySignal` 里 `EvtKind.Death` 已经不再挂它了）。
            { Death,        "Explosion Fenrisian Monstrosities" },
            // 🆕 2026-09-29 **新增 `Heal`**（判据齐）：原版 `BattleAnims.heal` → prefab **`Healing_Circles`**
            //   （那张表其余 12 个槽在本地**全是空引用**）。我们这边「治疗」= `EvtKind.Hit` 且
            //   `Amount < 0`（见 `BattleDriver.PlaySignal` 里那条分流）。
            { Heal,         "Healing_Circles" },
            // 🆕 2026-09-12 挑的（这两类以前**引擎里没有对应事件，压根播不出来**，
            //    所以从来没被挑过 —— 之前填的两个是占位）：
            //      · `Tap Webway Portal` 试片时几乎看不见（只有几点蓝星）
            //      · `Tap Fire Eldar`    试片时是个**硬边绿方块**
            //    换成下面这两个，都是「对得上 + 高置信」且试片里干净可读的：
            { Ability,      "Buff_Blue_SW" },               // 发动技能：单位身上升起蓝色光柱
            { Trigger,      "Faith_trigger_unit" },         // 触发效果：金色环形符记 + 火苗
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
            // ⚠️ `Return` 这件**比上面三件弱**，如实写明：
            //    台账里 `GSC RecallToHand` 判 **`MECH`（机制类）**、接 `_fx_from_event` 的
            //    「事件词 returned to hand / recalled」分支，但 `effect_index` 的置信度只有**中**（ratio 0.99）。
            //    另外两个候选 `Ferocity_RecallToDeck`（对得上/高/1.00）与 `OrkBuff_Recall`（对得上/高/1.32）
            //    都带**阵营专有**语义（前者是回**牌库**、后者是兽人的「回手增益标记」），
            //    而我们的 `EvtKind.Return` **不区分回手还是回牌库** ⇒ 先用 GSC 这件通用性最好的。
            //    ⚠️ **实拍还没做**（本工程规矩：挑特效要看 `Editor/VfxPicker.cs` 试片）—— 换了别忘了补。
            { Return,       "GSC RecallToHand" },
            // 🆕 2026-09-25 **收集灵魂石**（灵族）。**这件是按名字对的、不是拍脑袋挑的**：
            //    原版那一下是 `RemnantAeldari.CollectWaystoneEffect()` → 在残骸原位 `Instantiate`
            //    它自己那个 `collectParticles` 字段指的 prefab（`RemnantAeldari__CollectWaystoneEffect.c`，
            //    逐行读过）；而**原版 bundle 里那件就叫 `WaystoneCollect`**
            //    （`assets_full/bundle_battleprefabs_vfxandmisc_assets_all/GameObject/WaystoneCollect.json`），
            //    我们导出里也有同名的一份 ⇒ 同名直取，没有猜。
            //    ⚠️ **实拍还没看**（本工程规矩：挑特效要看 `Editor/VfxPicker.cs` 的试片）——
            //      `BattleScene` 的 `29_残骸体…` 那张只演了残骸体本身、没演收集。
            { CollectWaystone, "WaystoneCollect" },
            // 🆕 2026-10-14（`A1816`）**伏击翻面** —— 判据与「它自己带齐三层」见 <see cref="AmbushExit"/> 的 doc。
            //    ⚠️ 原版**两条翻面出口**（`EvtKind.AmbushExit.Effect` = `"damage"` / `"window"`）
            //    用的是**同一件**：本事件的语义 = 「这个单位不再面朝下」这个状态迁移，两条出口都是它
            //    （`"window"` 那一档是我们模型的形状、原版没有 —— 见 `RuleCore.RevealAmbush` 的注释）。
            //    ⇒ 两档都播；要严格照原版就只认 `Effect == "damage"`，但**那一条是消费端的事**，
            //      本表没有「同一事件按 `Effect` 分特效」这一层（今天也没有第二件可用）。
            { AmbushExit,   "AmbushEffect" },
        };

        /// <summary>阵营覆盖：**远程攻击**与**抽卡**按阵营换。
        /// ⚠️ **登场那条不在这里** —— 原版登场是「卡的淡入（`VfxMap.Deploy`）+ 阵营召唤法阵」**两件叠加**，
        /// 而 `Resolve` 一次只回一个名字 ⇒ 法阵改由 `BattleDriver.PlaySignal` 的 deploy 分支**单独补一发**
        /// （判据 → <see cref="SummonCircleOf"/>）。
        /// ⚠️ **没配到的阵营一律留白**（回 null ⇒ 不播），**两条都是**：`Draw` 只有 UM / SAU 两件
        /// （见 <see cref="Draw"/> 的 doc）、`AttackRanged` 只有原版那 4 个阵营有专属粒子。</summary>
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
            //   🆕 2026-10-14（`A1816`）**抽卡**也挂这两家的行 —— `UM_CardDraw` / `SAU_CardDraw`
            //   （判据 = `vfx_wiring_j4.tsv:65` 那条台账 + 两件都在 `effect_index.json` 与效果库里的实证，
            //    见 <see cref="Draw"/> 的 doc）。⚠️ **只加有判据的那两个阵营**，别顺手给别家配一件。
            { "Ultramarines", new Dictionary<string, string> {
                  { AttackRanged, "BulletImpact_2shot_trail" }, { Draw, "UM_CardDraw" } } },
            { "Goff",         new Dictionary<string, string> {
                  { AttackRanged, "BulletImpact_2shot_trail_ork_NEW" } } },
            { "Sautekh",      new Dictionary<string, string> {
                  { AttackRanged, "NecronGauss" }, { Draw, "SAU_CardDraw" } } },
            { "BlackLegion",  new Dictionary<string, string> {
                  { AttackRanged, "BulletImpact_2shot_trail_chaos" } } },
        };

        /// <summary>登场时那个**召唤法阵**（原版按阵营/逐卡，本地只有 4 个候选）。
        /// 判据 → `数据/游戏数据/card_vfx_by_card.json` 的 `generic.deploySummonCandidates`
        /// （`AeldariSummon` → `BlueSummonCircle` · `Tau_Summon` / `Tau_Kroot_Summon` → `Tau_SummonCircle` ·
        /// `RelentlessMarchSummon` → `GreenSummonCircle`）。
        /// ⚠️ **Ember / Tide 那两条是我们挑的**（这两个起始阵营在原版里没有对应物）；
        /// 其余阵营的召唤动画**在远端包** ⇒ 返回 null（**不放，如实留白**，不硬凑一个）。</summary>
        public static string SummonCircleOf(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            switch (faction)
            {
                case "Ember": return "Sororitas Summon Basic";       // 我们挑的
                case "Tide":  return "Tau_SummonCircle";             // 我们挑的
                // ⚠️ **键要用【我们的阵营名】**（= `CardDef.Faction`，13 个：SaimHann / TauEmpire / …；
                //    2026-09-30 核过与 `Resources/OffensiveCards.json` 的 `army` **逐字相同**）。
                //    写 "Aeldari"/"Tau" 那种**原版文件夹名**会**静默不播**（`switch` 落 default）。
                case "SaimHann": return "BlueSummonCircle";          // 原版 `AeldariSummon`（判据齐）
                case "TauEmpire": return "Tau_SummonCircle";         // 原版 `Tau_Summon`（判据齐）
                default: return null;                                // 远端包 —— 留白
            }
        }

        /// <summary>逐卡覆盖（键 = **卡 id**，与 `CardDef.Id` / `BattleEvent.CardId` 同一套；事件名同 <see cref="Resolve"/>）。
        /// **2026-10-14 起它由 `Resources/card_vfx_by_card.json` 载入**（见 <see cref="EnsureLoaded"/>），
        /// ⛔ 不再是写死的空字典。
        /// <para>值 = 该 (卡, 事件) 下**按 `confPriority` 取第一个命中**的那个 prefab 名。</para></summary>
        static readonly Dictionary<string, Dictionary<string, string>> ByCard =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>运行时那份逐卡表的 Resources 路径（**没有扩展名**）。
        /// 由 `工具/gen_card_vfx_by_card.py` 与 `数据/游戏数据/card_vfx_by_card.json` **同一次运行**产出。</summary>
        public const string CardTableResourcePath = "card_vfx_by_card";

        static bool _triedCardTable;
        /// <summary>`Resources` 里那份表载进来几张卡（0 = 没载到/空表 —— 那时逐卡那一档**恒不命中**，
        /// 退回 <see cref="ByFaction"/> / <see cref="ByEvent"/> 的老行为，**并已经出过声**）。</summary>
        public static int CardTableCards { get { EnsureLoaded(); return ByCard.Count; } }
        public static int CardTableEntries { get; private set; }

        // ---- 运行时那份 JSON 的形状（**数组，不是字典** —— `JsonUtility` 不支持字典）----
        [System.Serializable] public class RtEntry { public string n; public string c; public string a; }
        [System.Serializable] public class RtEvent { public string ev; public RtEntry[] es; }
        [System.Serializable] public class RtCard { public string id; public RtEvent[] events; }
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
        /// **数据**：`Resources/card_vfx_by_card.json`（288.9 KB · **989 张卡** · 2323 条），
        /// 与 `数据/游戏数据/card_vfx_by_card.json`（全量、给工具看）**由同一次运行产出** ⇒ 不会分叉。
        /// ⚠️ **2026-10-14 就地订正（铁律 5 · 账 `A1833`）**：这一行原来写「**240 KB · 979 张卡**」——
        ///    那是**当天上午那一版**的读数（246,036 B），下午 `A1817`（逐卡音效）重跑生成器之后就变了。
        ///    现读 = **295,821 B（288.9 KB）· `cardCount` 989 · `entryCount` 2323 · `audioCount` 586**
        ///    （`generatedAt` 2026-10-14 12:24）。⛔ **别再把这两个数当成手写常量** ——
        ///    要看真值就读文件头那几个字段（本行的数也会再变）。
        ///
        /// **取哪一条**：同一条 (卡, 事件) 可能有多个候选（原版真值 + 我们早先按名/按关键词推的），
        /// 按 JSON 自带的 `confPriority`（`authoritative` 最前）**取第一个命中的**。
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
                if (c == null || string.IsNullOrEmpty(c.id) || c.events == null) continue;
                Dictionary<string, string> m;
                if (!ByCard.TryGetValue(c.id, out m)) { m = new Dictionary<string, string>(); ByCard[c.id] = m; }
                foreach (var ev in c.events)
                {
                    if (ev == null || string.IsNullOrEmpty(ev.ev) || ev.es == null) continue;
                    string best = null; int bestRank = int.MaxValue;
                    foreach (var e in ev.es)
                    {
                        if (e == null || string.IsNullOrEmpty(e.n)) continue;
                        int rank;
                        if (!prio.TryGetValue(e.c ?? "", out rank)) rank = worst;
                        if (rank < bestRank) { bestRank = rank; best = e.n; }
                    }
                    if (best != null) { m[ev.ev] = best; CardTableEntries++; }
                }
            }
            Debug.Log($"[Vfx] 逐卡特效表已载入：{ByCard.Count} 张卡 / {CardTableEntries} 条"
                    + $"（`Resources/{CardTableResourcePath}.json`，按 `confPriority` 取权威优先）");
        }

        /// <summary>查一个事件该播什么。查不到返回 null（调用处会跳过）。</summary>
        public static string Resolve(string evt, string faction = null, string cardId = null)
        {
            if (string.IsNullOrEmpty(evt)) return null;

            Dictionary<string, string> m;
            // ⚠️ 逐卡那一档**要先载表**（2026-10-14 起它不再是空字典 —— 见 `EnsureLoaded` 的说明）。
            //    它排在最前面：**原版真值优先于我们按阵营/按事件的兜底**。
            if (!string.IsNullOrEmpty(cardId))
            {
                EnsureLoaded();
                if (ByCard.TryGetValue(cardId, out m))
                {
                    string s;
                    if (m.TryGetValue(evt, out s)) return s;
                }
            }
            if (!string.IsNullOrEmpty(faction) && ByFaction.TryGetValue(faction, out m))
            {
                string s;
                if (m.TryGetValue(evt, out s)) return s;
            }
            string fallback;
            return ByEvent.TryGetValue(evt, out fallback) ? fallback : null;
        }

        /// <summary>自检用：把表里所有名字列一遍</summary>
        public static IEnumerable<string> AllNames()
        {
            foreach (var kv in ByEvent) yield return kv.Value;
            foreach (var f in ByFaction) foreach (var kv in f.Value) yield return kv.Value;
            foreach (var c in ByCard) foreach (var kv in c.Value) yield return kv.Value;
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
            //    引擎也在发（`RuleCore.Draw` 的 `:1841` / `ApplyDamage` 的 `:5651` 与 `RevealAmbush` 的 `:5855`），
            //    **就是没进这张表**，而且 `BattleDriver.PlaySignal` 的 switch 里也没有它们的 case
            //    ⇒ 掉 `default: return`（**连 `PlayFeel` 都不进**）。同一形状第三次了（`CollectWaystone` 那次是第二次）。
            //    ⚠️ `Draw` 是**按阵营**配的（见它的 doc）⇒ 默认口径解析不出是预期的，
            //       `Unmapped()` 会把它列出来，判断时要配 <see cref="ResolvesForSomeFaction"/>。
            Draw, AmbushExit,
        };

        /// <summary>自检用：这个事件**至少有一个已注册阵营**配得上 —— <see cref="Unmapped"/> 的搭档。
        ///
        /// **为什么要有它**：`Draw` 这类**按阵营**配的事件（本地只有 2 个阵营有那件 prefab）在
        /// 「不带阵营」的默认口径下**必然**解析成 null，而那不是「结构上永远不播」——
        /// 对配了的阵营它是播的。⛔ **别拿它放宽判据**：它只在 `ByFaction` 里找，
        /// 「一个阵营都没有」仍然算没配。
        /// ⚠️ 它是**只读**的（不碰 `ByCard`、不触发 `EnsureLoaded`）—— 逐卡那一档
        /// 按设计就**不该**救一个事件名（逐卡表是「这张卡的这个动作」的覆盖，不是「事件有没有配」的兜底）。</summary>
        public static bool ResolvesForSomeFaction(string evt)
        {
            if (string.IsNullOrEmpty(evt)) return false;
            foreach (var f in ByFaction.Keys)
                if (Resolve(evt, f) != null) return true;
            return false;
        }

        /// <summary>自检用：**解析不出特效名**的事件（正常应当为空）。见 <see cref="Events"/>。</summary>
        public static IEnumerable<string> Unmapped()
        {
            foreach (var e in Events)
                if (Resolve(e) == null) yield return e;
        }

        /// <summary>自检用：事件 → 最终会播的名字（带阵营/卡覆盖）</summary>
        public static string Describe(string faction = null)
        {
            var parts = new List<string>();
            foreach (var e in Events) parts.Add($"{e}={Resolve(e, faction) ?? "—"}");
            return (faction != null ? $"[{faction}] " : "") + string.Join("  ", parts.ToArray());
        }
    }
}
