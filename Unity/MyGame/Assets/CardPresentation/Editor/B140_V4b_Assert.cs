// B140_V4b_Assert.cs — 存档写口沙箱的断言（`B139-V4b` 那 5 处「Unity 批处理下仍写玩家真根」）
//
// 被测：`CardPresentation/Core/SaveSandbox.cs` 的 `Dir()`，以及 5 个消费方
//   `CardProgress.SavePath`（`Shell/CardDetailPopup.cs`）· `RankedData.StorePath`（`Shell/RankedData.cs`）
//   · `ReplayStore.Dir`（`Battle/ReplayStore.cs`）· `NetConfig.Path`（`Net/NetConfig.cs`）
//   · `ProfileData.SavePath`（`Shell/ProfileData.cs`）
//
// 直接跑（类在**全局命名空间**、类名 == 文件名、不依赖任何宿主场景）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_V4b_Assert.Run -logFile -
//
// 判绿红：按**行首** `B140V4b ✓` / `B140V4b ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
//
// 四组：
//   A 批处理 + 无注入 ⇒ 5 处全部落在 `temporaryCachePath` 下、**没有一处**在 `SaveRoot.Path` 下；
//   B 非批处理 / 注入 Override ⇒ 路径回到 `SaveRoot.Path` / 注入值（证明助手**不是恒返沙箱**）；
//   C 🧨 灭自证：把助手拨回「恒返 `SaveRoot.Path`」（`SaveSandbox.DisableForTest`）⇒ **A 组那条判据必须翻红**；
//   D 5 处**真实保存各触发一次** ⇒ 真根那 5 个文件 / 录像目录**内容逐字节 + mtime 全不变**，
//      而且**沙箱里确实出现了新档**（否则「没变」是空判 —— 存都没存当然不变）。
//
// ⚠️ 期望值全部是**外部量**（`Application.temporaryCachePath` / `SaveRoot.Path` / 磁盘字节），
//   ⛔ 不用被测函数去算期望（那是同义反复）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using CardPresentation;
using CardPresentation.Net;

public static class B140_V4b_Assert
{
    const string Tag = "B140V4b";

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Section(string t) { Debug.Log("--- " + Tag + " · " + t + " ---"); }

    // ==================================================================
    //  5 个写口
    // ==================================================================
    sealed class Sink
    {
        public string Name;
        public Func<string> Path;
    }

    static Sink[] Sinks()
    {
        return new Sink[]
        {
            new Sink { Name = "CardProgress",  Path = () => CardPath() },
            new Sink { Name = "RankedData",    Path = () => RankedData.StorePath },
            new Sink { Name = "ReplayStore",   Path = () => ReplayStore.Dir },
            new Sink { Name = "NetConfig",     Path = () => NetConfig.Path },
            new Sink { Name = "ProfileData",   Path = () => ProfileData.SavePath },
        };
    }

