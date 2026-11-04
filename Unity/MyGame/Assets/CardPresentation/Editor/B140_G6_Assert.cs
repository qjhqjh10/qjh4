// B140_G6_Assert.cs — 「Mobile 档全局后处理」G6 的断言（`B140_WJ`）
//
// 被测：`Assets/Settings/Mobile_RPAsset.asset` 的两个字段（同域，两次参数级修复）：
//   ① **`m_VolumeProfile`**（G6 主项）—— `B140_WJ` 2026-11-04 由 `SampleSceneProfile` 改为 `{fileID: 0}`；
//   ② **`m_VolumeFrameworkUpdateMode`**（追加项）—— 2026-11-04 由 **0** 改为 **1**。
//      原版 `UniversalRenderPipelineAsset Medium.asset:94` = **1**；我们 `PC_RPAsset.asset:94` 本来也是 **1**。
//
// 🔴 键名订正（B140_WJ 现读 2026-11-04）：任务简报与 `B136` 审计里写的 `postProcessData` **是笔误** ——
//   `postProcessData` 是 **Renderer 资产**（`Mobile_Renderer.asset:31`）上的字段，两档都指向 URP 包自带的
//   `PostProcessData.asset`（guid `41439944d30ece34e96484bdb6645b55`），**不是**差异点、更不能置 0（置 0 会让整条后处理链拿不到 shader）。
//   RP 资产上真正决定「全局后处理面板」的是 `m_VolumeProfile`。
//
// 判据（原版，现读）：
//   `d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/UniversalRenderPipelineAsset Medium.asset:95`
//     → `m_VolumeProfile: {fileID: 0}`
//   同族三份 `High.asset:95` / `Low.asset:95` / `Ultra.asset:95` **全部 = `{fileID: 0}`**（4/4）。
//   我们改前 = `{fileID: 11400000, guid: 10fc4df2da32a41aaa32d77bc913491c, type: 2}` → `SampleSceneProfile`
//     （Bloom intensity 0.25 / threshold 1.0 / scatter 0.5 + Vignette 0.2）⇒ 切 Mobile 档整屏多 bloom + 暗角。
//   同工程 PC 档 `PC_RPAsset.asset:95` 本来就是 `{fileID: 0}`，本次把 Mobile 档对齐。
//
// 直接跑（类在**全局命名空间**、类名 == 文件名、不依赖任何宿主场景）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_G6_Assert.Run -logFile -
//
// 判绿红：按**行首** `B140G6 ✓` / `B140G6 ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
//
// 四组：
//   A 前提（硬断言，先断「文件在、键在、是这份资产」，再断值 —— 前提不成立必须报红，⛔ 不静默跳过）；
//      A7/A8 是追加项的键存在 + 可解析；
//   B 值：`m_VolumeProfile` fileID == 0 · 全文件不含 SampleSceneProfile 的 guid · 与 PC 档同键一致；
//      B7/B8 追加项：`m_VolumeFrameworkUpdateMode` == 1（原版 Medium.asset:94）；
//   C 🧨 灭自证：
//       C1 同一个解析器指向本文件里**已知非 0** 的 `blueNoise64LTex` → 2800000（证解析器能返非 0）；
//       C2 同一个解析器指向**合成的改前行**（含 `11400000` + SampleSceneProfile guid）→ 11400000；
//       C3 用**同一条判定函数**去判 C2 的读数 ⇒ 必须返回「不合判据」（若判定恒真，这里会翻红）；
//       C4 同一个解析器指向**另一份真资产** `PC_Renderer.asset` 的 `postProcessData` → 11400000（非 0）；
//       C5 追加项的灭自证：同一标量解析器读**已知为 0** 的 `m_RequireDepthTexture`，判定函数必须返 false；
//          C5d/C5e 用合成的改前行（`m_VolumeFrameworkUpdateMode: 0`）再证一次；
//   D 结构：置 0 之后「全局默认 volume profile」仍在（`UniversalRenderPipelineGlobalSettings.asset` 的
//       `URPDefaultVolumeProfileSettings.m_VolumeProfile` → `DefaultVolumeProfile.asset`），
//       证明这一步是「回到原版形态」，不是「把后处理链整个拆了」。
//
// ⚠️ 期望值全部是**外部量**（原版解包文件的行 / 本文件里别的非 0 字段 / 同工程另一份资产 / 合成旧值），
//   ⛔ 不用被测算出来的东西去算期望（那是同义反复）。
using System;
using System.IO;
using UnityEngine;

