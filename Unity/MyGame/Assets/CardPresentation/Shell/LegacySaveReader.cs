// LegacySaveReader.cs — 旧档 → 中央存档的**导入式迁移**（完全复刻 · Step 2）
//
// ⛔ **两条红线（方案 §七 Step 2 / §九·2）**
//   ① **旧文件一个都不删、不改名、不移动** —— 用户要回滚时旧客户端还能读它们。本文件**没有任何**
//      `File.Delete` / `File.Move` / `File.WriteAllText`（只读旧档，写只经 `SaveCenter`）。
//   ② **不覆盖已有的中央存档** —— 每份旧档导入前先 `SaveCenter.Exists(key)`，有就跳过。
//      ⇒ **幂等**：同一批旧档跑两次，第二次一行都不写（报告里全是「跳过」）。
//      ⇒ 也满足「先跑过新客户端、再回旧客户端、再回来」这种来回切换。
//
// 为什么不能「删档重来」：原版 `ClearLocalPlayerData()`（`PlayerDataManager.cs:10157-10172`）确实是把
// 本地档 `File.Delete` 掉，但**原版有云端权威副本**；我们**没有** ⇒ 必须导入式迁移（方案 §九·2）。
//
// 映射表（判据 = 各旧档的落盘常量，逐条指到文件:行号；⛔ 原版没有的 key 一律 `_ours_` 前缀）
//   ┌ 旧档（persistentDataPath 下）              ┐ → key                          · 方式
//   ├ WarpforgeDaily/daily.json                  ┐ → DailyCounters                · 整份
//   ├ WarpforgeDraft/run.json                    ┐ → ActiveEvent                 · 整份
//   ├ WarpforgeBattleLog/battlelog.json          ┐ → BattleLogData               · 整份
//   ├ WarpforgeDecks.json                        ┐ → PlayerDecks                 · 整份
//   ├ WarpforgeRanked/ranked.json                ┐ → PlayerSeason                · 整份
//   ├ WarpforgeCardLevels.json                   ┐ → _ours_CardProgress          · 整份
//   ├ WarpforgeInventory/<name>.json             ┐ → _ours_Inventory.<name>      · 整份（逐文件）
//   └ WarpforgeProfile/profile.json              ┐ → CosmeticData                · **只取 avatarIndex/titleIndex**
//   ⛔ 不导入（原因写在报告里）：`PlayerPrefs`（语言/音量，属**设置层**）· `WarpforgeNet.json`（联机设置，同设置层）·
//      `WarpforgeReplays/`（录像大文件；**引用**已经在 `BattleLogData` 的 `ReplayFile` 字段里）
//
// ⚠️ **`ProfileData` 只取两个字段**（简报明确要求）：`profile.json` 里还有 `playerName` / `avatarBorderOn` /
//    `achIds` / `achValues` —— 它们**也是玩家状态**（`Shell/ProfileData.cs` 的 `SaveDto`），但本轮**不迁**
//    （成就应归 `AchievementsData`、玩家名归 `NameChanges` 那条线，见报告 §五「未覆盖」）。
//
// ⚠️ 本文件**不接入任何现有子系统**：谁在什么时候调 `ImportAll()` 是 Step 4 的事（建议首次启动 + 双写期）。

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>把现有 N 份本地旧档**读进来**、转成中央存档格式（`SaveCenter`）。⛔ 旧文件不动。</summary>
    public static class LegacySaveReader
    {
        // ── 旧档的相对路径 / 文件名（**只此一处**；都取自各旧档自己的落盘常量，见每条注释）──────────
        // ⚠️ 这里抄的是**字符串**而不是引用那些常量：本文件要能独立编译（不依赖 Shell 里任何一个窗口类），
        //    出处写在每条后面 —— 改旧档落点时两边都要改。

        /// <summary>`Shell/DailyData.cs:1697,1699`（`StoreDirName` + `StoreFileName`）。</summary>
        public const string DailyRel = "WarpforgeDaily/daily.json";
        /// <summary>`Shell/DraftModeWindow.cs:727,729`。</summary>
        public const string DraftRel = "WarpforgeDraft/run.json";
        /// <summary>`Shell/BattleLogData.cs:182,184`。</summary>
        public const string BattleLogRel = "WarpforgeBattleLog/battlelog.json";
        /// <summary>`RuleEngine/Data/DeckStore.cs:22`（`FileName`，直接放在 `persistentDataPath` 根下）。</summary>
        public const string DecksRel = "WarpforgeDecks.json";
        /// <summary>`Shell/RankedData.cs:154`（目录 `WarpforgeRanked`）+ `:158`（`StorePath` ⇒ `ranked.json`）。</summary>
        public const string RankedRel = "WarpforgeRanked/ranked.json";
        /// <summary>`Shell/CardDetailPopup.cs:161`（`CardProgress.SaveFileName`，直接放在根下）。</summary>
        public const string CardLevelsRel = "WarpforgeCardLevels.json";
        /// <summary>`Shell/ProfileData.cs:982,983`（`SaveDirName` + `SaveFileName`）。</summary>
        public const string ProfileRel = "WarpforgeProfile/profile.json";

        /// <summary>`Shell/Inventory.cs:376`（`InventoryStore.StoreDirName`；每个背包一个 `<name>.json`）。</summary>
        public const string InventoryDirRel = "WarpforgeInventory";
        /// <summary>录像目录（`Battle/ReplayStore.cs`）。**不导入**：见类头。</summary>
        public const string ReplayDirRel = "WarpforgeReplays";
        /// <summary>联机设置（`Net/NetConfig.cs:3`）。**不导入**：属设置层。</summary>
        public const string NetConfigRel = "WarpforgeNet.json";

        /// <summary>自检用的**旧档根目录改写口**（`null` = `Application.persistentDataPath`）。</summary>
        public static string LegacyRootOverride;

        /// <summary>旧档根目录（玩家真目录，除非自检注入了别的）。</summary>
        public static string LegacyRoot
        {
            get
            {
                return string.IsNullOrEmpty(LegacyRootOverride)
                     ? Application.persistentDataPath
                     : LegacyRootOverride;
            }
        }

        /// <summary>上一次 `ImportAll()` 导入了几份（0 = 没导任何东西）。</summary>
        public static int LastImportedCount { get; private set; }
        /// <summary>上一次 `ImportAll()` 跳过了几份（没有旧档 / 中央存档已有）。</summary>
        public static int LastSkippedCount { get; private set; }
        /// <summary>上一次 `ImportAll()` 失败了几份（旧档坏了 / 写不进去）。</summary>
        public static int LastFailedCount { get; private set; }

        /// <summary>跑一遍导入，返回**给人看的报告**（同时整段 `Debug.Log` 一次）。
        /// <para>幂等：已经导过的 key 会被跳过；旧文件一个字节都不动。</para>
        /// <para>⚠️ 落盘关着（批处理 / 没设 `SaveCenter.OverrideRoot`）⇒ 直接返回说明，**不做任何事**。</para></summary>
        public static string ImportAll()
        {
            LastImportedCount = 0;
            LastSkippedCount = 0;
            LastFailedCount = 0;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[LegacySaveReader] 旧档 → 中央存档");
            sb.Append("  旧档根：`").Append(LegacyRoot).AppendLine("`");
            sb.Append("  新档根：`").Append(SaveCenter.RootDir).AppendLine("`");

            if (!SaveCenter.StoreOn)
            {
                sb.AppendLine("  ⏭ **什么都没做**：`SaveCenter.StoreOn == false`（批处理 / 不在 Play 且没设 `OverrideRoot`）"
                            + " ⇒ 一个字节都没碰。要真跑：先在真 Play 里，或设 `SaveCenter.OverrideRoot = <临时目录>`。");
                string off = sb.ToString();
                Debug.Log(off);
                return off;
            }

            string root = LegacyRoot;
            ImportWholeFile(sb, "日常", Path.Combine(root, DailyRel), SaveKeys.DailyCounters,
                "整份导入（`nextResetTicks/nextSet` 也在里面；`DailyLimits` 本轮不拆，见 `SaveKeys.cs` 那条注释）");
            ImportWholeFile(sb, "轮抽", Path.Combine(root, DraftRel), SaveKeys.ActiveEvent, "整份导入");
            ImportWholeFile(sb, "对局历史", Path.Combine(root, BattleLogRel), SaveKeys.BattleLogData, "整份导入");
            ImportWholeFile(sb, "卡组", Path.Combine(root, DecksRel), SaveKeys.PlayerDecks,
                "整份导入（`current` 也在里面；`CurrentDeck` 本轮不拆）");
            ImportWholeFile(sb, "段位", Path.Combine(root, RankedRel), SaveKeys.PlayerSeason,
                "整份导入（原版 `PlayerSeason` 语义对应；payload 是我们自己的形状）");
            ImportWholeFile(sb, "卡牌等级", Path.Combine(root, CardLevelsRel), SaveKeys.OursCardProgress,
                "整份导入（原版无对应 key ⇒ `_ours_`）");
            ImportProfileCosmetic(sb, Path.Combine(root, ProfileRel));
            ImportInventoryDir(sb, Path.Combine(root, InventoryDirRel));

            sb.AppendLine("  ⛔ 不导入（**故意**，原因见下）：");
            sb.AppendLine("     · `PlayerPrefs`（语言 / 音量，`Core/Loc.cs` / `Core/WarpforgeAudio.cs`）—— 原版把它们归「本地设置档」，"
                        + "**不进中央存档**（方案 §九·3）");
            sb.Append("     · `").Append(NetConfigRel).AppendLine("`（联机设置，`Net/NetConfig.cs`）—— 同属设置层");
            sb.Append("     · `").Append(ReplayDirRel).AppendLine("/`（录像文件）—— 体积大且不是「存档」；"
                        + "**引用**已经在 `BattleLogData` 的 `ReplayFile` 字段里，导了那一份就够了");

            sb.Append("  汇总：导入 ").Append(LastImportedCount)
              .Append(" · 跳过 ").Append(LastSkippedCount)
              .Append(" · 失败 ").Append(LastFailedCount).AppendLine();
            sb.Append("  ⚠️ 旧文件**一个都没动**（红线：用户回滚旧版时它们还得能读）");

            string report = sb.ToString();
            Debug.Log(report);
            return report;
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  内部：逐份导入
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>整份原样搬进中央存档（payload = 旧档的 JSON 原文）。</summary>
        static void ImportWholeFile(StringBuilder sb, string label, string legacyPath, string key, string how)
        {
            if (!File.Exists(legacyPath)) { Skip(sb, label, legacyPath, key, "没有这份旧档"); return; }
            if (SaveCenter.Exists(key)) { Skip(sb, label, legacyPath, key, "中央存档已有 `" + key + "`（不覆盖）"); return; }

            string text;
            try { text = File.ReadAllText(legacyPath, Encoding.UTF8); }
            catch (Exception e) { Fail(sb, label, legacyPath, key, "读不了：" + e.Message); return; }

            string problem;
            if (!SaveCenter.IsWellFormedJsonObject(text, out problem))
            {
                Fail(sb, label, legacyPath, key, "旧档不是合法 JSON 对象（" + problem + "）⇒ 不导，旧文件原地不动");
                return;
            }

            if (!SaveCenter.SetRaw(key, text))
            {
                Fail(sb, label, legacyPath, key, "写中央存档失败（原因见上一条 `[SaveCenter]` 日志）");
                return;
            }
            LastImportedCount++;
            sb.Append("  ✅ ").Append(label).Append("：`").Append(Rel(legacyPath)).Append("` → `").Append(key)
              .Append("`（").Append(Utf8Bytes(text)).Append(" B，" ).Append(how).AppendLine("）");
        }

        /// <summary>`profile.json` → `CosmeticData`，**只取 `avatarIndex` + `titleIndex`**（简报明确要求）。
        /// ⚠️ 同一份文件里的 `playerName` / `avatarBorderOn` / `achIds` / `achValues` **不是**「静态定义」，
        /// 它们也是玩家状态 —— 本轮按简报不迁，已写进报告 §五。</summary>
        static void ImportProfileCosmetic(StringBuilder sb, string legacyPath)
        {
            const string label = "个人资料";
            string key = SaveKeys.CosmeticData;
            if (!File.Exists(legacyPath)) { Skip(sb, label, legacyPath, key, "没有这份旧档"); return; }
            if (SaveCenter.Exists(key)) { Skip(sb, label, legacyPath, key, "中央存档已有 `" + key + "`（不覆盖）"); return; }

            string text;
            try { text = File.ReadAllText(legacyPath, Encoding.UTF8); }
            catch (Exception e) { Fail(sb, label, legacyPath, key, "读不了：" + e.Message); return; }

            string problem;
            if (!SaveCenter.IsWellFormedJsonObject(text, out problem))
            {
                Fail(sb, label, legacyPath, key, "旧档不是合法 JSON 对象（" + problem + "）⇒ 不导");
                return;
            }

            LegacyProfileFields fields;
            try { fields = JsonUtility.FromJson<LegacyProfileFields>(text); }
            catch (Exception e) { Fail(sb, label, legacyPath, key, "字段解不出来：" + e.Message); return; }
            if (fields == null) { Fail(sb, label, legacyPath, key, "解出来是 null ⇒ 不导"); return; }

            string payload = JsonUtility.ToJson(new LegacyCosmeticPayload
            {
                avatarIndex = fields.avatarIndex,
                titleIndex = fields.titleIndex,
            });
            if (!SaveCenter.SetRaw(key, payload))
            {
                Fail(sb, label, legacyPath, key, "写中央存档失败（原因见上一条 `[SaveCenter]` 日志）");
                return;
            }
            LastImportedCount++;
            sb.Append("  ✅ ").Append(label).Append("：`").Append(Rel(legacyPath)).Append("` → `").Append(key)
              .Append("`（**只取两个字段** avatarIndex=").Append(fields.avatarIndex)
              .Append(" · titleIndex=").Append(fields.titleIndex).AppendLine("）");
            sb.AppendLine("     ⚠️ 同一份文件里的 `playerName` / `avatarBorderOn` / `achIds/achValues` **本轮没迁**（见报告 §五）");
        }

        /// <summary>`WarpforgeInventory/<name>.json` → `_ours_Inventory.<name>`（一个背包一个 key）。</summary>
        static void ImportInventoryDir(StringBuilder sb, string legacyDir)
        {
            const string label = "背包";
            if (!Directory.Exists(legacyDir)) { Skip(sb, label, legacyDir, SaveKeys.OursInventoryPrefix + "*", "没有这个目录"); return; }

            string[] files = Directory.GetFiles(legacyDir);
            Array.Sort(files, StringComparer.Ordinal);          // 顺序稳定 ⇒ 报告可逐字比
            bool anyJson = false;
            for (int i = 0; i < files.Length; i++)
            {
                string f = files[i];
                if (!f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                anyJson = true;
                string name = Path.GetFileNameWithoutExtension(f);
                string key = SaveKeys.OursInventoryPrefix + name;
                if (SaveCenter.Exists(key)) { Skip(sb, label + "(" + name + ")", f, key, "中央存档已有（不覆盖）"); continue; }

                string text;
                try { text = File.ReadAllText(f, Encoding.UTF8); }
                catch (Exception e) { Fail(sb, label + "(" + name + ")", f, key, "读不了：" + e.Message); continue; }

                string problem;
                if (!SaveCenter.IsWellFormedJsonObject(text, out problem))
                {
                    Fail(sb, label + "(" + name + ")", f, key, "旧档不是合法 JSON 对象（" + problem + "）⇒ 不导");
                    continue;
                }
                if (!SaveCenter.SetRaw(key, text))
                {
                    Fail(sb, label + "(" + name + ")", f, key, "写中央存档失败（原因见上一条 `[SaveCenter]` 日志）");
                    continue;
                }
                LastImportedCount++;
                sb.Append("  ✅ ").Append(label).Append("：`").Append(Rel(f)).Append("` → `").Append(key)
                  .Append("`（").Append(Utf8Bytes(text)).Append(" B，整份导入；原版无对应 key ⇒ `_ours_`）").AppendLine();
            }
            if (!anyJson) Skip(sb, label, legacyDir, SaveKeys.OursInventoryPrefix + "*", "目录里没有 `.json`");
        }

        static void Skip(StringBuilder sb, string label, string legacyPath, string key, string why)
        {
            LastSkippedCount++;
            sb.Append("  ⏭ ").Append(label).Append("：`").Append(Rel(legacyPath)).Append("` → `").Append(key)
              .Append("`（").Append(why).AppendLine("）");
        }

        static void Fail(StringBuilder sb, string label, string legacyPath, string key, string why)
        {
            LastFailedCount++;
            sb.Append("  ✖ ").Append(label).Append("：`").Append(Rel(legacyPath)).Append("` → `").Append(key)
              .Append("`（").Append(why).AppendLine("）");
        }

        static string Rel(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string root = LegacyRoot;
            if (!string.IsNullOrEmpty(root) && path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                string tail = path.Substring(root.Length).TrimStart('\\', '/');
                return tail.Replace('\\', '/');
            }
            return path.Replace('\\', '/');
        }

        static int Utf8Bytes(string s)
        {
            return s == null ? 0 : new UTF8Encoding(false).GetByteCount(s);
        }
    }

    /// <summary>只读 `profile.json` 里我们关心的两个字段（`JsonUtility` 自动忽略其余字段）。
    /// <para>⚠️ 文件级、非 `MonoBehaviour`（同 `SaveVersionProbe` 的理由）。</para></summary>
    [Serializable]
#pragma warning disable 0649   // 这两个字段只由 `JsonUtility` 反序列化填 —— 编译器看不见写点（同理于全工程所有 JsonUtility DTO）
    class LegacyProfileFields
    {
        public int avatarIndex;
        public int titleIndex = -1;
    }
#pragma warning restore 0649

    /// <summary>`CosmeticData` 的 payload 形状（**只有原版这一步需要的两个字段**）。</summary>
    [Serializable]
    class LegacyCosmeticPayload
    {
        public int avatarIndex;
        public int titleIndex = -1;
    }
}
