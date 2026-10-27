// DeckSerializedData.cs — 账 `卡组:49` / `卡组:50` 的 **Unity 侧那一半**：
//   ① 把 `PlayerDeck` 装成原版 `SerializedPlayerDeckData`（DTO 在 `RuleEngine/Core/DeckRules.cs`）；
//   ② 原版 `DeckAndWarlordData.GetSerializedData()` 的等价出口 = **一个 JSON 串**。
//
// ============================ 判据（逐条） ============================
//  · `cpp2il/out_cpp2il_dev_ilspy/DeckAndWarlordData.cs:780-784`：
//      `GetSerializedData() { return JsonWrapper.SerializeObject(GetSerializedDeckData()); }`
//  · `decomp_full/DeckAndWarlordData__GetSerializedDeckData.c` 全文（11 组字段的偏移表 → DTO 的 `From`）
//  · 字段名/声明序 = `cpp2il/out_cpp2il_dev_ilspy/SerializedPlayerDeckData.cs:5-52`（逐字）
//
// ⚠️ **为什么分开两个文件**：`RuleEngine/Core/*.cs` 被离屏探针（`工具/ruleprobe`）**只编 Core 那一层**，
//    里面**不能出现 `UnityEngine`**（判据 → `RuleEngine/Core/RelatedCardData.cs:25` 那条纪律）。
//    而 `JsonUtility` 与 `CardProgress`（Unity 侧）都在这一边 ⇒ DTO 在 Core、装配与 JSON 在这里。
//
// 🔴 **边界（P3）**：原版这条串是**发给服务器 / 对手的那份卡组数据**（`PlayerBattleData` 那条链）。
//    本件**只产出对象与串**，⛔ **不发送、不落盘**（`卡组:49` 的改动请求就是这么写的）。
//    只有服务器才有的那几格（`eloRating` / `alliance*` / `warcry` / `currentDeckId` / `draftVictories`）
//    离线恒空 ⇒ 留默认值并**出声一次**（⛔ 不静默造数）。

using System;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>原版 `DeckAndWarlordData.GetSerializedData()` 的等价物（**客户端侧，不接发送**）。</summary>
    public static class DeckSerializedData
    {
        /// <summary>把我们的 `PlayerDeck` 装成原版 DTO。`deck == null` ⇒ `null`（⛔ 不造空壳）。</summary>
        /// <param name="warn">`true`（默认）= 那些**离线没有数据源**的格子出声一次。
        /// 自检/探针里成批调用时传 `false`（否则刷屏），并在报告里写明。</param>
        public static SerializedPlayerDeckData Build(PlayerDeck deck, bool warn = true)
        {
            return Build(deck, null, warn);
        }

        /// <summary>同上，但可以带一份「服务器那半」的模板（`extras`，见
        /// <see cref="SerializedPlayerDeckData.From"/> 的形参说明）。今天只有离线档，所以 `extras` 一律 `null`。</summary>
        public static SerializedPlayerDeckData Build(PlayerDeck deck, SerializedPlayerDeckData extras, bool warn = true)
        {
            if (deck == null) return null;
            var dto = SerializedPlayerDeckData.From(deck, TierOf, extras);
            if (warn) WarnMissingServerFields();
            return dto;
        }

        /// <summary>原版 `GetSerializedData()`：DTO → **JSON 串**。
        /// ⚠️ 序列化器**不是同一个**：原版 `JsonWrapper` 走的那一份我们拿不到（它在 PlayFab 侧的 dll 里，
        /// 本地没有反编译产物）⇒ 我们用 `JsonUtility.ToJson`（**字段名/嵌套/数组形状与原版一致**）；
        /// 已知差别只有「`null` 怎么写」（`JsonUtility` 写 `""`/`null` 与原版 Newtonsoft 不一定同形）。
        /// 本串今天**不发给任何人**（P3）⇒ 记录在案，⛔ 别拿它当「原版报文逐字节等价」的证据。</summary>
        public static string ToJson(PlayerDeck deck, bool warn = true)
        {
            var dto = Build(deck, warn);
            return dto == null ? null : JsonUtility.ToJson(dto, false);
        }

        /// <summary>卡 id → **0 基** `CardTier`。原版这一格 = `RawCardScript.get_CardTier()`（0 基，
        /// Tier1 = 0 … Tier4 = 3）；我们的 `CardProgress.Level` 是 **1 基** ⇒ 减一。
        /// ⚠️ 与 `BattleDriver.AddTierOf`（`CardPresentation/Battle/BattleDriver.cs` 里那一句
        /// `Mathf.Clamp(CardProgress.Level(cardId) - 1, 0, CardArt.MaxTier - 1)`）**是同一条规则的第二份** ——
        /// 那一份是私有方法、跨不过去（铁律：两处写同一条规则迟早不一致）⇒ 已记进报告建议收口到一处。
        /// 本件按 `CardProgress.MaxLevel` 夹（= 它的 `Level()` 自己就夹在 1..MaxLevel ⇒ 结果逐位相同）。</summary>
        static int TierOf(string cardId)
        {
            return Mathf.Clamp(CardProgress.Level(cardId) - 1, 0, CardProgress.MaxLevel - 1);
        }

        static bool _warned;

        /// <summary>服务器那半格出声（一进程一次）。</summary>
        static void WarnMissingServerFields()
        {
            if (_warned) return;
            _warned = true;
            Debug.Log("[Deck] `SerializedPlayerDeckData`（`卡组:49/50`）：`eloRating` / `allianceName` / "
                    + "`allianceRating` / `allianceBadgeId` / `allianceBadgeLevel` / `warcry` / `currentDeckId` / "
                    + "`draftVictories` 这 8 格**离线没有数据源**（原版来自服务器 `PlayerBattleData`，P3 边界）"
                    + " ⇒ 留默认值，**不是**漏填。⚠️ `playerAvatar` / `playerAvatarBorder` / `titleId` 我们也**没塞**："
                    + "本地档案（`ProfileData`）给的是**美术名**，而这三个格要的是**服务器目录 id**（出厂值就是两个 hash）"
                    + " ⇒ 塞进去是**错形的数据**（比空更糟）。要真填得等 P3。");
        }
    }
}
