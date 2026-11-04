// B141_W75_Assert.cs — 执行写手 `B141-W75`（2026-11-04 · 第一百二十八会话 · 批次 B141）
// 任务 = W129 `B141-DRAFT-BATTLE-STRUCT`：轮抽 Ongoing 页那颗开战钮「要不要改回 `Battle Button Button`」。
//
// ==================================================================
//  🔴 结论（本文件钉住的就是这条判据）
// ------------------------------------------------------------------
//  **`To Battle Button`（`UI_Button_Mulligan` + 文字）就是原版【实例树】里那一颗** —— 不是缺口。
//   判据（2026-11-04 现读，`d:/2/tools/assetripper_out/ExportedProject/Assets/`）：
//    · `Draft Mode Timed Mode Window.prefab`（**真实窗口**，轮抽窗实例化的是它）
//        `:4180-4194` 根下两颗钮 = `Debug Battle Button Button` + **`To Battle Button`**
//        `:30629-30656` `To Battle Button` 的 RT：`anchor (0.5,0.5) · apos (715.9,−448) · sd 440.333×120.604`
//        `:30658-30686` Image：`UI_Button_Mulligan`（guid `b5ad344d…`）· `m_Type 0`(Simple) · `PreserveAspect 1` · 白
//        `:30688-30746` EverguildButton：`m_Transition 2`(SpriteSwap) ·
//                     HL `UI_Button_Mulligan_hover`（`4b35b9c7…`）· Pressed `UI_Button_Mulligan_Pressed`（`5c4b866c…`）·
//                     **`m_SelectedSprite = {fileID: 0}`（null）** · Disabled 也 null ·
//                     `m_onClickSound` = `ButtonClick Positive`（guid `5c7d1fc3…`）· `text` → `Button Text`
//        `:27014` 状态脚本 `DraftModeMenuOngoingState.playButton: {fileID: 114421391099281046}`
//                     —— **正好指着这颗 `To Battle Button`**（`DraftModeMenuOngoingState__OnEnable.c` 把它的
//                     `OnClick` 接到 `PlayButtonOnClick`、`OnOpen` 里 `Finished` 档 `SetActive(false)`）
//    · 同一份 prefab 的 `Button Text`（`:30794` 起）：`m_text "Battle!"` · `mTerm Demo/ToBattle` ·
//      `m_fontSize 74.25` · `m_fontSizeBase 12` · auto[10, 74.25] · `HAlign 2`(Center) · `VAlign 8192`(Capline)
//
//  🔴 而 `Draft Mode Ongoing State.prefab` 里那一套 **`Battle Button Button / Button / CircleButton`**（`:679` 起，
//     绿 `m_ColorMultiplier 2` 的 ColorTint 钮 + `40k_UI_bt_play` 圆钮）**是一份孤儿 prefab**：
//      ① bundle 里它的根 **没有父件**（`RectTransform_5945483628974801758` 的 `m_Father = 0`，而另外两份
//         `Draft Mode Ongoing State` 拷贝分别嵌在 `Draft Mode Timed Mode Window` / `Draft Mode Menu Demo` 里，
//         那两份都是 `To Battle Button`）；
//      ② `b2f2be91d4e448b43809066fdd684cf0`（该 prefab 的 guid）在 `d:/2/新解包资源/assets_full/` **全库 0 命中**；
//      ③ `decomp_full` / `out_il2csharp` 里字符串 `"Draft Mode Ongoing State"` **0 命中**。
//      ⇒ **玩家永远看不到它**。W44/W52 那两份报告把它当成「原版」是**只读了 AssetRipper 的导出、没核哪一份
//      实例挂在活窗口上**（铁律 5 的就地订正）。**⛔ 照它建 = 与玩家看到的不一致**（铁律 11 例外①：原版本身不显示）。
//
// ==================================================================
//  调用方式（主对话在某个宿主收尾处加两行，想并进总数就带后一行）
// ------------------------------------------------------------------
//      B141_W75_Assert.Run();
//      pass += B141_W75_Assert.Pass; fail += B141_W75_Assert.Fail;
//  ⚠️ 判绿红按行首 `B141W75 ✓` / `B141W75 ✗` 数（⛔ 别 `grep -c ✗`）。
//  ⚠️ 它**会建一次 `DraftModeWindow`**（与 `DraftAssertD3` 同一个夹具、同一个收尾还原），跑完自动销毁。
//
//  灭自证（🧨 什么坏法会让哪条红）
//   · 有人照孤儿 prefab 补建 `Battle Button Button` / `CircleButton` ⇒ **A3 / A4 红**（各扫两处：钮下 + 整窗）。
//   · 把底图改成 `40k_bt_underbutton`（孤儿 prefab 那颗绿钮的底）⇒ **A8 红**。
//   · 把 `SpriteSwap` 的悬停/按下图换成别的（例如孤儿那颗 `40k_UI_bt_play` 一族）⇒ **A9 / A10 红**。
//   · 给这颗绑选中图（原版 `m_SelectedSprite` = null）⇒ **A11 红**。
//   · 改回硬编码英文 `Battle!`、不用 `Demo/ToBattle` 词条 ⇒ **A6 红**。
//   · 把整颗挪位置 / 改尺寸 ⇒ **A12 红**（期望值是 prefab 那几个字段算出来的，不是抄我们的常量）。
// ==================================================================
using System;
using CardPresentation;
using UnityEngine;

