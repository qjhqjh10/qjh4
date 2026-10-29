// B99G3_Assert.cs — 执行代理 `B99_G3`（2026-10-29）的断言集：**`Rank Icon` 的图源**（= 原版 `Image(+0x98).sprite`）
//
// 宿主接线（由调度台做，⛔ 本文件不改任何宿主）：
//   在 `Editor/ShellScene.cs` 的 `RunCore` 末尾、其它 `*_Assert.Run(...)` 那一串之后加一行
//     `B99G3Assert.Run(_sink);`
//   （`ShellScene.Run` 已经开着菜单壳、批次里也跑它 ⇒ 不额外增加一次 Unity 批处理。）
//   本组**不依赖任何宿主窗口**：自己 `RankedDivisionChangeWindow.Create(null, ctx)`（= `B97_PROFILE_Assert` 同一条
//   「没有 `WindowsManager` 只建出来」的分支）。
//
// 【它盯的是什么】改前 `Rank Icon` 只有一句 `NoteNoArt`（`Build()` 里那一行）—— `_rankIconSprite` **只由自检注入**，
//   于是 `ApplyParticleAndGlow` 的 `visible = sprite != null` **恒假** ⇒ `Rank Icon Glow` 恒 `SetActive(false)`、
//   粒子 `Renderer.enabled` 恒 false。原版这一跳（`decomp_full/RankedEventDivisionChangeWindowNewSystem__Open.c:172-197`）：
//     `rankData = RankedDivisions.GetRankData(table, last.`+0x14`)` → `rankData.`+0x18`（`AssetReferenceTyped<Sprite>`）
//     → `Load()` → `Image(+0x98).sprite` → `SetActive(Image.gameObject, sprite != null)`
//   ⇒ 本组断的就是「号 → 图 → 那两个可见性」这一整条。
//
// 【判据从哪来（⛔ 不拿我们自己的常量证明我们自己的实现）】
//   · 图名 = **官方本地化** `Ranked/Rank/1..6`（现读 `I`…`VI`）+ 原版资产名 `Roman_*`
//     （`assetripper_out/ExportedProject/Assets/MonoBehaviour/RankedDivisionsSO.asset` 的 `ranks` 6 条
//      `{divisionNumber: 1..6, image.m_AssetGUID}`，逐条对上 `Roman I..VI` 的 `m_RenderDataKey.guid`）；
//   · 号的**两条边界** = 机器码 `decomp_full/RankedDivisions__GetRankData.c:25-30`：
//     ① `param_2 = Math.Max(param_2, 1)`（≤0 夹到 1 ⇒ 画 `Roman_I`）；② 之后只有 `FirstOrDefault`，
//     **没有** `Last()` 兜底（那句在 `GetDivisionData.c:31-33`）⇒ ≥7 查不到 = `null` = **不画**；
//   · 显隐 = 原版 `SetActive(Image.sprite != null)`；粒子 ③④ = `ChangeParticleEffectAndSpriteRank` 末两跳。
//
// 【灭自证（每条正向都配一条结构上不可能同时满足的反向）】
//   · 同一个窗实例上「没号 ⇒ 空白 + 辉光关」→「给号 ⇒ 有图 + 辉光开」；
//   · 换一个号必须换一张图（🧨 写死 `Roman_II` ⇒ 红）；
//   · 号 0 必须落到 `Roman_I`、号 7 必须**不画**（🧨 顺手补个 `Last()` 兜底 ⇒ 后者红）；
//   · `RankNumber` 清空后重开必须**回到空白**（🧨 只清 quad、不清 `RankIconSprite` ⇒ 辉光留在上一张图上 ⇒ 红）。
using CardPresentation;
using UnityEngine;

/// <summary>`B99_G3` 的断言集。入口 = <see cref="Run"/>。</summary>
public static class B99G3Assert
{
    const string P = "[B99_G3] ";
    const string RankPath = "Division Image/Rank Icon";
    const string GlowPath = "Division Image/Rank Icon/Rank Icon Glow";

