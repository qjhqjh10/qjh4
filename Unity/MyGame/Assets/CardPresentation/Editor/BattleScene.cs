// BattleScene.cs — 可玩的对战场景 + 自检
//
// 两件事：
//   1. **建场景**（菜单 / CLI）—— 建好一个能点的对局：下方自己的 9 格、上方对手的 9 格、
//      底部手牌、HUD。存成 `Assets/CardPresentation/Scenes/Battle.unity`，
//      打开按 Play 就能拖牌、点单位打人、点 END TURN。
//   2. **自检**（CLI）—— 批处理下没有 play 循环、也没有真实输入，所以走 `Simulate*` 那套
//      （**和真实输入同一份逻辑**，不是另写一份），逐步断言 + 截图。
//
// 用法（菜单）：Tools > CardPresentation > 生成对战场景 / 对战自检
// 用法（CLI）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod BattleScene.Run \
//     -logFile "d:/4/_tmp_view/battle.log"
//   筛输出：grep "^BT "
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BattleScene
{
    const string P = "BT ";
    const string OutDir = @"d:/4/_tmp_view/battle";
    const string ScenePath = "Assets/CardPresentation/Scenes/Battle.unity";

    // ---- 版面（归一化，y 从底部算）----
    //
    // **全部来自原版实测**，不是拍的。原版 1920×1080 下量到的（出处：
    // `d:/warpforge/scripts/battle.gd` 的场卡尺寸体系 + `资料/对战排版_原版数值与改造方案.md`）：
    //     玩家行中心 y = 708 px  →  1 - 708/1080 = 0.3444
    //     敌方行中心 y = 466 px  →  1 - 466/1080 = 0.5685
    //     场卡 137.2 × 218.4 px，相邻中心距 149.3 px（= MinionSeparation 0.82 × 182.14）
    //     手牌卡 165 × 263 px，中心行 y≈950（= 玩家槽底 + 卡半高 + 9 px 隙）
    //
    // 换算：这套布局「可见高恒 10 世界单位」，1080p 下 **108 px / 世界单位**，
    //       所以 px → 归一化 = px/1920（横）、px/1080（纵）；
    //       px → 卡缩放   = px / (CardView 的尺寸 × 108)。
    //  ⚠️ 两行整体比原版**上移 0.05**（54 px）：原版手牌卡 263 px 高、底边贴着屏幕底（1081 px），
    //     它的前排卡同时也更高（218 px）。我这版卡更宽更矮（1.45:2.03），照搬 708 的话
    //     满手 12 张时中间那条弧会盖住前排督军卡底部的数值。上移 0.05 后正好留出 0.29 世界。
    const float EnemyLineY = 0.6185f;                                   // 原版 466/1080 = 0.5685
    const float PlayerLineY = 0.3944f;                                  // 原版 708/1080 = 0.3444
    const float BoardSpacing = 149.3f / 1920f;                          // 0.0778
    const float BoardScale = 137.2f / (CardView.Width * 108f);          // 0.876
    // 手牌中心行：原版量到 y≈950~961 px（0.110~0.120，卡底几乎贴着屏幕下沿）。
    // 取 0.155 是**构图上的微调**：卡底离下沿留 48 px，别真的贴着边；
    // 代价是满手 12 张那条弧离前排只剩 0.14 世界（原版是直接压上去的），仍然不重叠。
    const float HandBaselineY = 0.155f;
    const float HandScale = 165f / (CardView.Width * 108f);             // 1.054

    /// <summary>原版场卡的屏幕宽度占比（137.2/1920）—— 自检拿它当基准</summary>
    const float OriginalCardWidthRatio = 137.2f / 1920f;
    /// <summary>原版 9 槽整排跨度占比（8×149.3+137.2 = 1331.6 / 1920）</summary>
    const float OriginalBoardSpanRatio = 1331.6f / 1920f;

    [MenuItem("Tools/CardPresentation/生成对战场景")]
    public static void BuildAndSaveScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera cam = BuildScene(out _, out _, out _, out _);
        // 玩的时候手牌让位要走补间（不然拖拽时整排牌瞬移）。
        // **只在存场景这一路打开** —— 批处理自检要当场精确的位置，见 HandLayout.animateRelayout
        var hand = Object.FindObjectOfType<HandLayout>();
        if (hand != null) hand.animateRelayout = true;
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log(P + $"对战场景已存：{ScenePath} —— 打开按 Play 就能玩");
    }

    // ==================================================================
    //  自检
    // ==================================================================

    [MenuItem("Tools/CardPresentation/对战自检")]
    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        Debug.Log(P + "=== 对战自检 开始 ===");

        Camera cam = BuildScene(out BattleDriver driver, out BoardLayout pBoard,
                                out BoardLayout eBoard, out CardInteraction it);
        // 批处理下 `AddComponent` **不会**触发 `Awake`（那是 Play 模式的事），
        // 而选择器的按钮/底板、准星的 sprite/弧线都是运行时建在 `Build()` 里的 —— 这里显式补一次
        if (driver.selector != null) driver.selector.Build();
        if (driver.reticle != null) driver.reticle.Build();
        if (driver.skillPanel != null) driver.skillPanel.Build();
        driver.Begin(StarterCards.EmberFaction, StarterCards.TideFaction, 20260911);
        Step(0.3f);

        int pass = 0, fail = 0;

        void Check(bool ok, string msg)
        {
            if (ok) { pass++; Debug.Log(P + $"   ✓ {msg}"); }
            else { fail++; Debug.LogError(P + $"   ✗ {msg}"); }
        }

        var ctx = driver.Ctx;

        // ---- 1. 开局 ----
        Debug.Log(P + "--- 开局 ---");
        // 起手 3 张 + 第 1 回合抽 1 张（`Begin()` 里已经 BeginTurn 过了）= 4 张
        Check(ctx.Players[0].Hand.Count == RuleCore.StartHand + 1,
              $"手牌 {ctx.Players[0].Hand.Count} 张（起手 {RuleCore.StartHand} + 首回合抽 1）");
        Check(ctx.Players[0].Energy == 2, $"第 1 回合能量 {ctx.Players[0].Energy}（应 2）");
        Check(driver.HandCount == ctx.Players[0].Hand.Count, $"画面上的手牌 {driver.HandCount} 张 == 引擎的 {ctx.Players[0].Hand.Count} 张");
        Debug.Log(P + $"   画面上手牌：{driver.HandViewNames()}");
        Debug.Log(P + $"   引擎手牌　：{string.Join("/", HandNames(ctx, 0))}");
        Check(driver.MyUnits.Count == 1, "自己场上只有督军 1 个");
        Check(driver.FoeUnits.Count == 1, "对手场上也只有督军 1 个");
        Check(eBoard.SlotPosition(0).y > pBoard.SlotPosition(0).y, "对手的半场在自己的上面");
        Debug.Log(P + $"   我的阵营 {StarterCards.EmberFaction}，卡组 {DeckBuilder.ClassicDeckSize} 张，"
                    + $"手牌 {string.Join("/", HandNames(ctx, 0))}");
        Shot(cam, "01_开局");

        // ---- 1d. 特效：事件表里的名字在特效库里都找得到 ----
        Debug.Log(P + "--- 特效 ---");
        {
            Debug.Log(P + "   事件表 " + VfxMap.Describe(StarterCards.EmberFaction));
            Debug.Log(P + "   事件表 " + VfxMap.Describe(StarterCards.TideFaction));
            if (!WarpforgeVFX.WarpforgeEffectLibrary.Available)
            {
                Debug.Log(P + "   （特效库没加载 —— 跳过名字校验；卡牌流程不受影响）");
            }
            else
            {
                int n = 0, miss = 0;
                foreach (var name in VfxMap.AllNames())
                {
                    n++;
                    WarpforgeVFX.WFEffectEntry e;
                    if (WarpforgeVFX.WarpforgeEffectLibrary.Instance.TryGet(name, out e)) continue;
                    miss++;
                    Debug.LogWarning(P + "   库里没有这个特效：" + name);
                }
                Check(miss == 0, $"VfxMap 里 {n} 个特效名在库里都找得到（缺 {miss}）");
            }

            // ---- 事件时序表（`EventTiming`）：数错一位整段动作的节奏就全乱，**截图看不出来** ----
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Deploy) - 1.0f) < 1e-3f,
                  "登场 1.0s（原版 `Summon Troop Tween` 的 DelayTween duration=1.0）");
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Attack) - 0.3f) < 1e-3f,
                  "出手 0.3s（原版 `Recoil Normal Tween` 的 PunchTween duration=0.3）");
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Hit) - 0.5f) < 1e-3f,
                  "挨打 0.5s（原版 `Impact Light Tween`）");
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Ability) - 1.0f) < 1e-3f,
                  "技能 1.0s（原版 Mutation/Execution_BL/Vanguard/Hammer Slam 取中）");

            var eMeleeAtk = new BattleEvent { Kind = EvtKind.Attack, Ranged = false };
            var eRangedAtk = new BattleEvent { Kind = EvtKind.Attack, Ranged = true };
            var eHit = new BattleEvent { Kind = EvtKind.Hit };
            // 抬刀：出处是真反编译的 `_ResolveAttack_d__438__MoveNext.c:842`
            // `WaitForSeconds(attackStepTime × 2.0)`，`attackStepTime` 卡预制体实测 0.1
            Check(Mathf.Abs(EventTiming.DelayBetween(null, eMeleeAtk) - 0.2f) < 1e-3f,
                  $"一串里的第一条若是出手，先等**抬刀** {EventTiming.AttackWindUp}s（`attackStepTime 0.1 × 2.0`）");
            Check(Mathf.Abs(EventTiming.DelayBetween(eMeleeAtk, eHit)) < 1e-3f,
                  "近战出手 → 命中**不用等**（原版近战没有飞行段）");
            Check(Mathf.Abs(EventTiming.DelayBetween(eRangedAtk, eHit) - EventTiming.RangedFlight) < 1e-3f,
                  $"远程出手 → 命中要等弹道飞 {EventTiming.RangedFlight}s（出处 `card_anim_map` 的 `timeAtStartPos`）");
            Check(Mathf.Abs(EventTiming.DelayBetween(null, eHit)) < 1e-3f, "一串里的第一条若不是出手，不等");

            int unsourced = 0;
            foreach (EvtKind k in System.Enum.GetValues(typeof(EvtKind)))
                if (!EventTiming.IsSourced(k)) unsourced++;
            Check(unsourced == 1, $"只有**一条**是拍的（阵亡）—— `VarsGlobal` 资产缺失，秒数查不到");
            Debug.Log(P + "   事件时序（出处）：");
            foreach (EvtKind k in System.Enum.GetValues(typeof(EvtKind)))
                Debug.Log(P + $"     {k,-8} {EventTiming.DurationOf(k):F2}s　← {EventTiming.SourceOf(k)}");
        }

        // ---- 1b. 版面：原版实测数值对不对得上 ----
        Debug.Log(P + "--- 版面 ---");
        {
            float visW = LayoutSpace.VisibleWidth;
            var hand = Object.FindObjectOfType<HandLayout>();

            // ① 棋盘：9 槽跨度 / 场卡宽 —— 都是「占可见宽度的比例」，换分辨率也该成立
            float cardW = CardView.Width * pBoard.placedScale * LayoutSpace.Scale;
            float step = Mathf.Abs(pBoard.SlotPosition(1).x - pBoard.SlotPosition(0).x);
            float span = step * 8f + cardW;
            float spanRatio = span / visW, cardRatio = cardW / visW;
            Debug.Log(P + $"   棋盘：9 槽跨度 {spanRatio * 1920f:F1} px 占比 {spanRatio:P1}（原版 1331.6 / {OriginalBoardSpanRatio:P1}）"
                        + $"　场卡宽 {cardRatio * 1920f:F1} px（原版 137.2）　中心距 {step / visW * 1920f:F1} px（原版 149.3）");
            Check(Mathf.Abs(spanRatio - OriginalBoardSpanRatio) < 0.01f, "9 槽跨度 = 原版的 69.4% 可见宽");
            Check(Mathf.Abs(cardRatio - OriginalCardWidthRatio) < 0.004f, "场卡宽 = 原版的 137.2 px");
            Check(step > cardW, $"相邻两格不叠（空档 {(step - cardW) / visW * 1920f:F1} px）");

            // ② 两行不叠：我的行上沿 < 对手行下沿
            float pTop = pBoard.SlotPosition(0).y + CardView.Height * pBoard.placedScale * LayoutSpace.Scale * 0.5f;
            float pBot = pBoard.SlotPosition(0).y - CardView.Height * pBoard.placedScale * LayoutSpace.Scale * 0.5f;
            float eBot = eBoard.SlotPosition(0).y - CardView.Height * eBoard.placedScale * LayoutSpace.Scale * 0.5f;
            Check(eBot > pTop, $"两行不叠（我的上沿 {pTop:F2} < 对手下沿 {eBot:F2}，空档 {eBot - pTop:F2} 世界）");

            // ③ 敌方镜像：槽 0 在右边，且和自己这边左右对称
            var my0 = pBoard.SlotPosition(0); var foe0 = eBoard.SlotPosition(0);
            Check(foe0.x > eBoard.SlotPosition(8).x, $"敌方是镜像的（槽 0 在 x={foe0.x:F2}，槽 8 在 x={eBoard.SlotPosition(8).x:F2}）");
            Check(Mathf.Abs(foe0.x + my0.x) < 1e-4f, "两边的 0 号位左右对称（面对面）");

            // ④ 手牌：弧线、张角、间距
            float handTop = hand.SlotPosition(hand.numberOfCardsForMaxHeight / 2,
                                              hand.numberOfCardsForMaxHeight).y
                          + CardView.Height * hand.cardScale * LayoutSpace.Scale * 0.5f;
            float handBottom = hand.SlotPosition(0, hand.numberOfCardsForMaxHeight).y
                             - CardView.Height * hand.cardScale * LayoutSpace.Scale * 0.5f;
            Debug.Log(P + $"   手牌：{hand.Describe(4)}");
            Debug.Log(P + $"         {hand.Describe(12)}");
            Check(handTop <= pBot + 0.02f,
                  $"满手牌不压到战场（手牌上沿 {handTop:F2} ≤ 我的行下沿 {pBot:F2}）");
            Check(handBottom > -LayoutSpace.VisibleHeight * 0.5f + 0.05f,
                  $"满手牌不沉出屏幕底（下沿 {handBottom:F2}，屏底 -5.00）");

            // 弧线：中间高两头低；张角：两头朝外撇、中缝是正的
            float yMid = hand.SlotPosition(6, 12).y, yEnd = hand.SlotPosition(0, 12).y;
            Check(yMid > yEnd + 0.05f, $"弧线中间比两端高 {yMid - yEnd:F3} 世界（原版曲线：端低中高，不是抛物线）");
            float rotL = hand.RotationAt(0, 12), rotM1 = hand.RotationAt(5, 12), rotM2 = hand.RotationAt(6, 12);
            float rotR = hand.RotationAt(11, 12);
            Check(rotL > 0.5f && rotR < -0.5f, $"两端朝外撇（左 {rotL:F2}° / 右 {rotR:F2}°）");
            // 12 张时正中没有「那一张」，看的是**中缝两侧符号相反、各自都接近 0**
            Check(rotM1 > 0f && rotM2 < 0f && Mathf.Abs(rotM1) < 1f && Mathf.Abs(rotM2) < 1f,
                  $"中缝两侧几乎不正转（+{rotM1:F2}° / {rotM2:F2}°）");
            Check(Mathf.Abs(rotL + hand.RotationAt(11, 12)) < 1e-3f, "张角左右对称");

            // 间距自适应：牌少时不压缩、牌多时压在 maxLayoutSize 之内
            float s4 = hand.SpanWorld(4), s12 = hand.SpanWorld(12);
            float cap = hand.maxLayoutSize * visW;      // 16:9 下还要加宽高比修正，这里只看下界
            Check(s12 <= visW * 0.65f, $"12 张手牌跨度 {s12:F2} 世界 ≤ 可见宽的 65%（原版 60.25% 上限）");
            Check(Mathf.Abs(s4 - 3f * 1.45f * LayoutSpace.Scale) < 0.01f,
                  $"4 张时用原版间距 1.45 不压缩（实测 {s4 / 3f:F3} 世界/张）");
            Debug.Log(P + $"   手牌张数 → 跨度（世界）：4 张 {s4:F2}，12 张 {s12:F2}，上限 {cap:F2}");

            // ⑤ 原版美术：背景 + 卡框接上了没
            //    **装了才断言** —— 删掉 `Resources/Art/` 之后这两条自动跳过，
            //    自检照样全绿（「换自己的美术」这条路得能跑）
            Debug.Log(P + "   " + CardArt.Describe());
            var backdrop = driver.backdrop;
            if (CardArt.Available)
            {
                Check(backdrop != null && backdrop.Ready, "战场背景接上了（ArtBaker 烘的 arena1_bg）");
                if (backdrop != null && backdrop.Ready)
                {
                    var r = backdrop.ScreenRect();      // (图宽, 图高, 可见宽, 可见高)
                    Check(r.x >= r.z - 1e-3f && r.y >= r.w - 1e-3f,
                          $"背景「铺满」可见区（图 {r.x:F2}×{r.y:F2} ≥ 可见 {r.z:F2}×{r.w:F2}，不变形）");
                }
                var frameTex = CardArt.Frame(StarterCards.EmberFaction);
                Check(frameTex != null, $"卡框图加载到了（frame_{StarterCards.EmberFaction.ToLowerInvariant()}.png）");
                if (frameTex != null)
                {
                    var uv = CardView.FrameUv(frameTex);
                    Debug.Log(P + $"   卡框 {frameTex.width}×{frameTex.height}，卡本体 UV "
                                + $"u {uv.xMin:F3}..{uv.xMax:F3}  v {uv.yMin:F3}..{uv.yMax:F3}"
                                + $"（占比 {uv.width:P0}×{uv.height:P0}）");
                    Check(uv.width < 0.95f && uv.height < 0.95f,
                          "卡框 UV 裁到了卡本体上（不是把整张 1024² 留白一起铺上去）");
                }
            }
            else
            {
                Debug.Log(P + "   （`Resources/Art/` 是空的 —— 跳过美术那几条断言，"
                            + "用程序生成的占位卡面/纯色背景）");
            }

            Shot(cam, "01b_版面");
        }

        // ---- 1c. 满编手牌长什么样（12 张，专门看一眼扇形）----
        {
            var hand = Object.FindObjectOfType<HandLayout>();
            var driverRef = Object.FindObjectOfType<BattleDriver>();
            var root = new GameObject("HandPreview");
            var preview = new List<CardView>();
            for (int i = 0; i < 12; i++)
                preview.Add(CardView.Create(root.transform, CardData.Placeholder(i), $"Preview_{i:00}"));

            // 先把真手牌藏起来 —— 不然预览的 12 张会叠在真实的那几张上，截图上就成 16 张了
            int handN = driverRef != null ? driverRef.HandCount : 0;
            for (int i = 0; i < handN; i++)
            {
                var v = driverRef.HandViewAt(i);
                if (v != null) v.gameObject.SetActive(false);
            }

            hand.Refresh(preview);
            Step(0.05f);
            Shot(cam, "01c_满手12张");

            for (int i = 0; i < handN; i++)
            {
                var v = driverRef.HandViewAt(i);
                if (v != null) v.gameObject.SetActive(true);
            }
            Object.DestroyImmediate(root);
            Debug.Log(P + "   [图] 满手 12 张已截图（临时视图已销毁）");
        }

        // ---- 2. 拖拽上场（走真实的鼠标路径，不是 SimulatePlay）----
        Debug.Log(P + "--- 拖拽上场 ---");
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;      // 批处理下补间要手动推进
            int dragIdx = -1;
            for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                if (ctx.Players[0].Hand[i].Cost <= ctx.Players[0].Energy) { dragIdx = i; break; }

            if (dragIdx >= 0)
            {
                var view = driver.HandViewAt(dragIdx);
                int freeSlot = SimpleAI.FirstFreeSlot(ctx.Players[0]);
                var slotPos = pBoard.SlotPosition(freeSlot);
                int handBefore = ctx.Players[0].Hand.Count;
                int energyBefore = ctx.Players[0].Energy;
                string cardName = ctx.Players[0].Hand[dragIdx].Name;

                it.SimulateHover(view.transform.position);
                Step(0.05f);
                it.SimulatePress(view.transform.position);
                Step(0.2f);                                       // 拿起来

                // ---- 拖拽插槽：手牌边拖边让位（原版 GetClosestInHandSlot）----
                // 挑**最后一张**当探针（别挑到被拖的那张）：它排在空位右边，空位往右挪时它会左移一格
                int others = driver.HandCount - 1;
                int probeIdx = dragIdx >= others ? others - 1 : others;
                var probe = probeIdx >= 0 ? driver.HandViewAt(probeIdx) : null;
                float probeX0 = probe != null ? probe.transform.position.x : 0f;

                var farLeft = LayoutSpace.ToWorld(0.10f, HandBaselineY);
                for (int i = 0; i < 10; i++) { it.SimulateDrag(farLeft, 1f / 30f); Step(1f / 30f); }
                Check(it.InsertIndex == 0, $"拖到手牌最左 → 插槽 {it.InsertIndex}（应 0）");

                var farRight = LayoutSpace.ToWorld(0.90f, HandBaselineY);
                for (int i = 0; i < 10; i++) { it.SimulateDrag(farRight, 1f / 30f); Step(1f / 30f); }
                Check(it.InsertIndex == others, $"拖到手牌最右 → 插槽 {it.InsertIndex}（应 {others}）");
                if (probe != null)
                    Check(probe.transform.position.x < probeX0 - 0.1f,
                          $"空位右边的牌让位了（{probe.name} x {probeX0:F2} → {probe.transform.position.x:F2}）");

                for (int i = 0; i < 24; i++) { it.SimulateDrag(slotPos, 1f / 30f); Step(1f / 30f); }
                it.SimulateRelease(slotPos);
                // 落位动画走完才会触发 `OnDeployed`（引擎调用在回调里）——
                // 推进到「引擎里真有这个单位」为止，别写死一个时长（踩过：0.8s 不够，断言全挂）
                for (int i = 0; i < 90 && ctx.Players[0].Board[freeSlot] == null; i++) Step(1f / 30f);
                Step(0.2f);

                Check(ctx.Players[0].Board[freeSlot] != null,
                      $"拖到槽 {freeSlot} 后引擎里那一格有单位了（{ctx.Players[0].Board[freeSlot]?.Name}）");
                Check(ctx.Players[0].Board[freeSlot] != null && ctx.Players[0].Board[freeSlot].Name == cardName,
                      $"上去的正是拖的那张「{cardName}」");
                Check(ctx.Players[0].Hand.Count == handBefore - 1,
                      $"手牌 {handBefore} → {ctx.Players[0].Hand.Count}");
                Check(ctx.Players[0].Energy < energyBefore,
                      $"能量 {energyBefore} → {ctx.Players[0].Energy}");
                Check(driver.HandCount == ctx.Players[0].Hand.Count,
                      $"画面手牌 {driver.HandCount} == 引擎手牌 {ctx.Players[0].Hand.Count}（旧视图销毁了）");
            }
            else Debug.Log(P + "   （没有付得起的牌，跳过拖拽用例）");
        }
        Step(0.3f);
        Shot(cam, "02a_拖拽上场");

        // ---- 3. 费用约束：付不起的牌落不下去 ----
        Debug.Log(P + "--- 落点受费用约束 ---");
        int pricey = -1;
        for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
            if (ctx.Players[0].Hand[i].Cost > ctx.Players[0].Energy) { pricey = i; break; }
        if (pricey >= 0)
        {
            int code = RuleCore.CanPlayCard(ctx, 0, pricey, 1);
            Check(code == RuleCodes.ErrCost,
                  $"付不起的「{ctx.Players[0].Hand[pricey].Name}」({ctx.Players[0].Hand[pricey].Cost} 费)"
                + $"在 {ctx.Players[0].Energy} 能时被拒绝（{RuleCodes.Describe(code)}）");
        }
        else Debug.Log(P + "   （这局起手都付得起，跳过费用拒绝用例）");

        // ---- 4. 出牌（走引擎直通车，验数值账）----
        Debug.Log(P + "--- 出牌 ---");
        int played = 0;
        int unitsBefore = driver.MyUnits.Count;
        for (int guard = 0; guard < 8; guard++)
        {
            int card = SimpleAI.NextCardToPlay(ctx);
            if (card < 0) break;
            int slot = SimpleAI.FirstFreeSlot(ctx.Players[0]);
            if (slot < 0) break;
            int before = ctx.Players[0].Energy;
            if (driver.SimulatePlay(card, slot) != RuleCodes.OK) break;
            played++;
            Check(ctx.Players[0].Energy < before, $"打出后能量下降（{before} → {ctx.Players[0].Energy}）");
        }
        Check(driver.MyUnits.Count == unitsBefore + played,
              $"场上单位从 {unitsBefore} 涨到 {driver.MyUnits.Count}（出了 {played} 张）");
        // 前一步拖拽可能已经把能量花光了，所以「一张没出」也是正常结果 —— 关键是别剩下能出的牌
        Check(played > 0 || ctx.Players[0].Energy == 0,
              $"出牌阶段收尾：直通出了 {played} 张，能量剩 {ctx.Players[0].Energy}"
            + (played == 0 ? "（能量已被拖拽那步花光，符合预期）" : ""));
        Step(0.4f);
        Shot(cam, "02_我的回合出场");

        // ---- 5. 攻击 ----
        Debug.Log(P + "--- 攻击 ---");
        driver.SimulateEndTurn();                 // 交给对手
        driver.SimulateAiTurn();                  // 对手出牌 + 攻击 + 交回来
        Step(0.3f);
        Check(ctx.Active == 0, "对手走完，回合回到我这里");
        Debug.Log(P + $"   对手场上 {driver.FoeUnits.Count} 个  我场上 {driver.MyUnits.Count} 个");
        Debug.Log(P + $"   画面上手牌：{driver.HandViewNames()}");
        Debug.Log(P + $"   引擎手牌　：{string.Join("/", HandNames(ctx, 0))}");
        Debug.Log(P + $"   我场上　　：{driver.BoardViewNames(true)}");
        Shot(cam, "03_对手回合之后");

        // 找一对能打的
        int atkSlot = -1, tgtSlot = -1;
        for (int s = 0; s < RuleEngine.BoardSpec.Size && atkSlot < 0; s++)
        {
            var u = ctx.Players[0].Board[s];
            if (u == null || u.Exhausted) continue;
            for (int t = 0; t < RuleEngine.BoardSpec.Size; t++)
            {
                if (ctx.Players[1].Board[t] == null) continue;
                if (RuleCore.IsValidTarget(ctx, 0, s, 1, t, false) == RuleCodes.OK) { atkSlot = s; tgtSlot = t; break; }
            }
        }

        if (atkSlot >= 0)
        {
            // **原版的三步**：点自己的单位 → 弹选择器 → 选打法 → 点目标
            Check(driver.SimulateOpenCommand(atkSlot), $"点自己的槽 {atkSlot} → 弹出攻击方式选择器");
            Check(driver.SelectedSlot == atkSlot, "选中的就是那个单位");
            Check(driver.SelectorOpen, $"选择器开着（{driver.SelectorDescription}）");
            Check(driver.HasCommand(AttackKind.Melee), "有近战这一项");
            Step(0.25f);
            Shot(cam, "03b_攻击方式选择器");

            driver.SimulateCommand(AttackKind.Melee);
            Check(!driver.SelectorOpen, "选完打法，选择器收起");
            Check(driver.Command == AttackKind.Melee, "定下来的是近战");

            int foeBefore = ctx.Players[1].Board[tgtSlot].Health;
            Check(driver.SimulateResolve(tgtSlot) == RuleCodes.OK, $"打槽 {tgtSlot} 成功");
            var after = ctx.Players[1].Board[tgtSlot];
            Check(after == null || after.Health < foeBefore,
                  $"目标掉血了（{foeBefore} → {(after == null ? "阵亡" : after.Health.ToString())}）");
            Step(0.3f);
            Shot(cam, "04_攻击后");
        }
        else Debug.Log(P + "   （这一回合没有合法攻击，跳过）");

        // ---- 5b. 选择器：只有该有的项 / 拖出阈值 ----
        Debug.Log(P + "--- 攻击方式选择器（原版 Drag Attack Selector）---");
        ClearEffects();
        {
            // 远程单位 + 近战单位的表现应当不同：`Ballista` 近战 0 / 远程 4 —— 只该有「远程」那一项
            int probeSlot = FreeSlot(ctx, 0);
            if (probeSlot >= 0)
            {
                ctx.Players[0].Board[probeSlot] =
                    new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
                driver.RefreshAll();

                if (driver.SimulateOpenCommand(probeSlot))
                {
                    Check(driver.HasCommand(AttackKind.Ranged), "Ballista（近战 0 / 远程 4）有「远程」");
                    Check(!driver.HasCommand(AttackKind.Melee), "…而且**没有**「近战」—— 0 攻打不了近战");
                    Debug.Log(P + $"   选择器：{driver.SelectorDescription}");
                }

                // 指针压在按钮上 → 放大 1.3 倍 + 亮黄圈（原版 scaleMultiplierWhenSelected）
                var rp = driver.Selector.ButtonWorld(AttackKind.Ranged);
                Check(rp.HasValue, "拿得到「远程」按钮的世界坐标");
                if (rp.HasValue)
                {
                    var before = driver.Selector.ButtonScale(AttackKind.Ranged);
                    driver.SimulatePointerAt(rp.Value);
                    Check(driver.HoveredCommand == AttackKind.Ranged,
                          $"指针压到「远程」上 → 就是它（{driver.SelectorDescription}）");
                    Step(0.15f);
                    Check(driver.Selector.ButtonScale(AttackKind.Ranged) > before,
                          $"压着的按钮放大了（{before:F2} → {driver.Selector.ButtonScale(AttackKind.Ranged):F2}，原版 1.3 倍）");
                    Shot(cam, "03c_按钮高亮");
                }

                driver.SimulateDeselect();
                Check(!driver.SelectorOpen, "点槽外 → 选择器收起、取消指挥");
                ctx.Players[0].Board[probeSlot] = null;
                driver.RefreshAll();
            }
            else Debug.Log(P + "   （自己场上满了，跳过选择器用例）");

            // 拖拽阈值：0.085 × 屏高来自原版 `accumulatedDragForMinDistance`
            Check(Mathf.Abs(AttackSelector.DragThreshold01 - 0.085f) < 1e-6f,
                  $"拖拽阈值 = 原版的 0.085 屏高（{AttackSelector.DragThresholdWorld:F2} 世界单位）");
            Debug.Log(P + $"   拖出 {AttackSelector.DragThresholdWorld:F2} 世界单位（1080p ≈ "
                        + $"{AttackSelector.DragThreshold01 * 1080f:F0} px）才弹出选择器");
        }

        // ---- 5c. 选目标反馈：准星 + 弧线（原版 `NoCanvas2D/Attack Target Reticle`）----
        Debug.Log(P + "--- 选目标反馈：准星 + 弧线 ---");
        ClearEffects();
        {
            // 这一节要个干净的靶场，但**别把后面的用例饿着** —— 用完原样放回去
            var foeBackup = new UnitState[BoardSpec.Size];
            for (int t = 0; t < BoardSpec.Size; t++)
            {
                foeBackup[t] = ctx.Players[1].Board[t];
                if (t != BoardSpec.WarlordSlot) ctx.Players[1].Board[t] = null;
            }

            int retAtk = FreeSlot(ctx, 0);
            const int foeSlot = 1;                      // 离督军位远一点
            ctx.Players[0].Board[retAtk] =
                new UnitState(CardByName(StarterCards.Ember(), "Veteran"), false) { Exhausted = false };
            ctx.Players[1].Board[foeSlot] =
                new UnitState(CardByName(StarterCards.Tide(), "Tide Minion"), false);
            driver.RefreshAll();

            // 准星尺寸：原版 8.694 世界单位 × 49.77 px/单位 ÷ 108 px/单位 = 4.006
            var cs = TargetReticle.CrosshairWorldSize;
            Check(Mathf.Abs(cs.x - 4.006f) < 0.02f && Mathf.Abs(cs.y - 4.050f) < 0.02f,
                  $"准星世界尺寸 {cs.x:F3} × {cs.y:F3}（原版 8.694×8.791 按槽距换算过来的 4.006×4.050）");

            // 弧线材质：**必须是原版那个 shader** —— 截图上看不出「用的是不是它」
            Check(driver.reticle.LineShaderName == "Everguild/FX/Unlit UV scroll",
                  $"弧线用的是**原版材质**（`CroshairTrail` 的 shader = {driver.reticle.LineShaderName}）");
            Check(driver.reticle.LineTextureName == "CrosshairTrail",
                  $"弧线的贴图是原版那张（`_MainTex` = {driver.reticle.LineTextureName}）");

            var foeGo = driver.FoeUnits[foeSlot];
            Check(driver.SimulateOpenCommand(retAtk), $"点自己的槽 {retAtk} → 弹选择器");
            driver.SimulateCommand(AttackKind.Melee);
            Check(!driver.SelectorOpen, "定下打法 → 选择器收起");

            // ① 指针压在**合法**目标上 → 准星出现、压在目标身上、弧线拱起来
            driver.SimulatePointerAt(foeGo.transform.position);
            Check(driver.ReticleVisible, "指针压在合法目标上 → 准星出现");
            Vector3 cp;
            Check(driver.reticle.CrossPosition(out cp) &&
                  Vector3.Distance(new Vector3(cp.x, cp.y, 0f), new Vector3(foeGo.transform.position.x, foeGo.transform.position.y, 0f)) < 0.05f,
                  "准星**压在目标身上**（不是飘在别处）");
            Check(driver.reticle.ArcPointCount == 10,
                  $"弧线 {driver.reticle.ArcPointCount} 段（原版 `curvePoints` = 10）");
            float meleeBulge = driver.reticle.ArcBulge;
            Check(meleeBulge > 0.05f, $"近战的弧线拱起来了（离弦 {meleeBulge:F3} 世界单位）");
            var mc = driver.reticle.CrossColor;
            Check(mc.r > 0.8f && mc.g < 0.2f && mc.b < 0.1f,
                  $"近战 → 准星红（{mc.r:F2},{mc.g:F2},{mc.b:F2}，原版 `colorPresets` attackType=1）");
            // 合法目标的**底光**：原版 `Highlight`（红），锚在卡体那颗**近战**数值格上
            Check(driver.FoeUnits[foeSlot].CurrentGem == TargetGem.Melee,
                  "合法目标 → 卡面**近战**那颗数值格的底光点亮（原版 `Highlight` / `40K_melee_glow`）");
            Shot(cam, "09_选目标_准星");

            // ② 指针挪开 → 收起来。**不能显示「你正指着一个打不了的人」**
            driver.SimulatePointerAt(LayoutSpace.ToWorld(0.02f, 0.06f));
            Check(!driver.ReticleVisible, "指针离开合法目标 → 准星收起（不显示打不了的目标）");

            // ③ 换成远程：准星变紫，弧线**明显比近战平**（原版两条 profile 曲线差一个数量级）
            ctx.Players[0].Board[retAtk] =
                new UnitState(CardByName(StarterCards.Tide(), "Ballista"), false) { Exhausted = false };
            driver.RefreshAll();
            driver.SimulateOpenCommand(retAtk);
            driver.SimulateCommand(AttackKind.Ranged);
            driver.SimulatePointerAt(driver.FoeUnits[foeSlot].transform.position);
            Check(driver.ReticleVisible, "远程也能出准星");
            float rangedBulge = driver.reticle.ArcBulge;
            Check(rangedBulge < meleeBulge * 0.5f,
                  $"远程的弧线比近战平得多（{rangedBulge:F3} vs {meleeBulge:F3}）—— 原版 " +
                  "`curveProfileMelee` 0→1→0 对 `curveProfileRange` 0→0.099→0");
            var rc = driver.reticle.CrossColor;
            Check(rc.b > 0.8f && rc.r > 0.7f && rc.g < 0.5f,
                  $"远程 → 准星紫（{rc.r:F2},{rc.g:F2},{rc.b:F2}，原版 attackType=2）");
            Check(driver.FoeUnits[foeSlot].CurrentGem == TargetGem.Ranged,
                  "远程 → 点亮的是**远程**那颗数值格（原版 `Highlight ranged` / `40K_ranged_glow`）");
            Shot(cam, "10_选目标_远程准星");
            driver.SimulateDeselect();
            Check(driver.FoeUnits[foeSlot].CurrentGem == TargetGem.None,
                  "取消指挥 → 数值格底光也熄了");

            // ④ 主动技能：金色（原版 attackType=3/4）
            ctx.Players[0].Board[retAtk] =
                new UnitState(CardByName(StarterCards.Ember(), "Ironclad"), false) { Exhausted = false };
            driver.RefreshAll();
            if (driver.SimulateOpenCommand(retAtk) && driver.HasCommand(AttackKind.Ability))
            {
                driver.SimulateCommand(AttackKind.Ability);
                driver.SimulatePointerAt(driver.FoeUnits[foeSlot].transform.position);
                Check(driver.ReticleVisible, "技能（要选目标的那种）也出准星");
                var ac = driver.reticle.CrossColor;
                Check(ac.r > 0.8f && ac.g > 0.6f && ac.b < 0.4f,
                      $"技能 → 准星金（{ac.r:F2},{ac.g:F2},{ac.b:F2}，原版 attackType=3/4）");
            }
            else Debug.Log(P + "   （Ironclad 这轮放不出技能，跳过金色那条）");

            driver.SimulateDeselect();
            Check(!driver.ReticleVisible, "取消指挥 → 准星收起");

            // 靶场还原
            ctx.Players[0].Board[retAtk] = null;
            for (int t = 0; t < BoardSpec.Size; t++) ctx.Players[1].Board[t] = foeBackup[t];
            driver.RefreshAll();
        }

        // ---- 5. 护卫限制 ----
        Debug.Log(P + "--- 护卫（Vanguard）限制目标 ---");
        {
            var probe = RuleCore.NewBattle(
                DeckBuilder.StarterDeck(StarterCards.Of(StarterCards.EmberFaction), StarterCards.EmberFaction, 30, new System.Random(5)),
                DeckBuilder.StarterDeck(StarterCards.Of(StarterCards.TideFaction), StarterCards.TideFaction, 30, new System.Random(6)),
                7);
            RuleCore.BeginTurn(probe);
            // 手搓一个场景：对手场上放个护卫 + 一个普通单位，我放个攻击者
            probe.Players[1].Board[1] = new UnitState(CardByName(StarterCards.Tide(), "Reef Guard"), false) { Exhausted = false };
            probe.Players[1].Board[2] = new UnitState(CardByName(StarterCards.Tide(), "Siren"), false) { Exhausted = false };
            probe.Players[0].Board[1] = new UnitState(CardByName(StarterCards.Ember(), "Veteran"), false) { Exhausted = false };

            int noGuard = RuleCore.IsValidTarget(probe, 0, 1, 1, 2, false);
            int guard = RuleCore.IsValidTarget(probe, 0, 1, 1, 1, false);
            Check(noGuard == RuleCodes.ErrTarget, $"场上有护卫时打不了普通单位（{RuleCodes.Describe(noGuard)}）");
            Check(guard == RuleCodes.OK, "场上有护卫时可以打护卫");
        }

        // ---- 6. 技能与触发：引擎的**事件流** → 特效 ----
        //
        // 这两类特效以前**接不上** —— 不是特效的问题，是引擎里没有这两种事件。
        // 2026-09-12 引擎补上了 `EvtKind.Ability` / `EvtKind.Trigger`，这里验「真的变成画面」。
        Debug.Log(P + "--- 技能与触发（事件流 → 特效）---");
        {
            var ember = StarterCards.Ember();
            var tide = StarterCards.Tide();

            // 确保轮到我（上一小节打完可能已经自动交回合了）
            if (ctx.Active != 0) { driver.SimulateEndTurn(); driver.SimulateAiTurn(); }
            Check(ctx.Active == 0, "轮到我了（技能用例要有自己的回合）");

            ClearEffects();     // 前面几小节的火线/烟会盖住画面，截图前清一次

            // ---- ① 主动技能：走**和真实点击同一条路** ----
            //      路径是「点自己的单位 → 弹出选择器 → 选主动技能 → 点目标」，不是直接调引擎
            int casterSlot = FreeSlot(ctx, 0);
            int preySlot = FreeSlot(ctx, 1);
            Check(casterSlot >= 0 && preySlot >= 0, $"有空位摆这一对（我 {casterSlot} / 对手 {preySlot}）");

            if (casterSlot >= 0 && preySlot >= 0)
            {
                ctx.Players[0].Board[casterSlot] =
                    new UnitState(CardByName(ember, "Ironclad"), false) { Exhausted = false };
                ctx.Players[1].Board[preySlot] =
                    new UnitState(CardByName(tide, "Shellback"), false) { Exhausted = false };
                driver.RefreshAll();
                Step(0.3f);

                // 卡面上要**写出来**这个单位有技能 —— 不然玩家不知道有这回事
                // ⚠️ 期望值从 `CardText` 取，**不要写死 "ABILITY"** ——
                //    文案会跟着语言变（有中文字体就是「技能」），写死的话改文案就断一次
                var casterView = driver.MyUnits.ContainsKey(casterSlot) ? driver.MyUnits[casterSlot] : null;
                Check(casterView != null && casterView.Data.keywords != null
                      && casterView.Data.keywords.Contains(CardText.Keyword(KeywordTable.Ability)),
                      $"卡面上写明了主动技能（「{(casterView == null ? "没视图" : casterView.Data.keywords)}」）");
                var preyView = driver.FoeUnits.ContainsKey(preySlot) ? driver.FoeUnits[preySlot] : null;
                Check(preyView != null && preyView.Data.keywords != null
                      && preyView.Data.keywords.Contains(CardText.Keyword(KeywordTable.Backlash)),
                      $"对手单位的触发关键词也写在卡面上（「{(preyView == null ? "没视图" : preyView.Data.keywords)}」）");

                _fired.Clear();

                // 第一步：点自己的单位 → 弹选择器（**这时还不该播技能特效**）
                Check(driver.SimulateOpenCommand(casterSlot), $"点自己的槽 {casterSlot} → 弹选择器");
                Check(driver.SelectedSlot == casterSlot, "选中的就是 Ironclad");
                Check(driver.HasCommand(AttackKind.Ability), "选择器里有「主动技能」那一项");
                Check(driver.HasCommand(AttackKind.Melee), "…也有「近战」（Ironclad 2 攻）");
                Check(FiredCount(VfxMap.Resolve(VfxMap.Ability)) == 0, "只是弹了个选择器，还没放技能");
                Step(0.2f);
                Shot(cam, "06_攻击方式选择器");

                // 第二步：点「主动技能」→ 点亮合法目标 + 弹技能卡面板
                int startCode = driver.SimulateUseAbility(casterSlot, -1);
                Check(startCode == RuleCodes.OK,
                      $"选「主动技能」→ 进入选目标（{RuleCodes.Describe(startCode)}）");
                Check(driver.Command == AttackKind.Ability, "定下来的打法是主动技能");
                Step(0.3f);                       // 推完那 0.2s 的淡入
                Shot(cam, "06b_技能_选目标");

                // ---- 技能卡面板（原版 `ActiveSkillDesc`）----
                var sp = driver.skillPanel;
                Check(sp != null && sp.Visible, "放技能时弹出技能卡面板");
                if (sp != null && sp.Visible)
                {
                    Check(sp.ShownName == CardText.Name("Ironclad"),
                          $"面板上写的是施放者那张卡（「{sp.ShownName}」）");
                    Check(sp.ShownTargets == 1,
                          $"面板上的「可选目标数」= 合法目标数（{sp.ShownTargets}）");
                    Check(sp.Alpha > 0.9f, $"淡入推完了（alpha {sp.Alpha:F2}）");
                    Check(sp.CurrentLight == SkillPanel.Light.Available,
                          $"有合法目标 → 铺黄绿那层（原版 `LightAvailable`，{sp.CurrentLight}）");
                    Debug.Log(P + "   " + sp.Describe());

                    // 指针按在面板上 → 铺蓝那层（原版 `LightPressed`）
                    sp.SetPointer(sp.transform.position, true);
                    Check(sp.CurrentLight == SkillPanel.Light.Pressed,
                          $"指针按在面板上 → 铺蓝那层（原版 `LightPressed`，{sp.CurrentLight}）");
                    sp.SetPointer(sp.transform.position, false);
                    Check(sp.CurrentLight == SkillPanel.Light.Available, "松开 → 回到黄绿");
                    Shot(cam, "06c_技能卡面板");
                }

                // 第三步：点目标 → 结算
                int hp1 = ctx.Players[1].Board[preySlot].Health;
                int useCode = driver.SimulateUseAbility(casterSlot, preySlot);
                Check(useCode == RuleCodes.OK, $"点敌方目标 → 放技能（{RuleCodes.Describe(useCode)}）");
                Check(sp == null || sp.CurrentLight == SkillPanel.Light.Acting,
                      $"技能结算时面板铺白那层（原版 `ShowActingLight`，{(sp == null ? "无面板" : sp.CurrentLight.ToString())}）");
                var after = ctx.Players[1].Board[preySlot];
                Check(after == null || after.Health < hp1,
                      $"技能打中了（{hp1} → {(after == null ? "阵亡" : after.Health.ToString())}）");
                Check(driver.Command == AttackKind.None, "打完了，指挥状态收干净");
                // 事件排了时间线（`EventTiming`），得推到播完才断言 —— 以前是同一帧全播的
                StepThrough(driver);
                Check(FiredCount(VfxMap.Resolve(VfxMap.Ability)) > 0,
                      $"**引擎的 Ability 事件变成了特效**（{VfxMap.Resolve(VfxMap.Ability)}）");
                Check(FiredCount(VfxMap.Resolve(VfxMap.Hit)) > 0, "挨伤害也播了（Hit 事件）");
                Check(ctx.Players[0].Board[casterSlot].Exhausted, "放技能花掉了这个单位的行动");
                Step(0.3f);
                Shot(cam, "06b_技能_命中");
            }

            // ---- ② 触发效果：把带 Rally 的卡**正常打出去** ----
            ClearEffects();
            _fired.Clear();
            ctx.Players[0].Hand.Insert(0, CardByName(ember, "Flamecaller"));
            ctx.Players[0].Energy = 10;              // 这一段验的是触发，不是费用，别让能量挡路
            driver.RefreshAll();
            Step(0.2f);

            int rallySlot = FreeSlot(ctx, 0);
            Check(rallySlot >= 0, $"有空格部署（{rallySlot}）");
            if (rallySlot >= 0)
            {
                Check(driver.SimulatePlay(0, rallySlot) == RuleCodes.OK, "把 Flamecaller 打出去");

                var rallyView = driver.MyUnits.ContainsKey(rallySlot) ? driver.MyUnits[rallySlot] : null;
                Check(rallyView != null && rallyView.Data.keywords != null
                      && rallyView.Data.keywords.Contains(CardText.Keyword(KeywordTable.Rally)),
                      $"卡面写出了触发关键词（「{(rallyView == null ? "没视图" : rallyView.Data.keywords)}」）");

                StepThrough(driver);      // 事件时间线推到播完（Deploy 1.0s → Trigger）
                Check(FiredCount(VfxMap.Resolve(VfxMap.Deploy)) > 0,
                      $"**引擎的 Deploy 事件变成了特效**（{VfxMap.Resolve(VfxMap.Deploy)}）");
                Check(FiredCount(VfxMap.Resolve(VfxMap.Trigger)) > 0,
                      $"**引擎的 Trigger 事件变成了特效**（{VfxMap.Resolve(VfxMap.Trigger)}）");
                Step(0.35f);
                Shot(cam, "07_触发效果_Rally");
            }

            // ---- ③ 对手那边发动技能，画面同样要播（事件流不分敌我）----
            ClearEffects();
            _fired.Clear();
            driver.SimulateEndTurn();                // 交给对手
            int guardSlot = FreeSlot(ctx, 1);
            if (guardSlot >= 0)
            {
                // 珊瑚卫 0 攻、只会治疗 —— AI 的规则里它一定会放技能（打不了人就只能放技能）
                ctx.Players[1].Board[guardSlot] =
                    new UnitState(CardByName(tide, "Reef Guard"), false) { Exhausted = false };
                ctx.Players[1].Warlord.Health -= 5;  // 督军掉点血，治疗才有理由放
                driver.RefreshAll();

                driver.SimulateAiTurn();             // AI 出牌 + 放技能 + 攻击，然后交回来
                StepThrough(driver);                 // 事件时间线推到播完
                Check(FiredCount(VfxMap.Resolve(VfxMap.Ability)) > 0,
                      "对手单位放技能，画面照样播（同一个事件流，不区分敌我）");
                Step(0.3f);
                Shot(cam, "08_对手发动技能");
            }
            else Debug.Log(P + "   （对手场上满了，跳过「对手放技能」用例）");
        }

        // ---- 7. 打完一整局 ----
        Debug.Log(P + "--- 打完一整局 ---");
        int turnGuard = 0;
        while (!ctx.IsOver && turnGuard < 120)
        {
            SimpleAI.PlayTurn(ctx);
            // 这一段是**规则**的批量压力测试（120 回合），不是表现层走的那条路 ——
            // 一次 drain 会把上千个特效同时点着，批处理下既慢又看不出什么。
            // 特效的断言在上面几小节已经做过了，这里只把事件倒掉。
            driver.DropSignals();
            if (ctx.IsOver) break;
            RuleCore.EndTurn(ctx);
            RuleCore.BeginTurn(ctx);
            turnGuard++;
        }
        driver.RefreshAll();
        Step(0.2f);
        // 特效诊断：谁大得离谱一眼就能看出来（尺寸失控的效果会把整屏盖住）
        foreach (var pl in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
        {
            if (pl == null) continue;
            var rs = pl.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) { Debug.Log(P + $"   [特效诊断] {pl.name}：没有渲染器"); continue; }
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Debug.Log(P + $"   [特效诊断] {pl.name,-28} 位置 {pl.transform.position} "
                        + $"包围盒 {b.size}（{rs.Length} 个渲染器）");
        }
        Check(ctx.IsOver, $"分出胜负了（共 {ctx.Turn} 回合，{turnGuard} 轮循环）");
        Check(ctx.Winner >= 1 && ctx.Winner <= 3, $"赢家 = {ctx.Winner}（1/2 胜，3 平局）");

        // ---- 对局结算界面（原版 `EndBattlePanel`）----
        var end = driver.End;
        Check(end != null, "结算面板建出来了");
        if (end != null)
        {
            Check(end.Visible, "打完之后结算面板是显示的");
            Check(end.ResultText == "胜利" || end.ResultText == "失败" || end.ResultText == "平局",
                  $"结果文字是三种之一（实际「{end.ResultText}」）");
            Check(end.ShownSkulls >= 0 && end.ShownSkulls <= 3,
                  $"骷髅数在 0..3（实际 {end.ShownSkulls}）");
            // 骷髅判据只有一处（`DeckRules.SkullsFor`）；这里**独立算一遍对账** ——
            // 终局血量是「降到过的最低生命」的上界，所以面板那个数只会 ≥ 它
            int foeNow = Mathf.Max(0, ctx.Players[1 - driver.MyIndex].Warlord.Health);
            int floor = RuleEngine.DeckRules.SkullsFor(foeNow);
            Check(end.ShownSkulls >= floor,
                  $"骷髅数 ≥ 按终局血量算的下界（面板 {end.ShownSkulls} / 终局算 {floor}）—— 中途降得更低过就该更多");
        }
        string who = ctx.Winner == 3 ? "平局" : (ctx.Winner == 1 ? "我（Ember）胜" : "对手（Tide）胜");
        Debug.Log(P + $"   {who}；我督军剩 {Mathf.Max(0, ctx.Players[0].Warlord.Health)}，"
                    + $"对手督军剩 {Mathf.Max(0, ctx.Players[1].Warlord.Health)}");
        ClearEffects();
        Shot(cam, "05_对局结束");

        Debug.Log(P + $"=== 结束：{pass} 通过 / {fail} 失败 ===");

        // 最后验一下**存下来的那个场景**（自检上面的场景是当场建的，不是存的那份）
        var tail = new int[2] { pass, fail };
        CheckSavedScene(tail);
        pass = tail[0]; fail = tail[1];
        Debug.Log(P + $"=== 合计：{pass} 通过 / {fail} 失败 ===");

        if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    /// <summary>
    /// 打开存好的 `Battle.unity` 检查关键件还在不在。
    /// ⚠️ 这一步不能省：`BattleBackdrop` 的 `_mr/_tex` 是**私有字段、不进序列化**，
    ///    存场景之后再打开时它们是 null —— `Build()` 里那段「从子节点重新找回来」
    ///    就是为这个写的，必须真的验一次（不然只有进 Play 才会发现背景没了）。
    /// </summary>
    static void CheckSavedScene(int[] tally)
    {
        if (!File.Exists(ScenePath)) { Debug.LogWarning(P + "还没存过场景，跳过存档检查"); return; }

        EditorSceneManager.OpenScene(ScenePath);
        void Check(bool ok, string msg)
        {
            if (ok) { tally[0]++; Debug.Log(P + $"   ✓ {msg}"); }
            else { tally[1]++; Debug.LogError(P + $"   ✗ {msg}"); }
        }

        var bd = Object.FindObjectOfType<BattleBackdrop>();
        Check(bd != null, "存档里有 BattleBackdrop");
        if (bd == null) return;

        bd.Build();                     // 模拟运行时 Start() 的那一下
        Check(bd.Ready, $"存档重新打开后背景能重新绑上（{bd.Image?.name}）");
        var quad = bd.transform.Find("Backdrop");
        Check(quad != null && quad.GetComponent<MeshRenderer>() != null,
              "背景 quad 存在（它跟着场景一起存下来了）");
        Check(Object.FindObjectOfType<BattleDriver>() != null, "存档里有 BattleDriver");

        // ⚠️ 加这条是因为**差点漏掉**：`AttackSelector` 是 `BuildScene` 里新建的节点，
        //    而存档是上一次 `BuildAndSaveScene` 存的 —— 不重建场景，按 Play 就没有选择器，
        //    玩家点自己的单位不会有任何反应。自检里那部分是**当场建的新场景**，验不到存档。
        var sel = Object.FindObjectOfType<BattleDriver>();
        Check(sel != null && sel.selector != null, "存档里的 BattleDriver 接着 AttackSelector");
        Check(Object.FindObjectOfType<AttackSelector>() != null, "存档里有 AttackSelector 节点");
        Check(sel != null && sel.reticle != null, "存档里的 BattleDriver 接着 TargetReticle");
        Check(Object.FindObjectOfType<TargetReticle>() != null, "存档里有 TargetReticle 节点");
        Check(sel != null && sel.skillPanel != null, "存档里的 BattleDriver 接着 SkillPanel");
        Check(Object.FindObjectOfType<SkillPanel>() != null, "存档里有 SkillPanel 节点");
    }

    static string[] HandNames(BattleContext ctx, int p)
    {
        var l = new List<string>();
        foreach (var c in ctx.Players[p].Hand) l.Add($"{c.Name}({c.Cost})");
        return l.ToArray();
    }

    static CardDef CardByName(List<CardDef> pool, string name)
    {
        foreach (var c in pool) if (c.Name == name) return c;
        return pool[0];
    }

    // ==================================================================
    //  建场景
    // ==================================================================

    public static Camera BuildScene(out BattleDriver driver, out BoardLayout playerBoard,
                                    out BoardLayout enemyBoard, out CardInteraction interaction)
    {
        var sceneRoot = new GameObject("Battle");

        // 相机（布局基准，见 LayoutSpace）
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.055f, 0.06f, 0.08f);
        LayoutSpace.Apply(cam);                       // ⚠️ 必须在建任何卡之前 —— 坐标换算要用
        // ⚠️ 也必须**先把宽高比定死**再建东西：HUD 文字和格位底片的位置都是建的时候算一次的，
        //    批处理下相机的默认宽高比是 4:3，不先定死的话 16:9 的截图里它们会整片偏左（踩过）
        cam.aspect = LayoutSpace.DesignAspect;
        camGo.transform.position = new Vector3(0f, 0f, -20f);

        // 战场背景（原版 battlearena1 的实拍图，ArtBaker 烘的）—— 没有图就什么都不建
        var backdrop = sceneRoot.AddComponent<BattleBackdrop>();
        backdrop.Build();

        // 对手半场（上）—— **镜像**：原版敌方的 leftSlotPosNormal 是 +x，
        // 所以敌方槽 0 显示在画面**右侧**，两边的 0 号位在各自的左边（面对面）
        var eGo = new GameObject("EnemyBoard");
        eGo.transform.SetParent(sceneRoot.transform, false);
        enemyBoard = eGo.AddComponent<BoardLayout>();
        enemyBoard.lineY = EnemyLineY;
        enemyBoard.spacing = BoardSpacing;
        enemyBoard.placedScale = BoardScale;
        enemyBoard.mirror = true;
        enemyBoard.EnsureMarkers();

        // 我的半场（下）
        var pGo = new GameObject("PlayerBoard");
        pGo.transform.SetParent(sceneRoot.transform, false);
        playerBoard = pGo.AddComponent<BoardLayout>();
        playerBoard.lineY = PlayerLineY;
        playerBoard.spacing = BoardSpacing;
        playerBoard.placedScale = BoardScale;
        playerBoard.EnsureMarkers();

        // 手牌
        var handGo = new GameObject("Hand");
        handGo.transform.SetParent(sceneRoot.transform, false);
        var hand = handGo.AddComponent<HandLayout>();
        hand.baselineY = HandBaselineY;
        hand.cardScale = HandScale;

        // 交互
        interaction = sceneRoot.AddComponent<CardInteraction>();
        interaction.cam = cam;
        interaction.board = playerBoard;
        interaction.hand = hand;

        // 驱动
        driver = sceneRoot.AddComponent<BattleDriver>();
        driver.cam = cam;
        driver.playerBoard = playerBoard;
        driver.enemyBoard = enemyBoard;
        driver.hand = hand;
        driver.interaction = interaction;
        driver.boardRoot = sceneRoot.transform;
        driver.backdrop = backdrop;

        // 攻击方式选择器（原版 `Drag Attack Selector`）—— 单独一个节点，方便整块开关
        //
        // ⚠️ **这里只建空节点，不调 `Build()`** —— 它的按钮/底板/文字都是**运行时生成的**
        //    （和卡牌一样：存进场景的话 `ImageQuad` 的私有 `_tex`/运行时 `Material` 都不进序列化，
        //    重新打开就是一堆没材质的空壳）。`AttackSelector.Awake()` 会在 Play 时建。
        //    批处理自检没有 Awake，所以 `Run()` 里会显式补一次 `Build()`。
        var selGo = new GameObject("AttackSelector");
        selGo.transform.SetParent(sceneRoot.transform, false);
        driver.selector = selGo.AddComponent<AttackSelector>();

        // 选目标反馈：准星 + 弧线（原版 `NoCanvas2D/Attack Target Reticle`）—— 同样只建空节点，
        // 理由和上面选择器一模一样（准星是 `ImageQuad`，弧线是 `LineRenderer` + 运行时 `Material`）
        var retGo = new GameObject("TargetReticle");
        retGo.transform.SetParent(sceneRoot.transform, false);
        driver.reticle = retGo.AddComponent<TargetReticle>();

        // 技能卡面板（原版 `ActiveSkillDesc`）—— 同样只建空节点
        var skGo = new GameObject("SkillPanel");
        skGo.transform.SetParent(sceneRoot.transform, false);
        driver.skillPanel = skGo.AddComponent<SkillPanel>();

        // 特效钩子（特效库不在时只记日志，不影响流程）
        int shot = 0;
        _fired.Clear();
        CardEffects.Play = (name, pos, parent) =>
        {
            // 记名字：断言「引擎发了事件，画面真的播了对应的特效」靠它，
            // 不然只能看截图猜（截图看不出「播的是不是该播的那个」）
            _fired.Add(name);
            var fx = WarpforgeVFX.WarpforgeEffectPlayer.Play(name, null, pos, 1f, -1f);
            shot++;
            Debug.Log(P + $"   [特效] {name} @ {pos} → {(fx == null ? "**没播出来**" : fx.name)}"
                        + $"（活着的播放器 {WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount} 个）");
        };

        return cam;
    }

    /// <summary>自检期间 `CardEffects.Play` 收到过的特效名（按顺序）</summary>
    static readonly List<string> _fired = new List<string>();

    /// <summary>某个特效名在这一次里播过几次</summary>
    static int FiredCount(string effectName)
    {
        if (string.IsNullOrEmpty(effectName)) return 0;
        int n = 0;
        foreach (var s in _fired) if (s == effectName) n++;
        return n;
    }

    /// <summary>某一方第一个空的部署格，满了返回 -1</summary>
    static int FreeSlot(BattleContext ctx, int owner)
    {
        var p = ctx.Players[owner];
        for (int s = 0; s < BoardSpec.Size; s++)
            if (BoardSpec.IsDeployable(s) && p.Board[s] == null) return s;
        return -1;
    }

    /// <summary>
    /// 清掉之前几小节遗留的特效。
    ///
    /// ⚠️ **批处理下没有帧循环** —— `WarpforgeEffectPlayer` 靠 `Update` 自毁，自检里它不会被调，
    ///    于是每小节的效果一直堆着（实测播到第 24 个还在）。特别是阵亡特效 `Explosion_Possession`
    ///    会甩出满屏橙红火线，后面几张截图全被它盖住，人眼验收根本没法看。
    ///    **游戏里不存在这个问题**（有 Update），纯粹是自检环境的账。
    /// </summary>
    static void ClearEffects()
    {
        var alive = new List<WarpforgeVFX.WarpforgeEffectPlayer>();
        foreach (var p in WarpforgeVFX.WarpforgeEffectPlayer.ActivePlayers)
            if (p != null) alive.Add(p);
        foreach (var p in alive) p.Kill();
        Debug.Log(P + $"   （清掉 {alive.Count} 个遗留特效 —— 批处理里没有 Update，它们不会自己消失）");
    }

    static void Shot(Camera cam, string name)
    {
        const int W = 1920, H = 1080;
        cam.aspect = (float)W / H;

        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        rt.Release();

        File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log(P + $"   [图] {OutDir}/{name}.png");
    }

    static void Step(float dt)
    {
        // 事件时间线也要推 —— 批处理没有帧循环，`BattleDriver.Update` 不会跑。
        // 不推的话事件全卡在队列里，一条特效都不会播（踩过：断言全绿但画面全空）
        var d = Object.FindObjectOfType<BattleDriver>();
        if (d != null) d.AdvanceTimeline(dt);

        CardTween.Advance(dt);
        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>(true))
            if (ps != null) ps.Simulate(dt, withChildren: false, restart: false, fixedTimeStep: true);
    }

    /// <summary>
    /// 把事件时间线**推到播完**（每段动作有自己的时长，见 `EventTiming`）。
    /// 断言「某个特效播了没有」之前必须走这一步 —— 以前事件是同一帧全播的，`Step(0.3f)` 就够；
    /// 现在排了时间线，得推够。
    /// </summary>
    static void StepThrough(BattleDriver d, float maxSec = 6f)
    {
        float t = 0f;
        const float dt = 1f / 30f;
        while (d != null && d.TimelinePending > 0 && t < maxSec) { Step(dt); t += dt; }
        Step(0.1f);        // 再留一点给刚起来的特效
    }
}
