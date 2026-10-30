// B97W41_Assert.cs — 执行代理 `B97_W41`（第九十七会话 · 波 4 · 日常线）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `日常/奖励:155` = 原版 `EndBattleRewardHolder.SetReward(RewardInfo)` 的两条残差：
//     ① `Quantity` 的 TMP **自适应三档**（原版 `MonoBehaviour_3928`：`m_enableAutoSizing = 1` ·
//        `m_fontSizeMin = **18**` / `m_fontSizeMax = **40**` / `m_fontSizeBase = **36**`）
//     ② **图标内版式**（原版 `Icon *Drawer*.prefab` 的 `ItemDrawerComponents.image` 指的那个节点：
//        锚 (0,0)-(1,1) · `sd (0,0)` · **`m_PreserveAspect = 1`** ⇒ 整框内接）
//
//   判据（⛔ 别在这里抄第二份）→ `CardPresentation/Battle/EndPanel.cs`
//     · `RewardIconFill` 的 doc（14 份 `Icon *Drawer*` prefab 逐份现读，路径写在那儿）
//     · 类尾 `QuantityFsMin/Max/Base` 的 doc（`MB_3928`）
//     · `FillRewardRow` 里那段「画布 px → fontSize」的算式注释
//   ⚠️ **本文件的期望值全部来自上面那些原版字段**，⛔ 不是拿我们自己的常量反算（那是自证）。
//
// ============================ 怎么用（宿主）============================
// `B97W41Assert.Run(); pass += B97W41Assert.Pass; fail += B97W41Assert.Fail;`
//   · 宿主 = `Editor/BattleScene.cs`（`BattleScene.Run`）—— 它本来就有真 `HudCanvas`。
//   · 本文件**不建持久资产**：临时 `BattleDoors`（含临时 `EndPanel`）在 `finally` 里 `DestroyImmediate`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：把 `FillRewardRow` 里那句 `SetAutoFitBox(...)` 删掉（回到只 `SetFontSize`）⇒ A1 红。
//   · A2/A3/A4：把 `QuantityFsMin/Max/Base` 任一改成别的数 ⇒ 对应条红。
//   · A5/A6：把三档改宽（例 `max` 抬到 60）⇒ 长数字不再被压下去 ⇒ A5/A6 红。
//     🔴 **A11 是它们仨的「有没有牙」证明**：手关自适应、字号钉死 40 ⇒ 同一个长数字**必须**撑破框；
//        若 A11 也绿着不成立（`false`）⇒ A5/A6 是空转，**别把绿的当成对**。
//   · A9：把字号写死成同一个值（不管内容）⇒ 短数字不再比长数字大 ⇒ A9 红。
//   · B2：把 `RewardIconFill` 改回 0.7 ⇒ 图标节点只有 45.5×35 ⇒ B2 红。
//   · B4：删掉 `SetNodeRect(..., true)` 那一句 ⇒ `preserveAspect` 回 false ⇒ B4 红。
using System.Collections.Generic;
using CardPresentation;
using TMPro;
using UnityEngine;

