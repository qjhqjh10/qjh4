// B118W21_Assert.cs — 执行代理 `B118_W21`（第一百一十八会话 · 分诊 `B118_J10` 批 3「外壳 · 菜单 · 收藏」五条）
//
// ============================ 它盯的是哪五条 ============================
//  判据正本 = `资料/完全复刻/判据/B118_J10_215条真缺口分诊.md` §2 的 #106 / #197 / #198 / #200 / #203。
//  · A 段 **#106**（收藏筛选抽屉的 `withAnimation`）：原版 `CollectionFilterController.Toggle(bool, bool)`
//    （`decomp_generic_trial/out/CollectionFilterController_object___Toggle__15f0d30.c` 全文）有**两条**路：
//    `withAnimation = false` ⇒ `SetActive(false)` 硬切；`true` ⇒ `DOAnchorPosX(hiddenPosition.x, animationTime)`
//    + `OnComplete(SetActive(false))`。**三个调用点全传 `true`**（下面 A3 那条注释逐条列了出处）
//    ⇒ 我们只做动画那一条 = 无可观察差。本段钉住的是**动画那两支的两个数**（行程 / 时长）。
//  · B 段 **#200**：`PromptPopup.InstantiateInput` 的生产接线点 = 设置窗「兑换码」（原版 `GeneralTab.RedeemCode`
//    → `PromptContext{Title, Inputs=[Code]}`）。本段钉 `PromptContext.InputData` 四格 + 三条词条真的解得开。
//  · C 段 **#197**：`LiveopUIDrawer_GameModeIcon`（原版按事件 `PlayModes` 从 `GameModesIconCollection` 取图）。
//    重点盯 **`!HasValue` 与「值不在表里」是两档**（前者取 default，后者返回 null）—— 灭自证就在 C4。
//  · D 段 **#198**：`NavigationPanelController : …, IInitializationComplete`（`dump.cs:110267`）。
//  · E 段 **#203**：场景过渡层（`TransitionUIAnchor.EnableAnchor/DisableAnchor` + `SimpleSceneTransition` 那两跳）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A：把 `FltHiddenDx` 改回 `-165`（= 原版 `originalAnchorPosition.x`，**不是行程**）⇒ A1 红；
//       把 `FltAnimTime` 改成 0 ⇒ A2 红。
//  · C：把 `IconFileFor` 的 `default` 支改成「回 default 图标」⇒ **C4 红**（原版 `GetIcon` 在 `HasValue`
//       但表里没有时返回 **null**：`GameModesIconCollection__GetIcon.c` 的 `uVar4 = 0` 那一跳）。
//       把 `DefaultGameModeIconFile` 改成 classic ⇒ C1 红（`GameModesIconCollection.asset` 的 `defaultIcon` = skirmish）。
//  · D：把类声明里的 `IInitializationComplete` 删掉 ⇒ D1 红（⚠️ D2 仍绿，所以两条要一起看）。
//  · E：把 `EndTransitionAnchor` 里的 `UnregisterAnchorOverride()` 删掉 ⇒ E2 红；把那段 `localScale = one`
//       删掉 ⇒ E4 红（原版 `:148-152` 那一跳）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接在 `CardPresentation/Editor/ShellScene.cs` 的**方法尾聚合行**（`Debug.Log(P + $"=== 合计：…` 之前）
//  **同一物理行追加**（行数零变化 —— 那条聚合行本身以 `//` 结尾，追加必须落在**第一个 `//` 之前**）：
//      B118W21_Assert.Run(); _sink.Pass += B118W21_Assert.Pass; _sink.Fail += B118W21_Assert.Fail;
//  ⚠️ E 段要求宿主里**有 `WindowsManager`**（`ShellScene` 满足）。在别的宿主里跑会**红一条 E0**（那是
//  前提硬检查，⛔ 不是静默跳过）—— 这是故意的：前提不成立时必须看得见。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B118_W21`：批 3 五条（#106 抽屉动画两格 · #200 兑换码输入 · #197 模式图标抽屉 · #198 初始化接口 · #203 过渡层）。</summary>
public static class B118W21_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B118W21] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>唯一的入口（宿主 `ShellScene` 的聚合行里调）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B118_W21：批 3 五条（#106 / #197 / #198 / #200 / #203）");
        A_FilterDrawerAnimation();
        B_RedeemInput();
        C_GameModeIconDrawer();
        C2_HeaderGameModeIconNodes();
        D_InitializationComplete();
        E_SceneTransitionLayer();
    }

    // ============================================================ A · #106 收藏筛选抽屉（动画那两支的两个数）
    static void A_FilterDrawerAnimation()
    {
        // 原版 `CollectionFilterController` 的 prefab 实值（`bundle_menus_assets_all` 的 `Card Filters` 那一族）：
        //   `hiddenPosition.x = −550` · `originalAnchorPosition.x = −165` ⇒ **行程 385**
        //   （本仓把这两格折成一个「行程」常量：`Shell/CollectionWindow.cs:588` 那段注释写了为什么）。
        //   `animationTime = 0.3`（`CollectionFilterController` 序列化字段；本仓 `FltAnimTime` 逐字相同）。
        N(CollectionWindow.FltHiddenDx, -385f, 0.001f,
          "A1 `FltHiddenDx` = 原版行程 −550 −(−165) = −385（⛔ 不是 −165 那个绝对锚点值）");
        N(CollectionWindow.FltAnimTime, 0.3f, 0.0001f,
          "A2 `FltAnimTime` = 原版 `CollectionFilterController.animationTime` = 0.3s");
        // A3 = 判据核对（**不是**代码断言，写在日志里方便复查）：三个调用点全传 `withAnimation: true` ⇒ 硬切那条不可达。
        //   ① `CollectionDisplay<object>.ToggleFilters(bool)` → `(state, 1)`：`decomp_generic_trial/out/CollectionDisplay_object___ToggleFilters__15eec60.c`
        //      实读 `(**(code **)(*plVar1 + 0x178))(plVar1, param_2, 1, …)` —— **第三个实参是 1**。
        //   ② `DeckEditingWindow.cs:763` `Toggle(option: false, withAnimation: true)`。
        //   ③ `DeckEditingWindow.cs:1017` `Toggle(option: true, withAnimation: true)`。
        //   全库再没有第 4 个（`grep -n "\.Toggle(" Card*FilterController.cs / CollectionDisplay.cs / DeckEditingWindow.cs`
        //   逐文件看过；其余 `.Toggle(` 命中的都是别的类同名的 `Toggle`）。
        T(true, "A3 判据：原版 `CollectionFilterController.Toggle` 的 3 个调用点**全传 withAnimation=true**"
              + "（ToggleFilters / DeckEditingWindow ×2）⇒ 我们只有动画一条路 = 无可观察差（细节见本函数注释）");
    }

    // ============================================================ B · #200 兑换码输入（`InstantiateInput` 的生产接线）
    static void B_RedeemInput()
    {
        SettingsWindow.ResetRedeemProbeForTest();
        C(SettingsWindow.RedeemSubmitCount, 0, "B1 兑换码探针归零（重复跑不串上一次读数）");
        C(SettingsWindow.LastRedeemCodeForTest, null, "B2 归零之后没有残留的提交值");

        // 原版 `GeneralTab.RedeemCode` 的 `InputData` 四格（`.c` + ILSpy 逐字）：
        //   Name = "Code" · Label = `Settings/General/RedeemCodePlaceholder` · ContentType = Standard · DefaultValue = null
        var d = new PromptContext.InputData("Code", "Settings/General/RedeemCodePlaceholder",
                                            TMPro.TMP_InputField.ContentType.Standard, null);
        C(d.Name, "Code", "B3 输入格 Name = 原版字面量 `Code`（`TryRedeemCode` 就是拿它去 `result.Data[\"Code\"]`）");
        C(d.Label, "Settings/General/RedeemCodePlaceholder", "B4 输入格 Label = 原版 mTerm（占位字词条）");
        C(d.ContentType, TMPro.TMP_InputField.ContentType.Standard, "B5 ContentType = Standard（原版 `ContentType.Standard`）");
        C(d.DefaultValue, null, "B6 DefaultValue = null（原版 `.c` 里那一格就是 0）");

        // 三条词条必须**解得开**（⛔ 不许把键名印到玩家眼前 —— 这是本项目反复踩的那条）。
        C(Loc.T("Settings/General/RedeemCodePrompt") != "Settings/General/RedeemCodePrompt", true,
          "B7 标题词条 `Settings/General/RedeemCodePrompt` 在表里（否则屏上会印键名）");
        C(Loc.T("Settings/General/RedeemCodePlaceholder") != "Settings/General/RedeemCodePlaceholder", true,
          "B8 占位字词条 `Settings/General/RedeemCodePlaceholder` 在表里");
        C(Loc.T("MainMenu/General/OK") != "MainMenu/General/OK", true,
          "B9 Ok 钮词条 `MainMenu/General/OK` 在表里（原版 `Settings/General/OK` 那条注里点名的同一颗钮）");
        T(PromptPopup.InRowH > 0f, "B10 输入行高 > 0（= 原版 `inputs/template` 的 90；`PromptPopup.InRowH` 那条链活着）");
    }

    // ============================================================ C · #197 模式图标抽屉（PlayModes → 图标）
    static void C_GameModeIconDrawer()
    {
        // `GameModesIconCollection.asset` 实读：`0 → 40k_gamemode_icon_classic` · `13 → 40k_gamemode_icon_skirmish`
        // · `defaultIcon = 40k_gamemode_icon_skirmish`。`GetIcon` 的形状（`__GetIcon.c`）：`!HasValue` ⇒ default；
        // `HasValue` ⇒ 只在表里找，**找不到返回 null**。
        C(LiveopUIDrawer_GameModeIcon.IconFileFor(null), CardArt.DefaultGameModeIconFile,
          "C1 没有模式值 ⇒ 取 `defaultIcon`（= 原版 SO 实读那张）");
        C(CardArt.DefaultGameModeIconFile, "40k_gamemode_icon_skirmish",
          "C2 `defaultIcon` 就是 **skirmish** 那张（⛔ 不是 classic —— SO 实读如此，别「修正」）");
        C(LiveopUIDrawer_GameModeIcon.IconFileFor(PlayModes.Classic), "40k_gamemode_icon_classic",
          "C3 `PlayModes.Classic(0)` ⇒ `40k_gamemode_icon_classic`");
        C(LiveopUIDrawer_GameModeIcon.IconFileFor(PlayModes.Skirmish), "40k_gamemode_icon_skirmish",
          "C3b `PlayModes.Skirmish(13)` ⇒ `40k_gamemode_icon_skirmish`");
        C(LiveopUIDrawer_GameModeIcon.IconFileFor(PlayModes.Duel), null,
          "C4 🔴灭自证：`Duel(1)` **不在** SO 那张表里 ⇒ 原版 `GetIcon` 返回 **null**（⛔ 不是回 default 顶替）");
        T(CardArt.MenuUi("40k_gamemode_icon_classic") != null && CardArt.MenuUi("40k_gamemode_icon_skirmish") != null,
          "C5 两张模式图真的在工程里（`Resources/Art/ui_menu/`）—— 否则这条抽屉画不出任何东西");

        var drawer = new LiveopUIDrawer_GameModeIcon(null);
        drawer.Initialize(null);   // 原版三处早退之一：`icon`/事件为空 ⇒ return（我们出声，但不抛）
        C(drawer.CurrentPlayEvent, null, "C6 `Initialize(null)` 早退：`currentPLayEvent` 仍是 null（原版 `:13` 第一跳）");
        C(drawer.InitCallsForTest, 1, "C7 `Initialize` 被调过 1 次（证明上面那条走的是真方法，不是没进去）");
    }

    // ============================================================ C2 · 窗头 `Game Mode Icon` 节点与抽屉接线
    //  ✅ 已复刻（2026-11-05 · B146-W17 · 判据 = 三份 prefab 的 `Header Background/Game Mode Icon`：
    //    `RankedEventWindowV2`（`m_Color.a=1`）/ `SkirmishModeEventWindow`（a=1）/ `Two Sides Event Window`（a=0），
    //    三份都是 `100×100` · `m_Sprite=0` · `PA=0` · 父 = `Header Background`；且三份 prefab 的
    //    `WindowHeaderWithBackButton.eventUIDrawers` 各**一格** `LiveopUIDrawer_GameModeIcon`。
    //  🔴 灭自证：把 `LiveOpsEventWindow` / `ParentEventWindow` 的 `HeaderDrawers()` 传参删掉 ⇒ C9c / C16c / C23c 红；
    //    把 Parent 的 `m_Color.a=0` 改成 1 ⇒ C23j 红；把同名节点换成裸 `Node`（不建 ImageQuad）⇒ C9g 红。
    //  ⚠️ 本段要建三扇事件窗（`Create(null)` 即可，⛔ 不写存档），收尾 `DestroyImmediate` + 还原 `LastOpened`。
    static void C2_HeaderGameModeIconNodes()
    {
        var keepLast = LiveOpsEventWindow.LastOpened;
        var keepParentLast = ParentEventWindow.LastOpened;
        SkirmishEventWindow sk = null; RankedEventWindow rk = null; ParentEventWindow pe = null;
        try
        {
            sk = SkirmishEventWindow.Create(null);
            T(sk != null, "C8 前提：`SkirmishEventWindow.Create(null)` 建出窗");
            if (sk != null) { sk.Open(); CheckHeaderIcon(sk.transform, "C9", "遭遇战", "40k_gamemode_icon_skirmish"); }

            rk = RankedEventWindow.Create(null);
            T(rk != null, "C16 前提：`RankedEventWindow.Create(null)` 建出窗");
            if (rk != null) { rk.Open(); CheckHeaderIcon(rk.transform, "C17", "排位", "40k_gamemode_icon_classic"); }

            pe = ParentEventWindow.Create(null);
            T(pe != null, "C23 前提：`ParentEventWindow.Create(null)` 建出窗");
            if (pe != null) { pe.Build(); CheckHeaderIcon(pe.transform, "C24", "Two Sides", null); }
        }
        catch (System.Exception e)
        {
            T(false, "C2 段不应抛异常（实抛：" + e.GetType().Name + "：" + e.Message + "）");
        }
        finally
        {
            // 先 `Close()` 再销毁：`RankedEventWindow.Close()` 负责把导航侧栏还原（它 `Open()` 里藏过），
            // `LiveOpsEventWindow.Close()` 负责撤掉搜索/到期刷新那两个登记。⛔ 直接销毁会留下半截状态。
            TryClose(sk); TryClose(rk); TryClose(pe);
            if (sk != null) Object.DestroyImmediate(sk.gameObject);
            if (rk != null) Object.DestroyImmediate(rk.gameObject);
            if (pe != null) Object.DestroyImmediate(pe.gameObject);
            LiveOpsEventWindow.LastOpened = keepLast;
            // `ParentEventWindow.LastOpened` 是 public get / **private set** ⇒ 直接赋值编不过，走反射还原
            // （⚠️ 不还原会让 `MainMenuScene` 里那几条读 `LastOpened` 的断言摸到已销毁的实例）。
            var pi = typeof(ParentEventWindow).GetProperty("LastOpened",
                         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var set = pi != null ? pi.GetSetMethod(true) : null;
            if (set != null) set.Invoke(null, new object[] { keepParentLast });
        }
    }

    /// <summary>三扇窗共用的那一套：节点在 / 尺寸 100×100 / 图对 / 节点自己是 Image / 抽屉一格且指向同一颗。
    /// <paramref name="wantArt"/> 为 null = `Two Sides` 那一档（原版 `m_Color.a = 0` ⇒ 不可见，但**节点必须激活**）。</summary>
    static void CheckHeaderIcon(Transform root, string id, string who, string wantArt)
    {
        var hdr = FindDeep(root, "Game Mode Header With Back Button");
        T(hdr != null, id + " 前提：" + who + " 窗有 `Game Mode Header With Back Button`");
        var plate = hdr != null ? hdr.Find("Header Background") : null;
        var node = plate != null ? plate.Find("Game Mode Icon") : null;
        T(node != null, id + "a " + who + " 窗头有 `Header Background/Game Mode Icon`（原版父链）");
        var comp = hdr != null ? hdr.GetComponent<WindowHeaderWithBackButton>() : null;
        T(comp != null, id + "b 前提：窗头挂 `WindowHeaderWithBackButton`");
        if (comp != null)
        {
            T(comp.eventUIDrawers != null && comp.eventUIDrawers.Length == 1,
              id + "c `eventUIDrawers` = **1 格**（原版 prefab 那一格；null/0 ⇒ 抽屉没接上）");
            if (comp.eventUIDrawers != null && comp.eventUIDrawers.Length == 1)
                T(comp.eventUIDrawers[0] is LiveopUIDrawer_GameModeIcon,
                  id + "d 那一格是 `LiveopUIDrawer_GameModeIcon`（原版组件类型）");
        }
        if (node == null) return;
        // ⚠️ 世界档 `ImageQuad` 的宿主是**裸 `Transform`**（`ImageQuad.Create` 的 `new GameObject(name)`）⇒ ⛔ 别取 `RectTransform`（会抛 `MissingComponentException`，`B150` #10）。
        var q = node.GetComponent<ImageQuad>();
        T(q != null, id + "g 图标节点**自己就是**那颗 `Image`（原版 Image 挂在这一颗上；裸 `Node` 不行）");
        if (q == null) return;   // ⛔ 不静默跳过：上面那条已把「节点上没有 ImageQuad」报红
        N(q.WorldW * 108f, 100f, 0.5f, id + "e 图标宽 = 原版 **100** 设计 px（`ImageQuad.WorldW × 108`，不是 `rect.width`）");
        N(q.WorldH * 108f, 100f, 0.5f, id + "f 图标高 = 原版 **100** 设计 px");
        if (wantArt != null)
        {
            var want = CardArt.MenuUi(wantArt);
            T(want != null, id + "h 前提：工程里有 `" + wantArt + "` 那张图");
            T(q.Texture == want, id + "i 图标画的正是 `" + wantArt + "`（= 原版 `GameModesIconCollection` 那张，⛔ 不是占位白图）");
        }
        else
        {
            T(q.Tint.a <= 0.001f, id + "j Two Sides：图标 α = **0**（原版 `m_Color.a = 0` ⇒ 出厂不可见）");
            T(node.gameObject.activeSelf, id + "k 但**节点是激活的**（⛔ 别整颗 `SetActive(false)` —— 会把子件 `tooltip trigger` 一起藏掉）");
            T(node.Find("tooltip trigger") != null, id + "l 子件 `tooltip trigger` 在（原版那一颗 75.4255×71.6789）");
        }
        if (comp != null && comp.eventUIDrawers != null && comp.eventUIDrawers.Length == 1)
        {
            var drawer = comp.eventUIDrawers[0] as LiveopUIDrawer_GameModeIcon;
            if (drawer != null)
                T(drawer.icon == q, id + "m 抽屉的 `icon` 指向**同一颗** `ImageQuad`（⛔ 不是另建一颗 = 画了看不见）");
        }
    }

    /// <summary>关窗（收尾用）。异常只出声、不掀宿主 —— 本段是断言，不是产品路径。</summary>
    static void TryClose(GameWindow w)
    {
        if (w == null) return;
        try { w.Close(); }
        catch (System.Exception e) { Debug.LogWarning("[B118W21] C2 收尾 `Close()` 抛异常（不影响断言读数）：" + e.GetType().Name + "：" + e.Message); }
    }

    /// <summary>按名字递归找第一个后代（`Transform.Find` 只认**直接相对路径** ⇒ `Two Sides` 的窗头埋在页签底下，必须递归）。</summary>
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

    // ============================================================ D · #198 `IInitializationComplete`
    static void D_InitializationComplete()
    {
        C(typeof(IInitializationComplete).IsAssignableFrom(typeof(MainMenuRuntime)), true,
          "D1 `MainMenuRuntime : IInitializationComplete`（= 原版 `NavigationPanelController : …, IInitializationComplete`）");
        var mi = typeof(MainMenuRuntime).GetMethod("OnInitializationComplete",
                   System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        T(mi != null, "D2 真有公开实例方法 `OnInitializationComplete()`（接口实现）");
        if (mi != null)
        {
            C(mi.ReturnType, typeof(void), "D3 返回 `void`（原版签名 `void OnInitializationComplete()`）");
            C(mi.GetParameters().Length, 0, "D4 无参（同上）");
        }
        // `ready` 那道闸的读口存在（= 原版 `+0x60`）—— 新实例上必须是 false 之外没法断言（实例要建场景），
        // 这里只钉「读口在、类型对」。
        var pi = typeof(MainMenuRuntime).GetProperty("NavEventButtonsReadyForTest");
        T(pi != null && pi.PropertyType == typeof(bool), "D5 `ready` 读口在（`NavEventButtonsReadyForTest`，类型 bool）");
    }

    // ============================================================ E · #203 场景过渡层
    static void E_SceneTransitionLayer()
    {
        // E0 前提（**硬检查**，⛔ 不是静默跳过）：本段要在有壳的宿主里跑。
        var wm = WindowsManager.Instance;
        T(wm != null, "E0 前提：场上有 `WindowsManager`（本段必须在 `ShellScene` 这类有壳的宿主里跑）");

        // E1 `EndTransitionAnchor(null)`：原版这一跳会 NRE（`FUN_1803f47a0`），我们出声后返回。
        WindowsManager.EndTransitionAnchor(null);
        T(!WindowsManager.TransitionActiveForTest && !WindowsManager.HasAnchorOverride,
          "E1 `EndTransitionAnchor(null)` 之后：`anchorOverride` 已解、过渡态已清（原版此处 NRE；我们出声后照旧收尾）");

        if (wm == null) return;    // E0 已经红过一条；后面几条依赖实例（⛔ 不是静默跳过 —— 前提那条会红）

        // E2 `BeginTransitionAnchor` = 原版 `TransitionUIAnchor.EnableAnchor()`（`anchorOverride = this.transform`）。
        var anchor = WindowsManager.BeginTransitionAnchor();
        T(anchor != null && WindowsManager.TransitionActiveForTest && WindowsManager.HasAnchorOverride,
          "E2 `BeginTransitionAnchor()` 之后：有锚点节点 · 过渡态真 · `anchorOverride` 已指向它（= `EnableAnchor`）");

        // E3/E4 一次过渡里被挂进来的窗，`End` 之后要**归位 + 复位 P/S**（原版 `DisableAnchor` 的 `:103-152`）。
        var pp = PromptPopup.Create(wm, "B118W21 过渡探针", "OK", null, null, null);
        if (pp != null && anchor != null)
        {
            pp.transform.SetParent(anchor, false);          // = 过渡期间 `AttachToAnchor` 走 override 的结果
            pp.transform.localScale = new Vector3(2f, 2f, 1f);   // 让「复位」这一步有东西可复位
            pp.transform.localPosition = new Vector3(3f, 4f, 0f);

            WindowsManager.EndTransitionAnchor(anchor);

            T(!WindowsManager.TransitionActiveForTest && !WindowsManager.HasAnchorOverride,
              "E3 `EndTransitionAnchor(root)` 之后：过渡态清、`anchorOverride` 解掉（原版第一跳 `UnregisterAnchorOverride`）");
            T(pp.transform.parent != anchor,
              "E3b 窗已经**离开**过渡锚点（原版 `set_parentInternal(GetWindowAnchor(placement))`）");
            N(pp.transform.localScale.x, 1f, 1e-4f, "E4 `localScale` 复位到 1（原版 `:148-152` 的 `.one`）");
            N(pp.transform.localPosition.x, 0f, 1e-4f, "E5 `localPosition.x` 复位到 0（原版 `:126-131` 的 `.zero`）");
            N(pp.transform.localPosition.y, 0f, 1e-4f, "E5b `localPosition.y` 复位到 0");
            Object.DestroyImmediate(pp.gameObject);
        }
        else
        {
            T(false, "E3 前提：`PromptPopup.Create` / 过渡锚点建不出来 ⇒ 归位那三条**没跑**（不许静默）");
        }
    }
}
