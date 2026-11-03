// B120_W2_Assert.cs — 执行写手 `B120-W2`（2026-11-02）· `资料/完全复刻/报告/B120_战斗参数审计_总报告.md` §二 的 8 条
//
// ============================ 它盯的是什么 ============================
//  A 域（手牌/场上几何）· B 域（HUD）· C 域（动画时序）· F 域（音效路由）八条：
//    A-1  小屏档手牌【间距】要 ×1.365（`GetPosition.c:63-70` + `PlayerHand__Initialize.c`）· 横向跨度上限**不乘**
//    A-8  小屏档【悬停缩放】×1.365、【悬停抬起量】不乘（`CardScript__ShowCardInHand.c:38-79,108-135`）
//    A-10 `SpellDragAimYOffset` 原版 0.2 **世界单位** = 2.9673 px ⇒ 我们 0.027475（`VarsDesktop.json` + 相机算式）
//    B-5  结算面板副标题：不知道最低生命时传 **-1 = 不编数**（`EndPanel.cs:658-660`；原版那子树零文字）
//    B-6  疲劳横幅 0.5 s 要挂在**伤害演完之后**（`_ResolveFatigue`：`ShowError` → `DealMultiDamage` → `WaitForSeconds(0.5)`）
//    C-1  教程开局要播 `BattleHudShowNoHand`（`_TutorialStartSequence_d__592__MoveNext.c:283`）
//    F-1  单位语音：**无混音组（走 Master）+ 音效滑块音量 + `SoundFx <= 0` 整族不播**
//    DOC3 同一段的过期注释（`AudioListener.volume`）已就地订正
//
// ============================ 🔴 灭自证（哪一处改回去会让哪一条红）============================
//  🧨 A-1：把 `hl.betweenElementsSpacing = _handScaleBaseSpacing * m;` 改回不动 ⇒ A1-a/A1-b 红（213.7 px / 5 张起压）。
//  🧨 A-1：把 `hl.maxLayoutSizeSmallScreen = _handScaleBaseMaxSpan;` 改回 `* m` ⇒ A1-c 红（字段变 0.69615）。
//  🧨 A-8：把 `hl.hoverLift = _handScaleBaseHoverLift;` 改回 `* m` ⇒ A8-b 红（607 px）；把 `hoverScale` 不乘 ⇒ A8-a 红。
//  🧨 A-10：把常量改回 `0.2f` ⇒ A10 红（21.6 px）。
//  🧨 B-5：把调用点改回 `? 30 :` ⇒ B5 红（源码切片里出现 `30`）。
//  🧨 B-6：把 0.5 s 挪回 `beatGap` 之后（事件**起播前**）⇒ B6-b/B6-c 同时红
//           （起播间隔多 0.5，而队尾相对量少 0.5 —— 两个方向**不可能同时**满足）。
//  🧨 C-1：删掉 `_hudAnim.Play(BattleHudAnim.ShowNoHandClip)` ⇒ C1 红（`CurrentClipName` 不是它）。
//  🧨 F-1：把音源挂回 `Voices` ⇒ F1-c 红；把音量改回 `AudioListener.volume` ⇒ F1-b 红（记到 0.25 而不是 0.5）；
//           把闸删掉 ⇒ F1-a 红（`SoundFx = 0` 时计数仍然 +1）。
//
// ============================ 覆盖边界（如实记）============================
//  · **不跑 Unity 帧循环**：批处理 `Application.isPlaying == false` ⇒ `BattleHudAnim.Play` 直接落终态，
//    C1 只断「播的是**哪一条**」，「1 s 的滑动过程」由 `BattleScene` 的 E-C 那组几何断言覆盖（⛔ 不重复）。
//  · **B-5 只验调用点**：`EndPanel.Show` 的 `-1 ⇒ 只印回合数` 已在它自己的 doc 里（`EndPanel.cs:658-660`），
//    本件不再驱动一次真结算（那要打完整局）；源码切片只钉「调用点传的是 -1，不是 30」。
//  · **F-1 没有出声验证**（批处理 `Time.time` 恒 0、`AudioSetup.PlayCheck` 要 Play 模式）：只验**路由 + 音量实参 + 闸**。
//  · **A-1 的阈值断言按 16:9 算**：宿主若不是 16:9，`A1-b` 的前提那条会先红（⛔ 不是静默跳过）。
//  · 本件会 `Begin` / `BeginTutorial` 换局 ⇒ **接在宿主聚合行的【最后一跳】**（其后没有断言读对局状态）；
//    收尾会 `Begin` 回一局普通对局，并把 `SmallScreenUI` / `SoundFx` / `AudioListener.volume` 全部还原。
using System;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;
using MatchType = RuleEngine.MatchType;   // 🆕 2026-11-02：与 `System.IO.MatchType` 同名（CS0104）

