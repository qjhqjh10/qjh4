// B118W1_Assert.cs — 执行写手 `B118-W1`（2026-11-02）· 六件：T02 / T06 / T07 / T08 / R71 / T12
//
// ============================ 它盯的是什么（判据逐条带出处） ============================
//  A. `B117-T02` **42 处战斗按钮点击音**。原版口径 = `Battle Arena 1.unity` 里 **39 颗 `EverguildButton`**
//     逐颗现读（`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_*.json` 的
//     `useDefaultAudioClipIfNull` / `m_onClickSound`，2026-11-02 由本代理逐份解析）：
//       · 默认按钮音 **33 颗**（`useDefaultAudioClipIfNull = 1` 且 `m_onClickSound = {0,0}`）
//         = `WindowSounds.DefaultButtonCue`（cue `ButtonClick`；原版 `SoundManager.defaultButtonSound` pid 6176553109044381692）；
//       · 自带 cue **4 颗**：`BattleSettingsPanel/Generic Close Button`（MB 4038，pid 8084683923307806969
//         = `ButtonClick Back`）· `BattleSettingsPanel/Bottom buttons/Resign Button`（MB 4837，
//         pid 1387228150816026206 = `ButtonClick Negative`）· `BattleSettingsPanel/Auto Zoom Toggle`（MB 4356）
//         与 `BattleSettingsPanel/Volume Sliders/ChatToggle`（MB 4784）（两者同为 pid 8881786964274599484
//         = `Select Toggle`）；
//       · 静音 **2 颗**：`Card Display Window/Card Display/LowerSection/Voice Over Button`（MB 4852，
//         `useDefaultAudioClipIfNull = 0`）· `ChooseCardMenu/ButtonsGroup/Continue Button`（MB 4897，同为 0）。
//      ⇒ 断言钉的正是「谁该响、谁必须静」。⛔ 别整族一刀切（那会把原版两颗静音的做成响的）。
//  B. `B117-T06` **回放四颗钮的按下态换图**：四颗 `m_Transition = 2`(SpriteSwap)，`m_PressedSprite` 有值、
//     `m_HighlightedSprite / m_SelectedSprite / m_DisabledSprite` **全部 = Normal**
//     （MB 5177 Replay · 4109 Play · 4709 Pause · 5265 StepPlay）；四张 pressed 图名见 `PushedArtOf`。
//  C. `B117-T06` **`StepPlay` 的 `Time.timeScale = 15`**（`ClickNextStepReplay.c`，`DAT_1834b3164` = 15.0）
//     与 **终局哈希 `!same` 要发 Warn**（红线：不许静默失败）。
//  D. `B117-T07` **换牌收尾逐张节奏**：`PlayerHand._FinishMulligan_d__52__MoveNext.c` —— 每换一张等
//     `ReturnToDeckAnim(timeToDraw)` 演完（`timeToDraw` = 0.15 = `VarsGlobal.timeToDrawEnemyCard` = `CardFeel.DealDurationFoe`），
//     全部换完再等 **`TimeToDrawCard(isPlayer) × 2.0` = 0.6 s**（同文件 `:145-149`；`read_literal 0x1834b2bbc` 4B = 2.0
//     —— 🔴 2026-11-02 `B118-W17` 订正：原来记成「×0.5」，那是 `_AddCardsToMulligan` 那个 `0x1834b2bb4`）。
//  E. `B117-T08` **疲劳横幅的 0.5 s 节拍**：`BattleManager._ResolveFatigue_d__587__MoveNext.c` 末句
//     `yield return new WaitForSeconds(0.5f)`（字面量 `read_literal 0x1834b2bb4 = 0.5`）。
//  F. `B117-R71` **出牌记录卡名**：起始卡 `StarterCards.Unit("Falcon", …)`（`RuleEngine/Data/StarterCards.cs:56`）
//     与卡池 `ASH27` 共用英文名 `Falcon`；日志必须按**那一方阵营的卡池**取名（`PoolFor`），
//     否则玩家手里是 `猎鹰`、日志印成 `猎鹰战车`。
//  G. `B117-T12` **手牌区 Small Screen UI ×1.365**：`PlayerHand__Initialize.c`
//     `transform.localScale = 原值 × fVar5`，`fVar5` 在 13/13 场两支序列化值都是 1.365。
//
// ============================ 灭自证（🧨 结构上不可能同时满足） ============================
//  🧨 A：`Voice Over Button`（CardDisplayWindow）与 `Continue Button`（CardChoicePanel 的透明命中面）
//     **必须不带 cue**，而同一批里 `HideChooseButton`（换牌面板那颗眼睛）**必须带** ⇒
//     「整族一刀切」与「一个 cue 都不加」各红一条，且不可能同时绿。
//  🧨 B：按下图**四张名字互不相同**且**都能取到**（`restart/play/pause/next`）——
//     「挂上了换图件」可以靠同一张图满足，这条不行。
//  🧨 D：`timeToDraw` 取的是 `CardFeel.DealDurationFoe`（0.15）**且**尾拍乘 0.5 ——
//     把两处都写死成同一个数、或漏掉 ×0.5，两条里必有一条红。
//  🧨 G：`1320` 那条断 `1.365` 不等于 `1`、且 `ApplySmallScreenHandScale` 里**四个字段都乘 m**（少乘一个就红）。
//
// ============================ 怎么接 ============================
//  挂法（由调度台决定，⛔ 本文件不改宿主）：
//      B118W1_Assert.Run(); _sink.Pass += B118W1_Assert.Pass; _sink.Fail += B118W1_Assert.Fail;
//  判绿红：`grep "^B118W1 "` 的**合计行**；逐条按**行首** `B118W1 ✓` / `B118W1 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CardPresentation;

