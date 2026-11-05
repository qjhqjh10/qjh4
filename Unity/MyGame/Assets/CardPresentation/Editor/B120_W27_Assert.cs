// B120_W27_Assert.cs — 执行写手 `B120-W27`（2026-11-02）· 活状态表 `B120-Q04e` / `D118-80`
//
// ============================ 它盯的是什么 ============================
// 把「场上的卡」从层 **8**（`ArenaSlots.ArenaLayer`）搬到层 **10**（`ArenaSlots.BoardCardLayer` = 原版 `BoardElements`）
// 的**收尾**，与它**必须同刻**的三件事：
//   ① `BattleDriver` 的 **6 处卡换层点**全部 `SetLayer(ArenaSlots.BoardCardLayer)`
//   ② **3 个「这张卡在不在 3D 战场」的门**都认 8 或 10（判据只在 `CardFeel.IsBoardLayer` 一处）
//   ③ 落点射线掩码 = `1 << BoardCardLayer`，且 `TryHitSlotByBoardRay` 回**字面 `hits[0]`**
//   ④ `BattleDriver.ToCardData` 透传 `traitRev = u.TraitRev`（`B120-W20` dirty 链的唯一开关）
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//  · 层整数 **10** = 原版 `ProjectSettings/TagManager.asset` 的 `layers` 索引 10（`BoardElements`），
//    本仓 `ProjectSettings/TagManager.asset` 现读第 11 项（0 起算第 10）就是 `BoardElements`。
//  · 射线：`d:/2/tools/decomp_full/BattleManager__GetTargetInPos.c:204-265`（`iVar7 < 1 ⇒ return 0`；否则 `hits[0]`）
//  · 卡的 6 个换层点 = 原版 `SetUnitInPlayLayer` 那一路的 6 个呈现入口（出牌交接 / AI 飞行交接 ×2 / 落体 / 落定 / 兜底路）。
//  · 3 个门 = 深度缩放（`CardFeel.ApplyDepthScale` / `CardFeel.HookDepthScale` / `Card3DAnimationController` 的逐帧分支 /
//    `WFModuleTransformModifier` 的 `updateScaleByBoardPosition` 支）。
//
// ============================ 怎么用 ============================
//   `B120_W27_Assert.Run();`   （静态类 + 无参 `Run()`；**已接线** —— 聚合行 = `CardPresentation/Editor/BattleScene.cs:47035` 的 `B120_W27_Assert.Run(); pass += …; fail += …`，跑 `BattleScene.Run` 时执行；⚠️ 2026-11-05 `B145-E9` 就地订正：原来这里写「本文件尚未接线」，与实际不符）
//   本件**不建任何 GameObject**（纯源码 / 纯静态函数断言）⇒ 编辑模式与播放器都能跑。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A1  ← 任一处 `SetLayer(ArenaSlots.BoardCardLayer)` 改回 `ArenaLayer` ⇒ 计数变 5（应为 6）⇒ 红
//   · A1b ← 同上，逐行钉：那 6 个行号里任何一行不再是新调用 ⇒ 红（**这是「六处必须一次改完」的机器证据**）
//   · A2  ← 掩码改回 `1 << ArenaSlots.ArenaLayer` ⇒ 红（同时 A2b 会因「文件里仍出现 `1 << ArenaLayer`」红）
//   · A3  ← 把射线改回「扫缓冲找第一条卡」⇒ 方法体里重新出现 `for (` / `continue;` ⇒ 红
//   · A4  ← 删掉 `traitRev = u.TraitRev` ⇒ 红（= `B120-W20` dirty 链断线，表现层恒走老行为且**无任何报错**）
//   · B2/B3/B4 ← 任一个门改回只认 `ArenaLayer` ⇒ 红（迁到 10 之后 ⇒ 门恒 false ⇒ **深度缩放静默失效**）
//   · B1  ← `IsBoardLayer` 少了 `BoardCardLayer` 那一半 ⇒ 红
//   · D1  ← 同上，**行为**判别：`IsBoardLayer(10)` 必须为真（纯源码闸的补充，改口径也躲不掉）
//   · D4  ← 掩码若把层 8 也放进来（`| 1 << ArenaLayer`）⇒ 红（地面/背景会抢在卡前面被命中）
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环**：本件只读源码文本 + 跑纯静态函数，**不**证明「真机上点得中」。
//   · **不断言相机掩码**：两台相机的 `cullingMask` 由 `Editor/BattleScene.cs` 那边的断言钉（调度台改的，⛔ 不属本件）。
//   · **A1b 的行号是「当前落点」**：若上游有人在上方插行，A1b 会红 —— 那是**该重新钉行号**的信号，不是缺陷
//     （此时先看 A1 的计数是否仍为 6：计数对而 A1b 红 ⇒ 只是漂移）。
using System;
using System.IO;
using System.Text;
using UnityEngine;
using CardPresentation;

