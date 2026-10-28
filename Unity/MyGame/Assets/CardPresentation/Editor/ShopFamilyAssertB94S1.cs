// ShopFamilyAssertB94S1.cs — 商店族（`账/商店.tsv`）第九十四会话 `B94_S1` 的断言
//
// ============================ 它盯的是什么 ============================
// 本轮 `B94_S1` 在 `Shell/` 里**真改的**只有一处（其余 25 条经回源核为「已落地 / 卡在远端」），
// 另外补一条**原无断言**的 `ShopTab.SetStore` 分页闸：
//   A `GachaEventTab.Initialize` 第 ⑥ 句：`Where get cards` 钮的显隐闸（原版 `CardsSpecialConfig` 驱动）
//     —— 本轮把 `BuildPage` 里硬编码的 `SetActive(true)` 换成 `ApplyWhereToGetCardsVisible()`（账 `商店:108`）。
//   B `ShopTab.SetStore` 第 ① 步：`CheckTimer` 虚方法的分页闸（`PageShowsTimer`；账 `商店:305`）。
// 逐条的判据、做法、为什么某些格仍停在 B → `资料/完全复刻/报告/B94_S1.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `ShopFamilyAssertB94S1.Run();`
//   + 宿主里两行（照 `ShopFamilyAssertB93S1` 的先例）：
//     `_sink.Pass += ShopFamilyAssertB94S1.Pass; _sink.Fail += ShopFamilyAssertB94S1.Fail;`
//   · **不跑 Unity 批处理**、不建持久资产；临时结点在收尾 `DestroyImmediate`。
//   · 不依赖 `WindowsManager`（`ShopTabPage.PageShowsTimer` 只读 `_page`）；`GachaEventWindow.Build()` 是裸组件建树。
//
// ============================ 🔴 灭自证（两态/翻面法都写在文案里）============================
//   · A：把 `BuildPage` 里那句硬编码回 `SetActive(true)` ⇒ A5 红（`false` 那档关不掉）；
//        把 `WhereToGetCardsVisible` 默认值改成 `false` ⇒ A3 红。两态对照 ⇒ 写死成常量必有一条红。
//   · B：把 `_page==0` 那道闸放宽成「全 true」⇒ B3 红；把结果取反 ⇒ B1/B2 红。
using CardPresentation;   // 🆕 2026-10-29（调度台补）：`GachaEventWindow` / `ShopTabPage` 在 `namespace CardPresentation` 里，
                          //   原作者漏了这条 using ⇒ 编辑器程序集 3 条 CS0246/CS0103，整族断言一遍都编不过（同目录 `ShopFamilyAssertE13.cs:26` 有这条）。
using UnityEngine;

