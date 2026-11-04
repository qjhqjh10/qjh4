// B141_W7_Assert.cs — 执行写手 `B141_W7`（2026-11-04 · 第一百二十八会话 · W129 波次 · 账 `4-09`）
//
// ============================ 它盯的是什么 ============================
//   锻造页阵营条（`Forge Army Selector ▸ Viewport ▸ Army Content`）的两个**布局参数**：
//     · 内容高 —— 原版 **126**（`m_SizeDelta = (0, 126)`）
//     · 布局组底内边距 —— 原版 **5**（`HorizontalLayoutGroup.m_Padding.m_Bottom = 5`）
//   改前：我们把 `_selR`（选择条矩形，高 **125.1**）直接当内容容器、且**一处 pad 都没吃**
//   ⇒ 内容高 125.1、pad 0 ⇒ 条目比原版**低 2.5px**（并且内容节点矮 0.9px）。
//
// ============================ 判据出处（⛔ 别在这里再抄第二份数值）============================
//   🥇 `d:/2/tools/assetripper_out/ExportedProject/Assets/Forge Tab.prefab` 的 `Army Content` 段（逐字现读）：
//        RectTransform: m_AnchorMin = m_AnchorMax = `(0.5, 1)` · m_Pivot = `(0.5, 0.5)`
//                       m_AnchoredPosition = `(0, −63)` · **m_SizeDelta = `(0, 126)`**
//        HorizontalLayoutGroup: m_Padding = `{Left 0, Right 0, Top 0, Bottom 5}` · m_ChildAlignment = 4 (MiddleCenter)
//                               · m_Spacing = −14 · ctrlW/ctrlH/expandW/expandH 全 0
//        ContentSizeFitter: m_HorizontalFit = 2 (PreferredSize) · m_VerticalFit = 0
//   🥈 第二个独立来源（同一份数据的另一条读口）：`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Forge Tab" --depth 8`
//        ⇒ `Army Content  0.00×126.00  HorizontalLayoutGroup,… spacing=-14.0 align=4 pad=0,0,0,5`
//   ⇒ 几何后果（本文件量的是这个，不是常量）：内容 **顶边与选择条顶边重合**（`pos.y = −63 = −126/2` 且 pivot 居中），
//     条目在「126 − 5 = 121」那一格里 `MiddleCenter` ⇒ **条目中心比内容中心高 2.5px**。
//
// 🔴 **就地订正（铁律 5）：`判据/B141_W129_对账_UI动画.md` 的 W8 包与 `报告/B138_审计汇总_问题清单.md` 的 `4-09`
//    把原版内容高写成了 130 —— 那是【别的 prefab 的 Army Content】**（`assets_full` 里 15 个同名实例中有 6 个
//    是 `(0.5,0.5)` / `pos=(6.1e-05,0)` / `sz=(0,130)`，属于别的页；Forge 这一族 3 个实例**全部**是
//    `(0.5,1)` / `pos=(0,−63)` / `sz=(0,126)`）。⇒ 本条按 **126** 复刻，⛔ 别按 130 改（那是把别页的值搬过来）。
//    更正明细见 `资料/完全复刻/报告/B141_W129_ForgeTab.md`。
//
// ============================ 🔴 灭自证（🧨 每条「什么改动能让它红」）============================
//   · A1（前提）选择条高 = 125.1 —— 没有它，「内容高 126」与「拿选择条高当内容高」分不清。
//   · A2 ★ 内容高 = **126** ｜🧨 改回 `UguiLayout.HorizontalContentCentered(_selR, …)`（旧写法 ⇒ 125.1）⇒ 红。
//   · A3 ★ 内容顶边 = 选择条顶边 ｜🧨 把内容盒写成「居中于选择条」（`_selR.CY ± 63`）⇒ 顶边低 0.45px ⇒ 红
//        （所以容差钉 0.1 —— 0.5 的容差会让这种写法蒙混过关）。
//   · A4 ★ pad bottom = **5**（量法 = 条目中心与内容中心的差 `Δ`，`padB = 2Δ`，**不读任何常量**）
//        ｜🧨 去掉 `_armyContentR.y2 - ArmyPadBottom`（pad 回到 0）⇒ 条目中心 = 内容中心 ⇒ Δ=0 ⇒ 红。
//   · A5 ★ 灭自证（结构性）：`Δ > 1.5px` —— 与 A4 互为判别式：**「pad=0 且内容高也改对」这一档** A2 会绿、
//        A4/A5 必红；**「pad 对但高度还是 125.1」** A4/A5 会绿、A2 必红。两条路都堵死。
//   · A6/A7（回归）内容宽 1604.68 + 滚动极值 ±265.16 —— 只动了 y，**横向与可滚动区域一个字都不许变**。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   ① 独立跑（自己建一扇奖励窗，跑完拆掉）：
//        unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//          -projectPath "D:\4\Unity\MyGame" -executeMethod B141_W7_Assert.Run -logFile "d:/4/_tmp_view/b141_w7.log"
//   ② 并进 `Editor/RewardsScene.cs`（那一页本来就有锻造页）：
//        B141_W7_Assert.RunForge(win);
//        pass += B141_W7_Assert.Pass; fail += B141_W7_Assert.Fail;
//   判绿 = 合计行 `✗ 0`。⛔ 别用 `grep -c ✗`（`✗` 会出现在断言文案里），按**行首** `[B141W7] ✗` 数。
//   本文件**不改 `ForgeData`**（只切页 / 只读几何）⇒ 不需要还原段；`Run()` 自己建的窗在 `finally` 里拆掉。
using System;
using CardPresentation;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>`B141_W7`（W129 · 账 `4-09`）：锻造阵营条 `Army Content` = 内容高 **126** + pad bottom **5**（条目上移 2.5px）。</summary>
public static class B141_W7_Assert
{
    static readonly CheckSink _s = new CheckSink("[B141W7] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>本趟通过条数（宿主可读）。</summary>
    public static int Pass { get { return _s.Pass; } }
    /// <summary>本趟失败条数（宿主可读）。</summary>
    public static int Fail { get { return _s.Fail; } }

    /// <summary>独立入口：建一台 `WindowsManager` + 一扇 `RewardsWindow`（空场景 + 相机，形状照 `RewardsScene.Build`），跑完拆掉。</summary>
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
            RunForge(own);
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

    /// <summary>宿主已有的奖励窗里量锻造阵营条。<paramref name="win"/> 传 `null` 就自己建一扇（跑完拆掉）。</summary>
    public static void RunForge(RewardsWindow win = null)
    {
        _s.Pass = 0; _s.Fail = 0;
        MenuCheck.Section(_s, "W129 · 4-09 锻造阵营条：`Army Content` 内容高 126 + pad bottom 5（原版 Forge Tab.prefab）");

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
            MenuCheck.True(_s, win != null, "（前提）拿到一扇 `RewardsWindow`（锻造页在里面）");
            if (win == null) return;

            var tabNode = MenuCheck.FindChild(win.transform, "Forge Tab");
            var ft = tabNode != null ? tabNode.GetComponent<ForgeTab>() : null;
            MenuCheck.True(_s, ft != null, "（前提）`Forge Tab` 节点上挂着 `ForgeTab`");
            if (ft == null || tabNode == null) return;

            // 切到锻造页 = 真的走一次 `OnOpen`（`Refresh` / `FocusSelectedArmy` / `FocusClaimable`；只读，不改 `ForgeData`）
            if (win.tabButtons != null)
            {
                prevVisual = win.tabButtons.CurrentVisualIndex;
                win.tabButtons.Click(2);                     // 视觉第 3 键 = Forge（同 `B103F4_Assert` 那条口径）
                MenuCheck.True(_s, win.CurrentTab == WindowTabType.Forge, "（前提）已经切到锻造页");
            }

            // ⚠️ 搜索**必须从 `Forge Tab` 子树起** —— 战役页（`Campaign Tab`）里也有一个同名 `Army Content`
            var sel = MenuCheck.FindChild(tabNode, "Forge Army Selector");
            var content = sel != null ? MenuCheck.FindChild(sel, "Army Content") : null;
            MenuCheck.True(_s, sel != null && content != null,
                           "（前提）`Forge Army Selector ▸ Viewport ▸ Army Content` 三件都在");
            if (sel == null || content == null) return;

            var crt = content as RectTransform;
            var srt = sel as RectTransform;
            MenuCheck.True(_s, crt != null && srt != null,
                           "（前提）两件都是 `RectTransform`（量内容高要读 `sizeDelta`；`Node` 写进去的就是它）");
            if (crt == null || srt == null) return;

            float unit = LayoutSpace.Px(1f);                 // 1 设计 px 对应的本地单位（= 108 = DesignPxH / DesignHeight）
            float contentH = crt.sizeDelta.y / unit;
            float contentW = crt.sizeDelta.x / unit;
            float selH = srt.sizeDelta.y / unit;

            // ---- ① 前提：选择条自己那一档就是 125.1（不是内容高）
            MenuCheck.Near(_s, selH, 125.1f, 0.5f,
                           "（前提）选择条高 = 原版 **125.1**（`Forge Army Selector` sz=(−514.955, 125.1)）—— 它**不是**内容高");

            // ---- ② 内容高 = 126
            MenuCheck.Near(_s, contentH, 126f, 0.2f,
                           "★ `Army Content` 内容高 = 原版 **126**（`m_SizeDelta=(0,126)`）"
                           + "｜🧨 改回 `HorizontalContentCentered(_selR, …)`（旧写法，拿选择条 125.1 当容器）⇒ 红");

            // ---- ③ 内容顶边 = 选择条顶边（原版 a=(.5,1) + pos.y = −126/2）
            float contentTop = MenuCheck.DesignPx(content).y - contentH * 0.5f;
            float selTop = MenuCheck.DesignPx(sel).y - selH * 0.5f;
            MenuCheck.Near(_s, contentTop, selTop, 0.1f,
                           "★ `Army Content` **顶边 = 选择条顶边**（`a=(.5,1)` · `pos=(0,−63)=−126/2` · pivot 居中 ⇒ 顶边重合）"
                           + "｜🧨 把内容盒写成「居中于选择条」（`_selR.CY ± 63`）顶边会低 0.45px ⇒ 红");

            // ---- ④⑤ pad bottom = 5（量行为，不读常量）
            MenuCheck.True(_s, content.childCount > 0, "（前提）`Army Content` 下至少建出一条阵营条目（视口外的按规则不建）");
            if (content.childCount == 0) return;

            var item = content.GetChild(0);
            var irt = item as RectTransform;
            float itemH = irt != null ? irt.sizeDelta.y / unit : 0f;
            MenuCheck.Near(_s, itemH, 121.59f, 0.6f,
                           "（前提）条目高 = 121.59（`Forge Army Item Button` 根；pad 的观测量纲靠它）");

            // y 轴向下（`MenuCheck.DesignPx` = 设计帧左上原点）⇒ 条目在内容中心**上方**时差为正
            float dy = MenuCheck.DesignPx(content).y - MenuCheck.DesignPx(item).y;
            float padB = dy * 2f;                             // 「高 − padB」那一格的中心比整高中心高 padB/2
            MenuCheck.Near(_s, padB, 5f, 0.3f,
                           "★ 布局组 **pad bottom = 5**（原版 `m_Padding.m_Bottom=5`）—— 量法：条目中心比内容中心高 `padB/2`，"
                           + "`padB = 2 × Δ`（⛔ 不读实现里的常量）｜🧨 去掉 `_armyContentR.y2 − ArmyPadBottom` ⇒ Δ=0 ⇒ 红");
            MenuCheck.True(_s, dy > 1.5f,
                           "★ 灭自证（结构性）：条目中心确实比内容中心**高 ≥1.5px**（实测 " + dy.ToString("F2") + "px）"
                           + "｜🧨 与上一条互为判别式 —— 「高度改对但 pad 仍 0」这一档上面 A2 会绿、本两条必红");

            // ---- ⑥⑦ 回归：只动 y ⇒ 横向与可滚动区域的几何一个字都不许变
            MenuCheck.Near(_s, contentW, 1604.68f, 0.6f,
                           "★（回归）内容宽仍 = **1604.68**（13×136.36 + 12×(−14)）—— 改底内边距**不许**碰横向");
            var sc = ft.ArmyScroll;
            MenuCheck.True(_s, sc != null, "（前提）阵营条的滚动区建了（`ForgeTab.ArmyScroll`）");
            if (sc != null)
            {
                MenuCheck.Near(_s, sc.MaxOffset, 265.16f, 1f,
                               "★（回归）右极值 = 内容右(1927.69) − 视口右(1662.53) = **265.16px**（原值，⛔ 不因 pad 变）");
                MenuCheck.Near(_s, sc.MinOffset, -265.16f, 1f,
                               "★（回归）左极值 = 内容左(323.01) − 视口左(588.17) = **−265.16px**");
            }
        }
        finally
        {
            if (win != null && win.tabButtons != null && prevVisual >= 0 && prevVisual != win.tabButtons.CurrentVisualIndex)
                win.tabButtons.Click(prevVisual);
            if (own) { win.Close(); UnityEngine.Object.DestroyImmediate(win.gameObject); }
            Debug.Log(_s.P + "合计[4-09]：✓ " + _s.Pass + " / ✗ " + _s.Fail + "（共 " + (_s.Pass + _s.Fail) + " 条）");
        }
    }
}
