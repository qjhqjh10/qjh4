// B141_W8_Assert.cs — 执行写手 `B141-W8`（2026-11-04）· 第一百二十八会话
//
// ============================ 它盯的是什么 ============================
//  A · 战-T09 S2：把 `BattleDriver.Update()` 里那 17 条 `if (…) { …; return; }` 收口到**两条轴**：
//      · 通道 A = `UiTopWindow()`（窗口层：谁在最上层谁收输入）
//      · 通道 B = `UiStateNow()`（战斗阶段：原版 `BattleManager.UIstate(+0x290)` 15 档）
//  B · B124-TACTDROP：战术卡落点的目标 = **松手时指针下的那张卡**（`DropPointerUnit` → 原版 `GetTargetInPos`），
//      ⛔ 不是「落点格号 → `Board[slot]`」（督军格不在部署候选集里 ⇒ 会解到邻格）。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//  · 窗口层：`WindowsManager.cs:995-1006`（`currentWindow` 单一字段）· `T09b_uGUI拦路判据.md` §A·2（叠放次序）
//  · 战斗阶段：`dump.cs:46314-46332`（15 档枚举）· `T09b_uGUI拦路判据.md` §B·1（逐档表）
//  · 战术卡落点：`decomp_full/BattleManager__GetTargetInPos.c:104/:248`（射线取指针下的 `CardScript`）
//    · `BattleManager__Update.c:136-139`（存 `+0x440`）· `:676-684`（松手拿它过 `IsValidSpellTarget`）
//    · `BattleManager__IsValidSpellTarget.c:28-66`（只要求 `IsInPlay` + targeted + 非隐藏/护盾/伪装 + criteria，
//      **没有任何格号 / 候选集**）⇒ 敌方督军是合法落点
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//  · A2 ← 在 `ChatHotkeyUiState()` 里另写一份映射 ⇒ 与 `UiStateNow()` 分叉 ⇒ 红
//  · A7 ← 把 `UiStateNow()` 的 4 档删掉（退回 9）⇒ 红
//  · B4 ← 把 `TryBeginPlayTargetPick` 的非单位分支退回 `Board[slot]` ⇒ 目标不再是指针下的督军 ⇒ 红
//  · B7 ← 把「指针下的卡」写成恒真（例如直接拿落点格号）⇒ 指针下什么都没有时也会结算 ⇒ 红
//  · C 组（源码闸）← 把任何一条判据改成「各分支自己再判一遍 Visible / IsDragging」⇒ 计数不对 ⇒ 红
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//  · **批处理没有相机 / 指针**：`HitSlot` 在产品路径上依赖战场相机，自检里恒不命中 ⇒
//    B 组用 `SetDropPointerForTest`（= `EscapePressedForTest` 同族的钉死口）喂「指针下是哪一格」；
//    产品路径与断言走的是**同一条** `DropPointerUnit`（钉死口就在它第一句，后面是同一段 HitSlot 逻辑）。
//  · **12 / 5 两档只有源码闸**：`mulliganFinished(12)` 要真跑换牌收尾、`playingNonTargetSpell(5)` 要真机上
//    一张非单位法术在演 —— 批处理里都构造不出来（如实记，⛔ 不硬凑）。
//  · 真机手感（ESC / 拖动 / 点击）不在这里，归 `资料/真Play待验清单.md`。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>执行写手 `B141-W8`：T09 S2（两条轴收口）+ B124-TACTDROP（战术卡落点含督军格）。</summary>
public static class B141_W8_Assert
{
    const string Prefix = "[B141W8] ";
    static int _ok, _bad;

    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    /// <summary>宿主入口（要一台已经 `Begin()` 过的 driver —— 接在 `BattleScene` 卸 driver **之前**那条聚合行）。</summary>
    public static void Run(BattleDriver d)
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B141-W8：T09 S2（UiTopWindow / UiStateNow）+ B124-TACTDROP 开始 ===");
        try
        {
            A_Axis(d);
            B_TacticDropOnWarlord(d);
            C_SourceGate();
        }
        catch (Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message
                       + "\n" + e.StackTrace);
        }
        finally
        {
            if (d != null) d.SetDropPointerForTest(-1, -1);   // 钉死口一定清干净（否则后面的断言会拿到假指针）
        }
        Debug.Log(Prefix + "B141_W8 合计：✓ " + _ok + " / ✗ " + _bad + "（共 " + (_ok + _bad) + " 条）");
    }

    // ==================================================================
    //  A · 两条轴
    // ==================================================================
    static void A_Axis(BattleDriver d)
    {
        Check(d != null, "A0（前提）拿到活 driver");
        if (d == null) return;
        Check(d.Ctx != null, "A0b（前提）driver 已经 `Begin()` 过一局（`Ctx != null`）");
        if (d.Ctx == null) return;

        int st = d.UiStateNow();
        Check(st >= 0 && st <= 14, "A1 `UiStateNow()` 落在原版 15 档枚举的 0…14（实得 " + st + "）");
        Check(d.ChatHotkeyUiStateForTest() == st,
              "A2 `ChatHotkeyUiState()` 是 `UiStateNow()` 的**转发壳**（两者同值："
            + d.ChatHotkeyUiStateForTest() + " / " + st + "）"
            + "｜🧨 在 `ChatHotkeyUiState()` 里另写一份映射 ⇒ 迟早分叉 ⇒ 红");

        // ---- 通道 A：窗口层 ----
        var settings = d.Settings;
        Check(settings != null, "A3（前提）设置窗建起来了（不然窗口层这一组等于没验）");
        if (settings != null)
        {
            settings.Hide();
            Check(d.UiTopWindowForTest() == BattleDriver.UIWinNone,
                  "A4 没有窗口开着 ⇒ `UiTopWindow()` = `UIWinNone`（实得 " + d.UiTopWindowForTest() + "）");
            settings.Show();
            Check(d.UiTopWindowForTest() == BattleDriver.UIWinSettings,
                  "★ A5 设置窗开着 ⇒ `UiTopWindow()` = `UIWinSettings`（实得 " + d.UiTopWindowForTest() + "）"
                + "｜🧨 在 `Update` 的分支里自己再判 `Visible`（而不是问这个唯一判据）⇒ 本组就证明不了「谁在最上层」只剩一处");
            settings.Hide();
        }

        var log = d.BattleLog;
        if (settings != null && log != null)
        {
            log.Show(); settings.Show();     // 两扇同时开 ⇒ 优先序必须是 `UIWinSettings`（数值顺序 = `Update` 的先后）
            Check(d.UiTopWindowForTest() == BattleDriver.UIWinSettings,
                  "★ A6 设置窗 + 日志窗同时开 ⇒ 仍是 `UIWinSettings`（优先序 = 数值顺序，实得 " + d.UiTopWindowForTest() + "）"
                + "｜🧨 改 `UiTopWindow()` 的 if 顺序 / 改常量数值 ⇒ 红");
            log.Hide(); settings.Hide();
        }
        Check(d.UiTopWindowForTest() == BattleDriver.UIWinNone, "A6b 收尾：窗口全关 ⇒ 回到 `UIWinNone`");
    }

    // ==================================================================
    //  B · 战术卡落点含敌方督军格（B124-TACTDROP）
    // ==================================================================
    static void B_TacticDropOnWarlord(BattleDriver d)
    {
        var ctx = d.Ctx;
        int me = d.MyIndex, foe = 1 - me;
        var foeWarlord = ctx.Players[foe].Board[BoardSpec.WarlordSlot];
        Check(foeWarlord != null, "B1（前提）敌方督军格上有一个 `UnitState`"
              + "（督军格 = `BoardSpec.WarlordSlot` = " + BoardSpec.WarlordSlot + "）");
        if (foeWarlord == null) return;

        // 找一张「非单位 + targeted + 目标池含敌方督军」的卡 —— ⛔ 不手写卡名（卡池现读）
        CardDef def = null;
        var all = CardDatabase.Load();
        for (int i = 0; i < all.Count && def == null; i++)
        {
            var c = all[i];
            if (c == null || c.IsUnit || c.Targeted != 1) continue;
            var p = RuleCore.PlayTargetPool(ctx, me, c);
            if (p != null && p.Contains(foeWarlord)) def = c;
        }
        Check(def != null, "B2（前提）卡池里有「非单位 targeted 且目标池**含敌方督军**」的卡"
              + "（判据 = `RuleCore.PlayTargetPool`，⛔ 不是我们的常量）");
        if (def == null) return;

        // 落点格 = 两侧都空的那一格（承袭 `SimulateCardDroppedForTest` 的 `slot` 形参 = 「格号兜底」那条路）
        int empty = -1;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            if (s == BoardSpec.WarlordSlot) continue;
            if (ctx.Players[foe].Board[s] == null && ctx.Players[me].Board[s] == null) { empty = s; break; }
        }
        Check(empty >= 0, "B3（前提）棋盘上有一个两侧都空的格可作落点");
        if (empty < 0) return;

        // ---------- ① 灭自证先跑：指针下**什么都没有** ⇒ 进「等目标」态 ----------
        int idxA = AddToHand(d, def);
        Check(idxA >= 0, "B4（前提）这一手把卡加进了手牌（下标 " + idxA + "）");
        if (idxA < 0) return;
        d.SetDropPointerNoneForTest();
        d.SimulateCardDroppedForTest(idxA, empty);
        Check(d.AwaitingPlayTarget,
              "★ B5 指针下**没有卡** ⇒ 进「等目标」态（原版 `Update.c:635-673`）"
            + "｜🧨 把「指针下的卡」写成恒真（例如直接拿落点格号）⇒ 这里也会直接结算 ⇒ 红");
        Check(d.UiStateNow() == 4,
              "★ A7 非单位卡在等目标 ⇒ `UiStateNow()` = **4 (playingTargetedSpell)**（原版 `Update.c:1058-1061`），实得 "
            + d.UiStateNow() + "｜🧨 把这档删掉退回 9 ⇒ 红");
        d.SimulatePlayTargetPickForTest(-1, -1);      // 点空白 ⇒ 取消（牌留手里），别把等态留给下一段
        Check(!d.AwaitingPlayTarget, "B6（前提）等目标态已取消");

        // ---------- ② 被测：指针压在**敌方督军格**上 ⇒ 目标就是那位督军、当场结算 ----------
        int idxB = AddToHand(d, def);
        Check(idxB >= 0, "B7（前提）第二次把卡加进手牌（下标 " + idxB + "）");
        if (idxB < 0) return;
        int hand0 = ctx.Players[me].Hand.Count;
        d.SetDropPointerForTest(foe, BoardSpec.WarlordSlot);
        d.SimulateCardDroppedForTest(idxB, empty);
        Check(ReferenceEquals(d.LastPlayTargetForTest, foeWarlord),
              "★★ B8 松手时指针压在**敌方督军格** ⇒ 目标就是那位督军（原版 `Update.c:676-684`）"
            + "｜🧨 退回「落点格号 → `Board[slot]`」⇒ 这里是 null / 邻格那张 ⇒ 红（静默打错目标）");
        Check(!d.AwaitingPlayTarget,
              "★ B9 ……而且是**当场结算**、不进等目标态（比原版多要一次点击的那条差异消失）");
        Debug.Log(Prefix + "· B10（只记录、不计分）手牌数 " + hand0 + " → " + ctx.Players[me].Hand.Count
                + "（引擎收不收这一手取决于费用/回合，不在本条要验的范围；本条只验「目标选对了、且不再多问一次」）");
    }

    /// <summary>把一张卡塞进我方手牌并返回它的下标（费用抬高 —— 本组不验费用）。</summary>
    static int AddToHand(BattleDriver d, CardDef def)
    {
        var ctx = d.Ctx;
        int me = d.MyIndex;
        ctx.Players[me].Hand.Add(ctx.NewInstance(def));
        ctx.Players[me].Energy = 9;
        d.RefreshAll();
        var hand = ctx.Players[me].Hand;
        for (int i = 0; i < hand.Count; i++) if (ReferenceEquals(hand[i].Card, def)) return i;
        return -1;
    }

    // ==================================================================
    //  C · 源码闸（形状；⛔ 不用我们的常量反推原版）
    // ==================================================================
    static void C_SourceGate()
    {
        string path = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        Check(File.Exists(path), "C0（前提）源码文件在：" + path + "（⛔ 读不到就红，别改成跳过）");
        if (!File.Exists(path)) return;
        string src = StripComments(File.ReadAllText(path));

        // C1 —— `UiStateNow()` 里四个**新补的档**都在（4 / 5 / 12 / 13）
        int u0 = src.IndexOf("public int UiStateNow()", StringComparison.Ordinal);
        int u1 = src.IndexOf("bool DeployingNonUnit", StringComparison.Ordinal);
        Check(u0 >= 0 && u1 > u0, "C1a（前提）能切出 `UiStateNow()` 的方法体（" + u0 + " → " + u1 + "）");
        if (u0 >= 0 && u1 > u0)
        {
            string body = src.Substring(u0, u1 - u0);
            Check(body.IndexOf("? 4 : 9", StringComparison.Ordinal) >= 0,
                  "C1 ★ `UiStateNow()` 有 **4 (playingTargetedSpell)** 那一档（非单位卡等目标）"
                + "｜🧨 删掉它退回 9 ⇒ A7 同时红");
            Check(body.IndexOf("return 12;", StringComparison.Ordinal) >= 0,
                  "C1 ★ `UiStateNow()` 有 **12 (mulliganFinished)** 那一档（`_mulliganSettleViews`）");
            Check(body.IndexOf("? 13 : 9", StringComparison.Ordinal) >= 0,
                  "C1 ★ `UiStateNow()` 有 **13 (choosingAbilityTarget)** 那一档（技能已定、等目标）");
            Check(body.IndexOf("DeployingNonUnit ? 5 : 3", StringComparison.Ordinal) >= 0,
                  "C1 ★ `UiStateNow()` 有 **5 (playingNonTargetSpell)** 那一档（`DeployingNonUnit`）");
        }

        // C2 —— `ChatHotkeyUiState()` 只剩转发（同一份映射只能有一处）
        int c0 = src.IndexOf("int ChatHotkeyUiState()", StringComparison.Ordinal);
        Check(c0 >= 0 && src.IndexOf("return UiStateNow();", c0, StringComparison.Ordinal) > c0
              && src.IndexOf("return UiStateNow();", c0, StringComparison.Ordinal) - c0 < 80,
              "C2 ★ `ChatHotkeyUiState()` 只剩转发（`{ return UiStateNow(); }`）"
            + "｜🧨 把映射抄回去 ⇒ 两份同时存在 ⇒ 红");
        Check(CountOf(src, "if (Ctx == null) return 0;") == 1,
              "C2b ★ 全文件只有**一处** `if (Ctx == null) return 0;`（= `UiStateNow()` 的 inactive 档；"
            + "实得 " + CountOf(src, "if (Ctx == null) return 0;") + "）"
            + "｜🧨 第二份映射回来 ⇒ 红");

        // C3 —— `Update` 的窗口分支全部走 `UiTopWindow()`
        int v0 = src.IndexOf("void Update()", StringComparison.Ordinal);
        int v1 = src.IndexOf("bool TickMultiCards()", StringComparison.Ordinal);
        Check(v0 >= 0 && v1 > v0, "C3a（前提）能切出 `Update()` 的方法体");
        if (v0 >= 0 && v1 > v0)
        {
            string upd = src.Substring(v0, v1 - v0);
            int gated = CountOf(upd, "UiTopWindow() == UIWin");
            Check(gated >= 9,
                  "C3 ★ `Update()` 里至少 9 条窗口模态分支由 `UiTopWindow()` 把门（实得 " + gated + "）"
                + "｜🧨 把某一条改回裸 `Visible` / 直接调 tick ⇒ 计数掉 ⇒ 红");
            Check(CountOf(upd, "if (TickMultiCards()) return;") == 0
                  && CountOf(upd, "if (HandleMulligan())") == 0
                  && CountOf(upd, "if (HandleChoose())") == 0,
                  "C3c ★ 三条裸分支（`TickMultiCards()` / `HandleMulligan()` / `HandleChoose()`）**已无**不带门的写法");
        }

        // C4 —— ESC 开设置窗、结算出口、时间控制键都改读 `UiStateNow()`
        int s0 = src.IndexOf("bool HandleSettingsEsc()", StringComparison.Ordinal);
        int s1 = src.IndexOf("public bool TickSettingsEscForTest()", StringComparison.Ordinal);
        Check(s0 >= 0 && s1 > s0, "C4a（前提）能切出 `HandleSettingsEsc()`");
        if (s0 >= 0 && s1 > s0)
        {
            string body = src.Substring(s0, s1 - s0);
            Check(body.IndexOf("UiStateNow()", StringComparison.Ordinal) >= 0
                  && body.IndexOf("IsTargeting", StringComparison.Ordinal) < 0
                  && body.IndexOf("selector.Visible", StringComparison.Ordinal) < 0,
                  "C4 ★ `HandleSettingsEsc()` 的「选目标那几档」改从 `UiStateNow()` 取，且**不再**自己重判 `IsTargeting` / `selector.Visible`"
                + "｜🧨 把那两判写回来 ⇒ 红（两条轴又分叉）");
        }
        int e0 = src.IndexOf("public bool HandleEndBattleExit()", StringComparison.Ordinal);
        Check(e0 >= 0 && src.IndexOf("UiStateNow() != 8", e0, StringComparison.Ordinal) - e0 >= 0
              && src.IndexOf("UiStateNow() != 8", e0, StringComparison.Ordinal) - e0 < 200,
              "C5 ★ `HandleEndBattleExit()` 只活在 `postBattle(8)` 那一档");

        // C6 —— TACTDROP：非单位分支走 `DropPointerUnit`（⛔ 不是裸 `Board[slot]`）
        int t0 = src.IndexOf("bool TryBeginPlayTargetPick(", StringComparison.Ordinal);
        int t1 = src.IndexOf("static string SpellTargetSide(", StringComparison.Ordinal);
        Check(t0 >= 0 && t1 > t0, "C6a（前提）能切出 `TryBeginPlayTargetPick()` 的方法体");
        if (t0 >= 0 && t1 > t0)
        {
            string body = src.Substring(t0, t1 - t0);
            Check(body.IndexOf("DropPointerUnit(out", StringComparison.Ordinal) >= 0,
                  "C6 ★ 非单位卡的目标先问 `DropPointerUnit(out …)`（= 原版 `GetTargetInPos`：指针下那张卡）"
                + "｜🧨 退回「落点格号 → `Board[slot]`」⇒ B8 同时红");
            int a = body.IndexOf("DropPointerUnit(out", StringComparison.Ordinal);
            int b = body.IndexOf("Ctx.Players[owner4].Board[slot]", StringComparison.Ordinal);
            Check(b < 0 || b > a, "C6b ★ 格号那条只在 `DropPointerUnit` 之后（= 兜底），⛔ 不是首选路径");
        }
        int p0 = src.IndexOf("UnitState DropPointerUnit(out int owner, out int slot)", StringComparison.Ordinal);
        Check(p0 >= 0, "C7 ★ `DropPointerUnit` 存在（类尾新增）");
        if (p0 >= 0)
        {
            int p1 = src.IndexOf("public void SetDropPointerForTest", p0, StringComparison.Ordinal);
            string body = (p1 > p0) ? src.Substring(p0, p1 - p0) : src.Substring(p0);
            Check(body.IndexOf("HitSlot(_myUnits, world)", StringComparison.Ordinal) >= 0
                  && body.IndexOf("HitSlot(_foeUnits, world)", StringComparison.Ordinal) >= 0,
                  "C7b ★ `DropPointerUnit` 复用 `HitSlot`（⛔ 不另写一份命中）");
        }

        // C8 —— `CardInteraction` 记下松手点（结算延后 0.65 s，指针早就不在那一格）
        string hiPath = Path.Combine(Application.dataPath, "CardPresentation/Hand/CardInteraction.cs");
        Check(File.Exists(hiPath), "C8a（前提）源码文件在：" + hiPath);
        if (File.Exists(hiPath))
        {
            string hi = StripComments(File.ReadAllText(hiPath));
            Check(hi.IndexOf("LastDropWorld = world;", StringComparison.Ordinal) >= 0,
                  "C8 ★ `Release` 里把**松手那一刻**的指针点记下来（`LastDropWorld = world;`）"
                + "｜🧨 删掉 ⇒ `DropPointerUnit` 退回 `WorldPointer()`（另一个时刻的量）⇒ 真机上又会解到邻格");
        }
    }

    // ------------------------------------------------------------------
    static int CountOf(string s, string needle)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    /// <summary>去掉 `//` 行注释与 `/* */` 块注释（字符串里的 `//` 不处理 —— 本文件不靠它）。</summary>
    static string StripComments(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        bool line = false, block = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            char n = (i + 1 < s.Length) ? s[i + 1] : '\0';
            if (line) { if (c == '\n') { line = false; sb.Append(c); } continue; }
            if (block) { if (c == '*' && n == '/') { block = false; i++; } continue; }
            if (c == '/' && n == '/') { line = true; i++; continue; }
            if (c == '/' && n == '*') { block = true; i++; continue; }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
