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
        /// <summary>引擎侧的**规范关键词键**（`UnitState.Keywords` 的键，例 `"huntmark"`）。
        /// 只拿来配对 —— 脉冲高亮要按「刚触发的是哪个词条」（`BattleEvent.Keyword`）找到**这一位**。</summary>
        public string key;
        public int counter;     // 角标数字；0 = 不带角标（原版 `ToggleCounter(false)`）
        /// <summary>原版 `BoardTraitIcon.Initialize(icon, counter, traitEnabled)` 的**第 3 个（bool）参数**：
        /// true ⇒ 用 `originalMaterial`；false ⇒ 换 `disabledMaterial`（**只灰化，位置和大小都不变**）。
        /// 原版只有 `CardTraitDuty` / `CardTraitOath` 覆写了 `IsActive`，其余恒 true。</summary>
        public bool active;
    }

    public static class Badges
    {
        /// <summary>位子个数。原版左 3 + 右 4 = 7（**第 8 个关键词不再显示**）。</summary>
        public const int MaxSlots = 7;

        // ── 几何（**卡单位**：卡本体 2.0927 × 3.3313，和 `CardView` 同一套坐标）──────
        //
        // 🔴 **2026-09-20 重算 y（原来那套是错的）**。原版出处（逐份 JSON 实读）：
        //   · `TraitIcons` 挂在 `3DBody` 下，localPosition = **(0.296, 1.533, −0.014)**
        //   · 7 个 `TraitIconContainer*` 是 `TraitIcons` 的子节点，localScale 全 **0.750**：
        //       左列   x = **−0.8590**，y = 0.8260 / 0.4010 / −0.0390
        //       右列   x = **+0.2670**，y = 0.8260 / 0.4010 / −0.0390 / −0.4790
        //     ⇒ `TraitIcons.x + 容器x` = **∓0.563 两列**（与我们原来的 `ColX` **逐位吻合**）；
        //       `TraitIcons.y + 容器y` = **2.359 / 1.934 / 1.494 / 1.054**（**3DBody 空间**）。
        //
        //   两处关键认识：
        //   ① **3DBody 空间的 y=0 是「卡的底边」**（网格 `Card 3D WH40k` 实测 y∈[0.012, 2.973]，
        //      原点就在卡底）⇒ 换算到我们的**卡中心原点**要减半个卡高。
        //   ② 🔴 **不能再乘 0.88586**。0.88586 是 `Card 3D`（**那个网格**）自己的 localScale，
        //      而 `TraitIcons` 是 `3DBody` **的**孩子、和 `Card 3D` **平级** ⇒ 徽标活在
        //      **未缩放的 3DBody 空间**里。原注释写的「ourY = (origY/2.96 − 0.5) × 3.3313」
        //      既乘了 1.125（= 1/0.88586）又按 3.3313 拉伸，**两处都错**（这正是场卡
        //      「数值/徽标浮在卡体下面」那条 bug 的一半；另一半是 3D 体自己没下移）。
        //   判据链与实测过程见 `资料/3DBody_原版场上卡体规格.md` §四。
        const float TraitIconsY = 1.533f;                       // 3DBody 空间
        // 容器那一列的四个 y（实测，**非等距**：间距 0.425 / 0.440 / 0.440）—— 照抄，别线性化
        static readonly float[] RowYBody = { 0.826f, 0.401f, -0.039f, -0.479f };
        const float CardHalfH = CardView.CardUnitH * 0.5f;      // 3DBody 的 y=0 对齐到「卡底」

        static readonly float[] ColX = { -0.563f, 0.563f };
        static readonly float[] ColYLeft  = { BodyY(0), BodyY(1), BodyY(2) };
        static readonly float[] ColYRight = { BodyY(0), BodyY(1), BodyY(2), BodyY(3) };

        /// <summary>原版 3DBody 空间的 y → 我们的卡单位 y（**不乘 0.88586**，见上）。</summary>
        static float BodyY(int row) { return TraitIconsY + RowYBody[row] - CardHalfH; }

        /// <summary>第 i 个位（0..6）的卡单位坐标。顺序 = 原版子节点顺序（左 1/2/3、右 1/2/3/4）。</summary>
        public static Vector2 SlotAt(int i)
        {
            if (i < 3) return new Vector2(ColX[0], ColYLeft[i]);
            return new Vector2(ColX[1], ColYRight[i - 3]);
        }

        // 图标：原版 `TraitIcon` 的 SpriteRenderer size 0.8 × localScale 0.7593 × 容器 0.7502 = **0.456**
        public const float IconSize = 0.456f;
        // 🔴 **2026-09-29 更正（原来这条写错了）**：这里原来写
        //   「未激活的那张：localScale 0.49 ⇒ 0.294（原版激活/未激活**大小就差这一档**）」——
        //   错在**把两个不同的渲染器当成了同一个的两种状态**。实据（逐份 JSON 实读）：
        //   · **未激活不是「缩小版」** —— `BoardTraitIcon.Initialize` 在 `traitEnabled == false` 时
        //     只是给**同一批** `traitIcons` 换上 `disabledMaterial`（`BoardTraitIcon__Initialize.c`：
        //     `SetMaterial(渲染器, param_4 == 0 ? +0x60 /*disabledMaterial*/ : +0x80 /*originalMaterial*/)`）
        //     ⇒ **位置、大小都不变，只灰化**。
        //   · `localScale 0.49` 那一个是**另一个独立渲染器** `Trait Not Active`
        //     （`traitNotActiveIcons[]`，`BoardTraitIcon.cs` 桩里 `SpriteRenderer[] traitNotActiveIcons // 0x28`），
        //     它的贴图是**预制体里写死的**、四个实例共用同一个 sprite PathID，而且
        //     **那张贴图在解包全库（24 万个文件）里找不到**（既不在 `bundle_atlasindividual_assets_40ktraiticonatlas`，
        //     也不在那份预制体包里）⇒ **我们不知道它长什么样，按红线就不画**（如实标注，不猜）。
        //   ⚠️ 所以 `IconSizeInactive` **已无用处**（原来也是死代码，全工程无引用）—— 留个 0 值的占位会误导，直接删。
        /// <summary>原版那层「未激活」用的材质名 —— `BoardTraitIcon.disabledMaterial` 指向的资源
        /// （`bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-1344813161239158161.json`，
        /// `m_Name = "Sprite Greyscale"`，`_GreyScale = 1.0`）。我们的等价物是自建 shader
        /// `CardPresentation/TraitDisabled` 的 `_GreyScale` 属性（同一个语义、同一个属性名）。</summary>
        public const string DisabledMaterialName = "Sprite Greyscale";

        // ── 脉冲高亮（原版 `BoardTraitIcon.HighlightIcon`）──────────────────────────
        //
        // 原文（`BoardTraitIcon__HighlightIcon.c`）：先把上一次的补间 `Kill` 掉、把 `contentTransform`
        // 的 `localScale` **归位**到 `originalIconSize`（`Awake` 里cache 的那份），再
        //   `DOScale(contentTransform, originalIconSize × highlightAnimScale, highlightAnimTime)`
        //   `.SetLoops(2, Yoyo).SetEase(9 /* OutCubic */)`
        // 两个字段值来自预制体（`MonoBehaviour_-2012836666520331328.json`，7 份一样）：
        //   `highlightAnimScale = 1.6` · `highlightAnimTime = 0.5`
        // ⚠️ 归位那一步别省 —— 连点两下时没有它，第二次是从「已经放大 1.6 倍」的基础上再乘，越点越大。
        /// <summary>原版 `BoardTraitIcon.highlightAnimScale`（预制体字段）</summary>
        public const float HighlightAnimScale = 1.6f;
        /// <summary>原版 `BoardTraitIcon.highlightAnimTime`（预制体字段，秒）</summary>
        public const float HighlightAnimTime = 0.5f;
        /// <summary>原版 `SetLoops(2, Yoyo)` 的那个 2</summary>
        public const int HighlightAnimLoops = 2;
        // 底板：size (0.85, 0.874) × localScale (0.866, 1.069) × 容器 0.7502
        public const float PlateW = 0.552f, PlateH = 0.701f;

        // 🔴 **2026-09-20：图标与底板各自还要往「卡外」偏一截**（左右镜像）。原来只记了底板的 0.30，
        //    而且把图标的偏移**漏了** ⇒ 7 个徽标整体偏**内** 0.215 卡单位（≈卡宽的 10%）。
        //    实据（`TraitIconContainer*` 的子节点，`Transform/` 逐份实读）：
        //      · `Container`（**图标本体**）localPosition x = **∓0.287**，y = 0
        //      · `IconBackground`（**底板**）localPosition = (**∓0.300**, **−0.020**)
        //    ⚠️ 这两个数活在**容器自己的空间**里，容器 scale = **0.750**，所以乘 0.750 才是卡单位。
        //    ⚠️ 原注释写「底板偏 0.30，我们摆 0（正后方）—— 因为我们是平面 2D 卡」：**那条前提已经作废**
        //       （徽标只在**场上**显示，而场上是 3D 卡体）⇒ 按原版摆回去。
        public const float IconOutward  = 0.287f * 0.750f;   // = 0.21525
        public const float PlateOutward = 0.300f * 0.750f;   // = 0.225
        public const float PlateDy      = -0.020f * 0.750f;  // = −0.015（底板比图标略低）

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

        /// <summary>
        /// 🆕 2026-09-21：**图名 → 规范键**（卡面那条 `&lt;link>` 用它）。只列「图名 ≠ 规范键」的那几个，
        /// 其余**图名就是规范键**（`rally` / `armour` / `flying` … 小写化即可）。
        /// ⚠️ **必须和上面那张 `Alias` 成对维护** —— 正向表加了条目、这里不跟上，
        ///    卡面那条 `&lt;link>` 就会指到一个查不到的键（tooltip 只剩名字、没有解释）。
        /// ⚠️ **不能从 `Alias` 反推**，两个反例：`{"armor","armour"}` 反推会得到 `armour`→`armor`
        ///    （**错**，规范键就是 `armour`）；`questpoint` / `questpoints` 两条都指向 `questPoints`，
        ///    反推还有歧义。**手写这一张更短也更安全。**
        /// </summary>
        static readonly Dictionary<string, string> SpriteKey = new Dictionary<string, string>
        {
            { "frenzied", "destroyer" },     // 反向：图 `frenzied` = 关键词 `Destroyer`
            { "rage", "penitence" },         // 反向：图 `rage` = 关键词 `Penitence`
            { "markOfChaos", "darkpact" },   // 反向：图 `markOfChaos` = 关键词 `Dark Pacts`
            { "concussive", "concussion" },  // 反向：图 `concussive` = 关键词 `Concussion`（规则书拼法）
        };

        /// <summary>🆕 2026-09-21：图名 → 规范键（见 `SpriteKey`）。认不出就返回**小写化的图名**
        /// ——至少还能当个名字显示（`TipText.Trait` 那边会如实说「规则书里没有这个词的条目」）。</summary>
        public static string KeyOf(string sprite)
        {
            if (string.IsNullOrEmpty(sprite)) return null;
            // **档位数字烘在图名里**，两种写法都有：`SpiritStone_1..5`（**带下划线**）、
            // `questPoints1..3`（**不带**）。⇒ 判据要**两种都剥**，只按 `_` 剥会得到
            // `questpoints2` 这种查不到的键（2026-09-21 自检抓到的）。
            // ⚠️ 安全前提：73 张关键词图的名字里**没有以数字结尾的**（带数字的只有上面那 8 张）。
            string s = System.Text.RegularExpressions.Regex.Replace(sprite, @"_?\d+$", "");
            if (s.Length == 0) return null;
            string k;
            if (SpriteKey.TryGetValue(s, out k)) return k;
            return s.ToLowerInvariant();
        }

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

        /// <summary>和查表用的归一化**同一份**（只留小写字母数字）—— 给「按关键词找徽标位」那类配对用
        /// （`BattleEvent.Keyword` 写 `huntmark`、卡面写 `Hunt Mark`，得能配上）。
        /// ⚠️ 别在调用方另写一份归一化 —— 两份迟早不一致。</summary>
        public static string Norm(string s) { return Key(s); }

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
        /// 认不出图标的**跳过并计数**，由自检盯着，不静默吞掉。
        /// 🔴 **2026-10-21 更正（`A1361` · 铁律 5）**：这一句原来举的例子是「（`Talent` / `Secret` /
        ///   `Lord Commander` 这类**没有图的**）」—— **`Talent` 那半个例子不成立**：
        ///   `Resources/Art/traits/talent.png` 在（9070 B）、`Resources/Fonts/Warpforge Trait TextSprites.asset`
        ///   里有字形 `m_Name: talent`，`资料/关键词图标_现状与总表.md:146`（第 54 行 `Talent`）标的就是 ✓
        ///   ⇒ 它**不是**「没有图的」。⚠️ 同句剩下的 `Secret` / `Lord Commander` **另论、本批没动**：
        ///   `Secret` 在卡池里是 **`subtype`、不是关键词**（5 张）；`Lord Commander` 是卡名
        ///   （`AM_Lord_Commander`）＋ `AM3` 的一条 `desc` 文本 ⇒ 「它们算不算关键词」是**另一笔账**。
        ///   （本句只改文案 —— 判据、逻辑、去重规则一个字节没动。）
        /// </summary>
        public static List<Badge> For(IEnumerable<KeyValuePair<string, int>> keywords, string orderHint = null,
                                      IReadOnlyCollection<string> numericKeys = null,
                                      System.Func<string, bool> isActive = null)
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
                // 角标（2026-09-29 改口径，见 `CarriesValue`）；
                // 激活态（原版 `BoardTraitIcon.Initialize` 的第 4 个参数）由调用方给 ——
                // 原版只有 `Duty` / `Oath` 两个子类覆写了 `IsActive`，其余恒 true。
                bool act = isActive == null || isActive(kv.Key);
                list.Add(new Badge
                {
                    sprite = spr,
                    key = kv.Key,
                    counter = CarriesValue(kv.Key, kv.Value, numericKeys) ? Mathf.Max(1, kv.Value) : 0,
                    active = act
                });
            }
            return list;
        }

        /// <summary>
        /// 这个关键词**画不画角标数字**。
        ///
        /// 🔴 **原版判据（2026-09-29 读反编译坐实）**：`BoardTraitIcon.Initialize(icon, counter, enabled)`
        ///    —— `counter &lt; 1` ⇒ 换成 `Without counter` 那个子物体、否则 `With counter`
        ///    （`d:/2/tools/decomp_full/BoardTraitIcon__Initialize.c` 尾部两分支）。
        ///    那个 `counter` = `EntityScript.GetCurrentTraitValueWithModifiers(entity, traitId)`
        ///    = `traitData.值 + Σ 同 id 的 modifier`，**钳 ≥ 0**（`EntityScript__GetCurrentTraitValueWithModifiers.c`）。
        ///    而词条的基值来自 `CardTrait.GetNewTrait(..., int defaultValue = **0**)`
        ///    —— 卡面没写数字的词条基值就是 **0** ⇒ **不画角标**。
        ///
        /// ⚠️ **不能直接照抄 `值 ≥ 1`**：我们的引擎对「没有数字」的关键词**兜底给 1**
        ///    （`KeywordTable.FirstNumber`），照抄会把 `Flying` / `Rally` 这些**全都画上一个 1**。
        ///    ⇒ 判据按下面三条的**并集**，第 ① 条就是原版那条判据在我们数据里的等价物：
        ///
        /// ① **卡面原文里写了数字**（`CardDef.NumericKeywords`，由 `KeywordTable.HasNumber` 抽出来）
        ///    —— 实测卡池 1126 张里带数字的关键词只有
        ///    `Armour / Blast / Tide / Regeneration / Shuriken / Companion / Ecstasy / Oath / Sentry`
        ///    这几个（`cards_engine.json` 全量扫描），**正是我们原来那张手写表的内容**。
        /// ② **引擎当前值 ≥ 2** —— 已经被 modifier 抬上去的（原版这时也会画）。
        /// ③ **规则书「带数值?」那一列**（下面那张表）—— 留给**运行时授予**、卡面上本来不印数字的
        ///    （`Hunt Mark` / `Markerlight` / `Vulnerable` 这一族，出现在效果文字而不是关键词行）。
        ///
        /// 🔴 **两个调用点的实参不同 —— 就写在这里**（2026-10-11 · `A1332` 收口）：
        ///   · **徽标那一档**（`Badges.For:288`）传 `(kv.Key, kv.Value, numericKeys)` ⇒ **①②③ 三档全接**
        ///     （喂进来的是单位**当前**的关键词表，光环/授予抬起来的词走 ②）；
        ///   · **卡面关键词行那一档**（`CardText.KeywordSegment`，现读 `:241`）传 `(key, kv.Value, null)` ⇒
        ///     **只接 ②③**（① 那一档要 `CardDef.NumericKeywords`，那条路的签名里没有它）。
        ///   ✅ 两处**共用这一个函数体**，今天**逐张等价**（全池 1126 张：① = ② = 9 键、③ = 12 键，
        ///     `①∖③ = ∅`、`②∖③ = ∅` ⇒ 0 处不同）—— **但别以为改这里两处会一起变**：
        ///     卡面那条路少 ① 这一档，只有「卡池出现带数字、又不在 ③ 表里的词」时才现形（`A1332`）。
        ///
        /// ⚠️ **原来这条注释里举的反例（`SpiritStone_1..5` / `questPoints1..3`「原版画角标、我们不画」）
        ///    2026-09-29 查实是【误记】**：那两个是 **HUD 的阵营资源计数**，不是场上徽标 ——
        ///    全卡池 1126 张的关键词行里**一个都没有**（`spiritstone` 是 `KeywordTable` 的常量、
        ///    `questPoints` 是 `PlayerState` 的资源，都进不了 `UnitState.Keywords`）。
        /// </summary>
        public static bool CarriesValue(string keyword, int value = 1, IReadOnlyCollection<string> numericKeys = null)
        {
            string key = Key(Strip(keyword));
            // ① 卡面原文里写了数字（数据侧 = 原版那条判据的等价物）
            if (numericKeys != null && Contains(numericKeys, key)) return true;
            // ② 引擎当前值已经比兜底的 1 大
            if (value >= 2) return true;
            // ③ 规则书那张表（旁证 / 运行时授予的那一族）
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

        /// <summary>`IReadOnlyCollection` 上没有 `Contains`（那是 `ICollection` 的）——
        /// 集合只有两三个元素，线性扫就好，不值得为此引 `System.Linq`。</summary>
        static bool Contains(IReadOnlyCollection<string> set, string key)
        {
            foreach (var s in set) if (s == key) return true;
            return false;
        }

        /// <summary>去掉数值后缀与冒号正文（`"Armour 2"` → `"Armour"`）—— 只在排序时用，判据仍是 <see cref="SpriteOf"/>。</summary>
        static string Strip(string keyword)        {
            string k = (keyword ?? "").Trim();
            int colon = k.IndexOf(':');
            if (colon > 0) k = k.Substring(0, colon);
            return System.Text.RegularExpressions.Regex.Replace(k, @"[\s\.]*\d+[\s\.]*$", "").Trim();
        }
    }
}
