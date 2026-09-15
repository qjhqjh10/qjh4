// CardIcons.cs — 「卡面效果文字里的图标」的运行时入口（2026-09-15）
//
// **它干什么**：把效果文字里的**记号**换成 TMP 的行内 sprite 标签。
//
//     "Give +3 [Attack], +3 [Armor] or +3 Health to a friendly troop"
//       ↓ Rewrite("DA44", "desc", …)
//     "Give +3 <sprite name=\"Melee\">, +3 <sprite name=\"Ranged\"> or +3 Health to a friendly troop"
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

        /// <summary>这段文字里有没有 `<sprite name="…">` 标签</summary>
        public static bool HasIcons(string text)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.IndexOf("<sprite", System.StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// 把 `<sprite name="…">` 标签**全部剥掉**（返回纯文字）。
        /// 🔴 **点阵字库那条兜底路要用它** —— 那条路不认识 sprite 标签，原样喂进去会把
        /// `<sprite name="Melee">` 一个字一个字画出来（`TextCanvas` 的注释：非 ASCII 字形查不到
        /// **只跳格不留痕**，所以画出来是一串空格加乱码）。宁可少个图标，别画一串垃圾。
        /// </summary>
        public static string StripTags(string text)
        {
            if (!HasIcons(text)) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, "<sprite[^>]*>", "");
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
        /// 🔴 **四类记号的处理方式不一样**（计划表 1369 处，实测口径：
        /// **方括号 195 / 裸关键词 1095 / 符号 79**；另**「数字烘在图里」与前三类重叠、共 100 处**）：
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
                string tag = "<sprite name=\"" + it.sprite + "\">";
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
                    if (s.IndexOf(tag + it.token, System.StringComparison.Ordinal) >= 0) continue;
                    s = s.Replace(it.token, tag + it.token);
                    continue;
                }
                s = s.Replace(it.token, tag);
            }
            return s;
        }
    }
}