public static class B141_W75_Assert
{
    const string P = "B141W75 ";
    const int TestSeed = 20261104;

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        DraftModeWindow win = null;

        // 现场保存（同 `DraftAssertD3`：本夹具会开窗 ⇒ 几个静态通道跑完必须原样还回去）
        var prevMode = BattleDriver.PendingPlayMode;
        var prevPre = PrebuiltDecks.PendingSource;
        var prevRun = DraftModeWindow.RunForTest;

        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "前提：拿得到 `WindowsManager`")) return;
            win = DraftModeWindow.Create(wm, TestSeed);
            if (!Ok(win != null, "前提：`DraftModeWindow.Create(wm, seed)` 建出窗来")) return;

            // 默认开在 Pay 页；`GoToState` 里那句 `SetOn(_toBattleNode, !finished)` 决定它显不显示
            // —— 要量命中区（`A12`）就必须先站到 Ongoing 页。
            win.GoToState(DraftModeState.OngoingRun);

            var btn = win.ToBattleNode;
            if (!Ok(btn != null, "前提：`ToBattleNode` 拿得到（Ongoing 页建出来了）")) return;
            if (!Ok(btn.gameObject.activeInHierarchy, "前提：`To Battle Button` 在 Ongoing 页是激活的（`GoToState` 那支）")) return;

            // ---------------- A 组：节点树 = 原版【实例树】那一份（`Draft Mode Timed Mode Window.prefab`） ----------------
            Eq(btn.name, "To Battle Button",
               "A1 节点名 = `To Battle Button`（判据 `Draft Mode Timed Mode Window.prefab:4180-4194`；"
             + "教学/`PlayRankedButton` 那一条按名字找它 ⇒ ⛔ 别改名）");

            Ok(btn.parent != null && btn.parent.name == "Draft Mode Ongoing State",
               "A2 父件 = `Draft Mode Ongoing State`（原版这颗直接挂在状态页根下、不是挂在 `Battle Button Button` 里）"
             + (btn.parent != null ? "（实得 `" + btn.parent.name + "`）" : "（实得 无父件）"));

            Ok(Find(btn, "Battle Button Button") == null,
               "A3 这颗钮**下面没有** `Battle Button Button`（孤儿 prefab `Draft Mode Ongoing State.prefab:679` 那一级；"
             + "🧨 照它补建 ⇒ 本条红）");

            Ok(Find(btn, "CircleButton") == null,
               "A4 这颗钮**下面没有** `CircleButton`（孤儿 prefab 的 `40k_UI_bt_play` 圆钮；🧨 照它补建 ⇒ 本条红）");

            Ok(Find(win.transform, "Battle Button Button") == null,
               "A5 **整个窗里**都没有 `Battle Button Button`（不只是这颗钮下面 —— 孤儿 prefab 换掉的是整颗）");

            // ---------------- 文案 ----------------
            var tx = Find(btn, "Button Text");
            if (!Ok(tx != null, "A6 前提：`Button Text` 子件在（原版 `:4197-4218`）")) return;
            var lb = tx.GetComponent<Label>();
            if (!Ok(lb != null, "A6 前提：`Button Text` 上挂着 `Label`")) return;

            string term = Loc.T("Demo/ToBattle");
            Ok(!string.IsNullOrEmpty(term) && term != "Demo/ToBattle",
               "A6 `Demo/ToBattle` 词条查得到（⛔ 不许印键名）");
            Eq(lb.Text, term,
               "A6 文案 = 原版 `mTerm Demo/ToBattle`（`:30794` 起；🧨 写死英文 `Battle!` ⇒ 本条红）");
            Ok(Mathf.Abs(lb.FontPxNow - 74.25f) <= 1.0f,
               "A7 字号 ≈ 原版 `m_fontSize 74.25`（实得 " + lb.FontPxNow.ToString("F2") + "px；"
             + "🧨 改回旧的 42 或拿别的钮那档 ⇒ 本条红）");

            // ---------------- 底图 + 四态（`m_SpriteState`） ----------------
            var bg = Find(btn, "Bg");
            if (!Ok(bg != null, "A8 前提：底图件 `Bg` 在")) return;
            var bgq = bg.GetComponent<ImageQuad>();
            var artNormal = CardArt.MenuUi("UI_Button_Mulligan");
            // ⚠️ `!= null` 必须显式写：图取不到时两边都是 `null`，"== " 会**静默通过**（空对空 = 假绿）
            Ok(bgq != null && bgq.Texture != null && artNormal != null && bgq.Texture == artNormal,
               "A8 底图 = `UI_Button_Mulligan`（原版 Image sprite guid `b5ad344d…`，`:30658-30686`；"
             + "🧨 换成孤儿那颗的 `40k_bt_underbutton` ⇒ 本条红）");

            var hit = Find(btn, "Hit");
            if (!Ok(hit != null, "A9 前提：命中区 `Hit` 在")) return;
            var wb = hit.GetComponent<WindowButton>();
            if (!Ok(wb != null, "A9 前提：`Hit` 上挂着 `WindowButton`")) return;

            var artHover = CardArt.MenuUi("UI_Button_Mulligan_hover");
            var artPressed = CardArt.MenuUi("UI_Button_Mulligan_Pressed");
            Ok(wb.NormalTexForTest != null && wb.NormalTexForTest == artNormal,
               "A9 常态图 = `UI_Button_Mulligan`（`m_Type 0` Simple · `PreserveAspect 1`）");
            Ok(wb.HoverTexForTest != null && wb.HoverTexForTest == artHover,
               "A10 悬停图 = `UI_Button_Mulligan_hover`（原版 `m_HighlightedSprite` guid `4b35b9c7…`）");
            Ok(wb.PressedTexForTest != null && wb.PressedTexForTest == artPressed,
               "A10 按下图 = `UI_Button_Mulligan_Pressed`（原版 `m_PressedSprite` guid `5c4b866c…`）");
            Ok(wb.SelectedTexForTest == null,
               "A11 **不绑选中图** —— 原版这颗 `m_SelectedSprite = {fileID: 0}`（`:30704` 那一行；"
             + "W44 表里那条 `40k_UI_bt_play` 属于孤儿 prefab 的 `CircleButton`）");

            // ---------------- 版面（期望值 = prefab 字段算出来的屏幕像素，⛔ 不是抄我们的常量） ----------------
            Vector2 c, h; ImageQuad hq;
            if (!Ok(PointerLayer.HitBoxForTest(wb, out c, out h, out hq),
                    "A12 前提：这颗钮的命中区量得到（窗开着）")) return;
            Near(c.x, 1675.90f, 0.6f, "A12a 命中区中心 x = 1675.90px（`apos 715.9` + 屏心 960）");
            Near(c.y, 988.00f, 0.6f, "A12b 命中区中心 y = 988.00px（`apos −448` 从屏心 540 起算、y 向下）");
            Near(h.x * 2f, 440.333f, 0.8f, "A12c 命中区宽 = 440.333px（原版 `m_SizeDelta.x`）");
            Near(h.y * 2f, 120.604f, 0.8f, "A12d 命中区高 = 120.604px（原版 `m_SizeDelta.y`）");

            // ---------------- B 组：口径 / 灭自证 ----------------
            // ⚠️ 断「**这一颗**的三张图都取到了」，⛔ 不断整张 `MissingSwapArt` 为空 ——
            //    那张表是**全工程**累计的，别的窗缺图会把本条弄成假红（同族教训：拿全局表当局部判据）。
            Ok(!WindowButton.MissingSwapArt.Contains("UI_Button_Mulligan → UI_Button_Mulligan_hover")
               && !WindowButton.MissingPressedArt.Contains("UI_Button_Mulligan → UI_Button_Mulligan_Pressed"),
               "B1 这一颗的悬停/按下图**都取到了**（两张缺图表里都没有它；"
             + "🧨 删掉 `Resources/Art/` 里任一张 ⇒ 本条红）");
            Eq(wb.clickCue, WindowSounds.PositiveCue,
               "B2 点击音 = `ButtonClick Positive`（原版 `m_onClickSound` guid `5c7d1fc3…`）");
            Ok(wb.SelectedTexForTest == null
               && !WindowButton.MissingSelectedArt.Contains("[selected] 40k_UI_bt_play"),
               "B3 没人在这一颗上「顺手」绑了孤儿 prefab 的选中图（`MissingSelectedArt` 里也没有它）");
        }
        catch (Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            if (win != null) { try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            DraftModeWindow.ClearRunForTest();                     // ⛔ 别把自检那一局留给下一次真 Play
            WindowsManager.ClearReuseCacheForTest();
            DraftPacks.InvalidateCache();
            PrebuiltDecks.ClearPendingBattleDeck();
            if (prevPre != null) PrebuiltDecks.SetPendingBattleDeck(prevPre);
            BattleDriver.TakePendingPlayMode();                    // 读一次就清（`LaunchBattle` 放进去的那一格）
            if (prevMode.HasValue) BattleDriver.SetPendingPlayMode(prevMode.Value);
            if (prevRun != null) DraftModeWindow.SetRunForTest(prevRun);   // 万一外面本来有一局，还回去
            Debug.Log(P + "=== B141_W75 合计：✓ " + Pass + " / ✗ " + Fail + "（共 " + (Pass + Fail) + " 条） ===");
        }
    }

    // ============================================================ 小工具
    /// <summary>整棵子树按名字找（含未激活）——`Transform.Find` 只找**直接子件**，这里要用全树。</summary>
    static Transform Find(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name == name) return all[i];
        return null;
    }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq<T>(T got, T want, string msg)
    {
        return Ok(Equals(got, want), msg + "（实得 " + got + " / 期望 " + want + "）");
    }

    static bool Near(float got, float want, float tol, string msg)
    {
        return Ok(Mathf.Abs(got - want) <= tol,
                  msg + "（实得 " + got.ToString("F2") + " / 期望 " + want.ToString("F2") + " ± " + tol.ToString("F2") + "）");
    }
}
