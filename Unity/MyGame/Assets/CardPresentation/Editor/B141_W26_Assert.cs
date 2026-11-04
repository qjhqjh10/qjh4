// B141_W26_Assert.cs — 执行写手 `B141-W26`（第一百二十八会话 · 波次 `W129` · `B138_审计汇总_问题清单.md` §4 的 4-20 / 4-10 / 4-12）
//
// ============================ 它盯的是什么 ============================
//   · **4-20** 卡组行卡名的**左沿**：原版 **63.58**（`Deck List drawer` 行节点 x 8.4 + `Text fill` 左内缩 55.182）
//     —— 我们改前是 **68**（`DeckRuntime.RowNameX`）。
//   · **4-10** 卡组**行卡名**的**垂直档**：原版三族预制体分别是
//       `Deck Selector Hero Card Info button/Content/Card Name` → `m_VerticalAlignment = 512`（**Middle**）·
//       `Deck Selector Card Info button/Content/Background/Text fill/Card Name` → **8192（Capline）** ·
//       `Deck Selector Defensive Card Slot/Content/Text fill/Card Name` → **8192（Capline）**。
//     （判据 = `资料/完全复刻/判据/AUD02_serialized_params.tsv:122` / `:226` / `:194`，2026-11-04 现读。）
//   · **4-12** `m_margin` 的**第三入口收口**：`DeckRuntime.SetTmpMargin` 现在只是 `Label.SetMarginPx` 的薄转发，
//     业务调用 = 卡背抽屉那行 `Army` 小标题（原版 `m_margin = (22.7,0,0,0)` —— **22.7 设计 px**，与同颗 TMP 的
//     `m_fontSize = 32` 同一个局部单位；`判据/AUD02_裁定_卡组编辑.tsv:462` ＋ 原版 prefab 现读）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `Editor/DeckScene.cs` 里、**⑦ A1525③ 那一段之后**（那里卡组列表已经摆好、有 8+ 行真卡名）加：
//      B141_W26_Assert.Run(_rt); _sink.Pass += B141_W26_Assert.Pass; _sink.Fail += B141_W26_Assert.Fail;
//   ⛔ 别放在 ⑦ 之前（那时行名还没 `SetText`/`AlignLeftOn`，量出来是 0 宽）。
//
// ============================ 🧨 灭自证 ============================
//   · 4-20：期望值 = **原版 prefab 两个字面量之和**（8.4 + 55.182），**⛔ 不读 `DeckRuntime.RowNameX`**
//     （那是被测实现里的数，而且它是 `private const`、宿主根本读不到）。另加两条「改回去就红」：
//     ① 左沿与 **68** 相差 > 3px（退回 68 ⇒ 红）；② 各行**宽度不等**而左沿相等（退回居中 ⇒ 红）。
//   · 4-10：断的是**对比**——同一屏里必须**既有 Middle 行（督军）又有 Capline 行（卡/防御卡）**；
//     把三族统一改成任一档 ⇒ 红。
//   · 4-12：断 `Label.MarginPx`（= `Label._margin` 的读回口）—— **旧写法（直接写 `TMP.margin`）那里恒 0** ⇒ 红；
//     再加一条**行为等价判别式**：标题左沿必须仍是 **2.2**（把 `SetMargin` 排到 `AlignLeft` 之后 ⇒ 被重排回框心、
//     左沿 +1225.8px ⇒ 红）。
//
// 判绿红：按行首 `[B141_W26]` 的 `✓` / `✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B141_W26`：卡组行名左沿（4-20）· 行卡名垂直档（4-10）· `m_margin` 收口等价（4-12）。</summary>
public static class B141_W26_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141_W26] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    /// <summary>= `DeckScene` 那一份（`1080 / DesignHeight` = 108）—— 世界单位 ↔ 画布 px。</summary>
    const float PxPerUnit = 1080f / LayoutSpace.DesignHeight;

    // ---- 原版字面量（⛔ 不读 `DeckRuntime.RowNameX`：那是被测实现）----
    /// <summary>`Deck List drawer` 行节点在列表里的 x（原版 prefab）。</summary>
    const float OrigRowNodeX = 8.4f;
    /// <summary>`Text fill`（卡名框）相对行节点的左内缩（原版 prefab：行内 `55.182..301.083`）。</summary>
    const float OrigTextFillLeft = 55.182f;
    /// <summary>原版卡名**框**左沿的绝对 x = 8.4 + 55.182（判据 → `DeckRuntime.RowNameX` 那段 doc）。</summary>
    const float OrigRowNameX = OrigRowNodeX + OrigTextFillLeft;
    /// <summary>改前我们写死的值（4-20 要离开的那个数）。</summary>
    const float OldRowNameX = 68f;
    /// <summary>卡背 `Army` 小标题原版 `m_margin` 的左值 = **22.7 设计 px**（与同颗 TMP 的 `m_fontSize = 32` 同一个局部单位；
    /// 我们把那颗字号按设计 px 喂 ⇒ 边距同档。`判据/AUD02_裁定_卡组编辑.tsv:462` ＋ 原版 prefab 现读）。</summary>
    const float OrigArmyTitleMargin = 22.7f;

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>一段文字**渲出来的墨迹左缘**（画布 px · 左上原点）—— 口径同 `Editor/DeckScene.cs` 的
    /// `RowNameInkPx`：`PxOfWorld(节点位置).x − WorldW/2`（`RefreshBounds` 把 TMP 子节点摆在
    /// `−anchor.x·WorldW − bounds.min.x` ⇒ 墨迹左缘恒 = 节点 x − `WorldW/2`）。
    /// 量不出来（空串 / 哨兵 / 未激活）⇒ 返回 false，调用点**必红**（不静默放过）。</summary>
    static bool InkLeftPx(Label lb, out float left, out float wPx)
    {
        left = -9999f; wPx = 0f;
        if (lb == null) return false;
        wPx = lb.WorldW * PxPerUnit;
        if (!(wPx > 10f && wPx < 2000f)) return false;
        left = DeckRuntime.PxOfWorld(lb.transform.position).x - wPx * 0.5f;
        return true;
    }

    public static void Run(DeckRuntime rt)
    {
        MenuCheck.Section(_sink, "B141_W26 · 4-20 行名左沿 / 4-10 行卡名垂直档 / 4-12 margin 收口");

        // ==================== ① 4-20：行名左沿 = 63.58（原版框左沿）====================
        {
            float minL = 1e9f, maxL = -1e9f, minW = 1e9f, maxW = -1e9f;
            int n = 0, nCapline = 0, nMiddle = 0, nOther = 0;
            Label row0 = null;
            for (int i = 0; i < 11; i++)
            {
                // 与 `Editor/DeckScene.cs` 的 ⑦ 同一道前提：这一槽**真的有数据**（不可见 / 越界 ⇒ 跳过）。
                if (rt == null || rt.UiDeckRowAt(i) == null) continue;
                var t = FindDeep(rt.Root, "row_n" + i);
                var lb = t != null ? t.GetComponent<Label>() : null;
                if (i == 0) row0 = lb;
                float l, w;
                if (!InkLeftPx(lb, out l, out w)) continue;
                n++;
                if (l < minL) minL = l;
                if (l > maxL) maxL = l;
                if (w < minW) minW = w;
                if (w > maxW) maxW = w;
                // 4-10：逐行的垂直档（判据 = 原版三族 prefab 的 `m_VerticalAlignment`）
                if (lb.VAlignTier == Label.VAlign.Capline) nCapline++;
                else if (lb.VAlignTier == Label.VAlign.Middle) nMiddle++;
                else nOther++;
            }

            T(n >= 3, "（前提·4-20）一屏量到 ≥3 行行名的墨迹（实测 " + n + " 行；0 行 ⇒ 下面的结论无鉴别力）");
            N(maxL, OrigRowNameX, 0.6f,
              "★ **4-20 每一行的墨迹左缘都落在 " + OrigRowNameX.ToString("F3") + "**（原版 = 行节点 8.4 + `Text fill` 左内缩 55.182；"
            + "⛔ 期望值不是 `DeckRuntime.RowNameX`）"
            + "｜🧨 改回 68 ⇒ 红；只调 `SetAlignLeft()` 不调 `AlignLeftOn` ⇒ 整块停在框心 ⇒ 红");
            T(maxL - minL < 1f,
              "★ 4-20 各行**左缘相等**（最左 " + minL.ToString("F1") + " · 最右 " + maxL.ToString("F1")
            + "，差 " + (maxL - minL).ToString("F2") + " px）—— 这就是「左对齐」的定义（与名字长短无关）｜🧨 退回居中 ⇒ 红");
            T(Mathf.Abs(maxL - OldRowNameX) > 3f,
              "★ **4-20 灭自证**：左沿距旧值 68 超过 3px（实测 |" + maxL.ToString("F2") + " − 68| = "
            + Mathf.Abs(maxL - OldRowNameX).ToString("F2") + "）｜🧨 把 `RowNameX` 改回 68 ⇒ 本条与上一条一起红");
            T(maxW - minW > 20f,
              "★ 4-20 **鉴别力**：各行**宽度不等**（最窄 " + minW.ToString("F1") + " · 最宽 " + maxW.ToString("F1")
            + " px）—— 长短名并存，上一条「左缘相等」才不是「所有行一样宽」蒙出来的｜🧨 夹具退化成同宽 ⇒ 红");

            // ==================== ② 4-10：行卡名的垂直档（原版三族：督军 Middle / 卡·防御卡 Capline）====================
            bool heroShowing = rt != null && rt.UiDeckRowAt(0) != null && rt.UiDeckRowAt(0).Type == "hero";
            T(heroShowing, "（前提·4-10）第 0 行装的是**督军**（`UiDeckRowAt(0).Type == \"hero\"`）—— 不然「第 0 行 = Middle」没有判别力");
            if (heroShowing)
                C(row0 != null ? row0.VAlignTier : Label.VAlign.Middle, Label.VAlign.Middle,
                  "★ **4-10 督军行**：`row_n0` 的垂直档 = **Middle**（原版 `…/Hero Card Info button/Content/Card Name` 的 `m_VerticalAlignment` = **512**；"
                + "判据 `判据/AUD02_serialized_params.tsv:122`）｜🧨 三族统一改成 Capline ⇒ 红");
            T(nCapline >= 2,
              "★ **4-10 卡行 / 防御卡行**：至少 2 行的垂直档 = **Capline**（原版 `…/Card Info button/…/Text fill/Card Name` 与 "
            + "`…/Defensive Card Slot/…/Text fill/Card Name` 都是 **8192**；判据 `AUD02_serialized_params.tsv:226` / `:194`）"
            + "，实测 " + nCapline + " 行｜🧨 三族统一改成 Middle ⇒ 红");
            T(nCapline >= 1 && nMiddle >= 1,
              "★ **4-10 灭自证（对比）**：同一屏里**既要有 Middle 行、也要有 Capline 行**（实测 Middle " + nMiddle
            + " / Capline " + nCapline + "）—— 只断「某一档对了」挡不住「三族全改成同一档」｜🧨 统一档 ⇒ 红");
            T(nOther == 0,
              "★ 4-10：没有一行落进**别的档**（Top / Bottom / Midline；实测 " + nOther + " 行）—— 原版这三族只有 512 / 8192 两种");
        }

        // ==================== ③ 4-12：`m_margin` 唯一写口 = `Label.SetMargin`（等价判别式）====================
        {
            rt.UiSetTab(2);
            rt.UiToggleFilters();
            var at = FindDeep(rt.Root, "cosmoflt_title");
            var al = at != null ? at.GetComponent<Label>() : null;
            T(al != null, "（前提·4-12）卡背抽屉的 `Army` 小标题 `cosmoflt_title` 建出来了");
            if (al != null)
            {
                var mp = al.MarginPx;   // = `Label._margin × PxPerUnit`（画布 px）
                N(mp.x, OrigArmyTitleMargin, 0.5f,
                  "★ **4-12 收口**：`Label.MarginPx.x` = **22.7 设计 px**（原版 `m_margin = (22.7,0,0,0)`；`判据/AUD02_裁定_卡组编辑.tsv:462` ＋ 原版 prefab）"
                + "｜🧨 退回旧的「直接写 `TMP.margin`」写法 ⇒ `Label._margin` 恒 0 ⇒ 本条红（实得 " + mp.x.ToString("F1") + "）"
                + "｜🧨 走 `SetMargin(22.7)`（世界档单位）⇒ `MarginPx` = 2451.6 ⇒ 红");
                N(mp.y, 0f, 1e-3f, "★ 4-12：margin 上 = 0（原版同值）");
                N(mp.z, 0f, 1e-3f, "★ 4-12：margin 右 = 0（原版同值）");
                N(mp.w, 0f, 1e-3f, "★ 4-12：margin 下 = 0（原版同值）");

                float w, l;
                T(InkLeftPx(al, out l, out w), "（前提·4-12）`Army` 小标题量到了**真实宽度**（" + w.ToString("F1") + " px）");
                N(DeckRuntime.PxOfWorld(al.transform.position).x - w * 0.5f, 2.2f, 1f,
                  "★ **4-12 等价判别式**：标题左沿仍是 **2.2**（原版 `Army Filter/Title` 面板内 x=0 + 面板原点 2.2）"
                + "｜🧨 把 `SetMargin` 排到 `MenuDraw.AlignLeft` **之后** ⇒ `ForceRelayout` 把整块重新居中（+1225.8px）⇒ 红");
            }
            rt.UiToggleFilters();
            rt.UiSetTab(0);
        }
    }
}
