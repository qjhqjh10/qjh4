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

    /// <summary>同 `CheckAt`，但比**世界坐标**（`t.position`）—— 给「挂在别的层底下」的节点用。
    /// 🔴 `CheckAt` 比的是 `localPosition`，只对**窗口根的直接子件**成立（那些件的 local 恰好等于页坐标）；
    /// 子件在 `Deck info/General container` 这种层里时，`local` 是相对父节点的 ⇒ 必须走世界坐标。</summary>
    static void CheckAtWorld(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = MainMenuRuntime.Center(x1, x2, y1, y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f,
                  $"{what}（世界坐标差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>量一个节点**渲出来**的像素矩形（1920×1080 · 左上原点）。
    /// 用 `Label`/`ImageQuad` 自己算出来的 `WorldW/WorldH` + 节点世界坐标反算 ——
    /// **不抄源码常量**（§10·3 第 1 层那条：必须量渲染真值）。</summary>
    static bool RenderedRect(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        if (t == null) return false;
        float w, h;
        var lb = t.GetComponentInChildren<Label>();
        if (lb != null) { w = lb.WorldW * 108f; h = lb.WorldH * 108f; }
        else
        {
            var q = t.GetComponentInChildren<ImageQuad>();
            if (q == null) return false;
            w = q.WorldW * 108f; h = q.WorldH * 108f;
        }
        float cx = LayoutSpace.PxX(t.position.x), cy = LayoutSpace.PxY(t.position.y);
        x1 = cx - w * 0.5f; x2 = cx + w * 0.5f;
        y1 = cy - h * 0.5f; y2 = cy + h * 0.5f;
        return true;
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
                CheckAt(FindChild(pw.transform, "Deck info"), 638.38f, 1842.38f, 78.79f, 863.77f, "`Deck info` 容器");
                // 🔴 **2026-09-24 结构订正（`项目任务.md` §三 第 53 条）** —— 从根节点实读出来的真结构，逐条断：
                //    ① 红底在**子件** `Generic Window Red Background Big` 上（`Deck info` 自己**没有图**）
                //    ② `Background Info` 是**【空容器】+ act=0**（正本把好几件挂在它下面是错的）
                //    ③ `Deck Name`/`Warlord Name`/`Army Image`/`General container`/`Deck List Drawer` 直挂 `Deck info`
                {
                    var info = FindChild(pw.transform, "Deck info");
                    var bgBig = FindChild(info, "Generic Window Red Background Big");
                    CheckAtWorld(bgBig, 761.63f, 1831.93f, 187.00f, 869.00f,
                                 "红底在 `Generic Window Red Background Big` 上（761.63,187.00→1831.93,869.00 —— **不是** `Deck info` 那整块 1204×784.98）");
                    CheckTrue(bgBig != null && bgBig.GetComponentInChildren<ImageQuad>() != null,
                              "红底那一层**真的画出来了**（有 `ImageQuad`，不是空节点）");

                    var bgInfo = FindChild(info, "Background Info");
                    CheckTrue(bgInfo != null, "`Background Info` 建了（照原版留着）");
                    CheckTrue(bgInfo != null && !bgInfo.gameObject.activeSelf,
                              "`Background Info` **出厂 act=0**（照原版）");
                    Check(bgInfo != null ? bgInfo.childCount : -1, 0,
                          "`Background Info` 是**【空容器】**（实读一条子件都没有 —— 它**不是** `Warlord Name` 那几件的父）");

                    foreach (var nm in new[] { "Deck Name", "Warlord Name", "Army Image",
                                               "General container", "Deck List Drawer" })
                    {
                        var t = FindChild(info, nm);
                        CheckTrue(t != null && t.parent == info, $"`{nm}` 是 **`Deck info` 的直接子件**");
                    }
                    var gen0 = FindChild(info, "General container");
                    var lore = FindChild(info, "Lore Text");
                    var chgT = FindChild(info, "Change Deck Text");
                    CheckTrue(lore == null,
                              "`Lore Text` **没画**（原版喂的是 `DemoDeckInfoSO.Lore` = **卡组**简介；我们的卡组没这个字段，"
                              + "拿督军的效果文字顶上去会**和 `Change Deck` 叠** —— 见 `BuildGeneralContainer` 的注释）");
                    CheckTrue(chgT != null && gen0 != null && chgT.parent == gen0,
                              "`Change Deck` 挂在 **`General container`** 下（不是 `Background Info`）");
                }
                // **出厂态 = 总览**：原版 `DeckGeneralInfoDemo.SetContent` 末尾
                //   `generalInfoContainer.SetActive(true)` + `cardsInDeckPanel.SetActive(false)`
                //   （`PracticeModePopup__DeckSelected.c` 是开窗/换卡组的必经之路）。
                {
                    var gen = FindChild(pw.transform, "General container");
                    var dl = FindChild(pw.transform, "Deck List Drawer");
                    CheckTrue(pw.ShowingGeneralInfo, "出厂显示的是**总览**（`General container` 开着）");
                    CheckTrue(gen != null && gen.gameObject.activeSelf && dl != null && !dl.gameObject.activeSelf,
                              "两个抽屉**互斥**：`General container` 开、`Deck List Drawer` 关（`SetContent` 那两句）");
                }
                Check(PracticeModePopup.ListCols, 2,
                      "卡列表列数 = **2** = floor((526.10 − 22 + 22) ÷ 253)（原版 `GridLayoutGroup`：`constraint=0/Flexible` ⇒ 按宽算）");
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

                    // 🔴 **第 53 条的判据**（`项目任务.md` §三）：`Deck Name` / `Warlord Name` 与**卡列表**不叠。
                    //    量的是**渲出来的矩形**（`Label.WorldW/H` + 世界坐标反算 ⇒ 1920×1080 像素），**不是源码常量**。
                    //    原版这两个抽屉互斥、且 `Content` 起 y **318.42** —— 两个视图下这条都该成立。
                    {
                        float a1, b1, a2, b2, c1, d1, c2, d2;
                        bool okName = RenderedRect(FindChild(pw.transform, "Deck Name"), out a1, out b1, out a2, out b2);
                        bool okWl = RenderedRect(FindChild(pw.transform, "Warlord Name"), out c1, out d1, out c2, out d2);
                        float rowTop = float.MaxValue, rowBot = float.MinValue;
                        foreach (var row in pw.CardRows)
                        {
                            float e1, f1, e2, f2;
                            if (RenderedRect(row, out e1, out f1, out e2, out f2))
                            { rowTop = Mathf.Min(rowTop, f1); rowBot = Mathf.Max(rowBot, f2); }
                        }
                        CheckTrue(okName && okWl && rowTop < float.MaxValue,
                                  "`Deck Name` / `Warlord Name` / 卡列表三者都能量到渲染矩形");
                        if (okName && okWl && rowTop < float.MaxValue)
                        {
                            float nameBot = Mathf.Max(b2, d2);
                            CheckTrue(nameBot <= rowTop + 0.5f,
                                      $"**第 53 条的判据**：`Deck Name`/`Warlord Name` 渲出来的底边 **{nameBot:F1}px** "
                                      + $"**不越过**卡列表顶边 **{rowTop:F1}px**（原版 `Content` 起 y=318.42）");
                            CheckTrue(rowBot <= PracticeModePopup.DlContentB + 0.5f,
                                      $"卡列表渲出来的底边 {rowBot:F1}px 不超过 `Content` 下沿 {PracticeModePopup.DlContentB}px");
                        }
                    }
                }
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
                // 🔴 **2026-09-24 起 `Battle!` 走原版那条链**：`StartMatch`（开 `Searching Oponent Popup` 等 12s）
                //    → 等不到真人 → `StartBotBattle`。等待秒数 = 原版 `GetTimeToWaitForOpponent` 的常量支
                //    （`DAT_1834b3160` 读出来 = 12；轮询间隔 `DAT_1834b2bb8` = 1s）。
                {
                    var sp = FindChild(pw.transform, "Searching Oponent Popup");
                    CheckTrue(sp != null, "`Searching Oponent Popup` 建了（四窗共用 `SearchingMatchPopup`）");
                    CheckTrue(sp != null && sp.gameObject.activeSelf,
                              "点 `Battle!` ⇒ **匹配窗先出来**（原版 `MatchMakerManager.StartMatch`）");
                    CheckTrue(!pw.StartedBattle, "12 秒**还没到** ⇒ 还没开局（不抢跑）");
                    pw.TickSearch(9f);          // 打字机「Searching」9 个字 × 0.5s = 4.5s，这里一并推过去
                    CheckTrue(!pw.StartedBattle, "推到第 9 秒 ⇒ 仍未开局");
                    pw.TickSearch(3f);          // 满 12 秒
                    CheckTrue(sp == null || !sp.gameObject.activeSelf, "等满 12 秒 ⇒ 匹配窗自己关掉");
                }
                CheckTrue(pw.StartedBattle, "点 `Battle!` + 等满 12 秒 ⇒ **开战成立**（真机上这一步 `LoadScene(\"Battle\")`）");
                Check(CollectionData.CurrentIndex(), pw.DeckIndex,
                      "开战前**把选中的那套交给 `DeckLibrary`**（`BattleDriver.PickSavedDeck` 读的就是它）");
                Shoot("02_练习模式窗.png");

                // ---------------- `Deck info Popup`（2026-09-24 实读订正的那条入口）----------------
                // 原版：`Show Deck Content Button` = `DeckGeneralInfoDemo.cardInDeckInfoButton`
                //   → `CardInDeckInfoButtonOnClick → WindowsManager.OpenWindow(new DeckInfoContext(deck, 2, …))`
                //   🔴 **它不是抽屉开关**（正本原来把它当开关，那是错的 —— 见 `PracticeModePopup` 文件头）。
                Section("`Deck info Popup`（练习窗 `Show Deck Content` 那条 · 原版 `DeckInfoContext(deck, 2, …)`）");
                var sdHit = FindChild(pw.transform, "ShowDeckHit");
                var sdBtn = sdHit != null ? sdHit.GetComponent<WindowButton>() : null;
                CheckTrue(sdBtn != null, "`Show Deck Content` 有点击区");
                if (sdBtn != null)
                {
                    sdBtn.Click();
                    CheckTrue(PracticeModePopup.LastDeckInfo != null, "点它 ⇒ **开出了 `Deck info Popup`**");
                }
                // ⚠️ 它是模态窗，留着会盖住后面的截图 —— 断完就关（同 `PromptPopup` 那条）
                if (PracticeModePopup.LastDeckInfo != null) PracticeModePopup.LastDeckInfo.Close();

                // ---------------- `Deck List Drawer`（原版出厂关着的那个抽屉）----------------
                // 开关只有 `DeckGeneralInfoDemo.Toggle(bool)` 一个入口，而它**在本地全量反编译里找不到调用者**
                //   （`grep -l DeckGeneralInfoDemo__Toggle *.c` 只命中它自己）⇒ 如实记着，别自己给玩家编一个入口。
                Section("`Deck List Drawer`（原版出厂关着的抽屉 · 开关 = `Toggle(bool)`）");
                CheckTrue(pw.ToggleDeckInfo() == false, "切一次 ⇒ 落到**卡列表**那一侧");
                {
                    var gen = FindChild(pw.transform, "General container");
                    var dl = FindChild(pw.transform, "Deck List Drawer");
                    CheckTrue(gen != null && !gen.gameObject.activeSelf && dl != null && dl.gameObject.activeSelf,
                              "切完两个抽屉还是**互斥**（总览关、卡列表开）—— 原版 `Toggle` 那两句");
                    CheckTrue(FindChild(dl, "ShowInfoHit") != null,
                              "卡列表那一侧有自己的钮（原版 `Show Deck General Info button`，点它切回总览）");
                    Shoot("02b_练习窗_卡列表抽屉.png");     // 给下个会话留一张「另一个抽屉」的实拍
                }
                CheckTrue(pw.ToggleDeckInfo(), "再切一次 ⇒ 回到**总览**（`Change Deck` 就在这一侧）");
                var cdHit = FindChild(pw.transform, "ChangeDeckHit");
                var cdBtn = cdHit != null ? cdHit.GetComponent<WindowButton>() : null;
                CheckTrue(cdBtn != null, "`Change Deck` 有点击区（在 `General container` 里）");
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
                        // 🔴 **2026-09-26 起手页签归位**：原版 `DeckSelectionTabController.Start()` 唯一一句
                        //    就是 `ShowPrebuiltDecks(true)` ⇒ 出厂落在**预组**页。下面这一段原来是在「我的卡组」
                        //    页上验几何/抽签的 ⇒ **先切过去**再照旧。
                        Check(ds.OwnDecks, false, "起手落在**预组卡组**页（原版 `Start()` → `ShowPrebuiltDecks(true)`）");
                        ds.SwitchTab(true);
                        Check(ds.ShownCount, CollectionData.DeckCount(),
                              $"切到「我的卡组」⇒ 列出全部 {CollectionData.DeckCount()} 套");
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
                        // 再开一次，验：起手页签 / 预组那一页 / 搜索 / 点一格选中 / 关闭钮
                        var ds2 = pw.OpenDeckSelection();
                        if (ds2 != null)
                        {
                            // ---- ① 起手页签：**原版出厂就是「预组」页**（`DeckSelectionTabController__Start.c:5`）----
                            Check(ds2.OwnDecks, false, "开窗**起手落在「预组卡组」页**（原版 `Start()` → `ShowPrebuiltDecks(true)`）");

                            // ---- ② 预组页的内容：**两种模式都列** + 拼得齐 + 按原版难度序 ----
                            var tab = PrebuiltDecks.Tab;
                            CheckTrue(PrebuiltDecks.Available, "预组数据读到了（`Resources/prebuilt_decks.json`）");
                            Check(tab.Count, ds2.ShownCount, "预组页列出的条数 = `PrebuiltDecks.Tab` 的条数");
                            CheckTrue(tab.Count > 0, "预组页**不是空的**了（原来恒 0，数据没接）—— 现在 " + tab.Count + " 副");
                            int badMode = 0, badComplete = 0, badZh = 0, badSort = 0, badNoCbName = 0;
                            var ownCbMissing = new List<string>();     // 这副牌**自己的**卡背图没有
                            var noArtAtAll = new List<string>();       // 连「阵营默认卡背」兜底都没有 ⇒ 真画不出来
                            for (int i = 0; i < tab.Count; i++)
                            {
                                var d = tab[i];
                                // 🆕 2026-09-26：原来这条是「必须 == 0（只列经典）」——
                                //   现在**两种模式都列**（引擎支持遭遇模式之后照原版全列），
                                //   所以判据改成「只能是这两个合法值」，别的一个都不许混进来。
                                if (d.gameMode != 0 && d.gameMode != (int)GameMode.Skirmish) badMode++;
                                if (!d.complete) badComplete++;
                                if (string.IsNullOrEmpty(d.nameZh)) badZh++;
                                if (string.IsNullOrEmpty(d.cardback)) badNoCbName++;
                                if (CardArt.Cosmetic(d.cardback) == null) ownCbMissing.Add(d.deckId);
                                if (d.Cardback == null) noArtAtAll.Add(d.deckId);
                                if (i > 0)
                                {
                                    var p = tab[i - 1];
                                    if (p.difficulty > d.difficulty ||
                                        (p.difficulty == d.difficulty && p.armyOrder > d.armyOrder)) badSort++;
                                }
                            }
                            Check(badMode, 0, "预组页里只有两种合法模式（`gameMode` 0 = 经典 / 13 = 遭遇）"
                                            + "—— 12 张那批 2026-09-26 起**列出来了**（引擎支持了，不再是出声的偏离）");
                            Check(badComplete, 0, "预组页里**每个都拼得齐**（`complete`）—— 对应原版那句 `Where(!HasHiddenCards)`");
                            Check(badZh, 0, "每个都有**中文名**（没有的回落英文原名）");
                            Check(badNoCbName, 0, "每个都有**卡背名**（`cardback` 列 88 副 + 由 `cardbackId` 查表补的 15 副）");
                            Check(badSort, 0, "排序 = **`difficulty` 升序 → `CardArmy` 升序**（原版 `OrderBy`/`ThenBy` 两个键都升序）");
                            // 🔴 这两条**把已知缺口钉住**（不是断言 0 —— 那张图本地确实没有）。名单一变就红，必须回来看。
                            //    出处 → `资料/预组卡组_原版规格.md` §六 欠账那条。
                            // 🔴 2026-09-26：本页从 29 副（只经典）扩到 66 副（经典 + 遭遇）时，
                            //    原来躲在「不显示」后面的 6 条卡背命名缺口一起露了出来 —— 已全部解决
                            //    （5 条按命名规律接别名 + 1 条同阵营替身，见 `gen_prebuilt_decks.py` 的那两张表）。
                            //    ⇒ 现在这两条是**不许再有缺口**。
                            ownCbMissing.Sort(); noArtAtAll.Sort();
                            Check(ownCbMissing.Count, 0,
                                  $"{tab.Count} 副**统统**取得到自己那张卡背图 —— 实得 " +
                                  (ownCbMissing.Count == 0 ? "（一副都不缺）" : string.Join("、", ownCbMissing.ToArray())));
                            Check(noArtAtAll.Count, 0,
                                  "**画不出卡背的一副都没有** —— 实得 " +
                                  (noArtAtAll.Count == 0 ? "（一副都不缺）" : string.Join("、", noArtAtAll.ToArray())));
                            var subs = new List<string>();
                            for (int i = 0; i < tab.Count; i++) if (tab[i].cardbackFrom == "substitute") subs.Add(tab[i].deckId);
                            subs.Sort();
                            // ⚠️ 这一串**必须逐字对**（多一副少一副都说明替身表被动过）—— 4 副：
                            //    前两副是 09-26 用户拍板选的；后两副是「遭遇那批列出来」之后露出来的
                            //    （`ASH_SK_2` 的 `Warlord_Anvirr` 本地连相近的都没有 ⇒ 同阵营督军主题顶上）。
                            CheckTrue(string.Join(",", subs.ToArray()) == "ASH_SK_2,OrksDeck2,SpaceWolvesDeck5,SW_SK_3",
                                      "用**同阵营替身**的正好是那 4 副（`OrksDeck2`→Goff 督军主题 · `SpaceWolvesDeck5`→太空野狼「凶暴」·"
                                    + " `SW_SK_3`→凶暴 · `ASH_SK_2`→灵族另一张督军主题）—— 实得 " +
                                      (subs.Count == 0 ? "（一副都没有）" : string.Join("、", subs.ToArray())));
                            int dropped = PrebuiltDecks.NotListed;
                            PrebuiltDecks.CountByMode(out int cbc, out int sbc);
                            CheckTrue(cbc + sbc == tab.Count && sbc > 0,
                                      $"预组页**两种模式都列**（经典 {cbc} + 遭遇 {sbc} = {tab.Count}）—— "
                                    + "遭遇那批是 2026-09-26 引擎支持之后才列出来的");
                            CheckTrue(ds2.ScopeText.Contains("本页列 " + tab.Count),
                                      "红底板下沿那行小字**说清了列了多少 / 藏了多少**：" + ds2.ScopeText);
                            CheckTrue(ds2.EmptyText.Length == 0, "预组页非空 ⇒ **空态那行字不显示**");

                            // ---- ④ **搜索框不建**（2026-09-26 用户拍板「按照原版设计」）----
                            //    原版出厂 `act=N`，且四条证据都指向「没有任何代码打开它」（见 `DeckSelectionPopup.Search` 那段注释）
                            CheckTrue(ds2.SearchHit == null, "窗上**没有** `SearchHit`（搜索框不建）");
                            CheckTrue(ds2.transform.Find("Search Text") == null, "也没有 `Search Text` 那行字");
                            CheckTrue(ds2.transform.Find("InputFieldBackground") == null, "连输入框底板都没建（原版那 4 个节点一个不建）");
                            Check(ds2.ShownCount, tab.Count, "没有搜索 ⇒ 预组页恒列 " + tab.Count + " 副");

                            // ---- ⑤ 切到「我的卡组」⇒ 老的搜索断言照旧 ----
                            var tabOwn = ds2.TabHit(true);
                            var tabOwnBtn = tabOwn != null ? tabOwn.GetComponent<WindowButton>() : null;
                            CheckTrue(tabOwnBtn != null, "「我的卡组」页签有点击区");
                            if (tabOwnBtn != null)
                            {
                                tabOwnBtn.Click();
                                Check(ds2.OwnDecks, true, "点它 ⇒ 切到**我的卡组**那一页");
                                CheckTrue(ds2.ScopeText.Length == 0, "「我的卡组」页**不显示**那行范围小字（只预组页有）");
                                var c1 = ds2.Cells.Count > 0 ? ds2.Cells[0] : null;
                                CheckTrue(c1 != null && FindChild(c1, "DificultyLevel") == null,
                                          "「我的卡组」页**不画难度角标**（原版 `DeckCollectionDisplay.displayDifficultyLabel` 在这一页是 0）");
                                CheckTrue(c1 != null && FindChild(c1, "Game Mode Icon") == null,
                                          "「我的卡组」页也**没模式图标**（玩家自己的卡组没有 `gameMode` 这个概念）");
                                Check(ds2.ShownCount, CollectionData.DeckCount(),
                                      $"「我的卡组」页列出全部 {CollectionData.DeckCount()} 套（没有搜索 ⇒ 不过滤）");
                                CheckTrue(ds2.transform.Find("DificultyLevel") == null
                                          || FindChild(c1, "DificultyLevel") == null,
                                          "（复查）「我的卡组」页仍不画难度角标");
                            }
                            Shoot("03_选卡组弹窗.png");

                            // ---- ⑥ 切回「预组」+ **点一副** ⇒ 回调拿到的是预组（原来这里会静默无事发生）----
                            ds2.SwitchTab(false);
                            Check(ds2.OwnDecks, false, "切回「预组卡组」页");
                            var first = PrebuiltDecks.Tab[0];
                            CheckTrue(ds2.Cells.Count > 0, "预组页**画出了格子**（" + ds2.Cells.Count + " 个）");

                            // ---- ⑥b 格子上的三层图标：阵营（**左下**）· 模式 · 难度角标 ----
                            var c0 = ds2.Cells.Count > 0 ? ds2.Cells[0] : null;
                            var facI = c0 != null ? FindChild(c0, "Faction") : null;
                            CheckTrue(facI != null, "格子上**画了阵营图标**");
                            if (facI != null)
                            {
                                // 世界坐标 → 画布 px（同 `CollectionScene` 的 `PxOf/PxYOf`）
                                float fx = facI.position.x * 108f + 960f, fy = 540f - facI.position.y * 108f;
                                CheckTrue(fx < 500f && fy > 450f,
                                          "阵营图标在**左下**（原版 `Faction Icon [-10.5,273.7]`，`资料/说明书/04_界面UI/卡组界面说明书.md:56` 原话「左下阵营图标」）" +
                                          " —— 实得中心 (" + fx.ToString("F1") + ", " + fy.ToString("F1") + ")；" +
                                          "🔴 原来我们画在**右上**且无断言，2026-09-26 更正");
                            }
                            var gmI = c0 != null ? FindChild(c0, "Game Mode Icon") : null;
                            var gq = gmI != null ? gmI.GetComponent<ImageQuad>() : null;
                            // 🔴 **图标名必须由那一格的 `gameMode` 推出来**（原版就是 `GetGameModeIcon(gameMode)`）——
                            //    原来这里**写死** `40k_gamemode_icon_classic`、还配了句「第一副是经典模式」；
                            //    2026-09-26 页里混进遭遇副之后第一副变成了 `ASH_SK_1`（也 diff 5、也 army 30），
                            //    写死就红了 —— **这正是「别把数据的一个快照写进断言」那条**。
                            string wantGmIcon = tab[0].gameMode == (int)GameMode.Skirmish
                                              ? "40k_gamemode_icon_skirmish" : "40k_gamemode_icon_classic";
                            CheckTrue(gq != null && gq.Texture != null && gq.Texture.name == wantGmIcon,
                                      $"格子上画了**模式图标** = `{wantGmIcon}`（第一副 `{tab[0].deckId}` 的 `gameMode` = "
                                    + $"{tab[0].gameMode}）—— 判据是**从数据推的**，不是写死某一档");
                            var dfI = c0 != null ? FindChild(c0, "DificultyLevel") : null;
                            var dq = dfI != null ? dfI.GetComponent<ImageQuad>() : null;
                            CheckTrue(dq != null && dq.Texture != null && dq.Texture.name == "Menu_Icon_Gallons_1",
                                      $"**难度角标**画了，第一副难度 {tab[0].difficulty} ⇒ `Menu_Icon_Gallons_1`（一条杠）"
                                    + "（节点名 `DificultyLevel` 是**照抄原版的拼写**，别改成 Difficulty）");

                            ds2.Pick(new DeckSelectionPopup.DeckPick
                            {
                                Prebuilt = true,
                                Info = DeckSelectionPopup.InfoOf(first),
                                OwnIndex = -1,
                                PrebuiltDeck = first,
                            });
                            CheckTrue(pw.PickedPrebuilt != null && pw.PickedPrebuilt.deckId == first.deckId,
                                      "点一副预组 ⇒ **回调把那一副交出去了**（`PickedPrebuilt` = " + first.deckId + "）");
                            // ---- ⑥b 「本局用这副牌」通道：写进去 = **开战链真能用它**（原版走 `SetPlayerDeck`，没有这条分支）----
                            var pend = PrebuiltDecks.PendingSource;
                            CheckTrue(pend != null && pend.deckId == first.deckId,
                                      "选中预组 ⇒ **写进了「本局用这副牌」通道**（`PendingSource`）—— 开战不再只认 `DeckLibrary.Current`");
                            var pdck = PrebuiltDecks.ToPlayerDeck(first);
                            CheckTrue(pdck.WarlordId == first.heroId,
                                      "搓出来的 `PlayerDeck` 督军 = 预组的督军（**督军不占 30 张位**）");
                            CheckTrue(pdck.DefensiveId == first.defensiveId,
                                      "防御卡 = **我们补的那张**（" + first.defensiveNameZh + " " + first.defensiveId +
                                      "）—— 原版预组那份是 null（反汇编证实），**加它是我们的选择**");
                            Check(pdck.CardIds.Count, first.cardIds.Length, "普通卡位 = 预组卡表长度（督军/防御卡都不在内）");
                            CheckTrue(pdck.CardbackId == first.cardback, "卡背 = 原版那副牌自己的卡背");
                            var took = PrebuiltDecks.TakePendingBattleDeck();
                            CheckTrue(took != null && took.Name == pdck.Name,
                                      "`TakePendingBattleDeck()` 拿得到 —— 开局那条路读的就是它");
                            CheckTrue(PrebuiltDecks.TakePendingBattleDeck() == null,
                                      "**读一次就清** —— 下一局不会再带上上一局挑的预组牌");
                            Check(ds2.CurrentState, WindowState.Closed, "选完 ⇒ **窗自己关上**（原版 `Select` 的两步）");

                            // ---- ⑦ 再开一次，验关闭圆钮 ----
                            var ds3 = pw.OpenDeckSelection();
                            if (ds3 != null)
                            {
                                CheckTrue(ds3.SearchHit == null, "（复查）**搜索框确实不建**（照原版 `act=N`，且无代码打开它）");
                                var cls = ds3.CloseHit;
                                var clsBtn = cls != null ? cls.GetComponent<WindowButton>() : null;
                                if (clsBtn != null) clsBtn.Click();
                                Check(ds3.CurrentState, WindowState.Closed, "点关闭圆钮 ⇒ 窗关上");
                            }
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

        // ============================================================ 遭遇战 / 排位 活动窗（2026-09-24 建）
        // 正本 `资料/阶段二_战斗入口_原版规格.md` §二 B/C；几何 2026-09-24 又从根实读复核过一遍。
        Section("遭遇战 `SkirmishModeEventWindow`（正本 §二 B）");
        {
            var sc = menu.Find("Base Game Mode Container 1x1 - Skirmish");
            var sh = FindChild(sc, "Hit");
            var swb = sh != null ? sh.GetComponent<WindowButton>() : null;
            CheckTrue(swb != null, "遭遇战卡有点击区");
            if (swb != null) swb.Click();
            var sk = SkirmishEventWindow.LastOpened;
            CheckTrue(sk != null, "点遭遇战卡 ⇒ **开出了 `SkirmishModeEventWindow`**");
            if (sk != null)
            {
                Check(sk.type, WindowType.Popup, "`type` = **1 Popup**（§一 原文）");
                Check(sk.placement, WindowsPlacement.Canvas,
                      "`windowsPlacement` = **5 Canvas**（§一 原文 —— ⚠️ **不是练习窗那个 15**）");
                Check(sk.closeOnEsc, true, "`closeOnESC` = **1**（原文）");

                // 背景三层（§二 B）—— 红底那件是**九宫格**，`Menu Vignette` 是纯色
                CheckAtWorld(FindChild(sk.transform, "Reward Background Get Reward"), 0f, 1920f, 121.80f, 1013.11f,
                             "`Reward Background Get Reward`（`40k_general_popup_simple red`）");
                CheckAtWorld(FindChild(sk.transform, "Menu Vignette"), 0f, 1920f, 0f, 1080f, "`Menu Vignette`");

                // 中间那一栏
                CheckAtWorld(FindChild(sk.transform, "Ranked Deck Selection"), 525.30f, 1394.70f, -0.10f, 1080.10f,
                             "`Ranked Deck Selection`");
                CheckAtWorld(FindChild(sk.transform, "Warlod Image"), 450.93f, 1549.07f, -95.07f, 1003.07f,
                             "`Warlod Image`（1098.14²）");
                CheckAtWorld(FindChild(sk.transform, "Warlord Darkening"), 529.97f, 1470.03f, 418.18f, 975.82f,
                             "`Warlord Darkening`（`Smooth background square` Sliced · col(0,0,0,0.816)）");
                CheckAtWorld(FindChild(sk.transform, "Faction Icon Image"), 711.01f, 1233.64f, 133.50f, 656.14f,
                             "`Faction Icon Image`（522.63²）");
                // 🔴 **2026-09-24 更正**：这两行原版 `hAlign` 是 **Center**（旧 `menu_dump.py` 的 hAlign 映射
                //    错位一位，把它印成了 `Right`）⇒ 现在**不调 `Align*`**，节点就落在矩形中心 ⇒ 用 `CheckAtWorld` 断。
                //    （教训：**自检可以替错口径背书** —— 这条断言当时是绿的，但盯的是错的轴。）
                CheckAtWorld(FindChild(sk.transform, "Deck Name"), 694.70f, 1216.70f, 702.14f, 779.06f, "`Deck Name`（居中）");
                CheckAtWorld(FindChild(sk.transform, "Deck Warlord"), 694.70f, 1216.70f, 779.06f, 833.74f,
                             "`Deck Warlord`（居中）");
                CheckText(TextOf(FindChild(sk.transform, "Deck Name")), "菜单测试卡组 1",
                          "卡组名画的是**当前选中那套**（自检里第 1 套塞了督军）");
                // 四颗圆钮（HLG spacing 0 · align MiddleCenter · 每颗 160.87×128）
                foreach (var nm in new[] { "Previous Deck Button", "Change Deck Button", "View Deck Button", "Next Deck Button" })
                    CheckTrue(FindChild(sk.transform, nm) != null, $"`{nm}` 建了（四颗圆钮之一）");
                {
                    // 世界坐标 → 画布 px（本文件没有 `PxOf` 那种助手，照 `Deck Scroll View` 那条的写法）
                    var pb = FindChild(sk.transform, "Previous Deck Button");
                    float leftPx = pb != null ? pb.position.x * 108f + 960f - SkirmishEventWindow.DbBtnW * 0.5f : 0f;
                    CheckNear(leftPx, 631.26f, 0.6f,
                              "第 1 颗钮的左边界 = **631.26** = 栏中心 953.00 − 4×160.87/2（照原版 HLG 算）");
                }
                CheckText(TextOf(FindChild(sk.transform, "Numer Of Army Decks")),
                          CollectionData.DeckCount() + "/20",
                          "`Numer Of Army Decks` = **当前几套/20**（原版分母写死 20）");
                CheckTrue(sk.ArmyCells.Count >= 13 || sk.ArmyCells.Count > 0,
                          $"阵营列画了 {sk.ArmyCells.Count} 格（原版 `Army Content` 是 GridLayoutGroup 168² · 3 列）");

                // 顶上标题栏 + 两颗按钮
                CheckText(TextOf(FindChild(sk.transform, "Window Title")), "Game mode", "`Window Title` 文案");
                // 原版 hAlign = **Left**（旧 dump 错印成 Center）⇒ 比**渲出来的左边缘** = HLG 的 padLeft **155**
                {
                    float t1, t2, t3, t4;
                    bool ok = RenderedRect(FindChild(sk.transform, "Window Title"), out t1, out t2, out t3, out t4);
                    CheckTrue(ok, "`Window Title` 建了");
                    if (ok) CheckNear(t1, 155f, 2f, "`Window Title` **渲出来**的左边缘 = HLG 的 **padLeft 155**");
                }
                CheckAtWorld(FindChild(sk.transform, "Header Back Button"), -24.40f, 143.48f, 42.88f, 154.21f,
                             "`Header Back Button`");
                // 🔴 **2026-09-24 更正**：原来这条写「本地没有那两张图」——**是错的**（`Resources/Art/ui_menu/`
                //    里 `40k_gamemode_icon_skirmish` / `_classic` 早在，练习窗那个 Toggle 用的就是它们）
                //    ⇒ 遭遇战窗接 `skirmish` 那张，**按贴图名断**（不是断言「没建」）
                {
                    var gmi = FindChild(sk.transform, "Game Mode Icon");
                    var gmq = gmi != null ? gmi.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(gmq != null && gmq.Texture != null && gmq.Texture.name == "40k_gamemode_icon_skirmish",
                              "`Game Mode Icon` 用的是 **`40k_gamemode_icon_skirmish`**（HLG 里紧跟标题：左沿 529.86）");
                }

                // `To Battle Button` + 两颗图标（**遭遇战是金杯**）
                CheckAtWorld(FindChild(sk.transform, "To Battle Button"), 1376.76f, 1817.09f, 917.80f, 1038.40f,
                             "`To Battle Button`");
                // ⚠️ `Button Text` 这个名字在树里有**三处**（另一处在「无督军」那块）⇒ 必须**限定父节点**找
                CheckText(TextOf(FindChild(FindChild(sk.transform, "To Battle Button"), "Button Text")), "Battle!",
                          "开战钮文案 = `Battle!`");
                {
                    var tq = FindChild(sk.transform, "TrophyIcon");
                    var tQuad = tq != null ? tq.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(tQuad != null && tQuad.Texture != null && tQuad.Texture.name == "WF_UI_Trophy_Gold",
                              "`TrophyIcon` 用的是 **`WF_UI_Trophy_Gold`**（遭遇战那一张）");
                    var sq = FindChild(sk.transform, "ShieldIcon");
                    var sQuad = sq != null ? sq.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(sQuad != null && sQuad.Texture != null && sQuad.Texture.name == "UI_icon_shield",
                              "`ShieldIcon` 用的是 `UI_icon_shield`");
                }

                // 左列 `Reward Display` + 只有遭遇战有的 `Timer` / `Banned card in deck`
                CheckText(TextOf(FindChild(sk.transform, "Reward Tile")), "Progression", "左列标题 = `Progression`");
                {
                    // ⚠️ `Timer` 是 HLG（图标的位置由「倒计时文字多宽」决定）而那段文字本地没有
                    //    ⇒ **整件都不画**（连图标），只留节点 + 出声（原来画了个位置错的图标：偏左 207px、下端出屏）
                    var tm = FindChild(sk.transform, "Timer");
                    CheckTrue(tm != null, "`Timer` 节点在（原版这一件在**遭遇战**有、**排位根上没有**）");
                    CheckTrue(tm != null && tm.GetComponentInChildren<ImageQuad>() == null,
                              "`Timer` 里**没有画任何东西**（连图标都没画 —— 位置由本地没有的倒计时文字决定，出声）");
                }
                {
                    var ban = FindChild(sk.transform, "Banned card in deck");
                    CheckTrue(ban != null && !ban.gameObject.activeSelf,
                              "`Banned card in deck` 建了但**关着**（本地没有禁用卡表 —— 出声）");
                }
                Check(sk.MissingArt.Count, 0,
                      "遭遇战窗**图一张都不缺**（`Tex()` 会记账 —— 取不到的层 `MenuDraw` 是**静默不画**的）");
                CheckTrue(FindChild(sk.transform, "Scoring Bar Event Score Info") != null,
                          "记分条建了（那一族被 `scl 1.2563` + `0.8696` 两层包着 ⇒ rect 是**绕中心乘回去**算的）");
                Shoot("04_遭遇战窗.png");

                // 开战链：`Battle!` → `StartMatch`（匹配窗 + 12 秒）→ `StartBotBattle`
                {
                    var hit = FindChild(sk.transform, "BattleHit");
                    var hb = hit != null ? hit.GetComponent<WindowButton>() : null;
                    CheckTrue(hb != null, "`Battle!` 有点击区");
                    if (hb != null) hb.Click();
                    var sp = FindChild(sk.transform, "Searching Oponent Popup (1)");
                    CheckTrue(sp != null && sp.gameObject.activeSelf,
                              "点 `Battle!` ⇒ `Searching Oponent Popup` 出来（原版 `MatchMakerManager.StartMatch`）");
                    CheckTrue(!sk.StartedBattle, "12 秒还没到 ⇒ 不抢跑");
                    sk.TickSearch(12f);
                    CheckTrue(!(sp != null && sp.gameObject.activeSelf), "等满 12 秒 ⇒ 匹配窗自己关掉");
                    CheckTrue(sk.StartedBattle, "等满 12 秒 ⇒ **开战成立**（真机上 `LoadScene(\"Battle\")`）");
                }
                sk.Close();
                Check(sk.CurrentState, WindowState.Closed, "关掉遭遇战窗");
            }
        }

        Section("排位 `RankedEventWindowV2`（正本 §二 C · **与遭遇战逐格相同的那一半靠继承**）");
        {
            var rc = menu.Find("Base Game Mode Container 1x1 - Ranked");
            var rh = FindChild(rc, "Hit");
            var rwb = rh != null ? rh.GetComponent<WindowButton>() : null;
            CheckTrue(rwb != null, "排位卡有点击区");
            if (rwb != null) rwb.Click();
            var rk = RankedEventWindow.LastOpened;
            CheckTrue(rk != null, "点排位卡 ⇒ **开出了 `RankedEventWindowV2`**");
            if (rk != null)
            {
                Check(rk.placement, WindowsPlacement.Canvas, "`windowsPlacement` = **5 Canvas**（原文）");
                CheckAtWorld(FindChild(rk.transform, "General Red Background"), 0f, 1920f, 634.34f, 445.66f,
                             "`General Red Background`（容器本身没有图 —— 四个背景层才是画面）");
                CheckTrue(FindChild(rk.transform, "Noise") != null,
                          "`Noise` 建了（`UI Dirt And Noise skratches` Tiled —— **只有排位窗有这一层**）");
                CheckAtWorld(FindChild(rk.transform, "Menu Vignette"), -960f, 2880f, -28.23f, 1053.32f,
                             "`Menu Vignette`（⚠️ 排位窗的值与遭遇战**不一样**：−960,−28.23→2880,1053.32）");
                CheckText(TextOf(FindChild(rk.transform, "Rank Title")), "Rank", "左列标题 = `Rank`");
                Check(rk.MissingArt.Count, 0, "排位窗**图一张都不缺**（同遭遇战那条）");
                CheckText(TextOf(FindChild(FindChild(rk.transform, "LeaderboardButton"), "Button Text")), "Leaderboard",
                          "`LeaderboardButton` 文案（同样要限定父节点 —— `Button Text` 树里有三处）");
                CheckTrue(FindChild(rk.transform, "ChangeRankedToggle") != null, "`ChangeRankedToggle` 建了");
                CheckTrue(FindChild(rk.transform, "Timer") == null,
                          "**排位窗根上没有 `Timer`**（实读；⚠️ 原版 `Ranked Division Info/Content` **里面**"
                          + "还有一个 `Timer`，那一整棵我们没建 —— 别把这句读成「排位窗没有倒计时」）");
                CheckTrue(FindChild(rk.transform, "Banned card in deck") == null,
                          "**排位窗没有 `Banned card in deck`**（同上，反向断言）");
                {
                    var tq = FindChild(rk.transform, "TrophyIcon");
                    var tQuad = tq != null ? tq.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(tQuad != null && tQuad.Texture != null
                              && tQuad.Texture.name == "40k_ranking_icon_trophy_Plus",
                              "`TrophyIcon` 用的是 **`40k_ranking_icon_trophy_Plus`**（§二 C：与遭遇战不同）");
                }
                Shoot("05_排位窗.png");
                // 排位那条路比另外三扇多一步：`Battle!` ⇒ **先开全屏 `SearchingOpponentWindow`**（入口是我们定的）
                {
                    var hit = FindChild(rk.transform, "BattleHit");
                    var hb = hit != null ? hit.GetComponent<WindowButton>() : null;
                    CheckTrue(hb != null, "`Battle!` 有点击区");
                    if (hb != null) hb.Click();
                    var so = SearchingOpponentWindow.LastOpened;
                    CheckTrue(so != null && so.gameObject.activeSelf,
                              "排位：`Battle!` ⇒ 开**全屏** `SearchingOpponentWindow`（**本地入口是我们定的** —— 原版查不到）");
                    if (so != null)
                    {
                        Check(so.type, WindowType.Fullscreen, "`type` = **0 Fullscreen**（§一 原文）");
                        Check(so.placement, WindowsPlacement.None,
                              "`windowsPlacement` = **0 None**（四扇窗里只有它这样）");
                        // 原版 `Title` 的 hAlign = **Center**（旧 dump 错印成 Right）⇒ 节点就在矩形中心
                        CheckAtWorld(FindChild(so.transform, "Title"), 741.12f, 1178.88f, 136f, 186f,
                                     "`Title`（\"Searching opponent\" fs36 · 居中）");
                        CheckAtWorld(FindChild(so.transform, "Cancel Match"), 785f, 1135f, 942f, 1018f, "`Cancel Match`");
                        var ply = FindChild(so.transform, "Searching Opponent Player Info Container");
                        var en = FindChild(so.transform, "Searching Opponent Enemy Info Container");
                        var pf = FindChild(ply, "Found Player Container");
                        var ef = FindChild(en, "Found Player Container");
                        CheckTrue(pf != null && pf.gameObject.activeSelf,
                                  "**玩家**那一格走 `Found`（立绘 + 名字）");
                        CheckTrue(ef != null && !ef.gameObject.activeSelf,
                                  "**对手**那一格走 `Not Found`（空态 —— 原版 `Initialize(null,…)` 那一支）");
                        CheckTrue(FindChild(en, "Not Found Container") != null
                                  && FindChild(en, "Not Found Container").gameObject.activeSelf,
                                  "`Not Found Container` 露出来（原版它**没有图**，100×100 空框）");
                        Shoot("06_找对手窗.png");
                        // 取消 ⇒ 那扇窗自己关掉，**排位窗还在**（它是弹窗，不在 `currentWindow` 那个位上）
                        var ch = FindChild(so.transform, "CancelHit");
                        var cb = ch != null ? ch.GetComponent<WindowButton>() : null;
                        CheckTrue(cb != null, "`Cancel` 有点击区");
                        if (cb != null) cb.Click();
                        Check(so.CurrentState, WindowState.Closed, "点 `Cancel` ⇒ 找对手窗关上");
                        CheckTrue(rk.CurrentState != WindowState.Closed, "**排位窗还在**（没被那扇全屏窗带走）");
                    }
                    // 再走一遍：这回不取消，等满 12 秒 ⇒ 开战
                    if (hb != null) hb.Click();
                    rk.TickSearch(12f);
                    CheckTrue(rk.StartedBattle, "排位窗的 `Battle!` 走**同一条**开战链（等满 12 秒 ⇒ 开战）");
                }
                rk.Close();
                Check(rk.CurrentState, WindowState.Closed, "关掉排位窗");
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

        // 🆕 2026-09-25：标题的**竖直位置**（`项目任务.md` §三 第 15 条 第 63 行）——
        // 原来只断**宽度**，位置错了也照绿。
        // 原版那行归 `TextDarkening` 上的 **`VerticalLayoutGroup`** 排
        // （`bundle_menus_assets_all` 实读：`m_Padding.m_Left 11` · `m_ChildAlignment 3 (MiddleLeft)`
        //  · `m_Spacing −4.2` · `m_ChildControlHeight 0`）；两个孩子逐个量过尺寸：
        // `Event Title` 高 **55.708** · `Timer With Time Description` 高 **40.729**，暗带高 **105.866**
        // ⇒ 内容 = 55.708 + 40.729 − 4.2 = **92.237**，在暗带里**垂直居中** ⇒ 上下各留 **6.814**；
        // 标题是**第一个孩子** ⇒ 顶在内容最上面 ⇒
        // **标题上边缘距暗带顶 = 6.814** · **下边缘 = 6.814 + 55.708 = 62.522**。
        // 🔴 这几个数**硬写在测试里**（不引用 `MainMenuRuntime` 的常量）—— 引用就成了自证。
        // ⚠️ 轴向差点搞反（2026-09-25 实测量过）：`RenderedRect` 的 y 是**上到下**
        //    （`LayoutSpace.PxY = 540 − y×108`）⇒ **`y1` 是上边缘、`y2` 是下边缘**；
        //    而 `MainMenuRuntime` 里 `by1` 是**暗带顶**、`by2` 是**暗带底**。
        //    「拿 PxX 的式子去量 y」在 2026-09-23 就踩过一次（`LayoutSpace.PxY` 的注释）。
        {
            var mcard = menu.Find("Base Game Mode Container 1x1 - Tutorial");
            float bx1, by1, bx2, by2, tx1, ty1, tx2, ty2;
            if (RenderedRect(FindChild(mcard, "TextDarkening"), out bx1, out by1, out bx2, out by2)
                && RenderedRect(FindChild(mcard, "Event Title"), out tx1, out ty1, out tx2, out ty2))
            {
                // ⚠️ 量的是 `Label.WorldH`（**字形盒**，比 55.708 那个布局盒略小）⇒ 容差 1.5px
                CheckNear(ty1 - by1, 6.814f, 1.5f,
                          "★ 标题**上边缘距暗带顶 6.81px**（原版 VLG：105.866 − 92.237 的一半）");
                CheckNear(ty2 - by1, 62.522f, 1.5f,
                          "★ 标题**下边缘距暗带顶 62.52px**（= 6.814 + 标题高 55.708）");
            }
            else CheckTrue(false, "量不到模式卡的 `TextDarkening` / `Event Title` —— 上面两条断言的前提");
        }
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

        // 🔴 2026-09-24 探针：模式卡的卡图**实拍看着小于卡面**（§三 第 15 条 第 45 行）。
        //    已知：quad 的 `WorldW/WorldH` **是对的**（上面那两条），`SetUvRect` **不改尺寸**（读过源码），
        //    贴图 1024² 全不透明、裁的那块里也几乎没有 (86,86,86) 的平灰。
        //    ⇒ 那就把**这一格子树里每一层**的名字 / 渲出矩形 / 图 / 队列 / 染色打出来，别再靠肉眼猜。
        {
            var cardT = menu.Find(name);
            var sb = new System.Text.StringBuilder();
            sb.Append($"[Menu]   【探针】{name} 子树：\n");
            foreach (var ch in cardT.GetComponentsInChildren<Transform>(true))
            {
                var cq = ch.GetComponent<ImageQuad>();
                var cl = ch.GetComponent<Label>();
                if (cq == null && cl == null) continue;
                float cw = cq != null ? cq.WorldW * 108f : cl.WorldW * 108f;
                float chh = cq != null ? cq.WorldH * 108f : cl.WorldH * 108f;
                int qq = -1; string tex = "-";
                if (cq != null)
                {
                    var mr = cq.GetComponent<MeshRenderer>();
                    if (mr != null && mr.sharedMaterial != null)
                    { qq = mr.sharedMaterial.renderQueue; tex = cq.Texture != null ? cq.Texture.name : "<无>"; }
                }
                sb.Append($"      {ch.name,-26} {cw,8:F1}×{chh,-8:F1} q={qq,-5} z={ch.localPosition.z:F3} 图={tex}\n");
            }
            Debug.Log(sb.ToString());
        }
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