public static class B140_G6_Assert
{
    const string Tag = "B140G6";

    // 原版判据值（`UniversalRenderPipelineAsset Medium.asset:95`，4 份 RPAsset 全部为 0）
    const long OriginalFileId = 0;
    // 原版判据值（`UniversalRenderPipelineAsset Medium.asset:94` = 1；我们 `PC_RPAsset.asset:94` 也是 1）
    const int OriginalVolumeUpdateMode = 1;

    const string MobileRpAssetPath   = "Assets/Settings/Mobile_RPAsset.asset";   // 被测
    const string PcRpAssetPath       = "Assets/Settings/PC_RPAsset.asset";       // 同键对照
    const string PcRendererPath      = "Assets/Settings/PC_Renderer.asset";      // C4 另一份真资产
    const string GlobalSettingsPath  = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset";

    const string VolumeProfileKey    = "m_VolumeProfile";
    const string VolumeUpdateModeKey = "m_VolumeFrameworkUpdateMode";
    const string PostProcessDataKey  = "postProcessData";
    const string SampleSceneProfileGuid = "10fc4df2da32a41aaa32d77bc913491c";   // 改前误挂的那份
    const string DefaultVolumeProfileGuid = "ab09877e2e707104187f6f83e2f62510"; // 全局默认（中性）

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
    //  解析件（全部是纯字符串操作，便于 C 组拿合成行做灭自证）
    // ==================================================================
    static string ProjectRoot()
    {
        // Application.dataPath = <project>/Assets
        DirectoryInfo d = Directory.GetParent(Application.dataPath);
        return d == null ? Application.dataPath : d.FullName;
    }

