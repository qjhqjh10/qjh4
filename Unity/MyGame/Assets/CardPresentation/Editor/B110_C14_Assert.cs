// B110_C14_Assert.cs — 执行代理 `B110_D4`（第一百一十会话）的断言集
//
// ============================ 它盯的是什么 ============================
// 判据 = `资料/完全复刻/判据/B109_R16_死码C45.md` §1 的 C14 / C15 / C17 / C18 四行。
//   · C14 `EventImageSelectorController.Initialize(List<EventAsset>)` —— 3 句：遍历 → 逐格 `TryDraw`
//   · C15 `EventImageSelectorController.ImageEventDrawData.TryDraw(List<EventAsset>)` —— 5 句
//   · C17 `LiveopUIDrawer_EventAssetSprites.EventAssetImages.Initialize(ILiveOps)` —— 4 句
//   · C18 `LiveopUIDrawer_EventAssetSprites.Initialize(ILiveOps)` —— 外层遍历
// 原版方法体（**本文件逐句对着读的**）：
//   `d:/2/tools/decomp_full/EventImageSelectorController__Initialize.c`
//   `d:/2/tools/decomp_full/EventImageSelectorController.ImageEventDrawData__TryDraw.c`
//   `d:/2/tools/decomp_full/LiveopUIDrawer_EventAssetSprites.EventAssetImages__Initialize.c`
//   `d:/2/tools/decomp_full/LiveopUIDrawer_EventAssetSprites__Initialize.c`
// 实现 = `CardPresentation/Shell/EventImageSelector.cs`。
//
// ============================ 本文件 vs 既有的两份（⛔ 不重复、不打架）============================
// · **生产接线**（四个方法真被宿主调用）= `Shell/DraftModeWindow.cs`（`B109_E35` 接线，原版宿主 =
//   `DraftModeMenuDemo` 而不是 `ParentEventWindow`，证据 → `资料/完全复刻/报告/B109_E32.md` §2）；
//   那一层的断言在 `Editor/B109_E35_DraftImage_Assert.cs`。本文件**不重复**那一条。
// · 组件契约的粗覆盖在 `Editor/B109_E32_EventImage_Assert.cs`。
//   本文件只补 R16 点名、而那两份**没逐条钉**的三条（= 本批简报的验收项）：
//   ① C14/C15 **循环走完仍不命中** ⇒ `LastAssetName == null`（E32 只查了「类型不命中」那一格）
//   ② C17 `HasAsset` 为假 ⇒ `Enabled == false` 且**一张图都不写**
//   ③ C18 外层遍历 N 格 ⇒ **内层恰好被调 N 次**（逐格计数，N = 4；E32 只数了 2）
//   另补四条原版语义（读 `.c` 才看得出来、两份旧断言都没钉）：
//   `TryDraw` 命中后**立刻 return**（同 type 的第二条不覆盖）· 缓存名**只在命中分支写**（循环走完不清）·
//   `Image == null` 不抛 · 外层 `N` 格里「有资产 / 没资产」两档各自的 `enabled` 值。
//
// ============================ 怎么用 ============================
//   `B110_C14_Assert.Run();`            // 自带 sink（合计打到 Console）
//   `B110_C14_Assert.Run(_sink);`       // 宿主注入
//   `-executeMethod B110_C14_Assert.Run`
//   ⚠️ 本批白名单**不含任何断言宿主** ⇒ 今天**一条都不会跑**（如实标）；接线由调度台在
//   `Editor/ShellScene.cs` 的**合计行之前**那一行行尾追加（同 `B109_E35` 的接法，行数零变化）。
//
// ============================ 灭自证（每组怎么才能红）============================
//   · A4 钉「缓存名只在命中分支写」：把 `LastAssetName = a.AssetName` 提到 `if (a.Type != …) continue;` **之前**
//     ⇒ 未命中那一格也会写名 ⇒ A2 / A4 同时红（A2 要 null、A4 要「保持上一次的值」）。
//   · B4 钉「命中后立刻 return」：把 `return sprite != null;` 删掉 ⇒ 第二条同 type 会覆盖名字 ⇒ 红。
//   · C1 钉「`HasAsset` 为假 ⇒ 关掉」：把它写成「照样 `LoadAsset` + `enabled = true`」⇒ `Enabled` 翻真 ⇒ 红。
//   · D1 钉「外层遍历」：写成「只处理第一格 / 中途 return」⇒ 计数 < 4 ⇒ 红。
//   · 每条断言先断**前提**（探针建出来了、表长度对），再断结论 —— ⛔ 不用 `if (x != null) Check(...)`
//     那种「前提不成立就静默跳过」的写法（铁律：那是空转绿）。

