// CardDefPartial_B94B16.cs — 第九十四会话 · 波 3 · 执行代理 `B94_B16`
//   `CardDef` 的**又一半**：trigger→trait 折算（账 `战斗逻辑驱动:994`）、
//   trigger→ability **列表**（账 `:1217`）、`buffEffect.mutationType` 数据面（账 `:1254`）。
//
// 🔴 **为什么另起一个 `partial class CardDef` 块、写在【新文件】**：
//   完全同 `CardDefExtraTraits.cs` / `CardDefActiveAbility.cs` 的文件头纪律 ——
//   `CardDef.cs` 里有一批账落点按【行号】引用（现读 81 处，最大 `CardDef.cs:9137`），
//   在既有文件里插成员会让它们整体漂移（同族教训 → `资料/完全复刻/报告/B8_Q_trait口径与卡定义.md` §⑥）。
//   本文件走 partial ⇒ `CardDef.cs` 一个字节不动 ⇒ **预期零漂移**。
//
// 📌 本块只放**判据已齐、且落点本来就在 `CardDef` 名下**的三件；三件的消费端接线都不在这里：
//   ① `GetTraitFromAbilityTrigger` —— 原版是**静态查表函数**，没有运行期状态（消费者是表现层图标/动画，
//      `CardPresentation/Battle/AbilityIconAnim.cs:97 TraitOfTrigger` 是**同一规则的旧副本**
//      ⇒ 建议改成本函数的转发，见报告 §六·1）；
//   ② `AbilitiesForTrigger` —— 原版返回 `List<CardAbility>`；我们过去只有「归一到唯一一条」
//      （`CardDef.UniqueAbilityIndexForTrigger`）这一种形状 ⇒ 本块补**列表**形状。
//   ③ `AbilityMutationSpec` / `IsCumulativeMutation` —— 数据列（`mutationType`）**今天卡表里没有**
//      ⇒ 本块是**休眠实现**（`AttachAbilityMutations` 今天无人调用）；补数据要走
//      `工具/gen_cards_engine.py` + `Data/CardDatabase.cs`（⛔ 两个文件都不在本代理白名单，见报告 §六·2）。
//
// 判据（第一权威 = `d:/2/tools/decomp_full/`，逐句亲读 2026-10-29）：
//   · 账 `:994` → `CardTraitCollection__GetTraitFromAbilityTrigger.c:1-58`
//     （`param == 0 ⇒ return 10` · `param == 0x50(80) ⇒ 120` · `param == 0x51(81) ⇒ 0x3f2(1010)` ·
//      其余 `Enum.TryParse<DefinedTrait>(lowerFirst(Enum.ToString(param)))`，失败回 0）
//   · 账 `:1217` → `RawCardScript__GetAbilitiesForTriggerRawcard.c`（按 `cardAbilities[].trigger` 过滤出**列表**）
//   · 账 `:1254` → `RawCardScript__IsCumulativeMutation.c:17-47`
//     （`cardAbilities(+0x290) → abilityLogic(+0x50) → buffEffect(+0x50) → mutationType(+0x80) == 0x14(20)`）
//     唯一读点 = `CardScript__HasSingleUseMutation.c`（`IsCumulativeMutation` 为真 ⇒ 返 0；
//     否则扫 `activeEffects(+0x108)` 的 `enchantment(+0x88)` 有没有同一张 ⇒ 有返 1）
//   · `DefinedTrait` 枚举（134 个成员）= `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs` **逐个照抄**
//     （与 `CardDef.AbilityTriggerNumberOfName` 同一套「抄一份枚举表」的理由：那个类型在 `d:/2` 的桩里、
//      不在 `Assets/`，引擎里没有 C# 类型可用）
using System.Collections.Generic;

