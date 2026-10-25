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
using RuleEngine;

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

    // ==================================================================
    //  🔴 2026-10-19：`Dark Pact of *` 的「**无费用**」
    // ==================================================================
    // 背景：用户 2026-10-19 裁定这 4 张（`Fate/Blood/Excess/Resilience`）是战术卡触发生成的
    //   **契约 / 附魔模板**、**没有费用**（不是「0 费」）。
    // 数据判据：当前 build 的卡 SO `manaCost = -1`（原版「不可直接打出」哨兵）——
    //   `资料/普查产出_1018/allcards_1350/cards_raw.json` 里这 4 个 `uniqueId` 全 `-1`；
    //   生成链 = `Enchantments` SO 的 markOfChaos 映射（10→Blood / 20→Resilience /
    //   30→Excess / 40→Fate，50 = 四选一）+ 反编译 `CardScript__AddEffect.c:776-792` case 8
    //   `Enchantments.GetMarkOfChaos` → `RawCardScript.TriggerEnchantment`（只作附魔挂上去，从不直接打出）。
    // ⇒ 生成器（`工具/gen_cards_engine.py` 的 SO 真值层）落 `cost = -1`；
    //   `CardView.ShowsCost` = `cost >= 0 && type != "hero"` ⇒ 费用格（底板 + 数字）都不画。
    //
    // 三条断言，**每条都写明怎么才能红（灭自证）**：
    //   ① `cost == -1` —— 🧨 把生成器改回「只取 `manaCost >= 0`」⇒ 必红；
    //   ② `ShowsCost == false` —— 🧨 删掉 `ShowsCost` 里的 `cost >= 0` ⇒ 必红；
    //   ③ 费用底板节点不激活 —— 🧨 把 `Build`/`SetFace` 里的 `ShowsCost` 判据删掉 ⇒ 必红。
    static void CheckDarkPactNoCost()
    {
        Debug.Log(P + "--- Dark Pact of *（附魔模板）：无费用 ---");
        var pool = RuleEngine.CardDatabase.Load();
        var names = new[]
        {
            "Dark Pact of Fate", "Dark Pact of Blood",
            "Dark Pact of Excess", "Dark Pact of Resilience",
        };
        foreach (var n in names)
        {
            RuleEngine.CardDef def = null;
            foreach (var c in pool) if (c.Name == n) { def = c; break; }
            Check(def != null, $"卡池里有 `{n}`（找不到 ⇒ 这一张的下面两条**没验到**，不是通过）");
            if (def == null) continue;

            // ① 数据：`cost = -1`（原版哨兵）。⛔ 别读成「缺值」—— 缺值是 `-1`、但这里 SO 里就是 `-1`。
            Check(def.Cost == -1,
                  $"① `{n}` 的 `cost = -1`（现读 {def.Cost}；原版 `manaCost = -1` = 不可直接打出）"
                + "｜🧨 生成器改回「只取 >=0」⇒ 本条必红");

            var data = BattleDriver.ToCardData(def, def.Faction);
            // ② 卡面判据：不画费用。🧨 删掉 `ShowsCost` 的 `cost >= 0` ⇒ 必红。
            bool show = CardView.ShowsCost(data);
            Check(!show,
                  $"② `{n}` 的 `CardView.ShowsCost == false`（现读 {show}）"
                + "｜🧨 删掉 `ShowsCost` 的 `cost >= 0` ⇒ 本条必红");

            // ③ 真正的节点：费用底板（原版 `Cost Container/Cost Background`）**不激活**。
            var root = new GameObject("chk_darkpact_" + n.Replace(" ", "_"));
            var v = CardView.Create(root.transform, data, root.name);
            Check(!v.CostVisible,
                  $"③ `{n}` 的费用底板节点**不激活**（`CostVisible == false`）"
                + "｜🧨 把 `Build`/`SetFace` 里的 `ShowsCost` 判据删掉 ⇒ 本条必红");
            Object.DestroyImmediate(root);
        }
    }

    [MenuItem("Tools/CardPresentation/生成演示场景并截图")]
    public static void Run()
    {
        Debug.Log(P + "=== 卡牌基座自检 开始 ===");
        _pass = 0; _fail = 0;      // 这个入口也能从菜单跑，跑第二次不能累加
        // 🔴 2026-10-19：`Dark Pact of *` 的「无费用」（数据 + 卡面两条口径，见下面那个方法）
        CheckDarkPactNoCost();
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
        Step(1.0f);   // 落位动画走完、特效也进入亮的那一段（0.35s 时它才 4 个粒子）
                      // ⚠️ **2026-10-16 订正（铁律 5）**：这里原来写「落位动画 **0.92s** 快走完」——
                      //   那个 0.92 是**认错了来源**：它是 `Card Hand To Board`（2D→3D **交接** clip）的长度，
                      //   **不是**位移时长。位移 = `Board/DeploySequence.cs`，**2026-10-16 起是原版三段：飞 0.3 + 悬停 0.05 + 垂直落 0.2 = 0.55 s**。
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
        SceneStubAudit.Run(ScenePath);   // 🆕 A1506：存盘后自证「不许有 !u!115 存根」（干净时零输出）
        AssetDatabase.Refresh();

        Debug.Log(P + $"=== 图在 {OutDir}/，场景 {ScenePath} ===");
        Debug.Log(P + $"  手牌 {HandCount} 张，格位 {BoardLayout.SlotCount} 个（督军居中 = 槽 {BoardLayout.WarlordSlot}）");
        // ⚠️ **特效钩子在这个自检里恒为 0 次，而且这是对的** —— `CardInteraction` **不发特效事件**
        //    （那一路是 `BattleDriver` → `CardEffects.FireEvent`）。接线本身在 `BattleScene.Run` 里验。
        //    原来这里印成「⚠️ 没触发」，**看着像坏了、其实什么都不是** ⇒ 改成陈述事实，不当失败。
        Debug.Log(P + $"  特效钩子被调用 {effectPlays} 次（本自检不驱动它，接线在 BattleScene.Run 里验）");
        Check(it.Placed.Count == 1, $"台面上只登记了**落上去的那 1 张**（实测 {it.Placed.Count}）");

        // ---- 4. 卡面三件：整卡倾摆（A111）+ 立绘与卡框的层级（A112，原「破框遮罩」）+ 阵营行分档（2026-10-17）----
        Debug.Log(P + "--- 卡面：整卡倾摆 + 立绘不超出卡框（A112）+ 阵营行分档 ---");
        AssertCardTilt(cards[0]);
        AssertFrameCutout(cam);
        AssertArtAlphaThroughFrame();     // 🆕 2026-10-24（K1 · `新账-66`）：立绘透框 = `1−α`（一次乘法，不是平方）
        AssertTacticArtClamp();           // 🆕 2026-10-24（H1）：战术/防御卡的**生产**立绘也套原版硬矩形 `_Clamp_Range`
        AssertTacticArtScale();           // 🆕 2026-10-24（E9）：战术/防御卡的立绘 ×0.61 + 中心 y+0.5（`CardScalesByCardType` / `tacticsOffsetPosition`）
        AssertBigStateOverDrawGate();     // 🆕 2026-10-24（H5）：大图档的 **BIG 状态**由「前台 × useOverDraw」两轴门控
        AssertArmyLine();

        // ---- 4b. 卡面第四件：兵种行中文化（D5，2026-10-17）----
        Debug.Log(P + "--- 卡面：兵种行（原版 `RaceText`）按语言取词 ---");
        AssertSubtypeLine();
        AssertCardTextLanguageGate();     // 🔴 2026-10-18：`CardText.Zh` 改成语言闸（+ `Name` 补闸）
        AssertCardTextTablesInLoc();      // 🔴 2026-10-10（A1084）：那几族中文**已搬进 `Core/Loc.cs`**（键族 + 齐全 + 值 + 语料 + 别名 + 不复用近邻）
        AssertTitleMidline();             // 🔴 2026-10-18：`A848` —— 卡名那层 = 原版的 `Midline`
        // 🆕 2026-10-24（`E16`）：效果文字**装得进版面框、且不压数值格**（真机反复看到的溢出缺陷）
        Debug.Log(P + "--- 卡面：效果文字装得进版面框（框宽 / 纵向预算 / 与原版矩形中心的差 / 数值行）---");
        AssertFaceTextFit();
        AssertCreatedBy();                // 🔴 2026-10-18：`A985④` —— 卡面第五层「由谁造出来的」（原版 `CreatedByText`）
        AssertTextBg(cam);               // 🔵 2026-10-19（A1500② 的断言侧）：文字底板 —— 含**渲图像素**判据
        AssertRarityGem(cam);             // 🔴 2026-10-18（A1492）：稀有度宝石 —— 结构 + 渲图灭自证
        AssertCostTierRefresh();          // 🆕 2026-10-18（A1549 尾巴）：运行时切档 → 已建好的卡跟上
        AssertFaithCostIcon();            // 🆕 2026-10-19（A2140）：信仰费裸词 `4 Energy:` → `4〔金太阳〕:`（desc 不动）
        AssertCardAnimOriginal();         // 🆕 2026-10-22（W64）：动画/材质里的近似 → 原版真值（B02/B04/B05/B06/B07/B08）

        // ---- 4c. 🆕 2026-10-12（`A1658` / `A1659` / `A1665`）：**卡面层级三项**的断言 ----
        //   为什么挂这儿：那三条全是「**层与层之间的关系**」（用户 2026-10-12 真机上报的三件），
        //   而 `CardFaceProbe.CheckLayers` 那个入口**不在 `工具/_run_8_checks.sh` 的 12 条里**
        //   ⇒ **从本宿主**（在 12 条里）调一次，它们才真的会被跑到（⛔ 别只留在探针里当摆设）。
        //   判据全文 / 逐条改坏法 → `Editor/CardFaceProbe.cs` 的 `CheckLayers` 那一段注释。
        Debug.Log(P + "--- 卡面层级三项（A1658 底板在卡框下 · A1659 文字挂 `2DCard` · A1665 一卡一段队列）---");
        CardFaceProbe.CheckLayers();

        // ---- 4d. 🆕 2026-10-23（D2 · `W63·层级审查` F2）：**场上档特效锚点**的节点/位置 ----
        //   为什么挂本宿主：`CardFaceProbe` 不在 `工具/_run_8_checks.sh` 的 12 条里，
        //   而本宿主在 ⇒ 这几条才真的会被跑到（同 4c 那条注释的理由）。
        Debug.Log(P + "--- 卡上特效锚点（场上档 = 原版 `Board Elements/EffectAnchor`，local (0,1.34,−0.128)）---");
        AssertEffectAnchor();

        // ---- 4e. 🆕 2026-10-23（D4 · F1 Step 2/3）：**3D 卡体 = 不透明档 + Depth/Shadow pass** ----
        Debug.Log(P + "--- 3D 卡体：不透明（Geometry 2000）+ DepthOnly/DepthNormalsOnly/ShadowCaster ---");
        AssertBodyOpaque();

        // ---- 4e·2. 🆕 2026-10-24（W14）：用户「加攻变红」+「插图白边一直闪」两条的画法修复 ----
        //   S2：高亮态不许给整卡（含四个数值）着色 —— 颜色只走状态环（原版 `ChangeFrameColor` 实证）。
        //   S3：场上 3D 卡体的立绘 UV 再内缩到 crop 的子矩形（`Card3D.shader` 的 `artUv`）。
        //   判据全文 → `资料/普查产出_1024d/S1_场上卡牌四连症状.md` §2·4–2·11
        //             与 `W14_整卡染色与白边_执行报告.md`。
        Debug.Log(P + "--- W14 · 整卡染色（数值被高亮色染红）+ 场上插图白边 ---");
        AssertStatTintNotWholeCard();
        AssertBoardArtNoWhiteEdge(cam);

        // ---- 4g. 🆕 2026-10-24（L1 · `C1-B03`）：**描边与软光/影并层**（原版只有 1 颗 `Card Highlight And Shadow`）----
        Debug.Log(P + "--- 描边并层（C1-B03）：绿光走软光/影那份 `Card Frame SDF`，不再有 `outlineGlow` 层 ---");
        AssertOutlineMerged();

        // ---- 4f. 🆕 2026-10-23（D · `W63·层级审查` F4/F6）：**卡上特效随卡排队** + **手牌活动中那一段** ----
        Debug.Log(P + "--- 卡上特效随卡排队（F4）+ 手牌「活动中」档（F6）---");
        AssertEffectQueueFollowsCard();

        // ---- 5. 手牌布局四件（B1 批 2026-10-17）----
        Debug.Log(P + "--- 手牌布局：小屏档触发 · 最近空位 · 选中让位 · 层序 ---");
        AssertHandLayout(cam);

        // ---- 5b. 🆕 2026-10-24（`A3`）：手牌位姿补间 = `CardView.TweenPose` ----
        //   三件：① 角度写的是**局部**角（父链带旋转也不被抵掉）② 缩放走 `_baseScale`
        //   （⛔ 不是 `transform.DOScale` 直写 `localScale`）③ 当场 snap 要掐掉在飞的那条。
        Debug.Log(P + "--- 手牌位姿补间：局部角 / `_baseScale` / 掐补间（A3）---");
        AssertHandPoseTween();

        // ---- 6. 🆕 2026-10-23（战斗线 · 新账-73 / 新账-74）：远程收招第二段 + 缩放走唯一口 ----
        AssertRangedRecoverAndBaseScale();

        ProbeTextBg(cam);                 // 🔍 诊断（`WF_TEXTBG_PROBE=1` 才跑；不设 = 零输出）
        ProbeTextBgDiff(cam);             // 🔍 诊断（`A1934`；`WF_TEXTBG_DIFF=1` 才跑 —— 开/关两帧存 PNG + 逐像素 diff）

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
    /// **立绘与卡框的层级口径**（原账名 `A112`「破框遮罩」，方法名沿用）——「**立绘画在框下、
    /// 由框的 alpha 剪出来，不越出卡框**」这一条口径的判据 + 一张并排图。
    ///
    /// 🔴 **2026-10-10 改**：原来这里断的是「立绘两层」那条路（`UseFrontLayer = true`：前景角色抠图
    /// 盖在卡框上 ⇒ 越出）。用户点名「最严重的还是插图超出卡框」⇒ **真因就是那一层**，已改成出厂关
    /// （判据与出处 → `CardView.UseFrontLayer` 的注释）。**下面的断言跟着换成新口径**：
    ///   ① 出厂**没有**前景层（`ArtFrontTexture` = null、`ArtFrontZ` = NaN）；
    ///   ② opt-in 开关**仍然接得通**（打开时那一层真建出来、位置在卡框**之前**）——
    ///      原版是逐卡 opt-in、值在服务器，所以「口子有没有接丢」要守。
    /// ⚠️ 原来那条「`UseFrontLayer = false` 时卡框材质接成 `FrameCutout`」的接线判据**没有对象了**
    ///    （那条支路已删，见 `CardView` 建层处的注释）⇒ 换成了上面两条。
    ///
    /// 用**真卡**（`Howling Banshee Exarch` / `ASH79`，工程里当尺子的那张）——
    /// 演示场上那 12 张是 `CardData.Placeholder`，立绘没有抠图，那条 opt-in 路根本不会走
    /// （`CardArt.HasCutout` 为假）⇒ 拿它们断出来是空的。
    /// </summary>
    static void AssertFrameCutout(Camera cam)
    {
        RuleEngine.CardDef def = null;      // ⚠️ 全名：`CardDef` / `CardDatabase` 在 `RuleEngine` 命名空间下
        foreach (var c in RuleEngine.CardDatabase.Load()) if (c.Name == "Howling Banshee Exarch") { def = c; break; }
        if (def == null)
        {
            Check(false, "卡池里找不到 `Howling Banshee Exarch` ⇒ 这一档**没验到**（不是通过）");
            return;
        }
        var data = BattleDriver.ToCardData(def, def.Faction);
        Check(CardArt.HasCutout(data.artId),
              $"`{def.Name}` 的立绘**在抠图清单里**（artId = `{data.artId}`）—— 它是 opt-in 那条路上最该走的一张");

        // ① 出厂态：**不建前景层** ⇒ 立绘只有框下那一层（「插图不超出卡框」就是这一条）
        Check(!CardView.UseFrontLayer,
              "出厂 `CardView.UseFrontLayer` = **false** —— 原版默认不越出；"
            + "改回 true 就是复现「插图超出卡框」（判据见 `CardView.UseFrontLayer` 的注释）");
        var r1 = new GameObject("cutout_probe");
        var v1 = CardView.Create(r1.transform, data, "cutout_probe");
        Check(v1.ArtFrontTexture == null,
              "★ 出厂那张卡上**没有 `_artFront` 前景层**（贴图取不到）—— 有它就是「插图超出卡框」又回来了");
        Check(float.IsNaN(v1.ArtFrontZ), "……而且 `ArtFrontZ` = `float.NaN`（那一层根本没建）");
        string d1;
        Check(v1.CheckCutoutUvAlignment(out d1), $"卡框 `uv2` 与立绘 uv **对齐**（{d1}）");
        v1.SetPose(new Vector3(-1.15f, 0.55f, 0f), 0f, 0.62f);

        // ② opt-in 那条路（原版逐卡 opt-in、值在服务器 ⇒ 我们整卡池一起开）：**开关还接得通**
        //    —— 打开时前景层真建出来，而且 z 比卡框(0) **更靠前**（= 角色盖住卡框）。
        //    ⚠️ 静态开关只在**建卡时**读 ⇒ 断完立刻改回来（不还原会把后面所有卡都带上前景层）
        bool prev = CardView.UseFrontLayer;
        CardView.UseFrontLayer = true;
        var r2 = new GameObject("cutout_probe_overdraw");
        var v2 = CardView.Create(r2.transform, data, "cutout_probe_overdraw");
        CardView.UseFrontLayer = prev;
        Check(v2.ArtFrontTexture != null,
              "opt-in（`UseFrontLayer = true`）时前景层**真建出来了**、贴图接得上（接不上 = 那条路静默失效）");
        Check(!float.IsNaN(v2.ArtFrontZ) && Mathf.Abs(v2.ArtFrontZ - (-0.008f)) < 1e-4f,
              $"……而且 `ArtFrontZ` = **−0.008**、比卡框(0) **更靠前**（实测 {v2.ArtFrontZ.ToString("F4")}）"
            + " —— 这就是「角色轮廓盖住卡框、剑尖/肩甲出拱顶」");
        v2.SetPose(new Vector3(1.15f, 0.55f, 0f), 0f, 0.62f);

        // 并排图：左 = 出厂（不越出），右 = opt-in（越出 —— 原版默认不做的那件事）
        Debug.Log(P + "  [并排图] 07_破框并排：**左 = 出厂**（`UseFrontLayer=false`：立绘在框下、"
                    + "由框的 alpha 剪出来 ⇒ **不越出**）· **右 = opt-in**（`UseFrontLayer=true`："
                    + "角色抠图盖在框上 ⇒ 剑尖/肩甲出拱顶）—— 看两件事："
                    + "① 左边**没有**任何一层画到卡框之外（用户 2026-10-10 点名的那条）"
                    + "② 右边那一层就是原版**默认不做**的那件事");
        Shot(cam, 1920, 1080, "07_破框并排");

        Object.DestroyImmediate(r1);
        Object.DestroyImmediate(r2);
    }

    /// <summary>🆕 **2026-10-24（K1 · `新账-66` alpha 口径收口）**：立绘透过卡框半透明处的份额 =
    /// **`1 − α框`（一次乘法）**，⛔ 不是 `(1−α)²`。
    ///
    /// <para>判据（一手 · 反编译 + 解包 + 层序，全文 → `CardView.ArtVisibleThroughFrame` 的 doc）：
    /// ① 原版 `Front` 子序 = `CardImage(2) → Textbackgrounds(3) → CardFrame(4)` ⇒ **立绘在下、卡框在上**；
    /// ② 卡框用标准 alpha-over（`Everguild/Card ImageUI` / UI 默认材质 = `SrcAlpha OneMinusSrcAlpha`）；
    /// ③ 全 prefab 唯一那颗 `SoftMaskScript` 的 `AlphaMask` 序列化值 = **null**（默认白 ⇒ α_mask=1 ⇒ 输出 0），
    ///    它只把**自己那颗 Graphic**（CardFrame / textBackground）整层淡没，**不碰立绘**；
    /// ④ `CardImage` 与 `CardFrame` 是**兄弟**（不是父子）⇒「立绘被乘两次」的前提不存在。</para>
    ///
    /// <para>🧨 **改坏法（三条都要能红）**：
    /// · 把 `CardView.ArtVisibleThroughFrame` 改成平方 ⇒ 第 ① 组数字红（0.5 → 0.25、0.9 → 0.01）；
    /// · 把立绘层挪到卡框**上面**（`ArtZ` 不再大于 `FrameZ`）⇒ 第 ② 条红；
    /// · 给立绘换成吃贴图 alpha 的材质（「立绘逐顶点 alpha」那条平方改法）⇒ 第 ③ 条红。</para>
    /// </summary>
    static void AssertArtAlphaThroughFrame()
    {
        // ---- ① 公式：期望值直接来自「立绘在下、框在上」的标准 alpha-over 合成式 ----
        //   框 α=0.5 ⇒ 立绘露 0.5 · α=0.9 ⇒ 0.1 · α=0 ⇒ 1 · α=1 ⇒ 0 · α=0.1 ⇒ 0.9。
        //   ⚠️ 期望值是**常数**（不是把实现再算一遍）；非对称点 0.9 专挡「0.5 处恰好也对」的凑数改法。
        var cases = new[]
        {
            new Vector2(0.5f, 0.5f), new Vector2(0.9f, 0.1f), new Vector2(0.1f, 0.9f),
            new Vector2(0f, 1f),     new Vector2(1f, 0f),
        };
        foreach (var c in cases)
        {
            float got = CardView.ArtVisibleThroughFrame(c.x);
            Check(Mathf.Abs(got - c.y) < 1e-4f,
                  $"alpha 口径：框 α={c.x:0.###} ⇒ 立绘露 **{c.y:0.###}**（实测 {got:0.####}）"
                + " —— 平方/反相/取 β 都会在这一组红（判据 → `CardView.ArtVisibleThroughFrame`）");
        }

        // ---- ② 层序：立绘必须在卡框**后面**（本仓 z 越小越靠前）----
        // ---- ③ 立绘层必须**不吃贴图 alpha**（`ArtOpaque`：α 当 1）—— 这是上式退化成 `1−a_框` 的前提 ----
        RuleEngine.CardDef def = null;
        foreach (var c in RuleEngine.CardDatabase.Load())
            if (c.Name == "Howling Banshee Exarch") { def = c; break; }
        if (def == null) { Check(false, "卡池里找不到 `Howling Banshee Exarch` ⇒ ②③ 两条**没验到**（不是通过）"); return; }
        var data = BattleDriver.ToCardData(def, def.Faction);
        var root = new GameObject("art_alpha_probe");
        var v = CardView.Create(root.transform, data, "art_alpha_probe");
        Check(!float.IsNaN(v.ArtZ) && !float.IsNaN(v.FrameZ) && v.ArtZ > v.FrameZ,
              $"立绘层在卡框**后面**（ArtZ={v.ArtZ:0.###} · FrameZ={v.FrameZ:0.###}）—— 反了就不是「按 `1−α` 透出」，"
            + "而是整块盖住卡框");
        Check(v.ArtShaderName == "CardPresentation/ArtOpaque",
              $"立绘层走 `CardPresentation/ArtOpaque`（**忽略贴图 alpha**，实测 `{v.ArtShaderName}`）"
            + " —— 换成吃 alpha 的材质就是「立绘逐顶点 alpha」那条平方改法");
        Object.DestroyImmediate(root);
    }

    /// <summary>战术卡硬矩形自检的**卡框档位**（`AssertTacticArtClamp` 用；0 = 不覆盖 ⇒ 走玩家存档）。
    /// 为什么必须是 4：`astramilitarum_strat_tier1` 的开口整个落在硬矩形里（见该方法的离线表）。</summary>
    const int TacticArtClampProbeTier = 4;
    /// <summary>③ 灭自证的**最小越出量**（卡单位）：0.02 ≈ 6 px（682 px 宽的卡框上 1 px ≈ 0.00329）。</summary>
    const float TacticArtClampOverMin = 0.02f;
    /// <summary>🆕 **2026-10-24（`E5`）**：硬矩形两端 / 立绘板边这两条**精确期望值**的容差（卡单位）。
    /// 取 1e-3 ≈ 0.3 px（682 px 宽的卡框上 1 px ≈ 0.00329），而 float 端到端误差**离线按 float32 复算** ~1e-7
    /// （`0.32×2.7484×1.04×0.61` 与 `-w/2 + 0.82·w` 两条算路对同一端差 0.0 / 5.96e-8）
    /// ⇒ 余量 ~4 个数量级，不处在抖动带里。⛔ 别为了「让它绿」把这里放大 —— 放大到 0.12 就等于退回旧区间断言。</summary>
    const float ArtClampEdgeTol = 1e-3f;
    /// <summary>④ 「y 窗口未被收窄」的**最小上沿余量**（卡单位）：小图档的上沿必须比 BIG 档高出这么多。
    /// 0.05 是**我挑的判别阈**（不是原版参数）—— 离线实测两张样本分别高出 0.1427 / 0.2145 ⇒ 余量 2.9× / 4.3×，
    /// 不处在抖动带里；BIG 的 v 上沿 = `yB0 + 0.89·h`（见 `CardView.ClampRangeBig`）。</summary>
    const float TacticArtClampYMin = 0.05f;

    /// <summary>🆕 **2026-10-24（H1）**：战术 / 防御卡的**生产立绘**（`_art` 底层）也套原版硬矩形 `_Clamp_Range`。
    ///
    /// <para>判据（一手）：`CardImageController__SetImage.c` 的 `Tactic(0x14)` 分支（`:126-137`）**只关 `frameMask`**
    /// （`AlphaMask = null` + `enabled = false`），而 `Image.sprite = sprite` 在 `:174-177`、**对所有卡型无条件执行**；
    /// `ChangeState` / `DoChange` 对卡类型没有任何判断 ⇒ 硬矩形对战术/防御卡同样生效
    /// （判据全文 → `资料/普查产出_1024/F4_卡面条带与断言.md` §三）。原版战术卡**只有** `CardImage` 这一层立绘
    /// ⇒ 我们的等价层是 `_art`（⛔ 不是 `_artFront`）。</para>
    ///
    /// <para>⚠️ 期望值来源（**不是**把实现再算一遍）：`_Clamp_Range` SMALL = `(0.82, 1, 0.18, 0)` ⇒ u 半宽 **0.32**；
    /// 1024² 战术立绘的板宽 = 原版 `CardImage` 方形 **2.7484** × 我们的 `ArtCoverMargin` **1.04**
    /// × **卡型倍率**（`CardScalesByCardType` 的 `20 → 0.61`，见 `CardView.TacticArtScale`）
    /// ⇒ 硬矩形两端 x = **±0.32 × 2.7484 × 1.04 × 0.61 = ±0.557947** 卡单位。
    /// 🔴 **2026-10-24（`E9`）订正**：本行原写「±0.32 × 2.7484 × 1.04 = ±0.914668」（没有 0.61）——
    /// 那是「战术立绘是满幅方图」那个**既有偏离**还在时的读数；`E9` 接上原版 `cardScales` 之后板宽缩到
    /// **1.743585**（= 2.7484 × 1.04 × 0.61），硬矩形端点跟着按比例落到 **±0.557947**（⛔ 不是把 0.9147 留下来）。</para>
    ///
    /// <para>🔴 **2026-10-24（H4）卡框档位钉在 `TacticArtClampProbeTier = 4`，而且 ①/③ 两次渲染都钉住。**
    /// 原来 H1 钉的是 tier2，却在 ① 渲完之后**立刻**把 `DebugFrameTier` 还原了 ⇒ ③ 那一版渲在
    /// `CardProgress.Level` 的**缺省档 tier1** 上；而 `frame_astramilitarum_strat_tier1` 的开口
    /// 实测 x ∈ [−0.8362, +0.8395]，**整个落在硬矩形里** ⇒ ③ 结构性红（那不是「实现坏了」，是「测错了情况」）。
    /// 🔴 **2026-10-24（第七十三会话 E12）就地订正 H4 那张 x 表（铁律 5）**：H4 原文写
    /// `tier2 [−1.0403, +0.9942] · tier3 [−1.0370, +0.8395] · tier4 [−1.0463, +1.0463]` ——
    /// **那三个 x 数偏宽，量错了口径**：它把卡框左右那几处**装饰镂空**（独立的小透明连通域，
    /// 例 astra tier4 的 `cols 193..216 / 808..832`）也算成了「开口」；而工程里真正的画窗是
    /// **最大那一块开口**（`FrameMaskData` 的 `hole` 里面积最大的连通域）∪ 与它相连的半透明内斜面
    /// ∪ `FrameArtBleedPx = 3` 外扩。按该口径重算（离线，逐像素，⛔ 不读 C# 实现）：
    /// `tier1 [−0.8362, +0.8395]`（= H4 自己那一行，与 H1 运行时逐位相同 ✓）·
    /// `tier2 / tier3 / tier4` 全是 **[−0.8395, +0.8395]**（贴图列 257..766）。
    /// **交叉验证**：同一份口径算出的 tier4 y 窗口 `[−0.2585, +1.2135]` 与运行时 ④ 的读数逐位相同，
    /// 而 tier4 x `[−0.8395, +0.8395]` 也与运行时 ③ 的读数逐位相同 ⇒ 口径对、H4 的 x 表错。
    /// ⇒ **tier4 的开口比立绘板（±0.871792）窄**，所以 ③b 的极值由**画窗边**决定（不是板边）。
    /// ⚠️ 窄开口的框会「夹了等于没夹」⇒ 那是空验，所以 ③ 用**越出量 ≥ 0.02 卡单位**反证这一点
    /// （不是「① 不成立」—— 那太弱，见 ③ 的注释）。
    /// 判据表 → `资料/普查产出_1024/H4_战术卡断言样品与档位.md`（⚠️ 其中 x 一列以**本条注释**为准）；
    /// 卡框档位与存档解耦的事实 → `H3_卡面断言翻面.md` §一。</para>
    ///
    /// <para>🔴 **2026-10-24（第七十三会话 `M2`）判据翻面**：`_art` 的硬矩形档位现在**恒 0**（原版实况：
    /// 战术/防御卡插图铺满拱窗、无黑边 —— 判据四条 → `CardView.ArtClampKindFor` 的 doc）。
    /// ⇒ 本方法断的是「**插图铺满画窗**（①）+ **没被硬矩形削**（①b）+ 外壁精确值（③b，仅作对账）」，
    /// ⛔ 不再断「两端压在 ±0.557947 上」（那是 `H1` 那一版的口径，已被证伪）。
    ///
    /// <para>🧨 **改坏法**（`M2` 现行）：`ArtClampKindFor` 返回非 0 ⇒ ① 与 ①b 同时红；
    /// 把单位卡也接上 `clampKind` ⇒ ② 红；`cardScales 0.61` 丢了 / 边距改错 ⇒ ①②③b 都红；
    /// ③b 的逐样本期望与卡框档位绑死（见 `AssertArtClampForSample` 那条 `FrameTexture.name` 前提断言）。</para>
    ///
    /// <para>🔴 **2026-10-24（H6）补两个覆盖缺口**（此前「今天绿、但挡不住某类回归」）：
    /// ① **防御卡样卡** —— ①③ 原来只跑战术卡；防御卡（引擎 `type="defence"`；🔴 **2026-10-24 订正（铁律 5 · 主对话现读）**：SO `cardType` 也是 **20**、与战术卡同值 —— `cards_truth.json` 全表只有 **0/10/20**，**没有 5**）走同一条 `!d.isUnit` 分支
    ///    但**没有样卡**。现在「战术卡 + 防御卡」各一张，逐张跑 ①③④（样本与选型依据见
    ///    <see cref="AssertArtClampForSample"/> 的 doc）。
    /// ② **y 轴** —— ①③ 原来**只看顶点 `p.x`**，挡不住「只夹 x、y 被误夹」或「y 根本没夹」。
    ///    现在每个样本多一条 ④b：小图档 `_art` 的 **y 极值不许比 BIG 档（`clampKind = 2`）窄**
    ///    （上沿须高出 ≥ `TacticArtClampYMin`；下沿只作方向闸，理由见 ④ 的注释）。
    ///    ⚠️ 这条**不是**原版参数：小图档的 v 窗口就是 `(0,1]`（`ClampRangeSmall` 的第 2/4 分量），
    ///    「没被收窄」是从那里推出来的期望，判别阈 0.05 是**我们挑的**。
    ///    🔴 **2026-10-24（`W4` · T01）**：相对比较**挡不住「两档按同一错误比例一起缩」** ⇒ 另补 ④a
    ///    把 BIG 的 y 上沿**钉成绝对期望**（`cy − h/2 + 0.89·h`，原版字面量；见 `AssertArtClampForSample`）。</para>
    /// </summary>
    static void AssertTacticArtClamp()
    {
        Debug.Log(P + "--- 战术 / 防御卡立绘：铺满画窗、不吃 _Clamp_Range（M2 · 原 H1/H6）---");
        const float HardHalfW = 0.32f * 2.7484f * 1.04f * 0.61f;      // = 0.557947 卡单位（见上面期望值那一段）

        int prevTier = CardView.DebugFrameTier;
        bool prevOff = CardView.DebugDisableArtClamp;
        try
        {
            // 🔴 卡框档位钉到 `TacticArtClampProbeTier`(4)，并且**一直钉到两张样本都验完**才还原
            //   —— `DebugFrameTier` 这类全局覆盖必须在所有相关渲染都做完之后再还原（H4 踩过：
            //   ① 渲完就还原 ⇒ ③ 落在缺省档 tier1、开口整个在硬矩形里 ⇒ 结构性红）。
            CardView.DebugFrameTier = TacticArtClampProbeTier;
            // 逐样本的**画窗边**（字面量 · 口径 = 最大开口连通域 ∪ 半透明内斜面 ∪ 3 px 外扩，见上方 doc）：
            //   AM52  ← `frame_astramilitarum_strat_tier4` 贴图列 **257..766**（`FramePxXEdge` 映射后
            //           = −0.839481 / +0.839481，左右对称）⇒ 比立绘板 ±0.871792 **窄**
            //   SOR59 ← `frame_sororitas_strat_tier4` 贴图列 **255..769** ⇒ −0.846065 / +0.849357（左右不对称）
            // ⚠️ 这两个数**不是**从运行时的 ③b 读数抄来的（铁律：断言不许自证）：它们由 frame PNG 的像素
            //   按上面那条口径离线算出；跑完后与运行时读数逐位一致只是**交叉验证**。
            AssertArtClampForSample("Armoured Squadron", "战术卡 `Armoured Squadron`（AM52）", "tactic", HardHalfW,
                                    -0.839481f, 0.839481f, "frame_astramilitarum_strat_tier4");
            AssertArtClampForSample("Sacred Altar", "防御卡 `Sacred Altar`（SOR59）", "defence", HardHalfW,
                                    -0.846065f, 0.849357f, "frame_sororitas_strat_tier4");
            CardView.DebugFrameTier = prevTier;
        }
        finally { CardView.DebugFrameTier = prevTier; CardView.DebugDisableArtClamp = prevOff; }

        // ---- ② 单位卡：生产网格**逐顶点不变**（= 未套硬矩形那一份），而且 ≠ 小图档那一份 ----
        //   ⚠️ 这一段不读卡框档位（`clipWindow = false` 时 `frameTex` 压根不参与），仍按原档位跑。
        var goUnit = new GameObject("h1_unit_ref");
        try
        {
            RuleEngine.CardDef udef = null;
            foreach (var c in RuleEngine.CardDatabase.Load())
                if (c.Name == "Howling Banshee Exarch") { udef = c; break; }
            if (udef == null)
            {
                Check(false, "卡池里找不到 `Howling Banshee Exarch` ⇒ ② 单位卡**没验到**（不是通过）");
            }
            else
            {
                var udata = BattleDriver.ToCardData(udef, udef.Faction);
                var uv = CardView.Create(goUnit.transform, udata, "h1_unit_ref");
                var um = uv.ArtLayerMesh;
                var refOff = CardView.ArtMeshForTest(uv.ArtLayerTexture, 1f, uv.FrameTexture, false, 0);
                var refSmall = CardView.ArtMeshForTest(uv.ArtLayerTexture, 1f, uv.FrameTexture, false, 1);
                Check(um != null && MeshSame(um, refOff),
                      "② 单位卡的**生产**立绘网格 == 未套硬矩形那一份（逐顶点 / 逐 uv 比）"
                    + "｜🧨 把 `clampKind` 也接到单位卡 ⇒ 与 `clampKind=0` 那份对不上 ⇒ 必红");
                Check(!MeshSame(um, refSmall),
                      "……而且 **≠** 小图档（`clampKind=1`）那一份 —— 反证上一条不是「两份恰好相等」的空验"
                    + "（单位卡 671×1024 的小图档窗口 u (0.0117, 0.9883] 会真的窄一截）");
            }
        }
        finally { Object.DestroyImmediate(goUnit); }
    }

    /// <summary>🆕 **2026-10-24（H6）**：对**一张**样本卡跑立绘几何的六条断言
    /// （① x 极值**铺满卡框画窗**（覆盖，离线字面量）· ①b 两端**越出**硬矩形 ≥ 0.02（没被 `_Clamp_Range` 削）·
    /// ③ 关档后确实越界 ≥ 0.02 · ③b 关档后 x 正好压在**「立绘板边 / 卡框画窗边」里更窄的一条**上
    /// —— 🔴 2026-10-24 第七十三会话 E12 改：原来只断「板边」，而这两张样本的画窗比板窄 ·
    /// ④a y 上沿**绝对期望**（`cy − h/2 + 0.89·h`，原版字面量 —— 🔴 2026-10-24 `W4`/T01 新增）·
    /// ④b y 窗口未被收窄（相对墙）· ＋ 一条 `FrameTexture.name` 前提断言（卡框档位真的钉住了））。
    /// 🔴 **2026-10-24（`W4` · T01）**：④ 原来**只有相对比较** ⇒ 挡不住「两档按同一错误比例一起缩」
    /// （`TacticArtScale` / `ArtCoverMargin` 改错、或 BIG 的 v 上沿改错都仍绿）⇒ 拆成 ④a 绝对 + ④b 相对。
    ///
    /// <para>**为什么要两张样本**：战术卡（`cardType = 20`）与防御卡（🔴 现读 SO `cardType` **也是 20**、靠引擎 `type="defence"` 区分；`cards_truth.json` 无 5）在 `CardView` 里走
    /// **同一条** `!d.isUnit` 分支（`ArtClampKindFor`），原版判据（`CardImageController__SetImage.c:126-144`）
    /// 也只核过 tactical 那一支的参数；**防御卡此前没有任何样卡**（`H1` §五·6 / `H3` §四·3 / `H4` §四·5 都记着这个缺口）
    /// ⇒ 逻辑上同一条，但**没有证据**。这里各补一张，⛔ 不是拿战术卡顶替（`def.Type` 逐字断言）。
    /// 卡池现读 `type = "defence"` **39 张**（`cards_engine.json`）。</para>
    ///
    /// <para>🔑 **防御卡样本 = `Sacred Altar`（SOR59 · Sororitas · `art_sor59.png` 实测 1024²）**：
    /// 与战术卡样本**不同阵营**（独立卡框资产）⇒ ①③④ 对两张样本都有自己的判别力，能排除
    /// 「这张框恰好够宽」。选型依据原文引用过 H4 那张 13 阵营 `strat_tier4` 的 x 表
    /// （`astramilitarum [−1.0463,+1.0463]` 等）—— 🔴 **那张表的 x 数已作废**（口径把卡框两侧的
    /// 装饰镂空也算成开口，见 `AssertTacticArtClamp` doc 里那段订正）；按正确口径（最大开口连通域
    /// ∪ 半透明内斜面 ∪ 3 px 外扩），13 个阵营的 `strat_tier4` 画窗半宽落在 **0.77~0.86** 之间
    /// （本批只逐像素量了两张样本：astra `[−0.8395,+0.8395]` · sororitas `[−0.8461,+0.8494]`；
    /// 其余 11 个阵营**本轮没量**，若要用它们当样本须先按同一口径量）。</para>
    ///
    /// <para>🧨 **改坏法**（任一都能让本样本红）：`clampKind` 改回 0 ⇒ ③ 越出量够、但 ① 会到 ±1.04 ⇒ ① 红；
    /// 卡框档位没钉住（退回 tier1/2）⇒ `FrameTexture.name` 那条先报出来；
    /// 把小图档的 v 窗口换成 BIG 的 ⇒ ④ 红；③b 的清单见那一段（全仓只一处）。</para>
    /// <para>🔴 **2026-10-24（`E5`）① 从「区间」改成「两端精确贴边」**：旧写法只挡「越出」，
    /// 硬矩形**收窄**（含误用 BIG 档 `(0.75,0.89,0.25,0.1)` ⇒ ±0.4359）、网格塌成零宽、只夹一侧，
    /// 这三种改坏**都绿**。现在断 `|x − (±0.557947)| ≤ 1e-3`（期望值 = 原版 `_Clamp_Range` 的 `0.82/0.18`
    /// × `CardImage` 方框 2.7484 × 本工程边距 1.04 × 原版 `cardScales[20]` 0.61），
    /// 并补 ③b 把**外壁**也钉成精确值（立绘板 ±0.871792）⇒ 内壁/外壁两条精确墙。</para>
    /// </summary>
    /// <param name="expectType">卡类原文（`cards_engine.json` 的 `type`：`"tactic"` / `"defence"`）——
    /// 逐字断，⛔ 别用「非单位卡」糊过去（那样战术卡也能顶替防御卡样本）。</param>
    /// <param name="expWinL">这张卡的卡框**画窗左沿**（卡单位；离线按 frame PNG 的像素量得，见调用点的出处）。
    /// ③b 的期望 = 「立绘板边 / 画窗边 **取更窄者**」⇒ 本值比 `-hardHalfW` 更靠近 0 时它就是束缚。</param>
    /// <param name="expWinR">同上，右沿。</param>
    /// <param name="expectFrameName">本样本**必须**渲在这张卡框上（`FrameTexture.name`）。
    /// 加它是因为「档位没钉住」以前会**伪装成 ③b 的板边不符**（H4 那一轮就是这么查错方向的）。</param>
    static void AssertArtClampForSample(string cardName, string what, string expectType, float hardHalfW,
                                        float expWinL, float expWinR, string expectFrameName)
    {
        RuleEngine.CardDef def = null;
        foreach (var c in RuleEngine.CardDatabase.Load())
            if (c.Name == cardName) { def = c; break; }
        if (def == null || def.Type != expectType)
        {
            Check(false, $"卡池里找不到 `{cardName}` 或它的卡类不是 `{expectType}`（实测 `{(def == null ? "<缺卡>" : def.Type)}`）"
                       + $" ⇒ {what} **没验到**（不是通过）");
            return;
        }
        var data = BattleDriver.ToCardData(def, def.Faction);
        Check(!data.isUnit, $"★ 前提：{what} 是**非单位卡**（`isUnit == false`，实测 {data.isUnit}）"
                          + " —— 单位卡走的是另一条（`_art` 不套、`_artFront` 套）");

        string tag = cardName.Replace(' ', '_');
        var goOn = new GameObject("h6_on_" + tag);
        var goOff = new GameObject("h6_off_" + tag);
        bool prevOff = CardView.DebugDisableArtClamp;
        try
        {
            // ---- ① 生产档（硬矩形开）：x **两端各压在硬矩形的一条边上**（不是「落在区间里」）----
            //   ⚠️ `DebugFrameTier` 由调用方钉在 `TacticArtClampProbeTier`，**一直钉到本方法跑完**。
            //
            //   🔴 **2026-10-24（`E5`）判据收紧 —— 旧断言为什么是弱区间**：
            //   旧写法 = `x0 ≥ -hardHalfW - 1e-3 && x1 ≤ hardHalfW + 1e-3`（**只挡越出**）。
            //   它能挡「硬矩形没接上/接大了」，但**完全挡不住「收进来」** —— 下面这些改坏它都绿：
            //     · `ClampRangeSmall` 误用 BIG 那条 `(0.75,0.89,0.25,0.1)` ⇒ x 极值 **±0.4359**（⊆ 区间 ⇒ 绿）；
            //     · 硬矩形被收窄任意一档（例：u 取 0.20..0.80 ⇒ ±0.5232）⇒ 绿；
            //     · 网格塌成零宽 / 只剩一根竖线（`x0 == x1 == 0`）⇒ 绿；
            //     · 只夹一侧、另一侧被别的约束（卡框开口 / 卡体）截住 ⇒ 只要不越界就绿。
            //   ⇒ 现在断的是**两件事**（🔴 `M2` 起）：**(a) 铺满画窗**（①）、**(b) 没被硬矩形削**（①b）。
            //      ⚠️ 历史：`H1`~`E9` 那几版断的是「两端各压在 ±0.557947 上」—— 那个口径建立在
            //      「战术卡 `_art` 也套 `_Clamp_Range`」上，**已被原版实况证伪**（见 `CardView.ArtClampKindFor`）。
            //
            //   🔑 **期望值来源**（⛔ 本方法**不读** `ClampRangeSmall` / `ArtBoard` / 生产网格的任何数 ——
            //      期望是写死的**原版字段组合**，所以「实现改错 + 期望跟着变」这条自证路不存在）：
            //      · 硬矩形 SMALL 的 x/z 分量 = 原版 `_Clamp_Range` 的 **0.82 / 0.18**
            //        （判据 → `CardView.ClampRangeSmall` 的 doc：材质默认 `(0.82,1,0.18,0)`）⇒ 半宽 **0.32**（贴图 UV 口径）；
            //      · 立绘板宽 = 原版 `CardImage` 方框 **2.7484** × 我们的 `ArtCoverMargin` **1.04**
            //        × 原版 `CardScalesByCardType[20]` **0.61** = **1.743585** 卡单位（1024² 方图 ⇒ u 线性映射到 x）；
            //      · ⇒ 硬矩形两端 x = **±0.32 × 2.7484 × 1.04 × 0.61 = ±0.557947**（= 形参 `hardHalfW`）。
            //      ⚠️ 1.04 是**我们自己**的边距（`ArtCoverMargin`，不是原版字段）；另两项是原版字面量。
            //
            //   🥪 **为什么这条网格的束缚一定落在硬矩形上**（否则「精确等边」这个期望本身就不成立）：
            //     关档后的束缚取「立绘板边 / 画窗边」里**更窄**的那一条（判据与逐样本数值见 ③b 那段，
            //     ⛔ 别在这儿再写第二份）：两张样本的立绘板边都是 ±0.871792，硬矩形 ±0.557947
            //     ⇒ 两条候选**都宽于** ±0.557947 ⇒ 只有硬矩形能截住它。
            //     ⚠️ 换样本/换档位时要重判：**外壁比硬矩形窄**时两端就由外壁决定 —— 那时期望要按外壁改
            //        （⛔ **不是**把这里放宽回区间）。这正是本工程记过的坑：**条带路的 x 极值 ≠ 硬矩形边**。
            //
            //   🧨 **改坏必红**：上面清单里的四种「收进来」改坏现在全部红；
            //      **灭自证**的另一半在 ③b（那里把关档后的 x 极值钉在「板边 / 画窗边取更窄者」上，
            //      🧨 清单已**收在 ③b 一处**，⛔ 别在这儿再抄一份）。
            var vOn = CardView.Create(goOn.transform, data, "h6_on");

            // ★ 前提（🔴 **2026-10-24 第七十三会话 E12 新增**）：**卡框档位真的钉住了**。
            //   以前没有这一条：`DebugFrameTier` 万一没生效（退回 tier1/2，或 `CardArt.Frame` 逐级退回），
            //   症状只会是「③b 的 x 极值比板边窄」⇒ 查红的人从「板宽是不是错了」一路查错方向（H4 那一轮就是）。
            //   `FrameTexture` 是公开读口（`CardView.cs:1140`），拿名字就够了。
            Check(vOn.FrameTexture != null && vOn.FrameTexture.name == expectFrameName,
               $"★ 前提：{what} 渲在**指定的卡框档**上（`FrameTexture.name` 应为 `{expectFrameName}`，"
             + $"实测 `{(vOn.FrameTexture == null ? "<null>" : vOn.FrameTexture.name)}`；"
             + $"`DebugFrameTier={CardView.DebugFrameTier}`）"
             + "｜🧨 `DebugFrameTier` 那句挪走/改成 0 ⇒ 本条立刻红（而 ③b 会以「板边不符」的错方向红）"
             + "｜🧨 `CardArt.Frame` 找不到 tier4 资产时**逐级退回 tier1**（静默）⇒ 本条红");

            var mOn = vOn.ArtLayerMesh;
            Check(mOn != null && mOn.vertexCount >= 4,
                  $"★ 前提：{what} 上**建出了** `_art` 底层（`ArtLayerMesh` 非空、≥ 4 顶点）"
                + " —— 建不出来 ⇒ 下面几条**没验到**，不是通过");
            if (mOn != null && mOn.vertexCount >= 4)
            {
                float x0 = float.MaxValue, x1 = float.MinValue;
                foreach (var p in mOn.vertices) { if (p.x < x0) x0 = p.x; if (p.x > x1) x1 = p.x; }
                // 🔴 **2026-10-24（第七十三会话 `M2`）判据翻面**：`_art` 的硬矩形档位现在**恒 0**
                //    （原版实况：战术/防御卡插图铺满拱窗、无黑边；判据四条 → `CardView.ArtClampKindFor` 的 doc）。
                //    ⇒ ① 不再断「两端压在硬矩形边上」，改断**两件更有用的事**：
                //      (a) **铺满画窗**：x 极值**覆盖**（不是「等于」，那留给 ③b）离线量得的画窗边
                //          —— 这正是用户报的「插图不覆盖卡框上面空着的地方」那一条的验收判据；
                //      (b) **没被硬矩形削**：两端都要**越出** ±`hardHalfW` 至少 `TacticArtClampOverMin`
                //          —— 谁把 `clampKind` 改回 1/2，两端会退到 ±0.5579 ⇒ 这一条立刻红。
                float winL = expWinL, winR = expWinR;   // 离线字面量（调用点有出处），⛔ 不读实现
                Check(x0 <= winL + ArtClampEdgeTol && x1 >= winR - ArtClampEdgeTol,
                      $"① {what} **生产**立绘的 x 极值**铺满卡框画窗**"
                    + $"（实测 [{x0:F4}, {x1:F4}] 必须覆盖 [{winL:F4}, {winR:F4}]，容差 {ArtClampEdgeTol:F4}，"
                    + $"卡框钉 tier{TacticArtClampProbeTier}）"
                    + "｜🧨 立绘板被缩过头（`cardScales` 丢/倍率改错）⇒ 盖不住画窗 ⇒ 红"
                    + "｜🧨 把 `clampKind` 接回 1/2（= `H1` 那版）⇒ 两端退到 ±0.5579、盖不住 ⇒ 红");
                Check(Mathf.Min(-hardHalfW - x0, x1 - hardHalfW) >= TacticArtClampOverMin,
                      $"①b {what} 生产立绘**没被 `_Clamp_Range` 削**：x 两端都**越出**硬矩形 ±{hardHalfW:F4} "
                    + $"≥ {TacticArtClampOverMin:F2}（实测左越出 {-hardHalfW - x0:F4} · 右越出 {x1 - hardHalfW:F4}）"
                    + "｜🧨 `ArtClampKindFor` 返回非 0 ⇒ 两端退到 ±0.557947 ⇒ 红"
                    + "｜🧨 板缩到 <0.5579 以下（倍率改错）⇒ 也红（但通常 ① 先报）");

                // ---- ④ y 窗口未被收窄（H6 新增）：只断 x 极值挡不住「只夹 x、y 被误夹」 ----
                //   小图档的 v 窗口是 **(0,1]**（`CardView.ClampRangeSmall`）⇒ 纵向**一点都不该被夹**；
                //   有判别力的对照是 **BIG 档**（v (0.10,0.89]）。
                //
                //   🔴 **2026-10-24（`W4` · 本线 T01）判据翻面：④ 原来只有一条「相对」比较**
                //     （`y1 > by1 + TacticArtClampYMin`，`by1` 来自**实现自己建出来的** BIG 网格）。
                //     它能挡「两档变得一样」和「小图档自己被夹」，但**挡不住「两档按同一错误比例一起缩/放」**：
                //       · `TacticArtScale` 0.61 改错（例 0.5）⇒ 两档一起缩、差值几乎不变 ⇒ **旧断言仍绿**；
                //       · `ArtCoverMargin` 1.04 改错 ⇒ 同上；
                //       · `ClampRangeBig` 的 `v=0.89` 改成 0.85 ⇒ 只有 `by1` 动、小图档上沿不动 ⇒ **旧断言仍绿**。
                //   ⇒ 现在补一条 **④a 绝对期望**（见下面 `ExpBigTopY`），把 BIG 档一个端**钉死在原版字面量上**；
                //     旧那条相对比较降级成 **④b**（方向闸 / 前提闸：证明这一档的 BIG 上沿确实被硬矩形压住、
                //     而且小图档没有被收窄到 BIG 那条线上）。两条合起来 = 一条绝对墙 + 一条相对墙。
                //
                //   📐 **离线复算（float32，⛔ 本轮没跑 Unity）**：
                //     `h = 2.7484 × 1.04 × 0.61 = 1.74358496` · `cy = −0.044 + 0.5 = 0.456`
                //     ⇒ BIG 上沿 `cy − h/2 + 0.89·h` = **1.1359981** · BIG 下沿 `cy − h/2 + 0.10·h` = **−0.2414340**
                //     ⇒ 小图档（= 画窗）与 BIG 的上沿差：astra **0.0775** · sororitas **0.1493**（都 > 0.05，仍绿）
                //   🔴 **2026-10-24（`W4` · T01）就地订正（铁律 5）**：本段原来写的
                //     「小图档 y [−0.2585,+1.2135] vs BIG [−0.2585,+1.0708]，上沿高 0.1427 / 0.2145」
                //     **是 `E9` 之前的口径**（那时 `h = 2.858336`、`cy = −0.044`，BIG 下沿还是 −1.1873 被画窗截住）。
                //     `E9` 接上 `cardScales 0.61` / `tacticsOffsetPosition +0.5` 之后，**BIG 的上、下沿都改由硬矩形决定**
                //     （画窗下沿 −0.2585 / −0.2878 比硬矩形下沿还低）
                //     —— 判别阈 `TacticArtClampYMin` 0.05 仍在，但**余量不像原注释写的 2.9×/4.3× 那么宽**。
                //     小图档那两个数（画窗边）与 `E9` 无关（画窗是卡框贴图的属性），保留不变；
                //     上沿差也因此从 0.1427/0.2145 缩小到 0.0775/0.1493。
                //   ⚠️ **下沿为什么只作方向闸**：`E9` 之后 BIG 的下沿是**硬矩形**给的（离线 −0.2414340），
                //     理论上也可以钉成绝对值；但那要求「两张样本的画窗下沿都低于它」这一前提有**逐样本实测**，
                //     而现在只有 astra 那一份（−0.2585）被 `E12` 交叉验证过 ⇒ 本轮**只钉上沿**，下沿留作方向闸。
                //     要补下沿的绝对墙：先跑一次 `CardBaseDemo.Run` 收两张样本的 ④ 读数，再钉。
                var artY = vOn.ArtLayerTexture;
                // 🔴 2026-10-24（E9）：战术/防御卡的板 = `× 0.61` + 中心 `+0.5`（原版 `CardScalesByCardType`
                //    / `tacticsOffsetPosition`）⇒ 参考网格必须传同两个值，否则这条比的是「两个不同的口径」。
                var mBigY = artY != null
                          ? CardView.ArtMeshForTest(artY, 1f, vOn.FrameTexture, true, 2,
                                                    CardView.TacticArtScale, CardView.TacticArtDy) : null;
                if (mBigY == null || mBigY.vertexCount < 4)
                {
                    Check(false, $"④ y 对照：{what} 的 BIG 档（`clampKind = 2`）网格**建不出来**"
                               + $"（立绘贴图 `{(artY == null ? "null" : artY.name)}`）⇒ 这一条**没验到**（不是通过）");
                }
                else
                {
                    float y0 = float.MaxValue, y1 = float.MinValue;
                    foreach (var p in mOn.vertices) { if (p.y < y0) y0 = p.y; if (p.y > y1) y1 = p.y; }
                    float by0 = float.MaxValue, by1 = float.MinValue;
                    foreach (var p in mBigY.vertices) { if (p.y < by0) by0 = p.y; if (p.y > by1) by1 = p.y; }

                    // ④a **绝对期望**（T01 新增）：BIG 档 y 上沿 = `cy − h/2 + 0.89·h`。
                    //   🔑 四个组成项**全部是原版字面量**，⛔ 不读 `ClampRangeBig` / `TacticArtScale` / `ArtBoard`
                    //      / `ArtCoverMargin` / 生产网格（读了就是自证）：
                    //        · 2.7484 = 原版 `CardImage` 方框（RT `m_SizeDelta`）
                    //        · 0.61   = 原版 `CardScalesByCardType[20]`（战术/防御卡）
                    //        · 0.89   = 原版 `_Clamp_Range` BIG 的 v 上沿（`CardView.ClampRangeBig` 的 y 分量）
                    //        · −0.044 = 原版 `CardImage` 的 local y（菜单族）· +0.5 = 原版 `tacticsOffsetPosition.y`
                    //        · 1.04   = **我们自己的** `ArtCoverMargin`（既有偏离；改它必须同步改这里）
                    //   ⚠️ 成立前提：**BIG 的上沿由硬矩形决定**（画窗上沿更高）。两张样本 tier4 的画窗上沿
                    //      离线量得 1.2135（astra）/ 1.2853（sororitas），都高于期望 1.1360 ⇒ 前提成立。
                    //      换样本 / 换档位时若画窗更窄 ⇒ 上沿改由画窗给 ⇒ 期望要换成那个画窗值（⛔ 别放宽容差）。
                    const float ExpBoardH  = 2.7484f * 1.04f * 0.61f;                            // 1.74358496
                    const float ExpBoardCy = -0.044f + 0.5f;                                      // 0.456
                    const float ExpBigTopY = ExpBoardCy - ExpBoardH * 0.5f + 0.89f * ExpBoardH;   // 1.1359981
                    Check(Mathf.Abs(by1 - ExpBigTopY) <= ArtClampEdgeTol,
                          $"④a y 绝对期望：{what} BIG 档 `_art` 的 y 上沿**压在原版字面量上**"
                        + $"（实测 {by1:F4} vs 期望 {ExpBigTopY:F4} = {ExpBoardCy:F4} − {ExpBoardH:F4}/2 + 0.89×{ExpBoardH:F4}，"
                        + $"容差 {ArtClampEdgeTol:F4}；BIG y 极值 [{by0:F4}, {by1:F4}]，卡框钉 tier{TacticArtClampProbeTier}）"
                        + " —— 本条挡的是「**两档按同一错误比例一起缩/放**」与「BIG 的 v 上沿被改错」，"
                        + "两类都是旧 ④（只有相对比较）结构性抓不到的"
                        + "｜🧨 `TacticArtScale` 0.61 改成别的值（两档一起缩）⇒ 必红（旧相对判据在这一档是**绿**的）"
                        + "｜🧨 `ArtCoverMargin` 1.04 改错 ⇒ 同上"
                        + "｜🧨 `ClampRangeBig` 的 v 上沿 0.89 改成 0.85 之类 ⇒ 只有本端动 ⇒ 必红（旧相对判据也是绿的）"
                        + "｜🧨 卡框档位没钉住（画窗比硬矩形窄）⇒ 上沿改由画窗给 ⇒ 红（先看上面那条 `FrameTexture.name` 前提）");

                    // ④b **相对墙 / 前提闸**（原 ④，保留）：小图档不得被收窄到 BIG 那条线上。
                    Check(y0 <= by0 + 1e-3f && y1 > by1 + TacticArtClampYMin,
                          $"④b y 窗口未被收窄（相对）：{what} 小图档 `_art` 的 y 极值 [{y0:F4}, {y1:F4}] "
                        + $"**不比 BIG 档 [{by0:F4}, {by1:F4}] 窄**（上沿高出 **{y1 - by1:F4}** ≥ {TacticArtClampYMin:F2} 卡单位；"
                        + $"下沿差 {y0 - by0:F4} ≤ 1e-3）"
                        + "｜🧨 把 BIG 的 v 窗口 (0.10,0.89] 误用到小图档 ⇒ 上沿被压到 ≈BIG 上沿 ⇒ 红"
                        + "｜🧨 小图档自己也去夹 y（v 收窄）⇒ 上沿收进来 ⇒ 红"
                        + "｜⚠️ 下沿只作方向闸（`E9` 之后 BIG 的下沿由硬矩形给，但逐样本的画窗下沿只核过 astra 一份 ⇒ 暂不钉绝对值）"
                        + "｜🧨 BIG 网格建不出来 ⇒ 上一条已报「没验到」");
                }
            }

            // ---- ③ 灭自证（反面）：关掉硬矩形 ⇒ x 极值**确实越出** ±hardHalfW（不是只「不满足 ①」）----
            //   🔴 **2026-10-24（H4）判据翻面**：原来断的是「① 的判据不成立」。那太弱 ——
            //     只要 x 极值差一点点（甚至根本没到矩形边、而是落在矩形**里面**的更窄开口上）也会「不成立」，
            //     H1 那次就是这么红的（tier1 开口 [−0.8362, +0.8395] 整个在硬矩形里）。
            //     现在要求**越出量 ≥ `TacticArtClampOverMin`**（0.02 卡单位 ≈ 6 px）。
            //   🔴 **2026-10-24（`E5`）就地订正（铁律 5）**：本行原写「tier4 实测 0.1317 / 0.1316」——
            //     那是 **E9 之前**的口径（关档极值 = 卡体收边 1.04635、硬矩形 0.914668 ⇒ 差 0.1317）。
            //   🔴 **2026-10-24（第七十三会话 E12）再订正**：本行接着又写过「越出量应为 **0.313845**
            //     （= 板边 0.871792 − 硬矩形 0.557947）」—— **那个数也错**：关档后的极值**不是**板边，
            //     而是**卡框画窗边**（这两张样本的画窗 0.8395 / 0.8494 都比板 0.871792 **窄**，见 ③b）。
            //     按正确口径：astra **0.839481 − 0.557947 = 0.281534** · sororitas **0.849357 − 0.557947 = 0.291410**
            //     （运行时实测越出量 0.2815 / 0.2914，与这两个离线数逐位吻合）。
            //     ⇒ 越出量本身仍只作「≥ 0.02」的粗闸 —— 精度由 ① / ③b 两条精确墙负责。
            //   ⚠️ `DebugFrameTier` 在 ① 之前就钉住了、到这里**仍然钉着**（这才是和 ① 同一个卡框档）。
            CardView.DebugDisableArtClamp = true;
            var vOff = CardView.Create(goOff.transform, data, "h6_off");
            CardView.DebugDisableArtClamp = prevOff;

            var mOff = vOff.ArtLayerMesh;
            if (mOff == null || mOff.vertexCount < 4)
            {
                Check(false, $"③ 灭自证：{what} 关掉硬矩形后那块网格**建不出来** ⇒ 反面这次没验成（不是通过）");
            }
            else
            {
                // 🔴 **2026-10-24（第七十三会话 `M2`）**：`_art` 已**恒不套**硬矩形 ⇒ 这里的「关档」
                //   与生产态**是同一块网格**。本条判据仍然留着，语义变成：
                //   **生产立绘确实越出硬矩形 ±hardHalfW ≥ 0.02**（= 硬矩形没被接回去）。
                //   ⚠️ 它与 ①b 同族但方向互补：①b 看两端**各自**越出，本条看**最大**越出量（挡「塌成一根线」）。
                float o0 = float.MaxValue, o1 = float.MinValue;
                foreach (var p in mOff.vertices) { if (p.x < o0) o0 = p.x; if (p.x > o1) o1 = p.x; }
                float over = Mathf.Max(Mathf.Abs(o0) - hardHalfW, o1 - hardHalfW);
                Check(over >= TacticArtClampOverMin,
                      $"③ 灭自证：{what} 生产立绘的 x 极值**确实越出** ±{hardHalfW:F4}"
                    + $"（实测 [{o0:F4}, {o1:F4}]，最大越出 **{over:F4}** ≥ {TacticArtClampOverMin:F2} 卡单位；"
                    + $"卡框钉 tier{TacticArtClampProbeTier}）"
                    + " —— 这一条同时证明样品非空验：这张卡框的开口真的比硬矩形宽"
                    + "｜🧨 把 `clampKind` 接回 1/2 ⇒ 极值退到 ±0.5579 ⇒ 越出量不够 ⇒ 红"
                    + "｜🧨 档位没钉住（退回窄开口的 tier1/2）⇒ 越出量不够 ⇒ 红");

                // 🥪 ① 的**外壁**（🆕 2026-10-24 `E5`；🔴 2026-10-24 第七十三会话 E12 改成「两条边取更窄者」）：
                //    关掉硬矩形后的 x 极值 = **立绘板边 / 卡框画窗边**里**更窄**的那一条（谁窄谁是束缚）。
                //    · 立绘板半宽 = 原版 `CardImage` 方框 **2.7484** × 我们的 `ArtCoverMargin` **1.04**
                //      × 原版 `cardScales[20]` **0.61** ÷ 2 = **0.871792**（字面量，⛔ 不读 `ArtBoard`；
                //      1.04 是我们自己的边距，另两项是原版字段）。
                //    · 画窗边 = 逐样本字面量（`expWinL/expWinR`，来源写在各调用点 + 本方法上方的 doc）。
                //    🔴 **为什么以前写死板边是错的**：旧注释假设「tier4 的开口一定比板宽」——
                //      那是 H4 那张偏宽的 x 表带来的错觉（它把卡框左右的**装饰镂空**也算成了开口，
                //      见 `AssertTacticArtClamp` 的 doc 里那段订正）。按工程真正的画窗口径量，
                //      astra / sororitas 的 `strat_tier4` 开口都**比板窄** ⇒ 极值落在**画窗边**上。
                //    🔑 期望值**不是**从 ③b 的实测读数抄的（那就成了自证）：它由 frame PNG 的像素
                //      离线算出（口径见调用点），跑完与实测逐位一致只是交叉验证。
                //    为什么这条值得留：它与 ① 合起来仍是**两条精确墙**（内壁 ±0.557947 / 外壁 = min(板, 画窗)）
                //      ⇒ 任何「实现跟着改、期望也一起挪」都至少让一条离开边 ⇒ 灭自证成立。
                //    🧨 改坏法（**全仓只留这一份清单**，别在别处再写第二份）：
                //      · `cardScales 0.61` 丢了 / `ArtCoverMargin` 改错 ⇒ 板边挪 ⇒ 期望与实测同时偏 ⇒ 红；
                //      · **卡框档位没钉住**（退回 tier1/2 或 `CardArt.Frame` 逐级退回）⇒ 画窗边变 ⇒ 红
                //        （真正的第一道闸是上面那条 `FrameTexture.name` 前提断言，它会先报出来）；
                //      · 开口/窗口算法改了（`FrameMaskData` / `Bleed` / 连通域）⇒ 画窗边挪 ⇒ 红。
                //      ⚠️ 若**换样本**：先把新样本的画窗边按同一口径量出来填进 `expWinL/R`，
                //        ⛔ **不许**为了让它绿去放宽 `ArtClampEdgeTol`。
                float boardHalfW = 0.5f * 2.7484f * 1.04f * 0.61f;      // = 0.8717925 卡单位
                float expL = Mathf.Max(-boardHalfW, expWinL);           // 更窄 = 更靠近 0
                float expR = Mathf.Min(boardHalfW, expWinR);
                Check(Mathf.Abs(o0 - expL) <= ArtClampEdgeTol
                      && Mathf.Abs(o1 - expR) <= ArtClampEdgeTol,
                      $"③b {what} 关掉硬矩形后的 x 极值**正好压在「立绘板边 / 卡框画窗边」里更窄的一条**上"
                    + $"（实测 [{o0:F4}, {o1:F4}] vs 期望 [{expL:F4}, {expR:F4}]，容差 {ArtClampEdgeTol:F4}；"
                    + $"板边 ±{boardHalfW:F4} · 画窗 [{expWinL:F4}, {expWinR:F4}] · 卡框 `{expectFrameName}`）"
                    + " —— 这一条给 ① 提供**外壁**：外壁比硬矩形宽 ⇒ ① 的束缚确实是硬矩形（不是空验）"
                    + "｜🧨 `cardScales 0.61` 丢了 / 边距改错 / 卡框档位没钉住 / 窗口算法改了 ⇒ 任一端离开边 ⇒ 红");
            }
        }
        finally
        {
            CardView.DebugDisableArtClamp = prevOff;
            Object.DestroyImmediate(goOn);
            Object.DestroyImmediate(goOff);
        }
    }

    // ==================================================================
    //  战术 / 防御卡的立绘 `cardScales 0.61` + `tacticsOffsetPosition y+0.5`（E9 · 2026-10-24）
    // ==================================================================
    //
    // 🔴 **一手判据（三条，逐条亲读）**：
    //   ① `bundle_duplicateassetisolationso_assets_all/MonoBehaviour/CardScalesByCardType.json` 的
    //      `cardScalesByType`：`0 → 1.0`（Minion）· `10 → 1.0`（Hero）· **`20 → 0.6100000143051147`**（Tactic）·
    //      `5 → 0.60`（**全池没有一张卡读到 5** —— 防御卡的 SO `cardType` 也是 20 ⇒ **防御卡同样 0.61**）。
    //   ② `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_5393623139443973056.json`
    //      的 `CardImageController` 序列化：`tacticsOffsetPosition = {x:0, y:0.5, z:0}`。
    //   ③ `CardImageController__SetImage.c`：`:103-118` 把 `cardScales[卡型]` 乘进 `CardImage.localScale`
    //      （绕 pivot 等比缩）；`:126-128` **只在 `param_3 == 0x14`** 取 `tacticsOffsetPosition`、
    //      `:145-155` 把它**加在** `localPosition` 上 ⇒ **「缩尺寸」与「挪中心」是两件独立的事**。
    //   ⇒ 期望几何（卡单位；1024² 立绘；我们的 `ArtCoverMargin` 1.04）：
    //      **板宽 = 2.7484 × 1.04 × 0.61 = 1.743585** · **中心 y = −0.044 + 0.5 = 0.456**。
    //   🧨 错法 `(acy + 0.5) × 0.61` 会给出 **0.27816** —— ④ 专挡它。
    //
    // ⚠️ 期望值写死成**原版字面量**（2.7484 / 0.61 / 0.5 / −0.044），⛔ 不是读 `CardView.TacticArtScale`
    //   再和自己比（那样「常数改错 + 实现跟着错」会一起变绿）。拿 `CardView.*` 只用在「接没接上」那两条。

    /// <summary>🆕 **2026-10-24（`E9`）**：战术 / 防御卡的立绘按原版 `CardImageController` 的
    /// **卡型倍率 0.61**（`CardScalesByCardType` 20）＋ **中心偏移 +0.5**（`tacticsOffsetPosition`）摆放。
    ///
    /// <para>四层判据（缺一层就会「看着有依据」地错）：
    /// ① **几何读数**：`clampKind:0 + clipWindow:false` 那块就是**整块立绘板** ⇒ 量它 = `1.743585² @ y 0.456`；
    /// ② **生产网格接上了**（战术卡 + 防御卡各一张）：`_art` 逐顶点 = 传 `(0.61, +0.5)` 的参考网格，
    ///    且 **≠** 不传卡型那份（双向，缺一边就能「两边一起改回去」）；②b 再管**大图档的 `_artFront`**
    ///    （那是第二处调用点）；
    /// ③ **单位卡零变化**：`_art` = 不传那份、且 ≠ 传卡型那份；
    /// ④ **灭自证**：实测中心 0.456 **不等于** `(acy+0.5)×0.61 = 0.278`（把偏移也乘进倍率那种错法）。</para>
    ///
    /// <para>🧨 **改坏法**：① 把 `ArtTypeScaleFor` 改成恒 1 ⇒ ② 红、③ 仍绿；② 把它给单位卡也乘 ⇒ ③ 红；
    /// ③ 偏移写成 `(acy+0.5)*0.61` ⇒ ①④ 红；④ 只改常量、没接进 `ArtMeshInFrame`/`FrameMesh` ⇒ ①② 红。</para>
    /// </summary>
    static void AssertTacticArtScale()
    {
        Debug.Log(P + "--- 战术 / 防御卡立绘：cardScales 0.61 + tacticsOffsetPosition y+0.5（E9）---");
        const float UnitSquare = 2.7484f;                              // 原版 `CardImage` RT `m_SizeDelta`（卡单位）
        const float CoverMargin = 1.04f;                               // 我们的 `ArtCoverMargin`（既有偏离，见它的 doc）
        const float UnitY = -0.044f;                                   // 原版 `CardImage` 的 local y（我们的基准，同上）
        const float ExpectW = UnitSquare * CoverMargin * 0.61f;        // 1.74358496
        const float ExpectCy = UnitY + 0.5f;                           // 0.456
        const float WrongCy = (UnitY + 0.5f) * 0.61f;                  // 0.27816（⛔ 错式子的值）
        const float Tol = 2e-3f;                                       // 1.5 px（682 px 宽的框上 1 px ≈ 0.0033）

        RuleEngine.CardDef tdef = null, ddef = null, udef = null;
        foreach (var c in RuleEngine.CardDatabase.Load())
        {
            if (tdef == null && c.Name == "Armoured Squadron") tdef = c;       // AM52 · 战术卡 · 1024²
            if (ddef == null && c.Name == "Sacred Altar") ddef = c;            // SOR59 · 防御卡 · 1024²
            if (udef == null && c.Name == "Howling Banshee Exarch") udef = c;  // ASH79 · 单位卡 · 671×1024
            if (tdef != null && ddef != null && udef != null) break;
        }
        Check(tdef != null && ddef != null && udef != null,
              "★ 前提：卡池里找得到 `Armoured Squadron`（战术）/ `Sacred Altar`（防御）/ "
            + "`Howling Banshee Exarch`（单位）—— 找不齐 ⇒ 本条**没验到**（不是通过）");
        if (tdef == null || ddef == null || udef == null) return;

        var tdata = BattleDriver.ToCardData(tdef, tdef.Faction);
        var ddata = BattleDriver.ToCardData(ddef, ddef.Faction);
        var udata = BattleDriver.ToCardData(udef, udef.Faction);
        var artT = CardArt.Portrait(BattleDriver.ArtKey(tdef));
        var artD = CardArt.Portrait(BattleDriver.ArtKey(ddef));
        if (artT == null || artD == null)
        {
            Check(false, "★ 前提：两张非单位样本的立绘加载不到（`Resources/Art/` 不在？）⇒ 本条**没验到**（不是通过）");
            return;
        }
        Check(!tdata.isUnit && !ddata.isUnit && udata.isUnit && artT.width == 1024 && artT.height == 1024
              && artD.width == 1024 && artD.height == 1024,
              $"★ 前提：两张非单位样本都是 **1024²** 立绘、单位样本是单位卡"
            + $"（实测 tactic {artT.width}×{artT.height} / defence {artD.width}×{artD.height} / "
            + $"isUnit {tdata.isUnit},{ddata.isUnit},{udata.isUnit}）");
        if (tdata.isUnit || ddata.isUnit || artT.width != 1024 || artT.height != 1024
            || artD.width != 1024 || artD.height != 1024) return;

        bool prevOff = CardView.DebugDisableArtClamp;
        try
        {
            CardView.DebugDisableArtClamp = false;    // 硬矩形开着（① 用 clampKind:0 绕开它，②③ 走生产档）

            // ---- ① 几何：整块立绘板的宽/高/中心（不套硬矩形、不裁开口 ⇒ 就是 ArtBoard 那一份）----
            var boardT = CardView.ArtMeshForTest(artT, 1f, null, false, 0,
                                                 CardView.TacticArtScale, CardView.TacticArtDy);
            var boardU = CardView.ArtMeshForTest(artT, 1f, null, false, 0);   // 不传 = 单位卡口径
            if (boardT == null || boardU == null || boardT.vertexCount < 4 || boardU.vertexCount < 4)
            {
                Check(false, "★ 前提：整块立绘板（`clampKind:0`）建不出来 ⇒ ① 没验到（不是通过）");
            }
            else
            {
                var bT = boardT.bounds; var bU = boardU.bounds;
                Check(Mathf.Abs(bT.size.x - ExpectW) <= Tol && Mathf.Abs(bT.size.y - ExpectW) <= Tol
                      && Mathf.Abs(bT.center.y - ExpectCy) <= Tol,
                      $"① 战术卡立绘板 = **{ExpectW:F6}² @ y {ExpectCy:F3}**"
                    + $"（= 2.7484 × 1.04 × 0.61 / −0.044 + 0.5）—— 实测 {bT.size.x:F6}×{bT.size.y:F6} @ y {bT.center.y:F6}"
                    + "｜🧨 倍率没接（板还是 2.858336²）⇒ 宽红；🧨 偏移没接（y 还是 −0.044）⇒ 中心红"
                    + "｜🧨 把偏移也乘进倍率 ⇒ 中心 = 0.278 ⇒ 红（④ 再从反面钉一次）");
                // ⚠️ 不传卡型那份的读数要**先过卡体收边**：板 2.858336² 比卡宽（2.0927）宽
                //    ⇒ 量到的是 **2.0927 × 2.858336**（y 方向板比卡高 3.3313 矮 ⇒ 不被收）。
                const float CardW = 2.0927f, CardH = 3.3313f;
                Check(Mathf.Abs(bU.size.x - Mathf.Min(UnitSquare * CoverMargin, CardW)) <= Tol
                      && Mathf.Abs(bU.size.y - Mathf.Min(UnitSquare * CoverMargin, CardH)) <= Tol
                      && Mathf.Abs(bU.center.y - UnitY) <= Tol,
                      $"①b 不传卡型 ⇔ 单位卡口径：板仍是 **2.858336² @ y −0.044**（先被卡体收边 ⇒ 量到 "
                    + $"{Mathf.Min(UnitSquare * CoverMargin, CardW):F4} × {Mathf.Min(UnitSquare * CoverMargin, CardH):F4}）"
                    + $"—— 实测 {bU.size.x:F6}×{bU.size.y:F6} @ y {bU.center.y:F6}"
                    + "｜🧨 把卡型倍率写成「默认就是 0.61」⇒ 这里红（那会把所有单位卡缩掉）");
                Check(Mathf.Abs(bT.center.y - WrongCy) > 0.1f,
                      $"④ 灭自证：实测中心 **{bT.center.y:F4}** 不是 `(acy+0.5)×0.61 = {WrongCy:F4}`"
                    + "（原版「`localScale` 乘倍率」与「`localPosition` 加偏移」两条独立 —— ⛔ 别把偏移一起乘）");
            }

            // ---- ② 生产网格真的接上了（战术卡 + 防御卡各一张）----
            var samples = new[]
            {
                new { what = "战术卡 `Armoured Squadron`（AM52）", data = tdata, tag = "e9_tactic" },
                new { what = "防御卡 `Sacred Altar`（SOR59）",     data = ddata, tag = "e9_defence" },
            };
            foreach (var s in samples)
            {
                var go = new GameObject(s.tag);
                try
                {
                    var v = CardView.Create(go.transform, s.data, s.tag);
                    var prod = v != null ? v.ArtLayerMesh : null;
                    var art = v != null ? v.ArtLayerTexture : null;
                    var fr = v != null ? v.FrameTexture : null;
                    var on = art != null
                           ? CardView.ArtMeshForTest(art, 1f, fr, true, 0,
                                                     CardView.TacticArtScale, CardView.TacticArtDy) : null;
                    var off = art != null ? CardView.ArtMeshForTest(art, 1f, fr, true, 0) : null;
                    if (prod == null || on == null || off == null || prod.vertexCount < 4)
                    {
                        Check(false, $"★ 前提：{s.what} 的 `_art` 与两份参考网格都建得出来 ⇒ ② 没验到（不是通过）");
                        continue;
                    }
                    Check(MeshSame(prod, on) && !MeshSame(prod, off),
                          $"② {s.what} 生产 `_art` 网格 = **×0.61 + y+0.5** 那份（逐顶点 / 逐 uv），且 **≠** 不传卡型那份"
                        + $"（实测 {prod.vertexCount} 顶点 · 网格 {prod.bounds.size.x:F4}×{prod.bounds.size.y:F4}）"
                        + "｜🧨 `ArtLayerMeshFor` 漏传 `typeScale/typeDy` ⇒ 与 `off` 相同 ⇒ 红"
                        + "｜🧨 传反了 / 只传一半 ⇒ 与两份都对不上 ⇒ 红");
                }
                finally { Object.DestroyImmediate(go); }
            }

            // ---- ②b 大图档那一支（`ArtFrontMeshFor` 的 `bigState` 分支）也接上了 ----
            //   它是**第二处**调用点：`_art` 走 `ArtLayerMeshFor`、`_artFront` 大图档走这里 ——
            //   两处各写一遍，只接一处就会「小卡对了、详情窗里还是满幅」。
            var goB = new GameObject("e9_tactic_big");
            bool prevBigSw = CardView.BigCardOverdraw;
            try
            {
                CardView.BigCardOverdraw = true;      // 出厂值；显式写死，免得被别的探针改过
                var vB = CardView.Create(goB.transform, tdata, "e9_tactic_big", use: CardUse.ZoomWindow);
                var front = vB != null ? vB.ArtFrontMeshForTest : null;
                var artB = vB != null ? vB.ArtLayerTexture : null;
                var onB = artB != null
                        ? CardView.ArtMeshForTest(artB, 1f, null, false, 2,
                                                  CardView.TacticArtScale, CardView.TacticArtDy) : null;
                var offB = artB != null ? CardView.ArtMeshForTest(artB, 1f, null, false, 2) : null;
                if (front == null || onB == null || offB == null)
                {
                    Check(false, "★ 前提：战术卡大图档前景层（`_artFront`）建不出来 ⇒ ②b 没验到（不是通过）");
                }
                else
                {
                    Check(MeshSame(front, onB) && !MeshSame(front, offB),
                          "②b 战术卡**大图档**的前景层（`_artFront`，`ArtFrontMeshFor` 那一支）"
                        + "= **×0.61 + y+0.5** 那份，且 ≠ 不传卡型那份"
                        + $"（实测 {front.bounds.size.x:F4}×{front.bounds.size.y:F4}）"
                        + "｜🧨 只给 `_art` 接了卡型变换、漏了 `_artFront` ⇒ 详情窗里战术卡还是满幅方图 ⇒ 红");
                }
            }
            finally
            {
                CardView.BigCardOverdraw = prevBigSw;
                Object.DestroyImmediate(goB);
            }

            // ---- ③ 单位卡零变化（灭自证：必须 ≠ 传卡型那份，否则「两边一起乘」也能过）----
            var goU = new GameObject("e9_unit");
            try
            {
                var vU = CardView.Create(goU.transform, udata, "e9_unit");
                var prodU = vU != null ? vU.ArtLayerMesh : null;
                var artU = vU != null ? vU.ArtLayerTexture : null;
                var frU = vU != null ? vU.FrameTexture : null;
                var refU = artU != null ? CardView.ArtMeshForTest(artU, 1f, frU, false, 0) : null;
                var refScaled = artU != null
                              ? CardView.ArtMeshForTest(artU, 1f, frU, false, 0,
                                                        CardView.TacticArtScale, CardView.TacticArtDy) : null;
                if (prodU == null || refU == null || refScaled == null || prodU.vertexCount < 4)
                {
                    Check(false, "★ 前提：单位卡样本的 `_art` 与两份参考网格都建得出来 ⇒ ③ 没验到（不是通过）");
                }
                else
                {
                    Check(MeshSame(prodU, refU) && !MeshSame(prodU, refScaled),
                          "③ 单位卡 **零变化**：`_art` = 不传卡型那份（逐顶点 / 逐 uv），且 ≠ ×0.61 那份"
                        + $"（实测 {prodU.bounds.size.x:F4}×{prodU.bounds.size.y:F4}）"
                        + "｜🧨 把 `!isUnit` 那半边写反（单位卡也缩到 61%）⇒ 与 `refU` 对不上 ⇒ 红");
                }
            }
            finally { Object.DestroyImmediate(goU); }
        }
        finally { CardView.DebugDisableArtClamp = prevOff; }
    }

    /// <summary>🆕 **2026-10-24（H5）**：**大图档的 BIG 状态按 overdraw 两轴门控**。
    ///
    /// <para>判据（一手，`d:/2/tools/decomp_full/` 亲读）：原版 `CardImageController` 的**状态 1（Big）**
    /// = `_Clamp_Range` 换 BIG（`ChangeState.c:100-104`）+ 立绘 × `bigSizeMultiplier`（`:90-95`）
    /// + `frameMask` 打开（`:105-108`）。而**谁会被送进状态 1** 只有三处，判据一致：
    /// `CardDisplayWindow__ShowCard.c:62-84` · `CardOverdrawHandle.c:7-39` · `ChangeCardPosition.c:74-79`
    /// ⇒ **状态 1 ⇔ 「这一格是前台（idx == 0）」∧「这张卡 `useOverDraw != 0`」**（⛔ 与卡类型无关）。
    /// 本工程的两条轴 = `CardView.ArtOverdraw`（前台）/ `CardView.UseOverDraw`（逐卡），
    /// 合成在 `CardView.BigImageState` 一处。</para>
    ///
    /// <para>🔴 **为什么这条必须存在**（D3 之后仍然缺的那一半）：D3 只把 **×1.27 的倍率**按卡类型门控了，
    /// `bigState` 还是「大图档窗口 ∧ 总开关」—— 于是详情窗里的战术/防御卡（池内 `useOverDraw` 全 0）
    /// 与相关卡（`idx != 0`）都会被当成 BIG。`M2` 之后 `_art` 已不吃硬矩形 ⇒ 这条的**观测量**是
    /// `_artFront` 的两支（见下面那段）；`_art` 自身不再随状态变（这也是 `M2` 顺手确认的一件事：
    /// 非单位卡本来就乘不到 ×1.27，`BigArtScaleApplies` 把它挡住了）。
    /// 判据全文 → `资料/普查产出_1024/D3_大图放大门控.md` §1·3 与 `H5_卡面清理与BIG档.md` §件1。</para>
    ///
    /// <para>🔴 **2026-10-24（第七十三会话 `M2`）观测量换了一次，判据轴没换**：`ArtClampKindFor` 恒 0 之后
    /// `_art` 不再吃硬矩形（也不给非单位卡乘 ×1.27）⇒ **`_art` 在两档之间逐顶点相同**，原来拿
    /// 「`_art` 的 `clampKind 1 ⇄ 2`」当观测量的 5 条会集体失去观测量（当天 `Run` 就是这么红的）。
    /// 现观测量 = **`_artFront`**：`ArtFrontMeshFor` 按 `bigState` 分两支（大图档 `frameTex:null /
    /// clipWindow:false / clampKind:2` · 小图档 `frameTex / clipWindow:true / clampKind:1`），结构性不同，
    /// 且两条轴 setter 都会经 `RebakeBigStateMeshes` 重烘它 ⇒ 门控轴照样可观测、判别力不减。
    /// ⚠️ 战术卡在这两条轴上是**瞬时**支（不走单位卡那条 0.3 s 补间）⇒ 不用 `CardTween.Advance`。</para>
    ///
    /// <para>🧨 **改坏法**：① 把 `CardView.BigImageState` 里的 `_useOverDraw` 删掉（= D3 那一版）
    /// ⇒ ①/③ 红（战术卡 `_artFront` 仍是 BIG 那支）；② 只删 `_artOverdraw` ⇒ ② 红；③ 把重烘那步
    /// （`RebakeBigStateMeshes`）删掉 ⇒ 四条「切过去」的断言全红（轴改了网格不动）。</para>
    ///
    /// <para>⚠️ **量的是真卡的真网格**（不是把 `BigImageState` 读一遍）—— 档位与倍率都是烘进顶点的。
    /// 卡框钉 `TacticArtClampProbeTier`(4)（H4 的教训：只有开口比硬矩形宽时，硬矩形才决定网格）。</para>
    /// </summary>
    static void AssertBigStateOverDrawGate()
    {
        Debug.Log(P + "--- 大图档 BIG 状态的门控（H5：前台 × useOverDraw）---");
        RuleEngine.CardDef tdef = null, udef = null;
        foreach (var c in RuleEngine.CardDatabase.Load())
        {
            if (tdef == null && c.Name == "Armoured Squadron") tdef = c;          // AM52 · 战术卡 · 1024² 立绘
            if (udef == null && c.Name == "Howling Banshee Exarch") udef = c;     // ASH79 · 单位卡 · 671×1024
            if (tdef != null && udef != null) break;
        }
        Check(tdef != null && udef != null,
              "★ 前提：卡池里找得到战术卡 `Armoured Squadron` 与单位卡 `Howling Banshee Exarch`"
            + "（找不到 ⇒ 本条**没验到**，不是通过）");
        if (tdef == null || udef == null) return;

        var tdata = BattleDriver.ToCardData(tdef, tdef.Faction);
        var udata = BattleDriver.ToCardData(udef, udef.Faction);
        Check(!tdata.isUnit && udata.isUnit,
              $"★ 前提：样本类型对（战术 `isUnit={tdata.isUnit}` · 单位卡 `isUnit={udata.isUnit}`）");

        int prevTier = CardView.DebugFrameTier;
        bool prevBig = CardView.BigCardOverdraw;
        bool prevClamp = CardView.DebugDisableArtClamp;
        var goT = new GameObject("h5_tactic_big");
        var goU = new GameObject("h5_unit_big");
        try
        {
            CardView.DebugFrameTier = TacticArtClampProbeTier;   // 开口比硬矩形宽的那一档（见 H4 的离线表）
            CardView.BigCardOverdraw = true;                     // 出厂值；显式写死，免得被别的探针改过
            CardView.DebugDisableArtClamp = false;               // 硬矩形开着（本条量的就是它按状态换档）

            // ---------------- 战术卡（池内 `useOverDraw` = 0）----------------
            // 🔴 **2026-10-24（第七十三会话 `M2`）口径重写（铁律 5·c：一个值 ≠ 全部情况）**：
            //   本节原来拿 `_art` 的**硬矩形换档**（`clampKind 1 ⇄ 2`）当 BIG 状态的**观测量**。
            //   `ArtClampKindFor` 恒 0 之后 `_art` 不再吃硬矩形、（非单位卡也永远不乘 ×1.27）
            //   ⇒ **`_art` 在两档之间逐顶点相同**，原来那 5 条（前提 + ①①b②②b）全部失去观测量
            //   → 2026-10-24 `CardBaseDemo.Run` 里就是这么红的（其余 393 绿）。
            //   ⇒ 观测量换成 **`_artFront`（大图档角色抠图层）**：它在 `ArtFrontMeshFor` 里**按状态分两支**
            //      大图档 = `frameTex:null / clipWindow:false / clampKind:2`（几何 = 整块板 + 大图档硬矩形）
            //      小图档 = `frameTex / clipWindow:true / clampKind:1`（几何 = 裁到卡框开口 + 小图档硬矩形）
            //   两档**结构性不同**，且 `RebakeBigStateMeshes()` 在两条轴 setter 里都会重烘它
            //   ⇒ 门控轴仍然**可观测**，判别力（删 `_useOverDraw` / 删 `_artOverdraw` / 删重烘）一条不少。
            //   ⚠️ 战术卡在这两条轴上是**瞬时**支（不是单位卡那条 0.3 s 补间）⇒ 不用 `CardTween.Advance`。
            var tv = CardView.Create(goT.transform, tdata, goT.name, CardFace.Full, null, CardUse.ZoomWindow);
            var tArt = tv.ArtLayerTexture;
            var tFr = tv.FrameTexture;
            var tBigF = tArt != null
                      ? CardView.ArtMeshForTest(tArt, 1f, null, false, 2,
                                                CardView.TacticArtScale, CardView.TacticArtDy) : null;
            var tSmallF = tArt != null
                        ? CardView.ArtMeshForTest(tArt, 1f, tFr, true, 1,
                                                  CardView.TacticArtScale, CardView.TacticArtDy) : null;
            Check(tv.ArtFrontMeshForTest != null && tBigF != null && tSmallF != null,
                  "★ 前提：战术卡大图档的 `_artFront` 建出来了，且大/小两档参考网格都建得出来"
                + $"（front={tv.ArtFrontMeshForTest != null} / big={tBigF != null} / small={tSmallF != null}）"
                + " —— 缺一个 ⇒ 下面几条**没验到**（不是通过）");
            Check(tBigF != null && tSmallF != null && !MeshSame(tBigF, tSmallF),
                  "★ 前提：大图档 / 小图档两份参考网格**彼此不同** —— 否则下面几条是空验");
            Check(tv.ArtFrontMeshForTest != null && tBigF != null && tSmallF != null
                  && MeshSame(tv.ArtFrontMeshForTest, tBigF) && !MeshSame(tv.ArtFrontMeshForTest, tSmallF),
                  "★ 前提：大图档**刚建出来**时处 BIG（两轴默认 true ⇒ `_artFront` 用大图档那一支）"
                + " —— 这条不成立 ⇒ 下面的比较失去意义（不是通过）");

            tv.UseOverDraw = false;
            Check(MeshSame(tv.ArtFrontMeshForTest, tSmallF) && !MeshSame(tv.ArtFrontMeshForTest, tBigF),
                  "① 战术卡 `useOverDraw = 0` ⇒ **不进 BIG**（`_artFront` 逐顶点落回**小图档**那一支）"
                + "｜🧨 状态不看 `useOverDraw`（D3 那一版）⇒ 仍是大图档那一份 ⇒ 红");
            tv.UseOverDraw = true;
            Check(MeshSame(tv.ArtFrontMeshForTest, tBigF) && !MeshSame(tv.ArtFrontMeshForTest, tSmallF),
                  "①b 灭自证：置回 `= 1` ⇒ 逐顶点**回到**大图档那一份（证明 ① 是这一轴驱动的，不是一次性降档）");

            tv.ArtOverdraw = false;                              // = 扇形里 `idx != 0` 的相关卡
            Check(MeshSame(tv.ArtFrontMeshForTest, tSmallF) && !MeshSame(tv.ArtFrontMeshForTest, tBigF),
                  "② 相关卡（`ArtOverdraw = false` = 原版 `idx != 0`）**同样不进 BIG**"
                + "｜🧨 状态只看 `useOverDraw`、漏了前台轴 ⇒ 仍是大图档那份 ⇒ 红");
            tv.ArtOverdraw = true;
            Check(MeshSame(tv.ArtFrontMeshForTest, tBigF) && !MeshSame(tv.ArtFrontMeshForTest, tSmallF),
                  "②b 灭自证：置回前台 ⇒ 回到大图档那一份");

            // ---------------- 单位卡（池内 `useOverDraw` = 1）----------------
            var uv = CardView.Create(goU.transform, udata, goU.name, CardFace.Full, null, CardUse.ZoomWindow);
            var u127 = CardView.ArtMeshForTest(uv.ArtLayerTexture, CardView.BigArtScale, null, false, 2);
            var u100 = CardView.ArtMeshForTest(uv.ArtLayerTexture, 1f, uv.FrameTexture, false, 1);
            Check(!MeshSame(u127, u100),
                  "★ 前提：单位卡「×1.27 + 大图档」与「1.0 + 小图档」两份参考网格**彼此不同**");
            Check(uv.ArtFrontMeshForTest != null
                  && MeshSame(uv.ArtFrontMeshForTest, u127) && !MeshSame(uv.ArtFrontMeshForTest, u100),
                  "★ 前提：单位卡 `useOverDraw = 1` + 前台 ⇒ **进 BIG**（`_artFront` = ×1.27 那一份）");

            uv.UseOverDraw = false;
            // 🔴 **2026-10-24 同步点补（执行代理 E5 的 BIG 补间）**：`Run()` 里把 `CardTween.Mode`
            //   设成了 `UpdateType.Manual` ⇒ 这条 setter 现在**起一条 0.3 s 的补间并停在起点（k=0）**，
            //   不再像 E5 之前那样「赋值即终态」。**只泵补间、不推对局时钟**（同 `BattleScene`
            //   量扇形的那两处用法）⇒ 推过一次才是终态。
            //   ⚠️ 战术/相关卡那几条（①/②）不用：它们走 `!_builtData.isUnit` 的**瞬时**支。
            CardTween.Advance(CardView.BigStateTweenDuration + 0.05f);
            Check(MeshSame(uv.ArtFrontMeshForTest, u100) && !MeshSame(uv.ArtFrontMeshForTest, u127),
                  "③ 单位卡 `useOverDraw = 0` ⇒ **也退出 BIG**（×1.27 消失、硬矩形落回小图档）"
                + "｜🧨 门控只挡非单位卡 ⇒ 仍是 ×1.27 ⇒ 红；"
                + "🧨 补间没推进（停在起点 k=0）⇒ 本行也会红 —— **先检查上面那句 `CardTween.Advance`**");
            uv.UseOverDraw = true;
            CardTween.Advance(CardView.BigStateTweenDuration + 0.05f);   // 同上：置回也是一条 0.3 s 补间
            Check(MeshSame(uv.ArtFrontMeshForTest, u127) && !MeshSame(uv.ArtFrontMeshForTest, u100),
                  "③b 灭自证：置回 ⇒ 回到 ×1.27 那一份"
                + "｜🧨 补间没推进 ⇒ 停在起点（反而量到 u100）");
        }
        finally
        {
            CardView.DebugFrameTier = prevTier;
            CardView.BigCardOverdraw = prevBig;
            CardView.DebugDisableArtClamp = prevClamp;
            Object.DestroyImmediate(goT);
            Object.DestroyImmediate(goU);
        }
    }

    /// <summary>两块网格是否逐顶点 / 逐 uv 相同（自检用；容差 1e-4 见平方 1e-8）。</summary>
    static bool MeshSame(Mesh a, Mesh b)
    {
        if (a == null || b == null) return a == b;
        var va = a.vertices; var vb = b.vertices;
        var ua = a.uv; var ub = b.uv;
        if (va.Length != vb.Length || ua.Length != ub.Length) return false;
        for (int i = 0; i < va.Length; i++) if ((va[i] - vb[i]).sqrMagnitude > 1e-8f) return false;
        for (int i = 0; i < ua.Length; i++) if ((ua[i] - ub[i]).sqrMagnitude > 1e-8f) return false;
        return true;
    }

    /// <summary>
    /// 🆕 **2026-10-23（D2 · `W63·层级审查` 的 F2）**：**场上档特效锚点**的节点与位置。
    ///
    /// 判据（全部实读，不是我们挑的）：
    ///   · `d:/2/tools/decomp_full/CardScript__GetEffectAnchor.c` —— 状态 ∉ {0,1,4,8,9,10,12,16} ⇒
    ///     `BattleCardUI.effectsAnchor`（`+0x1A0`）；否则 `card2DController.transform`（= `2DCard`）。
    ///   · 卡 prefab 里那份 `BattleCardUI` 的**序列化 PPtr**：`effectsAnchor` → **`CardPrefab/Board Elements/EffectAnchor`**，
    ///     `localPosition = (0, **1.34**, **−0.128**)`（prefab 根的原点 = 卡底边）。
    ///   · 我们卡根原点在**卡心** ⇒ 换算只有 `y −= Height/2` 这一步（`CardView.BoardEffectAnchorLocal`）。
    ///
    /// 🧨 **改坏法**（两条**都要**能红）：
    ///   · 把锚点挂回 `FaceRoot`（`2DCard` 那一级）⇒ 「父节点是卡根」那条红；
    ///   · 把 `localPosition` 改回 `(0, +0.08, 0)`（那是**另一颗**同名节点 `2DCard/Front/EffectAnchor` 的位置，
    ///     它不被 `effectsAnchor` 引用）⇒ 位置那条红。
    /// ⚠️ 与它成对的**手牌档**不在这里断：那一档的锚点就是 `FaceRoot`，`CardFace.Hand` 建出来即成立。
    /// </summary>
    static void AssertEffectAnchor()
    {
        var root = new GameObject("fx_anchor_probe");
        var v = CardView.Create(root.transform, CardData.Placeholder(0), "anchor_board", CardFace.Board);
        var anchor = v.EffectAnchor(true);
        Check(anchor != null, "场上档特效锚点：`EffectAnchor(true)` 取得到（懒建那条路建出来了）");
        if (anchor != null)
        {
            Check(anchor.parent == v.transform,
                  $"……父节点 = **卡根**（原版 `Board Elements` 与 `2DCard` 同级；⛔ 不是 `2DCard` 那一级）"
                  + $" —— 实测 `{(anchor.parent == null ? "null" : anchor.parent.name)}`");
            // 期望值**只从 `CardView.BoardEffectAnchorLocal` 取**（与产品侧同一份判据，⛔ 不在测试里再抄一个常量）
            var want = CardView.BoardEffectAnchorLocal;
            var got = anchor.localPosition;
            Check((got - want).sqrMagnitude < 1e-8f,
                  $"……局部位 = 原版 `(0, 1.34, −0.128)` 经 `From3DBody`（`y −= Height/2`）换算 = "
                  + $"({want.x:F3}, {want.y:F3}, {want.z:F3})；实测 ({got.x:F3}, {got.y:F3}, {got.z:F3})");
            // ★ 前提：这条换算**不是恒等**（Height/2 > 0）—— 否则上面那条分不出「换算过」和「没换算」
            Check(Mathf.Abs(want.y - 1.34f) > 1f,
                  $"★ 前提：换算真的动过 y（1.34 → {want.y:F3}）—— 否则上面那条对「直接抄 1.34」也是绿的");
        }
        Object.DestroyImmediate(root);
    }

    /// <summary>
    /// 🆕 **2026-10-23（D · `W63·层级审查` F4 / F6）**：**卡上特效跟着这张卡排队** + **手牌「活动中」档**。
    ///
    /// 判据：
    ///   · 原版场上可视体 = `3DBody` 的 SortingGroup；卡上特效挂 `Board Elements/EffectAnchor`
    ///     （**无** SortingGroup）⇒ 两者**同 key、由距离排** ⇒ 「这张卡的特效」跟着这张卡
    ///     （`资料/普查产出_1023/D_战斗层级审查.md` §②·B）。
    ///   · 我们是「一张卡 = 一段 renderQueue」⇒ 落成 `renderQueue = 卡的 Queue` +
    ///     `sortingOrder = EffectSortingOrder(70) + 原值`（判据与号段约束见 `CardView.StampEffectRoot`）。
    ///   · `HandMoving` 层：`CardScript__ShowCardInCenter.c` 等三处（见 `CardView.QHandMoving`）。
    ///
    /// 🧨 **改坏法**：
    ///   · `StampEffectRoot` 什么都不做 ⇒ ②③红；
    ///   · 把 `EffectSortingOrder` 写成「固定 70」（不 + 原值）⇒ ⑤红（3 ⇒ 73）；
    ///   · `SetQueue` 里删掉 `_effectRoots` 那一段 ⇒ ④红（特效停在旧号，不随卡重排）；
    ///   · `QHandMoving` 改成 `QCard`（撞号）或 ≥ `CardFan.QCardBase`（越过详情窗）⇒ ⑥红。
    /// </summary>
    static void AssertEffectQueueFollowsCard()
    {
        var root = new GameObject("fx_queue_probe");
        var v = CardView.Create(root.transform, CardData.Placeholder(0), "fx_queue_board", CardFace.Board);
        // 假「特效实例」：一个挂在卡根上的子节点 + 一颗有材质的 MeshRenderer
        var fx = new GameObject("ProbeEffect");
        fx.transform.SetParent(v.transform, false);
        var mr = fx.AddComponent<MeshRenderer>();
        var sh = Shader.Find("Sprites/Default");
        var mat = sh != null ? new Material(sh) : null;
        if (mat != null) mr.sharedMaterial = mat;
        Check(mr.sharedMaterial != null,
              "★ 前提：假特效拿到材质（`Sprites/Default` 找不到 ⇒ 下面几条全在假绿）");
        if (mr.sharedMaterial == null)
        {
            Object.DestroyImmediate(root);
            return;
        }
        // 先给它一个**非 0** 的 sortingOrder —— 用来断「基准 + 原值」而不是「一律 70」
        mr.sortingOrder = 3;

        Check(CardView.OwnerOf(fx.transform) == v,
              "① `CardView.OwnerOf` 沿父链找到这张卡（= `CardEffects.Fire` 判定「特效挂给谁」的那个口）");

        v.StampEffectRoot(fx.transform);
        Check(mr.sharedMaterial.renderQueue == v.Queue,
              $"② 特效 renderQueue = **这张卡的那一段**（卡 {v.Queue} / 特效 {mr.sharedMaterial.renderQueue}）"
              + "—— 原版「特效与卡同 key」的等价物：进了卡的段就**跨不到邻卡**");
        Check(mr.sortingOrder == CardView.EffectSortingOrder + 3,
              $"③ 特效 sortingOrder = 基准 {CardView.EffectSortingOrder} + 原值 3 = {CardView.EffectSortingOrder + 3}"
              + $"（实测 {mr.sortingOrder}）—— 基准必须高于卡面最高层号 60，否则特效被卡面文字盖住");
        Check(CardView.EffectSortingOrder > 60,
              $"★ 前提：基准 {CardView.EffectSortingOrder} 真的高于卡面最高层号 60（否则 ③ 对「和卡面同号」也绿）");

        // ④ 特效**随卡重排**（换段时一起走）—— 这才是 F4 的「治本」那半
        v.SetQueue(3005);
        Check(v.Queue == 3005, $"★ 前提：`SetQueue(3005)` 生效（实测 {v.Queue}）");
        Check(mr.sharedMaterial != null && mr.sharedMaterial.renderQueue == 3005,
              $"④ 卡换段之后特效**跟着换**（实测 {mr.sharedMaterial.renderQueue}；应 3005）"
              + "｜🧨 删掉 `SetQueue` 里 `_effectRoots` 那一段 ⇒ 停在 3000 ⇒ 红");
        Check(mr.sortingOrder == CardView.EffectSortingOrder + 3,
              $"⑤ 反复盖章**不涨号**（仍是 {CardView.EffectSortingOrder + 3}，实测 {mr.sortingOrder}）"
              + "—— 基准取的是「第一次记下的原值」，不是「当前值 + 70」");

        // ⑥ F6：手牌「活动中」那一档的号段位置
        Check(CardView.QHandMoving == CardView.QCard + 1,
              $"⑥a `QHandMoving` = `QCard + 1`（{CardView.QHandMoving}，手牌段顶号的上一格）");
        Check(CardView.QHandMoving > CardView.QCard && CardView.QHandMoving < CardFan.QCardBase,
              $"⑥b 它夹在「手牌段顶号 {CardView.QCard}」与「详情窗卡叠 {CardFan.QCardBase}」之间"
              + $"（实测 {CardView.QHandMoving}）—— 🧨 取 `QCard` 会撞号、取 ≥ `QCardBase` 会越过详情窗");
        Check(CardView.QHandMoving > HandLayout.QueueFloor,
              $"⑥c 高于手牌段下限 {HandLayout.QueueFloor}（原版 `HandMoving` 高于 `Hand` 的同向）");

        if (mat != null) Object.DestroyImmediate(mat);
        Object.DestroyImmediate(root);
    }

    /// <summary>
    /// 🆕 **2026-10-23（D4 · `W63·层级审查` F1 Step 2/3）**：**3D 卡体 = 不透明 + 有深度/阴影 pass**。
    ///
    /// 判据（原始 bundle 逐字段实读，全文 → `资料/普查产出_1023/D3_F1判据与方案.md`）：
    ///   · 原版材质 `Card 3d Lvl1` 的 shader `Everguild/Cards/3D Card`：子标签
    ///     `QUEUE=Geometry` / `RenderType=Opaque`；pass 状态 `blend (1,0)=One/Zero` · `zWrite 1` · `cull 2`。
    ///   · 该 shader **5 个 pass**：Forward / DepthOnly / MotionVectors / DepthNormalsOnly / **ShadowCaster**。
    ///   · `renderQueue` 的排序优先级**高于** shader 标签 ⇒ 必须同时断「`SetQueue` 不盖章卡体」。
    ///
    /// 🧨 **改坏法**（逐条都能红）：
    ///   · `Card3D.shader` 的 `Queue` 改回 `Transparent` ⇒ ①②红；
    ///   · 删掉 `StampQueue` 里 `if (mr == _body3D) return;` ⇒ ④红；
    ///   · 删掉 `SubtreeQueues` 里那条 `continue` ⇒ ④b 红（`A1665`/`A1672②` 的 min/max 会被 2000 污染）；
    ///   · 删掉三个 `UsePass` ⇒ ⑤a/⑤b/⑤c 红（⑤c 同时是 F3「卡体阴影真生效」的前提）。
    /// </summary>
    static void AssertBodyOpaque()
    {
        var root = new GameObject("body_opaque_probe");
        var v = CardView.Create(root.transform, CardData.Placeholder(0), "opaque_board", CardFace.Board);
        var body = v.transform.Find("body3D");
        var mr = body != null ? body.GetComponent<MeshRenderer>() : null;
        Check(mr != null && mr.sharedMaterial != null,
              "★ 前提：场卡建出了 `body3D` 且有材质（取不到 ⇒ 下面几条全在假绿；`Card3D.shader` 编不过时就是这一条红）");
        if (mr != null && mr.sharedMaterial != null)
        {
            var m = mr.sharedMaterial;
            Check(m.renderQueue == 2000,
                  $"① 卡体材质在**不透明档**（`renderQueue` 实测 {m.renderQueue}；原版 `Card 3d Lvl1` 的 `QUEUE=Geometry` = 2000）");
            Check(m.GetTag("RenderType", false) == "Opaque" && m.GetTag("Queue", false) == "Geometry",
                  "② shader 标签 = `Queue=Geometry` / `RenderType=Opaque`"
                  + $"（实测 Queue=`{m.GetTag("Queue", false)}` · RenderType=`{m.GetTag("RenderType", false)}`）");
            // ★ A/B 前提：`SetQueue` 真的动过 —— 否则 ④ 对「什么都没发生」也是绿的
            v.SetQueue(2970);
            Check(v.Queue == 2970, $"★ 前提：`SetQueue(2970)` 生效（实测 {v.Queue}）");
            Check(mr.sharedMaterial != null && mr.sharedMaterial.renderQueue == 2000,
                  $"④ `SetQueue` **不盖章 3D 卡体**（盖章后实测 {mr.sharedMaterial.renderQueue}，应仍为 2000）"
                  + "｜🧨 删掉 `StampQueue` 里那条 `mr == _body3D` ⇒ 变 2970 ⇒ 红");
            Check(System.Array.IndexOf(v.SubtreeQueues(), 2000) < 0,
                  "④b `SubtreeQueues()` 里**没有**卡体那档 2000"
                  + "（否则 `A1665`「前卡整段>后卡整段」与 `A1672②`「每层都在自己那一段」会被它污染）");
            Check(m.FindPass("DepthOnly") >= 0,
                  $"⑤a 有 `DepthOnly` pass（实测 {m.FindPass("DepthOnly")}，-1 = 没有 ⇒ 卡体不进 `_CameraDepthTexture`）");
            Check(m.FindPass("DepthNormalsOnly") >= 0,
                  $"⑤b 有 `DepthNormalsOnly` pass（实测 {m.FindPass("DepthNormalsOnly")}，-1 = 没有 ⇒ 不进 `_CameraNormalsTexture`）");
            Check(m.FindPass("ShadowCaster") >= 0,
                  $"⑤c 有 `ShadowCaster` pass（实测 {m.FindPass("ShadowCaster")}）"
                  + "—— 没有它 `MeshRenderer.castShadows = On` 是**空转**（本工程 `Fallback Off`）");
            // ⑥ 溶解中同样不许被盖章（老隐患：盖章会把渲染器换成队列克隆 ⇒ `SetDissolveAmount` 写进没人用的那份）
            if (v.DissolveSupported)
            {
                v.BeginDissolve();
                Check(v.OnDissolveMaterial, "★ 前提：`BeginDissolve()` 真的把溶解材质换上了");
                v.SetQueue(2969);
                Check(v.OnDissolveMaterial,
                      "⑥ 溶解进行中 `SetQueue` **不会**把卡体换成队列克隆（`OnDissolveMaterial` 仍为真）");
                // ⑦ 🆕 2026-10-23（D5 · A4 的审查）：`EndDissolve()` 必须**无条件换回**
                //   判据 = 反编译 `BattleCardUI__RestoreOriginalMaterial.c`：整段没有「当前是不是溶解材质」
                //   的分支；`DelayedResetMaterial` 收尾也是无条件 `SetCardMaterial(defaultMaterial, true)`。
                //   🧨 改坏法：早退条件写成 `sharedMaterial == _dissolveMat` ⇒ ⑦ 立刻红（留在溶解材质上）。
                v.EndDissolve();
                Check(mr.sharedMaterial == m,
                      "⑦ `EndDissolve()` 之后换回的是**原来那一份材质**（引用相等，不只是「不是溶解材质」）"
                      + "｜🧨 早退条件写成 `== _dissolveMat` ⇒ 留在溶解材质上 ⇒ 红");
                Check(!v.OnDissolveMaterial && mr.sharedMaterial != null && mr.sharedMaterial.renderQueue == 2000,
                      $"⑦b ……而且材质不再是溶解那份、队列回到不透明档 2000（实测 queue={mr.sharedMaterial.renderQueue}）");
                Check(v.DissolveAmount < 0f, $"⑦c `DissolveAmount` 复位 −1（实测 {v.DissolveAmount}）");
            }
            else Check(true, "（⑥⑦ 跳过：取不到原版溶解 shader `Everguild/Cards/3D Card Dissolve`）");
        }
        Object.DestroyImmediate(root);
    }

    // ==================================================================
    //  🆕 2026-10-24（W14 · S2）：高亮态**不许**给整卡（含四个数值）着色
    // ==================================================================
    //
    //  判据（全量反编译，第一权威）：
    //   · `d:/2/tools/decomp_full/CardHighlight__ChangeFrameColor.c` —— 整个方法只做
    //     `SpriteRenderer.set_color`（`param_1 + 0x70` / `+0x78` 两颗 = `FrameHighlight` /
    //     `FrameHighlightRemnant`）+ `DOColor` 补间 + `ToggleFrames`。**没有一处碰卡的其它图层/材质。**
    //   · `CardHighlight__ChangeState.c` —— 6 个 case 只有两个出口：`ChangeFrameColor`（色）与
    //     `ChangeTargetScale`（缩放，仅 case 0/3/4）。**没有第三条路。**
    //   · `CardTextCountersController.DoColorChange(old,new)` 只改那个 counter 自己的颜色。
    //  ⇒ 用户 2026-10-24 报「加攻/加血时数值变红」，成因是 `CardView.SyncStatLayers` /
    //     `ApplyStatFlashes` 的 `_tint * c`：`_tint` 被 `CardHighlight.ColorOf(SelectedTargetInBoard)`
    //     设成 **#FF8400** ⇒ 白字 × 橙 = **(255,132,0)**。本批把 `ColorOf` 的棋盘两态改成白
    //     （判据全文 → `资料/普查产出_1024d/S1_场上卡牌四连症状.md` §2·4–2·7）。
    //
    //  🧨 **改坏法**（每条都能红）：
    //   · `ColorOf(SelectedTargetInBoard)` 改回 `SelectedTargetColor` ⇒ ① 红（r=1 / g=0.5176 / b=0）；
    //   · `ColorOf(ValidTarget)` 改回 `ValidTargetColor` ⇒ ② 红（g=1 / r=b=0）；
    //   · 在 `SyncStatLayers` 里把 `_tint * c` 改成 `c`（治标的那个「次选方案」）⇒ ⑥ 红
    //     （`SetTint(0.5 灰)` 之后数值必须变成 0.5×白）——这一条就是防「两边一起改回去」的灭自证；
    //   · 删掉 `SetHighlight` 里 `FrameColorOf` → `_rim` 那一路 ⇒ ③④ 红（环不亮/不按状态变色）。
    static void AssertStatTintNotWholeCard()
    {
        var root = new GameObject("stat_tint_probe");
        try
        {
            var v = CardView.Create(root.transform, CardData.Placeholder(0), "stat_tint_board", CardFace.Board);
            var melee = LayerMesh(v.transform, "stat1");
            var health = LayerMesh(v.transform, "stat4");
            Check(melee != null && health != null,
                  "★ 前提：场卡建出了数值层 `stat1` / `stat4`（`BuildStatLayers` 要 `isUnit` + 数字图集可用；"
                + "建不出来 ⇒ 下面全部**没验到**，别读成通过）");
            if (melee == null || health == null || melee.sharedMaterial == null) return;
            Check(melee.gameObject.activeSelf && health.gameObject.activeSelf,
                  "★ 前提：这两格**是开着的**（关着的层读到的颜色是上一次刷的值 ⇒ 会假绿）");

            // ---- ① 棋盘「指针压着的目标」：整卡白、数值白 ----
            v.SetHighlight(CardHighlightState.Normal);
            v.SetHighlight(CardHighlightState.SelectedTargetInBoard);
            var c1 = melee.sharedMaterial.color;
            Check(Mathf.Abs(c1.r - 1f) < 0.01f && Mathf.Abs(c1.g - 1f) < 0.01f && Mathf.Abs(c1.b - 1f) < 0.01f,
                  $"① `SelectedTargetInBoard` ⇒ 数值层颜色 = **白**（实测 ({c1.r:F3}, {c1.g:F3}, {c1.b:F3})）"
                + "｜🧨 把 `ColorOf` 这一档改回 `SelectedTargetColor` ⇒ (1, 0.5176, 0) ⇒ 红");
            Check(Mathf.Abs(v.Tint.r - 1f) < 0.01f && Mathf.Abs(v.Tint.g - 1f) < 0.01f,
                  $"①·b ……整卡 `_tint` 也是白（实测 ({v.Tint.r:F3}, {v.Tint.g:F3}, {v.Tint.b:F3})）"
                + " —— 数值色被染红的唯一入口就是它");

            // ---- ③ 颜色没丢：那一圈环仍是原版橙 #FF8400 ----
            var rim = v.RimTargetForTest;
            Check(Mathf.Abs(rim.r - 1f) < 0.01f && Mathf.Abs(rim.g - 0.5176f) < 0.01f && Mathf.Abs(rim.b - 0f) < 0.01f,
                  $"③ 同一态下**环仍是橙 #FF8400**（`RimTargetForTest` = ({rim.r:F3}, {rim.g:F3}, {rim.b:F3})）"
                + " —— 原版那 5 个状态色只点这一层（`FrameColorOf`）；🧨 把颜色从环上也删掉 ⇒ 红");
            Check(Mathf.Abs(rim.a - CardHighlight.RimAlphaScale) < 0.02f,
                  $"③·b 环的 alpha = 原版材质 `_Outline.a` {CardHighlight.RimAlphaScale:F3}（实测 {rim.a:F3}）");

            // ---- ② 合法目标：整卡也白、环是绿 ----
            v.SetHighlight(CardHighlightState.ValidTarget);
            var c2 = melee.sharedMaterial.color;
            Check(Mathf.Abs(c2.r - 1f) < 0.01f && Mathf.Abs(c2.g - 1f) < 0.01f && Mathf.Abs(c2.b - 1f) < 0.01f,
                  $"② `ValidTarget` ⇒ 数值层颜色 = **白**（实测 ({c2.r:F3}, {c2.g:F3}, {c2.b:F3})）"
                + "｜🧨 把 `ColorOf` 这一档改回 `ValidTargetColor` ⇒ (0, 1, 0.129) ⇒ 红");
            var rim2 = v.RimTargetForTest;
            Check(Mathf.Abs(rim2.r - 0f) < 0.01f && Mathf.Abs(rim2.g - 1f) < 0.01f
                  && Mathf.Abs(rim2.b - 0.1294f) < 0.02f,
                  $"④ ……而环是原版绿 #00FF21（实测 ({rim2.r:F3}, {rim2.g:F3}, {rim2.b:F3})）"
                + " —— 「整卡不染色」不等于「状态没表达」");

            // ---- ⑤ 端到端：真涨一点攻击，那一格必须是**涨色**（且逐分量相等，不是「偏绿」）----
            v.SetHighlight(CardHighlightState.Normal);
            var dOld = v.Data;
            var dNew = dOld;
            dNew.melee = dOld.melee + 1;
            v.SetData(dNew);
            var up = CardFeel.StatUpColor;
            var c5 = melee.sharedMaterial.color;
            Check(Mathf.Abs(c5.r - up.r) < 0.01f && Mathf.Abs(c5.g - up.g) < 0.01f
                  && Mathf.Abs(c5.b - up.b) < 0.01f,
                  $"⑤ `melee {dOld.melee} → {dNew.melee}` 之后那一格**逐分量 == `CardFeel.StatUpColor`**"
                + $"（实测 ({c5.r:F3}, {c5.g:F3}, {c5.b:F3})；期望 ({up.r:F3}, {up.g:F3}, {up.b:F3})）");

            // ---- ⑥ 灭自证：整卡着色这条路**仍然存在**（只是高亮态不再走它）----
            //    ⚠️ 「把 `_tint` 从数值层的乘法里删掉」也能让 ①② 变绿 —— 那会连带毁掉整卡淡出/详情窗压暗。
            //    这一条按**结构上不可能同时满足**的方式钉住它：直接给一个 0.5 灰的整卡色，
            //    数值层必须变成 0.5×白。删掉乘法 ⇒ 这里还是 1.0 ⇒ 红。
            var flashBefore = v.StatFlash(CardView.StatMelee);
            v.SetTint(new Color(0.5f, 0.5f, 0.5f, 1f));
            var c6 = melee.sharedMaterial.color;
            Check(Mathf.Abs(c6.r - 0.5f * flashBefore.r) < 0.01f
                  && Mathf.Abs(c6.g - 0.5f * flashBefore.g) < 0.01f
                  && Mathf.Abs(c6.b - 0.5f * flashBefore.b) < 0.01f,
                  $"⑥ ★灭自证：`SetTint(0.5 灰)` ⇒ 数值层 = 0.5 × 涨落色"
                + $"（实测 ({c6.r:F3}, {c6.g:F3}, {c6.b:F3})；期望 0.5 × ({flashBefore.r:F2}, {flashBefore.g:F2}, {flashBefore.b:F2})）"
                + "｜🧨 把 `SyncStatLayers` 的 `_tint * c` 改成 `c` ⇒ 这里仍是 1.0 ⇒ 红");
            v.ResetTint();
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ==================================================================
    //  🆕 2026-10-24（W14 · S3）：场上 3D 卡体的插图**左右不许露白边**
    // ==================================================================
    //
    //  根因（已闭合，判据全文 → `资料/普查产出_1024d/S1_场上卡牌四连症状.md` §2·8–2·11）：
    //    `Card3D.shader` 原来把 UV1 铺满**整张** 671×1024 的 crop，而原版 `_CardImage` 是 1024²
    //    图集整图、UV1 只采到图集 x 186.16…840.0 ⇒ 两端各多采**左 9.663 px / 右 7.013 px**，
    //    那正是立绘 PNG 的**纯白卡框内沿**（实测左/右各 5–6 px · 664/1168 张都有）。
    //    屏上卡片约 150 px 宽 ⇒ 5–6 px 缩成 ~1 px 亚像素白线；场上卡每帧按深度重算 `localScale`
    //    （`CardFeel.HookDepthScale` 0.36→0.70）+ 相机 autoZoom ⇒ 那条线每帧落进不同像素格 ⇒「一直闪」。
    //  ⇒ 修法（2026-10-24 W14 内缩 → **2026-10-25 B2 收边 + 按贴图来源给窗**）：
    //     `artUv` 的窗口改由材质属性 `_ArtUvRect` 给（唯一算法 = `CardArt.ArtUvRect`）：
    //       · **sprite 裁块**（672 张）→ 内缩到 `[0.016406, 0.985086] × [0.004883, 0.995117]`
    //         （= 原版窗 `[0.014412, 0.989541] × [0.001700, 0.997800]` 再四边各让 3~5 px，
    //           因为原来的余量只有 0~1.7 px ⇒ 一走 mip 白边就摊回来）
    //       · **整页方图**（496 张）→ **原版窗原样** `[0.1818, 0.8203] × [0.0017, 0.9978]`
    //         （这批的原版 sprite `textureRect` 就是整页，写死那组会把左右各 18% 的多余内容铺上去）
    //
    //  断两条：
    //    ① **常量普查**（确定性，不渲图）：shader 的 `_ArtUvRect` 默认值必须**逐位等于**
    //       `CardArt.ArtUvRect(<sprite 裁块>)`；裁块那一支的四个收边量 = 左 11 / 右 10 / 上 5 / 下 5 px
    //       且**落在原版窗内侧**；整页那一支 = 原版窗原样；窗口**真的被 `artUv` 用了**；
    //       最后扫一遍 1168 张立绘的尺寸，确认只有「方图 / 宽 670–671」两种来源。
    //    ② **渲图级**：把场卡放到上半屏空地、渲 1920×1080，沿立绘窗左右边界 ±3 px 扫列，
    //       不许出现「整列近白（r,g,b 都 >180 且通道差 <24、且该列 ≥90% 的行都这样）且邻列 <80」的列。
    //       ⚠️ 为什么加「近白 + 整列一致」这两条而不是只按亮度 180/80：
    //          立绘**画作内容**本身也可能在边界上出现亮列，那样会把正常画面判成缺陷（假红）。
    //          原版那条白边是**卡框内沿**：颜色近中性白、且沿整条边界高度一致 —— 两个特征加在一起才唯一。
    static void AssertBoardArtNoWhiteEdge(Camera cam)
    {
        // ---------- ① 常量普查（读 shader 源码，⛔ 不跑 Unity 也能判）----------
        //   ⚠️ 路径从 `Application.dataPath` 拼（批处理下 CWD 不一定在工程根）
        string shaderPath = System.IO.Path.Combine(Application.dataPath,
                                                   "CardPresentation/Shaders/Card3D.shader");
        string src = File.Exists(shaderPath) ? File.ReadAllText(shaderPath) : null;
        Check(!string.IsNullOrEmpty(src), $"★ 前提：读得到 `{shaderPath}`（读不到 ⇒ ① 这一组没验到）");
        Vector2 uvMin, uvMax;
        Vector4 artRect;    // shader 的 `_ArtUvRect` **属性默认值**（xy = min, zw = max）
        bool gotMin = ParseShaderFloat2(src, "ArtUvMin", out uvMin);
        bool gotMax = ParseShaderFloat2(src, "ArtUvMax", out uvMax);
        bool gotRect = ParseShaderVector4(src, "_ArtUvRect", out artRect);
        Check(gotMin && gotMax && gotRect,
              "★ 前提：shader 里 `ArtUvMin` / `ArtUvMax`（原版立绘窗）与 `_ArtUvRect` 的属性默认值 **都在**"
            + $"（实读 {gotMin}/{gotMax}/{gotRect}）"
            + "｜🧨 把内缩那层删掉（回到 `artUv = (uv1−Min)/(Max−Min)`）⇒ 这里就红");
        if (gotMin && gotMax && gotRect)
        {
            Check(src.IndexOf("lerp(_ArtUvRect.xy, _ArtUvRect.zw", System.StringComparison.Ordinal) >= 0,
                  "★ `artUv` **真的用了**那个 UV 窗（`lerp(_ArtUvRect.xy, _ArtUvRect.zw, artT)`）"
                + "｜🧨 属性留着、`artUv` 改回不内缩 ⇒ 本条红（防「定义了但没用」）");

            // ---------- ①·a 属性默认值 == `CardArt.ArtUvRect(<sprite 裁块>)`（两处必须一样）----------
            //   🔴 2026-10-25（B2）：窗从「shader 里两颗写死的 const」改成**按贴图来源给**
            //     （`_ArtUvRect`）：672 张 sprite 裁块 · 496 张整页方图，两种窗不同
            //     （判据与算式唯一出处 = `CardArt.ArtUvRect` 的文件头那一段）。
            //     这条断言就是**两处的绑定**：谁改了一边不改另一边 ⇒ 红。
            var cropTex = Resources.Load<Texture2D>("Art/cards/art_am1");    // 671×1024 = sprite 裁块
            var pageTex = Resources.Load<Texture2D>("Art/cards/art_am39");   // 1024²  = 整页方图
            Check(cropTex != null && cropTex.width < cropTex.height,
                  $"★ 前提：裁块样本 `art_am1` 在（{(cropTex == null ? "读不到" : cropTex.width + "×" + cropTex.height)}）"
                + " —— 读不到 ⇒ ①·a 没验到");
            Check(pageTex != null && pageTex.width == pageTex.height,
                  $"★ 前提：整页样本 `art_am39` 在（{(pageTex == null ? "读不到" : pageTex.width + "×" + pageTex.height)}）"
                + " —— 读不到 ⇒ ①·c 没验到");
            if (cropTex != null && pageTex != null)
            {
                var wantCrop = CardArt.ArtUvRect(cropTex);
                float d = Mathf.Max(Mathf.Abs(artRect.x - wantCrop.x), Mathf.Abs(artRect.y - wantCrop.y));
                d = Mathf.Max(d, Mathf.Max(Mathf.Abs(artRect.z - wantCrop.z), Mathf.Abs(artRect.w - wantCrop.w)));
                Check(d < 1e-5f,
                      $"①·a shader 的 `_ArtUvRect` 默认值 = `CardArt.ArtUvRect(<sprite 裁块>)` ⇒ "
                    + $"({wantCrop.x:F6},{wantCrop.y:F6},{wantCrop.z:F6},{wantCrop.w:F6})"
                    + $"（实读 ({artRect.x:F6},{artRect.y:F6},{artRect.z:F6},{artRect.w:F6})，最大差 {d:E2}）"
                    + "｜🧨 只改一边 ⇒ 红（默认值服务于「没被 C# 写过的材质」）");

                // ①·b 裁块那一支的**四个收边量**（px）+ 必须**落在原版窗内侧**
                const float TrX = 176.5f, TrW = 670.5f, TrH = 1024f, Atlas = 1024f;
                var origMin = new Vector2((uvMin.x * Atlas - TrX) / TrW, (uvMin.y * Atlas) / TrH);
                var origMax = new Vector2((uvMax.x * Atlas - TrX) / TrW, (uvMax.y * Atlas) / TrH);
                float padL = wantCrop.x * TrW, padR = (1f - wantCrop.z) * TrW;
                float padB = wantCrop.y * TrH, padT = (1f - wantCrop.w) * TrH;
                Check(Mathf.Abs(padL - 11f) < 0.02f && Mathf.Abs(padR - 10f) < 0.02f
                      && Mathf.Abs(padB - 5f) < 0.02f && Mathf.Abs(padT - 5f) < 0.02f,
                      $"①·b 裁块那一支的四边收边量 = 左 11 / 右 10 / 下 5 / 上 5 px"
                    + $"（实读 {padL:F2}/{padR:F2}/{padB:F2}/{padT:F2}）"
                    + "｜判据 = 1168 张实测每边近白条**最大** 左 8 / 右 7 / 上 2 / 下 2 px，统一 +3 px");
                Check(wantCrop.x > origMin.x && wantCrop.z < origMax.x
                      && wantCrop.y > origMin.y && wantCrop.w < origMax.y,
                      $"★ 前提：收边后的窗**落在原版窗内侧**（原版窗 = [{origMin.x:F5},{origMax.x:F5}]×"
                    + $"[{origMin.y:F5},{origMax.y:F5}]，我们 = [{wantCrop.x:F5},{wantCrop.z:F5}]×"
                    + $"[{wantCrop.y:F5},{wantCrop.w:F5}]）—— 跑出去 = 比原版**多采**，画面反而更错");

                // ①·c 整页方图那一支 = **原版窗原样**（不加余量：方图窗边没有卡框白边，实测最宽近白竖列只占 26% 行）
                var wantPage = CardArt.ArtUvRect(pageTex);
                Check(Mathf.Abs(wantPage.x - uvMin.x) < 1e-4f && Mathf.Abs(wantPage.y - uvMin.y) < 1e-4f
                      && Mathf.Abs(wantPage.z - uvMax.x) < 1e-4f && Mathf.Abs(wantPage.w - uvMax.y) < 1e-4f,
                      $"①·c 整页方图的窗 = 原版窗原样 ({uvMin.x:F4},{uvMin.y:F4},{uvMax.x:F4},{uvMax.y:F4})"
                    + $"（实读 ({wantPage.x:F4},{wantPage.y:F4},{wantPage.z:F4},{wantPage.w:F4})）"
                    + "｜🧨 把裁块那组余量套到方图上 ⇒ 比原版少采 18%，红");
                Check(wantPage.z - wantPage.x > 0.6f,
                      $"★ 前提：整页那一支**没有**被裁块那组收边误用（窗宽 {wantPage.z - wantPage.x:F4} 应 ≈ 0.6385；"
                    + $"裁块那组只有 {wantCrop.z - wantCrop.x:F4}）");

                // ①·d ★ 前提：**每张立绘要么是方图（整页）、要么宽 ≈ 670/671（sprite 裁块）**
                //   —— 别的形状出现时，`ArtUvRect` 的判别式（方图 ⇒ 整页）没覆盖到，
                //     那时会静默按裁块换算（错的），所以这里宁可红。
                CheckArtSizeCensus();
            }
        }

        // ---------- ② 渲图级：沿立绘窗左右边界 ±3 px 扫列 ----------
        const int PW = 1920, PH = 1080;
        var root = new GameObject("art_edge_probe");
        try
        {
            // 夹具 = **S1 点名的那个「有白边」样本** `art_um11`（左/右各 5–6 px 纯白）——
            //   ⚠️ 用 `Placeholder(0)` 的默认立绘不行：它不一定有白边，那样「改回 0..1」也不会有白线
            //   ⇒ 这条断言会在**不该绿的时候绿**。`CardData.artId` 是公开字段，直接换掉即可。
            var dEdge = CardData.Placeholder(0);
            dEdge.artId = "um11";
            var v = CardView.Create(root.transform, dEdge, "art_edge_board", CardFace.Board);
            var body = v.transform.Find("body3D");
            var mf = body != null ? body.GetComponent<MeshFilter>() : null;
            var mr = body != null ? body.GetComponent<MeshRenderer>() : null;
            bool okBody = mf != null && mf.sharedMesh != null && mr != null && mr.sharedMaterial != null;
            Check(okBody, "★ 前提：场卡有 `body3D` + 网格 + 材质（缺一件 ⇒ ② 没验到）");
            if (!okBody) return;

            // ★ 前提：卡体上挂的**就是那张立绘**。挂不上时 `BuildBody3D` 会退回
            //   `Texture2D.whiteTexture` —— 那时整张卡都是白的，「扫到白列」毫无意义（而且必然假红）。
            var artTex = mr.sharedMaterial.GetTexture("_CardImage");
            bool artOk = artTex != null && artTex != Texture2D.whiteTexture
                      && artTex.name.IndexOf("um11", System.StringComparison.OrdinalIgnoreCase) >= 0;
            Check(artOk, $"★ 前提：`_CardImage` 挂的是那张有白边的立绘 `art_um11`"
                       + $"（实得 `{(artTex == null ? "<null>" : artTex.name)}`）"
                       + " —— 挂不上 ⇒ 退回白图 / 换错图 ⇒ 下面这条「没验到」");
            if (!artOk) return;

            // ★ ①·e **材质上真的写进了窗**（不只是 shader 里声明了那个属性）。
            //   为什么要单独断：`_ArtUvRect` 有属性默认值，而**这张卡正好是 671 裁块**（默认值就是对的）
            //   ⇒ 「`BuildBody3D` 漏调 `ApplyArtUvRect`」在画面上**一点都看不出来**，只有回读材质才抓得到。
            {
                var rectTex = artTex as Texture2D;
                var wantRect = CardArt.ArtUvRect(rectTex);
                bool wired = mr.sharedMaterial.HasProperty(CardView.ArtUvRectProp);
                var gotVec = wired ? mr.sharedMaterial.GetVector(CardView.ArtUvRectProp) : Vector4.zero;
                float dr = wired ? Mathf.Max(Mathf.Abs(gotVec.x - wantRect.x), Mathf.Abs(gotVec.y - wantRect.y)) : 999f;
                dr = Mathf.Max(dr, wired ? Mathf.Max(Mathf.Abs(gotVec.z - wantRect.z), Mathf.Abs(gotVec.w - wantRect.w)) : 999f);
                Check(wired && dr < 1e-5f,
                      $"①·e 场卡材质 `_ArtUvRect` = `CardArt.ArtUvRect(<它的立绘>)` ⇒ "
                    + $"({wantRect.x:F6},{wantRect.y:F6},{wantRect.z:F6},{wantRect.w:F6})"
                    + $"（实读 {(wired ? $"({gotVec.x:F6},{gotVec.y:F6},{gotVec.z:F6},{gotVec.w:F6})" : "<没有这个属性>")}）"
                    + "｜🧨 `BuildBody3D` / `SetCardImageTo3DBase` 漏调 `ApplyArtUvRect` ⇒ 只吃默认值 ⇒ 红");
            }

            var mesh = mf.sharedMesh;
            var verts = mesh.vertices;
            // `Mesh.uv2` 就是 shader 的 `uv1`（TEXCOORD1）—— shader 里立绘吃的是 TEXCOORD1
            var uvs = mesh.uv2;
            Check(uvs != null && uvs.Length == verts.Length && verts.Length > 8,
                  $"★ 前提：网格带第二套 UV（`uv2`/TEXCOORD1，{verts.Length} 个顶点；uv2 {(uvs == null ? "null" : uvs.Length.ToString())}）");

            v.SetPose(new Vector3(0f, 1.6f, 0f), 0f, 1f);   // 上半屏空地（与 `AssertRarityGem` 同一处）
            cam.aspect = (float)PW / PH;                     // `WorldToViewportPoint` 也吃它，先设死

            // 立绘窗的屏幕范围：只取**正面**那块的顶点（shader 的 mask 是 `0.18 < uv1.x ≤ 0.82`，
            // 背面 0.015–0.128 与滚边 0.836–0.989 故意落在区间外）。
            float uMinV = float.MaxValue, uMaxV = float.MinValue;
            Vector3 pMin = Vector3.zero, pMax = Vector3.zero;
            float yLo = float.MaxValue, yHi = float.MinValue;
            bool anyFront = false;
            if (uvs != null)
            {
                for (int i = 0; i < verts.Length && i < uvs.Length; i++)
                {
                    float u = uvs[i].x;
                    if (u <= 0.18f || u > 0.82f) continue;
                    anyFront = true;
                    var vp = cam.WorldToViewportPoint(body.TransformPoint(verts[i]));
                    float sy = vp.y * PH;
                    if (sy < yLo) yLo = sy;
                    if (sy > yHi) yHi = sy;
                    if (u < uMinV) { uMinV = u; pMin = body.TransformPoint(verts[i]); }
                    if (u > uMaxV) { uMaxV = u; pMax = body.TransformPoint(verts[i]); }
                }
            }
            Check(anyFront && uMaxV > uMinV + 0.5f,
                  $"★ 前提：找得到正面那块的 UV1 范围（实测 [{uMinV:F4}, {uMaxV:F4}]；"
                + "找不到 = 网格上不是这套 UV ⇒ 下面没验到）");
            if (!anyFront || uMaxV <= uMinV + 0.5f) return;

            var sMin = cam.WorldToViewportPoint(pMin);
            var sMax = cam.WorldToViewportPoint(pMax);
            float k = (sMax.x * PW - sMin.x * PW) / (uMaxV - uMinV);
            float artLeft = sMin.x * PW + (0.18f - uMinV) * k;
            float artRight = sMin.x * PW + (0.82f - uMinV) * k;
            float artXLo = Mathf.Min(artLeft, artRight), artXHi = Mathf.Max(artLeft, artRight);
            float artW = artXHi - artXLo, artH = yHi - yLo;
            Check(artW > 80f && artH > 80f,
                  $"★ 前提：立绘窗在屏上够大（{artW:F0} × {artH:F0} px ≥ 80）—— 太小的话那条 1 px 白线量不出来，"
                + "读到的「没有白边」不算数");
            if (artW <= 80f || artH <= 80f) return;

            // 读回窗口：左右各留 8 px 余量，纵向只取中间一半（避开上下边与滚边）
            int bandY0 = Mathf.RoundToInt(Mathf.Lerp(yLo, yHi, 0.25f));
            int bandY1 = Mathf.RoundToInt(Mathf.Lerp(yLo, yHi, 0.75f));
            var win = new Rect(artXLo - 8f, bandY0, artW + 16f, Mathf.Max(2, bandY1 - bandY0));
            int rw = Mathf.Max(1, Mathf.RoundToInt(win.width));
            int rh = Mathf.Max(1, Mathf.RoundToInt(win.height));
            int rx = Mathf.Clamp(Mathf.RoundToInt(win.x), 0, Mathf.Max(0, PW - rw));
            // （y 方向不用算：`RenderPixels` 内部自己钳 `ry`，我们只需要 x 的局部列。）
            // 边界在这块读回窗口里的**局部列**（`RenderPixels` 会再钳一次，这里照同一算式算）
            int colLeft = Mathf.RoundToInt(artXLo) - rx;
            int colRight = Mathf.RoundToInt(artXHi) - rx;
            // ⚠️ 两条边界必须**真的落在这块读回窗口里**（±3 px 也还够得着）—— 否则下面那圈 `continue`
            //    会把两侧全跳过、扫了个空 ⇒ **静默假绿**。这是本工程反复踩的形状，宁可红。
            bool inWin = colLeft - 4 > 0 && colRight + 4 < rw - 1;
            Check(inWin,
                  $"★ 前提：两条立绘边界都落在这块读回窗口内（局部列 {colLeft} / {colRight}，窗口宽 {rw}）"
                + " —— 落在外面 = 下面根本没扫到，不算通过");
            if (!inWin) return;

            var px = RenderPixels(cam, PW, PH, win);
            Check(px != null && px.Length >= rw * rh,
                  $"★ 前提：渲图真的读回了像素（{rw}×{rh}，实得 {(px == null ? 0 : px.Length)}）");
            if (px == null || px.Length < rw * rh) return;

            // 逐列统计（`GetPixels32` 的 row 0 在**下边**）
            var colMean = new float[rw];
            var colWhiteFrac = new float[rw];
            for (int x = 0; x < rw; x++)
            {
                double s = 0; int white = 0;
                for (int y = 0; y < rh; y++)
                {
                    var c = px[y * rw + x];
                    s += Lum(c);
                    if (c.r > 180 && c.g > 180 && c.b > 180
                        && Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b)) < 24) white++;
                }
                colMean[x] = (float)(s / rh);
                colWhiteFrac[x] = (float)white / rh;
            }
            float allMean = 0f;
            for (int x = 0; x < rw; x++) allMean += colMean[x];
            allMean /= Mathf.Max(1, rw);
            Check(allMean > 8f,
                  $"★ 前提：这块**不是全黑**（列均值平均 {allMean:F1}）—— 全黑说明相机/摆位不对，"
                + "「没扫到白边」不算数");

            // 扫：左右边界 ±3 px
            int badCol = -1; float badMean = 0f, badNb = 0f;
            for (int side = 0; side < 2 && badCol < 0; side++)
            {
                int edge = side == 0 ? colLeft : colRight;
                for (int dx = -3; dx <= 3 && badCol < 0; dx++)
                {
                    int c = edge + dx;
                    if (c <= 0 || c >= rw - 1) continue;
                    if (colWhiteFrac[c] < 0.9f) continue;            // 要**整列**近白（卡框内沿的特征）
                    if (colMean[c] <= 180f) continue;
                    float nl = colMean[c - 1], nr = colMean[c + 1];
                    if (nl < 80f || nr < 80f)
                    {
                        badCol = c; badMean = colMean[c]; badNb = Mathf.Min(nl, nr);
                    }
                }
            }
            Check(badCol < 0,
                  badCol < 0
                    ? $"② 沿立绘左右边界 +−3 px 扫过：**没有「整列近白 + 邻列 <80」的列**"
                      + $"（立绘窗屏上 {artW:F0} px 宽、纵向取样 {rh} 行）"
                    : $"② 🔴 立绘边界第 {badCol - colLeft} px（距左边界）**出现一条近白竖列**："
                      + $"列均值 {badMean:F0} / 邻列 {badNb:F0} / 近白行占比 {colWhiteFrac[badCol]:F2}"
                      + " —— 这就是用户报的「插图白边」"
                  + "｜🧨 把 `artUv` 的内缩改回 0..1（铺满整张 crop）⇒ 这条必红");
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>从 shader 源码里抠出 `key = float2(a, b)` 那两个数（找不到 / 解析失败返回 false）。
    /// ⚠️ 只给 `AssertBoardArtNoWhiteEdge` 的「常量普查」用 —— 它判的是**源码里的常量与算式一致**，
    /// 不是「跑起来看着像」。</summary>
    static bool ParseShaderFloat2(string src, string key, out Vector2 v)
    {
        v = Vector2.zero;
        if (string.IsNullOrEmpty(src)) return false;
        int i = src.IndexOf(key, System.StringComparison.Ordinal);
        if (i < 0) return false;
        int lp = src.IndexOf("float2(", i, System.StringComparison.Ordinal);
        if (lp < 0) return false;
        int rp = src.IndexOf(')', lp);
        if (rp < 0) return false;
        var body = src.Substring(lp + 7, rp - lp - 7);
        var parts = body.Split(',');
        if (parts.Length < 2) return false;
        float a, b;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        const System.Globalization.NumberStyles NS = System.Globalization.NumberStyles.Float;
        if (!float.TryParse(parts[0].Trim().TrimEnd('f', 'F'), NS, inv, out a)) return false;
        if (!float.TryParse(parts[1].Trim().TrimEnd('f', 'F'), NS, inv, out b)) return false;
        v = new Vector2(a, b);
        return true;
    }

    /// <summary>从 shader 的 `Properties` 块里读 `_Key ("...", Vector) = (a,b,c,d,0)` 的**前四个分量**
    /// （第 5 个是 Unity 的占位 0，丢掉）。
    /// <para>⚠️ 找 `= (` 时从属性名那一次出现往后找 —— 属性上方的注释里也可能提到这个名字。</para></summary>
    static bool ParseShaderVector4(string src, string key, out Vector4 v)
    {
        v = Vector4.zero;
        if (string.IsNullOrEmpty(src)) return false;
        int i = src.IndexOf(key, System.StringComparison.Ordinal);
        if (i < 0) return false;
        int eq = src.IndexOf("= (", i, System.StringComparison.Ordinal);
        if (eq < 0) return false;
        int rp = src.IndexOf(')', eq);
        if (rp < 0) return false;
        var body = src.Substring(eq + 3, rp - eq - 3);
        var parts = body.Split(',');
        if (parts.Length < 4) return false;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        const System.Globalization.NumberStyles NS = System.Globalization.NumberStyles.Float;
        var f = new float[4];
        for (int k = 0; k < 4; k++)
            if (!float.TryParse(parts[k].Trim().TrimEnd('f', 'F'), NS, inv, out f[k])) return false;
        v = new Vector4(f[0], f[1], f[2], f[3]);
        return true;
    }

    /// <summary>①·d ★ 前提：`Resources/Art/cards/art_*.png` **每张的尺寸**都能被 `CardArt.ArtUvRect`
    /// 的判别式覆盖 —— 要么**方图**（= 原版 sprite 的 rect 是整页），要么**宽 ≈ 670/671**（= sprite 裁块）。
    /// <para>⚠️ 只读 PNG 的 IHDR 前 24 字节（**不加载纹理**），1168 张 ≈ 几十毫秒。</para>
    /// <para>为什么要有这条：出现第三种形状时 `ArtUvRect` 会**静默**按「裁块」换算（错的）；
    /// 那说明导出侧变了（`工具/import_original_art.py`），必须回来把换算补上，⛔ 不能当成绿。</para></summary>
    static void CheckArtSizeCensus()
    {
        string dir = System.IO.Path.Combine(Application.dataPath, "CardPresentation/Resources/Art/cards");
        var files = Directory.Exists(dir) ? Directory.GetFiles(dir, "art_*.png") : null;
        Check(files != null && files.Length > 0,
              $"★ 前提：`{dir}` 下有立绘（实得 {(files == null ? 0 : files.Length)} 张）—— 没有 ⇒ ①·d 没验到");
        if (files == null || files.Length == 0) return;
        int square = 0, crop = 0, other = 0;
        string firstOther = null;
        foreach (var f in files)
        {
            int w, h;
            try
            {
                var head = new byte[24];
                using (var fs = File.OpenRead(f))
                {
                    if (fs.Read(head, 0, 24) != 24) { other++; firstOther = firstOther ?? System.IO.Path.GetFileName(f); continue; }
                }
                // PNG IHDR：宽/高各 4 字节大端，偏移 16 / 20
                w = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
                h = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
            }
            catch { other++; firstOther = firstOther ?? System.IO.Path.GetFileName(f); continue; }
            if (Mathf.Abs(w - h) <= 1) square++;                       // 整页方图
            else if (w == 670 || w == 671) crop++;                     // sprite 裁块
            else { other++; firstOther = firstOther ?? (System.IO.Path.GetFileName(f) + " " + w + "×" + h); }
        }
        Check(other == 0,
              $"①·d 立绘尺寸普查：{files.Length} 张里 方图 {square} · 裁块 {crop} · **其它 {other}**"
            + (other > 0 ? $"（第一个：`{firstOther}`）⇒ `CardArt.ArtUvRect` 的判别式没覆盖这种形状，"
                         + "要么补换算、要么改导出（⛔ 别让它静默按裁块算）"
                         : " —— 只有这两种来源，判别式覆盖完整"));
    }

    /// <summary>
    /// **描边与软光/影是同一颗件、同一份材质**（`C1-B03` 并层，2026-10-24）。
    ///
    /// 判据（铁律 10 第 1 条：先查原版）：原版 `2DCard.prefab` 全文件**只有 1 颗**
    /// `Card Highlight And Shadow`（`2DCard.prefab:59-78`；`grep -c "m_Name: Card Highlight And Shadow"` = 1），
    /// 它的 Image 材质 `Card Frame SDF` **同时**带 `_Outline*`（绿光）与 `_ShadowColor`（阴影）
    /// ⇒ 绿光与阴影是**同一个 Image 的两个材质属性**，没有第二层。
    /// 我们原来另建过一层 `outlineGlow`（2026-10-21 `W61` `新账-66`），2026-10-22 把 quad 的
    /// `OutlineGlowScale` 降回 1.0 之后两层**几何完全相同、只差 z 0.005** = 纯冗余过绘制。
    /// 判据全文 → `资料/普查产出_1023e/C1_战斗线判据_布置与层级.md` 的 `C1-B03`（裁定 = **并层**）。
    ///
    /// 断四条：
    ///   · ② **结构**：卡面子树里「`Card Frame SDF` 签名」的 `MeshRenderer` **恰好 1 个**、
    ///     且没有叫 `outlineGlow` 的节点（几何与 z 只留一份）；
    ///   · ① **删了那层绿光仍能亮**：`SetOutline` + 推完 0.2 s 补间后材质 `_Outline` 真亮；
    ///   · ③ **场上不亮**：并层之后不能靠「藏那一层」灭描边，`SetFace(Board)` 必须把 `_Outline`
    ///     **连同 `_outlineTarget` 一起**归零（否则回手时 `SetOutline` 会因目标没变早退 ⇒ 永不重亮）；
    ///   · ④ **出生就在场上的卡**默认 α=0（模板值，不需要谁去灭）。
    ///
    /// 🧨 改坏法：
    ///   · 把 `outlineGlow` 那一层建回来 ⇒ ②/②b 红；
    ///   · `OutlineMat` 取不到材质（描边没并到软光/影那一层）⇒ ① 红；
    ///   · 删掉 `SetFace` 里的 `if (board) ClearOutline();` ⇒ ③ 红；
    ///   · `ClearOutline()` 不重置 `_outlineTarget` ⇒ ③ 的「目标 = 0」那半红。
    /// </summary>
    static void AssertOutlineMerged()
    {
        // ⚠️ `CardData.Placeholder` 给的是 **Ember / Tide**（我们自设计的阵营）—— `Resources/Art/card_sdf/`
        //    里**没有**它们的 SDF ⇒ 软光/影那一层根本建不出来 ⇒ 下面全在**假绿**上过。换一个有 SDF 的真阵营。
        var d = CardData.Placeholder(0);
        d.faction = "DarkAngels";

        var root = new GameObject("outline_merged_probe");
        var v = CardView.Create(root.transform, d, "outline_merged");

        // ---- ② 结构：只有一颗 `Card Frame SDF` ----
        //  分辨卡面那份与**战斗状态环**那份（`Card board Frame SDF`）：两边的 `_Outline` / `_ShadowColor` /
        //  `_NoiseIntensity` 都一样，只有 `_Inner_Edge` / `_Inner_Fallof` / `_SpriteAlphaEdge_Inner` 不同 ⇒
        //  用 `_Inner_Fallof = 0.224`（环那份是 1.0）当签名。
        int sdfCount = 0;
        foreach (var mr in v.GetComponentsInChildren<MeshRenderer>(true))
        {
            var m = mr.sharedMaterial;
            if (m == null || !m.HasProperty("_Outline") || !m.HasProperty("_ShadowColor")
                || !m.HasProperty("_Inner_Fallof")) continue;
            if (Mathf.Abs(m.GetFloat("_Inner_Fallof") - 0.224f) < 0.004f) sdfCount++;
        }
        Check(sdfCount == 1,
              "★ C1-B03 ② 卡面子树里 `Card Frame SDF`（`_Inner_Fallof = 0.224`）的 MeshRenderer **恰好 1 个**"
            + $"（原版 `2DCard.prefab` 全文件只有 1 颗 `Card Highlight And Shadow`）｜实测 {sdfCount}"
            + "；🧨 把 `outlineGlow` 专用层建回来 ⇒ 2 ⇒ 红");
        Transform stray = null;
        foreach (var t in v.GetComponentsInChildren<Transform>(true))
            if (t.name == "outlineGlow") { stray = t; break; }
        Check(stray == null,
              "★ ②b 卡面子树里**没有** `outlineGlow` 节点（几何与 z 只留一份）"
            + $"｜实测 {(stray == null ? "无" : "**有一个**")}；🧨 建回那一层 ⇒ 红");

        var prevMode = CardTween.Mode;
        try
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;      // 必须在 `SetOutline` 建补间之前切

            // ---- ① 删了专用层，绿光仍能亮 ----
            //  颜色 = 原版 `cardPlayableColor` **#1C7101**（(28,113,1)/255，预制体字段实读；
            //  判据 → `资料/普查产出_1022/W63_换牌倾斜与绿光判据_调查.md` §2）。
            var want = new Color(28f / 255f, 113f / 255f, 1f / 255f, 1f);
            v.SetOutline(want);
            CardTween.Advance(CardHighlight.OutlineAnimTime + 0.05f);
            var got = v.OutlineColor;
            Check(got.a > 0.9f && Mathf.Abs(got.g - want.g) < 0.02f,
                  "★ C1-B03 ① **删掉专用层之后绿光仍能亮**：`SetOutline(#1C7101)` 推完 0.2 s ⇒ "
                + $"材质 `_Outline` = ({got.r:F3},{got.g:F3},{got.b:F3},a={got.a:F3})"
                + $"｜{v.OutlineDebug}"
                + "；🧨 描边没并到软光/影那份材质上（`OutlineMat` 取不到）⇒ α 恒 0 ⇒ 红");
            Check(Mathf.Abs(v.OutlineFloatForTest("_Inner_Edge") - 0.333f) < 0.004f,
                  "★ ①b 亮描边那份材质就是 `Card Frame SDF`（`_Inner_Edge = 0.333`）"
                + $"｜实测 {v.OutlineFloatForTest("_Inner_Edge"):F4}；🧨 落到别的材质上 ⇒ 红");

            // ---- ③ 换到场上 ⇒ 描边必须归零（并层后不能再靠「藏那一层」）----
            v.SetFace(CardFace.Board);
            Check(v.OutlineTargetForTest.a <= 1e-4f && v.OutlineColor.a <= 0.02f,
                  "★ C1-B03 ③ `SetFace(Board)` ⇒ `_Outline` α 归 **0**、`_outlineTarget` 也清 0"
                + $"（实测 目标 {v.OutlineTargetForTest.a:F4} / 材质 {v.OutlineColor.a:F4}）"
                + "；🧨 删掉 `SetFace` 里 `if (board) ClearOutline();` ⇒ 上一步点亮的 α=1 留在场上 ⇒ 红"
                + "；🧨 `ClearOutline` 不重置 `_outlineTarget` ⇒ 回手时 `SetOutline` 早退、永不重亮 ⇒ 目标那半红");

            // ---- ③b 回手 ⇒ 同一个颜色能**重新点亮**（那条早退的真回归）----
            v.SetFace(CardFace.Full);
            v.SetOutline(want);                                   // 目标此刻是 0 ⇒ 必须重新补间
            CardTween.Advance(CardHighlight.OutlineAnimTime + 0.05f);
            Check(v.OutlineColor.a > 0.9f,
                  "★ ③b 从场上回手后再 `SetOutline(同一个颜色)` ⇒ 描边**重新亮**（α ≈ 1）"
                + $"｜实测 {v.OutlineColor.a:F3}；🧨 `ClearOutline` 只把材质写成 0、不清目标 ⇒ 这里恒 0 ⇒ 红");
        }
        finally { CardTween.Mode = prevMode; }   // 抛异常也别把全局补间模式留在 Manual

        // ---- ④ 出生就在场上的卡：模板 `_Outline` α = 0，不需要谁去灭 ----
        var rootB = new GameObject("outline_board_birth_probe");
        var vb = CardView.Create(rootB.transform, d, "outline_board_birth", CardFace.Board);
        Check(vb.OutlineColor.a <= 0.02f,
              $"★ ④ 场上**出生**的卡描边默认不亮（模板 `_Outline` α=0；实测 {vb.OutlineColor.a:F4}）"
            + "；🧨 并层后把模板 `_Outline` 写成不透明（shader 默认就是不透明白）⇒ 每张场卡一圈白框 ⇒ 红");
        Object.DestroyImmediate(rootB);
        Object.DestroyImmediate(root);
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
        //    **真正的成因只有一件**：`Placeholder` **不填 `subtype`** ⇒ `SubtypeLine`（`CardView.cs` 的 `SubtypeLine`）
        //    在 `case "unit"` 上返回 `RaceTerm(null)` = 空 ⇒ `Fill` 对空串**直接返回 null、兵种行那一层根本不建**。
        //    ⚠️ **错在夹具、不在断言**：那条前提正是用来挡「四层一刀切改 Midline」的判别式，
        //       **不许为了让前提过而把它删掉**。
        //    🔴 **2026-10-18 当场订正（独立审查 `REV_W_四写手.md` F3/F8 抓的）**：我第一版**顺手加了**
        //       `d.faction = "Ultramarines";` 并写了理由「`Placeholder` 给的是 `"Ember"`、不在表里」——
        //       **那个理由是错的**：`"Ember"` **本来就在阵营表里**（`Core/CardText.cs` 第一条就是它，
        //       中文「余烬」），而 `CardText.Faction`（CardPresentation/Core/CardText.cs）对**非空 key 永不回退** ⇒ **阵营行本来就建**。
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

    // ==================================================================
    //  卡面效果文字的版面（🆕 2026-10-24 · `E16`）
    // ==================================================================
    /// <summary>🔴 **效果文字必须「装得进版面框、且不压数值格」**（真机 2026-10-24 反复看到的缺陷：
    /// 卡池/详情窗大卡的字压到攻·甲·血那几个数字上、`战将` 挤在数字行里）。
    ///
    /// **判据（全部来自原版 prefab，⛔ 不是我们的常量）**
    ///  · **版面框**：`bundle_staticgeneralassets_assets_all/GameObject/DescTextUnit.json` 的
    ///    RectTransform `m_SizeDelta` = **152.74 × 76.16 px**（`m_LocalScale 0.01` ⇒ 1.5274 × 0.7616 卡单位）；
    ///    战术卡 `DescTextTactic` = **160 × 78.98 px**（⇒ 1.600 × 0.7898）。
    ///  · **摆位**：同两颗 TMP 是 **`m_VerticalAlignment = 512 (Middle)`** + **固定矩形**（anchor/pivot 0.5）
    ///    ⇒ **文字块中心 = 矩形中心**（`UnitDescAt.y` = 0.7323 / `TacticDescAt.y` = 0.7876）。
    ///    ⚠️ 我们 2026-09-12/15 那次为躲阵营行/兵种行改成了「下沿对齐」⇒ 短文字被一直压到底、
    ///    贴着数字。2026-10-24 改回中心对齐，判据收口在 `CardView.FaceDescBand01`。
    ///  · **数值格**：`Melee Attack Container` apos (−0.836, −1.190) · `m_SizeDelta` **0.4 × 0.4**
    ///    ⇒ 容器上沿 y01 = **0.7972**（`CardView.StatBandTop01`）。
    ///
    /// **四条断言，每条都写明怎么才能红（灭自证）**
    ///  ⓪ 两档中心 = 原版两个矩形的中心（0.7323 / 0.7876）—— 🧨 把 `UnitDescAt`/`TacticDescAt` 改掉 ⇒ 红。
    ///  ① **渲染字墨宽 ≤ 版面框宽** —— 🧨 删掉 `FitsBox` 的宽度判据 / 让 TMP 不折行 ⇒ 红。
    ///     ⚠️ 量的是**网格顶点**（字墨），⛔ 不是 `textBounds` ——「字号对而溢出」是这工程的常客，
    ///     只比字号或行盒照样全绿（`AutoFitBox` 那个老坑）。
    ///  ② **字墨整块落在对称预算里**（上不压阵营行、下不越下门）—— 🧨 把预算改回 `avail01`（0.547 卡单位）
    ///     而中心不动 ⇒ 长文字那张的下沿越门 ⇒ 红。
    ///  ③ **字墨块中心 = 原版矩形中心（容差 0.02 卡面高）** —— 🧨 把 `Fill(..., bottomAlign: true)` 改回来
    ///     ⇒ 中心下移 ≈0.05 卡面高 ⇒ 红。**这条是本笔的主判据。**
    ///  ④ **单位卡：字墨下沿 ≤ 数值容器上沿（0.7972）** —— 🧨 同上，下沿对齐时下沿在 0.834 ⇒ 红。
    ///  ⚠️ 四条各吃一个独立来源（框尺寸 / 对称预算 / 矩形中心 / 容器上沿）⇒「把实现与期望一起改回去」
    ///    满足不了；期望值全走 `CardView` 的公开 accessor（注释里钉着 prefab 字段），断言里不抄数值。
    static void AssertFaceTextFit()
    {
        // ---- ⓪ 判据自检：两档的中心 = 原版两个矩形的中心（prefab apos + 父链 −0.7745）----
        float cu, bu, ct, bt;
        CardView.FaceDescBand01(true, true, out cu, out bu);
        CardView.FaceDescBand01(false, false, out ct, out bt);
        Check(Mathf.Abs(cu - 0.7323f) < 1e-3f && Mathf.Abs(ct - 0.7876f) < 1e-3f,
              $"⓪ 两档的块中心 = 原版矩形的中心（单位 {cu:F4} 应 0.7323 · 战术 {ct:F4} 应 0.7876）"
            + "｜🧨 改 `UnitDescAt` / `TacticDescAt` ⇒ 红");
        Check(bu > 0.05f && bu < 0.35f && bt > 0.05f && bt < 0.35f,
              $"⓪ 对称预算是个合理的高度（单位 {bu:F4} · 战术 {bt:F4} 卡面高；合理区间 0.05~0.35）"
            + "｜🧨 把预算写成 0 或写成整块矩形高 ⇒ 红");

        Check(TmpFont.Available, "★ 前提：字体资产取得到 —— 取不到 `keywords` 那层根本不建，下面几条全假绿");
        if (!TmpFont.Available) return;

        var langBack = Loc.Current;
        Loc.PersistOverride = true;                       // 自检不许动玩家的真设置
        var root = new GameObject("facefit_probe");
        try
        {
            Loc.SetLanguage(AvailableLanguages.Chinese);   // 中文档 = **字最多**那一档（英文更短，过了不代表中文能过）
            var cases = new[]
            {
                new { name = "Chaplain Letharius",  why = "长中文效果文字",   needLines = 2 },
                new { name = "Varro Tigurius",      why = "短中文效果文字",   needLines = 1 },
                new { name = "Master of the Fleet", why = "战术卡（另一档框）", needLines = 1 },
            };
            var pool = RuleEngine.CardDatabase.Load();
            for (int ci = 0; ci < cases.Length; ci++)
            {
                var c = cases[ci];
                RuleEngine.CardDef def = null;
                for (int i = 0; i < pool.Count; i++) if (pool[i].Name == c.name) { def = pool[i]; break; }
                Check(def != null, $"★ 前提：卡池里有 `{c.name}`（{c.why}）—— 拿**真卡**当夹具，⛔ 不用自造串");
                if (def == null) continue;

                var cd = BattleDriver.ToCardData(def, def.Faction);   // 卡面正文只有这一条路（图标也已换好）
                var v = CardView.Create(root.transform, cd, "facefit_" + ci);
                Check(v != null, $"★ 前提：`{c.name}` 的卡面建出来了");
                if (v == null) continue;
                var t = LayerTmp(v.transform, "keywords");
                Check(t != null, $"★ 前提：`{c.name}` 的 `keywords` 层建出来了（正文非空）");
                if (t == null) continue;

                t.ForceMeshUpdate();                          // 批处理没有帧循环
                float ix0, ix1, iy0, iy1;
                // 🔴 **2026-10-25（B2·补）：标架必须是 `FaceContentRoot`（= `2DCard/Front`），不是卡根。**
                //   下面每一条期望值（`DescCenter01` / `FaceDescBand01` / `StatBandTop01`）都是
                //   **`Front` 局部 y01**：它们的源数据是原版 prefab 里 `Front/DescText*` 与
                //   `Front/Card Info/Melee Attack Container` 的 `m_AnchoredPosition`（原版层级现读 →
                //   `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:355-361`：`Card Info` 在 `Front` 下）。
                //   拿卡根量会整体偏 `FrontAposY = 0.08` 卡单位（= **0.02401 y01** —— 正是 E16 那两条
                //   报出来的差）。⚠️ 与同族的 `LayerCenterY` / `TmpInkCenterY` 同一条规则。
                //   ⛔ 别改期望值、也别改 `UnitDescAt`/`TacticDescAt`（那会把生产真的压低 0.08）。
                var inkFrame = v.FaceContentRoot != null ? v.FaceContentRoot : v.transform;
                Check(InkBoxCardLocal(inkFrame, t, out ix0, out ix1, out iy0, out iy1),
                      $"★ 前提：`{c.name}` 量到了**可见字墨**（量不到的话下面几条会拿 0 假绿）");
                if (!(iy1 > iy0)) continue;

                bool isUnit = cd.isUnit;
                bool hasRace = CardView.FaceShowsRace(cd);
                Vector2 box = CardView.DescBoxSize(isUnit);
                Vector2 cen = CardView.DescCenter01(isUnit);
                float bandCenter, bandBudget;
                CardView.FaceDescBand01(isUnit, hasRace, out bandCenter, out bandBudget);
                float half = bandBudget * 0.5f;

                float inkW = ix1 - ix0;
                float top01 = 0.5f - iy1 / CardView.Height;
                float bot01 = 0.5f - iy0 / CardView.Height;
                float mid01 = 0.5f - (iy0 + iy1) * 0.5f / CardView.Height;
                int lines = t.textInfo != null ? t.textInfo.lineCount : 0;
                string tag = $"[{c.name}／{cd.type}／{c.why}，{lines} 行]";

                Check(lines >= c.needLines,
                      $"{tag} ★ 前提：至少 {c.needLines} 行 —— 只有 1 行时 ②③④ 量不到折行后的块");
                // ---- ① 渲染字墨宽 ≤ 版面框宽（量网格顶点，不量行盒）----
                Check(inkW <= box.x * 1.02f,
                      $"{tag} ① 字墨宽 {inkW:F4} ≤ 版面框宽 {box.x:F4} 卡单位（余量 {(box.x - inkW):F4}）"
                    + "｜🧨 让 TMP 不折行 / 删 `FitsBox` 的宽度判据 ⇒ 红");
                // ---- ② 整块落在对称预算里 ----
                Check(top01 >= bandCenter - half - 0.005f && bot01 <= bandCenter + half + 0.005f,
                      $"{tag} ② 字墨纵向 {top01:F4}..{bot01:F4} 落在区间 "
                    + $"{(bandCenter - half):F4}..{(bandCenter + half):F4}（预算 ±{half:F4}）内"
                    + "｜🧨 预算改回 `avail01`（更大）而中心不动 ⇒ 长文字那张越门 ⇒ 红");
                // ---- ③ 块中心 = 原版矩形中心 ----
                Check(Mathf.Abs(mid01 - cen.y) <= 0.02f,
                      $"{tag} ③ ★ 字墨块中心 y01 {mid01:F4} = 原版矩形中心 {cen.y:F4}"
                    + $"（差 {Mathf.Abs(mid01 - cen.y):F4} ≤ 0.02）"
                    + "｜🧨 `Fill(..., bottomAlign: true)`（下沿对齐，2026-09 那版）⇒ 中心下移 ≈0.05 ⇒ 红");
                // ---- ④ 单位卡：不压数值格 ----
                //   ⚠️ 非单位卡（战术/防御）**没有数值格**（`Build` 里那几层由 `d.isUnit` 门控）
                //   ⇒ `FaceDescBand01` 对它们不加这道门，④ 也不该对它们断（否则是给一条生产上不存在的规则发断言）。
                if (isUnit)
                    Check(bot01 <= CardView.StatBandTop01 + 1e-3f,
                          $"{tag} ④ ★ 字墨下沿 {bot01:F4} ≤ 数值容器上沿 {CardView.StatBandTop01:F4}"
                        + "（原版 `Melee Attack Container` 0.4×0.4 @(−0.836,−1.190) 的上沿）"
                        + "｜🧨 下沿对齐那版的下沿在 0.834 ⇒ 红");

                Object.DestroyImmediate(v.gameObject);
            }
        }
        finally
        {
            Loc.RestoreForTest(langBack);
            Loc.PersistOverride = false;
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>**字墨**在**传入标架**局部坐标里的 bbox（x0/x1/y0/y1）—— 扫该 TMP 全部 `isVisible` 字形的四角顶点。
    /// <para>🔴 **2026-10-25（B2·补）**：`card` 这个标架由调用方给，**不是卡根** ——
    /// `AssertFaceTextFit` 传的是 `CardView.FaceContentRoot`（= `2DCard/Front`），因为它的期望值
    /// 全是 `Front` 局部 y01（见调用点那段注释）。拿卡根量会整体偏 `FrontAposY = 0.08`（= 0.02401 y01）。</para>
    /// <para>槽号按**字形自己的** `materialReferenceIndex` 取（效果文字里会混 `&lt;sprite name=…&gt;`
    /// ⇒ TMP 切出第二个材质槽；写死 `[0]` 会把那个字串到 0 号槽里同下标那个字的顶点上，
    /// 索引仍在数组内、**不报错**）。</para>
    /// <para>⚠️ 全仓同族已有两份（`CardFaceProbe.InkSpanY` 只出 y · `ShellScene.TmpSpanPx` 出屏幕像素），
    /// 都拿不到「指定局部系的 bbox」，而 `CardFaceProbe` 不在本笔白名单里 ⇒ 本文件写这一份，
    /// **只服务 `AssertFaceTextFit`**（⛔ 别在别处再写第四份）。</para></summary>
    static bool InkBoxCardLocal(Transform card, TMPro.TextMeshPro t,
                                out float x0, out float x1, out float y0, out float y1)
    {
        x0 = x1 = y0 = y1 = 0f;
        var ti = t != null ? t.textInfo : null;
        if (ti == null || ti.meshInfo == null || ti.meshInfo.Length == 0 || ti.characterInfo == null) return false;
        var meshes = ti.meshInfo;
        var chr = ti.characterInfo;
        float lx = float.MaxValue, hx = float.MinValue, ly = float.MaxValue, hy = float.MinValue;
        int n = Mathf.Min(ti.characterCount, chr.Length);
        for (int i = 0; i < n; i++)
        {
            if (!chr[i].isVisible) continue;                     // 不可见的字 TMP 写四角全 0
            int slot = chr[i].materialReferenceIndex;
            if (slot < 0 || slot >= meshes.Length) continue;     // 槽号越界 ⇒ 不出点（⛔ 不落回 0 号槽）
            var v = meshes[slot].vertices;
            if (v == null) continue;
            int vi = chr[i].vertexIndex;
            for (int k = 0; k < 4; k++)
            {
                int idx = vi + k;
                if (idx < 0 || idx >= v.Length) continue;
                Vector3 p = card.InverseTransformPoint(t.transform.TransformPoint(v[idx]));
                if (p.x < lx) lx = p.x;
                if (p.x > hx) hx = p.x;
                if (p.y < ly) ly = p.y;
                if (p.y > hy) hy = p.y;
            }
        }
        if (lx > hx || ly > hy) return false;
        x0 = lx; x1 = hx; y0 = ly; y1 = hy;
        return true;
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

    // ==================================================================
    //  卡面第六件：**文字底板**（原版 `Front/Textbackgrounds/TextBackground {Big,Small} UI`）
    //  —— 2026-10-19 补（账 `A1500②` 的断言侧）。判据 = **原版那两条 `m_AnchoredPosition`**，
    //  本轮由主对话逐字段现读（`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`
    //  的 `GameObject/TextBackground Big UI_213.json` / `...Small UI_470.json` → `m_Component`
    //  → `RectTransform_2621` / `RectTransform_3192`）：
    //    · `TextBackground Big UI`   sizeDelta **1.7766 × 1.55**  · apos **(−0.001, −0.65000)**
    //    · `TextBackground Small UI` sizeDelta **1.8635 × 0.9621** · apos **(0, −1.02000)**
    //    · 两块走**同一条父链**：`Textbackgrounds`（纯 `Transform`，lpos 0,0,0）→ `Front`
    //      （**apos y = +0.08000**）→ `2DCard`（apos 0,0）—— 所以两块只能**一起**平移。
    //    · Image（两块同款）：sprite `Card Text smooth background`(32×32)、`m_Color=(0,0,0,0.647)`
    //
    //  🔴 `Front` 那个 `+0.08` 是**整套卡面都没折进去**的那一跳（同父的 `CardImage`(−0.044) /
    //     `CardFrame`(−0.03) / `NameTextUnit`（−0.7745 + 0.501 = −0.2735）也都是原值直接用）⇒
    //     我们的卡面**整体**比原版低 0.08，那是整体平移、不影响任何层间关系（账还开着，⛔ 不在本轮）。
    //     ⇒ 本方法量的就是**不含 +0.08 的那一套**：`Big = −0.65` · `Small = −1.02`，
    //       两块**必须差 0.37 卡单位**（= 1.02 − 0.65）。⛔ **别把 `Front` 的 0.08 只折给其中一块**
    //       —— 那正是本轮落地时 `TextBgSmallAt` 走过的路（见 ① 的 🧨，本条今天应当红）。
    //
    //  🔴 **为什么必须有【渲图像素】那一条**：`Core/CardView.cs` 的 `TextBgVisible` **只看
    //     `activeSelf`**，而场景里 `m_IsActive` 就是 1 ⇒ **底板一个像素都没画，它照样返回 true**。
    //     2026-10-19 实测：**16k+ 条断言全绿**，扫全部 130 张 `_tmp_view/cardface/*.png` 底板 **0 命中**
    //     ⇒「层建了 + 开着 + 网格对 + 材质对」四条一起绿，仍然**推不出**「画出来了」。
    //
    //  ⚠️ 本块自带那几个渲染小工具（`LayerPxRect` / `RenderPixels` / `MeanLum`）就放在这里，
    //     ⛔ 没挪到 `Shot` 旁边 —— 少一处改动少一处风险。
    // ==================================================================
    static void AssertTextBg(Camera cam)
    {
        const float BigY = -0.65f, SmallY = -1.02f, Gap = 0.37f;
        const float BigW = 1.7766f, BigH = 1.55f, SmallW = 1.8635f, SmallH = 0.9621f;
        const float BgAlpha = 0.647f;
        const float NameGapUnit = 0.3765f, NameGapTactic = 0.1890f;

        var root = new GameObject("textbg_probe");
        try
        {
            // 两档：`CardData.Placeholder(i)` 的 `keywords` = (i % 3 == 0) ? "VANGUARD" : ""
            var dBig = CardData.Placeholder(0);        // 有描述 ⇒ `TextBackground Big UI`
            var dSmall = CardData.Placeholder(1);      // 无描述 ⇒ `TextBackground Small UI`
            Check(!string.IsNullOrEmpty(dBig.keywords) && string.IsNullOrEmpty(dSmall.keywords),
                  "★ 前提：两份夹具的 `keywords` **一有一无**（`Placeholder(0)` = \"VANGUARD\" / "
                + "`Placeholder(1)` = \"\"）—— 否则下面「按有没有描述分档」那一格是空跑");
            var vBig = CardView.Create(root.transform, dBig, "textbg_big");
            var vSmall = CardView.Create(root.transform, dSmall, "textbg_small");
            var rBig = LayerMesh(vBig.transform, "textBg");
            var rSmall = LayerMesh(vSmall.transform, "textBg");
            Check(rBig != null && rSmall != null,
                  "★ 前提：两张卡都建出了 `textBg` 层（底板图取不到就**整块不建** —— 那是 `CardView` 里"
                + "明写的降级）｜没建出来 = 这一档**没验到**，不是通过");
            if (rBig == null || rSmall == null) return;

            // ---- ① 板心**绝对值** = 原版那两条 `m_AnchoredPosition` ----
            //  灭自证：期望值来自**原版字段**，不是我们的常量 ⇒「把常量连实现一起改回去」满足不了。
            float yBig = LayerCenterY(vBig, rBig), ySmall = LayerCenterY(vSmall, rSmall);
            Check(Mathf.Abs(yBig - BigY) < 1e-3f,
                  $"① 有描述 ⇒ `TextBackground Big UI` 的板心 y = **{BigY:F2} 卡单位**（实测 {yBig:F4}；"
                + "出处 = `RectTransform_2621.m_AnchoredPosition.y` = −0.65）"
                + "｜🧨 把 `CardView.TextBgAt.y` 改回 0.67111（板心 −0.57）⇒ 本条红");
            Check(Mathf.Abs(ySmall - SmallY) < 1e-3f,
                  $"① 无描述 ⇒ `TextBackground Small UI` 的板心 y = **{SmallY:F2} 卡单位**（实测 {ySmall:F4}；"
                + "出处 = `RectTransform_3192.m_AnchoredPosition.y` = −1.02）"
                + "｜🧨 把 `CardView.TextBgSmallAt.y` 改成 0.78217（板心 −0.94）⇒ 本条红"
                + "（−0.94 = 原版 apos −1.02 **又折了一次 `Front` 的 +0.08** ⇒ 与上面那块**不同口径**）");
            Check(Mathf.Abs((yBig - ySmall) - Gap) < 1e-3f,
                  $"①·b 两块板心**必须差 {Gap:F2} 卡单位**（= 1.02 − 0.65；两条 `apos` 同父同层，"
                + $"只能一起平移），实测 {yBig - ySmall:F4}"
                + "｜🧨 只挪其中一块（今天就是这样）⇒ 本条红，而别处全绿");

            // ---- ② 与**卡上那一行字**的实测关系（与 ① 结构上不可能同时满足）----
            //  ① 锚的是**原版那一对 apos 原值**，② 锚的是**我们卡上那层 TMP 的实测墨心** ——
            //  两个来源不可能被同一次改动一起满足（例：把底板与 `UnitNameAt` 一起下移 0.08
            //  ⇒ ① 红、② 仍绿；只挪底板 ⇒ 两条都红）。
            //  🔴 **2026-10-22（`W62/T3`）**：容差从 0.02 收到 **0.005** —— 原来那个 0.02 是为了盖住
            //    「量行盒心」带来的 `c_G`（行盒心 ↔ 墨心，∝ 字号，实测 0.0181/0.0201）；现在量的就是墨心，
            //    那个系统性偏移不存在了。判据 = 节点中心 −0.2735（原版 `RectTransform_4173703600236829632`
            //    的 apos + 父级，见 `TmpInkCenterY` 的注释）。
            var tBig = LayerTmp(vBig.transform, "title");
            bool tOk = tBig != null;
            if (tOk)
            {
                tBig.ForceMeshUpdate();
                float l0, h0;
                tOk = CardFaceProbe.InkSpanY(tBig, out l0, out h0) && h0 - l0 > 0.01f;
            }
            Check(tOk, "★ 前提：卡名那层 TMP 建出来了、**字墨非空**"
                     + "（量不到可见字形 ⇒ 墨心是 NaN ⇒ 下面那条会**假绿**）");
            if (tOk)
            {
                float inkBig = TmpInkCenterY(vBig, tBig);
                Check(Mathf.Abs(yBig - (inkBig - NameGapUnit)) < 0.005f,
                      $"② 板心 = **单位**卡名**墨心** − {NameGapUnit:F4} 卡单位"
                    + $"（板心 {yBig:F4}，卡名墨心 {inkBig:F4}，差 {inkBig - yBig:F4}）"
                    + "｜旧口径量的是 `textBounds.center`（**行盒心**），Midline 下它与墨心差一个 `c_G`（∝ 字号）"
                    + " ⇒ 旧容差只能放 0.02、抓不出字号回归；现在量墨心、容差 0.005"
                    + "｜🧨 把底板与 `UnitNameAt` 一起平移 ⇒ ① 红、本条仍绿（这就是两条的分工）");
            }
            var dTac = CardData.Placeholder(0);
            dTac.isUnit = false;                       // 战术卡走 `TacticNameAt` 那一套
            var vTac = CardView.Create(root.transform, dTac, "textbg_tactic");
            var rTac = LayerMesh(vTac.transform, "textBg");
            var tTac = LayerTmp(vTac.transform, "title");
            bool tTacOk = rTac != null && tTac != null;
            if (tTacOk)
            {
                tTac.ForceMeshUpdate();
                float l1, h1;
                tTacOk = CardFaceProbe.InkSpanY(tTac, out l1, out h1) && h1 - l1 > 0.01f;
            }
            Check(tTacOk, "★ 前提：**战术卡**那一份的 `textBg` + 卡名层都建出来了、字墨非空");
            if (tTacOk)
            {
                float yTac = LayerCenterY(vTac, rTac), inkTac = TmpInkCenterY(vTac, tTac);
                Check(Mathf.Abs(yTac - (inkTac - NameGapTactic)) < 0.005f,
                      $"② 板心 = **战术**卡名**墨心** − {NameGapTactic:F4} 卡单位"
                    + $"（板心 {yTac:F4}，卡名墨心 {inkTac:F4}，差 {inkTac - yTac:F4}）"
                    + "｜容差 0.005（口径同单位卡那条：量墨心，不量行盒心）"
                    + "｜🧨 把单位/战术两套卡名位置写成一个（不分档）⇒ 本条红");
            }

            // ---- ③ 两档**都断**（铁律 5·c）：尺寸也跟着分档 ----
            Vector2 sBig = LayerSizeCardUnits(vBig, rBig);
            Vector2 sSmall = LayerSizeCardUnits(vSmall, rSmall);
            Check(Mathf.Abs(sBig.x - BigW) < 1e-3f && Mathf.Abs(sBig.y - BigH) < 1e-3f,
                  $"③ 有描述那块 quad = **{BigW:F4} × {BigH:F2} 卡单位**（实测 {sBig.x:F4} × {sBig.y:F4}）");
            Check(Mathf.Abs(sSmall.x - SmallW) < 1e-3f && Mathf.Abs(sSmall.y - SmallH) < 1e-3f,
                  $"③ 无描述那块 quad = **{SmallW:F4} × {SmallH:F4} 卡单位**（实测 {sSmall.x:F4} × {sSmall.y:F4}）"
                + "｜🧨 两档共用一个尺寸（不分档）⇒ 本条红");

            var mfBig = rBig.GetComponent<MeshFilter>();
            var meshBig = mfBig != null ? mfBig.sharedMesh : null;

            // ---- ③·b **Sliced 九宫格**（账 `A1933`）----
            //  判据全部来自**原版资产字段**（⛔ 不是我读我们自己的常量）：
            //    · Image：`m_Type = 1`(Sliced) · **`m_PixelsPerUnitMultiplier = 14.7`**
            //      （`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_-2228335281872200768.json`；
            //        Big / Small 两块同款。⚠️ **Image 上没有 `m_Border`** —— uGUI 的 Image 没这个字段，
            //        边宽只存在 sprite 里，见下一行）
            //    · sprite：32×32 · **`m_Border = {7,0,7,9}`**（边宽的唯一出处）· **`m_PixelsToUnits = 100`**
            //    · 边宽换算 = **uGUI 自己的算式**（`com.unity.ugui/Runtime/UGUI/UI/Core/Image.cs:1157`
            //      `adjustedBorders = GetAdjustedBorders(sprite.border / multipliedPixelsPerUnit, rect)`，
            //      而 `multipliedPixelsPerUnit = spritePPU ÷ canvasRefPPU × ppuMul`，原版 Canvas 的
            //      `m_ReferencePixelsPerUnit` **全库只有 100** ⇒ 左/右 = **7 ÷ 14.7**、上 = **9 ÷ 14.7** 卡单位）
            //    · **下边 0 ⇒ 下面那一行高为 0**、uGUI `GenerateSlicedSprite` 直接跳过 ⇒ **只画 6 块**
            //      ⇒ 一块 mesh = 6 × 4 = **24 顶点 / 12 三角**；竖切线 4 条、横切线 **3** 条（下边重复）
            //  🧨 **改坏法**：换回 `SpriteQuad`（整张拉伸、4 顶点）⇒ 下面每条都红。
            const float SliceLR = 7f / 14.7f, SliceT = 9f / 14.7f;     // 原版 border ÷ ppuMul
            if (meshBig != null)
            {
                var gx = LayerGridLinesCardUnits(rBig, true);
                var gy = LayerGridLinesCardUnits(rBig, false);
                Check(gx.Length == 4 && gy.Length == 3,
                      $"③·b **Sliced 的切线**：竖切线 **4** 条（左缘 / 左切 / 右切 / 右缘）· 横切线 **3** 条"
                    + $"（下缘 = 下切【重合成一条，因为下 border = 0】· 上切 · 上缘）｜实测 x {gx.Length} 条 / y {gy.Length} 条"
                    + "｜🧨 整张拉伸那一版是 2 / 2 ⇒ 本条红");
                Check(gx.Length == 4
                      && Mathf.Abs((gx[1] - gx[0]) - SliceLR) < 2e-3f
                      && Mathf.Abs((gx[3] - gx[2]) - SliceLR) < 2e-3f,
                      $"③·b 左/右切到内边 = **{SliceLR:F4} 卡单位**（= 原版 `m_Border.x = 7` ÷ `m_PixelsPerUnitMultiplier = 14.7`）"
                    + $"｜实测 左 {((gx.Length == 4) ? (gx[1] - gx[0]) : float.NaN):F4} · 右 {((gx.Length == 4) ? (gx[3] - gx[2]) : float.NaN):F4}"
                    + "｜🧨 把 border 当成「占宽的比例」（7/32）⇒ 本条红");
                Check(gy.Length == 3 && Mathf.Abs((gy[2] - gy[1]) - SliceT) < 2e-3f,
                      $"③·b **上切**到底板上缘 = **{SliceT:F4} 卡单位**"
                    + $"（= `m_Border.w = 9` ÷ 14.7；占底板高 {BigH:F2} 的 {SliceT / BigH:P1}）"
                    + $"｜实测 {((gy.Length == 3) ? (gy[2] - gy[1]) : float.NaN):F4}"
                    + "｜🔑 **这就是本账的可观测差别**：贴图那 7 行羽化带落在这一段里"
                    + $" ⇒ 羽化斜坡占底板高 {(SliceT * 7f / 9f) / BigH:P1}；整张拉伸那版是 {7f / 32f:P1}"
                    + "｜🧨 整张拉伸 / 把 9px 折成 9/32 比例 ⇒ 本条红");

                // UV 切点：按**贴图 32px** 切（⛔ 与上面那个 ÷14.7 是两码事 —— 一个切 UV、一个定画出来的宽度）
                var uvx = new List<float>(); var uvy = new List<float>();
                foreach (var q in meshBig.uv) { uvx.Add(q.x); uvy.Add(q.y); }
                var uu = DistinctSorted(uvx, 1e-4f);
                var vv = DistinctSorted(uvy, 1e-4f);
                Check(uu.Length == 4 && Mathf.Abs(uu[1] - 7f / 32f) < 1e-4f && Mathf.Abs(uu[2] - 25f / 32f) < 1e-4f,
                      $"③·b UV 的竖切点 = **7/32 / 25/32**（原版 sprite `m_Border.x = 7` · `z = 7` ÷ 贴图 32px）"
                    + $"｜实测 {string.Join(" / ", uu)}"
                    + "｜🧨 整张拉伸 ⇒ UV 只有 0 / 1 两个值、长度 2 ⇒ 本条红");
                Check(vv.Length == 3 && Mathf.Abs(vv[1] - 23f / 32f) < 1e-4f,
                      $"③·b UV 的横切点 = **23/32**（原版 sprite `m_Border.w = 9` ÷ 32px；**下边 0 ⇒ v=0 只有一条**）"
                    + $" ⇒ 顶上那 9 行（含 7 行羽化带）落在这 39.5% 里"
                    + $"｜实测 {string.Join(" / ", vv)}"
                    + "｜🔴 这条是**羽化带占高**的直接判据（整张拉伸那版羽化带只占 7/32 = 21.9%）");
            }

            // ---- ③·c **两档都断**（铁律 5·c）----
            //  原版那**两块**是各自被 Sliced 切的（同一个 `m_Border`、各自的 rect）⇒
            //  **边宽一样、占各自高度的比例不一样**：上 slice 占 Big 的 39.5% ↔ 占 Small 的 63.6%
            //  （`0.6122 / 0.9621`）—— ⛔ 不是「一块等比缩放」。
            var mfS = rSmall.GetComponent<MeshFilter>();
            var meshSmall = mfS != null ? mfS.sharedMesh : null;
            if (meshSmall != null)
            {
                var sx = LayerGridLinesCardUnits(rSmall, true);
                var sy = LayerGridLinesCardUnits(rSmall, false);
                Check(sx.Length == 4 && sy.Length == 3
                      && Mathf.Abs((sx[1] - sx[0]) - SliceLR) < 2e-3f
                      && Mathf.Abs((sy[2] - sy[1]) - SliceT) < 2e-3f,
                      $"③·c **无描述那一档（Small）也按 Sliced 切**：竖/横切线 4/3 条 · 左/右 {SliceLR:F4} · "
                    + $"上 {SliceT:F4} 卡单位（与 Big 同一套 border，但**它自己的板高只有 {SmallH:F4}**"
                    + $" ⇒ 上 slice 占 **{SliceT / SmallH:P1}**，Big 是 {SliceT / BigH:P1}）"
                    + $"｜实测 x {sx.Length} / y {sy.Length} · 上 {((sy.Length == 3) ? (sy[2] - sy[1]) : float.NaN):F4}"
                    + "｜🧨 只给 Big 换 Sliced、Small 留拉伸（或两档共用一块 mesh）⇒ 本条红");
            }

            // ---- ④ 防「活着但不出像素」----
            Check(meshBig != null && meshBig.vertexCount == 24,
                  $"④·0 底板的网格 = **24 顶点**（= 原版 Sliced 的 **6 块 × 4**）、不是 null"
                + $"（实测 {(meshBig == null ? "null" : meshBig.vertexCount.ToString())}）"
                + " —— 静态缓存网格（`TextBgQuad`）被 `UnloadUnusedAssets` 收走时 `sharedMesh` 变 null，"
                + "那一层会**静默不画**");
            var matBig = rBig.sharedMaterial;
            Check(matBig != null && Mathf.Abs(matBig.color.r) < 1e-4f && Mathf.Abs(matBig.color.g) < 1e-4f
                  && Mathf.Abs(matBig.color.b) < 1e-4f && Mathf.Abs(matBig.color.a - BgAlpha) < 2e-3f,
                  $"④·0 材质 = 原版那张 Image 的 `m_Color` **纯黑 α {BgAlpha:F3}**"
                + $"（实测 {(matBig == null ? "null" : matBig.color.ToString())}）"
                + "｜⚠️ 这一条**单独不算数**：2026-10-19 实测它逐字吻合、而像素一个都没画");

            // 把探针卡摆到**画面上半部那块空地**（我方手牌在 y≈−3.8、台面在 y≈−1.56 ⇒ 上半屏是空的）
            vBig.SetPose(new Vector3(0f, 1.6f, 0f), 0f, 1f);
            const int PW = 1920, PH = 1080;
            cam.aspect = (float)PW / PH;               // ⚠️ `WorldToScreenPoint` 也吃它，先设死再量
            var pxIn = LayerPxRect(cam, rBig, PW, PH);
            // ⚠️ 原来自这里还量了一条 `pxUp`（矩形**正上方**同宽半高那一条）给 ④·b 用 —— **2026-10-14 删掉**：
            //   正上方是**立绘窗**，拿文字板去比插画，原版没有这条判据（详见下面 ④ 那段的订正痕）。
            bool inView = pxIn.width > 8f && pxIn.height > 8f
                       && pxIn.xMin >= 0f && pxIn.yMin >= 0f && pxIn.xMax <= PW && pxIn.yMax <= PH;
            Check(inView,
                  $"★ 前提：底板那 {BigW:F4} × {BigH:F2} 卡单位**整块落在 {PW}×{PH} 画面内**"
                + $"（实测像素矩形 x {pxIn.x:F0} y {pxIn.y:F0} w {pxIn.width:F0} h {pxIn.height:F0}）");
            if (inView)
            {
                // ==========================================================================
                // 🔴 **2026-10-14（第二十九会话 · 账 `A1922`）本条整个换过判据** —— 原来那两格都是错的。
                //
                // **原来错在哪（诊断报告 `资料/普查产出_第二十九会话/A1922_文字底板_诊断.md`）**：
                //   ① 原来拿「**全卡**开/关后的净掉幅」去比一个固定阈值（≥25%，来路写的是
                //      「按 `m_Color` 的 α 0.647 反推该掉 ≈65%」）。**两个前提都不成立**：
                //      (a) 本工程是 **Linear** 色彩空间（`ProjectSettings.asset` 的 `m_ActiveColorSpace = 1`）
                //          ⇒ 0.647 那个数只在**线性**空间成立；
                //      (b) 底板**上方**的层（立绘 / 卡框 / 文字）**不被它压暗** ⇒ 全卡净掉幅必然
                //          **小于**这一层自己的压暗量，小多少**取决于这张卡被压的是多亮的东西**
                //          —— 实测同一次 run 两次量出 22.8% / 12.8% ⇒ **固定阈值从原理上就不稳**。
                //   ② 原来那条 `④·b` 比的是「底板矩形 vs 它**正上方**那一条」—— 而正上方就是**立绘窗**，
                //      等于拿文字板去比插画（插画本来就更暗）。**原版没有这条判据 ⇒ 已删**
                //      （上面那条 `pxUp` 的声明一并去掉）。
                //
                // ✅ **新判据（照原版、且灭自证）**：**把这一层单独留出来**，开 / 关各渲一帧，
                //    读数换到**线性**空间求**透过率**，它应当等于
                //    `1 − 原版 m_Color.a × 这块 quad 上贴图的平均 alpha`：
                //    🔴 **2026-10-22（`A1934` 收口）：估计量 = `mean-of-ratios`（逐像素 `on_i/off_i`
                //    再平均），⛔ **不是** `mean(lin 开) / mean(lin 关)`（= `Σon/Σoff`，ratio-of-means）。
                //    后者在**背景不均匀**时会偏 —— 判据 → `资料/普查产出_1021/W61_卡牌线_T07尾巴.md` §3·2。
                //      · `0.647` 来自**原版 Image 的 `m_Color`**（`bundle_battleprefabs_vfxandmisc_assets_all`
                //        的 `MonoBehaviour_-2228335281872200768.json`，Big / Small 两块同款）；
                //      · 贴图 `Card_Text_smooth_background` = 32×32 · RGB 全 255 ·
                //        **在这块 quad 上的 alpha 均值** —— 🔴 **2026-10-14（`A1933`）起按 Sliced 数**：
                //        **0.830**（6 块：上缘 39.5% 是羽化带、中段拉伸；算法 = `Core/CardView.cs` 的
                //        `TextBgQuad` 那一段）。整张拉伸那一档 = **0.8776**（= 贴图自己的均值，**改前**的映射，
                //        留作对照，⛔ 别再当成"我们的映射"）；
                //      · **背景无关**：开/关两帧的底（背景 + 卡其余层）**逐像素相同** ⇒ 比值里约得掉；
                //      · **灭自证**：期望值来自**原版字段 + 原版贴图**，⛔ 不来自我们自己的读数或代码。
                //    ⚠️ **映射已按原版改成 Sliced**（`A1933`，2026-10-14 落地）⇒ 期望值是上面那条 `0.830`。
                //    ⛔ **不许为了把这格弄绿去动容差**：容差只由上面那两个**原版**数决定。
                // ==========================================================================
                const float ColA = 0.647f;                       // 原版 Image 的 m_Color.a
                const float AlphaSliced = 0.830f;                // 原版 Sliced 映射的均值（**现在就是我们的**）
                const float AlphaStretch = 0.8776f;              // **改前**那份映射（整张拉伸），留作对照
                const float ThruSliced = 1f - ColA * AlphaSliced;     // ≈0.463 ← 现在是这一档
                const float ThruStretch = 1f - ColA * AlphaStretch;   // ≈0.432（改前）
                const float Tol = 0.06f;

                // 「只留这一层」：把同卡其余**所有** `MeshRenderer` 关掉（与下面 `ProbeTextBg` 同一套做法）
                // 🔴 **2026-10-22（`W64` 同步点修正）：关的是【整个沙盒】的 renderer，⛔ 不是只关 `vBig` 这一棵。**
                //   **为什么**：本方法的沙盒 `root` 里还有 **`vSmall`（:956）与 `vTac`（:1011）** 两张卡，
                //   它们**从未 `SetPose`**（只有 `vBig` 在 :1131 被摆到 `(0,1.6,0)`）⇒ 一直待在沙盒原点，
                //   而这三张卡 z 相同、队列相同 ⇒ 按到相机的距离排序 ⇒ 原点的两张**画在 `vBig` 前面**。
                //   原来只关 `vBig.transform` 的 renderer ⇒ 这两张**留在帧里、盖住待测矩形的一部分**；
                //   那部分像素「开/关底板」当然一个像素都不变 ⇒ 逐像素比值 = 1.0 ⇒ 把均值往上拖。
                //   **实测证据**（`_tmp_view/w64_sync_cardbase.log:3283`）：同一块矩形
                //   `mean-of-ratios` = **0.610**、`Σon/Σoff` = 0.912，而干净的探针帧（自己的 `root` 里只有一张卡）
                //   同矩形 = **0.461** ⇔ 原版字段 0.463。⇒ **是帧不合格，不是估计量、更不是画面。**
                var others = new List<MeshRenderer>();
                foreach (var rr in root.transform.GetComponentsInChildren<MeshRenderer>(true))
                    if (rr != rBig && rr.enabled) { rr.enabled = false; others.Add(rr); }
                var pxOn = RenderPixels(cam, PW, PH, pxIn);
                rBig.gameObject.SetActive(false);
                var pxOff = RenderPixels(cam, PW, PH, pxIn);
                rBig.gameObject.SetActive(true);
                foreach (var rr in others) if (rr != null) rr.enabled = true;

                float linOn = MeanLumLinear(pxOn);
                float linOff = MeanLumLinear(pxOff);
                // ==================================================================
                // 🔴 **2026-10-22（`A1934` 收口）：估计量换成 `mean-of-ratios`** —— 判据 →
                //    `资料/普查产出_1021/W61_卡牌线_T07尾巴.md` §3·2 / §3·4③。
                //
                //  **旧估计量（`Σon/Σoff`，ratio-of-means）为什么是假象**：文字底板是 **Sliced 九宫格**，
                //  上缘 9px（含 7 行羽化带）落在 quad 高 **39.5%** 里（上面 ③·b 那条断言量的就是它）。
                //  羽化带 alpha 低（透过率高），可它**正好压在更暗的背景上**（实测 off 帧线性亮度：
                //  顶部 0.026 / 羽化段 0.041 / 核心段 0.181）⇒ 亮度加权的 `Σon/Σoff` 把羽化段
                //  系统性压低权重，读数被核心段（alpha ≈ 0.996）拖到 **0.380**；
                //  同一帧、同一矩形、同一份 `Color32[]`，**逐像素求比值再平均 = 0.4609**，
                //  而原版字段推出 **0.463**（差 0.002）⇒ **底板画得对**。
                //  ⇒ 用户 2026-10-22 裁定：**卡面数值一个都不动、只修仪器的估计量**。
                //  ⛔ 所以这里换的**只是量法**，`TextBgAlpha` / 贴图 alpha / `m_Color` 一个字节都没动。
                // ==================================================================
                int nRat = 0, nRatSkip = 0;
                float thru = MeanThruLinear(pxOn, pxOff, out nRat, out nRatSkip);   // ★ 唯一使用的估计量
                // 旧估计量：**只留作日志对照**（W61 §3·2 那张表要两个数并排才看得出是谁在骗人）
                float thruRom = linOff > 1e-6f ? linOn / linOff : -1f;
                // 🔴 **核心段（下 55%）单独再量一次** —— 这一条查的是**帧合不合格**，不是画面：
                //  文字底板是 Sliced：**上缘 9px**（含 7 行羽化带）落在 quad 高 39.5% 里，**下缘 border = 0**
                //  ⇒ **下 55% 是拉伸的核心段、贴图 alpha ≈ 1**。核心段的透过率必须 = `1 − 0.647 × 1.0`
                //  （≈0.353）。若这一帧里有东西（别的卡 / 遮挡物）盖住它的一部分，那些像素读 1.0
                //  ⇒ 核心段读数被抬高、下面的 `④·核心` 红 —— 这正是 2026-10-22 那次 0.610 的病根。
                int rwPx = Mathf.RoundToInt(pxIn.width);
                int rhPx = rwPx > 0 ? pxOn.Length / rwPx : 0;
                int coreRows = Mathf.Max(1, Mathf.RoundToInt(rhPx * 0.55f));
                float thruCore = MeanThruLinearRows(pxOn, pxOff, rwPx, 0, coreRows, out _, out _);
                const float ThruCore = 1f - ColA * 1f;          // 0.353 —— 核心段贴图 alpha = 1
                string band = $"原版字段推出的带宽 [{ThruSliced - Tol:F3}, {ThruSliced + Tol:F3}]"
                            + $"（现在是 Sliced 映射 {ThruSliced:F3}；**改前**那份整张拉伸是 {ThruStretch:F3}，"
                            + $"容差 ±{Tol:F2}）";
                // 🔴 **2026-10-14 收窄说明（账 `A1934`）**：本条**只断「画了没有」，不断量级**。
                //   实测：这条 in-engine 配对读数 **0.927**（只掉 7%），而**同一次 run 存的图**
                //   （`_tmp_view/cardbase/cardbase_probe_textbg_only.png`）在矩形左右边缘量到的台阶是
                //   **≈38%**（外 112.0→内 69.8 / 内 53.5→外 87.9）——**那正好落在原版字段推的 0.432 一带**。
                //   ⇒ **两条测量路互相矛盾**（`Shot` 存图那条 vs `RenderPixels` 读回那条），
                //     所以此刻**不能拿原版带宽当红线**：真按带宽断，红的是「我们没画够」，
                //     而存图证据说我们画够了。**先把矛盾查清，再收窄判据**（账 `A1934`）。
                //   ⛔ 不许反过来「把带宽放宽到 0.95 就算过」—— 那正是把未确认的东西盖成绿；
                //     这里只是把**能确证的那半**（画没画）立成断言，量级那半**明写成待查**。
                // ✅ **2026-10-22：量级那半收回原版带** —— 0.378 那个读数已查清 = **估计量的偏差**
                //   （W61 §3·2：同一帧 `Σon/Σoff` 0.380 ↔ 逐像素比值 0.461 ↔ 原版字段 0.463）。
                // ✅ **2026-10-22（`W64` 同步点）：W61 §3·3 那条「只压暗 7–9%，最后一环没钉死」也查清了** ——
                //   **是本帧里有东西盖住了这一层的一部分**，不是合成失败：
                //   同一块矩形 32,064 px 里，被**沙盒里另外两张卡**（`vSmall` / `vTac`，未摆位）盖住的那部分
                //   「开/关底板」一个像素都不变 ⇒ 逐像素比值 = 1.0。换估计量之后读数从 0.912 变 0.610
                //   （`_tmp_view/w64_sync_cardbase.log:3283`），仍偏 —— 正说明**估计量已对、帧不合格**。
                //   ⇒ 修法 = 上面那段：关**整个沙盒**的 renderer（`root`），⛔ 不是放宽带宽。
                Check(linOff > 1e-6f && !float.IsNaN(thru) && thru > 0f && thru <= 0.95f,
                      $"④ ★ **底板真的画出了像素**：只留这一层，开/关各渲一帧，**线性**空间透过率 = {thru:F3}"
                    + "（估计量 = `mean-of-ratios`；要 ≤ 0.95 —— 透过率 1.000 就是「这一层一个像素都没画」）"
                    + $"｜线性读数 开 {linOn:F5} / 关 {linOff:F5}"
                    + $"｜⛔ 旧估计量 ratio-of-means = {thruRom:F3}（**已废**，只留对照）"
                    + $"｜参与平均 {nRat} px · 分母 0 跳过 {nRatSkip} px"
                    + $"｜同沙盒关掉 {others.Count} 个 renderer（含 `vSmall` / `vTac` —— `W64` 修的就是这里）"
                    + "｜🔴 2026-10-14 前的旧读法（全卡净掉幅 ≥25%）**已废** —— 它的期望值来路错了两处，见上面那段订正痕");

                // ---- ④·量级：**用原版字段推出的带**（2026-10-22 收回，⛔ 不是我们自己的常量）----
                //  期望值来路：`1 − 0.647 × 0.830 = 0.463`（`m_Color.a` 与 Sliced 贴图均值，出处见上）。
                //  实测同一帧：`mean-of-ratios` = 0.461 ⇒ 在带内；`Σon/Σoff` = 0.380 ⇒ 在带外（**假的**）。
                Check(thru >= ThruSliced - Tol && thru <= ThruSliced + Tol,
                      $"④·量级 ★ 底板透过率落在**原版字段推出的带**里：{thru:F3} ∈ {band}"
                    + "｜估计量 = `mean-of-ratios`（逐像素线性亮度比值再平均）"
                    + "｜判据 → `资料/普查产出_1021/W61_卡牌线_T07尾巴.md` §3·2："
                    + "同一帧 `Σon/Σoff` 读 0.380、逐像素比值读 0.461、原版字段 0.463 ⇒ **底板画得对**，"
                    + "当初那个 0.378 是估计量的假象"
                    + "｜🧨 ① 把估计量改回 `Σon/Σoff` ⇒ 读数掉回 0.38、本条红；"
                    + "② 把 `m_Color.a` 从 0.647 挪走（画面**真的**变浅/变深）⇒ 本条红");

                // ---- ④·核心：**这一帧是不是合格的一次「只留这一层」测量**（2026-10-22 `W64` 加）----
                //  **为什么要它**：`W64` 那次 0.610 不是画面错、也不是估计量错，而是**帧里有别的东西盖住了
                //  这一层的一部分**（`vSmall` / `vTac` 未摆位、画在 `vBig` 前面）—— 被盖住的像素读 1.0，
                //  把整块矩形的均值往上拖。上面已经在**取景**上修了；这一条是**判据侧**的哨兵：
                //  核心段（下 55%）是 Sliced 的拉伸段、贴图 alpha ≈ 1 ⇒ 透过率必须 ≈ `1 − 0.647 × 1`
                //  = **0.353**。有任何遮挡 ⇒ 这个数被抬高 ⇒ 本条红。
                //  🧨 把 ④ 的 renderer 收集范围改回 `vBig.transform`（漏掉沙盒里另两张卡）⇒ 本条必红。
                Check(!float.IsNaN(thruCore) && thruCore >= ThruCore - Tol && thruCore <= ThruCore + Tol,
                      $"④·核心 ★ 底板**下 55%**（Sliced 核心段、贴图 alpha ≈ 1）透过率 = {thruCore:F3}，"
                    + $"应落在 [{ThruCore - Tol:F3}, {ThruCore + Tol:F3}]（= `1 − 0.647 × 1.0`）"
                    + "｜⚠️ **本条查的是帧合不合格，不是画面**：这一层被别的东西盖住一部分 ⇒ 那些像素读 1.0、"
                    + "把核心段抬高 ⇒ 红。判据 → `资料/普查产出_1021/W61_卡牌线_T07尾巴.md` §3·2"
                    + "（核心段 alpha 的出处 = Sliced 映射：上缘 9px/39.5% 是羽化带、**下缘 border = 0**）"
                    + "｜🧨 把 ④ 的 renderer 收集范围从 `root` 改回 `vBig.transform` ⇒ 本条红（`W64` 的 0.610 就是这么来的）");

                // ==================================================================
                // ---- ⑥ 灭自证（`A1513`）：证明 ④ 那条检测器**真的会红** ----
                //  **为什么要它**：检测器自己也会坏（判据写错 / 窗口量错 / 读数路径变了）——
                //  ④ 今天绿，可能只是「它根本量不出差别」。这里用一个**必然更差**的现场反证：
                //  把底板 α 调 0（层还在、一个像素都不画），用**同一条算式**再量一次，
                //  透过率必须回到 ≈1（> 0.95）⇒ ④ 会判红。改完**当场还原**。
                //  🔴 2026-10-22（`A1934`）：算式换成 `mean-of-ratios` 之后**仍走同一条** —— α=0 那帧
                //  与「底板关」那帧**逐像素相同** ⇒ 逐像素比值的平均必然 = 1.000（比原来的均值比更硬：
                //  它连「两帧真的逐像素一致」都一起验了）。
                // ==================================================================
                if (matBig != null)
                {
                    // ⚠️ 与 ④ 同一条口径：关**整个沙盒**（`root`）的 renderer —— 见 ④ 上面那段
                    //    「`vSmall` / `vTac` 未摆位、画在 `vBig` 前面」的注释（`W64` 同步点修正）。
                    var others2 = new List<MeshRenderer>();
                    foreach (var rr in root.transform.GetComponentsInChildren<MeshRenderer>(true))
                        if (rr != rBig && rr.enabled) { rr.enabled = false; others2.Add(rr); }
                    var inkBack = matBig.color;
                    matBig.color = new Color(0f, 0f, 0f, 0f);
                    var inkPx = RenderPixels(cam, PW, PH, pxIn);
                    matBig.color = inkBack;
                    foreach (var rr in others2) if (rr != null) rr.enabled = true;
                    float thru0 = MeanThruLinear(inkPx, pxOff, out _, out _);
                    Check(thru0 > 0.95f,
                          $"⑥ 灭自证①：把底板 **α 调 0** ⇒ ④ 那条检测器**必须判红**（实测透过率 {thru0:F3} > 0.95）"
                        + "｜估计量 = `mean-of-ratios`（与 ④ 同一条算式；α=0 帧应与「底板关」帧逐像素相同 ⇒ 精确 1.000）"
                        + "｜这条自己不绿 = ④ 的算式/窗口坏了 ⇒ **④ 的绿也不能信**");

                    // ⑥·b 层序的**机制**：同一张卡的所有层共用**同一个** renderQueue（卡内层序靠 z / 到相机距离）。
                    //  判据：`CardView` 的「一张卡一段队列」（`SetQueue` / `StampQueue` 是唯一实现）。
                    var rCost = LayerMesh(vBig.transform, "costBg");
                    int qBg = matBig.renderQueue;
                    int qCost = (rCost != null && rCost.sharedMaterial != null) ? rCost.sharedMaterial.renderQueue : -1;
                    Check(rCost != null && qBg == vBig.Queue && qCost == qBg,
                          $"⑥·b 同一张卡的层**共用一个 renderQueue**（textBg {qBg} · costBg {qCost} · 卡 {vBig.Queue}）"
                        + "｜🧨 某层漏了 `StampQueue`（队列与别人不同）⇒ 这一层的层序就散了 ⇒ 本条红");
                    if (rCost != null)
                    {
                        matBig.renderQueue = qBg - 1;                       // 故意制造「层序散了」
                        bool broke = matBig.renderQueue != qCost;
                        matBig.renderQueue = qBg;                           // 当场还原
                        Check(broke, "⑥·b 灭自证②：把底板队列**调低 1** ⇒ 层序检测器**必须**判出不一致"
                                   + "｜不红 = 这条检测器没牙（层序错了它也看不见）");
                    }
                }
            }

            // ---- ⑤ 同类风险：别让那几块**静态缓存网格**被收成 null ----
            var dArm = CardData.Placeholder(0);
            dArm.armor = 2;                            // 有护甲才会建盾那一层
            var vArm = CardView.Create(root.transform, dArm, "textbg_armour");
            var names = new[] { "textBg", "costBg", "armourIcon", "gem" };
            int built = 0; string missing = "", broken = "";
            foreach (var nm in names)
            {
                var rr = LayerMesh(vArm.transform, nm);
                if (rr == null) { missing += " " + nm; continue; }
                built++;
                var mf = rr.GetComponent<MeshFilter>();
                var m = mf != null ? mf.sharedMesh : null;
                // ⚠️ 顶点数**按层**：`textBg` 是原版 Sliced 的 6 块（`A1933`）⇒ **24**；
                //    其余三层仍是 `SpriteQuad` / `GemMesh` 的单块 ⇒ **4**。⛔ 别一刀切。
                int want = nm == "textBg" ? 24 : 4;
                if (m == null) broken += " " + nm + "=**null**";
                else if (m.vertexCount != want) broken += " " + nm + "=" + m.vertexCount + " 顶点（应为 " + want + "）";
            }
            Check(built == names.Length,
                  $"⑤·★ 前提：`SpriteQuad` / `GemMesh` 那四层**都建出来了**（缺{missing} —— 缺 = 那几张图"
                + "取不到，是资源问题不是网格问题，这条**如实红**）");
            Check(broken.Length == 0,
                  $"⑤ 建出来的那几层 `sharedMesh` 都还在、顶点数也对（`textBg` = 原版 Sliced 的 24、"
                + $"其余三层 = 4）（坏：{broken}）"
                + "｜🧨 `UnloadUnusedAssets` 把静态缓存网格收走 ⇒ `sharedMesh` 变 null、那几层静默不画");

            // ==================================================================
            // ---- ⑦ 估计量防回归（`A1934` 收口 · 2026-10-22）----
            //  **为什么要它**：④ 读数的对错**全押在估计量上** —— 同一帧、同一矩形、同一份 `Color32[]`，
            //  `ratio-of-means`（`Σon/Σoff`）读 0.380、`mean-of-ratios` 读 0.461，而原版字段期望 0.463
            //  （判据 → `资料/普查产出_1021/W61_卡牌线_T07尾巴.md` §3·2 那张表）。
            //  ④ 今天绿，也可能只是「谁把估计量改回了 `Σon/Σoff`、带宽恰好又放回去了」——
            //  ⇒ 这里用**解析值已知**的合成样本，把**估计量本身**钉死（与渲染无关，一定跑得到）。
            //  ⚠️ 样本**故意做成背景不均匀**（off 100 与 10 差 10 倍）—— 那正是两种估计量分道的现场。
            //  🧨 把 ④ 改回 `Σon/Σoff`（或把 `MeanOfRatios` / `MeanThruLinear` 之一改回「先聚合再相除」）⇒ 本条必红。
            // ==================================================================
            {
                var synOn = new double[] { 50.0, 9.0 };
                var synOff = new double[] { 100.0, 10.0 };
                float mor = MeanOfRatios(synOn, synOff, out _, out _);
                float rom = RatioOfMeans(synOn, synOff);
                Check(Mathf.Abs(mor - 0.7f) < 1e-5f && Mathf.Abs(rom - 59f / 110f) < 1e-5f
                      && Mathf.Abs(mor - rom) > 0.1f,
                      $"⑦ 估计量 = **`mean-of-ratios`**（逐像素 `on_i / off_i` 再平均），⛔ 不是 `Σon/Σoff`"
                    + $"｜合成样本 off={{100, 10}} on={{50, 9}}：`mean-of-ratios` = {mor:F4}（解析值 **0.7000**）· "
                    + $"`ratio-of-means` = {rom:F4}（解析值 **0.5364**）⇒ 两者**必须**差得开"
                    + "｜🧨 谁把 ④ 的估计量改回 `Σon/Σoff` ⇒ 本条红"
                    + "（同一帧实测：0.461 vs 0.380，见 W61 §3·2）");

                // 分母 0（背景纯黑）那一格：按**跳过**处理 —— ⛔ 不记 0（记 0 会假装「全黑」把均值拉低），
                // 跳过的格数要如实报出来（④ 的日志里会打 `nRatSkip`）。口径与 `MeanOfRatios` 逐句相同。
                var zOn = new double[] { 50.0, 9.0, 3.0 };
                var zOff = new double[] { 100.0, 10.0, 0.0 };
                int zUsed, zSkip;
                float morZ = MeanOfRatios(zOn, zOff, out zUsed, out zSkip);
                Check(zUsed == 2 && zSkip == 1 && Mathf.Abs(morZ - 0.7f) < 1e-5f,
                      $"⑦·b `off_i == 0`（背景纯黑、无法求比值）的像素**跳过**、不参与平均"
                    + $"（实测 参与 {zUsed} px / 跳过 {zSkip} px，值 {morZ:F4} = 前两格 (0.5 + 0.9) / 2）"
                    + "｜🧨 把分母 0 的格子**记成 0** 塞进平均 ⇒ 变 0.4667、本条红");
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ==================================================================
    //  🔴 2026-10-18（`A1492`）：**稀有度宝石**（卡底那颗菱形）—— 结构断言 + 渲图灭自证
    //
    //  **为什么要有这一块**：宝石落点 2026-10-11 已按「卡框贴图上那个黑菱形凹槽」重算过
    //  （`CardView.FaceGemAt01` / `RarityGemX/Y`），但**一条断言都没有** —— 谁把
    //  `FrameGemPx/Py` / `GemMesh` 改回去，全套自检照样全绿。
    //
    //  **判据锚在【卡框贴图实测】上**（⛔ 不是我们自己的 `RarityGem*` 常量）：
    //    · 凹槽 bbox = 卡框贴图 `x 478..544 / y 919..981`（贴图像素、y 从顶部数）
    //      —— 2026-10-11 对 `frame_saimhann_tier3.png` 的黑菱形连通域实测；15 个阵营的卡框
    //      画在同一张 1024² 画布上、凹槽像素位置逐张一致 ⇒ 中心 **(511, 950)**。
    //    · ① **结构**：`gem` 层网格 bounds 的中心 = 凹槽中心（走**卡框层自己的网格 bounds +
    //      `FrameUvRect`** 换算，⛔ 不碰任何私有常量），且中心**落在凹槽矩形内**。
    //      容差 0.004 卡单位（≈1.3 贴图像素）。旧实现 y 偏 0.0491 / x 偏 0.0147 ⇒ **必红**。
    //    · ② **渲图（灭自证）**：只留 `frame` 渲一帧、再开 `gem` 渲一帧，两帧作差 = **宝石亮核**
    //      （权重 = 逐像素亮度增量）；其质心必须落在**凹槽中心**在屏幕上的位置 **≤ 3 px** 内，
    //      且「宝石确实比背后的卡框亮」（平均增量 ≥ 12）。
    //      ⚠️ **为什么不用「渲出来的暗区质心」当凹槽锚点**：实测卡框在凹槽**上下**同样暗
    //      （贴图行剖面：凹槽外 lum≈13..23、凹槽内≈0..25）⇒ 暗区质心被拉偏 ≈8 px，那条件不成立。
    //      **凹槽位置只能从贴图像素来** —— 这也是本条比「读 `RarityGemY`」有牙的地方
    //      （改实现不改判据 ⇒ 红；改判据不改实现 ⇒ 红）。
    //
    //  ⚠️ **0.042 的成因仍然没查清**（原版实拍里宝石比凹槽中心再靠右 ~0.042 卡单位；prefab 两个
    //     节点的 x 都是 0 ⇒ 推不出来，疑为原版 frame sprite 的 rect/pivot 与我们的 `FrameUv` 不同）
    //     ⇒ **本笔不改几何、也不拿断言去逼近它**（判据不足；见 `T04_卡面几何与图标.md:34`）。
    //  ⚠️ **「宝石整块完全落在凹槽内」也不断言**：`RarityGemW/H` 是**固定卡单位**（按
    //     `frame_saimhann_tier3` 那一张量出来的），而凹槽经各卡框 uv 换算后宽 **0.2149..0.2532**
    //     （150 张实测）⇒ 125/150 张卡框的凹槽比这块宝石窄。尺寸该不该按 uv 缩放
    //     **判据不足**（PnP 上量到的真宝石反而比凹槽小 ~20%）⇒ 只**记录**、不立红线。
    // ==================================================================
    static void AssertRarityGem(Camera cam)
    {
        var pool = CardDatabase.Load();
        CardDef def = null;
        for (int i = 0; i < pool.Count; i++) if (pool[i].Name == "Heavy Intercessor") { def = pool[i]; break; }
        Check(def != null, "★ 前提：卡池里有尺子卡 `Heavy Intercessor`（换名字要同时改这一行）");
        if (def == null) return;

        var data = BattleDriver.ToCardData(def, def.Faction);
        var root = new GameObject("gem_probe");
        try
        {
            var v = CardView.Create(root.transform, data, "gem_probe");
            var rFrame = LayerMesh(v.transform, "frame");
            var rGem = LayerMesh(v.transform, "gem");
            Check(rFrame != null && rGem != null,
                  "★ 前提：这张卡建出了 `frame` 与 `gem` 两层（缺一层 = 资源缺失 ⇒ 下面**没验到**，别读成通过）");
            if (rFrame == null || rGem == null) return;

            // 凹槽（**贴图像素**，y 从顶部数）—— 判据见上面那段
            const float Gx0 = 478f, Gx1 = 544f, Gy0 = 919f, Gy1 = 981f, Gcx = 511f, Gcy = 950f;

            // 贴图像素 → **卡框层局部坐标**。只走公开量：`FrameTexture` / `FrameUvRect` / 网格 bounds。
            // ⚠️ 层没有旋转与缩放（`AddLayer` 只给 z）⇒ 层局部系与卡根局部系同向、同尺度。
            Vector2 FramePxToLocal(float px, float py)
            {
                var tex = v.FrameTexture;
                var mf = rFrame.GetComponent<MeshFilter>();
                var uv = v.FrameUvRect;
                if (tex == null || mf == null || mf.sharedMesh == null
                    || uv.width <= 1e-5f || uv.height <= 1e-5f) return new Vector2(float.NaN, float.NaN);
                var b = mf.sharedMesh.bounds;
                float u = (px + 0.5f) / tex.width;
                float vv = 1f - (py + 0.5f) / tex.height;
                float fu = (u - uv.xMin) / uv.width;
                float fv = (vv - uv.yMin) / uv.height;
                return new Vector2(b.min.x + fu * b.size.x, b.min.y + fv * b.size.y);
            }

            var g00 = FramePxToLocal(Gx0, Gy0);
            var g11 = FramePxToLocal(Gx1, Gy1);
            var gc = FramePxToLocal(Gcx, Gcy);
            bool mapped = !float.IsNaN(gc.x) && !float.IsNaN(g00.x) && !float.IsNaN(g11.x);
            Check(mapped, "★ 前提：凹槽像素能换算到卡根坐标（卡框贴图 / `FrameUvRect` / 网格都在）");
            if (!mapped) return;

            var groove = Rect.MinMaxRect(Mathf.Min(g00.x, g11.x), Mathf.Min(g00.y, g11.y),
                                         Mathf.Max(g00.x, g11.x), Mathf.Max(g00.y, g11.y));
            // 卡根局部 → 卡单位（今天两者相等；写成除法是为将来改卡尺寸时不静默错）
            float kx = CardView.CardUnitW / CardView.Width, ky = CardView.CardUnitH / CardView.Height;
            var grooveCu = Rect.MinMaxRect(groove.xMin * kx, groove.yMin * ky,
                                           groove.xMax * kx, groove.yMax * ky);
            var gcCu = new Vector2(gc.x * kx, gc.y * ky);

            // ---- ① 结构：宝石中心 == 凹槽中心 ----
            var mfGem = rGem.GetComponent<MeshFilter>();
            var bGem = (mfGem != null && mfGem.sharedMesh != null) ? mfGem.sharedMesh.bounds : default(Bounds);
            var gemCu = new Vector2(bGem.center.x * kx, bGem.center.y * ky);
            float dCu = Vector2.Distance(gemCu, gcCu);
            Check(dCu < 0.004f,
                  $"① 宝石中心 = **卡框贴图上那个凹槽的中心** (511,950)（实测差 {dCu:F4} 卡单位）"
                + $"｜判据 = 贴图实测 bbox（478..544 / 919..981），⛔ 不是 `RarityGem*` 常量"
                + "｜🧨 改回旧固定值（y −1.4635 / x=0）⇒ 差 0.049 / 0.015 ⇒ 本条红");
            Check(grooveCu.Contains(gemCu),
                  $"①·b 宝石中心**落在凹槽矩形内**（凹槽 {grooveCu.xMin:F4}..{grooveCu.xMax:F4} × "
                + $"{grooveCu.yMin:F4}..{grooveCu.yMax:F4}，宝石中心 {gemCu.x:F4}, {gemCu.y:F4}）"
                + "｜🧨 宝石被挪出凹槽 ⇒ 红");
            // 只记录、不断言：整块 bounds 与凹槽的大小关系（尺寸判据不足，见方法头那段）
            Debug.Log(P + $"  [A1492] 宝石 bounds {bGem.size.x * kx:F4}×{bGem.size.y * ky:F4} 卡单位 · "
                        + $"凹槽 {grooveCu.width:F4}×{grooveCu.height:F4} ⇒ 每边溢出 "
                        + $"x {(bGem.size.x * kx - grooveCu.width) * 0.5f:F4} / "
                        + $"y {(bGem.size.y * ky - grooveCu.height) * 0.5f:F4} 卡单位（**记录，不断言**）");

            // ---- ② 渲图：宝石亮核质心 vs 凹槽中心的屏幕位置 ----
            const int PW = 1920, PH = 1080;
            v.SetPose(new Vector3(0f, 1.6f, 0f), 0f, 1f);      // 上半屏空地（与 AssertTextBg 同一处）
            cam.aspect = (float)PW / PH;                        // ⚠️ `WorldToViewportPoint` 也吃它，先设死
            var gemPx = LayerPxRect(cam, rGem, PW, PH);
            bool okPx = gemPx.width > 8f && gemPx.height > 8f
                     && gemPx.xMin > 4f && gemPx.yMin > 4f && gemPx.xMax < PW - 4f && gemPx.yMax < PH - 4f;
            Check(okPx, $"★ 前提：宝石那块**整块落在 {PW}×{PH} 画面内**"
                      + $"（像素矩形 x {gemPx.x:F0} y {gemPx.y:F0} w {gemPx.width:F0} h {gemPx.height:F0}）");
            if (okPx)
            {
                // 只留 `frame`（和 `gem`）；其余层全关 —— 两帧作差会把这些静态东西约掉，但关掉更干净
                var hidden = new List<MeshRenderer>();
                foreach (var rr in v.transform.GetComponentsInChildren<MeshRenderer>(true))
                    if (rr != rFrame && rr != rGem && rr.enabled) { rr.enabled = false; hidden.Add(rr); }

                var win = new Rect(gemPx.x - gemPx.width * 0.25f, gemPx.y - gemPx.height * 0.25f,
                                   gemPx.width * 1.5f, gemPx.height * 1.5f);
                // `RenderPixels` 会把这几个值再钳一次 —— 这里照它的算式先算，下面换算质心才与读数同坐标系
                int rw = Mathf.Max(1, Mathf.RoundToInt(win.width));
                int rh = Mathf.Max(1, Mathf.RoundToInt(win.height));
                int rx = Mathf.Clamp(Mathf.RoundToInt(win.x), 0, Mathf.Max(0, PW - rw));
                int ry = Mathf.Clamp(Mathf.RoundToInt(win.y), 0, Mathf.Max(0, PH - rh));

                rGem.enabled = false;
                var frameOnly = RenderPixels(cam, PW, PH, win);
                rGem.enabled = true;
                var withGem = RenderPixels(cam, PW, PH, win);
                foreach (var rr in hidden) if (rr != null) rr.enabled = true;

                double sw = 0, sx = 0, sy = 0, sdiff = 0;
                int n = 0, len = Mathf.Min(frameOnly.Length, withGem.Length);
                for (int i = 0; i < len; i++)
                {
                    float d = Lum(withGem[i]) - Lum(frameOnly[i]);
                    if (d <= 8f) continue;                       // 8/255 ≈ 噪声底（宝石增量实测 >100）
                    int x = i % rw, y = i / rw;                  // `GetPixels32` 的 row 0 在**下边**
                    sw += d; sx += x * d; sy += y * d; sdiff += d; n++;
                }

                // 凹槽中心 → 世界 → 屏幕 → 同一块读回窗口的局部坐标
                Vector3 world = rFrame.transform.TransformPoint(new Vector3(gc.x, gc.y, 0f));
                Vector3 vp = cam.WorldToViewportPoint(world);
                var expected = new Vector2(vp.x * PW - rx, vp.y * PH - ry);

                bool hasPix = sw > 0 && n >= 40;
                Check(hasPix, $"② ★ 前提：宝石**真的画出了像素**（亮核 {n} 像素 / 权重和 {sw:F0}）"
                            + "｜一个像素都没有 = 下面那条**没验到**，不是通过");
                if (hasPix)
                {
                    var core = new Vector2((float)(sx / sw), (float)(sy / sw));
                    float dist = Vector2.Distance(core, expected);
                    float meanDiff = (float)(sdiff / n);
                    Check(dist < 3f,
                          $"② **灭自证**：渲染结果里「宝石亮核质心」= 凹槽中心（屏幕差 {dist:F2} px ≤ 3）"
                        + $"｜亮核 {core.x:F1},{core.y:F1} · 凹槽期望 {expected.x:F1},{expected.y:F1}"
                        + "｜🧨 把 `GemMesh` 的 x 改回 0 / y 改回 −1.4635 ⇒ 质心偏 5 / 16 px ⇒ 本条红"
                        + "（期望点来自**贴图像素 511,950**，⛔ 不来自任何 `RarityGem*` 常量）");
                    Check(meanDiff > 12f,
                          $"②·b **宝石确实比它背后的卡框亮**（亮核内平均亮度增量 {meanDiff:F1} ≥ 12）"
                        + "｜🧨 宝石没画 / 画成暗色 / 被卡框盖住 ⇒ 本条红（质心那条也会跟着红）");
                }
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>一个像素的 Rec.709 亮度（0..255）—— 与 `MeanLum` 同一套权重，单像素版。</summary>
    static float Lum(Color32 c) { return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b; }

    // ==================================================================
    //  🆕 2026-10-18（`A1549` 尾巴）：**运行时切档 ⇒ 已建好的卡跟上**
    // ==================================================================
    //  原版的行为 = `CardTextCountersController.Toggle(...)` 每次按当时的
    //  `GameStaticData.smallScreenUI` **重新选容器**（`CardTextCountersController__Toggle.c`）。
    //  我们的费用格把档位**烘进了网格**（尺寸/落点），所以切档之后必须有一条刷新链：
    //    ① `CardView.RefreshCostTier()` —— 单卡重做；
    //    ② `SetPose` / `SetData` 开头自动比一次档位（= 切完设置后的下一次布局就生效）；
    //    ③ `CardView.RefreshAllCostTiers()` —— 给设置窗那颗开关一次性刷全场（接线在
    //       `Shell/**`，不在本写手白名单 ⇒ 见报告 §三）。
    //  ⚠️ 本条**动全局开关** ⇒ 与 `BattleScene.Run` 同一套自检纪律：`PersistOverride = true`
    //     （不写 `PlayerPrefs`）+ `finally` 里还原内存态。
    // ==================================================================
    static void AssertCostTierRefresh()
    {
        Debug.Log(P + "--- 费用格两档：运行时切档后的刷新链（A1549 尾巴）---");
        bool wasOn = SmallScreenUI.Enabled, wasChosen = SmallScreenUI.ChosenManually;
        bool wasPersist = SmallScreenUI.PersistOverride;
        SmallScreenUI.PersistOverride = true;
        var root = new GameObject("cost_tier_refresh");
        try
        {
            SmallScreenUI.Set(false);
            var v = CardView.Create(root.transform, CardData.Placeholder(0), "cost_tier");
            Check(v.CostTierBuilt == 0, "① 前提：普通档建出来的卡 `CostTierBuilt == 0`");
            var mr = LayerMesh(v.transform, "costBg");
            Check(mr != null, "① 前提：这张卡建出了 `costBg` 层（督军 / 底图取不到时没有 ⇒ 下面验不了）");
            if (mr == null) return;
            var mf0 = mr.GetComponent<MeshFilter>();
            float w0 = mf0 != null && mf0.sharedMesh != null ? mf0.sharedMesh.bounds.size.x : 0f;

            SmallScreenUI.Set(true);
            bool did = v.RefreshCostTier();
            Check(did, "② 切到大号档后 `RefreshCostTier()` **真的重做了**（返回 true）"
                     + "｜🧨 只改标记不换网格 ⇒ 本条红");
            Check(v.CostTierBuilt == 1, "② `CostTierBuilt` 跟到 1");
            var mr2 = LayerMesh(v.transform, "costBg");
            var mf2 = mr2 != null ? mr2.GetComponent<MeshFilter>() : null;
            float w1 = mf2 != null && mf2.sharedMesh != null ? mf2.sharedMesh.bounds.size.x : 0f;
            Check(w0 > 1e-4f && Mathf.Abs(w1 / w0 - CardView.CostBigScale) < 0.01f,
                  $"③ 网格**真的**按原版两档 ×{CardView.CostBigScale} 放大（{w0:F4} → {w1:F4}，比 {w1 / w0:F3}）"
                + "｜判据 = 原版 `Cost Container` ↔ `Cost Container Big` 逐项 ×1.2（见 `CardView.CostBigScale` 注释）");
            Check(!v.RefreshCostTier(), "④ 档位没变时再调一次 = 空操作（返回 false，不白重建）");

            // ⑤ **自动那条链**：切回去之后**不显式调** `RefreshCostTier`，只 `SetPose` 一次
            //    （= 真机上「设置切完 → 下一次布局」那条路）。
            SmallScreenUI.Set(false);
            v.SetPose(Vector3.zero, 0f, 1f);
            var mr3 = LayerMesh(v.transform, "costBg");
            var mf3 = mr3 != null ? mr3.GetComponent<MeshFilter>() : null;
            float w2 = mf3 != null && mf3.sharedMesh != null ? mf3.sharedMesh.bounds.size.x : 0f;
            Check(v.CostTierBuilt == 0 && Mathf.Abs(w2 / w0 - 1f) < 0.01f,
                  $"⑤ `SetPose` 开头的自动同步把档位刷回普通档（`CostTierBuilt` = {v.CostTierBuilt}，宽 {w2:F4}）"
                + "｜🧨 删掉 `SetPose` 里那句 `SyncCostTier()` ⇒ 本条红（真机上表现为「切完设置卡不变」）");
        }
        finally
        {
            Object.DestroyImmediate(root);
            // ⚠️ **还原内存态必须还在 `PersistOverride = true` 期间做** —— 否则 `Set` 会写
            //    `PlayerPrefs`（自检不许动玩家的真设置）。先把 `PersistOverride` 放回去就会踩这个。
            if (wasChosen) SmallScreenUI.Set(wasOn); else SmallScreenUI.ResetForTest();
            SmallScreenUI.PersistOverride = wasPersist;
        }
    }

    /// <summary>
    /// 🆕 2026-10-22（W64）：**动画 / 材质里那批「近似」换成原版真值**的断言。
    /// 逐条判据（全部一手，路径可复查）→ `资料/普查产出_1022/W64_兜底审计_动画.md` 的 B02–B08。
    ///
    /// <para>覆盖：B08 溶解材质 11 项 + `_DissolveTex = Noise Combined` · B04 登场 clip 缺的四条曲线 ·
    /// B05 材质复位 0.55 · B06 曲线 = smoothstep（不是 InOutSine）· B02 近战挨打**无旋转** ·
    /// B07 落位缩放**瞬时赋值**。</para>
    /// <para>🧨 每一条都写了改坏法 —— 改回近似就红。</para>
    /// <para>⚠️ **没覆盖**：B03（技能时长表在 `Core/AbilityAnimDurations.cs`）· B09/B10
    /// （`Core/UnitTweenTable.cs`）· B17（`WarpforgeEffectPlayer` / `BattleDriver`）——
    /// 这三个文件不在本批的文件所有权里，判据已备好，交给下一批。</para>
    /// </summary>
    static void AssertCardAnimOriginal()
    {
        Debug.Log(P + "--- 卡面动画/材质：近似 → 原版真值（W64 B02/B04/B05/B06/B07/B08）---");

        // ---------- B06：clip 的插值器 = smoothstep（Hermite 两端切线 0）----------
        // 判据：`AnimationClip_-3614292623624764332.json` 每条的 inSlope/outSlope 全 0 ⇒ 3t²−2t³。
        float c25 = CardFeel.ClipSmoothStep.Evaluate(0.25f);
        float c50 = CardFeel.ClipSmoothStep.Evaluate(0.5f);
        Check(Mathf.Abs(c25 - 0.15625f) < 1e-3f && Mathf.Abs(c50 - 0.5f) < 1e-3f,
              $"★ `CardFeel.ClipSmoothStep` 在 t=0.25 给 **{c25:F5}**、t=0.5 给 {c50:F5}"
            + "（精确 smoothstep 3t²−2t³ = 0.15625 / 0.5）"
            + "｜🧨 换回 `Ease.InOutSine`（t=0.25 ⇒ 0.14645）⇒ 本条红");

        // ---------- B04：登场 clip 的四条曲线常量（全部照关键帧原值）----------
        Check(Mathf.Abs(CardFeel.HandToBoardFrameOn - 0.0333f) < 1e-4f
              && Mathf.Abs(CardFeel.HandToBoardFrameOff - 0.75f) < 1e-4f,
              $"★ `CardFrame.m_Enabled`：1 @{CardFeel.HandToBoardFrameOn:F4} → 0 @{CardFeel.HandToBoardFrameOff:F2}"
            + "｜🧨 把 0.75 写成 0.7 或干脆不关 ⇒ 红");
        Check(Mathf.Abs(CardFeel.HandToBoardArtY + 0.065f) < 1e-4f
              && Mathf.Abs(CardFeel.HandToBoardArtShrinkStart - 0.3333f) < 1e-4f
              && Mathf.Abs(CardFeel.HandToBoardShrink - 0.9049578f) < 1e-4f,
              $"★ `CardImage`：y = **{CardFeel.HandToBoardArtY:F3}**（常量）、scale 1 → "
            + $"{CardFeel.HandToBoardShrink:F4} @{CardFeel.HandToBoardArtShrinkStart:F4}"
            + "｜🧨 把 −0.065 或 0.905 改回 0 / 1 ⇒ 红");
        Check(Mathf.Abs(CardFeel.HandToBoardFaceZ + 0.043f) < 1e-4f,
              $"★ `2DCard.m_LocalPosition.z` 终点 = **{CardFeel.HandToBoardFaceZ:F3}** @0.1667"
            + "｜🧨 改回 0 ⇒ 红");

        // ---------- B08：溶解材质的 11 项序列化值（原版 `Card 3d WH40K Dissolve`）----------
        // 判据：`bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-6651017123104198842.json`
        //   ⇒ 这里直接读**建出来的那张材质**（不是抄常量），才验得到「真的写进去了」。
        var disRoot = new GameObject("chk_dissolve_mat");
        var disCard = CardView.Create(disRoot.transform, CardData.Placeholder(1),
                                      disRoot.name, CardFace.Board);   // 场上形态才建 3D 体
        var dm = disCard != null ? disCard.DissolveMatView : null;
        Check(dm != null,
              "溶解材质建得出来（取不到原版 shader ⇒ 下面这一组**没验到**，不是通过）");
        if (dm != null)
        {
            // 🔴 **2026-10-22（`CardBaseDemo` 红 → 现核）**：只断 **shader 属性表里真有的**那几项。
            //   `_BorderWidth2` / `_Borderwidth` / `_SmoothStep` **不在**属性表里
            //   （`Shader_-8335403570401031012.json` 的 `m_Props` 23 个名字逐项读过）⇒ 它们是
            //   **材质 JSON 里 Standard shader 的残留死值**，`SetFloat` 落不进去（`HasProperty` 为假）。
            //   原来那条「10 项逐项抄」的断言把它们当活参数 ⇒ 必然红。⛔ 别改原版值、也别去猜别名。
            bool ok =
                   Mathf.Abs(dm.GetFloat("_BorderWidth") - 0.23f) < 1e-3f
                && Mathf.Abs(dm.GetFloat("_CountersIntensity") - 2.0f) < 1e-3f
                && Mathf.Abs(dm.GetFloat("_AlphaClipThreshold") - 0.145f) < 1e-3f
                && Mathf.Abs(dm.GetFloat("_DISSOLVE_CHANNEL") - 2.0f) < 1e-3f;
            Check(ok,
                  "★ 溶解材质的 **4 个活标量** = 原版：`_BorderWidth 0.23` · "
                + "`_CountersIntensity 2.0`（**不是正常体的 1.12**）· "
                + "`_AlphaClipThreshold 0.145` · `_DISSOLVE_CHANNEL 2`"
                + $"｜现读 {dm.GetFloat("_BorderWidth"):F3} / {dm.GetFloat("_CountersIntensity"):F3} / "
                + $"{dm.GetFloat("_AlphaClipThreshold"):F3} / {dm.GetFloat("_DISSOLVE_CHANNEL"):F1}"
                + "｜🧨 把 `_CountersIntensity` 抄回正常体（1.12）⇒ 红");
            Check(!dm.HasProperty("_BorderWidth2") && !dm.HasProperty("_Borderwidth")
                  && !dm.HasProperty("_SmoothStep"),
                  "★ `_BorderWidth2` / `_Borderwidth` / `_SmoothStep` **不是这个 shader 的属性**"
                + "（材质 JSON 里有、shader 属性表里没有 = Standard 残留死值，CLAUDE.md §三）"
                + "｜🧨 有人给 shader 加了同名属性（或把断言改回去要求它们有值）⇒ 本条红");
            Vector4 bc = dm.GetVector("_BorderColor"), bc2 = dm.GetVector("_BorderColor2");
            Vector4 cr = dm.GetVector("_ClampRange"), uv = dm.GetVector("_DissolveUVScale_XY_Offset_ZW");
            // ⚠️ 容差 1e-3：材质里存的是 float32（0.71408665 → 读回 0.7140866），不是打印精度问题。
            bool okv = Vector4.Distance(bc, CardView.DissolveBorderColor) < 1e-3f
                    && Vector4.Distance(bc2, CardView.DissolveBorderColor2) < 1e-3f
                    && Vector4.Distance(cr, CardView.DissolveClampRange) < 1e-3f
                    && Vector4.Distance(uv, CardView.DissolveUVScale) < 1e-3f;
            Check(okv,
                  "★ 溶解材质 **4 个活四分量** = 原版：`_BorderColor (0.714,1.915,2.639,0)` · "
                + "`_BorderColor2 (0,0.0286,0.576,0)` · `_ClampRange (0.82,1,0.18,0.01)` · "
                + "`_DissolveUVScale_XY_Offset_ZW (3,3,0,0)`"
                + $"｜现读 {bc} / {bc2} / {cr} / {uv}｜🧨 少写任一项（落回 shader 默认）⇒ 红");
            var dtex = dm.GetTexture("_DissolveTex");
            var want = CardArt.CardbackSdfNoise();
            Check(want != null && ReferenceEquals(dtex, want),
                  "★ `_DissolveTex` = 原版那张 **`Noise Combined`**（pathID −8214795764591836323 = "
                + "`CardArt.CardbackSdfNoise()`）"
                + $"｜现读 {(dtex == null ? "<null>" : dtex.name)}"
                + "｜🧨 换回 `Art/card3d/DissolveNoise`（那个文件根本不存在）⇒ 红");
            Check(dm.IsKeywordEnabled(CardView.DissolveKeywordB),
                  $"★ 关键字 `{CardView.DissolveKeywordB}` 已开（原版 `m_ValidKeywords`）"
                + "｜🧨 删掉 `EnableKeyword` ⇒ 红");
        }
        Object.DestroyImmediate(disRoot);

        // ---------- B02：近战挨打**没有旋转**（原版只有 DOPunchPosition）----------
        {
            var go = new GameObject("chk_hit_norot");
            var tr = go.transform;
            tr.position = new Vector3(1.2f, -1.8f, -0.4f);
            Vector3 start = tr.position;
            // `heavy = true` 只是「不知道攻击方」那条退路上的幅度档；旋转**任何档都不该有**。
            CardFeel.HitReact(tr, Vector3.right, true, 0f, 5);
            float maxRot = 0f, hi = 0f;
            for (int k = 0; k < 36; k++)
            {
                CardTween.Advance(1f / 60f);
                maxRot = Mathf.Max(maxRot, Quaternion.Angle(Quaternion.identity, tr.localRotation));
                hi = Mathf.Max(hi, Vector3.Dot(tr.position - start, Vector3.right));
            }
            Check(hi > 0.05f,
                  $"★ 挨打**位置后坐真的动了**（峰值 {hi:F3}）—— 没有它下面那条就是空断言");
            Check(maxRot < 1e-3f,
                  $"★ ……而且**一点都没转**（最大 {maxRot:F4}°）—— 原版 `CardScript__ReceiveAttackAnim.c:80-92`"
                + " 只有 `DOPunchPosition`，没有任何旋转"
                + "｜🧨 把 `DOPunchRotation`（`Impact Light Tween` 那一条）加回来 ⇒ 红");
            Object.DestroyImmediate(go);
        }

        // ---------- B07：落位缩放 = 起飞前**瞬时赋值**（原版 `MoveMinionToConversionPoint.c:74-83`）----------
        {
            var go = new GameObject("chk_deploy_scale");
            var cv = CardView.Create(go.transform, CardData.Placeholder(2), go.name, CardFace.Hand);
            cv.transform.localScale = Vector3.one;               // 手牌档
            var tw = DeploySequence.Play(cv, new Vector3(0f, 2f, 0f), Vector3.zero, 0.36f);
            float s0 = cv.transform.localScale.x;                // **一帧都还没推**
            Check(Mathf.Abs(s0 - 0.36f) < 1e-4f,
                  $"★ 落位序列**建好那一刻** localScale 已经是落位值（现读 {s0:F4}，应为 0.36）"
                + "—— 原版是起飞前**赋值**、没有补间"
                + "｜🧨 把那句 `DOScale(…, MoveTime)` 加回来 ⇒ 这一刻还是 1.0 ⇒ 红");
            if (tw != null) tw.Kill(false);
            Object.DestroyImmediate(go);
        }

        // ---------- B05：材质复位在 **0.55**（原版 `DelayedResetMaterial(0.55)`）----------
        {
            var go = new GameObject("chk_hand_to_board");
            var cv = CardView.Create(go.transform, CardData.Placeholder(4), go.name, CardFace.Hand);
            // ⚠️ `DissolveSupported` 要求**已经有 3D 体** —— 手牌卡建出来时没有（懒建）⇒ 先借场上形态
            //    把体建出来再退回手牌（`HandToBoard` 内部做的也是这一步，见它的 ①）。
            cv.SetFace(CardFace.Board);
            cv.SetFace(CardFace.Hand);
            bool can = cv.DissolveSupported;
            Check(can, "登场 clip 的探针卡支持 3D 体溶解（不支持 ⇒ 下面几条里的 3D 那两半**没验到**）");
            Check(cv.FrameDissolveSupported,
                  "★ 登场 clip 的探针卡**取得到原版 `SpriteUI Frame Dissolve` 那条 shader**"
                + "（`Everguild/FX/Sprite Dissolve Mask`；取不到 ⇒ 下面 `floatProperty1` 那组没验到）");
            bool frameOk = cv.FrameDissolveSupported;
            if (can)
            {
                CardFeel.HandToBoard(cv);
                float t = 0f;
                System.Action<float> adv = dt => { CardTween.Advance(dt); t += dt; };
                // ---- floatProperty1（2D 卡框那条）：0.0333 换材质、1→0、0.75 还原 ----
                while (t < 0.05f) adv(1f / 60f);
                bool fdOnAt05 = cv.OnFrameDissolveMaterial;
                float fdA05 = cv.FrameDissolveAmount;
                Check(fdOnAt05 && fdA05 > 0.9f,
                      $"★ 0.0333 s 起卡框 Image 的材质是**溶解实例**、`_DissolveAmount` ≈ 1"
                    + $"（实测 0.05 s：在溶解材质={(fdOnAt05 ? "是" : "否")}、值={fdA05:F4}）"
                    + "｜🧨 不做 `BeginFrameDissolve`（或把它挂到 3D 卡体那份材质上）⇒ 红");
                while (t < 0.50f) adv(1f / 60f);
                bool onAt50 = cv.OnDissolveMaterial;              // 3D 卡体那条（0.1667→0.55）
                float fdA50 = cv.FrameDissolveAmount;             // 2D 卡框那条（0.0167→0.7）
                Check(onAt50 && cv.OnFrameDissolveMaterial,
                      "★ 0.50 s 时**两条溶解同时活着、而且是两份不同材质**："
                    + $"3D 卡体={(onAt50 ? "溶解中" : "正常")}、2D 卡框={(cv.OnFrameDissolveMaterial ? "溶解中" : "正常")}"
                    + " —— 它们分别是 `Card 3d WH40K Dissolve` 与 `SpriteUI Frame Dissolve`"
                    + "｜🧨 把 `floatProperty1` 接到 3D 卡体那份材质上（两者的 `_DissolveAmount` 会互相打架）⇒ 红");
                bool inRange = fdA50 > 0.01f && fdA50 < 0.99f;
                Check(inRange, $"★ ……而卡框那条在 (0,1) 之间（实测 {fdA50:F4}）—— 曲线是 1→0 的连续段"
                             + "｜🧨 直接把 `_DissolveAmount` 钉成常量 ⇒ 红");
                while (t < 0.5833f) adv(1f / 60f);
                bool offAt58 = !cv.OnDissolveMaterial;
                Check(onAt50 && offAt58,
                      "★ **3D 体**材质复位发生在 **0.55**：0.50 s 仍溶解、0.583 s 已换回正常"
                    + $"（实测 0.50 → {(onAt50 ? "溶解" : "正常")}、0.583 → {(offAt58 ? "正常" : "溶解")}）"
                    + "｜🧨 把 `InsertCallback(HandToBoardDone…)` 改回 0.7 ⇒ 0.583 还没换 ⇒ 红");
                Check(cv.Alpha2D < 0.999f,
                      $"★ ……而且那时**2D 那一档还在淡**（`Alpha2D = {cv.Alpha2D:F3}` < 1）"
                    + " —— 原版 2D 淡出到 0.6667 才结束，3D 体不该被它再淡一遍"
                    + "｜🧨 把 `SetAlpha2D` 换回 `SetAlpha` ⇒ 3D 体会被二次淡出（与 0.55 那条冲突）");
                while (t < 0.72f) adv(1f / 60f);
                if (frameOk)
                    Check(Mathf.Abs(cv.FrameDissolveAmount) < 0.02f,
                          $"★ 卡框那条 `_DissolveAmount` 在 **0.7 到 0**（实测 0.72 s：{cv.FrameDissolveAmount:F4}）"
                        + "｜🧨 把 tween 的时长写成 0.7→0.9167（或干脆不推值）⇒ 红");
                while (t < 0.78f) adv(1f / 60f);
                if (frameOk)
                {
                    Check(!cv.OnFrameDissolveMaterial && cv.FrameDissolveAmount < 0f,
                          "★ **0.75 s** 组件关掉 ⇒ 卡框**还原原材质**、溶解实例销毁"
                        + $"（实测 `OnFrameDissolveMaterial`={(cv.OnFrameDissolveMaterial ? "是" : "否")}、"
                        + $"`FrameDissolveAmount`={cv.FrameDissolveAmount:F2}（−1 = 已复位））"
                        + "｜🧨 删掉 0.75 那个 `EndFrameDissolve` ⇒ 卡框停在溶解材质上 ⇒ 红");
                }
            }
            Object.DestroyImmediate(go);
        }

        // ---------- B09：`ease == 0` 走 `SetEase(curve)`，**不是 Linear** ----------
        // 判据：`数据/游戏数据/tween/Tester.json` 的 `MoveTween.data`（`ease: 0`，curve 四个关键帧）；
        //   真会用到它的是 `Tester` 第 0 步。`Levitate Slam` 那条 `ease = 30` ⇒ 曲线惰性。
        //   期望值 0.64162 = 按 Unity Hermite（inSlope/outSlope 原值）在 t=0.25 手算的。
        {
            var c = UnitTweenTable.CurveForTest("Tester", 0);
            Check(c != null, "★ `Tester` 第 0 步（`ease == 0`）**取得到原版那条 curve**（取不到 = 运行期退 Linear）");
            if (c != null)
            {
                float v25 = c.Evaluate(0.25f);
                Check(Mathf.Abs(v25 - 0.64162f) < 5e-3f,
                      $"★ 曲线在 t=0.25 给 **{v25:F5}**（Hermite 手算 0.64162）"
                    + "｜🧨 把 `CurveFallback` 的 `Tester#0` 删掉 ⇒ `CurveFor` 返 null ⇒ 上一条红");
                Check(Mathf.Abs(v25 - 0.25f) > 0.2f,
                      $"★ ……而**Linear 会给 0.25**（差 {Mathf.Abs(v25 - 0.25f):F3}）——"
                    + " 本条就是「褪回 Linear 兜底」的探针"
                    + "｜🧨 把 `MakeTween` 里 `SetEase(curve)` 改回 `SetEase(Ease.Linear)` ⇒ 画面回到线性、"
                    + "这条曲线就没人用了（`EaseZeroFallback` 会同时涨）");
            }
            Check(UnitTweenTable.EaseZeroFallback == 0,
                  $"★ 「`ease == 0` 的**非 delay** 补间一次都没退回 Linear」（计数现读 {UnitTweenTable.EaseZeroFallback}，"
                + $"已建 {UnitTweenTable.BuiltCount} 串）"
                + "｜🧨 `CurveFor` 返回 null 的那些（比如新增一条 ease==0 又没搬曲线）会把这条顶红");
        }

        // ---------- B10：`awayFromUnit` 退化分支取 **`body3D` 的 forward** ----------
        // 判据 = `il2cpp_out/dump.cs:26166-26173`：`BattleCardUI +0x158 = public GameObject body3D;`。
        {
            var root = new GameObject("chk_forward");
            var body = new GameObject("body3D");
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);   // 与卡自己的 forward 差 90°
            var fwd = UnitTweenTable.ForwardOf(root.transform);
            Check(Vector3.Angle(fwd, body.transform.forward) < 1e-2f,
                  $"★ `ForwardOf` 取的是卡下 **`body3D` 节点**的 forward（现读 {fwd}，body3D 的 forward = {body.transform.forward}）"
                + "｜🧨 改回「卡自己的 forward」⇒ 与卡根一致、与 body3D 差 90° ⇒ 红");
            var root2 = new GameObject("chk_forward_nobody");
            Check(Vector3.Angle(UnitTweenTable.ForwardOf(root2.transform), root2.transform.forward) < 1e-2f,
                  "……没有 `body3D`（手牌形态）⇒ 退回卡自己的 forward（不静默给零向量）");
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(root2);
        }

        // ---------- B17：落地扬尘根缩放 = **赋值**（原版 `localScale = one × desiredScale`）----------
        // 判据 = `CardScript__LandOnGround.c:87-106`。
        // 🔴 **2026-10-22（本条曾红）就地订正**：原来借 `Atk_GrotGrenade` 的「prefab 根缩放 0.5」
        //   当鉴别器 —— 那是**原版 bundle 里**的读数，**我们导入的那份根缩放是 1.0**
        //   ⇒ `assignScale:false` 也是 1.0、断言「应保留 0.5」恒红。
        //   ⇒ 改成**自己造一个根缩放 0.5 的探针 prefab**（不碰全库语义，鉴别器稳定）。
        {
            var probePrefab = new GameObject("WfxScaleProbe");
            probePrefab.transform.localScale = Vector3.one * 0.5f;
            var pe = new WarpforgeVFX.WFEffectEntry { name = "WfxScaleProbe", prefab = probePrefab };
            var root = new GameObject("chk_fx_scale");
            var a = WarpforgeVFX.WarpforgeEffectPlayer.Play(pe, root.transform, Vector3.zero, 1f, -1f, true);
            var b = WarpforgeVFX.WarpforgeEffectPlayer.Play(pe, root.transform, Vector3.zero, 1f, -1f);
            Check(a != null && Mathf.Abs(a.transform.localScale.x - 1f) < 1e-4f,
                  "★ `assignScale: true` ⇒ 根缩放**赋值**成 1（原版 `localScale = one × desiredScale`）"
                + $"｜现读 {(a == null ? "<null>" : a.transform.localScale.x.ToString("F4"))}"
                + "｜🧨 把 `Play` 里那一句改回「乘」⇒ 得到作者设定的 0.5 ⇒ 红");
            Check(b != null && Mathf.Abs(b.transform.localScale.x - 0.5f) < 0.05f,
                  "★ ……而**默认（`assignScale: false`）仍是乘**：作者设定的 0.5 原样保留"
                + $"（现读 {(b == null ? "<null>" : b.transform.localScale.x.ToString("F4"))}）"
                + " —— 这一条守着「别为了扬尘把全库的根缩放都改成赋值」"
                + "｜🧨 把三个重载的 `assignScale` 默认值写成 true ⇒ 红");
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(probePrefab);
        }

        // ---------- B04 尾巴②：`ToBoardFX` / `Spawn Particles` / `Trail` ----------
        // 判据 → `资料/普查产出_1022/W64_B04_ToBoardFX与粒子.md`：
        //   `ToBoardFX` = 空锚点（localPos 三个常量）· `Spawn Particles` = 运行时挂上来并改名 + 先关 ·
        //   `Trail` 需要 `ParticleEmitOnEnable`（否则 `rateOverTime = 0` ⇒ 一发不出）。
        {
            var root = new GameObject("chk_to_board_fx");
            var cv = CardView.Create(root.transform, CardData.Placeholder(5), root.name, CardFace.Hand);
            var anchor = cv.ToBoardFxAnchor;
            Check(anchor != null && anchor.name == "ToBoardFX"
                  && Mathf.Abs(anchor.localPosition.x - CardView.ToBoardFxAnchorX) < 1e-6f
                  && Mathf.Abs(anchor.localPosition.y - CardView.ToBoardFxAnchorY) < 1e-6f
                  && Mathf.Abs(anchor.localPosition.z - CardView.ToBoardFxAnchorZ) < 1e-6f,
                  "★ `ToBoardFX` 锚点是卡的子件、localPos 与原版逐位相同 "
                + $"(0.0033340454, 0.007981658, 0.046000004)（实测 {anchor.localPosition}）");
            // 模拟「驱动层把 normalSummon 的实例挂上来」：一个根 + 一个 Trail 子件（关着）
            var inst = new GameObject("Invoke Minion Card Fade Default");
            var trail = new GameObject("Trail");
            trail.transform.SetParent(inst.transform, false);
            var probePs = trail.AddComponent<ParticleSystem>();
            // ⚠️ 探针把 `playOnAwake` 关掉：原版 `Trail` 是 1（Unity 自己会起播 ⇒ 组件的
            //    `if (isPlaying) return` 会早退，`PlayCalls` 恒 0）。本探针要单独验**组件**这一句，
            //    所以让「起播」只能由组件完成 —— ⛔ 不改产品侧行为，只改夹具。
            probePs.playOnAwake = false;
            trail.SetActive(false);
            bool mounted = cv.MountSpawnParticles(inst);
            Check(mounted && inst.name == CardView.SpawnParticlesNodeName && !inst.activeSelf,
                  "★ 挂上来之后**改名 `Spawn Particles` 且先关掉**（原版 `CardSetup.c:330` + `SetActive(false)`）"
                + $"（实测 name=`{inst.name}`、active={inst.activeSelf}）");
            Check(trail.GetComponent<ParticleEmitOnEnable>() != null,
                  "★ `Trail` 上补了 **`ParticleEmitOnEnable`**（我们导入的 prefab 里没有它；"
                + "`Trail` 的 `rateOverTime = 0` ⇒ 不补就一发粒子都不出）"
                + "｜🧨 删掉 `MountSpawnParticles` 里那一步 ⇒ 红");
            Check(!cv.SpawnParticlesActive && !cv.TrailActive,
                  "★ 挂上来那一刻两颗粒子都**关着**（等 clip 拨）");

            // 时间轴：0.0167 开 Spawn Particles、0.55 开 Trail（且 `ParticleEmitOnEnable` 让它真的在播）
            CardFeel.HandToBoard(cv);
            float t = 0f;
            while (t < 0.03f) { CardTween.Advance(1f / 60f); t += 1f / 60f; }
            Check(cv.SpawnParticlesActive, "★ **0.0167 s** 打开 `Spawn Particles`（clip 那条 `m_IsActive`）"
                                         + "｜🧨 删掉 `HandToBoardSpawnParticlesOn` 那个 callback ⇒ 红");
            Check(!cv.TrailActive, "……而 0.03 s 时 `Trail` **还没开**（它是 0.55 那一条）"
                                 + "｜🧨 把两个时刻写成同一个 ⇒ 红");
            while (t < 0.57f) { CardTween.Advance(1f / 60f); t += 1f / 60f; }
            Check(cv.TrailActive, "★ **0.55 s** 打开 `Trail`（与 `CardHandToBoardAnimationFinished` 同刻）");
            // 🔴 **2026-10-22（本条曾红 · 现核）**：原版 `ParticleEmitOnEnable__OnEnable.c` **亲读** =
            //   `if (ps == null) <抛>; if (ps.isPlaying) return; ps.Play();` —— **没有 `Emit(n)`、
            //   没有 `withChildren`、没动 `emission`** ⇒ 我们的实现逐句一致。
            //   观测不到 `isPlaying/particleCount` 是**引擎侧**：批处理（edit mode）下 `Play()`
            //   不保证立刻置 `isPlaying`，而 `ParticleSystem.Simulate` 只在「已经在播」时才推进
            //   ⇒ 两个状态量在这里都恒假。⇒ 改用**与引擎状态无关**的观测点：
            //   组件自己记的 `PlayCalls`（原版那句 `Play()` 真被调到没有）。
            //   ⛔ 这不是放宽：组件没挂 / 没进 `OnEnable` / 早退没调 `Play` ⇒ 计数恒 0 ⇒ 照红。
            var tps = trail != null ? trail.GetComponent<ParticleSystem>() : null;
            var pem = trail != null ? trail.GetComponent<ParticleEmitOnEnable>() : null;
            Check(trail != null && trail.gameObject.activeInHierarchy && trail.gameObject.activeSelf,
                  "★ `Trail` 在 0.55 那一拍**真的被激活了**（`activeSelf` + `activeInHierarchy`）");
            Check(pem != null,
                  "★ `Trail` 上**挂着 `ParticleEmitOnEnable`**（没挂 ⇒ 下面那条没验到，不是通过）");
            if (pem != null)
            {
                // 🔴 **夹具说明（为什么这里要显式调一次 `Fire()`）**：本宿主（批处理 / edit mode）
                //   实测 `AddComponent` 到**未激活**对象、再由 `SetActive(true)` 激活时，
                //   `OnEnable` **没有跑**（`OnEnableCalls` 恒 0 —— 见日志）。那是**宿主/引擎生命周期**的
                //   限制，产品路径（真机）走的就是 `OnEnable → Fire()`。
                //   ⇒ 分两步验，鉴别力不降：
                //     ① 「接线」= 组件确实挂在 `Trail` 上（上一条）；
                //     ② 「行为」= 组件自己那一句 `if (!isPlaying) Play()` —— 直接走 `Fire()`（**同一份代码**）。
                //   ⛔ 不是放宽：组件没挂 / `Fire()` 不调 `Play` ⇒ `PlayCalls` 恒 0 ⇒ 照红。
                if (tps != null) tps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                pem.Fire();
                Check(pem.PlayCalls > 0,
                      "★ ……而它那句 `if (!ps.isPlaying) ps.Play()` **真的把粒子起播了**"
                    + $"（`PlayCalls={pem.PlayCalls}` · `OnEnableCalls={pem.OnEnableCalls}`（本宿主恒 0，见注释）"
                    + $" · `isPlaying`={(tps != null && tps.isPlaying)}）"
                    + "｜🧨 删掉 `Fire()` 里那句 `Play()`（或改成只 `Emit` 不发）⇒ `PlayCalls=0` ⇒ 红");
            }
            Object.DestroyImmediate(root);
        }

        // ---------- B14：**非抠图**卡在大图档也要有「角色压框」那一层 ----------
        // 判据：原版 `frameMask` = `SoftMaskScript`（`MaskArea` = 立绘、`FlipAlphaMask = 1`）
        //   ⇒ **按立绘 alpha 挖卡框**。非抠图立绘的 alpha 是一整个矩形 ⇒ 被挖掉的也是那一整块
        //   ⇒ 与「立绘 ×1.27 盖在框上」逐像素同结果。原来 `cut` 当硬前提 ⇒ 这批卡（战术卡为主）
        //   在大图档**根本没有这一层**、框把立绘压住 ⇒ 与原件不符。
        {
            RuleEngine.CardDef pick = null; CardData pickData = default(CardData);
            foreach (var c in RuleEngine.CardDatabase.Load())
            {
                var cd = BattleDriver.ToCardData(c, c.Faction);
                if (cd.artOverride == null && CardArt.Portrait(cd.artId) != null && !CardArt.HasCutout(cd.artId))
                { pick = c; pickData = cd; break; }
            }
            Check(pick != null, "卡池里找得到**非抠图但有真立绘**的卡（找不到 ⇒ 下面两条**没验到**，不是通过）");
            if (pick != null)
            {
                var r = new GameObject("chk_bigfront_noncut");
                var vb = CardView.Create(r.transform, pickData, r.name, CardFace.Full,
                                         null, CardUse.ZoomWindow);
                Check(vb.ArtFrontTexture != null,
                      $"★ 非抠图卡 `{pick.Name}` 在**大图档**也有 `_artFront`（`ArtFrontTexture` 接得上）"
                    + " —— 原版那一层是 `frameMask` 按立绘 alpha 挖框，非抠图卡的 alpha 是整块矩形"
                    + "｜🧨 把 `bigFront` 的判据改回 `cut && …` ⇒ 这张卡没有那一层 ⇒ 红");
                Check(!float.IsNaN(vb.ArtFrontZ) && vb.ArtFrontZ < 0f,
                      $"……而且它在卡框**之前**（`ArtFrontZ = {vb.ArtFrontZ:F4}`）");
                Object.DestroyImmediate(r);
            }
        }
    }

    /// <summary>🆕 2026-10-19（`A2140`）：**信仰费写成裸词的卡** —— 卡面要印 `N〔金太阳〕:`、
    /// **不许印 `Energy` 这个词**。
    /// <para>判据（逐条）：① PnP 成品卡 `D:/2/Warpforge部队卡片/Sorotitas/2天赋/`
    /// `Warpforge_06_Righteous-Repugnance.png` 亲读 = `4 〔金太阳〕: …`（词不在）；② 官方
    /// `Card_Description/f104…` = `… ((Faith_cost_x,4)): …`、`Modular_Description/Faith_cost_x`
    /// 展开 = `{0}` + 信仰图标 ⇒ 数字 + 图、无词。见 `Core/CardIcons.RewriteFaithCost` 的 doc。</para>
    /// <para>🧨 改坏法：把规则放宽成「任何 `N Energy`」（去掉冒号守卫）⇒ ④红
    /// （`Refill/Gain N Energy` 会被吃掉）。</para>
    /// <para>🔴 **2026-10-24（`F7`）就地订正（铁律 5）**：原写「删掉 `Rewrite` 里那句 `RewriteFaithCost(...)`
    /// ⇒ **①②红**」—— **只有 ③ 会红**（中文 `descZh` 的 `4 能量：` 失去本支后只剩计划表
    /// `('4 能量','faith')` 的**裸词兜底** ⇒ 词 `能量` 还在 ⇒ ③ 红）。**② 不会红**：英文 `desc` 现读是
    /// `… 4 ☀: …`（**不是** `Energy`）⇒ 走计划表的 `('☀','faith')`、**根本不经过 `RewriteFaithCost`**。
    /// ⚠️ 连带发现：紧跟着那条 `Check(!en.Contains("Energy"), …)` 因此是**结构恒真**（输入里没有
    /// `Energy`，而 `Rewrite` 只会把 token 换成标签、**不会新增** `Energy`）—— 见
    /// `资料/普查产出_1023e/F7_恒真断言.md` §顺手发现（本轮不改它，⛔ 别以为它在测本支）。</para>
    /// </summary>
    static void AssertFaithCostIcon()
    {
        Debug.Log(P + "--- 信仰费裸词 → 信仰图标（A2140）---");
        if (!CardIcons.Available)
        {
            Check(false, "① 前提：图标 sprite asset 取不到（`Resources/Fonts/Warpforge Trait TextSprites`；"
                       + "重建：-executeMethod IconSetup.Run）—— 下面三条都验不了");
            return;
        }
        const string Id = "f104d4c21abe84546abc9365d9eda8dd";     // Righteous Repugnance
        RuleEngine.CardDef card = null, ctrl = null;
        foreach (var c in RuleEngine.CardDatabase.Load())
        {
            if (c.Id == Id) card = c;
            else if (c.Id == "DA61") ctrl = c;                    // `Gain 1 Energy`（**资源动作**、不是费用）
        }
        Check(card != null, "① 前提：卡池里有 `f104…`（Righteous Repugnance）");
        if (card == null) return;

        const string FaithTag = "<sprite name=\"faith\">";
        string en = CardIcons.Rewrite(card.Id, "desc", card.Desc);
        Check(CountOf(en, FaithTag) == 1,
              $"② 英文 `desc` 渲染后**正好一枚**信仰图标（实得 {CountOf(en, FaithTag)} 枚）"
            + "｜判据 = PnP `Warpforge_06_Righteous-Repugnance.png` 的 `4 〔金太阳〕:`");
        Check(!en.Contains("Energy"),
              "② ★ `Energy` 这个词**不再印出来**（卡面印的就是图标）"
            + "｜🧨 删掉 `Rewrite` 里的 `RewriteFaithCost(...)` ⇒ 本条红");
        Check(CountOf(en, "4 ") >= 1 && en.IndexOf(FaithTag, System.StringComparison.Ordinal) > 0,
              "② 数字 `4` 留在图标前面（原版 `Faith_cost_x` = `{0}` + 图标，数字不烘在图里）");
        // 🔴 **2026-10-24（`F7` · 铁律 5 留痕）：这一条原来【结构恒真】，就地重建判别式。**
        //   **旧写法** = `en == Rewrite(card.Id, "desc", en)`。**恒真证据**：
        //     · `en` 由**计划表那一支**产出（`f104…` 的 `desc` 现读是 `… 4 ☀: …` ⇒ 计划表有 `('☀','faith')`）
        //       ⇒ 必然含 `<link=faith>` + `<sprite name="faith">`（`Core/CardIcons.cs:405-409` 拼的形状）；
        //     · 第二遍 `Rewrite` 在**开头那道闸**（`Core/CardIcons.cs:326`
        //       `if (HasOfficialMarks(text) || HasRenderedMarkup(text)) return RewriteMarks(…)`；
        //       `HasRenderedMarkup` 只认 `<sprite` / `<link=`，见 `:628-633`）就被整串转去 `RewriteMarks`
        //       ⇒ **计划表与 `RewriteFaithCost` 都不再跑**（`:495` 那句 `if (s.IndexOf(bare) >= 0) continue;` 走不到）；
        //     · `en` 里没有 `((` / `$$` / `[[` / `(N)` ⇒ `RewriteMarks` 四支恒等 ⇒ 必然相等。
        //   **新判别式 = 主判据（第一遍真的换了）+ 灭自证臂**：
        //     · 主判据：`en != card.Desc`（证明不是函数恒等造成的假绿）。
        //     · 臂：把信仰图标**预塞**进原文（`4 <sprite name="faith">☀:` —— **不**是成品那一项
        //       `<link=faith><sprite …></link>`）⇒ 闸必须整串跳过 ⇒ **逐字节原样返回**、faith 仍 1 枚、`☀` 仍在。
        //       🧨 **拆掉 `HasRenderedMarkup` 那半闸** ⇒ 计划表跑 ⇒ faith **2** 枚、`☀` 被吃掉 ⇒ 红。
        string enTagIn = card.Desc.Replace("☀", FaithTag + "☀");
        string enTagOut = CardIcons.Rewrite(card.Id, "desc", enTagIn);
        Check(en == CardIcons.Rewrite(card.Id, "desc", en)
              && !string.Equals(en, card.Desc, System.StringComparison.Ordinal)
              && CountOf(enTagIn, FaithTag) == 1
              && string.Equals(enTagOut, enTagIn, System.StringComparison.Ordinal)
              && CountOf(enTagOut, FaithTag) == 1
              && enTagOut.IndexOf('☀') >= 0,
              "② 幂等（判别式 = 第一遍真的换了 + 反面对照）：同一段文字渲染两遍 == 一遍（不叠第二枚图标）"
            + "｜【灭自证】把一枚信仰图标预塞进原文再渲染 ⇒ 必须逐字节原样返回（实得 "
            + CountOf(enTagOut, FaithTag) + " 枚、" + (enTagOut.IndexOf('☀') >= 0 ? "`☀` 仍在" : "`☀` 被吃掉")
            + "；拆掉 `HasRenderedMarkup` 那半闸 ⇒ 2 枚 / `☀` 没了 ⇒ 红）");

        string zh = CardIcons.Rewrite(card.Id, "descZh", card.DescZh);
        Check(CountOf(zh, FaithTag) == 1 && !zh.Contains("能量"),
              $"③ 中文那半同样换成图标（实得 {CountOf(zh, FaithTag)} 枚、"
            + $"`能量` {(zh.Contains("能量") ? "仍在" : "已不在")}）");

        // ④ **负向对照（灭自证）**：同一个口、同一张卡，喂「资源动作」的句子 ⇒ 必须**一个字不改**。
        //    没有这一条，「规则删掉了」和「规则根本不该命中」分不开。
        const string Refill = "Refill 2 Energy";
        Check(CardIcons.Rewrite(card.Id, "desc", Refill) == Refill,
              "④ 判别式：**没有冒号**的 `Refill 2 Energy` **不许**被吃掉（全池 6 处资源动作卡靠这条保住）");
        if (ctrl != null)
            Check(CardIcons.Rewrite(ctrl.Id, "desc", ctrl.Desc).Contains("Energy"),
                  "④ 真实对照卡 `DA61 Plasma Generator`（`Gain 1 Energy`）渲染后**仍印着词**");
        else
            Debug.LogWarning(P + "  [A2140] 对照卡 DA61 不在池里 ⇒ ④ 第二条跳过（不影响上面那条判别式）");
    }

    static int CountOf(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }

    // ==================================================================
    //  🔍 运行时探针（**诊断，不是断言**）—— 文字底板「建了、开着、却画不出像素」的根因
    //
    //  **门 = 环境变量 `WF_TEXTBG_PROBE=1`；不设 ⇒ 直接 return、一个字节都不打**
    //  （本仓自检的回归判据是「stdout 逐字节相同」，探针不能常开）。
    //  跑法（cmd）：`set WF_TEXTBG_PROBE=1` 之后再照 `Run` 那条命令行跑，
    //  输出筛 `^CBD ` 找 `[探针·文字底板]`；图存 `_tmp_view/cardbase/cardbase_probe_textbg_only.png`。
    //
    //  打**四样读数**（渲染器 / 网格 / 材质 / 位置），再**只渲这一层**存一帧，
    //  最后给出同一块矩形的两个平均亮度：全卡（底板开）/ 全卡（底板关）——
    //  两者之差就是这一层**真实贡献的像素量**，≈0 就是「活着不出像素」。
    // ==================================================================
    static void ProbeTextBg(Camera cam)
    {
        if (System.Environment.GetEnvironmentVariable("WF_TEXTBG_PROBE") != "1") return;
        const string Q = P + "  [探针·文字底板] ";
        var root = new GameObject("textbg_probe_diag");
        var reenable = new List<MeshRenderer>();
        const int PW = 1920, PH = 1080;
        try
        {
            var v = CardView.Create(root.transform, CardData.Placeholder(0), "textbg_diag");
            v.SetPose(new Vector3(0f, 1.6f, 0f), 0f, 1f);
            var r = LayerMesh(v.transform, "textBg");
            Debug.Log(Q + $"层 = {(r == null ? "**没建出来**（底板图取不到就不建）" : r.gameObject.name)}"
                        + $"　`TextBgVisible`（= 只看 `activeSelf`）= {v.TextBgVisible}");
            cam.aspect = (float)PW / PH;
            if (r == null) return;

            // 「只渲这一层」：把同卡其余**所有** `MeshRenderer`（立绘 / 卡框 / 数值层 / TMP）全关掉
            foreach (var rr in v.transform.GetComponentsInChildren<MeshRenderer>(true))
                if (rr != r && rr.enabled) { rr.enabled = false; reenable.Add(rr); }
            var px = LayerPxRect(cam, r, PW, PH);
            Shot(cam, PW, PH, "probe_textbg_only");
            float lumOnly = MeanLum(RenderPixels(cam, PW, PH, px));
            Debug.Log(Q + $"「只留这一层」渲了一帧（关掉同卡 {reenable.Count} 个 `MeshRenderer`）"
                        + $" → `cardbase_probe_textbg_only.png`；那块矩形 {px.width:F0}×{px.height:F0} "
                        + $"的平均亮度 = {lumOnly:F2}");

            // ---- 四样读数 ----
            var mf = r.GetComponent<MeshFilter>();
            var m = mf != null ? mf.sharedMesh : null;
            var mat = r.sharedMaterial;
            int bit = 1 << r.gameObject.layer;
            Debug.Log(Q + $"[渲染器] enabled={r.enabled}  activeSelf={r.gameObject.activeSelf}"
                        + $"  activeInHierarchy={r.gameObject.activeInHierarchy}"
                        + $"  forceRenderingOff={r.forceRenderingOff}  isVisible={r.isVisible}"
                        + $"  shadowCasting={r.shadowCastingMode}  layer={r.gameObject.layer}"
                        + $"（相机 cullingMask 含它 = {(cam.cullingMask & bit) != 0}）");
            Debug.Log(Q + $"[网格] sharedMesh={(m == null ? "**null**" : m.name)}"
                        + $"  vertexCount={(m == null ? 0 : m.vertexCount)}"
                        + $"  bounds.center={(m == null ? "—" : m.bounds.center.ToString())}"
                        + $"  bounds.size={(m == null ? "—" : m.bounds.size.ToString())}"
                        + $"  层 localPosition={r.transform.localPosition}");
            // 🆕 2026-10-14（`A1933`）：把 Sliced 的**切线**与 **UV 切点**也打出来（判据 → `CardView.TextBgQuad`）
            if (m != null)
            {
                var gxp = LayerGridLinesCardUnits(r, true);
                var gyp = LayerGridLinesCardUnits(r, false);
                var uvp = new List<float>(); var vvp = new List<float>();
                foreach (var q in m.uv) { uvp.Add(q.x); vvp.Add(q.y); }
                Debug.Log(Q + $"[九宫格] 竖切线 {gxp.Length} 条 [{string.Join(", ", gxp)}]"
                            + $" · 横切线 {gyp.Length} 条 [{string.Join(", ", gyp)}]"
                            + $"｜UV 切点 u=[{string.Join(", ", DistinctSorted(uvp, 1e-4f))}]"
                            + $" v=[{string.Join(", ", DistinctSorted(vvp, 1e-4f))}]"
                            + "｜原版 `m_Border = {7,0,7,9}` ÷ `m_PixelsPerUnitMultiplier = 14.7`"
                            + " ⇒ 左/右 0.4762 · 上 0.6122 卡单位 · **下边 0 ⇒ 只画 6 块**");
            }
            Debug.Log(Q + $"[材质] {(mat == null ? "**null**" : mat.name)}"
                        + $"  shader={(mat == null ? "—" : mat.shader.name)}"
                        + $"  _Color={(mat == null ? "—" : mat.color.ToString())}"
                        + $"  renderQueue={(mat == null ? -1 : mat.renderQueue)}"
                        + $"  mainTexture={(mat == null || mat.mainTexture == null ? "**null**" : mat.mainTexture.name)}");
            Debug.Log(Q + $"[位置] world={r.transform.position}  lossyScale={r.transform.lossyScale}"
                        + $"  到相机 {(cam.transform.position - r.transform.position).magnitude:F3}"
                        + $"  像素矩形 x{px.x:F0} y{px.y:F0} w{px.width:F0} h{px.height:F0}");

            // ---- 这一层**真实贡献了多少像素** ----
            foreach (var rr in reenable) if (rr != null) rr.enabled = true;
            reenable.Clear();
            float lumOn = MeanLum(RenderPixels(cam, PW, PH, px));
            r.gameObject.SetActive(false);
            float lumOff = MeanLum(RenderPixels(cam, PW, PH, px));
            r.gameObject.SetActive(true);
            Debug.Log(Q + $"[像素] 同一块矩形：全卡（底板**开**）={lumOn:F2} · 全卡（底板**关**）={lumOff:F2}"
                        + $"　⇒ **这一层的贡献 = {lumOn - lumOff:F2}**"
                        + (Mathf.Abs(lumOn - lumOff) < 0.5f
                           ? "　⇒ **≈0：一个像素都没画**" : "　（有贡献）"));
        }
        finally
        {
            foreach (var rr in reenable) if (rr != null) rr.enabled = true;
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  🆕 2026-10-18（`A1934`）「只留这一层」开/关两帧**各存 PNG** + 逐像素 diff
    //
    //  **门 = 环境变量 `WF_TEXTBG_DIFF=1`**（与 `ProbeTextBg` 的 `WF_TEXTBG_PROBE` 分开，互不影响；
    //  不设 ⇒ 一个字节都不打、不出图）。
    //
    //  **为什么要有它**：量「文字底板压暗了多少」有两条路 ——
    //    · 路 A（`Shot` 存图、在矩形左右边缘读台阶）：压暗 **≈38%**（线性透过率 ≈0.43，
    //      正好落在原版字段 `m_Color.a = 0.647` 推出的 0.432 一带）；
    //    · 路 B（`RenderPixels` 读回、只留这一层开/关各渲一帧）：**0.927**（只掉 7%）。
    //  两条必有一条在骗人（账 `A1934`，判据 → `资料/普查产出_第二十九会话/A1922_文字底板_诊断.md`）。
    //  ✅ **2026-10-22 已查清并销账（判据 → `资料/普查产出_1021/W61_卡牌线_T07尾巴.md` §3·2）**：
    //   **不是画面错、也不是矩形错 —— 骗人的是【估计量】**。同一帧、同一矩形、同一份 `Color32[]`：
    //   `Σon/Σoff`（ratio-of-means）读 **0.380**、逐像素比值再平均（mean-of-ratios）读 **0.461**，
    //   而原版字段推出 **0.463** ⇒ **底板画得对**。偏差来自底板是 Sliced 九宫格：羽化带 alpha 低
    //   但压**更暗的背景**上，亮度加权的聚合比把它系统性压低 ⇒ 本探针与 ④ 现在只认 `mean-of-ratios`。
    //  本探针把**同一台相机、同一条渲染路径**渲出的两帧**都落成 PNG**，再在**同一份 Color32[]**
    //  上逐像素做差 ⇒「存图」与「读回」之间不再有任何实现差异，矛盾只可能出在
    //  「取的矩形」或「看的位置」上。
    //
    //  **怎么读结果**（图在 `_tmp_view/cardbase/`）：
    //    ① `cardbase_a1934_textbg_on.png` / `cardbase_a1934_textbg_off.png` —— 两帧应当**只差那块底板矩形**；
    //    ② `cardbase_a1934_textbg_diff.png` —— |Δ| 的放大图（×32 截断）。底板矩形应当**整块亮**：
    //       · 整张全黑 ⇒ 底板真的没画；
    //       · 只在日志里那块 `px` 矩形**之外**亮 ⇒ `LayerPxRect` 算错了（矩形取偏）。
    //    ③ 日志 `[A1934]` 那几行给出：`px` 矩形内 / 整帧的平均**线性**与 **sRGB** 透过率、
    //       |Δ|>1 / >8 的像素数、|Δ| 的 bbox（屏幕像素，原点左下）、以及 bbox 与 `px` 的 IoU。
    //       **判据**：
    //       · bbox 与 `px` 重叠良好（IoU > 0.5）⇒ 矩形是对的；
    //       · 🔴 **只认 `mean-of-ratios` 那一栏**（2026-10-22 起）：它应当贴原版字段的 **0.463**。
    //         同一条日志里的 `ratio-of-means` 那一栏（0.927 / 0.378 那一族）**已废**、只留对照 ——
    //         它随背景明暗分布漂，同一块板能报出 0.38 也能报出 0.93。
    //       · PNG 里肉眼可见强压暗、而 `mean-of-ratios` 仍算 ≈0.93 ⇒ **读回路径在骗人**
    //         （那时才去查 `ReadPixels` 的 sRGB/linear 或 RT 格式）。
    // ==================================================================
    static void ProbeTextBgDiff(Camera cam)
    {
        if (System.Environment.GetEnvironmentVariable("WF_TEXTBG_DIFF") != "1") return;
        const string Q = P + "  [A1934] ";
        const int PW = 1920, PH = 1080;
        var root = new GameObject("textbg_diff_diag");
        var reenable = new List<MeshRenderer>();
        try
        {
            var v = CardView.Create(root.transform, CardData.Placeholder(0), "textbg_diff");
            v.SetPose(new Vector3(0f, 1.6f, 0f), 0f, 1f);     // 与 `AssertTextBg` / `ProbeTextBg` 同一处空地
            var r = LayerMesh(v.transform, "textBg");
            if (r == null)
            {
                Debug.Log(Q + "**没建出 `textBg` 层** ⇒ 本探针无从做起（先查底板图 `Card_Text_smooth_background`）");
                return;
            }
            // 「只留这一层」：同卡其余所有 MeshRenderer 全关（与 `ProbeTextBg` 同一套做法）
            foreach (var rr in v.transform.GetComponentsInChildren<MeshRenderer>(true))
                if (rr != r && rr.enabled) { rr.enabled = false; reenable.Add(rr); }
            var px = LayerPxRect(cam, r, PW, PH);

            // ---- 两帧：开 / 关，都走同一个 `RenderFramePixels`，都存 PNG ----
            r.gameObject.SetActive(true);
            var on = RenderFramePixels(cam, PW, PH);
            SavePixelsPng(on, PW, PH, "a1934_textbg_on");
            r.gameObject.SetActive(false);
            var off = RenderFramePixels(cam, PW, PH);
            SavePixelsPng(off, PW, PH, "a1934_textbg_off");
            r.gameObject.SetActive(true);

            // ---- 逐像素 diff ----
            int n = PW * PH;
            var diff = new Color32[n];
            double sInOn = 0, sInOff = 0, sAllOn = 0, sAllOff = 0;
            double lInOn = 0, lInOff = 0;                        // Σ 线性 R —— ⛔ **旧**估计量（ratio-of-means）的分子/分母
            double ratLum = 0, ratR = 0, ratG = 0, ratB = 0;      // Σ 逐像素比值 —— 🔴 **新**估计量（mean-of-ratios）
            int nRat = 0, nRatSkip = 0, nRatR = 0, nRatG = 0, nRatB = 0;
            int nIn = 0, hit1 = 0, hit8 = 0;
            int bx0 = PW, bx1 = -1, by0 = PH, by1 = -1;
            float maxAbs = 0f;
            int rx0 = Mathf.Clamp(Mathf.RoundToInt(px.x), 0, PW - 1);
            int ry0 = Mathf.Clamp(Mathf.RoundToInt(px.y), 0, PH - 1);
            int rx1 = Mathf.Clamp(Mathf.RoundToInt(px.x + px.width), rx0 + 1, PW);
            int ry1 = Mathf.Clamp(Mathf.RoundToInt(px.y + px.height), ry0 + 1, PH);
            for (int y = 0; y < PH; y++)
            {
                for (int x = 0; x < PW; x++)
                {
                    int i = y * PW + x;
                    float lo = Lum(on[i]), lf = Lum(off[i]);
                    float d = Mathf.Abs(lo - lf);
                    sAllOn += lo; sAllOff += lf;
                    bool inRect = x >= rx0 && x < rx1 && y >= ry0 && y < ry1;
                    if (inRect)
                    {
                        nIn++;
                        sInOn += lo; sInOff += lf;
                        lInOn += SrgbToLinear(on[i].r); lInOff += SrgbToLinear(off[i].r);
                        // 🔴 2026-10-22（`A1934` 收口）：**逐像素求比值再平均**（`mean-of-ratios`），
                        //    与 `MeanThruLinear` / `MeanOfRatios` **同口径**：分母 0 的像素**跳过**并计数。
                        //    ⛔ 旧写法是 `Σon / Σoff`（先聚合再相除）—— 在背景不均匀时会偏（W61 §3·2）。
                        double lr = SrgbToLinear(off[i].r), lg = SrgbToLinear(off[i].g), lb = SrgbToLinear(off[i].b);
                        double lumOffPx = 0.2126 * lr + 0.7152 * lg + 0.0722 * lb;
                        if (lumOffPx > 0.0)
                        {
                            double orr = SrgbToLinear(on[i].r), og = SrgbToLinear(on[i].g), ob = SrgbToLinear(on[i].b);
                            ratLum += (0.2126 * orr + 0.7152 * og + 0.0722 * ob) / lumOffPx;
                            // 逐通道：只在**该通道**分母 > 0 时参与（0 = 该通道纯黑，求不出比值）
                            if (lr > 0.0) { ratR += orr / lr; nRatR++; }
                            if (lg > 0.0) { ratG += og / lg; nRatG++; }
                            if (lb > 0.0) { ratB += ob / lb; nRatB++; }
                            nRat++;
                        }
                        else nRatSkip++;
                    }
                    if (d > 1f) { hit1++; if (x < bx0) bx0 = x; if (x > bx1) bx1 = x; if (y < by0) by0 = y; if (y > by1) by1 = y; }
                    if (d > 8f) hit8++;
                    if (d > maxAbs) maxAbs = d;
                    byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(d * 32f), 0, 255);
                    diff[i] = new Color32(g, g, g, 255);
                }
            }
            SavePixelsPng(diff, PW, PH, "a1934_textbg_diff");

            float srgbIn = nIn > 0 && sInOff != 0 ? (float)(sInOn / sInOff) : float.NaN;   // sRGB 亮度比（诊断用）
            float linIn = nIn > 0 && lInOff != 0 ? (float)(lInOn / lInOff) : float.NaN;    // ⛔ **旧**估计量 ratio-of-means（已废，只留对照）
            // 🔴 **新**估计量（`A1934` 2026-10-22）：mean-of-ratios —— 逐像素线性亮度比值再平均
            float morLum = nRat > 0 ? (float)(ratLum / nRat) : float.NaN;
            float morR = nRatR > 0 ? (float)(ratR / nRatR) : float.NaN;
            float morG = nRatG > 0 ? (float)(ratG / nRatG) : float.NaN;
            float morB = nRatB > 0 ? (float)(ratB / nRatB) : float.NaN;
            float srgbAll = sAllOff != 0 ? (float)(sAllOn / sAllOff) : float.NaN;
            bool bboxOk = bx1 >= 0;
            float iou = bboxOk ? IouRect(bx0, by0, bx1 + 1, by1 + 1, rx0, ry0, rx1, ry1) : 0f;
            Debug.Log(Q + $"「只留这一层」开/关两帧已存：`cardbase_a1934_textbg_on.png` / `_off.png` "
                        + $"· diff `cardbase_a1934_textbg_diff.png`（×32；像素原点左下）");
            Debug.Log(Q + $"`px` 矩形 x{px.x:F0} y{px.y:F0} w{px.width:F0} h{px.height:F0}（{nIn} px）"
                        + $"｜🔴 **`mean-of-ratios`**（线性亮度，逐像素比值再平均）= **{morLum:F4}**"
                        + $"（R {morR:F4} / G {morG:F4} / B {morB:F4}）"
                        + $"｜参与 {nRat} px · 分母 0 跳过 {nRatSkip} px"
                        + $"｜⛔ **旧估计量 `ratio-of-means`**（线性 R 的 Σon/Σoff，**已废**）= {linIn:F4}"
                        + "（这就是 0.378 那个假象的来源，W61 §3·2）"
                        + $"｜原版字段期望 = 0.463（`1 − 0.647 × 0.830`）"
                        + $"｜sRGB 亮度比 = {srgbIn:F3}｜整帧 sRGB 亮度比 = {srgbAll:F3}");
            Debug.Log(Q + $"|Δ| sRGB 亮度：>1 的像素 **{hit1}** · >8 的 **{hit8}** · 最大 {maxAbs:F1}"
                        + $"｜|Δ|>1 的 bbox = " + (bboxOk ? $"x{bx0}..{bx1} y{by0}..{by1}" : "**空（两帧逐像素相同）**")
                        + $"｜bbox vs `px` 的 IoU = {iou:F2}"
                        + (bboxOk && iou > 0.5f
                           ? " ⇒ **矩形取对了**（差异确实落在记录的那块里）"
                           : bboxOk
                             ? " ⇒ 🔴 **差异不在记录的那块矩形里** ⇒ 先查 `LayerPxRect`"
                             : " ⇒ 🔴 **两帧一个像素都没差** ⇒ 底板真的没画（或两帧渲的是同一状态）"));
            Debug.Log(Q + "怎么读：见本方法头部「怎么读结果」——① 肉眼看 on/off 两帧；"
                        + "② diff 图整块亮 = 底板真画了；③ 现在只认 `mean-of-ratios`（🔴 2026-10-22 起）——"
                        + "它应当贴原版字段的 0.463；`ratio-of-means` 那一栏**已废**，只留作 0.378 假象的对照。");
        }
        finally
        {
            foreach (var rr in reenable) if (rr != null) rr.enabled = true;
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>两个整数矩形（x0,y0,x1,y1，右/上开区间）的交并比。`A1934` 用。</summary>
    static float IouRect(int ax0, int ay0, int ax1, int ay1, int bx0, int by0, int bx1, int by1)
    {
        int ix0 = Mathf.Max(ax0, bx0), iy0 = Mathf.Max(ay0, by0);
        int ix1 = Mathf.Min(ax1, bx1), iy1 = Mathf.Min(ay1, by1);
        int iw = Mathf.Max(0, ix1 - ix0), ih = Mathf.Max(0, iy1 - iy0);
        long inter = (long)iw * ih;
        long union = (long)(ax1 - ax0) * (ay1 - ay0) + (long)(bx1 - bx0) * (by1 - by0) - inter;
        return union > 0 ? (float)inter / union : 0f;
    }

    /// <summary>渲**整帧**并读回全部像素（`Color32[]` 行 0 = 图**下边**，与 `ReadPixels` 同坐标系）。
    /// 与 `Shot` / `RenderPixels` **同一条渲染路径**（同一台相机、同一个 `cam.Render()`、同一种 RT）
    /// —— `A1934` 要的正是「把路 A 存图与路 B 读回之间的实现差异抹掉」。</summary>
    static Color32[] RenderFramePixels(Camera cam, int w, int h)
    {
        cam.aspect = (float)w / h;
        var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        var got = tex.GetPixels32();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        return got;
    }

    /// <summary>把一整帧 `Color32[]` 存成 PNG（`A1934` 用；路径规则与 `Shot` 同一目录/前缀）。</summary>
    static void SavePixelsPng(Color32[] p, int w, int h, string tag)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.SetPixels32(p);
        tex.Apply();
        File.WriteAllBytes($"{OutDir}/cardbase_{tag}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>在卡的子树里按**节点名**找一层 `MeshRenderer`（`textBg` / `costBg` / `armourIcon` / `gem` …）
    /// —— 与下面 `LayerTmp` 同一套扫法（`includeInactive: true`：关着的那层也要找得到）。</summary>
    static MeshRenderer LayerMesh(Transform card, string layerName)
    {
        foreach (var r in card.GetComponentsInChildren<MeshRenderer>(true))
            if (r.name == layerName) return r;
        return null;
    }

    /// <summary>一层 quad 的**中心 y**（卡单位：0 = **`2DCard/Front` 原点**、向上为正）。
    /// 网格顶点烘在层自己的局部系里（`CardView.AddLayer` 只给 z 偏移、不给旋转缩放）⇒ 先换到
    /// **`Front` 坐标系**，再按 `PlaceAt` 那套量纲折成卡单位（1 卡单位 = `Height / CardUnitH` 个局部单位，
    /// 今天 = 1 —— `Width`/`Height` 与 `CardUnitW`/`CardUnitH` 是同一对字面量）。
    /// ⚠️ 用 `InverseTransformPoint`、⛔ 不读 `localPosition`：位置**烘在网格顶点里**、不在 `transform` 上，
    ///    而且卡根自己带缩放与扇形角。
    /// 🔴 **2026-10-25（`M2`）**：标架从**卡根**改成 **`FaceContentRoot`（= `2DCard/Front`）** ——
    ///    卡面各层现在挂 `Front`（`+0.08`，见 `CardView.FrontAposY`），而本文件里所有期望值
    ///    （`BigY` / `SmallY` / 卡名墨心那一族）**本来就是 `Front` 局部值** ⇒ 量在卡根空间会整体多 0.08、全部假红。</summary>
    static float LayerCenterY(CardView card, MeshRenderer r)
    {
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return float.NaN;
        Vector3 w = r.transform.TransformPoint(mf.sharedMesh.bounds.center);
        return card.FaceContentRoot.InverseTransformPoint(w).y * (CardView.CardUnitH / CardView.Height);
    }

    /// <summary>一层 quad 的**尺寸**（卡单位）。量的是网格顶点 —— 那才是真正被画出来的矩形。</summary>
    static Vector2 LayerSizeCardUnits(CardView card, MeshRenderer r)
    {
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return Vector2.zero;
        var m = mf.sharedMesh;
        Vector3 a = card.transform.InverseTransformPoint(r.transform.TransformPoint(m.bounds.min));
        Vector3 b = card.transform.InverseTransformPoint(r.transform.TransformPoint(m.bounds.max));
        return new Vector2(Mathf.Abs(b.x - a.x) * (CardView.CardUnitW / CardView.Width),
                           Mathf.Abs(b.y - a.y) * (CardView.CardUnitH / CardView.Height));
    }

    /// <summary>一组标量**去重 + 升序**（容差 `tol`）。
    /// 用途：九宫格（`Image.Type = Sliced`）的**切线**就藏在网格的这套坐标里 ——
    /// 每多一条切线就多一组顶点，去重之后数个数就能判「切了几刀」。🆕 2026-10-14（账 `A1933`）。</summary>
    static float[] DistinctSorted(List<float> vals, float tol)
    {
        var a = new List<float>();
        for (int i = 0; i < vals.Count; i++)
        {
            bool dup = false;
            for (int j = 0; j < a.Count; j++) if (Mathf.Abs(a[j] - vals[i]) < tol) { dup = true; break; }
            if (!dup) a.Add(vals[i]);
        }
        a.Sort();
        return a.ToArray();
    }

    /// <summary>一层（`MeshRenderer`）网格里**去重升序**的坐标（卡单位）。`xAxis` = 取 x、否则取 y。
    /// 🔴 **量的是【网格自己的局部系】**（⛔ 不走 `TransformPoint`/`InverseTransformPoint`）——
    /// 网格顶点是照 `CardView.SpriteQuad`/`TextBgQuad` 那套「卡单位 × (Width/CardUnitW)」烘进去的，
    /// 所以乘回 `CardUnitW/Width` 就是卡单位。这么量有两个好处：
    /// ① **免疫 `2DCard` 的倾摆**（那一层会在 `Update` 里按位移自转 —— 走变换链的话 x/y 会互相混，
    ///    「数几条切线」这件事就废了）；② 与卡根的缩放/扇形角无关。
    /// ⚠️ 结论上它与 `LayerSizeCardUnits` 只在**卡根有旋转**时才不同（那时后者量的是 AABB）。🆕 2026-10-14（账 `A1933`）。</summary>
    static float[] LayerGridLinesCardUnits(MeshRenderer r, bool xAxis)
    {
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return new float[0];
        float k = xAxis ? CardView.CardUnitW / CardView.Width : CardView.CardUnitH / CardView.Height;
        var vals = new List<float>();
        foreach (var v in mf.sharedMesh.vertices) vals.Add((xAxis ? v.x : v.y) * k);
        return DistinctSorted(vals, 1e-4f);
    }

    /// <summary>一层 TMP 的**字墨中心 y**（卡单位）。
    ///
    /// <para>🔴 **2026-10-22（`W62/T3`）就地改口径：原来量的是「行盒心」，那是错的。**
    ///   旧实现是 `localPosition + textBounds.center`，而 `textBounds` 给的是 **TMP 的排版框**（行盒）。
    ///   卡名那一层是 **`Midline`**（`VerticalAlignmentOptions.Geometry` = 4096），它的定义是把
    ///   **字墨**的中点摆到节点中心 —— 判据：
    ///   · 节点：`bundle_battleprefabs_vfxandmisc_assets_all/RectTransform/RectTransform_4173703600236829632.json`
    ///     （`m_AnchoredPosition.y = 0.5009999871`、`m_LocalScale 0.01`、父级 −0.7745 ⇒ 节点中心 **−0.2735**）；
    ///   · 对齐：同包 `MonoBehaviour_-6860425428091954240.json` 的 **`m_VerticalAlignment = 4096`**；
    ///   · 语义：`TMP_Text.cs`/`TextMeshPro.cs:4223-4225` —— `Geometry` 分支按
    ///     `m_meshExtents` 的 `(max.y + min.y) / 2` 对齐，也就是**网格（字墨）中点**。
    ///   ⇒ 行盒心与墨心差一个 **`c_G`**，而 **`c_G ∝ 字号**：字号一变大行心就跟着偏。
    ///   2026-10-21 的 CJK `m_Scale` 1→0.9 就是把本条偏差从 **0.0181** 顶到 **0.0201** 的
    ///   （比值 1.1105 ≈ 1/0.9）—— 那是**口径错**才需要 0.02 的容差，也正是这条当年没抓住字号回归的原因。
    ///   现在量真墨心（`CardFaceProbe.InkSpanY`，与 `A848` 那条同源、全仓只此一份实现），容差收到 **0.005**。</para>
    /// ⚠️ `textInfo.meshInfo` 的顶点在 TMP 自己的局部系里，TMP 相对父级没有缩放/旋转 ⇒ 可直接相加。
    /// <returns>量不到可见字形时回 `NaN`（调用方拿它判「没验到」，⛔ 别当成通过）。</returns></summary>
    static float TmpInkCenterY(CardView card, TMPro.TextMeshPro t)
    {
        if (t == null) return float.NaN;
        t.ForceMeshUpdate();
        float lo, hi;
        if (!CardFaceProbe.InkSpanY(t, out lo, out hi)) return float.NaN;
        var parent = t.rectTransform.parent;
        Vector3 local = t.rectTransform.localPosition + new Vector3(0f, (lo + hi) * 0.5f, 0f);
        Vector3 w = parent != null ? parent.TransformPoint(local) : local;
        // 🔴 2026-10-25（`M2`）：与 `LayerCenterY` 同一个标架（`2DCard/Front`）—— 本文件里
        //    卡名/效果文字的期望 y 全是 `Front` 局部值。
        return card.FaceContentRoot.InverseTransformPoint(w).y * (CardView.CardUnitH / CardView.Height);
    }

    /// <summary>一层 quad 在**屏幕像素**里的外接矩形（原点左下，与 `ReadPixels` 同一套）。
    /// 用网格顶点算，⛔ 不用 `rectTransform.rect` —— 顶点才是真正被画出来的那四角。
    ///
    /// 🔴 **2026-10-14（第二十九会话 · 账 `A1922`）：这里原先是 `cam.WorldToScreenPoint` —— 错的。**
    ///   `WorldToScreenPoint` 返回的是**相机【当前目标】的像素空间**：没挂 `targetTexture` 时用的是
    ///   **Game View 的尺寸**（批处理下 = **640×480**），而调用方 `RenderPixels` 是按 **1920×1080** 渲的
    ///   ⇒ 量出来的矩形**只有真值的 1/3 宽、1/2.25 高**（实测把这个函数的输出
    ///   `x288 y248 w64 h74` 分别 ×3 / ×2.25 = `x864 y558 w192 h166`，与实图量到的
    ///   `x≈864..1063`、列剖面台阶 112.0→69.8 / 53.5→87.9 **逐位吻合**）
    ///   ⇒ **两帧取的是画面上一块无关的空地** ⇒ `lumOn == lumOff` ⇒
    ///   **③④ 那两条断言报了「文字底板一个像素都没画」的假红 —— 那一层其实画得好好的**
    ///   （同一次探针存的 `cardbase_probe_textbg_only.png` 里能**直接看到**那块半透明矩形：
    ///   矩形内均值 63.6、左右两侧外 104 / 106）。
    ///   ⇒ 改用 **`WorldToViewportPoint`（与相机尺寸/目标无关，恒 0..1）× 目标分辨率**。
    /// ⚠️ 同一个坑本仓别处早记过 —— `Editor/BattleScene.cs`：
    ///   「屏幕坐标必须**在 `targetTexture` 挂着的时候**算」。那个做法（挂 RT 再算）也行，
    ///   但这里**不用挂 RT**（viewport 那条路更省，也不会和 `RenderPixels` 抢 RT）。</summary>
    static Rect LayerPxRect(Camera cam, MeshRenderer r, int w, int h)
    {
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return new Rect(0f, 0f, 0f, 0f);
        float x1 = float.MaxValue, y1 = float.MaxValue, x2 = float.MinValue, y2 = float.MinValue;
        foreach (var v in mf.sharedMesh.vertices)
        {
            Vector3 vp = cam.WorldToViewportPoint(r.transform.TransformPoint(v));
            float sx = vp.x * w, sy = vp.y * h;
            x1 = Mathf.Min(x1, sx); x2 = Mathf.Max(x2, sx);
            y1 = Mathf.Min(y1, sy); y2 = Mathf.Max(y2, sy);
        }
        return new Rect(x1, y1, x2 - x1, y2 - y1);
    }

    /// <summary>渲一帧、只把那块像素读回来（`px` 原点在**左下**）。
    /// ⚠️ 与 `Shot` 同一条：`cam.aspect` 必须**在 `Render` 之前**设死。</summary>
    static Color32[] RenderPixels(Camera cam, int w, int h, Rect px)
    {
        cam.aspect = (float)w / h;
        int rw = Mathf.Max(1, Mathf.RoundToInt(px.width));
        int rh = Mathf.Max(1, Mathf.RoundToInt(px.height));
        int rx = Mathf.Clamp(Mathf.RoundToInt(px.x), 0, Mathf.Max(0, w - rw));
        int ry = Mathf.Clamp(Mathf.RoundToInt(px.y), 0, Mathf.Max(0, h - rh));
        var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();
        var tex = new Texture2D(rw, rh, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(rx, ry, rw, rh), 0, 0);
        tex.Apply();
        var got = tex.GetPixels32();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(tex);
        return got;
    }

    /// <summary>平均亮度（Rec.709 权重，0..255）。</summary>
    static float MeanLum(Color32[] p)
    {
        if (p == null || p.Length == 0) return 0f;
        double s = 0;
        for (int i = 0; i < p.Length; i++) s += 0.2126 * p[i].r + 0.7152 * p[i].g + 0.0722 * p[i].b;
        return (float)(s / p.Length);
    }

    /// <summary>平均亮度，但先按 **sRGB → 线性** 转换（0..1）。🆕 2026-10-14（账 `A1922`）。
    /// 🔴 **为什么必须有它**：本工程是 **Linear** 色彩空间（`ProjectSettings.asset` 的
    ///   `m_ActiveColorSpace = 1`）⇒ **混合是在线性空间发生的**，而 `Color32` 里存的是 sRGB 编码值。
    ///   拿 sRGB 读数直接比「原版 `m_Color.a = 0.647` 该掉多少」会**系统性偏小**
    ///   （实测同一块矩形：sRGB 净掉 22.8% ↔ 线性透过率 0.432 = `1 − 0.647 × 0.878` 逐位吻合）。
    /// ⚠️ 只用于**比值**（开/关两帧之比）—— 比值里那个「背景有多亮」约得掉，所以它是对的尺子。
    /// 判据 → `资料/普查产出_第二十九会话/A1922_文字底板_诊断.md` §三。</summary>
    static float MeanLumLinear(Color32[] p)
    {
        if (p == null || p.Length == 0) return 0f;
        double s = 0;
        for (int i = 0; i < p.Length; i++)
            s += 0.2126 * SrgbToLinear(p[i].r) + 0.7152 * SrgbToLinear(p[i].g) + 0.0722 * SrgbToLinear(p[i].b);
        return (float)(s / p.Length);
    }

    // ==================================================================
    //  🔴 2026-10-22（`A1934` 收口）：**透过率的两个估计量**
    //
    //  **背景**：量「文字底板压暗了多少」有两条路给过三个数（0.378 / 0.906 / 0.43），
    //  查下来**不是画面不对，是估计量不对** —— 判据全文 →
    //  `资料/普查产出_1021/W61_卡牌线_T07尾巴.md` §3·2。
    //   · `MeanOfRatios`（**现行**）= `mean(on_i / off_i)`：逐像素求透过率再平均。
    //     它是「这块 quad 上的平均 alpha」的直接估计，**背景不均匀也不偏**。
    //   · `RatioOfMeans`（**已废**，只留日志对照 + 防回归样本的反例）= `Σon / Σoff`：
    //     亮度加权 ⇒ 透过率高（alpha 低）而背景更暗的那些像素被系统性压低权重。
    //     实测同一帧：底板是 Sliced 九宫格，羽化带落在更暗的背景上 ⇒ 它读 **0.380**，
    //     而逐像素比值读 **0.461**（原版字段 0.463）⇒ **差 0.08，足以让一条断言红/绿反着报**。
    //
    //  ⚠️ **两种估计量都必须在【线性】空间算**：本工程是 Linear 色彩空间，混合在线性空间发生
    //  （详见 `MeanLumLinear` 的注释）。在 sRGB 读数上求比值会系统性偏小。
    //
    //  ⚠️ **`off_i == 0`（背景纯黑）**：该像素**无法**求比值 ⇒ **跳过**（不参与平均），并**计数**报出。
    //  ⛔ 不许「记 0」—— 记 0 等于假装那块被压成全黑，会把均值往小拉（这是发明，不是测量）。
    //  与原有口径一致：`ProbeTextBgDiff` 原来在分母为 0 时返回 `NaN`（= 无法测量），不是 0。
    // ==================================================================

    /// <summary>一个像素的**线性** Rec.709 亮度（0..1）—— `MeanLumLinear` 的单像素版
    /// （逐像素求比值要用它：`MeanLumLinear` 是**整块平均**，拆不开）。</summary>
    static double LinLum(Color32 c)
        => 0.2126 * SrgbToLinear(c.r) + 0.7152 * SrgbToLinear(c.g) + 0.0722 * SrgbToLinear(c.b);

    /// <summary>🔴 **`mean-of-ratios`**（`A1934` 现行估计量）：逐像素 `on_i / off_i` 再取平均。
    /// `off_i &lt;= 0` 的像素**跳过**并计入 `skipped`（见上面那段注释）；
    /// 全部跳过（或数组非法）⇒ `NaN`（= 无法测量，⛔ 不是 0）。
    /// 纯 `double[]` 入参 ⇒ ④ 的 ⑦ 号断言可以用**解析值已知**的合成样本把它钉死。</summary>
    static float MeanOfRatios(double[] on, double[] off, out int used, out int skipped)
    {
        used = 0; skipped = 0;
        if (on == null || off == null || on.Length != off.Length || on.Length == 0) return float.NaN;
        double s = 0;
        for (int i = 0; i < on.Length; i++)
        {
            if (off[i] <= 0.0) { skipped++; continue; }
            s += on[i] / off[i];
            used++;
        }
        return used > 0 ? (float)(s / used) : float.NaN;
    }

    /// <summary>⛔ **`ratio-of-means`（已废的旧估计量）**：`Σon / Σoff`。
    /// 只留两处用途：④ 的日志**对照**、⑦ 号防回归断言里的**反例**。⛔ 不要再拿它当判据。</summary>
    static float RatioOfMeans(double[] on, double[] off)
    {
        if (on == null || off == null || on.Length != off.Length || on.Length == 0) return float.NaN;
        double a = 0, b = 0;
        for (int i = 0; i < on.Length; i++) { a += on[i]; b += off[i]; }
        return b > 0.0 ? (float)(a / b) : float.NaN;
    }

    /// <summary>两帧像素 → **逐像素线性亮度透过率再平均**（= `mean-of-ratios`，`A1934` 2026-10-22 起的唯一估计量）。
    /// ④ / ⑥ / `ProbeTextBgDiff` 都走它（或与之同口径的累加），⛔ 别再出现「先各自平均再相除」。</summary>
    static float MeanThruLinear(Color32[] onPx, Color32[] offPx, out int used, out int skipped)
    {
        used = 0; skipped = 0;
        if (onPx == null || offPx == null || onPx.Length != offPx.Length || onPx.Length == 0) return float.NaN;
        int n = onPx.Length;
        var on = new double[n];
        var off = new double[n];
        for (int i = 0; i < n; i++) { on[i] = LinLum(onPx[i]); off[i] = LinLum(offPx[i]); }
        return MeanOfRatios(on, off, out used, out skipped);
    }

    /// <summary>同 `MeanThruLinear`，但**只取 `[rowFrom, rowTo)` 这几行**（`row 0` = 图**下边**，
    /// 与 `ReadPixels` / `GetPixels32` 同坐标系）。
    /// 🆕 2026-10-22（`W64`）：`A1934` 的 `④·核心` 要用它 —— 文字底板**下 55%** 是 Sliced 的
    /// 拉伸核心段（贴图 alpha ≈ 1），单独量它才能看出「这一帧有没有被别的东西盖住」。
    /// ⚠️ `w` = 一行的像素数；`px.Length` 必须 = `w × h`。</summary>
    static float MeanThruLinearRows(Color32[] onPx, Color32[] offPx, int w, int rowFrom, int rowTo,
                                    out int used, out int skipped)
    {
        used = 0; skipped = 0;
        if (onPx == null || offPx == null || w <= 0 || onPx.Length != offPx.Length) return float.NaN;
        int h = onPx.Length / w;
        if (rowFrom < 0) rowFrom = 0;
        if (rowTo > h) rowTo = h;
        if (rowTo <= rowFrom) return float.NaN;
        int n = (rowTo - rowFrom) * w, k = 0;
        var on = new double[n];
        var off = new double[n];
        for (int y = rowFrom; y < rowTo; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                on[k] = LinLum(onPx[i]); off[k] = LinLum(offPx[i]); k++;
            }
        return MeanOfRatios(on, off, out used, out skipped);
    }

    /// <summary>一个 sRGB 分量（0..255）→ 线性（0..1）—— 照 sRGB 规范的那两条分段式。
    /// ⚠️ 用 `Mathf.Pow`（不是 `System.Math`）—— 本文件没有 `using System;`，别为一处指数去动 using。</summary>
    static float SrgbToLinear(byte v)
    {
        float c = v / 255f;
        return c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
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
            // ⚠️ **这个字面量是【入参】、不是期望值从卡表里读**（2026-10-14 `A1801` 现核实）：
            //    `CardText.Name(id, nameZh)` 在中文档下**原样返回传进来的 `nameZh`**
            //    （`Core/CardText.cs`）⇒ 这两条断的是「闸通不过」，**换卡名不会让它红/绿**。
            //    但它的值假装是「卡表那一列」，故跟着卡表一起换成官方值：
            //    官方 `Card_Name/TAU46` = `["Sense of Stone","石之感"]`（原值「石之感知」是我们自译）。
            string zhName = CardText.Name("TAU46", "石之感");
            Check(zhName == "石之感", $"① 中文档卡名走卡表那一列（实得「{zhName}」）");

            // ---- ② 英文档：**卡名必须回英文** ----
            Loc.SetLanguage(AvailableLanguages.English);
            Check(!CardText.Zh,
                  "② 英文档下 `CardText.Zh` = **false** —— 🧨 把 `Zh` 改回只判 `TmpFont.Available` ⇒ 本条红");

            // ---- ③ 绕过闸的那一处：`Name(id, nameZh)` 原来无条件返回中文名 ----
            string enName = CardText.Name("TAU46", "石之感");
            // 🔴 **2026-10-18（N71 第二半）就地订正**：原来这条钉的是「英文档 ⇒ 回**裸 id** `TAU46`」——
            //   那是 `TermOr` 缺 Table 键时的**旧兜底**。`N71` 让 `TermOr` 在中/英档也吃官方全表之后，
            //   官方 `Card_Name/TAU46` = `["Sense of Stone","石之感"]` ⇒ 英文档应当印 **Sense of Stone**。
            //   ⛔ 别再改回 `"TAU46"`：那是把「查不到官方名」当成期望值。
            Check(enName == "Sense of Stone",
                  $"③ ★ 英文档卡名回**官方英文名**（实得「{enName}」）—— 官方 `Card_Name/TAU46` = Sense of Stone；"
                  + "🧨 删掉 `TermOr` 里那条 `Loc.OfficialCurrent` 兜底 ⇒ 本条红");
            Check(enName != "TAU46",
                  "③ 判别式：**不许退回裸 id**（防「两边都回 id」也照样过）");
            Check(enName != zhName,
                  "③ 判别式：两档的卡名**不是同一串**（防「两边都返回 id」也照样过）");
        }
        finally { Loc.RestoreForTest(langBack); Loc.PersistOverride = false; }
    }

    /// <summary>🔴 **2026-10-10（`A1084`）：`Core/CardText.cs` 那几族中文表【已搬进 `Core/Loc.cs`】**
    /// （`Card_Name/*` · `Card_Trait/*` · `Armies/*` · `Battle/Phrase/*` · `CardEffect/*`）。
    /// 本块断七件，**每一件的期望值都是字面量**（⛔ 一律不拿 `CardText.X()` / `Loc.T()` 当期望 ——
    /// 那等于拿实现证明实现，两边一起改照样绿）。
    ///
    /// 🧨 **改坏法（哪一条会红）**：
    ///   · 某条词条的**中文列被写成英文**（= 搬了个空壳）⇒ ③ / ④ 红；
    ///   · 新加一张起始卡而不建 `Card_Name/&lt;名字&gt;` ⇒ ② 红；
    ///   · **`AllChinese()` 不再覆盖这几族**（语料漏 ⇒ 字体烘字漏字、**而且不报错**）⇒ ⑥ 红
    ///     —— 这是搬表这件事**唯一会静默失败**的那一格（普查 §5.3·1）；
    ///   · 把 `Battle/Phrase/MELEE` 改成复用近邻 `Battle/Tips/MeleeAttack` ⇒ ⑧ 红（语义不同，⛔ 不许复用）；
    ///   · 把 `Armies/Goff` 的英文列从 `Orks` 改成 `Goff`（「照键名直译」）⇒ ④ 红；
    ///   · 删/搬 `KeywordZhAliases`（`装甲`/`爆破`）⇒ ⑦ 红（那 5 张卡会重复印关键词）；
    ///   · 关键词 tooltip 的标题换回 `KeywordZh`（没有语言闸）⇒ ⑤ 红（那正是 `A1086③`）。
    /// </summary>
    static void AssertCardTextTablesInLoc()
    {
        // ---- ① 键族前缀 = 原版 / 自拟的那几个字面量 ----
        Check(CardText.CardNameTermPrefix == "Card_Name/", "① 卡名键前缀 = `Card_Name/`（族名照原版）");
        Check(CardText.TraitTermPrefix == "Card_Trait/",
              "① 关键词键前缀 = `Card_Trait/`（= 原版 `GameStaticData.TraitNameToString` 的拼法）");
        Check(CardText.ArmyTermPrefix == "Armies/", "① 阵营键前缀 = `Armies/`（= 原版那 14 颗 `Localize` 的 mTerm 前缀）");
        Check(CardText.PhraseTermPrefix == "Battle/Phrase/", "① 短语族前缀 = 我们自拟的 `Battle/Phrase/`");

        // ---- ② 逐族齐全：起始卡**一张都不能漏**、16 个阵营一个都不能少 ----
        int missName = 0; string missWho = "";
        foreach (var f in new[] { RuleEngine.StarterCards.EmberFaction, RuleEngine.StarterCards.TideFaction })
            foreach (var c in RuleEngine.StarterCards.Of(f))
                if (!Loc.HasEntry(CardText.CardNameTermPrefix + c.Name))
                { missName++; if (missWho.Length < 80) missWho += c.Name + " "; }
        Check(missName == 0, $"② 起始卡每张都有 `Card_Name/<名字>` 词条（缺 {missName} 张：{missWho}）"
                           + " —— 🧨 新加一张卡而不建键 ⇒ 本条红（**这条路是静默回退**，不建键不会报错）");

        string[] armies = { "Ember","Tide","Neutral","Ultramarines","Goff","SaimHann","Sautekh","BlackLegion",
                            "Leviathan","TauEmpire","Sororitas","Genestealers","AstraMilitarum","DarkAngels",
                            "EmperorsChildren","SpaceWolves" };
        int missA = 0;
        foreach (var a in armies) if (!Loc.HasEntry(CardText.ArmyTermPrefix + a)) missA++;
        Check(missA == 0, $"② {armies.Length} 个阵营全有词条（缺 {missA} 个）");

        // ---- ③ / ④ 两档的值（**期望值 = 字面量**，抄自搬表之前那几张表 / 成品卡图）----
        var langBack = Loc.Current;
        Loc.PersistOverride = true;                       // 自检不许动玩家的真设置
        try
        {
            Check(TmpFont.Available, "★ 前提：中文字体资产在（不在 ⇒ 下面中文档那几条会退化成假绿）");
            Loc.RestoreForTest(AvailableLanguages.Chinese);
            Check(CardText.Zh, "★ 前提：中文档下 `CardText.Zh` = true");

            // ③ 中文档
            Check(CardText.Name("Ember Warlord") == "余烬督军",
                  $"③ 卡名 `Ember Warlord` → `余烬督军`（实得「{CardText.Name("Ember Warlord")}」）");
            // 🔴 2026-10-23（`C4`）就地改期望值：`waystone` 路标石→**灵魂石** · `armour` 护甲→**盔甲**
            //   —— `Core/Loc.cs` 那 42 条已换成官方值（判据 = `数据/游戏数据/官方本地化_en+zh-CN.json`
            //   的 `Card_Trait/{waystone,armour}`；出处 = `资料/普查产出_1023c/官方中文全量收口_执行报告.md` §一·①）。
            //   ⚠️ 只改期望值字面量，**没有放宽/合并任何一条断言**。
            Check(CardText.KeywordZh("vanguard") == "先锋" && CardText.KeywordZh("waystone") == "灵魂石",
                  "③ 关键词 `vanguard` / `waystone` → `先锋` / `灵魂石`（后者改前**只在那 62 条表里**、"
                + "12 条那张里没有 ⇒ 合并只许合并、⛔ 不许合并时丢词）");
            Check(CardText.KeywordZh("armour") == "盔甲", "③ `armour` → `盔甲`（⛔ 不是别名 `装甲` / 旧译 `护甲`）");
            Check(CardText.Keyword("armour") == "盔甲", "③ `Keyword(\"armour\")` 仍 → `盔甲`（带数值那一格由调用方拼）");
    // 🔴 2026-10-14（第二十七会话）就地改期望值：Armies/Goff 原来是我们自译的「高夫兽人」，官方 zh-CN = 「欧克蛮人」
        Check(CardText.Faction("Goff") == "欧克蛮人", "③ 阵营 `Goff` → `欧克蛮人`");
            Check(CardText.Phrase("GAME OVER") == "对局结束" && CardText.Phrase("YOU LOSE") == "你输了",
                  "③ HUD 短语 `GAME OVER` / `YOU LOSE` → `对局结束` / `你输了`");
            Check(CardText.TurnLabel(3) == "第 3 回合",
                  $"③ `TurnLabel(3)` → `第 3 回合`（实得「{CardText.TurnLabel(3)}」）");
            var sp = RuleEngine.EffectSpec.Parse("Damage 2 EnemyUnit");
            Check(sp != null && CardText.Effect(sp) == "伤害2·敌方单位",
                  $"③ 卡面效果小字 → `伤害2·敌方单位`（实得「{CardText.Effect(sp)}」）");
            Check(sp != null && CardText.EffectSentence(sp) == "对敌方单位造成 2 点伤害",
                  $"③ 技能卡面板整句 → `对敌方单位造成 2 点伤害`（实得「{CardText.EffectSentence(sp)}」）");
            var hp = RuleEngine.EffectSpec.Parse("Heal 3 Self");
            Check(hp != null && CardText.EffectSentence(hp) == "为自身回复 3 点生命",
                  $"③ …治疗那一档的**语序**（中文「为{0}回复」）→ 实得「{CardText.EffectSentence(hp)}」");

            // ④ 英文档（值**全部是字面量**）
            Loc.RestoreForTest(AvailableLanguages.English);
            Check(!CardText.Zh, "★ 前提：英文档 `CardText.Zh` = false");
            Check(CardText.Name("Ember Warlord") == "Ember Warlord", "④ 英文档卡名回英文 id");
            Check(CardText.Faction("Goff") == "Orks",
                  $"④ 英文档阵营 = **成品卡上印的那个** `Orks`（⛔ 不是键名 `Goff`；实得「{CardText.Faction("Goff")}」）");
            Check(CardText.Keyword("armour") == "ARMOUR",
                  $"④ 英文档 `Keyword` 仍回大写英文（**行为逐字照改前**；实得「{CardText.Keyword("armour")}」）");
            Check(CardText.KeywordDisplay("armour") == "Armour",
                  $"④ 英文档 `KeywordDisplay` = 英文列（表里那份 `Armour`；实得「{CardText.KeywordDisplay("armour")}」）");
            Check(CardText.KeywordZh("armour") == "盔甲",     // 🔴 2026-10-23（`C4`）期望值 护甲→盔甲（同 ③，官方值）
                  "④ **判别式**：`KeywordZh` **恒中文**（`KeywordSegment` 在中文档也拿它比「印过没有」）⇒ 英文档下不变");
            Check(CardText.Phrase("YOU WIN") == "YOU WIN", "④ 英文档短语**照原样回英文原文**（本笔没改这条路的显示）");
            Check(sp != null && CardText.EffectSentence(sp) == "Deal 2 damage to an enemy unit",
                  $"④ 英文档整句与改前逐字相同（实得「{CardText.EffectSentence(sp)}」）");
            Check(hp != null && CardText.EffectSentence(hp) == "Restore 3 health to self",
                  $"④ …治疗那一档（实得「{CardText.EffectSentence(hp)}」）");
            Check(CardText.TurnLabel(3) == "TURN 3",
                  $"④ 英文档 `TurnLabel(3)` = `TURN 3`（`Term` 那道语档闸在管；实得「{CardText.TurnLabel(3)}」）");

            // ---- ⑤ 关键词 tooltip 的**标题跟语档**（原 `A1086③` 的缺陷）----
            Loc.RestoreForTest(AvailableLanguages.Chinese);
            string tipZh = TipText.Trait("armour");
            // 🔴 2026-10-23（`C4`）期望值 护甲（Armour）→ **盔甲（Armour）**：标题 = `KeywordDisplay` 的中文列
            //   + 全角括号；中文列已换成官方 `Card_Trait/armour` = 盔甲。
            Check(tipZh != null && TitleOf(tipZh).Contains("盔甲（Armour）"),
                  $"⑤ 中文档关键词 tooltip 的标题 = `盔甲（Armour）`（**全角括号**，逐字同改前）"
                + $"—— 标题段「{TitleOf(tipZh)}」");
            Loc.RestoreForTest(AvailableLanguages.English);
            string tipEn = TipText.Trait("armour");
            Check(tipEn != null && TitleOf(tipEn).Contains("Armour") && !TitleOf(tipEn).Contains("护甲")
                  && !TitleOf(tipEn).Contains("（"),
                  $"⑤ 英文档标题**不再印中文、也不再用全角括号**（标题段「{TitleOf(tipEn)}」）"
                + " —— 🧨 把 `KeywordDisplay` 换回 `KeywordZh` + 拼全角括号 ⇒ 本条红（那正是 `A1086③`）");
        }
        finally { Loc.RestoreForTest(langBack); Loc.PersistOverride = false; }

        // ---- ⑥ 语料（`AllChinese`）**跟着搬了** —— 搬表唯一会静默失败的那一格 ----
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in CardText.AllChinese()) sb.Append(s);
            string all = sb.ToString();
            // 🔴 2026-10-23（`C4`）`路标石`→`灵魂石` · `护甲`→`盔甲`（语料 = `Loc` 中文列，已随官方值一起换）
            string[] must = { "余烬督军", "灵魂石", "欧克蛮人", "对局结束", "对{1}造成 {0} 点伤害", "盔甲", "眩晕" };
            int no = 0; string wh = "";
            foreach (var m in must)
                if (all.IndexOf(m, System.StringComparison.Ordinal) < 0) { no++; wh += "「" + m + "」"; }
            Check(no == 0, $"⑥ 字体语料（`CardText.AllChinese()`）**覆盖搬迁后的每一族**（缺 {no} 条：{wh}）"
                         + " —— 🧨 把 `AllChinese()` 改成只收某一族 ⇒ 本条红"
                         + "（漏字的后果**不是报错**，而是覆盖自检静默失效 ⇒ 卡面可能印方块）");
            int n = 0; foreach (var _ in CardText.AllChinese()) n++;
            Check(n >= 500, $"⑥ 语料条数 ≥ 500（实测 {n}）—— 搬表**之前不到 460 条**（那时只收 12 个关键词名）"
                          + "；⛔ 别把「条数掉回去」当成正常");
        }

        // ---- ⑦ `KeywordZhAliases`（装甲 / 爆破）**没被搬走**：它不上屏、只参与去重比对 ----
        Check(CardText.KeywordSegment(new System.Collections.Generic.Dictionary<string, int> { { "armour", 1 } },
                                      true, "装甲 2") == "",
              "⑦ `body` 里写的是**别名** `装甲` ⇒ 卡面**不再补**那一段"
            + " —— 🧨 删掉/搬走 `KeywordZhAliases` ⇒ 本条红（那 5 张卡会**重复印一遍关键词**）");
        Check(CardText.KeywordSegment(new System.Collections.Generic.Dictionary<string, int> { { "blast", 2 } },
                                      true, "爆破 2") == "",
              "⑦ 同上，`blast` 的别名 `爆破`（5 张卡 `AM13/AM28/AM34/AM37/AM54` 的翻车点）");

        // ---- ⑨ 🆕 2026-10-15（第三十二会话）：声明行紧跟 `<br>` 也要判成「已声明」 ----
        //   来由：官方那代文本用 `<br>` 断行，而 `IsDeclarationAt` 的「段首」那一支
        //   只看**紧邻字符**是不是句读符 ⇒ `<br>` 的 `>` 判不出来 ⇒ 词被再补一遍。
        //   实测：`ASH34 Avatar of Khaine` 卡面多印一个 `天赋。`（PnP 成品卡只印一遍）。
        //   🔴 2026-10-23（`C4`）改**测试夹具**里的词：`talent` 的词条已换成官方 **才能**（原 天赋）
        //     ⇒ 夹具正文跟着写 `才能：…`，否则这条**靠 `KeywordZhAliases` 的旧词别名才变绿**
        //     （= 测不到「`<br>` 后算段首」那一支，断言形同放水）。⚠️ 另外三条 `护甲 2。` 只是**无关前文**，
        //     不参与本条判据（`kwTalent` 里没有 `armour`），故原样保留。
        //     ⚠️ `ASH34` 的**真实** `descZh` 里仍是旧词 `天赋：`（卡面 `descZh` 还没迁到官方）
        //     —— 那一半由 `KeywordZhAliases` 的过渡补丁兜（见 `Core/CardText.cs`），**不是**本条在测的东西。
        var kwTalent = new System.Collections.Generic.Dictionary<string, int> { { "talent", 0 } };
        Check(CardText.KeywordSegment(kwTalent, true, "护甲 2。<br>才能：凯恩之怒。") == "",
              "⑨ 正文里 `才能：凯恩之怒。` 紧跟 `<br>` ⇒ 关键词段**不再补** `才能。`"
            + " —— 🧨 把 `IsDeclarationAt` 的「段首」那支改回**只看句读符** ⇒ 本条红"
            + "（实测 `ASH34 Avatar of Khaine` 卡面会多印一个关键词，而 PnP 只印一遍）");
        Check(CardText.KeywordSegment(kwTalent, true, "护甲 2。\n才能：凯恩之怒。") == "",
              "⑨ **判别式**：`<br>` 与真换行 `\\n` 必须**同效**"
            + " —— 只给 `<br>` 开小灶、或只认 `\\n`，两条必红其一");
        Check(CardText.KeywordSegment(kwTalent, true, "护甲 2。凯恩之怒。") != "",
              "⑨ **判别式（反向）**：正文里**没有**这个词的声明"
            + "（`凯恩之怒` 是效果的宾语、不是声明）⇒ 该补的还得补"
            + " —— 🧨 把 `DeclaredInBody` 改成恒真（例如「出现过就算」）⇒ 本条红");

        // ---- ⑧ **另立键、不复用近邻**（铁律 6：复用会让「改一处文案另一处跟着变」）----
        Check(Loc.ZhOf("Battle/Phrase/Melee") == "近战" && Loc.ZhOf("Battle/Phrase/Ranged") == "远程",
              "⑧ 打法名那两条**另立键**（`Battle/Phrase/{Melee,Ranged}`）");
        Check(Loc.ZhOf("Battle/Tips/MeleeAttack") != Loc.ZhOf("Battle/Phrase/Melee"),
              "⑧ **判别式**：`Battle/Tips/MeleeAttack`（**数值格标签**）与本条**不是同一句**"
            + " —— 🧨 为了省一条键把 `MELEE` 指到 `Battle/Tips/MeleeAttack` ⇒ 本条红");
        Check(Loc.ZhOf("Battle/Phrase/GameOver") != Loc.ZhOf("Battle/BattleEnd/Victory"),
              "⑧ **判别式**：`Battle/Phrase/GameOver`（HUD 回合行）≠ `Battle/BattleEnd/Victory`（结算面板标题）");
    }

    /// <summary>tooltip 第一条 `<b>…&lt;/b&gt;` 那一段（= 图标 + 标题）—— 判「标题跟语档」要**排开正文**
    /// （正文是规则书原文，今天两档都是中文，那是另一笔账）。</summary>
    static string TitleOf(string tip)
    {
        if (string.IsNullOrEmpty(tip)) return "";
        int a = tip.IndexOf("<b>", System.StringComparison.Ordinal);
        int b = tip.IndexOf("</b>", System.StringComparison.Ordinal);
        return (a >= 0 && b > a) ? tip.Substring(a + 3, b - a - 3) : tip;
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
    //  手牌位姿补间 = `CardView.TweenPose`（🆕 2026-10-24 · `A3` · 账 `J3`§六·1 / 新账-82②）
    //
    //  判据（原版一手，不是我们的常量）：
    //    · 位置 `DOMove`（**世界**）· 角度 `DOLocalRotate`（**局部**）· 缩放 `DOScale`（localScale）
    //      —— `d:/2/tools/decomp_full/CardScript__UpdateMinionInHandPosition.c`（逐句亲读）；
    //    · 时长 = `VarsGlobal.timeToPositionCard` = 0.2 s；三条**都不写 `SetEase`** ⇒ 各自吃默认 `OutQuad`；
    //    · 我们的等价物必须把「基础缩放 × 高亮系数」那一层一起带过去（`CardView.T04` 的 doc），
    //      而 `transform.DOScale` 直写会留下「`_baseScale` 变陈」的静默缺陷。
    //
    //  为什么要有这一段：上面那三条**都被旧实现违反过**（`DORotate` 世界角 / `DOScale` 直写 /
    //  `DOTween.To` 建的补间没有 target ⇒ `transform.DOKill()` 杀不掉）。每条的「改坏法」写在断言文案里。
    //  ⚠️ 探针**自建自拆**（不进场景、不进截图）。
    // ==================================================================
    static void AssertHandPoseTween()
    {
        var parent = new GameObject("chk_handpose_parent");
        var made = new List<CardView>();
        try
        {
            // 父件带 **30° 旋转 + 2× 缩放** —— 这正是「世界角」与「局部角」分叉、且局部位 ≠ 世界位的那一档。
            parent.transform.SetPositionAndRotation(new Vector3(0.7f, -0.4f, 0f), Quaternion.Euler(0f, 0f, 30f));
            parent.transform.localScale = Vector3.one * 2f;
            var layout = parent.AddComponent<HandLayout>();
            for (int i = 0; i < 3; i++)
                made.Add(CardView.Create(parent.transform, CardData.Placeholder(i + 1),
                                         "chk_handpose_" + i, CardFace.Hand));

            var c0 = made[0];
            int n = made.Count;
            float wantAng = layout.RotationAt(0, n);
            Vector3 wantWorld = layout.SlotPosition(0, n);

            // 先点亮「高亮」这一档：`_hlMul` = 1.05 —— 只有它 ≠ 1 时，「直写 localScale」才会露馅。
            c0.SetHighlight(CardHighlightState.Hover);
            Step(CardHighlight.AnimTime + 0.02f);
            Check(Mathf.Abs(c0.HighlightMul - CardHighlight.ScaleFactor) < 1e-3f,
                  $"前提：高亮系数已经到 {CardHighlight.ScaleFactor:F2}（实测 {c0.HighlightMul:F3}；"
                + "不到的话下面「基础 × 高亮」那条**没验到**）");

            // ① + ②：整条链走一遍（`HandLayout.Place` → `CardView.TweenPose`），推到补间终点
            layout.animateRelayout = true;
            layout.Refresh(made, -1, -1);
            Step(CardTween.RelayoutDuration + 0.05f);

            float gotAng = c0.transform.localEulerAngles.z;
            Check(Mathf.Abs(Mathf.DeltaAngle(gotAng, wantAng)) < 0.5f,
                  $"★ 重排补间写的是【局部】角：要 {wantAng:F2}°，实得**局部** {gotAng:F2}°"
                + $"（父件带 30° ⇒ 世界角那一版会得 {Mathf.DeltaAngle(0f, wantAng - 30f):F2}° 附近）"
                + "｜判据 `CardScript__UpdateMinionInHandPosition.c` 的 `DOLocalRotate`"
                + "｜🧨 改回 `DORotate`（世界欧拉）⇒ 差 30° ⇒ 红");

            Check(Vector3.Distance(c0.transform.position, wantWorld) < 1e-3f,
                  $"★ ……而**世界位置**仍落在槽位 {n}：要 ({wantWorld.x:F3},{wantWorld.y:F3},{wantWorld.z:F3})，"
                + $"实得 ({c0.transform.position.x:F3},{c0.transform.position.y:F3},{c0.transform.position.z:F3})"
                + "（父件带旋转+2× 缩放 ⇒ 直接喂世界坐标会被再变换一次）"
                + "｜🧨 `Place` 里不 `InverseTransformPoint`（把世界位当局部位喂 `SetPose`）⇒ 红");

            float wantBase = layout.cardScale * LayoutSpace.Scale;      // 槽 0、无悬停
            Check(Mathf.Abs(c0.BaseScale - wantBase) < 1e-3f,
                  $"★ 补间收尾后 **`BaseScale`** = 手牌档 {wantBase:F4}（实测 {c0.BaseScale:F4}）"
                + "｜🧨 退回 `transform.DOScale(...)` 直写 `localScale` ⇒ `BaseScale` 停在起点 1 ⇒ 红");

            Check(Mathf.Abs(c0.transform.localScale.x - c0.BaseScale * c0.HighlightMul) < 1e-3f,
                  $"★ ……而且 `localScale` = `BaseScale` × 高亮 = {c0.BaseScale:F4} × {c0.HighlightMul:F3}"
                + $" = {c0.BaseScale * c0.HighlightMul:F4}（实测 {c0.transform.localScale.x:F4}）"
                + "｜🧨 同上（`DOScale` 直写会把高亮那 1.05 抹掉）⇒ 红");

            // ③ 两个「掐补间」的口 —— 都在**半路**掐，然后推到超过总时长，看它会不会被继续推走。
            //    目标故意取一个**离槽位很远**的位姿 ⇒ 「没掐掉」与「掐掉了」终态完全不同（不是同一个点）。
            Vector3 farLocal = new Vector3(3.5f, -2.2f, 0f);
            Vector3 slotLocal = parent.transform.InverseTransformPoint(wantWorld);
            for (int pass = 0; pass < 2; pass++)
            {
                bool byApi = pass == 0;
                // 🔴 **2026-10-24（`E3` · `DG3` 顺手发现 1）：两个 pass 必须各自从【槽位】起。**
                //   原来 pass 1 直接接着 pass 0 的终态起步：pass 0（`KillPoseTween`）以前没掐掉 ⇒
                //   卡已经停在 `farLocal`，于是 ③2 的读数就是「3.500,−2.200 → 3.500,−2.200」——
                //   **掐不掐都绿**，它对 `SetTarget(transform)` 一点牙都没有（空验）。
                //   ⇒ 每轮先 `SetPose` 回槽位，再往 `farLocal` 补间；两个 pass 的起点/终点都不同。
                c0.SetPose(slotLocal, wantAng, wantBase);
                Vector3 startLocal = c0.transform.localPosition;
                float startBase = c0.BaseScale;
                var tw = c0.TweenPose(farLocal, 25f, 0.45f, CardTween.RelayoutDuration, Ease.OutQuad);
                Check(tw != null && tw.IsActive(),
                      $"③{pass + 1} 前提：位姿补间建起来了（`TweenPose` 返回的 tween 活着）");
                if (tw == null) continue;
                Step(CardTween.RelayoutDuration * 0.4f);
                Vector3 atCut = c0.transform.localPosition;
                float baseAtCut = c0.BaseScale;
                Check(Vector3.Distance(atCut, startLocal) > 0.5f && Vector3.Distance(atCut, farLocal) > 0.5f,
                      $"③{pass + 1} 前提：切口处**确实在半路**（局部位 {atCut.x:F3},{atCut.y:F3}，"
                    + $"起点 {startLocal.x:F3},{startLocal.y:F3}、终点 {farLocal.x:F3},{farLocal.y:F3}）"
                    + "｜不足 0.5 ⇒ 补间没在推 / 起点终点撞在一起 ⇒ 下面那条会空验");
                if (byApi) c0.KillPoseTween(); else c0.transform.DOKill();   // 后者靠 `SetTarget(transform)` 才杀得到
                // 🔴 **自证（`DG3` 要求补的那半条）**：先断「这条补间真的死了」—— 下次红时能一眼分清
                //   「闸门空转 / `transform.DOKill()` 杀不到」还是「`Kill()` 本身没牙」。
                Check(!tw.IsActive(),
                      $"③{pass + 1} 掐完之后补间**真的死了**（`tw.IsActive() == false`；实测 {tw.IsActive()}）"
                    + "｜🧨 删掉 `KillPoseTween` 里那句兜底 `DOTween.Kill(_poseTweenId)`（只留 `tw.Kill()`）"
                    + " ⇒ 编辑器批处理里 `DOTween.initialized == false` ⇒ `TweenExtensions.Kill` 直接 return ⇒ 空转 ⇒ 红");
                Step(CardTween.RelayoutDuration * 2f);
                Check(Vector3.Distance(c0.transform.localPosition, atCut) < 1e-4f
                      && Mathf.Abs(c0.BaseScale - baseAtCut) < 1e-4f,
                      $"★ ③{pass + 1} 半路用 `{(byApi ? "CardView.KillPoseTween()" : "transform.DOKill()")}` 掐掉之后，"
                    + $"位姿**不再被推走**（局部位 {atCut.x:F3},{atCut.y:F3} → "
                    + $"{c0.transform.localPosition.x:F3},{c0.transform.localPosition.y:F3}；"
                    + $"`BaseScale` {baseAtCut:F4} → {c0.BaseScale:F4}；起点 {startBase:F4}）"
                    + "｜🧨 `TweenPose` 里不写 `SetTarget(transform)` ⇒ `transform.DOKill()` 杀不到 `DOTween.To`"
                    + " 建的补间（`HandLayout.Place` 的防叠、`MulliganPanel` 掐重排补间都会静默失效）⇒ ③2 红"
                    + "｜🧨 `KillPoseTween` 的兜底（`DOTween.Kill(_poseTweenId)`）被删 / `TweenPose` 里不 `SetId` ⇒ ③1 红"
                    + "（`TweenExtensions.Kill` 在非 play 环境恒空转，见 `KillPoseTween` 的注释）");
            }
        }
        finally
        {
            for (int i = 0; i < made.Count; i++) if (made[i] != null) Object.DestroyImmediate(made[i].gameObject);
            Object.DestroyImmediate(parent);
        }
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

    // ==================================================================
    //  🆕 2026-10-23（战斗线 · **新账-73** 远程收招第二段 / **新账-74** 缩放走唯一口）
    // ==================================================================
    //  判据（第一权威 = 反编译，全部在 `d:/2/tools/decomp_full/`）：
    //   · **段1** `CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c:80-104` =
    //     `DOLocalMove(静止位)` + `DORotateQuaternion(identity)` + `DOScale(DefaultScale)`，时长 `attackStepTime` = 0.1；
    //     之后 `WaitForCompletion` + `WaitForSeconds(0.1)` ⇒ 0.2 s 才发 VFX（= `EventTiming.AttackStepRanged`）。
    //   · **段2** `CardScript__AfterAttack.c`（调用点 `CardScript._ResolveRangedAttack_d__364__MoveNext.c:39`，
    //     在段1 那条协程跑完之后才调）= 同一句 `DOLocalMove(静止位, attackStepTime)` + `SetDelay(0.65)`
    //     + `OnUpdate` = `localScale = MinionScaleByPositionSO.GetScale(...)` + `OnComplete` = `FinishAfterAttack`。
    //     🔴 **0.65 不是我们挑的** —— `GameAssembly.dll` `.rdata 0x1834b315c` 4 字节 float 实读 = 0.65
    //     （同一地址在 `FatigueCard._DrawFatigue_d__7:64-70` 当 0.65 的字面量用，两处互证）。
    //   · **收尾** `CardScript__FinishAfterAttack.c` = `set_localPosition(静止位)` / `set_rotation(identity)` /
    //     `set_localScale(DefaultScale)`。
    //
    //  为什么挂在**本宿主**：它在 `工具/_run_8_checks.sh` 的 12 条里，而且本来就
    //  `CardTween.Mode = Manual` + `Step(dt)` 手动推进（同 4c / 4d 两节的理由）。
    //
    //  🔴 **成对（灭自证）**：
    //    · ④（段2 在按深度重算）⟷ ③（那 0.65 s 里**不**参与）：
    //      「钩子挂整条序列」过得了 ④ 过不了 ③；「段2 忘了挂钩子」过得了 ③ 过不了 ④。
    //    · ②（`BaseScale` 跟上了目标）⟷ 老的 `DOScale` 写法（`localScale` 对、`BaseScale` 陈）⇒ 只有 ② 红。
    //    · ⑧（HUD 层的卡不吃这条曲线）⇒ 摘掉 `IsArenaCard` 那道门时**只有它红**。
    static void AssertRangedRecoverAndBaseScale()
    {
        Debug.Log(P + "--- 远程收招：第二段（新账-73）+ 缩放唯一口（新账-74）---");

        // 曲线两端 = 原版 SO 实读值，**在这里独立重写一遍两键插值**
        // （⛔ 不调 `CardFeel.DepthScaleCurve` —— 拿我们的实现证明我们的实现是自证）
        const float K0T = 0.002811431884765625f, K0V = 0.36000001430511475f;
        const float K1T = 7.699999809265137f,  K1V = 0.699999988079071f;
        float Curve(float x)
        {
            return Mathf.Lerp(K0V, K1V, Mathf.Clamp01((x - K0T) / (K1T - K0T)));
        }

        // 这张卡 = **3D 战场层**（`IsArenaCard` 的门）+ 站在**敌方行**的 z 上 + 按 `isPlayer = true` 算
        // ⇒ 深度系数 = 曲线两端之比（= 「我方卡冲到对面那一行」那一帧）。
        var root = new GameObject("chk_recover");
        var v = CardView.Create(root.transform, CardData.Placeholder(40), "chk_recover_card", CardFace.Board);
        v.gameObject.layer = ArenaSlots.ArenaLayer;
        v.SetPose(new Vector3(0f, 0f, ArenaSlots.EnemyZ), 0f, 0.36f);

        float ratio = Curve(ArenaSlots.EnemyZ - ArenaSlots.PlayerZ) / Curve(0f);
        Check(Mathf.Abs(ratio - 1.9444f) < 1e-3f,
              $"夹具：我方行 → 敌方行的深度系数 = {ratio:F4}（原版曲线 = 0.70 / 0.36）");

        var t = CardFeel.AttackRecover(v.transform, v.transform.localPosition, 0.30f, 0f, isPlayer: true);
        if (t == null)
        {
            Check(false, "夹具：`AttackRecover` 没建出来（下面的条目**没验到**，不是通过）");
            Object.DestroyImmediate(root);
            return;
        }
        float wantDur = CardFeel.AttackStepTime + CardFeel.AfterAttackDelay + CardFeel.AttackStepTime;
        // ⚠️ 这条必须在**任何 Step 之前**读 —— 段1 的缩放补间一开始跑就会把 0.36 往 0.30 拖
        //    （推进 1 ms 就已经差了 1.2e-3，超过下面 1e-3 的容差 ⇒ 会变成假红）。
        Check(Mathf.Abs(v.BaseScale - 0.36f) < 1e-3f, $"夹具：起始基础缩放 0.36（实测 {v.BaseScale:F4}）");

        // ⚠️ 先推进 1 ms 再读 `Duration()` —— DOTween 的 `Sequence` 是**首次推进时**才算总时长的
        //    （在那之前 `_duration` 还没 Validate，读出来是 0，会让下面那条假红）。
        Step(0.001f);
        Check(Mathf.Abs(t.Duration() - wantDur) < 1e-3f,
              $"① 收招是**两段**：整条 {t.Duration():F3}s = 0.1（段1）+ {CardFeel.AfterAttackDelay:F2}"
            + $"（原版 `AfterAttack` 那记 `SetDelay`）+ 0.1（段2归位）；期望 {wantDur:F3}"
            + "｜🧨 删掉段2 ⇒ 0.100 ⇒ 红；删掉那记 `SetDelay` ⇒ 0.200 ⇒ 红");

        Step(0.099f);                                        // t = 0.10（段1 跑完；上面已经推了 1 ms）
        Check(Mathf.Abs(v.BaseScale - 0.30f) < 2e-3f,
              $"②（新账-74）段1 的缩放补间写进的是 **`CardView.BaseScale`**（实测 {v.BaseScale:F4}，目标 0.30）"
            + "｜🧨 退回 `tr.DOScale(...)` 直写 `localScale` ⇒ `localScale` 变 0.300 而 `BaseScale` 仍 0.360 ⇒ 本条红");
        Check(Mathf.Abs(v.transform.localScale.x - v.BaseScale * v.HighlightMul) < 1e-3f,
              $"……而且结构不变量成立：`localScale` {v.transform.localScale.x:F4} = "
            + $"`BaseScale` {v.BaseScale:F4} × 高亮 {v.HighlightMul:F2}");

        Step(0.4f);                                          // t = 0.50（在段2 之前那 0.65 s 里）
        Check(Mathf.Abs(v.BaseScale - 0.30f) < 2e-3f,
              $"③ 那 {CardFeel.AfterAttackDelay:F2}s 的 `SetDelay` 期间**深度缩放不参与**（t=0.50 实测 {v.BaseScale:F4} = 0.30）"
            + $"｜🧨 把 `activeFrom` 传 0（= 钩子挂整条序列）⇒ 这里已是 {0.30f * ratio:F4} ⇒ 红");

        Step(0.3f);                                          // t = 0.80（段2 = 0.75~0.85 中间）
        float want2 = 0.30f * ratio;
        Check(Mathf.Abs(v.BaseScale - want2) < 5e-3f,
              $"④ 段2 在按**战场深度**重算：0.30 × {ratio:F4} = {want2:F4}（实测 {v.BaseScale:F4}）"
            + "｜🧨 段2 只做位移、不挂 `HookDepthScale` ⇒ 停在 0.300 ⇒ 红");

        Step(0.2f);                                          // t = 1.00（序列跑完）
        Check(!t.IsActive(), "夹具：序列已跑完（下面的复位才可读）");
        Check(Mathf.Abs(v.BaseScale - 0.30f) < 2e-3f,
              $"⑤ 收尾按原版 `FinishAfterAttack` 把静止缩放写回（实测 {v.BaseScale:F4} = 0.30）"
            + $"｜🧨 去掉 `HookDepthScale` 的 `OnKill` 复位 ⇒ 停在 {want2:F4} ⇒ 红");
        Check(Mathf.Abs(v.transform.localPosition.z - ArenaSlots.EnemyZ) < 1e-3f
              && Quaternion.Angle(v.transform.rotation, Quaternion.identity) < 0.5f,
              "……而且位置在静止位、朝向回 identity（`FinishAfterAttack` 那三句）");

        // 反档：**HUD 层**的卡（哪怕摆在敌行 z 上）⇒ 段2 也不写缩放
        var hRoot = new GameObject("chk_hud_recover");
        var h = CardView.Create(hRoot.transform, CardData.Placeholder(42), "chk_hud_card", CardFace.Hand);
        h.gameObject.layer = 0;
        h.SetPose(new Vector3(0f, 0f, ArenaSlots.EnemyZ), 0f, 0.36f);
        var ht = CardFeel.AttackRecover(h.transform, h.transform.localPosition, 0.30f, 0f, isPlayer: true);
        Step(CardFeel.AttackStepTime + CardFeel.AfterAttackDelay + 0.04f);   // t ≈ 0.79（段2 半路）
        Check(Mathf.Abs(h.BaseScale - 0.30f) < 2e-3f,
              $"⑥ 反档：**HUD 层**的卡不吃深度缩放（段2 半路实测 {h.BaseScale:F4} = 0.30）"
            + $"｜🧨 摘掉 `IsArenaCard` 那道门 ⇒ 这里变 {want2:F4} ⇒ **只有本条**红");
        Step(0.2f);                                           // 让它跑完（复位干净）
        Check(ht != null && Mathf.Abs(h.BaseScale - 0.30f) < 2e-3f, "……跑完也停在 0.30（没被写歪）");

        // ---- 新账-74：发牌那两条也不再直写 `localScale` ----
        var dRoot = new GameObject("chk_deal_scale");
        var d = CardView.Create(dRoot.transform, CardData.Placeholder(41), "chk_deal_card", CardFace.Hand);
        d.SetPose(Vector3.zero, 0f, 0.5f);      // 起 0.5 ⇒ 老写法跑完 `BaseScale` 会**停在 0.5**
        CardFeel.DealIn(d, new Vector3(5f, 0f, 0f));
        Step(CardFeel.DealDuration + 0.05f);
        Check(Mathf.Abs(d.BaseScale - 1f) < 1e-3f,
              $"⑦（新账-74）`DealIn` 收尾 **`BaseScale` = 1**（实测 {d.BaseScale:F4}，起始 0.5）"
            + "｜🧨 退回 `tr.DOScale(Vector3.one, …)` ⇒ `localScale` 变 1 而 `BaseScale` 仍 0.500 ⇒ 本条红");
        Check(Mathf.Abs(d.transform.localScale.x - d.BaseScale * d.HighlightMul) < 1e-3f,
              "……而且 `localScale` = `BaseScale × 高亮`（同一结构不变量）");

        CardFeel.DealInPose(d, new Vector3(-5f, 0f, 0f), new Vector3(2f, 0f, 0f), 0f, 0.65f);
        Step(CardFeel.DealDuration + 0.05f);
        Check(Mathf.Abs(d.BaseScale - 0.65f) < 2e-3f,
              $"⑧（新账-74）`DealInPose` 收尾 **`BaseScale` = 0.65**（实测 {d.BaseScale:F4}）"
            + "｜🧨 退回 `tr.DOScale(Vector3.one * scale, …)` ⇒ `BaseScale` 停在 1.000 ⇒ 本条红");

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(hRoot);
        Object.DestroyImmediate(dRoot);
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
