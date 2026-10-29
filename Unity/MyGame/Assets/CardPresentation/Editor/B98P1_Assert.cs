// B98P1_Assert.cs — 第九十八会话 · 波 13 · 执行代理 `B98_P1` 的断言（卡框显示矩形 / sprite rect / 导入目标名 / 能力文本 ref）
//
// ============================ 它盯的是什么（判据只写在这里一次）============================
//  甲 卡框尺寸：原版 `2DCard/CardFrame` 那颗 RectTransform 的**显示矩形**。
//      判据（2026-10-29 `B98_P1` 现读）：
//        · `assetripper_out/ExportedProject/Assets/**/*.prefab` 全扫 ⇒ **44 份**带 `CardFrame` 节点的 prefab
//          锚点/尺寸**完全一致**：`m_AnchorMin == m_AnchorMax == (0.5,0.5)`（**居中，不是拉伸**）、
//          `m_SizeDelta = (2.2452, 3.2572)`、`m_AnchoredPosition.y = -0.03`、`m_LocalScale = 1`
//          ⇒ `rect.rect == sizeDelta == 2.2452 × 3.2572`。
//        · `CardTierUIController.SetTier/SetFrame` 与 `Card2DController` 全族 14 个方法**只换 Sprite、不写
//          RectTransform**（`decomp_full/CardTierUIController__SetTier.c` / `Card2DController__SetFrame.c`）
//          ⇒ **各档同一尺寸**（`B98_M1` 说的「各档不同」不成立）。
//        · 原版 Sprite 的 `m_Rect` = **(171,11,682,998)**、PPU 100、pivot .5 —— **104/104 全一样**
//          （UnityPy 直读原版 bundle 现核）⇒ 画进去的是**整块 sprite rect**（`m_UseSpriteMesh=1`）。
//        · 原版卡框贴图 **1024 × 1024**（104/104）。
//  丙 能力文本 ref（`CardDef.ActiveAbilityTextReferenceId` + `CardDatabase.CardDto` 的列）

// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · P4/P5：把 `CardArtClanFrames.SpriteRectW` 改成 `textureRect` 那组数（611.9）⇒ 与 `CardView` 的常量不符 ⇒ 红。
//  · P6：`CardView.FrameQuadSize` 若被改成别的（例如按 alpha bbox 等比内接）⇒ 红。
//  · P7：用一张**不透明内容只占左下角**的 1024² 探针贴图 ⇒ 若 `FrameUv` 走 alpha bbox 而不是 sprite rect，
//        返回值的宽会≈0.098 而不是 0.666 ⇒ 红（这条把「名字/尺寸闸失效」暴露出来）。
//  · P8：同一张探针**不叫 `frame_*`` ⇒ 必须走 alpha bbox（宽 < 0.2）⇒ 证明 P7 的判别力不是假的。
//  · P11：`ImportResourceName` 去掉 `frame_clan_` 前缀 ⇒ P7 那条（`IsCardFrame` 靠 `frame_` 前缀）在导图后失效 ⇒ 红。

// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/BattleScene.cs` 或 `Editor/RuleEngineTest.cs` 的 `Run()` 里加：
//      B98P1_Assert.Run(); _pass += B98P1_Assert.Pass; _fail += B98P1_Assert.Fail;
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B98_P1`：卡框显示矩形（固定 2.2452×3.2572）· sprite rect (171,11,682,998) · 导入目标名 · 能力文本 ref。</summary>
public static class B98P1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[P1] ") { Near = MenuNearStyle.Compact3 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    static readonly int[] Armies = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 };

    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_P1 卡框：原版显示矩形（2.2452×3.2572 固定）+ sprite rect(171,11,682,998) + 导入目标名 + 能力文本 ref");
        SectionSpriteRect();
        SectionDisplaySize();
        SectionFrameUvBranch();
        SectionImportNames();
        SectionActiveAbilityRef();
        Debug.Log(_sink.P + "B98P1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==========================================================================================
    //  A —— 原版 Sprite 的 `m_Rect`（104/104 常量）+ 表里那组数其实是 `textureRect`
    // ==========================================================================================
    static void SectionSpriteRect()
    {
        MenuCheck.Section(_sink, "A sprite rect：`m_Rect` = (171,11,682,998) 常量 · 表里的 X/Y/W/H = `textureRect`（落在它里面）");

        // P0 = 前提硬检查（⛔ 不静默跳过）
        C(CardArtClanFrames.Count, 104, "P0 **前提**：表里 104 条槽位（13 阵营 × 4 档 × 2 类型）");

        Near(CardArtClanFrames.SpriteRectX, 171f, 1e-4f, "P1 `m_Rect.x` = 171（原版 104/104 全一样；UnityPy 直读 bundle）");
        Near(CardArtClanFrames.SpriteRectY, 11f, 1e-4f, "P1-b `m_Rect.y` = 11（**贴图底边**为原点）");
        Near(CardArtClanFrames.SpriteRectW, 682f, 1e-4f, "P1-c `m_Rect.width` = 682"
            + "｜🧨 改成 `textureRect` 那组数（611.9）⇒ P4 与本行一起红");
        Near(CardArtClanFrames.SpriteRectH, 998f, 1e-4f, "P1-d `m_Rect.height` = 998");
        C(CardArtClanFrames.TextureSize, 1024, "P2 原版卡框贴图画布 = 1024 × 1024（104/104；`CardView.IsCardFrame` 的另一半闸）");
        Near(CardArtClanFrames.PixelsToUnits, 100f, 1e-6f, "P2-b `m_PixelsToUnits` = 100");

        // 表里那 104 组数 = `textureRect`（不透明紧包围盒）⇒ **必须整块落在 `m_Rect` 里面**
        int bad = 0, n = 0;
        for (int a = 0; a < Armies.Length; a++)
            for (int tier = 1; tier <= 4; tier++)
                for (int k = 0; k < 2; k++)
                {
                    CardArtClanFrames.Entry e;
                    if (!CardArtClanFrames.TryGet(Armies[a], tier, k == 1, out e)) { bad++; continue; }
                    n++;
                    bool inside = e.X >= CardArtClanFrames.SpriteRectX - 0.5f
                               && e.Y >= CardArtClanFrames.SpriteRectY - 0.5f
                               && e.X + e.W <= CardArtClanFrames.SpriteRectX + CardArtClanFrames.SpriteRectW + 0.5f
                               && e.Y + e.H <= CardArtClanFrames.SpriteRectY + CardArtClanFrames.SpriteRectH + 0.5f;
                    if (!inside) bad++;
                }
        C(n, 104, "P3 **前提**：104 格全部读得到（`TryGet` 命中）");
        C(bad, 0, "P4 每条的 `textureRect` 都落在原版 `m_Rect`(171,11,682,998) 里（4 条边界各留 0.5 px 容差）"
               + "｜🧨 若把 `textureRect` 当成 `m_Rect` 去改常量 ⇒ P1/P4 至少一条红");
    }

    // ==========================================================================================
    //  B —— 显示矩形（原版 `CardFrame` 的 RectTransform = 2.2452 × 3.2572，各档不变）
    // ==========================================================================================
    static void SectionDisplaySize()
    {
        MenuCheck.Section(_sink, "B 显示矩形：44 份 prefab 全同 · 居中锚点（rect.rect = sizeDelta）· 各档不变");

        // P5 前提：`CardView` 里那组常量**真的在**（判据的另一半在那边，这里做交叉锁）
        var t = typeof(CardView);
        var fW = t.GetField("FrameUnitW", BindingFlags.NonPublic | BindingFlags.Static);
        var fH = t.GetField("FrameUnitH", BindingFlags.NonPublic | BindingFlags.Static);
        var fY = t.GetField("FrameUnitY", BindingFlags.NonPublic | BindingFlags.Static);
        var fSX = t.GetField("FrameSpriteRectX", BindingFlags.NonPublic | BindingFlags.Static);
        var fSY = t.GetField("FrameSpriteRectY", BindingFlags.NonPublic | BindingFlags.Static);
        var fSW = t.GetField("FrameSpriteRectW", BindingFlags.NonPublic | BindingFlags.Static);
        var fSH = t.GetField("FrameSpriteRectH", BindingFlags.NonPublic | BindingFlags.Static);
        bool all = fW != null && fH != null && fY != null && fSX != null && fSY != null && fSW != null && fSH != null;
        Tr(all, "P5 **前提**：`CardView` 的 7 个私有常量都还在（`FrameUnitW/H/Y` + `FrameSpriteRectX/Y/W/H`）");
        if (!all) return;

        float fWv = (float)fW.GetRawConstantValue(), fHv = (float)fH.GetRawConstantValue(), fYv = (float)fY.GetRawConstantValue();
        float fSXv = (float)fSX.GetRawConstantValue(), fSYv = (float)fSY.GetRawConstantValue();
        float fSWv = (float)fSW.GetRawConstantValue(), fSHv = (float)fSH.GetRawConstantValue();

        Near(CardArtClanFrames.DisplayW, fWv, 1e-5f, "P5-a 显示宽 = `CardView.FrameUnitW` = 2.2452（两边必须一致 —— 两处写同一条判据，靠这条锁死）");
        Near(CardArtClanFrames.DisplayH, fHv, 1e-5f, "P5-b 显示高 = `CardView.FrameUnitH` = 3.2572");
        Near(CardArtClanFrames.DisplayY, fYv, 1e-5f, "P5-c 显示中心 y = `CardView.FrameUnitY` = -0.03（prefab `m_AnchoredPosition.y`）");
        Near(CardArtClanFrames.SpriteRectW, fSWv, 1e-4f, "P5-d sprite rect 宽两边一致（682）");
        Near(CardArtClanFrames.SpriteRectH, fSHv, 1e-4f, "P5-e sprite rect 高两边一致（998）");
        Near(CardArtClanFrames.SpriteRectX, fSXv, 1e-4f, "P5-f sprite rect x 两边一致（171）");
        Near(CardArtClanFrames.SpriteRectY, fSYv, 1e-4f, "P5-g sprite rect y 两边一致（11）");

        // P6 = **渲染尺寸 = 原版矩形**（世界单位；`Width == CardUnitW` ⇒ s = 1）
        var q = CardView.FrameQuadSize;
        Near(q.x, 2.2452f, 1e-4f, "P6 **渲染宽** = 2.2452 卡单位（= 原版 `CardFrame.m_SizeDelta.x`）"
            + "｜🧨 改成「按 alpha bbox 等比内接」⇒ 本行红");
        Near(q.y, 3.2572f, 1e-4f, "P6-b **渲染高** = 3.2572（= 原版 `m_SizeDelta.y`；高的比宽大得多，⛔ 别拿宽反推）");
        Near(q.z, -0.03f, 1e-4f, "P6-c 中心 y = -0.03（原版 `m_AnchoredPosition.y`）");

        // 与卡本体的比例（判据的另一种写法：prefab 数直接除出来的两个比值）
        Near(CardArtClanFrames.DisplayW / CardView.CardUnitW, 1.072939f, 2e-4f,
            "P6-d 显示宽 / 卡本体宽 = 2.2452 / 2.0927 = 1.07294（原版卡框比卡**宽** 7.3%）");
        Near(CardArtClanFrames.DisplayH / CardView.CardUnitH, 0.977694f, 2e-4f,
            "P6-e 显示高 / 卡本体高 = 3.2572 / 3.3313 = 0.97769（原版卡框比卡**矮** 2.2%）");

        // 真建一张卡：`FrameDrawSize`（那一层网格的实际 bounds）必须等于原版矩形
        var root = new GameObject("B98P1_FRAME_ROOT");
        CardView v = null;
        try
        {
            var d = new CardData
            {
                id = "B98P1_FRAME", title = "B98P1", isUnit = true, type = "troop",
                faction = "Ultramarines", cost = 1, melee = 1, health = 1, keywords = "x",
            };
            v = CardView.Create(root.transform, d, "B98P1_FRAME");
            Tr(v.FrameTexture != null, "P7 **前提**：卡框层建出来了（`FrameTexture != null`；否则 P7-b 等于没跑）");
            var sz = v.FrameDrawSize;
            Near(sz.x, 2.2452f, 2e-3f, "P7-b **真建出来的卡框网格宽** = 2.2452（`FrameDrawSize` 的 x，不是拿常量比常量）");
            Near(sz.y, 3.2572f, 2e-3f, "P7-c **真建出来的卡框网格高** = 3.2572");
        }
        finally
        {
            if (v != null) UnityEngine.Object.DestroyImmediate(v.gameObject);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ==========================================================================================
    //  C —— `FrameUv` 的分支：原版卡框按 sprite rect，其余按 alpha bbox
    // ==========================================================================================
    static void SectionFrameUvBranch()
    {
        MenuCheck.Section(_sink, "C FrameUv：1024² + `frame_` 前缀 ⇒ sprite rect；否则 ⇒ alpha bbox（判据须能区分）");

        // 探针贴图：1024²，不透明内容只占**左下角 100×100**（alpha bbox 宽 ≈0.098，与 sprite rect 宽 0.666 差很远）
        Texture2D probe = null, probe2 = null;
        try
        {
            probe = MakeProbe("frame_clan_B98P1_probe");
            var uv = CardView.FrameUv(probe);
            Near(uv.x, 171f / 1024f, 1e-5f, "P8 原版卡框探针 ⇒ `FrameUv.x` = 171/1024（sprite rect，不是 alpha bbox 的 0）");
            Near(uv.y, 11f / 1024f, 1e-5f, "P8-b `FrameUv.y` = 11/1024");
            Near(uv.width, 682f / 1024f, 1e-5f, "P8-c **`FrameUv.width` = 682/1024 = 0.666**"
                + "｜🧨 若走 alpha bbox ⇒ ≈0.098 ⇒ 本行红（这就是「可见内容会被放大 11.5%」的那个岔口）");
            Near(uv.height, 998f / 1024f, 1e-5f, "P8-d `FrameUv.height` = 998/1024");
            var sr = CardArtClanFrames.SpriteRectUV;
            Tr(Mathf.Abs(uv.x - sr.x) < 1e-5f && Mathf.Abs(uv.y - sr.y) < 1e-5f
            && Mathf.Abs(uv.width - sr.width) < 1e-5f && Mathf.Abs(uv.height - sr.height) < 1e-5f,
               "P8-e `FrameUv` 与 `CardArtClanFrames.SpriteRectUV` 逐分量一致（两处写同一条判据，靠这条锁死）");

            // 灭自证：同样的内容、只把名字换成不叫 `frame_` 的 ⇒ 必须走 alpha bbox
            probe2 = MakeProbe("B98P1_not_a_frame");
            var uv2 = CardView.FrameUv(probe2);
            Tr(uv2.width < 0.2f, "P9 **灭自证**：同一张探针不叫 `frame_*` ⇒ `FrameUv.width` < 0.2（走 alpha bbox）"
               + "，实得 " + uv2.width.ToString("F4") + " —— 证明 P8 的判别力来自那条闸，不是常量蒙对｜🧨 把 `IsCardFrame` 改成恒真 ⇒ 本行红");
        }
        finally
        {
            if (probe != null) UnityEngine.Object.DestroyImmediate(probe);
            if (probe2 != null) UnityEngine.Object.DestroyImmediate(probe2);
        }
    }

    static Texture2D MakeProbe(string name)
    {
        var t = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
        t.name = name;
        var px = new Color32[1024 * 1024];
        for (int y = 0; y < 100; y++)
            for (int x = 0; x < 100; x++)
                px[y * 1024 + x] = new Color32(255, 255, 255, 255);
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    // ==========================================================================================
    //  D —— 导入目标名（`frame_clan_<ResourceName>`，104 个不重复、必须 1024² + `frame_` 前缀）
    // ==========================================================================================
    static void SectionImportNames()
    {
        MenuCheck.Section(_sink, "D 导入目标名：`frame_clan_<ResourceName>`（104 个不重复）· 导入后必须 1024² 才能走 sprite rect");

        var seen = new HashSet<string>();
        int miss = 0, bad = 0;
        for (int a = 0; a < Armies.Length; a++)
            for (int tier = 1; tier <= 4; tier++)
                for (int k = 0; k < 2; k++)
                {
                    string n = CardArtClanFrames.ImportResourceName(Armies[a], tier, k == 1);
                    if (n == null) { miss++; continue; }
                    if (!n.StartsWith("frame_clan_", StringComparison.Ordinal)) bad++;
                    if (n != "frame_clan_" + CardArtClanFrames.ResourceName(Armies[a], tier, k == 1)) bad++;
                    if (!seen.Add(n)) bad++;
                }
        C(miss, 0, "P10 104 格全部拿得到导入名（`ImportResourceName` 不返回 null）");
        C(bad, 0, "P10-b 104 个名字**互不重复**、都以 `frame_clan_` 开头、且 = `frame_clan_` + `ResourceName`"
                + "｜🧨 去掉前缀 ⇒ 导进来的贴图掉进 alpha-bbox 分支（P8 那条判据在真资产上就会红）");
        C(seen.Count, 104, "P10-c 去重后仍是 104 个（同名会让两张卡共用一个框）");

        // 今天没导 ⇒ 0 张；导了就必须 104 张、而且每张都得能被当成「原版卡框」（名字/尺寸两条闸）
        int loaded = 0, wrong = 0;
        for (int a = 0; a < Armies.Length; a++)
            for (int tier = 1; tier <= 4; tier++)
                for (int k = 0; k < 2; k++)
                {
                    var tex = CardArtClanFrames.FrameTexture(Armies[a], tier, k == 1);
                    if (tex == null) continue;
                    loaded++;
                    string want = CardArtClanFrames.ImportResourceName(Armies[a], tier, k == 1);
                    if (tex.name != want || tex.width != 1024 || tex.height != 1024) wrong++;
                    else
                    {
                        var uv = CardView.FrameUv(tex);
                        if (Mathf.Abs(uv.width - 682f / 1024f) > 1e-4f
                         || Mathf.Abs(uv.height - 998f / 1024f) > 1e-4f) wrong++;
                    }
                }
        Tr(loaded == 0 || loaded == 104, "P11 **状态**：原版卡框贴图要么**一张没导**（今天 = 0），要么**104 张全导**"
            + "（半批会让不同档的框一半真一半自绘）—— 实测 " + loaded);
        C(wrong, 0, "P11-b 导进来的那批：名字 = 导入名 · 尺寸 1024² · `FrameUv` = sprite rect"
                 + "（今天 0 张 ⇒ 本条是空过，但 `wrong` 永远不该非 0）｜🧨 导成 682×998 ⇒ 本行红");
    }

    // ==========================================================================================
    //  E —— 能力文本 ref（`CardDef` 的挂载口 + `CardDto` 的列）
    // ==========================================================================================
    static void SectionActiveAbilityRef()
    {
        MenuCheck.Section(_sink, "E 能力文本 ref：`CardDef.ActiveAbilityTextReferenceId` 三态 + `CardDto` 的列");

        var f = typeof(CardDatabase.CardDto).GetField("activeAbilityTextReferenceId");
        Tr(f != null && f.FieldType == typeof(string),
            "P12 **结构**：`CardDto` 有 `activeAbilityTextReferenceId`（string）这一列"
          + "｜🧨 删掉那个 DTO 字段 ⇒ 生成器出了数据也进不来 ⇒ 红");

        var def = new CardDef("B98P1_REF", "B98P1", "troop", "", "common", "Ultramarines", 0, 0, 1, 0);
        Tr(def.ActiveAbilityTextReferenceId == null, "P13 默认 `null` = **判不了**（夹具直建 / 旧表）");
        def.AttachActiveAbilityTextReference("SOR30_new");
        C(def.ActiveAbilityTextReferenceId, "SOR30_new", "P13-b 挂载后 = 传入值（`SkillPanel.AbilityKeyId` 会用它取键）");
        def.AttachActiveAbilityTextReference("");
        C(def.ActiveAbilityTextReferenceId, "", "P13-c **空串保空**（三态分开：`\"\"` = 明确没有 ref、回退 Id；"
            + "⛔ 别归一成 null）｜🧨 在挂载口写 `refId ?? null` 之类的归一 ⇒ 本行红");
        def.AttachActiveAbilityTextReference(null);
        Tr(def.ActiveAbilityTextReferenceId == null, "P13-d 再挂 `null` 能挂回去（挂载口是覆盖写，不是只写一次）");
    }
}
