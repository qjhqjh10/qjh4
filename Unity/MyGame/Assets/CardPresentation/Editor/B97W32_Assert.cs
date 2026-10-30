// B97W32_Assert.cs — 第九十七会话 · 波 1 补位 · 执行代理 `B97_W32`（工具 / 资源线）
//
// ============================ 它盯的是什么 ============================
// 一件活：`B97_W23` §五·5.1 记下的那个缺口 —— `wf_boosters_extra.bundle` 里 **299 个
// `ParticleSystemRenderer` / 200 个材质槽** 指向的 16 个材质**根本不在包里**（它们的 PPtr 是
// `m_FileID 4..7 → archive:/CAB-…`，原版是 Addressables 的**依赖包**），另有 **8 个包内材质
// 的 `m_Shader` 悬空**。⇒ 开卡包「爆散/翻牌」那一半粒子会缺材质或渲错。
//
// 修法（本件已落地，改动只在工具 + 产物，⛔ 没动任何业务 `.cs`）：
//   `工具/extract_missing_shaders.py --booster-mats`（`--prefabs` 也会走同一步）：
//   ① 从 4 个源包把 16 个材质（+ 贴图）抽成独立内层 CAB；② 从 2 个源包抽 4 个 shader；
//   ③ 并进**同一个产物**并把引用改写成「包内兄弟 CAB」。
//
// 判据（⛔ 别重新设计）→ `资料/完全复刻/报告/B97_W32.md` §二/§三：
//   · 16 个材质在原版**都在**（分属 duplicateassetisolation / battlesharedresources /
//     menusharedresources / fonts 四个包）⇒ 不是「原版本身没有」；
//   · 改前实测：材质槽 **200 悬空** · 材质→shader **8 悬空**；改后 **0 / 0**（同一统计脚本）。
//
// 🔴🔴 **2026-10-30（`B103_W7`）：第 4 段（贴图槽）就地重写 —— 原来那条尺子是错的。**
//   旧判据 `texNull == 0` 要求「**shader 声明的每一个贴图槽都绑了图**」，而原版材质本身就留空
//   1057 个（`unity_Lightmaps*` / `unity_ShadowMasks` 879 + TMP 四个 136 + URP 粒子两项 42）
//   ⇒ 那条断言**在原版身上也不可达**。实读 1248 个空槽 = 1057（原版本来空）+ **191（真悬空）**。
//   191 的来源 = **`material → texture` 这一层从来没被闭合过**（`BOOSTER_IMPORTS` 原来只有
//   material / shader 两类）⇒ `B103_W7` 已在打包侧补上（搬 `ring_warp`/`Glow`/`drops_cloud`/
//   `Default-Particle` 四张 + 改写 7 条 `m_TexEnvs`），本文件改成
//   **「空槽集合 ⊆ 原版材质同样为空的允许清单」**（表见类尾 `AllowedEmptyTextureProps`，107 对，逐条带出处）。
//
// ============================ 怎么跑（⚠️ 本代理无权跑 Unity，本轮未跑）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B97W32Assert.Run -logFile "d:/4/_tmp_view/b97w32.log"
//   判绿 = 小结那行 `✗` 为 0。
//   ⚠️ 与 `B97W23Assert` / `B97W29Assert` **互补、不重叠**：W23 第 6 段**只是打印**缺口的量
//      （不计入红）；本支把它变成**硬断言**（改前 200/8 → 改后必须 0），并补「容器/prefab 没被并坏」。
//      ⛔ 别再往本类里抄一遍 SetPass / 白名单 / 克隆材质那些断言。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · 把 `merge_imports_into_bundle(...)` 那一步跳过（只跑 `repack_tree`）⇒ 第 3 段大量 `null`
//     （实测会回到 200 个空槽）。
//   · 把 `BOOSTER_IMPORTS` 里两条 `kind="shader"` 删掉 ⇒ 第 4 段的「shader 不是 InternalErrorShader」
//     大量红（材质搬进来了、shader 仍指 `archive:/CAB-805d8e…`）。
//   · 把导入 CAB 的名字改回源包名（`CAB-805d8e…` 等）⇒ 与运行时已加载的 `wf_shaders.bundle`
//     内层同名 ⇒ Unity 判「同一个包」**整包拒收** ⇒ 第 1 段（能不能按容器键取 prefab）直接红。
//   · 把产物换回 `--booster-mats` 之前那一份（备份在 `_tmp_view/b97w32/backup/`，5,972,456 B）
//     ⇒ 第 1 段的字节数红 + 第 3 段 200 个空槽红。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CardPresentation;