public static class B120_W2_Assert
{
    const string Prefix = "[B120W2] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    static void Near(float got, float want, float tol, string what)
    {
        Check(Mathf.Abs(got - want) <= tol, what + $"（实得 {got:F5} / 期望 {want:F5} ±{tol:F5}）");
    }

    /// <summary>宿主入口（`BattleScene.Run` 的聚合行调；`driver` 必须**还活着**）。</summary>
    public static void Run(BattleDriver driver)
    {
        Debug.Log(Prefix + "=== B120 战斗参数审计 · W2 八条（A-1 / A-8 / A-10 / B-5 / B-6 / C-1 / F-1 / DOC3）===");
        // 前置：本件全部读口都要求 HUD / 手牌件在位 ⇒ 先自己开一局（同 `B116W42_Assert` 的手法）
        if (driver != null)
        {
            try { driver.Begin("Ultramarines", "Goff", 20260916); driver.RefreshAll(); }
            catch (Exception e) { Debug.LogWarning(Prefix + "前置 Begin 失败：" + e.Message); }
        }
        RunSmallScreenHand(driver);
        RunSpellAimOffset(driver);
        RunUnitVoice(driver);
        RunEndPanelSubtitleSource();
        RunFatigueOrder(driver);
        RunTutorialHudSlideIn(driver);
        RunW2b(driver);
        // ---- 收尾：还原成一个普通对局（后面的小节复用同一个 driver）----
        if (driver != null)
        {
            try { driver.Begin("Ultramarines", "Goff", 20260916); driver.RefreshAll(); } catch (Exception e) { Debug.LogWarning(Prefix + "收尾 Begin 失败：" + e.Message); }
        }
        Debug.Log(Prefix + $"B120W2 合计：{_ok} 通过 / {_bad} 失败");
    }

    // ==================================================================
    //  A-1 / A-8 · 小屏档 ×1.365 落在哪四格
    // ==================================================================
    static void RunSmallScreenHand(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- A-1 / A-8 · 小屏档手牌 ×1.365 ---");
        if (driver == null) { Check(false, "（前提）`driver` 不为 null"); return; }
        var hl = driver.SmallScreenHandLayoutForTest;
        Check(hl != null, "（前提）取得到 `ApplySmallScreenHandScale` 改的那一份 `HandLayout`");
        if (hl == null) return;

        bool prefWas = SmallScreenUI.PersistOverride;
        bool enWas = SmallScreenUI.Enabled;
        const float M = 1.365f;       // = `CombatAutoZoom.PlayerHandSmallScreenSizeMultiplier`（13/13 场两格同值）
        float spacing0 = 0f, hoverScale0 = 0f, hoverLift0 = 0f, maxSpan0 = 0f, cardScale0 = 0f;
        try
        {
            SmallScreenUI.PersistOverride = true;      // 自检不许写玩家真设置（同 `SettingsWindow.QualitySetterOverride` 那一族）

            // 先把基值抓在「小屏档关」那一档（`ApplySmallScreenHandScale` 只在第一次调用时抓基值）
            SmallScreenUI.Set(false);
            driver.ApplySmallScreenHandScaleForTest();
            spacing0 = hl.betweenElementsSpacing;
            hoverScale0 = hl.hoverScale;
            hoverLift0 = hl.hoverLift;
            maxSpan0 = hl.maxLayoutSizeSmallScreen;
            cardScale0 = hl.cardScale;

            SmallScreenUI.Set(true);
            driver.ApplySmallScreenHandScaleForTest();
            Near(CombatAutoZoom.PlayerHandSmallScreenSizeMultiplier, M, 1e-6f,
                 "（前提）小屏档常数 = 1.365（判据 = `PlayerHand__Initialize.c` 插值两端同值）");

            // ---- A-1-a：间距**乘**（16:9 下 156.6 → 213.7 px）----
            Near(hl.betweenElementsSpacing, spacing0 * M, 1e-6f,
                 "★ A1-a **相邻间距 ×1.365**（`GetPosition.c:63-70` 的 `fVar18 = lossyScale.x`）"
               + "｜🧨 改回不动 ⇒ 本行红（仍 156.6 px，而牌宽已经 225 px ⇒ 相邻重叠 ≈68 px）");
            Near(hl.betweenElementsSpacing * 108f, 213.7f, 0.2f,
                 "★ A1-a' 间距换算成设计 px = **213.7**（原版 1.45 × 容器 1.365 × 108）");

            // ---- A-1-b：压缩阈值随之校正 —— 5 张起压 ----
            float asp = LayoutSpace.Cam != null ? LayoutSpace.Cam.aspect : LayoutSpace.DesignAspect;
            Check(Mathf.Abs(asp - 16f / 9f) < 0.01f,
                  $"（前提）宿主在 16:9 下跑（实得 aspect {asp:F4}）—— 下面那条阈值断言按 16:9 算，⛔ 不是静默跳过");
            Check(hl.IsCompressed(5) && !hl.IsCompressed(4),
                  "★ A1-b 小屏档 **5 张起压、4 张不压**（原版同值）"
                + $"（实得 IsCompressed(5)={hl.IsCompressed(5)} / IsCompressed(4)={hl.IsCompressed(4)}）"
                + "｜🧨 间距不乘 ⇒ 9 张才压；跨度上限也乘 ⇒ 7 张才压 ⇒ 两种改坏法都会红");

            // ---- A-1-c：跨度上限**不乘** ----
            Near(hl.maxLayoutSizeSmallScreen, maxSpan0, 1e-6f,
                 "★ A1-c **跨度上限不乘 M**（容器放大后「可见宽（局部）」自己缩了 1.365 ⇒ 折成我们世界单位回到原值）"
               + "｜🧨 改回 `* m` ⇒ 本行红（0.51 → 0.69615，压缩阈值被推到 7 张）");
            Near(maxSpan0, 0.51f, 1e-4f, "（前提）跨度上限基值 = 0.51（MB 5271 `m_maxLayoutSizeSmallScreen`）");

            // ---- A-8-a：悬停缩放**乘**（世界 1.3 × 1.365 = 1.7745）----
            Near(hl.hoverScale, hoverScale0 * M, 1e-6f,
                 "★ A8-a **悬停缩放 ×1.365**（`ShowCardInHand.c:108-135` 局部 1.3 × 容器 1.365）"
               + "｜🧨 不乘 ⇒ 小屏档悬停那张比原版小 1.365 倍");
            Near(hl.hoverScale, 1.7745f, 1e-3f, "★ A8-a' 悬停缩放绝对值 = **1.7745**（= 1.3 × 1.365）");

            // ---- A-8-b：悬停抬起量**不乘**（`PlayerHand.transform.position.y + 30 世界单位` = 445 px）----
            Near(hl.hoverLift, hoverLift0, 1e-6f,
                 "★ A8-b **悬停抬起量不乘 M**（它取自容器自身的 `position.y`，不随 localScale 变）"
               + "｜🧨 改回 `* m` ⇒ 本行红（445 px → 607 px，小屏档悬停高 162 px）");
            Near(hl.hoverLift * 108f, 445f, 0.5f, "★ A8-b' 抬起量 = **445 px**（30 原版世界单位 × 14.8364）");

            // ---- 卡尺度仍按原版乘（这一格改前就是对的，锁住防回退）----
            Near(hl.cardScale, cardScale0 * M, 1e-6f, "★ A1-d（防回退）卡尺度 ×1.365 —— `PlayerHand.Initialize` 的 `transform.localScale`");
        }
        finally
        {
            SmallScreenUI.Set(enWas);
            SmallScreenUI.PersistOverride = prefWas;
            try { driver.ApplySmallScreenHandScaleForTest(); } catch { }
        }
        // 收尾还原：开档 → 间距回到「×M 或 ×1」那一档（同一份判据，别在断言里另写一个数）
        Near(hl.betweenElementsSpacing, spacing0 * (enWas ? M : 1f), 1e-6f,
             "（收尾）`SmallScreenUI` 的开关已还原、手牌三格跟着回到原档");
    }

