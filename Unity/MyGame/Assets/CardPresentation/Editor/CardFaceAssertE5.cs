// CardFaceAssertE5.cs — 执行代理 `E5` 的断言宿主（2026-10-24）
//
// 覆盖三件（判据全在各自方法头的注释里）：
//   A. **大图档状态切换的补间**（原版 `CardImageController.DoChange` 的 `scaleTime` = 0.3s / `Ease.OutBack`）
//   B. **`CardView.Create` 收两条 overDraw 轴**（`artOverdraw:` / `useOverDraw:`；不传 = 既有调用点零影响）
//   C. **锻造按钮的完整链路**（命中区 → 请求 → 特效；`Shell/CardDetailPopup` 那一串）
//   D. 🆕 **换位音 `CardMove`**（菜单侧 `Shell/CardDetailPopup.SwapToFront`；2026-10-25 执行代理 D）
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod CardFaceAssertE5.Run -logFile -
//
// 判绿红：`grep "^E5"` 的**合计行**；逐条按**行首** `E5 ✓` / `E5 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//   🆕 **2026-10-25（执行代理 `T`）**：末尾还挂了 `SoundCueAssertT`（战斗侧换位音）的 10+2 条，
//   它的行前缀是 **`BT T ✓` / `BT T ✗`**（同 `BT W10` 先例）⇒ **合计行里已经含它**；
//   要连它的明细一起数就 `grep -E "^(E5|BT T) [✓✗]"`。
//
// ⚠️ 本宿主**不建场景、不存盘**：只在临时根下建对象，收尾全部 `DestroyImmediate`。
// ⚠️ `CardProgress.ResetForTest()` 会**关掉落盘**并清空内存态 —— 本宿主必须先调它，
//    否则会改写开发机上的玩家存档（`WarpforgeCardLevels.json`）。
using System.Collections.Generic;
using DG.Tweening;
using RuleEngine;
using UnityEngine;
using CardPresentation;
using WarpforgeVFX;

