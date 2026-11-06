// B111_S4_Assert.cs — 执行代理 `B111_S4`（2026-11-01 · 第一百一十一会话 · 第 1 波）
//   活状态表 `B-20`：菜单材质 **#7 UI Additive** 的 23 个原版节点接线盘账（23 个里此前只接了 14 个）
//   活状态表 `B-21`：`Shell/MenuFxMaterials.cs` **收口**（路径常量 / 取用 / 逐颗克隆只留一处）
//
// ============================ ① `B-20` 的 23 个节点（判据 = 原版包的现读盘）============================
//   判据来源（⛔ 不是抄报告）：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 里
//   `m_Material.m_PathID = -8464978790139598553`（材质 `UI Additive`）的 uGUI `Image` —— **23 个**，
//   父链由 `RectTransform.m_Father` 逐级上溯（脚本：`_tmp_view/b111_s4/scan_additive_nodes.py`）。
//   `Nodes[]` 逐条抄了这 23 条父链 + 我们的落点 + **证据串**：
//     · `Status = 已接 / 等价`  ⇒ `Evidence` 必须是**另一个文件里真实存在的字面串**，本文件去那个文件里找；
//       （这就是灭自证：⛔ 不是「表里写了就算接了」）
//     · `Status = 待接`          ⇒ `Reason` 必须写明「为什么现在不能接 / 下一步是什么」。
//
// ============================ ② `B-21` 收口（`MenuFxMaterials` 是唯一一处）============================
//   收口前 `Shell/` 下有 **3 份**同一套「`Resources.Load` → 逐颗 `new Material(proto)` → 带过 `_Color`」：
//   本类 · `ExpansionPassEventWindow.cs`（`B110_M2` #6）· `ParentEventWindow.cs`（`B110_D4` #11/#7）。
//   本文件断三件：**常量值 = 落盘的 `.mat`**（反射读 `const`，不是读源码）· **`Proto` 的 shader 兜底行为**
//   （真 `Shader.Find` 一次：有 shader ⇒ 拿到材质；无 shader ⇒ 返回 null 且不抛）·
//   **`new Material(proto)` 这条纪律不许再长出第 4 份**。
//
// ============================ 灭自证（⛔ 别删）============================
//   · `Apply` 必须 `q.SetMaterial(inst)`（克隆体）而**不得** `q.SetMaterial(proto)` —— 后者 = 多颗共用
//     `renderQueue`/`_Color`，静默错（`B109_E33` 踩过：联盟 10 颗描边共用队列 3263、染色被抹白）。
//   · 单源检查**不能空转**：`MenuFxMaterials.cs` 自己必须命中 `new Material(proto)`，否则「0 份」也绿。
//   · `Proto` 的两条行为断言必须**成对**（有 shader 得材质 / 无 shader 得 null）——
//     只断前一条的话，把 `Shader.Find` 删掉换成一个恒返回默认材质的实现也能绿。
//
// ============================ 怎么接线（宿主）============================
//   本文件**没有**自己往断言宿主里接（宿主 `Editor/{...}.cs` 是调度台独占）。宿主里加两行即可：
//
//       B111_S4_Assert.Run();
//       pass += B111_S4_Assert.Pass; fail += B111_S4_Assert.Fail;
//
//   ⚠️ 它**不建窗口、不动场景、不跑播放**，只读资产 / 读源码文本 / 调一次 `Shader.Find` ⇒ 挂哪个宿主都安全。
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>`B111_S4`：#7 的 23 节点盘账（`B-20`）+ `MenuFxMaterials` 收口（`B-21`）。</summary>
public static class B111_S4_Assert
{
    public static int Pass;
    public static int Fail;

    const string ShellDir = "Assets/CardPresentation/Shell/";
    const string MatDir   = "Assets/CardPresentation/Resources/Art/vfx/";
    const string MatFile  = "MenuFxMaterials.cs";

    // ---------------------------------------------------------------- ① 23 个节点

    struct Node
    {
        public string Path;       // 原版父链（逐字，由 `scan_additive_nodes.py` 现读）
        public string Status;     // 已接 / 等价 / 待接
        public string Where;      // 我们的落点（相对 `Assets/`）
        public string Evidence;   // `已接`/`等价`：该文件里必须出现的字面串；`待接`：空
        public string Reason;     // `待接`/`等价`：为什么 / 下一步
    }