    // ==================================================================
    //  A-10 · `SpellDragAimYOffset`
    // ==================================================================
    static void RunSpellAimOffset(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- A-10 · 准星 `moveCardYOffset` 的量纲桥 ---");
        if (driver == null) { Check(false, "（前提）`driver` 不为 null"); return; }
        float v = driver.SpellDragAimYOffsetForTest;
        // 原版：HUD 画布 1 世界单位 = 1080 ÷ (2 × planeDistance 100 × tan(40°/2)) = 14.8364 px ⇒ 0.2 = 2.9673 px
        // 我们：HUD 正交平面 1 世界单位 = 1080 ÷ LayoutSpace.DesignHeight 10 = 108 px ⇒ 2.9673 ÷ 108 = 0.027475
        Near(v * 108f, 2.9673f, 0.01f,
             "★ A10 准星上抬 = **2.9673 px**（原版 `moveCardYOffset` 0.2 世界单位 × 14.8364）"
           + "｜🧨 改回 `0.2f` ⇒ 本行红（21.6 px，大 7.28 倍）");
        Check(v < 0.05f, "★ A10' 已经不是「0.2 我们的世界单位」那一档（实得 " + v.ToString("F6") + "）");
    }

    // ==================================================================
    //  F-1 · 单位语音的音量 + 路由 + 闸
    // ==================================================================
    static void RunUnitVoice(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- F-1 · 单位语音（attack / death VO）走 Master + 音效滑块 ---");
        if (driver == null) { Check(false, "（前提）`driver` 不为 null"); return; }
        float fxWas = WarpforgeAudio.SoundFx;
        float listenerWas = AudioListener.volume;
        AudioClip clip = null;
        try
        {
            clip = AudioClip.Create("B120W2Probe", 256, 1, 44100, false);
            Check(clip != null, "（前提）探测 clip 建出来了");

            // ---- F1-a：闸 = `SoundFxVolume > 0`（=0 时整族不播）----
            WarpforgeAudio.SetSoundFx(0f);
            int before = driver.UnitVoiceAudioOnlyCountForTest;
            driver.PlayUnitVoiceClipOnlyForTest(clip);
            Check(driver.UnitVoiceAudioOnlyCountForTest == before,
                  "★ F1-a `SoundFx = 0` ⇒ **整族不播**（`BattleManager__PlaySoundClip.c:28-35` 的闸）"
                + $"（计数 {before} → {driver.UnitVoiceAudioOnlyCountForTest}）"
                + "｜🧨 删掉 `if (WarpforgeAudio.SoundFx <= 0f) return;` ⇒ 本行红");

            // ---- F1-b：音量 = 音效滑块，⛔ 不是 `AudioListener.volume` ----
            WarpforgeAudio.SetSoundFx(0.5f);
            AudioListener.volume = 0.25f;          // 故意设成另一个值 ⇒ 谁被采信一眼看出
            int b2 = driver.UnitVoiceAudioOnlyCountForTest;
            driver.PlayUnitVoiceClipOnlyForTest(clip);
            Check(driver.UnitVoiceAudioOnlyCountForTest == b2 + 1,
                  "（前提）`SoundFx = 0.5` ⇒ 真的走了一次播放（计数 +1）");
            Near(driver.LastUnitVoiceVolumeForTest, 0.5f, 1e-4f,
                 "★ F1-b 音量实参 = **音效滑块 0.5**（`GameStaticData.SoundFxVolume`）"
               + "｜🧨 改回 `AudioListener.volume` ⇒ 本行红（会记到 0.25）");

            // ---- F1-c：路由 = 无混音组 ⇒ Master ----
            var src = driver.UnitVoiceSfxForTest;
            Check(src != null, "（前提）单位语音那颗 `AudioSource` 建出来了（没有 `hudRoot` 时会早退 ⇒ 下面两条空转）");
            Check(src != null && src.outputAudioMixerGroup == null,
                  "★ F1-c 音源 **不挂混音组**（原版 `AudioSource_1595` 的 `OutputAudioMixerGroup` 全 0 ⇒ 走 Master）"
                + (src != null && src.outputAudioMixerGroup != null ? "（实得挂上了 " + src.outputAudioMixerGroup.name + "）" : "")
                + "｜🧨 改回 `WarpforgeAudio.VoicesGroup` ⇒ 本行红（那会跟语音滑块走、并绕开 Master）");
        }
        finally
        {
            WarpforgeAudio.SetSoundFx(fxWas);
            AudioListener.volume = listenerWas;
        }
    }

