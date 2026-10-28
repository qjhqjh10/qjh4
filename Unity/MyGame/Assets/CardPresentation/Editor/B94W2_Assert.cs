// B94W2_Assert.cs — 第九十四会话 · 代理 W2 的断言集（宿主 = `Editor/SettingsScene.cs`，由它 `RunCore` 调一次）
//
// 覆盖两件活：
//   ① **`Core/MaterialChangerByQuality.cs`**（账 `未分类/其他` :423 `Initialize()` / :424 `TryApply()`，
//      此前**全仓没有这个类**）—— 组件机制 + 原版 5 个实例的预设表；
//   ② **设置窗画质下拉 = 原版 5 档**（原来是自设计的 Unity `Mobile`/`PC` 两档）。
//
// 判据全文 → `资料/完全复刻/判据/B94_R-G_画质档位映射.md`（§二 映射链 / §四 给施工的结论，含 5 个实例实测表）。
//
// 🔴 两条自证纪律：
//   · 期望值一律写**原版字面量**（shader 名字逐字、枚举值 0..4、本地化键名），⛔ 不从被测实现里读常量；
//   · 组件那族**只断 shader 的名字**（`shader.name`），⛔ 不断 `Material` 引用相等 —— 引用相等会被
//     「自己造一份同 shader 的材质」蒙过去，而玩家看到的差别只有 shader。
// ⚠️ 组件那一族的机制断言用**两条本工程一定取得到的 shader**（`Sprites/Default` / `URP/Unlit`），
//   **不依赖原版 bundle**；原版那两个 shader 名字由 `PresetTable` 那一段当**数据**断掉。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>见文件头。入口 = <see cref="Run"/>（宿主 `Editor/SettingsScene.cs` 的 `RunCore` 调一次）。</summary>
public static class B94W2Assert
{
    public static void Run(CheckSink s, SettingsWindow win)
    {
        MenuCheck.Section(s, "B94-W2：`MaterialChangerByQuality`（账 未分类/其他 :423/:424）+ 画质下拉 = 原版 5 档");
        ComponentMechanism(s);
        PresetTable(s);
        Dropdown(s, win);
    }

