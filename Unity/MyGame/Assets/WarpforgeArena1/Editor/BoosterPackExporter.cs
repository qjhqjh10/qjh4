// BoosterPackExporter.cs — 把**两扇卡包窗**（`Booster Pack Open Window` / `Booster Info Popup`）
//   及其依赖（开卡包粒子 prefab + 8 条开卡包 `AnimationClip`）从原版 AssetBundle 导成工程资产。
//
// ============================ 为什么要单独一个导出器（出处） ============================
// `资料/阶段二_商店_原版规格.md` **§五·三** 的依赖摸底（2026-10-03）：
//   · `boosterpacks_assets_all` **现管线一口都吃不下**（实读 367 GO / 299 ParticleSystem+299 PSR /
//     17 ForceField / 24 Material / 1 Mesh / 2 AnimationClip / 17 legacy `Animation` / 57 Sprite / 84 Texture2D）：
//       `import_original_art.py` 只搬静态 sprite · `extract_missing_shaders.py --prefabs` 只重打包、不落 Assets ·
//       `EffectExporter` **源包名硬编码 `battleprefabs_vfxandmisc_assets_all`**。
//   · `menus_assets_all` 里有**那 7 条开卡包 `AnimationClip`**（实测 8 条，见表）。
//   ⇒ **新写这一个**（照 `EffectExporter` 的形状抄：源包换这两包、根换这些件）。
//
// 🔴 **导出的东西里，我们运行时真正用哪几样**（别以为全都在用）：
//   | 产出 | 谁用 |
//   |---|---|
//   | **4 个开卡包粒子 prefab** → `Assets/CardPresentation/Effects/` | ✅ **运行时用**：`BoosterPackOpenWindow` 翻牌时按稀有度播（`WarpforgeEffectLibrary.TryGet(名字)` → `WarpforgeEffectPlayer.Play`） |
//   | **8 条 `AnimationClip`** → `Assets/WarpforgeVFX/Animations/*.anim` | ⚠️ **不直接播**（我们的菜单层是 ImageQuad/Label，没有 uGUI 动画）。它们的用途是**判据**：`Booster Window Open` 的**末帧关键帧**就是 5 张卡停住的位姿（`Shell/BoosterPackOpenWindow.cs` 里那几个常量照它抄，注释标了出处）。导出来是为了**能复核**，不是为了播放 |
//   | **2 个窗口 prefab** → `Assets/WarpforgeBooster/Prefabs/` | ⚠️ **参考/诊断用**：我们的窗口是**在菜单层重搭的**（同 `BoosterInfoPopup` 那条路），**不实例化**这份 prefab。它存在的意义是「依赖树可达」的物证 + 以后并排比对 |
//
// ============================ 落地在哪 ============================
//   · 粒子 prefab → **`Assets/CardPresentation/Effects/`** —— 那是 `EffectLibraryBuilder.UserPrefabDir`
//     （见 `WarpforgeVFX/Runtime/WFEffectInfo.cs:13` 与 `EffectLibraryBuilder.cs:51`），
//     **不是**我们挑的目录：丢别处 `EffectLibraryBuilder` 收不到。
//   · 窗口 prefab / 报告 → `Assets/WarpforgeBooster/`。
//   · 贴图 / 材质 / 网格 / `.anim` → **复用 `EffectExporter` 的那几个目录**（`Assets/WarpforgeVFX/{Textures,Materials,Meshes,Animations}`）。
//     理由：贴图导入设置（mip / maxTextureSize）、渲染队列真值表、`_EMISSION` 剥离这些是**同一条规则**，
//     抄第二份就是 CLAUDE.md §三 那条「两处写同一条规则 = 迟早不一致」⇒ 那几个助手已改成 `public` 直接调。
//     ⚠️ 代价：`EffectExporter.ClearGenerated()`（**全量重导特效**那条路）会连它们一起删
//     ⇒ 跑过全量特效重导之后，要**跟一次本导出器**。
//
// ============================ 跑法（三步，缺一不可） ============================
//   ① 先把这些根**重打成我们自己的小包**（源包里它们不是 addressable 根，Unity 枚举不到）：
//        `python 工具/extract_missing_shaders.py --prefabs`
//        （2026-10-03 起 `--prefabs` 同时打 `wf_prefabs_extra.bundle`（战场）+ **`wf_menus_extra.bundle`**
//          （两扇窗 + 4 个粒子 prefab + 8 条 clip）+ `wf_boosters_extra.bundle`（卡包外观 prefab，备着））
//   ② 导出：
//        unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//          -executeMethod BoosterPackExporter.Run -logFile "d:/4/_tmp_view/booster_export.log"
//        筛输出：`grep "^BP "`
//   ③ 生成效果库（否则粒子进不了 `WarpforgeEffectLibrary`，`TryGet` 取不到）：
//        `-executeMethod EffectLibraryBuilder.Run`
//
// ⚠️ 导出器**不碰** `Resources/`：那两张静态图（`Card Ready For Level Up` /
//    `40k_Cross_icon_cross_big Banned card`）走的是**另一条路** —— `工具/import_original_art.py` 的 `MENU_IMAGES`。
//
// ============================ 🆕 2026-10-09（A1101）动画那一跳 ============================
//   `Export()` 原来漏了 legacy `Animation` 的片段引用：`Booster Pack Open Window.prefab` 的
//   **7 个** `--- !u!111` 的 `m_Animation` **全是 guid 全 0**（`m_Animations[]` 里那几条同样全 0）
//   —— 与 A1095 在特效包里补的**是同一个形状的漏**。现已在本文件 `Export()` 末尾（binder 之前）
//   照 `EffectExporter.cs:1203-1268` **逐行补了同一支**，把那 7 处接回第 ① 步刚落的那 7 份 `.anim`。
//   判据（逐处清单 / 配对怎么核出来的）→ `资料/普查产出_第六会话/W_Booster动画地雷_A1101.md`。
//   ⚠️ 与 A1095 的差别：**这 7 处的目标 `.anim` 工程里是有的**（就是第 ① 步那 8 条里的 7 条）
//     ⇒ `ImportClip` 走**原地覆盖**（guid 不变），**没有造任何空 clip**。
//   ⚠️ 收敛判据（重导后怎么核「7 → 0」）：见同一份报告 §⑧。
//
// ============================ 🆕 2026-10-09（A1109）uGUI 与 TMP 那两跳 ============================
//   同一个 `Export()` 里**还缺两跳**，而且这两跳才是**窗口 prefab 的大头**：
//   `Booster Pack Open Window.prefab` 的 guid 全 0 共 **350** 条 = 动画 **30**（上一条已修）
//   + `Image.m_Sprite` **45** + `Image.m_Material` **21** + TMP `m_fontAsset` **127**
//   + TMP `m_sharedMaterial` **127**。根因：本 `Export()` 抄的是 `EffectExporter` 那套
//   「**粒子 / 网格 / `SpriteRenderer`**」——**里面根本没有 uGUI 与 TMP 两族**（现读全库核实），
//   而窗口 prefab 恰恰**全是**这两族。
//   ⚠️ **与上一条不同：简报说「照 `EffectExporter` 里对应的那两支抄」，现读不成立** ——
//     那两支**不存在**（`grep` 全库 `GetComponentsInChildren<Image>` / `TextMeshProUGUI` /
//     `fontSharedMaterial` **命中 0**）。⇒ 本件按**本函数里已有的那一支**（`SpriteRenderer` /
//     `SpriteMask` 的精灵跳）**同形**写，并在报告里逐行说明「母版不存在」这件事。
//   判据（320 条逐族清单 / 原版是哪份资产 / 工程里有没有）→
//     `资料/普查产出_第六会话/W_Booster两跳_A1109.md`。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using WarpforgeVFX;

