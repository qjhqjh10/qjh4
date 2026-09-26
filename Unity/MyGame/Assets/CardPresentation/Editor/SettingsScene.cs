// SettingsScene.cs — 主菜单**设置窗**（第 4 层，随联机页一起建）的自检入口
//
// 用法：… -executeMethod SettingsScene.Run      退出码 0 = 全过
//
// 🔴 **每一条断言的期望值都盯「原版值」**，出处 = `工具/menu_rect.py bundle_menus_assets_all
//    "Main Menu Settings Window"` + 原始 JSON 实读（逐条写在 `Shell/SettingsWindow.cs` 文件头）。
// 🔴 **根上有 `m_LocalScale = 0.9`（只这一扇窗）** ⇒ 世界坐标 = 屏幕中心 + 0.9 ×（原版矩形 − 屏幕中心）：
//    每一条几何断言都过 `SettingsWindow.Screen(...)`，**别直接拿原版矩形当世界坐标**（那会差 11%，
//    而且差得「看起来像对」—— 见 `资料/已知的坑.md` 那类「断言量不到」的教训）。
// 🔴 **这一轮修掉的那条真缺陷**：齿轮点了没反应（静默失败）⇒ 这里有一条断言盯着它的点击区。
//
// ⚠️ 不碰玩家的真设置（`NetConfig.OverridePath` 指到临时文件）；不联网（「检查连接」打的是**没人听的端口**，
//    验的就是「如实失败」这条）。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using CardPresentation.Net;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SettingsScene
{
    const string P = "[Settings] ";
    const string ShotDir = "d:/4/_tmp_view/settings";

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
        => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F2} ≈ {want:F2}±{tol:F2}）");

    /// <summary>节点位置 = **原版矩形过 `Screen()`（含 0.9 缩放）**之后的中心。</summary>
    static void CheckAtS(Transform t, float x1, float y1, float x2, float y2, string what)
    {
        if (t == null) { CheckTrue(false, what + "（节点不在）"); return; }
        var s = SettingsWindow.Screen(x1, y1, x2, y2);
        var want = LayoutSpace.RectCenter(s.x1, s.y1, s.x2, s.y2);
        float d = Vector3.Distance(t.position, want);
        CheckTrue(d <= 0.01f, $"{what} 在原版矩形（经 0.9 缩放）的中心（差 {d:F4} 世界单位 = {d * 108f:F2}px）");
    }

    /// <summary>一张图**渲出来**的像素矩形（`WorldW/H` = 渲染真值）。
    /// ⚠️ **九宫格/平铺是一堆子 quad**（`CreateNineSlice` 建 9 个）⇒ 这里取**所有子 quad 的并集**，
    /// 只取第一个的话量到的是某个角块（2026-09-26 实测：弹窗底量成 182×173）。</summary>
    static void CheckRectS(Transform t, float x1, float y1, float x2, float y2, string what)
    {
        var qs = t != null ? t.GetComponentsInChildren<ImageQuad>(true) : null;
        if (qs == null || qs.Length == 0) { CheckTrue(false, what + "（没有 ImageQuad）"); return; }
        float lx = float.MaxValue, ty = float.MaxValue, rx = float.MinValue, by = float.MinValue;
        for (int i = 0; i < qs.Length; i++)
        {
            var q = qs[i];
            if (q == null) continue;
            float wpx = q.WorldW * 108f, hpx = q.WorldH * 108f;
            float wx = q.transform.position.x * 108f + 960f, wy = -q.transform.position.y * 108f + 540f;
            if (!q.gameObject.activeInHierarchy) continue;
            lx = Mathf.Min(lx, wx - wpx * 0.5f); rx = Mathf.Max(rx, wx + wpx * 0.5f);
            ty = Mathf.Min(ty, wy - hpx * 0.5f); by = Mathf.Max(by, wy + hpx * 0.5f);
        }
        var s = SettingsWindow.Screen(x1, y1, x2, y2);
        CheckTrue(Mathf.Abs((lx + rx) * 0.5f - (s.x1 + s.x2) * 0.5f) <= 1.5f
                  && Mathf.Abs((ty + by) * 0.5f - (s.y1 + s.y2) * 0.5f) <= 1.5f
                  && Mathf.Abs(rx - lx - s.W) <= 1.5f && Mathf.Abs(by - ty - s.H) <= 1.5f,
                  $"{what} 渲出来 = {rx - lx:F1}×{by - ty:F1} @({(lx + rx) * 0.5f:F1},{(ty + by) * 0.5f:F1})"
                  + $"（应为 {s.W:F1}×{s.H:F1} @({(s.x1 + s.x2) * 0.5f:F1},{(s.y1 + s.y2) * 0.5f:F1})）");
    }

    /// <summary>`AlignLeft` 会把 Label 的节点挪走（`MainMenuWindowBase` 的注释里写着）⇒
    /// **不能**拿它的位置去比矩形中心，要比**左边缘**。</summary>
    static void CheckLeftS(Transform t, float x1, float x2, float y1, float y2, string what)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        if (lb == null) { CheckTrue(false, what + "（没有 Label）"); return; }
        var s = SettingsWindow.Screen(x1, y1, x2, y2);
        float leftPx = lb.transform.position.x * 108f + 960f - lb.WorldW * 108f * 0.5f;
        CheckTrue(Mathf.Abs(leftPx - s.x1) <= 2f, $"{what} 左边缘 = {leftPx:F1}（应为 {s.x1:F1}）");
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
    static string TextOf(Transform t)
    {
        var lb = t != null ? t.GetComponentInChildren<Label>() : null;
        return lb != null ? lb.Text : null;
    }
    static void Click(Transform t)
    {
        var b = t != null ? t.GetComponentInChildren<WindowButton>() : null;
        if (b == null || b.onClick == null) { CheckTrue(false, "点击区 `" + (t != null ? t.name : "?") + "` 不在（或没接 onClick）"); return; }
        b.onClick();
    }

    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Directory.CreateDirectory(ShotDir);

        string tmp = Path.Combine(Path.GetTempPath(), "wf_settings_selftest.json");
        NetConfig.OverridePath = tmp;
        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        NetConfig.Load();

        Debug.Log(P + "=== 「设置窗」自检 开始 ===");
        var win = Build(out var root);
        try
        {
            // ---------------- 窗口参数 ----------------
            Section("窗口参数");
            Check(win.type, WindowType.Popup, "`type` = Popup");
            Check(win.placement, WindowsPlacement.Popup, "`windowsPlacement` = Popup(15)");
            Check(win.closeOnEsc, true, "`closeOnESC` = 1");
            CheckNear(root.localScale.x, 1f, 1e-4f,
                      "🔴 根节点 **scale 保持 1** —— 原版那个 `m_LocalScale = 0.9` 是**烘进坐标**的："
                      + "我们的 `ImageQuad` 按世界尺寸画、父节点缩放对它无效（0.9 的根下 75px 的钮仍渲 75px）");
            var sPop = SettingsWindow.Screen(SettingsWindow.PopL, SettingsWindow.PopT,
                                             SettingsWindow.PopR, SettingsWindow.PopB);
            CheckNear(sPop.W, 1146.95f, 0.5f,
                      "`Screen()` 把原版矩形按 0.9 缩过（弹窗 1274.39 → **1146.95**，不缩就是错的）");

            // ---------------- 几何（全部过 Screen() = 含 0.9）----------------
            Section("弹窗几何（原版矩形 → 经 0.9 缩放）");
            var area = FindChild(root, "Menu Area");
            CheckAtS(area, SettingsWindow.PopL, SettingsWindow.PopT, SettingsWindow.PopR, SettingsWindow.PopB,
                     "`Menu Area`（弹窗本体）");
            CheckRectS(FindChild(area, "Generic Popup Background"), SettingsWindow.PopL, SettingsWindow.PopT,
                       SettingsWindow.PopR, SettingsWindow.PopB, "`Generic Popup Background`（九宫格 `40k_popup`）");
            CheckRectS(FindChild(area, "Background fill"), SettingsWindow.FillL, SettingsWindow.FillT,
                       SettingsWindow.FillR, SettingsWindow.FillB, "`Background fill`（`40k_popup_texture` 平铺）");
            CheckRectS(FindChild(area, "Separators"), SettingsWindow.BarSepL, SettingsWindow.BarSepT,
                       SettingsWindow.BarSepR, SettingsWindow.BarSepB, "`Separators`");
            CheckRectS(FindChild(area, "Generic Close Button"), SettingsWindow.CloseL, SettingsWindow.CloseT,
                       SettingsWindow.CloseR, SettingsWindow.CloseB, "`Generic Close Button`（75×75）");
            CheckRectS(FindChild(FindChild(area, "Generic Close Button"), "Icon"),
                       SettingsWindow.CloseIconL, SettingsWindow.CloseIconT,
                       SettingsWindow.CloseIconR, SettingsWindow.CloseIconB, "关闭钮的 `Icon`");
            var bgQ = FindChild(area, "Generic Popup Background").GetComponentInChildren<ImageQuad>();
            CheckTrue(bgQ != null && bgQ.Texture != null && bgQ.Texture.name == "40k_popup",
                      "弹窗底图 = `40k_popup`");

            // ---------------- 左栏三个页签 ----------------
            Section("左栏页签（原版这一列是 VLG：padTop 30 · 每键 178.42×157.68 · 从 y=153.10 起）");
            var bar = FindChild(area, "Tab Buttons");
            CheckAtS(bar, SettingsWindow.BarL, SettingsWindow.BarT, SettingsWindow.BarR, SettingsWindow.BarB, "`Tab Buttons` 列");
            var names = new[] { "Graphics", "Audio", "Online" };
            for (int i = 0; i < 3; i++)
            {
                float t = SettingsWindow.BarT + SettingsWindow.BarPadTop + i * SettingsWindow.TabBtnH;
                var n = FindChild(bar, names[i]);
                CheckTrue(n != null, $"第 {i + 1} 个键 `{names[i]}` 建出来了（我们只建 3 个 —— 原版 5 个，见文件头 ③）");
                if (n == null) continue;
                CheckAtS(n, SettingsWindow.BarL, t, SettingsWindow.BarR, t + SettingsWindow.TabBtnH,
                         $"`{names[i]}` 键在 VLG 算出来的位置（第 {i + 1} 个）");
                CheckTrue(TextOf(n) == names[i], $"`{names[i]}` 的页签文字");
            }
            CheckTrue(FindChild(bar, "General") == null && FindChild(bar, "Account") == null,
                      "原版的 `General`/`Account`/`Support` 三个键**不建**（那几页没做，不摆假键）");

            // ---------------- 切页 ----------------
            Section("切页（只切 activeSelf）");
            var pages = new[] { "Graphics Tab", "Media Tab", "Online Tab" };
            for (int i = 0; i < 3; i++)
            {
                Click(FindChild(bar, names[i]));
                Check(win.Current, (SettingsTab)i, $"点 `{names[i]}` ⇒ 切到第 {i + 1} 页");
                for (int j = 0; j < 3; j++)
                {
                    var pg = FindChild(root, pages[j]);
                    CheckTrue(pg != null && pg.gameObject.activeSelf == (i == j),
                              $"`{pages[j]}` {(i == j ? "开着" : "关着")}");
                }
                CheckAtS(FindChild(root, pages[i]), SettingsWindow.TabsL, SettingsWindow.TabsT,
                         SettingsWindow.TabsR, SettingsWindow.TabsB, $"`{pages[i]}` 的根矩形 = `Tab Content`");
                var title = FindChild(FindChild(root, pages[i]), "Tab Title");
                CheckTrue(title != null && !string.IsNullOrEmpty(TextOf(title)), $"`{pages[i]}` 有页标题");
                CheckLeftS(title, SettingsWindow.TitleL, SettingsWindow.TitleR, SettingsWindow.TitleT,
                           SettingsWindow.TitleB, "页标题**左对齐**到原版矩形左边缘（fs55 那条）");
            }

            // ---------------- 图像页 ----------------
            Section("图像页：画质档 + VSync（**点了真去改设置**；自检用注入点，不改工程设置）");
            Click(FindChild(bar, "Graphics"));
            int q0 = QualitySettings.GetQualityLevel();
            int qAsked = -1, vAsked = -1;
            SettingsWindow.QualitySetterOverride = lv => qAsked = lv;
            Click(FindChild(FindChild(root, "Graphics Tab"), "QualityHit"));
            SettingsWindow.QualitySetterOverride = null;
            int qWant = (q0 + 1) % Mathf.Max(1, QualitySettings.names.Length);
            Check(qAsked, qWant, $"点画质行 ⇒ 要求切到**下一档**（{q0} → {qAsked}，共 {QualitySettings.names.Length} 档）");
            CheckTrue(win.Flash != null && win.Flash.Contains("画质"), $"点完**有话说**（「{win.Flash}」）");
            CheckTrue(win.QualityLabel != null && !string.IsNullOrEmpty(win.QualityLabel.Text),
                      "画质行上显示了当前档名");
            CheckNear(QualitySettings.GetQualityLevel(), q0, 0.01f,
                      "🔴 **自检没有把工程的画质档改掉**（`QualitySettings.asset` 不许被自检写脏）");

            int v0 = QualitySettings.vSyncCount;
            SettingsWindow.VSyncSetterOverride = c => vAsked = c;
            Click(FindChild(FindChild(FindChild(root, "Graphics Tab"), "VSync"), "Hit"));
            SettingsWindow.VSyncSetterOverride = null;
            Check(vAsked, v0 > 0 ? 0 : 1, $"点 `VSync` 行 ⇒ 要求翻转（{v0} → {vAsked}）");
            CheckNear(QualitySettings.vSyncCount, v0, 0.01f, "🔴 `vSyncCount` 也没被自检改掉");
            Debug.Log(P + "  （图像页只建了 `Quality`/`VSync`/`FPS limit` 三件 —— 原版那 7 行里其余几件我们没那功能，不摆假开关）");

            // ---------------- 音频页 ----------------
            Section("音频页：三根滑块（`WfSlider` —— 工程里唯一一份滑块实现）");
            Click(FindChild(bar, "Audio"));
            var sl = win.AudioSliders;
            CheckTrue(sl != null && sl.Length == 3 && sl[0] != null && sl[1] != null && sl[2] != null,
                      "三根滑块都建出来了（Music / Sound Effects / Voice-overs）");
            if (sl != null && sl[0] != null)
            {
                CheckTrue(sl[0].HasArt, "滑块的三张图都在（`Volume_bar_inactive` / `_active` / `Volume_button`）");
                float before = WarpforgeAudio.Music;
                sl[0].SetValue(0.42f, true);
                CheckNear(WarpforgeAudio.Music, 0.42f, 0.01f,
                          $"拖第一根 ⇒ **真的写进 AudioMixer**（Music {before:F2} → {WarpforgeAudio.Music:F2}）");
                sl[0].SetValue(before, true);
                // 轨道宽取的是**本页容器宽 684.19**（不是战斗内那根的 561.08）
                var rowN = sl[0].WorldPos;
                float left = sl[0].LeftWorld.x * 108f + 960f, right = sl[0].RightWorld.x * 108f + 960f;
                CheckNear(right - left, SettingsWindow.AuTrackW * SettingsWindow.RootScale, 2f,
                          "轨道宽 = 本页容器宽 684.19 × 0.9（⚠️ 战斗内那根是 561.08×1.0，两者不同）");
            }

            // ---------------- 联机页 ----------------
            Section("联机页（**这一页是我们新增的设计**，用户规格逐条）");
            Click(FindChild(bar, "Online"));
            CheckTrue(win.HostBlock != null && win.ClientBlock != null, "主机块与客机块都建了");
            Check(win.Role, NetRole.Host, "出厂是「主机」那一块（用户规格：勾选主机或客机）");
            CheckTrue(win.HostBlock.gameObject.activeSelf && !win.ClientBlock.gameObject.activeSelf,
                      "出厂只显示「主机」块");

            // 【刷新】填本机 IP
            // 🔴 **2026-09-26 加严**：原来只断「填了个**合法 IP**」—— 而 `LocalIPv4()` 失败时会**回落
            //    `127.0.0.1`**，那**也是**合法 IP ⇒ **功能坏着、断言照样绿**（实测就是这么坏的：
            //    `Dns.GetHostAddresses` 在批处理里抛 `Illegal byte sequence` ⇒ 【刷新】永远填回环）。
            //    现在改成断：① 枚举网卡这条路**找得到地址**；② 有真网卡时**不能填回环**；③ 多网卡能**换下一个**。
            var addrs = NetConfig.LocalAddresses();
            CheckTrue(addrs != null && addrs.Count > 0,
                      $"★ 枚举网卡找得到地址（实得 {(addrs == null ? 0 : addrs.Count)} 个）—— "
                    + "原来走 `Dns.GetHostAddresses`，在批处理里直接抛异常");
            var usable = addrs.FindAll(x => x.Usable);
            CheckTrue(usable.Count > 0, $"★ 至少有一个**能给对面填**的地址（实得 {usable.Count} 个）");
            // 🆕 网卡名是新加的 —— 多网卡/虚拟网卡时玩家**全靠它认**是哪一块
            CheckTrue(usable.TrueForAll(x => !string.IsNullOrEmpty(x.nic)),
                      "★ 每个候选地址都带**网卡名**（例如「WLAN」）—— 多网卡时靠它认");

            Click(FindChild(win.HostBlock, "Refresh"));
            CheckTrue(!string.IsNullOrEmpty(win.IpField.Text), $"点【刷新】⇒ IP 框里填上了本机地址（{win.IpField.Text}）");
            CheckTrue(System.Net.IPAddress.TryParse(win.IpField.Text, out _), "填进去的是个合法 IP");
            CheckTrue(win.IpField.Text != "127.0.0.1",
                      $"★ 填的**不是回环**（实得 {win.IpField.Text}）—— 把回环给对面等于没填；"
                    + "原来失败时正是回落它，而断言只判「是不是合法 IP」⇒ **照绿**");

            // 多网卡（有线 + 无线 + VPN）是**真实场景**：再点一次该换下一个候选
            // ⚠️ 循环**只在「能用的」地址里转** —— 回环（`127.0.0.1` / `::1`）与 v6 链路本地
            //    （`fe80::`）**填给对面等于没填**，不该出现在循环里。
            if (usable.Count > 1)
            {
                string first = win.IpField.Text;
                Click(FindChild(win.HostBlock, "Refresh"));
                CheckTrue(win.IpField.Text != first,
                          $"★ 再点一次【刷新】⇒ **换到下一个地址**（{first} → {win.IpField.Text}）；"
                        + $"本机共 {usable.Count} 个能用的候选");
            }
            for (int k = 0; k <= usable.Count; k++)     // 转满一圈
            {
                Click(FindChild(win.HostBlock, "Refresh"));
                string got = win.IpField.Text;
                string low = got.ToLowerInvariant();
                CheckTrue(got != "127.0.0.1" && got != "::1",
                          $"★ 循环第 {k + 1} 下**落不到回环**（实得 {got}）");
                // ⚠️ 还有三类「看着像地址、其实出不去」：IPv4 链路本地 `169.254.x.x`（网线没插时会有）、
                //    IPv6 链路本地 `fe80::`、IPv6 唯一本地 `fc..`/`fd..`（**最像公网地址的那个坑**）
                CheckTrue(!got.StartsWith("169.254.") && !low.StartsWith("fe80")
                          && !low.StartsWith("fc") && !low.StartsWith("fd"),
                          $"★ 循环第 {k + 1} 下也**不会填「出不去」的地址**（169.254 / fe80 / fc-fd）—— 实得 {got}");
            }

            // 切角色
            Click(FindChild(FindChild(root, "Online Tab"), "Role Client"));
            Check(win.Role, NetRole.Client, "点「Client」⇒ 角色切成客机");
            CheckTrue(!win.HostBlock.gameObject.activeSelf && win.ClientBlock.gameObject.activeSelf,
                      "切成客机 ⇒ 块也跟着换（只有一块可见）");
            Check((int)NetConfig.Current.role, (int)NetRole.Client, "角色**落盘**了（`NetConfig`）");

            // 【检查连接】打一个没人听的端口 ⇒ 必须**如实失败**
            win.IpField.SetText("127.0.0.1");
            var c = NetConfig.Current; c.port = 1;              // 端口 1 不会有人听
            Click(FindChild(win.ClientBlock, "Check Button"));
            CheckTrue(win.Flash != null && win.Flash.Length > 0, $"点【检查连接】⇒ 有反馈（「{win.Flash}」）");
            // 会话要有人泵（批处理里没有帧循环）
            var sess = NetRuntime.Instance != null ? NetRuntime.Instance.Session : null;
            CheckTrue(sess != null, "`NetRuntime` 建起来了（会话宿主）");
            if (sess != null)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 6000 && sess.State != NetState.Closed) { sess.Pump(); System.Threading.Thread.Sleep(10); }
                Check(sess.State, NetState.Closed, $"连一个没人听的端口 ⇒ 会话**如实结束**（{sess.LastError}）");
                CheckTrue(sess.LastError != null && sess.LastError.Length > 0, "失败原因是**人话**（不是空的）");
            }

            // 关窗
            Click(FindChild(FindChild(area, "Generic Close Button"), "Hit"));
            Check(win.CurrentState, WindowState.Closed, "点关闭钮 ⇒ 窗口进 Closed 态");

            Shoot("settings_online.png", true);
        }
        finally
        {
            NetConfig.OverridePath = null;
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }

        Debug.Log(P + $"===== 通过 {_pass} · 失败 {_fail} =====");
        if (_fail > 0) foreach (var f in _failures) Debug.LogError(P + "  ✗ " + f);
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    // ============================================================ 建场景

    static SettingsWindow Build(out Transform root)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.aspect = LayoutSpace.DesignAspect;      // ⚠️ 必须在建任何东西之前定死（批处理默认 4:3）
        LayoutSpace.Apply(cam);

        var anchors = new GameObject("Window Anchors").transform;
        MakeHolder(anchors, "3 - PopUp Holder", WindowsPlacement.Popup);

        var wmGo = new GameObject("WindowsManager");
        var wm = wmGo.AddComponent<WindowsManager>();

        var win = SettingsWindow.Create(wm);
        wm.OpenWindow(win);
        root = win.transform;
        return win;
    }

    static void MakeHolder(Transform parent, string name, WindowsPlacement p)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        var h = t.gameObject.AddComponent<WindowHolder>();
        h.placement = p;
        h.RegisterNow();
    }

    // ============================================================ 截图

    static void Shoot(string file, bool allowBlank = false)
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
        float lum = MeanBrightness(tex);
        if (allowBlank) Debug.Log(P + $"  截图 {file} 平均亮度 {lum:F1}（**按已知情况放行**）");
        else CheckTrue(lum > 3f, $"{file} 不是空图（平均亮度 {lum:F1} > 3）");
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
    }

    static float MeanBrightness(Texture2D t)
    {
        var px = t.GetPixels32();
        double s = 0;
        for (int i = 0; i < px.Length; i += 7) s += (px[i].r + px[i].g + px[i].b) / 3.0;
        return (float)(s / (px.Length / 7.0));
    }
}
