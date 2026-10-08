// CardIcons.cs — 「卡面效果文字里的图标」的运行时入口（2026-09-15）
//
// **它干什么**：把效果文字里的**记号**换成 TMP 的行内 sprite 标签。
//
//     "Give +3 [Attack], +3 Ranged or +3 Health to a friendly troop"   ← `DA44` 的**真** desc
//       ↓ Rewrite("DA44", "desc", …)
//     "Give +3 <link=melee><sprite name=\"Melee\"></link>, +3 Ranged or +3 Health to a friendly troop"
//     （方括号那一支是「整串换掉」⇒ 记号不再出现；`<link>` **只包一层**、判据见 `Rewrite` 里那条 2026-10-20 的注释）
//
// 🔴 **2026-10-20（A969）**：上面那行里的 `[Armor]` 是**旧数据的写法** —— 卡池里现在写的是**裸词 `Ranged`**，
//    而 `[Armor]`/`[Armour]` 在 1126 张卡里**一处都没有**了。⇒ 那个位置本该画**第二枚图标（紫圈枪）**
//    （亲读 `D:/2/Warpforge部队卡片/Dark Angels/4计策/Warpforge_44_Ancient-Reliquary.png`：`+3〔拳〕, +3〔枪〕`、
//    卡面上**没有** `Ranged` 这个词），现在却印着裸词。**缺口在 `工具/gen_icon_plan.py` 那张表**（不在本文件）。
//
// **为什么记号不能按名字查**：`[Attack]` 这类 token **不是原版数据**，是卡图 OCR 猜的 ——
// 同一个 `[Attack]` 在 `DA44` 上是【拳】、在 `EC8 Alluress` 上是【枪】。所以答案是一张
// **按卡**的表：`Resources/card_icon_plan.json`（由 `工具/gen_icon_plan.py` 生成，每条带证据）。
// 那张表的来龙去脉、现状、以及**记号在卡面上怎么用**（方括号=换掉 / 裸词=插在词前 /
// 符号与「数字烘在图里」的=吃掉），见 `资料/卡面图标_对照与缺口.md`（**第三节**判据）。
//
// **查不到就原样返回**（记号照字面留在文本里）—— 宁可难看，也不猜一个错的图标（工程红线）。
// 计划表里 `sprite` 为空串时这里会**打一次警告**再原样返回（现状 0 处，见那张对照文档）。
//
// 图标资源本身：`Resources/Fonts/Warpforge Trait TextSprites.asset`（照抄原版的 TMP sprite asset，
// 建法与参数出处见 `Editor/IconSetup.cs`）。**它取不到时整个 Rewrite 退回原文**，不静默变空白。
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CardPresentation
{
    public static class CardIcons
    {
        /// <summary>sprite asset 在 Resources 下的路径（和 `IconSetup.SaResourcePath` 是同一份，改一边要改另一边）</summary>
        public const string SpriteAssetPath = "Fonts/Warpforge Trait TextSprites";
        /// <summary>计划表（运行时那份，`gen_icon_plan.py` 和 `数据/` 下那份一起写）</summary>
        public const string PlanPath = "card_icon_plan";

        static TMP_SpriteAsset _sa;
        static bool _triedSa;

        /// <summary>图标 sprite asset。没有返回 null（调用方用 <see cref="Available"/> 判）</summary>
        public static TMP_SpriteAsset SpriteAsset
        {
            get
            {
                if (!_triedSa)
                {
                    _triedSa = true;
                    _sa = Resources.Load<TMP_SpriteAsset>(SpriteAssetPath);
                    if (_sa == null)
                        Debug.LogWarning("[CardIcons] 找不到 sprite asset `Resources/" + SpriteAssetPath +
                                         "` —— 卡面图标画不出来。" +
                                         "重建：-executeMethod IconSetup.Run");
                }
                return _sa;
            }
        }

        /// <summary>能不能走图标渲染。不能的话调用方**照旧画纯文本**（别静默变空白）</summary>
        public static bool Available { get { return SpriteAsset != null; } }

        // ── 计划表（JSON 是**数组形状**的，就为了这里一句 FromJson —— 字典 JsonUtility 读不了）──
        [System.Serializable] class Plan { public int version; public string note; public Card[] cards; }
        [System.Serializable] class Card { public string id; public Field[] fields; }
        [System.Serializable] class Field { public string name; public Item[] items; }
        [System.Serializable] class Item { public string token; public string sprite; public string why; }

        static Dictionary<string, Dictionary<string, Item[]>> _plan;
        static int _itemCount;

        static void LoadPlan()
        {
            if (_plan != null) return;
            _plan = new Dictionary<string, Dictionary<string, Item[]>>();
            var ta = Resources.Load<TextAsset>(PlanPath);
            if (ta == null)
            {
                Debug.LogWarning("[CardIcons] 找不到 `Resources/" + PlanPath +
                                 "` —— 记号不会被替换（生成：工具/gen_icon_plan.py --write）");
                return;
            }
            var plan = JsonUtility.FromJson<Plan>(ta.text);
            if (plan == null || plan.cards == null) return;
            foreach (var c in plan.cards)
            {
                var fields = new Dictionary<string, Item[]>();
                foreach (var f in c.fields) { fields[f.name] = f.items; _itemCount += f.items.Length; }
                _plan[c.id] = fields;
            }
        }

        /// <summary>计划表里有多少张卡 / 多少处记号（自检用）</summary>
        public static int PlanCardCount { get { LoadPlan(); return _plan.Count; } }
        public static int PlanItemCount { get { LoadPlan(); return _itemCount; } }

        static readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>
        /// 🔴 **含图标时字号该不该放大 —— 不放大（恒返回 1）。**
        ///
        /// 一度以为要放（图标按世界单位摆、`FitToBox` 量不到它），**实测是错的**：
        /// 把字号乘 1.156 之后，行**更宽** ⇒ 折行变多 ⇒ 整块更高 ⇒ `FitToBox` 反而把字号往死里缩
        /// （实测 `Heavy Intercessor` 从正常的 2.49 掉到 1.02、`Lychguard` 从一行变两行）。
        ///
        /// 真因是**图标本来就随字号缩**：`glyph` 的缩放已经由 `IconSetup.CalibScale` 标定好，
        /// 「图标 ÷ 大写」是个**常数 2.0**，跟字号无关。所以字号该由**文字**自己定，
        /// 图标跟着走就行 —— **不需要任何补偿**。
        ///
        /// 这个函数留着是为了：① 把这段结论记在代码里，别再试一遍；② `Label` 那边也调它。
        /// </summary>
        public static float FontScaleFor(string text)
        {
            return 1f;
        }

        /// <summary>这段文字里有没有**富文本标签**（`&lt;sprite name="…">` / `&lt;nobr>` / `&lt;link=…>` …）</summary>
        public static bool HasIcons(string text)
        {
            return !string.IsNullOrEmpty(text) && text.IndexOf('<') >= 0;
        }

        /// <summary>
        /// 把**富文本标签**全部剥掉（返回纯文字）。
        /// 🔴 **点阵字库那条兜底路要用它** —— 那条路不认识任何标签，原样喂进去会把
        /// `&lt;sprite name="Melee">` 一个字一个字画出来（`TextCanvas` 的注释：非 ASCII 字形查不到
        /// **只跳格不留痕**，所以画出来是一串空格加乱码）。宁可少个图标，别画一串垃圾。
        ///
        /// 🔴 **2026-09-21 扩过**：原来只剥 `&lt;sprite …>`。而卡面的**关键词段**
        /// （`CardText.KeywordSegment`）现在还会带 `&lt;nobr>`（防图标与词被拆到两行）
        /// 与 `&lt;link=…>`（悬停出 trait tooltip）—— **那两个会原样印到点阵画面上**。
        /// ⇒ 判据改成「剥掉所有 `&lt;…>` 形状的标记」。
        /// ⚠️ 正文里真出现裸 `&lt;` 的概率极低；真出现的话 **TMP 那边也一样会当标签解析**，
        ///    两边行为一致，不会产生「TMP 有、点阵没有」的第三种结果。
        /// </summary>
        public static string StripTags(string text)
        {
            if (!HasIcons(text)) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, "</?[a-zA-Z][^>]*>", "");
        }

        /// <summary>
        /// 图标墨迹高 ÷ 拉丁大写墨迹高（**照成品卡图量的**）。
        /// 尺子 = `D:/2/Warpforge部队卡片/Dark Angels/3部队/Warpforge_12_Aggressor.png`
        /// 那一行 `Strike: Gain 〔questPoints1〕`：图标墨迹 h≈48 px、同行大写高 h≈24 px。
        /// 我方实测（`-executeMethod IconSizeProbe.Run`）：图标 0.38 / 大写 0.19 —— **分毫不差**。
        /// ⇒ 这条比例**与字号无关**（图标跟着字号一起缩），见 <see cref="FontScaleFor"/>。
        /// </summary>
        public const float IconToCap = 2.0f;

        /// <summary>
        /// 把这段效果文字里的记号换成图标标签。
        /// `field` 传 `"desc"` 或 `"descZh"`（两张表分开定——中文和英文的记号不一定对得上）。
        ///
        /// 🔴 **四类记号的处理方式不一样**（计划表 **2121 处 / 654 张** —— 按当前
        /// `Resources/card_icon_plan.json` **离线复算**：**「数字烘在图里」111 处（57 张）/
        /// 裸关键词 1321 处（476 张）/ 方括号 628 处 + 符号 61 处 = 整串换掉 689 处（258 张）**；
        /// 111+1321+689 = **2121** ＝ 计划表条目总数，闭上的）：
        /// ⚠️ **这一段的数字被订正过两次（铁律 5 留痕）**：原来写「1606 处 / 576 张 / 106 / 1306/
        /// 194」—— 那是 2026-10-20 `工具/gen_icon_plan.py` **重新生成计划表之前**的口径
        /// （数据侧那批把记号从 1625 处补到 2121 处，见 `资料/普查产出_1018第三会话/W_卡面图标2.md`）；
        /// 再早还写过「1369 处 / 195 / 1095 / 79」。**计划表一变这三个读数就要重算**，
        /// 别从旧文档里抄。
        /// · **`[Attack]` 这类方括号记号** —— 那是**卡图 OCR 猜出来的占位**，
        ///   不是卡面上印的字。原版卡面上那个位置**只有图标**。⇒ **换掉**（记号不再出现）。
        /// · **`Waystone.` / `路标石。` 这类裸关键词** —— 那是**卡面上真印着的字**。
        ///   原版印的是「**图标 + 紧跟那个词**」。⇒ **在它前面插入图标、词留着**。
        ///   ⚠️ 一开始两种都当「换掉」处理，结果关键词整串消失、卡面只剩图标（`Lychguard` 实测）。
        /// · **`☀` / `①` 这类符号**（**首字符不是字母/数字**）—— 那个字符**就是那张图**
        ///   （OCR 把它抄成了字符）⇒ **吃掉**。留着的话卡面会「图标 + 那个字符」画两遍。
        /// · **数字烘在图里的**（`SpiritStone_1..5` / `questPoints1..3`）—— 文本里那个数字要
        ///   **连 token 一起**吃掉（`1 Quest Point` 的 `1` 已经印在图上了）。
        ///   判据是「**那个数字就在图名里**」，**不是**按 token 名猜。
        /// </summary>
        public static string Rewrite(string cardId, string field, string text)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(cardId)) return text;
            if (!Available) return text;
            LoadPlan();

            Dictionary<string, Item[]> fields;
            if (!_plan.TryGetValue(cardId, out fields) || fields == null) return text;
            Item[] items;
            if (!fields.TryGetValue(field, out items) || items == null) return text;

            // ⚠️ **长的先换**：短 token 是长 token 的子串时（`Armour` ⊂ `Armour 1`），
            //    先换短的会把长的那条打散、它再也匹配不上。
            //    同长时按 token 排序 —— `List.Sort` **不稳定**，不补这一条的话
            //    两份内容一样的数据可能换出不同结果（不可复现）。
            var order = new List<Item>(items);
            order.Sort((a, b) =>
            {
                int c = (b.token ?? "").Length.CompareTo((a.token ?? "").Length);
                return c != 0 ? c : string.CompareOrdinal(a.token ?? "", b.token ?? "");
            });

            string s = text;
            foreach (var it in order)
            {
                if (string.IsNullOrEmpty(it.token)) continue;
                if (string.IsNullOrEmpty(it.sprite))
                {
                    // 缺口（原版就没认定 / 图集里没有那张图）：**原样留着**，并说一次
                    if (_warned.Add(cardId + "|" + field + "|" + it.token))
                        Debug.LogWarning("[CardIcons] " + cardId + " " + field + " 的 " + it.token +
                                         " 没有对应图标（见 `资料/卡面图标_对照与缺口.md` 第五节）—— 按文字画。");
                    continue;
                }
                // 🆕 2026-09-21：**图标外面包一层 `<link>`** —— 悬停出 tooltip。
                //   原版正文里 `[[枚举名]]` 展开成 `<link=…><nobr>图 + 词</nobr></link>`
                //   （`GameStaticData__TraitNameToString.c:84-109` 拼串、`ModifyLocalization.c:440-456` 替换）；
                //   我们这条本来就是「把 token 换成图」，所以把 link 加在同一处。
                //
                // 🔴 **2026-10-20（A969）改：这里拆成【两份】串** —— 原来只有**一份** `tag`
                //   （预先包好 link），裸关键词那一支又拿它去拼「图标 + 词」、外面再包一层
                //   ⇒ 卡面上是 `<link=X><link=X><sprite name="X"></link>词</link>`
                //   （同一个 id **套了两层**；1321 处 / 476 张，见下面 `裸关键词` 那支）。
                //
                //   **原版只有一层** —— 判据是实读全量反编译（`d:/2/tools/decomp_full/`，
                //   里面的 `_DAT_` 字面量已按 `资料/战斗规则与数值_出处.md` §三 那条路解出来）：
                //     · `GameStaticData__TraitNameToString.c:75-76`（`param_2` = 要不要图标那支）
                //       = `String.Concat("<nobr>" /*0x184237e80*/, _traitTextSprite, 本地化词,
                //                        "</nobr>" /*0x1842cf8e8*/)`
                //     · 同文件 `:91-109` 外面**再包一次、且只包这一次**：
                //       `Concat("<link=" /*0x184237880*/, 枚举名, ">" /*0x18423b778*/,
                //               上一串, "</link>" /*0x1842cf7e8*/)`
                //       ⇒ 原版整项 = `<link={DefinedTrait枚举名}><nobr>{图}{词}</nobr></link>`（**一层**）。
                //     · `ModifyLocalization.c:440-456` 是它的调用点：`[[X]]`
                //       （`"[["`/*0x1842b06f0*/ 与 `"]]"`/*0x1842b9628*/）→ `TraitNameToString(X, true)`，
                //       替换进正文的是**那一整串**（所以原版正文里也只有一层）。
                //   ⇒ 所以：`spriteTag` = **未包 link** 的那一份（给「图标 + 词」拼项用），
                //            `tag`       = 外面套一层 link 的那一份（方括号 / 符号 / 数字那三支直接用）。
                //   ✅ **2026-10-20（`A986②`）：原版那一项里的 `<nobr>` 我们【补上了】** ——
                //      这条注释原来写着「我们这条支里没有 `<nobr>`……这是另一条账，本轮没动」，
                //      **现在那笔账做完了**：裸关键词那一支的拼项改成 `<nobr>{图}{词}</nobr>`
                //      （形状与 `CardText.KeywordSegment:290` 一致），细节/折行影响见下面那一支的注释。
                //      ⚠️ 方括号 / 符号 / 数字那三支**照旧不加** —— 原版那三处没查到对应物（见那条注释）。
                string spriteTag = "<sprite name=\"" + it.sprite + "\">";
                string linkId = Badges.KeyOf(it.sprite);
                string tag = string.IsNullOrEmpty(linkId)
                           ? spriteTag
                           : "<link=" + linkId + ">" + spriteTag + "</link>";
                bool stone = it.sprite.IndexOf("SpiritStone", System.StringComparison.Ordinal) >= 0;

                // ⚠️ **幂等**（见下面裸关键词那条的注释）：这段文字可能已经换过一遍了。
                //    方括号那条换完**词是不留的**，所以「tag + token」查不到 ⇒ 单靠它判不出来；
                //    这里改成**整串里没有这个 token 了就跳过** —— 换过的那遍已经把 token 吃掉了。
                if (s.IndexOf(it.token, System.StringComparison.Ordinal) < 0) continue;

                // 这个 token 是**卡面真印着的字**（要留着），还是**图标自己的那个字符/数字**（要吃掉）？
                //   · 首字符是**字母或数字** ⇒ 是词（`Waystone.` / `Rally:` / `攻击`）⇒ **留着**
                //   · 首字符**不是**（符号、圈码）⇒ 它就是图标本身（`☀`、`⚡`、`①`）⇒ **吃掉**
                //     ⚠️ 判据不能写成「单字符」—— `☀`(U+2600) 是单字符，但**圆圈数字 `①`(U+2460)
                //        在中文语境里像数字、不是**；只有前一类的「字母或数字」才该留。
                //     不判的话卡面会变成「图标 + 那个字符」，**同一个东西画两遍**（方框乱码也会跟着出现）。
                char c0 = it.token[0];
                bool symbol = !(char.IsLetter(c0) || (c0 >= '0' && c0 <= '9'));

                // **数字烘在图里的那两种**（`SpiritStone_1..5` / `questPoints1..3`）：
                // 文本里那个数字要**连 token 一起**吃掉，否则会「图标里有 1、旁边又印一个 1」。
                // 判据是「那个数字就在这张图的名字里」——别按 token 名去猜（`1 Quest Point` 里的 `1`
                // 和 `Spirit Stone` 一样都要吃掉）。
                int di = it.sprite.IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
                bool numInArt = di >= 0 && it.sprite.IndexOf(it.token, System.StringComparison.OrdinalIgnoreCase) < 0;

                if (stone || numInArt)
                {
                    // 先把「数字 + 空格 + 整串 token」换掉，再把「数字 + 整串 token」换掉。
                    // ⚠️ 两条都只在**真的匹配得上**时才生效；`①` 那种没有前置数字的走下面普通替换。
                    // 🔴 **必须写 `[0-9]`，不能写 `\d`** —— .NET 的 `\d` 是 **Unicode** 数字类，
                    //    **把 `①`(U+2460) 也算成数字** ⇒ `\d+①` 会把 `①①` 整段吃掉、第一条
                    //    RegEx 变成空操作，然后 `s.Replace("①", 图标)` 又是空操作
                    //    ⇒ **卡面原样印出 `①`**（实测踩过：`Farseer` 的文字里真的留着 `①`）。
                    s = System.Text.RegularExpressions.Regex.Replace(
                        s, @"[0-9]+\s+" + System.Text.RegularExpressions.Regex.Escape(it.token), tag);
                    s = System.Text.RegularExpressions.Regex.Replace(
                        s, @"[0-9]+" + System.Text.RegularExpressions.Regex.Escape(it.token), tag);
                }
                else if (!symbol && !it.token.StartsWith("[", System.StringComparison.Ordinal))
                {
                    // 裸关键词：原版印的是「图标 + 紧跟那个词」⇒ **插在前面，词留着**
                    // ⚠️ **必须幂等** —— 这段文字可能已经换过一遍了（`SetData` 会把同一份
                    //    `CardData` 再喂给卡面一次）。不判的话第二遍会给**已经带图标的词**
                    //    再插一个图标，还会把方括号那条已经换好的结果当成裸词再插一次
                    //    （实测：`[践踏]` 第二遍变成 `<sprite>……<sprite>践踏`）。
                    // 🆕 2026-09-21：**词也一起包进来**（原版整项就是一个 `<link>`）——
                    //    悬停**图标或那个词**都能出 tooltip。
                    // 🔴 2026-10-20（A969）：拼项用**未包 link 的 `spriteTag`**、外面**只包这一层**
                    //    ⇒ 形状 = `<link=X><nobr><sprite name="X">词</nobr></link>`
                    //    （`<nobr>` 那一层见下条 `A986②`；`<link>` 这一层 = 原版那唯一一层）。
                    //
                    // 🆕 **2026-10-20（`A986②`）：再补 `<nobr>`（原版整项就是这三层）**
                    //    · **判据（原版，全量反编译 `d:/2/tools/decomp_full/`）**：
                    //      `GameStaticData__TraitNameToString.c:75-76` 拼的是
                    //      `Concat("<nobr>" /*0x184237e80*/, 图, 词, "</nobr>" /*0x1842cf8e8*/)`，
                    //      同文件 `:91-109` **外面再包一次、且只包这一次** `<link={枚举名}>…</link>`
                    //      ⇒ **原版整项 = `<link={枚举名}><nobr>{图}{词}</nobr></link>`**；
                    //      调用点 `ModifyLocalization.c:440-456` 把正文里的 `[[X]]` 换成这一整串
                    //      ⇒ 正文里也只有这一层。上面那条 `A969` 的注释已把这两段实读过，
                    //      它当时把「补 `<nobr>`」记成另一条账（那条账现在做完了）。
                    //    · **作用**：不加的话 TMP 会把**图标与词拆到两行**（图标留在上一行行尾、
                    //      词掉到下一行）；`CardText.KeywordSegment:288-290` 早就这么写了，
                    //      两条路（关键词段 / 正文记号）现在**同形**。
                    //    · 🔴 **这一改会改折行** —— 「不许在图标与词之间断行」是一条**新的排版约束**，
                    //      正文比原来更难排 ⇒ 卡面可能**多折一行**；而卡面字号是 `FitToBox` 量的
                    //      （见 `FontScaleFor` 那条注释）⇒ **凡是画卡面的宿主都要复跑版面自检**。
                    //      本支覆盖 **1321 处 / 476 张**（见本函数头注释那张按分支口径的复算表）。
                    //    · ⚠️ **方括号 / 符号 / 「数字烘在图里」那三支【不加】** —— 原版那三处
                    //      **没查到对应物**（那三支是我们这边 OCR 记号的处置，原版没有「记号」这回事），
                    //      照铁律 2「查不到就别照着猜」，本轮**没动**。
                    string bare = "<nobr>" + spriteTag + it.token + "</nobr>";
                    if (!string.IsNullOrEmpty(linkId))
                        bare = "<link=" + linkId + ">" + bare + "</link>";
                    // 🔴 **幂等判据 = 结果里已经有【一模一样的那一项】`bare` 就跳过**（`②g` / `②h` 钉它）。
                    //    两遍都成立，理由是**结构性**的：插入用的就是 `bare` 这个串**本身**
                    //    （`s.Replace(it.token, bare)` 的输出里必然含 `bare`）⇒ 第二遍一定查得到。
                    //    ⚠️ 判据**必须**跟着形状一起改 —— 这里历史上换过两次形状（双层 → 单层 → 加 `<nobr>`），
                    //       每一次都有一条按旧形状写的判据会**判不中**、于是同一个图标**再插一遍**
                    //       （`A969` 那个双层 bug 就是这么来的，所以 `A969` 当时特意写了这条注释）。
                    //    ⚠️ 上面那道守卫（`s.IndexOf(it.token) < 0 ⇒ continue`）**单独不够**：
                    //       本支**把词留着**（`bare` 里就含 `it.token`）⇒ 第二遍那个 token 照样找得到。
                    if (s.IndexOf(bare, System.StringComparison.Ordinal) >= 0) continue;
                    s = s.Replace(it.token, bare);
                    continue;
                }
                s = s.Replace(it.token, tag);
            }
            return s;
        }
    }
}
