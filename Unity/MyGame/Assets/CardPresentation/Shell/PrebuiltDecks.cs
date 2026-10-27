// PrebuiltDecks.cs — 原版「预组卡组」的**运行时数据源**（预组页签那一池）
//
// ============================ 数据从哪来 ============================
// `Resources/prebuilt_decks.json` —— 由 `工具/gen_prebuilt_decks.py` 生成，**别手改**。
// 源：`数据/游戏数据/decklists.json` 里 **`isPractice == 1` 的 103 副**
// （`isPracticeDeck` 是原版进 `practiceDecks` 的唯一条件，见 `资料/预组卡组_原版规格.md` §四）。
//
// 🔴 **卡池或 id 表变了 ⇒ 必须重跑生成器**（与 `cards_engine.json` 一样是构建步骤）。
// 🔴 **教程那 12 副不在其中、也没被改过**：它们 `isPractice == 0`，生成器按这一条过滤 + 断言兜底。
//    （教程牌的牌序就是教学脚本赖以成立的东西，见 `资料/教程线_原版规格与资源存量.md` §一。）
//
// 🆕 **2026-10-22：本类多了两份数据**（练习窗左列改成原版的「一对两副」之后）：
//   · `Resources/demo_deck_pairs.json` —— 84 份 `DemoDeckInfoSO` 的成对表（`Pairs`；只取 `kind=="practice"` 那 78 对）；
//   · `Resources/prebuilt_decks_extra.json` —— 那 78 对里**落不进上面 103 副池**的 78 副（`AllExtra`）。
//   `ById(deckId)` **合并两个来源**查（键 = `deckId`）；`Tab`/`All` 仍然**只看池**（预组页不受影响）。
//
// ============================ 两种模式混在一页（照原版）============================
// 那一池 103 副按 `gameMode` 分成两半：**Classic（30 张）49 副 + Skirmish（12 张）54 副**。
// 原版**不按模式筛**（全链无 `PlayModes` 比较，见 `资料/预组卡组_原版规格.md` §七 第 3 条）⇒ 原版是混着列的。
//
// 🔴 **2026-09-26：我们原来多了一条「只列经典」的偏离，现在删掉了。**
//    那条偏离的理由是「我们的对战只支持经典，那 54 副（12 张）点了开不了局」；
//    现在引擎侧补齐了遭遇模式（`RuleEngine/GameplayVariables.cs` 的 `Skirmish` 实例 +
//    `GameMode` 枚举 + `BattleDriver` 读 `gameMode` 建局）⇒ **103 副里 `complete` 的全列**，
//    与 `Tab` 那条注释逐字对应。规格全文 → `资料/加时与冲突模式_原版规格.md` §二。
//
// ⚠️ **两条曾经的「偏离」现在都已收口**（这里原来记着它们没做，别再照着老话去查）：
//   · **难度角标** ✅ 2026-09-26 已画（三张图 `Menu_Icon_Gallons_1/2/3`，按 PathID 反查到名字）；
//   · **模式角标**（`40k_gamemode_icon_classic` / `_skirmish`）✅ 已经画上 ——
//     **现在它真的在区分东西了**（同一页里经典 30 张 / 遭遇 12 张）。
//     逐值与位置 → `资料/预组卡组_原版规格.md` §六 第 3/4 项。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>预组卡组（原版 `PrebuiltDeck`）在我们这边的只读数据源。</summary>
    public static class PrebuiltDecks
    {
        /// <summary>`Assets/RuleEngine/Resources/prebuilt_decks.json`。</summary>
        public const string ResourcePath = "prebuilt_decks";

        /// <summary>🆕 **2026-10-22**：**池外的 78 副**（练习窗那 78 对里落不进 103 副池的那些）。
        /// 产物 = `工具/gen_practice_decks_extra.py` → `Resources/prebuilt_decks_extra.json`
        /// （**与池那份并列、形状逐字段相同** —— 运行时按 `deckId` 合并查，见 <see cref="ById"/>）。
        /// 🔴 **两份产物不许合并成一份**：池那份的 `git diff` 必须能要求「逐字节 0」（预组页那一列表就长在它上面）。</summary>
        public const string ResourcePathExtra = "prebuilt_decks_extra";

        /// <summary>🆕 **2026-10-22**：练习窗「一对两副」的成对表（原版 84 份 `DemoDeckInfoSO`）。
        /// 产物 = `工具/gen_demo_deck_pairs.py` → `Resources/demo_deck_pairs.json`（D3）。
        /// 本类只取 `kind == "practice"` 那 78 对（另外 6 份是教程，走 `TutorialModePopup` 那条路）。</summary>
        public const string PairResourcePath = "demo_deck_pairs";

        /// <summary>一副预组卡组。字段名与产物 JSON 一一对应（`JsonUtility` 按名匹配）。</summary>
        [System.Serializable]
        public class Deck
        {
            public string deckId;      // 原版 deckId（**练习池内 103/103 唯一**，可当键）
            public string name;        // 英文原名（原版资产 deckName，如 "Press the Attack"）
            public string nameZh;      // 我们译的中文（数据/卡牌翻译/zh_decks.json）
            public string faction;     // 阵营（12 个；用于阵营图标 + 卡背兜底）
            public int armyOrder;      // `CardArmy` 枚举值（10 一档）—— 原版第二排序键，**不是字符串序**
            public int gameMode;       // 0 = Classic（30 张）· 13 = Skirmish（12 张）
            public int difficulty;     // 0 / 5 / 10 / 15（原版第一排序键，升序）
            public string heroId;      // 督军（**我们的卡 id**）
            public string heroNote;    // 督军是怎么解出来的（id命中 / 名字命中 / id指向非督军…）—— 诊断用
            public string cardback;    // 卡背**名**（= `Art/cardbacks/<名>.png` 的 `<名>`）
            /// <summary>这个卡背名是怎么来的：`column` = `decklists.json` 的 `cardback` 列 · `guid` = 由 `cardbackId` 查表补的 ·
            /// `alias` = 原版 cosmetic 名换了个写法（有整套证据）· `substitute` = **我们换的同阵营替身**（原版图本地没有）· `none`。</summary>
            public string cardbackFrom;
            /// <summary>**原版本来该用哪张**（只在与 `cardback` 不同时有值 —— 即 `alias` / `substitute` 那几种）。</summary>
            public string cardbackOrig;
            /// <summary>🆕 **2026-10-19（界面线 P05）**：这副牌**在练习窗 `Lore Text` 里显示哪条官方词条**
            /// （原版 `DemoDeckInfoSO.loreLocalizationKey`，例 `Demo/AeldariDeck1`）。
            /// <para>判据/映射 → `工具/gen_prebuilt_decks.py` 的 `DEMO_LORE_KEY` 头注释（84 份 Demo DeckInfo 的
            /// `classicDeck` / `skirmishDeck` PPtr 落到 `prebuiltdecks_assets_all` 的 `deckId`）。
            /// 103 副里 **78 副有**；其余 25 副是 Demo DeckInfo 之后新增的卡组，**本地没有 lore** ⇒ 空串
            /// （练习窗**不编文案**、出声；同 `DemoDeckInfoSO.get_Lore` 为空时的原版观感）。</para></summary>
            public string loreKey;
            /// <summary>取当前语档的 lore 正文（官方词条经 `Loc.T`：手写表 → bulk → 官方全表）。
            /// 空键 ⇒ 返回 <c>null</c>（调用方要**关掉/清空** `Lore Text`，⛔ 不拿别的文案顶）。
            /// ⚠️ 只做「键 → 文本」这一步；「哪一副用哪条键」只由生成器决定（`DEMO_LORE_KEY`），⛔ 别在运行时猜。</summary>
            public string LoreText
            {
                get { return string.IsNullOrEmpty(loreKey) ? null : Loc.T(loreKey); }
            }
            public string[] cardIds;   // 整副牌（**我们的卡 id**，按原版卡表原序；督军与防御卡都不在内）
            /// <summary>
            /// **我们补的防御卡**（我们的卡 id）。🔴 **原版预组牌没有防御卡** —— 已用反汇编证实
            /// （`CardDeck(PrebuiltDeck,…)` 传给 `DeckBasicSetup` 的第 4 个实参是 0）。用户 2026-09-26 拍板
            /// 「人机对战应该有防御卡，可以加入，由你选择加入」⇒ 生成器按「本阵营 defence 里 cost 最低 → id 最小」补一张。
            /// **这是我们的选择，不是原版的做法** —— 判据 → `资料/预组卡组_原版规格.md` §五之七。
            /// </summary>
            public string defensiveId;
            public string defensiveName;
            public string defensiveNameZh;
            public bool complete;      // 卡与督军**全部**解析得出（= 原版 `!HasHiddenCards` 那个位置）
            public string[] missing;   // 没解析出的原版 id（诊断用）
            public string[] problems;  // 张数不符 / 同名 >2（诊断用）
            public string[] overLegendary; // 「我们标成传说、原版放了 2 张」——**口径差不是缺陷**

            /// <summary>显示名。🔴 **2026-10-14（第二十八会话 · `A1803`）改成「官方 zh-CN 优先」**：
            /// 先取官方词条 `PrebuiltDecks/&lt;deckId&gt;`（**键的 join 在生成期做过**）。
            /// 🔴 **2026-10-22 就地订正（铁律 5 · `W62`）**：这段原来写「官方键是**原版 SO 名**
            /// `AstraMilitarumDeck1`，与我们的 `deckId`（`AM_SK_1`）**不同源** ⇒ 只能按**英文名**匹配；
            /// 现读 103 副里配上 **74** 副」—— **三处都错**：
            /// ① 原版 `LocalizedName` 拼的是 **`prefix + deckId`**（不是 SO 名 / 不是 `deckName`）；
            /// ② 我们的 `deckId` 与官方键**同源**，生成器**同形直取**即可；
            /// ③ 现读（2026-10-22 重跑 `gen_official_names.py` 之后）**103 副里 101 副**有官方词条。
            /// 判据 → `d:/2/tools/decomp_full/PrebuiltDeck__get_LocalizedName.c:15`（读 `+0x48` = `deckId`，
            /// `dump.cs:22915-22916`）+ `工具/_loc_official/gen_official_names.py` 的 `resolve_prebuilt_decks`。
            /// 取不到（现读只剩 **2** 副：`DA_SK_4` / `DarkAngelsDeck7`，官方表里**彻底没有**这两个键 ⇒
            /// 原版 `LocalizedName` 也是 null/空名）⇒ 回改前的口径
            /// （**有中文用中文，没有才回落英文原名** —— `zh_decks.json`，**我们自译**）。
            /// ⚠️ 换成官方值之后**界面中文会变**，这是**对的**（官方值）。
            /// ⚠️ 判据只此一处（与卡面同一条口径，见 `Core/CardText.cs`）。</summary>
            public string DisplayName
            {
                get
                {
                    string off = Loc.BulkOr("PrebuiltDecks/" + deckId, null);
                    if (!string.IsNullOrEmpty(off)) return off;
                    return string.IsNullOrEmpty(nameZh) ? name : nameZh;
                }
            }

            /// <summary>这张卡组用的卡背。**判据只一处** ⇒ `CardArt.DeckCardback`（原版 `CardDeck.GetDeckCardback()`）。</summary>
            public Texture2D Cardback { get { return CardArt.DeckCardback(cardback, faction); } }

            /// <summary>阵营图标名（走 `DeckRuntime.FactionIcon`，与收藏窗同一处）。</summary>
            public string FactionIcon { get { return DeckRuntime.FactionIcon(faction); } }
        }

        [System.Serializable]
        class Doc
        {
            public int format;
            public string note;
            public string pool;
            public int deckCount;
            public Deck[] decks;
        }

        // ============================ 🆕 2026-10-22：练习窗的「一对两副」（原版 `DemoDeckInfoSO`）============================
        //
        // 判据（D3 现解，可复跑）→ `资料/普查产出_1022/D3_DemoDeckInfo成对表.md`：
        //   · 原版 `PracticeModePopup.decks` 数组**恰好 78 项**（`menus_assets_all` 里那份 MB 的序列化值），
        //     每项一个 `DemoDeckInfoSO`，每份 SO 带 **`classicDeck` + `skirmishDeck` 两副** + 一条 `loreLocalizationKey`
        //     + `armyColor` / `armyIcon`；
        //   · 原版 `DemoDeckInfoSO.GetDeck(mode)`（`decomp_full/DemoDeckInfoSO__GetDeck.c:5-9`）：
        //     `mode == 0 (Classic)` → `+0x18`(classicDeck)、否则 → `+0x20`(skirmishDeck)；
        //   · `PracticeModePopup` 出厂 `gameMode = 0xd (Skirmish)`（`__ctor.c` 首句）⇒ 默认列 Skirmish 那 78 副。
        // ⇒ 左列**一格 = 一对**，格上画的是「**当前模式那一副**」的名字。

        /// <summary>`DemoDeckInfoSO.armyColor` 的**原样 4 个 float**（原版存的是任意浮点、不是 8bit/255）。
        /// 🔴 要精确复刻就用它 —— 产物里另有一个 `armyColor`（`#RRGGBB`）只是**四舍五入的便利值**，⛔ 别拿它画。</summary>
        [System.Serializable]
        public struct PairColor
        {
            public float r, g, b, a;
            public Color ToColor() { return new Color(r, g, b, a); }
        }

        /// <summary>一对里的**一侧**（= 一个 `PrebuiltDeck` 引用 + 它在我们这边的落点）。</summary>
        [System.Serializable]
        public class PairSide
        {
            public string deckId;      // 原版 deckId —— **查表键**（`PrebuiltDecks.ById`）
            public string deckName;    // 原版 deckName（英文）
            public int gameMode;       // 0 = Classic · 13 = Skirmish（与所在一侧一致）
            public int isPractice;     // 原版 `isPracticeDeck`（练习对里两半都可能是 0）
            public int cardCount;      // 原版卡表张数（30 / 12）
            public int difficulty;     // 原版难度
            public string heroId;      // 原版督军 id（**原版 id**，不是我们池里的；只作诊断）
        }

        /// <summary>练习窗左列的一格 = 一个 `DemoDeckInfoSO`。</summary>
        [System.Serializable]
        public class Pair
        {
            public string so;          // 原版 SO 资产名（坐标，可复查）
            public string kind;        // "practice" = 练习窗那 78 对 · "tutorial" = 教程 6 份（**不列**）
            public int uiOrder;        // 原版 `PracticeModePopup.decks` 数组下标 = 左列真实顺序（0..77）
            public string loreKey;     // 原版 `loreLocalizationKey`（一对两副**共用一条**）
            public string armyIcon;    // 原版 `armyIcon` 的 Sprite 名（`40k_DeckSelection_icon_*`）
            public PairColor armyColorRaw;   // 原版 `armyColor`（精确值）
            public PairSide classic, skirmish;
        }

        [System.Serializable]
        class PairDoc
        {
            public int format;
            public Pair[] pairs;
        }

        static Doc _doc;
        static bool _loaded;
        static List<Deck> _tab;
        static Doc _extra;
        static bool _extraLoaded;
        static List<Pair> _pairs;
        static PairDoc _pairDoc;
        static bool _pairLoaded;

        /// <summary>产物读到了没有（自检用；读不到会**出声**，不静默）。</summary>
        public static bool Available { get { Load(); return _doc != null && _doc.decks != null; } }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                Debug.LogError("[Prebuilt] 读不到 `Resources/" + ResourcePath + ".json` —— " +
                               "请跑 `python 工具/gen_prebuilt_decks.py` 生成（预组页会是空的）");
                return;
            }
            try { _doc = JsonUtility.FromJson<Doc>(ta.text); }
            catch (System.Exception e)
            {
                Debug.LogError("[Prebuilt] `" + ResourcePath + ".json` 解析失败：" + e.Message);
                return;
            }
            if (_doc == null || _doc.decks == null || _doc.decks.Length == 0)
                Debug.LogError("[Prebuilt] `" + ResourcePath + ".json` 里一副牌都没有 —— 重跑生成器");
        }

        /// <summary>产物里全部 103 副（含 Skirmish 与拼不齐的）。</summary>
        public static IList<Deck> All
        {
            get { Load(); return _doc != null && _doc.decks != null ? (IList<Deck>)_doc.decks : new Deck[0]; }
        }

        // ------------------------------------------------------------ 🆕 池外那 78 副

        static void LoadExtra()
        {
            if (_extraLoaded) return;
            _extraLoaded = true;
            var ta = Resources.Load<TextAsset>(ResourcePathExtra);
            if (ta == null)
            {
                // ⚠️ 读不到**不拦窗**：池那 103 副仍然可用 ⇒ 这里 Error 一次、练习窗会给缺的那几对出声。
                Debug.LogError("[Prebuilt] 读不到 `Resources/" + ResourcePathExtra + ".json` —— "
                               + "请跑 `python 工具/gen_practice_decks_extra.py` 生成（练习窗会缺掉池外那 78 副）");
                return;
            }
            try { _extra = JsonUtility.FromJson<Doc>(ta.text); }
            catch (System.Exception e)
            {
                Debug.LogError("[Prebuilt] `" + ResourcePathExtra + ".json` 解析失败：" + e.Message);
                return;
            }
        }

        /// <summary>🆕 **2026-10-22**：池外那 78 副（= 练习窗 78 对里落不进 103 副池的那些）。
        /// ⚠️ **它们不进预组页**（`Tab` 只看 <see cref="All"/>）—— 原版预组页只列 `isPractice == 1` 那一池。</summary>
        public static IList<Deck> AllExtra
        {
            get { LoadExtra(); return _extra != null && _extra.decks != null ? (IList<Deck>)_extra.decks : new Deck[0]; }
        }

        // ------------------------------------------------------------ 🆕 练习窗的成对表

        static void LoadPairs()
        {
            if (_pairLoaded) return;
            _pairLoaded = true;
            var ta = Resources.Load<TextAsset>(PairResourcePath);
            if (ta == null)
            {
                Debug.LogError("[Prebuilt] 读不到 `Resources/" + PairResourcePath + ".json` —— "
                               + "请跑 `python 工具/gen_demo_deck_pairs.py` 生成（练习窗左列会是空的）");
                return;
            }
            try { _pairDoc = JsonUtility.FromJson<PairDoc>(ta.text); }
            catch (System.Exception e)
            {
                Debug.LogError("[Prebuilt] `" + PairResourcePath + ".json` 解析失败：" + e.Message);
                _pairDoc = null;
            }
        }

        /// <summary>🆕 **2026-10-22**：练习窗左列的那 78 对（`kind == "practice"`，按原版 `uiOrder` 升序）。
        /// 🔴 **顺序就是原版 `PracticeModePopup.decks` 数组的顺序**（D3 采到的 `uiOrder`）——
        /// 它**不是**按阵营/难度排的，⛔ 别再排一次（原版 `SetArmyButtons` 只按阵营筛，不重排）。</summary>
        public static IList<Pair> Pairs
        {
            get
            {
                if (_pairs != null) return _pairs;
                LoadPairs();
                _pairs = new List<Pair>();
                if (_pairDoc == null || _pairDoc.pairs == null) return _pairs;
                foreach (var p in _pairDoc.pairs)
                    if (p != null && p.kind == "practice") _pairs.Add(p);
                _pairs.Sort((a, b) => a.uiOrder.CompareTo(b.uiOrder));
                if (_pairs.Count == 0)
                    Debug.LogError("[Prebuilt] `demo_deck_pairs.json` 里一对 `kind == \"practice\"` 都没有 —— 重跑生成器");
                return _pairs;
            }
        }

        /// <summary>一对里**当前模式那一副**（原版 `DemoDeckInfoSO.GetDeck(mode)`：`0 → classic`、否则 `→ skirmish`）。
        /// 取不到（产物缺这副 / 不在两个来源里）⇒ **`LogWarning` 出声一次**、返回 null —— ⛔ 不静默、⛔ 不按名字猜。</summary>
        public static Deck PairDeck(Pair p, GameMode mode)
        {
            if (p == null) return null;
            var side = mode == GameMode.Classic ? p.classic : p.skirmish;
            if (side == null || string.IsNullOrEmpty(side.deckId))
            {
                Debug.LogWarning("[Prebuilt] 练习对「" + p.so + "」在 " + mode + " 侧**没有 deckId**（产物缺字段）");
                return null;
            }
            var d = ById(side.deckId);
            if (d == null && _warnedMissingPairDeck.Add(side.deckId))
                Debug.LogWarning("[Prebuilt] 练习对「" + p.so + "」的 " + mode + " 侧 deckId = **" + side.deckId
                                 + "** 在 `prebuilt_decks.json` 与 `prebuilt_decks_extra.json` 里**都查不到** ⇒ "
                                 + "这一格跳过（如实出声，⛔ 不按名字猜一副顶替）");
            return d;
        }
        static readonly HashSet<string> _warnedMissingPairDeck = new HashSet<string>();

        /// <summary>
        /// **预组页签显示的那一批** = `complete`（原版那句 `Where(!HasHiddenCards)` 就在这个位置），
        /// 排序照原版：**`difficulty` 升序 → `deckArmy` 升序**（`DeckSelectionTabController__Awake`）。
        ///
        /// 🔴 **2026-09-26：原来这里还有一条 `gameMode != 0`（只列经典 30 张那批）—— 已删。**
        ///    那条是**我们的偏离**（当时的对战打不了 12 张的遭遇牌），不是原版做法；
        ///    现在引擎支持遭遇模式了（`GameplayVariables.Skirmish` + `GameMode`），
        ///    ⇒ 照原版**把 103 副里 `complete` 的全列出来**（经典 + 遭遇混在一页，
        ///    每格右下角的 `gameMode` 图标就是区分它们的地方）。
        /// </summary>
        public static IList<Deck> Tab
        {
            get
            {
                if (_tab != null) return _tab;
                _tab = new List<Deck>();
                foreach (var d in All)
                {
                    if (d == null) continue;
                    if (!d.complete) continue;          // ← 原版 `!HasHiddenCards` 的位置
                    _tab.Add(d);
                }
                _tab.Sort((a, b) =>
                {
                    int c = a.difficulty.CompareTo(b.difficulty);     // 升序（原版 OrderBy）
                    return c != 0 ? c : a.armyOrder.CompareTo(b.armyOrder); // 升序（原版 ThenBy）
                });
                return _tab;
            }
        }

        /// <summary>被筛掉的那些有几副（页面要**如实说明**，不许静默）。
        /// 判据与 <see cref="Tab"/> **逐字对应**：不列出来的只有「拼不齐」那一类
        /// （~~遭遇模式~~ 2026-09-26 起不再被筛）。</summary>
        public static int NotListed
        {
            get
            {
                int n = 0;
                foreach (var d in All) if (d != null && !d.complete) n++;
                return n;
            }
        }

        /// <summary>本页列出来的里面，经典 / 遭遇各几副（页面底部那行说明要用）。</summary>
        public static void CountByMode(out int classic, out int skirmish)
        {
            classic = skirmish = 0;
            foreach (var d in Tab)
            {
                if (d == null) continue;
                if (d.gameMode == (int)GameMode.Skirmish) skirmish++; else classic++;
            }
        }

        /// <summary>🆕 **2026-10-22**：按 **`deckId`** 查一副 —— **两个来源合并**（池 103 副 + 池外 78 副）。
        /// 🔴 **键必须是 `deckId`**：D3 实测「按名字兜底」不成立（30 条同名候选逐条比牌表，**30/30 内容不同**）。
        /// 顺序 = 先池后补充；两份产物的 `deckId` 生成期已断言**无交集**，所以先后只影响速度。</summary>
        public static Deck ById(string deckId)
        {
            if (string.IsNullOrEmpty(deckId)) return null;
            foreach (var d in All) if (d != null && d.deckId == deckId) return d;
            foreach (var d in AllExtra) if (d != null && d.deckId == deckId) return d;
            return null;
        }

        /// <summary>
        /// 选中一副预组之后**开战链还没接** —— 这条**只在 `PrebuiltDecks` 里写一份**，
        /// `PracticeModePopup` 与 `LiveOpsEventWindow` 都转发过来（两处各写一句迟早不一致）。
        ///
        /// 🔴 **为什么还不能开战**：两个原因，**都还没解决** ——
        ///   ① 开战走的是 `CollectionData.Select(DeckIndex)` → `DeckLibrary.Current`，而**预组不在玩家的卡组库里**；
        ///      要能开局得给 `BattleDriver` 开一条「**本局用这副牌**」的显式入口。
        ///   ② 造一份 `PlayerDeck` 还差**防御卡** —— 原版预组资产里**没有防御卡字段**
        ///      （`PrebuiltDeck` 只有 `cardLibraryIds`；防御卡在运行期的 `CardDeck.defensiveCard`(+0x48)），
        ///      而我们的 `DeckRules` 要求**恰好 1 张 defence**。它从哪来**还没查清**。
        /// 红线「不许静默失败」⇒ 选中时**必须出声**。
        /// </summary>
        // ⚠️ **2026-09-26 删掉了原来的 `WarnNotPlayableYet`** —— 那时预组还不能开战，选中只能出声；
        //    现在两条开战链（`PracticeModePopup` / `LiveOpsEventWindow`）都接了「本局用这副牌」通道
        //    （见下面的 `SetPendingBattleDeck`），选中即生效，**不需要再警告**。
        //    （留这段说明是免得下个会话去别处找那个方法。）

        /// <summary>
        /// 把一副预组搓成我们引擎要的 <see cref="PlayerDeck"/>。
        ///
        /// 原版走的是 `DeckAndWarlordData(PrebuiltDeck, PlayerDataManager)`（RVA `0x7280A0`，见 §五之七），
        /// 它内部同样先造一张 `CardDeck`；我们这边对等的就是 `PlayerDeck`。
        ///
        /// 🔴 **防御卡是我们补的**（原版预组那份是 `null` —— 已用反汇编证实）：判据 = 生成器
        /// `pick_defensive()`「本阵营 defence 里 cost 最低 → id 最小」，逐条见 `资料/预组卡组_原版规格.md` §五之七。
        /// </summary>
        public static PlayerDeck ToPlayerDeck(Deck d)
        {
            if (d == null) return null;
            var pd = new PlayerDeck
            {
                Name = d.DisplayName,
                WarlordId = d.heroId,
                DefensiveId = d.defensiveId,
                CardbackId = d.cardback,
                // 🆕 2026-09-26：**模式跟着这副牌走**（原版 `PrebuiltDeck.gameMode` @0x78）。
                //    战斗开局就凭**这一个字段**决定用哪套 `GameplayVariables`（原来是在
                //    `BattleDriver.BeginFromDeckLibrary` 里另读一次 `PendingSource`）——
                //    现在「本局什么模式」只有一个来源 = 这副牌自己，不许在别处再判一次。
                //    判据 → `资料/加时与冲突模式_原版规格.md` §2.7。
                GameMode = d.gameMode,
            };
            if (d.cardIds != null) pd.CardIds.AddRange(d.cardIds);
            return pd;
        }

        // ============================================================ 「本局用这副牌」
        //
        // 🔴 **为什么要这条通道**：我们的开战链是 `CollectionData.Select(DeckIndex)` → `DeckLibrary.Current`
        //    （`BattleDriver.PickSavedDeck`）—— **预组不在玩家的卡组库里**，所以它永远轮不到。
        //    原版没这个问题：`MatchData.SetPlayerDeck` 收的就是一个 `CardDeck`（预组也是 CardDeck）。
        // ⇒ 在选卡组窗里挑了一副预组时把 `PlayerDeck` **放这儿**，战斗开局**读一次就清**。
        //    ⚠️ **不写 `DeckLibrary`** —— 不许悄悄改玩家的卡组库（那是「领奖把整副发下来」才做的事）。

        static PlayerDeck _pending;
        static Deck _pendingSrc;

        /// <summary>本局要用哪副预组（没挑就是 null）。</summary>
        public static Deck PendingSource { get { return _pendingSrc; } }

        /// <summary>选卡组窗挑中一副预组时调这个（同时会把「我的卡组」那条选择清掉）。</summary>
        public static void SetPendingBattleDeck(Deck d)
        {
            _pendingSrc = d;
            _pending = ToPlayerDeck(d);
        }

        /// <summary>选了「我的卡组」时调这个，把预组那条作废。</summary>
        public static void ClearPendingBattleDeck() { _pendingSrc = null; _pending = null; }

        /// <summary>开局读一次（**读完就清** —— 下一局不该还带着它）。没有给 null。</summary>
        public static PlayerDeck TakePendingBattleDeck()
        {
            var d = _pending;
            _pending = null;
            return d;
        }

        /// <summary>自检用：把缓存丢掉，下次重新从 `Resources` 读。</summary>
        public static void ResetForTest()
        {
            _doc = null; _loaded = false; _tab = null; _pending = null; _pendingSrc = null;
            _extra = null; _extraLoaded = false;
            _pairDoc = null; _pairLoaded = false; _pairs = null;
            _warnedMissingPairDeck.Clear();
        }

        // ============================================================ 门面方法（账的落点用）
        // 🔴 **为什么要有这两个**：账里 `DemoDeckInfoSO.GetName` / `PrebuiltDeck.HasHiddenCards` 是**方法**，
        //   而本类原来只有**属性**（`Deck.DisplayName` / `Deck.complete`）—— `方案 §4.1` 明令
        //   「纯属性 getter 不得当方法落点」⇒ 各包一层与原版**同形**的名字。
        //   逻辑一律不在这里另写一份（⛔ 两处写同一条规则 ⇒ 迟早不一致）。
        //   ⚠️ 追加在**类尾**（上面那些行号被账引用，插在中间会造成漂移）。

        /// <summary>≡ 原版 `DemoDeckInfoSO.GetName(PlayModes mode)`（🥇 `DemoDeckInfoSO__GetName.c:7-27`）：
        /// `mode == Classic(0)` 取 `classicDeck`（`+0x18`）、否则取 `skirmishDeck`（`+0x20`），再
        /// `I2 GetTranslation("PrebuiltDecks/" + deck.deckId(+0x48))` —— 与 <see cref="Deck.DisplayName"/> 同一条 join。
        /// <para>🔶 自己设计（非原版）：**官方表缺键时的回落**。原版 `I2 GetTranslation` 缺键返回键名本身
        /// （屏幕上会是 `PrebuiltDecks/DA_SK_4`），我们回落自译中文 / 英文原名
        /// （现读 **2/103** 副：`DA_SK_4` · `DarkAngelsDeck7`，`official_loc_13lang.json` 的 10,862 键里查无）。
        /// 为什么必须自己定：那两个键**官方表彻底没有**（判据为空，不是我们没找到）；
        /// 取「能读的名字」而不是把键名印给玩家。判据与理由的完整版 → `Deck.DisplayName` 的头注释（`:103-117`）。
        /// 调度台 2026-10-26 裁定保持。</para></summary>
        public static string DeckName(Deck d) { return d == null ? null : d.DisplayName; }

        /// <summary>≡ 原版 `PrebuiltDeck.HasHiddenCards()`（🥇 `PrebuiltDeck__HasHiddenCards.c:16-63`）：
        /// 原版 = ① 督军（`runTimeDeckHero ?? deckHero.Reference`）非空且 `!IsHidden()`，
        /// ② 卡库里**任一张** `IsHidden()` ⇒ true。我们的等价物 = **生成期**算好的 `Deck.complete` 位
        /// （`!complete` ⇔ 有解析不出的原版 id / 督军 ⇒ 判据在 `工具/gen_prebuilt_decks.py`）。
        /// <para>**机制差异**：原版**运行期逐张扫** `IsHidden()`；我们**生成期**一次性判定并把结果存成一位
        /// —— 同一份卡表下净结果相同，但「生成之后卡表再变」我们**不会**跟着变（原版会）。</para>
        /// <para>`d == null` ⇒ 原版走到 `FUN_1803f47a0`（抛）；我们返回 `true`（当「拼不齐」筛掉它，不炸）——如实登记。</para></summary>
        public static bool HasHidden(Deck d) { return d == null || !d.complete; }

        // ============================================================ 卡组:284 / 285 / 286（原版 `PrebuiltDeckCollection`）
        //
        // 原版那三张表（`decks` / `botDecks` / `practiceDecks`）与两个入口，判据逐句读自：
        //   · `decomp_full/PrebuiltDeckCollection__AddDeck.c:21-40` ＋ `cpp2il/out_cpp2il_dev_ilspy/PrebuiltDeckCollection.cs:234-253`
        //     ① 资产名含 "Tutorial" ⇒ 直接 return；② 否则 `decks.Add`；③ `Type == Bot(0)` ⇒ `botDecks`；④ `isPracticeDeck` ⇒ `practiceDecks`
        //   · `decomp_full/PrebuiltDeckCollection__GetDeckByDifficulty.c`（难度+模式过滤 → **未过滤表**长度取下标 → ElementAtOrDefault）
        //   · `decomp_full/PrebuiltDeckCollection__GetRandomDeck.c`（四道过滤 → `Random.Range(0, Count)`）
        // 数据 → `Resources/prebuilt_deck_lists.json`（`工具/gen_prebuilt_bot_decks.py`：224 副底表 + 每副的分类位）。
        //
        // ⚠️ **三份产物并列，⛔ 别合并**（那两份的 `git diff` 必须能要求「逐字节 0」）：
        //   `prebuilt_decks.json`       预组页那一池（103 副，`isPractice == 1`）—— `All` / `Tab`
        //   `prebuilt_decks_extra.json` 练习窗 78 对里落不进池的另外 78 副 —— `AllExtra`
        //   `prebuilt_deck_lists.json`  原版 `decks` 全部非教程副（224 副）＋ `botDecks` 的分类位 —— `Decks` / `BotDecks`
        // 🔴 **别拿 `All` 当原版的 `decks`**：`All` 是 103 副（按 isPractice 划），原版 `decks` 是 224 副（按「名字含 Tutorial」划）。

        /// <summary>`Assets/RuleEngine/Resources/prebuilt_deck_lists.json` —— 原版 `PrebuiltDeckCollection` 三张表的底表。</summary>
        public const string ListResourcePath = "prebuilt_deck_lists";

        /// <summary>原版 `PrebuiltDeck.DeckType.Bot` 的枚举值（`cpp2il dev PrebuiltDeck.cs:10-16`：Bot 0 · Default 1 · Tutorial 2 · Custom 3）。
        /// `AddDeck` 判的就是这一个数（`.c` 里 `*(int *)(deck + 0x98) == 0`）。</summary>
        public const int DeckTypeBot = 0;

        /// <summary>一副预组的**分类位**（原版 `PrebuiltDeck.Type` ＋ `isPracticeDeck`）—— `AddDeck` 就是凭这两个字段
        /// 把同一副牌分别塞进 `decks` / `botDecks` / `practiceDecks` 三张表。
        /// 🔴 分类在**生成期**定死（与 <see cref="HasHidden"/> 同一条口径）；原版是运行期读 SO 字段。</summary>
        [System.Serializable]
        public class DeckMeta
        {
            public string deckId;      // 与 `decks[]` 里 `Deck.deckId` 同键
            public string srcName;     // 原版 SO 资产名（= `deck.name`，`AddDeck` 拿它判 "Tutorial"）—— 诊断用
            public int type;           // 原版 `PrebuiltDeck.DeckType`（本表只有 0 Bot / 1 Default —— 教程那批进不了 `decks`）
            public int isPractice;     // 原版 `isPracticeDeck`（0/1）
        }

        [System.Serializable]
        class ListDoc
        {
            public int format;
            public string note;
            public string source;
            public int deckCount;      // 原版 `decks.Count`（现读 224）
            public int botCount;       // 原版 `botDecks.Count`（现读 196）
            public int practiceCount;  // 原版 `practiceDecks.Count`（现读 103 —— 与 `prebuilt_decks.json` 那池同一批，生成器已断言）
            public Deck[] decks;       // 原版 `decks`（**不是**预组页那 103 副）
            public DeckMeta[] meta;    // 与 `decks` 同序同长（生成器断言）
        }

        static ListDoc _lists;
        static bool _listsLoaded;
        static List<Deck> _botDecks;
        static Dictionary<string, DeckMeta> _metaById;

        static void LoadLists()
        {
            if (_listsLoaded) return;
            _listsLoaded = true;
            var ta = Resources.Load<TextAsset>(ListResourcePath);
            if (ta == null)
            {
                Debug.LogError("[Prebuilt] 读不到 `Resources/" + ListResourcePath + ".json` —— "
                               + "请跑 `python 工具/gen_prebuilt_bot_decks.py` 生成（bot 预组池会是空的 ⇒ "
                               + "对局对手只能退回按卡池自动凑）");
                return;
            }
            try { _lists = JsonUtility.FromJson<ListDoc>(ta.text); }
            catch (System.Exception e)
            {
                Debug.LogError("[Prebuilt] `" + ListResourcePath + ".json` 解析失败：" + e.Message);
                _lists = null;
                return;
            }
            if (_lists == null || _lists.decks == null || _lists.decks.Length == 0)
                Debug.LogError("[Prebuilt] `" + ListResourcePath + ".json` 里一副牌都没有 —— 重跑生成器");
            else if (_lists.decks.Length != _lists.deckCount)
                Debug.LogError("[Prebuilt] `" + ListResourcePath + ".json` 的 deckCount=" + _lists.deckCount
                               + " 与实际 " + _lists.decks.Length + " 条不符 —— 重跑生成器");
        }

        static void BuildMetaIndex()
        {
            if (_metaById != null) return;
            _metaById = new Dictionary<string, DeckMeta>();
            LoadLists();
            if (_lists == null || _lists.meta == null) return;
            foreach (var m in _lists.meta)
                if (m != null && !string.IsNullOrEmpty(m.deckId)) _metaById[m.deckId] = m;
        }

        /// <summary>这副牌的分类位（原版 SO 上的 `Type` / `isPracticeDeck`）。
        /// 查不到（不在这张表里 / 产物缺）⇒ <c>null</c>（真发生过就是产物问题，调用方别拿它当「不是 bot」）。</summary>
        public static DeckMeta MetaOf(Deck d)
        {
            if (d == null || string.IsNullOrEmpty(d.deckId)) return null;
            BuildMetaIndex();
            DeckMeta m;
            return _metaById.TryGetValue(d.deckId, out m) ? m : null;
        }

        /// <summary>≡ 原版 `PrebuiltDeckCollection.Decks`（`IReadOnlyList&lt;PrebuiltDeck&gt; Decks =&gt; decks`）：
        /// **所有非教程副**（现读 224 副）。原版 `AddDeck` 对资产名含 "Tutorial" 的直接 return ⇒ 本表天然不含教程副。
        /// 🔴 **不是** <see cref="All"/>（那是预组页那 103 副 `isPractice == 1` 的池）。</summary>
        public static IList<Deck> Decks
        {
            get
            {
                LoadLists();
                return _lists != null && _lists.decks != null ? (IList<Deck>)_lists.decks : new Deck[0];
            }
        }

        /// <summary>≡ 原版 `PrebuiltDeckCollection.BotDecks`：<see cref="Decks"/> 里 `Type == Bot(0)` 的那些
        /// （现读 **196** 副 —— 验收口就用这个数：原版 `Type==Bot` 的 SO 有多少副，这里就该有多少副）。
        /// 顺序 = <see cref="Decks"/> 的顺序（= 生成期按 SO 资产名升序；原版是 addressables 装载序，**不可复现**，
        /// 影响面只有「同一个下标抽到哪一副」，见 <see cref="GetDeckByDifficulty"/>）。</summary>
        public static IList<Deck> BotDecks
        {
            get
            {
                if (_botDecks != null) return _botDecks;
                _botDecks = new List<Deck>();
                foreach (var d in Decks)
                {
                    var m = MetaOf(d);
                    if (m != null && m.type == DeckTypeBot) _botDecks.Add(d);
                }
                if (_botDecks.Count == 0)
                    Debug.LogError("[Prebuilt] bot 预组池是空的（`" + ListResourcePath
                                   + ".json` 的 meta 里 0 条 `Type == Bot`）—— 重跑生成器");
                return _botDecks;
            }
        }

        /// <summary>原版 `GetDeckByDifficulty` 里那串 `Where` —— **只有** `difficulty` 与 `gameMode` 两道
        /// （`.c` 的 `System_Predicate` 体：`d.difficulty != difficultyLevel ⇒ false`、`d.gameMode != playMode ⇒ false`）。
        /// ⚠️ **不过滤 `HasHiddenCards`、不过滤 Type** —— 原版这一支就没有那两道，照抄。</summary>
        public static List<Deck> FilterByDifficulty(int difficulty, int playMode)
        {
            var o = new List<Deck>();
            foreach (var d in Decks)
                if (d != null && d.difficulty == difficulty && d.gameMode == playMode) o.Add(d);
            return o;
        }

        /// <summary>≡ 原版 `PrebuiltDeckCollection.GetDeckByDifficulty(difficultyLevel, playMode)`
        /// （🥇 `decomp_full/PrebuiltDeckCollection__GetDeckByDifficulty.c` · 🥈 `cpp2il dev PrebuiltDeckCollection.cs:200-232`）：
        /// ① 按 `(difficulty, gameMode)` 过滤；② **`Random.Range(0, 未过滤表长度)`**；③ 用那个下标去 `ElementAtOrDefault(过滤后序列)`。
        ///
        /// 🔴🔴 **②③ 是原版自带的缺陷，照抄、⛔ 不修**（调度台 2026-10-26 明令）：下标取自**未过滤**的
        ///   `decks`（现读 224），而序列是过滤后那串（现读 12～74 条）⇒ `index ≥ 过滤后条数` 时
        ///   `ElementAtOrDefault` 返回 **null**（原版调用方 `MatchData.SetEnemyRandomDeckData` 随后打
        ///   `[ERROR] Can't find a prebuilt deck for the given player match tier`）。
        ///   现读七个 (难度, 模式) 格子的命中率只有 **5.4% ～ 33.0%**（表由 `工具/gen_prebuilt_bot_decks.py` 打印）。
        ///
        /// ⚠️ **与原版唯一的机制差异**：原版 `UnityEngine.Random`，我们收 `System.Random`
        ///   （工程铁律「引擎里只用 `System.Random(seed)`」）⇒ 形状与「同 seed 同结果」一致，**具体抽到哪一副不同**。
        /// ⚠️ 过滤后为空 / 下标越界 ⇒ 返回 null（原版 `ElementAtOrDefault` 也是 null，**不是**我们把错吞了）。
        /// ⚠️ 本机**没有活调用点**：原版唯一调用者是排行 bot（`MatchData.cs:1385`，难度取自远端 ranked 事件），
        ///   我们离线没有排位事件 ⇒ 这条只作为**判据齐备的等价实现**存在（账 `卡组:285`）。</summary>
        public static Deck GetDeckByDifficulty(int difficulty, int playMode, System.Random rng)
        {
            if (rng == null) { Debug.LogError("[Prebuilt] GetDeckByDifficulty 没给 rng —— 返回 null"); return null; }
            var filtered = FilterByDifficulty(difficulty, playMode);
            int total = Decks.Count;                                   // ← 原版取的就是**未过滤**表的 Count
            if (total == 0) return null;
            int index = rng.Next(total);                               // ← 原版 `UnityEngine.Random.Range(0, decks.Count)`
            var picked = index < filtered.Count ? filtered[index] : null;  // ← 原版 `ElementAtOrDefault(过滤后序列, index)`
            if (picked == null)
                Debug.LogWarning("[Prebuilt] GetDeckByDifficulty(" + difficulty + " / " + playMode + ")：下标 "
                               + index + " ≥ 过滤后 " + filtered.Count + " 条 ⇒ null（**原版就是这样** —— "
                               + "它拿未过滤的 " + total + " 取下标，照抄不修）");
            return picked;
        }

        /// <summary>≡ 原版 `GetRandomDeck` 里那串过滤（🥇 `decomp_full/PrebuiltDeckCollection__GetRandomDeck.c`）：
        /// 逐副依次过 ① 阵营可选 ② 命中 `selectableBotDecks` 名单 ③ `!HasHiddenCards` ④ `!DeckHasBannedCards`。
        /// 顺序照 `.c`（① → ② → ③ → ④，任一不过就 continue）；遍历的是 <see cref="Decks"/>（224 副，**含 28 副 `Type==Default`**）。
        /// <para>原版三个形参我们只拿得到两个半（见下），另外两道是我们补的，逐条标了原因。</para></summary>
        /// <param name="possibleArmies">原版 `IReadOnlyCollection&lt;CardArmy&gt; possibleArmies` —— 按**督军阵营**（`deckHero.cardArmy`）过滤。
        /// 原版只在「集合非 null **且** Count &gt; 0」时才过滤（`.c`：`param_3 == 0 || Count &lt; 1 ⇒ 跳过这道`）；
        /// 离线调用者（`MatchData` 的 `PracticeOffline` 支）传的就是 null ⇒ 不过滤。我们这边用 `Deck.armyOrder`（= 该阵营的 `CardArmy` 枚举值）。</param>
        /// <param name="selectableDeckIds">原版 `List&lt;LoadableReference&lt;PrebuiltDeck&gt;&gt; selectableBotDecks` 的 `TargetId` 名单
        /// （= 那副牌的 `deckId`，`PrebuiltDeck__GetID` 返回的就是它）。🔶 原版的名单来自**远端** `AIBotsConfig.botDecks`
        /// （PlayFab titleData）；本机 0 命中（账 `战斗逻辑驱动:1059` 判 D）⇒ 传 null = 「没有名单、全部可选」。</param>
        /// <param name="bannedCardIds">原版 `IPlayEvent currentEvent` 的封禁表（`PlayEventData.IsCardBanned`，逐张问）。
        /// 🔶 原版判据在**远端事件**里；本机没有活动事件 ⇒ 传 null = 「没有封禁」。**别拿卡池猜封禁**。</param>
        /// <param name="playMode">🔶 自己设计（非原版）：原版 `GetRandomDeck` **不过滤模式** —— 模式跟着匹配结果走；
        /// 我们的模式由玩家选，不过滤会拿到 12 张的遭遇副去打经典局（构筑/开局全错）。判据为空（原版无此道），
        /// 调度台 2026-10-26 裁定保留这一道。null = 不过滤。</param>
        /// <param name="faction">🔶 自己设计（非原版）：原版用 `possibleArmies`（可空）而不是阵营字符串；我们的对手阵营
        /// 是先选好的（战场/环境都跟着它）⇒ 收一个字符串更直接。null = 不过滤。判据同上，调度台 2026-10-26 裁定。</param>
        public static List<Deck> FilterDecks(ICollection<string> selectableDeckIds = null,
                                            ICollection<int> possibleArmies = null,
                                            ICollection<string> bannedCardIds = null,
                                            int? playMode = null,
                                            string faction = null)
        {
            var o = new List<Deck>();
            bool armies = possibleArmies != null && possibleArmies.Count > 0;   // ← 原版 `Count < 1 ⇒ 跳过滤`
            foreach (var d in Decks)
            {
                if (d == null) continue;
                // ① 阵营可选（原版：`possibleArmies.Contains(deck.DeckHero.cardArmy)`）
                if (armies && !possibleArmies.Contains(d.armyOrder)) continue;
                // ② 命中远端可选名单（原版：`selectableBotDecks.Find(x => x.TargetId == deck.deckId) != null`）
                if (selectableDeckIds != null && !selectableDeckIds.Contains(d.deckId)) continue;
                // ③ !HasHiddenCards（我们的等价位 = 生成期算好的 `complete`，见 <see cref="HasHidden"/>）
                if (HasHidden(d)) continue;
                // ④ !DeckHasBannedCards（原版逐张问 currentEvent；我们只有一张 id 集合）
                if (bannedCardIds != null && d.cardIds != null)
                {
                    bool banned = false;
                    foreach (var cid in d.cardIds)
                        if (cid != null && bannedCardIds.Contains(cid)) { banned = true; break; }
                    if (banned) continue;
                }
                // ⑤ 🔶 自己设计（非原版）：模式闸 —— 原版无此道，理由见 `playMode` 参数注释
                if (playMode.HasValue && d.gameMode != playMode.Value) continue;
                // ⑥ 🔶 自己设计（非原版）：阵营闸 —— 原版走 `possibleArmies`，理由见 `faction` 参数注释
                if (!string.IsNullOrEmpty(faction) && d.faction != faction) continue;
                o.Add(d);
            }
            return o;
        }

        /// <summary>≡ 原版 `PrebuiltDeckCollection.GetRandomDeck(selectableBotDecks, possibleArmies, currentEvent)`
        /// （🥇 `decomp_full/PrebuiltDeckCollection__GetRandomDeck.c:113-127` 取那一跳：`Random.Range(0, list.Count)` → `list[index]`）。
        /// 过滤本体在 <see cref="FilterDecks"/>（**同一条过滤只写一处**）。
        ///
        /// ⚠️ 原版开头还有一句 `FeatureConfig.ShouldDisableNewMatchMaking()` 决定「② 那道名单闸要不要走」；
        ///   那个标志也在**远端** `FeatureConfig`（`ConfigManager.GetConfig&lt;FeatureConfig&gt;()`，离线为 null ⇒
        ///   `return false`），而它一旦为 false 就要用上 `selectableBotDecks` —— 离线那份名单是 null ⇒ 原版这条链**必 NRE**。
        ///   ⇒ 我们的口径：名单传 null 就**跳过**那道（注释在 `selectableDeckIds` 参数上）。
        ///
        /// ⚠️ 抽不到（过滤后为空）⇒ 原版会在空表上取 `[0]` 抛 `ArgumentOutOfRangeException`；
        ///   我们**返回 null 并出声**（与 <see cref="HasHidden"/> 的「原版抛、我们出声」同一条口径）。
        /// ⚠️ 原版用 `UnityEngine.Random`，我们收 `System.Random`（可复现）；具体抽到哪一副不同，分布形状相同。</summary>
        public static Deck GetRandomDeck(System.Random rng,
                                         ICollection<string> selectableDeckIds = null,
                                         ICollection<int> possibleArmies = null,
                                         ICollection<string> bannedCardIds = null,
                                         int? playMode = null,
                                         string faction = null)
        {
            if (rng == null) { Debug.LogError("[Prebuilt] GetRandomDeck 没给 rng —— 返回 null"); return null; }
            var cand = FilterDecks(selectableDeckIds, possibleArmies, bannedCardIds, playMode, faction);
            if (cand.Count == 0)
            {
                Debug.LogWarning("[Prebuilt] GetRandomDeck：一副都过不了过滤（Decks=" + Decks.Count
                               + " · playMode=" + (playMode.HasValue ? playMode.Value.ToString() : "any")
                               + " · faction=" + (string.IsNullOrEmpty(faction) ? "any" : faction)
                               + "）⇒ 返回 null（原版此处会在空表上取 [0] 抛异常）；调用方须**出声**并退兜底");
                return null;
            }
            return cand[rng.Next(cand.Count)];                          // ← 原版 `UnityEngine.Random.Range(0, list.Count)`
        }

        /// <summary>自检用：把这三张表的缓存也丢掉（<see cref="ResetForTest"/> 在类中部，⛔ 别往那里加行 —— 账的落点会漂）。</summary>
        public static void ResetListsForTest()
        {
            _lists = null; _listsLoaded = false; _botDecks = null; _metaById = null;
        }

        // ============================================================ 卡组:290（原版 `PrebuiltPack.IsValid`）
        //
        // 判据：🥇 `decomp_full/PrebuiltPack__IsValid.c`（逐句）+ 🥈 `cpp2il dev PrebuiltPack.cs:195-230`：
        //   `cardLibrary = GetCardLibrary()`（把 `cardIds` 逐张在全卡表里 `Find`，**查不到就跳过**）
        //   ① `cardLibrary.Count != cardIds.Count` ⇒ false
        //   ② 逐张 `IsHidden()` 任一为真 ⇒ false
        //   ③ 全过 ⇒ true
        // 活调用点（曾经被判「0 调用点」的那条已推翻）：`cpp2il dev Everguild.LiveOps/DraftEventBase.cs:378`
        //   的 `_GetRandomPack_b__0`：`pack.packEnabled && pack.packArmy == warlord.cardArmy && pack.IsValid()`。

        /// <summary>≡ 原版 `PrebuiltPack.IsValid()`（两条件，见上面那段判据）。
        /// <para><paramref name="resolvedCount"/> = 把 `cardIds` 逐张在我们卡池里查过之后**查到的张数**
        /// （原版 `GetCardLibrary().Count` —— 查不到的 id 原版是**跳过**，所以「少一张」就是条件①挂）。
        /// 现读 882 个包里 882 个都能全查到 ⇒ 当前恒 true（⇒ **断言必须自己造负例**，见下）。</para>
        /// <para>🔶 自己设计（非原版）：`hiddenCardIds` 这一半的**判据在远端** —— 原版逐张问 `RawCardScript.IsHidden()`，
        /// 而那一条是 `PlayerItem.IsHidden()` → `FeatureConfig.IsItemHidden(this)`（`FeatureConfig` 由
        /// `ConfigManager.GetConfig&lt;FeatureConfig&gt;()` 从 PlayFab titleData 下发，本机 0 命中）。
        /// 拿不到 ⇒ 本机默认**空集**（= 没有卡被隐藏，与「配置为空」等价）。为什么必须自己定：判据为空，
        /// 不是我们没查到。**接口留在这里**是为了 ① 负例造得出来（传一个非空集合即可）② 将来接回远端只改一行。
        /// 调度台 2026-10-26 裁定。</para>
        /// <para>`cardIds == null` ⇒ 原版会抛（`GetCardLibrary` 的 `cardIds == null` 分支）；我们返回 false。
        /// 有一张没查到（`resolvedCount &lt; cardIds.Length`）⇒ false（条件①）。</para></summary>
        public static bool PackIsValid(string[] cardIds, int resolvedCount, ICollection<string> hiddenCardIds = null)
        {
            if (cardIds == null) return false;
            if (resolvedCount != cardIds.Length) return false;               // ① 有 id 解析不出（原版跳过它 ⇒ 条数不等）
            if (hiddenCardIds != null && hiddenCardIds.Count > 0)            // ② 逐张 IsHidden()
                foreach (var cid in cardIds)
                    if (cid != null && hiddenCardIds.Contains(cid)) return false;
            return true;
        }

        // ============================================================ 活动卡组表（原版 `PlayerDataManager.fullEventAvailableDecks`）
        //
        // 判据（🥇 反编译真体，六段一一对应）：
        //   GetClosedDeckById      → cpp2il dev PlayerDataManager.cs:12242-12271 · decomp_full/PlayerDataManager__GetClosedDeckById.c
        //   GetClosedDeckByPackIds → cpp2il dev PlayerDataManager.cs:12272-12380 · decomp_full/PlayerDataManager__GetClosedDeckByPackIds.c
        //   GetDeckIdWithPacks     → cpp2il dev PlayerDataManager.cs:12382-12406 · decomp_full/PlayerDataManager__GetDeckIdWithPacks.c
        //   GetPackById            → cpp2il dev PlayerDataManager.cs:12407-12440 · decomp_full/PlayerDataManager__GetPackById.c
        //   GetClosedDeckByCards   → cpp2il dev PlayerDataManager.cs:12441-12520 · decomp_full/PlayerDataManager__GetClosedDeckByCards.c
        //   IsEventDeckFromWarlord → cpp2il dev PlayerDataManager.cs:8209-8260  · decomp_full/PlayerDataManager__IsEventDeckFromWarlord.c
        // 六条的共同形状 = **纯客户端查表**（`Enumerable.Where` + `FirstOrDefault`），没有一处服务器调用。
        // 唯一的外部依赖 = `fullEventAvailableDecks` 那张活动卡组表（PlayFab 下发，P3 数据）。
        //
        // 🔶 自己设计（非原版）：活动卡组表 `fullEventAvailableDecks` 的本地替身 · 原版那张表由 PlayFab 下发，
        //   本机 0 命中（P3 数据，按方案 §五边界②「数据本地模拟 + 出声」）· 调度台 2026-10-27 裁定（B88_J2 批次）
        //   为什么必须自己定：函数体是纯客户端查表（判据在、数据不在）；空表 = 与「云端没有活动卡组」等价。
        //   接口留在这里是为了 ① 负例造得出来（`SetEventDecksForTest`）② 将来接回远端只改装载那一行。
        // ⚠️ 追加在**类尾**（上面那些行号被账引用，插在中间会造成漂移）。

        static List<Deck> _eventDecks;

        /// <summary>原版 `fullEventAvailableDecks` 的本地替身（默认空表 = 云端没有活动卡组）。
        /// 元素只用到 `deckId` / `heroId` / `cardIds` 三格；`Deck` 的其余字段不影响这六个方法。</summary>
        public static IList<Deck> EventDecks
        {
            get { if (_eventDecks == null) _eventDecks = new List<Deck>(); return _eventDecks; }
        }

        /// <summary>自检 / 将来远端装载用：整表替换。传 null = 清空。</summary>
        public static void SetEventDecksForTest(IList<Deck> decks)
        {
            _eventDecks = new List<Deck>();
            if (decks != null) foreach (var d in decks) if (d != null) _eventDecks.Add(d);
        }

        /// <summary>≡ 原版 `PlayerDataManager.GetClosedDeckById(string deckId)`（判据见上面那一段）：
        /// `fullEventAvailableDecks.Where(x =&gt; x != null &amp;&amp; x.deckId == deckId).FirstOrDefault()`。
        /// 查不到给 null（原版 `FirstOrDefault` 同一档）。</summary>
        public static Deck GetClosedDeckById(string deckId)
        {
            if (string.IsNullOrEmpty(deckId)) return null;
            var all = EventDecks;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d != null && d.deckId == deckId) return d;
            }
            return null;
        }

        /// <summary>≡ 原版 `GetClosedDeckByCards(RawCardScript warlord, List&lt;RawCardScript&gt; cards)`：
        /// 在活动卡组里找**督军相同的**每一副 → 拷一份它的 `cardLibraryIds` → 逐张 `Remove(card.uniqueId)` →
        /// **一张不剩**的那一副就是答案（第一副命中的）。没有 ⇒ null。
        /// <para>原版 `DeckHero` 是 PPtr 比引用；我们比 `Deck.heroId == warlordHeroId`（同一条「按 id 认牌」）。</para></summary>
        public static Deck GetClosedDeckByCards(string warlordHeroId, IList<string> cardIds)
        {
            var all = EventDecks;
            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (d == null || d.heroId != warlordHeroId) continue;
                var rest = new List<string>(d.cardIds != null ? d.cardIds : new string[0]);
                if (cardIds != null)
                    for (int k = 0; k < cardIds.Count; k++)
                        if (cardIds[k] != null) rest.Remove(cardIds[k]);
                if (rest.Count == 0) return d;
            }
            return null;
        }

        /// <summary>≡ 原版 `IsEventDeckFromWarlord(string warlordId, string deckId)`：
        /// 按 `deckId` 取活动卡组 → 有、且它的督军 == `warlordId` ⇒ true；
        /// **取不到 / 没有督军** ⇒ 原版 `CustomDebug.LogError("Can't find deckFaction")` + false（照抄，含那句日志）。</summary>
        public static bool IsEventDeckFromWarlord(string warlordId, string deckId)
        {
            var d = GetClosedDeckById(deckId);
            if (d != null && !string.IsNullOrEmpty(d.heroId))
                return d.heroId == warlordId;
            Debug.LogError("[Prebuilt] Can't find deckFaction（IsEventDeckFromWarlord deckId=" + deckId + "）");
            return false;
        }

        /// <summary>≡ 原版 `GetPackById(string packId)`：在全部 `PrebuiltPack` 里按 `packId` 取第一张。
        /// 落点 = <see cref="DraftPacks.Resolve"/>（**同一份数据、同一张表**：882 个包）。
        /// <para>⚠️ 类型差异（如实标）：原版返回 `PrebuiltPack` SO；我们返回 <see cref="DraftPacks.PackDto"/>。</para></summary>
        public static DraftPacks.PackDto GetPackById(string packId)
        {
            return DraftPacks.Resolve(packId);
        }

        /// <summary>≡ 原版 `GetDeckIdWithPacks(List&lt;string&gt; prebuiltPacks)`：逐个 `GetPackById`，
        /// 取到的就把 **`pack.packId + ";"`** 追加（**查不到的包静默跳过** —— 原版那句 `if (packById != null)`）；
        /// 入参 null ⇒ 返回空串。</summary>
        public static string GetDeckIdWithPacks(IList<string> prebuiltPacks)
        {
            if (prebuiltPacks == null) return "";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < prebuiltPacks.Count; i++)
            {
                var p = GetPackById(prebuiltPacks[i]);
                if (p != null) sb.Append(p.packId).Append(';');
            }
            return sb.ToString();
        }

        /// <summary>≡ 原版 `GetClosedDeckByPackIds(string warlordId, List&lt;string&gt; packIds)`：
        /// 督军存在 ⇒ 每个 packId 取包 ⇒ 组一副 ClosedDeck；`packIds == null` ⇒ 出声 + 空牌组；
        /// **督军不存在** ⇒ 原版 `CustomDebug.LogError("GetClosedDeckByPackIds hero null")` + null。
        /// <para>落点 = <see cref="DraftPacks.BuildClosedDeck"/>（同一段「包 → 12 张」的逻辑，⛔ 不另写一份）。</para>
        /// <para>⚠️ 差异（如实标）：① 原版按 `uniqueId` 在 `FullHeroCollection` 里找督军 SO，我们只判
        /// <paramref name="warlordId"/> 非空（本地没有「英雄总表」）；② 原版 warlordId 是 **string**，
        /// `DraftPacks.ClosedDeck.warlordId` 是 **int**（那一格的类型差异已在 DraftPacks 文件头记录）
        /// ⇒ 这里填 **0** 并保留原版的空/缺判据，⛔ 不猜一个阵营号。</para></summary>
        public static DraftPacks.ClosedDeck GetClosedDeckByPackIds(string warlordId, IList<string> packIds)
        {
            if (string.IsNullOrEmpty(warlordId))
            {
                Debug.LogError("[Prebuilt] GetClosedDeckByPackIds hero null（未给 warlordId）");
                return null;
            }
            if (packIds == null)
            {
                Debug.Log("[Prebuilt] GetClosedDeckByPackIds packIds null ⇒ 空牌组");
                return new DraftPacks.ClosedDeck
                {
                    warlordId = 0, cardIds = new string[0], packKeys = new string[0], packIds = new string[0],
                };
            }
            return DraftPacks.BuildClosedDeck(0, packIds);
        }
    }
}
