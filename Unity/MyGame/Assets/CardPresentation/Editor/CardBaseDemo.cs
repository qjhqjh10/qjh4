// CardBaseDemo.cs — 卡牌基座的自检 + 演示场景
//
// 验两件事：
//   1. **分辨率无关**：同一份布局在 1080p / 2K / 超宽 / 4:3 下各渲一张
//   2. **交互闭环**：悬停抬起 → 邻牌让位 → 拖拽 → 落位 / 回弹 → 状态色，每一步都截图 + 断言
//
// 批处理下没有 play 循环、也没有真实输入，所以：
//   · 补间推进方式设成 `UpdateType.Manual`，由这里手动 `Advance(dt)`
//   · 指针位置直接喂给 `CardInteraction.SimulateXxx()`（同一套逻辑，不是另写一份）
//
// 用法（菜单）：Tools > CardPresentation > 生成演示场景并截图
// 用法（CLI）：
//   unset ELECTRON_RUN_AS_NODE && Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod CardBaseDemo.Run -logFile -
//   筛输出：grep "^CBD " d:/4/_tmp_view/cardbase.log
//   🔴 判据看最后那行 `=== 合计：N 通过 / M 失败 ===`，批处理下**退出码 0 = 全过 / 1 = 有失败**
//      （⚠️ 这两样是 **2026-09-17 才有的**：在那之前它**一个断言都没有**，
//        「CardBaseDemo 全过」这句话当时没有任何依据 —— 详见下面 `Check` 那一段注释）
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CardPresentation;

public static class CardBaseDemo
{
    const string P = "CBD ";
    const string OutDir = @"d:/4/_tmp_view/cardbase";
    const string ScenePath = "Assets/CardPresentation/Scenes/CardBase.unity";

    static readonly (int w, int h, string label)[] Resolutions =
    {
        (1920, 1080, "1080p"),
        (2560, 1440, "2K"),
        (3440, 1440, "超宽 21:9"),
        (1280, 1024, "4:3"),
    };

    const int HandCount = 12;
    const int BoardUnits = 3;