/// <summary>执行代理 `B94_S1`：商店族本波**真改的**那一处（`Where get cards` 显隐闸）＋ 补一条原无断言的
/// `ShopTab.SetStore` 分页闸。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量**（`true` / `false`），**不从被测实现里读** —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class ShopFamilyAssertB94S1
{
    static readonly CheckSink _sink = new CheckSink("[S1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_S1 商店族：Where-get-cards 显隐闸 / ShopTab 刷新计时器分页闸");

        SectionA_WhereGetCards闸();
        SectionB_刷新计时闸();
    }

    // ------------------------------------------------------------ A 抽卡页 `Where get cards` 钮的显隐闸
    static void SectionA_WhereGetCards闸()
    {
        MenuCheck.Section(_sink, "A `GachaEventTab.Initialize` 第 ⑥ 句：`Where get cards` 钮的显隐闸（config 驱动）");

        var go = new GameObject("S1b_GachaGate");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            bool built = false;
            try { win.Build(); built = true; }
            catch (System.Exception ex)
            {
                Tr(false, "A1 前提：`GachaEventWindow.Build()` 跑得完（炸了：「" + ex.GetType().Name + " " + ex.Message + "」）");
            }
            if (!built) return;

            Tr(win.GetCardsButtonNode != null, "A2 前提：`Where get cards` 钮（`Generic Simplified UI Button (1)`）建出来了");

            C(win.WhereToGetCardsVisible, true,
               "A3 ★★ `WhereToGetCardsVisible` 默认 = `true`（我们挑的默认值；原版读 `CardsSpecialConfig.whereToGetCardsData.expansionData.Length != 0`，"
             + "那份 config 的实例 SO 本机没有 ⇒ 默认显示；见 `GachaEventWindow.WhereToGetCardsVisible` 的 xmldoc）"
             + "｜🧨 把默认值改成 `false` ⇒ 本条红");
            C(win.WhereToGetCardsButtonActive, (bool?)true,
               "A4 ★ `Build()` 末尾按默认值把钮摆成**开**（= 原版 `Initialize` 第 ⑥ 句 `gameObject.SetActive(active)`）");

            // 两态对照：关掉再应用
            win.WhereToGetCardsVisible = false;
            win.ApplyWhereToGetCardsVisible();
            C(win.WhereToGetCardsButtonActive, (bool?)false,
               "A5 ★★ `WhereToGetCardsVisible = false` + `ApplyWhereToGetCardsVisible()` ⇒ 钮**关掉**"
             + "（= 原版 `expansionData.Length == 0` 那一档）"
             + "｜🧨 把 `BuildPage` 里那句挪回硬编码 `SetActive(true)`、或在 `ApplyWhereToGetCardsVisible` 里写死常量 ⇒ 本条红");

            win.WhereToGetCardsVisible = true;
            win.ApplyWhereToGetCardsVisible();
            C(win.WhereToGetCardsButtonActive, (bool?)true,
               "A6 ★ 两态对照：改回 `true` 再应用 ⇒ 钮又开（⛔ 别写死成常量，否则 A5/A6 必有一条红）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ B 商店页 `SetStore` 的刷新计时器分页闸
    static void SectionB_刷新计时闸()
    {
        MenuCheck.Section(_sink, "B `ShopTab.SetStore` 第 ① 步：`CheckTimer` 分页闸（`PageShowsTimer`）");

        // 判据 = `ShopTab__SetStore.c:22-40`（假 ⇒ 整个 `TimeCounter` 块 `SetActive(false)`；真 ⇒ 开 + Initialize）
        //        ＋ 四个 override（`ShopTab__CheckTimer.c` 基类恒 false · `CardShopTab`/`CosmeticsShopTab` 9 点规则 ·
        //        `VIPCardShopTab : CardShopTab` 不 override）。
        //   `PageShowsTimer` 只读 `_page`（`SetHost` 灌）⇒ 不需要真 `MainMenuSubmenuWindow`、也不需要建树；
        //   用 `AddComponent` 而不是 `new`（MonoBehaviour 禁 `new`，Unity 会警告/返回异常实例）。
        var go = new GameObject("S1b_ShopTabTimer");
        var t = go.AddComponent<ShopTabPage>();
        try
        {
            System.DateTime due;

            t.SetHost(null, null, 0);                       // Cards 页 = `CardShopTab`
            C(t.PageShowsTimer(out due), true,
               "B1 ★★ Cards 页（`CardShopTab` 的 9 点规则：目标时刻恒在将来 ⇒ `TotalMinutes > 0`）⇒ **显示刷新计时器**");

            t.SetHost(null, null, ShopTabPage.VipPage);     // VIP 页 = `VIPCardShopTab : CardShopTab`（不 override）
            C(t.PageShowsTimer(out due), true,
               "B2 ★ VIP 页（`VIPCardShopTab` **不 override `CheckTimer`**）⇒ 同 9 点规则 ⇒ **显示**");

            t.SetHost(null, null, 2);                       // Items 页 = 基类
            C(t.PageShowsTimer(out due), false,
               "B3 ★★ Items 页（基类 `ShopTab.CheckTimer` 全文 = `out = Now; return false;`）⇒ **整块隐藏**"
             + "｜🧨 把 `_page==0 || _page==VipPage` 那道闸放宽成「恒 true」⇒ 本条红");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
