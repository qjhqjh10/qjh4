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
        //     （`[Armor]` 只剩文件头 `:10` 那段描述旧 OCR 数据的老注释里还提得到）。
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
        //     `<sprite name="{sprite}">`（`Core/CardIcons.cs:238`）⇒ **这条今天应当绿**。
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
        //     （`BattleDriver.cs:11959`：`if (string.IsNullOrEmpty(body)) return new FaceBody(notes, "desc");`
        //      —— 那一行 2026-09-15 的 `720df56` 就在，**比这两条断言早**）
        //     ⇒ 探针**根本走不到** `KeywordSegment`（`BattleDriver.cs:11975`）那一行
        //     ⇒ ⑦ 自称钉的 `c.NumericKeywords`、⑦b 自称钉的「数组序」，**一条都没被覆盖过**。
        //   · ⚠️ **不是「batchmode / 空场景拿不到卡面文本」**（那是交件时的猜测）：`FaceTextFull` 是
        //     **纯静态函数**（只碰 `CardText` / `Badges` / `CardIcons` 那几个静态口），
        //     **不碰 `CardView`、不要战斗场景、不要 Canvas** —— 空串是**这一条早退**造成的，实据就是 `:11959`。
        //   · **修法（两件一起做）**：① 补一句**不含任何关键词声明**的 `desc`（下面 `ProbeBody`），
        //     让探针真走到那一行；② 加一条**前提断言**（`⑦·前提` / `⑦b·前提`）——
        //     前提一破就**明确报「夹具没走到 `KeywordSegment`」**，而不是像原来那样
        //     静默取空串、只报「`Vanguard 1` 没印出来」（那条读起来像生产代码坏了）。
        //     ⚠️ 本仓红线：**不许静默失败** ⇒ 前提必须自己有名字。
        {
            // ⛔ 这句 `desc` **不许**出现 `Vanguard` / `Shield` / `先锋` / `盾牌` 这些词：
            //    `CardText.KeywordSegment:248` 的 `DeclaredInBody` 会把「正文里已声明」的那一项**整段跳掉**，
            //    `:251` 连**中文写法**也判一次（英文档下 `other` 取的就是 `KeywordZh`），
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
            //   两张探针**必须真的走到** `CardText.KeywordSegment`（`BattleDriver.cs:11975`）——
            //   判据 = 卡面段**非空**。`FaceTextFull` 在 `:11959` 有一条**空正文早退**
            //   （`if (string.IsNullOrEmpty(body)) return new FaceBody(notes, "desc");`）：
            //   夹具的 `desc` 一旦被改回空串（`A1627` 修之前就是这样）⇒ **下面那条 ⑦ 会红**，
            //   而它打的是「`Vanguard 1` 没印出来」——**读起来像生产代码坏了**（`A1417` 那笔账的假象）。
            //   这一条把那件事**说成它自己的名字**：红的是前提，不是被钉的那一行。
            bool premiseOk = got.Length > 0 && gotCtrl.Length > 0;
            Check(premiseOk,
                  "★ ⑦·前提 **夹具必须真的走到 `CardText.KeywordSegment`**（`A1627`）：两张探针的卡面段" +
                  "**都非空**（= `" + premiseOk + "`）—— 空正文会被 `BattleDriver.FaceTextFull:11959`" +
                  "**提前返回**、根本到不了 `:11975` 那一行（那时 ⑦ 会以「没印出数字」的形状红，**是假象**）。" +
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
        foreach (var c in doc.cards)
        {
            if (c == null || c.fields == null) continue;
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
            //   **根本不查这张计划表**（判据 = `Core/CardIcons.cs:227` / `:448-453`），图标由 `RewriteMarks` 自己画。
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
