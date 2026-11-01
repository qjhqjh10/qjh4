// B110_W5_ShaderSupport_Assert.cs — 自建 shader 的「进包 + 编得过 + 真能渲」闸（账 `B-43` / `N48`）
//
// ============================ 这条账挡的是什么 ============================
//  `B-43`：2026-10-31 本会话新造的 5(+1) 个 UI shader，此前**只有**「`Library/ShaderCache` 里出现过
//          编译产物」这一条**间接**证据（判据 `资料/完全复刻/判据/B110_R2_小账时效复核.md` §五② /
//          「顺手发现」⑦）。没有任何断言看 `Shader.isSupported`。
//          shader 编不过 / 没进包 / 变体选不出来时，Unity **不报错**、材质直接渲染成**洋红**
//          ⇒ 玩家看到的是一整块紫红，而所有「文件在 / 名字对 / 参数对」的断言照样全绿。
//  `N48`：全工程此前**没有任何** `Shader.Find(…).isSupported` 断言（同上判据 §顺手发现⑦）。
//
// ============================ 判据（从哪来）============================
//  · 名字真本 = 每个 `.shader` 文件里的 `Shader "..."` 那一行（⛔ 不是文件名、也不是调用点字符串）。
//    本会话出过一次命名不统一（原版 `Everguild/UI/Pulsating` 落成了 `WarpforgeVFX/UI/UI Pulsating`）
//    ⇒ **「文件里的真名 == 调用点用的名字」本身就值得单独断一条**。
//  · `Shader.Find` 只能找到**进了包**的 shader（`Resources/` 下 或 `GraphicsSettings.m_AlwaysIncludedShaders`）。
//  · `Shader.isSupported` = 「当前平台/图形设备编得出来」。
//  · 🔴 `Material.SetPass(0) == false` 才是洋红的**直接**判据（变体选不出来时它 false）。
//    这条口径的出处在 `WarpforgeArena1/Editor/ArenaBuilder.cs:4910-4922`
//    （2026-09-21 查「原版网格 shader 为什么整屏洋红」时立的：**`isSupported` 只是必要条件，不是充分条件**）。
//    本件对**我们自己的工程 shader** 用同一个判据 —— 它们不像 bundle shader 那样只能运行时加载。
//
// ============================ 已知边界（⛔ 别把它当「全包了」）============================
//  ① **编辑器里 `Shader.Find` 能找到工程里所有 shader** ⇒「`Find != null`」在编辑器里**不能**证明它进了
//     player 包。所以另立一条「进包判据」：`Resources/` 路径 / `m_AlwaysIncludedShaders` 里的 guid /
//     被 `Resources` 下的材质以 guid 引用（三条任一成立即可进包）。
//  ② 本文件**不渲图**：`SetPass` 只证明「这一遍提交得出去」，不证明**画出来像不像原版**
//     （那是 `B110_M1/M2/M3` 与真机的事，见 `判据/B109_R15_菜单13材质.md` §3·4）。
//  ③ 只覆盖下面清单里的**自建** shader；原版 bundle shader 走 `WarpforgeVFX.Runtime.WarpforgeShaderLoader`，
//     判据不同（`ArenaBuilder.ProbeOriginalShader`），⛔ 别拿本件当它们的闸。
//
// ============================ 用法 ============================
//   宿主接线（调度台）：接进 `Editor/ShellScene.cs` 合计行**之前**，同一行追加 ⇒ 行数零变化：
//     B110_W5_ShaderSupport_Assert.Run(); _sink.Pass += B110_W5_ShaderSupport_Assert.Pass; _sink.Fail += B110_W5_ShaderSupport_Assert.Fail;
//   单发：
//     unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//       -executeMethod B110_W5_ShaderSupport_Assert.RunCli -logFile -
//   ⚠️ 判绿红看「B110W5 合计」，不看退出码。

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public static class B110_W5_ShaderSupport_Assert
{
    public static int Pass;
    public static int Fail;

    const string P = "[B110_W5] ";

    // ------------------------------------------------------------------ 清单
    // `Where` 只用于报告文字；`Container` 决定走哪条「进包判据」：
    //   "Resources"      ⇒ 文件必须在 `/Resources/` 下（这类文件 Unity 无条件打进包）
    //   "AlwaysIncluded" ⇒ 它的 guid 必须在 `ProjectSettings/GraphicsSettings.asset` 的 m_AlwaysIncludedShaders
    //   "MaterialRef"    ⇒ 它被一个 `Resources/` 下的 `.mat` 以 guid 引用（材质进包 ⇒ 它跟着进包）
    struct Entry
    {
        public string Name;       // `Shader "..."` 里的名字
        public string File;       // Assets 相对路径（含扩展名）
        public string Container;  // 见上
        public string Materials;  // ";" 分隔的 .mat 相对路径（要核「材质指的真是它」时填，其余留空）
        public string Where;      // 谁在用（报告用，不参与判定）
    }

    // 本会话（2026-10-31）新造的 6 个 —— `B-43` 的正主
    static readonly Entry[] NewThisSession =
    {
        new Entry { Name = "WarpforgeVFX/UI/UI Division Change",
                    File = "Assets/CardPresentation/Resources/Shaders/WFUIDivisionChange.shader",
                    Container = "Resources",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/WFUI_DivisionChange.mat",
                    Where = "Shell/RankedDivisionChangeWindow.cs（Division Image / Rank Icon）" },

        new Entry { Name = "WarpforgeVFX/UI/UI Greyscale Add",
                    File = "Assets/CardPresentation/Resources/Shaders/WFUIGreyscaleAdd.shader",
                    Container = "Resources",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/WFUI_GreyscaleAdd.mat",
                    Where = "Shell/TitleTab.cs（Image Top）" },

        new Entry { Name = "WarpforgeVFX/UI/UI Distort",
                    File = "Assets/CardPresentation/Resources/Shaders/WFUIDistort.shader",
                    Container = "Resources",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/WFUI_Distort_AtlasSmall.mat"
                              + ";Assets/CardPresentation/Resources/Art/vfx/WFUI_Distort_ForgeCandle.mat",
                    Where = "Shell/ExpansionPassEventWindow.cs（Atlas 小图）/ 锻造厂蜡烛" },

        new Entry { Name = "WarpforgeVFX/UI/Scan Lines",
                    File = "Assets/CardPresentation/Resources/Shaders/WFUIScanLines.shader",
                    Container = "Resources",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/WFUI_ScanLines.mat",
                    Where = "Shell/ParentEventWindow.cs:2570 的 `Shader.Find` 退路 + 材质" },

        new Entry { Name = "WarpforgeVFX/UI/Additive",
                    File = "Assets/CardPresentation/Resources/Shaders/WFUIAdditive.shader",
                    Container = "Resources",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/WFUI_Additive.mat",
                    Where = "Shell/ParentEventWindow.cs:2570 的 `Shader.Find` 退路 + 材质" },

        new Entry { Name = "WarpforgeVFX/UI/FX Shine For Animation Normal",
                    File = "Assets/CardPresentation/Resources/Shaders/WFUIFxShine.shader",
                    Container = "Resources",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/WFUI_FxShine.mat",
                    Where = "Shell/AvatarTab.cs:649 `DoShine`（#3 FX Shine）" },
    };

    // 更早的自建 shader —— 顺带覆盖（同一条判据，成本极低）
    static readonly Entry[] EarlierSelfMade =
    {
        new Entry { Name = "WarpforgeVFX/UI/UI Pulsating",
                    File = "Assets/CardPresentation/Resources/Shaders/WFUIPulsating.shader",
                    Container = "Resources",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/WFUI_Pulsating_Forge.mat"
                              + ";Assets/CardPresentation/Resources/Art/vfx/WFUI_Pulsating_Campaign.mat",
                    Where = "Shell/MenuFxMaterials.cs（#4 Pulsating）" },

        new Entry { Name = "WarpforgeVFX/UI/UI Glow SDF Noise",
                    File = "Assets/CardPresentation/Resources/Shaders/WFUIGlowSdfNoise.shader",
                    Container = "Resources",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/WFUI_GlowSdf_AllianceTrophy.mat"
                              + ";Assets/CardPresentation/Resources/Art/vfx/WFUI_GlowSdf_BorderChest.mat"
                              + ";Assets/CardPresentation/Resources/Art/vfx/WFUI_GlowSdf_BorderChestCollect.mat"
                              + ";Assets/CardPresentation/Resources/Art/vfx/WFUI_GlowSdf_HighlightRed.mat"
                              + ";Assets/CardPresentation/Resources/Art/vfx/WFUI_GlowSdf_HighlightSmoothRed.mat",
                    Where = "Shell/MenuFxMaterials.cs（#5 Glow SDF Noise）" },

        new Entry { Name = "WarpforgeVFX/UI/UI Clock",
                    File = "Assets/CardPresentation/Resources/Shaders/WFUIClock.shader",
                    Container = "Resources",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/WFUI_Clock.mat",
                    Where = "Shell/MenuFxMaterials.cs（#9 UI Clock）" },

        // ⚠️ 这一个**不在 `Resources/` 下、也不在 AlwaysIncluded 里**：它只靠
        //    `Resources/Art/vfx/Border Effect For Level up.mat` 的 guid 引用进包（生产代码**没有**对它调 `Shader.Find`）。
        new Entry { Name = "WarpforgeVFX/UI/UI Border Highlight",
                    File = "Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader",
                    Container = "MaterialRef",
                    Materials = "Assets/CardPresentation/Resources/Art/vfx/Border Effect For Level up.mat",
                    Where = "Shell/CardDisplayEffects.cs 的 `BuildImage`（`im.hasAmp` 那一段）挂 `Border Effect For Level up` —— 只经材质引用；⛔ 这里**不写行号**（2026-11-01 起：`W-8`/`W-17` 两次改动已让它漂过两回，指向方法名才稳）" },

        // 这一个在 `GraphicsSettings.m_AlwaysIncludedShaders` 里（guid `7d3c5a91…`）
        new Entry { Name = "CardPresentation/UIBlur",
                    File = "Assets/CardPresentation/Shaders/UIBlur.shader",
                    Container = "AlwaysIncluded",
                    Materials = "",
                    Where = "AlwaysIncludedShaders（背景模糊）" },
    };

    /// <summary>生产代码里**真的**调 `Shader.Find(<字面量>)` 的那两处（`Shell/ParentEventWindow.cs:2570`）。
    /// 断言「字面量 == 我们 shader 的真名」—— shader 改名而调用点没改时，那条退路会**静默失效**。</summary>
    const string ProductionFindSource = "Assets/CardPresentation/Shell/ParentEventWindow.cs";

    // ------------------------------------------------------------------ 入口

    public static void Run()
    {
        Pass = 0; Fail = 0;
        _setPassProbe = -1;
        Debug.Log(P + "===== 自建 shader 支持性断言开始（B-43 / N48）=====");

        foreach (var e in NewThisSession) CheckEntry(e, "新");
        foreach (var e in EarlierSelfMade) CheckEntry(e, "旧");

        NegativeControls();
        ProductionFindLiterals();

        Debug.Log(P + "===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    /// <summary>命令行单发入口（`-executeMethod`）。</summary>
    public static void RunCli()
    {
        Run();
        if (Application.isBatchMode) Application.Quit(Fail == 0 ? 0 : 1);
    }

    // ------------------------------------------------------------------ 逐条

    static void CheckEntry(Entry e, string tag)
    {
        // ① 前提：文件在，而且能 Load 成 Shader（断不出来就别让后面 NRE 逃掉）
        Shader byAsset = AssetDatabase.LoadAssetAtPath<Shader>(e.File);
        Check(byAsset != null, "前提[" + tag + "] `" + e.File + "` 能 Load 成 Shader");
        if (byAsset == null) return;                       // 前提已判红，不静默

        // ② 名字：文件里的 `Shader "..."` 就是清单里那个名字（命名不统一 = 调用点会找空）
        Check(byAsset.name == e.Name,
              "名字[" + tag + "] `" + Path.GetFileName(e.File) + "` 的真名 = `" + e.Name + "`（实得 `" + byAsset.name + "`）");

        // ③ `Shader.Find` 取得到（诊断用；编辑器里它找到的是工程内资产，见文件头边界①）
        Shader found = Shader.Find(e.Name);
        Check(found != null,
              "前提[" + tag + "] `Shader.Find(\"" + e.Name + "\")` 取得到（null ⇒ 没进包 / 名字写错）");

        // ④ 合并结论（灭自证锚点：`NegativeControls` 用的是**同一个** `ShaderOk`，见下）
        Check(ShaderOk(e.Name),
              "结论[" + tag + "] `" + e.Name + "` = `Shader.Find` 取得到 **且** `isSupported`（= 这台机器上编得出来）");

        // ⑤ `isSupported` 单独再断一次（失败时把「是 Find 空还是不支持」分得开）
        Shader sh = found != null ? found : byAsset;
        Check(sh.isSupported,
              "结论[" + tag + "] `" + e.Name + "` `isSupported == true`（false ⇒ 当前平台编不出来 ⇒ 材质渲成品红）");

        // ⑥ 编译期没有 error（比 isSupported 更早一层：源码/宏写坏）
        bool hasErr;
        try { hasErr = ShaderUtil.ShaderHasError(sh); }
        catch (Exception ex) { Debug.LogWarning(P + "ShaderUtil.ShaderHasError 抛异常（" + ex.GetType().Name + "），按「读不出」记一条红"); hasErr = true; }
        Check(!hasErr,
              "结论[" + tag + "] `" + e.Name + "` 编译无 error（`ShaderUtil.ShaderHasError == false`；true ⇒ 源码/宏写坏）");

        // ⑦ 洋红的**直接**判据：材质那一遍提交得出去吗（口径同 `ArenaBuilder.cs:4910-4922`）
        CheckPasses(sh, tag, e.Name);

        // ⑧ 进包判据（编辑器里 `Shader.Find` 恒能找到工程资产 ⇒ 必须另判）
        CheckInBuild(e, tag);

        // ⑨ 材质 → shader 的 guid 引用（生产代码走的是 `Resources.Load<Material>`，这条链断了同一个后果）
        if (!string.IsNullOrEmpty(e.Materials))
        {
            string want = MetaGuid(e.File);
            foreach (var mat in e.Materials.Split(';'))
            {
                if (string.IsNullOrEmpty(mat)) continue;
                string got = MatShaderGuid(mat);
                Check(got != null && want != null && got == want,
                      "材质[" + tag + "] `" + Path.GetFileName(mat) + "` 的 `m_Shader` 指向 `"
                    + Path.GetFileName(e.File) + "`（实得 guid `" + (got ?? "读不出") + "`，期望 `" + (want ?? "读不出") + "`）");
            }
        }
    }

    static void CheckPasses(Shader sh, string tag, string name)
    {
        bool passOk = false;
        Material m = null;
        try
        {
            m = new Material(sh);
            passOk = m.SetPass(0);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(P + "`new Material / SetPass` 抛异常：" + ex.GetType().Name + " " + ex.Message);
            passOk = false;
        }
        finally
        {
            if (m != null) UnityEngine.Object.DestroyImmediate(m);
        }
        Check(sh.passCount > 0, "结论[" + tag + "] `" + name + "` 的 `passCount > 0`（实得 " + sh.passCount + "）");

        // 🔴 `SetPass` 只在「这个环境真能提交材质」的前提下才是判据。先用一个**必定支持**的内建 shader
        //    校准一次：连它都 false ⇒ 那是 **headless/无渲染上下文**的问题，不是我们 shader 的问题。
        //    这时**出声 + 跳过**（⛔ 不是绿，是「没测」），⛔ 不要拿它给我们自己的 shader 判红。
        if (SetPassUsable())
            Check(passOk,
                  "结论[" + tag + "] `" + name + "` 的 `new Material(shader).SetPass(0) == true` "
                + "（false ⇒ 变体选不出来 = **洋红**；这是比 isSupported 更直接的判据）");
        else
            Debug.Log(P + "（跳过 `" + name + "` 的 SetPass 判据：本环境无可用渲染上下文 —— 见上面那条校准告警）");
    }

    static int _setPassProbe = -1;   // -1 = 未测 / 0 = 不可用 / 1 = 可用

    /// <summary>校准 `Material.SetPass` 在**当前环境**里到底能不能用（用内建 `Sprites/Default` 试）。
    /// 只在第一次调用时真测，结果缓存。</summary>
    static bool SetPassUsable()
    {
        if (_setPassProbe >= 0) return _setPassProbe == 1;
        bool ok = false;
        Shader d = Shader.Find("Sprites/Default");
        Material m = null;
        try
        {
            if (d != null) { m = new Material(d); ok = m.SetPass(0); }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(P + "SetPass 校准抛异常：" + ex.GetType().Name + " " + ex.Message);
            ok = false;
        }
        finally
        {
            if (m != null) UnityEngine.Object.DestroyImmediate(m);
        }
        _setPassProbe = ok ? 1 : 0;
        if (!ok)
            Debug.LogWarning(P + "**SetPass 校准失败**：连内建 `Sprites/Default` 的 `SetPass(0)` 都是 false ⇒ "
                           + "本环境（多半是 headless / 无渲染上下文）下 `SetPass` 判据不可用，"
                           + "逐 shader 的那一条**跳过**（⛔ 不是绿，是「没测」）。"
                           + " 其余判据（`Shader.Find` / `isSupported` / `ShaderHasError` / 进包）照常执行。");
        return ok;
    }

    static void CheckInBuild(Entry e, string tag)
    {
        switch (e.Container)
        {
            case "Resources":
                Check(e.File.Contains("/Resources/"),
                      "进包[" + tag + "] `" + Path.GetFileName(e.File) + "` 在 `/Resources/` 下（⇒ 无条件进包 ⇒ player 里 `Shader.Find` 也找得到）");
                break;

            case "AlwaysIncluded":
            {
                string gs = ReadFile("ProjectSettings/GraphicsSettings.asset");
                string guid = MetaGuid(e.File);
                Check(gs != null && guid != null && gs.Contains(guid),
                      "进包[" + tag + "] `" + Path.GetFileName(e.File) + "` 的 guid `" + (guid ?? "读不出")
                    + "` 在 `ProjectSettings/GraphicsSettings.asset` 的 m_AlwaysIncludedShaders 里");
                break;
            }

            case "MaterialRef":
            {
                // 被一个 Resources 下的材质以 guid 引用 ⇒ 跟着材质进包（生产代码没有 Shader.Find，所以这是唯一进包路径）
                string guid = MetaGuid(e.File);
                bool anyRef = false;
                if (!string.IsNullOrEmpty(e.Materials))
                    foreach (var mat in e.Materials.Split(';'))
                    {
                        if (string.IsNullOrEmpty(mat)) continue;
                        if (MatShaderGuid(mat) == guid) anyRef = true;
                    }
                Check(anyRef,
                      "进包[" + tag + "] `" + Path.GetFileName(e.File) + "` 被 `Resources/` 下的材质引用（它不在 Resources、也不在 AlwaysIncluded ⇒ 这是进了包的唯一依据）");
                break;
            }

            default:
                Check(false, "进包[" + tag + "] 清单里 `Container` 写错了：「" + e.Container + "」（只能是 Resources / AlwaysIncluded / MaterialRef）");
                break;
        }
    }

    // ------------------------------------------------------------------ 灭自证

    /// <summary>🔴 **灭自证**：`ShaderOk` 是 ④ 用的那条断言本身。这里喂它三个「必须不通过」的输入 ——
    /// 只要有人把 `ShaderOk` 改成恒真（或把名字比较写成 `Contains` 之类），这三条立刻红。
    /// 「故意写错一个 shader 名 ⇒ 必须红」是账 `B-43` 的验收要求原话。</summary>
    static void NegativeControls()
    {
        const string Fake = "WarpforgeVFX/UI/__B110_W5_ThisShadeDoesNotExist__";
        Check(Shader.Find(Fake) == null,
              "灭自证① 虚构名 `Shader.Find` 返回 null（这条为假 ⇒ 环境本身不可信，后面所有绿都不能信）");
        Check(!ShaderOk(Fake),
              "灭自证② 同一条断言函数 `ShaderOk` 喂虚构名必须判「不通过」（= 挡恒真的假闸）");
        Check(!ShaderOk("WarpforgeVFX/UI/Scan Line"),
              "灭自证③ `Scan Lines` 少写一个 `s` 必须判「不通过」（= 挡「差一个字符也绿」—— 本会话刚出过命名不统一）");
    }

    /// <summary>④ 与 灭自证 共用的**唯一**判定式：取得到 **且** 支持。</summary>
    static bool ShaderOk(string name)
    {
        Shader s = Shader.Find(name);
        return s != null && s.isSupported;
    }

    // ------------------------------------------------------------------ 生产调用点字面量

    static void ProductionFindLiterals()
    {
        string src = ReadFile(ProductionFindSource);
        Check(src != null, "前提 `" + ProductionFindSource + "` 能读到");
        if (src == null) return;

        Check(src.Contains("\"WarpforgeVFX/UI/Scan Lines\""),
              "生产字面量 `ParentEventWindow` 的 `Shader.Find(\"WarpforgeVFX/UI/Scan Lines\")` 与 `.shader` 里的真名逐字一致");
        Check(src.Contains("\"WarpforgeVFX/UI/Additive\""),
              "生产字面量 `ParentEventWindow` 的 `Shader.Find(\"WarpforgeVFX/UI/Additive\")` 与 `.shader` 里的真名逐字一致");
    }

    // ------------------------------------------------------------------ 工具

    static void Check(bool ok, string what)
    {
        if (ok) { Pass++; Debug.Log("B110W5 ✓ " + what); }
        else { Fail++; Debug.LogError("B110W5 ✗ " + what); }
    }

    /// <summary>`MyGame/`（= `Assets/` 的上一级）。</summary>
    static string ProjectRoot { get { return Path.GetDirectoryName(Application.dataPath); } }

    static string ReadFile(string assetsOrProjectRel)
    {
        try
        {
            string full = Path.Combine(ProjectRoot, assetsOrProjectRel.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(full) ? File.ReadAllText(full) : null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(P + "读文件失败 `" + assetsOrProjectRel + "`：" + ex.GetType().Name + " " + ex.Message);
            return null;
        }
    }

    /// <summary>`.meta` 里的 guid（= 这个资产在包/引用里的真身份）。</summary>
    static string MetaGuid(string assetRel)
    {
        string meta = ReadFile(assetRel + ".meta");
        if (meta == null) return null;
        return ValueAfter(meta, "guid:", "\r\n\n");
    }

    /// <summary>`.mat` 里 `m_Shader: {fileID: …, guid: <32 hex>, type: 3}` 的 guid。</summary>
    static string MatShaderGuid(string matRel)
    {
        string txt = ReadFile(matRel);
        if (txt == null) return null;
        int i = txt.IndexOf("m_Shader:", StringComparison.Ordinal);
        if (i < 0) return null;
        return ValueAfter(txt.Substring(i), "guid:", ",}\r\n");
    }

    /// <summary>取 `marker` 之后、直到任一 `stops` 字符（或行尾）之间的非空片段。</summary>
    static string ValueAfter(string text, string marker, string stops)
    {
        int i = text.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return null;
        int s = i + marker.Length;
        while (s < text.Length && (text[s] == ' ' || text[s] == '\t')) s++;
        int e = s;
        while (e < text.Length && stops.IndexOf(text[e]) < 0) e++;
        string v = text.Substring(s, e - s).Trim();
        return v.Length == 0 ? null : v;
    }

    // 宿主聚合行读这两个口（追加到类尾、零行漂）。
    // （本件用 public 字段，与 `B110_M2/M3` 同形 ⇒ 宿主那一行可以照抄。）
}
