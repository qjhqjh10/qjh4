// B94Y6_Assert.cs — 执行代理 Y6（第九十四会话 · 波 1.5）落地的四件活的断言
//
// ============================ 它盯的是什么（判据 → 报告，不在这里抄第二份）============================
//  · 任务 1：`InboxData.Title/Body` 的**运行时**三次占位符替换（`[[RewardName]]` / `[[PlayerName]]` /
//            `[[AllianceName]]`）——真体 = `out_b94/BaseInboxEvent_object__object_.{GetTitle__1589550,GetMessage__15892b0}.c`
//  · 任务 2：`ConfigProvider<T>` 的 `Initialize` / `UnpackInternal` / `OnDestroy` / `Key`（纯本地，非服务端）——
//            真体 = `out_b94/ConfigProvider_object_.*.c`
//  · 任务 3：`GameNewsEvent.SelectEvent` 的本地三句（本件不直接跑它 —— 需一个具体事件实例 + SaveGame，
//            成本高；由报告 §断言清单 说明为何不做运行期断言）
//  · 任务 4：`NetRuntime.ComputeDraftMatchOptions`（原版 `DraftEventBase.SetCustomMatchOptions` 的纯函数等价）——
//            真体 = `out_b94/DraftEventBase_object__object_.SetCustomMatchOptions__1813cb0.c`
//
// ============================ 怎么用（宿主）============================
// `B94Y6_Assert.Run();`
//   · **不跑 Unity 批处理**、不建持久资产；建的临时组件/结点在收尾 `DestroyImmediate`。
//   · 建议宿主：**`Editor/ShellScene.cs`**（收件箱 + 配置 + 联机都在这条线上）。
//     ⚠️ 本文件**还没接到宿主里**（本批只到 `Shell/*.cs` + 本件）⇒ 由调度台在宿主 `Run` 里加两行：
//       `B94Y6_Assert.Run();`
//       `_sink.Pass += B94Y6_Assert.Pass; _sink.Fail += B94Y6_Assert.Fail;`
//
// ============================ 🔴 灭自证 ============================
//   · 任务 1：每条**先断前提**（原串里确有 `[[…]]` token），再断结论（token 消失 + 替换成期望值）；
//            「无奖励 ⇒ `[[RewardName]]` 变空串」是**字面量**断言（"X[[RewardName]]Y" ⇒ "XY"）。
//   · 任务 2：`Initialize` 命中档断 `value == 42`（json 里写死 42）· 未命中档断 `value == 0` 且**缓存没写**；
//            `Key` 断字面量串；`OnDestroy` 用反射直调后断缓存被清。
//   · 任务 4：`ComputeDraftMatchOptions` 断四个边界的**字面量**结果（800 / 801 / won 2/3）。
//   ⛔ **期望值不从被测实现里读**（42 / 0 / 800 / "C1".."C5" / "XY" 全是字面量）。
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 Y6：B94 波 1.5 四件活的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B94Y6_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94Y6] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "Y6 · B94 波 1.5：收件箱占位符 / ConfigProvider / Draft 匹配参数");

        SectionA_收件箱占位符();
        SectionB_ConfigProvider();
        SectionC_Draft匹配参数();

        Debug.Log(_sink.P + "B94 Y6 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 收件箱运行时占位符替换（任务 1）
    static void SectionA_收件箱占位符()
    {
        MenuCheck.Section(_sink, "A 收件箱 Title/Body 的运行时三次 Replace（真体 BaseInboxEvent.GetTitle/GetMessage）");

        const string campaignArt = "40K_genearl_icon_Campaign_points_big";   // 战役点（RewardDisplayName 命中官方词条）

        // ---- 前提：一条带三 token 的标题 + 一件战役点奖励 ----
        var m = new InboxData.Msg
        {
            Id = "b94y6_title",
            TitleText = "P[[PlayerName]]A[[AllianceName]]R[[RewardName]]Z",
            BodyText = "p[[PlayerName]]a[[AllianceName]]r[[RewardName]]z",
            Rewards = new[] { new InboxData.Reward(campaignArt, 1) },
        };
        Tr(m.TitleText.Contains("[[PlayerName]]") && m.TitleText.Contains("[[AllianceName]]")
           && m.TitleText.Contains("[[RewardName]]"), "A0 前提：标题串里确有那三个 token（否则 A1..A4 都是空转绿）");

        string pn = ProfileData.PlayerName;         // 只读，⛔ 不改（setter 会 SaveIfAuto）
        string oldAlliance = SocialData.AllianceName;
        SocialData.AllianceName = "AllianceY6";     // 纯静态字段（无副作用）⇒ 可自由设/还

        string title = InboxData.Title(m);
        string body = InboxData.Body(m);

        Tr(!title.Contains("[["), "A1 标题里**不再有** `[[…]]` token（真体三跳 Replace 全跑了）");
        Tr(!body.Contains("[["), "A1b 正文里**不再有** `[[…]]` token（同构，取 content.message）");
        Tr(title.Contains("AllianceY6"), "A2 `[[AllianceName]]` ⇒ `SocialData.AllianceName`（字面量 AllianceY6）");
        Tr(body.Contains("AllianceY6"), "A2b 正文同 A2");
        if (!string.IsNullOrEmpty(pn))
            Tr(title.Contains(pn), "A3 `[[PlayerName]]` ⇒ `ProfileData.PlayerName`（原版 `PlayerDataManager.playerName`）");
        else
            Tr(true, "A3 跳过「PlayerName 命中」：当前 `ProfileData.PlayerName` 为空串（仍由 A1 证明 token 被替换掉）");
        string rewardName = Loc.T("Rewards Menu/CampaignPoints");
        Tr(!string.IsNullOrEmpty(rewardName) && title.Contains(rewardName),
           "A4 `[[RewardName]]` ⇒ 战役点官方词条（`Rewards Menu/CampaignPoints`）；非空且命中");

        // ---- 无奖励 ⇒ `[[RewardName]]` 变空串（字面量）----
        var noReward = new InboxData.Msg { Id = "b94y6_noreward", TitleText = "X[[RewardName]]Y", Rewards = null };
        Tr(noReward.TitleText.Contains("[[RewardName]]"), "A5 前提：无奖励那条标题里确有 token");
        C(InboxData.Title(noReward), "XY", "A6 无奖励 ⇒ `[[RewardName]]` 替换成空串（原版 `newValue` 兜底空串）");

        // ---- 无 token ⇒ 原样返回（等价性）----
        var plain = new InboxData.Msg { Id = "b94y6_plain", TitleText = "NoTokensHere" };
        C(InboxData.Title(plain), "NoTokensHere", "A7 不含 token 的标题原样返回（三跳 Replace 无命中 ⇒ 不变）");

        SocialData.AllianceName = oldAlliance;      // 还原
    }

    // ------------------------------------------------------------ B ConfigProvider（任务 2）
    static void SectionB_ConfigProvider()
    {
        MenuCheck.Section(_sink, "B ConfigProvider<T>：Initialize / UnpackInternal / Key / OnDestroy（真体 = 纯本地）");

        GameObject go = null;
        try
        {
            ConfigCache.ResetForTest();
            go = new GameObject("b94y6_cfg_host");
            var provider = go.AddComponent<TestCfgProvider>();

            // ---- 前提：Key = new T().ConfigKey（原版 get_Key）----
            C(provider.Key, "B94Y6.TestCfg", "B0 `ConfigProvider.Key` = `new T().ConfigKey`（字面量键）");
            C(provider.ConfigType, typeof(B94Y6TestCfg), "B0b `ConfigType` = typeof(T)");

            // ---- 命中档：字典里有这个键 + json value=42 ----
            var cfgJson = JsonUtility.ToJson(new B94Y6TestCfg { value = 42 });
            var dict = new Dictionary<string, string> { { "B94Y6.TestCfg", cfgJson } };
            provider.Initialize(dict);
            var cfg = provider.Config as B94Y6TestCfg;
            Tr(cfg != null, "B1 命中档：`Config` 是 `B94Y6TestCfg` 实例（`config = UnpackInternal(...)`）");
            if (cfg != null) C(cfg.value, 42, "B2 命中档：反序列化覆盖 ⇒ `value == 42`（原版 `DeserializeObjectWithType<T>`）");
            var cached = ConfigCache.Get<B94Y6TestCfg>();
            Tr(cached != null, "B3 命中档：缓存被写（原版 `UnpackInternal` 尾部 `<cache>.Write(1)`）");
            if (cached != null) C(cached.value, 42, "B3b 缓存里那份就是刚解出来的（value == 42）");

            // ---- 未命中档：字典里没有键 ⇒ 默认实例 + 打日志 + 缓存不写 ----
            ConfigCache.ResetForTest();
            var empty = new Dictionary<string, string> { { "Other.Key", "{\"value\":99}" } };
            provider.Initialize(empty);
            var def = provider.Config as B94Y6TestCfg;
            Tr(def != null, "B4 未命中档：仍是 `new T()` 的默认实例（原版先 new 再判）");
            if (def != null) C(def.value, 0, "B5 未命中档：默认实例 `value == 0`（未反序列化）");
            Tr(ConfigCache.Get<B94Y6TestCfg>() == null, "B6 未命中档：**缓存没写**（原版只有命中支才 Write）");

            // ---- OnDestroy：config != null ⇒ 清缓存（反射直调 protected virtual）----
            ConfigCache.ResetForTest();
            provider.Initialize(dict);                        // 先让缓存有货
            Tr(ConfigCache.Get<B94Y6TestCfg>() != null, "B7 前提：OnDestroy 前缓存里确实有一份");
            var mi = typeof(ConfigProvider<B94Y6TestCfg>).GetMethod("OnDestroy",
                        BindingFlags.Instance | BindingFlags.NonPublic);
            Tr(mi != null, "B8 前提：`OnDestroy` 是 `ConfigProvider<T>` 上的 protected virtual（反射找得到）");
            if (mi != null) mi.Invoke(provider, null);
            Tr(ConfigCache.Get<B94Y6TestCfg>() == null, "B9 `OnDestroy`（config != null）⇒ 缓存被清（原版 `<cache>.Clear(2)`）");
        }
        catch (System.Exception ex)
        {
            Tr(false, "B* 建 `TestCfgProvider` 或调 ConfigProvider 时抛了异常：" + ex.GetType().Name + "：" + ex.Message);
        }
        finally
        {
            ConfigCache.ResetForTest();
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------ C Draft 匹配参数（任务 4）
    static void SectionC_Draft匹配参数()
    {
        MenuCheck.Section(_sink, "C DraftEventBase.SetCustomMatchOptions 的纯函数等价（原版 · 轮抽房间属性）");

        // 边界 1：ELO 820 > 800 ⇒ 不清零
        var a = NetRuntime.ComputeDraftMatchOptions(820, 5, 2);
        C(a.Won, 5, "C1 胜场原样（= SaveGame.numberOfMatchesWon）");
        C(a.Elo, 820, "C2 ELO 820 > 800 ⇒ **不清零**");
        C(a.LossStreak, 2, "C3 连败原样（= GameModes.LossStreak）");

        // 边界 2：ELO 800（= 阈值）且 won 2 < 3 ⇒ 清零
        var b = NetRuntime.ComputeDraftMatchOptions(800, 2, 0);
        C(b.Elo, 0, "C4 ELO == 800（阈值，`<=`）且胜场 2 < 3 ⇒ **清零**");

        // 边界 3：ELO 500 但 won 3（不 < 3）⇒ 不清零
        var c = NetRuntime.ComputeDraftMatchOptions(500, 3, 0);
        C(c.Elo, 500, "C5 ELO 500 但胜场 3（不 < 3）⇒ **不清零**（两条同时成立才清）");

        // 边界 4：阈值常量 = 800（原版 GameStaticData.newbieELO）
        C(NetRuntime.DraftNewbieElo, 800, "C6 新手 ELO 阈值 = 800（原版 `GameStaticData.newbieELO` 实读）");

        // lobby 键表 = 恰好 5 个，逐字 C1..C5
        C(a.LobbyKeys != null ? a.LobbyKeys.Length : -1, 5, "C7 lobby 键表恰好 5 个键");
        if (a.LobbyKeys != null && a.LobbyKeys.Length == 5)
        {
            Tr(a.LobbyKeys[0] == "C1" && a.LobbyKeys[1] == "C2" && a.LobbyKeys[2] == "C3"
               && a.LobbyKeys[3] == "C4" && a.LobbyKeys[4] == "C5", "C8 lobby 键表逐字 = C1/C2/C3/C4/C5（真体 5 键数组）");
        }
    }

    // ============================================================ 测试用具体类型（嵌套 ⇒ 无脚本资产，AddComponent 可挂）
    /// <summary>`[Serializable]` 的测试配置（`JsonUtility.FromJson` 要求公开字段）。</summary>
    [System.Serializable]
    public class B94Y6TestCfg : IConfigData
    {
        public int value;
        public string ConfigKey { get { return "B94Y6.TestCfg"; } }
    }

    /// <summary>`ConfigProvider<T>` 的最小具体子类（只为自检 `AddComponent`）。</summary>
    public class TestCfgProvider : ConfigProvider<B94Y6TestCfg> { }
}