    // ============================================================ ① 组件机制（合成件，不依赖战场）
    static void ComponentMechanism(CheckSink s)
    {
        var qWas = GameStaticData.graphicsQuality;
        // 用 `CreatePrimitive(Quad)` 而不是裸 `AddComponent<MeshRenderer>()`：后者**没有网格**，
        // `sharedMaterial` 的赋值行为在编辑器里不稳（会静默不生效 ⇒ 假红）。Quad 自带 MeshFilter + MeshRenderer。
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "B94W2_Probe";
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        var goNoMesh = new GameObject("B94W2_Probe_NoMesh");
        Material mat = null;
        try
        {
            var mr = go.GetComponent<MeshRenderer>();
            // 两条**本工程一定取得到**的 shader（只验机制；⛔ 不依赖 bundle 里的原版 shader）
            Shader shLow = FindFirst("Sprites/Default", "UI/Default");
            Shader shGrain = FindFirst("Unlit/Texture", "Universal Render Pipeline/Unlit", "Unlit/Color");
            MenuCheck.True(s, shLow != null && shGrain != null && shLow != shGrain,
                $"（前提）两条已知 shader 取得到且**不是同一个**（`{ShaderNameOrNone(shLow)}` / `{ShaderNameOrNone(shGrain)}`）"
              + " —— 这一族**只验机制**，原版那两个 shader 名字在下面 `PresetTable` 里当**数据**断");
            if (shLow == null || shGrain == null || shLow == shGrain) return;

            mat = new Material(shGrain) { name = "B94W2_ProbeMat" };
            mr.sharedMaterial = mat;
            MenuCheck.True(s, mr.sharedMaterial != null && mr.sharedMaterial.shader == shGrain,
                $"（前提）探针渲染器当前的 shader = `{ShaderNameOrNone(shGrain)}`");

            var c = go.AddComponent<MaterialChangerByQuality>();
            c.SetPresets(new[]
            {
                new MaterialChangerByQuality.MaterialAndShaderQualityPresets
                { quality = GameStaticData.EverguildQualitySetting.VeryLow, shader = shLow },
                new MaterialChangerByQuality.MaterialAndShaderQualityPresets
                { quality = GameStaticData.EverguildQualitySetting.Low,     shader = shLow },
                new MaterialChangerByQuality.MaterialAndShaderQualityPresets
                { quality = GameStaticData.EverguildQualitySetting.Medium,  shader = shGrain },
            });
            MenuCheck.Check(s, c.GetPresets() != null ? c.GetPresets().Length : -1, 3,
                "施工口 `SetPresets` 填进 **3 行**预设（= 原版 5 个实例实际的预设行数：0/1/2 三行）");

            // ① 命中 low 档 ⇒ 换 shader（原版走「`sharedMaterial.shader = preset.shader`」那条支）
            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.VeryLow;
            c.Initialize();
            MenuCheck.Check(s, ShaderNameOf(mr), shLow.name,
                "① 档位 = `VeryLow` ⇒ `Initialize()` 把材质 shader 换成预设 `VeryLow` 那一行的 shader"
              + "（断**名字**、不比引用；改坏法：`Initialize` 什么都不做 ⇒ 仍是 URP/Unlit ⇒ 红）");

            // ② 命中 Medium ⇒ 换成 Grainny 那一行的 shader（**从 low 换回来**，两个方向都写一次才是强断言）
            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.Medium;
            c.Initialize();
            MenuCheck.Check(s, ShaderNameOf(mr), shGrain.name,
                "② 档位 = `Medium` ⇒ 换成 `Medium` 那一行的 shader（两个方向都写一次 —— 只验一态是弱断言）");

            // ③ Ultra ⇒ **不命中 ⇒ no-op**（原版行为；⛔ 别「修好」）
            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.Ultra;
            c.Initialize();
            MenuCheck.Check(s, ShaderNameOf(mr), shGrain.name,
                "③★ 档位 = **`Ultra`**（出厂档）⇒ **什么都不做** —— shader 仍是那一份"
              + "（原版 5 个实例的预设只有 0/1/2 三行 ⇒ `4` 必然不命中；⛔ 别「修好」，那是原版行为）");

            // ④ High 同理（3 也不在表里）
            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.High;
            c.Initialize();
            MenuCheck.Check(s, ShaderNameOf(mr), shGrain.name, "④ 档位 = `High` ⇒ 同样 no-op（表里没有 3 那一行）");

            // ⑤ TryApply：质量不等 ⇒ false；相等 ⇒ true（**true 不代表真改了** —— 原版语义）
            var p0 = c.GetPresets()[0];                        // VeryLow → shLow
            MenuCheck.True(s, !p0.TryApply(GameStaticData.EverguildQualitySetting.Low, mr),
                "⑤ `TryApply(Low, …)` ⇒ **false**（原版第一句 `qualityTest != quality` 直接 return false）");
            MenuCheck.True(s, p0.TryApply(GameStaticData.EverguildQualitySetting.VeryLow, mr),
                "⑤ …`TryApply(VeryLow, …)` ⇒ **true**（匹配即 true —— ⚠️ 原版即使 `material`/`shader` 都空也返 true）");
            MenuCheck.True(s, !p0.TryApply(GameStaticData.EverguildQualitySetting.VeryLow, null),
                "⑤ …`TryApply(档, null)` ⇒ **false**（原版这里 NRE；本工程不崩宿主 ⇒ 当未命中并出声）");

            // ⑥ 没有 MeshRenderer ⇒ LogError 且报文逐字照原版
            string err = null;
            Application.LogCallback cb = (m, stack, type) => { if (type == LogType.Error && err == null) err = m; };
            Application.logMessageReceived += cb;
            try { goNoMesh.AddComponent<MaterialChangerByQuality>().Initialize(); }
            finally { Application.logMessageReceived -= cb; }
            MenuCheck.Check(s, err, "[ERROR] Trying to change the material to an object without mesh",
                "⑥ 没有 `MeshRenderer` ⇒ **LogError**，而且报文**逐字**照原版那条"
              + "（`MaterialChangerByQuality.cs` 的 `Initialize`；改坏法：改成 LogWarning ⇒ 红）");
        }
        finally
        {
            GameStaticData.graphicsQuality = qWas;             // 逐值放回（后面各节的前提）
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(goNoMesh);
            if (mat != null) Object.DestroyImmediate(mat);
        }
    }

