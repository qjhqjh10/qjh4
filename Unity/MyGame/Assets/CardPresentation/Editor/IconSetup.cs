// IconSetup.cs — 把「卡面效果文字里的图标」做成能在 TMP 里行内渲染的资产
//
// **为什么需要它**：卡面那些图标（⟦拳⟧ 近战 / ⟦枪⟧ 远程 / ⟦太阳⟧ 信仰 / ⟦绿圈1⟧ 灵魂石…）
// 在原版里**不是**分开的 Image，而是 **TMP 的行内 sprite** —— 文本里写
// `<sprite name=Atlas_trait_icon_Melee>`（实证：`bundle_menus_assets_all` 里 14 个组件
// 的 `m_text` 就是这个写法）。TMP 要渲染它，必须有一个 `TMP_SpriteAsset`，
// 而**我们工程里一个都没有** —— 这就是「素材没准备好」里最要紧的那一件。
//
// **参数全部照抄原版**（出处：解包资产，不是截图）：
//   字形表 `bundle_fonts_assets_all/MonoBehaviour/Warpforge Trait TextSprites.json`
//     —— 87 个字符 / 96 个字形、`m_Scale ∈ {1.0, 1.65, 1.7}`、
//        `m_Metrics{w,h,bearingX,bearingY,advance}`、`m_GlyphRect{x,y,w,h}`、`m_AtlasIndex=0`
//   图集   `bundle_atlasindividual_assets_40ktraiticonatlas/Texture2D/40k Trait icon atlas.png`
//          1024×1024；78 张切片全是 **80×80**、`m_PixelsToUnits=100`、`m_Pivot=(0.5,0.5)`
//   ⚠️ 原版那份的 `m_FaceInfo` **是 0**（pointSize=0 / scale=0），照抄就行 ——
//      图标实际多大由每个字形的 `m_Scale` 和卡面 TMP 的 `m_fontSize` 决定，
//      卡面那个 `DescTextUnit` 是 **fontSize 23.55 / autoSize 1 / min1 max24 / lineSpacing 5**。
//   🔴 **但 `m_Scale` 不能照抄** —— 见下面 `CalibScale`：原版 faceInfo 是 0 会让 TMP
//      按**当前字体**算缩放，而两边字体不同 ⇒ 大小必须拿成品卡图量出来（2026-09-15）。
//   dump 工具：`工具/dump_trait_textsprites.py` → `数据/游戏数据/trait_textsprites.json`
//
// **别名**：我们的计划表（`数据/游戏数据/card_icon_plan.json`）用的是**短名**
//   （`Melee` / `shield` / `faith` / `SpiritStone_1`…），原版用的是全名
//   （`Atlas_trait_icon_Melee` / `Atlas_SpiritStone_1`）。两套名字**都注册**，
//   指向同一个字形 —— 这样「照卡面原文写 `<sprite name=Atlas_trait_icon_x>`」和
//   「照我们的计划表写 `<sprite name=x>`」都能渲染，不用在两处之间做翻译。
//
// 用法（本机带 ELECTRON_RUN_AS_NODE，启动前必须 unset，见 CLAUDE.md）：
//   -executeMethod IconSetup.Run      建资产（已存在就重建）+ 跑自检
//   -executeMethod IconSetup.Verify   只跑自检
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

public static class IconSetup
{
    const string P = "ICONSETUP ";
    const string OutDir = @"d:/4/_tmp_view/icons";

    /// <summary>原版图集 PNG（解包资源里那一张，1024×1024）</summary>
    const string AtlasSrc = @"d:/2/新解包资源/assets_full/bundle_atlasindividual_assets_40ktraiticonatlas/Texture2D/40k Trait icon atlas.png";
    /// <summary>图集进工程的落点（**不进 Resources** —— 它只被 sprite asset 引用）</summary>
    const string AtlasAsset = "Assets/CardPresentation/Icons/40k_Trait_icon_atlas.png";
    /// <summary>字形表（`工具/dump_trait_textsprites.py` 从原版 bundle 里 dump 的）</summary>
    const string TablePath = @"d:/4/Unity/数据/游戏数据/trait_textsprites.json";
    /// <summary>sprite asset 的落点 —— **必须在 Resources 下**，运行时靠 `Resources.Load` 取</summary>
    const string SaPath = "Assets/CardPresentation/Resources/Fonts/Warpforge Trait TextSprites.asset";
    /// <summary>运行时取的路径（和 `IconFont.ResourcePath` 是同一份，改一边要改另一边）</summary>
    public const string SaResourcePath = "Fonts/Warpforge Trait TextSprites";

