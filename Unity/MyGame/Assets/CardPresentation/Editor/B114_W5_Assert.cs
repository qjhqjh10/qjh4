// B114_W5_Assert.cs — 写手 `W-5`（2026-11-01 · 第一百一十四会话）：`B-239` **赛季结束窗的段位大图**。
//
// 被测实现 = `Shell/RankedSeasonEndedWindow.cs` **类尾**的 `BuildDivisionImage()` / `DivisionArtOf()`
//（`Build()` 里那句 `_division = BuildDivisionImage();`），以及 `Shell/RankedDivisionInfo.cs` 的
// `DivisionArtName()`（本轮按原版改成**夹取**，见 A6）。
//
// 判据（全部本地现读）：
//   · 原版行为 `decomp_full/RankedEventSeasonEndedWindow__Open.c:40-52`：`RankedDivisions.GetDivisionData(n)`
//     → `AssetReferenceTyped.Load()` → `Image.set_sprite(...)`（窗里的 `+0x88` 那颗 = `Division Image`，`.c:72`）。
//     🔴 那一段**整块**挂在「服务端统计 / `GetDivisionData` 非 null」的判据里 ⇒ **原版没有段位号时不替玩家编一个段位**。
//   · 越界语义（A6 的判据）`decomp_full/RankedDivisions__GetDivisionData.c:26-33`：
//     `Math.Max(n,1)` → `FirstOrDefault(divisionNumber == n)` → **查不到退回 `Last()`** ⇒ 越界号**永远有图**。
//   · prefab → `d:/2/tools/assetripper_out/ExportedProject/Assets/Ranked Season Ended Window.prefab`
//     （`Division Image` 的 `RectTransform &224470112387431263` / `Image &114621510681728303`）：
//     `m_SizeDelta = 754.958²` · `m_Type = 0`(Simple) · **`m_PreserveAspect = 0`**（铺满，不是等比内接）·
//     `m_Color` 白 α1 · 组件只有 `RectTransform/CanvasRenderer/Image`（**没有** `AnimateUIMaterialProperty`）。
//   · 段位号 → 图名表 = `Shell/RankedDivisionInfo.cs` 的 `DivisionArt`（7 条；`RankedDivisionsSO.asset` 的
//     `divisions[].image.m_AssetGUID` 与 7 张 Sprite 的 `m_RenderDataKey` 逐条相等）。
//
// ⚠️ **期望值一律独立算**（prefab 的 `754.958` / 表里那串字面量图名），
//    ⛔ **不调被测实现自己的 `DivisionArtOf()` 反算** —— 那是自证（同 `B106_W8_Assert.cs` 文件头那条纪律）。
//
// 🧨 **灭自证（改坏法逐条写在文案里）**
//   · `Build()` 退回 `_division = MenuDraw.Node(transform, "Division Image", DivisionR)` ⇒ **A3 红**
//     （节点上没有 `ImageQuad`）。这是 2026-11-01 之前的真实状态。
//   · 拿 `CardArt.Solid()` 之类占位图顶替 ⇒ **A3c 红**（贴图名不是段位图）。
//   · 图名写死成一张（不管段位号）⇒ **A4 红**（换段位号之后贴图不跟着换、或两张是同一张贴图）。
//   · **给「没拿到段位号」加一个演示值兜底**（例如 `RankedData.DisplayDivisionNumber` / 7）⇒ **A2 红**：
//     原版没有段位号时**不编段位**（`Open()` 那段整块被跳过），编一个 = 让画面「看起来有数据」。
//   · `DivisionArtName` 越界改回返 null（或改成取模）⇒ **A6 红**（原版是夹到 1 / 落 `Last()`）。
//
// 用法（宿主聚合行一行；`Run()` 自己开窗、跑完自己关，不依赖场上已有窗）：
//
//     B114_W5_Assert.Run(); _sink.Pass += B114_W5_Assert.Pass; _sink.Fail += B114_W5_Assert.Fail;
//
// ⚠️ 量尺寸**不能读 `RectTransform`**：`ImageQuad` 建的是**裸 `Transform`**（`ImageQuad.Create` 里
//    `new GameObject(name)`），没有 `RectTransform` ⇒ `rect.width` 取不到。口径 = `WorldH × 108`
//    （108 = `LayoutSpace.DesignPxH / DesignHeight`，与 `Wpx()` 同一套换算）。
using CardPresentation;
using UnityEngine;

