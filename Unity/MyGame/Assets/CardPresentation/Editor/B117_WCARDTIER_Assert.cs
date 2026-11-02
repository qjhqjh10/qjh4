// B117_WCARDTIER_Assert.cs — 执行代理 `B117-WCARDTIER`（开包/轮抽卡面档 · 开包翻牌后点击进详情）
//
// ============================ 判据（一手，逐实例现读 prefab）============================
//   来源 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all`（RectTransform + MonoBehaviour 逐节点 walk，
//          含「纯 Transform 转发节点」底下那两支 —— `menu_dump` 会在纯 Transform 处不下钻、把 `Card Info` 整支丢掉）。
//   四份 `CardUI` 变体（同一套 `NameTextUnit` / `DescTextUnit` / `RaceText` / `ArmyTextUnit`）：
//     · `Draft Mode Menu Demo/…/Generic Select Card Container/Content/CardUI`（轮抽**选将格**）
//         卡名 `16.3 / max 20` · 效果 `19 / max 19` · 兵种 `17 / max 17` · 阵营 `17 / max 17`   ⇒ **Booster**
//     · `Booster Pack Open Window/…/CardInBoosterPack UI n/…/CardUI`（开包）           —— 同上 ⇒ **Booster**
//     · `Draft Mode Menu Demo/…/Generic Multi Card Display/Content/CardUI Reference`（包预览）
//     · `Draft Mode Menu Demo/…/Energy View panel/…/CardUI-Warlord`（右栏战将）
//         两份都是 卡名 `19/19` · 效果 `23.55/24` · 兵种 `18/18` · 阵营 `15/15`          ⇒ **Standard**
//     · `…/Catalog Item Shop Container/…/Card Drawer/Content/CardUI`（商店卡抽屉，5 份）
//         卡名 `16.0 / max **19**` · 效果 `23.55/24` · 兵种 `18/18` · 阵营 `15/15`
//         ⇒ **有效上界与标准档相同**（TMP autosize 只涨到 `m_fontSizeMax`）⇒ 商店**不单开一档**
//
// ============================ 为什么这不是「拿我们自己的常量自证」============================
//   期望值一律写成**原版 prefab 的 pt 字面量**（20 / 19 / 17 / 17 · 19 / 24 / 18 / 15）。
//   我们的实现在别的单位（band / em）⇒ 用**标准档那一格**折算回 pt 再比：
//       pt(本档) = 本档值 ÷ (标准档值 ÷ 标准档 pt)
//   ⇒ 若把选将格改回标准档，得 19（≠20）⇒ 红；若把标准档一并改小，折算跟着变 ⇒ 也红。
//
// ============================ 断言一览（17 条 Check 调用）============================
//   接线前置 1 · A 段（轮抽卡档）8 · B 段（开包窗点击）7 · 合计 17
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：把 `DraftModeWindow` 选将格 `CardView.Create(...)` 的 `use: CardUse.Booster` 去掉 ⇒ A1–A5 红。
//   · A6/A7：把 `IsBoosterCardView` 扩成「宿主名含 `CardUI Reference` / `CardUI-Warlord` 也当 Booster」
//            ⇒ A6/A7 红（那会把包预览与右栏战将卡一起改错 —— `B106_W9` 已警告过这一条）。
//   · B3：在 `Reveal` / `QuickOpenAll` 里加回 `SlotHits[i].gameObject.SetActive(false)`
//          ⇒ B3 红（那正是 2026-10-17 之前的状态 = 本条的缺口）。
//   · B4：把 `Back` 状态也当成可点 ⇒ B4 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/MainMenuScene.cs`（或 `ShopScene.cs`）的 `Run()` 里那一串代接线之后加：
//      B117_WCARDTIER_Assert.Run(); pass += B117_WCARDTIER_Assert.Pass; fail += B117_WCARDTIER_Assert.Fail;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B117-WCARDTIER`：轮抽选将格的 `CardUse.Booster`（A）+ 开包翻牌后点卡进详情（B）。</summary>
public static class B117_WCARDTIER_Assert
{
    const string P = "B117WCARDTIER ";

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F2") + " ± " + tol.ToString("F2") + " pt；实得 " + got.ToString("F2") + "）");
    }

    /// <summary>把我们的实现值折算回**原版 pt**（用标准档那一格当尺子 —— ⛔ 不是硬编码我们的常量）。</summary>
    static float Pt(float got, float stdGot, float stdPt)
    {
        return Mathf.Abs(stdGot) < 1e-6f ? float.NaN : got / (stdGot / stdPt);
    }

    // ---- 原版 prefab 的字面量（pt）----
    const float PtStdName = 19f, PtStdDesc = 24f, PtStdArmy = 15f, PtStdRace = 18f;
    const float PtBoostName = 20f, PtBoostDesc = 19f, PtBoostArmy = 17f, PtBoostRace = 17f;

    const int TestSeed = 20261102;

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log(P + "=== 开包 / 轮抽的卡面档 + 开包翻牌后点击进详情 ===");
        SectionA_DraftWarlordTier();
        SectionB_BoosterClick();
        Debug.Log(P + "=== 合计：" + _pass + " 通过 / " + _fail + " 失败 ===");
    }

    // ==========================================================================================
    //  A 段 —— 轮抽选将格 = `CardUse.Booster`；同一扇窗的另两格**不是**
    // ==========================================================================================
    static void SectionA_DraftWarlordTier()
    {
        Debug.Log(P + "--- A 轮抽：选将格 = Booster（20/19/17/17）；包预览 / 右栏战将 = Standard ---");

        DraftModeWindow win = null;
        CardView std = null;
        GameObject root = null;
        var prevRun = DraftModeWindow.RunForTest;
        try
        {
            var wm = WindowsManager.Get();
            Check(wm != null, "A0 **前提**：拿得到 `WindowsManager`（拿不到就建不出轮抽窗）");
            if (wm == null) return;

            win = DraftModeWindow.Create(wm, TestSeed);
            Check(win != null, "A0-b **前提**：`DraftModeWindow.Create(wm, " + TestSeed + ")` 建出窗来");
            if (win == null) return;

            win.BeginRun(TestSeed);
            win.ClickFree();
            Check(win.State == DraftModeState.SelectWarlord, "A0-c **前提**：`Free` ⇒ 选将页（实得 " + win.State + "）");
            if (win.State != DraftModeState.SelectWarlord) return;

            var cv = win.WarlordCardViewForTest(0);
            Check(cv != null, "A0-d **前提**：选将页第 1 格的 `CardView` 建出来了");
            if (cv == null) return;

            Check(cv.Use == CardUse.Booster,
                  "A1 ★ 选将格的卡走 `CardUse.Booster`（实得 " + cv.Use + "）"
                + "｜🧨 把 `CardView.Create(...)` 的 `use: CardUse.Booster` 去掉 ⇒ 回 Standard ⇒ 本行红");

            // 标准档参照卡（同一趟建、量的同一份 `CardView` 实现）
            root = new GameObject("B117WCARDTIER_ROOT");
            std = CardView.Create(root.transform, UnitData(), "B117WCARDTIER_std");
            Check(std != null, "A1-b **前提**：标准档参照卡建出来了");
            if (std == null) return;

            CheckNear(Pt(cv.NameBandMaxForTest, std.NameBandMaxForTest, PtStdName), PtBoostName, 0.05f,
                      "A2 选将格**卡名** autosize 上界折回原版 = 20 pt（`CardUI/…/NameTextUnit` `m_fontSizeMax = 20`；标准档 19）");
            CheckNear(Pt(cv.DescBandMaxForTest, std.DescBandMaxForTest, PtStdDesc), PtBoostDesc, 0.05f,
                      "A3 选将格**效果文字**上界 = 19 pt（`DescTextUnit` 19/19；标准档 23.55/24 —— 这一档比标准档**小**）");
            CheckNear(Pt(cv.ArmyFontSizeForTest(true), std.ArmyFontSizeForTest(true), PtStdArmy), PtBoostArmy, 0.05f,
                      "A4 选将格**单位卡·阵营行** = 17 pt（`ArmyTextUnit` 17/17；标准档 15 —— **变大**）");
            CheckNear(Pt(cv.RaceFontSizeForTest(true), std.RaceFontSizeForTest(true), PtStdRace), PtBoostRace, 0.05f,
                      "A5 选将格**单位卡·兵种行** = 17 pt（`RaceText` 17/17；标准档 18 —— **变小**）"
                    + "｜A4/A5 **方向相反** ⇒ 不可能靠「整体放大/缩小」同时满足");

            // ---- A6 / A7：同一扇窗的另两格是标准档（⛔ 别一起套 Booster）----
            // 用「原版宿主节点名」建卡：`IsBoosterCardView` 若被扩成按名字认档，这两条就红。
            var gmd = new GameObject("Generic Multi Card Display");
            gmd.transform.SetParent(root.transform, false);
            var previewHost = new GameObject("CardUI Reference");
            previewHost.transform.SetParent(gmd.transform, false);
            var pv = CardView.Create(previewHost.transform, UnitData(), "CardUI");
            Check(pv != null && pv.Use == CardUse.Standard,
                  "A6 ★ 包预览宿主 `Generic Multi Card Display/…/CardUI Reference` 下的卡 = `CardUse.Standard`"
                + "（原版那棵树 卡名 19/19 · 效果 23.55/24 · 兵种 18/18 · 阵营 15/15）"
                + "｜🧨 把 `IsBoosterCardView` 扩成按 `CardUI Reference` 名字认档 ⇒ 本行红");
            if (pv != null) Object.DestroyImmediate(pv.gameObject);
            Object.DestroyImmediate(gmd);

            var wlHost = new GameObject("CardUI-Warlord");
            wlHost.transform.SetParent(root.transform, false);
            var wv = CardView.Create(wlHost.transform, UnitData(), "CardUI");
            Check(wv != null && wv.Use == CardUse.Standard,
                  "A7 ★ 右栏 `CardUI-Warlord` 下的卡 = `CardUse.Standard`（原版同标准档：19/19 · 23.55/24 · 18/18 · 15/15）"
                + "｜🧨 同上 ⇒ 本行红（`B106_W9` 的 §二·3 第 2 点专门警告过这一条）");
            if (wv != null) Object.DestroyImmediate(wv.gameObject);
            Object.DestroyImmediate(wlHost);
        }
        catch (System.Exception e)
        {
            Check(false, "A 段抛异常（后面的条目没跑完）：" + e.Message);
        }
        finally
        {
            if (std != null) { try { Object.DestroyImmediate(std.gameObject); } catch { } }
            if (root != null) { try { Object.DestroyImmediate(root); } catch { } }
            if (win != null) { try { Object.DestroyImmediate(win.gameObject); } catch { } }
            try { DraftModeWindow.ClearRunForTest(); } catch { }
            try { WindowsManager.ClearReuseCacheForTest(); } catch { }
            try { DraftPacks.InvalidateCache(); } catch { }
            try { DraftModeWindow.SetRunForTest(prevRun); } catch { }
        }
    }

    // ==========================================================================================
    //  B 段 —— 开包窗：翻牌之后那颗 collider **仍然可点**，点它 ⇒ 进卡片详情窗
    // ==========================================================================================
    static void SectionB_BoosterClick()
    {
        Debug.Log(P + "--- B 开包窗：翻牌后点卡 ⇒ 进详情（命中区必须仍开着）---");

        GameObject camGo = null, winGo = null;
        BoosterPackOpenWindow w = null;
        try
        {
            BoosterPackAssets.Reset();
            WindowSounds.ResetCounters();
            BoosterPackOpenWindow.ResetReOpenProtectionForTest();   // 静态跨窗计数（防连开抖）推回出厂值

            if (Camera.main == null)
            {
                camGo = new GameObject("Main Camera (B117WCARDTIER)");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.aspect = LayoutSpace.DesignAspect;
                LayoutSpace.Apply(cam);
            }

            // ⚠️ `GameWindow.Manager` 是 `internal set`（`Shell/GameWindow.cs:417`）⇒ 自检不能直接赋
            //    ⇒ 走**公开口** `BoosterPackOpenWindow.Create(wm)`（它自己 `Manager = mgr` + `AttachToAnchor`）
            var wm0 = WindowsManager.Get();
            if (wm0 != null)
            {
                w = BoosterPackOpenWindow.Create(wm0);
                winGo = w != null ? w.gameObject : null;
            }
            if (w == null)
            {
                winGo = new GameObject("Booster Pack Open Window (B117WCARDTIER)");
                w = winGo.AddComponent<BoosterPackOpenWindow>();
            }
            w.Show(0, 3);                                            // 第 0 页第 4 件 = 一包 Leviathan（同 `B98A5` / `B109_E2`）

            Check(w.Cards != null && w.Cards.Length == 5, "B0 **前提**：这一包 5 张卡");
            Check(w.SlotHits[0] != null && w.SlotHits[0].gameObject != null, "B0-b **前提**：第 1 格命中区 `SlotHits[0]` 建出来了");
            if (w.SlotHits[0] == null) return;

            // 跳过窗口 0.617 s 的开包 clip（`AnimationEndEvent` 才把每张 `ChangeState(2)`）—— 直接摆成 Ready
            for (int i = 0; i < 5; i++) w.CardStates[i] = (int)BoosterPackOpenWindow.CardState.Ready;

            w.OnSlotClick(0);
            Check(w.CardStates[0] == (int)BoosterPackOpenWindow.CardState.Opening
                  || w.CardStates[0] == (int)BoosterPackOpenWindow.CardState.Shaking,
                  "B1 `Ready(2)` 状态点卡 ⇒ 进入翻牌（实得 state " + w.CardStates[0] + "）"
                + "｜🧨 `OnSlotClick` 的 Ready 分支被删 ⇒ 本行红");

            w.Advance(5f);                                           // 推穿抖动 + 整段翻牌
            Check(w.CardStates[0] == (int)BoosterPackOpenWindow.CardState.Open,
                  "B2 推时间后第 1 张 = `Open(5)`（实得 state " + w.CardStates[0] + "）");

            Check(w.SlotHits[0].gameObject.activeSelf,
                  "B3 ★★ 翻完之后命中区**仍然开着**（原版 `CardInBoosterPack` 翻完给同一颗 `2DCard/UI Collider` "
                + "再加一个 `CardClicked` ⇒ 翻开的卡可以再点开详情）"
                + "｜🧨 在 `Reveal` / `QuickOpenAll` 里加回 `SlotHits[i].gameObject.SetActive(false)` ⇒ 本行红（= 本条的缺口原状）");

            // `Back(1)` 状态点卡什么都不做（原版两个 handler 各自有状态守卫）
            w.CardStates[3] = (int)BoosterPackOpenWindow.CardState.Back;
            w.OnSlotClick(3);
            Check(w.CardStates[3] == (int)BoosterPackOpenWindow.CardState.Back,
                  "B4 `Back(1)` 状态点卡**什么都不做**（状态仍 " + w.CardStates[3] + "）"
                + "｜🧨 去掉 `OnSlotClick` 的状态守卫（无脑 `Reveal`）⇒ 本行红");

            // 端到端：`Open` 状态再点 ⇒ `OpenCardDetail` 真的开出一扇 `CardDetailPopup`
            Check(wm0 != null, "B5 **前提**：`WindowsManager.Get()` 可用（拿不到就断不了「点卡开详情窗」）");
            if (wm0 != null)
            {
                try
                {
                    w.OnSlotClick(0);
                    var popup = Object.FindFirstObjectByType<CardDetailPopup>();
                    Check(popup != null,
                          "B5 ★★ `Open(5)` 状态再点卡 ⇒ 真的开出了卡片详情窗（`CardDetailPopup`，= 原版 `CardDisplayWindow.ShowCard`）"
                        + "｜🧨 删掉 `OnSlotClick` 的 Open 分支 ⇒ 本行红");
                    if (popup != null) Object.DestroyImmediate(popup.gameObject);
                }
                catch (System.Exception e)
                {
                    Check(false, "B5 抛异常（点卡开详情窗那条链）：" + e.Message);
                }
            }
        }
        catch (System.Exception e)
        {
            Check(false, "B 段抛异常（后面的条目没跑完）：" + e.Message);
        }
        finally
        {
            if (w != null && winGo != null) { try { Object.DestroyImmediate(winGo); } catch { } }
            if (camGo != null) { try { Object.DestroyImmediate(camGo); } catch { } }
        }
    }

    /// <summary>单位卡夹具：阵营 + 兵种都填满（否则阵营行 / 兵种行不建，A2–A5 等于没量）。
    /// ⚠️ `CardData.Simple` 的 `faction` 恒 null（本仓已知坑）⇒ 必须显式填。</summary>
    static CardData UnitData()
    {
        var d = CardData.Simple("B117WCARDTIER Unit", 3, 3, 4);
        d.type = "unit";
        d.faction = "Ultramarines";
        d.subtype = "Infantry";
        return d;
    }
}