    /// <summary>
    /// 🔴 **标定：把一个字形的 `m_Scale` 乘多少，图标才和成品卡图上一样大**（2026-09-15）。
    /// **为什么需要**：原版那份 sprite asset 的 `m_FaceInfo` 是 0 ⇒ TMP 退回**按当前字体**算缩放，
    /// 而原版卡面的字体和我们**不是同一份**（`Tooltips_Global_SDF`/`Tooltips_Global_Unit_SDF` vs 我们的那份）
    /// ⇒ 照抄原版的 `m_Scale` **不保证**渲出来一样大。所以大小只能**量**出来（铁律 7：拿成品卡图当尺子）。
    ///
    /// **尺子**（可复查）：`D:/2/Warpforge部队卡片/Dark Angels/3部队/Warpforge_12_Aggressor.png`（900×1200）
    /// 那一行 `Strike: Gain 〔questPoints1〕`：
    ///   · 图标墨迹 **h≈48 px**（`d:/4/_tmp_view/cardmeas/qp_zoom.png` 8× 放大图 60×65 里量出来的）
    ///   · 同行的拉丁大写高 **h≈24 px**（`S`、`G`、`i` 的升部都是 23~24；`b/t` 那 31 是升部不是大写）
    ///   ⇒ **图标 ÷ 大写 ≈ 2.0**、**图标 ÷ 卡高 = 4.0%**（48/1200）
    ///
    /// **我方量出来的**（`-executeMethod IconSizeProbe.Run`，输出在 `_tmp_view/iconsize/`）：
    ///   · 卡面效果文字字号 2.48（`CardView.KeywordFontSize`）、拉丁大写墨迹高 **0.19** 卡单位
    ///   · 目标图标高 = 2.0 × 0.19 = **0.38** 卡单位 = 卡高的 11.4%
    ///   · 原样（scale 1.7）时图标墨迹 **0.49** 卡单位（`IconSizeProbe` 扫描实测：
    ///     0.5→0.24 / 0.674→0.33 / 0.8→0.39 / 1.0→0.49，**线性吻合**，可直接按比例算）
    ///   ⇒ 系数 = 0.38 ÷ 0.49 = **0.776**
    ///
    /// ⚠️ **改这条要重跑 `IconSizeProbe.Run` 复量**，别照着感觉调。
    /// ⚠️ 和 `CardIcons.FontScaleFor`（字号那一侧）是**两个不同的东西**：那条是「字号该乘多少」，
    ///    这条是「字形本身该缩多少」。两个都从上面同一组测量来，改一个要想另一个。
    /// </summary>
    const float CalibScale = 0.776f;

    static void Log(string s) { Debug.Log(P + s); }
    static void Err(string s) { Debug.LogError(P + s); }

