// NodeDump.cs — 「我们侧」运行时 UI 树 dump 宿主（第一百一十七会话 · `B117_G7`）
//
// 为什么要有它
// ------------
// `工具/prefab_layer_diff.py` 能比「Unity YAML ↔ 15 列运行时 TSV dump」，但那份 dump 一直是
// **原版自己**的（`SceneJumpShot` mod，见 `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_*.tsv`）。
// 我们这边——外壳（主菜单/左栏）· 收藏卡池 · 卡组编辑 · 战斗 HUD · 战斗大图详情窗——**全是运行时 `.cs`
// 建出来的**（`CardPresentation/Scenes/Battle.unity` 里只有 31 个 GameObject）⇒ **没有任何可机读的树**，
// 界面回炉就跑不了「原版 ↔ 我们」的机读差异。
// 本宿主把**我们运行时建出来的窗口树**按**同一套 15 列**吐成 TSV。
//
// 列序 = 逐字照原版 mod 的 `DumpGo`（`d:/2/Warpforge_tools/scenejumpshot/SceneJumpShot.cs:41-97`）：
//   0 path（**父节点的全路径**，不是自己的）· 1 name · 2 activeSelf · 3 activeInHierarchy ·
//   4 rect（`RT` / `TF`）· 5 anchoredPosition · 6 sizeDelta · 7 anchorMin · 8 anchorMax · 9 pivot ·
//   10 `TXT` 标记 · 11 text · 12 fontSize · 13 `IMG` 标记 · 14 sprite
//   ⚠️ **表头故意照抄原版那行 11 列错位表头**（`path name activeSelf activeInHierarchy rect pos pivot
//      text fontSize color image`）。理由：`prefab_layer_diff.load_dump_tree` 用
//      `first[0] == "path" and "anchoredPosition" not in first` 判表头并跳过；
//      若写一行「正确的 15 列表头」（含 `anchoredPosition`），那行会被当**数据行**读成一颗假根节点。
//      ⇒ 「列名错位」这件事是原版 dump 的已知缺陷，这里保留它 = 与消费方（那份工具）逐字相容。
//
// 🔴 与原版**必须不同**的两处（都是为了「读得到真值」）
// ----------------------------------------------------
// ① **原版 dump 的 TXT 组整组失效**：它用 `st.GetField("m_text")`（不爬基类，字段在 `TMP_Text` 上）
//    ⇒ 原版那份 `runtime_ui_dump_drive.tsv` 里 `text` **324/324 全空**、`fontSize` **全 0**。
//    这里一律走**公开 API** `TMP_Text.text` / `TMP_Text.fontSize` ⇒ 拿得到真值（跑完会印读数自证）。
// ② 我们的图件主力是 `ImageQuad`（自建 mesh quad，不是 uGUI `Image`）⇒ `sprite` 列优先取
//    uGUI `Image.sprite`，其次 `RawImage.texture`、**其次 `ImageQuad.Texture`**、最后 `SpriteRenderer.sprite`。
//    语义等价：这一格「画的是哪张图」。⚠️ 这是**等价映射**、不是原版实现（原版只有 uGUI `Image`）。
//
// 两处**结构差异**（如实登记，⛔ 不是 bug）
// ----------------------------------------
// · 我们的 `Label` 把 TMP 建在**名为 `text` 的子节点**上（`Battle/Label.cs` 的 `BuildTmp()` →
//   `TmpFont.NewTextUgui(transform, "text", …)`），而原版 TMP 挂在节点**自己**身上。
//   ⇒ 本 dump 做两件事：**① 把 text/fontSize 提升到 `Label` 节点**（让它与原版那个节点同格可比）；
//   **② 跳过那颗纯实现细节的 `text` 子节点**（无子件、只挂 TMP、父上有 `Label`）——否则每颗字都会
//   多出一行「仅我们有」的噪声。
// · 我们的世界空间件（`ImageQuad` / 点阵 `Label`）挂在**普通 `Transform`** 上 ⇒ `rect` 列 = `TF`、
//   几何五列全空。这与原版 dump 对 `Transform` 节点的处理**完全一致**（原版也是 `TF` + 五空格）。
//
// 用法（⚠️ 必须用 Unity 批处理跑；**本文件不跑 Unity**，只保证编得过）
// -------------------------------------------------------------------
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod NodeDump.RunFromCLI \
//     -wfDumpWindow <slug> -wfDumpOut "d:/4/Unity/_tmp_view/nodedump/<slug>.tsv" -logFile -
//
//   slug（先只做这五个）：
//     shell              主菜单窗 + 左竖导航（`MainMenuScene.BuildForWindowShots()` 的出厂态）
//     collection_pool    收藏窗 · 卡牌页（`MainMenuRuntime.OpenCollection()`）
//     deck_editor        卡组编辑窗（`DeckScene.BuildForWindowShots()`，**用临时存档**、不碰真卡组）
//     battle_hud         战斗整棵树（`BattleScene.BuildScene(...)`）
//     card_display_window 战斗大图详情窗（`CardDisplayWindow.Create` + `Show(一张真卡)`）
//
// 🔴 **写盘纪律**（本工程踩过「裸跑就真写盘、冲掉 232 行手工修订」）：
//   不带 `-wfDumpWindow` **或** 不带 `-wfDumpOut` ⇒ **只打印用法、`EditorApplication.Exit(2)`、零写盘**。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CardPresentation;

