// GivePayload.cs — `give` / `gain` / `lose` 的**载荷**怎么解释（属性增减益 还是 关键词授予）
//
// **权威语义来源**：`d:/warpforge/scripts/rule_core.gd` 的 `_apply_gain`（`:3266`）+ `GIVE_KW`（`:2408`）。
//
// 为什么单独一个文件：448 张战术卡里 `give X to Y` 有 **164 个分句、X 去重 78 个**，
// 而原版**不是 78 个独立实现** —— 全都走 `_apply_gain` 一个函数 + 一张 54 条的 `GIVE_KW` 前缀表。
// 照这个结构办：**一个入口，一张表**，78 个载荷一起通。
//
// `_apply_gain` 干四件事（`:3266`）：
//   ① **多属性拆分** —— 先按 `, ` 再按 ` and ` 递归（Autarch `"+1 melee, +1 ranged and +1 Health"` → 三段）
//   ② **关键词授予** —— `GIVE_KW` 前缀匹配；值 = 载荷里第一个数字，没有就是 1
//      （`armour` / `shield` / `stun` 三个还要**同步状态字段**，见 `RuleCore.ApplyGain`）
//   ③ **属性增减益** —— 一条正则吃下所有数值型载荷
//      `([+-]?\d+)\s+(ranged attack|attack|ranged|health|armor|melee|might|fist|strength)\b`
//   ④ **限时登记** —— 带时长的记进 `temp_buffs`，回合结束按 `end` 移除
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。解析失败**不打印**，由调用方暴露。
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RuleEngine
{
    /// <summary>载荷里的一项：要么是**属性增减益**，要么是**关键词授予**。</summary>
    public class PayloadOp
    {
        /// <summary>属性增减益的目标字段：`attack` / `health` / `armour` / `ranged`。
        /// 关键词授予时为 null</summary>
        public string Attr;
        /// <summary>规范关键词名（`KeywordTable` 那套）。属性增减益时为 null</summary>
        public string Keyword;
        /// <summary>增减量（关键词授予时 = 关键词的 X 值，没有数字则 1）</summary>
        public int Value;
        /// <summary>
        /// **变体名**。目前只有 `Dark Pact of X` 这一族用（`blood` / `excess` / `fate` / `resilience`）——
        /// 规则书 :179 那四种契约的效果**互不相同**，不记下来就只能随机给一种。
        /// 出处：`rule_core.gd:1655` 的 `DARK_PACT_FX` 表 + `:1696` 那条
        /// `give (?:a )?(?:random )?dark pact(?:\s+of\s+([a-z]+))?` 正则。
        /// </summary>
        public string Variant;
        /// <summary>
        /// 这一项不是关键词、也不是属性 —— 是**一整段效果文字**
        /// （`Give "💀 Backlash: Return to your hand" to a friendly troop`）。
        /// 内容形如 `backlash: Return to your hand`（**小写**的 `关键词: 效果原文`）。
        /// 谁来解释它见 `EffectResolver.GrantEmbeddedAbility`。
        /// </summary>
        public string Embedded;
        /// <summary>原文那一小段（排查用）</summary>
        public string Source;
        /// <summary>
        /// 🆕 **2026-09-16**：这一项是**代词 `it`** —— 指的是**事件带过来的那份黑暗契约**，
        /// 不是「随机一份」。
        ///
        /// 出处：`Accursed Helbrute`（`BL74`）卡面
        /// `When a friendly troop receives a ✦Dark Pact, this troop gains it as well`
        /// —— 那个 `it` 就是前面那句里的 `a Dark Pact`。
        ///
        /// 🔴 **判据必须窄到「剥完噪声后整段恰好等于 `it as well`」** ——
        ///    全池 11 处 `as well` 里只有这一处是代词，其余都是
        ///    `give it <具体载荷> as well`（那些照走属性/关键词那两条路，别一起收）。
        /// ⚠️ `rule_core.gd` 全文 `grep "as well"` = **0 命中** ⇒ 这张卡参考实现也没做，
        ///    这个形状是**我们自己定的**，不是照抄（如实记，别当成原版语义）。
        /// </summary>
        public bool CopyEventPact;
        /// <summary>
        /// 🆕 **2026-09-16：这一段「本版不认识」** —— 它**没有**被解释成属性/关键词，
        /// 但**已经留在 `ops` 里了**，专门用来让「**丢了半句**」这件事**看得见**。
        ///
        /// 🔴 **为什么不是「一段不认识就整条判失败」**（实测，不是推断）：
        ///    真正受影响的只有 **4 张卡** —— `UM58`（丢 `1 health`）· `GOF_Worst_Temper`（丢
        ///    `wings flying`）· `TAU74`（丢 `+1 power`）· `DA21`（丢 2 点任务）。而改成
        ///    「一段不认识 ⇒ 整条 `null`」会把**现在还能用的那半一起打掉**
        ///    （`UM58` 从「+1近战 / +1远程」变成**一点不给**）——
        ///    **用缩水换可见，不划算**。本工程对 `or` 句已经定过同一个口径
        ///    （`EffectText.TryEitherOr`：失败时退回原路，不把认不出的判成半懂）。
        ///    ⇒ **保留 `any`，但让丢段有声**（卡面 `*` + 覆盖率 + 日志三处同时亮）。
        /// 影响面与实测脚本：`_tmp_view/seg_any_probe_out.txt`（正本 `静默桩家族_0916.md` §四）。
        /// </summary>
        public bool Unresolved;
        /// <summary>
        /// 🆕 **2026-09-16：这一项不是「给单位」的，是「给玩家自己」的阵营资源** ——
        /// 取值 <c>"faith"</c> / <c>"quest"</c> / <c>"spirit"</c>。
        ///
        /// 出处：`Inner Circle Companion`（`DA21`）卡面
        ///   `👁Stealth. 🌿Agenda: Gain 🛡Vanguard and ⌾2` —— 一条 `gain` 里**混了两种接受者**：
        ///   「给**这张单位** Vanguard」+「给**玩家自己** +2 任务点」。
        ///   原来 `2 quest points` 那一段 `GivePayload` 认不出 ⇒ 打 `Unresolved`（多段里丢一段）。
        ///
        /// 🔴 **结算在 `RuleCore.DoGive` 的目标循环之前单独做一次**（见那里的注释）——
        ///   **别放进 `ApplyOneGain`**：那是**逐目标**调的 ⇒ 每个目标各加一次，
        ///   而且会绕掉 `QuestPointThreshold` 与 `When you gain [Quest Point]` 广播。
        /// </summary>
        public string Resource;

        public bool IsKeyword { get { return Keyword != null; } }

        public bool IsEmbedded { get { return Embedded != null; } }

        public override string ToString()
        {
            if (CopyEventPact) return "事件里那份黑暗契约";
            if (IsEmbedded) return "嵌入效果(" + Embedded + ")";
            string v = string.IsNullOrEmpty(Variant) ? "" : "(" + Variant + ")";
            return IsKeyword ? Keyword + v + " " + Value : (Value >= 0 ? "+" : "") + Value + " " + Attr;
        }
    }

    public static class GivePayload
    {
        /// <summary>
        /// `GIVE_KW` —— 关键词前缀表，**逐条照抄 `rule_core.gd:2408`（54 条）**。
        /// ⚠️ **顺序有意义**（前缀匹配，先命中先用）：多词变体排在单词前面，
        ///    `blood thirst` 排在 `bloodthirst` 前、`long range` 排在 `longrange` 前。
        /// 🆕 **2026-09-14：本表有 1 条是「照卡面补原版的漏」**（`blind`，见下面那行的说明）——
        ///    已经不是纯粹的「逐条照抄」了，所以这里改口径：**照抄为主，补漏逐条标出处**。
        /// </summary>
        public static readonly string[][] GiveKw =
        {
            new[] { "blood thirst", "bloodthirst" }, new[] { "long range", "longrange" },
            new[] { "hunt mark", "huntmark" }, new[] { "dark pact", "darkpact" },
            new[] { "shuriken", "shuriken" }, new[] { "vulnerable", "vulnerable" },
            // 🆕 2026-09-14 A6 族 C：**`blind` 原版的 `GIVE_KW` 里没有** —— 参考实现
            //    （`rule_core.gd:2408` 那张 54 条的表）逐条比对过，确实缺这一个。
            //    代价：`Deal 2 damage to all units and give them Blind until your next turn`
            //    （`Fenrisian Blizzard`）那 6 张的「失明」**静默不发生**（卡面打着、引擎不认）。
            //    `blind` 本身**早就实现了**（`KeywordTable.Implemented` 里有，`DoBlind` / `UnitState.IsBlind`
            //    都在，规则书 `:166`），只是没人从**载荷**这条路给过它 ⇒ 这里是**照卡面补原版的漏**，
            //    和下面「裸 `+N`」那一段是同一条先例。
            new[] { "blind", "blind" },
            new[] { "armour", "armour" }, new[] { "armor", "armour" },
            new[] { "camouflage", "camouflage" }, new[] { "flying", "flying" },
            new[] { "stealth", "stealth" }, new[] { "vanguard", "vanguard" },
            new[] { "flank", "flank" }, new[] { "pindown", "pindown" },
            new[] { "invulnerable", "invulnerable" }, new[] { "regeneration", "regeneration" },
            new[] { "shield", "shield" }, new[] { "blast", "blast" },
            new[] { "stun", "stun" }, new[] { "waystone", "waystone" },
            new[] { "sentry", "sentry" }, new[] { "fast", "fast" },
            new[] { "rally", "rally" }, new[] { "slay", "slay" }, new[] { "strike", "strike" },
            new[] { "stomp", "stomp" }, new[] { "unstable", "unstable" },
            new[] { "concussive", "concussion" }, new[] { "tide", "tide" },
            new[] { "markerlight", "markerlight" }, new[] { "oath", "oath" },
            new[] { "agenda", "agenda" }, new[] { "ecstasy", "ecstasy" },
            new[] { "companion", "companion" }, new[] { "teleport", "teleport" },
            new[] { "swarm", "swarm" }, new[] { "synapse", "synapse" },
            new[] { "mob", "mob" }, new[] { "pack", "pack" }, new[] { "regiment", "regiment" },
            new[] { "uprising", "uprising" }, new[] { "duty", "duty" },
            new[] { "ferocity", "ferocity" }, new[] { "pray", "pray" },
            new[] { "codex", "codex" }, new[] { "destroyer", "destroyer" },
            new[] { "talent", "talent" }, new[] { "sniper", "sniper" },
            new[] { "penitence", "penitence" }, new[] { "sabotage", "sabotage" },
            new[] { "cruelty", "cruelty" }, new[] { "bloodthirst", "bloodthirst" },
            new[] { "cantattack", "cantattack" }, new[] { "can't attack", "cantattack" },
        };

        // 🔴 **故意不在表里的词**（下一个会话别再挖一遍）：
        //   · `a kustom job of your choice`（`Mekaniak`，Goff 天赋，全池只 1 处）——
        //     「Kustom Job」是什么**三层权威全都没有**：规则书没这个词、参考实现
        //     （`rule_core.gd`）里 0 命中、`d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 与
        //     反编译 `.c` 里也是 0 命中，卡池里**没有**任何叫 `Kustom Job …` 的卡。
        //     成品卡图（`Orks/2天赋/Warpforge_25B_Mekaniak.png`，照铁律 7 亲读）也只印着这一句。
        //     ⇒ **如实报「载荷词表里没有」**，不猜一种效果顶上去（猜 = 静默错一张）。
        //   · `a remnant it gains shield`（`Undying Legions`）**不是词表缺词**，
        //     是那句 `For the rest of this battle, when …` 的**常驻监听**没接上（条件从句被当了主语）。
        //     见 `资料/常驻效果_数据与设计.md`。

        /// <summary>属性增减益那条正则 —— **照抄 `rule_core.gd:3311`**。
        /// ⚠️ `might` / `fist` / `strength` 是**原版图标语义**（攻击强化 / 拳头图标），
        ///    都归到 `attack`（原版注释里标了出处：`Perfection` 机制 / `Enhanced Musculature`）。
        ///    `ranged attack` 要排在 `ranged` 前面，否则前缀短的那个先命中。
        ///
        /// 🆕 `weapon`（2026-09-14 A7）：**也是图标语义，归 `ranged`**。
        ///    它是数据管线给**紫圈枪图标**起的一个 token 名（图集里**没有** `weapon` 这张图，
        ///    只有 `Atlas_trait_icon_Melee/Ranged`）。三张卡**逐张照成品卡图核过**（铁律 7），
        ///    图标一律是**紫圈枪**：
        ///      · `Orks/3部队/Warpforge_16_Banner-Nob.png` —— `+1【拳】 and +1【枪】`
        ///      · `Sorotitas/4计策/Warpforge_44_Beacon-of-Faith.png` —— `+1【拳】 and +1【枪】`
        ///      · `Emperor_s Children/3部队/Warpforge_29_Malgarash-the-Adamant.png` —— `+1【枪】`
        ///    ⚠️ **别把它归成近战**：它总和 `[attack]`（拳）**成对**出现，归错了就是
        ///      「两个属性都加在近战上」——数值看着对得上、远程静默少加。
        ///    ⚠️ 还有一处**同名不同义**的坑（**没动它**，只记下来）：`[Armor]`/`[Armour]`
        ///      在 `+N Attack … +N Armour` 这个固定搭配里**也是枪**（5 张卡图核过），
        ///      而真正的护甲卡面一律写**裸词** `Armour 1`（不带方括号）。那是**数据侧**的错，
        ///      要修得走 `cardface_fixes.json`，**不是**在这里放宽词表（放宽会同时打到真护甲）。
        /// </summary>
        static readonly Regex ReAttr = new Regex(
            @"([+-]?\d+)\s+(ranged attack|attack|ranged|health|armor|armour|melee|might|fist|strength|weapon)\b",
            RegexOptions.Compiled);

        static readonly Regex ReNumber = new Regex(@"(\d+)", RegexOptions.Compiled);

        /// <summary>
        /// 🆕 **2026-09-16：整个载荷就是裸 `N &lt;属性&gt;`**（`1 Health` / `1 Armour`）。
        ///
        /// 🔴 为什么单开一条：下面的 `ReLeadingNoise` 会把**开头的数字**当成「数量噪声」剥掉
        ///   ⇒ 剥完只剩光秃秃的 `health`，而 **`health` 不在 `GiveKw` 表里** ⇒ 整段判「不认识」。
        ///   实测（探针量过）：`UM58 Knights of Macragge` 的 `… and 1 Health to your troops`
        ///   那半句**从来不给**。⇒ 和 ④ 能量同一个道理，**必须排在剥噪声之前**。
        /// ⚠️ 锚在 `^…$`：**只有整个载荷就是这一个形式**才收，别放宽成子串匹配。
        /// 全池实测只有 **3 处**这个写法（`UM58` · `DA37` · `EC53`），后两处不是载荷
        /// （`EC53` 是条件句 `if it has 0 Ranged Attack`，归 `rangedzero`）。
        /// </summary>
        static readonly Regex ReNumAttr = new Regex(
            @"^(\d+)\s+(ranged attack|attack|ranged|health|armor|armour|melee|might|fist|strength|weapon)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 🆕 **2026-09-16：整段载荷就是「给玩家自己的阵营资源」**（`2 Quest Points` / `2 Spirit Stones` / `1 ☀`）。
        ///
        /// 🔴 为什么单开一条：**必须排在剥开头数字之前**（同 ④ 能量那条）——
        ///   否则那个 `2` 会被 `ReLeadingNoise` 当「数量」剥掉、剩下光秃秃的 `quest points`，
        ///   一段都解不出来。出处：`Inner Circle Companion` 的 `Gain Vanguard and 2 Quest Points`
        ///   （那一段混在 `gain` 的多段里，走 `EffectText.TryGain` 那条资源正则要求**整条全等**、必然失配）。
        /// ⚠️ 锚在 `^…$`：只有**整段就是这一个**才收。
        /// ⚠️ 这条只管**多段里混着资源**的那种写法；整条载荷就是资源的（全池 62 处）
        ///   早在 `EffectText.TryGain` 里被接住了，两处**互不冲突**（那边先拦）。
        /// </summary>
        static readonly Regex RePureResource = new Regex(
            @"^\+?(\d+)\s*(?:additional\s+)?(spirit stones?|waystones?|quest\s*points?|faith|☀)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>阵营资源词 → 引擎的名字（`spirit` / `quest` / `faith`）——
        /// `RuleCore.DoFactionResource` 读的就是这三个。**只此一处**。</summary>
        static string NormResource(string w)
        {
            if (string.IsNullOrEmpty(w)) return null;
            string s = w.ToLowerInvariant();
            if (s.Contains("spirit") || s.Contains("waystone")) return "spirit";
            if (s.Contains("quest")) return "quest";
            if (s.Contains("faith") || s.Contains("☀")) return "faith";
            return null;
        }

        /// <summary>属性词 → 本类的规范名（`ranged attack`/`weapon` → `ranged`；`might`/`fist`/`strength` → `attack`）。
        /// **只此一处** —— `ReAttr` 那一支与 `ReNumAttr` 那一支读同一份（两处各写一份迟早不一致）。</summary>
        static string NormAttr(string attr)
        {
            switch (attr)
            {
                case "ranged attack": case "ranged": return "ranged";
                case "weapon": return "ranged";       // 紫圈枪图标的 token 名（三张卡图核过，见 `ReAttr` 说明）
                case "might": case "fist": case "strength": return "attack";
                case "armor": return "armour";
                default: return attr;                 // `attack` / `health` / `melee` / `armour` 原样
            }
        }

        /// <summary>
        /// **图标名**（不是关键词）—— 卡面转录里写成方括号：`[Wings] Flying` · `[skull] Slay: …`。
        /// 括号里那个词指的是**卡面上印的图标**，**后面紧跟的才是真关键词**
        /// ⇒ 解析时**连内容一起丢掉**（`[Attack]` / `[Dark Pact]` / `[Stomp]` 那种**括号里就是关键词**的
        /// **不在此列**，那些照旧只脱括号、留内容）。
        ///
        /// 🔴 不丢的代价（实测）：`GOF_Worst_Temper` 卡面 `Your Warlord gains +1 [Attack],
        ///   [Shield] Armour 1 and **[Wings] Flying** this turn` —— `Wings` 挡在 `Flying` 前面，
        ///   `GiveKw` 从头匹配不到 `flying` ⇒ **`Flying` 那半句从来不生效**。
        /// ⚠️ 判据收窄到**这张显式表**，**别**改成「括号里只要不是关键词就丢」——
        ///   全池还有 `[1]`（数量标记）、`[Energy]`、`[faith]` 等写法，放宽会误伤。
        /// 全池方括号标记实测 39 种（2026-09-16 扫过），属于**图标名**的是这几个。
        /// </summary>
        static readonly string[] IconNames =
            { "wings", "skull", "icon", "eye icon", "codex icon", "faith icon" };

        /// <summary>`N energy` / `[N] energy` / **`+N energy`**（方括号是付费标记，`rule_core.gd:2519` 会剥掉）。
        /// 🔴 **2026-09-16 补 `\+?`**：`Chaplain Gabutheron`（`DA82`）卡面写的是 **`gain +1 Energy`**
        /// —— 带正号，原来这条正则**整段失配** ⇒ 那半句从来不给（`Agenda: Heal 4 … and gain +1 Energy`）。
        /// ⚠️ 正号在这里**没有**别的含义（能量只会加），所以放宽是安全的。</summary>
        static readonly Regex ReEnergy = new Regex(@"^\+?(\d+)\s*energy$", RegexOptions.Compiled);

        /// <summary>裸 `+N` / `-N`（后面跟的是**图标**，图标在文本导出里丢了）</summary>
        static readonly Regex ReBareSigned = new Regex(@"^([+-]\d+)$", RegexOptions.Compiled);

        /// <summary>
        /// 拆一条载荷 → 若干 <see cref="PayloadOp"/>。
        /// **返回 null = 认不出来**（调用方要如实报，不许当成「无效果」）。
        /// </summary>
        public static List<PayloadOp> Parse(string payload)
        {
            var ops = new List<PayloadOp>();
            if (!ParseInto(payload, ops)) return null;
            return ops.Count > 0 ? ops : null;
        }

        /// <summary>
        /// 载荷开头的**噪声词**：冠词 / 数量 / 方括号里掉出来的阵营名。
        /// 实测卡面里这些是 OCR + 数值表合并时混进来的，不是语义的一部分：
        ///   `Give a [Dark Pact] to all friendly troops`     → 方括号是**关键词标记**，剥掉
        ///   `Give a [Chaos] Dark Pact to a friendly troop`  → `Chaos` 是阵营名噪声
        ///   `Give 2 random Dark Pact to a friendly troop`   → `2` 是数量（这一版按一份给）
        /// 出处：原版 `rule_core.gd:1696` 那条只吃 `a` / `random`，覆盖不到这些；
        /// 我们**反复**剥到剥不动为止，多剥一层不多写一条特判。
        /// </summary>
        static readonly Regex ReLeadingNoise = new Regex(
            @"^(?:\d+|a|an|the|random|chaos|of|additional)\s+", RegexOptions.Compiled);

        /// <summary>
        /// 多属性拆分 + 逐段解释。返回「有没有认出至少一段」。
        ///
        /// 🆕 **2026-09-16：入口先剥两类「卡面上根本不存在」的噪声，原样失败时再降级重试一次。**
        ///   ① **HTML 标记**（`<i>Shuriken</i> 2`，`Inspiring Legacy`）—— 抄录残留，无条件剥掉。
        ///   ② **非 ASCII 图标**（`🛡 Shield`，`Sacred Altar` · `a ❄ Dark Pact of Excess`，
        ///      `Noise Marine`）—— 卡面印的是图标，我们的 `desc` 里剩了个 emoji 代理 ⇒
        ///      词表一条都匹配不上。
        /// 🔴 **只在「原样解不出来」时才剥第二次**，而且**只在没加进任何一项时才重试**
        ///    （多段拆分可能已经成功加了几段，重试会把它们**加第二遍**）。
        ///    ⇒ `-1 🗡 and -1 🔫`（`Lord Kakophonist`，**图标就是属性名**）剥完仍是 `-1 and -1`，
        ///      照样解不出来 ⇒ 如实判「不认识」。**宁可判不认识，也不猜**（本工程红线）。
        /// </summary>
        static bool ParseInto(string payload, List<PayloadOp> ops)
        {
            if (string.IsNullOrEmpty(payload)) return false;
            // ①-b **图标名连同方括号一起丢**（`[Wings] Flying` / `[skull] Slay: …`）—— 见 `IconNames`。
            //     🔴 **必须在脱 `[]` 之前**做：脱完就分不出「括号里是图标名」还是「括号里就是关键词」了。
            string w = payload;
            foreach (string icon in IconNames)
                w = Regex.Replace(w, @"\[\s*" + Regex.Escape(icon) + @"\s*\]\s*", " ",
                                  RegexOptions.IgnoreCase);
            w = w.Replace("[", "").Replace("]", "").Trim();
            if (w.Length == 0) return false;

            // ① HTML 标记 —— 抄录残留，卡面上不存在，无条件剥
            if (w.IndexOf('<') >= 0)
            {
                w = Regex.Replace(w, @"</?[a-zA-Z][^>]*>", " ").Trim();
                if (w.Length == 0) return false;
            }

            int before = ops.Count;
            if (ParseIntoCore(w, ops)) return true;
            if (ops.Count != before) return false;        // 已经加进去几段了 ⇒ 别重试（会重复）

            // ② 非 ASCII 图标 —— **只在这一刻**剥一次重试
            if (!HasNonAscii(w)) return false;
            string bare = Regex.Replace(w, @"[^\x20-\x7E]", " ");
            bare = Regex.Replace(bare, @"\s{2,}", " ").Trim();
            if (bare.Length == 0 || bare == w) return false;
            int before2 = ops.Count;
            if (ParseIntoCore(bare, ops)) return true;
            if (ops.Count != before2) return false;
            return false;
        }

        /// <summary>载荷里有没有非 ASCII 字符（图标代理）。</summary>
        static bool HasNonAscii(string s)
        {
            foreach (char ch in s) if (ch > 0x7E || ch < 0x20) return true;
            return false;
        }

        /// <summary>真正的那套判据 —— 见 <see cref="ParseInto"/>（它负责剥噪声与降级重试）。</summary>
        static bool ParseIntoCore(string w, List<PayloadOp> ops)
        {
            if (string.IsNullOrEmpty(w)) return false;

            // ④ 能量 —— `N energy` **要先于**噪声剥离判：`1 energy` 开头的 `1` 也是数字，
            //    剥掉就只剩 `energy`，再也匹配不上 `^(\d+)\s*energy$` 了（2026-09-12 撞到）。
            var meEarly = ReEnergy.Match(w);
            if (meEarly.Success)
            {
                ops.Add(new PayloadOp { Attr = "energy", Value = int.Parse(meEarly.Groups[1].Value), Source = w });
                return true;
            }

            // ④-b 裸 `N <属性>`（`1 Health`）—— **和 ④ 同一个道理，必须排在剥噪声之前**。
            //     见 `ReNumAttr` 的说明（不排在前面的话，那个数字会被当「数量」剥掉，
            //     剩下光秃秃的 `health`、而它不在 `GiveKw` 表里 ⇒ 整段判不认识）。
            var meNumAttr = ReNumAttr.Match(w);
            if (meNumAttr.Success)
            {
                ops.Add(new PayloadOp { Attr = NormAttr(meNumAttr.Groups[2].Value.ToLowerInvariant()),
                                        Value = int.Parse(meNumAttr.Groups[1].Value), Source = w });
                return true;
            }

            // ④-c 🆕 **纯资源段**（`2 Quest Points`）—— **同样必须排在剥噪声之前**，见 `RePureResource`。
            var meRes = RePureResource.Match(w);
            if (meRes.Success)
            {
                string res = NormResource(meRes.Groups[2].Value);
                if (res != null)
                {
                    ops.Add(new PayloadOp { Resource = res,
                                            Value = int.Parse(meRes.Groups[1].Value), Source = w });
                    return true;
                }
            }

            // 反复剥掉开头的噪声词，直到剥不动 —— 见 `ReLeadingNoise` 的出处说明。
            // ⚠️ 循环上限写死，避免某条正则意外自我循环（这种 bug 在批处理里表现为「跑不完」）。
            for (int guard = 0; guard < 8; guard++)
            {
                string stripped = ReLeadingNoise.Replace(w, "");
                if (stripped == w || stripped.Length == 0) break;
                w = stripped;
            }
            if (w.Length == 0) return false;

            // 🔴 **2026-09-16：段首的「图标名」也要剥** —— 上面那个**按方括号**剥的版本
            //   **只在顶层载荷有效**：多段拆分是**递归**调本函数的，而 `[` `]` 在**第一次**进来时
            //   就被脱掉了 ⇒ 递归进来时只剩 `wings flying`，方括号没了、那条正则匹配不上。
            //   **实测撞到（就是这条）**：`Worst Temper` 的 `[Wings] Flying` 被拆成独立一段
            //   `wings flying` ⇒ `GiveKw` 从头匹配不到 `flying` ⇒ 整段还是判不认识。
            //   做法：段首若是图标名、**直接丢掉那一个词**（见 `IconNames` —— 那几个词
            //   **永远不是**合法的载荷词，`wings` / `skull` / `icon` 都不在 `GiveKw` 里）。
            for (int guard = 0; guard < 4; guard++)
            {
                string wl = w.ToLowerInvariant();
                bool cut = false;
                foreach (string icon in IconNames)
                {
                    if (wl.Length > icon.Length + 1
                        && wl.StartsWith(icon + " ", System.StringComparison.Ordinal))
                    { w = w.Substring(icon.Length + 1).TrimStart(); cut = true; break; }
                }
                if (!cut) break;
            }
            if (w.Length == 0) return false;

            // ✅ **整条载荷一律转小写再解释**。原版收到的 `desc` 早就被 `_lower()` 过一遍了
            //    （`rule_core.gd` 的 `_resolve_text` 入口），而我们的调用方喂进来的是**原文大小写**
            //    （`Give +2 Melee Attack and Vanguard`）。2026-09-12 撞到：原先只在关键词那一支
            //    用 `ToLowerInvariant`，属性那支的正则吃着 `Melee` 这种大写**直接失配** ——
            //    表现是黑暗契约的四种增益**一条都没加上**（解析成功了、结算静默为空）。
            string low = w.ToLowerInvariant();

            // ②-c 🆕 **2026-09-16：代词载荷 `it as well`** —— 全池只有 `Accursed Helbrute` 一张。
            //     卡面 `When a friendly troop receives a ✦Dark Pact, this troop gains it as well`
            //     里的 `it` = **事件带过来的那份契约**（种类由 `PactOf(事件主语)` 取）。
            //     ⚠️ 判据**必须是整段相等**：全池 11 处 `as well` 里其余都是
            //        `give it <具体载荷> as well`，那些已经在下面各支里被正常收掉，
            //        放宽成 `Contains` 会把它们一起吞掉。
            //     ⚠️ 排在 ①b（含 `:`）和 ①（多属性拆分）**之前** —— 这句里没有 `:` 也没有
            //        `, ` / ` and `，但先判更清楚，将来加词也不会被抢走。
            if (low == "it as well")
            {
                ops.Add(new PayloadOp { CopyEventPact = true, Source = w });
                return true;
            }

            // ①b **嵌入的一整段效果文字** —— `Give "💀 Backlash: Return to your hand" to a …`
            //
            //   判据：这一项里**含 `:`**，且 `:` **前面**那截是 `KeywordTable` 认得出的关键词。
            //   ⚠️ 必须在多属性拆分**之前**判 —— 那段文字里可能带 `,` 或 ` and `
            //      （`"Slay: Heal 2 and gain a Dark Pact of Excess"`），拆开了就废了。
            //   开头那个 `💀`（Backlash 的图标）是非 ASCII 噪声，一并剥掉。
            if (w.IndexOf(':') > 0)
            {
                int colon = w.IndexOf(':');
                string head = Regex.Replace(w.Substring(0, colon), @"[^\x20-\x7E]", "").Trim();
                head = head.Trim('"', '\'', '“', '”', ' ');
                // 🆕 2026-09-16：**头里可能粘着「图标词」的残渣** —— 卡面把图标印成方括号词
                //   （`[skull]` / `[Slay]`），而方括号在 `EffectText.ParseSegment` 的入口**早就被剥掉了**
                //   ⇒ 头成了 `skull slay` / `slay slay`（都不是关键词）⇒ **整段嵌入效果静默丢掉**。
                //   实测两张（全池按判据扫，就这两张）：
                //     · `Ferocious Rage (Beastboss' Talent)`：`' [skull] Slay: Draw a Beast'`
                //     · `Uge Choppa`：`"+2 [Attack] and [Slay] Slay: Heals 3"`
                //   做法：**从后往前逐词试**，哪一段能被 `KeywordTable` **整段**吃掉就用它
                //   （i 从 0 开始 = 先试整头，**与原行为完全一致**，只是多给短后缀机会）。
                string hitHead = null;
                var words = head.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < words.Length; i++)
                {
                    string cand = string.Join(" ", words, i, words.Length - i);
                    int hl2;
                    string hk2 = KeywordTable.Normalize(cand.ToLowerInvariant(), out hl2);
                    if (hk2 != null && hl2 == cand.Length) { hitHead = cand; break; }
                }
                if (hitHead != null)
                {
                    ops.Add(new PayloadOp
                    {
                        Embedded = (hitHead + w.Substring(colon)).ToLowerInvariant(),
                        Source = w,
                    });
                    return true;
                }
            }

            // ① 多属性拆分 —— 先按 `, ` 再按 ` and `（`rule_core.gd:3274`，顺序照抄）
            if (low.Contains(" and ") || low.Contains(", "))
            {
                // 🔴 **2026-09-16：先拆成段、确认真拆得开，再递归** —— 这里原来有个
                //   **会崩进程的潜伏雷**：判据吃的是 `low`（**小写**）、拆分吃的却是 `w`（原串）
                //   ⇒ 全大写的 ` AND ` 会被判成「有多段」却**一段也拆不开**，
                //   于是整段被**原样递归回本函数** = 无穷递归。
                //   实测 `GivePayload.Parse("+1 attack AND vanguard")` → **StackOverflow**，
                //   .NET 接不住 ⇒ 在 Unity 里就是**整个编辑器崩**。
                //   全池当前 **0 条**载荷含大写 ` AND `，属潜伏雷。
                //   修法：**拆不出 ≥2 段就退回去**，交给下面各支（它们本来可能认得）。
                var pieces = new List<string>();
                foreach (string s1 in w.Split(new[] { ", " }, System.StringSplitOptions.None))
                    foreach (string s2 in s1.Split(new[] { " and " }, System.StringSplitOptions.None))
                    {
                        string seg = s2.Trim();
                        if (seg.Length > 0) pieces.Add(seg);
                    }
                if (pieces.Count > 1)
                {
                    bool any = false;
                    foreach (string seg in pieces)
                    {
                        if (ParseInto(seg, ops)) any = true;
                        // 🆕 **这一段不认识 ⇒ 留个标记**，别让它静默消失（见 `PayloadOp.Unresolved`）。
                        //    标记只在 `any == true` 时才有意义（整条都解不出来时 `Parse` 会给 null、
                        //    调用方本来就会报），所以这里不做区分、一律留痕。
                        else ops.Add(new PayloadOp { Unresolved = true, Source = seg });
                    }
                    return any;
                }
            }

            // ② 关键词授予 —— `GIVE_KW` 前缀匹配（值 = 载荷里第一个数字，没有就是 1）
            //
            // ⚠️ **只认开头**。噪声（冠词 / 数量 / `[Chaos]` 这类方括号阵营名）已经在上面
            //    `ReLeadingNoise` 里剥干净了，所以这里的判据可以收得很紧。
            //    松成 `IndexOf >= 0` 会误伤 —— `Trigger the Codex ability of all friendly units`
            //    这行里的 `codex` 不是「授予典籍关键词」，是**效果文字里提到它**。
            foreach (var pair in GiveKw)
            {
                if (!low.StartsWith(pair[0])) continue;
                var m = ReNumber.Match(w);
                ops.Add(new PayloadOp
                {
                    Keyword = pair[1],
                    Value = m.Success ? int.Parse(m.Groups[1].Value) : 1,
                    Variant = ExtractVariant(pair[1], w),
                    Source = w,
                });
                return true;
            }

            // ③ 属性增减益
            var am = ReAttr.Match(low);
            if (am.Success)
            {
                ops.Add(new PayloadOp
                {
                    Attr = NormAttr(am.Groups[2].Value),       // 归一化收在 `NormAttr` 一处
                    Value = int.Parse(am.Groups[1].Value),
                    Source = w,
                });
                return true;
            }

            // ⑤ 裸 `+N` / `-N` —— **数字后面跟的是图标，而图标在文本导出里丢了**。
            //
            //    证据（照工作准则第 3 条，参数从资源里抄、不猜）：
            //    `D:/2/Warpforge部队卡片/Dark Angels/4计策/Warpforge_57_March-of-Vengeance.png`
            //    卡面原文是 `Give +1 ⟨拳头图标⟩ to a friendly unit. Gain ⟨能量图标⟩ 2`
            //    —— 那个红圈拳头 = **近战攻击**（原版自己的映射见 `rule_core.gd:3325`：
            //    `fist/strength=拳头图标→attack`）。文本里只剩 `+1`，所以按近战攻击补。
            //    ⚠️ **原版 `_apply_gain` 对这种情况是失配的**（那条正则要求数字后面跟属性词）
            //       → 原版把这 12 张卡静默丢了。我们照卡面补上，并在注释里标明这是**补原版的漏**。
            var mb = ReBareSigned.Match(w);
            if (mb.Success)
            {
                ops.Add(new PayloadOp
                {
                    Attr = "attack",
                    Value = int.Parse(mb.Groups[1].Value),
                    Source = w,
                });
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从载荷里摘出**变体名** —— 目前只有 `dark pact of blood` 这一族有。
        /// `rule_core.gd:1696`：`give (?:a )?(?:random )?dark pact(?:\s+of\s+([a-z]+))?`。
        /// 没写 `of X` 就是 `random`（原版 `:1670` 明写）。**别的关键词没有变体，返回 null。**
        /// </summary>
        static string ExtractVariant(string keyword, string w)
        {
            if (keyword == KeywordTable.DarkPact)
            {
                // 从**关键词本身**往后看，别从头 Match —— 前面可能还挂着剥剩下的噪声
                int at = w.IndexOf("dark pact", System.StringComparison.Ordinal);
                if (at < 0) return "random";
                var m = Regex.Match(w.Substring(at), @"dark pact(?:\s+of\s+([a-z]+))?");
                if (m.Success && m.Groups[1].Success) return m.Groups[1].Value;
                return "random";
            }
            return null;
        }

        /// <summary>
        /// 这条载荷**我们有没有机制去结算**。
        ///
        /// ⚠️ 和「能不能解析」是两件事（`资料/战术卡效果_移植方案.md` 第三节）：
        ///    `give Flank to a friendly troop` 解析得出来，但 `Flank` 没机制 → 这张卡
        ///    「能打但没用」。**两个数都要报**，只报一个会掩盖这类静默失效。
        /// </summary>
        public static bool Mechanized(string payload, out string why)
        {
            // 三选一：`Payload` 存的是三个选项（`|` 隔开）—— 三个都要有机制才算数
            if (payload != null && payload.IndexOf('|') >= 0)
            {
                foreach (string o in payload.Split('|'))
                {
                    if (!Mechanized(o, out why)) return false;
                }
                why = null;
                return true;
            }

            var ops = Parse(payload);
            if (ops == null) { why = "载荷词表里没有"; return false; }
            foreach (var op in ops)
            {
                // 嵌入的一整段效果文字：那一段**自己**要能解析成 op，否则挂上去也不触发。
                // 🔴 **判据必须和运行时同源**（`EffectResolver.GrantEmbeddedCore` 也调 `EffectText.Parse`）——
                //    原来这里调的是 `EffectSpec.Parse`（**封闭文法**，只认 `Damage/Heal/Draw`），
                //    运行时**也**调它 ⇒ 两边一致地解不了原版卡面原文，报表和实况一起「绿着没用」。
                if (op.IsEmbedded)
                {
                    var eops = EffectText.Parse(EmbeddedBody(op.Embedded), out _, out _);
                    if (eops == null || eops.Count == 0)
                    {
                        why = "嵌入的效果文字解析不了";
                        return false;
                    }
                    continue;
                }
                // 🆕 **载荷里有认不出的段**（见 `PayloadOp.Unresolved`）——
                //   这一段**没生效**，必须让报表与卡面都看得见（红线：不许静默失败）。
                //   ⚠️ 必须排在下面 `if (!op.IsKeyword) continue;` **之前** ——
                //      标记项既非 keyword、也无 `Attr`，不拦就**照旧放行**。
                if (op.Unresolved)
                {
                    why = "载荷里有认不出的段：「" + op.Source + "」";
                    return false;
                }
                if (!op.IsKeyword) continue;                 // 属性增减益：UnitState 有对应字段
                if (!KeywordTable.Implemented.Contains(op.Keyword))
                {
                    why = "关键词「" + op.Keyword + "」没有机制";
                    return false;
                }
            }
            why = null;
            return true;
        }

        /// <summary>`"backlash: return to your hand"` → `return to your hand`（`:` 后面那截）。
        /// ⚠️ **首尾的引号要一并剥掉** —— 卡面把整段效果文字用引号包着
        /// （`Give to a friendly troop Flank and 'Strike: Draw a card'`），
        /// 不剥的话正文尾巴上挂着一个 `'`，解析器当噪声拒掉（2026-09-14 实测）。</summary>
        public static string EmbeddedBody(string embedded)
        {
            if (string.IsNullOrEmpty(embedded)) return null;
            int c = embedded.IndexOf(':');
            return c < 0 ? null : embedded.Substring(c + 1).Trim().Trim('"', '\'', '“', '”').Trim();
        }

        /// <summary>`"backlash: return to your hand"` → `backlash`（`:` 前面那截，已小写）</summary>
        public static string EmbeddedKeyword(string embedded)
        {
            if (string.IsNullOrEmpty(embedded)) return null;
            int c = embedded.IndexOf(':');
            return c <= 0 ? null : embedded.Substring(0, c).Trim();
        }
    }
}