    public static void Run(CheckSink s)
    {
        MenuCheck.Section(s, "B99_G3 · `Rank Icon Glow` 恒灭 = 接线问题（名次号 → `RankedDivisionInfo.RankArt` → 图 → 辉光/粒子）");
        SectionTable(s);
        SectionWindow(s);
    }

    // ============================================================ ① 表：号 → 图名（不依赖窗口）
    static void SectionTable(CheckSink s)
    {
        int n = RankedDivisionInfo.RankArtCount;
        MenuCheck.Check(s, n, 6, "★ 名次表长度 = **6**（原版 `RankedDivisionsSO.asset` 的 `ranks` 6 条）"
                             + "｜🧨 少一条 / 多一条 ⇒ 红");

        // 逐个号：图名 == `Roman_` + 官方罗马数字（独立判据 = 官方本地化表）
        string badName = "", badOfficial = "", badArt = "";
        int squares = 0, squareSide = -1;
        for (int i = 1; i <= n; i++)
        {
            string name = RankedDivisionInfo.RankArtName(i);
            string official = Loc.OfficialCurrent("Ranked/Rank/" + i);
            if (string.IsNullOrEmpty(official) || official == "Ranked/Rank/" + i) badOfficial += " " + i;
            else if (name != "Roman_" + official) badName += " " + i + "=" + (name ?? "<null>") + "≠Roman_" + official;
            var tex = CardArt.MenuUi(name);
            if (tex == null) badArt += " " + i + "=" + (name ?? "<null>");
            else
            {
                if (tex.width == tex.height) squares++;
                if (squareSide < 0) squareSide = tex.width;
            }
        }

        MenuCheck.True(s, badOfficial.Length == 0,
            "★ 官方词条 `Ranked/Rank/1..6` 六条都取得到（现读 `I`…`VI`）"
          + "｜🧨 官方表没并入 / 键名写错 ⇒ 红（实得缺：" + (badOfficial.Length == 0 ? "无" : badOfficial.Trim()) + "）");
        MenuCheck.True(s, badName.Length == 0,
            "★ `RankArtName(1..6)` == `Roman_` + 官方罗马数字（**逐条**对）"
          + "｜🧨 表错位 / 名字被改成 `Roman_VII` 之类 ⇒ 红（实得错：" + (badName.Length == 0 ? "无" : badName.Trim()) + "）");
        MenuCheck.True(s, badArt.Length == 0,
            "★ 6 张名次图**都取得到**（`Resources/Art/ui_menu/Roman_I…VI`）"
          + "｜🧨 少导一张 ⇒ `Tex()` 静默返回 null、辉光不亮 ⇒ 红（实得缺：" + (badArt.Length == 0 ? "无" : badArt.Trim()) + "）");
        MenuCheck.Check(s, squares, 6, "★ 6 张名次图**都是正方形**（原版 128²；实测首张 " + squareSide + "）"
                                    + "｜🧨 某张被导成横图 ⇒ 画进 73.51² 的框里会变形 ⇒ 红");

        // 灭自证：6 个图名两两不同（🧨 全返回同一张也满足上面那三条）
        bool distinct = true;
        for (int i = 1; i <= n && distinct; i++)
            for (int j = i + 1; j <= n && distinct; j++)
                distinct = RankedDivisionInfo.RankArtName(i) != RankedDivisionInfo.RankArtName(j);
        MenuCheck.True(s, distinct, "★ 6 个图名**两两不同**｜🧨 让查表恒返回同一张（比如永远 `Roman_I`）⇒ 红");

        // 机器码的两条边界
        MenuCheck.True(s, RankedDivisionInfo.RankArtName(7) == null,
            "★ 号 **7** 查不到 ⇒ `null`（机器码 `GetRankData` 只有 `FirstOrDefault`、**没有** `Last()` 兜底；"
          + "那句兜底在 `GetDivisionData.c:31-33`）｜🧨 给名次表补一个 `Last()` 兜底（= 抄错函数）⇒ 红");
        MenuCheck.True(s, RankedDivisionInfo.RankArtName(1) != null,
            "（前提）号 1 在表里（`Math.Max(n,1)` 夹到 1 之后要能画出东西）");
    }

