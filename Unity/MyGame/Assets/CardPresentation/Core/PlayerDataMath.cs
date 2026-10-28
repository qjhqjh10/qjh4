// PlayerDataMath.cs — 原版 `PlayerDataManager` 里那 6 个**纯函数**的等价物（B89-E10 建）。
//
//   账 `其他/基础设施`：351 `AreDecksEqual` · 374 `ExtractDeckInfo` · 390 `GetEnviromentString` ·
//                      401 `GetMD5Hash` · 413 `GetXPFromMatch` · 421 `IsBattleLogCreated`
//
// ============================ 为什么单独一个文件 ============================
//  · 这 6 条**全是纯函数**（不读 `this` 的任何字段、无共享状态）⇒ 一起放最省，
//    也避免造一个 2000 行的 `PlayerDataManager` 上帝类（施工单 `B88_R10` §2.4 的建议落点）。
//  · 「状态那一半」（`practiceMatchesList` / `localStartTime` / `faction0Points`）另放
//    `Shell/PlayerProfileState.cs`（同一批建的，那一半**只放状态**）。
//
// ============================ 判据（逐条现读，别重查） ============================
//  🥇 `d:/2/tools/decomp_full/PlayerDataManager__*.c`（Ghidra 伪码 = 真方法体）
//  🥈 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs`（ILSpy，只用来交叉结构）
//  🥉 枚举 / 字段声明 = `d:/2/tools/il2cpp_out/dump.cs`
//
// ============================ 两条「照抄原版 / 刻意不同」 ============================
//  ① `GetXPFromMatch(Campaign(130), 赢)` **返 130**（= `MatchType` 值本身）—— 原版 bug
//     （`__GetXPFromMatch.c:36` 机器码 `return 0x82`）⇒ **照抄，⛔ 别修**。
//  ② `ExtractDeckInfo` 原版在 `deck == null` / `deck.deckHero == null` 时 **抛 NRE**
//     （`.c` 末尾 `FUN_1803f47a0()` = 抛异常帮助函数）；我们**不抛**、返回 `null` + 出声
//     —— 同 `SerializedPlayerDeckData.From` 的先例（批处理里抛一下整条就没了）。
//     **这是本文件唯一一处刻意行为差异**。
//
// ⚠️ 本文件**没有任何 Unity 状态**，也**不建 GameObject** —— 可以在批处理里随便调。

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `PlayerDataManager` 那 6 个纯函数的等价物（账 `其他/基础设施` :351/:374/:390/:401/:413/:421）。
    /// <para>全静态、零状态；每个方法头上写着「判据出处 + 我们与原版的差异」。⛔ 别把状态塞进来
    /// （状态在 <c>Shell/PlayerProfileState.cs</c>）。</para></summary>
    public static class PlayerDataMath
    {
        // ======================================================== 401 GetMD5Hash

        /// <summary>原版 `PlayerDataManager.GetMD5Hash(string TextToHash)`（账 401）。
        ///
        /// <para>判据 = `decomp_full/PlayerDataManager__GetMD5Hash.c:16,30`：
        /// <c>if (param_1 != 0 &amp;&amp; *(int*)(param_1+0x10) != 0) { … } return String.Empty;</c>
        /// ⇒ 🔴 **空串 / `null` 一律返 `string.Empty`，不是 `null`**
        /// （`*(DAT_184557100+0xb8)` = `String.Empty`；施工单 `B88_R10` §2.1 已把账上那句「返 null」订正掉）。
        /// 内部 = <c>Encoding.Default.GetBytes</c> → MD5 → <c>BitConverter.ToString</c> → <c>Replace("-","")</c>
        /// ⇒ **大写十六进制、无分隔符**。</para>
        ///
        /// <para>⚠️ 两处实现细节（语义逐字节相同，写在这里免得下次重查）：
        /// ① 编码用 **`Encoding.Default`**（照抄原版，不是 UTF-8 显式写死）—— 在 Unity 的运行时里
        ///    `Default` 就是 UTF-8；ASCII 输入下两种写法结果一致，非 ASCII 才分家，而原版用的就是它。
        /// ② 原版实例化的是 `MD5CryptoServiceProvider`（遗留 CSP 类）；我们用 `MD5.Create()` ——
        ///    同一个算法的工厂方法，输出逐字节相同，且不依赖那个类在 IL2CPP 各后端上的可用性。</para>
        ///
        /// <para>⚠️ 原版这个方法在**整棵树里 0 调用点**（两套反编译器都只有定义处）。</para></summary>
        public static string GetMD5Hash(string textToHash)
        {
            if (string.IsNullOrEmpty(textToHash)) return string.Empty;
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(textToHash));
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        // ======================================================== 351 AreDecksEqual

        /// <summary>原版 `PlayerDataManager.AreDecksEqual(List&lt;string&gt; cardLibraryIds, List&lt;RawCardScript&gt; cards)`（账 351）。
        /// 我们的 `<c>RawCardScript</c>` 等价物 = <see cref="RuleEngine.CardDef"/>（唯一带 `Id` 的那份卡定义）。
        ///
        /// <para>判据 = `decomp_full/PlayerDataManager__AreDecksEqual.c` 全文：
        /// <c>rest = new List&lt;string&gt;(cardLibraryIds); foreach (c in cards) rest.Remove(c.uniqueId); return rest.Count == 0;</c>
        /// （`+0x38` = `RawCardScript.uniqueId`；`List.Remove` 每次只摘**第一个**命中）。</para>
        ///
        /// <para>🔴 **它是不对称的**（这条要专门钉住，别看走眼）：
        /// 只判「`cards` 里每张卡都能在 `cardLibraryIds` 里销掉一张」，
        /// **不判两边张数相等** ⇒
        /// <c>AreDecksEqual({"a"}, {a, a})</c> = **true**（多一张），
        /// <c>AreDecksEqual({"a", "a"}, {a})</c> = **false**（少一张）。</para>
        ///
        /// <para>⚠️ `cardLibraryIds == null` / `cards == null`：原版分别抛 `ArgumentNullException`
        /// （`List` 构造）/ NRE；我们**不抛** ⇒ 出声 + 返回 `false`（同 <see cref="ExtractDeckInfo"/>
        /// 那一处口径）。`cards` 里的**元素为 null** 时原版抛 NRE，我们把它当「id = null」参与销账。</para></summary>
        public static bool AreDecksEqual(IList<string> cardLibraryIds, IList<RuleEngine.CardDef> cards)
        {
            if (cards == null)
            {
                Debug.LogWarning("[PlayerDataMath] `AreDecksEqual` 拿到 null 的 cards —— 原版会抛 NRE，"
                               + "我们出声并返回 false（⛔ 不是「相等」）。");
                return false;
            }
            return AreDecksEqual(cardLibraryIds, cards.Count, i => cards[i] == null ? null : cards[i].Id);
        }

        /// <summary>上面那个重载的同一份逻辑，但「第 i 张卡 → id」由一个选择器给
        /// （自检夹具不必造 <see cref="RuleEngine.CardDef"/>）。
        /// <para>⚠️ 这是**同一条规则的第二个入口**（不是第二份实现 —— 上面那个直接转调这里）。</para></summary>
        public static bool AreDecksEqual(IList<string> cardLibraryIds, int cardCount, Func<int, string> idOf)
        {
            if (cardLibraryIds == null)
            {
                Debug.LogWarning("[PlayerDataMath] `AreDecksEqual` 拿到 null 的 cardLibraryIds —— 原版会抛 ANE，"
                               + "我们出声并返回 false。");
                return false;
            }
            if (idOf == null)
            {
                Debug.LogWarning("[PlayerDataMath] `AreDecksEqual` 拿到 null 的 idOf（选择器）—— 返回 false。");
                return false;
            }
            var rest = new List<string>(cardLibraryIds);
            for (int i = 0; i < cardCount; i++) rest.Remove(idOf(i));   // 每次只摘第一个命中（照原版 List.Remove）
            return rest.Count == 0;
        }

        // ======================================================== 413 GetXPFromMatch（P0）

        /// <summary>原版 `PlayerDataManager.GetXPFromMatch(MatchType playMode, float matchWon)`（账 413 ·
        /// **P0：我们的赛后 XP 一分不发，就是缺这一条**）。
        ///
        /// <para>判据 = `decomp_full/PlayerDataManager__GetXPFromMatch.c` 全文（逐分支）+ ILSpy `:9765-9848` 交叉。
        /// `DAT_1834b2bb8` 实读 = **`1.0f`**（`GameAssembly.dll` 偏移 `0x34b07b8`）⇒ 赢的判据是
        /// **`matchWon == 1.0f` 精确相等**，不是 `&gt;= 0.5`。</para>
        ///
        /// <para>分支表（`.c` 逐句；⚠️ **施工单 §2.3 在 `PracticeOffline(50)` 那一格上写错了**，见下）：
        /// <code>
        /// Ranked(10) · RankedBot(20) · Unranked(170) · UnrankedBot(180) → 赢 100 / 输 30
        /// Campaign(130)                                              → 赢 130 / 输 0   （原版 bug，照抄）
        /// EventAI(80) · Tutorial(100) · ClosedDeck(110) · ClosedDeckBot(120) · TutorialReplay(140) → 0
        /// 其余全部（含 PracticeOffline(50) / Practice(90) / Duel(150) / Replay(160) /
        ///           Scene(190) / FastMode(200) / FastModeBot(210) / FastModeRanked(211) / Undefined(0)）
        ///                                                            → LogWarning + 0
        /// </code></para>
        ///
        /// <para>🔴 **就地订正（铁律 5 · 施工单 `B88_R10` §2.3 的第 5 行）**：那一行把
        /// `PracticeOffline(50)` 列进「直接 `return 0`」那一组 —— **`.c` 里没有 50**。
        /// `.c:22-27` 那三个 `if` 是 `0x50`(=**80**) / `100` / `0x6e`(=110)，而 `50 = 0x32`
        /// 落在 `21..110` 这一段、三个都不等 ⇒ **`goto LAB_18075dab2`（default）**。
        /// ILSpy `:9812-9820` 完全同形（`EventAI` / `Tutorial` / `ClosedDeck` 三个显式档，
        /// 其余 `goto IL_01b3` = default）。⇒ **返回值一样是 0，差别只在「会不会打一条告警」**；
        /// 本实现按 `.c` 走 default（**会告警**）。</para>
        ///
        /// <para>⚠️ `Practice(90)` 也走 `default`（告警 + 0）—— **⛔ 别顺手给它 XP**（账上那句就是这么写的）。</para></summary>
        public static int GetXPFromMatch(RuleEngine.MatchType playMode, float matchWon)
        {
            switch (playMode)
            {
                case RuleEngine.MatchType.Ranked:
                case RuleEngine.MatchType.RankedBot:
                case RuleEngine.MatchType.Unranked:
                case RuleEngine.MatchType.UnrankedBot:
                    return matchWon == 1.0f ? 100 : 30;

                case RuleEngine.MatchType.Campaign:
                    // 🔴 原版 bug 照抄：赢的那一档返回的是 **MatchType 值本身（130）**，不是某个 XP 配置。
                    //    判据 = `__GetXPFromMatch.c:36` 的机器码 `return 0x82` + ILSpy `:9795` `result = 130`。
                    //    ⛔ **别修**（修了就与玩家在原版里看到的经验值不一致）。
                    return matchWon == 1.0f ? 130 : 0;

                case RuleEngine.MatchType.EventAI:
                case RuleEngine.MatchType.Tutorial:
                case RuleEngine.MatchType.ClosedDeck:
                case RuleEngine.MatchType.ClosedDeckBot:
                case RuleEngine.MatchType.TutorialReplay:
                    return 0;

                default:
                    // 原文 = `"GetXPFromMatch undefined for playMode " + ((MatchType)playMode).ToString()`
                    //（`DAT_184238a30` = 前缀；ILSpy `:9827` 逐字）。
                    Debug.LogWarning("GetXPFromMatch undefined for playMode " + playMode);
                    return 0;
            }
        }

        // ======================================================== 390 GetEnviromentString

        /// <summary>原版 `PlayerDataManager.GetEnviromentString()`（账 390）= **常量 `"LIVE"`**。
        ///
        /// <para>判据 = `decomp_full/PlayerDataManager__GetEnviromentString.c` 全文（只有 `return DAT_1842a2a30;`）
        /// ＋ `il2cpp_out/stringliteral.json` 实解：`{'value': 'LIVE', 'address': '0x42A2A30'}`
        /// （= `0x1842A2A30 − ImageBase`）。本 build 没有 dev/prod 之分。</para>
        ///
        /// <para>⚠️ 原文 **0 调用点**（两棵树都只有定义处）⇒ 无玩家可见影响；留着是为了
        /// 把账上那一条落地（铁律 11：不许因为「没人用」就判不做）。</para></summary>
        public static string GetEnviromentString()
        {
            return "LIVE";
        }

        // ======================================================== 421 IsBattleLogCreated

        /// <summary>原版 `PlayerDataManager.IsBattleLogCreated(List&lt;BattleLogMatch&gt; battleLogData, MatchType matchType)`（账 421）。
        ///
        /// <para>判据 = `decomp_full/PlayerDataManager__IsBattleLogCreated.c` 全文：
        /// 先判 `matchType` 是否落在**位掩码白名单**里（不在 ⇒ `false`），
        /// 再判 `list != null &amp;&amp; list.Count &gt; 0`。</para>
        ///
        /// <para>白名单是**三段位掩码 + 一个特例**，逐位解出（`0x4000000100400` 等三串常量**原样照抄**）：
        /// <code>
        /// Mask1 = 0x4000000100400          (p ≤ 0x32)      → 位 10/20/50      → MatchType 10, 20, 50
        /// Mask2 = 0x1004000040100001       (p−0x96 ≤ 0x3c) → 位 0/20/30/50/60 → MatchType 150, 170, 180, 200, 210
        /// Mask3 = 0x40100001               (p−0x5a ≤ 0x1e) → 位 0/20/30      → MatchType  90, 110, 120
        /// 特例  : p == 0xd3                → MatchType 211
        /// </code>
        /// ⇒ **白名单 12 档** = {10, 20, 50, 90, 110, 120, 150, 170, 180, 200, 210, 211}
        /// （`Ranked` / `RankedBot` / `PracticeOffline` / `Practice` / `ClosedDeck` / `ClosedDeckBot` /
        /// `Duel` / `Unranked` / `UnrankedBot` / `FastMode` / `FastModeBot` / `FastModeRanked`）。</para>
        ///
        /// <para>🔴 **就地订正两处旧说法（铁律 5）**：
        /// ① 施工单 `B88_R10` §4.2 那张表白名单写了「⚠️ **`110 ClosedDeck` 不在白名单**（容易看错）」
        ///    —— **反了**：`0x40100001` 的**位 20** 就是它（`0x100000`），`90 + 20 = 110`，**在**白名单里。
        ///    真正**不在**的是 `EventAI(80)` / `Tutorial(100)` / `Campaign(130)` / `TutorialReplay(140)`
        ///    / `Replay(160)` / `Scene(190)` 这几档。
        /// ② 账行 421 的备注写「50/58/100/…/150/160/190/200 等」—— 那是**猜的**，
        ///    上表才是三串常量逐位解出来的。</para>
        ///
        /// <para>⚠️ 本方法**不看列表里每条记录的 `matchType`**（原版也不看 —— 它只数条数）。</para></summary>
        public static bool IsBattleLogCreated<T>(IReadOnlyCollection<T> battleLogData, RuleEngine.MatchType matchType)
        {
            // ↓ 下面四行是 `.c` 那一条复合条件**逐字搬**下来的（⛔ 别「简化」成集合查找：
            //   位移与掩码的形状就是原版判据本身，改形状会让「哪些档在白名单里」变成我们的解读）。
            const ulong Mask1 = 0x4000000100400UL;      // 位 10 / 20 / 50
            const ulong Mask2 = 0x1004000040100001UL;   // 位 0 / 20 / 30 / 50 / 60（相对 +150）
            const uint Mask3 = 0x40100001u;             // 位 0 / 20 / 30      （相对 +90）

            // ⚠️ `.c` 里那两段用的是「无符号下溢 ⇒ 巨值 ⇒ 大于阈值 ⇒ 跳过」的写法
            //    （`0x3c < param_3 - 0x96`）。C# 在 `checked` 上下文里减会抛 ⇒ 这里把
            //    「下溢那一路」显式写成 `< 下限` 的前置判断，**语义逐值相同**（都不看那一段掩码）。
            uint p = unchecked((uint)(int)matchType);
            bool miss1 = (p > 0x32u) || (((Mask1 >> (int)(p & 0x3fu)) & 1UL) == 0UL);
            bool miss2 = (p < 0x96u) || (p - 0x96u > 0x3cu)
                                    || (((Mask2 >> (int)((p - 0x96u) & 0x3fu)) & 1UL) == 0UL);
            bool miss3 = (p < 0x5au) || (p - 0x5au > 0x1eu)
                                    || (((Mask3 >> (int)((p - 0x5au) & 0x1fu)) & 1UL) == 0UL);
            if (miss1 && miss2 && miss3 && (p != 0xd3u)) return false;

            if (battleLogData == null) return false;
            return battleLogData.Count > 0;
        }

        // ======================================================== 374 ExtractDeckInfo

        /// <summary>原版 `CustomDeckData`（`dump.cs:79098-79109` · `struct`）。字段名 / 次序**逐字照抄**。
        /// <para>⚠️ 字段序与 ctor 形参序**不同**（原版就是这样）：ctor 第 1 个形参叫 `deckId`，
        /// 落到第 2 个字段 `uniqueId` 上 —— 照抄，别「理顺」。</para></summary>
        [Serializable]
        public struct CustomDeckData
        {
            public string deckName;        // 原版 +0x0
            public string uniqueId;        // 原版 +0x8（= ctor 的第 1 个形参 `deckId`）
            public string heroID;          // 原版 +0x10
            public string cardbackID;      // 原版 +0x18
            public string warcryID;        // 原版 +0x20
            public string[] cardID;        // 原版 +0x28
            public DateTime lastEditTime;  // 原版 +0x30

            /// <summary>形参序 = 原版 `CustomDeckData..ctor(string deckId, string deckName, string heroID,
            /// string[] cardID, DateTime lastEditTime, string cardback, string warcry)`（`dump.cs:79109`）。</summary>
            public CustomDeckData(string deckId, string deckName, string heroID, string[] cardID,
                                  DateTime lastEditTime, string cardback, string warcry)
            {
                this.deckName = deckName;
                this.uniqueId = deckId;
                this.heroID = heroID;
                this.cardbackID = cardback;
                this.warcryID = warcry;
                this.cardID = cardID;
                this.lastEditTime = lastEditTime;
            }
        }

        /// <summary>原版 `PlayerDataManager.ExtractDeckInfo(CardDeck deck)`（账 374）：把一副卡组摊成
        /// <see cref="CustomDeckData"/>。
        ///
        /// <para>判据 = `decomp_full/PlayerDataManager__ExtractDeckInfo.c` 全文（**`.c` 是唯一权威** ——
        /// ILSpy `:10485-10502` 把 `cardID` / `lastEditTime` / `cardback` / `warcry` 四个实参
        /// **全折叠成 `default`**，账上那句「cardback/warcry 被 IL 折叠成 null」就是被它带偏的；
        /// `.c` 里四个实参分别是 `uVar8`（卡 id 数组）/ `param_2+0x68` / `param_2+0x28` / `param_2+0x30`）：
        /// <code>
        /// new CustomDeckData(
        ///   deckId       = deck+0x18,                    // CardDeck.deckId
        ///   deckName     = deck+0x10,                    // CardDeck.deckName
        ///   heroID       = deck.deckHero(+0x40).uniqueId(+0x38),
        ///   cardID[]     = deck.cardLibrary(+0x58).Select(x => x.uniqueId(+0x38)).ToArray(),
        ///   lastEditTime = deck+0x68,                    // CardDeck.lastEditTime (DateTime)
        ///   cardback     = deck+0x28,                    // CardDeck.cardbackId
        ///   warcry       = deck+0x30)                    // CardDeck.warcryId
        /// </code></para>
        ///
        /// <para>⚠️ 原版在 `deck == null` / `deck.deckHero == null` 时**抛 NRE**；我们**返回 `null` + 出声**
        /// （见文件头「刻意不同」②）。</para>
        ///
        /// <para>🔶 自己设计（非原版）：**这三格填 `null` / `default` / `null`** —— ⚠️ 2026-10-28 `B89_RXb`
        /// 把口径收窄（原来是「三格都没有数据源」）：原版三格都是 `CardDeck` 的**序列化字段**
        /// （`deckId(+0x18)` / `lastEditTime(+0x68)` / `warcryId(+0x30)`），其中**只有 `deckId` 真来自服务器**；
        /// `lastEditTime` 是客户端本地时间戳、`warcry` 是可选装饰（是否玩家可选**还没查清**，见报告 §5）⇒
        /// **后两格是我们 `PlayerDeck` 少了列**（待办：补列后就不再填空）。
        /// 我们的 `PlayerDeck` 只有 `Name` / `WarlordId` / `DefensiveId` / `CardIds` / `GameMode` / `CardbackId`
        /// （见 `RuleEngine/Core/DeckRules.cs:717`）· 调度台 2026-10-28（B89-E10）裁定｜⛔ 别拿卡组名/时间戳编一个填进去。</para>
        ///
        /// <para>⚠️ 原版这个方法在**整棵树里 0 调用点**（两套反编译器都只有定义处）。</para></summary>
        public static CustomDeckData? ExtractDeckInfo(RuleEngine.PlayerDeck deck)
        {
            if (deck == null)
            {
                Debug.LogWarning("[PlayerDataMath] `ExtractDeckInfo` 拿到 null 卡组 —— 原版会抛 NRE，"
                               + "我们返回 null（⛔ 不造空壳）。");
                return null;
            }
            if (string.IsNullOrEmpty(deck.WarlordId))
            {
                Debug.LogWarning("[PlayerDataMath] `ExtractDeckInfo`：卡组 `" + (deck.Name ?? "<无名>")
                               + "` 没有战将 —— 原版会抛 NRE（`deck.deckHero` 为 null），我们返回 null。");
                return null;
            }

            string[] cardIds;
            if (deck.CardIds == null) cardIds = new string[0];
            else
            {
                cardIds = new string[deck.CardIds.Count];
                for (int i = 0; i < deck.CardIds.Count; i++) cardIds[i] = deck.CardIds[i];
            }

            WarnExtractDeckMissingColumns();

            // 🔶 自己设计（非原版）：第 1 / 5 / 7 个实参（deckId / lastEditTime / warcry）在 `PlayerDeck`
            //   上没有对应列 ⇒ 填 null / default / null（理由与裁定见上面那段 doc）。
            return new CustomDeckData(
                deckId: null,
                deckName: deck.Name,
                heroID: deck.WarlordId,
                cardID: cardIds,
                lastEditTime: default(DateTime),
                cardback: deck.CardbackId,
                warcry: null);
        }

        static bool _warnedExtractDeck;

        /// <summary>「`ExtractDeckInfo` 有三格在我们这边没有数据源」——一进程只说一次（⛔ 不静默）。</summary>
        static void WarnExtractDeckMissingColumns()
        {
            if (_warnedExtractDeck) return;
            _warnedExtractDeck = true;
            Debug.Log("[PlayerDataMath] `ExtractDeckInfo`：`deckId` / `lastEditTime` / `warcry` 三格"
                    + "**我们的 `PlayerDeck` 没有这些列**（原版取 `CardDeck+0x18/+0x68/+0x30`，"
                    + "离线也没有来源）⇒ 分别填 null / default / null。**不是漏填**（调度台 2026-10-28 裁定）。");
        }

        // ============================================================ 507 `ShouldShowRatePopup`（B89-E10b 追加在类尾）
        // 判据 = `decomp_full/PlayerDataManager__ShouldShowRatePopup.c`（全文现读 2026-10-28）：
        //   `lVar1 = *(this + 0x248)`（= `PlayerDataManager.tipsData`，服务器 key 14 `TipsData`）
        //   ① 它 == 0 ⇒ `FUN_1803f47a0()`（抛 NRE）
        //   ② `if (TipsData.get_campaignCompleted()) return false;`      ← `TipsData+0x1C`
        //   ③ `return *(int*)(lVar1 + 0x18) == 1;`                       ← `TipsData.campaignStageIndex`
        // ⇒ 语义只有两句：**战役通关过 ⇒ 不弹**；否则**只在第 1 关弹**。
        //
        // ⚠️ 数据源（`TipsData`）在**服务器只读用户数据 key 14** ⇒ 本仓没有实例（施工单 §2.2 #16 已记）。
        //   所以这里做成「**给两个值就判**」的纯函数，⛔ 不塞一个假的 TipsData 进来。

        /// <summary>原版 `PlayerDataManager.ShouldShowRatePopup()`（账 507）的纯判定形态。
        /// <para>判据 = `PlayerDataManager__ShouldShowRatePopup.c` 三句（见上面那段）。</para>
        /// <para>⚠️ 与原版的唯一差异：原版 `tipsData == null` 时**抛 NRE**（数据没解出来就是崩），
        /// 我们**没有那个数据源**，由调用方把两个值取来 —— 取不到就**别调这个方法**（⛔ 别拿 `false/0` 冒充「没通战役、第 0 关」）。</para></summary>
        /// <param name="campaignCompleted">= 原版 `TipsData.campaignCompleted`（`TipsData+0x1C`）。</param>
        /// <param name="campaignStageIndex">= 原版 `TipsData.campaignStageIndex`（`TipsData+0x18`）。</param>
        public static bool ShouldShowRatePopup(bool campaignCompleted, int campaignStageIndex)
        {
            if (campaignCompleted) return false;       // ② 通关过 ⇒ 不弹
            return campaignStageIndex == 1;            // ③ 否则只在第 1 关弹
        }

        // ============================================================ 423 `IsTherePendingMatch`（B89-E10b 追加在类尾）
        // 判据 = `decomp_full/PlayerDataManager__IsTherePendingMatch.c`（全文 3 行）→ 转调
        //   `decomp_full/OnlineData__IsTherePendingMatch.c`（全文现读）：
        //   ① `key = ((PlayerDataKey)0x12).ToString()` ⇒ **`"LastOpponentInfo"`**
        //      （`dump.cs:78412` 的 `enum PlayerDataKey`：`LastOpponentInfo = 18`；非 flag 枚举 `ToString()` = 成员名）
        //   ② `if (!UserReadOnlyData.ContainsKey(key)) return false;`
        //   ③ `return !string.IsNullOrEmpty(UserReadOnlyData[key].Value);`
        //   ⚠️ `OnlineData` 为 null ⇒ 原版抛 NRE（`param_1 == 0` → `FUN_1803f47a0`）。
        //
        // ⚠️ 数据源 = **PlayFab 只读用户数据**（P3）⇒ 本仓没有 `userInfo`，所以做成「给一张只读字典就判」的纯函数。

        /// <summary>原版 `PlayerDataManager.IsTherePendingMatch()`（账 423）的纯判定形态。
        /// <para>= `OnlineData.IsTherePendingMatch(userInfo)`：查只读用户数据里的 **`"LastOpponentInfo"`** 这一格，
        /// **键不在 ⇒ false**；**在但值是空串 ⇒ false**；有非空值 ⇒ **true**。</para>
        /// <para>⚠️ 与 <c>PlayerDataKey</c> 的耦合只在这里：键名是**枚举值的 `ToString()`**（`18` ⇒ `"LastOpponentInfo"`）
        /// ⇒ 将来若有人重排那个枚举，这里要跟着改（原版同样是靠枚举名，不是字面量常量）。</para></summary>
        /// <param name="userReadOnlyData">原版 `OnlineData.UserReadOnlyData`（键 = `PlayerDataKey.ToString()`，值 = 那一格）。</param>
        public static bool IsTherePendingMatch(IReadOnlyDictionary<string, string> userReadOnlyData)
        {
            if (userReadOnlyData == null)
            {
                Debug.LogWarning("[PlayerDataMath] `IsTherePendingMatch` 拿到 null 的只读用户数据 —— 原版会抛 NRE，"
                               + "我们出声并返回 false（⛔ 不是「有待打的局」）。");
                return false;
            }
            string key = PendingMatchKey;
            string value;
            if (!userReadOnlyData.TryGetValue(key, out value)) return false;
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>`((PlayerDataKey)18).ToString()` 的结果 —— **只此一处**（见上面判据 ①）。</summary>
        public const string PendingMatchKey = "LastOpponentInfo";
    }
}