/// <summary>「我们侧」运行时 UI 树 dump（15 列，逐字对原版 `SceneJumpShot.DumpGo`）。</summary>
public static class NodeDump
{
    const string P = "[NodeDump] ";

    /// <summary>⚠️ **故意照抄原版那行 11 列错位表头** —— 理由见文件头。⛔ 别「修正」它：
    /// 写成正确的 15 列表头会让 `prefab_layer_diff.load_dump_tree` 把这行当数据行。</summary>
    const string Header = "path\tname\tactiveSelf\tactiveInHierarchy\trect\tpos\tpivot\ttext\tfontSize\tcolor\timage";

    static readonly string[] KnownSlugs =
    {
        "shell", "collection_pool", "deck_editor", "battle_hud", "card_display_window",
    };

    public static void RunFromCLI()
    {
        string slug = ArgValue("-wfDumpWindow");
        string outPath = ArgValue("-wfDumpOut");

        // 🔴 显式动作闸：两样缺一样 ⇒ 只打印用法、rc=2、**零写盘**。
        if (string.IsNullOrEmpty(slug) || string.IsNullOrEmpty(outPath))
        {
            Usage(string.IsNullOrEmpty(slug) ? "缺 `-wfDumpWindow <slug>`" : "缺 `-wfDumpOut <路径>`");
            Exit(2);
            return;
        }
        if (Array.IndexOf(KnownSlugs, slug) < 0)
        {
            Debug.LogError(P + "未知 slug：`" + slug + "`（⛔ 不静默当默认值）");
            Usage("未知 slug");
            Exit(2);
            return;
        }

        int rc = RunCore(slug, outPath);
        Exit(rc);
    }

    // ============================================================ 本体