    public static void Run()
    {
        Build();
        Verify();
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    // ─────────────────────────── 建资产 ───────────────────────────

    [MenuItem("Warpforge/建图标 sprite asset")]
    public static void Build()
    {
        if (!File.Exists(TablePath)) { Err("找不到字形表 " + TablePath + " —— 先跑 工具/dump_trait_textsprites.py"); return; }
        if (!File.Exists(AtlasSrc)) { Err("找不到原版图集 " + AtlasSrc); return; }

        // ① 图集拷进工程（内容一样就跳过，别每次刷一屏）
        Directory.CreateDirectory(Path.GetDirectoryName(Abs(AtlasAsset)));
        bool same = File.Exists(Abs(AtlasAsset)) &&
                    new FileInfo(Abs(AtlasAsset)).Length == new FileInfo(AtlasSrc).Length;
        if (!same)
        {
            File.Copy(AtlasSrc, Abs(AtlasAsset), true);
            AssetDatabase.ImportAsset(AtlasAsset, ImportAssetOptions.ForceUpdate);
            Log("图集已拷入 " + AtlasAsset);
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasAsset);
        if (tex == null) { Err("图集没导入成功：" + AtlasAsset); return; }
        Log("图集 " + tex.width + "×" + tex.height);

        // ② 读字形表
        var txt = File.ReadAllText(TablePath);
        var tbl = JsonUtility.FromJson<Table>(txt);
        if (tbl == null || tbl.glyphs == null || tbl.glyphs.Length == 0) { Err("字形表解析失败（字段名对不上？）"); return; }
        Log("字形 " + tbl.glyphs.Length + " 条 / 字符 " + tbl.characters.Length + " 条");

        // ③ 建一份干净的 sprite asset
        AssetDatabase.DeleteAsset(SaPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Abs(SaPath)));
        var sa = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        sa.name = "Warpforge Trait TextSprites";
        sa.spriteSheet = tex;
        // ⚠️ 这两张表在 TMP 里是**只读属性**（实例化时已经建好 List）—— 别赋值，直接 Add
        sa.spriteCharacterTable.Clear();
        sa.spriteGlyphTable.Clear();

        // ⚠️ FaceInfo 照原版留 0（原版就是 0）——
        //    别自作聪明填 pointSize/scale，那会让「和原版一样大」这件事失去依据
        var face = new FaceInfo();
        face.familyName = "Warpforge Trait TextSprites";
        face.pointSize = tbl.face != null ? tbl.face.pointSize : 0f;
        face.scale = tbl.face != null ? tbl.face.scale : 0f;
        sa.faceInfo = face;

        // ④ 逐字形建 Sprite + Glyph（矩形、基线**照抄**；缩放乘标定系数 CalibScale，理由见那个常量的注释）
        var sprites = new List<Sprite>();
        var glyphOfIndex = new Dictionary<int, TMP_SpriteGlyph>();
        foreach (var g in tbl.glyphs)
        {
            var rect = new Rect(g.rect[0], g.rect[1], g.rect[2], g.rect[3]);
            var sp = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sp.name = "trait_sprite_" + g.index;
            sprites.Add(sp);

            var glyph = new TMP_SpriteGlyph
            {
                index = (uint)g.index,
                sprite = sp,
                scale = g.scale * CalibScale,
                atlasIndex = g.atlas,
                glyphRect = new GlyphRect((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height),
                metrics = new GlyphMetrics(g.w, g.h, g.bx, g.by, g.adv),
            };
            sa.spriteGlyphTable.Add(glyph);
            glyphOfIndex[g.index] = glyph;
        }

        // ⑤ 字符：原版全名 + 我们的短名（别名），指同一个字形
        var seen = new HashSet<string>();
        int alias = 0;
        foreach (var c in tbl.characters)
        {
            if (!glyphOfIndex.TryGetValue(c.glyph, out var glyph)) { Err("字符 " + c.name + " 指向不存在的字形 " + c.glyph); continue; }
            AddChar(sa, c.name, glyph, c.scale, seen);
            string shortName = ShortName(c.name);
            if (shortName != c.name && AddChar(sa, shortName, glyph, c.scale, seen)) alias++;
        }
        sa.UpdateLookupTables();
        Log("字符表 " + sa.spriteCharacterTable.Count + " 条（其中短名别名 " + alias + " 条）");

        // ⑥ 材质 + 一起存盘（不存的话下次开会话是 null）
        var mat = new Material(Shader.Find("TextMeshPro/Sprite"));
        mat.name = "Warpforge Trait TextSprites Material";
        mat.mainTexture = tex;
        sa.material = mat;

        AssetDatabase.CreateAsset(sa, SaPath);
        foreach (var sp in sprites) AssetDatabase.AddObjectToAsset(sp, sa);
        AssetDatabase.AddObjectToAsset(mat, sa);

        // ⑦ 🔴 **必须写 `m_Version`** —— TMP 的 `UpdateLookupTables()` 头一句是
        //    `if (material != null && string.IsNullOrEmpty(m_Version)) UpgradeSpriteAsset();`
        //    而 `UpgradeSpriteAsset()` 会 `m_SpriteCharacterTable.Clear(); m_GlyphTable.Clear();`
        //    （`com.unity.ugui@…/Runtime/TMP/TMP_SpriteAsset.cs:116,518`）
        //    ⇒ 有材质、没版本的新资产，**存盘后再读出来两张表就是空的**（实测踩过：
        //      同一次运行报「字形 96 条」，重开一次自检报「字形 0 条」）。
        //    原版那份的版本就是 `"1.1.0"`，照抄。
        var so = new SerializedObject(sa);
        var ver = so.FindProperty("m_Version");
        if (ver != null) { ver.stringValue = "1.1.0"; so.ApplyModifiedPropertiesWithoutUndo(); }
        else Err("找不到 m_Version 字段 —— TMP 版本变了？这条不修的话资产会在读出来时被清空");

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(SaPath, ImportAssetOptions.ForceUpdate);
        Log("OK 生成 " + SaPath);
    }

    static bool AddChar(TMP_SpriteAsset sa, string name, TMP_SpriteGlyph glyph, float scale, HashSet<string> seen)
    {
        if (!seen.Add(name)) return false;
        var ch = new TMP_SpriteCharacter(0xFFFE, glyph);   // 0xFFFE = 没有 unicode，按名字查（原版就是这样）
        ch.name = name;
        if (scale > 0f) ch.scale = scale;
        sa.spriteCharacterTable.Add(ch);
        return true;
    }

    /// <summary>`Atlas_trait_icon_Melee` → `Melee`；`Atlas_SpiritStone_1` → `SpiritStone_1`</summary>
    public static string ShortName(string full)
    {
        const string t = "Atlas_trait_icon_";
        if (full.StartsWith(t)) return full.Substring(t.Length);
        if (full.StartsWith("Atlas_")) return full.Substring("Atlas_".Length);
        return full;
    }

    // ─────────────────────────── 自检 ───────────────────────────

    /// <summary>
    /// 三条：① 资产在 ② 计划表里每个 sprite 名都能在 asset 里查到 ③ 真渲一张图。
    /// ③ 是给人看的 —— 断言测不出「渲出来是空方块」。
    /// </summary>
    [MenuItem("Warpforge/图标自检")]
    public static void Verify()
    {
        Directory.CreateDirectory(OutDir);
        int bad = 0;

        var sa = Resources.Load<TMP_SpriteAsset>(SaResourcePath);
        Check(sa != null, "① sprite asset 在 Resources 下取得到（" + SaResourcePath + "）", ref bad);
        if (sa == null) { Finish(bad); return; }
        Check(sa.spriteSheet != null, "① 图集挂着（" + (sa.spriteSheet == null ? "null" : sa.spriteSheet.width + "×" + sa.spriteSheet.height) + "）", ref bad);
        Check(sa.spriteGlyphTable.Count >= 90, "① 字形 " + sa.spriteGlyphTable.Count + " 条（原版 96）", ref bad);
        Check(sa.material != null, "① 材质在（" + (sa.material == null ? "null" : sa.material.shader.name) + "）", ref bad);

        // ② 计划表里的每个名字都要查得到
        var planPath = @"d:/4/Unity/数据/游戏数据/card_icon_plan.json";
        int names = 0, miss = 0;
        var missing = new List<string>();
        if (File.Exists(planPath))
        {
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(planPath),
                         @"""sprite"":\s*""([^""]+)"""))
            {
                string n = m.Groups[1].Value;
                names++;
                if (sa.GetSpriteIndexFromName(n) < 0 && !missing.Contains(n))
                { miss++; missing.Add(n); }
            }
        }
        Check(names > 0 && miss == 0,
              "② 计划表里的 sprite 名全部查得到：" + (names - miss) + "/" + names +
              (miss > 0 ? "  **缺** " + string.Join(", ", missing.ToArray()) : ""), ref bad);

