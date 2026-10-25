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
        /// <summary>🔴🆕 **2026-10-24（执行代理 `J1` · `C5` D14）：关键词天赋的 `OnPlay(2)` 帧**。
        ///
        /// <para>⚠️ **它不是一个「事件」**（⛔ 不在 <see cref="Events"/> 里、也没有 `EvtKind`）——
        /// 它是**原版 `CardTrait.EffectTriggerType.OnPlay(2)`** 那一族动画的**归置桶**。
        /// 判据（全量反编译，逐句读）：`CardScript__ActivateTraitsOnSummonOrEnchantment.c` 在
        /// **召唤 / 附魔那一刻**逐条 trait 调 `BattleCardUI__DisplayStatusAnim`，
        /// 后者只处理 `EffectTriggerType == Start(0)`（播放）与 `== OnPlay(2)`（挂上持久状态框）；
        /// 而 `BattleCardUI__DisplayTriggerAnim`（触发那一拍）**只认 `Trigger(3)` / `TriggerOnPlay(6)`**。
        /// ⇒ **`OnPlay(2)` 永远不在「发动技能」那一拍播**。</para>
        ///
        /// <para>我们这一侧它的播放口是 `Core/TraitFrames.cs`（`_playBook` ←
        /// `Resources/trait_effect_bindings.json` 的 `TriggerType == 2`），发生在**登场**那一拍。
        /// 生成器原来把同样的行也塞进 `ability` 桶（`TRAIT_TRIGGER_EVENT[2] = EV_ABILITY`），
        /// 而 `ability` 桶是**整桶全播**的 ⇒ 21 张卡（本批 5 张）**每发动一次主动技能都会顺带播一遍
        /// 登场框**（`FlyingEffect` / `StealthEffect` / `CamouflageEffect` / `ShieldTraitEffect_Idle*`）。
        /// ⇒ 2026-10-24 `J1` 起生成器把它们写进本桶，`ability` 桶**只留真·技能动画**。</para>
        ///
        /// <para>⛔ **别把它加进 <see cref="Events"/>**：`Events` 的语义是「引擎真的会发的事件名」，
        /// 而本桶**没有任何一拍**（`Unmapped()` / `Describe()` 会因此凭空多一条永远 null 的项）。</para></summary>
        public const string TraitOnPlay  = "trait_onplay";
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
        /// 单位翻开那一刻）。🔴 **2026-10-22（`W64` §B7）通用件入口已撤；2026-10-23（`W65`）复核后仍然撤** ——
        /// 但「为什么」已经换成确凿判据（原文那版「trait 真值表本地没有」**已过期**）。
        ///
        /// **原版的取法**（第一权威，方法体都在）：`BattleManager.GetTraitEffect(trait, FromCode(5), army)`
        /// → `CardTraitCollection.GetTraitEffectObject` → `CardTrait._traitEffects[]` 里按
        /// `EffectTriggerType` 挑那一条、再按阵营取 override（`TraitEffect.GetEffect`）。
        /// ⇒ **「伏击翻开播什么」写在 trait SO 里，不写在卡 SO 里**。
        ///
        /// ✅ **那张真值表现在本地有了**（2026-10-19 `K12` 解出、10-23 交叉验证）：
        /// `Unity/数据/游戏数据/trait_collection.json`（源 = `sharedassets0.assets` 的 `TraitsCollection`
        /// + 73 个 `CardTrait`；安卓 1.35.0 逐字段 0 差异）。
        /// 读出来的事实：**`Ambush`（DefinedTrait 1200）只有一条 `TraitEffect` = End(1)** →
        /// `Ambush Effect` → prefab **`AmbushEffect`**；**没有 `FromCode(5)`**。
        /// ⇒ 原版那条「从代码播」的链（两个 `ActivateTraitParticlesFromCode*` 都硬编码 `5`）对 ambush
        /// **本来就不播任何东西**（铁律 11 例外①：原版本身没有，不是缺口）。
        /// 而 End(1) 这一档在反编译里**没有调用点**（`DisplayStatusAnim` 0/2 · `DisplayTriggerAnim` 3/6 ·
        /// `ActivateTraitParticles*` 5 · `CardScript__RemoveTrait` 只调 `RemoveStatusAnim`）
        /// ⇒ **原版哪一刻播 End(1) 证不出来**，照铁律 3 ⛔ 不猜 ⇒ 本入口继续留空（`IsKnownGapEvent` 出声）。
        ///
        /// ✅ **U1「`AmbushEffect` 是孤儿」已结案**：trait SO 经 CardAnim `Ambush Effect` 明确引用它 ——
        /// 它**确实是 ambush 的那件**，只是槽位是 **End(1)**、不是 `FromCode(5)`。
        /// ⚠️ `EventTiming.SourceOf(EvtKind.AmbushExit)` 那段时序判据**仍然成立**（它写的是那件 prefab 自己的
        /// 1.35 s 补间）—— 只是今天没有件可播，**那条时长仍然是长度闸的上限**（没改它）。</summary>
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
        //     🔴 **2026-10-23（`T1`）就地订正（铁律 5）**：原文写「`attack_melee` 里另外 2 条
        //     （`EC29` / `SOR8`）是 `inferred-name` ⇒ 不收、留在旧桶」—— **那两条已经不在表里了**
        //     （卡 SO 没引用 `HeldrakeStrike` / `Arco-Flagellant Strike` ⇒ 无权威依据 ⇒ `notPlayed`）。
        //     ⇒ **运行期仍有件的就是 12 行**（数字没变，理由变了：改前是「2 条留在旧桶」，
        //       现在是「那 2 条根本不播」）。
        //   · `UnitDamaged(60)`：原版真值只有 **1 行**（`BL20 Buff_DiabolicStrength`）。
        //     🔴 **2026-10-23（`T1`）**：`hit` 桶改前是 4 行，其中 3 条是 `inferred-name` ——
        //     `Lord_of_the_storm_hit` / `Jammed Communications Damage` 两条**已被判为重复**
        //     （卡 SO 里同一条 CardAnim 已以 `authoritative` 记在 `play` / `ability`），
        //     `Master Interromancer Hit` **卡 SO 根本没引用** ⇒ 不播。
        //     ⇒ 本桶现在收 `hit` **1 行**。
        //   ⇒ 两个口径都要记住：**原版行数 13 + 1 = 14**（本件的验收口径）·
        //     **运行期有件 12 + 1 = 13**（`VfxMap.CardTableUnitAnimEntries` 那个数）。
        //
        // 【⚠️ 这两桶**没有** `ByEvent` 兜底】原版这两档**只有逐卡**（`cardAbilities[].abilityLogic[].
        //   cardAnimations[]`），没有「通用技能动画」这种东西 ⇒ 没配的卡就是没有
        //   ⇒ 已列进 <see cref="IsOriginalNoGenericEvent"/>（`ResolveFallback` 静默回 null，⛔ 不喊假警告）。
        //   消费端 = `CardAnimClips`（数据）+ `BattleDriver.PlayUnitSkillAnim`（播放）。
        //
        // 🔴 **2026-10-24（`A21` · `C3` 审查 D3）：这一桶的【消费时机】与【播在谁身上】还不符原版 ——
        //   已知缺陷，本文件里改不了（要动 `BattleDriver` 的 `ScheduleAttackPresentation` + `RuleEngine`）。**
        //   ✅ **2026-10-24（执行代理 `B1`，C2 §1·5 步 B/C/E）就地订正（铁律 5）：本条缺陷【已修】，下面那段
        //      「我们 / 修法」是**改之前**的记录，保留作痕迹。**修法落地 = ① `CanonicalTriggerName` 加
        //      `attacked → UnitAttack` 别名（本文件）；② `BattleDriver.PlaySignal` 的 `EvtKind.Trigger`
        //      支按 canonical 分流到 `PlayUnitSkillAnim`（**不再**走 `FireEventAll` 的通用件 ——
        //      那条路没有 `startPos/endPos` 锚点、没有卡体动画、没有 `SkillBeatDuration` 时长）；
        //      ③ 删掉 `ScheduleAttackPresentation` 末尾那句「攻方自己的 UnitAttack」。
        //      ⚠️ **仍不要**把本桶并进 `TryResolveTriggerPicks`：`ResolvedVfx` 没有字段能告诉
        //      `FireEventAll`「这一拍要用技能动画那套播」⇒ 并桶 = 把正确的数据送进错误的播放入口。
        //   原版判据（逐句读过）：
        //     · `BattleManager._ResolveAttack_d__438__MoveNext.c:987-990` → `BroadcastUnitAttacked(...)`，
        //       **近战与远程两条路都会走到**（`case 0xb/0xe/0x11/0x14` → `:1622`）。
        //     · `BroadcastUnitAttacked.c:22/29/45` → 对**被打者**、**攻方自己**、**场上每一张卡**各调
        //       `CardScript__ResolveUnitAttacked`。
        //     · `CardScript__ResolveUnitAttacked.c:28-30` → `RawCardScript__OnTrigger(0x32 = 50)`，**无条件**。
        //   ⇒ 原版：**任何一张带 `UnitAttack(50)` 能力的卡，在「任何单位攻击」（近战或远程）时都会播
        //     它自己那条技能动画，锚点 = 它自己那张卡**。
        //   我们：`BattleDriver.ScheduleAttackPresentation` 只在 `EvtKind.Attack && !e.Ranged` 时查本桶，
        //     且 `CardId` = **攻方** ⇒ 两个后果：
        //       ① **远程攻击时不播** —— 全池 11 张 `attack_melee` 卡 `ranged` 全部 > 0；其中
        //          `EC1/EC2/SW16/TL24/SAU31/UM70` 是**判据取远程**（`ranged > attack`）⇒
        //          `UM70 Invictor Warsuit` 那条 `BulletImpact_2shot_autocannon` **100% 永不播**。
        //       ② **「监听别人的攻击」那三条不播** —— `DA22 Techmarine`（我方载具攻击时）/
        //          `UM70`（敌方单位攻击时）/ `UM83 Valtus`（友方单位攻击时）。
        //   ⇒ 修法（下一批执行，⛔ 别在表现层「再补一次」了事）：不要在 `ScheduleAttackPresentation`
        //     里按 `e.Ranged` 查本桶，改挂在**引擎的 `UnitAttack` 触发事件**上（广播时每张卡各播自己
        //     那条、锚点 = 自己）—— 这需要 `RuleEngine` 的广播事件带上「触发者卡」。
        //   全量方案（逐步骤 / 断言 / 11 张卡清单）→ `资料/普查产出_1023e/A21_VfxMap白名单.md` §D3。
        /// <summary>原版 `AbilityTrigger.UnitAttack(50)` 触发的**技能动画**（逐卡；原版 13 行 / 运行期有件 12 条）。
        /// ⛔ **不是**「近战攻击特效」——`ByEvent[AttackMelee]` 已于 2026-10-22（`W63` §B）整行删除。</summary>
        public const string UnitAttack  = "unit_attack_anim";
        /// <summary>原版 `AbilityTrigger.UnitDamaged(60)` 触发的**技能动画**（逐卡；池内 **1** 条 —— `BL20`）。
        /// ⛔ **不是**「受击特效」——受击那一拍原版**一件粒子都不播**（`CardScript._ReceiveDamage`），
        /// 见 `BattleDriver.PlaySignal` 里那段注释。
        /// 🔴 2026-10-23（`T1`）：原来这里是 4 条，多出的 3 条是按名字猜的（`inferred-name`）——
        /// 其中 2 条已删重复、1 条无依据不播（见 `UnitAttack` 上面那段）。</summary>
        public const string UnitDamaged = "unit_damaged_anim";

        /// <summary>🆕 **2026-10-24（执行代理 `B1` · `C2` §1·2 · 战斗线 `D3`）**：原版
        /// `AbilityTrigger.UnitAttack = 50` 的**枚举名** —— ⚠️ **不是桶名**（桶名是 <see cref="UnitAttack"/>
        /// = `unit_attack_anim`）。表现层按它分流去 <c>PlayUnitSkillAnim</c>（判据 = 原版
        /// `CardScript__ResolveUnitAttacked.c:28-30` `OnTrigger(0x32)` → `TriggerAbilities(50)` →
        /// `CardAbility__CanTriggerAbility.c:126-140` → `PlayAbility` → `PlayAnim`）。</summary>
        public const string UnitAttackTriggerName = "UnitAttack";

        /// <summary>🆕 **2026-10-24（执行代理 `E3` · 战斗线 `D3`）**：`AbilityTrigger.UnitDamaged = 60`
        /// 的**枚举名** —— 与 <see cref="UnitAttackTriggerName"/> 同形（⚠️ 同样**不是桶名**，桶名是
        /// <see cref="UnitDamaged"/> = `unit_damaged_anim`）。
        /// <para>**它今天只有两个消费者**，都**只读**：① <see cref="SelfCheckUnitSkillAnims"/>（全池逐卡
        /// 自检要拿它去 `CardDef.AnimTimes` 里数「这张卡原版该有几行」）；② `IsRoutedTriggerName` 里那条
        /// `case "unitdamaged"` 的判据注释。⛔ **`BattleDriver` 不要改用它** —— 那一侧分流只看
        /// `UnitAttackTriggerName`（`UnitDamaged` 的播放口是 `PlayDefenderDamagedAnim`，由攻击事件直接调，
        /// 不经 Trigger 信号）。</para></summary>
        public const string UnitDamagedTriggerName = "UnitDamaged";

        /// <summary>🆕 **2026-10-24（执行代理 `B1` · 战斗线 `D3`）**：`CardDef.AttackedOps`（卡面正文
        /// 「被这套打过的单位…」）在引擎里发的 keyword —— 发射口 = `RuleCore.cs` 攻击结算段
        /// （`ctx.Emit(EvtKind.Trigger, …, keyword: "attacked")`）。
        /// <para>**为什么它也要归到 <see cref="UnitAttackTriggerName"/>**：原版那一族**不是另一个 trigger**，
        /// 就是同一步 `UnitAttack(50)` —— `BroadcastUnitAttacked` 对每张卡逐张 `OnTrigger(0x32)`
        /// （`BattleManagerSupport__BroadcastUnitAttacked.c:22 / :29 / :48-58`），差别只在**能力判据**
        /// （`CardAbility__CanTriggerAbility` 的 criteria）。⇒ 归一之后表现层按**同一档**播技能动画。</para></summary>
        public const string AttackedKeyword = "attacked";

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
        /// <para>`AmbushExit`（B7）：真值表**已解出** —— `Ambush` 只有一条 **End(1)** → `AmbushEffect`，
        /// **没有 `FromCode(5)`**；而反编译里**没有消费 End(1) 的调用点** ⇒ 原版哪一刻播它证不出来，
        /// 照铁律 3 留空 + 出声（详见 `AmbushExit` 的注释）。</para>
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
            // 🆕 2026-10-14（`A1816`）**伏击翻面**（`ambush_exit`）—— 🔴 **2026-10-22（`W64` §B7）留空；
            //   🆕 2026-10-23（`W65`）**真值表已解出，本条仍然留空 —— 但理由换了（不再是「判不了」）**：
            //   `CardTrait` SO 已逐字节解出（`数据/游戏数据/trait_collection.json`），`Ambush`（id 1200）
            //   **只有一条 `TraitEffect`：End(1)** → `Ambush Effect` → prefab **`AmbushEffect`**。
            //   它**没有 `FromCode(5)` 条目** ⇒ 原版那条「从代码播」的链（`ActivateTraitParticlesFromCode`
            //   硬编码 5）对 ambush **本来就什么都不播**（= 铁律 11 例外①，不是缺口）。
            //   而 End(1) 这一档：`d:/2/tools/decomp_full/` 里**没有任何调用点消费它**
            //   （`DisplayStatusAnim` 只认 0/2、`DisplayTriggerAnim` 只认 3/6、两个 `ActivateTraitParticles*`
            //   只认 5；`CardScript__RemoveTrait.c:62` 调的是 `RemoveStatusAnim`，不取 End effect）
            //   ⇒ **原版哪一刻播 End(1) 证不出来**，照铁律 3 ⛔ 不猜 ⇒ 入口继续撤（`IsKnownGapEvent` 仍出声）。
            //   ✅ **U1 里「`AmbushEffect` 是孤儿」这一半已结案**：trait SO 明确引用它（经 CardAnim
            //   `Ambush Effect`）—— 它**确实属于 ambush**，只是槽位是 **End(1) 而不是 FromCode(5)**。
            //   判据 → `资料/普查产出_1023e/A4_特效三件.md` · `数据/游戏数据/trait_collection.json`（ambush 段）。
            //   历史旁证（仍成立、仍不是判据）：`AmbushEffect` 的 AnimFX 音效 cue 是 `Trait Ambush`、
            //   换的材质是 `Card 3d Dissolve Blend Image Ambush`、`animinfo_lookup["AmbushEffect"]`
            //   的 CardAnim 名就是 `Ambush Effect`（`tm 1.5`）。
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
        /// 🔴 **2026-10-23（`T1` · Trait 全量权威化）：只有 `authoritative` 进得来。**
        /// `inferred-*` / `unknown` 在**数据侧**已被 `strip_unbacked_inference` 清出事件桶
        /// （删重复 / 无依据不播 → 运行时的 `notPlayed[]`），消费端**另有一道闸**再挡一遍
        /// （<see cref="CardTableNonAuthoritativeRows"/> 正常应 = 0）。以下三条是「权威行之间」的口径：
        /// · 只留**最优置信度那一档**（`confPriority` 里 rank 最小的；今天只剩一档）；
        /// · **同档全部候选都留**（判据 = 原版「all are triggered in order」—— 见上面那段）；
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

        // ============================ 🆕 2026-10-23（P2 · `新账-75③`）：**多播里钉不上 `AnimTimes` 的候选** ============================
        //   `ResolveAllTimed` 的多播桶里，天赋动画那一批（`ShieldTraitEffect_Idle` / `FlyingEffect` /
        //   `StealthEffect` / `CamouflageEffect` …）在卡的 `animTimes` 里**没有行** ⇒ `T = 0`（只有自身
        //   `vfxDelayTime`，现读全 0）⇒ 与主件**同一瞬间**起播。原版是每件各等**自己的** `vfxDelayTime`
        //   （`BattleManager__ResolveTraitAnim` 起手 `WaitForSeconds`）。
        //   ⇒ 今天**行为等价**，但原来**一个字都不说**；将来某件天赋动画的 `vd > 0` 就会静默错。
        //   ⚠️ 只对**多播桶**（`n > 1`）计数/出声（单候选钉不上是常态，见 `ResolveAllTimed` 里那一句）。
        static readonly HashSet<string> _warnedUnpinned = new HashSet<string>();
        const int UnpinnedWarnCap = 16;

        /// <summary>🆕 自检用：累计「多播桶里有 CardAnim 名却钉不上 `AnimTimes` 行」的候选数
        /// （每次解析都累加；扫全池前先调 <see cref="ResetUnpinnedForTest"/>）。</summary>
        public static int UnpinnedPicks { get; private set; }
        /// <summary>🆕 自检用：已经**出声过**的 (事件, 卡, 件) 种数（去重；上限 `UnpinnedWarnCap`）。</summary>
        public static int UnpinnedWarnCount { get { return _warnedUnpinned.Count; } }
        /// <summary>🆕 自检用：清零（不改变已载的表）。</summary>
        public static void ResetUnpinnedForTest() { UnpinnedPicks = 0; _warnedUnpinned.Clear(); }

        /// <summary>🆕 2026-10-23：多播桶里某条候选钉不上 `CardAnimTime` ⇒ 出声一次（去重 + 上限）。
        /// ⚠️ **只出声、不改变任何播放**（判据见上面那一段）。</summary>
        static void NoteUnpinnedPick(string evt, string card, CardPick p, int total)
        {
            UnpinnedPicks++;
            if (_warnedUnpinned.Count >= UnpinnedWarnCap) return;
            string key = evt + "|" + card + "|" + (p.A ?? p.N);
            if (!_warnedUnpinned.Add(key)) return;
            Debug.LogWarning($"[VfxMap] `{card}` 的 `{evt}` 有 {total} 个候选，其中 `{p.A ?? p.N}` **钉不上** "
                           + "`AnimTimes` 行（这张卡的 `animTimes` 里没有这一件的记录）⇒ 它的起点 `T` 只有自身 "
                           + "`vfxDelayTime`（现读天赋件全为 0 ⇒ **与主件同一瞬间起播**）。原版每件各等自己的 "
                           + "`vfxDelayTime`（`BattleManager__ResolveTraitAnim`）。⛔ 这不是「不播」，是**时刻未知**；"
                           + $"上限 {UnpinnedWarnCap} 条，其余只计数（`VfxMap.UnpinnedPicks`）。");
        }

        /// <summary>某件**按阵营**配的东西，这个阵营没有候选 ⇒ 出声（⛔ 不静默留白，也不硬凑一件替代品）。
        /// `what` = 那件东西的名字（事件名 / `阵营召唤法阵（deploy）`），同时做去重键与日志里的主语。
        /// <para>🔴 **2026-10-25（执行代理 `C`）：这里的 `faction` 必须是【卡自己的阵营】（原版 `cardArmy`）**
        /// —— 见 <see cref="Resolve"/> 的 doc。传成「座位阵营」时，跨阵营的卡会在这里报出一条
        /// **假缺口**（本例：卡的阵营配了、座位阵营没配），而真正该播的变体一处都没被查过。</para></summary>
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
        /// 🔴 **2026-10-23（`T1`）现读 12** —— 改前 12，**数字没变**：`attack_melee` 里那 2 条 `inferred-name`
        /// （EC29/SOR8）**不再进表**（无权威依据 ⇒ `notPlayed`），而有权威依据的 12 条照旧全搬过来。</summary>
        public static int CardTableUnitAttackEntries { get; private set; }
        /// <summary>`UnitDamaged` 桶里的行数。🔴 **2026-10-23（`T1`）现读 1**（只有 `BL20`）——
        /// 改前的 **4** 里有 3 条是 `inferred-name`（`Lord_of_the_storm_hit` / `Jammed Communications Damage` /
        /// `Master Interromancer Hit`）：前两条已被判为「卡 SO 里同一条 CardAnim 已以 authoritative 记在
        /// `play`/`ability` ⇒ 删重复」，第三条卡 SO 根本没引用 ⇒ 不播。⚠️ 断言里别写死 4。</summary>
        public static int CardTableUnitDamagedEntries { get; private set; }
        /// <summary>两个桶的行数合计。🔴 **2026-10-23（`T1`）现读 13**（改前 16）。</summary>
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

        // ================== 🔴 2026-10-23（`T1` · Trait 全量权威化）：**去兜底**的两个计数器 ==================
        /// <summary>数据里 `notPlayed[]` 的条数（= **改前在表里、现在不播**的那些，逐条出声）。
        /// 🔴 现读 **22** = 改前表里那 22 条非权威行（17 `inferred-name` + 5 `unknown`）**一条不落**：
        ///   · `outcome == "duplicate"` **16**（卡 SO 里同一条 CardAnim 已在别的桶有 `authoritative`；
        ///     **5 条 `penitence` 全在这里**）；
        ///   · `outcome == "unbacked"` **6**（卡 SO 根本没引用 ⇒ 无依据）。
        /// ⚠️ 它**不是**「解不出 prefab」那一档（那个是 `noPrefabCount`，仍在文件头字段里）。
        /// ⛔ 断言里别写死 22（数据会随生成器重跑变），要断「≥5 且 penitence 那几张在里面」。</summary>
        public static int CardTableNotPlayed { get; private set; }
        /// <summary>**第二道闸**：载入时因为 `c != authoritative` 被挡掉的行数。
        /// 🔴 **正常必须 = 0**（数据侧已经清空）；非 0 = 有人把推断又塞回了 `es[]`。
        /// 每次会话每条 (卡,事件) 只警告一次（上限 <see cref="NonAuthWarnCap"/>）。</summary>
        public static int CardTableNonAuthoritativeRows { get; private set; }
        const int NonAuthWarnCap = 24;
        const int NotPlayedWarnCap = 24;
        static readonly HashSet<string> _nonAuthWarned = new HashSet<string>();

        /// <summary>🆕 2026-10-23（`T1`）：`notPlayed` 里那些条目的**卡 id**（拷贝；断言用）。
        /// 顺序 = 数据里的顺序。⚠️ 会触发载表。</summary>
        public static string[] NotPlayedCardIds()
        {
            EnsureLoaded();
            return _notPlayedIds.ToArray();
        }
        static readonly List<string> _notPlayedIds = new List<string>();

        /// <summary>🆕 2026-10-23（`T1`）：某张卡是否在 `notPlayed` 里（断言用；按 id **或** 卡名比）。
        /// `evt` 传 `null` = 只看卡。</summary>
        public static bool IsNotPlayed(string cardIdOrName, string evt = null)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(cardIdOrName)) return false;
            for (int i = 0; i < _notPlayedIdx.Count; i++)
            {
                var x = _notPlayedIdx[i];
                if (evt != null && !string.Equals(x.Event, evt, System.StringComparison.Ordinal)) continue;
                if (string.Equals(x.Id, cardIdOrName, System.StringComparison.Ordinal)
                    || string.Equals(x.Name ?? "", cardIdOrName, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
        /// <summary>(id, 事件, 卡名) 三元组 —— `IsNotPlayed` 用；与 `_notPlayedIds` 同一次载入建立。</summary>
        struct NotPlayedKey
        {
            public string Id;
            public string Event;
            public string Name;
        }
        static readonly List<NotPlayedKey> _notPlayedIdx = new List<NotPlayedKey>();

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
        /// <summary>🔴 **2026-10-23（`T1` · Trait 全量权威化）**：**改前在表里、现在不播**的那一条。
        /// 由 `工具/gen_card_vfx_by_card.py` 的 `strip_unbacked_inference` 写出来。两种 `outcome`：
        ///   · `"duplicate"` —— 卡 SO 里**同一条 CardAnim** 已经以 `authoritative` 记在**别的桶**上
        ///     （典型 = 按名字猜成 `hit`，而卡 SO 说它属于 `ThisCardPlayed`）⇒ 删掉，免得同一件播两次；
        ///     **5 条 `penitence` `unknown` 全在这一档**（卡 SO 的 `ability` 桶里本来就有 `PenitenceTraitTrigger`）。
        ///   · `"unbacked"` —— 卡 SO 与 `CardTrait` SO **都没有引用**这条 CardAnim ⇒ 无权威依据，不播。
        /// ⚠️ 它**不是**「解不出 prefab」那一档（那个是 `noPrefabCount`），是「**猜到过、但不许播**」。
        /// 消费 = <see cref="EnsureLoaded"/>：逐条出声 + 计数，⛔ 不许静默丢掉。</summary>
        [System.Serializable] public class RtNotPlayed
        {
            public string id; public string nm; public string fa;
            public string ev; public string n; public string a;
            public string outcome;    // "duplicate" / "unbacked"
            public string why;
        }
        [System.Serializable] public class RtFile
        {
            public string[] confPriority;      // 权威优先；不认识的一律排最后
            public int notPlayedCount;         // 🆕 2026-10-23（`T1`）：`notPlayed[]` 的条数
            public RtNotPlayed[] notPlayed;    // 🆕 同上：无权威依据 ⇒ 不播（但**必须出声**）
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
        /// **取哪一条**：同一条 (卡, 事件) 可能有多个候选（都是原版真值），
        /// 按 JSON 自带的 `confPriority`（`authoritative` 最前）**只留最优那一档**；
        /// 同档有多个 ⇒ **全都留**（按序全播，判据 = 原版 `CardAbility.cs:27-29` 的 `randomAbilityChoice` 注释）。
        /// 🔴 **2026-10-23（`T1` · Trait 全量权威化）**：**非 `authoritative` 的档一律不进表**
        /// （第二道闸，见那个 `foreach` 里的注释）—— 改前「推断档只要桶里只有它一条就照样播」的行为已经没了。
        /// 数据侧同一件事由 `工具/gen_card_vfx_by_card.py` 的 `strip_unbacked_inference` 做完
        /// （删重复 / 无依据不播 → `notPlayed[]`）。
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
            // 🆕 2026-10-24（`A21`）：**白名单常驻反向闸** —— 名字级那一段不需要卡表，载表时跑一次，
            //   白名单被改错就**当场出声**（件级那一段要卡池 ⇒ `SelfCheckRoutedTriggers`，自检宿主显式调）。
            {
                var badNames = SelfCheckRoutedTriggerNames();
                for (int i = 0; i < badNames.Length; i++)
                    Debug.LogError("[Vfx][ASSERT] " + badNames[i]);
            }
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
            LoadTableFromJson(dto, "Resources/" + CardTableResourcePath + ".json", false);
        }

        // ==================================================================
        //  🆕 2026-10-24（执行代理 `A2` · 特效线「新账-72⑥」）：**合成表注入口（只给自检用）**
        //
        //  为什么需要：`EnsureLoaded` 里有一条规则 ——「非权威档（`inferred-*`）之间平票**只留第一条**」
        //  （`if (!authoritative && picks.Count > 1) picks.RemoveRange(1, …)`）。而现读**全表
        //  非权威平票行 = 0** ⇒ 任何基于真实 JSON 的断言都**只能恒绿**（= 自证）。
        //  ⇒ 本口让人喂一份**合成表**（同一个 `RtFile` 形状的 JSON），走**同一段** `LoadTableFromJson`：
        //    构造两条同档 `inferred` 行 ⇒ 断言只播第一条。
        //
        //  🔴 **生产路径零影响**：两件事保证 ——
        //    ① 本方法只有自检调（`Editor/BattleScene.cs` 的 `LoadForTest` 那一组）；`EnsureLoaded` 不经过它；
        //    ② 还原走**成对**的口子：`using (VfxMap.LoadForTest(json)) { … }`（自动、抛异常也还原），
        //       或显式 <see cref="RestoreSyntheticTableForTest"/> / <see cref="ReloadRealCardTableForTest"/>。
        //       —— 否则后面的自检会跑在合成表上，那会让别的断言静默失去意义。
        //       🔴 2026-10-24（`F2` · R1 P2-8）订正：本行原来只写「请调 `ReloadRealCardTableForTest`」，
        //          **还原只靠调用方自觉**（`try` 之前抛就漏）。判据与改法见下面那段。
        //
        //  ⚠️ **`allowNonAuthoritativeInferredTie` 这一档是干什么的**：2026-10-23（`T1`）之后，
        //    生产在**更前面**还有一道闸「非 `authoritative` 一律不进表」（`CardTableNonAuthoritativeRows`）
        //    ⇒ 今天就算喂两条 `inferred` 也**一件都留不下**，那条「只留第一条」的规则**结构上摸不到**。
        //    自检要证的是**那条规则本身**，所以本口允许显式打开「放非权威行进来」这一档
        //    （= 退回到 2026-10-23 之前的形状）。生产恒传 `false`，行为一个字不变。
        // ==================================================================

        //  🔴 **2026-10-24（`F2` · R1 P2-8）：注入/还原改成【成对且自愈】** —— 原文写的是
        //    「`LoadForTest` 之后请调 `ReloadRealCardTableForTest` 把真表放回去」，**还原只靠调用方自己写
        //    `try/finally`**：调用方若在 `try` **之前**抛 / 提前 `return`（或将来新加一个不配对 `finally`
        //    的调用点），全局共享表就**停在合成表上** —— 后面所有自检、**以及生产查询**都跑在错数据上，
        //    而且**一个字都不说**（静默）。
        //  ⇒ 现在三件事：
        //    ① `LoadForTest` **返回一个 `IDisposable` 作用域**：
        //       `using (VfxMap.LoadForTest(json)) { … }` ⇒ 出作用域**自动还原**（抛异常也还原）。
        //       老写法 `VfxMap.LoadForTest(json);`（丢掉返回值）**仍然编译、语义不变**，
        //       但那就退回「必须自己还原」的旧契约。
        //    ② 内部**记下还原点**（`_reloadTableBeforeSyntheticForTest`）：还原 = 回到**注入之前那一份表**
        //       （注入前是真表 ⇒ 重载真表；注入前是另一份合成表 ⇒ 装回它）。
        //       连续注入两次（自检里就是先 `LoadForTest(a)` 再 `LoadForTest(b)`）= **换一份合成表**，
        //       还原点仍是**第一次注入之前**（⛔ 不支持靠 `using` 嵌套回滚到中间那一层）。
        //    ③ 多一道**可断言的口子**：<see cref="IsSyntheticTableForTest"/> 与
        //       <see cref="RestoreSyntheticTableForTest"/>（幂等）—— 「合成表没留下」应该是**可断言的**，
        //       而不是只靠人记得还原（见 `F2_渲染与静态闩.md` 给 `BattleScene` 的断言片段）。
        //  🔴 **生产路径零变化**：`_tableIsSyntheticForTest` 在生产里恒 `false`，
        //     `EnsureLoaded` 与所有查询**一个字没动**。
        // ==================================================================

        /// <summary>自检用：**当前装的是合成表吗**（<see cref="LoadForTest"/> 之后为 `true`，还原之后为 `false`）。
        /// 收尾断言用它 —— 「合成表没留下」要**可断言**，⛔ 别只靠人记得还原。</summary>
        public static bool IsSyntheticTableForTest { get { return _tableIsSyntheticForTest; } }

        /// <summary>自检用：**当前这份表怎么再装一遍**（`null` = 生产真表）。</summary>
        static System.Action _reloadCurrentTableForTest;
        /// <summary>自检用：**注入之前**那一份表怎么装（`null` = 当时就是生产真表 / 还没装过）。</summary>
        static System.Action _reloadTableBeforeSyntheticForTest;
        static bool _tableIsSyntheticForTest;

        /// <summary>🆕 2026-10-24（`F2` · R1 P2-8）：<see cref="LoadForTest"/> 返回的**还原作用域**。
        /// `using` 出作用域 ⇒ 回到注入之前那一份表（<see cref="RestoreSyntheticTableForTest"/>，幂等；
        /// 抛异常也照样还原）。⚠️ 不用 `using` 时把它记下来手工 `Dispose()` 也行 —— 语义相同。</summary>
        public sealed class SyntheticTableScope : System.IDisposable
        {
            bool _disposed;
            internal SyntheticTableScope() { }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                RestoreSyntheticTableForTest();
            }
        }

        /// <summary>🆕 2026-10-24（`A2` · `新账-72⑥`）：**喂一份合成表**（自检专用；生产路径零影响）。
        /// JSON 形状 = `Resources/card_vfx_by_card.json`（`confPriority` + `cards[].events[].es[]`）。
        /// <para>⚠️ 走的是**同一段**装载逻辑（<see cref="LoadTableFromJson"/>）—— 所以它测的是
        /// **真实的过滤/分档/平票规则**，不是另写的一条。</para>
        /// <para>🔴 **2026-10-24（`F2` · R1 P2-8）**：本方法**返回一个 `IDisposable` 作用域**。
        /// 推荐 `using (VfxMap.LoadForTest(json)) { … }` ⇒ 出作用域**自动还原**（抛异常也还原）。
        /// 丢掉返回值（老写法）仍然编译，但那就**退回「必须自己还原」的旧契约** ——
        /// 那种调用点收尾必须调 <see cref="RestoreSyntheticTableForTest"/>（幂等）或
        /// <see cref="ReloadRealCardTableForTest"/>（强制真表）。</para></summary>
        /// <param name="json">合成表 JSON（`null` / 空 ⇒ 出声并清空，不装任何东西）。</param>
        /// <param name="allowNonAuthoritativeInferredTie">🆕 见上面那段：`true` = 放非权威行进表，
        /// 从而让「推断档之间只留第一条」这条规则**可被观测**。生产恒 `false`。</param>
        public static SyntheticTableScope LoadForTest(string json, bool allowNonAuthoritativeInferredTie = false)
        {
            // 第一次注入 ⇒ 记下还原点（连装两份 = 换表，还原点仍是第一次之前，见上面那段 ②）
            if (!_tableIsSyntheticForTest)
            {
                _reloadTableBeforeSyntheticForTest = _reloadCurrentTableForTest;
                _tableIsSyntheticForTest = true;
            }
            InstallSyntheticTableForTest(json, allowNonAuthoritativeInferredTie);
            _reloadCurrentTableForTest = () => InstallSyntheticTableForTest(json, allowNonAuthoritativeInferredTie);
            return new SyntheticTableScope();
        }

        /// <summary>🆕 2026-10-24（`F2` · R1 P2-8）：真正把合成表装进去（`LoadForTest` 的唯一装法 ——
        /// 还原时也走它，保证「装进去的东西」与「还原出来的东西」走**同一段**）。
        /// ⚠️ 不碰 `_tableIsSyntheticForTest` / 还原点（那是调用方的事）。</summary>
        static void InstallSyntheticTableForTest(string json, bool allowNonAuthoritativeInferredTie)
        {
            ResetCardTableForTest();
            _triedCardTable = true;            // 别让生产的懒载入把合成表覆盖掉（还原时 `EnsureLoaded` 会重新置位）
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[Vfx][LoadForTest] 传了空 JSON ⇒ 表保持清空（这不是通过）。");
                return;
            }
            RtFile dto;
            try { dto = JsonUtility.FromJson<RtFile>(json); }
            catch (System.Exception e)
            {
                Debug.LogError($"[Vfx][LoadForTest] 合成表解析失败：{e.Message}");
                return;
            }
            if (dto == null || dto.cards == null)
            {
                Debug.LogError("[Vfx][LoadForTest] 合成表形状不对（`cards` 为空）⇒ 表保持清空。");
                return;
            }
            LoadTableFromJson(dto, "（合成表 · 自检专用）", allowNonAuthoritativeInferredTie);
        }

        /// <summary>🆕 2026-10-24（`F2` · R1 P2-8）：回到**注入之前**那一份表（**幂等** —— 没注入过 /
        /// 已经还原过就什么都不做，⛔ 不会顺手清掉真表）。
        /// 注入前是真表 ⇒ 重载真表；注入前是另一份合成表 ⇒ 装回它（后者只有「不用 `using`、
        /// 手工连装两份」的调用点会走到）。</summary>
        public static void RestoreSyntheticTableForTest()
        {
            if (!_tableIsSyntheticForTest) return;
            _tableIsSyntheticForTest = false;
            var prev = _reloadTableBeforeSyntheticForTest;
            _reloadTableBeforeSyntheticForTest = null;
            ResetCardTableForTest();
            if (prev == null)
            {
                EnsureLoaded();                 // = 生产真表（载不到会自己出声，见 EnsureLoaded）
                _reloadCurrentTableForTest = null;
            }
            else
            {
                prev();
                _tableIsSyntheticForTest = true;
                _reloadCurrentTableForTest = prev;
            }
        }

        /// <summary>🆕 2026-10-24（`A2`）：把逐卡表**清空并允许重新懒载入**（自检用）。
        /// ⚠️ 只清表与计数器；`ByEvent` / `ByFaction`（写死的兜底表）不在这里（它们不是载入产物）。
        /// 🔴 2026-10-24（`F2` · R1 P2-8）：**不碰**合成表标记 / 还原点 —— 那些是
        /// <see cref="LoadForTest"/> / <see cref="RestoreSyntheticTableForTest"/> 的事
        /// （本方法被它们内部调用，这里一动就会把还原点清掉）。</summary>
        public static void ResetCardTableForTest()
        {
            ByCard.Clear();
            _byName.Clear();
            _factionOfId.Clear();
            _legendaryOfCard.Clear();
            _notPlayedIds.Clear();
            _notPlayedIdx.Clear();
            _unitAnimCards.Clear();
            _nonAuthWarned.Clear();
            CardTableEntries = 0;
            CardTableUnitAttackEntries = 0;
            CardTableUnitDamagedEntries = 0;
            CardTableMultiEntries = 0;
            CardTableCandidates = 0;
            CardTableMultiEntriesAll = 0;
            CardTableNotPlayed = 0;
            CardTableNonAuthoritativeRows = 0;
            _triedCardTable = false;
        }

        /// <summary>🆕 2026-10-24（`A2`）：把**真表**放回去（清空 + 懒载入 `Resources/card_vfx_by_card.json`）。
        /// ⚠️ 自检用完 <see cref="LoadForTest"/> **必须**还原 —— 否则后面的自检会跑在合成表上。
        /// 🔴 2026-10-24（`F2` · R1 P2-8）：本方法是**强制真表**那一档（连合成表标记/还原点一起清）；
        /// 一般情形优先用 <see cref="SyntheticTableScope"/>（`using`）或 <see cref="RestoreSyntheticTableForTest"/>
        /// ——那两个还原到的是「注入之前那一份表」，不会无脑盖掉调用方原来的状态。</summary>
        public static void ReloadRealCardTableForTest()
        {
            _tableIsSyntheticForTest = false;
            _reloadTableBeforeSyntheticForTest = null;
            _reloadCurrentTableForTest = null;
            ResetCardTableForTest();
            EnsureLoaded();
        }

        /// <summary>🆕 2026-10-24（`A2` · `新账-72⑥`）：**真正把一份 `RtFile` 装进表里** ——
        /// `EnsureLoaded`（生产）与 `LoadForTest`（自检）**共用这一段**（⛔ 别在别处再写第二份过滤逻辑）。
        /// <param name="sourceLabel">日志里那份表的来源（生产 = `Resources/...json`，自检 = 「合成表」）。</param>
        /// <param name="allowNonAuthoritativeInferredTie">见 <see cref="LoadForTest"/> 的 doc；生产恒 `false`。</param>
        /// </summary>
        static void LoadTableFromJson(RtFile dto, string sourceLabel, bool allowNonAuthoritativeInferredTie)
        {
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
                        // 🔴 **2026-10-23（`T1` · Trait 全量权威化）：只收 `authoritative`。**
                        //   以前这里是「按 `confPriority` 取最优档」，于是**推断档、`unknown` 档只要
                        //   桶里只有它一条就照样播**（数据里那 17 条 `inferred-name` + 5 条 `penitence`
                        //   `unknown` 就是这么进画面的）。用户 2026-10-23 明确要「把兜底都去掉」⇒
                        //   消费端也上闸：**不是 `authoritative` 的一律不进表**，并出声 + 计数。
                        //   ⚠️ 数据侧（生成器）已经把那些条目移走了 ⇒ 正常应恒为 0；
                        //      这条闸是**第二道**（数据回退时不会静默把推断播出去）。
                        if (!string.Equals(e.c ?? "", ConfAuthoritative, System.StringComparison.Ordinal))
                        {
                            // 🆕 2026-10-24（`A2` · `新账-72⑥`）：自检的合成表可以**显式**放非权威行进来，
                            //   好让下面「推断档平票只留第一条」那条规则**可被观测**（生产恒 false）。
                            if (!allowNonAuthoritativeInferredTie)
                            {
                                CardTableNonAuthoritativeRows++;
                                if (_nonAuthWarned.Count < NonAuthWarnCap && _nonAuthWarned.Add(c.id + "|" + ev.ev))
                                    Debug.LogWarning($"[Vfx] 逐卡表里有一条**非权威**档（`{e.c}`）："
                                                   + $"卡 `{c.nm ?? c.id}` 的 `{ev.ev}` → `{e.n}` ⇒ **不播**"
                                                   + "（2026-10-23 起只收 `authoritative`；数据侧应已清空，这里是第二道闸）");
                                continue;
                            }
                            CardTableNonAuthoritativeRows++;
                        }
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
                    // 🆕 **2026-10-23（`T1`）**：上面那道闸已经把非权威档全挡掉了 ⇒ 走到这里的
                    //   `picks` **全是 `authoritative`**，「`authoritative` 才全播」那条判断现在是
                    //   **结构性恒真**。⛔ 别删它 —— 它是数据回退时的第二道保险（删了就会把
                    //   `CardTableMultiEntriesAll == CardTableMultiEntries + 非权威平票行` 那条断言
                    //   变成两条恒等的自证）。
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
                    //   `hit` 那几条同理（受击那一拍原版一件粒子都不播，见 `PlaySignal` 那段注释）。
                    //   🔴 **2026-10-23（`T1`）就地订正（铁律 5）**：本段原文写「`attack_melee` 里另外 2 条
                    //   `inferred-name`（EC29/SOR8）留在旧桶」—— **那两条已经不在表里了**
                    //   （卡 SO 没引用 `HeldrakeStrike` / `Arco-Flagellant Strike` ⇒ 无权威依据 ⇒ 不播，
                    //   见 `notPlayed`）。同理 `hit` 从 4 条降到 **1 条**（只剩 `BL20` 那条 authoritative）。
                    //   ⇒ **`UnitDamaged` 现在只有 BL20 一张卡**（`CardTableUnitDamagedEntries == 1`）。
                    //   `attack_melee` 剩下的 12 条全是 authoritative ⇒ 全搬进 `UnitAttack`。
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
            // 🔴 **2026-10-23（`T1`）：`notPlayed[]` —— 有原版动画、但**没有权威依据**、所以不播的那些。**
            //   以前它们就混在 `es[]` 里（`c: inferred-name` / `unknown`）⇒ **桶里只有它一条时照样播**，
            //   这就是「按名兜底」。现在生成器把它们挪出来，这里**逐条出声**（⛔ 不许静默丢掉）。
            if (dto.notPlayed != null && dto.notPlayed.Length > 0)
            {
                int shown = 0;
                for (int i = 0; i < dto.notPlayed.Length; i++)
                {
                    var x = dto.notPlayed[i];
                    if (x == null) continue;
                    CardTableNotPlayed++;
                    if (!string.IsNullOrEmpty(x.id)) _notPlayedIds.Add(x.id);
                    _notPlayedIdx.Add(new NotPlayedKey { Id = x.id, Event = x.ev, Name = x.nm });
                    if (shown >= NotPlayedWarnCap) continue;
                    shown++;
                    Debug.LogWarning($"[Vfx] **不播**（{x.outcome ?? "?"}；⛔ 不是解失败）：卡 `{x.nm ?? x.id}` 的 "
                                   + $"`{x.ev}` 档 `{x.a ?? "?"}` → `{x.n}` —— {x.why}");
                }
                if (shown < CardTableNotPlayed)
                    Debug.LogWarning($"[Vfx] …另有 {CardTableNotPlayed - shown} 条同类，"
                                   + $"全表见 `{sourceLabel}` 的 `notPlayed`");
            }
            Debug.Log($"[Vfx] 逐卡特效表已载入：{ByCard.Count} 张卡 / {CardTableEntries} 条"
                    + $"（`{sourceLabel}`，"
                    + (allowNonAuthoritativeInferredTie
                        ? "⚠️ **自检档**：非权威行也放行了（只为观测「推断档平票只留第一条」那条规则）"
                        : "🔴 2026-10-23 起**只收 `authoritative`**") + "）"
                    + $"；卡名索引 {_byName.Count} 个名字（🆕 `A2082`：生产路径传的是**卡名**，"
                    + "没有这个索引逐卡那一档就恒不命中）"
                    + $"；🆕 `T13` 全播：{CardTableMultiEntries} 条会播多件 / 候选共 {CardTableCandidates} 件"
                    + "（= 最优档 `authoritative` 的同档平票，按原版 `abilityLogic` 顺序）"
                    + $"；⛔ 无权威依据**不播** {CardTableNotPlayed} 条（已逐条出声）"
                    + $"；非权威行 {CardTableNonAuthoritativeRows} 条"
                    + (allowNonAuthoritativeInferredTie ? "（本档放行）" : "（被第二道闸挡掉，正常应 = 0）"));
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
        /// 钉不出来 ⇒ **出声 + 返回 null**（宁可不播，也不给错特效）。
        ///
        /// <para>🔴🔴 **2026-10-25（执行代理 `C` · 特效线账）：`faction` 的语义 = 这张卡【自己】的阵营
        /// （原版 `cardArmy` / `RawCardScript.army`），⛔ 不是「这个座位这一局的阵营」。**
        /// 原版每一个逐卡 / clan 级的表都是按 `cardArmy` 索引的；我们过去在生产调用点一律传
        /// `FactionOf(seat)`，整副牌同阵营时看不出差别，**跨阵营的卡**（效果生成的卡 / 教程关卡牌 /
        /// 将来混编）就会取到另一个阵营的同名变体、或撞上「重名钉不住 ⇒ 整档不生效」。
        /// ⇒ 生产侧的**唯一取值口** = `BattleDriver.VfxFactionOf`（判据与取值顺序在它的 doc）。
        /// ⚠️ 本类**不替调用方猜**：`faction` 传空时按无阵营处理（重名那档直接出声、不生效）。</para></summary>
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
        /// <param name="faction">🔴🔴 **这张卡【自己】的阵营（原版 `cardArmy`）**，⛔ 不是座位阵营 ——
        /// 见 <see cref="Resolve"/> 的 doc 那一段（生产侧唯一取值口 = `BattleDriver.VfxFactionOf`）。</param>
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
                if (idx < 0)
                {
                    // 🆕 2026-10-23（P2 · `新账-75③`）：**多播桶里钉不上的候选要出声一次**。
                    //   行为**一个字不变**（照旧 `T` 只有自身 `vfxDelayTime`）—— 原来这条**彻底静默**：
                    //   现读 `ability` 桶里这类候选 **68** 个（天赋动画 `ShieldTraitEffect_Idle` /
                    //   `FlyingEffect` / `StealthEffect` … 它们不在卡的 `animTimes` 里）；
                    //   今天等价只是因为**天赋动画的 `vfxDelayTime` 全为 0**，将来数据一变就会静默错。
                    //   ⚠️ 只对 `n > 1`（多播）出声：单候选钉不上是**常态**（逐卡攻击件大多没有 `animTimes` 行），
                    //      对它出声会把真正的告警淹没。
                    if (n > 1) NoteUnpinnedPick(evt, cardIdOrName, picks[i], n);
                    continue;              // 钉不上 ⇒ 该条 T 只有自身 `vfxDelayTime`
                }
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

        /// <summary>🔴🆕 **2026-10-24（执行代理 `J1` · `C5` D13）：阵亡那一拍该播的那件** ——
        /// **只认原版 `customDeathParticles` 那一族**，⛔ 不拿 `death` 桶里的第一条。
        ///
        /// <para>**为什么需要另开一个口**（判据 = 全量反编译，逐句读）：
        /// 原版 `CardScript._UnitDeath_d__446__MoveNext.c` 的阵亡粒子是**二选一** ——
        /// `:233` 逐卡 `customDeathParticles`、`:240` 判空 ⇒ `:262` 默认 `cardDestroyFX`
        /// （= `Card 3D Death Explosion`）。`death` 桶里**没有**第二种东西。</para>
        ///
        /// <para>🔴 **我们运行期表的 `death` 桶里混着第二种**：生成器把
        /// `cardAbilities[].abilityLogic[].cardAnimations[]` 里 `AbilityTrigger = ThisUnitDeath(80)` /
        /// `UnitDeathByAbility(180)` 的动画也归到 `death`（`工具/gen_card_vfx_by_card.py` 的
        /// `TRIGGER_EVENT`）—— 那两族是「**死于能力**那一拍的能力动画」，**不是**阵亡爆散体
        /// （原版由 `TriggerAbilities(80/180)` 派发，不是 `UnitDeath` 的 `Instantiate`）。
        /// ⇒ `CardFeel.PickDeathFx` 原来调 `ResolvePerCard(Death, …)` 取 `picks[0]`，
        /// 现读 **6 行 / 6 张卡**的阵亡爆散体会播成**别的能力的特效**：
        /// `AM34`→`GuardSummon` · `BL62`→`Buff_MarkOfTzeentch` · `BL66`→`Buff_Chaos_Star` ·
        /// `GOF83`→`BeastSnagga_Summon` · `GOF3`→`Squig_Explosion`（`SAU72` 两条里第一条恰好是对的）。
        /// （全表由 `资料/普查产出_1023e/C5_动画特效审查_索引.md` §四 D13 现读）。</para>
        ///
        /// <para>**判据（运行期表里唯一分得出来的那一格）= `CardPick.A`（`CardAnim` 名）空**：
        /// `customDeathParticles` 在原版是 `AssetReferenceGameObject` ⇒ 解出来**没有 `CardAnim`**
        /// （`工具/gen_card_vfx_by_card.py` 的 `resolve_so_ref`：`type == 'GameObject'` ⇒ 只填 `prefab`）；
        /// 而 `cardAbilities[..].cardAnimations[]` 那一族**必带** `CardAnim`（它就是 CardAnim 引用）。
        /// 现读全池逐条核过：`death` 桶 **72 行 = 66 行 `A` 空（`customDeathParticles`）**
        /// `+ 6 行 `A` 非空（`cardAbilities` 那 6 条）` —— 没有第三种形状。</para>
        ///
        /// <para>⚠️ **不改数据侧的理由**（如实记）：按原版分桶最彻底的做法是让生成器把这两族
        /// 另立一个桶（`C5` §四 D13 修法 ①），但本批白名单不允许改数据产物 ⇒ 在**消费口**把
        /// 「不是爆散体」的那一族挡掉。⛔ 别把这里改成「按 A 猜名字」或「取最后一条」——
        /// 判据只有「这一行是不是卡 SO 的阵亡字段」。</para>
        ///
        /// <para>返回 `null` = 这张卡**没有**逐卡 `customDeathParticles` ⇒ 调用方用默认件
        /// （原版 `_UnitDeath…:240` 那个二选一）。⚠️ 与 <see cref="ResolvePerCard"/> 并存：
        /// 后者仍是「`death` 桶第一条」的**原始**查询（保留给需要看整桶的地方 / 自检对照）。</para></summary>
        public static string ResolveDeathParticle(string faction, string cardIdOrName)
        {
            List<CardPick> picks;
            if (!TryResolvePerCardPicks(Death, faction, cardIdOrName, out picks) || picks == null) return null;
            for (int i = 0; i < picks.Count; i++)
                if (string.IsNullOrEmpty(picks[i].A)) return picks[i].N;
            return null;
        }

        /// <summary>🆕 2026-10-24（执行代理 `J1` · `C5` D13 自检用）：`death` 桶里**不属于
        /// `customDeathParticles`** 的那几行（= `CardAnim` 非空的「死于能力」动画）。
        /// 只给断言用（证明我们**没有删数据**，只是不再拿它们当阵亡爆散体）。</summary>
        public static string[] DeathBucketAbilityRows(string faction, string cardIdOrName)
        {
            List<CardPick> picks;
            if (!TryResolvePerCardPicks(Death, faction, cardIdOrName, out picks) || picks == null)
                return new string[0];
            var bad = new List<string>();
            for (int i = 0; i < picks.Count; i++)
                if (!string.IsNullOrEmpty(picks[i].A)) bad.Add(picks[i].N);
            return bad.ToArray();
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
        //
        //  🔴 **2026-10-24（`A21` · `C3` D5）常驻说明：`Wait_*` 纯计时拍在运行期表里被折叠【是对的】——
        //     ⛔ 别当缺陷去修，修了会丢延迟。**
        //     原版 `cardAnimations[]` 每一条各当一拍入队，`Wait_1s` / `Wait_1,5s` / `Wait_300ms` / `Wait_2s`
        //     这种拍**不出件、只占时长**（它们的 `animAdressable` 是**空 GUID** ⇒ 原版本来就没有 prefab；
        //     `数据/索引/anim_address_map.json` 的 `unresolved.cardAnimBlankGuid` 逐条列着）。
        //     生成器写运行期表时把 `Prefab == 空` 的拍剔掉，但**延迟没有丢** —— 它烙在
        //     `cards_engine.json` 的 `animTimes[].t`（组偏移 = 起点）里。逐卡四例的实测对照（`t` 值）见
        //     `资料/普查产出_1023e/C3_动画特效审查_索引.md` §三·4（`GOF30` 0.0→0.3 · `GOF3` 0.0→1.0 …）。
        //     ⛔ 两种「修法」都是错：补回一条 `Wait_*` 占位 = **延迟再算一遍**（特效晚到）；
        //        把 `t` 清零 = **特效提前到**。
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
        /// ⚠️ **只放行「引擎按名字发出来的那一批」** —— 新增一条必须**有一个真正的发射口**
        /// （判据 = 全仓 `Core/` 里有一处带这个名字（或它的 `KeywordTable.*` 小写常量）发
        /// `EvtKind.Trigger`）；⛔ 不许「照着原版 trigger 名字表补齐」。
        /// 🔴 **2026-10-24（执行代理 `W5` · AN1 `R5`）就地订正（铁律 5）**：这一行原来写
        /// 「`Strike` / `Slay` / `Regiment` 这些**走旧路的一律不动**（它们今天怎么播就怎么播）」——
        /// **三个都已过期**：`Regiment` 是 `J1`/`C5 D11` 放行的（同批），`Strike`(580) / `Slay`(120)
        /// 是本次放行的（见下面 `case "strike"` / `case "slay"` 那两段）。那句「走旧路」当初的意思是
        /// 「不做归口 → 保持整桶播」；可**整桶播不是原版行为**（原版 `TriggerAbilities(triggerId)`
        /// 只取那一条 trigger 的 ability）⇒ 那正是 AN1 `R5` 记的 36 行 / 32 卡的缺陷。
        /// ⚠️ 仍然成立的那半句：**别把「引擎已会发」当成放行的充分条件** —— 还要**看候选侧有没有行**
        /// （`MustRouteTriggerNames` 的件级自检会报「全表一张卡都没有 ⇒ 空放」）。今天
        /// `rally` / `landing` / `sacrifice` / `spentallmana` / `survivor` / `swarm` / `abouttoattack`
        /// 这些**有发射口但 `cards_engine.animTimes` 里 0 行**（`an1_trigger_matrix.tsv` 现读）⇒
        /// 本批**没有**放行（放了就是空放，且会改变第 ④ 档的取件范围，没有判据支撑）。
        /// 🆕 **2026-10-25（执行代理 `R` · 引擎线 `A3`）补一个例外**：`Talent`(46) 也是 **0 行**，
        /// 但**有判据** —— 原版这一拍的画面**完全**由链② 的 trait 框承担（`Talent.asset` =
        /// `triggerType 3 / TalentTraitTrigger / TalentEffect`），且引擎侧已发事件。
        /// ⇒ 它进了白名单，并在 `TraitFrameOnlyTriggerNames` 里单独记账（件级自检对它问的是
        /// 「链② 绑着那件吗」，不是「逐卡有行吗」）。⛔ 别照这个例外去放行上面那 7 个没人查过账的名字。
        /// 🔴 **2026-10-24（`A21`）就地订正（铁律 5）**：这一段原来把 **`Mob`** 也写进「一律不动」——
        /// 它**有真发射口**（`RuleCore.cs:6008`），2026-10-24 已放行（见下面 `case "mob"` 那一段）。
        /// 同批还放行了 `Teleport` / `Uprising` / `Artifice`，并修好了 `Codex` 的**大小写**（C3 D6）。
        /// 🆕 **2026-10-24（`A27` · `C4` D6(C4)）**：再放行 `Cruelty` / `Stimulation` / `Ecstasy`
        /// （27 行 / 24 张，全在 EmperorsChildren；发射口逐处现核，见下面那一段 `case`）。
        /// 🆕 **2026-10-24（`A30` · `C4` D7 第二批）**：再放行 `UnitReceivesHuntMark`(520) /
        /// `OnCollectFaith`(665) —— 这两个的发射口是**监听者那一拍**（`EffectResolver.EmitListenerTrigger`），
        /// 映射补在 `TriggerNameOfWhenKind` 的 `GetsHuntMark` / `GainFaith` 两支
        /// ⇒ 发出来的 `BattleEvent.Keyword` 是**原版枚举名（PascalCase）**，不是引擎关键词（见下面那一段 `case`）。
        /// 🆕 **2026-10-24（引擎线 `G2` · `C4` D8/D9）**：再放行 `ThisUnitSummoned`(195)（`SW10` 的**自部署**，
        /// 映射按 `WhenEvent.SelfOnly` 分流）与 `WhileInPlay`(20)（`SW18` 的常驻重算，
        /// 发射口 = `RuleCore.EmitWhileInPlayPass`，挂在**动作队列排空**那一刻）。</para>
        /// <para>🆕 **2026-10-24（执行代理 `J1` · `C5` D11/D12）**：再放行两个 ——
        /// `Regiment`(301)（引擎正在发：`RuleCore.cs:6030` 远程攻击尾部，14 行 / 10 张 AM）与
        /// `TriggeredSynapse`(630)（映射同批补在 `EffectResolver.TriggerNameOfWhenKind` 的
        /// `TriggersPrefix` 分支，1 行 / 1 张 `TL18`）。判据见下面各自那一段 `case` 的注释。</para></summary>
        static bool IsRoutedTriggerName(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) return false;
            // 🔴🔴 **2026-10-24（`A21` · `C3` D6）：大小写必须在【这里】归一 —— 这是踩过的坑，别再犯。**
            //   引擎发出来的 keyword 是 `KeywordTable.*` 的**小写常量**（`codex` / `mob` / `uprising` /
            //   `teleport` / `artifice` / `penitence` …：`CardDef.cs:5361/5412/5424/5070/5314`），
            //   `FireTriggerAt` 的 `emitSignal` **原样**写进 `BattleEvent.Keyword`（`RuleCore.cs:10706`）。
            //   而原版枚举名 / 这里历史的 `case` 标签是 **PascalCase**。
            //   C# 的 `switch (string)` 是 **Ordinal（大小写敏感）** ⇒ 原来的 `case "Codex"` 对 `"codex"`
            //   **恒 false** ⇒ **23 张 Ultramarines / 32 条 Codex 逐卡动画一直没按 Codex 播**（整桶播）。
            //   ⇒ 统一 `ToLowerInvariant()` 再比；`CanonicalTriggerName` 仍在外面先把 `penitence` 换成 `Rage`。
            //   ⚠️ 用 `ToLowerInvariant` 而不是 `ToLower`：后者按当前 culture（土耳其语下 `I` 会变 `ı`）。
            //   ⚠️ 归一**不会撞名**：原版 `AbilityTrigger` 枚举（`dump.cs:20235-20335`）里
            //      **没有任何两个名字只差大小写**（2026-10-24 逐个核过）⇒ 小写化不产生歧义。
            //   ⚠️ 与下面两条 `OrdinalIgnoreCase` 的 `Ambush` / `TriggerEnchantment` 判断**同口径**。
            if (string.Equals(triggerName, KeywordTable.Ambush, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(triggerName, KeywordTable.TriggerEnchantment, System.StringComparison.OrdinalIgnoreCase))
                return true;
            switch (triggerName.ToLowerInvariant())
            {
                // ---- A · followAfterResolution ----
                case "followupresolution":
                // ---- E · 监听者 / 自发反应 ----
                case "othercardplayed":
                case "othercarddrawn":
                case "othercardspiritstone":
                case "otherunitsummoned":
                case "otherunitdeath":
                case "unitattack":
                case "unitdamaged":
                case "unitreceivesmarkofchaos":
                case "reanimated":
                case "triggeredferocity":
                case "triggeredpray":
                case "triggeredduty":
                case "triggeredswarm":
                // ---- F · 回合起止 ----
                case "turnstart":
                case "turnstartplayer":
                case "turnend":
                case "turnbeforeend":
                // ---- 🆕 2026-10-19（第五十七会话 `W57` · T21 C/D）：阵营能力自己那一拍 ----
                //   `AbilityTrigger.UseFaith = 605` / `UseSpiritStone = 600`。
                //   发射口 = `EffectResolver.ResolveFaithAbility` / `ResolveSpiritAbility`
                //   （经 `EmitAbilityUseTrigger`；闸 = `CardDef.HasAnimTrigger`）。
                //   ⚠️ 这一族以前**没有引擎事件**，`card_vfx_by_card.json` 的
                //   `abilityEventWhy` 逐行写着「未归口（保持 `ability`）」—— 加进来之后
                //   `TryResolveTriggerPicks` 就能按名字从两张桶里挑出这三条候选。
                case "usefaith":
                case "usespiritstone":
                    return true;
                // ---- 🆕 2026-10-23（引擎线 `codex 能力触发`）：`Codex` ----
                //   发射口是真的：`RuleCore.CheckCodex` → `FireTriggerAt(..., KeywordTable.Codex)`
                //   （`RuleCore.cs:11233`；keyword = `KeywordTable.Codex` = **`"codex"`**，`CardDef.cs:5412`）
                //   → `EvtKind.Trigger`。
                //   候选侧原版名 = `CardAnimTime.TriggerName = "Codex"`；现读（2026-10-24）
                //   **23 张卡 / 33 行**，其中 **32 行有件**（`UM28` 的那条 `Wait_1s` 是纯计时拍 = D5）。
                //   🔴 **2026-10-24（`A21` · `C3` D6）就地修正**：原来这一条写成 `case "Codex"`，
                //   **对引擎发的 `"codex"` 恒 false** ⇒ 这一条等于没写（整桶播）。现在靠方法头那句
                //   `ToLowerInvariant()` 修好；⛔ 别把标签再改回 PascalCase（那会静默复发）。
                case "codex":
                // ---- 🆕 2026-10-23（特效线「新账-86」）：`Rage`（= `penitence` 的**原版枚举名**）----
                //   判据三条（全文 → `资料/普查产出_1023c/Trait_pentitence与静音桶溯源.md` §A / §D）：
                //     · `penitence` = `DefinedTrait.rage = 280` 的**官方显示名**：
                //       `Unity/工具/gen_cards_engine.py:2002` 的 `TRAIT_DISPLAY["rage"] = "Penitence"`；
                //       官方本地化 `Card_Trait/rage` = [「Penitence」,「忏悔」]，而
                //       **`Card_Trait/penitence` 官方 0 条** ⇒ 它**不是**另一个 trait。
                //     · 引擎按**规范键**发事件：`RuleCore.HurtCore` → `FireTriggerOnBoard(..., KeywordTable.Penitence)`
                //       （`RuleCore.cs:7952`；`doubleRage` 那一路 `:7994` 同），事件载荷里的 `keyword`
                //       就是小写的 `"penitence"`（`KeywordTable.Penitence`，`CardDef.cs:4882`）。
                //     · 候选那一侧的原版名 = `AbilityTrigger.Rage = 130`
                //       ⇒ 五张卡（`SOR8/10/21/27/32`）的 `CardAnimTime.TriggerName` 全是 **`"Rage"`**
                //       （`cards_engine.json` 现读，5 张全一样）。
                //   ⇒ 两边**永不相交**：不放行的话 `target` 恒为 null ⇒ 这一拍要么播 `trigger` 粗桶
                //      （`SOR8` = `FlankEffect`，**错件**）、要么什么都不播（`SOR10/21/27/32`）。
                //   ⚠️ **只有 `Rage` 这一条**：原版 `rage` 的 `traitEffects` 是**空数组**
                //      （`数据/游戏数据/trait_collection.json`）⇒ ⛔ `trait_effect_bindings.json`
                //      **不该**加 `penitence` 行（缺的不是数据、是这一处归口）。
                case "rage":
                    return true;
                // ---- 🆕 2026-10-24（`A21` · `C3` 审查 D1-a / D1-b）：`Mob`(300) / `Teleport`(690) ----
                //   **判据 = 引擎真的带着这两个关键词发过 `EvtKind.Trigger`**（不是推断的）：
                //     · `Mob`(300)：`RuleCore.cs:6008` `FireTriggerAt(ctx, attacker, KeywordTable.Mob, p, atkSlot)`
                //       （近战攻击结算尾部那条「群体」；keyword = `KeywordTable.Mob` = **`"mob"`**，
                //       `CardDef.cs:5070`）。候选侧原版名 = `CardAnimTime.TriggerName = "Mob"`
                //       （`cards_engine.json` 现读 **11 张卡 / 15 行**：`GOF9/12/17/22/67/70/81/84/87/96/97`）。
                //     · `Teleport`(690)：`RuleCore.cs:3674` `FireTriggerAt(ctx, unit, KeywordTable.Teleport, p, slot)`
                //       （「当回合从牌库抽到即打出」那一支；keyword = **`"teleport"`**，`CardDef.cs:5314`）。
                //       候选 6 张 / 9 行（`DA17/30/32/37/65/67`）。
                //     ⚠️ 引擎发的是**小写**、候选是 PascalCase：**白名单这一步**靠方法头的
                //       `ToLowerInvariant()` 对上；候选匹配那一步靠 `NameIs` 的 `OrdinalIgnoreCase`。
                //       两处都要，少一处就是「加了白名单还是不播」（Codex 就是这么坏的 —— D6）。
                //   **不放行的后果（`C3` 实测）**：这两个 trigger 的那一拍 `target` 恒 null ⇒
                //     `TryResolveTriggerPicks` 走「④ 判不出 ⇒ 原样回 `trigger` 桶」⇒ **整桶播**：
                //     · `Teleport` 那 6 张的 `trigger` 桶里**全部**是 `ArmourEffect`
                //       （`ArmourTraitStart`、`EffectTriggerType.Start(0)`）⇒ 传送时播**护甲起效件** = 错件；
                //     · `Mob` 那 11 张的桶里混着 `TideEffect` / `RallyEffect` / `TalentEffect`（Start(0)）
                //       以及 `MobEffect`（Trigger(3)）⇒ 整桶一起播 = 错件 + 多播。
                //   ⚠️ **放行后如实的取舍（⛔ 别当回归去「修」）**：`MobEffect` / `ArmourEffect` 这类
                //     **trait 件在 `CardDef.AnimTimes` 里没有行** ⇒ 分类键（`AddTriggerNamesOf`）标不上名 ⇒ 被
                //     「未标名的一律丢」那条规则丢掉 ⇒ 这一拍只播**卡自己那条技能动画**。
                //     原版两件都播（trait 的 `DisplayTriggerAnim` + `PlayAbility` 各一趟）。
                //     ⇒ 补齐属于 **D4 / `P7`（`trigger` 桶按 `traitTriggerType` 分档）**：要先把
                //     `keyword` / `traitTriggerType` 落进运行期表，才能把「**属于这个 trigger 的** trait 件」
                //     与 Start(0) 那批分开。⛔ 别为了让 `MobEffect` 回来而放宽「未标名一律丢」——
                //     那会把 `TideEffect` / `RallyEffect` 一起放回来（错件 + 多播）。
                case "mob":
                case "teleport":
                // ---- 🆕 2026-10-24（`A21` · `C3` 第二批 D7-a / D7-b）：`Uprising`(680) / `Artifice`(560) ----
                //   两个都**只差白名单**（引擎已在发，2026-10-24 现核）：
                //     · `Uprising`(680)：`RuleCore.cs:3604` `FireTriggerOnSide(ctx, p, KeywordTable.Uprising, unit)`
                //       （「之后每部署一个部队时」；keyword = **`"uprising"`**，`CardDef.cs:5424`）
                //       → `FireTriggerAt` → `EvtKind.Trigger`。候选现读 **8 张 / 13 行**（Genestealers）。
                //     · `Artifice`(560)：`EffectResolver.cs:2187` `FireTriggerOnSide(ctx, p, KeywordTable.Artifice)`
                //       （「每次你打出战术时」；keyword = **`"artifice"`**，`CardDef.cs:5361`）。
                //       候选现读 **11 张 / 14 行**（`BL32` + `GSC24/31` + **Sautekh 8 张**——
                //       C3 第二批只覆盖新三阵营，所以它记的是「3 张 / 6 条」；⛔ 断言里别写死 3）。
                //   ⛔ **下面这三个名字【不许】跟着加**（引擎没有发射口）：`UnitLoseStealth`(647) /
                //     `UnitStunned`(291) / `OnSabotageCreated`(701)
                //     —— 见 `NoEmitTriggerNames` 那条常驻反向闸（`SelfCheckRoutedTriggerNames`）。
                //   🔴 **2026-10-24（引擎线 `G1b` · `C2` §2 · D8）就地订正（铁律 5）**：这一段原来还列着
                //     `OnCollectSpiritStone`(610) —— 它的发射口**同批补上了**
                //     （`TriggerNameOfWhenKind` 的 `GainSpirit` 支），见下面那一段 `case`。
                //   ⛔ 同理 `Unstable` / `OnSecretCreated` / `OnCollectQuestPoints` / `TriggeredCodex` 也不许加。
                case "uprising":
                case "artifice":
                    return true;
                // ---- 🆕 2026-10-24（`A27` · `C4` D6(C4)）：`Cruelty`(720) / `Stimulation`(710) / `Ecstasy`(715) ----
                //   三个都**只差白名单**（引擎已经在发；2026-10-24 `A27` 逐个现核，发的都是
                //   `KeywordTable.*` 的**小写常量**）：
                //     · `Cruelty`(720)：`RuleCore.cs:8076` `FireTriggerOnSide(ctx, ctx.Active, KeywordTable.Cruelty)`
                //       —— `HurtCore` 尾部的「**你的回合**、**敌方**单位受伤**未死**时，**己方**带该词的牌各响一次」；
                //       keyword = `KeywordTable.Cruelty` = **`"cruelty"`**（`CardDef.cs:5321`）。
                //       `FireTriggerOnSide` 逐单位走 `FireTriggerAt` → `EvtKind.Trigger`。
                //       候选现读（`cards_engine.json`，判据 = `animTimes[].tn == "Cruelty"`）
                //       **8 张卡 / 10 行，全部 EmperorsChildren**：`EC8/20/22/23/24/26/28/35`
                //       （`EC23`/`EC26` 各 2 行）。逐行都有 prefab（0 行 `n` 为空）。
                //     · `Stimulation`(710)：`EffectResolver.cs:2080` `FireTriggerAt(ctx, st, KeywordTable.Stimulation, sp, sslot)`
                //       —— `PlayTactic` 的「**被战术选中时、结算前**」；keyword = **`"stimulation"`**（`CardDef.cs:5428`）。
                //       候选 **10 张 / 10 行**（`EC3/6/10/13/14/18/21/22/55/56`），逐行都有 prefab。
                //     · `Ecstasy`(715)：`RuleCore.cs:8065` `FireTriggerOnBoard(ctx, u, KeywordTable.Ecstasy)`
                //       —— `HurtCore` 尾部的「生命降到阈值 X 及以下**未死**」；keyword = **`"ecstasy"`**（`CardDef.cs:5241`）。
                //       候选 **7 张 / 7 行**（`EC5/7/9/25/30/31/57`），逐行都有 prefab。
                //   **不放行的后果（`C4` 实测，2026-10-24）**：这三族那一拍 `target` 恒 null ⇒
                //     `TryResolveTriggerPicks` 走「④ 判不出 ⇒ 原样回 `trigger` 粗桶」⇒ 播的是
                //     **trait 状态框那几件**（`EC35` 播 `FlankEffect` + `CrueltyEffect`、`EC9` 播
                //     `EcstasyEffect`），**不是这张卡自己那条技能动画**；而 `EC20/EC22/EC23/EC24/
                //     EC28/EC31/EC35` 等**没有 `trigger` 桶**的卡 ⇒ 这一拍**什么都不播**。
                //   ⚠️ 大小写：引擎发**小写**、候选是 PascalCase —— 白名单这一半靠方法头的
                //     `ToLowerInvariant()`、候选那一半靠 `NameIs` 的 `OrdinalIgnoreCase`，**两处都要**
                //     （`Codex` 当年就是只对了一半 = D6）⇒ 断言里那两条 `lo`/`up` 也覆盖这三个名字。
                //   ⚠️ **发射侧的闸仍然生效**：`FireTriggerAt` 的「没正文就不发信号」
                //     （`RuleCore.cs:10708`：`ops == null && spec == null && skipDraw == null && 附魔空` ⇒ return false）。
                //     这三个词的正文靠 `Cruelty:` / `Stimulation:` / `Ecstasy N:` 前缀、或裸写正文那条路收
                //     （三个词都在 `CardDef.RoutableTriggers` 与 `CardDef.BodyKeywords` 里，现读确认）。
                //     ⚠️ 正文**解析器本批没跑**（不跑 Unity）—— 见 `A27_VfxMap第三批.md` 的「没查清」。
                //   ⛔ **下面这两个名字【不许】跟着加**（`EvtKind.Trigger` 根本发不出来，加了 = 永远等不到）：
                //     `ThisUnitSummoned`(195) —— 引擎部署那一拍发的是 `rally`（`RuleCore.cs:3658/4300`）
                //     / 听者的 `OtherUnitSummoned`（`EffectResolver.cs:6147`）；
                //     `WhileInPlay`(20) —— 原版是**每次重算都重演**的常驻能力，引擎里**没有这一拍**。
                //     （🔴 **2026-10-24 `A30` 就地订正**：这一段原来还列着 `UnitReceivesHuntMark`(520) /
                //       `OnCollectFaith`(665) —— 那两条的映射**同批补上了**，见下面那一段 `case`。）
                //     —— 这两个都在 `NoEmitTriggerNames` 那条常驻反向闸里（`SelfCheckRoutedTriggerNames`）：
                //        要放行必须**先补发射口/映射**，再从那张表里删掉（删 = 「我核过」的签名）。
                case "cruelty":
                case "stimulation":
                case "ecstasy":
                    return true;
                // ---- 🆕 2026-10-24（`A30` · `C4` D7 第二批）：`UnitReceivesHuntMark`(520) / `OnCollectFaith`(665) ----
                //   这两个**不是引擎关键词**（`KeywordTable` 里没有、也不该新造），而是**监听事件那一拍**
                //   的原版 `AbilityTrigger` 枚举名。发射口 = `EffectResolver.EmitListenerTrigger`
                //   （`EmitCardTrigger` 把 `KeywordWithAbility(triggerName, -1)` 原样写进
                //   `BattleEvent.Keyword` ⇒ **大小写是 PascalCase**，不是小写常量）：
                //     · `UnitReceivesHuntMark`：`TriggerNameOfWhenKind(WhenEventKind.GetsHuntMark)`
                //       （`EffectResolver.cs` 的常量 + `case`，2026-10-24 `A30` 补）。
                //       判据 = `CardScript__ReactToUnitHuntMarked.c:7`（`OnTrigger(0x208)` = 520）；
                //       事件源 = `BroadcastKeywordEvent(GetsHuntMark)`（`RuleCore` 加 huntmark 时调，
                //       我们这侧在 `EffectResolver.cs` 的 `ApplyOneGain` 尾）。
                //       候选现读 **5 行 / 2 张**（`cards_engine.json`，判据 = `animTimes[].tn`）：
                //       `SW20` 2 行（`Buff_Blue_SW_Wolf` + `Buff_Red`）· `SW35` 3 行
                //       （`Axe_Slash_User_SW` + `Cut Axe SW` **×2** —— 同名两行，⛔ 别按名字去重）。
                //     · `OnCollectFaith`：`TriggerNameOfWhenKind(WhenEventKind.GainFaith)`（同批补）。
                //       判据 = `CardScript__ReactToCollectFaith.c:7`（`OnTrigger(0x299)` = 665）；
                //       事件源 = `BroadcastWhen(WhenEventKind.GainFaith, …)`（`EffectResolver.cs`）。
                //       候选现读 **2 行 / 1 张**（`SOR38 Paragon Warsuit`）：
                //       `Faith_trigger_unit`（`n` 名相同、`a` 两个不同）+ `BulletImpact_Sororitas_MultiMelta`。
                //   **不放行的后果（`C4` 判断 + 2026-10-24 `A30` 现核）**：那一拍 `target` 恒 null ⇒
                //     `TryResolveTriggerPicks` 走「④ 判不出 ⇒ 原样回 `trigger` 粗桶」⇒ 播的是 trait 状态件
                //     （`SW20` = `RallyEffect`、`SW35` = `ArmourEffect`、`SOR38` = `ArmourEffect`），**错件**；
                //     而这三张卡自己那条监听动画（`Buff_Blue_SW_Wolf` / `Cut Axe SW` / `MultiMelta`）**一件不播**。
                //   ⚠️ 与上面几个「引擎小写关键词」不同：这里靠 `ToLowerInvariant()` 归一后与 PascalCase
                //     候选对上（`NameIs` 是 `OrdinalIgnoreCase`）⇒ **两处都要**，⛔ 别写成 `"gethuntmark"` /
                //     `"gainfaith"`（那是 `WhenEventKind` 的键，不是事件里发的名字）。
                //   ⚠️ 表现层已知代价（如实记，⛔ 别当新缺陷）：`SOR38` 的 `Faith_trigger_unit` 同时挂在
                //     `OnCollectFaith` 与 `UseFaith` 下（`animTimes` 两行、`a` 不同），两拍会各按自己的
                //     `a` 播；若将来分类键改成「只按 `n`」，它会掉进 ambiguous 那一档（见 A27 §七·4）。
                case "unitreceiveshuntmark":
                case "oncollectfaith":
                // ---- 🆕 2026-10-24（引擎线 `G1b` · `C2` §2 · D8）：`OnCollectSpiritStone`(610) ----
                //   发射口 = `EffectResolver.TriggerNameOfWhenKind(WhenEventKind.GainSpirit)`
                //   （本批补的分支；事件源 = `RuleCore.CollectWaystoneCore` 的
                //   `BroadcastWhen(..., GainSpirit, ...)`，全仓唯一一处）。
                //   判据 = `CardScript__ReactToCollectSpiritStone.c:5-9` 的 `OnTrigger(0x262 = 610)`；
                //   广播器 = `BattleManagerSupport__BroadcastCollectSpiritStone.c:22-38`。
                //   候选现读 **5 行 / 4 张**（`ASH80` / `ASH28` / `ASH78` 2 行 / `ASH25`，全 SaimHann）。
                //   ⚠️ 大小写：`EmitCardTrigger` 发的是 **PascalCase 原版枚举名**
                //   ⇒ `MustRouteTriggerKeywords` 那一格也写 PascalCase（同 520/665）。
                case "oncollectspiritstone":
                    return true;
                // ---- 🆕 2026-10-24（引擎线 `G2` · `C4` D8）：`ThisUnitSummoned`(195) ----
                //   **只差白名单 + 一条映射分流**（两条本批同批上）：
                //     · 映射：`EffectResolver.TriggerNameOfWhenKind(kind, selfOnly)` —— `Deploy` 的
                //       `selfOnly == true` 那一支回 `ThisUnitSummoned`（改前恒回 `OtherUnitSummoned`）。
                //       判据 = `CardScript__ResolveUnitSummoned.c`：`op_Equality(param_2, param_1)`
                //       ⇒ `OnTrigger(195)`；`op_Inequality` ⇒ `OnTrigger(190)`。
                //       调用点 = `FireBoardWhenListener`（`selfOnlyOnly`，只由 `RuleCore` 两条部署入口的
                //       预调传 `true`：`RuleCore.cs` 的 `PlayCard` 与 `DeployFree` 各一处）
                //       → `EmitListenerTrigger(..., selfOnlyOnly)`。
                //     · 候选现读（`cards_engine.json`，2026-10-24 现算）：**1 张 / 1 行** ——
                //       `SW10 Grey Hunter`（`{a/n: HuntMark_User, tg: 195, tn: "ThisUnitSummoned"}`，
                //       有 prefab）。同一张卡也是全池**唯一** `abilityTriggers` 含 195 的。
                //   **不放行的后果（`A30` 实测）**：部署那一拍 `target` 恒 null ⇒ 走 ④「原样回
                //     `trigger` 粗桶」；而 `SW10` 的 `trigger` 桶里没有这件 ⇒ **什么都不播**。
                //   ⚠️ 与 `OtherUnitSummoned`(190) **不是同一个名字**：190 那一族（`AM7` / `UM12` …）
                //     是「**你部署了一个部队**」；本名字是「**它自己**被部署」。⛔ 别把两者合并。
                //   ⚠️ 大小写：发射体是 `EmitCardTrigger` ⇒ `Keyword = KeywordWithAbility(原版 trigger 名, -1)`
                //     ⇒ 生产路径发的是 **PascalCase**（与上面 520/665 同形）⇒
                //     `MustRouteTriggerKeywords` 那一格也写 PascalCase。
                case "thisunitsummoned":
                    return true;
                // ---- 🆕 2026-10-24（引擎线 `G2` · `C4` D9）：`WhileInPlay`(20) ----
                //   引擎口 = `RuleCore.EmitWhileInPlayPass`，**在动作队列排空时**调
                //   （`ActionPump.PumpActionQueues` 尾部；= 原版 `BattleManager__FinishResolvingAction.c:91`
                //   那一跳，闸 = `ActionsLeftInQueue() < 1`）。
                //   判据 = `CardScript__UpdateWhileInPlay.c:19-20` → `RawCardScript__UpdateWhileInPlay.c:14`
                //   的 `RawCardScript__TriggerAbilities(0x14 = 20)`（**在缓存闸之前** ⇒ 每次都发）。
                //   候选现读（`cards_engine.json`，2026-10-24 现算）：**1 张 / 2 行** —— `SW18 Hrolf the Ironhowl`
                //   （`Buff_Blue_SW` + `Card_Buff_Hand_SW`）。全池 13 张卡 `abilityTriggers` 含 20，
                //   但另外 12 张**没有这一拍的动画行** ⇒ 发射口按 `HasAnimTrigger` 收窄（见那个方法的 doc）。
                //   ⚠️ **同一个 `CardAnim` 挂两个 trigger**：`Card_Buff_Hand_SW` 同时挂在
                //   `WhileInPlay`(20) 与 `ThisCardPlayed`(0) 下（同一个 `a` 键、两个 `tn`）。
                //   🔴 **2026-10-24（`A2`）已收口**：分类键改成「一条候选可以有多个 trigger 名」
                //   （`AddTriggerNamesOf`）⇒ 这一行在 `WhileInPlay` 那一拍取得到，件级自检
                //   **逐条验它**（⛔ 原来的 `AmbiguousTriggerLabel` 跳过已删）。
                //   ⚠️ 大小写：`EmitCardTrigger` ⇒ `Keyword = KeywordWithAbility(原版 trigger 名, -1)`
                //   ⇒ 生产路径发 **PascalCase**（与 520/665/195 同形）。
                case "whileinplay":
                    return true;
                // ---- 🆕 2026-10-24（执行代理 `J1` · `C5` D11）：`Regiment`(301) ----
                //   **只差白名单一条**（引擎已经在发，2026-10-24 `J1` 现核）：
                //     · 发射口：`RuleCore.cs:6030` `FireTriggerAt(ctx, attacker, KeywordTable.Regiment, p, atkSlot)`
                //       —— 在**远程**攻击结算尾部，与 `Mob`（`:6008`，近战）**成对**、差别只在「远程」；
                //       keyword = `KeywordTable.Regiment` = **`"regiment"`**（`CardDef.cs:5197`）
                //       → `FireTriggerAt` 的 `emitSignal`（`RuleCore.cs` 的 `ctx.Emit(EvtKind.Trigger, …)`）。
                //     · 候选现读（`cards_engine.json`，判据 = `animTimes[].tn == "Regiment"`）
                //       **14 行 / 10 张，全部 AstraMilitarum**：`AM6/9/14/25/26/29/31/32/37/69`
                //       （`AM6`/`AM37`/`AM69` 各 2 行）。逐行都有 prefab（0 行空件）。
                //   **不放行的后果（`C5` D11 实测）**：那一拍 `target` 恒 null ⇒
                //     `TryResolveTriggerPicks` 走「④ 判不出 ⇒ 原样回 `trigger` 粗桶」⇒
                //     `AM25`/`AM32`/`AM37`（有 `trigger` 桶、里面只有 `ArmourEffect`）
                //     **每次远程攻击都播护甲起效件**（错件 + 多播）；其余 7 张没有 `trigger` 桶 ⇒ **什么都不播**。
                //   ⚠️ 两张卡在基础数值下永远走不到远程（`AM26` 4/4 · `AM31` 6/6，
                //     `ChooseAttackTypeAutomatically` 平手取近战，原版同算式）⇒ 放行不改变它们。
                //   ⚠️ 放行后**如实留白**（与 `Mob`/`Teleport` 同一取舍）：trait 件在 `CardDef.AnimTimes`
                //     里没有行 ⇒ 分类键（`AddTriggerNamesOf`）标不上名 ⇒ 被「未标名一律丢」丢掉 ⇒ 这一拍只播卡自己那条
                //     技能动画。补齐属于 D4 / `P7`。
                case "regiment":
                    return true;
                // ---- 🆕 2026-10-24（执行代理 `J1` · `C5` D12）：`TriggeredSynapse`(630) ----
                //   **事件已经在发、只差「名字 → 原版枚举名」那一条映射 + 白名单**（同批上）：
                //     · 映射：`EffectResolver.TriggerNameOfWhenKind` 的 `TriggersPrefix` 分支
                //       补 `case KeywordTable.Synapse: return "TriggeredSynapse";`。
                //       事件源 = `EffectResolver.cs:2270` `BroadcastKeywordEvent(ctx,
                //       WhenEventKind.Triggers(KeywordTable.Synapse), anchor)`（突触重复那一拍之后）；
                //       发射体 = `EmitListenerTrigger` → `EmitCardTrigger` ⇒ `BattleEvent.Keyword`
                //       = **PascalCase 的原版枚举名**（不是小写关键词）。
                //     · 候选现读（`cards_engine.json`）**1 行 / 1 张**：`TL18 Zoanthrope`
                //       （`Psychic_Lightning_Tyranid` / `Atk_SmiteLightning_Tyranid`，有 prefab）。
                //   **不放行的后果**：`TriggerNameOfWhenKind` 回 null ⇒ `EmitListenerTrigger`
                //     直接 return ⇒ `EvtKind.Trigger` **从不发** ⇒ `TL18` 的技能动画一条都不播。
                //   ⚠️ 补映射是安全的：`EmitCardTrigger` 的闸 `HasTriggerSource` 会先问
                //     「这张卡有没有这条动画行 / 这条 ability」—— 现读 `abilityTriggers` 含 630 的
                //     只有 `TL18`/`TL19`/`TL70`，其中**只有 `TL18` 有动画行** ⇒ 不会给别的卡凭空发事件
                //     （`TL19`/`TL70` 会走「有 ability、没动画行」那支：照发事件 + 出声，表现层无件）。
                case "triggeredsynapse":
                    return true;
                // ---- 🆕 2026-10-25（执行代理 `R` · 引擎线 `A3` 的表现侧）：`Talent`(46) 回合开始那一拍 ----
                //   **判据（五跳，逐句读；台账 → `资料/普查产出_1025/A_引擎线_执行报告.md` §A3）**：
                //     `CardScript.OnTurnStart.c:160`（`HasCurrentTrait(0x34d)`）→ `:167 OnTrigger(0x2e = 46)`
                //     → `RawCardScript.TriggerAbility.c:60 PlayTriggerTraitAnim` → `BattleAction 0x44`
                //     → `CardScript.ActivateTriggerTraitAnim` → `GetTraitFromAbilityTrigger(46)` =
                //     `DefinedTrait.talent(845)` → `BattleCardUI.DisplayTriggerAnim(845)`
                //     （原版 `Talent.asset` = `triggerType 3 / TalentTraitTrigger / TalentEffect`）。
                //   **引擎侧已落地**（同一份台账）：`RuleCore.SpawnTalents` 在两条生成分支**之前**
                //     发 `EmitCardTrigger(..., "Talent", "talent:turnStart")` ⇒ `BattleEvent.Keyword = "Talent"`
                //     （PascalCase，`EmitCardTrigger` 的原版枚举名口径）。
                //   **放行做什么**：让 `TryResolveTriggerPicks` 走**第 ③ 档**（按名字取件）而不是掉回
                //     `trigger` 粗桶（掉回去 = 把别的拍的卡级行/召唤件也带到天赋那一拍）。
                //   ⚠️ **候选现读 = 0 行**（`cards_engine.json` 的 `animTimes[].tn` 全池没有 `Talent`，
                //     2026-10-25 现算）⇒ 这一档**结构上取不出卡级行** —— 天赋这一拍的画面**全部**由链②
                //     `TraitFrames.DisplayTrigger("Talent")` 出（`talent` 那行就在 `trait_effect_bindings.json` 里）。
                //     ⇒ 它进 `TraitFrameOnlyTriggerNames`（件级自检对它的期望不是「逐卡取件」，而是
                //     「链② 里确实绑着 `TalentEffect`」），⛔ 别因为它「0 行」就把这条 `case` 删掉。
                case "talent":
                    return true;
                // ---- 🆕 2026-10-24（执行代理 `W5` · AN1 `R5`）：`Strike`(580) / `Slay`(120) ----
                //   这两个是 `trigger` 桶里「名字没放行 ⇒ 掉回整桶」那 **36 行 / 32 卡** 的全部
                //   （`资料/普查产出_1024c/data/an1_*_emit.tsv`，`emit == TRIGGER_BUCKET(归口待定)` 那 36 行；
                //    逐行把 `(cardanim, prefab)` 钉到 `cards_engine.json` 的 `animTimes[]` 上，`tn` 只有两个值）：
                //     · `Strike`(580) —— **21 行 / 19 张**：`BL15`(2) · `BL31` · `BL71` · `DA9` ·
                //       `GSC16` · `GSC27` · `GSC33`(2) · `GSC71` · `GOF62` · `TL3` · `TL16` · `TL17` · `TL30` ·
                //       `ASH13` · `ASH16` · `ASH69` · `SOR16` · `SW22` · `TAU18`（⛔ 别把这张表当断言 ——
                //       它只说明「36 行里 21 行是 Strike」；件级自检按卡池现算，现读 19 张 / 21 行）。
                //     · `Slay`(120) —— **15 行 / 13 张**：`AM71`(2) · `AM72` · `DA6` · `DA10` · `DA24` ·
                //       `DA26`(2) · `DA36` · `EC11` · `EC12` · `EC34` · `SW27` · `SW34` · `UM90`（现读）。
                //   **判据 = 两个名字都真的有发射口（2026-10-24 现核，`EvtKind.Trigger` + 关键词原样写进
                //   `BattleEvent.Keyword`）**：
                //     · `Slay`(120)：`RuleCore.cs:5603` `FireTriggerAt(ctx, killer, KeywordTable.Slay,
                //       kOwner, kSlot, requireActorInPlayOrDying: true)` —— `KillUnit` 那条链的**唯一发射口**
                //       （原版 `BattleManager__AddTriggerSlay.c:27` → `…__ResolveTriggerSlay.c:116-226`）。
                //       keyword = `KeywordTable.Slay` = **`"slay"`**（`RuleEngine/Core/CardDef.cs` 的
                //       `KeywordTable.Slay` 常量；⛔ 别记死行号 —— `CardDef.cs` 一直在改）。
                //     · `Strike`(580)：`RuleCore.cs:6096` `FireTriggerAt(ctx, attacker, KeywordTable.Strike,
                //       p, atkSlot)` —— 攻击结算尾部、**排在 `UnitAttack(50)` 之后**（原版
                //       `CardScript__ResolveUnitAttacked` 的 `OnTrigger(580)` 在 `:99-101`），近战/远程都发
                //       （与 `Mob` 的差别就在这道 `!ranged` 闸）。keyword = `KeywordTable.Strike` =
                //       **`"strike"`**（同文件的 `KeywordTable.Strike`）。
                //   **不放行的后果（`AN1` `R5` 现读）**：这一拍 `target` 恒 null ⇒ 走第 ④ 档「判不出 ⇒
                //     原样回 `trigger` 粗桶」⇒ 把**别的拍的件**也一起播了。现读两种：
                //     · **错件**：`Strike` 那一拍播 `TalentEffect` / `CodexEffect` / `CrueltyEffect` /
                //       `EcstasyEffect` / `ArtificeEffect` / `OrkBuffGreenWaaagh`（`regeneration`）这些**别的
                //       trait 的框**（现读 `Strike` / `Slay` 两拍各有几十张卡中招 —— 逐卡算，⛔ 别写死张数）；
                //       `Slay` 那一拍播 `StrikeEffect`（`ASH69`/`ASH13`/`ASH16`/…）；
                //     · **多播**：`Strike` 那一拍播 `StrikeEffect`（本拍 `TraitFrames.DisplayTrigger("Strike")`
                //       **也**播同一件 ⇒ 两个锚点各一遍）—— 🔴 **2026-10-25（执行代理 `R` · `R8-b`）**：
                //       这一半和「别的 trait 的框」现在是**同一条闸**收口的 —— `TriggerBeatRows` 一律
                //       挡掉 trait 行（`IsTraitFrameRow`）；`IsTraitFrameDuplicate` 只剩诊断计数。
                //   ⚠️ 大小写：引擎发**小写**（`"strike"` / `"slay"`）、候选是 PascalCase（`animTimes[].tn`）——
                //     白名单这一半靠方法头的 `ToLowerInvariant()`、候选那一半靠 `NameIs` 的
                //     `OrdinalIgnoreCase`，**两处都要**（`Codex` 当年就是只对了一半 = `C3` D6）。
                //   ⚠️ 放行后**如实留白**（与 `Mob`/`Teleport`/`Regiment` 同一取舍）：trait 件在
                //     `CardDef.AnimTimes` 里没有行 ⇒ 分类键（`AddTriggerNamesOf`）标不上名 ⇒ 第 ③ 档的
                //     `CandidateHasTrigger` 把它们丢掉 ⇒ 这一拍只播**卡自己那几条**技能动画；
                //     trait 框由 `TraitFrames._triggerBook` 播（判据 = 原版 `ActivateTriggerTraitAnim`
                //     **只**调 `DisplayTriggerAnim` 一次）。补齐 trait 那一侧属于 `D4`/`P7`。
                case "strike":
                case "slay":
                    return true;
            }
            return false;
        }

        /// <summary>🆕 2026-10-23（特效线「新账-86」）：**引擎关键词 → 原版 trigger 名**的别名归一。
        ///
        /// <para>目前只有一条：**`penitence` → `Rage`**（判据三条 → <see cref="IsRoutedTriggerName"/>
        /// 里 `case "Rage"` 那一整段注释）。为什么必须有它：`penitence` 是显示名、`Rage` 是原版
        /// `AbilityTrigger` 枚举名，`NameIs` 是**逐字比较**（大小写不敏感）⇒ 不归一就永远筛不出候选。</para>
        ///
        /// <para>🆕 **2026-10-24（执行代理 `B1` · `C2` §1·5 步 B · 战斗线 `D3`）：第二条别名
        /// `attacked` → <see cref="UnitAttackTriggerName"/>**。判据：原版那一族（`EC1`/`EC2`/`SW26`/`TL11`）
        /// 触发的**就是** `UnitAttack(50)` 那一步（见 <see cref="AttackedKeyword"/> 的 doc）——
        /// 不归一的话它 `IsRoutedTriggerName` 判不中 ⇒ 掉回 `trigger` 粗桶（错件/多播），
        /// 而它的**动画**（`unit_attack_anim` 桶）永远不会被发现。
        /// ⚠️ **只此一处**：⛔ 别把 `"attacked"` 塞进 `IsRoutedTriggerName` / `MustRouteTriggerNames`
        /// —— 它不是原版枚举名，白名单里放的是枚举名那一档。</para>
        ///
        /// <para>🔑 **判据收口在这里**：调用点只认本方法的返回值，⛔ 别在别处再写一份映射。
        /// 大小写口径与 <see cref="IsRoutedTriggerName"/> 开头两条一致（`OrdinalIgnoreCase`）——
        /// 引擎发的是小写常量、原版名是 PascalCase。空输入原样回空（调用方先判空）。</para>
        /// <para>⚠️ **2026-10-24（`B1`）**：可见性从 `static` 提到 `internal` ——
        /// `BattleDriver.PlaySignal` 要按它分流（D3），⛔ 别在驱动里再写一份别名表。</para></summary>
        internal static string CanonicalTriggerName(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) return null;
            if (string.Equals(triggerName, KeywordTable.Penitence, System.StringComparison.OrdinalIgnoreCase))
                return "Rage";          // `DefinedTrait.rage = 280` 的枚举名（`AbilityTrigger.Rage = 130`）
            // 🆕 2026-10-24（`B1` · D3）：`attacked` 是 `UnitAttack(50)` 那一步的**另一个拨出点**
            //   （`AttackedOps` 族），不是另一个 trigger。
            if (string.Equals(triggerName, AttackedKeyword, System.StringComparison.OrdinalIgnoreCase))
                return UnitAttackTriggerName;
            return triggerName;
        }

        /// <summary>🆕 2026-10-25（执行代理 `R` · AN1 **`R8-b`**；**取代** 2026-10-24 `A2`/`D4` 那版
        /// `IsNonTriggerBeatTraitRow`）：**这条候选是不是「trait 框」**（数据侧 `src: cardtrait-so` 那批）。
        ///
        /// <para>**判据（第一权威，逐句读）**：原版触发那一拍是**两件互不替代**的东西 ——
        ///   ① `RawCardScript__TriggerAbility.c:85-110` → `AbilityLogic__PlayAnim`：**只播触发卡自己的**
        ///      `cardAnimations[]`（= 我们的「卡级行」，本类里 `TriggerTypeOf` 回 `-1`），**不碰 trait**；
        ///   ② `RawCardScript__TriggerAbility.c:58-62` → `BattleManager__PlayTriggerTraitAnim.c:33` →
        ///      `CardScript__ActivateTriggerTraitAnim.c:6-20`：按 `GetTraitFromAbilityTrigger(triggerId)`
        ///      把**那一张 trait 框**播**一次**（`HighlightTraitIcon` + `DisplayTriggerAnim`，
        ///      后者 `BattleCardUI__DisplayTriggerAnim.c:75-82` **只播该 trait 表里 `triggerType ∈ {3,6}`
        ///      的 TraitEffect**）。</para>
        ///
        /// <para>⇒ **trait 框只归链②**（我们的 `TraitFrames.DisplayTrigger`，吃 `trait_effect_bindings.json`
        /// 里 3/6 那批行）。`VfxMap` 的 `trigger` 桶**在任何一档都不许**再播 trait 行 —— 播了就是
        /// （a）同一件被两条链各实例化一次（`R8`），或（b）**别的 trait 的框出现在错的一拍**（`R8-b`，
        /// 例：`GOF26` + `unstable` 那一拍的 `ConcussiveEffect` 属 `concussive(930)` 的 `Trigger(3)`，
        /// 与 trigger 81 无关）。判据 → `资料/普查产出_1024d/W5_特效VfxMap_执行报告.md:148`。</para>
        ///
        /// <para>⚠️ **运行期没有 `src` 这一列**（`card_vfx_by_card.json` 只有 `n`/`a`/`c`）⇒ 这里先用
        /// `TraitFrames.TriggerTypeOf`（反查 `trait_effect_bindings.json`，**它就是原版 `CardTrait` SO**）
        /// 判，再对**名字撞车**的行做一次自证：若这条 (CardAnim, prefab) 在**这张卡自己的** `animTimes[]`
        /// 里标得出 trigger 名（<see cref="AddTriggerNamesOf"/>），那它是**卡级行**（原版
        /// `AbilityLogic.PlayAnim` 播的那一类），⛔ 不算 trait。现读这种撞车 **39 行**
        /// （`UM84/85/86` 的 `CodexEffect`、`GSC32` 的 `ArmourEffect`、`AM17` 的 `ArmourEffect` …）——
        /// 按名字一刀切会把它们从第 ④ 档误删。</para>
        ///
        /// <para>⚠️ **第 ② 档（`times` 为空）结构上做不到这次自证** —— 那时只能按名字判。
        /// 2026-10-25 现算：所有「没有 `animTimes`」的卡，`trigger` 桶里**一条卡级行都没有**
        /// （0 条）⇒ 这一档按名字判不会误伤；真出现那种形状时**查不到更精确的判据**（如实记）。</para></summary>
        static bool IsTraitFrameRow(CardPick p, IReadOnlyList<CardDef.CardAnimTime> times)
        {
            if (TraitFrames.TriggerTypeOf(p.A, p.N) < 0) return false;   // 名字不在 trait 表里 ⇒ 卡级行
            if (times == null || times.Count == 0) return true;          // 没逐候选时间 ⇒ 只能按名字判
            var labels = new List<string>();
            AddTriggerNamesOf(labels, times, p);
            return labels.Count == 0;      // 标得出名 ⇒ 它是「名字撞车」的卡级行，⛔ 不是 trait
        }

        /// <summary>🆕 2026-10-24（执行代理 `W5` · AN1 **`R8`**）：**这一条候选是不是「本拍由 `TraitFrames` 播的那一件」**。
        ///
        /// <para>**为什么要有它（缺陷，两条链各播一次）**：
        ///   · 链①（`VfxMap`）：`trigger` 桶掉回**整桶**那两档（第 ② 档 = 这张卡没有 `animTimes`；
        ///     第 ④ 档 = 关键词不在白名单）⇒ **旧版**桶里的 trait 行**照播**（那时 `TriggerBeatRows`
        ///     只挡 0/1/2/5）。2026-10-25 起**所有档一律挡掉全部 trait 行**（见 <see cref="IsTraitFrameRow"/>）。
        ///     触发点 = `BattleDriver.PlaySignal` 的
        ///     `EvtKind.Trigger` 支 → `VfxMap.ResolveAllTimed(Trigger, …)` → `FireEventAll`
        ///     （调用点是 `BattleDriver.PlayFeel` 里 `evt == null && e.Kind == EvtKind.Trigger` 那条
        ///     `perCardOnly = true` 的支 —— ⛔ 别记死行号，`BattleDriver.cs` 一直在改）。
        ///   · 链②（`TraitFrames`）：同一条事件还会走 `BattleDriver.PlayTraitPulse`
        ///     （`BattleDriver.PlayTraitPulse`；调用点 = 同一文件里 `case EvtKind.Trigger:` 那一行 ——
        ///     现读 `:21330`，⛔ 别把行号当判据）
        ///     → `TraitFrames.DisplayTrigger(keyword)`（`Core/TraitFrames.cs:869`）
        ///     → `_triggerBook`（= `trait_effect_bindings.json` 里 `TriggerType ∈ {3,6}` 的行）⇒ **同一件**。
        ///   ⇒ 同一件**实例化两次**，而且锚点还不一样（链① 落 `FireEventAll` 的目标点、链② 落卡的
        ///     `effectsAnchor`）—— 原版**只播一次**：`CardScript__ActivateTriggerTraitAnim.c` **只**调
        ///     `BattleCardUI__DisplayTriggerAnim`（就是链②）；`AbilityLogic.PlayAnim` 播的是**卡自己的**
        ///     `cardAnimations[]`，**不含 trait frame**。</para>
        ///
        /// <para>**怎么判（判据 = 数据本身，⛔ 不按名字猜）**：把这条候选的 `A`（`CardAnim`）/
        /// `N`（prefab）与「本拍 trait 的那几条真值行」逐字比 —— 真值行 = `TraitFrames.TriggerBindingsFor(keyword)`
        /// （它内部 `Norm` 成 trait 名；**只收 3/6**，正是 `DisplayTrigger` 会播的那一批）。
        /// ⚠️ **连 `Overrides` 一起比**：`prefab` 是**按阵营换过**的（`gen_card_vfx_by_card.py` 生成期就把
        /// owner 阵营的 override 套上了），只比默认那格会漏掉 `shield` / `invulnerable` 那 10 条覆盖。
        /// ⚠️ **`keyword` 为空时只影响「计数分类」，不影响过滤**（2026-10-25 起 trait 行一律挡掉）：
        /// 那时链② 拿不到名字 ⇒ 这一拍确实一件 trait 框都不播 —— 生产路径的 `EvtKind.Trigger`
        /// 一直带着关键词（`BattleDriver.PlaySignal`），走到这里说明**上游丢了关键词**，如实留白，⛔ 不拿别的行充数。
        /// 同理「推断出来的名字」不算 —— 这里只认**事件自己的关键词**。</para>
        ///
        /// <para>🔴 **2026-10-25（执行代理 `R` · AN1 `R8-b`）就地改口径**：原来这段写「它只挡**同一件**，
        /// 不挡别的 trait 的框 —— 第 ④ 档里剩下的那批**仍然会播**」。**现在不成立了**：
        /// `TriggerBeatRows` 起**一律排除所有 trait 行**（<see cref="IsTraitFrameRow"/>，判据 = 原版
        /// `ActivateTriggerTraitAnim` 只调 `DisplayTriggerAnim` 一次），本方法因此**只剩一个用途** ——
        /// 给诊断计数 <see cref="TraitFrameDuplicates"/> 分类：这一条被挡掉的 trait 行，本拍链②
        /// **会不会播同一件**。⛔ 别把它当成「过滤条件」再收窄（`R8-b` 就是那么漏出来的）。</para>
        ///
        /// <para>⚠️ **它按名字比，分不出「卡自己那条同名动画」**：若某张卡的 `cardAnimations[]` 里
        /// **本来就有**一条标着这个 trait 名的动画、prefab 又正好等于该 trait 的框，原版会**播两次**
        /// （卡动画 + trait 框）—— 这一类现在**不归本方法管**：`IsTraitFrameRow` 会先用
        /// `AddTriggerNamesOf` 把「标得出 trigger 名」的卡级行排除在 trait 之外。现读 39 行是这种撞车形状。</para>
        ///
        /// <para>🧨 **灭自证**：把本闸去掉 ⇒ `AnimFXCheck` 的 `R8` 那组逐卡断言的**计数那一半**
        /// （`witness > 0`）会红（现读 36 个（卡, 关键词）对：`codex` 9 · `strike` 9 ·
        /// `artifice` 7 · `backlash` 6 · `mob` 3 · `cruelty` 1 · `ecstasy` 1）。</para></summary>
        static bool IsTraitFrameDuplicate(CardPick p, string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false;
            var rows = TraitFrames.TriggerBindingsFor(keyword);
            if (rows == null || rows.Length == 0) return false;
            for (int i = 0; i < rows.Length; i++)
            {
                if (MatchesTraitEffect(p, rows[i].CardAnim, rows[i].Prefab)) return true;
                var ov = rows[i].Overrides;
                if (ov == null) continue;
                for (int k = 0; k < ov.Length; k++)
                    if (MatchesTraitEffect(p, ov[k].CardAnim, ov[k].Prefab)) return true;
            }
            return false;
        }

        /// <summary><see cref="IsTraitFrameDuplicate"/> 的逐个比较：`CardAnim` 优先、prefab 兜底
        /// （与 `TriggerTypeOf` / `AddTriggerNamesOf` 同一套「有 `A` 就只认 `A`、没有才看 `N`」口径）。</summary>
        static bool MatchesTraitEffect(CardPick p, string anim, string prefab)
        {
            if (!string.IsNullOrEmpty(p.A) && !string.IsNullOrEmpty(anim)
                && string.Equals(p.A, anim, System.StringComparison.Ordinal)) return true;
            if (!string.IsNullOrEmpty(p.N) && !string.IsNullOrEmpty(prefab)
                && string.Equals(p.N, prefab, System.StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>`R8` 的诊断口：整桶那一侧**主动让给 `TraitFrames`** 的件数（只增不减，自检按增量读）。
        /// ⛔ 它不是「播了多少件」的计数 —— 是「**没播**了多少件（因为另一条链会播）」。</summary>
        public static int TraitFrameDuplicates { get; private set; }

        /// <summary>🆕 2026-10-25（执行代理 `R` · AN1 **`R8-b`**）：整桶那一侧挡掉的**「本拍链② 也不会播」
        /// 的 trait 行**数（只增不减，自检按增量读）。
        /// <para>它 = `R8-b` 那个缺陷的**判别计数**：`GOF26` + `unstable` 那一拍的 `ConcussiveEffect`
        /// （属 `concussive` 的 `Trigger(3)`，与本拍无关）就记在这里。⛔ 它不是「播了多少件」——
        /// 是「**本来就不该播**、现在被挡掉了多少件」。灭自证：删掉 `IsTraitFrameRow` 那道闸 ⇒ 增量 0。</para></summary>
        public static int TraitFramesOnOtherBeat { get; private set; }

        /// <summary>`D4` / `R8-b`：把桶里的 trait 行滤掉（产出**新 list**，⛔ 别原地改表）。
        /// 全被滤掉 ⇒ 回空 list（调用方据此「这一拍什么都不播」，⛔ 不再掉回整桶）。
        /// <para>🔴 **2026-10-25（执行代理 `R` · AN1 `R8-b`）**：口径收成「**所有档一律排除 trait 行**」——
        /// 判据 = 原版 `CardScript__ActivateTriggerTraitAnim.c` **只**调 `CardBattleUI.DisplayTriggerAnim`
        /// 一次（trait 框归链②），`AbilityLogic.PlayAnim` 播的是卡自己的 `cardAnimations[]`。
        /// ⛔ 别再按 `TriggerType` 挑（旧版只挡 0/1/2/5、放行 3/6，于是别的 trait 的框会混进这一拍）。</para>
        /// <para>分类与计数：<see cref="IsTraitFrameRow"/>（含「名字撞车的卡级行」自证）+
        /// <see cref="IsTraitFrameDuplicate"/>（本拍链② 会不会播同一件 ⇒ <see cref="TraitFrameDuplicates"/>；
        /// 否则记 <see cref="TraitFramesOnOtherBeat"/>）。</para>
        /// <para>⚠️ <paramref name="keyword"/> 传**事件自己的关键词**（`CanonicalTriggerName` 归一后）；
        /// 传 `null` = 链② 拿不到名字（自检直调 / 旧事件）⇒ 只影响计数分类，**不影响过滤**。</para></summary>
        /// <param name="times">这张卡的 `CardDef.AnimTimes`（判「名字撞车的卡级行」用；`null` = 按名字判）。</param>
        static List<CardPick> TriggerBeatRows(List<CardPick> bucket, string keyword,
                                              IReadOnlyList<CardDef.CardAnimTime> times = null)
        {
            if (bucket == null || bucket.Count == 0) return bucket;
            var kept = new List<CardPick>(bucket.Count);
            for (int i = 0; i < bucket.Count; i++)
            {
                var p = bucket[i];
                if (!IsTraitFrameRow(p, times)) { kept.Add(p); continue; }
                if (IsTraitFrameDuplicate(p, keyword)) TraitFrameDuplicates++;
                else TraitFramesOnOtherBeat++;
            }
            return kept;
        }

        /// <summary>🆕 2026-10-24（第七十三会话 · 执行代理 `W6`）：<see cref="TriggerBeatRows"/> 之上再加一层 ——
        /// **判不出目标 trigger 时（第 ④ 支），卡级行还要自己那条 `CardAnimTime.TriggerName` 对得上这一拍**。
        ///
        /// <para>**为什么需要这一层**（判据 → `资料/普查产出_1024c/W2_生成器桶归类与数据列.md` §4·2）：
        /// 生成器从 2026-10-24 起把「引擎真会发」的那批触发动画从 `ability` 桶挪回 **`trigger`** 桶
        /// （`W2` 的三级归口）⇒ `trigger` 桶里的**卡级行**（`TraitFrames.TriggerTypeOf == -1`，
        /// <see cref="TriggerBeatRows"/> 会**原样保留**它们）从 **36 行涨到 410 行**（运行期表现读）。
        /// 第 ④ 支是「判不出目标名字 ⇒ 回 `trigger` 粗桶」的兜底路 —— 照旧整批播 = **把别的 trigger
        /// 的动画也都在这一拍放一遍**：`SOR8` 的 `PenitenceEffect`（原版 `Rage`）会在
        /// `keyword="Flank"` 那一拍播出来；`GSC16`（`Ambush` + `Strike` 两种都有）在**关键词为空**
        /// 那一档会把 `Ambush` 两件一起播出来（现读实据，断言 `T21·G`）。</para>
        ///
        /// <para>**判据 = 卡级行自己那条 `CardAnimTime.TriggerName`**（<see cref="AddTriggerNamesOf"/>，
        /// 与第 ③ 支**同一个钉法**，⛔ 别在别处再写一遍 A/N 匹配）：
        ///   · **标不上名**（`names` 为空）⇒ **保留** —— 它证明不了自己属于别的 trigger
        ///     （原版的卡 SO 顶层字段 / 我们自己的推断行就是这一档；今天卡级行里是 0 条，但别把规则写成「没名字就丢」）；
        ///   · 标了名、且其中**有** <paramref name="gateName"/> ⇒ 保留（`NameIs`，大小写不敏感）；
        ///   · 标了名、但一个都不是 ⇒ **丢**，并**出声一次**（<see cref="NoteTriggerGateDropped"/>，按卡去重）。
        /// ⚠️ <paramref name="gateName"/> 为 `null`（关键词为空、且保守推断也没推出唯一名字）⇒ **只留没标名的**
        /// —— 那时我们连这一拍叫什么都说不出，⛔ 不拿「随便哪一行」充数。</para>
        ///
        /// <para>⛔ **只管卡级行**：trait 行在**上游** <see cref="TriggerBeatRows"/> 就已经全被滤掉
        /// （🔴 **2026-10-25（执行代理 `R` · `R8-b`）**：旧口径只挡 `Start(0)`/`OnPlay(2)`、
        /// 放行 `Trigger(3)`/`TriggerOnPlay(6)`，于是**别的 trait 的框**会混进这一拍 —— 那是缺陷，不是设计）。
        /// 判据 = 原版 `ActivateTriggerTraitAnim` 只调 `DisplayTriggerAnim` 一次 ⇒ trait 框归链②。
        /// ⚠️ **`DA10`** 那条「别切过头」的正例靠的是另一件事：它的 `Trait_GainQuest` 是**卡级行**
        /// （`TriggerTypeOf` 回 `-1`，`animTimes` 里标着 `Slay`）⇒ 照旧保留。⛔ 别在这里再判一次 trait 行。</para>
        ///
        /// <para>🧨 **灭自证**（两处一起红）：删掉这一层 ⇒ `AnimFXCheck` ⑤ 的 `flank8`
        /// （`SOR8` + `keyword="Flank"`）由 0 件变 **2 件**（`PenitenceEffect` / `Arco-Flagellant Strike`）；
        /// `BattleScene` 的 `T21·G` 灭自证（`Acolyte Heavy`）由「不含 `Buff_GSC`」变「含」。</para>
        ///
        /// <para>⚠️ **与第 ② 支不同**：`times` 为空时**标不上名** ⇒ 第 ② 支照旧只走
        /// <see cref="TriggerBeatRows"/>（结构上做不到，`W2` §5·2 已如实记）。</para></summary>
        /// <param name="gateName">本拍的 trigger 名（判卡级行用；可以是保守推断出来的名字）。</param>
        /// <param name="chainName">**事件自己的关键词**（`CanonicalTriggerName` 归一后；判 `R8` 那条
        /// 「本拍由 `TraitFrames` 播」用）。⚠️ 与 <paramref name="gateName"/> **不是一回事**：
        /// 后者可以来自推断，而链② 只认事件里的关键词 ⇒ 推断出来的名字**不能**用来挡（否则两件都不播）。</param>
        static List<CardPick> TriggerBeatRowsMatching(List<CardPick> bucket,
                                                     IReadOnlyList<CardDef.CardAnimTime> times,
                                                     string gateName, string chainName, string cardKey)
        {
            var kept = TriggerBeatRows(bucket, chainName, times);
            if (kept == null || kept.Count == 0) return kept;
            if (times == null || times.Count == 0) return kept;   // 没有逐候选时间 ⇒ 标不上名 ⇒ 全保留
            var names = new List<string>();
            var outp = new List<CardPick>(kept.Count);
            int dropped = 0;
            string droppedNames = null;
            for (int i = 0; i < kept.Count; i++)
            {
                var p = kept[i];
                // 🔴 **2026-10-25（执行代理 `R` · `R8-b`）**：上游 `TriggerBeatRows` 已经把 trait 行全滤掉了
                //   ⇒ 走到这里的**全是卡级行**（含「名字撞车的卡级行」，它也是原版 `AbilityLogic.PlayAnim`
                //   会播的那一类）⇒ 一律按**本拍名字**筛，⛔ 别再给 `TriggerTypeOf >= 0` 开后门
                //   （那会让 `GSC32` 的 `ArmourEffect` 在 `Strike` 那一拍混进来）。
                names.Clear();
                AddTriggerNamesOf(names, times, p);
                bool keep = names.Count == 0;                       // 没标名 ⇒ 判不出 ⇒ 保留
                if (!keep && gateName != null)
                    for (int k = 0; k < names.Count; k++)
                        if (NameIs(names[k], gateName)) { keep = true; break; }
                if (keep) { outp.Add(p); continue; }
                dropped++;
                droppedNames = (droppedNames == null ? "" : droppedNames + " / ") + p.N;
            }
            if (dropped > 0) NoteTriggerGateDropped(cardKey, gateName, dropped, droppedNames);
            return outp;
        }

        /// <summary>🆕 2026-10-24（`W6`）：<see cref="TriggerBeatRowsMatching"/> 挡掉的卡级行计数
        /// （诊断口 —— 证明这条闸**不是没人验的装饰**；⛔ 它不是自检断言，别拿它当「播了多少件」）。</summary>
        public static int TriggerBeatGateDrops { get; private set; }

        static readonly HashSet<string> _warnedGateDrop = new HashSet<string>();
        const int GateDropWarnCap = 16;

        /// <summary>🆕 2026-10-24（`W6`）：**临时闭麦** —— `SelfCheckTriggerBeatSplit` 会**故意**拿
        /// 一个编出来的关键词把每张卡的第 ④ 支跑一遍（那不是生产），不闭麦就会在 `BattleScene.Run`
        /// 里刷出十几条看着像缺陷的告警。⛔ 只闭「出声」，计数（<see cref="TriggerBeatGateDrops"/>）照旧累加。</summary>
        static bool _quietGateWarn;

        /// <summary>「第 ④ 支挡掉了标着别的 trigger 的卡级行」—— 每种 (卡, 本拍名字) 只说一次（上限
        /// <see cref="GateDropWarnCap"/>）：「⛔ 不许静默」在这里的具体做法。
        /// ⚠️ 这条告警同时是**缺 alias 的信号**：若关键词与数据里的 trigger 名差一个写法
        /// （先例：`penitence` ↔ `Rage`、`attacked` ↔ `UnitAttack`），就会走到这里 ——
        /// 补 <see cref="CanonicalTriggerName"/>，⛔ 别把闸放宽。</summary>
        static void NoteTriggerGateDropped(string cardKey, string gateName, int dropped, string names)
        {
            TriggerBeatGateDrops += dropped;
            if (_quietGateWarn) return;                              // 自检故意跑第 ④ 支 ⇒ 别刷屏
            if (_warnedGateDrop.Count >= GateDropWarnCap) return;
            if (!_warnedGateDrop.Add(cardKey + "|" + (gateName ?? ""))) return;
            Debug.LogWarning($"[Vfx] `{cardKey}` 触发那一拍：`{dropped}` 条**卡级行标着别的 trigger** ⇒ 不播"
                           + $"（`{names}`；本拍的名字 = `{gateName ?? "（判不出）"}`）。"
                           + "若这是「别名没归一」（`CanonicalTriggerName`）⇒ 补别名；"
                           + $"⛔ 别放宽这条闸（上限 {GateDropWarnCap} 条，其余只计数 `VfxMap.TriggerBeatGateDrops`）。");
        }

        /// <summary>🆕 2026-10-19（W · `T21`）：一条候选**自己那条** `CardAnimTime` 的原版 trigger 名
        /// （`CardAnimTime.TriggerName`，如 `Ambush` / `TriggerEnchantment` / `Strike`）。
        ///
        /// <para>钉法同 <see cref="ResolveAllTimed"/>：**先 `A`**（CardAnim 精确 join 键）、**后 `N`**
        /// （prefab 名；`A` 为空的那几条来自卡 SO 顶层字段 / 我们自己的推断档）。
        /// ⚠️ **它是分类器、不是计时器** —— 只回名字，计时仍由 <see cref="FindAnimTimeIndex"/> 的游标做
        /// （同名重复按顺序逐次消费那一条规则只属于计时）。</para>
        /// <para>钉不上 ⇒ `null`（= 这条候选不是来自 `cardAnimations[]`）。</para>
        ///
        /// <para>🔴 **2026-10-24（执行代理 `A2` · 战斗线 `UM37`/`UM84`）就地改口径（铁律 5）**：
        /// 原来这里**只取第一条**同名行 —— 而同一个 `CardAnim` 可以在**两条不同的 `CardAbility`** 里
        /// 各出现一次、`TriggerName` 不同（现读 3 行：`UM37` 的
        /// `BulletTrail_Stormraven_SingleTarget`（`ThisCardPlayed` 0.0.2 ↔ `Codex` 1.0.0）·
        /// `UM84` 的 `Buff_Purple_target`（`ThisCardPlayed` 0.0.1 ↔ `Codex` 1.0.1）·
        /// `SW18` 的 `Card_Buff_Hand_SW`（`WhileInPlay` 0.0.1 ↔ `ThisCardPlayed` 1.0.0））。
        /// 「只取第一条」⇒ 第二条那个 trigger **结构上取不到**（`UM37` / `UM84` 的 `Codex` 那一拍
        /// 一直播不出来）。⇒ 现在返回**这一条候选可能属于的【全部】 trigger 名**（去重），
        /// 归口时「命中任意一个即算」；`Order` 第一段（= 原版 `cardAbilities` 下标）仍然是它俩
        /// 唯一的区分依据，**在 `times` 里按该键出现的次数逐个对齐**（见 `TrimToLabelledRowCount`）。
        /// ⛔ 别改回「只取第一条」—— 那会让这两张卡的 Codex 拍静默消失。</para></summary>
        static void AddTriggerNamesOf(List<string> names, IReadOnlyList<CardDef.CardAnimTime> times, CardPick p)
        {
            if (times == null || times.Count == 0) return;
            // `A` 非空 ⇒ **只认 `A`**（它是 `cardAnimations[]` 的精确 join 键）。
            // ⚠️ **不许在 `A` 查不到时退回 `N`**：`A` 有值但 `times` 里没有 = 这条候选来自
            //    别的数据源（典型 = 我们自己的 `inferred-keyword` 行）。退回按 prefab 名匹配会把它
            //    错标成同名的 `Ambush` 候选（`GSC32` 的 `ArmourEffect` 两边同名）⇒ **多播**。
            if (!string.IsNullOrEmpty(p.A))
            {
                for (int i = 0; i < times.Count; i++)
                    if (times[i] != null && times[i].Anim == p.A) AddTriggerName(names, times[i].TriggerName);
                return;
            }
            if (!string.IsNullOrEmpty(p.N))
                for (int i = 0; i < times.Count; i++)
                    if (times[i] != null && times[i].Prefab == p.N) AddTriggerName(names, times[i].TriggerName);
        }

        /// <summary><see cref="AddTriggerNamesOf"/> 的判定版：这条候选**有没有**标到 <paramref name="target"/>。
        /// （与 `AddTriggerNamesOf` 同一套 `A`/`N` 钉法 —— ⛔ 别在别处再写一遍。）</summary>
        static bool CandidateHasTrigger(IReadOnlyList<CardDef.CardAnimTime> times, CardPick p, string target)
        {
            if (times == null || times.Count == 0 || string.IsNullOrEmpty(target)) return false;
            if (!string.IsNullOrEmpty(p.A))
            {
                for (int i = 0; i < times.Count; i++)
                    if (times[i] != null && times[i].Anim == p.A && NameIs(times[i].TriggerName, target)) return true;
                return false;
            }
            if (!string.IsNullOrEmpty(p.N))
                for (int i = 0; i < times.Count; i++)
                    if (times[i] != null && times[i].Prefab == p.N && NameIs(times[i].TriggerName, target)) return true;
            return false;
        }

        /// <summary>🆕 2026-10-24（`A2` · `UM37`/`UM84`）：**同一个 `CardAnim` 在多条 `CardAbility` 里各出现一次**
        /// 时，`times` 侧标到<paramref name="target"/>的行数就是这一拍**应该播的件数**；
        /// 候选侧因为分不出是哪一条 occurrence，可能多收 ⇒ 按「先来先得」截到应有条数。
        ///
        /// <para>⚠️ **现读这条分支一次都不触发**（3 行歧义里，另一条 occurrence 都落在别的桶：
        /// `UM37`/`UM84`/`SW18` 的第二条都在 `play` 桶 ⇒ 归口时只看 `trigger`+`ability` ⇒ 候选数 ≤ 行数）。
        /// 留着它是防**将来**出现「同一个 `CardAnim` 同时落在 `trigger` 与 `ability` 两张桶」的形状 ——
        /// 那时两件同 prefab，多播一次**画面一样**、只是多播一遍 ⇒ 截断并出声。</para></summary>
        static List<CardPick> TrimToLabelledRowCount(List<CardPick> merged,
                                                     IReadOnlyList<CardDef.CardAnimTime> times,
                                                     string target, string cardKey)
        {
            if (merged == null || merged.Count <= 1 || times == null || times.Count == 0) return merged;
            var seen = new Dictionary<string, int>(System.StringComparer.Ordinal);
            var trimmed = new List<CardPick>(merged.Count);
            bool cut = false;
            string cutKey = null;
            for (int i = 0; i < merged.Count; i++)
            {
                var p = merged[i];
                string key = !string.IsNullOrEmpty(p.A) ? p.A : p.N;
                int want = 0;
                if (!string.IsNullOrEmpty(key))
                {
                    for (int t = 0; t < times.Count; t++)
                    {
                        var at = times[t];
                        if (at == null || !NameIs(at.TriggerName, target)) continue;
                        string k = !string.IsNullOrEmpty(at.Anim) ? at.Anim : at.Prefab;
                        if (string.Equals(k, key, System.StringComparison.Ordinal)) want++;
                    }
                }
                int got;
                seen.TryGetValue(key ?? "", out got);
                if (want > 0 && got >= want) { cut = true; cutKey = key; continue; }
                seen[key ?? ""] = got + 1;
                trimmed.Add(p);
            }
            if (cut)
                Debug.LogWarning($"[Vfx] `{cardKey}` 的 `{target}` 那一拍里 `{cutKey}` 的候选**多于**"
                               + "`animTimes` 标到这一拍的条数（同一个 `CardAnim` 落在两张桶里？）"
                               + " ⇒ **截到应有条数**（同一 prefab，多播只是多放一遍）。");
            return cut ? trimmed : merged;
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
        ///    **回 `trigger` 桶**（= 今天的行为）并如实留白 —— ⛔ 不猜（多播比不播更糟）。
        ///    🆕 **2026-10-24（`A2` · `D4`）**：回桶时**按拍分件** —— 只留触发那一拍该播的
        ///    **卡级行**（= 原版 `AbilityLogic.PlayAnim` 播的 `cardAnimations[]`）。
        ///    🔴 **2026-10-25（执行代理 `R` · `R8-b`）就地改口径（铁律 5）**：原来这里写「`Trigger(3)` /
        ///    `TriggerOnPlay(6)` + 卡级行」，即 trait 的 3/6 档照播 —— **那是缺陷**（别的 trait 的框会在
        ///    错的一拍出现）。现在**所有 trait 行一律挡掉**（trait 框只归链②，见 `IsTraitFrameRow`）。
        ///    🆕 **2026-10-24（`W6`）**：卡级行**还要对得上这一拍的名字**（`CardAnimTime.TriggerName`）
        ///    —— 见 `TriggerBeatRowsMatching`；`ability` 里那 408 行挪过来之后，不挡就会把
        ///    别的 trigger 的动画都在这一拍播一遍（`SOR8` 的 `Rage` 件在 `Flank` 那一拍）。
        ///    🆕 **2026-10-24（`W5` · `R8`）**：**本拍由 `TraitFrames` 负责播的那件**记进
        ///    `TraitFrameDuplicates`（诊断口；过滤早已无条件生效）。原版只播一次
        ///    （链①=本方法所在的整桶路、链②=`TraitFrames.DisplayTrigger`）。</para>
        ///
        /// <para>**目标怎么定**：`keyword` 非空 ⇒ **先经 <see cref="CanonicalTriggerName"/> 归一**
        /// （🆕 2026-10-23「新账-86」：`penitence` → `Rage`），归一名是
        /// <see cref="IsRoutedTriggerName"/> 认的那一批 ⇒ 用它；
        /// 否则**只在「整张卡只标出一种 trigger 名、且它是那一批之一」时**推断。
        /// ✅ **2026-10-19（战斗线 `T21`）就地更新**：`keyword` **生产路径已经传得进来了**
        /// （`BattleDriver.PlaySignal` → `FireEventAll(keyword:)`）。保守推断档只剩给
        /// 「`keyword` 为空」的旧事件 / 自检直调。</para>
        ///
        /// <para>⚠️ **未标名的候选**（`AddTriggerNamesOf` 一个名字都没加 —— 卡 SO 顶层字段 / 关键词天赋行）：
        /// 归口生效时**两张桶都丢掉**（它们证明不了自己属于这个 trigger）。今天受影响的只有
        /// `GSC32`/`GSC28`/`GSC37` 那几条**关键词天赋**行（Concussive/Armour/Talent —— 这几个
        /// 关键词在我们引擎里**根本不触发**，报告 §3·2），丢掉它们 = 不再在伏击那一拍乱播。
        /// 🔴 **2026-10-23（`T1`）**：这几条 2026-10-23 已由 `inferred-keyword` 升成 `authoritative`
        /// （`src: cardtrait-so`）—— **结论不变**：它们不在 `CardDef.AnimTimes` 里 ⇒ 一个 trigger 名
        /// 都标不上 ⇒ 仍被丢掉。⛔ 别因为「现在是 authoritative」就改成「一律保留」。</para>
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
            // 🆕 2026-10-24（`W5` · `R8`）：`chainName` = **事件自己的关键词**（归一后），只用来问
            //   「这一拍 `TraitFrames` 会播哪几件」；`keyword` 为空 ⇒ `null`（不挡，见 `IsTraitFrameDuplicate`）。
            string chainName = string.IsNullOrEmpty(keyword) ? null : CanonicalTriggerName(keyword);
            if (times == null || times.Count == 0)
            {
                // 🆕 2026-10-20（`FACTION-DOUBLE605`）：带能力序号的拍**证明不了**是哪几条动画
                //   ⇒ 宁可留白（多发一条事件时「全播」= 两遍，那正是本件要修的缺陷）。
                if (abilityIndex >= 0) return false;
                // 🆕 2026-10-24（`A2` · `D4`）：整桶里**只留触发那一拍该播的**（**卡级行**）。
                //   🔴 **2026-10-25（执行代理 `R` · `R8-b`）**：trait 行**一律**挡掉（判据见 `IsTraitFrameRow`）
                //   —— 旧版放行的 `Trigger(3)/TriggerOnPlay(6)` 就是「别的 trait 的框混进来」的来源。
                //   这一档 `times` 为空 ⇒ 名字撞车的卡级行**结构上判不出**（按名字判，见那个方法的 doc）。
                picks = TriggerBeatRows(trig, chainName, times);
                return picks != null && picks.Count > 0;
            }

            // ① 标名（两张桶一起）—— 🆕 2026-10-24（`A2`）：一条候选可以有**多个** trigger 名
            //   （同一个 `CardAnim` 在两条 `CardAbility` 里各出现一次，见 `AddTriggerNamesOf`）。
            var names = new List<string>();
            if (hasTrig) foreach (var p in trig) AddTriggerNamesOf(names, times, p);
            if (hasAbil) foreach (var p in abil) AddTriggerNamesOf(names, times, p);

            // ② 定目标
            //   🆕 2026-10-23（特效线「新账-86」）：**先归一成原版 trigger 名再判白名单**
            //   （`penitence` → `Rage`，映射只在 `CanonicalTriggerName` 一处）。不归一的话
            //   `target = "penitence"` 与候选侧的 `"Rage"` 永远 `NameIs` 不上 ⇒ 这一拍照样播不出来。
            string target = null;
            string targetSrc = !string.IsNullOrEmpty(keyword)
                             ? keyword
                             : (names.Count == 1 ? names[0] : null);   // 保守推断档（旧事件 / 自检直调）
            string canonical = null;
            if (targetSrc != null)
            {
                canonical = CanonicalTriggerName(targetSrc);
                if (IsRoutedTriggerName(canonical)) target = canonical;
            }
            if (target == null)
            {
                // 🆕 2026-10-20（`FACTION-DOUBLE605`）：带能力序号的拍**判不出目标 trigger 就留白**
                //   —— 掉回 `trigger` 粗桶会把整批候选再播一遍（同一张卡发两条事件时 = 播两遍）。
                if (abilityIndex >= 0) return false;
                // 🆕 2026-10-24（`A2` · `D4`）：同上 —— 整桶里只留触发那一拍该播的。
                // 🆕 2026-10-24（`W6`）：**再挡一道** —— 卡级行必须自己那条 `CardAnimTime.TriggerName`
                //   对得上这一拍（判据 / 为什么 / 灭自证 → `TriggerBeatRowsMatching` 的 doc）。
                //   ⚠️ 本拍的「名字」取 `canonical`（关键词归一后的那个），**不是** `keyword` ——
                //   `keyword` 为空但保守推断出唯一名字时（`DA10` 的 `Slay`）也要按它对；
                //   `penitence` → `Rage` 这类归一必须先生效，否则 `SOR8/10/21/27/32` 会被误挡。
                // 🆕 2026-10-24（`W5` · `R8`）：`chainName` 只传**事件自己的关键词**（空 ⇒ `null`）——
                //   它现在只决定**计数分类**（本拍链② 会不会播同一件），⛔ 不影响过滤（2026-10-25 起
                //   trait 行一律挡掉）；推断出来的名字仍然别传（链② 拿不到它 = 分类会说谎）。
                picks = TriggerBeatRowsMatching(trig, times, canonical, chainName, cardIdOrName);
                return picks != null && picks.Count > 0;
            }

            // ③ 两张桶一起、只留标到目标的候选
            var merged = new List<CardPick>();
            if (hasTrig) foreach (var p in trig) if (CandidateHasTrigger(times, p, target)) merged.Add(p);
            if (hasAbil) foreach (var p in abil) if (CandidateHasTrigger(times, p, target)) merged.Add(p);
            // 🆕 2026-10-24（`A2` · `UM37`/`UM84`）：同一个 `CardAnim` 在两条 `CardAbility` 里各出现一次时，
            //   两边的候选都可能「命中」这个 target（分类键只看 `CardAnim`，分不出哪一条 occurrence）
            //   ⇒ 按 `times` 标到这一拍的行数截断（现读一次都不触发，见该方法的 doc）。
            merged = TrimToLabelledRowCount(merged, times, target, cardIdOrName);
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

        // ==================================================================
        //  🆕 2026-10-24（`A21`）：**`IsRoutedTriggerName` 白名单的两条常驻断言**
        //
        //  判据 → `资料/普查产出_1023e/C3_动画特效审查_索引.md` §四 D1 / D6 / D7
        //  为什么必须**双向**：只断「加进来的对」不够 —— 以后有人照着「原版 trigger 名字表」补齐时，
        //  会把**引擎根本没有发射口**的那几个也塞进来 ⇒ 要么永远等不到那一拍，要么更糟：在**别的一拍**
        //  上把这条 trigger 的候选播出去（错件 / 多播）。⇒ 两边各一条闸：
        //    · <see cref="NoEmitTriggerNames"/>   —— **反向**：没有发射口的**不许**进白名单；
        //    · <see cref="MustRouteTriggerNames"/> —— **正向锁**：有发射口的**必须**在（防误删）。
        //  另加**第三条闸：大小写三态** —— 引擎发的是小写常量、原版枚举是 PascalCase，
        //  而 C# `switch (string)` 是 Ordinal ⇒ `case "Codex"` 对 `"codex"` 恒 false（C3 D6 就是这么坏的）。
        //  真正的证明是件级那一段（在真实卡表上跑 `ResolveAllTimed`，看取到的是卡自己那条、不是整桶）。
        // ==================================================================

        /// <summary>🆕 2026-10-24（`A21` · `C3` D1-c / D1-d / D7-c）：**引擎里【没有】发射口的原版 trigger 名**
        /// —— ⛔ **不许进 <see cref="IsRoutedTriggerName"/>**。
        ///
        /// <para>「没有发射口」的判据（2026-10-24 现核）：全仓 `Core/` 里**没有任何一处**带着这个
        /// **原版 trigger 名**发 `EvtKind.Trigger`（`FireTriggerOnSide` / `FireTriggerAt` /
        /// `EmitListenerTrigger` 的调用点逐个 grep 过）。两种成因都要按这张表挡：
        ///   ① **事件根本不存在**（只在 `CardDef` 的枚举表 / 注释里出现）；
        ///   ② **事件有、但名字到不了 `EvtKind.Trigger`**（`TriggerNameOfWhenKind` 没映射 ⇒
        ///      `EmitListenerTrigger` 直接 return），或**引擎发的是另一个名字**。
        /// 要放行某个名字，**必须先补发射口 / 映射**，再把它从本表删掉 ——
        /// 删这一行本身就是「我核过发射口了」的签名。</para>
        /// <list type="bullet">
        /// <item>`Unstable`(81)：`RuleCore.cs:8829-8844` 只 `EnqueueAbility`（`UnstableBlastDeferred`），
        ///   **不 `Emit(EvtKind.Trigger)`**（2026-10-24 `A21` 现核；C3 原文写的 `:8807-8822` 是那段注释的
        ///   起点，不是代码）⇒ Goff 18 张卡 / 36 条动画今天**永远不播**（C3 D1-c：要先补引擎那一跳）。</item>
        /// <item>`OnSecretCreated`(700)：全仓 `Core/` 只有 `CardDef` 的枚举表（DA 2 张 / 3 条）。</item>
        /// <item>`OnCollectQuestPoints`(695)：同上（DA 2 张 / 2 条）。</item>
        /// <item>`TriggeredCodex`(650)：只出现在注释里（UM 1 张 / 1 条）。⚠️ 与已在白名单里的 `Codex`
        ///   **不是同一个名字** —— `Codex` 有真发射口（`RuleCore.CheckCodex` → `FireTriggerAt`），它没有。</item>
        /// <item>✅ **2026-10-24（引擎线 `G1b` · `C2` §2 · D8）：`OnCollectSpiritStone`(610) 已放行，不在本表。**
        ///   留痕说明它当初为什么在：那一拍**有事件**（`WhenEventKind.GainSpirit`），但
        ///   `EffectResolver.TriggerNameOfWhenKind` 当时**没有 `GainSpirit` 分支** ⇒ `EmitListenerTrigger`
        ///   在 `tn == null` 时直接 return ⇒ `EvtKind.Trigger` **从不发**；而 `CrossTriggerDrops` 只是把
        ///   610 那条从 `ability` 桶里**丢掉**（C3 D8，丢弃本身对、⛔ 别删）。本批补上映射
        ///   （`CardScript__ReactToCollectSpiritStone.c:5-9` = `0x262`）⇒ 现在它在 `MustRouteTriggerNames` 里。</item>
        /// <item>🆕 `UnitLoseStealth`(647)：SaimHann 1 条 / 1 张，没有发射口。</item>
        /// <item>🆕 `UnitStunned`(291)：SaimHann 1 条 / 1 张，没有发射口。</item>
        /// <item>🆕 `OnSabotageCreated`(701)：Genestealers 1 条 / 1 张，没有发射口。</item>
        /// <item>🆕 **2026-10-24（`A27` · `C4` D7(C4)）：`UnitReceivesHuntMark`(520) / `OnCollectFaith`(665)**
        ///   —— ⚠️ **曾在这一张表里，2026-10-24（`A30`）已放行并从数组里删掉**，留痕说明它们为什么当初在这：
        ///   它们**不是**「没有事件」，而是「**事件到了映射这一层断了**」—— 监听事件**有**
        ///   （`WhenEventKind.GetsHuntMark` / `GainFaith`），但 `EffectResolver.TriggerNameOfWhenKind`
        ///   当时**没有这两个分支** ⇒ `EmitListenerTrigger` 在 `tn == null` 时直接 return
        ///   ⇒ `EvtKind.Trigger` **从不发**（连「播错件」都没有）。`A30` 同批补了映射
        ///   （`CardScript__ReactToUnitHuntMarked.c:7` = `0x208` / `CardScript__ReactToCollectFaith.c:7` = `0x299`）
        ///   ⇒ 现在它们是**正向锁**里的一员（见 <see cref="MustRouteTriggerNames"/>）。</item>
        /// <item>✅ **2026-10-24（引擎线 `G2` · `C4` D8）：`ThisUnitSummoned`(195) 已放行，不在本表。**
        ///   留痕说明它当初为什么在：部署那一拍引擎一侧只有两条出口 —— `rally`（`RuleCore` 部署入口的
        ///   `FireTriggerOnBoard`）与监听者的 `OtherUnitSummoned(190)`（`TriggerNameOfWhenKind` 的
        ///   `Deploy` 支，改前**不分自/他**）⇒ 与候选名 `ThisUnitSummoned` 永不相交。
        ///   本批把 `SelfOnly` 那一支分流成 195（判据 → `IsRoutedTriggerName` 里那个 `case`），
        ///   它现在是**正向锁**里的一员（见 <see cref="MustRouteTriggerNames"/>）。</item>
        /// <item>✅ **2026-10-24（引擎线 `G2` · `C4` D9）：`WhileInPlay`(20) 已放行，不在本表。**
        ///   留痕说明它当初为什么在：这一族是**常驻重算**能力，本仓的效果走光环层
        ///   （`Aura.Recompose`），**从来不 `Emit(EvtKind.Trigger)`** ⇒ 没有「一拍」。
        ///   本批补了表现口 `RuleCore.EmitWhileInPlayPass`（挂在**动作队列排空**那一刻，
        ///   判据 = `BattleManager__FinishResolvingAction.c:91`）⇒ 它现在是**正向锁**里的一员。
        ///   ⚠️ 已知歧义：`SW18` 的 `Card_Buff_Hand_SW` 同时挂在 `WhileInPlay` 与 `ThisCardPlayed`
        ///   下 ⇒ 件级自检**跳过**那一行（如实记，⛔ 不是通过）。</item>
        /// </list></summary>
        static readonly string[] NoEmitTriggerNames =
        {
            "Unstable", "OnSecretCreated", "OnCollectQuestPoints", "TriggeredCodex",
            // 🔴 **2026-10-24（引擎线 `G1b` · `C2` §2 · D8）**：`OnCollectSpiritStone`(610)
            //   **已补发射口并从本表移出** —— 映射 = `EffectResolver.TriggerNameOfWhenKind` 的
            //   `WhenEventKind.GainSpirit` 支；事件源 = `RuleCore.CollectWaystoneCore` 的
            //   `BroadcastWhen(..., GainSpirit, ...)`。⇒ 现在它在 `MustRouteTriggerNames` 里。
            "UnitLoseStealth", "UnitStunned", "OnSabotageCreated",
            // 🆕 2026-10-24（`A27` · `C4` D8(C4) / D9(C4)）：**名字不对 / 没有那一拍**
            //   （`UnitReceivesHuntMark` / `OnCollectFaith` 已于 2026-10-24 `A30` 补好映射并移出本表 ·
            //    `ThisUnitSummoned` / `WhileInPlay` 已于 2026-10-24 `G2` 补好发射口并移出本表
            //    ⇒ 它们现在都在 `MustRouteTriggerNames` 里）
        };

        /// <summary>🆕 2026-10-24（`A21`）：**必须**在白名单里的那批（**引擎已在发**，只差一条白名单）。
        /// 判据 = <see cref="IsRoutedTriggerName"/> 里各自 `case` 的整段注释。
        /// ⚠️ 这一条只是**锁**（防误删）；真正的证明见 <see cref="SelfCheckRoutedTriggers"/> 的件级那一段。
        /// ⚠️ 顺序与 <see cref="MustRouteTriggerKeywords"/> **逐个对应**（两张表必须同长、同序）。</summary>
        static readonly string[] MustRouteTriggerNames =
        {
            "Mob", "Teleport", "Codex", "Uprising", "Artifice",
            // 🆕 2026-10-24（`A27` · `C4` D6(C4)）：27 行 / 24 张，全在 EmperorsChildren
            "Cruelty", "Stimulation", "Ecstasy",
            // 🆕 2026-10-24（`A30` · `C4` D7 第二批）：监听者那一拍（映射在 `TriggerNameOfWhenKind`）
            //   5 行 / 2 张（SW20 / SW35）· 2 行 / 1 张（SOR38）
            // 🆕 2026-10-24（引擎线 `G1b` · `C2` §2 · D8）：收集路标石那一拍
            //   **5 行 / 4 张**（`ASH80` / `ASH28` / `ASH78` 2 行 / `ASH25`）。
            "UnitReceivesHuntMark", "OnCollectFaith", "OnCollectSpiritStone",
            // 🆕 2026-10-24（引擎线 `G2` · `C4` D8）：`Deploy` 的 **`SelfOnly`** 那一支
            //   1 行 / 1 张（`SW10 Grey Hunter`）—— 映射分流 + 白名单同批上。
            "ThisUnitSummoned",
            // 🆕 2026-10-24（引擎线 `G2` · `C4` D9）：常驻重算那一拍
            //   2 行 / 1 张（`SW18 Hrolf the Ironhowl`；其中 1 行是歧义行，件级会跳过）。
            "WhileInPlay",
            // 🆕 2026-10-24（执行代理 `J1` · `C5` D11/D12）：14 行 / 10 张（AM）· 1 行 / 1 张（TL18）
            "Regiment", "TriggeredSynapse",
            // 🆕 2026-10-24（执行代理 `W5` · AN1 `R5`）：`trigger` 桶「名字没放行 ⇒ 掉回整桶」那 **36 行 /
            //   32 卡** 全部 = 这两个名字（`Strike` 21 行 / 19 张 · `Slay` 15 行 / 13 张，现读）。
            //   发射口逐条现核 → `IsRoutedTriggerName` 里 `case "strike"` / `case "slay"` 那一段。
            "Strike", "Slay",
            // 🆕 2026-10-25（执行代理 `R` · 引擎线 `A3` 的表现侧）：`Talent`(46) 回合开始那一拍。
            //   ⚠️ 它**没有** `animTimes` 行 ⇒ 件级那半按 `TraitFrameOnlyTriggerNames` 的口径验
            //   （链② 绑着 `TalentEffect`），⛔ 别因为它「0 张卡」就从这里删掉。
            "Talent",
        };

        /// <summary>🆕 2026-10-24（`A21`）：上面那些名字**引擎实际发出来的 keyword 字面量**
        /// —— 件级自检拿它当 `ResolveAllTimed(..., keyword:)` 的实参，与生产路径逐参一致。
        /// ⚠️ 与 <see cref="MustRouteTriggerNames"/> **逐个对应**（同长、同序）。
        /// <para>🔴 **2026-10-24（`A30`）就地补注**：原来这里写「全是**小写** `KeywordTable.*` 常量」——
        /// 那只覆盖「关键词族」（`FireTriggerAt` 原样把小写关键词写进 `BattleEvent.Keyword`）。
        /// **监听者那一族（`UnitReceivesHuntMark` / `OnCollectFaith`）不同**：发射体是
        /// `EffectResolver.EmitCardTrigger` ⇒ `BattleEvent.Keyword = KeywordWithAbility(原版 trigger 名, -1)`
        /// ⇒ 生产路径发的是 **PascalCase 的原版枚举名**，不是小写关键词。
        /// 自检要**逐参一致** ⇒ 这两格必须写 PascalCase（⛔ 写成 `"gethuntmark"` / `"gainfaith"` 会让
        /// `IsRoutedTriggerName` 判不中 ⇒ 掉回 `trigger` 粗桶 ⇒ 自检报「漏播」）。</para></summary>
        static readonly string[] MustRouteTriggerKeywords =
        {
            KeywordTable.Mob, KeywordTable.Teleport, KeywordTable.Codex,
            KeywordTable.Uprising, KeywordTable.Artifice,
            KeywordTable.Cruelty, KeywordTable.Stimulation, KeywordTable.Ecstasy,
            // 🆕 2026-10-24（`A30`）：监听者那一族发的是原版枚举名本身（见上面那段补注）。
            //   与 `MustRouteTriggerNames` 同长同序 ⇒ 这两格与上表最后两项逐字相同。
            // 🆕 2026-10-24（`G1b` · D8）：`EmitCardTrigger` 发的是 PascalCase 原版枚举名
            //   ⇒ 这一格与上表最后一项逐字相同（⛔ 别写成 `"gainspirit"`）。
            "UnitReceivesHuntMark", "OnCollectFaith", "OnCollectSpiritStone",
            // 🆕 2026-10-24（`G2` · D8）：同上 —— `EmitCardTrigger` 发的是 PascalCase 原版枚举名。
            "ThisUnitSummoned",
            // 🆕 2026-10-24（`G2` · D9）：同上（`EmitWhileInPlayPass` → `EmitCardTrigger`）。
            "WhileInPlay",
            // 🆕 2026-10-24（执行代理 `J1` · `C5` D11/D12）：
            //   `Regiment` 发的是 `KeywordTable.Regiment` 的**小写常量**（`FireTriggerAt` 原样写进
            //   `BattleEvent.Keyword`）；`TriggeredSynapse` 走 `EmitCardTrigger` ⇒ **PascalCase 原版枚举名**
            //   （与上面 520/665/195/20 同形）。两格都必须与 `MustRouteTriggerNames` **逐个对应**。
            KeywordTable.Regiment, "TriggeredSynapse",
            // 🆕 2026-10-24（执行代理 `W5` · AN1 `R5`）：两个都走 `FireTriggerAt` ⇒ 引擎把
            //   `KeywordTable.*` 的**小写常量**原样写进 `BattleEvent.Keyword`
            //   （`"strike"` / `"slay"`，= `KeywordTable.Strike` / `KeywordTable.Slay`）⇒ 这一格必须小写。
            //   ⛔ 写成 PascalCase 不会漏（`IsRoutedTriggerName` 会 `ToLowerInvariant`），但**自检要逐参一致**：
            //   件级那半拿这一格当 `keyword:` 实参，写错会让「引擎发的形态」这一态没人验。
            KeywordTable.Strike, KeywordTable.Slay,
            // 🆕 2026-10-25（执行代理 `R` · 引擎线 `A3`）：`talent` 走 `EmitCardTrigger(..., "Talent", …)`
            //   ⇒ 生产路径发的是 **PascalCase 原版枚举名**（与 520/665/195/20 同形）。
            "Talent",
        };

        /// <summary>🆕 2026-10-25（执行代理 `R` · 引擎线 `A3` 的表现侧）：**这些 trigger 名一个
        /// `animTimes` 行都没有** —— 它们的画面**完全**由 **链②**（`TraitFrames.DisplayTrigger`）出。
        /// <para>为什么单开一张表：`SelfCheckRoutedTriggers` 对 `MustRouteTriggerNames` 里每个名字都要求
        /// 「按卡现算，至少有一张卡标着它」，否则报**空放**。`Talent` 现读 **0 行**（2026-10-25 现算：
        /// 全池 `animTimes[].tn` 没有 `Talent`）—— 但这一拍**原版有槽**：`Talent.asset` =
        /// `triggerType 3 / TalentTraitTrigger / TalentEffect`（判据 → `资料/普查产出_1025/A_引擎线_执行报告.md` §A3
        /// 的五跳），由链② 播。⇒ 对这些名字，件级自检改问一件事：
        /// **「链② 里确实绑着触发那一拍那一件吗」**（`TriggerType ∈ {3,6}` 且有 `Prefab`）。
        /// ⛔ 别把 `Talent` 塞进 `NoEmitTriggerNames`（那等于说「引擎不会发」—— 已发了），
        /// 也⛔ 别为了让「空放」消失就从 `MustRouteTriggerNames` 删掉（那会让它掉回整桶）。</para></summary>
        static readonly string[] TraitFrameOnlyTriggerNames = { "Talent" };

        /// <summary>🆕 2026-10-24（`A21`）：白名单自检的**名字级**那一半（**不需要卡池**）。
        /// <see cref="EnsureLoaded"/> 载表时跑一次 ⇒ 白名单被改错**当场出声**，不必等自检宿主。
        /// 返回违规描述；空数组 = 通过。</summary>
        public static string[] SelfCheckRoutedTriggerNames()
        {
            var bad = new List<string>();
            for (int i = 0; i < NoEmitTriggerNames.Length; i++)
            {
                string n = NoEmitTriggerNames[i];
                if (IsRoutedTriggerName(n))
                    bad.Add($"`{n}` **引擎没有发射口**（判据见 `NoEmitTriggerNames` 的逐条注释）却进了 "
                          + "`IsRoutedTriggerName` 白名单 ⇒ 这条 trigger 的候选会在**别的一拍**上被播出去"
                          + "（错件 / 多播）。要放行必须先补 `EvtKind.Trigger` 发射口，再从 "
                          + "`NoEmitTriggerNames` 里删掉它。");
            }
            for (int i = 0; i < MustRouteTriggerNames.Length; i++)
            {
                string n = MustRouteTriggerNames[i];
                if (!IsRoutedTriggerName(n))
                    bad.Add($"`{n}` **不在**白名单里 ⇒ 这一拍 `target` 恒 null ⇒ `TryResolveTriggerPicks` "
                          + "走「判不出 ⇒ 原样回 `trigger` 粗桶」= 整桶播（错件）。发射口已存在"
                          + "（见 `IsRoutedTriggerName` 里那个 `case` 的注释）⇒ 它必须在白名单里。");
                // 🔴 **大小写三态（C3 D6）**：引擎发**小写**、原版枚举/候选是 PascalCase。
                //   今天 `IsRoutedTriggerName` 统一 `ToLowerInvariant()` ⇒ 三态**都必须 true**：
                //     · `codex`（引擎发的形态）—— 这一条正是 D6 那个 bug 的靶子；
                //     · `CODEX`（全大写）—— 归一若被改回 Ordinal `switch`，这条会先红。
                //   ⛔ 别把这条删掉当「重复」：`case "Codex"` 那种写法对 `"codex"` 恒 false，而
                //   第一条断言 `IsRoutedTriggerName(n)`（PascalCase）**照样是绿的** —— 只断前者抓不到 D6。
                string lo = n.ToLowerInvariant();
                string up = n.ToUpperInvariant();
                if (!IsRoutedTriggerName(lo))
                    bad.Add($"`{n}` 的**引擎小写形态** `{lo}` 不在白名单 ⇒ 正是 C3 D6 那个 bug"
                          + "（C# `switch (string)` 是 Ordinal；`case \"Codex\"` 对 `\"codex\"` 恒 false）。"
                          + "修法 = 让 `IsRoutedTriggerName` 先 `ToLowerInvariant()` 再比。");
                if (!IsRoutedTriggerName(up))
                    bad.Add($"`{n}` 的**全大写形态** `{up}` 不在白名单 ⇒ 大小写归一没生效"
                          + "（`switch (triggerName.ToLowerInvariant())` 那一句被改回去了？）。");
            }
            return bad.ToArray();
        }

        /// <summary>🆕 2026-10-24（`A21` · `C3` D1 / D6 / D7）：白名单自检的**完整版**（名字级 + **件级**）。
        /// 返回违规描述；**空数组 = 通过**（⛔ 别把「卡池没载到」当通过 —— 那会单列一条）。
        ///
        /// <para>件级那一段做什么：对**每一张**有 `CardAnimTime.TriggerName == <白名单名>` 行的卡，
        /// 按**生产路径逐参**（`perCardOnly: true` + 引擎关键词常量，见 `BattleDriver.PlaySignal` 的
        /// Trigger 支）调一次 `ResolveAllTimed`，逐件比：
        ///   · 取到的每一件都必须是**这张卡标着这个 trigger 名**的那批候选（多一件 = 整桶播了）；
        ///   · 那批候选一件都不能少（少一件 = 归口把它丢了）。
        /// 现读口径（2026-10-24，逐张现算 —— ⛔ 断言里别写死这些数）：
        /// `Mob` 11 张/15 行 · `Teleport` 6 张/9 行 · `Codex` 23 张/33 行
        /// （**32 行有件**：`UM28` 那条 `Wait_1s` 是纯计时拍 = D5；🆕 **2026-10-24（`A2`）起
        /// 「一个 `CardAnim` 挂两个 trigger」的 3 行**（`UM37` / `UM84` / `SW18`）**也逐条验**
        /// —— 分类键改成「一条候选可以有多个 trigger 名」之后它们不再是盲区）·
        /// `Uprising` 8 张/13 行 · `Artifice` 11 张/14 行
        /// （C3 第二批只覆盖新三阵营 ⇒ 它记的 `Artifice`「3 张/6 条」不含 Sautekh 那 8 张）·
        /// 🆕 `Cruelty` **8 张/10 行** · `Stimulation` **10 张/10 行** · `Ecstasy` **7 张/7 行**
        /// （三个全在 EmperorsChildren；2026-10-24 `A27` 现算：**0 行无件、0 行歧义**）·
        /// 🆕 **2026-10-24（`A30`）** `UnitReceivesHuntMark` **2 张/5 行**（`SW20` 2 · `SW35` 3）·
        /// `OnCollectFaith` **1 张/2 行**（`SOR38`）—— 三条都逐行有 prefab（现读 `cards_engine.json`）·
        /// 🆕 **2026-10-24（`G2` · D8）** `ThisUnitSummoned` **1 张/1 行**（`SW10 Grey Hunter`）·
        /// 🆕 **2026-10-24（`G2` · D9）** `WhileInPlay` **1 张/2 行**（`SW18 Hrolf the Ironhowl`；
        /// 🆕 **2026-10-24（`A2`）起 `Card_Buff_Hand_SW` 那行也验** —— 它同时挂在
        /// `WhileInPlay` 与 `ThisCardPlayed` 下，现在两边都取得到）·
        /// 🆕 **2026-10-24（执行代理 `J1` · `C5` D11/D12）** `Regiment` **10 张/14 行**
        /// （全 AstraMilitarum；`AM6`/`AM37`/`AM69` 各 2 行；**0 行无件、0 行歧义**）·
        /// `TriggeredSynapse` **1 张/1 行**（`TL18 Zoanthrope`）。
        /// 🆕 **2026-10-24（引擎线 `G1b` · `C2` §2 · D8）** `OnCollectSpiritStone` **4 张/5 行**
        /// （全 SaimHann：`ASH80` 1 · `ASH28` 1 · `ASH78` 2 · `ASH25` 1；逐行有 prefab）·
        /// 🆕 **2026-10-24（执行代理 `W5` · AN1 `R5`）** `Strike` **19 张/21 行** · `Slay` **13 张/15 行**
        /// （两个合起来正是 `trigger` 桶里那 36 行 / 32 卡；现读 **0 多播 / 0 漏播**）——
        /// 这两条就是 R5 的**件级证明**：放行前它们走第 ④ 档「整桶播」，放行后只取卡自己那几条。</para>
        /// <para>⚠️ 本方法走的是**生产解释路径**（`ResolveAllTimed`）⇒ 理论上会给 `UnpinnedPicks` 那类计数器加数
        /// （现读**增量 0**：候选全部来自 `cardAnimations[]`、都能钉上 `AnimTimes`）。若宿主对那个计数敏感，
        /// 断言前先 `ResetUnpinnedForTest()`。</para></summary>
        public static string[] SelfCheckRoutedTriggers()
        {
            var bad = new List<string>(SelfCheckRoutedTriggerNames());
            EnsureLoaded();
            var pool = CardDatabase.Load();
            if (pool == null || pool.Count == 0)
            {
                bad.Add("**没验**：卡池没载到（`CardDatabase.Load()` 回空）⇒ 件级那一段跑不了。"
                      + "这不是通过 —— 见 `SelfCheckRoutedTriggers` 的 doc。");
                return bad.ToArray();
            }
            // 引擎实际发出来的关键词（小写常量；白名单靠 `ToLowerInvariant` 对上、候选靠 `NameIs` 对上）
            string[] kws = MustRouteTriggerKeywords;
            if (kws.Length != MustRouteTriggerNames.Length)
                bad.Add("`MustRouteTriggerNames` 与 `MustRouteTriggerKeywords` **长度不等** ⇒ 两张表错位了"
                      + "（它们是逐个对应的，改成同一个长度）。");
            for (int k = 0; k < MustRouteTriggerNames.Length && k < kws.Length; k++)
            {
                string name = MustRouteTriggerNames[k];
                string kw = kws[k];
                int cards = 0, rows = 0;
                foreach (var kv in ByCard)
                {
                    var card = CardDatabase.FindById(pool, kv.Key);
                    if (card == null || card.AnimTimes == null || card.AnimTimes.Count == 0) continue;
                    if (string.Equals(card.Type, "enchantment", System.StringComparison.OrdinalIgnoreCase)) continue;   // 🔴 2026-10-25：附魔不是单位（它的 trigger 60 是附魔能力，不是单位技能动画）⇒ 不进 unit_*_anim 桶、本自检也不要求搬
                    List<string> want = null;
                    for (int i = 0; i < card.AnimTimes.Count; i++)
                    {
                        var t = card.AnimTimes[i];
                        if (t == null || !NameIs(t.TriggerName, name)) continue;
                        // `Wait_*` 纯计时拍没有 prefab（原版 `animAdressable` 就是空 GUID）⇒ 本来就没件
                        if (string.IsNullOrEmpty(t.Prefab)) continue;
                        // 🔴 **2026-10-24（`A2` · `UM37`/`UM84`）就地改口径（铁律 5）**：原来这里用
                        //    `AmbiguousTriggerLabel` **跳过**「同一个 `CardAnim` 挂两个 trigger」的行
                        //    （`UM37` / `UM84` / `SW18` 共 3 行）—— 那时分类键只取第一条同名行，
                        //    第二条 occurrence **结构上取不到**，跳过是「如实记账」。现在分类键
                        //    **带上了它**（`AddTriggerNamesOf`：一条候选可以有多个 trigger 名）⇒
                        //    这三行**必须逐条验**。⛔ 别把跳过加回来（那会让这三张卡的 Codex /
                        //    WhileInPlay 拍永远没人验）。
                        if (want == null) want = new List<string>();
                        want.Add(t.Prefab);
                    }
                    if (want == null) continue;
                    cards++; rows += want.Count;
                    string faction;
                    _factionOfId.TryGetValue(kv.Key, out faction);
                    // 传 **id**（不是卡名）：重名 3 组的解析与本件无关，这里要验的是**归口**
                    var got = ResolveAllTimed(Trigger, faction, kv.Key, card.AnimTimes,
                                              null, true, kw);
                    var left = new List<string>(want);
                    for (int i = 0; i < got.Length; i++)
                    {
                        if (!left.Remove(got[i].Name))
                            bad.Add($"`{card.Name}`（{kv.Key}）的 `{name}` 那一拍**多播**了 `{got[i].Name}`"
                                  + "（不属于这个 trigger 的候选）⇒ 归口没生效 / 整桶播了。");
                    }
                    for (int i = 0; i < left.Count; i++)
                        bad.Add($"`{card.Name}`（{kv.Key}）的 `{name}` 那一拍**漏播** `{left[i]}`"
                              + "（原版这一行的 outer trigger 标的就是它）");
                }
                if (cards == 0)
                {
                    // 🆕 2026-10-25（执行代理 `R` · 引擎线 `A3`）：`TraitFrameOnlyTriggerNames` 里的名字
                    //   **本来就没有 `animTimes` 行** —— 它们的画面完全由链② 出。
                    //   ⇒ 这里的期望换成「**链② 里确实绑着触发那一拍那一件**」（见那张表的 doc）。
                    if (System.Array.IndexOf(TraitFrameOnlyTriggerNames, name) >= 0)
                    {
                        var tb = TraitFrames.TriggerBindingsFor(name);
                        int withPrefab = 0;
                        for (int t = 0; t < tb.Length; t++)
                            if ((tb[t].TriggerType == 3 || tb[t].TriggerType == 6)
                                && !string.IsNullOrEmpty(tb[t].Prefab)) withPrefab++;
                        if (withPrefab == 0)
                            bad.Add($"`{name}` 不在 `animTimes` 里（= 只能由链② 播），而 `TraitFrames.TriggerBindingsFor(\"{name}\")` 里"
                                  + $"**没有** `TriggerType ∈ {{3,6}}` 且带 `Prefab` 的行（现读 {tb.Length} 行 / {withPrefab} 行可用）"
                                  + " ⇒ **两条链都不播** = 这一拍彻底静默（原版 `Talent.asset` 是有这一件的）。");
                    }
                    else
                        bad.Add($"全表**一张卡**都没有 `TriggerName == {name}` 的候选 ⇒ 白名单这一条是空放"
                              + "（数据侧变了？⛔ 别把白名单留在那儿当装饰）");
                }
            }
            return bad.ToArray();
        }

        // ==================================================================
        //  🆕 2026-10-24（执行代理 `E3`）：**`D3` 两桶 / `D4` 分拍 的全池逐卡自检**
        //
        //  为什么上面那份 `SelfCheckRoutedTriggers` 盖不住这两件：
        //   · **`D3`**：`UnitAttack` / `UnitDamaged` **不走 `TryResolveTriggerPicks`**（`BattleDriver.PlaySignal`
        //     按 canonical 直接分流到 `PlayUnitSkillAnim`，见那一支的注释），所以它们不在
        //     `MustRouteTriggerNames` 的逐卡覆盖里 ⇒「载入时把 `attack_melee` / `hit` 的行**搬**进这两个桶」
        //     这件事**一条断言都没有**。判据 → `资料/普查产出_1023e/A21_VfxMap白名单.md` §二·4 第 2 步。
        //   · **`D4`**：`TriggerBeatRows` 只在**掉回整桶**那两档（第 ② / ④ 支）生效，而上面那份自检
        //     走的是第 ③ 档（目标 trigger 名定得出来）⇒ 那道滤波**一条也没被覆盖**。
        //     判据 → 同文件 §一 + `C4_动画特效审查_索引.md` §四 D4 / P7。
        //  ⇒ 两个口都**返回违规描述数组**（空 = 通过），宿主 = `Editor/BattleVfxAssertE3.cs`。
        //  ⚠️ 期望值**全部按卡池现算**（`CardDef.AnimTimes` + 桶内容），⛔ 不写死行数 —— 数据一变会以
        //     「漏搬 / 多播」报出来，而不是静默变空（那样断言就退化成自证了）。
        //  ⚠️ 两个口都走**生产解释路径**（`ResolveAllTimed`）⇒ 会给 `UnpinnedPicks` 加数，
        //     对那个计数敏感的宿主先调 `ResetUnpinnedForTest()`（见 `SelfCheckRoutedTriggers` 同一条提示）。
        // ==================================================================

        /// <summary>🆕 2026-10-24（执行代理 `E3` · 战斗线 `D3`）：**`unit_attack_anim` / `unit_damaged_anim`
        /// 两桶的全池逐卡自检**。返回违规描述；**空数组 = 通过**（⛔ 卡池没载到**不算**通过，单列一条）。
        ///
        /// <para>它盯四件事（每一件都对应一个**改坏了会静默**的地方）：</para>
        /// <list type="number">
        /// <item>**搬全了**：原版数据里（`CardDef.AnimTimes`，`tn == UnitAttack/UnitDamaged` 且有 prefab 的行）
        ///   有行的每一张卡，都必须在对应桶的 `_unitAnimCards` 名单里 —— 漏一张 = 这张卡的技能动画**永远不播**。</item>
        /// <item>**不多不少**：对桶里的每一张卡，按生产路径（`perCardOnly: true`）取回来的件必须**正好**是
        ///   它自己那批 —— 多一件 = 把不属于这个 trigger 的行也搬进来了；少一件 = 归口丢件。</item>
        /// <item>**「搬」不是「抄」**：旧桶（`attack_melee` / `hit`）里**不许**还留着这几件 ——
        ///   留着 = 同一件在命中帧播一次、再当技能动画播一次（原版没有的第二下）。</item>
        /// <item>**计数与逐卡加总一致**：`CardTableUnitAttackEntries` / `…DamagedEntries` 必须等于
        ///   按卡池现算出来的行数（那两个数是 `W63`/`T1` 的口径，被别处引用）。</item>
        /// </list>
        /// <para>⚠️ 它**不判播放时机/锚点**（那是 `BattleDriver` 的事，宿主里有行为级断言）。</para>
        /// <para>🧨 **改坏就红**：删掉 `LoadTableFromJson` 里 `m[UnitAttack] = picks;` 那一跳 ⇒ ①②④ 同时红；
        ///   把 `movedToUnitAnim` 改成「也写回旧桶」⇒ ③ 红；把 `_unitAnimCards` 的登记删掉 ⇒ ① 红。</para></summary>
        public static string[] SelfCheckUnitSkillAnims()
        {
            var bad = new List<string>();
            EnsureLoaded();
            var pool = CardDatabase.Load();
            if (pool == null || pool.Count == 0)
            {
                bad.Add("**没验**：卡池没载到（`CardDatabase.Load()` 回空）⇒ `UnitAttack`/`UnitDamaged` 的"
                      + "逐卡归口跑不了。这不是通过 —— 见 `SelfCheckUnitSkillAnims` 的 doc。");
                return bad.ToArray();
            }

            string[] evts = { UnitAttack, UnitDamaged };
            string[] names = { UnitAttackTriggerName, UnitDamagedTriggerName };
            for (int e = 0; e < evts.Length; e++)
            {
                string evt = evts[e], trigName = names[e];
                string oldEvt = evt == UnitAttack ? AttackMelee : Hit;
                int expectedRows = 0;

                // ① 原版数据里「有这一族行」的卡，一张都不许漏搬
                for (int c = 0; c < pool.Count; c++)
                {
                    var card = pool[c];
                    if (card == null || card.AnimTimes == null || card.AnimTimes.Count == 0) continue;
                    if (string.Equals(card.Type, "enchantment", System.StringComparison.OrdinalIgnoreCase)) continue;   // 🔴 2026-10-25：附魔不是单位（它的 trigger 60 是附魔能力，不是单位技能动画）⇒ 不进 unit_*_anim 桶、本自检也不要求搬
                    int rows = 0;
                    for (int i = 0; i < card.AnimTimes.Count; i++)
                    {
                        var t = card.AnimTimes[i];
                        // `Wait_*` 纯计时拍没有 prefab（原版 `animAdressable` 是空 GUID）⇒ 本来就没件
                        if (t == null || !NameIs(t.TriggerName, trigName) || string.IsNullOrEmpty(t.Prefab)) continue;
                        rows++;
                    }
                    if (rows == 0) continue;
                    expectedRows += rows;
                    if (System.Array.IndexOf(UnitAnimCardIds(evt), card.Id) < 0)
                        bad.Add($"`{card.Name}`（{card.Id}）在原版数据里有 **{rows} 行 `{trigName}`**，"
                              + $"却没被搬进 `{evt}` 桶（`UnitAnimCardIds(\"{evt}\")` 里没有它）"
                              + " ⇒ 这张卡的技能动画**永远不播**。");
                }

                // ② 桶里每一张卡：按生产路径取回来的 = 它自己那批（不多不少）
                var ids = UnitAnimCardIds(evt);
                if (ids.Length == 0)
                {
                    bad.Add($"`{evt}` 桶里**一张卡都没有** ⇒ 这一族整个是空放（数据侧变了？⛔ 别把桶留成装饰）。");
                    continue;
                }
                for (int k = 0; k < ids.Length; k++)
                {
                    string id = ids[k];
                    var card = CardDatabase.FindById(pool, id);
                    if (card == null)
                    {
                        bad.Add($"`{evt}` 桶里有 `{id}`，但卡池里找不到这张卡（id 漂了？）");
                        continue;
                    }
                    var want = new List<string>();
                    if (card.AnimTimes != null)
                        for (int i = 0; i < card.AnimTimes.Count; i++)
                        {
                            var t = card.AnimTimes[i];
                            if (t == null || !NameIs(t.TriggerName, trigName) || string.IsNullOrEmpty(t.Prefab)) continue;
                            want.Add(t.Prefab);
                        }
                    if (want.Count == 0)
                    {
                        bad.Add($"`{evt}` 桶里有 `{card.Name}`（{id}），而它的 `animTimes` 里**一行 `{trigName}` 都没有**"
                              + " ⇒ 桶里装错了东西（这一拍会播不属于它的件）。");
                        continue;
                    }
                    string faction;
                    _factionOfId.TryGetValue(id, out faction);
                    var got = ResolveAllTimed(evt, faction, id, card.AnimTimes, null, true);
                    var left = new List<string>(want);
                    for (int i = 0; i < got.Length; i++)
                        if (!left.Remove(got[i].Name))
                            bad.Add($"`{card.Name}`（{id}）的 `{evt}` 那一拍**多播**了 `{got[i].Name}`"
                                  + "（不属于它自己那批 `animTimes` 行）⇒ 搬的时候把别的行也带进来了。");
                    for (int i = 0; i < left.Count; i++)
                        bad.Add($"`{card.Name}`（{id}）的 `{evt}` 那一拍**漏播** `{left[i]}`"
                              + "（它自己的 `animTimes` 行里有这一件，归口却取不到）⇒ 归口丢件。");

                    // ③ 「搬」不是「抄」：旧桶（命中帧那一拍）不许还留着
                    var oldGot = ResolveAllTimed(oldEvt, faction, id, card.AnimTimes, null, true);
                    for (int i = 0; i < oldGot.Length; i++)
                        if (want.Contains(oldGot[i].Name))
                            bad.Add($"`{card.Name}`（{id}）的旧桶 `{oldEvt}` 里**还留着** `{oldGot[i].Name}`"
                                  + "（`W63` 那一跳是「搬」不是「抄」）⇒ 同一件会在命中帧播一次、再当技能动画播一次。");
                }

                // ④ 计数与逐卡加总一致（那两个数被别处引用，漂了就是静默的）
                int counted = evt == UnitAttack ? CardTableUnitAttackEntries : CardTableUnitDamagedEntries;
                if (counted != expectedRows)
                    bad.Add($"`{(evt == UnitAttack ? "CardTableUnitAttackEntries" : "CardTableUnitDamagedEntries")}` = {counted}，"
                          + $"而按卡池现算 `{trigName}` 有 **{expectedRows}** 行 ⇒ 搬的时候漏了/多了。");
            }
            return bad.ToArray();
        }

        /// <summary>🆕 2026-10-24（执行代理 `E3` · 战斗线 `D4`；🔴 **2026-10-25（执行代理 `R` · `R8-b`）改口径**）：
        /// **`trigger` 桶按拍分件**的全池逐卡自检。返回违规描述；**空数组 = 通过**。
        ///
        /// <para>**它验的是哪一档**：`TryResolveTriggerPicks` 掉回**整桶**的那两条路 ——
        ///   ② `times` 为空（这张卡没有逐候选时间）、④ 关键词不在白名单（目标 trigger 名定不出来）。
        ///   两条都必须**只**回**卡级行**（原版 `AbilityLogic.PlayAnim` 播的 `cardAnimations[]`），
        ///   **所有 trait 行一律不播**（trait 框只归链② `TraitFrames.DisplayTrigger`；判据 =
        ///   `CardScript__ActivateTriggerTraitAnim.c:6-20` 只调 `DisplayTriggerAnim` 一次，
        ///   `BattleManager__PlayTriggerTraitAnim.c:33` → `DisplayTriggerAnim` 只认 `triggerType ∈ {3,6}`）。</para>
        /// <para>🆕 **2026-10-24（`W5` · `R8`）那一半保留**：`TraitFrameDuplicates` 仍记账「本拍链②
        ///   会不会播同一件」；⚠️ 但**过滤已与它无关**（2026-10-25 起 trait 行一律挡掉）。
        ///   本口传的是编出来的 `__E3_not_a_trigger__` ⇒ 不是任何 trait 的触发名 ⇒
        ///   这一趟只会给 `TraitFramesOnOtherBeat` 加数（那正是 `R8-b` 的判别计数）。</para>
        ///
        /// <para>🆕 **2026-10-24（`W6`）那一层保留**：第 ④ 支还多一道 —— 卡级行要自己那条
        ///   `CardAnimTime.TriggerName` 对得上本拍（<see cref="TriggerBeatRowsMatching"/>）。
        ///   本口传的关键词是编出来的 `__E3_not_a_trigger__` ⇒ **标了名的卡级行全部预期被挡掉**、
        ///   只剩「没标名」的；第 ② 支**结构上做不到**这一层（`times` 为空 ⇒ 标不上名）⇒ 预期是全部卡级行。
        ///   ⚠️ 「标了名、且正是本拍那个名字 ⇒ 保留」这半边由 `DA10`（保守推断出 `Slay`）与
        ///   `BL15`（`keyword="Strike"`）那几条断言钉，⛔ 别在本口硬造一个假名字去测它。</para>
        ///
        /// <para>**期望值怎么来**：⛔ **不调被测的 `TriggerBeatRows` / `TriggerBeatRowsMatching`** ——
        ///   只用两个原始判据独立算：`TraitFrames.TriggerTypeOf`（名 → TriggerType，反查
        ///   `trait_effect_bindings.json`）＋ `AddTriggerNamesOf`（这条候选在**这张卡自己的**
        ///   `animTimes[]` 里标着哪些 trigger 名）。trait 分类 = 名字在表里**且**标不出任何 trigger 名
        ///   （后者 = 「名字撞车的卡级行」，见 `IsTraitFrameRow` 的 doc —— 现读 39 行）。</para>
        ///
        /// <para>⚠️ **不是「有 trait 行的卡数」这种计数断言** —— 这里逐卡比**名字多重集**，所以
        ///   ① 把 trait 行放回来 ⇒ 多播；② 把滤波写成「一律丢」⇒ 卡级行也没了 ⇒ 漏播；
        ///   ③ 把 `TriggerBeatRows` 只接到 ② 或只接到 ④ ⇒ 另一档的那批卡报出来。三种改法都会红。</para>
        /// <para>🧨 **改坏就红**：删掉 `TriggerBeatRows` 的两处调用、把 `IsTraitFrameRow` 改回旧口径
        ///   （只挡 0/1/2/5、放行 3/6）、或改成恒 false（整桶播）⇒ 本口逐条报出来。
        ///   第 ④ 支那一层同理：把它写成「一律丢卡级行」⇒ 有「没标名」行的卡报漏播；
        ///   把它删掉 ⇒ 标着别的 trigger 的卡级行回来 ⇒ 报多播。计数那一半
        ///   （`TraitFramesOnOtherBeat` 增量为 0）也在这里盯着 ⇒ 闸整个删掉时**不是空放**。</para></summary>
        public static string[] SelfCheckTriggerBeatSplit()
        {
            var bad = new List<string>();
            EnsureLoaded();
            var pool = CardDatabase.Load();
            // 这一档**不需要卡池**（桶内容 + `TraitFrames` 就够）：`times` 只影响钉行、不影响名字集合。
            //   卡池没载到时 `times` 传 null ⇒ 走第 ② 支，照样验得动（⛔ 不因为卡池缺席就跳过）。
            const string NotATriggerKeyword = "__E3_not_a_trigger__";   // 必然不在白名单 ⇒ 走第 ④ 支
            int cardsWithTraitRows = 0, droppedRows = 0, witness0 = TraitFramesOnOtherBeat;

            foreach (var kv in ByCard)
            {
                List<CardPick> trig;
                if (kv.Value == null || !kv.Value.TryGetValue(Trigger, out trig)) continue;
                if (trig == null || trig.Count == 0) continue;

                var card = pool != null ? CardDatabase.FindById(pool, kv.Key) : null;
                var times = card != null ? card.AnimTimes : null;
                bool canLabel = times != null && times.Count > 0;

                // 期望值：**独立**算（只用原始判据，⛔ 不调被测的 `TriggerBeatRows*`）
                // ⚠️ 两档的**判据不同**（这一点踩过）：
                //   · ② `times` 为空 ⇒ 只能**按名字**判 trait（`IsTraitFrameRow(p, null)`）⇒ 名字撞车的
                //     卡级行**也会被挡**（结构上分不出来）⇒ `expectTwo` 只收「名字不在 trait 表里」的；
                //   · ④ `times` 有 ⇒ 撞车行能自证成卡级行 ⇒ `expectFour` 收「不是 trait**或**标得出名」的，
                //     再叠 `W6` 那道名字闸。
                var expectTwo  = new List<string>();
                var expectFour = new List<string>();
                int traitRows = 0;
                for (int i = 0; i < trig.Count; i++)
                {
                    var p = trig[i];
                    bool nameIsTrait = TraitFrames.TriggerTypeOf(p.A, p.N) >= 0;
                    var labels = new List<string>();
                    if (canLabel) AddTriggerNamesOf(labels, times, p);
                    if (nameIsTrait) traitRows++;
                    if (!nameIsTrait) expectTwo.Add(p.N);                       // ②：按名字判
                    // ④：名字在 trait 表里、且**这张卡自己的** `animTimes[]` 里标不出任何 trigger 名 ⇒ 真 trait
                    //    （标得出 = 「名字撞车」的卡级行，原版 `AbilityLogic.PlayAnim` 会播 ⇒ ⛔ 不算 trait）
                    if (nameIsTrait && labels.Count == 0) continue;
                    bool keepFour = true;
                    if (canLabel && labels.Count > 0)
                    {
                        keepFour = false;
                        for (int k = 0; k < labels.Count; k++)
                            if (NameIs(labels[k], NotATriggerKeyword)) { keepFour = true; break; }
                    }
                    if (keepFour) expectFour.Add(p.N);
                }
                if (traitRows == 0) continue;            // 桶里没有 trait 行 ⇒ 这一张没什么可验
                cardsWithTraitRows++;
                droppedRows += traitRows;

                string faction;
                _factionOfId.TryGetValue(kv.Key, out faction);
                // 第 ④ 支（关键词不在白名单 ⇒ 目标 trigger 名定不出来）+ 第 ② 支（times 为空）。
                //   两档的预期**不同**：④ 多一层「卡级行对名字」的闸（`W6`），② 结构上做不到。
                // ⚠️ 这两趟是**故意**用编出来的关键词去踩第 ④ 支（不是生产）⇒ 临时闭麦，
                //    否则 `BattleScene.Run` 里会刷出十几条「卡级行标着别的 trigger」的告警（看着像缺陷）。
                bool quietWas = _quietGateWarn;
                _quietGateWarn = true;
                var gotFour = ResolveAllTimed(Trigger, faction, kv.Key, times, null, true, NotATriggerKeyword);
                var gotTwo  = ResolveAllTimed(Trigger, faction, kv.Key, null,  null, true, NotATriggerKeyword);
                _quietGateWarn = quietWas;
                for (int pass = 0; pass < 2; pass++)
                {
                    var got = pass == 0 ? gotFour : gotTwo;
                    string tag = pass == 0 ? "④ keyword 不在白名单" : "② 没有 animTimes";
                    var want = pass == 0 ? expectFour : expectTwo;
                    var left = new List<string>(want);
                    for (int i = 0; i < got.Length; i++)
                        if (!left.Remove(got[i].Name))
                            bad.Add($"`{kv.Key}` 的触发那一拍（第 {tag} 支）**多播**了 `{got[i].Name}`"
                                  + "（它不属于触发那一拍 —— trait 行（`src: cardtrait-so`）又回来了？）");
                    for (int i = 0; i < left.Count; i++)
                        bad.Add($"`{kv.Key}` 的触发那一拍（第 {tag} 支）**漏播** `{left[i]}`"
                              + "（触发那一拍该播的**卡级行**被切掉了）");
                }
            }
            if (cardsWithTraitRows == 0)
                bad.Add("全表**一张卡**都没有「`trigger` 桶里混着 trait 行」⇒ 本自检空放"
                      + "（数据侧变了？⛔ 别把 `TriggerBeatRows` 留成没人验的装饰）。");
            if (bad.Count == 0 && droppedRows == 0)
                bad.Add("自检跑了但**一行都没被挡掉** ⇒ `IsTraitFrameRow` 恒 false"
                      + "（`TraitFrames` 的 `TriggerType` 反查表没建起来？）");
            if (bad.Count == 0 && TraitFramesOnOtherBeat == witness0)
                bad.Add("自检跑了但一条「**本拍链② 也不播**的 trait 行」都没被挡掉"
                      + "（`VfxMap.TraitFramesOnOtherBeat` 增量 0）⇒ `R8-b` 那道闸没人验（`IsTraitFrameRow` 恒 false？）");
            return bad.ToArray();
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

        /// <summary>🆕 2026-10-25（执行代理 `C` · 特效线账）：**自检读口** —— 「卡名 + 阵营 → 表里那张卡的 id」
        /// （= <see cref="ResolveCardIdOf"/> 的只读口，⛔ 产品别用它）。
        /// <para>用途：钉住「**阵营参数取的是卡自己的 `cardArmy`，不是座位阵营**」这条判据 ——
        /// 跨阵营同名卡（`Terminator` / `Terminator Champion` / `Maulerfiend`）用**对的阵营**能钉到唯一那张，
        /// 用**错的阵营**会返回 `null` 并出声（`ResolveCardIdOf` 的重名不猜口径，见 <see cref="Resolve"/> 的 doc）。</para></summary>
        public static string ResolveCardIdForTest(string keyOrName, string faction)
            => ResolveCardIdOf(keyOrName, faction);

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
