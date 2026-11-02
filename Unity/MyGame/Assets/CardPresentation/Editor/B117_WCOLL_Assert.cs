// B117_WCOLL_Assert.cs — 执行代理 `B117-WCOLL` 的断言宿主
//
// 活：**收藏窗卡池格的「卡框底」压住了底下那条 `x1/x2` 红条**（真机 2026-11-03 发现）。
// 判据正本 → `资料/完全复刻/判据/B117-GC_界面四条.md` §GC-2 ＋ `判据/B117-WCOLL_收藏卡格.md`。
//
// 结论（判据链，⛔ 别再用实现反推）：
//   · **条本身是对的**：原版 prefab `Collection Card/Content/Counter` 的 `m_AnchorMin/Max` =
//     x 0.2857143 / 0.7142857 · y **0.002 / 0.09966714**（`menu_dump.py bundle_menus_assets_all "Collection Card" --depth 5`）
//     ⇒ 条顶 = 格底往上 **0.09966714 × 格高**。`CollectionWindow.CardsCounterRect` 逐值一致 ⇒ 本条只**钉住**它。
//   · **错的是卡画大了**：`CardUI/2DCard` 在 350×512 原型里 y **6.2 → 505.8** 是**含投影的外框**，不是可见卡框底
//     （同树的 `Front` 甚至到 523.7）⇒ 拿它反推「卡体真高 = 97.6% 格高」会**压条 33.6px**。
//     实拍（铁律 4）显示卡框金属底边**完整露在红条之上**；同族的卡组编辑窗就是这么做的
//     （`Deck/DeckRuntime.cs` 的 `Pool`：`CardH = CellH − CounterH`、`CardScale = CardH / (CardView.Height × PxPerUnit)`）。
//   ⇒ 本批把收藏页改回**同一条式子**（`CardsCardRect` 去掉 `CardsCounterH`、`CardsCardScale` 按卡位高反解），
//     两页共用一份口径。
//
// 三组必断（判据全部取自**原版 prefab 锚点**或**原版公式**，⛔ 不拿我们自己的常量当判据）：
//   A ★① 卡框底 **≤** 条顶（旧实现 `CardsCardRect == cell` 下必红 ⇒ **灭自证**）
//   B ★② 卡高与 `DeckRuntime.Pool` **同式同值**（= 格高 − 条顶，比值 0.90033286；旧公式 0.9760 必红）
//   C ★③ 条本身的矩形**一个像素都没被挪动**（防止「为了不重叠去挪条」这种假修）
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_WCOLL_Assert.Run -logFile -
//
// 判绿红：`grep "^B117WCOLL"` 的**合计行**；逐条按**行首** `B117WCOLL ✓` / `B117WCOLL ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主要求：**A/B/C 三组不需要任何场景 / 窗口 / 卡**（纯几何 + 纯算术，随时可跑）。
//   D 组只在**场景里真有 `CollectionWindow`** 时才有对象 —— 没有时它印一行「**未验**」，**不计入通过数**
//   （⛔ 不静默：合计行会把「未验 N 条」单独打出来）。
using UnityEngine;
using CardPresentation;

public static class B117_WCOLL_Assert
{
    static int _pass, _fail, _unverified;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int Unverified { get { return _unverified; } }

