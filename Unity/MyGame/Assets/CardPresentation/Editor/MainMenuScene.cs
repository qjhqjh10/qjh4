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

    /// <summary>🆕 A17：把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
    /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
    static void CheckHoverSwap(Transform root, string what)
    {
        int n; string bad = WindowButton.AuditHoverSwap(root, out n);
        CheckTrue(n > 0, what + "：**确实有**接了悬停换图的按钮（n=" + n + "，否则这条等于没查）");
        if (bad.Length > 0) CheckTrue(false, what + "：换图要「悬停换得动 + 离开还原得回」—— " + bad);
    }

    static void CheckNoMissingSwapArt(string what)
        => CheckTrue(WindowButton.MissingSwapArt.Count == 0,
                     what + "：**悬停图一张都不缺**（缺的会列在这里：" + string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");
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
    static void CheckAtWorld(Transform t, float x1, float x2, float y1, float y2, string what)    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var want = MainMenuRuntime.Center(x1, x2, y1, y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f,
                  $"{what}（世界坐标差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>🆕 2026-10-03：取一段文字的**字距**（没有 `Label` 就给 `NaN`）。
    /// 原版有几处 TMP 带 `m_characterSpacing`（`Window Title` 5 · `DivisionText` −2.6 · 开关两行 −4），
    /// 以前 `Label` 没有接口、这几处一直没复刻；现在有了就必须钉住。</summary>
    static float CharSpacingOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.CharSpacing : float.NaN;
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

    /// <summary>量**命中区那颗 quad 自己**的渲染矩形（不是承载它的节点）。
    /// 🔴 **为什么单开一个（2026-10-03 踩过）**：`MenuDraw.Hit` 的写法是「**节点摆在父原点**（`localPosition = 0`）、
    /// quad 摆在矩形中心」（照抄 `MainMenuSubmenuWindow.AddHit`，那边 1000+ 条断言盯着、不许改写法）。
    /// ⇒ `RenderedRect(t)` 拿 `t.position` 当中心，**只有「没被裁」时才恰好等于矩形中心**（所以一直没露）；
    /// 一被裁，quad 的中心移了、**节点没动** ⇒ 它报的是「以**整块**中心为中心、高 = **截后**高」的**假矩形**。
    /// 实据（2026-10-03）：排行榜压边行那颗 `Hit` 真值 y = 864.57..**937.83**，`RenderedRect` 报 **954.42**
    /// （= 整块中心 917.79 + 73.26/2）⇒ 一条**判对了实现、量错了东西**的假红。
    /// 判据 = **量渲染真值**：`ImageQuad` **自己的** `transform.position` + `WorldW/WorldH`
    /// （同上面「行底九宫格量子块、别量根节点」那条）。</summary>
    static bool HitQuadRect(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        var q = t != null ? t.GetComponentInChildren<ImageQuad>() : null;
        if (q == null) return false;
        float w = q.WorldW * 108f, h = q.WorldH * 108f;
        float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
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
        // 🔴 **2026-09-26 加**：主菜单**刚建好、还没开任何弹窗**时先拍一张。
        //    原来只有末尾那张 `01_主菜单.png`，而那时**屏幕上压着一个提示弹窗**（自检跑完遗留的）
        //    ⇒ 拿它当「主菜单长什么样」的参照会**看错**（2026-09-26 就被它骗过一次）。
        Shoot("00_主菜单_无弹窗.png");

        Section("整屏背景（§五 A：`Image(sprite=0)` + 双色渐变，**不是图也不是 3D**）");
        var bg = menu.Find("Background");
        CheckTrue(bg != null, "`Background` 建了");
        if (bg != null)
        {
            var q = bg.GetComponentInChildren<ImageQuad>();
            CheckTrue(q != null && q.Texture != null && q.Texture.name.StartsWith("grad_"),
                      "背景用的是**运行时生成的双色渐变**（`CardArt.Gradient`，c1/c2/angle 照 `MB1931`）");
            if (q != null) CheckNear(q.WorldH, LayoutSpace.DesignHeight, 0.01f, "背景铺满可见高度（10 个世界单位）");
            // 🔴 **方向也要对**（2026-09-27 修：我们原来把轴画成 90° 歪的 —— 上暗下亮，原版是左暗右亮）。
            //    判据 = 原版实拍量出来的亮度场（左 9 → 右 20、上下几乎不变）→ `资料/主菜单_原版规格.md` §A。
            //    渐变贴图里 **索引 0 = 左下**（`SetPixels` 的约定）⇒ 比「右上 vs 左下」即可。
            var gt = q != null ? q.Texture as Texture2D : null;
            if (gt != null)
            {
                int N = gt.width;
                float tr = gt.GetPixel(N - 1, N - 1).r;    // 右上
                float bl = gt.GetPixel(0, 0).r;            // 左下
                CheckTrue(tr > bl,
                          $"★ 背景渐变**亮在右上、暗在左下**（右上 R={tr:F3} > 左下 R={bl:F3}）—— "
                        + "原版实拍就是「左暗右亮」（9→20、上下几乎不变）；我们把轴画歪 90° 时是反的");
            }
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
        // 🔴 **2026-09-28：顶栏那一档抬到【所有窗口之上】**（用户拍板：选卡组弹窗那两个页签占 y 35.07~107.25、
        //    与顶栏 y 0~100 重合，「要照原版让顶栏压住它」⇒ 做层序，不是删页签）。
        //    做法与那条「反证」（原版兄弟序里 `3 - PopUp Holder` 其实排在 `Upper bar` 之后）→ `MainMenuRuntime` 的注释。
        {
            CheckTrue(MainMenuRuntime.QBarPanel > LeaderboardWindow.QBase,
                      $"顶栏那一档（{MainMenuRuntime.QBarPanel}）**高于全工程最高的窗口档**"
                      + $"（排行榜 `QBase = {LeaderboardWindow.QBase}`）");
            var barChat = menu.Find("ChatPreview");
            int inBand = 0, wrong = 0; string firstWrong = null;
            foreach (var q in menu.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q.RenderQueue <= 0) continue;
                bool underBar = q.transform.IsChildOf(bar) || (barChat != null && q.transform.IsChildOf(barChat));
                if (underBar)
                {
                    if (q.RenderQueue >= MainMenuRuntime.QBarPanel && q.RenderQueue <= MainMenuRuntime.QBarOverlay) inBand++;
                    else { wrong++; if (firstWrong == null) firstWrong = q.name + "（队列 " + q.RenderQueue + "）"; }
                }
                else if (q.RenderQueue >= MainMenuRuntime.QBarPanel)
                { wrong++; if (firstWrong == null) firstWrong = "**非顶栏件** " + q.name + "（队列 " + q.RenderQueue + "）"; }
            }
            int labInBand = 0;
            foreach (var lb in menu.GetComponentsInChildren<Label>(true))
                if (lb.RenderQueue == MainMenuRuntime.QBarText
                    && (lb.transform.IsChildOf(bar) || (barChat != null && lb.transform.IsChildOf(barChat)))) labInBand++;
            CheckTrue(inBand >= 12, $"顶栏那条带子里有 {inBand} 张图排在 {MainMenuRuntime.QBarPanel}~{MainMenuRuntime.QBarOverlay} 档");
            CheckTrue(labInBand >= 3, $"顶栏的文字也跟着抬了（{labInBand} 段排在 `QBarText`）");
            CheckTrue(wrong == 0, "顶栏带子**之外**的件一张都没排到 3600+（实测 " + wrong + " 处"
                      + (firstWrong == null ? "）" : "：" + firstWrong + "）"));
        }
        CheckAt(FindChild(FindChild(bar, "SettingsBtn"), "Image"), 1803.1f, 1890.9f, 4.6f, 66.4f, "`SettingsBtn` 齿轮");
        // 🔴 **2026-09-26 加的**：这颗齿轮从建出来那天起**点了没反应**（只建了图、没接点击 = 静默失败）。
        //    这一条钉住「它有点击区、而且点了真能开设置窗」—— 光有矩形断言抓不到这种缺陷。
        {
            var gear = FindChild(FindChild(bar, "SettingsBtn"), "Image");
            var wb = gear != null ? gear.GetComponent<WindowButton>() : null;
            CheckTrue(wb != null && wb.onClick != null, "`SettingsBtn` **接了点击**（原来没有 —— 点了什么都没发生）");
            if (wb != null && wb.onClick != null)
            {
                wb.onClick();
                CheckTrue(SettingsWindow.Instance != null && SettingsWindow.Instance.CurrentState == WindowState.Open,
                          "点齿轮 ⇒ **真的开了设置窗**（`SettingsWindow`）");
                CheckTrue(FindChild(SettingsWindow.Instance.transform, "Tab Buttons") != null,
                          "开出来的那扇窗里有 `Tab Buttons`（三个页签：图像 / 音频 / 联机）");
                SettingsWindow.Instance.Close();
            }
        }
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
        // 🆕 2026-09-27：**顶栏那块头像立绘**（原版运行期由 `AvatarDisplay.ChangeAvatar` 赋图，
        //   出厂 `m_Sprite=0/m_Enabled=0` **不是设计**）—— 我们原来这一层根本没建，永远只有那面空盾。
        {
            var avNode = FindChild(prof, "Avatar Item Small");
            var topArt = FindChild(avNode, "Image");
            var topBd = FindChild(avNode, "Border");
            var aq = topArt != null ? topArt.GetComponentInChildren<ImageQuad>() : null;
            var bq = topBd != null ? topBd.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(aq != null && aq.Texture != null,
                      "★ 顶栏**头像立绘**建出来了（原版 `AvatarDisplay.avatarImage`；判据 → `资料/阶段二_多人界面_原版规格.md`）");
            CheckTrue(aq != null && bq != null && aq.RenderQueue > bq.RenderQueue,
                      "★ 顶栏立绘的渲染队列**比盾牌框高一档** —— 那张盾的中心是**不透明黑**，反了就是一块黑");
            CheckAt(topArt, -58.09f, 218.79f, -34.59f, 169.83f,
                    "顶栏立绘（**盒子比盾大，这是照原版 prefab 算的**：`Image` 的 `localScale=2` ⇒ 容器×2；"
                  + "立绘贴图实心部分只占 43%×60% ⇒ 露出来的只有人像。推导 → `BuildTopAvatar` 的注释）");
            // 🔴 **为什么立绘那一格要比盾大**（这条断言把那个理由钉住）：立绘贴图的**实心部分**只占
            //    **43%×60%**（`alpha>128` 的包围盒 220×306 / 512），而盾的**孔径**占边框的 **92%×93%**
            //    （近黑不透明区域的包围盒 236×266 / 256×286）⇒ **「实心的高」必须对得上「孔径的高」**，
            //    否则人像不是浮在一圈黑里（同格 ⇒ 矮 36%）、就是糊住整套框子。两个比例都是量出来的。
            {
                var aq2 = topArt != null ? topArt.GetComponentInChildren<ImageQuad>() : null;
                var bq2 = topBd != null ? topBd.GetComponentInChildren<ImageQuad>() : null;
                float artH = aq2 != null ? aq2.WorldH : 0f;
                float bdH = bq2 != null ? bq2.WorldH : 0f;
                float solidH = artH * 0.596f, holeH = bdH * 0.930f;
                // 🔴 **两格都必须【保宽高比】画**（原版 `m_PreserveAspect = 1`，13 个战场里 39/39 全是）——
                //    盾那一格我们原来按拉伸画（宽高比 1.349），**比实拍的 ≈107px 宽了 1.6 倍**。2026-09-27 修。
                CheckTrue(bq2 != null && Mathf.Abs(bq2.WorldW / bq2.WorldH - 256f / 286f) < 0.02f,
                          $"★ 盾那一格按**贴图宽高比**画（实得 {(bq2 == null ? 0f : bq2.WorldW / bq2.WorldH):F3}，"
                        + $"贴图 256/286 = {256f / 286f:F3}）—— 拉伸画会是 1.349，比原版实拍宽 1.6 倍");
                CheckTrue(Mathf.Abs(solidH - holeH) / holeH < 0.06f,
                          $"★ 立绘**实心部分**的高 ≈ 盾**孔径**的高（{solidH * 108f:F1}px vs {holeH * 108f:F1}px，"
                        + $"差 {100f * Mathf.Abs(solidH - holeH) / holeH:F1}%）—— 这一条就是「立绘为什么比盾大」的理由");
            }
            // 玩家在档案窗换了头像 ⇒ 这一层要跟着换（原版走 `PlayerAvatarDataManager.OnAvatarChanged`）
            int oldIdx = ProfileData.AvatarIndex;
            var before = aq != null ? aq.Texture : null;
            ProfileData.AvatarIndex = (oldIdx + 7) % Mathf.Max(1, ProfileData.Avatars.Count);
            menu.RefreshTopAvatarIfChanged();
            var after = aq != null ? aq.Texture : null;
            CheckTrue(after != null && after != before, "★ 换了头像 ⇒ 顶栏那一层**跟着换图**（不是一张死图）");
            ProfileData.AvatarIndex = oldIdx;
            menu.RefreshTopAvatarIfChanged();
            CheckTrue((aq != null ? aq.Texture : null) == before, "★ 换回来 ⇒ 又变回原来那张（`AvatarIndex` 是唯一一份状态）");
        }

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

                    // 🆕 2026-10-03（§三第29条 A14）：费用曲线那两件**补上了**
                    //   判据 → `资料/阶段二_战斗入口_原版规格.md` §二 A 那张表；画法与 `Deck info Popup` 共用 `CostCurveDrawer`
                    // ⚠️ 必须用 `CheckAtWorld` —— `Cost/balance text` 挂在 `General container` 下（不是窗口根的直接子件），
                    //    而 `CheckAt` 比的是 **`localPosition`**（只对直接子件成立）⇒ 第一版差 565.73px。
                    CheckAtWorld(FindChild(gen0, "Deck Information Cost/balance text"),
                                 1276.27f, 1540.27f, 326.95f, 381.27f, "`Cost/balance text`（fs34 auto[1-34] · 居中）");
                    CheckText(TextOf(FindChild(gen0, "Deck Information Cost/balance text")), "Card / Energy cost",
                              "…文案 = 原版的 `Card / Energy cost`");
                    Check(pw.CostRows.Count, 9, "费用曲线 **9 行**（费用 0..8）");
                    int csum = 0; for (int k2 = 0; k2 < pw.CostRows.Count; k2++) csum += pw.CostRows[k2];
                    CheckTrue(csum >= 0, $"…读数：{CostCurveDrawer.Dump(pw.CostRows.ToArray())}");
                    var cdr = FindChild(gen0, "Deck CostQuanityt Row Drawer");
                    CheckTrue(cdr != null, "第 1 行建了（原版把行名拼错成 `Quanityt`，**照抄别改**）");
                    if (cdr != null)
                    {
                        // 抽屉中心 (1408.275, 504.71)、scl 1.2 ⇒ 第 0 行行心 y = 504.71 + (−90.045×1.2) = 396.656
                        // 行 223.59×18.91 ×1.2 = 268.308 × 22.692
                        CheckAtWorld(cdr, 1274.12f, 1542.43f, 385.31f, 408.00f,
                                     "第 0 行（中心 **1408.28, 396.66** = 抽屉中心 − 90.045×**1.2** 那一档缩放）");
                        var cc = FindChild(cdr, "Card Cost");
                        CheckTrue(cc != null && FindChild(cdr, "Cards in deck") != null,
                                  "行里有 `Card Cost` + `Cards in deck` 两段字");
                        CheckTrue(FindChild(cdr, "Background") != null, "…还有滑块底 `40k_CardAmount_bar_bg`");
                    }
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
                            // 🔴 **层序**（用户 2026-09-28 拍板）：顶栏那一档**压在这扇弹窗之上** ——
                            //    实拍里页签条（y 35.07~107.25）与顶栏（y 0~100）重合却**只看得见顶栏**；
                            //    `PointerLayer.HitButton` 也按「队列最大者赢」挑点击 ⇒ 页签**看不见、也点不到**（原版如此）。
                            CheckTrue(MainMenuRuntime.QBarPanel > DeckSelectionPopup.QDs,
                                      $"**顶栏压住选卡组弹窗**（顶栏 {MainMenuRuntime.QBarPanel} > 弹窗 {DeckSelectionPopup.QDs}）");

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
                                          "「我的卡组」页**不画模式图标**"
                                          + "（⚠️ 2026-09-26 订正：**理由换了** —— 原来写「玩家自己的卡组没有 `gameMode` 这个概念」"
                                          + "**是错的**：原版 `CardDeck.gameMode` @0x70 就是玩家卡组的字段。"
                                          + "真正的理由是**那一格本来就不画**：全库没有 `ItemDrawer<CardDeck>`（只有 `DeckDrawer : ItemDrawer<PrebuiltDeck>` 带 `gameModeIcon`），"
                                          + "`DeckCollectionTab` 的字段里也没有图标。判据 → `资料/加时与冲突模式_原版规格.md` §2.7）");
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

        // ============================================================ 玩家档案窗（2026-09-27 建 · 多人界面第 2 件）
        // 判据：骨架 → 正本 `资料/阶段二_多人界面_原版规格.md` §2·1；页签键那一层 → `Shell/PlayerProfileWindow.cs` 文件头。
        // 🔴 **断的全是「原版参数」**，不是我们自己的常量（10·3 第 3 层）。
        Section("玩家档案窗 `Player Profile Window`（入口 = 顶栏头像）");
        {
            var avatarBorder = FindChild(FindChild(prof, "Avatar Item Small"), "Border");
            var avb = avatarBorder != null ? avatarBorder.GetComponent<WindowButton>() : null;
            CheckTrue(avb != null && avb.onClick != null,
                      "顶栏头像**接了点击**（原来没有 —— 点了什么都没发生，同齿轮当初那个静默失败）");
            if (avb != null && avb.onClick != null)
            {
                avb.onClick();
                var pp = PlayerProfileWindow.LastOpened;
                CheckTrue(pp != null && pp.CurrentState == WindowState.Open, "点头像 ⇒ **真的开了玩家档案窗**");
                if (pp != null)
                {
                    Check(pp.type, WindowType.Popup, "`type` = **1 Popup**（原文）");
                    Check(pp.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**（原文）");
                    CheckTrue(pp.closeOnEsc, "`closeOnESC` = **1**（原文）");
                    CheckNear(pp.extraScaleSmallScreen, 1.075f, 1e-4f,
                              "`extraScaleSmallScreen` = **1.075**（⚠️ 不是 1.07 —— 那是练习窗的值，**逐窗实测**）");
                    Shoot("09_档案窗_Profile页.png");    // 六页都建完了，但原来**一张截图都没有**

                    var t = pp.transform;
                    CheckAtWorld(FindChild(t, "Menu Dark Background"), -1327.3f, 3247.3f, -746.18f, 1826.18f,
                                 "压暗层 `Menu Dark Background`（纯色 (0,0,0,0.7725)，**原版就没图**）");
                    CheckAtWorld(FindChild(t, "Menu Area"), 0f, 1920f, 0f, 1080f, "`Menu Area`（满屏容器）");
                    // ⚠️ `Tab  Area` 名字里是**两个空格**（原版就这么拼）
                    CheckAtWorld(FindChild(FindChild(t, "Menu Area"), "Tab  Area"),
                                 273.48f, 1824.52f, 118f, 962f, "`Tab  Area`（**名字两个空格**）");
                    var tabArea = FindChild(FindChild(t, "Menu Area"), "Tab  Area");
                    CheckAtWorld(FindChild(tabArea, "Generic Window Red Background Big"),
                                 273.48f, 1824.52f, 118.92f, 962f, "红底（`UI_Deck_Information_Back`，Sliced 九宫格）");
                    CheckAtWorld(FindChild(tabArea, "Generic Close Button Orange"),
                                 1760.80f, 1835.19f, 118.92f, 194.52f,
                                 "右上橙色关闭钮（⚠️ 原版**右溢出窗框 ~10.7px** —— 别「对齐」掉）");
                    CheckAtWorld(FindChild(tabArea, "Tab Content"), 351.03f, 1746.97f, 118.92f, 962f, "`Tab Content`");

                    // 左栏：`Tab Buttons` + 六个键（**位置是 VerticalLayoutGroup 算出来的**，正本 §2·1）
                    var bar2 = FindChild(FindChild(t, "Menu Area"), "Tab Buttons");
                    CheckAtWorld(bar2, 95.48f, 273.48f, 180.24f, 885.752f,
                                 "`Tab Buttons`（`VLG` spacing 10 / align 5 MiddleRight / ctrlH+expandH）");
                    for (int i = 0; i < PlayerProfileWindow.Tabs.Length; i++)
                    {
                        var spec = PlayerProfileWindow.Tabs[i];
                        float top = PlayerProfileWindow.BarT + PlayerProfileWindow.KeyStep * i,
                              bot = top + PlayerProfileWindow.KeyH;
                        var key = FindChild(bar2, spec.Node);
                        CheckAtWorld(key, PlayerProfileWindow.KeyL, PlayerProfileWindow.KeyR, top, bot,
                                     $"键 {i} `{spec.Node}`（布局后：165 × 109.252、y 从 180.24 步进 119.252）");
                        if (key == null) continue;
                        CheckAtWorld(FindChild(key, "Icon"), spec.IconL, spec.IconR, spec.IconT, spec.IconB,
                                     $"键 {i} 的 `Icon` 框（**六个键各不相同** —— 原版没挂 AspectRatioFitter，别统一）");
                        var lab = FindChild(key, "Tab Toggle Title");
                        CheckAtWorld(lab, PlayerProfileWindow.LabL, PlayerProfileWindow.LabR, spec.LabT, spec.LabB,
                                     $"键 {i} 的 `Label`（155 宽 **裸文字** —— 这里**没有**名字条底图）");
                        CheckText(TextOf(lab), spec.Label,
                                  $"键 {i} 文案 = `{spec.Label}`（⚠️ **键名与文案不一致**：`Trophies`→`Achievements`、`Ranked`→`Ranking`）");
                        // 🔴 **断「渲染出来的字放得进框」**，不是断字号（`AutoFitBox` 那条教训：字号对而溢出，
                        //    自检照样全绿 —— 2026-09-22 踩过）。原版这六条 TMP 是 **NoWrap + Overflow + autosize(10→35)**。
                        {
                            var lb = lab != null ? lab.GetComponent<Label>() : null;
                            float wPx = lb != null ? lb.WorldW * 108f : -1f;
                            float hPx = lb != null ? lb.WorldH * 108f : -1f;
                            float bw = PlayerProfileWindow.LabR - PlayerProfileWindow.LabL, bh = spec.LabB - spec.LabT;
                            CheckTrue(wPx > 0f && wPx <= bw + 0.5f,
                                      $"键 {i} 的「{spec.Label}」**渲染宽度 {wPx:F1}px ≤ 框宽 {bw}px**（原版 NoWrap+Overflow ⇒ 超了就画到键外面）");
                            CheckTrue(hPx > 0f && hPx <= bh + 0.5f,
                                      $"键 {i} 的「{spec.Label}」**渲染高度 {hPx:F1}px ≤ 框高 {bh:F2}px**");
                        }
                    }

                    // 六个页根：`Avatar`/`Title` 两个**两侧各溢 16.33**（真值）
                    CheckAtWorld(FindChild(t, "Profile Tab"), 351.03f, 1746.97f, 118.92f, 962f, "`Profile Tab` 页根");
                    CheckAtWorld(FindChild(t, "Avatar Tab"), 318.37f, 1746.97f, 118.92f, 962f,
                                 "`Avatar Tab` 页根（**两侧各溢 16.33** —— 真值，别「对齐」掉）");
                    CheckAtWorld(FindChild(t, "Title Tab"), 318.37f, 1746.97f, 118.92f, 962f, "`Title Tab` 页根（同上）");
                    CheckAtWorld(FindChild(t, "Battle Log Tab"), 351.03f, 1746.97f, 118.92f, 962f, "`Battle Log Tab` 页根");
                    CheckAtWorld(FindChild(t, "Trophies Tab"), 351.03f, 1746.97f, 118.92f, 962f, "`Trophies Tab` 页根");
                    CheckAtWorld(FindChild(t, "Ranked Tab"), 351.03f, 1746.97f, 118.92f, 962f, "`Ranked Tab` 页根");

                    // 🔴 **出厂打开的是 `Title` 页（第 3 个键），不是第一页** —— 唯一信号 = 原版只有
                    //    `Title Tab` 是 `m_IsActive=true`（`m_IsOn` 六个键全 0，数据里看不出哪个键亮着）
                    Check(pp.CurrentTab, WindowTabType.ProfileTitle, "出厂落在 **`Title` 页**（不是第一页）");
                    CheckTrue(FindChild(t, "Title Tab") != null && FindChild(t, "Title Tab").gameObject.activeSelf,
                              "`Title Tab` 页根是 **active** 的");
                    CheckTrue(FindChild(t, "Profile Tab") != null && !FindChild(t, "Profile Tab").gameObject.activeSelf,
                              "`Profile Tab` 页根出厂 **inactive**");

                    // 选中态：**只换贴图、不换色**（`EverguildToggle.colorTintOnValueChange=0` /
                    // `changeSpriteOnValueChange=1`，判据见 `Shell/PlayerProfileWindow.cs` 文件头 ①）
                    var texOn = CardArt.MenuUi(PlayerProfileWindow.ArtTabOn);
                    var texOff = CardArt.MenuUi(PlayerProfileWindow.ArtTabOff);
                    CheckTrue(texOn != null && texOff != null, "页签底两张图都在工程里（`40K_settings_button` / `_hover`）");
                    for (int i = 0; i < pp.ButtonBgs.Length; i++)
                    {
                        var bgq = pp.ButtonBgs[i];
                        bool shouldOn = i == PlayerProfileWindow.DefaultTabIndex;
                        CheckTrue(bgq != null && bgq.Texture == (shouldOn ? (Texture)texOn : texOff),
                                  $"键 {i} 的 `button_bg` 贴图 = **{(shouldOn ? "选中" : "未选")}**那张"
                                  + $"（{(shouldOn ? PlayerProfileWindow.ArtTabOn : PlayerProfileWindow.ArtTabOff)}）");
                        // `m_Color` **逐键不同**（第 1 键 a=1、其余 0.7098039388656616；rgb 六键都是 (1,0.5723677,0)）
                        float wantA = i == 0 ? 1f : 0.7098039388656616f;
                        CheckNear(bgq != null ? bgq.Tint.a : -1f, wantA, 1e-3f,
                                  $"键 {i} 的 `button_bg` alpha = **{wantA:F4}**（原版逐键真值，见 `PlayerProfileWindow.cs` 文件头的存疑那条）");
                    }

                    // 点第 1 个键 ⇒ 切到 `Profile` 页（换页只切 activeSelf，不重建）
                    {
                        var k0 = FindChild(bar2, PlayerProfileWindow.Tabs[0].Node);
                        var h0 = k0 != null ? FindChild(k0, "Hit") : null;
                        var w0 = h0 != null ? h0.GetComponent<WindowButton>() : null;
                        CheckTrue(w0 != null && w0.onClick != null, "第 1 个键有点击区");
                        if (w0 != null) w0.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileInfo, "点第 1 个键 ⇒ 切到 `Profile` 页");
                        CheckTrue(FindChild(t, "Profile Tab") != null && FindChild(t, "Profile Tab").gameObject.activeSelf,
                                  "切页之后 `Profile Tab` 页根 **active**");
                        CheckTrue(FindChild(t, "Title Tab") != null && !FindChild(t, "Title Tab").gameObject.activeSelf,
                                  "切页之后 `Title Tab` 页根 **关掉**");
                        CheckTrue(pp.ButtonBgs[0] != null && pp.ButtonBgs[0].Texture == texOn,
                                  "切页之后第 1 个键的底色 **翻成选中那张**");
                    }

                    // ---- Title 页（**出厂就打开的那一页**）----
                    {
                        var k2 = FindChild(bar2, PlayerProfileWindow.Tabs[2].Node);
                        var h2 = k2 != null ? FindChild(k2, "Hit") : null;
                        var w2 = h2 != null ? h2.GetComponent<WindowButton>() : null;
                        if (w2 != null) w2.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileTitle, "点第 3 个键 ⇒ 切回 `Title` 页");

                        // 🔴 **锚一律从页根开始** —— `FindChild(t, "Selected Item Panel")` 会先撞上
                        //    **别的页**的同名节点（六页都有 `Selected Item Panel` / `Item Display Panel` /
                        //    `Scroll Rect`）⇒ 第一版就这么红了三条（Title 块的断言全打在 Avatar 页上）。
                        var tp = FindChild(t, "Title Tab");
                        CheckTrue(tp != null, "`Title Tab` 页根在");
                        var sel = FindChild(tp, "Selected Item Panel");
                        CheckAtWorld(sel, 318.37f, 587.32f, 292.02f, 758.89f, "`Selected Item Panel`");
                        CheckAtWorld(FindChild(sel, "Avatar Name"), 322.54f, 587.32f, 516.41f, 631.55f,
                                     "`Avatar Name`（⚠️ 这一行装的是**称号名**，不是玩家名）");
                        CheckAtWorld(FindChild(sel, "Select Avatar Button"), 343.49f, 562.21f, 670.43f, 736.48f,
                                     "`Select Avatar Button`");
                        CheckText(TextOf(FindChild(sel, "Button Text")), "Select",
                                  "按钮文案 = `Select`（⚠️ **我们挑的**：原版那条是**葡语占位串 `Selecionar`**、还没挂 `Localize`）");
                        CheckTrue(FindChild(sel, "Toggle borde") == null,
                                  "`Toggle borde` **不建**（出厂 act=F + 全包无 MonoBehaviour 指向它 + 无 Animation ⇒ 死节点）");

                        var disp = FindChild(tp, "Item Display Panel");
                        CheckAtWorld(disp, 632.79f, 1701.49f, 210.69f, 868.61f, "`Item Display Panel`");
                        var bgBig = FindChild(disp, "Background");
                        CheckAtWorld(bgBig, 632.79f, 1701.49f, 210.69f, 868.61f,
                                     "面板底（`UI_Deck_Information_submenu_Back` · Sliced · 九宫 18,18,18,18）");
                        var ttl = FindChild(disp, "Select Item");
                        // 🔴 **别用 `CheckAtWorld` 断它** —— 原版这行是**左对齐**（`m_HorizontalAlignment = Left`、
                        //    垂直 Middle），我们照做了 `alignLeft` ⇒ `Label` 会被推到「左边缘落在 654.16」，
                        //    **不是**矩形中心（第一版就这么断的，报了 117.99px 的假失败：那个差刚好 = 标签宽的一半）。
                        {
                            var lbTtl = ttl != null ? ttl.GetComponent<Label>() : null;
                            float leftWorld = ttl != null ? ttl.position.x - (lbTtl != null ? lbTtl.WorldW * 0.5f : 0f) : -99f;
                            float wantLeft = MainMenuRuntime.Center(654.16f, 654.16f, 0f, 0f).x;
                            CheckTrue(Mathf.Abs(leftWorld - wantLeft) < 0.02f,
                                      $"`Select Item` **左对齐**到 x=654.16（原版 `m_HorizontalAlignment=Left`；实得 {leftWorld * 108f + 960f:F2}px）");
                            float wantCy = MainMenuRuntime.Center(0f, 0f, 147.51f, 210.70f).y;
                            CheckTrue(Mathf.Abs(ttl.position.y - wantCy) < 0.02f,
                                      $"`Select Item` 垂直居中在 y 147.51..210.70（实得 {(540f - ttl.position.y * 108f):F2}px）");
                        }
                        CheckText(TextOf(ttl), "Select your title", "小标题文案");
                        CheckTrue(ttl != null && bgBig != null && ttl.position.y > bgBig.position.y,
                                  "🔴 `Select Item` **溢在面板之上**（真值：它顶边 147.51 比面板顶 210.69 还高）—— 别「修正」成对齐");
                        var scr = FindChild(disp, "Scroll Rect");
                        CheckAtWorld(scr, 654.16f, 1680.12f, 210.69f, 855.46f,
                                     "`Scroll Rect` 视口（纵向 · Clamped · inertia=1 · elasticity=0.1）");
                        CheckAtWorld(FindChild(scr, "Item Drawer"), 654.16f, 1698.81f, 210.70f, 210.70f,
                                     "`Item Drawer` 内容容器（宽 1044.65 ⇒ **比视口宽 18.69、两侧各溢 9.35**）");
                        // 清单：**原版资产清单**（不是玩家存档）—— 由 `工具/gen_profile_cosmetics.py` 抽自
                        // `素材/Warpforge原版/装饰品/定义数据/` 的 462 个称号 SO。
                        CheckTrue(ProfileData.Loaded,
                                  "称号/头像清单**读进来了**（`Resources/profile_cosmetics.json`；读不到会 `LogError`）");
                        Check(TitleTab.Titles.Count, 462, "称号条数 = **462**（本地 SO 的实数，470 头像同理）");
                        CheckTrue(ProfileData.Avatars.Count == 469,
                                  $"头像条数 = **469**（实数；已排除 `Avatar_WF_*` 两张占位图）——实得 {ProfileData.Avatars.Count}");
                        CheckTrue(TitleTab.Titles.Count == 0 || TitleTab.Titles[0].Name.Length > 0,
                                  "称号有显示名（**我们是从资源名反推的**：`Title_UM_Premium_1` → `UM Premium 1`，"
                                  + "原版真名在远端 I2 语言表）");
                        // 表格按视口裁：462 条 × 3 列 = 154 行，一屏只该建出看得见的那几行
                        var grid = FindChild(scr, "Item Drawer");
                        int built = grid != null ? grid.childCount : -1;
                        CheckTrue(built >= 1 && built < 30,
                                  $"`Item Drawer` 底下**只建了看得见的格子**（实得 {built} 个；462 条全建会卡）");
                        // 🔴 **滚轮真的会重画**（2026-09-27 修：原来 `OnChanged` 是空的 ⇒ 滚了什么都不动，
                        //    而 462 条只建得出前几行 ⇒ **后面的称号根本够不到**，还是静默的）
                        {
                            var pl = PointerLayer.Instance;
                            float y0t = grid != null && grid.childCount > 0 ? grid.GetChild(0).position.y : 0f;
                            int n0t = grid != null ? grid.childCount : 0;
                            bool hit = pl != null && pl.WheelAt(1167f, 533f, -120f);
                            float y1t = grid != null && grid.childCount > 0 ? grid.GetChild(0).position.y : 0f;
                            CheckTrue(hit, "滚轮落在称号网格上（`PointerLayer.WheelAt`）");
                            CheckTrue(y1t > y0t + 0.01f,
                                      $"滚一格 ⇒ **内容真的往上走了**（world y {y0t:F3} → {y1t:F3} = {(y1t - y0t) * 108f:F1}px）");
                            CheckTrue(grid != null && grid.childCount == n0t,
                                      $"滚动重建之后格子数不变（{n0t}）—— 重建是**先清再建**（不清会越滚越多）");
                            if (pl != null) pl.WheelAt(1167f, 533f, 120f);   // 滚回去，后面的断言要确定性
                        }
                    }

                    CheckTrue(pp.MissingArt.Count == 0,
                              "这一扇用到的图**一张都不缺**（缺的会列在 `MissingArt`："
                              + string.Join("、", pp.MissingArt.ToArray()) + "）");
                    CheckHoverSwap(pp.transform, "提示窗");
                    // ⚠️ **`pp.Close()` 挪到所有页断完之后**（原来在这儿，加了 Avatar 页之后它会先关窗）
                    // ---- Avatar 页 ----
                    // 🔴 **锚一律从页根开始**（`FindChild(t, "Selected Item Panel")` 会先撞上别的页的同名节点 ——
                    //    六页都有 `Selected Item Panel` / `Item Display Panel` / `Scroll Rect`）。
                    {
                        var k1 = FindChild(bar2, PlayerProfileWindow.Tabs[1].Node);
                        var h1 = k1 != null ? FindChild(k1, "Hit") : null;
                        var w1 = h1 != null ? h1.GetComponent<WindowButton>() : null;
                        if (w1 != null) w1.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileAvatar, "点第 2 个键 ⇒ 切到 `Avatar` 页");

                        var pg = FindChild(t, "Avatar Tab");
                        CheckTrue(pg != null, "`Avatar Tab` 页根在");
                        var sel2 = FindChild(pg, "Selected Item Panel");
                        CheckAtWorld(sel2, 318.37f, 587.32f, 292.02f, 758.89f, "`Avatar Tab/Selected Item Panel`");
                        var mi = FindChild(sel2, "Avatar Menu Item");
                        CheckAtWorld(mi, 318.37f, 585.45f, 360.15f, 633.73f, "`Avatar Menu Item`（大图预览那块）");
                        var ic2 = FindChild(mi, "Image Container");
                        CheckAtWorld(ic2, 318.37f, 585.45f, 360.15f, 573.73f, "`Image Container`（267.08×213.58）");
                        CheckAtWorld(FindChild(ic2, "Border"), 318.37f, 585.45f, 381.51f, 595.09f,
                                     "头像边框（`Player Profile Border` 256×286 · preserveAspect）");
                        var bighl = FindChild(ic2, "Highlight");
                        CheckTrue(bighl != null && !bighl.gameObject.activeSelf,
                                  "大图的 `Highlight` **恒关**（原版两个调用点都写死 `highlight=false`）");
                        CheckTrue(FindChild(ic2, "Image") != null && FindChild(ic2, "Image").GetComponentInChildren<ImageQuad>() != null,
                                  "大图那一层**真的画出来了**（有 `ImageQuad`，不是空节点）");
                        CheckAtWorld(FindChild(mi, "Avatar Name"), 303.37f, 600.45f, 253.13f, 321.03f,
                                     "大图的 `Avatar Name`（⚠️ 比父件宽 —— 真值）");
                        CheckTrue(TextOf(FindChild(mi, "Avatar Name")).Length > 0,
                                  "大图底下显示的是**选中那张头像的名字**");
                        CheckAtWorld(FindChild(sel2, "Select Avatar Button"), 343.49f, 562.21f, 670.43f, 736.48f,
                                     "`Select Avatar Button`");
                        CheckText(TextOf(FindChild(sel2, "Toggle borde") != null
                                         ? FindChild(FindChild(sel2, "Toggle borde"), "Button Text") : null),
                                  "Toggle Border",
                                  "`Toggle borde` 在**这一页是活的**（⚠️ 同一名字在 Title 页是死节点）—— 文案 `Toggle Border`");

                        var disp2 = FindChild(pg, "Item Display Panel");
                        CheckAtWorld(disp2, 632.79f, 1701.49f, 210.69f, 868.61f, "`Avatar Tab/Item Display Panel`");
                        CheckText(TextOf(FindChild(disp2, "Select Item")), "Select your avatar", "小标题文案 = `Select your avatar`");
                        var scr2 = FindChild(disp2, "Scroll Rect");
                        CheckAtWorld(scr2, 654.16f, 1680.12f, 210.69f, 855.46f, "`Scroll Rect` 视口（与 Title 页同值）");
                        var grid2 = FindChild(scr2, "Item Drawer");
                        CheckAtWorld(grid2, 654.16f, 1698.81f, 210.70f, 210.70f, "`Item Drawer`（宽 1044.65）");
                        // 网格参数：`GridLayoutGroup` cell 180×180 · spacing (25,50) · pad L13 T40 ⇒ **5 列**
                        Check(AvatarTab.Columns, 5,
                              "头像网格 **5 列** = `floor((1044.65 − 13 + 25) ÷ (180 + 25))`（原版 `GridLayoutGroup` 柔性格）");
                        var av = pp.Page(WindowTabType.ProfileAvatar) as AvatarTab;
                        CheckTrue(av != null && av.BuiltCells > 0 && av.BuiltCells < 40,
                                  $"头像网格**只建了看得见的格子**（实得 {(av != null ? av.BuiltCells : -1)} 个；469 条全建会卡）");
                        Check(ProfileData.Avatars.Count, 469, "头像清单 **469** 条（原版可选头像 SO 的实数）");
                        // 🔴 **滚轮真的会重画**（2026-09-27 修，同称号页那一条）：
                        //    469 条只建得出前两行 ⇒ 不接 `OnChanged` 就等于「后面的头像根本够不到」。
                        {
                            var pl = PointerLayer.Instance;
                            var cell0 = grid2 != null && grid2.childCount > 0 ? grid2.GetChild(0) : null;
                            var img0 = cell0 != null ? FindChild(cell0, "Image") : null;
                            float y0a = cell0 != null ? cell0.position.y : 0f;
                            float iy0 = img0 != null ? img0.position.y : 0f;
                            int n0a = grid2 != null ? grid2.childCount : 0;
                            bool hit = pl != null && pl.WheelAt(1167f, 533f, -120f);
                            cell0 = grid2 != null && grid2.childCount > 0 ? grid2.GetChild(0) : null;
                            img0 = cell0 != null ? FindChild(cell0, "Image") : null;
                            float y1a = cell0 != null ? cell0.position.y : 0f;
                            float iy1 = img0 != null ? img0.position.y : 0f;
                            CheckTrue(hit, "滚轮落在头像网格上（`PointerLayer.WheelAt`）");
                            CheckTrue(y1a > y0a + 0.01f,
                                      $"滚一格 ⇒ **内容真的往上走了**（world y {y0a:F3} → {y1a:F3} = {(y1a - y0a) * 108f:F1}px）");
                            CheckTrue(grid2 != null && grid2.childCount == n0a,
                                      $"滚动重建之后格子数不变（{n0a}）—— 重建是**先清再建**（不清会越滚越多）");
                            CheckTrue(iy1 > iy0 + 0.01f,
                                      $"格子里那一层**跟着格子一起走**（world y {iy0:F3} → {iy1:F3}）"
                                      + " —— 原来子件用的是**未偏移**坐标（底板走了、图与字留在原地）");
                            if (pl != null) pl.WheelAt(1167f, 533f, 120f);   // 滚回去
                        }

                        // 🆕 **滚动区登记表不许「只增不减」/ 死条目不许还能被滚到**（2026-09-27 修的那颗雷）。
                        //    原来 `HitScroll` 那句是 `if (s.Owner != null && !s.Owner.activeInHierarchy) continue;`
                        //    —— 宿主**被销毁**时 Unity 的假 null 让它**判不出**，死条目照样命中。
                        //    判据 → `项目任务.md` §〇 A ②；实现 → `PointerLayer.PruneScrolls` / `UnregisterOwnedBy`。
                        {
                            var pl2 = PointerLayer.Instance;
                            if (pl2 != null)
                            {
                                var host = new GameObject("scroll_probe_host");
                                var vp = new PxRect(100f, 100f, 300f, 300f);
                                var probe = new MenuScroll(vp, 0f, 500f) { Owner = host };
                                PointerLayer.RegisterScroll(probe);
                                int n1 = PointerLayer.ScrollCountForTest;
                                CheckTrue(pl2.ScrollUnder(200f, 200f) == probe,
                                          "刚登记的滚动区**能被滚到**（这块是活的）");
                                // 宿主一销毁 ⇒ 这条就是死的：不许再命中，而且**要从登记表里落下去**
                                Object.DestroyImmediate(host);
                                CheckTrue(pl2.ScrollUnder(200f, 200f) != probe,
                                          "★ 宿主**销毁之后**那个区**不再吃滚轮**（原来会因为 Unity 假 null 判不出 ⇒ 死条目照样命中）");
                                CheckTrue(PointerLayer.ScrollCountForTest < n1,
                                          $"★ 死条目**当场从登记表里落下去**（{n1} → {PointerLayer.ScrollCountForTest}）—— 登记表不许只增不减");
                                // `UnregisterOwnedBy`：窗口重建时按宿主一次性撤（`PlayerProfileWindow.Setup` 就用它）
                                var host2 = new GameObject("scroll_probe_host2");
                                var s1 = new MenuScroll(vp, 0f, 500f) { Owner = host2 };
                                var s2 = new MenuScroll(vp, 100f, 600f) { Owner = host2 };
                                PointerLayer.RegisterScroll(s1);
                                PointerLayer.RegisterScroll(s2);
                                int n2 = PointerLayer.ScrollCountForTest;
                                PointerLayer.UnregisterOwnedBy(host2);
                                CheckTrue(PointerLayer.ScrollCountForTest <= n2 - 2,
                                          $"★ `UnregisterOwnedBy` 一次把这个宿主名下的区全撤了（{n2} → {PointerLayer.ScrollCountForTest}）");
                                Object.DestroyImmediate(host2);
                            }
                        }
                    }

                    // ---- Profile 页（2026-09-27 建 · 第 1 页）----
                    // 判据：`资料/普查产出_0927/档案窗_Profile页.md`（§A 层×参数 + §A.1 activeSelf 绑定表 + §C 查不到的）。
                    // 🔴 三类断言**都要**（10·3 第 3 层）：① 建起来了（对原版 rect）② **该藏的时候藏住了**
                    //    ③ 交互真的通（改名那条四跳链的终点）。
                    // 🔴 **锚一律从页根开始**（`FindChild` 是深度优先找**第一个**同名 —— 六页同名节点很多）。
                    {
                        var k0p = FindChild(bar2, PlayerProfileWindow.Tabs[0].Node);
                        var h0p = k0p != null ? FindChild(k0p, "Hit") : null;
                        var w0p = h0p != null ? h0p.GetComponent<WindowButton>() : null;
                        if (w0p != null) w0p.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileInfo, "点第 1 个键 ⇒ 切回 `Profile` 页");

                        var pf = FindChild(t, "Profile Tab");
                        CheckTrue(pf != null, "`Profile Tab` 页根在");
                        var tab = pp.Page(WindowTabType.ProfileInfo) as ProfileTab;
                        CheckTrue(tab != null, "`Profile` 页的组件是 `ProfileTab`（**类名照原版**）");

                        // ① 顶部三件（§A 三行）
                        var pidN = FindChild(pf, "PlayerId");
                        CheckAtWorld(pidN, 351.03f, 913.41f, 867.38f, 907.38f,
                                     "`PlayerId`（锚 (.5,.5) · pos (−416.777,−346.916) · 尺寸 562.38×40）");
                        CheckAtWorld(FindChild(pidN, "Image"), 350.67f, 377.85f, 867.88f, 906.87f,
                                     "`PlayerId/Image`（复制图标 `40k_profile_icon_copy` 27×39 · preserveAspect）");
                        CheckText(TextOf(FindChild(pidN, "playerIdText")), ProfileTab.IdLine,
                                  "PlayerId 那一行 = **空态**（ⓒ：原版拷的是服务器上的 PlayfabId，我们一条都没有）");
                        var clN = FindChild(pf, "Consecutive login days");
                        CheckTrue(clN != null && !clN.gameObject.activeSelf,
                                  "`Consecutive login days` **建成但关着**（prefab 出厂 F + 全包查不到激活点）");
                        var ivN = FindChild(pf, "Invite to alliance");
                        CheckTrue(ivN != null && !ivN.gameObject.activeSelf,
                                  "`Invite to alliance` **建成但关着**（我们不在联盟 ⇒ 走 `Initialize` 末尾那条 `SetActive(false)`）");

                        // ② `Player Info`：头像 + 名号 + 等级
                        var piN = FindChild(pf, "Player Info");
                        CheckAtWorld(piN, 351.03f, 1186.98f, 168.16f, 320.54f, "`Player Info`");
                        var avN = FindChild(piN, "Avatar Item Small");
                        CheckAtWorld(avN, 351.03f, 510.19f, 168.16f, 320.54f, "`Avatar Item Small`");
                        var avIc = FindChild(avN, "Image Container");
                        CheckAtWorld(FindChild(avIc, "Highlight"), 351.03f, 513.52f, 164.63f, 281.50f,
                                     "头像 `Highlight`（`Player_Avatar_selected` · 出厂 T）");
                        CheckAtWorld(FindChild(avIc, "Border"), 351.03f, 510.19f, 179.66f, 294.68f,
                                     "头像 `Border`（⚠️ **比容器还高** —— 真值，别「对齐」掉）");
                        var avImg = FindChild(avIc, "Image");
                        var avBd = FindChild(avIc, "Border");
                        var avIq = avImg != null ? avImg.GetComponentInChildren<ImageQuad>() : null;
                        var avBq = avBd != null ? avBd.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(avIq != null && avIq.Texture != null,
                                  "头像立绘那一层**真的画出来了**（取自全工程**唯一**那份选中状态 `ProfileData.AvatarIndex`）");
                        CheckTrue(avIq != null && avBq != null && avIq.RenderQueue > avBq.RenderQueue,
                                  "★ 立绘的渲染队列**比边框高一档** —— `Player_Profile_Border` 那张图的中心是"
                                + "**不透明黑**（实测 RGBA (0,0,0,255)），反了就把立绘压成黑块"
                                + "（2026-09-27 修；实据 = 原版兄弟序 `Highlight → Border → Image`）");
                        CheckTrue(FindChild(avN, "Avatar Name") != null && !FindChild(avN, "Avatar Name").gameObject.activeSelf,
                                  "`Avatar Name` **恒关**（原版 `AvatarDisplay` 只 set_text、从不 SetActive）");

                        var wa = FindChild(piN, "Info Section with Alliance");
                        CheckTrue(wa != null && !wa.gameObject.activeSelf,
                                  "`Info Section with Alliance` **关**（出厂态就是 with-OFF / without-ON）");
                        var na = FindChild(piN, "Info Section without Alliance");
                        CheckTrue(na != null && na.gameObject.activeSelf, "`Info Section without Alliance` **开着**");
                        var nb = FindChild(FindChild(na, "Name and Title Holder"), "NameHolder");
                        CheckAtWorld(FindChild(nb, "Edit Name Button"), 510.19f, 563.29f, 168.16f, 217.99f,
                                     "`Edit Name Button`（53.10×49.83 · 钮底+描边+图标三层）");
                        string name0 = ProfileData.PlayerName;
                        CheckText(TextOf(FindChild(nb, "Player Name")), name0,
                                  "`Player Name` 显示的是**我们的名字源**（`ProfileData.PlayerName`）");
                        CheckText(TextOf(FindChild(na, "Player Title")), ProfileTab.PlaceholderTitle,
                                  "`Player Title` = **照抄原版预制体的占位串**（我们没有这个数据）");
                        var lvN = FindChild(piN, "Player Level");
                        CheckAtWorld(lvN, 453.15f, 506.27f, 259.19f, 312.31f, "`Player Level` 圆底（237×237）");
                        CheckText(TextOf(FindChild(lvN, "Player Level Text")), "-",
                                  "`Player Level Text` = `-`（ⓐ **照抄原版自己的空态串**，不是我们编的）");

                        // ③ `Ranking`：两张卡 + 传奇那一份（互斥支）
                        var rkN = FindChild(pf, "Ranking");
                        CheckAtWorld(rkN, 351.02f, 1137.12f, 327.70f, 857.21f, "`Ranking`（786.10×529.51）");
                        var crN = FindChild(rkN, "Current Rank");
                        CheckAtWorld(crN, 351.03f, 776.28f, 327.71f, 857.22f, "`Current Rank`（425.26×529.51）");
                        CheckAtWorld(FindChild(crN, "Generic Window Red Background Small"), 351.03f, 776.28f, 327.71f, 857.22f,
                                     "卡片底（`UI_Deck_Selection_Back_simple` · 九宫 197,0,199,0）");
                        var crCt = FindChild(crN, "Content");
                        CheckAtWorld(FindChild(crCt, "Title"), 381.55f, 742.53f, 339.65f, 387.65f, "`Current Rank/Title`");
                        CheckText(TextOf(FindChild(crCt, "Title")), "Current Rank", "标题 = `Current Rank`");
                        CheckAtWorld(FindChild(crCt, "RankTitleBG"), 374.42f, 749.67f, 387.65f, 447.65f,
                                     "`RankTitleBG`（`40K_main_rank_display` · a=0.918）");
                        CheckTrue(FindChild(crCt, "Timer") != null && !FindChild(crCt, "Timer").gameObject.activeSelf,
                                  "`Current Rank/Timer` **关着**（原版 `RankingDisplay` 只填内容、从不 SetActive 它）");
                        var msN = FindChild(FindChild(FindChild(crCt, "footer"), "MainRating"), "Mission Milestones Progress");
                        CheckTrue(msN != null, "`Mission Milestones Progress` 挂在 **`MainRating` 下面**（出厂 T 的那一份才有）");
                        Check(FindChild(msN, "steps") != null ? FindChild(msN, "steps").childCount : -1, 4,
                              "四个 `RankedSealStep`（48.30×60.33）—— 原版那层 tint 是 (1,1,1,**0**)，我们**不点亮**（没有段位数据）");
                        var hrN = FindChild(rkN, "Highest Rank");
                        CheckAtWorld(hrN, 777.72f, 1137.12f, 327.71f, 857.22f, "`Highest Rank`（359.40×529.51）");
                        CheckAtWorld(FindChild(hrN, "Generic Window Red Background Small"), 777.72f, 1137.12f, 327.71f, 857.22f,
                                     "卡片底（`UI_Deck_Selection_Back` · 九宫 0,325,0,35）");
                        CheckAtWorld(FindChild(FindChild(hrN, "Content"), "RankTitleBG"), 727.24f, 1184.44f, 388.49f, 448.49f,
                                     "`Highest Rank/RankTitleBG`（⚠️ **比卡片还宽、右溢出 47.32** —— 真值）");
                        var hrMr = FindChild(FindChild(FindChild(hrN, "Content"), "footer"), "MainRating");
                        CheckTrue(hrMr != null && !hrMr.gameObject.activeSelf,
                                  "`Highest Rank/MainRating` **关**（出厂 F：`displaySeals=0` —— 两份卡逐项不同）");
                        var lgN = FindChild(rkN, "Legendary Display Profile");
                        CheckTrue(lgN != null && !lgN.gameObject.activeSelf,
                                  "`Legendary Display Profile` **关**（与 `Highest Rank` 互斥的两支；出厂两份都是 T ⇒ 出厂态不是运行态）");

                        // ④ `Events` 三格（⚠️ 第一格名字里是**两个空格**）
                        var evN = FindChild(pf, "Events");
                        CheckAtWorld(evN, 1047.88f, 1730.12f, 327.71f, 857.22f, "`Events`");
                        var wcN = FindChild(evN, "Warlord  Mastery Container");
                        CheckTrue(wcN != null, "`Warlord  Mastery Container`（**名字里两个空格** —— 原版真值）");
                        CheckAtWorld(wcN, 1175.12f, 1730.12f, 327.71f, 502.71f, "战将精通那格（555×175）");
                        CheckText(TextOf(FindChild(wcN, "Title")), "Highest Warlod Mastery",
                                  "标题**照抄预制体字面值**（含原版自己的拼写 `Warlod`）");
                        CheckAtWorld(FindChild(evN, "Forge Profile Container"), 1175.12f, 1730.12f, 507.71f, 682.71f,
                                     "锻造厂那格（底图 `40K_profile_ForgeLevel_bg` · Simple）");
                        CheckAtWorld(FindChild(evN, "Campaign Profile Container"), 1175.12f, 1730.12f, 687.71f, 862.71f,
                                     "战役那格");
                        CheckText(TextOf(FindChild(FindChild(evN, "Forge Profile Container"), "ArmyName")), "",
                                  "三格的**数据留空**（ArmyName / Level 都不编数字 —— 用户口径「数据可以空着」）");
                        CheckText(TextOf(FindChild(FindChild(evN, "Campaign Profile Container"), "Title")),
                                  "Current campaign",
                                  "战役那格标题照抄预制体（Forge 那格原版也是这句 —— **疑似原版复制粘贴**，照抄不擅自改）");

                        // ⑤ `ChooseNameWindow`（内嵌的**全屏**改名窗）
                        var cnw = FindChild(pf, "ChooseNameWindow");
                        CheckTrue(cnw != null && !cnw.gameObject.activeSelf,
                                  "`ChooseNameWindow` **出厂关**（`ProfileTab.Start` 显式 SetActive(false)）");
                        CheckAtWorld(cnw, -0.13f, 1920.13f, 0f, 1080f,
                                     "它**铺满整屏**（比页根还大 —— 真值；它是窗，不是子面板）");
                        CheckAtWorld(FindChild(cnw, "Generic Popup Background"), 519.49f, 1400.51f, 395.00f, 696.03f,
                                     "面板（`40k_popup` · 九宫 169,160,169,160）");
                        CheckAtWorld(FindChild(cnw, "Choose Name Input Field"), 540.13f, 1376.29f, 496.00f, 556.00f,
                                     "输入框（`40K_dropdown_bg` · 九宫 23,20,23,20）");
                        CheckText(TextOf(FindChild(cnw, "MessageText")), "Choose your player name", "文案 = `Choose your player name`");
                        CheckText(TextOf(FindChild(FindChild(cnw, "Change Name Button"), "Button Text")), "Free",
                                  "按钮文案 = `Free`（原版两套参数里的**首次改名**那一套）");
                        var pdN = FindChild(FindChild(cnw, "Change Name Button"), "Price Display");
                        CheckTrue(pdN != null && !pdN.gameObject.activeSelf,
                                  "`Price Display` **关**（原版 `TimesNameChange>=1` 才开）");

                        // ⑥ 交互：改名那条四跳链的终点（判据 ③）
                        if (tab != null)
                        {
                            var ehN = FindChild(nb, "EditNameHit");
                            var ehb = ehN != null ? ehN.GetComponent<WindowButton>() : null;
                            CheckTrue(ehb != null && ehb.onClick != null, "`Edit Name Button` **接了点击**");
                            if (ehb != null) ehb.Click();
                            CheckTrue(tab.NameWindowOpen, "点 `Edit Name Button` ⇒ **改名窗开了**");
                            CheckTrue(cnw != null && cnw.gameObject.activeSelf, "改名窗那一层的 `activeSelf` 也真的翻了");
                            tab.UiSetName("Test Commander");
                            CheckTrue(!tab.NameWindowOpen, "提交之后**窗自己关掉**");
                            CheckText(TextOf(FindChild(nb, "Player Name")), "Test Commander",
                                      "`Player Name` **真的换了**（走的是全工程**唯一**那个写点）");
                            CheckText(ProfileData.PlayerName, "Test Commander",
                                      "名字源也换了（**联机层的显示名读的是同一个源** —— 两处写同一条规则就会不一致）");
                            tab.UiSetName("");
                            CheckText(ProfileData.PlayerName, "Test Commander", "空名字**不被接受**（原版那道校验）");
                            tab.UiSetName(name0);       // 还原（后面的断言还会看这个名字）
                            CheckText(ProfileData.PlayerName, name0, "名字已还原");
                        }
                    }
                    // ---- Battle Log 页（2026-09-27 建 · 第 4 页）----
                    // 判据：`资料/普查产出_0927/档案窗_BattleLog与页签按钮.md`（§A·1 层×参数 · §B 行模板 `logPrefab`）。
                    // 🔴 它的行模板**就是后面「对局历史」那件要用的同一个**（正本 §B·5：`BattleLogPopup` 共用）。
                    {
                        var k3 = FindChild(bar2, PlayerProfileWindow.Tabs[3].Node);
                        var h3 = k3 != null ? FindChild(k3, "Hit") : null;
                        var w3 = h3 != null ? h3.GetComponent<WindowButton>() : null;
                        if (w3 != null) w3.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileBattleLog, "点第 4 个键 ⇒ 切到 `Battle Log` 页");

                        var bl = FindChild(t, "Battle Log Tab");
                        CheckTrue(bl != null, "`Battle Log Tab` 页根在");
                        var tab3 = pp.Page(WindowTabType.ProfileBattleLog) as BattleLogTab;
                        CheckTrue(tab3 != null, "这一页的组件是 `BattleLogTab`（类名照原版）");

                        var mtN = FindChild(bl, "Matches");
                        CheckAtWorld(mtN, 326.03f, 1771.97f, 162.84f, 904.48f,
                                     "`Matches`（`ScrollRect` —— ⚠️ **比页根宽 25、两侧各溢**，`sizeDelta=(50,−101.441)`）");
                        var vpN = FindChild(mtN, "Viewport");
                        CheckAtWorld(vpN, 326.03f, 1771.97f, 162.84f, 887.48f,
                                     "`Viewport`（`Mask` + `showGraphic=0` ⇒ 只建节点、不画）");
                        CheckAtWorld(FindChild(vpN, "Content"), 326.03f, 1771.97f, 162.84f, 162.84f,
                                     "`Content`（`VerticalLayoutGroup` spacing 25 · UpperLeft · 锚在顶边）");

                        // ---- ①b 🔴 **我们自己加的入口**（用户 2026-09-27 拍板「接在档案窗 `Battle Log` 页」）----
                        //  原版那扇 `Battle Log Popup` 的**打开点查不到**（只在 `WindowsManager` 预载表里，
                        //  `OpenWindow<BattleLogPopup>()` 的泛型调用产物缺失 ⇒ 我们**不编入口**了整整一轮）；
                        //  这一颗是用户拍板补的 ⇒ 断「**它在了 + 不压列表 + 点了真开**」，并**标明它不是复刻**。
                        {
                            var eb = FindChild(bl, "Open Log Popup Button");
                            CheckTrue(eb != null, "`Open Log Popup Button`（**这一颗是我们加的** —— 原版那一页没有它）");
                            CheckAtWorld(eb, 1506.97f, 1746.97f, 122.92f, 158.92f,
                                         "入口按钮的 rect（右上对齐内容区右缘 1746.97、落在**列表上方那条空带**里）");
                            // 🔴 **最要紧的一条**：它**不许压到列表**（列表顶 162.84）—— 这是这颗钮唯一会犯的错
                            float bY2 = 0f;
                            {
                                var br = eb != null ? eb.GetComponentInChildren<ImageQuad>() : null;
                                if (br != null) bY2 = LayoutSpace.PxY(eb.position.y) + br.WorldH * 108f * 0.5f;
                            }
                            CheckTrue(eb != null && bY2 > 0f && bY2 <= 162.84f,
                                      $"入口按钮**下沿在列表之上**（下沿 {bY2:F2} ≤ 162.84，不压第一行）");
                            var ebHit = eb != null ? FindChild(eb, "Hit") : null;
                            var ebBtn = ebHit != null ? ebHit.GetComponent<WindowButton>() : null;
                            CheckTrue(ebBtn != null && ebBtn.onClick != null, "入口有点击、且绑了动作");
                            if (ebBtn != null) ebBtn.Click();
                            var pop = BattleLogPopup.LastOpened;
                            CheckTrue(pop != null && pop.CurrentState == WindowState.Open,
                                      "★ 点它 ⇒ **真的开了 `Battle Log Popup`**（原来界面里根本进不去）");
                            if (pop != null) pop.Close();
                        }
                        Shoot("10_档案窗_BattleLog页.png");   // 给下个会话留一张：**那颗我们加的入口长什么样**

                        // ① 空态：本地没有对局记录（原版读服务器）⇒ 照原版**留空**，不造空态文案
                        Check(BattleLogData.Count, 0, "本地对局记录 **0 条**（原版在 `PlayerDataManager.battleLogData`）");
                        CheckTrue(FindChild(vpN, "Content") != null && FindChild(vpN, "Content").childCount == 0,
                                  "⇒ `Content` 底下**一行都不建**（原版也没有空态节点，`OnOpen` 只是清空）");

                        // ② 行模板：喂一条 ⇒ 逐件对 §B·2 那棵树（**这就是「对局历史」要复用的那一份**）
                        if (tab3 != null)
                        {
                            BattleLogData.Add(new BattleLogData.Match
                            {
                                Result = BattleLogData.Outcome.Victory,
                                OwnHeroName = "Uriel Ventris", EnemyHeroName = "Ghazghkull Thraka",
                                OwnName = "Test Commander", EnemyName = "Bot",
                                OwnSkulls = 3, EnemySkulls = 1,
                                OwnScore = "987 (+12)", EnemyScore = "Gold IV", Mode = "Skirmish mode",
                            });
                            tab3.RebuildRows();
                            Check(tab3.BuiltRows, 1, "喂一条 ⇒ **建出一行**（原版是 `Instantiate(logPrefab, holder)`）");
                            var row = FindChild(FindChild(vpN, "Content"), "Match Log");
                            CheckAtWorld(row, 326.03f, 1771.97f, 162.84f, 366.04f,
                                         "行矩形（行高 203.20、从 `Content` 顶边起）");
                            CheckText(TextOf(FindChild(row, "Result")), BattleLogData.ResultText(BattleLogData.Outcome.Victory),
                                      "结果那行 = `CardText.Phrase(...)`（**与结算面板同一个源**，不是另写一份）");
                            var pInfo = FindChild(row, "Player Info");
                            var eInfo = FindChild(row, "Enemy Info");
                            // 🔴 对齐**不能断中心**：原版这几行是 `Left`/`Right` 对齐 ⇒ `MenuDraw.AlignLeft/AlignRight`
                            //    会把整块字挪到矩形的那一条边上（Title 页那条教训），断中心会报假失败。
                            var ownHero = FindChild(pInfo, "Hero Name");
                            var ownHeroLb = ownHero != null ? ownHero.GetComponent<Label>() : null;
                            float ownRightPx = ownHero != null && ownHeroLb != null
                                ? (ownHero.position.x + ownHeroLb.WorldW * 0.5f) * 108f + 960f : -1f;
                            CheckTrue(Mathf.Abs(ownRightPx - 901f) < 0.5f,
                                      $"我方 `Hero Name` **右对齐到 x=901**（原版 `Right/Middle`；实得 {ownRightPx:F2}）");
                            var foeHero = FindChild(eInfo, "Hero Name");
                            var foeHeroLb = foeHero != null ? foeHero.GetComponent<Label>() : null;
                            float foeLeftPx = foeHero != null && foeHeroLb != null
                                ? (foeHero.position.x - foeHeroLb.WorldW * 0.5f) * 108f + 960f : -1f;
                            CheckTrue(Mathf.Abs(foeLeftPx - 1197f) < 0.5f,
                                      $"敌方 `Hero Name` **左对齐到 x=1197**（原版 `Left/Middle` —— 两份不是镜像，逐条不同；实得 {foeLeftPx:F2}）");
                            CheckAtWorld(FindChild(pInfo, "Score Icon"), 389.54f, 454.54f, 271.94f, 336.94f, "我方段位图标");
                            CheckAtWorld(FindChild(pInfo, "Skulls"), 887.52f, 987.52f, 254.44f, 354.44f, "我方骷髅（100×100）");
                            CheckText(TextOf(FindChild(pInfo, "skullCounter")), "x3", "骷髅数 `x3`");
                            CheckAtWorld(FindChild(row, "ReplayButton"), 1675.47f, 1740.47f, 188.86f, 253.86f,
                                         "`ReplayButton`（65×65 · `40k_general_bt_yellow`）");
                            CheckAtWorld(FindChild(row, "PinButton"), 1597.60f, 1662.60f, 188.86f, 253.86f, "`PinButton`");
                            CheckAtWorld(FindChild(FindChild(row, "Details"), "Sword"), 999.00f, 1099.00f, 254.44f, 354.44f,
                                         "中间那把剑（`40k_main_bt_play`）");
                            // 钉住色：原版两个常量（未钉 (1,1,1,1) / 已钉 (0,1,0,1)），实读自 DLL
                            var pinIc = FindChild(FindChild(row, "PinButton"), "pinicon");
                            var pinQ = pinIc != null ? pinIc.GetComponent<ImageQuad>() : null;
                            CheckTrue(pinQ != null && Mathf.Abs(pinQ.Tint.g - 1f) < 1e-3f && Mathf.Abs(pinQ.Tint.r - 1f) < 1e-3f,
                                      "未钉 ⇒ `pinicon` 是 **(1,1,1,1)**（原版 `SetPinState` 的常量）");
                            var pinHit = FindChild(FindChild(row, "PinButton"), "Hit");
                            var pinBtn = pinHit != null ? pinHit.GetComponent<WindowButton>() : null;
                            CheckTrue(pinBtn != null && pinBtn.onClick != null, "`PinButton` 接了点击");
                            if (pinBtn != null) pinBtn.Click();
                            CheckTrue(pinQ != null && pinQ.Tint.r < 1e-3f && Mathf.Abs(pinQ.Tint.g - 1f) < 1e-3f
                                      && pinQ.Tint.b < 1e-3f,
                                      "钉住 ⇒ **变绿 (0,1,0,1)**（真值，不是我们挑的颜色）");
                            // 两条必须出声的（原版那两条都要吃服务器/回放，我们都没有）
                            var repHit = FindChild(FindChild(row, "ReplayButton"), "Hit");
                            var repBtn = repHit != null ? repHit.GetComponent<WindowButton>() : null;
                            CheckTrue(repBtn != null && repBtn.onClick != null, "`ReplayButton` 接了点击（点了会**出声说回放没做**）");
                            var foeHit = FindChild(eInfo, "EnemyClickHit");
                            var foeBtn = foeHit != null ? foeHit.GetComponent<WindowButton>() : null;
                            CheckTrue(foeBtn != null && foeBtn.onClick != null,
                                      "敌方那块（`UIGenericEventCatcher`）也接了点击（点对手会出声说没有档案）");
                            // 清干净（后面的断言与别的自检不该看到这条假数据）
                            BattleLogData.ResetForTest();
                            tab3.RebuildRows();
                            Check(tab3.BuiltRows, 0, "清空之后**回到空态**（自检不留下假数据）");

                            // ---- 🆕 2026-10-03：**视口外的整行不建** ----
                            //   ⚠️ 这一页**本来就有**这道守卫（`BattleLogTab.cs:182` 的 `_scroll.Intersects`）——
                            //   本件是给 `BattleLogPopup` 那棵树补上同一道（那边一直缺）；这里把它**钉住**，
                            //   顺带钉「整行在视口外 ⇒ 连它的 `Hit` 一起不建」（等价原版 `RectMask2D` 裁掉的部分点不到）。
                            //   🔴 期望值**现算**（`RowsInViewport`）：视口 162.84..887.48（上面 `CheckAtWorld(vpN, …)`
                            //   刚按原版值钉过）、行距 = 原版 203.20 + 25。⛔ 不写死条数。
                            {
                                const int NF = 5;      // 现算：视口高 724.64、行距 228.2 ⇒ 第 5 行整行落在视口外
                                for (int i = 0; i < NF; i++)
                                    BattleLogData.Add(new BattleLogData.Match
                                    {
                                        Result = BattleLogData.Outcome.Victory,
                                        OwnHeroName = "Feed " + i, EnemyHeroName = "Bot",
                                        OwnName = "Feed " + i, EnemyName = "Bot",
                                        OwnSkulls = 1, EnemySkulls = 0, OwnScore = "1", EnemyScore = "2",
                                        Mode = "Skirmish mode",
                                    });
                                tab3.RebuildRows();
                                const float FTop = 162.84f, FBot = 887.48f;
                                float fpitch = OrigMatchRowH + OrigMatchRowGap;     // 原版：203.20 + 25
                                int wantF = RowsInViewport(NF, FTop, FBot, fpitch, OrigMatchRowH, 0f);
                                Check(tab3.BuiltRows, wantF, $"喂 {NF} 行 ⇒ 建了与视口相交的那 {wantF} 行（现算）");
                                CheckTrue(wantF > 0 && wantF < NF,
                                          $"…而且**确实有整行落在视口外**（{NF - wantF} 行连节点一起不建 —— 它的 `Hit` 也不存在）");
                                var fc = FindChild(vpN, "Content");
                                int fn = 0, fout = 0; float fmin = float.MaxValue;
                                if (fc != null)
                                    foreach (var rt in fc.GetComponentsInChildren<Transform>(true))
                                    {
                                        if (rt.name != "Match Log") continue;
                                        fn++;
                                        float cy = LayoutSpace.PxY(rt.position.y);
                                        float y1 = cy - OrigMatchRowH * 0.5f, y2 = cy + OrigMatchRowH * 0.5f;
                                        fmin = Mathf.Min(fmin, y1);
                                        if (y2 <= FTop + 0.01f || y1 >= FBot - 0.01f) fout++;
                                    }
                                Check(fn, tab3.BuiltRows, "行节点个数 == `BuiltRows`（两者不许各说各的）");
                                CheckTrue(fout == 0, $"每一颗建出来的行都与视口相交（越界 {fout} 颗）");
                                CheckNear(fmin, FTop, 0.5f,
                                          "最上面那颗行的顶边 = 视口顶 162.84（顺带证明滚动偏移是 0 —— 上面的期望值按它现算）");
                                BattleLogData.ResetForTest();
                                tab3.RebuildRows();
                                Check(tab3.BuiltRows, 0, "清空 ⇒ 回到空态（不留假数据）");
                            }
                        }
                    }
                    // ---- Trophies 页（2026-09-27 建 · 第 5 页）----
                    // 判据：`资料/普查产出_0927/档案窗_Trophies页.md`（§A·1 层×参数 · §A·4 三个运行时生成的族 · §C3 102 条成就）。
                    {
                        var k4 = FindChild(bar2, PlayerProfileWindow.Tabs[4].Node);
                        var h4 = k4 != null ? FindChild(k4, "Hit") : null;
                        var w4 = h4 != null ? h4.GetComponent<WindowButton>() : null;
                        if (w4 != null) w4.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileTrophies, "点第 5 个键 ⇒ 切到 `Trophies` 页");

                        var tr = FindChild(t, "Trophies Tab");
                        CheckTrue(tr != null, "`Trophies Tab` 页根在");
                        var tab4 = pp.Page(WindowTabType.ProfileTrophies) as AchievementsMenu;
                        CheckTrue(tab4 != null, "这一页的组件是 `AchievementsMenu`（**原版类名**：键名叫 Trophies、文案是 Achievements）");

                        CheckAtWorld(FindChild(tr, "bg"), 635.16f, 1746.96f, 213.16f, 891.68f,
                                     "`bg`（`UI_Deck_Information_submenu_Back` · 九宫 18,18,18,18）");
                        var btns = FindChild(tr, "buttons");
                        CheckAtWorld(btns, 351.04f, 635.15f, 230.64f, 928.28f,
                                     "`buttons`（`ToggleGroup` + VLG spacing 20 · **UpperLeft**）");
                        // 🔴 **运行时是 4 个分类，不是预制体里烘焙的 5 个**（`Enum.GetValues` 按值升序 ⇒ Battle→Collection→Victories→Account）
                        Check(btns != null ? btns.childCount : -1, 4,
                              "四个分类页签（**不是预制体里那 5 个** —— 运行时 `DestroyAllChildren` 后按枚举重建，判据 ①）");
                        var texOnT = CardArt.MenuUi(AchievementsMenu.ArtTabOn);
                        var texOffT = CardArt.MenuUi(AchievementsMenu.ArtTab);
                        CheckTrue(texOnT != null && texOffT != null, "页签底两张图在工程里（复用设置窗那两张）");
                        var names4 = new[] { "Battle", "Collection", "Victories", "Account" };
                        for (int i = 0; i < 4; i++)
                        {
                            float top = 230.64f + 120f * i;
                            var tg = FindChild(btns, i == 0 ? "Achievement Type Toggle" : "Achievement Type Toggle (" + i + ")");
                            CheckAtWorld(tg, 351.04f, 635.15f, top, top + 100f,
                                         $"分类键 {i}（284.11×100、步进 120 —— 宽被布局组强制成父宽，**不是 prefab 自己那 333.301**）");
                            CheckText(TextOf(FindChild(tg, "Tab Toggle Title")), names4[i],
                                      $"分类 {i} 文案 = `{names4[i]}`（⚠️ **我们挑的**：原版是 I2 词条 `Achievements/Types/<名>`，译文在远端查不到；"
                                      + "预制体里那 4 个 `'Secret'` 是占位）");
                            var bgq = FindChild(tg, "button_bg");
                            var q = bgq != null ? bgq.GetComponent<ImageQuad>() : null;
                            bool on = i == 0;      // 出厂选中 `Battle`（ctor 写死 `filter = 1`）
                            CheckTrue(q != null && q.Texture == (on ? (Texture)texOnT : texOffT),
                                      $"分类键 {i} 底色贴图 = **{(on ? "选中" : "未选")}**那张（换图不换色，与左栏六键同一条判据）");
                        }
                        Check(tab4 != null ? tab4.Filter : 0, ProfileData.TypeBattle, "出厂选中 **Battle**");

                        CheckAtWorld(FindChild(tr, "Scroll"), 635.14f, 1746.98f, 213.16f, 891.69f, "`Scroll`（`ScrollRect` v=1 · mode=2 Elastic）");
                        CheckAtWorld(FindChild(FindChild(tr, "Scroll"), "Viewport"), 635.14f, 1746.98f, 216.14f, 891.69f,
                                     "`Viewport`（**`RectMask2D`** —— 与 Battle Log 那页的 `Mask` 不同）");
                        var holder = FindChild(FindChild(FindChild(tr, "Scroll"), "Viewport"), "ContainerHolder");
                        CheckTrue(holder != null && Mathf.Abs(holder.position.y
                                  - MainMenuRuntime.Center(0f, 0f, 200.27f, 200.27f).y) < 0.02f,
                                  "`ContainerHolder` 顶 = **200.27**（⚠️ 比视口顶 216.14 还高 15.87 —— 真值）");
                        Check(AchievementsMenu.Columns, 2, "网格 **2 列** = ⌊(1111.82 + 10) ÷ (520 + 10)⌋（原版 `GridLayoutGroup` Flexible）");

                        // 数据：本地 **102 条**（`AllAchievements` + `ACH1..102` 两个 SO）；类型分布 36/28/30/8
                        Check(ProfileData.Achievements.Count, 102, "成就 **102 条**（原版资产实数）");
                        Check(ProfileData.OfType(ProfileData.TypeBattle).Count, 36, "Battle **36** 条");
                        Check(ProfileData.OfType(ProfileData.TypeCollection).Count, 28, "Collection **28** 条");
                        Check(ProfileData.OfType(ProfileData.TypeVictories).Count, 30, "Victories **30** 条");
                        Check(ProfileData.OfType(ProfileData.TypeAccount).Count, 8, "Account **8** 条");

                        if (tab4 != null)
                        {
                            CheckTrue(tab4.BuiltCells > 0 && tab4.BuiltCells < 12,
                                      $"一屏**只建看得见的格子**（实得 {tab4.BuiltCells} 个；36 条全建会卡）");
                            var c0 = FindChild(holder, "Achievement Container");
                            CheckAtWorld(c0, 666.05f, 1186.05f, 232.27f, 382.27f,
                                         "第一个格子（520×150 · 横向余量 61.82 ⇒ 左内缩 **30.91**）");
                            var a0 = ProfileData.OfType(ProfileData.TypeBattle)[0];
                            CheckText(TextOf(FindChild(c0, "title")), a0.Name + " 1/5",
                                      "`title` = `{名} {档}/{总档}`（原版格式串 `{0} {1}/{2}`；我们没进度 ⇒ 恒第 1 档）");
                            CheckText(TextOf(FindChild(c0, "description")), a0.Challenge,
                                      "`description` = **`challenge` 的 id**（真字符串；⚠️ 原版那格是 I2 词条，译文查不到 —— 我们拿它顶）");
                            CheckTrue(TextOf(FindChild(c0, "rewards")).EndsWith(" points"),
                                      "`rewards` 照预制体 `'2 points'` 那个形式（真词条 `Achievements/Points` 在远端）");
                            var slN = FindChild(FindChild(c0, "Progress"), "Slider");
                            var cntN = slN != null ? FindChild(slN, "counter") : null;
                            CheckText(TextOf(cntN), "0/" + a0.Thresholds[0],
                                      "`counter` = `0/{该档阈值}`（格式串 `{0}/{1}`；阈值是资产里的真数）");
                            CheckAtWorld(slN != null ? FindChild(slN, "Background") : null,
                                         818.05f, 1038.74f, 334.82f, 361.00f,
                                         "进度条底（`40k_campaign_bar_bg` · 九宫 20,0,20,0）");
                            CheckTrue(FindChild(c0, "Image") != null
                                      && FindChild(c0, "Image").GetComponentInChildren<ImageQuad>() != null,
                                      "勋章图那一层**真的画出来了**（`40k_Achievements_icon_medal1`）");
                            // 计数条：**全量之和、不受筛选影响**（判据 ③）
                            CheckAtWorld(FindChild(tr, "Counter"), 1568.89f, 1703.96f, 161.10f, 202.22f, "`Counter` 计数条");
                            CheckText(TextOf(FindChild(FindChild(tr, "Counter"), "EverguildTextMeshPro")),
                                      AchievementsMenu.PointsText(),
                                      "计数条 = 全量成就积分之和（我们没进度 ⇒ `0`；原版 `ToString()` 无千分位）");

                            // 点第二个分类 ⇒ 换筛选 + 重建（原版 `NotifyToggleOn` → 写 filter → `Refresh()`）
                            var tg1 = FindChild(btns, "Achievement Type Toggle (1)");
                            var h1b = tg1 != null ? FindChild(tg1, "Hit") : null;
                            var w1b = h1b != null ? h1b.GetComponent<WindowButton>() : null;
                            CheckTrue(w1b != null && w1b.onClick != null, "第二个分类键接了点击");
                            if (w1b != null) w1b.Click();
                            Check(tab4.Filter, ProfileData.TypeCollection, "点了 `Collection` ⇒ `filter` 跟着变");
                            Check(holder != null ? holder.childCount : -1, tab4.BuiltCells,
                                  "切分类 ⇒ **重建成新筛的那批**（`Refresh()` 先清空再 Instantiate）");
                            var back = FindChild(btns, "Achievement Type Toggle");
                            var hb0 = back != null ? FindChild(back, "Hit") : null;
                            var wb0 = hb0 != null ? hb0.GetComponent<WindowButton>() : null;
                            if (wb0 != null) wb0.Click();      // 切回 Battle（后面不留状态）
                            Check(tab4.Filter, ProfileData.TypeBattle, "再点回 `Battle`");
                        }
                    }
                    // ---- Ranking 页（2026-09-27 建 · 第 6 页）----
                    // 判据：`资料/普查产出_0927/档案窗_Ranking页与图名表.md`（§A 层×参数 · **§A·4 两处偏差** · §A·6 乱序）。
                    {
                        var k5 = FindChild(bar2, PlayerProfileWindow.Tabs[5].Node);
                        var h5 = k5 != null ? FindChild(k5, "Hit") : null;
                        var w5 = h5 != null ? h5.GetComponent<WindowButton>() : null;
                        if (w5 != null) w5.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileRanking, "点第 6 个键 ⇒ 切到 `Ranking` 页");

                        var rk = FindChild(t, "Ranked Tab");
                        CheckTrue(rk != null, "`Ranked Tab` 页根在");
                        var tab5 = pp.Page(WindowTabType.ProfileRanking) as RankedTab;
                        CheckTrue(tab5 != null, "这一页的组件是 `RankedTab`（类名照原版）");

                        CheckAtWorld(FindChild(rk, "Profile Player Info"), 351.03f, 1186.98f, 168.16f, 320.54f,
                                     "`Profile Player Info`（与 Profile 页同构，但是**另一个实例**）");
                        CheckAtWorld(FindChild(FindChild(rk, "Profile Player Info"), "Player Level"), 453.15f, 506.27f, 259.19f, 312.31f,
                                     "`Player Level`（圆底 + `-`）");
                        CheckTrue(FindChild(FindChild(rk, "Profile Player Info"), "Info Section with Alliance") != null
                                  && !FindChild(FindChild(rk, "Profile Player Info"), "Info Section with Alliance").gameObject.activeSelf,
                                  "`Info Section with Alliance` **关**（与 Profile 页同一条互斥判据）");

                        var t4 = FindChild(rk, "Top4");
                        CheckAtWorld(t4, 351.03f, 1186.99f, 333.95f, 886.97f, "`Top4`（四格阵营分）");
                        CheckAtWorld(FindChild(t4, "bg"), 351.03f, 1186.99f, 333.95f, 886.97f, "`Top4/bg`");
                        var t4c = FindChild(t4, "content");
                        CheckAtWorld(t4c, 367.75f, 1170.27f, 345.01f, 875.91f, "`Top4/content`（`HorizontalLayoutGroup`）");
                        for (int i = 1; i <= 4; i++)
                        {
                            float top = (i == 1 || i == 3) ? 355.01f : 635.46f;
                            float left = (i <= 2) ? 374.36f + 10f : 953.66f + 10f;
                            CheckAtWorld(FindChild(rk, "#" + i + " FactionScoreBig"), left, left + 164f, top, top + 230.448f,
                                         $"`#{i} FactionScoreBig`（⚠️ 宽 **164 是推出来的** —— 表里 0.00 是 `ctrlW=1` 下算不准的首选宽，§A·4 偏差 3）");
                        }
                        // 🔴 `top4Factions` 的**数组顺序不是树序**（§A·6）—— 我们按树上的名字摆，并把这件事记死
                        CheckTrue(RankedTab.Top4Order[1] == "#3" && RankedTab.Top4Order[2] == "#2",
                                  "`top4Factions` 数组顺序 = `#1,#3,#2,#4`（**不是树序** —— 照树序抄会张冠李戴，§A·6）");

                        var ctr = FindChild(t4c, "center");
                        CheckAtWorld(ctr, 584.36f, 953.66f, 345.01f, 875.91f, "`center` 那一列");
                        CheckText(TextOf(FindChild(ctr, "DivisionText")), "Global Rating",
                                  "⚠️ 节点名叫 `DivisionText`，**装的是大标题 `Global Rating`**（照抄预制体）");
                        CheckAtWorld(FindChild(ctr, "DivisionImage"), 584.36f, 953.66f, 599.34f, 732.62f, "`DivisionImage`（段位大图形位）");
                        var gr = FindChild(FindChild(FindChild(FindChild(ctr, "footer"), "MainRating"), "Global Rating"), "Main Icon");
                        CheckAtWorld(gr, 591.29f, 591.29f + 58.6f, 770.59f, 827.94f,
                                     "中间列 `Main Icon` 的 x = **591.29**（**§A·4 的修正值**：表里那个 651.29 把出厂 F 的 `Secondary Icon` 也算进主轴了）");
                        CheckText(TextOf(FindChild(FindChild(FindChild(FindChild(ctr, "footer"), "MainRating"), "Global Rating"),
                                                   "Individual rating value")), RankedTab.ScoreEmpty,
                                  "全局评分 = **空态串 `------`**（照原版预制体自己的空态；`'32'`/`'3000'` 是示例数字，不抄）");

                        var af = FindChild(rk, "AllFactions");
                        CheckAtWorld(af, 1302.77f, 1746.97f, 264.81f, 886.97f, "`AllFactions`（阵营排行榜）");
                        CheckText(TextOf(FindChild(af, "Faction Ranking Points")), "Faction Rating", "表头文案 = `Faction Rating`");
                        CheckAtWorld(FindChild(af, "info"), 1686.86f, 1737.87f, 205.89f, 254.37f, "表头那个 info 图标");
                        CheckAtWorld(FindChild(af, "scroll rect"), 1305.29f, 1746.97f, 288.62f, 864.38f, "`scroll rect`");
                        var vp5 = FindChild(af, "viewport");
                        CheckAtWorld(vp5, 1305.29f, 1746.97f, 288.62f, 864.38f, "`viewport`（`RectMask2D`）");
                        var cont5 = FindChild(vp5, "content");
                        CheckAtWorld(cont5, 1305.29f, 1735.56f, 288.62f, 288.62f, "`content`（`VerticalLayoutGroup` + `ContentSizeFitter`）");
                        if (tab5 != null)
                        {
                            Check(tab5.BuiltRows, 4, "四行阵营排行（预制体里烘焙了 4 个 `FactionScoreSmall` 实例）");
                            var row0 = FindChild(cont5, "FactionScoreSmall");
                            CheckAtWorld(row0, 1305.29f, 1744.03f, 288.62f, 406.33f,
                                         "第 1 行（438.74×117.71；`content` 宽 430.27 ⇒ **行比容器宽 8.47**，真值）");
                            // 🔴 **2026-10-03 订正**：这几件**冒到视口顶之上**（`icon` 顶上冒 10.79），
                            //    而 `MenuDraw.Rect` 现在**横纵两轴都截 uv**（等价于原版 `viewport` 上那个
                            //    `RectMask2D` —— 原版**本来就会把它们裁掉**）⇒ 再拿「未裁整块的中心」去比
                            //    会差好几像素。**改成量渲出来的矩形**：**没被裁的那几条边**（下边缘 / 左右）
                            //    仍等于原版值，另外单独钉一条「上边缘真的被裁住了」。
                            {
                                float ix1, iy1, ix2, iy2;
                                var icn = FindChild(row0, "icon");
                                CheckTrue(RenderedRect(icn, out ix1, out iy1, out ix2, out iy2)
                                          && Mathf.Abs(ix1 - 1305.29f) < 1.5f && Mathf.Abs(ix2 - 1448.39f) < 1.5f
                                          && Mathf.Abs(iy2 - 406.33f) < 1.5f,
                                          "行里的 `icon`：左右 1305.29..1448.39 + 下边缘 406.33（原版值）—— 实测 "
                                          + $"x {ix1:F2}..{ix2:F2} · y {iy1:F2}..{iy2:F2}");
                                CheckTrue(iy1 > 277.83f + 0.5f,
                                          $"…上边缘**真被视口裁住了**（原版块顶 277.83，实渲 {iy1:F2}）");
                            }
                            var ard = FindChild(row0, "Alliance Rating Display");
                            {
                                // `Main Icon` 开了 `keepAspect` ⇒ **渲出来的宽比框窄**，只能比中心与下边缘
                                float mx1, my1, mx2, my2;
                                var mi = FindChild(ard, "Main Icon");
                                CheckTrue(RenderedRect(mi, out mx1, out my1, out mx2, out my2)
                                          && Mathf.Abs((mx1 + mx2) * 0.5f - (1295.14f + 1446.96f) * 0.5f) < 1.5f
                                          && Mathf.Abs(my2 - 384.23f) < 1.5f,
                                          "行里 `Main Icon`：**中心 x = 1371.05**（§A·4 的修正值）+ 下边缘 384.23 —— 实测 "
                                          + $"x {mx1:F2}..{mx2:F2} · y {my1:F2}..{my2:F2}");
                            }
                            var mx = FindChild(row0, "Alliance Rating Display (1)");
                            CheckTrue(mx != null, "`Alliance Rating Display (1)`（**最高分**那一行）建了");
                            var mxIc = FindChild(mx, "Main Icon");
                            var mxQ = mxIc != null ? mxIc.GetComponent<ImageQuad>() : null;
                            CheckTrue(mxQ != null && mxQ.Texture == (Texture)CardArt.MenuUi("Menu_Icon_Galon"),
                                      "最高分那一行的图标是 **`Menu_Icon_Galon`**（值那一行是 `40k_UI_icon_ranked_Skirmish` —— **两张不一样**，§A·2 末）");
                            // ⚠️ 这一栏**滚不动**是对的：4 行 × 117.711 = 470.84 < 视口高 575.76（`MenuScroll` 会夹到 0）
                            var pl5 = PointerLayer.Instance;
                            var r0 = FindChild(cont5, "FactionScoreSmall");
                            float y0r = r0 != null ? r0.position.y : 0f;
                            if (pl5 != null) pl5.WheelAt(1526f, 576f, -120f);
                            var r0b = FindChild(cont5, "FactionScoreSmall");
                            float y1r = r0b != null ? r0b.position.y : 0f;
                            CheckTrue(Mathf.Abs(y1r - y0r) < 0.001f,
                                      "内容比视口矮（4×117.711 = 470.84 < 575.76）⇒ **滚轮不动**才是对的（`MenuScroll` 夹到 0）");
                        }

                        // 点头像 ⇒ **跳到 Avatar 页**（原版 `RankedTab.<Initialize>b__10_0` 就是这条，§C·1 第 4 条）
                        var avHit = FindChild(FindChild(rk, "Profile Player Info"), "AvatarHit");
                        var avBtn = avHit != null ? avHit.GetComponent<WindowButton>() : null;
                        CheckTrue(avBtn != null && avBtn.onClick != null, "本页头像接了点击");
                        if (avBtn != null) avBtn.Click();
                        Check(pp.CurrentTab, WindowTabType.ProfileAvatar, "点本页头像 ⇒ **切到 `Avatar` 页**（原版行为）");
                        var back5 = FindChild(bar2, PlayerProfileWindow.Tabs[5].Node);
                        var hb5 = back5 != null ? FindChild(back5, "Hit") : null;
                        var wb5 = hb5 != null ? hb5.GetComponent<WindowButton>() : null;
                        if (wb5 != null) wb5.Click();      // 切回 Ranking 页（后面不留状态）
                        Check(pp.CurrentTab, WindowTabType.ProfileRanking, "再切回 `Ranking` 页");
                    }
                    pp.Close();          // 六页都断完了才关窗（见上面那条注释）
                }
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
                // 🆕 2026-10-03：`Next Deck Button` 的箭头**水平翻转**（原版挂 `UIFlippable`）。
                //   判据 = **uv 宽为负**（`SetUvRect(1,0,-1,1)`）—— 工程里 `CollectionWindow` / `MatchLogRow` 同一招。
                {
                    var nd = FindChild(FindChild(sk.transform, "Next Deck Button"), "Icon");
                    var nq = nd != null ? nd.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(nq != null && nq.UvRect.width < 0f,
                              "`Next Deck Button` 的箭头**已水平翻转**（原版 `UIFlippable`）—— 实测 uv.w = "
                              + (nq != null ? nq.UvRect.width.ToString("F2") : "无 quad"));
                    var pv = FindChild(FindChild(sk.transform, "Previous Deck Button"), "Icon");
                    var pq = pv != null ? pv.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(pq != null && pq.UvRect.width > 0f, "…而 `Previous Deck Button` 那颗**不翻**（原版只有 Next 翻）");
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
                // 🆕 2026-10-03：原版这行 TMP `charSpacing = 5`（以前 `Label` 没接口 ⇒ 没复刻）
                CheckNear(CharSpacingOf(FindChild(sk.transform, "Window Title")), 5f, 0.01f,
                          "`Window Title` 的 `characterSpacing` = **5**（原版 TMP 原文）");
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
                CheckHoverSwap(sk.transform, "遭遇战窗");
                CheckTrue(FindChild(sk.transform, "Scoring Bar Event Score Info") != null,
                          "记分条建了（那一族被 `scl 1.2563` + `0.8696` 两层包着 ⇒ rect 是**绕中心乘回去**算的）");
                Shoot("04_遭遇战窗.png");

                // 🆕 2026-09-26：**「玩家自建遭遇卡组」这一条路的验收**（甲/乙/丙/丁 四件）。
                //    在这一版之前整条路是断的：`DeckEditorState.Skirmish` **全仓没有任何赋值点**
                //    ⇒ 编辑器恒按经典 30 张算、模式也带不进对局。
                //    判据（模式为什么挂在卡组上 / 原版在哪定模式 / 选卡组按模式筛）→
                //    `资料/加时与冲突模式_原版规格.md` §2.7。
                {
                    // ① 建一副遭遇卡组：照原版 `SelectDecksTab.CreateDeck`「**建组那一刻定模式**」
                    int before = CollectionData.DeckCount();
                    CollectionData.PendingEditDeck = -1;
                    sk.CreateDeckInMode();
                    Check(CollectionData.DeckCount(), before + 1, "点 `Create deck` ⇒ 卡组库里多一套");
                    var made = CollectionData.Raw(CollectionData.DeckCount() - 1);
                    CheckTrue(made != null && made.GameMode == (int)GameMode.Skirmish,
                              "★ 建出来的那套 `GameMode` = **13（遭遇）**（实得 "
                            + (made == null ? -999 : made.GameMode) + "）—— **建组那一刻定死**，之后没有改的路径");
                    Check(CollectionData.PendingEditDeck, CollectionData.DeckCount() - 1,
                          "★ 交接下标指向新那套（切 `DeckEditor` 时编辑器打开它 ⇒ 编辑器按**遭遇那套规则**跑：上限 12 张）");
                    Check(CollectionData.CurrentIndex(), CollectionData.DeckCount() - 1,
                          "新卡组即选中（照原版 `DeckLibrary.Create` 的行为）");

                    // ② 模式不对的牌**开不了战、且出声**（不静默）—— 原版同款判据：
                    //    `SkirmishEventWindow.OnDeckSelected` → `RankedDeckSelector.HasValidDeckWithValidationMessage`
                    //    → `DeckUtility.ValidateDeck(deck, out err, isSkirmish)`
                    int classicIdx = -1;
                    for (int i = 0; i < CollectionData.DeckCount(); i++)
                    { var dd = CollectionData.Raw(i); if (dd != null && dd.GameMode == 0) { classicIdx = i; break; } }
                    CheckTrue(classicIdx >= 0, "库里有一套经典卡组（自检夹具 —— 用来验「遭遇窗不收经典牌」）");
                    CheckTrue(!sk.StartedBattle, "（前提）此刻还没开过战");
                    if (classicIdx >= 0)
                    {
                        sk.DeckIndex = classicIdx;
                        string whyBad;
                        CheckTrue(!sk.SelectedDeckFitsMode(out whyBad) && !string.IsNullOrEmpty(whyBad),
                                  "★ 遭遇窗里选中一副**经典**卡组 ⇒ 判为不合模式，并给出一句人话：" + whyBad);
                        sk.StartMatch();
                        CheckTrue(!sk.StartedBattle,
                                  "★ 点 `Battle!` **真的没开成**（挡在 `StartMatch` 里，弹窗说明原因）—— 不是静默放行");
                        var spBad = FindChild(sk.transform, "Searching Oponent Popup (1)");
                        CheckTrue(spBad == null || !spBad.gameObject.activeSelf,
                                  "★ 而且**匹配窗都没弹**（挡在匹配之前）—— 拦得足够早");
                    }

                    // ③ 给新那套补个督军（`CreateDeckInMode` 建的是**空牌**；没督军 `Battle!` 照样会如实拒绝），
                    //    然后重开一次窗口 —— 它会**吸附到本窗模式下能用的那一套**（原版是列表里只剩同模式的）
                    {
                        var libM = DeckLibrary.Load();
                        var mine = libM.Decks[libM.Count - 1];
                        foreach (var c in CardDatabase.Load())
                            if (c.Type == "hero") { mine.WarlordId = c.Id; break; }
                        libM.Save();
                        CollectionData.ResetForTest();
                    }
                    sk.Open();                        // 重开：按新模式吸附 + 重画（`_search` 也会重建）
                    var landed = CollectionData.Raw(sk.DeckIndex);
                    CheckTrue(landed != null && landed.GameMode == (int)GameMode.Skirmish,
                              "★ 重开窗 ⇒ **自动落在遭遇那套**上（默认选中那套模式不对的话，一进来点 Battle! 就会被挡）"
                            + "（实得 " + (landed == null ? "null" : landed.Name + " · mode " + landed.GameMode) + "）");
                    string whyOk;
                    CheckTrue(sk.SelectedDeckFitsMode(out whyOk), "★ 选中**遭遇**那套 ⇒ 合模式（可以开战）");

                    // ④ 选卡组弹窗的「我的卡组」页**按模式筛** —— 原版 `DeckSelectionPopup` 的 `TryOpen`
                    //    拿着 `DeckSelectionContext`，筛选 lambda = `候选.GameMode == context.Deck.GameMode
                    //    || 候选.GameMode == context.GameMode`（`DeckSelectionPopup___TryOpen_b__10_0.c:10-17`）
                    {
                        var dsp = sk.OpenDeckSelection();
                        CheckTrue(dsp != null, "遭遇窗里开得出 `Deck Selection Popup`");
                        if (dsp != null)
                        {
                            int skCount = 0, allCount = CollectionData.DeckCount();
                            for (int i = 0; i < allCount; i++)
                            { var dd = CollectionData.Raw(i); if (dd != null && dd.GameMode == (int)GameMode.Skirmish) skCount++; }
                            dsp.SwitchTab(true);
                            Check(dsp.ShownCount, skCount,
                                  $"★「我的卡组」页**只列遭遇那批**（{skCount} 套 / 全库 {allCount} 套）"
                                + " —— 经典那几套**不出现**（不是列出来再拦）");
                            dsp.Close();
                        }
                    }
                }

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
                // 🆕 2026-10-03（§三 第 29 条 **A2**）：**联机等待态** —— 显示窗内那扇弹窗、
                //    **不跑那 12 秒 bot 倒计时**（等的是真人）。原来 P2P 那条路上三扇模式窗
                //    `Searching` 是 false 又不开全屏窗 ⇒ **屏幕上什么都没有**。
                {
                    var spn = FindChild(sk.transform, "Searching Oponent Popup (1)");
                    var pop = spn != null ? spn.GetComponent<SearchingMatchPopup>() : null;
                    CheckTrue(pop != null, "匹配弹窗上挂着 `SearchingMatchPopup`");
                    if (pop != null)
                    {
                        pop.Show();
                        pop.BeginNetWait();
                        CheckTrue(pop.IsShowing, "联机等待态：**窗显示出来**（原来是屏幕上什么都没有）");
                        CheckTrue(!pop.Searching, "…而且**不跑**那 12 秒 bot 倒计时（条件 `Searching` 为假）");
                        pop.Tick(20f);          // 推进 20 秒 —— 单机那一支 12 秒就该转 bot 了
                        CheckTrue(pop.IsShowing && !pop.Searching, "推进 20 秒也**不会自己收掉**（等真人等到为止）");
                        CheckTrue(pop.TypedText.Length > 0, "打字机照跑（联机等待也显示原版那句 `Searching`）");
                        // 🆕 2026-10-03：**`Cog` 自转**（原版 `Skull` 上那条 legacy `Animation` + clip `SearchingOpponentCog`）
                        //   判据 = 那条 clip 的 37 个键（`AnimationClip_-6716633893720174568.json`）。抽 6 个点核。
                        var S = CardPresentation.SearchingMatchPopup.CogLoop;
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(0f), 0f, 0.01f,
                                  "`Cog` 曲线 t=0 ⇒ 0°（clip 第 1 键）");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(0.41666666f), 20f, 0.01f,
                                  "t=0.4167 ⇒ 20°（第 2 键）");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(0.55f), 20f, 0.01f,
                                  "t=0.55 ⇒ 仍 20°（**保持段** —— 曲线是「补间 + 保持」而不是匀速）");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(2.65f), 100f, 0.01f, "t=2.65 ⇒ 100°");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(9.883333f), 360f, 0.01f,
                                  "t=9.8833 ⇒ 360°（一圈走满）");
                        CheckNear(CardPresentation.SearchingMatchPopup.CogAngleAt(S), 0f, 0.01f,
                                  "走到一圈末尾 ⇒ **回绕到 0°**（原版 `m_WrapMode = 2 Loop`）");
                        pop.Show(); pop.Tick(0.41666666f);          // 从头放 0.4167s
                        var cogN = FindChild(spn, "Cog");
                        CheckTrue(cogN != null
                                  && Mathf.Abs(Mathf.DeltaAngle(cogN.localRotation.eulerAngles.z, 20f)) < 0.5f,
                                  $"`Tick` 之后 `Cog` 节点**真的转到 20°**（实测 "
                                  + (cogN != null ? cogN.localRotation.eulerAngles.z.ToString("F2") : "无节点") + "）");
                        pop.Hide();
                    }
                }
                // ⚠️ **用完还原**：这一节把选中的那套换成了**遭遇**牌，而下面排位窗是本窗的兄弟
                //    （`DeckGameMode` 默认经典）⇒ 不还回去的话排位那一节会被同样的关卡挡掉。
                CollectionData.Select(0);
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
                CheckHoverSwap(rk.transform, "排位窗");
                CheckText(TextOf(FindChild(FindChild(rk.transform, "LeaderboardButton"), "Button Text")), "Leaderboard",
                          "`LeaderboardButton` 文案（同样要限定父节点 —— `Button Text` 树里有三处）");
                CheckTrue(FindChild(rk.transform, "ChangeRankedToggle") != null, "`ChangeRankedToggle` 建了");
                // 🆕 2026-10-03：开关那两行原版 TMP 带 `charSpacing = -4`；排位窗的 `Game Mode Icon`
                //   取 **classic** 那张（原版由 `playMode` 喂图，本地只有 classic/skirmish 两张，
                //   而排位走的就是经典模式 ⇒ 映射到 classic，**属推断、代码里已如实标**）
                {
                    var tg0 = FindChild(rk.transform, "ChangeRankedToggle");
                    CheckNear(CharSpacingOf(FindChild(tg0, "UnrankedText")), -4f, 0.01f,
                              "`UnrankedText` 的 `characterSpacing` = **−4**（原版 TMP 原文）");
                    CheckNear(CharSpacingOf(FindChild(tg0, "RankedText")), -4f, 0.01f,
                              "`RankedText` 的 `characterSpacing` = **−4**");
                    var gmi2 = FindChild(rk.transform, "Game Mode Icon");
                    var gmq2 = gmi2 != null ? gmi2.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(gmq2 != null && gmq2.Texture != null && gmq2.Texture.name == "40k_gamemode_icon_classic",
                              "排位窗的 `Game Mode Icon` = **`40k_gamemode_icon_classic`**（按 `playMode = Classic` 推的）");
                }
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
                // 🔴 2026-09-26 修一处**截图污染**：上面那扇「模式不对」的模态提示窗一直没关，
                //    把 `05_排位窗.png` 与 `06_找对手窗.png` 都盖住了（看图才发现 —— 断言全绿）。
                //    同 `:401` 那个口子。
                foreach (var pp in Object.FindObjectsByType<PromptPopup>(FindObjectsSortMode.None))
                    if (pp != null) pp.Close();
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
                        // ================================================================
                        //  🆕 「找到对手」那一态（联机那一支）—— 判据 → `资料/阶段二_多人界面_原版规格.md` §6·4
                        //  🔴 这一态的**时机/时长/名字**原版没有可抄（`OpponentFound` 零调用点，是死代码）
                        //     ⇒ 那些是**我们定的**；但**画出来的东西照原版**：督军立绘 = 对手本局那副牌的督军 ·
                        //     立绘 **uv 镜像** · 名字**不翻** · `Found` / `Not Found` **二选一**。
                        // ================================================================
                        {
                            CheckTrue(CardPresentation.Net.NetMatchmaking.HoldForPresentation == null,
                                      "**没配对时不注册**「切场景前那一口气」（单机那条 12 秒链行为一字不改）");
                            // 拿原版那张立绘的主人当例子：`UM3` = Uriel Ventris
                            //（原版 prefab 敌方那格填的就是 `UI_Deck_Warlord_Uriel Ventris`，正本 §6·2）
                            so.ShowOpponent("UM3", "青剑湖");
                            CheckTrue(ef != null && ef.gameObject.activeSelf, "配到人 ⇒ **对手那格翻成 `Found`**");
                            var nf2 = FindChild(en, "Not Found Container");
                            CheckTrue(nf2 != null && !nf2.gameObject.activeSelf,
                                      "同一条：`Not Found Container` 收起来（**二选一**，不是两个都亮）");
                            var aq = FindChild(ef, "Warlord Image") != null
                                     ? FindChild(ef, "Warlord Image").GetComponentInChildren<ImageQuad>() : null;
                            CheckTrue(aq != null && aq.Texture != null,
                                      "对手那格画出了督军立绘（`UM3` 的 `art_um3` 取得到）");
                            CheckTrue(aq != null && aq.UvRect.width < 0f,
                                      "🔴 立绘 **uv 镜像**（原版 `m_LocalScale=(-1,1,1)`）—— 量的是**真 uv**（宽为负）；"
                                      + "⚠️ 改父节点 scale 是另一回事：`MenuDraw` 先算世界坐标再减父位置，父一有 scale"
                                      + "子件位置会被再乘一次（`资料/已知的坑.md:306`）");
                            CheckTrue(TextOf(FindChild(ef, "Player Name")) == "青剑湖",
                                      "名字换成**对面的真名**（不再是 prefab 那行 `Player name`）");
                            CheckTrue(TextOf(FindChild(ef, "Player Name")) != SearchingOpponentWindow.PlaceholderName,
                                      "**占位名必须被覆盖掉** —— 否则玩家会把 `Player name` 当成对手的真名");
                            // 名字那行**不跟着翻**（原版只翻立绘、不翻文字）
                            var nm = FindChild(ef, "Player Name");
                            var nq = nm != null ? nm.GetComponentInChildren<ImageQuad>() : null;
                            CheckTrue(nq == null || nq.UvRect.width > 0f, "名字那行**不跟着翻**（原版只翻立绘）");
                            // 「切场景前那一口气」：拦下 ⇒ 到点才放行
                            so.NetWatch = true;
                            so.PresentationHold = 1f;
                            bool done = false; so.OnPresentationDone = () => { done = true; };
                            CheckTrue(so.HoldSceneForPresentation(null),
                                      "配到人时 `HoldForPresentation` **拦下这一次切场景**（让界面先展示）");
                            so.Tick(0.5f); CheckTrue(!done, "展示中途（0.5s / 1s）**还不放行**");
                            so.Tick(0.6f); CheckTrue(done,
                                      "到时（1.1s / 1s）⇒ 放行切战场（1s 取原版 `StartBattleWithDelay` 的 `waitLoadTime`）；"
                                      + "⚠️ **拿它当展示时间是我们挑的**");
                            so.OnPresentationDone = null;
                            // 关窗 ⇒ **必须把那一口气放掉**（否则这一局卡在「切不了场景」，静默失败）
                            CardPresentation.Net.NetMatchmaking.HoldForPresentation = so.HoldSceneForPresentation;
                            so.Close();
                            CheckTrue(CardPresentation.Net.NetMatchmaking.HoldForPresentation == null,
                                      "🔴 关窗时**把那一口气放掉**（不然这一局永远切不了场景）");
                            so.gameObject.SetActive(true);      // 后面的 `Shoot` 还要拍它
                        }
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

        // ============================================================ 四个排行榜 + 段位信息块
        //   （2026-09-27 建 · 多人界面那一批 第 3 件 + 第 5 件）
        // 判据：四棵树的层×参数 → `资料/普查产出_0927/排行榜_{遭遇战,经典,轮抽,嵌入版与行族}.md`；
        //       入口链 → `排行榜_入口与调用.md`（§1 `RankedEventWindowV2` 上两颗 prefab，按 playMode 二选一）；
        //       段位块 → `段位块_RankedDivisionInfo.md`（表 + 字段→节点 + 查不到的）。
        // 🔴 断的全是「原版参数」，不是我们自己的常量（§10·3 第 3 层）。
        Section("排行榜 ①：入口 —— 排位窗那颗 `LeaderboardButton` 按 `playMode` 二选一（**复刻，不是我们挑的**）");
        {
            var rc = menu.Find("Base Game Mode Container 1x1 - Ranked");
            var rh = rc != null ? FindChild(rc, "Hit") : null;
            var rwb = rh != null ? rh.GetComponent<WindowButton>() : null;
            CheckTrue(rwb != null, "排位卡还点得开（上一节刚把它关掉）");
            if (rwb != null) rwb.Click();
            // ⚠️ `LastOpened` 是 `LiveOpsEventWindow` 上的**静态**（排位与遭遇战共用一个字段）
            //    ⇒ 要断排位独有的 `LeaderboardKindForMode` 得转回 `RankedEventWindow`。
            var rk = RankedEventWindow.LastOpened as RankedEventWindow;
            CheckTrue(rk != null, "排位窗又开出来了");
            if (rk != null)
            {
                Check(rk.LeaderboardKindForMode, LeaderboardKind.Classic,
                      "本窗 `DeckGameMode` = 经典 ⇒ 那颗钮开的是**经典榜**"
                      + "（原版 `playMode == Classic(0)` ⇒ `rankingPrefabClassic`，否则 ⇒ `rankingPrefab`）");
                var lbh = FindChild(FindChild(rk.transform, "LeaderboardButton"), "Hit");
                var lbb = lbh != null ? lbh.GetComponent<WindowButton>() : null;
                CheckTrue(lbb != null, "`LeaderboardButton` 有点击区（**原来点了只 `NotBuilt` 打日志**）");
                if (lbb != null) lbb.Click();
                var lb0 = LeaderboardWindow.LastOpened;
                CheckTrue(lb0 != null && lb0.Kind == LeaderboardKind.Classic,
                          "点它 ⇒ 开 `RankedClassicLeaderboardPopup Variant`");
                CheckTrue(lb0 != null && lb0.CurrentState == WindowState.Open, "那一扇是**开着**的");
                Check(lb0 != null ? lb0.TabCount : -1, 2, "经典榜 **2 个页签**（没有 Alliances —— 三条独立证据）");
                if (lb0 != null) lb0.Close();
                rk.Close();
            }
        }

        Section("排行榜 ②：经典榜骨架（三扇全屏榜**共用那一套**，逐格照原版）");
        {
            var lb = menu.OpenLeaderboard(LeaderboardKind.Classic);
            Check(lb.type, WindowType.Popup, "`type` = **1 Popup**（`RankedRankingWindow` 根上的字段）");
            Check(lb.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
            CheckTrue(lb.closeOnEsc, "`closeOnESC` = **1**");
            CheckNear(lb.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = **1.0**");
            var t = lb.transform;
            CheckAtWorld(FindChild(t, "Menu Dark Background"), -1327.30f, 3247.30f, -746.18f, 1826.18f,
                         "压暗层（**无图**、纯色 (0,0,0,0.773)）");
            var tabs = FindChild(t, "Tab Buttons");
            CheckAtWorld(tabs, 38.62f, 210.59f, 104.70f, 928.48f, "`Tab Buttons`（VLG spacing 0 · 首格顶 104.70）");
            CheckAtWorld(FindChild(tabs, "Player"), 45.59f, 210.59f, 104.70f, 262.38f,
                         "页签 1 `Player`（165×157.684）");
            CheckAtWorld(FindChild(tabs, "Armies"), 45.59f, 210.59f, 262.38f, 420.07f,
                         "页签 2 `Armies`（**紧挨着叠下去** —— 不是我们排的，是 VLG 算的）");
            {
                var pi = FindChild(FindChild(tabs, "Player"), "Icon");
                var pq = pi != null ? pi.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(pq != null && pq.Texture != null && pq.Texture.name == "40K_Chat_icon_Global",
                          "`Player` 页签图标 = `40K_Chat_icon_Global`（**本轮才导进来的那张**）");
                var ai = FindChild(FindChild(tabs, "Armies"), "Icon");
                var aq = ai != null ? ai.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(aq != null && aq.Texture != null && aq.Texture.name == "40K_Profile_icon_title",
                          "`Armies` 页签图标 = `40K_Profile_icon_title`");
            }
            CheckTrue(FindChild(tabs, "Label") == null,
                      "页签的 `Label` **不建**（原版出厂 inactive、且全子树零引用 ⇒ 死件；页签是**纯图标**的）");
            var panel = FindChild(t, "Ranking Display");
            CheckAtWorld(panel, 202.40f, 1717.60f, 16.32f, 1006.93f, "`Ranking Display`");
            CheckAtWorld(FindChild(panel, "Generic Window Red Background Big"), 202.40f, 1717.60f, 16.32f, 1006.93f,
                         "红底板（`UI_Deck_Information_Back` · 九宫 42,363,655,81）");
            CheckAtWorld(FindChild(panel, "Title"), 643.55f, 1276.45f, 36.95f, 146.52f, "`Title`");
            CheckText(TextOf(FindChild(panel, "Title")), "TOP PLAYERS", "四棵榜的标题**一律** `TOP PLAYERS`");
            CheckAtWorld(FindChild(panel, "TopBar"), 282.95f, 1637.05f, 141.63f, 147.63f,
                         "`TopBar`（6px 分隔线 —— 原版**没有列标题表头**，就是这一条）");
            var lbContent = FindChild(panel, "Content");
            CheckAtWorld(lbContent, 248.99f, 1671.01f, 147.64f, 937.83f, "`Content`");
            CheckAtWorld(FindChild(lbContent, "Army Selector"), 248.99f, 1671.01f, 147.64f, 258.59f,
                         "`Army Selector`（1422.02×110.95）");
            // 🆕 2026-10-03（§三 第 29 条 A3）：`Army Content` **真的填起来了**。
            //   判据 = **反编译方法体**（`ArmySelector__Initialize.c`：清空 → 遍历阵营 → `Instantiate(armyItemButton)`）
            //        + `python 工具/menu_dump.py bundle_menus_assets_all "Army Item Button" --depth 4`（逐节点几何）。
            //   🔴 三处是我们挑的（阵营清单 / 不隐藏任何阵营 / 不建角标）—— 见 `BuildArmySelector` 的注释。
            var armSel = FindChild(lbContent, "Army Selector");
            var armContent = FindChild(armSel, "Army Content");
            CheckTrue(armContent != null, "`Army Content` 建了");
            Check(LeaderboardWindow.ArmyTotal, 13, "军种项一共 **13** 颗（= `CampaignData.Armies` 那 13 个阵营）");
            CheckTrue(lb.ArmyButtonCount > 0 && lb.ArmyButtonCount <= 13,
                      $"这一轮真画出来 **{lb.ArmyButtonCount}** 颗（滚出 1422.02 视口的不建 —— 内容总宽 1604.68）");
            CheckTrue(lb.ArmyScroll != null && Mathf.Abs(lb.ArmyScroll.MaxOffset - (1604.68f - 1422.02f)) < 0.5f,
                      "军种条**横向可滚 182.66px**（= 内容 1604.68 − 视口 1422.02；原版 `ScrollRect h=1 v=0 mode=1`）");
            CheckTrue(lb.SelectedArmy == null, "起手**没有任何阵营被选中**（原版 `toggle.isOn` 只在 `army == selected` 时置 1）");
            var a0 = FindChild(armContent, CampaignData.Armies[0]);
            CheckTrue(a0 != null, "第 1 颗军种项在（**GO 名 = 阵营名** —— 原版 `Initialize` 里 `set_name(前缀 + army)`）");
            if (a0 != null)
            {
                // 按钮 136.36×121.59，中心 y = `Army Selector` 的中心 203.115；
                // 第 1 颗中心 x = 248.99 + 136.36/2（`LeftAligned`，见 `BuildArmySelector` 那条「我们挑的」）
                CheckAtWorld(a0, 248.99f, 385.35f, 142.32f, 263.91f,
                             "第 1 颗军种项（136.36×121.59 @ 中心 317.17,203.12 —— 比 110.95 的条**高**，上下被裁）");
                var ic0 = FindChild(a0, "Icon");
                var q0 = ic0 != null ? ic0.GetComponentInChildren<ImageQuad>() : null;
                string wantIcon = DeckRuntime.FactionIcon(CampaignData.Armies[0]);
                CheckTrue(q0 != null && q0.Texture != null && q0.Texture.name == wantIcon,
                          "`Icon` = `" + wantIcon + "`（该阵营的徽记 —— 原版无图、运行期 `ArmyIconsSO.GetArmyIcon`）");
                CheckTrue(FindChild(a0, "HighlightBG") == null,
                          "**没选中 ⇒ `HighlightBG` 不建**（判据 = `ArmyItemContainer__Initialize` 里 `SetActive(false)`）");
                var h0 = FindChild(a0, "Hit");
                var wb0 = h0 != null ? h0.GetComponent<WindowButton>() : null;
                CheckTrue(wb0 != null, "军种项有点击区");
                if (wb0 != null)
                {
                    wb0.ClickForTest();
                    Check(lb.SelectedArmy, CampaignData.Armies[0], "点它 ⇒ 选中");
                    var a0b = FindChild(armContent, CampaignData.Armies[0]);
                    CheckTrue(a0b != null && FindChild(a0b, "HighlightBG") != null,
                              "…**选中那一颗长出 `HighlightBG`**（原版 `Click(on)` 里 `SetActive(on)`）");
                    // ⚠️ `FindChild` 是**按名字精确匹配**的、不认识 `A/B/C` ⇒ 路径要走 `Transform.Find`
                    CheckTrue(a0b != null && a0b.Find("HighlightBG/Arrow") != null, "…`HighlightBG` 里那枚 `Arrow` 也在");
                    var h0b = a0b != null ? FindChild(a0b, "Hit") : null;
                    if (h0b != null) h0b.GetComponent<WindowButton>().ClickForTest();
                    CheckTrue(lb.SelectedArmy == null, "再点一次 ⇒ 取消选中（Toggle 语义）");
                }
            }
            lb.ArmyScroll.ScrollBy(200f);           // 滚到底 ⇒ 最后一颗（第 13 个阵营）进视口
            CheckTrue(FindChild(armContent, CampaignData.Armies[12]) != null,
                      $"滚到最右 ⇒ 第 13 颗（{CampaignData.Armies[12]}）进视口（起手时它在 1717.31 之外）");
            lb.ArmyScroll.ScrollBy(-200f);
            CheckAtWorld(FindChild(lbContent, "Separator Line"), 248.99f, 1671.01f, 260.37f, 266.37f, "`Separator Line`");
            var sv = FindChild(lbContent, "Scroll View");
            CheckAtWorld(sv, 248.99f, 1671.01f, 288.59f, 937.83f, "`Scroll View`（649.24 高）");
            var vp = FindChild(sv, "Viewport");
            CheckAtWorld(vp, 248.99f, 1671.01f, 288.59f, 937.83f, "`Viewport`（原版是 `UIMask`(a=0)+`RectMask2D`）");
            CheckAtWorld(FindChild(vp, "Content"), 360f, 1560f, 288.59f, 288.59f,
                         "内层 `Content`（**1200 宽** —— 左右各留 111）");
            var cb = FindChild(t, "Generic Close Button Orange");
            CheckAtWorld(cb, 1656.81f, 1731.19f, 9.19f, 84.80f, "关窗钮（74.39×75.61）");
            CheckTrue(FindChild(cb, "Background") != null && FindChild(cb, "Icon") != null,
                      "它那两个可见子件（圆底 `40k_general_bt_yellow` + 叉）");
            CheckTrue(FindChild(t, "Timer") == null,
                      "**`Timer` 不建**（`Ends in: 23d 5h` = 服务器数据 + 用户明确不要赛季倒计时）");
            var seasonBtn = FindChild(t, "Generic Simplified UI Button_updated");
            CheckTrue(seasonBtn != null && !seasonBtn.gameObject.activeSelf,
                      "`Last season` 钮**建了但关着** —— 照原版 `AllowChangeSeason(有上一赛季榜)`"
                      + "（本地没有上一赛季榜 ⇒ 关）");
            var seasonTx = FindChild(t, "Last Season Text");
            CheckTrue(seasonTx != null && !seasonTx.gameObject.activeSelf,
                      "`Last Season Text` **建了但关着** —— 照原版 `Initialize` 传 `showCurrent=true`"
                      + " ⇒ `SetActive(!showCurrent)`");
            Check(lb.BuiltRows, 0, "本地 **0 条**榜单数据 ⇒ 一行都不建（**照原版留空**：四棵榜都没有空态节点）");
            Shoot("07_排行榜_经典.png");

            // ---- 喂一行 ⇒ 断**行族几何**（`PlayerRankingRow` 逐格）----
            // ⚠️ `RebuildForTest()` 会**整窗重画**（`Build()` 把子件全清掉）⇒ 上面取的 `tabs`/`vp` 全失效，
            //    下面一律**重新找**（拿旧引用会静默变 null）。
            LeaderboardData.InjectForTest(LeaderboardKind.Classic, LeaderboardTab.Player,
                new List<LeaderboardRowData>
                {
                    new LeaderboardRowData { Rank = 7, Name = "Test Commander", Guild = "Bando del Ventris",
                                             Points = "4500", Avatar = "Avatar_ASH_Howling Banshee", IsSelf = true },
                });
            lb.RebuildForTest();
            Check(lb.BuiltRows, 1, "喂一行 ⇒ **建出一行**（空数据那条路仍是原版那个样子）");
            var row = FindChild(FindChild(FindChild(FindChild(lb.transform, "Scroll View"), "Viewport"), "Content"),
                                "PlayerRankingRow");
            CheckAtWorld(row, 360f, 1560f, 288.59f, 388.59f, "行矩形（高 **100**，从视口顶边起）");
            CheckAtWorld(FindChild(row, "Ranking"), 372f, 472f, 302.63f, 374.54f, "行内 `Ranking`（名次）");
            CheckText(TextOf(FindChild(row, "Ranking")), "7", "名次文字");
            CheckAtWorld(FindChild(row, "border"), 495f, 615f, 289.57f, 396.01f,
                         "行内 `border`（头像格 —— **这一层才是 Button**，`target` = `Icon` 的 Image）");
            CheckAtWorld(FindChild(row, "Name Holder"), 650f, 1383.95f, 288.59f, 388.59f, "行内 `Name Holder`");
            CheckText(TextOf(FindChild(row, "Name")), "Test Commander", "行内 `Name`");
            // `Points` 是**左对齐**的（原版 `m_HorizontalAlignment = Left`）⇒ 不能用 `CheckAtWorld` 比节点中心
            //（`AlignLeftOn` 会把节点挪到「文字左缘 = 矩形左缘」那个位置）。**量它渲出来的左缘**才对。
            {
                float px1, py1, px2, py2;
                bool ok = RenderedRect(FindChild(row, "Points"), out px1, out py1, out px2, out py2);
                CheckTrue(ok && Mathf.Abs(px1 - 1420f) < 2f,
                          "行内 `Points`（右端）**左缘对齐 1420**（原版 hAlign = Left）—— 实测 " + px1.ToString("F1"));
                CheckTrue(ok && Mathf.Abs((py1 + py2) * 0.5f - 338.845f) < 2f,
                          "行内 `Points` 竖直居中于 299.86/377.83 那个框 —— 实测中心 " + ((py1 + py2) * 0.5f).ToString("F1"));
            }
            {
                // 🆕 2026-10-03：同上 —— 冒到视口之上/之下的那两截被真裁掉了（原版 `RectMask2D` 同）。
                // 🔴 实测这一件**上下都被裁**（原版块 y 257.45..423.12、实渲 288.59..394.44）
                // ⇒ 只剩**左右两条边**是原版值（实测 1301.70..1410.00 逐位吻合），另钉一条「真的被裁过」。
                float rx1, ry1, rx2, ry2;
                CheckTrue(RenderedRect(FindChild(row, "RankingIcon"), out rx1, out ry1, out rx2, out ry2)
                          && Mathf.Abs(rx1 - 1301.70f) < 1.5f && Mathf.Abs(rx2 - 1410f) < 1.5f,
                          "行内 `RankingIcon`：左右 1301.70..1410（原版值）—— 实测 "
                          + $"x {rx1:F2}..{rx2:F2} · y {ry1:F2}..{ry2:F2}");
                CheckTrue((ry2 - ry1) < 165.67f - 0.5f,
                          $"…上下都**真被视口裁过**（原版块高 165.67，实渲 {ry2 - ry1:F2}）");
            }
            {
                var bq = FindChild(row, "BackgroundHighlight");   // `IsSelf = true` ⇒ 走高亮那张
                var bgq = bq != null ? bq.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(bgq != null && bgq.Texture != null && bgq.Texture.name == "Background",
                          "行底用的是 Unity **内置** `Background`（32×32 · 九宫 10,10,10,10）");

                // 🆕 2026-10-03（本件）：**行底九宫格被逐子块截到视口内** —— 这一行**天生就是压边态**：
                //   行 0 的顶 = 视口顶 288.59，而底块 `BgR` 局部 y = −3.22..103.22（**上下各溢出 3.22**）
                //   ⇒ 上面那 3.22px 必须被**截掉**（判据 = 原版 `RectMask2D` 只裁渲染、不挪 `RectTransform`）。
                // ⚠️ **量子块、别量根节点**：根节点位置按设计**一律不动**（量到根在视口外是**对的**，动了才是 bug）。
                // ⚠️ **必须用带 `true` 的重载** —— 整块在框外的子块走 `SetActive(false)`，默认重载看不到。
                // 写法照上面 `RankingIcon` 那两条（`RenderedRect` 量**渲出来**的矩形，不抄源码常量）。
                var blocks = bq != null ? bq.GetComponentsInChildren<ImageQuad>(true) : new ImageQuad[0];
                CheckTrue(blocks.Length > 0, $"行底九宫格建出来了（{blocks.Length} 块）");
                int nHidden = 0, nOutB = 0;
                float bTop = float.MaxValue, bBot = float.MinValue, bL = float.MaxValue, bR = float.MinValue;
                foreach (var bk in blocks)
                {
                    if (bk == null) continue;
                    // ⚠️ **先判 `activeSelf`、再量矩形** —— 反过来写的话，被 `SetActive(false)` 的那一块
                    //    会因为 `GetComponentInChildren`（不带 `true`）在未激活对象上拿不到自己而**被静默跳过**，
                    //    下面那条「一块都没被关掉」就等于没验。
                    if (!bk.gameObject.activeSelf) { nHidden++; continue; }   // 整块在框外 ⇒ 被关掉（节点还在）
                    float kx1, ky1, kx2, ky2;
                    if (!RenderedRect(bk.transform, out kx1, out ky1, out kx2, out ky2)) continue;
                    bTop = Mathf.Min(bTop, ky1); bBot = Mathf.Max(bBot, ky2);
                    bL = Mathf.Min(bL, kx1); bR = Mathf.Max(bR, kx2);
                    if (ky1 < 288.59f - 0.5f || ky2 > 937.83f + 0.5f
                        || kx1 < 248.99f - 0.5f || kx2 > 1671.01f + 0.5f) nOutB++;
                }
                CheckTrue(nOutB == 0,
                          $"每一块都落在视口 248.99..1671.01 × 288.59..937.83 内（越界 {nOutB} 块；"
                          + $"实测 x {bL:F2}..{bR:F2} · y {bTop:F2}..{bBot:F2}）");
                CheckTrue(nHidden == 0,
                          $"没有哪一块是「整块在框外、被 `SetActive(false)` 关掉」的（实得 {nHidden} 块 —— "
                          + "这一行只压了 3.22px，该走**截**那一档、不该走**关**那一档）");
                CheckNear(bTop, 288.59f, 0.5f,
                          "★ 最上面那一块**确实被截到视口顶 288.59**（没截的话它该在 285.37 = 行顶 − 3.22）"
                          + " —— 这就是「九宫格逐子块截」的判据");
            }
            {
                // 🔴 立绘**必须排在边框之后**：`Player_Profile_Border` 的中心是**不透明黑**
                //    （实测 RGBA=(0,0,0,255)）⇒ 同队列时谁盖谁由「到相机的距离」定，会把立绘压成黑块。
                var bImg = FindChild(FindChild(row, "border"), "Image");
                var iImg = FindChild(FindChild(row, "border"), "Icon");
                var bQuad = bImg != null ? bImg.GetComponentInChildren<ImageQuad>() : null;
                var iQuad = iImg != null ? iImg.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(bQuad != null && iQuad != null && iQuad.RenderQueue > bQuad.RenderQueue,
                          "行内立绘的渲染队列**比边框高一档**（不然黑色边框心会把立绘盖掉）");
            }
            Check(lb.MissingArt.Count, 0, "建了行之后**还是一张图都不缺**（含内置 `Background`）");
            CheckHoverSwap(lb.transform, "排行榜窗");
            // 🆕 2026-10-03（§三 第 29 条 A3②）：**行被点 ⇒ 开那个玩家的档案窗**（原版 `profileButton`）。
            //   我们照做（开**同一扇** `PlayerProfileWindow`），但走 `CreateFor` 那一支 ——
            //   🔴 他的资料在服务器 ⇒ 六页换成 `StrangerProfilePage` 画如实说明，**不拿本地自己那一份冒充他**。
            {
                var hRow = FindChild(row, "Hit");
                var wbRow = hRow != null ? hRow.GetComponent<WindowButton>() : null;
                // ⚠️ 这一条同时是**命中区那道 `clip` 守卫的回归守**：行 0 完全在视口内 ⇒ 它的 `Hit` **必须还在**
                //    （`clip` 传反了 ⇒ 整颗不建 ⇒ 这条立刻红）。**压边的那一半**在下面「喂 7 行」那一段。
                CheckTrue(wbRow != null, "行有点击区（原版挂的是 `profileButton`）");
                if (wbRow != null)
                {
                    wbRow.ClickForTest();
                    var pw = PlayerProfileWindow.LastOpened;
                    CheckTrue(pw != null && pw.ViewedPlayer == "Test Commander",
                              "点行 ⇒ 开了**那一行那个玩家**的档案窗（`ViewedPlayer` = 行里的名字）");
                    if (pw != null)
                    {
                        var p0 = FindChild(pw.transform, "Profile Tab");
                        CheckTrue(p0 != null && p0.GetComponent<StrangerProfilePage>() != null,
                                  "六页换成了 `StrangerProfilePage`（原版这一页的数据全在服务器）");
                        CheckTrue(p0 != null && FindChild(p0, "Stranger Name") != null
                                  && TextOf(FindChild(p0, "Stranger Name")) == "「Test Commander」的档案",
                                  "页里画了那行**如实说明**（不是他的真资料 —— 本地没有）");
                        pw.Close();
                    }
                }
            }
            Shoot("07b_排行榜_经典_喂了一行.png");     // 空数据那张（`07`）看不出行族画得对不对
            LeaderboardData.ClearForTest();
            lb.RebuildForTest();
            Check(lb.BuiltRows, 0, "清空数据 ⇒ 行又没了");

            // ============================================================ 🆕 2026-10-03（本件 ①）
            // **滚出视口的整行不建** + **这一格真的滚得动**。
            // 🔴 原来 `LeaderboardWindow.RebuildRows` **从不设** `_scroll.ContentX2`（用的是
            //    `MenuScroll.TopAligned(ScrollR, 0f)`）⇒ `ContentX1 == ContentX2 == Viewport.y1`
            //    ⇒ `ClampLo == ClampHi == 0` ⇒ **滚不动**。而本批刚加了「视口外不建」的跳过
            //    ⇒ 第 7 行起从「画到框外（至少看得见）」变成「**完全不存在**」⇒ 不修就是丢数据。
            // 判据（原版）：这一格是 `ScrollRect m_Horizontal=0 / m_Vertical=1 / m_MovementType=1`，
            //    内层 `Content` 挂 `ContentSizeFitter m_VerticalFit=1`、行挂在它下面
            //    （实据 → `资料/普查产出_0927/排行榜_嵌入版与行族.md:38,40,61`）
            //    ⇒ 内容比视口高时就该滚得动；滚出视口的那部分**不画**（原版 `RectMask2D` 裁掉）。
            // 🔴 期望值一律**现算**（`RowsInViewport`）：视口 288.59..937.83（上面 `CheckAtWorld` 刚按原版值钉过）、
            //    行距 = 原版 100 + 15。⛔ **不写死条数**（写死 = 拿我们的常量断言我们的常量）。
            {
                const int N = 12;
                var many = new List<LeaderboardRowData>();
                for (int i = 1; i <= N; i++)
                    many.Add(new LeaderboardRowData { Rank = i, Name = "Runner " + i, Points = (1000 - i).ToString(),
                                                      IsSelf = i == 1 });
                LeaderboardData.InjectForTest(LeaderboardKind.Classic, LeaderboardTab.Player, many);
                lb.RebuildForTest();

                const float VpTop = 288.59f, VpBot = 937.83f;   // 原版值（= 上面那句 `CheckAtWorld(vp, …)` 钉过的两个数）
                float pitch = OrigLbRowH + OrigLbRowGap;   // 原版：行高 100 + 行距 15
                var vpN2 = FindChild(FindChild(lb.transform, "Scroll View"), "Viewport");
                CheckNear(vpN2 != null ? LayoutSpace.PxY(vpN2.position.y) : -9999f, (VpTop + VpBot) * 0.5f, 0.5f,
                          "`Viewport` 的**实测**中心 = 288.59..937.83 的中心（下面那些期望值就按这个矩形现算）");
                var ctn = FindChild(vpN2, "Content");

                int wantTop = RowsInViewport(N, VpTop, VpBot, pitch, OrigLbRowH, 0f);
                Check(lb.BuiltRows, wantTop,
                      $"★ {N} 行里**恰好建了与视口相交的那几行**（现算 {wantTop} 行；视口高 {VpBot - VpTop:F2}、"
                      + $"行距 {pitch:F2}）—— 整行在视口外的**连节点一起不建**（省 quad，顺带它的点击区也不存在）");
                CheckTrue(wantTop > 0 && wantTop < N,
                          $"…而且**确实有行被丢掉**（{N - wantTop} 行落在视口外）—— 否则这一条等于没验");

                // 断 C：内层 `Content` 下**每一颗**行的矩形都要与视口相交；节点个数 == `BuiltRows`
                CheckTrue(ctn != null, "内层 `Content` 在");
                if (ctn != null)
                {
                    int nRows = 0, nOut = 0; string worst = "";
                    float minTop = float.MaxValue;
                    foreach (var rt in ctn.GetComponentsInChildren<Transform>(true))
                    {
                        if (rt.name != "PlayerRankingRow") continue;
                        nRows++;
                        float cy = LayoutSpace.PxY(rt.position.y);
                        float y1 = cy - OrigLbRowH * 0.5f, y2 = cy + OrigLbRowH * 0.5f;
                        minTop = Mathf.Min(minTop, y1);
                        if (y2 <= VpTop + 0.01f || y1 >= VpBot - 0.01f) { nOut++; worst = $"y {y1:F2}..{y2:F2}"; }
                    }
                    Check(nRows, lb.BuiltRows, "建出来的行节点个数 == `BuiltRows`（两者不许各说各的）");
                    CheckTrue(nOut == 0,
                              $"每一颗建出来的行都与视口相交（越界 {nOut} 颗"
                              + (worst.Length > 0 ? "：" + worst : "") + "）");
                    CheckNear(minTop, VpTop, 0.5f,
                              "最上面那颗行的顶边 = 视口顶 288.59（顺带证明滚动偏移是 0 —— 上面的期望值是在这个前提下算的）");
                }

                // ---- 滚到最下 ⇒ **建的是另一批行**（本件 ① 的验收）----
                var rs = lb.RowsScroll;
                CheckTrue(rs != null, "榜单那一格有滚动区（`MenuScroll` —— 原版 = `ScrollRect h=0 v=1`）");
                if (rs != null)
                {
                    CheckTrue(rs.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                    // 🔴 **2026-10-04（A35②）**：这里原来算的是 `N * LeaderboardRow.RowH + …` ——
                    //   **拿实现常量当期望值**（同式自证：把 `LeaderboardRow.RowGap` 改坏也不会红）。
                    //   现在改用 `OrigLbRowH / OrigLbRowGap`（原版字面量 + 出处，见文件里那两个常量的注释）。
                    float contentH = N * OrigLbRowH + (N - 1) * OrigLbRowGap;
                    // 🔴 这一条就是本件那个 bug 的判据：修之前 `ContentX2` 从没设过 ⇒ `ClampLo == ClampHi == 0`
                    CheckNear(rs.MaxOffset, contentH - (VpBot - VpTop), 0.5f,
                              $"可滚范围 = 内容高（{N}×100 + {N - 1}×15 = {contentH:F0}）− 视口高 {VpBot - VpTop:F2}"
                              + " —— **修之前这里恒 0**（滚轮/拖拽全被夹回 0）");
                    CheckTrue(rs.MaxOffset > 1f, "★ 确实**滚得动**了");
                    rs.SetOffset(rs.MaxOffset);
                    CheckNear(rs.Offset, rs.MaxOffset, 0.01f, "滚到了最下（`SetOffset` 没被夹回去）");
                    int wantBot = RowsInViewport(N, VpTop, VpBot, pitch, OrigLbRowH, rs.Offset);
                    Check(lb.BuiltRows, wantBot,
                          $"滚到最下 ⇒ 仍然**恰好建了与视口相交的那几行**（现算 {wantBot} 行，偏移 {rs.Offset:F2}）");
                    CheckTrue(lb.BuiltRows > 0 && lb.BuiltRows < N, "…而且仍然有行落在视口外");
                    // 「换了一批」怎么证：按**建出来的行的名次文字**看（第 1 名该滚出去、第 N 名该进来）
                    var ranks = new List<string>();
                    foreach (var rt in ctn.GetComponentsInChildren<Transform>(true))
                    {
                        if (rt.name != "PlayerRankingRow") continue;
                        var rk = FindChild(rt, "Ranking");
                        var rl = rk != null ? rk.GetComponentInChildren<Label>() : null;
                        if (rl != null) ranks.Add(rl.Text);
                    }
                    CheckTrue(ranks.Count == lb.BuiltRows, "滚完之后行节点个数**仍然** == `BuiltRows`");
                    CheckTrue(!ranks.Contains("1") && ranks.Contains(N.ToString()),
                              $"★ 滚到最下建的是**另一批**行（第 1 名已滚出视口、第 {N} 名进来了；建出来的名次："
                              + string.Join("/", ranks.ToArray()) + "）");
                    rs.SetOffset(0f);
                }

                // ---- 压边那一行的**命中区**（`MenuDraw.Hit` 吃 `clip`）：喂 7 行 ⇒ 最后建出来的那颗压在视口下沿上 ----
                const int N7 = 7;
                var seven = new List<LeaderboardRowData>();
                for (int i = 1; i <= N7; i++)
                    seven.Add(new LeaderboardRowData { Rank = i, Name = "Edge " + i, Points = "1", IsSelf = i == 1 });
                LeaderboardData.InjectForTest(LeaderboardKind.Classic, LeaderboardTab.Player, seven);
                lb.RebuildForTest();
                int want7 = RowsInViewport(N7, VpTop, VpBot, pitch, OrigLbRowH, 0f);
                Check(lb.BuiltRows, want7, $"喂 {N7} 行 ⇒ 建了与视口相交的那 {want7} 行（现算）");
                CheckTrue(want7 > 0 && want7 < N7, $"…而且确实有整行落在视口外（{N7 - want7} 行不建）");
                {
                    var c7 = FindChild(FindChild(lb.transform, "Scroll View"), "Viewport");
                    c7 = FindChild(c7, "Content");
                    CheckTrue(c7 != null, "内层 `Content` 在（下面几颗逐行找）");
                    Transform edge = null, top1 = null;
                    float edgeY = float.MinValue, topY = float.MaxValue;
                    if (c7 != null)
                        foreach (var rt in c7.GetComponentsInChildren<Transform>(true))
                        {
                            if (rt.name != "PlayerRankingRow") continue;
                            float cy = LayoutSpace.PxY(rt.position.y);
                            if (cy > edgeY) { edgeY = cy; edge = rt; }
                            if (cy < topY) { topY = cy; top1 = rt; }
                        }
                    CheckTrue(edge != null && top1 != null && edge != top1,
                              $"建出来的最后一行（压在视口下沿）与第一行都找到了（共 {lb.BuiltRows} 行）");
                    float edgeRowBot = edgeY + OrigLbRowH * 0.5f;
                    CheckTrue(edgeRowBot > VpBot + 0.5f,
                              $"最后那颗行**压在视口下沿上**（行底 {edgeRowBot:F2} > 视口底 {VpBot:F2}）—— 「压边态」的前提");
                    float ex1, ey1, ex2, ey2, tx1, ty1, tx2, ty2;
                    var hitE = FindChild(FindChild(edge, "border"), "Hit");
                    var hitT = FindChild(FindChild(top1, "border"), "Hit");
                    // 🔴 **2026-10-03 就地更正（铁律 5）**：这里原来用的是 `RenderedRect` —— 它拿**节点**的
                    //    位置当矩形中心，而 `MenuDraw.Hit` 的节点**摆在父原点**（`border` 的中心没动）、
                    //    quad 才摆在矩形中心 ⇒ **一被裁就量歪**（报出来的是「整块中心 + 截后高」）。
                    //    ⇒ 这一处必须量**那颗 `ImageQuad` 自己**（`HitQuadRect`；同「量子块、别量根节点」那条）。
                    CheckTrue(HitQuadRect(hitE, out ex1, out ey1, out ex2, out ey2),
                              "压边那一行（最后建出来的那颗）的 `border/Hit` 建出来了");
                    CheckTrue(HitQuadRect(hitT, out tx1, out ty1, out tx2, out ty2),
                              "第一行的 `border/Hit` 建出来了（当「没被截」的参照 —— 它完全在视口内）");
                    CheckTrue(ey2 <= VpBot + 0.5f,
                              $"★ 压边行的 `border/Hit` **底边被截到视口内**（实得 {ey2:F2} ≤ {VpBot:F2} + 0.5）—— "
                              + "判据 = 原版 `RectMask2D` 的射线那一面（框外的点判不中任何东西）");
                    CheckTrue((ey2 - ey1) < (ty2 - ty1) - 0.5f,
                              $"…而且**真被截短过**（整行那颗高 {ty2 - ty1:F2} vs 压边这颗 {ey2 - ey1:F2}）"
                              + "—— 否则这一条等于没验");
                }

                LeaderboardData.ClearForTest();
                lb.RebuildForTest();
                Check(lb.BuiltRows, 0, "清空 + 重画 ⇒ 又回到空态（自检不留假数据，滚动偏移也回到 0）");
            }

            // ---- 切页签（`Armies` 那一格用 `PlayerRankingRow For Army`）----
            var armiesHit = FindChild(FindChild(FindChild(lb.transform, "Tab Buttons"), "Armies"), "Hit");
            var armiesBtn = armiesHit != null ? armiesHit.GetComponent<WindowButton>() : null;
            CheckTrue(armiesBtn != null, "`Armies` 页签有点击区");
            if (armiesBtn != null) armiesBtn.Click();
            Check(lb.CurrentTab, LeaderboardTab.Armies, "点 `Armies` ⇒ 切到那一格");
            CheckTrue(FindChild(FindChild(lb.transform, "Tab Buttons"), "Player") != null
                      && FindChild(FindChild(lb.transform, "Tab Buttons"), "Armies") != null,
                      "切页之后**两个页签都还在**（重画的，不是拆掉一个）");
            lb.Close();
            Check(lb.CurrentState, WindowState.Closed, "关掉经典榜");
        }

        Section("排行榜 ③：遭遇战榜 —— **3 个页签** + 联盟行族（`AllianceRankingRow Variant`）");
        {
            var lb = menu.OpenLeaderboard(LeaderboardKind.Skirmish);
            Check(lb.TabCount, 3, "遭遇战榜 **3 个页签**（Player / Armies / Alliances）");
            var tabs = FindChild(lb.transform, "Tab Buttons");
            CheckAtWorld(FindChild(tabs, "Alliances"), 45.59f, 210.59f, 420.07f, 577.75f, "页签 3 `Alliances`");
            {
                var ai = FindChild(FindChild(tabs, "Alliances"), "Icon");
                var aq = ai != null ? ai.GetComponentInChildren<ImageQuad>() : null;
                CheckTrue(aq != null && aq.Texture != null && aq.Texture.name == "40K_Chat_icon_Alliance_v2",
                          "`Alliances` 页签图标 = `40K_Chat_icon_Alliance_v2`（本轮新导）");
            }
            LeaderboardData.InjectForTest(LeaderboardKind.Skirmish, LeaderboardTab.Alliances,
                new List<LeaderboardRowData>
                {
                    new LeaderboardRowData { Rank = 1, Guild = "[WF] Warpforge", Points = "12345" },
                });
            lb.SelectTab(LeaderboardTab.Alliances);
            Check(lb.CurrentTab, LeaderboardTab.Alliances, "切到 `Alliances`");
            var row = FindChild(FindChild(FindChild(FindChild(lb.transform, "Scroll View"), "Viewport"), "Content"),
                                "AllianceRankingRow");
            CheckTrue(row != null, "联盟行建出来了（**节点名照原版 `AllianceRankingRow`**）");
            CheckTrue(row != null && FindChild(row, "BadgeDrawer") != null,
                      "它那一格是 **`BadgeDrawer`**（不是 `border`）");
            CheckTrue(row != null && FindChild(row, "border") == null, "联盟行**没有头像格**（原版就没有）");
            CheckText(row != null ? TextOf(FindChild(row, "Guild Name")) : null, "[WF] Warpforge",
                      "联盟行的名字走 `Guild Name`（玩家行走 `Name`）");
            LeaderboardData.ClearForTest();
            lb.Close();
        }

        Section("排行榜 ④：轮抽榜 —— 默认 `Alliances`，`Armies` 那格**原版就没接线**（点了如实出声）");
        {
            var lb = menu.OpenLeaderboard(LeaderboardKind.Draft);
            Check(lb.TabCount, 2, "轮抽榜 **2 个页签**（树上也是 2 个）");
            Check(lb.CurrentTab, LeaderboardTab.Alliances,
                      "**默认落在 `Alliances`** —— 原版 `tabDefinitions` 只登记了它（`Open()` 开 `tabDefinitions[0]`）");
            var tabs = FindChild(lb.transform, "Tab Buttons");
            var hit = FindChild(FindChild(tabs, "Armies"), "Hit");
            var btn = hit != null ? hit.GetComponent<WindowButton>() : null;
            CheckTrue(btn != null, "`Armies` 页签**建出来了**（照树）");
            if (btn != null) btn.Click();
            Check(lb.CurrentTab, LeaderboardTab.Alliances,
                  "点了 `Armies` ⇒ **不切换**（原版 `tabDefinitions` 没登记它 ⇒ 那个钮点不动；我们如实出声）");
            LeaderboardData.InjectForTest(LeaderboardKind.Draft, LeaderboardTab.Alliances,
                new List<LeaderboardRowData> { new LeaderboardRowData { Rank = 2, Guild = "[WF] X", Points = "900" } });
            lb.RebuildForTest();
            var row = FindChild(FindChild(FindChild(FindChild(lb.transform, "Scroll View"), "Viewport"), "Content"),
                                "AllianceRankingRow");
            var ri = row != null ? FindChild(row, "RankingIcon") : null;
            var rq = ri != null ? ri.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(rq != null && rq.Texture != null && rq.Texture.name == "40k_battle_Win_Skull",
                      "轮抽的联盟行 `RankingIcon` = **`40k_battle_Win Skull`**（与遭遇战那族的唯一差别）");
            LeaderboardData.ClearForTest();
            lb.Close();
        }

        Section("排行榜 ⑤：嵌入版 `Ranked Leaderboard Display`（🔴 **原版全库零引用** —— 没有入口，我们也不编）");
        {
            var lb = menu.OpenLeaderboard(LeaderboardKind.Embedded);
            var t = lb.transform;
            CheckTrue(FindChild(t, "Menu Dark Background") == null, "**没有压暗层**（原版这一棵就没有）");
            CheckTrue(FindChild(t, "Tab Buttons") == null, "**没有页签**");
            CheckTrue(FindChild(t, "Generic Close Button Orange") == null, "**没有关闭键**");
            CheckTrue(FindChild(t, "Army Selector") == null, "**没有 `Army Selector`**（`subMenu` 的 PPtr = 0）");
            CheckAtWorld(FindChild(t, "Generic Window Red Background Big"), 208.72f, 1727.48f, 47.60f, 1054.86f,
                         "红底板（比三扇全屏榜那块大一圈 1518.76×1007.26）");
            CheckAtWorld(FindChild(t, "Title"), 643.55f, 1276.45f, 73.06f, 174.90f, "`Title`（同一句 `TOP PLAYERS`）");
            CheckAtWorld(FindChild(t, "TopBar"), 282.95f, 1637.05f, 170.00f, 176.00f, "`TopBar`");
            CheckAtWorld(FindChild(FindChild(t, "Content"), "Scroll View"), 248.99f, 1671.01f, 176.01f, 1006.93f,
                         "列表区（⚠️ **高是推算的**：原版这一棵的 `Scroll View` 序列化高就是 0、没有 `LayoutElement`）");
            Check(lb.BuiltRows, 0, "没有数据 ⇒ 一行都不建");
            // 🆕 2026-10-03（本件 ①）：**同一份 `RebuildRows` 换一个视口也得成立** —— 嵌入版的视口高
            //   是 **830.92**、三扇弹窗是 649.24 ⇒ 这里现算一遍，防「只对某一种情况调过」（铁律 5·c）。
            {
                const int NE = 12;
                var manyE = new List<LeaderboardRowData>();
                for (int i = 1; i <= NE; i++)
                    manyE.Add(new LeaderboardRowData { Rank = i, Name = "Emb " + i, Points = "1", IsSelf = i == 1 });
                LeaderboardData.InjectForTest(LeaderboardKind.Embedded, LeaderboardTab.Player, manyE);
                lb.RebuildForTest();
                int wantE = RowsInViewport(NE, 176.01f, 1006.93f, OrigLbRowH + OrigLbRowGap,
                                           OrigLbRowH, 0f);
                Check(lb.BuiltRows, wantE,
                      $"嵌入版：{NE} 行里建了与视口（176.01..1006.93）相交的那 {wantE} 行（现算，⛔ 不写死）");
                CheckTrue(wantE > 0 && wantE < NE,
                          "…而且**确实有行在视口外**（嵌入版视口高 830.92 ≠ 三扇弹窗那 649.24）");
                var rsE = lb.RowsScroll;
                CheckTrue(rsE != null && rsE.MaxOffset > 1f,
                          "嵌入版这一格**也滚得动**（可滚范围 "
                          + (rsE != null ? rsE.MaxOffset.ToString("F2") : "—") + "px；修之前这一格同样恒 0）");
                LeaderboardData.ClearForTest();
                lb.RebuildForTest();
                Check(lb.BuiltRows, 0, "清完 ⇒ 回到空态（不留假数据）");
            }
            lb.Close();
        }

        Section("段位信息块 `Ranked Division Info/Content`（**实例 ③** —— `RankedEventWindowV2` 下那个）");
        {
            var rc = menu.Find("Base Game Mode Container 1x1 - Ranked");
            var rh = rc != null ? FindChild(rc, "Hit") : null;
            var rwb = rh != null ? rh.GetComponent<WindowButton>() : null;
            if (rwb != null) rwb.Click();
            var rk = RankedEventWindow.LastOpened;
            CheckTrue(rk != null, "排位窗开出来了");
            if (rk != null)
            {
                var col = FindChild(rk.transform, "Ranked Division Info");
                CheckAtWorld(col, 0f, 638f, 146.93f, 959.07f, "`Ranked Division Info`（638×812.13）");
                var dvContent = FindChild(col, "Content");
                CheckAtWorld(dvContent, 65.05f, 572.95f, 222.75f, 886.57f, "`Content`（整棵的根）");
                CheckAtWorld(FindChild(dvContent, "RankTitleBG"), 90.40f, 547.60f, 222.75f, 296.45f,
                             "`RankTitleBG`（段位名那一条）");
                CheckAtWorld(FindChild(dvContent, "DivisionText"), 141.30f, 496.70f, 225.61f, 293.59f, "`DivisionText`");
                CheckText(TextOf(FindChild(dvContent, "DivisionText")), "",
                          "🔴 段位名**留空**（`Division V` 是服务器数据 —— 用户口径：不编数字）");
                // 🆕 2026-10-03：原版这行 TMP `charSpacing = -2.6` —— **留空也把值设上**（将来填字就对了）
                CheckNear(CharSpacingOf(FindChild(dvContent, "DivisionText")), -2.6f, 0.01f,
                          "`DivisionText` 的 `characterSpacing` = **−2.6**（原版 TMP 原文）");
                var di = FindChild(dvContent, "DivisionImage");
                CheckAtWorld(di, 49.10f, 588.90f, 234.04f, 806.66f, "`DivisionImage`");
                CheckTrue(di != null && di.GetComponentInChildren<ImageQuad>() == null,
                          "🔴 段位大图**不画**（`RankedDivisionsSO` 本地没有 ⇒ 画任何一张都是**编一个段位**）");
                var footer = FindChild(dvContent, "footer");
                CheckAtWorld(FindChild(footer, "MainRating"), 114f, 524f, 717.95f, 785.75f,
                             "`footer/MainRating`（⚠️ 原版工具把它算成 0 高 —— 嵌套布局组不递归；按 `MainRating` 取）");
                var ms = FindChild(footer, "Mission Milestones Progress");
                CheckAtWorld(FindChild(ms, "Background"), 63.38f, 574.62f, 715f, 788.70f,
                             "`Mission Milestones Progress/Background`（**比父宽** 511.24 vs 410 —— 原版就那样，别「对齐」掉）");
                CheckTrue(FindChild(ms, "counter") == null,
                          "`counter` **不建**（里程碑计数：出厂 inactive + 全 bundle 零引用者）");
                var steps = FindChild(ms, "steps");
                CheckAtWorld(FindChild(steps, "RankedSealStep"), 221.55f, 321.55f, 708.59f, 795.10f,
                             "阶梯 1 `RankedSealStep`（100×86.51）");
                CheckAtWorld(FindChild(steps, "RankedSealStep (1)"), 316.45f, 416.45f, 708.59f, 795.10f, "阶梯 2");
                {
                    var empty = FindChild(FindChild(steps, "RankedSealStep"), "Empty");
                    var eq = empty != null ? empty.GetComponentInChildren<ImageQuad>() : null;
                    CheckTrue(eq != null && eq.Texture != null && eq.Texture.name == "Rank_Skull_Empty",
                              "阶梯的 `Empty` 态 = `Rank_Skull_Empty`（**本轮才导进来的那张**）");
                    var fill = FindChild(FindChild(steps, "RankedSealStep"), "Fill");
                    CheckTrue(fill != null && fill.GetComponentInChildren<ImageQuad>() == null,
                              "🔴 `Fill`（已达成那一档）**不画** —— 进度是服务器数据（保留 `Empty` 态）");
                }
                CheckTrue(FindChild(dvContent, "Timer") == null, "**`Timer` 不建**（同上：赛季倒计时不做）");
                CheckTrue(FindChild(FindChild(dvContent, "Legendary Ratings"), "Position") != null
                          && FindChild(FindChild(dvContent, "Legendary Ratings"), "Global Rating") != null,
                          "`Legendary Ratings` 下那两支**建了结构**（原版预制体里就是 inactive ⇒ 里面不画东西）");
                Check(rk.MissingArt.Count, 0, "排位窗（含段位块）**图一张都不缺**");
                CheckHoverSwap(rk.transform, "排位窗（含段位块）");
                Shoot("08_排位窗_段位块.png");
                rk.Close();
            }
        }


        // ============================================================ 社交 / 聊天 / 好友挑战
        //   （2026-09-27 建 · 多人界面那一批 第 4 件）
        // 判据：社交 → `资料/普查产出_0927/社交_联盟与好友页.md`（§A·1 层×参数 · §A·2 行族独立根 · §B 判定）；
        //       聊天窗 + 挑战弹窗 → `资料/普查产出_0927/聊天窗与挑战弹窗.md`（§A / §B）；
        //       三条**入口链** → `资料/普查产出_0927/多人界面_入口与调用.md`（§① SOCIAL 键 · §② ChatPreview · §③）。
        // 🔴 **断的全是「原版参数」**，不是我们自己的常量（§10·3 第 3 层）。
        Section("社交窗 `Social Submenu Variant`（入口 = 左竖导航第 5 键 —— **这一条是复刻，不是我们挑的**）");
        {
            var socialBtn = menu.Find("Main Menu Navigation Button - Social");
            var sh = socialBtn != null ? FindChild(socialBtn, "Hit") : null;
            var swb = sh != null ? sh.GetComponent<WindowButton>() : null;
            CheckTrue(swb != null && swb.onClick != null, "SOCIAL 键**接了点击**（原来点了只打一句日志）");
            if (swb != null && swb.onClick != null)
            {
                swb.onClick();
                var sw = SocialWindow.LastOpened;
                CheckTrue(sw != null && sw.CurrentState == WindowState.Open, "点 SOCIAL ⇒ **真的开了社交窗**");
                if (sw != null)
                {
                    Check(sw.type, WindowType.Fullscreen, "`type` = **0 Fullscreen**（原文）");
                    Check(sw.placement, WindowsPlacement.Canvas, "`windowsPlacement` = **5 Canvas**（原文）");
                    CheckTrue(sw.closeOnEsc, "`closeOnESC` = **1**（原文）");
                    CheckNear(sw.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = **1.0**（原文）");

                    var t = sw.transform;
                    // 壳：与奖励窗/商店**同一份**（`Content Area` / `Tab Buttons` / `Tabs` 实测同值）
                    CheckAtWorld(FindChild(t, "Content Area"), 167.17f, 1920.00f, 70.94f, 1080f,
                                 "`Content Area`（**与奖励窗逐值相同** ⇒ 直接复用那套壳）");
                    var bar2 = FindChild(FindChild(t, "Content Area"), "Tab Buttons");
                    CheckAtWorld(bar2, 167.17f, 332.17f, 70.94f, 1080f, "`Tab Buttons`（165 宽）");
                    // 🔴 **原版这条 `Shadow` 是 0 高**（`sz=(165,0)`）⇒ 我们**不建** ——
                    //    照奖励窗那个 47.64 画会**凭空多一条线**（这就是「一个值 ≠ 全部情况」那条铁律的活例子）。
                    CheckTrue(FindChild(bar2, "Shadow") == null,
                              "`Tab Buttons/Shadow` **不建**（原版 `sizeDelta=(165,0)` ⇒ 静态看不见）");
                    CheckAtWorld(FindChild(FindChild(t, "Content Area"), "Tabs"), 167.17f, 1920.00f, 70.94f, 1080f, "`Tabs`");

                    // 左栏两键：`Alliances` / `Friends`（**位置是 VLG 算出来的**，165×180，顶 190.94 / 370.94）
                    for (int i = 0; i < SocialWindow.Buttons.Length; i++)
                    {
                        float top = 70.94f + 120f + 180f * i, bot = top + 180f;
                        var br = FindChild(bar2, "SocialTabButton_" + i);
                        CheckAtWorld(br, 167.17f, 332.17f, top, bot, $"左栏第 {i + 1} 键（165×180）");
                        CheckText(TextOf(FindChild(br, "Text")), SocialWindow.Buttons[i].Label.ToUpperInvariant(),
                                  $"左栏第 {i + 1} 键文案 = `{SocialWindow.Buttons[i].Label}`（原版 TMP 是 UpperCase 款）");
                        var iq = FindChild(br, "Icon") != null
                               ? FindChild(br, "Icon").GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(iq != null && iq.Texture != null && iq.Texture.name == SocialWindow.Buttons[i].Art,
                                  $"第 {i + 1} 键图标 = `{SocialWindow.Buttons[i].Art}`"
                                  + (i == 1 ? "（⚠️ 名字里是**空格 + v2**，落盘成下划线版）" : ""));
                    }
                    Check(sw.tabButtons.CurrentType, WindowTabType.SocialAlliances,
                          "**默认落在 `Alliances`**（原版 `Alliances Tab` act T / `Friends Tab` act **F**）");
                    CheckTrue(FindChild(FindChild(FindChild(t, "Content Area"), "Tabs"), "Friends Tab") != null
                              && !FindChild(FindChild(FindChild(t, "Content Area"), "Tabs"), "Friends Tab").gameObject.activeSelf,
                              "`Friends Tab` 出厂**是关的**");

                    // ---- 未入盟支（`AllianceSearchTab`）----
                    var aRoot = FindChild(FindChild(FindChild(t, "Content Area"), "Tabs"), "Alliances Tab");
                    var nmv = FindChild(aRoot, "AllianceNotMemberVariant");
                    CheckAtWorld(nmv, 332.17f, 1919.50f, 70.90f, 1080.02f, "`AllianceNotMemberVariant`（默认这一支）");
                    var hdr = FindChild(nmv, "Alliance Header Buttons");
                    CheckAtWorld(hdr, 331.67f, 1920.00f, 89.87f, 162.04f, "`Alliance Header Buttons`");
                    CheckAtWorld(FindChild(hdr, "Generic Tab UI Button Search"), 360.47f, 620.47f, 90.30f, 157.94f,
                                 "`Join` 键（`40K_tab_button_overwindow` 489×97 · 九宫 188,0,99,30）");
                    CheckText(TextOf(FindChild(hdr, "Generic Tab UI Button Search")), "Join",
                              "第一个键的文案是 **`Join`**（⚠️ 节点名叫 `…Button Search`）");
                    CheckAtWorld(FindChild(hdr, "Generic Tab UI Button Create"), 632.92f, 892.92f, 90.30f, 157.94f,
                                 "`Create` 键");
                    CheckText(TextOf(FindChild(hdr, "Generic Tab UI Button Create")), "Create", "第二个键的文案 `Create`");
                    var lv = FindChild(nmv, "List View");
                    CheckAtWorld(lv, 360.99f, 1902.59f, 162.04f, 1080.02f, "`List View`（`JoinAllianceMenu`）");
                    CheckAtWorld(FindChild(lv, "Search Field"), 1402.00f, 1798.57f, 172.90f, 229.86f, "搜索框");
                    CheckAtWorld(FindChild(lv, "Generic Round Button Variant"), 1800.78f, 1860.78f, 171.38f, 231.38f,
                                 "搜索圆钮（`40k_general_bt_yellow`）");
                    CheckAtWorld(FindChild(lv, "List Area"), 361.00f, 1874.90f, 252.29f, 1079.77f, "`List Area`");
                    // 数据全空 ⇒ 三个列表**一行都不建**（原版也没有空态节点，留白即可）
                    Check(SocialData.Invitations.Count + SocialData.OpenAlliances.Count, 0,
                          "本地邀请 / 公开联盟 **0 条**（原版读服务器）");
                    Check(FindChild(FindChild(FindChild(lv, "Invitations"), "List"), "Invitation List Entry"), null,
                          "⇒ `Invitations/List` 底下**一行都不建**");

                    // ---- 建盟表：出厂 act F，点 `Create` 键才亮（**这个切换是纯本地的，能用**）----
                    var cav = FindChild(nmv, "Create Alliance View");
                    CheckTrue(cav != null && !cav.gameObject.activeSelf, "`Create Alliance View` 出厂 **act F**");
                    CheckAtWorld(cav, 368.48f, 1882.38f, 165.12f, 1080.02f, "`Create Alliance View` 矩形");
                    var createBtn = FindChild(hdr, "CreateHit");
                    var cb2 = createBtn != null ? createBtn.GetComponent<WindowButton>() : null;
                    CheckTrue(cb2 != null && cb2.onClick != null, "`Create` 键接了点击");
                    if (cb2 != null)
                    {
                        cb2.onClick();
                        CheckTrue(cav.gameObject.activeSelf && !lv.gameObject.activeSelf,
                                  "点 `Create` ⇒ **建盟表亮、`List View` 藏**（原版 `ShowCreateAllianceMenu`）");
                        var jb = FindChild(hdr, "JoinHit");
                        if (jb != null && jb.GetComponent<WindowButton>() != null) jb.GetComponent<WindowButton>().onClick();
                        CheckTrue(lv.gameObject.activeSelf && !cav.gameObject.activeSelf, "点 `Join` ⇒ 切回来");
                    }
                    // ⚠️ **左对齐的文字不能断中心**：`MenuDraw.AlignLeft` 会把整块字挪到矩形左边缘
                    // （`Title` 页那条教训）⇒ 这里断的是**左边缘 x**。
                    var nt = FindChild(cav, "Name input title");
                    var ntl = nt != null ? nt.GetComponent<Label>() : null;
                    float nlLeft = (nt != null && ntl != null) ? (nt.position.x - ntl.WorldW * 0.5f) * 108f + 960f : -1f;
                    CheckTrue(Mathf.Abs(nlLeft - 432.47f) < 1f,
                              $"建盟表「联盟名」标题**左对齐到 x=432.47**（原版 `Left/Middle`；实得 {nlLeft:F2}）");
                    CheckAtWorld(FindChild(cav, "Name Input"), 432.47f, 1332.47f, 307.95f, 367.35f,
                                 "`Name Input`（打不了字 —— 出声，不静默）");

                    // ---- 已入盟支（`AllianceMemberTab`）：本地**走不到**，但建出来了 ----
                    var mv = FindChild(aRoot, "AllianceMemberVariant");
                    CheckTrue(mv != null && !mv.gameObject.activeSelf,
                              "`AllianceMemberVariant` 出厂 **act F**（原版按 `AlliancesManager` 二选一 ⇒ 本地恒走不到）");
                    mv.gameObject.SetActive(true);
                    var mt = sw.PageAlliances.Member;
                    CheckTrue(mt != null && mt.GeneralView != null && mt.TrophiesView != null,
                              "建出来了：`GeneralDetails` + `TrophiesWindow` 两棵都在");
                    CheckTrue(mt.GeneralView.activeSelf && !mt.TrophiesView.activeSelf,
                              "默认 `General`（原版 `TrophiesWindow` act **F**）");
                    var trh = FindChild(FindChild(mv, "Alliance Header Buttons (1)"), "Generic Tab UI Button Trophies/Hit");
                    if (trh != null && trh.GetComponent<WindowButton>() != null)
                    {
                        trh.GetComponent<WindowButton>().onClick();
                        CheckTrue(mt.TrophiesView.activeSelf && !mt.GeneralView.activeSelf,
                                  "点 `Trophies` ⇒ 切到奖杯页（**这个切换是纯本地的，能用**）");
                        mt.ShowGeneral();
                    }
                    mv.gameObject.SetActive(false);

                    // ---- 好友页 ----
                    var tabsNode = FindChild(FindChild(t, "Content Area"), "Tabs");
                    sw.tabButtons.Click(1);
                    var fr = FindChild(tabsNode, "Friends Tab");
                    CheckTrue(fr != null && fr.gameObject.activeSelf, "点左栏第 2 键 ⇒ 切到 `Friends Tab`");
                    var hd = FindChild(fr, "Header");
                    // 🔴 **`Header` 比窗框宽**（右边界 2085 > 1920）—— 原版就这么摆，别「对齐」掉
                    CheckAtWorld(hd, 332.17f, 2085.00f, 70.94f, 300.54f, "`Header`（⚠️ **右边界超出窗框**，原版如此）");
                    var fp = FindChild(hd, "Find players panel");
                    CheckAtWorld(fp, 357.37f, 1100.86f, 147.14f, 256.53f, "`Find players panel`");
                    CheckAtWorld(FindChild(fp, "Search Field"), 398.77f, 925.42f, 173.35f, 230.31f, "搜索框");
                    CheckText(TextOf(FindChild(fp, "Search Field")), "Enter player name", "占位文案 `Enter player name`");
                    CheckAtWorld(FindChild(fp, "Add Friend Button"), 953.12f, 1033.12f, 167.63f, 236.04f,
                                 "`Add Friend Button`（⚠️ **没有 Button Text**，纯图标钮）");
                    CheckAtWorld(FindChild(fp, "Instant duel Button"), 1045.70f, 1125.70f, 167.63f, 236.04f,
                                 "`Instant duel Button`");
                    CheckText(TextOf(FindChild(fp, "Search Player")), "Search player", "`Search Player` 那行字");
                    var fl = FindChild(fr, "Friends List");
                    CheckText(TextOf(FindChild(fl, "Friends Title")), "Your friends:", "`Your friends:`");
                    CheckAtWorld(FindChild(fl, "Divisor line"), 343.14f, 1898.89f, 311.12f, 314.80f, "分隔线");
                    var fcont = FindChild(fl, "Friends Container");
                    CheckAtWorld(fcont, 332.15f, 1875.80f, 314.80f, 1080.06f, "`Friends Container`（`ScrollRect`）");
                    // 好友表恒空 ⇒ 0 行；喂一条 ⇒ 建出一行、且尺寸 = 网格 cell
                    Check(SocialData.Friends.Count, 0, "本地好友 **0 条**（服务器源）");
                    Check(FindChild(FindChild(FindChild(fcont, "Viewport"), "Content"), "Friend Info Item"), null,
                          "⇒ 好友行**一行都不建**（原版出厂 0 子，行全是运行期 `Instantiate` 的）");
                    SocialData.Friends.Add(new SocialData.Friend { Name = "Test Friend", Online = false });
                    sw.PageFriends.RebuildForTest();
                    var frow = FindChild(FindChild(FindChild(fcont, "Viewport"), "Content"), "Friend Info Item");
                    CheckTrue(frow != null, "喂一条 ⇒ **建出一行**");
                    if (frow != null)
                    {
                        CheckAtWorld(frow, 348.15f, 1069.45f, 334.80f, 419.62f,
                                     "行 = **网格 cell 721.3×84.82**（`GridLayoutGroup` 的 cell，不是 prefab 自带的 670.16）");
                        CheckText(TextOf(FindChild(frow, "Friend name")), "Test Friend", "行里那行名字");
                        CheckTrue(FindChild(frow, "Disconnected") != null && FindChild(frow, "Connected Image") == null,
                                  "离线 ⇒ **只有 `Disconnected`** 那个点（在线才建 `Connected Image`）");
                        // 三颗右对齐的图标钮（`a=(1,.5) pos=(-224.771/-133.8/-42.829,0)`，各 68.644×69.315）
                        var rowR2 = new PxRect(348.15f, 334.80f, 1069.45f, 419.62f);
                        float cy = (rowR2.y1 + rowR2.y2) * 0.5f;
                        CheckAtWorld(FindChild(frow, "Challenge button"),
                                     rowR2.x2 - 133.8f - 34.322f, rowR2.x2 - 133.8f + 34.322f,
                                     cy - 34.6575f, cy + 34.6575f,
                                     "行内 `Challenge button`（**从右边沿往里 133.8**，不是从左边算）");
                    }
                    SocialData.ResetForTest();
                    sw.PageFriends.RebuildForTest();
                    CheckTrue(FindChild(FindChild(FindChild(fcont, "Viewport"), "Content"), "Friend Info Item") == null,
                              "清空数据 ⇒ 行又没了（原版 `OnOpen` 清空重填）");
                    sw.tabButtons.Click(0);

                    // ---- 🆕 2026-10-03（A25①）：`SocialPage.Clip` 这条路**真的带电了吗** ----
                    // 病根（改之前）：`Clip` 是 `protected`、**全仓一处赋值都没有** ⇒ `Rect` / `Text` / `Hit` /
                    //   `Cosmetic` 四处转发过去的 `clip` **恒为 null** ⇒ 社交页画的东西**一处都吃不到裁切**
                    //   （滚动内容越出视口照样画满）。现在补了写入口：`SocialPage.SetClip` / `SocialView.SetClip`。
                    // 🔴 **判据 / 矩形都取原版真值**：`Friends Container>Viewport` = 332.15,314.80→1875.80,1080.06
                    //   （普查 §A·1 第 396 行：它身上就是 `RectMask2D`；上面 `CheckAtWorld(fcont, …)` 刚钉过同一个数）。
                    // 🔴 **凭什么说这几条能真红**：下面每块探针都**跨在视口那条边上** ⇒ 只要 `Clip` 没传到
                    //   `MenuDraw`，量到的就是**整块**（下沿 1150 / 命中区照建 / 右边那块字照样建）——
                    //   把 `SetClip` 或任一处转发拆掉，这几条立刻红。
                    {
                        var fvp = new PxRect(332.15f, 314.80f, 1875.80f, 1080.06f);   // 原版 `Viewport` 真值
                        var probe = MenuDraw.Node(sw.transform, "ClipProbe", fvp);      // 探针的临时节点（断完就删）
                        var pg = sw.PageFriends;                                        // **真的页对象**，不是派生出来的假页

                        pg.SetClip(fvp);
                        CheckTrue(pg.ClipNow.HasValue && MenuDraw.SameRect(pg.ClipNow.Value, fvp),
                                  "★ `SetClip(视口)` 之后 `ClipNow` 就是那个视口（写入口真的通了）");
                        // 三块探针：跨下沿的图 / 整块在下沿以外 / 跨下沿的命中区（各自验一处转发）
                        var low = pg.Rect(probe, null, new PxRect(500f, 950f, 900f, 1150f), "ClipProbeLow", 0);
                        var outHit = pg.Hit(probe, "ClipProbeOut", new PxRect(500f, 1100f, 900f, 1200f), 0, () => { });
                        var edgeHit = pg.Hit(probe, "ClipProbeEdge", new PxRect(500f, 950f, 900f, 1150f), 0, () => { });
                        // `SocialPage.Text` 那一处**只判横轴**（判据就是它自己那一句）⇒ 拿「整块在右沿以外」来验
                        var outText = pg.Text(probe, new PxRect(1900f, 500f, 2000f, 600f), "x", Color.white,
                                              "ClipProbeText", 30f, 0);
                        // 九宫格（`Rect`/`Text`/`Hit`/`Cosmetic` 都传了 `Clip`，**九宫格这一路原来漏了**，同一批补上）：
                        // 先用**框内**那一次证明「图取得到」（否则下面那次 null 是自我实现、什么都验不到）
                        var nineIn = pg.Nine(probe, "40K_dropdown_bg", new PxRect(500f, 400f, 900f, 500f),
                                             new Vector4(23f, 20f, 23f, 20f), "ClipProbeNineIn", 0);
                        var nineOut = pg.Nine(probe, "40K_dropdown_bg", new PxRect(500f, 1100f, 900f, 1200f),
                                              new Vector4(23f, 20f, 23f, 20f), "ClipProbeNineOut", 0);
                        pg.SetClip(null);
                        CheckTrue(pg.ClipNow == null,
                                  "`SetClip(null)` 清掉了（**画完必须清** —— 不清的话后面画的件会继续吃这道裁切）");

                        float lx1 = 0f, ly1 = 0f, lx2 = 0f, ly2 = 0f;
                        CheckTrue(low != null && RenderedRect(low.transform, out lx1, out ly1, out lx2, out ly2),
                                  "跨在下沿上的那块图建出来了");
                        CheckNear(ly2, fvp.y2, 0.5f,
                                  "★ 它的**下边缘被截到视口下沿 1080.06**（没设 `Clip` 时这里会是 1150 —— 真红点）");
                        CheckNear(ly1, 950f, 0.5f, "…上边缘没被碰（只截越界的那一侧）");
                        CheckTrue(outHit == null,
                                  "★ 整块在视口外的命中区**连节点一起不建**（原版 `RectMask2D` 的射线那一面）");
                        CheckTrue(outText == null, "★ 整块在视口右沿以外的文字**不建**（`Text` 那一处转发）");
                        CheckTrue(nineIn != null, "框内的九宫格建出来了（`40K_dropdown_bg` 取得到图 —— 下面那条的对照组）");
                        CheckTrue(nineOut == null,
                                  "★ 整块在视口外的**九宫格也连节点一起不建**（`Nine` 那一处原来漏了传 `Clip`）");
                        float ex1 = 0f, ey1 = 0f, ex2 = 0f, ey2 = 0f;
                        CheckTrue(edgeHit != null && HitQuadRect(edgeHit, out ex1, out ey1, out ex2, out ey2),
                                  "跨在下沿上的命中区建出来了");
                        CheckNear(ey2, fvp.y2, 0.5f, "★ 它的 quad **也被截到同一条下沿**（命中区跟着裁）");

                        // 子视图那一层：`SocialView.SetClip` 转调宿主页 ⇒ 同一道裁切（`AllianceSearchTab` 是真的视图）
                        var view = sw.PageAlliances.Search;
                        CheckTrue(view != null, "联盟页那一支的子视图在（`AllianceSearchTab`）");
                        if (view != null)
                        {
                            view.SetClip(fvp);
                            var vq = view.Rect(probe, null, new PxRect(500f, 950f, 900f, 1150f), "ClipProbeView", 0);
                            view.SetClip(null);
                            float vx1 = 0f, vy1 = 0f, vx2 = 0f, vy2 = 0f;
                            CheckTrue(vq != null && RenderedRect(vq.transform, out vx1, out vy1, out vx2, out vy2),
                                      "子视图画的同一块也建出来了");
                            CheckNear(vy2, fvp.y2, 0.5f,
                                      "★ 子视图（`SocialView.SetClip` → 宿主页）**也吃到同一道裁切**");
                        }

                        SocialWindow.DestroySafe(probe.gameObject);   // 探针断完就删（别留给后面的断言与截图）
                    }

                    // ============================================================ 🆕 2026-10-03（A25④）：
                    // **三处滚动视口**（联盟页公开列表 / 好友页 / 已入盟支的成员列）**真的接上滚动 + 裁切了吗**。
                    // 🔴 改之前：三个视口**一处 `MenuScroll` 都没有**（`grep MenuScroll Shell/{AlliancesTab,FriendsTab,
                    //   AllianceMemberTab}.cs` 零命中）⇒ 内容一多就**画到框外**（原版那三个 `RectMask2D`/`Mask` 没人等效）。
                    //   本批补了「整行滚出视口 ⇒ 不建」这道守卫 ⇒ **没有滚动区就会把后面的行彻底藏掉** ——
                    //   两件必须**一起**补（`BattleLogPopup` 上就是这么踩过来的）。下面每处都断三件：
                    //   ① 滚动区真的建了（且档位照原版）· ② 走**真路**（`PointerLayer.WheelAt(视口中心, ∓120)`）真的滚得动
                    //   · ③ 越界内容**不再画到框外**（量**渲出来那块**，不是节点 —— 裁剪会把 quad 挪走、节点不动）。
                    // 🔴 **档位先读原版那个 `ScrollRect` 的 `m_MovementType` 再定**：三件**全是 `m_MovementType = 1`**
                    //   （原始 JSON 实读：`bundle_menus_assets_all/MonoBehaviour/` 里 `…-992038356198235997.json`（Friends
                    //   Container）· `…-8780120914984378205.json`（Open Alliances，`RecyclableScrollRect : ScrollRect`）
                    //   · `…-2224558054710517597.json`（MemberList Scroll View），三条都靠 `m_Content` 的 pid 认的）。
                    //   UGUI 的枚举是 `Unrestricted=0 / Elastic=1 / Clamped=2`（判据 = 本工程那份 UGUI 源码的
                    //   `ScrollRect.MovementType`）⇒ **三处都是 Elastic**。
                    // ⚠️ `Shell/MenuScroll.cs` 文件头与 `资料/阶段二_滚动与指针_原版规格.md` §1·1 那句
                    //   「1 = Clamped / 2 = Elastic」**是反的**（本批发现，已写进报告）：同一仓里 `BattleLogPopup.cs:19`
                    //   按 `2 (Clamped)` / `1 (Elastic)` 读、`资料/普查产出_0923/A3_Cards页.md:55` 写 `1(Elastic)`。
                    // ⚠️ 期望值一律**现算**（`RowsInViewport` / `CellsInViewport`），参数取**原版值**；⛔ 不写死条数。
                    {
                        var pl3 = PointerLayer.Instance;
                        float qTol = 1.0f;      // 量「有没有画到框外」的容差（被截到边上的那块会正好落在边界上）

                        // ------------------------------------------------ ① `Open Alliances`（联盟页 · 未入盟支）
                        {
                            const int NA = 11;
                            for (int i = 1; i <= NA; i++)
                                SocialData.OpenAlliances.Add(new SocialData.AllianceListing
                                { Name = "Open " + i, Region = "Global", Members = i, MemberMax = 20, Rating = "" });
                            var search = sw.PageAlliances.Search;
                            search.RebuildForTest();          // = 原版 `FillOpenAlliances`（清空重填）那条路
                            var oaNode = FindChild(FindChild(lv, "List Area"), "Open Alliances");
                            var oaVp = FindChild(oaNode, "Viewport");
                            var oaList = FindChild(oaVp, "List");
                            CheckAtWorld(oaVp, 360.99f, 1874.90f, 337.29f, 1079.77f,
                                         "`Open Alliances>Viewport`（原版 `Image + RectMask2D`；下面滚动区的视口就是它）");
                            var os = search.OpenListScroll;
                            CheckTrue(os != null,
                                      "★ 联盟页 `Open Alliances` 那一格**有滚动区了**（原版 = `RecyclableScrollRect`；"
                                      + "改之前一处都没有）");
                            if (os != null && oaList != null)
                            {
                                const float VTop = 337.29f, VBot = 1079.77f;       // = 原版 `Viewport`（上面刚钉过）
                                // 🔴 **原版值，⛔ 不引用实现常量**（A35②）—— 而且**这两列的行距不是一个数**（A35①）：
                                //   · 行高 **110** = 行 prefab `Alliance List Entry` 的 `sizeDelta.y`
                                //     （§A·2·2 的独立根 `471114273799851884`）；
                                //   · 行距 **5** = `Open Alliances` 那一格 **`RecyclableScrollRect`** 的 `_spacingY`
                                //     （原始 JSON 实读：`bundle_menus_assets_all/MonoBehaviour/
                                //      MonoBehaviour_-8780120914984378205.json` 的 `_spacingY: 5.0` /
                                //      `_cellHeight: 110.0` / `IsGrid: 0`）⇒ **行距 = `_spacingY + _cellHeight` = 115**
                                //     （判据 = `PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c:255,270`
                                //      与 `…_InitCoroutine_d__19__MoveNext.c:57-63`；同仓 `A3_Cards页.md:78`）。
                                //   ⚠️ `Invitations` 那一列才是间距 **10**（它自己是普通 VLG、`spacing=10`，
                                //     普查 `社交_联盟与好友页.md:67`）—— **这里原来错抄了那个 10**：
                                //     11 行时可滚范围多 50px、第 6 行起 y 全偏，而断言与实现同式 ⇒ 必绿（①）。
                                const float RowH0 = 110f, SpacingY0 = 5f;
                                float pitchA = RowH0 + SpacingY0;
                                CheckNear(os.Viewport.y1, VTop, 0.5f,
                                          "滚动区视口 = `Viewport` 那个节点自己的矩形（**没有另挑一个**）");
                                CheckTrue(os.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                                CheckTrue(os.Elastic, "**Elastic**（原版 `m_MovementType=1`：拖出去有橡皮筋、松手回弹）");
                                float contentA = NA * RowH0 + (NA - 1) * SpacingY0;
                                CheckNear(os.MaxOffset, contentA - (VBot - VTop), 0.5f,
                                          $"可滚范围 = 内容高（{NA}×110 + {NA - 1}×5 = {contentA:F0}）− 视口高 {VBot - VTop:F2}"
                                          + " —— **补之前这里恒 0**（滚轮全被夹回 0，而「整行滚出视口 ⇒ 不建」"
                                          + "会把第 8 行起彻底藏掉）"
                                          + "；⚠️ 行距是 `RecyclableScrollRect._spacingY = 5`（**不是** `Invitations`"
                                          + " 那一列的 10 —— 2026-10-04 订正 ①）");
                                CheckTrue(os.MaxOffset > 1f, "★ 确实**滚得动**了");
                                int wantA = RowsInViewport(NA, VTop, VBot, pitchA, RowH0, 0f);
                                Check(CountChildren(oaList, "Entry"), wantA,
                                      $"★ 喂 {NA} 行 ⇒ **恰好建了与视口相交的那 {wantA} 行**（现算；"
                                      + $"视口高 {VBot - VTop:F2}、行距 {pitchA:F0}）—— 整行在视口外的连节点一起不建");
                                CheckTrue(wantA > 0 && wantA < NA,
                                          $"…而且**确实有整行落在视口外**（{NA - wantA} 行不建）—— 否则这一条等于没验");
                                float aTop0, aBot0, aTop1, aBot1;
                                CheckTrue(RowSpan(oaList, "Entry", RowH0, out aTop0, out aBot0),
                                          "建出来的行节点都在（量它们的位置，不量被裁过的 quad）");
                                CheckNear(aTop0, VTop, 0.5f, "最上面那行的顶边 = 视口顶 337.29（顺带证明偏移是 0）");
                                CheckTrue(aBot0 > VBot + 0.5f,
                                          $"…而最后一颗建出来的行**压在视口下沿上**（行底 {aBot0:F2} > 视口底 {VBot:F2}）"
                                          + "—— 这条是下面「没有 quad 画到框外」的**前提**（不满足说明这条测试自己失效了）");
                                CheckTrue(pl3 != null && pl3.ScrollUnder(1117.94f, 708.53f) == os,
                                          "视口中心（1117.94,708.53）上命中的滚动区**就是这一格**");
                                CheckTrue(pl3 != null && pl3.WheelAt(1117.94f, 708.53f, -120f),
                                          "滚轮落在这一格上（`PointerLayer.WheelAt` —— 与真鼠标同一条路）");
                                CheckTrue(os.Offset > 0f, $"往下滚一格 ⇒ 偏移往正走（现在 {os.Offset:F2}px）");
                                RowSpan(oaList, "Entry", RowH0, out aTop1, out aBot1);
                                CheckNear(aTop0 - aTop1, os.Offset, 0.5f,
                                          "★ **滚动之后行真的换了位置**：内容往上走的像素数 == 滚动偏移");
                                Check(CountChildren(oaList, "Entry"),
                                      RowsInViewport(NA, VTop, VBot, pitchA, RowH0, os.Offset),
                                      "滚一格之后在建的行数 == 现算值（重建是**先清再建**，不清会越滚越多）");
                                var badA = QuadsOutside(oaList, new PxRect(360.99f, VTop, 1874.90f, VBot), qTol);
                                CheckTrue(badA.Count == 0,
                                          "★ **没有一颗 quad 画到视口外**（量的是**渲出来那块**：越界 " + badA.Count + " 颗"
                                          + (badA.Count > 0 ? "：" + string.Join(" / ", badA.ToArray()) : "")
                                          + "）—— 拆掉 `SetClip` 这一条立刻红");
                                os.SetOffset(os.MaxOffset);
                                int wantA2 = RowsInViewport(NA, VTop, VBot, pitchA, RowH0, os.Offset);
                                Check(CountChildren(oaList, "Entry"), wantA2,
                                      $"滚到最下 ⇒ 仍然**恰好建了与视口相交的那几行**（现算 {wantA2} 行）");
                                CheckTrue(!RowTextsOf(oaList, "Entry", "Title").Contains("Open 1")
                                          && RowTextsOf(oaList, "Entry", "Title").Contains("Open " + NA),
                                          "★ 滚到最下**看得见最后一行了**（第 1 行滚出视口、第 " + NA + " 行进来；"
                                          + "建出来的：" + string.Join("/", RowTextsOf(oaList, "Entry", "Title").ToArray())
                                          + "）—— 改之前这一行**永远看不到也点不到**");
                                os.SetOffset(0f);
                                SocialData.OpenAlliances.Clear();
                                search.RebuildForTest();
                            }
                        }

                        // ------------------------------------------------ ② 好友页（`GridLayoutGroup` 网格）
                        {
                            const int NF2 = 20;
                            for (int i = 1; i <= NF2; i++)
                                SocialData.Friends.Add(new SocialData.Friend { Name = "Friend " + i, Online = i % 2 == 0 });
                            sw.tabButtons.Click(1);           // 切到好友页 = `ChangeTab` → `OnOpen` → `BuildRows`（真路）
                            var fpg = sw.PageFriends;
                            var fsc = fpg.ListScroll;
                            CheckTrue(fsc != null,
                                      "★ 好友页 `Friends Container` 那一格**有滚动区了**（原版 = `ScrollRect`；"
                                      + "改之前连 `MenuScroll` 都没有 —— 文件里当时还写着「没有 `_scroll`」）");
                            var fvNode = FindChild(FindChild(FindChild(t, "Content Area"), "Tabs"), "Friends Tab");
                            fvNode = FindChild(FindChild(FindChild(fvNode, "Friends List"), "Friends Container"), "Viewport");
                            var fContent = FindChild(fvNode, "Content");
                            CheckAtWorld(fvNode, 332.15f, 1875.80f, 314.80f, 1080.06f,
                                         "`Friends Container>Viewport`（原版是 `RectMask2D` 那个节点）");
                            CheckTrue(fsc != null && fContent != null, "滚动区与它下面的 `Content` 都在");
                            if (fsc != null && fContent != null)
                            {
                                const float VTop = 314.80f, VBot = 1080.06f;      // = 原版 `Viewport`（上面刚钉过）
                                // 原版 `GridLayoutGroup`（`Content` 上）：cell 721.3×84.82 · spacing (11.2,12.7)
                                // · pad **(左 16, 右 0, 上 20, 下 0)**（普查 `社交_联盟与好友页.md:576`「§B·12 三处
                                // `GridLayoutGroup`」那张表，表头写明是「左,右,上,下」）—— 与上面
                                // `CheckAtWorld(frow, 348.15f, 1069.45f, 334.80f, 419.62f)` 用的是同一组数
                                // （那条钉的是**行本身**；`348.15 = 332.15 + 16`、`334.80 = 314.80 + 20`
                                // 也反证了左 16 / 上 20）。
                                const float CW = 721.3f, CH = 84.82f, GX = 11.2f, GY = 12.7f,
                                            PL = 16f, PR = 0f, PT = 20f;
                                // 🔴 列数式照 UGUI 那一份**独立复算一遍**（`Library/PackageCache/
                                //   com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/GridLayoutGroup.cs:184`）：
                                //   分子吃的是 `padding.horizontal = 左 + 右 = 16`（**不是 `2×左 = 32`**）、
                                //   末尾那个 **`+ 0.001f`** 也不能少 —— 两条原来都写错（2026-10-04 审查查出 ⑤）。
                                //   ⚠️ 今天两种写法都得 2 列（所以一直没现形）；`W ∈ [1469.8, 1485.8)` 时分岔。
                                int colsF = Mathf.Max(1, Mathf.FloorToInt(
                                    ((1875.80f - 332.15f) - (PL + PR) + GX + 0.001f) / (CW + GX)));
                                int rowsF = Mathf.CeilToInt(NF2 / (float)colsF);
                                // 🆕 2026-10-04（A35⑤）：**列数式的可真红判据** —— 这一屏（W = 1543.65）
                                //   两种写法同得 2 列、分不出来 ⇒ 拿**别的宽度**去咬它
                                //   （`FriendsTab.ColumnsFor` 就是生产路径上那个纯函数）。
                                //   期望值 = 照原版 `GridLayoutGroup.cs:184`
                                //   `Max(1, Floor((w − (16+0) + 11.2 + 0.001) / (721.3 + 11.2)))` **手算**出来的，
                                //   ⛔ 不调实现的任何东西算期望：
                                //     · w = 1543.65（这一屏）→ `1538.851 / 732.5 = 2.1008` ⇒ **2**
                                //     · w = 1480 → `1475.201 / 732.5 = 2.0140` ⇒ **2**
                                //       ⚠️ 旧写法（分子吃 `2×padLeft = 32`）在这里是 `1459.2 / 732.5 = 1.9921`
                                //       ⇒ **1 列** —— 所以这一条**改回旧写法立刻红**（= ⑤ 的可真红面）
                                //     · w = 1475 → `1470.201 / 732.5 = 2.0071` ⇒ **2**
                                //       （旧写法若把 `spacing.x` 漏在分子外 ⇒ `1459 / 732.5 = 1.9918` ⇒ 1 列）
                                //     · w = 700 → `695.201 / 732.5 = 0.9491` ⇒ 夹到 **1**
                                //   ⚠️ `+0.001f` 那一位**没法用宽度表咬住**（它只在 `w` 落进 0.001 宽的整除窗口
                                //   时才起作用，浮点噪声同量级）⇒ 那一处只靠代码 + 注释，已记进报告。
                                Check(FriendsTab.ColumnsFor(1543.65f), 2, "列数（这一屏 W = 1543.65）");
                                Check(FriendsTab.ColumnsFor(1480f), 2,
                                      "★ 列数（W = 1480：原版 **2 列**；旧写法 `− 2×padLeft` 只给 1 列 ⇒ 能真红）");
                                Check(FriendsTab.ColumnsFor(1475f), 2,
                                      "★ 列数（W = 1475：`spacing.x` 必须**加在分子上**；漏了就只给 1 列）");
                                Check(FriendsTab.ColumnsFor(700f), 1, "列数下限夹到 1（窄到一行放不下两格）");
                                CheckTrue(fsc.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                                CheckTrue(fsc.Elastic, "**Elastic**（原版 `m_MovementType=1`）");
                                // 内容高 = UGUI `GridLayoutGroup.cs:188` 的 MinSize
                                // `padding.vertical + (cell.y + spacing.y) × 排数 − spacing.y`（同一份判据，独立算一遍）
                                // = 原版 `Content` 上那个 `ContentSizeFitter(m_VerticalFit = 1 MinSize)` 的高。
                                float contentF = PT + rowsF * CH + Mathf.Max(0, rowsF - 1) * GY;
                                CheckNear(fsc.MaxOffset, contentF - (VBot - VTop), 0.5f,
                                          $"可滚范围 = 内容高（pad 20 + {rowsF} 排×84.82 + {rowsF - 1}×12.7 = {contentF:F2}）"
                                          + $" − 视口高 {VBot - VTop:F2} —— **补之前这里恒 0**（滚轮全被夹回 0）");
                                CheckTrue(fsc.MaxOffset > 1f, "★ 确实**滚得动**（可滚范围 > 0）");
                                int wantF2 = CellsInViewport(NF2, colsF, PT, CH + GY, CH, VTop, VBot, 0f);
                                Check(fpg.BuiltRows, wantF2,
                                      $"★ 喂 {NF2} 条好友（{colsF} 列 ⇒ {rowsF} 排）⇒ **恰好建了与视口相交的"
                                      + $"那 {wantF2} 格**（现算；视口高 {VBot - VTop:F2}、排距 {CH + GY:F2}）");
                                Check(CountChildren(fContent, "Friend Info Item"), fpg.BuiltRows,
                                      "建出来的格节点个数 == `BuiltRows`（两者不许各说各的）");
                                CheckTrue(wantF2 > 0 && wantF2 < NF2,
                                          $"…而且**确实有格落在视口外**（{NF2 - wantF2} 格不建）");
                                float fTop0, fBot0, fTop1, fBot1;
                                CheckTrue(RowSpan(fContent, "Friend Info Item", CH, out fTop0, out fBot0),
                                          "建出来的好友格都在");
                                CheckTrue(fBot0 > VBot + 0.5f,
                                          $"…而最后一排**压在视口下沿上**（格底 {fBot0:F2} > 视口底 {VBot:F2}）"
                                          + "—— 下面「没有 quad 画到框外」的前提");
                                CheckTrue(pl3 != null && pl3.ScrollUnder(1103.98f, 697.43f) == fsc,
                                          "视口中心（1103.98,697.43）上命中的滚动区**就是这一格**");
                                CheckTrue(pl3 != null && pl3.WheelAt(1103.98f, 697.43f, -120f),
                                          "滚轮落在这一格上（`PointerLayer.WheelAt`）");
                                CheckTrue(fsc.Offset > 0f, $"往下滚一格 ⇒ 偏移往正走（现在 {fsc.Offset:F2}px）");
                                RowSpan(fContent, "Friend Info Item", CH, out fTop1, out fBot1);
                                CheckNear(fTop0 - fTop1, fsc.Offset, 0.5f,
                                          "★ **滚动之后格子真的换了位置**：内容往上走的像素数 == 滚动偏移");
                                Check(fpg.BuiltRows, CellsInViewport(NF2, colsF, PT, CH + GY, CH, VTop, VBot, fsc.Offset),
                                      "滚一格之后在建的格数 == 现算值");
                                var badF = QuadsOutside(fContent, new PxRect(332.15f, VTop, 1875.80f, VBot), qTol);
                                CheckTrue(badF.Count == 0,
                                          "★ **没有一颗 quad 画到视口外**（越界 " + badF.Count + " 颗"
                                          + (badF.Count > 0 ? "：" + string.Join(" / ", badF.ToArray()) : "") + "）");
                                fsc.SetOffset(0f);
                                SocialData.Friends.Clear();
                                fpg.RebuildForTest();
                                Check(fpg.BuiltRows, 0, "清空好友 ⇒ 格又没了（自检不留假数据）");
                            }
                        }

                        // ------------------------------------------------ ③ 已入盟支的成员列
                        //   ⚠️ 这一支本地**走不到**（原版按服务器的 `AlliancesManager` 二选一）⇒ 自检手动点亮，
                        //      并把**未入盟支关掉**：两个视口叠在一起会让 `ScrollUnder` 命中的不是这一格（假红）。
                        {
                            const int NM = 10;
                            for (int i = 1; i <= NM; i++)
                                SocialData.Members.Add(new SocialData.Member
                                { Index = i, Name = "Member " + i, Role = "Alliance Master", Online = i % 2 == 0,
                                  DraftRating = "", RankedRating = "" });
                            sw.tabButtons.Click(0);           // 先在联盟页（上面切到好友页了）
                            bool nmWas = nmv.gameObject.activeSelf;
                            nmv.gameObject.SetActive(false);
                            mv.gameObject.SetActive(true);
                            var mt2 = sw.PageAlliances.Member;
                            mt2.RebuildMembersForTest();      // = 原版 `AllianceMemberList` 清空重填那条路
                            var ms = mt2.MemberScroll;
                            CheckTrue(ms != null, "★ 成员列那一格**有滚动区了**（原版 `MemberList>Scroll View` 的 `ScrollRect`）");
                            var mVp = FindChild(FindChild(mv, "GeneralDetails"), "MemberList");
                            mVp = FindChild(FindChild(mVp, "Scroll View"), "Viewport");
                            var mContent = FindChild(mVp, "Content");
                            CheckAtWorld(mVp, 369.67f, 1880.67f, 493.63f, 1080.05f,
                                         "`MemberList>Scroll View>Viewport`（原版 `Image + Mask`，`showGraphic=0`）");
                            if (ms != null && mContent != null)
                            {
                                const float VTop = 493.63f, VBot = 1080.05f;      // = 原版 `Viewport`（上面刚钉过）
                                // 原版那个 `GridLayoutGroup`（原始 JSON 实读 `MonoBehaviour_6868526478606655651.json`，
                                // 挂在 `MemberList>Scroll View>Viewport>Content` 的 GO `6909745686555386019` 上）：
                                // cell 750×100 · spacing (10,**7.22**) · pad **(左0,右0,上9,下75)**。
                                const float CH2 = 100f, GY2 = 7.22f, PT2 = 9f, PB2 = 75f;
                                CheckNear(ms.Viewport.y1, VTop, 0.5f,
                                          "滚动区视口 = `Viewport` 那个节点自己的矩形（**没有另挑一个**）");
                                CheckTrue(ms.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                                CheckTrue(ms.Elastic, "**Elastic**（原版 `m_MovementType=1`）");
                                // 内容高 = UGUI `GridLayoutGroup.cs:188` 的 MinSize
                                // `padding.vertical + (cell.y + spacing.y) × 行数 − spacing.y`（**独立复算**）。
                                // ⚠️ 这里的「行数」取 **NM**（每行一条）= **本工程现在的排法** ——
                                // 原版那个 `GridLayoutGroup` 是 `m_Constraint = 0 (Flexible)` + `cellSize.x = 750`
                                // + `spacing.x = 10`，视口宽 1511 ⇒ `cellCountX = 2`（`GridLayoutGroup.cs:184`）
                                // **两列** ⇒ 原版行数 = `CeilToInt(NM / 2)`、内容高 `612.88`。
                                // 🔴 这条**列数偏离不在 A35 那 9 条里**（2026-10-04 顺带查出 —— 见报告）⇒
                                // 本断言暂时与「单列」这个现状对齐；真照原版改列数时，这一条要连它一起收
                                // （还有 `AllianceMemberRow.BuildAll` 的行位与它上面那条内容高算式）。
                                float contentM = PT2 + NM * CH2 + (NM - 1) * GY2 + PB2;
                                CheckNear(ms.MaxOffset, contentM - (VBot - VTop), 0.5f,
                                          $"可滚范围 = 内容高（9 + {NM}×100 + {NM - 1}×7.22 + 75 = {contentM:F2}）"
                                          + $"− 视口高 {VBot - VTop:F2} —— **补之前这里恒 0**");
                                CheckTrue(ms.MaxOffset > 1f, "★ 确实**滚得动**了");
                                int wantM = RowsInViewport(NM, VTop + PT2, VBot, CH2 + GY2, CH2, 0f);
                                Check(CountChildren(mContent, "Alliance Member Entry"), wantM,
                                      $"★ 喂 {NM} 个成员 ⇒ **恰好建了与视口相交的那 {wantM} 行**（现算）");
                                CheckTrue(wantM > 0 && wantM < NM,
                                          $"…而且**确实有整行落在视口外**（{NM - wantM} 行不建）");
                                float mTop0, mBot0, mTop1, mBot1;
                                CheckTrue(RowSpan(mContent, "Alliance Member Entry", CH2, out mTop0, out mBot0),
                                          "建出来的成员行都在");
                                CheckNear(mTop0, VTop + PT2, 0.5f, "最上面那行的顶边 = 视口顶 + padTop 9 = 502.63");
                                CheckTrue(mBot0 > VBot + 0.5f,
                                          $"…而最后一颗建出来的行**压在视口下沿上**（行底 {mBot0:F2} > 视口底 {VBot:F2}）");
                                CheckTrue(pl3 != null && pl3.ScrollUnder(1125.17f, 786.84f) == ms,
                                          "视口中心（1125.17,786.84）上命中的滚动区**就是这一格**");
                                CheckTrue(pl3 != null && pl3.WheelAt(1125.17f, 786.84f, -120f),
                                          "滚轮落在这一格上（`PointerLayer.WheelAt`）");
                                CheckTrue(ms.Offset > 0f, $"往下滚一格 ⇒ 偏移往正走（现在 {ms.Offset:F2}px）");
                                RowSpan(mContent, "Alliance Member Entry", CH2, out mTop1, out mBot1);
                                CheckNear(mTop0 - mTop1, ms.Offset, 0.5f,
                                          "★ **滚动之后行真的换了位置**：内容往上走的像素数 == 滚动偏移");
                                Check(CountChildren(mContent, "Alliance Member Entry"),
                                      RowsInViewport(NM, VTop + PT2, VBot, CH2 + GY2, CH2, ms.Offset),
                                      "滚一格之后在建的行数 == 现算值（先清再建）");
                                var badM = QuadsOutside(mContent, new PxRect(369.67f, VTop, 1880.67f, VBot), qTol);
                                CheckTrue(badM.Count == 0,
                                          "★ **没有一颗 quad 画到视口外**（越界 " + badM.Count + " 颗"
                                          + (badM.Count > 0 ? "：" + string.Join(" / ", badM.ToArray()) : "") + "）");
                                ms.SetOffset(ms.MaxOffset);
                                var mNames = RowTextsOf(mContent, "Alliance Member Entry", "member name");
                                CheckTrue(!mNames.Contains("Member 1") && mNames.Contains("Member " + NM),
                                          "★ 滚到最下建的是**另一批**行（第 1 个滚出视口、第 " + NM + " 个进来；建出来的："
                                          + string.Join("/", mNames.ToArray()) + "）");
                                ms.SetOffset(0f);
                                // 🆕 2026-10-04（A35④）：**空表那一支的内容高** —— 原版那个 `GridLayoutGroup` 的
                                //   MinSize 在 0 子节点时 = `padding.vertical − spacing.y = (9 + 75) − 7.22 = **76.78**`
                                //   （与原版那份 **0 子节点** `Content` 的序列化 `sizeDelta.y` 逐值相同，
                                //   普查 `社交_联盟与好友页.md:232`，RT `-1825538911737290589`）
                                //   ⇒ 可滚范围 = `76.78 − 视口高 586.42` = **负数**（`ClampHi` 夹到 0 = 滚不动）。
                                //   🔴 这条**能真红**：把空表那一支改回 `0f` ⇒ 这里少 76.78（远超 0.5 的容差）。
                                //   ⚠️ 今天**看不见**（本地成员表恒空、这一支也走不到）—— 所以这是一条**口径断言**。
                                SocialData.Members.Clear();
                                mt2.RebuildMembersForTest();
                                CheckNear(ms.MaxOffset, 76.78f - (VBot - VTop), 0.5f,
                                          "空表：内容高 = 原版 `GridLayoutGroup` 的 MinSize 值 **76.78**"
                                          + "（`(9+75) − 7.22`；与原版那份 0 子节点 `Content` 的序列化 "
                                          + "`sizeDelta.y` 相同）− 视口高 ⇒ 可滚范围是负的（夹到 0，滚不动）");
                            }
                            SocialData.Members.Clear();
                            mt2.RebuildMembersForTest();
                            nmv.gameObject.SetActive(nmWas);
                            mv.gameObject.SetActive(false);   // 恢复：这一支本地**走不到**（出厂 act F）
                        }
                        SocialData.ResetForTest();            // 上面三处喂的数据一律清掉（自检不留假数据）
                    }

                    Check(sw.MissingArt.Count, 0,
                          "社交窗**没有取不到的图**（取不到的件根本没画）");
                    CheckHoverSwap(sw.transform, "社交窗");
                }
            }
        }

        Section("聊天窗 `ChatPanel`（入口 = 主菜单右上 `ChatPreview` 那颗钮 —— **原来没接点击**）");
        {
            var cp = menu.Find("ChatPreview");
            var ch = cp != null ? FindChild(cp, "ChatHit") : null;
            var cwb = ch != null ? ch.GetComponent<WindowButton>() : null;
            CheckTrue(cwb != null && cwb.onClick != null, "`ChatPreview` 那颗钮**接了点击**");
            if (cwb != null && cwb.onClick != null)
            {
                cwb.onClick();
                var chatWin = ChatPanel.LastOpened;
                CheckTrue(chatWin != null && chatWin.CurrentState == WindowState.Open, "点它 ⇒ **真的开了聊天窗**");
                if (chatWin != null)
                {
                    Check(chatWin.type, WindowType.Popup, "`type` = **1 Popup**（⚠️ 与社交窗相反）");
                    Check(chatWin.placement, WindowsPlacement.Canvas, "`windowsPlacement` = **5 Canvas**");
                    CheckNear(chatWin.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = **1**");
                    var ct = chatWin.transform;
                    CheckAtWorld(FindChild(ct, "Holder"), -78.12f, 1893.88f, -4f, 1076f, "`Holder`");
                    CheckAtWorld(FindChild(ct, "CloseBackground"), -2056.50f, 3872.26f,
                                 -651.18f, 1723.18f, "`CloseBackground`（纯色 (0,0,0,0.518)，点外关闭）");
                    var chol = FindChild(ct, "Chat");
                    CheckAtWorld(chol, 563.88f, 1863.88f, 146f, 1076f, "`Chat`（1300×930）");
                    CheckAtWorld(FindChild(chol, "ChatBackground"), 563.88f, 1863.88f, 146f, 1076f,
                                 "`ChatBackground`（`Chat_background` 九宫 138,113,137,107）");
                    // 两个频道键：VLG align UpperRight + reverse ⇒ 165×157.684，顶 182.82，从上往下
                    for (int i = 0; i < ChatPanel.Channels.Length; i++)
                    {
                        float top = 182.82f + 157.684f * i;
                        CheckAtWorld(FindChild(FindChild(chol, "Tab Buttons"), "Orange Tab Toggle " + i),
                                     425.71f, 590.71f, top, top + 157.684f, $"频道键 {i}（{ChatPanel.Channels[i]}）");
                    }
                    // `Enter Text`：**运行期**矩形（序列化高是 0，真值 66.53 —— 判据 ④）
                    var ent = FindChild(chol, "Enter Text");
                    CheckAtWorld(ent, 613.88f, 1813.88f, 959.47f, 1026.00f, "`Enter Text`（**运行期** 66.53 高）");
                    CheckAtWorld(FindChild(ent, "Button"), 1750.38f, 1790.38f, 972.735f, 1012.735f, "发送钮（40×40）");
                    var po = FindChild(chol, "Player Options Panel");
                    CheckTrue(po != null && !po.gameObject.activeSelf, "`Player Options Panel` 出厂 **act F**");
                    var cb3 = FindChild(FindChild(chol, "Generic Close Button Orange"), "Hit");
                    CheckTrue(cb3 != null && cb3.GetComponent<WindowButton>() != null, "右上那颗圆钮**接了关闭**");

                    // ★ 挑战弹窗：从选项面板的 `Challenge` 钮开（原版 `ChallengeManager.OpenStartChallengeWindow`）
                    po.gameObject.SetActive(true);
                    var chal = FindChild(FindChild(FindChild(po, "Buttons"), "Challenge"), "Hit");
                    var chb = chal != null ? chal.GetComponent<WindowButton>() : null;
                    CheckTrue(chb != null && chb.onClick != null, "`Challenge` 键接了点击");
                    if (chb != null) chb.onClick();
                    var duel = DuelPopupWindow.LastOpened;
                    CheckTrue(duel != null && duel.CurrentState == WindowState.Open, "点 `Challenge` ⇒ **开好友挑战弹窗**");
                    if (duel != null)
                    {
                        Check(duel.type, WindowType.Popup, "`type` = **1 Popup**");
                        Check(duel.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                        CheckNear(duel.extraScaleSmallScreen, 1.15f, 1e-4f,
                                  "`extraScaleSmallScreen` = **1.15**（⚠️ 逐窗实测，不是 1.07/1.0）");
                        var dt = duel.transform;
                        var dwin = FindChild(dt, "Window");
                        CheckAtWorld(dwin, 535f, 1385f, 245f, 675f, "`Window`（850×430）");
                        CheckAtWorld(FindChild(dwin, "Generic Popup Background"), 535f, 1385f, 245f, 675f,
                                     "`Generic Popup Background`（`40k_popup` 九宫 169,160,169,160）");
                        // `MessageText`：全文 + `52.5` 号（`{0}` 已被对手名替换）
                        CheckText(TextOf(FindChild(dwin, "MessageText")),
                                  string.Format(DuelPopupWindow.MessageFormat, "Everrookie2"),
                                  "`MessageText`（**原档那句的全文**，`{0}` 换成了被挑战者）");
                        var dbtns = FindChild(dwin, "Buttons");
                        CheckAtWorld(FindChild(dbtns, "Button Skirmish"), 610f, 960f, 567f, 643f,
                                     "`Button Skirmish`（HLG 排出来的位：x 610..960）");
                        CheckAtWorld(FindChild(dbtns, "Button Classic"), 960f, 1310f, 567f, 643f,
                                     "`Button Classic`（x 960..1310 —— 两个钮以窗心 960 对称）");
                        CheckText(TextOf(FindChild(dbtns, "Button Classic")), "Continue",
                                  "⚠️ `Button Classic` 的文案是 **`Continue`**（节点名叫 `Classic` —— 原档如此）");
                        CheckAtWorld(FindChild(dwin, "Generic Rounded Button Green"), 1341.80f, 1416.80f,
                                     212.10f, 287.10f, "右上那颗 75×75 绿圆钮（**判为关闭钮**）");
                        duel.Close();
                    }
                    // 🔴 **消息行的五级阶梯**（2026-09-28 从原版**兄弟序**读出 —— 这条以前记的是「原版关服、
                    //    没有消息行可看 ⇒ 没尺子」，其实**尺子在 bundle 里**）：
                    //    · 原版 `ChatMessageRow` 的 `m_Children`（`bundle_mainmenualwaysloaded_assets_all/
                    //      RectTransform/RectTransform_7760131448890879999.json:22-39`）＝
                    //        `RowBackground`(0) → `Player Header`(1) → `Friend Header`(2) → `Message`(3)
                    //    · 头部内（`RectTransform_-6649793586198712321.json`）＝ `Sender`(0) → `Time`(1) → `Profile border`(2)，
                    //      而**框是 `Profile border` 自己身上的 Image**、立绘（`Profile content`）是它**唯一**的子节点
                    //    ⇒ 画序 = **行底 < 信使名/时间 < 框 < 立绘 < 正文**（头像压文字、正文压过头像）。
                    //    ⚠️ 本窗默认 **0 条消息**（数据留空态）⇒ 自检自己塞一条（同 `LeaderboardData.InjectForTest`），验完清掉。
                    int Q(Transform p, string n)
                    {
                        var t = FindChild(p, n);
                        if (t == null) return -1;
                        // ⚠️ 这条行里**两种渲染后端都有**：底/框/立绘是 `ImageQuad`，信使名/时间/正文是 `Label`
                        //    —— 第一版只读了 `ImageQuad` ⇒ 三个文字节点全读到 -1（自检当场抓出来）。
                        var lb = t.GetComponentInChildren<Label>();
                        if (lb != null) return lb.RenderQueue;
                        var iq = t.GetComponentInChildren<ImageQuad>();
                        return iq != null ? iq.RenderQueue : -1;
                    }
                    SocialData.ChatMessages.Add(new SocialData.ChatMessage
                    {
                        Channel = "Global", Sender = "LadderProbe", Time = "0d 0h",
                        Text = "queue ladder probe", Mine = false, Height = 0f,
                        // ⚠️ **必须给一张真头像**：`MenuDraw.Rect` 在贴图为 null 时**连节点都不建**
                        //    ⇒ 不给的话「立绘」那一层整条不存在，断言读到 -1（第一版就是这么红的）。
                        AvatarArt = ProfileData.AvatarArt,
                    });
                    chatWin.RefreshMessages();
                    var cRow = FindChild(chol, "ChatMessageRow");
                    CheckTrue(cRow != null, "塞一条消息 ⇒ **消息行建出来了**（`Chat Tab Global/Viewport/Content` 下）");
                    if (cRow != null)
                    {
                        var qRowBg = Q(cRow, "RowBackground");
                        var qSender = Q(FindChild(cRow, "Friend Header"), "Sender");
                        var qTime = Q(FindChild(cRow, "Friend Header"), "Time");
                        var qBorder = Q(FindChild(cRow, "Profile border"), "Border");
                        var qAvatar = Q(FindChild(cRow, "Profile border"), "Profile content");
                        var qMsg = Q(cRow, "Message");
                        CheckTrue(qRowBg >= 0 && qSender > qRowBg && qTime > qRowBg,
                                  $"行底 **{qRowBg}** < 信使名/时间 **{qSender}/{qTime}**（原版 `RowBackground` 是 sibling 0）");
                        CheckTrue(qBorder > qSender && qAvatar > qBorder,
                                  $"信使名/时间 **{qSender}** < 框 **{qBorder}** < 立绘 **{qAvatar}**"
                                  + "（头像压文字：原版 `Profile border` 是头部第 3 个兄弟，框在立绘之前）");
                        CheckTrue(qMsg > qAvatar,
                                  $"正文 **{qMsg}** > 头像 **{qAvatar}**（原版 `Message` 是行里最后一个兄弟 ⇒ **正文压过头像**）");
                    }
                    SocialData.ChatMessages.Clear();
                    chatWin.RefreshMessages();
                    chatWin.Close();
                }
            }
        }

        Section("对局历史弹窗 `Battle Log Popup`（🔴 **界面里没有入口** —— 原版的打开点查不到，见下）");
        {
            // 🔴 这一扇**不是**从哪个钮点开的：普查 §E 把它只在 `WindowsManager` 预载表里这件事查实了，
            //    而那个 `OpenWindow<BattleLogPopup>()` 的产物缺失 ⇒ 我们**不编入口**，自检直接开。
            var blp = menu.OpenBattleLogPopup();
            CheckTrue(blp != null && blp.CurrentState == WindowState.Open, "开得起来");
            if (blp != null)
            {
                Check(blp.type, WindowType.Popup, "`type` = **1 Popup**");
                Check(blp.placement, WindowsPlacement.Popup, "`windowsPlacement` = **15 Popup**");
                CheckTrue(blp.closeOnEsc, "`closeOnESC` = **1**");
                CheckNear(blp.extraScaleSmallScreen, 1f, 1e-4f, "`extraScaleSmallScreen` = **1.0**");
                var bt2 = blp.transform;
                CheckAtWorld(FindChild(bt2, "Menu Dark Background"), -1327.30f, 3247.30f, -746.18f, 1826.18f,
                             "压暗层（纯色 (0,0,0,0.773)）");
                var bc = FindChild(bt2, "Content");
                CheckAtWorld(bc, 135f, 1785f, 55f, 1055f, "`Content`（1650×1000）");
                CheckAtWorld(FindChild(bc, "Background"), 135f, 1785f, 55f, 1055f,
                             "`Background`（`UI_Deck_Information_Back` 九宫 42,363,655,81）");
                var cb4 = FindChild(bc, "Close Button");
                CheckAtWorld(cb4, 1685f, 1815f, 25f, 155f, "`Close Button`（**130×130** —— 比常规那颗大）");
                CheckAtWorld(FindChild(cb4, "Background"), 1699.49f, 1798.59f, 39.06f, 138.74f,
                             "可见圆 `Background`（⚠️ **略偏左上**：左缝 14.49 / 右缝 16.41 —— 原版如此）");
                CheckTrue(FindChild(cb4, "Icon") != null, "`Icon`（`40k_general_bt_yellow_close`）");
                CheckAtWorld(FindChild(bc, "Matches"), 235f, 1710f, 130f, 980f, "`Matches`（`ScrollRect`）");
                CheckAtWorld(FindChild(FindChild(bc, "Matches"), "Viewport"), 235f, 1710f, 130f, 963f,
                             "`Viewport`（`UIMask` + `showGraphic=0`）");
                // 空态：本地 0 条 ⇒ 一行都不建（原版这扇窗也没有空态节点）
                Check(blp.BuiltRows, 0, "本地 **0 条** ⇒ 一行都不建");
                // 喂一条 ⇒ 建出一行，**几何与档案窗那一页同一份 `MatchLogRow`**
                BattleLogData.Add(new BattleLogData.Match
                {
                    Result = BattleLogData.Outcome.Victory, OwnHeroName = "Uriel Ventris",
                    EnemyHeroName = "Ghazghkull Thraka", OwnName = "Test Commander", EnemyName = "Bot",
                    OwnSkulls = 3, EnemySkulls = 1, OwnScore = "987 (+12)", EnemyScore = "Gold IV",
                    Mode = "Skirmish mode",
                });
                blp.RebuildForTest();
                Check(blp.BuiltRows, 1, "喂一条 ⇒ **建出一行**");
                var bvp = FindChild(FindChild(bc, "Matches"), "Viewport");
                var brow = FindChild(FindChild(bvp, "Content"), "Match Log");
                CheckAtWorld(brow, 235f, 1710f, 130f, 333.20f,
                             "行矩形（行高 **203.20**、从视口顶边起 —— 与档案窗那一页逐值相同）");
                CheckText(TextOf(FindChild(brow, "Result")),
                          BattleLogData.ResultText(BattleLogData.Outcome.Victory), "行里那行结果");
                BattleLogData.ResetForTest();
                blp.RebuildForTest();
                Check(blp.BuiltRows, 0, "清空 ⇒ 行又没了");

                // ---- 🆕 2026-10-03（本件 ②）：**视口外的整行不建** ----
                // 这一棵树上**原来没有**这道守卫（档案窗那一页有）⇒ 整行在视口外的那些照样被逐件建出来，
                // 每个 quad 各自靠 `RowCtx.Clip` 截 ⇒ 白建几十个节点，而且它们本来就画不到（原版 `RectMask2D`）。
                // 🔴 期望值**现算**（`RowsInViewport`）：视口 = `ViewportR` 130..963（上面 `CheckAtWorld` 刚按原版值钉过）、
                //    行距 = 原版 203.20 + 25。⛔ 不写死条数。
                {
                    const int NF = 6;
                    for (int i = 0; i < NF; i++)
                        BattleLogData.Add(new BattleLogData.Match
                        {
                            Result = BattleLogData.Outcome.Victory,
                            OwnHeroName = "Feed " + i, EnemyHeroName = "Bot",
                            OwnName = "Feed " + i, EnemyName = "Bot",
                            OwnSkulls = 1, EnemySkulls = 0, OwnScore = "1", EnemyScore = "2",
                            Mode = "Skirmish mode",
                        });
                    blp.RebuildForTest();
                    const float FTop = 130f, FBot = 963f;      // = `ViewportR`（原版值）
                    float fpitch = OrigMatchRowH + OrigMatchRowGap;    // 原版：203.20 + 25
                    int wantF = RowsInViewport(NF, FTop, FBot, fpitch, OrigMatchRowH, 0f);
                    Check(blp.BuiltRows, wantF, $"喂 {NF} 行 ⇒ 建了与视口相交的那 {wantF} 行（现算）");
                    CheckTrue(wantF > 0 && wantF < NF,
                              $"…而且**确实有整行落在视口外**（{NF - wantF} 行连节点一起不建 —— "
                              + "原来它们是「逐件建出来、再被自己的 `Clip` 截掉」，白建还得画到框外）");
                    var fc = FindChild(bvp, "Content");
                    int fn = 0, fout = 0; float fmin = float.MaxValue;
                    if (fc != null)
                        foreach (var rt in fc.GetComponentsInChildren<Transform>(true))
                        {
                            if (rt.name != "Match Log") continue;
                            fn++;
                            float cy = LayoutSpace.PxY(rt.position.y);
                            float y1 = cy - OrigMatchRowH * 0.5f, y2 = cy + OrigMatchRowH * 0.5f;
                            fmin = Mathf.Min(fmin, y1);
                            if (y2 <= FTop + 0.01f || y1 >= FBot - 0.01f) fout++;
                        }
                    Check(fn, blp.BuiltRows, "行节点个数 == `BuiltRows`（两者不许各说各的）");
                    CheckTrue(fout == 0, $"每一颗建出来的行都与视口相交（越界 {fout} 颗）");
                    CheckNear(fmin, FTop, 0.5f, "最上面那颗行的顶边 = 视口顶 130（顺带证明偏移是 0）");

                    // ---- 🆕 2026-10-03（A25②）：**这一格补上了滚动区**（改之前连 `MenuScroll` 都没有）----
                    // 判据（原版）：`Matches` 上是 `ScrollRect` `h=0 v=1` · **`m_MovementType=2`(Clamped)** ·
                    //   灵敏度 1.0（普查 §B 表 · §D6 的对照表）；内层 `Content` 挂 `ContentSizeFitter
                    //   m_VerticalFit=1` ⇒ **可滚范围 = 内容高 − 视口高**。
                    // 🔴 **改之前**：一处滚动都没有 ⇒ 滚不动，而「整行在视口外 ⇒ 不建」那道守卫又把后面的行
                    //   **彻底藏掉** ⇒ **第 5 行起永远看不到也点不到**。下面三条就是它的验收。
                    {
                        var rs = blp.RowsScroll;
                        CheckTrue(rs != null, "★ 这一格**有滚动区了**（原版 = `ScrollRect`；改之前一处都没有）");
                        // 🆕 2026-10-04（A35⑦⑧）：**宿主必须设** —— 走 `SocialPage.RegisterScroll` 那一份登记
                        //   （原来这里是全批唯一一处直调 `PointerLayer.RegisterScroll`：那条路在指针层缺席时
                        //   `return` 得一声不响），而 `Owner` 空着会被 `PointerLayer.PruneScrolls` 当**死条目**
                        //   删掉（`PointerLayer.cs:218-227`）⇒ 滚轮永远落不上、画面却正常。这条钉住这一对不变量。
                        CheckTrue(rs != null && rs.Owner != null,
                                  "滚动区带宿主（`Owner` 为空 ⇒ 下一次 `PruneScrolls` 就把它删了，且原来不出声）");
                        CheckTrue(rs != null && rs.Vertical, "滚的是**纵轴**（原版 `m_Horizontal=0 / m_Vertical=1`）");
                        CheckTrue(rs != null && !rs.Elastic,
                                  "**Clamped**（原版 `m_MovementType=2`）—— ⚠️ 别套档案窗那一页的 `1(Elastic)`（判据 ④）");
                        if (rs != null)
                        {
                            // 🔴 **2026-10-04（A35②）**：同上 —— 原来算的是 `NF * MatchLogRow.RowH + …`（同式自证），
                            //   现在改成从原版值重抄的 `OrigMatchRowH / OrigMatchRowGap`。
                            float contentH = NF * OrigMatchRowH + (NF - 1) * OrigMatchRowGap;
                            CheckNear(rs.MaxOffset, contentH - (FBot - FTop), 0.5f,
                                      $"可滚范围 = 内容高（{NF}×203.20 + {NF - 1}×25 = {contentH:F2}）− 视口高 {FBot - FTop:F2}"
                                      + " —— **补之前这里恒 0**（滚轮/拖拽全被夹回 0）");
                            CheckTrue(rs.MaxOffset > 1f, "★ 确实**滚得动**了");

                            // ① 滚一格走**真路**：指针层命中这一格 → `MenuScroll.Wheel` → `OnChanged` 重建
                            float topBefore = TopRowTop(fc);
                            var pl2 = PointerLayer.Instance;
                            // 先钉「视口中心命中的就是这一格」—— 免得滚轮落到别的窗口的滚动区上、把下面几条判成假红
                            CheckTrue(pl2 != null && pl2.ScrollUnder(972.5f, 546.5f) == rs,
                                      "视口中心（972.5,546.5）上命中的滚动区**就是这一格**");
                            bool wHit = pl2 != null && pl2.WheelAt(972.5f, 546.5f, -120f);   // 视口中心 = 235..1710 × 130..963
                            CheckTrue(wHit, "滚轮落在这一格上（`PointerLayer.WheelAt`）");
                            CheckTrue(rs.Offset > 0f, $"往下滚一格 ⇒ 偏移往正走（现在 {rs.Offset:F2}px）");
                            CheckNear(topBefore - TopRowTop(fc), rs.Offset, 0.5f,
                                      "★ **滚动之后行真的换了位置**：内容往上走的像素数 == 滚动偏移"
                                      + "（⚠️ 只看容器不够 —— 「底板走了、行没走」那一类只有这条抓得到）");
                            Check(blp.BuiltRows, RowsInViewport(NF, FTop, FBot, fpitch, OrigMatchRowH, rs.Offset),
                                  "滚一格之后在建的行数 == 现算值（重建是**先清再建**，不清会越滚越多）");
                            rs.SetOffset(0f);
                            CheckNear(TopRowTop(fc), FTop, 0.5f, "滚回 0 ⇒ 行回到视口顶（下面的期望值按这个前提算）");

                            // ② 滚到最下 ⇒ 建的是**另一批**行（原来最后那几行根本够不到）
                            rs.SetOffset(rs.MaxOffset);
                            CheckNear(rs.Offset, rs.MaxOffset, 0.01f, "滚到了最下（`SetOffset` 没被夹回去）");
                            int wantBot = RowsInViewport(NF, FTop, FBot, fpitch, OrigMatchRowH, rs.Offset);
                            Check(blp.BuiltRows, wantBot,
                                  $"滚到最下 ⇒ 仍然**恰好建了与视口相交的那几行**（现算 {wantBot} 行，偏移 {rs.Offset:F2}）");
                            var feed = new List<string>();
                            Transform lastRow = null;
                            foreach (var rt in fc.GetComponentsInChildren<Transform>(true))
                            {
                                if (rt.name != "Match Log") continue;
                                var hn = FindChild(FindChild(rt, "Player Info"), "Hero Name");
                                var hl = hn != null ? hn.GetComponentInChildren<Label>() : null;
                                if (hl == null) continue;
                                feed.Add(hl.Text);
                                if (hl.Text == "Feed " + (NF - 1)) lastRow = rt;
                            }
                            CheckTrue(feed.Count == blp.BuiltRows, "滚完之后行节点个数**仍然** == `BuiltRows`");
                            CheckTrue(!feed.Contains("Feed 0") && feed.Contains("Feed " + (NF - 1)),
                                      $"★ 滚到最下**看得见最后一行了**（第 1 行滚出视口、第 {NF} 行进来；建出来的："
                                      + string.Join("/", feed.ToArray()) + "）—— 改之前这一行**永远看不到也点不到**");
                            // 「点得到」那一半：那一行的 `ReplayButton/Hit` 真的在（整行在视口外的连 `Hit` 都不建）
                            var lastHit = lastRow != null ? FindChild(FindChild(lastRow, "ReplayButton"), "Hit") : null;
                            var lastBtn = lastHit != null ? lastHit.GetComponent<WindowButton>() : null;
                            CheckTrue(lastBtn != null && lastBtn.onClick != null,
                                      "★ 最后那一行的 `ReplayButton` **命中区也在**（原来它连节点都建不出来 ⇒ 点不到）");
                            rs.SetOffset(0f);
                        }
                    }

                    BattleLogData.ResetForTest();
                    blp.RebuildForTest();
                    Check(blp.BuiltRows, 0, "清空 ⇒ 行又没了（自检不留假数据）");
                }
                Check(blp.MissingArt.Count, 0, "弹窗**没有取不到的图**");
                CheckHoverSwap(blp.transform, "战斗日志弹窗");
                blp.Close();
            }
        }

        Section("图：一张都不能少");
        Check(menu.MissingArt.Count, 0, "没有取不到的图（取不到的件**根本没画**，所以这条必须 0）");
        // 🆕 A17：主菜单自己的那些键原版**全是 ColorTint**（普查 §块 1：5 个导航键/齿轮/收件箱/头像/ChatPreview 都 `trans=1`）
        // ⇒ 本窗**不该**出现换图按钮；这一条盯的是「别把色偏档也接上换图」+ 全局「悬停图一张都不缺」。
        CheckTrue(WindowButton.MissingSwapArt.Count == 0, "悬停图一张都不缺（缺的：" +
                  string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");
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

    // ================================================================ 断言用的**原版行几何**（判据源，独立于实现）
    //
    // 🔴 **2026-10-04（A35②）：断言里⛔ 不许再引用实现常量**（`MatchLogRow.RowH` / `LeaderboardRow.RowH` …
    //    —— 原来这几条就是这么写的）。那是**同式自证**：等号两边是**同一个表达式**，把实现常量改错也不会红
    //    （A35① 的 `Open Alliances` 行距就是这么漏过去的：断言拿 `110 + 10` 当「原版值」，而那个 `10` 正是
    //    实现里照 `Invitations` 抄错的那一个）。
    //    ✅ 做法 = **把原版值在断言处重抄一遍并注明出处**，与实现常量的对应靠**对账**、不靠共用
    //    （铁律：断言要盯「原版参数」，不是盯我们自己的常量）。
    /// <summary>对局历史那一族的行几何（实读原始 JSON）：
    /// 行 `Match Log` 的 `sizeDelta.y = 203.20`（普查 `资料/普查产出_0927/对局历史_行模板与弹窗.md:167`）·
    /// 内层 `Content` 那个 `VerticalLayoutGroup` 的 `spacing = 25.0`（同份 `:166`）；
    /// 内容高算式同份 `:197`（`N × 203.2 + (N−1) × 25`）。</summary>
    const float OrigMatchRowH = 203.20f, OrigMatchRowGap = 25f;
    /// <summary>排行榜那一族的行几何（实读原始 JSON）：
    /// 行 `PlayerRankingRow` 的 `sizeDelta.y = 100`（普查 `资料/普查产出_0927/排行榜_经典.md:68`）·
    /// 内层 `Content` 那个 `VerticalLayoutGroup` 的 `spacing = 15.0`（同份 `:67`；
    /// 嵌入版同值，见 `排行榜_嵌入版与行族.md:40`）。</summary>
    const float OrigLbRowH = 100f, OrigLbRowGap = 15f;

    /// <summary>🆕 2026-10-03：**现算**「按原版行距排下去，有几行的矩形与视口相交」——
    /// 这就是「滚出视口的整行不建」那几条断言的期望值。⛔ **别写死条数**（写死 = 拿我们的常量断言我们的常量）。
    /// 判据 = 原版 `RectMask2D` 的可见性语义（与 `MenuScroll.Intersects` 同一条式子，这里**独立算一遍**
    /// —— 不拿它自己算出来的结果当期望）。
    /// <para>`offset` = 滚动偏移（画布像素；正 = 内容上移 = 看到下面那些行）。
    /// `pitch` / `rowH` 一律传**原版参数**（榜单 100+15 · 日志行 203.20+25），不是我们的实现常量。</para></summary>
    static int RowsInViewport(int n, float vpTop, float vpBot, float pitch, float rowH, float offset)
    {
        int c = 0;
        for (int i = 0; i < n; i++)
        {
            float y1 = vpTop + i * pitch - offset, y2 = y1 + rowH;
            if (y2 > vpTop + 0.01f && y1 < vpBot - 0.01f) c++;
        }
        return c;
    }

    /// <summary>🆕 2026-10-03（A25④）：**网格**布局里「与视口相交」的格数（现算）—— 给好友页那种
    /// `GridLayoutGroup` 用（`RowsInViewport` 只管单列）。格 i 的顶边 = `vpTop + padTop + (i/cols)·pitch`。
    /// 判据与 `RowsInViewport` **同一条**（= `MenuDraw.ClipRect` 的交集判法），只是多一层 grid 分组。</summary>
    static int CellsInViewport(int n, int cols, float padTop, float pitch, float cellH,
                               float vpTop, float vpBot, float offset)
    {
        int c = 0;
        for (int i = 0; i < n; i++)
        {
            float y1 = vpTop + padTop + (i / cols) * pitch - offset, y2 = y1 + cellH;
            if (y2 > vpTop + 0.01f && y1 < vpBot - 0.01f) c++;
        }
        return c;
    }

    /// <summary>🆕 2026-10-03（A25④）：数一个节点下**直接子节点**里叫 `name` 的有几个
    /// （= 「这一屏真建出来几行」，与实现里的 `BuiltRows` 对账）。
    /// ⚠️ 只数**直接子节点** —— 行里面也有同名件（例如成员行里还有 `background`）时会数重。</summary>
    static int CountChildren(Transform parent, string name)
    {
        int c = 0;
        if (parent == null) return c;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) c++;
        return c;
    }

    /// <summary>🆕 2026-10-03（A25④）：把一棵子树里指定行节点的某段文字收集起来（按树的顺序）——
    /// 「滚动之后建的是不是**另一批**行」那几条靠它（比行数更能说明问题：第 1 行该滚出去、最后一行该进来）。</summary>
    static List<string> RowTextsOf(Transform content, string rowName, string textNode)
    {
        var list = new List<string>();
        if (content == null) return list;
        foreach (var rt in content.GetComponentsInChildren<Transform>(true))
        {
            if (rt.name != rowName) continue;
            var tn = FindChild(rt, textNode);
            var lb = tn != null ? tn.GetComponentInChildren<Label>() : null;
            if (lb != null) list.Add(lb.Text);
        }
        return list;
    }

    /// <summary>🆕 2026-10-03（A25④）：量子树里**建出来的行**的总跨度（最上沿 / 最下沿，画布像素 y）。
    /// `rowName` = 行节点的名字，`rowH` = 原版行高。
    /// ⚠️ 只读**行节点自己**的位置（`MenuDraw.Node` 按矩形中心摆）：**被裁过的 quad 位置会动**
    /// （`ClipNineChildren` 把它挪到截后那块的中心），拿它量位移会飘 —— 同 `TopRowTop` 那条注释
    /// （`资料/已知的坑.md` 2026-10-03「裁剪会移动 quad 的节点」）。没有行时返回 false。</summary>
    static bool RowSpan(Transform content, string rowName, float rowH, out float top, out float bot)
    {
        top = float.MaxValue; bot = float.MinValue;
        if (content == null) return false;
        foreach (var rt in content.GetComponentsInChildren<Transform>(true))
        {
            if (rt.name != rowName) continue;
            float cy = LayoutSpace.PxY(rt.position.y);
            top = Mathf.Min(top, cy - rowH * 0.5f);
            bot = Mathf.Max(bot, cy + rowH * 0.5f);
        }
        return top <= bot;
    }

    /// <summary>🆕 2026-10-03（A25④）：量子树里**每一颗 `ImageQuad` 渲出来的**那块，返回越出 `vp` 的那些
    /// （`名字 x1,y1..x2,y2`）—— 「越界内容不再画到框外」那几条断言的判据。
    /// 🔴 量的是**渲出来那块**（`q.WorldW/H` + **quad 自己**的位置），不是承载它的节点：
    /// 裁剪会把 quad 挪走、节点不动（`HitQuadRect` 那条注释里的坑，2026-10-03 踩过）。
    /// 🔴 判据 = 原版 `RectMask2D` 只裁**渲染**；**未激活的**（被 `ClipNineChildren` 判为整块在框外的）
    /// 不算 —— 它压根不画。
    /// ⚠️ 文字（`Label`）不是 `ImageQuad` ⇒ 不在量程内：**文字仍是「整块在框外才不建」**
    /// （那条缺口记在 `MenuWindowBase.Clip` 的注释 / `项目任务.md` §三 第 29 条 A9，本批没动它）。</summary>
    static List<string> QuadsOutside(Transform root, PxRect vp, float tol)
    {
        var bad = new List<string>();
        if (root == null) return bad;
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
        {
            if (q == null || !q.gameObject.activeInHierarchy) continue;
            float w = q.WorldW * 108f, h = q.WorldH * 108f;
            float cx = LayoutSpace.PxX(q.transform.position.x), cy = LayoutSpace.PxY(q.transform.position.y);
            float x1 = cx - w * 0.5f, x2 = cx + w * 0.5f, y1 = cy - h * 0.5f, y2 = cy + h * 0.5f;
            if (x1 < vp.x1 - tol || x2 > vp.x2 + tol || y1 < vp.y1 - tol || y2 > vp.y2 + tol)
                bad.Add(q.name + " " + x1.ToString("F1") + "," + y1.ToString("F1")
                        + ".." + x2.ToString("F1") + "," + y2.ToString("F1"));
        }
        return bad;
    }

    /// <summary>🆕 2026-10-03：量**最上面那颗建出来的行**的顶边（画布像素 y）—— 给「滚动之后内容真的往上走了」
    /// 那两条用。⚠️ 只读**行节点自己**的位置（`RenderedRect` 那一类量的是被裁过的 quad，拿它比位移会飘
    /// —— 见 `资料/已知的坑.md` 2026-10-03「裁剪会移动 quad 的节点」那条）。
    /// 没有行时返回 `float.MaxValue`（调用方自己判）。</summary>
    static float TopRowTop(Transform content)
    {
        float top = float.MaxValue;
        if (content == null) return top;
        foreach (var rt in content.GetComponentsInChildren<Transform>(true))
            if (rt.name == "Match Log")
                top = Mathf.Min(top, LayoutSpace.PxY(rt.position.y) - OrigMatchRowH * 0.5f);
        return top;
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
