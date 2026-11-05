// B146_W15_Assert.cs — 批次 **B146** · 执行代理 `W15`（S1 抓到的 `G-3`：**绿光不看 `UIstate`**）
//
// ============================ 它盯的是什么 ============================
//  原版 `CardScript__CanAttackNow.c:44-56`：
//      `if (this.isPlayer) { if (UIstate != 1 /*normalBattle*/ && UIstate != 6 /*minionAttacking*/) return false; }`
//  ⇒ ① 只有 **`UIstate ∈ {1, 6}`** 才放行；② **只对本机玩家自己的卡判**（`isPlayer`），敌方卡整段跳过。
//  本单 = **只做显示侧**：`BattleDriver.SyncBoard` / `RefreshCanActForView` 里「画不画绿光」带上这道闸。
//  ⛔ 引擎闸（`RuleCore.CanAttackNow` / `DeclareAttack`）**本单一句没碰**，归 `B146-C04-USTATE-GATE`。
//
// ============================ 分组 ============================
//  P 前提：拿到活 driver + 一局已 `Begin()` 的 `Ctx`。
//  A **同一张本机卡 · 两种 UI 档对照**：{1, 6} 亮 · {9,10,11,14,3} 灭 —— 这是本单的核心判别式；
//    只断「不亮」证明不了是 UI 档关的（`CanActNow` 的疲劳/眩晕/配额同样能让它灭）。
//  B 敌方卡：UI 档 = 9 时**照原规则亮**（闸不作用于敌方）。
//  C 结构：源码层钉住「闸只此一处」+「座位取 `_me`、⛔ 不是写死 0」+「每帧采样已接（且不主动重推视图）」。
//  D 复位：取消钉死 ⇒ 回到不加闸的基线（防把假状态留给后面的断言）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A2/A3：把 `GreenLightAllowsPlayerAct` 改成恒 `true` ⇒ A3..A7 红。
//  · A4：把 `== 6` 那一档删掉 ⇒ A2 红。
//  · B1：把座位判据写成 `return true;`（不判 `owner != _me`）⇒ B1 红（敌方也会被 9 档关掉）。
//  · B1b：把座位写死 `owner != 0` ⇒ 只在客机/非 0 座位上才红（本机单机 `_me == 0` ⇒ 不断言这一档，见 C3）。
//  · C1/C2：把显示侧的 `GreenLightAllowsPlayerAct` 从 `SyncBoard` / `RefreshCanActForView` 摘掉 ⇒ 红。
//  · C3：把 `owner != _me` 改成 `owner != 0` ⇒ 红。
//  · C4：把 `TickGreenLightUiState()` 从 `Update` 那句里删掉 ⇒ 红。
//
// ============================ 怎么接（宿主，调度台统一做）============================
//  这是**要一台活 driver** 的断言（同 `B141_W8_Assert`）：在 `Assets/CardPresentation/Editor/BattleScene.cs`
//  卸 driver **之前**那条聚合行上、**同一物理行**追加（⛔ 不新增行 ⇒ 账行号零漂移）：
//    `B146_W15_Assert.Run(drv); _pass += B146_W15_Assert.Pass; _fail += B146_W15_Assert.Fail;`
//  ⚠️ 本文件**未接宿主**（`BattleScene.cs` 是本波热点大文件，W15 不占它的写手位）⇒ 需调度台按上面一行接上。
using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B146_W15_Assert
{
    const string Prefix = "[B146W15] ";
    static int _ok, _bad;

    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    /// <summary>宿主入口：`d` 必须已经 `Begin()` 过一局（`Ctx != null`）。</summary>
    public static void Run(BattleDriver d)
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B146-W15：绿光 UIstate 显示闸（判据 CardScript__CanAttackNow.c:44-56）开始 ===");
        try
        {
            P_Precondition(d);
            if (d != null && d.Ctx != null) { A_MineSeatUiStateGate(d); B_FoeSeatNotGated(d); }
            C_Source();
        }
        catch (Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message
                       + "\n" + e.StackTrace);
        }
        finally
        {
            if (d != null) d.SetGreenLightUiStateForTest(-1);   // 钉死口一定清干净（批处理回到「无闸」基线）
        }
        Debug.Log(Prefix + "B146_W15 合计：✓ " + _ok + " / ✗ " + _bad + "（共 " + (_ok + _bad) + " 条）");
    }

    // ==================================================================
    //  P · 前提
    // ==================================================================
    static void P_Precondition(BattleDriver d)
    {
        Check(d != null, "P0（前提）拿到活 driver —— 否则整组验不到");
        if (d == null) return;
        Check(d.Ctx != null, "P0b（前提）driver 已经 `Begin()` 过一局（`Ctx != null`）");
        if (d.Ctx == null) return;
        Check(d.MyIndex == 0 || d.MyIndex == 1, "P1 本机座位 `MyIndex` ∈ {0,1}（实得 " + d.MyIndex + "）");
        Check(d.GreenLightUiStateForTest >= 0 && d.GreenLightUiStateForTest <= 14,
              "P2 绿光稳定档落在原版 15 档枚举 0…14（实得 " + d.GreenLightUiStateForTest + "）");
    }

    // ==================================================================
    //  A · 同一张**本机卡**，换 UI 档 ⇒ 绿光亮/灭对照
    // ==================================================================
    static void A_MineSeatUiStateGate(BattleDriver d)
    {
        int my = d.MyIndex;
        int svWinner = d.Ctx.Winner, svActive = d.Ctx.Active;
        int slot = -1; UnitState u = null;
        // 还原用的现场
        bool svEx = false; int svStun = 0, svAtk = 0, svA = 0, svR = 0;
        d.Ctx.Winner = 0;
        d.Ctx.Active = my;
        d.SetGreenLightUiStateForTest(1);                     // 先把闸放到「全放行」档，造夹具时不受它干扰
        try
        {
            var pl = d.Ctx.Players[my];
            for (int s = 0; s < pl.Board.Length && slot < 0; s++)
            {
                var x = pl.Board[s];
                if (x == null || x.IsRemnant) continue;
                svEx = x.Exhausted; svStun = x.KwValue(KeywordTable.Stun); svAtk = x.AttacksThisTurn;
                svA = x.AttackBase; svR = x.RangedAttackBase;
                x.Exhausted = false; x.RemoveAll(KeywordTable.Stun); x.AttacksThisTurn = 0;
                x.AttackBase = Mathf.Max(1, x.AttackBase); x.RangedAttackBase = Mathf.Max(1, x.RangedAttackBase);
                if (RuleCore.CanActNow(d.Ctx, my, s)) { slot = s; u = x; }
                else
                {
                    x.Exhausted = svEx; RestoreStun(x, svStun); x.AttacksThisTurn = svAtk;
                    x.AttackBase = svA; x.RangedAttackBase = svR;
                }
            }
            Check(slot >= 0, "A0（前提）本机侧存在一张可以造出「能行动」的场上卡 —— 找不到就整组验不到（⛔ 不是通过）");
            if (slot < 0) return;

            var v = d.BoardViewAt(slot, true);
            Check(v != null, "A0b（前提）那一格有场上视图（`BoardViewAt` 非 null）");
            if (v == null) return;

            // ---- {1,6} ⇒ 亮（原版白名单两档）----
            d.SetGreenLightUiStateForTest(1); d.RefreshAll();
            bool lit1 = v.CanActVisible;
            Check(lit1, $"A1 UI 档 = {1}（normalBattle）⇒ 本机第 {slot} 格（{u.Name}）绿光**亮**"
                      + "｜🧨 把 `GreenLightAllowsPlayerAct` 改成恒 false ⇒ 红");
            d.SetGreenLightUiStateForTest(6); d.RefreshAll();
            bool lit6 = v.CanActVisible;
            Check(lit6, $"A2 UI 档 = {6}（minionAttacking）⇒ 仍然**亮**（原版白名单的另一档）"
                      + "｜🧨 把 `== 6` 那一档删掉 ⇒ 红");

            // ---- 其余档 ⇒ 灭（同一张卡，只换 UI 档 ⇒ 差异只能来自这道闸）----
            int[] offs = { 9, 10, 14, 11, 3 };                 // 9 choosingUnitTarget · 10 targetedActiveAbility · 14 choosingResolutionCard · 11 mulligan · 3 playingMinion
            for (int i = 0; i < offs.Length; i++)
            {
                d.SetGreenLightUiStateForTest(offs[i]); d.RefreshAll();
                bool lit = v.CanActVisible;
                Check(!lit, $"A{3 + i} UI 档 = {offs[i]} ⇒ 本机第 {slot} 格（{u.Name}）绿光**灭**"
                          + $"（同一张卡、`CanActNow` 仍为真：{RuleCore.CanActNow(d.Ctx, my, slot)}）"
                          + "｜🧨 闸改成恒 true ⇒ 红；🧨 只断「灭」而 `CanActNow` 本来就假 ⇒ 这一句会把那种假绿一起抓出来");
            }

            // ---- 灭自证：判据是**档**，不是「一律灭」----
            d.SetGreenLightUiStateForTest(1); d.RefreshAll();
            Check(v.CanActVisible, "A8（灭自证）钉死口回到 1 ⇒ 又亮（证明 A3…A7 的「灭」是档造成的，不是一次性的状态污染）");
        }
        finally
        {
            if (u != null)
            {
                u.Exhausted = svEx; RestoreStun(u, svStun); u.AttacksThisTurn = svAtk;
                u.AttackBase = svA; u.RangedAttackBase = svR;
            }
            d.Ctx.Winner = svWinner; d.Ctx.Active = svActive;
            d.SetGreenLightUiStateForTest(-1);
            d.RefreshAll();
        }
    }

    // ==================================================================
    //  B · **敌方卡**不受这道闸（原版同一句写在 `if (this.isPlayer)` 里面）
    // ==================================================================
    static void B_FoeSeatNotGated(BattleDriver d)
    {
        int foe = 1 - d.MyIndex;
        int svWinner = d.Ctx.Winner, svActive = d.Ctx.Active;
        int slot = -1; UnitState u = null;
        bool svEx = false; int svStun = 0, svAtk = 0, svA = 0, svR = 0;
        d.Ctx.Winner = 0;
        d.Ctx.Active = foe;                                    // 让敌方那一排「能行动」（`CanActNow` 内含 `p != Active ⇒ 假`）
        d.SetGreenLightUiStateForTest(1);
        try
        {
            var pl = d.Ctx.Players[foe];
            for (int s = 0; s < pl.Board.Length && slot < 0; s++)
            {
                var x = pl.Board[s];
                if (x == null || x.IsRemnant) continue;
                svEx = x.Exhausted; svStun = x.KwValue(KeywordTable.Stun); svAtk = x.AttacksThisTurn;
                svA = x.AttackBase; svR = x.RangedAttackBase;
                x.Exhausted = false; x.RemoveAll(KeywordTable.Stun); x.AttacksThisTurn = 0;
                x.AttackBase = Mathf.Max(1, x.AttackBase); x.RangedAttackBase = Mathf.Max(1, x.RangedAttackBase);
                if (RuleCore.CanActNow(d.Ctx, foe, s)) { slot = s; u = x; }
                else
                {
                    x.Exhausted = svEx; RestoreStun(x, svStun); x.AttacksThisTurn = svAtk;
                    x.AttackBase = svA; x.RangedAttackBase = svR;
                }
            }
            Check(slot >= 0, "B0（前提）敌方侧存在一张可以造出「能行动」的场上卡");
            if (slot < 0) return;

            var v = d.BoardViewAt(slot, false);
            Check(v != null, "B0b（前提）那一格有敌方场上视图");
            if (v == null) return;

            d.SetGreenLightUiStateForTest(9); d.RefreshAll();
            Check(v.CanActVisible, $"B1 UI 档 = 9 时**敌方**第 {slot} 格（{u.Name}）绿光照原规则**亮**"
                                 + "（原版整段在 `if (this.isPlayer)` 里 ⇒ 敌方跳过）"
                                 + "｜🧨 把座位判据删成 `return false;` / 不判 `owner != _me` ⇒ 红");

            Check(d.GreenLightAllowsPlayerActForTest(foe), "B2（读口）`GreenLightAllowsPlayerAct(敌方座位)` = true");
            Check(!d.GreenLightAllowsPlayerActForTest(d.MyIndex), "B3（读口）同一档下**本机座位** = false（两侧不同 ⇒ 闸真的按座位分）");
        }
        finally
        {
            if (u != null)
            {
                u.Exhausted = svEx; RestoreStun(u, svStun); u.AttacksThisTurn = svAtk;
                u.AttackBase = svA; u.RangedAttackBase = svR;
            }
            d.Ctx.Winner = svWinner; d.Ctx.Active = svActive;
            d.SetGreenLightUiStateForTest(-1);
            d.RefreshAll();
        }
    }

    static void RestoreStun(UnitState u, int layers)
    {
        u.RemoveAll(KeywordTable.Stun);
        if (layers > 0) u.AddKeyword(KeywordTable.Stun, layers);
    }

    // ==================================================================
    //  C · 结构（源码层，证明「判据只此一处 + 座位不是写死的 0 + 每帧 tick 已接」）
    // ==================================================================
    static void C_Source()
    {
        string path = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        string src = File.Exists(path) ? File.ReadAllText(path) : "";
        Check(src.Length > 0, "C0（前提）读得到 `CardPresentation/Battle/BattleDriver.cs`（路径：" + path + "）");
        if (src.Length == 0) return;

        int nSync = CountOf(src, "&& GreenLightAllowsPlayerAct(owner)");
        int nRefr = CountOf(src, "&& GreenLightAllowsPlayerAct(p)");
        Check(nSync == 1, $"C1 `SyncBoard` 那一路接上了显示闸（`&& GreenLightAllowsPlayerAct(owner)` 命中 {nSync} 处，应为 1）"
                        + "｜🧨 摘掉 ⇒ 红");
        Check(nRefr == 1, $"C2 `RefreshCanActForView` 那一路也接上了（`&& GreenLightAllowsPlayerAct(p)` 命中 {nRefr} 处，应为 1 —— 漏这一处 ⇒ 登场演完补推那一下会把绿光又点亮）"
                        + "｜🧨 摘掉 ⇒ 红");
        Check(src.Contains("if (owner != _me) return true;"),
              "C3 座位判据 = **`owner != _me`**（⛔ 不是写死 0：P2P 客机 `_me == 1`；本机单机 `_me == 0`，这一条是本组**唯一**能钉住它的地方）"
            + "｜🧨 改成 `owner != 0` / 删掉 ⇒ 红");
        Check(src.Contains("return st == 1 || st == 6;"),
              "C4 白名单 = **{1 normalBattle, 6 minionAttacking}**（原版 `CanAttackNow.c:44-56` 两档）"
            + "｜🧨 多放一档（比如 10）/ 少放 6 ⇒ 红");
        Check(src.Contains("TickHudButtonFeedback(WorldPointer()); TickGreenLightUiState();"),
              "C5 每帧 tick 已接在 `Update`（`TickHudButtonFeedback` 同一句）—— 采样稳定档，**不主动重推视图**（原版 `ActivateMinion` 是事件驱动的，"
            + "⛔ 换档就刷会让「一按住手牌整排绿光灭」）"
            + "｜🧨 删掉这一句 ⇒ 稳定档永远停在 1 ⇒ 闸在生产/真机上等于没接 ⇒ 红");
        Check(src.Contains("if (raw == 3 || raw == 5) return;"),
              "C6 演出档（3 playingMinion / 5 playingNonTargetSpell）保持上一稳定档 —— 防「演出中整排绿光被关掉、且回不来」"
            + "｜🧨 删掉 ⇒ 红");
    }

    static int CountOf(string s, string needle)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }
}