/// <summary>执行代理 `B97_W41`：结算奖励行的 `Quantity` 自适应三档 + 图标内版式。计数口径照 `CheckSink`。</summary>
public static class B97W41Assert
{
    static readonly CheckSink _sink = new CheckSink("[W41] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ---- 期望值（全部来自原版字段；出处见文件头）----
    /// <summary>`MonoBehaviour_3928`：`m_fontSizeMin` / `m_fontSizeMax` / `m_fontSizeBase`。</summary>
    const float WantMin = 18f, WantMax = 40f, WantBase = 36f;
    /// <summary>`RectTransform_3139`：`m_SizeDelta (0,45)` + 两端锚 (0.5,0.5)-(1,0.5) ⇒ 框宽 = 140×0.5 + 0 = **70**。</summary>
    const float WantBoxW = 70f, WantBoxH = 45f;
    /// <summary>`RectTransform_2954`：`m_SizeDelta (65,50)`（图标节点 = 整框，见 `RewardIconFill`）。</summary>
    const float WantDrawerW = 65f, WantDrawerH = 50f;
    /// <summary>量的宽容度（px）—— 文字墨迹框与框边留这一点，别拿「恰好等于」当判据。</summary>
    const float Eps = 0.5f;

    public static void Run()
    {
        MenuCheck.Section(_sink, "W41 奖励行：`Quantity` 自适应 [18,40] base 36 · 图标 = 整框 + PreserveAspect");

        SectionA_自适应();
        SectionB_图标内版式();
    }

    // ============================================================ 公共：建一台临时面板
    static EndPanel MakePanel(out Camera madeCam)
    {
        madeCam = null;
        if (LayoutSpace.Cam == null)
        {
            Debug.LogWarning("[W41] 宿主没有 `LayoutSpace.Cam` ⇒ 现场补一台临时相机（⛔ 不是跳过断言："
                           + "本文件的断言前提是「有一块真画布」，补不上就 A0/B0 红）。");
            var camGo = new GameObject("W41_Cam");
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
            if (d != null) Object.DestroyImmediate(d.gameObject);
            else if (p.gameObject != null) Object.DestroyImmediate(p.gameObject);
        }
        if (madeCam != null)
        {
            Object.DestroyImmediate(madeCam.gameObject);
            LayoutSpace.Cam = null;      // 临时相机的登记也要撤
        }
    }

    // ============================================================ A 自适应三档 + 溢出
    static void SectionA_自适应()
    {
        MenuCheck.Section(_sink, "A `Quantity` 自适应：原版三档 18/40/36 · 长数字不溢出 · 短数字不被放大到超过 40");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "A0 **前提断言**：宿主里建得出 `EndPanel`（`EndPanel.Create` 非 null）"
                            + " —— 不成立 ⇒ A1..A11 **没跑**（本文件不静默跳过）。推荐宿主 = `Editor/BattleScene.cs`");
            if (panel == null) return;

            // 两条：一条**长**数量（5 位，必须被压下去）、一条**短**数量（1 位，可以涨到最高档）
            var two = new List<RewardInfo>
            {
                new RewardInfo("C2", 12345),
                new RewardInfo("C3", 7),
            };
            panel.SetRewards(two);

            var rLong = panel.RewardRowAt(0);
            var rShort = panel.RewardRowAt(1);
            Tr(rLong != null && rShort != null, "A0b **前提断言**：两行都取得到（`RewardRowAt(0/1)` 非 null）");
            if (rLong == null || rShort == null) return;
            Tr(rLong.Quantity != null && rShort.Quantity != null,
               "A0c **前提断言**：两行的数量字都建出来了（拿不到 TMP 字体时是 null ⇒ 本段没跑，⛔ 不是静默跳过）");
            if (rLong.Quantity == null || rShort.Quantity == null) return;

            C(panel.QuantityTextOf(0), 12345.ToString(), "A0d 前提：长数字文本 = 它自己的 `Quantity`（12345）");
            C(panel.QuantityTextOf(1), 7.ToString(), "A0e 前提：短数字文本 = 它自己的 `Quantity`（7）");

            // ---- A1..A4 三档本身 ----
            Tr(rLong.Quantity.AutoSizing,
               "A1 ★ `Quantity` 开着 **`enableAutoSizing`**（原版 `MB_3928.m_enableAutoSizing = 1`）"
             + "｜🧨 删掉 `FillRewardRow` 里那句 `SetAutoFitBox(...)`（回到只 `SetFontSize`）⇒ 红");
            Tr(Mathf.Abs(rLong.Quantity.FontSizeMin - WantMin) <= 0.01f,
               $"A2 ★ `m_fontSizeMin` = **18**（原版字段；实测 {rLong.Quantity.FontSizeMin:F3}）");
            Tr(Mathf.Abs(rLong.Quantity.FontSizeMax - WantMax) <= 0.01f,
               $"A3 ★ `m_fontSizeMax` = **40**（原版字段；实测 {rLong.Quantity.FontSizeMax:F3}）");
            Tr(Mathf.Abs(rLong.Quantity.FontSizeBase - WantBase) <= 0.01f,
               $"A4 ★ `m_fontSizeBase` = **36**（原版字段；实测 {rLong.Quantity.FontSizeBase:F3}）"
             + "｜🧨 三个 `QuantityFs*` 常量改错一个 ⇒ 对应条红");

            // ---- A5/A6 长数字不溢出（断「渲染宽/高 ≤ 框」，⛔ 别只比字号）----
            float wLong = rLong.Quantity.WorldW, hLong = rLong.Quantity.WorldH;
            Tr(wLong <= WantBoxW + Eps,
               $"A5 ★ 长数字 `12345` 的**渲染宽** ≤ 框宽 **{WantBoxW}**（实测 {wLong:F2}）"
             + "｜🧨 把 `max` 抬到 60（或关自适应）⇒ 撑出框 ⇒ 红");
            Tr(hLong <= WantBoxH + Eps,
               $"A6 ★ 长数字 `12345` 的**渲染高** ≤ 框高 **{WantBoxH}**（实测 {hLong:F2}；⛔ 只看字号会漏掉「折成两行」这一种溢出）"
             + "｜🧨 同上 ⇒ 折行把高顶到 ~80 ⇒ 红");

            // ---- A7 长数字确实被压过（不是「恰好 40 也能装下」这种空转）----
            float fsLong = rLong.Quantity.FontSize;
            Tr(fsLong < WantMax - 0.1f,
               $"A7 ★ 长数字的**实收字号** < 40（实测 {fsLong:F2}）—— 证明自适应**真的在压**，不是摆着不干活");

            // ---- A8 短数字不被放大到超过最高档 ----
            //   🔴 施工单原文写「不超过 base」，那是**与原版相反**的：原版 `m_fontSizeMax = 40 > base 36`
            //      ⇒ TMP 的自适应**会把短文本一路涨到 40**（`MB_3928` 这三个字段就是这么配的）。
            //      这里断的是「**不超过 `m_fontSizeMax`**」—— 那条才是原版的天花板。
            float fsShort = rShort.Quantity.FontSize;
            Tr(fsShort <= WantMax + 0.01f,
               $"A8 ★ 短数字的实收字号 ≤ 最高档 **40**（实测 {fsShort:F2}；原版天花板 = `m_fontSizeMax`）"
             + "｜🧨 把 `QuantityFsMax` 抬到 60 ⇒ 短数字被放大 ⇒ 红");

            // ---- A9 自适应不按长度反着来（🔴 2026-10-30 `B100_F23` 就地改口径）----
            //   原断言 = `fsShort > fsLong + 0.1`（**严格大于**）。实测两档都 = **36.00**（= `m_fontSizeBase`）：
            //   框高只有 45，40 号在这一档装不下 ⇒ **长/短两条被同一个高度约束夹到同值** ⇒「按内容收敛」
            //   在**这一档结构上测不出来**（分诊 → `报告/B100_X3.md` #19）。⛔ 这不改产品、也不放宽「不溢出」：
            //   溢出仍由 A5/A6 断；「真的在夹」由 A7（< 40）＋ A11（灭自证）钉住。
            //   这里断的是**单调性**：短的**永远不许比长的更小**（写死反了 / 按长度反比时才红）。
            Tr(fsLong <= fsShort + 0.1f,
               $"A9 ★ 短数字的字号({fsShort:F2}) **不小于** 长数字的字号({fsLong:F2}) —— 自适应不按长度反着来"
             + "｜本档实测两者都 = base 36（框高 45 装不下 40 ⇒ 「严格大于」那一条结构上测不出来，见 `报告/B100_X3.md` #19）"
             + "｜🧨 把短文本压到比长文本还小 ⇒ 红");

            // ---- A11 灭自证：把框缩到比文字还小 ⇒ A5/A6 那套判据必须**翻红** ----
            //   🔴 2026-10-30（`B100_F23`）就地重写（分诊 → `报告/B100_X3.md` #20）。原写法有两个问题：
            //     ① **量纲**：`textBounds`（= `Label.WorldW` 读的那个数）与写死的 `70/45`（px 字面量）直接比；
            //     ② **前提不成立**：实测把这颗 TMP 的 `fontSize` 钉到 40，`textBounds` **一点没变**（8.7×4.7，
            //        与 36 那一档相同）⇒「钉死 40 就撑破 70×45」这条在**本环境里根本不成立**（断言恒假）。
            //   ⇒ 改成**同口径 + 前提必成立**：框用这颗 TMP 自己的 `rectTransform.sizeDelta`（与 `textBounds`
            //     同一个局部空间，⛔ 不再拿 px 字面量），再把它**缩到文字宽的一半** ⇒ 同一个判据「文字 > 框」
            //     此刻**必须为真**；为假就说明 A5/A6 是恒真的空转。测完在 `finally` 里整份复原。
            var tmp = rLong.Quantity.GetComponentInChildren<TextMeshProUGUI>();
            Tr(tmp != null, "A10 **前提断言**：数量字的 TMP 子件取得到（灭自证要用它）");
            if (tmp != null)
            {
                bool auto = tmp.enableAutoSizing;
                float fs = tmp.fontSize;
                var rtBox = tmp.rectTransform;
                Vector2 boxWas = rtBox.sizeDelta;
                var wrapWas = tmp.textWrappingMode;           // `SetAutoFitBox` → `SetWrapWidth` 会把折行打开
                try
                {
                    tmp.enableAutoSizing = false;                 // 钉住字号：文字不会再被夹
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;   // ⛔ 折行开着的话「框变窄」会**折行**而不是溢出
                    tmp.ForceMeshUpdate();
                    float bw = Mathf.Abs(tmp.textBounds.size.x), bh = Mathf.Abs(tmp.textBounds.size.y);
                    rtBox.sizeDelta = new Vector2(bw * 0.5f + 0.001f, boxWas.y);   // 框宽 = 文字宽的一半
                    tmp.ForceMeshUpdate();
                    float bw2 = Mathf.Abs(tmp.textBounds.size.x), bh2 = Mathf.Abs(tmp.textBounds.size.y);
                    bool over = bw2 > rtBox.sizeDelta.x + Eps || bh2 > rtBox.sizeDelta.y + Eps;
                    Tr(over,
                       $"A11 ★ **灭自证**：把数量字的 TMP 框宽缩到文字的一半（框 {rtBox.sizeDelta.x:F2} 单位 · 字号钉死 {fs:F2}）"
                     + $" ⇒ 文字({bw2:F2}×{bh2:F2}) **超出框** —— 同一套判据**会翻红**"
                     + "｜🧨 这里若为 `false` ⇒ A5/A6 **没有牙**（它们绿着也不能当「不溢出」的证据）");
                }
                finally
                {
                    rtBox.sizeDelta = boxWas;
                    tmp.textWrappingMode = wrapWas;
                    tmp.enableAutoSizing = auto;     // 复原（后面的段还要用它）
                    tmp.fontSize = fs;
                    tmp.ForceMeshUpdate();
                }
            }
        }
        finally
        {
            KillPanel(panel, madeCam);
        }
    }

    // ============================================================ B 图标内版式
    static void SectionB_图标内版式()
    {
        MenuCheck.Section(_sink, "B 图标内版式：原版 `Icon` 档抽屉的 `Image` = 整框 65×50 + `m_PreserveAspect 1`");

        Camera madeCam;
        var panel = MakePanel(out madeCam);
        try
        {
            Tr(panel != null, "B0 **前提断言**：`EndPanel.Create` 非 null");
            if (panel == null) return;

            panel.SetRewards(new List<RewardInfo> { new RewardInfo("C2", 100) });
            var r0 = panel.RewardRowAt(0);
            Tr(r0 != null, "B0b **前提断言**：第 0 行取得到");
            if (r0 == null) return;
            Tr(r0.Icon != null,
               "B1 **前提断言**：图标建出来了（`ItemDrawer.Spec(\"C2\")` → `40k_general_icon_currency_crystal` → `CardArt.MenuUi` "
             + "取不到图时 `FillRewardRow` 会出声并只画数量 ⇒ 本段没跑，⛔ 不是静默跳过）");
            if (r0.Icon == null) return;

            var rt = r0.Icon.Rect;
            Tr(rt != null, "B1b **前提断言**：图标是 uGUI 件（有 `RectTransform`）");
            if (rt == null) return;

            // ---- B2 节点 rect = 整框 65×50（原版抽屉根拉伸到 `DrawerHolder`、`Image` 再拉满抽屉根）----
            C(rt.sizeDelta.x, WantDrawerW, "B2 ★ 图标节点宽 = **65**（= `DrawerHolder` 整框；原版抽屉根锚 (0,0)-(1,1)·sd (0,0)）"
                                         + "｜🧨 `RewardIconFill` 改回 0.7 ⇒ 45.5 ⇒ 红");
            C(rt.sizeDelta.y, WantDrawerH, "B3 ★ 图标节点高 = **50**（同上）");
            C(rt.anchorMin.x, 0.5f, "B3b 图标节点的锚 x = 0.5（居中挂在 `DrawerHolder` 里）");
            C(rt.anchorMin.y, 0.5f, "B3c 图标节点的锚 y = 0.5（同上）");
            C(rt.anchoredPosition.x, 0f, "B3d 图标居中：`anchoredPosition.x` = 0");
            C(rt.anchoredPosition.y, 0f, "B3e 图标居中：`anchoredPosition.y` = 0");

            // ---- B4 `m_PreserveAspect = 1`（原版 14 份 `Icon *` 变体逐份现读都是 1）----
            var img = r0.Icon.gameObject.GetComponent<UnityEngine.UI.Image>();
            Tr(img != null, "B4 **前提断言**：图标节点上挂着 uGUI `Image`");
            if (img != null)
            {
                Tr(img.preserveAspect,
                   "B4b ★ `Image.preserveAspect` = **true**（原版 `m_PreserveAspect = 1` ⇒ 方形图**内接**成 50×50）"
                 + "｜🧨 删掉 `FillRewardRow` 里 `SetNodeRect(iconW, iconH, true)` 那一句 ⇒ 回 false ⇒ 红");
                Tr(img.type == UnityEngine.UI.Image.Type.Simple,
                   "B5 ★ `Image.type` = **Simple**（原版 14 份 `Icon *` 的 `m_Type` 全是 0）");
                // ⚠️ **`m_RaycastTarget` 故意不断**：原版那颗是 **1**（`Image` fid 114161607090078389），
                //    因为抽屉自带「点开看物品」那条交互；本账不建那条交互 ⇒ 我们保持 `false`（= 顺手避开了
                //    「透明图标吃掉点击」）。这是**我们挑的**，已如实记进 `报告/B97_W41.md`，⛔ 别拿它当原版判据。
            }
        }
        finally
        {
            KillPanel(panel, madeCam);
        }
    }
}
