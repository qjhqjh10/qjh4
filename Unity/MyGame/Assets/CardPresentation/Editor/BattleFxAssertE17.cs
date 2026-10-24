// BattleFxAssertE17.cs — 执行代理 `E17`（2026-10-24）的断言宿主
//
// 本文件**只放断言**（⛔ 不改任何产品行为、不建场景、不进 `Editor/BattleScene.cs`）。
// 调用方式（主对话在 `BattleScene.RunCore` 的收尾处加一行）：
//
//     BattleFxAssertE17.Run();
//     pass += BattleFxAssertE17.Pass; fail += BattleFxAssertE17.Fail;    // 想并进总数就加这句
//
// ⚠️ **需要 driver 实例**（取现成的 `UnitChatPanel` / `SkillPanel`）；
//    找不到就**报红**（⛔ 不静默跳过）。静态那一段（打字机结构）不依赖 driver，照跑。
//
// ==================================================================
//  两件覆盖的内容（判据逐条写在每条断言的消息里）
// ------------------------------------------------------------------
//  A. **打字机节拍**（判据 = `GameStaticData.timeBetweenCharsTypewriter` = **0.01 s/字**）：
//     · `dump.cs:119528` 字段 `0x1B8`；值 = `GameStaticData__.cctor.c:246-247` 写死 `0x3c23d70a`；
//     · 结构 = **先等一拍再显第 1 字、之后每字一拍，总时长 = 字数 × 0.01**
//       （`VoiceLineBox._AnimateText_d__22__MoveNext.c:23,27,29,43,59,63,65`）。
//     ⇒ 断言分两层：① 纯函数层（`TypewriterVisibleAt` / `TypewriterSecondsFor` 的时刻表）；
//       ② 真面板层（`UnitChatPanel.Speak` + `Advance`，读 TMP 的 `maxVisibleCharacters`）。
//     🧨 灭自证：把节拍改回 **0.03** ⇒ ①「t=0.005 仍是 0 字」与 ②「t=0.106 还没打完」会红；
//        把「先等一拍」改回「第 1 字当场出来」⇒ ①「t=0 应为 0 字」红。
//  B. **技能面板淡出 = 0.1 s、且 `TargetsAvailableText` 不参与淡出**
//     （判据 = `ActiveSkillDisplay__FadeOut.c:26` 的 `fadeTime × DAT_1834b2bb4(=0.5)` ·
//      `__FadeIn.c:55` 的 `× DAT_1834b2bb8(=1.0)` ·
//      `__SetAlpha.c:40-49` / `__FadePower.c:20-42` 的 6 件**不含** `targetsAvailableText(+0x78)`）。
//     🧨 灭自证：淡出时长改回 0.2 ⇒ 「t=0.105 已收完」红；
//        把 `_targets` 乘回 `Alpha` ⇒ 「淡出前后 colour.a 不变」红。
// ==================================================================
using System.Collections.Generic;
using CardPresentation;
using DG.Tweening;               // `UpdateType.Manual`
using RuleEngine;
using UnityEngine;

public static class BattleFxAssertE17
{
    const string P = "[E17] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进 `BattleScene` 的总数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;

        bool CheckTrue(bool ok, string msg)
        {
            if (ok) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
            Fail++; Debug.LogError(P + $"   ✗ {msg}"); return false;
        }
        bool Check<T>(T got, T want, string msg)
        {
            bool ok = EqualityComparer<T>.Default.Equals(got, want);
            return CheckTrue(ok, msg + $"（实得 {got} / 期望 {want}）");
        }
        bool Near(float got, float want, float tol, string msg)
        {
            return CheckTrue(Mathf.Abs(got - want) <= tol, msg + $"（实得 {got:F4} / 期望 {want:F4} ±{tol:F4}）");
        }

        // ---- A① 打字机：纯结构（不需要 fixture）----
        RunTypewriterStructure(CheckTrue, Check, Near);

        var driver = Object.FindObjectOfType<BattleDriver>();