    /// <summary>`CardProgress.SavePath` 是 **`private static`**（另 4 处本来就是公开属性）。
    /// 为了**不改 `CardDetailPopup.cs` 的类结构**（那个类在中段加/减行会让下游账落点整体漂移），
    /// 这里用反射**只读一个属性、不调任何方法**。Editor / Mono 下可用。</summary>
    static readonly System.Reflection.PropertyInfo _cardPathProp =
        typeof(CardProgress).GetProperty("SavePath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

    static string CardPath()
    {
        if (_cardPathProp == null) return null;
        try { return (string)_cardPathProp.GetValue(null); } catch { return null; }
    }

    // ==================================================================
    //  路径判据
    // ==================================================================
    static string TempRoot() { return System.IO.Path.GetFullPath(Application.temporaryCachePath); }
    static string RealRoot() { return System.IO.Path.GetFullPath(SaveRoot.Path); }

    static bool Under(string path, string root)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(root)) return false;
        string p, r;
        try { p = System.IO.Path.GetFullPath(path); } catch { return false; }
        try { r = System.IO.Path.GetFullPath(root); } catch { return false; }
        if (!r.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString())) r += System.IO.Path.DirectorySeparatorChar;
        return p.StartsWith(r, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>5 处是否**全部**在临时沙箱里（A 组的判据本体；C 组拿它做灭自证）。</summary>
    static bool AllInSandbox()
    {
        var temp = TempRoot();
        var sinks = Sinks();
        for (int i = 0; i < sinks.Length; i++)
        {
            string p = sinks[i].Path();
            if (p == null || !Under(p, temp)) return false;
        }
        return true;
    }

    // ==================================================================
    //  A —— 批处理 + 无注入 ⇒ 全在沙箱
    // ==================================================================
    static void A_BatchInSandbox()
    {
        Section("A 批处理（无注入）⇒ 5 处全部落沙箱");
        SaveSandbox.BatchModeProbe = null;      // 用真正的 `Application.isBatchMode`
        SaveSandbox.OverridePath = null;
        SaveSandbox.DisableForTest = false;

        Check(Application.isBatchMode,
              "A0（前提）本宿主跑在 `-batchmode` 下（`Application.isBatchMode == true`）—— 否则本组不成立、⛔ 不当绿");
        Check(_cardPathProp != null,
              "A0a（前提）反射拿得到 `CardProgress.SavePath`（`private static`）—— 拿不到后面 5 处里就少了 1 处");
        CheckTrueRootNotTemp();

        foreach (var s in Sinks())
        {
            string p = s.Path();
            Check(p != null, "A1（前提·" + s.Name + "）路径算得出来（非 null）");
            if (p == null) continue;            // ⛔ 不静默跳过 —— 上面那条已经红了
            Check(Under(p, TempRoot()), "A2 " + s.Name + " 落在 `temporaryCachePath` 下（" + p + "）");
            Check(!Under(p, RealRoot()), "A3 " + s.Name + " **不在** 玩家真根 `SaveRoot.Path` 下（真根 " + RealRoot() + "）");
        }
        Check(AllInSandbox(), "A4 5 处**一起**看 = 全在沙箱里（这一条就是 C 组要打红的那条判据）");
    }

    static void CheckTrueRootNotTemp()
    {
        // 前提：真根与临时区**不重叠**（否则 A3 恒真、整组是空判）
        Check(!Under(RealRoot(), TempRoot()) && !Under(TempRoot(), RealRoot()),
              "A0b（前提）玩家真根「" + RealRoot() + "」与临时区「" + TempRoot() + "」互不包含");
    }

    // ==================================================================
    //  B —— 非批处理 / 注入 Override ⇒ 回原根 / 注入值
    // ==================================================================
    static void B_NotAlwaysSandbox()
    {
        Section("B 非批处理 / 注入 ⇒ 回到原根或注入值（助手不是恒返沙箱）");
        string tempDir = System.IO.Path.Combine(TempRoot(), "wf_b140v4b_override_" + SaveSandbox.ProcessTag);
        try
        {
            // B1：非批处理 ⇒ 原样 `SaveRoot.Path`
            SaveSandbox.OverridePath = null;
            SaveSandbox.DisableForTest = false;
            SaveSandbox.BatchModeProbe = () => false;
            var real = RealRoot();
            foreach (var s in Sinks())
            {
                string p = s.Path();
                Check(p != null, "B1（前提·" + s.Name + "）非批处理下路径非 null");
                if (p == null) continue;
                Check(Under(p, real), "B1 " + s.Name + " 非批处理 ⇒ 回到 `SaveRoot.Path` 下（" + p + "）");
                Check(!Under(p, TempRoot() + System.IO.Path.DirectorySeparatorChar + SaveSandbox.DirName),
                      "B1 " + s.Name + " 非批处理 ⇒ **不是**沙箱");
            }

            // B2：批处理 + 注入 Override ⇒ 注入值（优先级最高，既有自检全靠它）
            Directory.CreateDirectory(tempDir);
            SaveSandbox.BatchModeProbe = () => true;
            SaveSandbox.OverridePath = tempDir;
            foreach (var s in Sinks())
            {
                string p = s.Path();
                Check(p != null, "B2（前提·" + s.Name + "）注入 Override 后路径非 null");
                if (p == null) continue;
                Check(Under(p, tempDir), "B2 " + s.Name + " 批处理 + 注入 ⇒ 落在注入根下（" + p + "）");
            }
        }
        finally
        {
            SaveSandbox.OverridePath = null;
            SaveSandbox.BatchModeProbe = null;
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }
    }

    // ==================================================================
    //  C —— 🧨 灭自证
    // ==================================================================
    static void C_AntiSelfProof()
    {
        Section("C 🧨 灭自证：把助手拨回「恒返 SaveRoot.Path」⇒ A 组判据必须翻红");
        try
        {
            SaveSandbox.BatchModeProbe = null;
            SaveSandbox.OverridePath = null;
            SaveSandbox.DisableForTest = true;      // 模拟「这道闸被人删了」
            Check(!AllInSandbox(),
                  "C1 🧨 闸被拨掉 ⇒ `AllInSandbox()` 变 **false**（= A4 会红）—— 证明 A 组量的不是助手自己的意图");
            foreach (var s in Sinks())
            {
                string p = s.Path();
                Check(p != null && Under(p, RealRoot()),
                      "C2 " + s.Name + " 跟着回到了真根（" + (p ?? "<null>") + "）—— 5 处确实**都**接了这道闸");
            }
        }
        finally { SaveSandbox.DisableForTest = false; }
    }

    // ==================================================================
    //  D —— 5 处真实保存各触发一次 ⇒ 真根一个字节都不动
    // ==================================================================
    struct FileSnap
    {
        public bool Exists;
        public long Len;
        public long Ticks;
        public string Sha;
    }

    static FileSnap Snap(string path)
    {
        var s = new FileSnap();
        if (!File.Exists(path)) return s;
        var b = File.ReadAllBytes(path);
        s.Exists = true; s.Len = b.LongLength; s.Ticks = File.GetLastWriteTimeUtc(path).Ticks;
        using (var sha = SHA256.Create()) s.Sha = Convert.ToBase64String(sha.ComputeHash(b));
        return s;
    }

    static bool Same(FileSnap a, FileSnap b)
    {
        return a.Exists == b.Exists && a.Len == b.Len && a.Ticks == b.Ticks && a.Sha == b.Sha;
    }

    static string Desc(FileSnap s)
    {
        return s.Exists ? ("存在 · " + s.Len + " B · mtime " + new DateTime(s.Ticks, DateTimeKind.Utc).ToString("o") + " · sha " + s.Sha)
                        : "不存在";
    }

    /// <summary>录像目录的快照（文件名 + 大小 + mtime，排序后拼成一串）。</summary>
    static string DirSnap(string dir)
    {
        if (!Directory.Exists(dir)) return "<no-dir>";
        var names = Directory.GetFiles(dir);
        Array.Sort(names, StringComparer.OrdinalIgnoreCase);
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < names.Length; i++)
            sb.Append(System.IO.Path.GetFileName(names[i])).Append('|').Append(new FileInfo(names[i]).Length)
              .Append('|').Append(File.GetLastWriteTimeUtc(names[i]).Ticks).Append('\n');
        return sb.ToString();
    }

    static void D_RealSavesUntouched()
    {
        Section("D 5 处真实保存各触发一次 ⇒ 玩家真根逐字节 + mtime 不变");

        SaveSandbox.BatchModeProbe = null;
        SaveSandbox.OverridePath = null;
        SaveSandbox.DisableForTest = false;

        string real = RealRoot();
        string fCard = System.IO.Path.Combine(real, CardProgress.SaveFileName);
        string fRanked = System.IO.Path.Combine(real, "WarpforgeRanked", "ranked.json");
        string dReplay = System.IO.Path.Combine(real, ReplayStore.DirName);
        string fNet = System.IO.Path.Combine(real, NetConfig.FileName);
        string fProfile = System.IO.Path.Combine(real, ProfileData.SaveDirName, ProfileData.SaveFileName);

        FileSnap c0 = Snap(fCard), r0 = Snap(fRanked), n0 = Snap(fNet), p0 = Snap(fProfile);
        string rep0 = DirSnap(dReplay);
        Debug.Log(Tag + " … 真根快照：" + fCard + " = " + Desc(c0));

        // ---- 1 卡牌等级 --------------------------------------------------
        bool keepPersist = CardProgress.PersistOn;
        try
        {
            CardProgress.SetPersistForTest(true);
            bool ok = CardProgress.SetLevel("WF_B140V4B_PROBE", 2);
            Check(ok, "D1a `CardProgress.SetLevel` 这一次**写盘成功**（否则「真根没变」是空判）");
        }
        finally { CardProgress.SetPersistForTest(keepPersist); }
        Check(CardPath() != null && File.Exists(CardPath()),
              "D1b 卡牌等级确实落进了沙箱（" + (CardPath() ?? "<null>") + "）");

        // ---- 2 排位 ------------------------------------------------------
        RankedData.SaveNow();
        Check(RankedData.StorePath != null && File.Exists(RankedData.StorePath),
              "D2 排位档确实落进了沙箱（" + (RankedData.StorePath ?? "<null>") + "）");

        // ---- 3 录像 ------------------------------------------------------
        var rec = new ReplayRecord();
        string rn = ReplayStore.Save(rec);
        Check(rn != null && ReplayStore.Dir != null && File.Exists(System.IO.Path.Combine(ReplayStore.Dir, rn)),
              "D3 录像确实落进了沙箱（" + (ReplayStore.Dir ?? "<null>") + " / " + (rn ?? "<null>") + "）");

        // ---- 4 联机设置 --------------------------------------------------
        NetConfig.Save();
        Check(NetConfig.Path != null && File.Exists(NetConfig.Path),
              "D4 联机设置确实落进了沙箱（" + (NetConfig.Path ?? "<null>") + "）");

        // ---- 5 玩家档案 --------------------------------------------------
        ProfileData.SaveNow();
        Check(ProfileData.SavePath != null && File.Exists(ProfileData.SavePath),
              "D5 玩家档案确实落进了沙箱（" + (ProfileData.SavePath ?? "<null>") + "）");

        // ---- 真根对照 ----------------------------------------------------
        FileSnap c1 = Snap(fCard), r1 = Snap(fRanked), n1 = Snap(fNet), p1 = Snap(fProfile);
        string rep1 = DirSnap(dReplay);
        Check(Same(c0, c1), "D6 🧨 真根 `WarpforgeCardLevels.json` **内容逐字节 + mtime 不变**（前 " + Desc(c0) + " / 后 " + Desc(c1) + "）");
        Check(Same(r0, r1), "D7 🧨 真根 `WarpforgeRanked/ranked.json` **内容逐字节 + mtime 不变**（前 " + Desc(r0) + " / 后 " + Desc(r1) + "）");
        Check(rep0 == rep1, "D8 🧨 真根 `WarpforgeReplays/` **一个文件都没多/少/改**（前 " + (rep0.Length == 0 ? "<空目录>" : rep0.Trim()) + "）");
        Check(Same(n0, n1), "D9 🧨 真根 `WarpforgeNet.json` **内容逐字节 + mtime 不变**（前 " + Desc(n0) + " / 后 " + Desc(n1) + "）");
        Check(Same(p0, p1), "D10 🧨 真根 `WarpforgeProfile/profile.json` **内容逐字节 + mtime 不变**（前 " + Desc(p0) + " / 后 " + Desc(p1) + "）");
    }

    // ==================================================================
    //  入口
    // ==================================================================
    public static void Run()
    {
        _pass = _fail = 0;
        Func<bool> prevProbe = SaveSandbox.BatchModeProbe;
        string prevOverride = SaveSandbox.OverridePath;
        bool prevDisable = SaveSandbox.DisableForTest;
        try
        {
            Debug.Log("=== " + Tag + " 开始（存档写口沙箱 · 进程 tag " + SaveSandbox.ProcessTag + "）===");
            A_BatchInSandbox();
            B_NotAlwaysSandbox();
            C_AntiSelfProof();
            D_RealSavesUntouched();
        }
        finally
        {
            SaveSandbox.BatchModeProbe = prevProbe;
            SaveSandbox.OverridePath = prevOverride;
            SaveSandbox.DisableForTest = prevDisable;
            Debug.Log("=== " + Tag + " 结束：通过 " + _pass + " · 失败 " + _fail + " ===");
        }
    }
}