namespace RuleEngine
{
    public partial class CardDef
    {
        // ==========================================================================================
        //  一、原版 `DefinedTrait` 枚举名 → 数值（134 条，逐条转写自 `DefinedTrait.cs`）
        // ==========================================================================================
        //  ⚠️ 比较器用 **Ordinal（大小写敏感）** —— 原版 `Enum.TryParse<DefinedTrait>(s)` 的默认行为
        //     就是大小写敏感；换成 IgnoreCase 会把 `Rally` / `rally` 混成一档（与原版不一致）。
        //  🔴 这张表**只服务** `GetTraitFromAbilityTrigger` 的「名字对名字」那一跳。
        //     ⛔ 别拿它当「卡上有哪些 trait」的判据 —— 那是 `_keywords`（卡面解析结果）。
        static readonly Dictionary<string, int> DefinedTraitNumberOfName =
            new Dictionary<string, int>(System.StringComparer.Ordinal)
        {
            { "none", 0 },             { "ephemeral", 5 },             { "rally", 10 },             { "sneakAttack", 20 },             { "fast", 40 },             { "stealth", 70 },
            { "shield", 80 },             { "stun", 100 },             { "syphon", 110 },             { "backlash", 120 },             { "jam", 130 },             { "ward", 140 },
            { "wardWb", 145 },             { "cantAttack", 150 },             { "tainted", 160 },             { "unstoppable", 180 },             { "coward", 190 },             { "poisoned", 200 },
            { "bloodThirst", 220 },             { "dropPod", 230 },             { "dodge", 240 },             { "unstunnable", 250 },             { "slay", 260 },             { "rage", 280 },
            { "doubleRage", 300 },             { "invulnerable", 310 },             { "corrupt", 320 },             { "markOfChaos", 330 },             { "giantKiller", 340 },             { "blast", 350 },
            { "berzerk", 360 },             { "maintenance", 370 },             { "poisonous", 380 },             { "flying", 390 },             { "activeAbility", 400 },             { "psychicAbility", 405 },
            { "plasmaAbility", 407 },             { "passiveAbility", 410 },             { "whileInPlayAbility", 420 },             { "survivor", 430 },             { "survivorSpent", 440 },             { "relentless", 450 },
            { "transient", 455 },             { "flank", 460 },             { "sacrifice", 470 },             { "vulnerable", 480 },             { "reload", 490 },             { "notCollectable", 500 },
            { "secret", 510 },             { "terror", 520 },             { "trap", 530 },             { "secretOrder", 540 },             { "secretIdentity", 550 },             { "fear", 570 },
            { "meltdown", 580 },             { "massive", 590 },             { "resistant", 600 },             { "pack", 610 },             { "psyker", 620 },             { "reactor", 625 },
            { "firstStrike", 630 },             { "precognition", 640 },             { "swarm", 650 },             { "psychicPower", 660 },             { "plasmaPower", 665 },             { "sentinel", 670 },
            { "frenzied", 680 },             { "spikes", 700 },             { "horns", 710 },             { "daemonhost", 720 },             { "summonRift", 730 },             { "quest", 750 },
            { "questCompleted", 755 },             { "requiem", 760 },             { "landing", 770 },             { "mutation", 780 },             { "chaosOnly", 790 },             { "chooseOne", 820 },
            { "resolution", 840 },             { "talent", 845 },             { "bastion", 850 },             { "mission", 860 },             { "nonCombatant", 880 },             { "piercing", 900 },
            { "tide", 910 },             { "mob", 920 },             { "concussive", 930 },             { "large", 940 },             { "mekaniak", 950 },             { "support", 960 },
            { "pindown", 970 },             { "blind", 975 },             { "vanguard", 980 },             { "camouflage", 985 },             { "sniper", 990 },             { "codex", 1000 },
            { "unstable", 1010 },             { "armour", 1020 },             { "regeneration", 1030 },             { "artifice", 1040 },             { "remnant", 1050 },             { "markOfKhorne", 1060 },
            { "markOfNurgle", 1070 },             { "markOfSlaanesh", 1080 },             { "markOfTzeentch", 1090 },             { "daemonicAllegiance", 1100 },             { "strike", 1110 },             { "combo", 1130 },
            { "shuriken", 1120 },             { "waystone", 1140 },             { "synapse", 1150 },             { "longrange", 1160 },             { "markerlight", 1170 },             { "companion", 1180 },
            { "pray", 1185 },             { "notDestroyRemnant", 1190 },             { "ambush", 1200 },             { "uprising", 1210 },             { "duty", 1220 },             { "regiment", 1225 },
            { "teleport", 1230 },             { "agenda", 1235 },             { "stomp", 1240 },             { "stimulation", 1245 },             { "ecstasy", 1250 },             { "cruelty", 1255 },
            { "huntMark", 1260 },             { "ferocity", 1265 },             { "dontReturnFerocity", 1270 },             { "oath", 1275 },             { "oathInAllTurns", 1276 },             { "oathDouble", 1277 },
            { "oathTripleActivation", 1278 },             { "sentry", 1280 },
        };

        /// <summary>反查表（数值 → 枚举名），由上面那张表**懒构建**（数值唯一，先到先得）。
        /// ⛔ 不另抄一份名字表（两处写同一条规则，迟早不一致）。</summary>
        static Dictionary<int, string> _definedTraitNameByNumber;

