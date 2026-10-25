// DraftAssertD3.cs — 执行代理 `E3`（2026-10-24）的断言宿主：轮抽的三条尾巴
//   **(A) 卡包预览弹窗** · **(B) 战斗结果回灌** · **(C) `Finished` 的触发者**
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不进任何 `Editor/*Scene.cs` 宿主 —— 宿主不在本批白名单）。
// 调用方式（主对话在某个宿主收尾处加两行，想并进总数就带后一行）：
//
//     DraftAssertD3.Run();
//     pass += DraftAssertD3.Pass; fail += DraftAssertD3.Fail;
//
// 或者单独跑：`-executeMethod DraftAssertD3.Run`
//
// ==================================================================
//  🔴 期望值从哪来（⛔ 不许拿我们自己的常量证明我们自己的实现）
// ------------------------------------------------------------------
//  · **(A) 版面**：`bundle_menus_assets_all` 的 `Generic Multi Card Display`（父链实读到 `Draft Mode Menu Demo`）
//    根 RT `RectTransform_-5481408756880803131.json`：`anchor(0,.5)-(1,.5) · apos(0,−20.475) · size(0,818.04)`
//    ⇒ 设计帧 1920×1080 下 y[151.455, 969.495]；`CardUI Reference` 布局框 2.54×3.37 × `m_LocalScale 223.1`
//    ⇒ 视觉 **567.60×751.67**；`Content` 的 `HorizontalLayoutGroup.m_Spacing = 500` · `m_ChildAlignment = 4`
//    · `apos.x = −8` ⇒ 三个卡心 **449.46 / 952.00 / 1454.54**（间距 502.54 < 卡宽 567.60 ⇒ **相邻重叠 65.06**）。
//    这几条**不是**从 `DraftModeWindow` 的常量抄来的期望值：它们是**prefab 字段**算出来的，
//    🧨 谁把摆位改成「三张平分屏宽」（= 392.4 / 960 / 1527.6）这几条立刻红。
//  · **(A) 行为**：`DraftModeSelectPacksStateDemo__PackPreview.c`（`GetCardLibrary` → `Initialize(cards, **null**)`
//    → `WindowsManager.OpenWindow`）+ `UIMultiCardDisplay__Initialize.c` 末句
//    `headerText.SetActive(!IsNullOrEmpty(headerTitle))` ⇒ **标题为空 ⇒ 关**。
//    `BackgroundCloseButton`（`MonoBehaviour_-1185320953084347707.json`）的 `window = {PathID:0}`
//    + `onClick` 空 ⇒ **点背景不关窗**（所以那颗不是 `ShadeHit`）。
//  · **(B) 计数**：`DraftEventBase.OnMatchFinished` → `PlayerDataManager.RecordEventMatchFinished`
//    →（服务端）`numberOfMatchesWon/Lost`；`DraftRunDisplay__Initialize.c` 第一句
//    `victoryCounterText.text = save+0x48.ToString()` ⇒ **Victories 数字 = 胜场**。
//    平局是**第三格**（`numberOfMatchesDraw +0x50`）⇒ 两边都不算。
//  · **(C) 状态机**：原版 `DraftEventBase.UpdateState`（泛型实例 **VA `0x181814090`**，逐句反汇编）有三支：
//    `lost >= maxTries` / `joinState == Finished(2)` / `won >= matches.Length` ⇒ `rewardsCollected ? 22 : 20`；
//    否则 `selectedPacks.Count < draftRounds ? 10 : 15`；`selectedWarlord == null` 时
//    `(joinState==Joined && payState==notPaid) ? 0 : 5`。
//    🔴 **本仓现行口径（2026-10-24 用户拍板「轮抽不做奖励」+ 主对话定的本地收尾值）**：
//    **打满 `DraftModeWindow.MatchesToFinish`（4）场 · 败满 `MaxTries`（3）场 · `joinState == Finished`
//     ⇒ 一律落 `Finished(20)`**，**没有领奖步、没有 `Collected(22)`**；否则
//    `selectedPacks.Count < draftRounds ? 10 : 15`。`matches.Length` 那条**随「不做奖励」一并消失**。
//    ⇒ 🔴 **「4 轮打完」落 `OngoingRun(15)`，⛔ 不是 `Finished(20)`**（第 ⑤ 组那条专门钉它）。
//  · **`maxTries = 3`**：`StagedEventData.maxTries(+0x90)` 在服务端；本地唯一的旁证 =
//    prefab 的 `Losses container` **恒 3 格**（S3 §2.7「失败 3 场结束」）+ `SetupStageMarks` 把它当 `max`。
//    🔴 **3 是我们挑的**，本条断言钉的是「= `DraftModeWindow.MaxTries` 且 = 败场格数」，不是「原版就是 3」。
// ==================================================================
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class DraftAssertD3
{
    const string P = "[D3] ";
    const int TestSeed = 20261024;

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        DraftModeWindow win = null;

        // 现场保存（本类会动三个静态通道 + 包池缓存，跑完必须原样还回去）
        var prevMode = BattleDriver.PendingPlayMode;
        var prevPre = PrebuiltDecks.PendingSource;
        var prevRun = DraftModeWindow.RunForTest;

        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "前提：拿得到 `WindowsManager`")) return;

            win = DraftModeWindow.Create(wm, TestSeed);
            if (!Ok(win != null, "前提：`DraftModeWindow.Create(wm, seed)` 建出窗来")) return;

            WarlordPageGroup(win);
            PacksPanelGroup(win);
            PreviewGroup(win);
            BattleResultGroup(win);
            FinishedGroup(win);
            RunSaveGroup();
            RewardPanelNotCalledGroup();
            ResumeGroup(wm);
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
            Debug.Log(P + $"=== D3 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ==================================================================
    //  断言小工具（与 `DraftWindowAssertD2` / `DraftAssertD1` 同形）
    // ==================================================================
    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}"); return false;
    }

    static bool Eq<T>(T got, T want, string msg)
    {
        return Ok(EqualityComparer<T>.Default.Equals(got, want), msg + $"（实得 {got} / 期望 {want}）");
    }

    static bool Near(float got, float want, float tol, string msg)
    {
        return Ok(Mathf.Abs(got - want) <= tol, msg + $"（实得 {got:F2} / 期望 {want:F2} ± {tol:F2}）");
    }

    // ==================================================================
    //  🆕 V（2026-10-25）：行模板那几条要用的三个小工具
    // ==================================================================

    /// <summary>原版 `CardUIDisplayColorsSO` 的六档色 —— **自检侧独立抄的一份**（按 `rarity` 0..5）。
    /// 判据 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/CardUIDisplayColorsSO.json`
    /// 的 `colors[]` 逐条现读。🔴 **故意不复用 `Core/RarityColors`** —— 拿被测实现证明被测实现 = 自证。</summary>
    static readonly Color[] SoRarity =
    {
        new Color(0.50943398f, 0.50943398f, 0.50943398f, 1f),           // 0 None
        new Color(0.15841937f, 0.28123885f, 0.37735850f, 1f),           // 1 Common
        new Color(0.18866073f, 0.34905660f, 0.07409222f, 1f),           // 2 Rare
        new Color(0.41509432f, 0.17817728f, 0.39588487f, 1f),           // 3 Epic
        new Color(0.79245281f, 0.43817982f, 0f,           1f),           // 4 Legendary
        new Color(0.62264150f, 0.07262561f, 0.05580276f, 0.63921571f),  // 5 Special
    };

    /// <summary>卡面 `rarity` 串 → 原版 `CardRarity` 档号（`CardRarity.cs`：None0/Common1/Rare2/Epic3/Legendary4/Special5）。</summary>
    static int SoRarityTier(string rarity)
    {
        switch ((rarity ?? "").ToLowerInvariant())
        {
            case "common": return 1;
            case "rare": return 2;
            case "epic": return 3;
            case "legendary": return 4;
            case "special": return 5;
            default: return 0;
        }
    }

    static bool ColorNear(Color a, Color b, float tol)
    {
        return Mathf.Abs(a.r - b.r) <= tol && Mathf.Abs(a.g - b.g) <= tol
            && Mathf.Abs(a.b - b.b) <= tol && Mathf.Abs(a.a - b.a) <= tol;
    }

    /// <summary>子树里第一颗 quad（九宫格根自己有 9 块、根上通常没有 quad）。</summary>
    static ImageQuad FirstQuadIn(Transform root)
    {
        if (root == null) return null;
        var qs = root.GetComponentsInChildren<ImageQuad>(true);
        return qs.Length > 0 ? qs[0] : null;
    }

    /// <summary>一棵**九宫格根**自己那几块的并集（设计帧 px）—— 只收**名字 = `<根名>_ij` 的直接子件**。
    /// <para>🔴 **2026-10-25 实测踩过（第一次跑 ⑤c 的 12 条红里 9 条是这一条引起的）**：原版
    /// `Background` 底下**还挂着** `Rarity Gradient` / `Background Border` / `Cost Image` / `banned Icon` /
    /// `Text fill`（父链就是这样，我们的树也照它建）⇒ 用 `GetComponentsInChildren` 全收会把
    /// 「探出行外 3px 的费用圈」与「隐藏的 banned 圈」并进来 —— 实测量到
    /// **左沿 1530 / 上沿 311.39 / 高 48…54.11**（= 那两颗探出格子的边），而真值 18.48 / 0 / 45。
    /// 只收 `<根名>_ij` 才是「这一颗九宫格自己的 9 块」。</para>
    /// <para>⚠️ 九宫格那 9 块的命名见 `ImageQuad.CreateNineSlice`：`$"{name}_{i}{j}"`（`Battle/ImageQuad.cs:631`）。</para></summary>
    static bool OwnQuadsPx(Transform root, out PxRect u)
    {
        u = default(PxRect);
        if (root == null) return false;
        string prefix = root.name + "_";
        bool any = false;
        float x1 = 0f, y1 = 0f, x2 = 0f, y2 = 0f;
        for (int i = 0; i < root.childCount; i++)
        {
            var ch = root.GetChild(i);
            if (ch == null || !ch.name.StartsWith(prefix, System.StringComparison.Ordinal)) continue;
            var q = ch.GetComponent<ImageQuad>();
            if (q == null) continue;
            var r = MenuDraw.QuadRectPx(q);
            if (!any) { x1 = r.x1; y1 = r.y1; x2 = r.x2; y2 = r.y2; any = true; }
            else
            {
                x1 = Mathf.Min(x1, r.x1); y1 = Mathf.Min(y1, r.y1);
                x2 = Mathf.Max(x2, r.x2); y2 = Mathf.Max(y2, r.y2);
            }
        }
        if (any) u = new PxRect(x1, y1, x2, y2);
        return any;
    }

    /// <summary>期望矩形**先与滚动视口求交** —— 原版 `RectMask2D` 也会把探出视口的那一段裁掉
    /// （本批实测：第 0 行费用圈的 −3 上探被视口顶 270 裁成 270；我们的 `ViewportClip` 同一条口径）。
    /// 完全在视口内的件 ⇒ 逐位不变。</summary>
    static PxRect ClipView(PxRect r)
    {
        var v = DraftModeWindow.DeckScrollRectForTest;
        float x1 = Mathf.Max(r.x1, v.x1), y1 = Mathf.Max(r.y1, v.y1);
        float x2 = Mathf.Min(r.x2, v.x2), y2 = Mathf.Min(r.y2, v.y2);
        return new PxRect(x1, y1, Mathf.Max(x1, x2), Mathf.Max(y1, y2));
    }

    /// <summary>一颗 `Label` 的**墨迹**左右沿（设计帧 px）。
    /// 依据：`Label.AlignLeftOn/AlignRightOn` 的写入口径是 `node.localPosition.x = 目标 − 父x + WorldW/2`
    /// ⇒ **节点中心 = 墨迹中心**、`WorldW` = 墨迹宽（`Battle/Label.cs` 的 `PlaceLeftNow` / `WorldW`）。</summary>
    static void InkSpanPx(Label lb, out float left, out float right)
    {
        left = right = 0f;
        if (lb == null) return;
        var p = lb.transform.position;
        float cx = LayoutSpace.ToDesignPixel(p).x;
        float half = LayoutSpace.ToDesignPixel(p + new Vector3(lb.WorldW * 0.5f, 0f, 0f)).x - cx;
        left = cx - half; right = cx + half;
    }

    static string GoToPacks(DraftModeWindow win)
    {
        if (win.State != DraftModeState.SelectPack) win.ClickFree();
        if (win.State != DraftModeState.SelectWarlord)
        { Ok(false, "前提：`Free` 之后进得了选将页（当前 " + win.State + "）"); return null; }
        if (win.Warlords == null || win.Warlords.Count == 0)
        { Ok(false, "前提：有可选战将"); return null; }
        string id = win.Warlords[0].Id;
        if (!Ok(win.ChooseWarlord(id), "前提：选得中第一张战将 `" + id + "`")) return null;
        if (!Ok(win.ContinueAfterWarlord(), "前提：`Continue` ⇒ 选包页")) return null;
        return id;
    }

    static DraftPacks.PackDto FakePack(string id, int army, string[] cardIds)
    {
        return new DraftPacks.PackDto
        {
            key = id, packId = id, packName = "D3 fake " + id, army = army,
            armyName = ArmyIcons.Faction(army), tier = 0, tierName = "Common", enabled = true,
            cardIds = cardIds, srcFile = "D3_assert", duplicate = false,
        };
    }

    /// <summary>造两个「卡完全不同」的合成包（预览要断「换了包，画出来的卡也跟着换」）。</summary>
    static List<DraftPacks.PackDto> TwoFakePacks(int army, out string[] idsA, out string[] idsB)
    {
        idsA = null; idsB = null;
        var pool = CardDatabase.Load();
        if (pool == null) return null;
        var all = new List<string>();
        for (int i = 0; i < pool.Count && all.Count < 6; i++)
            if (pool[i] != null && !string.IsNullOrEmpty(pool[i].Id) && pool[i].Type == "unit") all.Add(pool[i].Id);
        if (all.Count < 6) return null;
        idsA = new[] { all[0], all[1], all[2] };
        idsB = new[] { all[3], all[4], all[5] };
        return new List<DraftPacks.PackDto> { FakePack("D3_PACK_A", army, idsA), FakePack("D3_PACK_B", army, idsB) };
    }

    // ==================================================================
    //  (A) 卡包预览弹窗
    // ==================================================================
    static void PreviewGroup(DraftModeWindow win)
    {
        Debug.Log(P + "--- (A) 卡包预览弹窗（原版 `Generic Multi Card Display`）---");

        // ---- ① 节点在、默认关（原版 GO `m_IsActive = false`；`UIMultiCardDisplay.Awake` 也关它）----
        win.BeginRun(TestSeed);
        var prev = win.PackPreviewNode;
        Ok(prev != null, "① 预览层节点建出来了（原版 prefab 名 `Generic Multi Card Display`）");
        if (prev != null)
        {
            Ok(!prev.gameObject.activeSelf,
               "① 预览层**默认关着**（原版 `Generic Multi Card Display.json` 的 `m_IsActive = false`）"
             + "🧨 怎么改会红：`Build()` 里忘了 `SetActive(false)`（那样一开窗就糊一层黑 + 三张卡）");
            Ok(prev.Find("Menu Dark Background") != null && prev.Find("Viewport/Content") != null
               && prev.Find("Close button") != null && prev.Find("Header Text") != null,
               "① 四件都在：`Menu Dark Background` / `Viewport/Content` / `Close button` / `Header Text`（名字逐字照 prefab）");
            var backTx = prev.Find("Close button/Text");
            Ok(backTx != null, "① `Close button/Text` 在（原版那颗 'Back' 字）");
            Ok(!win.PackPreviewHeaderVisible,
               "① `Header Text` **不显示**（`PackPreview` 给 `Initialize(cards, null)` ⇒ "
             + "`Initialize` 末句 `SetActive(!IsNullOrEmpty(headerTitle))` = 关）"
             + "🧨 怎么改会红：把 pack 名当标题传进去（原版这一刻**没有**标题）");
        }
        Ok(!win.PackPreviewVisible, "① `PackPreviewVisible` = false");

        // ---- ② 合成两个卡完全不同的包（真包池随机、可能两个包 3 张一样 ⇒ 断不出「跟着换」）----
        // ⚠️ 顺序要紧：**先选定战将**才知道阵营（`RollRound` 按 `packArmy` 筛），包池换完再 `Continue`。
        win.BeginRun(TestSeed);
        win.ClickFree();
        if (!Ok(win.State == DraftModeState.SelectWarlord, "② 前提：`Free` ⇒ 选将页")) { RestoreRealPool(); return; }
        if (!Ok(win.Warlords != null && win.Warlords.Count > 0, "② 前提：有可选战将")) { RestoreRealPool(); return; }
        string wl = win.Warlords[0].Id;
        if (!Ok(win.ChooseWarlord(wl), "② 前提：选得中第一张战将 `" + wl + "`")) { RestoreRealPool(); return; }
        int army = win.WarlordArmyId();
        string[] idsA, idsB;
        var fake = TwoFakePacks(army, out idsA, out idsB);
        if (!Ok(fake != null, "② 前提：卡池里能凑出 6 张 unit 卡（造两个合成包）")) { RestoreRealPool(); return; }
        DraftPacks.SetPacksForTest(fake);
        if (!Ok(win.ContinueAfterWarlord(), "② `Continue` ⇒ 选包页（此刻池里只有 2 个合成包）")) { RestoreRealPool(); return; }
        Eq(win.OfferedPacks.Count, 2, "② 合成池只有 2 个包 ⇒ 这一轮就 2 个（=`RollRound` 如实少给）");

        // ---- ③ 打开预览 ----
        string keyA = win.OfferedPacks[0].key;
        Ok(win.ShowPackPreview(keyA), "③ `ShowPackPreview(\"" + keyA + "\")` 成功");
        Ok(win.PackPreviewVisible, "③ 预览层亮了");
        Eq(win.PackPreviewCardCount, 3, "③ 画了 **3 张**卡（原版 882/882 包恒 3 张 ⇒ `GetCardLibrary` 给 3 张）"
          + "🧨 怎么改会红：只画 1 张 / 把包名当成一张卡");
        for (int i = 0; i < 3; i++)
            Eq(win.PackPreviewCardIdAt(i), idsA[i], "③ 第 " + (i + 1) + " 张 = 包 A 的 `cardIds[" + i + "]`（顺序一致）");

        // ---- ④ 版面（期望值 = prefab 字段算出来的，⛔ 不是抄本文件的常量）----
        PxRect r0, r1, r2;
        // ⚠️ 三条分开调：`out` 参数在 `&&` 短路里可能没被赋值（CS0165）
        bool have0 = win.PackPreviewCardRectAt(0, out r0);
        bool have1 = win.PackPreviewCardRectAt(1, out r1);
        bool have2 = win.PackPreviewCardRectAt(2, out r2);
        bool have = have0 && have1 && have2;
        Ok(have, "④ 三张卡的矩形都记下来了");
        if (have)
        {
            Near(r0.CX, 449.46f, 0.02f, "④ 第 1 张卡心 x = **449.46**（`Content` apos −8 ⇒ 中心 952；"
                 + "总宽 3×2.54+2×500 = 1007.62 ⇒ 左沿 448.19 ⇒ 卡心 449.46）"
                 + "🧨 怎么改会红：三张平分屏宽（那会是 392.4 / 960 / 1527.6）");
            Near(r1.CX, 952.00f, 0.02f, "④ 第 2 张卡心 x = **952.00**（= 960 + apos −8）");
            Near(r2.CX, 1454.54f, 0.02f, "④ 第 3 张卡心 x = **1454.54**");
            Near(r1.CX - r0.CX, 502.54f, 0.02f, "④ 卡心间距 = **502.54**（`CardUI Reference` 布局框 2.54 + HLG 间距 500）");
            Near(r0.W, 567.60f, 0.02f, "④ 卡视觉宽 = **567.60**（2.54 × `m_LocalScale 223.1`）");
            Near(r0.H, 751.67f, 0.02f, "④ 卡视觉高 = **751.67**（3.37 × 223.1）");
            Ok(r1.CX - r0.CX < r0.W,
               "④ 相邻卡**重叠**（间距 502.54 < 卡宽 567.60，重叠 65.06）—— 这是 prefab 那张真实的版面"
             + "🧨 怎么改会红：改成「首尾不重叠、居中铺开」（那要么缩卡、要么把间距写大）");
            Near(r0.CY, 560.48f, 0.02f, "④ 卡心 y = **560.48**（预览根 y[151.46,969.50] 的竖直中心）");
        }

        // ---- ⑤ 换一个包 ⇒ 画出来的卡跟着换（断「不是写死/不是缓存」）----
        string keyB = win.OfferedPacks[1].key;
        Ok(win.ShowPackPreview(keyB), "⑤ 换预览第二个包（`" + keyB + "`）");
        for (int i = 0; i < 3; i++)
            Eq(win.PackPreviewCardIdAt(i), idsB[i], "⑤ 第 " + (i + 1) + " 张换成了包 B 的卡（⛔ 不是上一包的残留）");
        Eq(win.PackPreviewCardCount, 3, "⑤ 还是 3 张（清旧 + 重建，⛔ 不叠成 6 张）");

        // ---- ⑥ 隐藏时**不吃点击**（活性的那一关；判据 = `PointerLayer.HitQuad` 的 `isActiveAndEnabled`）----
        var prevNow = win.PackPreviewNode;
        var backNode = prevNow != null ? prevNow.Find("Close button") : null;
        var backBtn = backNode != null ? backNode.GetComponentInChildren<WindowButton>() : null;
        Ok(backBtn != null, "⑥ `Close button` 上真的有 `WindowButton`（`MenuDraw.Hit` 建的）");
        if (backBtn != null)
        {
            Ok(PointerLayer.HitQuadForTest(backBtn) != null,
               "⑥ 预览**开着**时那颗钮可命中（`HitQuadForTest` 非空）");
            win.ClosePackPreview();
            Ok(!win.PackPreviewVisible, "⑥ `ClosePackPreview()` ⇒ 关上了");
            Ok(PointerLayer.HitQuadForTest(backBtn) == null,
               "⑥ 预览**关着**时那颗钮**不可命中**（`PointerLayer.HitQuad` 的 `isActiveAndEnabled` 那一关）"
             + "🧨 怎么改会红：把预览层改成「只把 quad 的 alpha 置 0 / 移出屏外」而节点仍 active"
             + " —— 那样关着的预览会**偷吃**轮抽窗的点击，而画面上什么都看不到");
        }
        else win.ClosePackPreview();
        Eq(win.PackPreviewCardCount, 0, "⑥ 关掉时把卡也清了（⛔ 不留在树里）");

        // ---- ⑦ 不在这一轮的包里 ⇒ 拒绝并出声（⛔ 不抛异常、不静默开一个空预览）----
        Ok(!win.ShowPackPreview("D3_NOT_OFFERED"), "⑦ `ShowPackPreview(不在本轮包里的 key)` ⇒ **false**");
        Ok(!win.PackPreviewVisible, "⑦ 而且**没有**开预览（空预览 = 玩家看到一片黑，那是骗人）");

        // ---- ⑧ 换页自动收（原版那是另一扇窗，`ChangeTabCO` 不会带着它走）----
        Ok(win.ShowPackPreview(win.OfferedPacks[0].key), "⑧ 再开一次预览");
        Ok(win.ESCPressed() && !win.PackPreviewVisible && win.gameObject.activeSelf,
           "⑧ ESC ⇒ **只关预览**、轮抽窗还在（原版预览是另一扇窗、`closeOnESC = 1`）"
         + "🧨 怎么改会红：不在 `ESCPressed()` 里先吃掉这一层（那 ESC 会连轮抽窗一起关掉）");
        Ok(win.ShowPackPreview(win.OfferedPacks[0].key), "⑧ 再开一次预览（给下一条用）");
        win.GoToState(DraftModeState.OngoingRun);
        Ok(!win.PackPreviewVisible, "⑧ 换页 ⇒ 预览自动收（🧨 怎么改会红：`GoToState` 忘了那句 ClosePackPreview）");

        Ok(RestoreRealPool(), "⑧ 收尾：真包池回来了（count = " + DraftPacks.Count + "）");
        Ok(!win.MissingArt.Contains("40k_UI_bt_back"),
           "⑧ `Close button` 的底图 `40k_UI_bt_back` **没有**进 `MissingArt`（本机有这张图）");
    }

    /// <summary>与 `DraftWindowAssertD2.RestoreRealPool` 同源（两条候选路径）。</summary>
    static bool RestoreRealPool()
    {
        DraftPacks.InvalidateCache();
        if (DraftPacks.Available && DraftPacks.Count > 100) return true;
        var cands = new[]
        {
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../数据/游戏数据/draft_packs.json")),
            Path.Combine(Application.dataPath, "CardPresentation/Resources/draft_packs.json"),
        };
        for (int i = 0; i < cands.Length; i++)
        {
            if (!File.Exists(cands[i])) continue;
            try { DraftPacks.SetPacksForTest(DraftPacks.Parse(File.ReadAllText(cands[i]))); }
            catch (Exception e) { Debug.LogError(P + "   兜底读 `" + cands[i] + "` 失败：" + e.Message); }
            break;
        }
        return DraftPacks.Count > 100;
    }

    // ==================================================================
    //  (B) 战斗结果回灌
    // ==================================================================
    static BattleLogData.Match LogRow(BattleLogData.Outcome r, string mode)
    {
        return new BattleLogData.Match { Result = r, Mode = mode, OwnHeroName = "D3", EnemyHeroName = "bot" };
    }

    static string DraftModeName() { return PlayModeNames.Name(GameMode.ClosedDeck); }

    static void BattleResultGroup(DraftModeWindow win)
    {
        Debug.Log(P + "--- (B) 战斗结果回灌（原版 `OnMatchFinished` → 战绩两格）---");

        // ---- ① 纯函数：只认 `ClosedDeck`、新→旧、平局单独一档 ----
        string d = DraftModeName();
        Eq(d, "ClosedDeck", "① 轮抽的模式串 = `ClosedDeck`（`PlayModeNames.Name(GameMode.ClosedDeck)`）");
        var log = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Victory, d),               // 最新：轮抽·胜
            LogRow(BattleLogData.Outcome.Victory, "Classic"),       // 别的模式 ⇒ 必须被跳过
            LogRow(BattleLogData.Outcome.Defeat, d),                // 轮抽·负
            LogRow(BattleLogData.Outcome.Draw, d),                  // 轮抽·平 ⇒ null
        };
        var outs = DraftModeWindow.DraftOutcomes(log);
        Eq(outs.Count, 3, "① 4 条记录里只有 **3 条**是轮抽局（`Mode != ClosedDeck` 的那条被跳过）"
          + "🧨 怎么改会红：不按模式筛（那会把别的高难度局算成轮抽战绩）");
        if (outs.Count == 3)
        {
            Eq(outs[0], (bool?)true, "① 第 1 条 = 胜（**新→旧**：记录表 `Insert(0, …)`）");
            Eq(outs[1], (bool?)false, "① 第 2 条 = 负");
            Eq(outs[2], (bool?)null, "① 第 3 条 = 平 ⇒ `null`（原版平局是**第三格** `numberOfMatchesDraw +0x50`）");
        }
        var rs = new DraftRunSave { matchesWon = 0, matchesLost = 0 };
        int applied = DraftModeWindow.ApplyOutcomes(rs, outs);
        Eq(applied, 2, "① `ApplyOutcomes` 只动了 2 场（平局不算）");
        Eq(rs.matchesWon, 1, "① 胜场 +1");
        Eq(rs.matchesLost, 1, "① 败场 +1（🧨 怎么改会红：把平局也算成负）");

        // ---- ② 先把 4 轮抽完（= 本批 (C) 的核心判据：组卡完成 ⇒ `OngoingRun(15)`，⛔ 不是 `Finished(20)`）----
        win.BeginRun(TestSeed);
        if (GoToPacks(win) == null) return;
        int guard = 0;
        while (win.State == DraftModeState.SelectPack && guard++ < 16)
        {
            if (win.OfferedPacks.Count == 0) { Ok(false, "② 第 " + (win.CurrentDraftRound + 1) + " 轮没包"); return; }
            win.PickPack(win.OfferedPacks[0].key);
        }
        Eq(win.CurrentDraftRound, DraftPacks.RoundsPerDraft, "② 选满 `RoundsPerDraft` 包");
        Eq(win.State, DraftModeState.OngoingRun,
           "② **4 轮（组卡）打完 ⇒ `OngoingRun(15)`，⛔ 不是 `Finished(20)`**"
         + "（原版 `UpdateState`：`selectedPacks.Count < draftRounds ? SelectPack(10) : OngoingRun(15)`）"
         + "🧨 怎么改会红：把「组卡完成」直接当收尾（那样玩家一次都没打就进收尾页）");
        Ok(win.State != DraftModeState.Finished, "② 反证：此刻**不在** Finished 页");

        // ---- ③ 喂 2 场胜 ⇒ 战绩 / 屏幕 / 格数三处一起动 ----
        // 把游标钉到 0：本组喂的是**参数版日志**（与全局 `BattleLogData` 无关）；
        // 真实的那条「基线」由 `BeginRun` / `ResumeRun` 走生产路径（见最后一组）。
        DraftModeWindow.RunForTest.consumedDraftMatches = 0;
        var wins2 = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Victory, d),
            LogRow(BattleLogData.Outcome.Victory, d),
        };
        Eq(win.Victories, 0, "③ 喂之前胜场 = 0（新一局的基线：⛔ 不把上一次的记录算进来）");
        Eq(win.IngestBattleResults(wins2), 2, "③ 吃进 2 场（`IngestBattleResults(log)` 返回吃了几个）");
        Eq(win.Victories, 2, "③ `Victories` = **2**（原版 `save.numberOfMatchesWon +0x48`）");
        Eq(win.State, DraftModeState.OngoingRun, "③ 2 胜 0 负 ⇒ 还停在进行中页（`lost < maxTries`）");
        Eq(win.VictoriesText, "2", "③ Ongoing 页 `Victories text Number` 印 **2**"
          + "（原版 `DraftRunDisplay.Initialize` 第一句 `victoryCounterText.text = save+0x48`）"
          + "🧨 怎么改会红：把它写回常量 `\"0\"`（就是本批要修的那个「胜负场恒 0」）");
        Eq(win.FilledWinMarks, 2, "③ 胜场格**亮了 2 格**（原版 `SetupStageMarks` 的 `i < current`）");
        Eq(win.FilledLossMarks, 0, "③ 败场格一格没亮");

        // ---- ④ 幂等：同一份日志再吃一次 ⇒ 不吃（游标） ----
        Eq(win.IngestBattleResults(wins2), 0, "④ 同一份日志**再吃一次** = 0 场（`consumedDraftMatches` 游标）"
          + "🧨 怎么改会红：不记游标（那每次回窗都白送一场胜）");
        Eq(win.Victories, 2, "④ 胜场仍是 2");

        // ---- ⑤ 输了不算胜；输满 `MaxTries` ⇒ 收尾（原版 `UpdateState` 的 `lost >= maxTries` 那一条）----
        var mixed = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Defeat, d),      // 新的一条
            LogRow(BattleLogData.Outcome.Victory, d),     // 旧的（已经吃过）
            LogRow(BattleLogData.Outcome.Victory, d),
        };
        Eq(win.IngestBattleResults(mixed), 1, "⑤ 只吃新的那 1 场（列表整体换了，靠条数差认新旧）");
        Eq(win.Defeats, 1, "⑤ 败场 = 1");
        Eq(win.Victories, 2, "⑤ 胜场还是 2（⛔ 没被重算）");
        Eq(win.State, DraftModeState.OngoingRun, "⑤ 1 负 ⇒ 还能打");
        Eq(win.FilledLossMarks, 1, "⑤ 败场格亮了 1 格");

        var threeLosses = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Defeat, d),
            LogRow(BattleLogData.Outcome.Defeat, d),
            LogRow(BattleLogData.Outcome.Defeat, d),
            LogRow(BattleLogData.Outcome.Defeat, d),
            LogRow(BattleLogData.Outcome.Defeat, d),      // 游标已到 3 ⇒ 只吃前 2 条，正好补到 3 负
        };
        Eq(win.IngestBattleResults(threeLosses), 2, "⑤ 只吃了 2 场（第 3 条起是「已经吃过」的旧记录）");
        Eq(win.Defeats, 3, "⑤ 输满 **3** 场（`DraftModeWindow.MaxTries`）");
        Eq(win.State, DraftModeState.Finished,
           "⑤ **输满 `maxTries` ⇒ `Finished(20)`**（原版 `UpdateState` 的 `lost >= data.maxTries ⇒ 收尾`）"
         + "🧨 怎么改会红：`UpdateState` 不读败场（那输多少都停在进行中页）");
        // 🔴 **2026-10-25（执行代理 D · 铁律 5 就地改期望）**：用户 2026-10-24 拍板「轮抽不做奖励」
        //   ⇒ `Reward Info Panel`（宝箱图 + 领奖钮）**整块不建**（`BuildOngoingPage` 里 `_rewardPanelRoot = null`）。
        //   原来的期望是「Finished ⇒ 面板显示」——那条随领奖步一起作废。
        Ok(win.RewardPanelNode == null,
           "⑤ `Finished` ⇒ **不建 `Reward Info Panel`**（本版没有奖励 / 领奖步；非空 = 有人把奖励面板接回来了）");
        Ok(win.ResetNode != null && win.ResetNode.gameObject.activeSelf,
           "⑤ `Finished` ⇒ `Reset Event Button` **仍显示**（有意偏离：原版那一刻唯一的出口是领奖钮，"
         + "我们把 Reset 留作「结束这一局 / 重开一轮」的出口）🧨 改回 `SetOn(_resetNode, !finished)` ⇒ 本条红");
        Ok(win.ToBattleNode != null && !win.ToBattleNode.gameObject.activeSelf,
           "⑤ `Finished` ⇒ `To Battle Button` **藏起来**（S3 §2.7 同一支）");
        Eq(win.FilledLossMarks, 3, "⑤ 败场格亮满 3 格");
        Ok(!string.IsNullOrEmpty(win.QuoteText), "⑤ `Quote End` 出了一句结束语（按胜场档取，见 `QuoteTermForWins`）");

        // ---- ⑥ 「**打满 `MatchesToFinish`（4）场结束**」—— 2026-10-24 主对话定的本地收尾口径 ----
        //   🔴 原版那第三条（`won >= data.matches.Length`）随「轮抽不做奖励」一并消失（`matches` 是服务端值、
        //   本地 0 命中）⇒ 现在按**场次**收尾：`matchesWon + matchesLost >= MatchesToFinish`。
        //   ⛔ 别再回到「不设胜场上限」（那是旧口径，2026-10-25 已作废）。
        win.BeginRun(TestSeed);
        if (GoToPacks(win) == null) return;
        guard = 0;
        while (win.State == DraftModeState.SelectPack && guard++ < 16)
        {
            if (win.OfferedPacks.Count == 0) { Ok(false, "⑥ 第 " + (win.CurrentDraftRound + 1) + " 轮没包"); return; }
            win.PickPack(win.OfferedPacks[0].key);
        }
        DraftModeWindow.RunForTest.consumedDraftMatches = 0;
        var threeWins = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Victory, d),
            LogRow(BattleLogData.Outcome.Victory, d),
            LogRow(BattleLogData.Outcome.Victory, d),
        };
        win.IngestBattleResults(threeWins);
        Eq(win.Victories, 3, "⑥ 吃进 3 场胜");
        Eq(win.State, DraftModeState.OngoingRun,
           "⑥ **3 胜 0 负 ⇒ 还没打满 " + DraftModeWindow.MatchesToFinish + " 场 ⇒ 仍在 `OngoingRun`**"
         + "🧨 提前收尾（比如写死「赢 3 场就结束」）⇒ 本条红");
        var fourth = new List<BattleLogData.Match>
        {
            LogRow(BattleLogData.Outcome.Victory, d),      // 新的一条（第 4 场）
            LogRow(BattleLogData.Outcome.Victory, d),
            LogRow(BattleLogData.Outcome.Victory, d),
            LogRow(BattleLogData.Outcome.Victory, d),
        };
        win.IngestBattleResults(fourth);
        Eq(win.Victories, 4, "⑥ 又吃掉 1 场 ⇒ 4 胜");
        Eq(win.State, DraftModeState.Finished,
           "⑥ **打满 `MatchesToFinish`（" + DraftModeWindow.MatchesToFinish + "）场 ⇒ `Finished(20)`**"
         + "（2026-10-24 收尾口径：打满 4 场结束 · 败满 3 场提前结束）"
         + "🧨 改回「不设场次上限」⇒ 本条红");
        Ok(win.FilledWinMarks >= DraftModeWindow.MatchesToFinish,
           "⑥ 胜场格**至少亮 " + DraftModeWindow.MatchesToFinish + " 格**（格子数 = `max(RoundsPerDraft, 胜场)`；"
         + "⛔ 不许把多出来的胜场藏起来）（实得 " + win.FilledWinMarks + "）");
    }

    // ==================================================================
    //  (C) `Finished` 的触发者
    // ==================================================================
    static void FinishedGroup(DraftModeWindow win)
    {
        Debug.Log(P + "--- (C) `Finished` 的触发者（原版 `UpdateState` / `FinishRun`）---");

        // ---- ① `FinishRun()` = 显式那一支（`..._FinishRun_g__Callback_0.c`：set_DraftState(0x14) → UpdateState）----
        win.BeginRun(TestSeed);
        if (GoToPacks(win) == null) return;
        Ok(DraftModeWindow.RunForTest != null, "① 有一局存档（`RunForTest`）");
        win.FinishRun();
        Eq(win.State, DraftModeState.Finished, "① `FinishRun()` ⇒ `Finished(20)`");
        Eq(DraftModeWindow.RunForTest != null ? DraftModeWindow.RunForTest.joinState : -1, (int)DraftJoinState.Finished,
           "① 顺手把 `joinState` 钉成 `Finished(2)`（原版 `set_DraftState` 那半边）");

        // ---- ② `rewardsCollected` **不再影响收尾页**（本版没有领奖步，2026-10-25 执行代理 D 就地改期望）----
        DraftModeWindow.RunForTest.rewardsCollected = true;
        win.UpdateState();
        Eq(win.State, DraftModeState.Finished,
           "② `rewardsCollected = true` 也**只落 `Finished(20)`** —— 本版没有 `Collected(22)` 那一跳"
         + "（用户 2026-10-24「轮抽不做奖励」）"
         + "🧨 把 `_run.rewardsCollected ? Collected : Finished` 加回来 ⇒ 本条红");
        Ok(win.gameObject.activeSelf, "② 收尾页**不关窗**（`Collected` 那一支已不存在）");

        // ---- ③ `CollectReward()` 现在只是「结束这一局并清档」的兼容口（⛔ 不发奖励）----
        win.BeginRun(TestSeed);
        // ★ 它的合法前提仍是「此刻已经在 Finished」——走生产入口 `FinishRun()` 把这一局造到那里
        //   （⛔ 别直写 `joinState = 2`，那会绕过被测的那一跳）。
        win.FinishRun();              // ⇒ joinState = Finished(2) ⇒ Finished(20)
        win.CollectReward();
        Ok(DraftModeWindow.RunForTest == null,
           "③ `CollectReward()` ⇒ **清掉这一局存档**（= 原版 `ResetCollectedDraft` 那一半）"
         + "🧨 怎么改会红：留着那一局（下次开窗会接着一局已结束的收尾页，玩家再也开不了新局）");
        Ok(!win.gameObject.activeSelf, "③ 顺带关窗（本版没有领奖钮；它是旧调用点/旧自检的兼容口）");

        // ---- ④ `UpdateState` 的两条「没战将」分支 = 原版那两行 ----
        win.BeginRun(TestSeed);
        DraftModeWindow.RunForTest.joinState = (int)DraftJoinState.Joined;
        DraftModeWindow.RunForTest.payState = (int)DraftPayState.NotPaid;
        win.UpdateState();
        Eq(win.State, DraftModeState.Intro,
           "④ `joinState == Joined && payState == notPaid(0)` ⇒ `Intro(0)`"
         + "（原版 `UpdateState` ②：`cmove` 到 0；`GetTabFromState(0)` 落 Pay 页）");
        // ★ 这条**必须在此刻**查：下一句把 `payState` 改成 free 再 `UpdateState()` 就会推到 `SelectWarlord`，
        //   而 `GoToState` 每次都 `SetOn(_pagePay, page == _pagePay)` ⇒ 那时 Pay 页已经被关掉，再查恒 False。
        //   （2026-10-24 第七十三会话 E12 修：原断言写在 `SelectWarlord` 之后 = 查错时机 ⇒ 假红。）
        Ok(win.PageNode(DraftModeState.Intro) != null
           && win.PageNode(DraftModeState.Intro) == win.PageNode(DraftModeState.PayScreen)
           && win.PageNode(DraftModeState.PayScreen).gameObject.activeSelf,
           "④ `Intro` 与 `PayScreen` **同页**（S3 §〇·3），且此刻**显示着**");
        DraftModeWindow.RunForTest.payState = (int)DraftPayState.Free;
        win.UpdateState();
        Eq(win.State, DraftModeState.SelectWarlord,
           "④ `payState == free(1)` 且还没选战将 ⇒ `SelectWarlord(5)`");

        // ---- ⑤ 选将之后按「已选包数」分流（原版 ③ 那一行）----
        if (GoToPacks(win) == null) return;
        Eq(win.State, DraftModeState.SelectPack, "⑤ 选了战将、0 包 ⇒ `SelectPack(10)`");
        win.PickPack(win.OfferedPacks[0].key);
        Eq(win.State, DraftModeState.SelectPack, "⑤ 1 包（< `draftRounds`）⇒ 还是 `SelectPack(10)`");
        Eq(win.StageCounterText, "2/" + DraftPacks.RoundsPerDraft, "⑤ `Stage Counter` 跟着到 2/4");
    }

    // ==================================================================
    //  (B-续) 跨窗续打：关窗（= 进对局）之后回壳再开，战绩与进度还在
    // ==================================================================
    // ==================================================================
    //  (D) 一局存档落盘（🆕 2026-10-25 执行代理 D）
    // ==================================================================
    //  判据 / 口径：`资料/任务线/界面线/任务总表.md` §一 轮抽「⑤ 一局存档落盘（原版在服务端，我们现只在进程内）」。
    //  原版这一局活在服务端 `DraftEventSaveGame` 里 ⇒ 重启也在；我们此前只有静态 `_run`（退出即丢）。
    //  ⚠️ 本组用 `OverrideDir` 指向临时目录 ⇒ **绝不碰玩家真档**（`DraftModeWindow.StoreOn` 的第 ① 支）。
    static void RunSaveGroup()
    {
        Debug.Log(P + "--- (D) 一局存档落盘（`persistentDataPath/WarpforgeDraft/run.json`）---");
        var prevDir = DraftModeWindow.OverrideDir;
        var tmp = Path.Combine(Path.GetTempPath(), "wf_draft_store_" + Guid.NewGuid().ToString("N"));
        try
        {
            DraftModeWindow.OverrideDir = tmp;
            DraftModeWindow.DeleteRunForTest();
            Ok(!File.Exists(DraftModeWindow.StorePath), "① 前提：临时目录里一开始**没有**存档");

            var run = new DraftRunSave
            {
                joinState = (int)DraftJoinState.Joined,
                payState = (int)DraftPayState.Free,
                seed = 424242,
                warlordId = "TESTWL",
                reRolls = 2,
                matchesWon = 1,
                matchesLost = 1,
                pickedKeys = new[] { "k1", "k2" },
                selectedCards = new[] { "c1", "c2", "c3" },
                consumedDraftMatches = 7,
            };
            DraftModeWindow.SetRunForTest(run);
            DraftModeWindow.SaveRunNowForTest();
            Ok(File.Exists(DraftModeWindow.StorePath),
               "② 落盘：存档文件真的写出来了（`" + DraftModeWindow.StorePath + "`）"
             + "🧨 怎么改会红：`SaveRunNow` 没接进 `SyncRun`/`BeginRun`（本组是直调口，红说明写盘本身坏了）");

            DraftModeWindow.ReloadRunForTest();
            var got = DraftModeWindow.RunForTest;
            Ok(got != null, "③ 重读：盘上那一份读回来了");
            if (got != null)
            {
                Eq(got.seed, 424242, "③ 种子");
                Eq(got.warlordId, "TESTWL", "③ 战将 id");
                Eq(got.reRolls, 2, "③ 重掷次数（原版 `currentReRolls`）");
                Eq(got.matchesWon, 1, "③ 胜场");
                Eq(got.matchesLost, 1, "③ 败场");
                Eq(got.consumedDraftMatches, 7, "③ 战绩游标（⛔ 丢了会把已经吃过的旧对局再算一遍）");
                Ok(got.pickedKeys != null && got.pickedKeys.Length == 2 && got.pickedKeys[1] == "k2",
                   "③ 已选包（**顺序**也在 —— 顺序就是轮次）");
                Ok(got.selectedCards != null && got.selectedCards.Length == 3, "③ 已拿到的卡");
            }

            // ④ 清档 ⇒ 写盘 = 删文件（放弃 / 收尾之后不该再留一局）
            DraftModeWindow.SetRunForTest(null);
            DraftModeWindow.SaveRunNowForTest();
            Ok(!File.Exists(DraftModeWindow.StorePath),
               "④ `_run == null` ⇒ 写盘 = **删档**（否则下次开窗会接着一局已结束的收尾页）");
        }
        finally
        {
            DraftModeWindow.DeleteRunForTest();          // 清静态 + 删文件
            DraftModeWindow.OverrideDir = prevDir;       // ⛔ 必须还原（后面的组 + 真 Play 都靠它）
            try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch { }
        }
    }

    // ==================================================================
    //  (E) `BuildRewardPanel` 当前**不被调用**（🆕 2026-10-25 执行代理 D）
    // ==================================================================
    //  用户 2026-10-24 拍板「轮抽不做奖励」⇒ 那一块**整块不建**（`BuildOngoingPage` 里显式
    //  `_rewardPanelRoot = null`）。方法本体**保留**（将来恢复奖励的唯一入口，doc 已写明）。
    //  ⇒ 必须有一条断言证明它**现在没人调**：否则将来有人「顺手接回去」时没有任何东西会红
    //     （= 领奖步静默复活，而这正是这一批要拆掉的东西）。
    static void RewardPanelNotCalledGroup()
    {
        Debug.Log(P + "--- (E) `BuildRewardPanel` 当前不被调用（奖励面板整块不建）---");
        const string rel = "CardPresentation/Shell/DraftModeWindow.cs";
        var path = Path.Combine(Application.dataPath, rel);
        if (!File.Exists(path))
        {
            Ok(false, "⑧ 找不到源码 `" + path + "` ⇒ 「`BuildRewardPanel` 当前不被调用」**没有验到**"
                    + "（⛔ 这不是「验过没问题」）");
            return;
        }

        var lines = File.ReadAllLines(path);
        int defs = 0, calls = 0, nullAssign = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string s = lines[i].Trim();
            if (s.StartsWith("//") || s.StartsWith("*") || s.StartsWith("/*")) continue;
            if (s.Contains("Transform BuildRewardPanel(Transform page)")) { defs++; continue; }
            if (s.Contains("BuildRewardPanel(")) calls++;
            if (s.Replace(" ", "") == "_rewardPanelRoot=null;") nullAssign++;
        }

        Ok(defs == 1, "⑧ `BuildRewardPanel` **只有一处定义**（现读 " + defs + " 处）");
        Ok(calls == 0,
           "⑧ `BuildRewardPanel(` **没有任何非注释调用点**（现读 " + calls + " 处）"
         + "｜🧨 把 `_rewardPanelRoot = BuildRewardPanel(page);` 接回来 ⇒ 本条红（领奖步会静默复活）");
        Ok(nullAssign >= 1,
           "⑧ 源码里**显式** `_rewardPanelRoot = null`（`BuildOngoingPage` 那处 = 「奖励面板整块不建」的落点，"
         + "`Build()` 重建时也清一次；现读 " + nullAssign + " 处）");
    }

    static void ResumeGroup(WindowsManager wm)
    {
        Debug.Log(P + "--- (B-续) 关窗再开 ⇒ 接着上一局（原版进度在服务端）---");

        // 前一组的窗可能已经被关掉了，而且它带着别的状态 ⇒ 干脆起一扇**新的**（钉种子 = 新局）
        var old = DraftModeWindow.LastOpened;
        if (old != null) { try { UnityEngine.Object.DestroyImmediate(old.gameObject); } catch { } }
        var win = DraftModeWindow.Create(wm, TestSeed);
        if (win == null) { Ok(false, "前提：建得出第一扇窗"); return; }
        win.BeginRun(TestSeed);
        if (GoToPacks(win) == null) return;
        win.PickPack(win.OfferedPacks[0].key);
        // ⚠️ 这里**直接写存档那一格**，不往全局 `BattleLogData` 里塞假记录（自检不动玩家的真表）。
        //    「吃日志 ⇒ 计数」那条链已由上一组 ②/③ 用**参数版**逐条验过（同一个 `ApplyOutcomes`）。
        DraftModeWindow.RunForTest.matchesWon = 1;
        int winsBefore = win.Victories, packsBefore = win.CurrentDraftRound;
        string wlBefore = win.WarlordId;
        Eq(winsBefore, 1, "① 记下现场：1 胜 / " + packsBefore + " 包");

        // 「进对局」= 窗被销毁（切场景）。这里显式销毁它，再用**生产那条路**（`Create(wm)` 不带种子）重建。
        UnityEngine.Object.DestroyImmediate(win.gameObject);
        var win2 = DraftModeWindow.Create(wm);            // seed = 0 ⇒ 走 `Open()` 的 resume 分支
        Ok(win2 != null, "② 重开一扇窗（`Create(wm)` ⇒ `Open()` 走 `ResumeRun`）");
        if (win2 == null) return;
        Eq(win2.WarlordId, wlBefore, "② 战将还在");
        Eq(win2.CurrentDraftRound, packsBefore, "② 已选包数还在（⛔ 没被 `BeginRun` 重置）"
          + "🧨 怎么改会红：`Open()` 无条件 `BeginRun`（那就是本批要修的「关一次窗进度全丢」）");
        Eq(win2.Victories, winsBefore, "② 胜场还在");
        Ok(win2.State == DraftModeState.SelectPack || win2.State == DraftModeState.OngoingRun,
           "② 落在「接着打」的那一页（实得 " + win2.State + "）");
        UnityEngine.Object.DestroyImmediate(win2.gameObject);

        // ---- ③ 「还没报名」那一档：原版 `Open` 落页看的是**存着的** `DraftState`（`GetStartingTab`），
        //      不是 `UpdateState` 现算 ⇒ `joinState = NotJoined` 必须回 **Pay 页**（不是选将页）----
        var win3 = DraftModeWindow.Create(wm, TestSeed);          // 钉种子 ⇒ 新局（NotJoined）
        Ok(win3 != null, "③ 前提：建得出第三扇窗");
        if (win3 == null) return;
        win3.BeginRun(TestSeed);
        UnityEngine.Object.DestroyImmediate(win3.gameObject);     // 「关窗」
        var win4 = DraftModeWindow.Create(wm);                    // 生产路 ⇒ `ResumeRun`
        Ok(win4 != null, "③ 再开一扇（`ResumeRun`）");
        if (win4 == null) return;
        Eq(win4.State, DraftModeState.PayScreen,
           "③ `joinState = NotJoined` 的那一局 ⇒ 回 **Pay 页**（原版 `GetStartingTab` → `GetTabFromState(DraftState)`）"
         + "🧨 怎么改会红：拿 `UpdateState` 现算落页（它会给 `SelectWarlord` —— 玩家会跳过报名页）");
        UnityEngine.Object.DestroyImmediate(win4.gameObject);
    }

    // ==================================================================
    //  ★ U1（2026-10-24）：选将页的**版面**与**真命中**（`Draft Mode Select Warlord State`）
    // ------------------------------------------------------------------
    //  🔴 期望值来源 = **prefab 现读**（⛔ 不是抄 `DraftModeWindow` 的常量）：
    //     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
    //      "Generic Select Card Container" --depth 5 --relative`
    //   · 选将格 `Generic Select Card Container`：布局 412.75×617.69 · `m_LocalScale 1.221`
    //     ⇒ 视觉 **503.97×754.20**；同层 `Spacing` 宽 80、`Content` 的 HLG `m_Spacing = 20.37`
    //     ⇒ 卡格中心**步距 = 412.75 + 80 + 2×20.37 = 533.49**
    //     （D2 §1.1 旁证②用同一算法自证：prefab 里 1 卡 + 1 `Spacing` 的 CSF 结果 = 513.12 = 412.75+20.37+80）；
    //     🔴 旧期望值 **492.75** 漏了 HLG 那两道缝（那是「4 张挤一排」的成因之一）。
    //   · **3 张居中**（`Content` 是 `HLG align = MiddleCenter(4)` + `CSF H=MinSize`）：
    //     总宽 = 3×412.75 + 2×80 + 4×20.37 = **1479.73** ⇒ 左余 (1920−1479.73)/2 = **220.135**
    //     ⇒ 首格布局中心 x = 220.135 + 206.375 = **426.51**，第 i 格 = **426.51 + 533.49 i**；
    //     格视觉框左 = 426.51 − 251.985 = **174.525**（+533.49 i），格视觉框顶 y = 554.42 − 377.10 = **177.32**
    //     （554.42 = 视口顶 101.90 + 锚点偏移 452.52，S3 §2.4）。
    //   · 卡体 `2DCard`：**视觉** 386.60×615.42 @ 格视觉框左上角 +(13.1, −19.6)
    //     （那一行的 `⇲ls=184.7` = `CardUI 151.3 × 容器 1.221`，**已经含**容器缩放
    //      ⇒ S3 §2.4 写的「386.6×615.42 **× 1.221** = 472×751.4」是**重复乘**；2026-10-24 订正）
    //     ⇒ 卡心 = (i×533.49 + **380.93**, **465.42**)。
    //   · `Select Button`：视觉 210.02×81.66 @ 格视觉框左上角 +(101.4, 585.5)→(311.4, 667.1)
    //     ⇒ 第 i 格命中区 x = i×533.49 + **275.93 … 485.93** · y = **762.82 … 844.42**。
    //   · `WarlordsPerDraft = 3`（⛔ 不是读到的字段值 —— `DraftEventData.warlordsPerDraft` 在服务端、
    //     本地 0 命中；3 是「版面证据 + 用户口述」定的，判据 → `DraftModeWindow.WarlordsPerDraft` 的 doc）。
    //     3 张总宽 1479.73 < 1920 ⇒ 原版那颗 `ScrollRect` 的 `m_Enabled = 0` 同观感 ⇒ 我们**不建滚动**。
    //  🧨 改坏法（每条都对应一次真缺陷）：
    //     · 卡高改回 751.4 / 卡按格心居中 ⇒ ② 红（真机症状：`选择` 按钮压在战将数值行上，四张卡挤成一排）；
    //     · 命中区没建出来 / 建到格心 ⇒ ③④ 红（`HitBoxForTest` 直接 false / 点是点得中但落到别处）；
    //     · 命中区队列降到 ≤ 吸收层 ⇒ ④ 红（真机 symptom = 「点在按钮上，命中的是 `AbsorbHit`」）；
    //     · 步距写回 492.75 / 不居中 ⇒ ②③ 红（那正是「一屏只放得下 4 张」的老版面）；
    //     · 张数写回 5 或 4 ⇒ ①② 红（`Warlords.Count` 与「第 4 张不该存在」两条一起红）；
    //     · `Continue` 出厂可点 ⇒ ⑤ 红（原版 `OnOpen` 末句 `interactable = false`）；
    //     · 卡体改成 `MenuDraw.Hit` 绑 `onClick`（= 2026-10-24 `W3` 那一版的错口径）⇒ ⑥ 红
    //       （原版 `2DCard/UI Collider` **没有处理器** ⇒ 点卡体什么都不做）。
    // ==================================================================
    static void WarlordPageGroup(DraftModeWindow win)
    {
        Debug.Log(P + "--- (U1) 选将页：版面 + 真命中（原版 `Draft Mode Select Warlord State`）---");

        win.BeginRun(TestSeed);
        win.ClickFree();
        if (!Ok(win.State == DraftModeState.SelectWarlord, "① 前提：`Free` ⇒ 选将页（实得 " + win.State + "）")) return;
        int n = win.Warlords != null ? win.Warlords.Count : 0;
        if (!Ok(n > 0, "① 前提：这一局有可选战将（实得 " + n + "）")) return;
        Eq(n, 3, "① 这一局给 **3 张**战将候选（`WarlordsPerDraft = 3`；用户 2026-10-24 真机点名「原版是三个」）"
          + "🧨 怎么改会红：常量写回 5 / 4（那是「一屏只看到 4 张」的老版面）");
        // 3 张总宽 1479.73 < 1920 ⇒ 一屏放得下、原版那颗 `ScrollRect` 的 `m_Enabled = 0` 同观感
        // ⇒ 我们**不建滚动区**（滚动区只在「整行超一屏」时才建）。
        Ok(!win.WarlordScrollingForTest,
           "① 3 张时**不建横向滚动**（原版 `Generic Card Selector Menu` 的 `ScrollRect.m_Enabled = 0`，拖不动）"
         + "🧨 怎么改会红：无条件 `new MenuScroll(...)`（那会让滚轮在这一页吃掉一次事件，而原版拖不动）");
        // 3 格、第 4 格**不存在**（⛔ 不是「建了但藏在屏外」）
        var wlContent = win.transform.Find("Draft Mode Select Warlord State/Generic Card Selector Menu/Viewport/Content");
        Ok(wlContent != null, "① 选将页的 `Content` 节点找得到（`…/Generic Card Selector Menu/Viewport/Content`）");
        Eq(wlContent != null ? wlContent.childCount : -1, 3,
           "① `Content` 下**正好 3 格**（第 4 张**不该存在**——⛔ 不是「建了但被视口裁掉」）"
         + "🧨 怎么改会红：`WarlordsPerDraft` 写回 4/5");
        Ok(win.WarlordCardViewForTest(3) == null && win.WarlordSelectButtonForTest(3) == null,
           "① 第 4 格的卡视图 / `Select Button` 都取不到（`null` = 越界或没建）");
        int vis = n;         // 3 张全在一屏内（不再有「屏外那一格」）

        // ---- ② 卡体（`2DCard`）的**渲染**矩形：从 `CardView` 自己量（`BaseScale × Width/Height × 108` + 世界位置）----
        for (int i = 0; i < vis; i++)
        {
            PxRect r;
            if (!Ok(win.WarlordCardRectRenderedForTest(i, out r), "② 第 " + (i + 1) + " 格的卡量得到（`CardView` 在）")) continue;
            Near(r.H, 615.42f, 0.6f, "② 第 " + (i + 1) + " 格 **卡体视觉高 = 615.42**（原版 `2DCard`；"
                                   + "🧨 751.4 = 把容器那 1.221 乘了两次）");
            Near(r.W, 386.60f, 0.6f, "② 第 " + (i + 1) + " 格 **卡体视觉宽 = 386.60**（原版 `2DCard`）");
            Near(r.CX, i * 533.49f + 380.93f, 0.6f, "② 第 " + (i + 1) + " 格卡心 x = 格视觉框左 + 206.4"
                                                  + "（原版卡在格子里**偏左**：卡心 = 布局框中心 − 45.59）"
                                                  + "；步距 **533.49** = 412.75 + 80 + 2×20.37（旧值 492.75 漏了 HLG 两道缝）");
            Near(r.CY, 465.42f, 0.6f, "② 第 " + (i + 1) + " 格卡心 y = **465.42**（原版 `2DCard` 顶部 157.72 + 615.42/2；"
                                    + "🧨 旧写法按格心居中 = 554.42 ⇒ 卡比原版低 89.0px）");
        }
        // ---- ②′ 整行**居中**（`Content` HLG `align = MiddleCenter`）：总宽 1479.73 ⇒ 左余 220.135 ----
        {
            PxRect r0, r1, r2;
            bool h0 = win.WarlordCardRectRenderedForTest(0, out r0);
            bool h1 = win.WarlordCardRectRenderedForTest(1, out r1);
            bool h2 = win.WarlordCardRectRenderedForTest(2, out r2);
            if (Ok(h0 && h1 && h2, "②′ 三格的卡都量得到"))
            {
                Near(r1.CX, 914.42f, 0.6f, "②′ 中间那张卡心 x = **914.42**（= 槽心 960 − 卡在槽里偏左的 45.59）"
                                         + "🧨 怎么改会红：左对齐起排（那会是 694.28，整行左移 220.1）");
                Near(r2.CX - r0.CX, 1066.98f, 0.6f, "②′ 首尾卡心相距 = 2×533.49 = **1066.98**"
                                                  + "🧨 怎么改会红：步距写回 492.75（那会是 985.50）");
                Near((r0.CX + r2.CX) * 0.5f, 914.42f, 0.6f, "②′ 首尾对称（整行居中）");
            }
        }

        // ---- ③ `Select Button` 的**命中区**（`PointerLayer.HitBoxForTest` = 生产那一份 `HitBoxPx`）----
        for (int i = 0; i < vis; i++)
        {
            var b = win.WarlordSelectButtonForTest(i);
            Vector2 c = Vector2.zero, h = Vector2.zero; ImageQuad q = null;
            if (!Ok(b != null && PointerLayer.HitBoxForTest(b, out c, out h, out q),
                    "③ 第 " + (i + 1) + " 格 `Select Button` 的命中区**量得到**"
                  + "（`null` = 命中区没建出来 / 没有 `ImageQuad` —— 那正是「真鼠标点不动」，`WindowButton.Click()` 直调看不出来）"))
                continue;
            Near(c.x - h.x, i * 533.49f + 275.93f, 0.6f, "③ 第 " + (i + 1) + " 格命中区左 = 格视觉框左 + 101.4");
            Near(c.x + h.x, i * 533.49f + 485.93f, 0.6f, "③ 第 " + (i + 1) + " 格命中区右 = 格视觉框左 + 311.4");
            Near(c.y - h.y, 762.82f, 0.6f, "③ 第 " + (i + 1) + " 格命中区顶 = 格视觉框顶 + 585.5");
            Near(c.y + h.y, 844.42f, 0.6f, "③ 第 " + (i + 1) + " 格命中区底 = 格视觉框顶 + 667.1（= 原版那颗 `Select Button` 的框）");
        }

        // ---- ④ 真命中：按 `PointerLayer` 那条链（同一个 `AllButtons` + `HitBoxForTest` + **渲染队列高的赢**）
        //      算「点这一点，吃到的是谁」，⛔ 不是直调 `Click()` ----
        int absorbQ = QuadQueue(win.transform.Find("General Red Background/AbsorbHit"));
        Ok(absorbQ != int.MinValue, "④ 前提：窗内吸收层 `AbsorbHit` 在、量得到队列（实得 " + absorbQ + "）");
        for (int i = 0; i < vis; i++)
        {
            float px = i * 533.49f + 380.93f;          // 按钮中心 x（275.93…485.93 的中点）
            float py = (762.82f + 844.42f) * 0.5f;     // 按钮中心 y = 803.62
            int wq; string who;
            var winner = HitTestInWindow(win.transform, px, py, out wq, out who);
            Ok(winner != null && winner == win.WarlordSelectButtonForTest(i),
               "④ 点第 " + (i + 1) + " 格按钮中心 (" + px.ToString("F1") + "," + py.ToString("F1") + ") "
             + "⇒ 吃到的是**这颗 `Select Button`**（实得 `" + who + "`；"
             + "🧨 真机症状：落到 `AbsorbHit` = 用户报的「按钮点不到」）");
            Ok(wq > absorbQ, "④ 而且它的队列 " + wq + " **严格高于**窗内吸收层 `AbsorbHit` 的 " + absorbQ
                           + "（同档时 `ImageQuad` 世界 z 恒 0 ⇒ 谁吃到退化成枚举顺序）");
        }

        // ---- ⑤ `Continue`：出厂灰（原版 `OnOpen` 末句），选中之后才可点（`SelectWarlord` 末句）----
        var cont = win.WarlordContinueButtonForTest;
        if (!Ok(cont != null, "⑤ `Continue` 的 `WindowButton` 在")) return;
        Ok(!cont.Interactable, "⑤ 还没选战将 ⇒ `Continue.interactable = false`（原版 `OnOpen`）");
        if (WindowButton.GrayShaderAvailable)
            Ok(cont.GrayedForTest, "⑤ 而且它**真的灰了**（`Everguild/UI/Greyscale`；原版 `DoStateTransition(Disabled)` 那一支）");

        // 真点一下：走**上面那套命中表**选出来的那颗（= 生产会派发的那一颗），再验三处可见反馈
        int dq; string dwho;
        var clickable = HitTestInWindow(win.transform, 380.93f, (762.82f + 844.42f) * 0.5f, out dq, out dwho);
        if (Ok(clickable != null, "⑤ 前提：第 1 格按钮中心算得出命中件（实得 `" + dwho + "`）"))
            clickable.Click();
        string first = win.Warlords[0].Id;
        Eq(win.WarlordId, first, "⑤ 点一下 ⇒ **真的选中**第 1 位战将 `" + first + "`");
        Ok(cont.Interactable, "⑤ 选中之后 `Continue` 可点（原版 `SelectWarlord` 末句）");
        var cv0 = win.WarlordCardViewForTest(0);
        Ok(cv0 != null && cv0.State == CardHighlightState.Selected,
           "⑤ 选中那张卡**高亮成 `Selected`**（原版 `ToggleSelected` → `Card2DController.ToggleHighlight(true)`）");
        var sb0 = win.WarlordSelectButtonForTest(0);
        Ok(sb0 != null && !sb0.Interactable,
           "⑤ 而且**那一格按钮自己灰掉**（原版 `ToggleSelected` 的 `selectButton.interactable = !selected`）");
        var cv1 = win.WarlordCardViewForTest(1 < n ? 1 : 0);
        Ok(cv1 == null || cv1.State != CardHighlightState.Selected || n < 2,
           "⑤ 没选的那几张**不高亮**（⛔ 别把整排都点亮）");

        // ---- ⑥ 卡体那一下 = 原版 `2DCard/UI Collider`（`UIGenericEventCatcher + NonDrawingGraphic`，无处理器）
        //      ⇒ **吞掉、什么都不做**；⛔ 不是以前那样**穿透到窗内吸收层 `AbsorbHit`**
        //      （真机 click_log 2026-10-24 #37/#39 的观感就是「点了卡，结果命中的是窗户的吸收层」）----
        {
            float px = 380.93f, py = 300f;      // 第 1 格卡体（卡体 y 157.72…773.14，取上半部分，避开 `Select Button`）
            int cq; string cwho;
            var cb = HitTestInWindow(win.transform, px, py, out cq, out cwho);
            Ok(cb != null && cb.name == "CardBodyHit" && MenuDraw.WasAbsorb(cb.transform),
               "⑥ 点卡体 ⇒ 吃到的是**这张卡自己的吸收区**（原版 `2DCard/UI Collider`；实得 `" + cwho + "`）");
            // 档梯子：压暗 4130 < 窗内吸收 4168 < **卡体吸收 4169** < 内容钮 4170
            //   —— 卡体必须**严格高于**窗内吸收层（否则那一下会落进窗子的关闭链）。
            Ok(cq > absorbQ, "⑥ 卡体吸收区的队列 " + cq + " **严格高于**窗内吸收层 `AbsorbHit` 的 " + absorbQ
                           + "（同档时谁吃到退化成枚举顺序 —— 那正是真机 F1 的成因）");
            string before = win.WarlordId;
            if (cb != null) cb.Click();
            Eq(win.WarlordId, before, "⑥ 而且它**什么都不做**（原版那张 `UI Collider` 上没有处理器：不选、不换页、不关窗）");
            var cbWb = cb != null ? cb.GetComponent<WindowButton>() : null;
            Ok(cbWb == null || cbWb.onClick == null,
               "⑥ 而且它**没有 `onClick`**（🧨 改成 `MenuDraw.Hit` + `ChooseWarlord` ⇒ 这条与上一条一起红；"
             + "原版 `2DCard/UI Collider` 是 `UIGenericEventCatcher + NonDrawingGraphic`，无处理器）");
        }

        // 收尾：把这一组留下的选中态清掉（后面的组各自 `BeginRun`，这里只是不让「本组最后的状态」飘到别处）
        win.BeginRun(TestSeed);
    }

    // ==================================================================
    //  ★ U2（2026-10-24 第七十四会话 · `W7`）：选包页 `Reroll` 行 · 右栏 Deck Info 内容块 · Card List 头部
    // ------------------------------------------------------------------
    //  判据：
    //   · 几何 = `menu_dump bundle_menus_assets_all "Draft Mode Select Packs State" --depth 8 --relative`
    //            + `… "Draft Mode Deck Info Panel" --depth 12 --relative`（平移量 = `DeckPanelR` 左上角，
    //            与 `DeckToggleR[0]` 对齐验证过）。
    //   · `Reroll` 价格 = `d:/2/tools/decomp_full/Everguild.LiveOps.DraftEventData__GetReRollCosts.c`：
    //            `FirstOrDefault(当前次数)` → 找不到 `LastOrDefault` → **表空 ⇒ 0**；**没有次数上限**。
    //            `cost == 0` ⇒ `PriceDisplayButton__Setup.c` 走 `PriceDisplay.SetupFree` ⇒ 画「Free」。
    //   · 费用分布 = `DeckEnergyCostDrawer__Initialize.c`（`cost < 行数−1` 落该行、否则全进最后一行；
    //            归一化 = 张数 / 10 · 现读 `dump.cs:105906` `NUMBER_OF_CARDS_FOR_NORMALIZATION = 10`）。
    //   · Card List 计数 = `DrafModeDeckInfoCardListPanel__Initialize.c`：`deck.CardCount + "/" + deckSize`。
    //   · 🆕 ④′ **两件卡背的 z 旋转** = prefab `m_LocalRotation` 实读（`Cardback` **+1.60°** ·
    //            子件 `Cardback Front` **−3.43°**，两件 `m_Pivot` 都 (0.5,0.5)）⇒ 我们走
    //            `MenuDraw.Rect(…, rotZDeg:)` 把角度写在**Transform 的 `localRotation`** 上（⛔ 不是转顶点）；
    //            `Cardback Front` 在 prefab 里树深 **4**（stretch 锚点）⇒ 两级叠加 = 世界 **−1.83°**。
    //   · 🆕 ④″ **回归条**：`MenuDraw.Rect` 是全工程共用件（2026-10-24 `W18` 现数 = **451 个真调用点**；
    //            `grep "MenuDraw.Rect("` 得 **468** 行，其中 **17** 行整行是注释）⇒
    //            必须钉住「默认 `rotZDeg = 0f` 时**一处都不转**」（抽 3 个 `Rect` 建的件）。
    //   🧨 改坏法：把 `Reroll` 删掉 / 价格编一个数 / 费用分布写成 8 行或按别的归一 / 头部不建 /
    //            卡背不传 `rotZDeg`（或把角度写反、或只转顶点）/ **把 `rotZDeg` 默认值改成非 0** ⇒ 逐条红。
    // ==================================================================
    static void PacksPanelGroup(DraftModeWindow win)
    {
        Debug.Log(P + "--- (U2) 选包页 `Reroll` / 右栏 Deck Info / Card List 头部 ---");

        win.BeginRun(TestSeed);
        win.ClickFree();
        if (!Ok(win.State == DraftModeState.SelectWarlord, "① 前提：`Free` ⇒ 选将页（实得 " + win.State + "）")) return;
        if (!Ok(win.Warlords != null && win.Warlords.Count > 0, "① 前提：有可选战将")) return;
        if (!Ok(win.ChooseWarlord(win.Warlords[0].Id), "① 前提：选得中第一张战将")) return;
        if (!Ok(win.ContinueAfterWarlord(), "① 前提：`Continue` ⇒ 选包页")) return;

        // ---- ① `Reroll` 整行（上一批是「整支不建」）----
        var reroll = win.RerollNode;
        Ok(reroll != null, "① `Reroll` 整行**建出来了**（原版 `Draft Mode Select Packs State > Reroll`）"
          + "🧨 怎么改会红：删掉这一行（那就是本批要修的那条差异）");
        Ok(reroll != null && reroll.Find("Reroll Text") != null
           && reroll.Find("Price Display Button 2 Variant/Generic UI Button") != null
           && reroll.Find("Price Display Button 2 Variant/Price Display") != null,
           "① 三个子件都在：`Reroll Text` / `Price Display Button 2 Variant/Generic UI Button` / `…/Price Display`"
         + "（节点名逐字照 prefab）");

        // ---- ② 价格：表空 ⇒ 0 = 免费（原版 `PriceDisplay.SetupFree` 画「Free」）----
        Eq(win.ReRollCost, 0, "② `reRollCosts` 表空 ⇒ 价格 = **0 = 免费**（原版 `GetReRollCosts` 表空那一支）"
          + "🧨 怎么改会红：自己编一个价格（那张表在服务端、本地 0 命中）");
        Eq(win.RerollPriceText, Loc.T("MainMenu/Free"),
           "② 价格钮上印的是「Free」（`cost == 0` ⇒ 原版 `PriceDisplay.SetupFree`）");

        // ---- ③ 点一次 ⇒ 真的重抽 + 次数 +1；点第二次**照样成功**（原版没有上限）----
        var before = new List<string>();
        for (int i = 0; i < win.OfferedPacks.Count; i++) before.Add(win.OfferedPacks[i].key);
        int r0 = win.ReRolls;
        Ok(win.ReRollPacks(), "③ `ReRollPacks()` 成功");
        Eq(win.ReRolls, r0 + 1, "③ `currentReRolls` +1（原版 `save.currentReRolls`）");
        bool changed = false;
        for (int i = 0; i < win.OfferedPacks.Count; i++)
            if (!before.Contains(win.OfferedPacks[i].key)) changed = true;
        Ok(changed, "③ 包**真的换了**（盐 = `seed + reRolls×7919`，见 `RollOffers`）"
          + "🧨 怎么改会红：重掷不换盐（那样点了等于没点）");
        Ok(win.ReRollPacks(), "③ 再点一次**仍然成功**（原版没有「次数用完」的上限）");
        Eq(win.ReRolls, r0 + 2, "③ 次数 = +2");

        // ---- ④ 右栏 Deck Info 内容块：卡背两层 + 战将卡面 + 9 行费用分布 ----
        var holder = win.DeckInfoHolder;
        Ok(holder != null, "④ 右栏 `Energy View panel` 内容块在");
        if (holder != null)
        {
            Ok(holder.Find("Cardback") != null && holder.Find("Cardback/Cardback Front") != null,
               "④ 卡背两层都在，且 `Cardback Front` 是 `Cardback` 的**子件**（原版树深 4：`Cardback` → `Cardback Front`）");
            // ---- ④′ z 旋转（原版 `m_LocalRotation` 实读：`Cardback` **+1.60°** · 子件 `Cardback Front` **−3.43°**）----
            //   判据：`python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
            //          "Draft Mode Deck Info Panel" --depth 12 --relative` 表末
            //          「↻ 2 处带 `m_LocalRotation`（绕 z ≠ 0）」那一块；两件的 `m_Pivot` 都是 **(0.5,0.5)**。
            //   ⚠️ 期望值**不来自** `DraftModeWindow.DeckBackRotZ` 那两个常量（否则就是自证）。
            //   ⚠️ 用 `Mathf.DeltaAngle` 折算：`localEulerAngles.z` 会把 −3.43 回成 **356.57**。
            //   🔴 **子件关系是判据的一部分**：`Cardback Front` 的树深是 4（锚点 stretch、`sizeDelta ≈ 0`）
            //      ⇒ 自己的 −3.43° 与父件的 +1.60° **叠加** ⇒ 画出来的世界角 = **−1.83°**。
            var cbBackT = holder.Find("Cardback");
            var cbFrontT = holder.Find("Cardback/Cardback Front");
            Near(LocalRotZ(cbBackT), 1.60f, 1e-2f,
               "④′ `Cardback` 的 `localEulerAngles.z` = **+1.60°**（原版 `m_LocalRotation` 实读）"
             + "🧨 怎么改会红：不传 `rotZDeg`（= 画成轴对齐外接框，那正是本批要修的差异）"
             + " · 只转网格顶点（那样 `localEulerAngles` 仍是 0）");
            Near(LocalRotZ(cbFrontT), -3.43f, 1e-2f,
               "④′ `Cardback Front` 的**局部** `localEulerAngles.z` = **−3.43°**（同上；`DeltaAngle` 折算过 356.57）"
             + "🧨 怎么改会红：两件的角度写反 / 都写成同一个值");
            Near(WorldRotZ(cbFrontT), -1.83f, 1e-2f,
               "④′ 它的**世界**角 = +1.60 − 3.43 = **−1.83°**（原版子件关系 ⇒ 两级角度叠加）"
             + "🧨 怎么改会红：把 `Cardback Front` 建成 `Energy View panel` 的**兄弟**（那样世界角直接是 −3.43）；"
             + "或把父级那个 +1.60° 并进子件这一份");
            // ---- ④″ 回归条：`MenuDraw.Rect` 的**默认值**（`rotZDeg = 0`）⇒ 其它件一个都没转 ----
            //   抽 3 个**确定由 `MenuDraw.Rect` 建出来**的件（⛔ 不抽 `Node` / `Text` / `Nine` 那些）：
            //   `General Red Background/Menu Dark Background`（`DraftModeWindow.cs` 的 `BuildBackdrop` ①）·
            //   `General Red Background/Menu Vignette`（同函数 ④）·
            //   `Generic Multi Card Display/Menu Dark Background`（`BuildPackPreview` 的压暗层）。
            string[] zeroRot =
            {
                "General Red Background/Menu Dark Background",
                "General Red Background/Menu Vignette",
                "Generic Multi Card Display/Menu Dark Background",
            };
            for (int i = 0; i < zeroRot.Length; i++)
            {
                var zt = win.transform.Find(zeroRot[i]);
                Ok(zt != null, "④″ 前提：`" + zeroRot[i] + "` 在（它是 `MenuDraw.Rect` 建的）");
                Near(LocalRotZ(zt), 0f, 1e-2f,
                   "④″ `" + zeroRot[i] + "` **没有旋转**（`MenuDraw.Rect` 的 `rotZDeg` 默认 `0f` ⇒ 逐位不变）"
                 + "🧨 灭自证：把 `MenuDraw.Rect` 的 `rotZDeg` 默认值改成非 0 ⇒ **这三条一起红**");
            }
            Ok(holder.Find("CardUI-Warlord") != null, "④ 战将卡面在（原版 `CardUI-Warlord`，视觉 192.48×254.89）");
            var costContent = holder.Find("Content");
            Ok(costContent != null, "④ `Deck Information cost drawer > Content` 在（原版那颗 VLG）");
            Eq(costContent != null ? costContent.childCount : -1, 9,
               "④ 费用分布 = **9 行**（`DeckCostQuantityRowDrawer` 数组长度；`dump.cs` 与 prefab 同一条）"
             + "🧨 怎么改会红：只画 8 行 / 把 9 行写死成别的数");
            if (costContent != null && costContent.childCount == 9)
            {
                var row8 = costContent.GetChild(8);
                Ok(row8 != null && row8.name == "Deck CostQuanityt Row Drawer" && row8.Find("Card Cost") != null
                   && row8.Find("Slider") != null && row8.Find("Cards in deck") != null,
                   "④ 每行三件都在：`Card Cost` / `Slider` / `Cards in deck`（节点名逐字照 prefab）");
            }
            // 此刻一张卡都还没选 ⇒ 9 行全是 0；难度：读 `Cards in deck` 的字要有 TMP
            // ⇒ 这里只断「行数 + 三件在」（数值口径由 `DeckEnergyCostDrawer__Initialize` 那一段反汇编钉住）。
            Eq(win.CardsCounterText, Loc.T("Demo/EnergyBalance/Cards") + "0/" + DraftModeWindow.DeckSizeForDraft,
               "④ 右栏 `CardCounter` = 「" + Loc.T("Demo/EnergyBalance/Cards") + "0/"
             + DraftModeWindow.DeckSizeForDraft + "」—— **`Cards:` 与数字之间没有空格**（原版 `Concat`）"
             + "🧨 怎么改会红：中间补一个空格（那是修正前那一版）");
        }

        // ---- ⑤ Card List 页头部：`Warlord Name` + `Card counter` + `Separator` ----
        win.SetDeckTab(1);
        var tabHolder = win.transform.Find("Draft Mode Deck Info Panel/Cards in deck panel");
        Ok(tabHolder != null, "⑤ `Cards in deck panel` 找得到");
        if (tabHolder != null)
        {
            Ok(tabHolder.Find("Warlord Name") != null, "⑤ 头部 `Warlord Name` 在（原版 33px · Left/Middle · 可点开卡片详情）");
            Ok(tabHolder.Find("Card counter") != null, "⑤ 头部 `Card counter` 在（原版 28px · Right/Middle）");
            Ok(tabHolder.Find("Separator") != null, "⑤ 头部之下有 `Separator`（原版 `40k_Separator Fade Sides Horizontal`）");
            Ok(tabHolder.Find("Warlord Name Hit") != null, "⑤ `Warlord Name` 带命中区（原版那颗是 `EverguildButton`）");
        }
        Eq(win.DeckListCounterText, "0/" + DraftModeWindow.DeckSizeForDraft,
           "⑤ `Card counter` 文本 = `张数/卡组张数`（原版 `deck.CardCount + \"/\" + deckSize`）"
         + "🧨 怎么改会红：prefab 那个 `30/30` 是占位默认值，直接照抄就红");

        // ---- ⑤b 🆕 D5（2026-10-25）：`Card List` 的 `Scroll View` 真挂上（原版 `ScrollRect v=1 / Elastic / 50`）----
        //   判据：`m_ScrollSensitivity=50 · m_Horizontal=0 · m_Vertical=1 · m_MovementType=1(Elastic) ·
        //   m_Elasticity=0.1 · m_Inertia=1 · m_DecelerationRate=0.135`（`MonoBehaviour_-1440327974928745787.json:22`）；
        //   Content VLG `m_Spacing=2.5`；行高 45 ⇒ 步距 47.5；视口 330.97×588.88。
        win.BeginRun(TestSeed);
        if (GoToPacks(win) == null) return;
        if (win.OfferedPacks.Count == 0) { Ok(false, "⑤b 第一轮没包"); return; }
        win.PickPack(win.OfferedPacks[0].key);       // 3 张卡入手
        win.SetDeckTab(1);
        int gotCards = win.SelectedCards.Count;
        // 🔴 V（2026-10-25 执行代理）：原版 `DeckListDrawer.stackCopies = 1`（`MB_1819977727535512261` 实读）⇒
        //   **同一种卡并成一行**（`DeckListDrawer__Initialize.c`：命中已存在那一行就 `SetCount(count+1)`）
        //   ⇒ 行数 = **去重后的卡种数**，⛔ 不是张数（同一个包里可能重复出同一张）。
        var deckIds = new List<string>();
        for (int i = 0; i < win.SelectedCards.Count; i++)
            if (!deckIds.Contains(win.SelectedCards[i])) deckIds.Add(win.SelectedCards[i]);
        Eq(win.DeckListCounterText, gotCards + "/" + DraftModeWindow.DeckSizeForDraft,
           "⑤b 有卡之后 `Card counter` 跟着变（" + gotCards + " 张）");
        var dlScroll = win.DeckListScrollForTest;
        Ok(dlScroll != null, "⑤b `Scroll View` 真的挂上了 `MenuScroll`（此前这一页**根本没有滚动**）");
        if (dlScroll != null)
        {
            Ok(dlScroll.Vertical && dlScroll.Elastic && dlScroll.Inertia,
               "⑤b 滚动参数 = 原版 `m_Vertical=1` · `m_MovementType=1`(Elastic) · `m_Inertia=1`"
             + "｜🧨 改回横向 / Clamped ⇒ 本条红");
            Near(dlScroll.Viewport.W, 330.97f, 0.05f, "⑤b 视口宽 = **330.97**（原版 `Scroll View` 的 `m_SizeDelta`）");
            Near(dlScroll.Viewport.H, 588.88f, 0.05f, "⑤b 视口高 = **588.88**");
            Near(dlScroll.NotchPx, 50f, 1e-4f, "⑤b `m_ScrollSensitivity` = **50**（原版这台 `ScrollRect` 实读）");
            var content = win.DeckListContentForTest;
            Eq(content != null ? content.childCount : -1, deckIds.Count,
               "⑤b `Content` 底下**一种卡一行**（原版 `stackCopies=1` ⇒ 去重；也不按视口高度截断 —— "
             + "截断 = 多出来的卡看不到）");
            float wantH = deckIds.Count <= 0 ? 0f
                        : deckIds.Count * DraftModeWindow.DeckRowH + (deckIds.Count - 1) * DraftModeWindow.DeckRowGap;
            Near(dlScroll.ContentX2 - dlScroll.ContentX1, wantH, 0.01f,
                 "⑤b 内容高 = **n×45 + (n−1)×2.5**（行高 45 · VLG `m_Spacing 2.5`）"
               + "🧨 把行高改回 38 / 漏掉 spacing ⇒ 本条红");
            // 滚一格 = 50 px（口径 = uGUI `ScrollRect.OnScroll`：`position += delta × m_ScrollSensitivity`）。
            // ⚠️ 12 张的内容高 567.5 < 视口 588.88 ⇒ 本来**滚不动**（ClampHi = 0），所以这里临时把内容撑高
            //    并**摘掉 OnChanged**（否则回调会把内容高写回真值、顺手把偏移夹回 0 ⇒ 量到的是 0）。
            float realContent = dlScroll.ContentX2;
            var cb = dlScroll.OnChanged;
            dlScroll.OnChanged = null;
            dlScroll.ContentX2 = dlScroll.Viewport.y1 + 1000f;
            dlScroll.Offset = 0f;
            dlScroll.Wheel(-1f);
            Near(dlScroll.Offset, 50f, 0.01f,
                 "⑤b 滚一格 = **50 px**（`Wheel(-1)` ⇒ `delta × m_ScrollSensitivity`）"
               + "🧨 传 `1f` / `120f` 当灵敏度 ⇒ 本条红");
            dlScroll.OnChanged = cb;
            dlScroll.ContentX2 = realContent;
            dlScroll.Offset = 0f;
        }

        // ---- ⑤c 🆕 V（2026-10-25 执行代理）：**行模板 `Deck Selector Card Info button`** 的子件 / 几何 / 染色 / 滚动
        //   🔴 期望值**全部来自 prefab 字段**，⛔ 不是抄 `DraftModeWindow` 的常量：
        //     `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all \
        //        --rt -6068752977877108027 --depth 8 --relative --md`
        //     （行 330.97×45；`Background` 行内 18.48..330.97 · `Rarity Gradient` 207.32..330.97 × 0..44.89 ·
        //      `Background Border` 19.59..330.97 · `Cost Image` 2.18..53.18 × −3..48 · `Text fill` 59.10..322.85 × 3.15..41.85）
        //   染色判据 = `UICardInfoItem__Initialize.c` 那两句（只染 `Rarity Gradient` + `Background Border`）；
        //   色值 = `CardUIDisplayColorsSO.json`（本文件顶部 `SoRarity` 独立抄的一份）。
        {
            var pool = CardDatabase.Load();
            float rowX1 = DraftModeWindow.DeckScrollRectForTest.x1;       // 1530
            float rowX2 = DraftModeWindow.DeckScrollRectForTest.x2;       // 1860.97
            float rowTop = DraftModeWindow.DeckScrollRectForTest.y1;      // 270
            int rows = win.DeckListRowCountForTest;
            Eq(rows, deckIds.Count, "⑤c 行数 = **去重后的卡种数**（原版 `stackCopies=1`）");
            int scan = Mathf.Min(rows, deckIds.Count);
            for (int i = 0; i < scan; i++)
            {
                string who = "⑤c 第 " + i + " 行";
                var row = win.DeckListRowForTest(i);
                if (!Ok(row != null && row.name == "Deck Selector Card Info button",
                        who + " 根节点名 = 原版 `Deck Selector Card Info button`（行高 45 · `LayoutElement.m_MinHeight`）"))
                    continue;
                float ry = rowTop + i * (DraftModeWindow.DeckRowH + DraftModeWindow.DeckRowGap);
                var def = CardDatabase.FindById(pool, deckIds[i]);
                var wantTint = SoRarity[SoRarityTier(def != null ? def.Rarity : null)];

                // ① 行底（九宫 150,0,150,0）
                PxRect rb;
                bool hasBg = OwnQuadsPx(DraftModeWindow.DeckListRowChildForTest(row, "Background"), out rb);
                Ok(hasBg, who + " 前提：`Background`（`40k_deck_cardlist_bg` 九宫格）建出来了");
                if (hasBg)
                {
                    var wantBg = ClipView(new PxRect(rowX1 + 18.48f, ry, rowX2, ry + 45f));
                    Near(rb.x1, wantBg.x1, 0.6f, who + " 行底左沿 = 行左 + **18.48**（锚 0.05584 × 行宽 330.97）"
                       + "🧨 把行底铺满整行 ⇒ 左沿变成 1530 ⇒ 红");
                    Near(rb.x2, wantBg.x2, 0.6f, who + " 行底右沿 = 行右（**1860.97**）");
                    Near(rb.y1, wantBg.y1, 0.6f, who + " 行底上沿 = 行顶");
                    Near(rb.H, wantBg.H, 0.6f, who + " 行底高 = **45**（= 行高，`LayoutElement.m_MinHeight`）");
                }

                // ② 稀有度色条（**吃稀有度色**）
                var gradT = DraftModeWindow.DeckListRowChildForTest(row, "Rarity Gradient");
                var gradQ = gradT != null ? gradT.GetComponent<ImageQuad>() : null;
                Ok(gradQ != null, who + " `Rarity Gradient` 在（原版 `Background` 的第一个子件 · Simple 拉伸）");
                if (gradQ != null)
                {
                    var gr = MenuDraw.QuadRectPx(gradQ);
                    Near(gr.x1, rowX1 + 207.32f, 0.6f, who + " 色条左沿 = 行左 + **207.32**（锚 0.60575）");
                    Near(gr.x2, rowX2, 0.6f, who + " 色条右沿 = 行右");
                    Near(gr.H, 44.89f, 0.6f, who + " 色条高 = **44.89**（锚 0→1 + sizeDelta.y −0.1035）");
                    Ok(ColorNear(gradQ.Tint, wantTint, 0.01f),
                       who + " 色条 tint = 原版 `CardUIDisplayColorsSO` 该稀有度的色（rarity="
                     + (def != null ? def.Rarity : "?") + " ⇒ 实得 " + gradQ.Tint + " / 期望 " + wantTint + "）"
                     + "🧨 拿 `CardRarityColorsSO` 那份（宝箱用的另一颗 SO）顶 / 忘了染色 ⇒ 红");
                }

                // ③ 描边（**也吃稀有度色**）
                PxRect rbd;
                bool hasBd = OwnQuadsPx(DraftModeWindow.DeckListRowChildForTest(row, "Background Border"), out rbd);
                Ok(hasBd, who + " 前提：`Background Border`（九宫 5,5,5,5）建出来了");
                if (hasBd)
                {
                    var wantBd = ClipView(new PxRect(rowX1 + 19.59f, ry, rowX2, ry + 45f));
                    Near(rbd.x1, wantBd.x1, 0.6f, who + " 描边左沿 = 行左 + **19.59**（比行底外扩 1.11）");
                    Near(rbd.x2, wantBd.x2, 0.6f, who + " 描边右沿 = 行右");
                    var bdQ = FirstQuadIn(DraftModeWindow.DeckListRowChildForTest(row, "Background Border"));
                    Ok(bdQ != null && ColorNear(bdQ.Tint, wantTint, 0.01f),
                       who + " 描边 tint 与色条**同一个**稀有度色（原版 `imagesToChangeColorByRarity` 那两颗）");
                }

                // ④ 费用圈 + `Cost`（`cost < 0` ⇒ 两件都不建）
                var costT = DraftModeWindow.DeckListRowChildForTest(row, "Cost Image");
                var costQ = costT != null ? costT.GetComponent<ImageQuad>() : null;
                int cost = def != null ? def.Cost : -1;
                if (cost < 0)
                    Ok(costT == null, who + " `cost < 0` ⇒ `Cost Image` **不建**（原版 `SetActive(cost >= 0)`）");
                else
                {
                    Ok(costQ != null, who + " `Cost Image` 在（`Card Frame Cost Icon` · 51×51）");
                    if (costQ != null)
                    {
                        var cr = MenuDraw.QuadRectPx(costQ);
                        // 费用圈**上下各探出行外 3px** ⇒ 第 0 行那 3px 会被视口顶裁掉（原版 `RectMask2D` 同）
                        var wantCost = ClipView(new PxRect(rowX1 + 2.18f, ry - 3f, rowX1 + 53.18f, ry + 48f));
                        Near(cr.x1, wantCost.x1, 0.6f, who + " 费用圈左沿 = 行左 + **2.18**（锚 0.03 · 中心 27.68）");
                        Near(cr.W, wantCost.W, 0.6f, who + " 费用圈宽 = **51**（高 = 行高 45 + sizeDelta.y 6；ARF 1:1 高控宽）");
                        Near(cr.y1, wantCost.y1, 0.6f, who + " 费用圈上沿 = 行顶 **−3**（**探出行外**，原版就这样；"
                           + "第 0 行被视口顶裁到 270 = 原版 `RectMask2D` 同一条）");
                        Near(cr.H, wantCost.H, 0.6f, who + " 费用圈高 = **51**（整块在视口内时）");
                    }
                    Eq(DraftModeWindow.DeckListRowTextForTest(row, "Cost"), cost.ToString(),
                       who + " `Cost` 文本 = 费用数字（`UICardInfoItem.SetCost` 的 `cost.ToString()`）");
                    var costLb = DraftModeWindow.DeckListRowChildForTest(row, "Cost");
                    var costLabel = costLb != null ? costLb.GetComponent<Label>() : null;
                    float cl, crr; InkSpanPx(costLabel, out cl, out crr);
                    Near((cl + crr) * 0.5f, rowX1 + 27.68f, 1.5f,
                         who + " `Cost` 的墨迹中心 x = 行左 + **27.68**（= 费用圈中心）");
                }

                // ⑤ `banned Icon`：节点在、**恒关着**（轮抽没有限时活动 ⇒ `IsCardBanned` 恒 false）
                var banned = DraftModeWindow.DeckListRowChildForTest(row, "banned Icon");
                Ok(banned != null && !banned.gameObject.activeSelf,
                   who + " `banned Icon` **节点在但关着**（原版 `SetActive(banned)`；轮抽没有 LiveOps 活动 ⇒ 恒 false）"
                 + "🧨 把它删掉 / 让它显示 ⇒ 红");

                // ⑥ 文字区：卡名（左对齐）+ 份数（右对齐，>1 张才印）
                var tf = DraftModeWindow.DeckListRowChildForTest(row, "Text fill");
                Ok(tf != null, who + " `Text fill` 在（原版那颗 `HorizontalLayoutGroup`）");
                Eq(DraftModeWindow.DeckListRowTextForTest(row, "Card Name"),
                   def != null ? CardText.Name(def) : deckIds[i],
                   who + " `Card Name` = 本地化卡名（原版 `GetLocalizedCardName`）");
                var nameT = DraftModeWindow.DeckListRowChildForTest(row, "Card Name");
                var nameLb = nameT != null ? nameT.GetComponent<Label>() : null;
                float nl, nr; InkSpanPx(nameLb, out nl, out nr);
                Near(nl, rowX1 + 59.10f, 1.5f,
                     who + " 卡名**墨迹左沿** = 行左 + **59.10**（`Text fill` 左沿；原版 `HorizontalAlignment=Left`）"
                   + "🧨 退回居中 ⇒ 墨迹左沿会跑到 ~150 ⇒ 红");
                Ok(nameLb != null && ColorNear(nameLb.color, new Color(0.934f, 0.934f, 0.934f, 1f), 0.01f),
                   who + " 卡名字色 = **(0.934,0.934,0.934)**（原版 `m_fontColor` 实读；⛔ 不是纯白）");
                int nCopies = 0;
                for (int k = 0; k < win.SelectedCards.Count; k++)
                    if (win.SelectedCards[k] == deckIds[i]) nCopies++;
                var cntT = DraftModeWindow.DeckListRowChildForTest(row, "Count");
                if (nCopies > 1)
                {
                    Eq(DraftModeWindow.DeckListRowTextForTest(row, "Count"), "x" + nCopies,
                       who + " `Count` 文本 = `x{n}`（原版 `SetCount` 的 `String.Format(\"x{0}\", n)`，小写 x 无空格）");
                    var cntLb = cntT != null ? cntT.GetComponent<Label>() : null;
                    float xl, xr; InkSpanPx(cntLb, out xl, out xr);
                    Near(xr, rowX1 + 322.85f, 1.5f,
                         who + " 份数**墨迹右沿** = 行左 + **322.85**（`Text fill` 右沿；原版 `HorizontalAlignment=Right`）");
                }
                else
                    Ok(cntT == null, who + " 只有 1 张 ⇒ `Count` **不建**（原版 `SetActive(1 < count)`）");
            }

            // ⑦ 命中区 = `Background` 的矩形（原版那颗 `EverguildButton` 的 `m_TargetGraphic`）
            var row0 = win.DeckListRowForTest(0);
            var hitT = DraftModeWindow.DeckListRowChildForTest(row0, "Deck Selector Card Info button Hit");
            // ⚠️ `MenuDraw.Hit` 的 quad 挂在命中节点**底下那颗 `Hit`** 上（`MakeHitQuad`），节点自己身上没有
            //    ⇒ 必须 `GetComponentInChildren`（第一次跑就是栽在这一句：拿 `GetComponent` 恒 null）。
            var hitQ = hitT != null ? hitT.GetComponentInChildren<ImageQuad>(true) : null;
            Ok(hitQ != null, "⑤c 行上有命中区（原版那整行是 `EverguildButton`）");
            if (hitQ != null)
            {
                var hr = MenuDraw.QuadRectPx(hitQ);
                Near(hr.x1, rowX1 + 18.48f, 0.6f, "⑤c 命中区左沿 = 行左 + **18.48**（= `Background` 左沿；"
                   + "色条/描边/费用圈/文字全 `m_RaycastTarget = 0`）");
                Near(hr.x2, rowX2, 0.6f, "⑤c 命中区右沿 = 行右");
            }

            // ⑧ 滚动之后行位置仍对（Elastic ⇒ 内容不够高也拖得动）：行整体位移 = −Offset
            //   🔴 **方向有讲究**（第一次跑失败的那条）：往上拖（Offset > 0）时第 0 行整块**移出视口顶**
            //   ⇒ 量到的是「被视口裁过的那一块」（恒 270），断言等于没断。**往下拖**（Offset < 0）让整行
            //   落在视口内，量到的才是真位置。
            var dl2 = win.DeckListScrollForTest;
            if (dl2 != null && scan > 0)
            {
                float baseY = rowTop;                       // 第 0 行的行顶
                dl2.BeginDrag(470f);
                dl2.DragTo(500f);                           // 往下拖 30px ⇒ Elastic 越界 ⇒ Offset 为负
                float off = dl2.Offset;
                Ok(off < -1f, "⑤c 前提：向下拖动产生了**负**偏移（Elastic 橡皮筋；实得 " + off.ToString("F2") + "）");
                var g1 = DraftModeWindow.DeckListRowChildForTest(win.DeckListRowForTest(0), "Rarity Gradient");
                var q1 = g1 != null ? g1.GetComponent<ImageQuad>() : null;
                Ok(q1 != null, "⑤c 滚动后第 0 行还在（重建过 ⇒ 要现取节点）");
                if (q1 != null)
                    Near(MenuDraw.QuadRectPx(q1).y1, baseY - off, 0.9f,
                         "⑤c 滚动后第 0 行的色条上沿 = 基准 **270** − Offset(" + off.ToString("F2") + ")"
                       + "🧨 滚了但没重画（`OnChanged` 没接）⇒ 位置停在 270 ⇒ 红");
                dl2.EndDrag();
                dl2.SetOffset(0f);                          // 复位（会回调重建）
                var g2 = DraftModeWindow.DeckListRowChildForTest(win.DeckListRowForTest(0), "Rarity Gradient");
                var q2 = g2 != null ? g2.GetComponent<ImageQuad>() : null;
                if (q2 != null)
                    Near(MenuDraw.QuadRectPx(q2).y1, baseY, 0.9f, "⑤c 复位后第 0 行回 **270**（偏移夹回 0 也重画）");
            }

            // ⑨ 🔴 「新加一层要配一条『该藏的时候藏住了吗』」：切到 Deck Info 页 ⇒ **整行（含底/色条/描边）都不显示**
            //   （原版三页 `SetActive` 互斥；只关文字会留下一条条空壳底板）
            win.SetDeckTab(0);
            var rowHidden = win.DeckListRowForTest(0);
            Ok(rowHidden == null || !rowHidden.gameObject.activeInHierarchy,
               "⑤c 切到 Deck Info 页 ⇒ **行整棵不显示**（`Cards in deck panel` 的 `SetActive(false)` 管住整棵子树）"
             + "🧨 把行底/色条挂到面板根上（不挂在 `Scroll View` 底下）⇒ 这一页会留下一排空壳 ⇒ 红");
            win.SetDeckTab(1);                              // 还回 Card List 页（后面的组按这一页看不看得见行）
        }

        // ---- ⑥ 三页互斥：切到 Card List ⇒ Deck Info 那三件跟着关（原版三页 SetActive 互斥）----
        Ok(win.DeckInfoHolder != null && win.DeckInfoHolder.childCount == 0,
           "⑥ 切到 Card List 页 ⇒ `Energy View panel` 的内容被收掉（原版三页 `SetActive` 互斥）"
         + "🧨 怎么改会红：把卡背/费用分布挂到面板根上（那样三页同时显示）");

        // ---- ⑦ 🆕 D6（2026-10-25）：Alliance Info 页 = 提示节点在 + **出声恰好一次** ----
        int hintBefore = DraftModeWindow.AllianceHintLogCountForTest;
        win.SetDeckTab(2);
        var tabHolder2 = win.transform.Find("Draft Mode Deck Info Panel/Cards in deck panel");
        var hint = tabHolder2 != null ? tabHolder2.Find("Alliance Hint") : null;
        Ok(hint != null && hint.gameObject.activeSelf,
           "⑦ 切到第 3 页（Alliance Info）⇒ **`Alliance Hint` 节点在且显示**（原版这一页要服务端数据，本地留空 + 一句自拟提示）");
        win.SetDeckTab(2);                            // 同一页再刷一次 ⇒ 不该再出声
        Eq(DraftModeWindow.AllianceHintLogCountForTest - hintBefore, 1,
           "⑦ 「本机没有联盟/排行榜数据」那条日志**恰好出声一次**（同页重复刷新不刷屏）"
         + "🧨 把 `_deckAllianceSaid` 那道闸删掉 ⇒ 本条红");

        win.SetDeckTab(0);
        win.BeginRun(TestSeed);        // 收尾：别把选中态/时钟带给后面的组
    }

    /// <summary>`PointerLayer` 那条命中链的**同口径**复刻（自检用）：候选 = 场上所有 `WindowButton`
    /// （生产 `CollectHits` 用的是同一个 `AllButtons`）· 命中矩形 = `HitBoxForTest`（生产 `HitBoxPx` 的
    /// **唯一实现**，⛔ 不在这里重算）· 排序 = 生产那个比较器的三条键**逐条同序**：
    /// **渲染队列↓ → `z` 小的先 → 后兄弟先**（第三条见 <see cref="AboveInHierarchy"/>）。
    /// ⚠️ 只收**本窗**（`win` 子树）里的钮，并且不过 `PointerReachable`（自检只开这一扇窗）。
    /// </summary>
    static WindowButton HitTestInWindow(Transform win, float px, float py, out int q, out string who)
    {
        WindowButton best = null; ImageQuad bestQ0 = null;
        int bestQ = int.MinValue; float bestZ = float.MaxValue;
        string bestWho = "空点（本窗没有钮盖住这一点）";
        var all = PointerLayer.AllButtonsForTest();
        for (int i = 0; i < all.Length; i++)
        {
            var b = all[i];
            if (b == null || b.transform == null || win == null || !b.transform.IsChildOf(win)) continue;
            Vector2 c, h; ImageQuad quad;
            if (!PointerLayer.HitBoxForTest(b, out c, out h, out quad)) continue;
            if (Mathf.Abs(px - c.x) > h.x || Mathf.Abs(py - c.y) > h.y) continue;
            int qq = quad != null ? quad.RenderQueue : int.MinValue;
            float z = quad != null ? quad.transform.position.z : 0f;
            bool take;
            if (best == null) take = true;
            else if (qq != bestQ) take = qq > bestQ;
            else if (z < bestZ - 1e-4f) take = true;
            else if (z > bestZ + 1e-4f) take = false;
            else take = AboveInHierarchy(quad != null ? quad.transform : null, bestQ0 != null ? bestQ0.transform : null);
            if (!take) continue;
            best = b; bestQ0 = quad; bestQ = qq; bestZ = z;
            bestWho = b.name + "（队列 " + qq + "，框 (" + (c.x - h.x).ToString("F1") + ","
                    + (c.y - h.y).ToString("F1") + ")-(" + (c.x + h.x).ToString("F1") + ","
                    + (c.y + h.y).ToString("F1") + ")）";
        }
        q = bestQ; who = bestWho; return best;
    }

    /// <summary>同队列同 `z` 时的平局判据 = **后兄弟先**。
    /// 🔴 口径**照抄** `PointerLayer.IsAboveInHierarchy`（它 `private static`、本文件转发不到）：
    /// 从根向下逐级比 `GetSiblingIndex()`，同一级下标相同 ⇒ 同一个父 ⇒ 继续往下；
    /// 一条链是另一条的前缀（祖先 vs 后代）⇒ **后代赢**（子件画在父件之后）。
    /// ⚠️ **两份同一条规则**（生产一份、本文件一份）：改生产那份时这份要跟着改 —— 见 CLAUDE.md §三。
    /// </summary>
    static bool AboveInHierarchy(Transform a, Transform b)
    {
        if (a == b || a == null) return false;
        if (b == null) return true;
        var pa = new List<Transform>(); var pb = new List<Transform>();
        for (var t = a; t != null; t = t.parent) pa.Insert(0, t);
        for (var t = b; t != null; t = t.parent) pb.Insert(0, t);
        int n = Mathf.Min(pa.Count, pb.Count);
        for (int i = 0; i < n; i++)
        {
            if (pa[i] == pb[i]) continue;
            return pa[i].GetSiblingIndex() > pb[i].GetSiblingIndex();
        }
        return pa.Count > pb.Count;      // 前缀 ⇒ 后代赢
    }

    /// <summary>一个节点子树里那颗 `ImageQuad` 的渲染队列（量不到 ⇒ `int.MinValue`）。</summary>
    static int QuadQueue(Transform t)
    {
        if (t == null) return int.MinValue;
        var q = t.GetComponentInChildren<ImageQuad>();
        return q != null ? q.RenderQueue : int.MinValue;
    }

    /// <summary>一个节点**自己**的 z 旋转（度，已用 `Mathf.DeltaAngle` 折到 ±180）。
    /// <para>为什么不用裸 `localEulerAngles.z`：Unity 把它归一到 `[0,360)` ⇒ prefab 的 **−3.43°** 读出来是
    /// **356.57**，拿 `Near(356.57, -3.43)` 比会**假红**。</para>
    /// <para>`null`（节点不在）⇒ 回 `NaN` ⇒ `Near` 的比较恒 false ⇒ **必红**（⛔ 不静默当 0 过）。</para></summary>
    static float LocalRotZ(Transform t)
    {
        if (t == null) return float.NaN;
        return Mathf.DeltaAngle(0f, t.localEulerAngles.z);
    }

    /// <summary>一个节点的**世界** z 旋转（度，已折到 ±180）。`null` ⇒ `NaN`（`Near` 必红）。
    /// <para>用来钉「父子角度叠加」那条：本壳窗根与各 Holder **都没有旋转** ⇒ 世界角 = 父链各级 local 角之和。
    /// 把相加结果写成字面量（**+1.60 − 3.43 = −1.83**）而不是拿两个常量现算 —— 后者是自证。</para></summary>
    static float WorldRotZ(Transform t)
    {
        if (t == null) return float.NaN;
        return Mathf.DeltaAngle(0f, t.eulerAngles.z);
    }
}