    // ==================================================================
    //  断言（🔴 **2026-09-17 补的**）
    //
    //  这个入口**原来一个断言都没有** —— 全文只有 `Debug.Log` 诊断行 + 截图，**没有通过/失败汇总**
    //  ⇒ 交接文档里常年写的「`CardBaseDemo` 全过」**从来没有依据**；而它当时其实有**两条 ❌ 一直挂着**
    //  （悬停 / 落位，已修，见 `Run()` 里那段注释）。现在照 `BattleScene` 的样补上：
    //  `Check(ok, msg)` + 末尾 `=== 合计：N 通过 / M 失败 ===` + 批处理下用**退出码**回话。
    //
    //  ⚠️ 它的另一半职责（**看截图**）不变：涉及版面的改动，断言绿了也要**看一眼图**
    //  —— 「断言全绿 ≠ 版面是对的」是这工程踩过的坑（`CLAUDE.md` 第三节）。
    // ==================================================================
    static int _pass, _fail;
    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + $"   ✓ {msg}"); }
        else { _fail++; Debug.LogError(P + $"   ✗ {msg}"); }
    }

    [MenuItem("Tools/CardPresentation/生成演示场景并截图")]
    public static void Run()
    {
        Debug.Log(P + "=== 卡牌基座自检 开始 ===");
        _pass = 0; _fail = 0;      // 这个入口也能从菜单跑，跑第二次不能累加
        Debug.Log(P + "  " + CardArt.Describe());
        Directory.CreateDirectory(OutDir);

        // 动画要能手动推进，否则批处理下根本验不了（见 CardTween）
        CardTween.Mode = UpdateType.Manual;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera cam = BuildScene(out Transform handRoot, out BoardLayout board, out HandLayout handLayout);

        var cards = DealHand(handRoot, HandCount);
        DealBoard(board, BoardUnits);

        // 交互驱动
        // 接上特效钩子：卡牌表现 ←→ 特效库 就这一处耦合
        int effectPlays = 0;
        CardEffects.Play = (name, pos, parent) =>
        {
            effectPlays++;
            Debug.Log(P + $"  [特效] {name} @ {pos}");
            var player = WarpforgeVFX.WarpforgeEffectPlayer.PlayFuzzy(name, null, pos);
            Debug.Log(P + $"  [特效] 播放器 = {(player == null ? "**null（没播出来）**" : player.name)}"
                        + $"  库={(WarpforgeVFX.WarpforgeEffectLibrary.Available ? "有" : "没有")}");
        };

        var itGo = new GameObject("Interaction");
        var it = itGo.AddComponent<CardInteraction>();
        it.hand = handLayout;
        it.board = board;
        it.cam = cam;
        it.OnDeployed += (c, slot) => Debug.Log(P + $"  [事件] {c.name} 落到槽 {slot}");
        it.SetCards(cards);

        // ---- 1. 分辨率无关 ----
        Debug.Log(P + "--- 布局：各分辨率 ---");
        foreach (var r in Resolutions)
        {
            cam.aspect = (float)r.w / r.h;       // ⚠️ 必须在 Render() 之前设，见下
            board.EnsureMarkers();               // 底片/槽带按可见宽算，切分辨率要重摆一遍
            it.Relayout();                       // 手牌弧线/间距也按可见宽算，同理
            ReplaceBoardCards(board);            // 台面上的卡也一样（重建时的缩放是上一档的）
            var bd = Object.FindObjectOfType<BattleBackdrop>();
            if (bd != null) bd.Refresh();        // 背景「铺满」也按可见区算
            Shot(cam, r.w, r.h, $"res_{r.label.Replace(":", "")}");
            Debug.Log(P + $"  {r.label,-10} {r.w}×{r.h}  {LayoutSpace.Describe()}");
            AssertBoardFits(board);
        }
        // ⚠️ 恢复 16:9 之后必须**重排一次** —— 上面最后一档是 4:3，卡还停在 4:3 的位置上，
        //    不重排的话后面「悬停第 6 张」拿到的坐标是过期的（点在别的牌身上，踩过）
        cam.aspect = 16f / 9f;
        it.Relayout();

        // ---- 2. 交互闭环 ----
        Debug.Log(P + "--- 交互序列 ---");
        Shot(cam, 1920, 1080, "01_发牌");

        // 悬停第 6 张
        // 🔴 **2026-09-17 修**：探针点原来取的是**卡中心** —— 那条会命中**第 5 张**（实测 `HoveredIndex == 5`），
        //    因为手牌是**重叠扇形**、而层序是「**左边压上面**」（`HandLayout.Refresh`：`z = i * zOrderStep`，
        //    注释写明这是**原版**的「左卡 z < 右卡 z」）。第 6 张的中心被第 5 张盖住
        //    ⇒ **引擎判第 5 张是对的**，错的是这里「点中心 = 点到这张」的假设。
        //    ⇒ 改成点**这张卡的右 1/4 处**（朝远离左邻牌的那一侧，那里只有它自己）。
        //    ⚠️ 这两个 ❌（本条与下面「落位」）是**同一个根因**，而且**不是新坏的**：
        //       2026-09-17 用改动前的代码复跑过，输出逐字相同。
        var hoverCard = cards[6];
        var homeBefore = hoverCard.transform.position;
        it.SimulateHover(hoverCard.transform.TransformPoint(new Vector3(CardView.Width * 0.25f, 0f, 0f)));
        Step(0.05f);
        bool lifted = hoverCard.transform.position.y > homeBefore.y + 0.05f;
        // 🔴 **先断「点到了这张」再断「抬起来了」** —— 这两条混在一起断的话，
        //    「测试把点喂错了卡」会伪装成「产品不会抬起」（2026-09-17 就是这么误了一轮）。
        Check(it.HoveredIndex == 6, $"悬停这张卡的**右 1/4 处** ⇒ 命中的就是它（实测第 {it.HoveredIndex} 张）");
        Check(lifted, $"……而且**抬起来了**：y {homeBefore.y:F2} → {hoverCard.transform.position.y:F2}（+{hoverCard.transform.position.y - homeBefore.y:F2}）");
        Shot(cam, 1920, 1080, "02_悬停抬起");

        // 拖到督军左边那个格位
        int targetSlot = BoardLayout.WarlordSlot - 1;
        it.SimulatePress(hoverCard.transform.position);
        Step(0.2f);                                   // 拿起动画
        var slotPos = board.SlotPosition(targetSlot);
        for (int i = 0; i < 20; i++) { it.SimulateDrag(slotPos, 1f / 30f); Step(1f / 30f); }
        Debug.Log(P + $"  拖拽跟手：卡到 {hoverCard.transform.position}  目标槽位 {slotPos}");
        Shot(cam, 1920, 1080, "03_拖拽中");

        it.SimulateRelease(slotPos);
        Step(1.0f);   // 落位动画 0.92s 快走完、特效也进入亮的那一段（0.35s 时它才 4 个粒子）
        Shot(cam, 1920, 1080, "04a_落位特效");
        {
            int psN = 0, alive = 0; float maxR = 0f;
            foreach (var ps in Object.FindObjectsOfType<ParticleSystem>(true))
            {
                if (ps == null) continue;
                psN++; alive += ps.particleCount;
                var r = ps.GetComponent<Renderer>();
                if (r != null) maxR = Mathf.Max(maxR, r.bounds.extents.magnitude);
            }
            Debug.Log(P + $"  [特效诊断] 场上粒子系统 {psN} 个，活粒子合计 {alive}，最大半径 {maxR:F2}"
                        + $"  存活播放器 {WarpforgeVFX.WarpforgeEffectPlayer.ActiveCount} 个");
        }
        Step(0.5f);                                   // 把落位动画和特效尾巴走完
        float dSlot = Vector3.Distance(hoverCard.transform.position, slotPos);
        CardView atSlot; it.Placed.TryGetValue(targetSlot, out atSlot);
        Check(dSlot < 0.05f, $"松手后**落在槽 {targetSlot} 上**（到槽位距离 {dSlot:F3} < 0.05）");
        Check(atSlot == hoverCard,
              $"……而且 `Placed[{targetSlot}]` 登记的**就是这一张**（实测 {(atSlot == null ? "<空>" : atSlot.name)}）");
        Shot(cam, 1920, 1080, "04_落位");

        // 不合法落点 → 回弹
        var backCard = cards[0];
        int placedBefore = it.Placed.Count;
        it.SimulateHover(backCard.transform.position);
        Step(0.05f);
        it.SimulatePress(backCard.transform.position);
        Step(0.2f);
        var nowhere = LayoutSpace.ToWorld(0.12f, 0.88f);      // 战场上方，离任何格位都远
        for (int i = 0; i < 20; i++) { it.SimulateDrag(nowhere, 1f / 30f); Step(1f / 30f); }
        it.SimulateRelease(nowhere);
        Step(0.5f);
        // ① 不能落上去（这一条比「回到手牌」更重要 —— 落上去才是真的错）
        Check(it.Placed.Count == placedBefore,
              $"非法落点**没被算成合法**（台面上的牌 {placedBefore} → {it.Placed.Count} 张）");
        // ② 而且要**回到手牌位**（不是弹到某处停着）
        var homePos = handLayout.SlotPosition(0, cards.Count - it.Placed.Count);
        float dHome = Vector3.Distance(backCard.transform.position, homePos);
        Check(dHome < 0.05f, $"……并且**弹回手牌第 0 位**（到 {homePos} 的距离 {dHome:F3}）");
        Shot(cam, 1920, 1080, "05_回弹");

        // ---- 3. 6 种状态色 ----
        Debug.Log(P + "--- 状态色 ---");
        var states = new[]
        {
            CardHighlightState.Normal, CardHighlightState.Playable, CardHighlightState.Unplayable,
            CardHighlightState.Selected, CardHighlightState.ValidTarget, CardHighlightState.Hover,
        };
        var coloredCards = new List<CardView>();
        var coloredStates = new List<CardHighlightState>();
        for (int i = 0; i < states.Length && i < cards.Count; i++)
        {
            bool placed = false;
            foreach (var kv in it.Placed) if (kv.Value == cards[i]) { placed = true; break; }
            if (placed) continue;
            cards[i].SetHighlight(states[i]);
            coloredCards.Add(cards[i]);
            coloredStates.Add(states[i]);
        }
        Step(0.05f);
        Check(coloredCards.Count >= 5,
              $"六种状态各能上到一张卡上（本局实上 {coloredCards.Count} 张：{string.Join(" / ", states)}）");
        // ⚠️ **逐张断「真的生效了」**，不是「调过 `SetHighlight` 就当它对」——
        //    「调了但没生效」正是这个工程反复踩的形状（不报错、只是没作用）。
        for (int i = 0; i < coloredCards.Count; i++)
            Check(coloredCards[i].State == coloredStates[i],
                  $"……状态 `{coloredStates[i]}` 真的生效（实测 `{coloredCards[i].State}`）");
        Shot(cam, 1920, 1080, "06_状态色");

        // ---- 存场景 ----
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log(P + $"=== 图在 {OutDir}/，场景 {ScenePath} ===");
        Debug.Log(P + $"  手牌 {HandCount} 张，格位 {BoardLayout.SlotCount} 个（督军居中 = 槽 {BoardLayout.WarlordSlot}）");
        // ⚠️ **特效钩子在这个自检里恒为 0 次，而且这是对的** —— `CardInteraction` **不发特效事件**
        //    （那一路是 `BattleDriver` → `CardEffects.FireEvent`）。接线本身在 `BattleScene.Run` 里验。
        //    原来这里印成「⚠️ 没触发」，**看着像坏了、其实什么都不是** ⇒ 改成陈述事实，不当失败。
        Debug.Log(P + $"  特效钩子被调用 {effectPlays} 次（本自检不驱动它，接线在 BattleScene.Run 里验）");
        Check(it.Placed.Count == 1, $"台面上只登记了**落上去的那 1 张**（实测 {it.Placed.Count}）");

        // ---- 4. 卡面三件：整卡倾摆（A111）+ 破框遮罩（A112）+ 阵营行分档（2026-10-17）----
        Debug.Log(P + "--- 卡面：整卡倾摆 + 破框挖洞 + 阵营行分档 ---");
        AssertCardTilt(cards[0]);
        AssertFrameCutout(cam);
        AssertArmyLine();

        // ---- 4b. 卡面第四件：兵种行中文化（D5，2026-10-17）----
        Debug.Log(P + "--- 卡面：兵种行（原版 `RaceText`）按语言取词 ---");
        AssertSubtypeLine();
        AssertCardTextLanguageGate();     // 🔴 2026-10-18：`CardText.Zh` 改成语言闸（+ `Name` 补闸）
        AssertTitleMidline();             // 🔴 2026-10-18：`A848` —— 卡名那层 = 原版的 `Midline`
        AssertCreatedBy();                // 🔴 2026-10-18：`A985④` —— 卡面第五层「由谁造出来的」（原版 `CreatedByText`）

        // ---- 5. 手牌布局四件（B1 批 2026-10-17）----
        Debug.Log(P + "--- 手牌布局：小屏档触发 · 最近空位 · 选中让位 · 层序 ---");
        AssertHandLayout(cam);

        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    /// <summary>
    /// 9 格在高/窄分辨率下放得下吗 —— **断言，不是估算**（准则 #5：数值对不代表画面对）。
    /// 两条都要过：① 整排跨度在可见区内 ② 相邻格不叠（间距随可见宽缩，底片也跟着缩，比例恒定）。
    /// </summary>
    static void AssertBoardFits(BoardLayout board)
    {
        int last = BoardLayout.SlotCount - 1;
        // 用**落位后的**尺寸判 —— 底片和落上去的卡都是 placedScale * Scale，不是整卡尺寸
        float cardW = CardView.Width * board.placedScale * LayoutSpace.Scale;
        float halfCard = cardW * 0.5f;

        float left = board.SlotPosition(0).x - halfCard;
        float right = board.SlotPosition(last).x + halfCard;
        float halfVisible = LayoutSpace.VisibleWidth * 0.5f;
        bool inView = left >= -halfVisible && right <= halfVisible;

        float step = Mathf.Abs(board.SlotPosition(1).x - board.SlotPosition(0).x);
        float gap = step - cardW;
        bool noOverlap = gap > 0f;

        Debug.Log(P + $"    {BoardLayout.SlotCount} 格跨度 [{left:F2}, {right:F2}] 可见半宽 {halfVisible:F2}"
                    + $"　相邻间隔 {step:F2} 空档 {gap:F2}");
        Check(inView, $"{LayoutSpace.Describe()}：9 格整排**在可见区内**（[{left:F2}, {right:F2}] ⊆ ±{halfVisible:F2}）");
        Check(noOverlap, $"……相邻格**不重叠**（步距 {step:F2}，卡宽 {cardW:F2}，空档 {gap:F2} > 0）");
    }

    /// <summary>推进一帧：补间 + 粒子。批处理下粒子不会自己走，得手动 Simulate。</summary>
    static void Step(float dt)
    {
        CardTween.Advance(dt);
        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>(true))
            if (ps != null) ps.Simulate(dt, withChildren: false, restart: false, fixedTimeStep: true);
    }

    static Camera BuildScene(out Transform hand, out BoardLayout board, out HandLayout handLayout)
    {
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
        LayoutSpace.Apply(cam);
        cam.aspect = LayoutSpace.DesignAspect;    // 见 BattleScene：建的时候就得定死宽高比

        // 战场背景（装了原版美术就有）
        var bdGo = new GameObject("Backdrop");
        var bd = bdGo.AddComponent<BattleBackdrop>();
        bd.Build();

        // 战场底板（给个参照，免得卡漂在纯黑里看不出位置）。
        // **有背景图就不画了** —— 那块深色板子会盖在战场图上，反而碍事
        var boardGo = new GameObject("Board");
        board = boardGo.AddComponent<BoardLayout>();
        if (!bd.Ready)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = "Floor";
            floor.transform.SetParent(boardGo.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            floor.transform.localScale = new Vector3(LayoutSpace.DesignWidth * 0.8f, LayoutSpace.DesignHeight * 0.42f, 1f);
            var fm = new Material(Shader.Find("Unlit/Color"));
            fm.color = new Color(0.10f, 0.12f, 0.16f, 1f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = fm;
            Object.DestroyImmediate(floor.GetComponent<Collider>());
        }

        board.EnsureMarkers();      // 9 个格位底片（拖拽时会整体变色）

        var handGo = new GameObject("Hand");
        hand = handGo.transform;
        handLayout = handGo.AddComponent<HandLayout>();
        return cam;
    }

    /// <summary>手牌那一排 —— 🆕 2026-10-17 起建 **`CardFace.Hand`**（= `Full` **只少阵营行**一层）。
    /// 判据 = 用户 2026-09-22 裁定「手牌 + 场上不印阵营行，其余都印」（2026-10-17 由他指认实拍复核过；
    /// 见 `CardView.CardFace`）。
    /// 这一排同时是那条判据的**肉眼验收面**：发牌那张截图里，手牌那排**不该**有橙色的阵营名
    /// （`DealBoard` 那排仍是默认 `Full` —— 那是**卡面演示**，不是真实战场形态，真实的是 `CardFace.Board`）。</summary>
    static List<CardView> DealHand(Transform hand, int n)
    {
        var list = new List<CardView>();
        for (int i = 0; i < n; i++)
            list.Add(CardView.Create(hand, CardData.Placeholder(i), $"Hand_{i:00}", CardFace.Hand));
        return list;
    }

    static void DealBoard(BoardLayout board, int n)
    {
        var root = new GameObject("BoardCards");
        int[] slots = { BoardLayout.WarlordSlot, BoardLayout.WarlordSlot - 1, BoardLayout.WarlordSlot + 1,
                        BoardLayout.WarlordSlot - 2, BoardLayout.WarlordSlot + 2 };
        for (int i = 0; i < n && i < slots.Length; i++)
        {
            var v = CardView.Create(root.transform, CardData.Placeholder(10 + i), $"Unit_{i:00}");
            v.SetPose(board.SlotPosition(slots[i]), 0f, board.placedScale * LayoutSpace.Scale);
        }
    }

    /// <summary>台面上的卡按**当前**分辨率重摆一遍（DealBoard 只在建景时摆过一次）</summary>
    static void ReplaceBoardCards(BoardLayout board)
    {
        var root = GameObject.Find("BoardCards");
        if (root == null) return;
        int[] slots = { BoardLayout.WarlordSlot, BoardLayout.WarlordSlot - 1, BoardLayout.WarlordSlot + 1,
                        BoardLayout.WarlordSlot - 2, BoardLayout.WarlordSlot + 2 };
        for (int i = 0; i < root.transform.childCount && i < slots.Length; i++)
        {
            var v = root.transform.GetChild(i).GetComponent<CardView>();
            if (v != null) v.SetPose(board.SlotPosition(slots[i]), 0f, board.placedScale * LayoutSpace.Scale);
        }
    }

    // ==================================================================
    //  卡面三件（A111 整卡倾摆 / A112 破框挖洞 / 阵营行分档 2026-10-17 补）
    //
    //  断的是**原版判据**（方法体 / prefab 序列化值 / 资源实测），不是我们自己的常量：
    //    · `AutoCardRotation` 的三个参数、两个转轴、角度算式、回正与停手 —— 全部有出处，
    //      见 `Core/AutoCardRotation.cs` 文件头（反编译 `AutoCardRotation__*.c` + 153 个实例的 prefab 值）。
    //    · 破框那条路的判据是**几何**：卡框 mesh 的 `uv2` 必须等于立绘 mesh 在同一点上的 uv
    //      —— 这条路坏过的两次（缓存键 / `ArtCoverMargin`）都是「差一个常数」，静态就能断出来。
    //    · 阵营行那一档的判据是**用户 2026-09-22 的裁定**（手牌 + 场上不印、其余都印；
    //      **2026-10-17 由他指认实拍复核过**：详情窗里卡名下那行**橙字**就是阵营行、印着；战斗里没有）
    //      —— 完整判据与那两张实拍的坐标 → `CardView.FaceShowsArmy` 的注释。
    // ==================================================================

    /// <summary>
    /// **整卡倾摆**（原版 `AutoCardRotation`）：参数 · 算式 · 转轴 · 结构 · 回正，五样都断。
    /// 批处理没有帧循环（`Update` 不跑）⇒ 直接喂 `AutoCardRotation.Step(dt)`——
    /// 那两道门（`hasChanged` / 2 秒计时器）不在 `Step` 里，免得把断言建在「批处理下 `hasChanged` 可不可信」上。
    /// </summary>
    static void AssertCardTilt(CardView card)
    {
        var tilt = card.Tilt;
        Check(tilt != null && tilt.transform != card.transform && tilt.transform.parent == card.transform,
              "整卡倾摆挂在**卡面那一层节点**上，不是卡根上（原版 `CardUI Reference / 2DCard` 两级结构）");
        if (tilt == null) return;

        Check(Mathf.Approximately(tilt.RotationSpeed, 10f) && Mathf.Approximately(tilt.MaxXAngle, 10f)
              && Mathf.Approximately(tilt.MaxYAngle, 10f),
              $"三个参数 = 原版 prefab 值 **10 / 10 / 10**（实测 {tilt.RotationSpeed} / {tilt.MaxXAngle} / {tilt.MaxYAngle}"
            + "；⚠️ `.ctor` 里那套 2/15/15 是死值，153/153 个实例都序列化了这三个数）");
        Check(Mathf.Approximately(AutoCardRotation.EvalTimeAfterMovement, 2f),
              $"`EVAL_TIME_AFTER_MOVEMENT` = **2 秒**（实读 {AutoCardRotation.EvalTimeAfterMovement}，出处 `.cctor` 的 0x40000000）");

        // ① 角度 = clamp(位移 × 上限, ±上限)
        var q5 = AutoCardRotation.GetRotation(0.5f, 10f, Vector3.down);
        var q10 = AutoCardRotation.GetRotation(100f, 10f, Vector3.down);
        Check(Mathf.Abs(Quaternion.Angle(q5, Quaternion.AngleAxis(5f, Vector3.down))) < 0.01f
              && Mathf.Abs(Quaternion.Angle(Quaternion.identity, q5) - 5f) < 0.01f,
              $"角度算式 = **位移 × 上限**：0.5 单位 ⇒ {Quaternion.Angle(Quaternion.identity, q5):F2}°（要 5°）");
        Check(Mathf.Abs(Quaternion.Angle(Quaternion.identity, q10) - 10f) < 0.01f,
              $"……而且夹在 **±10°**：位移 100 单位也只到 {Quaternion.Angle(Quaternion.identity, q10):F2}°");
        Check(Mathf.Abs(Quaternion.Angle(AutoCardRotation.GetRotation(-100f, 10f, Vector3.down),
                                        Quaternion.AngleAxis(-10f, Vector3.down))) < 0.01f,
              "……反方向同理（−100 单位 ⇒ −10°）");

        // ② 转轴与方向（原版：x 位移绕 `Vector3.down`、y 位移绕 `Vector3.right`）
        var right = AutoCardRotation.TargetRotationFor(1f, 0f, 10f, 10f);   // 位移 1 单位 = 上限 10°
        var up = AutoCardRotation.TargetRotationFor(0f, 1f, 10f, 10f);
        Check((right * Vector3.back).x > 0.15f,
              $"卡往**右**动 ⇒ 卡面转向 +X（前缘往里倒，实测法线 x = {(right * Vector3.back).x:F3}；换轴就变号）");
        Check((up * Vector3.up).z > 0.15f,
              $"卡往**上**动 ⇒ 顶边转向 +Z（往远离相机的方向倒，实测顶边 z = {(up * Vector3.up).z:F3}）");
        Check(Quaternion.Angle(AutoCardRotation.TargetRotationFor(0f, 0f, 10f, 10f), Quaternion.identity) < 0.01f,
              "位移为 0 ⇒ 目标是**回正**（所以停住会自己站直，不是停在歪的那一下）");

        // ③ 结构：倾摆只动卡面那层；**卡根的扇形角不许被抹掉**
        var homePos = card.transform.localPosition;
        var homeRot = card.transform.localRotation;
        float fanZ = 12.75f;                       // 手牌扇形角的量级（原版一条 legacy clip 里的真值）
        float baseScale = card.transform.localScale.x;
        card.SetPose(homePos, fanZ, baseScale);
        tilt.ResetBaseline();                      // ← 把当前位姿记成「上一帧」（= `OnEnable` 那件事）
        for (int i = 0; i < 20; i++)               // 快拖 20 帧：每帧 0.25 单位（≈15 单位/秒）⇒ 目标 2.5°/帧
        {
            card.transform.position += new Vector3(0.25f, 0.25f, 0f);
            tilt.Step(1f / 60f);
        }
        float tiltDeg = Quaternion.Angle(tilt.transform.localRotation, Quaternion.identity);
        float rootZ = Mathf.DeltaAngle(0f, card.transform.localEulerAngles.z);
        Check(tiltDeg > 0.5f && tiltDeg <= 10.01f,
              $"快拖 20 帧后卡面**真的歪了**：{tiltDeg:F2}°（上限 10°，下限只表示「看得见」）");
        Check(Mathf.Abs(rootZ - fanZ) < 0.01f,
              $"……而**卡根的扇形角没被抹掉**：{fanZ:F2}° → {rootZ:F2}°（倾摆与扇形角合成一层时这条必红）");
        Check(Mathf.Abs(Mathf.DeltaAngle(0f, tilt.transform.localEulerAngles.z)) < 0.01f,
              "……倾摆**没有绕 Z 转**（原版那两个轴只出 X/Y 的倾斜，不是把卡当纸片转）");

        // ④ 回正：不喂位移 ⇒ 目标回正，2 秒后收敛到 0
        for (int i = 0; i < 120; i++) tilt.Step(1f / 60f);
        Check(Quaternion.Angle(tilt.transform.localRotation, Quaternion.identity) < 0.5f,
              $"停住 2 秒后**自己回正**（残角 {Quaternion.Angle(tilt.transform.localRotation, Quaternion.identity):F2}°）");

        // 复原（后面还要存场景、拍图）—— 连扇形角一起还原，别让自检改了演示场景的样子
        card.SetPose(homePos, 0f, baseScale);
        card.transform.localRotation = homeRot;
        tilt.ResetBaseline();
    }

    /// <summary>
    /// **破框（`FrameCutout`：卡框按立绘 alpha 挖洞、立绘只画一次）**的两条判据 + 一张并排图。
    ///
    /// 用**真卡**（`Howling Banshee Exarch` / `ASH79`，工程里当尺子的那张）——
    /// 演示场上那 12 张是 `CardData.Placeholder`，立绘没有抠图，整条路根本不会走
    /// （`CardArt.HasCutout` 为假）⇒ 拿它们断出来是空的。
    /// </summary>
    static void AssertFrameCutout(Camera cam)
    {
        RuleEngine.CardDef def = null;      // ⚠️ 全名：`CardDef` / `CardDatabase` 在 `RuleEngine` 命名空间下
        foreach (var c in RuleEngine.CardDatabase.Load()) if (c.Name == "Howling Banshee Exarch") { def = c; break; }
        if (def == null)
        {
            Check(false, "卡池里找不到 `Howling Banshee Exarch` ⇒ 破框这一档**没验到**（不是通过）");
            return;
        }
        var data = BattleDriver.ToCardData(def, def.Faction);
        Check(CardArt.HasCutout(data.artId),
              $"`{def.Name}` 的立绘**在抠图清单里**（artId = `{data.artId}`）—— 下面两条才有意义");

        // ① 采样对齐：卡框的 uv2 == 立绘网格在同一点上的 uv
        var r1 = new GameObject("cutout_probe");
        var v1 = CardView.Create(r1.transform, data, "cutout_probe");
        string d1;
        Check(v1.CheckCutoutUvAlignment(out d1), $"卡框 `uv2` 与立绘 uv **对齐**（{d1}）");
        v1.SetPose(new Vector3(-1.15f, 0.55f, 0f), 0f, 0.62f);

        // ② 切到「卡框挖洞」那条路，验材质接线（⚠️ 静态开关只在**建卡时**读 ⇒ 改完立刻改回来）
        bool prev = CardView.UseFrontLayer;
        CardView.UseFrontLayer = false;
        var r2 = new GameObject("cutout_probe_cut");
        var v2 = CardView.Create(r2.transform, data, "cutout_probe_cut");
        string d2;
        bool wireOk = v2.CheckCutoutWiring(out d2);
        string d2b;
        bool uvOk = v2.CheckCutoutUvAlignment(out d2b);
        CardView.UseFrontLayer = prev;
        Check(wireOk, $"`UseFrontLayer = false` 时卡框材质接线正确（{d2}）");
        Check(uvOk, $"……而且那条路上 `uv2` 仍然对齐（{d2b}）");
        v2.SetPose(new Vector3(1.15f, 0.55f, 0f), 0f, 0.62f);

        // 并排图：左 = 立绘两层（现状），右 = 卡框挖洞（A112 这条路）
        Debug.Log(P + "  [并排图] 07_破框并排：**左 = 立绘两层**（`UseFrontLayer=true`）· "
                    + "**右 = 卡框挖洞**（`UseFrontLayer=false`）—— 看两件事："
                    + "① 角色破框的轮廓**对得上**（右侧别出现一圈多挖/少挖的错位，那是 uv2 的事）"
                    + "② 两张的角色外轮廓**没有第二层重影**");
        Shot(cam, 1920, 1080, "07_破框并排");

        Object.DestroyImmediate(r1);
        Object.DestroyImmediate(r2);
    }

    /// <summary>
    /// **阵营行按展示场景分档** —— 判据 = **用户 2026-09-22 的裁定**：
    /// 🔴 **口径 = 用户 2026-09-22 裁定「手牌 + 场上不印，其余都印」**，**2026-10-17 由用户指认实拍复核过**
    /// （`点击卡片查看详情的参考.png`：卡名（白）下面那行**橙色** `萨姆-罕` **就是阵营行、印着**；
    /// `战斗截图参考.png`：手牌与场上**都没有**那一行）。完整判据 → `CardView.FaceShowsArmy` 的注释。
    /// 📌 **它来回过一次**（10-17 我误改成「哪都不印」、当天改回）—— **再要动，先去看那两张实拍**。
    ///
    /// 分两层断，**别把两层混成一条**：
    ///   ① **口径**（`CardView.FaceShowsArmy`，纯函数）—— 三档各断一次，不建卡、不依赖任何资源；
    ///   ② **实况**（`CardView.ArmyShown`）—— **同一份 `CardData`** 建三张卡：`Full` **真印出来了**、
    ///      `Hand` / `Board` **没印**。② 才是能分辨两档的那一条。
    ///
    /// 🧨 **改坏法**：
    ///   · `FaceShowsArmy` 改成恒 `false`（或把 `Hand` 也算进去）⇒ 下面 **`Full` 那三条红**；
    ///   · 把 `BuildTextLayers` ③ 里的 `_army = Fill(...)` 注释掉 ⇒ **实况那条也红**（两处都要对）；
    ///   · `SetFace` 里那句 `Show(_army, FaceShowsArmy(f))` 换回 `Show(_army, !board)`
    ///     ⇒ **换档那两条红**（`Hand` 那次对不上）。
    /// </summary>
    static void AssertArmyLine()
    {
        // ---- ① 口径（纯函数）：`Full` 印，`Hand` / `Board` 不印 ----
        Check(CardView.FaceShowsArmy(CardFace.Full),
              "口径：`CardFace.Full`（放大窗 / 卡池 / 收藏 / 卡组编辑 / 选牌）**印**阵营行");
        Check(!CardView.FaceShowsArmy(CardFace.Hand), "口径：`CardFace.Hand`（手牌）**不印**");
        Check(!CardView.FaceShowsArmy(CardFace.Board), "口径：`CardFace.Board`（场上）**不印**");

        // ---- ② 实况：同一份数据、只换档位 ----
        var root = new GameObject("army_probe");
        var d = CardData.Placeholder(0);                         // faction = "Ember"（非空）
        var vFull = CardView.Create(root.transform, d, "army_full");
        var vHand = CardView.Create(root.transform, d, "army_hand", CardFace.Hand);
        var vBoard = CardView.Create(root.transform, d, "army_board", CardFace.Board);

        // ★ 前提先断：字体资产取不到时 TMP 那些层**根本不会挂** ⇒ 下面「没印」的三条会**恒真**
        //   —— 那就成了假绿（「没验到」伪装成「验过了」）。
        Check(TmpFont.Available, "★ 前提：字体资产取得到（`TmpFont.Available`）—— 取不到的话下面几条全在假绿");
        Check(!string.IsNullOrEmpty(d.faction), "★ 前提：探针卡的 `faction` 非空（阵营行有字可印）");
        if (TmpFont.Available && !string.IsNullOrEmpty(d.faction))
        {
            Check(vFull.ArmyShown,  "`Full` 档（放大窗 / 卡池那一路）：阵营行**真印出来了**（层在、且开着）");
            Check(!vHand.ArmyShown,  "`Hand` 档（手牌那一路）：**没印**（层建着、关着）");
            Check(!vBoard.ArmyShown, "`Board` 档（场上）：**没印**");
            // 灭自证：两档吃的是**同一份 `CardData`** ⇒「两边一起改回同一个值」满足不了这一条。
            Check(vFull.ArmyShown != vHand.ArmyShown,
                  "……而且两档**确实是两种状态**（数据完全相同 ⇒ 只能由档位决定）");

            // ---- ③ 换档（`SetFace` 是唯一的换场景入口：出牌 / 回手都走它。铁律 10 第 5 条）----
            vHand.SetFace(CardFace.Full);
            Check(vHand.ArmyShown, "`SetFace(Hand → Full)` 之后**印出来了** —— 手牌档那一层是建着的、不是空档");
            vHand.SetFace(CardFace.Hand);
            Check(!vHand.ArmyShown, "……再 `SetFace(Full → Hand)` 又**收起来了**");
            vFull.SetFace(CardFace.Board);
            Check(!vFull.ArmyShown, "`SetFace(Full → Board)`（出牌那一步）之后**也收起来了**");
        }

        Object.DestroyImmediate(root);
    }

    /// <summary>
    /// 🆕 2026-10-17（D5）**兵种行中文化** —— 判据三处，**都不是我们自己的常量**：
    ///   · **键** = 原版 `GameStaticData.CardRaceToString` / `MinionRaceToString` 两个方法体
    ///     （`d:/2/tools/decomp_full/`）：`GetTranslation(String.Concat("Card_Race/", &lt;race&gt;))`；
    ///     前缀那两条字面量**已按 RVA 读出**（`0x1842cdf40` = `"Card_Race/"` ·
    ///     `0x1842ce040` = `"Card_Race/Warlord"`，与 `d:/2/tools/all_strings.txt` 的表偏移**逐位对上**）；
    ///   · **中文那几个词** = **实拍**（`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`：
    ///     督军卡那一行的 `战将` · 守护者防御小队 / 风暴守护者 / 战巫 / 游侠的 `步兵` · 武器平台的 `载具`）；
    ///   · **哪些 subtype 会印** = `CardView.SubtypeLine` + `TacticSubtypeShown`
    ///     （现算 `RuleEngine/Resources/cards_engine.json`：1126 张里 **19 个 subtype / 722 张**会印）。
    ///
    /// 分三层断，**别混成一条**：
    ///   ① **键齐不齐** —— 会印的 19 个一个都不能缺（缺一个 ⇒ 那张卡在中文档下**整行掉回英文**）；
    ///   ② **词条两列的值** —— 中文档 = 中文名、英文档 = 英文原值；
    ///   ③ **实况** —— **同一份 `CardData`**、只换语言 ⇒ **卡上那一格的字跟着换**。
    ///
    /// 🧨 **改坏法（③ 才是灭自证的那一条）**：把 `SubtypeLine` 的出口改回 `return d.subtype`
    /// （硬编码英文）—— **只做这一件事**，①② 仍然全绿、**③ 红**；反过来清空词条表也过不了 ①。
    /// </summary>
    /// <summary>🔴 **`A848`：卡名那层的纵向对齐 = 原版的 `Midline`(4096)**（2026-10-18 补）。
    ///
    /// 判据：原版 **78 颗卡名实例（6 个互斥变体 × 13 个场景实例）全是 `Midline`+Asar**
    /// （`资料/普查产出_1018/R4_卡面与手牌现核.md`）。⚠️ **另三层（`keywords`/`army`/`race`）原版本来就是
    /// `Middle`** ⇒ **只有卡名这一层该是 Midline** —— 所以这条断言**同时把那三层钉成「不是 Midline」**
    /// （一刀切把四层都改会改错）。
    ///
    /// 🧨 **改坏法**：删掉 `CardView` 里那句 `_title.verticalAlignment = Midline` ⇒ ① 红；
    /// 把四层一刀切改成 Midline ⇒ ② 那三条红。
    /// </summary>
    static void AssertTitleMidline()
    {
        var root = new GameObject("title_probe");
        var d = CardData.Placeholder(0);
        d.type = "unit";                      // 走单位卡那一支（卡名/阵营/兵种三层都建）
        // 🔴 **2026-10-18 收口自检修（铁律 5）**：夹具原来只给了 `type`/`title` ⇒ 下面「② 前提」那条**必然红**。
        //    **真正的成因只有一件**：`Placeholder` **不填 `subtype`** ⇒ `SubtypeLine`（`CardView.cs:1797-1811`）
        //    在 `case "unit"` 上返回 `RaceTerm(null)` = 空 ⇒ `Fill` 对空串**直接返回 null、兵种行那一层根本不建**。
        //    ⚠️ **错在夹具、不在断言**：那条前提正是用来挡「四层一刀切改 Midline」的判别式，
        //       **不许为了让前提过而把它删掉**。
        //    🔴 **2026-10-18 当场订正（独立审查 `REV_W_四写手.md` F3/F8 抓的）**：我第一版**顺手加了**
        //       `d.faction = "Ultramarines";` 并写了理由「`Placeholder` 给的是 `"Ember"`、不在表里」——
        //       **那个理由是错的**：`"Ember"` **本来就在阵营表里**（`Core/CardText.cs:452` 第一条就是它，
        //       中文「余烬」），而 `CardText.Faction`（`:505-511`）对**非空 key 永不回退** ⇒ **阵营行本来就建**。
        //       ⇒ 那行**对这条断言毫无作用**（多余改动 + 一条错论据）**已撤掉**；阵营行沿用 `Placeholder` 给的值。
        d.subtype = "Infantry";               // ← **唯一**要补的一件：兵种行非空才会建那一层（判据在 `SubtypeLine`）
        d.title = "Probe Title";              // 卡名非空才会建那一层（`Fill` 对空串直接返回 null）
        // 🔴 **2026-10-18（`A994③` 那一轮补）**：**带一枚徽标** —— 卡面**第 5 层文字**
        //    （角标数字 `badgeCounter0`）只有给了徽标才会建（`BuildBadgeSlot` 还要底板/图标两张图
        //    都取得到，取不到就整位空着 ⇒ 下面那条前提会红，那是**如实报**，⛔ 不是删断言的理由）。
        d.badges = new System.Collections.Generic.List<Badge>
        {
            new Badge { sprite = "armour", counter = 2, active = true },
        };
        var cv = CardView.Create(root.transform, d, "title_probe_card");
        try
        {
            var tmps = root.GetComponentsInChildren<TMPro.TextMeshPro>(true);
            TMPro.TextMeshPro title = null, counter = null;
            bool armySeen = false, raceSeen = false, kwSeen = false;
            foreach (var t in tmps)
            {
                if (t.name == "title") title = t;
                else if (t.name == "army") armySeen = true;
                else if (t.name == "race") raceSeen = true;
                else if (t.name == "keywords") kwSeen = true;
                else if (t.name == "badgeCounter0") counter = t;
            }
            Check(title != null, "① 卡名那层建出来了（节点名 = `title`）");
            if (title == null) return;
            Check((int)title.verticalAlignment == 4096,
                  $"① ★ 卡名对齐 = **原版那一档 `Midline`(4096)**（实得 {(int)title.verticalAlignment}）"
                + "｜🧨 删掉 `_title.verticalAlignment = Midline` ⇒ 本条红");
            // ② 对照：另三层**原版就是 Middle**（一刀切会改错，所以这几条是**反方向**的判别式）
            //  🔴 **2026-10-18（`A994③` 那一轮顺手补齐）**：原来只查 `army`/`race` 两层 ——
            //     卡面其实是**四层文字**，`keywords`（效果文字，原版 `DescTextUnit`/`DescTextTactic`）
            //     那一层**从来没被钉过**。本笔现读原版卡预制体
            //     （`bundle_staticgeneralassets_assets_all`，逐颗读 `m_VerticalAlignment`）：
            //     `NameText{Unit,Unit Big,Unit No description,Unit No Description Big,Tactic,TacticBig}` **6 个变体全 `4096`**；
            //     `DescTextUnit`/`DescTextUnit Big`/`DescTextTactic`/`DescTextTactic Big`/`DescTacticSmall`
            //     与 `ArmyTextUnit`/`ArmyTextTactc`/`Army No Desciption`/`RaceText`×3/`RaceText Big`/
            //     `CostText`×3/`Melee Attack Text`/`Range Attack Text`/`HealthText`/`Armour Text`
            //     —— **全部 `512`**。⇒ 只有卡名那一层该是 `Midline`，⛔ 一刀切会改错四层。
            Check(armySeen && raceSeen && kwSeen,
                  "② 前提：`army` / `race` / `keywords` 三层也建出来了（否则下面几条会退化成空跑）");
            foreach (var t in tmps)
                if (t.name == "army" || t.name == "race" || t.name == "keywords")
                    Check((int)t.verticalAlignment == 512,
                          $"② 对照：`{t.name}` 那层**原版就是 `Middle`(512)**（实得 {(int)t.verticalAlignment}）"
                        + "｜🧨 一刀切把四层都改成 Midline ⇒ 本条红");
            // ②·b 🔴 **2026-10-18（`A848` 的补齐件）**：卡面**第 5 层文字** —— 角标数字
            //   （原版 `TraitCounter`，现读 `bundle_battleprefabs_vfxandmisc_assets_all` 的
            //   `GameObject/TraitCounter*.json` 那 6 份：`m_VerticalAlignment = 4096`）——
            //   也是 `Midline`，原来一直按出厂 `Middle` 画。
            Check(counter != null,
                  "②·b 前提：角标那层（节点名 `badgeCounter0`）建出来了 —— 建不出来说明徽标底板/图标缺图");
            if (counter != null)
                Check((int)counter.verticalAlignment == 4096,
                      $"②·b ★ 角标数字那层对齐 = 原版 `TraitCounter` 的 **`Midline`(4096)**（实得 {(int)counter.verticalAlignment}）"
                    + "｜🧨 删掉 `PlaceBadgeCounter` 里那句 `Geometry` ⇒ 本条红");
            // ③ 🔴 **次序守卫（灭自证那一族）**：同一份数据**再刷一次**，两层的摆位必须**逐位不变**。
            //    判据：`PlaceAt` 摆的是 `textBounds`（行盒），它**随档位整体平移** ⇒ 若档位在 `PlaceAt`
            //    **之前**就已设上（或刷新时不复位），第二次摆出来会与第一次差一个 `c_G`（≈1.25px）。
            //    ⚠️ 这一条与 ①/②·b **结构上不可能同时满足**：把「先摆后设档」改成「先设档后摆」
            //       ⇒ ①/②·b 仍绿、**只有本条红**；反过来把档位整段删掉 ⇒ ①/②·b 红、本条绿。
            if (title != null && cv != null)
            {
                float t0 = title.rectTransform.localPosition.y;
                float c0 = counter != null ? counter.rectTransform.localPosition.y : 0f;
                cv.SetData(d);                      // 与战场「每刷新一次」走的是同一条（`SetData` → `BuildTextLayers`）
                float t1 = title.rectTransform.localPosition.y;
                Check(Mathf.Abs(t1 - t0) < 1e-5f,
                      $"③ ★ 再刷一次，卡名摆位**逐位不变**（y {t0:F6} → {t1:F6}，差 {Mathf.Abs(t1 - t0):F6}）"
                    + "｜🧨 删掉 `BuildTextLayers` ① 里那句「`Fill` 之前复位成 `Middle`」"
                    + "（或把设档位挪到 `Fill` 之前）⇒ 差一个 `c_G`、本条红");
                if (counter != null)
                {
                    float c1 = counter.rectTransform.localPosition.y;
                    Check(Mathf.Abs(c1 - c0) < 1e-5f,
                          $"③ ★ 再刷一次，角标摆位**逐位不变**（y {c0:F6} → {c1:F6}，差 {Mathf.Abs(c1 - c0):F6}）"
                        + "｜🧨 删掉 `PlaceBadgeCounter` 里那句「先复位成 `Middle`」⇒ 本条红");
                }
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>🆕 2026-10-18（`A985④`）卡面第五层：**「由谁造出来的」**（原版卡根的**第一个子件** `CreatedByText`）。
    ///
    /// **原版判据**（完整出处写在我们这边的实现上：`CardView.SetCreatedBy` 与 `CreatedByAt01` 那两段）：
    ///   · 位姿 `anchoredPosition (0, 1.65)` · `sizeDelta (2.5, 0.38)` · 锚点/轴心全 0.5；纯白字、
    ///     `m_fontSize 2.45` + autosize、材质带 0.2 黑描边；
    ///   · 文案 = `GetTranslation("Battle/HUD/CreatedBy").Replace("{0}", 创建者本地化卡名)`
    ///     （`SupportMethods__GetCreatedByText.c` 亲读）—— 吃的是**创建者那张卡**，不是玩家名；
    ///   · 消费面 `BattleCardUI.DisplayCreatedByText`：**先把节点关掉**，只有「有来源」且状态落在
    ///     手牌那两档（`inHandShowing=8` / `inHandPlaying=10`）时才点亮 ⇒ **场上不印**。
    ///   · **实况旁证**（`资料/原版实拍/arena_0920/runtime_ui_dump_*.tsv`：13 个战场 × 每场 8 个实例
    ///     = 104 行，`activeSelf` **全是 false** —— 出厂关着、只在运行时点亮）：`CreatedByText`
    ///     `pos 0.0,1.6` · `size 2.5,0.4`（dump 只印一位小数 ⇒ 真值是 **1.65** / **2.5×0.38**）· 锚点/轴心 0.5/0.5；
    ///     自己那份 `RectTransform/RectTransform_4803967432083755832.json` 与父件（卡根 `CardUI`，
    ///     `RectTransform_-4981929636261473480.json`）**锚点/轴心都是 0.5 ⇒ 两个 rect 同心**；
    ///     父件 rect 真值 = **2.5437×3.3686**（比 `2DCard` 的 2.1×3.3 大；dump 印的是 2.5×3.4）
    ///     ⇒ y = 1.65 是**从卡心量起**、与我们这套坐标同一个原点。⛔ 别照父件的 3.3686 折一遍 ——
    ///     那会把这一行整体压低 ~0.019 卡单位（≈2 px）。
    ///
    /// 🔴 **两态**（口径 = `CreatedByShown`，它取的是**真 `activeSelf`**，⛔ 不是「层建出来没有」）：
    ///   有来源 ⇒ 印「由 &lt;创建者卡名&gt; 创建」／没有来源 ⇒ 不印且**层被拆掉**／`SetFace(Board)` ⇒ 关掉。
    ///
    /// 🧨 **改坏法（逐条指出哪一条会红）**：
    ///   · 删掉 `SetFace` 里那句 `Show(_createdBy, !board)` ⇒ **④ 红**（层建着、场上还亮着）；
    ///   · 把它换成 `Show(_createdBy, true)`（恒亮）⇒ ④ 红；
    ///   · `FillCreatedBy` 那句 `Fill(...)` 整段注释掉 ⇒ **② 红**（而 ③ 仍绿 —— 所以 ②③ 是一条的两半）；
    ///   · `CreatedByLine` 改回裸卡名（不套模板）⇒ **② 红**；把 `CardText.Name` 的语言闸拆掉 ⇒ **⑥ 红**。
    ///
    /// ⚠️ **两条「我们挑的」不当原版判据断言**（实现在 `CardView` 里也如实标了）：
    ///   ① **压谁** —— 我们按「可见」把它放在卡面之前，**未跑实况**；
    ///   ② **没照搬** `m_fontSizeMin 0.3` + Overflow（照搬会长卡名横向溢出 ~2 倍卡宽）
    ///      ⇒ ⛔ 本方法**不**断「渲染宽度 ≤ 框宽」：那要先知道 0.2 描边给网格留了多少白，
    ///      而这一步**没跑过 Unity 就量不出来**（铁律 5·c：没查到就写没查到，⛔ 别拿猜测当判据）。
    /// </summary>
    static void AssertCreatedBy()
    {
        // ---- ① 拿一张**真卡**当创建者（`SetCreatedBy` 吃的就是 `CardDef`）----
        RuleEngine.CardDef def = null;
        foreach (var c in RuleEngine.CardDatabase.Load())
            if (c.Name == "Howling Banshee Exarch") { def = c; break; }   // 工程里当尺子的那张（同破框那一段）
        Check(def != null, "① 前提：卡池里有 `Howling Banshee Exarch`（拿它当创建者，不是自造的假名字）");
        if (def == null) return;
        // ★ 前提：两个卡名任一为空 ⇒ 下面每条 `Contains` **恒真** ⇒ 那几条全是假绿，必须先堵掉
        Check(!string.IsNullOrEmpty(def.Name) && !string.IsNullOrEmpty(def.NameZh),
              "① 前提：创建者的**中/英卡名都非空**（空串会让下面的 `Contains` 恒真）");
        // ★ 前提：字体资产不在 ⇒ 那一层根本不建 ⇒ 下面几条退化成「null == null」（同上，假绿）
        Check(TmpFont.Available, "★ 前提：字体资产取得到 —— 取不到的话下面几条全在假绿");

        var root = new GameObject("createdby_probe");
        var langBack = Loc.Current;
        Loc.PersistOverride = true;                    // 自检不许动玩家的真设置
        try
        {
            var d = CardData.Placeholder(0);           // 两张探针卡吃**同一份** `CardData`
            var vWith = CardView.Create(root.transform, d, "cb_with", CardFace.Hand);
            var vNull = CardView.Create(root.transform, d, "cb_null", CardFace.Hand);

            Loc.SetLanguage(AvailableLanguages.Chinese);
            vWith.SetCreatedBy(def);                   // ← 有来源（「被效果造出来的」那一档）
            vNull.SetCreatedBy(def);                   // ← 先也给一个，下面再抽掉

            // ---- ② 有来源 ⇒ 印，而且印的是**创建者那张卡的中文名**夹在模板里 ----
            string zh = vWith.CreatedByShown;
            Check(!string.IsNullOrEmpty(zh),
                  $"② 有来源 ⇒ **印出来了**：「{zh}」"
                + "｜🧨 把 `FillCreatedBy` 那句 `Fill(...)` 注释掉 ⇒ 本条红");
            Check(zh != null && zh.Contains(def.NameZh) && zh != def.NameZh,
                  $"② ……内容是**创建者的本地化卡名**「{def.NameZh}」**套在模板里**"
                + "（⛔ 不是玩家名、也不是裸卡名）｜🧨 `CreatedByLine` 改回裸卡名 ⇒ 本条红");
            // ★ ②b **真渲出来了**（不是「层在、字不在」—— 那是这工程的常客，见 `CLAUDE.md` 第三节）。
            //   量法用全仓唯一那一份 `ShellScene.TmpSpanPx`（`A844` 收口）：它扫的是 TMP 自己
            //   `textInfo` 里 `isVisible` 的那些字形顶点。⛔ 别在这儿自创一套扫网格的循环。
            var cb = LayerTmp(vWith.transform, "createdBy");
            float sx1, sy1, sx2, sy2; int sverts = 0;
            bool spanned = cb != null
                        && ShellScene.TmpSpanPx(cb.transform, out sx1, out sy1, out sx2, out sy2, out sverts);
            Check(spanned,
                  $"②b ★ 那行字**真有可见顶点**（{sverts} 个；节点名 `createdBy`）"
                + " —— 断「层开着」不够，要断「字真画出来了」");

            // ---- ③ 来源被抽掉 ⇒ **那一层被拆掉**（同一张视图，先有后无）----
            vNull.SetCreatedBy(null);
            Check(vNull.CreatedByShown == null,
                  "③ `SetCreatedBy(null)`（抽掉来源）⇒ **不印** —— 走的是 `Fill` 收到空串就拆层那条路"
                + "｜🧨 让 `Fill` 收到空串时**不拆层**（改成保留旧对象）⇒ 本条红，而 ② 仍绿");
            // ★ 灭自证①：两张卡的 `CardData` **逐字相同** ⇒ 只能由「有没有来源」决定。
            //   「把两边一起写死」满足不了本条。
            Check(zh != null && vNull.CreatedByShown == null,
                  "③ ★ 判别式：同一份 `CardData` 的两张卡**确实是两种状态**（一个有串、一个是 null）");

            // ---- ④ 换到场上形态 ⇒ 收起来（**层还在、只是关着**）----
            //   🔴 铁律 10 第 5 条：`SetFace` 是**唯一**的换场景入口（手牌打出去走的就是它）。
            vWith.SetFace(CardFace.Board);
            Check(vWith.CreatedByShown == null,
                  "④ `SetFace(Board)` 之后**收起来了**（原版 inPlay 那几档先把节点关掉再 return）"
                + "｜🧨 删掉 `SetFace` 里那句 `Show(_createdBy, !board)` ⇒ 本条红");
            // ★ 灭自证②（**本节最重要的一条**）：那一层**还在树上**、只是 `activeSelf == false`。
            //   —— 断「`_createdBy != null`」的话，「亮着」与「关着」**两种状态都绿**（同义反复）；
            //      只有读真 `activeSelf`（`CreatedByShown` 就是）才分得开。⛔ 别把它换成断非 null。
            Check(cb != null && !cb.gameObject.activeSelf,
                  "④ ★ 判别式：这一层**建着、只是关着**（`activeSelf == false`）"
                + " —— 只删 `Show` 那一句 ⇒ 本条红、而 ②③ 仍绿（正是这条与它们的分别）");

            // ---- ⑤ 回到手牌 ⇒ 又亮出来，内容逐字不变 ----
            //   ⚠️ 本工程**今天没有** `Board → Hand` 的调用点（`SetFace` 只用于出牌那一步）；
            //      这条防的是「将来加了回手入口、漏设这一处」—— 就是铁律 10 第 5 条那个形状。
            vWith.SetFace(CardFace.Hand);
            Check(!string.IsNullOrEmpty(zh) && vWith.CreatedByShown == zh,
                  $"⑤ `SetFace(Hand)` 之后**又亮出来**、内容逐字不变（「{vWith.CreatedByShown}」）");

            // ---- ⑥ 语言：同一张卡、只换语言 ⇒ 换的是**创建者卡名**那一截 ----
            Loc.SetLanguage(AvailableLanguages.English);
            vWith.SetCreatedBy(def);                   // `Loc` 不发事件 ⇒ 换完语言要重推一次（同 `AssertSubtypeLine`）
            string en = vWith.CreatedByShown;
            Check(en != null && en.Contains(def.Name) && en != def.Name,
                  $"⑥ 英文档 ⇒ 印的是**英文卡名**（「{en}」；中文档是「{zh}」）"
                + "｜🧨 把 `CardText.Name` 那道语言闸拆掉 ⇒ 本条红");
            // ★ 灭自证③：数据逐字相同、只有语言不同 ⇒ 两串必须不同
            Check(zh != en, "⑥ ★ 判别式：两档**确实是两串**（「把两边一起写死」满足不了本条）");
        }
        finally
        {
            Loc.RestoreForTest(langBack);
            Loc.PersistOverride = false;
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>在卡的子树里按**节点名**找一层 TMP（`title` / `army` / `createdBy` …）——
    /// 扫法与 `AssertTitleMidline` 那一段逐句相同（`includeInactive: true`：关着的那层也要找得到）。</summary>
    static TMPro.TextMeshPro LayerTmp(Transform card, string layerName)
    {
        foreach (var t in card.GetComponentsInChildren<TMPro.TextMeshPro>(true))
            if (t.name == layerName) return t;
        return null;
    }

    /// <summary>🔴 **`CardText.Zh` 是【语言闸】不是【字体闸】**（2026-10-18 改；判据 = 原版有语言选择器 ⇒ 选了英文卡面就该是英文）。
    ///
    /// 改之前：`Zh = TmpFont.Available` —— 中文字体资产一进仓库它**恒真** ⇒ **切成 English 之后，
    /// 卡名/阵营/关键词/效果文字/`Phrases` 兜底那 17 条仍然是中文**（只有走 `Loc.T` 的件会变）。
    /// 改之后：`Zh = TmpFont.Available &amp;&amp; Loc.Current == Chinese`；**并且** `Name(id, nameZh)` 也补上了这道闸
    /// （它原来**无条件**返回 `nameZh` ⇒ 绕过语言闸）。
    ///
    /// 🧨 **改坏法（逐条指出哪一条会红）**：
    ///   · 把 `Zh` 改回 `TmpFont.Available` ⇒ **②** 红；
    ///   · 只改 `Zh`、不改 `Name(id,nameZh)` 那道闸 ⇒ **③** 红（那一条正是绕过闸的那处）。
    /// </summary>
    static void AssertCardTextLanguageGate()
    {
        var langBack = Loc.Current;
        Loc.PersistOverride = true;                       // 自检不许动玩家的真设置
        try
        {
            // ---- ① 中文档：行为**与改之前逐字相同**（默认语档就是中文 ⇒ 零变化）----
            Loc.SetLanguage(AvailableLanguages.Chinese);
            Check(CardText.Zh, "① 中文档下 `CardText.Zh` = true（字体 + 语档两条都满足）");
            string zhName = CardText.Name("TAU52", "石之感知");
            Check(zhName == "石之感知", $"① 中文档卡名走卡表那一列（实得「{zhName}」）");

            // ---- ② 英文档：**卡名必须回英文** ----
            Loc.SetLanguage(AvailableLanguages.English);
            Check(!CardText.Zh,
                  "② 英文档下 `CardText.Zh` = **false** —— 🧨 把 `Zh` 改回只判 `TmpFont.Available` ⇒ 本条红");

            // ---- ③ 绕过闸的那一处：`Name(id, nameZh)` 原来无条件返回中文名 ----
            string enName = CardText.Name("TAU52", "石之感知");
            Check(enName == "TAU52",
                  $"③ ★ 英文档卡名回**英文 id**（实得「{enName}」）—— 🧨 只改 `Zh`、不给 `Name(id,nameZh)` 补闸 ⇒ 本条红");
            Check(enName != zhName,
                  "③ 判别式：两档的卡名**不是同一串**（防「两边都返回 id」也照样过）");
        }
        finally { Loc.RestoreForTest(langBack); Loc.PersistOverride = false; }
    }

    static void AssertSubtypeLine()
    {
        // ---- ① 键：会印到卡面上的 19 个（= 数据里出现过的 subtype ∩ `SubtypeLine` 会印）----
        string[] printed =
        {
            "Infantry", "Vehicle", "Warlord", "Defence", "Monster", "Beast", "Battlesuit", "Drone",
            "Psychic Power", "Combat Elixir", "Daemon", "Secret", "Dark Pact", "Sabotage",
            "Codicil", "Genomic Enhancement", "Invocation", "Overlord Power", "Rune",
        };
        int missing = 0;
        // ⚠️ **期望值写字面量，不写 `CardView.RaceTermPrefix`** —— 后者是**被测实现**的常量，
        //    两边都用它 = 同义反复（「把前缀连实现一起改掉」照样全绿）。判别式单列一条在下面。
        const string KeyPrefix = "Card_Race/";                 // 原版 `.rdata` 那条（`0x1842cdf40`）
        Check(CardView.RaceTermPrefix == KeyPrefix,
              "实现的键前缀 = **原版那条字面量** `Card_Race/`（⛔ 不是自拟的 `Card/Subtype/…`）"
            + " —— 🧨 改成自拟前缀这条立刻红");
        foreach (var s in printed) if (!Loc.HasEntry(KeyPrefix + s)) missing++;
        Check(missing == 0, $"会印在卡面上的 {printed.Length} 个字种**全都有词条**（缺 {missing} 个）");

        // ---- ② 词条值：中文档 / 英文档各断一次 ----
        //   ⚠️ 中文那一列里**只有 3 个是实拍**，其余 16 个是我们自己中文表
        //   （`数据/本地化/i18n/zh_CN.csv`）的既有译法 —— 这里只钉实拍那 3 个（能钉住的才钉）。
        var langBack = Loc.Current;
        Loc.PersistOverride = true;                       // 自检不许动玩家的真设置
        try
        {
            Loc.SetLanguage(AvailableLanguages.Chinese);
            string zhInf = Loc.T("Card_Race/Infantry"), zhVeh = Loc.T("Card_Race/Vehicle");
            string zhWar = Loc.T("Card_Race/Warlord");
            Check(zhInf == "步兵" && zhVeh == "载具" && zhWar == "战将",
                  "★ 实拍读到的那三个（`用户实拍_1017/卡组编辑界面参考.png`）：**步兵 / 载具 / 战将**"
                + $"—— 实测 \"{zhInf}\" / \"{zhVeh}\" / \"{zhWar}\"");
            Loc.SetLanguage(AvailableLanguages.English);
            Check(Loc.T("Card_Race/Infantry") == "Infantry" && Loc.T("Card_Race/Warlord") == "Warlord",
                  "…英文那一列 = **英文原值本身**（原版的英文文案在远端 I2 表里，本地只拿得到原值）");
        }
        finally { Loc.RestoreForTest(langBack); Loc.PersistOverride = false; }

        // ---- ③ 实况：同一份数据、只换语言 ⇒ 卡上那格的字跟着换 ----
        var root = new GameObject("race_probe");
        var d = CardData.Placeholder(0);
        d.subtype = "Infantry";      // ⚠️ `Placeholder` **不带** subtype / type，这里显式给
        d.type = "unit";             //    走 `unit` 那一支（唯一「印 subtype 原值」的那条）
        var v = CardView.Create(root.transform, d, "race_probe_card");

        // ★ 前提先断：字体资产不在 ⇒ 那一层根本不挂 ⇒ 下面两条会退化成 `null == null`（**假绿**）
        Check(TmpFont.Available, "★ 前提：字体资产取得到 —— 取不到的话下面两条在假绿");
        if (TmpFont.Available)
        {
            Loc.PersistOverride = true;
            try
            {
                Loc.SetLanguage(AvailableLanguages.Chinese);
                v.SetData(d);                       // `Loc` **不发事件** ⇒ 换完语言要自己重画一遍
                string zh = v.RaceText;
                Loc.SetLanguage(AvailableLanguages.English);
                v.SetData(d);
                string en = v.RaceText;
                Check(zh == "步兵", $"实况（中文档）：卡上那一格 = **步兵**（实测 \"{zh}\"）");
                Check(en == "Infantry", $"实况（英文档）：**同一张卡**那一格 = `Infantry`（实测 \"{en}\"）");
                // 灭自证：`CardData` 一模一样、只有语言不同 ⇒ 两格**必须不同**
                //（「把两边一起写死」同时满足不了这一条）。
                Check(zh != en, "……而且两档**确实是两个字**（数据完全相同 ⇒ 只能由语言决定）");
            }
            finally { Loc.RestoreForTest(langBack); Loc.PersistOverride = false; }
        }
        Object.DestroyImmediate(root);
    }

    // ==================================================================
    //  手牌布局四件（B1 批 2026-10-17）：小屏档触发 · 最近空位 · 选中让位 · 层序
    //
    //  判据**全部来自原版**，不是我们自己的常量：
    //    · 小屏档   → `CardsHorizontalLayout__GetPosition.c:83-88`（静态 bool 的三目，**与屏宽无关**）
    //    · 最近空位 → `CardsHorizontalLayout__GetClosestInHandSlot.c:14-33`
    //                 （逐卡比 |Δx|；**严格小于才换** ⇒ 平局取小序号；初值 `.rdata 0x1834b3354` = 1e6）
    //    · 选中让位 → `CardsHorizontalLayout__GetPosition.c` 的 `else { fVar10 = 0f; }`（:212-214）
    //                 （**装得下 ⇒ extra 归零** ⇒ 只有压缩态才让位）
    //                 + `VarsDesktop.extraSpaceOnSelectedCard` = 平线 **2.0 世界单位**
    //    · 悬停抬起 → `VarsDesktop.cardInHandShownYOffset` **30 px** / `cardInHandShownScale` **1.3**
    //    · 层序     → `PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:55`
    //                 （`sortingOrder = 名单长度 − 序号 − 1` ⇒ **左边的压在上面**）
    //  ⚠️ 探针**自建自拆**：这一段跑在 `SaveScene` 之后 ⇒ 不进场景、也不进任何截图。
    // ==================================================================
    static void AssertHandLayout(Camera cam)
    {
        float aspectBack = cam.aspect;
        var probe = new GameObject("hand_probe_layout");
        var layout = probe.AddComponent<HandLayout>();

        // ================= 项 9：小屏档的触发条件 = `SmallScreenUI` 开关 =================
        //  🔴 判别式**两个方向都堵**：4:3（可见宽 < 设计宽）**不该**触发；超宽 + 开关开**必须**触发。
        //     —— 旧的「按可见宽猜」在这两格上各错一次（4:3 走成 0.51 / 超宽走成 0.59）。
        SmallScreenUI.PersistOverride = true;          // 自检不许动玩家的真设置
        try
        {
            cam.aspect = 16f / 9f;
            SmallScreenUI.ResetForTest();
            float capNormal = layout.MaxLayoutWorld();
            SmallScreenUI.Set(true);
            float capSmall = layout.MaxLayoutWorld();
            float wantDelta = (layout.maxLayoutSize - layout.maxLayoutSizeSmallScreen) * LayoutSpace.VisibleWidth;
            Check(Mathf.Abs((capNormal - capSmall) - wantDelta) < 1e-4f,
                  $"小屏开关开着 ⇒ 上限切到 `m_maxLayoutSizeSmallScreen`：差值 {capNormal - capSmall:F4}"
                + $"（要 {wantDelta:F4} = ({layout.maxLayoutSize} − {layout.maxLayoutSizeSmallScreen})"
                + $" × 可见宽 {LayoutSpace.VisibleWidth:F3}）");

            cam.aspect = 4f / 3f;
            SmallScreenUI.ResetForTest();
            float got43 = layout.MaxLayoutWorld() / LayoutSpace.VisibleWidth;
            float want43 = layout.maxLayoutSize + layout.aspectRatioModifier.Evaluate(4f / 3f);
            Check(Mathf.Abs(got43 - want43) < 1e-4f,
                  $"**4:3 不是触发条件**：可见宽 {LayoutSpace.VisibleWidth:F2} < 设计宽 {LayoutSpace.DesignWidth:F2}，"
                + $"但开关关着 ⇒ 仍走 {layout.maxLayoutSize}（实测比值 {got43:F4}，要 {want43:F4}）"
                + " —— 旧的「按可见宽猜」在这一格会走 0.51 ⇒ 本行变红");

            cam.aspect = 21f / 9f;
            SmallScreenUI.Set(true);
            float gotUltra = layout.MaxLayoutWorld() / LayoutSpace.VisibleWidth;
            float wantUltra = layout.maxLayoutSizeSmallScreen + layout.aspectRatioModifier.Evaluate(21f / 9f);
            Check(Mathf.Abs(gotUltra - wantUltra) < 1e-4f,
                  $"**超宽屏 + 开关开着 ⇒ 仍走小屏档**（实测比值 {gotUltra:F4}，要 {wantUltra:F4}）"
                + " —— 旧的「按可见宽猜」在这一格会走 0.59 ⇒ 本行变红");
        }
        finally
        {
            SmallScreenUI.ResetForTest();
            SmallScreenUI.PersistOverride = false;
            cam.aspect = aspectBack;
        }

        // ================= 项 11：`GetClosestInHandSlot` = 逐卡比 |Δx| 取最近 =================
        //  槽位与 `Refresh` 同一套：others 张看得见 ⇒ **slots = others + 1** 个候选槽
        const int others = 7;
        int slots = others + 1;
        float sp = layout.SpacingFor(slots) * LayoutSpace.VisibleWidth;
        float x0 = layout.SlotPosition(0, slots).x;
        Debug.Log(P + $"    [最近空位] 探针：候选槽 {slots} 个，间距 {sp:F4} 世界，槽 0 在 x = {x0:F4}");

        Check(layout.GetClosestInHandSlot(new Vector3(x0 - 5f, 0f, 0f), others) == 0,
              "指针在整把手牌**左边很远处** ⇒ 空位 0");
        Check(layout.GetClosestInHandSlot(new Vector3(x0 + others * sp + 5f, 0f, 0f), others) == others,
              $"指针在整把手牌**右边很远处** ⇒ 空位 {others}（不是 {slots} —— 原版只在 {slots} 个候选位置里挑，"
            + "返回值域就是 0..total−1）");

        // 🔴 判别式：分界在**两张卡中心的【中点】**上，不是在卡中心上 ——
        //    旧的「指针一过第 s 张中心就判 s+1」在第一种喂法里会**整排错成下一个槽**。
        int bad = 0;
        for (int s = 0; s < slots; s++)
            if (layout.GetClosestInHandSlot(new Vector3(x0 + (s + 0.3f) * sp, 0f, 0f), others) != s) bad++;
        Check(bad == 0, $"槽中心 + **30% 间距** ⇒ 仍判这一个槽（{slots} 个槽全测，错 {bad} 个；"
                      + "旧写法在这里会判成下一个槽）");

        int badFar = 0;
        for (int s = 0; s < slots; s++)
            if (layout.GetClosestInHandSlot(new Vector3(x0 + (s + 0.7f) * sp, 0f, 0f), others)
                != Mathf.Min(s + 1, slots - 1)) badFar++;
        Check(badFar == 0, $"槽中心 + **70% 间距** ⇒ 判下一个槽（错的 {badFar} 个；末端夹在 {slots - 1}）");

        float mid = x0 + 2.5f * sp;                  // 槽 2 与槽 3 的中点
        Check(layout.GetClosestInHandSlot(new Vector3(mid - 0.01f, 0f, 0f), others) == 2
              && layout.GetClosestInHandSlot(new Vector3(mid + 0.01f, 0f, 0f), others) == 3,
              "**分界确实在中点上**：中点左 0.01 ⇒ 槽 2、右 0.01 ⇒ 槽 3（旧写法左边那半就判 3）");

        // ================= 项 8 / 7：8 张（压缩态）那一把 =================
        var c8 = new List<CardView>();
        for (int i = 0; i < 8; i++)
            c8.Add(CardView.Create(probe.transform, CardData.Placeholder(i), $"HP8_{i:00}", CardFace.Hand));

        layout.Refresh(c8, -1, -1);
        var rest8 = new Vector3[8];
        for (int i = 0; i < 8; i++) rest8[i] = c8[i].transform.position;

        // ---- 项 7：层序 —— z 随序号单调靠后 ⇒ 左边压上面（= 原版 `sortingOrder = 名单长度 − 序号 − 1`）----
        int zBad = 0;
        for (int i = 1; i < 8; i++)
            if (!(c8[i].transform.position.z - c8[i - 1].transform.position.z > 0f)) zBad++;
        Check(zBad == 0, $"层序：z 随序号**单调靠后** ⇒ 左边的压在上面（8 张里错 {zBad} 张，"
                       + $"步长 {layout.zOrderStep:F4}）—— 原版是 `sortingOrder = 名单长度 − 序号 − 1`，符号反了就变「右压左」");
        // ⚠️ 下面这条只**打诊断、不断言**：原版的 z 步长（0.001 ≈ 0.015 px）在这儿**不适用**
        //    （它靠嵌套 Canvas 的 `sortingOrder` 排序，我们是世界空间 quad ⇒ 只能靠 z）。
        //    量的是「一张卡自己各层的 z 跨度」，它必须**小于**步长，否则邻牌的文字会穿透（2026-09-12 实测过）。
        //    ⛔ 不写成断言的原因：卡内层集合会随卡面档位变，而这一批**不能跑 Unity 去实测那个跨度**
        //    （没实测过的判据写成断言 = 假绿/假红两头都可能）。
        float zSpan = LocalZSpan(c8[0].transform);
        Debug.Log(P + $"    [层序诊断] 卡内层 z 跨度 {zSpan:F4}（卡局部空间）× 卡缩放 "
                    + $"{layout.cardScale * LayoutSpace.Scale:F3} = {zSpan * layout.cardScale * LayoutSpace.Scale:F4}"
                    + $"    手牌 z 步长 {layout.zOrderStep:F4}");

        // ---- 项 8：`useExtraSpaceOnSelectedCard` —— 原版**装得下就把 extra 归零** ⇒ 只有压缩态才让位 ----
        //  让位量按**原版值**算（`VarsDesktop` 平线 2.0 世界单位），不读我们的字段 ——
        //  否则「两边一起改成 0」也会全绿（自证）。
        float wantExtra = 2.0f * HandLayout.OurUnitsPerWorldUnit;
        Check(Mathf.Abs(layout.selectedCardExtra - wantExtra) < 1e-5f,
              $"让位量 = 原版 `VarsDesktop.extraSpaceOnSelectedCard` 平线 **2.0 世界单位** = 29.7 px"
            + $"（实测 {layout.selectedCardExtra * 108f:F1} px）");

        const int sel = 4;
        layout.Refresh(c8, sel, -1);
        int spreadBad = 0; float worst8 = 0f;
        for (int i = 0; i < 8; i++)
        {
            if (i == sel) continue;
            float dx = c8[i].transform.position.x - rest8[i].x;
            float want = i < sel ? -wantExtra : wantExtra;
            if (Mathf.Abs(dx - want) > 1e-4f) spreadBad++;
            worst8 = Mathf.Max(worst8, Mathf.Abs(dx));
        }
        Check(spreadBad == 0,
              $"**压缩态（8 张 ≥ 阈值）**选中一张 ⇒ **两侧各自整半边**都让 2.0 世界单位"
            + $"（错 {spreadBad} 张，实测最大位移 {worst8 * 108f:F1} px）"
            + " —— 只挪紧邻那一张（被换掉的自造 `neighborShift`）会让本行变红");

        // ---- 项 8 的**反向**判别式：装得下（5 张）⇒ 一点不让 ----
        var probe5 = new GameObject("hand_probe_layout5");
        var layout5 = probe5.AddComponent<HandLayout>();
        var c5 = new List<CardView>();
        for (int i = 0; i < 5; i++)
            c5.Add(CardView.Create(probe5.transform, CardData.Placeholder(i), $"HP5_{i:00}", CardFace.Hand));
        layout5.Refresh(c5, -1, -1);
        var rest5 = new Vector3[5];
        for (int i = 0; i < 5; i++) rest5[i] = c5[i].transform.position;
        layout5.Refresh(c5, 2, -1);
        float moved5 = 0f;
        for (int i = 0; i < 5; i++)
            if (i != 2) moved5 = Mathf.Max(moved5, Mathf.Abs(c5[i].transform.position.x - rest5[i].x));
        Check(moved5 < 1e-4f,
              $"**装得下（5 张 < 阈值）⇒ 一点不让**（实测最大位移 {moved5:F5}）——"
            + " 原版在「装得下」那一支把 `extra` 归零；把门闸改回 `!IsCompressed` 会让本行变红");

        // ---- 悬停抬起 / 放大 = 原版 `VarsDesktop` 那两个值 ----
        Check(Mathf.Abs(layout.hoverLift * 108f - 30f) < 0.01f,
              $"悬停抬起 = 原版 `cardInHandShownYOffset` **30 px**（实测 {layout.hoverLift * 108f:F2} px）"
            + " —— 换回自造的 0.06 世界单位（= 6.5 px）会让本行变红");
        Check(Mathf.Abs(layout.hoverScale - 1.3f) < 1e-4f,
              $"悬停放大 = 原版 `cardInHandShownScale` **1.3**（实测 {layout.hoverScale}）");

        // ---- 敌方那一份的第一道门 = 原版 `useExtraSpaceOnSelectedCard = 0`（我方 = 1）----
        var foe = new GameObject("hand_probe_foe").AddComponent<HandLayout>();
        foe.ConfigureForEnemy();
        Check(!foe.useExtraSpaceOnSelectedCard,
              "敌手那份 `useExtraSpaceOnSelectedCard = 0`（我方 = 1）—— 原版 MB 4053 的序列化值");

        Object.DestroyImmediate(probe5);
        Object.DestroyImmediate(foe.gameObject);
        Object.DestroyImmediate(probe);
    }

    /// <summary>一张卡**自己内部**各层在卡根坐标系里的 z 跨度（递归累加 `localPosition.z`）。
    /// 用途只有一个：量出「手牌 z 步长必须大于它」那个约束里的右边那一项（见 `HandLayout.zOrderStep` 注释）。</summary>
    static float LocalZSpan(Transform t)
    {
        float lo = 0f, hi = 0f;
        WalkZ(t, 0f, ref lo, ref hi);
        return hi - lo;
    }

    static void WalkZ(Transform t, float z, ref float lo, ref float hi)
    {
        if (z < lo) lo = z;
        if (z > hi) hi = z;
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            WalkZ(c, z + c.localPosition.z, ref lo, ref hi);
        }
    }

    static void Shot(Camera cam, int w, int h, string tag)
    {
        // ⚠️ `cam.aspect` 一旦被显式赋值就**固定住**，不再跟着 RenderTexture 走 ——
        //    必须在 Render() **之前**设，否则会拿上一个分辨率的宽高比去渲（踩过）
        cam.aspect = (float)w / h;

        var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        File.WriteAllBytes($"{OutDir}/cardbase_{tag}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
    }
}
