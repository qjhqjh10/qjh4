// B106_W9_Assert.cs — 第一百零六会话 · 波 1 · 执行代理 `B106_W9`（AUD02 卡 UI ❌ 9 条）的断言
//
// ============================ 它盯的是什么（判据只写在这里一次）============================
//  A 字号分档 —— `Core/CardView.cs` 的 `NameUnitBandMaxForUse` / `DescUnitBandMaxForUse` /
//     `ArmyFontSizeForUse` / `RaceFontSizeForUse`（按 `CardUse` 分档）。
//     判据 = `bundle_menus_assets_all` 逐实例现读（按 `m_fontSize` / `m_fontSizeMax` 分组）：
//       · 标准档：卡名 `19/19` · 效果 `23.55/24` · 兵种 `18/18` · 阵营 `15/15`
//       · 开包 / 轮抽 / 目录商店（7 个实例）：卡名 `16.3/`**`20`** · 效果 `19/`**`19`** ·
//         兵种 `17/`**`17`** · 阵营 `17/`**`17`**
//     🔴 **有效字号看 `m_fontSizeMax`**（TMP autosize 从 `clamp(fontSizeBase,min,max)` 起一路涨到 max，
//        `TextMeshPro.cs:4139`）⇒ 名义 `m_fontSize`（16.3 / 19 / 17 / 14）只是起点。
//     ⚠️ 商店页签那 5 个实例 `fs 16 / max 19` ⇒ **上界与标准档相同** ⇒ 不单开一档（本文件 A6 盯这一条）。
//  B 角标九宫格 —— `Core/CardView.cs` 的 `SlicedBadgeQuad`（原版 Image `m_Type=1(Sliced)`）。
//     判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-944947045233275165.json`：
//       `m_Type=1` · `m_PixelsPerUnitMultiplier=1.0` · `m_PreserveAspect=0`；sprite `WF_Special offer_Value`
//       `324×87` · `m_Border=(162,0,162,0)` · `m_PixelsToUnits=100`；rect `1.2101×0.392`。
//     ⇒ 边宽 162×100/(100×1.0)=162 卡单位 > 矩形 1.2101 ⇒ uGUI `GetAdjustedBorders`（`Image.cs:1479`）
//       把两边缩到 0.6051、中段 0 ⇒ **只画左右 2 块（8 顶点）**；两块拼起来仍覆盖整块矩形。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1：把 `NameUnitBandMaxForUse` 改回恒 `NameUnitBandMax` ⇒ A1 红（开包族回到 1.90）。
//  · A2：把 `DescUnitBandMaxForUse` 改回恒 `DescUnitBandMax` ⇒ A2 红（开包族回到 2.40）。
//  · A3/A4：把 `ArmyFontSizeForUse` / `RaceFontSizeForUse` 改回静态常量 ⇒ A3/A4 红（比值回 1）。
//  · A3 与 A4 **方向相反**（阵营 17>15、兵种 17<18）⇒ 不可能靠「开包族整体放大/缩小」同时满足。
//  · A5：标准档两行改掉任何一个 ⇒ A5 红（防「顺手把标准档也改了」）。
//  · A6：给 `CardUse.Booster` 也套上「商店页签 = 16」那档（把 band max 改小）⇒ A6 红。
//  · B1：把 `SlicedBadgeQuad` 换回 `SpriteQuad` ⇒ B1 红（顶点数 4 而不是 8）。
//  · B2：把边宽算式写成「× ppuMul」那类方向反的式子 ⇒ 两边缩不到刚好装下 ⇒ B2 红（外接尺寸 ≠ 矩形）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs`（或 `CardBaseDemo.cs`）的 `Run()` 里那一串代接线之后加：
//      B106W9_Assert.Run(); pass += B106W9_Assert.Pass; fail += B106W9_Assert.Fail;
//  （宿主由调度台代接线；本文件只碰 `CardPresentation` 的只读口 ＋ 自建临时 GameObject，不写盘。）
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B106_W9`：AUD02 卡 UI —— 字号按宿主分档（A）+ 新卡角标九宫格（B）。</summary>
public static class B106W9_Assert
{
    static readonly CheckSink _sink = new CheckSink("[W9] ") { Near = MenuNearStyle.Compact3 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B106_W9 AUD02 卡 UI：字号按宿主分档 + 角标九宫格");
        SectionA_FontTiers();
        SectionB_BadgeSliced();
        Debug.Log(_sink.P + "B106W9 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==========================================================================================
    //  A 段 —— 字号按宿主分档
    // ==========================================================================================
    static void SectionA_FontTiers()
    {
        MenuCheck.Section(_sink, "A 字号分档：标准档 19/24/18/15 ↔ 开包族 20/19/17/17（原版 m_fontSizeMax）");

        // A0 = 前提硬检查（⛔ 不静默跳过 —— 字体不可用则文字层根本不建，A1–A6 等于没跑）
        Tr(TmpFont.Available, "A0 **前提**：TMP 字体资产可用（`TmpFont.Available`）");
        if (!TmpFont.Available) return;

        var root = new GameObject("B106W9_A_ROOT");
        CardView std = null, boost = null, stdTactic = null;
        try
        {
            var d = UnitData();
            std = CardView.Create(root.transform, d, "B106W9_std");
            boost = CardView.Create(root.transform, d, "B106W9_boost", CardFace.Full, null, CardUse.Booster);
            stdTactic = CardView.Create(root.transform, TacticData(), "B106W9_tactic");

            Tr(std != null && boost != null && stdTactic != null, "A0-a **前提**：三张夹具卡都建出来了");
            if (std == null || boost == null || stdTactic == null) return;

            Near(std.NameBandMaxForTest, 1.425f, 1e-3f,
                 "A1 标准档卡名 autosize 上界 = 1.425（原版 `NameTextUnit` 19 pt ÷10 × shipped 实拍 0.75；`B117_F14`，判据 → `CardView.TitleEmOfCard`）"
               + "｜🧨 把 `NameUnitBandMaxForUse` 写死回常量 ⇒ 本行在开包那一侧才红（见 A1b）");
            Near(boost.NameBandMaxForTest, 1.500f, 1e-3f,
                 "A1b 开包族卡名上界 = 1.500（原版开包实例 `m_fontSizeMax = 20` × 0.75）"
               + "｜🧨 改回恒 `NameUnitBandMax`(1.90) ⇒ 本行红");

            Near(std.DescBandMaxForTest, 1.872f, 1e-3f, "A2 标准档效果文字上界 = 1.872（`DescTextUnit` 24 pt ÷10 × shipped 实拍 0.78；`B117_F14`）；名义 `KeywordFontSize` 仍是 prefab 的 23.55");
            Near(boost.DescBandMaxForTest, 1.482f, 1e-3f,
                 "A2b 开包族效果文字上界 = 1.482（原版开包实例 `19/max 19` × 0.78）"
               + "｜🧨 改回恒 `DescUnitBandMax`(2.40) ⇒ 本行红");

            float armyRatio = boost.ArmyFontSizeForTest(true) / Mathf.Max(1e-6f, std.ArmyFontSizeForTest(true));
            Near(armyRatio, 17f / 15f, 2e-3f,
                 "A3 开包族 / 标准档 **单位卡**阵营行字号比 = 17/15（max 17 ↔ 15）"
               + "｜🧨 把 `ArmyFontSizeForUse` 改回静态 `ArmyFontSize` ⇒ 比值回 1.0 ⇒ 本行红");
            float raceRatio = boost.RaceFontSizeForTest(true) / Mathf.Max(1e-6f, std.RaceFontSizeForTest(true));
            Near(raceRatio, 17f / 18f, 2e-3f,
                 "A4 开包族 / 标准档 **单位卡**兵种行字号比 = 17/18（max 17 ↔ 18，**变小**）"
               + "｜🧨 把 `RaceFontSizeForUse` 改回静态 `RaceFontSize` ⇒ 比值回 1.0 ⇒ 本行红");

            Near(std.ArmyFontSizeForTest(true), CardView.ArmyFontSize, 1e-4f,
                 "A5 标准档阵营行 = 静态 `ArmyFontSize`（零回归）｜🧨 顺手改掉标准档 ⇒ 本行红");
            Near(std.RaceFontSizeForTest(true), CardView.RaceFontSize, 1e-4f,
                 "A5b 标准档兵种行 = 静态 `RaceFontSize`（零回归）");

            // 商店页签那一档「fs 16 / max 19」= 标准档 ⇒ 有效字号相同，**不单开分支**。
            Near(boost.NameBandMaxForTest, std.NameBandMaxForTest * 20f / 19f, 1e-3f,
                 "A6 开包族 / 标准档卡名上界之比 = **20/19**（两档同乘 0.75 ⇒ 比值不变）—— 商店页签那档（16/19）与标准档**同界** ⇒ 不分档");

            Near(stdTactic.ArmyFontSizeForTest(false), std.ArmyFontSizeForTest(true), 1e-4f,
                 "A7 标准档：战术卡阵营行与单位卡阵营行**同值**（原版 `ArmyTextTactc` 41/41 `m_fontSizeMax = 15`"
               + " ⇒ 有效上界 = 15 = 我们的 0.15；⚠️ 名义 `fs 14` 只是 autosize 起点，⛔ 别改成 0.14）");
            Near(boost.ArmyFontSizeForTest(false), std.ArmyFontSizeForTest(false), 1e-4f,
                 "A7b 开包族**战术卡**阵营行 = 标准档（两棵 prefab 的 `ArmyTextTactc` 都是 `14/max 15`）"
               + "｜🧨 把 `unit` 那一档去掉、开包族一律 0.17 ⇒ 本行红");
            Near(boost.RaceFontSizeForTest(false), std.RaceFontSizeForTest(false), 1e-4f,
                 "A7c 开包族**战术卡**兵种行 = 标准档 0.18（`Tactic Container/RaceText` 两棵 prefab 都是 18/18）"
               + "｜🧨 同上 ⇒ 本行红");

            // A8 —— 宿主推断：开包窗**不传 `use`**，靠原版宿主节点名认档（`IsBoosterCardView`）。
            var bp = new GameObject("Booster Animation Parent");
            bp.transform.SetParent(root.transform, false);
            var cardsNode = new GameObject("Cards");
            cardsNode.transform.SetParent(bp.transform, false);
            var inCard = new GameObject("CardInBoosterPack UI 1");
            inCard.transform.SetParent(cardsNode.transform, false);
            var inferred = CardView.Create(inCard.transform, d, "CardUI");
            Tr(inferred != null && inferred.Use == CardUse.Booster,
               "A8 挂在 `Booster Animation Parent/Cards/…` 下、**不传 `use`** 的卡自动落到 `CardUse.Booster`"
             + "｜🧨 把 `IsBoosterCardView` 删掉（`v._use = use`）⇒ 本行红");
            if (inferred != null) Object.DestroyImmediate(inferred.gameObject);
            Object.DestroyImmediate(bp);
        }
        finally
        {
            if (std != null) Object.DestroyImmediate(std.gameObject);
            if (boost != null) Object.DestroyImmediate(boost.gameObject);
            if (stdTactic != null) Object.DestroyImmediate(stdTactic.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    // ==========================================================================================
    //  B 段 —— 新卡角标 = 原版的 Sliced 九宫格
    // ==========================================================================================
    static void SectionB_BadgeSliced()
    {
        MenuCheck.Section(_sink, "B 新卡角标：原版 `m_Type=1(Sliced)` ⇒ 左右两块、中段 0（8 顶点）");

        var root = new GameObject("B106W9_B_ROOT");
        CardView v = null;
        try
        {
            v = CardView.Create(root.transform, UnitData(), "B106W9_badge");
            Tr(v != null, "B0 **前提**：夹具卡建出来了");
            if (v == null) return;

            v.ToggleNewCardBadge(true);
            Tr(v.NewCardBadgeBuilt,
               "B0-a **前提**：角标那一层建出来了（图 `Resources/Art/ui_menu/WF_Special_offer_Value.png` 取得到）");
            if (!v.NewCardBadgeBuilt) return;

            int vc = CardView.NewBadgeMeshVertexCountForTest();
            C(vc, 8,
              "B1 角标网格 = **2 块 × 4 顶点 = 8**（原版 Sliced：左右各 162px 不拉伸、中段 0 ⇒ `Image.cs:1199` 跳过）"
            + "｜🧨 换回 `SpriteQuad`（整图拉伸）⇒ 4 顶点 ⇒ 本行红");

            float s = CardView.Width / CardView.CardUnitW;
            Vector2 want = new Vector2(CardView.NewBadgeW * s, CardView.NewBadgeH * s);
            Vector2 got = CardView.NewBadgeMeshSizeForTest();
            Near(got.x, want.x, 1e-3f,
                 "B2 角标网格外接宽 = `NewBadgeW × Width/CardUnitW`（缩边后两块拼起来仍覆盖整块矩形）"
               + "｜🧨 边宽算式方向写反 ⇒ 本行红");
            Near(got.y, want.y, 1e-3f, "B2b 角标网格外接高 = `NewBadgeH × Width/CardUnitW`");
        }
        finally
        {
            if (v != null) Object.DestroyImmediate(v.gameObject);
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>单位卡夹具：阵营 + 兵种都填满（否则阵营行 / 兵种行不建，A 段等于没量）。</summary>
    static CardData UnitData()
    {
        var d = CardData.Simple("B106W9 Unit", 3, 3, 4);
        d.type = "unit";          // ⚠️ `Simple` 的 `faction` 恒 null（本仓已知坑）⇒ 下面显式填
        d.faction = "Ultramarines";
        d.subtype = "Infantry";
        return d;
    }

    /// <summary>战术卡夹具（兵种行只在 `spellType` 判据成立时印，本段只量阵营行字号，不看兵种行）。</summary>
    static CardData TacticData()
    {
        var d = CardData.Simple("B106W9 Tactic", 2, 0, 0);
        d.isUnit = false;
        d.type = "tactic";
        d.faction = "Ultramarines";
        d.subtype = "Infantry";
        return d;
    }
}
