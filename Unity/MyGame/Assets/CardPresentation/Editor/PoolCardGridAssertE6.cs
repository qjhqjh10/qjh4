// PoolCardGridAssertE6.cs — 执行代理 `E6` 的断言宿主（2026-10-24 · 界面线 · 收藏页卡池格子）
//
// 覆盖：**收藏 → 卡牌页（Cards 页）卡池小卡的格子几何**
//   ① 格 / 间距 / 列数 / 居中偏移 = **原版 prefab 的 RSR 序列化字段 + 反编译出来的算法**
//   ② 张数条（`/Counter`）的锚点几何
//   ③ **卡块**（`CardView` 渲染出来的那一块）与格、与张数条的关系 —— 真读活对象的缩放/位置
//
// 判据出处（**全部是一手**，逐条见下面各段注释）：
//   · `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_{345129740864909092,
//      _7174171443954770645,_-1327870198181403947}.json` —— `PrototypeCell` = `Collection Card` 的那 3 份 RSR，
//     `_useFixedCellSize=1` · `_cellWidth=262.5` · `_cellHeight=384` · `_spacingX=_spacingY=0` · `_mobileSizeScale=1.5`
//   · `d:/2/tools/decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__ConfigureColumnNumber.c` —— 列数算法
//   · `d:/2/tools/decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c` —— 居中偏移/格位算法
//   · `d:/2/新解包资源/.../bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_40971030049256792.json`
//     （`CollectionCard`：`initialCardScale=150` / `initialHeight=512`）+ 反汇编
//     `CollectionCard__OnRectTransformDimensionsChange` @ VA `0x1806D9980`（`localScale = sizeDelta.y × 150/512`）
//   · 原版**实拍**（用户 `C:/Users/qjh36/Desktop/warpforge参考图/收藏—张牌的界面参考图（打开过滤器按钮后）.png`）：
//     卡宽 ≈ 格的 **81.7%** · 卡高 ≈ **82.8%** · 行距/列距 = **1.465**（= 384/262.5 = 1.4629 ⇒ 反证**不是** 225×364.5）
//
// ⛔ **别把期望值改成读我们自己的常量再和自己比**（自证）：下面的 `Orig*` 全是**原版字面量**，
//    改实现不改本文件 ⇒ 必红；`A2` §Q6 那套「225×364.5 / spacing 20 / 卡占格 61.3%」**是错的**
//    （那两份 MB 的 `PrototypeCell` 是 `Collection Deck[ With Highlight]` = 卡组列表的格，不是卡池）。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   · 随宿主跑：`CollectionScene.Run` 里已接一行 `PoolCardGridAssertE6.Run(win, _sink)`（计数进宿主合计）
//   · 单独跑（**只跑纯几何那一段**，不建窗、不碰存档）：
//       unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//         -projectPath "D:\4\Unity\MyGame" -executeMethod PoolCardGridAssertE6.Run -logFile -
//   判绿红看合计行 `[E6] === 合计`；逐条按**行首** `✓` / `✗` 数。
using UnityEngine;
using CardPresentation;

public static class PoolCardGridAssertE6
{
    // ==================================================================
    //  原版字面量（⛔ 不是抄 `CollectionWindow` 的常量 —— 出处见文件头）
    // ==================================================================
    const float OrigCellW = 262.5f;          // RSR `_cellWidth`
    const float OrigCellH = 384f;            // RSR `_cellHeight`
    const float OrigSpacingX = 0f;           // RSR `_spacingX`
    const float OrigSpacingY = 0f;           // RSR `_spacingY`
    const int OrigCols = 6;                  // floor(1589.7 ÷ (262.5 + 0))
    const float OrigPadX = 7.35f;            // ((1589.7 − (0 + 262.5) × 6) + 0) × 0.5
    const float OrigViewportW = 1589.7f;     // `Scroll View` 视口宽（1919.9 − 330.2）

    // `/Counter` 的锚点（`/Content/Counter`，prefab 逐字段：anchorMin=(0.2857143, 0.002) / anchorMax=(0.7142857, 0.09966714)）
    const float OrigCntMinX = 0.2857143f, OrigCntMaxX = 0.7142857f;
    const float OrigCntMinY = 0.002f, OrigCntMaxY = 0.09966714f;
    const float OrigCounterH = OrigCntMaxY * OrigCellH;          // 38.272
    const float OrigCardH = OrigCellH - OrigCounterH;            // 345.728

    // 卡块**宽**占比：原版**实拍**量出来的那一档（用户截图，见文件头）。带 ±3% 的带宽
    //   —— 带宽要**同时**排掉两种错读：`A2` 的 0.613（太小）与「150/512 直译」的 0.897（太大、压住张数条）。
    //   ⚠️ **高**占比不拿实拍的 82.8% 来断：原版**看得见**的那圈是 `CardFrame`（2.2452×3.2572，比例 **0.689**），
    //   而我们 `CardView` 的 quad 铺的是**卡本体** 2.0927×3.3313（比例 **0.628** ⇒ 更瘦高，见 `CardView.Width/Height`），
    //   所以同一个「按宽对齐」的缩放，我们的可见高度必然比原版高约 9%（90% vs 82.8%）。
    //   那一层差在 `CardView`（⛔ 本代理不许碰）⇒ 高度这里只钉**结构**（卡块正好填满格高 − 张数条、不越界）。
    const float ShotCardWRatio = 0.817f;
    const float RatioTol = 0.03f;

