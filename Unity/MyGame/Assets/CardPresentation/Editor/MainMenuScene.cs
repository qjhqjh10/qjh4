// MainMenuScene.cs — 主菜单场景（`MainMenu.unity`）的**建 / 自检 / 存盘**入口
//
// 用法：
//   … -executeMethod MainMenuScene.Run              自检（结构 + 截图），退出码 0 = 全过
//   … -executeMethod MainMenuScene.BuildAndSaveScene 建出场景存盘（给人打开按 Play 用）
//
// 施工图：`资料/主菜单_原版规格.md`（§二 层×参数 · **§五 绝对坐标** · §三② 出厂/运行时分界）。
// 🔴 每条断言都写了**它盯的是哪个原版值 + 出处** —— 不许拿我们自己写的常量断言我们自己写的常量。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;        // `DeckStore`/`DeckLibrary`/`CardDatabase`（战斗入口那一段要用）
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainMenuScene
{
    const string P = "[Menu] ";
    const string ScenePath = "Assets/CardPresentation/Scenes/MainMenu.unity";
    const string ShotDir = "d:/4/_tmp_view/menu";

    static int _pass, _fail;
    static readonly List<string> _failures = new List<string>();

    static void Section(string t) { Debug.Log(P + $"--- {t} ---"); }

    static void Check<T>(T got, T want, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(got, want)) { _pass++; Debug.Log(P + $"   ✓ {msg}"); }
        else
        {
            _fail++;
            var line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
            _failures.Add(line);
            Debug.LogError(P + $"   ✗ {line}");
        }
    }

    static void CheckTrue(bool c, string msg) { Check(c, true, msg); }
    /// <summary>文本比对 + 取一段文字（战斗入口那段要断文案）。</summary>
    static void CheckText(string got, string want, string msg)
        => CheckTrue(got == want, $"{msg} —— 实测「{got}」，期望「{want}」");
    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.Text : null;
    }

    static void CheckNear(float got, float want, float tol, string msg)
        => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F3} ≈ {want:F3}±{tol:F3}）");

    /// <summary>世界坐标比对（±0.01 世界单位 ≈ ±1 px）。</summary>
    static void CheckAt(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = MainMenuRuntime.Center(x1, x2, y1, y2);
        var got = t.localPosition;
        float d = Vector3.Distance(got, want);
        CheckTrue(d <= 0.01f,
                  $"{what} 在 §五 给的矩形中心（差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    // ============================================================ 建场景

    static MainMenuRuntime Build(out Transform root)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 相机（外壳那边也有一台，`DontDestroyOnLoad` 会带过来；这里建一台是为了
        // **单独打开本场景按 Play 也能看** —— 两台的参数一致，见 `ShellScene.Build`）
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.aspect = LayoutSpace.DesignAspect;   // ⚠️ 必须在建任何东西之前定死（批处理默认 4:3）
        LayoutSpace.Apply(cam);

        var rootGo = new GameObject("MainMenu");
        var rt = rootGo.AddComponent<MainMenuRuntime>();
        rt.Build();
        root = rootGo.transform;
        return rt;
    }

    static void Shoot(string file)
    {
        var cam = Camera.main;
        if (cam == null) return;
        const int W = 1920, H = 1080;
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
    }

    // ============================================================ 自检

    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Directory.CreateDirectory(ShotDir);
        Debug.Log(P + "=== 主菜单自检 开始 ===");

        var menu = Build(out var root);

        Section("整屏背景（§五 A：`Image(sprite=0)` + 双色渐变，**不是图也不是 3D**）");
        var bg = menu.Find("Background");
        CheckTrue(bg != null, "`Background` 建了");
        if (bg != null)
        {
            var q = bg.GetComponentInChildren<ImageQuad>();
            CheckTrue(q != null && q.Texture != null && q.Texture.name.StartsWith("grad_"),
                      "背景用的是**运行时生成的双色渐变**（`CardArt.Gradient`，c1/c2/angle 照 `MB1931`）");
            if (q != null) CheckNear(q.WorldH, LayoutSpace.DesignHeight, 0.01f, "背景铺满可见高度（10 个世界单位）");
        }

        Section("左竖导航骨架（§五 C：`Navigation Panel` 0..191 · 分隔线 71..1080）");
        var nav = menu.Find("Navigation Panel");
        CheckTrue(nav != null, "`Navigation Panel` 建了");
        CheckAt(FindChild(nav, "Background"), -164.4f, 165.1f, 0.1f, 1145.9f, "侧栏 `Background`（`White Square`）");
        CheckAt(FindChild(nav, "Separators Left"), -2.5f, 0.3f, 71.0f, 1080.0f, "`Separators Left`");
        CheckAt(FindChild(nav, "Separators Right"), 164.0f, 166.8f, 71.0f, 1080.0f, "`Separators Right`");
        CheckAt(FindChild(nav, "Panel Shadow"), -762.5f, 1166.2f, 511.7f, 581.7f, "`Panel Shadow`");

        Section("5 个导航按钮（§五 C：**y 是按 VLG 算的**，spacing −16.35 / padTop −5）");
        CheckNavButton(menu, "Home",       143.4f, 313.1f, "40k_main_bt_play");
        CheckNavButton(menu, "Collection", 296.7f, 466.4f, "40k_main_bt_collection");
        CheckNavButton(menu, "Shop",       450.1f, 619.8f, "40k_main_bt_shop");
        CheckNavButton(menu, "Rewards",    603.4f, 773.1f, "40k_main_bt_rewards");
        CheckNavButton(menu, "Social",     756.7f, 926.4f, "40k_main_bt_friends");
        CheckAt(FindChild(menu.Find("Main Menu Navigation Button - Home"), "Selected highlight"),
                -0.3f, 164.0f, 143.4f, 313.1f, "**选中态高亮**在 `Home` 的整按钮矩形上（原版 5 个出厂都 active）");

        Section("顶栏（§五 B：`Upper bar` 0..100 · 齿轮 1803..1891 · 三个钮**按 HLG 算**）");
        var bar = menu.Find("Upper bar");
        CheckTrue(bar != null, "`Upper bar` 建了");
        CheckAt(FindChild(bar, "Background"), -11.7f, 1920f, 0f, 71.3f, "顶栏 `Background`（`UI_Main_Upper bar`）");
        CheckAt(FindChild(FindChild(bar, "SettingsBtn"), "Image"), 1803.1f, 1890.9f, 4.6f, 66.4f, "`SettingsBtn` 齿轮");
        CheckAt(FindChild(FindChild(bar, "TopBarButtons"), "Image"), 425.3f, 480.3f, 15.5f, 55.5f,
                "`InboxBtn`（HLG 算的值，**不是 JSON 的 397.8**）");
        CheckAt(FindChild(FindChild(bar, "TopBarButtons"), "Challenge button"), 490.1f, 537.6f, 11.8f, 59.2f,
                "`Challenge button`（HLG 算的值）");
        CheckTrue(FindChild(bar, "Feedback Button") == null,
                  "`Feedback Button` **没建**（原版出厂 `activeSelf=False`，§二 表 #10）");
        var resBar = FindChild(bar, "Resources Bar");
        CheckTrue(resBar != null && resBar.GetComponentInChildren<ImageQuad>() == null,
                  "`Resources Bar` **没有背景图**（原版那一格没有 Image —— 我们第一版自加过一层浅灰药丸，已纠）");

        var prof = FindChild(bar, "Player Profile");
        CheckAt(FindChild(prof, "Background"), 23.0f, 411.0f, 11.6f, 135.6f, "玩家信息块底（`40k_main_player frame`）");
        CheckAt(FindChild(prof, "Player Name"), 136.9f, 401.9f, 13.7f, 61.7f, "`Player Name` 文字位");
        CheckAt(FindChild(prof, "Planer Name Background"), 25.6f, 472.1f, 14.9f, 60.5f, "名字条底");
        CheckAt(FindChild(FindChild(prof, "Avatar Item Small"), "Border"), -10.0f, 165.5f, 9.0f, 139.1f,
                "头像金框（**§五 B 已把 scl 1.25 算进去**；`chain_rect` 给的是未缩放值）");

        Section("右侧聊天预览（§五 D：1475..1875 × 85..145）");
        var chat = menu.Find("ChatPreview");
        CheckTrue(chat != null, "`ChatPreview` 建了");
        CheckAt(FindChild(chat, "Container"), 1474.7f, 1847.0f, 85f, 145f, "聊天底板（`Closed-Chat_background`）");
        CheckAt(FindChild(chat, "Button"), 1811.0f, 1879.0f, 81.8f, 148.3f, "聊天入口钮（`40K_icon_menu_chat`）");

        Section("模式卡区（§五 E 容器 + §九 卡结构）");
        var modes = menu.Find("GameModes");
        CheckTrue(modes != null, "`GameModes` 容器建了");
        var content = FindChild(modes, "Content");
        CheckTrue(content != null, "`Viewport/Content` 建了（原版出厂 0 子，靠 liveop 数据灌）");
        CheckCard(menu, "Base Game Mode Container 1x1 - Tutorial", 205f, 535f, 414.4f, "Tutorial（1x1）");
        CheckCard(menu, "Draft Game Mode Container 1x2", 760f, 535f, 848.8f, "Draft（1x2）");

        // 🆕 2026-09-24「战斗入口」的三张模式卡 —— **用户拍板：模式卡就是入口**
        //    （「是直接点击这些卡片，然后就进去这些对应模式的界面的」）。
        //    🔴 原版这张「模式 → 卡图 → 窗」的映射在 **liveop 服务端**（本地查不到、正本也写着「别自己编」）
        //    ⇒ **这三张 + 它们点开哪扇窗，都是我们定的**（逐条记在 `资料/阶段二_战斗入口_原版规格.md` §〇/§五）。
        Section("战斗入口：三张模式卡（**入口是我们定的**，见 `资料/阶段二_战斗入口_原版规格.md`）");
        CheckCard(menu, "Base Game Mode Container 1x1 - Practice", 205f + 2 * 555f, 535f, 414.4f, "Practice（1x1）");
        CheckCard(menu, "Base Game Mode Container 1x1 - Skirmish", 205f + 3 * 555f, 535f, 414.4f, "Skirmish（1x1）");
        CheckCard(menu, "Base Game Mode Container 1x1 - Ranked", 205f + 4 * 555f, 535f, 414.4f, "Ranked（1x1）");
        {
            var pc = menu.Find("Base Game Mode Container 1x1 - Practice");
            var ph = FindChild(pc, "Hit");
            var pwb = ph != null ? ph.GetComponent<WindowButton>() : null;
            CheckTrue(pwb != null && pwb.onClick != null, "练习卡有**点击区**（`WindowButton`）");
            CheckTrue(FindChild(menu.Find("Base Game Mode Container 1x1 - Tutorial"), "Hit") == null,
                      "Tutorial 卡**没有**点击区（它还不是入口 —— 那一扇窗还没建）");
            // ⚠️ **别碰玩家的真存档**：`DeckStore.OverridePath` 先指到临时文件（同 `CollectionScene` 的规矩）
            DeckStore.OverridePath = "d:/4/_tmp_view/menu/_menu_test_decks.json";
            try { System.IO.File.Delete(DeckStore.OverridePath); } catch { }
            CollectionData.ResetForTest();
            {
                var lib = DeckLibrary.Load();
                for (int i = 0; i < 3; i++) lib.Create("菜单测试卡组 " + (i + 1));
                // 给第 0 套塞督军 + 几张部队（`Battle!` 没有督军会**如实拒绝**，那样验不出开战那条）
                var pool = CardDatabase.Load();
                var d0 = lib.Decks[0];
                foreach (var c in pool) if (c.Type == "hero") { d0.WarlordId = c.Id; break; }
                int added = 0;
                foreach (var c in pool) if (c.Type == "unit" && added < 4) { d0.CardIds.Add(c.Id); added++; }
                lib.Save();
            }
            CollectionData.ResetForTest();
            // 主菜单自检原来**没有 `WindowsManager`**（主菜单原版也是挂在壳里跑的）——
            // 战斗入口这条路要开窗 ⇒ 这里补一个宿主（`EnsureHost` 会连**指针层**一起建好）
            WindowsManager.EnsureHost(menu.transform);
            if (pwb != null) pwb.Click();
            var pw = PracticeModePopup.LastOpened;
            CheckTrue(pw != null, "点练习卡 ⇒ **开出了 `Practice Mode Menu`**");
            if (pw != null)
            {
                Check(pw.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                Check(pw.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                CheckNear(pw.extraScaleSmallScreen, 1.07f, 1e-4f,
                          "`extraScaleSmallScreen` = **1.07**（⚠️ 别的窗多是 1.0 —— **逐窗实测**）");
                CheckAt(FindChild(pw.transform, "Deck info"), 638.38f, 1842.38f, 78.79f, 863.77f, "`Deck info` 红底");
                CheckAt(FindChild(pw.transform, "Army Selector"), 69.42f, 246.54f, 182.18f, 880.17f, "`Army Selector`");
                CheckAt(FindChild(pw.transform, "Decks Scroll view"), 261.28f, 634.88f, 262.64f, 803.43f,
                        "`Decks Scroll view`");
                CheckAt(FindChild(pw.transform, "Back Bg"), 215.76f, 280f, 892.86f, 956.10f, "`Back` 圆钮");
                CheckText(TextOf(FindChild(pw.transform, "Battle Text")), "Battle!", "开战钮文案 = `Battle!`");
                CheckText(TextOf(FindChild(pw.transform, "tooltip")), "Select deck to play", "`tooltip` 文案");
                CheckText(TextOf(FindChild(pw.transform, "Toggle Label")), "Game mode", "`Game mode` 开关文案");
                CheckTrue(pw.DeckRows.Count > 0, $"卡组列表画了 {pw.DeckRows.Count} 行");
                CheckTrue(pw.ArmyCells.Count > 0, $"阵营列画了 {pw.ArmyCells.Count} 格（13 个阵营，可纵向滚）");
                // ⚠️ 开窗时选中的是 `DeckLibrary.Current`（= 新建的**第 3 套，空的**）⇒ 这里先把第 1 套点上再断
                {
                    var r0a = FindChild(pw.transform, "DeckRow_0");
                    var h0a = r0a != null ? FindChild(r0a, "Hit") : null;
                    var w0a = h0a != null ? h0a.GetComponent<WindowButton>() : null;
                    CheckTrue(w0a != null, "第 1 套卡组那一行有点击区");
                    if (w0a != null) w0a.Click();
                    Check(pw.DeckIndex, 0, "点第 1 套 ⇒ 选中它");
                    CheckTrue(pw.CardRows.Count > 0,
                              $"卡列表画了 {pw.CardRows.Count} 行（第 1 套里塞了督军 + 4 张部队）");
                    // 🔴 实拍抓的：卡名会**超过 231px 的格宽**、撞进右边那一列（`Death Spinner Warp Spider`）
                    //    ⇒ 现在断「每一行的名字放得进格子里」（量的是 **Label 自己量出来的宽**）
                    int over = 0; float widest = 0f;
                    foreach (var row in pw.CardRows)
                    {
                        var nl = row.GetComponentInChildren<Label>();
                        if (nl == null) continue;
                        float wpx = nl.WorldW * 108f;
                        widest = Mathf.Max(widest, wpx);
                        if (wpx > PracticeModePopup.DlCellW + 0.5f) over++;
                    }
                    Check(over, 0, $"卡列表**每一行的卡名都放得进 {PracticeModePopup.DlCellW}px 的格**（最宽 {widest:F1}px；"
                                   + "判据是**原版格宽**，不是我们自己的常量 —— 第一版撞列就是这条没断）");
                }
                Check(PracticeModePopup.ListCols, 2, "卡列表列数 = **2** = floor((569.79 + 22) ÷ (231 + 22))（照 GridLayoutGroup 算）");
                // 换一套卡组（验「选中态 + 卡列表跟着换」）
                //   ⚠️ 第 2 套是**空卡组**（只有名字、没督军没卡）⇒ 「卡列表 0 行 + `Battle!` 如实拒绝」**都对**，
                //      这正是本轮要验的两条**反面**判据；验完再点回第 1 套去开战。
                if (pw.DeckRows.Count > 1)
                {
                    var r1 = pw.DeckRows[1];
                    var h1 = FindChild(r1, "Hit");
                    var w1 = h1 != null ? h1.GetComponent<WindowButton>() : null;
                    if (w1 != null) w1.Click();
                    Check(pw.DeckIndex, 1, "点第 2 套卡组 ⇒ 选中的换成它");
                    Check(pw.CardRows.Count, 0, "换到**空卡组** ⇒ 卡列表 0 行（跟着换了，不是没刷新）");
                    var bh0 = pw.BtHit;
                    var bwb0 = bh0 != null ? bh0.GetComponent<WindowButton>() : null;
                    if (bwb0 != null) bwb0.Click();
                    CheckTrue(!pw.StartedBattle, "**没有督军的卡组 ⇒ `Battle!` 如实拒绝**（不许静默开局）");
                    // ⚠️ 那个提示窗是**模态**的，不关掉会把后面那张主菜单截图盖住（第一版就是这样）
                    foreach (var pp in Object.FindObjectsByType<PromptPopup>(FindObjectsSortMode.None))
                        if (pp != null) pp.Close();
                    var r0 = FindChild(pw.transform, "DeckRow_0");
                    var h0 = r0 != null ? FindChild(r0, "Hit") : null;
                    var w0 = h0 != null ? h0.GetComponent<WindowButton>() : null;
                    if (w0 != null) w0.Click();
                    Check(pw.DeckIndex, 0, "点回第 1 套 ⇒ 选中回来");
                    CheckTrue(pw.CardRows.Count > 0, "卡列表也跟着回来了");
                }
                var bh = pw.BtHit;
                var bwb = bh != null ? bh.GetComponent<WindowButton>() : null;
                CheckTrue(bwb != null, "`Battle!` 有点击区");
                if (bwb != null) bwb.Click();
                CheckTrue(pw.StartedBattle, "点 `Battle!` ⇒ **开战成立**（真机上这一步 `LoadScene(\"Battle\")`）");
                Check(CollectionData.CurrentIndex(), pw.DeckIndex,
                      "开战前**把选中的那套交给 `DeckLibrary`**（`BattleDriver.PickSavedDeck` 读的就是它）");
                Shoot("02_练习模式窗.png");

                // ---------------- `Deck Selection Popup with Tabs`（2026-09-24）----------------
                // 出入口 = 练习窗 → `Show Deck Content`（翻抽屉）→ `Change Deck`
                //（= 原版 `DeckGeneralInfoDemo.ChangePlayerDeckButton` → `OpenWindow(new DeckSelectionContext(...))`）
                Section("`Deck Selection Popup with Tabs`（正本 `阶段二_战斗入口_原版规格.md` 的反向入口 · A1 §3 逐节点）");
                pw.ToggleDeckInfo();
                var cdHit = FindChild(pw.transform, "ChangeDeckHit");
                var cdBtn = cdHit != null ? cdHit.GetComponent<WindowButton>() : null;
                CheckTrue(cdBtn != null, "`Change Deck` 有点击区（在 `Show Deck Content` 翻开的抽屉里）");
                if (cdBtn != null)
                {
                    cdBtn.Click();
                    var ds = PracticeModePopup.LastDeckSelection;
                    CheckTrue(ds != null, "点 `Change Deck` ⇒ **开出了 `Deck Selection Popup with Tabs`**");
                    if (ds != null)
                    {
                        Check(ds.type, WindowType.Popup, "`type` = **1 Popup**（A1 §3 原文）");
                        Check(ds.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**（原文）");
                        Check(ds.closeOnEsc, true, "`closeOnESC` = **1**（原文）");
                        Check(DeckSelectionPopup.Cols, 6,
                              "列数 = **6** = floor(1565 ÷ (225 + 20))（`_controlSegmentSize=1` ⇒ 按宽度算）");
                        CheckNear(DeckSelectionPopup.PadX, 57.5f, 0.1f,
                                  "内容居中左边距 = **57.5** = (1565 − (6×225 + 5×20)) / 2");
                        var svh = FindChild(ds.transform, "Deck Scroll View");
                        CheckAt(svh, 194.50f, 1759.50f, 208.63f, 986.69f,
                                "`Deck Scroll View`（RSR 视口 **1565 × 778.06**）");
                        Check(ds.ShownCount, CollectionData.DeckCount(),
                              $"起手在「我的卡组」那一页 ⇒ 列出全部 {CollectionData.DeckCount()} 套");
                        if (ds.Cells.Count > 0)
                        {
                            var c0 = ds.Cells[0];
                            // 世界坐标 → 画布 px（同 `CollectionScene` 的 `PxOf/PxYOf`；本文件没有那两个助手）
                            CheckNear(c0.position.x * 108f + 960f, 364.5f, 0.7f,
                                      "第 1 格中心 x = **364.5**（194.50 + pad 57.5 + 225/2）");
                            CheckNear(540f - c0.position.y * 108f, 390.88f, 0.7f,
                                      "第 1 格中心 y = **390.88**（208.63 + 364.5/2）");
                        }
                        // 抽一套（原版 `RandomizeDeck`：从当前列表随机挑 ⇒ **选中并关窗**）
                        var rnd = ds.RandomHit;
                        var rndBtn = rnd != null ? rnd.GetComponent<WindowButton>() : null;
                        CheckTrue(rndBtn != null, "`Random` 有点击区");
                        if (rndBtn != null)
                        {
                            rndBtn.Click();
                            Check(ds.CurrentState, WindowState.Closed, "点 `Random` ⇒ **关窗**（原版 `Select` = 关窗 + 回调）");
                            CheckTrue(pw.DeckIndex >= 0 && pw.DeckIndex < CollectionData.DeckCount(),
                                      $"回调把选中的那套交回练习窗（现在是第 {pw.DeckIndex + 1} 套：「"
                                      + CollectionData.DeckAt(pw.DeckIndex).Name + "」）");
                        }
                        // 再开一次，验：搜索 / 预组那一页的空态 / 点一格选中 / 关闭钮
                        var ds2 = pw.OpenDeckSelection();
                        if (ds2 != null)
                        {
                            CheckTrue(ds2.SearchHit != null, "搜索框有点击区（**出厂 act=N，我们照常显示** —— 出声的偏离）");
                            ds2.Search = "1";
                            ds2.RebuildListNow();
                            CheckTrue(ds2.ShownCount > 0 && ds2.ShownCount < CollectionData.DeckCount(),
                                      $"搜索串「1」⇒ 列表从 {CollectionData.DeckCount()} 套降到 **{ds2.ShownCount}** 套（真筛得动）");
                            ds2.Search = "zzz-不可能命中";
                            ds2.RebuildListNow();
                            Check(ds2.ShownCount, 0, "搜一个不存在的名字 ⇒ 0 套");
                            CheckTrue(ds2.transform.Find("Empty Note") != null
                                      && ds2.transform.Find("Empty Note").gameObject.activeSelf,
                                      "**空态那行字露出来了**（我们自己加的 —— 原版没有，用来如实说明为什么空）");
                            ds2.Search = "";
                            ds2.RebuildListNow();
                            var tabPre = ds2.TabHit(false);
                            var tabPreBtn = tabPre != null ? tabPre.GetComponent<WindowButton>() : null;
                            CheckTrue(tabPreBtn != null, "「预组卡组」页签有点击区");
                            if (tabPreBtn != null)
                            {
                                tabPreBtn.Click();
                                Check(ds2.OwnDecks, false, "点它 ⇒ 切到**预组**那一页");
                                Check(ds2.ShownCount, 0,
                                      "预组那一页 **0 套** —— 数据没接（**如实留空，不编数据**；见 `DeckSelectionPopup` 文件头）");
                                CheckTrue(ds2.EmptyText.Contains("预组卡组的数据还没接"),
                                          "空态那行字**说清了原因**：" + ds2.EmptyText);
                                ds2.TabHit(true).GetComponent<WindowButton>().Click();
                                Check(ds2.OwnDecks, true, "切回「我的卡组」");
                            }
                            Shoot("03_选卡组弹窗.png");
                            var cls = ds2.CloseHit;
                            var clsBtn = cls != null ? cls.GetComponent<WindowButton>() : null;
                            if (clsBtn != null) clsBtn.Click();
                            Check(ds2.CurrentState, WindowState.Closed, "点关闭圆钮 ⇒ 窗关上");
                        }
                    }
                }
                pw.ToggleDeckInfo();      // 抽屉收回去（后面那张练习窗的截图不该带抽屉）
                var bk = pw.BackHit;
                var bkb = bk != null ? bk.GetComponent<WindowButton>() : null;
                if (bkb != null) bkb.Click();
                Check(pw.CurrentState, WindowState.Closed, "点 `Back` ⇒ 窗关上");
            }
        }

        Section("图：一张都不能少");
        Check(menu.MissingArt.Count, 0, "没有取不到的图（取不到的件**根本没画**，所以这条必须 0）");

        Section("染色与字号（§七：**原版靠 `Image.m_Color` 把亮图染暗**，不补就会渲成白块）");
        CheckTint(FindChild(nav, "Panel Shadow"), new Color(0f, 0f, 0f, 0.46667f), 0.002f,
                  "`Panel Shadow` 染成半透明纯黑（`m_Color (0,0,0,0.4667)`）");
        var sep = FindChild(nav, "Separators Left");
        CheckTint(sep, new Color(0.64706f, 0.38039f, 0.26275f, 1f), 0.002f, "分隔线染成古铜色");
        CheckTint(FindChild(prof, "Planer Name Background"), new Color(0.39623f, 0.19251f, 0.30954f, 1f), 0.002f,
                  "名字条底染成暗紫红");
        var shadowInner = FindChild(nav, "Background");
        var navQuad = shadowInner != null ? shadowInner.GetComponentInChildren<ImageQuad>() : null;
        CheckTrue(navQuad != null && !IsAllWhiteVerts(navQuad),
                  "侧栏底板用了**四角顶点色**（原版 `MB1941`：只补 `m_Color` 补不出这块板）");
        Check(CountByName(root, "Selected highlight"), 1,
              "`Selected highlight` **只有 1 个**（= 选中态；原版 5 个出厂都 active，可见性由 Toggle 驱动）");
        // ⚠️ 开了 autosize 之后**具体多大是 TMP 自己算的**（原版那个 32 也只是「框里的存量」）⇒ 断言要比**区间**
        CheckFontInRange(FindChild(prof, "Player Name"), 10f, 32f, "`Player Name` 字号落在原版 autosize 区间 10→32 内");
        CheckFont(FindChild(chat, "Message Preview"), 18f, "聊天两行字号 = 18px（`auto=0`，不缩）");
        CheckFits(FindChild(prof, "Player Name"), 265f, "`Player Name` 放得进 265px 的框");
        CheckFits(FindChild(chat, "Message Preview"), 327.3f, "聊天行放得进 327.3px 的框");
        // 导航标签：原版 autosize 18→33，框 146.9px（`COLLECTION` 靠自适应缩小 —— 原版存的就是 30.45）
        CheckFits(FindChild(menu.Find("Main Menu Navigation Button - Home"), "Text"), 146.92f, "`PLAY` 放得进 146.9px 的条");
        CheckFits(FindChild(menu.Find("Main Menu Navigation Button - Collection"), "Text"), 146.92f,
                  "`COLLECTION` 放得进 146.9px 的条（**这条就是 autosize 的判据**）");
        CheckFits(FindChild(menu.Find("Base Game Mode Container 1x1 - Tutorial"), "Event Title"), 513.7f,
                  "模式卡标题放得进 513.7px 的框");
        MeasureText(FindChild(prof, "Player Name"), "Player Name", 32f);
        MeasureText(FindChild(chat, "Message Preview"), "Player Name: Message", 18f);
        MeasureText(FindChild(menu.Find("Base Game Mode Container 1x1 - Tutorial"), "Event Title"),
                    "TUTORIAL", 58.8f);
        MeasureText(FindChild(menu.Find("Main Menu Navigation Button - Home"), "Text"), "PLAY", 33f);
        MeasureText(FindChild(menu.Find("Main Menu Navigation Button - Collection"), "Text"), "COLLECTION", 30.45f);
        Shoot("01_主菜单.png");
        Debug.Log(P + menu.Dump());
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (_fail > 0) foreach (var f in _failures) Debug.LogError(P + "   ✗ " + f);
        EditorApplication.Exit(_fail > 0 ? 1 : 0);
    }

    static void CheckNavButton(MainMenuRuntime menu, string name, float y1, float y2, string art)
    {
        var b = menu.Find("Main Menu Navigation Button - " + name);
        if (b == null) { CheckTrue(false, $"导航钮 `{name}` 建了"); return; }
        // ⚠️ 按钮那层是**纯容器**（没有 rect 语义，位置在原点）；带 §五 那个矩形的是它的子件。
        //    而 `Selected highlight` **只有选中的那个按钮才画**（2026-09-22 起）⇒ 这里探**五个都有**的两件。
        CheckAt(FindChild(b, "Text Background"), 9.3f, 156.2f, y1 + 118.3f, y1 + 157.7f, $"导航钮 `{name}` 的文案条");
        CheckAt(FindChild(b, "Badge Highlight"), 117.8f, 152.8f, y1 + 91.7f, y1 + 126.7f, $"导航钮 `{name}` 的红点");
        var img = FindChild(b, "Image");
        var q = img != null ? img.GetComponentInChildren<ImageQuad>() : null;
        CheckTrue(q != null && q.Texture != null && q.Texture.name == art, $"`{name}` 用的是 `{art}`");
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>
    /// **量**一段文字渲出来到底多大（不猜）。原版给的是 `m_fontSize`（画布像素）——
    /// 我们想知道「按 `fontSize = px/108` 摆出来，实际占多少像素」。
    /// 判据：**大写高应当 ≈ `fontSize × TmpFont.WorldCapPerFontSize`**；宽按字符数自带。
    /// </summary>
    static void MeasureText(Transform t, string what, float origFontPx)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, $"量文字：`{what}` 节点不在"); return; }
        Debug.Log(P + $"  【量】`{what}`：原版 em {origFontPx}px ⇒ 我们实际 {lb.FontPxNow:F2}px"
                  + $" · 宽 {lb.WorldW * 108f:F1}px · (TMP fontSize {lb.FontSize:F4})");
        // ⚠️ 只信 `GlyphHeightWorld`（汉字高 ≈ 1 em，`TmpFont` 实测换算）与 `WorldW`（TMP 的 textBounds）。
        //    **`CapHeightWorld` 在只调过 `SetFontSize` 时是无效的**（走「按档位估」的兜底路）—— 第一版就被它骗了。
    }

    /// <summary>比 `ImageQuad` 的染色（原版 `Image.m_Color`）。</summary>
    static void CheckTint(Transform t, Color want, float tol, string what)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var got = q.Tint;
        CheckTrue(Mathf.Abs(got.r - want.r) <= tol && Mathf.Abs(got.g - want.g) <= tol
                  && Mathf.Abs(got.b - want.b) <= tol && Mathf.Abs(got.a - want.a) <= tol,
                  $"{what}（实得 {got.r:F3},{got.g:F3},{got.b:F3},{got.a:F3}）");
    }

    /// <summary>比 TMP 的字号（世界单位；原版值是**画布像素** ⇒ 期望值传 `px/108f`）。</summary>
    static void CheckFont(Transform t, float wantPx, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（节点不在）"); return; }
        // 🔴 比 **`FontPxNow`（像素口径的实际生效字号）** —— 别用 `CapHeightWorld`/`GlyphHeightWorld`：
        //    那两个是**回读传入值**的伪测量（`已知的坑.md`「两个属性看着像测量」那条），拿它断言 = 自证。
        CheckNear(lb.FontPxNow, wantPx, 0.6f, what);
    }

    /// <summary>字号落在原版 autosize 的 [min,max] 里（开了 auto 之后**具体值由 TMP 算**，不能断言等于某个数）。</summary>
    static void CheckFontInRange(Transform t, float minPx, float maxPx, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（节点不在）"); return; }
        float px = lb.FontPxNow;
        CheckTrue(px >= minPx - 0.1f && px <= maxPx + 0.1f, $"{what}（实得 {px:F2}px）");
    }

    /// <summary>**渲染宽度必须放得进框** —— 这条才是能抓住「autosize 没生效」的断言
    /// （第一版 `COLLECTION` 宽 212px 却摆在 146.9px 的条里，画面溢出而自检全绿）。</summary>
    static void CheckFits(Transform t, float boxPx, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（节点不在）"); return; }
        float w = lb.WorldW * 108f;
        CheckTrue(w <= boxPx + 1f, $"{what}（渲出 {w:F1}px ≤ 框 {boxPx:F1}px）");
    }

    /// <summary>四角顶点色是不是全白（= 没用顶点色）。</summary>
    static bool IsAllWhiteVerts(ImageQuad q)
    {
        var mf = q.GetComponent<MeshFilter>();
        var m = mf != null ? mf.sharedMesh : null;
        if (m == null || m.colors == null || m.colors.Length < 4) return true;
        foreach (var c in m.colors)
            if (Mathf.Abs(c.r - 1f) > 0.01f || Mathf.Abs(c.g - 1f) > 0.01f || Mathf.Abs(c.b - 1f) > 0.01f) return false;
        return true;
    }

    static int CountByName(Transform root, string name)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) n++;
        return n;
    }

    /// <summary>
    /// 一张模式卡：**只断言有原版出处的三项** —— 列的左边界（`205 + 555c`，§二·4 实证）、宽高（`itemSize` 535×414.4 实证）。
    /// 🔴 **行 y 不断言** —— 它是运行时算的、本地取不到（§五 E 的 `?`），我们取的值**没有原版出处**，断言它就等于自证。
    /// </summary>
    static void CheckCard(MainMenuRuntime menu, string name, float colLeft, float w, float h, string what)
    {
        var card = menu.Find(name);
        if (card == null) { CheckTrue(false, $"模式卡 {what} 建了"); return; }
        var bg = FindChild(card, "Background Image");
        var q = bg != null ? bg.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) { CheckTrue(false, $"{what} 的卡图建了"); return; }
        CheckNear(q.WorldW, w / 108f, 0.01f, $"{what} 卡宽 = {w}px（`itemSize.x` 实证）");
        CheckNear(q.WorldH, h / 108f, 0.01f, $"{what} 卡高 = {h}px（`itemSize.y` 实证）");
        var wantX = MainMenuRuntime.Center(colLeft, colLeft + w, 0f, 0f).x;
        CheckNear(q.transform.localPosition.x, wantX, 0.01f, $"{what} 在第 {colLeft}px 起的那一列（列左 = 205+555c 实证）");
        // 🔴 **分层顺序**（§10·3 找茬点 4）：卡图必须**大于**整屏渐变的队列 ——
        //    两者 z 都是 0，同队列时谁盖谁**不确定**（第一版 Tutorial 卡就是这么被渐变盖住的）。
        var bgQuad = menu.Find("Background") != null ? menu.Find("Background").GetComponentInChildren<ImageQuad>() : null;
        if (bgQuad != null)
        {
            int qa = q.GetComponent<MeshRenderer>().sharedMaterial.renderQueue;
            int qb = bgQuad.GetComponent<MeshRenderer>().sharedMaterial.renderQueue;
            CheckTrue(qa > qb, $"{what} 卡图的渲染队列（{qa}）**大于**整屏渐变的（{qb}）");
        }
    }

    // ============================================================ 存场景

    /// <summary>建出场景存盘，给人打开按 Play 用。**顺带把它加进 Build Settings** —— 壳要能 `LoadScene("MainMenu")`。</summary>
    public static void BuildAndSaveScene()
    {
        Directory.CreateDirectory(ShotDir);
        var menu = Build(out var root);

        if (root == null || root.GetComponent<MainMenuRuntime>() == null)
        {
            Debug.LogError(P + "✗ 场景里没挂 `MainMenuRuntime` —— 按 Play 会是一片空");
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        Debug.Log(P + $"  场景 {ScenePath}");

        AddToBuildSettings(ScenePath);
        Shoot("00_主菜单.png");
        Debug.Log(P + menu.Dump());
    }

    /// <summary>把场景加进 `EditorBuildSettings`（幂等）—— 否则 `SceneManager.LoadScene("MainMenu")` 会抛。</summary>
    static void AddToBuildSettings(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) { Debug.Log(P + "  （已在 Build Settings 里）"); return; }
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log(P + "  已加进 Build Settings（壳要靠它 `LoadScene`）");
    }
}
