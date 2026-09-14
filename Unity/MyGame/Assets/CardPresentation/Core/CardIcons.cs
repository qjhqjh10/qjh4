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
// 那张表的来龙去脉、以及**还缺哪 4 处**，见 `资料/卡面图标_对照与缺口.md`。
//
// **查不到就原样返回**（记号照字面留在文本里）—— 宁可难看，也不猜一个错的图标（工程红线）。
// 缺口的四处在计划表里 `sprite` 是空串，这里会**打一次警告**再原样返回。
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
        /// 把这段效果文字里的记号换成图标标签。
        /// `field` 传 `"desc"` 或 `"descZh"`（两张表分开定——中文和英文的记号不一定对得上）。
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

            string s = text;
            foreach (var it in items)
            {
                if (string.IsNullOrEmpty(it.token)) continue;
                if (string.IsNullOrEmpty(it.sprite))
                {
                    // 缺口（原版就没认定 / 图集里没有那张图）：**原样留着**，并说一次
                    if (_warned.Add(cardId + "|" + field + "|" + it.token))
                        Debug.LogWarning("[CardIcons] " + cardId + " " + field + " 的 " + it.token +
                                         " 没有对应图标（见 `资料/卡面图标_对照与缺口.md` 第四节）—— 按文字画。");
                    continue;
                }
                string tag = "<sprite name=\"" + it.sprite + "\">";

                // ⚠️ 灵魂石那类**数字烘在图里**（`SpiritStone_1..5`）：卡面文本里那个 `N ` 要一起吃掉，
                //    否则会「图标里有 1、旁边又印一个 1」。计划表给的就是带档位的那张图，
                //    所以这里只做「连数字一起换掉」这一件事。
                if (it.token.IndexOf("Spirit Stone", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    s = System.Text.RegularExpressions.Regex.Replace(
                        s, @"(\d)\s*" + System.Text.RegularExpressions.Regex.Escape(it.token), tag);
                }
                s = s.Replace(it.token, tag);
            }
            return s;
        }
    }
}
