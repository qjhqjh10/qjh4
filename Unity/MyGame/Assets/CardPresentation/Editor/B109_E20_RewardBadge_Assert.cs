// B109_E20_RewardBadge_Assert.cs — 第一百零九会话 · 执行代理 `B109_E20`
//
// ============================ 它盯的是什么 ============================
//  判据正本 = `资料/完全复刻/判据/B109_R13_AUD05剩余.md` §J1（`B109_R13` 已把立项前提推翻并查清）。
//  这一条要落的是**领奖窗（`RewardWindow`）卡片格的「新卡角标」那一拍**：
//    原版 `RewardWindow.Open` 每条奖励 Draw 完之后做一次接口分派
//      `sub_1803f3750(drawer, typeof(ICardDrawer))`（≈ `drawer as ICardDrawer`，`out_il2csharp/RewardWindow.cs:154`）
//      ⇒ 非 null 才 `ToggleNewCardBadge(GetOwnedCount<ObtainableItem>(item) < 2, withAnimation: true)`
//        （`:162` = `sub_180014bc0(1, typeof(ICardDrawer), drawer, owned<2 ? 1 : 0)`；
//         `disasm_va 0x180014BC0` = 接口分派 thunk，槽 1 + 两参 ⇒ 只能是 `ToggleNewCardBadge(bool,bool)`，
//         第 5 实参 `1` = `withAnimation:true`）。
//    🔴 **`DrawerOverride` 不按「已拥有」分档** —— ILSpy 那行 `(DrawerOverride)1` 是
//      `NoteDecompilerIssue` 之后凭空编的局部（该枚举只有 0/10/15/20/30，且那个局部之后没有任何读者）⇒ 伪码。
//
//    A ★ 判据主体：`RewardWindow.NewCardBadgeOn(cardId)` = **已拥有 &lt; 2**（不是卡包那条 `== 1`）
//    B ★ 守卫可达：`Type = "RawCardScript"` 的 `ItemSpec` ⇒ `PickDrawer` 落到 `CardDrawer`
//        （= `RewardWindow.BuildItem` 里那句 `res.Drawer == ItemDrawer.DrawerCard` 的判据那一侧）
//    C ★ 取件路径：卡抽屉的卡体是 `res.Node` 的**孙节点**（`Content/CardUI`）⇒ 必须
//        `GetComponentInChildren<CardView>(true)`；`res.Node.Find("CardUI")` 恒 null（`Transform.Find` 只找直接子件）
//    D ★ 端到端：已拥有 1 ⇒ `NewCardBadgeShown == true`；已拥有 2 ⇒ `false`；且 `NewCardBadgeBuilt == true`
//    E ★ 反向：`withAnimation:false` 时**不得**动 `localScale`（原版 `(withAnimation & option)` 合取）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  A 把 `NewCardBadgeOn` 改成恒 `true` ⇒ A3（拥有 2）红；改成卡包那条 `== 1` ⇒ A1（拥有 0）红。
//    （期望值**从 `Inventories.Cards` 的真实计数推**，⛔ 不从 `RewardWindow` 回读同一个常量。）
//  B 把 `ItemTypeSets` 里 `RawCardScript → CardDrawer` 那一格改掉 ⇒ B 红。
//  C 把 `RewardWindow` 里那句改成 `res.Node.Find(ItemDrawer.NodeCardUI)` ⇒ C 红（Find 只找直接子件）。
//  D 把 `BuildItem` 第 4 跳删掉/注释掉 ⇒ 本文件 D 组**仍绿**（它直调 `CardView`）——
//    这一段只保「机制对」，**接线**由报告 §没覆盖 那一条如实说明；⚠️ 见文末「边界」。
//  E 把 `ToggleNewCardBadge` 里那句 `withAnimation && isNewCard` 的合取去掉 ⇒ E 红。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（只要一个空场景）：任一宿主的聚合行、`//` **之前**同一物理行追加：
//      B109E20_Assert.Run(); pass += B109E20_Assert.Pass; fail += B109E20_Assert.Fail;
//  建议落点：`Editor/ShellScene.cs` 或 `Editor/RewardsScene.cs`（领奖窗那两个宿主）。
//
// ============================ 边界（如实记）============================
//  · **端到端的「接线」没被断言覆盖**：今天 `ItemDrawer.Spec()` 只从 `IdTypes` 取原版类型，
//    而 `IdTypes` **不含任何卡 id** ⇒ 真数据（`UM2` / `UM34` …）选出的抽屉**不是**卡抽屉
//    ⇒ `RewardWindow.BuildItem` 里那一跳**今天一次都不会触发**（A 组末尾把它当「记录」打日志，不当断言）。
//    要真跑起来，得让卡奖励的 `ItemSpec.Type` 被填成 `RawCardScript`（落点在 `Shell/ItemDrawer*.cs`，
//    不归本代理）—— 详见 `资料/完全复刻/报告/B109_E20.md` §顺手发现。
//  · 本文件只建临时 GameObject / 只改内存里的背包数量（`finally` 复原），**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B109_E20`：领奖窗「新卡角标」那一拍（判据 → `判据/B109_R13_AUD05剩余.md` §J1）。</summary>
public static class B109E20_Assert
{
    static readonly CheckSink _sink = new CheckSink("[E20] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>夹具卡 id：战役奖励真数据里出现过的那张（`Shell/CampaignData.cs:434` 的 `RewardSpec("UM2",…)`）。</summary>
    const string CardId = "UM2";

    /// <summary>自包含：只要一个空场景就能跑。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B109_E20 · 领奖窗「新卡角标」= `GetOwnedCount(item) < 2`（判据 → `判据/B109_R13_AUD05剩余.md` §J1）");
        try { SectionA_Rule(); }
        catch (System.Exception e) { Tr(false, "「A 判据主体」这一组抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }
        try { SectionB_Wiring(); }
        catch (System.Exception e) { Tr(false, "「B/C/D/E 抽屉侧」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }

        // ---- 记录（**不是断言**）：今天真数据上这一跳走不到。见文件头「边界」。
        var probeDef = CollectionData.Card(CardId);
        if (probeDef != null)
        {
            string prod = ItemDrawer.PickDrawer(ItemDrawer.Spec(probeDef.Id, null, probeDef.Id), DrawerOverride.Default);
            Debug.Log("[B109E20] 记录（不是断言）：`ItemDrawer.Spec(\"" + probeDef.Id + "\")` 今天选出的抽屉 = `"
                    + (prod ?? "<null>") + "` ⇒ `BuildItem` 的第 4 跳在真数据上**一次都不会触发**"
                    + "（卡奖励要走到卡抽屉，`ItemSpec.Type` 必须是 `RawCardScript`，而 `ItemDrawer.Spec` 只从 `IdTypes` 取、"
                    + "那张表今天不含卡 id）。详见 `资料/完全复刻/报告/B109_E20.md` §顺手发现。");
        }
        Debug.Log(_sink.P + "B109E20 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==========================================================================================
    //  A —— 判据主体：拥有数 < 2（**不是**卡包那条 `== 1`）
    // ==========================================================================================
    static void SectionA_Rule()
    {
        var def = CollectionData.Card(CardId);
        Tr(def != null, "A0 **前提**①：卡池里有 `" + CardId + "`（`CollectionData.Card` 查得到）");
        if (def == null) return;
        Tr(!string.IsNullOrEmpty(def.Rarity), "A0 **前提**②：`" + CardId + "` 的 `Rarity` 非空（`CardProgress.Owned` 要它）");
        if (string.IsNullOrEmpty(def.Rarity)) return;

        var inv = Inventories.Cards;
        Tr(inv != null, "A0 **前提**③：`Inventories.Cards` 在（拥有数的正本）");
        if (inv == null) return;

        var snap = inv.SnapshotAmounts();                 // 背包里**所有**条目的数量（`finally` 复原）
        bool hadEntry = inv.Get(def.Id) != null;
        int before = CardProgress.Owned(def.Id, def.Rarity);   // 摆之前的口径值（没条目时 = 「给足」兜底）
        try
        {
            // ---- 0 件（`< 2` 与 `== 1` 的**唯一区分点**：卡包那条口径在这里答「不是新卡」）----
            inv.Add(def, 0, null, true);
            int owned0 = inv.Get(def.Id) != null ? inv.Get(def.Id).Amount : -1;
            C(owned0, 0, "A1-前提：把 `" + def.Id + "` 的拥有数摆成 0（读回 = " + owned0 + "）");
            Tr(RewardWindow.NewCardBadgeOn(def.Id) == (owned0 < 2),
               "A1 ★ 拥有 0 件 ⇒ **显**新卡角标（原版 `GetOwnedCount(item) < 2`）"
             + "｜🧨 改成卡包那条 `== 1` ⇒ 本行红（0 件在原版领奖窗里**也算新卡**）"
             + "｜🧨 改成恒 `true` ⇒ 见 A3");

            // ---- 1 件 ----
            inv.Add(def, 1, null, true);
            int owned1 = inv.Get(def.Id) != null ? inv.Get(def.Id).Amount : -1;
            C(owned1, 1, "A2-前提：摆成 1（读回 = " + owned1 + "）");
            Tr(RewardWindow.NewCardBadgeOn(def.Id) == (owned1 < 2),
               "A2 ★ 拥有 1 件 ⇒ **显**（`1 < 2`）");

            // ---- 2 件 ----
            inv.Add(def, 2, null, true);
            int owned2 = inv.Get(def.Id) != null ? inv.Get(def.Id).Amount : -1;
            C(owned2, 2, "A3-前提：摆成 2（读回 = " + owned2 + "）");
            Tr(RewardWindow.NewCardBadgeOn(def.Id) == (owned2 < 2),
               "A3 ★ 拥有 2 件 ⇒ **不显**（原版 `< 2`）"
             + "｜🧨 把 `NewCardBadgeOn` 改成恒 `true` ⇒ 本行红");

            // ---- 3 件（冗余一档，防「只特判 2」那种巧合写法）----
            inv.Add(def, 3, null, true);
            int owned3 = inv.Get(def.Id) != null ? inv.Get(def.Id).Amount : -1;
            Tr(RewardWindow.NewCardBadgeOn(def.Id) == (owned3 < 2),
               "A4 ★ 拥有 3 件 ⇒ **不显**（读回 = " + owned3 + "）");

            // ---- 卡池里查不到的 id ⇒ `false`（⛔ 不拿别的档顶）----
            Tr(!RewardWindow.NewCardBadgeOn("B109E20-卡池里没有的 id"),
               "A5 卡 id 查不到 ⇒ `false`（判据空时不猜；调用方那一路本来就只在卡抽屉上跑）");
        }
        finally
        {
            if (hadEntry) inv.RestoreAmounts(snap);
            else
            {
                // 测试前**没有**这条：`RestoreAmounts` 不会新建条目 ⇒ 手工把它调回「摆之前那个口径值」，
                // 让 `CardProgress.Owned` 的读数与测试前**逐值相等**（= 游戏自己播种时会写的那种值）。
                var info = inv.Get(def.Id);
                if (info != null) info.SetAmount(before);
            }
        }
    }

    // ==========================================================================================
    //  B/C/D/E —— 卡抽屉侧：守卫可达 + 取件路径 + 端到端 + 动画合取
    // ==========================================================================================
    static void SectionB_Wiring()
    {
        var def = CollectionData.Card(CardId);
        Tr(def != null, "B0 **前提**：卡池里有 `" + CardId + "`");
        if (def == null) return;

        // B —— 守卫那一侧：卡类型的 `ItemSpec` ⇒ `PickDrawer` 必须给出 `CardDrawer`
        var spec = new ItemSpec
        {
            Id = def.Id, CardId = def.Id, Label = def.Id,
            Type = "RawCardScript",        // 原版类型键（`IdTypes` / `ItemTypeSets` 那张真表的键）
            Kind = ItemKind.Generic,
        };
        C(ItemDrawer.PickDrawer(spec, DrawerOverride.Default), ItemDrawer.DrawerCard,
          "B1 ★ `Type = \"RawCardScript\"` ⇒ `PickDrawer` 落到 **`CardDrawer`** —— 这正是 `RewardWindow.BuildItem` 里"
        + " `res.Drawer == ItemDrawer.DrawerCard` 那个守卫的另一侧（原版等价物 = `drawer is ICardDrawer`）"
        + "｜🧨 把 `ItemTypeSets` 里 `RawCardScript → CardDrawer` 那一格改掉 ⇒ 本行红");

        var root = new GameObject("B109E20_ROOT");
        ItemDrawResult res = default(ItemDrawResult);
        var inv = Inventories.Cards;
        var snap = inv.SnapshotAmounts();
        bool hadEntry = inv.Get(def.Id) != null;
        int before = CardProgress.Owned(def.Id, def.Rarity);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);
            st.NamePx = 0f; st.QuantityPx = 0f;          // 文字层不参与本项（也免了 TMP 依赖）
            res = ItemDrawer.Draw(root.transform, new PxRect(0f, 0f, 200f, 300f), spec, 1,
                                  DrawerOverride.Default, st);
            Tr(res.Node != null, "C0 **前提**①：卡抽屉的根节点建出来了");
            C(res.Drawer, ItemDrawer.DrawerCard, "C0 **前提**②：`Draw` 真的走了 `CardDrawer`");
            if (res.Node == null) return;

            // C —— 取件路径：卡体是**孙节点**（`Content/CardUI`）⇒ `Transform.Find` 找不到
            Tr(res.Node.Find(ItemDrawer.NodeCardUI) == null,
               "C1 ★ 反例：`res.Node.Find(\"" + ItemDrawer.NodeCardUI + "\")` 是 **null**"
             + "（`Transform.Find` 只找**直接子件**，卡体挂在 `Content/` 下面）"
             + "⇒ 取件必须走 `GetComponentInChildren<CardView>(true)`");
            var cv = res.Node.GetComponentInChildren<CardView>(true);
            Tr(cv != null, "C2 ★ `res.Node.GetComponentInChildren<CardView>(true)` 取到了卡体"
                         + "（= `RewardWindow.BuildItem` 第 4 跳用的**同一个**取法）");
            if (cv == null) return;

            // D —— 端到端：拥有 1 ⇒ 显；拥有 2 ⇒ 不显（角标层必须真建出来，否则「不显」是假绿）
            inv.Add(def, 1, null, true);
            int owned1 = inv.Get(def.Id) != null ? inv.Get(def.Id).Amount : -1;
            cv.ToggleNewCardBadge(RewardWindow.NewCardBadgeOn(def.Id), withAnimation: true);
            Tr(cv.NewCardBadgeBuilt,
               "D0 **前提**：角标那一层建出来了（图 `Resources/Art/ui_menu/WF_Special_offer_Value.png` 取得到）"
             + " —— 没建出来 ⇒ 下面的「不显」是空断");
            C(owned1, 1, "D1-前提：拥有数 = 1（读回 = " + owned1 + "）");
            Tr(cv.NewCardBadgeShown,
               "D1 ★ 拥有 1 件 ⇒ 卡面上的**新卡角标真的显示着**（`NewCardBadgeShown` = 读场上那一态，不是自报 bool）");

            inv.Add(def, 2, null, true);
            int owned2 = inv.Get(def.Id) != null ? inv.Get(def.Id).Amount : -1;
            cv.ToggleNewCardBadge(RewardWindow.NewCardBadgeOn(def.Id), withAnimation: true);
            C(owned2, 2, "D2-前提：拥有数 = 2（读回 = " + owned2 + "）");
            Tr(!cv.NewCardBadgeShown,
               "D2 ★ 拥有 2 件 ⇒ 角标**收起来了**（原版 `SetActive(false)`）"
             + "｜🧨 把判据改成恒 `true` ⇒ 本行红（A3 也一起红）");

            // E —— 反向：`withAnimation:false` 不得动缩放（原版 `(withAnimation & option)` 合取）
            Transform badge = FindDeep(cv.transform, "newCardBadge");
            Tr(badge != null, "E0 **前提**：角标那一层的节点找得到（名字 = `newCardBadge`，`CardView.AddLayer` 建的）");
            if (badge != null)
            {
                badge.localScale = new Vector3(3f, 3f, 3f);
                cv.ToggleNewCardBadge(true, withAnimation: false);
                N(badge.localScale.x, 3f, 1e-4f,
                  "E1 ★ `withAnimation:false` 时**不得**碰 `localScale`（原版 `if ((withAnimation & option) != 0)` 才 `DOScale(1.1, 0.35)`）"
                + "｜🧨 去掉 `withAnimation &&` 那道合取（无条件 `DOKill` + `localScale = one`）⇒ 本行红");
                Tr(cv.NewCardBadgeShown, "E1-附：`isNewCard:true` 这一支仍然是显示的");
                badge.localScale = Vector3.one;
            }
        }
        finally
        {
            if (res.View != null) res.View.Release();     // 抽屉句柄表是静态的 ⇒ 只撤自己那一格
            Object.DestroyImmediate(root);
            if (hadEntry) inv.RestoreAmounts(snap);
            else
            {
                var info = inv.Get(def.Id);
                if (info != null) info.SetAmount(before);  // 同 A 段：让它与测试前逐值相等
            }
        }
    }

    /// <summary>按名字递归找（`Transform.Find` 只找**直接子件**）。</summary>
    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var t = FindDeep(root.GetChild(i), name);
            if (t != null) return t;
        }
        return null;
    }
}