public static class CardFaceAssertE5
{
    static int _pass, _fail;

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepTier = CardView.DebugFrameTier;
        bool keepOverdraw = CardView.BigCardOverdraw;
        bool keepNoClamp = CardView.DebugDisableArtClamp;
        bool keepNoScaleGate = CardView.DebugDisableBigArtScaleGate;
        var keepTweenMode = CardTween.Mode;
        try
        {
            CardView.DebugFrameTier = 0;              // 正常口径（卡框档 = 卡等级）
            CardView.BigCardOverdraw = true;           // 大图档「角色压框」总开关（补间只在这一档谈得上）
            CardView.DebugDisableArtClamp = false;
            CardView.DebugDisableBigArtScaleGate = false;

            var pool = CardDatabase.Load();
            Debug.Log("[E5] 卡池 " + pool.Count + " 张 ⇒ 开始跑断言");

            var unit = FindByName(pool, "Howling Banshee Exarch");   // 单位卡 · 池内 useOverDraw = 1
            var common = FirstOfRarity(pool, "common");
            var legendary = FirstOfRarity(pool, "legendary");
            Check(unit != null && common != null && legendary != null,
                  "P0 样本齐（单位卡 `Howling Banshee Exarch` + 一张 common + 一张 legendary；实得 unit="
                  + (unit != null) + " common=" + (common != null) + " legendary=" + (legendary != null) + "）");

            if (unit != null) { RunTween(unit); RunCreateAxes(unit); }
            else { Check(false, "A/B 组：找不到单位样本 ⇒ 本组判不了（⛔ 不静默跳过）"); }
            if (common != null && legendary != null) RunCraftChain(common, legendary);
            else Check(false, "C 组：样本不齐 ⇒ 本组判不了（⛔ 不静默跳过）");

            RunSwapSound(pool);

            // 🆕 **2026-10-25（执行代理 `T` · 台账 `AUD` 那条「跑不到」）**：把**战斗侧**那套换位音断言
            //   （`SoundCueAssertT`：cue 存在 / 4 个 clip 名 / 参数 / 常量 / 两条负例 / `PlayFx` 走通 /
            //   **读源码**断战斗窗「恰好一处 + 顺序」/ 元断言）挂在**本宿主**跑一次。
            //   **为什么挂这儿**：它是「卡面 / 窗」的事（同本宿主 C/D 组），而 `Editor/BattleScene.cs`
            //   本批**另有代理在改** ⇒ 借这个已有入口，不再动那个文件。
            //   ⚠️ **计数隔离**：`SoundCueAssertT.Run()` 自己**首尾各 `WindowSounds.ResetCounters()` 一次**，
            //      它的 T-7 只读 `Played` 的「单次增量」（== 1）⇒ **不会**搅乱本宿主 D 组那几个 `Played` 读数；
            //      这里跑完再 reset 一次兜底，免得后面的宿主读到我们这一趟的窗口音计数。
            //   ⚠️ 它自己的行前缀是 **`BT T`**（同 `BadgeFilterAssert` 的 `BT W10` 先例）⇒ 逐条数时要连它一起数。
            RunSoundCueAssertT();
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message
                       + "\n" + e.StackTrace);
        }
        finally
        {
            CardView.DebugFrameTier = keepTier;
            CardView.BigCardOverdraw = keepOverdraw;
            CardView.DebugDisableArtClamp = keepNoClamp;
            CardView.DebugDisableBigArtScaleGate = keepNoScaleGate;
            CardTween.Mode = keepTweenMode;
        }
        Debug.Log("E5 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  A · 大图档状态切换的补间
    // ==================================================================
    //
    // 🔴 判据（一手）：
    //   · 时长 **0.3 s** —— `CardImageController__DoChange.c` 用 `*(float*)(this+0x24)` 当三条补间的
    //     duration；`CardImageController__.ctor.c:16` 写 `0x3e99999a` = 0.3f；序列化
    //     `MonoBehaviour_5393623139443973056.json` 的 `"scaleTime": 0.30000001192092896`。
    //   · 缓动 **`Ease.OutBack`** —— `DoChange.c` 三处 `SetEase<object>(tween, 0x1b, …)`，`0x1b` = 27。
    //   · 触发 **`ChangeState(…, instant=false)`** —— 换位（`ChangeCardPosition.c:74-79`）传 `instant = 0`。
    //   · 补的是 `_Clamp_Range`（Vector4）+ 立绘 scale；我们烘在网格里 ⇒ 逐帧重烘 `_artFront`。
    //
    // ⚠️ 这三个数**不是**「我们挑的」：期望值在断言里写死成原版字面量（0.3f / Ease.OutBack / uv 0.1…0.89），
    //    ⛔ 不是读 `CardView` 的常量再和自己比（那是同义反复）。
    static void RunTween(CardDef def)
    {
        var root = new GameObject("E5_A_root");
        try
        {
            var data = BattleDriver.ToCardData(def, def.Faction);
            var v = CardView.Create(root.transform, data, "CardUI", use: CardUse.ZoomWindow,
                                    artOverdraw: true, useOverDraw: true);
            Check(v != null, "A0 卡建出来了（`CardView.Create` 返回非 null）");
            if (v == null) return;

            var mesh0 = v.ArtFrontMeshForTest;
            Vector4 uv0 = UvRect(mesh0);
            // 出生 = 状态 1（Big）：硬矩形 BIG = (0.75, 0.89, 0.25, 0.1) ⇒ 单位卡（671×1024）的
            //   贴图 uv 窗口约 v ∈ (0.1, 0.89]。**这两条是后面所有补间断言的前提**。
            Check(mesh0 != null && v.ArtFrontShown,
                  "A1 前提：这张单位卡建出了 `_artFront` 且亮着（`BigStateTweenActive` 应为 false）");
            CheckClose(uv0.z, 0.10f, 0.02f, "A1b 出生硬矩形 = **BIG**（贴图 v 下沿 0.1）—— 说明出生就是状态 1");
            CheckClose(uv0.w, 0.89f, 0.02f, "A1c 出生硬矩形 = **BIG**（贴图 v 上沿 0.89）");
            Check(!v.BigStateTweenActive, "A1d 刚建好**没有**在播的补间（一次到位，不是「先建再补」）");

            // ---- 触发：切到状态 0（related card / useOverDraw=0 那一路），Manual 模式推进 ----
            CardTween.Mode = UpdateType.Manual;
            v.ArtOverdraw = false;
            Check(v.BigStateTweenActive,
                  "A2 切状态**起了补间**（Manual 模式；瞬时换网格 ⇒ 这条红）");
            CheckClose(v.BigStateTweenProgress, 0f, 1e-4f,
                       "A2b 起手进度 = 0（= 原版 `.From(…, setImmediately: true)`：立刻摆到**起点**）");
            Vector4 uvStart = UvRect(v.ArtFrontMeshForTest);
            CheckClose(uvStart.z, 0.10f, 0.02f, "A2c 起点的网格仍是 BIG（还没动：0.1）");
            Check(v.ArtFrontShown,
                  "A2d 变小的过程中**先留着**那一层（让它在补间里缩回去；收尾才藏）");

            // ---- 中间态：t = 0.02s / 0.3s ≈ 6.7% ⇒ OutBack ≈ 0.286，硬矩形应落在两档**之间** ----
            CardTween.Advance(0.02f);
            Check(v.BigStateTweenProgress > 0.05f && v.BigStateTweenProgress < 0.95f,
                  "A3 推进 0.02s ⇒ 进度落在 (0.05, 0.95)（实得 " + v.BigStateTweenProgress.ToString("F4") + "）");
            Vector4 uvMid = UvRect(v.ArtFrontMeshForTest);
            Check(uvMid.z > 0.002f && uvMid.z < 0.098f,
                  "A3b 中间态的硬矩形**真在插值**（v 下沿落在 (0.002, 0.098)；实得 "
                  + uvMid.z.ToString("F4") + "）—— 把补间删掉/改瞬时 ⇒ 这里会是 0 或 0.1");
            Check(uvMid.z < uvStart.z - 0.002f,
                  "A3c 中间态比起点**更小**（v 下沿 " + uvMid.z.ToString("F4") + " < " + uvStart.z.ToString("F4") + "）");

            // ---- 灭自证（缓动 = OutBack）：0.85 × 0.3s 处 OutBack **过冲**到 >1 ----
            CardTween.Advance(0.235f);           // 累计 0.255s = 85%
            Check(v.BigStateTweenProgress > 1.005f,
                  "A8 **灭自证**：85% 处进度 > 1（OutBack 的过冲；实得 "
                  + v.BigStateTweenProgress.ToString("F4") + "）—— 换成 Linear / OutQuad 这里会 ≤ 1");

            // ---- 收尾：推完 ⇒ 精确落到状态 0 ----
            CardTween.Advance(0.2f);
            Check(!v.BigStateTweenActive, "A4 推完 ⇒ 补间结束（`BigStateTweenActive == false`）");
            CheckClose(v.BigStateTweenProgress, 1f, 1e-4f, "A4b 推完 ⇒ 进度 = 1");
            Vector4 uvEnd = UvRect(v.ArtFrontMeshForTest);
            CheckClose(uvEnd.z, 0f, 0.005f, "A4c 终态的硬矩形 = **SMALL**（贴图 v 下沿 0）");
            CheckClose(uvEnd.w, 1f, 0.005f, "A4d 终态的硬矩形 = **SMALL**（贴图 v 上沿 1）");
            Check(!v.ArtFrontShown, "A4e 收尾把那一层**藏起来**了（状态 0 ⇒ `ArtFrontVisible == false`）");

            // ---- 触发条件：状态没变 ⇒ 不补（原版 `ChangeState` 第一句 `currentState == stateToChange ⇒ return`）----
            v.UseOverDraw = false;              // bigState 已经是 false ⇒ 合取结果没变
            Check(!v.BigStateTweenActive, "A5 状态**没变**再来一次 ⇒ 不起补间（`from == target` 早退）");

            // ---- 批处理闸：没有帧循环 ⇒ 不补，走瞬时（同 `CollectionWindow` 那条 isPlaying 闸的教训）----
            CardTween.Mode = UpdateType.Normal;
            // ⚠️ 两条轴都要摆回 `true` 才进得了状态 1（A5 刚把 `useOverDraw` 置了 false，原版是合取）。
            v.UseOverDraw = true;               // 此时 artOverdraw 仍是 false ⇒ 状态没变、不该起补间
            v.ArtOverdraw = true;
            Check(!v.BigStateTweenActive,
                  "A6 非 play 且非 Manual ⇒ **不播补间**（批处理里 DOTween 不推进）");
            CheckClose(UvRect(v.ArtFrontMeshForTest).z, 0.10f, 0.02f,
                       "A6b 同一拍**立刻**是新状态（BIG）：瞬时支真的落到网格上了");
            Check(v.ArtFrontShown, "A6c 变大 ⇒ 那一层立刻亮");

            // ---- 常量 = 原版字面量（写死期望值，⛔ 不自证）----
            CheckClose(CardView.BigStateTweenDuration, 0.3f, 1e-6f,
                       "A7 时长 = 原版 `scaleTime` **0.3s**（ctor `+0x24` / 序列化 `scaleTime`）");
            Check(CardView.BigStateTweenEase == Ease.OutBack,
                  "A7b 缓动 = 原版 `DoChange` 的 `SetEase(0x1b)` = **OutBack**（实得 "
                  + CardView.BigStateTweenEase + "）");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  B · `CardView.Create` 收两条 overDraw 轴
    // ==================================================================
    //
    // 结构根因（`H5` §六·2）：两条轴原来只能在建卡**之后**赋值 ⇒ 出生先按 Big 烘一遍、赋值时再重烘。
    // 断言 = 「不传 = 逐项不变」+「传了 = 真的生效」+「灭自证：两者**必须不同**」。
    static void RunCreateAxes(CardDef def)
    {
        var root = new GameObject("E5_B_root");
        try
        {
            var data = BattleDriver.ToCardData(def, def.Faction);

            var b0 = CardView.Create(root.transform, data, "CardUI0", use: CardUse.ZoomWindow);
            Check(b0 != null && b0.ArtOverdraw && b0.UseOverDraw,
                  "B1 不传两条轴 ⇒ 两条都还是出厂 `true`（既有 20 个调用点零影响的第一半）");
            Vector4 uv0 = UvRect(b0 != null ? b0.ArtFrontMeshForTest : null);
            Check(b0 != null && b0.ArtFrontShown && Mathf.Abs(uv0.z - 0.10f) < 0.02f,
                  "B1b 不传 ⇒ 层亮着、网格仍是 BIG（`v 下沿 ≈ 0.1`；实得 " + uv0.z.ToString("F4") + "）");
            Check(b0 != null && !b0.BigStateTweenActive,
                  "B1c 不传 ⇒ 建卡那趟**不产生补间**（轴在建卡前就定了，一次烘好）");

            var b1 = CardView.Create(root.transform, data, "CardUI1", use: CardUse.ZoomWindow,
                                     artOverdraw: true, useOverDraw: false);
            Vector4 uv1 = UvRect(b1 != null ? b1.ArtFrontMeshForTest : null);
            Check(b1 != null && !b1.UseOverDraw, "B2 传 `useOverDraw: false` ⇒ 属性真的是 false");
            Check(b1 != null && !b1.ArtFrontShown, "B2b 传 `useOverDraw: false` ⇒ 层**不亮**");
            Check(Mathf.Abs(uv1.z) < 0.005f && Mathf.Abs(uv1.w - 1f) < 0.005f,
                  "B2c 传 `useOverDraw: false` ⇒ 网格 = **SMALL** 硬矩形（v " + uv1.z.ToString("F4")
                  + "…" + uv1.w.ToString("F4") + "）—— 出生一次到位，没有「先 Big 再重烘」");
            Check(!b1.BigStateTweenActive, "B2d 传轴**不会**顺手起一条补间（不是「建完再切」）");

            // 灭自证：两条路**必须**给出不同的网格 —— 把轴改成空操作 ⇒ 这条红
            Check(Mathf.Abs(uv0.z - uv1.z) > 0.05f,
                  "B3 **灭自证**：不传（BIG）与传 false（SMALL）的硬矩形**必须不同**（差 "
                  + Mathf.Abs(uv0.z - uv1.z).ToString("F4") + "，期望 > 0.05）");

            var b2 = CardView.Create(root.transform, data, "CardUI2", use: CardUse.ZoomWindow,
                                     artOverdraw: false, useOverDraw: true);
            Vector4 uv2 = UvRect(b2 != null ? b2.ArtFrontMeshForTest : null);
            Check(b2 != null && !b2.ArtOverdraw && !b2.ArtFrontShown && Mathf.Abs(uv2.z) < 0.005f,
                  "B4 另一条轴（`artOverdraw: false` = 原版 `idx != 0` 的相关卡）同样落 SMALL 且不亮");

            var b3 = CardView.Create(root.transform, data, "CardUI3", use: CardUse.ZoomWindow,
                                     useOverDraw: true);
            var b3uv = UvRect(b3 != null ? b3.ArtFrontMeshForTest : null);
            Check(b3 != null && b3.ArtOverdraw && b3.UseOverDraw && b3.ArtFrontShown
                  && Mathf.Abs(b3uv.z - uv0.z) < 1e-4f,
                  "B5 **只传一条**（另一条 `null`）⇒ 与完全不传**逐项相同**（没传的那条保持出厂值）");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  C · 锻造按钮的完整链路（`CraftHit` → `OnCraftClicked` → 请求 → 特效）
    // ==================================================================
    //
    // 链路判据（一手）：
    //   · 原版 `CraftingPanel__TryCraftingInternal.c:51` = **先发请求、同一次调用里 `DoVFX()`**；
    //     失败那一支走 `OnFail_22_1`、**不播**。
    //   · 特效表 = `CraftingPanel.craftVFXs[]`（`A31` §2·1 逐行实读，含 rarity 5 的复用行）。
    //   · 闸 = `CraftingPanel__TryCrafting.c:26-49`：传奇 **且** owned == 1 ⇒ 弹确认窗、**不合成不播**。
    // ⚠️ 每一环**一条**断言；`CardProgress.ResetForTest()` 关落盘（绝不改玩家存档）。
    static void RunCraftChain(CardDef common, CardDef legendary)
    {
        CardProgress.ResetForTest();

        // ---- C1 特效表（8 件 + 第 5 行原版复用值）----
        string[,] want = {
            { "Wildcard_Use_Button 1 Common",    "Wildcard_Use_Card 1 Common"    },
            { "Wildcard_Use_Button 2 Rare",      "Wildcard_Use_Card 2 Rare"      },
            { "Wildcard_Use_Button 3 Epic",      "Wildcard_Use_Card 3 Epic"      },
            { "Wildcard_Use_Button 4 Legendary", "Wildcard_Use_Card 4 Legendary" },
        };
        int wrong = 0; string wrongAt = "";
        for (int r = 1; r <= 4; r++)
            for (int k = 0; k < 2; k++)
            {
                string got = WarpforgeUiEffect.WildcardCraftPrefab(r, k == 0);
                string exp = want[r - 1, k];
                if (got != exp) { wrong++; wrongAt += " [rarity " + r + (k == 0 ? " Button" : " Card") + ": " + got + "]"; }
            }
        Check(wrong == 0, "C1 原版 `craftVFXs[]` 的 8 件逐行对上（rarity 1..4 × Button/Card）" +
                          (wrong == 0 ? "" : "；不符 " + wrong + " 处：" + wrongAt));
        Check(WarpforgeUiEffect.WildcardCraftPrefab(5, true) == "Wildcard_Use_Button 4 Legendary"
              && WarpforgeUiEffect.WildcardCraftPrefab(5, false) == "Wildcard_Use_Card 1 Common",
              "C1b 原版第 5 行（Button 复用 4 / Card 回落 1）**照原值收着**（⛔ 别当笔误改掉）");

        // ---- C2 命中区 → 回调：`CraftHit` 在、挂着 `WindowButton`、且有 onClick ----
        CardProgress.SetWildcardsForTest(common.Rarity, 3);
        var go1 = NewPopup(common);
        try
        {
            var pop = go1.GetComponent<CardDetailPopup>();
            var hit = pop != null ? pop.CraftHit : null;
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            Check(hit != null && wb != null && wb.Interactable,
                  "C2 「创建副本」命中区 `CraftHit` 建出来了、挂着 `WindowButton` 且可交互（可合成稀有度 + 有通配符）");

            // ---- C3 真点一下 ⇒ 请求真的发出去了（扣 1 张通配符 + owned +1）----
            int wild0 = CardProgress.Wildcards(common.Rarity);
            int owned0 = CardProgress.Owned(common.Id, common.Rarity);
            // 🔴 **2026-10-25（执行代理 D · 铁律 5）：走【真实入口】`WindowButton.Click()`** ——
            //   原来的 `else pop.OnCraftClicked()` 那条兜底已删：`C2` 已经断过 `wb != null`，
            //   再留兜底就等于「这条路断了也能绿」（断言自证）。
            // 🔴🔴 **2026-10-25 第二轮就地订正（α：断言写错）**：`wb` 必须在**点之前**判。
            //   `DoCraft()` 末尾是 `RebuildKeepingState()` → `Build()`，它把整棵子树**销毁重建**
            //   ⇒ 点完之后 `wb` 是**已销毁组件**（Unity 假 null）⇒ `wb != null` 恒 false。
            //   上一版把它写进点击【之后】的 `Check(...)` 里 ⇒ 数明明动了（3→2 / 11→12）却判红。
            //   （同族先例 = `Editor/MainMenuScene.cs` 的「首格点击后节点被重建 ⇒ 再查恒 null」。）
            bool hadBtn = wb != null;
            if (hadBtn) wb.Click();
            int wild1 = CardProgress.Wildcards(common.Rarity);
            int owned1 = CardProgress.Owned(common.Id, common.Rarity);
            Check(hadBtn && wild1 == wild0 - 1 && owned1 == owned0 + 1,
                  "C3 点一下（走真实入口 `CraftHit` 的 `WindowButton.Click()`）= **真发请求**：通配符 "
                  + wild0 + "→" + wild1 + " · 拥有数 " + owned0 + "→" + owned1
                  + "（空实现 ⇒ 两个数都不动 ⇒ 红）"
                  + "｜🧨 `wb` 的判空必须在**点击之前**（`DoCraft → RebuildKeepingState` 会重建整棵树，"
                  + "点完之后那颗 `WindowButton` 是已销毁对象 ⇒ 假 null）");

            // ---- C4/C5 同一拍播了两件特效，且按这张卡的稀有度选件 ----
            var players = go1.GetComponentsInChildren<WarpforgeEffectPlayer>(true);
            int n = players != null ? players.Length : 0;
            Check(n == 2, "C4 同一拍播了 **2** 件特效（按钮那道 + 前台卡那道；实得 " + n + " 件）");
            string names = "";
            foreach (var p in players) names += p.gameObject.name + " | ";
            string wantBtn = WarpforgeUiEffect.WildcardCraftPrefab(CraftRarityOf(common), true);
            string wantCard = WarpforgeUiEffect.WildcardCraftPrefab(CraftRarityOf(common), false);
            Check(names.Contains(wantBtn) && names.Contains(wantCard),
                  "C5 两件按**这张卡的稀有度**选件（期望「" + wantBtn + "」+「" + wantCard + "」；实得 " + names + "）");
        }
        finally { Object.DestroyImmediate(go1); }

        // ---- C6 失败支 ⇒ **一件都不播**（原版失败走 `OnFail`、不调 DoVFX）----
        CardProgress.SetWildcardsForTest(common.Rarity, 0);
        var go2 = NewPopup(common);
        try
        {
            var pop2 = go2.GetComponent<CardDetailPopup>();
            int ownedA = CardProgress.Owned(common.Id, common.Rarity);
            pop2.OnCraftClicked();          // ⚠️ 直调：模拟「有人绕过了灰钮」（原版灰钮那一支根本不派发）
            var pl2 = go2.GetComponentsInChildren<WarpforgeEffectPlayer>(true);
            Check(CardProgress.Wildcards(common.Rarity) == 0
                  && CardProgress.Owned(common.Id, common.Rarity) == ownedA
                  && (pl2 == null || pl2.Length == 0),
                  "C6 通配符 = 0 ⇒ 请求失败 ⇒ **不合成、不播特效**（owned 不变、特效 0 件；实得 "
                  + (pl2 != null ? pl2.Length : 0) + " 件）");
        }
        finally { Object.DestroyImmediate(go2); }

        // ---- C7 闸：传奇且 owned == 1 ⇒ 弹确认窗、**不合成不播**（原版 `TryCrafting` 那一支 `return`）----
        CardProgress.SetWildcardsForTest(legendary.Rarity, 3);
        CardProgress.AddOwned(legendary.Id, legendary.Rarity, 1 - CardProgress.Owned(legendary.Id, legendary.Rarity));
        var go3 = NewPopup(legendary);
        try
        {
            var pop3 = go3.GetComponent<CardDetailPopup>();
            int wildB = CardProgress.Wildcards(legendary.Rarity);
            int ownedB = CardProgress.Owned(legendary.Id, legendary.Rarity);
            // 🔴 **2026-10-25（执行代理 D）：走【真实入口】**（原来这里直调 `pop3.OnCraftClicked()`）——
            //   传奇且 owned==1 那张的钮**仍是可点的**（有通配符 ⇒ `CheckButtons` 给可交互），
            //   所以这一条不该靠直调绕开钮。⛔ 只有 C6（通配符 0 ⇒ 钮本来就灰）那一支才保留直调，
            //   它模拟的是「有人绕过灰钮」的负例。
            // ⚠️ 「钮在且可交互」必须在**点击之前**判（同 C3：点一下会重建整棵树 ⇒ 点完是假 null）。
            var wb3 = pop3.CraftHit != null ? pop3.CraftHit.GetComponent<WindowButton>() : null;
            bool hadBtn3 = wb3 != null && wb3.Interactable;
            if (hadBtn3) wb3.Click();
            var pl3 = go3.GetComponentsInChildren<WarpforgeEffectPlayer>(true);
            Check(ownedB == 1 && hadBtn3 && pop3.CraftConfirmPopup != null
                  && CardProgress.Wildcards(legendary.Rarity) == wildB
                  && CardProgress.Owned(legendary.Id, legendary.Rarity) == ownedB
                  && (pl3 == null || pl3.Length == 0),
                  "C7 传奇 + owned==1 ⇒ **点真实入口只弹确认窗**（不走合成、不播特效；通配符 " + wildB + " 不变、"
                  + "owned " + ownedB + " 不变、特效 " + (pl3 != null ? pl3.Length : 0) + " 件）");
            pop3.HideCraftConfirm();
        }
        finally { Object.DestroyImmediate(go3); }

        CardProgress.ResetForTest();        // 收尾复位（⛔ 别把摆出来的假库存留给后面的宿主）
    }

    static GameObject NewPopup(CardDef def)
    {
        // 照 `ShowCard` 里「没有 `WindowsManager`（自检里可能）⇒ 自给自足」那条支：先激活再 `Build()`。
        var go = new GameObject("E5_C_popup");
        go.AddComponent<CardDetailPopup>();
        go.GetComponent<CardDetailPopup>().ShowCard(def);
        return go;
    }

    /// <summary>稀有度键 → 原版 `craftVFXs[]` 的 1 基号（与 `CardDetailPopup.CraftRarityIndex` 同一条表；
    /// 这里独立写一份只为**不拿被测实现证明被测实现**）。</summary>
    static int CraftRarityOf(CardDef d)
    {
        switch ((d.Rarity ?? "").ToLowerInvariant())
        {
            case "common": return 1;
            case "rare": return 2;
            case "epic": return 3;
            case "legendary": return 4;
            default: return 0;
        }
    }

    // ================================================================== 小工具

    // ==================================================================
    //  D · 换位音 `CardMove`（菜单侧）—— 🆕 2026-10-25 执行代理 D
    // ==================================================================
    //
    // 判据（一手）：
    //   · 原版 `CardDisplayWindow__ChangeCardPosition.c:68-73`：换位状态置真（`+0x150 = 1`）**之后**、
    //     `SetImageOverDraw` 与两条换位补间**之前**，一声 `SoundManager.Play2D(<soundOnCardSwap>, MixerType 0 = FX)`。
    //   · cue = **`CardMove`**（pid 4812904303840730115 · 4 clip）。菜单侧 `CardDetailPopup` 的根 MB
    //     与战斗那扇 `CardDisplayWindow` **同一个 pid（2033）** ⇒ 同一个 cue 常量（`CardDisplayWindow.SwapSoundCue`）。
    //   · 战斗侧早已接（`Battle/CardDisplayWindow.cs:1592`，由 `SoundCueAssertT` 的 T-8a/T-8b 钉着）
    //     ⇒ **本组钉菜单侧这一声**（在此之前它一直是默片）。
    static void RunSwapSound(List<CardDef> pool)
    {
        Debug.Log("[E5] --- D 组：换位音 `CardMove`（菜单侧 `CardDetailPopup.SwapToFront`）---");

        // ③ 先做**静态**那条（不依赖运行期）：读源码断「位置」。⚠️ 批处理里补间不推进，
        //    纯运行期断言**证明不了**「在补间之前」。
        CheckSwapSoundCallSite();

        // 找一个**带相关卡**的样本（扇形要有第 1 格才能换位）
        CardDef sample = null;
        for (int i = 0; i < pool.Count && sample == null; i++)
        {
            var d = pool[i];
            if (d == null || string.IsNullOrEmpty(d.Id)) continue;
            var rel = RelatedCards.Find(d, CardDetailPopup.Slots - 1);
            if (rel != null && rel.Count > 0) sample = d;
        }
        Check(sample != null,
              "D0 找到一张**带相关卡**的样本（换位要有第 1 格可点；实得 "
            + (sample != null ? sample.Name : "<没有这样的卡>") + "）");
        if (sample == null) return;

        bool keepEnabled = WFSoundPlayer.Enabled;
        var go = NewPopup(sample);
        try
        {
            var pop = go.GetComponent<CardDetailPopup>();

            // ① 负例：**只开窗 / 重建，不换位** ⇒ 换位音一声都不出
            WindowSounds.ResetCounters();
            pop.ShowCard(sample);                       // = 生产那条「开窗/换卡」路（`ShowCard → Build`）
            Check(WindowSounds.Played == 0 && WindowSounds.LastCue != CardDisplayWindow.SwapSoundCue,
                  "D1 **负例**：只开窗 / 重建（不换位）⇒ 换位音**一声都不出**（Played = " + WindowSounds.Played
                + " · LastCue = `" + WindowSounds.LastCue + "`）"
                + "｜🧨 把那行 `PlayFx` 挪进 `Build()` / `ShowCard()` ⇒ 本条红");

            Check(pop.SlotCount >= 2, "D2 前提：扇形有第 1 格可换（`SlotCount` 实得 " + pop.SlotCount + "）");
            if (pop.SlotCount < 2) return;
            Check(pop.SlotViews[1] != null, "D2b 前提：第 1 格真的建出了卡（`SlotViews[1] != null`）");
            if (pop.SlotViews[1] == null) return;

            // ② 正例：换一次位 ⇒ 该 cue **恰好 1 次**
            WFSoundPlayer.Enabled = false;               // 自检：不建 AudioSource、不出声（同 `SoundCueAssertT`）
            WindowSounds.ResetCounters();
            pop.SwapToFront(1);
            Check(WindowSounds.Played == 1 && WindowSounds.LastCue == CardDisplayWindow.SwapSoundCue,
                  "D3 换位 ⇒ cue `" + CardDisplayWindow.SwapSoundCue + "` **恰好播 1 次**"
                + "（Played = " + WindowSounds.Played + " · LastCue = `" + WindowSounds.LastCue + "`）"
                + "｜🧨 删掉 `SwapToFront` 里那行 `PlayFx` ⇒ Played = 0 ⇒ 红；"
                + "🧨 播两次（比如 `Open` 和 `Swap` 各一次）⇒ Played = 2 ⇒ 红");
        }
        finally
        {
            WFSoundPlayer.Enabled = keepEnabled;
            WindowSounds.ResetCounters();
            Object.DestroyImmediate(go);                 // 顺带把还没推进的补间一起带走（`SetLink` 会杀）
        }
    }

    /// <summary>读源码断**换位音的位置**：`_swapping = true;` &lt; `PlayFx(CardDisplayWindow.SwapSoundCue…)` &lt;
    /// `b.ArtOverdraw = true;`（原版那两句 `SetImageOverDraw` 在两条补间之前 ⇒ 用它当「补间之前」的界桩）。
    /// <para>⚠️ 读不到源码 = **没验到**，必须报红（⛔ 不能静默通过）。</para></summary>
    static void CheckSwapSoundCallSite()
    {
        const string rel = "CardPresentation/Shell/CardDetailPopup.cs";
        string path = System.IO.Path.Combine(Application.dataPath, rel);
        if (!System.IO.File.Exists(path))
        {
            Check(false, "D4 找不到源码 `" + path + "` ⇒ 「换位音的**位置**」**没有验到**"
                       + "（⛔ 这不是「验过没问题」）");
            return;
        }

        var lines = System.IO.File.ReadAllLines(path);
        int iMethod = -1, iSwapTrue = -1, iPlay = -1, iOverdraw = -1, nPlay = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string s = lines[i].Trim();
            if (iMethod < 0)
            {
                if (s.StartsWith("public void SwapToFront(int idx)")) iMethod = i;
                continue;
            }
            // 出了这个方法就停（下一个成员一定有 XML 文档注释；`void FinishSwap()` 是方法内的局部函数 ⇒ 不能当界）
            if (s.StartsWith("/// <summary>") || s.StartsWith("public ")) break;
            if (s.StartsWith("//") || s.StartsWith("*") || s.StartsWith("/*")) continue;   // 注释行不算
            if (s == "_swapping = true;") iSwapTrue = i;
            if (s.Contains("WindowSounds.PlayFx(CardDisplayWindow.SwapSoundCue")) { nPlay++; if (iPlay < 0) iPlay = i; }
            if (s.StartsWith("b.ArtOverdraw = true;")) iOverdraw = i;
        }

        Check(nPlay == 1,
              "D4a `SwapToFront` 里 `WindowSounds.PlayFx(CardDisplayWindow.SwapSoundCue, …)` **恰好一处**"
            + "（非注释行，现读 " + nPlay + " 处）"
            + "｜🧨 删掉（换位变默片）⇒ 0 处 ⇒ 红；🧨 换成一个写死的别的 cue 名 ⇒ 本串不再被引用 ⇒ 红");
        Check(nPlay == 1 && iSwapTrue >= 0 && iOverdraw >= 0 && iSwapTrue < iPlay && iPlay < iOverdraw,
              "D4b **位置**也是判据 —— `_swapping = true;`（行 " + (iSwapTrue + 1) + "） < PlayFx（行 "
            + (iPlay + 1) + "） < `b.ArtOverdraw = true;`（行 " + (iOverdraw + 1) + "）"
            + "（原版 `ChangeCardPosition.c:68-73`：播音在 `SetImageOverDraw` 与两条补间**之前**）"
            + "｜🧨 把它挪进 `OnComplete`（= `CardSwapFinished` 之后）/ 挪到补间之后 ⇒ 顺序不成立 ⇒ 红");
    }

    /// <summary>🆕 **2026-10-25（执行代理 `T`）**：把 `SoundCueAssertT`（**战斗侧**换位音：
    /// `Battle/CardDisplayWindow.SwapToFront` 那一跳 + `CardMove` cue 的数据侧）挂在本宿主上跑一次，
    /// 并把它那两条计数并进本文件的总数（与 `BattleVfxAssertE3` 挂 `BadgeFilterAssert` 同一个做法）。
    /// <para>**为什么要有这一步**：`SoundCueAssertT` 原来**全仓没有宿主调用**（台账
    /// `资料/任务线/音效线/任务总表.md:20` 记的就是这条）⇒ 它那 10 条断言**永远跑不到**。
    /// 本文件已经覆盖**菜单侧**（D 组 = `Shell/CardDetailPopup`），战斗侧正缺一个跑它的地方。</para>
    /// <para>🔴 **计数隔离**（D 组那几条 `WindowSounds.Played` 依赖它）：`SoundCueAssertT.Run()` 内部
    /// 在 T-7 前后**各 `WindowSounds.ResetCounters()` 一次**、只读「单次增量 == 1」⇒ 不看历史累计；
    /// 这里再 reset 一次兜底。⛔ 别把这两行调换到 `RunSwapSound(pool)` **之前** —— D1 的负例
    /// （「只开窗不换位 ⇒ Played == 0」）会读到我们这一趟的清零动作之前的状态，语义就混了。</para>
    /// <para>⚠️ 它的 `✗`/`✓` 行前缀是 **`BT T`**（各断各的）⇒ `grep "^E5"` 数「合计行」时要记得
    /// 合计里**已经含**它那 10+1 条。</para></summary>
    static void RunSoundCueAssertT()
    {
        SoundCueAssertT.Run();
        _pass += SoundCueAssertT.Passed;
        _fail += SoundCueAssertT.Failed;
        WindowSounds.ResetCounters();        // 兜底：本宿主收尾时窗口音计数归零
    }

    static CardDef FindByName(List<CardDef> pool, string name)
    {
        foreach (var d in pool) if (d != null && d.Name == name) return d;
        return null;
    }

    /// <summary>某稀有度下**第一张可合成**的卡（四档之一；`special` 那批原版整块不建）。
    /// ⚠️ 必须带**非空 `Id`** —— `CardProgress` 的字典按 `Id` 存，空 key 会抛。</summary>
    static CardDef FirstOfRarity(List<CardDef> pool, string rarity)
    {
        foreach (var d in pool)
            if (d != null && !string.IsNullOrEmpty(d.Id) && (d.Rarity ?? "").ToLowerInvariant() == rarity) return d;
        return null;
    }

    /// <summary>网格 uv 的包围（uMin, uMax, vMin, vMax）；没有网格 ⇒ 返回 `(-1,-1,-1,-1)`。</summary>
    static Vector4 UvRect(Mesh m)
    {
        if (m == null || m.uv == null || m.uv.Length == 0) return new Vector4(-1f, -1f, -1f, -1f);
        var uv = m.uv;
        float uMin = float.MaxValue, uMax = float.MinValue, vMin = float.MaxValue, vMax = float.MinValue;
        for (int i = 0; i < uv.Length; i++)
        {
            if (uv[i].x < uMin) uMin = uv[i].x;
            if (uv[i].x > uMax) uMax = uv[i].x;
            if (uv[i].y < vMin) vMin = uv[i].y;
            if (uv[i].y > vMax) vMax = uv[i].y;
        }
        return new Vector4(uMin, uMax, vMin, vMax);
    }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("E5 ✓ " + msg); }
        else { _fail++; Debug.LogError("E5 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                                             + "；实得 " + got.ToString("F4") + "）");
    }
}
