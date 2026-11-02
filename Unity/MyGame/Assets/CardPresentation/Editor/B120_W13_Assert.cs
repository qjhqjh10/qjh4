// B120_W13_Assert.cs — 执行写手 `B120-W13`（2026-11-02）· `B120-Q04`：**3D 落点射线链**
//
// ============================ 它盯的是什么 ============================
// `Battle/BattleDriver.cs` 的 `TryHitSlotByBoardRay`（= 原版 `BattleManager.GetTargetInPos` 真生效支的对应物）
// 与它的调用方 `HitSlot`。Q04 报的六处不一致，本件逐处钉住**改后的值**：
//   ① 取**首条卡**（⛔ 不遍历取最近）· ② 掩码 `1 << BoardCardLayer`（层 10 = 原版 `BoardElements`；🔴 2026-11-02 `B120-W27` 已一比一）
//   ③ miss ⇒ **-1**（⛔ 不回落投影）· ④ 返回格号（外层签名保留）· ⑤ 屏幕点由调用方世界点往返
//   ⑥ 拖牌落格是**另一条链**（`MinionManager.GetClosestAvailableSlot`）—— ⛔ 不在本件范围
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · `d:/2/tools/decomp_full/BattleManager__GetTargetInPos.c:204-265`（逐句读；`:250-252` = `iVar7 < 1 ⇒ return 0`）
//   · ILSpy `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:46450-46495`（`LayerMask.NameToLayer("BoardElements")` 明文 +
//     `Physics.RaycastNonAlloc(origin, direction, boardTargetOnTouchUp, 200f, layerMask)` + `hits[0]` + `GetComponentInParent<CardScript>()`）
//   · 层整数 10 = 原版 `ProjectSettings/TagManager.asset` 的 `layers` 索引 10（同表 8 = CustomPostEffectIgnore）
//   · `200f` = `工具/read_literal.py <GameAssembly.dll> 0x1834b3258`（4B float 实读 200）
//   · Q04 全文 → `资料/完全复刻/报告/B120_Q_未决判据.md` §四
//
// ============================ 怎么用 ============================
//   `B120_W13_Assert.Run();`   （静态类 + 无参 `Run()`；**本文件尚未接线**，宿主与聚合行由调度台决定）
//   本文件不建持久资产：探针树（相机 / 驱动 / 探针卡）在 `finally` 里 `DestroyImmediate`。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A4 ← 把 `TryHitSlotByBoardRay` 改回「遍历全部命中、取 `distance` 最小」⇒ 方法体里重新出现 `distance` ⇒ 红
//          （这条是**唯一无条件**的灭自证：③ 段的行为判别力受 `RaycastNonAlloc` 缓冲序影响，见 C3 的读数）
//   · A5 ← 把 `HitSlot` 的 3D 档改回「先射线、miss 再回落投影」⇒ 那句 early-return 的形状对不上 ⇒ 红
//   · A1/A2 ← 掩码改回 `1 << 别的层` / 丢了 `QueryTriggerInteraction.UseGlobal` ⇒ 红
//   · B1 ← 让探针卡的 3D 挡板不建（删 `CardView.cs` 的 `EnsureTapBlockCollider(go)`）⇒ 射线打不到 ⇒ 红
//   · B3 ← 把回落投影加回 `HitSlot` ⇒ 关掉挡板后这里会答**格号**而不是 -1 ⇒ 红
//   · C2 ← 改成「取最近」⇒ 若本机缓冲序不是「最近在前」（C3 打印读数），答案会与「首条卡」不同 ⇒ 红
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环**：B/C 段是编辑模式下的一次性几何探针（`Physics.RaycastNonAlloc` 在编辑模式可用）。
//   · **不断言「真机上点得中」**：那条要真 Play（`资料/真Play待验清单.md`）—— 本件只钉射线链的形状与语义。
//   · **③ 段（首条 vs 最近）的行为判别力不是无条件成立的**：`RaycastNonAlloc` 的缓冲序**不保证**是距离序，
//     所以「本机这一跑恰好最近在前」时该夹具**分不出**两种实现 —— 那时 C3 会打一行「判别力：无」，
//     由 A4 的源码闸兜住。⛔ 别把 C3 的读数当结论。
//   · **层号已一比一**（🔴 2026-11-02 `B120-W27` 订正）：层 10 已命名 `BoardElements`（= `ArenaSlots.BoardCardLayer`），
//     卡被换到 10、射线掩码 `1 << BoardCardLayer` = 原版那句；层 8（`ArenaLayer`）只剩 arena prefab 的地面/背景/VFX 宿主。
//     ⚠️ 本件 B/C 段的探针卡现在**必须**设成 `BoardCardLayer`（掩码不再含 bit8）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using CardPresentation;

