// B118_T11_Assert.cs — 执行代理 `B118-W9`（`B118-T11`「场上卡挡地面点击射线」）的断言
//
// ============================ 它盯的是什么 ============================
// `Core/CardView.cs` 的 `EnsureTapBlockCollider`：场上卡 `body3D` 上那颗 **3D `BoxCollider`**（原版 `Card 3D` 那颗）。
// 缺了它 ⇒ `TapParticleController` 的 3D 射线穿过卡体、落到 board limit / 地面 ⇒ **点场上卡会多播一次地面反馈**。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · `CardPrefab.prefab`（`d:/2/tools/assetripper_out/ExportedProject/Assets/`）
//     `--- !u!65 &65948132087033897`：`m_Size (1.827034, 3.01, 0.5)` · `m_Center (0.013507843, 1.55, -0.016031265)`
//     · `m_IsTrigger: 0` · `m_Enabled: 1`；节点 `Card 3D` `m_Layer: 10`（`BoardElements`）、`localScale 0.88586`
//   · 用它的那一侧：`decomp_full/TapParticleController__Update.c:162`（mask `0x40000C01` 含 bit10）+ 闸⑬
//     「最近那条没有 `TapHitMaterialDefinition` ⇒ 整帧不播、不顺延」（本仓 `Battle/TapParticleController.cs:254-256`）
//   · 裁定与三条正证 → `资料/完全复刻/判据/B118_T11_碰撞层裁定.md`
//
// ============================ 怎么用 ============================
//   `B118T11_Assert.Run();`   （静态类 + 无参 `Run()`；**本文件尚未接线**，宿主与聚合行由调度台决定）
//   本文件不建持久资产：探针树在 `finally` 里 `DestroyImmediate`。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//   · A3/A4 ← 删掉 `CardView.cs:7942` 那句 `EnsureTapBlockCollider(go);` ⇒ 卡上没有 3D collider
//             ⇒ 射线打不到本卡 ⇒ A3 红、A4 随之红。
//   · A3    ← 把那颗 collider 的 `enabled` 关掉 ⇒ A5 的对照支立刻能看到「打不到」，证明 A3 不是空转。
//   · A1    ← 把 size / center 改成「看起来差不多」的值（例如照 2D 那组 1.97 × 3.02）⇒ A1 红。
//   · A2    ← 把 `body.layer = gameObject.layer;` 删掉（`new GameObject` 默认层 0）且卡不换层 ⇒ A2 红。
//   · A5b   ← 让手牌档也建 `body3D`（`BuildBody3D` 无条件跑）且不关 ⇒ A5b 红（手牌会挡住地面点击）。
//
// ============================ ⚠️ 本文件**不覆盖**的 ============================
//   · `TapParticleController.Tick` 的八道闸（`Mouse.current` / `BattleDriver` / `EventSystem`）—— 批处理里没有，
//     A4 只断「闸⑬口径等价」：最近那条身上**没有** `TapHitMaterialDefinition` ⇒ 生产码走到 `:254-256` 的 `return`。
//   · 真机「点场上卡不出地面粒子」（视觉/听觉）→ `资料/真Play待验清单.md`。
using UnityEngine;
using CardPresentation;

public static class B118T11_Assert
{
    const string Tag = "B118T11";
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static bool VecEq(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude <= 1e-10f; }

    // 原版字面量（⛔ 别读我们自己的常量反推 —— 那是拿实现证明实现）
    static readonly Vector3 WantSize = new Vector3(1.827034f, 3.01f, 0.5f);
    static readonly Vector3 WantCenter = new Vector3(0.013507843f, 1.55f, -0.016031265f);

    static readonly RaycastHit[] _buf = new RaycastHit[8];

    /// <summary>照生产码挑**最近的**那条（`RaycastNonAlloc` 不排序，见 `TapParticleController.cs:234-239`）。</summary>
    static bool NearestHit(Ray ray, out RaycastHit best, out int count)
    {
        count = Physics.RaycastNonAlloc(ray, _buf, float.MaxValue, TapParticleController.CollisionMask);
        best = default(RaycastHit);
        bool any = false;
        float bd = float.MaxValue;
        for (int i = 0; i < count; i++)
            if (_buf[i].distance < bd) { bd = _buf[i].distance; best = _buf[i]; any = true; }
        return any;
    }