        /// <summary>原版 `Enum.TryParse&lt;DefinedTrait&gt;(name)` 的等价物：**大小写敏感**。
        /// 命中 ⇒ 枚举数值；认不出 / 空 ⇒ `0`（= `DefinedTrait.none`，与原版「解析失败回 0」同义）。</summary>
        public static int DefinedTraitNumber(string name)
        {
            int v;
            return (!string.IsNullOrEmpty(name) && DefinedTraitNumberOfName.TryGetValue(name, out v)) ? v : 0;
        }

        /// <summary>`DefinedTrait` 数值 → 枚举名（`null` = 这个数值不在枚举里）。
        /// 用途：表现层把 trait 数值折回我们那套「小写驼峰关键词名」。</summary>
        public static string DefinedTraitName(int value)
        {
            if (_definedTraitNameByNumber == null)
            {
                var d = new Dictionary<int, string>(DefinedTraitNumberOfName.Count);
                foreach (var kv in DefinedTraitNumberOfName)
                    if (!d.ContainsKey(kv.Value)) d[kv.Value] = kv.Key;
                _definedTraitNameByNumber = d;
            }
            string s;
            return _definedTraitNameByNumber.TryGetValue(value, out s) ? s : null;
        }

        /// <summary>`DefinedTrait` 表规模（自检钉「整表被抄漏」用）。</summary>
        public static int DefinedTraitCount { get { return DefinedTraitNumberOfName.Count; } }

        // ==========================================================================================
        //  二、原版 `AbilityTrigger` 数值 → 枚举名（**反查**，唯一的正表仍是 `AbilityTriggerNumberOfName`）
        // ==========================================================================================
        static Dictionary<int, string> _abilityTriggerNameByNumber;

        /// <summary>原版 `Enum.ToString(param)` 的等价物（`null` = 这个数值不在枚举里）。
        /// 数据源 = `CardDef.AbilityTriggerNumberOfName`（97 条，已与 `AbilityTrigger.cs` 97/97 对拍过）——
        /// 本方法只是它的**反向视图**，⛔ 不另抄一份名字表。</summary>
        public static string AbilityTriggerNameOf(int abilityTrigger)
        {
            if (_abilityTriggerNameByNumber == null)
            {
                var d = new Dictionary<int, string>(AbilityTriggerNumberOfName.Count);
                foreach (var kv in AbilityTriggerNumberOfName)
                    if (!d.ContainsKey(kv.Value)) d[kv.Value] = kv.Key;
                _abilityTriggerNameByNumber = d;
            }
            string s;
            return _abilityTriggerNameByNumber.TryGetValue(abilityTrigger, out s) ? s : null;
        }

        // ==========================================================================================
        //  三、账 `:994` · 原版 `CardTraitCollection.GetTraitFromAbilityTrigger(AbilityTrigger)`
        // ==========================================================================================
        /// <summary>原版 `CardTraitCollection.GetTraitFromAbilityTrigger(AbilityTrigger)`（`+0x0`）——
        /// **把 trigger 折成 trait 数值**（不是名字）。
        ///
        /// <para>逐句照 `CardTraitCollection__GetTraitFromAbilityTrigger.c:1-58`：
        /// `0(ThisCardPlayed) ⇒ 10 rally` · `0x50(80 ThisUnitDeath) ⇒ 0x78(120) backlash` ·
        /// `0x51(81 Unstable) ⇒ 0x3f2(1010)` · 其余 = `Enum.TryParse&lt;DefinedTrait&gt;(lowerFirst(Enum.ToString(param)))`，
        /// 失败回 `0 none`；但 `param` 不在 `AbilityTrigger` 枚举里时 `Enum.ToString` 回的是**十进制串**、`Enum.TryParse` **照样接受**（不校验成员身份）⇒ 直接回原数值（⛔ 不是 0）。</para>
        ///
        /// <para>🔴 **三个硬编码必须在 `Enum.TryParse` 之前**：`thisCardPlayed` / `thisUnitDeath` 都不是
        /// `DefinedTrait` 的成员（原版靠那三个 switch 支兜），⛔ 别「顺手删掉硬编码只留名字解析」——
        /// 那会把 `rally` 与 `backlash` 折成 none（正好是最常见的两条）。</para>
        ///
        /// <para>现读名义（97 个 trigger）：能折出 trait 的 = **32**（29 同名 + 3 硬编码），其余 65 个 ⇒ 0。
        /// 消费者 = `CardPresentation/Battle/AbilityIconAnim.cs` 的图标脉冲（⚠️ 它现在是**同一规则的旧副本**）。</para></summary>
        public static int GetTraitFromAbilityTrigger(int abilityTrigger)
        {
            // ---- 三个硬编码（判据 `.c` 的三个 switch 支，逐字照抄）----
            if (abilityTrigger == 0) return 10;      // ThisCardPlayed → DefinedTrait.rally
            if (abilityTrigger == 80) return 120;    // ThisUnitDeath  → DefinedTrait.backlash
            if (abilityTrigger == 81) return 1010;   // Unstable       → DefinedTrait.unstable

            // ---- 其余：名字对名字（`lowerFirst(Enum.ToString(param))` → `Enum.TryParse<DefinedTrait>`）----
            string name = AbilityTriggerNameOf(abilityTrigger);
            if (string.IsNullOrEmpty(name)) return abilityTrigger;   // 原版 `Enum.ToString(未定义值)` = 十进制串 ⇒ `Enum.TryParse` 照样接受、直接回该数（⛔ 别写 0）
            string camel = char.ToLowerInvariant(name[0]) + name.Substring(1);
            return DefinedTraitNumber(camel);
        }

