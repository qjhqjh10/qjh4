// CardText.cs — 卡面上的中文文案（显示名 / 关键词 / 效果小字）
//
// **只放显示用的字**：规则、存档、测试、美术文件名一律用英文 `CardDef.Id` / `Name`，一个字都不改。
// 和原版一个路子 —— 原版卡上只存 `refNameId`，显示名走 I2 Localization 的 term 表。
//
// 🔴 **2026-10-10（`A1084`）：本文件不再自己存中文表** —— 那几族（`Names` / `KeywordNames` /
//    `KeywordZhNames` / `FactionNames` / `Phrases` / 效果词）**已整体搬进 `Core/Loc.cs`**
//    （键族见下面那段头部注释），本文件一律**转发**（`Term` / `TermOr`）。
//    理由：它们与 `Loc` 是**两条并行的取值路径** —— 工程红线「两处写同一条规则 = 迟早不一致」。
//
// 中文有**两个来源**，别混：
//   1. **我们自己设计的 26 张卡** → `Core/Loc.cs` 的 `Card_Name/<CardDef.Name>` 那一族，
//      名字是我们自己起的。
//   2. **原版 1131 张卡** → 卡表里的 `CardDef.NameZh` / `DescZh`
//      （`数据/卡牌翻译/zh_cards.json` → `工具/gen_cards_engine.py` → `cards_engine.json`）。
//      ⚠️ 那是**原版的文案**，走的是和原版美术一样的口径：**个人使用、不进发布版本**，
//      而且它和 `cards_engine.json` 里的英文 `desc` 一样是**已进仓库的卡牌数据**
//      （红线第 4 条讲的是「原版解包资源与直接衍生物留在本地」，这条数据在 2026-09-12 之前就已经在仓库里了，
//      本次只是把中文和它并到一起，**没有新开口子**）。真要发布，两份一起换掉。
//
// ⚠️ 走中文的前提是**两个**：① **拿得到中文字体资产**（`TmpFont.Available`）—— 自写的 5×7 点阵字库
//    一个汉字也画不出来；② **当前语档就是中文**（`Loc.Current == AvailableLanguages.Chinese`）。
//    两条缺一条就**自动回英文**（不会出现「汉字变方块/空白」，也不会出现「选了 English 卡面还是中文」）。
//    🔴 **2026-10-18 更正（铁律 5）**：原来这里只写第 ① 条、`Zh` 也只判字体 —— 而**中文字体资产早已进仓库**
//      ⇒ `Zh` **恒真** ⇒ **切成 English 之后，卡名/阵营/关键词/效果文字/`Phrases` 兜底那 17 条仍是中文**
//      （只有走 `Loc.T` 的件会变）。原版有语言选择器 ⇒ 选了英文就该是英文，故补上第 ② 条。
//    建字体资产时统计要烘哪些字走 `AllChinese()`（那个**不看当前语言，永远给中文** —— ⛔ 别把它也闸上）。
using System.Collections.Generic;
using RuleEngine;

namespace CardPresentation
{
    public static class CardText
    {
        /// <summary>当前显示中文吗。**两个条件都满足才算**：① 拿得到中文字体资产 ② 当前语档是中文 —— 见文件头。
        /// ⚠️ 改这个属性会**连带**卡名/阵营/关键词/效果文字/短语那一族的行为（它们都走 `Term`/`TermOr`，
        /// 而那两道闸判的就是它）⇒ 改完要并排比（铁律 10⑥）。</summary>
        public static bool Zh
        {
            // 🔴 **2026-10-18 更正（铁律 5）**：原来只有 `TmpFont.Available` 一项 ⇒ 字体资产一进仓库它就**恒真**，
            //   「选了 English」对它毫无影响（**拿字体闸当语言闸**）。补上语档那一项。
            get { return TmpFont.Available && Loc.Current == AvailableLanguages.Chinese; }
        }

        // ==================================================================
        //  🔴 **2026-10-10（`A1084`）：本文件那几张中文表【已整体搬进 `Core/Loc.cs`】**
        //
        //  **搬走的是哪几族**（键名 + 出处逐族写在 `Core/Loc.cs` 那一块的头注释里）：
        //    `Card_Name/<CardDef.Name>`（26 张自造卡）· `Card_Trait/<canonical 键>`（74 个关键词）
        //    · `Armies/<CardDef.Faction>`（16 个阵营）· `Battle/Phrase/*`（12 条 HUD 短语 + `TurnLabel`）
        //    · `CardEffect/*`（效果小字 + 技能卡面板整句的构件）。
        //
        //  **为什么**：它们与 `Loc` 是**两条并行的取值路径**（工程红线：两处写同一条规则 = 迟早不一致）。
        //    搬完 ⇒ **判据只此一份**，本文件一律**转发**（见下面那两个转发口 `Term` / `TermOr`）。
        //
        //  🔴 **搬表【必须】同批处理的两件**（普查 §5.3·1 点名的静默失败面）——
        //    ① **`AllChinese()`**：它是**字体语料**的唯一来源（`Editor/TmpSetup.Corpus()` 读它）。
        //       搬完已改成**内转 `Loc.AllChinese()`**（见文件末尾）⇒ 搬走的这几族**照样进语料**（只多不少）。
        //    ② **`Zh`**：**字体闸 + 语言闸**二合一，转发一律经它（`Term` / `TermOr`）。
        //    ⛔ 少做① ⇒ `TmpSetup` 那条覆盖自检**静默失去覆盖面**（`CheckCoverage` 只会说「全过」）。
        //
        //  ⚠️ **`KeywordZhAliases`（`装甲` / `爆破`）【留在本文件、没搬】** —— 它**不上屏**，只参与
        //    「卡面这一段印过没有」的比对；搬走会让 5 张卡重复印一遍关键词（判据见它自己那段注释）。
        //  ⚠️ **本文件里剩下的中文字面量**：只有 `KeywordSegment` 那个句读符 `"。"`（跟着语档走，不是词条）。
        //    判断「还有没有漏搬的」就照这条查（`grep` 代码位的中文串，应只剩它一处）。
        // ==================================================================

