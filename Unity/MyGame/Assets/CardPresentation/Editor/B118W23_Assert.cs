// B118W23_Assert.cs — 执行代理 `B118_W23`（第一百一十八会话 · 批 2「战斗表现 / HUD 读数口」四条）
//
// 判据正本 → `资料/完全复刻/判据/B118_J10_215条真缺口分诊.md` §2 的 #24 / #52 / #87 / #90
// 报告      → `资料/完全复刻/报告/B118_W23_批2战斗表现四条.md`
//
// ============================ 它盯的是哪三段 ============================
//  · **U 段（#90）**：`AbilityIconAnim.TraitOfTrigger` 原来以 `TraitFrames` 绑定表当
//    `Enum.IsDefined(typeof(DefinedTrait))` 的**代理** ⇒ 覆盖窄于原版 134 个成员。
//    本轮改成转发 `RuleEngine/Core/CardDefPartial_B94B16.cs:142 GetTraitFromAbilityTrigger`
//    （原版 `CardTraitCollection__GetTraitFromAbilityTrigger.c:1-58`：三个硬编码 + 134 条同名解析）。
//    🔴 **判据数是 32/97**（`CardDefPartial_B94B16.cs:140` 现读：「97 个 trigger 里能折出 trait 的 = 32」），
//    而旧代理只覆盖 **25/32**（差 7 个：rage / sacrifice / survivor / survivorSpent / requiem / landing / support
//    —— 那 7 个在 `Resources/trait_effect_bindings.json` 里没有键）。
//  · **E 段（#52）**：`BattleDriver.MaybeAiEmote` 原来有「概率 0.06 + 冷却 14 s」两道闸。
//    判据现读（ILSpy `VoiceLinesController.AIResponse` 全文）= `AI__ExecuteAction.c:463` 每条**走成的**
//    动作调 `PlayRandomAIVoiceLine` → `Random.Range(2, 11)` → `AIResponse(chatId, 0, isResponse:false)`，
//    里面**掷一次** `Random.Range(0f,1f) < emote_chance_after_action`；掷中才排延迟协程。
//    ⇒ **概率闸要留**（原版有，值在 RemoteConfig、本地不可得）、**冷却要删**（原版没有）、
//      而且原版**没有「已排期就去重」**。
//    ⇒ 验收 = ① 连排两条都成（旧实现第二条被 `_aiEmotePending` 挡掉）；
//              ② 保留 `_lastAiEmoteAt` 原值再排一条**也成**（旧实现会被 14 s 冷却挡掉）。
//  · **N 段（#24）**：`ChatHotkeyUiState()` 是 15 档 `UIstateOptions` 的唯一映射，
//    `BattleUiIsNormalBattle` = `ChatHotkeyUiState() == 1`（合并成一份，⛔ 别再写第二套）。
//  · **#87 没有断言** —— 它的残差是「原版哪几类时机**不**刷词条图标」的**调用点审计**（纯静态对账，
//    结论写进报告 §#87）；⛔ 不为一条审计结论造一条运行期断言（那是自证）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · U1：把 `TraitOfTrigger` 改回旧实现（`TraitExists(camel) ? camel : null`）⇒ 可折数 **32 → 25** ⇒ U1 红。
//  · U2：把 Core 那三个硬编码删掉只留名字解析 ⇒ `ThisCardPlayed` 折不出 `rally` ⇒ U2/U4 红。
//  · E1：把 `MaybeAiEmote` 开头那句 `if (!AiShouldDriveOpponent) return;` 删掉 ⇒ 挂联机层那条仍排上 ⇒ E 段红。
//  · E2：给 `MaybeAiEmote` 加回 `if (_aiEmotePending) return;` ⇒ 第二条排不上 ⇒ E2 红。
//  · E4：加回 `if (_clock - _lastAiEmoteAt < AiEmoteCooldown) return;` ⇒ 保留冷却值再排那条排不上 ⇒ E4 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  · `Run()` —— **纯常量 / 纯函数**（只要 `RuleEngine`，不需要 `BattleDriver`）⇒ 可接 `BattleScene` /
//    `RuleEngineTest` 的聚合行**同一物理行追加**（⚠️ 必须落在该行第一个 `//` 之前）：
//        B118W23_Assert.Run(); pass += B118W23_Assert.Pass; fail += B118W23_Assert.Fail;
//  · `RunWithDriver(BattleDriver d)` —— 要一局**已经 `Begin()` 过、且未终局**的 driver（#24 / #52）⇒
//    接在 `Editor/BattleScene.cs` 里 `BattleChatAssertE7` 那一族之后。
//  ⛔ 本代理**没有**任何宿主接线（`Editor/BattleScene.cs` 归调度台）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B118_W23`：批 2 四条（#24 HUD 读数口 / #52 AI 自发发言无条件 / #87 审计 / #90 trigger→trait 转发）。</summary>
public static class B118W23_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B118W23] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>`AbilityTrigger` 的数值上界（现读最大 440 = Landing；留一倍余量给反查循环）。</summary>
    const int TriggerScanMax = 2048;

    /// <summary>纯函数段（#90）。宿主：任一聚合行。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B118_W23 · #90 `AbilityIconAnim.TraitOfTrigger` → 转发 Core（原版 `GetTraitFromAbilityTrigger`）");
        U_TraitOfTrigger();
    }

    static void U_TraitOfTrigger()
    {
        // U0 前提：Core 那张 `DefinedTrait` 表是**整表**抄进来的（134 条，判据 `DefinedTrait.cs` 逐条）。
        C(RuleEngine.CardDef.DefinedTraitCount, 134,
          "U0（前提）`DefinedTrait` 表 134 条齐全（原版 `DefinedTrait.cs` 逐条转写）");

        // U1 🔴 主判据：97 个 trigger 里能折出 trait 的 = **32**（原版逐项推：29 同名 + 3 硬编码）。
        //    旧代理只到 25（缺 rage / sacrifice / survivor / survivorSpent / requiem / landing / support）。
        int foldable = 0, seen = 0;
        for (int n = 0; n < TriggerScanMax; n++)
        {
            string name = RuleEngine.CardDef.AbilityTriggerNameOf(n);
            if (name == null) continue;                 // 这个数值不在 `AbilityTrigger` 枚举里
            seen++;
            if (AbilityIconAnim.TraitOfTrigger(name) != null) foldable++;
        }
        C(seen, 97, "U1（前提）`AbilityTriggerNameOf` 反查出 97 个 trigger（原版枚举 97 条）");
        C(foldable, 32,
          "★★ U1 **97 个 trigger 里 32 个能折出 trait**（原版 `CardTraitCollection__GetTraitFromAbilityTrigger.c` 逐项推）"
        + "｜🧨 改回「以 `TraitFrames` 绑定表当代理」⇒ 只剩 25（红）");

        // U2 🔴 三个硬编码（原版 switch 三支）逐条钉住 —— 它们排在名字解析**之前**，最容易被人「顺手删」。
        C(AbilityIconAnim.TraitOfTrigger("ThisCardPlayed"), "rally", "U2 `ThisCardPlayed → rally`（原版硬编码 0→10）");
        C(AbilityIconAnim.TraitOfTrigger("ThisUnitDeath"), "backlash", "U2 `ThisUnitDeath → backlash`（原版硬编码 80→120）");

        // U3 🔴 `Unstable` 徽标缺口（账 `战斗逻辑驱动:994` 的活缺口，池内 18 张）。
        C(AbilityIconAnim.TraitOfTrigger("Unstable"), "unstable", "U3 `Unstable → unstable`（原版硬编码 81→1010；账 :994 的 Unstable 徽标缺口在此）");

        // U4 旧代理**漏掉**的那 7 个里抽 3 个（独立于 U1 的计数，逐个钉名字）。
        C(AbilityIconAnim.TraitOfTrigger("Landing"), "landing", "U4 `Landing → landing`（旧代理缺这一位；`PlayNow(int)` 的兜底也走同一条）");
        C(AbilityIconAnim.TraitOfTrigger("Requiem"), "requiem", "U4 `Requiem → requiem`（旧代理缺）");
        C(AbilityIconAnim.TraitOfTrigger("Support"), "support", "U4 `Support → support`（旧代理缺）");

        // U5 负例：原版对绝大多数 trigger 也返回 `none`（⛔ 不是「折不出就是 bug」）。
        C(AbilityIconAnim.TraitOfTrigger("TurnStart"), null, "U5 `TurnStart` 折不出（原版 `Enum.TryParse` 失败 ⇒ none）");
        C(AbilityIconAnim.TraitOfTrigger(""), null, "U5 空名字 ⇒ null（⛔ 不猜）");
        C(AbilityIconAnim.TraitOfTrigger((string)null), null, "U5 null 名字 ⇒ null（⛔ 不猜）");
    }

    /// <summary>需要一局已 `Begin()` 的 driver（#24 / #52）。宿主：`BattleScene` 里 `BattleChatAssertE7` 那一族之后。</summary>
    public static void RunWithDriver(BattleDriver d)
    {
        MenuCheck.Section(_sink, "🆕 B118_W23 · #24 HUD 读数口 / #52 AI 自发发言无条件");
        N_UiState(d);
        E_AiEmoteUnconditional(d);
    }

    static void N_UiState(BattleDriver d)
    {
        C(d != null, true, "N0（前提）拿到 `BattleDriver`");
        if (d == null) return;
        C(d.Ctx != null, true, "N0（前提）driver 已经 `Begin()` 过一局（`Ctx != null`）");
        if (d.Ctx == null) return;

        // 🔴 2026-11-03（`B124-W28`）：批处理**没有帧循环** ⇒ `TickBlockingWindow` 永远不会自己收尾
        //    （`BattleDriver.cs:20897` 那句 `_timeline.Count == 0 && _clock >= _blockingQuietAt` 只由帧驱动）
        //    ⇒ 上一批演出留下的 `_blocking` 会一直挂到收工（`B124-W27` 补上 `!WaitingForBlockingAction` 之后
        //    这条前提就**必然**是假 —— 实测 N1 恒 = 3，而 3 正是 `ChatHotkeyUiState()` 对 `_blocking` 的正确映射）。
        //    ⇒ 这里按**帧循环本该做的事**显式放行（`SetBlockingForTest` 的存在理由就是这个；
        //    它只改前置条件，**映射函数本身一个字没碰**，下面的断言才是被测对象）。
        bool wasBlocking = d.WaitingForBlockingAction;
        if (wasBlocking) d.SetBlockingForTest(false);

        // N1 档位落在原版 `UIstateOptions` 的 0…14 闭区间（枚举 15 档）。
        int st = d.ChatHotkeyUiStateForTest();
        T(st >= 0 && st <= 14, $"N1 `ChatHotkeyUiState()` 落在 0…14（原版 15 档枚举；实得 {st}）");

        // N2 🔴 `normalBattle(1)` 的等价物：一局进行中、没有模态窗 ⇒ 必须**恰好是 1**。
        //    这正是 `BattleUiIsNormalBattle`（= `ChatHotkeyUiState() == 1`）背后的那一档，
        //    也是原版 `CanUseWaystone.c:44-47` 第 ③ 道闸的判据。
        bool plain = !d.Ctx.IsOver && !d.InMulligan
                  && !(d.Choose != null && d.Choose.Visible)
                  && !(d.End != null && d.End.Visible)
                  // 🔴 2026-11-03（`B124-W27`）：**漏了 `_blocking`** —— `ChatHotkeyUiState()` 把 `WaitingForBlockingAction`
                  //   正映射成 **3**（`BattleDriver.cs:36276`，playingMinion 演出中）⇒ 原前提不成立（实测实得 3）。映射是对的，补前提。
                  && !d.WaitingForBlockingAction
                  // 🔴 2026-11-03（`B124-W28`）：`ChatHotkeyUiState()` 的**其余前置分支也要列齐** ——
                  //    否则「前提成立、实测却不是 1」又会变成哑谜（判据 = `BattleDriver.cs:36262-36275` 那串 if）。
                  && !d.AwaitingPlayTarget                                             // → 9
                  && !(d.selector != null && d.selector.Visible)                        // → 6
                  && !(d.interaction != null && d.interaction.IsDragging)               // → 2
                  && !(d.skillPanel != null && d.skillPanel.Visible)                    // → 10
                  && d.SelectedSlot < 0;                                               // → 9 / 10
        T(plain, "N2（前提）这一局处在「无终局 / 无换牌 / 无选牌 / 无演出阻塞 / 无选目标 / 无选择器 / 无拖拽 / 无技能面板 / 无已选格」的普通档"
               + $"（终局 {d.Ctx.IsOver} · 换牌 {d.InMulligan} · 阻塞 {d.WaitingForBlockingAction}(批处理已显式放行 {wasBlocking})"
               + $" · 选目标 {d.AwaitingPlayTarget} · 选择器 {(d.selector != null && d.selector.Visible)}"
               + $" · 拖拽 {(d.interaction != null && d.interaction.IsDragging)}"
               + $" · 技能面板 {(d.skillPanel != null && d.skillPanel.Visible)} · 已选格 {d.SelectedSlot}）");
        if (plain) C(st, 1, "★★ N2 普通档 ⇒ `UIstate == normalBattle(1)`（原版 `BattleManager.GetUIState()` 唯一取值器；"
                          + "`BattleUiIsNormalBattle` 已并入这一份映射）");
    }

    static void E_AiEmoteUnconditional(BattleDriver d)
    {
        if (d == null) return;
        C(d.AiShouldDriveOpponent, true, "E0（前提）单机局（`AiShouldDriveOpponent`；联机局不替真人说话）");
        C(d.CanAiChatForTest, true, "E0（前提）AI 发言闸开着（非开场独白 / 非终局 / 未被静音）");
        if (!d.AiShouldDriveOpponent || !d.CanAiChatForTest) return;

        d.ResetAiEmoteForTest();
        bool first = d.ScheduleAiEmoteForTest();
        T(first, "E1 第 1 条自发发言**排上了**（原版每条动作都排）");
        float delay = d.LastAiEmoteDelayForTest;
        T(delay >= BattleDriver.AiReplyDelayMin + BattleDriver.AiResponseExtraDelay - 1e-3f
          && delay <= BattleDriver.AiReplyDelayMax + BattleDriver.AiResponseExtraDelay + 1e-3f,
          $"E1 延迟落在 `[min,max]+0.3`（原版 `Random.Range(min_reply_delay, max_reply_delay) + DAT_1834b2dc8(=0.3f)`；实得 {delay:F2}s）");

        // E2 🔴 主判据：**上一条还没到点**再排一条，仍然要排上 ——
        //    原版 `AI__ExecuteAction` 每条动作各起一条协程，没有「已经排过就 return」这道去重。
        bool second = d.ScheduleAiEmoteForTest();
        T(second, "★★ E2 连排两条**都成**（原版没有「已排期就去重」）"
                + "｜🧨 给 `MaybeAiEmote` 加回 `if (_aiEmotePending) return;` ⇒ 本行 false（红）");

        // E4 🔴 冷却已删：**保留 `_lastAiEmoteAt` 原值**（`ignoreCooldown:false`）再排一条，仍然要排上。
        //    旧实现（`if (_clock - _lastAiEmoteAt < AiEmoteCooldown) return;`，冷却 14 s）会在这里排不上。
        bool third = d.ScheduleAiEmoteForTest(ignoreCooldown: false);
        T(third, "★★ E4 冷却档**不再挡**（原版 `AIResponse` 全文没有冷却这一支）"
                + "｜🧨 加回 `if (_clock - _lastAiEmoteAt < AiEmoteCooldown) return;` ⇒ 本行 false（红）");

        d.ResetAiEmoteForTest();
        T(!d.AiEmotePendingForTest, "E3 复位后「已排期」标志清掉（⛔ 别把标志留在 true 卡住后续所有发言）");
    }
}
