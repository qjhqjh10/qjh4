// Badges.cs —— 棋盘**单位卡身上的 buff / debuff 徽标**（2026-09-15）
//
// **原版出处**（三条独立证据，缺一不可）：
//  ① 组件：`BattleCardUI.boardTraitIcons[7]` —— 7 个 `BoardTraitIcon`
//     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleCardUI.cs:226`）。
//     闭环：`BoardTraitIcon` 的 MonoScript PathID = `-1269599923680593461`，带
//     `traitIcons[2]/traitNotActiveIcons[2]/counterText` 的实例全库**只有 7 个**，
//     与 7 个容器**逐个对上**（2026-09-15 全库扫）。
//  ② 驱动链：`CardScript.UpdateTraitIcons()` → `BattleCardUI.UpdateTraitIcons()`
//     （反编译 `CardScript__UpdateTraitIcons.c:8`；**加/移除效果、眩晕、伏击、回合结束**等
//     15+ 个卡事件都会触发重算）。守卫：非单位直接 return；**伏击（ambush 且未翻面）不显示**；
//     每次重算先把 7 个位全部 `Toggle(false)`。图标取 `CardTrait._traitIcon`（运行时按
//     卡当前拥有的 trait 查表），角标数值取 `GetCurrentTraitValueWithModifiers()`，
//     激活态取 `CardTrait.IsActive(CardScript)`。
//  ③ 几何：`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/TraitIconContainer*.json`
//     （7 份）—— 父链 `TraitIconContainer ← TraitIcons ← 3DBody ← Board Elements ← CardPrefab`。
//
// 🔴 **本地查不到、因而是「我们挑的」**（按项目红线必须写明）：
//  · **哪些关键词进这 7 个位** —— 原版筛选器 `CardTraitCollection.GetTraitInPlayListForIcons`
//    的**方法体没被反编译**，`CardTrait` 资产本体（带 `_traitIcon` 的那份）也没解出来
//    ⇒ 「blast / vulnerable 到底进不进位」**本地无确证**。我们的判据见 `For(...)`。
//  · **底板怎么画** —— 原版那个 sprite（PathID 2729514481590049735）在解包里**找不到本体**，
//    我们按工程里现成的 `Art/ui/Base3d_Trait_Background.png` 画（按自身比例 fit 进格子）。
//  · **手牌上显不显示** —— 原版手牌与棋盘**是同一个 prefab**（7 个位物理上都在手牌上），
//    但有没有被隐藏**没找到判据**（`ToggleTraitIcons`/`FadeAllTraitsIcons` 在已反编译集合里无调用者）
//    ⇒ 我们**只在棋盘（场上）显示**，手牌不显示。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一个徽标位要画的东西。`sprite` 为 null = 这个位空着。</summary>
    public struct Badge
    {
        public string sprite;   // `Art/traits/` 里的图名（图集文件名就是关键词名）
        public int counter;     // 角标数字；0 = 不带角标（原版 `ToggleCounter(false)`）
        public bool active;     // 原版「激活 / 未激活」两套 renderer + `disabledMaterial`
    }

    public static class Badges
    {
        /// <summary>位子个数。原版左 3 + 右 4 = 7（**第 8 个关键词不再显示**）。</summary>
        public const int MaxSlots = 7;

        // ── 几何（**卡单位**：卡本体 2.0927 × 3.3313，和 `CardView` 同一套坐标）──────
        //
        // 原版那 7 个容器挂在 `3DBody` 下（`TraitIcons` 自身偏移 (0.296, 1.533)），
        // 而 `3DBody` 的卡面网格 `Card 3D WH40k.obj` 实测 x∈[−1.048,1.042]、y∈[0.012,2.973]
        // ——**x 以卡中线为 0、y 以卡底为 0**，和我们的「中心原点」差一个 y 偏移。
        // 换算：ourY = (origY / 2.96 − 0.5) × 3.3313；x 直接照搬（两边的半宽都是 ~1.046）。
        //   ⇒ 两列 x = ±0.563，y = +0.99 / +0.51 / +0.01 / −0.49（左列少最后一个）。
        static readonly float[] ColX = { -0.563f, 0.563f };
        static readonly float[] ColYLeft = { 0.99f, 0.51f, 0.01f };            // 左 3
        static readonly float[] ColYRight = { 0.99f, 0.51f, 0.01f, -0.49f };  // 右 4

        /// <summary>第 i 个位（0..6）的卡单位坐标。顺序 = 原版子节点顺序（左 1/2/3、右 1/2/3/4）。</summary>
        public static Vector2 SlotAt(int i)
        {
            if (i < 3) return new Vector2(ColX[0], ColYLeft[i]);
            return new Vector2(ColX[1], ColYRight[i - 3]);
        }

        // 图标：原版 `TraitIcon` 的 SpriteRenderer size 0.8 × localScale 0.7593 × 容器 0.7502 = **0.456**
        public const float IconSize = 0.456f;
        // 未激活的那张：localScale 0.49 ⇒ 0.294（原版激活/未激活**大小就差这一档**）
        public const float IconSizeInactive = 0.294f;
        // 底板：size (0.85, 0.874) × localScale (0.866, 1.069) × 容器 0.7502
        public const float PlateW = 0.552f, PlateH = 0.701f;
        /// <summary>
        /// 底板相对图标的横向偏移。原版是 **0.30**（往卡外，左右镜像）—— 那是 `3DBody` 那套斜着的
        /// 3D 卡用的，底板是一块斜插的铭牌。**我们摆 0（正后方）**：平面 2D 卡照搬 0.30 会
        /// 让底板和图标分家（2026-09-15 渲染出来一眼可见）。**这是一处明写的偏离。**
        /// </summary>
        public const float PlateOffset = 0f;

        // ── 关键词 → 图名 ─────────────────────────────────────────────────
        //
        // 图集文件名**就是关键词名**（`Art/traits/`，78 张，见 `资料/关键词图标/`），
        // 但**大小写不统一**（`blast` / `bloodThirst` / `SpiritStone_1`），所以我们按
        // 「只留小写字母数字」归一后查表 —— 这样 `Blood Thirst` 能查到 `bloodThirst`。
        static Dictionary<string, string> _byKey;

        /// <summary>
        /// 卡面上/关键词表里出现、但**图集里没有同名图**的那几个 —— 逐条有理由，**不是兜底猜测**。
        /// 出处：`资料/卡面图标_现状与缺口.md` §二之二（逐张卡图核过）。
        /// </summary>
        static readonly Dictionary<string, string> Alias = new Dictionary<string, string>
        {
            { "destroyer", "frenzied" },    // 卡面给「毁灭者」用的图就叫 `frenzied`（`Skorpekh Destroyer` 核过）
            { "penitence", "rage" },        // `Death Cult Assassin` 卡面写 Penitence、画的是 `rage`
            { "questpoint", "questPoints" },
            { "questpoints", "questPoints" },
            { "armor", "armour" },
            { "darkpact", "markOfChaos" },  // 黑暗契约 = 混沌印记那一枚（规则书 :179「黑暗契约」）
            // ⚠️ 引擎里的关键词是 `Concussion`（规则书中文「震荡」），而图集那张图叫 `concussive.png`
            //    —— **同一个词的两种拼法**（`资料/卡面图标_现状与缺口.md` §二之二·补 第 3 条逐张核过）。
            { "concussion", "concussive" },
        };

        static void EnsureTable()
        {
            if (_byKey != null) return;
            _byKey = new Dictionary<string, string>();
            // 路径和 `CardArt.Trait` **同一份**（`Root + "traits"`）—— 两处写死迟早不一致。
            foreach (var t in Resources.LoadAll<Texture2D>(CardArt.Root + "traits"))
            {
                if (t == null) continue;
                _byKey[Key(t.name)] = t.name;      // 值用**真名**（大小写照文件）
            }
        }

        static string Key(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        /// <summary>
        /// 关键词 → `Art/traits/` 里的图名；**认不出就返回 null**（调用方必须判 —— 红线：宁可少画，不给错图）。
        /// 「Armour 2」「Blast 2.」这类**带数值**的先剥掉数值；「Rally: …」这种**带正文**的只取冒号前。
        /// </summary>
        public static string SpriteOf(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            string k = keyword.Trim();
            int colon = k.IndexOf(':');
            if (colon > 0) k = k.Substring(0, colon);
            k = System.Text.RegularExpressions.Regex.Replace(k, @"[\s\.]*\d+[\s\.]*$", "");
            string key = Key(k);
            if (key.Length == 0) return null;

            EnsureTable();
            string name;
            if (_byKey.TryGetValue(key, out name)) return name;
            if (Alias.TryGetValue(key, out name) && _byKey.ContainsKey(Key(name))) return name;
            return null;
        }

        /// <summary>
        /// 从**单位当前的关键词表**挑出要显示的那几个（最多 7 个，顺序稳定 ⇒ 对局可复现）。
        ///
        /// 🔴 **判据是我们定的**（原版的筛选器没被反编译，见文件头）：**凡是查得到图标的都显示**。
        /// 也就是说「引擎当前认为这个单位身上有什么，卡上就画什么」—— 加/减益、光环给的、
        /// 限时增益到期收回，全都会跟着变，因为喂进来的是 <see cref="UnitState"/> 的**当前**表。
        /// 认不出图标的（`Talent` / `Secret` / `Lord Commander` 这类没有图的）**跳过并计数**，
        /// 由自检盯着，不静默吞掉。
        /// </summary>
        public static List<Badge> For(IEnumerable<KeyValuePair<string, int>> keywords, string orderHint = null)
        {
            var list = new List<Badge>();
            if (keywords == null) return list;

            // 顺序：**卡面效果文字里先出现的排前面**（和玩家在卡上读到的词序一致），
            // 文字里没有的（后面加上来的增益）按名字排 —— 全程确定 ⇒ 对局可复现。
            var ordered = new List<KeyValuePair<string, int>>();
            foreach (var kv in keywords) ordered.Add(kv);
            string hint = (orderHint ?? "").ToLowerInvariant();
            ordered.Sort((a, b) =>
            {
                int ia = hint.IndexOf(Strip(a.Key).ToLowerInvariant(), System.StringComparison.Ordinal);
                int ib = hint.IndexOf(Strip(b.Key).ToLowerInvariant(), System.StringComparison.Ordinal);
                if (ia < 0) ia = int.MaxValue;
                if (ib < 0) ib = int.MaxValue;
                if (ia != ib) return ia.CompareTo(ib);
                return string.CompareOrdinal(a.Key, b.Key);
            });

            foreach (var kv in ordered)
            {
                if (list.Count >= MaxSlots) break;
                string spr = SpriteOf(kv.Key);
                if (spr == null) continue;
                // 角标：**只有「带数值」的关键词才画**（见 `CarriesValue` 的出处）。
                // 引擎里无数值的关键词值恒为 1（`KeywordTable.Parse`），不能拿「值 > 1」当判据 ——
                // 那样 `Armour 1` / `Hunt Mark 1` 就没角标了，而原版这两个是带数字的。
                list.Add(new Badge
                {
                    sprite = spr,
                    counter = CarriesValue(kv.Key) ? Mathf.Max(1, kv.Value) : 0,
                    active = true
                });
            }
            return list;
        }

        /// <summary>
        /// 这个关键词**卡面上带不带数值**（带数值的才画角标）。
        ///
        /// **出处是规则书**（`资料/关键词图标/_规则书关键词表.md` 的「带数值?」列 —— 11 条：
        /// Armour · Blast · Companion · Ecstasy · Markerlight · Oath · Regeneration · Sentry ·
        /// Shuriken · Tide · Vulnerable），外加 **`Hunt Mark`**（规则书中文版 `:189` 明写
        /// 「名称无 X 但**实际带数值**（标记数）」，所以它也算）。
        ///
        /// ⚠️ 这张表**只影响角标画不画**，不影响图标认不认（那只看 `SpriteOf`）。
        /// ⚠️ 原版是按 `CardTrait` 资产里的字段决定「有角标版 / 无角标版」切哪一支的，
        /// 那份资产没解出来 ⇒ 这里是**照规则书推的**，不是抄原版字段。
        /// </summary>
        public static bool CarriesValue(string keyword)
        {
            string key = Key(Strip(keyword));
            switch (key)
            {
                case "armour": case "blast": case "companion": case "ecstasy":
                case "markerlight": case "oath": case "regeneration": case "sentry":
                case "shuriken": case "tide": case "vulnerable": case "huntmark":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>去掉数值后缀与冒号正文（`"Armour 2"` → `"Armour"`）—— 只在排序时用，判据仍是 <see cref="SpriteOf"/>。</summary>
        static string Strip(string keyword)
        {
            string k = (keyword ?? "").Trim();
            int colon = k.IndexOf(':');
            if (colon > 0) k = k.Substring(0, colon);
            return System.Text.RegularExpressions.Regex.Replace(k, @"[\s\.]*\d+[\s\.]*$", "").Trim();
        }
    }
}