        // ②b 运行时那条路走一遍：计划表要能被 `JsonUtility` 读出来（**数组形状**就是为了它），
        //     再拿两张卡真换一次 —— 只查 JSON 的话，C# 这边解析器坏了也发现不了
        int pc = CardPresentation.CardIcons.PlanCardCount, pi = CardPresentation.CardIcons.PlanItemCount;
        Check(pc > 100 && pi > 200,
              "②b 运行时读计划表：卡 " + pc + " 张 / 记号 " + pi + " 处（`CardIcons`）", ref bad);
        string da44 = CardPresentation.CardIcons.Rewrite(
            "DA44", "desc", "Give +3 [Attack], +3 [Armor] or +3 Health to a friendly troop");
        Check(da44.Contains("<sprite name=\"Melee\">") && da44.Contains("<sprite name=\"Ranged\">")
              && da44.IndexOf("[Attack]", System.StringComparison.Ordinal) < 0,
              "②c 换出来了（DA44）：" + da44, ref bad);
        string ash = CardPresentation.CardIcons.Rewrite(
            "ASH_Autarch", "desc", "1 [Spirit Stone]: Give +1 melee, +1 ranged and +1 Health to all your troops");
        Check(ash.Contains("<sprite name=\"SpiritStone_1\">") && ash.IndexOf("[Spirit Stone]", System.StringComparison.Ordinal) < 0
              && ash.IndexOf("1 <sprite", System.StringComparison.Ordinal) < 0,
              "②d 灵魂石：档位数字**连图标一起**换掉（不留多余的 1）：" + ash, ref bad);

