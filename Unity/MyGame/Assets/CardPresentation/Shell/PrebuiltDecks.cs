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

            /// <summary>显示名：**有中文用中文，没有才回落英文原名**（与卡面同一条口径，
            /// 判据在 `Core/CardText.cs`；工程里没有语言开关，原版也没有）。</summary>
            public string DisplayName { get { return string.IsNullOrEmpty(nameZh) ? name : nameZh; } }

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

        static Doc _doc;
        static bool _loaded;
        static List<Deck> _tab;

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

        public static Deck ById(string deckId)
        {
            if (string.IsNullOrEmpty(deckId)) return null;
            foreach (var d in All) if (d != null && d.deckId == deckId) return d;
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
        public static void ResetForTest() { _doc = null; _loaded = false; _tab = null; _pending = null; _pendingSrc = null; }
    }
}
