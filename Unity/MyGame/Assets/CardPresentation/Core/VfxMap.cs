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
            { Deploy,       "Sororitas Summon Basic" },     // 登场：火柱升起（实拍很好看）
            { AttackMelee,  "BulletImpactBurst_crowd" },    // 近战：碎屑迸溅
            { AttackRanged, "BulletImpact_2shot" },         // 远程：弹着点
            // 🔴 **2026-09-15 实拍复核过**（`_tmp_view/pick_hit.png`）：**五个候选里这件最好看**
            //    （橙色火星 → 橙团 → 淡出；其余几个全是暗点/灰团）。
            // ⚠️ 但要说清楚：台账把 `Tap Firepit` 判 **`TAP_SKIP`** = **3D 战场火盆的点击反馈**，
            //    **不是「挨打」**—— 语义上它不对。原版在攻击结算处接的是 `AttackHitSmall`
            //    （在我们导出索引里判「**两边全程空（完全脚本驱动）**」）和 `Actual_Explosion`（**不在索引**）
            //    ⇒ **没有能用的原版命中特效**，只能挑好看的。**这是我们挑的，不是原版的做法。**
            { Hit,          "Tap Firepit" },
            // 🔴 **2026-09-15 实拍复核过**（`VfxPicker`，`_tmp_view/pick_death.png` 是五个候选 × 三个时刻的对照图）：
            //    · `Explosion Fenrisian Monstrosities` = **一整个橙色爆炸，五个里明显最好** ← 改用它
            //    · `Explosion_Possession`（原来这件）= 橙色碎片 + 烟，能用但小；台账判 **`ATK_EVENT`**
            //      （BL 战术 `Rites of Possession` 的命中）⇒ 本来就不该当通用阵亡
            //    · `Explosion_Short` → 渲成**洋红色方块**、`Necrons death explosion` → **黑方块**（都是坏 shader）
            //    · `Antimatter Explosion` → 只有一个小绿点
            // ⚠️ 原版的通用死亡爆散是 **`Card 3D Death Explosion`**，那件**不在我们的导出索引里**
            //    （脚本驱动的 3D 效果）⇒ 这一件**给不了原版语义**，只能在「能播的里面挑最好看的」。
            //    **这是我们挑的，不是原版的做法。**
            { Death,        "Explosion Fenrisian Monstrosities" },
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
        };

        /// <summary>阵营覆盖：登场特效按阵营换（火/水两套明显不同的）</summary>
        static readonly Dictionary<string, Dictionary<string, string>> ByFaction =
            new Dictionary<string, Dictionary<string, string>>
        {
            { "Ember", new Dictionary<string, string> { { Deploy, "Sororitas Summon Basic" } } },
            { "Tide",  new Dictionary<string, string> { { Deploy, "Tau_SummonCircle" } } },
        };

        /// <summary>逐卡覆盖（键 = 卡名，和 `CardData.id` 一致）。现在是空的，留给以后配。</summary>
        static readonly Dictionary<string, Dictionary<string, string>> ByCard =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>查一个事件该播什么。查不到返回 null（调用处会跳过）。</summary>
        public static string Resolve(string evt, string faction = null, string cardId = null)
        {
            if (string.IsNullOrEmpty(evt)) return null;

            Dictionary<string, string> m;
            if (!string.IsNullOrEmpty(cardId) && ByCard.TryGetValue(cardId, out m))
            {
                string s;
                if (m.TryGetValue(evt, out s)) return s;
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
            PlayCard, Deploy, AttackMelee, AttackRanged, Hit, Death, Ability, Trigger,
            Return, GainFaith, GainSpirit, GainQuest,
        };

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