        // ②e **裸关键词**（卡面上真印着的字）：图标是**插在前面**的，词要留着。
        //     一开始按「换掉」处理，卡面只剩一个图标、关键词整串没了（`Lychguard` 实测）。
        string ly = CardPresentation.CardIcons.Rewrite("SAU_Lychguard", "descZh", "残骸。装甲 2。");
        Check(ly.Contains("<sprite name=\"remnant\">残骸。"),
              "②e 裸关键词：图标插在前、**词留着**：" + ly, ref bad);

        // ②f 方括号记号是**换掉**（那是 OCR 占位，卡面上本来就没印那个词）
        Check(da44.IndexOf("Attack", System.StringComparison.Ordinal) < 0 &&
              da44.Contains("<sprite name=\"Melee\">,"),
              "②f 方括号记号：**换掉**、词不留：" + da44, ref bad);

        // ②f2 **符号类 token**（`☀` / `①`）—— 那个字符**就是那张图**，要**吃掉**不能留着。
        //     判据：token 首字符不是字母/数字 ⇒ 符号。不判的话卡面变成「图标 + 字符」= 同一个东西两遍。
        string sun = CardPresentation.CardIcons.Rewrite("SOR67", "desc", "☀：部署一个额外的战斗修女");
        Check(sun == "<sprite name=\"faith\">：部署一个额外的战斗修女",
              "②f2 符号 token（`☀`）被**吃掉**、不留在文字里：" + sun, ref bad);
        string circ = CardPresentation.CardIcons.Rewrite("ASH_Farseer", "descZh", "①：给一个友方部队");
        Check(circ.IndexOf("①", System.StringComparison.Ordinal) < 0 &&
              circ.Contains("<sprite name=\"SpiritStone_1\">"),
              "②f2 圈码 token（`①`）同样被吃掉：" + circ, ref bad);
        // ②f3 **数字烘在图里**：`1 Quest Point` 的 `questPoints1` 图上已经印着 1 ⇒ 整串吃掉。
        //     判据是「那个数字就在图名里」（`1` ∈ `questPoints1`），**不是**按 token 名猜
        //     —— 之前只认 `Spirit Stone`，`Quest Point` 会留下「1 Quest Point」纯文本 + 图标。
        string qp = CardPresentation.CardIcons.Rewrite("DA12", "desc", "Gain 1 Quest Point");
        Check(qp.IndexOf("Quest Point", System.StringComparison.Ordinal) < 0 &&
              qp == "Gain <sprite name=\"questPoints1\">",
              "②f3 `N Quest Point` 整串被吃掉（数字已在图里）：" + qp, ref bad);

