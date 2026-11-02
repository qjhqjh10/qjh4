// B120_W19_Assert.cs — 执行写手 `B120-W19`（2026-11-02）· **层号一比一迁移：`BoardElements`(10)**
//
// ============================ 它盯的是什么 ============================
// 原版把**棋盘上的卡**放在 **层 10 = `BoardElements`**：
//   · 原版 `ProjectSettings/TagManager.asset` 的 `layers` 索引 **10 = `BoardElements`**；
//   · 原版 `BattleManager.GetTargetInPos` 的射线掩码 = `1 << LayerMask.NameToLayer("BoardElements")`
//     （`cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:46450-46495` 明文 + `decomp_full/BattleManager__GetTargetInPos.c:204-265`）；
//   · 原版战场相机 `m_CullingMask.m_Bits = 1137` = 层 **0/4/5/6/10**（1137 = 1 + 16 + 32 + 64 + 1024）⇒ **含 10**；
//     原版 UI 相机 `= 32`（层 5）⇒ 天然不含 10。
// 我们原来把「卡」和「arena prefab 整棵树（地面/背景/tap 宿主）」**混在层 8（`ArenaSlots.ArenaLayer`）**，
// 于是 `GetTargetInPos` 那条射线只能靠「扫到第一条解析得出 `CardView` 的」绕开地面件 —— 不是原版形状。
//
// 本件钉三件事：
//   A 段 · **常量**：`ArenaSlots.BoardCardLayer == 10`、`ArenaLayer` 仍是 8（⛔ 没被改名/改值）、两者不相等、
//          `TapParticleController.CollisionMask` 数值没变（`0x40000D01`）且**同时**含 bit8 与 bit10。
//   B 段 · **源码闸**：掩码声明绑到了 `BoardCardLayer`（不是写死 10）；卡的 3D 挡板仍然**跟随卡自己的层**。
//   C 段 · **迁移未完成项 → 显式红**（⏳ 待迁移；`调度台` 把下面几处改完就自动转绿，⛔ 别静默跳过）：
//          ① `BattleDriver.cs` 里六处 `SetLayer(ArenaSlots.ArenaLayer)`（卡）② `BoardTargetRayLayerMask`
//          ③ `CardFeel.IsArenaCard` 的门 ④ 相机 `cullingMask`（战场要含 10、HUD 要去掉 10）
//          ⑤ 审计报告 `资料/完全复刻/报告/B120_W19_层依赖审计.md` 在盘上。
//
// ============================ 怎么用 ============================
//   `B120_W19_Assert.Run();`   （静态类 + 无参 `Run()`；**本文件尚未接线**，宿主与聚合行由调度台决定）
//   ⚠️ **C 段是「待迁移红」**：`Fail` 里那几条在迁移落地前**本来就该红** —— 它们不是回归。
//      读口：`Pass` / `Fail` / `PendingFail`（`PendingFail` = `Fail` 里属于「待迁移」的那部分）。
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环**：A/B/C 全是编辑模式下的常量读 + 源码文本闸。
//   · **不断言「真机上点得中 / 卡被哪台相机画」**：那要真 Play（`资料/真Play待验清单.md`）。
//     C④ 只证明**源码里改过**，证明不了**序列化场景里也改过** —— 场景要重建（`BattleScene.BuildAndSaveScene`）。
//   · **不断言 `TagManager.asset` 的文本**：走 `LayerMask.NameToLayer`（工程层表的运行时视图），
//     这一条同时也是「层名有没有真的生效」的唯一证据。
//   · ⚠️ 本件**不改** `B120_W13_Assert`：那句 `A6`（本工程没有 `BoardElements` 名字）在层名落地后
//     **会反过来红** —— 那是「把掩码改成 `1 << BoardCardLayer`」的信号，见 C②。
using System;
using System.IO;
using System.Text;
using UnityEngine;
using CardPresentation;

/// <summary>执行写手 `B120-W19`：`BoardElements`(10) 层号一比一迁移的常量闸 + 源码闸 + 迁移待办闸。</summary>
public static class B120_W19_Assert
{
    const string Prefix = "[B120W19] ";

    /// <summary>原版 `BoardElements` 的层号（原版 `TagManager` 索引 10）。</summary>
    const int OriginalBoardElementsLayer = 10;

    /// <summary>`CollisionMask` 的期望数值（= 原版 `0x40000C01` | 本工程 bit8）。</summary>
    const int ExpectedCollisionMask = 0x40000D01;

