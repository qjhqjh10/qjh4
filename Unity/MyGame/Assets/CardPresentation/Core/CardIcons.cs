// CardIcons.cs — 「卡面效果文字里的图标」的运行时入口（2026-09-15）
//
// **它干什么**：把效果文字里的**记号**换成 TMP 的行内 sprite 标签。
//
//     "Give +3 [Attack], +3 [Ranged] or +3 Health to a friendly troop"   ← `DA44` 的**真** desc（**现读**）
//       ↓ Rewrite("DA39", "desc", …)
//     "Give +3 <link=melee><sprite name=\"Melee\"></link>, +3 <link=ranged><sprite name=\"Ranged\"></link> or +3 Health to a friendly troop"
//     （方括号那一支是「整串换掉」⇒ 记号不再出现；`<link>` **只包一层**、判据见 `Rewrite` 里那条 2026-10-20 的注释）
//
// 🔴 **更正（`A1372` · 铁律 5）：上面这两行【原来都写错了】**（输入写成裸词 `+3 Ranged`、输出里也只换了第一枚），
//    而且下面那段「缺口在 `工具/gen_icon_plan.py`」的话**三条前提今天全不成立**。
//    现读（可复核）：`cards_engine.json` 的 `DA44.desc` =
//      `Give +3 [Attack], +3 [Ranged] or +3 Health to a friendly troop`（**带方括号**）；
//    `Resources/card_icon_plan.json` 的 `DA44` / `desc` 子里 `[Attack]→Melee` 与 `[Ranged]→Ranged` **两条都在**
//    ⇒ 卡面上那两处**都换成了图标**（亲读 `D:/2/Warpforge部队卡片/Dark Angels/4计策/Warpforge_44_Ancient-Reliquary.png`：
//    `+3〔拳〕, +3〔枪〕`、卡面上**没有** `Ranged` 这个词 —— 判据是铁律 7，
//    `[Armor]`/`[Armour]` 在 `+N Attack … +N Armour` 这个固定搭配里就是**枪**）。
//    ⇒ **缺口已闭合**，本文件原来那段是【过期的缺口描述】。
//    同一条已由 `A1361` 在 `Editor/IconSetup.cs` 的 ②c 那两段里就地更正过（那里标的日期是 2026-10-21）；
//    **本文件是同一句话的第二个副本**（铁律 5：同一句被复制到别处的地方一起改）。
//    ⚠️ 例子里那个 `[Armor]` 只是**老 OCR 数据**的写法（`[Armor]`/`[Armour]` 现在全池仍是 0 处）——
//    它写在这里只当「同一段里 `[Attack]` 到底是拳还是枪」这个坑的引子，**不是今天的缺口**。
//
// **为什么记号不能按名字查**：`[Attack]` 这类 token **名字不可信** —— 同一个 `[Attack]` 在 `DA44` 上是
// 【拳】、在 `EC8 Alluress` 上是【枪】（老数据里两个意思都有：`工具/gen_icon_plan.py:369-372`）。
// 所以答案是一张**按卡**的表：`Resources/card_icon_plan.json`（由 `工具/gen_icon_plan.py` 生成，
// 每条**带证据**，并与逐张看卡图抄的 `资料/卡表核对_卡图提取/_还原效果文字.md` 交叉核对过）。
//
// 🔴 **2026-10-14（第二十九会话 · `A1771`）更正 —— 这一段原来那两句【都不成立】了**：
//   原句 = 「`[Attack]` 这类 token **不是原版数据**，是**卡图 OCR 猜的**」。要改口的两处：
//   ① **原版【有】结构化的图标记号** —— 官方 `Card_Description/*` 的正文本身就写着 `[[armour]]` /
//      `((Melee_attack_icon_x,+1))` 这种记号（`数据/游戏数据/官方本地化_en+zh-CN.json`，
//      2026-10-14 现数：EN 列 `[[armour]]` **115** 处 · `((Melee_attack_icon_x,+1))` **65** 处）
//      ⇒ ⛔ 别再说「原版没有效果文字图标的结构化数据」；我们这套 `[Attack]` **只是我们自己那一代转录的形态**。
//   ② **「猜」只对 OCR 那一代的【名字】成立，对今天的【答案】不成立** —— 今天的答案有硬判据：
//      · `card_icon_plan.json` 每条带证据（逐张卡图亲读）；
//      · **属性那一批记号是我们自己写的**（`数据/游戏数据/cardface_fixes.json` 的
//        `_2026-10-18_属性图标方括号`；说明见 `工具/gen_icon_plan.py:1957-1980`），英文侧该写 `[Attack]`
//        还是 `[Melee]` 由引擎**唯一**的属性词表定 —— `GivePayload.ReAttr`
//        （`RuleEngine/Core/GivePayload.cs:212`）+ `NormAttr`（同文件 `:261-271`）。
//   ⇒ **结论不变**（名字不可信 ⇒ 只按卡查）；**退场的是「原版没有 / 全靠猜」这两句**。
//   ⏭ **本层是过渡件**：等官方 `Card_Description` 换进来（`A1763` 要给 `Rewrite` 接 `[[…]]`/`((…))`
//      两套记号），按方括号的这一支就该**整体退场** —— 账 = `A1771`（⛔ 本轮**只改注释**，代码没动）。
//      判据全文 → `资料/普查产出_第二十七会话/兜底自设计_总清单.md:118`（§Ｄ 那一行）+ 同文件 `:238-239`。
// 按卡那张表（`card_icon_plan.json`）的来龙去脉、现状、以及**记号在卡面上怎么用**（方括号=换掉 / 裸词=插在词前 /
// 符号与「数字烘在图里」的=吃掉），见 `资料/卡面图标_对照与缺口.md`（**第三节**判据）。
//
// **查不到就原样返回**（记号照字面留在文本里）—— 宁可难看，也不猜一个错的图标（工程红线）。
// 计划表里 `sprite` 为空串时这里会**打一次警告**再原样返回（**处数别写死** ——
// `工具/gen_icon_plan.py` 每跑一次都会把「没认出来的」那一行打出来，以它为准；历史上这里写过「现状 0 处」，
// 那种写法一改数据就烂，见 `Rewrite` 里那段 2026-10-20 的注释）。
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
        /// 🔴 **四类记号的处理方式不一样**（**口径**如下；**处数 / 卡数一律不写在这里** ——
        /// 它们随每一次数据改动而变，**以 `工具/gen_icon_plan.py` 的自报 + `资料/卡面图标_对照与缺口.md`
        /// 的逐卡明细行数为准**，那两处是生成器自己算的、不会烂）：
        /// ⚠️ **铁律 5 留痕**：这一段历史上依次写过 `1369 处 / 195 / 1095 / 79` → `1606 处 / 576 张 / 106 /
        /// 1306 / 194` → `2121 处 / 654 张 / 111 / 1321 / 689`；`A1372` 现读是 **717 张卡 / 2376 处条目**
        /// （方括号 661 / 裸关键词 1645 / 首字符非字母数字 70）—— **四个读数、四个时点，全是「当时对、隔天烂」**
        /// ⇒ 从此改成「**口径 + 指针**」，⛔ 别再往这里填一个数字。
        /// **四类的处数之间没有固定的比例关系**，⛔ 别按某一版的读数去反推今天的。
        /// 四类的**口径**直接读下面那四条（就是 `Rewrite` 里的四个分支），这里不另抄一份。
        /// · **`[Attack]` 这类方括号记号** —— 那是**我们这边的占位**，**不是卡面上印的字**：
        ///   卡面上那个位置**只有图标**（原版正文用的是 `[[armour]]` / `((Melee_attack_icon_x,+1))`
        ///   这种 I2 记号 —— 见文件头那条 `A1771` 的更正）。⇒ **换掉**（记号不再出现）。
        ///   ⚠️ **2026-10-14（`A1771`）就地改过这一句的措辞**：原句是「那是**卡图 OCR 猜出来的**占位」——
        ///   那对 OCR 那一代的**名字**成立（`[Health]` 画的是拳、`[Power]` 画的是枪），
        ///   但今天的**答案不是猜的**（按卡查 + `GivePayload.ReAttr` 词表）。**完整更正与出处见文件头**
        ///   （⛔ 别在这里再抄一份 —— 两处写同一条规矩迟早不一致）。
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
            if (string.IsNullOrEmpty(text)) return text;
            if (!Available) return text;

            // ★★★ 2026-10-14（`A1763` 的**代码层** + `A1945`）：**官方模板文本【不进】自造那一层**。
            //   判据：正文里带 `[[` / `((` / `$$` ⇒ 那就是原版 I2 模板（`ModifyLocalization` 那四支的输入），
            //   ⇒ 整串交给 `RewriteMarks`（本文件末尾那一片）**按原版四支渲染**；
            //   ⛔ **不再往下走那张按卡的 `card_icon_plan`** —— 那份表的 token（`[Attack]` / `①` / `路标石。`）
            //   全是**我们自造的记号**，把官方模板文本喂给它是两个世代的表配错文本（`资料/普查产出_第三十会话/
            //   卡表块_施工设计_1014.md` §五·11 点名过这一条）。
            //
            //   🔴 **这就是 `A1945` 那句「方括号那一层退场」的落地形态** —— 分清「退场」不是「删代码」：
            //     · **删掉**下面那一支 = 卡面**立刻掉光图标**：今天的 `desc`/`descZh` 仍是**自造记号那一代**
            //       （官方文本要等 P3 的数据层才灌进来）⇒ 删了 9 张卡一行字都带不出图标，那是**退步**；
            //     · **按文本世代分流**（本行）= 官方文本来了就**不再经过**自造层 ⇒ 退场目的达到、且零退步。
            //   ⚠️ 门只认那三对标点：`(N)`（费用图标那一支）**单独出现时不算** —— 原版那一支在**全表 9441 条里
            //   0 处输入**（现数），不为它单独开门；`RewriteMarks` 内部那一支照旧按原版实现并带「图不在就留着」的闸。
            if (HasOfficialMarks(text)) return RewriteMarks(text, field == DescZhField);

            // 🆕 2026-10-19（`A2140`）：**信仰费写成裸词的** —— `N Energy:` / `N 能量：`
            //   卡面真印的是 `N〔金太阳〕:`（**数字留着、词换成图标**）。
            //   这一条必须在按卡表**之前**跑：它不吃 `card_icon_plan` 的条目，只按句法定位。
            //   判据与「为什么只认冒号」写在 <see cref="RewriteFaithCost"/> 的 doc 里。
            text = RewriteFaithCost(text, field == DescZhField);

            if (string.IsNullOrEmpty(cardId)) return text;
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
                //
                //       🔴 **2026-10-14 就地更正（`A1763` 的 P0 那轮现核 · 铁律 5）—— 上面这一行两处不准**：
                //         ⓐ **`param_2` 不是「要不要图标」**（两个分支都插图标），它是**加粗开关**；
                //         ⓑ **`:75-76` 是 `param_2 == false` 那支**（`:43` 判 `param_2 != 0` 走 `:44-67` 的
                //            5 项数组 `<nobr>`/图/`<b>`/词/`</b></nobr>`），而 `ModifyLocalization.c:455`
                //            的调用点**传的是 1**（真）⇒ **原版正文里那一项是带 `<b>` 的**。
                //         ⇒ 完整形状与三档（有词条加粗 / 有词条不加粗 / 没词条只印枚举名）写在下面
                //            `RewriteMarkTraits` 的 doc 里（**本函数的这一支是我们自己的形状、有断言钉着，
                //            ⛔ 不动**；更正只落在注释）。
                //     · 同文件 `:91-109` 外面**再包一次、且只包这一次**：
                //       `Concat("<link=" /*0x184237880*/, 枚举名, ">" /*0x18423b778*/,
                //               上一串, "</link>" /*0x1842cf7e8*/)`
                //       ⇒ 原版整项 = `<link={DefinedTrait枚举名}><nobr>{图}{词}</nobr></link>`（**一层**）。
                //         ⚠️ **2026-10-14 订正**：这一句少了 `<b>`（正解见上面那条 2026-10-14 的 更正 ⓐⓑ）——
                //            原版正文那一项是 `<nobr>{图}<b>{词}</b></nobr>`。**`<link>` 只有一层**这一半点没错。
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
                    //      🔴 **2026-10-14 订正（`A1763` 的 P0 那轮现核）**：这一句少了 `<b>` ——
                    //         `:75-76` 是 `param_2 == false` 那支，而调用点传的是真 ⇒ 正解见上面
                    //         `A969` 那条里的 2026-10-14 更正（⛔ 不在这里再抄一份形状）。
                    //    · **作用**：不加的话 TMP 会把**图标与词拆到两行**（图标留在上一行行尾、
                    //      词掉到下一行）；`CardText.KeywordSegment:288-290` 早就这么写了，
                    //      两条路（关键词段 / 正文记号）现在**同形**。
                    //    · 🔴 **这一改会改折行** —— 「不许在图标与词之间断行」是一条**新的排版约束**，
                    //      正文比原来更难排 ⇒ 卡面可能**多折一行**；而卡面字号是 `FitToBox` 量的
                    //      （见 `FontScaleFor` 那条注释）⇒ **凡是画卡面的宿主都要复跑版面自检**。
                    //      本支覆盖 **1321 处 / 476 张**（见本函数头注释那张按分支口径的复算表）。
                    //    · ⚠️ **方括号 / 符号 / 「数字烘在图里」那三支【不加】** —— 原版那三处
                    //      **没查到对应物**（那三支是我们这边占位的处置），照铁律 2「查不到就别照着猜」，
                    //      本轮**没动**。
                    //      🔴 **2026-10-14（`A1771`）就地更正**：这里原来多给了一个理由「原版没有『记号』
                    //      这回事」—— **不成立**（原版正文确实有记号：`[[armour]]` / `((Melee_attack_icon_x,+1))`，
                    //      见**文件头**那条更正）⇒ 那半句删掉；**操作结论不变**（那三支本轮不加 `<nobr>`）。
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

        // ── 🆕 2026-10-19（`A2140`）：**信仰费的裸词 → 信仰图标** ─────────────────────────────
        //
        // **问题**：两张卡的 `desc`/`descZh` 把信仰费写成了裸词 ——
        //   · `f104d4c21abe84546abc9365d9eda8dd Righteous Repugnance`：`… 4 Energy: Give +2 …` /
        //     `… 4 能量：改为给予 …`
        //   · `SOR44 Moment of Grace`：`…\n5 Energy: Draw a card`（中文那半已经是 `5 ☀：`，走计划表）
        //   而卡面印的是 **`4〔金太阳〕:`**、**没有 `Energy` 这个词**。
        //
        // **判据（逐条带出处）**：
        //   ① PnP 成品卡 `D:/2/Warpforge部队卡片/Sorotitas/2天赋/Warpforge_06_Righteous-Repugnance.png`
        //      亲读：`4 〔金太阳〕: Give +2〔拳〕 and +2〔枪〕 instead and Heal them 1`（**词不在**）。
        //      同族 `…/4计策/Warpforge_47_Moment-of-Grace.png`：`5 〔金太阳〕: Draw a card`。
        //   ② 官方表 `数据/游戏数据/官方本地化_en+zh-CN.json`：
        //      `Card_Description/f104…` = `… ((Faith_cost_x,4)): Give ((Melee_attack_icon_x,+2)) …`、
        //      `Card_Description/SOR44` = `… ((Faith_cost_x,5)): ((Draw_a_card))`；
        //      `Modular_Description/Faith_cost_x` = `<nobr><b>{0}<link=faith><sprite name=Atlas_trait_icon_faith></b></nobr></link>`
        //      ⇒ **数字 + 信仰图标**、没有词。（图标名在运行时 sprite asset 里就是 `faith`。）
        //
        // **为什么写成句法规则、而不是再往 `card_icon_plan` 加两条按卡条目**：
        //   · 计划表**表达不了**「把词换成图标」—— 它的裸词分支是「图标 + **留着词**」（见 `Rewrite`
        //     里那条 `bare` 分支的注释），写进去会印成 `4 〔太阳〕Energy:`，比现在更错；
        //   · 这条句法本身有判别力：**只有「数字 + Energy/能量 + 冒号」才是【费用】**。
        //     全池另 6 处 `Refill 1 Energy` / `Gain 1 Energy` / `refill 2 Energy`（DA61/GSC63/SOR31/
        //     UM45/UM32）都是**资源动作**、卡面印的就是词，**不许**被这条吃。
        //     ⇒ 冒号是判据的一部分，⛔ 别把 `(?=\s*:)` 放宽掉。
        //   · 影响面（现读 `cards_engine.json` `count=1127`）：EN 命中 **2 张**（就是这两张）、
        //     ZH 命中 **1 张**（`f104…`；`SOR44` 中文写 `☀` 已由计划表处理）。
        //
        // **幂等**：替换后那个词没了 ⇒ 第二遍 `IsMatch` 为 false。**`desc` 一个字节都不动** ——
        //   本函数只改【渲染出来的串】，`CardDef.Desc` 原样（引擎唯一解析输入）。
        // **与 `A1945` 的关系**：官方文本一旦当上显示源，带的是 `((...))` 记号 ⇒ 在 `Rewrite` 开头
        //   就被 `HasOfficialMarks` 分流走、**根本到不了这里**（不会画两遍）。
        static readonly System.Text.RegularExpressions.Regex _rxFaithCostEn =
            new System.Text.RegularExpressions.Regex(@"[0-9]\s*Energy(?=\s*:)",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        static readonly System.Text.RegularExpressions.Regex _rxFaithCostZh =
            new System.Text.RegularExpressions.Regex(@"[0-9]\s*能量(?=\s*[：:])",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>把 `<c>N Energy:</c>` / `<c>N 能量：</c>` 里的**词**换成信仰图标（数字保留）。
        /// 见上面那一段的判据；没有匹配时**原样返回**（零拷贝路径）。</summary>
        static string RewriteFaithCost(string text, bool zh)
        {
            var rx = zh ? _rxFaithCostZh : _rxFaithCostEn;
            if (text.IndexOf(zh ? "能量" : "Energy", System.StringComparison.Ordinal) < 0) return text;
            if (!rx.IsMatch(text)) return text;

            string spriteTag = "<sprite name=\"faith\">";
            string linkId = Badges.KeyOf("faith");
            string tag = string.IsNullOrEmpty(linkId)
                       ? spriteTag
                       : "<link=" + linkId + ">" + spriteTag + "</link>";
            // 用 MatchEvaluator 而不是替换串：`tag` 里有 `<`/`"`，也不让 `$` 有机会被当分组引用。
            // 匹配到的是「数字 + 空白 + 词」⇒ 去掉词、**数字与空白逐字保留**。
            return rx.Replace(text, m =>
            {
                string v = m.Value;
                int cut = v.Length - (zh ? 2 : 6);   // 「能量」2 个字符 / "Energy" 6 个
                return v.Substring(0, cut) + tag;
            });
        }

        // ==========================================================================================
        //  官方 I2 记号渲染层  ——  P0 · 账 `A1763`（代码层）· 前置 `A1945`   （2026-10-14）
        // ==========================================================================================
        //
        //  **这一片干什么**：把**原版自己的记号**渲染成 TMP 能画的东西。上面那条（`card_icon_plan`）
        //  是**我们自造记号**那一代的处置；这一片是**原版那一代**的处置 —— 两个世代靠
        //  <see cref="HasOfficialMarks"/> 分流（见 <see cref="Rewrite"/> 里那条注释）。
        //
        //  🔴 **判据（唯一）= 全量反编译 `d:/2/tools/decomp_full/SupportMethods__ModifyLocalization.c`**
        //    （497 行）。⚠️ 文件名与内容**现读核对过**：里面就是 `SupportMethods.ModifyLocalization(string)`
        //    （本仓踩过 6 例「文件名与内容对不上」⇒ 按名字找到后必须现读确认）。
        //    它按**固定顺序**跑四支，**每一支都在上一支改过的串上继续**（`local_res8` 一路传下去）：
        //
        //      ① `:75-186`   `((Key,a,b))` → `Modular_Description/<Key>` 的本地化，`{0}`/`{1}` 代参
        //      ② `:188-319`  `$$ID$$`      → `Card_Name/<ID>` 的本地化
        //      ③ `:320-467`  `[[枚举名]]`  → `GameStaticData.TraitNameToString(值, true)`
        //      ④ `:469-487`  `(N)`（N=0..8）→ `" <sprite name=ManaN>"`（**前导空格是真的**）
        //    ⇒ 本文件**照这个顺序**实现（<see cref="RewriteMarks"/>）。
        //
        //  **`_DAT_xxxxxxxx` 字面量怎么读出来的（可复核）**：解码路 = `资料/战斗规则与数值_出处.md` §三
        //    （`RVA = 地址 − 0x180000000` → PE 节表 → 文件偏移 → 从 `d:/2/unity_run_ref/GameAssembly.dll` 读）。
        //    ⚠️ 反编译里偏移**常写成十进制**（`+ 300` 就是 `0x12C`）⇒ `grep 0x12c` 会漏。
        //    本层用到的每一条（**地址 → 值**，逐条现读）：
        //      `0x18426a500` = `((`     · `0x184270f48` = `))`   · `0x1842b3a48` = 正则 `\(\((.*?)\)\)`
        //      `0x184237f88` = `Modular_Description/`            · `0x184265d10` = `{0}` · `0x184272af0` = `{1}`
        //      `0x1842b80e0` = `""`（空串，即「把记号剥掉」）
        //      `0x1842552f8` = `$$`     · `0x1842b3848` = 正则 `\$\$(.*?)\$\$` · `0x1842cde40` = `Card_Name/`
        //      `0x18428b900` = `PlayerDataManager not found`（② 那条覆盖路取不到玩家数据时的出声文字）
        //      `0x1842b06f0` = `[[`     · `0x1842b9628` = `]]`   · `0x1842be020` = `MainMenu/General/`
        //      `0x1842ce340` = `Card_Trait/`  · `0x184238f80` = `<sprite name=Atlas_trait_icon_`
        //      `0x184237e80` = `<nobr>` · `0x1842d26d8` = `<b>` · `0x1842cf1e8` = `</b></nobr>`
        //      `0x1842cf8e8` = `</nobr>` · `0x184237880` = `<link=` · `0x18423b778` = `>` · `0x1842cf7e8` = `</link>`
        //      `0x184269e20` = `(`      · `0x18426f0c8` = `)`  · `0x1842c79a0` = `" <sprite name=Mana"`（**带前导空格**）
        //
        //  🔴 **本层【只做渲染】，一行数据都不动**（`A1763` 的 P0 之所以排在 P1~P3 之前，就是因为它
        //    「不动 `cards_engine.json` ⇒ 零指纹扰动」）。词条从 `Core/Loc.cs` 现有的两个口取
        //    （手写表 `HasEntry` / 数据面 `HasBulk`）—— **词条缺了就把记号原样留着 + 出声**，
        //    ⛔ 不猜、不静默（工程红线）。缺在哪、还差什么，逐条写在下面各支里，
        //    也记进了 `资料/普查产出_第三十一会话/G_卡表P0.md`。

        /// <summary>中文那一档的字段名（`Rewrite` 拿它判语档；与 `BattleDriver.FaceTextFull` 的调用口径同一份）。</summary>
        public const string DescZhField = "descZh";

        /// <summary>
        /// 这段文字里有没有**原版记号**（`[[…]]` / `((…))` / `$$…$$`）。
        /// <para>⚠️ **`(N)`（费用图标）不算** —— 它**单独出现**时文本与我们的自造记号那一代无法区分，
        /// 而它在官方全表的输入是 **0 处**（现数：`数据/游戏数据/官方本地化_en+zh-CN.json` 9441 条里
        /// `(\d)` 命中 0）⇒ 不为它开门。<see cref="RewriteMarks"/> 里那一支照样按原版实现。</para>
        /// </summary>
        public static bool HasOfficialMarks(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf("[[", System.StringComparison.Ordinal) >= 0
                || text.IndexOf("((", System.StringComparison.Ordinal) >= 0
                || text.IndexOf("$$", System.StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// **按原版四支（按序）**把官方记号渲染成 TMP 标签。`zh` = 取中文列还是英文列。
        /// <para>⛔ **不许改这里四支的顺序** —— 上一支的产物会被下一支继续处理，是**原版的设计**：
        /// 例如 `Modular_Description/Blind_a_random_enemy` 的值本身就是
        /// `[[blind]] a random enemy`（现读官方表）⇒ ① 插进来的 `[[blind]]` 由 ③ 收掉。
        /// 顺序倒过来那一处就渲染不出来。</para>
        /// <para>⚠️ **没查清的部分（照实写，⛔ 没猜）**：原版 ① 用的是 `GetTermTranslation`、③ 用的是
        /// `GetTranslation`（两个不同的 I2 口，回退行为可能有别）；我们两边都走 `Loc` 现有的口。
        /// 原版 ② 还有一条「拿玩家收藏里那张卡的 `GetLocalizedCardName` 覆盖词条」的路
        /// （`:275-305`，取不到玩家数据时印 `PlayerDataManager not found`）—— 卡面渲染**没有那个上下文**
        /// ⇒ 我们**只走词条这一路**，这一条如实记在这里。</para>
        /// </summary>
        public static string RewriteMarks(string text, bool zh)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string s = RewriteMarkModular(text, zh);     // ① ((Key,a,b))
            s = RewriteMarkCardRefs(s, zh);              // ② $$ID$$
            s = RewriteMarkTraits(s, zh);                // ③ [[枚举名]]
            s = RewriteMarkMana(s);                      // ④ (N)
            return s;
        }

        static readonly System.Text.RegularExpressions.Regex RxMarkModular =
            new System.Text.RegularExpressions.Regex(@"\(\((.*?)\)\)",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        static readonly System.Text.RegularExpressions.Regex RxMarkCardRef =
            new System.Text.RegularExpressions.Regex(@"\$\$(.*?)\$\$",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        static readonly System.Text.RegularExpressions.Regex RxMarkTrait =
            new System.Text.RegularExpressions.Regex(@"\[\[(.*?)\]\]",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        /// <summary>官方词条值里那些**不带引号**的 `&lt;sprite name=Atlas_trait_icon_x&gt;`（原版写法）。
        /// 我们**保留引号**：`Core/CardText.cs:275` 定过这条口径（「两条路写法要一致」），
        /// 而且 `Editor/IconSetup.cs:23-25` 已经把 `Atlas_*` 全名与短名**都注册**进了
        /// `Resources/Fonts/Warpforge Trait TextSprites.asset` ⇒ 官方那串**不用翻译图名**就能画。</summary>
        static readonly System.Text.RegularExpressions.Regex RxSpriteUnquoted =
            new System.Text.RegularExpressions.Regex(@"<sprite name=(?!"")([^>\s]+)>",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        static readonly HashSet<string> _warnedMark = new HashSet<string>();

        static void WarnMarkOnce(string key, string msg)
        {
            if (_warnedMark.Add(key)) Debug.LogWarning("[CardIcons] " + msg);
        }

        /// <summary>取一条官方词条（跟语档）。**取不到回 null**（⛔ 不返回键名 —— 那是「静默失败」的变体）。
        /// <para>两个口：手写表 `Loc.HasEntry`（`Card_Trait/*` 那一族在这里）· 数据面 `Loc.HasBulk`。
        /// ⚠️ `Loc.BulkOr` 用的是**当前语言**（不是本函数的 `zh`）—— 这半句仍成立，本层只如实吃它的结果。
        /// 🔴 **2026-10-15（`AF` 波）就地订正（铁律 5）**：本段原来的**后半句**写的是
        /// 「而且它自己有一道『值是未渲染记号 ⇒ 当取不到』的闸（`Core/Loc.cs` 那段 doc 写着
        /// 『要真接上，先做 `A1763` 的记号渲染层』）—— 那道闸**留给调度台裁**」
        /// ⇒ **原写「那道闸还在、留给调度台裁」＝实际它当天（`AC` 波）已经拆了**：
        /// `BulkOr` 现在的判据是「**渲染完还有没有记号**」（值带记号时先过 `CardIcons.RewriteMarks`，
        /// 渲不掉才回兜底 + 出声），**不再是**「值里有没有记号」。
        /// 判据与逐条现数（三路各多少、逐条是哪些）→ `资料/普查产出_第三十一会话/AC_Loc记号闸.md`；
        /// 实现 → `Core/Loc.cs:2697-2765`。⚠️ **对本层的影响**：`Loc.BulkOr(term, null)` 现在**会回一个
        /// 已经渲染过的串**（可能是 `&lt;link=…&gt;&lt;nobr&gt;&lt;sprite …&gt;&lt;b&gt;词&lt;/b&gt;&lt;/nobr&gt;&lt;/link&gt;`）——
        /// 本层照旧 `QuoteSprites` + 原样吃它即可；而「渲不掉 ⇒ 回兜底」那一路今天
        /// **0 个活的输入**（剩下 `Modular_Description/Duplicitous` / `Maintenance_x` 两条零引用的键）。</para></summary>
        static string OfficialTerm(string term, bool zh)
        {
            if (string.IsNullOrEmpty(term)) return null;
            // 🆕 2026-10-17（`N42`）：**非中英语档 ⇒ 官方 13 语言表的当前那一列**（中/英档恒 null ⇒ 下面逐字节不变）。
            //   ⚠️ 这一步要排在 `HasEntry` 之前 —— `[[关键词]]` / `$$卡 id$$` 这类嵌套词在日/韩/俄…正文里
            //   也要出**对应语言**，改前它们会落回英/中列（半外半英）。
            //   ⚠️ 本函数收的是**官方词条键**（`Card_Trait/*` / `Card_Name/*` / `Modular_Description/*`），
            //   与 `zh` 这个形参（=「取中文列」）无关：当前语档是哪一列由 `Loc` 自己判。
            string v13 = Loc.Lang13Current(term);
            if (!string.IsNullOrEmpty(v13)) return v13;
            if (Loc.HasEntry(term))
            {
                string v = zh ? Loc.ZhOf(term) : Loc.EnOf(term);
                return string.IsNullOrEmpty(v) ? null : v;
            }
            if (Loc.HasBulk(term))
            {
                string v = Loc.BulkOr(term, null);
                return string.IsNullOrEmpty(v) ? null : v;
            }
            return null;
        }

        /// <summary>这个图名在我们的 sprite asset 里有没有（取不到 asset 时恒 false —— 调用方另有闸）。</summary>
        static bool SpriteExists(string name)
        {
            var sa = SpriteAsset;
            return sa != null && !string.IsNullOrEmpty(name) && sa.GetSpriteIndexFromName(name) >= 0;
        }

        /// <summary>把官方值里不带引号的 `&lt;sprite name=X&gt;` 补成带引号（见 <see cref="RxSpriteUnquoted"/>）。
        /// 图名**认不出时只出声、不删标签**（原版照印；⛔ 不替原版决定「换一张图」）。</summary>
        static string QuoteSprites(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf("<sprite name=", System.StringComparison.Ordinal) < 0) return s;
            return RxSpriteUnquoted.Replace(s, m =>
            {
                string n = m.Groups[1].Value;
                if (!SpriteExists(n))
                    WarnMarkOnce("sprite|" + n, "官方词条里的图 `" + n +
                        "` 在我们的 sprite asset 里**没有** ⇒ 原样印（画不出来）。asset 与别名见 `Editor/IconSetup.cs`");
                return "<sprite name=\"" + n + "\">";
            });
        }

        /// <summary>
        /// **① `((Key,a,b))` → `Modular_Description/&lt;Key&gt;`**（判据 `ModifyLocalization.c:75-186`）。
        /// 原版步骤（逐句）：正则 `\(\((.*?)\)\)` 逐条命中 → 把命中串里的 `((`/`))` 剥掉 →
        /// **按 `,` 切成最多 3 段**（`String.Split(s, 0x2c)`）→ 词条键 = `Modular_Description/` + 第 0 段
        /// → `GetTermTranslation` → 第 1 段非空则 `Replace("{0}", 第1段)`、第 2 段非空则 `Replace("{1}", 第2段)`
        /// → 把**命中整串**换成这个词条。
        /// <para>⚠️ 原版**不 Trim** 任何一段（`((Deal_x_damage, 3))` 那样带空格的会把空格代进去）—— 照抄。</para>
        /// <para>🔴 **缺口（现读）**：`Modular_Description/*` 官方有 **340** 条（`Card_Description` 里用到
        /// **86** 种键 / **508** 处），而它在 `Resources/` 下**一条都没有**
        /// （`Resources/official_names.json` 只有 `Avatars/Titles/Achievements/Cardbacks/Event_Description/
        /// PrebuiltDecks` 六族 = 1327 条；`Loc` 手写表里也没有）⇒ **本支今天会全部「原样留着 + 出声」**。
        /// 这是**已知的数据侧缺口**（`资料/普查产出_第三十会话/卡表块_施工设计_1014.md` §六·3 已把它记成
        /// `A1763` 的数据缺口 / `A1982`），**不是本层的 bug**。</para>
        /// <para>🔴 **2026-10-15（`AG` 波）就地订正（铁律 5）—— 上面那段的【结论】已经过期**
        /// （原句**照留**：它是「写它那一刻为什么这么写」的留痕，⛔ 别删）。**原写 X ⇒ 实际 Y**：
        /// <list type="bullet">
        /// <item>原写「它在 `Resources/` 下**一条都没有**」，并给出「`Resources/official_names.json`
        ///   只有 `Avatars/Titles/Achievements/Cardbacks/Event_Description/PrebuiltDecks`
        ///   **六族 = 1327 条**」⇒ **实际（2026-10-15 现读）= 2758 条 / 【九族】**：
        ///   `Card_Name` **1065** · `Avatars` **419** · `Titles` **368** · **`Modular_Description` 340** ·
        ///   `Achievements` **198** · `Cardbacks` **157** · `Event_Description` **111** ·
        ///   `PrebuiltDecks` **74** · `Card_Trait` **26**。⇒ **`Modular_Description/*` 就在里面**，
        ///   「本地下一条都没有」**不成立**。</item>
        /// <item>原写「⇒ 本支今天会全部『原样留着 + 出声』」⇒ **今天不成立**：卡池 `descZh` 里那
        ///   **73 个键 / 247 处**（现读）**全部命中** `official_names.json`
        ///   ⇒ 本支走的是下面那段**正常路**（取到词条 → 换掉命中整串）；「原样留着 + 出声」
        ///   只剩**这一条键取不到值**那一档（键两张表都没有 / 值里的记号渲不掉 ——
        ///   即 `OfficialTerm` 为 null、走下面那句 `WarnMarkOnce`），
        ///   而现读那 73 个键**一条都不落**在这一档里。</item>
        /// <item>⚠️ **与它配对的那道闸也已经拆了**（2026-10-15 `AC` 波）：`Loc.BulkOr` 原来判
        ///   「值里含 `[[` / `((` ⇒ 当取不到」，现在判「**渲染完还有没有记号**」
        ///   （见 <see cref="OfficialTerm"/> 上面那段订正；实现 = `Core/Loc.cs:2697-2765`）——
        ///   **这一条 + 上面那条合起来**，才是「`((Key))` 今天真的渲得出中文」。</item>
        /// </list>
        /// ⚠️ `A1763` / `A1982` 那两笔账指的是**别的**缺口（记号的渲染层 / 官方表本身的覆盖），
        /// ⛔ **不是**「`Modular_Description` 本地没有」—— 那半句已经作废。
        /// 逐条现数（`BulkOr` 三路各多少、逐条是哪些）→ `资料/普查产出_第三十一会话/AC_Loc记号闸.md`，
        /// ⛔ 别在这里抄第二份。</para>
        /// </summary>
        static string RewriteMarkModular(string s, bool zh)
        {
            if (s.IndexOf("((", System.StringComparison.Ordinal) < 0) return s;
            return RxMarkModular.Replace(s, m =>
            {
                string[] parts = m.Groups[1].Value.Split(',');
                string key = parts[0];
                string term = OfficialTerm("Modular_Description/" + key, zh);
                if (string.IsNullOrEmpty(term))
                {
                    WarnMarkOnce("mod|" + key, "官方记号 `" + m.Value + "` 认得出（键 = `Modular_Description/" + key +
                        "`）但**本地没有这条词条** ⇒ 原样留着（⛔ 不猜）。它属 `A1763` 的数据侧（`A1982`）");
                    return m.Value;
                }
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) term = term.Replace("{0}", parts[1]);
                if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2])) term = term.Replace("{1}", parts[2]);
                return QuoteSprites(term);
            });
        }

        /// <summary>
        /// **② `$$ID$$` → `Card_Name/&lt;ID&gt;`**（判据 `ModifyLocalization.c:188-319`）。
        /// 原版：正则 `\$\$(.*?)\$\$` → 命中串里的 `$$` 各剥一次 → 键 = `Card_Name/` + id → `GetTermTranslation`
        /// → **再查玩家收藏里有没有这张卡**，有就用 `RawCardScript.GetLocalizedCardName(那张卡)` **覆盖**词条；
        /// 取不到玩家数据时印 `PlayerDataManager not found`（那么走面板/战斗里会更实际——见那条注释）。
        /// <para>🔴 **缺口（现读）**：卡池正文在用的 id **116** 种 / **151** 处（既有 `AM5`/`TL37` 这样的
        /// `前缀+数字`，也有 `27a4b748…` 这样的 **32 位 GUID**）；而 `Card_Name/*` 在 `Resources/` 下**也没有**
        /// （同 ① 的六族里没有它，`Loc` 手写表里那 20 来条是我们自造的 `Ember Warlord` 那种，不是官方 id）
        /// ⇒ **本支今天也会全部「原样留着 + 出声」**。数据侧同属 `A1763` / `A1982`。</para>
        /// <para>🔴 **2026-10-18（`A1957`）现读订正 —— 上面那段【已过期】，⛔ 别照它再立账**：
        /// `Resources/official_names.json` 现在带 `Card_Name/*` **1198 条**
        /// （九个族、共 **3156** 条 —— ⚠️ **2026-10-23 现读 = 3,240 条**：`PrebuiltDecks/*` 101 → 179，
        /// 池外 78 副练习窗卡组也 join 进表，见界面线 `W65` / `gen_official_names.py`；其余八族不变）。
        /// 官方全表里含 `$$…$$` 的描述 **189 条**（`Card_Description` 182 + `Card_Abilities` 7）·
        /// **141 个唯一 id / 363 处**；我们卡池 `descZh` 里 `$$…$$` **107 个 id / 139 处** ——
        /// **逐 id 在 `official_names.json` 的 `Card_Name/*` 全都能查到（0 个解不出）** ⇒ 本支今天**全部能展开**，
        /// 「原样留着 + 出声」只剩下「将来出现新 id」时的兜底。判据与计数口径 →
        /// `资料/普查产出_1018/W_卡牌线_数据侧.md` §一 `A1957`。</para>
        /// </summary>
        static string RewriteMarkCardRefs(string s, bool zh)
        {
            if (s.IndexOf("$$", System.StringComparison.Ordinal) < 0) return s;
            return RxMarkCardRef.Replace(s, m =>
            {
                string id = m.Groups[1].Value;
                string term = OfficialTerm("Card_Name/" + id, zh);
                if (string.IsNullOrEmpty(term))
                {
                    WarnMarkOnce("cref|" + id, "官方记号 `" + m.Value + "`（卡名指代）认得出（键 = `Card_Name/" + id +
                        "`）但**本地没有这条词条** ⇒ 原样留着（⛔ 不猜）。它属 `A1763` 的数据侧（`A1982`）");
                    return m.Value;
                }
                // ⚠️ 原版这里还有一刀「玩家收藏里那张卡的显示名覆盖词条」—— 卡面渲染没那个上下文 ⇒ 不做（见方法 doc）。
                return term;
            });
        }

        /// <summary>
        /// **③ `[[枚举名]]` → `GameStaticData.TraitNameToString(值, true)`**（判据 `ModifyLocalization.c:320-467`
        /// + `GameStaticData__TraitNameToString.c` 全文）。
        /// <para>原版：`Enum.GetValues(&lt;DefinedTrait&gt;)` **遍历枚举成员**，对每个成员拼出 `[[成员名]]`、
        /// 命中就整串换成 `TraitNameToString(成员, true)` ⇒ **只认枚举名**，认不出的 `[[x]]` 原样留着。</para>
        /// <para>🔴 **`TraitNameToString` 的整项形状（本轮现核，铁律 5）** —— 它的第 2 个形参是**加粗开关、
        /// 不是「要不要图标」**（两个分支都插图标）：</para>
        /// <list type="bullet">
        ///  <item>`term` 取到 · `param_2 = true`（**`ModifyLocalization` 的调用点就是传 1**，`:455`）⇒
        ///        `&lt;nobr&gt;{图}&lt;b&gt;{词}&lt;/b&gt;&lt;/nobr&gt;`（`:44-67`，数组 5 项 = `&lt;nobr&gt;`/图/`&lt;b&gt;`/词/`&lt;/b&gt;&lt;/nobr&gt;`）</item>
        ///  <item>`term` 取到 · `param_2 = false` ⇒ `&lt;nobr&gt;{图}{词}&lt;/nobr&gt;`（`:75-76`）</item>
        ///  <item>`term` 取不到 ⇒ **只剩枚举名**（`:78-83`，`Enum.ToString()` 那一支）</item>
        ///  <item>三种都被外面**再包一层、且只包这一层** `&lt;link={枚举名}&gt;…&lt;/link&gt;`（`:85-115`）</item>
        /// </list>
        /// <para>⛔ **所以上面 `A969`/`A986②` 那两段注释里「原版整项 = `&lt;link&gt;&lt;nobr&gt;图词&lt;/nobr&gt;&lt;/link&gt;`」少了
        /// `&lt;b&gt;`** —— 那是照 `:75-76`（`param_2 = false` 那支）读的。**我们这一支照调用点那一档写**（带 `&lt;b&gt;`）；
        /// 上面那条（裸关键词那一支）是**我们自己的形状**、有断言钉着，**不动**（更正只落在这里）。</para>
        /// <para>⚠️ **`melee`/`ranged`/`reanimate` 那三词【不写】**：原版确实有一条「这三词走
        /// `MainMenu/General/*`」的特例（`:322-357`，数组 = `melee`/`ranged`/`reanimate`，前缀 `MainMenu/General/`，
        /// 形状 `&lt;nobr&gt;&lt;b&gt;{词条}&lt;/b&gt;&lt;/nobr&gt;`），但**卡池正文里 0 处输入**（现数：这三条只出现在 `Missions/*`
        /// 文案里 = 4/4/2 处；`Card_Description`/`Modular_Description`/`Card_Name`/`Card_Abilities` 全 0）
        /// ⇒ 接了也永远没有输入。判据与口径 → `资料/任务_卡组卡牌战斗.md:294` 的 ⑤。</para>
        /// <para>⚠️ **图名规则**：原版 `GetTraitTextSprite(值)` = `&lt;sprite name=Atlas_trait_icon_{枚举名}&gt;`
        /// （`CardTraitCollection__GetTraitTextSprite.c` 全 6 行，前缀字面量 `0x184238f80` 已解）。
        /// 卡池正文在用的 **55** 种枚举名**逐种核过**：我们 dump 的原版图集里**每一种都有**对应字形
        /// （`数据/游戏数据/trait_textsprites.json`，87 个字符名）⇒ **不需要任何图名映射表**。</para>
        /// <para>🔴 **词条缺口（现读，两张表都算，exact + 归一化两条路）**：55 种里 **47 种 / 1194 处**查得到
        /// （`Loc` 手写表 `Card_Trait/*` 62 条），**8 种 / 56 处查不到**：
        /// `markOfChaos` 18 · `frenzied` 12 · `concussive` 8 · `markOfSlaanesh` 8 · `rage` 6 ·
        /// `markOfKhorne` 2 · `markOfNurgle` 1 · `markOfTzeentch` 1 —— **这 8 个名字在官方表里全都有**
        /// （`Card_Trait/&lt;名&gt;`），只是我们的 `Loc` 用的是**规范键**（`destroyer`/`penitence`/`darkpact`/`concussion`）
        /// ⇒ **是数据侧的改名映射缺口，不是本层的 bug**。⇒ 那 56 处今天落到下面那条**原版自己的兜底**
        /// （只印枚举名），⛔ **不由本层私自加别名表**（那会把缺口藏起来，而且 `markOfSlaanesh` 的官方词
        /// 是 `Dark Pact of Excess`、与 `darkpact` 那条**不是同一个词**）。</para>
        /// <para>🔴 **2026-10-18（`A1982`）现算订正 —— 上面那段读数【已过期】，⛔ 别照它再立账**：
        /// 运行时两个口 = `Loc.cs` 的 `Card_Trait/*` **64 条** ＋ `Resources/official_names.json` 的 `Card_Trait` **27 条**。
        /// 按这两个口逐 token 解（exact → `Norm` 兜底）：
        /// <list type="bullet">
        /// <item>**官方全表**（`Card_Description` / `Card_Abilities` / `Card_Name` / `Modular_Description` / `Missions`）：
        ///   `[[X]]` **3508 处 / 74 种**，缺词条 **8 处 / 7 种** = `reanimate` 2 · `盔甲` · `震荡的` · `不稳定` ·
        ///   `集结` · `tainted` · `maintenance` 各 1（后四个是官方表里**已经是中文的 token**）。</item>
        /// <item>**我们卡池**（`cards_engine.json` 的 `descZh` + `desc` —— 真正会进渲染层的输入）：
        ///   `[[X]]` **1079 处 / 60 种**，缺词条 **0 处 / 0 种** ⇒ **对玩家可见的卡面，这条账已经关上**。</item>
        /// </list>
        /// ⇒ 「46 处/8 种」与「56 处/8 种」两个旧读数都是**更早的分母**（那时 `Card_Trait` 的 27 条还没进
        /// `official_names.json`、`descZh` 也还没换官方表）；**现口径的唯一结论 = 卡池 0 缺**。
        /// 计算脚本口径见 `资料/普查产出_1018/W_卡牌线_数据侧.md` §一 `A1982`。</para>
        /// </summary>
        static string RewriteMarkTraits(string s, bool zh)
        {
            if (s.IndexOf("[[", System.StringComparison.Ordinal) < 0) return s;
            return RxMarkTrait.Replace(s, m =>
            {
                string name = m.Groups[1].Value;
                string term = TraitTermOf(name, zh);
                string spriteName = "Atlas_trait_icon_" + name;
                bool hasSprite = SpriteExists(spriteName);
                if (string.IsNullOrEmpty(term) && !hasSprite)
                {
                    // 原版只在**枚举成员**上动手（`Enum.GetValues` 遍历）⇒ 认不出就**原样留着**（不是「印名字」）
                    WarnMarkOnce("trait|" + name, "官方记号 `" + m.Value + "` 认不出（既没有 `Card_Trait/" + name +
                        "` 词条、也没有 `" + spriteName + "` 这张图）⇒ 原样留着（⛔ 不猜）。判据 = 原版那一支只认枚举成员");
                    return m.Value;
                }
                string inner;
                if (string.IsNullOrEmpty(term))
                {
                    inner = name;                       // 原版兜底：词条为空 ⇒ 只印枚举名（`:78-83`）
                    WarnMarkOnce("traitterm|" + name, "官方记号 `" + m.Value + "` 的枚举名认得，但 `Card_Trait/" + name +
                        "` **本地没有词条** ⇒ 按原版的兜底只印枚举名。缺的这 8 个名字见 `RewriteMarkTraits` 的 doc");
                }
                else
                {
                    inner = "<nobr>" + (hasSprite ? SpriteTagOf(spriteName) : "") + "<b>" + term + "</b></nobr>";
                }
                return "<link=" + name + ">" + inner + "</link>";
            });
        }

        /// <summary>词条：`Card_Trait/&lt;枚举名&gt;`，认不出再试**归一化**那一路（只留小写字母数字）。
        /// <para>⚠️ 归一化那一路是**为我们的表服务的**：`Loc` 的手写键是小写无空格的规范键
        /// （`bloodThirst → bloodthirst` · `cantAttack → cantattack` · `spiritStone → spiritstone`
        /// —— 现读 `Core/Loc.cs` 那张表就是这三个拼法），而官方枚举名是驼峰。⛔ 别把它扩成一张别名表。</para></summary>
        static string TraitTermOf(string enumName, bool zh)
        {
            string t = OfficialTerm("Card_Trait/" + enumName, zh);
            if (!string.IsNullOrEmpty(t)) return t;
            string norm = Norm(enumName);
            if (norm.Length > 0 && norm != enumName) return OfficialTerm("Card_Trait/" + norm, zh);
            return null;
        }

        static string Norm(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        static string SpriteTagOf(string name) { return "<sprite name=\"" + name + "\">"; }

        /// <summary>
        /// **④ `(N)` → `" &lt;sprite name=ManaN&gt;"`**（判据 `ModifyLocalization.c:469-487`）。
        /// 原版：`for (N = 0; N &lt; 9; N++)`，命中原样串就换成 `Concat(" &lt;sprite name=Mana", N, "&gt;")`
        /// —— **那个前导空格是原版字符串本身带的**（`0x1842c79a0` = `" &lt;sprite name=Mana"`），照抄。
        /// <para>🔴 **这一支今天【0 处输入】**（现数：官方 9441 条词条里 `(\d)` 命中 **0**）⇒ 它是**照原版实现、
        /// 等将来有输入**。⚠️ 而我们的 sprite asset 里**没有 `Mana0..8` 这几张图**（原版那份 dump 的 87 个
        /// 字符名里没有 `Mana*`，那是原版**另一个** sprite asset 的东西）⇒ 真来输入时**不能盲换**
        /// （会换出一个渲染不出来的标签 = 静默失败）⇒ 本支带一道「图不在就留着 + 出声」的闸。</para>
        /// </summary>
        static string RewriteMarkMana(string s)
        {
            if (s.IndexOf('(') < 0) return s;
            for (int n = 0; n <= 8; n++)
            {
                string mark = "(" + n + ")";
                if (s.IndexOf(mark, System.StringComparison.Ordinal) < 0) continue;
                string spriteName = "Mana" + n;
                if (!SpriteExists(spriteName))
                {
                    WarnMarkOnce("mana|" + n, "官方记号 `" + mark + "`（= 费用图标）认得出，但 `" + spriteName +
                        "` 这张图**不在**我们的 sprite asset 里 ⇒ 原样留着（⛔ 不换成一个渲染不出来的标签）");
                    continue;
                }
                s = s.Replace(mark, " " + SpriteTagOf(spriteName));
            }
            return s;
        }
    }
}
