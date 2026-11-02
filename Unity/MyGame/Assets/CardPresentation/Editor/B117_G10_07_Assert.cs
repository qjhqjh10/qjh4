// B117_G10_07_Assert.cs — 执行代理 `B117-G10-07`（2026-11-02）· 战斗域第 7 分片「场面 · 槽位 · 单位徽标」
//
// ============================ 它盯的是什么 ============================
//  四组，对应分片单里的四个重点：
//    A · **槽位几何**  `Board/ArenaSlots.cs` —— 每侧 4 格的世界坐标/间距/朝向/缩放，逐值对原版 prefab
//    B · **单位徽标**  `Core/Badges.cs`       —— 7 个位的位置/尺寸、哪 47 个关键词上徽标、敌伏击整排不画
//    C · **单位高亮**  `Core/CardHighlight.cs` —— 原版 6 档状态（色 / 补间时长 / 缩不缩放 / 整卡染不染）
//    D · **关键词特效锚** `Core/TraitParticles.cs` —— 粒子挂在卡的 `effectsAnchor`，⛔ 不是卡根
//
// ============================ 判据（全部实读，出处逐条写在断言里）============================
//  · 槽位：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4372.json`（玩家）
//    / `_4373.json`（敌方）—— `MinionSeparation` 0.82 / 1.53 · `minionExtraDistanceFromHero` 0.09 / 0.13 ·
//    `desiredScale` 0.36 / 0.69 · `heroScale` 0.40 / 0.77 · `heroExtraOffset.z` −0.42 / −0.75 ·
//    `slotsPerSide` 4 · `ExtraYOnBoard` 0 · `MinionArea` 的 local z = −6.655 / +1.043
//    （`Transform_1309.json` / `Transform_1314.json`，两者 scale 都是 1、父链全 identity）。
//    落点式子 = `MinionManager__FillMinionPositions.c`：左 `−(k·Sep + extra)` / 右 `+(k·Sep + extra)`、y 显式写 0；
//    督军位走 `BattleManager__GetHeroBoardPos.c`（= `heroExtraOffset` + 静态 `Vector3.zero`）⇒ **不加 extra**。
//    🔴 **原版【不镜像】敌方槽位**（`MinionManager__Awake.c` 对两个 `MinionArea` 跑同一套式子；
//      序列化里敌方 `leftSlotPosNormal` 是 +x 且只有 3 项 = `Awake` 会重建的**死值**）→ `报告/B8_M_敌方槽位镜像.md`。
//  · 徽标：`bundle_battleprefabs_vfxandmisc_assets_all/Transform/` 三跳实读 ——
//      `TraitIcons` local (0.296, 1.533, −0.014) ｜ `TraitIconContainer` 左列 x −0.859、右列 x +0.267、
//      scale 0.7502、y = 0.826 / 0.401 / −0.039 / −0.479 ｜ `Container`(∓0.287) 与 `Without counter`(±0.287) **互相抵消**
//      ⇒ 图标中心 = 容器中心 = **∓0.563**，y（3DBody 空间）= 2.359 / 1.934 / 1.494 / 1.054，减半个卡高（3.3313/2）折到卡心系。
//    图标边长 = `m_Size 0.8 × TraitIcon 0.7593 × 容器 0.7502` = 0.4557；底板 = `0.85×0.866×0.7502` / `0.874×1.069×0.7502`。
//    筛选 = `CardTraitCollection__GetTraitInPlayListForIcons.c`（`hasIcon && isInPlayIcon && traitIcon != null`，47 条）
//      ＋ `BattleCardUI__UpdateTraitIcons.c:40-46` 那道「**对面 + ambush ⇒ 整排一枚都不画**」。
//    卡体可见半宽 = `Card 3D` scale 0.88586 × 网格半宽 1.0482 = **0.9286**（图标不许超出）。
//  · 高亮：卡 prefab 上 `CardHighlight` 组件实读 5 色 + `CardHighlightAnimTime 0.1` / `ScaleFactor 1.05`；
//      `CardHighlight__ChangeState.c` 逐 case（只有 0/3/4 缩放、2 与 5 的补间时长 = 0）·
//      `CardHighlight__ChangeFrameColor.c`（**只 set 那两颗 SpriteRenderer 的 color，不碰卡的材质**）·
//      `dump.cs:34132-34141` 的 `HighlightStates` 6 值。
//  · 特效锚：`CardScript__ActivateTraitParticlesFromCode.c:75` → `BattleManager._ResolveTraitAnim_d__465__MoveNext.c:112-118`
//      的 `startParent = actingCard.CardUI.effectsAnchor` = `CardPrefab/Board Elements/EffectAnchor`
//      （`assetripper_out/…/CardPrefab.prefab` 的 Transform `4161342230376464`：local **(0, 1.34, −0.128)**）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 A 组：**敌方不镜像**——槽 0 在两侧都必须是 **负** x、槽 8 都必须是 **正** x。
//     谁把敌方镜像回来（历史上的 `-x` 翻号），A6a/A6b 会一起红；只改一个方向不可能同时变绿。
//  🧨 B 组：**「图标不外偏」只能用老实现反证**——同时断言 `IconOutward == 0` ＋ 「老那一档（+0.21525）会超出卡体 0.9286」。
//     把 `IconOutward` 改回 0.21525 ⇒ B4 红；把卡体半宽放宽到能容下老值 ⇒ 那条反证里的字面量是**原版**的，改不了 ⇒ 也红。
//  🧨 B 组：**敌伏击三态**——`{shield}` 敌 1 枚 · `{shield, ambush}` 敌 0 枚 · `{shield, ambush}` 我 1 枚。
//     删掉那道闸 ⇒ 中间那条红；让闸也对自家生效 ⇒ 第三条红。**两条不可能同时满足**。
//  🧨 C 组：**整卡着色 vs 状态环是两条路**——`ColorOf(ValidTarget)` 必须是**白**（不染整卡）而
//     `FrameColorOf(ValidTarget)` 必须是**绿**。把整卡着色改回状态色 ⇒ 前一条红；把环也改成白 ⇒ 后一条红。
//  🧨 D 组：**锚点 ≠ 卡根**——同时断言「`SpawnAnchor(卡根)` 返回的节点**不是**卡根」＋「它的局部位是 (0, −0.32565, −0.128)」。
//     把 `SpawnAnchor` 退回 `return at;` ⇒ 两条一起红（节点就是卡根、局部位是 0）。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：零前提（自带临时根 + 收尾 `DestroyImmediate`），不碰场景/存档。
//  挂法（由调度台决定）：`Editor/BattleScene.cs` 的聚合行**同一物理行**追加
//      B117_G10_07_Assert.Run(); _sink.Pass += B117_G10_07_Assert.Pass; _sink.Fail += B117_G10_07_Assert.Fail;
//  判绿红：`grep "^B117G1007"` 的**合计行**；逐条按**行首** `B117G1007 ✓` / `B117G1007 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B117_G10_07`：场面 · 槽位 · 单位徽标 · 单位高亮 · 关键词特效锚 四组静态判据。</summary>
public static class B117_G10_07_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1007 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1007 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol + "；实得 " + got.ToString("F5") + "）");
    }

    /// <summary>卡体可见半宽（原版 `Card 3D` scale 0.88586 × 网格半宽 1.0482）—— 徽标不许超过它。</summary>
    const float VisibleCardHalfW = 0.9286f;

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var root = new GameObject("B117_G10_07_Root");
        try
        {
            Group("A·槽位几何", () => A_SlotGeometry());
            Group("B·单位徽标", () => B_Badges());
            Group("C·单位高亮", () => C_CardHighlight());
            Group("D·关键词特效锚", () => D_TraitParticlesAnchor(root.transform));
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
        Debug.Log("B117G1007 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    // ================================================================= A · 槽位几何
    static void A_SlotGeometry()
    {
        // ---- 前提（硬检查，⛔ 不许 `if (x != null) Check(...)` 那种静默跳过）----
        Check(ArenaSlots.SlotCount == 9,
              "A0a 总格数 = **9**（督军 1 + 两侧各 4；`MinionManager__Awake.c` 按 `slotsPerSide` 重建）"
            + "（实得 " + ArenaSlots.SlotCount + "）");
        Check(ArenaSlots.SlotsPerSide == 4,
              "A0b 每侧 = **4**（`MonoBehaviour_4372/4373.json` 的 `slotsPerSide: 4`）（实得 " + ArenaSlots.SlotsPerSide + "）");
        Check(ArenaSlots.WarlordSlot == 4, "A0c 督军位 = 中央格 4（实得 " + ArenaSlots.WarlordSlot + "）");

        // ---- 玩家侧：逐值 = `MonoBehaviour_4372.json` ----
        CheckClose(ArenaSlots.PlayerPitch, 0.8199999928474426f, 1e-5f, "A1a `MinionSeparation`（玩家）= 0.82");
        CheckClose(ArenaSlots.PlayerExtra, 0.09000000357627869f, 1e-5f, "A1b `minionExtraDistanceFromHero`（玩家）= 0.09");
        CheckClose(ArenaSlots.PlayerZ, -6.65500020980835f, 1e-5f, "A1c 玩家 `MinionArea` local z = −6.655（`Transform_1309.json`）");
        CheckClose(ArenaSlots.PlayerCardScale, 0.36000001430511475f, 1e-5f, "A1d `desiredScale`（玩家）= 0.36");
        CheckClose(ArenaSlots.PlayerHeroScale, 0.4000000059604645f, 1e-5f, "A1e `heroScale`（玩家）= 0.40");
        CheckClose(ArenaSlots.PlayerHeroExtraZ, -0.41999998688697815f, 1e-5f, "A1f `heroExtraOffset.z`（玩家）= −0.42");

        // ---- 敌方侧：逐值 = `MonoBehaviour_4373.json` ----
        CheckClose(ArenaSlots.EnemyPitch, 1.5299999713897705f, 1e-5f, "A2a `MinionSeparation`（敌方）= 1.53");
        CheckClose(ArenaSlots.EnemyExtra, 0.12999999523162842f, 1e-6f, "A2b `minionExtraDistanceFromHero`（敌方）= 0.13");
        CheckClose(ArenaSlots.EnemyZ, 1.0429999828338623f, 1e-5f, "A2c 敌方 `MinionArea` local z = +1.043（`Transform_1314.json`）");
        CheckClose(ArenaSlots.EnemyCardScale, 0.6899999976158142f, 1e-5f, "A2d `desiredScale`（敌方）= 0.69");
        CheckClose(ArenaSlots.EnemyHeroScale, 0.7699999809265137f, 1e-5f, "A2e `heroScale`（敌方）= 0.77");
        CheckClose(ArenaSlots.EnemyHeroExtraZ, -0.75f, 1e-6f, "A2f `heroExtraOffset.z`（敌方）= −0.75");

        // ---- 督军位：`GetHeroBoardPos` = 静态 `Vector3.zero` + `heroExtraOffset`，**不加 extra** ----
        var heroMe = ArenaSlots.Position(ArenaSlots.WarlordSlot, false);
        var heroFoe = ArenaSlots.Position(ArenaSlots.WarlordSlot, true);
        CheckClose(heroMe.x, 0f, 1e-6f, "A3a 督军位（我）x 居中 = 0");
        CheckClose(heroMe.y, 0f, 1e-6f, "A3b 督军位（我）y = 0（`GetHeroBoardPos` 只加 `heroExtraOffset`，不吃 `ExtraYOnBoard`）");
        CheckClose(heroMe.z, -7.075f, 1e-4f, "A3c 督军位（我）z = −6.655 + (−0.42) = **−7.075**");
        CheckClose(heroFoe.z, 0.293f, 1e-4f, "A3d 督军位（敌）z = +1.043 + (−0.75) = **+0.293**");

        // ---- 侧格：左 = −(k·Sep + extra)、右 = +(k·Sep + extra)（`FillMinionPositions.c`）----
        CheckClose(ArenaSlots.Position(5, false).x, 0.91f, 1e-5f, "A4a 我·槽 5（右 1）= +(0.82 + 0.09) = 0.91");
        CheckClose(ArenaSlots.Position(3, false).x, -0.91f, 1e-5f, "A4b 我·槽 3（左 1）= −(0.82 + 0.09) = −0.91");
        CheckClose(ArenaSlots.Position(8, false).x, 3.37f, 1e-5f, "A4c 我·槽 8（右 4）= +(4×0.82 + 0.09) = 3.37");
        CheckClose(ArenaSlots.Position(0, false).x, -3.37f, 1e-5f, "A4d 我·槽 0（左 4）= −(4×0.82 + 0.09) = −3.37");
        CheckClose(ArenaSlots.Position(5, true).x, 1.66f, 1e-5f, "A4e 敌·槽 5（右 1）= +(1.53 + 0.13) = 1.66");
        CheckClose(ArenaSlots.Position(0, true).x, -6.25f, 1e-5f, "A4f 敌·槽 0（左 4）= −(4×1.53 + 0.13) = −6.25");

        bool allYZero = true;
        for (int s = 0; s < ArenaSlots.SlotCount; s++)
            for (int e = 0; e < 2; e++)
                if (Mathf.Abs(ArenaSlots.Position(s, e == 1).y) > 1e-6f) allYZero = false;
        Check(allYZero, "A5 18 个格位的 y 全是 0（`ExtraYOnBoard` 13 场 × 2 实例实读恒 0；`FillMinionPositions` 也显式写 0）");

        // ---- 🧨 灭自证：原版【不镜像】敌方槽位 ----
        //  历史上的错法 = 敌方那一行把 x 取反。那一改会同时打红这两条（而且是**互反相**的）。
        Check(ArenaSlots.Position(0, true).x < 0f && ArenaSlots.Position(0, false).x < 0f,
              "A6a 🧨 **两侧的槽 0 都在左边**（x < 0）—— 原版【不镜像】敌方（`MinionManager__Awake` 对两个 MinionArea 跑同一套式子）"
            + "（实得 我 " + ArenaSlots.Position(0, false).x.ToString("F3")
            + " / 敌 " + ArenaSlots.Position(0, true).x.ToString("F3") + "）");
        Check(ArenaSlots.Position(8, true).x > 0f && ArenaSlots.Position(8, false).x > 0f,
              "A6b 🧨 **两侧的槽 8 都在右边**（x > 0）—— 与 A6a 互反相，⛔ 不可能靠翻号同时变绿"
            + "（实得 我 " + ArenaSlots.Position(8, false).x.ToString("F3")
            + " / 敌 " + ArenaSlots.Position(8, true).x.ToString("F3") + "）");

        // ---- 非部队/非督军的在场卡缩放（`GetUnitSizeInPlay` 的第三支）----
        CheckClose(ArenaSlots.InPlayScale(false), 0.75f, 1e-6f, "A7a `InPlayScale(我)` = `VarsGlobal.minionInPlayScale` = 0.75");
        CheckClose(ArenaSlots.InPlayScale(true), 0.645f, 1e-6f, "A7b `InPlayScale(敌)` = 0.75 × `enemyUnitsScaleRatio 0.86` = 0.645");

        // ---- 卡根 = 地面点 + 半个卡高 × 缩放（我们的卡心约定 vs 原版的卡底约定）----
        var rp = ArenaSlots.RootPosition(4, false, ArenaSlots.HeroScale(false));
        CheckClose(rp.y, 3.3313f * 0.5f * 0.40f, 1e-5f,
                   "A8 `RootPosition` 抬了半个卡高 × 缩放（卡体 3.3313/2 × heroScale 0.40 = 0.66626）");
    }

    // ================================================================= B · 单位徽标
    static void B_Badges()
    {
        const float halfH = 3.3313f * 0.5f;      // 卡心 → 卡底

        // ---- 前提 ----
        Check(Badges.MaxSlots == 7,
              "B0 徽标位 = **7**（左 3 + 右 4；`BattleCardUI.boardTraitIcons[7]`，第 8 个关键词不显示）"
            + "（实得 " + Badges.MaxSlots + "）");

        // ---- 位置：`TraitIcons(0.296, 1.533)` + 容器（左 −0.859 / 右 +0.267）= ∓0.563；y 见下表 ----
        CheckClose(Badges.SlotAt(0).x, -0.563f, 2e-3f, "B1a 左列 x = 0.296 + (−0.859) = **−0.563**");
        CheckClose(Badges.SlotAt(3).x, 0.563f, 2e-3f, "B1b 右列 x = 0.296 + (+0.267) = **+0.563**");
        CheckClose(Badges.SlotAt(0).y, 2.359f - halfH, 2e-3f, "B1c 左 1 y（3DBody 2.359 → 卡心系）");
        CheckClose(Badges.SlotAt(1).y, 1.934f - halfH, 2e-3f, "B1d 左 2 y（3DBody 1.934）");
        CheckClose(Badges.SlotAt(2).y, 1.494f - halfH, 2e-3f, "B1e 左 3 y（3DBody 1.494）");
        CheckClose(Badges.SlotAt(3).y, 2.359f - halfH, 2e-3f, "B1f 右 1 y（与左 1 同高）");
        CheckClose(Badges.SlotAt(6).y, 1.054f - halfH, 2e-3f, "B1g 右 4 y（3DBody 1.054；**右列比左列多一格**）");
        Check(Badges.SlotAt(2).x < 0f && Badges.SlotAt(3).x > 0f,
              "B1h 下标 0..2 = 左列、3..6 = 右列（否则 B1a/B1b 会撞在同一个 x 上）");

        // ---- 尺寸 ----
        CheckClose(Badges.IconSize, 0.8f * 0.7593f * 0.7502f, 5e-4f,
                   "B2 图标边长 = `m_Size 0.8 × TraitIcon 0.7593 × 容器 0.7502` = 0.4557");
        CheckClose(Badges.PlateW, 0.85f * 0.866f * 0.7502f, 1e-3f,
                   "B3a 底板宽 = 0.85 × 0.866 × 0.7502 = 0.5522");
        CheckClose(Badges.PlateH, 0.874f * 1.069f * 0.7502f, 1e-3f,
                   "B3b 底板高 = 0.874 × 1.069 × 0.7502 = 0.7009");

        // ---- 🧨 灭自证：「图标不往卡外偏」只能靠**老实现会超出卡体**来证 ----
        CheckClose(Badges.IconOutward, 0f, 1e-6f,
                   "B4a 🧨 `IconOutward` = **0**（原版 `Container(∓0.287)` 与 `Without counter(±0.287)` 抵消 ⇒ 图标中心 = 容器中心）");
        float iconCenter = Mathf.Abs(Badges.SlotAt(0).x);
        float oldHalfW = iconCenter + 0.21525f + Badges.IconSize * 0.5f;
        float newHalfW = iconCenter + Badges.IconSize * 0.5f;
        Check(newHalfW <= VisibleCardHalfW && oldHalfW > VisibleCardHalfW,
              "B4b 🧨 **新值在卡体内、老值（+0.21525）超出卡体** —— 卡体可见半宽 0.9286；"
            + "新外缘 " + newHalfW.ToString("F4") + " ✓ / 老外缘 " + oldHalfW.ToString("F4") + " ✗（退回老值 ⇒ 本条必红）");

        // ---- 筛选：那 47 条（`hasIcon && isInPlayIcon && traitIcon != null`）----
        string shieldSpr = Badges.SpriteOf("Shield");
        Check(!string.IsNullOrEmpty(shieldSpr),
              "B5a （前提）`Shield` 查得到图名（`Resources/Art/traits`）—— 实得 `" + (shieldSpr ?? "<null>") + "`");
        if (string.IsNullOrEmpty(shieldSpr)) return;

        Check(Badges.PlaysInPlayIcon("shield"), "B5b `shield` 在场上徽标那 47 条里");
        Check(!Badges.PlaysInPlayIcon("armour"),
              "B5c `armour` **不在** —— 它 `hasIcon=true` 但 `isInPlayIcon=false`（`trait_collection.json`），"
            + "⛔ 别拿卡面/收藏那一族（59 条）当场上徽标用");
        Check(!Badges.PlaysInPlayIcon("ambush"),
              "B5d `ambush` **不在** —— `hasIcon=false`，它自己永远进不了这 7 格");

        // ---- 🧨 灭自证：敌伏击整排不画（`BattleCardUI__UpdateTraitIcons.c:40-46`）----
        var shieldOnly = new Dictionary<string, int> { { "Shield", 1 } };
        var shieldAmbush = new Dictionary<string, int> { { "Shield", 1 }, { "Ambush", 1 } };
        int foeShield = Badges.For(shieldOnly, null, null, null, false).Count;
        int foeAmbush = Badges.For(shieldAmbush, null, null, null, false).Count;
        int myAmbush = Badges.For(shieldAmbush, null, null, null, true).Count;
        Check(foeShield == 1,
              "B6a 敌方「只有盾」⇒ 画 1 枚（实得 " + foeShield + "）");
        Check(foeAmbush == 0,
              "B6b 🧨 敌方「盾 + 伏击」⇒ **整排 0 枚**（原版那句 `HasCurrentTrait(ambush) && !isPlayer ⇒ return`）"
            + "（实得 " + foeAmbush + "）—— 删掉那道闸 ⇒ 本条红");
        Check(myAmbush == 1,
              "B6c 🧨 我方「盾 + 伏击」⇒ 仍画 1 枚（那道闸只认 `!isPlayer`）"
            + "（实得 " + myAmbush + "）—— 把闸改成不分敌我 ⇒ 本条红");

        // ---- 未激活那条通道：灰化 + ✕ **叠加**，不是二选一 ----
        CheckClose(Badges.NotActiveScale, 0.49f / 0.7593f, 1e-4f,
                   "B7a ✕ 相对图标的比例 = 0.49 / 0.7593 = 0.64533（原版 `Trait Not Active` 的 localScale ÷ `TraitIcon` 的）");
        CheckClose(Badges.NotActiveCrossSize, Badges.IconSize * (0.49f / 0.7593f), 1e-5f,
                   "B7b ✕ 边长 = 图标边长 × 0.64533（两者共用同一个中心）");
    }

    // ================================================================= C · 单位高亮
    static void C_CardHighlight()
    {
        // ---- 原版 5 色（卡 prefab 上 `CardHighlight` 组件实读）----
        CheckClose(CardHighlight.AnimTime, 0.1f, 1e-6f, "C0a `CardHighlightAnimTime` = 0.1 s");
        CheckClose(CardHighlight.ScaleFactor, 1.05f, 1e-6f, "C0b `ScaleFactor` = 1.05");
        CheckClose(CardHighlight.OutlineAnimTime, 0.2f, 1e-6f, "C0c `HIGHLIGHT_COLOR_CHANGE_TIME` = 0.2 s（`ChangeHighlightColor` 的唯一字面量）");

        CheckClose(CardHighlight.ValidTargetColor.r, 0f, 1e-4f, "C1a `ValidTargetColor.r` = 0");
        CheckClose(CardHighlight.ValidTargetColor.g, 1f, 1e-4f, "C1b `ValidTargetColor.g` = 1");
        CheckClose(CardHighlight.ValidTargetColor.b, 0.1294118f, 1e-4f, "C1c `ValidTargetColor.b` = 0.1294118 ⇒ 绿 **#00FF21**");
        CheckClose(CardHighlight.SelectedTargetColor.r, 1f, 1e-4f, "C1d `SelectedTargetColor.r` = 1");
        CheckClose(CardHighlight.SelectedTargetColor.g, 0.5176f, 1e-4f, "C1e `SelectedTargetColor.g` = 0.5176");
        CheckClose(CardHighlight.SelectedTargetColor.b, 0f, 1e-4f, "C1f `SelectedTargetColor.b` = 0 ⇒ 橙 **#FF8400**");
        CheckClose(CardHighlight.PlayableColor.r, 1f, 1e-4f, "C1g `PlayableColor` = (1,1,0) 黄 —— 它是 **`displayingActiveAbility`** 那一档，"
                                                             + "⛔ 不是「打得出去」");
        CheckClose(CardHighlight.PlayableColor.g, 1f, 1e-4f, "C1h `PlayableColor.g` = 1");
        CheckClose(CardHighlight.PlayableColor.b, 0f, 1e-4f, "C1i `PlayableColor.b` = 0");
        Check(CardHighlight.SelectedColor == Color.white, "C1j `SelectedColor` = 白");

        // ---- 🧨 整卡着色 vs 状态环 = 两条路（2026-10-24 改判的落点）----
        Check(CardHighlight.ColorOf(CardHighlightState.ValidTarget) == Color.white,
              "C2a 🧨 `ColorOf(ValidTarget)` = **白** —— 原版 `ChangeFrameColor` 只 set 那两颗 SpriteRenderer，"
            + "⛔ 不碰卡的材质；整卡染绿会和数值的涨落色相乘（用户报的「加攻变红」）");
        Check(CardHighlight.ColorOf(CardHighlightState.SelectedTargetInBoard) == Color.white,
              "C2b 🧨 `ColorOf(SelectedTargetInBoard)` = **白**（同上；改回橙 ⇒ 白字 × #FF8400 = (255,132,0)）");
        Check(CardHighlight.FrameColorOf(CardHighlightState.ValidTarget) == CardHighlight.ValidTargetColor,
              "C2c 🧨 `FrameColorOf(ValidTarget)` = **绿**（状态环那一路才是原版那一层）—— 与 C2a 互反相，不可能同时变绿");
        Check(CardHighlight.FrameColorOf(CardHighlightState.SelectedTargetInBoard) == CardHighlight.SelectedTargetColor,
              "C2d 🧨 `FrameColorOf(SelectedTargetInBoard)` = **橙 #FF8400**");
        Check(CardHighlight.FrameColorOf(CardHighlightState.Normal).a < 1e-4f,
              "C2e `FrameColorOf(Normal)` 的 α = 0（原版 `RegularColor` 的 α 就是 0 ⇒ 环整个关掉）");

        // ---- 缩放：只有原版 case 0/3/4 缩放 ----
        CheckClose(CardHighlight.ScaleOf(CardHighlightState.ValidTarget, onBoard: true), 1.05f, 1e-6f,
                   "C3a `potentialTargetInBoard(3)` **×1.05**");
        CheckClose(CardHighlight.ScaleOf(CardHighlightState.ValidTarget, onBoard: false), 1f, 1e-6f,
                   "C3b `potentialTargetInHand(1)` **不缩放**");
        CheckClose(CardHighlight.ScaleOf(CardHighlightState.Selected), 1f, 1e-6f,
                   "C3c `selected(2)` **不缩放**（`ChangeState.c` 里 case 2 没有 `ChangeTargetScale`）");
        CheckClose(CardHighlight.ScaleOf(CardHighlightState.SelectedTargetInBoard), 1.05f, 1e-6f,
                   "C3d `selectedTargetInBoard(4)` ×1.05");

        // ---- 补间时长：case 2 / 5 是瞬切 ----
        CheckClose(CardHighlight.TweenTimeOf(CardHighlightState.Selected), 0f, 1e-6f,
                   "C4a `selected` 的补间时长 = **0**（瞬切）");
        CheckClose(CardHighlight.TweenTimeOf(CardHighlightState.ValidTarget), 0.1f, 1e-6f,
                   "C4b 其余档 = `CardHighlightAnimTime` 0.1");

        // ---- 6 档枚举：名字与数值逐条照 `dump.cs:34132-34141` ----
        Check((int)CardHighlight.CardBoardHighlightState.regular == 0
           && (int)CardHighlight.CardBoardHighlightState.potentialTargetInHand == 1
           && (int)CardHighlight.CardBoardHighlightState.selected == 2
           && (int)CardHighlight.CardBoardHighlightState.potentialTargetInBoard == 3
           && (int)CardHighlight.CardBoardHighlightState.selectedTargetInBoard == 4
           && (int)CardHighlight.CardBoardHighlightState.displayingActiveAbility == 5,
              "C5 🧨 `HighlightStates` 6 值逐条 = 0..5（原版 `dump.cs` 的声明顺序）—— 有人调换顺序 ⇒ 红");

        // ---- 两枚举的映射（只有这一处）----
        Check(CardHighlight.CardBoardHighlight.MapToView(CardHighlight.CardBoardHighlightState.potentialTargetInHand)
              == CardHighlightState.ValidTarget
           && CardHighlight.CardBoardHighlight.MapToView(CardHighlight.CardBoardHighlightState.potentialTargetInBoard)
              == CardHighlightState.ValidTarget,
              "C6a 原版 1 / 3 两档都落我们的 `ValidTarget`（同一个绿色，只差缩不缩放）");
        Check(CardHighlight.CardBoardHighlight.MapToView(CardHighlight.CardBoardHighlightState.selected)
              == CardHighlightState.Selected
           && CardHighlight.CardBoardHighlight.MapToView(CardHighlight.CardBoardHighlightState.selectedTargetInBoard)
              == CardHighlightState.SelectedTargetInBoard
           && CardHighlight.CardBoardHighlight.MapToView(CardHighlight.CardBoardHighlightState.regular)
              == CardHighlightState.Normal,
              "C6b 2 / 4 / 0 三档的映射");

        // ---- 「打得出去」走 SDF `_Outline`，⛔ 不走状态环 ----
        Check(CardHighlight.FrameColorOf(CardHighlightState.Playable).a < 1e-4f,
              "C7 「打得出去」在**状态环**这一路不亮（原版它走 SDF `_Outline`，见 `OutlineOf`）—— "
            + "⛔ 别把 `Playable` 画成黄环（那个黄属于 `displayingActiveAbility`）");
        Check(CardHighlight.OutlineOf(null, canBePlayed: false).a < 1e-4f,
              "C8 `OutlineOf(…, canBePlayed: false)` 的 α = 0（打不出去 ⇒ 根本不描边）");
    }

    // ================================================================= D · 关键词特效锚
    static void D_TraitParticlesAnchor(Transform root)
    {
        // ---- 前提：绑定表载进来了（否则整条链一件都不播，后面的断言没意义）----
        Check(TraitParticles.Loaded,
              "D0 （前提）`trait_effect_bindings.json` 的 FromCode 表已载入（实得 " + TraitParticles.BindingCount + " 条）");
        Check(TraitParticles.PrefabFor("huntMark") == "HuntMark_DamageEffect",
              "D1 `huntMark` 的 FromCode 件 = `HuntMark_DamageEffect`（原版 `HuntMark.asset` 的 `triggerType==5` 那一条）"
            + "（实得 `" + (TraitParticles.PrefabFor("huntMark") ?? "<null>") + "`）");

        // ---- 锚点本身（原版 `CardPrefab/Board Elements/EffectAnchor` = (0, 1.34, −0.128)，3DBody 空间）----
        var a = CardView.BoardEffectAnchorLocal;
        CheckClose(a.x, 0f, 1e-5f, "D2a 锚点 x = 0");
        CheckClose(a.y, 1.34f - 3.3313f * 0.5f, 1e-4f, "D2b 锚点 y = 1.34 − 卡高/2 = −0.32565（卡底→卡心）");
        CheckClose(a.z, -0.128f, 1e-4f, "D2c 锚点 z = −0.128（prefab 实读 −0.12799978）");

        // ---- 真建一张卡，验 `SpawnAnchor` 真把挂点换成了锚 ----
        var go = new GameObject("B117_G10_07_Probe");
        go.transform.SetParent(root, false);
        var cv = go.AddComponent<CardView>();       // CardView 没有 Awake/Start，Editor 里可以直接挂
        Check(cv != null, "D3a （前提）探针卡上挂到了 `CardView`");
        if (cv == null) return;

        Transform anchor = TraitParticles.SpawnAnchor(go.transform);
        Check(anchor != null && anchor != go.transform,
              "D3b 🧨 `SpawnAnchor(卡根)` 交回的是**子节点**、⛔ 不是卡根本身"
            + "（原版 `startParent = actingCard.CardUI.effectsAnchor`）—— 退回 `return at;` ⇒ 本条红");
        if (anchor != null && anchor != go.transform)
        {
            CheckClose(anchor.localPosition.x, 0f, 1e-4f, "D3c 锚点局部位 x = 0");
            CheckClose(anchor.localPosition.y, -0.32565f, 1e-4f,
                       "D3d 🧨 锚点局部位 y = **−0.32565**（⛔ 不是 0）—— 与 D3b 一起把「挂在卡根」挡死");
            CheckClose(anchor.localPosition.z, -0.128f, 1e-4f, "D3e 锚点局部位 z = −0.128");
        }
        Check(ReferenceEquals(anchor, cv.EffectAnchor(true)),
              "D3f 挂点走的就是 `CardView.EffectAnchor(onBoard: true)`（判据只此一处，⛔ 别在调用点另判）");

        // ---- 反相：已经是锚点 / 不是卡 ⇒ **原样返回**（`PlayInTarget` 的调用点传的就是督军的锚）----
        var plain = new GameObject("B117_G10_07_NotACard");
        plain.transform.SetParent(root, false);
        Check(ReferenceEquals(TraitParticles.SpawnAnchor(plain.transform), plain.transform),
              "D4a 传**非卡**的 transform ⇒ 原样返回（⛔ 别用 `GetComponentInParent` 把别人的锚套上去）");
        if (anchor != null)
            Check(ReferenceEquals(TraitParticles.SpawnAnchor(anchor), anchor),
                  "D4b 传**已经是锚点**的 transform（= `PlayInTarget` 的 `targetAt`）⇒ 原样返回 —— "
                + "`B102TRAIT_Assert` 的 A1 靠这条保持一致");
        Check(TraitParticles.SpawnAnchor(null) == null, "D4c `SpawnAnchor(null)` = null（不 NRE）");

        // ---- 收尾（⛔ 别留脏对象）----
        Object.DestroyImmediate(plain);
        Object.DestroyImmediate(go);
    }
}
