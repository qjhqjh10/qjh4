// B117_WCARD_Assert.cs — `UI-21` 卡面字号校正 · `B117-53` 场上 2D 阴影 · `B117-54` 墓地不可命中
//    执行代理 `B117-WCARD` · 2026-11-02 · 报告 → `资料/完全复刻/判据/B117-WCARD_卡面字号与场上层.md`
//
// ============================ 判据（一手） ============================
//  ① `UI-21` 字号：**渲染尺寸 = autosize 上界 `bandMax`**（`FitToBox` 的快路 `Core/CardView.cs:5949`
//     第一句就是 `t.fontSize = bandMax;`）⇒ **只改名义 em 不会动画面**。判据目标 = 原版**实拍**
//     「卡名 单字 advance / 卡宽 = 6.86%」「效果行 墨迹高 / 卡高 = 4.12%」，
//     量法与合格带见 `Editor/B117_F14_Assert.cs`（**本文件直接读它的 `NameRatioMin/Max` / `DescRatioMin/Max`**，
//     ⛔ 不在第二处再抄一份带子）。`B117_F14` 的 ×0.75 / ×0.78 方向对、幅度不对（实跑 6.13% / 4.94%）
//     ⇒ `B117_WCARD` 按「目标 ÷ 实测」再校正：卡名 ×1.1193（1.425 → **1.595**）、效果 ×0.8333（1.872 → **1.560**）。
//  ② `B117-53`：原版 inPlay 系把整棵 `2DCard` 关掉，其中 `Card Highlight And Shadow` 走
//     `Card2DController.ToggleCardShadow(false)` = `_ShadowColor.a` **0.2 s 淡到 0**
//     （`decomp_full/Card2DController__ToggleCardShadow.c`；`DAT_1834b2bb0` = `HIGHLIGHT_COLOR_CHANGE_TIME`
//     = `CardHighlight.OutlineAnimTime`）—— **不是 `SetActive(false)`**（那一层与描边同材质同节点）。
//  ③ `B117-54`：原版 `CardScript.SetColliderNotInHand(state)` 对 `inCemetery(6)` / `removedFromGame(11)`
//     调 `CardCollider2D.Toggle(false)`（`CardScript__RemoveFromGameCard.c:22-26`、
//     `CardCollider2D__SetColliderNotInHand.c:11-24`）⇒ 真 collider + 两条命中判据一起关，回切要恢复。
//     ⚠️ 接线在 `Battle/BattleDriver.cs:25623` / `:35469`（恢复口 `:35499` + `Core/CardFeel.cs:3630/3679/3682`），
//     本文件只钉 **CardView 侧的契约**（那边不在本代理白名单，改不了）。
//
// ============================ 灭自证（三条各有） ============================
//  ① 除「比值落在带里」，还断 **`title.fontSize` / `keywords.fontSize` 就是新的 `bandMax`** ——
//     只改名义 em（或把 band 改回 1.425 / 1.872）⇒ 这一条先红，比值也会跟着越带。
//  ② 断「淡出走到 0.1 s 时 0 < `_shadowMul` < 1」——改成瞬写 0 / `SetActive(false)` ⇒ 中途读数就是 0 ⇒ 红；
//     终态再断「那层节点**仍 active**」—— 改成关节点 ⇒ 红。
//  ③ 断「关掉后 `HitEnabled` / 真 `BoxCollider2D.enabled` / `Contains` / `HitByCollider` 四者同时走」——
//     只在 `Contains` 里加一句假判据（物理层没关）⇒ collider 那一半红。
//
// 用法（⚠️ 要主对话跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod CollectionScene.Run -logFile -
//   ⚠️ **本文件还没接进任何宿主**（调度台统一接线，避免多人往同一条聚合行追加）：
//   把 `B117_WCARD_Assert.Run();` 追加到 `Editor/CollectionScene.cs` 那条聚合行的**行尾 `//` 注释之前**（零行数变化）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using TMPro;
using UnityEngine;