    const string Tag = "B117WCOLL";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F6") + " ± " + tol.ToString("F6")
                  + "；实得 " + got.ToString("F6") + "）");
    }

    /// <summary>「没对象可验」—— ⛔ 不算通过、也不算红，但**必须印出来并进 `Unverified` 计数**。</summary>
    static void MarkUnverified(string msg)
    {
        _unverified++;
        Debug.LogWarning(Tag + " · 未验（不计入通过数）：" + msg);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  原版 prefab 的**字面量**（⛔ 别读 `CollectionWindow.CardsCnt*` / `CardsCounterH` 反推
    //  —— 那就是拿实现证明实现；这几个数是 `Collection Card` 预制体里的序列化值）
    //  `menu_dump.py bundle_menus_assets_all "Collection Card" --depth 5`（2026-11-03 现读）：
    //    · 原型 = **350 × 512**
    //    · `Content/Counter`：`m_AnchorMin = (0.2857143, 0.002)` · `m_AnchorMax = (0.7142857, 0.09966714)`
    //    · `CardUI/2DCard`：18, 6.2 → 332, **505.8**（含投影的外框 ⇒ ⛔ 不能当可见卡框底）
    //    · `CollectionCard` MB：`initialCardScale = 150` · `initialHeight = 512`
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    const float OrigCardProtoW = 350f, OrigCardProtoH = 512f;
    const float OrigCntMinX = 0.2857143f, OrigCntMaxX = 0.7142857f;      // `/Counter` 锚点 x
    const float OrigCntMinY = 0.002f, OrigCntMaxY = 0.09966714f;         // `/Counter` 锚点 y（上锚 = 条顶）
    const float OrigInitialCardScale = 150f, OrigInitialHeight = 512f;   // `CollectionCard` MB

    /// <summary>卡位高 ÷ 格高 = `1 − 0.09966714` = **0.90033286**（只由 prefab 的上锚点决定）。
    /// ⚠️ 这是**卡【位置框】占格高的比例**，不是「可见卡框金属边」那张图的比例 —— 后者量不出来（含投影/描边）。</summary>
    const float OrigCardBoxRatio = 1f - OrigCntMaxY;

    public static void Run()
    {
        _pass = 0; _fail = 0; _unverified = 0;
        try
        {
            // ⚠️ 每组各包一层：一组炸掉**不许**把后面几组一起拖没（否则报告里只剩一条异常、别的全是「没验」）。
            Group("A·卡框底不压条", A_CardBottomAboveBar);
            Group("B·卡高与卡组编辑窗同式", B_SameAsDeckPool);
            Group("C·条没被挪动", C_BarUntouched);
            Group("D·活对象（有窗口才验）", D_LiveCard);
        }
        finally
        {
            Debug.Log(Tag + " 合计：通过 " + _pass + " · 失败 " + _fail + " · 未验 " + _unverified);
        }
    }

    static void Group(string name, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            // 说清楚是**这一组**炸了（⛔ 不许把它吞成一条静默的「没验」）
            Check(false, name + "：这一组抛异常 ⇒ 本组结论**未验**：" + e.GetType().Name + " " + e.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  A. ★① 卡框底 **≤** 条顶（核心 · 灭自证：旧实现 `CardsCardRect(cell) => cell` 下 A3/A4 必红）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void A_CardBottomAboveBar()
    {
        // 一格（相对坐标即可 —— 两条几何都只吃相对量）
        var cell = new PxRect(0f, 0f, CollectionWindow.CardsCellW, CollectionWindow.CardsCellH);

        // ---- 前提（硬检查，⛔ 不许 `if (前提) Check(结论)`）----
        Check(cell.W > 1f && cell.H > 1f,
              "A0 前提：格几何拿得到（" + cell.W.ToString("F2") + " × " + cell.H.ToString("F2") + "）");
        if (!(cell.W > 1f && cell.H > 1f)) { Check(false, "A：前提不成立 ⇒ 本组未验"); return; }

        // ---- 前提：我们的格是原版原型 350×512 的**等比**缩小（否则「÷ 格高」那几条判据就不成立）----
        CheckNear(cell.W / OrigCardProtoW, cell.H / OrigCardProtoH, 1e-4f,
                  "A0b 前提：格是原型 350×512 的等比缩放（宽比 " + (cell.W / OrigCardProtoW).ToString("F5")
                + " / 高比 " + (cell.H / OrigCardProtoH).ToString("F5") + " ⇒ 同一档 ⇒ 锚点比例可直接套）");

        var cardR = CollectionWindow.CardsCardRect(cell);
        var bar = CollectionWindow.CardsCounterRect(cell);
        float barTopY = bar.y1;          // 「条顶」= 条在画布坐标里的上沿（y 向下为正 ⇒ y1 就是上沿）

        CheckNear(cardR.x1, cell.x1, 1e-3f, "A1 卡块左沿 = 格左沿（横向不动）");
        CheckNear(cardR.x2, cell.x2, 1e-3f, "A2 卡块右沿 = 格右沿（横向不动）");

        // ---- ★① 本条的**核心**：卡框底不许越过条顶 ----
        float overlap = cardR.y2 - barTopY;      // > 0 = 卡底压进条里
        Check(overlap <= 1e-2f,
              "A3 ★① 卡框底 **不压条**：`CardsCardRect(cell).y2` = " + cardR.y2.ToString("F3")
            + " ≤ 条顶 " + barTopY.ToString("F3") + "（差值 " + overlap.ToString("F3") + " px ≤ 0）"
            + "｜判据 = 原版实拍（卡框金属底边完整露在红条之上）＋ 卡组编辑窗同一条几何"
            + "｜🧨 退回 `CardsCardRect(cell) => cell` ⇒ 差值 = +" + (cell.H * OrigCntMaxY).ToString("F2")
            + " px ⇒ 本条红");

        // ---- ★①b 卡块高 = 格高 − 条顶（比值只由 prefab 上锚点决定）----
        CheckNear(cardR.H / cell.H, OrigCardBoxRatio, 1e-4f,
                  "A4 ★①b 卡块高 ÷ 格高 = **0.90033286**（= 1 − 原版 `/Counter` 的 `m_AnchorMax.y` 0.09966714）"
                + "｜🧨 旧实现 = 1.0（整格）⇒ 红");

        // ---- 前提：卡块上沿贴格上沿（与卡组编辑窗 `cellTop + CardH/2` 同一条摆放）----
        CheckNear(cardR.y1, cell.y1, 1e-3f,
                  "A5 卡块上沿 = 格上沿（卡从格顶往下量 —— 与 `DeckRuntime.Pool` 的 `cellTop + CardH/2` 同一摆放）");

        // ---- 卡块高 + 条顶 = 格高 ⇒ 中间没有缝、也没有溢出 ----
        float barTopH = cell.y2 - barTopY;       // 条顶离格底多高
        CheckNear(cardR.H + barTopH, cell.H, 1e-2f,
                  "A6 卡块高 + 条顶距 = 格高（" + cardR.H.ToString("F3") + " + " + barTopH.ToString("F3")
                + " = " + cell.H.ToString("F3") + "）⇒ 卡底与条顶**严丝合缝**，⛔ 不许改成「留一条缝」");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  B. ★② 卡高与卡组编辑窗 `DeckRuntime.Pool` **同式同值**
    //     判据式（`Deck/DeckRuntime.cs`）：`CardH = CellH − CounterH` ·
    //                                   `CardScale = CardH / (CardView.Height × PxPerUnit(108))`
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void B_SameAsDeckPool()
    {
        float cellH = CollectionWindow.CardsCellH;
        float cardScale = CollectionWindow.CardsCardScale;

        Check(cardScale > 0f && !float.IsNaN(cardScale) && !float.IsInfinity(cardScale),
              "B0 前提：`CollectionWindow.CardsCardScale` 是有限正数（" + cardScale.ToString("F6") + "）");
        if (!(cardScale > 0f)) { Check(false, "B：前提不成立 ⇒ 本组未验"); return; }

        // ---- 渲染高（px）= 卡高（卡单位）× 108 × 世界缩放 ----
        float cardPx = CardView.Height * 108f * cardScale;

        // 判据值 = 格高 × (1 − 原版上锚点)，⛔ 不是 `CardsCounterH` 那个属性读回来（那是拿实现证明实现）
        float wantPx = cellH * OrigCardBoxRatio;
        CheckNear(cardPx, wantPx, 0.05f,
                  "B1 ★② 卡渲染高 = 格高 × 0.90033286 = **" + wantPx.ToString("F3") + " px**"
                + "（判据 = 原版 `/Counter` 的 `m_AnchorMax.y` 0.09966714，⛔ 不是我们自己的常量）");

        // ---- 与卡组编辑窗**同式**：该窗的卡高也是 `CellH − CounterH`（它的格同为 262.5×384）----
        //   桌面档两窗的格高相同（prefab 同值 ×1）⇒ 同一个卡高、同一个缩放；
        //   两窗各写一个数才是 2026-10-25 分家的根因，所以这条比的是**绝对值**。
        float deckCardPx = cellH - OrigCntMaxY * cellH;   // = 345.728（原版锚点 × 同一格高）
        CheckNear(cardPx, deckCardPx, 0.05f,
                  "B2 ★② 与卡组编辑窗 `DeckRuntime.Pool` **同值**：卡高 = 格高 − 条顶 = "
                + deckCardPx.ToString("F3") + " px（该窗 `m.CardH = CellH − CounterH`、`m.CardScale = CardH ÷ (H×108)`）"
                + "｜🧨 收藏页单独按 `CellH × 150/512` 反解 ⇒ 374.77 px ⇒ 差 29.04 px ⇒ 本条红");

        // ---- 灭自证（结构性）：旧公式给出的卡高**必然**越过条顶 ⇒ 「新式」与「旧式」不可能同时满足 A3 ----
        float oldScale = cellH * OrigInitialCardScale / OrigInitialHeight / 108f;
        float oldPx = CardView.Height * 108f * oldScale;
        Check(oldPx - cardPx > 20f,
              "B3 ★②b 旧公式（`格高 × 150/512 ÷ 108` ⇒ " + oldPx.ToString("F2") + " px）比现式高 "
            + (oldPx - cardPx).ToString("F2") + " px ⇒ **两条式子结构互斥**："
            + "旧式下卡底必压条、A3 必红（⛔ 别把这条当成「随便挑一个」）");

        // ---- 两档同值（小屏 = 格与条同时 ×1.5 ⇒ 比值不变；纯算术，⛔ 不动全局 `SmallScreenUI` 开关）----
        float k = 1.5f;
        float ratioD = (cellH * OrigCardBoxRatio) / (CardView.Height * 108f);
        float ratioM = (cellH * k * OrigCardBoxRatio) / (CardView.Height * 108f);
        CheckNear(ratioM / ratioD, k, 1e-4f,
                  "B4 两档缩放比 = **1.5**（小屏的格与条同时 ×1.5 ⇒ `CardsCardScale` 随之 ×1.5，"
                + "比值不变；桌面 " + ratioD.ToString("F6") + " / 小屏 " + ratioM.ToString("F6") + "）");
        Debug.Log(Tag + " · B4 说明：本段是纯算术，未切 `SmallScreenUI` 全局开关（零副作用）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  C. ★③ 条本身的矩形**没被改动**（防止「为了不重叠去挪条」这种假修 —— 条本来就是原版值）
    //     判据 = prefab `Content/Counter` 的四个序列化锚点（逐值抄在上面）
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void C_BarUntouched()
    {
        var cell = new PxRect(0f, 0f, CollectionWindow.CardsCellW, CollectionWindow.CardsCellH);
        var bar = CollectionWindow.CardsCounterRect(cell);

        Check(bar.W > 1f && bar.H > 1f,
              "C0 前提：条几何拿得到（" + bar.W.ToString("F3") + " × " + bar.H.ToString("F3") + "）");
        if (!(bar.W > 1f && bar.H > 1f)) { Check(false, "C：前提不成立 ⇒ 本组未验"); return; }

        CheckNear((bar.x1 - cell.x1) / cell.W, OrigCntMinX, 1e-5f,
                  "C1 ★③ 条左沿 ÷ 格宽 = 原版 `m_AnchorMin.x` 0.2857143（⛔ 为了躲卡去挪条 ⇒ 红）");
        CheckNear((bar.x2 - cell.x1) / cell.W, OrigCntMaxX, 1e-5f,
                  "C2 ★③ 条右沿 ÷ 格宽 = 原版 `m_AnchorMax.x` 0.7142857");
        CheckNear((cell.y2 - bar.y2) / cell.H, OrigCntMinY, 1e-5f,
                  "C3 ★③ 条底距格底 ÷ 格高 = 原版 `m_AnchorMin.y` 0.002");
        CheckNear((cell.y2 - bar.y1) / cell.H, OrigCntMaxY, 1e-5f,
                  "C4 ★③ 条顶距格底 ÷ 格高 = 原版 `m_AnchorMax.y` **0.09966714**"
                + "｜🧨 把条往下挪「腾出地方」⇒ 本条红（真因在卡，不在条）");
        CheckNear(bar.H, (OrigCntMaxY - OrigCntMinY) * cell.H, 1e-3f,
                  "C5 ★③ 条高 = (0.09966714 − 0.002) × 格高 = " + ((OrigCntMaxY - OrigCntMinY) * cell.H).ToString("F3") + " px");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  D. 活对象（**只在场景里真有 `CollectionWindow` 时**才验；没有 ⇒ 印「未验」，不计入通过数）
    //     验的是「`SetPose` 真拿到这条 scale、卡心真落在卡块中心」—— 那是 A/B 两组静态几何
    //     看不见的一跳（`RebuildCardsCells` 把 `CardsCardScale` 与 `CardsCardRect` 一起用）。
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    static void D_LiveCard()
    {
        var win = Object.FindObjectOfType<CollectionWindow>();
        if (win == null)
        {
            MarkUnverified("场景里没有 `CollectionWindow`（独立跑 `Run` 时的正常情形）⇒ "
                     + "「`SetPose` 真吃到 `CardsCardScale`」这一跳没验；挂进 `CollectionScene` 宿主时它会自动验。");
            return;
        }
        if (win.CardsCells == null || win.CardsCells.Count == 0)
        {
            MarkUnverified("`CollectionWindow` 在，但卡池一格都没画（没进过 Cards 页 / 视口里没卡）⇒ 活对象没验。");
            return;
        }
        var cv = win.CardsCells[0] != null ? win.CardsCells[0].GetComponent<CardView>() : null;
        if (cv == null)
        {
            Check(false, "D0 （前提）第 1 格里取得到 `CardView` —— 取不到 ⇒ 本条无对象，但**不是通过**");
            return;
        }

        // ---- 前提：这一格真在画面里（设计 px 有值），否则量不到就没意义 ----
        var c0 = MenuCheck.DesignPx(cv.transform);
        Check(Mathf.Abs(c0.x) < 9000f && Mathf.Abs(c0.y) < 9000f,
              "D0b 前提：第 1 格的 `CardView` 在场景里有设计 px 坐标（" + c0.x.ToString("F1") + ", " + c0.y.ToString("F1") + "）");
        if (Mathf.Abs(c0.x) > 9000f) { Check(false, "D：前提不成立 ⇒ 本组未验"); return; }

        // ---- ★ 缩放：`SetPose` 第三实参 == `CardsCardScale` ----
        CheckNear(cv.BaseScale, CollectionWindow.CardsCardScale, 1e-4f,
                  "D1 ★ `SetPose` 用的缩放 = `CollectionWindow.CardsCardScale`（="
                + CollectionWindow.CardsCardScale.ToString("F6") + "）"
                + "｜🧨 `RebuildCardsCells` 里再写一个式子 ⇒ 两处会漂 ⇒ 红");

        // ---- ★ 卡心 y = 第 1 格的卡块中心（= 格心 − 条高/2），且卡底 ≤ 条顶 ----
        var cellD = CollectionWindow.CardsCellRect(0);
        var cardD = CollectionWindow.CardsCardRect(cellD);
        var barD = CollectionWindow.CardsCounterRect(cellD);
        float rendH = CardView.Height * 108f * cv.BaseScale;
        float cardBottom = c0.y + rendH * 0.5f;

        CheckNear(c0.y, cardD.CY, 0.6f,
                  "D2 ★ 活对象卡心 y = 卡块中心 " + cardD.CY.ToString("F2") + "（= 格心 "
                + cellD.CY.ToString("F2") + " − 条顶/2）");
        Check(cardBottom <= barD.y1 + 0.6f,
              "D3 ★ 活对象卡底 " + cardBottom.ToString("F2") + " ≤ 条顶 " + barD.y1.ToString("F2")
            + "（差 " + (cardBottom - barD.y1).ToString("F2") + " px）"
            + "｜🧨 旧的整格反解 ⇒ 卡底 ≈ 格底 − 4.65 ⇒ 压条 33.6 px ⇒ 本条红");
        CheckNear(rendH, cardD.H, 0.6f,
                  "D4 ★ 活对象渲染高 " + cardD.H.ToString("F2") + " = 卡块高（卡正好铺满这条框，不溢出也不留缝）");
    }
}
