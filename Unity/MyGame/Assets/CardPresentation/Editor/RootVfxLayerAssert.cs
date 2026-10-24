// RootVfxLayerAssert.cs — 战斗线 `C6·Q1`（根特效归哪台相机）+ `C6·Q2`（卡根参考系）的断言
//
// 判据（只读判据代理 C6，2026-10-24）：
//   · `资料/普查产出_1024c/C6_表现界面轮抽剩余判据.md` §一（Q1）/ §二（Q2）
//   · `资料/普查产出_1024d/B1_战斗内卡牌全链路审查.md` §二·2·6 #6（层错）
//   · `资料/普查产出_1024d/R_真机全量验收计划.md` D36
//
// 断言口径（三条，⛔ 别改成「断我们自己写的常量」）：
//   ① **死亡爆散体**的 renderer 层 ∈ **战场相机** `cullingMask` 且 ∉ **UI/HUD 相机** `cullingMask`；
//   ② **残骸 Destroy 粒子**同①；
//   ③ **灭自证**：同一段生产代码在「2D 档（卡在层 0）」下必须落在 **HUD** 那一侧
//      ⇒ 任何把层写死成 0 或 8 的改法，都必然把其中一档弄红（结构上不可能同时满足）。
//
// 相机从**存盘的 `Battle.unity`**里取（`BoardCamera*` / tag=MainCamera），不是自己再造一对 ——
// 造一对就是拿我们的常量证明我们的常量（CI 会绿，屏幕照样错）。
//
// 宿主 = `WarpforgeArena1/Editor/AnimFXCheck.Run`（`工具/_run_8_checks.sh` 的第 13 条）。
using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardPresentation
{
    public static class RootVfxLayerAssert
    {
        const string P = "ROOTVFX ";
        const string BattleScenePath = "Assets/CardPresentation/Scenes/Battle.unity";

        /// <summary>残骸体 / 残骸 Destroy 粒子那两件（`CardFeel.RemnantDestroyByBody` 的死灵那一支）。</summary>
        const string RemnantBodyNecrons = "RemnantBody3D Necrons";
        const string RemnantDestroyNecrons = "Remnant Necrons Destroy particles";

        /// <summary>跑全部断言。`check(ok, 说明)` 由宿主提供（计入宿主的合计）。</summary>
        public static void Run(Action<bool, string> check)
        {
            if (check == null) return;
            Debug.Log(P + "=== C6·Q1 根特效层 / C6·Q2 卡根参考系 开始 ===");

            Scene battle = default(Scene);
            bool openedByUs = false;
            Camera boardCam = null, hudCam = null;
            GameObject fixtureRoot = null;
            CardView card = null;
            var spawned = new List<WarpforgeVFX.WarpforgeEffectPlayer>();

            try
            {
                // ---- 相机：从**存盘的战场场景**取真件 --------------------------------------------------
                battle = EnsureBattleScene(out openedByUs);
                FindCams(battle, out boardCam, out hudCam);
                check(boardCam != null,
                      "① 能取到**战场透视相机**（`Battle.unity` 里名字以 `BoardCamera` 开头的那台；"
                    + "取不到 ⇒ 下面「∈ 战场相机 cullingMask」这条断不了）");
                check(hudCam != null,
                      "① 能取到 **HUD/UI 相机**（`Battle.unity` 里 tag=MainCamera 那台）");
                if (boardCam == null || hudCam == null)
                {
                    Debug.LogWarning(P + $"  （相机取不到：场景有效 = {battle.IsValid()}、"
                                   + $"path = `{(battle.IsValid() ? battle.path : "<none>")}`）");
                    return;
                }
                // 相机自身合同的**前置**（这是判据，不是被测件 —— 它红了说明这条路整条前提没了）
                check((boardCam.cullingMask & (1 << ArenaSlots.ArenaLayer)) != 0,
                      $"★ 战场相机画得到 `ArenaLayer`({ArenaSlots.ArenaLayer})（mask = {boardCam.cullingMask}）");
                check((hudCam.cullingMask & (1 << ArenaSlots.ArenaLayer)) == 0,
                      $"★ HUD 相机**不画** `ArenaLayer`({ArenaSlots.ArenaLayer})（mask = {hudCam.cullingMask}）");
                check((hudCam.cullingMask & 1) != 0,
                      $"★ HUD 相机画得到 layer 0（2D 兜底档要用；mask = {hudCam.cullingMask}）");

                // ---- 夹具：一张真卡（`CardFace.Board` ⇒ 有 `body3D` 节点）----------------------------
                // ⚠️ `CardView.Create` 收 `CardData`，卡池给的是 `CardDef` —— 换算**只走**
                //   `BattleDriver.ToCardData`（生产那一份，public static；⛔ 别在这儿再写一份）。
                var pool = RuleEngine.CardDatabase.Load();
                var def = RuleEngine.CardDatabase.FindById(pool, "SOR8");
                if (def == null && pool != null && pool.Count > 0) def = pool[0];
                check(def != null, "① 夹具：卡池里取得到一张卡（`SOR8` / 兜底第一张）");
                if (def == null) return;
                var data = BattleDriver.ToCardData(def, def.Faction);

                fixtureRoot = new GameObject("ROOTVFX_fixtures");
                fixtureRoot.transform.position = Vector3.zero;
                card = CardView.Create(fixtureRoot.transform, data, "ROOTVFX_card", CardFace.Board);
                card.SetFace(CardFace.Board);                       // 建 `body3D`（爆散体那条路要取它的位姿）
                // 位姿**故意不在原点**：能验「卡根 ≠ 卡底边」这一步真的是按卡的位姿算的
                card.transform.position = new Vector3(1.2f, 2.5f, -0.4f);
                check(Mathf.Abs(card.transform.lossyScale.y - 1f) < 1e-3f,
                      $"① 夹具的卡在世界里缩放 = 1（实得 {card.transform.lossyScale.y:F4}；"
                    + "下面的「1.34 卡单位」按它换算）");

                // ---- ① 死亡爆散体：3D 档（卡在 ArenaLayer）----------------------------------------
                card.gameObject.layer = ArenaSlots.ArenaLayer;
                check(CardFeel.RootFxLayerForCard(card) == ArenaSlots.ArenaLayer,
                      $"① `RootFxLayerForCard`：3D 档的卡 ⇒ 层 {ArenaSlots.ArenaLayer}（实得 {CardFeel.RootFxLayerForCard(card)}）");
                check(CardFeel.RootFxLayerForCard(null) == 0,
                      "① `RootFxLayerForCard`：卡为 null ⇒ 层 0（不把「取不到」变成层 8 —— 那会被两台相机都漏掉）");

                var deadBody = CardFeel.ProbeSpawnDeathBody(card, CardFeel.DeathBodyFx);
                check(deadBody != null,
                      $"① 死亡爆散体真播出来了（效果库里有 `{CardFeel.DeathBodyFx}`）");
                if (deadBody != null)
                {
                    spawned.Add(deadBody);
                    AssertLayerAndCamera(check, "① 死亡爆散体（3D 档）", deadBody, ArenaSlots.ArenaLayer,
                                         boardCam, hudCam);
                    // Q2：落点 = **卡底边中心**（原版 prefab 根 = 卡底边），不是卡心
                    Vector3 bottom = card.transform.TransformPoint(new Vector3(0f, -CardView.Height * 0.5f, 0f));
                    float dBottom = Vector3.Distance(deadBody.transform.position, bottom);
                    check(dBottom < 1e-3f,
                          $"② 死亡爆散体落点 = **卡底边中心**（`TransformPoint(0, −Height/2, 0)`；"
                        + $"偏差 {dBottom:E2} < 1e-3）—— 判据 `CardScript._UnitDeath…:245-263` 取卡 transform.position，"
                        + "而**原版 prefab 根的原点就是卡底边**（C6 §2·1）");
                    float dCenter = Vector3.Distance(deadBody.transform.position, card.transform.position);
                    check(dCenter > 0.5f,
                          $"② 🧨 灭自证：落点**不是卡心**（旧写法 `card.transform.position`；"
                        + $"两者相距 {dCenter:F3} > 0.5 —— 改回卡心 ⇒ 这条立刻红）");
                }

                // ---- ③ 灭自证反面：2D 兜底档（卡在层 0）⇒ 必须落回 HUD 那台 -------------------------
                card.gameObject.layer = 0;
                check(CardFeel.RootFxLayerForCard(card) == 0,
                      $"③ `RootFxLayerForCard`：2D 档的卡 ⇒ 层 0（实得 {CardFeel.RootFxLayerForCard(card)}）");
                var deadBody2 = CardFeel.ProbeSpawnDeathBody(card, CardFeel.DeathBodyFx);
                check(deadBody2 != null, "③ 2D 档的死亡爆散体也真播出来了");
                if (deadBody2 != null)
                {
                    spawned.Add(deadBody2);
                    AssertLayerAndCamera(check, "③ 死亡爆散体（2D 档）", deadBody2, 0, boardCam, hudCam);
                    check(deadBody != null
                          && Vector3.Distance(deadBody2.transform.position, deadBody.transform.position) < 1e-3f,
                          "③ 🧨 灭自证：**只有层**随档位变，落点不跟着变（两档同一个世界点）");
                }

                // ---- ② 残骸 Destroy 粒子：3D 档 ------------------------------------------------------
                card.gameObject.layer = ArenaSlots.ArenaLayer;
                card.SetRemnantBody(true, RemnantBodyNecrons);
                bool hasRemnant = card.RemnantBodyRoot != null;
                check(hasRemnant,
                      $"② 夹具：残骸体 `{RemnantBodyNecrons}` 真建出来了（效果库里有那一件）");
                if (hasRemnant)
                {
                    var rem = CardFeel.ProbeSpawnRemnantDestroy(card, RemnantDestroyNecrons);
                    check(rem != null, $"② 残骸 Destroy 粒子真播出来了（`{RemnantDestroyNecrons}`）");
                    if (rem != null)
                    {
                        spawned.Add(rem);
                        AssertLayerAndCamera(check, "② 残骸 Destroy 粒子（3D 档）", rem, ArenaSlots.ArenaLayer,
                                             boardCam, hudCam);
                        float dRoot = Vector3.Distance(rem.transform.position,
                                                       card.RemnantBodyRoot.transform.position);
                        check(dRoot < 1e-3f,
                              $"② 残骸粒子的落点 = **残骸体根**（`RemnantBody__DoDestroyByAttack` 取 "
                            + "`GetDeathParticlePosition()`；死灵那一支 = 残骸体自身；"
                            + $"偏差 {dRoot:E2}）");
                    }

                    // 2D 档：层回到 0 ⇒ HUD 那台
                    card.gameObject.layer = 0;
                    var rem2 = CardFeel.ProbeSpawnRemnantDestroy(card, RemnantDestroyNecrons);
                    check(rem2 != null, "② 2D 档的残骸 Destroy 粒子也真播出来了");
                    if (rem2 != null)
                    {
                        spawned.Add(rem2);
                        AssertLayerAndCamera(check, "② 残骸 Destroy 粒子（2D 档）", rem2, 0, boardCam, hudCam);
                    }
                    card.gameObject.layer = ArenaSlots.ArenaLayer;
                }

                // ---- Q2：近战端点 `dy` = (受击方 effectsAnchor.y − 受击方 y) --------------------------
                // 原版 `fVar15 = anchor.position.y − targetCard.position.y`，随后 × `attackPositionYOffset`
                // （`CardScript._AttackMeleeAnim_d__357__MoveNext.c`）。原版卡根 = 卡底边 ⇒ 我们量到卡底边。
                float anchorH = CardFeel.EffectsAnchorHeight(card);
                float expectH = 1.34f * card.transform.lossyScale.y;
                check(Mathf.Abs(anchorH - expectH) < 1e-3f,
                      $"② `EffectsAnchorHeight` = 锚点到**卡底边**的高度 = `1.34 × lossyScale.y` = {expectH:F4}"
                    + $"（实得 {anchorH:F4}）—— 判据 `CardPrefab/Board Elements/EffectAnchor` `(0,1.34,−0.128)`"
                    + " + 原版根在卡底边（C6 §2·1）");

                Vector3 origin = new Vector3(0f, 2f, -5f);
                Vector3 targetPos = card.transform.position;
                Vector3 withDy = CardFeel.MeleeEndPos(origin, targetPos, true, card);
                Vector3 noDy = CardFeel.MeleeEndPos(origin, targetPos, true);
                float upDelta = withDy.y - noDy.y;
                float expectUp = expectH * CardFeel.AttackPositionYOffset;
                check(Mathf.Abs(upDelta - expectUp) < 1e-3f,
                      $"② 近战端点（带卡视图的重载）= 旧重载 + `up × {CardFeel.AttackPositionYOffset} × dy`"
                    + $"（抬高 {upDelta:F4}，应为 {expectUp:F4}）");
                check(Vector3.Distance(withDy, noDy) > 0.5f,
                      $"② 🧨 灭自证：`dy` 真的不是 0（两条重载相距 {Vector3.Distance(withDy, noDy):F3} > 0.5 —— "
                    + "把 `dy` 改回 0 / 把重载接回旧实现 ⇒ 这条立刻红）");
                check(Mathf.Abs(withDy.x - noDy.x) < 1e-4f && Mathf.Abs(withDy.z - noDy.z) < 1e-4f,
                      "② `dy` 只抬 Y（x/z 逐位不变）");
            }
            catch (Exception e)
            {
                check(false, "断言过程抛异常（下面这条必须看）：" + e.GetType().Name + "：" + e.Message);
                Debug.LogException(e);
            }
            finally
            {
                for (int i = 0; i < spawned.Count; i++)
                    if (spawned[i] != null) spawned[i].Kill();          // 别把「活着的特效」留给别的宿主
                if (fixtureRoot != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(fixtureRoot);
                    else UnityEngine.Object.DestroyImmediate(fixtureRoot);
                }
                if (openedByUs && battle.IsValid())
                    EditorSceneManager.CloseScene(battle, true);
            }

            Debug.Log(P + "=== C6·Q1 / C6·Q2 结束 ===");
        }

        /// <summary>「实例在**指定层**」+「相机画 / 不画」三条一组（两个族共用）。
        /// 相机那两条**按档位分方向**：层 8（3D 档）⇒ 战场透视相机**画** / HUD **不画**；
        /// 层 0（2D 兜底档）⇒ HUD **画** / 战场透视相机**不画**（`Battle.unity` 存盘里战场 cam mask = 256，不含层 0）。</summary>
        static void AssertLayerAndCamera(Action<bool, string> check, string what,
                                         WarpforgeVFX.WarpforgeEffectPlayer p, int expectLayer,
                                         Camera boardCam, Camera hudCam)
        {
            check(p.gameObject.layer == expectLayer,
                  $"{what}：根节点层 = {expectLayer}（实得 {p.gameObject.layer}）");

            int total = 0, wrong = 0;
            foreach (var r in p.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                total++;
                if (r.gameObject.layer != expectLayer) wrong++;
            }
            check(total > 0 && wrong == 0,
                  $"{what}：**整棵子树** {total} 个 renderer 全在层 {expectLayer}（不在的 {wrong} 个）—— "
                + "`WarpforgeEffectPlayer.Play` 的 `SetLayerRec` 覆盖到每一层渲染件");

            // 两条轴**各自按档位分方向**（2026-10-24 修：原来「战场相机画得到它」那半是无条件执行的，
            // 2D 档（层 0）也被要求「战场相机画得到」⇒ 恒假。判据 = D3_特效2红诊断.md §1/§4，
            // 真相机 mask 现读 `Battle.unity`：战场 cam 256（只画 8）· HUD cam −257（除 8 全画））。
            int bit = 1 << expectLayer;
            bool boardDraws = boardCam != null && (boardCam.cullingMask & bit) != 0;
            bool hudDraws   = hudCam   != null && (hudCam.cullingMask   & bit) != 0;
            string boardMask = boardCam != null ? boardCam.cullingMask.ToString() : "无相机";
            string hudMask   = hudCam   != null ? hudCam.cullingMask.ToString()   : "无相机";
            if (expectLayer == ArenaSlots.ArenaLayer)
            {
                // 3D 档：竞技场世界 ⇒ 战场透视相机，HUD 不能画
                check(boardDraws,
                      $"{what}：**战场透视相机**画得到它（mask {boardMask} ∋ 1<<{expectLayer}）");
                check(!hudDraws,
                      $"{what}：**HUD 正交相机不画**它（mask {hudMask} ∌ 1<<{expectLayer}）");
            }
            else
            {
                // 2D 兜底档：层 0 ⇒ **只归 HUD**；战场相机 mask 里没有层 0（Battle.unity 存盘 = 256）
                check(hudDraws,
                      $"{what}：HUD 相机画得到层 {expectLayer}（2D 兜底档只有这一台相机 —— "
                    + $"mask {hudMask} ∋ 1<<{expectLayer}）");
                check(!boardDraws,
                      $"{what}：**战场透视相机不画**它（mask {boardMask} ∌ 1<<{expectLayer}"
                    + " —— 2D 兜底档归 HUD）");
            }
        }

        // ---- 场景 / 相机 -------------------------------------------------------------------------

        /// <summary>拿**存盘的战场场景**（已开着就复用，没开就**附加**打开一份，跑完关掉）。</summary>
        static Scene EnsureBattleScene(out bool openedByUs)
        {
            openedByUs = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.IsValid() && s.path == BattleScenePath) return s;
            }
            try
            {
                if (System.IO.File.Exists(BattleScenePath))
                {
                    var sc = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);
                    openedByUs = sc.IsValid();
                    return sc;
                }
                Debug.LogWarning(P + $"  找不到 `{BattleScenePath}` ⇒ 取不到真相机。"
                               + "先跑一次 `BattleScene.BuildAndSaveScene`。");
            }
            catch (Exception e)
            {
                Debug.LogWarning(P + $"  附加打开 `{BattleScenePath}` 失败：{e.Message}");
            }
            return default(Scene);
        }

        /// <summary>相机按**名字/标签**认（⛔ 别按 `cullingMask` 认 —— 那就变成拿掩码证明掩码）。</summary>
        static void FindCams(Scene sc, out Camera board, out Camera hud)
        {
            board = null; hud = null;
            if (!sc.IsValid()) return;
            foreach (var root in sc.GetRootGameObjects())
            {
                if (root == null) continue;
                foreach (var c in root.GetComponentsInChildren<Camera>(true))
                {
                    if (c == null) continue;
                    if (board == null && !string.IsNullOrEmpty(c.name)
                        && c.name.StartsWith("BoardCamera", StringComparison.Ordinal))
                        board = c;
                    if (hud == null && c.CompareTag("MainCamera")) hud = c;
                }
            }
        }
    }
}