        // ②g **幂等** —— 同一份文字会被换两遍（`SetData` 会把同一份 `CardData` 再喂给卡面一次）。
        //     不幂等的话第二遍会在**已经带图标的词**上再插一个图标（实测踩过）。
        string g1 = CardPresentation.CardIcons.Rewrite("GOF100", "descZh",
                     "给予一个友方单位[践踏]。如果其已经拥有[践踏]，改为本回合给予其 +2[攻击]。");
        string g2 = CardPresentation.CardIcons.Rewrite("GOF100", "descZh", g1);
        // ⚠️ 这张卡的原文用的是**方括号**写法（`[践踏]`）⇒ 卡面上本来就**没有**「践踏」这个词，
        //    换完只剩图标才是对的（别指望这里能查到「践踏」）。
        Check(g1 == g2 && CountOf(g1, "<sprite name=\"stomp\">") == 2
                       && CountOf(g1, "<sprite name=\"Melee\">") == 1,
              "②g 换两遍和一遍结果相同（幂等）：" + g2, ref bad);
        Check(ly == CardPresentation.CardIcons.Rewrite("SAU_Lychguard", "descZh", ly),
              "②h 裸关键词那条也幂等", ref bad);

        // ③ 真渲一张：一行字 + 三种图标
        //    ⚠️ 用**世界空间**的 `TextMeshPro`（和 `TmpSetup.Verify` 一样）——
        //       UI 那版要 Canvas 才出网格，批处理下没有 Canvas
        var go = new GameObject("iconprobe");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = CardPresentation.TmpFont.Font;
        tmp.spriteAsset = sa;
        tmp.fontSize = 23.55f;                 // 卡面 `DescTextUnit` 的 m_fontSize
        tmp.text = "Give +2 <sprite name=\"Melee\">, +2 <sprite name=\"Ranged\"> and +3 " +
                   "<sprite name=\"Atlas_trait_icon_faith\"> Faith <sprite name=\"SpiritStone_1\">";
        tmp.ForceMeshUpdate();
        int vis = tmp.textInfo.characterCount;
        var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
        Check(vis > 0 && mesh != null && mesh.vertexCount > 0,
              "③ TMP 生成了 " + vis + " 个字形 / 顶点 " + (mesh == null ? 0 : mesh.vertexCount), ref bad);
        string png = Path.Combine(OutDir, "icon_probe.png");
        RenderToPng(tmp, png);
        Log("③ 探针图 → " + png + "（**人眼看一下图标出没出来**）");
        Object.DestroyImmediate(go);

        Finish(bad);
    }

    static void Check(bool ok, string msg, ref int bad)
    {
        Log((ok ? "OK " : "!! ") + msg);
        if (!ok) bad++;
    }

    static int CountOf(string s, string sub)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }

    static void Finish(int bad)
    {
        Log(bad == 0 ? "全部通过" : ("**有 " + bad + " 条没过**"));
        if (Application.isBatchMode) EditorApplication.Exit(bad == 0 ? 0 : 1);
    }

    /// <summary>把 TMP 摆在相机前渲一张 PNG（批处理下没有帧循环，得手动 Render）</summary>
    static void RenderToPng(TMP_Text tmp, string path)
    {
        const int W = 900, H = 200;
        var camGo = new GameObject("iconprobe_cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 1.6f;
        cam.transform.position = new Vector3(0, 0, -10);
        camGo.transform.LookAt(Vector3.zero);

        tmp.transform.position = Vector3.zero;
        tmp.alignment = TextAlignmentOptions.Center;

        var rt = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(path, tex.EncodeToPNG());

        var px = tex.GetPixels32();
        var bg = (Color32)cam.backgroundColor;
        int ink = 0;
        for (int i = 0; i < px.Length; i++)
            if (Mathf.Abs(px[i].r - bg.r) > 8 || Mathf.Abs(px[i].g - bg.g) > 8 || Mathf.Abs(px[i].b - bg.b) > 8) ink++;
        Log("③ 渲出来有东西：非背景像素 " + ink + " / " + px.Length);

        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
    }

    static string Abs(string assetPath)
    {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
    }

    // JsonUtility 用的扁平结构（和 `数据/游戏数据/trait_textsprites.json` 一一对应）
    [System.Serializable] class Table
    {
        public Face face; public Glyph[] glyphs; public Char[] characters;
    }
    [System.Serializable] class Face { public float pointSize; public float scale; }
    [System.Serializable] class Glyph
    {
        public int index; public float w, h, bx, by, adv; public int[] rect;
        public float scale; public int atlas;
    }
    [System.Serializable] class Char { public string name; public int glyph; public float scale; }
}
