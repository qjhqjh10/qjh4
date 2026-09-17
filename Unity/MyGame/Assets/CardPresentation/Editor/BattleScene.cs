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
    //  ⚠️ **2026-09-12 撤掉了「两行整体上移 0.05」那个补丁**。它的由来是：我们的卡当时是
    //     1.45×2.03（比原版**矮 12%**），照搬 708 的话手牌顶不到战场、中间那条弧会盖住
    //     前排督军卡底部的数值 —— 于是把两行一起上移 54 px 躲开。
    //     现在卡本体改成原版的 2.0927×3.3313，两个尺寸都**精确对上**了
    //     （场卡 137.2×218.4、手牌 165.0×262.7，见 `Run()` 里的断言），补丁就该撤：
    //     按原版数值，手牌上沿正好贴住玩家行下沿（差 1.4 px）—— **原版就是这么贴着的**。
    const float EnemyLineY = 0.5685f;                                   // 原版 466/1080
    const float PlayerLineY = 0.3444f;                                  // 原版 708/1080
    const float BoardSpacing = 149.3f / 1920f;                          // 0.0778
    const float BoardScale = 137.2f / (CardView.Width * 108f);          // 0.607
    // 手牌中心行：原版 y≈950 px（0.1204）—— 卡底正好压在屏幕下沿上。
    const float HandBaselineY = 0.1204f;
    const float HandScale = 165f / (CardView.Width * 108f);             // 0.730

    /// <summary>原版场卡的屏幕宽度占比（137.2/1920）—— 自检拿它当基准</summary>
    const float OriginalCardWidthRatio = 137.2f / 1920f;
    /// <summary>原版 9 槽整排跨度占比（8×149.3+137.2 = 1331.6 / 1920）</summary>
    const float OriginalBoardSpanRatio = 1331.6f / 1920f;
    /// <summary>原版场卡高度 px（`3.3313 × 0.36 × 182.14`，见 `审查更正清单_0827.md:136`）</summary>
    const float BoardCardHeightPx = 218.4f;
    /// <summary>原版手牌卡高度 px（`3.3313 × 0.73 × 108`）</summary>
    const float HandCardHeightPx = 262.6f;

    [MenuItem("Tools/CardPresentation/生成对战场景")]
    public static void BuildAndSaveScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera cam = BuildScene(out BattleDriver driver, out _, out _, out _);
        // 玩的时候手牌让位要走补间（不然拖拽时整排牌瞬移）。
        // **只在存场景这一路打开** —— 批处理自检要当场精确的位置，见 HandLayout.animateRelayout
        var hand = Object.FindObjectOfType<HandLayout>();
        if (hand != null) hand.animateRelayout = true;
        // 手感补间（攻击位移 / 命中抖动 / 阵亡消散 / 发牌入场）同理，见 `BattleDriver.animateFeel`
        if (driver != null) driver.animateFeel = true;
        // 开局换牌（原版单机是进的；批处理自检默认跳过，见 `BattleDriver.mulliganEnabled`）
        if (driver != null) driver.mulliganEnabled = true;
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

        // 🆕 **DOTween 补间报错的计数器**（2026-09-16 加）。
        //
        // 为什么要有它：构建后 player 验证在真包里抓到 **22 条**
        // `DOTWEEN ► Target or field is missing/null`（补间打在一个**已经销毁的 Transform** 上），
        // 而**编辑器自检一条都没有**。加这个计数器，是要把「编辑器 0 条」从**假设**变成**量出来的数** ——
        // 顺手也是一把**永久的尺子**：这条断言哪天变红，就说明编辑器这条路也看得见了。
        // （成因与两轮实验见 `资料/特效还原_进度与交接.md` §七。）
        int dotween = 0;
        Application.LogCallback dwCounter =
            (cond, stack, type) => { if (cond != null && cond.Contains("DOTWEEN")) dotween++; };
        Application.logMessageReceived += dwCounter;

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

            // 🔴 **每个事件都必须能解析出特效名**（2026-09-15 加）。
            //    判据是 `VfxMap.Events`（**唯一一份**）。原来 `PlaySignal` 的 switch 漏了 4 个 kind
            //    —— `Return` 与三个阵营资源事件 —— 它们**结构上永远不播**，
            //    而且**当时没有任何断言会红**（名字都在库里，只是永远查不到）。
            //    这条就是给那个形状上的锁。
            {
                int unmapped = 0;
                foreach (var ev in VfxMap.Unmapped())
                {
                    unmapped++;
                    Debug.LogWarning(P + "   这个事件没配特效（结构上永远不播）：" + ev);
                }
                Check(unmapped == 0, $"事件表里每个事件都配了特效（没配的 {unmapped} 个）");
            }

            // ---- 事件时序表（`EventTiming`）：数错一位整段动作的节奏就全乱，**截图看不出来** ----
            // ⚠️ 2026-09-13 更正两条：出手**要算上蓄力**（卡预制体 `timeToChargeAttack` 0.35）、
            //    挨打**要算上复位**（`Impact Light Tween` 的 `ResetBodyTween` 是 `appendType=After`）。
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Deploy) - 1.0f) < 1e-3f,
                  "登场 1.0s（原版 `Summon Troop Tween` 的 DelayTween duration=1.0）");
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Attack) - 0.65f) < 1e-3f,
                  $"出手 {EventTiming.DurationOf(EvtKind.Attack)}s = 蓄力 0.35（卡预制体 `timeToChargeAttack`）"
                  + " + 冲一下 0.3（`Recoil Normal Tween`）");
            Check(Mathf.Abs(EventTiming.DurationOf(EvtKind.Hit) - 0.75f) < 1e-3f,
                  $"挨打 {EventTiming.DurationOf(EvtKind.Hit)}s（`Impact Light Tween`：0.5 的旋转 Punch + 0.25 复位）");
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

        // ---- 1e. 「临时卡（Ephemeral）」角标 ----
        // 规则书 `:183`/`:229` 说临时卡「回合结束若在手牌则移除，**从游戏中移除（非弃置）**」——
        // 玩家得**看得出哪张是临时的**，否则牌忽然没了就是「静默失败」。
        // ⚠️ 这一段只验**表现层这一侧**（引擎侧的机制在 `RuleEngineTest.TestEphemeral` 里）：
        //    图标导进来了没有、角标画不画得出来。
        Debug.Log(P + "--- 临时卡（Ephemeral）角标 ---");
        {
            var ephIcon = CardArt.Trait("ephemeral");
            Check(ephIcon != null,
                  "原版关键词图标 `Atlas_trait_icon_ephemeral` 已经导进 `Resources/Art/traits/`"
                  + "（跑 `工具/import_original_art.py`）");
            if (ephIcon != null)
                Debug.Log(P + $"   图标 {ephIcon.width}×{ephIcon.height}");

            // 拿**手牌里那张真实的卡**当数据源，另建一个视图来试「开/关」两条路。
            // ⚠️ 不直接改手牌里那张视图 —— `ShowEphemeral` 的状态由 `SyncHand` 每轮重刷，
            //    在这儿改了会被下一轮覆盖，测出来的东西不可信。
            var sample = driver.HandViewAt(0);
            Check(sample != null, "手牌里有牌可以当数据源");
            if (sample != null && ephIcon != null)
            {
                var probe = CardView.Create(driver.transform, sample.Data, "EphProbe");
                // 摆到可见区中央偏上，拍两张**只差角标**的图 —— 光看一张分不出「画没画」。
                // ⚠️ **放大 1.6 倍**：手牌尺寸下（165×263 px）80×80 的图标只有 24 px 左右，
                //    截图上几乎看不出差别 —— 尺子太小会把「画了」读成「没画」。
                probe.transform.localPosition = new Vector3(0f, 0.9f, -0.5f);
                probe.transform.localScale = Vector3.one * 1.6f;
                Check(!probe.EphemeralShown, "新造的卡**默认不带**角标（临时卡是少数）");
                Shot(cam, "27a_临时卡角标_关");

                probe.ShowEphemeral(true);
                Check(probe.EphemeralShown, "★ 打开之后 `EphemeralShown` 为真（角标真的画出来了）");
                Shot(cam, "27b_临时卡角标_开");

                probe.ShowEphemeral(false);
                Check(!probe.EphemeralShown, "★ 关掉之后为假 —— 不然一张临时卡打出去后，"
                      + "接手它那个视图的普通卡会**一直带着角标**");
                Object.DestroyImmediate(probe.gameObject);
            }
        }

        // ---- 1f. 🆕 棋盘单位卡的 buff/debuff 徽标（原版 `BattleCardUI.boardTraitIcons`）----
        // 出处与「哪些是我们挑的」全在 `Core/Badges.cs` 的文件头；这里只验它真画出来了、
        // 画的是不是**该画的那一枚**（截图看不出这个 —— 得断言贴图）。
        Debug.Log(P + "--- 棋盘徽标（buff/debuff）---");
        {
            Check(Badges.MaxSlots == 7, "7 个位（原版左 3 + 右 4，第 8 个关键词不再显示）");

            // ① 判据：关键词 → 图（**大小写/空格不统一**，靠归一后查表）
            Check(Badges.SpriteOf("Armour 2") == "armour", "`Armour 2` → `armour`（剥掉数值）");
            Check(Badges.SpriteOf("Blast 2.") == "blast", "`Blast 2.` → `blast`（剥掉数值与句点）");
            Check(Badges.SpriteOf("Flying") == "flying", "`Flying` → `flying`");
            Check(Badges.SpriteOf("Blood Thirst") == "bloodThirst", "`Blood Thirst` → `bloodThirst`（空格 + 驼峰）");
            Check(Badges.SpriteOf("Destroyer") == "frenzied",
                  "★ `Destroyer` → `frenzied` —— **图集里没有 `destroyer.png`**，按 token 名猜会画不出来");
            Check(Badges.SpriteOf("Talent: Skyborne Deployment") == "talent",
                  "`Talent: xxx` 取冒号前的词 → 图集里**真有** `talent.png`");
            Check(Badges.SpriteOf("Lord Commander") == null,
                  "★ 认不出就是 null（**不猜**）—— `Lord Commander` 这种图集里没有");
            Check(Badges.SpriteOf(null) == null, "空关键词不炸");

            // ② 角标：只有**卡面上带数值**的关键词才画（出处：规则书关键词表的「带数值」列）
            Check(Badges.CarriesValue("Armour 2") && Badges.CarriesValue("Hunt Mark 1"),
                  "带数值的（Armour / Hunt Mark）画角标");
            Check(!Badges.CarriesValue("Flying") && !Badges.CarriesValue("Rally"),
                  "不带数值的（Flying / Rally）不画角标");

            // ③ 位子就是原版预制体的那 7 个（`TraitIconContainer*.json` 换算，见 `Badges.SlotAt`）
            Check(Mathf.Abs(Badges.SlotAt(0).x + 0.563f) < 0.002f && Mathf.Abs(Badges.SlotAt(0).y - 0.99f) < 0.01f,
                  "左 1 在 (−0.563, +0.99) —— 原版 `TraitIconContainer 1` 的本地坐标换算值");
            Check(Mathf.Abs(Badges.SlotAt(6).x - 0.563f) < 0.002f && Badges.SlotAt(6).y < Badges.SlotAt(5).y,
                  "右 4 在右下（原版右列比左列多一个位）");

            // ④ 全卡池覆盖：**认不出图标的报数**（红线：不许静默少画）
            {
                int cards = 0, withBadges = 0, unresolved = 0;
                var missing = new System.Collections.Generic.SortedSet<string>();
                foreach (var c in RuleEngine.CardDatabase.Load())
                {
                    cards++;
                    var b = Badges.For(c.Keywords, c.Desc);
                    if (b.Count > 0) withBadges++;
                    foreach (var kv in c.Keywords)
                        if (Badges.SpriteOf(kv.Key) == null) { unresolved++; missing.Add(kv.Key); }
                }
                Debug.Log(P + $"   全卡池 {cards} 张：能画出徽标的 {withBadges} 张，"
                            + $"认不出图的关键词 {unresolved} 处 / {missing.Count} 种");
                foreach (var m in missing) Debug.Log(P + $"     认不出：`{m}`");
                Check(cards > 1000, "卡池读到了");
                Check(withBadges > 400, "过半数的卡至少有一枚徽标（原版这些卡身上就是有关键词的）");
            }

            // ⑤ 真渲染：造一张**带 4 枚徽标**的卡，拍「有 / 无」两张图（只差徽标那几层）
            {
                var badges = new System.Collections.Generic.List<Badge>
                {
                    new Badge { sprite = "armour",   counter = 2, active = true },
                    new Badge { sprite = "blast",    counter = 3, active = true },
                    new Badge { sprite = "flying",   counter = 0, active = true },
                    new Badge { sprite = "vulnerable", counter = 1, active = true },
                };
                var d = CardData.Simple("BadgeProbe", 3, 3, 4);
                d.artId = "UM_Heavy_Intercessor";  // 借一张有立绘的卡，截图里好看（立绘按 **id** 取名）
                d.badges = badges;
                var probe = CardView.Create(driver.transform, d, "BadgeProbe");
                probe.transform.localPosition = new Vector3(-0.62f, 0.86f, -0.5f);
                probe.transform.localScale = Vector3.one * 1.9f;
                Check(probe.BadgesShown == 4, $"4 枚都画出来了（实际 {probe.BadgesShown}）");
                Check(probe.BadgeTexture(0) != null && probe.BadgeTexture(0).name.ToLower().Contains("armour"),
                      "★ 第 1 枚画的**就是** `armour` 那张图（不是随便一张）");
                Check(probe.BadgeCounter(0) == "2" && probe.BadgeCounter(1) == "3",
                      "角标数字 = 关键词的值（护甲 2 / 爆裂 3）");
                Check(probe.BadgeCounter(2) == "", "不带数值的关键词（Flying）没有角标");
                Shot(cam, "28a_徽标_四枚");
                Check(probe.SetBadges(null) == 0 && probe.BadgesShown == 0,
                      "★ 清空后一位都不剩（原版每轮重算前也是先全部 Toggle(false)）");
                var kept = probe.BadgeTexture(0);     // 清空只关层、不销毁
                Check(kept != null, "清空只是关掉层，贴图还在（下次复用）");
                Shot(cam, "28b_徽标_清空");
                Object.DestroyImmediate(probe.gameObject);
            }

            // ⑥ 场上真单位：**每张卡「画出来的数量」必须等于「它拿到的徽标数」**
            //    （这一条不依赖「此刻场上正好有带关键词的卡」，是条恒等式，什么时候都成立）
            {
                int total = 0, withBadges = 0, bad = 0;
                foreach (var kv in driver.MyUnits)
                {
                    total++;
                    if (kv.Value == null) continue;
                    int want = kv.Value.Data.badges != null ? kv.Value.Data.badges.Count : 0;
                    if (kv.Value.BadgesShown != want) bad++;
                    if (want > 0) withBadges++;
                }
                foreach (var kv in driver.FoeUnits)
                {
                    total++;
                    if (kv.Value == null) continue;
                    int want = kv.Value.Data.badges != null ? kv.Value.Data.badges.Count : 0;
                    if (kv.Value.BadgesShown != want) bad++;
                    if (want > 0) withBadges++;
                }
                Debug.Log(P + $"   场上有卡 {total} 张，其中带徽标的 {withBadges} 张");
                Check(total > 0, "场上有卡");
                Check(bad == 0, "★ 每张场上卡「画出来的徽标数 == 数据里的徽标数」（这张不等就是漏画/多画）");
                // ⚠️ 这一刻**可能一张带关键词的卡都没有**（测试脚本刚开局）—— 所以这条只记数不判死，
                //    真渲染那条路由上面第 ⑤ 步的探针卡 + 整局截图盯着（`28a_徽标_四枚.png`）。
                Debug.Log(P + (withBadges > 0 ? "   本刻场上有带徽标的卡 ✓" : "   本刻场上没有带徽标的卡（不是失败）"));
            }
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
            float cardH = CardView.Height * pBoard.placedScale * LayoutSpace.Scale;
            Debug.Log(P + $"   棋盘：9 槽跨度 {spanRatio * 1920f:F1} px 占比 {spanRatio:P1}（原版 1331.6 / {OriginalBoardSpanRatio:P1}）"
                        + $"　场卡 {cardRatio * 1920f:F1}×{cardH / visW * 1920f:F1} px（原版 137.2×218.4）"
                        + $"　中心距 {step / visW * 1920f:F1} px（原版 149.3）");
            Check(Mathf.Abs(spanRatio - OriginalBoardSpanRatio) < 0.01f, "9 槽跨度 = 原版的 69.4% 可见宽");
            Check(Mathf.Abs(cardRatio - OriginalCardWidthRatio) < 0.004f, "场卡宽 = 原版的 137.2 px");
            // 🆕 2026-09-12：**高度**这条原来没有 —— 宽一直是对的，高矮了 26 px（192.4 vs 218.4），
            //    根因是卡本体比例被我们改成 1.45×2.03（0.714）而不是原版的 2.0927×3.3313（0.628）。
            //    现在两样都断言，改坏哪一样都会红。
            Check(Mathf.Abs(cardH / visW * 1920f - BoardCardHeightPx) < 2f,
                  $"场卡高 = 原版的 {BoardCardHeightPx} px");

            // 手牌卡同一条道理：宽对了不算数，**高**也要对（原来只有宽是对的）
            float handW = CardView.Width * HandScale * LayoutSpace.Scale;
            float handH = CardView.Height * HandScale * LayoutSpace.Scale;
            Debug.Log(P + $"   手牌卡 {handW / visW * 1920f:F1}×{handH / visW * 1920f:F1} px（原版 165.0×{HandCardHeightPx}）");
            Check(Mathf.Abs(handW / visW * 1920f - 165f) < 2f, "手牌卡宽 = 原版的 165.0 px");
            Check(Mathf.Abs(handH / visW * 1920f - HandCardHeightPx) < 2f,
                  $"手牌卡高 = 原版的 {HandCardHeightPx} px");
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
            // ⚠️ 2026-09-12：这两条原来写的是「不压到战场 / 不沉出屏幕底」。卡本体改成原版的
            //    2.0927×3.3313 之后，按**原版数值**摆，这两件事**本来就会各差一点点**：
            //      · 原版手牌卡 263 px 高、中心行 950 px → 卡底在 **1081.3 px**（屏幕下沿是 1080）；
            //      · 中列那张因为弧高（`ArcHeightFull`，来自原版布局曲线）还要往上抬 ~30 px，
            //        会探进前排卡底部一点。
            //    所以改成**和原版几何对账**，而不是「绝对不许碰」—— 后者当初成立是因为我们的卡
            //    偏矮 12%、两行被整体上移了 54 px，那个补丁已经撤掉（见文件头）。
            float handBottomPx = (LayoutSpace.VisibleHeight * 0.5f - handBottom) * 108f;
            float handTopPx    = (LayoutSpace.VisibleHeight * 0.5f - handTop) * 108f;
            float rowBottomPx  = (LayoutSpace.VisibleHeight * 0.5f - pBot) * 108f;
            Debug.Log(P + $"   手牌卡底 {handBottomPx:F1} px（原版 1081.3）　中列卡顶 {handTopPx:F1} px"
                        + $"　我的行下沿 {rowBottomPx:F1} px（原版 817.2）　叠 {rowBottomPx - handTopPx:F1} px");
            Check(Mathf.Abs(handBottomPx - 1081.3f) < 3f,
                  "手牌卡底压着屏幕下沿 = 原版的 1081.3 px（卡底本来就会出屏 1 px）");
            Check(rowBottomPx - handTopPx < 40f,
                  $"中列手牌探进前排的深度很小（{rowBottomPx - handTopPx:F1} px ≤ 40；原版同样会探进去）");

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

        // ---- 1e. 右侧能量区：敌方水晶 / 底板 / 任务点（2026-09-12 补的一批原版元素）----
        // ⚠️ 这几件的毛病**截图看不出来** —— 少一颗水晶、底板图拿错、任务点在水晶内侧，
        //    画面都「看着挺满」。所以按数值断言。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv != null)
            {
                // ① 敌方水晶：原版 `EnemyMana` 是有的，我们原来一颗都没画
                Check(drv.FoeEnergyGemTex.StartsWith("40k_battle_energy_"),
                      $"敌方能量水晶用的是原版两张图（现在 `{drv.FoeEnergyGemTex}`）");
                var fp = drv.FoeEnergyPos01;
                Check(Mathf.Abs(fp.x - 0.97060f) < 0.002f && Mathf.Abs(fp.y - 0.73278f) < 0.002f,
                      $"敌方水晶在 x01 {fp.x:F5} / y01 {fp.y:F5}（原版权威表 0.97060 / 0.73278）");
                Check(drv.FoeEnergyText != null && drv.FoeEnergyText.Contains("/"),
                      $"敌方能量数字读得出来（`{drv.FoeEnergyText}`）");

                // ② 两块底板：`Card Frame Cost Icon`
                Check(drv.MyEnergyPlateTex == "Card_Frame_Cost_Icon" && drv.FoeEnergyPlateTex == "Card_Frame_Cost_Icon",
                      $"两块能量底板都是 `Card_Frame_Cost_Icon`（我 `{drv.MyEnergyPlateTex}` / 敌 `{drv.FoeEnergyPlateTex}`）");

                // ③ 任务点：**我方在水晶下方、敌方在上方**（原来两个都摆在水晶内侧，是接片的偏移）
                var mp = drv.QuestIconPos(true);
                var qp = drv.QuestIconPos(false);
                var myE = drv.MyEnergyPos01;
                Check(Mathf.Abs(mp.x - 0.97180f) < 0.002f && Mathf.Abs(mp.y - 0.40347f) < 0.002f,
                      $"我方任务点在 x01 {mp.x:F5} / y01 {mp.y:F5}（原版 0.97180 / 0.40347）");
                Check(Mathf.Abs(qp.x - 0.97133f) < 0.002f && Mathf.Abs(qp.y - 0.81569f) < 0.002f,
                      $"敌方任务点在 x01 {qp.x:F5} / y01 {qp.y:F5}（原版 0.97133 / 0.81569）");
                Check(mp.y < myE.y && qp.y > fp.y,
                      $"任务点在水晶**外侧**（我 {mp.y:F3} < 水晶 {myE.y:F3}；敌 {qp.y:F3} > {fp.y:F3}）");
            }
        }

        // ---- 1f. 四小件（2026-09-13 补）：牌库张数底板 / 手牌数底板 / 本回合已出牌数 / 里程碑骷髅 ----
        // ⚠️ 这四样的毛病**截图都看不出来**：半透明板画成实心、板压根没画、出牌灯该亮不亮、
        //    骷髅摆到名牌外面 —— 画面都「看着挺满」。所以一律按数值断言。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv != null)
            {
                // ① 牌库张数底板（原版 `PlayerDeck/Player Deck Size Container`，敌方那个小一号）
                Check(drv.DeckSizePlateTex == "40K_display",
                      $"张数底板用的是 `40K_display`（现在 `{drv.DeckSizePlateTex}`）");
                Check(Mathf.Abs(drv.MyDeckSizePlateWorldH - 59.11f / 108f) < 0.002f,
                      $"我方张数底板高 {drv.MyDeckSizePlateWorldH:F4}（原版 59.11 px = 0.5473，"
                    + "是 `AspectRatioFitter` 4.0346 按父宽 238.5 算出来的，不是 sizeDelta）");
                Check(Mathf.Abs(drv.FoeDeckSizePlateWorldH - 52.05f / 108f) < 0.002f,
                      $"敌方张数底板高 {drv.FoeDeckSizePlateWorldH:F4}（原版 52.05 px = 0.4819，敌方小一号）");
                Check(Mathf.Abs(drv.DeckSizePlateAlpha - 0.6941177f) < 0.01f,
                      $"张数底板是**半透明**的（α {drv.DeckSizePlateAlpha:F4}，原版 m_Color.a = 0.6941）");
                Check(drv.PileLabelWorldW > 0.01f && drv.PileLabelWorldW < drv.MyDeckSizePlateWorldH * (442f / 112f) - 0.05f,
                      $"张数文字宽 {drv.PileLabelWorldW:F3} < 底板宽 "
                    + $"{drv.MyDeckSizePlateWorldH * (442f / 112f):F3}（原版文字是 TMP 自动缩字号塞进去的）");

                // ② 手牌数底板（原版 `CardsInHandText/Bg (1)`，图**也是** `40K_display`）
                Check(drv.HandPlateTex == "40K_display",
                      $"手牌数底板用的是 `40K_display`（现在 `{drv.HandPlateTex}`）");
                Check(Mathf.Abs(drv.HandPlateWorldH - 65.7f / 108f) < 0.004f,
                      $"手牌数底板高 {drv.HandPlateWorldH:F4}（原版缩放链 108×0.9259×0.009 = 0.9 → 259.3×65.7 px）");
                // 板子 259 px 宽，贴在屏幕左缘就会被切掉一半（**第一版就是这样**，截图里只剩右半边）
                {
                    var hp = drv.HandPlatePos01;
                    float halfW01 = drv.HandPlateWorldH * (442f / 112f) * 0.5f / LayoutSpace.VisibleWidth;
                    Check(hp.x - halfW01 >= -0.002f,
                          $"手牌数底板整块在屏幕内（中心 x01 {hp.x:F4} − 半宽 {halfW01:F4} = {hp.x - halfW01:F4} ≥ 0）");
                }

                // ③ 本回合已出牌数（原版 `LeftArea/CardsPlayedInTurnHolder` 里的三枚）
                Check(drv.PlayedPipTex(0) == "40k_general_bt_yellow" && drv.PlayedPipTex(2) == "40k_general_bt_yellow",
                      $"三枚「已出牌数」用的是 `40k_general_bt_yellow`（现在 `{drv.PlayedPipTex(0)}`）");
                Check(drv.PlayedPipsOn == Mathf.Min(3, drv.CardsPlayedThisTurn),
                      $"亮的枚数 = min(3, 本回合已出牌数)（现在亮 {drv.PlayedPipsOn} / 出了 {drv.CardsPlayedThisTurn}）"
                    + " —— 第 2 节打出牌之后还会再验一次「真的会亮」");

                // ④ 里程碑骷髅（原版 `LeftArea/PlayerInfo/Milestones`）
                Check(drv.SkullIconTex == "40k_battle_Win_Skull",
                      $"里程碑骷髅用的是 `40k_battle_Win_Skull`（现在 `{drv.SkullIconTex}`）");
                var sp = drv.SkullIconPos01;
                Check(Mathf.Abs(sp.x - 0.10073f) < 0.002f && Mathf.Abs(sp.y - 0.11426f) < 0.002f,
                      $"骷髅在 x01 {sp.x:F5} / y01 {sp.y:F5}"
                    + "（原版绝对中心 (193.4, 956.6) → 0.10073 / 0.11426；权威表那两行是错的，见留档）");
                Check(!string.IsNullOrEmpty(drv.SkullScoreText) && drv.SkullScoreText[0] == 'x',
                      $"里程碑分数是 `x N` 那种写法（现在 `{drv.SkullScoreText}`）");
                // 骷髅和名牌几乎重叠：同 z 会被名牌整个盖住（**第一版就是这么丢的** ——
                // 断言查不到「画没画出来」，但能钉住「它在名牌前面」这个必要条件）
                Check(drv.SkullIconZ < 0.3f,
                      $"骷髅的 z = {drv.SkullIconZ:F3} 比 HUD 图默认的 0.3 更近（同 z 就被名牌盖住了）");
                // ⑤ 名牌位置 —— 2026-09-13 更正：原来抄的是 `FrontCanvas/Alliance Panel` 底下那份
                //    **inactive** 实例的坐标（`EnemyInfo (157,108)` / `PlayerInfo (32,977)`），
                //    不是 `LeftArea` 下 HUD 那一份 → 我方偏右 44 px / 偏高 37 px、敌方偏右 ~168 px。
                //    顺带把「骷髅压住名字」也治好了（名牌下移 37 px 之后骷髅正好在文字上方）。
                var mp = drv.MyPlatePos01;
                var ep = drv.EnemyPlatePos01;
                Check(Mathf.Abs(mp.x - (-0.005938f)) < 0.002f && Mathf.Abs(mp.y - 0.060602f) < 0.002f,
                      $"我方名牌左缘中点 x01 {mp.x:F6} / y01 {mp.y:F6}（原版 −0.005938 / 0.060602 —— 贴屏幕左缘、出血 11 px）");
                Check(Mathf.Abs(ep.x - (-0.005833f)) < 0.002f && Mathf.Abs(ep.y - 0.927083f) < 0.002f,
                      $"敌方名牌左缘中点 x01 {ep.x:F6} / y01 {ep.y:F6}（原版 −0.005833 / 0.927083）");
                Check(drv.SkullBottomY01 - drv.MyPlateTextCenterY01 > 0.005f,
                      $"里程碑骷髅在名牌文字的**上方**、不压字（下沿 {drv.SkullBottomY01:F5} − 文字中心 "
                    + $"{drv.MyPlateTextCenterY01:F5} = {drv.SkullBottomY01 - drv.MyPlateTextCenterY01:F5} > 0.005）");
                // ⑥ 牌堆上的回合灯 —— 2026-09-13 更正：原来只按锚点矩形折算、**漏了 `anchoredPosition (7.9,65.8)`**，
                //    灯被摆到牌堆右下角（偏低 66 px / 偏左 8 px）。原版绝对中心 (1813.46, 985.2)。
                var lp = drv.MyDeckLightPos01;
                Check(Mathf.Abs(lp.x - 0.94451f) < 0.002f && Mathf.Abs(lp.y - 0.08778f) < 0.002f,
                      $"牌堆回合灯在 x01 {lp.x:F5} / y01 {lp.y:F5}（原版 0.94451 / 0.08778 —— 锚点矩形 + `anchoredPosition`）");
            }
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
                int landSteps = 0;
                int atSlotStep = -1;
                for (int i = 0; i < 90 && ctx.Players[0].Board[freeSlot] == null; i++)
                {
                    Step(1f / 30f);
                    landSteps++;
                    if (atSlotStep < 0 && Vector3.Distance(view.transform.position, slotPos) < 0.02f)
                        atSlotStep = landSteps;      // 卡**到格位**的那一帧（对比「引擎里那一格填上」的那一帧）
                }
                float landSec = landSteps / 30f;
                Step(0.2f);
                Debug.Log(P + $"   落位：卡到格位用了 {atSlotStep / 30f:F2}s，引擎那一格填上用了 {landSec:F2}s"
                            + $"（原版 `minionToConversionPointTime` = {DeploySequence.MoveTime:F2}s，"
                            + "差值 = DOTween 起步那一帧 + Step 粒度）");

                Check(ctx.Players[0].Board[freeSlot] != null,
                      $"拖到槽 {freeSlot} 后引擎里那一格有单位了（{ctx.Players[0].Board[freeSlot]?.Name}）");
                // 落位时长要**跟着原版字段走**：`MinionManager.minionToConversionPointTime = 0.3`
                // ⚠️ 2026-09-13 之前这里是 **0.92s** —— 那是把 `Card Hand To Board`（「2D 卡→3D 身体」的
                //    交接动画）的长度当成了「手牌飞到场位」的时长，认错了来源。
                Check(landSec <= DeploySequence.MoveTime + 0.25f,
                      $"落位 {landSec:F2}s 完成（原版 `minionToConversionPointTime` = {DeploySequence.MoveTime:F2}s；"
                      + $"卡实际在第 {atSlotStep} 帧到位，余量是 DOTween 起步帧 + Step 粒度）");
                Check(ctx.Players[0].Board[freeSlot] != null && ctx.Players[0].Board[freeSlot].Name == cardName,
                      $"上去的正是拖的那张「{cardName}」");
                Check(ctx.Players[0].Hand.Count == handBefore - 1,
                      $"手牌 {handBefore} → {ctx.Players[0].Hand.Count}");
                Check(ctx.Players[0].Energy < energyBefore,
                      $"能量 {energyBefore} → {ctx.Players[0].Energy}");
                Check(driver.HandCount == ctx.Players[0].Hand.Count,
                      $"画面手牌 {driver.HandCount} == 引擎手牌 {ctx.Players[0].Hand.Count}（旧视图销毁了）");

                // 本回合已出牌数：**真打出一张之后第一枚灯要亮**（原版 `CardsPlayedInTurn1..3`）
                Check(driver.CardsPlayedThisTurn >= 1,
                      $"打出这张牌后本回合已出牌数 = {driver.CardsPlayedThisTurn}");
                Check(driver.PlayedPipsOn == Mathf.Min(3, driver.CardsPlayedThisTurn),
                      $"已出牌数的灯亮了 {driver.PlayedPipsOn} 枚（应 = min(3, {driver.CardsPlayedThisTurn})）");
            }
            else Debug.Log(P + "   （没有付得起的牌，跳过拖拽用例）");
        }
        Step(0.3f);
        Shot(cam, "02a_拖拽上场");

        // ---- 2b. 墓地/战斗日志（原版 `CemeteryLogPanel` + `ShowCemeteryBtn`，2026-09-13）----
        // 起因：用户点名要 ① 墓地/战斗日志。引擎那边**留档**（`Ctx.ActionLog`）+ 面板这一层。
        // ⚠️ 这几条的毛病截图也看不出来：面板打不开、行是空的、日志里没有刚发生的事。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv != null)
            {
                Check(drv.CemeteryBtnTex == "40k_UI_bt_battlelog",
                      $"敌方名牌上的「看日志」按钮用的是原版那张图（现在 `{drv.CemeteryBtnTex}`）");
                var log = drv.BattleLog;
                Check(log != null && !log.Visible, "日志面板**默认是关着的**（原版要点了才划出来）");

                drv.ShowBattleLog();
                Check(log != null && log.Visible && log.RootActive, "点开之后面板真的显示出来了");
                Check(log != null && log.ShadeActive, "压暗层也跟着出来了");
                Check(log != null && log.RowsInFrontOfBg,
                      "日志的行画在底板**前面**（z 顺序反了的话底板会把行全盖住 —— 第一版就是这样）");
                Check(log != null && log.FilledRows > 0, $"日志里有内容（{log.FilledRows} 行有字）");
                Check(log != null && log.RowText(0) != null && log.RowText(0).Contains("回合"),
                      $"最新一行带着回合号：`{log.RowText(0)}`");
                // 日志里必须**真的有刚刚那一步**（上一步是真拖了一张牌上场）
                bool sawPlay = false;
                for (int i = 0; i < 8 && !sawPlay; i++)
                {
                    var t = log.RowText(i);
                    if (!string.IsNullOrEmpty(t) && (t.Contains("打出") || t.Contains("进入格位"))) sawPlay = true;
                }
                Check(sawPlay, "日志里能找到刚打出的那张牌（不是一份空壳）");
                // 字**真的画出来了**没有 —— 只看 `RowText(0)` 有值是不够的（见 `RowTextWidth` 的注释）
                Check(log.RowTextWidth(0) > 0.01f,
                      $"最新那行的文字真有宽度（{log.RowTextWidth(0):F3} 世界单位 > 0.01）—— TMP 建出字形了");
                Step(0.2f);
                Shot(cam, "02b_战斗日志");
                log.Hide();
                Check(!log.Visible, "关掉之后面板收起来了");
            }
        }

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
            int card, slot;
            if (!SimpleAI.NextPlay(ctx, out card, out slot)) break;   // 单位卡和战术卡都算
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
        // ---- 4.5 等待提示（原版 `WaitText`）----
        // 位置/尺寸照原版字段：锚 (0.5,1) + (7,−175.1)、**1344×79.4 px**、压暗 α **0.6118**
        //（出处 `runtime_ui_dump_drive_0912.tsv:350-355`）。⚠️ **触发时机与文案是我们挑的**
        //（原版查不到），理由见 `Battle/WaitBanner.cs` 文件头。
        driver.RefreshAll();                      // 批处理里没有 Update() 循环，HUD 得手动刷
        var wait = driver.Wait;
        Check(wait != null, "等待提示建起来了（原版 `WaitText`）");
        if (wait != null)
        {
            Check(wait.Visible, "★ 对手回合 → 等待提示**显示**");
            string want = CardText.Phrase("WAITING FOR OPPONENT");
            Check(wait.ShownText == want,
                  $"★ 条上写的字：「{wait.ShownText}」=「{want}」"
                + "（⚠️ 这一句是**我们加的**：原版 `Text` 节点是空的、本地化 key 没解出来）");
            // ⚠️ 尺寸断言的是 **`Generic Popup Background` 的 1323×90**（真正画出来的那个子节点），
            //    不是父容器 `WaitText` 的 1344×79.4 —— 2026-09-17 照场景 JSON 更正。
            Check(Mathf.Abs(wait.BarWorldW - 1323f / 108f) < 0.01f,
                  $"★ 提示条宽 = 原版 `Generic Popup Background` 1323 px ÷ 108 = {1323f / 108f:F3} 世界单位（实得 {wait.BarWorldW:F3}）");
            Check(Mathf.Abs(wait.BarWorldH - 90f / 108f) < 0.01f,
                  $"★ 提示条高 = 原版 90 px ÷ 108 = {90f / 108f:F3} 世界单位（实得 {wait.BarWorldH:F3}）");
            // ⚠️ **不能断言「= 9 块」**：原版这张 `40k_popup` 的上下边框各 **160 px**，而提示条只有 **90 px** 高
            //    ⇒ 中间那一行被挤没（顶/底边框按比例压扁）—— **这正是 Unity `Sliced` 的退化行为**，
            //    原版自己也是这么画的。所以判据是「角块都在（≥6）」。
            Check(wait.BarFramePieces >= 6,
                  $"★ 底板九宫格建了 {wait.BarFramePieces} 块（`40k_popup` 是 `Sliced`；提示条比它的上下边框还矮 ⇒ 6 是正常的退化）");
            Check(wait.BarFillPieces >= 1,
                  $"★ 填充层平铺了 {wait.BarFillPieces} 块（原版是 `Tiled`，128 px 一块）");
            Check(Mathf.Abs(wait.ShadeTint.a - 0.6118f) < 0.001f,
                  $"★ 整屏压暗的 α = 原版 0.6118（实得 {wait.ShadeTint.a:F4}）");
            Shot(cam, "02b_等待提示");
        }

        driver.SimulateAiTurn();                  // 对手出牌 + 攻击 + 交回来
        Step(0.3f);
        Check(ctx.Active == 0, "对手走完，回合回到我这里");
        if (wait != null) Check(!wait.Visible, "★ 回到我的回合 → 等待提示**关掉**（不是一直挂着）");
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

        // ---- 🆕 替代行动（原版 `Duty` / `Pray` / `Ferocity` / `Agenda`）在**界面上**真的能用 ----
        //   2026-09-13 A2：这四个关键词以前既没有引擎实现、也没有入口（只有我们自定的 `Ability:`）；
        //   现在引擎逐关键词做完（`RuleCore.UseAlternative`），界面上**就是那一格主动技能按钮**
        //   （原版本来也只有一格 —— 这四个在原版就是单位的主动能力）。
        Debug.Log(P + "--- 替代行动（议程 …）走界面这条路 ---");
        {
            var cAlt = driver.Ctx;
            var bikes = CardByName(CardDatabase.Load(), "Ravenwing Bikes");
            Check(bikes != null && bikes.TriggerOps("agenda") != null,
                  "卡池里有 `Ravenwing Bikes`，它的 `Agenda:` 正文收得下来");
            if (bikes != null)
            {
                int slotA = 2;
                var backup = cAlt.Players[0].Board[slotA];
                cAlt.Players[0].Board[slotA] = new UnitState(bikes, false) { Exhausted = false };
                driver.RefreshAll();

                driver.SimulateOpenCommand(slotA);
                Check(driver.HasCommand(AttackKind.Ability),
                      "★ 带 `Agenda` 的单位**给出了主动技能那一格**"
                      + "（没有引擎实现的话这里根本不会亮）");
                int questBefore = cAlt.Players[0].QuestPoints;
                driver.SimulateCommand(AttackKind.Ability);
                Check(cAlt.Players[0].QuestPoints == questBefore + 1,
                      "★ 点下去**真的结算了议程的正文**"
                      + $"（任务点 {questBefore} → {cAlt.Players[0].QuestPoints}）");
                Check(cAlt.Players[0].Board[slotA].Exhausted, "花掉了这个单位本回合的行动");

                driver.RefreshAll();
                driver.SimulateOpenCommand(slotA);
                Check(!driver.HasCommand(AttackKind.Ability),
                      "★ 同一回合不能再点第二次（行动额度只有一次，规则书 `:150`）");

                cAlt.Players[0].Board[slotA] = backup;      // 靶场还原
                driver.RefreshAll();
                driver.SimulateDeselect();
            }
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
            // 名牌上那行 `x N` 和结算面板必须是**同一份判据**（`DeckRules.SkullsFor`）——
            // 两处各算各的迟早不一致，而「名板一个数、结算另一个数」是最难被发现的那种错
            Check(driver.SkullScoreText == "x" + end.ShownSkulls,
                  $"名牌里程碑 `{driver.SkullScoreText}` == 结算面板的 {end.ShownSkulls} 个骷髅");
        }
        // ---- 结算「开门」视频（原版 `EndBattleDoors`；资产在 `Resources/Art/videos/`）----
        // 反编译 `SetupDoor` 返回 `VideoClip.length`、调用方拿它 WaitForSeconds —— 这里就对这条约定对账：
        // **推够片长之前内容层不能揭晓，推够了必须揭晓**。
        var doors = end == null ? null : end.Doors;
        Check(doors != null, "开门那层建出来了");
        if (doors != null)
        {
            if (doors.HasVideo)
            {
                Check(doors.Playing, $"结算时视频在播（`{doors.Clip}`，片长 {doors.Length:F2}s）");
                Check(doors.ScreenVisible, "视频那层是显示着的");
                // 判据：`EndBattleDoors.SetupDoor` 自己就调了 `ShowRewards` —— **奖励跟视频同时出**，
                // 不是播完才揭晓（第一版按「播完揭晓」做的，读了反编译之后改掉了）
                Check(end.ContentVisible, "奖励和视频**同时**出现（原版 SetupDoor 里就是直接 ShowRewards）");
                Check(!end.TitleVisible, "结果文字藏着 —— 那段视频自己带 VICTORY/DEFEAT/DRAW 字样，原版也没有结果文字节点");

                float t = 0f;
                while (doors.Playing && t < 20f) { Step(1f / 30f); t += 1f / 30f; }

                Check(!doors.Playing, $"视频播完了（这一轮又推了 {t:F2}s）");
                // `Elapsed` 会被 `Advance` 夹在片长上 —— 正好等于片长才说明「等够了、也没多等」。
                // ⚠️ 别拿循环里的 t 和片长比：`Show` 之后到这儿之间已经推过别的 dt 了（这里 0.2s），
                //    第一版就是这么写的，差 0.19s 蒙混过关。
                Check(Mathf.Abs(doors.Elapsed - doors.Length) < 0.001f,
                      $"播到的位置正好 = 片长（{doors.Elapsed:F2}s / {doors.Length:F2}s）");
                Check(doors.ScreenVisible, "播完视频那层还在（留最后一帧当底图，不是播完就撤）");
            }
            else
            {
                // 走到这里说明 `Resources/Art/videos/` 没放视频（那目录 gitignore，删了就是这样）
                Check(end.ContentVisible, "没有视频资产 → 内容照常显示（不卡住、不静默等）");
                Check(end.TitleVisible, "没有视频时结果文字是我们自己画的（有视频时那是视频里的字）");
                Debug.Log(P + "   ⚠️ 没放开门视频（`Resources/Art/videos/`），这条只验了「没有也能走通」");
            }
        }

        string who = ctx.Winner == 3 ? "平局" : (ctx.Winner == 1 ? "我（Ember）胜" : "对手（Tide）胜");
        Debug.Log(P + $"   {who}；我督军剩 {Mathf.Max(0, ctx.Players[0].Warlord.Health)}，"
                    + $"对手督军剩 {Mathf.Max(0, ctx.Players[1].Warlord.Health)}");
        ClearEffects();
        Shot(cam, "05_对局结束");

        // ---- 8. 原版卡牌接进对战（Ultramarines vs Goff，2026-09-12）----
        // ⚠️ 上面几节跑的都还是**我们自己设计**的那 26 张（`StarterCards`）—— 那套是专门为覆盖
        //    关键词/触发设计的，`RuleEngineTest` 里那批规则用例靠它们，所以**留着**（而且它是
        //    「卡池可以换」这件事的活证明）。这一节才是「原版 1131 张真的打进一局」：
        //    **不带参数** `Begin()` = 默认两个原版阵营 + 从原版卡池自动凑牌。
        Debug.Log(P + "--- 原版卡牌接进对战（Ultramarines vs Goff）---");
        {
            // ⚠️ 这里必须**显式传**阵营：`Begin` 只在参数非 null 时覆盖字段（那是对的行为 ——
            //    重开一局不该把阵营悄悄改回默认），而上一节已经把字段设成 Ember/Tide 了。
            //    「按 Play 时用哪两个」是另一回事：`Start()` 调的是无参 `Begin()`，
            //    那个用的是字段初值 = 下面这两条常量（新实例才会走初值）。
            Check(BattleDriver.DefaultFactionA == "Ultramarines"
                  && BattleDriver.DefaultFactionB == "Goff",
                  $"无参 Begin() 的默认阵营 = {BattleDriver.DefaultFactionA} vs {BattleDriver.DefaultFactionB}"
                  + "（打开 Battle.unity 按 Play 打的就是这两个）");

            driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20260912);
            Step(0.3f);
            var c2 = driver.Ctx;

            Check(driver.MyFaction == BattleDriver.DefaultFactionA
                  && driver.FoeFaction == BattleDriver.DefaultFactionB,
                  $"阵营 = {driver.MyFaction} vs {driver.FoeFaction}");
            Check(c2.Players[0].Warlord.Card.FromOriginalPool,
                  $"我方督军「{c2.Players[0].Warlord.Name}」来自原版卡池");
            Check(c2.Players[0].Warlord.Card.Faction == driver.MyFaction, "督军阵营和写的一致");

            int handPool = 0, deckPool = 0, foeDeckPool = 0;
            foreach (var card in c2.Players[0].Hand) if (card.FromOriginalPool) handPool++;
            foreach (var card in c2.Players[0].Deck) if (card.FromOriginalPool) deckPool++;
            foreach (var card in c2.Players[1].Deck) if (card.FromOriginalPool) foeDeckPool++;
            Check(handPool == c2.Players[0].Hand.Count,
                  $"手牌 {handPool}/{c2.Players[0].Hand.Count} 张来自原版卡池");
            Check(deckPool == c2.Players[0].Deck.Count && foeDeckPool == c2.Players[1].Deck.Count,
                  $"双方抽牌堆全部来自原版卡池（我 {deckPool} / 对手 {foeDeckPool}）");

            // 卡面：中文名 + 卡自己的效果原文（原版卡面就是这两样）
            var v0 = driver.HandViewAt(0);
            var h0 = c2.Players[0].Hand[0];
            Check(v0 != null && v0.Data.title == h0.NameZh,
                  $"手牌第一张卡面用**中文名**「{(v0 == null ? "?" : v0.Data.title)}」"
                  + $"（英文 {h0.Name}）");
            Check(v0 != null && !string.IsNullOrEmpty(v0.Data.keywords),
                  $"卡面效果行：「{Short(v0 == null ? "" : v0.Data.keywords, 34)}」");
            Debug.Log(P + "   手牌：" + driver.HandViewNames());

            // ---- 卡牌放大展示窗（原版 `CardDisplayWindow`；轻点卡牌开关）----
            // 判据来自反编译 `BasicCardUI.ToggleOpenCardDisplayOnTouch` —— **轻点**就是开关。
            {
                var cdw = driver.CardDisplay;
                Check(cdw != null, "卡牌展示窗建出来了");
                var view = driver.HandViewAt(0);

                void Tap()
                {
                    it.SimulateHover(view.transform.position);
                    Step(0.05f);
                    it.SimulatePress(view.transform.position);
                    Step(0.05f);
                    it.SimulateRelease(view.transform.position);      // 原地松开 = 轻点
                    Step(0.1f);
                }

                Tap();
                Check(cdw.Visible, "轻点手牌 → 展示窗打开（原版 `ToggleOpenCardDisplayOnTouch`）");
                Check(cdw.ShownTitle == h0.NameZh, $"窗里就是这张卡：{cdw.ShownTitle}");
                Check(!string.IsNullOrEmpty(cdw.ShownBody),
                      $"下面写着它的效果：「{Short(cdw.ShownBody, 26)}」");
                float bigH = cdw.Card == null ? 0f
                           : CardView.Height * cdw.Card.transform.localScale.y * 108f;
                Check(Mathf.Abs(bigH - CardDisplayWindow.CardHeightPx) < 2f,
                      $"放大卡高 {bigH:F0} px = 原版的 {CardDisplayWindow.CardHeightPx:F0} px");
                Shot(cam, "11_卡牌展示窗");

                Tap();
                Check(!cdw.Visible, "再轻点一次 → 关掉");
            }

            // 真拖一张上场（和上面那条一样的鼠标路径，不是直接调 SimulatePlay）
            int dragIdx = -1;
            for (int i = 0; i < c2.Players[0].Hand.Count; i++)
                if (c2.Players[0].Hand[i].Cost <= c2.Players[0].Energy) { dragIdx = i; break; }

            if (dragIdx >= 0)
            {
                var view = driver.HandViewAt(dragIdx);
                int freeSlot = SimpleAI.FirstFreeSlot(c2.Players[0]);
                var slotPos = pBoard.SlotPosition(freeSlot);
                it.SimulateHover(view.transform.position);
                Step(0.05f);
                it.SimulatePress(view.transform.position);
                Step(0.2f);
                for (int i = 0; i < 24; i++) { it.SimulateDrag(slotPos, 1f / 30f); Step(1f / 30f); }
                it.SimulateRelease(slotPos);
                for (int i = 0; i < 90 && c2.Players[0].Board[freeSlot] == null; i++) Step(1f / 30f);
                Step(0.2f);
                Check(c2.Players[0].Board[freeSlot] != null,
                      $"原版卡拖上场成功：{c2.Players[0].Board[freeSlot]?.Name} → 槽 {freeSlot}");
                // ⚠️ 这条同时是 `CardInteraction._placed` 那个坑的回归测试：
                //    上面第 2 节已经往场上摆过牌，`_placed` 里还留着那些槽号。
                //    `Begin()` 不清它的话，这一张会**弹回手上**（2026-09-12 就是这么撞出来的）。
                Check(it.Placed.Count >= 1, $"落位登记被新一局接管了（Placed {it.Placed.Count} 个）");
            }
            else Debug.Log(P + "   （开局手牌都付不起，跳过拖拽）");

            // 「按 R 再来一局」—— 面板上写着这句话，原来**没有代码接**。这里验它真的能重开。
            var beforeCtx = c2;
            driver.Restart();
            Step(0.3f);
            var c3 = driver.Ctx;
            Check(!ReferenceEquals(c3, beforeCtx), "重开一局：引擎上下文换新的了");
            // ⚠️ 第三十四轮：回合 1 的 `BeginTurn` 里**天赋**会从卡池现拿一张塞进手牌
            //    （`RuleCore.SpawnTalents`）—— 它**不属于起手**，要单独算进来，不然这里差 1 张。
            Check(c3.Turn == 1
                  && c3.Players[0].Hand.Count == RuleCore.StartHand + 1 + Conjured(c3, 0),
                  $"重开一局回到第 1 回合、手牌 {c3.Players[0].Hand.Count} 张"
                  + $"（起手 {RuleCore.StartHand} + 抽 1 + 天赋生成的 {Conjured(c3, 0)} 张）");
            Check(c3.Players[0].Warlord.Card.FromOriginalPool, "重开之后还是原版卡池");

            // ⚠️ 重开**不能叠份**：`BuildHud` 每调一次就建一套，HUD 会叠两层、
            //    而且旧的那个 `EndPanel` 会成孤儿（`_endPanel` 指向新建的那个，旧的永远不 Hide）。
            //    这条是截图抓出来的 —— 新一局已经开打，上一局的「对局结束/骷髅/按R再来一局」还压着。
            int turnLabels = 0;
            foreach (var t in driver.GetComponentsInChildren<Transform>(true))
                if (t.name == "TurnLabel") turnLabels++;
            Check(turnLabels == 1, $"重开之后 HUD 只有一份（TurnLabel ×{turnLabels}）");
            Check(driver.End != null && !driver.End.Visible, "重开之后上一局的结算面板收掉了");
            Shot(cam, "09_原版卡_开局");

            // 对手用**同一套卡池**连打几轮 —— 只打一回合的话对手可能手牌全付不起，看不出东西
            int foeUnits = 0;
            for (int round = 0; round < 4 && !c3.IsOver; round++)
            {
                driver.SimulateAiTurn();
                StepThrough(driver);
                Step(0.3f);
                foeUnits = 0;
                for (int s = 0; s < BoardSpec.Size; s++)
                    if (c3.Players[1].Board[s] != null && !c3.Players[1].Board[s].IsWarlord) foeUnits++;
                Debug.Log(P + $"   AI 第 {round + 1} 轮打完：场上单位 {foeUnits} 个，"
                            + $"能量 {c3.Players[1].Energy}，手牌 {c3.Players[1].Hand.Count} 张");
                if (foeUnits > 0) break;
            }
            Check(foeUnits > 0, $"AI 真的用原版卡池出牌了（场上 {foeUnits} 个单位）");
            Shot(cam, "10_原版卡_AI回合");
            ClearEffects();
        }


        // ---- 8b. 卡框按稀有度分档（原版 tier1–4，2026-09-12 加）----
        // 稀有度→tier 的对应是**实测**出来的（四张不同稀有度的原版卡面逐一比对），
        // 见 `工具/import_original_art.py` 的 `RARITY_TIER` 与 `资料/留档_排查证据/卡面组装_0912/tier_map.png`。
        Debug.Log(P + "--- 卡框分档（稀有度）---");
        {
            var f1 = CardArt.Frame("Ultramarines", "common");
            var f2 = CardArt.Frame("Ultramarines", "rare");
            var f3 = CardArt.Frame("Ultramarines", "epic");
            var f4 = CardArt.Frame("Ultramarines", "legendary");
            Check(f1 != null && f2 != null && f3 != null && f4 != null,
                  $"四档卡框都导进来了（{f1?.name} / {f2?.name} / {f3?.name} / {f4?.name}）");
            Check(f1 != f2 && f2 != f3 && f3 != f4 && f1 != f4, "四档拿到的**不是同一张**图");
            Check(f1 != null && f1.name.Contains("tier1") && f4 != null && f4.name.Contains("tier4"),
                  "common→tier1、legendary→tier4（映射是对照原版卡面实测的，不是猜的）");
            Check(CardArt.Frame("Ultramarines", "没这个稀有度") == f1
                  && CardArt.Frame("Ultramarines", null) == f1, "不认识的稀有度退回 tier1");
            // 战术卡**另一套框**（原版 troop / stratagem 分开；战术卡那张下半截是大片文字区）
            var ft = CardArt.Frame("Ultramarines", "legendary", true);
            Check(ft != null && ft.name.Contains("strat") && ft != f4,
                  $"战术卡用的是**另一套框**（{ft?.name} ≠ {f4?.name}）");

            // 真造一张卡，验**卡面确实换了框**（不是只有 `CardArt` 会取）
            var probe = CardView.Create(cam.transform, new CardData
            {
                id = "Aggressor Sergeant", title = "TEST", cost = 1, melee = 1, ranged = 0,
                // ⚠️ 立绘按**引擎卡 id** 取名（2026-09-15 起）—— 这里必须给 artId，
                //    只给 `id`（卡名）会取不到图（`CardArt.Portrait` 的键是 id）。
                artId = "UM_Aggressor_Sergeant",
                health = 1, armor = 0, keywords = "", isUnit = true,
                frame = BattleDriver.FactionColor("Ultramarines"), faction = "Ultramarines",
                rarity = "legendary",
            }, "FrameTierProbe");
            Check(probe.FrameTexture != null && probe.FrameTexture.name.Contains("tier4"),
                  $"legendary 的卡真的用了 tier4 框（{probe.FrameTexture?.name}）");
            Check(probe.ArtLayerTexture != null && probe.ArtLayerTexture.name.StartsWith("art_"),
                  $"立绘那层用的是**真插图**（{probe.ArtLayerTexture?.name}）");
            // 底部那颗**稀有度宝石**：卡框分档改的是框的形制，颜色在这颗宝石上（原版 `Rarity` 节点）
            Check(probe.GemTexture != null && probe.GemTexture.name.Contains("legendary"),
                  $"底部稀有度宝石用了 legendary 那张（{probe.GemTexture?.name}）");
            var probe2 = CardView.Create(cam.transform, new CardData
            {
                id = "Aggressor Sergeant", title = "TEST2", cost = 1, melee = 1, ranged = 0,
                health = 1, armor = 0, keywords = "", isUnit = true,
                frame = BattleDriver.FactionColor("Ultramarines"), faction = "Ultramarines",
                rarity = "common",
            }, "GemTierProbe");
            Check(probe2.GemTexture != null && probe2.GemTexture.name.Contains("common")
                  && probe2.GemTexture != probe.GemTexture,
                  $"common 用的是另一张（{probe2.GemTexture?.name}）");
            if (Application.isPlaying) Object.Destroy(probe2.gameObject);
            else Object.DestroyImmediate(probe2.gameObject);
            if (Application.isPlaying) Object.Destroy(probe.gameObject);
            else Object.DestroyImmediate(probe.gameObject);
        }

        // ---- 9. 卡组编辑器编的那套牌 → 对战（2026-09-12 接的最后一环）----
        // 第 8 节验的是「自动凑一副原版牌也能打」；这一节验**玩家自己编的那副**能不能真的打进对局 ——
        // 也就是 `DeckLibrary` → `Begin(myDeck:)` 这条调用点，以及「不许静默替换」这条红线。
        // ⚠️ 存档必须**隔离**（`DeckStore.OverridePath` 指到临时文件），绝不碰玩家的真存档 ——
        //    做法和 `DeckScene.cs` 那一段一样。
        Debug.Log(P + "--- 卡组编辑器编的那套牌 ---");
        {
            string tmpStore = System.IO.Path.Combine(OutDir, "decks_selftest.json");
            var pool9 = CardDatabase.Load();
            string savedOverride = DeckStore.OverridePath;
            DeckStore.OverridePath = tmpStore;
            try
            {
                if (File.Exists(tmpStore)) File.Delete(tmpStore);

                // ① 一副都没编过 → 走自动凑，且**没什么要交代的**（那是默认行为）
                var none = BattleDriver.PickSavedDeck(out _);
                Check(none == null, "存档里一副卡组都没有 → PickSavedDeck 给 null（走自动凑）");
                driver.BeginFromDeckLibrary();          // 按 Play 走的就是这一条
                Step(0.3f);
                Check(driver.DeckNotice == "" && driver.HintText == "",
                      "自动凑的时候提示行是空的（默认行为不用跟玩家交代）");

                // ② 合法卡组：**真的用上了**；战术卡 2026-09-12 起**能打了** ——
                //    能解析干净的收下，解析不了的照样丢并在提示行说清张数
                const int tactics = 12;         // 故意塞 12 张战术卡
                var legal = MakeDeck(pool9, "Ultramarines", tactics, "自检·极限战士");
                var libA = DeckLibrary.Load();
                libA.Add(legal);                // 落盘，并设为「当前选中」
                Check(BattleDriver.PickSavedDeck(out _)?.Name == legal.Name,
                      $"从卡组库里读出当前选中那套：「{legal.Name}」");

                driver.BeginFromDeckLibrary();  // ★ 走**按 Play 时那条路**，不是手工传卡组
                Step(0.3f);
                var c9 = driver.Ctx;
                var src9 = driver.MyDeckSource;
                var warlord9 = CardDatabase.FindById(pool9, legal.WarlordId);
                Check(src9 != null && src9.Name == legal.Name,
                      $"开局真的读了卡组库：「{(src9 == null ? "null" : src9.Name)}」");
                Check(driver.MyFaction == "Ultramarines" && warlord9 != null
                      && warlord9.Faction == driver.MyFaction,
                      $"我方阵营跟着**督军**走 = {driver.MyFaction}（督军 {warlord9?.Name}）");
                Check(c9.Players[0].Warlord.Name == warlord9.Name,
                      $"带队的督军就是编的那张：「{c9.Players[0].Warlord.Name}」");

                // 塞进去的那 12 张里，几张能解析？（能解析的会被收下 → 上场牌数跟着变）
                int tacKept = 0, tacDropped = 0;
                foreach (var id in legal.CardIds)
                {
                    var cc = CardDatabase.FindById(pool9, id);
                    if (cc == null || cc.Type != "tactic") continue;
                    if (DeckBuilder.TacticPlayable(cc)) tacKept++; else tacDropped++;
                }
                // ⚠️ 第三十四轮：天赋从卡池现拿的那几张**不属于卡组** ⇒ 账要扣掉（见 `Conjured`）。
                int conj9 = Conjured(c9, 0);
                int inPlay = c9.Players[0].Hand.Count + c9.Players[0].Deck.Count - conj9;
                // ⚠️ 2026-09-13 第三十三轮：**防御卡现在也上场**（开局就在手里，见 `RuleCore.BuildPlayer`）
                //    ⇒ 账要多一张。这张断言以前是 `CardCount - tacDropped`，防御卡进来后就成了 30-2 而不是 29。
                int wantUnits = DeckRules.CardCount(false) - tacDropped + 1;   // +1 = 防御卡
                Check(inPlay == wantUnits,
                      $"上场的牌 = 编的 30 张 − {tacDropped} 张解析不了的战术 + 1 防御 = {inPlay} 张（应 {wantUnits}）"
                      + (conj9 > 0 ? $"（已扣掉天赋凭空生成的 {conj9} 张）" : ""));
                Check(c9.Players[0].Hand.Exists(x => x != null && x.Type == "defence"),
                      "防御卡真的在手里（第三十三轮起它上场了，以前是被丢掉）");
                Check(tacKept > 0, $"战术卡留下了 {tacKept} 张（能解析的现在能打了，不是全丢）");
                int tacInPlay = 0;
                foreach (var card in c9.Players[0].Hand) if (card.Type == "tactic") tacInPlay++;
                foreach (var card in c9.Players[0].Deck) if (card.Type == "tactic") tacInPlay++;
                // ⚠️ **天赋生成的也是 `tactic`** ⇒ 按**份数**减掉。
                //    别改成「逐张判 `MarkedEphemeralCount(card) == 0`」——
                //    `CardDef` 是**共享模板**，卡组里那张同名卡会被**一起**判成生成的（多减一张）。
                tacInPlay -= conj9;
                Check(tacInPlay == tacKept,
                      $"收下的战术卡真的在牌里（手牌 + 牌库 {tacInPlay + conj9} 张 − 天赋 {conj9} = "
                      + $"{tacInPlay} 张，应 {tacKept}）");

                Check(driver.DeckNotice.Contains(legal.Name),
                      $"提示行说了用的是哪副牌：「{Short(driver.DeckNotice, 44)}」");
                // ⚠️ **2026-09-16 修：这条断言原来写的是 `Contains(tacDropped + " 张")` —— 脆的。**
                //    它靠两个巧合活着：① `tacDropped = 0` 时靠提示行里别的数字（`10 张`/`20 张`）
                //    **含子串 `0 张`** 蒙混通过；② 张数凑巧不是别人的子串。
                //    实测：把 `Angels of Death` / `Self-Destruction` 修好之后**掉 0 张**，
                //    提示行正确地**不再带那一句**，这条立刻变红 —— 而行为是**变好**了。
                //    ⇒ 改成按分支精确比对**那句话本身**。
                Check(tacDropped == 0
                        ? !driver.DeckNotice.Contains("没上场")
                        : driver.DeckNotice.Contains($"{tacDropped} 张（效果本版解析不了的战术卡）没上场"),
                      $"丢掉的张数也说清了（{tacDropped} 张解析不了的战术 —— 防御卡**不再**算丢）");
                // ⚠️ 2026-09-13 加：**提示行不许再提「防御卡」**。
                //    上一次改 `FromDeck` 收防御卡时忘了改这句文案 —— 截图里防御卡明明在手上，
                //    提示行还在说「防御卡…没上场」（**对玩家说错话比不说更糟**）。
                //    这条断言把「文案」和「取舍」钉在一起，以后再改就不会漏。
                Check(!driver.DeckNotice.Contains("防御卡"),
                      $"提示行**不再**说防御卡没上场（「{Short(driver.DeckNotice, 40)}」）");
                Check(driver.HintText == driver.DeckNotice,
                      "那句就写在提示行上 —— 开局就看得见，不用去翻日志");
                Shot(cam, "12_玩家编的卡组");

                // ③ 「按 R 再来一局」**不能把卡组弄丢**（原来 Restart 不带卡组，重开就变自动凑了）
                driver.Restart();
                Step(0.3f);
                var c9b = driver.Ctx;
                Check(ReferenceEquals(driver.MyDeckSource, src9), "重开一局：还是这副牌");
                Check(c9b.Players[0].Warlord.Name == warlord9.Name
                      && c9b.Players[0].Hand.Count + c9b.Players[0].Deck.Count
                         == wantUnits + Conjured(c9b, 0),
                      "重开一局：引擎里也还是编的那副（没悄悄退回自动凑）"
                      + $"，牌数 {wantUnits} + 天赋 {Conjured(c9b, 0)} 对得上");

                // ④ 不合法的卡组 → **明说**原因再退回，不能装作打的就是你那副
                var bad = MakeDeck(pool9, "Ultramarines", tactics, "自检·缺防御卡");
                bad.DefensiveId = null;                       // 规则书:47 要 1 张防御卡
                driver.Begin(seed: 20260915, myDeck: bad);
                Step(0.3f);
                Check(driver.DeckNotice.Contains("不合法") && driver.DeckNotice.Contains("退回自动凑"),
                      $"不合法的卡组明说再退回：「{Short(driver.DeckNotice, 44)}」");
                Check(!driver.DeckNotice.Contains("本局用你编的"),
                      "退回时**不会**说成「用你编的」（说了就是骗玩家）");
                Check(driver.Ctx.Players[0].Hand.Count + driver.Ctx.Players[0].Deck.Count
                      == DeckBuilder.ClassicDeckSize - 1 + Conjured(driver.Ctx, 0),
                      $"退回的确实是自动凑的一副"
                      + $"（{driver.Ctx.Players[0].Hand.Count + driver.Ctx.Players[0].Deck.Count}"
                      + $" 张 = {DeckBuilder.ClassicDeckSize} − 督军 + 天赋生成的 {Conjured(driver.Ctx, 0)} 张，"
                      + $"不是编的那 {wantUnits} 张）");

                // ⑤ 全是战术卡 → 2026-09-12 起**能解析的都收下**（不再「只剩督军」）；
                //    解析不了的照样丢并说清张数。顺带用**超长卡组名**试截断
                //   （`Label` 是 NoWrap 的，名字长了会横着铺出屏幕）
                var longName = "自检·一副名字特别长的、全是战术卡的卡组（专门用来试截断的）";
                var allTactic = MakeDeck(pool9, "Ultramarines", DeckRules.CardCount(false), longName);
                int allDropped = 0;
                foreach (var id in allTactic.CardIds)
                {
                    var cc = CardDatabase.FindById(pool9, id);
                    if (cc != null && cc.Type == "tactic" && !DeckBuilder.TacticPlayable(cc)) allDropped++;
                }
                driver.Begin(seed: 20260916, myDeck: allTactic);
                Step(0.3f);
                // ⚠️ 2026-09-16 一并改成按分支精确比对（同上一条：`Contains(allDropped + " 张")`
                //    靠子串巧合通过，掉 0 张时提示行合理地没有那一句 ⇒ 断言误报成红）。
                Check(driver.DeckNotice.Contains("本局用你编的")
                      && (allDropped == 0
                            ? !driver.DeckNotice.Contains("没上场")
                            : driver.DeckNotice.Contains($"{allDropped} 张（效果本版解析不了的战术卡）没上场")),
                      $"全是战术卡：能解析的收下、{allDropped} 张解析不了的说明白"
                      + "（防御卡第三十三轮起不再算丢）"
                      + $"（「{Short(driver.DeckNotice, 44)}」）");
                Check(driver.DeckNotice.Contains("…") && !driver.DeckNotice.Contains(longName),
                      "超长卡组名被截断了（没整段塞进提示行）");
                Check(driver.HintWidth > 0f && driver.HintWidth < LayoutSpace.VisibleWidth * 0.9f,
                      $"那句没铺出屏幕（宽 {driver.HintWidth:F2} / 可见 {LayoutSpace.VisibleWidth:F2} 世界单位）");

                // ⑥ 玩家自己选的就是 Goff → 对手让开，别开局先打一场内战
                //   （要先显式把对手字段压成 Goff，不然验不到「撞上了」这个分支）
                var goff = MakeDeck(pool9, "Goff", 4, "自检·兽人");
                driver.Begin("Goff", "Goff", 20260917);
                driver.Begin(seed: 20260918, myDeck: goff);
                Step(0.3f);
                Check(driver.MyFaction == "Goff" && driver.FoeFaction != "Goff",
                      $"玩家选 Goff 时对手换人（不是内战）：{driver.MyFaction} vs {driver.FoeFaction}");

                // ⑦ 读的是**当前选中**的那套，不是第一套
                var libB = DeckLibrary.Load();
                var second = MakeDeck(pool9, "Goff", 2, "自检·第二套");
                libB.Add(second);                              // Add 会把它设为当前选中
                Check(BattleDriver.PickSavedDeck(out _)?.Name == second.Name,
                      $"读的是当前选中那套：「{second.Name}」");
                libB.Select(0);
                libB.Save();                                   // `Select` 自己不落盘（改的是内存里的下标）
                Check(BattleDriver.PickSavedDeck(out _)?.Name == legal.Name,
                      $"换选一套之后读到的也换了：「{legal.Name}」");

                // ⑧ 存档坏掉 → **不能装作没事**（说不出卡组就直说，别让玩家以为打的是自己那副）
                File.WriteAllText(tmpStore, "{ 这不是 json");
                string note2;
                var broken = BattleDriver.PickSavedDeck(out note2);
                Check(broken == null && !string.IsNullOrEmpty(note2),
                      $"存档坏了：读不出卡组，但带回一句人话「{Short(note2, 26)}」");
                driver.Begin(seed: 20260919, myDeck: broken, deckNote: note2);
                Step(0.3f);
                Check(driver.DeckNotice.Contains("读不出来"),
                      $"存档坏了会在提示行上说：「{Short(driver.DeckNotice, 44)}」");
                ClearEffects();
            }
            finally
            {
                DeckStore.OverridePath = savedOverride;        // 玩家的真存档路径还回去
                if (File.Exists(tmpStore)) File.Delete(tmpStore);
            }
        }

        // ---- 10. 战术卡：**真的用鼠标拖出去打**（端到端）----
        // 第 9 节验的是「战术卡进得了牌组」、`RuleEngineTest` 验的是「引擎打得出去」——
        // 中间那段**拖拽路径**（`CardInteraction.ResolveDrop` 认不认敌方半场、
        // `OnCardDeployed` 走不走战术分支）只有真拖一次才算验过。
        Debug.Log(P + "--- 战术卡：鼠标拖出去打 ---");
        {
            var poolT = CardDatabase.Load();
            CardDef pickT = null;
            foreach (var c in poolT)
            {
                if (c == null || c.Type != "tactic" || c.Cost > 3) continue;
                if (!DeckBuilder.TacticPlayable(c)) continue;
                var opsT = EffectText.Parse(c.Desc, out _, out _);
                if (EffectText.PickSide(opsT) != "enemy") continue;
                bool allDeal = opsT.Count > 0;
                foreach (var o in opsT) if (o.Verb != "deal") allDeal = false;
                if (!allDeal) continue;
                pickT = c; break;
            }
            Check(pickT != null, "找到一张「只打敌方单位」的低费原版战术卡");

            var dkT = pickT == null ? null : DeckWithTactic(poolT, pickT.Faction, pickT.Name);
            Check(dkT != null, pickT == null ? "（没挑到卡）" : $"凑出一副带「{pickT.Name}」的合法卡组");
            if (dkT != null)
            {
                CardTween.Mode = DG.Tweening.UpdateType.Manual;
                // ⚠️ `NewBattle` 默认**洗牌**（种子定死 → 同一 seed 可复现，但那张战术卡**第几回合**
                //    抽到是跟着 seed 变的）。所以这里**逐个种子试**：抽到手 + 能量够就开打。
                //    不写死一个 seed 是因为牌序随卡组内容变 —— 写死了下次改卡池这条就红。
                bool ready = false;
                int tacIdx = -1, tgt = -1;
                BattleContext cT = null;
                for (int attempt = 0; attempt < 8 && !ready; attempt++)
                {
                    driver.Begin(seed: 20260921 + attempt, myDeck: dkT);
                    Step(0.3f);
                    cT = driver.Ctx;
                    for (int round = 0; round < 14 && !cT.IsOver; round++)
                    {
                        tacIdx = -1;
                        for (int i = 0; i < cT.Players[0].Hand.Count; i++)
                            if (cT.Players[0].Hand[i].Name == pickT.Name) { tacIdx = i; break; }
                        if (tacIdx >= 0 && cT.Players[0].Energy >= pickT.Cost) { ready = true; break; }
                        driver.SimulateEndTurn();
                        driver.SimulateAiTurn();
                        Step(0.3f);
                    }
                }
                Check(ready, $"试了几个种子之后，「{pickT.Name}」到手且付得起（第 {cT.Turn} 回合）");

                if (ready)
                {
                    // 挑个敌方目标：优先部队；没有就督军（`an enemy` **含督军** —— 引擎自检里钉过）
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var u = cT.Players[1].Board[s];
                        if (u != null && !u.IsWarlord) { tgt = s; break; }
                    }
                    if (tgt < 0) tgt = BoardSpec.WarlordSlot;
                    var tgtUnit = cT.Players[1].Board[tgt];
                    int hpBefore = tgtUnit.Health;
                    int handBefore = cT.Players[0].Hand.Count;
                    int energyBefore = cT.Players[0].Energy;

                    var view = driver.HandViewAt(tacIdx);
                    var tgtPos = eBoard.SlotPosition(tgt);
                    it.SimulateHover(view.transform.position);
                    Step(0.05f);
                    it.SimulatePress(view.transform.position);
                    Step(0.2f);
                    for (int i = 0; i < 24; i++) { it.SimulateDrag(tgtPos, 1f / 30f); Step(1f / 30f); }
                    it.SimulateRelease(tgtPos);
                    // 打出动画走完才触发 `OnDeployed`（引擎调用在回调里）—— 推到「手牌真的少一张」
                    for (int i = 0; i < 90 && cT.Players[0].Hand.Count >= handBefore; i++) Step(1f / 30f);
                    Step(0.2f);

                    Check(cT.Players[0].Hand.Count == handBefore - 1,
                          $"战术卡离开了手牌（{handBefore} → {cT.Players[0].Hand.Count}）");
                    Check(cT.Players[0].Energy < energyBefore,
                          $"扣了费（{energyBefore} → {cT.Players[0].Energy}）");
                    Check(cT.Players[0].Discard.Count > 0, "进了弃牌堆");
                    Check(tgtUnit.Health < hpBefore,
                          $"目标真的挨了打（{tgtUnit.Name} {hpBefore} → {tgtUnit.Health}）");
                    Check(driver.HandCount == cT.Players[0].Hand.Count,
                          $"画面手牌 {driver.HandCount} == 引擎手牌 {cT.Players[0].Hand.Count}（视图销毁了）");
                    Shot(cam, "13_战术卡打出去");
                }
            }
        }

        // ---- 11. 造牌 / 免费部署：**画面真的跟上了没有**（定点驱动）----
        // 第 10 节验的是「拖得出去、引擎结算了」；引擎那边 `RuleEngineTest` 查得很密
        // （手牌张数 / 场上槽位 / 池子内容 / 同种子可复现）。**中间那段一直没人验**：
        // 造出来的牌**画面会不会给它建视图**（`SyncHand` 按名字配、配不上的新建）、
        // 部署出来的单位**画面上有没有那一格**。这一节就是补这一段 ——
        // 判据是 `driver.HandCount == 引擎手牌张数`（视图和引擎不同步时这两个数会分叉）。
        Debug.Log(P + "--- 造牌 / 免费部署：画面跟上没有 ---");
        {
            var poolC = CardDatabase.Load();

            // ① 造牌：`Mercurial Host`（帝皇之子 4 费）→ `Create 3 random Combat Elixir in your hand`
            DriveTacticAndCheck(driver, it, cam, poolC, "Mercurial Host", pBoard, eBoard, "14_造牌_3张战斗药剂",
                Check, (c, before) =>
                {
                    // ⚠️ **手牌上限**（`RuleCore.HandMax` = 10）：超出的直接进弃牌堆。
                    //    2026-09-13 第三十三轮防御卡进手牌之后起手多一张 ⇒ 这条链**正好撞上限**，
                    //    所以净增不是 +2 而是「夹到上限」，3 张药剂里有 1 张直接进了弃牌堆。
                    int expectHand = System.Math.Min(RuleCore.HandMax, before.Hand + 2);
                    Check(c.Players[0].Hand.Count == expectHand,
                          $"造牌：手牌 −1 打出去 +3 造出来 → {c.Players[0].Hand.Count}"
                          + $"（{before.Hand} + 2 被上限 {RuleCore.HandMax} 夹住）");
                    int elixirs = 0, elixirsOut = 0;
                    foreach (var h in c.Players[0].Hand)
                        if (h.Subtype == "Combat Elixir" || h.Subtype == "Elixir") elixirs++;
                    foreach (var h in c.Players[0].Discard)
                        if (h.Subtype == "Combat Elixir" || h.Subtype == "Elixir") elixirsOut++;
                    Check(elixirs + elixirsOut == 3,
                          $"3 张战斗药剂**都造出来了**（手牌 {elixirs} + 弃牌堆 {elixirsOut} —— 溢出的那张进弃牌堆）");
                    Check(driver.HandCount == c.Players[0].Hand.Count,
                          $"**画面手牌 {driver.HandCount} == 引擎手牌 {c.Players[0].Hand.Count}**"
                          + "（造出来的牌画面也建了视图）");
                });

            // ② 免费部署：`Skyborne Deployment`（赛姆汉 1 费）→ `Deploy two Storm Guardian`
            DriveTacticAndCheck(driver, it, cam, poolC, "Skyborne Deployment", pBoard, eBoard, "15_部署_两个风暴守卫",
                Check, (c, before) =>
                {
                    int now = 0, occupied = 0;
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var u = c.Players[0].Board[s];
                        if (u != null) occupied++;
                        if (u != null && u.Name == "Storm Guardian") now++;
                    }
                    Check(now == 2, $"部署：场上多了 2 个风暴守卫（原本占 {before.OnBoard} 格，现在 {occupied} 格）");
                    Check(occupied == before.OnBoard + 2, "……而且真的占了 2 个新格位");
                    Check(driver.HandCount == c.Players[0].Hand.Count,
                          $"画面手牌 {driver.HandCount} == 引擎手牌 {c.Players[0].Hand.Count}");
                });
        }

        // ---- 12. 投降（原版 `BattleResult.Forfeit`；2026-09-12 用户点名要的）----
        // ⚠️ 走的是**公开方法** `BattleDriver.Forfeit()`（按键那条路只是临时入口，
        //    原版没找到投降按钮）。断言四件事：判负、记下是谁投的、结算面板出得来、副标题说清是投降。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv != null)
            {
                drv.Begin("Ultramarines", "Goff", 20260912);
                var c = drv.Ctx;
                Check(c.Winner == 0, "（投降前）对局进行中");
                c.Players[drv.MyIndex].Warlord.Health = 30;

                drv.Forfeit();

                Check(c.ForfeitedBy == drv.MyIndex, "记下是「我」投降的");
                Check(c.Winner == 1 - drv.MyIndex + 1, "投降 → **对手**胜（不看我方督军血量）");
                Check(c.Players[drv.MyIndex].Warlord.Health > 0,
                      "投降时我方督军还活着（不是被打死的）");
                var end2 = drv.End;
                Check(end2 != null && end2.Visible, "结算面板弹出来了");
                Check(end2 != null && end2.ResultText == "失败", "结算标题是「失败」");
                Check(end2 != null && end2.SubText != null && end2.SubText.Contains("投降"),
                      $"副标题写明是投降（现在：`{(end2 == null ? "<无面板>" : end2.SubText)}`）");
                // 结算面板的**层序**：内容要盖在卡上面、压暗要盖住所有卡（含手牌）。
                // ⚠️ 这两条都踩过：压暗原来在 z=0，而手牌在 z 0~0.24（更远）→ 手牌整排没被压暗、
                //    亮着从面板底下透出来。截图一眼能看出来，断言能防它再犯。
                var ep2 = Object.FindObjectOfType<EndPanel>();
                if (ep2 != null)
                {
                    var dimT = ep2.transform.Find("dim");
                    float dimZ = dimT != null ? dimT.position.z : 0f;
                    float handMaxZ = float.MinValue;
                    foreach (var cv in Object.FindObjectsOfType<CardView>())
                        if (cv.name.StartsWith("Hand"))
                            handMaxZ = Mathf.Max(handMaxZ, cv.transform.position.z);
                    Check(dimT != null && dimZ < handMaxZ,
                          $"压暗层在最靠前（dim z={dimZ:F2} < 手牌最远 z={handMaxZ:F2}）");
                    var subT = ep2.transform.Find("content/end_sub");
                    Check(subT != null && subT.position.z < handMaxZ,
                          $"结算副标题盖在手牌上面（z={(subT != null ? subT.position.z : 0f):F2} < {handMaxZ:F2}）");
                }
                Shot(cam, "16_投降结算");
            }
        }

        // ---- 13. 回合时钟（原版 `ClockManager`；2026-09-12 用户点名要的）----
        // 数值出处：`DefaultScenario.json:19-21`（60 / 10 / 15 秒）、`ClockManager__GetTotalTime.c`、
        // `ClockManager__Update.c:110-141`（超时 → 15 秒倒计时 → `EndTurnClick(true)` 自动结束回合）。
        // 批处理下没有帧循环，所以**手动按秒推表**（`TickClockForTest`），不是等真实时间。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv != null)
            {
                drv.Begin("Ultramarines", "Goff", 20260913);
                // 🔴 2026-09-17：时长**照原版的三步结构算**（`TurnSecondsForThisMatch` =
                // ScenarioVariables 默认 → 缩时标志 → **matchType 覆盖**）。本局 matchType=80(EventAI)
                // ⇒ 覆盖成 **240 s**（`ClockManager__GetTotalTime.c`；常量 `0x1834b31c4` 复核过 = 240.0）。
                float total = drv.TurnSecondsForThisMatch;
                Check(Mathf.Abs(total - 240f) < 0.01f,
                      $"本局时长 = 原版 GetTotalTime 算出来的 {total:F0} s（matchType {drv.matchType} = EventAI ⇒ 240）");
                Check(drv.ClockCountingDown == false, "开局不在倒计时那一段");
                Check(Mathf.Abs(drv.ClockLeft - total) < 0.01f, $"开局表是满的（{drv.ClockLeft:F1} s）");
                string mss = $"{Mathf.FloorToInt(total) / 60}:{Mathf.FloorToInt(total) % 60:00}";
                Check(drv.ClockText == mss, $"时钟写着 `{drv.ClockText}`（m:ss）");

                drv.TickClockForTest(total - 35f);  // 走到剩 35 s（原版 `timeToHurryUp`）
                Check(Mathf.Abs(drv.ClockLeft - 35f) < 0.01f, $"推 {(int)(total - 35f)} s 后剩 {drv.ClockLeft:F1} s");
                Check(drv.ClockText == "0:35", $"时钟写着 `{drv.ClockText}`");

                drv.TickClockForTest(34f);          // 只剩 1 s
                drv.TickClockForTest(2f);           // 越过 0 → 进那 15 秒倒计时
                Check(drv.ClockCountingDown, "总时长走完 → 进了倒计时那一段");
                Check(Mathf.Abs(drv.ClockLeft - 15f) < 0.01f, $"倒计时从 15 s 起（现在 {drv.ClockLeft:F1}）");
                Check(drv.Ctx.Active == drv.MyIndex, "……这时**还没**结束回合（倒计时那 15 秒还在玩家手里）");

                drv.TickClockForTest(16f);          // 倒计时走完 → 自动结束回合
                Check(drv.Ctx.Active != drv.MyIndex, "倒计时走完 → **自动结束回合**（原版 EndTurnClick(true)）");
                Check(drv.Ctx.Winner == 0, "……但没有额外惩罚：对局还在打");
                Shot(cam, "17_回合时钟");
            }
        }

        // ---- 14. 设置面板 → 投降（**原版就是这么进的**）----
        // 出处：`BattleSettingsWindow.cs:9` `resignButton`；入口 `SettingsBtn`（权威表 x[1808.0,1871.9] y[9.2,73.1]）。
        // 第 12 节验的是「投降这个动作」，这一节验的是「**从哪个门进去**」—— 少一个都不算还原。
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv != null)
            {
                drv.Begin("Ultramarines", "Goff", 20260914);
                var sp = drv.Settings;
                Check(sp != null && !sp.Visible, "设置面板平时是关着的");
                Check(sp != null && sp.HasArt, "面板的图都取到了（底 / 圆形关闭钮 / 投降钮）");
                Check(sp != null && sp.ResignText == "投降", $"投降按钮上写着「{(sp == null ? "" : sp.ResignText)}」");

                Check(drv.SettingsBtnReady, "右上角设置按钮：贴图是 `UI_Settings_Icon`、命中矩形认自己");
                Check(drv.SimulateOpenSettings() && sp.Visible, "点设置按钮 → 面板打开");
                Shot(cam, "18_设置面板");

                // 走**和真实点击同一条判定**（`HitResign` → `Forfeit`），不是直接叫 Forfeit
                var w = sp.ResignWorldPos;
                Check(sp.HitResign(w), "投降按钮的命中判定打得中");
                if (sp.HitResign(w)) { sp.Hide(); drv.Forfeit(); }
                Check(drv.Ctx.ForfeitedBy == drv.MyIndex, "从设置面板点「投降」→ 真的判了投降");
                Check(!sp.Visible, "投完面板自己收起来了");
                Shot(cam, "18b_投降之后");
            }
        }

        // ---- 15. 手感补间（用户 2026-09-13 点名的六件）----
        // 参数全在 `CardFeel.cs`，**每一条都有出处**（原版 AnimationClip / UnitTweenSO /
        // 卡预制体序列化字段 / 反编译常量 + 从 `GameAssembly.dll` 读出的 `_DAT_`）。
        // ⚠️ 这一段**显式打开** `animateFeel` —— 前面那十几节的断言要的是「当场精确的坐标」，
        //    和 `HandLayout.animateRelayout` 是同一条规矩（见 `BuildAndSaveScene`）。
        Debug.Log(P + "--- 手感补间（发牌 / 攻击 / 命中 / 阵亡 / 数值 / 重排）---");
        {
            // ① 先把「参数表」本身钉住 —— 抄错一位整套手感就不对，而**截图看不出来**
            // ② **出处登记表**：每个常量都必须登记自己是「原版字段 / 原版推导 / 我们挑的」三档里的哪一档
            //    （用户 2026-09-13 追问过「这些都是原版资料里的参数吧」—— 答案是**大部分是、有七处不是**，
            //     所以把这件事做成会被自检打出来的清单，而不是散在注释里）
            {
                var miss = CardFeel.Unclassified();
                Check(miss.Count == 0, miss.Count == 0
                      ? $"手感常量 {CardFeel.Catalog.Length} 条**全部登记了出处**"
                      : $"有 {miss.Count} 个手感常量没登记出处：{string.Join("/", miss)}");
                Debug.Log(P + $"   出处分档：原版字段 {CardFeel.CountOf(CardFeel.Src.Field)} 条 · "
                            + $"原版推导 {CardFeel.CountOf(CardFeel.Src.Derived)} 条 · "
                            + $"**我们挑的 {CardFeel.CountOf(CardFeel.Src.Ours)} 条**");
                Debug.Log(P + "   出处登记表：\n" + CardFeel.ProvenanceReport());
            }

            // ③ **每条序列的「真实长度」必须等于它标的那个时长。**
            //    ⚠️ 这条是 2026-09-13 加上的，因为踩了一个很阴的坑：DOTween 的 `Join` 接的是
            //    **当前游标**（上一次 `Append` 的结束处）而不是序列开头 —— 写错的话那一段
            //    从中间起算、整条序列被拖长，而**截图和别的断言都看不出来**（就是慢一点）。
            //    实测：落位那条 0.30s 被拖成 0.405s（旧版更离谱：0.92 → 1.10）。
            {
                var hv0 = driver.HandViewAt(0);
                CardData probeData = hv0 != null ? hv0.Data : CardData.Placeholder(1);
                var probe = CardView.Create(driver.transform, probeData, "ProbeFeel");
                if (probe == null) Debug.Log(P + "   （建不出探针卡，跳过序列长度检查）");
                else
                {
                    float dDeploy = Dur(DeploySequence.Play(probe, Vector3.zero, 1f));
                    Check(Mathf.Abs(dDeploy - DeploySequence.MoveTime) < 1e-3f,
                          $"落位序列真实长度 {dDeploy:F3}s == 原版 `minionToConversionPointTime` {DeploySequence.MoveTime:F2}s");
                    float dDissolve = Dur(CardFeel.Dissolve(probe));
                    Check(Mathf.Abs(dDissolve - CardFeel.DissolveTime) < 1e-3f,
                          $"消散序列 {dDissolve:F3}s == {CardFeel.DissolveTime:F3}s（clip 的 `_DissolveAmount` 窗口）");
                    float dDeal = Dur(CardFeel.DealIn(probe, Vector3.zero));
                    Check(Mathf.Abs(dDeal - CardFeel.DealDuration) < 1e-3f,
                          $"发牌序列 {dDeal:F3}s == {CardFeel.DealDuration:F2}s");
                    float dLunge = Dur(CardFeel.Lunge(probe.transform, Vector3.right));
                    Check(Mathf.Abs(dLunge - CardFeel.AttackPunchDuration) < 1e-3f,
                          $"前冲序列 {dLunge:F3}s == {CardFeel.AttackPunchDuration:F2}s（`Recoil Normal Tween`）");
                    float dCharge = Dur(CardFeel.Charge(probe.transform, Vector3.right));
                    Check(Mathf.Abs(dCharge - CardFeel.ChargeTime) < 1e-3f,
                          $"蓄力序列 {dCharge:F3}s == {CardFeel.ChargeTime:F2}s（卡预制体 `timeToChargeAttack`）");
                    // 飘字那条：时间轴必须**严格照 clip**（0.117 进 / 0.2 缩完 / 停到 1.667 / 1.833 消失）
                    CardFeel.PopNumber(driver.transform, Vector3.zero, 1, false);
                    float dPop = Dur(CardFeel.LastPopTween);
                    Check(Mathf.Abs(dPop - CardFeel.PopOut) < 1e-3f,
                          $"飘字序列 {dPop:F3}s == {CardFeel.PopOut:F3}s（`InBattleDamageCounter Variation 1`）");

                    if (Application.isPlaying) Object.Destroy(probe.gameObject);
                    else Object.DestroyImmediate(probe.gameObject);
                }
            }

            Check(Mathf.Abs(CardFeel.PushBackDuration - 0.4f) < 1e-4f && CardFeel.PushBackVibrato == 8
                  && Mathf.Abs(CardFeel.PushBackElasticity - 0.3f) < 1e-4f,
                  "后坐 punch = 0.4s / vibrato 8 / 弹性 0.3（`CardScript__DoPushBack.c` + 从 DLL 读的两个 `_DAT_`）");
            Check(Mathf.Abs(CardFeel.AttackPunchUnits - 0.1f) < 1e-4f
                  && Mathf.Abs(CardFeel.AttackPunchDuration - 0.3f) < 1e-4f
                  && CardFeel.AttackPunchVibrato == 10,
                  "攻击 punch = 0.1 原版单位 / 0.3s / vibrato 10（`Recoil Normal Tween`）");
            Check(Mathf.Abs(CardFeel.ToOurs(0.1f) - 0.16865f) < 1e-3f,
                  $"原版 0.1 世界单位 → 我们 {CardFeel.ToOurs(0.1f):F4}（棋盘 182.14 px/单位 ÷ 本工程 108 px/单位）");
            Check(Mathf.Abs(CardFeel.ChargeTime - 0.35f) < 1e-4f && Mathf.Abs(CardFeel.ChargeAngleDeg + 10f) < 1e-4f
                  && Mathf.Abs(CardFeel.ChargeBackModifier - 0.5f) < 1e-4f,
                  "蓄力 0.35s / 后仰 10° / 后撤系数 0.5（卡预制体 `timeToChargeAttack` 等三个字段）");
            Check(Mathf.Abs(CardFeel.HitRotLightDeg - 3f) < 1e-4f
                  && Mathf.Abs(CardFeel.HitRotDuration - 0.5f) < 1e-4f && CardFeel.HitRotVibrato == 7,
                  "挨打的旋转 punch = 3° / 0.5s / vibrato 7（`Impact Light Tween`）");
            Check(Mathf.Abs(CardFeel.DissolveTime - 0.5333f) < 1e-3f,
                  $"消散 {CardFeel.DissolveTime:F4}s（`Card Hand To Board` 里 `_DissolveAmount` 1→0 的窗口）");
            Check(Mathf.Abs(CardFeel.PopHoldUntil - 1.6667f) < 1e-3f && Mathf.Abs(CardFeel.PopOut - 1.8333f) < 1e-3f
                  && Mathf.Abs(CardFeel.PopIn - 0.1167f) < 1e-3f,
                  "飘字 0.117s 进 / 停到 1.667s / 1.833s 消失（`InBattleDamageCounter Variation 1`）");

            var drv = Object.FindObjectOfType<BattleDriver>();
            var hand = Object.FindObjectOfType<HandLayout>();
            if (drv == null || hand == null) Check(false, "找不到 BattleDriver / HandLayout");
            else
            {
                CardTween.Mode = DG.Tweening.UpdateType.Manual;
                drv.animateFeel = true;
                drv.Begin("Ultramarines", "Goff", 20260915);

                // ---- ① 发牌入场 ----
                Check(drv.DealtCount > 0, $"发牌入场：开局这一轮 {drv.DealtCount} 张是**新进手牌**的");
                var dv = drv.DealtView(0);
                if (dv != null)
                {
                    float fromDeck = Vector3.Distance(dv.transform.position, drv.DeckWorld);
                    Check(fromDeck < 0.4f, $"……第一张此刻还压在**牌堆**上（离牌堆中心 {fromDeck:F3} 世界单位）");
                    Check(dv.Alpha < 1f, $"……而且是**淡入**中（alpha {dv.Alpha:F2} < 1）");

                    int idx = drv.HandIndex(dv);
                    CardTween.Advance(CardFeel.DealDuration + 0.1f);
                    var want = hand.SlotPosition(Mathf.Max(0, idx), drv.HandCount);
                    float off = Mathf.Abs(dv.transform.position.x - want.x)
                              + Mathf.Abs(dv.transform.position.y - want.y);
                    Check(off < 0.02f, $"推出 {CardFeel.DealDuration:F2}s → 它**落到了手牌位**上（偏差 {off:F4}）");
                    Check(dv.Alpha > 0.99f, $"……而且已经**不透明**了（alpha {dv.Alpha:F2}）");
                    Shot(cam, "19_发牌入场之后");
                }

                // ---- ②③④⑤ 一次攻击同时验：攻击位移 / 命中抖动 / 阵亡消散 / 数值过渡 ----
                // 局面是**自己摆的**（照 `5b` 的做法），不靠抽牌运气：
                // 我方督军解除疲劳当攻击者，对面放一个 1 血靶子保证一刀砍死。
                int probe = RuleEngine.BoardSpec.WarlordSlot;
                var mine = drv.Ctx.Players[0].Board[probe];
                int victim = FreeSlot(drv.Ctx, 1);
                CardDef dummy = null;
                foreach (var c in drv.Ctx.CardPool)
                    if (dummy == null && c != null && c.Type == "unit" && c.Faction == "Goff"
                        && !c.Has(KeywordTable.Stealth) && !c.Has(KeywordTable.Flying)) dummy = c;
                if (mine == null || victim < 0 || dummy == null)
                    Debug.Log(P + "   （摆不出攻击用例，跳过 ②③④⑤）");
                else
                {
                    mine.Exhausted = false;
                    drv.Ctx.Players[1].Board[victim] = new UnitState(dummy, false) { Health = 1 };
                    drv.RefreshAll();

                    var atkView = drv.MyUnits[probe];
                    var restAtk = pBoard.SlotPosition(probe);
                    Check(atkView != null, "攻击者的视图在场上");
                    Check(drv.SimulateOpenCommand(probe), "点自己的单位 → 攻击方式选择器弹出");
                    drv.SimulateCommand(AttackKind.Melee);
                    Check(drv.SimulateResolve(victim) == RuleCodes.OK, $"朝槽 {victim} 打出去");
                    Debug.Log(P + $"   [probe] 打完 t={drv.Clock:F3} 待播 {drv.TimelinePending} "
                                + $"对面槽{victim}视图 {(drv.FoeUnits.ContainsKey(victim) ? "在" : "没了")}");
                    Debug.Log(P + "   [probe] 时间线：\n" + drv.TimelineDump());

                    // 时间轴（`PlaySignals` 排的）：抬刀 0.2（`attackStepTime × 2`）→ **出手事件在 0.2**，
                    // 蓄力 0.35 + 冲 0.3；**命中事件在 0.85**（= 0.2 + `DurationOf(Attack)` 0.65）；
                    // **阵亡在 1.70**（= 0.85 + `DurationOf(Hit)` 0.75 + `DeathHold` 0.1）。
                    //
                    // ⚠️ 采样必须**细推**（1/60 一步）：`Step(dt)` 是先推事件、再把补间推 `dt` 秒，
                    //    一次推 0.45 s 就等于「命中那一下整段弹跳被跳过去了」——
                    //    第一版这么写，量出来位移恒为 0.000（而截图上看不出差别）。
                    float t0 = drv.Clock;
                    float At(float rel) { return t0 + rel; }
                    void AdvanceTo(float rel)
                    {
                        int guard = 0;
                        while (drv.Clock < At(rel) && guard++ < 2000) Step(1f / 60f);
                    }

                    AdvanceTo(0.42f);                       // 出手事件已发、蓄力走到一半
                    Debug.Log(P + $"   [probe] t={drv.Clock:F3} 待播 {drv.TimelinePending} "
                                + $"对面槽{victim}视图 {(drv.FoeUnits.ContainsKey(victim) ? "在" : "没了")} "
                                + $"消散中 {drv.DyingCount}");
                    float moved = Vector3.Distance(atkView.transform.position, restAtk);
                    Check(moved > 0.02f, $"② 攻击位移：攻击者**离开了静止位** {moved:F3} 世界单位（蓄力/前冲在动）");
                    Shot(cam, "20_出手");

                    AdvanceTo(0.87f);                       // 刚过命中那一刻（0.85）
                    var vicView = drv.FoeUnits.ContainsKey(victim) ? drv.FoeUnits[victim] : null;
                    Debug.Log(P + $"   攻击者 {atkView.name}@槽{probe}　受击者 {(vicView == null ? "无" : vicView.name)}@槽{victim}"
                                + $"　对面场上：{drv.BoardViewNames(false)}");
                    Debug.Log(P + $"   命中帧：攻击者离位 {Vector3.Distance(atkView.transform.position, restAtk):F3}"
                                + $"（角 {Mathf.DeltaAngle(atkView.transform.eulerAngles.z, 0f):F2}°）"
                                + $"　受击者离位 {(vicView == null ? -1f : Vector3.Distance(vicView.transform.position, eBoard.SlotPosition(victim))):F3}"
                                + $"（角 {(vicView == null ? 0f : Mathf.DeltaAngle(vicView.transform.eulerAngles.z, 0f)):F2}°）");
                    Check(vicView != null, "③ 受击者的视图还在（阵亡是排在时间线上的，这一刻还没轮到）");
                    if (vicView != null)
                    {
                        var restVic = eBoard.SlotPosition(victim);
                        float mv = Vector3.Distance(vicView.transform.position, restVic);
                        Check(mv > 0.02f, $"③ 命中抖动：受击者被弹开了 {mv:F3} 世界单位");
                        Check(Mathf.Abs(Mathf.DeltaAngle(vicView.transform.eulerAngles.z, 0f)) > 0.3f,
                              $"……而且**转了一下**（z 偏了 {Mathf.DeltaAngle(vicView.transform.eulerAngles.z, 0f):F2}°）");
                    }

                    Check(drv.LastPop != null, "⑤ 数值过渡：伤害数值飘出来了");
                    if (drv.LastPop != null)
                    {
                        Debug.Log(P + $"   飘字：「{drv.LastPop.Text}」");
                        Check(drv.LastPop.Text.StartsWith("-"), $"……是**伤害**（负数）：「{drv.LastPop.Text}」");
                        Check(drv.LastPop.color.a > 0.05f, $"……而且开始显影了（alpha {drv.LastPop.color.a:F2}）");
                        // ⚠️ 位置也要断言：飘字挂在驱动层自己的节点下，父节点一旦不在原点，
                        //    它会飘到别的地方去（而**截图上看不出来** —— 那一下 alpha 才 0.17、还被烟盖着）
                        var slotPos = eBoard.SlotPosition(victim);
                        float dPop = Vector3.Distance(drv.LastPop.transform.position, slotPos);
                        Check(dPop < 0.9f && drv.LastPop.transform.position.y > slotPos.y,
                              $"……而且飘在**挨打那张卡上**（离格位中心 {dPop:F3} 世界单位、在上方）");
                    }
                    Shot(cam, "21_命中与飘字");

                    // ⚠️ **2026-09-13 改：这里原来是写死的 `1.72`**，注释还写着「刚过阵亡那一刻（1.70）」。
                    //    引擎改成「伤害**同时结算** → 死亡触发排在其后」（规则书 :145 + :238）之后，
                    //    时间线上**多了一条** `Hit` —— 被攻击者的**反击**（原版 `rule_core.gd:4310`
                    //    修正过「近战击杀免反」那条规则偏差，所以目标死了也照样反击，反击是一次真伤害、
                    //    要占 `DurationOf(Hit)` 0.75 s）。⇒ 阵亡时刻从 1.70 推到 **2.45**。
                    //    **不再写死**：按事件表推出的时刻 = 命中那一刻 + 两次 Hit 的时长 + DeathHold。
                    //    （下次谁动了 `EventTiming`，这条会自己跟上，不会再变成一条骗人的断言。）
                    float deathAt = 0.85f + 2f * EventTiming.DurationOf(EvtKind.Hit) + EventTiming.DeathHold;
                    AdvanceTo(deathAt + 0.02f);             // 刚过阵亡那一刻
                    Check(drv.DyingCount == 1, $"④ 阵亡消散：有 {drv.DyingCount} 张卡正在消散（视图已被从场上摘掉）"
                          + $"（推到 t0+{deathAt + 0.02f:F2}s）");
                    var dying = drv.DyingView(0);
                    // ⚠️ 断言要**连着非空一起判** —— 第一版写成 `dying == null || dying.Alpha < 1f`，
                    //    「视图根本没进消散表」反而让它通过了（那一版 `DyingCount` 就是 0）
                    Check(dying != null && dying.Alpha < 1f,
                          $"……而且它**正在变淡**（alpha {(dying == null ? -1f : dying.Alpha):F2}，不是「啪」一下没了）");
                    Shot(cam, "22_阵亡消散");

                    while (drv.DyingCount > 0 && drv.Clock < At(deathAt + 0.02f) + CardFeel.DissolveTime + 0.2f) Step(1f / 30f);
                    Check(drv.DyingCount == 0, "……推完 0.53s → 消散结束、视图销毁");
                }

                // ---- ⑥ 手牌重排 ----
                // ⚠️ 时长**原版查不到**：`PlayerHand.PositionCardInHand(…, timeToPositionCard)` 的
                //    调用方没被反编译 —— 0.18s 是**我们挑的**（`CardTween.RelayoutDuration` 里标了）。
                bool savedAnim = hand.animateRelayout;
                hand.animateRelayout = true;
                var hv = drv.HandViewAt(0);
                if (hv != null && drv.HandCount >= 2)
                {
                    var list = new List<CardView>();
                    for (int i = 0; i < drv.HandCount; i++) list.Add(drv.HandViewAt(i));

                    float restY = hv.transform.position.y;
                    hand.Refresh(list, 0);                       // 悬停第 0 张 → 它该抬起来
                    Step(1f / 60f);
                    Check(hv.transform.position.y - restY < hand.hoverLift * 0.5f,
                          "⑥ 手牌重排走的是**补间**：刷新后的第一帧还没抬到位");
                    CardTween.Advance(CardTween.RelayoutDuration + 0.05f);
                    Check(Mathf.Abs(hv.transform.position.y - (restY + hand.hoverLift)) < 0.01f,
                          $"……推完 {CardTween.RelayoutDuration:F2}s → 抬到位（{hand.hoverLift:F2} 世界单位）");

                    hand.Refresh(list);                          // 收工：把悬停撤掉
                    CardTween.Advance(CardTween.RelayoutDuration + 0.05f);
                    Check(Mathf.Abs(hv.transform.position.y - restY) < 0.01f, "……松开后回到原位");
                }
                hand.animateRelayout = savedAnim;
                drv.animateFeel = false;
            }
        }

        // ---- 16. 「原版有、我们原来缺」的 HUD 件（2026-09-13 补摆）----
        // 清单与绝对坐标出自 `资料/战斗UI_原版对账表.md` §三点五③；逐件出处写在 `BuildHudExtras` 里。
        // 这一节验的是「**摆上去了、图取得到、位置和资料对得上**」—— 那批件全是显示件，没有行为可验。
        Debug.Log(P + "--- 原版有、我们原来缺的 HUD 件 ---");
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                drv.Begin("Ultramarines", "Goff", 20260916);
                Step(0.3f);

                Check(drv.HudExtraCount == 9, $"补摆的图 {drv.HudExtraCount} 件（应有 9：头衔底条×2 / 头像块 / 三个按钮 / 能量累积×2 / 加时标记）");
                // ⚠️ 这条是「不许静默失败」：图名字写错、资源没同步进来，都会在这里红
                Check(drv.HudExtrasMissingArt() == 0, $"这 9 件的贴图**都取到了**（缺图 {drv.HudExtrasMissingArt()} 件）");
                Debug.Log(P + "   补摆清单：\n" + drv.HudExtraReport());

                // 位置：和资料里的绝对矩形**中心**比（每件都在 1.5 px 内）
                void At(string name, float wantX, float wantY)
                {
                    var p = drv.HudExtraPosPx(name);
                    Check(Mathf.Abs(p.x - wantX) < 1.5f && Mathf.Abs(p.y - wantY) < 1.5f,
                          $"{name} 中心 ({p.x:F1},{p.y:F1}) ≈ 资料 ({wantX},{wantY})");
                }
                At("TitleBackground_Me", 209.7f, 1049.5f);      // 我 x[54.2,365.2] y[1028.5,1070.5]
                At("TitleBackground_Foe", 210.0f, 113.8f);      // 敌 x[54.5,365.5] y[92.8,134.8]
                At("AvatarItemSmall_Me", 58.15f, 997.65f);      // 容器 x[-19.7,136] y[948.1,1084.6]，
                                                                 // 但实绘的 Border 在 `Image Container` 里
                                                                 // （stretch `size(0,-37.4)`、偏移 +18.7）
                                                                 // → 实绘 y[948.1,1047.2]、中心 997.65
                At("ChatButton", 83.15f, 911.1f);               // x[50.9,115.4] y[880.2,942.0]
                At("CenterCameraButton", 50.15f, 599.1f);       // x[17.9,82.4] y[568.2,630.0]
                At("OffensiveButton", 54.5f, 500.35f);          // x[0,109] y[446.9,553.8]
                At("EnergyAccumulation_Foe", 1785.55f, 287.95f);
                At("EnergyAccumulation_Me", 1785.6f, 555.65f);
                At("OvertimeIndicator", 1753.2f, 377.0f);       // x[1718.9,1787.5] y[341.5,412.5]

                // 任务点：🔴 **只属于暗黑天使**（2026-09-13 更正 —— 原来无条件摆给全部 13 个阵营，
                // 是**张冠李戴**；机器码级出处见 `BattleDriver.ShowsQuestPoints` 的注释）。
                // 做法照原版：**物件照建、`SetActive` 切显隐** ⇒ 数字与坐标仍然量得到，只是藏着。
                // ⚠️ 这一局的双方都不是暗黑天使 ⇒ **一件都不该可见**。
                //    （原来这条断言只写 `QpText == "0/3"` —— 它只证明「建出来了」，
                //     证明不了「该不该显示」，所以这个错一直没被抓到。）
                Check(!drv.QuestPointsVisible(true) && !drv.QuestPointsVisible(false),
                      "双方都不是暗黑天使 → 任务点那一组**都不显示**（原来无条件摆给所有阵营，是张冠李戴）");
                // ✅ 2026-09-13 第三十三轮：**任务点机制有了**（`PlayerState.QuestPoints`）——
                //    那批 DarkAngels 卡的卡面图标就是任务点徽记（OCR 把它丢了，只留 `Gain 1`）。
                //    这一局双方都是 0，所以显示仍是 `0/3`，但**它现在是引擎算出来的真值**了。
                Check(drv.QpText == "0/3",
                      $"任务点数字「{drv.QpText}」= 引擎真值 `X/3`（这一局双方都是 0）");
                Check(!BattleDriver.ShowsQuestPoints("Ultramarines")
                      && !BattleDriver.ShowsQuestPoints("Goff")
                      && BattleDriver.ShowsQuestPoints("DarkAngels"),
                      "任务点**只给暗黑天使**（原版按督军阵营开关：`cmp [督军+0x2c],0x6e`）");

                // ---- 阵营资源那两件（信仰 / 灵魂石，2026-09-13 第三十三轮）----
                // 判据是「有值就显示」（`ShowsFactionResource`）—— **这是我们挑的**：
                // 原版按阵营 `Toggle`，而那个调用方没被反编译（见那个方法的注释）。
                Check(!drv.FaithVisible(true) && !drv.FaithVisible(false)
                      && !drv.SpiritStoneVisible(true) && !drv.SpiritStoneVisible(false),
                      "双方都 0 信仰 / 0 灵魂石 → 两组**都不显示**（有值才显示）");
                Check(BattleDriver.ShowsFactionResource(0) == false
                      && BattleDriver.ShowsFactionResource(1) == true,
                      "判据 `ShowsFactionResource`：0 显示不了、1 能显示");
                Check(drv.FaithTex == "40k_Battle_Display_Faith",
                      $"信仰那张图取到了：{drv.FaithTex}（取不到 = 美术没同步进来）");
                Check(drv.StoneGemTex == "UI_Gem_Eldar",
                      $"灵魂石那颗宝石取到了：{drv.StoneGemTex}");
                var qPos = drv.HudExtraPosPx("__none__");        // 只为确认找不到时返回 (-1,-1)
                Check(qPos.x < 0f, "查不到的名字返回 (-1,-1)（自检自己的哨兵值）");

                // **z 序**（同 z 的两张图谁压谁不确定，只能靠断言钉）：
                //   原版：头衔底条是名称条的**底**（在名牌后面）；头像块是 `PlayerName` 的子节点
                //         且排在 `NameBackground` 后面（**画在名牌上面**）
                Check(drv.HudExtraZDelta("TitleBackground_Me") < 0f,
                      $"头衔底条在名牌**后面**（z 差 {drv.HudExtraZDelta("TitleBackground_Me"):F2}）");
                Check(drv.HudExtraZDelta("AvatarItemSmall_Me") > 0f,
                      $"头像块在名牌**前面**（z 差 {drv.HudExtraZDelta("AvatarItemSmall_Me"):F2}，原版它是 PlayerName 的子节点）");

                // 加时标记：**默认关着**，但图要在（原版只在加时里亮；我们还没有加时机制）
                Check(!drv.OvertimeVisible, "加时标记默认**不显示**（我们还没有加时机制，原版也只在加时里出现）");
                Check(drv.OvertimeTex == "40k_icon_overtime", $"……但图已经接好了：{drv.OvertimeTex}");

                Shot(cam, "23_HUD补摆件");
            }
        }

        // ---- 17. 开局换牌（原版 `Mulligan` 子树 / 规则书 :46）----
        // 引擎侧的规则账在 `RuleEngineTest.TestMulligan` 里（15 条）；这一节验的是**画面这一侧**：
        // 换牌在 BeginTurn **之前**、面板的件都在、点「换」真的标记、点「完成」真的换掉并开打。
        Debug.Log(P + "--- 开局换牌（Mulligan）---");
        {
            var drv = Object.FindObjectOfType<BattleDriver>();
            if (drv == null) Check(false, "找不到 BattleDriver");
            else
            {
                string Sig(BattleContext c)
                {
                    var l = new List<string>();
                    // ⚠️ **凭空生成的牌不算**（第三十四轮）：换牌结束时 `BeginTurn` 里的**天赋**
                    //    会从卡池现拿一张塞进手牌，而这张签名比的是「**卡组自己的牌有没有多/少**」。
                    //    按**份数**扣 —— ⚠️ 别改成逐张判 `MarkedEphemeralCount > 0`：
                    //    `CardDef` 是**共享模板**，卡组里那张同名卡会被**一起**跳过（多扣一张）。
                    var skip = new Dictionary<string, int>();
                    foreach (var x in c.Players[0].Hand)
                    {
                        int m = c.MarkedCount(x);
                        if (m > 0)
                        {
                            int used;
                            skip.TryGetValue(x.Name, out used);
                            if (used < m) { skip[x.Name] = used + 1; continue; }
                        }
                        l.Add(x.Name);
                    }
                    foreach (var x in c.Players[0].Deck) l.Add(x.Name);
                    foreach (var x in c.Players[0].Discard) l.Add(x.Name);
                    l.Sort();
                    return string.Join(",", l.ToArray());
                }

                bool saved = drv.mulliganEnabled;
                drv.mulliganEnabled = true;
                drv.Begin("Ultramarines", "Goff", 20260917);

                Check(drv.InMulligan, "Begin 之后**先进换牌阶段**");
                // 这两条是「换牌发生在 BeginTurn 之前」的硬证据
                Check(drv.Ctx.Players[0].Energy == 0, $"……这时**还没发能量**（{drv.Ctx.Players[0].Energy}）");
                Check(drv.Ctx.Players[0].Hand.Count == RuleCore.StartHand,
                      $"……也**还没抽第 1 张**（手牌 {drv.Ctx.Players[0].Hand.Count} = 起手 {RuleCore.StartHand}）");

                var mp = drv.Mulligan;
                Check(mp != null && mp.Visible, "换牌面板开着");
                Check(mp.BarHasArt && mp.PlayHasArt && mp.EyeHasArt,
                      $"面板的件都取到图了（底「{mp.BarTex}」/ 眼睛「{mp.EyeTex}」）");
                Check(mp.CardButtonCount == drv.HandCount,
                      $"每张起手牌上都贴了「换」按钮（{mp.CardButtonCount} 个 == 手牌 {drv.HandCount} 张）");
                Check(!string.IsNullOrEmpty(mp.PromptText), $"提示行写着「{mp.PromptText}」");
                Check(!drv.TurnLabelVisible, "换牌阶段**不显示回合行**（对局还没开始，写「第 0 回合」是误导）");
                Debug.Log(P + "   " + mp.Describe());
                Shot(cam, "24_开局换牌");

                // 点「换」→ 标记；再点一次 → 取消（走的是面板的命中判定，不是直接改标记）
                Check(drv.SimulateMulliganToggle(0) && mp.IsMarked(0), "点第 1 张牌的「换」→ 标记上了");
                Check(mp.Marked.Count == 1, "……而且只标记了 1 张");
                Check(mp.CardBtnTex(0) == "UI_Button_Mulligan_Pressed",
                      $"……按钮换成按下态那张图（{mp.CardBtnTex(0)}）");
                Check(drv.SimulateMulliganToggle(0) && !mp.IsMarked(0), "再点一次 → 取消标记");
                Check(drv.SimulateMulliganToggle(0) && drv.SimulateMulliganToggle(2) && mp.Marked.Count == 2,
                      "标记第 1、3 张（`Marked` 应当是升序的 [0,2]）");
                Shot(cam, "25_换牌标记");

                // 🔴 「眼睛」那颗钮（原版 `HideMulliganButton` → `MulliganManager.ToggleMulliganVisibility`
                //    → `ShowMulliganElements`）。**2026-09-17 照反编译核实**：它是**开关**（按 activeInHierarchy
                //    取反），且一次收**四样** —— 两个 GameObject + 每张卡的换牌按钮 + **压暗层**
                //    （`Shade.SwitchShade`）。我们原来只收按钮、压暗还盖着 —— 那是**漏做**：玩家按眼睛
                //    就是为了看战场，原版那时屏幕是亮的。
                Check(drv.SimulateMulliganEye() && !mp.CardButtonsShown && !mp.ShadeActive,
                      "点「眼睛」→ 换牌按钮**和压暗层**一起收起（原版 `ShowMulliganElements(false)`）");
                // ⚠️ 图要拍在**两次点击之间** —— 拍在后面那一次之后，画面是「又都回来了」，
                //    和文件名对不上（第一次写的时候就是这么错的，图上按钮还在）。
                Shot(cam, "25b_换牌收起");
                Check(drv.SimulateMulliganEye() && mp.CardButtonsShown && mp.ShadeActive,
                      "再点一次 → 按钮和压暗都回来（**是开关，不是按住**）");

                string sigBefore = Sig(drv.Ctx);
                int handBefore = drv.Ctx.Players[0].Hand.Count;
                Check(drv.SimulateMulliganDone(), "点「完成换牌」");

                Check(!drv.InMulligan && !mp.Visible, "……换牌阶段结束、面板收起");
                Check(drv.Ctx.Players[0].Energy == 2, $"……这时才发能量（{drv.Ctx.Players[0].Energy} = 回合 1）");
                // ⚠️ 第三十四轮：换牌结束后 `BeginTurn` 里的**天赋**也会塞一张进来 ⇒ 单算。
                Check(drv.Ctx.Players[0].Hand.Count == handBefore + 1 + Conjured(drv.Ctx, 0),
                      $"……也才抽第 1 张（手牌 {handBefore} → {drv.Ctx.Players[0].Hand.Count}，"
                      + $"其中天赋生成的 {Conjured(drv.Ctx, 0)} 张）");
                // 换牌 + 抽牌的净效果：牌**一张不多一张不少**（换掉的回牌库、补抽回来）
                Check(Sig(drv.Ctx) == sigBefore, "牌一张不多一张不少（换掉 2 张 → 回牌库重洗 → 补抽 2 张）");
                Debug.Log(P + $"   换完手牌：{string.Join("/", HandNames(drv.Ctx, 0))}");
                Shot(cam, "26_换牌之后");

                // ---- 换牌倒计时（原版 `BattleManager._MulliganCountdown`）------------------
                // 原版：逐秒 -1 → **< 10 s** 把剩余秒数写到「完成换牌」那颗钮上（`SetMulliganTimer`）
                //       → **< 1 s** 自动完成（`ProcessMulliganDone` = 等价玩家点完成）。
                // 总秒数字段 = `VarsGlobal.mulliganTimeLimit`（**值拿不到，默认是我们挑的**，见字段注释）。
                // 上一段已经把换牌走完了 ⇒ 重开一局来验。
                drv.Begin("Ultramarines", "Goff", 20260919);
                Check(drv.InMulligan && mp.Visible, "重开一局 → 又进换牌阶段（下面验倒计时）");
                Check(Mathf.Abs(drv.MulliganSecondsLeft - drv.mulliganSeconds) < 0.01f,
                      $"倒计时从总秒数起（{drv.MulliganSecondsLeft:F1} = mulliganSeconds {drv.mulliganSeconds:F1}）");
                Check(mp.DoneText == MulliganPanel.DoneLabel,
                      $"开局时按钮上写的是「{MulliganPanel.DoneLabel}」（还没进最后 10 秒）");
                int toGo = Mathf.CeilToInt(drv.mulliganSeconds) - 9;
                for (int i = 0; i < toGo; i++) drv.TickMulliganForTest(1f);
                Check(mp.DoneText == "0:09", $"剩 9 s 时按钮上写着「0:09」（实得「{mp.DoneText}」）");
                Check(drv.InMulligan, "……这时**还没**自动完成（原版只在 <1 s 才自动完成）");
                Shot(cam, "25c_换牌倒计时");
                for (int i = 0; i < 12 && drv.InMulligan; i++) drv.TickMulliganForTest(1f);
                Check(!drv.InMulligan, "走到 0 → **自动完成换牌**（= 玩家点「完成换牌」同一条路）");
                Check(mp.DoneText == MulliganPanel.DoneLabel,
                      $"……收尾时按钮的字复原成「{MulliganPanel.DoneLabel}」（实得「{mp.DoneText}」）");

                // 关掉开关 → 回到默认路径（自检里那十几节用的就是这条）
                drv.mulliganEnabled = false;
                drv.Begin("Ultramarines", "Goff", 20260918);
                Check(!drv.InMulligan && drv.Ctx.Players[0].Energy == 2,
                      "不开换牌 → Begin 之后直接就是回合 1（老路径不变）");
                drv.mulliganEnabled = saved;
            }
        }


        // ---- 20. 选牌面板（原版 `ChooseCardMenu`）：引擎不再替玩家挑 ----
        //   引擎那半在 `RuleEngineTest.TestPlayerChoice`（含「不同下标 → 不同卡」那条硬判据）；
        //   这一节验的是**表现层那半**：面板真的弹出来、点得动、选完关得掉。
        //   ⚠️ 必须走 `SimulatePlayViaPanel` —— **不是** `SimulatePlay`（那个直接调引擎、绕过面板）。
        Debug.Log(P + "--- 选牌面板 ---");
        {
            var pool = CardDatabase.Load();
            var rd = CardDatabase.Find(pool, "Rapid Deployment");   // `Choose a troop in your hand. Lower its cost by 2`
            Check(rd != null, "卡池里有 `Rapid Deployment`（`Choose a troop in your hand`）");
            var panel = driver != null ? driver.Choose : null;
            Check(panel != null, "选牌面板建出来了");
            Check(panel != null && !panel.Visible, "平时是关着的");

            if (rd != null && panel != null)
            {
                ctx = driver.Ctx;                       // 上面那节重建过对局，这里重新取一次
                ctx.Players[0].Hand.Add(rd);
                ctx.Players[0].Energy = 9;              // 保证付得起（费 3）
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i], rd)) { idx = i; break; }
                Check(idx >= 0, "注入的那张牌在手牌里");

                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走**面板那条路**出牌（`BeginPlay` 返回成功）");
                Step(0.05f);

                Check(panel.Visible, "★ **面板真的弹出来了**（不用面板的话这一步直接就打出去了）");
                Check(driver.ChooseOptionCount >= 2,
                      $"★ 上面摆着 **{driver.ChooseOptionCount}** 张候选（应 ≥2 —— 手牌里的部队）");
                Check(panel.TitleText == "选择一张牌",
                      $"★ 标题「{panel.TitleText}」= 运行时实测那句"
                      + "（**不是**静态兜底的英文 `Choose one card` —— 铁律 4）");
                Check(panel.ConfirmText == "继续", $"★ 确认文案「{panel.ConfirmText}」");
                Check(panel.BarHasArt && panel.PlayHasArt && panel.EyeHasArt,
                      "★ 底条 / 圆钮 / 眼睛三张图都在"
                      + $"（`{panel.BarTex}` · `40k_UI_bt_play` · `{panel.EyeTex}`）");
                Shot(cam, "20_选牌面板");

                Check(driver.SimulateChoosePick(0), "点第 1 张候选（走面板的命中判定，真路径）");
                Check(driver.SimulateChooseDone(), "点「继续」");
                Check(!panel.Visible, "★ 选完**面板关掉了**");
                Check(ctx.LastChosenCard != null, "★ 引擎真的按面板选了一张（`LastChosenCard` 非空）");

                // 收尾：收尾：这一节是全自检**最后**跑的一节 —— 播出来的特效要清掉再交棒
                // （批处理下没有帧循环，`WarpforgeEffectPlayer` 不会自毁，见 `ClearEffects`）
                ClearEffects();
                Step(0.2f);
            }
        }

        // ---- 21. 🆕 2026-09-14：三选一 / 选效果 / 变身 —— **同一个面板**，候选不是卡池里的卡 ----
        //   为什么是同一个面板：原版 `ChooseCardMenu` 全树**只有两个调用点**（`ChoiceOfCardPlayer`
        //   与 `_SetupEnviromentalEffectPhase`），**没有第三个** —— 「choose one」走的也是它
        //   （`CardScript.NeedsToChooseFromPool()` → `BattleManager.ChooseCardMethod`
        //     → `ChoiceOfCardPlayer` → `ChooseCardMenu.Setup`）。
        //   候选在数据侧是 `TargetCriteria.filterSpecificCards`（`List<RawCardScript>`）——**就是真卡**，
        //   而且面板**带 Done 钮**（`chooseButton` / `ClickChooseDone`）。
        //   ⇒ 我们复用同一个面板 + 同一个「选完点继续」；**不是卡**的那几项由
        //     `BattleDriver.MakeChoiceCard` **合成一张卡**（源卡的插图 + 选项文字当效果文字，
        //     `cost = -1` ⇒ 不画费用六边形）。
        Debug.Log(P + "--- 三选一 / 选效果 / 变身面板 ---");
        {
            var pool = CardDatabase.Load();
            var panel = driver != null ? driver.Choose : null;
            Check(panel != null, "面板还是那一个（三族共用，没另开第三份克隆）");

            // ---- 21-a 三选一：`The Fang` = `Choose one: Deploy a Grey Hunter; Heal 4 … or Draw 2 cards` ----
            var fang = CardDatabase.Find(pool, "The Fang");
            Check(fang != null, "卡池里有 `The Fang`（`chooseone`，费 2）");
            if (fang != null && panel != null)
            {
                ctx = driver.Ctx;
                ctx.Players[0].Hand.Add(fang);
                ctx.Players[0].Energy = 9;
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i], fang)) { idx = i; break; }
                Check(idx >= 0, "注入的那张牌在手牌里");

                int before = UnitsOnBoard(ctx, 0);
                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走**面板那条路**打出 `The Fang`");
                Step(0.05f);

                Check(panel.Visible, "★ **三选一面板弹出来了**（不用面板的话这一步直接就打出去了）");
                Check(panel.TitleText == "选择一项", $"★ 标题「{panel.TitleText}」");
                Check(driver.ChooseOptionCount == 3,
                      $"★ 上面摆着 **{driver.ChooseOptionCount}** 张候选（卡面就是三项）");
                Check(driver.ChooseOptionName(0) == "Deploy a Grey Hunter",
                      $"★ 第 1 张候选的卡名「{driver.ChooseOptionName(0)}」= 卡面原文"
                      + "（**不是**小写的 `deploy a grey hunter` —— 解析层 `TryChooseOne` 存的是原文大小写）");
                Check(driver.ChooseOptionName(2) == "Draw 2 cards",
                      $"★ 第 3 张候选「{driver.ChooseOptionName(2)}」");
                Shot(cam, "21a_三选一面板");

                Check(driver.SimulateChoosePick(0), "点第 1 张候选");
                Check(driver.SimulateChooseDone(), "点「继续」");
                Check(!panel.Visible, "★ 选完**面板关掉了**");
                Check(UnitsOnBoard(ctx, 0) > before,
                      $"★ 选第 1 项真的**部署了 `Grey Hunter`**（场上 {before} → {UnitsOnBoard(ctx, 0)} 个单位）");
                ClearEffects();
                Step(0.15f);
            }

            // ---- 21-b 选效果（池子是**真卡**）：`Exemplary Warrior` 的三项就是三张真卡 ----
            var ew = CardDatabase.Find(pool, "Exemplary Warrior");
            Check(ew != null, "卡池里有 `Exemplary Warrior`（`chooseeffect`，费 2）");
            if (ew != null && panel != null)
            {
                ctx = driver.Ctx;
                ctx.Players[0].Hand.Add(ew);
                ctx.Players[0].Energy = 9;
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i], ew)) { idx = i; break; }

                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走面板那条路打出 `Exemplary Warrior`");
                Step(0.05f);

                Check(panel.Visible, "★ **选效果面板弹出来了**");
                Check(panel.TitleText == "选择一个效果", $"★ 标题「{panel.TitleText}」");
                Check(driver.ChooseOptionCount == 3, $"★ 摆着 {driver.ChooseOptionCount} 张候选");
                Check(driver.ChooseOptionId(0) == "Righteous Fury"
                      && driver.ChooseOptionId(2) == "Paragon of Ultramar",
                      $"★ 三项都是**真卡**（`{driver.ChooseOptionId(0)}` / "
                      + $"`{driver.ChooseOptionId(1)}` / `{driver.ChooseOptionId(2)}`；卡面标题是中文："
                      + $"`{driver.ChooseOptionName(0)}`）"
                      + " —— 池子里的条目本来就是卡名（`ChooseEffectPools`）");
                Shot(cam, "21b_选效果面板");

                Check(driver.SimulateChoosePick(0), "点第 1 张候选（`Righteous Fury`）");
                Check(driver.SimulateChooseDone(), "点「继续」");
                Check(!panel.Visible, "★ 选完**面板关掉了**");
                ClearEffects();
                Step(0.15f);
            }

            // ---- 21-c 选效果（池子是**载荷**）：合成卡 —— 源卡的插图 + 选项文字 ----
            var ha = CardDatabase.Find(pool, "Hyper-adaptation");
            Check(ha != null, "卡池里有 `Hyper-adaptation`（`Choose an effect and give it to a friendly troop`）");
            if (ha != null && panel != null)
            {
                ctx = driver.Ctx;
                ctx.Players[0].Hand.Add(ha);
                ctx.Players[0].Energy = 9;
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i], ha)) { idx = i; break; }

                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走面板那条路打出 `Hyper-adaptation`");
                Step(0.05f);

                Check(panel.Visible, "★ 面板弹出来了");
                Check(driver.ChooseOptionCount == 3, $"★ 摆着 {driver.ChooseOptionCount} 张候选");
                Check(driver.ChooseOptionName(0) == "+1 Armour"
                      && driver.ChooseOptionName(1) == "+2 Melee Attack",
                      $"★ 三项是**载荷文字**（`{driver.ChooseOptionName(0)}` / "
                      + $"`{driver.ChooseOptionName(1)}` / `{driver.ChooseOptionName(2)}`）"
                      + " —— 合成卡，插图用**这张卡自己的**（用户 2026-09-13 的界面线索）");
                Shot(cam, "21c_选效果_合成卡");

                Check(driver.SimulateChoosePick(0), "点第 1 张候选");
                Check(driver.SimulateChooseDone(), "点「继续」");
                Check(!panel.Visible, "★ 选完**面板关掉了**");
                ClearEffects();
                Step(0.15f);
            }

            // ---- 21-d 变身：`Hrolf the Ironhowl` 的 `become A or B` ----
            // 🔴 **2026-09-14 用户裁决翻了这条的口径**：卡面 `Stratagems in your hand become a
            //    Hunting Wolf or Fenrisian Wolf` **没有 `choose` 字样** ⇒ 按规则书英文版 `:475`
            //    的反面（只有写了 `choose` 才轮到玩家），**引擎随机挑、不开面板**。
            //    ⇒ 这条从「**面板弹出来、摆两只狼**」翻面成「**面板不弹、手牌里的战略卡直接变成一只**」。
            var hrolf = CardDatabase.Find(pool, "Hrolf the Ironhowl");
            Check(hrolf != null, "卡池里有 `Hrolf the Ironhowl`");
            if (hrolf != null && panel != null)
            {
                ctx = driver.Ctx;
                // 手牌里先塞一张**战略卡**（`become` 换的就是手牌里的战略卡）
                var strat = CardDatabase.Find(pool, "Fenrisian Wolfpack");
                Check(strat != null, "卡池里有 `Fenrisian Wolfpack`（拿它当手牌里的战略卡）");
                if (strat != null) ctx.Players[0].Hand.Add(strat);
                ctx.Players[0].Hand.Add(hrolf);
                ctx.Players[0].Energy = 9;
                driver.RefreshAll();
                Step(0.05f);

                int idx = -1;
                for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
                    if (ReferenceEquals(ctx.Players[0].Hand[i], hrolf)) { idx = i; break; }

                Check(driver.SimulatePlayViaPanel(idx, SimpleAI.FirstFreeSlot(ctx.Players[0])),
                      "走面板那条路部署 `Hrolf the Ironhowl`");
                Step(0.05f);

                Check(!panel.Visible,
                      "★ 变身**不再弹面板** —— 卡面没有 `choose`，按用户口径该**随机**"
                      + "（原来这里钉的是「面板弹出来了」，已推翻）");

                string becomeLog = null;
                foreach (string e in ctx.Events)
                    if (e != null && e.Contains("变身")) becomeLog = e;
                Check(becomeLog != null,
                      "★ 日志里留下了变身那一笔（**不静默**）—— " + (becomeLog ?? "<无>"));

                bool anyWolf = false;
                foreach (var c in ctx.Players[0].Hand)
                    if (c != null && (c.Name == "Hunting Wolf" || c.Name == "Fenrisian Wolf")) anyWolf = true;
                Check(anyWolf, "★ 手牌里的战略卡真的变成了狼（`Fenrisian Wolfpack` 那只不在了）");
                ClearEffects();
                Step(0.15f);
            }
        }

        Application.logMessageReceived -= dwCounter;
        Check(dotween == 0, $"全程没有 DOTween 补间报错（实测 **{dotween}** 条；"
                          + "真包里这一族曾经有 22 条，成因与修法见 `资料/特效还原_进度与交接.md` §七）");

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

    /// <summary>场上**部队**数（**不含督军** —— 它一直占着一格，数进去会把差别抹平）。</summary>
    static int UnitsOnBoard(BattleContext ctx, int p)
    {
        int n = 0;
        var b = ctx.Players[p].Board;
        for (int i = 0; i < b.Length; i++)
            if (i != BoardSpec.WarlordSlot && b[i] != null) n++;
        return n;
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
        // ⚠️ `Release()` 只是把显存还掉，**对象本身还在**（原来一直是靠 GC 收的）。
        //    自检一跑几十张图，收在 `EditorApplication.Exit` 之后的那些会在退出阶段被拆 ⇒
        //    显式销毁，别把账留给进程收尾（2026-09-14：选牌面板那节加进来之后退出时段错误，
        //    先把这处真泄漏堵上）。
        Object.DestroyImmediate(rt);

        File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log(P + $"   [图] {OutDir}/{name}.png");
    }

    /// <summary>日志里截断长文本用（原版卡的效果文字很长，整条打出来刷屏）</summary>
    static string Short(string s, int n)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= n) return s ?? "";
        return s.Substring(0, n) + "…";
    }

    /// <summary>
    /// **这一方手牌里「凭空生成」的牌有几张** —— 目前只有**天赋**（`RuleCore.SpawnTalents`）生成的战术卡。
    ///
    /// **为什么自检需要它**（2026-09-13 第三十四轮）：天赋卡是**回合开始时从卡池现拿的**，
    /// **不属于那 30 张卡组** ⇒「手牌 + 牌库 = 卡组张数」这类不变量会被它**顶掉一张**，
    /// 于是本轮 `BattleScene` 一次红了 7 条**计数**断言（不是引擎错，是断言写死了老行为）。
    ///
    /// ⚠️ 判据用 `BattleContext.MarkedEphemeralCount`（**只数标记**）——
    ///    **不要**用 `IsEphemeral`：那会把「卡面自带 `Ephemeral`」的 98 张也算进来，
    ///    而那些**本来就是卡组里的牌**，减掉它们就等于把不变量改错了方向。
    /// </summary>
    static int Conjured(BattleContext ctx, int p)
    {
        int n = 0;
        foreach (var c in ctx.Players[p].Hand)
            if (c != null) n += ctx.MarkedCount(c);
        return n;
    }

    /// <summary>
    /// 从真卡池里凑一副**合法**卡组：1 督军 + 1 防御卡 + 30 张同阵营卡。
    /// 其中**前 <paramref name="tactics"/> 张故意放战术卡** —— 战术卡引擎还不支持（`ErrUnimplemented`），
    /// `DeckBuilder.FromDeck` 一定会把它们丢掉，正好用来量「丢了几张」那句话说得对不对。
    /// （同名上限照 `DeckRules.CopyLimit` 走，不然它自己就先不合法的。）
    /// </summary>
    static PlayerDeck MakeDeck(List<CardDef> pool, string faction, int tactics, string name)
    {
        CardDef warlord = null, def = null;
        foreach (var c in pool)
        {
            if (c == null || c.Faction != faction) continue;
            if (warlord == null && c.Type == "hero") warlord = c;
            if (def == null && c.Type == "defence") def = c;
        }
        if (warlord == null) return null;

        int want = DeckRules.CardCount(false);
        var ids = new List<string>();
        foreach (var c in pool)                       // 战术卡先塞（这些是要被丢掉的那批）
        {
            if (ids.Count >= tactics || c == null || c.Type != "tactic" || c.Faction != faction) continue;
            for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && ids.Count < tactics; i++) ids.Add(c.Id);
        }
        foreach (var c in pool)                       // 剩下的用单位卡填满
        {
            if (ids.Count >= want || c == null || c.Type != "unit" || c.Faction != faction) continue;
            for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && ids.Count < want; i++) ids.Add(c.Id);
        }
        return new PlayerDeck(name, warlord.Id, def == null ? null : def.Id, ids);
    }

    /// <summary>
    /// 自检用：凑一副**带指定战术卡**的合法卡组（1 督军 + 1 防御 + 28 单位 + 1 张指定战术 = 30）。
    ///
    /// 为什么要单独一个：`MakeDeck` 塞的战术卡是不挑的，拖拽用例需要**确定是这张**
    /// （能完整解析、且只打敌方一个单位 —— 否则 `FromDeck` 会把它丢掉，就没得拖了）。
    /// </summary>
    static PlayerDeck DeckWithTactic(List<CardDef> pool, string faction, string tacticName)
    {
        CardDef warlord = null, def = null, tactic = null;
        foreach (var c in pool)
        {
            if (c == null || c.Faction != faction) continue;
            if (warlord == null && c.Type == "hero") warlord = c;
            if (def == null && c.Type == "defence") def = c;
            if (tactic == null && c.Type == "tactic" && c.Name == tacticName) tactic = c;
        }
        if (warlord == null || def == null || tactic == null) return null;

        int want = DeckRules.CardCount(false);
        var ids = new List<string> { tactic.Id };          // 战术卡排在最前，保证一定在牌里
        foreach (var c in pool)
        {
            if (ids.Count >= want || c == null || c.Type != "unit" || c.Faction != faction) continue;
            for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && ids.Count < want; i++) ids.Add(c.Id);
        }
        return ids.Count < want ? null : new PlayerDeck("自检·带战术卡", warlord.Id, def.Id, ids);
    }

    /// <summary>驱动一次战术卡的前后快照（造牌/部署那节的断言用）</summary>
    struct TacticBefore
    {
        public int Hand;        // 打出前的手牌张数
        public int OnBoard;     // 打出前自己场上占了几格
    }

    /// <summary>
    /// **定点驱动**：凑一副带指定战术卡的牌组 → 逐种子试到「到手且付得起」→
    /// 用真鼠标把它拖到自己半场打出去 → 等手牌真的变了 → 交给调用方断言 → 截图。
    ///
    /// 为什么要有它：战术卡是随机抽的，`BattleScene` 那条常规驱动路径**碰不到**某一张具体的卡。
    /// 造牌/部署这类效果的验收点又恰恰在「打完那一刻画面跟没跟上」，
    /// 所以必须把那张卡**钉死**再打一次。
    ///
    /// ⚠️ 拖到自己半场就行：不需要选目标的战术卡，`CanPlayTactic` 不要求格位，落哪都算数。
    /// </summary>
    static void DriveTacticAndCheck(BattleDriver driver, CardInteraction it, Camera cam,
                                    List<CardDef> pool, string tacticName,
                                    BoardLayout pBoard, BoardLayout eBoard, string shotName,
                                    System.Action<bool, string> check,
                                    System.Action<BattleContext, TacticBefore> verify)
    {
        CardDef tactic = CardDatabase.Find(pool, tacticName);
        check(tactic != null, $"卡池里有「{tacticName}」");
        if (tactic == null) return;

        var deck = DeckWithTactic(pool, tactic.Faction, tacticName);
        check(deck != null, $"凑出一副带「{tacticName}」的合法卡组（{tactic.Faction}）");
        if (deck == null) return;

        CardTween.Mode = DG.Tweening.UpdateType.Manual;
        bool ready = false;
        int idx = -1;
        BattleContext ctx = null;
        // ⚠️ 牌序随卡组内容变，写死一个 seed 迟早红 —— 逐个试到「到手 + 能量够」
        for (int attempt = 0; attempt < 8 && !ready; attempt++)
        {
            driver.Begin(seed: 20260930 + attempt, myDeck: deck);
            Step(0.3f);
            ctx = driver.Ctx;
            for (int round = 0; round < 14 && !ctx.IsOver; round++)
            {
                idx = HandIdxByName(ctx, tacticName);
                if (idx >= 0 && ctx.Players[0].Energy >= tactic.Cost) { ready = true; break; }
                driver.SimulateEndTurn();
                driver.SimulateAiTurn();
                Step(0.3f);
            }
        }
        check(ready, ready ? $"「{tacticName}」到手且付得起（第 {ctx.Turn} 回合）"
                            : $"「{tacticName}」试了 8 个种子都没到手/付不起");
        if (!ready) return;

        var before = new TacticBefore { Hand = ctx.Players[0].Hand.Count, OnBoard = 0 };
        for (int s = 0; s < BoardSpec.Size; s++)
            if (ctx.Players[0].Board[s] != null) before.OnBoard++;

        // 拖到**自己半场**（不需要目标的战术卡落哪都算数）
        int dropSlot = -1;
        for (int s = 0; s < BoardSpec.Size; s++)
            if (BoardSpec.IsDeployable(s) && ctx.Players[0].Board[s] == null) { dropSlot = s; break; }
        if (dropSlot < 0) dropSlot = BoardSpec.WarlordSlot;      // 满场也行，反正不落格位

        var view = driver.HandViewAt(idx);
        var dropPos = pBoard.SlotPosition(dropSlot);
        it.SimulateHover(view.transform.position);
        Step(0.05f);
        it.SimulatePress(view.transform.position);
        Step(0.2f);
        for (int i = 0; i < 24; i++) { it.SimulateDrag(dropPos, 1f / 30f); Step(1f / 30f); }
        it.SimulateRelease(dropPos);
        // 打出动画走完才触发 `OnDeployed`（引擎调用在回调里）—— 推到「手牌真的变了」
        for (int i = 0; i < 120 && ctx.Players[0].Hand.Count == before.Hand; i++) Step(1f / 30f);
        StepThrough(driver, 2f);

        verify(ctx, before);
        Shot(cam, shotName);
    }

    /// <summary>补间的**真实长度**（秒）。`null` 返回 -1。
    /// ⚠️ 自检用语：DOTween 的 `Join` 接的是当前游标而不是序列开头，写错会把齐序列悄悄拖长 ——
    ///    所以「标的时长」和「真实长度」必须对得上（见第 15 节 ③）。</summary>
    static float Dur(DG.Tweening.Tween t)
    {
        return t == null ? -1f : DG.Tweening.TweenExtensions.Duration(t);
    }

    static int HandIdxByName(BattleContext ctx, string name)    {
        for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
            if (ctx.Players[0].Hand[i].Name == name) return i;
        return -1;
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
