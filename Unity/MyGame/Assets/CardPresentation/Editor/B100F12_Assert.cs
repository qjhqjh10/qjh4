// B100F12_Assert.cs — 第一百会话 · 执行代理 `B100_F12` 的断言（卡框贴图 104/104 可被 `Resources.Load` 取到）
//
// ============================ 它盯的是什么 ============================
//  `P11`（`Editor/BattleScene.cs` · `B98P1_Assert.SectionImportNames`）报「原版卡框贴图要么 0 张、要么 104 张，实测 80」。
//  诊断（`报告/B100_X3.md` 第 26 行）已定案：**不是半批导入** ——
//    · 104 张**全在盘上**（`Assets/CardPresentation/Resources/Art/cards/clan/*.png`）；
//    · 其中 **24 张文件名保留了空格**（`Astra Militarum` / `Emperor Children` / `Space Wolves` × 4 档 × 2 类）；
//    · 而 `CardArtClanFrames.ResourceName` 查的是 `FrameTextureName.Replace(' ', '_')` ⇒ 恰好命中 80。
//  修法 = **盘侧重命名**（24 张 + 各自 `.meta`，GUID 一字未改），`FrameTextureName` 的**空格不许动**
//  （那是原版 Sprite 名，`B98M1_Assert` C5-d 在钉它）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A2：把 `ResourceName` 的 `Replace(' ', '_')` 删掉 ⇒ 期望名变回带空格、与盘侧不符 ⇒ 红。
//  · B1：把任意一张 png **改回带空格的名字**（模拟「又按原版名拷进来」）⇒ 该格 `Resources.Load` 取不到 ⇒ loaded 104→103 ⇒ 红。
//  · B2：把 `ImportResourceName` 的前缀 `frame_clan_` 去掉 ⇒ 全 104 格取不到 ⇒ 红。
//  · C2：文件名以 `frame_clan_` 开头但**不是 1024²** ⇒ `FrameUv` 掉进 alpha-bbox 分支 ⇒ 本行红。
//  · D1：往 `cards/clan/` 里再拷一张**带空格**的 png ⇒ 红（这是防止「80/104」复发的回归闸，与 B1 是同一现象的两面）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/BattleScene.cs` 的 `Run()` 里加：
//      B100F12_Assert.Run(); _pass += B100F12_Assert.Pass; _fail += B100F12_Assert.Fail;
//  ⚠️ 本批**不需要**再加这一段：同一宿主的 `B98P1_Assert` 已在跑，`P11` 那条会因为盘侧修好而**自然转绿**
//     （它断的是 `loaded == 0 || loaded == 104`，现在是 104）。本文件是**独立复算 + 新增回归闸**（D 段）。
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B100_F12`：原版卡框贴图 104/104 可被取到（文件名归一化到 `_`）+「带空格的文件名不许再回来」。</summary>
public static class B100F12_Assert
{
    static readonly CheckSink _sink = new CheckSink("[F12] ") { Near = MenuNearStyle.Compact3 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    static readonly int[] Armies = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 };

    /// <summary>`Resources` 里卡框那族的目录（相对 `Assets/`）—— 与 `CardArtClanFrames.FrameTexture` 的 `CardArt.Root + "cards/clan/"` 同源。</summary>
    const string ClanDir = "CardPresentation/Resources/Art/cards/clan";

    public static void Run()
    {
        MenuCheck.Section(_sink, "B100_F12 原版卡框贴图 104/104 可被取到（24 张带空格的文件名已归一化成 `_`）");
        SectionNames();
        SectionLoadAll();
        SectionLoadedTextureShape();
        SectionNoSpacedFileName();
        SectionSpotCheck();
        Debug.Log(_sink.P + "B100F12 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==========================================================================================
    //  A —— 前提：104 个导入名互不重复、都以 `frame_clan_` 开头、且 = `frame_clan_` + `ResourceName`
    //      （这一段是 B/C 的前提；前提不成立时下面那些计数没意义 —— 见 CLAUDE.md「先写前提断言」）
    // ==========================================================================================
    static void SectionNames()
    {
        MenuCheck.Section(_sink, "A 前提：104 个导入名（`frame_clan_<ResourceName>`）互不重复");

        var seen = new HashSet<string>();
        int miss = 0, bad = 0;
        for (int a = 0; a < Armies.Length; a++)
            for (int tier = 1; tier <= 4; tier++)
                for (int k = 0; k < 2; k++)
                {
                    string n = CardArtClanFrames.ImportResourceName(Armies[a], tier, k == 1);
                    if (n == null) { miss++; continue; }
                    if (!n.StartsWith("frame_clan_", StringComparison.Ordinal)) bad++;
                    if (n != "frame_clan_" + CardArtClanFrames.ResourceName(Armies[a], tier, k == 1)) bad++;
                    if (n.IndexOf(' ') >= 0) bad++;
                    if (!seen.Add(n)) bad++;
                }
        C(miss, 0, "A1 104 格全部拿得到导入名（`ImportResourceName` 不返回 null）");
        C(bad, 0, "A2 104 个名字：互不重复 · 以 `frame_clan_` 开头 · **不含空格** · = `frame_clan_` + `ResourceName`"
                + "｜🧨 删掉 `ResourceName` 的 `Replace(' ', '_')` ⇒ 期望名变回带空格 ⇒ 本行红");
        C(seen.Count, 104, "A3 去重后仍是 104 个（重名会让两张卡共用一个框）");
    }

    // ==========================================================================================
    //  B —— 逐格 `Resources.Load`：**必须 104/104**
    //      判据（2026-10-30 `B100_F12` 定）：原版 104 张卡框贴图全在盘上 ⇒ 一个都不许缺。
    //      ⛔ 别把这条改回「0 或 104」—— 那正是 P11 挂了整整一轮还看着「合理」的原因。
    // ==========================================================================================
    static void SectionLoadAll()
    {
        MenuCheck.Section(_sink, "B 逐格 `Resources.Load`：104/104（不再接受「0 或 104」）");

        int loaded = 0;
        var missing = new List<string>();
        for (int a = 0; a < Armies.Length; a++)
            for (int tier = 1; tier <= 4; tier++)
                for (int k = 0; k < 2; k++)
                {
                    var tex = CardArtClanFrames.FrameTexture(Armies[a], tier, k == 1);
                    if (tex != null) { loaded++; continue; }
                    missing.Add(CardArtClanFrames.ImportResourceName(Armies[a], tier, k == 1));
                }

        if (missing.Count > 0)
            Debug.Log(_sink.P + "取不到的 " + missing.Count + " 格："
                    + string.Join(", ", missing.GetRange(0, Mathf.Min(8, missing.Count)).ToArray())
                    + (missing.Count > 8 ? " …" : ""));

        C(loaded, 104, "B1 `FrameTexture` 104 格**全部**取得到 —— 实测 " + loaded
                     + "｜🧨 把任意一张 png 名字改回带空格（或删掉 `frame_clan_` 前缀）⇒ 本行红");
        C(missing.Count, 0, "B2 缺失清单为空｜🧨 这一条是 B1 的「缺哪几格」明细，红的时候日志里带名字");
    }

    // ==========================================================================================
    //  C —— 取到的那张**就是**那一格：名字 = 导入名 · 1024² · `FrameUv` = 原版 sprite rect
    //      （名字不对 ⇒ 取错了资产；尺寸不对 ⇒ 掉进 alpha-bbox 分支，可见内容会放大 ≈11.5% 宽）
    // ==========================================================================================
    static void SectionLoadedTextureShape()
    {
        MenuCheck.Section(_sink, "C 取到的贴图：名字 = 导入名 · 1024² · `FrameUv` = sprite rect (171,11,682,998)/1024");

        int wrong = 0, loaded = 0;
        float wMin = 9f, hMin = 9f;
        for (int a = 0; a < Armies.Length; a++)
            for (int tier = 1; tier <= 4; tier++)
                for (int k = 0; k < 2; k++)
                {
                    var tex = CardArtClanFrames.FrameTexture(Armies[a], tier, k == 1);
                    if (tex == null) continue;
                    loaded++;
                    string want = CardArtClanFrames.ImportResourceName(Armies[a], tier, k == 1);
                    if (tex.name != want || tex.width != 1024 || tex.height != 1024) { wrong++; continue; }
                    var uv = CardView.FrameUv(tex);
                    wMin = Mathf.Min(wMin, uv.width);
                    hMin = Mathf.Min(hMin, uv.height);
                    if (Mathf.Abs(uv.width - 682f / 1024f) > 1e-4f
                     || Mathf.Abs(uv.height - 998f / 1024f) > 1e-4f) wrong++;
                }
        C(loaded, 104, "C0 **前提**：C 段量的是满批 104 张（前提不成立时本段其余两条空转，见头说明）");
        C(wrong, 0, "C1 104 张：贴图名 = 导入名 · 尺寸 1024² · `FrameUv` = 682/1024 × 998/1024"
                  + "｜🧨 导成 682×998 ⇒ 走 alpha-bbox ⇒ 本行红");
        Near(wMin * 1024f, 682f, 1e-3f, "C2 104 张里**最小**的 uv 宽 = 682/1024（`FrameUv` 是 0…1 归一量 ⇒ 先 ×1024 换回像素再比）｜🔴 2026-10-30 订正：原来直接拿 uv 比 682，量纲错，恒红");
        Near(hMin * 1024f, 998f, 1e-3f, "C2-b 104 张里**最小**的 uv 高 = 998/1024（同上，`× 1024` 后比）");
    }

    // ==========================================================================================
    //  D —— 回归闸：`cards/clan/` 下**不许再有带空格的文件名**（这正是 80/104 的成因）
    //      盘侧真相只有一处（`Application.dataPath`）；名字规则只有一处（同一目录的 104 个导入名）。
    // ==========================================================================================
    static void SectionNoSpacedFileName()
    {
        MenuCheck.Section(_sink, "D 回归闸：`cards/clan/` 下没有带空格的文件名 · png 数 = 104");

        string dir = Path.Combine(Application.dataPath, ClanDir.Replace('/', Path.DirectorySeparatorChar));
        bool exists = Directory.Exists(dir);
        Tr(exists, "D0 **前提**：目录存在 —— " + dir
                 + "｜🧨 目录不在 ⇒ 本段其余断言空转（宁可这里红，也不让 D1/D2 静默通过）");
        if (!exists) return;

        var files = Directory.GetFiles(dir);
        var spaced = new List<string>();
        int png = 0;
        for (int i = 0; i < files.Length; i++)
        {
            string fn = Path.GetFileName(files[i]);
            if (fn.IndexOf(' ') >= 0) spaced.Add(fn);
            if (fn.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) png++;
        }

        if (spaced.Count > 0)
            Debug.Log(_sink.P + "带空格的文件名 " + spaced.Count + " 个："
                    + string.Join(", ", spaced.GetRange(0, Mathf.Min(6, spaced.Count)).ToArray())
                    + (spaced.Count > 6 ? " …" : ""));

        C(spaced.Count, 0, "D1 目录里**没有**带空格的文件名（含 `.meta`）"
                         + "｜🧨 再按原版 Texture2D 名（`40k_Cardframes_…_Astra Militarum_…`）拷一张进来 ⇒ 本行红"
                         + "；这一条就是「80/104」复发的唯一入口");
        C(png, 104, "D2 目录里恰好 104 张 png（13 阵营 × 4 档 × 2 类）｜🧨 少一张 ⇒ 红（多一张同样红：D1/B1 会先说话）");
    }

    // ==========================================================================================
    //  E —— 定点抽查：原来挂掉的那三家里各挑一格（名字里带空格的阵营 ⇒ 修完必须能取到）
    //      为什么还要单列：计数相等也可能「A 家的框取到了 B 家那张」，这里把名字钉到具体一格。
    // ==========================================================================================
    static void SectionSpotCheck()
    {
        MenuCheck.Section(_sink, "E 定点抽查：原版名带空格的三个阵营（空格的原来正是这 24 张）");

        var spot = new int[][] {
            new int[] { 100, 1, 0 }, new int[] { 100, 4, 1 },
            new int[] { 120, 2, 0 }, new int[] { 120, 3, 1 },
            new int[] { 130, 1, 1 }, new int[] { 130, 4, 0 },
        };
        for (int i = 0; i < spot.Length; i++)
        {
            int army = spot[i][0], tier = spot[i][1];
            bool tactic = spot[i][2] == 1;
            var tex = CardArtClanFrames.FrameTexture(army, tier, tactic);
            string want = CardArtClanFrames.ImportResourceName(army, tier, tactic);
            Tr(tex != null, "E1-" + i + " army " + army + " tier" + tier + (tactic ? " 计策" : " 部队")
                          + " 取得到（原版 Texture2D 名 = `" + CardArtClanFrames.FrameTextureName(army, tier, tactic) + "`，"
                          + "盘侧名 = `" + want + "`）");
            if (tex != null) C(tex.name, want, "E2-" + i + " 取到的就是那一格（贴图名逐字相等）");
        }
    }
}
