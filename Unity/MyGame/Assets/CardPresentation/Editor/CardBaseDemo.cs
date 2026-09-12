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

    [MenuItem("Tools/CardPresentation/生成演示场景并截图")]
    public static void Run()
    {
        Debug.Log(P + "=== 卡牌基座自检 开始 ===");
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
        var hoverCard = cards[6];
        var homeBefore = hoverCard.transform.position;
        it.SimulateHover(hoverCard.transform.position);
        Step(0.05f);
        bool lifted = hoverCard.transform.position.y > homeBefore.y + 0.05f;
        Debug.Log(P + $"  悬停抬起：y {homeBefore.y:F2} → {hoverCard.transform.position.y:F2}  {(lifted ? "✅" : "❌")}");
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
        bool onSlot = Vector3.Distance(hoverCard.transform.position, slotPos) < 0.05f;
        Debug.Log(P + $"  落位：到槽 {targetSlot} 的距离 {Vector3.Distance(hoverCard.transform.position, slotPos):F3}"
                    + $"  {(onSlot ? "✅" : "❌")}  台面上的牌 {it.Placed.Count} 张");
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
        Debug.Log(P + $"  非法落点：台面上的牌 {placedBefore} → {it.Placed.Count} 张  "
                    + (it.Placed.Count == placedBefore ? "✅ 没落上去" : "❌ 被错误地当成合法落点"));
        Debug.Log(P + $"  回弹坐标 {backCard.transform.position}  目标手牌位 {handLayout.SlotPosition(0, cards.Count - it.Placed.Count)}");
        Shot(cam, 1920, 1080, "05_回弹");

        // ---- 3. 6 种状态色 ----
        Debug.Log(P + "--- 状态色 ---");
        var states = new[]
        {
            CardHighlightState.Normal, CardHighlightState.Playable, CardHighlightState.Unplayable,
            CardHighlightState.Selected, CardHighlightState.ValidTarget, CardHighlightState.Hover,
        };
        for (int i = 0; i < states.Length && i < cards.Count; i++)
        {
            bool placed = false;
            foreach (var kv in it.Placed) if (kv.Value == cards[i]) { placed = true; break; }
            if (placed) continue;
            cards[i].SetHighlight(states[i]);
        }
        Step(0.05f);
        Debug.Log(P + "  六种状态已上色：" + string.Join(" / ", states));
        Shot(cam, 1920, 1080, "06_状态色");

        // ---- 存场景 ----
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log(P + $"=== 结束：图在 {OutDir}/，场景 {ScenePath} ===");
        Debug.Log(P + $"  手牌 {HandCount} 张，格位 {BoardLayout.SlotCount} 个（督军居中 = 槽 {BoardLayout.WarlordSlot}），台面 {it.Placed.Count} 张");
        Debug.Log(P + $"  特效钩子被调用 {effectPlays} 次  {(effectPlays > 0 ? "✅ 卡牌→特效接通" : "⚠️ 没触发")}");
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

        Debug.Log(P + $"    {BoardLayout.SlotCount} 格跨度 [{left:F2}, {right:F2}] 可见半宽 {halfVisible:F2}  "
                    + (inView ? "✅ 放得下" : "❌ 出界")
                    + $"　相邻间隔 {step:F2} 空档 {gap:F2} " + (noOverlap ? "✅ 不叠" : "❌ 重叠"));
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