    /// <summary>随宿主跑（`CollectionScene.Run` 里接的那一行）：断言失败记进**宿主自己的** `CheckSink`。</summary>
    public static void Run(CollectionWindow win, CheckSink s)
    {
        MenuCheck.Section(s, "E6 · 卡池小卡格子几何（原版 RSR 字段 + 反编译算法）");

        // ---------------------------------------------------------------- ① 格 / 间距 / 列数
        MenuCheck.Near(s, CollectionWindow.CardsCellW, OrigCellW, 0.01f,
                       "格宽 = **262.5**（RSR `_cellWidth`，`PrototypeCell` = `Collection Card` 的那 3 份逐字相同）");
        MenuCheck.Near(s, CollectionWindow.CardsCellH, OrigCellH, 0.01f,
                       "格高 = **384**（RSR `_cellHeight`）");
        MenuCheck.True(s, CollectionWindow.CardsCols == OrigCols,
                       $"列数 = **{OrigCols}** = floor({OrigViewportW} ÷ ({OrigCellW} + {OrigSpacingX}))"
                       + "（`ConfigureColumnNumber.c`；⚠️ 序列化的 `_segments` 在 `_useFixedCellSize=1` 时不参与）");
        MenuCheck.Near(s, CollectionWindow.CardsPadX, OrigPadX, 0.01f,
                       "居中偏移 = **7.35** = ((视口 1589.7 − (0 + 262.5) × 6) + 0) × 0.5（`CreateCellPool.c`）");

        // 间距：相邻两格的边距 = spacing（原版 0）—— 用格矩形**现算**，改 `CardsCellRect` 的间距必红
        var c0 = CollectionWindow.CardsCellRect(0);
        var c1 = CollectionWindow.CardsCellRect(1);
        var cNextRow = CollectionWindow.CardsCellRect(OrigCols);
        MenuCheck.Near(s, c1.x1 - c0.x2, OrigSpacingX, 0.01f,
                       "列间距 = **0**（`_spacingX`）—— 第 0/1 格之间不留缝");
        MenuCheck.Near(s, cNextRow.y1 - c0.y2, OrigSpacingY, 0.01f,
                       "行间距 = **0**（`_spacingY`）—— 第 0 格 / 第 7 格（下一行首）之间不留缝");
        MenuCheck.Near(s, c1.x1 - c0.x1, OrigCellW, 0.01f, "列距（pitch）= 格宽 = **262.5**");
        MenuCheck.Near(s, cNextRow.y1 - c0.y1, OrigCellH, 0.01f, "行距（pitch）= 格高 = **384**");
        // 整行必须在视口里、左右留白相等（贴左但整体居中）
        MenuCheck.Near(s, CollectionWindow.CardsViewport.W - OrigCols * OrigCellW,
                       2f * OrigPadX, 0.01f,
                       "6 列内容 1575 + 两侧各 7.35 = 视口 1589.7（**整体居中**，不是贴边）");
        MenuCheck.True(s, c0.x1 >= CollectionWindow.CardsViewport.x1 - 0.01f
                          && CollectionWindow.CardsCellRect(OrigCols - 1).x2 <= CollectionWindow.CardsViewport.x2 + 0.01f,
                       "第 0 格左沿 / 第 5 格右沿都落在视口内（⛔ 溢出会压到右侧滚动条那一带）");

        // ---------------------------------------------------------------- ② 张数条 + 卡块
        var cnt = CollectionWindow.CardsCounterRect(c0);
        MenuCheck.Near(s, cnt.W, (OrigCntMaxX - OrigCntMinX) * OrigCellW, 0.01f,
                       "张数条宽 = **112.5** = (0.7142857 − 0.2857143) × 262.5（原版 `/Counter` 锚点，不是我们挑的）");
        // 🩹 **2026-10-24（自检首跑订正）**：这里原写 `OrigCounterH`（38.272 = `maxY × 格高` = **距格底的上沿位置**），
        //   而 `CardsCounterRect` 返回的是**条自身高度** = `(maxY − minY) × 格高` = **37.504** ⇒ 两者不是一回事。
        MenuCheck.Near(s, cnt.H, (OrigCntMaxY - OrigCntMinY) * OrigCellH, 0.01f,
                       "张数条高 = **37.504** = (0.09966714 − 0.002) × 384（原版 `/Counter` 的上下锚点之差）");
        MenuCheck.Near(s, c0.y2 - cnt.y2, OrigCntMinY * OrigCellH, 0.02f,
                       "张数条**贴格底**：条底距格底 = 0.002 × 384 = **0.768**（原版 `anchorMin.y`）");
        var cardR = CollectionWindow.CardsCardRect(c0);
        MenuCheck.Near(s, cardR.H, OrigCardH, 0.01f, "卡块高 = 格高 − 张数条 = **345.728**");
        // 🔴 **灭自证的结构式**：一个矩形不可能既「高 345.728」又「下沿越过张数条上沿」——
        //   谁把卡改回整格（原版公式直译 = 374.83）就必红，而不是「两边一起改就绿」。
        MenuCheck.True(s, cardR.y2 <= cnt.y1 + 0.01f,
                       "★ 卡块下沿 **≤** 张数条上沿（用户 2026-10-18 报的「张数与卡牌重叠」那条，⛔ 别改回整格）");

        // ---------------------------------------------------------------- ③ 活对象（只在有窗口时）
        if (win == null) return;
        if (win.CardsCells == null || win.CardsCells.Count == 0)
        {
            MenuCheck.True(s, false,
                           "（前提）卡池画出了至少 1 格 —— 否则 ③ 整段无对象（**不静默跳过**）");
            return;
        }

        var cv0 = win.CardsCells[0] != null ? win.CardsCells[0].GetComponent<CardView>() : null;
        if (cv0 == null)
        {
            MenuCheck.True(s, false, "（前提）第 1 格里取得到 `CardView`");
            return;
        }
        float sc = cv0.BaseScale;                       // SetPose 写进去的**基础缩放**（不含高亮系数）
        float rendW = CardView.Width * 108f * sc;       // 卡体渲染宽（px；108 = 每卡单位 px）
        float rendH = CardView.Height * 108f * sc;
        MenuCheck.Near(s, rendW / OrigCellW, ShotCardWRatio, RatioTol,
                       "★ 卡体渲染宽 ÷ 格宽 ≈ **原版实拍的 81.7%**（带宽 ±3% ⇒ 同时排掉 61%(A2 错读) 与 90%(整格直译)）");
        // 高度那一条见下面「卡底 = 张数条上沿」那两句（⛔ 别拿实拍的 82.8% 断卡**本体**高，理由见常量那段注释）

        // 竖直：卡底**不许**越过张数条上沿（用活对象的中心 + 渲染高现算，⛔ 不读 `CardsCardRect`）
        var cellD = CollectionWindow.CardsCellRect(0);  // 设计 px（格位数学；与建窗同一份）
        var cardC = MenuCheck.DesignPx(cv0.transform);
        float cardBottom = cardC.y + rendH * 0.5f;
        float counterTop = cellD.y2 - OrigCounterH;
        MenuCheck.True(s, cardBottom <= counterTop + 0.5f,
                       $"★ 活对象：卡底 {cardBottom:F2} ≤ 张数条上沿 {counterTop:F2}"
                       + "（把缩放改回原版公式直译 1.0417 ⇒ 卡底 516.16 > 504 ⇒ 这一条红）");
        MenuCheck.True(s, cardBottom >= counterTop - 2f,
                       $"★ 活对象：卡底 {cardBottom:F2} 离张数条上沿不超过 2px"
                       + "（= 卡块**真的填满**「格高 − 张数条」；卡被改小 ⇒ 这一条红）");
        // 横向：卡心必须落在格中线上（`SetPose` 的矩形左右对称）
        MenuCheck.Near(s, cardC.x, (cellD.x1 + cellD.x2) * 0.5f, 0.5f,
                       "活对象：卡心 x 落在格中线上（左右对称，不是贴左）");
        // 格容器本体的真尺寸（`Content i` 那颗 RectTransform）—— 由 `CardsCellRect` 建出来的，量它防「建错」
        var cellRT = win.CardsCells[0].parent as RectTransform;
        if (cellRT != null)
        {
            // 🩹 **2026-10-24（自检首跑订正）**：`RectTransform.rect` 是**世界/局部单位**（1 单位 = 108 px），
            //   原来直接拿它比设计 px（262.5 / 384）⇒ 必红（实得 2.43 / 3.56）。这里乘 108 换成设计 px。
            MenuCheck.Near(s, cellRT.rect.width * 108f, OrigCellW, 0.5f, "活对象：格容器 `Content 0` 宽 = **262.5**");
            MenuCheck.Near(s, cellRT.rect.height * 108f, OrigCellH, 0.5f, "活对象：格容器 `Content 0` 高 = **384**");
        }
        else MenuCheck.True(s, false, "（前提）第 1 格有父容器 `Content 0`");
    }

    /// <summary>单独入口：**只跑纯几何那一段**（不建窗、不碰存档 —— 见文件头用法）。</summary>
    public static void Run()
    {
        var s = new CheckSink("[E6] ") { Near = MenuNearStyle.Compact2 };
        Run(null, s);
        Debug.Log(s.P + $"=== 合计：✓ {s.Pass} · ✗ {s.Fail} ==="
                  + "（③ 活对象那一段要随 `CollectionScene.Run` 跑，本入口不含）");
    }
}
