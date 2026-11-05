// B145_E21_Assert.cs — 第一百三十一会话 · 执行代理 `B145-E21`（`achHighest` 进存档 payload 字段闸 + 补缺迁移）
//
// ============================ 它盯的是什么 ============================
//  `Shell/LegacySaveReader.ProfileFields` 是「payload 少一个键 ⇒ **不许写盘**」的字段闸，也是
//  「导入时中央档缺不缺字段」的判据。`SaveDto` 自 2026-11-04（`B142_E66`）起有 `achHighest`，
//  但字段闸的清单里**没有它** ⇒ ① 它被谁删掉都不会被闸拦住；② `ImportProfile` 的补缺迁移
//  （`TryFillMissingProfileFields` 按 `LegacyProfileFields` 重建 payload）会把旧档的 `achHighest` **丢掉**。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//  · `d:/2/tools/decomp_full/AchievementsManager__GetHighestValue.c:15-20` =
//    `System_Math__Max(*(lVar3+0x3c), *(lVar3+0x38), 0)` —— current 与 highest 两格取大、下限 0。
//  · `d:/2/tools/decomp_full/AchievementsManager__GetSave.c` = 查不到就 `new AchievementPersistence` 再塞回字典。
//  · 我们的落点：`ProfileData.SaveDto.achHighest`（`ProfileData.cs:996`）· 读口兜底 `LoadFromDisk`（`:1076`）
//    · 写口 `SaveNow`（`:1104`）· 投影 `BuildAchievementsPayload`（`LegacySaveReader.cs:656`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A2：把 `ProfileFields` 里的 `"achHighest"` 删掉 ⇒ `MissingProfileFields` 对缺键 payload 返回 null ⇒ A2/A3 红。
//  · B2：把 `LegacyProfileFields.achHighest` 删掉 ⇒ 反射拿不到字段 ⇒ B2 红。
//  · C3/C4：把迁移里新增的 `if (!HasTopField(flat,"achHighest")) …` 删掉 ⇒ 重建出来的 payload 没有该键
//          ⇒ `TryFillMissingProfileFields` 报「补完还是缺」⇒ 中央档不被改写 ⇒ C3/C4 读回旧值（缺键）⇒ 红。
//  · D3：把兜底写成 `new int[...]`（全 0）或 `h = 0` ⇒ D3 读回 0（current 是 9）⇒ 红。
//  · E3：把 `outp[i] = h < v ? v : h` 写成 `outp[i] = h` ⇒ 旧档 7 < current 9 时写入 7 ⇒ E3 读回 7 ⇒ 红。
//  · C7：把 `LegacyProfileFields.equippedBorderIndex` 删掉 ⇒ 迁移重建的 payload 里该键消失 ⇒ C7 读回 0 ⇒ 红。
//  · F3：把 `"achHighest"` 加进 `ProfileFields` 但 `SaveDto` 改名 ⇒ F3 那条逐键反射红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `Assets/CardPresentation/Editor/MainMenuScene.cs` 的 `MainMenuScene.Run()` —— 在 `B142_E66_Assert.Run();`
//  那一串**同一物理行**、**行尾注释之前**追加（零行数变化）：
//      B145_E21_Assert.Run(); _sink.Pass += B145_E21_Assert.Pass; _sink.Fail += B145_E21_Assert.Fail;
//  ⚠️ 本件**不建 GameObject、不要场景、不跑 Unity 批处理**；存档两段（C/D/E）把
//     `SaveCenter.OverrideRoot` + `LegacySaveReader.LegacyRootOverride` **都**指到临时目录，
//     收尾还原两个口并删临时目录 ⇒ 玩家真档（`%LocalLow%/QJHWarpforge/…` 与 `C:\warpforgesave`）一个字节都不碰。
using System;
using System.IO;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B145-E21`：`achHighest` 进 `LegacySaveReader.ProfileFields` 字段闸 + 补缺迁移不丢列。</summary>
public static class B145_E21_Assert
{
    static readonly CheckSink _sink = new CheckSink("[E21] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // 合成成就 id（⛔ 不与真实 102 条撞；只活在临时目录里，导入完随目录删掉）
    const string IdMigrate = "Ach_E21_MIGRATE";
    const string IdFallback = "Ach_E21_FALLBACK";
    const string IdClamp = "Ach_E21_CLAMP";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B145-E21 `achHighest` 进存档字段闸 + 补缺迁移（旧档缺列 / 短列 / 高于 current）");

        string tmp = null;
        string oldLegacy = LegacySaveReader.LegacyRootOverride;
        string oldRoot = SaveCenter.OverrideRoot;
        try
        {
            tmp = Path.Combine(Path.GetTempPath(), "wf_e21_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            // 逐段 catch：一段抛异常不该让后面几段一条都不跑（同 `B142_E66_Assert` 的写法）。
            try { SectionA_GateListAndMissing(); }
            catch (Exception e) { Tr(false, "A 段抛异常（后面的 B 段接着跑）：" + e); }
            try { SectionB_LegacyDtoHasColumn(); }
            catch (Exception e) { Tr(false, "B 段抛异常：" + e); }
            try { SectionC_MigrationKeepsLegacyHighest(Path.Combine(tmp, "c")); }
            catch (Exception e) { Tr(false, "C 段抛异常：" + e); }
            try { SectionD_MigrationFallbackToCurrent(Path.Combine(tmp, "d")); }
            catch (Exception e) { Tr(false, "D 段抛异常：" + e); }
            try { SectionE_MigrationNeverLowersHighest(Path.Combine(tmp, "e")); }
            catch (Exception e) { Tr(false, "E 段抛异常：" + e); }
            try { SectionF_FieldGateCoversSaveDto(); }
            catch (Exception e) { Tr(false, "F 段抛异常：" + e); }
        }
        catch (Exception e)
        {
            Tr(false, "断言宿主抛异常（建临时目录那一句炸了 ⇒ 上面六段**一段都没跑**）：" + e);
        }
        finally
        {
            // 两个存档口还回去，再删临时目录（⛔ 玩家真档一个字节都没碰）
            LegacySaveReader.LegacyRootOverride = oldLegacy;
            SaveCenter.OverrideRoot = oldRoot;
            try { if (tmp != null && Directory.Exists(tmp)) Directory.Delete(tmp, true); }
            catch (Exception e) { Debug.LogWarning("[E21] 临时目录没删干净（不影响断言）：" + e.Message); }
        }

        Debug.Log(_sink.P + "B145E21 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A：字段闸的清单 + 「缺键」判定
    static void SectionA_GateListAndMissing()
    {
        int iIds = Array.IndexOf(LegacySaveReader.ProfileFields, "achIds");
        int iVals = Array.IndexOf(LegacySaveReader.ProfileFields, "achValues");
        int iHigh = Array.IndexOf(LegacySaveReader.ProfileFields, "achHighest");
        Tr(iHigh >= 0, "A1 ★ `ProfileFields` 含 `achHighest`（字段闸从此会拦住它的删除/改名）");
        C(LegacySaveReader.ProfileFields == null ? -1 : LegacySaveReader.ProfileFields.Length, 8,
          "A2 清单是 8 个顶层键（`ver` + 7 个真数据）");
        Tr(iIds >= 0 && iVals > iIds && iHigh == iVals + 1,
           "A3 `achHighest` 排在 `achValues` 之后（同族位置：三个成就列连在一起、⛔ 别乱序）");

        const string full =
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"achIds\":[\"A\"],\"achValues\":[3],\"achHighest\":[3]}";
        C(LegacySaveReader.MissingProfileFields(full), null, "A4 8 个键齐全 ⇒ 返回 null（允许写盘）");

        const string noHigh =
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"achIds\":[\"A\"],\"achValues\":[3]}";
        C(LegacySaveReader.MissingProfileFields(noHigh), "`achHighest`",
          "A5 ★ 缺 `achHighest` ⇒ 点名报缺（⛔ 不是 null、不是把缺键当合法）");

        // 边界：`JsonUtility` 对空数组序列化成 `[]`，摊平后是「键 = []」这一条 ⇒ 必须算「有」（否则新档永远写不出去）
        const string emptyHigh =
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"achIds\":[],\"achValues\":[],\"achHighest\":[]}";
        C(LegacySaveReader.MissingProfileFields(emptyHigh), null,
          "A6 空数组 `\"achHighest\":[]` 也算「有该键」（`HasTopField` 的空数组分支；否则 `SaveNow` 恒被闸挡）");
    }

    // ============================================================ B：补缺 DTO 也有这一列（反射，因为它是 internal）
    static void SectionB_LegacyDtoHasColumn()
    {
        Type t = typeof(LegacySaveReader).Assembly.GetType("CardPresentation.LegacyProfileFields", false);
        Tr(t != null, "B1 前提：`CardPresentation.LegacyProfileFields` 在运行时程序集里（文件级 internal ⇒ 只能反射）");
        FieldInfo f = t == null ? null : t.GetField("achHighest", BindingFlags.Instance | BindingFlags.Public);
        Tr(f != null, "B2 ★ `LegacyProfileFields` 有 `achHighest` 列（补缺迁移按它重建 payload，缺列就会把这条丢掉）");
        Tr(f != null && f.FieldType == typeof(int[]), "B3 类型是 `int[]`（与 `SaveDto.achHighest` / `achValues` 同型）");
    }

    // ============================================================ C：旧档有这一列 ⇒ 迁移按下标搬过来
    static void SectionC_MigrationKeepsLegacyHighest(string dir)
    {
        // 旧档 = 2026-11-04 之后的形状（有 achHighest）；中央档 = 那之前（缺这一列）
        string merged = RunImport(dir,
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"equippedBorderIndex\":3,\"achIds\":[\"" + IdMigrate + "\"],\"achValues\":[4],\"achHighest\":[12]}",
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"equippedBorderIndex\":3,\"achIds\":[\"" + IdMigrate + "\"],\"achValues\":[4]}",
            "C");

        Tr(merged != null, "C1 前提：迁移之后中央档读得出来（`GetRaw` 非 null）");
        C(merged == null ? "读不出来" : LegacySaveReader.MissingProfileFields(merged), null,
          "C2 ★ 迁移后的 payload 过字段闸（缺列不再导致「补完还是缺」）");

        ProfileData.SaveDto dto = Parse(merged);
        Tr(dto != null, "C3 前提：迁移后的 payload 能反序列化成 `SaveDto`");
        C(Get(dto, IdMigrate, false), 4, "C4 `achValues` 仍是 4（原有列没被改动）");
        C(Get(dto, IdMigrate, true), 12,
          "C5 ★ 迁移把旧档的 `achHighest` = 12 搬了过来（⛔ 不是被 current = 4 覆盖、⛔ 不是 0）");

        string legacyPath = Path.Combine(dir, "legacy", "WarpforgeProfile", "profile.json");
        string legacyNow = File.Exists(legacyPath) ? File.ReadAllText(legacyPath) : "";
        Tr(legacyNow.Contains("\"achHighest\":[12]"), "C6 旧档原件**一个字节没动**（迁移只写中央档）");
        C(dto == null ? int.MinValue : dto.equippedBorderIndex, 3,
          "C7 ★ 迁移重建 payload 时**没把 `equippedBorderIndex` 丢掉**（`LegacyProfileFields` 必须覆盖 `SaveDto` 全部字段，⛔ 只覆盖闸里的那 8 个）");
    }

    // ============================================================ D：旧档没有这一列 ⇒ 按 max(current,0) 兜底
    static void SectionD_MigrationFallbackToCurrent(string dir)
    {
        string merged = RunImport(dir,
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"achIds\":[\"" + IdFallback + "\"],\"achValues\":[9]}",
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"achIds\":[\"" + IdFallback + "\"],\"achValues\":[9]}",
            "D");

        C(merged == null ? "读不出来" : LegacySaveReader.MissingProfileFields(merged), null,
          "D1 迁移后的 payload 过字段闸");
        ProfileData.SaveDto dto = Parse(merged);
        Tr(dto != null, "D2 前提：迁移后的 payload 能反序列化成 `SaveDto`");
        C(Get(dto, IdFallback, true), 9,
          "D3 ★ 旧档**没有**这一列 ⇒ 按 `max(current,0)` = 9 兜底（⛔ 写 0 会把历史最高清零，这里会读回 0）");
    }

    // ============================================================ E：旧档这一列低于 current ⇒ 只涨不落
    static void SectionE_MigrationNeverLowersHighest(string dir)
    {
        string merged = RunImport(dir,
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"achIds\":[\"" + IdClamp + "\"],\"achValues\":[9],\"achHighest\":[7]}",
            "{\"ver\":1,\"playerName\":\"\",\"avatarIndex\":0,\"titleIndex\":-1,\"avatarBorderOn\":true,"
          + "\"achIds\":[\"" + IdClamp + "\"],\"achValues\":[9]}",
            "E");

        C(merged == null ? "读不出来" : LegacySaveReader.MissingProfileFields(merged), null,
          "E1 迁移后的 payload 过字段闸");
        ProfileData.SaveDto dto = Parse(merged);
        Tr(dto != null, "E2 前提：迁移后的 payload 能反序列化成 `SaveDto`");
        C(Get(dto, IdClamp, true), 9,
          "E3 ★ 旧档 highest = 7 但 current = 9 ⇒ 夹到 9（只涨不落；⛔ 直接搬 7 会读回 7）");
    }

    // ============================================================ F：字段闸清单 ⊆ `SaveDto`（补上「从来没有漂移断言」那条）
    static void SectionF_FieldGateCoversSaveDto()
    {
        Type dto = typeof(ProfileData.SaveDto);
        for (int i = 0; i < LegacySaveReader.ProfileFields.Length; i++)
        {
            string name = LegacySaveReader.ProfileFields[i];
            FieldInfo f = dto.GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Tr(f != null, "F1." + i + " 闸里的 `" + name + "` 在 `ProfileData.SaveDto` 里存在（改名/删字段会当场红）");
        }
        Tr(dto.GetField("achHighest", BindingFlags.Instance | BindingFlags.Public) != null,
           "F2 `SaveDto` 仍有 `achHighest`（字段闸与写口看的是同一份形状）");
        Tr(Array.IndexOf(LegacySaveReader.ProfileFields, "equippedBorderIndex") < 0,
           "F3 `equippedBorderIndex` **故意不在**闸里（理由：`ProfileData.cs:1495-1498` —— 它是后加的键，"
         + "加进闸会把这一批老档全判成「字段不全」；⛔ 别为了「清单整齐」把它塞进去）");

        // 差集必须**恰好**是 `equippedBorderIndex`：以后谁给 `SaveDto` 加字段却忘了进闸，这里当场红。
        FieldInfo[] sf = dto.GetFields(BindingFlags.Instance | BindingFlags.Public);
        string diff = "";
        for (int i = 0; i < sf.Length; i++)
            if (Array.IndexOf(LegacySaveReader.ProfileFields, sf[i].Name) < 0) diff += sf[i].Name + ",";
        C(diff, "equippedBorderIndex,",
          "F4 ★ 闸与 `SaveDto` 的差集**恰好**只有 `equippedBorderIndex`（新增字段忘进闸 / 闸里多出一个不存在的键 ⇒ 红）");

        // 迁移是「按 `LegacyProfileFields` 重建 payload」⇒ 它必须覆盖 `SaveDto` 的**全部**字段（⛔ 不是闸那 8 个）。
        Type legacy = typeof(LegacySaveReader).Assembly.GetType("CardPresentation.LegacyProfileFields", false);
        int notCovered = 0;
        if (legacy != null)
            for (int i = 0; i < sf.Length; i++)
                if (legacy.GetField(sf[i].Name, BindingFlags.Instance | BindingFlags.Public) == null) notCovered++;
        C(legacy == null ? -1 : notCovered, 0,
          "F5 ★ `LegacyProfileFields` 覆盖 `SaveDto` 的**全部**字段（少一列 = 迁移重建时把那一列静默丢掉，`equippedBorderIndex` 就是这个坑）");
    }

    // ============================================================ 工具
    /// <summary>跑一遍导入：临时旧档根写 `profile.json`、临时中央根先种一份**缺 `achHighest`** 的中央档，
    /// 然后 `ImportAll()`（`OverrideRoot` 已设 ⇒ `StoreOn` 为真）。返回**迁移后**的中央 payload。</summary>
    static string RunImport(string dir, string legacyJson, string centralJson, string tag)
    {
        string legacyRoot = Path.Combine(dir, "legacy");
        string centralRoot = Path.Combine(dir, "central");
        Directory.CreateDirectory(Path.Combine(legacyRoot, "WarpforgeProfile"));
        Directory.CreateDirectory(centralRoot);
        File.WriteAllText(Path.Combine(legacyRoot, "WarpforgeProfile", "profile.json"), legacyJson);

        SaveCenter.OverrideRoot = centralRoot;
        LegacySaveReader.LegacyRootOverride = legacyRoot;

        Tr(SaveCenter.SetRaw(SaveKeys.CosmeticData, centralJson), tag + "0 前提：种下的中央档写进临时目录了");
        Tr(LegacySaveReader.MissingProfileFields(centralJson) != null, tag + "0b 前提：种下的中央档确实缺 `achHighest`（否则测不到迁移）");

        LegacySaveReader.ImportAll();
        return SaveCenter.GetRaw(SaveKeys.CosmeticData);
    }

    static ProfileData.SaveDto Parse(string payload)
    {
        if (string.IsNullOrEmpty(payload)) return null;
        try { return JsonUtility.FromJson<ProfileData.SaveDto>(payload); }
        catch (Exception e) { Debug.LogWarning("[E21] payload 解析失败：" + e.Message); return null; }
    }

    /// <summary>取某条成就的 current / highest（找不到 ⇒ `int.MinValue`，让断言直接红而**不是静默跳过**）。</summary>
    static int Get(ProfileData.SaveDto dto, string id, bool highest)
    {
        if (dto == null || dto.achIds == null) return int.MinValue;
        for (int i = 0; i < dto.achIds.Length; i++)
        {
            if (dto.achIds[i] != id) continue;
            int[] col = highest ? dto.achHighest : dto.achValues;
            return (col != null && i < col.Length) ? col[i] : int.MinValue;
        }
        return int.MinValue;
    }
}