    // ============================================================ ② 窗口：号 → 图 → 辉光/粒子
    static void SectionWindow(CheckSink s)
    {
        var ctx = new RankedDivisionChangeWindow.Context { IsDivisionChange = false };
        var win = RankedDivisionChangeWindow.Create(null, ctx);
        var savedRanks = RankedData.Ranks;
        int savedElo = RankedData.LegendaryElo;
        float tick = RankedDivisionChangeWindow.PromoteDelay(RankedDivisionChangeWindow.StepR.Length) + 0.001f;
        try
        {
            // ---- 前提（硬检查，⛔ 不写成 `if (x != null) Check(...)`）----
            MenuCheck.True(s, win != null, "（前提）升段窗建出来了（`Create(null, ctx)` 走「没有 `WindowsManager` 只建出来」那条分支）");
            if (win == null) return;
            var q0 = RankQuad(win);
            MenuCheck.True(s, q0 != null, "（前提）`" + RankPath + "` 上挂了 `ImageQuad`（图的落点）"
                                        + "｜🧨 `BuildRankIcon` 退回 `MenuDraw.Node` ⇒ 红");
            MenuCheck.True(s, win.RankParticleForTest != null,
                "（前提）`Particles Rank Up ▸ Sparks Burn ▸ Promote Letter Effect` 那颗 `ParticleSystem` 在");
            if (q0 == null || win.RankParticleForTest == null) return;

            // ---- 状态 1：没有名次号（= 本机常态）⇒ 与改前的可观测行为**一致** ----
            MenuCheck.True(s, string.IsNullOrEmpty(win.RankArtUsed),
                "★ ① 没拿到名次号 ⇒ `RankArtUsed` 空串（如实「没画」，⛔ 不编演示名次号）"
              + "（实得「" + (win.RankArtUsed ?? "<null>") + "」）");
            MenuCheck.True(s, !IsRomanArt(q0.Texture),
                "★ ② 没号 ⇒ `Rank Icon` 上**不是**任何一张罗马名次图（仍是 `CardArt.Solid()` 的占位）"
              + "（实得 " + TexName(q0.Texture) + "）");
            MenuCheck.Near(s, q0.Tint.a, 0f, 1e-4f,
                "★ ③ 没号 ⇒ 占位仍 **α = 0**（完全透明；`BuildRankIcon` 建的就是 α0）");
            MenuCheck.True(s, win.RankIconSprite == null,
                "★ ④ 没号 ⇒ `RankIconSprite` **null**（原版 `Image.sprite = null` 的等价物；这是辉光/粒子那两跳的闸）");

            win.TickForTest(tick);
            MenuCheck.True(s, !win.NodeActiveForTest(GlowPath),
                "★ ⑤ 没号 + 走到 promote 那一拍 ⇒ `Rank Icon Glow` **仍关着**（原版 `SetActive(sprite != null)`）"
              + "｜🧨 把辉光改成恒开 ⇒ 红");
            MenuCheck.True(s, !win.RankParticleEnabled,
                "★ ⑥ 没号 ⇒ 粒子 ④ `Renderer.enabled = false`（原版 `ChangeParticleEffectAndSpriteRank` 末跳）"
              + "｜🧨 恒 true ⇒ 红");

            // ---- 状态 2：给号 2（= 参考图「金 II」那一档）⇒ 图与辉光都出来 ----
            win.RankNumber = 2;
            win.Show(win.Ctx);
            var q2 = RankQuad(win);
            MenuCheck.True(s, q2 != null, "（前提·状态 2）重建后 `" + RankPath + "` 仍有 `ImageQuad`");
            if (q2 == null) return;

            string want2 = RankedDivisionInfo.RankArtName(2);
            MenuCheck.Check(s, win.RankArtUsed, want2,
                "★ ⑦ 号 2 ⇒ `RankArtUsed` == `RankedDivisionInfo.RankArtName(2)` = 「" + (want2 ?? "<null>") + "」");
            MenuCheck.Check(s, TexName(q2.Texture), want2,
                "★ ⑧ 号 2 ⇒ `Rank Icon` 的画布纹理**真的换成**那张图（原版 `Image(+0x98).sprite = Load(...)`）"
              + "｜🧨 只置 `RankIconSprite` 不换纹理（= 辉光亮了但底下还是白块）⇒ 红");
            MenuCheck.Near(s, q2.Tint.a, 1f, 1e-4f,
                "★ ⑨ 号 2 ⇒ 占位 α 从 0 **提到 1**（不然图取到了也看不见）"
              + "｜🧨 忘掉 `SetTint` ⇒ 红");
            MenuCheck.True(s, win.RankIconSprite != null && win.RankIconSprite.texture == q2.Texture,
                "★ ⑩ `RankIconSprite` 被喂进来了、且它的纹理就是那张图（`ApplyParticleAndGlow` ①②③④ 的判据）");

            win.StartSequence();
            win.TickForTest(tick);
            MenuCheck.True(s, win.NodeActiveForTest(GlowPath),
                "★ ⑪ 号 2 + promote ⇒ `Rank Icon Glow` **被打开**（这就是本件要修的「恒灭」）"
              + "｜🧨 不喂 `RankIconSprite` ⇒ 红");
            MenuCheck.True(s, win.RankParticleEnabled,
                "★ ⑫ 号 2 + promote ⇒ 粒子 ④ `Renderer.enabled = true`");
            MenuCheck.Check(s, win.RankParticleForTest.textureSheetAnimation.GetSprite(0), win.RankIconSprite,
                "★ ⑬ 粒子 ③ `textureSheetAnimation.SetSprite(0, rankIcon.m_Sprite)` 落到了那一格");

            // ---- 状态 3：换号 5 ⇒ 换图（🧨 写死一张 ⇒ 红）----
            var texII = q2.Texture;
            win.RankNumber = 5;
            win.Show(win.Ctx);
            var q5 = RankQuad(win);
            MenuCheck.True(s, q5 != null, "（前提·状态 3）重建后 `" + RankPath + "` 仍有 `ImageQuad`");
            if (q5 == null) return;
            MenuCheck.Check(s, win.RankArtUsed, RankedDivisionInfo.RankArtName(5),
                "★ ⑭ 号 5 ⇒ `RankArtUsed` == 「" + (RankedDivisionInfo.RankArtName(5) ?? "<null>") + "」"
              + "（实得「" + (win.RankArtUsed ?? "<null>") + "」）");
            MenuCheck.True(s, q5.Texture != null && q5.Texture != texII,
                "★ ⑮ 号 5 的图 **≠** 号 2 的图（" + TexName(q5.Texture) + " vs " + TexName(texII) + "）"
              + "｜🧨 把名次图写死成 `Roman_II` ⇒ 红");

            // ---- 状态 4：机器码的两条边界 ----
            win.RankNumber = 0;
            win.Show(win.Ctx);
            MenuCheck.Check(s, win.RankArtUsed, RankedDivisionInfo.RankArtName(1),
                "★ ⑯ 号 **0** ⇒ 夹到 1 ⇒ 「" + (RankedDivisionInfo.RankArtName(1) ?? "<null>") + "」"
              + "（机器码 `Math.Max(param_2, 1)`）｜🧨 写成「≤0 不画」⇒ 红（实得「" + (win.RankArtUsed ?? "<null>") + "」）");

            win.RankNumber = 7;
            win.Show(win.Ctx);
            MenuCheck.True(s, string.IsNullOrEmpty(win.RankArtUsed),
                "★ ⑰ 号 **7** ⇒ **不画**（`GetRankData` 查不到就返回 `null`，**没有** `Last()` 兜底）"
              + "｜🧨 补个 `Last()` 兜底 ⇒ 这里会变成 `Roman_VI` ⇒ 红（实得「" + (win.RankArtUsed ?? "<null>") + "」）");

            // ---- 状态 5：`RankNumber` 清空 + 本地真有阈值表 ⇒ 走 `RankedData.CurrentDivision.RankNumber` ----
            //   ⚠️ 全局静态注入，**必须还原**（`finally` 里 `SetRanksForTest(savedRanks, savedElo)`）；
            //      ⛔ 不动 `Points`（`SetPointsForTest` 会写玩家存档）。
            RankedData.SetRanksForTest(new[]
            {
                new RankedRank { division = 4, rank = 3, sealsRequiredToUpdate = 3, correspondingMMR = 0 },
                new RankedRank { division = 4, rank = 3, sealsRequiredToUpdate = 3, correspondingMMR = 100 },
            }, 1000);
            MenuCheck.True(s, RankedData.HasRankTable, "（前提·状态 5）注入阈值表后 `RankedData.HasRankTable` = true");
            win.RankNumber = null;
            win.Show(win.Ctx);
            MenuCheck.Check(s, win.RankArtUsed, RankedDivisionInfo.RankArtName(3),
                "★ ⑱ `RankNumber` 为空、但本地**真有**阈值表（`CurrentDivision.Known`）⇒ 用它的 `RankNumber` = 3"
              + " ⇒ 「" + (RankedDivisionInfo.RankArtName(3) ?? "<null>") + "」（实得「" + (win.RankArtUsed ?? "<null>") + "」）"
              + "｜🧨 不看 `Known` 直接读 `RankNumber`（没有表时是 `default(int)` = 0）⇒ 会画出 `Roman_I` ⇒ 状态 1 那条红");

            // ---- 状态 6：还原阈值表 + 清空号 ⇒ 必须**回到空白**（清场；🧨 只清 quad 不清 `RankIconSprite` ⇒ 红）----
            RankedData.SetRanksForTest(savedRanks, savedElo);
            win.Show(win.Ctx);
            MenuCheck.True(s, string.IsNullOrEmpty(win.RankArtUsed),
                "★ ⑲ 阈值表还原、号仍空 ⇒ `RankArtUsed` 回到空串");
            MenuCheck.True(s, win.RankIconSprite == null,
                "★ ⑳ 同上 ⇒ `RankIconSprite` 也被**清回 null**（原版 `Image.sprite = null`）"
              + "｜🧨 只清 quad、不清字段 ⇒ 复用的窗会把上一张名次图的辉光留着 ⇒ 红");
            var q6 = RankQuad(win);
            MenuCheck.True(s, q6 != null && !IsRomanArt(q6.Texture),
                "★ ㉑ 同上 ⇒ `Rank Icon` 的画布纹理退回占位（不是罗马图）（实得 " + TexName(q6 == null ? null : q6.Texture) + "）");
            win.StartSequence();
            win.TickForTest(tick);
            MenuCheck.True(s, !win.NodeActiveForTest(GlowPath),
                "★ ㉒ 同上 + promote ⇒ `Rank Icon Glow` **重新关上**（与状态 1 同一条判据，换个顺序再断一次）");
        }
        finally
        {
            RankedData.SetRanksForTest(savedRanks, savedElo);
            if (win != null) Object.DestroyImmediate(win.gameObject);
        }
    }

    // ============================================================ 小工具
    static ImageQuad RankQuad(RankedDivisionChangeWindow w)
    {
        var n = w.NodeForTest(RankPath);
        return n == null ? null : n.GetComponent<ImageQuad>();
    }

    /// <summary>这张纹理是不是 6 张罗马名次图之一（⛔ 不判「等于 `CardArt.Solid()`」—— 那会把占位实现也钉死）。</summary>
    static bool IsRomanArt(Texture t)
    {
        if (t == null) return false;
        string nm = t.name ?? "";
        if (!nm.StartsWith("Roman_")) return false;
        for (int i = 1; i <= RankedDivisionInfo.RankArtCount; i++)
            if (nm == RankedDivisionInfo.RankArtName(i)) return true;
        return false;
    }

    static string TexName(Texture t) { return t == null ? "<null>" : (t.name ?? "<unnamed>"); }
}