    static string Abs(string assetRelativePath)
    {
        return Path.Combine(ProjectRoot(), assetRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    /// 数「trim 后以 `<key>:` 开头」的行数（避免 m_ObsoleteDefaultVolumeProfile 这类前缀撞名）
    static int CountKey(string[] lines, string key)
    {
        string prefix = key + ":";
        int n = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string t = lines[i].Trim();
            if (t.StartsWith(prefix, StringComparison.Ordinal)) n++;
        }
        return n;
    }

    /// 取该键那一行的行号（1 基，找不到返回 -1）
    static int FindKeyLine(string[] lines, string key)
    {
        string prefix = key + ":";
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().StartsWith(prefix, StringComparison.Ordinal)) return i + 1;
        }
        return -1;
    }

    /// 从含 `{fileID: N ...}` 的文本里取 N。`hasBraces` 区分「有花括号但 fileID 不合法」与「根本没花括号」。
    static bool TryParseFileId(string text, out long id, out bool hasBraces)
    {
        id = 0;
        hasBraces = false;
        if (text == null) return false;

        int b = text.IndexOf('{');
        if (b < 0) return false;
        hasBraces = true;

        int k = text.IndexOf("fileID:", b, StringComparison.Ordinal);
        if (k < 0) return false;
        k += "fileID:".Length;
        while (k < text.Length && text[k] == ' ') k++;

        int start = k;
        if (k < text.Length && (text[k] == '-' || text[k] == '+')) k++;
        int digits = k;
        while (k < text.Length && text[k] >= '0' && text[k] <= '9') k++;
        if (k == digits) return false;

        return long.TryParse(text.Substring(start, k - start), out id);
    }

    /// 读取某份资产里某个键的 fileID；找不到键 / 解析不出都返回 false
    static bool TryReadKeyFileId(string[] lines, string key, out long id, out int lineNo)
    {
        id = 0;
        lineNo = FindKeyLine(lines, key);
        if (lineNo < 0) return false;
        bool ignored;
        return TryParseFileId(lines[lineNo - 1], out id, out ignored);
    }

    /// 🔑 唯一的「合不合原版判据」函数 —— C3 会拿它去判一个**已知非 0** 的读数，证它不恒真
    static bool IsOriginalVolumeProfile(long fileId)
    {
        return fileId == OriginalFileId;
    }

    /// 标量整数字段（`key: 1`）的**行级**解析 —— `m_VolumeFrameworkUpdateMode` 这类**没有花括号**。
    /// 拆成行级是为了让 C5 能拿合成行做灭自证（与 C2/C3 同一手法）。
    static bool TryParseScalarIntLine(string line, string key, out int value)
    {
        value = 0;
        if (line == null) return false;
        string t = line.Trim();
        string prefix = key + ":";
        if (!t.StartsWith(prefix, StringComparison.Ordinal)) return false;
        return int.TryParse(t.Substring(prefix.Length).Trim(), out value);
    }

    static bool TryReadScalarInt(string[] lines, string key, out int value, out int lineNo)
    {
        value = 0;
        lineNo = FindKeyLine(lines, key);
        if (lineNo < 0) return false;
        return TryParseScalarIntLine(lines[lineNo - 1], key, out value);
    }

    /// 🔑 `m_VolumeFrameworkUpdateMode` 的原版判据：**1**（原版 `Medium.asset:94` / 我们 `PC_RPAsset.asset:94`）。
    ///    C5 会拿它去判一个**已知为 0** 的字段，证它不恒真。
    static bool IsOriginalVolumeUpdateMode(int v)
    {
        return v == OriginalVolumeUpdateMode;
    }

    // ==================================================================
    //  A 前提（硬断言：前提不成立就报红，⛔ 不写 `if (前提) Check(结论)`）
    // ==================================================================
    static string[] LoadMobileRpAsset()
    {
        Section("A 前提");
        string abs = Abs(MobileRpAssetPath);
        Check(File.Exists(abs), "A1 被测资产存在：" + abs);
        if (!File.Exists(abs)) return null;   // 后面全部无法进行，让 A1 的红说明原因

        string[] lines = File.ReadAllLines(abs);
        Check(lines.Length > 100, "A2 读到 " + lines.Length + " 行（>100，确认是 RPAsset 本体而不是空壳/占位）");

        // 确认「这确实是 Mobile 档那份 RPAsset」，而不是同目录别的资产
        int nameCount = 0, verCount = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string t = lines[i].Trim();
            if (t == "m_Name: Mobile_RPAsset") nameCount++;
            if (t.StartsWith("k_AssetVersion:", StringComparison.Ordinal)) verCount++;
        }
        Check(nameCount == 1, "A3 资产身份：`m_Name: Mobile_RPAsset` 命中 1 处（实得 " + nameCount + "）");
        Check(verCount == 1, "A4 资产身份：`k_AssetVersion:` 命中 1 处（实得 " + verCount + "）");

        int keyCount = CountKey(lines, VolumeProfileKey);
        Check(keyCount == 1, "A5 键 `" + VolumeProfileKey + ":` 恰好 1 处（实得 " + keyCount + "）");

        long id; int lineNo;
        bool ok = TryReadKeyFileId(lines, VolumeProfileKey, out id, out lineNo);
        Check(ok, "A6 能解析出 fileID（行 " + lineNo + "）；解析失败 ⇒ 后续值判定无意义");

        // ── 同域第二件（追加）：m_VolumeFrameworkUpdateMode ──
        int modeCount = CountKey(lines, VolumeUpdateModeKey);
        Check(modeCount == 1, "A7 键 `" + VolumeUpdateModeKey + ":` 恰好 1 处（实得 " + modeCount + "）");
        int modeVal; int modeLine;
        Check(TryReadScalarInt(lines, VolumeUpdateModeKey, out modeVal, out modeLine),
              "A8 能解析出 `" + VolumeUpdateModeKey + "` 的整数（行 " + modeLine + "）；解析失败 ⇒ B8 无意义");
        return lines;
    }

    // ==================================================================
    //  B 值
    // ==================================================================
    static void B_Value(string[] lines)
    {
        Section("B 值（原版判据 → 现读）");
        if (lines == null) { Check(false, "B0 前提缺失（A1 失败），值判定无法进行"); return; }

        long id; int lineNo;
        bool ok = TryReadKeyFileId(lines, VolumeProfileKey, out id, out lineNo);
        Check(ok, "B1 现读到 `" + VolumeProfileKey + "` 的 fileID（行 " + lineNo + "）");
        if (!ok) { Check(false, "B2 值判定无法进行（B1 红）"); return; }

        Check(IsOriginalVolumeProfile(id),
              "B2 fileID == " + OriginalFileId + "（原版 Medium/High/Low/Ultra 4/4）；实得 " + id + " @ " + MobileRpAssetPath + ":" + lineNo);

        bool hasGuid = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf(SampleSceneProfileGuid, StringComparison.OrdinalIgnoreCase) >= 0) { hasGuid = true; break; }
        }
        Check(!hasGuid, "B3 全文件不含 SampleSceneProfile 的 guid `" + SampleSceneProfileGuid + "`（改前误挂的那份）");

        // 结构一致：同工程 PC 档本来就是 0（⚠️ 这是「与本工程另一档一致」，不是原版判据本身）
        string pcAbs = Abs(PcRpAssetPath);
        Check(File.Exists(pcAbs), "B4 对照资产存在：" + pcAbs);
        if (File.Exists(pcAbs))
        {
            string[] pcLines = File.ReadAllLines(pcAbs);
            long pcId; int pcLine;
            bool pcOk = TryReadKeyFileId(pcLines, VolumeProfileKey, out pcId, out pcLine);
            Check(pcOk, "B5 PC 档同键可解析（行 " + pcLine + "）");
            Check(pcOk && pcId == id, "B6 两档同键一致：Mobile=" + id + " / PC=" + pcId);
        }

        // ── 同域第二件（追加 2026-11-04）：m_VolumeFrameworkUpdateMode ──
        // 原版 `UniversalRenderPipelineAsset Medium.asset:94` = 1；我们 `PC_RPAsset.asset:94` 也是 1
        int modeVal; int modeLine;
        bool modeOk = TryReadScalarInt(lines, VolumeUpdateModeKey, out modeVal, out modeLine);
        Check(modeOk, "B7 现读到 `" + VolumeUpdateModeKey + "`（行 " + modeLine + "）");
        if (modeOk)
        {
            Check(IsOriginalVolumeUpdateMode(modeVal),
                  "B8 `" + VolumeUpdateModeKey + "` == " + OriginalVolumeUpdateMode +
                  "（原版 Medium.asset:94 / 我们 PC_RPAsset.asset:94）；实得 " + modeVal + " @ " + MobileRpAssetPath + ":" + modeLine);
        }
        else
        {
            Check(false, "B8 值判定无法进行（B7 红）");
        }
    }

    // ==================================================================
    //  C 🧨 灭自证
    // ==================================================================
    static void C_AntiSelfProof(string[] lines)
    {
        Section("C 灭自证");

        // C1 —— 同一个解析器，指着本文件里**已知非 0** 的纹理引用（blueNoise64LTex: fileID 2800000）
        if (lines != null)
        {
            long texId; int texLine;
            bool texOk = TryReadKeyFileId(lines, "blueNoise64LTex", out texId, out texLine);
            Check(texOk, "C1a 解析器能读另一个键 `blueNoise64LTex`（行 " + texLine + "）");
            Check(texOk && texId != 0,
                  "C1b 解析器能返回**非 0**：blueNoise64LTex fileID = " + texId + "（期望 2800000）⇒ 判定不是「只会读到 0」");
            Check(texId == 2800000, "C1c blueNoise64LTex fileID 恰为 2800000；实得 " + texId);
        }
        else
        {
            Check(false, "C1 前提缺失（A1 失败）");
        }

        // C2 —— 同一个解析器，指着一行**合成的改前状态**（就是 B140_WJ 改掉的那一行原样）
        const string OldLine = "  m_VolumeProfile: {fileID: 11400000, guid: 10fc4df2da32a41aaa32d77bc913491c, type: 2}";
        long oldId; bool oldHasBraces;
        bool oldOk = TryParseFileId(OldLine, out oldId, out oldHasBraces);
        Check(oldOk && oldHasBraces, "C2a 解析器能读改前行（有花括号且 fileID 可解析）");
        Check(oldOk && oldId == 11400000, "C2b 改前行的 fileID = " + oldId + "（期望 11400000）");

        // C3 —— 🔑 拿【同一条判定函数】去判 C2 的读数：必须返回 false。
        //        如果 IsOriginalVolumeProfile 写成恒真（例如 return true / 判错变量），这一条必红。
        Check(oldOk && !IsOriginalVolumeProfile(oldId),
              "C3 灭自证：判定函数对改前值 11400000 **返回 false**（若恒真 ⇒ 这里红）");

        // C3b —— 反向：对 0 必须返回 true（否则 B2 的红就是假红）
        Check(IsOriginalVolumeProfile(0), "C3b 判定函数对 0 返回 true（正向不是恒假）");

        // C4 —— 同一个解析器，指向**另一份真资产**（PC_Renderer.asset 的 postProcessData = 11400000）
        string prAbs = Abs(PcRendererPath);
        Check(File.Exists(prAbs), "C4a 另一份真资产存在：" + prAbs);
        if (File.Exists(prAbs))
        {
            string[] prLines = File.ReadAllLines(prAbs);
            long ppId; int ppLine;
            bool ppOk = TryReadKeyFileId(prLines, PostProcessDataKey, out ppId, out ppLine);
            Check(ppOk, "C4b 读 `" + PostProcessDataKey + "`（行 " + ppLine + "）");
            Check(ppOk && ppId != 0,
                  "C4c `" + PostProcessDataKey + "` = " + ppId + "（非 0）⇒ 它是 Renderer 上的字段、本次**未动**（置 0 会拆掉整条后处理链的 shader 资源）");
        }

        // C5 —— 第二件（m_VolumeFrameworkUpdateMode）的灭自证：
        //   同一个标量解析器读一个**已知为 0** 的字段（本文件 `m_RequireDepthTexture: 0`），判定函数必须返 false。
        if (lines != null)
        {
            int depthVal; int depthLine;
            bool depthOk = TryReadScalarInt(lines, "m_RequireDepthTexture", out depthVal, out depthLine);
            Check(depthOk, "C5a 标量解析器能读另一个键 `m_RequireDepthTexture`（行 " + depthLine + "）");
            Check(depthOk && depthVal == 0, "C5b `m_RequireDepthTexture` = " + depthVal + "（已知为 0 的锚点值）");
            Check(depthOk && !IsOriginalVolumeUpdateMode(depthVal),
                  "C5c 灭自证：判定函数对**已知为 0** 的读数返回 false（若恒真 ⇒ 这里红）");
        }
        else
        {
            Check(false, "C5 前提缺失（A1 失败）");
        }

        // C5d/C5e —— 同一个行级解析器读**合成的改前行**（就是这次从 0 改成 1 的那一行）
        int oldMode;
        bool oldModeOk = TryParseScalarIntLine("  m_VolumeFrameworkUpdateMode: 0", VolumeUpdateModeKey, out oldMode);
        Check(oldModeOk && oldMode == 0, "C5d 改前行 `m_VolumeFrameworkUpdateMode: 0` 解析为 " + oldMode);
        Check(oldModeOk && !IsOriginalVolumeUpdateMode(oldMode),
              "C5e 灭自证：判定函数对改前值 0 返回 false（若有人把值改回 0 ⇒ 这里与 B8 一起红）");
    }

    // ==================================================================
    //  D 结构：置 0 之后，全局默认 volume profile 仍在
    // ==================================================================
    static void D_GlobalDefaultProfileStillWired()
    {
        Section("D 结构");
        string abs = Abs(GlobalSettingsPath);
        Check(File.Exists(abs), "D1 全局设置资产存在：" + abs);
        if (!File.Exists(abs)) return;

        string[] lines = File.ReadAllLines(abs);
        bool hasDefault = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf(DefaultVolumeProfileGuid, StringComparison.OrdinalIgnoreCase) >= 0) { hasDefault = true; break; }
        }
        Check(hasDefault,
              "D2 全局 `URPDefaultVolumeProfileSettings` 仍指向 DefaultVolumeProfile.asset（`" + DefaultVolumeProfileGuid + "`）" +
              " ⇒ 本次只是回到原版形态，没有拆掉后处理链");

        // 顺带记录：旧的 m_ObsoleteDefaultVolumeProfile 必须是 0（否则会与上面那条抢）
        long obId; int obLine;
        bool obOk = TryReadKeyFileId(lines, "m_ObsoleteDefaultVolumeProfile", out obId, out obLine);
        Check(obOk, "D3 能读到 `m_ObsoleteDefaultVolumeProfile`（行 " + obLine + "）");
        Check(obOk && obId == 0, "D4 `m_ObsoleteDefaultVolumeProfile` == 0（实得 " + obId + "）");
    }

    // ==================================================================
    //  入口
    // ==================================================================
    public static void Run()
    {
        _pass = _fail = 0;
        Debug.Log("=== " + Tag + " 开始（Mobile 档全局后处理 · project " + ProjectRoot() + "）===");
        string[] lines = null;
        try
        {
            lines = LoadMobileRpAsset();
            B_Value(lines);
            C_AntiSelfProof(lines);
            D_GlobalDefaultProfileStillWired();
        }
        catch (Exception e)
        {
            _fail++;
            Debug.LogError(Tag + " ✗ 异常：" + e);
        }
        finally
        {
            Debug.Log("=== " + Tag + " 结束：通过 " + _pass + " · 失败 " + _fail + " ===");
        }
    }
}