public static class BoosterPackExporter
{
    const string P = "BP ";

    // 与 `EffectExporter.BundleDir` 同一个源包目录（不各写一份）
    static string BundleDir { get { return EffectExporter.BundleDir; } }

    /// <summary>卡包那两批件的**实测归属**（2026-10-03 只读普查：`UnityPy` 扫两个包全部 `GameObject` 的
    /// `m_Name` + `Transform.m_Father`）——
    /// 🔴 **本类不写死源包**：`LoadPacks()` 扫**整个源包目录 + 我们重打的小包**，按名字取
    ///    （写死源包就是上一版的错：把 4 个粒子 prefab 记成了 `boosterpacks_assets_all` 的）。
    ///   · `menus_assets_all`（16768 GO / 313 根）：`Booster Pack Open Window` · `Booster Info Popup` ·
    ///     `Boosterpack Open Card Rarity 1..4`（**4 件都是根**）· **8 条 clip**（全库唯一）
    ///   · `boosterpacks_assets_all`（367 GO / **17 根**）：`Booster Pack Standard` + 15 阵营/扩展变体
    ///     + `Booster Pack All Armies Variant`（**卡包外观 prefab**，原版 `visualPrefab` 就在这一族里）·
    ///     2 条 clip（`Booster Open Standard` / `Booster Open Sororitas`）。
    ///     ⚠️ **本导出器目前不导这一批** —— 我们的窗口画的是整屏纯色背景，用不上；
    ///        它们由 `extract_missing_shaders.py` 的 `BOOSTER_GROUPS[1]` 重打进 `wf_boosters_extra.bundle` 备着。</summary>

    /// <summary>我们自己重打的小包目录（`extract_missing_shaders.py --prefabs` 的产物）。</summary>
    const string StreamDir = "Assets/StreamingAssets/WarpforgeVFX";

    const string Root = "Assets/WarpforgeBooster";
    const string OutPrefabDir = Root + "/Prefabs";
    const string ReportPath = Root + "/导出报告.tsv";
    /// <summary>`EffectLibraryBuilder.UserPrefabDir` —— 卡包粒子 prefab 的落点（**不是我们挑的目录**）。</summary>
    static string EffectsDir { get { return EffectLibraryBuilder.UserPrefabDir; } }

    // ============================================================ 根（每一条都有判据）

    /// <summary>两扇窗。出处：`资料/阶段二_商店_原版规格.md` §五·二 / §五·三。
    /// 🔴 **实测归属 = `menus_assets_all.bundle`**（2026-10-03 只读普查：`Booster Pack Open Window`
    ///    Transform `1342405282582379555` · `Booster Info Popup` `5823516886071648943`，**两件都是根**）。
    /// ⚠️ 它们**不是 addressable 根** ⇒ 原包里 `GetAllAssetNames()` / `LoadAllAssets()` 两条都枚举不到，
    ///    必须先跑 `extract_missing_shaders.py --prefabs` 重打进 **`wf_menus_extra.bundle`**。</summary>
    static readonly string[] WindowRoots = { "Booster Pack Open Window", "Booster Info Popup" };

    /// <summary>开卡包粒子。**名字从 prefab 字段来、不是我猜的**：
    /// `Booster Pack Open Window` 的 `CardInBoosterPack.contentByRarities[]` 每条一个 `particles` PPtr，
    /// 逐个解出来就是这 4 件（`ContentByRarity__get_Particles.c`；逐条 rarities：
    /// `0/1 → Rarity 1` · `2 → Rarity 2` · `3 → Rarity 3` · `4/5 → Rarity 4`）。
    /// 🔴 **实测归属 = `menus_assets_all.bundle`**（**不是** `boosterpacks_assets_all` —— 2026-10-03 第一次实跑
    ///    `[P1] 这个包里没有 GameObject …` 四连红就是这个错：**照包名猜了归属**）。
    ///    四件都是**根**：`1`(Transform `4179873410099752137`) · `2`(`6344549431542765525`) ·
    ///    `3`(`-216069053023463151`) · `4`(`-5965722982508144977`)。重打进 **`wf_menus_extra.bundle`**。
    /// 🔴 **表只此一份** —— 转调运行期那份（`Shell/BoosterPackOpenWindow.CardFxNames`）：
    /// 那 4 个名字的**消费方**是窗口（翻牌播粒子），导出器只是照着导。
    /// （编辑期程序集引用运行期程序集是允许的、反过来不行 ⇒ 表放在运行期那一侧。）</summary>
    static string[] CardFxRoots { get { return CardPresentation.BoosterPackOpenWindow.CardFxNames; } }

