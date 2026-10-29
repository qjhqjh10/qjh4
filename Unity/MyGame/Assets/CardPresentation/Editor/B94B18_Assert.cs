// B94B18_Assert.cs — 执行代理 B18（第九十四会话 · 波 4）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `未分类/其他:312` `GameObjectUniqueIdentifier` 的**注册点**那一半
//   （注册表 = `MenuDraw.MenuUniqueIds`（B94_S3 已建）· 消费端 = `TutorialPointerUi.TutorialPointerData.Target`（B94_B6 已建）
//     · 本轮补的 = **谁把 id 登记进去**）。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B94_B6_判据补全.md` §6/§7 ＋ 本轮报告 `资料/完全复刻/报告/B94_B18.md`。
//
// ============================ 12 个唯一 id（现读，`B94_B18` 复核过实例面）============================
//   原版实例 **17 个**（`bundle_menus_assets_all` 13 + `mainmenuwarpforge` 场景 4）—— 按 `m_Script`
//   pathID `5370249714765181407` 反查现数，去重后是 **12 个 id**（B6 判据 §6 写的「10 个」是**算错了**，
//   它自己的表就列了 12 行；本轮逐个 `id` 字段现读确认 = 12）。
//
//   | id | 原版 GO 名（现读） | 我们建它的人（本轮接的注册点） |
//   |---|---|---|
//   | `PlayRankedButton`（×6） | `To Battle Button` | `MenuTutorialFlow.Resolve`（建它的是 `LiveOpsEventWindow`，白名单外）|
//   | `RankedModeMenuButton` | `Skirmish Ranked Game Mode Container 1x2` | `MainMenuRuntime.BuildModeCard` |
//   | `RewardsMenu_MissionsButton` | `MissionsRewardsButton` | `MenuTutorialFlow.TabButton`（建它的是 `GameWindowWithTabs`）|
//   | `RewardsMenu_CampaignButton` | `CampaignRewardsButton` | 同上 |
//   | `RewardsMenu_ForgeButton` | `Forge Button` | 同上 |
//   | `CollectionCardsButton` | `Cards` | 同上 |
//   | `CardCollectionReferenceCardPointer` | `Reference Card Pointer` | ⛔ **本批接不了**（我们根本没建那颗节点，见报告 §三）|
//   | `CampaignRewardUnlockBaseReward` | `Unlock Button` | `MenuTutorialFlow.Resolve`（建它的是 `CampaignRewardWindow`）|
//   | `RewardsMenuButton` | `Main Menu Navigation Button - Rewards` | `MainMenuRuntime.BuildNavigationPanel` |
//   | `CollectionMenuButton` | `Main Menu Navigation Button - Collection` | 同上 |
//   | `HomeMenuButton` | `Main Menu Navigation Button - Home` | 同上 |
//   | `UpgradeCardButton` | `Upgrade Button`（我们叫 `UpgradeHit`） | `CardDetailPopup`（升级钮那一处）|
//
// ============================ 怎么用（宿主）============================
// `B94B18Assert.Run();`
//   + 宿主里两行（照 `B94B6Assert` 的先例）：
//     `_sink.Pass += B94B18Assert.Pass; _sink.Fail += B94B18Assert.Fail;`
//   · 🔴 **推荐宿主 = `Editor/MainMenuScene.cs`（`MainMenuScene.Run`）** —— C 段要读一棵**真建出来的常驻主菜单**
//     （`MainMenuRuntime.InstanceOrFind()` —— ⛔ 别读 `Instance`：它只在 `Awake` 赋值、编辑模式恒 null）。接到别的宿主上 C0 会红（那是**故意的**：不许静默跳过）。
//   · 本文件**不建持久资产**；建的 `GameObject` 都在 `finally` 里 `DestroyImmediate`。
//   · 静态表（`MenuUniqueIds`）每条都在 `finally` 里 `ClearForTest()` —— 否则会污染别的宿主。
//     ⚠️ 顺带影响：`ClearForTest()` 会把**生产注册的那几条**一起清掉（C 段必须排在 A/B 之前跑，本文件已这样排）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `MenuUniqueIds.Register` 改成空实现 ⇒ `Find` 回 null ⇒ A 全红。
//   · A 段：把 `TutorialPointerData.Target` 改成走 `FindComponent`（返回组件）⇒ 编译不过；
//           改成 `id` 没用上（恒回 `this` 之类）⇒ A 红。
//   · B 段：把 `RegisterUnique` 里那两行「先摘旧」删掉 ⇒ 不红（原版「后来的赢」仍然成立），
//           但**再来一条 LogError**；真正能红的是「不登记新的」⇒ B1 红。
//   · C 段：把 `BuildNavigationPanel` 末尾那三行 `RegisterUnique` 删掉 ⇒ C1/C2/C3 红。
//           把 `BuildModeCard` 里 `RankedModeMenuButton` 那两行删掉 ⇒ C4 红。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 B18：唯一 id 的**注册点**（`GameObjectUniqueIdentifier.Awake` 自注册那一半）的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B94B18Assert
{
    static readonly CheckSink _sink = new CheckSink("[B18] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>原版 `GameObjectUniqueIdentifier` 的全部唯一 id（现读 12 个；判据 → 报告 §一 那张表）。</summary>
    public static readonly string[] AllUniqueIds =
    {
        "PlayRankedButton",                 // ×6（模式窗的 `To Battle Button`）
        "RankedModeMenuButton",             // 主菜单模式卡
        "RewardsMenu_MissionsButton",       // 奖励窗 Missions 页签
        "RewardsMenu_CampaignButton",       // 奖励窗 Campaign 页签
        "RewardsMenu_ForgeButton",          // 奖励窗 Forge 页签
        "CollectionCardsButton",            // 收藏窗 Cards 页签
        "CardCollectionReferenceCardPointer",// ⛔ 本仓没有这颗节点（见报告 §三）
        "CampaignRewardUnlockBaseReward",   // 战役窗基础列 `Unlock Button`
        "RewardsMenuButton",                // 左竖导航 Rewards
        "CollectionMenuButton",             // 左竖导航 Collection
        "HomeMenuButton",                   // 左竖导航 Home
        "UpgradeCardButton",                // 卡牌详情窗「升级」钮
    };

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B18 唯一 id 的注册点（原版 `GameObjectUniqueIdentifier.Awake` 自注册那一半）");

        SectionC_生产注册点();          // ⚠️ 必须排在 A/B 之前（A/B 末尾会 `ClearForTest`，把生产登记一起清掉）
        SectionA_十二条id往返();
        SectionB_顶掉旧的与空值();

        Debug.Log(_sink.P + "B18 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ C 生产注册点（真建出来的那棵树）
    static void SectionC_生产注册点()
    {
        MenuCheck.Section(_sink, "C 生产注册点：主菜单里那 4 颗（原版 `Awake` 自注册 ⇒ 本仓建节点时显式登记；"
                                + "断言前先走一趟生产刷新路径把活树那几条重新登记）");

        // 🔴 R8 订正（2026-10-29 FX5 · 判据 `B94_DG2_battle_menu.md` §二·#11）：原来读 `Instance`，而它
        //   **只在 `Awake` 里赋值**、编辑模式批处理下恒 null（`Shell/MainMenuRuntime.cs:874-877` 自己记过这条坑、
        //   `:1697-1707` 就是 `InstanceOrFind()`）⇒ C0 是假红（并且它一红就让 C1–C6 从来没执行过）。改用 `InstanceOrFind()`。
        var menu = MainMenuRuntime.InstanceOrFind();
        bool hasMenu = menu != null;
        C(hasMenu, true,
          "C0 **前提断言**：宿主里有一棵常驻主菜单（`MainMenuRuntime.InstanceOrFind()` ≠ null）—— "
        + "不成立 ⇒ C1..C6 **没跑**（本文件不静默跳过：这一条会红）。推荐宿主 = `Editor/MainMenuScene.cs`");
        if (!hasMenu) return;

        // 🔴 **2026-10-29（B95_A1 · 判据 `B94_DG2_battle_menu.md` §四·2 ＋ 本轮现核源码）—— C1..C5 为什么在第一趟真跑时恒红，以及这里怎么处置**
        //   **事实（可核）**：宿主 `Editor/MainMenuScene.cs` 里有 **4 台探针 `MainMenuRuntime`** ——
        //   A374 `:1833`（`pgo`，拆在 `:2142`）＋ A855 `:16809/:16827/:16837`（`g1/g2/g3`，拆在 `:16842`），全都
        //   `AddComponent<MainMenuRuntime>()` → **`Build()`** → `DestroyImmediate`。
        //   **而 `Build()` 会登记这四条**（`Shell/MainMenuRuntime.cs:1122-1127` 三颗导航钮 + `:2848-2849` 排位模式卡），
        //   登记进的是**全局静态表** `MenuUniqueIds`（原版 `GameObjectUniqueIdentifier.Awake` 也是全局表、「后来的赢」）
        //   ⇒ 探针一 `Build()`，**活树那四条就被顶掉**；探针一 `DestroyImmediate`，表里那四条就**指向已销毁节点**
        //   （本仓的节点是**裸 `Transform`**、没有原版 `OnDestroy` 自注销那一半 —— 见 `Shell/MenuTutorialFlow.cs` 的
        //   `RegisterUnique` doc）⇒ `MenuUniqueIds.Find` 回 null ⇒ C1..C5 恒红（C6 因为两边都是 null 反而**空转绿**）。
        //   🔑 **这与生产建树对不对无关**：原版在同一份宿主下也一样 —— 探针节点的 `OnDestroy` 只摘得掉它自己那几条，
        //   活树那几条早在探针 `Awake` 的「后来的赢」里被摘走了。⇒ 归档 = **α（断言写错：它读的是一个会被宿主探针
        //   彻底改写的全局静态表，却没在断言前把活树的登记恢复回来）**，⛔ **不是** β 实现缺陷。
        //   ⇒ **处置**：断言前先走一趟**生产刷新路径** `RebuildNavForLiveOpsRefresh()`
        //   （原版 = `MainMenuWindow.RefreshSignal` → `NavigationPanelController.OnEventsRefreshed` ＋
        //   `RefreshLiveOpContainers`；本仓 `Shell/MainMenuRuntime.cs:4201` 内部复用的**正是 `Build()` 调的那两个口**
        //   —— `BuildNavigationPanel`（`:1040`）与 `_placeAllCards`（`:2517/:2615`））把活树那几条**重新登记**上去，再断言。
        //   🧨 **灭自证**：把 `BuildNavigationPanel` 末尾那三行 `RegisterUnique` / `BuildModeCard` 里那两行删掉 ⇒
        //      重建时它们同样不会被登记 ⇒ **C1..C5 照样红**（本条并没有把「登记」这件事换成「断言自己登记」）。
        if (MenuUniqueIds.Find("HomeMenuButton") == null)
            Debug.LogWarning("[B18] C 段：重建前 `HomeMenuButton` 解析不到 —— 宿主里那 4 台**探针**菜单（A374 / A855）"
                           + "建过又拆，把全局表那四条登记带成了**悬空引用**（本仓没有原版 `OnDestroy` 自注销那一半）。"
                           + "⇒ 下面走生产刷新路径重新登记（⛔ 不是「跳过断言」）。");
        menu.RebuildNavForLiveOpsRefresh();

        // ⚠️ **已知缺（出声，⛔ 不硬造）**：`CardCollectionReferenceCardPointer` 是原版那 12 个唯一 id 里**唯一**一颗
        //   本仓**没有节点**的（原版它在收藏窗的 `Reference Card Pointer`；判据 → 报告 §三）。
        //   ⇒ C 段**只断另外四条生产注册点**；不许为了让它变绿去造一颗节点（那是自设计，违反铁律 18·0）。
        if (MenuUniqueIds.Find("CardCollectionReferenceCardPointer") == null)
            Debug.LogWarning("[B18] C 段：`CardCollectionReferenceCardPointer` 未登记 —— **已知缺**：本仓没有原版那颗 "
                           + "`Reference Card Pointer` 节点（⛔ 不硬造，铁律 18·0）；C1..C6 只断另外四条。");

        // 三颗导航钮：id 与 idx 的对应 = 原版场景那三份 MonoBehaviour 的 `id` 实读（Home=0 / Collection=1 / Rewards=3）
        Tr(MenuUniqueIds.Find("HomeMenuButton") != null,
           "C1 ★ 左竖导航第 0 颗（`Home`）已按 `HomeMenuButton` 登记（`MainMenuRuntime.BuildNavigationPanel` 末尾）");
        Tr(MenuUniqueIds.Find("CollectionMenuButton") != null,
           "C2 ★ 左竖导航第 1 颗（`Collection`）已按 `CollectionMenuButton` 登记");
        Tr(MenuUniqueIds.Find("RewardsMenuButton") != null,
           "C3 ★ 左竖导航第 3 颗（`Rewards`）已按 `RewardsMenuButton` 登记");

        // 排位模式卡（`_placeAllCards` 的 `Put("Base Game Mode Container 1x2 - Ranked", …)`）
        var ranked = MenuUniqueIds.Find("RankedModeMenuButton");
        Tr(ranked != null,
           "C4 ★ 排位模式卡已按 `RankedModeMenuButton` 登记（`MainMenuRuntime.BuildModeCard` 里按卡名判）");
        // 🔴 C5 多加一个「在**这棵活树**里」：光比名字挡不住「同名的别家族件」，也挡不住「指向已销毁/别的树上的节点」
        //   （`MainMenuRuntime.cs:2830-2856` 的视口裁切会**不建**整张卡，那时 `Find` 命中的只能是别人）。
        Tr(ranked != null && ranked.name == "Base Game Mode Container 1x2 - Ranked"
                          && ranked.transform.IsChildOf(menu.transform),
           "C5 ★ 登记的那一颗**就是**排位那张模式卡（名字对得上、而且挂在本台菜单的树里 —— 不是同名的别家族件）");

        // 消费者一侧：唯一消费者 `TutorialPointerData.Target` 必须能取回**同一颗 gameObject**
        var data = new TutorialPointerData { id = "RewardsMenuButton" };
        Tr(ReferenceEquals(data.Target, MenuUniqueIds.Find("RewardsMenuButton")),
           "C6 ★ `TutorialPointerData.Target` 与 `MenuUniqueIds.Find` 取回**同一个 gameObject**（消费链路通）");
    }

    // ============================================================ A 12 个 id 的往返（登记 → 查回同一颗）
    static void SectionA_十二条id往返()
    {
        MenuCheck.Section(_sink, "A 12 个唯一 id 逐条：登记 ⇒ `Find` / `TutorialPointerData.Target` 取回**同一颗**");

        C(AllUniqueIds.Length, 12,
          "A0 **前提断言**：id 清单长度 = 12（现读实例 17 个去重；B6 判据 §6 写的「10 个」是算错，见报告 §一）");

        var made = new System.Collections.Generic.List<GameObject>();
        try
        {
            MenuUniqueIds.ClearForTest();
            C(MenuUniqueIds.Count, 0, "A1 起点：清表之后 0 条");

            for (int i = 0; i < AllUniqueIds.Length; i++)
            {
                var go = new GameObject("B18_" + AllUniqueIds[i]);
                made.Add(go);
                MenuTutorialFlow.RegisterUnique(AllUniqueIds[i], go.transform);   // 生产用的就是这一个口

                var found = MenuUniqueIds.Find(AllUniqueIds[i]);
                Tr(ReferenceEquals(found, go), "A2[" + i + "] `" + AllUniqueIds[i] + "` ⇒ `Find` 取回**登记的那一颗**");

                var pd = new TutorialPointerData { id = AllUniqueIds[i] };
                Tr(ReferenceEquals(pd.Target, go),
                   "A3[" + i + "] `" + AllUniqueIds[i] + "` ⇒ `TutorialPointerData.Target` = **同一颗 gameObject**（原版唯一消费者）");
            }

            C(MenuUniqueIds.Count, AllUniqueIds.Length,
              "A4 登记完之后表里正好 12 条（没有多登记、也没有互相顶掉）");
        }
        finally
        {
            MenuUniqueIds.ClearForTest();
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i]);
        }
    }

    // ============================================================ B 「后来的赢」/ 空 id / null 节点
    static void SectionB_顶掉旧的与空值()
    {
        MenuCheck.Section(_sink, "B `RegisterUnique`：等价原版 `Awake`（后来的赢）· 空 id 不登记 · null 节点跳过");

        GameObject a = null, b = null;
        try
        {
            MenuUniqueIds.ClearForTest();
            a = new GameObject("B18_Old");
            b = new GameObject("B18_New");

            MenuTutorialFlow.RegisterUnique("PlayRankedButton", a.transform);
            Tr(ReferenceEquals(MenuUniqueIds.Find("PlayRankedButton"), a),
               "B1 先登记 A ⇒ `Find` 回 A");

            // 原版 `Awake`：同名且旧的还 activeAndEnabled ⇒ 报重复 + 先 Remove 旧的，再 `set_Item`（**后来的赢**）
            MenuTutorialFlow.RegisterUnique("PlayRankedButton", b.transform);
            Tr(ReferenceEquals(MenuUniqueIds.Find("PlayRankedButton"), b),
               "B2 ★ 再登记 B ⇒ **后来的赢**（同原版 `Awake` 的 `Remove` + `set_Item`）");
            C(MenuUniqueIds.Count, 1, "B3 还是 1 条（顶掉，不是追加）");

            int before = MenuUniqueIds.Count;
            MenuTutorialFlow.RegisterUnique(null, b.transform);
            MenuTutorialFlow.RegisterUnique("", b.transform);
            MenuTutorialFlow.RegisterUnique("SomeId_B18", null);
            C(MenuUniqueIds.Count, before,
              "B4 空 id / null 节点 ⇒ **什么都不做**（不登记、也不抛）");

            var missing = new TutorialPointerData { id = "NoSuchId_B18" };
            Tr(missing.Target == null,
               "B5 ★ 未登记的 id ⇒ `Target` = **null**（原版那一支打 `[ERROR] Tutorial. Can't find button with ID …`；"
             + "⚠️ 原版 `Find` 的「命中但值 null」那支才是 NRE、本仓不可达 —— 见 `MenuDraw.cs` 的 `MenuUniqueIds` 段差异②）");
        }
        finally
        {
            MenuUniqueIds.ClearForTest();
            if (a != null) Object.DestroyImmediate(a);
            if (b != null) Object.DestroyImmediate(b);
        }
    }
}
