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
