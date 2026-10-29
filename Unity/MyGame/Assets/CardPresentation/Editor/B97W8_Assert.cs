// B97W8_Assert.cs — 执行代理 `B97_W8`（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `日常/奖励:155` = 原版 `EndBattleRewardHolder.SetReward(RewardInfo)`
//   —— 结算面板的**奖励行**（原版 `EndBattlePanel/AllRewardsHolder/RewardsHolder[/RewardHolder]`）。
//
//   判据（⛔ 别在这里抄第二份）→ `CardPresentation/Battle/EndPanel.cs` 类尾那一节的长注释
//   （解包目录 `bundle_scenes_scenes_battlearena1/`：`RectTransform_3458`(RewardsHolder) / `_3481`(RewardHolder) /
//    `_2954`(DrawerHolder) / `_3139`(Quantity) + `MonoBehaviour_4801`(底板 Image) / `_4113`(HorizontalLayoutGroup) /
//    `_3928`(Quantity 的 TMP)）· 填充逻辑 `decomp_full/EndBattleRewardHolder__SetReward.c` ·
//   显隐 `decomp_full/EndBattleDoors__ShowRewards.c`。
//   ⚠️ **本文件里的期望值全部是从上面那些解包字段算出来的**（不是拿我们自己的常量反算 —— 那样就是自证）。
//
// ============================ 怎么用（宿主）============================
// `B97W8Assert.Run();`
//   + 宿主里两行：`_sink.Pass += B97W8Assert.Pass; _sink.Fail += B97W8Assert.Fail;`
//   · 🔴 **推荐宿主 = `Editor/BattleScene.cs`（`BattleScene.Run`）** —— 它本来就有真 `HudCanvas`（`LayoutSpace.Cam` 已 `Apply`）。
//     C 段要调 `EndPanel.Show(...)`（会碰开门视频那一支）⇒ 只有战斗宿主跑得起来。
//   · 本文件**不建持久资产**：建的 `BattleDoors` 整棵（含临时 `EndPanel`）在 `finally` 里 `DestroyImmediate`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A9：把 `LayoutRewardRows` 里的 `RewardRowSpacing` 改回 0 ⇒ A9 红（两条行会叠在一起）。
//   · A22/A23：把 `FillRewardRow` 里的数量写死成 `"1"` ⇒ A22/A23 红（两条奖励喂的是 7 / 250）。
//     把 `SetFontSize(QuantityFontPx)` 那行删掉 ⇒ A20 红（字号会掉回档位值，不是原版 40）。
//   · A4：把 `RewardRowLocalY` 换回 `RewardRowAnchorY`（−106.88，那是**锚在容器顶边**的口径）⇒ A4 红（整行低 50 px）。
//   · B2：把 `ApplyRewardVisibility` 里那句 `SetActive(count > 0)` 删掉 ⇒ B2 红（0 条时行还亮着）。
//   · B3：把那句改成恒 `SetActive(false)` ⇒ B3 红（有奖励也藏）。
//   · C2：把 `SetVisible` 末尾新增的 `if (v) ApplyRewardVisibility();` 删掉 ⇒ C2 红
//     （`SetVisible(true)` 会把所有子件一起点亮，0 条时奖励行也被点亮）。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_W8`：结算面板奖励行（`EndBattleRewardHolder.SetReward`）的几何 / 显隐 / 文案断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W8Assert
{
    static readonly CheckSink _sink = new CheckSink("[W8] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static bool Near(float a, float b) { return Mathf.Abs(a - b) <= 0.01f; }

    // ---- 期望值（全部从解包字段来；出处见文件头）----
    /// <summary>`RectTransform_3458.m_SizeDelta`。</summary>
    const float WantRewardsRowW = 495.2f, WantRewardsRowH = 49.0f;
    /// <summary>`RectTransform_3481.m_SizeDelta`。</summary>
    const float WantRowW = 140.0f, WantRowH = 50.0f;
    /// <summary>`MonoBehaviour_4113.m_Spacing`（`RewardsHolder` 的 `HorizontalLayoutGroup`）。</summary>
    const float WantRowSpacing = 33.0f;
    /// <summary>`RectTransform_2954`：`m_AnchoredPosition (5,0)` · `m_SizeDelta (65,50)` · `m_Pivot (1,0.5)`。</summary>
    const float WantDrawerW = 65.0f, WantDrawerH = 50.0f, WantDrawerApX = 5.0f;
    /// <summary>`RectTransform_3139.m_SizeDelta.y`（框宽 = 140×0.5 + 0 = 70）。</summary>
    const float WantQtyBoxH = 45.0f;
    /// <summary>`MonoBehaviour_3928.m_fontSize`。</summary>
    const float WantQtyFontSize = 40.0f;
    /// <summary>`AllRewardsHolder` 的中心 = 面板中心**下方 260 px**（`RT_2854` 的 `m_AnchoredPosition.y`）。</summary>
    const float AllRewardsHolderApY = -260.0f;
    /// <summary>`RewardsHolder` 在**面板中心**下的行心 = −260 + 50（容器半高）− 106.88（布局组 `ap.y`）= **−316.88**。
    /// 这就是 `EndPanel.RewardRowPanelY` 那个常量 —— 这里**独立算一遍**（⛔ 不读那个常量来证明它自己）。</summary>
    const float WantPanelCenterY = AllRewardsHolderApY + 50.0f + (-106.88f);

    public static void Run()
    {
        MenuCheck.Section(_sink, "W8 结算面板奖励行（原版 `EndBattleRewardHolder.SetReward` · `RewardsHolder` 495.2×49）");

        SectionA_几何();
        SectionB_零条与两条();
        SectionC_Show之后的显隐();
    }

    // ============================================================ 公共：建一台临时面板
    /// <summary>建一台临时 `EndPanel`（挂在一块临时 `BattleDoors` 下）。
    /// 宿主必须已有 `HudCanvas`（`LayoutSpace.Cam` 已被 `LayoutSpace.Apply` 过）。</summary>
    static EndPanel MakePanel(out Camera madeCam)
    {
        madeCam = null;
        if (LayoutSpace.Cam == null)
        {
            Debug.LogWarning("[W8] 宿主没有 `LayoutSpace.Cam` ⇒ 现场补一台临时相机（⛔ 不是跳过断言："
                           + "本文件的断言前提是「有一块真画布」，补不上就 A0 红）。");
            var camGo = new GameObject("W8_Cam");
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
            // 面板整棵（含 `BattleDoors` 外骨架）都由 `CreateEnvelope` 建在那棵临时 `BattleDoors` 下
            var d = p.Doors;
            if (d != null) Object.DestroyImmediate(d.gameObject);
            else if (p.gameObject != null) Object.DestroyImmediate(p.gameObject);
        }
        if (madeCam != null)
        {
            Object.DestroyImmediate(madeCam.gameObject);
            LayoutSpace.Cam = null;      // 临时相机的登记也要撤（⛔ 别把宿主后面的断言带进一台已销毁的相机）
        }
    }

    // ============================================================ A 几何（原版解包字段算出来的期望值）
    static void SectionA_几何()
    {
        MenuCheck.Section(_sink, "A 几何：容器 495.2×49 @ 面板中心 −316.88 · 行 140×50 / 间距 33 · `DrawerHolder` 65×50 ap(5,0) · `Quantity` 框 70×45 / 字 40px");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "A0 **前提断言**：宿主里建得出 `EndPanel`（`EndPanel.Create` 非 null）"
                            + " —— 不成立 ⇒ A1..A9 **没跑**（本文件不静默跳过）。推荐宿主 = `Editor/BattleScene.cs`");
            if (panel == null) return;

            var two = new List<RewardInfo>
            {
                new RewardInfo("C2", 7),      // 两条**数量不同**（灭自证：写死 "1" 就红）
                new RewardInfo("C3", 250),
            };
            panel.SetRewards(two);

            // ---- A1 容器存在 ----
            var row = panel.RewardsRow;
            Tr(row != null, "A1 ★ `RewardsHolder` 建出来了（原版 `AllRewardsHolder/RewardsHolder`）");
            if (row == null) return;

            // ---- A2 容器的 rect 与**面板中心**坐标 ----
            var rrt = (RectTransform)row;
            C(rrt.sizeDelta.x, WantRewardsRowW, "A2 ★ `RewardsHolder.m_SizeDelta.x` = **495.2**（`RectTransform_3458`）");
            C(rrt.sizeDelta.y, WantRewardsRowH, "A3 ★ `RewardsHolder.m_SizeDelta.y` = **49**（同上）");
            // 面板中心坐标 = `AllRewardsHolder` 的中心（−260）+ 本节点的 `ap.y`
            float panelY = AllRewardsHolderApY + rrt.anchoredPosition.y;
            Tr(Near(panelY, WantPanelCenterY),
               "A4 ★ `RewardsHolder` 行心在**面板中心**下的 y = **−316.88**"
             + $"（= `AllRewardsHolder` 中心 −260 + 容器半高 50 + 布局组 `ap.y` −106.88；实测 {panelY:F2}）"
             + "｜🧨 把 `RewardRowAnchorY`(−106.88) 直接写进中心锚的 `anchoredPosition` ⇒ 整行低 50 px ⇒ 本条红");

            // ---- A5 行数 ----
            int rowCount = 0;
            for (int i = 0; i < row.childCount; i++) if (row.GetChild(i).name == "RewardHolder") rowCount++;
            C(rowCount, 2, "A5 ★ `RewardsHolder` 下**恰 2** 个 `RewardHolder`（喂了 2 条奖励）");

            var r0 = panel.RewardRowAt(0);
            var r1 = panel.RewardRowAt(1);
            Tr(r0 != null && r1 != null, "A6 **前提断言**：两行都取得到（`RewardRowAt(0/1)` 非 null）");
            if (r0 == null || r1 == null) return;

            var rr0 = (RectTransform)r0.Root;
            var rr1 = (RectTransform)r1.Root;
            C(rr0.sizeDelta.x, WantRowW, "A7 ★ `RewardHolder.m_SizeDelta.x` = **140**（`RectTransform_3481`）");
            C(rr0.sizeDelta.y, WantRowH, "A8 ★ `RewardHolder.m_SizeDelta.y` = **50**（同上）");

            // ---- A9 行距 = 原版 `HorizontalLayoutGroup.m_Spacing` ----
            float step = rr1.anchoredPosition.x - rr0.anchoredPosition.x;
            Tr(Near(step, WantRowW + WantRowSpacing),
               $"A9 ★ 行距 = **{WantRowW + WantRowSpacing}** px（140 + spacing 33，来自 `MB_4113`；实测 {step:F2}）"
             + "｜🧨 把 spacing 改回 0 ⇒ 两条行叠在一起 ⇒ 红");

            // ---- A10 `DrawerHolder` ----
            var d0 = (RectTransform)r0.DrawerHolder;
            C(d0.sizeDelta.x, WantDrawerW, "A10 ★ `DrawerHolder.m_SizeDelta.x` = **65**（`RectTransform_2954`）");
            C(d0.sizeDelta.y, WantDrawerH, "A11 ★ `DrawerHolder.m_SizeDelta.y` = **50**（同上）");
            C(d0.anchoredPosition.x, WantDrawerApX, "A12 ★ `DrawerHolder.m_AnchoredPosition.x` = **5**（同上）");
            Tr(Near(d0.pivot.x, 1f) && Near(d0.pivot.y, 0.5f),
               $"A13 ★ `DrawerHolder.m_Pivot` = **(1, 0.5)**（实测 {d0.pivot.x:F2}, {d0.pivot.y:F2}）");

            // ---- A14 `Quantity` 的框 ----
            var q0 = r0.QuantityBox;
            Tr(q0 != null, "A14 **前提断言**：`Quantity` 框建出来了");
            if (q0 == null) return;
            Tr(Near(q0.anchorMin.x, 0.5f) && Near(q0.anchorMax.x, 1f) && Near(q0.anchorMin.y, 0.5f) && Near(q0.anchorMax.y, 0.5f),
               $"A15 ★ `Quantity` 锚 = **(0.5,0.5)-(1,0.5)**（`RectTransform_3139`；实测 ({q0.anchorMin.x:F2},{q0.anchorMin.y:F2})-({q0.anchorMax.x:F2},{q0.anchorMax.y:F2})）");
            Tr(Near(q0.pivot.x, 1f) && Near(q0.pivot.y, 0.5f),
               $"A16 ★ `Quantity.m_Pivot` = **(1, 0.5)**（实测 {q0.pivot.x:F2}, {q0.pivot.y:F2}）");
            C(q0.sizeDelta.y, WantQtyBoxH, "A17 ★ `Quantity.m_SizeDelta.y` = **45**（同上）");
            float qBoxW = (q0.anchorMax.x - q0.anchorMin.x) * WantRowW + q0.sizeDelta.x;
            C(qBoxW, 70.0f, "A18 ★ `Quantity` 的**框宽** = **70**（= 140×0.5 + sd.x 0 —— 两端锚那一半）");

            // ---- A19/A20 字号与字色（原版 `MB_3928`：`m_fontSize 40` · `m_fontColor` 白）----
            Tr(r0.Quantity != null && r1.Quantity != null, "A19 **前提断言**：两行的数量字都建出来了（拿不到 TMP 字体时是 null）");
            if (r0.Quantity == null || r1.Quantity == null) return;
            // 🔴 **2026-10-29 就地订正（`B97_W41` · 铁律 5）**：本条原来断 `FontSize == 40`——
            //    接上原版自适应后 `m_fontSize` 是 **TMP 收敛结果**（起点 36、上限 40，而框高只有 45）⇒ 恒等断言必红且断错了对象。
            //    现在改断**序列化三档 + 落在带内**：`fontSizeMin/Max = 18/40`（= 原版 `MB_3928`），`FontSize ∈ [18, 40]`。
            Tr(r0.Quantity.AutoSizing,
               "A20 ★ `Quantity` **开了自动缩放**（原版 `m_enableAutoSizing`；实测 " + r0.Quantity.AutoSizing + "）"
             + "｜🧨 关掉自适应 ⇒ 红");
            Tr(Near(r0.Quantity.FontSizeMin, 18.0f) && Near(r0.Quantity.FontSizeMax, WantQtyFontSize),
               "A20b ★ 自适应两档 = **18 / 40**（原版 `m_fontSizeMin/Max`；实测 "
             + r0.Quantity.FontSizeMin.ToString("F3") + " / " + r0.Quantity.FontSizeMax.ToString("F3") + "）"
             + "｜🧨 把档位写成 0 或别的数 ⇒ 红");
            Tr(r0.Quantity.FontSize >= 17.99f && r0.Quantity.FontSize <= WantQtyFontSize + 0.01f,
               "A20c ★ 收敛后的 `m_fontSize` **落在 [18, 40] 带内**（实测 " + r0.Quantity.FontSize.ToString("F3") + "）"
             + "｜🧨 删掉 `SetAutoFitBox` 那一行 ⇒ 掉回出厂值 ⇒ 红");
            Tr(r0.Quantity.color.r > 0.99f && r0.Quantity.color.g > 0.99f && r0.Quantity.color.b > 0.99f,
               $"A21 ★ `Quantity` 字色 = **白**（原版 `m_fontColor` 全 1；实测 {r0.Quantity.color}）");

            // ---- A22 文案 = 各自 `RewardInfo.Quantity`（灭自证：写死 1 就红）----
            C(panel.QuantityTextOf(0), 7.ToString(), "A22 ★ 第 0 条的数量文本 = 它自己的 `Quantity`（**7**）");
            C(panel.QuantityTextOf(1), 250.ToString(), "A23 ★ 第 1 条的数量文本 = 它自己的 `Quantity`（**250**）"
                                                      + "｜🧨 把数量写死成 1 ⇒ A22/A23 红");
        }
        finally
        {
            KillPanel(panel, madeCam);
        }
    }

    // ============================================================ B 0 条 ⇒ 整行藏 / 再喂回来 ⇒ 又露
    static void SectionB_零条与两条()
    {
        MenuCheck.Section(_sink, "B 显隐：0 条 ⇒ 整行（连底板）藏（原版 `EndBattleDoors__ShowRewards.c` 头一段）· 再喂回来 ⇒ 又露");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "B0 **前提断言**：`EndPanel.Create` 非 null");
            if (panel == null) return;

            var none = new List<RewardInfo>();
            panel.SetRewards(none);
            var row = panel.RewardsRow;
            Tr(row != null, "B1 **前提断言**：0 条时 `RewardsHolder` 节点**仍然在**（只是关着 —— 原版那条路也是 SetActive(false)、不是销毁）");
            if (row == null) return;
            Tr(!row.gameObject.activeSelf,
               "B2 ★ 0 条 ⇒ `RewardsHolder.gameObject.activeSelf == false`（整行连底板藏）"
             + "｜🧨 删掉 `ApplyRewardVisibility` 里那句 `SetActive(count > 0)` ⇒ 红");

            var one = new List<RewardInfo> { new RewardInfo("C2", 3) };
            panel.SetRewards(one);
            Tr(row.gameObject.activeSelf,
               "B3 ★ 再喂 1 条 ⇒ **又露出来**（灭自证：不是「建完就永久藏」）｜🧨 把那句改成恒 false ⇒ 红");
            var r0 = panel.RewardRowAt(0);
            Tr(r0 != null && r0.Root != null && r0.Root.gameObject.activeSelf,
               "B4 ★ 露出来的那一行自己是 activeSelf == true");
            C(panel.QuantityTextOf(0), 3.ToString(), "B5 复用同一行：数量文本被**重写**成新那条的 3（不是上一次的 7）");

            int rows = 0;
            for (int i = 0; i < row.childCount; i++) if (row.GetChild(i).name == "RewardHolder") rows++;
            C(rows, 1, "B6 从 2 条降到 1 条 ⇒ 多出来的 `RewardHolder` **被销毁**（原版每次 Instantiate 一批，不多留僵尸行）");

            panel.SetRewards(none);
            Tr(!row.gameObject.activeSelf, "B7 再清空 ⇒ 又藏（两态都能来回，不是一次性开关）");
        }
        finally
        {
            KillPanel(panel, madeCam);
        }
    }

    // ============================================================ C `Show()` 之后（`SetVisible` 那一刀不许把奖励行点亮）
    static void SectionC_Show之后的显隐()
    {
        MenuCheck.Section(_sink, "C `Show()` 之后：`SetVisible(true)` 会把所有子件一起点亮 ⇒ 奖励行必须按清单**重算**显隐");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "C0 **前提断言**：`EndPanel.Create` 非 null");
            if (panel == null) return;

            // 0 条 + 显示一次
            panel.SetRewards(new List<RewardInfo>());
            panel.Show(2, 0, 1, 5, -1, 20);
            var row = panel.RewardsRow;
            Tr(row != null, "C1 **前提断言**：`RewardsHolder` 存在");
            if (row == null) return;
            Tr(!row.gameObject.activeSelf,
               "C2 ★ 0 条时 `Show()` 之后奖励行**仍然藏着**"
             + "｜🧨 删掉 `SetVisible` 末尾那句 `if (v) ApplyRewardVisibility();` ⇒ `SetVisible(true)` 会把它点亮 ⇒ 红");

            // 2 条 + 再显示一次
            panel.SetRewards(new List<RewardInfo> { new RewardInfo("C2", 11), new RewardInfo("C3", 22) });
            panel.Show(2, 0, 1, 5, -1, 20);
            Tr(row.gameObject.activeSelf, "C3 ★ 2 条时 `Show()` 之后奖励行**露着**（灭自证：不是恒藏）");
            C(panel.QuantityTextOf(0), 11.ToString(), "C4 第 0 条数量文本 = 11（`Show` 之后再核一次）");
            C(panel.QuantityTextOf(1), 22.ToString(), "C5 第 1 条数量文本 = 22（同上）");
        }
        finally
        {
            KillPanel(panel, madeCam);
        }
    }
}