/// <summary>执行写手 `B120-W13`：3D 落点射线链（`GetTargetInPos` 真生效支）的源码闸 + 行为断言。</summary>
public static class B120_W13_Assert
{
    const string Prefix = "[B120W13] ";
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

    /// <summary>只记录、不计分（用例 = 「判别力」「前提读数」这类**不是断言**的量）。</summary>
    static void Tr(string what) { Debug.Log(Prefix + "· " + what); }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B120-W13：3D 落点射线链（`GetTargetInPos` 真生效支）开始 ===");
        A_SourceGate();
        B_Behaviour();
        C_FirstVersusNearest();
        Debug.Log(Prefix + "—— 合计：通过 " + _ok + " · 失败 " + _bad);
    }

    // ==================================================================
    //  A · 源码闸（形状；⛔ 不用我们的常量反推原版）
    // ==================================================================
    static void A_SourceGate()
    {
        string p = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        Check(File.Exists(p), "A0（前提）源码文件在：" + p + "（⛔ 读不到就红，别改成跳过）");
        if (!File.Exists(p)) return;

        string raw = File.ReadAllText(p);
        string src = StripLineComments(raw);          // 注释里的示例不算接线

        // A1 —— 射线调用的**形状**（原版 5 参重载：200f + 1<<BoardElements + 默认 QueryTriggerInteraction）
        string flat = Flatten(src);
        Check(flat.Contains("Physics.RaycastNonAlloc(ray, _boardRayHits, BoardTargetRayMaxDistance, BoardTargetRayLayerMask, QueryTriggerInteraction.UseGlobal)"),
            "A1 ★ 射线 = `Physics.RaycastNonAlloc(ray, _boardRayHits, BoardTargetRayMaxDistance, BoardTargetRayLayerMask, QueryTriggerInteraction.UseGlobal)`"
            + "（原版 `...RaycastNonAlloc(origin, direction, buf, 200f, layerMask)` 的 5 参重载 ⇒ `UseGlobal`，"
            + "原工程与本工程 `m_QueriesHitTriggers` 都是 1）"
            + "｜🧨 改回 `QueryTriggerInteraction.Ignore` ⇒ 红");

        // A2/A3 —— 两个常量（值本身照抄原版：200f；掩码 = 1 << 本仓层）
        Check(flat.Contains("const float BoardTargetRayMaxDistance = 200f;"),
            "A2 ★ `BoardTargetRayMaxDistance = 200f`（原版 `DAT_1834b3258` 4B 实读 = 200）"
            + "｜🧨 改成别的距离 ⇒ 红（远处卡会点不中 / 近处地面被误命中）");
        Check(flat.Contains("const int BoardTargetRayLayerMask = 1 << ArenaSlots.BoardCardLayer;"),
            "A3 ★ `BoardTargetRayLayerMask = 1 << ArenaSlots.BoardCardLayer`（层 10 = 原版 `BoardElements`；🔴 2026-11-02 `B120-W27` 已一比一）"
            + "｜🧨 改回 `1 << ArenaLayer`（8）/ 写死别的数 ⇒ 红（卡在 10 上 ⇒ 点卡全打不中，且地面/背景会先被命中）");

        // A4 —— **无条件**灭自证：射线方法体里不许再出现「按 distance 挑」
        int i0 = src.IndexOf("bool TryHitSlotByBoardRay(", StringComparison.Ordinal);
        int i1 = src.IndexOf("static int SlotOfView(", StringComparison.Ordinal);
        Check(i0 >= 0 && i1 > i0, "A4a（前提）能在源码里切出 `TryHitSlotByBoardRay` 的方法体（" + i0 + " → " + i1 + "）");
        if (i0 >= 0 && i1 > i0)
        {
            string body = src.Substring(i0, i1 - i0);
            int distHits = CountOf(body, "distance");
            Check(distHits == 0,
                "A4 ★ 射线方法体里**没有** `distance`（= 不再「遍历全部命中取最近」；原版直接吃 `hits[0]`）"
                + "，实得 " + distHits + " 处"
                + "｜🧨 改回 `if (h.distance >= bestDist) continue;` 那一套 ⇒ 红（**这是 ③ 段唯一无条件的灭自证**）");
            Check(CountOf(body, "SlotOfView(") == 1,
                "A4b 方法体里 `SlotOfView(` **恰好 1 处** ⇒ 命中即返回（首条），不会在循环里继续比"
                + "｜实得 " + CountOf(body, "SlotOfView("));
        }

        // A5 —— `HitSlot`：3D 档**直接返回**射线结果（⛔ 不回落投影）
        Check(flat.Contains("if (use3DBoard && boardCam != null && cam != null) { int raySlot; TryHitSlotByBoardRay(views, world, out raySlot); return raySlot; }"),
            "A5 ★ `HitSlot` 的 3D 档 = **一句 early-return**（`TryHitSlotByBoardRay` → `return raySlot`），"
            + "miss 即 -1、⛔ 不回落投影（原版 `iVar7 < 1 ⇒ return 0`，`BattleManager__GetTargetInPos.c:250-252`）"
            + "｜🧨 改回「先射线、miss 再落到投影循环」⇒ 形状对不上 ⇒ 红");

        // A6 —— 🔴 2026-11-02（`B120-W27`）**就地改写**：原来这里断「本工程**没有** `BoardElements`」（层号无法一比一的证据）
        //        —— 迁移完成后那条会红。现在断**正命题**：层 10 存在、名字就是 `BoardElements`、且与常量同一格。
        int boardElements = LayerMask.NameToLayer("BoardElements");
        Check(boardElements == ArenaSlots.BoardCardLayer,
            "A6 ★ 本工程**有** `BoardElements` 这一层，且 `LayerMask.NameToLayer(\"BoardElements\")` = `ArenaSlots.BoardCardLayer`"
            + "（实得 " + boardElements + " / " + ArenaSlots.BoardCardLayer + "）—— 原版 `1 << NameToLayer(\"BoardElements\")`（层 10）**已一比一**"
            + "｜🧨 层表里删掉/改名这一层、或改动 `BoardCardLayer` 的值 ⇒ 红");
        Check(ArenaSlots.ArenaLayer == 8,
            "A6b ★ 本仓的**竞技场层** `ArenaSlots.ArenaLayer` 仍是 **8**（⛔ 没被改名/改值；它现在**只**留给 arena prefab 的地面/背景/VFX 宿主）"
            + "｜实得 " + ArenaSlots.ArenaLayer);
    }

    // ==================================================================
    //  B · 行为：命中卡 → 卡所在格；空白 → -1；关掉 3D 挡板 → 仍 -1（⛔ 不回落）
    // ==================================================================
    static void B_Behaviour()
    {
        var root = new GameObject("B120W13_probe");
        root.transform.position = new Vector3(0f, 400f, 0f);      // 抬到 400 米高：射线沿途不会撞上别人的 collider
        var camGo = new GameObject("B120W13_cam");
        CardView card = null;
        try
        {
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -12f);
            camGo.transform.localRotation = Quaternion.identity;  // Unity 相机看自身 +Z
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 300f;

            var drv = root.AddComponent<BattleDriver>();
            drv.cam = cam;
            drv.boardCam = cam;                                   // 测试里两台相机合一：`WorldToScreenPoint` → `ScreenPointToRay` 是恒等往返
            drv.use3DBoard = true;

            var data = CardData.Simple("W13Probe", 3, 3, 4);
            card = CardView.Create(root.transform, data, "w13_card", CardFace.Board);
            Check(card != null, "B0a（前提）探针卡建出来了（`CardView.Create(face: Board)`）");
            if (card == null) return;
            card.SetLayer(ArenaSlots.BoardCardLayer);             // = 出牌落地那一步（🔴 `B120-W27`：卡在层 10，掩码只含 bit10）
            Physics.SyncTransforms();

            var col = card.TapBlockColliderForTest;
            Check(col != null, "B0b（前提）场上卡 `body3D` 上有 3D `BoxCollider`（取不到 ⇒ 下面全部会一起红，不是空转绿）");
            if (col == null) return;
            Check(col.gameObject.layer == ArenaSlots.BoardCardLayer,
                "B0c（前提）挡板的层 = `ArenaSlots.BoardCardLayer`(" + ArenaSlots.BoardCardLayer + ") ⇒ 落在射线掩码里"
                + "｜实得 " + col.gameObject.layer);

            var views = new Dictionary<int, CardView> { { 3, card } };
            Vector3 aim = col.bounds.center;                      // 射线穿过挡板中心

            // B1 —— 命中卡 ⇒ 卡所在格
            int hit = drv.HitSlotWithViewsForTest(views, aim);
            Check(hit == 3, "B1 ★ 射线对准卡 ⇒ 返回**它所在的格号** 3（实得 " + hit + "）"
                + "｜🧨 删掉 `CardView` 的 `EnsureTapBlockCollider(go)` ⇒ 打不到 ⇒ -1 ⇒ 红");

            // B2 —— 空白处 ⇒ -1（原版 `iVar7 < 1 ⇒ return 0`）
            int miss = drv.HitSlotWithViewsForTest(views, aim + Vector3.up * 40f);
            Check(miss == -1, "B2 ★ 射线打在空白处 ⇒ **-1**（原版 `iVar7 < 1 ⇒ return 0`；实得 " + miss + "）"
                + "｜🧨 若有人把它改成「兜到最近的格」，这里会变 ≥0 ⇒ 红");

            // B3 —— 关掉 3D 挡板 ⇒ 仍 -1（**不回落投影**；旧版这里会走 `CardHit` 的投影判据）
            bool was = col.enabled;
            col.enabled = false;
            Physics.SyncTransforms();
            int off = drv.HitSlotWithViewsForTest(views, aim);
            col.enabled = was;
            Physics.SyncTransforms();
            Check(off == -1, "B3 ★ **关掉卡的 3D 挡板**后同一个点 ⇒ 仍是 **-1**（3D 档 miss 即 null，⛔ 不回落投影；实得 " + off + "）"
                + "｜🧨 把 `HitSlot` 的回落投影加回来 ⇒ 这里会走 `CardHit` 的投影、答 3 ⇒ 红");

            // B3b —— 前提读数：「旧投影判据在这个点上会不会命中」（不是断言；`Contains` 与 `CardHit` 不完全等同）
            Vector3 sp = cam.WorldToScreenPoint(aim);
            Vector3 ptr = LayoutSpace.ScreenToWorld(new Vector2(sp.x, sp.y), cam);
            Tr("B3b 参考读数：旧投影判据 `Contains(ptr)` = " + card.Contains(ptr)
               + "（ptr = " + ptr.ToString("F3") + "）；`CardHit` 同口径但走 collider 四角，不完全等同 ⇒ 只作参考");

            // B4 —— 恢复后仍是 3（证明 B3 不是「探针卡坏了」）
            Check(drv.HitSlotWithViewsForTest(views, aim) == 3,
                "B4 ★ 重新打开挡板后仍答 3 ⇒ B3 的 -1 是「不回落」而非「卡坏了」");
        }
        finally
        {
            if (card != null) UnityEngine.Object.DestroyImmediate(card.gameObject);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  C · 灭自证：首条卡 vs 最近卡（同一射线上排多张）
    // ==================================================================
    static void C_FirstVersusNearest()
    {
        var root = new GameObject("B120W13_multi");
        root.transform.position = new Vector3(0f, 400f, 0f);
        var camGo = new GameObject("B120W13_multi_cam");
        var made = new List<CardView>();
        try
        {
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -12f);
            camGo.transform.localRotation = Quaternion.identity;
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;

            var drv = root.AddComponent<BattleDriver>();
            drv.cam = cam; drv.boardCam = cam; drv.use3DBoard = true;

            var data = CardData.Simple("W13MultiProbe", 3, 3, 4);
            var first = CardView.Create(root.transform, data, "w13_m0", CardFace.Board);
            if (first == null) { Check(false, "C0（前提）探针卡建出来了"); return; }
            first.SetLayer(ArenaSlots.BoardCardLayer);
            Physics.SyncTransforms();
            var col0 = first.TapBlockColliderForTest;
            if (col0 == null) { Check(false, "C0（前提）探针卡有 3D 挡板"); return; }

            // 挡板相对卡根的局部偏移（各张卡同构 ⇒ 同一偏移）
            Vector3 local = col0.bounds.center - first.transform.position;
            Vector3 camPos = cam.transform.position;

            // 4 张卡排在同一条**相机轴**上（距离 6 / 8 / 10 / 12）⇒ 一条射线全穿
            var views = new Dictionary<int, CardView>();
            float[] dists = { 6f, 8f, 10f, 12f };
            for (int i = 0; i < dists.Length; i++)
            {
                Vector3 c = camPos + Vector3.forward * dists[i];
                if (i == 0) { first.transform.position = c - local; made.Add(first); }
                else
                {
                    var cv = CardView.Create(root.transform, data, "w13_m" + i, CardFace.Board);
                    if (cv == null) { Check(false, "C0（前提）第 " + i + " 张探针卡建出来了"); return; }
                    cv.SetLayer(ArenaSlots.BoardCardLayer);
                    cv.transform.position = c - local;
                    made.Add(cv);
                }
                views[3 + i] = made[i];
            }
            Physics.SyncTransforms();

            // 原始缓冲（口径 = 生产：同一个 mask / 同一个 UseGlobal）
            var buf = new RaycastHit[16];
            var ray = new Ray(camPos, Vector3.forward);
            int n = Physics.RaycastNonAlloc(ray, buf, 200f, 1 << ArenaSlots.BoardCardLayer, QueryTriggerInteraction.UseGlobal);
            Check(n >= 2, "C1（前提）这条射线至少命中 2 张卡（实得 " + n + "）—— 否则下面没有判别前提");

            int firstSlot = int.MinValue, nearestSlot = int.MinValue, firstIdx = -1, nearestIdx = -1;
            float best = float.MaxValue;
            for (int i = 0; i < n && i < buf.Length; i++)
            {
                int s = SlotOfHit(views, buf[i]);
                if (s < 0) continue;
                if (firstSlot == int.MinValue) { firstSlot = s; firstIdx = i; }
                if (buf[i].distance < best) { best = buf[i].distance; nearestSlot = s; nearestIdx = i; }
            }
            Check(firstIdx >= 0 && nearestIdx >= 0, "C1b（前提）缓冲里既有「首条卡」也有「最近卡」");

            int got = drv.HitSlotByBoardRayForTest(views, camPos + Vector3.forward * dists[0]);
            Check(got == firstSlot,
                "C2 ★ 生产答案 = 缓冲里**第一条卡**（格 " + firstSlot + " · 缓冲下标 " + firstIdx + "），实得 " + got
                + "｜🧨 改回「遍历取最近」（= 格 " + nearestSlot + "）⇒ 只要本机缓冲序不是最近在前 ⇒ 红");

            Tr("C3 判别力读数：首条卡 = 格 " + firstSlot + "（下标 " + firstIdx + "）· 最近卡 = 格 " + nearestSlot
               + "（下标 " + nearestIdx + " · distance " + best.ToString("F2") + "）⇒ "
               + (firstSlot != nearestSlot ? "**有判别力**（本夹具能把两种实现分开）"
                                           : "**无判别力**（本机缓冲序恰好最近在前；由 A4 源码闸无条件兜住）"));
        }
        finally
        {
            foreach (var c in made) if (c != null) UnityEngine.Object.DestroyImmediate(c.gameObject);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>命中 → 格号（`views` 里找不到 ⇒ -1）。⛔ 按对象身份比，别按名字/位置。</summary>
    static int SlotOfHit(Dictionary<int, CardView> views, RaycastHit h)
    {
        if (h.collider == null) return -1;
        var v = h.collider.GetComponentInParent<CardView>();
        if (v == null) return -1;
        foreach (var kv in views) if (object.ReferenceEquals(kv.Value, v)) return kv.Key;
        return -1;
    }

    // ---- 文本工具 ----

    /// <summary>把 `//` 之后的内容裁掉（本文件只用来数「代码里有没有这个词」，注释里的示例不算）。</summary>
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

    // ==================================================================
    //  R · 层号结论（🔴 2026-11-02 `B120_W27`：**已一比一**，本口现在返回 **10**）
    // ==================================================================
    //  ⚠️ 放在 `Run` 之外单独可调：它读的是**本工程**的层表状态（`ProjectSettings/TagManager.asset`）。
    /// <summary>本工程层表里 `BoardElements` 的索引（现读 = **10** = `ArenaSlots.BoardCardLayer`；老版本返回 -1 的时代已结束）。</summary>
    public static int BoardElementsLayerOrMinusOne() { return LayerMask.NameToLayer("BoardElements"); }
}
