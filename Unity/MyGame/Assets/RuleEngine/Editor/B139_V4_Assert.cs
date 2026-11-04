// B139_V4_Assert.cs — 执行代理 `B139_V4`（2026-11-04 · 批次 `B139` · 文件所有权 = `RuleEngine/Data/DeckStore.cs`）
//
// ============================ 它盯的是什么 ============================
//  缺陷（调度台 2026-11-04 现读）：`C:\warpforgesave\WarpforgeDecks.json` mtime **2026-11-04 12:32**、
//  内容 `{version:1, current:-1, decks:[]}` —— **玩家真存档被自检写成了空表**
//  （同目录 `WarpforgeCardLevels.json` / `WarpforgeRanked/` 也是自检写的，那两处不在本件白名单里）。
//  根因：`DeckStore.Path` = `OverridePath ?? 玩家真路径`，**`SaveAll` 没有批处理闸**（`SaveCenter.StoreOn` 有）；
//  而各场景宿主（`DeckScene` / `ShellScene` / `CollectionScene` / `MainMenuScene` / `BattleScene`）都在 `finally` 里把
//  `OverridePath` **还原成 `null`**（`DeckScene.cs:1151` 那段注释自己就写着「那之后落的都是玩家的真存档」）
//  ⇒ 之后任何 `Save()` / `Delete()` 落的都是玩家真档。
//
// ============================ 修法（⛔ 不是「拒绝写」）============================
//  `DeckStore.Path` 的第二档改成 `BatchSafePath()`：**批处理（`-batchmode`）且没注入 `OverridePath` ⇒ 落临时沙箱**
//  （`Application.temporaryCachePath/wfcheck_save/<一次进程一个 tag>/WarpforgeDecks.json`），并 `Debug.Log` 出声写明落点。
//  ⛔ 拒绝写会让 `CardPresentation/Editor/B102DECK_Assert.cs:191` 与 `RuleEngine/Editor/DeckRulesTest.cs:251`
//  （都直接调 `SaveAll` 并断「写成功」）变红 ⇒ 改道 = 行为不变、只搬落点。
//  读也一起改道（`Path` 是读写共用的一档）⇒ 批处理里 `DeckLibrary.Load` 读的也是沙箱，
//  ⛔ 不会再把**玩家真卡组**当夹具读进来。
//
// ============================ 判据 / 覆盖边界 ============================
//  · 本闸是 **🔶 自己设计（非原版）**：原版没有自检宿主（卡组存在服务端 `CardDeck.syncedToServer`），
//    「自检别写玩家真档」这条判据在原版里根本不存在 ⇒ 判据只能是我们定的。裁定 = 调度台 2026-11-04。
//  · 本件只验 `RuleEngine/Data/DeckStore.cs` 这一条腿。**同一个病在别的存档类里可能还在**（本件白名单外，
//    如实记在报告里，⛔ 不在此处改）。
//  · 真根的「没被碰」是**文件三样（存在性 / 长度 / mtime）**比出来的，不是「逻辑上不会写」——
//    两个候选路径（`SaveRoot.PrimaryRoot` / `%USERPROFILE%\warpforgesave`）都核，且**只算字符串、不解析**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1–A5：把 `DeckStore.Path` 的第二档改回 `Path.Combine(SaveRoot.Path, FileName)` ⇒ A1/A2/A5 红。
//  · A6–A9：把闸改成「批处理下直接 `return false`」⇒ A6 红（这正是被否的那条路）。
//  · A10/A12/D1：让 `SaveAll` 在批处理下仍落真根 ⇒ 三样之一变 ⇒ 红。
//  · ★ B2 / B6：把 `Path` 改成「批处理一律返回沙箱（不看 `OverridePath`）」⇒ B2 红；
//    把闸改成「沙箱路径无条件生效」⇒ B6 红（那次写会误写沙箱）。
//  · ★ C1 / C3：把闸改成「无条件改道」⇒ 非批处理下 `Path` 不再是 `SaveRoot.Path` ⇒ C1/C3 红。
using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;
using UnityEngine;
using IO = System.IO;

