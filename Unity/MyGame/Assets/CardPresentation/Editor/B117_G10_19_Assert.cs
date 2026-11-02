// B117_G10_19_Assert.cs — 执行代理 `B117_G10_19`（战斗域第 19 部分：**结算面板 · 战后出口**）的断言
//
// ============================ 它盯的是什么 ============================
//   原版 `EndBattlePanel` **16 颗节点**（`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv:1149-1164`）
//   + `AllRewardsHolder` 上那块 **`CanvasGroup` 的淡入**（`EndBattleDoors.allRewardsGroup`，`+0x70`）
//   + `RewardsHolder/HolderRating` 那一棵（`Trophy` / `ArmyIcon` / `RatingText`）
//   + 战后出口的闸门（`matchFinishedAndWaitingToLeave`）。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B117-G10-19_结算面板与战后出口.md`
//   · 逐值出处 = AssetRipper 场景 YAML `…/Battle Scenarios/Battle Arena 1.unity`（RT / Image / TMP 组件逐颗现读）
//     + 运行时 dump 上面那 16 行 + 反编译 `EndBattleDoors__ShowRewards.c` / `SetupDoor.c` /
//       `BattleManager._CloseBattleDoors_d__393__MoveNext.c`。
//   ⚠️ **本文件里的期望值全部是原版字段的原始数值**（⛔ 不是读 `EndPanel` 的常量来证明它自己 —— 那就是自证）。
//
// ============================ 怎么用（宿主）============================
// `B117G1019Assert.Run();`
//   + 宿主里两行：`_sink.Pass += B117G1019Assert.Pass; _sink.Fail += B117G1019Assert.Fail;`
//   · 推荐宿主 = `Editor/BattleScene.cs`（`BattleScene.Run`，那里 `LayoutSpace.Cam` 已 `Apply`）。
//   · 本文件**不建持久资产**：整棵（`BattleDoors` + `EndBattlePanel`）在 `finally` 里 `DestroyImmediate`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A27 / A33：把 `TrophyX` / `RatingTextX` 写成 0 ⇒ 两条同时红（两颗会叠在行心）。
//   · A30：把 `BuildRatingRow` 里 `army.gameObject.SetActive(false)` 删掉 ⇒ A30 红
//     （原版 `ArmyIcon` 序列化就是 `m_IsActive 0`）。
//   · B3 / B5：把 `SetRating` 改成「恒显示」⇒ B5 红；改成「恒隐藏」⇒ B3 红。**两条方向相反、一起钉死**。
//   · B6：把负分那档的字色写成恒白 ⇒ 红。
//   · B9 / B10：把 `LayoutRewardRows` 里的 `extra` 项删掉（= 只排奖励行）⇒ 两条同时红
//     （单条奖励会回到居中 x=0，而有评分时原版是 −109.987 / +86.5）。
//   · C3 / C5：把 `BeginRewardsFade` 的 `alpha = 0` 删掉 ⇒ C3 红；把 `RewardsFadeSeconds` 改成 0.1（或忽略延时）
//     ⇒ C5 红（中途会提前到 1 / 0.4）。
//   · D4：把 `Show` 里 `skipSequence` 那一支的 `Advance(_doors.Length)` 删掉 ⇒ 闸门永不置位 ⇒ D4 红
//     （而且真机上就是**永久软锁**）。
using System.Collections.Generic;
using CardPresentation;
using TMPro;
using UnityEngine;