    static int _ok, _bad, _pending;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。**含**「待迁移红」。</summary>
    public static int Fail { get { return _bad; } }
    /// <summary>`Fail` 里属于「迁移还没做完」的那部分（不是回归）。</summary>
    public static int PendingFail { get { return _pending; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    /// <summary>「迁移待办」闸：<paramref name="done"/> = 迁移已落地。未落地 ⇒ **显式红**（并计入 <see cref="PendingFail"/>）。</summary>
    static void Pending(bool done, string what)
    {
        if (done) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; _pending++; Debug.LogError(Prefix + "✗ [待迁移] " + what); }
    }

    /// <summary>只记录、不计分（读数 / 证据）。</summary>
    static void Tr(string what) { Debug.Log(Prefix + "· " + what); }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _ok = 0; _bad = 0; _pending = 0;
        Debug.Log(Prefix + "=== B120-W19：`BoardElements`(10) 层号一比一迁移 开始 ===");
        A_LayerConstants();
        B_SourceGate();
        C_MigrationPending();
        Debug.Log(Prefix + "—— 合计：通过 " + _ok + " · 失败 " + _bad
                + "（其中**待迁移** " + _pending + " 条 —— 那不是回归，是 `调度台` 还没改完的那几处）");
    }

    // ==================================================================
    //  A · 常量（值本身是原版判据，⛔ 别拿我们的常量反推原版）
    // ==================================================================
    static void A_LayerConstants()
    {
        int named = LayerMask.NameToLayer("BoardElements");
        Check(named == OriginalBoardElementsLayer,
            "A1（前提）工程层表里 `BoardElements` = " + OriginalBoardElementsLayer
            + "（`LayerMask.NameToLayer` 实得 " + named + "）"
            + "｜🧨 名字被删/挪位 ⇒ 红 —— 先看 `ProjectSettings/TagManager.asset` 的 `layers` 索引 10");

        Check(ArenaSlots.BoardCardLayer == OriginalBoardElementsLayer,
            "A2 ★ `ArenaSlots.BoardCardLayer` = **" + OriginalBoardElementsLayer + "**（原版 `BoardElements`）"
            + "｜实得 " + ArenaSlots.BoardCardLayer);

        Check(ArenaSlots.ArenaLayer == 8,
            "A3 ★ `ArenaSlots.ArenaLayer` 仍 = **8**（⛔ 没被改名/改值 —— 它继续服务 arena 地面/背景/tap 宿主/VFX 宿主）"
            + "｜实得 " + ArenaSlots.ArenaLayer);

        Check(ArenaSlots.ArenaLayer != ArenaSlots.BoardCardLayer,
            "A4 ★ 两层**必须不同**（一个是 arena 整棵树、一个是只有卡）"
            + "｜实得 " + ArenaSlots.ArenaLayer + " / " + ArenaSlots.BoardCardLayer);

        int mask = TapParticleController.CollisionMask;
        Check(mask == ExpectedCollisionMask,
            "A5 ★ `TapParticleController.CollisionMask` 数值没变 = `0x" + ExpectedCollisionMask.ToString("X8") + "`"
            + "（原版 `0x40000C01` 本来就含 bit10 + 本工程 🔶 bit8）｜实得 0x" + mask.ToString("X8"));

        Check((mask & (1 << ArenaSlots.BoardCardLayer)) != 0,
            "A5b ★ 掩码**含 bit10**（" + ArenaSlots.BoardCardLayer + "）—— 否则迁到 10 之后**点卡就穿透到地面**"
            + "（原版 mask 的 `0x400` 那一位）｜实得 0x" + mask.ToString("X8"));

        Check((mask & (1 << ArenaSlots.ArenaLayer)) != 0,
            "A5c ★ 掩码**含 bit8**（" + ArenaSlots.ArenaLayer + "）—— arena prefab 的 tap 宿主仍在这一层"
            + "（`ArenaBuilder.BuildOneArenaPrefab` 整树刷 8）｜实得 0x" + mask.ToString("X8"));
    }

