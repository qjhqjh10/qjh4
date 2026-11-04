// B144_W3_B8_Assert.cs — `B144-W3` · **物理审计 B8：回合开始「清 `Prayed`」的逐卡位置**（只读源码切片，零前提）
//
// ============================ 盯的是什么 ============================
//  原版 `CardScript__OnTurnStart.c` 是**按卡逐个**跑的一支函数（`BattleManagerSupport__BroadcastTurnStart.c:20-26`
//  对 `CardsInPlay` 逐张调），卡内次序（逐行现读，第一权威 = `d:/2/tools/decomp_full/`）：
//      `:85` 清召唤病 → **`:99` ActivateMinion（解疲劳）→ `:100` `*(param_1+0x67)=0`（清 `Prayed`）**
//      → `:102-109` 开舱（`dropPod`，末了 `:263-266` 发 `Landing`) → `:110-118` stealth 到期
//      → `:119-126` 晕/瞎闸门 → `:130-137` 待死闸 → `:137+` TurnStart 触发。
//
//  我们改前：`RuleCore.BeginTurn` 先把**整圈**主动方单位解疲劳 + 开舱 + stealth 到期，**再**另起一圈
//  把双方的 `Prayed` 整段清掉 ⇒ 每张卡的**开舱 Landing 触发**都还能读到「自己在祈祷」（与 `:100` 在 `:102` 之前相反）。
//  2026-11-05（`B144-W3`）把主动方那半搬进逐卡圈、紧跟 `RefreshForNewTurn`（= `:99` 之后），对手方那半保持原处。
//  落点 = `RuleEngine/Core/RuleCore.cs:2129`（同一物理行追加 ⇒ 行数零变化）。
//
// ============================ 怎么接 ============================
//  `B144_W3_B8_Assert.Run();`（静态类 + 无参 `Run()`；全局命名空间、类名 == 文件名）
//  ⚠️ 本文件**零前提**：只读源码文本，不建对象、不碰存档、不跑引擎。
//  ⚠️ 宿主接线**不在本写手白名单**（`Editor/*Scene.cs` 与 `RuleEngineTest.cs` 都不可改）⇒ 待调度台接线。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：把 `u.Prayed = false;` 从 `u.RefreshForNewTurn();` 那一行**删回**下面那一圈 ⇒ A1 红。
//   · A2：把它挪到**同一圈里 `OpenDropPod` 之后** ⇒ A1 仍绿、**只有 A2 红**（两格各钉一件事，不同时变）。
//   · A4：把「对手方那半」的 `pu.Prayed = false;` 整句删掉 ⇒ A4 红（对手祈祷过的单位会永远挂着）。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class B144_W3_B8_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "[B144W3B8]";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    public static void Run()
    {
        _pass = _fail = 0;

        const string RC = "RuleEngine/Core/RuleCore.cs";
        var lines = LiveLines(RC);
        Check(lines.Count > 0, "A0（前提）读得到 `" + RC + "` 的源码（读不到时下面全是空判）");
        if (lines.Count == 0) { Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败"); return; }

        int iRefresh = IndexOfBoth(lines, "u.RefreshForNewTurn();", "u.Prayed = false;");
        int iLoop    = LastIndexOfBefore(lines, "for (int s = 0; s < BoardSpec.Size; s++)", iRefresh);
        int iDropPod = IndexOfAfter(lines, "OpenDropPod(ctx, ctx.Active, s, u)", iRefresh);
        int iStealth = IndexOfAfter(lines, "if (u.Has(KeywordTable.Stealth))", iRefresh);
        int iEnemy   = IndexOf(lines, "pu.Prayed = false;");

        // ---- A1：清 `Prayed` 就贴在 `RefreshForNewTurn`（= 原版 `:99` 解疲劳）那一行上 ----
        Check(iRefresh >= 0,
              "★ A1 逐卡圈里**同一物理行**同时有 `u.RefreshForNewTurn();` 与 `u.Prayed = false;`"
            + "（= 原版 `:99 ActivateMinion` → `:100 +0x67=0` 的相邻两行，落在**每一张卡**上）"
            + "｜🧨 把 `u.Prayed = false;` 删回下面那一圈 ⇒ 本条红");

        // ---- A2：它排在【开舱】与【stealth 到期】之前（`:100` 在 `:102` / `:110` 之前）----
        Check(iRefresh >= 0 && iDropPod > iRefresh && iStealth > iRefresh,
              "★ A2 清 `Prayed` 排在**开舱 `OpenDropPod` 与 stealth 到期之前**（原版 `:100` < `:102` < `:110`）"
            + "｜🧨 把它挪到同一圈 `OpenDropPod` 之后 ⇒ **A1 仍绿、只本条红**（两格各钉一件事）");

        // ---- A2b：逐卡圈（不是整段循环）—— 它必须落在**紧邻上方**那个 `for (int s = 0; …)` 里 ----
        Check(iLoop >= 0 && iRefresh > iLoop && iRefresh - iLoop < 12,
              "（前提）A2 钉的那一行落在**紧邻上方**的 `for (int s = 0; s < BoardSpec.Size; s++)` 圈里"
            + "（距圈头 " + (iLoop >= 0 ? (iRefresh - iLoop).ToString() : "?") + " 行 —— 超出 12 行说明钉错了圈）");

        // ---- A4：对手方那半**没被删**（那一圈依旧双方全清）----
        Check(iEnemy >= 0 && iEnemy > iDropPod,
              "★ A4 对手方那半仍在（`pu.Prayed = false;` 在逐卡圈**之后**、对双方扫）"
            + " —— 原版 `:100` 对**全场每一张在场卡**各来一次，我们这一半靠这圈补"
            + "｜🧨 删掉这句 ⇒ 对手祈祷过的单位永远挂着（`Devout Serenity` / `Sororitas Rhino` 会多治）");

        Debug.Log(Tag + " 合计：" + _pass + " 通过 / " + _fail + " 失败");
    }

    // ---- 读源码（**只留活代码行**：`//` 开头的整行注释不算 —— 留痕注释里提到名字是故意的）----
    static List<string> LiveLines(string relPath)
    {
        var path = Path.Combine(Application.dataPath, relPath);
        var list = new List<string>();
        if (!File.Exists(path)) return list;
        foreach (var l0 in File.ReadAllLines(path))
            if (!l0.TrimStart().StartsWith("//")) list.Add(l0);
        return list;
    }

    static int IndexOf(List<string> lines, string must)
    {
        for (int i = 0; i < lines.Count; i++) if (lines[i].Contains(must)) return i;
        return -1;
    }

    static int IndexOfBoth(List<string> lines, string a, string b)
    {
        for (int i = 0; i < lines.Count; i++) if (lines[i].Contains(a) && lines[i].Contains(b)) return i;
        return -1;
    }

    /// <summary>从 `before` 往上找第一条含 `must` 的行（`before` 已由调用方保证是有效下标）。</summary>
    static int LastIndexOfBefore(List<string> lines, string must, int before)
    {
        if (before < 0) return -1;
        for (int i = Math.Min(before - 1, lines.Count - 1); i >= 0; i--) if (lines[i].Contains(must)) return i;
        return -1;
    }

    /// <summary>从 `after` 往下找第一条含 `must` 的行。</summary>
    static int IndexOfAfter(List<string> lines, string must, int after)
    {
        for (int i = Math.Max(after + 1, 0); i < lines.Count; i++) if (lines[i].Contains(must)) return i;
        return -1;
    }
}
