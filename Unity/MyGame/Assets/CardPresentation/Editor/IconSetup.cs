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
    /// 🔴 **标定：把一个字形的 `m_Scale` 乘多少，图标才和成品卡图上一样大。**
    /// **为什么需要**：原版那份 sprite asset 的 `m_FaceInfo` 是 **0**（pointSize / scale 全 0）⇒ TMP 走
    /// 「**按正文字体的 faceInfo 算缩放**」那一支（`Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TMP_Text.cs:4164`）：
    ///   `spriteScale = fontSize / fontFace.pointSize × fontFace.scale × 0.1`，
    ///   `currentElementScale = fontFace.ascentLine / glyph.metrics.height × sprite.scale × glyph.scale × spriteScale`
    /// ⇒ **同一个 `m_Scale`，正文字体换了、图标大小就跟着变**。所以大小只能**量**出来（铁律 7）。
    ///
    /// **两条仪器口径（2026-10-23 查实，别再踩）**
    ///   · `IconSizeProbe.Run` 建的是**裸 TMP**、`font = TmpFont.Font`（**CJK 面**：
    ///     `ascentLine 57.55 / pointSize 50 / scale 0.9`），而**卡面效果文字的正文字体是
    ///     `Face.LatinCardBody`（Pragati：`ascentLine 70 / pointSize 95 / scale 1`）**
    ///     ⇒ 同一字号下两边图标差 **0.711 倍**（`70×0.1/95 = 0.07368` ÷ `57.55×0.9×0.1/50 = 0.10359`）。
    ///     所以探针报的 `questPoints1` 墨高 **0.32 卡单位**（CJK 面）**不是**卡面会渲出来的值；
    ///     换成卡面真实那一面是 **≈0.23**。⛔ 别再把探针的绝对数直接当卡面读数用。
    ///   · 下面 ⑨ 那条断言（`Verify`）**按 `Face.LatinCardBody` 量**，不复用探针的绝对数。
    ///
    /// **尺子（2026-10-23 亲测，可复查）**：`D:/2/Warpforge部队卡片/Dark Angels/3部队/Warpforge_12_Aggressor.png`（900×1200）
    /// 那一行 `〔骷髅〕Strike: Gain 〔questPoints1〕`：
    ///   · 图标墨迹 **51 px 高**（阈值 90~110 稳定在 50~51；阈值 150 时 48）
    ///   · 同一行**拉丁大写**（`S` / `G`）**31 px**；小写 x-height **23~24 px**
    ///   ⇒ **图标 ÷ 大写 = 51/31 = 1.65**；**图标 ÷ x-height ≈ 48/24 ≈ 2.0**。
    ///   🔴 **2026-10-23 就地订正（铁律 5）**：本节原来写「图标 ÷ 大写 ≈ **2.0**」「大写高 h≈24 px」——
    ///     **那个 24 是一行的【小写】字母**（`a/i/n` 23~24），真正的拉丁大写是 **31 px** ⇒
    ///     **2.0 是「图标 ÷ x-height」，不是「图标 ÷ 大写」**。本文件下面所有 2.0 一律按 1.65 更正。
    ///
    /// **换算链（每个数的量纲写死）**
    ///   · `1.65`   = 成品卡图上「图标墨高 ÷ **大写**墨高」——**无量纲**（见上）
    ///   · `0.155`  = **我们**卡面效果文字在**名义字号**下的**大写**墨迹高，单位 = **卡单位**
    ///                （`KeywordFontSize 2.3552 × TmpFont.CapPerFontSize(LatinCardBody) 0.0656 ≈ 0.1545`。
    ///                 ⚠️ 原注释那条 **0.19** 是拿 `TmpFont.Font`（CJK 面）量的，口径不对，已作废）
    ///   · 两者相乘 = 「目标图标墨高」= **0.255 卡单位**（**名义字号**下）
    ///   · ÷ `CardView.Height`（3.3313）= **7.7% 卡高** —— ⚠️ **这是【名义】值，不能当标定目标**
    ///
    /// 🔴 **为什么「÷ 卡高」不能当标定目标**：卡面效果文字过 `CardView.FitToBox`（`CardView.cs:4267`）
    ///   **按版面框回缩**，正文字号不是名义值 —— `DA65` 实测回缩到名义的 **≈0.40**
    ///   （卡面 CJK 墨高 26 px ÷ 卡高 1001 px = 2.6%，而名义 em 是 7.07% 卡高）
    ///   ⇒ 「图标 ÷ 卡高」**逐卡不同**（文本越长越小）。
    ///   原注释「目标图标高 = 2.0 × 0.19 = 0.38 卡单位 = 卡高的 11.4%」两处都错：
    ///     ① `0.38` 是拿 **x-height 口径的 2.0** 乘 **大写口径的 0.19**（两个量纲混乘）；正确 = `1.65 × 0.155 ≈ 0.255`；
    ///     ② 「11.4% 卡高」只在名义字号下成立，而卡面永远先过 `FitToBox`
    ///        ⇒ 与成品图的「图标 ÷ 卡高 4.0%（48/1200）」**本来就不可比** ——
    ///        成品图那 4.0% 是**印刷版式**；铁律 10 第 3 条：PnP 是印刷品，量「内容」可以、量「版式」不行。
    ///
    /// **本值怎么来的（0.842）**
    ///   名义字号下「图标 quad ÷ 大写 quad」= (`70/95` ÷ `60/95`) × `glyph.scale` × 墨占
    ///     = `1.1667 × glyph.scale × 0.9875`（`questPoints1` 的墨占 79/80）
    ///   要它等于成品图的 **1.65** ⇒ `glyph.scale` = 1.432 ⇒ **`CalibScale` = 1.432 ÷ 1.7 = 0.842**。
    ///   ✅ **模型与实物对得上**：同一公式算 `DA65` 的 `armour`（旧值 0.776）= 0.968，卡面渲图实测 **0.98**
    ///     （2026-10-23：图标墨高 25.5 px ÷ CJK 墨高 26 px）—— 差 1% ⇒ 这套公式可以直接用来标定。
    ///   📌 旧值 **0.776** 的来历：2026-09-15 按「2.0 × 0.19 / 0.49」算的，那条链同时错在
    ///     **x-height 当大写**、**CJK 面当正文面**、**名义值当卡面值**三处；字号按面分档（2026-10-18 `N55`）之后
    ///     连「正文字体不是同一份」这个前提也没了 ⇒ 本次按上面的实测重标。
    ///
    /// 🔴🔴 **2026-10-23（`新账-89` 执行代理现核）—— 上面那条 0.842 只对得上【PnP】，对不上【数字客户端】。**
    ///   本节把两把尺子的**适用边界**写死，⛔ 下一个人别再混用：
    ///
    ///   · **尺子 A（PnP 印刷品）**：`Warpforge_12_Aggressor.png` 图标 51 px ÷ 拉丁大写 31 px = **1.65**。
    ///     ⇒ 反推 `CalibScale = 0.842`（本轮没动它）。
    ///     ⚠️ **铁律 10 第 3 条**：PnP 是**印刷品**，量它可以、拿它当**数字版**的版式规格不行。
    ///   · **尺子 B（数字客户端实拍 · 中文）**：`桌面/warpforge参考图/点击卡片查看详情的参考.png`
    ///     —— `获得 ①` 那个 `Atlas_SpiritStone_1` 的**绿环墨高 31 px**（绿色掩码阈值 6~10 稳定）
    ///     ÷ 同行汉字 `获得` 墨高 **24 px**（亮度阈值 70~90 稳定）= **1.292**；
    ///     同行 em 由两条独立路给到 **≈26 px**（① 汉字墨高 ÷ 0.93；② 行距 40 px ÷ CJK 的 1.537 em）
    ///     ⇒ 绿环 = **1.19 em**。
    ///   🔴 **两把尺子不同向**：`1.65` 是「图标 ÷ **拉丁大写**」，`1.29` 是「图标 ÷ **汉字**」——
    ///     **分母不是同一个东西**（汉字 ≈0.93 em，拉丁大写 ≈0.63 em）。⛔ 别互相折算、也别拿一个去推翻另一个。
    ///
    ///   🔑 **一个决定性的原件读数（本轮用 UnityPy 解开 bundle 现核，⛔ 不是推断）**：
    ///     原版卡预制体 `DescTextUnit` / `DescTextTactic` 的 TMP 组件 `m_fontAsset` = PPtr(FileID 11,
    ///     PathID **3485036404935369831**) → 落到 `bundle_fonts_assets_all` 里就是 **`Pragati-Regular SDF`**
    ///     （同一次现核：`NameTextUnit*` → `Asar-Regular SDF` ✔ 与我们的 `Face.LatinUi` 一致 ·
    ///      `ArmyText*` / `RaceText` → Pragati ✔）。
    ///     ⇒ **原版卡面正文的主字体就是 Pragati，中文那侧走的是兜底**，
    ///       「中文卡面要把主面切成 CJK」这个说法**不成立**（`TMP_Text.cs:4024-4036`：`<sprite>` 走
    ///       `ValidateHtmlTag` 那一支、**不更新** `m_currentFontAsset` ⇒ 图标用的是**前一个字符**的 faceInfo；
    ///       中文卡里图标前面通常是一个空格/数字（Pragati 画的）⇒ 图标按 **Pragati** 的 `ascentLine 70` 算）。
    ///   📐 **按尺子 B 反推（量纲写死，可复查）**：
    ///     图标 quad = `ascentLine(Pragati 70) × 字号/95 × 0.1 × 1.0(字符 scale) × 1.65(字形 m_Scale) × C`
    ///     汉字 quad = `TmpFont.WorldGlyphPerFontSize × 字号`（2026-10-21 起 ≈0.08532 = 0.948 em × 0.9）
    ///     实拍两者之比 = 图标绿环 31/0.90(墨占) ÷ 汉字 24/0.981(墨占) = **1.404**
    ///     ⇒ `C = 1.404 ÷ (70×0.1/95×1.65 ÷ 0.08532) = 1.404 ÷ 1.4255 = **0.985**`
    ///     —— 与「**原版根本不做标定（C ≡ 1.0，直接用字形表的 `m_Scale`）」**只差 **1.5%**。
    ///     🔴 **本条只是【读数 + 推算】，本轮【没有】改这个常数**：改成 ≈1.0 会让**拉丁卡面的图标同步放大 19%**
    ///     （违反 `新账-89` 简报里「拉丁卡面一字不动」那条），而且会把 ⑨ 那条 PnP 锚点顶红 ——
    ///     那是**换尺子**级别的决定，要调度台裁。⑪（`Verify` 末尾）现在会把两个面的实测读数都打出来，供裁断。
    ///
    /// ⚠️ **改这条必须重跑 `IconSetup.Run`** 重建资产 —— `Build()` 才把 `glyph.scale × CalibScale` 写进
    ///   `Warpforge Trait TextSprites.asset`；**只跑 `Verify` 量到的还是旧资产**（⑨ 会红，文案里提示这一点）。
    /// ⚠️ 和 `CardIcons.FontScaleFor`（字号那一侧）是**两个不同的东西**：那条是「字号该乘多少」（今天恒 1），
    ///   这条是「字形本身该缩多少」。
    /// </summary>
    const float CalibScale = 0.842f;

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
    /// 七组：① 资产在 ② 计划表里每个 sprite 名都能在 asset 里查到 ③ 真渲一张图
    /// ④ **徽记链的通用护栏**（英文卡面画的每一枚徽记，中文卡面也要画；含灭自证，`A1409`）
    ///    ＋ **`KnownZhGaps`「只许变短」的机械保证**（表条数 === 显式常量，`A1423`）
    /// ⑤ **素材盘 ↔ 计划表 的对账**（盘上有图却没被用上的要出声，`A1370`）
    /// ⑥ **两份 `card_icon_plan.json` 的 `cards` 数组逐字节对账**（含灭自证，`A1421`；
    ///    🔴 `A1425` 起是**逐字节**、`File.ReadAllBytes`）。
    /// ⑦ **生产那一行真在传 `c.NumericKeywords`**（`A1417`）—— 钉的是
    ///    `BattleDriver.FaceTextFull` 那个**调用点**，⛔ 不是 `Badges.CarriesValue` 自己那一侧（那一侧早有断言）。
    /// ⑧ **官方源换代（`A1771`，2026-10-19）**：`[Attack]` 的答案来源从「卡图 OCR 逐张猜」换成
    ///    **官方 `Card_Description` EN** ⇒ 这里钉**产物侧**四条：每条 `[Attack]` 都是 `Melee` ·
    ///    显示快照等价（全池 `Rewrite` 无残留、Melee 枚数逐卡相等）· `工具/ruleprobe/` 0 引用
    ///    （`PinnedPlayMd5` 不受影响）· 幂等。**官方覆盖率/逐卡序列一致/`desc` 未被写**在生成器里闸
    ///    （`gen_icon_plan.py` 的 `bad` ⇒ `rc≠0`、零落盘）。
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
        //    ⚠️ 路径走 `DataPlanFileAbs` 那个常量 —— ⛔ 别在这儿再抄一份字面量：
        //       ⑥ 就是拿 `DataPlanFileAbs` ↔ `PlanFileAbs` 去对账的，抄第二份迟早会劈叉。
        var planPath = DataPlanFileAbs;
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
        // ②c 🔴 **2026-10-20（A980）重写：夹具改成【卡池里的真文本】**。
        //
        //   **为什么**：原来喂的是一句硬编码的串
        //   `"Give +3 [Attack], +3 [Armor] or +3 Health to a friendly troop"`，
        //   而 `[Armor]` / `[Armour]` 这个写法**在卡池里一处都没有**
        //   （**口径**：扫 `cards_engine.json` 的 `desc` + `descZh` 找这两个 token；2026-10-20 当轮实测 **0 处**
        //    —— ⛔ 卡池张数与处数一律**现读**（`count` / 现扫），别把当轮数字写死，见 `A1451`）。
        //   这条断言要卡面上**同时**出现 Melee 与 Ranged 两枚图标 ⇒ 它测的是一个
        //   **已经不存在的 token** ⇒ **夹具过时**，不是实现缺陷。
        //   🔴 **2026-10-21 更正（`A1361` · 铁律 5）**：这一句原来还引着「`工具/gen_icon_plan.py:187`
        //     那条 `("DA39","[Armor]")` 因此**匹配不上任何 token**」—— **那个行号与键名都已失效**：
        //     逐卡表现读 `gen_icon_plan.py:194` = **`("DA39","[Ranged]")`**（该行自己的注释写着
        //     「键 2026-10-18 由 `[Armor]` 订正过来」）；全脚本**再无** `("DA39","[Armor]")` 这条
        //     （`[Armor]` 只剩文件头 `本文件` 那段描述旧 OCR 数据的老注释里还提得到）。
        //     ⚠️ **连带「匹配不上任何 token」这个论断也翻了**：`DA44` 的 `desc` 现读 =
        //     `Give +3 [Attack], +3 [Ranged] or +3 Health to a friendly troop`（第二枚是**带方括号的**
        //     `[Ranged]`、不是裸词），计划表那份也逐字对得上 ⇒ 那条 `("DA39","[Ranged]")` 今天**匹配得到**。
        //   ⚠️ 所以**不能只改期望值**（那只会把一条空转的断言刷绿）—— **夹具要跟着数据走**
        //      （照 ②i 的写法：拿 `CardDatabase.Load()` 里的真文本，不再另抄一份）。
        //
        //   顺带钉住**一条真缺口**（见下面第二条 `Check` 的长注释）。
        RuleEngine.CardDef da = null;
        foreach (var c in RuleEngine.CardDatabase.Load())
            if (c != null && c.Id == "DA39") { da = c; break; }
        Check(da != null && !string.IsNullOrEmpty(da.Desc),
              "②c 卡池里取得到 DA44 的 `desc`（夹具不再自己抄一份）", ref bad);
        string da44 = da == null ? "" : CardPresentation.CardIcons.Rewrite("DA39", "desc", da.Desc);
        Check(CountOf(da44, "<sprite name=\"Melee\">") == 1
              && CardPresentation.CardIcons.StripTags(da44).IndexOf("[Attack]", System.StringComparison.Ordinal) < 0,
              "②c 方括号记号换掉（DA44 **真文本**，`[Attack]` → 拳图标）：" + da44, ref bad);
        // ★ **A980-c：这一段原来是「已知缺口 · 不是回归」**（卡图上是**两枚**图标、我们当时只画了拳）。
        //   亲读 `D:/2/Warpforge部队卡片/Dark Angels/4计策/Warpforge_44_Ancient-Reliquary.png`：
        //   `Give +3〔粉圈拳〕, +3〔紫圈枪〕 or +3 Health to a friendly troop` —— **两处都只印图、不印词**；
        //   判据 = 铁律 7（`[Armor]`/`[Armour]` 在 **`+N Attack … +N Armour` 这个固定搭配**里
        //   就是**枪（远程）**）+ 上面那张成品卡图。缺口那一侧在 `工具/gen_icon_plan.py`，不在本文件。
        //   🔴 **2026-10-21 更正（`A1361` · 铁律 5）**：本段原来写「我们的 `desc` 那个位置写的正是
        //     **裸词 `Ranged`**、计划表里也没有对应项 ⇒ 卡面印成 `+3 Ranged`」「**这一条现在就是红的**」
        //     —— **两条前提都已不成立**：`desc` 现读是**带方括号的** `[Ranged]`、计划表 `DA44/desc`
        //     现读有 `[Ranged] → Ranged` 这一条，而 `CardIcons` 对方括号那一支发的正是
        //     `<sprite name="{sprite}">`（`Core/CardIcons.cs`）⇒ **这条今天应当绿**。
        //   ⚠️ **本批禁 Unity、没有实跑读数** ⇒ 绿没绿以收口时 `-executeMethod IconSetup.Verify` 为准；
        //     若它仍红，那是**另一条判据**在起作用，⛔ 别照本段旧话去改 `desc`。
        Check(CountOf(da44, "<sprite name=\"Ranged\">") == 1,
              "★ ②c【A980-c】DA44 的 `desc` 应换出**两枚**图标（拳 + 枪）—— 卡图那两处都只印图、不印词；"
              + "改坏法：删掉计划表里 `DA44` 那两条逐卡条目、或把 token 改回裸词 `Ranged` ⇒ 少一枚 ⇒ 红。"
              + "实得：" + da44, ref bad);
        string ash = CardPresentation.CardIcons.Rewrite(
            "ASH83", "desc", "1 [Spirit Stone]: Give +1 melee, +1 ranged and +1 Health to all your troops");
        // 🔴 **2026-10-20 修：这条守卫原来【空转】（改坏了照样绿）。**
        //    原判据 = `ash.IndexOf("1 <sprite", Ordinal) < 0`，意图是「档位数字已烘在图里
        //    （`SpiritStone_1`）⇒ 不该再印那个 `1`」。而 `A969` 给图标外面包了一层 `<link=…>`
        //    之后，**坏结果**产生的是 `1 <link=spiritstone><sprite name="SpiritStone_1">…`
        //    ⇒ 那个子串**两态都匹配不到**（好结果是 `…</link>: Give …`）—— 守卫没牙。
        //    实得串（现成日志）：`d:/4/_tmp_view/iconsetup_1020b.log:457`
        //      = `<link=spiritstone><sprite name="SpiritStone_1"></link>: Give +1 melee, …`。
        //    🆕 改成 ②i 那一族的写法（**不绑死标签的层数与顺序**，将来补 `<nobr>` 也不会假红）：
        //      (a) 图标在 —— 数 `<sprite name="SpiritStone_1">` 的**枚数**；
        //      (b) 记号 `[Spirit Stone]` 没了；
        //      (c) **冒号前面不许再留数字** —— `StripTags()` 剥掉所有标签后，看 `:` 之前
        //          那一段里有没有 `0-9`（坏结果剥完是 `1 : Give …` ⇒ 命中 ⇒ 红）。
        //    🧨 **改坏法**（必须让它红）：把 `Core/CardIcons.cs` 里「数字烘在图里」那一支的第一条
        //      正则 `@"[0-9]+\s+" + Escape(it.token)` 的 `[0-9]+\s+` 去掉（只换 token、不吃数字）
        //      ⇒ 实得 `1 <link=spiritstone><sprite name="SpiritStone_1"></link>: Give …`。
        //      离线复刻两态对照见 `资料/普查产出_1018第三会话/W_卡面残余.md` §③。
        string ashPlain = CardPresentation.CardIcons.StripTags(ash);
        int ashColon = ashPlain.IndexOf(':');
        bool ashNoStrayDigit = ashColon >= 0 && ashPlain.Substring(0, ashColon)
            .IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }) < 0;
        Check(CountOf(ash, "<sprite name=\"SpiritStone_1\">") == 1
              && ash.IndexOf("[Spirit Stone]", System.StringComparison.Ordinal) < 0
              && ashNoStrayDigit,
              "②d 灵魂石：档位数字**连图标一起**换掉（剥标签后冒号前不留数字）：" + ash, ref bad);

        // ②e **裸关键词**（卡面上真印着的字）：图标是**插在前面**的，词要留着。
        //     一开始按「换掉」处理，卡面只剩一个图标、关键词整串没了（`Lychguard` 实测）。
        // 🔴 2026-10-20（A980）：**别拿整串全等去比**（那是它过期的原因 —— 现在外面还有一层 `<link>`）。
        //     照 ②i 的写法：**计图标枚数 + `StripTags()` 之后查词在不在**。
        //     ⚠️ 再加一条「**link 只包了一层**」：`A969` 那个双层形状
        //        `<link=remnant><link=remnant><sprite name="remnant"></link>残骸。</link>`
        //        单看「枚数 1 + 词还在」**判不出来**（两层同 id ⇒ 那两条照样成立）
        //        ⇒ 必须数 `<link=remnant>` 的出现次数，那才是**两态可分辨**的那一条。
        // 🔴 **2026-10-15（第三十二会话）就地改夹具（铁律 5）**：原夹具用 `SAU23|descZh` + 手写串
        //   `残骸。装甲 2。` —— **过时**：`SAU23` 现读 `descZh = '[[remnant]]。[[armour]] 2。[[vanguard]]'`
        //   （**带官方记号**），而 `gen_icon_plan.py` 对带记号的字段**整字段跳过** —— 那批文本运行时走
        //   `RewriteMarks`、**根本不查本表**（见本函数开头 `HasOfficialMarks` 那一支）⇒ 计划表里
        //   `SAU23` 只剩 `desc` ⇒ `Rewrite` 原样返回 ⇒ 四个子句全不成立。
        //   换成**走计划表那一代**的卡：`SAU41|descZh = '复生一个友方残骸。'`
        //   （计划表现读 `descZh:[('残骸','remnant')]`，token 是**裸词**、不带方括号）。
        //   ⚠️ 判据里的 `残骸。` 跟着放宽成 `残骸`（token 本身不带句号）。
        string ly = CardPresentation.CardIcons.Rewrite("SAU41", "descZh", "复生一个友方残骸。");
        Check(CountOf(ly, "<sprite name=\"remnant\">") == 1
              && CountOf(ly, "<link=remnant>") == 1
              && ly.IndexOf("<sprite name=\"remnant\">", System.StringComparison.Ordinal)
                 < ly.IndexOf("残骸", System.StringComparison.Ordinal)
              && CardPresentation.CardIcons.StripTags(ly).Contains("残骸"),
              "②e 裸关键词：图标插在**词前面**、**词留着**、`<link>` **只包一层**：" + ly, ref bad);

        // ②f 方括号记号是**换掉**（那是 OCR 占位，卡面上本来就没印那个词）
        // 🔴 2026-10-20（A980）：原来写成 `da44.Contains("<sprite name=\"Melee\">,")` ——
        //     **过时**：`Rewrite` 给方括号那一支也包了一层 `<link>`，sprite 后面紧跟的是
        //     `</link>` 而不是逗号（实得 `<sprite name="Melee"></link>,`）。
        //     改成「计图标枚数 + 剥标签后**查不到那个词**」——
        //     ⚠️ 词的判据**必须走 `StripTags`**，否则标签自带的名字（`name="Melee"`）自己就把
        //        `Melee` 命中了；而这里要证的是**卡面上不留 `Attack` 这个字**。
        Check(CountOf(da44, "<sprite name=\"Melee\">") == 1
              && CardPresentation.CardIcons.StripTags(da44).IndexOf("Attack", System.StringComparison.Ordinal) < 0,
              "②f 方括号记号：**换掉**、词不留：" + da44, ref bad);

        // ②f2 **符号类 token**（`☀` / `①`）—— 那个字符**就是那张图**，要**吃掉**不能留着。
        //     判据：token 首字符不是字母/数字 ⇒ 符号。不判的话卡面变成「图标 + 字符」= 同一个东西两遍。
        // 🔴 2026-10-20（A980）：原来拿**整串全等**去比 —— 过时（现在外面还有一层 `<link>`，
        //     实得 `<link=faith><sprite name="faith"></link>：部署一个额外的战斗修女`）。
        //     照 ②i 的写法改：**图标在 + 那个符号不在了 + 后面的话还在**，三条都要。
        string sun = CardPresentation.CardIcons.Rewrite("SOR66", "desc", "☀：部署一个额外的战斗修女");
        Check(CountOf(sun, "<sprite name=\"faith\">") == 1
              && sun.IndexOf('☀') < 0
              && CardPresentation.CardIcons.StripTags(sun).Contains("：部署一个额外的战斗修女"),
              "②f2 符号 token（`☀`）被**吃掉**、不留在文字里：" + sun, ref bad);
        // 🔴 **2026-10-15（第三十二会话）就地改夹具（铁律 5）**：原夹具用 `ASH80|descZh` —— 同 ②e，
        //   它现读带官方记号（`[[waystone]]。…((Spirit_x,1)) …`）⇒ 计划表里没有 `descZh` 字段。
        //   换成 `ASH83|descZh = '①：给予你的所有部队 +1 [攻击]、+1 [远程]和 +1 生命'`
        //   （计划表现读 `descZh:[('[攻击]','Melee'),('[远程]','Ranged'),('①','SpiritStone_1')]`，
        //    该字段**不带记号**、走计划表那一路）。
        string circ = CardPresentation.CardIcons.Rewrite("ASH83", "descZh",
                     "①：给予你的所有部队 +1 [攻击]、+1 [远程]和 +1 生命");
        Check(circ.IndexOf("①", System.StringComparison.Ordinal) < 0 &&
              circ.Contains("<sprite name=\"SpiritStone_1\">"),
              "②f2 圈码 token（`①`）同样被吃掉：" + circ, ref bad);
        // ②f3 **数字烘在图里**：`1 Quest Point` 的 `questPoints1` 图上已经印着 1 ⇒ 整串吃掉。
        //     判据是「那个数字就在图名里」（`1` ∈ `questPoints1`），**不是**按 token 名猜
        //     —— 之前只认 `Spirit Stone`，`Quest Point` 会留下「1 Quest Point」纯文本 + 图标。
        // 🔴 2026-10-20（A980）：原来拿**整串全等**去比 —— 过时（现在外面还有一层 `<link>`，
        //     实得 `Gain <link=questpoints><sprite name="questPoints1"></link>`）。
        //     照 ②i 的写法改：**图标在 + 剥标签后 `Quest Point` 与那个 `1` 都没了 + `Gain` 还在**。
        string qp = CardPresentation.CardIcons.Rewrite("DA9", "desc", "Gain 1 Quest Point");
        string qpPlain = CardPresentation.CardIcons.StripTags(qp);
        Check(CountOf(qp, "<sprite name=\"questPoints1\">") == 1
              && qpPlain.IndexOf("Quest Point", System.StringComparison.Ordinal) < 0
              && qpPlain.IndexOf("1", System.StringComparison.Ordinal) < 0
              && qpPlain.Contains("Gain"),
              "②f3 `N Quest Point` 整串被吃掉（数字已在图里）：" + qp, ref bad);

        // ②g **幂等** —— 同一份文字会被换两遍（`SetData` 会把同一份 `CardData` 再喂给卡面一次）。
        //     不幂等的话第二遍会在**已经带图标的词**上再插一个图标（实测踩过）。
        // 🔴 **2026-10-15（第三十二会话）就地改夹具（铁律 5）**：原夹具喂的是手写中文串
        //   `给予一个友方单位[践踏]。…`，而 `GOF101` 现读 `descZh` 带官方记号
        //   （`给一个友方单位[[stomp]]。…((Melee_attack_icon_x,+2))`）⇒ 计划表里没有那个字段。
        //   换成**该卡 `desc` 那一半**（现读 = 下面这一串；计划表现读
        //   `desc:[('[Stomp]','stomp'),('[Stomp]','stomp'),('[Attack]','Melee')]`
        //   ⇒ 与下面「stomp ×2 + Melee ×1」的判据逐字对得上）。
        string g1 = CardPresentation.CardIcons.Rewrite("GOF101", "desc",
                     "Give [Stomp] Stomp to a friendly unit. If it already has [Stomp] Stomp, "
                     + "give it +2 [Attack] this turn instead");
        string g2 = CardPresentation.CardIcons.Rewrite("GOF101", "desc", g1);
        // ⚠️ 这张卡的原文用的是**方括号**写法（`[Stomp]`）⇒ 卡面上本来就**没有** `Stomp` 这个词，
        //    换完只剩图标才是对的（别指望这里能查到 `Stomp`）。
        Check(g1 == g2 && CountOf(g1, "<sprite name=\"stomp\">") == 2
                       && CountOf(g1, "<sprite name=\"Melee\">") == 1,
              "②g 换两遍和一遍结果相同（幂等）：" + g2, ref bad);
        // ⚠️ 跟着 ②e 换了卡 —— 原来 ②e 的 `ly` **根本没被换过**（那条恒真 = 空转），本条也就没在测。
        //    换过之后 `ly` 里含 `<link=remnant><nobr><sprite …>残骸</nobr></link>`，
        //    **这一条才真的在测幂等**（判据 = `CardIcons.cs` 那句 `if (s.IndexOf(bare) >= 0) continue;`）。
        Check(ly == CardPresentation.CardIcons.Rewrite("SAU41", "descZh", ly),
              "②h 裸关键词那条也幂等", ref bad);

        // ②i / ②j 🔴 **Oath 徽记 = 「徽记 + 词」**（2026-10-18，`资料/普查产出_1018/V-OATH_Oath徽记与记号.md` 候选 A）。
        //
        //   **钉什么**：这三张卡的 `Oath` 在卡面上印的是「**徽记 + 紧跟那个词**」
        //   （亲读成品卡图：`Ultramarines/3部队/Warpforge_20_Chaplain-Cassius.png` 的徽记在**句中**、
        //     `…/Warpforge_25_Ferren-Areios.png` 与 `…/Warpforge_12_Vico-Therbeus.png` 各 **2 枚**）。
        //   我们这边两处会把它弄坏：① `desc` 里写成**方括号** `[Oath]` ⇒ `Rewrite` 走「整串换掉」那支
        //   ⇒ **词被吃掉**（`UM84` 原来就是这样，`Talent` 同病）；② 徽记那一条**不在计划表里**
        //   ⇒ **整枚不画**（`UM106` / `UM93` 的首句原来就是，因为 `Oath abilities`
        //   没有数字没有冒号、句首关键词正则扫不到）。
        //
        //   ⚠️ **判别式写成下面 (a) + (b) 这两条**，**别拿整串全等去比**（②e / ②f / ②f2 / ②f3 就是
        //      因为拿全等比才被 `<link>` 那一层刷红的，2026-10-20 一起改了）：
        //      **(a) 徽记在**（查 `<sprite name="oath">` 的出现**次数**）
        //      **(b) 词也留着**（`StripTags` 把标签剥掉之后还查得到那个词）。
        //      🆕 **2026-10-20（A969）就地更正**：这里原来写着「判别式**不能**写成
        //         `<sprite name="oath">Oath` 那种紧贴写法，因为 `Rewrite` 把图标与词**分别**包了
        //         `<link>`、图标后面紧跟的是 `</link>`」—— 🧨 **那句是照【两层 link】的坏形状写的**，
        //         而两层本身就是 bug（原版只有一层，判据见 `CardIcons.cs` 那条 2026-10-20 注释）。
        //         修完之后形状 = `<link=oath><sprite name="oath">Oath abilities</link>`，
        //         与 `CardText.KeywordSegment` / 原版 `TraitNameToString` **同一个形状**（一层）
        //         ⇒ 「图标紧贴词」**现在成立**（下面 `d.IndexOf(enWord) > iD` 那半句仍然对）。
        //         但判据**照旧按 (a)+(b) 写** —— 它不绑死标签的层数与顺序，将来补上 `<nobr>` 也不会假红。
        //   🧨 **改坏哪里会让它红**：
        //      · `desc` 改回 `[Oath]` 写法（计划表跟着重跑）⇒ (b) 拿不到那个词 ⇒ 红；
        //      · 把 `gen_icon_plan.py` 的 `BARE_TOKEN_BY_CARD` 里对应那条删掉 ⇒ 次数少 1 ⇒ 红；
        //      · 把 token 写成短的 `"Oath"`（`UM106` / `UM93`）⇒ 被同卡更长的
        //        `"Oath 1:"` 那条按**长度降序 + 幂等守卫**挡掉 ⇒ 次数回到 1 ⇒ 红。
        {
            // `want` = 这张卡的卡面上那枚徽记应该出现几次（= 亲读读数）
            // 🆕 `A1409`：「**中文卡面徽记在不在**」这件事的**通用判据**已经在下面 **④a** 了
            //    （对全池每张卡都成立）⇒ 这张逐卡表**只剩**「钉得更死的那几条」（枚数 / 词序 / 幂等），
            //    ⛔ **别再往这里加卡**（加了也盖不住新卡 —— 那正是 `A1409` 要治的）。
            string[,] oathCards = {
                { "UM100",             "Oath abilities", "誓言能力", "1" },
                { "UM106",             "Oath abilities", "誓言能力", "2" },
                { "UM93", "Oath abilities", "誓言能力", "2" },
            };
            for (int i = 0; i < oathCards.GetLength(0); i++)
            {
                string id = oathCards[i, 0], enWord = oathCards[i, 1], zhWord = oathCards[i, 2];
                int want = int.Parse(oathCards[i, 3]);
                RuleEngine.CardDef cd = null;
                foreach (var c in RuleEngine.CardDatabase.Load())
                    if (c != null && c.Id == id) { cd = c; break; }
                Check(cd != null && !string.IsNullOrEmpty(cd.Desc) && !string.IsNullOrEmpty(cd.DescZh),
                      "②i 卡池里取得到 " + id + " 的 desc / descZh（卡面走的是 `DescZh`）", ref bad);
                if (cd == null || string.IsNullOrEmpty(cd.Desc) || string.IsNullOrEmpty(cd.DescZh)) continue;

                string d = CardPresentation.CardIcons.Rewrite(id, "desc", cd.Desc);
                string z = CardPresentation.CardIcons.Rewrite(id, "descZh", cd.DescZh);
                // ⚠️ **数两档名字**（`oath` 与 `Atlas_trait_icon_oath`，同一个字形 —— 见 `CountSprite` 的 doc）：
                //    英文 `desc` 走**计划表**（短名），中文 `descZh` 走**官方 `[[oath]]`**（`RewriteMarkTraits` 写全名）
                //    ⇒ 只数短名会把中文那三条全判成 0/1（2026-10-23 实测的假红）。
                int nD = CountSprite(d, "oath"), nZ = CountSprite(z, "oath");
                int iD = IndexOfSprite(d, "oath");
                Check(nD == want && iD >= 0 && d.IndexOf(enWord, System.StringComparison.Ordinal) > iD
                      && CardPresentation.CardIcons.StripTags(d).Contains(enWord),
                      "★ ②i " + id + " [desc]：Oath 徽记 **" + nD + "/" + want + "** 枚，"
                      + "且徽记在词**前面**、词也留着 —— " + d.Replace("\n", "\\n"), ref bad);
                Check(nZ == want && CardPresentation.CardIcons.StripTags(z).Contains(zhWord),
                      "★ ②i " + id + " [descZh]：Oath 徽记 **" + nZ + "/" + want + "** 枚，且词也留着 —— " + z, ref bad);
                Check(d == CardPresentation.CardIcons.Rewrite(id, "desc", d)
                      && z == CardPresentation.CardIcons.Rewrite(id, "descZh", z),
                      "★ ②i " + id + "：这两条也**幂等**（卡面同一条文字会被换两遍）", ref bad);
            }

            // `UM84` 的第二句：`[Talent]: …` 与 `[Oath]` **同根因**（OCR 把「图标 + 词」压成了一个 token
            // ⇒ 整串换掉、词 `Talent` 没了）。亲读卡图：`〔纸卷〕Talent: Catechism of Death`。
            {
                RuleEngine.CardDef cd = null;
                foreach (var c in RuleEngine.CardDatabase.Load())
                    if (c != null && c.Id == "UM100") { cd = c; break; }
                string d = cd == null ? "" : CardPresentation.CardIcons.Rewrite("UM100", "desc", cd.Desc);
                Check(d.IndexOf("<sprite name=\"talent\">", System.StringComparison.Ordinal) >= 0
                      && CardPresentation.CardIcons.StripTags(d).Contains("Talent:"),
                      "★ ②j `UM84` 的 `Talent:` 同样是「**纸卷 + 词**」（改回 `[Talent]` ⇒ 词没了 ⇒ 红）—— "
                      + d.Replace("\n", "\\n"), ref bad);
            }
        }

        // ═════════════════ ④ 徽记链的【通用】护栏（`A1409`）═════════════════
        //
        // 🔴 **为什么要单开这一条**（判据原文 → `项目任务.md` §29·b 的 `A1409`）：
        //   「中文卡面徽记在不在」原来只有上面 ②i 那张**逐卡表**（3 张：`UM84` / `UM106` /
        //   `UM93`）⇒ `A1360` 补的 5 张（`UM74` `GOF8` `GOF21` `DA16` `EC35`）与
        //   `A1400` 补的 14 张（`Invulnerable`）**一张都不在表里** ⇒ **把那两笔改回去也不会红**。
        //   ⇒ 这里改成**对全池每张卡都成立的一条通用判据**（⛔ 别再一张一张往表里挤）。
        //
        // **判据（一句话）**：**英文卡面画出来的每一枚徽记，中文卡面也必须画。**
        //   · 「画出来」= 计划表里该卡 `desc` / `descZh` 子表里出现过那个 `sprite`
        //     （那就是 `CardPresentation.CardIcons.Rewrite` 会插进卡面文字里的那一枚）。
        //   · 方向**只有这一个**（`desc` 的 sprite 集合 ⊆ `descZh` 的）。反方向当时实测有一批不成立
        //     （当轮读数 **40 张**，⛔ `A1451`：那是读数不是基线，池一变就会漂），
        //     那些是「**中文侧多画的**」（例：`TAU42` 的中文写了「短距」+「护盾」）—— **不是缺陷**，
        //     ⛔ 所以**不要**加反过来那一条（加了就是 40 条假红）。
        //
        // ⚠️ **为什么判据不是「只查中文【段首】关键词」**（`A1409` 原文那句措辞）：
        //   `A1400` 那批 `Invulnerable` 是**句中**的裸词（`Give Invulnerable to your Warlord …`）、
        //   **根本不是段首关键词** ⇒ 只查段首**盖不住**它。而「英文画了、中文没画」这个形状
        //   **同时**盖住两批（`A1360` 的段首词 + `A1400` 的句中词）⇒ 判据取的是它，比原文那句更宽一层。
        //
        // ⚠️ **这一条【只看得见「两侧不对称」】** —— 两侧**都**没画的那一类它看不见，**别以为它管全**。
        //   已亲读实证的盲区样本：`ASH42 Avatar of Khaine` 的英文 `desc` =
        //   `… Gain Blast 6 and Camouflage. …`，`Blast` / `Camouflage` 都**不在**计划表任何一侧里
        //   —— 而**成品卡图上是印着图标的**（亲读
        //   `D:/2/Warpforge部队卡片/Aeldari/3部队/Warpforge_42_Avatar-of-Khaine.png`：
        //   `(2) Gain 〔紫蓝爆炸〕Blast 6 and 〔迷彩盔〕Camouflage.`，两枚都是**句中**关键词）。
        //   根因是同一个：**图标链只画「段首关键词 + OCR 方括号记号」**，句中的关键词全靠逐卡表补
        //   （`_FAMILY8` / `_STUN_CARDS` / `_TOKEN_BY_CARD`）。那一整类**不是本条能覆盖的**，
        //   见 `A1366` / `A1376` / `A1400` 那一族。⇒ 本条**不替代**它，只是**补上中文侧那一半**。
        //
        // 🧨 **灭自证（④b）**：这条判据读的就是计划表本身 ⇒ 谁把谓词改坏（比如让它恒返回空），
        //   真表那一侧**照样全绿**。⇒ ④b 拿**同一份真表**造一份「**改回去**」的合成表
        //   （把 `A1360` 的 5 张 + `A1400` 的 14 张按改动前的样子抽掉），**要求这条判据必须认出来**。
        //   两份数据独立构造、过**同一个谓词** ⇒ 「恒绿」与「能认出来」**结构上不可能同时成立**。
        {
            var doc = LoadPlanDoc();
            int nCards = (doc == null || doc.cards == null) ? 0 : doc.cards.Length;
            Check(nCards > 600, "④ 读得到运行时那份计划表（" + PlanFileAbs + "）：卡 " + nCards + " 张", ref bad);
            // ⚠️ 这一份**只算一次、三处共用**（④a 的 `fresh` ／ ④d 的陈旧条目 ／ ④c 的读数）——
            //    `ZhGaps` 是纯函数，同一份 doc 算两遍没有意义，还会给「两处不一」留机会。
            var gapsAll = ZhGaps(doc);
            // 🆕 2026-10-19（B4 · E10 §三·5）：「黑暗契约」那一族走 `RewriteMarks`、**不参与计划表对账** ——
            //   必须真的认出那张卡（认不出 = `MentionsDarkPact` 与卡池 id 对不上，本条红）。
            Check(ZhGapsDarkPactSkipped > 0,
                  "④a′ **黑暗契约**那一族走 `CreatePool.MentionsDarkPact` 跳过计划表对账（实测跳过 "
                + ZhGapsDarkPactSkipped + " 张）—— 🧨 删掉 `ZhGaps` 里那句 `continue` ⇒ 0 张 ⇒ 红", ref bad);
            if (nCards > 0)
            {
                var gaps = gapsAll;
                var fresh = new List<string>();
                foreach (var g in gaps) if (System.Array.IndexOf(KnownZhGaps, g) < 0) fresh.Add(g);
                Check(fresh.Count == 0,
                      "★ ④a **英文卡面画的每一枚徽记、中文卡面也画**（全池 " + nCards + " 张；" +
                      "方向 = `desc` 的 sprite 集合 ⊆ `descZh` 的）：**新缺口 " + fresh.Count + " 条**" +
                      (fresh.Count == 0 ? "" : " → " + string.Join(" / ", fresh.ToArray())) +
                      // 🔴 2026-10-15（第三十二会话）就地订正：原来这里把 `gaps.Count`（**总缺口数**）
                      //   写成了「**已知缺口**」，而基线表 `KnownZhGaps` 现读是**空的**（①c = 0/0）
                      //   ⇒ 日志读起来像「N 条已被基线收编」。现按三个数分开打。
                      "；**总缺口 " + gaps.Count + " 条 / 其中基线 " + (gaps.Count - fresh.Count) +
                      " 条 / 新缺口 " + fresh.Count + " 条**（基线表 = `KnownZhGaps`，逐条带出处；那张表" +
                      "**不是「不用修」** —— 铁律 11 说全都要做、只有先后之分，它只把「已查出的」" +
                      "和「今天新冒出来的」分开）" +
                      "。改坏法：删掉任意一张卡 `descZh` 里那一枚（或让中文那个词扫不到）⇒ 新缺口 +1 ⇒ 红。",
                      ref bad);
                if (gaps.Count > 0)
                    Log("④a 已知缺口清单（**要人判**，别拿它当基线）：" + string.Join(" / ", gaps.ToArray()));

                // ④b 【灭自证】：把「**今天两边都画着**」的每一枚**从 `descZh` 侧抽掉** ⇒ ④a 必须**全部**认出来。
                //   🔴 **2026-10-15（第三十二会话）改：控制集从【手写 19 条】改成【从现计划表推导】** ——
                //   原来那 19 条（`A1360` 5 + `A1400` 14）的注释自陈「这 19 条今天**都在**真计划表的 `descZh` 里」，
                //   而 2026-10-15 起 `gen_icon_plan.py` 对**带官方记号**的字段**整字段跳过** ⇒ 当时实测只剩 **5 条**在表里
                //   （⛔ 那是当轮读数、别当基线；现在还剩几条以 ④b 打出来的「控制集 N 条」为准）
                //   ⇒ 抽掉那 5 条之外**等于没抽** ⇒ 这条灭自证**退化成空转**（改动前同池就已经是 5/19）。
                //   ✅ **判据形式没变**：仍是「抽掉 ⇒ ④a 每一条都得认出来」＋「抽掉的条数 == 控制集长度」（**前提检查**）。
                //   出处 = `资料/普查产出_第三十二会话/D3_IconSetup五条红.md` §一·5。
                var broke = LoadPlanDoc();
                var control = RevertControlFor(broke);
                var controlSet = new HashSet<string>(control);
                // ⚠️ **按【去重后的 (卡,sprite) 对】计数**，不是按抽掉的条目数 —— 同一张卡可能
                //   `descZh` 里**重复**同一枚 sprite（同一张卡可以有 N 条条目、却只有 M 个不同的 (卡,sprite) 对）
                //   ⇒ 拿条目数去比控制集长度会**假红**。⛔ 别在这里写死「抽掉几条」（`A1451`：读数会漂），
                //   真读数看 ④b 那一行打出来的「控制集 N 条（去重后）· 实际抽掉 M 条」。
                var removedPairs = new HashSet<string>();
                if (broke != null && broke.cards != null)
                    foreach (var c in broke.cards)
                        foreach (var f in c.fields)
                        {
                            if (f.name != "descZh" || f.items == null) continue;
                            var keep = new List<PlanItem>();
                            foreach (var it in f.items)
                            {
                                if (it != null && controlSet.Contains(c.id + "|" + it.sprite))
                                { removedPairs.Add(c.id + "|" + it.sprite); continue; }
                                keep.Add(it);
                            }
                            f.items = keep.ToArray();
                        }
                var ctrl = broke == null ? new List<string>() : ZhGaps(broke);
                var missed = new List<string>();
                foreach (var pair in control) if (ctrl.IndexOf(pair) < 0) missed.Add(pair);
                string missTxt = missed.Count == 0 ? "" : ("；**漏掉** " + string.Join(" / ", missed.GetRange(0, System.Math.Min(8, missed.Count)).ToArray()));
                int removed = removedPairs.Count;
                Check(control.Count > 0 && removed == control.Count && missed.Count == 0,
                      "★ ④b【灭自证】把「今天两边都画着」的每一枚从 `descZh` 侧抽掉（控制集 " + control.Count +
                      " 条（去重后）· 实际抽掉 " + removed + " 条）⇒ ④a 必须认出来：" +
                      (control.Count - missed.Count) + "/" + control.Count + " 条被抓到" + missTxt +
                      "。这条是「结构上不可能同时满足」的口径：谓词恒绿 ⇒ 这里必红。" +
                      "⚠️ 控制集为空（或抽不动）**也算红** —— 那是「这一格今天没在测任何东西」的前提检查。", ref bad);
            }

            // ═══ ④c `KnownZhGaps`「**只许变短**」的机械保证（`A1423`）═══════════
            //
            // 🔴 **为什么要它**（判据原文 → `项目任务.md` §29·b 的 `A1423`）：
            //   ④a 判红的只是「**不在表里**的新缺口」⇒ **表本身就是基线**，而「**不许往里加**」
            //   原来**只有 `KnownZhGaps` 的注释在挡** —— 谁把一条新缺口写进表里，④a 照样绿
            //   （这正是 ④a 那张「能抓到哪几类回归」表里点名的**设计上的已知弱点**）。
            //   ⇒ 给表配一个**显式常量** `KnownZhGapsCount`，**只改一处就红**。
            //
            // ⚠️ **副作用是【故意留的】**（调度台 2026-10-10 裁定「要加」）：**每修好一条也会红** ——
            //   它逼着「**修一条 ⇒ 同时删表项 + 常量 −1**」**两处一起改**，表才不会变成谎话
            //   （本仓「一处判据」那条纪律的同族）。
            //   🔴 **⛔ 别为了「不红」把这条改成软告警**：改成 `Log` / `LogWarning` 就等于
            //   退回「只有注释在挡」，那正是本条要治的病。
            Check(GapTableOk(KnownZhGaps, KnownZhGapsCount),
                  "★ ④c `KnownZhGaps` **条数 === 常量**：" + KnownZhGaps.Length + " / " + KnownZhGapsCount +
                  "（`A1423` —— 这张表「**只许变短**」的机械保证）。改坏法：① 往里**加**一条" +
                  "（把新缺口当基线）⇒ 红；② **修好一条却只删表项、忘改常量**（或反过来只改常量）⇒ 红。" +
                  "⚠️ 每修好一条**本来就要红一次**、要的是「两处一起改」—— ⛔ 别把这条降成软告警。", ref bad);

            // ④c-b【灭自证】—— 守卫做成**纯函数**，再拿**同一张真表**造三个变体，要求它分别给出正确答案：
            Check(!GapTableOk(WithExtra(KnownZhGaps), KnownZhGapsCount)
                  && !GapTableOk(WithoutLast(KnownZhGaps), KnownZhGapsCount)
                  && !GapTableOk(KnownZhGaps, KnownZhGapsCount + 1)
                  && GapTableOk(KnownZhGaps, KnownZhGapsCount),
                  "★ ④c-b【灭自证】`GapTableOk` 必须**拒**掉这三个变体：" +
                  "真表 **+1 条**=" + !GapTableOk(WithExtra(KnownZhGaps), KnownZhGapsCount) +
                  " ／ 真表 **−1 条**=" + !GapTableOk(WithoutLast(KnownZhGaps), KnownZhGapsCount) +
                  " ／ **声明值写错**（" + (KnownZhGapsCount + 1) + "）= " +
                  !GapTableOk(KnownZhGaps, KnownZhGapsCount + 1) +
                  "（真表本身必须过=" + GapTableOk(KnownZhGaps, KnownZhGapsCount) + "）。" +
                  "🔴 第三个变体挡的是最阴的两种写法：把常量写成 `KnownZhGaps.Length`（**自引用** ⇒ ④c 恒绿）" +
                  "、或把 `Check` 写成恒真 —— 那两种在「声明值写错」这一格上**必红**。", ref bad);

            // ④d（**配套**，超出 `A1423` 原文那一条）：表里的每一条**今天还得是真缺口**。
            //   「修好一条 ⇒ 两处一起改」里的**前半步（删表项）**原来也没有机械保证：
            //   修好一条却**忘了删那一行** ⇒ `KnownZhGaps.Length` 仍是旧数 ⇒ ④c **绿** ⇒ 表照样变成谎话。
            //   判据 = `KnownZhGaps ⊆ ZhGaps(今天的计划表)`（用的就是 ④a 那一份 `gapsAll`）。
            var stale = new List<string>();
            foreach (var k in KnownZhGaps) if (gapsAll.IndexOf(k) < 0) stale.Add(k);
            Check(stale.Count == 0,
                  "★ ④d `KnownZhGaps` 里每一条**今天都还是真缺口**（表 " + KnownZhGaps.Length +
                  " 条；**已经不是缺口的陈旧条目 " + stale.Count + " 条**" +
                  (stale.Count == 0 ? "" : " → " + string.Join(" / ", stale.ToArray())) +
                  "）。修好一条就**把那一行删掉**，④c 会同时要你改常量 —— 两处一起改 = 表不会变成谎话。" +
                  "（本条是 ④c 的**配套**，比 `A1423` 原文多收一道。）", ref bad);
        }

        // ═════════════════ ⑤ 素材盘 ↔ 计划表 的对账（`A1370`）═════════════════
        //
        // 🔴 **为什么单开这一条**：本工程**原来没有任何一处**在看「盘上有哪些图、它们有没有被用上」——
        //   `②` 只查【计划表名 → 资产】、`工具/gen_icon_doc.py` 只把计划表原样铺开
        //   ⇒ 一个**整类**的缝：**素材盘上明明有图，而计划表一条都没用**。
        //   `A1366` 的 `stun` 正是从这个缝漏的（当轮读数：`traits/stun.png` 在盘上、30 张卡的正文里都写着 `Stun`，
        //   而计划表里 `stun` **0 条** ⇒ 那 30 张卡的卡面一枚徽记都没画；⛔ 别当基线）。
        //
        // **这一条做什么**：
        //   · ⑤a 盘上图的**张数**（原版图集 78 个切片）与计划表用到的**图名个数**——两条「还在量」的断言；
        //   · ⑤b **计划表 → 盘**：计划表里每个图名盘上都有同名 `.png`（**大小写不敏感**）。硬断言。
        //   · ⑤c（`Log`）**盘 → 计划表**：`盘上全集 − 计划表用过的` = **无人使用的图**，**每次都打出来**。
        //     其中「**卡池正文里出现过同名词**」的那些单独再列一遍 —— 那一撮才是 `stun` 那一类，
        //     并额外发一条 `Debug.LogWarning`（「出声」）。
        //
        // ⚠️ **为什么一律大小写不敏感**：`Art/traits/` 下 `Cruelty.png` / `Ecstasy.png` 是**大写开头**，
        //   其余（含 `Melee` / `Ranged` / `SpiritStone_*` / `markOf*` / `questPoints*`）也**不是全小写**
        //   ⇒ 任何**大小写敏感**的「盘上名 ↔ 图名」比对都会把它们**误报成差集**
        //   （出处 → `资料/普查产出_第十四会话/W_icon链两笔.md` 顺手发现 6、`W_图标族8词.md` §四 8）。
        // ⚠️ **盘那一侧的输入【不在版本库里】**：`Resources/Art/**` 在 `.gitignore` 里，是
        //   `工具/import_original_art.py` 导出来的 ⇒ 这条对账**只看这台机器的盘**，
        //   换台机器/清过素材就是另一组读数（所以 ⑤a 只是在量它、不是判它「对」）。
        {
            var disk = new List<string>();
            string dir = Abs(TraitsDirAsset);
            if (Directory.Exists(dir))
                foreach (var p in Directory.GetFiles(dir))          // ⚠️ 别写 `GetFiles(dir, "*.png")` ——
                    if (p.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                        disk.Add(Path.GetFileNameWithoutExtension(p));   // Win32 的 8.3 短名会把 `x.png_old` 也算进来
            disk.Sort(System.StringComparer.Ordinal);
            int caseOdd = 0;
            foreach (var n in disk) if (n != n.ToLowerInvariant()) caseOdd++;

            var usedKeys = new HashSet<string>();
            var doc2 = LoadPlanDoc();
            if (doc2 != null && doc2.cards != null)
                foreach (var c in doc2.cards)
                    foreach (var f in c.fields)
                        foreach (var it in f.items)
                            if (!string.IsNullOrEmpty(it.sprite)) usedKeys.Add(it.sprite.ToLowerInvariant());

            var onDisk = new HashSet<string>();
            foreach (var n in disk) onDisk.Add(n.ToLowerInvariant());
            var unused = new List<string>();      // 盘上有图、计划表里 0 条
            foreach (var n in disk) if (!usedKeys.Contains(n.ToLowerInvariant())) unused.Add(n);
            var noDisk = new List<string>();      // 计划表里有名字、盘上没这张图
            foreach (var k in usedKeys) if (!onDisk.Contains(k)) noDisk.Add(k);
            noDisk.Sort(System.StringComparer.Ordinal);

            Check(disk.Count >= 70,
                  "⑤a 盘上 `Resources/Art/traits/*.png` **" + disk.Count + "** 张" +
                  "（原版 `40ktraiticonatlas` 78 个切片；**" + caseOdd + "** 张文件名不是全小写）", ref bad);
            Check(usedKeys.Count >= 50, "⑤a 计划表里用到的图名 **" + usedKeys.Count + "** 个", ref bad);
            Check(noDisk.Count == 0,
                  "★ ⑤b 计划表里每个图名**盘上都有同名图**（大小写不敏感）：" +
                  (noDisk.Count == 0 ? "全过" : "**盘上没有** " + string.Join(" / ", noDisk.ToArray())), ref bad);

            // ⑤c 🔴 **这一行 = `A1370` 要的「出声」**
            var suspect = new List<string>();
            if (unused.Count > 0)
            {
                string pool = NormPool();
                foreach (var n in unused)
                    if (pool.IndexOf(NormKey(n), System.StringComparison.Ordinal) >= 0) suspect.Add(n);
                Log("⑤c **盘上有图、而计划表一条都没用**（" + unused.Count + " / " + disk.Count + " 张）：" +
                    string.Join(" ", unused.ToArray()) +
                    " ｜ 其中【卡池正文里出现过同名词】的 **" + suspect.Count + "** 张" +
                    (suspect.Count == 0 ? "" : "：" + string.Join(" ", suspect.ToArray())));
                if (suspect.Count > 0)
                    Debug.LogWarning(P + "⑤c 上面这 " + suspect.Count + " 张图【**盘上有、正文里也出现过那个词、" +
                                     "而计划表一条都没用**】—— 这正是 `A1366` 的 `stun` 漏掉时的形状，" +
                                     "逐张人判是不是缺口（**不是硬错**：`relentless` 是天赋名 `Relentless March` 里那个词、" +
                                     "那枚画的是 `talent`，亲读 `Necron/1督军/Warpforge_3_Nemesor-Zahndrekh.png`）：" +
                                     string.Join(" ", suspect.ToArray()));
            }
            else Log("⑤c **盘上有图、而计划表一条都没用**：无（" + disk.Count + " 张全都被计划表用上了）");
        }

        // ═════════════════ ⑥ 两份计划表的对账（`A1421`）═════════════════
        //
        // 🔴 **为什么单开这一条**（判据原文 → `项目任务.md` §29·b 的 `A1421`）：
        //   同一个生成器 `工具/gen_icon_plan.py` **同时写两份** `card_icon_plan.json`：
        //   · `数据/游戏数据/` 那份 = 人读的（上面 `②` 与 `工具/gen_icon_doc.py` 读它）；
        //   · `Assets/CardPresentation/Resources/` 那份 = **运行时**的（`CardIcons` 与 ④/⑤ 读它）。
        //   「两份应当逐字节相同」这件事**原来没有任何一处在对账** —— 生成器确实是两份一起写，
        //   但那是它的自觉；**没有任何断言钉住它**。⇒ **两份一旦劈叉**（手工改过其中一份、
        //   或某次生成只写了一半），`②` 与 `④a` 就**各看一份、同时绿**，而玩家看到的
        //   永远只有 `Resources` 那份。
        //
        // **判据（`A1421` 原文）**：两份的 **`cards` 数组逐字节相同**。
        //   ⛔ **不比整个文件** —— 两份的 `note` 头**本来就不同**（`Resources` 那份自己写着
        //   「运行时那一份」）。⇒ 判据取「**从 `"cards"` 这一键起、比到文件尾**」。
        //   ⚠️ **⛔ 这里不写死读数**（本仓的老坑：读数写死 = 「当时对、隔天烂」，见 `A1372`；
        //   `A1451` 起统一改成「**口径 + 指针**」）—— 生成器每重跑一次读数就会变。
        //   **口径**（`A1425` 起）：**字节**（`File.ReadAllBytes`），不是字符；BOM 会算进去。
        //   **指针**：真读数看这一条跑出来的 `⑥ 读数` 那一行（两份长度 / 头部长度 / `cards` 体长度 / FNV-1a64）。
        //   📌 历史留痕：`A1421` 落地时（2026-10-10）曾按**字符**口径记过一组读数
        //     （文件 666037 / 665985 字符）—— 已被 `A1425` 的字节口径取代、**不再可比**，⛔ 别当基线。
        //     `A1425` 的缘由：判据原文写「**逐字节**相同」而实现走 `File.ReadAllText`（**吃 BOM**）
        //     ⇒ 「其中一份被加了 BOM」这件事**看不见**（今天两份都无 BOM、所以今天不假）。
        //
        // 🧨 **灭自证（⑥b）**：这条判据有两个**天然恒绿**的写法 ——
        //   ① 两条路径常量抄成**同一个文件**（那比的就是「自己跟自己」）；
        //   ② 两边都取不到 `"cards"` 时拿 `==` 直接比 ⇒ **`null == null` 恒真**。
        //   ⇒ ⑥b 拿**同一份真内容**造变体，要求**同一个比较器**给出「同／不同」的**正确答案**
        //   （中间翻一位 · 截掉 100 **字符**（= `.Length` 那个单位）⇒ 必须**不同**；两边都取不到 ⇒ 必须**不同**，⛔ 不是相同）。
        //   「恒绿」与「认得出这些变体」**结构上不可能同时成立**。
        {
            // 🔴 **2026-10-11（`A1425` · 铁律 5/11）：判据从「字符」改成「**字节**」** ——
            //   `A1421` 的判据原文写的就是「两份的 `cards` 数组**逐字节**相同」，
            //   而实现走的是 `File.ReadAllText`（**会把 BOM 吃掉**）⇒ **「某一份哪天被加了 BOM」
            //   这件事它看不见**（BOM 是可见字符之外的三个字节）。今天两份都**无 BOM**
            //   （二进制头就是 `{`）⇒ 字符相同 ⇔ 字节相同，所以**今天不假**，但**判据与实现要同口径**。
            //   ⇒ 全套换 `File.ReadAllBytes` + 逐字节比 + 按字节报长度/指纹/首个不同处。
            //   ⚠️ **FNV-1a 的乘法基数不变**，但喂进去的单位从 `char`（UTF-16 码元）变成 `byte`
            //     ⇒ **指纹值与改前不可比**（读数那行是给自己对比用的，别跨版本比）。
            byte[] rawA = File.Exists(DataPlanFileAbs) ? File.ReadAllBytes(DataPlanFileAbs) : null;
            byte[] rawB = File.Exists(PlanFileAbs) ? File.ReadAllBytes(PlanFileAbs) : null;
            byte[] bodyA = CardsBody(rawA), bodyB = CardsBody(rawB);

            Check(rawA != null && bodyA != null,
                  "⑥ 读得到 `数据/游戏数据/` 那份计划表、且里面有 `\"cards\"` 键（" + DataPlanFileAbs + "）", ref bad);
            Check(rawB != null && bodyB != null,
                  "⑥ 读得到 `Resources/` 那份计划表、且里面有 `\"cards\"` 键（" + PlanFileAbs + "）", ref bad);

            int fd = FirstDiff(bodyA, bodyB);
            Check(SameCardsBody(bodyA, bodyB),
                  "★ ⑥a **两份 `card_icon_plan.json` 的 `cards` 数组逐字节相同**（`" +
                  Blen(bodyA) + "` / `" + Blen(bodyB) + "` 字节" +
                  (fd < 0 ? "" : "；**首个不同处在第 " + fd + " 个字节**") +
                  "）。⛔ 头部**不参与**比 —— 两份的 `note` 本来就不同" +
                  "（`Resources` 那份写着「运行时那一份」）。" +
                  "改坏法：只重写其中一份 / 手工改其中一份 ⇒ 红。" +
                  "出处：`工具/gen_icon_plan.py` 是两份一起写，但**那是它的自觉、原来没有断言钉住**（`A1421`）。",
                  ref bad);
            Log("⑥ 读数：`cards` 体 " + Blen(bodyA) + " / " + Blen(bodyB) + " **字节**；指纹 FNV-1a64（按字节） = " +
                BodyHash(bodyA) + " / " + BodyHash(bodyB) + "；头部 " +
                (rawA == null || bodyA == null ? "?" : (rawA.Length - bodyA.Length).ToString()) + " / " +
                (rawB == null || bodyB == null ? "?" : (rawB.Length - bodyB.Length).ToString()) +
                " 字节（**本来就不同**，别拿它当信号）");

            // ⑥b【灭自证】
            bool pathDistinct = !string.Equals(DataPlanFileAbs, PlanFileAbs, System.StringComparison.OrdinalIgnoreCase);
            byte[] flip = MidFlip(bodyA);
            byte[] cut = (bodyA == null || bodyA.Length <= 100) ? null : Slice(bodyA, 0, bodyA.Length - 100);
            Check(pathDistinct
                  && SameCardsBody(bodyA, bodyA)
                  && !SameCardsBody(bodyA, flip)
                  && !SameCardsBody(bodyA, cut)
                  && !SameCardsBody(null, null),
                  "★ ⑥b【灭自证】比较器必须**结构上不可能恒绿**：" +
                  "两条路径确实不同=" + pathDistinct +
                  "（抄成同一条 ⇒ 比的是自己跟自己）；同一份内容 ⇒ **相同**=" + SameCardsBody(bodyA, bodyA) +
                  "（⛔ 若恒「不同」⇒ 这里红）；中间翻一位 ⇒ **不同**=" + !SameCardsBody(bodyA, flip) +
                  "；截掉 100 **字节** ⇒ **不同**=" + !SameCardsBody(bodyA, cut) +
                  "；**两边都取不到 ⇒ 必须「不同」**=" + !SameCardsBody(null, null) +
                  "（这条挡的是 `null == null` **恒真**那个洞 —— 取不到键时别当成「相同」）。", ref bad);
        }

        // ⑦ 生产那一行真在传 `c.NumericKeywords`（`A1417`）
        // ═══════════════════════════════════════════════════════════════════
        //
        // 🔴 **为什么单开这一条**（判据原文 → `项目任务.md` §29·b 的 `A1417`，出自 `A1341` 那批的顺手发现）：
        //   `CardText.KeywordSegment` 的 `numericKeys` 那一档**全仓只有生产那一处在传**
        //   （`BattleDriver.FaceTextFull`），另 5 个自检调用点**全吃默认 `null`**；
        //   而 `A1332` 报告建议的第 4 条断言（`CarriesValue("Foo", 1, new[]{"foo"}) == true`）
        //   **只钉住 `Badges` 那一侧** ⇒ **生产这一行没有任何断言**：谁把那行实参删掉 / 改成 `null`，
        //   全仓**不会有一条红**，而卡面会**静默少印数字**（那一档正好是 `A1341` 补上的第①档）。
        //
        // **夹具**：一张**合成卡**（不进卡池）。它的关键词**带数字、又不在 `Badges.CarriesValue`
        //   的 ③ 表里、值又是 1** ⇒ 三档里**只有第 ① 档能说话**（`Armour 1` 那种不行：
        //   `armour` 在 ③ 表里 ⇒ 删掉 `numericKeys` 照样印数字，**钉不住**）。
        //   取 `Vanguard`：`Card_Trait/vanguard` 在表里（有显示名 ⇒ 判据②过得去），
        //   而 `vanguard` **不在** ③ 那 12 个键里（现读 `Core/Badges.cs` 的 `CarriesValue`）。
        //
        // **判据（两条一起看才叫钉住）**：
        //   (a) 带数字那张 ⇒ 卡面那段**必须出现那个数字**（`numericKeys` 没传 ⇒ 这里红）；
        //   (b) 不带数字的对照张 ⇒ **不许**多印一个数字（挡「恒给所有词加数字」那种退化写法）。
        //   ⇒ (a) 恒绿与 (b) 恒绿**结构上不可能同时成立**（这正是 `A1417` 要的「灭自证」那一半）。
        //
        // ⚠️ **落点说明（如实记）**：这段更自然的位置是**卡面宿主**（`CardPresentation/Editor/BattleScene.cs`
        //   那几条盯 `KeywordSegment` 的断言旁边）；本轮白名单只到「图标工具 + `CardPresentation/Core/**`」，
        //   所以放在**图标链的自检宿主**里 —— 它钉的是同一行（走 `BattleDriver.FaceTextFull`，
        //   ⛔ 不是绕过它直调 `KeywordSegment`），**效力一样**。
        //
        // 🔴🔴 **2026-10-12（`A1627`）：这一段原来【从加上那天起就不可能绿】—— 判为【夹具前提错】，不是生产代码坏。**
        //   · **实况**（收口验证实跑）：`probe=` / `ctrl=` / ⑦b 的 `orderGot` **全是空串** ⇒ ⑦ / ⑦b 两条都红。
        //   · **根因**（现读代码，`文件:行号` 见下）：三张合成卡原来 `desc: ""`、`descZh: ""`，而
        //     `BattleDriver.FaceTextFull` 有一条**空正文早退**
        //     （`BattleDriver.cs` 的 `IsNullOrEmpty`：`if (string.IsNullOrEmpty(body)) return new FaceBody(notes, "desc");`
        //      —— 那一行 2026-09-15 的 `720df56` 就在，**比这两条断言早**）
        //     ⇒ 探针**根本走不到** `KeywordSegment`（`BattleDriver.cs` 的 `KeywordSegment`）那一行
        //     ⇒ ⑦ 自称钉的 `c.NumericKeywords`、⑦b 自称钉的「数组序」，**一条都没被覆盖过**。
        //   · ⚠️ **不是「batchmode / 空场景拿不到卡面文本」**（那是交件时的猜测）：`FaceTextFull` 是
        //     **纯静态函数**（只碰 `CardText` / `Badges` / `CardIcons` 那几个静态口），
        //     **不碰 `CardView`、不要战斗场景、不要 Canvas** —— 空串是**这一条早退**造成的，实据就是 `本文件`。
        //   · **修法（两件一起做）**：① 补一句**不含任何关键词声明**的 `desc`（下面 `ProbeBody`），
        //     让探针真走到那一行；② 加一条**前提断言**（`⑦·前提` / `⑦b·前提`）——
        //     前提一破就**明确报「夹具没走到 `KeywordSegment`」**，而不是像原来那样
        //     静默取空串、只报「`Vanguard 1` 没印出来」（那条读起来像生产代码坏了）。
        //     ⚠️ 本仓红线：**不许静默失败** ⇒ 前提必须自己有名字。
        {
            // ⛔ 这句 `desc` **不许**出现 `Vanguard` / `Shield` / `先锋` / `盾牌` 这些词：
            //    `CardText.KeywordSegment:248` 的 `DeclaredInBody` 会把「正文里已声明」的那一项**整段跳掉**，
            //    `本文件` 连**中文写法**也判一次（英文档下 `other` 取的就是 `KeywordZh`），
            //    而 `IsDeclarationAt(c)` **把段首也算成声明** ⇒ 一次失手，探针的关键词段又变空。
            const string ProbeBody = "Probe body text.";
            var probe = new RuleEngine.CardDef(
                id: "Z_PROBE_NUMKEY", name: "Z Probe Numkey", type: "unit",
                desc: ProbeBody, rarity: "common", faction: "Ultramarines",
                cost: 1, attack: 1, health: 1, rangedAttack: 0,
                keywords: new[] { "Vanguard 1" },
                nameZh: "数档探针", descZh: "", fromOriginalPool: true, subtype: "Infantry");
            var ctrl = new RuleEngine.CardDef(
                id: "Z_PROBE_NONUM", name: "Z Probe Nonum", type: "unit",
                desc: ProbeBody, rarity: "common", faction: "Ultramarines",
                cost: 1, attack: 1, health: 1, rangedAttack: 0,
                keywords: new[] { "Vanguard" },
                nameZh: "无数档探针", descZh: "", fromOriginalPool: true, subtype: "Infantry");

            string got = CardPresentation.BattleDriver.FaceTextFull(probe).body ?? "";
            string gotCtrl = CardPresentation.BattleDriver.FaceTextFull(ctrl).body ?? "";
            bool num = got.Contains("Vanguard 1") || got.Contains("先锋 1");
            bool noNum = gotCtrl.Contains("Vanguard 1") || gotCtrl.Contains("先锋 1");

            // 🔴 **`A1627` 前提断言（⛔ 别删。这一条就是「不许静默失败」那根红线在夹具上的落实）**：
            //   两张探针**必须真的走到** `CardText.KeywordSegment`（`BattleDriver.cs` 的 `CardText.KeywordSegment`）——
            //   判据 = 卡面段**非空**。`FaceTextFull` 在 `CardPresentation/Battle/BattleDriver.cs` 有一条**空正文早退**
            //   （`if (string.IsNullOrEmpty(body)) return new FaceBody(notes, "desc");`）：
            //   夹具的 `desc` 一旦被改回空串（`A1627` 修之前就是这样）⇒ **下面那条 ⑦ 会红**，
            //   而它打的是「`Vanguard 1` 没印出来」——**读起来像生产代码坏了**（`A1417` 那笔账的假象）。
            //   这一条把那件事**说成它自己的名字**：红的是前提，不是被钉的那一行。
            bool premiseOk = got.Length > 0 && gotCtrl.Length > 0;
            Check(premiseOk,
                  "★ ⑦·前提 **夹具必须真的走到 `CardText.KeywordSegment`**（`A1627`）：两张探针的卡面段" +
                  "**都非空**（= `" + premiseOk + "`）—— 空正文会被 `BattleDriver.FaceTextFull:11959`" +
                  "**提前返回**、根本到不了 `CardPresentation/Battle/BattleDriver.cs` 那一行（那时 ⑦ 会以「没印出数字」的形状红，**是假象**）。" +
                  "⚠️ 要探针产出文本，`desc` 必须是**非空且不含关键词声明**的一句（见头上 `ProbeBody`）。" +
                  "实测：`probe=`" + got + "` / `ctrl=`" + gotCtrl + "`", ref bad);

            Check(num && !noNum,
                  "★ ⑦ **生产那一行真在传 `c.NumericKeywords`**（`A1417`）：合成卡 `Vanguard 1` 的卡面段" +
                  "**必须印出那个数字**（= `" + num + "`，走 `BattleDriver.FaceTextFull` ⇒ `CardText.KeywordSegment`）" +
                  "；而**不带数字**的对照张**不许**多印（= `" + !noNum + "`）。" +
                  "🔴 **这一条只可能在两种情形下红**：① `BattleDriver.FaceTextFull` 那一行的第 4 个实参" +
                  "被删/被改成 `null`（⇒ 第①档静默失效、卡面少印数字）；② `CarriesValue` 的第①档坏了。" +
                  "⚠️ 为什么夹具是 `Vanguard`：它**不在** ③ 表里、值又是 1 ⇒ **三档里只有第①档能说话**" +
                  "（用 `Armour 1` 钉不住 —— 它在 ③ 表里，删掉实参照样印数字）。" +
                  "⚠️ 「夹具到底有没有走到 `KeywordSegment`」由上面那条 `⑦·前提` **单独报**（`A1627`）——" +
                  "本条的 `num=false` 只有在**前提成立**时才等于「生产那一行坏了」。" +
                  "实测：`probe=`" + got + "` / `ctrl=`" + gotCtrl + "`", ref bad);

            // ⑦b 🔴 **2026-10-11（`A1406`）：钉住「关键词段的次序 = 卡表 `keywords` 数组的先后」**
            //    —— `CardText.KeywordSegment` 现在**按 `CardDef.Keywords` 的枚举顺序输出**
            //    （那 = `KeywordTable.Parse` 的插入序 = 数组序；.NET `Dictionary` 无删除时枚举序 = 插入序）。
            //    🔑 **夹具为什么要挑这一对**：`["Vanguard","Shield"]` 的**字母序是反的**
            //    （`Shield` < `Vanguard`）⇒ 谁把那个 `Sort` 加回去、或枚举序哪天变了，**这里必红**。
            //    判据出处：卡面声明行的次序逐张实测（2026-10-11 当轮读数：69 张可比卡，数组序对 67、字母序只对 21）。
            //    ⛔ 那是**当轮读数**、不是基线（池一变就会漂）；本条钉的是**次序来源**（数组序 ≻ 字母序），不钉某个具体张数。
            var orderProbe = new RuleEngine.CardDef(
                id: "Z_PROBE_ORDER", name: "Z Probe Order", type: "unit",
                desc: ProbeBody, rarity: "common", faction: "Ultramarines",
                cost: 1, attack: 1, health: 1, rangedAttack: 0,
                keywords: new[] { "Vanguard", "Shield" },
                nameZh: "次序探针", descZh: "", fromOriginalPool: true, subtype: "Infantry");
            string orderGot = CardPresentation.BattleDriver.FaceTextFull(orderProbe).body ?? "";
            int iV = orderGot.IndexOf("Vanguard", System.StringComparison.Ordinal);
            int iS = orderGot.IndexOf("Shield", System.StringComparison.Ordinal);

            // 🔴 **`A1627` 前提断言（⛔ 别删）**：同 `⑦·前提` —— `Z_PROBE_ORDER` 的 `desc` 被改回空串
            //   ⇒ 走不到 `KeywordSegment` ⇒ `orderGot` 空 ⇒ 下面那条 ⑦b 红成「`Vanguard` 不在输出里」，
            //   而真相是**前提不成立**。
            bool orderPremiseOk = orderGot.Length > 0;
            Check(orderPremiseOk,
                  "★ ⑦b·前提 **夹具必须真的走到 `CardText.KeywordSegment`**（`A1627`）：`Z_PROBE_ORDER` 的" +
                  "卡面段**非空**（= `" + orderPremiseOk + "`）—— 空正文会被 `BattleDriver.FaceTextFull:11959`" +
                  "**提前返回**。实测输出 = `" + orderGot + "`", ref bad);

            Check(iV >= 0 && iS >= 0 && iV < iS,
                  "★ ⑦b **关键词段次序 = 卡表 `keywords` 数组的先后**（`A1406`）：合成卡 " +
                  "`[\"Vanguard\",\"Shield\"]` 必须印成 `Vanguard` 在前、`Shield` 在后" +
                  "（字母序是反的 ⇒ 谁把 `KeywordSegment` 里那个 `Sort` 加回去，这里必红）。" +
                  "⚠️ 「夹具到底有没有走到 `KeywordSegment`」由上面那条 `⑦b·前提` **单独报**（`A1627`）。" +
                  "实测输出 = `" + orderGot + "`", ref bad);
        }

        // ═════════════════ ⑧ 官方源换代（`A1771`，2026-10-19）═════════════════
        //
        //  🔴 **背景**：`[Attack]` 画拳还是枪，原来是「卡图 OCR + 还原表逐张猜」的证据层
        //    （`工具/gen_icon_plan.py` 的 `TOKEN_BY_CARD` 里 119 条逐卡条目）。2026-10-19 起
        //    **来源正本 = 官方 `Card_Description` 的 EN 列**；`[Attack]` 升成 `TOKEN_BY_RULE`
        //    的官方 EN 规则（→ `Melee`），那 119 条逐卡条目**已整批删除**。
        //    判据 → `资料/普查产出_1018/AUD_A1771_A1945_可行性.md` §四·4·1 / §五。
        //
        //  **分工（⛔ 别在这里重做生成器那条判据 —— 两处写同一条规则迟早不一致）**：
        //   · **生成器**里做：官方覆盖率（A1）· 逐卡图标序列一致（A2）· `[Attack]` 官方直证（A3）·
        //     `desc` 未被写（A4）—— 全是 `gen_icon_plan.py` 的 `bad` 机制：不过就 `rc≠0`、**零落盘**。
        //   · **这里（产物侧）**：⑧a `[Attack]` 在计划表里全部 = `Melee` · ⑧b【灭自证】·
        //     ⑧c 显示快照等价（全池 `Rewrite` 后无残留、Melee 枚数与计划表逐卡相等）·
        //     ⑧d `PinnedPlayMd5` 不受影响（`工具/ruleprobe/` **0 引用**，结构性证明）·
        //     ⑧e 幂等（②i 已全池覆盖，这里补一条走官方规则、有两处 `[Attack]` 的定点）。
        {
            var doc8 = LoadPlanDoc();
            int atkItems = 0, atkCards = 0;
            if (doc8 != null && doc8.cards != null)
                foreach (var c in doc8.cards)
                {
                    if (c == null || c.fields == null) continue;
                    bool has = false;
                    foreach (var f in c.fields)
                    {
                        if (f == null || f.items == null) continue;
                        foreach (var it in f.items)
                            if (it != null && it.token == "[Attack]") { atkItems++; has = true; }
                    }
                    if (has) atkCards++;
                }
            var notMelee = AtkNotMelee(doc8);
            Check(atkItems > 0 && atkCards > 0 && notMelee.Count == 0,
                  "★ ⑧a **计划表里每一处 `[Attack]` 都画 `Melee`**（`A1771`：来源 = 官方 " +
                  "`Card_Description` EN，123/123 处 = melee）：" + atkItems + " 条 / " + atkCards + " 张卡；" +
                  "非 Melee **" + notMelee.Count + "** 条" +
                  (notMelee.Count == 0 ? "" : " → " + string.Join(" / ", notMelee.ToArray())) +
                  "。改坏法：把计划表里任一条 `[Attack]` 的 `sprite` 改成 `Ranged` ⇒ 红。", ref bad);

            // ⑧b【灭自证】：同一份纯函数 `AtkNotMelee` 必须认得出「改坏」的副本。
            //   ⚠️ 用 `LoadPlanDoc()` **重新读一份**再改 —— 改的是独立对象，不污染 `doc8`。
            var brokeAtk = LoadPlanDoc();
            bool flipped = false;
            if (brokeAtk != null && brokeAtk.cards != null)
                foreach (var c in brokeAtk.cards)
                {
                    if (flipped) break;
                    if (c == null || c.fields == null) continue;
                    foreach (var f in c.fields)
                    {
                        if (flipped) break;
                        if (f == null || f.items == null) continue;
                        foreach (var it in f.items)
                            if (it != null && it.token == "[Attack]") { it.sprite = "Ranged"; flipped = true; break; }
                    }
                }
            var ctrlBad = AtkNotMelee(brokeAtk);
            Check(flipped && ctrlBad.Count == 1 && AtkNotMelee(doc8).Count == 0,
                  "★ ⑧b【灭自证】⑧a 的谓词**结构上不可能恒绿**：真表 ⇒ 0 条；翻掉一条 `[Attack]` ⇒ " +
                  "必须正好 1 条（实得 " + ctrlBad.Count + " 条）。", ref bad);

            // ⑧c **显示快照等价**：换代前后运行时的显示串必须**逐字节相同**。
            //   机械证明：计划表里 `[Attack]` 全是 `Melee`（⑧a）+ 运行时按同一份计划表替换
            //   ⇒ 对全池 `desc` 跑 `Rewrite`：`[Attack]` 一处都不许残留、且每张卡画出的
            //   `Melee` 枚数 == 计划表 `desc` 子表里 `sprite=Melee` 的条目数（条目是**按处**的）。
            var cards8 = RuleEngine.CardDatabase.Load();
            int snapCards = 0, snapMismatch = 0;
            var snapSample = new List<string>();
            foreach (var c in cards8)
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                if (c.Desc == null || c.Desc.IndexOf("[Attack]", System.StringComparison.Ordinal) < 0) continue;
                snapCards++;
                string r8 = CardPresentation.CardIcons.Rewrite(c.Id, "desc", c.Desc);
                int want = 0;
                if (doc8 != null && doc8.cards != null)
                    foreach (var pcard in doc8.cards)
                    {
                        if (pcard == null || pcard.id != c.Id || pcard.fields == null) continue;
                        foreach (var f in pcard.fields)
                        {
                            if (f == null || f.name != "desc" || f.items == null) continue;
                            foreach (var it in f.items)
                                if (it != null && it.sprite == "Melee") want++;
                        }
                    }
                int got = CountOf(r8, "<sprite name=\"Melee\">");
                bool residual = CardPresentation.CardIcons.StripTags(r8)
                    .IndexOf("[Attack]", System.StringComparison.Ordinal) >= 0;
                if (got != want || residual)
                {
                    snapMismatch++;
                    if (snapSample.Count < 5)
                        snapSample.Add(c.Id + " got=" + got + "/want=" + want + (residual ? " **残留**" : ""));
                }
            }
            Check(snapCards > 0 && snapMismatch == 0,
                  "★ ⑧c **显示快照等价**（`A1771`，零可见变化）：全池 " + snapCards + " 张带 `[Attack]` 的卡，" +
                  "`Rewrite` 后无残留、Melee 枚数与计划表逐卡相等；不一致 **" + snapMismatch + "** 张" +
                  (snapMismatch == 0 ? "" : " → " + string.Join(" / ", snapSample.ToArray())) +
                  "。改坏法：把任一条 `[Attack]` 的 sprite 改成 `Ranged` ⇒ 枚数对不上 ⇒ 红。", ref bad);
            // ⑧c【灭自证】：不存在的卡 id ⇒ 计划表查不到 ⇒ `[Attack]` 必须**原样残留**
            //   ⇒ 证明上面那个「残留」谓词**能看见坏态**（不是恒绿）。
            string missRes = CardPresentation.CardIcons.Rewrite("Z_NO_SUCH_CARD_A1771", "desc", "Give +1 [Attack]");
            Check(CardPresentation.CardIcons.StripTags(missRes)
                      .IndexOf("[Attack]", System.StringComparison.Ordinal) >= 0,
                  "★ ⑧c【灭自证】「计划表查不到 ⇒ `[Attack]` 原样残留」必须为真（谓词能看见坏态）；" +
                  "实测 = `" + missRes + "`", ref bad);

            // ⑧d **`PinnedPlayMd5` 不受影响**（`A1771` 判据 R1）：`工具/ruleprobe/`（含 `PlayProbe.cs` 的
            //   `PinnedPlayMd5` 哨兵）**不许**引用图标计划链 —— 0 引用 ⇒ 换代对它**结构上不可能有影响**。
            //   ⚠️ 不钉 md5 的具体值（⛔ `A2137`：别为了抹红去重钉哨兵）。
            const string probeDirAbs = @"d:/4/Unity/工具/ruleprobe";
            var refs = new List<string>();
            if (Directory.Exists(probeDirAbs))
                foreach (var p in Directory.GetFiles(probeDirAbs, "*.cs", SearchOption.AllDirectories))
                    if (HasPlanRef(File.ReadAllText(p))) refs.Add(Path.GetFileName(p));
            Check(Directory.Exists(probeDirAbs) && refs.Count == 0 && HasPlanRef("// 用了 CardIcons.Rewrite"),
                  "★ ⑧d **`工具/ruleprobe/` 对图标计划链 0 引用**（⇒ `PinnedPlayMd5` 不可能被 `A1771` 影响）：" +
                  (Directory.Exists(probeDirAbs) ? "扫到 " + refs.Count + " 个引用文件" : "**目录不存在**") +
                  (refs.Count == 0 ? "" : " → " + string.Join(" / ", refs.ToArray())) +
                  "；灭自证（合成串必须被同一谓词认出）= " + HasPlanRef("// 用了 CardIcons.Rewrite") +
                  "。改坏法：在 `ruleprobe` 任一 `.cs` 里加一句 `CardIcons` ⇒ 红。", ref bad);

            // ⑧e 幂等（②i 已全池覆盖）：补一条**走官方规则**、`desc` 里有**两处** `[Attack]` 的定点 ——
            //   `SOR60 Holy Fire`：`Give +2 [Attack] to your Warlord and +1 [Attack] to adjacent troops …`
            RuleEngine.CardDef fire = null;
            foreach (var c in cards8)
                if (c != null && c.Id == "SOR60") { fire = c; break; }
            Check(fire != null && !string.IsNullOrEmpty(fire.Desc),
                  "⑧e·前提 从卡池取得到 `SOR60` 的真 `desc`（夹具不自己抄）", ref bad);
            string fire1 = fire == null ? "" : CardPresentation.CardIcons.Rewrite("SOR60", "desc", fire.Desc);
            string fire2 = CardPresentation.CardIcons.Rewrite("SOR60", "desc", fire1);
            // ⑧e【灭自证】：先给每一处 `[Attack]` **预塞**一枚 Melee 图标再跑（模拟「不幂等」的坏态）
            //   ⇒ 枚数必须变成 4（每处多画一枚）⇒ 与判据要求的「正好 2」不等。
            string fireBroken = fire == null ? "" : fire.Desc.Replace("[Attack]", "<sprite name=\"Melee\">[Attack]");
            int fireBrokenN = CountOf(CardPresentation.CardIcons.Rewrite("SOR60", "desc", fireBroken),
                                      "<sprite name=\"Melee\">");
            Check(fire != null && CountOf(fire1, "<sprite name=\"Melee\">") == 2 && fire1 == fire2
                  && fireBrokenN != 2,
                  "★ ⑧e 幂等（`[Attack]` 走官方规则那一支）：`SOR60` **正好 2 处** `[Attack]` 都换成 Melee" +
                  "（实得 " + CountOf(fire1, "<sprite name=\"Melee\">") + " 枚）、且换两遍 == 换一遍；" +
                  "【灭自证】预塞一枚图标再跑 ⇒ 枚数必须 ≠ 2（实得 " + fireBrokenN + " 枚）。" +
                  "改坏法：让规则条目在已替换的串上再命中一次 ⇒ 枚数翻倍 / 串不等 ⇒ 红。实测 = `" + fire1 + "`", ref bad);
        }

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

        // ═════════════════ ⑨ 图标尺度「量出来」的护栏（`CalibScale` 的锚，2026-10-23）═════════════════
        //
        // 🔴 **钉什么**：**卡面效果文字的正文面（`Face.LatinCardBody` = Pragati）下**，
        //   一个行内图标的墨高 ÷ 同一行的**拉丁大写**墨高，必须落在**成品卡图量出来的**比例附近。
        //   · 目标 `1.65` = `Warpforge_12_Aggressor.png` 亲测：`questPoints1` 图标 51 px ÷ 大写 `S`/`G` 31 px
        //     （复量命令写在 `CalibScale` 的 doc 里：阈值 90~110 稳定 50~51）。
        //   · ⛔ **不是**断 `CalibScale` 这个常数本身 —— 断言量的是**渲染出来的几何**，
        //     常数只是它的一条输入（资产里的 `glyph.scale` 才是真正被量的东西）。
        //
        // 🔴 **「图标」那一侧量的是渲染出来的**：那个 sprite 的网格 quad 就是它的 80×80 字形框
        //   （墨占 79/80 ⇒ 用 quad 当墨高只差 1%）。**「大写」那一侧不用网格** ——
        //   字符 quad 会把字形上下的留白也算进去（实测 `H` 的 quad 比 `capLine` 高 ≈11%），
        //   所以直接用**正文字体自己的 `faceInfo`** 折算：
        //   `capH = capLine / pointSize × scale × 0.1 × fontSize` —— 与 TMP 自己算字号的那条式子同源
        //   （`TMP_Text.cs:7183` 的 `fontScale = fontSize/pointSize × scale × 0.1`），**不含任何本项目的常数**。
        //   ⇒ 比值 = `ascentLine / capLine × glyph.scale`（`Pragati` = `70/60 × glyph.scale`）。
        //
        // 🧨 **改坏法（必须能红）**：
        //   · `Build()` 里把 `g.scale * CalibScale` 改回 `g.scale`（= CalibScale 1.0）⇒ 1.67 → **1.98** ⇒ 红；
        //   · 把正文面换回 `TmpFont.Font`（CJK 面）⇒ TMP 走 `TMP_Text.cs:4164` 那一支、图标 ×1.41 ⇒ 2.29 ⇒ 红；
        //   · 改了 `CalibScale` 却**没重跑 `IconSetup.Run`**（资产里还是旧 scale = 0.776）⇒ 量到 **1.54** ⇒ 红
        //     （这条是它最实用的一格：常数与资产必须**成对**改）；
        //   · 字形表里 `questPoints1` 的 `m_Scale` 被改成别的数 ⇒ 红。
        //   ⚠️ **判别力 ≈ ±10%**：它抓「结构性漂移」（换面 / 资产没重建 / 常数改了 10% 以上），
        //     不是小数级别的调参（±3% 以内的调整它盖不住，那也不需要它盖）。
        {
            const float PnpIconOverCap = 1.65f;   // PnP 墨比：`Warpforge_12_Aggressor.png` 51 px ÷ 31 px
            const float TolRel = 0.10f;           // ±10%，见上「判别力」

            float ratio = -1f, iconQ = -1f, capH = -1f, faceAscent = -1f, faceCapLine = -1f;
            var go9 = new GameObject("iconscale_probe");
            try
            {
                var t9 = go9.AddComponent<TextMeshPro>();
                t9.font = CardPresentation.TmpFont.FaceAsset(CardPresentation.TmpFont.Face.LatinCardBody);
                t9.spriteAsset = sa;
                t9.fontSize = CardPresentation.CardView.KeywordFontSize;   // 卡面效果文字的字号（现读 2.3552）
                t9.enableWordWrapping = false;
                // `H` 只是把行撑起来（和 `IconSizeProbe.Measure` 同一写法）；真正量的那个字形在**最后一个**
                t9.text = "H<sprite name=\"questPoints1\">";
                t9.ForceMeshUpdate();
                float s9 = Mathf.Abs(t9.transform.lossyScale.y);
                int si9 = t9.textInfo.characterCount - 1;
                // 前提检查：末尾那个必须**真的是 sprite 字形**（sprite 名写错 ⇒ TMP 不生成那个字符 ⇒ 这里红）
                bool isSprite9 = si9 >= 1 && t9.textInfo.characterInfo[si9].elementType == TMP_TextElementType.Sprite;
                if (!isSprite9) Err("⑨ 前提不成立：`<sprite name=\"questPoints1\">` 没生成 sprite 字形（名字写错 / 资产里没这条）");
                else iconQ = InkQuadH(t9.textInfo, si9) * s9;

                // 大写墨迹高的**解析值**（同一面、同一字号的 TMP 换算，⛔ 不是我们的常数）
                var fi = t9.font != null ? t9.font.faceInfo : new FaceInfo();
                faceAscent = fi.ascentLine;
                faceCapLine = fi.capLine;
                capH = fi.capLine / fi.pointSize * fi.scale * 0.1f * t9.fontSize;
                if (capH > 0f && iconQ > 0f) ratio = iconQ / capH;

                // 顺手把「名义口径」的读数打出来（⛔ 不断言：卡面会过 `FitToBox`，见 `CalibScale` doc）
                Log("⑨ 读数：正文字面 ascent/capLine = " + faceAscent.ToString("F2") + " / " +
                    faceCapLine.ToString("F2") + " · fontSize " + t9.fontSize.ToString("F4") +
                    " · 图标 quad " + iconQ.ToString("F5") + " · 大写（解析）" + capH.ToString("F5") +
                    " · **图标 ÷ 大写 = " + ratio.ToString("F3") + "**（目标 " + PnpIconOverCap.ToString("F2") +
                    " ±" + (TolRel * 100f).ToString("F0") + "%）" +
                    "；换算成卡高：图标 " + (iconQ / CardPresentation.CardView.Height * 100f).ToString("F2") +
                    "% 卡高（**名义值** —— 卡面还要过 `FitToBox`，⛔ 别拿它跟成品图那 4.0% 直接比）");
            }
            catch (System.Exception e)
            {
                Err("⑨ 量不了图标尺度：" + e.GetType().Name + ": " + e.Message);
            }
            finally { Object.DestroyImmediate(go9); }

            Check(ratio > 0f && Mathf.Abs(ratio / PnpIconOverCap - 1f) <= TolRel,
                  "★ ⑨ **图标墨高 ÷ 拉丁大写墨高**（卡面正文面 `LatinCardBody`、卡面效果文字的字号）" +
                  "落在成品卡图的比例附近：实测 **" + ratio.ToString("F3") + "** / 目标 " +
                  PnpIconOverCap.ToString("F2") + "（出处 `Warpforge_12_Aggressor.png`：`questPoints1` 图标 51 px ÷ 大写 `S`/`G` 31 px）" +
                  "· 容差 ±" + (TolRel * 100f).ToString("F0") + "%" +
                  "。🧨 改坏法：① `Build()` 里 `g.scale * CalibScale` 改回 `g.scale`（CalibScale 1.0）⇒ 1.98 ⇒ 红；" +
                  "② 正文面换成 `TmpFont.Font`（CJK 面；`TMP_Text.cs:4164` 用**正文面**的 faceInfo 算图标缩放）⇒ 2.29 ⇒ 红；" +
                  "③ **改了 `CalibScale` 却没重跑 `IconSetup.Run`**（资产里还是旧 scale = 0.776 ⇒ 1.54）⇒ 红 —— " +
                  "先跑 `-executeMethod IconSetup.Run` 重建资产再验；④ 字形表里 `questPoints1` 的 `m_Scale` 被改 ⇒ 红。" +
                  "⚠️ 判别力 ≈ ±10%：抓的是结构性漂移（换面 / 资产没重建 / 常数改了 10% 以上），不是小数级调参。", ref bad);
        }

        // ═════════════ ⑪ 【**只读数、不断言**】图标 ÷ 汉字 —— 两个「面前」各量一遍（`新账-89` / 2026-10-23）═════════════
        //
        // 🔴 **为什么这里【没有 Check】**：本条的目标值（数字客户端实拍的 **1.404**，见 `CalibScale` doc 里
        //   「尺子 B」那一段）与 ⑨ 的目标值（PnP 的 **1.65**）是**两把不同的尺子**，而且
        //   「图标按哪一面的 `ascentLine` 缩放」取决于 TMP 的 `<sprite>` 继承规则
        //   （`TMP_Text.cs:4024-4036`：`<sprite>` 不更新 `m_currentFontAsset` ⇒ 用**前一个字符**的 faceInfo）。
        //   这两个口径**要调度台裁**，在此之前把读数打出来（读数不会「假绿」）；
        //   ⛔ 等裁完再把它改成 `Check` —— 那时判据只有一个，改坏法也写得出来。
        //
        // 📐 **量的是什么**（两条都量**渲染出来的网格 quad**，⛔ 不读常数）：
        //   · 分子 = 末尾那个 `<sprite name="SpiritStone_1">` 的 quad 高（= 它的 80×80 字形框）；
        //   · 分母 = **同一个 TMP 里**那个汉字 `获` 的 quad 高（字典里的 `WorldGlyphPerFontSize` 是同一个量，
        //     这里走网格以确保「数字口径」一致）；
        //   · 两次唯一的差别 = **图标前一个字符是拉丁 `H` 还是汉字 `获`** ⇒ 读数差的就是「面」的那一跳。
        // 🎯 **目标（数字客户端）**：实拍绿环 31 px ÷ 汉字 24 px = 1.292（都是**墨高**）；
        //   换成 **quad/quad** 要各除自己的墨占：图标 0.90（图集里绿环 72/80）· 汉字 0.981（0.93 em ÷ 0.948 em）
        //   ⇒ **目标 quad 比 = 31/0.90 ÷ 24/0.981 = 34.44 ÷ 24.46 = 1.404**。
        //   现读 `CalibScale = 0.842` 下：`H` 那条应给 ≈ **1.20**、`获` 那条应给 ≈ **1.69**
        //   （= 1.4255 × 0.842 与 2.003 × 0.842）—— 两条**一低一高把 1.404 夹在中间**，
        //   这正是「同一个常数配两个面」的必然结果；要落到 1.404（或反过来锚 PnP 的 1.65），只能二选一。
        {
            const float TargetQuadRatio = 1.404f;      // 数字客户端实拍推出来的 quad/quad（推导见上）

            float qLatin = -1f, qCjk = -1f, cjkQuad = -1f, rLatin = -1f, rCjk = -1f;
            string cjkCharFont = "?";      // `获` 实际解析到哪一份资产 —— 兜底没挂上时会在这里露出来（.notdef 方框）
            try
            {
                // ── ① 图标跟在【拉丁】后面（= 原版中文卡里最常见的形状：`获得 + 空格 + 图标`）
                var goL = new GameObject("iconcjk_probe_latin");
                try
                {
                    var tL = goL.AddComponent<TextMeshPro>();
                    tL.font = CardPresentation.TmpFont.FaceAsset(CardPresentation.TmpFont.Face.LatinCardBody);
                    tL.spriteAsset = sa;
                    tL.fontSize = CardPresentation.CardView.KeywordFontSize;
                    tL.textWrappingMode = TextWrappingModes.NoWrap;
                    tL.text = "H<sprite name=\"SpiritStone_1\">";
                    tL.ForceMeshUpdate();
                    int si = tL.textInfo.characterCount - 1;
                    if (si >= 1 && tL.textInfo.characterInfo[si].elementType == TMP_TextElementType.Sprite)
                        qLatin = InkQuadH(tL.textInfo, si) * Mathf.Abs(tL.transform.lossyScale.y);
                }
                finally { Object.DestroyImmediate(goL); }

                // ── ② 图标跟在【汉字】后面（中文卡里紧贴写法就是这一种；TMP 会拿 CJK 兜底面的 faceInfo）
                var goC = new GameObject("iconcjk_probe_cjk");
                try
                {
                    var tC = goC.AddComponent<TextMeshPro>();
                    tC.font = CardPresentation.TmpFont.FaceAsset(CardPresentation.TmpFont.Face.LatinCardBody);
                    tC.spriteAsset = sa;
                    tC.fontSize = CardPresentation.CardView.KeywordFontSize;
                    tC.textWrappingMode = TextWrappingModes.NoWrap;
                    tC.text = "获<sprite name=\"SpiritStone_1\">";
                    tC.ForceMeshUpdate();
                    float s = Mathf.Abs(tC.transform.lossyScale.y);
                    int si = tC.textInfo.characterCount - 1;
                    if (tC.textInfo.characterCount >= 2)
                    {
                        cjkQuad = InkQuadH(tC.textInfo, 0) * s;      // `获` 的字形框（同一个 TMP、同一个字号）
                        var cf = tC.textInfo.characterInfo[0].fontAsset;
                        cjkCharFont = cf != null ? cf.name : "null";
                    }
                    if (si >= 1 && tC.textInfo.characterInfo[si].elementType == TMP_TextElementType.Sprite)
                        qCjk = InkQuadH(tC.textInfo, si) * s;
                }
                finally { Object.DestroyImmediate(goC); }

                if (cjkQuad > 0f && qLatin > 0f) rLatin = qLatin / cjkQuad;
                if (cjkQuad > 0f && qCjk > 0f) rCjk = qCjk / cjkQuad;
                float curC = CalibScale;
                Log("⑪ 读数（**只读、不断言**）：`CalibScale` 现读 " + curC.ToString("F3") +
                    " · 汉字 `获`（解析到 `" + cjkCharFont + "`）的字形框 " + cjkQuad.ToString("F5") +
                    " · 图标 quad：跟拉丁后 **" + qLatin.ToString("F5") + "**（比 " + rLatin.ToString("F3") + "）·" +
                    " 跟汉字后 **" + qCjk.ToString("F5") + "**（比 " + rCjk.ToString("F3") + "）" +
                    " —— 目标（数字客户端实拍 `获得①` 换算成 quad 比）= **" + TargetQuadRatio.ToString("F3") + "**" +
                    "；要落到目标上，`CalibScale` 应各为 " +
                    (rLatin > 0f ? (curC * TargetQuadRatio / rLatin).ToString("F3") : "?") + "（拉丁面） / " +
                    (rCjk > 0f ? (curC * TargetQuadRatio / rCjk).ToString("F3") : "?") + "（CJK 面）");
                Log("⑪ 判读：两条读数**差 1.41 倍**（= Pragati `ascentLine×0.1/95 = 0.07368` ÷ CJK `57.55×0.9×0.1/50 = 0.10359`）" +
                    " ⇒ 原版到底是哪一面，看实拍对不上的那一条就知道；⛔ 在这个数被裁清楚之前别动 `CalibScale`。");
            }
            catch (System.Exception e)
            {
                Log("⑪ 读数失败（不影响其它断言）：" + e.GetType().Name + ": " + e.Message);
            }
        }

        // ═════════════ ⑫ 本批（`新账-90`）修掉的三条：各自的「**改回去 ⇒ 红**」判别式（2026-10-23）═════════════════
        //
        // 🔴 **为什么要单开这一段**：`IconSetup.Run` 那 5 条既有红在 2026-10-23 修完之后，
        //   主断言（`②i` / `④a` / `④b`）**只证明了「今天是绿的」**；「改回去会不会红」原来只写在注释里
        //   （`🧨 改坏哪里会让它红`）—— **注释挡不住回归**。
        //   而且有一条**结构上的盲区**：`④a` / `④b` **两侧读的都是计划表** ⇒
        //   「**卡池原文改了、计划表没重跑**」这一格它们**看不见**（那一格正是 `ASH83` 修法的形状）。
        // ⇒ 本段给三条各配一条**结构上不可能同时满足**的断言：一半断「今天对」、另一半断
        //   「**把坏态喂回同一个谓词，必须给出另一个读数**」—— 谓词恒真 / 恒假都过不了。
        // ⛔ 别把本段并进 `②i` / `④a`（那两条是判据本体、本段是它们的哨兵）· ⛔ 别动 ⑨ / ⑪（尺子未裁 / 只读数）。
        // 📌 「抽掉计划表里那一枚 ⇒ ④a 必须认出来」那一半**已经由 ④b 覆盖**（`faith` / `waystone` 都在它的控制集里）
        //    —— 本段补的是**它看不见的那一格**（计划表 vs 原文）与**运行时文本**那一段。
        {
            // ───── ⑫a `②i` 的检测器（`D1` 修法）：**两档 sprite 名，各用在哪一侧** ─────
            //   今天的形状（`r14` 实测）：`desc` 走计划表 ⇒ **短名** `<sprite name="oath">`；
            //   `descZh` 走官方 `[[oath]]` ⇒ **全名** `<sprite name="Atlas_trait_icon_oath">`
            //   （`RewriteMarkTraits`，判据 = `Core/CardIcons.cs` 那一支；两档名字在 `Build()` 里注册成**同一个字形**）。
            //   🧨 **两半结构上不可能同时满足**：
            //     · 把 `CountSprite` 改回「只数短名」⇒ `②i` 那侧的 `nZ` 归 0 ⇒ **②i 红**（2026-10-23 修之前就是 `0/1`·`0/2`·`0/2`）；
            //     · 把中文那侧改成写短名（或把 Oath 的别名从 `Build()` 里删掉）⇒ **本条的「哪一侧为 0」红**。
            {
                int cShort = CountSprite("<sprite name=\"oath\">x", "oath");
                int cLong = CountSprite("<sprite name=\"Atlas_trait_icon_oath\">x", "oath");
                int cBleed = CountSprite("<sprite name=\"oathless\">x", "oath");
                int iLong = IndexOfSprite("<sprite name=\"Atlas_trait_icon_oath\">", "oath");
                Check(cShort == 1 && cLong == 1 && cBleed == 0 && iLong == 0,
                      "★ ⑫a【灭自证】`CountSprite` / `IndexOfSprite` 必须**两档名字都数、且不按前缀误吃**：" +
                      "短名=1 / 全名=1 / `oathless`=0 / 全名的下标=0 —— 实得 " + cShort + " / " + cLong +
                      " / " + cBleed + " / " + iLong + "。🧨 改坏法：把 `CountSprite` 只留短名那一项 ⇒ 全名=0 ⇒ 红。", ref bad);

                string[] oathIds = { "UM100", "UM106", "UM93" };
                int enShort = 0, enLong = 0, zhShort = 0, zhLong = 0, seen = 0;
                foreach (var oid in oathIds)
                {
                    var ocd = PoolCard(oid);
                    if (ocd == null || string.IsNullOrEmpty(ocd.Desc) || string.IsNullOrEmpty(ocd.DescZh)) continue;
                    seen++;
                    string od = CardPresentation.CardIcons.Rewrite(oid, "desc", ocd.Desc);
                    string oz = CardPresentation.CardIcons.Rewrite(oid, "descZh", ocd.DescZh);
                    enShort += CountOf(od, "<sprite name=\"oath\">");
                    enLong += CountOf(od, "<sprite name=\"Atlas_trait_icon_oath\">");
                    zhShort += CountOf(oz, "<sprite name=\"oath\">");
                    zhLong += CountOf(oz, "<sprite name=\"Atlas_trait_icon_oath\">");
                }
                Check(seen == oathIds.Length && enShort > 0 && zhLong > 0 && enLong == 0 && zhShort == 0
                      && enShort == zhLong,
                      "★ ⑫a【灭自证】`②i` 那三张的**两档名字各在哪一侧**：英文侧只该有**短名**（短 " + enShort +
                      " / 全 " + enLong + "）、中文侧只该有**全名**（短 " + zhShort + " / 全 " + zhLong +
                      "），且两侧枚数必须相等（" + enShort + " == " + zhLong + "，夹具 " + seen + "/" + oathIds.Length + " 张）。" +
                      "🧨 只数一档 ⇒ 另一侧恒 0 ⇒ 上面 `②i` 必红；把中文侧改成写短名 ⇒ 本条红。", ref bad);
            }

            // ───── ⑫b 计划表 ↔ **今天的卡池原文**：每条 token 都还得在原文里找得到 ─────
            //   🔴 **这是 `④a` / `④b` 结构上的盲区**：它们两侧读的都是计划表 ⇒「**数据改了、计划表没重跑**」
            //      那一格它们看不见（计划表自己跟自己比仍然自洽）。而 `ASH83` 的修法正是
            //      「改 `cardface_fixes.json` → **必须**重跑 `gen_cards_engine.py` + `gen_icon_plan.py`」——
            //      少跑最后一步，`④a` 照样绿，而计划表里那条 token **已经不在正文里**（= 空转条目、卡面画不出来）。
            //   🧨 改坏法：改任何一张卡的 `desc` / `descZh` 却不重跑生成器 ⇒「找不到」的条数 > 0 ⇒ 红。
            //   灭自证：同一条谓词喂一份**只塞一条假 token** 的合成计划表 ⇒ 必须**正好报 1 条**
            //      （谓词恒绿 ⇒ 这里 0 条 ⇒ 红；谓词恒红 ⇒ 真表那半先红，两处不可能同时满足）。
            {
                var planDoc = LoadPlanDoc();
                List<string> missingCards;
                var missTok = PlanTokensMissingInPool(planDoc, out missingCards);
                int scanned = CountPlanTokens(planDoc);
                // 合成表：真卡 + **绝不可能出现在任何卡面里的串** ⇒ 谓词必须认出来
                var fakeDoc = new PlanDoc
                {
                    version = 1,
                    cards = new[]
                    {
                        new PlanCard
                        {
                            id = "ASH83",
                            fields = new[]
                            {
                                new PlanField
                                {
                                    name = "descZh",
                                    items = new[] { new PlanItem { token = ProbeAbsentToken, sprite = "faith" } }
                                }
                            }
                        }
                    }
                };
                List<string> fakeMissingCards;
                var fakeMiss = PlanTokensMissingInPool(fakeDoc, out fakeMissingCards);
                string missTxt = missTok.Count == 0 ? "" : ("；例：" + string.Join(" / ",
                    missTok.GetRange(0, System.Math.Min(6, missTok.Count)).ToArray()));
                Check(scanned > 100 && missTok.Count == 0 && missingCards.Count == 0
                      && fakeMiss.Count == 1 && fakeMissingCards.Count == 0,
                      "★ ⑫b【灭自证】计划表里**每一条 token 都还得在今天的卡池原文里**：扫了 " + scanned +
                      " 条 · 找不到 **" + missTok.Count + "** 条 · 计划表里有而卡池里没有的卡 " + missingCards.Count +
                      " 张" + missTxt + "。这条补的是 `④a` / `④b` 的盲区（它们两侧都读计划表 ⇒「数据改了、计划表没重跑」看不见）。" +
                      "🧨 改坏法：改 `cardface_fixes.json` 的 `descZh` 却不重跑 `gen_cards_engine.py` + `gen_icon_plan.py` ⇒ 红。" +
                      "灭自证：同一谓词喂「一条假 token」的合成表 ⇒ 必须正好 1 条（实得 " + fakeMiss.Count + " 条）。", ref bad);
            }

            // ───── ⑫c 数据侧那一条（`ASH83|waystone`）：**端到端**（中文正文 + 计划表 + 替换链）─────
            //   ①今天：`Rewrite("ASH83","descZh", 卡池真值)` 必须画得出 `waystone`；
            //   ②灭自证：把**修之前那一版中文**（2026-10-23 的原值，见 `ASH83DescZhBeforeFix`）喂给同一个 `Rewrite`
            //     ⇒ 必须 **0 枚**。两半结构上不可能同时满足 ⇒「谓词恒真」与「恒假」都过不了。
            //   📌 这一条也把「计划表有那一枚、正文里却没有」的**空转**挡在外面（`ASH83` 那枚 token = `路标石。`）。
            {
                var acd = PoolCard("ASH83");
                string aNow = acd == null || string.IsNullOrEmpty(acd.DescZh)
                    ? null : CardPresentation.CardIcons.Rewrite("ASH83", "descZh", acd.DescZh);
                string aOld = CardPresentation.CardIcons.Rewrite("ASH83", "descZh", Ash83DescZhBeforeFix);
                int nNow = aNow == null ? -1 : CountSprite(aNow, "waystone");
                int nOld = CountSprite(aOld, "waystone");
                Check(nNow >= 1 && nOld == 0,
                      "★ ⑫c【灭自证】`ASH83` 的 `waystone` 徽记**来自中文正文那一句**：今天的 `descZh` 换出 **" + nNow +
                      "** 枚；把 `路标石。` 那一句去掉（= 2026-10-23 修之前的原值）⇒ 必须 **0** 枚（实得 " + nOld +
                      "）。🧨 改坏法：删掉 `cardface_fixes.json` 里 `descZh | Autarch` 的 `路标石。` ⇒ 重跑生成器后 `④a` 红；" +
                      "**没重跑生成器**时由本条红（计划表那一枚变空转）。实测：" + (aNow ?? "(卡池里取不到 ASH83)"), ref bad);
            }

            // ───── ⑫d 生成器那一条（`f104…|faith`）：**端到端**（运行时 `RewriteFaithCost`）─────
            //   ①今天：`Rewrite("f104…","descZh", 卡池真值)` 必须有 `faith` —— 那一枚是**运行时按句法画的**
            //     （`Core/CardIcons.cs` 的 `RewriteFaithCost`，在计划表之前跑），计划表那条 `4 能量` **恒空转**；
            //   ②灭自证：把那句里的 `能量` 去掉（`4 能量：` → `4 ：`）⇒ 同一支必须画 **0 枚**。
            //   ⇒「恒真」过不了 ②、「恒假」过不了 ①。
            {
                const string faithCardId = "f104d4c21abe84546abc9365d9eda8dd";
                var fcd = PoolCard(faithCardId);
                string fNow = fcd == null || string.IsNullOrEmpty(fcd.DescZh)
                    ? null : CardPresentation.CardIcons.Rewrite(faithCardId, "descZh", fcd.DescZh);
                string fNoWord = fcd == null || string.IsNullOrEmpty(fcd.DescZh) ? null : fcd.DescZh.Replace("能量", "");
                string fOld = fNoWord == null ? null : CardPresentation.CardIcons.Rewrite(faithCardId, "descZh", fNoWord);
                int fN = fNow == null ? -1 : CountSprite(fNow, "faith");
                int fO = fOld == null ? -1 : CountSprite(fOld, "faith");
                Check(fN >= 1 && fO == 0,
                      "★ ⑫d【灭自证】`" + faithCardId + "` 的 `faith` 徽记**来自运行时的「数字 + 能量 + 冒号」句法**：" +
                      "今天的 `descZh` 换出 **" + fN + "** 枚；把那句里的 `能量` 去掉（`4 能量：` → `4 ：`）⇒ 必须 **0** 枚（实得 " +
                      fO + "）。🧨 改坏法：① 让生成器不再镜像这条句法（`gen_icon_plan.py` 的 `FAITH_COST`）⇒ `④a` 红；" +
                      "② 把计划表 token 缩成光秃秃的 `能量` ⇒ 运行时会在「获得 1 能量」那类资源动作上凭空插图标 ⇒ 本条核不到、" +
                      "但 `gen_icon_plan.py --check` 会红（那条判据在生成器自己那边）。实测：" + (fNow ?? "(卡池里取不到那张卡)"), ref bad);
            }
        }

        // ═════════════ ⑬ `<sprite>` 的「面」不随前一个字符变（卡牌线 `新账-100` · 2026-10-23）═════════════════
        //
        // 🔴 **钉什么**：**同一个图标，前面是汉字 / 中文标点 / 拉丁 / 空格，渲染出来的 quad 必须一样大**。
        //   这不是尺寸调参 —— 它钉的是 `Core/CardIcons.PinSpriteFace` 那条**结构性**约束
        //   （⛔ 不动 `CalibScale`：尺子那一笔在别处；本条只让尺寸与「前一个字符」无关）。
        //   病灶（TMP 的 sprite 那一支：sprite asset 的 `faceInfo.pointSize == 0` 时用 `m_currentFontAsset`
        //   的 faceInfo 算缩放，而 `<sprite>` 不更新 `m_currentFontAsset`）→ 判据全文写在
        //   `CardIcons.PinSpriteFace` 的 doc 里，⛔ 别在这儿抄第二份。
        //
        // 📐 **量的是渲染出来的网格 quad**（`InkQuadH`），⛔ 不读常数：
        //   · **夹具**：卡池里**真卡**的中文正文（走卡面唯一那条组字路 `BattleDriver.FaceTextFull`），
        //     取**第一枚「紧跟在汉字/中文标点后面」的有效图标**，同一字号下量四份：
        //       ① 不钉面（= 今天之前的形状） ② 钉面（= `CardView.Fill` 今天的形状）
        //       ③ 钉面 + 那枚图标前插一个普通空格 ④ 同一枚图标跟在拉丁 `H` 后面（= Pragati 面的参照）
        //     ⇒ ② ÷ ③ 必须 ≈ 1.000（**图标大小与前面的空格无关**）· ② ÷ ④ 必须 ≈ 1.000（**钉的是拉丁面**）
        //   · **灭自证**：① ÷ ② 必须 **> 1.20**（不钉面时继承 CJK 面 ⇒ 明显更大；理论 1.406）。
        //     把 `CardView.Fill` 里那句 `PinSpriteFace` 删掉 ⇒ ② 读成 ①、第一条读到 **1.406** ⇒ 红。
        //     ⚠️ 若哪天 sprite asset 自己带上 faceInfo（`pointSize > 0`，那是**尺子 / `CalibScale` 那一笔**），
        //        「继承」这件事从根本上不成立 ⇒ 本条**自动跳过那半条**（打印理由），⛔ 那不是放宽。
        //   · **端到端**（wiring）：真建一个 `CardView`，它的关键词层文本里必须**真的有** `<font=Default><sprite`；
        //     那一层够 2 枚时再顺带断言「同一层所有 sprite 的 quad 相差 ≤ 2%」——
        //     防「探针量的和卡面用的不是同一条路」。
        {
            const float Tol13 = 0.02f;            // ±2%（简报要的）
            var pinFace = CardPresentation.TmpFont.FaceAsset(CardPresentation.TmpFont.Face.LatinCardBody);
            Check(pinFace != null, "⑬ 前提：卡面正文面（`Face.LatinCardBody` = Pragati）取得到", ref bad);

            // ① 挑夹具：卡池里第一张「中文正文里有一枚**有效**图标紧跟在汉字/中文标点后面」的卡。
            //    两遍：**先要 ≥2 枚有效图标**（端到端那条能顺带量「同一层里大小一致」），
            //    没有就退到 ≥1 枚（端到端改断 wiring；读数那三条一条都不少）。
            string fid = null, fraw = null, fname = null;
            int ford = -1, fvalid = 0;
            if (pinFace != null)
            {
                for (int pass = 0; pass < 2 && fid == null; pass++)
                {
                    int need = pass == 0 ? 2 : 1;
                    foreach (var c in RuleEngine.CardDatabase.Load())
                    {
                        if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                        string t = CardPresentation.BattleDriver.FaceTextFull(c).body;
                        if (string.IsNullOrEmpty(t)) continue;
                        int valid = 0, hitOrd = -1; string hitName = null;
                        for (int at = t.IndexOf("<sprite", System.StringComparison.Ordinal); at >= 0;
                             at = t.IndexOf("<sprite", at + 1, System.StringComparison.Ordinal))
                        {
                            string nm = SpriteNameAt(t, at);
                            if (string.IsNullOrEmpty(nm) || sa.GetSpriteIndexFromName(nm) < 0) continue;  // TMP 生成不出字形的那种
                            if (hitOrd < 0 && IsCjkChar(PrevVisibleChar(t, at))) { hitOrd = valid; hitName = nm; }
                            valid++;
                        }
                        if (hitOrd >= 0 && valid >= need)
                        { fid = c.Id; fraw = t; fname = hitName; ford = hitOrd; fvalid = valid; break; }
                    }
                }
            }
            Check(fid != null,
                  "⑬ 前提：卡池里找得到一张「中文正文里图标**紧跟在汉字后面**」的卡（夹具）" +
                  "—— ⛔ 换数据后这里红了 = 要**重新挑一张**（挑法见上一行注释），**不是把判据放宽**", ref bad);
            if (fid != null)
                Log("⑬ 夹具：`" + fid + "` · 全文有 " + fvalid + " 枚有效图标（≥2 才能量「同一层大小一致」那一半）");

            // ② 四种排法各量一次
            float qRaw = -1f, qPin = -1f, qSpace = -1f, qLatin = -1f;
            if (fid != null && pinFace != null)
            {
                string pinned = CardPresentation.CardIcons.PinSpriteFace(fraw);
                const string open = "<font=Default>";
                int at2 = FindNthSprite(pinned, ford, sa);
                string spaced = null;
                if (at2 >= 0)
                    spaced = (at2 >= open.Length &&
                              string.CompareOrdinal(pinned, at2 - open.Length, open, 0, open.Length) == 0)
                           ? pinned.Insert(at2 - open.Length, " ")     // 插在 `<font=…>` 之前 = 那枚图标「前面有个空格」
                           : pinned.Insert(at2, " ");
                qRaw = MeasureSpriteQuad(fraw, pinFace, sa, ford);
                qPin = MeasureSpriteQuad(pinned, pinFace, sa, ford);
                qSpace = spaced == null ? -1f : MeasureSpriteQuad(spaced, pinFace, sa, ford);
                qLatin = MeasureSpriteQuad("H<sprite name=\"" + fname + "\">", pinFace, sa, 0);
                Log("⑬ 读数：夹具 `" + fid + "` · 图标 `" + fname + "`（正文第 " + ford + " 枚有效图标）· quad " +
                    "不钉面 " + qRaw.ToString("F5") + " · 钉面 " + qPin.ToString("F5") +
                    " · 钉面+空格 " + qSpace.ToString("F5") + " · 拉丁 `H` 后 " + qLatin.ToString("F5"));
            }

            Check(qPin > 0f && qSpace > 0f && Mathf.Abs(qPin / qSpace - 1f) <= Tol13,
                  "★ ⑬ **图标大小与它前面有没有空格无关**（`" + fid + "` · `" + fname + "`）：钉面后 **无空格 " +
                  qPin.ToString("F5") + " ÷ 有空格 " + qSpace.ToString("F5") + " = " +
                  (qSpace > 0f ? (qPin / qSpace).ToString("F3") : "?") + "**（目标 1.000 ±2%）" +
                  "。🧨 改坏法：删掉 `CardView.Fill` 里那句 `PinSpriteFace` ⇒ 两枚回到「CJK 面 vs Pragati 面」= **1.406** ⇒ 红", ref bad);
            Check(qPin > 0f && qLatin > 0f && Mathf.Abs(qPin / qLatin - 1f) <= Tol13,
                  "★ ⑬ 钉的是**拉丁正文面**（不是「两边都钉成 CJK」那种假绿）：跟汉字后 " + qPin.ToString("F5") +
                  " ÷ 跟拉丁 `H` 后 " + qLatin.ToString("F5") + " = " +
                  (qLatin > 0f ? (qPin / qLatin).ToString("F3") : "?") + "（目标 1.000 ±2%）", ref bad);
            if (sa.faceInfo.pointSize <= 0f)
                Check(qRaw > 0f && qPin > 0f && qRaw > qPin * 1.20f,
                      "★ ⑬【灭自证】**不钉面**时那枚图标明显更大（继承 CJK 面）：不钉 " + qRaw.ToString("F5") +
                      " ÷ 钉住 " + qPin.ToString("F5") + " = " + (qPin > 0f ? (qRaw / qPin).ToString("F3") : "?") +
                      "（**必须 > 1.20**，理论 1.406）。⇒ 把 `CardView.Fill` 那句 `PinSpriteFace` 删掉，上面第一条读到 1.406 ⇒ 红", ref bad);
            else
                Log("⑬ 跳过「灭自证」那半条：sprite asset 自带 faceInfo（pointSize=" + sa.faceInfo.pointSize +
                    "）⇒ 面继承这件事本来就不成立（那是尺子 / `CalibScale` 那一笔账）");

            // ③ 端到端（wiring）：真建一个 `CardView`，量它的关键词层
            if (fid != null && pinFace != null)
            {
                var root = new GameObject("pinface_cardview_probe");
                try
                {
                    var cd = PoolCard(fid);
                    var view = cd == null ? null : CardPresentation.CardView.Create(
                        root.transform, CardPresentation.BattleDriver.ToCardData(cd, cd.Faction), "PinFaceProbe");
                    TextMeshPro kt = null;
                    if (view != null)
                        foreach (var tt in view.GetComponentsInChildren<TextMeshPro>(true))
                            if (tt.text != null && tt.text.IndexOf("<sprite", System.StringComparison.Ordinal) >= 0)
                            { kt = tt; break; }
                    bool wired = kt != null &&
                                 kt.text.IndexOf("<font=Default><sprite", System.StringComparison.Ordinal) >= 0;
                    int n = 0; float mn = float.MaxValue, mx = 0f;
                    if (kt != null)
                    {
                        var ti = kt.textInfo;
                        float sc = Mathf.Abs(kt.transform.lossyScale.y);
                        for (int i = 0; ti != null && i < ti.characterCount; i++)
                        {
                            if (ti.characterInfo[i].elementType != TMP_TextElementType.Sprite) continue;
                            float h = InkQuadH(ti, i) * sc;
                            if (h <= 0f) continue;
                            n++; if (h < mn) mn = h; if (h > mx) mx = h;
                        }
                    }
                    Check(wired,
                          "★ ⑬【端到端】真 `CardView` 的关键词层文本里**图标都钉住了**（含 `<font=Default><sprite`）" +
                          "。🧨 改坏法：`CardView.Fill` 不再调 `PinSpriteFace` ⇒ 卡面文本里**没有** `<font=Default>` ⇒ 红", ref bad);
                    if (n >= 2)
                        Check(mx / mn <= 1f + Tol13,
                              "★ ⑬【端到端】同一张卡的关键词层里 **" + n + "** 枚图标**一样大**（最大 " +
                              mx.ToString("F5") + " ÷ 最小 " + mn.ToString("F5") + " = " +
                              (mn > 0f ? (mx / mn).ToString("F3") : "?") + "，目标 ≤ 1.02）" +
                              "。🧨 改坏法：不钉面时被中文 `。` 隔开的那几枚回到 1.406 ⇒ 红", ref bad);
                    else
                        Log("⑬ 端到端「同一层大小一致」那半条今天量不到：这张卡的关键词层只有 " + n +
                            " 枚图标（夹具走的是「≥1 枚」那一档 —— 读数那三条不受影响）");
                }
                catch (System.Exception e)
                {
                    Err("⑬ 端到端量不了 `CardView`：" + e.GetType().Name + ": " + e.Message);
                }
                finally { Object.DestroyImmediate(root); }
            }
        }

        Finish(bad);
    }

    /// <summary>`text[at..]` 那个 `&lt;sprite …&gt;` 的**图名**（带引号 / 不带引号两种写法都认）；认不出 ⇒ null。
    /// 判据：原版标签写作不带引号的 `&lt;sprite name=Atlas_trait_icon_x&gt;`，我们的两口都补成带引号，
    /// 两条路并存 ⇒ 这里两种都得认（⑬ 拿它判「这枚图 TMP 生成得出来吗」）。</summary>
    static string SpriteNameAt(string text, int at)
    {
        if (string.IsNullOrEmpty(text) || at < 0 || at + 7 > text.Length) return null;
        if (string.CompareOrdinal(text, at, "<sprite", 0, 7) != 0) return null;
        int gt = text.IndexOf('>', at + 7);
        if (gt < 0) return null;
        int ni = text.IndexOf("name=", at + 7, System.StringComparison.OrdinalIgnoreCase);
        if (ni < 0 || ni > gt) return null;
        int v = ni + 5;
        if (v >= gt) return null;
        if (text[v] == '"')
        {
            int e = text.IndexOf('"', v + 1);
            return e > v && e < gt ? text.Substring(v + 1, e - v - 1) : null;
        }
        int sp = text.IndexOfAny(new[] { ' ', '>', '\t' }, v);
        if (sp < 0 || sp > gt) sp = gt;
        return sp > v ? text.Substring(v, sp - v) : null;
    }

    /// <summary>`text` 里第 `k` 枚**有效**（图名在 `sa` 里、TMP 真生成得出字形）`&lt;sprite …&gt;` 的下标；没有 ⇒ -1。
    /// ⚠️ 必须按「有效」数：图名认不出的 `<sprite>` 会被 TMP 当**普通文字**印出来（不产生 sprite 元素），
    /// 拿它数序号会让「字符串里第 k 枚」与「网格里第 k 枚」错位。</summary>
    static int FindNthSprite(string text, int k, TMP_SpriteAsset sa)
    {
        if (string.IsNullOrEmpty(text) || k < 0) return -1;
        int n = 0;
        for (int at = text.IndexOf("<sprite", System.StringComparison.Ordinal); at >= 0;
             at = text.IndexOf("<sprite", at + 1, System.StringComparison.Ordinal))
        {
            string nm = SpriteNameAt(text, at);
            if (string.IsNullOrEmpty(nm) || sa == null || sa.GetSpriteIndexFromName(nm) < 0) continue;
            if (n++ == k) return at;
        }
        return -1;
    }

    /// <summary>`at` 之前**最后一个真正的字符**（跳过紧贴着的 `&lt;…&gt;` 标签）；没有 ⇒ `'\0'`。
    /// ⚠️ 判「面」要的是**字符**不是标签：`&lt;sprite&gt;` 继承的是 `m_currentFontAsset`，而一堆标签
    /// （`&lt;link=…&gt;` / `&lt;nobr&gt;` / `&lt;b&gt;`）不改它 ⇒ 得跳过它们往前找一个真字符。</summary>
    static char PrevVisibleChar(string text, int at)
    {
        int i = at - 1;
        while (i >= 0)
        {
            if (text[i] == '>')
            {
                int lt = text.LastIndexOf('<', i);
                if (lt >= 0) { i = lt - 1; continue; }
            }
            return text[i];
        }
        return '\0';
    }

    /// <summary>汉字 / 中文标点（拉丁那三份面都没有这些字形 ⇒ TMP 会落到 CJK 兜底）。</summary>
    static bool IsCjkChar(char c)
    {
        return (c >= 0x2E80 && c <= 0x9FFF) || (c >= 0xF900 && c <= 0xFAFF) || (c >= 0xFF00 && c <= 0xFFEF);
    }

    /// <summary>拿 `text` 建一个**卡面正文口径**的 TMP（同一面 / 同一字号 / 不折行），量第 `k` 枚 sprite 的
    /// 网格 quad 高（× `lossyScale`）。量不到 ⇒ -1。</summary>
    static float MeasureSpriteQuad(string text, TMP_FontAsset face, TMP_SpriteAsset sa, int k)
    {
        if (string.IsNullOrEmpty(text) || face == null) return -1f;
        var go = new GameObject("pinface_probe");
        try
        {
            var t = go.AddComponent<TextMeshPro>();
            t.font = face;
            t.spriteAsset = sa;
            t.fontSize = CardPresentation.CardView.KeywordFontSize;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = text;
            t.ForceMeshUpdate();
            var ti = t.textInfo;
            if (ti == null) return -1f;
            float sc = Mathf.Abs(t.transform.lossyScale.y);
            int n = 0;
            for (int i = 0; i < ti.characterCount; i++)
            {
                if (ti.characterInfo[i].elementType != TMP_TextElementType.Sprite) continue;
                if (n++ != k) continue;
                return InkQuadH(ti, i) * sc;
            }
            return -1f;
        }
        finally { Object.DestroyImmediate(go); }
    }

    /// <summary>
    /// 第 `i` 个字符的**网格 quad 高**（局部单位；调用方自己乘 `lossyScale`）。
    /// 顶点布局照 TMP：每字符 4 个顶点（左下/左上/右上/右下），`characterInfo[i].vertexIndex` 是它的起点。
    /// ⚠️ 这是 **quad**，不是墨迹：对 **sprite** 它 = 那个字形框（80×80，墨占 79/80 ⇒ 差 1%）；
    /// 对**字符**它会带上字形上下的留白（实测 `H` 的 quad 比 `capLine` 高 ≈11%）——
    /// ⑨ 那条断言**只拿它量 sprite**，字符那一侧改用 `faceInfo` 解析折算（理由见那里的注释）。
    /// </summary>
    static float InkQuadH(TMP_TextInfo ti, int i)
    {
        if (ti == null || i < 0 || i >= ti.characterCount) return -1f;
        var ci = ti.characterInfo[i];
        int vi = ci.vertexIndex, mi = ci.materialReferenceIndex;
        if (mi < 0 || mi >= ti.meshInfo.Length) return -1f;
        var v = ti.meshInfo[mi].vertices;
        if (vi < 0 || vi + 3 >= v.Length) return -1f;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int k = 0; k < 4; k++) { float y = v[vi + k].y; if (y < minY) minY = y; if (y > maxY) maxY = y; }
        return maxY - minY;
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

    /// <summary>
    /// 一个徽记在卡面文字里**画了几枚** —— **短名与 `Atlas_trait_icon_*` 全名都算**。
    ///
    /// 🔴 **为什么必须数两档**（2026-10-23 · 卡牌线 `D1` 现核）：
    ///   `Build()` 把两套名字注册到**同一个字形**上（[`Build` 上方文件头「别名」那一段] 的落地）——
    ///   现读 `Resources/Fonts/Warpforge Trait TextSprites.asset`：
    ///   `oath` 与 `Atlas_trait_icon_oath` **都是 `m_GlyphIndex: 91` / `m_Scale: 1.65`**
    ///   （`talent` / `Atlas_trait_icon_talent` 同为 `:15`）。而**两条渲染路各写一档**：
    ///     · **官方 I2 那一代**（`descZh` 里的 `[[oath]]`）走 `CardIcons.RewriteMarkTraits`
    ///       ⇒ 原版 `GetTraitTextSprite` 拼的是**全名** `<sprite name=Atlas_trait_icon_oath>`
    ///       （`Core/CardIcons.cs:747-750`、`CardTraitCollection__GetTraitTextSprite.c`）;
    ///     · **我们自造那一代**（计划表 `card_icon_plan.json`）走 `CardIcons.Rewrite` 的方括号/裸词两支
    ///       ⇒ 写的是**短名** `<sprite name="oath">`。
    ///   ⇒ 只数一档会把**另一半**误判成「没画」（2026-10-23 实测：②i 三条全红，中文那侧其实都画着）。
    ///   ⚠️ 别名等价性由本文件 `Build()` 保证（`AliasImageName` / 两张同名表），⛔ 不是「看得差不多」。
    /// </summary>
    static int CountSprite(string s, string shortName)
    {
        return CountOf(s, "<sprite name=\"" + shortName + "\">")
             + CountOf(s, "<sprite name=\"Atlas_trait_icon_" + shortName + "\">");
    }

    /// <summary>同上：某个徽记**第一次**出现的位置（两档名字取较小的那个下标；都没有 = -1）。</summary>
    static int IndexOfSprite(string s, string shortName)
    {
        int a = s.IndexOf("<sprite name=\"" + shortName + "\">", System.StringComparison.Ordinal);
        int b = s.IndexOf("<sprite name=\"Atlas_trait_icon_" + shortName + "\">", System.StringComparison.Ordinal);
        if (a < 0) return b;
        if (b < 0) return a;
        return System.Math.Min(a, b);
    }

    // ─────────────── ④ / ⑤ / ⑥ 用的常量与纯函数（`A1409` / `A1370` / `A1421` / `A1423`）───────────────

    /// <summary>
    /// ④ / ⑤ / ⑥ 读的是**运行时那一份**计划表（`Resources/card_icon_plan.json` —— `CardIcons` 取的就是它）。
    /// ⚠️ 两份 `card_icon_plan.json`（`数据/游戏数据/` 与 `Resources/`）是**同一个生成器一起写的**，
    /// 上面 `②` 读的是 `数据/` 那份、这里读 `Resources/` 这份；**别把两份的内容差异**当成信号 ——
    /// 那件事**已经由 ⑥ 专门对账**（`A1421`：判据 = 两份的 `cards` 数组逐字节相同）。
    /// </summary>
    const string PlanFileAbs = @"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_icon_plan.json";
    /// <summary>
    /// ⑥ 对账的另一半 = `数据/游戏数据/` 那份（**人读的**：上面 `②` 与 `工具/gen_icon_doc.py` 读它）。
    /// ⚠️ 它与 `PlanFileAbs` **必须是两个不同的文件** —— ⑥b 钉着这一条（抄成同一条 ⇒ 比的是自己跟自己、恒绿）。
    /// </summary>
    const string DataPlanFileAbs = @"d:/4/Unity/数据/游戏数据/card_icon_plan.json";
    /// <summary>⑤ 对账用的素材目录（工程相对路径；`CardArt.Trait` / `Badges.EnsureTable` 取的是同一处）</summary>
    const string TraitsDirAsset = "Assets/CardPresentation/Resources/Art/traits";

    static PlanDoc LoadPlanDoc()
    {
        if (!File.Exists(PlanFileAbs)) return null;
        // ⚠️ 这是**独立**的一份解析（不复用 `CardIcons` 那份私有计划表）——
        //    检测器与被测实现走两个口，免得实现那边的加载 bug 把数据缺口一起藏掉。
        return JsonUtility.FromJson<PlanDoc>(File.ReadAllText(PlanFileAbs));
    }

    /// <summary>
    /// ⑧a 的谓词本体：计划表里**不是 `Melee` 的 `[Attack]` 条目**（`"卡id→sprite"` 列表）。
    /// 纯函数（只读传入的 doc）⇒ ⑧b 的灭自证拿**改坏的副本**跑同一个函数。
    /// 判据 = `A1771`：`[Attack]` 的答案**只**来自官方 `Card_Description` EN（123/123 处 = melee），
    /// 所以计划表里每一条 `[Attack]` 都必须是 `Melee`。出处 →
    /// `资料/普查产出_1018/AUD_A1771_A1945_可行性.md` §四·4·1 步 3。
    /// </summary>
    static List<string> AtkNotMelee(PlanDoc doc)
    {
        var badItems = new List<string>();
        if (doc == null || doc.cards == null) return badItems;
        foreach (var c in doc.cards)
        {
            if (c == null || c.fields == null) continue;
            foreach (var f in c.fields)
            {
                if (f == null || f.items == null) continue;
                foreach (var it in f.items)
                    if (it != null && it.token == "[Attack]" && it.sprite != "Melee")
                        badItems.Add(c.id + "→" + it.sprite);
            }
        }
        return badItems;
    }

    /// <summary>
    /// ⑧d 的谓词本体：一段源码里有没有引用**图标计划链**（`CardIcons` / `card_icon_plan`）。
    /// 纯函数（`工具/ruleprobe/` 那一侧用它证明 `PinnedPlayMd5` **结构上不可能**被 `A1771` 影响）。
    /// </summary>
    static bool HasPlanRef(string src)
    {
        if (string.IsNullOrEmpty(src)) return false;
        return src.IndexOf("CardIcons", System.StringComparison.Ordinal) >= 0
            || src.IndexOf("card_icon_plan", System.StringComparison.Ordinal) >= 0;
    }

    /// <summary>
    /// ④a 的谓词本体：返回 `"卡id|sprite"` 列表 —— **英文 `desc` 画了、中文 `descZh` 没画**的徽记。
    /// 纯函数（只读传入的 doc），⇒ ④b 能拿同一份函数去跑「改回去」的合成表。
    /// </summary>
    static List<string> ZhGaps(PlanDoc doc)
    {
        var gaps = new List<string>();
        if (doc == null || doc.cards == null) return gaps;
        ZhGapsDarkPactSkipped = 0;
        foreach (var c in doc.cards)
        {
            if (c == null || c.fields == null) continue;
            // 🆕 2026-10-19（B4 · `N70`/`N71` 收口）：**「黑暗契约」那一族不参与这张计划表的对账** ——
            //   判据只此一处 = `RuleEngine.CreatePool.MentionsDarkPact`（英文 = `MentionsDarkPactEn`、
            //   中文 = `ContainsAnyDarkPactZh`，E10 本轮已把两份副本收口）。
            //   为什么跳过：那 37 张的徽记**不走图标计划表** —— 中文值里是官方的 `[[…]]`（或 `❄` / `[黑暗契约]`
            //   这类已映射的 token），运行时由 `CardIcons.RewriteMarks` 画；而 `gen_icon_plan.py` 对
            //   带官方记号的字段**整字段跳过** ⇒ 拿「计划表 en ⊆ 计划表 zh」去比它们 = **比两个空集/残缺集**
            //   （旧口径会报假缺口；`A2054` 的接口需求 → `资料/普查产出_1019/E10_报告.md` §三·5）。
            //   ⛔ 别在这儿再写一份 `markOf*` / 「黑暗契约」字面量（E10 刚收口的两份副本就是教训）。
            if (MentionsDarkPactCard(c.id)) { ZhGapsDarkPactSkipped++; continue; }
            var en = new HashSet<string>();
            var zh = new HashSet<string>();
            bool sawZhField = false;   // 🆕 2026-10-15：`descZh` 那半边在计划表里**有没有字段**
            foreach (var f in c.fields)
            {
                if (f == null || f.items == null) continue;
                HashSet<string> set = f.name == "desc" ? en : (f.name == "descZh" ? zh : null);
                if (set == null) continue;
                if (f.name == "descZh") sawZhField = true;
                foreach (var it in f.items)
                    if (it != null && !string.IsNullOrEmpty(it.sprite)) set.Add(it.sprite);
            }
            // 🔴 **2026-10-15（第三十二会话）：`descZh` 那半边【整字段缺席】= 「载体不适用」⇒ 本条跳过。**
            //   为什么：2026-10-15 起 `工具/gen_icon_plan.py` 对**带官方记号**（`[[…]]`/`((…))`/`$$…$$`）的字段
            //   **整字段跳过** —— 那批文本运行时走 `CardIcons.Rewrite` 的 `HasOfficialMarks` 分流、
            //   **根本不查这张计划表**（判据 = `Core/CardIcons.cs` 的 `RewriteMarks` / `_rxFaithCostZh`），图标由 `RewriteMarks` 自己画。
            //   旧判据把「计划表里没有」当成「中文没画」⇒ 当轮实测会报 **769 条假缺口**
            //   （改动前同池 752 条 —— 两条都是假阳；⛔ 两个都是**当轮读数**、别当基线）。
            //   ⚠️ **与 `gen_icon_plan.py` 那 4 处自检面同一条口径**（「不是改期望值，是载体不适用」），
            //   出处 = `资料/普查产出_第三十二会话/R5_gen_icon_plan.md`。
            //   ⚠️ `descZh` 本来就空的那种卡也走这一支 —— 那也是对的：`FaceTextFull` 那时回落印 `desc`，
            //   英文那几枚图标照样画得出来（不构成缺口）。
            if (!sawZhField) continue;
            var miss = new List<string>();
            foreach (var s in en) if (!zh.Contains(s)) miss.Add(s);
            miss.Sort(System.StringComparer.Ordinal);
            foreach (var s in miss) gaps.Add(c.id + "|" + s);
        }
        gaps.Sort(System.StringComparer.Ordinal);
        return gaps;
    }

    /// <summary>上一次 `ZhGaps` 跳过了几张「黑暗契约」卡（自检/日志用；0 = 一张都没认出来）。</summary>
    static int ZhGapsDarkPactSkipped;

    static Dictionary<string, RuleEngine.CardDef> _poolById;

    /// <summary>卡池里按 `id` 取一张卡（只建一次表；`MentionsDarkPactCard` 与 `⑫` 共用）。
    /// ⚠️ 取不到 = `null`，**别在调用方写"猜一个"**（铁律 2）。</summary>
    static RuleEngine.CardDef PoolCard(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_poolById == null)
        {
            _poolById = new Dictionary<string, RuleEngine.CardDef>();
            foreach (var c in RuleEngine.CardDatabase.Load())
                if (c != null && !string.IsNullOrEmpty(c.Id)) _poolById[c.Id] = c;
        }
        RuleEngine.CardDef def;
        return _poolById.TryGetValue(id, out def) ? def : null;
    }

    /// <summary>`c.id` 那张卡是不是「提到 Dark Pact / 黑暗契约」（判据只此一处 = `CreatePool.MentionsDarkPact`）。</summary>
    static bool MentionsDarkPactCard(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        var def = PoolCard(id);
        return def != null && RuleEngine.CreatePool.MentionsDarkPact(def);
    }

    /// <summary>⑫b 的灭自证用的**绝不可能出现在任何卡面里的串**（不是 token、不是任何一张卡的正文子串）。</summary>
    const string ProbeAbsentToken = "◤⑫b-probe-never-in-any-card◢";

    /// <summary>⑫c 的**坏态夹具**：`ASH83` 在 2026-10-23 修**之前**的 `descZh` 原值（`路标石。` 那一句还没有）。
    /// 出处 = `资料/普查产出_1023c/卡牌线_三条收尾_执行报告.md` §一·2（`r12` 现读）＋ `cardface_fixes.json` 的
    /// `descZh | Autarch` 当轮 diff。⚠️ 它是**夹具**（证明谓词看得见坏态），⛔ 不是读数、别跟着数据更新。</summary>
    const string Ash83DescZhBeforeFix = "①：给予你的所有部队 +1 [攻击]、+1 [远程]和 +1 生命";

    /// <summary>⑫b 的谓词本体：计划表里**今天的卡池原文中已经找不到**的 token（`"卡id|字段|token"`）。
    /// 纯函数（只读传入的 `doc` 与卡池）⇒ ⑫b 的灭自证拿「只塞一条假 token」的合成表跑**同一个函数**。
    /// ⚠️ 「计划表里有这张卡、卡池里没有」是**另一类**（走 `missingCards`）—— 别混进 token 那一列
    /// （混进去会让合成表的读数变成 2 条、把「谓词能不能看见坏态」这件事搅浑）。</summary>
    static List<string> PlanTokensMissingInPool(PlanDoc doc, out List<string> missingCards)
    {
        var pairs = new List<string>();
        missingCards = new List<string>();
        if (doc == null || doc.cards == null) return pairs;
        foreach (var pc in doc.cards)
        {
            if (pc == null) continue;
            var cd = PoolCard(pc.id);
            if (cd == null) { missingCards.Add(pc.id); continue; }
            if (pc.fields == null) continue;
            foreach (var f in pc.fields)
            {
                if (f == null || f.items == null) continue;
                string text = f.name == "desc" ? cd.Desc
                            : (f.name == CardPresentation.CardIcons.DescZhField ? cd.DescZh : null);
                if (text == null) continue;
                foreach (var it in f.items)
                {
                    if (it == null || string.IsNullOrEmpty(it.token)) continue;
                    if (text.IndexOf(it.token, System.StringComparison.Ordinal) < 0)
                        pairs.Add(pc.id + "|" + f.name + "|" + it.token);
                }
            }
        }
        pairs.Sort(System.StringComparer.Ordinal);
        missingCards.Sort(System.StringComparer.Ordinal);
        return pairs;
    }

    /// <summary>⑫b 的**前提检查**用：计划表里一共多少条 token（`0` = 计划表没读到 / 结构变了）。
    /// ⚠️ 只用来判「这一格今天在测东西」，⛔ 别拿它当基线读数（`A1451`）。</summary>
    static int CountPlanTokens(PlanDoc doc)
    {
        if (doc == null || doc.cards == null) return 0;
        int n = 0;
        foreach (var pc in doc.cards)
        {
            if (pc == null || pc.fields == null) continue;
            foreach (var f in pc.fields)
                if (f != null && f.items != null) n += f.items.Length;
        }
        return n;
    }


    /// <summary>
    /// 🔴 **已知的「英文画了、中文没画」缺口**（`A1409` 的 ④a 用它把「已查出的」和「新冒出来的」分开）。
    ///
    /// **这不是「允许它不用修」** —— 铁律 11 说与原版不符 / 复刻有缺漏**全部都要做**、只有先后之分；
    /// 这张表只是不让「今天还没做的那几条」把 ④a 一直刷红。⇒ **它只允许变短**：
    /// 修掉一条就把那一行删掉（④a 照旧绿），**⛔ 别往里加**（往里加 = 悄悄把新缺口当基线，那正是本条要治的病）。
    ///
    /// 逐条出处（2026-10-10 现核：`cards_engine.json` 与计划表左右对读，`desc` 侧的 sprite 列在这里）：
    ///
    /// 🔴 **「⛔ 别往里加」现在有机械保证了**（`A1423`）：下面那个 `KnownZhGapsCount` 常量 + `Verify()` 的
    ///    `④c`。**改这张表就必须同步改常量**，否则 `IconSetup.Verify` 红 ——
    ///    ⚠️ **每修好一条也会红一次，那是故意的**（逼着「删表项 + 常量 −1」两处一起改，
    ///    表才不会变成谎话）。另配 `④d` 钉住「表里的每一条今天还得是真缺口」
    ///    （防「修好了却忘了删那一行 ⇒ 条数没变 ⇒ ④c 照样绿」）。
    /// </summary>
    static readonly string[] KnownZhGaps =
    {
        // ── ⚠️ **① / ② 两类的 14 条，2026-10-10 已修好、已从本表删除**（`A1442`；逐条出处见
        //    `资料/普查产出_第十四会话/W_中文徽记缺口批.md` §一 / §四·2）。**删之前逐条现核过**
        //    「那条缺口在改造后的计划表里已经消失」（重跑 `ZhGaps` ⇒ 这 14 条在全池里一条都不再出现）：
        //    · ① 中文写法不在词表（7 条）：`AM13|blast` `AM34|blast` `AM40|armour` `AM43|armour`
        //      `AM74|armour` `SAU43|armour` `SAU_Lychguard|armour`
        //      —— 根因是中文写「爆破 / 装甲」而词表那两格是「爆裂 / 护甲」；修法是词表加 2 行中文别名。
        //    · ② 中文那一段整段丢（7 条）：`ASH_Death_Spinner_Warp_Spider|SpiritStone_1` ·
        //      `ASH_Death_Spinner_Warp_Spider|stealth` · `ASH42|SpiritStone_2` ·
        //      `ASH_Bright_Lance_Vyper|sniper` · `DA16|shield` · `GOF8|tide` · `UM90|stealth`
        //      —— 修法是补数据侧 `数据/游戏数据/cardface_fixes.json` 的 `descZh` 列。
        //    ⛔ **别再往本表里加任何一条**（往里加 = 把新缺口当基线）—— 判据 = `④c`。

        // ── ⚠️ **③ 类那 1 条（`BL19|markOfSlaanesh`）也已在 2026-10-10 修好、已从本表删除**（`A1473`）——
        //    修的是**中文侧那一半**：`数据/游戏数据/cardface_fixes.json` 里 `BL19 Noise Marine` 的
        //    `descZh` 由 `…获得一个 [黑暗契约]纵欲黑暗契约` 改成 `…获得一个 ❄纵欲黑暗契约`
        //    （英文侧**早就是** `❄`）。判据 = **英文卡面**那一半 —— 原版客户端**没有中文表**
        //    （全仓已定案，`资料/全量反编译复核_靠推断的清单.md` §2.2）⇒ 中文侧唯一可核的是英文卡面口径。
        //    🔴 **2026-10-14 更正（铁律 5）**：「原版客户端**没有**中文表」**不成立** ——
        //      原版**有**官方中文表，由**远端 Addressables 包 `localization_assets_all`** 下发，
        //      **2026-10-14 已解开**并落至 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`
        //      （判据全文 → `CLAUDE.md` §一·7 的「2026-10-14 更正」段）；**错因** = 把
        //      「不在离线包体里」读成了「根本没有」。⚠️ 本句引的那格（`全量反编译复核_靠推断的清单.md` §2.2）
        //      **已就地订正**，别再照它当旧判据。
        //      ⇒ **本笔的处置【不变】**（当时只有英文卡面可核，那一步已经做完、数据侧也已改完）；
        //        但**「中文侧唯一可核的是英文卡面口径」这句已过期** —— 中文侧现在另有两条可核的路：
        //        ① 官方 zh-CN 的 `Card_Description/*`（⚠️ 它是 I2 占位符模板、要等 `Core/CardIcons` 的
        //           `[[…]]`/`((…))` 记号层，见 `A1763`）；② `A1103` 那份**原版运行期真渲中文**的 dump。
        //        ⚠️ **要不要拿它们回核本笔 = 另记的账**（本笔只订正措辞）。
        //    出处：`资料/普查产出_第十四会话/W_A1420A1444.md` §一（逐条判据链 ①~⑥）。
        //    ⚠️ **本条原来那句「两枚不是同一张图」同时订正**（同出处 §一·2 的逐像素实测）：
        //      `markOfChaos` / `markOfKhorne` / `markOfNurgle` **逐字节相同**；`markOfSlaanesh` 与它们
        //      只差 **15 个 `alpha=0` 像素的 RGB**（`markOfTzeentch` 差 6 个）⇒ **渲染上完全一样**。
        //      ⇒ 本笔修的是**名字级一致性**（④a 比的是 sprite **名**集合），**画面不因此改变** ——
        //      ⛔ 别把它说成「玩家会看出画错了一枚徽记」。
        //    ⇒ `ZhGaps(今天的计划表)` 由 **1 条 → 0 条**。⛔ **别再往本表里加任何一条** —— 判据 = `④c`。

        // 🔵 **—— 本表现在是【空的】（`KnownZhGaps.Length == 0`）——**
        //    这是本表**第一次**到 0 条。`A1423` 那条机械保证（条数 === 常量）**两次兑现**都在这里：
        //    第一次 = `A1442`（删 14 条，`15 → 1`）· 这一次 = `A1473`（删最后 1 条，**`1 → 0`**）。
        //    ⇒ `KnownZhGapsCount` 必须**同时**跟到 `0`（只改一处 ⇒ ④c 红，那是故意的）。
        //    🔴 **这次兑现顺带暴露了那条守卫自己的一个边界 bug**：`WithoutLast` 原来写
        //    `new string[t.Length - 1]` —— 表非空时没事，**表一空就是 `new string[-1]` ⇒ 抛异常
        //    ⇒ ④c-b 崩、整个 `Verify()` 挂掉**（**不是红、是崩**）⇒ 本笔一并给它加了守卫
        //    （判据与取舍写在下面 `WithoutLast` 的 `///` 里）。
        //    ⚠️ 表为空**不是**「这条守卫可以退休」：它照样钉着「⛔ 别往里加」——
        //    今天往回加一条 ⇒ 表 1 条 ≠ 常量 0 ⇒ ④c 红。
    };

    /// <summary>
    /// 🔴 `KnownZhGaps` 的**显式条数**（`A1423`）—— 判据原文 = 「该表条数必须 == 一个显式常量」。
    ///
    /// **为什么不能省**：`KnownZhGaps` 是基线表 ⇒ 「**只许变短**」若只有注释在挡，谁把一条新缺口
    /// 写进去、④a 照样绿。配上这个常量，**改表不改常量（或反过来）都会红**。
    ///
    /// ⚠️ **「每修好一条也会红」是【故意留的副作用】**（调度台 2026-10-10 裁定「要加」）：
    ///   它逼着「**删表项 + 常量 −1**」两处一起改；`Verify()` 的 `④c-b` 再拿**同一张真表**造的三个
    ///   变体（+1 条 / −1 条 / **声明值写错**）去跑同一个守卫 ⇒ 「守卫恒绿」与「认得出变体」
    ///   结构上不可能同时成立（第三个变体挡的是「把常量写成 `KnownZhGaps.Length`」那种自引用写法）。
    ///
    /// ⛔ **别把它改成软告警**（`Log` / `LogWarning`）—— 那就等于退回「只有注释在挡」。
    /// 🔴 **口径（`A1451`：不写死读数）**：表 `KnownZhGaps` 必须**恒为空**、且本常量 = **0**，
    ///    两处由 ④c 一起钉住（改任一处都会红）；`A1473` 删掉最后那 1 条之后就是这个状态。
    ///    ⛔ 别把「现在 0 条」抄成一条会过期的读数 —— 判据是「表长度 === 本常量」，不是某个历史数字。
    ///    🔵 **变更史（两次兑现，都走「删表项 + 常量一起改」）**：`A1442` 删 14 条 ⇒ `15 → 1`；
    ///    `A1473` 删最后 1 条（`BL19|markOfSlaanesh`）⇒ **`1 → 0`**。
    ///    ⚠️ 表为空**不是**「守卫可以退休」：它照样钉着「⛔ 别往里加」——
    ///    今天往回加一条 ⇒ 表 1 条 ≠ 常量 0 ⇒ ④c 红（那一格正是本常量存在的理由）。
    /// </summary>
    const int KnownZhGapsCount = 0;

    /// <summary>
    /// `④c` 的守卫本体 —— **纯函数** ⇒ `④c-b` 能拿同一张真表造变体去跑它。
    /// `declared` 必须是**独立于表的显式常量**：⛔ 别把调用点写成 `KnownZhGaps.Length`
    /// （那叫自引用、等于没判 —— `④c-b` 的第三个变体就是专门挡它的）。
    /// </summary>
    static bool GapTableOk(string[] table, int declared)
    {
        return table != null && table.Length == declared;
    }

    /// <summary>`④c-b` 变体一：真表 **+1 条**（模拟「把一条新缺口写进基线表」）</summary>
    static string[] WithExtra(string[] t)
    {
        var r = new string[t.Length + 1];
        System.Array.Copy(t, r, t.Length);
        r[t.Length] = "ZZ_MUTANT|blast";     // 卡池里不存在这个 id ⇒ 只是个数变多，不冒充真缺口
        return r;
    }

    /// <summary>
    /// `④c-b` 变体二：真表 **−1 条**（模拟「修好一条、删了表项却忘了改常量」的反面）。
    ///
    /// 🔴 **表为空时「−1 条」这一变体【不存在】⇒ 返回 `null`**（`A1473`：本表第一次到 0 条时暴露的
    ///    边界 bug —— 原来写的是 `new string[t.Length - 1]`，`t.Length == 0` 时就是 `new string[-1]`
    ///    ⇒ **抛 `OverflowException`** ⇒ `④c-b` 那一格崩、**整个 `Verify()` 挂掉**（不是红，是崩））。
    ///    ⇒ 守卫就加在这一句：**先判 `t.Length == 0`**，别让 `t.Length - 1` 变成负数。
    /// ⚠️ `null` **仍然被 `GapTableOk` 拒**（它第一句就是 `table != null && …`）⇒
    ///    `④c-b` 那句「**三个变体全被拒**」的口径**不变**，**没有**把任何一条降成软告警。
    /// ⚠️ **如实说**：表为空时这一格是**退化**的 —— 它拒的是 `null` 本身，不是「长度差」。
    ///    空表下真正在挡「守卫恒绿」的是**变体一**（真表 +1 条 ⇒ 长度差）与**变体三**
    ///    （声明值写错 ⇒ 常量自引用那种写法必红），那两个在空表下**仍然有效**。
    /// </summary>
    static string[] WithoutLast(string[] t)
    {
        if (t == null || t.Length == 0) return null;  // 「−1 条」无从构造 ⇒ 交给 `GapTableOk` 的 null 判据去拒
        var r = new string[t.Length - 1];
        System.Array.Copy(t, r, t.Length - 1);
        return r;
    }

    /// <summary>
    /// ④b 的**控制集（推导式）**：`"卡id|sprite"` —— 对每一张卡取「`desc` 的 sprite 集合 ∩ `descZh` 的 sprite 集合」
    /// 里的每一枚。那些正是「**今天英文与中文两边都画着**」的徽记；把它们从 `descZh` 侧抽掉，④a 必须逐条认出来。
    ///
    /// <para>🔴 **2026-10-15（第三十二会话）从【手写 19 条】改成【推导】**。原表出处是
    ///   `A1360`（`资料/普查产出_第十四会话/W_A1360中文段首词.md` §一，5 条段首关键词）＋
    ///   `A1400`（同目录 `W_图标族8词.md` §四 1，`Invulnerable` 那 14 张）—— 它当时自陈「这 19 条今天都在真计划表的 `descZh` 里」。
    ///   ⚠️ 2026-10-15 起 `gen_icon_plan.py` 对**带官方记号**（`[[…]]`/`((…))`/`$$…$$`）的字段**整字段跳过**
    ///   （那批文本运行时走 `CardIcons.RewriteMarks`、**不查本表**）⇒ `descZh` 换代之后一大批卡落进那一支
    ///   ⇒ 手写那 19 条当时（2026-10-15）实测**只剩 5 条**在表里 ⇒ 抽掉那 5 条之外**等于没抽** ⇒ 这条灭自证**退化成空转**。
    ///   （⚠️ 改动前同池就已经是 5/19，**不是**那次改动引入的。）</para>
    ///
    /// <para>✅ **判据形式没变**：仍是「抽掉 ⇒ ④a 必须每一条都认出来」＋「实际抽掉的条数 == 控制集长度」（前提检查）。
    ///   改成推导之后，控制集永远与「**今天真画着的那一批**」同步 —— 数据换代不会再让它空转。
    ///   出处 = `资料/普查产出_第三十二会话/D3_IconSetup五条红.md` §一·5。</para>
    /// </summary>
    static List<string> RevertControlFor(PlanDoc doc)
    {
        var list = new List<string>();
        if (doc == null || doc.cards == null) return list;
        foreach (var c in doc.cards)
        {
            if (c == null || c.fields == null) continue;
            // 🔴 **2026-10-23（卡牌线 `D1` 现核）：控制集必须和谓词【同一个作用域】** ——
            //   `ZhGaps` 对「黑暗契约」那一族**整卡 `continue`**（见它上面那段：那 41 张的徽记走
            //   `RewriteMarks`、不参与本表对账）。而这里原来没跳 ⇒ 那一族的 (卡,sprite) 对**照样进控制集**，
            //   抽掉它们**永远不可能**让 `ZhGaps` 多报一条 ⇒ **14 条必然漏**（实测 `705/719`，
            //   漏掉的正好 = 控制集里属于黑暗契约卡的那 14 对）。
            //   ⚠️ 这不是「放宽」：判据仍是「抽掉的 ⇒ ④a 每一条都得认出来」；只是**控制集不再包含
            //   谓词本来就不判的卡**（否则那 14 条是**结构上给不出答案**的空转项）。
            //   判据只此一处 = `CreatePool.MentionsDarkPact`（`MentionsDarkPactCard` 转发）。
            if (MentionsDarkPactCard(c.id)) continue;
            var en = new HashSet<string>();
            var zh = new HashSet<string>();
            foreach (var f in c.fields)
            {
                if (f == null || f.items == null) continue;
                HashSet<string> set = f.name == "desc" ? en : (f.name == "descZh" ? zh : null);
                if (set == null) continue;
                foreach (var it in f.items)
                    if (it != null && !string.IsNullOrEmpty(it.sprite)) set.Add(it.sprite);
            }
            foreach (var sp in en) if (zh.Contains(sp)) list.Add(c.id + "|" + sp);
        }
        list.Sort(System.StringComparer.Ordinal);
        return list;
    }

    /// <summary>归一化：只留字母数字、全小写（和 `Badges.Key` 同口径）。`Hunt Mark` → `huntmark`</summary>
    static string NormKey(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char ch in s) if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }

    static string _poolNorm;

    /// <summary>⑤c 用：卡池 1126 张的 `desc` + `descZh` 归一化后拼成的一条大串（只算一次）</summary>
    static string NormPool()
    {
        if (_poolNorm != null) return _poolNorm;
        var sb = new System.Text.StringBuilder();
        foreach (var c in RuleEngine.CardDatabase.Load())
            if (c != null) sb.Append(NormKey(c.Desc)).Append(' ').Append(NormKey(c.DescZh)).Append(' ');
        _poolNorm = sb.ToString();
        return _poolNorm;
    }

    // ─────────────── ⑥（`A1421`）的两份计划表对账：取体 / 比较器 / 变体 / 读数 ───────────────

    /// <summary>
    /// ⑥ 的**取体**：从 **`"cards"` 这一键**起、截到文件尾。
    /// ⛔ **不是整个文件** —— 两份的 `note` 头**本来就不同**（`Resources` 那份自己写着「运行时那一份」）。
    /// 🔴 取不到这个键 ⇒ 返回 `null`；**`null` 不许冒充「相同」**（`SameCardsBody` 里钉着）。
    /// 🔴 **2026-10-11（`A1425`）：入参与出参都是 `byte[]`**（改前是 `string`，走 `File.ReadAllText`
    /// —— 那条路**会把 BOM 吃掉** ⇒ 判据原文那句「逐字节相同」**落不了地**）。
    /// </summary>
    static byte[] CardsBody(byte[] raw)
    {
        if (raw == null || raw.Length == 0) return null;
        int i = IndexOfAscii(raw, "\"cards\"");
        return i < 0 ? null : Slice(raw, i, raw.Length - i);
    }

    /// <summary>`byte[]` 里找一段**纯 ASCII** 子串（`"cards"` 这个键名就是）—— 朴素查找，够用。</summary>
    static int IndexOfAscii(byte[] hay, string needle)
    {
        if (hay == null || needle == null || needle.Length == 0) return -1;
        for (int i = 0; i + needle.Length <= hay.Length; i++)
        {
            int j = 0;
            while (j < needle.Length && hay[i + j] == (byte)needle[j]) j++;
            if (j == needle.Length) return i;
        }
        return -1;
    }

    /// <summary>`byte[]` 切片（`Array.Copy`；⛔ 别拿 `Span` —— 本仓的 csc 版本不必冒这个险）。</summary>
    static byte[] Slice(byte[] src, int start, int len)
    {
        var outb = new byte[len];
        System.Array.Copy(src, start, outb, 0, len);
        return outb;
    }

    /// <summary>
    /// ⑥ 的**比较器本体**（纯函数 ⇒ ⑥b 拿它跑变体）。
    /// 🔴 `null` **不算「相同」** —— 这就是 `null == null` 恒真那个洞（两边都取不到键时，
    /// 若拿 `==` 直接比，断言会**恒绿**）。
    /// 🔴 **2026-10-11（`A1425`）：逐字节比**（`SequenceEqual` 的等价写法）—— 判据原文就是这个。
    /// </summary>
    static bool SameCardsBody(byte[] a, byte[] b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    /// <summary>⑥b 变体一：把**正中间那一位**翻掉（**长度不变** ⇒ 挡「只比长度」那种退化比较器）</summary>
    static byte[] MidFlip(byte[] s)
    {
        if (s == null || s.Length == 0) return s;
        var c = (byte[])s.Clone();
        int i = c.Length / 2;
        c[i] = (byte)(c[i] ^ 1);
        return c;
    }

    /// <summary>⑥ 报错时用：**第一处不同**的下标（两边完全一样 ⇒ -1）—— 给人一个可复核的坐标，不是只报「不等」</summary>
    static int FirstDiff(byte[] a, byte[] b)
    {
        if (a == null || b == null) return -1;
        int n = System.Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++) if (a[i] != b[i]) return i;
        return a.Length == b.Length ? -1 : n;
    }

    /// <summary>
    /// ⑥ 的读数用：FNV-1a 64 位指纹（`cards` 体 90 万**字节**用；**给人一个能在外面对着算的指纹**）。
    /// 不引 `System.Security.Cryptography` 的 sha256 —— 这里只当「读数」，不当判据。
    /// ⚠️ `A1425` 把喂进来的单位从 `char` 换成 `byte` ⇒ **指纹值与改前不可比**（别跨版本对）。
    /// </summary>
    static string BodyHash(byte[] s)
    {
        if (s == null) return "-";
        ulong h = 14695981039346656037UL;
        foreach (byte c in s) { h ^= c; h *= 1099511628211UL; }
        return h.ToString("x16");
    }

    /// <summary>⑥ 报长度用：`byte[]` ⇒ 字符串（`null` ⇒ `"?"`，⛔ 别抛）。</summary>
    static string Blen(byte[] s) { return s == null ? "?" : s.Length.ToString(); }

    // JsonUtility 用的扁平结构（④ 读计划表；只声明用得上的字段，`why` 不读）
    [System.Serializable] class PlanDoc { public int version; public PlanCard[] cards; }
    [System.Serializable] class PlanCard { public string id; public PlanField[] fields; }
    [System.Serializable] class PlanField { public string name; public PlanItem[] items; }
    [System.Serializable] class PlanItem { public string token; public string sprite; }

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
