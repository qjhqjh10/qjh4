// B110_W3_Assert.cs — 第一百一十会话 · 执行代理 `B110_W3`（2026-10-31）
//
// ============================ 它盯的是什么 ============================
//  `B-17` · 每日奖励 mock 表的**自相矛盾**：
//    `DailyData._rewardFreeName[3]` 印 `"20 Skulls"` 而 `_rewardFreeCount[3] = 160`
//    （Premium 同理 `40` vs `320`；卡包 `1`↔`20`、`2`↔`40`）
//    ⇒ `CollectReward` 按 `RewardAmount` 发、物品窗也印同一个数 ⇒ **玩家看到「160」而卡面写着 20 Skulls**。
//  修法（2026-10-31）= **改数量那一列**（`{150,1,200,20}` / `{300,2,400,40}`），卡面文案**一字不动**。
//
//  `B-18` · 图标 → 原版 item 类型的映射原来有**两份**
//    （`DailyData.ItemTypeNameOfIcon` 与 `DailyRewardPopup.RewardItemTypeOf`）。
//    修法 = 前者 `public` 化、后者只转发 ⇒ 本件在**行为面**（逐值相等）与**结构面**（方法体里没有第二份 `case`）两头钉住。
//
// ============================ 判据（原版 / 自定，逐条对上）============================
//  · 原版**查不到**（不是没查）：原版这两张表由服务端下发 —— `DailyRewardItemContainer__Initialize.c:95-114`
//    收的是 `ChallengeMilestone.rewards` 按 `rewardTier` 分组后的列表；关服后本地 0 命中（`d:/2/新解包资源` 无 `sequentialDays` 类数据）。
//    ⇒ 本地唯一可用判据 = **自洽**：卡面文案里的数 == `RewardAmount` == `CollectReward` 实发数。
//    同口径的**兄弟见证** = `_streakName`/`_streakAmount`（同一会话、同一提交写的，本来就是自洽的：1/20/2/40）。
//  · 🔶 这两张 mock 表本身是自定值（`DailyData.cs:765` 的 🔶 标记；用户 2026-09-17 边界②全解锁/固定资源）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A2：把 `_rewardFreeCount` 改回 `{150,20,200,160}`（或只改其中一格）⇒ 对应那一条红。
//  · A6/A7：表写成「全同一个数」/ 两轨共用一张 ⇒ 红。
//  · A8..A13：有人「反过来改文案」去迁就 `160`（`"160 Skulls"`）⇒ 名字那几条红（⛔ 修的是数量列，不是文案）。
//  · C8：`RewardItemTypeOf` 改成返回常量 / 又抄一份 switch ⇒ 红。
//  · C9/C10/C13：第二份映射重新出现（`case` 分支 / 图标名字面量 / 卡包分支数 2）⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/<宿主>.cs` 那条聚合行**同一物理行、第一个 `//` 之前**追加（行数零变化）：
//      B110_W3_Assert.Run(); pass += B110_W3_Assert.Pass; fail += B110_W3_Assert.Fail;
//  ⚠️ 本件**不需要**建任何窗口/场景：只读 `DailyData` 的静态读口 + 扫两份源码文本。
using System;
using System.IO;
using System.Text;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B110_W3`：每日奖励表的「文案数 == 发放数」（B-17）· 图标→类型映射唯一（B-18）。</summary>
public static class B110_W3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B110W3] ");

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B110_W3：每日奖励「文案数 == 发放数」（B-17）· 图标→类型映射唯一（B-18）");

        SectionA_DailyTableConsistent();
        SectionB_StreakTableFaithful();
        SectionC_TypeMapSingleSource();

        Debug.Log(_sink.P + "B110W3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 每日奖励表：文案数 == 发放数（逐天 × 两轨）
    static void SectionA_DailyTableConsistent()
    {
        MenuCheck.Section(_sink, "A 每日奖励：卡面文案里的数 == `RewardAmount` == `RewardSpecsOf[0].Quantity`（4 天 × 普通/Premium）");

        C(DailyData.RewardDays, 4, "A0（前提）`RewardDays == 4`（表是 4 天；变了这里要一起核）");
        Tr(DailyData.RewardSpecsOverrideForTest == null,
           "A0b（前提）`RewardSpecsOverrideForTest` 未被别的自检留在非 null 态（否则下面读的是夹具，不是真表）");

        int compared = 0;
        for (int day = 0; day < DailyData.RewardDays; day++)
        {
            for (int p = 0; p <= 1; p++)
            {
                bool prem = p == 1;
                string tag = "第 " + (day + 1) + " 天" + (prem ? " Premium" : " 普通");
                string name = DailyData.RewardName(day, prem);
                int amount = DailyData.RewardAmount(day, prem);

                int printed;
                bool hasNum = TryLeadingInt(name, out printed);
                Tr(hasNum, "A1（前提）" + tag + " 的文案以数字开头（`" + name + "`）—— 否则下面无从比");
                if (!hasNum) continue;

                C(printed, amount,
                  "A2 ★★ " + tag + "：文案里的数（" + printed + "）== 实际发放数（" + amount + "）"
                + "｜🧨 两边不一致 ⇒ 玩家看到 20 却拿到 160（B-17 的原形状）");
                compared++;

                var specs = DailyData.RewardSpecsOf(day, prem);
                Tr(specs != null && specs.Length == 1,
                   "A3（前提）" + tag + " 的奖励列表恰好 1 条（空表 / 多条 ⇒ A4 不成立）");
                if (specs != null && specs.Length == 1)
                    C(specs[0].Quantity, amount,
                      "A4 ★ " + tag + "：预览/发奖那份 `RewardSpec[0].Quantity` == `RewardAmount`（同一份数据，⛔ 别另写一个数）");
            }
        }
        C(compared, DailyData.RewardDays * 2, "A5 ★ 8 格全部比过（4 天 × 2 轨）—— 少一格说明有格没进循环");

        // 灭自证：表不是「全同一个值」/ 两轨不是同一张
        Tr(DailyData.RewardAmount(0, false) != DailyData.RewardAmount(1, false),
           "A6 ★（灭自证）第 1 天 vs 第 2 天数量不同 ⇒ 表真的按天下标取，不是恒某个固定值");
        Tr(DailyData.RewardAmount(0, false) != DailyData.RewardAmount(0, true),
           "A7 ★（灭自证）普通轨 vs Premium 轨数量不同 ⇒ 轨道参数真的被用上");

        // 文案本身**不许动**（修的是数量那一列）：逐条钉住玩家卡面看到的那 6 条字
        C(DailyData.RewardName(0, false), "150 Gold", "A8 第 1 天（普通）文案保持 `150 Gold`（⛔ 别反过来改文案去迁就旧数量）");
        C(DailyData.RewardName(1, false), "1 Booster pack", "A9 第 2 天（普通）文案保持 `1 Booster pack`");
        C(DailyData.RewardName(3, false), "20 Skulls", "A10 第 4 天（普通）文案保持 `20 Skulls`");
        C(DailyData.RewardName(0, true), "300 Gold", "A11 第 1 天（Premium）文案保持 `300 Gold`");
        C(DailyData.RewardName(1, true), "2 Booster packs", "A12 第 2 天（Premium）文案保持 `2 Booster packs`");
        C(DailyData.RewardName(3, true), "40 Skulls", "A13 第 4 天（Premium）文案保持 `40 Skulls`");
    }

    // ============================================================ B · 连登表：同一口径的兄弟见证
    static void SectionB_StreakTableFaithful()
    {
        MenuCheck.Section(_sink, "B 连登：同口径（名字里的数 == `StreakRewardSpecsOf(i)[0].Quantity`）—— 每日奖励的兄弟见证");

        int n = DailyData.StreakDays;
        C(n, 7, "B0（前提）`StreakDays == 7`（`_streakName` / `_streakAmount` 都是 7 格）");
        Tr(DailyData.StreakRewardSpecsOverrideForTest == null,
           "B0c（前提）`StreakRewardSpecsOverrideForTest` 是 null（否则下面读的是夹具）");
        Tr(n >= 1 && n <= 7, "B0b 格数落在 `_streakName` 的 7 格之内（否则取下标会越界）");

        int compared = 0;
        for (int i = 0; i < n && i < 7; i++)
        {
            string name = DailyData.StreakRewardName(i);
            int printed;
            bool hasNum = TryLeadingInt(name, out printed);
            Tr(hasNum, "B1（前提）连登第 " + (i + 1) + " 格文案以数字开头（`" + name + "`）");
            if (!hasNum) continue;

            var specs = DailyData.StreakRewardSpecsOf(i);
            Tr(specs != null && specs.Length == 1, "B2（前提）连登第 " + (i + 1) + " 格恰好 1 条奖励");
            if (specs == null || specs.Length != 1) continue;

            C(specs[0].Quantity, printed,
              "B3 ★ 连登第 " + (i + 1) + " 格：`" + name + "` 印的数 == 实发数（" + specs[0].Quantity + "）");
            compared++;
        }
        C(compared, n, "B4 ★ 全部 " + n + " 格都比过（少一格说明有格没进循环）");
    }

    // ============================================================ C · 图标→类型映射唯一
    static void SectionC_TypeMapSingleSource()
    {
        MenuCheck.Section(_sink, "C 图标→类型映射：唯一一份在 `DailyData.ItemTypeNameOfIcon`，`DailyRewardPopup.RewardItemTypeOf` 只转发（B-18）");

        // ---- 行为面：逐值相等（含未知值 / null）----
        string[] arts = { "40k_topmarquee_currency_gold", "40K_missions_icon_Daily_skulls",
                          "40k_main_bt_rewards", "B110W3_NO_SUCH_ART", null };
        for (int i = 0; i < arts.Length; i++)
        {
            string a = arts[i];
            C(DailyRewardPopup.RewardItemTypeOf(a), DailyData.ItemTypeNameOfIcon(a),
              "C1 ★ `" + (a ?? "(null)") + "`：转发的返回值与唯一映射**逐字相同**（含 null）");
        }
        C(DailyData.ItemTypeNameOfIcon("40k_topmarquee_currency_gold"), "Currency", "C2 金币图标 ⇒ `Currency`");
        C(DailyData.ItemTypeNameOfIcon("40K_missions_icon_Daily_skulls"), "Currency", "C3 骷髅图标 ⇒ `Currency`");
        C(DailyData.ItemTypeNameOfIcon("40k_main_bt_rewards"), "Everguild.LiveOps.ShopContainer",
          "C4 卡包图标 ⇒ `Everguild.LiveOps.ShopContainer`");
        Tr(DailyData.ItemTypeNameOfIcon("B110W3_NO_SUCH_ART") == null,
           "C5 未知图标 ⇒ **null**（判据空；铁律 2：⛔ 不猜一个类型出来）");

        // ---- 结构面 ①：`DailyRewardPopup.RewardItemTypeOf` 的方法体只许转发 ----
        string popupSrc = ReadSourceOrNull("CardPresentation/Shell/DailyRewardPopup.cs");
        Tr(popupSrc != null, "C6（前提）读得到 `DailyRewardPopup.cs` 源码（读不到 ⇒ C7/C8/C9 空转）");
        if (popupSrc != null)
        {
            string body = MethodBodyNoComments(popupSrc, "public static string RewardItemTypeOf(string art)");
            Tr(body != null, "C7（前提）在源码里定位到 `RewardItemTypeOf` 的方法体");
            if (body != null)
            {
                Tr(body.IndexOf("DailyData.ItemTypeNameOfIcon", StringComparison.Ordinal) >= 0,
                   "C8 ★★ 方法体**转发**到 `DailyData.ItemTypeNameOfIcon`｜🧨 改成返回常量 / 另写一份 ⇒ 红");
                Tr(body.IndexOf("case ", StringComparison.Ordinal) < 0,
                   "C9 ★★ 方法体里**没有** `case` 分支 ⇒ 它不再是第二份映射");
                Tr(body.IndexOf("\"40k_", StringComparison.Ordinal) < 0,
                   "C10 ★ 方法体里**没有**图标名字面量（映射表只在 `DailyData`）");
            }
        }

        // ---- 结构面 ②：唯一那份映射里，卡包分支恰好 1 个 ----
        string dataSrc = ReadSourceOrNull("CardPresentation/Shell/DailyData.cs");
        Tr(dataSrc != null, "C11（前提）读得到 `DailyData.cs` 源码");
        if (dataSrc != null)
        {
            string mapBody = MethodBodyNoComments(dataSrc, "public static string ItemTypeNameOfIcon(string art)");
            Tr(mapBody != null, "C12（前提）定位到 `ItemTypeNameOfIcon` 的方法体（也顺带钉住它仍是 `public`）");
            if (mapBody != null)
            {
                C(CountOf(mapBody, "case \"40k_main_bt_rewards\""), 1,
                  "C13 ★ 唯一映射里**恰好 1** 个卡包分支｜🧨 第二份映射混进来 ⇒ 变 2");
                Tr(mapBody.IndexOf("Everguild.LiveOps.ShopContainer", StringComparison.Ordinal) >= 0,
                   "C14 ★ 唯一映射里确有卡包那一档的返回值");
            }
        }
    }

    // ============================================================ 辅助
    /// <summary>取文案开头的十进制整数（`"20 Skulls"` ⇒ 20 · `"1 Booster pack"` ⇒ 1）。</summary>
    static bool TryLeadingInt(string s, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(s)) return false;
        int i = 0, acc = 0;
        while (i < s.Length && s[i] >= '0' && s[i] <= '9') { acc = acc * 10 + (s[i] - '0'); i++; }
        if (i == 0) return false;
        value = acc;
        return true;
    }

    /// <summary>`Application.dataPath` + 相对路径读源码；读不到返回 null 并出声（⛔ 不静默）。</summary>
    static string ReadSourceOrNull(string rel)
    {
        var full = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
        try { return File.Exists(full) ? File.ReadAllText(full) : null; }
        catch (Exception e) { Debug.LogWarning("[B110W3] 读源码失败：" + full + "：" + e.Message); return null; }
    }

    /// <summary>去 `//` 行注释（够用：本件要扫的两个方法体里没有含 `//` 的字符串字面量）。
    /// <para>🔴 为什么要去注释：本仓踩过「代码被追加到行末 `//` 之后 ⇒ 看着接上了、断言一条没跑」
    /// （`资料/完全复刻/报告/B102_W1_断言接线被注释吞掉.md`）⇒ 源码类判据一律先去注释。</para></summary>
    static string StripComments(string src)
    {
        if (src == null) return null;
        var sb = new StringBuilder(src.Length);
        foreach (var raw in src.Split('\n'))
        {
            string line = raw;
            int ix = line.IndexOf("//", StringComparison.Ordinal);
            if (ix >= 0) line = line.Substring(0, ix);
            sb.Append(line).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>去注释后按签名定位方法体（花括号配对；找不到返回 null）。</summary>
    static string MethodBodyNoComments(string src, string signature)
    {
        string clean = StripComments(src);
        if (clean == null || signature == null) return null;
        int i = clean.IndexOf(signature, StringComparison.Ordinal);
        if (i < 0) return null;
        int b = clean.IndexOf('{', i + signature.Length);
        if (b < 0) return null;
        int depth = 0;
        for (int k = b; k < clean.Length; k++)
        {
            char ch = clean[k];
            if (ch == '{') depth++;
            else if (ch == '}')
            {
                depth--;
                if (depth == 0) return clean.Substring(b, k - b + 1);
            }
        }
        return null;
    }

    /// <summary>数 `needle` 在 `hay` 里出现几次（Ordinal）。</summary>
    static int CountOf(string hay, string needle)
    {
        if (string.IsNullOrEmpty(hay) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while (true)
        {
            int k = hay.IndexOf(needle, i, StringComparison.Ordinal);
            if (k < 0) break;
            n++; i = k + needle.Length;
        }
        return n;
    }
}