    // ==================================================================
    //  B-5 · 结算面板副标题「不知道就不编数」（调用点：传 -1）
    // ==================================================================
    static void RunEndPanelSubtitleSource()
    {
        Debug.Log(Prefix + "--- B-5 · 结算副标题：不知道最低生命 ⇒ -1（不编数） ---");
        string src = ReadAsset("CardPresentation/Battle/BattleDriver.cs");
        Check(!string.IsNullOrEmpty(src), "（前提）读得到 `BattleDriver.cs` 源码（下面两条是源码切片断言）");
        if (string.IsNullOrEmpty(src)) return;
        int i = src.IndexOf("_endPanel.Show(", StringComparison.Ordinal);
        Check(i >= 0, "（前提）找得到 `_endPanel.Show(` 那个调用点");
        if (i < 0) return;
        // 🔴 2026-11-03（`B124-W28`）：原来这里切的是**固定 700 字**窗口 —— 本轮就地改判：
        //    `-1` 那行的判据说明一长（250 字），`skipSequence:` 就被挤出了 700 字窗口 ⇒ 前提假红
        //    （切到的根本不是同一个调用）。改成「截到**配对的 `)`**」，与注释长短无关。
        string slice = CallSlice(src, i);
        Check(slice.Contains("int.MaxValue ? -1 :"),
              "★ B5 「最低生命未知」时传 **-1**（= `EndPanel.Show` 的「只说回合数、不编数」语义，`EndPanel.cs:658-660`）"
            + "｜🧨 改回 `? 30 :` ⇒ 本行红");
        Check(!slice.Contains("int.MaxValue ? 30 :"),
              "★ B5' 调用点里**不再出现 `? 30 :`**（遭遇局起始生命 20 ⇒ 印 30 是错数）");
        Check(slice.Contains("skipSequence: TutorialSkipsNormalEnd()"),
              "（前提）这一刀没切错方法（`skipSequence:` 仍在切片里 —— 它就是同一个 `Show(` 调用）");
    }