        /// <summary>名字版（原版调用方手上拿的是枚举，我们很多路径手上是**名字**）：
        /// 先按 `AbilityTrigger` 名 → 数值（认不出 ⇒ `0`），再走数值版。
        /// ⛔ 别在这里另写一套 switch —— 唯一判据是上面的数值版。</summary>
        public static int GetTraitFromAbilityTrigger(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) return 0;
            int n = AbilityTriggerNumber(triggerName);
            if (n < 0) return 0;
            return GetTraitFromAbilityTrigger(n);
        }

        // ==========================================================================================
        //  四、账 `:1217` · 原版 `RawCardScript.GetAbilitiesForTriggerRawcard(AbilityTrigger)`
        // ==========================================================================================
        /// <summary>原版 `RawCardScript.GetAbilitiesForTriggerRawcard(AbilityTrigger)` —— 按
        /// `cardAbilities[].trigger`（`+0x10`）过滤出**该 trigger 的全部 ability**（返回**列表**，
        /// 同一 trigger 可以有多条）。
        ///
        /// <para>我们的等价物 = `AbilityTargets` 里 `Trigger == 该值` 的那些行（**下标 = `cardAbilities` 下标**）。
        /// 🔴 与 <see cref="UniqueAbilityIndexForTrigger(string, out string)"/> 的分工：
        /// 那条是「**唯一**下标」口（不唯一 ⇒ 返回 -1 + 出声），本方法是原版的**列表**形状
        /// （原版压根不要求唯一）。两者扫的是**同一列** `AbilityTargets`，⛔ 别再造第三处扫描。</para>
        ///
        /// <para>三态（⛔ 别合并）：`null` = **判不了**（`abilityTargets` 列没落 / 旧表 / 名字认不出）·
        /// 空表 = **明确没有**这一 trigger 的 ability · 非空 = 命中列表（按 `cardAbilities` 下标原序）。</para></summary>
        public IReadOnlyList<AbilityTargetSpec> AbilitiesForTrigger(int abilityTrigger)
        {
            if (!AbilityTargetsVersioned) return null;          // 列没落 ⇒ 未知
            var list = new List<AbilityTargetSpec>();
            for (int i = 0; i < _abilityTargets.Count; i++)
            {
                var s = _abilityTargets[i];
                if (s != null && s.Trigger == abilityTrigger) list.Add(s);
            }
            return list;
        }