/// <summary>执行代理 `B117_G10_19`：结算面板 16 节点 / 奖励块淡入 / 评分行 / 战后出口闸门的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B117G1019Assert
{
    static readonly CheckSink _sink = new CheckSink("[G10/19] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static bool Near(float a, float b) { return Mathf.Abs(a - b) <= 0.01f; }

    public static void Run()
    {
        MenuCheck.Section(_sink, "G10-19 结算面板（原版 `EndBattlePanel` 16 节点）· 奖励块淡入 · `HolderRating` · 战后出口");

        SectionA_几何();
        SectionB_显隐与布局();
        SectionC_淡入();
        SectionD_出口闸();
    }

    // ============================================================ 公共：建一台临时面板
    /// <summary>建一台临时 `EndPanel`（`EndPanel.Create` 会自己铺 `BattleDoors` 外骨架 + 面板）。
    /// 宿主必须已有 `HudCanvas`（`LayoutSpace.Cam` 已被 `LayoutSpace.Apply` 过）。</summary>
    static EndPanel MakePanel(out Camera madeCam)
    {
        madeCam = null;
        if (LayoutSpace.Cam == null)
        {
            Debug.LogWarning("[G10/19] 宿主没有 `LayoutSpace.Cam` ⇒ 现场补一台临时相机（⛔ 不是跳过断言："
                           + "本文件的断言前提是「有一块真画布」，补不上就 A0 红）。");
            var camGo = new GameObject("G1019_Cam");
            madeCam = camGo.AddComponent<Camera>();
            madeCam.clearFlags = CameraClearFlags.SolidColor;
            madeCam.aspect = LayoutSpace.DesignAspect;
            LayoutSpace.Apply(madeCam);
        }
        return EndPanel.Create(null);
    }

    static void KillPanel(EndPanel p, Camera madeCam)
    {
        if (p != null)
        {
            var d = p.Doors;
            if (d != null && d.gameObject != null) Object.DestroyImmediate(d.gameObject);
            else if (p.gameObject != null) Object.DestroyImmediate(p.gameObject);
        }
        if (madeCam != null)
        {
            Object.DestroyImmediate(madeCam.gameObject);
            LayoutSpace.Cam = null;      // 临时相机的登记也要撤（⛔ 别把宿主后面的断言带进一台已销毁的相机）
        }
    }

    // ============================================================ A 几何（16 颗逐颗）
    static void SectionA_几何()
    {
        MenuCheck.Section(_sink, "A 几何：16 节点逐颗（尺寸 / 位置 / PA / 字体面 / 字号三档）");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "A0 **前提断言**：宿主里建得出 `EndPanel`（`EndPanel.Create` 非 null）"
                            + " —— 不成立 ⇒ A1..A36 **没跑**（本文件不静默跳过）。推荐宿主 = `Editor/BattleScene.cs`");
            if (panel == null) return;

            var root = panel.transform;

            // ---- A1 面板根 ----
            C(root.name, "EndBattlePanel", "A1 面板根节点名 = 原版 `EndBattlePanel`（`BattleDoors/Canvas/EndBattlePanel`）");

            // ---- A2..A5 第 2 颗 `Background` ----
            var bg = root.Find("Background") as RectTransform;
            Tr(bg != null, "A2 ★ 第 2 颗 `EndBattlePanel/Background` 建出来了");
            if (bg != null)
            {
                var sz = HudCanvas.SizePx(bg);
                Tr(Near(sz.x, 3840f) && Near(sz.y, 2160f),
                   $"A3 `Background` = 原版 **3840×2160**（两倍屏；实测 {sz.x:F1}×{sz.y:F1}）");
                Tr(Near(bg.anchoredPosition.x, 0f) && Near(bg.anchoredPosition.y, 0f),
                   $"A4 `Background` 居中 @ (0,0)（实测 {bg.anchoredPosition.x:F2}, {bg.anchoredPosition.y:F2}）");
            }
            var dim = panel.DimGraphicForTest;
            Tr(dim != null && dim.color.a > 0.75f && dim.color.a < 0.76f,
               "A5 压暗层 α = 原版 **0.7529412**（`EndBattlePanel/Background` 的 `Image.m_Color`；实测 "
             + (dim != null ? dim.color.a.ToString("F4") : "null") + "）");

            // ---- A6..A8 第 3 颗 `Video Image` ----
            var vid = root.Find("Video Image") as RectTransform;
            Tr(vid != null, "A6 ★ 第 3 颗 `EndBattlePanel/Video Image` 建出来了");
            if (vid != null)
            {
                var sz = HudCanvas.SizePx(vid);
                Tr(Near(sz.x, 1920f) && Near(sz.y, 1080f), $"A7 `Video Image` = 1920×1080（实测 {sz.x:F1}×{sz.y:F1}）");
                Tr(Near(vid.anchoredPosition.x, -6f) && Near(vid.anchoredPosition.y, 64f),
                   $"A8 `Video Image` @ 原版 (−6, +64)（实测 {vid.anchoredPosition.x:F2}, {vid.anchoredPosition.y:F2}）");
            }

            // ---- A9..A12 第 4 颗 `AllRewardsHolder` ----
            var all = root.Find("AllRewardsHolder") as RectTransform;
            Tr(all != null, "A9 ★ 第 4 颗 `AllRewardsHolder` 建出来了");
            if (all != null)
            {
                var sz = HudCanvas.SizePx(all);
                Tr(Near(sz.x, 100f) && Near(sz.y, 100f), $"A10 `AllRewardsHolder` = 100×100（实测 {sz.x:F1}×{sz.y:F1}）");
                Tr(Near(all.anchoredPosition.x, 0f) && Near(all.anchoredPosition.y, -260f),
                   $"A11 `AllRewardsHolder` @ 面板中心下方 **−260**（实测 y {all.anchoredPosition.y:F2}）");
            }
            Tr(panel.AllRewardsGroupForTest != null,
               "A12 ★ `AllRewardsHolder` 挂着原版那块 **`CanvasGroup`**（`EndBattleDoors.allRewardsGroup` = `+0x70`，`m_Alpha 1`）");

            // ---- A13..A18 第 5..8 颗 `SkullsHolder` + 三颗骷髅 ----
            var skullRow = all != null ? all.Find("SkullsHolder") as RectTransform : null;
            Tr(skullRow != null, "A13 ★ 第 5 颗 `SkullsHolder` 建出来了");
            if (skullRow != null)
            {
                var sz = HudCanvas.SizePx(skullRow);
                Tr(Near(sz.x, 648.1f) && Near(sz.y, 52.4f), $"A14 `SkullsHolder` = 648.1×52.4（实测 {sz.x:F1}×{sz.y:F1}）");
            }
            var sk = panel.SkullQuads;
            Tr(sk != null && sk.Length == 3 && sk[0] != null && sk[1] != null && sk[2] != null,
               "A15 ★ 第 6..8 颗（三颗骷髅）都建出来了");
            if (sk != null && sk.Length == 3 && sk[0] != null && sk[1] != null && sk[2] != null)
            {
                var r0 = (RectTransform)sk[0].transform;
                var r1 = (RectTransform)sk[1].transform;
                var r2 = (RectTransform)sk[2].transform;
                var ssz = HudCanvas.SizePx(r0);
                Tr(Near(ssz.x, 64.33f) && Near(ssz.y, 71.205f),
                   $"A16 骷髅 rect = 原版 **64.33×71.205**（PA=0 拉满；实测 {ssz.x:F2}×{ssz.y:F2}）");
                float a0 = r0.anchoredPosition.x, a1 = r1.anchoredPosition.x, a2 = r2.anchoredPosition.x;
                // 步距 = 宽 64.33 + `SkullsHolder` 的 `HorizontalLayoutGroup.m_Spacing` 6.1999998 = 70.53
                Tr(Near(a1 - a0, 70.53f) && Near(a2 - a1, 70.53f),
                   $"A17 骷髅步距 = 原版 **70.53**（= 64.33 + spacing 6.2；实测 {a1 - a0:F2} / {a2 - a1:F2}）");
                Tr(Near(a1, 0f), $"A18 中间那颗落在行心 x = 0（实测 {a1:F2}）");
            }

            // ---- A19..A21 第 9 颗 `RewardsHolder` ----
            var rr = panel.RewardsRow as RectTransform;
            Tr(rr != null, "A19 ★ 第 9 颗 `RewardsHolder` 建出来了");
            if (rr != null)
            {
                var sz = HudCanvas.SizePx(rr);
                Tr(Near(sz.x, 495.2f) && Near(sz.y, 49f), $"A20 `RewardsHolder` = 495.2×49（实测 {sz.x:F1}×{sz.y:F1}）");
                Tr(Near(rr.anchoredPosition.y, -56.88f),
                   $"A21 `RewardsHolder` 行心 = `AllRewardsHolder` 中心上方 **−56.88**（实测 {rr.anchoredPosition.y:F2}）");
            }

            // ---- A22..A37 第 13..16 颗 `HolderRating` 那一棵（2026-11-02 之前整棵缺）----
            var rating = panel.RatingHolderForTest as RectTransform;
            Tr(rating != null, "A22 ★ 第 13 颗 `HolderRating` 建出来了（原版 `RewardsHolder/HolderRating`）");
            if (rating != null)
            {
                var sz = HudCanvas.SizePx(rating);
                Tr(Near(sz.x, 186.9742f) && Near(sz.y, 45f),
                   $"A23 `HolderRating` = 原版 **186.9742×45**（实测 {sz.x:F4}×{sz.y:F1}）");

                var trophy = panel.TrophyForTest;
                Tr(trophy != null, "A24 ★ 第 14 颗 `Trophy` 建出来了");
                if (trophy != null)
                {
                    var trt = trophy.Rect;
                    var tsz = HudCanvas.SizePx(trt);
                    Tr(Near(tsz.x, 60.0588f) && Near(tsz.y, 60f),
                       $"A25 `Trophy` = 原版 **60.0588×60**（实测 {tsz.x:F4}×{tsz.y:F1}）");
                    var img = trophy.gameObject.GetComponent<UnityEngine.UI.Image>();
                    Tr(img != null && img.preserveAspect,
                       "A26 `Trophy` 的 `Image.m_PreserveAspect = 1`（原版 `Image 4063` 是 PA=1 · Simple）");
                    Tr(Near(trt.anchoredPosition.x, -32.651f),
                       $"A27 ★ `Trophy` 中心 x = **−32.651**（布局组 surplus>0 那一档；实测 {trt.anchoredPosition.x:F3}）"
                     + "｜🧨 把它写成 0 ⇒ 与 A33 一起红（两颗会叠在行心）");
                }

                var army = panel.ArmyIconForTest;
                Tr(army != null, "A28 ★ 第 15 颗 `ArmyIcon` 建出来了");
                if (army != null)
                {
                    var art = army.Rect;
                    var asz = HudCanvas.SizePx(art);
                    Tr(Near(asz.x, 64.8399f) && Near(asz.y, 60f),
                       $"A29 `ArmyIcon` = 原版 **64.8399×60**（实测 {asz.x:F4}×{asz.y:F1}）");
                    Tr(!army.gameObject.activeSelf,
                       "A30 ★ `ArmyIcon` **恒不激活**（原版序列化 `m_IsActive = 0`；图由 liveops/`ArmyUtilities` 才填）");
                    Tr(Near(art.anchoredPosition.x, -1.0084f),
                       $"A31 `ArmyIcon` 中心 x = **−1.0084**（= 序列化 `ap.x 92.478745` 换到中心锚；实测 {art.anchoredPosition.x:F4}）");
                }

                var rt = panel.RatingTextForTest;
                Tr(rt != null, "A32 ★ 第 16 颗 `RatingText` 建出来了");
                if (rt != null)
                {
                    var rrt = (RectTransform)rt.transform;
                    Tr(Near(rrt.anchoredPosition.x, 30.029f),
                       $"A33 ★ `RatingText` 中心 x = **+30.029**（实测 {rrt.anchoredPosition.x:F3}）"
                     + "｜🧨 把它写成 0 ⇒ 与 A27 一起红");
                    if (trophy != null)
                        Tr(Near(rrt.anchoredPosition.x - trophy.Rect.anchoredPosition.x, 62.6804f),
                           $"A34 两颗的中心距 = **62.6804**（= (60.0588 + 65.302)/2，spacing 0；实测 "
                         + $"{rrt.anchoredPosition.x - trophy.Rect.anchoredPosition.x:F4}）");
                    // 🔴 **2026-11-02（`B117-FIXD`）就地订正（铁律 5）**：必须传 `includeInactive: true` ——
                    //   `HolderRating` 生产恒关（`BuildRatingRow` 末句 `SetActive(false)`；原版 `EarnedRating == 0` 就不亮），
                    //   而 `GetComponentInChildren<T>()` **默认跳过非激活对象** ⇒ 原来这条前提**恒假**（与实现无关）。
                    var tmp = rt.GetComponentInChildren<TextMeshProUGUI>(true);
                    Tr(tmp != null, "A35 **前提断言**：`RatingText` 的 TMP 子件取得到（含非激活件 —— `HolderRating` 生产恒关）");
                    if (tmp != null)
                    {
                        Tr(Near(tmp.fontSizeMax, 40f) && Near(tmp.fontSizeMin, 18f) && tmp.enableAutoSizing,
                           $"A36 字号 = 原版 **40 / 自适应 [18,40]**（`m_fontSize 40` · `Min 18` · `Max 40`；实测 max "
                         + $"{tmp.fontSizeMax:F1} · min {tmp.fontSizeMin:F1} · auto {tmp.enableAutoSizing}）");
                        Tr(tmp.font != null && tmp.font.name.Contains("Pragati"),
                           "A37 `RatingText` 用 **Pragati**（原版 `m_fontAsset` = `Pragati-Regular SDF`；实测 "
                         + (tmp.font != null ? tmp.font.name : "null") + "）");
                    }
                }
            }
        }
        finally { KillPanel(panel, madeCam); }
    }

    // ============================================================ B 显隐与布局
    static void SectionB_显隐与布局()
    {
        MenuCheck.Section(_sink, "B 显隐与布局：三档全空 ⇒ 整块藏 · 评分 0/15/负 三档 · 同一个布局组下 奖励行 −109.987 / 评分行 +86.5");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "B0 **前提断言**：`EndPanel.Create` 非 null");
            if (panel == null) return;

            var all = panel.transform.Find("AllRewardsHolder");
            Tr(all != null, "B0b **前提断言**：`AllRewardsHolder` 取得到");

            // ---- B1 三档全空（0 骷髅 / 0 奖励 / 评分 0）⇒ 整块藏 ----
            panel.Show(2, 0, 0, 5, -1, -1);        // 我方胜 · 已达成骷髅 0
            Tr(all != null && !all.gameObject.activeSelf,
               "B1 ★ 骷髅 0 + 奖励空 + 评分 0 ⇒ **`AllRewardsHolder` 整块藏**"
             + "（原版 `EndBattleDoors__ShowRewards.c:30-52`：那一档 `SetActive(false)` 之后直接 return）");

            // ---- B2/B3 评分行的显隐：两条方向相反，一起钉死「恒亮 / 恒藏」 ----
            Tr(panel.RatingHolderForTest == null || !panel.RatingHolderForTest.gameObject.activeSelf,
               "B2 评分 0 ⇒ `HolderRating` 不亮");
            panel.SetRating(15);
            Tr(panel.RatingHolderForTest != null && panel.RatingHolderForTest.gameObject.activeSelf,
               "B3 ★ 评分 15 ⇒ `HolderRating` 亮了（把 `SetRating` 改成恒隐藏 ⇒ 红）");
            Tr(panel.RatingTextForTest != null && panel.RatingTextForTest.Text == "15",
               "B4 评分文字 = **15**（`ratingText.text = EarnedRating.ToString()`；实测「"
             + (panel.RatingTextForTest != null ? panel.RatingTextForTest.Text : "null") + "」）");
            panel.SetRating(0);
            Tr(panel.RatingHolderForTest != null && !panel.RatingHolderForTest.gameObject.activeSelf,
               "B5 ★ 评分回到 0 ⇒ 又不亮（把 `SetRating` 改成恒显示 ⇒ 红；与 B3 方向相反）");

            // ---- B6 负分 ⇒ 红 ----
            panel.SetRating(-7);
            var rtLbl = panel.RatingTextForTest;
            Tr(rtLbl != null && rtLbl.color.r > 0.5f && rtLbl.color.g < 0.4f,
               "B6 ★ 负分 ⇒ 字色转红（原版换 `ratingLoseMaterial`；我们按可见结果取红。实测 "
             + (rtLbl != null ? rtLbl.color.ToString() : "null") + "）");

            // ---- B7/B8 骷髅 > 0 ⇒ 整块亮 + 只亮最左那 N 颗 ----
            panel.SetRating(0);
            panel.Show(2, 0, 2, 5, -1, -1);
            Tr(all != null && all.gameObject.activeSelf, "B7 骷髅 > 0 ⇒ `AllRewardsHolder` 整块亮");
            var sk = panel.SkullQuads;
            Tr(sk != null && sk.Length == 3 && sk[0] != null && sk[1] != null && sk[2] != null
               && sk[0].gameObject.activeSelf && sk[1].gameObject.activeSelf && !sk[2].gameObject.activeSelf,
               "B8 点亮的必须是**最左那两颗**（`i < ShownSkulls`；原版 `ShowRewards.c:55-68`）");

            // ---- B9/B10 同一个 `HorizontalLayoutGroup`：1 条奖励 + 评分 ----
            //   内容 = 140 + 33 + 186.9742 = 359.9742 < 495.2 ⇒ surplus > 0 ⇒ pos = 67.6129
            //   ⇒ RewardHolder 中心 x = 67.6129 + 70 − 247.6 = **−109.987**；HolderRating = 67.6129 + 140 + 33 + 93.4871 − 247.6 = **+86.5**
            panel.SetRewards(new List<RewardInfo> { new RewardInfo("C2", 7) });
            panel.SetRating(15);
            var row = panel.RewardRowAt(0);
            Tr(row != null && row.Root != null && Near(((RectTransform)row.Root).anchoredPosition.x, -109.987f),
               "B9 ★ 1 条奖励 + 评分 ⇒ 奖励行中心 x = **−109.987**（原版同一个布局组（spacing 33）算出来的；实测 "
             + (row != null && row.Root != null ? ((RectTransform)row.Root).anchoredPosition.x.ToString("F3") : "null") + "）"
             + "｜🧨 `LayoutRewardRows` 里漏掉评分那一项 ⇒ 会回到居中 x=0 ⇒ 红");
            Tr(panel.RatingHolderForTest != null && Near(((RectTransform)panel.RatingHolderForTest).anchoredPosition.x, 86.5f),
               "B10 ★ 同一档下评分行中心 x = **+86.5**（实测 "
             + (panel.RatingHolderForTest != null ? ((RectTransform)panel.RatingHolderForTest).anchoredPosition.x.ToString("F3") : "null") + "）");
        }
        finally { KillPanel(panel, madeCam); }
    }

    // ============================================================ C 淡入
    static void SectionC_淡入()
    {
        MenuCheck.Section(_sink, "C 奖励块淡入：原版 `DOFade(cg, 0, **1.0**).From().SetDelay(**0.15**)`");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "C0 **前提断言**：`EndPanel.Create` 非 null");
            if (panel == null) return;
            panel.Show(2, 0, 2, 5, -1, -1);       // 有骷髅 ⇒ 那一块真的会露

            Tr(Near(EndPanel.RewardsFadeSeconds, 1f),
               $"C1 淡入时长 = 原版 **1.0 s**（`0x1834b2bb8` 实读；实测 {EndPanel.RewardsFadeSeconds:F2}）");
            Tr(Near(EndPanel.RewardsFadeDelay, 0.15f),
               $"C2 起始延时 = 原版 **0.15 s**（`0x1834b2bbc`，与 `SetupDoor` 加在片长上的是**同一个常量**；实测 {EndPanel.RewardsFadeDelay:F3}）");

            panel.RestartRewardsFadeForTest();
            Tr(Near(panel.RewardsAlphaForTest, 0f), $"C3 ★ 起点 α = **0**（原版 `.From()` 从 0 补到当前值；实测 {panel.RewardsAlphaForTest:F3}）");
            panel.Advance(0.10f);
            Tr(Near(panel.RewardsAlphaForTest, 0f), $"C4 延时内（t=0.10）α 仍是 0（实测 {panel.RewardsAlphaForTest:F3}）");
            panel.Advance(0.30f);     // t = 0.40 ⇒ p = (0.40 − 0.15) / 1.0 = 0.25
            Tr(Mathf.Abs(panel.RewardsAlphaForTest - 0.25f) < 0.02f,
               $"C5 ★ 中途（t=0.40）α ≈ **0.25**（实测 {panel.RewardsAlphaForTest:F3}）"
             + "｜🧨 把时长改成 0 / 忽略延时 ⇒ 这里会是 1 / 0.40 ⇒ 红");
            panel.Advance(0.75f);     // t = 1.15 ⇒ p = 1
            Tr(Near(panel.RewardsAlphaForTest, 1f), $"C6 t=1.15 ⇒ α = **1**（实测 {panel.RewardsAlphaForTest:F3}）");
            panel.Advance(1f);
            Tr(Near(panel.RewardsAlphaForTest, 1f), "C7 到位之后不再动（α 仍是 1）");
        }
        finally { KillPanel(panel, madeCam); }
    }

    // ============================================================ D 战后出口
    static void SectionD_出口闸()
    {
        MenuCheck.Section(_sink, "D 战后出口：闸门 = 开门视频播完（片长 + 0.15）之后才开 · 跳过演出也照样开闸");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "D0 **前提断言**：`EndPanel.Create` 非 null");
            if (panel == null) return;
            panel.Show(2, 0, 1, 5, -1, -1);
            var doors = panel.Doors;
            Tr(doors != null && doors.HasVideo,
               "D1 **前提断言**：这一档有开门视频（`Resources/Art/videos/victory.webm` 在）—— 不在的话 D2/D3 没有意义");
            if (doors == null || !doors.HasVideo) return;

            Tr(!panel.ExitReady,
               "D2 ★ 视频还没播完 ⇒ 出口闸**关着**（原版 `matchFinishedAndWaitingToLeave` = `+0x510` 还没置位）");
            panel.Advance(doors.Length);
            Tr(panel.ExitReady,
               "D3 ★ 推够 `片长 + 0.15` ⇒ 闸门**开**"
             + "（原版 `_CloseBattleDoors_d__393__MoveNext.c:51-56 → :72`；`BattleManager__Update.c:105` 那句闸）");

            // ---- D4 跳过演出那一档：丢帧，但闸照开 ----
            panel.Hide();
            panel.Show(2, 0, 1, 5, -1, -1, skipSequence: true);
            Tr(doors.ScreenVisible == false && doors.FrameDropped && panel.ExitReady,
               "D4 ★ `skipSequence` ⇒ 视频那层**丢掉最后一帧**（不垫底）、但出口闸**照样开**"
             + "｜🧨 把 `Show` 里那一支的 `Advance(_doors.Length)` 删掉 ⇒ 闸门永不置位 ⇒ 真机**永久软锁**");
        }
        finally { KillPanel(panel, madeCam); }
    }
}