/// <summary>执行写手 `B120-W27`：`BoardElements` 层迁移收尾 + `D118-80` 的 `traitRev` 开关。</summary>
public static class B120_W27_Assert
{
    const string Prefix = "[B120W27] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    /// <summary>6 处卡换层点的**契约值**（原版 `SetUnitInPlayLayer` 那一路的 6 个呈现入口）。🔴 2026-11-05（`B145-E9` · 活状态表 `A-28`）：上游插行 ⇒ 六个落点整体漂移，据【现读】重钉为 13907 / 25969 / 26064 / 26534 / 26808 / 37992（旧值 13894 / 25926 / 26021 / 26491 / 26765 / 37921 现在分别指向 `handoff.Apply = …` / 注释 / `startScale` / `GetLastPlayed` / 血量回写注释 / 空行 ⇒ 全不是换层语句）；判据 = `BattleDriver.cs` 里 `SetLayer(ArenaSlots.BoardCardLayer)` **恰 6 处**、逐行现读确认；A1 的计数仍为 6。</summary>
    static readonly int[] CardLayerSites = { 13907, 25969, 26064, 26534, 26808, 37992 };

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B120-W27：BoardElements(10) 层迁移收尾 + traitRev 开关 开始 ===");
        A_DriverSourceGate();
        B_ThreeGates();
        C_LayerTable();
        D_PureFunctions();
        Debug.Log(Prefix + "—— 合计：通过 " + _ok + " · 失败 " + _bad);
    }

    // ==================================================================
    //  A · `BattleDriver.cs` 源码闸（6 处换层 / 射线掩码 / 字面 hits[0] / traitRev）
    // ==================================================================
    static void A_DriverSourceGate()
    {
        string p = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        Check(File.Exists(p), "A0（前提）源码文件在：" + p + "（⛔ 读不到就红，别改成跳过）");
        if (!File.Exists(p)) return;

        string[] lines = File.ReadAllLines(p);
        string src = StripLineComments(File.ReadAllText(p));   // 注释里的示例不算接线（本文件注释里大量提到旧写法）
        string flat = Flatten(src);

        // ---- A1：6 处卡换层点全部迁到 `BoardCardLayer`，且**没有一处**留在 `ArenaLayer` ----
        int to10 = CountOf(flat, "SetLayer(ArenaSlots.BoardCardLayer)");
        int to8 = CountOf(flat, "SetLayer(ArenaSlots.ArenaLayer)");
        Check(to10 == CardLayerSites.Length,
            "A1 ★ 卡换层点 `SetLayer(ArenaSlots.BoardCardLayer)` **恰好 " + CardLayerSites.Length + " 处**（实得 " + to10 + "）"
            + "｜🧨 任一处改回 `ArenaLayer` ⇒ 5 ⇒ 红（六处必须一次改完：只改一半 ⇒ 有的卡被透视相机画、有的被 HUD 画）");
        Check(to8 == 0,
            "A1c ★ 文件里**没有** `SetLayer(ArenaSlots.ArenaLayer)`（实得 " + to8 + "）—— 层 8 现在只留给 arena prefab 的地面/背景/VFX 宿主"
            + "｜🧨 把某张卡换回 8 ⇒ 红（那张卡在射线掩码外 ⇒ 点不中）");

        // ---- A1b：逐行钉（灭自证：哪一处改回 8 ⇒ 对应那条红，而不是被「另一处仍然对」掩盖）----
        for (int i = 0; i < CardLayerSites.Length; i++)
        {
            int ln = CardLayerSites[i];
            bool ok = ln >= 1 && ln <= lines.Length
                      && lines[ln - 1].Contains("SetLayer(ArenaSlots.BoardCardLayer)");
            Check(ok,
                "A1b-[" + (i + 1) + "] ★ 第 " + ln + " 行的卡换层 = `SetLayer(ArenaSlots.BoardCardLayer)`"
                + "｜🧨 这一处改回 `ArenaLayer` ⇒ 本条红（⚠️ 若上游插行导致整体漂移，本条会一起红 —— 先看 A1 的计数）");
        }

        // ---- A2：落点射线掩码 = 1 << BoardCardLayer（= 原版 `1 << NameToLayer("BoardElements")`）----
        Check(flat.Contains("const int BoardTargetRayLayerMask = 1 << ArenaSlots.BoardCardLayer;"),
            "A2 ★ `BoardTargetRayLayerMask = 1 << ArenaSlots.BoardCardLayer`（层 10 = 原版 `BoardElements`，已一比一）"
            + "｜🧨 改回 `1 << ArenaSlots.ArenaLayer` ⇒ 红（卡在 10 上 ⇒ 射线一条都打不到卡）");
        int mask8 = CountOf(flat, "1 << ArenaSlots.ArenaLayer");
        Check(mask8 == 0,
            "A2b ★ 文件里**没有** `1 << ArenaSlots.ArenaLayer`（实得 " + mask8 + "）—— 掩码里必须**不含** bit8"
            + "｜🧨 写成 `(1 << BoardCardLayer) | (1 << ArenaLayer)` ⇒ 红（地面/背景会抢在卡前面被 `hits[0]` 命中）");

        // ---- A3：`TryHitSlotByBoardRay` 回**字面** `hits[0]`（层 10 上只有卡 ⇒ 不再需要「往回找第一条卡」）----
        string ray = ExtractMethodBody(src, "bool TryHitSlotByBoardRay(Dictionary<int, CardView> views, Vector3 world, out int slot, out int cardHitCount)");
        Check(ray != null, "A3a（前提）在源码里找得到 `TryHitSlotByBoardRay` 的方法体（找不到 ⇒ 签名/改名了 ⇒ 红）");
        if (ray != null)
        {
            string flatRay = Flatten(ray);
            Check(flatRay.Contains("_boardRayHits[0]"),
                "A3 ★ 射线取**字面 `_boardRayHits[0]`**（原版 `hits[0]`，`RaycastNonAlloc` 不排序）"
                + "｜🧨 改成「遍历缓冲挑一条」⇒ 红");
            Check(!flatRay.Contains("for ("),
                "A3b ★ 方法体里**没有循环**（= 不再扫整个缓冲；层 10 上只有卡 ⇒ 首条就是卡）"
                + "｜🧨 把「缓冲里第一条解析得出 `CardView` 的」那版加回来 ⇒ 红");
            Check(!flatRay.Contains("continue;"),
                "A3c ★ 方法体里**没有 `continue;`**（同一条：没有「跳过非卡命中」的变通了）");
            Check(flatRay.Contains("if (n < 1) return false;"),
                "A3d ★ 空命中 = `false`（原版 `iVar7 < 1 ⇒ return 0`，⛔ 不兜底、不回落投影）");
            Check(flatRay.Contains("QueryTriggerInteraction.UseGlobal"),
                "A3e 射线仍是 5 参重载（`UseGlobal`）—— 与 `B120_W13` 的 A1 同一条判据，这里只防它被换成 4 参");
            Check(flatRay.Contains("cardHitCount = 1;"),
                "A3f 计数出参的语义 = **首条命中**（0 / 1）—— `HitSlotHitCountForTest` 与 `HitSlot` 仍同源");
        }

        // ---- A4：`ToCardData` 透传 `traitRev`（`D118-80` 的唯一开关）----
        string tcd = ExtractMethodBody(src, "CardData ToCardData(UnitState u, string faction, bool isPlayer = true)");
        Check(tcd != null, "A4a（前提）找得到 `ToCardData(UnitState, string, bool)` 的方法体");
        if (tcd != null)
        {
            Check(Flatten(tcd).Contains("traitRev = u.TraitRev"),
                "A4 ★ `ToCardData` 里 `traitRev = u.TraitRev`（引擎 `UnitState.TraitRev` → `CardData.traitRev`）"
                + "｜🧨 删掉 ⇒ `CardView.SetBadgesDirtyAware` 恒走老行为（每次都重建），**没有任何报错** ⇒ 红");
        }
    }

    // ==================================================================
    //  B · 三个门（必须与换层同刻；任一处只认 8 ⇒ 深度缩放静默失效）
    // ==================================================================
    static void B_ThreeGates()
    {
        string feel = ReadAsset("CardPresentation/Core/CardFeel.cs");
        Check(feel != null, "B0a（前提）读得到 `CardPresentation/Core/CardFeel.cs`");
        if (feel != null)
        {
            string isb = ExtractMethodBody(StripLineComments(feel), "public static bool IsBoardLayer(int layer)");
            Check(isb != null, "B1a（前提）`CardFeel.IsBoardLayer(int layer)` 存在（判据只此一处）");
            if (isb != null)
            {
                string f = Flatten(isb);
                Check(f.Contains("ArenaSlots.BoardCardLayer") && f.Contains("ArenaSlots.ArenaLayer"),
                    "B1 ★ `IsBoardLayer` = **8 或 10** 都算（`BoardCardLayer` + `ArenaLayer` 各一半）"
                    + "｜🧨 只留 `ArenaLayer` ⇒ 迁到 10 之后门恒 false ⇒ 红");
            }
            string isa = ExtractMethodBody(StripLineComments(feel), "public static bool IsArenaCard(CardView card)");
            Check(isa != null, "B2a（前提）`CardFeel.IsArenaCard` 存在");
            if (isa != null)
            {
                Check(Flatten(isa).Contains("IsBoardLayer("),
                    "B2 ★ `IsArenaCard` 走**同一个** `IsBoardLayer`（⛔ 别在这里写第二份层号判断）"
                    + "｜🧨 改回 `== ArenaSlots.ArenaLayer` ⇒ 红");
            }
        }

        // B3 / B4：两个「没有 `CardView` 时按层号兜底」的门
        string a3d = ReadAsset("CardPresentation/Battle/Card3DAnimationController.cs");
        Check(a3d != null, "B3a（前提）读得到 `Card3DAnimationController.cs`");
        if (a3d != null)
        {
            string s = StripLineComments(a3d);
            Check(Flatten(s).Contains("CardFeel.IsBoardLayer("),
                "B3 ★ `Card3DAnimationController` 的层兜底 = `CardFeel.IsBoardLayer(...)`（逐帧深度缩放那条门）"
                + "｜🧨 改回 `layer == ArenaSlots.ArenaLayer` ⇒ 红");
            Check(!Flatten(s).Contains("== ArenaSlots.ArenaLayer"),
                "B3b ★ 文件里**没有** `== ArenaSlots.ArenaLayer`（实得 "
                + CountOf(Flatten(s), "== ArenaSlots.ArenaLayer") + " 处）");
        }

        string wfm = ReadAsset("WarpforgeVFX/Runtime/WFModuleTransformModifier.cs");
        Check(wfm != null, "B4a（前提）读得到 `WFModuleTransformModifier.cs`");
        if (wfm != null)
        {
            string s = StripLineComments(wfm);
            Check(Flatten(s).Contains("CardFeel.IsBoardLayer("),
                "B4 ★ `WFModuleTransformModifier` 的层兜底 = `CardFeel.IsBoardLayer(...)`（`updateScaleByBoardPosition` 那条门）"
                + "｜🧨 改回 `layer == ArenaSlots.ArenaLayer` ⇒ 红");
            Check(!Flatten(s).Contains("== ArenaSlots.ArenaLayer"),
                "B4b ★ 文件里**没有** `== ArenaSlots.ArenaLayer`（实得 "
                + CountOf(Flatten(s), "== ArenaSlots.ArenaLayer") + " 处）");
        }
    }

    // ==================================================================
    //  C · 常量与层表（实读，不用我们的常量反推原版）
    // ==================================================================
    static void C_LayerTable()
    {
        int original = 10;   // 原版 `TagManager.asset` 的 `layers` 索引 10 = `BoardElements`
        Check(ArenaSlots.BoardCardLayer == original,
            "C1 ★ `ArenaSlots.BoardCardLayer` = **" + original + "**（原版 `BoardElements` 的层索引）｜实得 " + ArenaSlots.BoardCardLayer);
        Check(ArenaSlots.ArenaLayer == 8,
            "C2 ★ `ArenaSlots.ArenaLayer` 仍是 **8**（⛔ 没被改名/改值）｜实得 " + ArenaSlots.ArenaLayer);
        Check(ArenaSlots.ArenaLayer != ArenaSlots.BoardCardLayer,
            "C3 两层**不同**（8 ≠ 10）—— 迁移有意义的前提");

        int named = LayerMask.NameToLayer("BoardElements");
        Check(named == original,
            "C4 ★ **本工程层表里真有 `BoardElements` 这一层**（`LayerMask.NameToLayer` = " + named + "，期望 " + original + "）"
            + "⇒ 原版那句 `1 << NameToLayer(\"BoardElements\")` 现在**能一比一复现**"
            + "｜🧨 层表被删/改名 ⇒ 红（那时掩码就退化成写死数字）");
        Check(named == ArenaSlots.BoardCardLayer,
            "C5 ★ 层表名字与常量**指同一格**（" + named + " vs " + ArenaSlots.BoardCardLayer + "）"
            + "｜🧨 只改常量不改层表（或反过来）⇒ 红");
    }

    // ==================================================================
    //  D · 纯函数行为（源码闸的补充：改口径也躲不掉）
    // ==================================================================
    static void D_PureFunctions()
    {
        Check(CardFeel.IsBoardLayer(ArenaSlots.BoardCardLayer),
            "D1 ★ `IsBoardLayer(10)` = **true** —— 迁过去之后门必须认这一层（源码闸能骗过，这条不能：它真的调函数）"
            + "｜🧨 `IsBoardLayer` 只留 `ArenaLayer` ⇒ 红");
        Check(CardFeel.IsBoardLayer(ArenaSlots.ArenaLayer),
            "D2 `IsBoardLayer(8)` = **true**（过渡期：还没换过去的卡 / 旧存档场景里的卡照旧算「在战场上」）");
        Check(!CardFeel.IsBoardLayer(0),
            "D3 `IsBoardLayer(0)` = **false**（2D 兜底档的卡在 HUD 正交平面 ⇒ ⛔ 不能吃深度缩放）");
        Check(!CardFeel.IsBoardLayer(5),
            "D3b `IsBoardLayer(5)` = **false**（UI 层同理）");

        int mask = 1 << ArenaSlots.BoardCardLayer;
        Check((mask & (1 << ArenaSlots.BoardCardLayer)) != 0 && (mask & (1 << ArenaSlots.ArenaLayer)) == 0,
            "D4 ★ 掩码含 bit10、**不含** bit8（地面/背景/VFX 宿主在 8 ⇒ 它们不许抢 `hits[0]`）"
            + "｜🧨 把 8 也并进掩码 ⇒ 红");
    }

    // ---- 文本工具 ----

    static string ReadAsset(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel);
            if (!File.Exists(p)) { Debug.LogError(Prefix + "✗ 读不到 " + p); return null; }
            return File.ReadAllText(p);
        }
        catch (Exception e) { Debug.LogError(Prefix + "✗ 读 " + rel + " 抛了：" + e.Message); return null; }
    }

    /// <summary>按签名切出一个方法体（从签名行到与之配对的右花括号）。找不到 ⇒ `null`。</summary>
    static string ExtractMethodBody(string src, string signature)
    {
        int i = src.IndexOf(signature, StringComparison.Ordinal);
        if (i < 0) return null;
        int open = src.IndexOf('{', i);
        if (open < 0) return null;
        int depth = 0;
        for (int k = open; k < src.Length; k++)
        {
            if (src[k] == '{') depth++;
            else if (src[k] == '}')
            {
                depth--;
                if (depth == 0) return src.Substring(i, k - i + 1);
            }
        }
        return null;
    }

    /// <summary>把 `//` 之后的内容裁掉（只用来数「代码里有没有这个词」，注释里的示例不算）。</summary>
    static string StripLineComments(string src)
    {
        var sb = new StringBuilder(src.Length);
        int i = 0;
        while (i < src.Length)
        {
            char c = src[i];
            if (c == '"')                                   // 跳过字符串字面量（里面的 `//` 不是注释）
            {
                sb.Append(c); i++;
                while (i < src.Length)
                {
                    char d = src[i]; sb.Append(d); i++;
                    if (d == '\\' && i < src.Length) { sb.Append(src[i]); i++; continue; }
                    if (d == '"') break;
                }
                continue;
            }
            if (c == '/' && i + 1 < src.Length && src[i + 1] == '/')
            {
                while (i < src.Length && src[i] != '\n') i++;
                continue;
            }
            sb.Append(c); i++;
        }
        return sb.ToString();
    }

    /// <summary>把连续空白压成一个空格（跨行也能比对调用形状）。</summary>
    static string Flatten(string s)
    {
        var sb = new StringBuilder(s.Length);
        bool prevWs = false;
        foreach (char c in s)
        {
            bool ws = char.IsWhiteSpace(c);
            if (ws) { if (!prevWs) sb.Append(' '); }
            else sb.Append(c);
            prevWs = ws;
        }
        return sb.ToString();
    }

    static int CountOf(string hay, string needle)
    {
        int n = 0, at = 0;
        while (true)
        {
            int k = hay.IndexOf(needle, at, StringComparison.Ordinal);
            if (k < 0) return n;
            n++; at = k + needle.Length;
        }
    }
}