    public static void Run()
    {
        var root = new GameObject("B118T11_probe");
        // 抬到 100 米高：离战场装配远远的，射线沿途不会撞上别人的 collider
        root.transform.position = new Vector3(0f, 100f, 0f);
        try
        {
            var data = CardData.Simple("TapBlockProbe", 3, 3, 4);
            var board = CardView.Create(root.transform, data, "board_probe", CardFace.Board);
            Check(board != null, "★ 前提：场上档探针卡建出来了（`CardView.Create(face: Board)`）");
            if (board == null) return;

            board.SetLayer(ArenaSlots.ArenaLayer);      // = 出牌落地路径 `BattleDriver` 那一步

            var col = board.TapBlockColliderForTest;
            Check(col != null,
                  "A1a ★ 前提：场上卡 `body3D` 上有 3D `BoxCollider`（取不到 ⇒ 下面几条会一起红，不是空转绿）");

            // ---- A1：几何逐值照原版 `Card 3D` ----
            Check(col != null && VecEq(col.size, WantSize),
                  "A1 size = 原版 `CardPrefab.prefab` `Card 3D` 那颗 `BoxCollider` 的 (1.827034, 3.01, 0.5)"
                + "｜🧨 改成 2D 那组 1.97 × 3.02 ⇒ 红（实得 " + (col != null ? col.size.ToString("F6") : "null") + "）");
            Check(col != null && VecEq(col.center, WantCenter),
                  "A1b center = 原版 (0.013507843, 1.55, -0.016031265)（网格原点在卡底边 ⇒ 中心抬 1.55）"
                + "｜实得 " + (col != null ? col.center.ToString("F6") : "null"));
            Check(col != null && !col.isTrigger && col.enabled,
                  "A1c 原版 `m_IsTrigger: 0` / `m_Enabled: 1` ⇒ `isTrigger == false` 且 `enabled == true`");

            // ---- A2：落在射线掩码里 ----
            Check(col != null && col.gameObject.layer == ArenaSlots.ArenaLayer,
                  "A2 场上形态 `body3D` 的层 = `ArenaSlots.ArenaLayer`(" + ArenaSlots.ArenaLayer + ")"
                + "（`SetLayer` 递归覆盖子件）｜实得 " + (col != null ? col.gameObject.layer.ToString() : "null"));
            Check((TapParticleController.CollisionMask & (1 << ArenaSlots.ArenaLayer)) != 0,
                  "A2b `TapParticleController.CollisionMask`(0x" + TapParticleController.CollisionMask.ToString("X8")
                + ") 含 bit" + ArenaSlots.ArenaLayer + " —— 否则这颗挡板对那条射线是**看不见的**（静默）");
            Check(col != null && col.gameObject.activeSelf,
                  "A2c 挡板所在 `body3D` 当前是激活的（原版 `ToggleBody3D(true)`）");

            // ---- A3/A4：走生产码那条 3D 射线 ----
            Physics.SyncTransforms();           // 卡的位移是直接写 transform 的 ⇒ 不刷物理世界会读到上一帧
            Vector3 p = board.transform.position;
            var ray = new Ray(p + Vector3.back * 20f, Vector3.forward);
            RaycastHit hit;
            int n;
            bool any = NearestHit(ray, out hit, out n);
            Check(any, "A3a ★ 前提：穿卡中心的那条 3D 射线至少命中一条（掩码 = `TapParticleController.CollisionMask`）");
            Check(any && hit.collider == col,
                  "A3 ★ 最近一条命中 = **本卡那颗 3D 挡板**（⛔ 不是 board limit / 地面）"
                + "｜实得 " + (any && hit.collider != null ? hit.collider.name : "<无>") + " · 命中数 " + n
                + "｜🧨 删掉 `CardView.cs:7942` 的 `EnsureTapBlockCollider(go);` ⇒ 红");
            Check(any && hit.collider != null && hit.collider.GetComponent<TapHitMaterialDefinition>() == null,
                  "A4 ★ 挡板上**没有** `TapHitMaterialDefinition` ⇒ 生产码闸⑬（`TapParticleController.cs:254-256`）"
                + "`return`、**不顺延到下一条** ⇒ 点场上卡不播地面反馈");

            // ---- A5a 🧨灭自证：关掉挡板 ⇒ 上面那条就变了 ----
            if (col != null)
            {
                bool before = col.enabled;
                col.enabled = false;
                Physics.SyncTransforms();
                RaycastHit hit2; int n2;
                bool any2 = NearestHit(ray, out hit2, out n2);
                Check(!(any2 && hit2.collider == col),
                      "A5a 🧨 关掉挡板后最近一条**不再**是它 ⇒ A3 不是空转（"
                    + (any2 && hit2.collider != null ? hit2.collider.name : "<无>") + "）");
                col.enabled = before;
                Physics.SyncTransforms();
            }

            // ---- A5b：手牌档不该有挡板（否则手牌会挡住地面点击）----
            var hand = CardView.Create(root.transform, data, "hand_probe", CardFace.Hand);
            Check(hand != null, "★ 前提：手牌档探针卡建出来了");
            var hcol = hand != null ? hand.TapBlockColliderForTest : null;
            Check(hcol == null || !hcol.gameObject.activeSelf || !hcol.enabled,
                  "A5b 手牌档**没有**活的 3D 挡板（原版 `ToggleBody3D(false)` / 2D 卡在场下是关的）"
                + "｜🧨 让 `BuildBody3D` 无条件跑且不关 ⇒ 红");

            Debug.Log(Tag + " —— 合计：通过 " + _pass + " · 失败 " + _fail);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
