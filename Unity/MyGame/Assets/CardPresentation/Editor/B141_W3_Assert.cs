// B141_W3_Assert.cs — 执行写手 `B141_W3`（2026-11-04 · 第一百二十八会话 · W129 波次 · 账 `4-02` / `4-15`）
//
// ============================ 它盯的是什么 ============================
//  ① `4-02` 战役阵营条 `Campaign Army Selector ▸ Viewport ▸ Army Content` 的**布局组参数**：
//      原版 `spacing = 10` · `m_Padding = L48 / R25`；改前我们是 **−14 / 0 / 0**
//      （`ArmySpacing` 把锻造页的 `SelSpacing = −14` 抄了过来）⇒ 13 格内容宽 **1392** vs 原版 **1753**（少 361px）。
//  ② `4-15` 阵营格的**可点区**：原版 = 子树各靶各自 `m_RaycastPadding` 后取并集 ⇒ **142×132**
//      （整格 120×130，`Icon` 那颗 pad **−11 全边**、另 4 靶全 0；上沿 −6 / 下沿 +126）；改前是整格 **120×130**。
//      ⚠️ **2026-11-04（W16 续做）就地订正**：本条上一版按 `报告/B131` 的「并集 142×130」落地 —— 那是审计的**圆整值**，
//      逐颗算出来竖轴是 `−6 … 126`（**132**）。现已按真值改（`CampaignTab.ArmyHitRect`）。
//      量的时候注意：本页视口高 137、条目高 130 ⇒ 顶边只留 3.5px 余量，`RectMask2D` 会再裁掉 2.5px
//      ⇒ **实测高约 129.5 = 132 与视口交集**（原版同一个视口也会裁，不是偏差）。
//  ③ `4-15` 的**另一半（`CampaignTab.cs:1695` 的 info 悬停热区）不成立** —— 见下面「就地订正」。
//
// ============================ 判据出处（⛔ 别在这里再抄第二份数值）============================
//  · `4-02` 🥇 `d:/2/tools/assetripper_out/ExportedProject/Assets/Campaign Tab.prefab` 的 `Army Content` 段（`--- !u!114 &114630643969539439`）：
//        `m_Padding {m_Left 48, m_Right 25, m_Top 0, m_Bottom 0}` · `m_Spacing 10` · `m_ChildAlignment 3`
//        · ctrlW/expandW 0 · `ContentSizeFitter m_HorizontalFit 2 (PreferredSize)`
//    🥈 第二个独立读口：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3003792235550560883.json`
//        （与 `…_4880439462271588095.json`）逐字同值 —— 这两份是 bundle 里真跑的那份。
//    ⇒ 内容宽 = `48 + 25 + n×120 + (n−1)×10` ⇒ n=13 时 **1753**（改前 1392）。
//  · `4-15`（阵营格）🥇 `Campaign Army Item Button.prefab`：5 颗 `m_RaycastTarget = 1` 的图 ——
//        `HighlightBG`(120×110) / `Icon`(120×100 @顶边下 5) / `Badge Highlight`(35×35) /
//        `Slider/Background` 与 `Slider/Outline`（滑条带 110×20 @110..130 里锚 (0,0.2)-(1,0.8) 的 12 高芯）——
//        **只有 `Icon` 带 pad**，值是 `(−11,−11,−11,−11)`。
//        并集 = 横向 `−11 … 131`（**142**）· 竖向 `−6 … 126`（**132**）。
//        （`Badge Highlight` 那块含在 `HighlightBG` 里、`Slider/Fill Area/Fill` 是 `sizeDelta 0` 的退化矩形 ⇒ 不出并集）
//
// 🔴 **就地订正（铁律 5）**：`报告/B131_审计_UI版面与字体.md` 七·一 #12 与
//    `判据/B141_W129_对账_UI动画.md` 的 `4-15` 把 `Shell/CampaignTab.cs:1695`（info 悬停热区）
//    写成「原版 −15 全边」。**现读 prefab 不成立**：`Campaign Tab.prefab` 的 `Info Button`
//    （sz **41.1569×41.1573** · 锚 (0.5,0.5) · pos **(179.57, 28.829)** —— 与生产代码 `infoR` 逐字对上）
//    那颗 Image 的 `m_RaycastPadding` 是 **(0,0,0,0)**。−15 那 20 颗 `info` 全在 **`Missions Tab.prefab`**
//    （同图 `aae24d8f…`、同 41² 框，但那是**任务页**的件）⇒ 两页各是各的 prefab，⛔ 别互搬。
//    ⇒ 本文件 C 组断的是「**战役这颗 info ≈ 41.16² 不许被扩成 71.16²**」（防止将来有人照审计那行改错）。
//    更正明细 → `资料/完全复刻/报告/B141_W129_CampaignTab.md`
//
// ============================ 🔴 灭自证（🧨 每条「什么改动会让它红」）============================
//  · A1（前提）`Army Content` 下有条目 —— 没有它，下面几条全是空转。
//  · A2 ★ 内容宽 = `48+25+n×120+(n−1)×10`（13 格 = **1753**）｜🧨 把 pad 改回 `0f,0f` ⇒ 1670；再把 spacing 改回 −14 ⇒ **1392** ⇒ 红。
//  · A3 ★ 相邻条目中心距 = **130**（= 120 + 10）｜🧨 spacing 改回 −14 ⇒ **106** ⇒ 红。
//  · A4 ★ 条目组相对内容左沿的偏移（= padLeft **48**）｜🧨 padLeft 改回 0 ⇒ 差 48px ⇒ 红。
//  · A5 灭自证（结构性）：`contentW − 1392 > 300` —— 「只改 spacing 不改 pad」这一档会被 A2 抓住、
//       「只改 pad 不改 spacing」会被 A3 抓住；本条的判别式是「两者都改了才算过」。
//  · B2 ★ 命中区宽 = **142** = 条目宽 120 + 2×11｜🧨 去掉 `MenuDraw.PaddedRect` ⇒ 120 ⇒ 红。
//  · B3 ★ 原版并集（未裁）高 = **132**（下沿 +126 − 上沿 −6）｜🧨 上一版按审计的 130 落 ⇒ 下面两条「边」的断言必红。
//  · B4 ★ 命中区**下沿** = 条目顶边 + **126**｜🧨 沿用整格 ⇒ +130 ⇒ 红。
//  · B5 ★ 命中区**上沿** = `max(条目顶边 − 6, 视口上沿)`｜🧨 不扩 ⇒ 上沿 = 条目顶边 ⇒ 红。
//  · B6 ★ 命中区高 = 132 与视口的交集（本夹具 ≈ 129.5，视口吃掉 2.5px）。
//  · B7 灭自证（结构性）：`hitW − itemW ≈ 22`（> 20 且 < 24）—— 与 B2 互为判别式：整格照旧 ⇒ 差 0 ⇒ 红。
//  · C2 ★ info 热区宽 = **41.1569**（= info 图标自己的框宽，pad 0）｜🧨 按审计那行套 −15 ⇒ 71.16 ⇒ 红。
//  ⛔ 所有前提都单独成条（`MenuCheck.True`），⛔ 不写 `if (x != null) Check(...)` 那种「前提不成立就静默跳过」。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   ① 独立跑（自己建一扇奖励窗，跑完拆掉）：
//        unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//          -projectPath "D:\4\Unity\MyGame" -executeMethod B141_W3_Assert.Run -logFile "d:/4/_view/b141_w3.log"
//   ② 并进宿主（`Editor/RewardsScene.cs` 那一页本来就有战役页；由调度台在 W132 统一接线）：
//        B141_W3_Assert.RunCamp(win);
//        pass += B141_W3_Assert.Pass; fail += B141_W3_Assert.Fail;
//   判绿 = 合计行 `✗ 0`。⛔ 别用 `grep -c ✗`（`✗` 会出现在断言文案里），按**行首** `[B141W3] ✗` 数。
//   本文件**不改 `CampaignData`**（只切页 / 只读几何）⇒ 不需要还原段；`Run()` 自己建的窗在 `finally` 里拆掉。
using System;
using CardPresentation;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>`B141_W3`（W129 · 账 `4-02` / `4-15`）：战役阵营条 HLG（spacing 10 · pad L48/R25）+ 阵营格命中区外扩 11。</summary>
public static class B141_W3_Assert
{
    static readonly CheckSink _s = new CheckSink("[B141W3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>本趟通过条数（宿主可读）。</summary>
    public static int Pass { get { return _s.Pass; } }
    /// <summary>本趟失败条数（宿主可读）。</summary>
    public static int Fail { get { return _s.Fail; } }

    // ---- 原版值（判据见文件头；⛔ 不是从实现里读的常量）----
    const float OrigPadLeft = 48f, OrigPadRight = 25f, OrigSpacing = 10f;
    const float OrigItemW = 120f, OrigItemH = 130f;
    const float OrigIconPad = 11f;                       // `Campaign Army Item Button/Icon` 的 m_RaycastPadding = −11
    const float OrigIconDy = 5f;                         // …/Icon 的顶边下移（`m_AnchoredPosition.y = −5`）
    const float OrigHlH = 110f;                          // …/HighlightBG 高
    const float OrigSliderH = 20f, OrigBarAnchorLo = 0.2f, OrigBarAnchorHi = 0.8f;   // …/Slider（110×20）里 Background 的锚
    const float OrigInfoW = 41.1569f, OrigInfoH = 41.1573f;   // `Campaign Tab ▸ Campaign Header ▸ Info Button` 的 sizeDelta

    /// <summary>独立入口：建一台 `WindowsManager` + 一扇 `RewardsWindow`（空场景 + 相机，形状照 `B141_W7_Assert.Run`），跑完拆掉。</summary>
    public static void Run()
    {
        _s.Pass = 0; _s.Fail = 0;
        RewardsWindow own = null;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.aspect = LayoutSpace.DesignAspect;      // ⚠️ 必须在建任何东西之前定死（批处理默认 4:3）
            LayoutSpace.Apply(cam);

            var mgr = WindowsManager.EnsureHost();
            own = RewardsWindow.Create(mgr);
            mgr.OpenWindow(own);
            RunCamp(own);
        }
        catch (Exception e)
        {
            MenuCheck.True(_s, false, "这一段抛异常（后面的断言没跑到，⛔ 别读成通过）—— "
                                      + e.GetType().Name + ": " + e.Message);
        }
        finally
        {
            if (own != null) { own.Close(); UnityEngine.Object.DestroyImmediate(own.gameObject); }
            Debug.Log(_s.P + "合计：✓ " + _s.Pass + " / ✗ " + _s.Fail + "（共 " + (_s.Pass + _s.Fail) + " 条）");
        }
    }

