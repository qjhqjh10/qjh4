// B140_WS_Assert.cs — 网格内部标志（审计 `B136` top-5 #4）的自检断言宿主
//
// 活：把 `B140_WS_MeshFlags` 那 101 份网格的四个内部标志钉住，并把「编辑器绿灯 ≠ 出包安全」这件事
//     本身也钉成一条断言（否则下一轮会有人拿编辑器绿去证明出包没问题）。
//
// 判据出处：
//   · `资料/完全复刻/报告/B136_审计_网格与预制体结构.md` A-14 / A-15 / A-16
//   · 原版逐份值 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Mesh/<同名>.asset`
//     （已被烘进 `B140_WS_MeshFlags.Manifest`；本文件**只读**那份清单，不另抄一份）
//
// 跑法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_WS_Assert.Run -logFile -
//
// 判绿红：按**行首** `B140WS ✓` / `B140WS ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 会出现在文案里）。
//
// 覆盖：
//   A  名单内 101 份网格的四个字段逐值 = 原版（四个字段各一档，逐条失败点名）
//   B  读顶点那条路在**编辑器**里仍然拿得到顶点（先断前提：网格在、顶点数对）
//   C  灭自证：把 `m_KeepVertices` 改回 0 ⇒ A 的判据必须报红；而**顶点在盘上照样读得出**
//      ⇒ 直接证明「编辑器静态证据不足以说明出包后安全」——所以出包验证是**必需**的，不是加分项
//
// ⚠️ `C` 只在一份**临时副本**上做（`Library/B140_WS_tmp/`），⛔ 一个字节都不动工程里的资产。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class B140_WS_Assert
{
    const string Tag = "B140WS";
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + what); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + what); }
    }

    public static void Run()
    {
        _pass = _fail = 0;
        Debug.Log(Tag + " ── 开始（网格内部标志 · B136 top-5 #4）");
        CheckPrereq();
        CheckA_Flags();
        CheckB_Vertices();
        CheckC_SelfProof();
        Debug.Log(Tag + $"合计：✓ {_pass} · ✗ {_fail}" + (_fail == 0 ? "（全绿）" : "（有红，见上）"));
    }

    // ── 前提 ───────────────────────────────────────────────────────────────────

    static void CheckPrereq()
    {
        int n = B140_WS_MeshFlags.Count;
        Check(n == B140_WS_MeshFlags.ExpectedCount,
              $"前提：清单条数 = {B140_WS_MeshFlags.ExpectedCount}（实得 {n}）");

        int missing = 0, unparsable = 0;
        for (int i = 0; i < n; i++)
        {
            string full = B140_WS_MeshFlags.FullPath(B140_WS_MeshFlags.AssetPath(i));
            if (!File.Exists(full)) { missing++; Debug.LogError(Tag + "   ✗ 缺文件 " + B140_WS_MeshFlags.AssetPath(i)); continue; }
            if (B140_WS_MeshFlags.ReadFlagsFromFile(full) == null)
            {
                unparsable++;
                Debug.LogError(Tag + "   ✗ 四个字段读不全 " + B140_WS_MeshFlags.AssetPath(i));
            }
        }
        Check(missing == 0, $"前提：101 份网格文件都在（缺 {missing}）");
        Check(unparsable == 0, $"前提：101 份的四个字段都读得出（读不全 {unparsable}）");
    }

    // ── A：四个字段逐值 = 原版 ─────────────────────────────────────────────────

    static void CheckA_Flags()
    {
        int n = B140_WS_MeshFlags.Count;
        var bad = new List<string>[4];
        for (int k = 0; k < 4; k++) bad[k] = new List<string>();

        for (int i = 0; i < n; i++)
        {
            string full = B140_WS_MeshFlags.FullPath(B140_WS_MeshFlags.AssetPath(i));
            int[] got = B140_WS_MeshFlags.ReadFlagsFromFile(full);
            int[] want = B140_WS_MeshFlags.Expected(i);
            if (got == null) { for (int k = 0; k < 4; k++) bad[k].Add(B140_WS_MeshFlags.AssetPath(i) + "（读不到）"); continue; }
            for (int k = 0; k < 4; k++)
                if (got[k] != want[k])
                    bad[k].Add($"{B140_WS_MeshFlags.AssetPath(i)} 现 {got[k]} / 原版 {want[k]}");
        }

        for (int k = 0; k < 4; k++)
        {
            foreach (var s in bad[k]) Debug.LogError(Tag + "   ✗ " + s);
            Check(bad[k].Count == 0,
                  $"A：`{B140_WS_MeshFlags.Fields[k]}` 101 份逐值 = 原版（不符 {bad[k].Count}）");
        }
    }

    // ── B：编辑器里拿得到顶点 ──────────────────────────────────────────────────

    static void CheckB_Vertices()
    {
        // 前提①：走**运行期同一个读口**（`CardArt.Card3DMesh()` = `Resources.Load<Mesh>`）能拿到网格
        Mesh card3d = CardPresentation.CardArt.Card3DMesh();
        Check(card3d != null, "B 前提①：`CardArt.Card3DMesh()`（Resources.Load）拿到 `Card 3D WH40k`");
        if (card3d == null) { Check(false, "B：网格拿不到 ⇒ 下面两条无从谈起"); return; }

        // 前提②：顶点数就是原版的 881（审计 A-01）
        Check(card3d.vertexCount == 881, $"B 前提②：`Card 3D WH40k` 顶点数 = 881（实得 {card3d.vertexCount}）");

        // B-1：`mesh.vertices` 在编辑器里读得出、且非空 / 无 NaN（= 这条读口的「编辑器侧」证据）
        var vs = card3d.vertices;
        bool finite = vs != null && vs.Length == card3d.vertexCount && vs.Length > 0;
        if (finite)
            for (int i = 0; i < vs.Length; i++)
                if (float.IsNaN(vs[i].x) || float.IsNaN(vs[i].y) || float.IsNaN(vs[i].z)
                 || float.IsInfinity(vs[i].x) || float.IsInfinity(vs[i].y) || float.IsInfinity(vs[i].z))
                { finite = false; break; }
        Check(finite, $"B-1：`Card 3D WH40k`.vertices 非空且无 NaN/Inf（{(vs == null ? "null" : vs.Length + " 个")}）");

        // B-2：另一份走 Resources 的网格（`FxObject_cylinder_short`，原版 38 顶点）同一档
        Mesh fx = Resources.Load<Mesh>(CardPresentation.CardArt.Root + "card3d/FxObject_cylinder_short");
        Check(fx != null, "B 前提③：`FxObject_cylinder_short` 拿得到");
        var fv = fx != null ? fx.vertices : null;
        Check(fx != null && fv != null && fv.Length == fx.vertexCount && fv.Length > 0,
              $"B-2：`FxObject_cylinder_short`.vertices 非空（{(fv == null ? "null" : fv.Length + " 个")}）");

        // B-3：`CardView.cs:3792` 那条 `sharedMesh.vertices` 读口**还在**（没被「缓存一个数」替掉 = 没退化成自证）
        string cv = Path.Combine(Application.dataPath, "CardPresentation/Core/CardView.cs");
        bool src = File.Exists(cv)
                   && Regex.IsMatch(Encoding.UTF8.GetString(File.ReadAllBytes(cv)), @"sharedMesh\.vertices");
        Check(src, "B-3：`CardView.cs` 里 `sharedMesh.vertices` 那条读口仍在（不是缓存一个数）");

        // 🔴 **事实订正（铁律 5）**：那条读口读的是 `_badgePlates[i]`，其网格由 `BadgePlateQuad` **运行时 new Mesh 建**出来
        //    （`CardView.cs:4236`）⇒ **它不是这 101 份资产网格之一，四个内部标志管不到它**。
        //    这条断言把「订正成立」本身钉住 —— 免得下一轮又把它当成本活的验收口。
        bool runtimeBuilt = File.Exists(cv)
                            && Regex.IsMatch(Encoding.UTF8.GetString(File.ReadAllBytes(cv)), @"new Mesh \{ name = ""BadgePlateQuad_");
        Check(runtimeBuilt, "B-4（订正）：`CardView.cs:3792` 的 sharedMesh 是运行时 `new Mesh` 建的徽标底板网格，不在这 101 份里");
    }

    // ── C：灭自证 ──────────────────────────────────────────────────────────────

    static void CheckC_SelfProof()
    {
        // 取一份**有 KeepVertices 要求**的样本：`Card 3D WH40k`（原版 = 1/1）
        string asset = "Assets/CardPresentation/Resources/Art/card3d/Card 3D WH40k.asset";
        string src = B140_WS_MeshFlags.FullPath(asset);
        if (!File.Exists(src)) { Check(false, "C 前提：样本文件存在"); return; }

        int[] cur = B140_WS_MeshFlags.ReadFlagsFromFile(src);
        Check(cur != null && cur[2] == 1 && cur[3] == 1,
              $"C 前提：样本 `{asset}` 现在 KeepVertices/KeepIndices = 1/1（{(cur == null ? "读不到" : cur[2] + "/" + cur[3])}）");
        if (cur == null) return;

        string vBefore = B140_WS_MeshFlags.VertexFingerprint(src);

        // 在**临时副本**上把它改回 0（⛔ 不动工程资产）
        string tmpDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/B140_WS_tmp");
        Directory.CreateDirectory(tmpDir);
        string tmp = Path.Combine(tmpDir, "Card 3D WH40k.keepv0.asset");
        File.Copy(src, tmp, true);

        int[] broken = new[] { cur[0], cur[1], 0, 0 };
        if (!B140_WS_MeshFlags.PatchFileFields(tmp, broken, out string err))
        {
            Check(false, "C：临时副本改写失败 —— " + err);
            return;
        }

        // C-1：改回 0 之后，A 的判据**必须**报红（= 这条断言真的看那个字段，不是同义反复）
        int[] got = B140_WS_MeshFlags.ReadFlagsFromFile(tmp);
        bool aWouldBeRed = got == null || got[2] != 1 || got[3] != 1;
        Check(aWouldBeRed, "C-1（灭自证）：KeepVertices/Indices 改回 0/0 ⇒ A 的判据报红");

        // C-2：同一份坏掉的副本里，**顶点照样在盘上** ⇒ 「盘上/编辑器看得见顶点」不能当出包安全的证据
        string vAfter = B140_WS_MeshFlags.VertexFingerprint(tmp);
        Check(vAfter == vBefore,
              "C-2（灭自证）：字段改坏后顶点指纹**不变** ⇒ 编辑器侧证据看不出这个字段，必须出包验");

        // C-3：工程里那份**没被动过**（临时副本的活不许漏到真资产）
        int[] real = B140_WS_MeshFlags.ReadFlagsFromFile(src);
        Check(real != null && real[2] == 1 && real[3] == 1,
              "C-3：工程里那份未被临时副本连带改动（KeepVertices/Indices 仍 1/1）");

        try { File.Delete(tmp); } catch { /* 临时件删不掉不算失败 */ }
    }
}
