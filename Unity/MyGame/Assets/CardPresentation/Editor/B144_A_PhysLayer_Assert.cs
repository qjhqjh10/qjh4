// B144_A_PhysLayer_Assert.cs — 执行写手 `B144-A`（2026-11-05）· 物理与碰撞层域
//
// ============================ 它盯的是什么 ============================
// `报告/B132_审计_物理与碰撞层.md` 里那批「参数不是原版」的格子里，**离线可改的那一半**：
//   ① `ProjectSettings/TagManager.asset` 的层名表 + tags 列表（原版 32 项逐索引）
//   ② arena prefab 的 `CardLowBoardLimit` 层 = 11、`Cache Stealth` 3D 卡体（MeshFilter + BoxCollider，层 10）
//   ③ 独立战场场景那台 `BoardCamera` 的清屏/背景/深度/掩码/HDR/MSAA/遮挡七格
//   ④ 两条已经被别人修好、但**没有断言钉住**的高优先项（回归闸）：`BoardCamera` 的 tag、外壳拖拽阈值 5 px
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//  · 层名表 / tags：`d:/2/tools/assetripper_out/ExportedProject/ProjectSettings/TagManager.asset`（32 项 `layers`）
//  · `CardLowBoardLimit`：`Data/Scenes/Battle Scenarios/Battle Arena *.unity` —— GO `m_Layer: 11`（`BoardCardLimit`）
//  · `Cache Stealth`：同目录 GO `&323`（arena1）—— `m_Layer: 10` · **`m_IsActive: 0`** · `MeshFilter`(mesh guid
//    `3a56ea04…` = `Card 3D WH40k`) + `BoxCollider`(size 1.827034×3.01×0.5 · center (0.013507843,1.55,−0.016031265))
//  · 预览相机：`Battle Arena 1.unity` 的 `Camera &1461` —— `m_ClearFlags 2` · `m_Depth −1` · `m_Bits 1137` ·
//    `m_HDR 1` · `m_AllowMSAA 0` · `m_OcclusionCulling 0` · `m_BackGroundColor (0.028763004, 0.028794324, 0.029411793, 0.019607844)`
//  · tag：原版 `BoardCamera` = `MainCamera`、`UI Camera` = `UICamera`（自定义 tag，`TagManager.tags` 第 4 条）
//  · 拖拽阈值：`Intro.unity:5429` `m_DragThreshold: 5`
//
// ============================ 怎么用 ============================
//   `B144_A_PhysLayer_Assert.Run();`   （静态类 + 无参 `Run()`；**本文件尚未接线**，宿主与聚合行由调度台决定）
//   本件**不建 GameObject、不跑批处理**，只读文本 + 纯静态比较 ⇒ 编辑模式与播放器都能跑。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A1  ← 把任一层名删掉 / 改回空串 ⇒ 红（索引也会对不上）
//   · A2  ← 把 `TagManager.asset` 的 tags 删回 1 条 ⇒ 红
//   · B1/B2 ← 删掉 `ArenaBuilder` 里 `OriginalNonZeroHostLayers` 的刷层例外 ⇒ 下次重建 prefab 后 B1 红
//   · B3  ← 删掉 `RestoreInactiveCardBodies` 的调用 ⇒ 下次重建后 B3 红
//   · C1  ← 预览相机那七格任一格改回 Unity 默认值 ⇒ 红
//   · D1  ← `BoardCamera` 的 tag 改回 `Untagged` ⇒ 红（`Camera.main` 立刻退回 HUD 正交相机）
//   · D2  ← `PointerLayer.DragThreshold` 改回 10 ⇒ 红
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环**：本件不证明「点得中 / 画得出」，只证明「磁盘上的值 = 原版值」。
//   · **B/C 两段读的是【产物】**（prefab / 场景 YAML）：代码改了但**没重建**时它们会红 ——
//     那是「该重建了」的信号，不是缺陷（见每条文案里的「待 Unity 重建」）。
//   · **不断言 `Battle.unity` 的相机掩码**：那台是 `BattleScene.cs` 的产物（归 B139-S / 调度台），本轮只钉 tag。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>执行写手 `B144-A`：物理与碰撞层的原版参数（层名表 / arena 碰撞体层 / 预览相机 / tag / 拖拽阈值）。</summary>
public static class B144_A_PhysLayer_Assert
{
    const string Prefix = "[B144A] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B144-A：物理与碰撞层（B132 审计离线可改项）开始 ===");
        A_TagManager();
        B_ArenaPrefabColliders();
        C_PreviewCamera();
        D_RegressionTags();
        E_PhysSettingsAndRaycaster();
        F_TextAndNav();
        Debug.Log(Prefix + "—— 合计：通过 " + _ok + " · 失败 " + _bad);
    }

    // ==================================================================
    //  A · 层名表 + tags（原版 `TagManager.asset` 逐索引）
    // ==================================================================
    /// <summary>原版的 8 个有名字的层（索引 → 名字）。⚠️ 只列 B132 审计覆盖到的那 8 项 ——
    /// 原版还有 `Default/TransparentFX/Ignore Raycast/Water/UI` 五条，那几条我们本来就同值（B132 #4/#10/#11 ✅）。</summary>
    static readonly int[] LayerIdx = { 6, 8, 9, 10, 11, 12, 27, 30 };
    static readonly string[] LayerName = { "AfterTransparents", "CustomPostEffectIgnore", "2DElements", "BoardElements",
                                           "BoardCardLimit", "ParticleTapCollider", "Blocking", "GroundCollider" };

    /// <summary>原版 `tags:` 六条（顺序也是逐条照原版 —— 顺序影响 Inspector 里的下拉，不影响行为）。</summary>
    static readonly string[] OrigTags = { "particle", "FloorPositionReference", "PopUpAnchor", "UICamera", "EditorPreview", "MainCanvas" };

    static void A_TagManager()
    {
        string p = Path.Combine(Application.dataPath, "../ProjectSettings/TagManager.asset");
        p = Path.GetFullPath(p);
        Check(File.Exists(p), "A0（前提）`TagManager.asset` 在：" + p + "（⛔ 读不到就红，别改成跳过）");
        if (!File.Exists(p)) return;

        string txt = File.ReadAllText(p);
        var layers = ParseLayers(txt);
        Check(layers != null && layers.Count == 32,
            "A0b（前提）`layers` 段能解出 **32** 项（实得 " + (layers == null ? -1 : layers.Count) + "）");
        if (layers == null || layers.Count != 32) return;

        for (int i = 0; i < LayerIdx.Length; i++)
        {
            int idx = LayerIdx[i];
            string want = LayerName[i];
            Check(layers[idx] == want,
                "A1 ★ 层 " + idx + " = `" + want + "`（实得 `" + layers[idx] + "`）"
                + "｜判据 = 原版 `TagManager.asset` `layers[" + idx + "]`｜🧨 改成空串 / 改回旧名（层 30 原来叫 `WarpforgeIso`）⇒ 红");
        }

        var tags = ParseTags(txt);
        bool tagsOk = tags != null && tags.Count == OrigTags.Length;
        for (int i = 0; tagsOk && i < OrigTags.Length; i++) tagsOk = tags[i] == OrigTags[i];
        Check(tagsOk,
            "A2 ★ `tags:` = 原版 6 条、顺序一致（实得 " + (tags == null ? "-" : tags.Count + " 条：" + string.Join("/", tags.ToArray())) + "）"
            + "｜🧨 删回 1 条 ⇒ 红（`UICamera` tag 是原版 `UI Camera` 的；少了它就挂不上）");

        // 运行时视图（灭自证：文本对了但 Unity 没重新导入时，NameToLayer 会露馅）
        for (int i = 0; i < LayerIdx.Length; i++)
        {
            int got = LayerMask.NameToLayer(LayerName[i]);
            Check(got == LayerIdx[i],
                "A3 ★ `LayerMask.NameToLayer(\"" + LayerName[i] + "\")` = " + LayerIdx[i] + "（实得 " + got + "）");
        }
    }

    static List<string> ParseLayers(string txt)
    {
        var lines = txt.Replace("\r\n", "\n").Split('\n');
        int i = Array.IndexOf(lines, "  layers:");
        if (i < 0) return null;
        var outp = new List<string>();
        for (i++; i < lines.Length && lines[i].StartsWith("  -"); i++)
        {
            string v = lines[i].Substring(3).Trim();
            outp.Add(v == "-" ? "" : v);      // `  - ` 后面什么都没有 ⇒ 空层名
        }
        return outp;
    }

    static List<string> ParseTags(string txt)
    {
        var lines = txt.Replace("\r\n", "\n").Split('\n');
        int i = Array.IndexOf(lines, "  tags:");
        if (i < 0) return null;
        var outp = new List<string>();
        for (i++; i < lines.Length && lines[i].StartsWith("  -"); i++) outp.Add(lines[i].Substring(3).Trim());
        return outp;
    }

    // ==================================================================
    //  B · arena prefab 的两颗碰撞体（`CardLowBoardLimit` 层 11 / `Cache Stealth` 3D 卡体）
    // ==================================================================
    static readonly string[] AllArenas =
    {
        "battlearena1", "battlearena2", "battlearena3", "battlearenaaeldari", "battlearenaastramilitarum",
        "battlearenablacklegion", "battlearenadarkangels", "battlearenaemperorschildren", "battlearenagenestealers",
        "battlearenaleviathan", "battlearenasororitas", "battlearenaspacewolves", "battlearenatauviorla",
    };

    /// <summary>一份 Unity YAML 里的一块文档。</summary>
    struct Doc { public int Cls; public string Fid; public string Body; }

    static List<Doc> ParseDocs(string txt)
    {
        var outp = new List<Doc>();
        var lines = txt.Replace("\r\n", "\n").Split('\n');
        int start = -1; int cls = 0; string fid = null;
        for (int i = 0; i <= lines.Length; i++)
        {
            bool end = i == lines.Length;
            if (end || lines[i].StartsWith("--- !u!"))
            {
                if (start >= 0) outp.Add(new Doc { Cls = cls, Fid = fid, Body = string.Join("\n", lines, start, i - start) });
                if (end) break;
                int up = lines[i].IndexOf("!u!", StringComparison.Ordinal);
                int amp = lines[i].IndexOf(" &", StringComparison.Ordinal);
                if (up >= 0 && amp > up)
                {
                    int.TryParse(lines[i].Substring(up + 3, amp - up - 3).Trim(), out cls);
                    fid = lines[i].Substring(amp + 2).Trim();
                }
                else { cls = 0; fid = null; }
                start = i;
            }
        }
        return outp;
    }

    static string Field(string body, string key)
    {
        foreach (var l in body.Replace("\r\n", "\n").Split('\n'))
        {
            string t = l.Trim();
            if (t.StartsWith(key + ":", StringComparison.Ordinal)) return t.Substring(key.Length + 1).Trim();
        }
        return null;
    }

    /// <summary>按 `m_Name` 找 GameObject，返回它的 `m_Layer` 与「组件类号列表」。</summary>
    static bool FindGo(List<Doc> docs, string name, out int layer, out List<int> componentClasses)
    {
        layer = -999; componentClasses = null;
        for (int i = 0; i < docs.Count; i++)
        {
            if (docs[i].Cls != 1) continue;
            if (Field(docs[i].Body, "m_Name") != name) continue;
            int L; if (!int.TryParse(Field(docs[i].Body, "m_Layer"), out L)) return false;
            layer = L;
            componentClasses = new List<int>();
            var body = docs[i].Body.Replace("\r\n", "\n");
            bool inComp = false;
            foreach (var l in body.Split('\n'))
            {
                if (l.Trim() == "m_Component:") { inComp = true; continue; }
                if (inComp)
                {
                    if (!l.TrimStart().StartsWith("- component:", StringComparison.Ordinal)) { inComp = false; continue; }
                    int a = l.IndexOf("{fileID: ", StringComparison.Ordinal);
                    if (a < 0) continue;
                    a += 9;
                    int b = l.IndexOf('}', a);
                    if (b <= a) continue;
                    string cfid = l.Substring(a, b - a).Trim();
                    for (int k = 0; k < docs.Count; k++)
                        if (docs[k].Fid == cfid) { componentClasses.Add(docs[k].Cls); break; }
                }
            }
            return true;
        }
        return false;
    }

    static string PrefabPath(string arena)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "Resources/ArenaPrefabs/" + arena + ".prefab"));
    }

    static void B_ArenaPrefabColliders()
    {
        int nLimLayer = 0, nLimBox = 0, nCacheMesh = 0, nCacheBox = 0, nCacheLayer = 0, nCacheInactive = 0, nFiles = 0;
        var bad = new List<string>();
        foreach (var arena in AllArenas)
        {
            string p = PrefabPath(arena);
            if (!File.Exists(p)) { bad.Add(arena + "：**prefab 不存在**"); continue; }
            nFiles++;
            var docs = ParseDocs(File.ReadAllText(p));

            // ---- `CardLowBoardLimit`：原版层 11 + 一颗 BoxCollider（尺寸 1×1×1、center 0）----
            int L; List<int> comps;
            if (!FindGo(docs, "CardLowBoardLimit", out L, out comps)) bad.Add(arena + "：找不到 `CardLowBoardLimit`");
            else
            {
                if (L == 11) nLimLayer++; else bad.Add(arena + "：`CardLowBoardLimit` 层 = " + L + "（应为 11 = `BoardCardLimit`）");
                if (comps.Contains(65)) nLimBox++; else bad.Add(arena + "：`CardLowBoardLimit` 没有 `BoxCollider`（class 65）");
            }

            // ---- `Cache Stealth`：原版层 10 · GO 自己关着 · MeshFilter + BoxCollider 逐值 ----
            if (!FindGo(docs, "Cache Stealth", out L, out comps)) bad.Add(arena + "：找不到 `Cache Stealth`");
            else
            {
                if (L == 10) nCacheLayer++; else bad.Add(arena + "：`Cache Stealth` 层 = " + L + "（应为 10 = `BoardElements`）");
                if (comps.Contains(33)) nCacheMesh++; else bad.Add(arena + "：`Cache Stealth` 没有 `MeshFilter`（class 33）");
                if (comps.Contains(65)) nCacheBox++; else bad.Add(arena + "：`Cache Stealth` 没有 `BoxCollider`（class 65）");
                bool inactive = false;
                for (int i = 0; i < docs.Count; i++)
                {
                    if (docs[i].Cls != 1) continue;
                    if (Field(docs[i].Body, "m_Name") != "Cache Stealth") continue;
                    inactive = Field(docs[i].Body, "m_IsActive") == "0";
                    break;
                }
                if (inactive) nCacheInactive++;
                else bad.Add(arena + "：`Cache Stealth` 的 `m_IsActive` **不是 0** —— 原版 13/13 场都是关着的"
                                 + "（关着 ⇒ 这颗碰撞体不参与物理；开起来就多出一颗只该在 3D 卡上的挡板）");
            }
        }
        for (int i = 0; i < bad.Count; i++) Debug.LogError(Prefix + "✗ " + bad[i]);

        Check(nFiles == AllArenas.Length, "B0（前提）13 件 arena prefab 都在（实得 " + nFiles + "）");
        Check(nLimLayer == AllArenas.Length && nLimBox == AllArenas.Length,
            "B1 ★ 13/13 `CardLowBoardLimit` 层 = 11（" + nLimLayer + "/13）且有 `BoxCollider`（" + nLimBox + "/13）"
            + "｜🧨 删掉 `ArenaBuilder` 刷层段里的 `OriginalNonZeroHostLayers` 例外 + 重建 ⇒ 全部回 8 ⇒ 红（⚠️ 待 Unity 重建）");
        Check(nCacheLayer == AllArenas.Length && nCacheMesh == AllArenas.Length && nCacheBox == AllArenas.Length,
            "B3 ★ 13/13 `Cache Stealth` 层 = 10（" + nCacheLayer + "/13）· 有 `MeshFilter`（" + nCacheMesh + "/13）"
            + "· 有 `BoxCollider`（" + nCacheBox + "/13）｜原版 GO `&323` 是 `Transform+MeshRenderer+MeshFilter+BoxCollider`"
            + "｜🧨 删掉 `RestoreInactiveCardBodies` 的调用 + 重建 ⇒ 只剩 Transform ⇒ 红（⚠️ 待 Unity 重建）");
        Check(nCacheInactive == AllArenas.Length,
            "B4 ★ 13/13 `Cache Stealth` 的 `m_IsActive: 0`（" + nCacheInactive + "/13）—— 原版就是关着的死件，⛔ 别顺手打开");

        // ---- 源码闸：`ArenaBuilder` 里那两处接线必须还在 ----
        string ab = Path.GetFullPath(Path.Combine(Application.dataPath, "WarpforgeArena1/Editor/ArenaBuilder.cs"));
        if (File.Exists(ab))
        {
            string src = File.ReadAllText(ab);
            Check(src.Contains("OriginalNonZeroHostLayers(mf)") && src.Contains("_origHostLayersByScene[sceneName]"),
                "B5 ★ `ArenaBuilder.BuildOneArenaPrefab` 仍按清单保留原版非零层（`OriginalNonZeroHostLayers` 被用到）");
            Check(src.Contains("RestoreInactiveCardBodies(root.transform, sceneName)"),
                "B6 ★ `ArenaBuilder.BuildOneArenaPrefab` 仍补建 `Cache Stealth` 的 3D 卡体（`RestoreInactiveCardBodies` 被调用）");
        }
        else Debug.LogError(Prefix + "✗ B5/B6 读不到 `ArenaBuilder.cs`（" + ab + "）");
    }

    // ==================================================================
    //  C · 独立战场场景那台 `BoardCamera`（B132 #36/#41/#42/#43）
    // ==================================================================
    static void C_PreviewCamera()
    {
        int nCam = 0, nMask = 0, nClear = 0, nDepth = 0, nHdr = 0, nMsaa = 0, nOccl = 0;
        var bad = new List<string>();
        foreach (var arena in AllArenas)
        {
            string p = Path.GetFullPath(Path.Combine(Application.dataPath, "WarpforgeArena1/Scenes/" + arena + ".unity"));
            if (!File.Exists(p)) { bad.Add(arena + "：**场景不存在**"); continue; }
            var docs = ParseDocs(File.ReadAllText(p));
            bool found = false;
            for (int i = 0; i < docs.Count; i++)
            {
                if (docs[i].Cls != 20) continue;   // Camera
                found = true;
                string s = docs[i].Body;
                int mask; int.TryParse(Field(s, "m_Bits"), out mask);
                if (mask == 1137) nMask++; else bad.Add(arena + "：相机 `m_Bits` = " + mask + "（应为 1137）");
                if (Field(s, "m_ClearFlags") == "2") nClear++; else bad.Add(arena + "：`m_ClearFlags` = " + Field(s, "m_ClearFlags") + "（应为 2 = SolidColor）");
                if (Field(s, "m_Depth") == "-1") nDepth++; else bad.Add(arena + "：`m_Depth` = " + Field(s, "m_Depth") + "（应为 −1）");
                if (Field(s, "m_HDR") == "1") nHdr++; else bad.Add(arena + "：`m_HDR` = " + Field(s, "m_HDR") + "（应为 1）");
                if (Field(s, "m_AllowMSAA") == "0") nMsaa++; else bad.Add(arena + "：`m_AllowMSAA` = " + Field(s, "m_AllowMSAA") + "（应为 0）");
                if (Field(s, "m_OcclusionCulling") == "0") nOccl++; else bad.Add(arena + "：`m_OcclusionCulling` = " + Field(s, "m_OcclusionCulling") + "（应为 0）");
                nCam++;
            }
            if (!found) bad.Add(arena + "：场景里**没有 Camera**");
        }
        for (int i = 0; i < bad.Count; i++) Debug.LogError(Prefix + "✗ " + bad[i]);
        Check(nCam == AllArenas.Length, "C0（前提）13 个独立战场场景各有 1 台相机（实得 " + nCam + "）");
        Check(nMask == nCam && nClear == nCam && nDepth == nCam && nHdr == nCam && nMsaa == nCam && nOccl == nCam,
            "C1 ★ 13/13 预览相机七格 = 原版 `Camera &1461`（掩码 1137 " + nMask + " · ClearFlags 2 " + nClear
            + " · Depth −1 " + nDepth + " · HDR 1 " + nHdr + " · MSAA 0 " + nMsaa + " · Occl 0 " + nOccl + "）"
            + "｜🧨 删掉 `ArenaBuilder.BuildSceneTail` 里那七行 + 重建 ⇒ 全部回 `Camera` 构造默认值 ⇒ 红（⚠️ 待 Unity 重建）");
    }

    // ==================================================================
    //  D · 已经修好但没有断言钉住的高优先项（回归闸）
    // ==================================================================
    static void D_RegressionTags()
    {
        string p = Path.GetFullPath(Path.Combine(Application.dataPath, "CardPresentation/Scenes/Battle.unity"));
        Check(File.Exists(p), "D0（前提）`Battle.unity` 在：" + p);
        if (File.Exists(p))
        {
            var docs = ParseDocs(File.ReadAllText(p));
            string board = null, ui = null;
            for (int i = 0; i < docs.Count; i++)
            {
                if (docs[i].Cls != 1) continue;
                string nm = Field(docs[i].Body, "m_Name");
                // ✅ 已复刻（2026-11-05 · B150 · 原版名带中文，Unity YAML 会给整串加引号 ⇒ 先剥引号再 StartsWith）
                if (nm != null && nm.Trim('"').StartsWith("BoardCamera", StringComparison.Ordinal)) board = Field(docs[i].Body, "m_TagString");
                if (nm == "UI Camera") ui = Field(docs[i].Body, "m_TagString");
            }
            Check(board == "MainCamera",
                "D1 ★ `BoardCamera` 的 tag = `MainCamera`（实得 `" + board + "`）｜判据 = 原版 GO `&348`"
                + "｜🧨 改回 `Untagged` ⇒ `Camera.main` 退回 HUD 正交相机 ⇒ `TapParticleController` 的地面射线打空（B132 #37/#92）");
            Check(ui == "UICamera",
                "D2 ★ `UI Camera` 的 tag = `UICamera`（实得 `" + ui + "`）｜判据 = 原版 GO `&350`（自定义 tag，`TagManager.tags` 第 4 条）"
                + "｜🧨 两台对调 / 改回 `MainCamera` ⇒ 红");
        }

        Check(Math.Abs(CardPresentation.PointerLayer.DragThreshold - 5f) < 1e-4f,
            "D3 ★ `PointerLayer.DragThreshold` = 5 px（实得 " + CardPresentation.PointerLayer.DragThreshold
            + "）｜判据 = 原版 `Intro.unity:5429` `m_DragThreshold: 5`｜🧨 改回 uGUI 默认 10 ⇒ 红（外壳拖拽比原版钝一倍，B132 #99）");

        int mask = CardPresentation.TapParticleController.CollisionMask;
        Check(mask == unchecked((int)0x40000D01),
            "D4 ★ `TapParticleController.CollisionMask` = `0x40000D01`（实得 `0x" + mask.ToString("X8") + "`）"
            + "｜= 原版 `0x40000C01`（层 0/10/11/30）+ 🔶 本工程 bit8（arena 整树在层 8）；"
            + "bit10 显式绑 `BoardCardLayer`｜🧨 去掉 bit8 ⇒ 165 颗 tap 宿主一颗都命不中（B132 #90）");
    }

    // ==================================================================
    //  E · `DynamicsManager` 两键（B132 ❓#30/#31）+ `EverguildGraphicRaycaster`（B132 #82）
    // ==================================================================
    static void E_PhysSettingsAndRaycaster()
    {
        // ---- E1/E2：原版 `DynamicsManager.asset:10` `m_DefaultMaxDepenetrationVelocity: 10` ·
        //      `:24` `m_InvokeCollisionCallbacks: 1`（这两格 B132 当时判 ❓「Unity 默认值查不到」，
        //      本轮在原版文件里直接读到了序列化值）
        string dm = Path.GetFullPath(Path.Combine(Application.dataPath, "../ProjectSettings/DynamicsManager.asset"));
        Check(File.Exists(dm), "E0（前提）`DynamicsManager.asset` 在：" + dm);
        if (File.Exists(dm))
        {
            string txt = File.ReadAllText(dm);
            Check(txt.Contains("m_DefaultMaxDepenetrationVelocity: 10"),
                "E1 ★ 3D `m_DefaultMaxDepenetrationVelocity: 10`（原版同值）"
                + "｜🧨 删掉这一键 / 改值 ⇒ 红｜⚠️ Unity 下次重写该文件时若把它丢掉，本条会红 = 该键在本 Editor 版本不被识别");
            Check(txt.Contains("m_InvokeCollisionCallbacks: 1"),
                "E2 ★ 3D `m_InvokeCollisionCallbacks: 1`（原版同值）"
                + "｜🧨 删掉这一键 / 改值 ⇒ 红｜⚠️ 同上");
        }

        // ---- E3：`HudCanvas` 两处 AddComponent 必须用原版类（⛔ 不是基类 `GraphicRaycaster`）
        string hc = Path.GetFullPath(Path.Combine(Application.dataPath, "CardPresentation/Battle/HudCanvas.cs"));
        Check(File.Exists(hc), "E0b（前提）`HudCanvas.cs` 在：" + hc);
        if (File.Exists(hc))
        {
            string src = File.ReadAllText(hc);
            int nSub = CountOccurrences(src, "AddComponent<EverguildGraphicRaycaster>()");
            int nBase = CountOccurrences(src, "AddComponent<GraphicRaycaster>()");
            Check(nSub == 2 && nBase == 0,
                "E3 ★ `HudCanvas` 两处 raycaster 都用 `EverguildGraphicRaycaster`（实得 " + nSub + " 处；基类 " + nBase + " 处）"
                + "｜判据 = 原版 `Battle Arena 1.unity` 的 4 个实例 script guid `ab00d768…`"
                + "｜🧨 任一处换回基类 ⇒ 那一台的 `sortOrderPriority` 变 `int.MinValue` ⇒ 排序垫底 ⇒ 红（B132 #82）");
        }

        // ---- E4：覆写本身在（灭自证：删了覆写 ⇒ 与基类同值）
        var probe = new GameObject("B144A_Probe");
        try
        {
            var gr = probe.AddComponent<CardPresentation.EverguildGraphicRaycaster>();
            Check(gr.sortOrderPriority == 0,
                "E4 ★ `EverguildGraphicRaycaster.sortOrderPriority` = 0（实得 " + gr.sortOrderPriority + "）"
                + "｜🧨 删掉那个 override ⇒ 回基类 `int.MinValue` ⇒ 红");
            Check(gr.sortOrderPriority != int.MinValue,
                "E4b ★ 不等于基类默认 `int.MinValue`（这是一条**结构性**判别式：改回基类实现也躲不掉）");
        }
        finally { UnityEngine.Object.DestroyImmediate(probe); }
    }

    static int CountOccurrences(string hay, string needle)
    {
        if (string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = hay.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    // ==================================================================
    //  F · `INPUT-BACKEND-MIGRATE` 第一拍：打字焦点（`InputTextFrame`）+ 导航轴（`GetAxisRaw`）
    // ==================================================================
    static void F_TextAndNav()
    {
        // ---- F1–F4：`InputTextFrame` 的三条契约（同一帧只取一次 / 只有焦点能取 / 换焦点重取）----
        object a = new object(), b = new object();
        try
        {
            CardPresentation.InputTextFrame.Claim(a);
            CardPresentation.InputTextFrame.SeedForTest("abc");
            Check(CardPresentation.InputTextFrame.Take(a) == "abc",
                "F1 ★ 焦点 A 取到本帧的字 \"abc\"｜🧨 `Take` 不按 owner 判 / 不去缓冲取 ⇒ 红");
            Check(CardPresentation.InputTextFrame.Take(a) == "",
                "F2 ★ **同一帧第二次取 = 空串**（取完清空）｜🧨 删掉 `_buf = \"\"` ⇒ 每次取都拿到同一串 ⇒ 一个字变两个 ⇒ 红");
            Check(CardPresentation.InputTextFrame.Take(b) == "",
                "F3 ★ **非焦点 B 一个字符都拿不到**｜🧨 删掉 `if (!ReferenceEquals(_owner, owner)) return \"\";` ⇒ B 也能拿 ⇒ **两个输入框双打** ⇒ 红");

            CardPresentation.InputTextFrame.Claim(b);
            CardPresentation.InputTextFrame.SeedForTest("xyz");
            Check(CardPresentation.InputTextFrame.Take(a) == "" && CardPresentation.InputTextFrame.Take(b) == "xyz",
                "F4 ★ 换焦点后：旧焦点 A 拿空、新焦点 B 拿到 \"xyz\"（`Claim` 复位帧号）｜🧨 `Claim` 不复位 ⇒ 本帧沿用旧缓冲 ⇒ 红");

            CardPresentation.InputTextFrame.Release(a);      // 不是持有者 ⇒ 不生效
            Check(CardPresentation.InputTextFrame.IsOwner(b),
                "F5 ★ 非持有者 `Release` **不会**把当前焦点清掉｜🧨 去掉 `ReferenceEquals` 守卫 ⇒ 红");
            CardPresentation.InputTextFrame.Release(b);
            Check(!CardPresentation.InputTextFrame.IsOwner(b),
                "F6 ★ 持有者 `Release` 之后不再拥有焦点");
        }
        finally { CardPresentation.InputTextFrame.Release(a); CardPresentation.InputTextFrame.Release(b); }

        // ---- F7：三个打字入口**都不再有** `onTextInput` 事件（源码闸，注释里提到不算）----
        string[] textFiles = { "CardPresentation/Shell/PointerLayer.cs", "CardPresentation/Deck/DeckRuntime.cs", "CardPresentation/Shell/TextInputField.cs" };
        for (int i = 0; i < textFiles.Length; i++)
        {
            string p = Path.GetFullPath(Path.Combine(Application.dataPath, textFiles[i]));
            Check(File.Exists(p), "F7-[" + i + "]（前提）在：" + p);
            if (!File.Exists(p)) continue;
            int n = CountOccurrences(StripLineComments(File.ReadAllText(p)), "onTextInput");
            Check(n == 0,
                "F7-[" + i + "] ★ `" + Path.GetFileName(p) + "` 的**代码**里没有 `onTextInput`（实得 " + n + "）"
                + "｜🧨 谁把事件订阅加回来 ⇒ 再在 `Take` 里取一遍 ⇒ 红（正是「一个字变两个」那条）");
        }

        // ---- F8：导航轴改成了 `GetAxisRaw`（调度台 2026-11-05 裁定：不是 `GetAxis`）----
        string pl = Path.GetFullPath(Path.Combine(Application.dataPath, "CardPresentation/Shell/PointerLayer.cs"));
        if (File.Exists(pl))
        {
            string src = StripLineComments(File.ReadAllText(pl));
            Check(CountOccurrences(src, "Input.GetAxisRaw(\"Horizontal\")") == 1
               && CountOccurrences(src, "Input.GetAxisRaw(\"Vertical\")") == 1,
                "F8 ★ `PointerLayer` 导航轴 = `Input.GetAxisRaw(\"Horizontal\"/\"Vertical\")` 各 1 处");
            Check(CountOccurrences(src, "Input.GetAxis(") == 0,
                "F8b ★ **没有** `Input.GetAxis(` —— 那条会过平滑/死区，会改掉「键盘量 + 手柄量」的原始加和语义"
                + "｜🧨 改成 `GetAxis` ⇒ 红（调度台 2026-11-05 裁定用 `GetAxisRaw`）");
            Check(CountOccurrences(src, "Input.GetButtonDown(\"Submit\")") == 1
               && CountOccurrences(src, "Input.GetButtonDown(\"Cancel\")") == 1,
                "F8c ★ `Submit`/`Cancel` 走 `Input.GetButtonDown`（原版 `EverguildInput` 读的就是这两条轴名）");
        }
    }

    /// <summary>去掉行注释（`//` 之后）与块注释（`/* … */`）—— 源码闸只认代码，注释里提到旧写法不算接线。</summary>
    // ✅ 已复刻（2026-11-05 · B150 · 原实现只剥 `//`，`DeckRuntime.cs:7237` 的 `onTextInput` 在块注释里 ⇒ 补 `/* */`）
    static string StripLineComments(string src)
    {
        if (string.IsNullOrEmpty(src)) return "";
        var sb = new System.Text.StringBuilder(src.Length);
        bool inBlock = false;
        for (int i = 0; i < src.Length; i++)
        {
            if (inBlock)
            {
                if (src[i] == '*' && i + 1 < src.Length && src[i + 1] == '/') { inBlock = false; i++; }
                else if (src[i] == '\n') sb.Append('\n');
                continue;
            }
            if (src[i] == '/' && i + 1 < src.Length && src[i + 1] == '/')
            {
                while (i < src.Length && src[i] != '\n') i++;
                if (i < src.Length) sb.Append('\n');
                continue;
            }
            if (src[i] == '/' && i + 1 < src.Length && src[i + 1] == '*') { inBlock = true; i++; continue; }
            sb.Append(src[i]);
        }
        return sb.ToString();
    }
}
