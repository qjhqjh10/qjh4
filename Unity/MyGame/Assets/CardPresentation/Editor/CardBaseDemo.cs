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

    static List<CardView> DealHand(Transform hand, int n)
    {
        var list = new List<CardView>();
        for (int i = 0; i < n; i++)
            list.Add(CardView.Create(hand, CardData.Placeholder(i), $"Hand_{i:00}"));
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