    /// <summary>在（已有的或自建的）奖励窗里量战役页的两个参数。<paramref name="win"/> 传 `null` 就自己建一扇（跑完拆掉）。</summary>
    public static void RunCamp(RewardsWindow win = null)
    {
        _s.Pass = 0; _s.Fail = 0;
        MenuCheck.Section(_s, "W129 · 4-02 / 4-15 战役阵营条：HLG spacing 10 + pad L48/R25 · 阵营格命中区 142×132");

        bool own = false;
        int prevVisual = -1;
        try
        {
            if (win == null)
            {
                var mgr = WindowsManager.EnsureHost();
                win = RewardsWindow.Create(mgr);
                mgr.OpenWindow(win);
                own = true;
            }
            MenuCheck.True(_s, win != null, "（前提）拿到一扇 `RewardsWindow`（战役页在里面）");
            if (win == null) return;

            var tabNode = MenuCheck.FindChild(win.transform, "Campaign Tab");
            var ct = tabNode != null ? tabNode.GetComponent<CampaignTab>() : null;
            MenuCheck.True(_s, ct != null, "（前提）`Campaign Tab` 节点上挂着 `CampaignTab`");
            if (ct == null || tabNode == null) return;

            // 切到战役页 = 真的走一次 `OnOpen`（`FocusSelectedArmy`；只读，不改 `CampaignData`）
            if (win.tabButtons != null)
            {
                prevVisual = win.tabButtons.CurrentVisualIndex;
                win.tabButtons.Click(1);                     // 视觉第 2 键 = Campaign（同 `RewardsScene` §三·c 那句 `Click(1)`）
                MenuCheck.True(_s, win.CurrentTab == WindowTabType.Campaign, "（前提）已经切到战役页");
            }

            var sel = MenuCheck.FindChild(tabNode, "Campaign Army Selector");
            var content = sel != null ? MenuCheck.FindChild(sel, "Army Content") : null;
            MenuCheck.True(_s, sel != null && content != null,
                           "（前提）`Campaign Army Selector ▸ Viewport ▸ Army Content` 三件都在");
            if (sel == null || content == null) return;

            var crt = content as RectTransform;
            var srt = sel as RectTransform;
            MenuCheck.True(_s, crt != null && srt != null,
                           "（前提）两件都是 `RectTransform`（量内容宽要读 `sizeDelta`；`Node` 写进去的就是它）");
            if (crt == null || srt == null) return;
            MenuCheck.True(_s, content.childCount > 0, "（前提）`Army Content` 下至少建出一条阵营条目（视口外的按规则不建）");
            if (content.childCount == 0) return;

            float unit = LayoutSpace.Px(1f);                 // 1 设计 px 对应的本地单位（= 108 = DesignPxH / DesignHeight）
            int n = CampaignData.Armies.Length;
            float contentW = crt.sizeDelta.x / unit;
            float cx = MenuCheck.DesignPx(content).x;

            // ---------------------------------------------------------------- A · 4-02 布局组参数
            MenuCheck.Near(_s, contentW, OrigPadLeft + OrigPadRight + n * OrigItemW + (n - 1) * OrigSpacing, 0.6f,
                           "★ 内容宽 = 原版 HLG 式 `48 + 25 + " + n + "×120 + " + (n - 1) + "×10` = **"
                           + (OrigPadLeft + OrigPadRight + n * OrigItemW + (n - 1) * OrigSpacing).ToString("F0") + "**"
                           + "（实测 " + contentW.ToString("F1") + "）"
                           + "｜🧨 pad 改回 `0f,0f` ⇒ 1670；spacing 再改回 −14 ⇒ **1392** ⇒ 红");

            MenuCheck.True(_s, contentW - 1392f > 300f,
                           "★ 灭自证（结构性）：内容宽比**改前那个 1392** 宽出 > 300px（实测 "
                           + (contentW - 1392f).ToString("F1") + "）—— 「只改 spacing」会被上一条的 pad 项抓、"
                           + "「只改 pad」会被下面那条 spacing 抓，本条的判别式是**两者都改了**才算过");

            // 逐条量「哪几格真的建出来了」（视口外的整条不建 ⇒ 只能拿建出来的那些算）
            int firstIdx = -1, secondIdx = -1;
            float firstCx = 0f, secondCx = 0f;
            Transform firstItem = null;
            float bestHitW = -1f, bestHitH = -1f; float bestItemW = -1f; int bestIdx = -1;
            Transform bestItem = null, bestHitTr = null;
            for (int k = 0; k < content.childCount; k++)
            {
                var c = content.GetChild(k);
                int idx;
                if (!c.name.StartsWith("CampaignArmyItem_")
                    || !int.TryParse(c.name.Substring("CampaignArmyItem_".Length), out idx)) continue;
                float icx = MenuCheck.DesignPx(c).x;
                if (firstIdx < 0) { firstIdx = idx; firstCx = icx; firstItem = c; }
                else if (secondIdx < 0) { secondIdx = idx; secondCx = icx; }

                var h = c.Find("Hit");                        // ⚠️ `MenuCheck.FindChild` 会抓到里层同名 quad ⇒ 用**直接子件**
                var hrt = h as RectTransform;
                var irt = c as RectTransform;
                if (hrt != null && irt != null)
                {
                    float hw = hrt.sizeDelta.x / unit;
                    if (hw > bestHitW)
                    {
                        bestHitW = hw; bestHitH = hrt.sizeDelta.y / unit; bestItemW = irt.sizeDelta.x / unit; bestIdx = idx;
                        bestItem = c; bestHitTr = h;
                    }
                }
            }
            MenuCheck.True(_s, firstIdx >= 0, "（前提）至少解析出一条 `CampaignArmyItem_<i>` 的名字/次序");
            if (firstIdx < 0) return;

            // A3 ★ 相邻两格的中心距 = 120 + spacing
            MenuCheck.True(_s, secondIdx > firstIdx,
                           "（前提）建出了**两条以上**相邻条目（实测 first=" + firstIdx + " second=" + secondIdx + "）");
            if (secondIdx > firstIdx)
                MenuCheck.Near(_s, (secondCx - firstCx) / (secondIdx - firstIdx), OrigItemW + OrigSpacing, 0.5f,
                               "★ 相邻条目中心距 = **130**（= 条目宽 120 + `m_Spacing` 10）"
                               + "｜🧨 spacing 改回 −14 ⇒ **106** ⇒ 红");

            // A4 ★ padLeft = 48（量行为：条目左沿相对内容左沿的偏移，⛔ 不读实现里的常量）
            var sc = ct.ArmyScroll;
            MenuCheck.True(_s, sc != null, "（前提）阵营条的滚动区建了（`CampaignTab.ArmyScroll`）");
            float off = sc != null ? sc.Offset : 0f;
            float contentLeft = cx - contentW * 0.5f;          // 内容左沿（未含滚动偏移）
            float itemLeft = firstCx - OrigItemW * 0.5f;        // 条目左沿（已含滚动偏移）
            MenuCheck.Near(_s, itemLeft - (contentLeft - off + firstIdx * (OrigItemW + OrigSpacing)), OrigPadLeft, 0.6f,
                           "★ 条目左沿 − 内容左沿 − 序号步进 = **padLeft 48**（实测 "
                           + (itemLeft - (contentLeft - off + firstIdx * (OrigItemW + OrigSpacing))).ToString("F2") + "）"
                           + "｜🧨 `HorizontalChild(..., 0f, ...)`（padLeft 传 0）⇒ 0 ⇒ 红");

            // ---------------------------------------------------------------- B · 4-15 阵营格命中区
            MenuCheck.True(_s, bestHitW > 0f,
                           "（前提）至少有一颗阵营格里量到 `Hit` 命中区（直接子件 `Hit`；名字 idx=" + bestIdx + "）");
            if (bestHitW > 0f)
            {
                MenuCheck.Near(_s, bestHitW, OrigItemW + 2f * OrigIconPad, 0.6f,
                               "★ 阵营格命中区宽 = **142**（整格 120 + `Icon` 的 pad −11 × 2）"
                               + "｜🧨 去掉 `MenuDraw.PaddedRect`（传整格 `r`）⇒ 120 ⇒ 红");
                MenuCheck.Near(_s, bestHitW - bestItemW, 2f * OrigIconPad, 0.6f,
                               "★ 灭自证（结构性）：命中区比**条目自己的框**宽出 **22px**（= 每边 11）—— "
                               + "实测 " + (bestHitW - bestItemW).ToString("F2") + "，条目框 " + bestItemW.ToString("F2")
                               + "｜🧨 传整格 ⇒ 差 0 ⇒ 红");

                // 竖轴：原版并集 = `Icon` 顶边下 5 − pad 11（= −6）… `Slider/Background` 芯底 110 + 20×0.8（= +126）
                float wantTopOff = OrigIconDy - OrigIconPad;                       // −6
                float wantBotOff = OrigHlH + OrigSliderH * OrigBarAnchorHi;        // 126
                MenuCheck.Near(_s, wantBotOff - wantTopOff, 132f, 0.001f,
                               "★ 原版并集（**未裁**）高 = **132** = 下沿 +126 − 上沿 −6"
                               + "（判据：`Icon` 顶边下 5 + pad −11；`Slider/Background` 锚 (0,0.2)-(1,0.8) ⇒ 110+16）"
                               + "｜🧨 上一版按审计的 130 落（竖向沿用整格 0..130）⇒ 本条仍是 132（常量），下面两条边会红");

                MenuCheck.True(_s, bestItem != null && bestHitTr != null,
                               "（前提）参考格与它的命中区都拿得到（量边要用设计帧坐标）");
                if (bestItem != null && bestHitTr != null)
                {
                    float itemTop = MenuCheck.DesignPx(bestItem).y - OrigItemH * 0.5f;
                    float selH = srt.sizeDelta.y / unit;
                    float viewTop = MenuCheck.DesignPx(sel).y - selH * 0.5f;        // 视口上沿（`RectMask2D` 会再裁一刀）
                    float hitCy = MenuCheck.DesignPx(bestHitTr).y;
                    float hitTop = hitCy - bestHitH * 0.5f;
                    float hitBot = hitCy + bestHitH * 0.5f;

                    MenuCheck.Near(_s, hitBot - itemTop, wantBotOff, 0.6f,
                                   "★ 命中区**下沿** = 条目顶边 + **126**（实测 +" + (hitBot - itemTop).ToString("F2") + "）"
                                   + "｜🧨 上一版沿用整格 ⇒ +130 ⇒ 红");
                    MenuCheck.Near(_s, hitTop, Mathf.Max(itemTop + wantTopOff, viewTop), 0.6f,
                                   "★ 命中区**上沿** = `max(条目顶边 − 6, 视口上沿)`（实测 "
                                   + hitTop.ToString("F2") + "；条目顶 " + itemTop.ToString("F2")
                                   + " · 视口上 " + viewTop.ToString("F2") + "）—— 本页视口只给 3.5px 余量 ⇒ 上沿被裁 2.5px"
                                   + "（**原版也裁**：同一个 137 高视口 + 同一个 130 高条目）"
                                   + "｜🧨 上一版上沿 = 条目顶边（不扩）⇒ 红");
                    MenuCheck.Near(_s, bestHitH, Mathf.Min(132f, (itemTop + wantBotOff) - Mathf.Max(itemTop + wantTopOff, viewTop)), 0.6f,
                                   "★ 命中区高 = **132 与视口交集**（实测 " + bestHitH.ToString("F2")
                                   + "；未裁真值 132、本夹具有 2.5px 被视口吃掉 ⇒ 约 129.5）"
                                   + "｜🧨 上一版 = 130 且下沿 +130 ⇒ 上一条与下沿那条必红");
                }
            }

            // ---------------------------------------------------------------- C · 4-15 另一半：info 热区（原版 pad = 0）
            var hdr = MenuCheck.FindChild(tabNode, "Campaign Header");
            var infoHit = hdr != null ? hdr.Find("Info Button TipHit") : null;
            MenuCheck.True(_s, infoHit != null,
                           "（前提）`Campaign Header ▸ Info Button TipHit` 在（`CampaignTab.AddInfoTooltip` 建的那颗）");
            var ihrt = infoHit as RectTransform;
            MenuCheck.True(_s, ihrt != null, "（前提）info 热区是 `RectTransform`（量 pad 要看它的 `sizeDelta`）");
            if (ihrt != null)
            {
                MenuCheck.Near(_s, ihrt.sizeDelta.x / unit, OrigInfoW, 0.6f,
                               "★ info 悬停热区宽 = **41.1569**（= `Info Button` 自己的框宽；原版 `m_RaycastPadding` = **(0,0,0,0)**）"
                               + "｜🧨 按 `报告/B131` 那行「−15 全边」去扩 ⇒ 71.16 ⇒ 红（那条判据对**任务页**成立、对战役这颗不成立）");
                MenuCheck.Near(_s, ihrt.sizeDelta.y / unit, OrigInfoH, 0.6f,
                               "★ ……高 = **41.1573**（同一条：pad 0）");
            }
        }
        finally
        {
            if (win != null && win.tabButtons != null && prevVisual >= 0 && prevVisual != win.tabButtons.CurrentVisualIndex)
                win.tabButtons.Click(prevVisual);
            if (own) { win.Close(); UnityEngine.Object.DestroyImmediate(win.gameObject); }
            Debug.Log(_s.P + "合计[4-02/4-15]：✓ " + _s.Pass + " / ✗ " + _s.Fail + "（共 " + (_s.Pass + _s.Fail) + " 条）");
        }
    }
}
