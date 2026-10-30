// B102AUD04_Assert.cs — 执行代理 `B102_AUD04`（2026-10-30 · 第一百零二会话）：**聊天窗九宫格边框没除 `m_PixelsPerUnitMultiplier`**
//
// ============================ 它盯的是什么 ============================
//  `AUD02` 普查的 12 行（2 个数值 · 同一批对象按两棵 dump 树各算一次）：
//   ① `Chat/Enter Text/Background`（= `ChatPanel/Holder/Chat/Enter Text/Background`）
//      sprite `Chat_text_background` 536×115 · `m_Border=(53,43,52,43)` · `m_Type=Sliced` ·
//      **`m_PixelsPerUnitMultiplier = 2.0`** ⇒ 画出来的边宽 = **26.5 / 21.5 / 26 / 21.5**（= 原值 ÷ 2）。
//   ② `Chat/Player Options Panel/Buttons/{Add as a friend, Challenge, Report, Block, Profile}`（×2 棵树 = 10 行）
//      sprite `UI_Button_Mulligan` 410×124 · `m_Border=(333,96,333,96)` · **`m_PixelsPerUnitMultiplier = 3.0`**
//      ⇒ 画出来的角块 = **111 / 32 / 111 / 32**（333÷3、96÷3）。
//  判据（⛔ 不是我们的常量）：uGUI `Image.cs` 的 `multipliedPixelsPerUnit = pixelsPerUnit / pixelsPerUnitMultiplier`
//   —— **乘数在【分母】**（同一句也写在 `Battle/ImageQuad.cs` 的 `borderOutPx` doc 里，那里有 2026-10-19
//   `A1939` 的就地订正：原来写成「13 × 5 = 65」**方向反了**）。
//  被测落点 = `Shell/ChatPanel.cs` 的两处 `Nine(...)` 调用（本批给本窗的 `Nine` 包装器补了
//   `borderOutPx` 形参 —— 公共件 `MenuDraw.Nine` 本来就有这个口，⛔ 没有改公共件）。
//
// ============================ 🔴 为什么要分「量到了什么」和「原版说应该多少」============================
//  · 角块在 `ImageQuad.CreateNineSlice` 里是**烘进 quad 尺寸**的（`ImageQuad.WorldW/WorldH × 108`），
//    不是保留成 `border` 参数 ⇒ 本件读的是**渲染出来的几何**，⛔ 不回读 `borderOutPx` 那个入参（那是自证）。
//  · 🔴 **竖轴有个必须说清的夹取**：uGUI `Image.GetAdjustedBorders` 在 `border[axis].x + border[axis].z > rect.size[axis]`
//    时**按轴**把角块夹到一半（`ImageQuad.CreateNineSlice` 的 `scX/scY` 逐字实现同一条）。
//    五颗按钮的行高 = `OptRowH` **57.6** < `32 + 32 = 64` ⇒ **竖轴被夹成 57.6 ÷ 2 = 28.8**（原版同样夹）。
//    ⇒ 竖轴那条断言写 **28.8**（= 图上真的画出来的），**不写 32**；⛔ 写 32 会永远红，写 28.8 又不具判别力
//      （改回 96 也是 28.8 —— 两边都夹到行高的一半）⇒ **判别力全在【横轴】：111 vs 178.65**。
//    ⚠️ `Enter Text/Background` 那一处 **不夹**（26.5+26 = 52.5 ≪ 宽 1200 · 21.5+21.5 = 43 ≪ 高 66.53）
//      ⇒ 那一条四个量**全部有判别力**。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · 把两处 `Nine(...)` 的 `borderOutPx:` 实参删掉（= 退回「拿 `m_Border` 原值当边宽」）⇒
//      A1（Enter Text 的 26.5/21.5/26/21.5）**四处全红**；A2（按钮横轴 111）红，实测会变成 **178.65**
//      （= 333 ÷ (333+333) × 357.3，`GetAdjustedBorders` 的夹取结果）；A3（不等于旧值）也红。
//  · 把 `borderOutPx` 误当成「改 `border`」（= 连 UV 切分一起除）⇒ A4 **红**（UV 仍是 `m_Border` 原值
//      `53/536`、`43/115`、`333/410`；除错了会变成它们的一半）。正向 A1/A2 那时**仍绿** ⇒ 这一对
//      **结构上不可能同时满足**，正是「只改画出来的角块、别改切的纹素」那条口径的锁。
//  · 把 `MenuDraw.Nine` 的 `borderOutPx` 支接成「`borderOutPx × 某数`」⇒ A1/A2 红（不对就是不对）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/ShellScene.cs` 的 `ShellScene.Run`（与 `B102MISSAssert` 同一个 `_sink`）。
//  在**合计行之前**、**同一行追加**（⇒ 行数零变化，照既有代接线的做法）：
//      B102AUD04_Assert.Run(); _sink.Pass += B102AUD04_Assert.Pass; _sink.Fail += B102AUD04_Assert.Fail;
//  🆕 本件**不需要宿主已开着聊天窗** —— 自己建探针根 + 直接调本窗的生产建树口 `ChatPanel.Build()`，
//     收尾 `DestroyImmediate`。因此**任何宿主**（ShellScene / BattleScene）都能跑；
//     也可以单独跑：`-executeMethod B102AUD04_Assert.Run`。
//  ⚠️ 为什么**不走** `ChatPanel.Create(WindowsManager)`：那条会 `WindowsManager.Get()`（没有现成实例时
//     `EnsureHost` 会顺带认领/建主菜单，见 `WindowsManager.EnsureHost` 末段），在 BattleScene 那种宿主里
//     是**副作用**；而本件要量的九宫格全在 `Build()` 里，`Create` 只加窗口档位与换父。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B102_AUD04`：聊天窗两处九宫格边框补 `borderOutPx`（`m_PixelsPerUnitMultiplier` 2 / 3）。</summary>
public static class B102AUD04_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102AUD04] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Nr(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ============================================================ 常量（判据侧，⛔ 不读实现）
    const float PxPerUnit = 108f;                   // = `LayoutSpace.DesignPxH / DesignHeight`（A0 里先断一次）

    // ① `Enter Text/Background`：`m_Border=(53,43,52,43)` ÷ `ppuMul 2.0`
    const float EntL = 26.5f, EntB = 21.5f, EntR = 26f, EntT = 21.5f;
    const float EntBorderL = 53f, EntBorderB = 43f;
    const float EntTexW = 536f, EntTexH = 115f;
    const float EntW = 1200f, EntH = 66.53f;         // `EnterR` 的宽高（原版运行期矩形）

    // ② 五颗按钮：`m_Border=(333,96,333,96)` ÷ `ppuMul 3.0`；行 357.3 × 57.6
    const float BtnL = 111f, BtnR = 111f;
    const float BtnRowW = 357.3f, BtnRowH = 57.6f;
    const float BtnBorderL = 333f, BtnTexW = 410f;
    /// <summary>竖轴被 `GetAdjustedBorders` 夹到的一半（= 行高 ÷ 2）—— **画出来的**值，不是名义 32。</summary>
    const float BtnCornerH = 28.8f;
    /// <summary>旧写法（不除 ppuMul）在横轴上画出来的数：`333 ÷ (333+333) × 357.3` = 178.65。</summary>
    const float BtnOldW = 178.65f;

    static readonly string[] BtnNode = { "Add as a friend", "Challenge", "Report", "Block", "Profile" };

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B102_AUD04 聊天窗九宫格边框：`m_PixelsPerUnitMultiplier` 2.0 / 3.0 ⇒ 边宽要再除一次");
        ChatPanel chat = null;
        try
        {
            var go = new GameObject("B102AUD04_ChatProbe", typeof(RectTransform));
            chat = go.AddComponent<ChatPanel>();
            chat.Build();                                  // 生产建树口（⛔ 不复制一份建法）
            SectionA0_Premise(chat);
            SectionA1_EnterText(chat);
            SectionA2_Buttons(chat);
        }
        catch (Exception ex)
        {
            Tr(false, "（不静默）夹具抛了异常：" + ex.GetType().Name + "：" + ex.Message);
        }
        finally
        {
            if (chat != null) UnityEngine.Object.DestroyImmediate(chat.gameObject);
        }
        Debug.Log(_sink.P + "B102AUD04 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具（读「画出来的」几何）
    static Transform Child(Transform p, string path) { return p == null ? null : p.Find(path); }

    static ImageQuad QuadOf(Transform nineRoot, string name)
    {
        if (nineRoot == null) return null;
        var t = nineRoot.Find(name);
        return t != null ? t.GetComponent<ImageQuad>() : null;
    }

    static float Wpx(ImageQuad q) { return q != null ? q.WorldW * PxPerUnit : -1f; }
    static float Hpx(ImageQuad q) { return q != null ? q.WorldH * PxPerUnit : -1f; }

    // ============================================================ A0 前提（硬断言，⛔ 不写成 `if (x != null) Check(...)`）
    static void SectionA0_Premise(ChatPanel chat)
    {
        MenuCheck.Section(_sink, "A0 前提：探针窗建出来了 · 两处九宫格的根与子块都在");

        Tr(chat != null, "A0-0 **前提**：`ChatPanel` 组件建出来了（`AddComponent` + `Build()`）");
        if (chat == null) return;

        Nr(ImageQuad.PixelsPerUnit, PxPerUnit, 1e-3f,
           "A0-0b **前提**：量纲 `ImageQuad.PixelsPerUnit == 108`（下面所有 `WorldW/H × 108` 的换算前提；"
         + "量到别的数 ⇒ 本件全部读数作废，⛔ 不是绿）");

        var bg = Child(chat.transform, "Holder/Chat/Enter Text/Background");
        Tr(bg != null, "A0-1 **前提**：`Holder/Chat/Enter Text/Background` 在（① 那棵九宫格的根）");
        var bgQs = bg != null ? bg.GetComponentsInChildren<ImageQuad>(true) : new ImageQuad[0];
        C(bgQs.Length, 9,
          "A0-1b **前提**：它 **9 块**子块全建出来了（不夹取：横 52.5 ≪ 1200、竖 43 ≪ 66.53 ⇒ 三行三列都有正尺寸）");

        for (int i = 0; i < BtnNode.Length; i++)
        {
            var root = Child(chat.transform, "Holder/Chat/Player Options Panel/Buttons/" + BtnNode[i] + "/Image");
            Tr(root != null, $"A0-2.{i + 1} **前提**：`Player Options Panel/Buttons/{BtnNode[i]}/Image` 在（五颗钮之一）");
            if (root == null) continue;
            var qs = root.GetComponentsInChildren<ImageQuad>(true);
            // 🔴 6 块而不是 9 块：竖轴被夹成 28.8+28.8 = 57.6 = 行高 ⇒ **中段高 0** ⇒
            //    `CreateNineSlice` 的 `if (w <= 0f || h <= 0f) continue;` 跳过它（uGUI `GenerateSlicedSprite`
            //    对非正尺寸那格同样 `continue`）⇒ **原版也只有 6 块**。⛔ 别按 9 断。
            C(qs.Length, 6,
              $"A0-2b.{i + 1} **前提**：`{BtnNode[i]}` 的九宫格 = **6 块**（中段高 0 被跳过；32+32 > 行高 57.6 ⇒ 竖轴夹取）");
        }
    }

    // ============================================================ A1 ① Enter Text/Background（不夹取 ⇒ 四个量全有判别力）
    static void SectionA1_EnterText(ChatPanel chat)
    {
        MenuCheck.Section(_sink, "A1 ① `Enter Text/Background`：26.5 / 21.5 / 26 / 21.5（= `m_Border` 53,43,52,43 ÷ ppuMul 2.0）");

        var bg = Child(chat.transform, "Holder/Chat/Enter Text/Background");
        if (bg == null) { Tr(false, "A1 **前提不成立**：九宫格根不在（本节没跑，⛔ 不是绿）"); return; }

        var q00 = QuadOf(bg, "Background_00");          // 左下 = 左列 × 下行
        var q20 = QuadOf(bg, "Background_20");          // 右下 = 右列 × 下行
        var q02 = QuadOf(bg, "Background_02");          // 左上 = 左列 × 上行
        var q22 = QuadOf(bg, "Background_22");          // 右上 = 右列 × 上行
        var q10 = QuadOf(bg, "Background_10");          // 中列 × 下行（宽应 = 1200 − 26.5 − 26）

        Nr(Wpx(q00), EntL, 0.05f, "A1-1 **左边宽 = 26.5**（`m_Border.x 53 ÷ ppuMul 2`）"
                                + "｜🧨 不传 `borderOutPx` ⇒ 53 ⇒ 红（本件唯一的判别量之一）");
        Nr(Hpx(q00), EntB, 0.05f, "A1-2 **下边高 = 21.5**（`m_Border.y 43 ÷ 2`）"
                                + "｜🧨 不传 ⇒ 43 ⇒ 红");
        Nr(Wpx(q20), EntR, 0.05f, "A1-3 **右边宽 = 26**（`m_Border.z 52 ÷ 2`）｜🧨 不传 ⇒ 52 ⇒ 红");
        Nr(Hpx(q22), EntT, 0.05f, "A1-4 **上边高 = 21.5**（`m_Border.w 43 ÷ 2`）｜🧨 不传 ⇒ 43 ⇒ 红");
        Nr(Wpx(q10), EntW - EntL - EntR, 0.05f,
           $"A1-5 中列宽 = **{EntW - EntL - EntR}**（= 1200 − 26.5 − 26；两侧角块真的只吃掉 52.5）"
         + "｜🧨 把两边角块都改成 0（例如传 0）⇒ 中列吃掉整行 ⇒ 红");
        var q11 = QuadOf(bg, "Background_11");          // 中列 × 中行
        Nr(Hpx(q11), EntH - EntB - EntT, 0.05f,
           $"A1-5b 中行高 = **{EntH - EntB - EntT}**（= 66.53 − 21.5 − 21.5；这一处**不夹取**）"
         + "｜🧨 上下角块写大（例如 43）⇒ 中行被吃到别的数 ⇒ 红");

        // A4 型锁：UV 切分必须**仍按 `m_Border` 原值**（= 只有「画多大」变了，切的纹素没变）
        Tr(q00 != null && Mathf.Abs(q00.UvRect.width - EntBorderL / EntTexW) < 1e-4f,
           "A1-6 **UV 未被动过**：左块 uv 宽 = 53/536（≈0.0989）—— 除 `ppuMul` 只改**画出来的角块**，"
         + "⛔ 不改 UV 切分｜🧨 把 `border` 形参也一起除（改成 26.5）⇒ 本条红（那就是改了切的纹素位置）");
        Tr(q00 != null && Mathf.Abs(q00.UvRect.height - EntBorderB / EntTexH) < 1e-4f,
           "A1-7 **UV 未被动过**：下块 uv 高 = 43/115（≈0.3739）｜🧨 同上 ⇒ 红");

        // A3 型锁：不等于旧写法画出来的那两个数（结构上不可能与 A1-1/A1-3 同时为真）
        Tr(q00 != null && Mathf.Abs(Wpx(q00) - EntBorderL) > 0.05f,
           "A1-8 **灭自证**：左边宽 ≠ 53（旧写法「直接拿 `m_Border` 当边宽」的那个数）"
         + "｜🧨 删掉 `borderOutPx:` 实参 ⇒ 本条与 A1-1 同时红");
        Tr(q20 != null && Mathf.Abs(Wpx(q20) - 52f) > 0.05f,
           "A1-9 **灭自证**：右边宽 ≠ 52｜🧨 同上");
    }

    // ============================================================ A2 ② 五颗按钮（横轴有判别力，竖轴被夹取）
    static void SectionA2_Buttons(ChatPanel chat)
    {
        MenuCheck.Section(_sink, "A2 ② 五颗按钮：横轴角块 111（= 333 ÷ ppuMul 3）· 竖轴被夹取成 28.8（原版同样夹）");

        for (int i = 0; i < BtnNode.Length; i++)
        {
            var root = Child(chat.transform, "Holder/Chat/Player Options Panel/Buttons/" + BtnNode[i] + "/Image");
            if (root == null) { Tr(false, $"A2-{i + 1} **前提不成立**：`{BtnNode[i]}` 的九宫格根不在（这一颗没跑，⛔ 不是绿）"); continue; }

            var q00 = QuadOf(root, "Image_00");         // 左下
            var q20 = QuadOf(root, "Image_20");         // 右下
            var q02 = QuadOf(root, "Image_02");         // 左上

            Nr(Wpx(q00), BtnL, 0.05f,
               $"A2-{i + 1}a `{BtnNode[i]}` **左边宽 = 111**（333 ÷ 3）"
             + "｜🧨 不传 `borderOutPx` ⇒ 333 被 `GetAdjustedBorders` 夹成 178.65 ⇒ 红（**本件对五颗钮唯一的判别量**）");
            Nr(Wpx(q20), BtnR, 0.05f, $"A2-{i + 1}b `{BtnNode[i]}` **右边宽 = 111**｜🧨 不传 ⇒ 178.65 ⇒ 红");
            Nr(Hpx(q00), BtnCornerH, 0.05f,
               $"A2-{i + 1}c `{BtnNode[i]}` **下块高 = 28.8**（名义 32，但 32+32 > 行高 57.6 ⇒ 被按轴夹到 57.6÷2；"
             + "原版 uGUI 同样夹；⚠️ 这个量**不具判别力**：改回 96 也是 28.8）");
            Nr(Hpx(q02), BtnCornerH, 0.05f, $"A2-{i + 1}d `{BtnNode[i]}` **上块高 = 28.8**（同上）");

            var q10 = QuadOf(root, "Image_10");          // 中列 × 下行（中行因为高 0 没建）
            Nr(Wpx(q10), BtnRowW - BtnL - BtnR, 0.05f,
               $"A2-{i + 1}g `{BtnNode[i]}` **中列宽 = {BtnRowW - BtnL - BtnR}**（= 357.3 − 111 − 111，横轴没夹取）"
             + "｜🧨 横轴角块写大（例如 333）⇒ 中列被吃到 0.6 ⇒ 红；⛔ 拿 135.3 当期望 = 从被测实现反算，本条是**字面量**算出来的");
            Tr(QuadOf(root, "Image_01") == null,
               $"A2-{i + 1}h `{BtnNode[i]}` **中行不存在**（`Image_01` / `Image_11` / `Image_21` 都没建）"
             + " —— 32+32 > 行高 57.6 ⇒ 竖轴夹取后中段高 0 ⇒ `CreateNineSlice` 跳过（uGUI 同样 `continue`）"
             + "｜🧨 把 `borderOutPx` 的竖轴写得小到 32+32 ≤ 57.6（例如 20/20）⇒ 中行会长出来 ⇒ 红");

            Tr(q00 != null && Mathf.Abs(Wpx(q00) - BtnOldW) > 0.5f,
               $"A2-{i + 1}e **灭自证**：横轴角块 ≠ 178.65（旧写法不除 ppuMul 时被夹出来的那个数）"
             + "｜🧨 删掉 `borderOutPx:` ⇒ 本条与 A2-a/b 同时红");

            Tr(q00 != null && Mathf.Abs(q00.UvRect.width - BtnBorderL / BtnTexW) < 1e-4f,
               $"A2-{i + 1}f **UV 未被动过**：左块 uv 宽 = 333/410（≈0.8122）—— 切的纹素没变、只改了画多大"
             + "｜🧨 把 `border` 形参一起除 ⇒ 红");
        }
    }
}