public static class B97W32Assert
{
    const string P = "B97W32 ";

    const string BundleRelPath = "WarpforgeVFX/wf_boosters_extra.bundle";

    /// <summary>`--booster-mats` 之后主产物的实测字节数（2026-10-30 分包形状现读 = 5,974,300 B，
    /// 与 `StreamingAssets/WarpforgeVFX/wf_boosters_extra.bundle` 的 `FileInfo.Length` 一致；材质/shader 拆成 6 只依赖包后从 7,578,817 缩回）。
    /// ⚠️ 改前是 **5,972,456 B**（复现：`python -I -X utf8 工具/extract_missing_shaders.py --booster-mats --check`）。</summary>
    const long ExpectedBundleBytes = 5974300L;

    /// <summary>17 棵 prefab 的容器键（`m_Container` 38 条 = 19 资产 × 2 种写法 ⇒ 17 prefab + 2 clip）。
    /// 并完外部 CAB 之后这两张表**必须原样**（⛔ 别把材质也登记进容器：W23 §4.3 第 2 条钉的是
    /// `LoadAllAssets<Shader>()` 在这里仍然取不到东西）。</summary>
    const int ExpectedPrefabCount = 17;

    /// <summary>16 个「原本整个不在包里」的材质名（逐字，来自 `--check` 的 [P1] 行）。
    /// 改后它们应当在 prefab 的渲染器上**按名字**出现。</summary>
    static readonly string[] ImportedMaterialNames = new string[]
    {
        "Spark Additive Extra Color", "ParticlesUnlit", "Glow Rays Additive Extra Color",
        "Smoke Sprite Sheet Extra Blend", "smoke_light", "Crosshair_2_Thin", "Sororitas_Icon",
        "Hexagon_tiled", "Smoke5_extra color", "Crosshair Ring Extra", "levelup_ring",
        "Shine trail Additive Extra", "Smoke Sprite Sheet Extra Additive", "Waterfall_Clouds",
        "Aura_2", "Pragati-Regular Atlas Material",
    };

    static int _ok, _bad;

    static void Check(bool cond, string label)
    {
        if (cond) { _ok++; } else { _bad++; }
        Debug.Log(P + (cond ? "✓ " : "✗ ") + label);
    }

    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(P + "=== 卡包 17 棵 prefab「粒子材质并进产物」自检 开始 ===");

        // ---------------------------------------------------------------- 1 · 产物与容器
        var path = Path.Combine(Application.streamingAssetsPath, BundleRelPath);
        bool exists = File.Exists(path);
        Check(exists, "产物包在盘上：" + path);
        if (exists)
        {
            long size = 0;
            try { size = new FileInfo(path).Length; }
            catch (Exception e) { Debug.Log(P + "  取文件大小抛异常：" + e.Message); }
            Check(size == ExpectedBundleBytes,
                  "产物字节数 == " + ExpectedBundleBytes + "（期望改后那一份；改前是 5,972,456）—— 实得 " + size);
        }

        bool loaded = BoosterPackAssets.TryEnsureLoaded();
        Check(loaded, "包能被加载（判定 = 按容器键取得到 `Booster Pack Standard`）");

        // 17 棵 prefab 按容器键取回 —— 逐棵那个断言在 `B97W23Assert` 第 3 段已经钉过，
        // 这里只要**计数**（它同时挡「内层 CAB 撞名 ⇒ 整包被拒收」：那种情况下一个都取不到）。
        var prefabs = new List<GameObject>();
        for (int i = 0; i < BoosterPackAssets.PrefabNames.Length; i++)
        {
            GameObject pf;
            if (BoosterPackAssets.TryGetPrefab(BoosterPackAssets.PrefabNames[i], out pf) && pf != null)
                prefabs.Add(pf);
        }
        Check(prefabs.Count == ExpectedPrefabCount,
              "17 棵 prefab 全部按容器键取回（实得 " + prefabs.Count + "；0 = 整包被拒收）");