        // ---- 键前缀（**只此一份**：本文件的转发与 `Editor/CardBaseDemo.cs` 的断言都读它）----
        //      ⚠️ 断言里**写字面量**跟这些常量对，⛔ 别拿常量去断常量（同义反复，两边一起改照样绿）。

        /// <summary>卡名词条的键前缀。族名照原版（原版那 8 条 `Card_Name/{EC21,EC57,…}` 是异画特例），
        /// 我们自造那 26 张的**键名自拟**（原版两张表都查不到）。</summary>
        public const string CardNameTermPrefix = "Card_Name/";
        /// <summary>关键词词条的键前缀。**族 = 原版**（`GameStaticData.TraitNameToString` 拼的就是它）；值取不到。</summary>
        public const string TraitTermPrefix = "Card_Trait/";
        /// <summary>阵营词条的键前缀。**族 = 原版**（`Armies/<Faction>`，表① 里那 14 条 mTerm 全在）。</summary>
        public const string ArmyTermPrefix = "Armies/";
        /// <summary>HUD 短语词条的键前缀（**自拟族**；另立键、不复用 `Battle/BattleEnd/*` 与 `Battle/Tips/*`）。</summary>
        public const string PhraseTermPrefix = "Battle/Phrase/";
        /// <summary>效果小字 / 技能卡面板整句的键前缀（**自拟族**）。</summary>
        public const string EffectTermPrefix = "CardEffect/";

        /// <summary>`TurnLabel` 那一条的键（数字在中间 ⇒ 单独一条，值是 `第 {0} 回合` / `TURN {0}`）。</summary>
        public const string TurnLabelTerm = PhraseTermPrefix + "TurnLabel";

        // ==================================================================
        //  🆕 2026-09-15：**关键词段**（卡面上「关键词 + 图标」那一段）
        // ==================================================================
        //
        // **为什么要有它**：原版卡面把关键词**印在效果文字前面**
        // （`〔盾〕Armour 1. 〔箭〕Flank. Rally: Stun an enemy`），而我们的 `desc` 是 OCR 来的、
        // 常常只剩后半段 —— 实测 **1130 张里 306 张**的关键词在 `desc` 里一个字都没有
        // （`资料/PnP卡图_逐张对账_0915.md` §四·E）。补在**表现层**，
        // **不去改 `desc`**：`desc` 是引擎解析效果用的原文，动它会改结算。

        /// <summary>🔴 **2026-10-10（`A1084`）：那张 62 条的 `KeywordZhNames` 表已整体搬进 `Core/Loc.cs`**
        /// （键 = `Card_Trait/&lt;canonical 键&gt;`，族名照原版 `GameStaticData.TraitNameToString` 的拼法）。
        /// ⚠️ 同时**合并掉**了原来那张 12 条的 `KeywordNames`（那 12 个键是这 62 个的**子集**、值逐字相同）
        /// —— 原先两张表并存 ⇒ 同一个词两条路，正是本文件头部那条「两处写同一条规则」。
        /// ⛔ 别在本文件重建任何一张关键词表（加/改词一律去 `Loc.cs` 那一块）。</summary>
        static string TraitTerm(string canonicalKey)
        {
            return TraitTermPrefix + canonicalKey.Trim().ToLowerInvariant();
        }

        /// <summary>关键词 → **中文名**；**查不到返回 null**（调用方不许猜）。
        /// <para>🔴 **恒中文、不看语档**（与改前逐字同口径）—— 调用方自己决定要不要：
        /// `KeywordSegment` 拿它当「中文档那一列」，`TipText.Trait` 拿它当**规则书文案表的键**
        /// （那张表按中文名索引）。⇒ 显示层要跟语档走请用 <see cref="KeywordDisplay"/>。</para>
        /// <para>⚠️ 表里没有这个键 ⇒ **出声一次**（走 `Loc.T` 那条去重通道）后返回 `null` —— ⛔ 不许回键名，
        /// 那会让卡面印出 `Card_Trait/lord commander`（`KeywordSegment` 判「认不出」靠的就是 null）。</para></summary>
        public static string KeywordZh(string canonicalKey)
        {
            if (string.IsNullOrEmpty(canonicalKey)) return null;
            string t = TraitTerm(canonicalKey);
            if (Loc.HasEntry(t)) return Loc.ZhOf(t);
            Loc.T(t);                       // 缺键：出声 + 记账（⛔ 不是静默）
            return null;
        }

        /// <summary>关键词 → **英文名**（`KeywordEn` 的显示那一半）。
        /// <para>🔴 **恒英文、不看语档** —— 与 `KeywordEn` 同口径（`KeywordSegment` 在**中文档**下也拿英文名
        /// 去比「效果正文里已经写过没有」，见那里的 `other`）。⇒ 这里取的是**英文那一列**，不是 `Loc.T`。</para>
        /// <para>👉 **要按语档取显示名**（关键词 tooltip 的标题）请用 <see cref="KeywordDisplay"/>。</para></summary>
        public static string KeywordDisplay(string canonicalKey)
        {
            if (string.IsNullOrEmpty(canonicalKey)) return null;
            string t = TraitTerm(canonicalKey);
            if (Loc.HasEntry(t)) return Term(t);            // 中文档 ⇒ 中文列；英文档 ⇒ 英文列
            Loc.T(t);
            return null;
        }

