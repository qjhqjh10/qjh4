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
//   └ WarpforgeProfile/profile.json              ┐ → CosmeticData                · **整份**（6 个字段全进，见下）
//                                                 └ → AchievementsData            · **投影**（achIds/achValues → 原版形状）
//   ⛔ 不导入（原因写在报告里）：`PlayerPrefs`（语言/音量，属**设置层**）· `WarpforgeNet.json`（联机设置，同设置层）·
//      `WarpforgeReplays/`（录像大文件；**引用**已经在 `BattleLogData` 的 `ReplayFile` 字段里）
//
// 🔴 **`profile.json` 的 6 个字段（2026-10-25 · 执行代理 R10 / 切换单 C8 补齐）**
//    `Shell/ProfileData.cs` 的 `SaveDto` 有 7 个字段（`ver` + 6 个真数据）。**这 6 个全都属于玩家状态**：
//      · `playerName`      —— 玩家显示名。原版在 **PlayFab 玩家档案**（`PlayerDataManager.SetPlayfabDisplayName`
//                             → `PlayFabClientAPI.UpdateUserTitleDisplayName`），**不是**某个 `PlayerDataKey`
//                             ⇒ 我们放 `CosmeticData`（本地没有「玩家档案」这一层）。
//                             ⚠️ **别按方案 §六·1 的旧建议塞进 `NameChanges`** —— 那个 key（`PlayerDataKey` 47）装的是
//                             `NameChangeRecord { originalName, changes[] }`（**改名历史**），不是当前显示名。
//      · `avatarIndex` / `titleIndex` / `avatarBorderOn` —— 外观，原版 `CosmeticData`（key 13）✅ 同一个 key。
//      · `achIds` / `achValues` —— 成就进度，原版 `AchievementsData`（key 10），形状 =
//                             `Dictionary<string, AchievementPersistence>`（`Everguild.LiveOps.AchievementPersistence`：
//                             `id` / `highestValue` / `value` / `enabled` + 基类 `ChallengeProgress` 的
//                             `collectedRewards` / `premiumCollectedRewards` / `customData` / `resetProgress`）。
//                             ⇒ 见 <see cref="BuildAchievementsPayload"/>。
//    🔴 **为什么整份进 `CosmeticData`（而不是「只把外观放进去」）**：双写是 **1 个 key ↔ 1 份旧档**，
//       而 `profile.json` 是**一个文件装 6 个字段** ⇒ 中央那份 payload 必须是这 6 个字段的**并集**，
//       否则双写就会用少字段覆盖多字段（就是 E3/R4 点名的那颗「数据丢失地雷」）。
//
// ⚠️ **调用点（2026-10-25 现状）**：`Shell/ShellRuntime.cs:322-326` 的 `Awake` —— 门是 `SaveCenter.StoreOn`，
//    顺序固定 **先 `ImportAll()`、后 `RegisterMirrors()`**（见那段注释）。
//    ⛔ 早先这里写的是「本文件不接入任何现有子系统」—— 那是 R4 接线**之前**的话，已过期（铁律 5 就地更正）。

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
            for (int i = 0; i < Entries.Length; i++)
            {
                Entry e = Entries[i];
                string legacyPath = Path.Combine(root, e.Rel);
                if (e.ProfileSpecial) ImportProfile(sb, legacyPath, e.Label, e.Key);
                else ImportWholeFile(sb, e.Label, legacyPath, e.Key, e.How);
            }
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
            string newer = NewerLegacyReport();
            if (newer.Length > 0)
            {
                sb.AppendLine("  🔴 **双写期诊断**（旧档比中央档新 ⇒ 中央档已过期）：");
                sb.Append(newer);
            }
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

        /// <summary>`profile.json` → `CosmeticData`（**整份**：`ver` + 6 个字段）+ 成就**投影**到 `AchievementsData`。
        /// <para>🔴 **2026-10-25（执行代理 `R10` · 切换单 C8）**：原来这里只取 `avatarIndex`/`titleIndex`
        /// —— 那是 `profile.json` 6 个字段里的一半不到，中央档因此**承载不了整份旧档** ⇒ 双写会抹掉
        /// `playerName` / `avatarBorderOn` / `achIds` / `achValues`（E3/R4 点名的那颗地雷）。现在改成：
        /// ① 旧档原文**整份**转存 `CosmeticData`（逐字，与其它 6 类「整份导入」同口径）
        ///    ⇒ 「切前 dump vs 切后中央档」逐字段相等是**构造上成立**的，不是靠两个 DTO 对得上；
        /// ② 再把 `achIds`/`achValues` 投影成原版 `AchievementsData` 的形状（见 <see cref="BuildAchievementsPayload"/>）。</para>
        /// <para>⚠️ **已有中央档时**：不覆盖（红线②），但会**查一下它的字段全不全** —— 若它是旧版导入留下的
        /// 「只有两个字段」那种，这里**大声报出来**（不然双写/读口都会把另外 4 个字段当不存在，静默丢数据）。</para></summary>
        static void ImportProfile(StringBuilder sb, string legacyPath, string label, string key)
        {
            if (!File.Exists(legacyPath)) { Skip(sb, label, legacyPath, key, "没有这份旧档"); return; }

            string text;
            try { text = File.ReadAllText(legacyPath, Encoding.UTF8); }
            catch (Exception e) { Fail(sb, label, legacyPath, key, "读不了：" + e.Message); return; }

            string problem;
            if (!SaveCenter.IsWellFormedJsonObject(text, out problem))
            {
                Fail(sb, label, legacyPath, key, "旧档不是合法 JSON 对象（" + problem + "）⇒ 不导");
                return;
            }

            bool wroteAnything = false;

            // ---- ① `CosmeticData`：整份转存（⛔ 不按字段挑；挑字段 = 双写时用少覆盖多）----
            if (!SaveCenter.Exists(key))
            {
                if (!SaveCenter.SetRaw(key, text))
                {
                    Fail(sb, label, legacyPath, key, "写中央存档失败（原因见上一条 `[SaveCenter]` 日志）");
                    return;
                }
                wroteAnything = true;
                sb.Append("  ✅ ").Append(label).Append("：`").Append(Rel(legacyPath)).Append("` → `").Append(key)
                  .Append("`（").Append(Utf8Bytes(text)).Append(" B，**整份导入**；6 个字段全进：")
                  .AppendLine("playerName / avatarIndex / titleIndex / avatarBorderOn / achIds / achValues）");
            }
            else
            {
                string centralPayload = SaveCenter.GetRaw(key);
                string incomplete = centralPayload == null ? null : MissingProfileFields(centralPayload);
                if (centralPayload == null)
                {
                    // 存在但读不出来（坏档已隔离 / 未来版本被锁）⇒ **一个字节都别写**。
                    // 读口 `ReadThroughLegacy` 会自动退回旧档那一份，玩家的 6 个字段照样读得到。
                    Fail(sb, label, legacyPath, key, "中央档 `" + key + "` **存在但读不出来**（坏档 / 未来版本，见上面 `[SaveCenter]` 日志）"
                       + " ⇒ 本次不导入、不动它（读口会退回旧档那一份）");
                }
                else if (incomplete == null)
                {
                    Skip(sb, label, legacyPath, key, "中央存档已有 `" + key + "`（不覆盖）");
                }
                else
                {
                    // 🔴 中央档**缺字段**（旧版只迁了两个字段留下的那种）：**只补缺的键**、已有的一律不动。
                    //    为什么必须补：读口是「先中央后旧档」⇒ 不补的话那 4 个字段在玩家眼里就是**没了**
                    //    （旧档里其实还在）。⛔ 也别整份覆盖 —— 红线②「不覆盖已有的中央存档」。
                    string merged, why;
                    if (!TryFillMissingProfileFields(centralPayload, text, out merged, out why))
                    {
                        Fail(sb, label, legacyPath, key, "中央档 `" + key + "` **字段不全**（缺 " + incomplete
                           + "）而且**补不回来**：" + why + " ⇒ ⛔ 不覆盖（红线②），请人工处理");
                    }
                    else if (!SaveCenter.SetRaw(key, merged))
                    {
                        Fail(sb, label, legacyPath, key, "中央档 `" + key + "` 补字段时写盘失败（原因见上一条 `[SaveCenter]` 日志）");
                    }
                    else
                    {
                        wroteAnything = true;
                        sb.Append("  ✅ ").Append(label).Append("：中央档 `").Append(key)
                          .Append("` **补齐了缺的字段**（").Append(incomplete)
                          .AppendLine("；已有的键一个都没动）");
                    }
                }
            }

            // ---- ② `AchievementsData`：成就进度投影（原版 key 10；旧档里没有单独文件 ⇒ 从 profile.json 取）----
            if (!SaveCenter.Exists(SaveKeys.AchievementsData))
            {
                LegacyProfileAchievements ach = null;
                try { ach = JsonUtility.FromJson<LegacyProfileAchievements>(text); }
                catch (Exception e) { Fail(sb, label, legacyPath, SaveKeys.AchievementsData, "成就字段解不出来：" + e.Message); }
                if (ach != null)
                {
                    string ap = BuildAchievementsPayload(ach.achIds, ach.achValues);
                    if (!SaveCenter.SetRaw(SaveKeys.AchievementsData, ap))
                        Fail(sb, label, legacyPath, SaveKeys.AchievementsData, "写中央存档失败（原因见上一条 `[SaveCenter]` 日志）");
                    else
                    {
                        wroteAnything = true;
                        sb.Append("  ✅ ").Append(label).Append("：`").Append(Rel(legacyPath)).Append("` → `")
                          .Append(SaveKeys.AchievementsData).Append("`（投影 ").Append(ach.achIds == null ? 0 : ach.achIds.Length)
                          .AppendLine(" 条成就；形状 = 原版 `AchievementPersistence` 字典）");
                    }
                }
            }

            if (wroteAnything) LastImportedCount++;
        }

        /// <summary>`profile.json` 那一族**应该有**的 7 个顶层键（`ver` + 6 个真数据）。
        /// 判据 = `Shell/ProfileData.cs` 的 `SaveDto`（那边的字段名就是这份文件的形状）。</summary>
        public static readonly string[] ProfileFields =
        {
            "ver", "playerName", "avatarIndex", "titleIndex", "avatarBorderOn", "achIds", "achValues",
        };

        /// <summary>这份 payload 缺了 `profile.json` 该有的哪几个顶层键（返回 `null` = **一个都不缺**）。
        /// <para>🆕 **2026-10-25（`R10`）**：两处用它 —— ① 导入时查「已有的中央档是不是旧版留下的 2 字段版」
        /// （<see cref="ImportProfile"/>）② **写口**（`ProfileData.SaveNow`）写盘前的最后一道闸：
        /// payload 少一个字段就**不许写**（否则双写会用少字段覆盖多字段 = 抹掉玩家名/成就）。</para>
        /// <para>⚠️ 解析不了也返回非 null（"解析失败…"）—— 调用方一律当「不能写」处理。</para></summary>
        public static string MissingProfileFields(string payloadJson)
        {
            if (payloadJson == null) return "整份读不出来";
            string err;
            SortedDictionary<string, string> flat = SaveJsonDiff.Flatten(payloadJson, out err);
            if (err != null) return "解析失败：" + err;
            StringBuilder missing = null;
            for (int i = 0; i < ProfileFields.Length; i++)
            {
                if (HasTopField(flat, ProfileFields[i])) continue;
                if (missing == null) missing = new StringBuilder();
                if (missing.Length > 0) missing.Append(" / ");
                missing.Append('`').Append(ProfileFields[i]).Append('`');
            }
            return missing == null ? null : missing.ToString();
        }

        /// <summary>「补缺迁移」：中央档**缺**的顶层键从旧档补上，**已有的键一个都不动**。
        /// <para>只用在 <see cref="ImportProfile"/>：旧版导入器只写 `avatarIndex`/`titleIndex` 两个字段，
        /// 留下的中央档会被读口（先中央后旧档）当成「权威」⇒ 另外 4 个字段在玩家眼里就没了。
        /// ⚠️ 拿**键是否存在**判断「缺」（⚠️ 不是拿值是否为空 —— 空名字 / 0 值都是**合法值**）。</para>
        /// <para>⚠️ 代价（已知、可接受）：输出是**按 <see cref="LegacyProfileFields"/> 重新序列化**的
        /// ⇒ 中央档里若有**未知的多余键**会丢（v1 的 payload 不会有；将来加字段请同步改那个 DTO）。</para>
        /// <para>返回 false ⇒ `why` 给原因（调用方**不许**当成「补好了」）。</para></summary>
        static bool TryFillMissingProfileFields(string centralPayload, string legacyText,
                                                out string merged, out string why)
        {
            merged = null; why = null;
            string ce;
            SortedDictionary<string, string> flat = SaveJsonDiff.Flatten(centralPayload ?? "{}", out ce);
            if (ce != null) { why = "中央档解析失败：" + ce; return false; }

            LegacyProfileFields c, l;
            try
            {
                c = JsonUtility.FromJson<LegacyProfileFields>(centralPayload ?? "{}") ?? new LegacyProfileFields();
                l = JsonUtility.FromJson<LegacyProfileFields>(legacyText);
            }
            catch (Exception e) { why = "解析失败：" + e.Message; return false; }
            if (l == null) { why = "旧档解析成 null"; return false; }

            if (!HasTopField(flat, "ver")) c.ver = l.ver;
            if (!HasTopField(flat, "playerName")) c.playerName = l.playerName;
            if (!HasTopField(flat, "avatarIndex")) c.avatarIndex = l.avatarIndex;
            if (!HasTopField(flat, "titleIndex")) c.titleIndex = l.titleIndex;
            if (!HasTopField(flat, "avatarBorderOn")) c.avatarBorderOn = l.avatarBorderOn;
            // 两个数组按**一对**处理（只补一半的话，id 与 value 会错位）
            if (!HasTopField(flat, "achIds") || !HasTopField(flat, "achValues"))
            {
                c.achIds = l.achIds;
                c.achValues = l.achValues;
            }

            merged = JsonUtility.ToJson(c);
            string still = MissingProfileFields(merged);
            if (still != null) { why = "补完还是缺 " + still + "（旧档本身也不全？）"; merged = null; return false; }
            return true;
        }

        /// <summary>摊平后的键表里有没有这个**顶层字段**。        /// <para>⚠️ **非空数组不会在「数组本身」那个路径上留条目** —— `SaveJsonDiff.Flatten` 只写
        /// `"achIds"[0]` / `"achIds"[1]` …；空数组才写 `"achIds" = "[]"`（判据 = `FlattenArray`）。
        /// 所以「有字段」= 恰好那个键 **或** 以 `<键>[` 开头的下标键。⛔ 别只查 `ContainsKey`。</para></summary>
        static bool HasTopField(SortedDictionary<string, string> flat, string name)
        {
            string q = SaveJsonDiff.Quote(name);
            if (flat.ContainsKey(q)) return true;
            string prefix = q + "[";
            foreach (KeyValuePair<string, string> kv in flat)
                if (kv.Key.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>`WarpforgeInventory/<name>.json` → `_ours_Inventory.<name>`（一个背包一个 key）。</summary>
        static void ImportInventoryDir(StringBuilder sb, string legacyDir)
        {
            const string label = "背包";
            if (!Directory.Exists(legacyDir)) { Skip(sb, label, legacyDir, SaveKeys.OursInventoryPrefix + "*", "没有这个目录"); return; }

            string[] files = Directory.GetFiles(legacyDir);
            Array.Sort(files, StringComparer.Ordinal);          // 顺序稳定 ⇒ 报告可逐字比
            // 🔴 Windows 的文件名**大小写不敏感** ⇒ `Shop.json` 与 `shop.json` 会折成两个 key
            //    （`_ours_Inventory.Shop` / `.shop`）却落到**同一份中央档**（`<key>.json`）⇒ 后写的静默盖掉先写的。
            //    老档一个都不删 ⇒ 数据不会永久丢，但中央档那份会互相覆盖 ⇒ **必须出声**。
            string foldProblem = InventoryCaseFoldProblem(files);
            if (foldProblem.Length > 0) Fail(sb, label, legacyDir, SaveKeys.OursInventoryPrefix + "*", foldProblem);
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

        // ════════════════════════════════════════════════════════════════════════════════════
        //  Step 4 —— 「key ↔ 旧档」的唯一一张表 + 双写登记 + 双写期诊断
        //  ⚠️ 本节成员**追加在类尾**（`CLAUDE.md`：新成员别插进已有成员之间，免得下游行号漂）。
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>一条「旧档 → 中央 key」的映射。<see cref="Entries"/> 是**唯一正本**：
        /// 导入（Step 2）、双写登记（Step 4）、双写期诊断都从它取，⛔ 别在别处再抄一份。</summary>
        public struct Entry
        {
            /// <summary>给人看的名字（报告里那一列）。</summary>
            public string Label;
            /// <summary>旧档相对 `persistentDataPath` 的路径（`Path.Combine` 吃得下 `/`）。</summary>
            public string Rel;
            /// <summary>中央存档的 key（`SaveKeys`）。</summary>
            public string Key;
            /// <summary>`true` = 这份旧档走**专用导入**（<see cref="ImportProfile"/>：整份转存 + 成就投影），
            /// 而不是 <see cref="ImportWholeFile"/>。现读只有 `profile.json` 一条。
            /// <para>🔴 **它不再决定「能不能双写」** —— 2026-10-25（执行代理 `R10` · 切换单 C8）起
            /// `profile.json` 的 6 个字段**全都**进了中央档，「双写会抹数据」的前提已经不成立。
            /// 双写与否现在看 <see cref="NoMirror"/>（两件事分开，别再合成一个标志）。</para></summary>
            public bool ProfileSpecial;
            /// <summary>`true` = **禁止**把这份旧档登记进双写表（中央 payload 只承载旧档的**一部分**字段
            /// ⇒ 双写会用少字段覆盖多字段 = 静默抹数据）。
            /// <para>🔴 现读：**0 条**（C8 把最后一条 `profile.json` 补齐了）。**留着它是防线**：
            /// 以后再有「只迁了一部分」的 key，**必须**把它设成 true，并在注释里写清缺哪几个字段。</para></summary>
            public bool NoMirror;
            /// <summary>整份导入时写进报告的那句说明。</summary>
            public string How;
        }

        /// <summary>8 类旧档里**在文件里的那 7 类**（`WarpforgeInventory/` 是目录、逐文件，单独处理）。
        /// 判据 = 每条 `Rel` 后面的注释指回原模块的落盘常量。</summary>
        public static readonly Entry[] Entries =
        {
            new Entry { Label = "日常", Rel = DailyRel, Key = SaveKeys.DailyCounters,
                        How = "整份导入（`nextResetTicks/nextSet` 也在里面；`DailyLimits` 本轮不拆，见 `SaveKeys.cs` 那条注释）" },
            new Entry { Label = "轮抽", Rel = DraftRel, Key = SaveKeys.ActiveEvent, How = "整份导入" },
            new Entry { Label = "对局历史", Rel = BattleLogRel, Key = SaveKeys.BattleLogData, How = "整份导入" },
            new Entry { Label = "卡组", Rel = DecksRel, Key = SaveKeys.PlayerDecks,
                        How = "整份导入（`current` 也在里面；`CurrentDeck` 本轮不拆）" },
            new Entry { Label = "段位", Rel = RankedRel, Key = SaveKeys.PlayerSeason,
                        How = "整份导入（原版 `PlayerSeason` 语义对应；payload 是我们自己的形状）" },
            new Entry { Label = "卡牌等级", Rel = CardLevelsRel, Key = SaveKeys.OursCardProgress,
                        How = "整份导入（原版无对应 key ⇒ `_ours_`）" },
            new Entry { Label = "个人资料", Rel = ProfileRel, Key = SaveKeys.CosmeticData, ProfileSpecial = true,
                        How = "整份导入（6 个字段全进）+ 成就投影到 `AchievementsData`" },
        };

        /// <summary>Step 4：把已知的旧档逐条登记进 `SaveCenter` 的双写表 —— 登记之后写中央档会**自动双写**旧档。
        /// 返回登记了几条。幂等；⛔ 不读旧档内容、不写盘。
        /// <para>🔴 **`NoMirror` 的条目不登记**（现读 **0 条**）：命中「中央 payload 只有旧档的一部分字段」时，
        /// 双写会**用少字段覆盖多字段**（`profile.json` 当年就是这样：中央 2 字段 vs 旧档 6 字段 =
        /// 抹掉玩家名与成就进度，见 E3/R4 报告）。**那条现在已补齐**（`R10` · C8），所以 `profile.json`
        /// 也进双写表了；这道闸留着给以后「只迁一部分」的 key 用。</para></summary>
        public static int RegisterMirrors()
        {
            int n = 0;
            string root = LegacyRoot;
            for (int i = 0; i < Entries.Length; i++)
            {
                Entry e = Entries[i];
                if (e.NoMirror) continue;              // 🔴 见上面那段：只迁了一部分字段的 key **不许双写**
                if (SaveCenter.RegisterLegacyMirror(e.Key, Path.Combine(root, e.Rel))) n++;
            }
            string invDir = Path.Combine(root, InventoryDirRel);
            if (Directory.Exists(invDir))
            {
                string[] files = Directory.GetFiles(invDir);
                Array.Sort(files, StringComparer.Ordinal);
                // 同 `ImportInventoryDir`：登记之前先查「大小写折叠后撞车」（那会让两个 key 写同一份旧档 ——
                // `RegisterLegacyMirror` 的「两个 key 抢同一文件」也会拒，但那条错误信息不含全局视角）。
                string foldProblem = InventoryCaseFoldProblem(files);
                if (foldProblem.Length > 0)
                    Debug.LogError("[LegacySaveReader] 双写登记：" + foldProblem);
                for (int i = 0; i < files.Length; i++)
                {
                    if (!files[i].EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    if (SaveCenter.RegisterLegacyMirror(SaveKeys.OursInventoryPrefix + name, files[i])) n++;
                }
            }
            Debug.Log("[LegacySaveReader] 双写登记完成：" + n + " 条（`SaveCenter.LegacyMirrorOn` = "
                    + (SaveCenter.LegacyMirrorOn ? "true" : "false") + "）");
            return n;
        }

        /// <summary>列出「**旧档比中央档新**」的 key（已登记双写的那些）。返回 "" = 一条都没有（正常）。
        /// <para>为什么要有这条：双写期两边都由我们的客户端写，应当同步；但用户若**回滚到旧客户端**玩了一轮
        /// （旧客户端只写旧档、不知道中央档），那条 key 的旧档就会更新、中央档停在旧值 ——
        /// 而下一版启动时 `ImportAll()` 会**跳过**它（中央档已存在、不覆盖）⇒ 进度**静默倒退**。
        /// 这条把它显式报出来。⛔ **不自动倒灌**：用旧档覆盖中央档要人拍板（方案 §七 Step 4）。</para>
        /// <para>🔴 **判据是「内容不同 ∧ 旧档 mtime 更新」，⛔ 不能只看 mtime** ——
        /// `SaveCenter.SetRaw` 的双写顺序是**先中央、后旧档** ⇒ 正常双写之后旧档的 mtime **本来就更新**
        /// （差几毫秒）。只看 mtime 会在每一份健康存档上误报。</para></summary>
        public static string NewerLegacyReport()
        {
            string[] keys = SaveCenter.MirrorKeys();
            if (keys.Length == 0) return "";
            StringBuilder sb = null;
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                string legacyPath = SaveCenter.LegacyMirrorPathOf(key);
                if (string.IsNullOrEmpty(legacyPath)) continue;
                if (!File.Exists(legacyPath) || !File.Exists(SaveCenter.PathOf(key))) continue;

                // ① 内容一样 ⇒ 两边同步，**不管 mtime 谁新**（正常双写之后旧档 mtime 必然更新）
                string central = SaveCenter.GetRaw(key);
                string legacy;
                try { legacy = File.ReadAllText(legacyPath, Encoding.UTF8); }
                catch (Exception) { continue; }
                if (central != null && SaveCenter.DiffPayload(central, legacy).Length == 0) continue;

                // ② 内容不同 ∧ 旧档 mtime 更新 ⇒ 才可能是「回滚旧客户端玩过一轮」
                DateTime legacyTime = File.GetLastWriteTimeUtc(legacyPath);
                DateTime centralTime = File.GetLastWriteTimeUtc(SaveCenter.PathOf(key));
                if (legacyTime <= centralTime) continue;
                if (sb == null) sb = new StringBuilder();
                sb.Append("     ⚠️ `").Append(key).Append("`：旧档 ")
                  .Append(legacyTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture))
                  .Append("Z **比中央档 ")
                  .Append(centralTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture))
                  .AppendLine("Z 新，且内容不同** —— 像是用旧客户端写过 ⇒ 中央档已过期");
            }
            return sb == null ? "" : sb.ToString();
        }

        /// <summary>`WarpforgeInventory/` 里那批 `<name>.json` 折出来的 key 有没有**大小写折叠后撞车**。
        /// 返回 "" = 通过；非空 = 第一条问题（见 `SaveKeys.ValidateCaseFolding` 的理由）。
        /// <para>⚠️ 这一档**只在 Windows/macOS 这些文件名大小写不敏感的机器上**是真风险；
        /// 判据不是「你猜玩家会不会有两个」，而是「**我们不许静默盖掉**」（红线）。</para></summary>
        static string InventoryCaseFoldProblem(string[] files)
        {
            if (files == null || files.Length == 0) return "";
            int n = 0;
            for (int i = 0; i < files.Length; i++)
                if (files[i].EndsWith(".json", StringComparison.OrdinalIgnoreCase)) n++;
            if (n < 2) return "";
            string[] keys = new string[n];
            int w = 0;
            for (int i = 0; i < files.Length; i++)
            {
                if (!files[i].EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                keys[w++] = SaveKeys.OursInventoryPrefix + Path.GetFileNameWithoutExtension(files[i]);
            }
            return SaveKeys.ValidateCaseFolding(keys);
        }

        // ════════════════════════════════════════════════════════════════════════════════════════
        //  C8 —— `profile.json` 的成就进度 → 原版 `AchievementsData` 的形状
    //  判据（2026-10-25 · 执行代理 R10）
    //    · `PlayerDataKey.AchievementsData = 10`（`PlayerDataKey.cs:13`）
    //    · `PlayerDataManager.rawAchievementsData` 的类型 = `Dictionary<string, AchievementPersistence>`
    //      （`Warpforge_code/Scripts/Assembly-CSharp/PlayerDataManager.cs:953`）
    //    · `Everguild.LiveOps.AchievementPersistence`：`id` / `highestValue` / `value` / `enabled`
    //      （`.../Everguild/LiveOps/AchievementPersistence.cs:6-17`），基类 `ChallengeProgress`：
    //      `collectedRewards` / `premiumCollectedRewards` / `synced`（`[JsonIgnore]`）/
    //      `customData`（`[JsonIgnoreIfEmpty]`）/ `resetProgress`（`[JsonIgnoreIfEmpty(false)]`）。
    //  ⚠️ 三处「我们只能填默认值」的地方（如实记着，⛔ 不是原版读出来的）：
    //    ① `collectedRewards` / `premiumCollectedRewards`：原版记「已领了几档」；我们的档位是
    //       从进度值**现推**的（`ProfileData.AchievementTierOf`），没有领取记录 ⇒ 恒 0。
    //    ② `highestValue`：原版是「历史最高」，我们是「当前值」—— 我们的写点基本只涨不落，
    //       两者**在正常玩法下相等**；但 `SetAchievementValue` 显式调低时原版会保留最高值、我们不能
    //       ⇒ 已知偏离（要补就得在中央档多存一列，属后续项）。
    //    ③ `customData` 恒空（原版 `[JsonIgnoreIfEmpty]` ⇒ 空的时候本来就不写进 JSON）。
    // ════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>`AchievementsData` 的 payload（**原版形状**：`{ "Ach_N": {...}, ... }`）。
    /// 返回的是一份**可以直接交给 `SaveCenter.SetRaw` 的 JSON 对象**（`{}` = 一条进度都没有）。
    /// <para>⚠️ **为什么不是数组**：原版那一格是 `Dictionary<string, AchievementPersistence>`
    /// （`PlayerDataManager.cs:953`）⇒ 我们照它的形状写（键 = 成就 id）。`JsonUtility` 吃不了字典，
    /// 所以这里用 `SaveJsonDiff.Quote` 手工拼 —— 读的人将来照 `SaveJsonDiff.Flatten` 摊平即可
    /// （路径形如 `"Ach_1"."value"`）。</para>
    /// <para>⚠️ **顺序 = 序数序**（`SortedDictionary`）：原版是 `Dictionary` 的插入序；我们排序是为了
    /// 「同一份数据每次写出来逐字节一样」（可 diff / 可复现）。键顺序不影响语义。</para>
    /// <para>⚠️ 长度不齐时按**短的那边**配对；空 id 跳过；负数按 0 收（与 `ProfileData` 的 `Mathf.Max(0,…)` 同义）。</para></summary>
        public static string BuildAchievementsPayload(string[] ids, int[] values)
        {
            var map = new SortedDictionary<string, int>(StringComparer.Ordinal);
            if (ids != null && values != null)
            {
                for (int i = 0; i < ids.Length && i < values.Length; i++)
                {
                    if (string.IsNullOrEmpty(ids[i])) continue;
                    map[ids[i]] = values[i] < 0 ? 0 : values[i];   // 同一个 id 出现两次 ⇒ 后写赢（同 `Dictionary` 赋值）
                }
            }
            if (map.Count == 0) return "{}";

            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            bool first = true;
            foreach (KeyValuePair<string, int> kv in map)
            {
                if (!first) sb.Append(',');
                first = false;
                string q = SaveJsonDiff.Quote(kv.Key);
                string v = kv.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                sb.Append(q).Append(":{")
                  .Append("\"collectedRewards\":0,\"premiumCollectedRewards\":0,")
                  .Append("\"id\":").Append(q)
                  .Append(",\"highestValue\":").Append(v)
                  .Append(",\"value\":").Append(v)
                  .Append(",\"enabled\":true}");
            }
            sb.Append('}');
            return sb.ToString();
        }
    }

    /// <summary>`profile.json` 的 **7 字段形状** —— ⚠️ **只用于「补缺迁移」**（<see cref="LegacySaveReader"/> 的
    /// `TryFillMissingProfileFields`）；正常导入（整份转存）与正常写出（`ProfileData.SaveNow` 的 `ToJson`）都不走它。
    /// <para>🔴 **字段名必须与 `Shell/ProfileData.cs` 的 `SaveDto` 一字不差**（含默认值语义：
    /// `ver=1` / `titleIndex=-1` / `avatarBorderOn=true` / 两个空数组）—— 脚手架里有一条
    /// 「`SaveDto` 字段名 vs `LegacySaveReader.ProfileFields`」的漂移断言钉着这件事。</para>
    /// <para>⚠️ 文件级、非 `MonoBehaviour`（同 `SaveVersionProbe` 的理由）。</para></summary>
    [Serializable]
#pragma warning disable 0649   // 全部字段只由 `JsonUtility` 反序列化填 —— 编译器看不见写点（同理于全工程所有 JsonUtility DTO）
    class LegacyProfileFields
    {
        public int ver = 1;
        public string playerName = "";
        public int avatarIndex = 0;
        public int titleIndex = -1;
        public bool avatarBorderOn = true;
        public string[] achIds = new string[0];
        public int[] achValues = new int[0];
    }
#pragma warning restore 0649

    /// <summary>只读 `profile.json` 里的 `achIds` / `achValues`（`JsonUtility` 自动忽略其余字段）。
    /// <para>⚠️ 文件级、非 `MonoBehaviour`（同 `SaveVersionProbe` 的理由）。</para></summary>
    [Serializable]
#pragma warning disable 0649   // 这两个字段只由 `JsonUtility` 反序列化填 —— 编译器看不见写点（同理于全工程所有 JsonUtility DTO）
    class LegacyProfileAchievements
    {
        public string[] achIds;
        public int[] achValues;
    }
#pragma warning restore 0649
}