    static int RunCore(string slug, string outPath)
    {
        Debug.Log(P + "=== 我们侧树 dump 开始：`" + slug + "` ===");

        // 建场期间不出声 —— 照 7 个自检宿主的同一条口径（`WFSoundPlayer.Enabled` 是**全局静态开关**）
        bool prevSound = WarpforgeVFX.WFSoundPlayer.Enabled;
        WarpforgeVFX.WFSoundPlayer.Enabled = false;
        try
        {
            Transform target;
            try { target = BuildHost(slug); }
            catch (Exception e)
            {
                Debug.LogError(P + "建宿主抛异常（⛔ 不写半份 dump）：" + e);
                return 1;
            }
            if (target == null)
            {
                Debug.LogError(P + "宿主建出来是 null（⛔ 不写半份 dump）—— 这一扇**没开出来**");
                return 1;
            }

            // 根顺序：**目标那棵排第一**（其余场景根照原版 `DumpTree` 那样跟在后面）——
            // 让产物「首节点是根」，同时不丢同一个场景里的旁挂根（如 `CardDisplayWindow` 的世界壳）。
            var roots = CollectRoots(target);

            // 「等版面」对**每一棵根**都推一次 —— `card_display_window` 的卡体挂在**另一个根**
            //（`Card Display Window (world)`）上，只 settle 目标那棵会漏掉它里面的 `Label`。
            for (int i = 0; i < roots.Count; i++) Settle(roots[i]);

            var sb = new StringBuilder();
            sb.Append(Header).Append("\r\n");
            var stats = new Stats();
            for (int i = 0; i < roots.Count; i++) DumpGo(roots[i], roots[i].name, sb, stats);

            // ---- 读数自证（「TXT 组没失效」的唯一硬证据）----
            Debug.Log(P + $"行数（含表头）= {stats.Rows + 1} · 节点 = {stats.Rows}");
            Debug.Log(P + $"TXT 标记行 = {stats.TxtRows} · **text 非空 = {stats.TextNonEmpty}** · **fontSize 非零 = {stats.FontSizeNonZero}**"
                        + "（原版 dump 这两项是 0 / 0 —— 我们这一版必须 > 0，否则就是又退回了 `GetField` 那条失效路）");
            Debug.Log(P + $"IMG 标记行 = {stats.ImgRows} · **sprite 非空 = {stats.SpriteNonEmpty}**");

            // ---- 首节点是根 ----
            var lines = sb.ToString().Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
            {
                var f = lines[1].Split('\t');
                bool rootOk = f.Length >= 2 && f[0] == f[1];
                Debug.Log(P + $"首节点 = `{(f.Length > 1 ? f[1] : "<空>")}` · path==name（是根）= **{rootOk}**");
                if (!rootOk) Debug.LogError(P + "⚠️ 首节点不是根（path 列 ≠ name 列）—— 产物结构可疑，别当成功");
            }

            // ---- 落盘（**到这一步才写**；前面任何一步失败都零写盘）----
            var dir = Path.GetDirectoryName(Path.GetFullPath(outPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            // UTF-8 **带 BOM**（原版 `new StreamWriter(path, false, new UTF8Encoding(true))` 同款）；
            // `prefab_layer_diff.read_lines` 会自己剥 BOM，两种都读得动。
            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(true));
            Debug.Log(P + "已写出：" + outPath);

            // 判 rc：0 = 有内容且 TXT 组活着
            if (stats.Rows <= 0) { Debug.LogError(P + "行数 0 ⇒ 判失败"); return 1; }
            if (stats.TextNonEmpty <= 0 || stats.FontSizeNonZero <= 0)
            {
                Debug.LogError(P + "TXT 组读出来是空的 ⇒ 判失败（原版那条失效路又回来了）");
                return 1;
            }
            Debug.Log(P + "=== 完成：`" + slug + "` ===");
            return 0;
        }
        finally
        {
            WarpforgeVFX.WFSoundPlayer.Enabled = prevSound;
        }
    }

    /// <summary>五条建场路 —— 每一行都指名**那条生产 API**（⛔ 不是我们造的开法）。</summary>
    static Transform BuildHost(string slug)
    {
        switch (slug)
        {
            case "shell":
                // 出厂态（不开任何窗）= 主菜单窗 + 左竖导航。⚠️ 只建不存（`BuildForWindowShots` 的契约）。
                return MainMenuScene.BuildForWindowShots();

            case "collection_pool":
            {
                MainMenuScene.BuildForWindowShots();
                var menu = MainMenuRuntime.InstanceOrFind();
                if (menu == null) { Debug.LogError(P + "`MainMenuRuntime.InstanceOrFind()` = null"); return null; }
                var win = menu.OpenCollection();      // 原版 COLLECTION 导航钮那一跳（closeAll: true）
                return win != null ? win.transform : null;
            }

            case "deck_editor":
                // ⚠️ 内部会把 `DeckStore.OverridePath` 指到临时文件 ⇒ **不碰玩家的真卡组**。
                return DeckScene.BuildForWindowShots();

            case "battle_hud":
            {
                // ⚠️ `BattleScene.BuildScene` **自己不建场**（它往当前场景里建）⇒ 先 `NewScene`，
                //    与 `BattleScene.BuildAndSaveScene`（`:242`）同一个次序。
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BattleScene.BuildScene(out var drv, out var pb, out var eb, out var ic);
                if (drv == null) { Debug.LogError(P + "`BattleScene.BuildScene` 交出来的 driver = null"); return null; }
                // 🔴 **HUD 不在 `BuildScene` 里建** —— 它在 `driver.Begin(...)` 那一跳才建。
                //    下面这一串**逐句照 `BattleScene.RunCore`（`:646-652`）**：三件显式 `Build()`
                //    （`AddComponent` 在批处理下不保证跑 `Awake`，所以那三处由调用方补）+ `Begin` 开一局
                //    + 推一段补间（**批处理没有帧循环**，不推的话事件全卡在队列里、一个字都不建）。
                if (drv.selector != null) drv.selector.Build();
                if (drv.reticle != null) drv.reticle.Build();
                if (drv.skillPanel != null) drv.skillPanel.Build();
                drv.Begin(RuleEngine.StarterCards.EmberFaction, RuleEngine.StarterCards.TideFaction, 20260911);
                StepSeconds(BattleDriver.TurnHeadsUpHold + 0.3f);
                return FindRootByName("Battle");
            }

            case "card_display_window":
            {
                BuildCameraOnly();
                var win = CardDisplayWindow.Create(null);   // 内部会 `HudCanvas.Ensure(LayoutSpace.Cam)`
                if (win == null) { Debug.LogError(P + "`CardDisplayWindow.Create(null)` = null"); return null; }
                // 给一张**真卡**（`Simple` 的 `faction` 恒 null ⇒ 卡框/卡背取不到，本工程踩过）
                var card = CardData.Simple("NodeDump Sample", 3, 2, 4);
                card.faction = "Ultramarines";
                win.Show(card, null, null);
                return win.transform;
            }
        }
        return null;
    }

    /// <summary>只建一台相机（`LayoutSpace` 的口径与各宿主逐字相同 —— 宽高比必须**在建任何东西之前**定死）。</summary>
    static Camera BuildCameraOnly()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.aspect = LayoutSpace.DesignAspect;
        LayoutSpace.Apply(cam);
        camGo.AddComponent<AudioListener>();
        return cam;
    }