    /// <summary>开卡包那几条 `AnimationClip`（`menus_assets_all`）。
    /// 🔴 **判据 = `Booster Pack Open Window` 的 MB 字段**（`MonoBehaviour_9012570135841684515.json`），
    /// 不是「把包里带 Booster 字样的都捞出来」：
    ///   · `windowAnimation`（GO 上一挂的 `Animation`）的 `m_Animations` = **[Booster Window Open, Booster Window Close]**
    ///     （`m_Animation` 默认那条 = `Booster Window Open`）；`closeAnimation` 字段 = **"Booster Window Close"**（字符串）。
    ///   · `backgroundAnimation`（`Booster pack Background` 上的 `Animation`）的 `m_Animations` = **[Booster Window - Background Shake On Open]**。
    ///   · `CardInBoosterPack.cardAnimation` 的 `m_Animations` = **[Card Idle, 8301228418192668656, Card Open Rare, Card Open Legendary]**，
    ///     其中 `8301228418192668656` 与 `contentByRarities[0].animationClip` 同 pid ⇒ = **Card Open Normal**。
    ///   · `OpenCardbacks`：**同包同族**（12 条 clip 里与开卡包同批次的那条）——
    ///     ⚠️ **这一条的判据比上面几条弱**（没有字段直接指着它，见 §五·三 原文：「`menus` 里那 7 条开卡包 `AnimationClip`」把它算在内）。
    ///     标成**判据较弱**，导出它只是不留缺口。
    /// ✅ **实测归属已核**（2026-10-03）：这 8 条**全部**在 `menus_assets_all/AnimationClip/`，
    ///    **8/8 名字逐字全中、全库唯一（无重名）** —— 所以按名字取是安全的。
    /// ⚠️ 顺带**不导**同包里另外 4 条（`Division Config` / `SearchingOpponentCog` / `Rank Up` / `Division Up`）——
    ///    它们属于别的界面（实测名字即用途）。</summary>
    static readonly string[] ClipNames =
    {
        "Booster Window Open",                      // 5 张卡的位姿来源（末帧关键帧）★
        "Booster Window Close",
        "Booster Window - Background Shake On Open",
        "Booster Opening - Card Idle",
        "Booster Opening - Card Open Normal",
        "Booster Opening - Card Open Rare",
        "Booster Opening - Card Open Legendary",
        "OpenCardbacks",                            // 判据较弱（见上）
    };

    /// <summary>稀有度 → 粒子 prefab 名。**判据与实现在窗口那一侧**
    /// （`CardPresentation.BoosterPackOpenWindow.CardFxFor`，含 `contentByRarities` 的逐条出处）。</summary>
    public static string CardFxFor(int rarity)
    {
        return CardPresentation.BoosterPackOpenWindow.CardFxFor(rarity);
    }

    // ============================================================ 报告
    static readonly Dictionary<string, string> Report = new Dictionary<string, string>();

