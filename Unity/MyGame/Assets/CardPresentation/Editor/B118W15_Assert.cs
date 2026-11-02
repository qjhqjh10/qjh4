// B118W15_Assert.cs — 执行代理 `B118-W15` 的断言宿主 · 2026-11-02
//
// 覆盖本代理改的两件事：
//   A · `D118-51` 换牌补抽**不发** `WhenEventKind.Draw` 广播（普通抽牌**照发**）
//   B · `D118-56` `CardFace.Board` 也**建** 2D 卡面四层（初始隐藏），回手时按正常路径打开
//   C · 源码级灭自证（防止「两边一起改回去」）
//
// 怎么跑（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B118W15_Assert.Run -logFile -
//
// 判绿红：看 `B118W15 合计` 那一行；逐条按**行首** `B118W15 ✓` / `B118W15 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主要求：`Run()` **零前提** —— 不读 `BattleDriver` / 场景 / 存档；自带临时根 + 收尾 `DestroyImmediate`；
//   自己把 `CardTween.Mode` 拨成 `Manual`、`LinkEnabled` 拨成 `false`，退出原样还原（批处理没有帧循环）。
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B118W15_Assert
{
    const string Tag = "B118W15";
    static int _pass, _fail;

    /// <summary>给宿主聚合行用的读口（否则失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckNum(int got, int want, string msg)
    {
        Check(got == want, msg + "（期望 " + want + "；实得 " + got + "）");
    }

    /// <summary>批处理没有帧循环 ⇒ 补间只能手动推（`CardTween.Mode = Manual`，见 `Run`）。</summary>
    static void Step(float dt) { CardTween.Advance(dt); }

    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ==================================================================
    //  夹具（本文件自带 —— `RuleEngineTest` 的那些 helper 是 private）
    // ==================================================================
    static CardDef Unit(string name, int cost, int atk, int hp)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, null);
    }

    /// <summary>和 `RuleEngineTest.Deck` 同形：督军 + 20 张垫牌 + **把手牌放表尾**（抽牌取末尾 ⇒ 起手正好是它们）。</summary>
    static List<CardDef> Deck(params CardDef[] hand)
    {
        var d = new List<CardDef> { new CardDef("FixtureWarlord", "FixtureWarlord", "hero", "", null, "Test", 0, 2, 30, 0, null) };   // = `RuleEngineTest.Hero("FixtureWarlord", 2, 30)`
        for (int i = 0; i < 20; i++) d.Add(Unit("filler" + i, 1, 0, 1));
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static bool HandHas(BattleContext ctx, int p, string name)
    {
        foreach (var inst in ctx.Players[p].Hand)
            if (inst != null && inst.Card != null && inst.Card.Name == name) return true;
        return false;
    }

    /// <summary>`When you draw a card` 那类**手牌监听**的触发计数（同 `RuleEngineTest.HandEffectCountOn`）：
    /// **每次 `WhenEventKind.Draw` 广播都会让手牌那一份挂上 `HandEffects`** ⇒ 这是「广播发没发」的可观测代理
    /// （`ctx.Events` 那张**字符串日志**不会被 `BroadcastWhen` 写 —— 见 `RuleCore.cs` 里 `EvtKind.Draw` 那段的注释）。</summary>
    static int HandEffectCount(BattleContext ctx, int p, string name)
    {
        int best = 0;
        foreach (var inst in ctx.Players[p].Hand)
            if (inst != null && inst.Card != null && inst.Card.Name == name)
                if (inst.HandEffects.Count > best) best = inst.HandEffects.Count;
        return best;
    }

    // ==================================================================
    //  A · `D118-51`：换牌补抽**不发** `WhenEventKind.Draw`；普通抽牌**照发**
    // ==================================================================
    //  原版判据：`BattleManager._ResolveDrawCard_d__432__MoveNext.c:283-295` 把
    //    `BroadcastCardDrawn` / `AddDrawTrapAction` / `SetCardTurnDrawn` **三件包在同一个
    //    `IsDuringMulligan == 0` 闸里**；而换牌补抽走的是另一条路 `BattleManager__GetMulliganCard.c`
    //    （表头 `GetItem(0)` + `RemoveAt(0)`，**一个广播都不发**）。⇒ 换牌期补抽不得发 `Draw` 广播。
    static void A_MulliganRefillSilent()
    {
        var watcher = new CardDef("B118W15W", "B118W15W", "unit", "When you draw a card, gain +1 Attack",
                                  "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        var d0 = Deck(watcher, Unit("A", 1, 1, 1), Unit("B", 1, 1, 1));
        var d1 = Deck(Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1));
        var ctx = RuleCore.NewBattle(d0, d1, seed: 7, shuffle: false, openMulligan: true);

        Check(ctx != null && ctx.MulliganOpen, "A0 前提：`openMulligan: true` ⇒ 开局在换牌阶段");
        Check(HandHas(ctx, 0, "B118W15W"), "A0b 前提：watcher **在起手手牌里**（不在的话下面「没变」= 0==0 的假绿）");
        var ps = ctx.Players[0];
        int hand0 = ps.Hand.Count, deck0 = ps.Deck.Count;
        int n0 = HandEffectCount(ctx, 0, "B118W15W");

        int r = RuleCore.Mulligan(ctx, 0, new List<int> { 1, 2 });
        CheckNum(r, 2, "A1 换牌 2 张（前提：真的走到「摘 → 回库 → 补抽」那条路上）");
        CheckNum(ps.Hand.Count, hand0, "A2 ★ **补抽到手**：手牌张数不变（3 → 3）");
        CheckNum(ps.Deck.Count, deck0, "A2b 牌库张数也不变（弃回 2 + 补抽 2）");
        CheckNum(HandEffectCount(ctx, 0, "B118W15W"), n0,
                 "A3 ★ **换牌补抽不发 `WhenEventKind.Draw` 广播**（手牌监听计数不动）"
               + "｜🧨 把 `Mulligan` 那行改回 `Draw(ctx, player)` ⇒ 补抽 2 次 ⇒ 计数 +4 ⇒ 红");

        // ---- 反一刀切：普通抽牌**仍然**广播 ----
        RuleCore.EndMulligan(ctx);
        Check(!ctx.MulliganOpen, "A4 前提：`EndMulligan` 之后阶段关闭（下面那次抽牌是「真·抽牌」）");
        int n1 = HandEffectCount(ctx, 0, "B118W15W");
        RuleCore.Draw(ctx, 0);
        Check(HandEffectCount(ctx, 0, "B118W15W") > n1,
              "A5 ★ **普通抽牌仍然广播**（`Draw` 缺省 `asDraw: true`）—— 实得 "
            + n1 + " → " + HandEffectCount(ctx, 0, "B118W15W")
            + "｜🧨 把广播一刀切关掉（删掉 `if (asDraw)` 那一支 / 让缺省变 `false`）⇒ 这条红");
    }

    // ==================================================================
    //  B · `D118-56`：场上出生的视图也建 2D 卡面四层（初始隐藏），回手打开
    // ==================================================================
    //  原版判据：`CardPrefab.prefab` 里 `2DCard`（含 `Front/CardFrame` / `Textbackgrounds` /
    //    `Cost Container` / `Rarity`）与 `Card 3D` **两套都在**，靠
    //    `BattleCardUI__ShowBoardObjects.c:13-25` 的 `Card2DController.SetCardVisibility(false)` +
    //    `Toggle(show ^ 1)` **切显隐** —— ⛔ 不是「场上就把 2D 那棵砍掉」。
    static CardView MakeBoardCard(Transform root, string name, CardFace face)
    {
        var d = CardData.Simple(name, 3, 2, 4);
        d.faction = "Ultramarines";           // ⚠️ 不给阵营 ⇒ `CardArt.Frame` 返 null ⇒ 走占位支路、`_frame` 建不出来
        d.rarity = "common";
        d.keywords = "Deal 1 damage to an enemy.";   // 有 desc ⇒ 卡名 + 关键词两层都建得出来（`Fill` 对空串返回 null）
        return CardView.Create(root, d, name, face);
    }

    static MeshRenderer Layer(CardView v, string name)
    {
        if (v == null) return null;
        foreach (var r in v.GetComponentsInChildren<MeshRenderer>(true))
            if (r != null && r.gameObject.name == name) return r;
        return null;
    }

    static bool LayerActive(CardView v, string name)
    {
        var r = Layer(v, name);
        return r != null && r.gameObject.activeSelf;
    }

    static void B_BoardHasFull2DCard(Transform root)
    {
        var v = MakeBoardCard(root, "B118W15_board", CardFace.Board);
        Check(v != null, "B0 ★ 前提：**出生就在场上**的探针视图建出来了");
        if (v == null) return;
        Check(v.Face == CardFace.Board, "B0b 前提：形态确实是 `Board`（出生入口，⛔ 不是从手牌搬过去的）");
        Check(v.Body3DVisible, "B0c 前提：这张卡有 3D 卡体（否则下面「只 3D」那几条没有鉴别力）");

        // ---- ① 四层**存在**（改前 `if (!board)` ⇒ 全是 null）----
        Check(v.FrameLayer != null, "B1 ★ 场上出生也**建了卡框层**（`FrameLayer != null`）"
                                  + "｜🧨 把那行改回 `if (!board) { _frame = ... }` ⇒ null ⇒ 红");
        Check(Layer(v, "textBg") != null, "B2 ★ 文字底板层建出来了（`textBg`）");
        Check(Layer(v, "costBg") != null, "B3 ★ 费用底板层建出来了（`costBg`）");
        Check(Layer(v, "gem") != null, "B4 ★ 稀有度宝石层建出来了（`gem`）");

        // ---- ② 初始**隐藏**（⛔ 不许一出生就露出来）----
        Check(!v.FrameVisible, "B5 ★ 卡框层**初始隐藏**（`FrameVisible == false`）—— 场上看得到的只有 3D 体");
        Check(!LayerActive(v, "textBg"), "B6 ★ 文字底板**初始隐藏**");
        Check(!LayerActive(v, "costBg"), "B7 ★ 费用底板**初始隐藏**");
        Check(!LayerActive(v, "gem"), "B8 ★ 宝石层**初始隐藏**");
        Check(!v.ArtVisible, "B9 ★ 2D 立绘层也是关的（既有口径：场上 3D 体与 2D 立绘**二选一**）"
                            + "｜🧨 把任意一处 `Show(x, false)` 删掉 ⇒ 场上一眼双份/多一块框 ⇒ 红");

        // ---- ③ 队列在段内 ----
        v.SetQueue(2965);
        CheckNum(v.Queue, 2965, "B10 前提：`SetQueue(2965)` 生效（场上那一段 2962–2979 里的一个值）");
        Check(Layer(v, "frame").sharedMaterial != null && Layer(v, "frame").sharedMaterial.renderQueue == 2965,
              "B11 ★ 卡框层跟着这张卡换段（`renderQueue == 2965`）｜实得 "
            + (Layer(v, "frame").sharedMaterial != null ? Layer(v, "frame").sharedMaterial.renderQueue : -1));
        Check(Layer(v, "textBg").sharedMaterial.renderQueue == 2965, "B11b 文字底板同段");
        Check(Layer(v, "costBg").sharedMaterial.renderQueue == 2965, "B11c 费用底板同段");
        Check(Layer(v, "gem").sharedMaterial.renderQueue == 2965, "B11d 宝石层同段");

        // ---- ④ 回手（换形态）⇒ 四层**可见**，且 2D/3D 二选一仍对 ----
        v.SetFace(CardFace.Hand);
        Check(v.Face == CardFace.Hand, "B12 前提：`SetFace(Hand)` 真的换了形态（⛔ 不是早退）");
        Check(v.FrameVisible, "B13 ★ 回手之后**卡框可见**（原来是「一块没有卡框的矩形」）");
        Check(LayerActive(v, "textBg"), "B14 ★ 文字底板可见");
        Check(LayerActive(v, "costBg"), "B15 ★ 费用底板可见");
        Check(LayerActive(v, "gem"), "B16 ★ 宝石层可见");
        Check(v.ArtVisible && !v.Body3DVisible, "B17 ★ 2D / 3D 二选一仍对：手牌形态**只 2D**（`ArtVisible` 且 `!Body3DVisible`）");

        // ---- ⑤ 卡名 / 关键词（`Build` 里那条 `if (_faceMode != Board) BuildTextLayers` 仍是懒建，
        //        换形态那一趟（`SetFace` 尾部）会把它建出来 —— 所以这两条钉的是「回手之后有字」）----
        Check(TmpFont.Available, "B18 前提：字体资产可用（否则下面两条文字层断言没有意义）");
        Check(v.TitleText != null && v.TitleText.gameObject.activeSelf, "B19 ★ 回手之后**卡名层存在且可见**");
        Check(v.KeywordsText != null && v.KeywordsText.gameObject.activeSelf, "B20 ★ 回手之后**关键词层存在且可见**");
    }

    // ==================================================================
    //  B2 · 第二轮：文字层**在 2D 淡入那一拍**建出来，且与框同一组 α
    // ==================================================================
    //  原版 `2DCard` 是**一个 CanvasGroup** ⇒ 框/字/图同一时刻一起淡（`BattleCardUI__ShowBoardObjects.c:13-25`
    //  只切整组显隐）。改前：文字要到 `SetFace(Hand)` 才建 ⇒ 回手那 0.6 s「有框有图、没有字」。
    //  本轮：`CardFeel.ReturnToHand` 的 `artOn` 回调里先 `BuildHandTextLayersNow()`（只建层、不碰 α），
    //  紧接着那一句 `SetAlpha2D(0f)` 把整组（含字）一起压到 0，再由同一条 tween 淡回 1。
    static void B2_TextOnFadeIn(Transform root)
    {
        var v = MakeBoardCard(root, "B118W15_return", CardFace.Board);
        Check(v != null, "B21 ★ 前提：回手探针（**场上出生**）建出来了");
        if (v == null) return;
        Check(v.Face == CardFace.Board && v.TitleText == null && v.KeywordsText == null,
              "B21b ★ 前提：回手**之前**文字层**还不存在**（`Build` 对场上档不建 —— 这正是本轮要治的缺陷）");

        float artOn = CardFeel.HandToBoardEnd - CardFeel.HandToBoardDissolveEnd;          // 0.2167
        float fadeA = CardFeel.HandToBoardEnd - CardFeel.HandToBoardFadeEnd;              // 0.25
        float fadeB = CardFeel.HandToBoardEnd - CardFeel.HandToBoardFadeStart;            // 0.5834

        bool keep = CardFeel.ForceNoDissolveForTest;
        CardFeel.ForceNoDissolveForTest = true;      // 走兜底支路（两支逐时刻一致；不依赖溶解 shader）
        var tw = CardFeel.ReturnToHand(v, 0f, null);
        CardFeel.ForceNoDissolveForTest = keep;
        Check(tw != null, "B22 前提：回手序列起播了");
        if (tw == null) return;

        // ---- 淡入那一拍（artOn + 0.05 s）----
        Step(artOn + 0.05f);
        Check(v.TitleText != null && v.KeywordsText != null,
              "B23 ★ 2D 淡入开始那一刻，**文字层已经建出来了**（卡名 + 关键词都不是 null）"
            + "｜🧨 删掉 `CardFeel` 那两句 `BuildHandTextLayersNow()` ⇒ 这里仍为 null ⇒ 红");
        Check(v.TitleText != null && v.TitleText.gameObject.activeSelf,
              "B23b ★ 而且它们是**开着**的（`SetFaceGroupVisible(true)` 那一步要关的就这几层）");
        float a1 = v.TitleText != null ? v.TitleText.color.a : -1f;
        Check(a1 >= 0f && a1 < 0.2f,
              "B24 ★ 但**是淡的、不是实字**（α≈0；实得 " + a1.ToString("F3") + "）"
            + "｜🧨 在 `SetAlpha2D(0f)` **之后**才建 / 建完把字设成 1 ⇒ 这条红");

        // ---- 淡入窗口的中点 ----
        Step((fadeA + fadeB) * 0.5f - (artOn + 0.05f));
        float ta = v.TitleText != null ? v.TitleText.color.a : -1f;
        float fa = v.FrameMaskAlphaNow;
        Check(ta > 0.15f && ta < 0.85f, "B25 前提：中点时字确实在**半途**（α = " + ta.ToString("F3") + "，应在 0.15–0.85）");
        Check(Mathf.Abs(ta - fa) < 0.02f,
              "B26 ★ 中点：文字 α 与**卡框** α **同值**（= 同一组 CanvasGroup 一起淡；"
            + "字 " + ta.ToString("F3") + " vs 框 " + fa.ToString("F3") + "）"
            + "｜🧨 让文字走一条与框不同步的补间 ⇒ 这条红");

        Step(1f);       // 走完（总 0.9167 s）⇒ 收口
        tw.Kill();
        Check(Mathf.Abs(v.Alpha2D - 1f) < 1e-4f, "B27 收尾：整组 α 回 1（`OnKill` 收口仍在）");
    }

    // ==================================================================
    //  C · 源码级灭自证（防「两边一起改回去」）
    // ==================================================================
    static string ReadSrc(params string[] parts)
    {
        string p = System.IO.Path.Combine(Application.dataPath, System.IO.Path.Combine(parts));
        return System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p) : null;
    }

    static void C_SourceGuards()
    {
        string core = ReadSrc("RuleEngine", "Core", "RuleCore.cs");
        Check(core != null, "C0 前提：读得到 `RuleEngine/Core/RuleCore.cs`");
        if (core != null)
        {
            Check(core.Contains("Draw(ctx, player, asDraw: false)"),
                  "C1 ★ `Mulligan` 的补抽**走的是静默口**（`asDraw: false`）｜🧨 改回 `Draw(ctx, player)` ⇒ 红");
            Check(core.Contains("if (asDraw) BroadcastWhen(ctx, WhenEventKind.Draw"),
                  "C2 ★ 广播那一句**仍**被 `if (asDraw)` 挡着（⛔ 别把闸拆了）");
            Check(core.Contains("if (asDraw && !ctx.MulliganOpen) ctx.Emit(EvtKind.Draw"),
                  "C3 ★ `EvtKind.Draw` 同闸（原版三件同闸）");
            Check(core.Contains("if (asDraw) inst.DrawnThisTurn = true;"),
                  "C4 ★ `DrawnThisTurn` 同闸（原版 `SetCardTurnDrawn` 也在 `IsDuringMulligan == 0` 里）");
        }

        string view = ReadSrc("CardPresentation", "Core", "CardView.cs");
        Check(view != null, "C6 前提：读得到 `CardPresentation/Core/CardView.cs`");
        if (view != null)
        {
            Check(view.Contains("if (board) Show(_frame, false);"), "C7 ★ 卡框层建后**初始隐藏**那一句在");
            Check(view.Contains("if (board) Show(_textBg, false);"), "C8 ★ 文字底板同上");
            Check(view.Contains("if (board) Show(_costBg, false);"), "C9 ★ 费用底板同上");
            Check(view.Contains("if (board) Show(_gem, false);"), "C10 ★ 宝石层同上");
            Check(!view.Contains("if (!board) { _frame = AddLayer"),
                  "C11 ★ 旧形状（场上不建卡框）**没有**被搬回来｜🧨 恢复 `if (!board)` ⇒ 红");
            Check(view.Contains("public void BuildHandTextLayersNow()"),
                  "C12 ★ 第二轮那个「按手牌形态建文字层」的入口在（删掉它 ⇒ 回手那 0.6 s 又变成有框没字）");
        }

        string feel = ReadSrc("CardPresentation", "Core", "CardFeel.cs");
        Check(feel != null, "C13 前提：读得到 `CardPresentation/Core/CardFeel.cs`");
        if (feel != null)
            Check(Count(feel, "BuildHandTextLayersNow()") == 2,
                  "C14 ★ `ReturnToHand` 的**两支**（溶解 / 兜底）都在 `artOn` 那一拍建文字层"
                + "｜实得 " + Count(feel, "BuildHandTextLayersNow()") + "（应为 2）");
    }

    static int Count(string hay, string needle)
    {
        if (hay == null || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = hay.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    // ==================================================================
    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        int keepFormat = CardView.HandTextFormat;
        var root = new GameObject("B118W15_probe");
        root.transform.position = new Vector3(0f, 160f, 0f);
        try
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            CardTween.LinkEnabled = false;
            CardView.SetHandTextFormat(0);   // B19/B20 要读「卡名 + 关键词两层都建出来」⇒ 必须是初始文字态，⛔ 不随全局设置飘
            Group("A·换牌补抽不广播", A_MulliganRefillSilent);
            Group("B·场上出生也建 2D 卡面", () => B_BoardHasFull2DCard(root.transform));
            Group("B2·文字层在淡入那一拍建出来", () => B2_TextOnFadeIn(root.transform));
            Group("C·源码级灭自证", C_SourceGuards);
        }
        finally
        {
            CardTween.Mode = keepMode;
            CardTween.LinkEnabled = keepLink;
            CardView.SetHandTextFormat(keepFormat);
            Object.DestroyImmediate(root);
        }
        Debug.Log(Tag + " 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }
}