        // ---------------------------------------------------------------- 2 · 渲染器材质槽：一个都不许是 null
        int renderers = 0, slots = 0, nullSlots = 0, badShader = 0;
        var seenMatNames = new HashSet<string>();
        for (int i = 0; i < prefabs.Count; i++)
        {
            var rs = prefabs[i].GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rs.Length; r++)
            {
                renderers++;
                var ms = rs[r].sharedMaterials;
                for (int m = 0; m < ms.Length; m++)
                {
                    slots++;
                    var mat = ms[m];
                    if (mat == null) { nullSlots++; continue; }
                    if (mat.name != null) seenMatNames.Add(mat.name);
                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader") badShader++;
                }
            }
        }
        Debug.Log(P + "渲染器 " + renderers + " 个 · 材质槽 " + slots + " 个 · 空槽 " + nullSlots + " · shader 坏 " + badShader);
        // 🔴 核心两条：改前实测 200 / 8。⛔ 别放宽成「少于多少」——原版这两个数就是 0。
        Check(nullSlots == 0, "材质槽**没有一个**是 null（改前实测 200 个 —— 这就是本件修的那个缺口）");
        Check(badShader == 0, "材质 shader 全部可取且不是 `Hidden/InternalErrorShader`（改前实测 8 个坏）");

        // ---------------------------------------------------------------- 3 · 那 16 个材质确实在树上
        int missing = 0;
        for (int i = 0; i < ImportedMaterialNames.Length; i++)
        {
            bool hit = seenMatNames.Contains(ImportedMaterialNames[i]);
            if (!hit) { missing++; Debug.Log(P + "  ✗ 树上没有材质：" + ImportedMaterialNames[i]); }
        }
        Check(missing == 0, "16 个「原本不在包里」的材质全部出现在渲染器上（缺 " + missing + "）");

        // ---------------------------------------------------------------- 4 · 贴图槽
        // 🔴 **2026-10-30（`B103_W7`）就地重写这一段**：原来那条 `texNull == 0` 是**一把错尺子**
        //    —— `Material.GetTexturePropertyNames()` 给的是 **shader 声明的全部贴图属性**，
        //    而原版材质本身就没绑的槽（`unity_Lightmaps*` / `unity_ShadowMasks` / TMP 的四个 /
        //    URP 粒子的 `_BumpMap`/`_EmissionMap`）**永远为空** ⇒ 那条断言在原版身上都不可达。
        //    实读 1248 个空槽 = **1057 个「原版本来就空」+ 191 个真悬空**（诊断 → `报告/B103_D1.md` §1）。
        //    191 个已由 `B103_W7` 修掉（搬 4 张贴图进依赖包 + 改写 7 条 `m_TexEnvs`）；
        //    现在这条断言改成 **「空槽集合 ⊆ 原版同样为空的允许清单」**（表见类尾 `AllowedEmptyTextureProps`）。
        // ⚠️ 本段沿用 `sharedMaterial`（只取第 1 个材质）—— 第 2 段走的是 `sharedMaterials`，两者口径不同
        //    （`报告/B103_D1.md` 附录 A5）。本件**没改这个口径**：改了会让「191 / 1057」这两个已对账的
        //    基线一起变，等于把这次修复的验收坐标挪走。要统一请单独一轮做（调度台决定）。
        int texSlots = 0, texNull = 0, texNullAllowed = 0, texNullUnexplained = 0;
        var pairCount = new Dictionary<string, int>();
        var pairAllowed = new Dictionary<string, bool>();
        for (int i = 0; i < prefabs.Count; i++)
        {
            var rs = prefabs[i].GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rs.Length; r++)
            {
                var m = rs[r].sharedMaterial;
                if (m == null) continue;
                string[] names = m.GetTexturePropertyNames();
                for (int t = 0; t < names.Length; t++)
                {
                    texSlots++;
                    var tex = m.GetTexture(names[t]);
                    if (tex != null) continue;
                    texNull++;
                    string key = m.name + "." + names[t];
                    int c;
                    pairCount.TryGetValue(key, out c);
                    pairCount[key] = c + 1;
                    bool ok;
                    if (!pairAllowed.TryGetValue(key, out ok))
                    {
                        ok = IsAllowedEmptyTexture(m.name, names[t]);
                        pairAllowed[key] = ok;
                    }
                    if (ok) texNullAllowed++;
                    else { texNullUnexplained++; }
                }
            }
        }
        Debug.Log(P + "贴图槽 " + texSlots + " 个 · 空 " + texNull + " 个（原版同样为空 " + texNullAllowed
                  + " · 🔴 原版有值却空着 " + texNullUnexplained + "）");
        var keys = new List<string>(pairCount.Keys);
        keys.Sort(StringComparer.Ordinal);
        for (int i = 0; i < keys.Count; i++)
        {
            bool ok = pairAllowed[keys[i]];
            Debug.Log(P + "  空贴图槽：" + keys[i] + " ×" + pairCount[keys[i]]
                      + (ok ? "（原版同样为空 ✔）" : "（🔴 原版有值 ⇒ 真回归）"));
        }
        // 🔴 **非空转**：`texSlots > 0` 一起断 —— 包被拒收 / 一棵 prefab 都没取回时
        //    `pairCount` 是空的、`texNullUnexplained` 恒 0，上面那条会**空转绿**（本仓踩过 3 次的坑）。
        Check(texSlots > 0 && texNullUnexplained == 0,
              "空贴图槽集合 ⊆【原版材质同样为空】的允许清单（贴图槽 " + texSlots + " 个；实测 "
              + pairCount.Count + " 种 (材质,属性) 对；不允许的 " + texNullUnexplained + " 个槽 —— 有就说明贴图又悬空了）");
        // 🔴 灭自证：单点钉住「允许清单里没有 `_MainTex` / `_BaseMap`」——
        //    允许清单是按 (材质名, 属性名) 匹配的，如果有人手滑把这两条写进去，
        //    上面那条会变绿而这里会红（原版这 7 条**全都有值**，它们才是本件修的东西）。
        Check(!AllowListMentions("_MainTex") && !AllowListMentions("_BaseMap"),
              "允许清单里**没有** `_MainTex` / `_BaseMap`（原版这 7 条全都有值 —— 它们才是本件修的东西）");

        // ---------------------------------------------------------------- 5 · 容器/预载表的钉子仍然成立
        AssetBundle bundle;
        if (BoosterPackAssets.TryGetBundle(out bundle) && bundle != null)
        {
            int byLoadAll = -1;
            try
            {
                var all = bundle.LoadAllAssets<Shader>();
                byLoadAll = all == null ? -1 : all.Length;
            }
            catch (Exception e) { Debug.Log(P + "  `LoadAllAssets<Shader>()` 抛异常：" + e.Message); }
            Debug.Log(P + "【判据钉子】本包 `LoadAllAssets<Shader>()` = " + byLoadAll
                      + "（设计上**必须是 0**：6 个 shader 只在次内层 CAB 里、不进容器）");
            // 🔴 本件**特意没**把材质/shader 登记进容器：容器仍是 38 条 = 17 prefab + 2 clip ×2 种写法。
            //    万一这条变成非 0，说明有人把 shader 也登记进容器了 ⇒ W23 §4.3 第 2 条的判据要跟着更新。
            Check(byLoadAll == 0, "`LoadAllAssets<Shader>()` 仍然是 0（W23 §4.3 第 2 条那条钉子）");
        }
        else
        {
            Check(false, "拿得到 AssetBundle（`TryGetBundle`）");
        }

        Debug.Log(P + "=== 小结：" + _ok + " 通过 / " + _bad + " 失败 ===");
    }

    // 🔴 2026-10-30（第一百零二会话 · 调度台）：本件此前**全仓 0 宿主**（从没被执行过）。接进 `ShellScene` 时需要
    //   两个只读口把计数并进宿主的 `_sink`，否则它的 ✗ 只印不计（= 静默）。追加在类尾 ⇒ 本文件既有账落点零漂移。
    public static int OkCount { get { return _ok; } }
    public static int BadCount { get { return _bad; } }

    // ================================================================ 允许为空的贴图槽清单（`B103_W7` · 2026-10-30）
    // 判据 = **原版材质 JSON** 的 `m_SavedProperties.m_TexEnvs` 里同名项的
    //        `second.m_Texture == { m_FileID: 0, m_PathID: 0 }` ⇒ 「shader 声明了、原版就没绑」。
    // 出处 = `d:/2/新解包资源/assets_full/bundle_<包>/Material/Material_<pid>.json`（每行的注释就是这一对）。
    // 为什么需要这张表：`Material.GetTexturePropertyNames()` 返回的是 **shader 声明的全部槽**，不是
    //   「材质真绑了的」—— 拿它断「一个都不许空」等于要求原版做它没做的事（诊断 → `报告/B103_D1.md` §1.2）。
    // 🔴 **⛔ 别把 `_MainTex` / `_BaseMap` 那 7 条塞进来** —— 原版全都有值，它们是 `B103_W7` 修掉的真缺口
    //    （191 个运行时槽）；再空 = 真回归，第 4 段会红。
    // 规模：**107 个 (材质,属性) 对**；按运行时槽数摊开 = **1057**（`unity_*` 879 + TMP 136 + URP 42）。
    //       —— 这与 `ShellScene` 的实读「贴图槽 1459 · 空 1248（= 1057 + 191）」逐数对得上。
    static readonly string[] Unity3Props =
        { "unity_Lightmaps", "unity_LightmapsInd", "unity_ShadowMasks" };
    static readonly string[] Tmp4Props =
        { "_BumpMap", "_Cube", "_FaceTex", "_OutlineTex" };
    static readonly string[] UrpParticle2Props =
        { "_BumpMap", "_EmissionMap" };

    static readonly Dictionary<string, string[]> AllowedEmptyTextureProps = new Dictionary<string, string[]>
    {
        // ---- 组 A（31 个材质 × 3 个 `unity_*` 槽）：原版该三项都存在、`m_Texture = {0,0}` ----
        //    `Everguild/FX/Extra Color` / `Everguild/FX/Alpha Mask One Layer` / 卡包外观 shader 都声明这三项，
        //    Unity 只有在**烘焙过的光照贴图场景**里才会去填；原版材质里全是 {0,0}。
        { "Glow Rays Additive Extra Color UI", Unity3Props },      // boosterpacks_assets_all/Material_7108491343267576609.json
        { "Embers Extra Color", Unity3Props },                     // boosterpacks_assets_all/Material_-2237545402817537697.json
        { "Spark Additive Extra Color", Unity3Props },             // duplicateassetisolation_assets_all/Material_-8031580538284717323.json
        { "Ring_Warped_extra color UI", Unity3Props },             // boosterpacks_assets_all/Material_-6836153499158306411.json
        { "Glow Rays Additive Extra Color", Unity3Props },         // duplicateassetisolation_assets_all/Material_-3866995267438876787.json
        { "Glow Additive Extra Color UI", Unity3Props },           // boosterpacks_assets_all/Material_-388003590706267446.json
        { "Aura_2", Unity3Props },                                 // menusharedresources_assets_all/Material_1954984720533256764.json
        { "Smoke Sprite Sheet Extra Additive", Unity3Props },      // battlesharedresources_assets_all/Material_-5076812091876950191.json
        { "Smoke Sprite Sheet Extra Blend", Unity3Props },         // duplicateassetisolation_assets_all/Material_-6711203846430893277.json
        { "SmallDots Add", Unity3Props },                          // boosterpacks_assets_all/Material_-7172907109476874863.json
        { "Sororitas_Icon", Unity3Props },                         // duplicateassetisolation_assets_all/Material_2386124791906240890.json
        { "Smoke5_extra color", Unity3Props },                     // duplicateassetisolation_assets_all/Material_-8460820609251426512.json
        { "Hexagon_tiled", Unity3Props },                          // duplicateassetisolation_assets_all/Material_-8975087252871707060.json
        { "Crosshair_2_Thin", Unity3Props },                       // duplicateassetisolation_assets_all/Material_-8848298278991051372.json
        { "Crosshair Ring Extra", Unity3Props },                   // duplicateassetisolation_assets_all/Material_5733879997349476095.json
        //   16 个「卡包外观」材质（shader `Everguild/Cards/Shader Graphs/Booster Pack`）—— 同样三项 {0,0}
        { "Card Booster Pack Standard", Unity3Props },             // boosterpacks_assets_all/Material_3143908562808481385.json
        { "Card Booster Pack Tyranids Leviathan", Unity3Props },   // boosterpacks_assets_all/Material_-5379894290471149958.json
        { "Card Booster Pack Tau Empire", Unity3Props },           // boosterpacks_assets_all/Material_-1038931975123322646.json
        { "Card Booster Pack Space Marines Ultramarines", Unity3Props },               // Material_2781682559873082159.json
        { "Card Booster Pack Space Marines Ultramarines Expansion 1", Unity3Props },   // Material_8637464849487199629.json
        { "Card Booster Pack Space Marines Space Wolves", Unity3Props },               // Material_-1005157760323407856.json
        { "Card Booster Pack Space Marines Dark Angels", Unity3Props },                // Material_2997788394506672207.json
        { "Card Booster Pack Sororitas", Unity3Props },            // boosterpacks_assets_all/Material_-6395674202644108821.json
        { "Card Booster Pack Orks Goff", Unity3Props },            // boosterpacks_assets_all/Material_5985970350592098103.json
        { "Card Booster Pack Orks Goff Expansion", Unity3Props },  // boosterpacks_assets_all/Material_8749820902701657744.json
        { "Card Booster Pack Necrons Sautekh", Unity3Props },      // boosterpacks_assets_all/Material_-2518919678889035147.json
        { "Card Booster Pack Genestealers", Unity3Props },         // boosterpacks_assets_all/Material_7922706269947121124.json
        { "Card Booster Pack Emperors Children", Unity3Props },    // boosterpacks_assets_all/Material_-1814310203973018340.json
        { "Card Booster Pack ChaosSM Black Legion", Unity3Props }, // boosterpacks_assets_all/Material_-6578417608764191686.json
        { "Card Booster Pack Astra Militarum", Unity3Props },      // boosterpacks_assets_all/Material_-3363119327302140448.json
        { "Card Booster Pack Aledari SaimHann", Unity3Props },     // boosterpacks_assets_all/Material_87272187708828613.json
        // ---- 组 B（TMP 字体图集）：`TextMeshPro/Distance Field` 声明四项、原版没绑 ----
        { "Pragati-Regular Atlas Material", Tmp4Props },           // fonts_assets_all/Material_-5050103597772954521.json
        // ---- 组 C（URP 粒子）：`Universal Render Pipeline/Particles/Unlit` 声明两项、原版没绑 ----
        { "ParticlesUnlit", UrpParticle2Props },                   // duplicateassetisolation_assets_all/Material_6242070269927333232.json
        { "levelup_ring", UrpParticle2Props },                     // duplicateassetisolation_assets_all/Material_-1981429699457920947.json
        { "Waterfall_Noise", UrpParticle2Props },                  // boosterpacks_assets_all/Material_8966088880632587443.json
        { "Waterfall_Clouds", UrpParticle2Props },                 // battlesharedresources_assets_all/Material_-986066795884241450.json
        { "SandParticle_add", UrpParticle2Props },                 // boosterpacks_assets_all/Material_1009379366977535475.json
    };

    static bool IsAllowedEmptyTexture(string material, string prop)
    {
        string[] props;
        return AllowedEmptyTextureProps.TryGetValue(material, out props) && Array.IndexOf(props, prop) >= 0;
    }

    /// <summary>允许清单里**任何一条**是否放行这个属性名（第 4 段那条灭自证用）。</summary>
    static bool AllowListMentions(string prop)
    {
        foreach (var kv in AllowedEmptyTextureProps)
            if (Array.IndexOf(kv.Value, prop) >= 0) return true;
        return false;
    }
}
