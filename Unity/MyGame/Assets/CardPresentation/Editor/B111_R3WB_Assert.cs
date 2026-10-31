// B111_R3WB_Assert.cs — 第 111 会话第 1 波执行代理 `B111_R3W`（2026-11-01）· 账 N138 的 ① 断言
//
// 判据：`判据/B106_奖励预览链_施工单.md §三 A2` —— 原版 Icon 抽屉的主图 = `锚(0,0)-(1,1) + sizeDelta 0 + PA=1`（铺满）。
//   本次现读 `d:/2/tools/assetripper_out/ExportedProject/Assets/` 的 Icon 抽屉 prefab（Title / Currency / Random Card）。
//
// 本件只验 **①**（`ItemDrawer` 的新几何档 + `DrawIconFill`）；**②**（两扇日奖励窗换 `DrawIconFill`）的视觉/结构由调度台的
// `Editor/RewardsScene.cs`（③）核 —— 理由：那批 `ArtOf`/`MissingArt` 断言在本代理黑名单里。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//   · A3：把 `ItemDrawer.IconRect()` 改回「永远 `Square(box, IconFill)`」⇒ `IconFillBox=true` 时 `IconRectForTest` 返回
//     居中方形（30,80,170,220）而不是整个 `box`（0,0,200,300）⇒ A3 红。
//   · A2：把默认 `IconFillBox` 改成 true ⇒ A2（默认档仍是居中方形）红。
//   · A4：把 `DrawIconFill` 里的 `Kind = Generic` 改掉 / 换 `DrawerOverride.Icon` ⇒ 抽屉名不再是 `IconDrawer` ⇒ A4 红。
//
// ============================ 怎么跑 / 怎么接（⚠️ 本代理不动宿主）============================
//   单独跑：unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//             -projectPath "D:\4\Unity\MyGame" -executeMethod B111_R3WB_Assert.Run -logFile -
//   接宿主（任意 `Editor/*Scene.cs`，本件自建自拆、不依赖宿主状态）：
//     同一物理行追加：B111_R3WB_Assert.Run(); _sink.Pass += B111_R3WB_Assert.Pass; _sink.Fail += B111_R3WB_Assert.Fail;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B111_R3W`：N138 ① —— `ItemDrawer` 的 Icon 铺满档（`IconFillBox` / `IconRect` / `DrawIconFill`）。</summary>
public static class B111_R3WB_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111R3WB] ") { Near = MenuNearStyle.Compact3 };
    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static bool Near(float a, float b) { return Mathf.Abs(a - b) < 0.01f; }

    // 非方框：宽 200 / 高 300。`Square(box, 0.7)` = min(200,300)*0.7 = 140 居中 ⇒ (30,80,170,220)。
    static readonly PxRect Box = new PxRect(0f, 0f, 200f, 300f);

    public static void Run()
    {
        MenuCheck.Section(_sink, "B111_R3W · N138 ① `ItemDrawer` Icon 铺满档");

        // A0 前提：无图 ⇒ 不画（两窗靠 `Art()` 记 MissingArt / 出声，⛔ 别静默画占位板）
        GameObject go = null;
        try
        {
            go = new GameObject("B111R3WB");
            Tr(go != null && go.transform != null, "A0 前提：建得到一个父节点（下面几档都在它下面建抽屉）");
            if (go == null) return;

            // A1 默认档：`IconFillBox` 默认 false（既有 100+ 调用点逐字节不变）
            var def = ItemDrawerStyle.Default(0, 1, 2);
            C(def.IconFillBox, false, "A1 ★ 默认 `IconFillBox == false`（`ItemDrawerStyle.Default` 未置位 ⇒ 既有调用点行为不变）");

            // A2 既有几何不变：默认档仍画居中方形 140×140 @ (30,80)-(170,220)
            def.IconFill = 0.7f; def.IconFillBox = false;
            var r2 = ItemDrawer.IconRectForTest(Box, def);
            Tr(Near(r2.x1, 30f) && Near(r2.y1, 80f) && Near(r2.x2, 170f) && Near(r2.y2, 220f),
               "A2 ★ `IconFillBox=false` ⇒ 居中方形 (30,80,170,220)（= min(200,300)×0.7 居中；期望值是**独立算的常量**，不是拿 `Square` 现算）"
               + "｜🧨 默认 `IconFillBox` 改成 true ⇒ 本行红");

            // A3 铺满档：`IconFillBox=true` ⇒ 整个 box（0,0,200,300）
            var fill = ItemDrawerStyle.Default(0, 1, 2);
            fill.IconFill = 1f; fill.IconFillBox = true;
            var r3 = ItemDrawer.IconRectForTest(Box, fill);
            Tr(Near(r3.x1, 0f) && Near(r3.y1, 0f) && Near(r3.x2, 200f) && Near(r3.y2, 300f),
               "A3 ★★ `IconFillBox=true` ⇒ 铺满整个 box (0,0,200,300)（= 原版 Icon 变体锚(0,0)-(1,1)+sizeDelta 0）"
               + "｜🧨 `IconRect` 改回永远 `Square(box, IconFill)` ⇒ 返回 (30,80,170,220) ⇒ 本行红");

            // A4 `DrawIconFill` 建出的抽屉 = `Icon` 档（不是占位板派发 / 不是别的档）
            var view = ItemDrawer.DrawIconFill(go.transform, Box, "40K_Profile_icon_title", "B111R3WB", 10);
            Tr(view != null && view.Node != null, "A4 前提：`DrawIconFill`（有图名）建得出抽屉根");
            if (view != null)
                C(view.Drawer, ItemDrawer.DrawerIcon,
                  "A4 ★ `DrawIconFill` 选中的抽屉 = `IconDrawer`（`ItemKind.Generic` ⇒ `PickDrawer` 的 `DrawerIcon` 支）"
                  + "｜🧨 把 `Kind` 改掉 / 换 `DrawerOverride.Icon` ⇒ 抽屉名变 ⇒ 本行红");

            // B1 无图 ⇒ 不画（返回 null，让调用方照旧记 MissingArt / 出声）
            C(ItemDrawer.DrawIconFill(go.transform, Box, null, "x", 10), (ItemDrawerView)null,
              "B1 ★ `DrawIconFill(null art)` ⇒ null（⛔ 不静默画占位板）");
        }
        finally
        {
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
