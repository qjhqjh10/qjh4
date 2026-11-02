// B117_WBD_Assert.cs — 执行代理 `B117-WBD`（第一百一十七会话）落在 `Battle/BattleDriver.cs` 上的那几条账的断言
//   · `B117-31` 两处过期注释（`SkirmishWarlordLifeChange = -10` ⇒ 现读 0）
//   · `B117-38` 阻塞窗口 = 整批事件 ⇒ 现改为「这一批里有没有真动作」
//   · `B117-48` 点出牌记录里的卡名 ⇒ 开**大卡信息窗**（不是小预览卡）
//   · `B117-49` 点行命中区 ⇒ 只认**行内 TMP 链接**（带一条可测的降级路径）
//   · `B117-30` 详情窗督军血量：**驱动那一跳**（喂 `Ctx.Vars.warlordLifeChange`）—— 窗那一半由 `B117F12_Assert` 覆盖
//   · `B117-40` 回放「暂停 = 跳过动画」（`ReplaySkipAnims` 的 BattleDriver 那一半）
//   · 跨文件连带：AI 三条 `Handoff.Apply` 里的登场 clip 改成「从 0.65 偏移起播」
//
// ============================ 宿主 ============================
//   `Run()` 里有一段要**真面板**（`BattleLogPanel.Create` 依赖 `LayoutSpace.Cam` / `HudCanvas`）
//   ⇒ **宿主必须是 `Editor/BattleScene.cs` 的聚合行**（那一条 `Bxxx_Assert.Run(); pass += …; fail += …;`）。
//   其余几段是纯函数 / 源码级，接到哪儿都能跑；⚠️ 但**接线由调度台统一做**，⛔ 本文件不改任何宿主。
//
// ============================ 判据（一手，逐条现读）============================
//   B117-38 · `d:/2/tools/decomp_full/BattleManager__WaitingForBlockingAction.c:26-40`：
//     逐条动作问 `actionType(+0x0) == 0x1b /* transformUnit */` 或 `blockPlayerActions(+0xF4)`；
//     字段偏移互证 = `d:/2/tools/il2cpp_out/dump.cs:118994`。
//   B117-48 · `CemeteryManager__ClickCardLink.c` 尾段 `BattleManager__DisplayCard(manager, card, 1, 0)`；
//     `BattleManager__DisplayCard.c` 尾段 `CardDisplayWindow__ShowBattleCard(...)`（大卡窗）。
//   B117-49 · 同一个 `ClickCardLink.c:56-70`：`FindIntersectingLink` 未命中 ⇒ 扫下一行；全未命中 ⇒ return。
//   B117-30 · `BattleManager__DisplayCard.c` 把 `matchData(+0xA8).warlordLifeChange(+0x20)` 当第 3 实参传下去。
//   B117-40 · `BattleManager__ReplaySkipAnims.c`（回放 ∧ `+0x521`）· `ClickPauseReplay.c:10` 置 1 ·
//     `ClickPlayReplay.c:10` 清 0 · `CardScript__get_AttackStepTime.c` 除以 `DAT_1834b2e8c`（实读 3.0）。
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B117_WBD_Assert
{
    const string P = "[B117_WBD] ";

    static int _ok, _bad;
    /// <summary>宿主聚合行用的读口（否则失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    static string ReadSrc(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch (Exception ex) { Debug.LogWarning(P + "读源码失败 " + rel + "：" + ex.Message); return null; }
    }

    public static void Run()
    {
        Debug.Log(P + "--- B117-WBD：BattleDriver 六条 + 登场 clip 偏移 ---");
        string drvSrc = ReadSrc("CardPresentation/Battle/BattleDriver.cs");
        string logSrc = ReadSrc("CardPresentation/Battle/BattleLogPanel.cs");

        SecCommentDrift(drvSrc);
        SecBlocking(drvSrc);
        SecLogClickSource(drvSrc);
        SecLogClickHitTest(logSrc);
        SecWarlordWiring(drvSrc);
        SecReplaySkip(drvSrc);
        SecDeployClipSeek(drvSrc);

        Debug.Log(P + "B117-WBD 合计：" + _ok + " 通过 / " + _bad + " 失败");
    }

    // ================================================================================================
    //  B117-31 · 过期注释：`SkirmishWarlordLifeChange = -10` ⇒ 现读 0
    // ================================================================================================
    static void SecCommentDrift(string src)
    {
        Debug.Log(P + "--- B117-31：两处过期注释（-10 ⇒ 0）---");
        Check(src != null, "（前提）读得到 `BattleDriver.cs`");
        if (src == null) return;
        Check(src.IndexOf("SkirmishWarlordLifeChange = -10", StringComparison.Ordinal) < 0,
              "★ B117-31：`BattleDriver.cs` 里**不再**写 `SkirmishWarlordLifeChange = -10`"
            + "（现读 = 0，`GameplayVariables.cs:116`，2026-11-03 `B117_F6` 改）"
            + "｜🧨 把那句注释改回 −10 ⇒ 本条红");
        Check(GameplayVariables.SkirmishWarlordLifeChange == 0,
              $"★ B117-31（判别式）：常量现读 = {GameplayVariables.SkirmishWarlordLifeChange}（应为 0）"
            + " —— 防「注释改了、常量又被改回去」两边打架");
        Check(GameplayVariables.ClassicWarlordLifeChange == 10,
              $"★ B117-31（判别式）：经典档现读 = {GameplayVariables.ClassicWarlordLifeChange}（应为 +10）");
        // ⚠️ 另一处 `RuleCore.cs:645`（「经典 0 ⇒ 这一段一次都不走」）**不归本代理**（`RuleCore.cs` 有别的写手约束）
        //    ⇒ 由调度台转派；本文件不断言它。
    }

    // ================================================================================================
    //  B117-38 · 阻塞窗口 = 「这一批里有没有真动作」（不是「有没有事件」）
    // ================================================================================================
    static BattleEvent Ev(EvtKind k, string effect = null)
    {
        return new BattleEvent { Kind = k, Player = 0, Slot = 4, CardId = "B117WBD", Effect = effect };
    }

    static void SecBlocking(string src)
    {
        Debug.Log(P + "--- B117-38：阻塞窗口 = 逐动作 flag ---");
        // ---- ① 纯视觉 / 资源类：一条都不挡 ----
        Check(!BattleDriver.BlocksPlayerActions(Ev(EvtKind.Trigger)),
              "★ ① `Trigger` **不挡**（原版触发效果那一族不是带 `blockPlayerActions` 的排队动作）");
        Check(!BattleDriver.BlocksPlayerActions(Ev(EvtKind.Draw)),
              "★ ① 普通 `Draw` **不挡**（改前它会把拖牌/结束回合一起锁住 = 手感杀手 #2）");
        Check(!BattleDriver.BlocksPlayerActions(Ev(EvtKind.GainFaith))
              && !BattleDriver.BlocksPlayerActions(Ev(EvtKind.StatusApplied))
              && !BattleDriver.BlocksPlayerActions(Ev(EvtKind.CollectWaystone)),
              "★ ① `GainFaith` / `StatusApplied` / `CollectWaystone` **都不挡**（纯资源 / 纯表现）");

        // ---- ② 真动作：都挡 ----
        Check(BattleDriver.BlocksPlayerActions(Ev(EvtKind.Attack))
              && BattleDriver.BlocksPlayerActions(Ev(EvtKind.Hit))
              && BattleDriver.BlocksPlayerActions(Ev(EvtKind.Deploy))
              && BattleDriver.BlocksPlayerActions(Ev(EvtKind.Ability))
              && BattleDriver.BlocksPlayerActions(Ev(EvtKind.Death)),
              "★ ② `Attack` / `Hit` / `Deploy` / `Ability` / `Death` **都挡**");

        // ---- ③ 判别式：`Draw` 的 `display` 那一档**例外**（原版 `displayFromHand(57)` 真阻塞 2.7 s）----
        Check(BattleDriver.BlocksPlayerActions(Ev(EvtKind.Draw, "display")),
              "★ ③ `Draw` 且 `Effect == \"display\"` ⇒ **挡**（原版 `displayFromHand` 真 `yield`；"
            + "🧨 把 `Draw` 一刀切成「全不挡」⇒ 本条红）");

        // ---- ④ 灭自证：一批只有 Trigger + Draw ⇒ **不开闸**（改前 `Count > 0` 会开）----
        var visualBatch = new List<BattleEvent> { Ev(EvtKind.Trigger), Ev(EvtKind.Draw) };
        Check(!BattleDriver.BatchBlocksPlayerActions(visualBatch),
              "★ ④ **灭自证**：一批只有 `Trigger + Draw` ⇒ `BatchBlocksPlayerActions` = **false**"
            + "（改前那句 `if (_signalBuf.Count > 0)` 会开闸 ⇒ 这条红）");
        var realBatch = new List<BattleEvent> { Ev(EvtKind.Trigger), Ev(EvtKind.Hit) };
        Check(BattleDriver.BatchBlocksPlayerActions(realBatch),
              "★ ④（反向·防一刀切）：同一批里混进一条 `Hit` ⇒ **true**（真动作仍然挡）");
        Check(!BattleDriver.BatchBlocksPlayerActions(new List<BattleEvent>()),
              "★ ④ 空批 ⇒ false（⛔ 不许把「什么都没有」当成阻塞）");

        // ---- ⑤ 源级：开闸口换掉了（防「函数写好了、没人用」）----
        if (src == null) { Check(false, "（前提）读得到 `BattleDriver.cs` —— ⑤ 源级三条没跑"); return; }
        Check(src.IndexOf("if (BatchBlocksPlayerActions(_signalBuf)) BeginBlockingWindow(t);", StringComparison.Ordinal) >= 0,
              "★ ⑤（源级）`PlaySignals` 的开闸口 = `if (BatchBlocksPlayerActions(_signalBuf)) BeginBlockingWindow(t);`");
        Check(src.IndexOf("if (_signalBuf.Count > 0) BeginBlockingWindow(t);", StringComparison.Ordinal) < 0,
              "★ ⑤（源级·灭自证）旧写法 `if (_signalBuf.Count > 0) BeginBlockingWindow(t);` **已消失**");
        Check(src.IndexOf("BeginBlockingWindow(_clock + duration + BlockingFrameMargin);", StringComparison.Ordinal) >= 0,
              "★ ⑤（源级）起飞那一拍**仍**自己开闸（`OnPlayerDeployFlightStart`）—— ⛔ 别把那条也一起删了");
    }

    // ================================================================================================
    //  B117-48 · 点日志卡名 ⇒ 大卡窗（源级）
    // ================================================================================================
    static void SecLogClickSource(string src)
    {
        Debug.Log(P + "--- B117-48：点日志卡名 ⇒ `CardDisplayWindow`（大卡窗）---");
        if (src == null) { Check(false, "（前提）读得到 `BattleDriver.cs` —— B117-48 三条没跑"); return; }
        Check(src.IndexOf("ShowLogCardLarge(key);", StringComparison.Ordinal) >= 0,
              "★ B117-48：`ClickLogRow` 的终点 = `ShowLogCardLarge(key)`（= 大卡窗）");
        // ⚠️ 判据带**缩进 + 分号**：`ClickLogRow` 那段订正注释里引用了旧写法（不带分号），
        //    只按裸名字搜会把「注释里那句历史」当成「代码还在」⇒ 假红。
        Check(src.IndexOf("            ShowLogCard(key, row);", StringComparison.Ordinal) < 0,
              "★ B117-48（灭自证）：**旧调用点 `ShowLogCard(key, row);` 已消失**"
            + "（那弹的是 226×359.8 的小预览卡 = 悬停那一档）");
        Check(src.IndexOf("_cardDisplay.ShowBattleCard(cd, def, rows, enemyInPlayAmbush);", StringComparison.Ordinal) >= 0,
              "★ B117-48：`ShowLogCardLarge` 落到 `_cardDisplay.ShowBattleCard(...)`"
            + "（原版 `DisplayCard → CardDisplayWindow.ShowBattleCard` 那一跳）");
        Check(src.IndexOf("if (!TutorialAllowsCardDisplay)", StringComparison.Ordinal) >= 0,
              "★ B117-48：教程闸照抄（原版 `DisplayCard.c:44-50` 的 `hideLargeCardDisplay`）");
    }

    // ================================================================================================
    //  B117-49 · 点行命中区 = 行内 TMP 链接（带可测降级）
    // ================================================================================================
    static void SecLogClickHitTest(string logSrc)
    {
        Debug.Log(P + "--- B117-49：命中区 = 行内 TMP 链接 ---");
        if (logSrc == null) { Check(false, "（前提）读得到 `BattleLogPanel.cs` —— B117-49 源级没跑"); }
        else
        {
            Check(logSrc.IndexOf("public int LinkRowAt(Vector3 wp, Camera cam)", StringComparison.Ordinal) >= 0,
                  "★ B117-49（源级）`BattleLogPanel.LinkRowAt` 存在（原版 `ClickCardLink` 那条 `FindIntersectingLink`）");
            Check(logSrc.IndexOf("int row = ped != null ? LinkRowAtScreen(ped.position, ped.pressEventCamera) : -1;",
                                 StringComparison.Ordinal) >= 0,
                  "★ B117-49（源级）uGUI 的 `BG.PointerDown` 走 `LinkRowAtScreen`（⛔ 不再是 `RowAtScreen`）");
            Check(logSrc.IndexOf("if (ForceLinkProbeUnavailableForTest || cam == null)", StringComparison.Ordinal) >= 0,
                  "★ B117-49（源级）**降级路径**在（`ForceLinkProbeUnavailableForTest || cam == null`）");
        }

        // ---- 运行时：真面板 + 真 TMP（宿主必须是 `Editor/BattleScene.cs`）----
        var cardName = "Deathwing Terminator";
        var entries = new List<BattleLogPanel.Entry>
        {
            new BattleLogPanel.Entry { CardId = cardName, LinkText = cardName, Text = cardName + " attacks" },
            new BattleLogPanel.Entry { CardId = null, LinkText = null, Text = "No card on this row" },
        };
        BattleLogPanel panel = null;
        try
        {
            panel = BattleLogPanel.Create(null);
            if (panel != null) { panel.SetEntries(entries); panel.Show(); }
        }
        catch (Exception ex) { Debug.LogWarning(P + "建临时日志面板抛了：" + ex); panel = null; }

        Check(panel != null, "（前提）临时日志面板建起来了（`BattleLogPanel.Create(null)`）—— 宿主必须是 `Editor/BattleScene.cs`；"
                           + "不成立 ⇒ 下面 ①②③④ 一条都没跑（这一条红，⛔ 不许静默跳过）");
        if (panel == null) return;

        try
        {
            var cam = LayoutSpace.Cam;
            Check(cam != null, "（前提）`LayoutSpace.Cam` 非空（TMP 的 `FindIntersectingLink` 要相机；它为空时走降级分支）");
            Check(!string.IsNullOrEmpty(panel.RowLinkKey(0)) && string.IsNullOrEmpty(panel.RowLinkKey(1)),
                  "（前提）第 0 行**有**卡名链接、第 1 行**没有**（夹具形状）");

            // ① 压在链接上 ⇒ 命中第 0 行
            // 🔴 **2026-11-02（`B117-FIXD`）**：批处理**没有帧循环** ⇒ `Show()` 的同一拍 TMP 还没生成字形，
            //   `FindIntersectingLink` 判 `textInfo.characterInfo` ⇒ 那一拍扫出来恒 −1。
            //   先显式生成一次（同族已记在 `Editor/BattleScene.cs:37198` 的 `B117-FIXC` 注释里）。
            panel.ForceRowTextLayoutForTest();
            Vector3 probe = Vector3.zero;
            bool found = cam != null && panel.FindLinkProbe(0, cam, out probe);
            Check(found, "（前提）扫得到一个**压在链接字形上**的点（`FindLinkProbe`）");
            if (found)
                Check(panel.LinkRowAt(probe, cam) == 0,
                      "★ ① 压在链接上 ⇒ `LinkRowAt` = 0（原版 `FindIntersectingLink` 命中那一行）"
                    + "｜🧨 把判据写成恒 -1 ⇒ 红");

            // ② 灭自证：**没有链接的那一行**，行中心仍然在行矩形里 ⇒ `RowAt` 答它、`LinkRowAt` 答 -1
            Vector3 c1;
            bool got1 = panel.RowCenterWorld(1, out c1);
            Check(got1, "（前提）第 1 行底板的中心取得到");
            if (got1)
            {
                Check(panel.RowAt(c1) == 1,
                      "★ ②（前提）同一个点，`RowAt`（整行矩形）仍然命中第 1 行 —— 说明这个点在**行内**");
                Check(panel.LinkRowAt(c1, cam) == -1,
                      "★ ② **灭自证**：这一行**没有链接** ⇒ `LinkRowAt` = **-1**（原版只认链接）"
                    + "｜🧨 退回「整行矩形」⇒ 这里会答 1 ⇒ 红");
            }

            // ③ 降级路径可测：强制「TMP 答不出来」⇒ 退回整行命中，并且**留下痕迹**
            panel.ForceLinkProbeUnavailableForTest = true;
            int deg = got1 ? panel.LinkRowAt(c1, cam) : -1;
            bool flagged = panel.LastLinkProbeDegraded;
            panel.ForceLinkProbeUnavailableForTest = false;
            Check(got1 && deg == 1 && flagged,
                  $"★ ③ **降级路径**：强制「链接探测不可用」⇒ 退回整行命中（第 1 行，实得 {deg}）且 "
                + $"`LastLinkProbeDegraded` = {flagged}（⛔ 不许静默退化）");

            // ④ 复位之后不能残留降级标记（防「一次降级、永远降级」）
            int again = got1 ? panel.LinkRowAt(c1, cam) : -1;
            Check(!panel.LastLinkProbeDegraded && again == -1,
                  $"★ ④ 关掉强制降级之后 ⇒ 又答 -1（实得 {again}）、标记复位 = {!panel.LastLinkProbeDegraded}");
        }
        finally
        {
            try { panel.Hide(); } catch (Exception) { }
            try { UnityEngine.Object.DestroyImmediate(panel.gameObject); } catch (Exception) { }
        }
    }

    // ================================================================================================
    //  B117-30 · 详情窗督军血量：**驱动那一跳**（窗那一半由 `B117F12_Assert` 覆盖）
    // ================================================================================================
    static void SecWarlordWiring(string src)
    {
        Debug.Log(P + "--- B117-30：`Begin` 把 `Ctx.Vars.warlordLifeChange` 喂给详情窗 ---");
        if (src == null) { Check(false, "（前提）读得到 `BattleDriver.cs` —— B117-30 没跑"); return; }
        Check(src.IndexOf("CardDisplayWindow.SetWarlordLifeChange(_vars", StringComparison.Ordinal) >= 0,
              "★ B117-30（源级）：`Begin` 里把本局模式的增量喂进详情窗"
            + "（原版 `BattleManager__DisplayCard.c` 把 `matchData+0xA8 → +0x20` 当第 3 实参传下去）"
            + "｜🧨 删掉那一跳 ⇒ 详情窗又回到「显示 SO 原值、比实战少 10」⇒ 红");
        Check(src.IndexOf("_vars = vars ?? GameplayVariables.Classic;", StringComparison.Ordinal) >= 0,
              "★ B117-30（前提）：喂值紧跟在 `_vars` 赋值那一行之后（`Begin` 是唯一汇合点）");
        // 喂值口本身是幂等的静态口 —— 这里做一次往返（证明它真写得进、读得出）
        int old = CardDisplayWindow.WarlordLifeChangeForTest;
        try
        {
            CardDisplayWindow.SetWarlordLifeChange(7);
            Check(CardDisplayWindow.WarlordLifeChangeForTest == 7,
                  "★ B117-30：`SetWarlordLifeChange(7)` ⇒ 读回 7（写口 ↔ 读口同源）");
        }
        finally { CardDisplayWindow.SetWarlordLifeChange(old); }   // 复位，别把增量漏给后续断言
    }

    // ================================================================================================
    //  B117-40 · 回放「暂停 = 跳过动画」（BattleDriver 这一半）
    // ================================================================================================
    static void SecReplaySkip(string src)
    {
        Debug.Log(P + "--- B117-40：回放跳过动画（`ReplaySkipAnims`）---");
        var root = new GameObject("B117WBD_replay");
        var drv = root.AddComponent<BattleDriver>();
        try
        {
            Check(!drv.ReplaySkipAnimsNow,
                  "★ ① 非回放局 ⇒ `ReplaySkipAnimsNow` = false（原版 `matchType != 0xA0 ⇒ 0`）");

            // ② **灭自证**：回放但**没暂停** ⇒ 仍然 false
            //    （改前那半句只看 `_replaySession` ⇒ 这种状态会算成「跳过」⇒ 正常回放被 3× 加速）
            drv.SetReplaySessionForTest(true);
            Check(!drv.ReplaySkipAnimsNow,
                  "★ ② **灭自证**：回放局 ∧ **未暂停** ⇒ `ReplaySkipAnimsNow` = **false**"
                + "（原版 `ReplaySkipAnims` 还要求 `+0x521`，只有 `ClickPauseReplay` 才置）"
                + "｜🧨 退回「只看 `_replaySession`」⇒ 本条红");

            // ③ 暂停 ⇒ true（= 打开跳过动画）
            drv.SetReplayPaused(true);
            Check(drv.ReplaySkipAnimsNow,
                  "★ ③ 回放局 ∧ 暂停 ⇒ `ReplaySkipAnimsNow` = **true**（原版 `ClickPauseReplay.c:10` 写 `+0x521 = 1`）");

            // ④ 继续 ⇒ false（原版 `ClickPlayReplay.c:10` 清 0）
            drv.SetReplayPaused(false);
            Check(!drv.ReplaySkipAnimsNow,
                  "★ ④ 继续播放 ⇒ 回到 false（原版 `ClickPlayReplay.c:10` 清 0）");
        }
        finally
        {
            try { drv.SetReplaySessionForTest(false); drv.SetReplayPaused(false); } catch (Exception) { }
            UnityEngine.Object.DestroyImmediate(root);
        }

        if (src == null) { Check(false, "（前提）读得到 `BattleDriver.cs` —— B117-40 源级没跑"); return; }
        Check(src.IndexOf("float AttackMotionDurationOf(BattleEvent e)", StringComparison.Ordinal) >= 0
           && src.IndexOf("float step = ReplayScaledAttackStep(CardFeel.AttackStepTime);", StringComparison.Ordinal) >= 0,
              "★ ⑤（源级）攻击序列时长走 `ReplayScaledAttackStep(CardFeel.AttackStepTime)`（= 原版 `get_AttackStepTime` ÷3）");
        Check(src.IndexOf("NoteAttackTween(CardFeel.MeleeAttack(", StringComparison.Ordinal) >= 0
           && src.IndexOf("NoteAttackTween(CardFeel.AttackRecover(", StringComparison.Ordinal) >= 0,
              "★ ⑤（源级）两条攻击补间都登记进「暂停即提速」那一组（`NoteAttackTween(...)`）");
        Check(src.IndexOf("void ApplyReplaySkipToLiveAttackTweens", StringComparison.Ordinal) < 0
           && src.IndexOf("readonly List<Tween> _attackTweens", StringComparison.Ordinal) < 0 && src.IndexOf("_attackTweens.Add(", StringComparison.Ordinal) < 0,
              "★ ⑤（源级·反向）`ApplyReplaySkipToLiveAttackTweens` / `_attackTweens` 已删（C08）：跳过档改由 `stepScale` 按段缩，⛔ 别加回来（会双重缩放）");
        Check(src.IndexOf("plan.At = ReplayScaledAttackStep(EventTiming.AttackStepRanged);", StringComparison.Ordinal) >= 0
           && src.IndexOf("float vfxAt = ReplayScaledAttackStep(", StringComparison.Ordinal) >= 0,
              "★ ⑤（源级）远程那两处调度（VFX 时刻 / `plan.At`）也走同一个 ÷3 口径");
    }

    // ================================================================================================
    //  跨文件连带 · 登场 clip 从 0.65 偏移起播（AI 三条 `Handoff.Apply`）
    // ================================================================================================
    static void SecDeployClipSeek(string src)
    {
        Debug.Log(P + "--- 连带：AI 登场 clip 的起播相位 ---");
        if (src == null) { Check(false, "（前提）读得到 `BattleDriver.cs` —— 连带那三条没跑"); return; }

        int n = 0, i = 0;
        const string pat = "StartHandToBoardClip(v, DeploySequence.HandoffTime - DeploySequence.MoveTime);";
        while ((i = src.IndexOf(pat, i, StringComparison.Ordinal)) >= 0) { n++; i += pat.Length; }
        Check(n == 3, $"★ 三条 `Handoff.Apply` 都传了相位偏移（实得 {n} 处，应为 3）"
                    + " —— 交接在 0.95 才跑，clip 本该在飞行结束（0.3）起播");

        Check(src.IndexOf("else if (tw != null && seekSeconds > 0f) tw.GotoWithCallbacks(seekSeconds);",
                          StringComparison.Ordinal) >= 0,
              "★ 用 `GotoWithCallbacks`（⛔ 不是 `Goto`）—— 否则 clip 在 0.55 那条"
            + "「`SetDissolveAmount(0) / RestoreOriginalMaterial`」回调会被跳过去，溶解材质停在半途");
        Check(src.IndexOf("StartHandToBoardClip(card));", StringComparison.Ordinal) >= 0,
              "★（判别式·防一刀切）**玩家**那条路（`OnPlayerDeployFlightStart`）仍按 `DeploySequence.MoveTime` 起播、不传偏移");
        Check(src.IndexOf("（0.4 s）", StringComparison.Ordinal) < 0,
              "★ 注释漂移已订正：`BattleDriver.cs` 里不再有「登场交接 = 0.4 s」那种说法（现读 0.95 / 全序列 1.30）");
    }
}