using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>`B110_D4`：C14 / C15 / C17 / C18 四条的**组件契约**断言（生产接线那一半在 `B109_E35`）。</summary>
public static class B110_C14_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B110D4] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    /// <summary>独立跑（自带 sink，合计打到 Console）。</summary>
    public static void Run() { RunCore(_sink); }

    /// <summary>宿主跑（用宿主的 sink，合计进宿主读数）。</summary>
    public static void Run(CheckSink s) { RunCore(s ?? _sink); }

    // 三个 type：`GameModeIcon(12)` 当「命中档」· `HeaderImage(3)` / `BackgroundImage(1)` 当「未命中档」。
    const int T_Hit = (int)LiveOpsAssetRef.GameModeIcon;
    const int T_Miss1 = (int)LiveOpsAssetRef.HeaderImage;
    const int T_Miss2 = (int)LiveOpsAssetRef.BackgroundImage;

    /// <summary>探针资产名 —— **故意不在工程里**（本机没有那批 liveops 图）⇒ `LoadAsset` 必然返 null。
    /// 这样「`Enabled` 写没写 / 名字记没记」这两个可观的量才不被「图恰好导进来了」干扰。</summary>
    const string ProbeArt = "B110_D4_Probe_NotInProject";

    /// <summary>自检用的「一格图」：只记两格 + 各被写了几次。**不是 MonoBehaviour**。</summary>
    sealed class ProbeTarget : IEventImageTarget
    {
        Texture2D _spr;
        bool _enabled;
        public Texture2D Sprite { get { return _spr; } set { _spr = value; SpriteWrites++; } }
        public bool Enabled { get { return _enabled; } set { _enabled = value; EnabledWrites++; } }
        /// <summary>`Sprite` 被写过几次。</summary>
        public int SpriteWrites;
        /// <summary>`Enabled` 被写过几次。</summary>
        public int EnabledWrites;
    }

    static List<EventImageSelectorController.EventAsset> AssetsOf(params int[] types)
    {
        var a = new List<EventImageSelectorController.EventAsset>();
        for (int i = 0; i < types.Length; i++)
            a.Add(new EventImageSelectorController.EventAsset { Type = types[i], AssetName = ProbeArt });
        return a;
    }

    static LiveOpsLocalEvent EventOf(params int[] types)
    {
        var arr = new LiveOpsLocalAsset[types.Length];
        for (int i = 0; i < types.Length; i++)
            arr[i] = new LiveOpsLocalAsset { Key = types[i], SubObjectName = ProbeArt };
        return new LiveOpsLocalEvent { EventId = "b110_d4_probe", Assets = arr };
    }

    static void RunCore(CheckSink s)
    {
        MenuCheck.Section(s, "B110_D4 · C14/C15/C17/C18 组件契约（生产接线那一半 → B109_E35）");
        try { TryDrawCore(s); }
        catch (System.Exception e)
        {
            MenuCheck.True(s, false, "C15 断言段抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message);
        }
        try { ControllerCore(s); }
        catch (System.Exception e)
        {
            MenuCheck.True(s, false, "C14 断言段抛异常：" + e.GetType().Name + "：" + e.Message);
        }
        try { AssetImagesCore(s); }
        catch (System.Exception e)
        {
            MenuCheck.True(s, false, "C17 断言段抛异常：" + e.GetType().Name + "：" + e.Message);
        }
        try { OuterCore(s); }
        catch (System.Exception e)
        {
            MenuCheck.True(s, false, "C18 断言段抛异常：" + e.GetType().Name + "：" + e.Message);
        }
        Debug.Log(s.P + "B110D4 合计：" + s.Pass + " 通过 / " + s.Fail + " 失败");
    }

    // ============================================================ C15 `ImageEventDrawData.TryDraw`
    // 原版 5 句（`.c:34-82`）：命中 ⇒ 写 `+0x20`（缓存名）→ Load → `sprite` → `enabled = 1` → **return**；
    // 循环走完 ⇒ `enabled = 0`（**`+0x20` 不清**）。
    static void TryDrawCore(CheckSink s)
    {
        // ---- A1：命中格 ----
        var t1 = new ProbeTarget();
        var d1 = new EventImageSelectorController.ImageEventDrawData { Image = t1, EventAssetType = T_Hit };
        bool ok1 = d1.TryDraw(AssetsOf(T_Hit));
        MenuCheck.Check(s, d1.LastAssetName, ProbeArt, "A1-① 命中 ⇒ 缓存名 = 资产名（原版 `.c:34` 那一句）");
        MenuCheck.Check(s, ok1, false, "A1-② 图不在工程 ⇒ 返回 false（`Load` 返 null 的同一支）");
        MenuCheck.Check(s, t1.EnabledWrites, 1, "A1-③ `Enabled` 恰被写 **1** 次（原版命中支只写一次）");
        MenuCheck.Check(s, t1.Enabled, false, "A1-④ 图取不到 ⇒ `Enabled == false`（本仓 `Enabled = sprite != null` 那一条等价）");

        // ---- A2：**从未命中**的格，循环走完 ⇒ 名字仍是 null（本批验收项 ①）----
        var t2 = new ProbeTarget();
        var d2 = new EventImageSelectorController.ImageEventDrawData { Image = t2, EventAssetType = T_Miss1 };
        bool ok2 = d2.TryDraw(AssetsOf(T_Hit, T_Miss2, T_Hit));      // 整张表都**不含** T_Miss1
        MenuCheck.Check(s, d2.LastAssetName, (string)null, "A2-① 循环走完仍不命中 ⇒ `LastAssetName` **保持 null**（🧨 把写名提到 type 判断之前 ⇒ 红）");
        MenuCheck.Check(s, ok2, false, "A2-② 未命中 ⇒ 返回 false");
        MenuCheck.Check(s, t2.Enabled, false, "A2-③ 未命中 ⇒ `Enabled == false`（原版循环走完那一句 `set_enabled(0)`）");
        MenuCheck.Check(s, t2.EnabledWrites, 1, "A2-④ 未命中那一支也**写了一次** `Enabled`（⛔ 不是「什么都不做」）");

        // ---- A3：空表 / null 表 ----
        var t3 = new ProbeTarget();
        var d3 = new EventImageSelectorController.ImageEventDrawData { Image = t3, EventAssetType = T_Hit };
        MenuCheck.Check(s, d3.TryDraw(new List<EventImageSelectorController.EventAsset>()), false, "A3-① 空表 ⇒ false");
        MenuCheck.Check(s, d3.LastAssetName, (string)null, "A3-② 空表 ⇒ 名字仍是 null");
        MenuCheck.Check(s, d3.TryDraw(null), false, "A3-③ `eventAssets == null` 不抛、返 false（原版 `param_2 != 0` 那道闸）");

        // ---- A4：缓存名**只在命中分支写** —— 命中过之后再来一次未命中，名字**不清**（原版 `.c` 事实）----
        var t4 = new ProbeTarget();
        var d4 = new EventImageSelectorController.ImageEventDrawData { Image = t4, EventAssetType = T_Hit };
        d4.TryDraw(AssetsOf(T_Hit));
        string afterHit = d4.LastAssetName;
        d4.TryDraw(AssetsOf(T_Miss1, T_Miss2));                     // 换一张不含 T_Hit 的表
        MenuCheck.Check(s, afterHit, ProbeArt, "A4 **前提**：第一次命中真的记下了名字（否则下一条等于没查）");
        MenuCheck.Check(s, d4.LastAssetName, ProbeArt, "A4-① 命中后改喂不含该 type 的表 ⇒ 名字**保持上一次的值**（原版循环走完不清缓存；🧨 「每次都清空」⇒ 红）");
        MenuCheck.Check(s, t4.Enabled, false, "A4-② 但 `Enabled` 被关掉了（名字缓存与显隐是两件事）");

        // ---- A5：`Image == null` ----
        var d5 = new EventImageSelectorController.ImageEventDrawData { Image = null, EventAssetType = T_Hit };
        MenuCheck.Check(s, d5.TryDraw(AssetsOf(T_Hit)), false, "A5 `Image == null` ⇒ 返 false、不抛（原版两处 NRE 那道闸的等价）");
        MenuCheck.Check(s, d5.LastAssetName, (string)null, "A5-② `Image == null` ⇒ 连名字都不记（原版 `+0x10 == 0` 先抛，我们这道闸在更前面）");
    }

    // ============================================================ C14 `EventImageSelectorController.Initialize`
    // 原版 3 句（`.c`）：`if (list == 0) NRE; foreach (d in list) d.TryDraw(assets);`
    static void ControllerCore(CheckSink s)
    {
        var a = new ProbeTarget();
        var b = new ProbeTarget();
        var c = new ProbeTarget();
        var list = new List<EventImageSelectorController.ImageEventDrawData>
        {
            new EventImageSelectorController.ImageEventDrawData { Image = a, EventAssetType = T_Hit },
            new EventImageSelectorController.ImageEventDrawData { Image = b, EventAssetType = T_Miss1 },
            new EventImageSelectorController.ImageEventDrawData { Image = c, EventAssetType = T_Miss2 },
        };
        MenuCheck.Check(s, list.Count, 3, "B1 **前提**：喂 **3** 格（下面 B2 的计数按 3 算）");

        int drawn = EventImageSelectorController.Initialize(list, AssetsOf(T_Hit));
        MenuCheck.Check(s, drawn, 0, "B1-① 图不在工程 ⇒ 画上 **0** 格");
        MenuCheck.Check(s, list[0].LastAssetName, ProbeArt, "B1-② 命中那一格记下了资产名");
        MenuCheck.Check(s, list[1].LastAssetName, (string)null, "B1-③ 未命中的第 2 格 ⇒ `LastAssetName == null`（本批验收项 ①）");
        MenuCheck.Check(s, list[2].LastAssetName, (string)null, "B1-④ 未命中的第 3 格 ⇒ `LastAssetName == null`");

        // B2：**每一格都被走了一次**（外层没有提前 break）
        MenuCheck.Check(s, a.EnabledWrites, 1, "B2-① 第 1 格 `Enabled` 恰写 1 次");
        MenuCheck.Check(s, b.EnabledWrites, 1, "B2-② 第 2 格 `Enabled` 恰写 1 次（🧨 外层 `break` ⇒ 0 ⇒ 红）");
        MenuCheck.Check(s, c.EnabledWrites, 1, "B2-③ 第 3 格 `Enabled` 恰写 1 次（🧨 只遍历 2 格 ⇒ 0 ⇒ 红）");

        // B3：null 表 ⇒ 0（原版 NRE 那道闸的等价）
        MenuCheck.Check(s, EventImageSelectorController.Initialize(null, AssetsOf(T_Hit)), 0,
                        "B3 `drawDatas == null` ⇒ 返 0、不抛");

        // B4：命中后**立刻 return** —— 同 type 的第二条不覆盖名字（原版 `.c` 命中支末尾 `return`）
        var d = new ProbeTarget();
        var two = new List<EventImageSelectorController.ImageEventDrawData>
        {
            new EventImageSelectorController.ImageEventDrawData { Image = d, EventAssetType = T_Hit },
        };
        var dup = new List<EventImageSelectorController.EventAsset>
        {
            new EventImageSelectorController.EventAsset { Type = T_Hit, AssetName = "B110_D4_First" },
            new EventImageSelectorController.EventAsset { Type = T_Hit, AssetName = "B110_D4_Second" },
        };
        EventImageSelectorController.Initialize(two, dup);
        MenuCheck.Check(s, two[0].LastAssetName, "B110_D4_First",
                        "B4 同 type 两条 ⇒ 认**第一条**（原版命中支末尾 `return`；🧨 删掉那个 return ⇒ 记成 Second ⇒ 红）");
    }

    // ============================================================ C17 `EventAssetImages.Initialize`
    // 原版 4 句（`.c`）：`has = HasAsset(evt, type)` → `!has ⇒ enabled = 0; return` → `enabled = 1` → `sprite = LoadAsset(...)`
    static void AssetImagesCore(CheckSink s)
    {
        // ---- C1：`HasAsset` 为假 ⇒ 关掉、且**一张图都不写**（本批验收项 ②）----
        var t1 = new ProbeTarget();
        var cell1 = new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = T_Miss1, Image = t1 };
        var evt1 = EventOf(T_Hit);                                  // 事件里**只有** T_Hit
        MenuCheck.Check(s, EventArt.HasAsset(evt1, T_Miss1), false, "C1 **前提**：`HasAsset(evt, T_Miss1) == false`（否则下一条等于没查）");
        bool ok1 = cell1.Initialize(evt1);
        MenuCheck.Check(s, ok1, false, "C1-① `HasAsset` 为假 ⇒ 返回 false");
        MenuCheck.Check(s, t1.Enabled, false, "C1-② `HasAsset` 为假 ⇒ `Enabled == false`（原版 `.c` 第二句 `set_enabled(0)`；🧨 照样置真 ⇒ 红）");
        MenuCheck.Check(s, t1.EnabledWrites, 1, "C1-③ `Enabled` 恰写 1 次");
        MenuCheck.Check(s, t1.SpriteWrites, 0, "C1-④ **一张图都没写**（原版 `!has` 支直接 return，⛔ 不调 `LoadAsset`）");

        // ---- C2：`HasAsset` 为真 ⇒ 先置真、再写图（原版 ③④ 两句的**顺序**）----
        var t2 = new ProbeTarget();
        var cell2 = new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = T_Hit, Image = t2 };
        var evt2 = EventOf(T_Hit);
        MenuCheck.Check(s, EventArt.HasAsset(evt2, T_Hit), true, "C2 **前提**：`HasAsset(evt, T_Hit) == true`");
        bool ok2 = cell2.Initialize(evt2);
        MenuCheck.Check(s, t2.Enabled, true, "C2-① 有资产 ⇒ `Enabled == true`（图取不到也照样置真 —— 原版 `enabled = 1` 无条件）");
        MenuCheck.Check(s, t2.EnabledWrites, 1, "C2-② `Enabled` 恰写 1 次");
        MenuCheck.Check(s, t2.SpriteWrites, 1, "C2-③ `Sprite` 被写 **1** 次（`LoadAsset` 那一跳真被调用）");
        MenuCheck.Check(s, ok2, false, "C2-④ 但图不在工程 ⇒ 返回 false（返回值是**我们**的读口，原版该方法无返回值）");

        // ---- C3：`Image == null` ----
        var cell3 = new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = T_Hit, Image = null };
        MenuCheck.Check(s, cell3.Initialize(evt2), false, "C3 `Image == null` ⇒ 返 false、不抛");
    }

    // ============================================================ C18 外层 `LiveopUIDrawer_EventAssetSprites.Initialize`
    // 原版（`.c`）：`+0x28 = evt`（存事件）→ `foreach (imgs in list) { has = …; enabled = has; if (has) sprite = …; }`
    static void OuterCore(CheckSink s)
    {
        // ---- D1：外层遍历 N 格 ⇒ **内层恰好被调 N 次**（本批验收项 ③）----
        var q0 = new ProbeTarget();     // 有资产
        var q1 = new ProbeTarget();     // 没资产
        var q2 = new ProbeTarget();     // 有资产
        var q3 = new ProbeTarget();     // 没资产
        var cells = new List<LiveopUIDrawer_EventAssetSprites.EventAssetImages>
        {
            new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = T_Hit,   Image = q0 },
            new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = T_Miss1, Image = q1 },
            new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = T_Hit,   Image = q2 },
            new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = T_Miss2, Image = q3 },
        };
        var evt = EventOf(T_Hit);                                       // 只有 T_Hit 有资产
        MenuCheck.Check(s, cells.Count, 4, "D1 **前提**：喂 **4** 格（下面按 4 计数）");

        int drawn = LiveopUIDrawer_EventAssetSprites.Initialize(cells, evt);
        MenuCheck.Check(s, drawn, 0, "D1-① 图不在工程 ⇒ 画上 **0** 格");
        MenuCheck.Check(s, q0.EnabledWrites + q1.EnabledWrites + q2.EnabledWrites + q3.EnabledWrites, 4,
                        "D1-② 外层遍历 **4** 格 ⇒ 内层恰被调 **4** 次（每格各写一次 `Enabled`；🧨 中途 return / 只处理第一格 ⇒ < 4 ⇒ 红）");
        MenuCheck.Check(s, q0.EnabledWrites, 1, "D1-③ 第 1 格恰 1 次");
        MenuCheck.Check(s, q1.EnabledWrites, 1, "D1-④ 第 2 格恰 1 次");
        MenuCheck.Check(s, q2.EnabledWrites, 1, "D1-⑤ 第 3 格恰 1 次");
        MenuCheck.Check(s, q3.EnabledWrites, 1, "D1-⑥ 第 4 格恰 1 次");

        // ---- D2：两档各自的终态 ----
        MenuCheck.Check(s, q0.Enabled, true, "D2-① 有资产那格（第 1）⇒ `Enabled == true`");
        MenuCheck.Check(s, q2.Enabled, true, "D2-② 有资产那格（第 3）⇒ `Enabled == true`");
        MenuCheck.Check(s, q1.Enabled, false, "D2-③ 没资产那格（第 2）⇒ `Enabled == false`");
        MenuCheck.Check(s, q3.Enabled, false, "D2-④ 没资产那格（第 4）⇒ `Enabled == false`");
        MenuCheck.Check(s, q0.SpriteWrites + q2.SpriteWrites, 2, "D2-⑤ 只有「有资产」的两格各写一次图");
        MenuCheck.Check(s, q1.SpriteWrites + q3.SpriteWrites, 0, "D2-⑥ 「没资产」的两格**一张图都没写**");

        // ---- D3：null 表 / null 事件 ----
        MenuCheck.Check(s, LiveopUIDrawer_EventAssetSprites.Initialize(null, evt), 0, "D3-① `eventAssetImages == null` ⇒ 返 0、不抛");
        var qt = new ProbeTarget();
        var one = new List<LiveopUIDrawer_EventAssetSprites.EventAssetImages>
        {
            new LiveopUIDrawer_EventAssetSprites.EventAssetImages { EventAssetType = T_Hit, Image = qt },
        };
        LiveopUIDrawer_EventAssetSprites.Initialize(one, null);
        MenuCheck.Check(s, EventArt.HasAsset(null, T_Hit), false, "D3-② `HasAsset(null, …) == false`（原版 `HasAsset` 对 null 事件的等价）");
        MenuCheck.Check(s, qt.Enabled, false, "D3-③ 事件为 null ⇒ 那格被关掉（走 `!has` 支）");
    }
}
