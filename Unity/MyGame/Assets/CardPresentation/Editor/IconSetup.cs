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
    /// 六组：① 资产在 ② 计划表里每个 sprite 名都能在 asset 里查到 ③ 真渲一张图
    /// ④ **徽记链的通用护栏**（英文卡面画的每一枚徽记，中文卡面也要画；含灭自证，`A1409`）
    ///    ＋ **`KnownZhGaps`「只许变短」的机械保证**（表条数 === 显式常量，`A1423`）
    /// ⑤ **素材盘 ↔ 计划表 的对账**（盘上有图却没被用上的要出声，`A1370`）
    /// ⑥ **两份 `card_icon_plan.json` 的 `cards` 数组逐字节对账**（含灭自证，`A1421`）。
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
        //   而 `[Armor]` / `[Armour]` 这个写法**当前卡池 1126 张里一处都没有**
        //   （2026-10-20 实扫 `cards_engine.json` 的 `desc` + `descZh`：**0 处**）。
        //   这条断言要卡面上**同时**出现 Melee 与 Ranged 两枚图标 ⇒ 它测的是一个
        //   **已经不存在的 token** ⇒ **夹具过时**，不是实现缺陷。
        //   🔴 **2026-10-21 更正（`A1361` · 铁律 5）**：这一句原来还引着「`工具/gen_icon_plan.py:187`
        //     那条 `("DA44","[Armor]")` 因此**匹配不上任何 token**」—— **那个行号与键名都已失效**：
        //     逐卡表现读 `gen_icon_plan.py:194` = **`("DA44","[Ranged]")`**（该行自己的注释写着
        //     「键 2026-10-18 由 `[Armor]` 订正过来」）；全脚本**再无** `("DA44","[Armor]")` 这条
        //     （`[Armor]` 只剩文件头 `:10` 那段描述旧 OCR 数据的老注释里还提得到）。
        //     ⚠️ **连带「匹配不上任何 token」这个论断也翻了**：`DA44` 的 `desc` 现读 =
        //     `Give +3 [Attack], +3 [Ranged] or +3 Health to a friendly troop`（第二枚是**带方括号的**
        //     `[Ranged]`、不是裸词），计划表那份也逐字对得上 ⇒ 那条 `("DA44","[Ranged]")` 今天**匹配得到**。
        //   ⚠️ 所以**不能只改期望值**（那只会把一条空转的断言刷绿）—— **夹具要跟着数据走**
        //      （照 ②i 的写法：拿 `CardDatabase.Load()` 里的真文本，不再另抄一份）。
        //
        //   顺带钉住**一条真缺口**（见下面第二条 `Check` 的长注释）。
        RuleEngine.CardDef da = null;
        foreach (var c in RuleEngine.CardDatabase.Load())
            if (c != null && c.Id == "DA44") { da = c; break; }
        Check(da != null && !string.IsNullOrEmpty(da.Desc),
              "②c 卡池里取得到 DA44 的 `desc`（夹具不再自己抄一份）", ref bad);
        string da44 = da == null ? "" : CardPresentation.CardIcons.Rewrite("DA44", "desc", da.Desc);
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
        //     `<sprite name="{sprite}">`（`Core/CardIcons.cs:238`）⇒ **这条今天应当绿**。
        //   ⚠️ **本批禁 Unity、没有实跑读数** ⇒ 绿没绿以收口时 `-executeMethod IconSetup.Verify` 为准；
        //     若它仍红，那是**另一条判据**在起作用，⛔ 别照本段旧话去改 `desc`。
        Check(CountOf(da44, "<sprite name=\"Ranged\">") == 1,
              "★ ②c【A980-c】DA44 的 `desc` 应换出**两枚**图标（拳 + 枪）—— 卡图那两处都只印图、不印词；"
              + "改坏法：删掉计划表里 `DA44` 那两条逐卡条目、或把 token 改回裸词 `Ranged` ⇒ 少一枚 ⇒ 红。"
              + "实得：" + da44, ref bad);
        string ash = CardPresentation.CardIcons.Rewrite(
            "ASH_Autarch", "desc", "1 [Spirit Stone]: Give +1 melee, +1 ranged and +1 Health to all your troops");
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
        string ly = CardPresentation.CardIcons.Rewrite("SAU_Lychguard", "descZh", "残骸。装甲 2。");
        Check(CountOf(ly, "<sprite name=\"remnant\">") == 1
              && CountOf(ly, "<link=remnant>") == 1
              && ly.IndexOf("<sprite name=\"remnant\">", System.StringComparison.Ordinal)
                 < ly.IndexOf("残骸。", System.StringComparison.Ordinal)
              && CardPresentation.CardIcons.StripTags(ly).Contains("残骸。"),
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
        string sun = CardPresentation.CardIcons.Rewrite("SOR67", "desc", "☀：部署一个额外的战斗修女");
        Check(CountOf(sun, "<sprite name=\"faith\">") == 1
              && sun.IndexOf('☀') < 0
              && CardPresentation.CardIcons.StripTags(sun).Contains("：部署一个额外的战斗修女"),
              "②f2 符号 token（`☀`）被**吃掉**、不留在文字里：" + sun, ref bad);
        string circ = CardPresentation.CardIcons.Rewrite("ASH_Farseer", "descZh", "①：给一个友方部队");
        Check(circ.IndexOf("①", System.StringComparison.Ordinal) < 0 &&
              circ.Contains("<sprite name=\"SpiritStone_1\">"),
              "②f2 圈码 token（`①`）同样被吃掉：" + circ, ref bad);
        // ②f3 **数字烘在图里**：`1 Quest Point` 的 `questPoints1` 图上已经印着 1 ⇒ 整串吃掉。
        //     判据是「那个数字就在图名里」（`1` ∈ `questPoints1`），**不是**按 token 名猜
        //     —— 之前只认 `Spirit Stone`，`Quest Point` 会留下「1 Quest Point」纯文本 + 图标。
        // 🔴 2026-10-20（A980）：原来拿**整串全等**去比 —— 过时（现在外面还有一层 `<link>`，
        //     实得 `Gain <link=questpoints><sprite name="questPoints1"></link>`）。
        //     照 ②i 的写法改：**图标在 + 剥标签后 `Quest Point` 与那个 `1` 都没了 + `Gain` 还在**。
        string qp = CardPresentation.CardIcons.Rewrite("DA12", "desc", "Gain 1 Quest Point");
        string qpPlain = CardPresentation.CardIcons.StripTags(qp);
        Check(CountOf(qp, "<sprite name=\"questPoints1\">") == 1
              && qpPlain.IndexOf("Quest Point", System.StringComparison.Ordinal) < 0
              && qpPlain.IndexOf("1", System.StringComparison.Ordinal) < 0
              && qpPlain.Contains("Gain"),
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

        // ②i / ②j 🔴 **Oath 徽记 = 「徽记 + 词」**（2026-10-18，`资料/普查产出_1018/V-OATH_Oath徽记与记号.md` 候选 A）。
        //
        //   **钉什么**：这三张卡的 `Oath` 在卡面上印的是「**徽记 + 紧跟那个词**」
        //   （亲读成品卡图：`Ultramarines/3部队/Warpforge_20_Chaplain-Cassius.png` 的徽记在**句中**、
        //     `…/Warpforge_25_Ferren-Areios.png` 与 `…/Warpforge_12_Vico-Therbeus.png` 各 **2 枚**）。
        //   我们这边两处会把它弄坏：① `desc` 里写成**方括号** `[Oath]` ⇒ `Rewrite` 走「整串换掉」那支
        //   ⇒ **词被吃掉**（`UM84` 原来就是这样，`Talent` 同病）；② 徽记那一条**不在计划表里**
        //   ⇒ **整枚不画**（`UM89` / `UM_Vico_Therbeus` 的首句原来就是，因为 `Oath abilities`
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
        //      · 把 token 写成短的 `"Oath"`（`UM89` / `UM_Vico_Therbeus`）⇒ 被同卡更长的
        //        `"Oath 1:"` 那条按**长度降序 + 幂等守卫**挡掉 ⇒ 次数回到 1 ⇒ 红。
        {
            // `want` = 这张卡的卡面上那枚徽记应该出现几次（= 亲读读数）
            // 🆕 `A1409`：「**中文卡面徽记在不在**」这件事的**通用判据**已经在下面 **④a** 了
            //    （对全池每张卡都成立）⇒ 这张逐卡表**只剩**「钉得更死的那几条」（枚数 / 词序 / 幂等），
            //    ⛔ **别再往这里加卡**（加了也盖不住新卡 —— 那正是 `A1409` 要治的）。
            string[,] oathCards = {
                { "UM84",             "Oath abilities", "誓言能力", "1" },
                { "UM89",             "Oath abilities", "誓言能力", "2" },
                { "UM_Vico_Therbeus", "Oath abilities", "誓言能力", "2" },
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
                int nD = CountOf(d, "<sprite name=\"oath\">"), nZ = CountOf(z, "<sprite name=\"oath\">");
                int iD = d.IndexOf("<sprite name=\"oath\">", System.StringComparison.Ordinal);
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
                    if (c != null && c.Id == "UM84") { cd = c; break; }
                string d = cd == null ? "" : CardPresentation.CardIcons.Rewrite("UM84", "desc", cd.Desc);
                Check(d.IndexOf("<sprite name=\"talent\">", System.StringComparison.Ordinal) >= 0
                      && CardPresentation.CardIcons.StripTags(d).Contains("Talent:"),
                      "★ ②j `UM84` 的 `Talent:` 同样是「**纸卷 + 词**」（改回 `[Talent]` ⇒ 词没了 ⇒ 红）—— "
                      + d.Replace("\n", "\\n"), ref bad);
            }
        }

        // ═════════════════ ④ 徽记链的【通用】护栏（`A1409`）═════════════════
        //
        // 🔴 **为什么要单开这一条**（判据原文 → `项目任务.md` §29·b 的 `A1409`）：
        //   「中文卡面徽记在不在」原来只有上面 ②i 那张**逐卡表**（3 张：`UM84` / `UM89` /
        //   `UM_Vico_Therbeus`）⇒ `A1360` 补的 5 张（`UM74` `GOF8` `GOF21` `DA16` `EC35`）与
        //   `A1400` 补的 14 张（`Invulnerable`）**一张都不在表里** ⇒ **把那两笔改回去也不会红**。
        //   ⇒ 这里改成**对全池每张卡都成立的一条通用判据**（⛔ 别再一张一张往表里挤）。
        //
        // **判据（一句话）**：**英文卡面画出来的每一枚徽记，中文卡面也必须画。**
        //   · 「画出来」= 计划表里该卡 `desc` / `descZh` 子表里出现过那个 `sprite`
        //     （那就是 `CardPresentation.CardIcons.Rewrite` 会插进卡面文字里的那一枚）。
        //   · 方向**只有这一个**（`desc` 的 sprite 集合 ⊆ `descZh` 的）。反方向今天有 40 张不成立，
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
            if (nCards > 0)
            {
                var gaps = gapsAll;
                var fresh = new List<string>();
                foreach (var g in gaps) if (System.Array.IndexOf(KnownZhGaps, g) < 0) fresh.Add(g);
                Check(fresh.Count == 0,
                      "★ ④a **英文卡面画的每一枚徽记、中文卡面也画**（全池 " + nCards + " 张；" +
                      "方向 = `desc` 的 sprite 集合 ⊆ `descZh` 的）：**新缺口 " + fresh.Count + " 条**" +
                      (fresh.Count == 0 ? "" : " → " + string.Join(" / ", fresh.ToArray())) +
                      "；**已知缺口 " + gaps.Count + " 条**（`KnownZhGaps`，逐条带出处；那张表" +
                      "**不是「不用修」** —— 铁律 11 说全都要做、只有先后之分，它只把「已查出的」" +
                      "和「今天新冒出来的」分开）" +
                      "。改坏法：删掉任意一张卡 `descZh` 里那一枚（或让中文那个词扫不到）⇒ 新缺口 +1 ⇒ 红。",
                      ref bad);
                if (gaps.Count > 0)
                    Log("④a 已知缺口清单（**要人判**，别拿它当基线）：" + string.Join(" / ", gaps.ToArray()));

                // ④b 【灭自证】把 `A1360` 5 张 + `A1400` 14 张**改回去** ⇒ ④a 必须认出来
                var broke = LoadPlanDoc();
                int removed = 0;
                if (broke != null && broke.cards != null)
                    foreach (var c in broke.cards)
                        foreach (var f in c.fields)
                        {
                            if (f.name != "descZh" || f.items == null) continue;
                            var keep = new List<PlanItem>();
                            foreach (var it in f.items)
                            {
                                if (System.Array.IndexOf(RevertControl, c.id + "|" + it.sprite) >= 0)
                                { removed++; continue; }
                                keep.Add(it);
                            }
                            f.items = keep.ToArray();
                        }
                var ctrl = broke == null ? new List<string>() : ZhGaps(broke);
                var missed = new List<string>();
                foreach (var pair in RevertControl) if (ctrl.IndexOf(pair) < 0) missed.Add(pair);
                Check(removed == RevertControl.Length && missed.Count == 0,
                      "★ ④b【灭自证】把 `A1360` 那 5 张 + `A1400` 那 14 张**改回去**（抽掉 " + removed +
                      " 条 `descZh` 条目）⇒ ④a 必须认出来：" +
                      (RevertControl.Length - missed.Count) + "/" + RevertControl.Length + " 条被抓到" +
                      (missed.Count == 0 ? "" : "；**漏掉** " + string.Join(" / ", missed.ToArray())) +
                      "。这条是「结构上不可能同时满足」的口径：谓词恒绿 ⇒ 这里必红。", ref bad);
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
        //   `A1366` 的 `stun` 正是从这个缝漏的（`traits/stun.png` 在盘上、30 张卡的正文里都写着 `Stun`，
        //   而计划表里 `stun` **0 条** ⇒ 那 30 张卡的卡面一枚徽记都没画）。
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
        //   ⚠️ **下面这几个数字别照抄**（本仓的老坑：读数写死 = 「当时对、隔天烂」，见 `A1372`）——
        //   **生成器每重跑一次就会变**；真读数以这一条打出来的 `⑥ 读数` 那一行为准。
        //   本条落地时的现读（2026-10-10，**字符数 = C# `.Length` 那个单位**）：
        //   文件 **666037 / 665985** 字符（UTF-8 字节分别是 **902181 / 902117**）、
        //   **头部 133 / 81 字符**（那 52 个字符就是全部差异）、
        //   `cards` 体 **各 665904 字符且逐字节相同**（FNV-1a64 都是 `a4580070c903191b`）。
        //
        // 🧨 **灭自证（⑥b）**：这条判据有两个**天然恒绿**的写法 ——
        //   ① 两条路径常量抄成**同一个文件**（那比的就是「自己跟自己」）；
        //   ② 两边都取不到 `"cards"` 时拿 `==` 直接比 ⇒ **`null == null` 恒真**。
        //   ⇒ ⑥b 拿**同一份真内容**造变体，要求**同一个比较器**给出「同／不同」的**正确答案**
        //   （中间翻一位 · 截掉 100 **字符**（= `.Length` 那个单位）⇒ 必须**不同**；两边都取不到 ⇒ 必须**不同**，⛔ 不是相同）。
        //   「恒绿」与「认得出这些变体」**结构上不可能同时成立**。
        {
            string rawA = File.Exists(DataPlanFileAbs) ? File.ReadAllText(DataPlanFileAbs) : null;
            string rawB = File.Exists(PlanFileAbs) ? File.ReadAllText(PlanFileAbs) : null;
            string bodyA = CardsBody(rawA), bodyB = CardsBody(rawB);

            Check(rawA != null && bodyA != null,
                  "⑥ 读得到 `数据/游戏数据/` 那份计划表、且里面有 `\"cards\"` 键（" + DataPlanFileAbs + "）", ref bad);
            Check(rawB != null && bodyB != null,
                  "⑥ 读得到 `Resources/` 那份计划表、且里面有 `\"cards\"` 键（" + PlanFileAbs + "）", ref bad);

            int fd = FirstDiff(bodyA, bodyB);
            Check(SameCardsBody(bodyA, bodyB),
                  "★ ⑥a **两份 `card_icon_plan.json` 的 `cards` 数组逐字节相同**（`" +
                  (bodyA == null ? "?" : bodyA.Length.ToString()) + "` / `" +
                  (bodyB == null ? "?" : bodyB.Length.ToString()) + "` 字符" +
                  (fd < 0 ? "" : "；**首个不同处在第 " + fd + " 个字符**") +
                  "）。⛔ 头部**不参与**比 —— 两份的 `note` 本来就不同" +
                  "（`Resources` 那份写着「运行时那一份」）。" +
                  "改坏法：只重写其中一份 / 手工改其中一份 ⇒ 红。" +
                  "出处：`工具/gen_icon_plan.py` 是两份一起写，但**那是它的自觉、原来没有断言钉住**（`A1421`）。",
                  ref bad);
            Log("⑥ 读数：`cards` 体 " + (bodyA == null ? "?" : bodyA.Length.ToString()) + " / " +
                (bodyB == null ? "?" : bodyB.Length.ToString()) + " 字符；指纹 FNV-1a64 = " +
                BodyHash(bodyA) + " / " + BodyHash(bodyB) + "；头部 " +
                (rawA == null || bodyA == null ? "?" : (rawA.Length - bodyA.Length).ToString()) + " / " +
                (rawB == null || bodyB == null ? "?" : (rawB.Length - bodyB.Length).ToString()) +
                " 字符（**本来就不同**，别拿它当信号）");

            // ⑥b【灭自证】
            bool pathDistinct = !string.Equals(DataPlanFileAbs, PlanFileAbs, System.StringComparison.OrdinalIgnoreCase);
            string flip = MidFlip(bodyA);
            string cut = (bodyA == null || bodyA.Length <= 100) ? null : bodyA.Substring(0, bodyA.Length - 100);
            Check(pathDistinct
                  && SameCardsBody(bodyA, bodyA)
                  && !SameCardsBody(bodyA, flip)
                  && !SameCardsBody(bodyA, cut)
                  && !SameCardsBody(null, null),
                  "★ ⑥b【灭自证】比较器必须**结构上不可能恒绿**：" +
                  "两条路径确实不同=" + pathDistinct +
                  "（抄成同一条 ⇒ 比的是自己跟自己）；同一份内容 ⇒ **相同**=" + SameCardsBody(bodyA, bodyA) +
                  "（⛔ 若恒「不同」⇒ 这里红）；中间翻一位 ⇒ **不同**=" + !SameCardsBody(bodyA, flip) +
                  "；截掉 100 字符 ⇒ **不同**=" + !SameCardsBody(bodyA, cut) +
                  "；**两边都取不到 ⇒ 必须「不同」**=" + !SameCardsBody(null, null) +
                  "（这条挡的是 `null == null` **恒真**那个洞 —— 取不到键时别当成「相同」）。", ref bad);
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
    /// ④a 的谓词本体：返回 `"卡id|sprite"` 列表 —— **英文 `desc` 画了、中文 `descZh` 没画**的徽记。
    /// 纯函数（只读传入的 doc），⇒ ④b 能拿同一份函数去跑「改回去」的合成表。
    /// </summary>
    static List<string> ZhGaps(PlanDoc doc)
    {
        var gaps = new List<string>();
        if (doc == null || doc.cards == null) return gaps;
        foreach (var c in doc.cards)
        {
            if (c == null || c.fields == null) continue;
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
            var miss = new List<string>();
            foreach (var s in en) if (!zh.Contains(s)) miss.Add(s);
            miss.Sort(System.StringComparer.Ordinal);
            foreach (var s in miss) gaps.Add(c.id + "|" + s);
        }
        gaps.Sort(System.StringComparer.Ordinal);
        return gaps;
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
        //      `ASH_Bright_Lance_Vyper|sniper` · `DA16|shield` · `GOF8|tide` · `UM_Phobos_Lieutenant|stealth`
        //      —— 修法是补数据侧 `数据/游戏数据/cardface_fixes.json` 的 `descZh` 列。
        //    ⛔ **别再往本表里加任何一条**（往里加 = 把新缺口当基线）—— 判据 = `④c`。

        // ── ⚠️ **③ 类那 1 条（`BL19|markOfSlaanesh`）也已在 2026-10-10 修好、已从本表删除**（`A1473`）——
        //    修的是**中文侧那一半**：`数据/游戏数据/cardface_fixes.json` 里 `BL19 Noise Marine` 的
        //    `descZh` 由 `…获得一个 [黑暗契约]纵欲黑暗契约` 改成 `…获得一个 ❄纵欲黑暗契约`
        //    （英文侧**早就是** `❄`）。判据 = **英文卡面**那一半 —— 原版客户端**没有中文表**
        //    （全仓已定案，`资料/全量反编译复核_靠推断的清单.md` §2.2）⇒ 中文侧唯一可核的是英文卡面口径。
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
    /// 🔴 现读（2026-10-10；`A1473` 删掉最后那 1 条之后）：表 **0** 条、
    ///    `ZhGaps(今天的计划表)` 也正好 **0** 条、**无陈旧条目**。
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
    /// ④b「**改回去**」合成表：`"卡id|sprite"` —— 把这两笔落地的中文徽记**从计划表里抽掉**，
    /// ④a 的谓词**必须**报出来。出处：
    ///   · `A1360`（`资料/普查产出_第十四会话/W_A1360中文段首词.md` §一）= `UM74 誓言 3：` / `GOF8 集结：` /
    ///     `GOF21 群体：` / `DA16 议程：` / `EC35 激励：` 这 5 条（都是**段首**关键词）；
    ///   · `A1400`（同目录 `W_图标族8词.md` §四 1）= `Invulnerable` 那 **14 张**（**句中**裸词）。
    /// ⚠️ 与真表的关系：这 19 条今天**都在**真计划表的 `descZh` 里（两笔都落地了）；
    ///   ④b 抽掉的是**副本**，⛔ 不碰盘上那份 json。
    /// </summary>
    static readonly string[] RevertControl =
    {
        "UM74|oath", "GOF8|rally", "GOF21|mob", "DA16|agenda", "EC35|stimulation",
        "ASH21|invulnerable", "AM36|invulnerable", "BL60|invulnerable", "DA_Rites_of_Penance|invulnerable",
        "DA82|invulnerable", "GSC58|invulnerable", "GSC47|invulnerable", "SOR38|invulnerable",
        "SW29|invulnerable", "SW18|invulnerable", "TL61|invulnerable", "UM88|invulnerable",
        "UM52|invulnerable", "UM96|invulnerable",
    };

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
    /// </summary>
    static string CardsBody(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        int i = raw.IndexOf("\"cards\"", System.StringComparison.Ordinal);
        return i < 0 ? null : raw.Substring(i);
    }

    /// <summary>
    /// ⑥ 的**比较器本体**（纯函数 ⇒ ⑥b 拿它跑变体）。
    /// 🔴 `null` **不算「相同」** —— 这就是 `null == null` 恒真那个洞（两边都取不到键时，
    /// 若拿 `==` 直接比，断言会**恒绿**）。
    /// </summary>
    static bool SameCardsBody(string a, string b)
    {
        return a != null && b != null && string.Equals(a, b, System.StringComparison.Ordinal);
    }

    /// <summary>⑥b 变体一：把**正中间那一位**翻掉（**长度不变** ⇒ 挡「只比长度」那种退化比较器）</summary>
    static string MidFlip(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        int i = s.Length / 2;
        return s.Substring(0, i) + (char)(s[i] ^ 1) + s.Substring(i + 1);
    }

    /// <summary>⑥ 报错时用：**第一处不同**的下标（两边完全一样 ⇒ -1）—— 给人一个可复核的坐标，不是只报「不等」</summary>
    static int FirstDiff(string a, string b)
    {
        if (a == null || b == null) return -1;
        int n = System.Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++) if (a[i] != b[i]) return i;
        return a.Length == b.Length ? -1 : n;
    }

    /// <summary>
    /// ⑥ 的读数用：FNV-1a 64 位指纹（`cards` 体 90 万字符用；**给人一个能在外面对着算的指纹**）。
    /// 不引 `System.Security.Cryptography` 的 sha256 —— 这里只当「读数」，不当判据。
    /// </summary>
    static string BodyHash(string s)
    {
        if (s == null) return "-";
        ulong h = 14695981039346656037UL;
        foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
        return h.ToString("x16");
    }

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
