// B142_E66_Assert.cs — 第一百四十二会话 · 执行代理 `B142-E66`（两件：成就 `highestValue` 第二格 · 锻造指针 offset = 点击位置）
//
// ============================ 它盯的是什么 ============================
//  ① **`个人资料#L37`**：原版 `AchievementsManager.GetSave(ach)` 返回的 `AchievementPersistence` 有
//     **`currentValue` + `highestValue` 两格**，我们原来只有一格（`_achProgress`）。本笔补第二格 + 写点 + 读口 + 存档兼容。
//  ② **`教程#L98`**：原版 `TutorialStageClaimForgeFirstLevel__DoMenuActions.c:148-150` 的 `AddPointer(item,
//     GetMouseClickPosition(), filterRaycast:1)` —— 指针 offset 是**运行时鼠标点击位置**，我们原来取常量 (0,0)。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//  · `d:/2/tools/decomp_full/AchievementsManager__GetHighestValue.c` = `Math.Max(<0x3c>, <0x38>, 0)`
//    （`set_CurrentValue.c` 证实 `CurrentValue` 就在 **0x3c**，⇒ 0x38 = `highestValue`）；
//    `AchievementsManager__GetSave.c` = 查不到就 `new AchievementPersistence` 再塞回字典。
//  · `d:/2/tools/decomp_full/TutorialStageClaimForgeFirstLevel__DoMenuActions.c:148-150`；
//    `TutorialManager__AddPointerAction.c` = `set_parentInternal(target)` + `set_localPosition(position)`（**不保持世界位置**）。
//  · 施工判据 → `资料/完全复刻/判据/B142_R18_CHK05块6.md` §2·2 / §2·3；本件报告 → `资料/完全复刻/报告/B142_E66.md`
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A6：把 `RaiseAchievementHighest` 删掉（highest 恒 0）⇒ current 调到 3 之后读出来是 3 ⇒ A6 红。
//  · A6（第二面）：把 `AchievementHighestValue` 改成直接返回 `AchievementValue`（highest = current 的别名）⇒ 同上红。
//  · B4：把 `LoadFromDisk` 里「旧档缺 `achHighest` ⇒ 用 `achValues` 初始化」那一支删掉（落 0）
//        ⇒ current 调到 4 之后 highest 读出来是 4 ⇒ B4 红。
//  · C9：把 `SaveNow` 里 `_save.achHighest = …` 删掉（只写内存不落盘）⇒ 重读后把 current 调到 2，
//        highest 读出来是 2 ⇒ C9 红。
//  · D5/D6：把 `DeepPointerOffsetFor` 的判据放宽成「所有阶段都吃点击位置」⇒ 对照组（战役阶段）返回点击数 ⇒ 红。
//  · D2：删掉「没有点击位置 ⇒ 退回 (0,0)」那一支（改为直接 `_lastClickPx`）也能过 —— 但 D3 会红（没出声）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `Assets/CardPresentation/Editor/MainMenuScene.cs` 的 `MainMenuScene.Run()` —— 在**现有**某条
//  `Check(...)` / `_sink.Pass += …` 的**同一物理行**后面追加（零行数变化）：
//      B142_E66_Assert.Run(); _pass += B142_E66_Assert.Pass; _fail += B142_E66_Assert.Fail;
//  ⚠️ 本件**不建 GameObject、不要场景**；`MenuTutorialFlow` 那几条只调纯静态函数（不 Attach、不写存档、不跑 Unity 批处理）。
//  ⚠️ 存档两段（A/B/C）把 `ProfileData.SavePathOverrideForTest` + `SaveCenter.OverrideRoot` **都**指到临时目录
//     ⇒ 中央档 / 旧档一个字节都不碰玩家真档；收尾还原两个口、把测试用的合成成就 id 归 0。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B142-E66`：`AchievementPersistence.highestValue` 第二格 + 锻造首级领取阶段指针 offset = 点击位置。</summary>
public static class B142_E66_Assert
{
    static readonly CheckSink _sink = new CheckSink("[E66] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // 合成的成就 id（⛔ 不与真实 102 条撞 —— `AchievementById` 查不到 ⇒ 里程碑提示那条路只会静默返回）
    const string IdHighest = "Ach_E66_HIGHEST";
    const string IdLegacy = "Ach_E66_LEGACY";
    const string IdRoundTrip = "Ach_E66_ROUNDTRIP";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B142-E66 成就 `highestValue` 第二格（只涨不落 · 旧档迁移）· 锻造指针 offset = 点击位置");

        string tmp = null;
        string oldPath = ProfileData.SavePathOverrideForTest;
        string oldRoot = SaveCenter.OverrideRoot;
        try
        {
            tmp = Path.Combine(Path.GetTempPath(), "wf_e66_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(tmp, "live"));
            Directory.CreateDirectory(Path.Combine(tmp, "legacy"));

            // 逐段 catch：一段抛异常不该让后面几段一条都不跑（同 `RuleEngineTest_EngineX2` 的写法）。
            try { SectionA_HighestOnlyRises(Path.Combine(tmp, "live")); }
            catch (Exception e) { Tr(false, "A 段抛异常（后面的 B 段接着跑）：" + e); }
            try { SectionB_LegacyMigration(Path.Combine(tmp, "legacy")); }
            catch (Exception e) { Tr(false, "B 段抛异常：" + e); }
            try { SectionC_SaveRoundTrip(Path.Combine(tmp, "live")); }
            catch (Exception e) { Tr(false, "C 段抛异常：" + e); }
            try { SectionD_TutorialPointerOffset(); }
            catch (Exception e) { Tr(false, "D 段抛异常：" + e); }
        }
        catch (Exception e)
        {
            Tr(false, "断言宿主抛异常（建临时目录那一句炸了 ⇒ 上面四段**一段都没跑**）：" + e);
        }
        finally
        {
            // 收尾 ①：把三个合成 id 归 0（批处理下 `SaveIfAuto` 不写盘；非批处理也留在临时路径上）。
            try
            {
                ProfileData.SetAchievementValue(IdHighest, 0);
                ProfileData.SetAchievementValue(IdLegacy, 0);
                ProfileData.SetAchievementValue(IdRoundTrip, 0);
            }
            catch (Exception e) { Debug.LogWarning("[E66] 收尾归零失败（不影响断言）：" + e.Message); }
            // 收尾 ②：两个存档口还回去，再删临时目录。
            ProfileData.SavePathOverrideForTest = oldPath;
            SaveCenter.OverrideRoot = oldRoot;
            try { if (tmp != null && Directory.Exists(tmp)) Directory.Delete(tmp, true); }
            catch (Exception e) { Debug.LogWarning("[E66] 临时目录没删干净（不影响断言）：" + e.Message); }
        }

        Debug.Log(_sink.P + "B142E66 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A：highestValue 只涨不落
    static void SectionA_HighestOnlyRises(string dir)
    {
        ProfileData.SavePathOverrideForTest = Path.Combine(dir, "profile.json");
        SaveCenter.OverrideRoot = Path.Combine(dir, "central");
        Directory.CreateDirectory(SaveCenter.OverrideRoot);
        ProfileData.ReloadSaveForTest();                       // 起点 = 空（临时目录里什么都没有）

        C(ProfileData.AchievementValue(IdHighest), 0, "A0 前提：临时档里这条成就一条记录都没有（起点 0）");

        ProfileData.SetAchievementValue(IdHighest, 7);
        C(ProfileData.AchievementValue(IdHighest), 7, "A1 `SetAchievementValue(7)` ⇒ current = 7");
        C(ProfileData.AchievementHighestValue(IdHighest), 7, "A2 `highestValue` 同拍抬到 7");

        ProfileData.AddAchievementValue(IdHighest, 5);
        C(ProfileData.AchievementValue(IdHighest), 12, "A3 累加 5 ⇒ current = 12（`AddAchievementValue` 是累加不是覆盖）");
        C(ProfileData.AchievementHighestValue(IdHighest), 12, "A4 `highestValue` = 12");

        ProfileData.SetAchievementValue(IdHighest, 3);
        C(ProfileData.AchievementValue(IdHighest), 3, "A5 `SetAchievementValue(3)` ⇒ current 掉到 3");
        C(ProfileData.AchievementHighestValue(IdHighest), 12,
          "A6 ★ 灭自证：`highestValue` **仍是 12**（只涨不落；若 highest 只是 current 的别名 / 从没被写 ⇒ 这里是 3）");

        ProfileData.SetAchievementValueAtLeast(IdHighest, 5);
        C(ProfileData.AchievementValue(IdHighest), 5, "A7 `SetAchievementValueAtLeast(5)` ⇒ current = 5（原版那条「抬到至少」）");
        C(ProfileData.AchievementHighestValue(IdHighest), 12, "A8 抬 current 不改变历史最高（仍 12）");

        ProfileData.SetAchievementValueAtLeast(IdHighest, 20);
        C(ProfileData.AchievementValue(IdHighest), 20, "A9 `SetAchievementValueAtLeast(20)` ⇒ current = 20");
        C(ProfileData.AchievementHighestValue(IdHighest), 20, "A10 `highestValue` = 20（新的历史最高）");

        var pv = ProfileData.AchievementPersistenceOf(IdHighest);
        C(pv.currentValue, 20, "A11 `GetSave` 等价物：两格一起返回 —— `currentValue`");
        C(pv.highestValue, 20, "A12 … `highestValue`");
    }

    // ============================================================ B：旧档（没有 `achHighest` 键）迁移
    static void SectionB_LegacyMigration(string dir)
    {
        string path = Path.Combine(dir, "profile.json");
        ProfileData.SavePathOverrideForTest = path;
        SaveCenter.OverrideRoot = Path.Combine(dir, "central");   // 空目录 ⇒ 中央档不存在 ⇒ 读旧档
        Directory.CreateDirectory(SaveCenter.OverrideRoot);

        // 旧档 = **本笔之前**写下的那一份形状：有 `achIds`/`achValues`、**没有** `achHighest`。
        File.WriteAllText(path,
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"achIds\":[\"" + IdLegacy + "\"],\"achValues\":[9]}");

        ProfileData.ReloadSaveForTest();
        C(ProfileData.AchievementValue(IdLegacy), 9, "B1 旧档读回来：current = 9");
        C(ProfileData.AchievementHighestValue(IdLegacy), 9,
          "B2 ★ 旧档没有 `achHighest` 键 ⇒ 用 `currentValue` 初始化成 9（⛔ 不是 0）");

        ProfileData.SetAchievementValue(IdLegacy, 4);
        C(ProfileData.AchievementValue(IdLegacy), 4, "B3 current 调到 4");
        C(ProfileData.AchievementHighestValue(IdLegacy), 9,
          "B4 ★ 灭自证：迁移**真的发生了** —— highest 仍是 9；若「缺键 ⇒ 按 current 初始化」没做（落 0）⇒ 这里读出来是 4");
    }

    // ============================================================ C：写盘 → 重读（新字段真的进了保存形状）
    static void SectionC_SaveRoundTrip(string dir)
    {
        string path = Path.Combine(dir, "profile.json");
        ProfileData.SavePathOverrideForTest = path;
        SaveCenter.OverrideRoot = Path.Combine(dir, "central");
        Directory.CreateDirectory(SaveCenter.OverrideRoot);
        ProfileData.ReloadSaveForTest();

        ProfileData.SetAchievementValue(IdRoundTrip, 11);
        ProfileData.SaveNowForTest();

        string text = SaveCenter.ReadThroughLegacy(SaveKeys.CosmeticData, path);
        Tr(text != null, "C1 写盘落到了中央档（`OverrideRoot` 下的 `CosmeticData`；🔴 2026-11-03 起 `SaveCenter.LegacyMirrorOn` 默认 **false** ⇒ `profile.json` 不再双写，读口走 `SaveCenter.ReadThroughLegacy`）");
        Tr(text != null && text.Contains("\"achHighest\""), "C2 ★ payload 里有 `achHighest` 键（新字段真的进了保存形状，⛔ 不是只在内存里）");

        var dto = JsonUtility.FromJson<ProfileData.SaveDto>(text ?? "");
        int idx = -1;
        if (dto != null && dto.achIds != null)
            for (int i = 0; i < dto.achIds.Length; i++) if (dto.achIds[i] == IdRoundTrip) { idx = i; break; }
        Tr(idx >= 0, "C3 前提：这条成就在 payload 的 `achIds` 里");
        if (idx >= 0)
        {
            C(dto.achValues != null && dto.achValues.Length > idx ? dto.achValues[idx] : -1, 11,
              "C4 `achValues` 那一格 = 11（原有字段没被新字段挤掉）");
            bool aligned = dto.achHighest != null && dto.achHighest.Length > idx;
            Tr(aligned, "C5 `achHighest` 与 `achIds` 下标的对齐在（长度够、能按下标取到）");
            C(aligned ? dto.achHighest[idx] : -1, 11, "C6 `achHighest` 那一格 = 11");
        }

        ProfileData.ReloadSaveForTest();
        C(ProfileData.AchievementValue(IdRoundTrip), 11, "C7 从盘上重读：current 还是 11");
        C(ProfileData.AchievementHighestValue(IdRoundTrip), 11, "C8 从盘上重读：highest 还是 11（跨档存活）");

        ProfileData.SetAchievementValue(IdRoundTrip, 2);
        C(ProfileData.AchievementHighestValue(IdRoundTrip), 11,
          "C9 ★ 灭自证：读回来的是**真的 highest**，不是「拿 current 兜的」—— 把 current 调到 2，highest 仍是 11");
    }

    // ============================================================ D：锻造首级领取的指针 offset = 点击位置
    static void SectionD_TutorialPointerOffset()
    {
        MenuTutorialStageDef forge = null, campaign = null;
        for (int i = 0; i < MenuTutorialStages.Count; i++)
        {
            var d = MenuTutorialStages.At(i);
            if (d == null) continue;
            if (d.Id == MenuTutorialStageId.ClaimForgeFirstLevel) forge = d;
            if (d.Id == MenuTutorialStageId.GetCampaignNode) campaign = d;
        }
        Tr(forge != null && forge.DeepTarget == MenuTutorialTargetKind.ForgeClaimButton,
           "D0 前提：阶段表里有 `ClaimForgeFirstLevel`，且它的**深目标**就是锻造领取钮");
        Tr(campaign != null && campaign.DeepTarget == MenuTutorialTargetKind.CampaignClaimButton,
           "D0b 前提：对照组 = `GetCampaignNode`（深目标是战役领取钮，阶段表的 offset 是常量）");

        MenuTutorialFlow.ClearClickPositionForTest();
        Tr(!MenuTutorialFlow.HasLastClickPosition, "D1 起点：这一趟没记到过点击位置");
        C(MenuTutorialFlow.DeepPointerOffsetFor(forge), Vector2.zero,
          "D2 没有点击位置 ⇒ offset 退回 (0,0)（原版 `GetMouseClickPosition()` 无点击时也是零）");
        Tr(MenuTutorialFlow.MissingClickPositionWarned, "D3 ★ 出声一次（⛔ 不静默退回）");

        var click = new Vector2(641f, 359f);
        MenuTutorialFlow.SetClickPositionForTest(click);
        C(MenuTutorialFlow.DeepPointerOffsetFor(forge), click,
          "D4 ★ 有点击位置 ⇒ 锻造阶段的指针 offset **就是点击点**（`TutorialStageClaimForgeFirstLevel__DoMenuActions.c:148-150`）");
        C(MenuTutorialFlow.DeepPointerOffsetFor(campaign), campaign != null ? campaign.DeepOffset : Vector2.zero,
          "D5 ★ 灭自证：别的阶段**不吃**点击位置、仍取阶段表的 `DeepOffset`（若把这条推广到全阶段 ⇒ 这里会变成点击点）");
        Tr(campaign == null || MenuTutorialFlow.DeepPointerOffsetFor(campaign) != click,
          "D6 同上：对照组返回的值确实不是那个点击点");

        C(MenuTutorialFlow.PxPlusOffset(new Vector2(100f, 200f), click), new Vector2(741f, -159f),
          "D7 消费口：指针指尖 = 目标中心 + offset，且 **y 取反**（原版 offset 是目标本地 y 向上，我们的设计像素 y 向下）");

        MenuTutorialFlow.ClearClickPositionForTest();     // 收尾：别把注入值留给后面的自检
    }
}
