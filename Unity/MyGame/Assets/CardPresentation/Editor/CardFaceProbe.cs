// ============================================================================
//  「单卡渲染」量尺 —— 把卡池里指定的几张卡按**对局里那一条代码路径**渲染成 PNG。
//
// 为什么要它：卡面组装对不对，**截图看不出细节、自检数字也看不出「像不像」** ——
// 唯一靠得住的尺子是**跟原版拼好的卡图并排比**：
//   `D:/2/Warpforge部队卡片/<阵营>/<类别>/…png`（原版 Print&Play 卡图，就是成品卡面）。
// 这个探针把我们的卡按同一张卡渲成同尺寸的图，放进 `_tmp_view/cardface/`，肉眼一比就知道差在哪。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod CardFaceProbe.Run -logFile -
//
// ⚠️ 卡面数据走 `BattleDriver.ToCardData` / `FaceText`（**探针不另写一份**，否则两边迟早不一致）。
// ============================================================================
using System.IO;
using UnityEngine;
using TMPro;
using RuleEngine;
using CardPresentation;

public static class CardFaceProbe
{
    const string OutDir = "d:/4/_tmp_view/cardface";
    // 渲染尺寸：跟着卡本体宽高比（2.0927 : 3.3313 = 0.628），留一点边给卡框
    const int W = 700, H = 1115;

    /// <summary>要渲哪几张（英文名 = 卡池里的 `name`）。
    /// 2026-09-13 扩成**按稀有度/类型覆盖面**挑** —— 用户要求核对「稀有度宝石、数值的位置」与
    /// 「插图有没有白边/黑边/超出卡框」，只渲一两种看不出问题（宝石是**按稀有度取色**的，
    /// 战术卡没有数值格，督军卡有「角色越出卡框」）。</summary>
    static readonly string[] Names =
    {
        "Heavy Intercessor",     // 单位卡（有兵种行、有数值）
        "Aggressor Sergeant",    // 手牌里看着碎的那张（对照它是卡本身的问题还是缩小导致的）
        "Roboute Guilliman",     // 督军 —— **角色越出卡框**最明显的一张（对照原版成品卡）
        "Armoured Offensive",    // 战术卡（没有抠图 → 只有一层）
        "Pariah Vanguard",       // 用户点名要看的那张
        "Canoptek Scarab",       // Sautekh common 单位（小费用、有护甲？）
        "Lychguard",             // Sautekh epic 单位
        "stormlord",             // Sautekh legendary 督军「风暴王」（用户点名问的）
        "Awakened Dynasty",      // Sautekh common 战术卡
        "Lord of the Storm",     // Sautekh legendary 战术卡
        "Da Old Ways",           // Goff rare 战术卡
        "Beast Snagga Boy",      // Goff rare 单位
        // ── 2026-09-15 加：**卡面图标**的三种记号各来一张（判据见 `资料/卡面图标_现状与缺口.md` §二之五）
        "Armorium Cherub",       // 修女会（`SOR67`）—— 行首 `☀`（符号**吃掉**，画成信仰图标）
        "Farseer",               // 灵族（`ASH_Farseer`）—— `①`（灵魂石档位，数字烘在图里 ⇒ 连数字吃掉）
        // ── 2026-09-15 加：**这一轮补立绘的那批**（原来 21 张配不上图，工具改完只剩 `Moment of Grace`）
        //    判据：`资料/PnP卡图_逐张对账_0915.md` §五 / §六。名字都**唯一**，不会撞同名跨阵营的卡。
        "Dark Pact of Excess",   // `BL20` —— 原版叫 `Mark of Chaos_Slaanesh`（改名，按 id 点名）
        "Chosen of the Four",    // `BL2`  —— 原版叫 `Warmaster`
        "Lord Kaphrael",         // `EC1`  —— 原版叫 `Lord Exultant`
        "Veldras the Sublime",   // `EC24` —— 原版叫 `Threnodic Choir Flawless`（**前缀陷阱**那张）
        "Undying Legions",       // `SAU61`—— 原版叫 `Annihilation Command`
        "Awakened Obelisk",      // `SAU65`—— 与重复行 `SAU_Awakening_Obelisk` **共用一张图**
        "Alien Idol",            // `GSC66`—— 防御卡，验「`defence_` 变体没被配成 `strat_`」
        "Moment of Grace",       // `SOR47`—— 原版旧名是 `Righteous Repugnance`（**错开一位**那张，用户点的）
        "Righteous Repugnance",  // `SOR6` —— 它该拿的是 `Purgator Mirabilis` 那张图（金色赎罪引擎）
        // ── 2026-09-19 加：**用户点名要并排比的那张**（PnP 有 `Aeldari/3部队/Warpforge_06_Howling-Banshee-Exarch.png`）
        //    并排比出来的问题：我们把「文字底板」做成了下半截一块**不透明白/黑板**，而 PnP 是
        //    **立绘铺满整张卡、文字直接压在立绘上**。见 `资料/PnP卡图_逐张对账_0915.md` 与本次的更正。
        "Howling Banshee Exarch",// `ASH79`
    };

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        var pool = CardDatabase.Load();
        Debug.Log($"[cardface] 卡池 {pool.Count} 张，开始渲染 {Names.Length} 张 → {OutDir}");