    static void LoadReport()
    {
        if (!File.Exists(ReportPath)) return;
        foreach (var line in File.ReadAllLines(ReportPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            var p = line.Split('\t');
            if (p.Length >= 2) Report[p[0]] = string.Join("\t", p.Skip(1));
        }
        Debug.Log(P + $"已载入既有报告 {Report.Count} 条");
    }

    static void SaveReport()
    {
        var lines = new List<string> { "# 件名\t状态/原因" };
        foreach (var kv in Report.OrderBy(k => k.Key)) lines.Add($"{kv.Key}\t{kv.Value}");
        File.WriteAllLines(ReportPath, lines, new System.Text.UTF8Encoding(true));
    }

    static void EnsureFolders()
    {
        foreach (var p in new[] { "Assets", Root, OutPrefabDir, EffectsDir })
            if (!AssetDatabase.IsValidFolder(p))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\', '/'), Path.GetFileName(p));
    }

    // ============================================================ 包
    static readonly Dictionary<string, AssetBundle> LoadedPacks = new Dictionary<string, AssetBundle>();

    static void LoadPacks()
    {
        LoadedPacks.Clear();
        int n = 0, extra = 0;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b == null) continue;
            n++;
            LoadedPacks[Path.GetFileName(f)] = b;
        }
        // 我们自己重打的小包也要加载（非 addressable 的那几件只在那里是可加载根）
        try
        {
            foreach (var f in Directory.GetFiles(StreamDir, "*.bundle"))
            {
                var b = AssetBundle.LoadFromFile(f);
                if (b == null) continue;
                var fn = Path.GetFileName(f);
                if (LoadedPacks.ContainsKey(fn)) continue;
                LoadedPacks[fn] = b; extra++;
            }
        }
        catch (Exception e) { Debug.LogWarning(P + $"扫 {StreamDir} 失败：{e.Message}"); }
        Debug.Log(P + $"源包 {n} 个 + 重打的小包 {extra} 个已加载");
    }

    /// <summary>按名字在所有已加载的包里取一件。</summary>
    static T FindInPacks<T>(string name) where T : UnityEngine.Object
    {
        T hit = null;
        foreach (var kv in LoadedPacks)
        {
            try { hit = kv.Value.LoadAsset<T>(name); } catch { }
            if (hit != null) return hit;
        }
        // 兜底：枚举（重打过的小包里物件是容器根，按名字取应该已经命中；这一遍是为了「按名字取不到」时出声）
        foreach (var kv in LoadedPacks)
        {
            T[] all = null;
            try { all = kv.Value.LoadAllAssets<T>(); } catch { }
            if (all == null) continue;
            foreach (var a in all) if (a != null && a.name == name) return a;
        }
        return null;
    }

    // ============================================================ 入口

    /// <summary>导出。**幂等**：同名产物原地覆盖（不删目录 —— 见 CLAUDE.md §三
    /// 「生成资产别用 `GenerateUniqueAssetPath` + 每次先删光」那条：那会每跑一次换一批 guid）。</summary>
    public static void Run()
    {
        Debug.Log(P + "=== 卡包两扇窗 导出 开始 ===");
        EnsureFolders();
        LoadPacks();
        Report.Clear();
        LoadReport();

        // ---- ① 8 条 AnimationClip（判据见表）----
        int clipOk = 0;
        foreach (var n in ClipNames)
        {
            var c = FindInPacks<AnimationClip>(n);
            if (c == null)
            {
                Report[n] = "FAIL\t已加载的包里没有这条 AnimationClip（先跑 extract_missing_shaders.py --prefabs）";
                Debug.LogError(P + $"🔴 取不到 AnimationClip `{n}`" +
                               " —— 它**不是 addressable 根**，要先跑 `python 工具/extract_missing_shaders.py --prefabs`");
                continue;
            }
            try
            {
                EffectExporter.ImportClip(c);
                Report[n] = $"OK\tclip {c.length:F3}s · sample {c.frameRate:F0} · 曲线 {ClipCurveCount(c)} 条";
                clipOk++;
            }
            catch (Exception e)
            {
                Report[n] = $"FAIL\t{e.GetType().Name}: {e.Message}";
                Debug.LogError(P + $"导出 clip `{n}` 失败：{e.GetType().Name}: {e.Message}");
            }
        }
        Debug.Log(P + $"AnimationClip：{clipOk}/{ClipNames.Length} → Assets/WarpforgeVFX/Animations/（复用特效那套目录，见文件头）");

        // ---- ② 4 个开卡包粒子 prefab（运行时真正用的那 4 件）----
        int fxOk = 0;
        foreach (var n in CardFxRoots)
        {
            var g = FindInPacks<GameObject>(n);
            if (g == null)
            {
                Report[n] = "FAIL\t已加载的包里没有这个 GameObject";
                Debug.LogError(P + $"🔴 取不到粒子 prefab `{n}`" +
                               " —— 先跑 `python 工具/extract_missing_shaders.py --prefabs`");
                continue;
            }
            try
            {
                string detail = Export(g, EffectsDir, true);
                Report[n] = "OK\t" + detail;
                fxOk++;
            }
            catch (Exception e)
            {
                Report[n] = $"FAIL\t{e.GetType().Name}: {e.Message}";
                Debug.LogError(P + $"导出 `{n}` 失败：{e.GetType().Name}: {e.Message}");
            }
        }
        Debug.Log(P + $"开卡包粒子：{fxOk}/{CardFxRoots.Length} → {EffectsDir}" +
                   "（⚠️ 之后**必须**跑一次 `-executeMethod EffectLibraryBuilder.Run`，否则运行时 `TryGet` 取不到）");

        // ---- ③ 两扇窗的 prefab（参考/诊断用；我们运行时不用它，见文件头那张表）----
        int winOk = 0;
        foreach (var n in WindowRoots)
        {
            var g = FindInPacks<GameObject>(n);
            if (g == null)
            {
                Report["window:" + n] = "FAIL\t已加载的包里没有这个 GameObject";
                Debug.LogError(P + $"🔴 取不到窗口 prefab `{n}`" +
                               " —— 先跑 `python 工具/extract_missing_shaders.py --prefabs`");
                continue;
            }
            try
            {
                string detail = Export(g, OutPrefabDir, false);
                Report["window:" + n] = "OK\t" + detail;
                winOk++;
            }
            catch (Exception e)
            {
                Report["window:" + n] = $"FAIL\t{e.GetType().Name}: {e.Message}";
                Debug.LogError(P + $"导出窗口 `{n}` 失败：{e.GetType().Name}: {e.Message}");
            }
        }
        Debug.Log(P + $"窗口 prefab：{winOk}/{WindowRoots.Length} → {OutPrefabDir}（参考用，运行时不实例化）");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SaveReport();
        Debug.Log(P + $"=== 卡包两扇窗 导出 结束：clip {clipOk}/{ClipNames.Length} · " +
                   $"粒子 {fxOk}/{CardFxRoots.Length} · 窗口 {winOk}/{WindowRoots.Length} · 报告 {ReportPath} ===");
    }

    static int ClipCurveCount(AnimationClip c)
    {
        try { return AnimationUtility.GetCurveBindings(c).Length
                   + AnimationUtility.GetObjectReferenceCurveBindings(c).Length; }
        catch { return -1; }
    }

    // ============================================================ 单个 prefab

    /// <summary>导一件（形状照 `EffectExporter.Export` 抄；**规则**全部转发到那边的助手，不在这里再写一份）。
    /// 返回一句「导了什么」写进报告。</summary>
    static string Export(GameObject src, string prefabDir, bool asEffect)
    {
        var inst = UnityEngine.Object.Instantiate(src);
        inst.name = src.name;
        EffectExporter.StripMissingScripts(inst);

        int approx = 0;
        var usedShaders = new HashSet<string>();
        var defs = new List<WFMatDef>();
        var defIndex = new Dictionary<string, int>();
        var slots = new List<int>();
        var trailSlots = new List<int>();

        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            var ps = r.GetComponent<ParticleSystem>();

            // 粒子拖尾槽：**按渲染器序号记账，每个渲染器记且只记一条**（判据见 `EffectExporter.Export` 里那段）
            var psr0 = r as ParticleSystemRenderer;
            if (psr0 != null && psr0.trailMaterial != null)
            {
                int di = EffectExporter.DefIndex(defs, defIndex, psr0.trailMaterial);
                EffectExporter.StripGlobalKeywords(defs[di], ps, psr0.trailMaterial.shader);
                trailSlots.Add(di);
                var tm = EffectExporter.ImportMaterial(psr0.trailMaterial, out bool ta, out string to);
                if (tm != null) { psr0.trailMaterial = tm; if (ta) approx++; usedShaders.Add(to); }
            }
            else trailSlots.Add(-1);

            var mats = r.sharedMaterials;
            if (mats.Length == 0) continue;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var om = mats[i];
                if (om == null) { slots.Add(-1); continue; }
                int di = EffectExporter.DefIndex(defs, defIndex, om);
                EffectExporter.StripGlobalKeywords(defs[di], ps, om.shader);
                slots.Add(di);
                var nm = EffectExporter.ImportMaterial(om, out bool wasApprox, out string origShader);
                if (nm != null) { mats[i] = nm; changed = true; usedShaders.Add(origShader); if (wasApprox) approx++; }
            }
            if (changed) r.sharedMaterials = mats;
        }

        foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null) mf.sharedMesh = EffectExporter.ImportMesh(mf.sharedMesh);
        foreach (var psr in inst.GetComponentsInChildren<ParticleSystemRenderer>(true))
            if (psr.mesh != null) psr.mesh = EffectExporter.ImportMesh(psr.mesh);

        foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sprite != null) sr.sprite = EffectExporter.ImportSprite(sr.sprite);
        foreach (var sm in inst.GetComponentsInChildren<SpriteMask>(true))
            if (sm.sprite != null) sm.sprite = EffectExporter.ImportSprite(sm.sprite);

        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            var tsa = ps.textureSheetAnimation;
            int n = tsa.spriteCount;
            for (int i = 0; i < n; i++)
            {
                var s = tsa.GetSprite(i);
                if (s == null) continue;
                var imported = EffectExporter.ImportSprite(s);
                if (imported != null) ps.textureSheetAnimation.SetSprite(i, imported);
            }
        }

        // ---- 🆕 2026-10-09（A1101）：**legacy `Animation` 的片段那一跳** ----
        // 本 `Export()` 从 `EffectExporter.Export` 抄形状时**漏了这一跳**：上面「精灵」那一跳
        //（`:385-401`）做了，「动画」那一跳没做。母版 = `EffectExporter.cs:1203-1268`（A1095 给特效包
        // 45 处补的那一支）—— **本支就是照它逐行抄的**，四处「有意的不一样」也一一对应。
        //   实测（现读 `WarpforgeBooster/Prefabs/Booster Pack Open Window.prefab`，962823 B / 纯 LF）：
        //     `--- !u!111`（`Animation`）共 **7 个**，**7/7** 的 `m_Animation` 都是
        //     `{fileID: …, guid: 00000000000000000000000000000000, type: 0}`（`m_Animations[]` 里那几条同样全 0）；
        //     另一扇 `Booster Info Popup.prefab` 是 **0 个** `Animation`（**那件本来就没有，不是漏做**）。
        //   根因：本文件第 ① 步（`:238-262`）**只把 8 条 clip 落成 `.anim`**，第 ③ 步导窗口 prefab 时
        //     **没有任何一步把引用接回去**（`grep Animation` 在本文件里只有「clip 导入」那一支）⇒ 落成 guid 全 0。
        //   判据 → `资料/普查产出_第六会话/W_legacy动画地雷.md` §⑦·1（把这里记成「同型地雷 7 处」）。
        // ✅ **与 A1095 那 45 处不同：这 7 处要的 `.anim` 工程里【是有的】** ——
        //    7 处的 `fileID` **就是原版包里的 pathID**，逐个反查到 `assets_full/bundle_menus_assets_all/
        //    AnimationClip/AnimationClip_<pid>.json` 的 `m_Name`，**7/7 逐字命中第 ① 步 `ClipNames` 里那 8 条中的 7 条**
        //    （8 条里唯一没被这 7 处用到的是 `OpenCardbacks`）：
        //      `-7993935874865337289` = `Booster Opening - Card Idle`            → `CardInBoosterPack UI 1..5`（默认那条）
        //      `8301228418192668656`  = `Booster Opening - Card Open Normal`     ↑ 同上（`m_Animations[1]`）
        //      `-9033435933554767437` = `Booster Opening - Card Open Rare`       ↑ 同上（`m_Animations[2]`）
        //      `-1691631367813043119` = `Booster Opening - Card Open Legendary`  ↑ 同上（`m_Animations[3]`）
        //      `-177604310092043705`  = `Booster Window - Background Shake On Open` → `Booster pack Background`
        //      `5911697182262325120`  = `Booster Window Open`                    → `Booster Pack Open Window`（默认那条）
        //      `-8254079481253810722` = `Booster Window Close`                   ↑ 同上（`m_Animations[1]`）
        //    ⇒ `ImportClip` 走的都是**原地覆盖**那一支（`EffectExporter.cs:2374-2381`，**guid 不变**），
        //      所以接回去之后引用的正是第 ① 步刚落的那 7 份 `.anim`（`WarpforgeVFX/Animations/`，guid 见报告）。
        // ⚠️ **两条入口都要写、缺一不可**：`m_Animation`（默认那条）与 `m_Animations[]`
        //    （真正被 `Animation.Play("名字")` 查的那张表）。本件里已有活证据：
        //    **5 个 `CardInBoosterPack UI *` 的 `m_Animations` 各有 4 条**（Idle / Open Normal / Rare / Legendary），
        //    `Booster Pack Open Window` 有 2 条（Open / Close）⇒ **只补 `m_Animation` 会静默漏掉其余几条**。
        // ⚠️ **不传 `loop`**（与 `EffectExporter` 那一支**同样有意不传**）：clip 自己的 `m_WrapMode` /
        //    `AnimationClipSettings` 会随 `Instantiate` 一起过来，传 `loop` 反而是**我们替原版做决定**。
        //    （本件反证：实测原版这 **7 条片段**的 `m_WrapMode` = `2/0/0/0/0/0/0` —— **彼此不同**
        //      （`Booster Opening - Card Idle` 那条是 `2` = Loop，其余 6 条是 `0`）⇒ 更不该统一设一个值。）
        // ⚠️ **零幻觉兜底**：接不上就**留空 + 出声**，⛔ **绝不 `CreateAsset` 一个空 clip 顶上**（那是静默造一个假动作）。
        int clipOk = 0, clipMiss = 0;
        foreach (var an in inst.GetComponentsInChildren<Animation>(true))
        {
            // ① `m_Animations[]`。⚠️ `AnimationUtility.GetAnimationClips(Animation)` 那个重载**已 obsolete**
            //    （Unity 文档原文：「is obsolete and has been replaced with GetAnimationClips(GameObject)」）
            //    ⇒ 走 `GameObject` 那个（非 obsolete、语义相同：取该物件上 `Animation` 的片段表）。
            var clips = AnimationUtility.GetAnimationClips(an.gameObject);
            var def = an.clip;        // ② `m_Animation`：**先取下来**，别指望写回数组之后它还认得原来那条
            if (clips != null && clips.Length > 0)
            {
                var imported = new AnimationClip[clips.Length];
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] == null) continue;      // 空槽 ⇒ 原样留空（原版就是空槽的那种，不算「丢」）
                    var a = EffectExporter.ImportClip(clips[i]);
                    if (a == null)
                    {
                        clipMiss++;
                        Debug.LogWarning(P + $"A1101 `{src.name}` / `{an.gameObject.name}` 上 `Animation` 的"
                            + $"第 {i} 条片段（`{clips[i].name}`）落不成工程 `.anim` ⇒ **这一格留空**"
                            + "（⛔ 不拿空 clip 顶上 —— 那等于静默造一个假动作）");
                        continue;
                    }
                    imported[i] = a; clipOk++;
                }
                AnimationUtility.SetAnimationClips(an, imported);
            }
            // ③ `m_Animation` 写在**数组之后**（顺序有讲究，与 `EffectExporter` 那一支同）：先让它指的那条
            //    已经在 `m_Animations` 里，再设默认值 ⇒ 不依赖「`clip` setter 会不会顺手往数组里补一条」
            //    这个**没查证的细节**。
            if (def != null)
            {
                var d = EffectExporter.ImportClip(def);
                if (d != null) an.clip = d;
            }
        }
        if (clipOk > 0 || clipMiss > 0)
            Debug.Log(P + $"A1101 `{src.name}` legacy `Animation` 片段：接回 **{clipOk}** 条"
                        + (clipMiss > 0 ? $"、**没接上 {clipMiss} 条**（见上面的告警）" : ""));

        // ---- 🆕 2026-10-09（A1109）：**uGUI 与 TMP 那两跳** ----
        // 本 `Export()` 从 `EffectExporter.Export` 抄形状时，**uGUI 与 TMP 这两族整个没接**：
        //   上面只做了「渲染器材质 / 网格 / `SpriteRenderer`·`SpriteMask` / 粒子 TSA」四跳 ——
        //   那四跳服务的是**粒子 prefab**；而**窗口 prefab 里几乎没有粒子，全是 uGUI 与 TMP**。
        // 🔴 **实测（现读两份 prefab，均纯 LF）**：
        //   · `Booster Pack Open Window.prefab`：guid 全 0 共 **350** 条 =
        //     动画 **30**（A1101 已修）+ `Image.m_Sprite` **45** + `Image.m_Material` **21**
        //     + TMP `m_fontAsset` **127** + TMP `m_sharedMaterial` **127**
        //     ⇒ **本件要接的就是后四族、合计 320 条**（简报写「299 条」—— 现读 320，见报告 §②）。
        //   · `Booster Info Popup.prefab`：同四族另有 **29** 条
        //     （`m_Sprite` 13 · `m_Material` 2 · `m_fontAsset` 7 · `m_sharedMaterial` 7）。
        //   · 那 4 个粒子 prefab 现读 `guid 0 = 0` ⇒ 下面两支对它们**是空转**（0 次迭代），正确。
        // ⚠️ **简报说「照 `EffectExporter` 里对应的那两支抄」—— 现读不成立**：`EffectExporter` 里
        //   **根本没有** uGUI `Image` 与 TMP 这两支（全库 `grep` `GetComponentsInChildren<Image>` /
        //   `TextMeshProUGUI` / `fontSharedMaterial` **命中 0**；它的 `Import*` 只有
        //   Sprite / Material / Texture / Mesh / Clip 五个）。⇒ 本件按**本函数里已有的那一支**
        //   （上面 `SpriteRenderer` / `SpriteMask` 的精灵跳）**同形**写，逐行对应见报告 §④。
        // 逐处清单（哪一族 / 原版是哪份资产 / 工程里有没有）→
        //   `资料/普查产出_第六会话/W_Booster两跳_A1109.md` §②。
        // ⚠️ **零幻觉兜底（与 A1101 同一条纪律）**：接不上就**留空 + 点名出声**，
        //   ⛔ **绝不**拿别的图/别的字体/空材质顶上 —— 那等于静默换了一个假件。

        // (1) uGUI `Image.m_Sprite` —— 与上面 `SpriteRenderer` / `SpriteMask` 那一跳**逐行同形**
        //     （同一句 `EffectExporter.ImportSprite`、同一条「导不出来就留空 + 出声」的兜底）。
        //     ⚠️ 只覆盖 `UnityEngine.UI.Image`：本件普查里持有 `m_Sprite` 的**全是它**
        //        （`RawImage.m_Texture` 的 guid-0 实测 **0 处** ⇒ 不为它写一支没判据的分支）。
        int uiSpriteOk = 0, uiSpriteMiss = 0;
        foreach (var img in inst.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            if (img.sprite == null) continue;      // 原版本来就空的那种（Open Window 的 71 个 Image 里 26 个）
            var imported = EffectExporter.ImportSprite(img.sprite);
            if (imported == null)
            {
                uiSpriteMiss++;
                Debug.LogWarning(P + $"A1109 `{src.name}` / `{img.gameObject.name}` 上的 `Image.m_Sprite`"
                    + $"（`{img.sprite.name}`）落不成工程 sprite ⇒ **这一格留空**"
                    + "（⛔ 不拿别的图顶上 —— 那等于静默画错一张）");
                continue;
            }
            img.sprite = imported; uiSpriteOk++;
        }

        // (2) uGUI `Image.m_Material`。
        //     🔴 **不能直接读 `Image.material`** —— 那是个**带回落**的 getter：
        //        `m_Material == null` 时它返回**内建默认材质**（带 alpha 分离贴图的精灵还会返回
        //        `defaultETC1GraphicMaterial`）⇒ 照它取值会把**本来没有材质**的那 50 个 Image 也算成
        //        「有材质」，然后给它们各写一份工程副本 —— **原始数据被静默改掉**（`m_Material` 由空变非空）。
        //     ⇒ 一律按**序列化字段名**读（`SerializedFieldRef`），与普查时数的就是同一个字段名。
        //     ⚠️ 这 5 份材质的原 shader 都是 **Everguild 私有 shader**（`Everguild/Card ImageUI` ·
        //        `Everguild/FX/Card Highlight And Shadow` · `Everguild/UI/Card Ready for level up` ·
        //        `Shader Graphs/Nebula`）—— `ShaderMap` 里**都没有** ⇒ 会落到 `URP/Unlit*` 的**近似替代**
        //        （`ImportMaterial` 自己会出声）。这与上面渲染器材质那一跳**是同一条既有口径**：
        //        近似就近似、但要**说出来**，不是留空。
        int uiMatOk = 0, uiMatMiss = 0;
        foreach (var img in inst.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            var om = SerializedFieldRef(img, "m_Material") as Material;
            if (om == null) continue;
            var nm = EffectExporter.ImportMaterial(om, out bool uiApprox, out string uiShader);
            if (nm == null)
            {
                uiMatMiss++;
                Debug.LogWarning(P + $"A1109 `{src.name}` / `{img.gameObject.name}` 上的 `Image.m_Material`"
                    + $"（`{om.name}` / 原 shader `{uiShader}`）导不成工程材质 ⇒ **这一格留空**"
                    + "（⛔ 不拿空材质顶上）");
                continue;
            }
            img.material = nm; uiMatOk++;
            if (uiApprox)
                Debug.LogWarning(P + $"A1109 `{src.name}` / `{img.gameObject.name}` 的 `{om.name}`"
                    + $"（原 shader `{uiShader}`）**是按近似 shader 替代的** —— 版面对、观感不是原版");
        }

        // (3) TMP 的 `m_fontAsset` + `m_sharedMaterial`（两份都数 **127** 条）。
        //     🔴 **这一族在工程里【没有目标】** —— 实测全工程只有两份 `TMP_FontAsset`
        //        （`NotoSerifCJK-Regular SDF` = 我们自己生成的 CJK 字体 · `LiberationSans SDF` = TMP 自带），
        //        而原版这两份（`Pragati-Regular SDF` ×92 · `Asar-Regular SDF` ×35）**从没被导入过**：
        //        卡面数字走 `Core/PragatiDigits` 的**位图表**（`工具/gen_pragati_digits.py`），
        //        菜单/HUD 走我们那份 CJK 字体。⇒ 今天**接不上**，**点名出声 + 留空**。
        //     ✅ 但这一支**写成「接得回来就接」**：按**资产名**到工程里找同名 `TMP_FontAsset` /
        //        同名 `Material` ⇒ 将来谁把原版那两份导进来，**这里不用改代码**就会接上。
        //        ⛔ 找不到就返回 null（留空），**绝不新建**（⛔ 更不 `CreateFontAsset` 现造一份 ——
        //        那是**我们自己画的字体**，不是原版）。
        //     ⚠️ **材质那一跳写在字体之后、且只在字体接上时才写**：`m_sharedMaterial` 就是字体图集的
        //        那份材质，两者**成对**；只挂材质不挂字体，TMP 会在 `GetPaddingForMaterial` 里对着
        //        null 字体算 ⇒ 坏得更明显。
        int tmpFontOk = 0, tmpFontMiss = 0, tmpMatOk = 0, tmpMatMiss = 0;
        var missingFonts = new Dictionary<string, int>();
        foreach (var t in inst.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            var srcFont = SerializedFieldRef(t, "m_fontAsset") as TMPro.TMP_FontAsset;
            var srcMat = SerializedFieldRef(t, "m_sharedMaterial") as Material;
            if (srcFont == null && srcMat == null) continue;

            TMPro.TMP_FontAsset f = null;
            if (srcFont != null)
            {
                f = FindProjectAsset<TMPro.TMP_FontAsset>(srcFont.name);
                if (f != null) { t.font = f; tmpFontOk++; }
                else
                {
                    tmpFontMiss++;
                    missingFonts[srcFont.name] =
                        (missingFonts.TryGetValue(srcFont.name, out int c0) ? c0 : 0) + 1;
                }
            }
            if (srcMat == null) continue;
            var pm = f != null ? FindProjectAsset<Material>(srcMat.name) : null;
            if (pm != null) { t.fontSharedMaterial = pm; tmpMatOk++; } else tmpMatMiss++;
        }

        if (uiSpriteOk > 0 || uiSpriteMiss > 0)
            Debug.Log(P + $"A1109 `{src.name}` uGUI `Image.m_Sprite`：接回 **{uiSpriteOk}** 条"
                        + (uiSpriteMiss > 0 ? $"、**没接上 {uiSpriteMiss} 条**（见上面的告警）" : ""));
        if (uiMatOk > 0 || uiMatMiss > 0)
            Debug.Log(P + $"A1109 `{src.name}` uGUI `Image.m_Material`：接回 **{uiMatOk}** 条"
                        + (uiMatMiss > 0 ? $"、**没接上 {uiMatMiss} 条**（见上面的告警）" : ""));
        if (tmpFontOk > 0 || tmpFontMiss > 0 || tmpMatOk > 0 || tmpMatMiss > 0)
        {
            var mv = new List<string>();
            foreach (var kv in missingFonts.OrderByDescending(k => k.Value))
                mv.Add($"`{kv.Key}`×{kv.Value}");
            Debug.Log(P + $"A1109 `{src.name}` TMP：字体 接回 **{tmpFontOk}** / 留空 **{tmpFontMiss}**"
                        + $" · 材质 接回 **{tmpMatOk}** / 留空 **{tmpMatMiss}**"
                        + (mv.Count > 0 ? $"（工程里没有这些 TMP_FontAsset：{string.Join(" · ", mv)}）" : ""));
        }

        var binder = inst.GetComponent<WarpforgeEffectBinder>();
        if (binder == null) binder = inst.AddComponent<WarpforgeEffectBinder>();
        binder.materials = defs.ToArray();
        binder.rendererSlots = slots.ToArray();
        binder.trailSlots = trailSlots.ToArray();
        binder.emissionOn = EffectExporter.EmissionFlagFor(src.name);
        // ⚠️ **硬写死 `""`，而本 `Export()` 里【没有 `Animator` 那一支】** —— 与 `EffectExporter.Export`
        //    的另一处形状差（那边是 `:1194-1201` 接控制器 + `:1280` 记 `origController` 给 binder）。
        //    **本件现读实测「今天无影响」**：两扇窗 + 那 4 个粒子 prefab 的 `--- !u!95`（`Animator`）**全 = 0**
        //    ⇒ 这一行现在没有消费方。🔴 **但将来把带 `Animator` 的根加进 `WindowRoots` / `CardFxRoots`**，
        //    那条控制器会**静默落成 guid 全 0**（且 binder 也拿不到名字）—— 记成待办，见报告 §⑦。
        //    **没顺手补的原因**：`EffectExporter.ImportAnimatorController` 是 **`static` 私有**
        //    （`EffectExporter.cs:2414`），复用就得动别人的已交件；自己再写一份则违 `CLAUDE.md` §三
        //    「两处写同一条规则 = 迟早不一致」。⇒ 等到真有带 `Animator` 的根时，**先把那个助手放开**
        //    再照 `EffectExporter` 同形补这一支（顺便把 `animatorController` 那一格改成记账值）。
        binder.animatorController = "";

        // 粒子那 4 件要进效果库 ⇒ 挂 `WFEffectInfo` 并给一个**原版 clip 时长**当寿命
        //（`EffectLibraryBuilder.CollectUserEffects`：`info != null ? info.destroyTime : natural`）。
        float life = -1f;
        if (asEffect)
        {
            var info = inst.GetComponent<WFEffectInfo>();
            if (info == null) info = inst.AddComponent<WFEffectInfo>();
            // `EffectName` 是**只读**的（= `displayName` 非空就取它、否则取 prefab 名）
            // ⇒ 要显式钉住就写 `displayName`。
            info.displayName = src.name;
            life = RarityFxLifetime(src.name);
            if (life > 0f) info.destroyTime = life;
            info.notes = "由 工具/… BoosterPackExporter 从 boosterpacks_assets_all 导出；寿命取同名的开卡包 clip 时长";
        }

        string path = $"{prefabDir}/{EffectExporter.Sanitize(src.name)}.prefab";
        // ⚠️ 路径固定 + `SaveAsPrefabAsset` **原地覆盖**（有就覆盖、没有才新建）——
        //    **别用 `GenerateUniqueAssetPath` / 别每次先删光**：那会每跑一次换一批 guid，
        //    而效果库（`WarpforgeEffectLibrary.asset`）记的是 prefab 引用 ⇒ 全断（CLAUDE.md §三 那条）。
        PrefabUtility.SaveAsPrefabAsset(inst, path);
        UnityEngine.Object.DestroyImmediate(inst);

        return $"材质定义{defs.Count}个 · 渲染器槽{slots.Count}个 · 原 shader: {string.Join(", ", usedShaders.OrderBy(x => x))}"
             + (approx > 0 ? $" · 近似替代 {approx} 处" : "")
             + (life > 0f ? $" · 效果寿命 {life:F3}s" : "")
             // 🆕 A1109：uGUI 与 TMP 那两跳的记账（`截` = 没接上、留空）
             + $" · uGUI 图{uiSpriteOk}/截{uiSpriteMiss} · uGUI 材{uiMatOk}/截{uiMatMiss}"
             + $" · TMP 字{tmpFontOk}/截{tmpFontMiss} · TMP 材{tmpMatOk}/截{tmpMatMiss}";
    }

    /// <summary>精确读一个**序列化字段**里的对象引用（返回 `null` = 该字段本来就没接）。
    ///
    /// 为什么要一个助手、而不直接用组件的公开属性：**uGUI 那几个 getter 带回落** ——
    ///   · `Image.material` 在 `m_Material == null` 时返回**内建默认材质**（`Graphic.defaultMaterial`），
    ///     带 alpha 分离贴图的精灵还会返回 `defaultETC1GraphicMaterial`；
    ///   · `Image.sprite` 倒是精确的（本件那一支就直接用了它）。
    /// 照那些带回落的 getter 取值 ⇒ 会把「**本来就没有材质**」的组件也算成「有」，然后给它写一份
    /// 工程副本 —— `m_Material` 由空变非空 = **静默改了原始数据**。
    ///
    /// TMP 的 `font` / `fontSharedMaterial` 本身是非回落的公开属性，本可以直用；这里也走字段名，
    /// 是为了**与普查用的是同一批字段名**（`m_fontAsset` / `m_sharedMaterial`）—— 一处口径，别两套。</summary>
    static UnityEngine.Object SerializedFieldRef(UnityEngine.Object comp, string field)
    {
        try
        {
            using (var so = new SerializedObject(comp))
            {
                var p = so.FindProperty(field);
                if (p == null) return null;
                if (p.propertyType != SerializedPropertyType.ObjectReference) return null;
                return p.objectReferenceValue;
            }
        }
        catch (Exception e)
        {
            // ⛔ 不许静默：读不出来要出声（这一格按「没有」处理，即留空 —— 与「接不上就留空」同一口径）
            Debug.LogWarning(P + $"A1109 读 `{comp.GetType().Name}.{field}` 失败（这一格按「没有」处理）："
                           + $"{e.GetType().Name}: {e.Message}");
            return null;
        }
    }

    /// <summary>按**资产名**到工程里找一份资产（`AssetDatabase.FindAssets` 的结果逐条比文件名）。
    ///
    /// 用在哪：TMP 的 `m_fontAsset` / `m_sharedMaterial` —— 那两格在包里是 **guid 全 0** 的引用，
    /// 运行时拿到的对象**不是**工程资产，接不回来；唯一能接回来的路是「工程里已经有一份同名的」
    /// ⇒ 那就是它。⛔ 找不到就返回 `null`（调用方**留空 + 出声**），**绝不新建**。
    /// 结果按 `(类型, 名字)` 缓存：一份 prefab 里同一份字体要被问上百次。</summary>
    static readonly Dictionary<string, UnityEngine.Object> ProjAssetCache
        = new Dictionary<string, UnityEngine.Object>();

    static T FindProjectAsset<T>(string name) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(name)) return null;
        string key = typeof(T).Name + "|" + name;
        if (ProjAssetCache.TryGetValue(key, out var hit)) return hit as T;
        T found = null;
        try
        {
            foreach (var g in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) != name) continue;
                found = AssetDatabase.LoadAssetAtPath<T>(p);
                if (found != null) break;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(P + $"找工程资产 `{name}`（{typeof(T).Name}）失败（按「没有」处理）："
                           + $"{e.GetType().Name}: {e.Message}");
        }
        ProjAssetCache[key] = found;
        return found;
    }

    /// <summary>粒子 prefab 的寿命 = **同名开卡包 clip 的时长**。
    /// 判据：`Booster Opening - Card Open {Normal,Rare,Legendary}` 是那条翻牌动画本身；
    /// `ContentByRarity.AnimationClip` 就是它 ⇒ 特效播完 = 动画播完。
    /// `Rarity 1`（普通档）用的是 `Booster Opening - Card Open Normal`（`contentByRarities[0/1]` 实测同 pid）。
    /// ⚠️ 这只是**给效果库的一个兜底寿命**；取不到就留给 `EffectLibraryBuilder.MeasureParticles` 去量。</summary>
    static float RarityFxLifetime(string prefabName)
    {
        string clip = prefabName == CardFxRoots[3] ? "Booster Opening - Card Open Legendary"
                    : prefabName == CardFxRoots[2] ? "Booster Opening - Card Open Rare"
                    : "Booster Opening - Card Open Normal";
        var c = FindInPacks<AnimationClip>(clip);
        return c != null ? c.length : -1f;
    }

    /// <summary>只体检、不写盘（找根 + 报名字）。用法 `-executeMethod BoosterPackExporter.Check`，筛 `^BP `。</summary>
    public static void Check()
    {
        Debug.Log(P + "=== 体检（不写盘）===");
        LoadPacks();
        int ok = 0;
        foreach (var n in WindowRoots.Concat(CardFxRoots))
        {
            var g = FindInPacks<GameObject>(n);
            Debug.Log(P + $"  {(g != null ? "✓" : "✗")} GameObject `{n}`");
            if (g != null) ok++;
        }
        foreach (var n in ClipNames)
        {
            var c = FindInPacks<AnimationClip>(n);
            Debug.Log(P + $"  {(c != null ? "✓" : "✗")} AnimationClip `{n}`" + (c != null ? $"（{c.length:F3}s）" : ""));
            if (c != null) ok++;
        }
        Debug.Log(P + $"=== 体检结束：{ok}/{WindowRoots.Length + CardFxRoots.Length + ClipNames.Length} 件找到 ===");
    }
}