        /// <inheritdoc cref="AbilitiesForTrigger(int)"/>
        public IReadOnlyList<AbilityTargetSpec> AbilitiesForTrigger(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) return null;
            int n = AbilityTriggerNumber(triggerName);
            if (n < 0) return null;                             // 认不出名字 ⇒ 判不了（⛔ 不当「没有」）
            return AbilitiesForTrigger(n);
        }

        // ==========================================================================================
        //  五、账 `:1254` · 原版 `RawCardScript.IsCumulativeMutation()`（**休眠实现**：卡表今天无该列）
        // ==========================================================================================
        //  判据：`RawCardScript__IsCumulativeMutation.c:17-47` —— 遍历
        //    `cardAbilities(+0x290)` → `abilityLogic(+0x50)` → `buffEffect(+0x50)`，
        //    任一 `buffEffect.mutationType(+0x80) == 0x14(20 Cumulative)` ⇒ 真。
        //  唯一读点：`CardScript__HasSingleUseMutation.c`（`this` 的 `activeEffects(+0x108)` 里
        //    有没有 `enchantment(+0x88)` 正是这张变异卡；`IsCumulativeMutation(变异卡)` 为真时**直接返 0**
        //    ⇒ 累计型变异**不参与**「单次变异已挂过」的去重）。上游闸 = `AbilityLogic.GetTargets`
        //    在 `criteria.traitsFilter == 780 (mutation)` 时用它剔除已挂过同一条单次变异的卡。
        //
        //  🔴 **为什么是休眠实现**：`cards_truth.json` / `cards_engine.json` **都没有 `mutationType` 列**
        //     （`cards_raw.json` 单份现扫：`buffEffect.mutationType` 1,873 条**值全 0**；`MutationType`
        //      枚举 = `none 0 / Normal 10 / Cumulative 20`）⇒ 池内**没有任何一张卡**会命中 `== 20`。
        //     ⇒ `AttachAbilityMutations` 今天无人调用、本属性恒走「未知」档。
        //     ⛔ **不是判 D**：数据一变即可达（铁律 11：休眠 ⇒ 建出来、账记休眠）。

        /// <summary>原版 `MutationType` 枚举（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/MutationType.cs`，3 个成员）。</summary>
        public const int MutationNone = 0;
        /// <inheritdoc cref="MutationNone"/>
        public const int MutationNormal = 10;
        /// <inheritdoc cref="MutationNone"/>
        public const int MutationCumulative = 20;

        /// <summary>一行 `abilityLogic[].buffEffect.mutationType`（原版 `CardEffect.mutationType` `+0x80`）。
        /// `MutationType = -1` = 该格坏值/缺键（⛔ 不是 `0 none`）。</summary>
        public sealed class AbilityMutationSpec
        {
            /// <summary>`cardAbilities` 下标（`-1` = 坏格）。</summary>
            public int Ab = -1;
            /// <summary>`abilityLogic` 下标（`-1` = 坏格）。</summary>
            public int Lg = -1;
            /// <summary>原版 `buffEffect.mutationType`（`0 none` / `10 Normal` / `20 Cumulative`）；`-1` = 未知。</summary>
            public int MutationType = -1;
        }

        AbilityMutationSpec[] _abilityMutations;

        /// <summary>`abilityMutations` 列**来自卡表**的正面信号（`null` = 整列**未知** / 旧表 / 手写夹具）。
        /// ⛔ 与「明确没有（`[]`）」不是一回事 —— 同 `HasPermanentBuffs` / `HasAbilitySummon` 的口径。</summary>
        public bool HasAbilityMutations { get { return _abilityMutations != null; } }

        /// <summary>由 `CardDatabase.Parse` 在**构造之后**挂载（与 `AttachPermanentBuffs` / `AttachAbilitySummon` 同族；
        /// 理由 = `CardDef` 构造函数上那四十处账落点，⛔ 不往它加形参）。`null` = 旧表（未知）。</summary>
        public void AttachAbilityMutations(AbilityMutationSpec[] rows)
        {
            _abilityMutations = rows;
        }

        /// <summary>全部行（`null` = 未知）。消费端一般只要 <see cref="MutationTypeAt"/> / <see cref="IsCumulativeMutation"/>。</summary>
        public AbilityMutationSpec[] AbilityMutations { get { return _abilityMutations; } }

        /// <summary>第 <paramref name="ability"/> 条第 <paramref name="logic"/> 行的 `mutationType`。
        /// `-1` = 没有这一行 / 旧表 / 坏格（⛔ 不是 `0 none`）。</summary>
        public int MutationTypeAt(int ability, int logic)
        {
            if (_abilityMutations == null || ability < 0 || logic < 0) return -1;
            for (int i = 0; i < _abilityMutations.Length; i++)
            {
                var s = _abilityMutations[i];
                if (s != null && s.Ab == ability && s.Lg == logic) return s.MutationType;
            }
            return -1;
        }

        /// <summary>原版 `RawCardScript.IsCumulativeMutation()` —— 这张卡**有没有**一条
        /// `buffEffect.mutationType == 20 (Cumulative)` 的效果行。
        ///
        /// <para>⚠️ 原版签名是 `bool`（没有「未知」档）⇒ 本方法在**列未知**时返 `false`
        /// （= 原版「扫不到就是 false」的形状）。要区分「未知」请先看 <see cref="HasAbilityMutations"/>。</para>
        /// <para>🔴 反例提醒：原版**只认 `== 20`**，`10 Normal` 与 `0 none` 都返 false
        /// （⛔ 别把「有 buffEffect」当成「累计型」）。</para></summary>
        public bool IsCumulativeMutation()
        {
            if (_abilityMutations == null) return false;
            for (int i = 0; i < _abilityMutations.Length; i++)
            {
                var s = _abilityMutations[i];
                if (s != null && s.MutationType == MutationCumulative) return true;
            }
            return false;
        }
    }
}