        foreach (var name in Names)
        {
            var def = FindByName(pool, name);
            if (def == null) { Debug.LogError($"[cardface] 卡池里没有 `{name}`"); continue; }

            var data = BattleDriver.ToCardData(def, def.Faction);
            // 把**运行时真正加载到的那张贴图** dump 出来 —— 排查「卡面渲出来和磁盘上的 PNG 不一样」时，
            // 这是唯一的决定性证据（截图里看不出是贴图的问题还是采样/压缩的问题）
            {
                var tex = CardArt.Portrait(BattleDriver.ArtKey(def));   // ⚠️ 立绘按 id 取名（自造的 26 张按卡名）
                if (tex != null)
                {
                    File.WriteAllBytes(Path.Combine(OutDir, SafeName(name) + "_tex.png"), tex.EncodeToPNG());
                    Debug.Log($"[cardface] 贴图 {name}: {tex.width}×{tex.height} format={tex.format} mip={tex.mipmapCount}");
                }
            }
            var root = new GameObject("probe_" + name);
            var view = CardView.Create(root.transform, data, name);
            // 卡的 z 都是 0 附近，相机放远一点正对着拍
            Shot(view, Path.Combine(OutDir, SafeName(name) + ".png"));

            // 诊断：再渲一张**只有底层**（关掉前景抠图层）——分层看杂色到底谁带来的
            {
                CardView.DebugNoArtFront = true;
                var root2 = new GameObject("probe_nofront_" + name);
                var v2 = CardView.Create(root2.transform, data, name + "_nofront");
                Shot(v2, Path.Combine(OutDir, SafeName(name) + "_nofront.png"));
                Object.DestroyImmediate(root2);
                CardView.DebugNoArtFront = false;
            }
            // 再渲一张**不可打出**态：手牌上费用不够的卡就是这个状态，
            // 用来确认「卡片四周那圈白边」到底是不是状态描边（`_rim`）
            view.SetHighlight(CardHighlightState.Unplayable);
            Shot(view, Path.Combine(OutDir, SafeName(name) + "_unplayable.png"));
            // 选中态：描边**该**出现（羽化后应该是软光，不是硬方框）
            view.SetHighlight(CardHighlightState.Selected);
            Shot(view, Path.Combine(OutDir, SafeName(name) + "_selected.png"));
            view.SetHighlight(CardHighlightState.Normal);

            var uv = view.FrameUvRect;
            Debug.Log($"[cardface] {name} 卡本体 {CardView.Width:F4}×{CardView.Height:F4}  "
                    + $"卡框实绘 {view.FrameDrawSize.x:F4}×{view.FrameDrawSize.y:F4}  "
                    + $"卡框 UV x[{uv.xMin:F3},{uv.xMax:F3}] y[{uv.yMin:F3},{uv.yMax:F3}]  "
                    + $"立绘实绘 {view.ArtDrawSize.x:F4}×{view.ArtDrawSize.y:F4}");

            foreach (var t in root.GetComponentsInChildren<TextMeshPro>(true))
            {
                t.ForceMeshUpdate();
                Debug.Log($"[cardface] {name}/{t.name}  fontSize={t.fontSize:F3}  "
                        + $"rect宽={t.rectTransform.sizeDelta.x:F3}  "
                        + $"bounds={t.textBounds.size.x:F3}x{t.textBounds.size.y:F3}  "
                        + $"lines={t.textInfo.lineCount}  text='{Trim(t.text)}'");
            }
            Object.DestroyImmediate(root);

            // 手牌 vs 场上：同一张卡按两种**真实尺寸**各渲一张 ——
            // 用来量「数值 / 稀有度宝石会不会跟着卡的大小偏移」。卡本体在 scale=1 时是
            // 2.0927 世界单位（= 226 px @108 px/单位），手牌 165 px / 场上 137.2 px（出处
            // `资料/对战排版_原版数值与改造方案.md`）→ 缩放 0.7301 / 0.6071。
            // ⚠️ 结构上不可能偏（`SetPose` 只改 `localScale`，所有图层都是卡单位坐标），
            //    但用户点名要查，就**渲出来量**：两张图缩到同尺寸比像素差。
            // ⚠️ **必须在 `DestroyImmediate(root)` 之后** —— 第一版放在前面，
            //    主视图还立在场景里，于是两张卡**叠在一张图上**（重影）。
            if (name == "Heavy Intercessor")
            {
                var scales = new float[] { 0.7301f, 0.6071f };
                var tags = new string[] { "hand", "board" };
                for (int i = 0; i < scales.Length; i++)
                {
                    var r2 = new GameObject("probe_" + tags[i]);
                    var v2 = CardView.Create(r2.transform, data, name + "_" + tags[i]);
                    v2.SetPose(Vector3.zero, 0f, scales[i]);
                    Shot(v2, Path.Combine(OutDir, SafeName(name) + "_" + tags[i] + ".png"));
                    Object.DestroyImmediate(r2);
                }
            }
        }
        Debug.Log("[cardface] 结束");
    }

    /// <summary>全池渲染 —— 「逐张并排验收」的输入清单（2026-09-15）。
    ///
    /// 为什么单开一个入口：`Run()` 的 `Names[]` 是**为了排查某个具体问题手挑的 26 张**，
    /// 而「逐张并排验收」要的是**全池 1127 张**（抽查看不出阵营级/系统性缺陷 ——
    /// 阵营行印成 `BLACKLEGION` 就是这么漏到验收阶段的）。
    ///
    /// 输出：`_tmp_view/cardface_all/<净化后的卡id>.png` + `_manifest.tsv`
    /// （`id / faction / name / file` —— 下游 python 拼版**只认这份清单**，不靠文件名反推）。
    /// ⚠️ **只渲主视图**：`_tex`/`_nofront`/`_selected`/`_unplayable` 那四张是排查用的，全池跑太贵。
    /// ⚠️ 走的是 `BattleDriver.ToCardData` + `CardView.Create`，**与对局同一条代码路径**。
    /// </summary>
    public static void RunAll()
    {
        const string OutAll = "d:/4/_tmp_view/cardface_all";
        Directory.CreateDirectory(OutAll);
        var pool = CardDatabase.Load();
        var manifest = new System.Text.StringBuilder("id\tfaction\tname\tfile\n");
        int n = 0, fail = 0;
        Debug.Log($"[cardface-all] 卡池 {pool.Count} 张 → {OutAll}");

        foreach (var def in pool)
        {
            string file = SafeName(def.Id) + ".png";
            try
            {
                var data = BattleDriver.ToCardData(def, def.Faction);
                var root = new GameObject("probe_all_" + def.Id);
                var view = CardView.Create(root.transform, data, def.Id);
                Shot(view, Path.Combine(OutAll, file), quiet: true);
                Object.DestroyImmediate(root);
                manifest.Append(def.Id).Append('\t').Append(def.Faction).Append('\t')
                        .Append(def.Name).Append('\t').Append(file).Append('\n');
                n++;
            }
            catch (System.Exception e)
            {
                // 不许静默失败：渲不出来的卡要留在日志里，且不写进清单（下游才不会当它「已渲」）
                Debug.LogError($"[cardface-all] `{def.Id}` 渲不出来：{e.GetType().Name}: {e.Message}");
                fail++;
            }
            // 1127 张贴图一路加载不释放会撑爆内存；批处理下没有帧循环，得手动回收
            if (n % 100 == 0) { Resources.UnloadUnusedAssets(); }
        }

        File.WriteAllText(Path.Combine(OutAll, "_manifest.tsv"), manifest.ToString(),
                          new System.Text.UTF8Encoding(false));
        Debug.Log($"[cardface-all] 渲完成功 {n} · 失败 {fail} · 清单 {OutAll}/_manifest.tsv");
    }

    static CardDef FindByName(System.Collections.Generic.List<CardDef> pool, string name)
    {
        foreach (var c in pool) if (c.Name == name) return c;
        return null;
    }

    static string SafeName(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }

    static string Trim(string s)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= 24 ? s : s.Substring(0, 24) + "…");

    /// <summary>把这张卡单独渲进一张图 —— 相机正交、正好框住整张卡（含卡框外沿）</summary>
    static void Shot(CardView view, string path, bool quiet = false)
    {
        var camGo = new GameObject("probeCam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        // 视野比卡本体大一圈：卡框是 2.2452×3.2572 且偏下 0.03，别把它切了
        cam.orthographicSize = 3.5f * 0.5f * 1.06f;
        cam.aspect = (float)W / H;
        cam.transform.position = new Vector3(0f, -0.03f, -20f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);   // 深底（想查「哪里是透的」就临时改成亮绿）

        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(camGo);
        if (!quiet) { Debug.Log($"[cardface] 写出 {path}"); }
    }
}