    // ==================================================================
    //  B-6 · 疲劳 0.5 s 的次序：`ShowError` → 伤害 → `WaitForSeconds(0.5)`
    // ==================================================================
    static void RunFatigueOrder(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- B-6 · 疲劳横幅 0.5 s 挂在【伤害演完之后】 ---");
        if (driver == null || driver.Ctx == null) { Check(false, "（前提）`driver.Ctx` 在位"); return; }
        int seat = driver.MySeat;
        Check(seat >= 0 && seat < driver.Ctx.Players.Length && driver.Ctx.Players[seat].Warlord != null,
              "（前提）我方督军在位（疲劳最后打在督军那一格）");
        if (seat < 0 || seat >= driver.Ctx.Players.Length || driver.Ctx.Players[seat].Warlord == null) return;

        Near(driver.FatigueBannerHoldForTest, 0.5f, 1e-6f,
             "★ B6-a 尾拍常量 = **0.5 s**（`_ResolveFatigue` 的 `WaitForSeconds(0.5)`；`read_literal 0x1834b2bb4`）");

        // 两批**同形**事件：A 批的 Fatigue 量与 Hit 相同 ⇒ 0.5 s 生效；B 批量不同 ⇒ 不生效（对照组）。
        // 组内间隔与队尾都相对「本批第一条事件」量 ⇒ 批与批之间起点不同不影响比较。
        // ⚠️ 量取「记账值 + 7」：`NoteFatigueBanners` 有「`Amount <= _fatigueShown` ⇒ 不弹」的去重闸，
        //    宿主要是刚真打过一次疲劳，写死 3 会被它挡掉（那样两批都成了对照组 ⇒ B6-c 假红）。
        int a0 = driver.FatigueShownForTest(seat) + 7;
        bool okA = ScheduleFatigueProbe(driver, seat, a0, a0, out float gapA, out float tailA);
        bool okB = ScheduleFatigueProbe(driver, seat, a0 + 1, a0, out float gapB, out float tailB);
        Check(okA && okB, "（前提）两批合成事件都排进了时间线（各 2 条）");
        if (!okA || !okB) return;

        // ---- 起播间隔：0.5 不在 `beatGap` 里（否则 A 比 B 多 0.5）----
        Near(gapA, gapB, 1e-4f,
             "★ B6-b 伤害那条 `Hit` 的**起播时刻**没被 0.5 s 推后（有/无横幅两批逐值相同）"
           + "｜🧨 把 0.5 挪回 `t += beatGap;` 之后（事件起播前）⇒ A 比 B 多 0.5 ⇒ 本行红");

        // ---- 队尾：0.5 加在事件**后面**（A 的队尾相对量比 B 多 0.5）----
        Near(tailA - tailB, 0.5f, 1e-4f,
             "★ B6-c 0.5 s 加在**伤害演完之后**（队尾相对量 A − B = 0.5）"
           + "｜🧨 与 B6-b 是**两个方向**：同一处改动不可能让两条同时绿 —— 放在前面则 B6-b 红、放后面且删掉则 B6-c 红");

        // ---- 0.5 是「一次性」的：消费掉之后字段回 null（⛔ 不许每帧重复加）----
        Check(driver.FatigueHoldEventForTest == null,
              "★ B6-d 那一拍**被消费掉**（`_fatigueHoldEvent` 回 null）—— 同一批不会重复加 0.5");
    }