public static class B114_W5_Assert
{
    const string P = "[B114W5] ";
    const float PxPerUnit = 108f;                       // = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight

    // 判据值（prefab `Division Image` 的 `m_SizeDelta`，⛔ 不取我们自己的 `DivisionR`）
    const float PrefabSize = 754.958f;
    const float Tol = 0.1f;                             // dump 那两位小数（755.00）与 prefab 的 0.042 差

    // 表里的**字面量**图名（判据 = `RankedDivisionInfo` 的 GUID 证据链，⛔ 不从实现里反算）
    const string Art4 = "04-Admiral", Art1 = "01-Rook", Art7 = "07-Legend";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;

        var win = WindowsManager.OpenRankedSeasonEnded();
        if (!Ok(win != null && win.CurrentState == WindowState.Open,
                "A0 前提：`WindowsManager.OpenRankedSeasonEnded()` 真的开出一扇窗（`CurrentState == Open`）"
              + " —— 窗没开 ⇒ 下面每一条都会空转，所以这里直接 return"))
            return;

        try
        {
            // ---- 前提：表与图都在（缺任何一样，后面的「非空」都会变成假绿）----
            if (!Ok(RankedDivisionInfo.DivisionArtCount > 0,
                    "A0b 前提：段位图名表非空（`RankedDivisionInfo.DivisionArtCount` = "
                  + RankedDivisionInfo.DivisionArtCount + "）")) return;
            if (!Ok(CardArt.MenuUi(Art4) != null,
                    "A0c 前提：段位图 `" + Art4 + "` 在 `Resources/Art/ui_menu/` 里取得到"
                  + "（取不到 ⇒ 被测实现会走「出声 + 不画」那一支，A3 就不该判它红）")) return;

            // ---- A1/A2★ **默认态**（`ctx = null`、没设段位号）：节点照建，但**不画**、也不编段位 ----
            var node = win.DivisionNode;
            if (!Ok(node != null, "A1★ `Division Image` 节点建出来了（原版 prefab 有这一颗；⛔ 不是「没数据就整个不显示」）"))
                return;
            Ok(node.name == "Division Image", "A1b 节点名 = **`Division Image`**（prefab `m_Name` 实读；现读 `"
                                            + node.name + "`）");
            Ok(win.DivisionArtBlank && string.IsNullOrEmpty(win.DivisionArtUsed) && node.GetComponent<ImageQuad>() == null,
               "A2★ 没拿到段位号 ⇒ **不画、也不编一个**（`DivisionArtBlank` = " + win.DivisionArtBlank
             + "、`DivisionArtUsed` = `" + (win.DivisionArtUsed ?? "<null>") + "`、节点上 `ImageQuad` = "
             + (node.GetComponent<ImageQuad>() != null) + "）"
             + "｜🧨 给「没拿到段位号」加演示值兜底（如 `RankedData.DisplayDivisionNumber`）⇒ 这里会画出图 ⇒ 红"
             + "（原版 `Open()` 那一段整块挂在「服务端统计非 null」里 ⇒ 没数据时它**不替玩家编段位**）");

            // ---- A3★ 给了段位号 4 ⇒ 真的画出来、尺寸对、图对 ----
            win.DivisionNumber = 4; win.Show();
            var quad = win.DivisionNode != null ? win.DivisionNode.GetComponent<ImageQuad>() : null;
            if (!Ok(quad != null,
                    "A3★ 段位号 **4** ⇒ `Division Image` 上有 **`ImageQuad`**（原版那颗 `Image` 的载体）"
                  + "｜🧨 退回 `MenuDraw.Node(...)` 就只剩空节点 ⇒ 这里 null ⇒ 红（2026-11-01 之前就是这个状态）"))
                return;

            float wPx = quad.WorldW * PxPerUnit, hPx = quad.WorldH * PxPerUnit;
            Ok(Mathf.Abs(wPx - PrefabSize) <= Tol && Mathf.Abs(hPx - PrefabSize) <= Tol,
               "A3b★ 尺寸 = prefab `m_SizeDelta` **754.958²**（现读 " + wPx.ToString("F3") + "×" + hPx.ToString("F3")
             + "；容差 ±" + Tol + "）｜🧨 换成 `RankedDivisionInfo` 那颗 PA=1 的等比内接（539.79²）⇒ 红");

            var tex = quad.Texture;
            Ok(tex != null, "A3c★ `Division Image` 的 sprite **非空**（`Image.m_Sprite = 0` 是空白态，不是目标态）");
            Ok(tex != null && tex.name == Art4 && win.DivisionArtUsed == Art4,
               "A3d★ 贴图 = **`" + Art4 + "`**（= `RankedDivisionInfo.DivisionArtName(4)`，表是 7 条字节证据；现读 "
             + (tex != null ? "`" + tex.name + "`" : "**null**") + " / `DivisionArtUsed` = `"
             + (win.DivisionArtUsed ?? "<null>") + "`）"
             + "｜🧨 拿 `CardArt.Solid()` 之类的占位图顶替 ⇒ 贴图名不是段位图 ⇒ 红");
            Ok(quad.Tint.a > 0.5f, "A3e 画得出来（`Tint.a` = " + quad.Tint.a.ToString("F3")
                                 + "；α=0 等于「建了但看不见」）");

            // ---- A4🧨 换段位号 ⇒ 贴图必须跟着换（钉住「号→图」是查表直读，不是写死一张）----
            win.DivisionNumber = 1; win.Show();
            var q1 = win.DivisionNode != null ? win.DivisionNode.GetComponent<ImageQuad>() : null;
            Ok(q1 != null && q1.Texture != null && q1.Texture.name == Art1,
               "A4🧨 段位号 **1** ⇒ 贴图 **`" + Art1 + "`**（现读 "
             + (q1 != null && q1.Texture != null ? "`" + q1.Texture.name + "`" : "**null**") + "）");

            win.DivisionNumber = 7; win.Show();
            var q7 = win.DivisionNode != null ? win.DivisionNode.GetComponent<ImageQuad>() : null;
            Ok(q7 != null && q7.Texture != null && q7.Texture.name == Art7,
               "A4b🧨 段位号 **7** ⇒ 贴图 **`" + Art7 + "`**（现读 "
             + (q7 != null && q7.Texture != null ? "`" + q7.Texture.name + "`" : "**null**") + "）");
            Ok(q1 != null && q7 != null && q1.Texture != null && q7.Texture != null && q1.Texture != q7.Texture,
               "A4c🧨 段位 1 与 7 画的是**两张不同的贴图**（⛔ 不是同一张图换个用法）"
             + "｜🧨 实现里把图名写死成一张 ⇒ 1 与 7 相同 ⇒ 红");

            // ---- A5 父子关系：`Rank Icon` 仍挂在 `Division Image` 底下（prefab `m_Children` 实读）----
            Ok(win.RankIconNode != null && win.RankIconNode.parent == win.DivisionNode,
               "A5 `Division Image/Rank Icon` 的父子关系没被这次改动破坏"
             + "（prefab `Division Image` 的 `m_Children` = 只有 `Rank Icon` 一颗）"
             + "｜🧨 把载体建成 `Rank Icon` 的**兄弟** ⇒ 这条红");

            // ---- A6★ `DivisionArtName` 的越界语义 = 原版夹取（`Math.Max(n,1)` + 查不到退回 `Last()`）----
            // 期望值 = 表里那串**字面量**图名（⛔ 不用实现反算「应该有图」这种同义反复）。
            string lo0 = RankedDivisionInfo.DivisionArtName(0);
            string hi99 = RankedDivisionInfo.DivisionArtName(99);
            string neg = RankedDivisionInfo.DivisionArtName(-5);
            Ok(lo0 == Art1 && neg == Art1,
               "A6★ 越界（`0` 与 `-5`）⇒ **夹到第 1 档**（现读 `" + (lo0 ?? "<null>") + "` / `" + (neg ?? "<null>")
             + "`，期望 **`" + Art1 + "`**）—— 原版 `Math.Max(n,1)`（`RankedDivisions__GetDivisionData.c:26`）"
             + "｜🧨 改回「越界返 null」⇒ 红");
            Ok(hi99 == Art7,
               "A6b★ 越界（`99`）⇒ **落到最后一条**（现读 `" + (hi99 ?? "<null>") + "`，期望 **`" + Art7
             + "`**）—— 原版 `FirstOrDefault` 查不到退回 `Last()`（`.c:31-33`）"
             + "｜🧨 改回「越界返 null」或改成取模 ⇒ 红");
        }
        finally
        {
            if (win != null) win.Close();
        }
    }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }
}