    // ==================================================================
    //  B · 源码闸（形状；注释里的示例不算）
    // ==================================================================
    static void B_SourceGate()
    {
        string tapPath = Path.Combine(Application.dataPath, "CardPresentation/Battle/TapParticleController.cs");
        Check(File.Exists(tapPath), "B0（前提）源码文件在：" + tapPath + "（⛔ 读不到就红，别改成跳过）");
        if (File.Exists(tapPath))
        {
            string src = Flatten(StripLineComments(File.ReadAllText(tapPath)));
            Check(src.Contains("public const int CollisionMask = 0x40000D01 | (1 << ArenaSlots.BoardCardLayer);"),
                "B1 ★ `CollisionMask` 声明**绑到 `ArenaSlots.BoardCardLayer`**（不是写死 `1 << 10`）"
                + "｜🧨 换成写死数字 ⇒ 层号再变一次就静默点卡穿透");
        }

        string cvPath = Path.Combine(Application.dataPath, "CardPresentation/Core/CardView.cs");
        Check(File.Exists(cvPath), "B2（前提）源码文件在：" + cvPath + "（⛔ 读不到就红）");
        if (File.Exists(cvPath))
        {
            string cv = Flatten(StripLineComments(File.ReadAllText(cvPath)));
            Check(cv.Contains("body.layer = gameObject.layer;"),
                "B2 ★ 卡的 3D 挡板**跟随卡自己的层**（`EnsureTapBlockCollider` 里 `body.layer = gameObject.layer;`）"
                + "⇒ 卡换到 10 之后挡板自动跟着走、**不用改 `CardView`**"
                + "｜🧨 改成写死 `ArenaSlots.ArenaLayer` ⇒ 挡板留在 8、`GetTargetInPos` 点不中卡");
        }

        string drvPath = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        Check(File.Exists(drvPath), "B3（前提）源码文件在：" + drvPath + "（⛔ 读不到就红）");
        if (File.Exists(drvPath))
        {
            string drv = StripLineComments(File.ReadAllText(drvPath));
            Tr("B3（读数）`BattleDriver` 里 `.distance` 出现 " + CountOf(drv, ".distance")
               + " 处 —— 卡迁到层 10（层上只有卡）之后，`GetTargetInPos` 射线体应能回到**字面 `hits[0]`**；"
               + "本件不断言那个形状，留给 `B120_W13_Assert` 的 A4 兜（它盯的是「方法体里没有 `distance`」）");
        }
    }

