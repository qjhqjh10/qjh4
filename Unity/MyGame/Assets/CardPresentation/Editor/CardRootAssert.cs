// CardRootAssert.cs — 执行代理 `W15`（2026-10-24）：**卡根参考系那条链的接线**断言
//
// 覆盖三件（判据全文 → `资料/普查产出_1024c/C6_表现界面轮抽剩余判据.md` §1·3 / §2·1
//                  + `资料/普查产出_1024d/W11_根特效层与卡根_执行报告.md` §五 / §六·1）：
//   A. `CardView.SetRemnantBody` 的**挂载点** = 卡底边（原版 `BattleCardUI__CreateRemnantBody.c:54-75`
//      的 `Instantiate(prefab, this.transform.position, <静态 Quaternion>, this.transform)` ⇒ 局部位置 (0,0,0)，
//      而原版 `CardPrefab` 根 = **卡底边**）。
//   B. `BattleDriver.PlayAttackFeel` 的近战端点 = **带 `CardView` 的那条 `MeleeEndPos`**（`dy` 真的生效）。
//   C. 「宿主取不到」那两处退路（`FireAtAttackAnchor` 锚点为 null / `PlayDeployLandDust` 卡视图为 null）
//      = **竞技场世界点 + `ArenaLayer`**（改前是 HUD 投影点 + prefab 自带层 ⇒ 被 HUD 正交相机画）。
//
// 断言口径：
//   · 每一条都走**生产那条路**（B 读 `LastMeleeEndPosForTest` —— 生产那一行自己记的账；
//     C 调 `BattleDriver` 的两个 `...ForTest` 入口，它们调的就是生产方法本身）。
//   · 期望值从**判据**来（`ArenaSlots` / `CardFeel` 契约 / 原版 prefab 几何），⛔ 不拿我们自己的常量自证。
//   · 每组都配一条**灭自证**：把生产改回旧写法 ⇒ 必红（见每条消息末尾的 🧨）。
//
// 宿主 = `Editor/BattleScene.RunCore`（在 `DestroyImmediate(driver)` **之前**调一次）：
//     CardRootAssert.Run();
//     pass += CardRootAssert.Pass; fail += CardRootAssert.Fail;
// ⚠️ **必须在销毁 driver 之前** —— B / C 都要用活着的那台 driver 与它已登记的场上视图。
// ⚠️ 与 `Editor/RootVfxLayerAssert.cs`（W11）**分工**：那件钉 `CardFeel` 侧的契约
//    （爆散体/残骸粒子的层 + `EffectsAnchorHeight`/`MeleeEndPos` 本身）；
//    本件只钉**接线**（卡根挂点 / 生产那一行的第四参 / 两处退路的落点与层）。⛔ 别在两处重复写同一条。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class CardRootAssert
{
    const string P = "[W15] ";

    /// <summary>残骸体 prefab（效果库里的键；`RootVfxLayerAssert` 用的同一件 —— 它在批处理里取得到）。</summary>
    const string RemnantPrefab = "RemnantBody3D Necrons";

    /// <summary>攻击件退路用的效果名（真实存在、`BattleScene` 别处也在用；`cardAnim: null` ⇒ 不排位移）。</summary>
    const string AttackFxProbe = "BulletImpact_artillery_1shot";

    /// <summary>本类这一趟的通过 / 失败数（宿主并进 `BattleScene` 的总数）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;

        bool Check(bool ok, string msg)
        {
            if (ok) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
            Fail++; Debug.LogError(P + $"   ✗ {msg}"); return false;
        }

        Debug.Log(P + "=== 卡根参考系接线（W15）开始 ===");
        try
        {
            RunRemnantMount(Check);
            RunMeleeEndPoint(Check);
            RunHostFallbacks(Check);
        }
        catch (Exception e)
        {
            Check(false, "断言过程抛异常（下面这条必须看）：" + e.GetType().Name + "：" + e.Message);
            Debug.LogException(e);
        }
        Debug.Log(P + $"=== 结束：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ==================================================================
    //  A. 残骸体挂载点 = 卡底边
    // ==================================================================
    static void RunRemnantMount(Func<bool, string, bool> check)
    {
        Debug.Log(P + "--- A：`CardView.SetRemnantBody` 的挂载点 = 卡底边 ---");

        CardData data;
        var pool = CardDatabase.Load();
        var def = CardDatabase.FindById(pool, "SOR8");
        if (def == null && pool != null && pool.Count > 0) def = pool[0];
        check(def != null, "A（前提）卡池里取得到一张卡（`SOR8` / 兜底第一张）—— 取不到下面两条验不了");
        if (def == null) return;
        data = BattleDriver.ToCardData(def, def.Faction);

        // 期望的**局部**偏移（判据）：卡底边 = `From3DBody(0,0,0)` = `(0, −Height/2, 0)`。
        // ⛔ 不写成字面量 −1.66565：Height 是 `CardView` 自己那份卡单位（改数据时要跟着变）。
        float wantLocalY = -CardView.Height * 0.5f;

        var root = new GameObject("W15_remnant_fixtures");
        root.transform.position = Vector3.zero;
        try
        {
            // ---- ① 基准档（卡缩放 = 1）----
            var card = CardView.Create(root.transform, data, "W15_card", CardFace.Board);
            if (card == null) { check(false, "A（前提）`CardView.Create` 建得出卡视图"); return; }
            card.SetFace(CardFace.Board);                       // 建 `body3D`（残骸体那条路要取它的缩放）
            card.transform.position = new Vector3(1.2f, 2.5f, -0.4f);   // 故意不在原点：能验「按卡的位姿算」
            check(Mathf.Abs(card.transform.lossyScale.y - 1f) < 1e-3f,
                  $"A（前提）夹具的卡在世界里缩放 = 1（实得 {card.transform.lossyScale.y:F4}）");

            card.SetRemnantBody(true, RemnantPrefab);
            var rem = card.RemnantBodyRoot;
            if (!check(rem != null, $"A 夹具：残骸体 `{RemnantPrefab}` 真建出来了（效果库里有那一件；"
                                  + "建不出来 ⇒ 下面三条等于没验）"))
                return;

            Vector3 lp = rem.transform.localPosition;
            check(Mathf.Abs(lp.y - wantLocalY) < 1e-3f,
                  $"★ A 残骸体挂点 = **卡底边**（局部 y = −Height/2 = {wantLocalY:F4}；实得 {lp.y:F4}）"
                + "｜判据 = 原版 `BattleCardUI__CreateRemnantBody.c:54-75` 把 prefab 实例化在**卡 transform** 下、"
                + "世界位置 = 卡 transform 的位置 ⇒ 局部 (0,0,0)（原版卡根 = 卡底边，C6 §2·1）"
                + "｜🧨 改回 `Vector3.zero` ⇒ 本条红（残骸体高半张卡）");
            check(Mathf.Abs(lp.x) < 1e-3f && Mathf.Abs(lp.z) < 1e-3f,
                  $"A ……x / z 仍是 0（实得 ({lp.x:F4}, {lp.z:F4})）—— 原版那具 prefab 自带的 `x = 100`"
                + "（后台位）由 `WarpforgeEffectPlayer.Play` 归位，⛔ 别把这一条也改掉");

            Vector3 wantWorld = card.transform.TransformPoint(new Vector3(0f, wantLocalY, 0f));
            check(Vector3.Distance(rem.transform.position, wantWorld) < 1e-3f,
                  $"A 世界位置 = 卡根的 `(0, −Height/2, 0)`（偏差 "
                + $"{Vector3.Distance(rem.transform.position, wantWorld):E2} < 1e-3）");

            float dCenter = Vector3.Distance(rem.transform.position, card.transform.position);
            check(dCenter > CardView.Height * 0.45f,
                  $"A 🧨 灭自证：残骸体**不再挂在卡心**（离卡心 {dCenter:F3} > {CardView.Height * 0.45f:F3}）—— "
                + "把它挂回 `transform` 原点 ⇒ 这条立刻红（而上面那条世界位置会跟着一起错）");

            // ---- ② 缩放档（卡 0.36 —— 我方兵那一档）----
            // 为什么单列：局部 y 与缩放无关，但**世界**偏移要乘卡的 `lossyScale` ——
            //   只断①的话，「把世界偏移写死成 0.6」这种改法在缩放 1 的夹具上照样绿。
            var card2 = CardView.Create(root.transform, data, "W15_card_scaled", CardFace.Board);
            if (card2 != null)
            {
                card2.SetFace(CardFace.Board);
                card2.transform.localScale = Vector3.one * 0.36f;
                card2.transform.position = new Vector3(-1.5f, 0.5f, 2f);
                card2.SetRemnantBody(true, RemnantPrefab);
                var rem2 = card2.RemnantBodyRoot;
                if (check(rem2 != null, "A（缩放档）夹具：0.36 缩放的卡上也真建出了残骸体"))
                {
                    float wantWorld2 = CardView.Height * 0.5f * 0.36f;
                    float got = Vector3.Distance(rem2.transform.position, card2.transform.position);
                    check(Mathf.Abs(got - wantWorld2) < 1e-3f,
                          $"A 0.36 缩放档：世界偏移 = `Height/2 × 卡的 lossyScale` = {wantWorld2:F4}"
                        + $"（实得 {got:F4}）—— 挂点按**卡的局部**写，世界量才会自己跟着缩放"
                        + "｜🧨 改成直接写世界 `position` / 写死世界偏移 ⇒ 这条红");
                }
            }
        }
        finally
        {
            if (root != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }

    // ==================================================================
    //  B. 近战端点（生产那一行的第四参）
    // ==================================================================
    static void RunMeleeEndPoint(Func<bool, string, bool> check)
    {
        Debug.Log(P + "--- B：近战端点带 `dy`（生产那一行传了 `tv`）---");

        var driver = UnityEngine.Object.FindObjectOfType<BattleDriver>();
        if (!check(driver != null, "B（前提）找得到一台活着的 `BattleDriver`（没有它就验不了生产那一行）"))
            return;
        if (!check(driver.Ctx != null, "B（前提）这台 driver 上挂着一局 `Ctx`（`PlayAttackFeel` 进门就读它）"))
            return;

        // 夹具 = **自己摆两张探针单位**（贴督军那格 3 / 5）再刷一次 —— ⛔ 不指望「这一刻盘上刚好有人」
        //   （这一节跑在整轮末尾，盘面是这一局打到现在剩下的）。手法与 `BattleScene` 自己的 A849 / E7 两节相同：
        //   白盒改棋盘**不发事件** ⇒ 必须自己 `RefreshAll()`，**用完恢复**。
        BoardBackup bak = default(BoardBackup);
        CardView atk = null, tgt = null;
        const int atkSlot = 3, tgtSlot = 5;            // 两侧都「贴督军那格」= 合法（连续无洞）
        int me = driver.MySeat;
        try
        {
            if (!check(SaveAndPlaceProbeUnits(driver, ref bak, out atk, out tgt),
                       "B（前提）夹具：双方各摆一张探针单位、并真刷出了两张视图"
                     + " —— 摆不出来 ⇒ 下面三条没验（⛔ 不算跳过）"))
                return;
            if (!check(atk != null && tgt != null, "B（前提）两张探针视图都取得到"))
                return;

            var e = new BattleEvent
            {
                Kind = EvtKind.Attack,
                Player = me, Slot = atkSlot,
                TargetPlayer = 1 - me, TargetSlot = tgtSlot,
                CardId = atk.Data.id,
                Ranged = false,
            };

            Vector3 origin = atk.transform.position;
            Vector3 targetPos = tgt.transform.position;
            bool isPlayer = e.Player == me;

            driver.ClearMeleeEndProbeForTest();
            driver.PlayAttackFeelForTest(e);          // ⬅ **生产那条**近战表现入口（⛔ 不是替身）
            var rec = driver.LastMeleeEndPosForTest;
            if (!check(rec.HasValue,
                       "B 生产近战那一支真的走到了（`PlayAttackFeel` 把端点记下来了）"
                     + " —— 没记到 ⇒ 要么它提前 return（视图不在），要么那一行被改动了，⛔ 不算跳过"))
                return;

            Vector3 noDy = CardFeel.MeleeEndPos(origin, targetPos, isPlayer);        // 旧写法（三参 ⇒ dy = 0）
            Vector3 withDy = CardFeel.MeleeEndPos(origin, targetPos, isPlayer, tgt); // 判据那条（带受击方卡视图）

            float dWith = Vector3.Distance(rec.Value, withDy);
            float dNo = Vector3.Distance(rec.Value, noDy);
            float dy = CardFeel.EffectsAnchorHeight(tgt);
            float wantUp = dy * CardFeel.AttackPositionYOffset;
            check(dWith < 1e-3f,
                  $"★ B 生产端点 = **带 `CardView` 的那条 `MeleeEndPos`**（与判据点偏差 {dWith:E2} < 1e-3）"
                + "｜判据 = `CardScript._AttackMeleeAnim_d__357__MoveNext.c` 的 `fVar15 = anchor.y − targetCard.y`"
                + "，随后 `× attackPositionYOffset`（C6 §2·1）");
            check(wantUp > 1e-3f,
                  $"B（前提）受击方那张卡的 `dy` 量得出来且不为 0（dy = {dy:F4}）—— 它为 0 时下面两条分不出两档"
                + "（`EffectsAnchorHeight` 只在取不到锚点时回 0，并会出声）");
            check(dNo > wantUp * 0.9f,
                  $"★ B 🧨 灭自证：端点**不是**三参那条（两者相距 {dNo:F4} > {wantUp * 0.9f:F4}）"
                + " —— 改回 `MeleeEndPos(v, tv, isPlayer)`（三参 ⇒ `dy = 0`）⇒ 本条立刻红");

            float upDelta = rec.Value.y - noDy.y;
            check(Mathf.Abs(upDelta - wantUp) < 1e-3f,
                  $"★ B 端点抬高 = `up × AttackPositionYOffset({CardFeel.AttackPositionYOffset}) × dy`"
                + $" = {wantUp:F4}（dy = {dy:F4} · 实得抬高 {upDelta:F4}）"
                + "｜🧨 `dy` 恒 0（三参那条）⇒ 抬高 0 ⇒ 红");
            check(Mathf.Abs(rec.Value.x - noDy.x) < 1e-4f && Mathf.Abs(rec.Value.z - noDy.z) < 1e-4f,
                  "B `dy` 只抬 Y（x / z 逐位不变）—— 原版那一句乘的是 `Vector3.up`");
        }
        finally
        {
            // 这一段真起了一条补间（`CardFeel.MeleeAttack`）⇒ 推到底，让卡自己回正（别把半途姿态留给后面）。
            try { CardTween.Advance(1f); } catch (Exception) { /* 推不动不是本条的判据 */ }
            RestoreBoards(driver, bak);       // 把夹具棋盘还回这一局（后面 C 还要用同一台 driver）
        }
    }

    // ==================================================================
    //  C. 「宿主取不到」那两处退路 = 竞技场世界点 + ArenaLayer
    // ==================================================================
    static void RunHostFallbacks(Func<bool, string, bool> check)
    {
        Debug.Log(P + "--- C：两处「宿主取不到」的退路（竞技场世界点 + ArenaLayer）---");

        var driver = UnityEngine.Object.FindObjectOfType<BattleDriver>();
        if (!check(driver != null, "C（前提）找得到一台活着的 `BattleDriver`"))
            return;
        if (!check(driver.playerBoard != null && driver.enemyBoard != null,
                   "C（前提）两块 `BoardLayout` 都在（HUD 投影点与 `use3D` 开关都挂在它们身上）"))
            return;
        if (!check(driver.Ctx != null, "C（前提）driver 上挂着一局 `Ctx`（竞技场点要靠 `Player/Slot` 现算）"))
            return;
        // 3D 那一半的前提：真有一台战场透视相机（没有它，「HUD 投影点」与「竞技场点」是同一个平面，
        //   `dHud > 1` 那条灭自证就分不出两档）。本宿主别处（A457 前置总闸）也把「boardCam 在」当前提。
        if (!check(driver.boardCam != null, "C（前提）3D 战场相机（`driver.boardCam`）在"))
            return;

        int me = driver.MySeat;

        // 夹具格 = **我方贴督军那格（3）**，⛔ 不用督军位 4（那一档缩放是 `HeroScale`，会把「按格算缩放」这件事混进来）。
        // 探针单位由 `SaveAndPlaceProbeUnits` 现摆（不指望「这一刻盘上刚好有人」）；用完 `RestoreBoards` 还回去。
        const int slot = 3;
        const bool foe = false;                       // 夹具格永远在**我方**那半边
        float cardScale = ArenaSlots.CardScale(foe);
        CardView slotView = null;

        bool savedUse3D = driver.use3DBoard;
        bool savedP = driver.playerBoard.use3D, savedE = driver.enemyBoard.use3D;
        BoardBackup bak = default(BoardBackup);
        try
        {
            // ---------------- ① 3D 档：竞技场世界点 + ArenaLayer ----------------
            driver.use3DBoard = true;
            driver.playerBoard.use3D = true;
            driver.enemyBoard.use3D = true;

            if (!check(SaveAndPlaceProbeUnits(driver, ref bak, out slotView, out _),
                       "C（前提）夹具：我方 3 号格摆了一张探针单位、并真刷出了它的视图"
                     + " —— 摆不出来 ⇒ 下面「攻击件落点 = 卡根」那条没验（⛔ 不算跳过）"))
                return;

            Vector3 hudDust3D = driver.playerBoard.DropTargetWorld(slot) + Vector3.up * BattleDriver.LandDustOffsetY;
            driver.ClearHostFallbackProbeForTest();         // ⬅ 先清成哨兵：退路要是没走，下面几条读到哨兵 ⇒ 红
            driver.PlayDeployLandDustForTest(me, slot);     // ⬅ **生产那条**扬尘入口，且故意让 `dustHost = null`
            KillLastFallbackFx(driver);
            check(driver.LastHostFallbackArena,
                  "★ C 落地扬尘（卡视图取不到）：3D 档走**竞技场世界点**（不是 HUD 投影点）"
                + "｜判据 = C6 §1·3「扬尘 = 竞技场事件，原版由战场透视相机画」");
            check(driver.LastHostFallbackLayer == ArenaSlots.ArenaLayer,
                  $"★ C ……层 = `ArenaLayer`({ArenaSlots.ArenaLayer})（实得 {driver.LastHostFallbackLayer}）"
                + " —— 原版 13 场逐场实读：**只有战场透视相机画层 0**（mask 1137 / 32）"
                + "｜🧨 不传 `layer` ⇒ 停在 prefab 自带层 0 ⇒ 被 HUD 正交相机画 ⇒ 红");
            float dHud = Vector3.Distance(driver.LastHostFallbackPos, hudDust3D);
            check(dHud > 1f,
                  $"★ C 🧨 灭自证：扬尘落点与 **HUD 投影点**相距 {dHud:F3} > 1（两套坐标系真不是同一个点）"
                + " —— 改回 `CardEffects.Fire(name, DropTargetWorld…)` ⇒ 上面两条 + 本条一起红");

            Vector3 localGround = ArenaSlots.Position(slot, false);
            Vector3 wantGround = driver.boardRoot != null ? driver.boardRoot.TransformPoint(localGround) : localGround;
            check(Mathf.Abs(driver.LastHostFallbackPos.y - (wantGround.y + BattleDriver.LandDustOffsetY)) < 1e-3f,
                  $"C ……落点 y = **地面** + `LandDustOffsetY`({BattleDriver.LandDustOffsetY}) = "
                + $"{wantGround.y + BattleDriver.LandDustOffsetY:F4}（实得 {driver.LastHostFallbackPos.y:F4}）"
                + "｜判据 = `ArenaSlots.Position` 的 y 恒 0（卡站在地面上）+ 原版 `CardScript__LandOnGround.c` 的 `+up×0.015`"
                + "｜🧨 用卡心（卡根）当地面 ⇒ 高半张卡 ⇒ 这条红");

            // ---- 攻击件那一处退路（锚点 = null）----
            Vector3 hudAnchor = driver.playerBoard.DropTargetWorld(slot);
            driver.ClearHostFallbackProbeForTest();
            driver.FireAttackVfxForTest(AttackFxProbe, null, null, hudAnchor, Vector3.zero, me, slot);
            KillLastFallbackFx(driver);
            check(driver.LastHostFallbackArena && driver.LastHostFallbackLayer == ArenaSlots.ArenaLayer,
                  $"★ C 攻击件退路（锚点为 null）：3D 档同样走竞技场世界点 + `ArenaLayer`"
                + $"（arena = {driver.LastHostFallbackArena} · layer = {driver.LastHostFallbackLayer}）");
            if (slotView != null)
            {
                float dv = Vector3.Distance(driver.LastHostFallbackPos, slotView.transform.position);
                check(dv < 1e-3f,
                      $"C ……落点 = 那一格**卡的卡根世界位**（偏差 {dv:E2}）—— 与「视图在时调用方喂的 "
                    + "`transform.position`」是同一个点（口径不因视图在不在而变）");
            }
            else
            {
                Vector3 localRoot = ArenaSlots.RootPosition(slot, false, cardScale);
                Vector3 wantRoot = driver.boardRoot != null ? driver.boardRoot.TransformPoint(localRoot) : localRoot;
                float dr = Vector3.Distance(driver.LastHostFallbackPos, wantRoot);
                check(dr < 1e-3f,
                      $"C ……没视图时落点 = `ArenaSlots.RootPosition` 的预测卡根（偏差 {dr:E2}）"
                    + " —— 判据 = `ArenaSlots` 的「卡根 = 地面 + 半个卡高 × desiredScale」");
            }

            // ---------------- ② 2D 兜底档：老口径逐位不变 ----------------
            driver.use3DBoard = false;
            driver.playerBoard.use3D = false;
            driver.enemyBoard.use3D = false;

            Vector3 hudDust2D = driver.playerBoard.DropTargetWorld(slot) + Vector3.up * BattleDriver.LandDustOffsetY;
            driver.ClearHostFallbackProbeForTest();
            driver.PlayDeployLandDustForTest(me, slot);
            KillLastFallbackFx(driver);
            check(!driver.LastHostFallbackArena && driver.LastHostFallbackLayer == -1,
                  $"C 2D 兜底档：**不走**竞技场那一档（arena = {driver.LastHostFallbackArena} · layer = {driver.LastHostFallbackLayer}）"
                + " —— 这一档卡本来就活在 HUD 正交平面上，喂竞技场点才是错");
            check(Vector3.Distance(driver.LastHostFallbackPos, hudDust2D) < 1e-3f,
                  "C ……落点 = **HUD 投影点**（与改前逐位相同）"
                + "｜🧨 把 2D 档也改成竞技场点 ⇒ 本条红（那一档没有透视相机，特效会消失到没有相机画的地方）");

            driver.ClearHostFallbackProbeForTest();
            driver.FireAttackVfxForTest(AttackFxProbe, null, null, hudAnchor, Vector3.zero, me, slot);
            KillLastFallbackFx(driver);
            check(!driver.LastHostFallbackArena && driver.LastHostFallbackLayer == -1
                  && Vector3.Distance(driver.LastHostFallbackPos, hudAnchor) < 1e-3f,
                  "C 2D 兜底档（攻击件）：落点 = 老口径的 `anchorPos`、层 = prefab 自带 —— 逐位不变");
        }
        finally
        {
            KillLastFallbackFx(driver);
            driver.use3DBoard = savedUse3D;
            driver.playerBoard.use3D = savedP;
            driver.enemyBoard.use3D = savedE;
            RestoreBoards(driver, bak);      // 旗标先还原再刷（刷新要按**原来那一档**重摆）
        }
    }

    // ==================================================================
    //  夹具：摆探针单位 / 还棋盘
    // ==================================================================

    /// <summary>棋盘备份（`BattleScene` 的 A849 / E7 两节用的是同一个手法：保存 → 白盒改 → `RefreshAll` → 恢复）。</summary>
    struct BoardBackup
    {
        public bool Valid;
        public int Me, Foe;
        public UnitState[] MeBoard, FoeBoard;
    }

    /// <summary>双方各摆一张探针单位（贴督军那格 **3 / 5**）并刷出视图。
    /// <para>为什么不信「这一刻盘上刚好有人」：本文件跑在整轮末尾，盘面是这一局打到现在剩下的。</para>
    /// <para>⚠️ 白盒改棋盘**不发事件** ⇒ 必须自己 `RefreshAll()`（本文件夹具的惯例）。
    /// 调用方**必须**在 `finally` 里 <see cref="RestoreBoards"/>（哪怕本函数返回 false）。</para>
    /// <para>返回 false = 摆不出来（判不出座位 / 卡池里没有单位卡 / 刷完仍取不到视图）。</para></summary>
    static bool SaveAndPlaceProbeUnits(BattleDriver driver, ref BoardBackup bak,
                                       out CardView mine, out CardView theirs)
    {
        mine = null; theirs = null;
        var ctx = driver != null ? driver.Ctx : null;
        if (ctx == null || driver.MySeat < 0 || driver.MySeat >= ctx.Players.Length) return false;
        int me = driver.MySeat, foe = 1 - me;
        if (foe < 0 || foe >= ctx.Players.Length) return false;

        var pool = CardDatabase.Load();
        var unitDefMe = FirstUnitOf(pool, FactionOf(ctx, me));
        var unitDefFoe = FirstUnitOf(pool, FactionOf(ctx, foe));
        if (unitDefMe == null || unitDefFoe == null) return false;

        bak = new BoardBackup { Valid = true, Me = me, Foe = foe,
                                MeBoard = new UnitState[BoardSpec.Size], FoeBoard = new UnitState[BoardSpec.Size] };
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            bak.MeBoard[s] = ctx.Players[me].Board[s];
            bak.FoeBoard[s] = ctx.Players[foe].Board[s];
            if (s == ArenaSlots.WarlordSlot) continue;     // 督军留在原位（两半场都「从贴督军那格起连续」）
            ctx.Players[me].Board[s] = null;
            ctx.Players[foe].Board[s] = null;
        }
        ctx.Players[me].Board[3] = new UnitState(unitDefMe, false);
        ctx.Players[foe].Board[5] = new UnitState(unitDefFoe, false);

        driver.RefreshAll();                               // 白盒改棋盘不发事件 ⇒ 自己刷

        driver.MyUnits.TryGetValue(3, out mine);
        driver.FoeUnits.TryGetValue(5, out theirs);
        return mine != null && theirs != null;
    }

    /// <summary>把 <see cref="SaveAndPlaceProbeUnits"/> 改过的棋盘还回去并重刷（`Valid == false` ⇒ 什么都不做）。</summary>
    static void RestoreBoards(BattleDriver driver, BoardBackup bak)
    {
        if (!bak.Valid || driver == null || bak.MeBoard == null || bak.FoeBoard == null) return;
        var ctx = driver.Ctx;
        if (ctx == null || bak.Me < 0 || bak.Me >= ctx.Players.Length
            || bak.Foe < 0 || bak.Foe >= ctx.Players.Length) return;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            ctx.Players[bak.Me].Board[s] = bak.MeBoard[s];
            ctx.Players[bak.Foe].Board[s] = bak.FoeBoard[s];
        }
        driver.RefreshAll();
    }

    /// <summary>那个座位督军的阵营（判不出来给 null —— `FirstUnitOf` 会退回任意单位卡）。</summary>
    static string FactionOf(BattleContext ctx, int seat)
    {
        if (ctx == null || seat < 0 || seat >= ctx.Players.Length) return null;
        var w = ctx.Players[seat].Warlord;
        return w != null && w.Card != null ? w.Card.Faction : null;
    }

    /// <summary>卡池里第一张「能站场」的单位卡（优先这个阵营；没有就任意阵营）。</summary>
    static CardDef FirstUnitOf(List<CardDef> pool, string faction)
    {
        if (pool == null) return null;
        if (!string.IsNullOrEmpty(faction))
            foreach (var c in pool)
                if (c != null && c.Type == "unit" && c.Health > 0 && c.Faction == faction) return c;
        foreach (var c in pool)
            if (c != null && c.Type == "unit" && c.Health > 0) return c;
        return null;
    }

    static void KillLastFallbackFx(BattleDriver driver)
    {
        if (driver == null) return;
        var fx = driver.LastHostFallbackFx;
        if (fx != null) fx.Kill();
    }
}