    /// <summary>读**组件真正写进去的那一份**材质的 shader 名 —— 原版/我们的 `Apply` 在运行期写
    /// `Renderer.material`（实例）、编辑期写 `sharedMaterial`（见 `MaterialChangerByQuality.MaterialAndShaderQualityPresets.Apply`）
    /// ⇒ 这里按**同一个分叉**读，否则在 play mode 下会读到旧值（假红）。</summary>
    static string ShaderNameOf(Renderer r)
    {
        if (r == null) return "<null renderer>";
        var m = Application.isPlaying ? r.material : r.sharedMaterial;
        return m != null && m.shader != null ? m.shader.name : "<null shader>";
    }

    /// <summary>按顺序取第一个取得到的 shader（⛔ 不依赖 bundle；这一族只验机制）。</summary>
    static Shader FindFirst(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            var sh = Shader.Find(names[i]);
            if (sh != null) return sh;
        }
        return null;
    }

    static string ShaderNameOrNone(Shader sh) { return sh != null ? sh.name : "<取不到>"; }

    // ============================================================ ② 原版 5 个实例的预设表（纯数据，不碰 bundle）
    static void PresetTable(CheckSink s)
    {
        var t = MaterialChangerByQuality.ArenaPresetTable.ArenaEntries;
        var sh = MaterialChangerByQuality.ArenaPresetTable.SharedBundleEntries;
        MenuCheck.Check(s, t.Length + sh.Length, 5,
            "预设表合计 **5 条** = 原版实测的 5 个实例（3 条已确认归属 + 2 条在共用包里、归属**未查清**）"
          + "｜判据 → `B94_R-G_画质档位映射.md` §4.1 那张表");

        var low = MaterialChangerByQuality.ArenaPresetTable.BuildPresets(false);
        MenuCheck.Check(s, low.Length, 3,
            "普通版预设 = **3 行**（`0 VeryLow` / `1 Low` / `2 Medium`；原版 5 个实例逐条实测都只有这三行）");
        MenuCheck.Check(s, low[0].shaderName, "Everguild/Misc/Unlit shadows receiver",
            "…`VeryLow` 那一行 = `Everguild/Misc/Unlit shadows receiver`（逐字照原版 `m_Name`）");
        MenuCheck.Check(s, low[1].shaderName, "Everguild/Misc/Unlit shadows receiver", "…`Low` 那一行 = 同一个 shader");
        MenuCheck.Check(s, low[2].shaderName, "Everguild/FX/Floor Planar Reflections Grainny",
            "…`Medium` 那一行 = `Everguild/FX/Floor Planar Reflections Grainny`");
        MenuCheck.Check(s, low[0].quality, GameStaticData.EverguildQualitySetting.VeryLow,
            "…三行的 `quality` 依次是 0 / 1 / 2（枚举值，不是 Unity 档号）");
        MenuCheck.True(s, low[0].material == null && low[1].material == null && low[2].material == null,
            "…三行的 `material` **全 null**（原版实测：5 个实例都只填 shader ⇒ 走「换 `sharedMaterial.shader`」那条支）");

        var vc = MaterialChangerByQuality.ArenaPresetTable.BuildPresets(true);
        MenuCheck.Check(s, vc[0].shaderName, "Everguild/Misc/Unlit shadows receiver Vertex Color Shadow Mask",
            "顶点色版 low = `…Unlit shadows receiver Vertex Color Shadow Mask`（`battlearenatauviorla` 的 `Floor`）");
        MenuCheck.Check(s, vc[2].shaderName, "Everguild/FX/Floor Planar Reflections Grainny Vertex color shadow mask",
            "顶点色版 Medium = `…Grainny Vertex color shadow mask`（⚠️ 原版这个名字里 `color` 是**小写**，逐字照抄）");

        MaterialChangerByQuality.ArenaPresetTable.Entry e;
        MenuCheck.True(s, MaterialChangerByQuality.ArenaPresetTable.TryMatch("battlearena3", "Ground", out e)
                       && !e.VertexColor, "登记：`battlearena3` 的 `Ground`（普通版）");
        MenuCheck.True(s, MaterialChangerByQuality.ArenaPresetTable.TryMatch("battlearenaaeldari", "Floor (1)", out e)
                       && !e.VertexColor, "登记：`battlearenaaeldari` 的 `Floor (1)`（普通版）");
        MenuCheck.True(s, MaterialChangerByQuality.ArenaPresetTable.TryMatch("battlearenatauviorla", "Floor", out e)
                       && e.VertexColor, "登记：`battlearenatauviorla` 的 `Floor`（**唯一**用顶点色版的那一份）");
        MenuCheck.True(s, !MaterialChangerByQuality.ArenaPresetTable.TryMatch("battlearena2", "Floor", out e),
            "反例：`battlearena2` 的 `Floor` **不在**表里 —— 各场自己的 `Floor` 是**本场**的 MeshRenderer，"
          + "不是共用包里那两份（⛔ 别按「场里有同名对象」猜归属，见 `SharedBundleEntries` 的注释）");
        MenuCheck.True(s, !MaterialChangerByQuality.ArenaPresetTable.TryMatch("battlearena3", "Floor", out e),
            "反例：`battlearena3` 的 `Floor` 不在表里（它那一场的实例挂在 **`Ground`** 上，与名字对不上就是没命中）");
    }

    // ============================================================ ③ 画质下拉 = 原版 5 档
    static void Dropdown(CheckSink s, SettingsWindow win)
    {
        if (win == null)
        {
            MenuCheck.True(s, false, "（前提）设置窗实例拿不到 ⇒ 画质下拉这一族无法断");
            return;
        }
        var qWas = GameStaticData.graphicsQuality;
        bool dirtyWas = SettingsWindow.ApplyGraphicsChangesOnClose;
        try
        {
            // 原版 `GraphicsTab__OnSetup` 建选项用的就是这一串键（`Enum.GetValues` + `"Settings/Graphics/" + 名`）
            var names = new[] { "VeryLow", "Low", "Medium", "High", "Ultra" };
            int missing = 0; string firstMissing = null;
            for (int i = 0; i < names.Length; i++)
            {
                string k = "Settings/Graphics/" + names[i];
                string v = Loc.T(k);
                // 🔴 **判据是 `Loc.T` 不是 `Loc.HasEntry`**：这 5 条**在官方全表里、不在手写 `Table` 里**
                //    ⇒ `Loc.HasEntry` 返 false（它只看手写表），而 `Loc.T` 的三档兜底取得到真值。
                //    写 `HasEntry` 会得到一条**假红**，还会诱导别人往手写表里抄一份（两份迟早不一致）。
                if (string.IsNullOrEmpty(v) || v == k) { missing++; if (firstMissing == null) firstMissing = k; }
            }
            MenuCheck.True(s, missing == 0,
                $"五档的本地化文案都取得到（`Settings/Graphics/{{VeryLow…Ultra}}`；取不到 {missing} 条"
              + (firstMissing == null ? "" : $"，第一条 `{firstMissing}`") + "；取不到 ⇒ 界面会印键名 = 静默失败）");

            // 🔴 前提：原版 `GraphicsQualityDropdownChange.c:45` 只在「选中档 ≠ 当前档」时置脏位
            //    ⇒ 不先换一个值，选第 0 行（`VeryLow`）等于「没变」，脏位当然不置（= 假红，不是实现缺陷）。
            //    注入后 `High(3) → VeryLow(0)` 是真的变了 ⇒ 下面 `:247/:250/:253` 三条断言一个字都不用改。
            GameStaticData.graphicsQuality = GameStaticData.EverguildQualitySetting.High;
            MenuCheck.True(s, (int)GameStaticData.graphicsQuality != 0,
                "（前提）进本轮时当前档 ≠ `VeryLow`（否则选第 0 行不会置脏位 ⇒ 下面那条「脏位」就成了假红）");

            win.OpenTab(SettingsTab.Graphics);
            var hit = MenuCheck.FindChild(win.transform, "QualityHit");
            MenuCheck.True(s, hit != null,
                "（前提）图像页的 `QualityHit` 命中区在（原版 `Quality Selector > Quality DropDown` 那一颗）");
            if (hit != null) InvokeClick(hit);
            MenuCheck.True(s, win.DropdownOpen(SettingsWindow.DdQuality),
                "点画质框 ⇒ 列表打开（原版 `TMP_Dropdown.Show()` 的等价物）");

            MenuCheck.Check(s, win.DropdownRowCount(SettingsWindow.DdQuality), 5,
                "★★ 列表 **5 行** = 原版 `EverguildQualitySetting` 五个值（改坏法：退回 Unity 的 "
              + "`QualitySettings.names`（本工程 `Mobile`/`PC` 两档）⇒ 只 2 行 ⇒ 红）");
            for (int i = 0; i < names.Length; i++)
            {
                var row = win.DropdownRowHit(SettingsWindow.DdQuality, i);
                MenuCheck.True(s, row != null, $"第 {i} 行（`{names[i]}`）存在");
                if (row == null) continue;
                var lbT = MenuCheck.FindChild(row.parent, "Item Label");
                var lb = lbT != null ? lbT.GetComponent<Label>() : null;
                MenuCheck.Check(s, lb != null ? lb.Text : "<null>", Loc.T("Settings/Graphics/" + names[i]),
                    $"第 {i} 行的字 = `Loc.T(\"Settings/Graphics/{names[i]}\")` —— 顺序 = 枚举值序（VeryLow → Ultra）"
                  + "（⛔ 别把行序排错：原版 `Enum.GetValues` 就是这个序）");
            }
            MenuCheck.True(s, win.DropdownRowHit(SettingsWindow.DdQuality, 5) == null,
                "…没有第 6 行（⛔ 别把 Unity 那两档混进来）");

            // 选第 0 行 ⇒ 写 `graphicsQuality` + 置「关窗才生效」脏位 + caption 换字
            var row0 = win.DropdownRowHit(SettingsWindow.DdQuality, 0);
            MenuCheck.True(s, row0 != null, "（前提）第 0 行的命中区在");
            if (row0 != null) InvokeClick(row0);
            MenuCheck.Check(s, (int)GameStaticData.graphicsQuality, 0,
                "选第 0 行 ⇒ `GameStaticData.graphicsQuality`(+0x120) = **`VeryLow`(0)**"
              + "（原版 `GraphicsQualityDropdownChange` 那一句；改坏法：只改 UI 不写字段 ⇒ 红）");
            MenuCheck.True(s, SettingsWindow.ApplyGraphicsChangesOnClose,
                "…并且置了 **「关窗才生效」脏位**（原版 `SettingsMenu.ApplyGraphicsChangesOnClose`；"
              + "改坏法：选中就当场应用 ⇒ 这条红）");
            MenuCheck.Check(s, win.QualityLabel != null ? win.QualityLabel.Text : "<null>",
                Loc.T("Settings/Graphics/VeryLow"),
                "…下拉框 caption 换成新档的**本地化文案**（原版那颗 caption 就是选中项的 label 文本）");
            MenuCheck.True(s, !SuperSampling.RowVisible,
                "…`Use super sampling` 那一行**随之藏起来**（`VeryLow` 的 `allowSuperSampling` = 0；"
              + "原版 `ConfigureSuperSamplingVisibility` 只看这个字段 ∧ 平台）");
        }
        finally
        {
            GameStaticData.graphicsQuality = qWas;             // 逐值放回
            SettingsWindow.ApplyGraphicsChangesOnClose = dirtyWas;
            if (win != null) win.RebuildGfxRows();
        }
    }

    /// <summary>点一下：等价于宿主那套 `Click(Transform)`（取 `WindowButton.onClick` 直接调）。</summary>
    static void InvokeClick(Transform t)
    {
        if (t == null) return;
        var b = t.GetComponentInChildren<WindowButton>();
        if (b != null && b.onClick != null) b.onClick();
    }
}