    // ==================================================================
    //  C · 迁移未完成项（**显式红**；调度台改完自动转绿）
    // ==================================================================
    static void C_MigrationPending()
    {
        string drvPath = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        string drv = File.Exists(drvPath) ? StripLineComments(File.ReadAllText(drvPath)) : null;

        if (drv == null)
        {
            Check(false, "C0（前提）读不到 `BattleDriver.cs` ⇒ C① 与 C② 断不了（⛔ 别改成跳过）");
        }
        else
        {
            // C① —— 卡的换层点仍指向 ArenaLayer(8)
            int cardSetters = CountOf(drv, "SetLayer(ArenaSlots.ArenaLayer)");
            Pending(cardSetters == 0,
                "C① `BattleDriver.cs` 里「卡换层」的 `SetLayer(ArenaSlots.ArenaLayer)` 已全部改成 "
                + "`SetLayer(ArenaSlots.BoardCardLayer)`（现读还剩 " + cardSetters
                + " 处 = 2026-11-02 的 6 处：`:13894 :25926 :26021 :26491 :26765 :37921`）"
                + "｜🧨 只改一部分 ⇒ 一半的卡留在 8、一半在 10（两台相机各画一半、`GetTargetInPos` 漏卡）");

            // C② —— 射线掩码
            string flat = Flatten(drv);
            Pending(flat.Contains("const int BoardTargetRayLayerMask = 1 << ArenaSlots.BoardCardLayer;"),
                "C② `BoardTargetRayLayerMask` 改成 `1 << ArenaSlots.BoardCardLayer`（原版 = `1 << NameToLayer(\"BoardElements\")`）"
                + "｜🧨 留着 `1 << ArenaSlots.ArenaLayer` ⇒ 卡迁到 10 之后射线**一张都命不中**（静默返回 −1）");

            // C②b —— 迁移后可以回到字面 `hits[0]`（层上只有卡）
            int ri = drv.IndexOf("bool TryHitSlotByBoardRay(", StringComparison.Ordinal);
            int rayBody = ri >= 0 ? CountOf(drv.Substring(ri, Math.Min(4000, drv.Length - ri)), "GetComponentInParent<CardView>") : -1;
            Tr("C②b 读数：`TryHitSlotByBoardRay` 里 `GetComponentInParent<CardView>()` 出现 " + rayBody
               + " 处 —— 层上只剩卡之后它可以回到「字面 `hits[0]` ⇒ `GetComponentInParent<CardView>()`」，由调度台裁定");
        }

        // C③ —— `CardFeel.IsArenaCard` 的门
        string cfPath = Path.Combine(Application.dataPath, "CardPresentation/Core/CardFeel.cs");
        string cf = File.Exists(cfPath) ? StripLineComments(File.ReadAllText(cfPath)) : null;
        if (cf == null)
        {
            Check(false, "C③（前提）读不到 `CardFeel.cs`（⛔ 别改成跳过）");
        }
        else
        {
            int i0 = cf.IndexOf("public static bool IsArenaCard(CardView card)", StringComparison.Ordinal);
            string body = i0 >= 0 ? cf.Substring(i0, Math.Min(240, cf.Length - i0)) : "";
            Pending(body.Contains("ArenaSlots.BoardCardLayer"),
                "C③ `CardFeel.IsArenaCard` 的门改成比较 `ArenaSlots.BoardCardLayer`"
                + "｜🧨 不改 ⇒ 迁移后 `IsArenaCard` **恒 false** ⇒ 深度缩放（`HookDepthScale` / `WFModuleTransformModifier`）**静默失效**，画面只是「有点不对」");
        }

        // C④ —— 相机 cullingMask（战场要含 10；HUD 要去掉 10）
        string bsPath = Path.Combine(Application.dataPath, "CardPresentation/Editor/BattleScene.cs");
        string bs = File.Exists(bsPath) ? StripLineComments(File.ReadAllText(bsPath)) : null;
        if (bs == null)
        {
            Check(false, "C④（前提）读不到 `Editor/BattleScene.cs`（⛔ 别改成跳过）");
        }
        else
        {
            int n = CountOf(bs, "BoardCardLayer");
            Pending(n >= 2,
                "C④ `Editor/BattleScene.cs` 的相机掩码已按新层改（`BoardCardLayer` 出现 " + n
                + " 处，期望 ≥ 2 = ①战场相机 `cullingMask` 加 bit10 ②HUD 相机 `&= ~(1 << BoardCardLayer)` 去掉 bit10）"
                + "｜🧨 只加不减 ⇒ 卡被**两台相机各画一次**（叠影/亮度双份）；只减不加 ⇒ 卡**整块消失**（静默）");
            Tr("C④b 读数：`BattleScene.cs` 里 `cullingMask` 出现 " + CountOf(bs, "cullingMask")
               + " 处；`1 << ArenaLayer` 那几处（战场相机 / HUD 去掉 / 断言）都要跟着评估");
        }

        // C⑤ —— 审计报告在盘上（给调度台照着改的清单）
        string rep = Path.Combine(Application.dataPath, "../../资料/完全复刻/报告/B120_W19_层依赖审计.md");
        bool repOk = File.Exists(rep);
        Check(repOk, "C⑤ 审计报告在盘上：" + Path.GetFullPath(rep) + "（每行 = 文件:行 | 用途 | 迁移后该在哪层 | 谁改）");
        if (repOk)
        {
            string r = File.ReadAllText(rep);
            Check(r.Contains("BattleDriver.cs:13894") && r.Contains("BattleScene.cs:47711") && r.Contains("BattleScene.cs:47792"),
                "C⑤b 报告含关键迁移锚（`BattleDriver.cs:13894` 卡换层 · `BattleScene.cs:47711` 战场相机掩码 · `:47792` HUD 去掉）");
        }

        // C⑥ —— 提醒：W13 的 A6 会因为「层名已落地」而反向变红
        Tr("C⑥ 提醒：`Editor/B120_W13_Assert.cs` 的 `A6`（本工程没有 `BoardElements` 名字）现在**会红** —— "
           + "那是它的注释里预告过的信号（「把掩码改成 `1 << BoardElements`」），不是回归；B120_W13 的写手/调度台按 C② 一起改掉");
    }

    // ---- 文本工具 ----

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
        if (string.IsNullOrEmpty(hay) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, at = 0;
        while (true)
        {
            int k = hay.IndexOf(needle, at, StringComparison.Ordinal);
            if (k < 0) return n;
            n++; at = k + needle.Length;
        }
    }
}