    /// <summary>批处理下的「推一帧」—— 逐句照 `BattleScene.Step`（`:48951-48960`）。
    /// 不推的话：事件全卡在时间线队列里（一条特效都不播）、`CardTween` 在 Manual 档下永远不动、
    /// 粒子不 `Simulate` 就停在出生态 —— 三项都会让 dump 出来的是**半成品树**。</summary>
    static void StepSeconds(float dt)
    {
        var d = UnityEngine.Object.FindObjectOfType<BattleDriver>();
        if (d != null) d.AdvanceTimeline(dt);
        CardTween.Advance(dt);
        foreach (var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>(true))
            if (ps != null) ps.Simulate(dt, withChildren: false, restart: false, fixedTimeStep: true);
    }

    static Transform FindRootByName(string name)
    {
        var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var go in sc.GetRootGameObjects())
            if (go.name == name) return go.transform;
        return null;
    }

    /// <summary>场景里的全部根，**目标所在的那棵排第一**（其余按引擎返回序）。
    /// <para>⚠️ `target` **不一定就是根** —— `card_display_window` 的靶子是
    /// `BattleHud Canvas ▸ … ▸ Card Display Window`，而它要的那棵**顶层的根是画布**
    /// ⇒ 这里先沿父链爬到顶，再把**那个根**排第一。⛔ 别直接把 `target` 塞进列表：
    /// 目标不是根时，它的祖先根会被当成「另一个根」再 dump 一遍（整棵窗出现两次）。</para></summary>
    static List<Transform> CollectRoots(Transform target)
    {
        var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var list = new List<Transform>();
        Transform top = target;
        while (top.parent != null) top = top.parent;
        list.Add(top);                       // 目标那棵 → 第一（「首节点是根」）
        foreach (var go in sc.GetRootGameObjects())
            if (go.transform != top) list.Add(go.transform);
        return list;
    }

    /// <summary>「等版面」—— 批处理下没有帧循环，所以只能显式推。
    /// 与 `Editor/WindowShots.cs` 的 `Settle` **同一口径**（`Canvas.ForceUpdateCanvases` +
    /// 每颗 `Label.ForceRelayout()`）。</summary>
    static void Settle(Transform root)
    {
        Canvas.ForceUpdateCanvases();
        if (root != null)
        {
            var labels = root.GetComponentsInChildren<Label>(true);
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] != null) labels[i].ForceRelayout();
        }
        Canvas.ForceUpdateCanvases();
    }

    // ============================================================ dump

    sealed class Stats
    {
        public int Rows, TxtRows, TextNonEmpty, FontSizeNonZero, ImgRows, SpriteNonEmpty;
    }

    static string Esc(string s)
    {
        return (s ?? "").Replace("\t", " ").Replace("\n", "\\n").Replace("\r", "");
    }

    static string F1(float v) { return v.ToString("F1", CultureInfo.InvariantCulture); }
    static string F3(float v) { return v.ToString("F3", CultureInfo.InvariantCulture); }

    /// <summary>逐字对原版 `DumpGo`。`path` = **父节点的全路径**（递归时传 `path + "/" + 自己.name`）。
    /// 根的第一行 `path` 就等于自己的 `name`（原版 `DumpTree` 也是这么调的）。</summary>
    static void DumpGo(Transform t, string path, StringBuilder sb, Stats st)
    {
        var go = t.gameObject;
        sb.Append(path).Append('\t').Append(Esc(go.name))
          .Append('\t').Append(go.activeSelf).Append('\t').Append(go.activeInHierarchy);

        // ---- 几何五列 ----
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            var ap = rt.anchoredPosition; var sd = rt.sizeDelta;
            var amin = rt.anchorMin; var amax = rt.anchorMax; var pv = rt.pivot;
            sb.Append("\tRT\t").Append(F1(ap.x)).Append(',').Append(F1(ap.y))
              .Append('\t').Append(F1(sd.x)).Append(',').Append(F1(sd.y))
              .Append('\t').Append(F3(amin.x)).Append(',').Append(F3(amin.y))
              .Append('\t').Append(F3(amax.x)).Append(',').Append(F3(amax.y))
              .Append('\t').Append(F3(pv.x)).Append(',').Append(F3(pv.y));
        }
        else
        {
            sb.Append("\tTF\t\t\t\t\t");       // 与原版对 `Transform` 节点逐字相同（五个空列）
        }

        // ---- TXT 两列（🔴 走公开 API，**不是** `GetField`）----
        string text = null; float fontSize = 0f; bool hasText = false;
        var tmp = go.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            hasText = true; text = tmp.text ?? ""; fontSize = tmp.fontSize;
        }
        else
        {
            // 我们的 `Label` 把 TMP 建在子节点 `text` 上 ⇒ 提升到 Label 节点（让它与原版那一格可比）
            var lb = go.GetComponent<Label>();
            if (lb != null)
            {
                hasText = true;
                var kid = t.Find("text");
                var kt = kid != null ? kid.GetComponent<TMP_Text>() : null;
                if (kt != null) { text = kt.text ?? ""; fontSize = kt.fontSize; }
                else { text = lb.Text ?? ""; fontSize = lb.FontSize; }   // 点阵后端的兜底
            }
        }
        if (hasText)
        {
            st.TxtRows++;
            if (!string.IsNullOrEmpty(text)) st.TextNonEmpty++;
            if (Math.Abs(fontSize) > 0.0001f) st.FontSizeNonZero++;
            sb.Append("\tTXT\t").Append(Esc(text)).Append('\t').Append(F1(fontSize));
        }
        else
        {
            sb.Append("\t\t\t");
        }

        // ---- IMG 两列 ----
        string sprite = null; bool hasImg = false;
        var uimg = go.GetComponent<UnityEngine.UI.Image>();
        if (uimg != null) { hasImg = true; sprite = uimg.sprite != null ? uimg.sprite.name : ""; }
        else
        {
            var raw = go.GetComponent<UnityEngine.UI.RawImage>();
            if (raw != null) { hasImg = true; sprite = raw.texture != null ? raw.texture.name : ""; }
            else
            {
                var q = go.GetComponent<ImageQuad>();      // 🔴 我们的图件主力（等价映射，见文件头 ②）
                if (q != null) { hasImg = true; sprite = q.Texture != null ? q.Texture.name : ""; }
                else
                {
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null) { hasImg = true; sprite = sr.sprite != null ? sr.sprite.name : ""; }
                }
            }
        }
        if (hasImg)
        {
            st.ImgRows++;
            if (!string.IsNullOrEmpty(sprite)) st.SpriteNonEmpty++;
            sb.Append("\tIMG\t").Append(Esc(sprite));
        }
        else
        {
            sb.Append("\t\t");
        }

        sb.Append("\r\n");
        st.Rows++;

        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            // 跳过 `Label` 那颗纯实现细节的 TMP 子节点（无子件 · 只挂 TMP · 父上有 `Label`）——
            // 它的 text/fontSize 已经提升到父节点了，留着就是每颗字多一行「仅我们有」的噪声。
            if (IsLabelTextImpl(c)) continue;
            DumpGo(c, path + "/" + go.name, sb, st);
        }
    }

    /// <summary>`Label` 的 TMP 子节点（`TmpFont.NewTextUgui(transform, "text", …)` 建的那颗）。</summary>
    static bool IsLabelTextImpl(Transform c)
    {
        if (c == null || c.name != "text" || c.childCount != 0) return false;
        var parent = c.parent;
        if (parent == null || parent.GetComponent<Label>() == null) return false;
        return c.GetComponent<TMP_Text>() != null;
    }

    // ============================================================ 杂项

    static string ArgValue(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }

    /// <summary>⚠️ 只在批处理下退出进程 —— 从菜单/编辑器里调时**不能**把编辑器关掉。</summary>
    static void Exit(int rc)
    {
        if (Application.isBatchMode) EditorApplication.Exit(rc);
        else if (rc != 0) Debug.LogError(P + "（非批处理：不调 `EditorApplication.Exit`，rc=" + rc + "）");
    }

    static void Usage(string why)
    {
        if (!string.IsNullOrEmpty(why)) Debug.LogError(P + why);
        Debug.Log(P + "用法（裸跑/缺参 = 只打印这段、rc=2、**零写盘**）：");
        Debug.Log(P + "  … -executeMethod NodeDump.RunFromCLI -wfDumpWindow <slug> -wfDumpOut \"d:/4/Unity/_tmp_view/nodedump/<slug>.tsv\" -logFile -");
        Debug.Log(P + "  slug：" + string.Join(" | ", KnownSlugs));
        Debug.Log(P + "  · shell                主菜单窗 + 左竖导航（`MainMenuScene.BuildForWindowShots()` 出厂态）");
        Debug.Log(P + "  · collection_pool      收藏窗 · 卡牌页（`MainMenuRuntime.OpenCollection()`）");
        Debug.Log(P + "  · deck_editor          卡组编辑窗（`DeckScene.BuildForWindowShots()`，用临时存档）");
        Debug.Log(P + "  · battle_hud           战斗整棵树（`BattleScene.BuildScene(...)`）");
        Debug.Log(P + "  · card_display_window  战斗大图详情窗（`CardDisplayWindow.Create` + `Show(真卡)`）");
    }
}