    /// <summary>喂 `[Fatigue, Hit]` 一批，读出「Hit 起播间隔」与「队尾相对第一条事件」。
    /// <para>⚠️ `fatigueAmount != hitAmount` 时 `NoteFatigueBanners` 找不到对应 `Hit` ⇒ 0.5 不生效（对照组）。</para></summary>
    static bool ScheduleFatigueProbe(BattleDriver driver, int seat, int fatigueAmount, int hitAmount,
                                     out float gap, out float tailRel)
    {
        gap = float.NaN; tailRel = float.NaN;
        // ⚠️ 重试 3 次：`PlaySignals` 末尾那句 `AdvanceTimeline(0f)` 会把「起播时刻 ≤ 现在」的事件**当场播掉并从
        //    时间线移除**。批处理里 `_clock` 不动、`_timelineTailAt` 会一路往前 ⇒ 正常情况下第 1 次就留在线上；
        //    真被播掉时换一批新对象再来（新对象身份不同 ⇒ 永远不会和上一批串）。
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var fat = new BattleEvent { Kind = EvtKind.Fatigue, Player = seat, Slot = BoardSpec.WarlordSlot, Amount = fatigueAmount };
            var hit = new BattleEvent { Kind = EvtKind.Hit, Player = seat, Slot = BoardSpec.WarlordSlot, Amount = hitAmount };
            driver.Ctx.Signals.Add(fat);
            driver.Ctx.Signals.Add(hit);
            driver.RefreshAll();

            int iF = -1, iH = -1;
            for (int k = 0; k < driver.PendingSignalCountForTest; k++)
            {
                var e = driver.PendingSignalEventForTest(k);
                if (ReferenceEquals(e, fat)) iF = k;
                else if (ReferenceEquals(e, hit)) iH = k;
            }
            if (iF < 0 || iH < 0) continue;      // 被当场播掉了 ⇒ 再来一批
            float atF = driver.PendingSignalAtForTest(iF);
            float atH = driver.PendingSignalAtForTest(iH);
            gap = atH - atF;
            tailRel = driver.TimelineTailAtForTest - atF;
            return true;
        }
        return false;
    }

    // ==================================================================
    //  C-1 · 教程开局 HUD 滑入（`BattleHudShowNoHand`）
    // ==================================================================
    static void RunTutorialHudSlideIn(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- C-1 · 教程开局播 `BattleHudShowNoHand` ---");
        if (driver == null) { Check(false, "（前提）`driver` 不为 null"); return; }

        driver.BeginTutorial(0);
        Check(driver.Ctx != null, "（前提）`BeginTutorial(0)` 真的开了一局教程（取不到关卡数据会走失败支）");
        if (driver.Ctx == null) return;
        var ha = driver.HudAnimForTest;
        Check(driver.HudAnimBuiltForTest, "（前提）HUD 动画播放器 + rig 都建出来了（没有它这一步会走 `CanvasGroup` 兜底）");
        if (ha == null || !ha.Built) return;

        // 先把动画状态清掉，再让「督军两拍」跑到末拍（末拍补 HUD 滑入）
        ha.ResetAll();
        driver.TickTutorialViewForTest(1.0f);     // 一次喂足：`TutWarlordBeatSeconds = 0.6` ⇒ 一定越过末拍（⛔ 别喂 30 s，那会把后面的教程等待一起走完）
        Check(ha.CurrentClipName == BattleHudAnim.ShowNoHandClip,
              "★ C1 教程开局末拍播 **`BattleHudShowNoHand`**（`_TutorialStartSequence_d__592__MoveNext.c:283` 的 (1,1,1)，1 s）"
            + $"（实得 `{ha.CurrentClipName ?? "<没播>"}`）"
            + "｜🧨 把那句 `_hudAnim.Play(BattleHudAnim.ShowNoHandClip)` 删掉 ⇒ 本行红（教程局 HUD 静止出现）");
        Check(ha.CurrentClipName != BattleHudAnim.ShowClip,
              "★ C1' ⛔ 不是换牌收尾那条 `BattleHudShow`（两条 clip 不同：NoHand 那 4 条不含手牌区/敌方手牌区）");
    }

    // ==================================================================
    //  W2b 续单：F-14 · B-2 · B-3 · B-11
    // ==================================================================
    static void RunW2b(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- W2b · F-14 / B-2 / B-3 / B-11 ---");
        RunTutorialSfx(driver);
        RunSkillPanelCanUse(driver);
        RunHighlightRelationSource();
        RunSkipFromSettings(driver);
    }

    /// <summary>F-14：教程动作音效 —— 与 F-1 **同一个误读**（原版走 `PlayNextSoundInQueue`，闸与音量都是
    /// `FUN_180632c40` = `SoundFxVolume`；源无混音组 = Master）。</summary>
    static void RunTutorialSfx(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- F-14 · 教程动作音效（Master + 音效滑块 + 闸）---");
        if (driver == null) { Check(false, "（前提）`driver` 不为 null"); return; }
        var src = driver.EnsureTutorialSfxForTest();
        Check(src != null, "（前提）教程音效那颗 `AudioSource` 建出来了（没 `hudRoot` ⇒ 会返 null，下面两条空转）");
        Check(src != null && src.outputAudioMixerGroup == null,
              "★ F14-c 音源 **不挂混音组**（原版 `PlayNextSoundInQueue` 走 `BattleManager.audioSource` = `AudioSource_1595`，组全 0 ⇒ Master）"
            + (src != null && src.outputAudioMixerGroup != null ? "（实得挂上了 " + src.outputAudioMixerGroup.name + "）" : "")
            + "｜🧨 改回 `WarpforgeAudio.VoicesGroup` ⇒ 本行红");

        float fxWas = WarpforgeAudio.SoundFx;
        float listenerWas = AudioListener.volume;
        AudioClip clip = null;
        try
        {
            clip = AudioClip.Create("B120W2bProbe", 128, 1, 44100, false);
            Check(clip != null, "（前提）探测 clip 建出来了（null ⇒ `PlayTutorialClip` 会早退 ⇒ 下面两条空转）");
            // ---- F14-a：闸 ----
            WarpforgeAudio.SetSoundFx(0f);
            float before = driver.LastTutorialSfxVolumeForTest;
            driver.PlayTutorialClipForTest(clip);
            Check(driver.LastTutorialSfxVolumeForTest == before,
                  "★ F14-a `SoundFx = 0` ⇒ **不播**（原版 `PlayNextSoundInQueue.c` 的 `0 < FUN_180632c40(0)`）"
                + $"（音量读口 {before:F3} → {driver.LastTutorialSfxVolumeForTest:F3}）"
                + "｜🧨 删掉 `if (WarpforgeAudio.SoundFx <= 0f) return;` ⇒ 本行红");
            // ---- F14-b：音量 = 音效滑块 ----
            WarpforgeAudio.SetSoundFx(0.4f);
            AudioListener.volume = 0.8f;          // 故意另一个值
            driver.PlayTutorialClipForTest(clip);
            Near(driver.LastTutorialSfxVolumeForTest, 0.4f, 1e-4f,
                 "★ F14-b 音量实参 = **音效滑块 0.4**（⛔ 不是 `AudioListener.volume` 0.8）"
               + "｜🧨 改回 `AudioListener.volume` ⇒ 本行红");
        }
        finally
        {
            WarpforgeAudio.SetSoundFx(fxWas);
            AudioListener.volume = listenerWas;
        }
    }

    /// <summary>B-2：技能面板的「可用层」判据来自 `RuleCore.CanStartActiveAbility`（原版 `manager.CanUseActiveAbility`）。</summary>
    static void RunSkillPanelCanUse(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- B-2 · 技能面板可用层（canUse）---");
        if (driver == null || driver.Ctx == null) { Check(false, "（前提）`driver.Ctx` 在位"); return; }
        Check(driver.skillPanel != null, "（前提）`skillPanel` 在位（它是 `driver.skillPanel` 这个公开字段）");
        if (driver.skillPanel == null) return;

        int seat = driver.MySeat;
        UnitState cand = null; int slot = -1;
        var board = driver.Ctx.Players[seat].Board;
        for (int i = 0; i < board.Length; i++)
        {
            var u = board[i];
            if (u != null && u.Ability != null) { cand = u; slot = i; break; }
        }
        // 🔴 2026-11-03（`B124-W28`）：`Run()` 开头那句 `Begin(...)` 起手**棋盘是空的** ⇒ 上面那一圈
        //    永远找不到（这条前提一直假红，W26 换 C1 夹具后暴露）。这里按本仓通行的**白盒摆盘**补一个
        //    带主动能力的单位（夹具同 `B110_C09_Assert.B110C09Power`；正文走 `keywords` 那一路）。
        //    ⚠️ 摆完**收尾要清掉** —— 后面几节不许继承这格棋。
        CardDef fixture = null;
        if (cand == null)
        {
            fixture = new CardDef("B120W2AbilityFixture", "B120W2AbilityFixture", "unit", "", "common",
                                  "Ultramarines", 0, 0, 0, 0, new[] { "Ability: Damage 4 EnemyUnit" },
                                  subtype: "Infantry", hasActiveAbility: 1);
            fixture.AttachActiveAbilityCost(2, 0, 0, 0);
            Check(fixture.Ability != null, "（前提）夹具卡的 `Ability` 解析出来了（`Ability: Damage 4 EnemyUnit`）");
            for (int i = 0; i < board.Length; i++)
                if (BoardSpec.IsDeployable(i) && board[i] == null) { slot = i; break; }
            if (fixture.Ability != null && slot >= 0)
            {
                cand = new UnitState(driver.Ctx.NewInstance(fixture), false);
                cand.DeployedTurn = driver.Ctx.Turn;
                cand.PlayOrder = driver.Ctx.TakePlayOrder();
                board[slot] = cand;
            }
        }
        Check(cand != null, "（前提）我方棋盘上有**带主动能力**的单位（没有就验不出这一格）");
        if (cand == null) return;

        bool expect = RuleCore.CanStartActiveAbility(driver.Ctx, seat, slot) == RuleCodes.OK;
        try
        {
            driver.ShowSkillPanelForTest(cand, 1);
            var got = driver.SkillPanelCanUseForTest;
            Check(got.HasValue,
                  "★ B2-a 面板**真的收到了 `canUse`**（改前恒 `null` ⇒ 退回 `ShownTargets > 0` 那条旧判据）"
                + $"（实得 {(got.HasValue ? (got.Value ? "true" : "false") : "<null>")}）"
                + "｜🧨 把 `canUse: _canUse` 从 `Show(...)` 里去掉 ⇒ 本行红");
            Check(got.HasValue && got.Value == expect,
                  "★ B2-b `canUse` == `RuleCore.CanStartActiveAbility(ctx, seat, slot) == RuleCodes.OK`"
                + $"（实得 {(got.HasValue ? got.Value.ToString() : "<null>")} / 期望 {expect}；座 {seat} 格 {slot}）");
        }
        finally
        {
            if (driver.skillPanel != null) driver.skillPanel.Hide();
            if (fixture != null && slot >= 0 && board[slot] == cand) board[slot] = null;   // 收尾清掉夹具（别留给后面几节）
        }
    }

    /// <summary>B-3：教程高亮要**显式**收到 `rel` / `dx`，且与提示文案同源。
    /// <para>⚠️ 本节是**源码切片**断言（高亮对象在 `TutorialOverlay` 里、没有世界位置读口）——
    /// 位置算术本身由 `TutorialOverlay.HighlightPosForTest` 那一族钉着（`B120_W3`）。</para></summary>
    static void RunHighlightRelationSource()
    {
        Debug.Log(Prefix + "--- B-3 · 教程高亮 relation / dx（源码切片）---");
        string src = ReadAsset("CardPresentation/Battle/BattleDriver.cs");
        Check(!string.IsNullOrEmpty(src), "（前提）读得到 `BattleDriver.cs` 源码");
        if (string.IsNullOrEmpty(src)) return;

        int i = src.IndexOf("ov.SetHighlight(true, c, sz,", StringComparison.Ordinal);
        Check(i >= 0, "（前提）找得到高亮那个调用点");
        if (i < 0) return;
        int len = Math.Min(200, src.Length - i);
        string slice = src.Substring(i, len);
        Check(slice.Contains("(TutRelation)rel, dx)"),
              "★ B3-a 高亮**显式传** `(TutRelation)rel, dx`（原版高亮与提示文案过同一个 `GetTipPosition(..., positionRelation, …)`）"
            + "｜🧨 改回 `SetHighlight(true, c, sz)` ⇒ 本行红");
        Check(src.Contains("int rel = tp != null ? tp.positionRelation : 0; float dx = TutorialTipOffset(pref);"),
              "★ B3-b `rel` / `dx` 取自**同一组来源**（`tp.positionRelation` / `TutorialTipOffset(pref)`）");
        Check(src.Contains("TutorialTipOffset(pref), (TutRelation)rel,"),
              "★ B3-c（同源见证）提示文案 `SetTip(...)` 用的就是这两个表达式 —— 两条路径一份判据");
    }

    /// <summary>B-11：设置窗「跳过教程」那颗钮**不查** `minTimeBeforeSkip`（`TrySkip()` 那条闸仍在）。</summary>
    static void RunSkipFromSettings(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- B-11 · 设置窗「跳过教程」不查 1 s 闸 ---");
        if (driver == null) { Check(false, "（前提）`driver` 不为 null"); return; }
        driver.BeginTutorial(0);
        Check(driver.Ctx != null && driver.Ctx.MatchType == MatchType.Tutorial,
              "（前提）当前是**教程局**（非教程局 `ApplyTutorialSkip` 按原版只出声、不判负）");
        if (driver.Ctx == null || driver.Ctx.MatchType != MatchType.Tutorial) return;
        var ov = driver.TutorialView;
        Check(ov != null, "（前提）`TutorialOverlay` 建出来了");
        if (ov == null) return;

        ov.SetShownForTest(0f);
        Check(!driver.TutorialTrySkipForTest(),
              "★ B11-a `TrySkip()` 的 1 s 闸**仍然在**（B-11 只换设置窗这条链，⛔ 没把闸删掉）"
            + "｜🧨 把 `TrySkip` 的闸也删了 ⇒ 本行红（那会顺带改掉提示自己那条语义）");

        ov.SetShownForTest(0f);                 // 仍然"刚弹出"（低于 1 s）
        driver.SkipTutorialFromSettingsForTest();
        Check(driver.Ctx.IsOver,
              "★ B11-b 设置窗那颗钮**不看时间** ⇒ 0 s 也能跳过（原版 `SkipTutorialButtonOnClick` 全文两句、无闸）"
            + "｜🧨 改回 `if (ov != null && !ov.TrySkip()) return;` ⇒ 早退、对局不会结束 ⇒ 本行红");
    }

    // ==================================================================
    //  工具
    // ==================================================================
    /// <summary>从 `at`（指向调用名）截到**配对的那个 `)` 之后**。
    /// <para>为什么不用固定字数窗口：2026-11-03（`B124-W28`）实测 `_endPanel.Show(` 与本行之间隔了 250 字注释，
    /// 700 字窗口切不到 `skipSequence:` ⇒ 断言变成假红（切到的是半截）。括号配对与注释长短无关。</para></summary>
    static string CallSlice(string src, int at, int maxLen = 4000)
    {
        int open = src.IndexOf('(', at);
        if (open < 0) return "";
        int end = Math.Min(src.Length, open + maxLen);
        int depth = 0;
        for (int i = open; i < end; i++)
        {
            char c = src[i];
            if (c == '"')                        // 跳过字符串字面量（里面的括号不算）
            {
                i++;
                while (i < end && src[i] != '"') { if (src[i] == '\\') i++; i++; }
                continue;
            }
            if (c == '(') depth++;
            else if (c == ')') { depth--; if (depth == 0) return src.Substring(at, i - at + 1); }
        }
        return src.Substring(at, end - at);
    }

    static string ReadAsset(string assetRelativePath)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, assetRelativePath);
            return File.Exists(p) ? File.ReadAllText(p) : "";
        }
        catch (Exception e)
        {
            Debug.LogWarning(Prefix + "读源码失败 `" + assetRelativePath + "`：" + e.Message);
            return "";
        }
    }
}
