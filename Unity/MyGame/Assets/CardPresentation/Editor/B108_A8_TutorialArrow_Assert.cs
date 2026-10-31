// B108_A8_TutorialArrow_Assert.cs — 执行代理 `B108_A8`（第一百零八会话 · 波 1 · AUD09 §四 的裁定落地）
//
// ============================ 它盯的是哪一条 ============================
//  `资料/完全复刻/判据/AUD09_资源落地_第2轮.md` §四 挂的那一条：
//    `Battle/TutorialOverlay.cs` 的教学**左右箭头**贴图是 `🔶 自己设计（非原版）`，
//    注释自己写着「原版两颗 `SpriteRenderer` 的 `m_Sprite` 是真·空引用」。
//  按铁律 18·0（自设计只允许「服务器相关」）⇒ 判据拿得到（原版 = 空引用）就不许自设计。
//
// ============================ 裁定（原文 → `报告/B108_A8.md`）============================
//  **原版确实没有图**（不是「远端没下发」）⇒ 走「删掉我们画的箭头」那一支。
//  本代理**独立复核**了三条链（⛔ 没照抄 `B103_R9.md` 的结论）：
//    ① 本地 `assets_full/bundle_scenes_scenes_battlearena1/SpriteRenderer/SpriteRenderer_1760/1761/1762/1763.json`
//       的 `m_Sprite = {m_FileID:0, m_PathID:0}` 且 `m_WasSpriteAssigned = false`；
//       同文件 `SpriteRenderer_1754` 是 `{m_FileID:10, m_PathID:6814035871592514908}` + `m_WasSpriteAssigned = true`（**非 0 反例**）。
//    ② 安卓 1.35.0 远端包：`XAPK`（d:/2，只读）→ `UnityDataAssetPack.apk`（CRC `0x4f53bc16` 两侧一致）
//       → `assets/aa/Android/scenes_scenes_battlearena1.bundle`（CRC `0x44be256d` 两侧一致）
//       → 同 pathID `1760/1761/1762/1763` **仍是** `None` + `wasAssigned=false`，反例 `1754` 仍是 `(10, 6814…)`。
//       ⇒ **排除「远端 Addressables 没下发」**。
//    ③ 13 个战场场景 YAML（`assetripper_out/…/Battle Scenarios/*.unity`）逐场现扫：所有 `arrow1*`/`arrow2` 全是 `{fileID: 0}`。
//       反编译 `TutorialTipScript__SetupArrows.c` 只做 `SetSpriteAlpha` + `SetActive` + `DOFade`；
//       `il2csharp` 的 `TutorialTipScript.cs` 里**没有任何 `.sprite =`** ⇒ 原版任何时刻都不给箭头贴图。
//  ⇒ 我们那两颗（`40k_general_bt_arrow`）已由 `B105_W1`（2026-10-31）**整层删除**；本文件把它**钉死**。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `B108_A8_TutorialArrow_Assert.Run();`
//  宿主里两行（同族先例 `B94Y5_Assert` / `B108_A2_PlayerChain_Assert`）：
//    `B108_A8_TutorialArrow_Assert.Run(); pass += B108_A8_TutorialArrow_Assert.Pass; fail += B108_A8_TutorialArrow_Assert.Fail;`
//  · 世界档（`TutorialOverlay.Create(root.transform)` 传 `uguiHost: null`）—— 本件的断言全是「树里有没有那个对象」，
//    与画布档无关，但世界档建件最省。⚠️ 世界档下 `UseHost(null)` 会**故意** `Debug.LogError` 一句
//    （本工程红线：不许静默失败）—— 那是**预期日志**，不计入失败（失败只按 `CheckSink` 的 `✗` 数）。
//  · 临时根在 `finally` 里 `DestroyImmediate`；不建持久资产、不切场景、不改静态状态。
//
// ============================ 🔴 灭自证（每条都把「改哪一处会红」写进文案）============================
//  · A2：把 `tutorial_stages.json` 里任一条 `showLeftArrow/showRightArrow` 改成 `true` ⇒ A2 红
//    （A1 是它的前提：字段总数必须 > 0，否则「0 个 true」是空转）。
//  · B2/B3：把 `TutorialOverlay.Build` 里那段建箭头代码（或用别的名字）加回来 ⇒ 红。
//  · B4：`SetTip(showLeftArrow:true, showRightArrow:true)` 之后**新长出任何子对象** ⇒ B4 红
//    （防的正是「改名重建」—— 只数 `arrow1`/`arrow2` 会漏）。
//  · C1-C4：把 `ArrowLeft/RightVisible` 改成 `true` / `ArrowLeft/RightAlpha` 改成别的数 ⇒ 红。
//  · D1-D4：把 `_arrowL` / `_arrowR` / `SetArrow(` / `ArrowLPos` / `ArrowRPos` **写回代码** ⇒ 红
//    （D 先剥掉 `//` 注释再查 —— 历史留痕里这些词还在，⛔ 那不是活代码）。
using System;
using System.IO;
using System.Text.RegularExpressions;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B108_A8`：把「原版没有教学左右箭头」这条裁定钉死。
/// <para>⛔ 期望值全是字面量 / 树上的真实对象，**不从被测实现里读回来**。</para></summary>
public static class B108_A8_TutorialArrow_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B108A8] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void Eq<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    const string SrcOverlay = "CardPresentation/Battle/TutorialOverlay.cs";
    const string DataStages = "RuleEngine/Resources/tutorial_stages.json";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B108_A8 · 教学左右箭头：原版 `m_Sprite` 真·空引用（本地 + 安卓 1.35.0 双证）⇒ 我们整层不建");
        SectionA_DataGate();
        SectionB_RuntimeTree();
        SectionC_ReadPorts();
        SectionD_SourceGuard();
        Debug.Log(_sink.P + "B108_A8 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 数据侧那道「可见闸」
    static void SectionA_DataGate()
    {
        MenuCheck.Section(_sink, "A · 数据侧：`tutorial_stages.json` 的箭头开关必须全是 false（否则「零可见」这个前提就变了）");

        string txt = ReadAsset(DataStages);
        Tr(txt.Length > 10000, "A0 **前提**：`" + DataStages + "` 读得到（长度 > 10000）");
        if (txt.Length <= 10000) return;

        int left = Count(txt, "\"showLeftArrow\"");
        int right = Count(txt, "\"showRightArrow\"");
        int leftTrue = CountTrue(txt, "\"showLeftArrow\"");
        int rightTrue = CountTrue(txt, "\"showRightArrow\"");

        Tr(left > 0 && left == right,
           "A1 **前提**：两个开关都真在数据里（左 " + left + " 条 / 右 " + right + " 条 —— 两侧相等才说明取法没漏一边）");
        Eq(leftTrue, 0, "A2 `showLeftArrow` 为 true 的条数 = 0（R2 §4 实测 0；⚠️ 一旦有 true，这条提示在原版里也画不出东西，"
                      + "但前提口径就变了 ⇒ 必须回来重读本裁定）");
        Eq(rightTrue, 0, "A3 `showRightArrow` 为 true 的条数 = 0");
    }

    // ============================================================ B · 运行时对象树（真正的灭自证）
    static void SectionB_RuntimeTree()
    {
        MenuCheck.Section(_sink, "B · 运行时树：建出来的教程根里**不许**有任何 `TutorialArrows` / `arrow1` / `arrow2`");

        var root = new GameObject("B108A8_Root");
        try
        {
            var ov = TutorialOverlay.Create(root.transform);   // 世界档（uguiHost = null）
            if (ov == null)
            {
                Tr(false, "B0 **前提**：`TutorialOverlay.Create` 交回非空实例");
                return;
            }

            // 🔴 前提（硬检查）：根真的建出来了 —— `ArrowObjectCountForTest` 在根没建时返回 **-1**
            //    （那是「不知道」，⛔ 不是「没有箭头」）。先把它 = 0 断出来，否则下面的 0 是空转。
            Eq(ov.ArrowObjectCountForTest, 0,
               "B1 `Build()` 之后根下 `arrow1`+`arrow2` 的对象数 = 0（-1 = 根没建出来，同样判红）");

            int arrowsNode = CountNamed(ov.transform, "TutorialArrows");
            Eq(arrowsNode, 0, "B2 整棵子树里**没有** `TutorialArrows` 这个节点（原版那层我们整层不建）");

            int before = CountArrowish(ov.transform);

            // 🔴 这一条是「数据说 true 也不画」的闸：即使把两个形参都传 true，也不许长出任何对象。
            ov.SetTip(true, "B108_A8 probe", false, Vector3.zero, 0f, TutRelation.RightOf,
                      showLeftArrow: true, showRightArrow: true);

            Eq(ov.ArrowObjectCountForTest, 0,
               "B3 `SetTip(showLeftArrow:true, showRightArrow:true)` 之后仍然 0 个箭头对象"
             + "｜🧨 把建箭头那段加回来 ⇒ 实得 2 ⇒ 红");

            int after = CountArrowish(ov.transform);
            Eq(after, before,
               "B4 `SetTip` **没有新长出任何名字带 arrow 的对象**（前 " + before + " / 后 " + after
             + "）—— 防的是「换个名字重建」：只数 `arrow1`/`arrow2` 会漏掉 `ArrowLeft` 那种写法");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ============================================================ C · 源码兼容读口
    static void SectionC_ReadPorts()
    {
        MenuCheck.Section(_sink, "C · 源码兼容读口：恒 false / 恒 -1（`BattleScene` 那两条 `G-5` 旧断言还引用着）");

        var root = new GameObject("B108A8_Root_Ports");
        try
        {
            var ov = TutorialOverlay.Create(root.transform);
            if (ov == null) { Tr(false, "C0 **前提**：教程视图建出来了"); return; }

            Eq(ov.ArrowLeftVisible, false, "C1 `ArrowLeftVisible` 恒 false（原版那颗 `m_Sprite` 为空 ⇒ 永远不可见）");
            Eq(ov.ArrowRightVisible, false, "C2 `ArrowRightVisible` 恒 false");
            Eq(ov.ArrowLeftAlpha, -1f, "C3 `ArrowLeftAlpha` 恒 -1（= 「这一层不存在」，不是「透明但还在」）");
            Eq(ov.ArrowRightAlpha, -1f, "C4 `ArrowRightAlpha` 恒 -1");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ============================================================ D · 源码守卫（剥注释后查）
    static void SectionD_SourceGuard()
    {
        MenuCheck.Section(_sink, "D · 源码守卫：`TutorialOverlay.cs` 的**活代码**里不许再出现箭头字段 / 建箭头入口");

        string raw = ReadAsset(SrcOverlay);
        Tr(raw.Length > 10000, "D0 **前提**：`" + SrcOverlay + "` 读得到（长度 > 10000）");
        if (raw.Length <= 10000) return;

        // 🔴 剥掉 `//` 行注释与 `/* */` 块注释再查 —— 历史留痕里这些词**还在**（「❌ 已删：原来是 …」），
        //    ⛔ 那不是活代码；不剥的话这条守卫会被自己的注释判红。
        string code = StripComments(raw);

        Tr(code.Contains("ArrowObjectCountForTest"),
           "D0b **前提**：剥注释后仍读得到 `ArrowObjectCountForTest`（证明确实拿到了活代码，不是读错文件）");

        Eq(Count(code, "_arrowL"), 0, "D1 活代码里 0 处 `_arrowL`｜🧨 把字段/建件写回 ⇒ 红");
        Eq(Count(code, "_arrowR"), 0, "D2 活代码里 0 处 `_arrowR`");
        Eq(Count(code, "SetArrow("), 0, "D3 活代码里 0 处 `SetArrow(`（那颗「设箭头」的私有方法已删）");
        Eq(Count(code, "ArrowLPos") + Count(code, "ArrowRPos"), 0,
           "D4 活代码里 0 处 `ArrowLPos` / `ArrowRPos`（摆位常量已删）");
    }

    // ============================================================ 工具
    static string ReadAsset(string assetRelativePath)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, assetRelativePath);
            return File.Exists(p) ? File.ReadAllText(p) : "";
        }
        catch (Exception e)
        {
            Debug.LogWarning("[B108A8] 读文件失败 `" + assetRelativePath + "`：" + e.Message);
            return "";
        }
    }

    static int Count(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    /// <summary>数 `"<field>": true` 的条数（空白/换行都容忍）。
    /// <para>⚠️ 只认**字符串形式**：`JsonUtility` 写出来是 `"showLeftArrow": true`（不是 `1`）——
    /// 若哪天生成器换了序列化器，这条会数成 0，而 A1（字段总数）不受影响 ⇒ 不会静默放过。</para></summary>
    static int CountTrue(string json, string field)
    {
        return Regex.Matches(json, Regex.Escape(field) + "\\s*:\\s*true\\b").Count;
    }

    static int CountNamed(Transform t, string exactName)
    {
        if (t == null) return 0;
        int n = t.name == exactName ? 1 : 0;
        for (int i = 0; i < t.childCount; i++) n += CountNamed(t.GetChild(i), exactName);
        return n;
    }

    /// <summary>数「名字里带 `arrow`」的节点（忽略大小写）—— 教学标注那四根 `MeleeTextArrow` 等**本来就在**
    /// （原版 `TutorialObjs` 的子件），所以「前后相等」就是「没有新长出来」。
    /// <para>⚠️ 用它而不用「子孙总数」：`Label.SetText` 之类的懒建行为会让总数合法地变，那会造成假红。</para></summary>
    static int CountArrowish(Transform t)
    {
        if (t == null) return 0;
        int n = t.name != null && t.name.IndexOf("arrow", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0;
        for (int i = 0; i < t.childCount; i++) n += CountArrowish(t.GetChild(i));
        return n;
    }

    /// <summary>剥掉 C# 的 `//` 行注释与 `/* … */` 块注释（**只做够用的近似**：本件只用它查标识符出现次数）。
    /// <para>⚠️ 不处理字符串字面量里的 `//` —— `TutorialOverlay.cs` 里没有带 `//` 的字符串常量，
    /// 真有的话最坏是**多剥一点**（守卫只会更宽松，不会假红）。</para></summary>
    static string StripComments(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        int i = 0, n = s.Length;
        while (i < n)
        {
            if (i + 1 < n && s[i] == '/' && s[i + 1] == '/')
            {
                while (i < n && s[i] != '\n') i++;
            }
            else if (i + 1 < n && s[i] == '/' && s[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < n && !(s[i] == '*' && s[i + 1] == '/')) i++;
                i = Math.Min(n, i + 2);
            }
            else sb.Append(s[i++]);
        }
        return sb.ToString();
    }
}