        // ---- A② 打字机：真面板推帧 ----
        if (driver == null || driver.UnitChat == null)
        {
            CheckTrue(false, "找不到 `BattleDriver.UnitChat`（A② 那一段没法验 —— ⛔ 不许静默跳过）");
        }
        else
        {
            RunTypewriterLive(driver.UnitChat, CheckTrue, Check, Near);
        }

        // ---- B 技能面板 ----
        var sp = driver != null ? driver.skillPanel : null;
        if (sp == null) sp = Object.FindObjectOfType<SkillPanel>();
        if (sp == null)
        {
            CheckTrue(false, "找不到 `SkillPanel`（B 节那一段没法验 —— ⛔ 不许静默跳过）");
        }
        else
        {
            RunSkillPanelFade(sp, CheckTrue, Check, Near);
        }

        Debug.Log(P + $"=== E17 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ==================================================================
    //  A① · 打字机节拍：时刻表（原版 0.01 s/字 · 先等一拍）
    // ==================================================================
    static void RunTypewriterStructure(System.Func<bool, string, bool> CheckTrue,
                                       System.Func<int, int, string, bool> CheckInt,
                                       System.Func<float, float, float, string, bool> Near)
    {
        Debug.Log(P + "--- A① · 打字机节拍 = 0.01 s/字、先等一拍（判据 `GameStaticData.timeBetweenCharsTypewriter`）---");

        // 节拍的值本身（对**原版常量** 0.01，⛔ 不是对自己）。
        Near(UnitChatPanel.TypewriterCharSeconds, 0.01f, 1e-6f,
             "★ 节拍 = 原版 `timeBetweenCharsTypewriter` = 0.01 s/字"
           + "｜🧨 改回 0.03 ⇒ 红（`GameStaticData__.cctor.c:246-247` 的 `0x3c23d70a`）");

        const int N = 10;
        // 总时长 = 字数 × 0.01（原版 `AnimateText` 打完那一刻）。期望值写死成算式，
        // ⛔ 不读 `TypewriterSecondsFor` 自己的返回值当期望（那是自证）。
        Near(UnitChatPanel.TypewriterSecondsFor(N), N * 0.01f, 2e-4f,
             $"★ {N} 个字的打字总时长 = {N} × 0.01 s（原版 `AnimateText`：先等一拍 + 每字一拍）");

        CheckInt(UnitChatPanel.TypewriterVisibleAt(N, 0f), 0,
                 "★ t=0（刚 Speak）⇒ **一个字都不显**（原版第 1 句就是 `maxVisibleCharacters = 0` 再等一拍）");
        CheckInt(UnitChatPanel.TypewriterVisibleAt(N, 0.005f), 0,
                 "★ t=0.005（第一拍的中间）⇒ 仍是 0 个字"
               + "｜🧨 改回「第 1 字当场出来」或节拍 0.03 ⇒ 红");
        CheckInt(UnitChatPanel.TypewriterVisibleAt(N, 0.011f), 1,
                 "★ t=0.011 ⇒ 第 1 个字（先等的那一拍走完）");
        CheckInt(UnitChatPanel.TypewriterVisibleAt(N, 0.055f), 5,
                 "★ t=0.055 ⇒ 5 个字（每字一拍 · 0.01）｜🧨 节拍 0.03 ⇒ 实得 2 ⇒ 红");
        CheckInt(UnitChatPanel.TypewriterVisibleAt(N, 0.095f), 9,
                 "★ t=0.095（= 10×0.01 − 半拍）⇒ 9 个字、还差最后一拍");
        CheckInt(UnitChatPanel.TypewriterVisibleAt(N, 0.106f), N,
                 "★ t=0.106 ⇒ 全显（总时长 = 字数 × 0.01，不是 (字数−1) × 节拍）");
    }

    // ==================================================================
    //  A② · 打字机：真面板（`Speak` → `Advance` → 读 TMP `maxVisibleCharacters`）
    // ==================================================================
    static void RunTypewriterLive(UnitChatPanel chat,
                                  System.Func<bool, string, bool> CheckTrue,
                                  System.Func<int, int, string, bool> CheckInt,
                                  System.Func<float, float, float, string, bool> Near)
    {
        Debug.Log(P + "--- A② · 打字机真面板（`Advance` 推帧、读 TMP 实际显了几个字）---");

        const string Line = "ABCDEFGHIJ";       // 10 个纯 ASCII ⇒ `CharacterCount` 必须是 10
        chat.HideAll();
        // `cardId`/`evt` 都给**不会命中官方词条**的值（否则台词会被 `Chat_Messages/*` 覆盖、字数对不上）。
        bool spoke = chat.Speak(1, null, "E17Probe", null, null, Line, null, expectVoice: false);
        if (!CheckTrue(spoke, "★ 前提：`Speak` 说得成（敌方位那颗气泡建出来了）"))
        {
            chat.HideAll();
            return;
        }

        CheckInt(chat.TypeTotalForTest, Line.Length,
                 $"★ 这一句排出来 {Line.Length} 个字（TMP `characterCount`）");
        CheckInt(chat.VisibleCharsForTest, 0,
                 "★ Speak 之后**当场 0 个字**（原版先等一拍）");

        chat.Advance(0.005f);
        CheckInt(chat.VisibleCharsForTest, 0,
                 "★ t=0.005 ⇒ 仍是 0 个字（第一拍没走完）｜🧨 节拍 0.03 / 第 1 字当场出 ⇒ 红");
        CheckTrue(chat.TypingForTest, "★ t=0.005 还在打字");

        chat.Advance(0.006f);                   // t = 0.011
        CheckInt(chat.VisibleCharsForTest, 1,
                 "★ t=0.011 ⇒ 显第 1 个字（先等一拍 · 之后每字一拍）");

        chat.Advance(0.044f);                   // t = 0.055
        CheckInt(chat.VisibleCharsForTest, 5,
                 "★ t=0.055 ⇒ 显 5 个字（每字 0.01 s）｜🧨 节拍 0.03 ⇒ 实得 2 ⇒ 红");

        chat.Advance(0.040f);                   // t = 0.095
        CheckTrue(chat.VisibleCharsForTest == 9 && chat.TypingForTest,
                  $"★ t=0.095（= {Line.Length}×0.01 − 半拍）⇒ 显 9 个字、还在打"
                + $"（实得 {chat.VisibleCharsForTest} 个字 / typing={chat.TypingForTest}）"
                + "｜🧨 总时长写成 (字数−1)×节拍 ⇒ 这时已经打完 ⇒ 红");

        chat.Advance(0.011f);                   // t = 0.106
        CheckTrue(chat.VisibleCharsForTest > 9000 && !chat.TypingForTest,
                  $"★ t=0.106 ⇒ 全显（`maxVisibleCharacters` = 全显档）、打字结束"
                + $"（实得 {chat.VisibleCharsForTest} / typing={chat.TypingForTest}）");

        // 附带把「时长」的读口也钉一下（气泡寿命从这一刻起算）。
        Near(UnitChatPanel.TypewriterSecondsFor(chat.TypeTotalForTest), 10 * 0.01f, 2e-4f,
             "★ 这一句的打字总时长 = 字数 × 0.01 s");

        chat.HideAll();
    }

    // ==================================================================
    //  B · 技能面板：淡入 0.2 s / 淡出 0.1 s · `TargetsAvailableText` 不参与淡出
    // ==================================================================
    static void RunSkillPanelFade(SkillPanel sp,
                                  System.Func<bool, string, bool> CheckTrue,
                                  System.Func<int, int, string, bool> CheckInt,
                                  System.Func<float, float, float, string, bool> Near)
    {
        Debug.Log(P + "--- B · 技能面板：淡入 0.2 s / 淡出 0.1 s（`fadeTime × 0.5`）· 那一行不参与淡出 ---");

        Near(SkillPanel.FadeTime, 0.2f, 1e-6f, "★ 淡入时长 = 原版 `fadeTime` = 0.2 s");
        Near(SkillPanel.FadeOutTime, 0.1f, 1e-6f,
             "★ 淡出时长 = `fadeTime × 0.5` = 0.1 s（`ActiveSkillDisplay__FadeOut.c:26` 的 `DAT_1834b2bb4`）"
           + "｜🧨 改回 0.2 ⇒ 红");

        var savedMode = CardTween.Mode;
        bool savedVisible = sp.Visible;
        string savedName = sp.ShownName;
        int savedTargets = sp.ShownTargets;
        CardTween.Mode = UpdateType.Manual;      // 批处理没有帧循环：只泵补间
        try
        {
            // 干净的起点（`Hide()` 在已隐藏时走 `HideImmediate` 那支）。
            sp.Hide();
            CardTween.Advance(0.3f);
            CheckTrue(!sp.Visible && sp.Alpha <= 1e-4f, "★ 前提：面板先收干净（alpha = 0）");

            var targets = sp.TextForTest(3);     // 3 = `TargetsAvailableText`
            if (!CheckTrue(targets != null, "★ 前提：`TargetsAvailableText` 建出来了"))
                return;                          // `finally` 会还原现场

            // ---- 淡入：0.2 s ----
            sp.Show("E17 Probe", (EffectSpec)null, 0);
            CheckTrue(sp.Visible, "★ 面板弹出来了");
            CheckTrue(targets.gameObject.activeSelf, "★ 那一行跟着面板亮起来（它不参与淡入，靠开关 GameObject 藏）");
            CardTween.Advance(0.15f);
            CheckTrue(sp.Alpha > 0.05f && sp.Alpha < 0.999f,
                      $"★ 淡入在 t=0.15 时**还没完**（0.2 s 那一档；实得 alpha {sp.Alpha:F3}）"
                    + "｜🧨 把淡入也改成 0.1 ⇒ 这时已经到 1 ⇒ 红");
            CardTween.Advance(0.07f);            // 累计 0.22 s
            Near(sp.Alpha, 1f, 1e-3f, "★ 淡入走完（t=0.22 ⇒ alpha = 1）");

            // ---- 那一行的 alpha：淡出**之前** ----
            float targetsAlphaBefore = targets.color.a;
            Near(targetsAlphaBefore, 1f, 1e-4f,
                 "★ 淡出前：`TargetsAvailableText` 的 α = 基色 α（1.0）");

            // ---- 淡出：0.1 s ----
            sp.Hide();
            CheckTrue(!sp.Visible, "★ 面板进入收起态");
            CardTween.Advance(0.05f);
            CheckTrue(sp.Alpha > 0.10f,
                      $"★ t=0.05（淡出走到一半）alpha 仍明显 > 0（实得 {sp.Alpha:F3}）—— 0.1 s 那一档");
            CardTween.Advance(0.055f);           // 累计 0.105 s > 0.1
            CheckTrue(sp.Alpha <= 1e-3f,
                      $"★ t=0.105 ⇒ **淡出已结束**（alpha {sp.Alpha:F4}）"
                    + "｜🧨 淡出改回 0.2 ⇒ 这时 alpha ≈ 0.23 ⇒ 红");

            // ---- 那一行的 alpha：淡出**之后**（原版它从来不参与 α 补间） ----
            float targetsAlphaAfter = targets.color.a;
            Near(targetsAlphaAfter, targetsAlphaBefore, 1e-6f,
                 "★ 淡出前后 `TargetsAvailableText` 的 α **一字不变**"
               + "｜🧨 把 `ApplyColors` 里那件乘回 `Alpha` ⇒ 淡出后变 0 ⇒ 红");
            CheckTrue(!targets.gameObject.activeSelf,
                      "★ 面板收完后那一行**整个关掉**（它不参与淡出，只能靠开关 GameObject 藏）");
        }
        finally
        {
            // 还原现场：面板收干净、补间模式交回去；本来就是开着的就照原名摆回去。
            sp.Hide();
            CardTween.Advance(SkillPanel.FadeOutTime + 0.05f);
            CardTween.Mode = savedMode;
            if (savedVisible && !string.IsNullOrEmpty(savedName)) sp.Show(savedName, (EffectSpec)null, savedTargets);
        }
    }
}