        /// <summary>关键词的**英文**显示名。表里那一条的英文列 = 改前 `KeywordEnExceptions` 那 6 条例外
        /// （`Can't Attack` / `Hunt Mark` / `Long Range` / `Blood Thirst` / `Dark Pacts` / `Spirit Stone`）
        /// 与「首字母大写」那套兜底的**合并结果**（两张表已并进 `Loc`，见上）。
        /// <para>⚠️ 表里没有这个键 ⇒ 仍按**首字母大写**还原 —— 那是**兜底、不是原版写法**（与改前逐字相同），
        /// 而且 `Editor/BattleScene.cs` 有一条断言就钉着它（`KeywordZh("lord commander") == null`
        /// 且 `KeywordEn("lord commander") != null`）。</para></summary>
        public static string KeywordEn(string canonicalKey)
        {
            if (string.IsNullOrEmpty(canonicalKey)) return null;
            string s = canonicalKey.Trim();
            string t = TraitTerm(s);
            if (Loc.HasEntry(t)) return Loc.EnOf(t);        // ⚠️ 恒英文（`EnOf` 不跟语档）
            Loc.T(t);
            return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>同一个关键词在我们数据里的**别的中文写法**（判「已经印过没有」时要一起看）。
        /// 只收**确认出现过**的（`grep card_stats.json` 抄出来的），不是同义词大典。</summary>
        static readonly Dictionary<string, string[]> KeywordZhAliases = new Dictionary<string, string[]>
        {
            { "armour", new[] { "装甲" } },     // 数据里 `护甲` / `装甲` 两种都有
            // 🔴 2026-09-15 加（逐张并排验收查出来的）：`Blast` 在 `KeywordZhNames` 里是「**爆裂**」，
            //    而 `descZh` 里写的是「**爆破**」⇒ 判「没出现过」⇒ 卡面印成「爆裂 2。爆破 2。」**重复一遍**。
            //    **影响 5 张**（`AM13/AM28/AM34/AM37/AM54`，逐张开原图核过）。
            //    ⚠️ 这条**必须两个方向都收**：只收「爆裂」会让 `descZh` 写「爆破」的那批重复印，
            //       只收「爆破」会让写「爆裂」的那批重复印 —— 所以两个词都当「已印过」。
            { "blast", new[] { "爆破" } },
        };

        static bool AlreadyInHay(string hay, string key)
        {
            string[] alts;
            if (KeywordZhAliases.TryGetValue(key, out alts))
                foreach (var a in alts)
                    if (hay.IndexOf(a, System.StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>
        /// 拼出**卡面那段关键词**（原版印在效果文字**前面**）。`keywords` = 单位/卡的 canonical 键表
        /// （`UnitState.Keywords` 或 `CardDef.Keywords`），`body` = 效果正文（用来判「哪些已经印过了」）。
        ///
        /// 三条判据（**只在这一处**）：
        /// ① **只补 `body` 里没出现过的** —— `Lychguard` 那种 `desc` 本身就等于关键词列表的，一个字都不补；
        /// ② **只补有显示名的**（`KeywordZh`/`KeywordEn` 查得到）—— `lord commander` 这类表外词**不补**；
        /// ③ 数值**只有带数值的关键词才印** —— 走**徽标那同一个入口** `Badges.CarriesValue`
        ///    （出处规则书「带数值」列）。
        ///    🔴 **但两处传的实参不同**（2026-10-11 · `A1332` 收口，铁律 5 把那句含糊的「判据同徽标」补全）。
        ///    ⚠️ **下面用的是 `CarriesValue` 那边的档号，与上面本方法自己的 ①②③ 不是一回事**：
        ///    这边传的是 `CarriesValue(key, kv.Value, null)` ⇒ 只接它那两档
        ///    —— **「引擎当前值 ≥ 2」** 与 **「那张硬编码表」**；
        ///    **「卡面原文里写了数字」（`numericKeys` = `CardDef.NumericKeywords`）那一档没接** ——
        ///    本方法的签名里没有它（生产调用点 `BattleDriver.FaceTextFull` 那行手里有 `c`，
        ///    把 `numericKeys` 接进来要给本方法加参数 ⇒ 那是下一笔，别在这儿猜）。
        ///    ✅ **今天两处逐张等价**（全池 1126 张现读：三个口径分别 **9 / 9 / 12 键**，
        ///    两个差集**都为空**，逐张过门槛 **0 处不同** ⇒ 2026-10-11 那次收口**不动任何一张卡的输出**）。
        ///    ⛔ **别再照「判据同徽标」去改 `Badges.CarriesValue` 的分支就以为这边跟着变** ——
        ///    两处**共用的是那个函数体**，剩下唯一的差别就是这个 `numericKeys` 实参；
        ///    哪天卡池出现「带数字但不在那张表里」的词，**只有这一档会把两处拉开**（`A1332`）。
        ///
        /// ④ 🆕 2026-09-21：**每个词前面要加它自己的图标**（原版卡面印的就是「图标 + 词」）。
        ///    ⚠️ 走到这里才补的，全是 `body` 里**一个字都没提过**的关键词 —— 也就是原版卡面
        ///    **只有图标那一行**能体现的（`Aeldari/3部队/Warpforge_20_Howling-Banshee.png` 逐张核过：
        ///    卡面 `◈Waystone.  ⬇Flank.` —— 这两个词 `desc` 里一个都没有，卡面照样带图标印着）。
        ///    认不出图的（`Badges.SpriteOf` 返回 null）**照旧只印词、不猜图**（工程红线）。
        ///    判据转调 `Badges.SpriteOf`（**只此一份**，别名表也在那儿）—— 别在这儿另写一张。
        ///    ⑤ 每一项还包一层 **`&lt;link=规范键>`** —— 原版就是这么干的
        ///    （`GameStaticData__TraitNameToString.c:84-109` 整项套 `&lt;link=&lt;DefinedTrait枚举名>>`，
        ///    由 `TextTooltipController__GetTraitTooltip.c:30,35-36` 命中后弹 trait tooltip）。
        ///    ⇒ 悬停关键词语出解释那条路（`TipText.Trait`）**就靠这层 link**，别删。
        ///    ⚠️ **link id 用我们的规范键**（`armour`/`rally`…），不用原版的枚举名 ——
        ///      原版那串是 `DefinedTrait` 的成员名，我们查的是自己的表（`TipText.Trait`），
        ///      **两套名字一一对应但不必逐字相同**。
        ///
        /// ⚠️ 顺序按 **canonical 键排序**：引擎里关键词是 `Dictionary`、枚举顺序不稳；
        ///    卡面本来该按卡自己的顺序印，但**数据里没有那个顺序** ⇒ 这是我们挑的，标明在此。
        ///    ⚠️ 排序键用**不加图标的那个词**：加了 `&lt;sprite …>` 前缀之后 Ordinal 会比到
        ///       **图名**上去（中文模式下会按英文图名排，顺序莫名其妙地变）。
        /// </summary>
        public static string KeywordSegment(IEnumerable<KeyValuePair<string, int>> keywords,
                                            bool zh, string body)
        {
            if (keywords == null) return "";
            string hay = body ?? "";
            var parts = new List<KeyValuePair<string, string>>();   // (排序键, 印出去的那段)
            var seen = new HashSet<string>();
            foreach (var kv in keywords)
            {
                string key = kv.Key;
                if (string.IsNullOrEmpty(key)) continue;
                string word = zh ? KeywordZh(key) : KeywordEn(key);   // ②
                if (string.IsNullOrEmpty(word)) continue;
                if (hay.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0) continue;   // ①
                // 中文卡面里 `Flying` 也可能写成英文（数据里两种都有）—— 两个写法都判一次
                string other = zh ? KeywordEn(key) : KeywordZh(key);
                if (!string.IsNullOrEmpty(other) &&
                    hay.IndexOf(other, System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                // ⚠️ 我们的中文数据里**同一个词有两种写法**（`护甲` / `装甲` 都有，
                //    见 `Lychguard` 的「装甲 2」与 `Heavy Intercessor` 的「护甲 1」）——
                //    只按一种判会在另一种写法上**补出重复的一段**。这里补一张**同义写法**表。
                if (zh && AlreadyInHay(hay, key)) continue;
                // ③ 走徽标那个**同一个入口**（`Badges.CarriesValue`）—— ⛔ 别在这儿另写一张表/另一套分支
                //    （两处写同一条规则 = 迟早不一致，`A1332` 就是这条）。
                //    ⚠️ 实参 `(key, kv.Value, null)`：`kv.Value` 让「判不判」与「印几」**同源**
                //    （接上 `CarriesValue` 里「引擎当前值 ≥ 2」那一档）；`numericKeys` 传 `null`
                //    = 「卡面原文里写了数字」那一档**这边不接**（签名里没有它，理由与影响见上面 ③ 那段）。
                //    这一改**不动任何一张卡的输出**（全池 1126 张 0 处不同，离线复算见 `A1332` 报告）。
                if (kv.Value > 0 && CardPresentation.Badges.CarriesValue(key, kv.Value, null)) word += " " + kv.Value;   // ③ + 值 ≥ 2 那一档
                if (!seen.Add(word)) continue;
                string sprite = CardPresentation.Badges.SpriteOf(key);                                // ④
                // 🔴 **每一项要包 `<nobr>`**（原版就是这么写的）：`<nobr><sprite …>词</nobr>`。
                //    不包的话 TMP 会把图标与词**拆到两行**（图标留在上一行行尾、词掉到下一行）。
                //    出处（全量反编译 `d:/2/tools/decomp_full/`）：`GameStaticData__TraitNameToString.c:75-76`
                //    = `<nobr>` + sprite + 本地化词 + `</nobr>`，**图标与词之间不加空格**。
                //    ⚠️ 原版标签写的是**不带引号**的 `<sprite name=Atlas_trait_icon_armour>`
                //       （真值样本 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-9193901185350865183.json`
                //       的 `m_text`：`'<sprite name=Atlas_trait_icon_fast>5d 20h 15m'`）。
                //       我们**保留引号**：图名走的是我们自己的图集（`Art/traits/` 的文件名，不是
                //       `Atlas_trait_icon_*`），而且卡面另一条路 `CardIcons.Rewrite` 也是带引号的写法
                //       —— 两条路写法要一致，别只改一处。
                string item = string.IsNullOrEmpty(sprite)
                            ? "<link=" + key + ">" + word + "</link>"
                            : "<link=" + key + "><nobr><sprite name=\"" + sprite + "\">" + word + "</nobr></link>";
                parts.Add(new KeyValuePair<string, string>(word, item));
            }
            if (parts.Count == 0) return "";
            parts.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            var outParts = new List<string>(parts.Count);
            foreach (var p in parts) outParts.Add(p.Value);
            return string.Join(zh ? "。" : ". ", outParts.ToArray()) + (zh ? "。" : ". ");
        }

        // ==================================================================
        //  效果小字 / 技能卡面板整句 —— **词全在 `Core/Loc.cs`**（族 = `CardEffect/*`，`A1084` 搬的）
        //
        //  改前这里是 16 个 `const string`（`伤害`/`治疗`/`对`/`造成`/`点伤害`…），拼句子时**按语序手工串**。
        //  🔴 **为什么整句建键、而不是「一个词一条键 + 代码拼」**：中英**语序不同**
        //  （中文「为{1}回复 {0} 点生命」/ 英文「Restore {0} health to {1}」）⇒ 拿词拼就必然要按语档分支写两套语序。
        //  ⇒ 表里存的是**整句模板**，`{0}` = 数量、`{1}` = 目标词（目标词自己也走 `CardEffect/Target/*`）。
        //  ⚠️ 这 16 个常量的中文**已逐字搬进 `Loc`**（`CardEffect/Short/*` · `CardEffect/Target/*` ·
        //     `CardEffect/Sentence/*`）⇒ 它们的字照样进字体语料（`AllChinese()` 内转 `Loc.AllChinese()`）。
        // ==================================================================

        // ==================================================================
        //  HUD 固定短语
        // ==================================================================
        //
        //  🔴 **2026-10-10（`A1084`）：本文件那张 20 条的 `Phrases` 表【已整体搬进 `Core/Loc.cs`】**
        //     —— 键族 = `Battle/Phrase/*`（**自拟族**；**另立键、⛔ 不复用** `Battle/BattleEnd/*`
        //     与 `Battle/Tips/{MeleeAttack,RangeAttack}`：那两条语义是**结算面板标题**与**数值格标签**，
        //     复用会让「改一处文案另一处跟着变」，铁律 6）。
        //     这里只剩**转发表**（`PhraseTerms`：英文原文 → `Loc` 键）与两个转发口。
        //     ⛔ 别在本文件重建那张表（加/改短语一律去 `Loc.cs` 那一块）。
        //     · 20 条的去向：**16 条有键**（4 条原有 + 11 条另立 + 1 条 `THIS UNIT ALREADY ACTED` 走原版键）；
        //       **4 条（`HAND`/`DECK`/`DISC`/`HP`）零消费点 ⇒ 删掉、不建键**（普查 §5.4 grep 实证）。
        //  ⚠️ HUD 走的是 `Label`，它有 TMP 后端之后才画得出汉字 ——
        //    拿不到字体资产时 `Zh == false`，`Phrase` 一律回英文，行为跟以前一样。

        /// <summary>英文原文 → `Core/Loc.cs` 的键（**这一族的唯一一张映射表**）。
        /// <para>**为什么要有它**：这一族短语的调用点两处都有 —— 战斗侧（窗口/HUD）走 `Loc.T`，
        /// 而另有调用点**在本文件之外**（`Shell/BattleLogData.cs` 的 `DisplayOf` 还在用
        /// `Phrase("VICTORY"/"DEFEAT"/"DRAW")`）。**同一条语义只许有一条权威**（工程红线：
        /// 两处写同一条规则 = 迟早不一致）⇒ 权威在 `Loc`，这里只做**转发**。</para>
        /// <para>🔴 **2026-10-10（`A1084`）**：原来这里查不到时会回落到本文件那张**中文兜底表**
        /// （`Phrases`，20 条）—— 那张表**已删**。现在**没登记的英文原文照原样回英文**
        /// （= 它原来的第一道 `if`，⛔ 不静默变空白）。</para>
        /// <para>⚠️ 键名与逐条判据（谁是原版 `mTerm`、谁自拟）写在 `Loc.cs` 那一块的注释里，⛔ 别在这儿抄第二份。</para>
        /// <para>🧨 **改坏法**：把 `MELEE`/`RANGED` 那两条改成复用近邻 `Battle/Tips/{MeleeAttack,RangeAttack}`
        /// ⇒ 「改一处文案另一处跟着变」（那两条是**数值格标签**、不是打法名）。</para></summary>
        static readonly Dictionary<string, string> PhraseTerms = new Dictionary<string, string>
        {
            { "VICTORY", "Battle/BattleEnd/Victory" },
            { "DEFEAT",  "Battle/BattleEnd/Defeat"  },
            { "DRAW",    "Battle/BattleEnd/Draw"    },
            // 🆕 **2026-10-18（第十三轮 · G2b）**：回合那三条 —— 原版**都有确凿 `mTerm`**
            //   （`Battle/HUD/{EndTurn,YourTurn,EnemyTurn}`，载波 = 代码字面量；逐条判据写在
            //    `Core/Loc.cs` 那一块）。
            { "END TURN",   "Battle/HUD/EndTurn"   },
            { "YOUR TURN",  "Battle/HUD/YourTurn"  },
            { "ENEMY TURN", "Battle/HUD/EnemyTurn" },
            // ---- 🆕 **2026-10-10（`A1084`）**：原 `Phrases` 表里那 11 条（**另立键**，见上面那条判据）----
            //   ⚠️ 这 11 条的 **EN 列 = 原来 `Phrases` 的字典键（这些英文原文）逐字** ⇒ 英文档零变化。
            { "GAME OVER",            PhraseTermPrefix + "GameOver"      },
            { "YOU WIN",              PhraseTermPrefix + "YouWin"        },
            { "YOU LOSE",             PhraseTermPrefix + "YouLose"       },
            { "CHOOSE ACTION",        PhraseTermPrefix + "ChooseAction"  },
            { "PICK A TARGET",        PhraseTermPrefix + "PickTarget"    },
            { "NO LEGAL TARGET",      PhraseTermPrefix + "NoLegalTarget" },
            { "MELEE",                PhraseTermPrefix + "Melee"         },
            { "RANGED",               PhraseTermPrefix + "Ranged"        },
            { "ABILITY",              PhraseTermPrefix + "Ability"       },
            { "STUNNED",              PhraseTermPrefix + "Stunned"       },
            { "THIS UNIT CANNOT ACT", PhraseTermPrefix + "CannotAct"     },
            // ⚠️ 下面这条**今天没有消费点**（`BattleDriver.OpenCommand` 已改走 `Loc.T`），但它**在原版有确凿键**
            //    （`Battle/Tips/UnitNotReady`，判据 → `Loc.cs` 那一段）⇒ 留着转发，⛔ 别照着它再写一处消费点。
            { "THIS UNIT ALREADY ACTED", "Battle/Tips/UnitNotReady" },
            // ⛔ **原 `Phrases` 里 `HAND`/`DECK`/`DISC`/`HP` 那 4 条【已删、不建键】** ——
            //    全仓零消费点（普查 §5.4 grep 实证）⇒ 删它们**不改任何行为**
            //    （`Phrase("HP")` 现在回 `"HP"`、改前回 `"生命"`，但没有任何调用点）。
        };

        /// <summary>**从词条表取字，并守住那道字体闸**（= `Phrase` 的头一道 `if`）。
        /// <para>走 <see cref="Loc.T"/>（按当前语言取；缺键会出声并回键名），但
        /// **拿不到中文字体资产 / 语档不是中文时一律回英文列**（<see cref="Loc.EnOf"/>）——
        /// 直接 `Loc.T` 会绕过这道闸，中文就画成空格/方块（本工程的静默失败红线）。</para>
        /// <para>🔴 **2026-10-10（`A1084`）就地订正**：原来这里写着「⚠️ **只给「原版有对应 `mTerm`」的那一族用**；
        /// 原版没有词条的仍然留在本文件 `Phrases` 那张兜底表里。⛔ 别拿它当通用取值口」——
        /// **那张兜底表已删**（`A1084` 把 `CardText` 那几族中文整体搬进 `Loc`）⇒ 本函数现在是
        /// **本文件取值的主口**（`TermOr` 也走它）。「别乱用」那条规矩**仍然成立**，
        /// 理由变成：**任何取值都必须过这道闸**（绕过它 = 英文档印中文 / 缺字体印方块）。</para></summary>
        public static string Term(string term)
        {
            if (string.IsNullOrEmpty(term)) return "";
            if (!Loc.HasEntry(term)) return Loc.T(term);       // 表里没有 ⇒ 让它出声 + 回键名（不静默）
            return Zh ? Loc.T(term) : Loc.EnOf(term);          // 闸：没有中文字体资产 ⇒ 回英文
        }

        /// <summary>取值：**表里有 ⇒ <see cref="Term"/>；表里没有 ⇒ 出声 + 回 `fallback`**。
        /// <para>🔴 **为什么要有这个「带兜底」的版本**（而不是一律用 `Term`）：卡名/关键词/阵营/效果/短语
        /// 这几族的调用点**都有改前就存在的优雅兜底**（卡名回英文 id、关键词回首字母大写、阵营回 key…）
        /// —— 用 `Term` 那条「缺键 ⇒ 印键名本身」会把 `Card_Name/Scavenger` 印到卡面上。
        /// 本工程要的是「**不静默**」，而这里**既出声又保住界面**：出声走 `Loc.T`（`MissingCount` /
        /// `LastMissingKey` 也一起记上、且按键去重不刷屏），界面回改前那句兜底。</para>
        /// <para>🔴 **`fallback` 一律是「英文 / 原值」**，⛔ 不许再写中文兜底 —— 那等于没搬干净
        /// （本文件今天只剩 `KeywordSegment` 那个句读符 `"。"` 一处中文字面量）。</para>
        /// <para>⚠️ **`fallback` 允许为 `null`**（例：`KeywordZh` 那条要的兜底就是「认不出 = `null`」）。</para></summary>
        public static string TermOr(string term, string fallback)
        {
            if (string.IsNullOrEmpty(term)) return fallback;
            if (Loc.HasEntry(term)) return Term(term);
            Loc.T(term);                       // 缺键：出声 + 记账（⛔ 不是静默）
            return fallback;
        }

        /// <summary>固定短语的中文。**没登记的照原样回英文**（⛔ 不静默变空白）。
        /// 映射表 = <see cref="PhraseTerms"/>（权威 = `Loc` 表）。</summary>
        public static string Phrase(string en)
        {
            if (!Zh || string.IsNullOrEmpty(en)) return en;   // 闸：英文档 / 没字体 ⇒ 照原样回英文（同改前）
            string term;
            if (!PhraseTerms.TryGetValue(en, out term)) return en;
            return TermOr(term, en);
        }

        /// <summary>`TURN 3` / `第 3 回合`（数字在中间，所以单列一个）。
        /// 🔴 **2026-10-18（第十三轮 · G2b）就地修一处同族缺陷**：原来写的是
        /// `Zh ? "第 N 回合" : "TURN N"`，而 `Zh` = **拿得到中文字体资产**（不是「玩家选了中文」）
        /// ⇒ **英文档下这一行仍然印中文**（「第 3 回合   YOUR TURN」）。这一行与 `Phrase` 那三条
        /// 一起显示在同一处（`BattleDriver.UpdateHud` 的回合行）⇒ 只改 `Phrase` 会半中半英。
        /// ⇒ 判据补上「**当前语档是中文**」那一半。⚠️ 本串**原版没有对应词条**（`Battle/HUD/*` 那 8 条里
        /// 没有「回合 N」这一条，2026-10-18 逐条核过）⇒ 中文仍是我们自己的写法，如实标着。
        /// 🔴 **2026-10-10（`A1084`）**：这一串也搬进 `Loc` 了（键 = <see cref="TurnLabelTerm"/>，
        /// 值 = `第 {0} 回合` / `TURN {0}`）⇒ 上面那个手写的 `Zh &amp;&amp; Loc.Current == Chinese` 二选一
        /// **已经不需要**（`Term` 里判的就是同一条）。
        /// ⚠️ 缺键时回 **`TURN N`**（英文；⛔ 不是中文兜底 —— 兜底不许再写中文）。</summary>
        public static string TurnLabel(int n)
        {
            return TermOr(TurnLabelTerm, "TURN " + n).Replace("{0}", n.ToString());
        }

        /// <summary>阵营名（键 = `CardDef.Faction` / `StarterCards.*Faction`）。
        /// <para>🔴 **2026-10-10（`A1084`）：原来那两张表（`FactionNames` 16 条 + `FactionNamesEn` 13 条）
        /// 已合并成【一条词条两列】搬进 `Core/Loc.cs`**（键 = `Armies/&lt;Faction&gt;`）——
        /// 原先两张表并存就是「同一个阵营两条路」（英文那一列还是**另起**的一个函数形状）。</para>
        /// <para>⚠️ **两列不是同一串、别当直译**：`Goff` 中文列 `高夫兽人` / 英文列 `Orks`；
        /// `Leviathan` 英文列 `Hive Fleet Leviathan`。两列的出处（`factions.json` 的 `cn` 字段 ·
        /// `card_stats.json` `subtitle` 的逐阵营众数 · `Goff` 那个例外）**整段搬在 `Loc.cs` 那一块**，
        /// ⛔ 别在这儿抄第二份。</para>
        /// <para>**表里没有的，回退到 key 本身**（`BlackLegion`）——**不再 `ToUpperInvariant`**：
        /// 那种兜底会印出 `BLACKLEGION` 这种连在一起的全大写串，比原样回 key 难看也难查。</para>
        /// <para>⚠️ 这个函数对**非空 key 永不回退成空串**（`Editor/CardBaseDemo.cs` 有一条注释依赖这一点：
        /// `CardView` 的阵营行挂不挂，判的是它非不非空）。</para></summary>
        public static string Faction(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            return TermOr(ArmyTermPrefix + key, key);
        }

        // ==================================================================

        /// <summary>
        /// 卡的显示名。没收录的照原样回英文 —— **不要返回空串**，
        /// 那会变成一张没名字的卡，比英文名难查得多。
        /// <para>🔴 **2026-10-10（`A1084`）**：原来那张 26 条的 `Names` 表已搬进 `Core/Loc.cs`
        /// （键 = `Card_Name/&lt;CardDef.Name&gt;`）。**这条路对「表里没有」不出声**是**有意**的：
        /// `Name(id, nameZh)` 对**原版那 1131 张卡**本来就会落到这个兜底（它们的名字走 `CardDef.NameZh`、
        /// 不在本键族里）⇒ 出声会把日志刷爆。**「新加的起始卡忘了建键」由断言兜** ——
        /// `Editor/CardBaseDemo.cs` 逐张遍历 `StarterCards` 查 `Loc.HasEntry`（🧨 删一条键就红）。</para>
        /// </summary>
        public static string Name(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            return TermOr(CardNameTermPrefix + id, id);
        }

        /// <summary>
        /// 显示名的**带上「卡表里的中文名」**那版 —— 原版卡走这条。
        ///
        /// 两份中文来源分工：
        ///   · **我们自己设计的 26 张** → `Core/Loc.cs` 的 `Card_Name/&lt;CardDef.Name&gt;` 那一族（我们起的名字）
        ///   · **原版 1131 张** → 卡表里的 `CardDef.NameZh`（`数据/卡牌翻译/zh_cards.json`，
        ///     2026-09-12 由 `工具/gen_cards_engine.py` 并进 `cards_engine.json`）
        /// 两边都没有就回英文 id（**不静默**：1131 张里目前有 3 张没有中文名）。
        /// </summary>
        public static string Name(string id, string nameZh)
        {
            // 🔴 **2026-10-18 更正（铁律 5，由 G2b 点名）**：原来这里**无条件**返回 `nameZh` —— 那等于**绕过语言闸**
            //   （英文档下卡名照样印中文）。补上 `Zh` 这一道，与 `Name(id)` 那条路同口径。
            if (Zh && !string.IsNullOrEmpty(nameZh)) return nameZh;
            return Name(id);
        }

        /// <summary>关键词的中文名。`ARMOUR 2` 那种带数值的由调用方拼（`Keyword(Armour) + " " + n`）。
        /// <para>🔴 **2026-10-10（`A1084`）**：原来读的是本文件那张 **12 条**的 `KeywordNames`；
        /// 那张表**已并入** `KeywordZhNames`、一起搬去 `Loc` 的同一个键族（`Card_Trait/*`）——
        /// 那 12 个键本来就是那 74 个里的**子集**、值逐字相同。⇒ 现在**一个词只有一条词条**。</para>
        /// <para>⚠️ **副作用（有意的、如实记）**：那 12 个之外的词（例：`stun`）在**中文档**下
        /// 从原来的大写英文 `STUN` 变成中文 `眩晕` —— 这正是「一个词只许有一条显示路径」要的结果
        /// （不然同一个词在卡面的两段里一个中文一个英文）。**英文档一个字没变**（仍 `ToUpperInvariant`）。</para>
        /// <para>没收录的照样回大写英文，**行为跟以前一致**。</para></summary>
        public static string Keyword(string norm)
        {
            if (string.IsNullOrEmpty(norm)) return norm;
            if (!Zh) return norm.ToUpperInvariant();
            return TermOr(TraitTerm(norm), norm.ToUpperInvariant());
        }

        /// <summary>效果的目标词（`CardEffect/Target/*`）。
        /// **认不出 ⇒ 空串**（`Effect` 那个小字靠这个判「不拼那一段」）。
        /// ⚠️ 它**跟语档**：中文档给 `自身`/`己方战将`…，英文档给 `self`/`your warlord`…
        /// —— 改前那是**两张分开的 `switch`**（`TargetZh` / `TargetEn`），现在一条词条两列。</summary>
        static string TargetWord(string t)
        {
            string k = t == EffectTargets.Self ? "CardEffect/Target/Self"
                     : t == EffectTargets.OwnWarlord ? "CardEffect/Target/OwnWarlord"
                     : t == EffectTargets.EnemyWarlord ? "CardEffect/Target/EnemyWarlord"
                     : t == EffectTargets.EnemyUnit ? "CardEffect/Target/EnemyUnit"
                     : null;
            return k == null ? "" : TermOr(k, "");
        }

        /// <summary>效果的中文小字（`伤害2·敌方单位`）。**字体不在 / 英文档时退回引擎原文**（`DMG2 SELF`）
        /// —— 那条路**一个字没改**（走 `EffectSpec.Short()`）。
        /// <para>🔴 **2026-10-10（`A1084`）**：那 7 个中文词（`伤害`/`治疗`/`抽牌`/`自身`/…）搬进
        /// `Loc` 的 `CardEffect/Short/*` + `CardEffect/Target/*`；`·`（U+00B7）是**分隔符不是词条**，留在这里。</para></summary>
        public static string Effect(EffectSpec spec)
        {
            if (spec == null) return null;
            if (!Zh) return spec.Short();

            string verb = spec.Verb == "damage" ? TermOr("CardEffect/Short/Damage", "DMG")
                        : spec.Verb == "heal" ? TermOr("CardEffect/Short/Heal", "HEAL")
                        : TermOr("CardEffect/Short/Draw", "DRAW");
            string s = verb + spec.Amount;
            string t = TargetWord(spec.Target);
            return t.Length > 0 ? s + "·" + t : s;
        }

        /// <summary>
        /// 效果 → **一整句**（技能卡面板 `ActiveSkillDesc.DescText` 用）。
        /// 和 `Effect()` 的区别：那个是卡面上省地方的小字（`伤害2·敌方单位`），这个是句子。
        /// <para>🔴 **2026-10-10（`A1084`）**：句子的**模板（含语序）**搬进了 `Loc`
        /// （`CardEffect/Sentence/*`）—— 中英语序不同，所以建的是**整句键**、⛔ 不是拿词拼。
        /// `{0}` = 数量、`{1}` = 目标词（目标词自己也走词条）。
        /// ⚠️ 缺键时回**引擎原文** `spec.Short()`（英文；⛔ 不是中文兜底）。</para>
        /// </summary>
        public static string EffectSentence(EffectSpec spec)
        {
            if (spec == null) return "";

            string key = spec.Verb == "draw"
                       ? (spec.Amount > 1 ? "CardEffect/Sentence/DrawMany" : "CardEffect/Sentence/DrawOne")
                       : spec.Verb == "heal" ? "CardEffect/Sentence/Heal"
                       : "CardEffect/Sentence/Damage";
            string tpl = TermOr(key, null);
            if (tpl == null) return spec.Short();        // 缺键：已经出声过了 ⇒ 回引擎原文（⛔ 不编文案）

            tpl = tpl.Replace("{0}", spec.Amount.ToString());
            if (spec.Verb != "draw")
            {
                string who = TargetWord(spec.Target);
                tpl = tpl.Replace("{1}", who.Length > 0 ? who : "?");   // `?` = 认不出目标（同改前 `TargetEn` 的兜底）
            }
            return tpl;
        }

        /// <summary>技能卡面板上的「可选目标数」：原版 `SupportMethods.GetTargetsAvailableText(n)` =
        /// `GetTranslation("Battle/HUD/TargetsAvailable")` 之后 **`String.Replace("{0}", n)`**
        /// （方法体 + 那个 `"{0}"` 字面量都亲读过，出处写在 `Core/Loc.cs` 那一块）⇒ 词条值是**带占位符**的。
        /// <para>🔴 **2026-10-18（第十三轮 · G2b）**：这里原来拼的是 `可选目标 N` / `N available`
        /// （`Zh` 二选一）—— 现在走词条 ⇒ 两档都跟语言走，中文是 `可用 N`（`zh_CN.csv:49` 的同一个模式）。
        /// ⚠️ 那道字体闸照旧（见 <see cref="Term"/>）；占位符**只有这一个口**替换。</para></summary>
        public static string TargetsAvailable(int n)
        {
            string s = Term("Battle/HUD/TargetsAvailable");
            // 词条值 = `{0} available` / `可用 {0}`；表里万一没有 ⇒ `Term` 已回键名（不静默），这里原样返回
            return s.IndexOf("{0}", System.StringComparison.Ordinal) >= 0
                 ? s.Replace("{0}", n.ToString())
                 : s;
        }

        /// <summary>
        /// 表里**所有**中文串。给 `TmpSetup` 建字体资产时统计要烘哪些字用 ——
        /// **不看当前语言**（建资产那会儿字体还不存在，`Zh` 必然是 false）。
        /// 以后加卡/加关键词，字会自动进语料，不会漏（「两处写同一条规则 = 迟早不一致」）。
        ///
        /// <para>🔴 **2026-10-10（`A1084`）**：本文件那几张表已**整体搬进 `Core/Loc.cs`**
        /// ⇒ 语料**只剩一个来源** = `Loc` 的中文列。⛔ 别在这儿再手写第二份（那又是两条路，
        /// 而且搬表这件事的全部风险就在这一条上：**搬走而语料没跟着走 ⇒ 覆盖自检静默失去覆盖面**）。</para>
        /// <para>✅ **覆盖面只多不少（逐条核过）**：改前语料 = `Names`(26) ∪ `KeywordNames`(12) ∪
        /// `Phrases`(20) ∪ `FactionNames`(16) ∪ `Loc.AllChinese()` ∪ 那 16 个效果词常量；
        /// 搬完这些**全在 `Loc` 里** ⇒ `Loc.AllChinese()` 是它的**超集**。
        /// 顺带把改前**漏收**的那一批也收进来了：`KeywordZhNames`（74 个关键词的中文名，
        /// 改前只收了 `KeywordNames` 那 12 个）—— 那是搬表**之前就存在**的一个覆盖缺口。</para>
        /// <para>⚠️ 这条自检**只在建字体资产时跑**（`Editor/TmpSetup.Corpus()`），而字体资产是 TMP
        /// **Dynamic**（运行期按需补字形）⇒ 漏字的后果**不是立刻印方块**，而是**这条自检静默失去覆盖面**
        /// （`CheckCoverage` 只会说「全过」）。⇒ 改完语料要重跑一次 `TmpSetup.BuildCjkFontAsset`。</para>
        /// <para>⚠️ 语料**永远是中文**（建资产那会儿字体还不存在）⇒ ⛔ 别给它加语言闸（见本文件头那条）。</para>
        /// </summary>
        public static IEnumerable<string> AllChinese()
        {
            foreach (var v in Loc.AllChinese()) yield return v;
        }
    }
}
