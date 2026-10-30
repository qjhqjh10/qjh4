// B101R8_Assert.cs — 第一百零一会话 · 执行代理 `B101_R8`（抽屉最后两档 `CardDrawer` / `CardAlternateArtDrawer`）
//
// ============================ 它盯的是什么 ============================
//  这两档在 `B101_E3` 里被记成「本机没有 `BasicCardUI` 等价件 + 需真机验缩放」⇒ 没做。
//  `B101_R8` 把两条都推翻了（判据 → `资料/完全复刻/报告/B101_R8_抽屉最后两档.md` §A）：
//    · 等价物 = `Core/CardView.cs`（原版 `CardUI/2DCard` 那棵子树，逐节点同名同类）；
//    · 缩放 = **算式**（`UIScaleToFit__Fit.c`）：`paddingType=1`（乘）· `padding=(1.35,1.35)` ·
//      `targetRect = Content`(712×1080) · `CardUI.sizeDelta = (2.5437, 3.3686)`
//      ⇒ 每卡单位 `1.35×712/2.5437 = 377.87` 设计 px ⇒ `localScale = 377.87/108 = 3.4988`（整格时）。
//
//  本件只断言**不依赖 Unity 运行时**的那一半（纯算式 + 登记表）；画面与观感仍归真机验收。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1：把 `ItemDrawerCard.cs` 的 `DrCardPadMul` 从 `1.35` 改成 `1.0`（= 忘了那层"乘"的 padding）
//        ⇒ A1 红（3.4988 → 2.5917）。
//  · A2：把 `DrRootW`（712）或 `DrCardUIW`（2.5437）改错 ⇒ A1/A2 一起红。
//  · C1：把 `CardDrawerScale` 改成 `return 3.4988f;`（写死常量，丢掉 `frameK`）⇒ C1 红
//        （`frameK = 0.5` 不再是 1.7494）。
//  · C2：同上 —— 写死常量 ⇒ C2 的「与卡体设计宽度自洽」那一条也红（它是**另一条**独立推导：
//        用 `CardView.Width × 108 × scale / 712` 反算"卡宽 ÷ Content 宽"，必须 = 1.1106）。
//  · B1/B2：把 `DrawerImpls` 里那两条删掉 ⇒ 红（那是"登记有没有真发生"的唯一机检口）。
//  · B3：把 `ShowInfoOnClickOf` 里那两条去掉 ⇒ 红（原版 `showItemInfoOnClick` **19/19 · 7/7 全为 1**）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  放到「跑得到菜单外壳」的那条宿主里（`CardPresentation/Editor/ShellScene.cs` 最稳：抽屉所有档的
//  消费点都在外壳侧；`RewardsScene.cs` 也吃这块）。在它那条聚合行末尾**同一物理行**追加：
//      B101R8_Assert.Run(); pass += B101R8_Assert.Pass; fail += B101R8_Assert.Fail;
//
// ============================ 判据 / 报告 ============================
//  · 原版：`d:/2/tools/decomp_full/UIScaleToFit__Fit.c`（算式）· `Card Drawer.prefab` / `Card Alternate Art Drawer.prefab`（序列化字段）
//  · 本件报告：`资料/完全复刻/报告/B101_R8_抽屉最后两档.md` §A-4 / §六
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B101_R8`：卡牌抽屉两档的**缩放算式** + **登记表**。</summary>
public static class B101R8_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B101R8] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>整格（`frameK = 1`）时的目标缩放 —— 由原版四个数直接推出来，**不是抄现成常量**：
    /// `1.35 × 712 / 2.5437 / 108`（最后一个 108 = `ImageQuad.PixelsPerUnit` = 1080/10）。</summary>
    const float WantScaleAtK1 = 1.35f * 712f / 2.5437f / 108f;   // = 3.4988…

    public static void Run()
    {
        MenuCheck.Section(_sink, "B101_R8 抽屉最后两档：缩放算式 + 登记表（纯计算，不依赖 Unity 运行时）");
        SectionA_ScaleFormula();
        SectionB_Registration();
        SectionC_Discriminators();
        Debug.Log(_sink.P + "B101R8 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 缩放算式
    static void SectionA_ScaleFormula()
    {
        MenuCheck.Section(_sink, "A 缩放：`UIScaleToFit` 的 `1.35×Content.w / CardUI.w` 落到 `CardView.localScale`");

        float k1 = ItemDrawer.CardDrawerScale(1f);
        Tr(float.IsFinite(k1) && k1 > 0f, "A0 **前提**：`CardDrawerScale(1)` 是有限正数（= " + k1.ToString("F4") + "）");
        if (!(float.IsFinite(k1) && k1 > 0f)) return;

        N(k1, WantScaleAtK1, 0.0005f,
          "A1 整格缩放 = `1.35×712/2.5437/108` = " + WantScaleAtK1.ToString("F4")
          + "（把 `DrCardPadMul` 改回 1.0 ⇒ 变 2.5917 ⇒ 红）");

        N(ItemDrawer.CardDrawerScale(0.5f), WantScaleAtK1 * 0.5f, 0.0005f,
          "A2 与抽屉格的换算 `frameK` **线性**（0.5 格 ⇒ 一半；`FrameOf` 的 `K` 语义）");
    }

    // ============================================================ B · 三处登记真的发生了
    static void SectionB_Registration()
    {
        MenuCheck.Section(_sink, "B 登记：`DrawerImpls` 两条 + `showItemInfoOnClick` 两条");

        C(ItemDrawer.ImplOfDrawerClass("CardDrawer"), ItemDrawer.DrawerCard,
          "B1 `CardDrawer` 登记进 `DrawerImpls`（删掉那条 ⇒ 走兜底 `IconDrawer` ⇒ 红）");
        C(ItemDrawer.ImplOfDrawerClass("CardAlternateArtDrawer"), ItemDrawer.DrawerCardAltArt,
          "B2 `CardAlternateArtDrawer` 登记进 `DrawerImpls`");

        // 原版 `showItemInfoOnClick`：`CardDrawer` 19/19 = 1、`CardAlternateArtDrawer` 7/7 = 1（全票一致）
        Tr(ItemDrawer.ShowInfoOnClickOf(ItemDrawer.DrawerCard),
           "B3 `CardDrawer` 的 `showItemInfoOnClick`（原版 19/19 全为 1）⇒ 可点开卡详情");
        Tr(ItemDrawer.ShowInfoOnClickOf(ItemDrawer.DrawerCardAltArt),
           "B4 `CardAlternateArtDrawer` 的 `showItemInfoOnClick`（原版 7/7 全为 1）");
    }

    // ============================================================ C · 灭自证
    static void SectionC_Discriminators()
    {
        MenuCheck.Section(_sink, "C 灭自证：写死常量 / 丢掉输入值都要能被抓出来");

        float k1 = ItemDrawer.CardDrawerScale(1f), k2 = ItemDrawer.CardDrawerScale(2f);
        N(k2 / k1, 2f, 0.002f,
          "C1 `CardDrawerScale` 对 `frameK` 的两倍响应（改成 `return 3.4988f;` ⇒ 2 倍处得 1.0 ⇒ 红）");

        // 另一条独立推导：卡体（`CardView.Width` 卡单位）× 108 px/单位 × scale ÷ Content 宽
        // 必须 = 2.0927×377.87/712 = 1.1106（原版卡体比 `Content` **大 35%**，见报告 §A-4）。
        float cardWpx = CardView.Width * ImageQuad.PixelsPerUnit * k1;
        N(cardWpx / 712f, 1.1106f, 0.002f,
          "C2 自洽：卡体宽 ÷ `Content` 宽 = 1.1106（= 2.0927×377.87/712；把 2.5437 或 712 写错 ⇒ 红）");
    }
}