    /// <summary>原版 23 个节点的盘账表。⛔ 改表就要改下面那几条计数断言（它们会一起红）。</summary>
    static readonly Node[] Nodes =
    {
        // ---- 14 个「模式卡 / 报价卡」的 `Border`：我们只有 **1 个**调用点（`BuildModeCard` ④）----
        N("Border / Base Game Mode Container 1x1"),
        N("Border / Base Game Mode Container 1x1 Claim"),
        N("Border / Base Game Mode Container 1x1 Claim AutoCollect"),
        N("Border / Base Game Mode Container 1x2"),
        N("Border / Base Game Mode Container 1x2 No Timer"),
        N("Border / Base Game Mode Container 1x2 Raid or Army Release"),
        N("Border / Ranked Game Mode Container 1x2"),
        N("Border / Skirmish Ranked Game Mode Container 1x2"),
        N("Border / Draft Game Mode Container 1x2"),
        N("Border / Main Menu Offer Container 1x1"),
        N("Border / Main Menu Offer Container 1x1 Premium_booster_title_avatarOrResource"),
        N("Border / Main Menu Offer Container Carousel 1x1"),
        N("Border / Main Menu Offer Container Static Image 1x1"),
        N("Border / Main Menu Offer Container Static Image 1x2"),

        // ---- 已接（`B110_D4`）----
        S("Glow / Select Team Tab / Two Sides Event Window", "已接",
          "CardPresentation/Shell/ParentEventWindow.cs",
          "ApplyEventUiMaterial(MenuDraw.Rect(page, Tex(ArtGlow", ""),

        // ---- 等价（既有加法材质，视觉同族）----
        S("Rank Icon Glow / Rank Icon / Division Image / Ranked Division Change Window", "等价",
          "CardPresentation/Shell/RankedDivisionChangeWindow.cs",
          "\"Art/vfx/UI Additive\"",
          "用的是既有 `Resources/Art/vfx/UI Additive.mat`（`WarpforgeVFX/Sprites/Additive`）：同一套预乘 + `Blend One One`，"
          + "只少 8bit alpha 量化与 stencil（`B110_M3` 顺手发现 ④ 判「不是缺陷」）。⛔ 别为统一而换成 WFUI_Additive —— 那是另一条 shader。"),

        // ---- 待接（7 条：本波白名单外 ⇒ 补丁交回调度台；详表见 `报告/B111_S4.md` §B-20）----
        // 🆕 2026-11-01（`B111_S4B`）：这 7 条的补丁**已全部落盘**（P3–P7）⇒ 状态从「待接」改成「已接」并钉上证据串。
        S("Border / Game mode deck selector", "已接",
          "CardPresentation/Shell/CollectionWindow.cs",
          "MenuFxMaterials.ApplyAll(deckSelectorBorder, MenuFxMaterials.Additive)", ""),
        S("Border / Reward Event Container 1x1", "已接",
          "CardPresentation/Shell/RankedArmyBonusCard.cs",
          "MenuFxMaterials.ApplyAll(bonusBorderNine, MenuFxMaterials.Additive)",
          "P5 同处把 tint 从 `Color.white` 订正回原版 0.33962262（原版 `m_Color = (0.3396,…)`）。"),
        S("Border / Two Side Event Select Team", "已接",
          "CardPresentation/Shell/ParentEventWindow.cs",
          "MenuFxMaterials.ApplyAll(teamBorderNine, MenuFxMaterials.Additive)",
          "原版 = 1 个模板 prefab + 2 个实例；我们过程式建两张卡 ⇒ 这 3 条共用 `BuildTeamCard` 的同一调用点。"),
        S("Border / Two Side Event Select Team A", "已接",
          "CardPresentation/Shell/ParentEventWindow.cs",
          "MenuFxMaterials.ApplyAll(teamBorderNine, MenuFxMaterials.Additive)", "同上（`BuildTeamCard` 调两次）。"),
        S("Border / Two Side Event Select Team B", "已接",
          "CardPresentation/Shell/ParentEventWindow.cs",
          "MenuFxMaterials.ApplyAll(teamBorderNine, MenuFxMaterials.Additive)", "同上（`BuildTeamCard` 调两次）。"),
        S("Glow / VS Text / Event On Going Tab / Two Sides Event Window", "已接",
          "CardPresentation/Shell/ParentEventWindow.cs",
          "ApplyEventUiMaterial(MenuDraw.Rect(p2Vs, Tex(ArtGlow",
          "P7 连带补了这页整对节点（`VS Text` 容器 + `Glow` + `VS Text` 图）；几何 `P2VsR` / `P2VsGlowR` 抄自 `menu_dump.py`。"),
        S("Debug Add Skulls / Buttons / Member Options Panel", "已接",
          "CardPresentation/Shell/AllianceMemberOptionsPopup.cs",
          "if (i == IDebug) MenuFxMaterials.ApplyAll(bgo, MenuFxMaterials.Additive)",
          "只有这一颗挂 #7（同面板另外七颗不是）。"),
    };