/// <summary>执行代理 `B139_V4`：自检/批处理**不写玩家真存档根**（`DeckStore` 的批处理沙箱）。</summary>
public static class B139_V4_Assert
{
    const string P = "[B139V4] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "✓ " + msg); }
        else { Fail++; Debug.LogError(P + "✗ " + msg); }
        return cond;
    }

    /// <summary>一份文件的「三样」：在不在 / 多长 / 最后写时间（UTC）。</summary>
    struct Snap { public bool Exists; public long Len; public DateTime Mtime; }

    static Snap Take(string p)
    {
        try
        {
            var fi = new FileInfo(p);
            if (!fi.Exists) return new Snap { Exists = false, Len = -1, Mtime = DateTime.MinValue };
            return new Snap { Exists = true, Len = fi.Length, Mtime = fi.LastWriteTimeUtc };
        }
        catch { return new Snap { Exists = false, Len = -1, Mtime = DateTime.MinValue }; }
    }

    static string Show(Snap s)
    {
        return s.Exists ? ("存在 / " + s.Len + " B / " + s.Mtime.ToString("O")) : "不存在";
    }

    static bool Same(Snap a, Snap b)
    {
        return a.Exists == b.Exists && a.Len == b.Len && a.Mtime == b.Mtime;
    }

    /// <summary>玩家真存档的**两个候选**路径（`SaveRoot` 的 ② 首选 / ③ 回落）。
    /// 🔴 只算字符串 —— 本方法**不碰 `SaveRoot.Path`**（那个 getter 会 `Resolve()`：建目录 + 写探针再删）。</summary>
    static List<string> PlayerCandidates()
    {
        var list = new List<string>();
        list.Add(IO.Path.Combine(CardPresentation.SaveRoot.PrimaryRoot, DeckStore.FileName));
        try
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(user))
                list.Add(IO.Path.Combine(IO.Path.Combine(user, CardPresentation.SaveRoot.FallbackDirName), DeckStore.FileName));
        }
        catch { /* 拿不到 %USERPROFILE% 就只核首选那条 */ }
        return list;
    }

    /// <summary>两个候选路径的「三样」与跑之前**逐个**比对。</summary>
    static bool RealUntouched(List<string> paths, List<Snap> before)
    {
        if (paths.Count != before.Count) return false;
        for (int i = 0; i < paths.Count; i++)
        {
            var now = Take(paths[i]);
            if (!Same(now, before[i]))
            {
                Debug.LogError(P + "   ↳ 真根候选变了：" + paths[i]
                               + "（" + Show(before[i]) + " ⇒ " + Show(now) + "）");
                return false;
            }
        }
        return true;
    }

    public static void Run()
    {
        Pass = 0; Fail = 0;

        string keepOverride = DeckStore.OverridePath;
        Func<bool> keepProbe = DeckStore.BatchModeProbe;
        string keepSaveRoot = CardPresentation.SaveRoot.Override;
        bool saveRootTouched = false;

        var realPaths = PlayerCandidates();
        var realBefore = new List<Snap>();
        foreach (var p in realPaths) realBefore.Add(Take(p));

        string sandbox = null;
        string explicitPath = null;
        string fakeRoot = null;

        try
        {
            // ──────────────────────────────────────────────────────────────────
            //  §A 批处理 + 没注入 `OverridePath` ⇒ 落沙箱、真根零触碰
            // ──────────────────────────────────────────────────────────────────
            Debug.Log(P + "===== §A 批处理 + 无 `OverridePath` ⇒ 沙箱 =====");
            DeckStore.OverridePath = null;
            DeckStore.BatchModeProbe = () => true;

            sandbox = DeckStore.Path;
            Debug.Log(P + "  沙箱实际落点 = " + (sandbox ?? "<null>"));
            Debug.Log(P + "  玩家真根候选 = " + string.Join(" · ", realPaths.ToArray()));

            string temp = Application.temporaryCachePath;
            Ok(!string.IsNullOrEmpty(sandbox) && sandbox.StartsWith(temp, StringComparison.OrdinalIgnoreCase),
               "A1 批处理 ⇒ `DeckStore.Path` 在 `Application.temporaryCachePath` 下（" + temp + "）");
            Ok(!string.IsNullOrEmpty(sandbox)
               && sandbox.Replace('\\', '/').IndexOf("/" + DeckStore.BatchSandboxDirName + "/", StringComparison.OrdinalIgnoreCase) >= 0,
               "A2 落在 `" + DeckStore.BatchSandboxDirName + "` 那一层（⛔ 不是玩家真根）");
            Ok(!string.IsNullOrEmpty(sandbox) && sandbox.EndsWith(DeckStore.FileName, StringComparison.Ordinal),
               "A3 文件名不变（`" + DeckStore.FileName + "`）");
            Ok(string.Equals(DeckStore.Path, sandbox, StringComparison.Ordinal),
               "A4 连读两次同一落点（沙箱稳定 —— 不来回新建、也不抖到真根）");

            bool hitsReal = false;
            foreach (var p in realPaths)
                if (string.Equals(p, sandbox, StringComparison.OrdinalIgnoreCase)) hitsReal = true;
            Ok(!hitsReal, "A5 沙箱路径 ≠ 玩家真根任一候选｜🧨 第二档改回 `Path.Combine(SaveRoot.Path, FileName)` ⇒ 本行红");

            var decks = new List<PlayerDeck>
            {
                new PlayerDeck("B139沙箱", "Anvirr Keltoc", "Aspect Shrine", new List<string> { "Autarch" }),
            };
            string err;
            Ok(DeckStore.SaveAll(decks, 0, out err),
               "A6 批处理下 `SaveAll` 仍然**写成功**（err=" + (err ?? "null") + "）｜🧨 改成「批处理直接拒绝写」⇒ 本行红");
            Ok(!string.IsNullOrEmpty(sandbox) && File.Exists(sandbox),
               "A7 沙箱文件真的落盘（" + sandbox + "）");

            DeckStore.LoadNote note; string detail; int cur;
            var back = DeckStore.LoadAll(out note, out detail, out cur);
            Ok(back.Count == 1 && back[0] != null && string.Equals(back[0].Name, "B139沙箱", StringComparison.Ordinal),
               "A8 读回来的就是沙箱那一份（1 套 / 名字对；实际 " + back.Count + " 套）");
            Ok(note == DeckStore.LoadNote.None, "A9 读档码 = `None`（detail=" + (detail ?? "null") + "）");

            Ok(RealUntouched(realPaths, realBefore),
               "★ A10 玩家真根（两个候选）**存在性 / 长度 / mtime 三样全没变** —— 这一趟写的是沙箱");

            DeckStore.DeleteFile();
            Ok(string.IsNullOrEmpty(sandbox) || !File.Exists(sandbox),
               "A11 `DeleteFile()` 删的也是沙箱那一份（⛔ 不删真档）");
            Ok(RealUntouched(realPaths, realBefore),
               "★ A12 ……而玩家真根**仍然**一模一样（删也删不到它）");

            // ──────────────────────────────────────────────────────────────────
            //  §B 灭自证：显式 `OverridePath` 必须**赢过**沙箱
            // ──────────────────────────────────────────────────────────────────
            Debug.Log(P + "===== §B 灭自证：显式 `OverridePath` 赢过沙箱 =====");
            string bDir = IO.Path.Combine(temp, DeckStore.BatchSandboxDirName + "_explicit");
            Directory.CreateDirectory(bDir);
            explicitPath = IO.Path.Combine(bDir, "explicit_decks.json");
            try { if (File.Exists(explicitPath)) File.Delete(explicitPath); } catch { }

            // 对照：先往沙箱写一份（A11 刚把它删了），后面要证明 B 组那一次写**没动沙箱**
            Ok(DeckStore.SaveAll(decks, 0, out err), "B1 对照：再往沙箱写一份（err=" + (err ?? "null") + "）");
            var sandboxSnap = Take(sandbox);
            Ok(sandboxSnap.Exists, "B2 对照沙箱那一份在（" + Show(sandboxSnap) + "）");

            DeckStore.OverridePath = explicitPath;          // ← 显式注入，仍然处在「批处理」判据下
            Ok(string.Equals(DeckStore.Path, explicitPath, StringComparison.Ordinal),
               "★ B3 **显式注入 ⇒ `Path` 就是它**（⛔ 没被沙箱抢走）｜🧨 把 `Path` 改成「批处理一律返回沙箱」⇒ 本行红");

            var two = new List<PlayerDeck>
            {
                decks[0],
                new PlayerDeck("第二套", "Ghazghkull Thraka", null, new List<string>()),
            };
            Ok(DeckStore.SaveAll(two, 1, out err), "B4 写显式路径成功（err=" + (err ?? "null") + "）");
            Ok(File.Exists(explicitPath), "B5 文件落在显式路径上（" + explicitPath + "）");
            var exBack = DeckStore.LoadAll();
            Ok(exBack.Count == 2, "B6 读回来 2 套（实际 " + exBack.Count + "）");
            Ok(Same(Take(sandbox), sandboxSnap),
               "★ B7 这一次写**没动沙箱**（" + Show(sandboxSnap) + " ⇒ " + Show(Take(sandbox))
               + "）｜🧨 把关改成「无条件沙箱」⇒ 本行红 —— 证明闸**只在「没注入」时**才改道");

            // ──────────────────────────────────────────────────────────────────
            //  §C 非批处理 ⇒ 照旧走 `SaveRoot.Path`（闸不误伤正式运行）
            //     ⚠️ 用 `SaveRoot.Override` 指一个**假根**（只改字符串档位 ⇒ 不解析、不写盘），
            //        ⛔ 绝不拿玩家真根当这一组的落点。
            // ──────────────────────────────────────────────────────────────────
            Debug.Log(P + "===== §C 非批处理 ⇒ 照旧 `SaveRoot.Path`（闸不误伤正式运行）=====");
            DeckStore.OverridePath = null;
            DeckStore.BatchModeProbe = () => false;

            fakeRoot = IO.Path.GetFullPath(IO.Path.Combine(Application.dataPath, "../Temp/_b139_v4_fakeroot"));
            Directory.CreateDirectory(fakeRoot);
            saveRootTouched = true;
            CardPresentation.SaveRoot.Override = fakeRoot;

            string expect = IO.Path.Combine(fakeRoot, DeckStore.FileName);
            Ok(string.Equals(DeckStore.Path, expect, StringComparison.Ordinal),
               "★ C1 非批处理 ⇒ `Path` = `SaveRoot.Path` 拼文件名（期望 " + expect + "）｜🧨 改成「无条件沙箱」⇒ 本行红");
            Ok(DeckStore.SaveAll(two, 1, out err), "C2 非批处理下写成功（err=" + (err ?? "null") + "）");
            Ok(File.Exists(expect), "C3 落在 `SaveRoot` 那一档上（" + expect + "）");
            var cBack = DeckStore.LoadAll();
            Ok(cBack.Count == 2, "C4 读回来 2 套（实际 " + cBack.Count + "）");
            Ok(RealUntouched(realPaths, realBefore),
               "★ C5 ……这一组（假根）也没碰到玩家真根的两个候选");
        }
        catch (Exception e)
        {
            Fail++;
            Debug.LogError(P + "✗ 断言过程抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            DeckStore.OverridePath = keepOverride;
            DeckStore.BatchModeProbe = keepProbe;
            if (saveRootTouched) CardPresentation.SaveRoot.Override = keepSaveRoot;

            try { if (explicitPath != null && File.Exists(explicitPath)) File.Delete(explicitPath); } catch { }
            try { if (sandbox != null && File.Exists(sandbox)) File.Delete(sandbox); } catch { }
            try { if (fakeRoot != null && Directory.Exists(fakeRoot)) Directory.Delete(fakeRoot, true); } catch { }

            Ok(RealUntouched(realPaths, realBefore),
               "★ D1 整段跑完（含收尾删除）之后，玩家真根的两个候选**仍然一模一样** —— 全程零触碰");

            Debug.Log(P + $"=== B139_V4 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }
}