public static class B117_WCARD_Assert
{
    const string P = "B117WCARD ";
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var langBack = Loc.Current;
        bool persistBack = Loc.PersistOverride;
        Loc.PersistOverride = true;                       // 自检不许动玩家的真设置
        try
        {
            if (!TmpFont.Available)
            {
                // ⛔ 别读成通过：字体取不到时 `title`/`keywords` 两层根本不建
                Check(false, "前提：字体资产取得到 —— 取不到的话 ① 全是空转");
            }
            else
            {
                Loc.SetLanguage(AvailableLanguages.Chinese);  // 中文档 = 字最多那一档（同 `B117_F14_Assert`）
                FontRun();
                ShadowRun();
                HitRun();
            }
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言没跑到）：" + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            Loc.SetLanguage(langBack);
            Loc.PersistOverride = persistBack;
        }
        Debug.Log(P + "合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    static RuleEngine.CardDef Find(System.Collections.Generic.List<RuleEngine.CardDef> pool, string name)
    {
        for (int i = 0; i < pool.Count; i++) if (pool[i].Name == name) return pool[i];
        return null;
    }

    // ---------------------------------------------------------------- ① 字号
    static void FontRun()
    {
        var pool = CardDatabase.Load();
        var def = Find(pool, "Uriel Ventris");       // 与 `B117_F14_Assert` 同一张夹具（判据 6.86/4.12 就是它量出来的）
        Check(def != null, "① 前提：卡池里有 `Uriel Ventris`（`B117_F14` 的夹具卡）");
        if (def == null) return;
        var cd = BattleDriver.ToCardData(def, def.Faction);
        var root = new GameObject("b117_wcard_font");
        try
        {
            var cv = CardView.Create(root.transform, cd, "b117w_font");
            Check(cv != null, "① 前提：探针卡建出来了");
            if (cv == null) return;
            var title = cv.TitleText;
            var desc = cv.KeywordsText;
            Check(title != null && !string.IsNullOrEmpty(title.text), "① 前提：卡名层建出来且非空");
            Check(desc != null && !string.IsNullOrEmpty(desc.text), "① 前提：效果文字层建出来且非空");
            if (title == null || desc == null) return;

            float nAdv, nInk, dAdv, dInk; int nN, dN;
            bool okN = CjkMetrics(cv.transform, title, out nAdv, out nInk, out nN);
            bool okD = CjkMetrics(cv.transform, desc, out dAdv, out dInk, out dN);
            Check(okN && okD, $"① 前提：两层都量得出**汉字**字形（卡名 {nN} 个 / 效果 {dN} 个；0 个 ⇒ 下面拿 NaN 假红）");
            if (!okN || !okD) return;

            float nameRatio = nAdv / CardView.Width;      // 卡单位 ÷ 卡单位
            float descRatio = dInk / CardView.Height;
            Check(nameRatio >= B117F14_Assert.NameRatioMin && nameRatio <= B117F14_Assert.NameRatioMax,
                  $"① 卡名「单字 advance / 卡宽」= **{nameRatio * 100f:F2}%** ∈"
                + $" [{B117F14_Assert.NameRatioMin * 100f:F2}%, {B117F14_Assert.NameRatioMax * 100f:F2}%]"
                + $"（实测 advance {nAdv:F4} · 卡宽 {CardView.Width:F4} · {nN} 个汉字）｜判据 = 原版实拍 6.86%"
                + "｜🧨 `NameUnitBandMax` 改回 1.425 ⇒ ≈6.13% ⇒ 红");
            Check(descRatio >= B117F14_Assert.DescRatioMin && descRatio <= B117F14_Assert.DescRatioMax,
                  $"① 效果文字「墨迹高 / 卡高」= **{descRatio * 100f:F2}%** ∈"
                + $" [{B117F14_Assert.DescRatioMin * 100f:F2}%, {B117F14_Assert.DescRatioMax * 100f:F2}%]"
                + $"（实测墨迹高 {dInk:F4} · 卡高 {CardView.Height:F4} · {dN} 个汉字）｜判据 = 原版实拍 4.12%"
                + "｜🧨 `DescUnitBandMax` 改回 1.872 ⇒ ≈4.94% ⇒ 红");

            // ---- 灭自证：渲染字号**就是**新的 band（只改名义 em ⇒ 这里纹丝不动 ⇒ 红）----
            Check(Mathf.Abs(title.fontSize - 1.595f) <= 0.006f,
                  $"① ★ 卡名渲染字号 {title.fontSize:F4} = `NameUnitBandMax` 1.595（±0.005 网格）"
                + "｜🧨 只改 `TitleEmOfCard` 而不改 band ⇒ 字号停在旧上界 1.425 ⇒ 红（这就是 F14 当初踩的坑）");
            Check(Mathf.Abs(desc.fontSize - 1.560f) <= 0.006f,
                  $"① ★ 效果文字渲染字号 {desc.fontSize:F4} = `DescUnitBandMax` 1.560"
                + "｜🧨 只改 `DescEmOfCard` 而不改 band ⇒ 字号停在旧上界 1.872 ⇒ 红");

            // ---- 三档 × 两类 = autosize 上界（数值 = 报告 §1·2 那张推算表；⛔ 别从实现读回去）----
            Band("NameUnit", 1.595f);
            Band("NameTactic", 2.015f);
            Band("NameUnitBig", 2.015f);
            Band("NameTacticBig", 2.015f);
            Band("DescUnit", 1.560f);
            Band("DescTactic", 1.235f);
            Band("DescUnitBig", 1.690f);
            Band("DescTacticBig", 1.690f);

            // ---- 开包族那一档（实例口 `NameBandMaxForTest` / `DescBandMaxForTest`）----
            var boosterRoot = new GameObject("Booster Animation Parent");   // `IsBoosterCardView` 按父链名认这一族
            try
            {
                var bv = CardView.Create(boosterRoot.transform, cd, "b117w_booster");
                Check(bv != null, "① 前提：开包族探针卡建出来了");
                if (bv != null)
                {
                    Check(Mathf.Abs(bv.NameBandMaxForTest - 1.679f) < 1e-3f,
                          $"① 开包族卡名上界 = 1.679（1.595 × 20/19；实得 {bv.NameBandMaxForTest:F4}）");
                    Check(Mathf.Abs(bv.DescBandMaxForTest - 1.235f) < 1e-3f,
                          $"① 开包族效果文字上界 = 1.235（1.560 × 19/24；实得 {bv.DescBandMaxForTest:F4}）");
                }
            }
            finally { Object.DestroyImmediate(boosterRoot); }
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void Band(string layer, float want)
    {
        float got = CardView.BandMaxForTest(layer);
        Check(Mathf.Abs(got - want) < 1e-3f,
              $"① `{layer}` autosize 上界 = {got:F4}（应 {want:F3}）｜判据 = F14 实跑反推（报告 §1·2）");
    }

    // ---------------------------------------------------------------- ② 场上 2D 阴影
    static void ShadowRun()
    {
        var pool = CardDatabase.Load();
        var def = Find(pool, "Uriel Ventris");
        Check(def != null, "② 前提：夹具卡找得到");
        if (def == null) return;
        var cd = BattleDriver.ToCardData(def, def.Faction);

        // ---- 出生就在场上：这层从头就是 0（出生入口，铁律 10 第 5 条）----
        var rootB = new GameObject("b117_wcard_board");
        try
        {
            var bv = CardView.Create(rootB.transform, cd, "b117w_board", CardFace.Board);
            Check(bv != null, "② 前提：场上出生的探针卡建出来了");
            if (bv != null)
            {
                Check(bv.CardShadowLayerBuilt, "② 前提：SDF 软光/影层建出来了（shader/贴图取得到；取不到 ⇒ 该前提已报红）");
                if (bv.CardShadowLayerBuilt)
                    Check(Mathf.Abs(bv.CardShadowMulForTest) < 1e-4f,
                          $"② 出生就在场上 ⇒ 这层从头就是 0（实得 `_shadowMul` = {bv.CardShadowMulForTest:F3}）"
                        + "｜🧨 只在 `SetFace` 里关 ⇒ 出生那一档漏掉 ⇒ 红");
            }
        }
        finally { Object.DestroyImmediate(rootB); }

        // ---- 手牌 → 场上：0.2 s 淡出（中途有值 ⇒ 不是瞬切）；回手 ⇒ 淡回来 ----
        var root = new GameObject("b117_wcard_fade");
        try
        {
            var cv = CardView.Create(root.transform, cd, "b117w_fade", CardFace.Hand);
            Check(cv != null, "② 前提：手牌档探针卡建出来了");
            if (cv == null) return;
            Check(cv.CardShadowLayerBuilt, "② 前提：SDF 层建出来了（取不到 ⇒ 下面三条全空转，必须报红）");
            if (!cv.CardShadowLayerBuilt) return;
            Check(cv.CardShadowMulForTest > 0.99f,
                  $"② 前提：手牌档阴影是满的（实得 {cv.CardShadowMulForTest:F3}）");

            cv.SetFace(CardFace.Board);
            CardTween.Advance(0.1f);                       // 批处理没有帧循环 ⇒ 手动推半步
            float mid = cv.CardShadowMulForTest;
            Check(mid > 0.05f && mid < 0.95f,
                  $"② ★ 淡出**进行中**：0.1 s 时 `_shadowMul` = {mid:F3}（应在 0.05~0.95）"
                + "｜🧨 改成瞬写 0 / `SetActive(false)` ⇒ 中途读数就是 0 ⇒ 红");

            CardTween.Advance(0.15f);
            Check(Mathf.Abs(cv.CardShadowMulForTest) < 0.01f,
                  $"② 场上终态 `_shadowMul` = {cv.CardShadowMulForTest:F4}（应 0）");
            Check(cv.CardShadowAlphaForTest < 0.01f,
                  $"② 场上终态材质 `_ShadowColor.a` = {cv.CardShadowAlphaForTest:F4}（应 0；原版 `ToggleCardShadow(false)` 写的就是它）");
            Check(cv.CardShadowLayerActiveForTest,
                  "② ★ 那一层节点**仍然 active**（原版是淡 α，不是关节点）｜🧨 改成 `SetActive(false)` ⇒ 红");

            cv.SetFace(CardFace.Full);
            CardTween.Advance(0.25f);
            Check(cv.CardShadowMulForTest > 0.99f,
                  $"② 回手 ⇒ 阴影补回满值（实得 {cv.CardShadowMulForTest:F3}）｜🧨 只关不开 ⇒ 红");
            Check(Mathf.Abs(cv.CardShadowAlphaForTest - 0.604f) < 0.02f,
                  $"② 回手终态材质 `_ShadowColor.a` = {cv.CardShadowAlphaForTest:F4}（应标准档 `ShadowAlpha` 0.604）");
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ---------------------------------------------------------------- ③ 墓地 / 移出游戏不可命中
    static void HitRun()
    {
        var pool = CardDatabase.Load();
        var def = Find(pool, "Uriel Ventris");
        Check(def != null, "③ 前提：夹具卡找得到");
        if (def == null) return;
        var cd = BattleDriver.ToCardData(def, def.Faction);
        var root = new GameObject("b117_wcard_hit");
        try
        {
            var cv = CardView.Create(root.transform, cd, "b117w_hit", CardFace.Hand);
            Check(cv != null, "③ 前提：探针卡建出来了");
            if (cv == null) return;
            cv.transform.position = new Vector3(4f, 0f, 0f);   // 挪开原点，避免和别的探针卡叠在一起
            Physics2D.SyncTransforms();
            Vector3 w = cv.transform.position;                 // 卡心（各层都在卡根的局部原点附近）

            Check(cv.HitColliderEnabledForTest, "③ 前提：真 `BoxCollider2D` 建出来且开着");
            Check(cv.HitEnabled && cv.Contains(w), "③ 前提：常态下卡心吃命中（`HitEnabled` + `Contains` 都为真）");
            if (!cv.HitColliderEnabledForTest) return;

            cv.SetHitEnabled(false);                           // = 原版 `CardCollider2D.Toggle(false)`
            Physics2D.SyncTransforms();
            Check(!cv.HitEnabled, "③ `SetHitEnabled(false)` ⇒ `HitEnabled` = 假");
            Check(!cv.HitColliderEnabledForTest,
                  "③ ★ 真 `BoxCollider2D` 也关了（原版 `Toggle` 写的就是它）｜🧨 只在 `Contains` 里加一句假判据 ⇒ 这一条红");
            Check(!cv.Contains(w), "③ 关掉后 `Contains` = 假（原版 `inCemetery(6)` / `removedFromGame(11)` 档）");
            Check(!cv.HitByCollider(w), "③ 关掉后 `HitByCollider` = 假（**物理层**也查不到）");
            Check(!cv.ShowInBigColliderEnabled, "③ 第二面（uGUI `UI Collider`）同时关（`CardCollider2D__Toggle.c:6-9`）");

            cv.SetHitEnabled(true);
            Physics2D.SyncTransforms();
            Check(cv.HitEnabled && cv.HitColliderEnabledForTest && cv.Contains(w),
                  "③ ★ 回切 ⇒ 恢复（撤销溶卡 / 淡入到点那两个恢复口要的是这个）｜🧨 只关不开 ⇒ 红");
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>量一层 TMP 里**汉字字形**的两个渲染量（换算到**卡单位**，标架 = 卡根）。
    /// ⚠️ 与 `Editor/B117_F14_Assert.cs` 的 `CjkMetrics` **同一条量法**（那边是判据本体、本文件不改它）：
    /// 单字前进宽取**相邻两字 `xAdvance` 之差**（`xAdvance` 是累积笔位，直接当字宽会随下标线性放大），
    /// 墨迹高取四角并集的 y 跨度；两者都取**中位数**避开行末那个被再次修正的字。</summary>
    static bool CjkMetrics(Transform card, TextMeshPro t, out float avgAdvance, out float medianInkH, out int nCjk)
    {
        avgAdvance = 0f; medianInkH = 0f; nCjk = 0;
        if (card == null || t == null) return false;
        t.ForceMeshUpdate();                       // 批处理没有帧循环
        var ti = t.textInfo;
        if (ti == null || ti.characterInfo == null) return false;
        int n = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
        Vector3 o0 = card.InverseTransformPoint(t.transform.TransformPoint(Vector3.zero));
        Vector3 o1 = card.InverseTransformPoint(t.transform.TransformPoint(Vector3.right));
        float xUnit = Vector3.Distance(o0, o1);
        var advs = new List<float>(n);
        var hs = new List<float>(n);
        int prevHan = -1;
        for (int i = 0; i < n; i++)
        {
            var ch = ti.characterInfo[i];
            if (!IsHan(ch.character)) continue;     // 只量汉字：图标 / 标点 / 拉丁的墨高与前进宽不是同一把尺
            Vector3 bl = ch.vertex_BL.position, tl = ch.vertex_TL.position;
            Vector3 tr = ch.vertex_TR.position, br = ch.vertex_BR.position;
            if (bl.x == 0f && bl.y == 0f && tl.x == 0f && tl.y == 0f
                && tr.x == 0f && tr.y == 0f && br.x == 0f && br.y == 0f) continue;   // 不可见字形
            if (prevHan >= 0 && i == prevHan + 1)
            {
                float d = (ch.xAdvance - ti.characterInfo[prevHan].xAdvance) * xUnit;
                if (d > 1e-5f) advs.Add(d);
            }
            prevHan = i;
            Vector3 c0 = card.InverseTransformPoint(t.transform.TransformPoint(bl));
            Vector3 c1 = card.InverseTransformPoint(t.transform.TransformPoint(tl));
            Vector3 c2 = card.InverseTransformPoint(t.transform.TransformPoint(tr));
            Vector3 c3 = card.InverseTransformPoint(t.transform.TransformPoint(br));
            float hi = Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y));
            float lo = Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y));
            if (hi - lo > 1e-5f) hs.Add(hi - lo);
            nCjk++;
        }
        if (nCjk == 0 || advs.Count == 0 || hs.Count == 0) return false;
        advs.Sort();
        avgAdvance = advs[advs.Count / 2];
        hs.Sort();
        medianInkH = hs[hs.Count / 2];
        return avgAdvance > 1e-5f && medianInkH > 1e-5f;
    }

    /// <summary>汉字（含扩展 A）—— 卡面中文档里只有它们是「一字 = 一个 em」的等宽全角字形。</summary>
    static bool IsHan(char c) { return (c >= '\u3400' && c <= '\u4DBF') || (c >= '\u4E00' && c <= '\u9FFF'); }
}
