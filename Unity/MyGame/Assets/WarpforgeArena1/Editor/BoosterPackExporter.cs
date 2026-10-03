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

        var binder = inst.GetComponent<WarpforgeEffectBinder>();
        if (binder == null) binder = inst.AddComponent<WarpforgeEffectBinder>();
        binder.materials = defs.ToArray();
        binder.rendererSlots = slots.ToArray();
        binder.trailSlots = trailSlots.ToArray();
        binder.emissionOn = EffectExporter.EmissionFlagFor(src.name);
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
             + (life > 0f ? $" · 效果寿命 {life:F3}s" : "");
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