/// <summary>执行写手 `B118-W1`：T02 点击音 / T06 回放三缺 / T07 换牌逐张 / T08 疲劳节拍 / R71 卡名 / T12 小手牌档。</summary>
public static class B118W1_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string P = "B118W1";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + " ✓ " + msg); }
        else { _fail++; Debug.LogError(P + " ✗ " + msg); }
    }

    static void Group(string name, Action body)
    {
        try { body(); }
        catch (Exception e)
        {
            Check(false, name + "：**这一段抛异常**（后面的断言没跑到，⛔ 别读成通过）—— "
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Group("A·T02 点击音接线", A_ClickCue);
        Group("B·T06 回放四颗按下态", B_ReplayPressed);
        Group("C·T06 StepPlay 的 timeScale / 终局 Warn", C_ReplayStepAndHash);
        Group("D·T07 换牌逐张节奏", D_MulliganSettle);
        Group("E·T08 疲劳横幅 0.5s 节拍", E_FatigueHold);
        Group("F·R71 按阵营卡池取卡名", F_CardNameByFaction);
        Group("G·T12 手牌区 Small Screen ×1.365", G_HandScale);
        Group("H·B118-J9 手牌交互三件（抽牌让位先后 / 缓动 / 敌方重排）", H_HandFeel);
        Debug.Log(P + " 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ---- 读源码（**只留活代码行**：`//` 开头的整行注释不算 —— 留痕注释里提到名字是故意的）----
    static List<string> LiveLines(string relPath)
    {
        var path = Path.Combine(Application.dataPath, relPath);
        var list = new List<string>();
        if (!File.Exists(path)) return list;
        foreach (var l0 in File.ReadAllLines(path))
        {
            string l = l0.TrimStart();
            if (l.StartsWith("//")) continue;
            list.Add(l0);
        }
        return list;
    }

    static int CountOf(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }

    static string FindLine(List<string> lines, string must)
    {
        for (int i = 0; i < lines.Count; i++) if (lines[i].Contains(must)) return lines[i];
        return null;
    }

    /// <summary>从 `must` 那一行起**往下拼 `span` 行**（跨行调用用：`AttachClick(...)` 的 cue 常落在第 2~3 行）。</summary>
    static string FindSpan(List<string> lines, string must, int span)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (!lines[i].Contains(must)) continue;
            var sb = new System.Text.StringBuilder();
            for (int k = i; k < lines.Count && k < i + span; k++) sb.Append(lines[k]).Append('\n');
            return sb.ToString();
        }
        return null;
    }

    /// <summary>找**同时**含 `a` 与 `b` 的第一行（同一行上的复合判据用）。</summary>
    static string FindLineBoth(List<string> lines, string a, string b)
    {
        for (int i = 0; i < lines.Count; i++)
            if (lines[i].Contains(a) && lines[i].Contains(b)) return lines[i];
        return null;
    }

    // ================================================================= A · T02
    static void A_ClickCue()
    {
        const string DRV = "CardPresentation/Battle/BattleDriver.cs";
        const string SET = "CardPresentation/Battle/SettingsPanel.cs";
        const string CHO = "CardPresentation/Battle/CardChoicePanel.cs";
        const string CHT = "CardPresentation/Battle/ChatPopupPanel.cs";
        const string CDW = "CardPresentation/Battle/CardDisplayWindow.cs";

        var drv = LiveLines(DRV); var set = LiveLines(SET); var cho = LiveLines(CHO);
        var cht = LiveLines(CHT); var cdw = LiveLines(CDW);

        Check(drv.Count > 0 && set.Count > 0 && cho.Count > 0 && cht.Count > 0 && cdw.Count > 0,
              "A0（前提）五份源码都读得到（读不到时本条之后全是空判）");
        if (drv.Count == 0 || set.Count == 0 || cho.Count == 0 || cht.Count == 0 || cdw.Count == 0) return;

        // A1 HUD 七颗（结束回合 / 设置 / 墓地 / 聊天 / 相机复位 / 进攻卡 / 联盟）—— 原版全是默认按钮音
        var drvAttach = drv.FindAll(x => x.Contains("BattleHudButton.Attach("));
        int withDefault = 0, without = 0;
        foreach (var l in drvAttach)
        {
            if (l.Contains("WindowSounds.DefaultButtonCue")) withDefault++; else without++;
        }
        Check(drvAttach.Count == 7 && withDefault == 7 && without == 0,
              "A1 `BattleDriver.cs` 七颗 HUD 钮**全部**带 `WindowSounds.DefaultButtonCue`"
            + "（原版 `TurnBtn`/`SettingsBtn`/`ShowCemeteryBtn`/`ChatButton`/`CenterCameraButton`/`OffensiveButton`/"
            + "联盟头像都是 `useDefaultAudioClipIfNull=1` 且无自带 cue；实得 " + withDefault + "/" + drvAttach.Count + "）");

        // A2 设置面板五处（自带 cue 的三颗 + 默认的跳过教程 + 两条 label 命中面）
        Check((FindSpan(set, "_btnClose = AttachClick(", 3) ?? "").Contains("WindowSounds.DefaultCloseCue"),
              "A2a 设置面板 `Generic Close Button` ⇒ `ButtonClick Back`（原版 MB 4038 的 `m_onClickSound`；"
            + "⚠️ 调用跨 3 行，cue 落在最后一行）");
        Check((FindLine(set, "_btnResign = AttachClick(") ?? "").Contains("WindowSounds.NegativeCue"),
              "A2b 设置面板 `Resign Button` ⇒ `ButtonClick Negative`（原版 MB 4837）");
        Check((FindLine(set, "_btnAutoZoom = AttachClick(") ?? "").Contains("WindowSounds.SelectToggleCue"),
              "A2c 设置面板 `Auto Zoom Toggle` ⇒ `Select Toggle`（原版 MB 4356）");
        Check((FindLine(set, "_btnMuteChat = AttachClick(") ?? "").Contains("WindowSounds.SelectToggleCue"),
              "A2d 设置面板 `ChatToggle` ⇒ `Select Toggle`（原版 MB 4784）");
        Check((FindLine(set, "_btnSkip = AttachClick(") ?? "").Contains("WindowSounds.DefaultButtonCue"),
              "A2e 设置面板 `SkipTutorial Button` ⇒ 默认按钮音（原版 MB 4945 无自带 cue）");
        Check((FindLine(set, "AttachClick(_azLabelHit,") ?? "").Contains("WindowSounds.SelectToggleCue")
           && (FindLine(set, "AttachClick(_muteLabelHit,") ?? "").Contains("WindowSounds.SelectToggleCue"),
              "A2f 两条 label 命中面跟着同一颗原版钮走（`Select Toggle`）");

        // A3 选卡面板：眼睛响、其余三件静（🧨 见文件头 A）
        string eye = FindLine(cho, "_btnEye = AttachChoiceButton(");
        string done = FindLine(cho, "_btnDoneParent = AttachChoiceButton(");
        string bar = FindLine(cho, "_btnBar = AttachChoiceButton(");
        string play = FindLine(cho, "_btnPlay = AttachChoiceButton(");
        Check(eye != null && eye.Contains("WindowSounds.DefaultButtonCue"),
              "A3a 选卡面板 `HideChooseButton`（原版 MB 4387 `useDefaultAudioClipIfNull=1`）⇒ 默认按钮音");
        bool silent3 = done != null && bar != null && play != null
                    && !done.Contains("WindowSounds.") && !bar.Contains("WindowSounds.") && !play.Contains("WindowSounds.");
        Check(silent3,
              "A3b 🧨 `Continue Button`（原版 MB 4897 `useDefaultAudioClipIfNull=0`）/ `BG` / `ContinueButton`"
            + " **三件都不带 cue**（原版点那一整块是**静的**）—— 与 A3a 同时成立 ⇒ 一刀切必红");

        // A4 聊天弹窗六颗 ⇒ 默认按钮音
        var chatAttach = FindLine(cht, "BattleHudButton.Attach(hit,");
        Check(chatAttach != null && chatAttach.Contains("WindowSounds.DefaultButtonCue"),
              "A4 聊天弹窗六颗 `ChatButton (0..5)` ⇒ 默认按钮音（原版六颗都是 `useDefaultAudioClipIfNull=1` 无自带 cue）");

        // A5 详情窗两处**必须静**（🧨 见文件头 A）
        string bgClose = FindLine(cdw, "BattleHudButton.Attach(_backdrop,");
        string voice = FindLine(cdw, "BattleHudButton.Attach(_voiceBtn,");
        Check(bgClose != null && !bgClose.Contains("WindowSounds."),
              "A5a 详情窗背景关闭面 `cdw_background_close` **不带 cue**（原版它不是按钮，没有按钮音）");
        Check(voice != null && !voice.Contains("WindowSounds."),
              "A5b 🧨 `Voice Over Button`（原版 MB 4852 `useDefaultAudioClipIfNull=0`）**不带 cue**"
            + " —— 39 颗里另一颗静音档");

        // A6' 换牌面板两颗（手搓命中路 · `B118-W1` 第二轮补）
        var mul = LiveLines("CardPresentation/Battle/MulliganPanel.cs");
        Check(mul.Count > 0, "A7a（前提）读得到 `CardPresentation/Battle/MulliganPanel.cs`");
        if (mul.Count > 0)
        {
            string msrc = string.Join("\n", mul.ToArray());
            Check(msrc.Contains("PlayButtonCue(\"HideMulliganButton\")")
               && msrc.Contains("PlayButtonCue(\"MulliganContinueButton\")"),
                  "A7 换牌面板两颗原版钮（`HideMulliganButton` MB 4464 · `MulliganContinueButton/Button` MB 4025，"
                + "都 `useDefaultAudioClipIfNull=1`）各播一次点击音");
            Check(msrc.Contains("WindowSounds.PlayFx(WindowSounds.DefaultButtonCue"),
                  "A8 🧨 那一声走的是 `WindowSounds.DefaultButtonCue`（= 原版 `SoundManager.defaultButtonSound`）");
        }

        // A6'' 多卡展示窗两颗（一对反相 · 🧨）
        var mcd = LiveLines("CardPresentation/Battle/MultiCardDisplay.cs");
        Check(mcd.Count > 0, "A9a（前提）读得到 `CardPresentation/Battle/MultiCardDisplay.cs`");
        if (mcd.Count > 0)
        {
            string mcdBar = FindLine(mcd, "BattleHudButton.Attach(_bar,");
            string mcdBg = FindLine(mcd, "BattleHudButton.Attach(_bgClose,");
            Check(mcdBar != null && mcdBar.Contains("WindowSounds.DefaultButtonCue"),
                  "A9 `BattleContinueButton/Button`（原版 MB 5115，`useDefaultAudioClipIfNull=1`）⇒ 默认按钮音");
            Check(mcdBg != null && !mcdBg.Contains("WindowSounds."),
                  "A10 🧨 `Menu Dark Background`（压暗层，原版不是按钮）⇒ **不带 cue** —— 与 A9 同时成立 ⇒ 一刀切必红");
        }

        // A6 cue 名（原版 pid → cue 名的映射，判据 = `WindowSounds.cs` 文件头那张表）
        Check(WindowSounds.DefaultButtonCue == "ButtonClick"
           && WindowSounds.DefaultCloseCue == "ButtonClick Back"
           && WindowSounds.SelectToggleCue == "Select Toggle"
           && WindowSounds.NegativeCue == "ButtonClick Negative",
              "A6 四条 cue 名与原版 pid 表逐字对上（`ButtonClick` / `ButtonClick Back` / `Select Toggle` / `ButtonClick Negative`）");
    }

    // ================================================================= B · T06 按下态
    static void B_ReplayPressed()
    {
        const string RB = "CardPresentation/Battle/ReplayBar.cs";
        var rb = LiveLines(RB);
        Check(rb.Count > 0, "B0（前提）读得到 `" + RB + "`");
        if (rb.Count == 0) return;
        string src = string.Join("\n", rb.ToArray());

        string[] pushed = { "40K_replay_bt_restart_pushed", "40K_replay_bt_play_pushed",
                            "40K_replay_bt_pause_pushed", "40K_replay_bt_next_ushed" };
        var uniq = new HashSet<string>();
        int present = 0;
        for (int i = 0; i < pushed.Length; i++)
            if (src.Contains("\"" + pushed[i] + "\"")) { present++; uniq.Add(pushed[i]); }
        Check(present == 4 && uniq.Count == 4,
              "B1 🧨 四张 pressed 图名都在且**互不相同**（实得 " + present + " 处 / " + uniq.Count + " 个不同；"
            + "⚠️ `next` 那张原版名少一个 p = `40K_replay_bt_next_ushed`，照抄别改）");

        int have = 0;
        for (int i = 0; i < pushed.Length; i++)
        {
            var tex = CardArt.Ui(pushed[i]);
            if (tex != null) have++;
        }
        Check(have == 4, "B2 四张 pressed 图**都在 `Resources/Art/ui/` 里取得到**（实得 " + have + "/4）");

        // 原版 `m_HighlightedSprite == Normal` ⇒ 悬停不换图：第二参必须与第三参同一个 `normal`
        Check(src.Contains("BattleButtonFx.Swap(piece, \"ReplayBar_\" + which + \"_fx\", normal, normal, pressed)"),
              "B3 🧨 `Swap(piece, name, normal, normal, pressed)` —— 悬停图 = Normal（原版四颗 `m_HighlightedSprite` 全等于 Normal）");

        Check(src.Contains("EventTriggerType.PointerDown") && src.Contains("EventTriggerType.PointerUp")
           && src.Contains("EventTriggerType.PointerExit"),
              "B4 按下 / 松开 / 指针离开三条事件都接上（离开回 Normal，同 uGUI `Selectable.OnPointerExit`）");
        Check(src.Contains("PressFxForTest") && src.Contains("PressForTest"),
              "B5 自检读口 `PressFxForTest` / `PressForTest` 都在（否则这一档只能靠肉眼看）");
    }

    // ================================================================= C · T06 timeScale / 终局 Warn
    static void C_ReplayStepAndHash()
    {
        const string DRV = "CardPresentation/Battle/BattleDriver.cs";
        var drv = LiveLines(DRV);
        Check(drv.Count > 0, "C0（前提）读得到 `" + DRV + "`");
        if (drv.Count == 0) return;

        string step = FindSpan(drv, "SetReplaySpeed(ReplayFastForwardScale);   // 单步", 2)
                   ?? FindSpan(drv, "SetReplaySpeed(ReplayFastForwardScale)", 2);
        Check(step != null && step.Contains("StepReplaySignal"),
              "C1 🆕 `StepPlay` 支在同一段里既设 `Time.timeScale = 15` 又推一条最前的事件"
            + "（原版 `ClickNextStepReplay.c` 第一句 `Time.timeScale = 15`）");
        Check(FindLine(drv, "const float ReplayFastForwardScale") != null
           && (FindLine(drv, "const float ReplayFastForwardScale") ?? "").Contains("15f"),
              "C2 那个倍率常量 = **15**（原版 `DAT_1834b3164` 实读 15.0）");

        var playLine = FindLine(drv, "case ReplayBar.Btn.Play:");
        var pauseLine = FindLine(drv, "case ReplayBar.Btn.Pause:");
        var replayLine = FindLine(drv, "case ReplayBar.Btn.Replay:");
        Check(playLine != null && playLine.Contains("ResetReplaySpeed")
           && pauseLine != null && pauseLine.Contains("ResetReplaySpeed")
           && replayLine != null && replayLine.Contains("ResetReplaySpeed"),
              "C3 离开 15× 档的三条路（Play / Pause / Restart）都显式复位（原版靠重载场景复位）");

        Check(FindLine(drv, "if (!same) Debug.LogWarning(") != null,
              "C4 🧨 终局指纹 `!same` 走 **`Debug.LogWarning`**（老档没有 `traceFull` 时这是唯一那道闸 —— 红线：不许静默失败）");
    }

    // ================================================================= D · T07 换牌逐张
    static void D_MulliganSettle()
    {
        const string DRV = "CardPresentation/Battle/BattleDriver.cs";
        var drv = LiveLines(DRV);
        Check(drv.Count > 0, "D0（前提）读得到 `" + DRV + "`");
        if (drv.Count == 0) return;
        string src = string.Join("\n", drv.ToArray());

        Check(src.Contains("BeginMulliganSettle(settleViews)"),
              "D1 `OnMulliganDone` 里起「逐张回库」那条链");
        string tid = FindLine(drv, "const float MulliganSettleTimeToDraw");
        Check(tid != null && tid.Contains("CardFeel.DealDurationFoe"),
              "D2 🧨 逐张时长 = **`CardFeel.DealDurationFoe`**（原版 `timeToDrawEnemyCard`，⛔ 别在别处再写死一个数）");
        Check(Mathf.Abs(CardFeel.DealDurationFoe - 0.15f) < 1e-6f,
              "D3 🧨 `CardFeel.DealDurationFoe` = **0.15**（原版 `VarsGlobal.timeToDrawEnemyCard`；实得 "
            + CardFeel.DealDurationFoe.ToString("0.####") + "）");
        // 🔴 2026-11-02（`B118-W17`）就地订正：原来这条断的是**源文本里写着 `MulliganSettleTimeToDraw * 0.5f`**
        //    —— 拿我们自己的常量自证（那条乘数是错的：原版尾拍是 `TimeToDrawCard(isPlayer) × 2.0`，不是 ×0.5）。
        //    现在改成断**数值 0.6 s**，期望由原版两个量算：`timeToDrawPlayerCard 0.3`（= `CardFeel.DealDuration`）× `2.0`。
        Check(Mathf.Abs(BattleDriver.MulliganSettleTailForTest - 0.6f) < 1e-5f,
              "D4 🧨 换牌收尾尾拍 = **0.6 s**（原版 `PlayerHand._FinishMulligan_d__52__MoveNext.c:145-149`："
            + "`WaitForSeconds(TimeToDrawCard(我方 0.3) × DAT_1834b2bbc)`，`read_literal` 4B 实读 = **2.0**；实得 "
            + BattleDriver.MulliganSettleTailForTest.ToString("0.####") + " s）"
            + "｜🧨 改回 ×0.5（= 0.075 s）⇒ 红");
        Check(src.Contains("MulliganSettleTailSeconds") && !src.Contains("MulliganSettleTimeToDraw * 0.5f"),
              "D4b 🧨 泵**真的**用了那个尾拍常量（常量改了、泵还乘 0.5 = 静默无效 ⇒ 这条红）");
        Check(src.Contains("if (!Application.isPlaying) return false;"),
              "D5 只在真机走这一段（批处理没有帧循环 ⇒ 逐张会永远等不到下一拍）");
        Check(src.Contains("interaction.enabled = true;") && src.Contains("void FinishMulliganSettle("),
              "D6 输入在**收尾**那一刻才放开（原版演出期间靠压暗层挡输入 ⇒ `interaction.enabled` 期间必须保持 false）");
        Check(src.Contains("if (_mulliganSettleViews != null) return;"),
              "D7 演出期间 `SyncHand` 直接返回（否则引擎那一批新牌会**一次性**重建，把逐张飞回吞掉）");
    }

    // ================================================================= E · T08 疲劳 0.5s
    static void E_FatigueHold()
    {
        const string DRV = "CardPresentation/Battle/BattleDriver.cs";
        var drv = LiveLines(DRV);
        Check(drv.Count > 0, "E0（前提）读得到 `" + DRV + "`");
        if (drv.Count == 0) return;
        string src = string.Join("\n", drv.ToArray());

        string c = FindLine(drv, "const float FatigueBannerHold");
        Check(c != null && c.Contains("0.5f"),
              "E1 🧨 节拍常量 = **0.5**（原版 `_ResolveFatigue` 末句 `WaitForSeconds(0.5f)`，`read_literal 0x1834b2bb4`）");
        Check(src.Contains("_fatigueHoldEvent = e;"),
              "E2 弹横幅那一条 `Hit` 被记下来（`NoteFatigueBanners`）");
        string loop = FindLine(drv, "ReferenceEquals(e, _fatigueHoldEvent)");
        Check(loop != null && loop.Contains("t += beatGap") && loop.Contains("FatigueBannerHold"),
              "E3 🧨 那 0.5 s 加在**这一拍之后**（`t += beatGap` 之后再 `+= FatigueBannerHold`）"
            + " —— 原版次序是「先伤害、后等 0.5」，不是「先等再伤害」");
        Check(src.Contains("_fatigueHoldEvent = null;"),
              "E4 批次之间把记账清掉（合成夹具喂的旧事件不会给下一批凭空加 0.5 s）");
    }

    // ================================================================= F · R71 卡名
    static void F_CardNameByFaction()
    {
        const string DRV = "CardPresentation/Battle/BattleDriver.cs";
        var drv = LiveLines(DRV);
        Check(drv.Count > 0, "F0（前提）读得到 `" + DRV + "`");
        if (drv.Count == 0) return;
        string src = string.Join("\n", drv.ToArray());

        Check(FindLine(drv, "string card = Zh(e.CardId, e.Player);") != null,
              "F1 日志行按**那一方**取名（`Zh(e.CardId, e.Player)`）");
        Check(src.Contains("string Zh(string name, int player)"),
              "F2 两参重载在（`Zh(string, int)`）");
        Check(src.Contains("var pool = PoolFor(_pool, fac);"),
              "F3 🧨 走**阵营卡池的唯一解析口** `PoolFor`（Ember/Tide 两套自造阵营 ⇒ `StarterCards.Of`，其余 ⇒ `_pool`）");
        Check(src.Contains("ps.Warlord.Card.Faction"),
              "F4 那一方阵营取**督军那件的 `Faction`**（`PlayerState.Warlord.Card.Faction`）");
        Check(FindLine(drv, "string card = Zh(e.CardId);") == null,
              "F5 🧨 旧的一参写法**已不在**日志行上（改回一参 ⇒ 起始卡 `Falcon` 又会被印成卡池的 `猎鹰战车`）");
        Check(src.Contains("if (ReferenceEquals(pool, _pool)) return Zh(name);"),
              "F6 原版阵营 ⇒ 逐字节退回旧口径（⛔ 不改变任何原版阵营的显示名）");
    }

    // ================================================================= G · T12 手牌 ×1.365
    static void G_HandScale()
    {
        const string DRV = "CardPresentation/Battle/BattleDriver.cs";
        var drv = LiveLines(DRV);
        Check(drv.Count > 0, "G0（前提）读得到 `" + DRV + "`");
        if (drv.Count == 0) return;
        string src = string.Join("\n", drv.ToArray());

        Check(Mathf.Abs(CombatAutoZoom.PlayerHandSmallScreenSizeMultiplier - 1.365f) < 1e-6f,
              "G1 🧨 倍数 = **1.365**（原版 13/13 场 `PlayerHand` 的 `smallScreenSizeMultiplier` 逐场现读；"
            + "实得 " + CombatAutoZoom.PlayerHandSmallScreenSizeMultiplier.ToString("0.####") + "）");
        Check(Mathf.Abs(CombatAutoZoom.PlayerHandSmallScreenSizeMultiplier - 1f) > 0.3f,
              "G2 🧨 「恒 1.0」与「恒 1.365」都不可能同时过 G1 + G2");
        Check(src.Contains("EnsureAudioListener(); ApplySmallScreenHandScale();"),
              "G3 每局 `Begin` 都落一次（`PlayerHand.Initialize` 也是开局那一下）");
        string body = src.Substring(Math.Max(0, src.IndexOf("void ApplySmallScreenHandScale()", StringComparison.Ordinal)));
        body = body.Substring(0, Math.Min(body.Length, 1400));
        int scaled = CountOf(body, "* m;");
        Check(scaled >= 4,
              "G4 🧨 四个字段都乘了同一个 m（卡尺度 / 基线高度 / 悬停抬起量 / 小屏档横向跨度上限；实得 "
            + scaled + " 处 `* m;`）");
        Check(body.Contains("if (_handScaleBaseCard < 0f)"),
              "G5 基值只抓一次 ⇒ 连打多局不会越乘越大（幂等）");
    }

    // ============================================================ H · `B118-J9` 手牌交互三件
    //  判据（执行写手 `B118-W17`，全文 → `判据/B118_J9_手牌交互手感审计.md` §4·3 / §4·5 / §4·7）：
    //   · **抽牌 ↔ 让位先后**：`PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c` —— state0 `DrawCard` →
    //     `WaitForSeconds(timeToDraw + 0.1)` → state1 才 `Add` + `SetupCardInHand` + `MoveCardsInHandToPosition`
    //     ⇒ 先飞牌、后让位（原来同帧就让位 = 提前约 0.4 s）。
    //   · **抽卡缓动**：`CardScript__DrawCard.c:73,86` 两条 `SetEase(0xc)`，DOTween 枚举 `0xc = 12 = OutQuart`。
    //   · **敌方手牌重排**：原版同一趟 `MoveCardsInHandToPosition` 0.2 s（无 `SetEase` ⇒ 默认 `OutQuad`）；
    //     我们原来在 `ReapplyFoeHandPoses` 里直写 `localPosition` ⇒ 对手每次抽牌整排瞬移。
    static void H_HandFeel()
    {
        const string DRV = "CardPresentation/Battle/BattleDriver.cs";
        const string FEEL = "CardPresentation/Core/CardFeel.cs";
        var drv = LiveLines(DRV);
        var feel = LiveLines(FEEL);
        Check(drv.Count > 0 && feel.Count > 0, "H0（前提）读得到 BattleDriver.cs / CardFeel.cs");
        if (drv.Count == 0 || feel.Count == 0) return;
        string dsrc = string.Join("\n", drv.ToArray());
        string fsrc = string.Join("\n", feel.ToArray());

        // H1 抽牌后让位的时刻 = 最后一次抽牌时长 + 0.1（原版两个量：0.3 + 0.1 = 0.4）。
        //    期望由**原版常量**算 ⇒ 不是自证；🧨 不延后（0 / 0.3）或延后过头都不是 0.4。
        Check(Mathf.Abs(CardFeel.DealDuration - 0.3f) < 1e-6f,
              "H1a（前提）`CardFeel.DealDuration` = 0.3（原版 `timeToDrawPlayerCard`；实得 "
            + CardFeel.DealDuration.ToString("0.###") + "）");
        Check(Mathf.Abs(BattleDriver.HandRelayoutDelay(true, 1) - 0.4f) < 1e-5f,
              "H1 🧨 抽 1 张时**手牌让位**排在抽牌起飞后 **0.4 s**（原版 `timeToDraw 0.3 + 0.1`；实得 "
            + BattleDriver.HandRelayoutDelay(true, 1).ToString("0.####") + " s）"
            + "｜🧨 同帧让位（0 / 0.3）⇒ 红");

        // H2 那一趟 `SetCards` 真的传了 `relayout:false`（否则 H1 的时刻只是个没人用的数）。
        Check(FindLineBoth(drv, "interaction.SetCards(", "!_deferHandRelayout") != null,
              "H2 🧨 抽牌那一趟 `SetCards` 用的是 `relayout: !Ctx.MulliganOpen && !_deferHandRelayout`"
            + "（🧨 退回 `relayout: !Ctx.MulliganOpen` ⇒ 整排当场让位 ⇒ 红）");
        Check(CountOf(dsrc, "ScheduleDeferredHandRelayout();") >= 1,
              "H3 🧨 延后的那一趟**真的被排上了**（`ScheduleDeferredHandRelayout();` 有调用点；"
            + "只有定义没有调用 = 手牌永远不让位 = 静默缺陷）");
        string dh = FindSpan(drv, "bool DeferHandRelayout()", 4) ?? "";
        Check(dh.Contains("_dealt.Count > 0") && dh.Contains("!Ctx.MulliganOpen") && dh.Contains("animateFeel"),
              "H4 延后只在「有画面演出 + 不在换牌期 + 这一轮真有新牌」时成立（批处理 ⇒ 不延后，自检仍拿当场坐标）");

        // H5/H6 抽卡缓动 = OutQuart（原版 `CardScript__DrawCard.c:73,86` 的 `SetEase(0xc)`；DOTween 枚举 0xc = 12）。
        //    逐条点名（⛔ 别只数「有几处 OutQuart」—— 本文件别处还有 4 处，数总数挡不住回退）。
        int qMovePlayer = CountOf(fsrc, "DOMove(to, DealDuration).SetEase(Ease.OutQuart)");   // DealInPose + DealIn
        int qScalePlayer = CountOf(fsrc, "DealDuration, Ease.OutQuart");                      // ScaleTo 两处
        int qMoveFoe = CountOf(fsrc, "DOMove(to, d).SetEase(Ease.OutQuart)");                 // DealInQuad
        int qScaleFoe = CountOf(fsrc, "DOScale(Vector3.one, d).SetEase(Ease.OutQuart)");      // DealInQuad
        Check(qMovePlayer == 2 && qScalePlayer == 2,
              "H5 🧨 **我方**抽卡两条补间都是 `OutQuart`（`DealInPose` 的 DOMove + `ScaleTo`；"
            + $"实得 DOMove {qMovePlayer} / 缩放 {qScalePlayer}，各应为 2）｜🧨 任一条退回 `OutCubic` ⇒ 红");
        Check(qMoveFoe == 1 && qScaleFoe == 1,
              "H6 🧨 **敌方**抽卡两条补间也是 `OutQuart`（`DealInQuad` 的 DOMove + DOScale；"
            + $"实得 {qMoveFoe} / {qScaleFoe}，各应为 1）｜🧨 只改我方那条 ⇒ 红");

        // H7 抽卡 α 淡入是**我们自加**的（原版 `DrawCard` 全文没有 fade）⇒ 必须带 `🔶 自己设计` 标记。
        Check(fsrc.Contains("🔶 自己设计（非原版）：抽牌 alpha 淡入")
              && fsrc.Contains("🔶 自己设计（非原版）：敌方抽牌的卡背 alpha 淡入"),
              "H7 🧨 抽卡那两条 α 淡入都打了 `🔶 自己设计（非原版）` 标记（铁律 18：原版没有的东西必须能 grep 出来）");

        // H8/H9 敌方手牌重排走 0.2 s 补间（原版同一趟 `MoveCardsInHandToPosition`）。
        Check(FindLine(drv, "void ReapplyFoeHandPoses(bool animate = false") != null,
              "H8 🧨 `ReapplyFoeHandPoses` 有 `animate` 闸（HUD 滑出那条逐帧重摆仍走直写；⛔ 两条路合并会让换牌那 1 s 的整排抖动）");
        string foeBody = FindSpan(drv, "void ReapplyFoeHandPoses(bool animate = false", 26) ?? "";
        Check(FindLine(drv, "ReapplyFoeHandPoses(animate: true)") != null
              && foeBody.Contains("CardTween.RelayoutDuration") && foeBody.Contains("Ease.OutQuad")
              && foeBody.Contains("_foeDealt.Contains(q)"),
              "H9 🧨 敌方那一排**真的补间**了：0.2 s（`CardTween.RelayoutDuration`）+ `OutQuad`，"
            + "且新飞进来那几张走直写（`_foeDealt.Contains(q)` 那道闸 —— 否则会和 `DealInQuad` 抢同一个 transform）");
    }
}