    /// <summary>`已接` 那 14 条共用同一个调用点（逐条列出是为了「23 条一条不漏」的账，不是为了 14 个断言）。</summary>
    static Node N(string path)
    {
        return new Node
        {
            Path = path,
            Status = "已接",
            Where = "CardPresentation/Shell/MainMenuRuntime.cs",
            Evidence = "MenuFxMaterials.ApplyAll(borderNine",
            Reason = "",
        };
    }

    static Node S(string path, string status, string where, string evidence, string reason)
    {
        return new Node { Path = path, Status = status, Where = where, Evidence = evidence, Reason = reason };
    }

    static Node P(string path, string where, string patch, string reason)
    {
        return new Node { Path = path, Status = "待接", Where = where, Evidence = "", Reason = patch + "：" + reason };
    }

    // ---------------------------------------------------------------- 入口

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log("[B111_S4] ===== 菜单材质 #7 盘账（B-20）+ MenuFxMaterials 收口（B-21）=====");

        CheckNodeTable();
        CheckModeCardCallSite();
        CheckConstants();
        CheckProtoBehaviour();
        CheckSingleSource();

        Debug.Log("[B111_S4] ===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    // ---------------------------------------------------------------- 工具

    static void Check(string what, bool ok)
    {
        if (ok) { Pass++; Debug.Log("[B111_S4] ✓ " + what); }
        else    { Fail++; Debug.LogError("[B111_S4] ✗ " + what); }
    }

    static string Text(string assetRel)
    {
        var p = "Assets/" + assetRel;
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    // ---------------------------------------------------------------- ① 23 个节点

    static void CheckNodeTable()
    {
        Debug.Log("[B111_S4] ---- ① `B-20`：23 个原版节点 ----");

        Check("表里 = **23** 条（= 原版包内引用 `UI Additive` 的 uGUI Image 总数；实测 " + Nodes.Length + "）",
              Nodes.Length == 23);

        int hooked = 0, equivalent = 0, pending = 0;
        for (int i = 0; i < Nodes.Length; i++)
        {
            var n = Nodes[i];
            bool known = n.Status == "已接" || n.Status == "等价" || n.Status == "待接";
            Check("[" + (i + 1) + "/23] `" + n.Path + "` 的 Status 是三者之一（实测 `" + n.Status + "`）", known);

            // 落点文件必须真的存在（否则这张表已经烂了）
            bool fileOk = File.Exists("Assets/" + n.Where);
            Check("[" + (i + 1) + "/23] 落点文件在：`" + n.Where + "`", fileOk);

            if (n.Status == "待接")
            {
                pending++;
                Check("[" + (i + 1) + "/23] `待接` 写明了下一步（≥ 12 字，含补丁号或判据结论）",
                      n.Reason != null && n.Reason.Length >= 12);
            }
            else
            {
                if (n.Status == "已接") hooked++; else equivalent++;

                // 🔴 灭自证：证据必须**在落点文件里真的出现**，⛔ 不是「表里写了就算」
                var src = fileOk ? Text(n.Where) : null;
                Check("[" + (i + 1) + "/23] 证据串真的在 `" + n.Where + "` 里：`" + n.Evidence + "`",
                      src != null && n.Evidence.Length > 0 && src.Contains(n.Evidence));
            }
        }

        Check("计数：已接 = 22（14 个模式/报价卡 Border + `Glow / Select Team Tab` + P3–P7 落盘的 7 条；实测 " + hooked + "）", hooked == 22);
        Check("计数：等价 = 1（`Rank Icon Glow`；实测 " + equivalent + "）", equivalent == 1);
        Check("计数：待接 = 0（`B111_S4B` 把 P3–P7 全部落盘后应归零；实测 " + pending + "）", pending == 0);

        // 23 条父链不许重复（防「同一颗数两遍还把另一颗漏了」）
        var seen = new HashSet<string>();
        bool dup = false;
        for (int i = 0; i < Nodes.Length; i++) if (!seen.Add(Nodes[i].Path)) dup = true;
        Check("23 条父链互不重复", !dup);
    }

    /// <summary>那 14 条 `Border` 必须共用**同一个**调用点，而且「先建后换」。</summary>
    static void CheckModeCardCallSite()
    {
        Debug.Log("[B111_S4] ---- ② 14 条共用调用点（`BuildModeCard` ④）----");

        var mm = Text("CardPresentation/Shell/MainMenuRuntime.cs");
        Check("`Shell/MainMenuRuntime.cs` 在", mm != null);
        if (mm == null) return;

        Check("`MenuDraw.Nine(…)` 的返回值被接住（`var borderNine = MenuDraw.Nine(`）",
              mm.Contains("var borderNine = MenuDraw.Nine("));
        Check("同处接了 `MenuFxMaterials.ApplyAll(borderNine, …)`",
              mm.Contains("MenuFxMaterials.ApplyAll(borderNine"));

        int iBuild = mm.IndexOf("var borderNine = MenuDraw.Nine(", System.StringComparison.Ordinal);
        int iApply = mm.IndexOf("MenuFxMaterials.ApplyAll(borderNine", System.StringComparison.Ordinal);
        Check("**先建后换**（build@" + iBuild + " < apply@" + iApply + "）—— 顺序反了队列就带不过去",
              iBuild >= 0 && iApply > iBuild);
        Check("那行 `ApplyAll(…)` **不在行末注释里**（行内第一个 `//` 必须在它之后；2026-10-30 那条坑）",
              CallBeforeLineComment(mm, "MenuFxMaterials.ApplyAll(borderNine"));
    }

    // ---------------------------------------------------------------- ③ B-21 · 常量

    static void CheckConstants()
    {
        Debug.Log("[B111_S4] ---- ③ `B-21`：路径常量（反射读 `const`，不读源码）----");

        CheckConst("Additive",          "Art/vfx/WFUI_Additive");
        CheckConst("ScanLines",         "Art/vfx/WFUI_ScanLines");
        CheckConst("DistortAtlasSmall", "Art/vfx/WFUI_Distort_AtlasSmall");
    }

    static void CheckConst(string fieldName, string want)
    {
        var f = typeof(CardPresentation.MenuFxMaterials).GetField(
            fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        string got = null;
        if (f != null && f.IsLiteral) got = f.GetRawConstantValue() as string;

        Check("`MenuFxMaterials." + fieldName + "` = `" + want + "`（实测 `" + (got ?? "null") + "`）", got == want);
        Check("`Resources/" + want + ".mat` 存在（常量指的那个文件真的落盘了）",
              got != null && File.Exists(MatDir + got.Substring(got.LastIndexOf('/') + 1) + ".mat"));
    }

    // ---------------------------------------------------------------- ④ B-21 · Proto 行为

    const string NoSuchMat = "Art/vfx/__B111_S4_no_such_mat__";

    static void CheckProtoBehaviour()
    {
        Debug.Log("[B111_S4] ---- ④ `B-21`：`Proto` 的 shader 兜底（行为断言）----");

        Material byShader = null;
        Material byNothing = null;
        bool threw = false;
        try
        {
            byShader  = CardPresentation.MenuFxMaterials.Proto(NoSuchMat, "WarpforgeVFX/UI/Additive");
            byNothing = CardPresentation.MenuFxMaterials.Proto(NoSuchMat, "No/Such/Shader");
        }
        catch (System.Exception e) { threw = true; Debug.LogError("[B111_S4] Proto 抛了：" + e); }

        Check("`Proto` 取不到 `.mat` 时**不抛**", !threw);
        Check("`Proto(不存在, \"WarpforgeVFX/UI/Additive\")` 按 shader 现建出了材质（null ⇒ `Shader.Find` 那条路没接上）",
              byShader != null);
        Check("现建那份的 shader 名 = `WarpforgeVFX/UI/Additive`（原版 `Everguild/UI/Additive` 的改名对照）",
              byShader != null && byShader.shader != null && byShader.shader.name == "WarpforgeVFX/UI/Additive");
        // 灭自证：上面那条必须**成对**——没有 shader 名时必须拿不到（否则「恒返回默认材质」也能绿）
        Check("反例挡板：`Proto(不存在, \"No/Such/Shader\")` = null（不抛、不自造空材质）", byNothing == null);
        Check("反例挡板：`Proto(不存在)`（不给 shader 名）= null", CardPresentation.MenuFxMaterials.Proto(NoSuchMat) == null);
        Check("`MenuFxMaterials.Load(不存在)` = null", CardPresentation.MenuFxMaterials.Load(NoSuchMat) == null);

        // ⚠️ 现建出来的两份是**原型**，不是挂到 quad 上的那一份 ⇒ 不回收、不挂场景（这里只读不挂）
    }

    // ---------------------------------------------------------------- ⑤ B-21 · 单源

    static void CheckSingleSource()
    {
        Debug.Log("[B111_S4] ---- ⑤ `B-21`：`new Material(proto)` 只留一处 ----");

        var src = Text("CardPresentation/Shell/" + MatFile);
        Check("`Shell/" + MatFile + "` 在", src != null);
        if (src == null) return;

        // 灭自证：单源检查不能空转 —— 本类自己必须命中
        Check("`" + MatFile + "` 自己就有 `new Material(proto)`（否则「0 份」也会绿）",
              src.Contains("new Material(proto)"));
        Check("`" + MatFile + "` 的 `Apply` 挂的是**克隆体**（`q.SetMaterial(inst)`）",
              src.Contains("q.SetMaterial(inst)"));
        // 🔴 灭自证：直接挂原型 = 多颗共用 renderQueue / _Color（`B109_E33` 踩过：10 颗共用队列、染色被抹白）
        Check("`" + MatFile + "` ⛔ **没有** `q.SetMaterial(proto)`（直接挂原型就是那个静默错）",
              !src.Contains("q.SetMaterial(proto)"));
        Check("`" + MatFile + "` 把旧 tint 带过去（`inst.color = tint`）",
              src.Contains("inst.color = tint"));
        Check("`ApplyAll` 逐颗转调（`GetComponentsInChildren<ImageQuad>(true)` + 循环 `Apply(`）",
              src.Contains("GetComponentsInChildren<ImageQuad>(true)") && src.Contains("Apply(quads[i], resPath)"));

        // 🔴 漂移探测器：这条纪律**不许再长出第 4 份**。
        //    `MenuFxMaterials.cs` 是唯一合法处。
        //    🔴 **2026-11-06（`B150` 残余 · 铁律 5）就地订正**：本探测器原来跑的是**裸文本** `Contains`
        //    ⇒ 两处**说明性注释**被误报成「第 4/第 5 份克隆实现」：
        //      · `DraftModeWindow.cs:4945/4955`（`// ⛔ 别把 new Material(proto) 写回这里`）—— 产品侧早已收口到
        //        `MenuFxMaterials.ApplyOrangeButtonToGreen`（`:4919` 的注释 + `:4927` 的 `TryResolve` 那一族）；
        //      · `ExpansionPassEventWindow.cs:2807/2809`（补丁 P1 的收口说明）—— 产品侧已走
        //        `MenuFxMaterials.Apply(q, MenuFxMaterials.DistortAtlasSmall)`（`:2818`）。
        //    ⇒ 先**剥注释**再比（`StripComments`），并把已落地的 `ExpansionPassEventWindow.cs` 从白名单删掉
        //      （同「`ParentEventWindow.cs` 已收口、从白名单删除」那一条的处理）。
        //    `AvatarTab.cs` **不是** B-21 那套等价实现：`WFUI_FxShine`（头像扫光、不同族）、单实例缓存进 `_shineMat`
        //    反复**就地改** `_Shine_Movement`、无 `_Color` 带过 ⇒ 原版 `AvatarDisplay__DoShine` 的逐字复刻，探测器误命中。
        var allowed = new HashSet<string> { MatFile, "AvatarTab.cs" };
        var offenders = new List<string>();
        var hits = new List<string>();
        foreach (var path in Directory.GetFiles(ShellDir, "*.cs"))
        {
            var name = Path.GetFileName(path);
            if (name == MatFile) continue;
            var txt = StripComments(File.ReadAllText(path));
            if (!txt.Contains("new Material(proto)")) continue;
            hits.Add(name);
            if (!allowed.Contains(name)) offenders.Add(name);
        }
        Check("除 `" + MatFile + "` 外，出现 `new Material(proto)` 的文件都在**已知清单**里（判据 = **剥注释之后**的代码文本）"
              + "（已知只剩 `AvatarTab.cs`（不同族：`WFUI_FxShine` 头像扫光，见上）；"
              + "`ExpansionPassEventWindow.cs`（`B110_M2` P1）· `ParentEventWindow.cs`（`B110_D4` P2）· `DraftModeWindow.cs`（`B143-W9`）"
              + "**都已收口到 `MenuFxMaterials.Apply/ApplyProto`、剥注释后命中 0**"
              + "；另见：" + string.Join(",", hits.ToArray()) + "）",
              offenders.Count == 0);
        // 📌 只记录、不断言：补丁落盘后命中数就是 0。
        Debug.Log("[B111_S4] · 已知清单之外的克隆实现命中 " + offenders.Count + " 份（剥注释后；应为 0）");

        // 交叉一致性：三处对同一份材质必须用同一个名字（别一边写字面量、一边写常量）
        var pe = Text("CardPresentation/Shell/ParentEventWindow.cs");
        if (pe != null)
            Check("`ParentEventWindow.cs` 引用的 `#7` 材质名与常量一致（字面量或 `MenuFxMaterials.Additive` 二选一）",
                  pe.Contains("MenuFxMaterials.Additive") || pe.Contains("\"Art/vfx/WFUI_Additive\""));
        var ep = Text("CardPresentation/Shell/ExpansionPassEventWindow.cs");
        if (ep != null)
            Check("`ExpansionPassEventWindow.cs` 引用的 `#6` 材质名与常量一致（字面量或 `MenuFxMaterials.DistortAtlasSmall` 二选一）",
                  ep.Contains("MenuFxMaterials.DistortAtlasSmall") || ep.Contains("\"Art/vfx/WFUI_Distort_AtlasSmall\""));
    }

    /// <summary>在整份文本里找 `token` 出现的那一行，判它的**第一个 `//`** 是不是在 `token` 之后。</summary>
    static bool CallBeforeLineComment(string text, string token)
    {
        if (text == null) return false;
        int idx = text.IndexOf(token, System.StringComparison.Ordinal);
        if (idx < 0) return false;
        int lineStart = text.LastIndexOf('\n', idx) + 1;
        int comment = text.IndexOf("//", lineStart, System.StringComparison.Ordinal);
        return comment < 0 || comment > idx;
    }

    /// <summary>🆕 **2026-11-06（`B150` 残余 · 铁律 5）**：把 C# 源码里的**注释**剥掉（行注释与块注释），
    /// 字符串/字符字面量原样保留 —— 漂移探测器要判的是「**这条纪律有没有在代码里再长一份**」，
    /// ⛔ **不是**「有没有人在注释里提到那串字」。
    /// <para>为什么必须剥（本波实测）：`DraftModeWindow.cs` 与 `ExpansionPassEventWindow.cs` 收口之后，
    /// 只剩 `// ⛔ 别把 new Material(proto) 写回这里（B111_S4 探测器会红）` 这类**说明性注释**，
    /// 按裸文本 `Contains` 会**误报成第 4/第 5 份克隆实现** ⇒ 断言假红（而产品侧一个字节都没错）。</para>
    /// <para>⚠️ 与 `B142_E31_Assert.CountOfCode` 的「按行首个 `//` 切」不同：本份还要处理块注释与字符串里的 `//`
    /// （文件里真有 `"Art/vfx/…"` 这种串 ⇒ 裸切会把后半行吞掉）。</para></summary>
    static string StripComments(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        var sb = new System.Text.StringBuilder(s.Length);
        bool inStr = false, inChr = false, inLine = false, inBlock = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i], n = i + 1 < s.Length ? s[i + 1] : '\0';
            if (inLine) { if (c == '\n') { inLine = false; sb.Append(c); } continue; }
            if (inBlock) { if (c == '*' && n == '/') { inBlock = false; i++; } else if (c == '\n') sb.Append(c); continue; }
            if (inStr)
            {
                sb.Append(c);
                if (c == '\\' && i + 1 < s.Length) { sb.Append(s[i + 1]); i++; continue; }
                if (c == '"') inStr = false;
                continue;
            }
            if (inChr)
            {
                sb.Append(c);
                if (c == '\\' && i + 1 < s.Length) { sb.Append(s[i + 1]); i++; continue; }
                if (c == '\'') inChr = false;
                continue;
            }
            if (c == '/' && n == '/') { inLine = true; i++; continue; }
            if (c == '/' && n == '*') { inBlock = true; i++; continue; }
            if (c == '"') { inStr = true; sb.Append(c); continue; }
            if (c == '\'') { inChr = true; sb.Append(c); continue; }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
