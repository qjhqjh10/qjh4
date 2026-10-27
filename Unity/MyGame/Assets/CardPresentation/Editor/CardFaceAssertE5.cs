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

            // 🔴 2026-10-27（执行代理 `B9_W4NFG`）：**嵌套淡入组**（原版 `NestedFadeGroup`：9 组根 + 子树 SR/TMP）
            RunNestedFadeGroups();

            // 🆕 2026-10-27（执行代理 `B9_W15CVIEW` · 账 `HUD:108` / `HUD:153` / `HUD:148` / `HUD:189`）：
            //   K-03 那四条（`Core/CardView.cs`）的断言 —— 判据与本组说明见 `RunK03Four` 的头注释。
            //   ⚠️ 它会把 `Loc` 语言临时切到英文再切回（`PersistOverride` + `RestoreForTest`，**不碰 `PlayerPrefs`**）
            //      ⇒ 排在 A–E 组**之后**（免得影响它们对中文文案的读数），且自己 `finally` 里一定恢复语档再往下走。
            RunK03Four(pool);

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
        // 🔴 **2026-10-26（B9 · `X4` 就地订正，α：夹具摆在生产看不见的桶里）**：今日 `F7` 把通配符库存键
        //   升成 **两维 `army|rarity`**（`CardProgress.WildKey`；判据 = 原版 `CardServices.CheckForCardCrafting`
        //   的谓词 `w.cardArmy == card.cardArmy && w.cardRarity == card.Rarity`，见 `Shell/CardDetailPopup.cs:381-414`），
        //   而本自检原来摆的是**一维旧桶**（`Wildcards(rarity)` = 「不分阵营」兼容桶）⇒ 生产链**看不见**它
        //   （两维桶缺键 = 缺省 99）⇒ C3 的「3→3」与 C6 的「0 张」两条**都没验到东西**
        //   （C6 更是从 `F7` 起一直**空跑**）。⇒ 摆/读一律改走两维口（先例 = `Editor/CollectionScene.cs:9055/9072/9111`）。
        Check(!string.IsNullOrEmpty(common.Faction),
              "（夹具前提）样本卡有阵营 —— 否则 `WildKey(Faction, Rarity)` 会退化成「不分阵营」旧桶、本组又变空跑");
        CardProgress.SetWildcardsForTest(CardProgress.WildKey(common.Faction, common.Rarity), 3);
        Check(CardProgress.Wildcards(common.Faction, common.Rarity) == 3,
              "（夹具前提）`" + common.Faction + "|" + common.Rarity + "` 那桶真是 **3** 张（用**生产侧那个读口**读回来；"
            + "摆进旧桶 ⇒ 这里读到缺省 99 ⇒ 红，⛔ 不静默空跑）");
        var go1 = NewPopup(common);
        try
        {
            var pop = go1.GetComponent<CardDetailPopup>();
            var hit = pop != null ? pop.CraftHit : null;
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            Check(hit != null && wb != null && wb.Interactable,
                  "C2 「创建副本」命中区 `CraftHit` 建出来了、挂着 `WindowButton` 且可交互（可合成稀有度 + 有通配符）");

            // ---- C3 真点一下 ⇒ 请求真的发出去了（扣 1 张通配符 + owned +1）----
            int wild0 = CardProgress.Wildcards(common.Faction, common.Rarity);   // 🔴 两维桶（`army|rarity`，见上面 C2 前那段订正）
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
            int wild1 = CardProgress.Wildcards(common.Faction, common.Rarity);   // 🔴 两维桶（同上）
            int owned1 = CardProgress.Owned(common.Id, common.Rarity);
            Check(hadBtn && wild1 == wild0 - 1 && owned1 == owned0 + 1,
                  "C3 点一下（走真实入口 `CraftHit` 的 `WindowButton.Click()`）= **真发请求**：通配符 "
                  + wild0 + "→" + wild1 + " · 拥有数 " + owned0 + "→" + owned1
                  + "（空实现 ⇒ 两个数都不动 ⇒ 红）"
                  + "｜🧨 `wb` 的判空必须在**点击之前**（`DoCraft → RebuildKeepingState` 会重建整棵树，"
                  + "点完之后那颗 `WindowButton` 是已销毁对象 ⇒ 假 null）"
                  + "｜🧨 摆/读若退回一维 `Wildcards(rarity)` ⇒ 生产扣的是两维桶、这里读的是旧桶 ⇒ "
                  + wild0 + "→" + wild0 + "（= `F7` 之前那条假绿）");

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
        // 🔴 **2026-10-26（B9 · `X4`）就地订正 —— 这一条自 `F7` 起是【空跑】**：原来摆的是
        //   `SetWildcardsForTest(common.Rarity, 0)`（一维旧桶），而生产读 `(Faction, Rarity)` 两维桶
        //   ⇒ 那桶一直是缺省 **99** ⇒ 合成照样成功、特效照播。也就是说「没有通配符 ⇒ 不许播特效」
        //   这条判据**从 `F7` 起没有任何断言在真守**（`D2` 报告 §四 的牙口缺口）。现在真摆 0：
        //   `MatchWildcard` 会走「同阵营兜底」（别的稀有度桶仍有 99）⇒ `Craftable = false`
        //   ⇒ `DoCraft()` 拒绝、`TryCraft` 返回 false ⇒ 不合成、不播
        //   （判据 = 原版 `CardServices.CheckForCardCrafting` 兜底那一支 `return false`；
        //     我们的等价物见 `Shell/CardDetailPopup.cs:459-471`）。
        //   ⚠️ 对照组 = 上面 C3（同一个读口、3 张时**真扣成 2 且 owned +1**）⇒ 这一条不是「什么都验不到」。
        CardProgress.SetWildcardsForTest(CardProgress.WildKey(common.Faction, common.Rarity), 0);
        Check(CardProgress.Wildcards(common.Faction, common.Rarity) == 0,
              "（夹具前提）`" + common.Faction + "|" + common.Rarity + "` 那桶**真的摆成 0 张**"
            + "（⛔ 不摆 0 的话下面那条只剩「什么也没发生」——`F7` 之后它就是那样空跑的）");
        var go2 = NewPopup(common);
        try
        {
            var pop2 = go2.GetComponent<CardDetailPopup>();
            int ownedA = CardProgress.Owned(common.Id, common.Rarity);
            pop2.OnCraftClicked();          // ⚠️ 直调：模拟「有人绕过了灰钮」（原版灰钮那一支根本不派发）
            var pl2 = go2.GetComponentsInChildren<WarpforgeEffectPlayer>(true);
            Check(CardProgress.Wildcards(common.Faction, common.Rarity) == 0
                  && CardProgress.Owned(common.Id, common.Rarity) == ownedA
                  && (pl2 == null || pl2.Length == 0),
                  "C6 通配符 = 0 ⇒ 请求失败 ⇒ **不合成、不播特效**（owned 不变、特效 0 件；实得 "
                  + (pl2 != null ? pl2.Length : 0) + " 件）"
                  + "｜🧨 抠掉 `DoCraft` 里 `!wm.Craftable` 那道闸 ⇒ 本行红（会 owned+1 + 播 2 件）"
                  + "｜🧨 摆/读退回一维旧桶 ⇒ 生产看两维桶仍是 99 ⇒ 同样 owned+1 + 播 2 件 ⇒ 本行红");
        }
        finally { Object.DestroyImmediate(go2); }

        // ---- C7 闸：传奇且 owned == 1 ⇒ 弹确认窗、**不合成不播**（原版 `TryCrafting` 那一支 `return`）----
        // 🔴 **2026-10-26（B9 · `X4`）顺手一起改（同 C3/C6 那笔 `F7` 换桶，`D2` §七.5 标过的那一格）**：
        //   这一条今天是**碰巧绿**（闸由 `owned == 1` 挡下、与通配符无关 ⇒ 摆在旧桶里也绿）
        //   ⇒ 留着等于给下一个人埋一个「摆位其实没生效」的假前提。摆/读一并改两维。
        CardProgress.SetWildcardsForTest(CardProgress.WildKey(legendary.Faction, legendary.Rarity), 3);
        CardProgress.AddOwned(legendary.Id, legendary.Rarity, 1 - CardProgress.Owned(legendary.Id, legendary.Rarity));
        var go3 = NewPopup(legendary);
        try
        {
            var pop3 = go3.GetComponent<CardDetailPopup>();
            int wildB = CardProgress.Wildcards(legendary.Faction, legendary.Rarity);   // 🔴 两维桶
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
                  && CardProgress.Wildcards(legendary.Faction, legendary.Rarity) == wildB
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

    // ==================================================================
    //  E · 嵌套淡入组（原版 `NestedFadeGroup`：9 组根 + 子树 SR/TMP）
    // ==================================================================
    //
    // 🔴 判据（一手）：`资料/完全复刻/判据/NestedFadeGroup_结构判定.md`
    //   —— 原版 `CardPrefab` 现读 57 个实例 = **9 组根 + 38 SR + 10 TMP**；9 个组根的子树子件数 =
    //   `Base Attack Counters` 4 · `Base Health Counters` 1 · 7 个 `TraitIconContainer*` 各 6；
    //   组根 α 一广播，**只淡那一组**（9 个组根互不为父，`parentChainNFG = 0`）。
    //   ⚠️ 我们的结构是**等价实现**（每组一个 float 通道 + 建层时直接持有子件引用），
    //   所以件数按「**语义角色**」数：trait 槽 4 件 —— 原版那 6 件里 `TraitIcon` / `Trait Not Active`
    //   各有两套（`With counter` / `Without counter`），原版靠 activeSelf 二选一 ⇒ 角色只有 4 个。
    //   ⛔ 期望值全部**写死在断言里**（原版字面量 9 / 4 / 1 / 名表），不读 `CardView` 的常量再和自己比。
    static void RunNestedFadeGroups()
    {
        // 原版 9 个组根的节点名（判据件 §二/§三 逐个抄；⛔ 不是从 `CardView` 读出来的）
        var wantNames = new[]
        {
            "TraitIconContainer 1", "TraitIconContainer 2", "TraitIconContainer 3",
            "TraitIconContainer Right 1", "TraitIconContainer Right 2",
            "TraitIconContainer Right 3", "TraitIconContainer Right 4",
            "Base Attack Counters", "Base Health Counters",
        };
        // 原版每个组根子树里的子件数（`Base Attack` 4 · `Base Health` 1 · 7 个 trait 各 6）
        var wantKids = new[] { 6, 6, 6, 6, 6, 6, 6, 4, 1 };
        const int MaxK = 4;                     // 我们每组的枚举槽数（trait 4 / BaseAttack 4 / BaseHealth 1）

        Check(CardView.FadeGroupCount == 9 && wantNames.Length == 9 && wantKids.Length == 9,
              "E1 组根数量 = 原版的 **9**（`CardView.FadeGroupCount` 实得 " + CardView.FadeGroupCount + "）");

        var root = new GameObject("E5_E_root");
        var keepMode = CardTween.Mode;
        try
        {
            CardTween.Mode = UpdateType.Manual;      // 批处理无帧循环 ⇒ 补间全走手动推进

            // 4 枚徽标：最后一枚 `active = false` ⇒ 顺带把 `Trait Not Active`（那枚 ✕）也建出来
            var badges = new List<Badge>
            {
                new Badge { sprite = "armour",     counter = 2, active = true  },
                new Badge { sprite = "blast",      counter = 3, active = true  },
                new Badge { sprite = "flying",     counter = 0, active = true  },
                new Badge { sprite = "vulnerable", counter = 1, active = false },
            };
            var d = CardData.Simple("FadeGroupProbe", 3, 3, 4);
            d.badges = badges;
            var v = CardView.Create(root.transform, d, "FadeGroupProbe");
            Check(v != null, "E2 前提：探针卡建出来了（`CardView.Create` 返回非 null）");
            if (v == null) return;

            // 点亮「这张卡自己的攻击类型」= 原版 `BasicCardUI.AttackTypeChanged` ⇒ 底光
            // （`Highlight` / `Highlight ranged`）是**懒建**的，不点这一下 `Base Attack Counters` 只有 2 件、
            // 另外 2 件（两个底光）永远建不出来。判据 = 原版那 4 件就挂在同一个组根下。
            v.SetOwnAttackType(AttackTypes.Melee);

            // ---- E1b 名表逐字对 ----
            var nameBad = "";
            for (int g = 0; g < CardView.FadeGroupCount; g++)
            {
                var nm = v.FadeGroupName((CardView.FadeGroup)g);
                if (nm != wantNames[g]) nameBad += "[" + g + "] 期望 `" + wantNames[g] + "` 实得 `" + nm + "` ";
            }
            Check(nameBad.Length == 0,
                  "E1b 9 个组根名与原版 prefab 的节点名**逐字一致**"
                + (nameBad.Length == 0 ? "" : "（不一致：" + nameBad + "）"));

            // ---- E2 组根 → 子件对照表（实际建出来几件、缺哪几件）----
            var counts = new int[CardView.FadeGroupCount];
            var table = "";
            var told = "";
            for (int g = 0; g < CardView.FadeGroupCount; g++)
            {
                int slots = (g == (int)CardView.FadeGroup.BaseHealthCounters) ? 1 : MaxK;
                var built = "";
                int n = 0;
                for (int k = 0; k < slots; k++)
                {
                    if (v.FadeGroupMemberBuilt((CardView.FadeGroup)g, k))
                    {
                        n++;
                        built += v.FadeGroupMemberName((CardView.FadeGroup)g, k) + " ";
                    }
                    else built += "〔缺:" + v.FadeGroupMemberName((CardView.FadeGroup)g, k) + "〕 ";
                }
                counts[g] = n;
                table += "\n        · " + wantNames[g] + " → 实建 " + n + "/" + slots + "：" + built;
                if (n < slots) told += wantNames[g] + "(" + n + "/" + slots + ") ";
            }
            Debug.Log("[E5] 组根 → 子件对照（原版每根 " + string.Join("/", wantKids) + "；我们按语义角色）" + table);
            // 前 4 个槽有徽标 ⇒ 底板 + 图标 + 角标 三件必须有（✕ 只在未激活位建；
            // 槽 4/5/6 本次没有徽标 ⇒ 一件不建是**正常**的，不在这里判）
            var traitBad = "";
            for (int g = 0; g < 4; g++)
                if (counts[g] < 3) traitBad += wantNames[g] + "(" + counts[g] + ") ";
            Check(traitBad.Length == 0,
                  "E2 前 4 个徽标槽各自的子树都建出了 ≥3 件（底板 `IconBackground` + 图标 `TraitIcon` + 角标 `TraitCounter`）"
                + (traitBad.Length == 0 ? "" : "；不足：" + traitBad));
            // ✕（`Trait Not Active`）—— 第 4 枚徽标是未激活的 ⇒ 它必须建出来
            Check(v.FadeGroupMemberBuilt((CardView.FadeGroup)3, 2)
                  && v.FadeGroupMemberName((CardView.FadeGroup)3, 2) == "Trait Not Active",
                  "E3 未激活那枚徽标把 `Trait Not Active`（✕）建出来了，且它挂在**同一个组根**下（槽 3 第 3 件）");
            if (told.Length > 0)
                Debug.Log("[E5] 本次没建的子件（不是缺陷，是这一档没建）：" + told);

            // ---- E4 前提：数值组那两套载体必须建出来，否则下面两条判不了 ----
            bool atkPrem = v.FadeGroupMemberBuilt((CardView.FadeGroup)7, 0)
                        && v.FadeGroupMemberBuilt((CardView.FadeGroup)7, 1);
            bool hpPrem = v.FadeGroupMemberBuilt((CardView.FadeGroup)8, 0);
            Check(atkPrem && hpPrem,
                  "E4 前提：`_stats[]` 四个数值层建出来了（近战=" + v.FadeGroupMemberBuilt((CardView.FadeGroup)7, 0)
                + " 远程=" + v.FadeGroupMemberBuilt((CardView.FadeGroup)7, 1)
                + " 生命=" + hpPrem + "）—— 没有它两条「数值组只淡自己」判不了（⛔ 不是通过）");

            // 基线（全部组 × 全部槽）
            var baseline = new float[CardView.FadeGroupCount, MaxK];
            for (int g = 0; g < CardView.FadeGroupCount; g++)
                for (int k = 0; k < MaxK; k++)
                    baseline[g, k] = v.FadeGroupMemberAlpha((CardView.FadeGroup)g, k);

            // 基线的硬前提：参与断言的每一件必须**本来就不透明**（α ≈ 1），否则 ×0.4 恒等于 ×0
            var baseBad = "";
            for (int g = 0; g < 7; g++)
                for (int k = 0; k < MaxK; k++)
                    if (baseline[g, k] >= 0f && baseline[g, k] < 0.99f)
                        baseBad += wantNames[g] + "." + v.FadeGroupMemberName((CardView.FadeGroup)g, k)
                                 + "=" + baseline[g, k].ToString("F3") + " ";
            Check(baseBad.Length == 0,
                  "E5 前提：9 个组根的每一件基线 α 都 ≈ 1（原版 57 个实例序列化值 `alpha = 1`）"
                + (baseBad.Length == 0 ? "" : "；异常：" + baseBad));

            // ---- E6 只淡组 A ⇒ 组 A 逐件 ×0.4、其余 8 组**逐位不变** ----
            int A = (int)CardView.FadeGroup.TraitIcon2;            // 第 2 个 trait 组根（左列第 2 槽）
            v.SetFadeGroupAlpha((CardView.FadeGroup)A, 0.4f);
            var aBad = ""; float aWorst = 0f;
            for (int k = 0; k < MaxK; k++)
            {
                float b0 = baseline[A, k];
                if (b0 < 0f) continue;                              // 这一件没建
                float now = v.FadeGroupMemberAlpha((CardView.FadeGroup)A, k);
                float dev = Mathf.Abs(now - b0 * 0.4f);
                if (dev > aWorst) aWorst = dev;
                if (dev > 0.02f)
                    aBad += v.FadeGroupMemberName((CardView.FadeGroup)A, k) + "(" + now.ToString("F4") + ") ";
            }
            Check(aBad.Length == 0,
                  "E6 ★ 只淡组 A（`" + wantNames[A] + "` → 0.4）⇒ 它子树里**每一件** α 都 ≈ 0.4×基线"
                + "（最大偏差 " + aWorst.ToString("F4") + (aBad.Length == 0 ? "）" : "；异常：" + aBad + "）"));

            var bBad = "";
            for (int g = 0; g < CardView.FadeGroupCount; g++)
            {
                if (g == A) continue;
                for (int k = 0; k < MaxK; k++)
                {
                    float b0 = baseline[g, k];
                    if (b0 < 0f) continue;
                    float now = v.FadeGroupMemberAlpha((CardView.FadeGroup)g, k);
                    if (now != b0)
                        bBad += wantNames[g] + "." + v.FadeGroupMemberName((CardView.FadeGroup)g, k)
                              + " " + b0.ToString("F4") + "→" + now.ToString("F4") + " ";
                }
            }
            Check(bBad.Length == 0,
                  "E7 ★ **只淡组 A ⇒ 其余 8 组的每一件 α【逐位不变】**"
                + "｜🧨 旧的整卡单标量（`_badgeAlpha` 一乘全乘）在这条上**必然红** —— 这就是「分组淡入」与「一淡全淡」的分界"
                + (bBad.Length == 0 ? "" : "；被一起改了：" + bBad));

            // ---- E8 角标那一条**独立通道**（TMP 顶点色，不在 `_layers` 里）也要跟着组 α ----
            int kCounter = 3;
            bool counterBuilt = v.FadeGroupMemberBuilt((CardView.FadeGroup)A, kCounter)
                             && v.FadeGroupMemberName((CardView.FadeGroup)A, kCounter) == "TraitCounter";
            Check(counterBuilt,
                  "E8 前提：组 A 的角标 `TraitCounter`（TMP）建出来了（原版 `With counter` 那一支）");
            if (counterBuilt)
            {
                float cA = v.FadeGroupMemberAlpha((CardView.FadeGroup)A, kCounter);
                int B = (int)CardView.FadeGroup.TraitIconRight2;
                float cB = v.FadeGroupMemberAlpha((CardView.FadeGroup)B, kCounter);
                Check(Mathf.Abs(cA - baseline[A, kCounter] * 0.4f) <= 0.02f
                      && Mathf.Abs(cB - baseline[B, kCounter]) <= 1e-6f,
                      "E8b ★ 角标字（TMP 通道）也跟着组 α：组 A 的角标 = 0.4×基线（实得 " + cA.ToString("F4")
                    + "，基线 " + baseline[A, kCounter].ToString("F4") + "）、组 B 的角标不动（实得 "
                    + cB.ToString("F4") + "）");
            }

            // ---- E9 淡到 0 ⇒ 那一组**真的全透明**（每一条通道都 0），别的组仍 1 ----
            v.SetFadeGroupAlpha((CardView.FadeGroup)A, 0f);
            var zBad = "";
            for (int k = 0; k < MaxK; k++)
            {
                if (baseline[A, k] < 0f) continue;
                float now = v.FadeGroupMemberAlpha((CardView.FadeGroup)A, k);
                if (Mathf.Abs(now) > 1e-6f)
                    zBad += v.FadeGroupMemberName((CardView.FadeGroup)A, k) + "(" + now.ToString("F4") + ") ";
            }
            Check(zBad.Length == 0,
                  "E9 ★ 组 A 淡到 0 ⇒ 它子树里**每一条通道**（MR 材质 α / TMP 顶点 α）都 = 0"
                + "｜🧨 只管材质、漏掉 TMP 角标 ⇒ 这里会剩 1（「该藏的时候没藏住」）"
                + (zBad.Length == 0 ? "" : "；没归零：" + zBad));
            v.SetFadeGroupAlpha((CardView.FadeGroup)A, 1f);

            // ---- E10 回归保护：**逐组设 0.5** 与**旧的整卡淡入 `SetBadgeAlpha(0.5)`** 结果逐位相同 ----
            for (int g = 0; g < 7; g++) v.SetFadeGroupAlpha((CardView.FadeGroup)g, 0.5f);
            var perGroup = new float[7, MaxK];
            for (int g = 0; g < 7; g++)
                for (int k = 0; k < MaxK; k++) perGroup[g, k] = v.FadeGroupMemberAlpha((CardView.FadeGroup)g, k);
            for (int g = 0; g < 7; g++) v.SetFadeGroupAlpha((CardView.FadeGroup)g, 1f);
            v.SetBadgeAlpha(0.5f);                                   // ← 旧口：原版 `FadeAllTraitsIcons` 的瞬时版
            var regBad = "";
            for (int g = 0; g < 7; g++)
                for (int k = 0; k < MaxK; k++)
                {
                    if (perGroup[g, k] < 0f) continue;
                    float now = v.FadeGroupMemberAlpha((CardView.FadeGroup)g, k);
                    if (Mathf.Abs(now - perGroup[g, k]) > 1e-6f)
                        regBad += wantNames[g] + "." + v.FadeGroupMemberName((CardView.FadeGroup)g, k) + " ";
                }
            Check(regBad.Length == 0,
                  "E10 ★ **回归保护**：`SetBadgeAlpha(0.5)`（原版 `FadeAllTraitsIcons`）= 把 7 个 trait 组根逐个设 0.5，"
                + "两条路的结果**逐位相同**（7 组 × 4 件全比）"
                + (regBad.Length == 0 ? "" : "；不一致：" + regBad));
            Check(Mathf.Abs(v.BadgeAlpha - 0.5f) <= 1e-6f,
                  "E10b 旧读口 `BadgeAlpha` 仍报统一命令的值（实得 " + v.BadgeAlpha.ToString("F4") + "）—— 旧调用点零影响");

            // ---- E11 数值组：只淡自己那一组 ----
            if (atkPrem && hpPrem)
            {
                float melee0 = baseline[7, 0], range0 = baseline[7, 1], hp0 = baseline[8, 0];
                v.SetFadeGroupAlpha((CardView.FadeGroup)7, 0.5f);    // Base Attack Counters
                float melee1 = v.FadeGroupMemberAlpha((CardView.FadeGroup)7, 0);
                float range1 = v.FadeGroupMemberAlpha((CardView.FadeGroup)7, 1);
                float hp1 = v.FadeGroupMemberAlpha((CardView.FadeGroup)8, 0);
                Check(Mathf.Abs(melee1 - melee0 * 0.5f) <= 0.02f && Mathf.Abs(range1 - range0 * 0.5f) <= 0.02f,
                      "E11 ★ `Base Attack Counters` 淡到 0.5 ⇒ 近战 / 远程那两格 α 减半（" + melee0.ToString("F4")
                    + "→" + melee1.ToString("F4") + " · " + range0.ToString("F4") + "→" + range1.ToString("F4") + "）");
                Check(Mathf.Abs(hp1 - hp0) <= 1e-6f,
                      "E11b ★ 同一拍 `Base Health Counters`（生命）**一点没动**（" + hp0.ToString("F4")
                    + "→" + hp1.ToString("F4") + "）—— 两个组根互不影响");
                v.SetFadeGroupAlpha((CardView.FadeGroup)8, 0.5f);    // Base Health Counters
                float hp2 = v.FadeGroupMemberAlpha((CardView.FadeGroup)8, 0);
                float melee2 = v.FadeGroupMemberAlpha((CardView.FadeGroup)7, 0);
                Check(Mathf.Abs(hp2 - hp0 * 0.5f) <= 0.02f && Mathf.Abs(melee2 - melee1) <= 1e-6f,
                      "E11c ★ 反过来淡 `Base Health Counters` ⇒ 生命减半（" + hp0.ToString("F4") + "→" + hp2.ToString("F4")
                    + "），近战保持上一拍的值（" + melee1.ToString("F4") + "→" + melee2.ToString("F4") + "）");
            }
            else Check(false, "E11 前提不成立（数值层没建）⇒ 「数值组只淡自己」没有验到（⛔ 不是通过）");

            // ---- E12 复位：全部组设回 1 ⇒ 每一件都回到基线 ----
            for (int g = 0; g < CardView.FadeGroupCount; g++) v.SetFadeGroupAlpha((CardView.FadeGroup)g, 1f);
            v.SetBadgeAlpha(1f);
            var resetBad = "";
            for (int g = 0; g < CardView.FadeGroupCount; g++)
                for (int k = 0; k < MaxK; k++)
                {
                    if (baseline[g, k] < 0f) continue;
                    float now = v.FadeGroupMemberAlpha((CardView.FadeGroup)g, k);
                    if (Mathf.Abs(now - baseline[g, k]) > 1e-6f)
                        resetBad += wantNames[g] + "." + v.FadeGroupMemberName((CardView.FadeGroup)g, k) + " ";
                }
            Check(resetBad.Length == 0,
                  "E12 ★ 9 个组根全部设回 1 ⇒ 每一件 α 都回到基线（**没有残留**，也没有叠乘）"
                + "｜🧨 相对乘法写成逐帧叠乘 ⇒ 这条红"
                + (resetBad.Length == 0 ? "" : "；没回位：" + resetBad));
        }
        catch (System.Exception e)
        {
            Check(false, "E 组抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            CardTween.Mode = keepMode;
            Object.DestroyImmediate(root);
        }
    }
    // ==================================================================
    //  F · 账 `HUD:108` / `HUD:153` / `HUD:148` / `HUD:189`（`Core/CardView.cs`，K-03 五条里的四条）
    // ==================================================================
    //
    // 🔴 判据（一手：反编译方法体逐行 + 原版材质 JSON 实读），**期望值全部写死成原版字面量**：
    //   · `HUD:108` `decomp_full/BattleCardUI__ChooseDissolveMaterial.c` 全文 +
    //     `CardScript__HasToUseLegendarySummonEffect.c` 全文 ⇒ `!HasCurrentTrait(ambush 0x4B0) && cardRarity == 4`
    //     （`CardRarity.Legendary = 4`，`Assembly-CSharp/CardRarity.cs`）⇒ 传说那份；
    //     两份原版材质唯一差别 = `_BorderColor` / `_BorderColor2`
    //     （`bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-6651017123104198842.json` = 普通
    //      `Card 3d WH40K Dissolve` · `…Material_-1409023286357856989.json` = 传说 `… Legendary`）。
    //   · `HUD:153` `BattleCardUI__ShowOriginalRemnantCard.c` 全文 + `SupportMethods__ShouldShowOriginalRemnantCard.c`
    //     全文 ⇒ `body3D.SetActive(show && isRemnant(+0x65) && cardArmy ∈ {Sautekh 40, SaimHann 30})`。
    //     ⚠️ 原版 `show == false` 那一支**没有闸**；我们在它之前加了一道纯防御的早退（代码里已标 🔶 自己设计）
    //     ⇒ 反例① 就钉「非残骸卡一个开关都不动」。
    //   · `HUD:148` `BattleCardUI__ShowAvailableTargets.c:1-133` ⇒ 末句 `String.Concat(desc, "<i>", 那段, "</i>")`
    //     （**无空格**；两个 DAT 就是 `<i>` / `</i>`）。
    //   · `HUD:189` `BattleCardUI__ChangedLanguage.c` 全文（`SetCardTextsExcludingStats(rawCardData)` +
    //     `cardState == inHand ⇒ SetCardTextInHand(true)`）+ `BattleHud__BroadcastChangedLanguage.c`
    //     ⇒ **四颗 TMP** 按新语档重取、**数值一个字节不动**（`ExcludingStats`）。
    //
    // ⚠️ 本组**没有**跑过 Unity（子代理不许 `-executeMethod`）⇒ 每条断言都先写**前提硬检查**：
    //    前提不成立会**红**（⛔ 不是静默跳过），日志里能一眼看出是「环境缺件」还是「实现坏了」。
    static void RunK03Four(List<CardDef> pool)
    {
        Debug.Log("[E5] --- K-03 五条（CardView）：HUD:108 / HUD:153 / HUD:148 / HUD:189 ---");
        RunK03DissolveLegendary(pool);
        RunK03Remnant(pool);
        RunK03DescSuffix(pool);
        RunK03Language(pool);
    }

    /// <summary>`HUD:108` 原版 `BattleCardUI.ChooseDissolveMaterial()`：传说档 = 那两项颜色 + 材质名。
    /// 三个观测点（施工单要求：「两条反例都要读，防恒真」）：传说单位 → 传说那份 / 伏击（面朝下）→ 普通 /
    /// 普通稀有度 → 普通；外加**灭自证**：翻回正面再溶解必须回到传说档（缓存过档位 ⇒ 红）。</summary>
    static void RunK03DissolveLegendary(List<CardDef> pool)
    {
        // 原版材质 JSON 的 `m_Colors` 原值（⛔ 不读 `CardView` 的常量再和自己比）
        var legC1 = new Vector4(2.6390156745910645f, 2.4975879192352295f, 0.7184754610061646f, 0f);
        var legC2 = new Vector4(0.5764706134796143f, 0.10125879943370819f, 0f, 0f);
        var norC1 = new Vector4(0.714086651802063f, 1.91457998752594f, 2.6390156745910645f, 0f);
        var norC2 = new Vector4(0f, 0.028591640293598175f, 0.5764706134796143f, 0f);
        const string nameLeg = "Card 3d WH40K Dissolve Legendary";
        const string nameNor = "Card 3d WH40K Dissolve";

        CardDef leg = null, com = null;
        foreach (var d in pool)
        {
            if (d == null || d.Type != "unit") continue;
            if (leg == null && (d.Rarity ?? "").ToLowerInvariant() == "legendary") leg = d;
            if (com == null && (d.Rarity ?? "").ToLowerInvariant() == "common") com = d;
        }
        Check(leg != null && com != null,
              "K03/108 前提：样本齐（传说单位 + 普通单位；实得 leg=" + (leg != null) + " common=" + (com != null) + "）");
        if (leg == null || com == null) return;

        var root = new GameObject("E5_K03_108_root");
        try
        {
            var a = CardView.Create(root.transform, BattleDriver.ToCardData(leg, leg.Faction), "K03Leg", CardFace.Board);
            var m = a != null ? a.DissolveMatView : null;
            Check(a != null && m != null,
                  "K03/108 前提：传说卡（「" + leg.Name + "」）建出了溶解材质（取不到原版 shader ⇒ 本组判不了，⛔ 不是通过）");
            if (m == null) return;

            Check(a.HasToUseLegendarySummonEffect, "K03/108 传说单位（非伏击）⇒ `HasToUseLegendarySummonEffect` 为真");
            Check(m.name == nameLeg, "K03/108 ★ 材质名 = 原版资产名 `" + nameLeg + "`（实得 `" + m.name + "`）");
            Check(Vector4.Distance(m.GetVector("_BorderColor"), legC1) < 1e-3f
                  && Vector4.Distance(m.GetVector("_BorderColor2"), legC2) < 1e-3f,
                  "K03/108 ★ 两项颜色 = 传说材质 JSON 原值（`_BorderColor` 金 / `_BorderColor2` 橙红）"
                + "｜现读 " + m.GetVector("_BorderColor") + " / " + m.GetVector("_BorderColor2"));
            Check(a.BeginDissolve() && a.OnDissolveMaterial, "K03/108 `BeginDissolve` 真把溶解材质挂到了 3D 卡体上");
            Check(a.DissolveMatView.name == nameLeg, "K03/108 ★ `BeginDissolve` 那一跳仍是**传说**档（实得 `"
                  + a.DissolveMatView.name + "`）");

            // 反例①：伏击 ⇒ 原版 `HasCurrentTrait(ambush 0x4B0)` 那一半
            a.SetAmbush(true, false);
            Check(!a.HasToUseLegendarySummonEffect, "K03/108 反例①：伏击（面朝下）的**传说**卡 ⇒ 判据为假");
            a.BeginDissolve();
            Check(a.DissolveMatView.name == nameNor, "K03/108 ★ 反例① 材质名 = **普通**那份（实得 `"
                  + a.DissolveMatView.name + "`）｜🧨 漏掉伏击那一半 ⇒ 红");

            // 灭自证：翻回正面再溶解 ⇒ 必须回到传说档
            a.SetAmbush(false, false);
            a.BeginDissolve();
            Check(a.DissolveMatView.name == nameLeg,
                  "K03/108 ★ 灭自证：翻回正面再 `BeginDissolve` ⇒ 回到**传说**档（实得 `"
                  + a.DissolveMatView.name + "`）｜🧨 「材质只建一次、档位缓存住」⇒ 这里红");

            // 反例②：普通稀有度
            var b = CardView.Create(root.transform, BattleDriver.ToCardData(com, com.Faction), "K03Com", CardFace.Board);
            var bm = b != null ? b.DissolveMatView : null;
            Check(b != null && bm != null, "K03/108 前提：普通卡（「" + com.Name + "」）也建出了溶解材质");
            if (bm != null)
            {
                Check(!b.HasToUseLegendarySummonEffect, "K03/108 反例②：普通稀有度 ⇒ 判据为假");
                Check(bm.name == nameNor, "K03/108 ★ 反例② 材质名 = 普通那份（实得 `" + bm.name + "`）");
                Check(Vector4.Distance(bm.GetVector("_BorderColor"), norC1) < 1e-3f
                      && Vector4.Distance(bm.GetVector("_BorderColor2"), norC2) < 1e-3f,
                      "K03/108 ★ 反例② 两项颜色 = 普通材质 JSON 原值｜现读 "
                    + bm.GetVector("_BorderColor") + " / " + bm.GetVector("_BorderColor2"));
            }
        }
        catch (System.Exception e)
        {
            Check(false, "K03/108 抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message);
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>`HUD:153` 原版 `BattleCardUI.ShowOriginalRemnantCard(bool)`：三条件
    /// `show && isRemnant && 阵营 ∈ {SaimHann, Sautekh}`。四格全读 + 两条反例 + 一条「两处开关不互相覆盖」。</summary>
    static void RunK03Remnant(List<CardDef> pool)
    {
        CardDef ae = null, other = null;
        foreach (var d in pool)
        {
            if (d == null || d.Type != "unit") continue;
            if (ae == null && d.Faction == "SaimHann") ae = d;
            if (other == null && d.Faction != "SaimHann" && d.Faction != "Sautekh") other = d;
        }
        Check(ae != null && other != null,
              "K03/153 前提：样本齐（灵族 + 非那两族；实得 ae=" + (ae != null) + " other=" + (other != null) + "）");
        if (ae == null || other == null) return;

        // 阵营闸（`SupportMethods.ShouldShowOriginalRemnantCard` 的后半句；枚举值 30 / 40）
        Check(CardView.HasOriginalRemnantCardArmy("SaimHann")
              && CardView.HasOriginalRemnantCardArmy("Sautekh")
              && !CardView.HasOriginalRemnantCardArmy("Ultramarines")
              && !CardView.HasOriginalRemnantCardArmy(null),
              "K03/153 阵营闸：只 `SaimHann`(30) / `Sautekh`(40) 两族放行，其余（含 null）不放行");

        var root = new GameObject("E5_K03_153_root");
        try
        {
            var v = CardView.Create(root.transform, BattleDriver.ToCardData(ae, ae.Faction), "K03Rem", CardFace.Board);
            Check(v != null && v.Body3DVisible, "K03/153 前提：场上有 3D 卡身（「" + ae.Name + "」/ " + ae.Faction + "）");
            if (v == null) return;
            v.SetRemnantBody(true, "RemnantBody3D Aeldari");
            Check(v.RemnantBodyVisible,
                  "K03/153 前提：残骸体建出来了（效果库里没有 `RemnantBody3D Aeldari` ⇒ 本组判不了，⛔ 不是通过）");
            if (!v.RemnantBodyVisible) return;

            Check(v.ShowOriginalRemnantCard(true), "K03/153 ★ 灵族残骸 + show=true ⇒ 亮出原卡卡身（返回真）");
            Check(v.OriginalRemnantCardShown && v.Body3DVisible, "K03/153 ★ …锁存为真、且 `_body3D` 真的可见");
            v.SetRemnantBody(true, "RemnantBody3D Aeldari");   // = `BattleDriver.SyncBoard` 每次刷新那一跳
            Check(v.Body3DVisible,
                  "K03/153 ★ 灭自证：棋盘刷新那一跳（`SetRemnantBody` 再来一次）**不许把刚亮的卡身关回去**"
                + "（两处开关互相覆盖 ⇒ 红）");
            Check(!v.ShowOriginalRemnantCard(false) && !v.OriginalRemnantCardShown && !v.Body3DVisible,
                  "K03/153 show=false ⇒ 收回（返回假 / 锁存清零 / 卡身关）");

            // 反例①：**非残骸** ⇒ 一个开关都不动（原版这一支没有闸；我们的早退是 🔶 防御，必须不改画面）
            var n = CardView.Create(root.transform, BattleDriver.ToCardData(ae, ae.Faction), "K03Norm", CardFace.Board);
            Check(n != null && !n.RemnantBodyVisible, "K03/153 前提：反例①那张**不是残骸**（没建残骸体）");
            if (n != null)
            {
                bool wasBody = n.Body3DVisible;
                Check(!n.ShowOriginalRemnantCard(true) && !n.OriginalRemnantCardShown && n.Body3DVisible == wasBody,
                      "K03/153 反例①：非残骸 ⇒ 返回假、`_body3D` **一个开关都不动**"
                    + "（⛔ 不许把正常卡的卡身关没；实得 `Body3DVisible` " + wasBody + "→" + n.Body3DVisible + "）");
            }

            // 反例②：残骸体在、但阵营不在那两族 ⇒ 闸不放行
            var o = CardView.Create(root.transform, BattleDriver.ToCardData(other, other.Faction), "K03Other", CardFace.Board);
            if (o != null) o.SetRemnantBody(true, "RemnantBody3D Aeldari");
            Check(o != null && o.RemnantBodyVisible,
                  "K03/153 前提：反例②那张也挂上了残骸体（「" + other.Name + "」/ " + other.Faction + "）");
            if (o != null && o.RemnantBodyVisible)
                Check(!o.ShowOriginalRemnantCard(true) && !o.OriginalRemnantCardShown && !o.Body3DVisible,
                      "K03/153 反例②：残骸但阵营不在 {SaimHann, Sautekh} ⇒ **不亮**原卡卡身"
                    + "｜🧨 阵营闸写成恒真 ⇒ 红");
        }
        catch (System.Exception e)
        {
            Check(false, "K03/153 抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message);
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>`HUD:148` 的**写口**那一半：`CardView.SetDescriptionSuffix` 必须把「可用目标」那段
    /// 拼在**描述尾部**（`desc + "<i>" + 那段 + "</i>"`，无空格），并且**与刷新/复原两条路都不打架**。</summary>
    static void RunK03DescSuffix(List<CardDef> pool)
    {
        var sample = FindByName(pool, "Howling Banshee Exarch");
        Check(sample != null, "K03/148 前提：找到样本卡 `Howling Banshee Exarch`");
        if (sample == null) return;

        var root = new GameObject("E5_K03_148_root");
        try
        {
            var v = CardView.Create(root.transform, BattleDriver.ToCardData(sample, sample.Faction), "K03Desc");
            Check(v != null && v.KeywordsText != null,
                  "K03/148 前提：非场上形态建出了**效果文字那颗 TMP**（`TmpFont` 不可用 ⇒ 本组判不了，⛔ 不是通过）");
            if (v == null || v.KeywordsText == null) return;

            string bare = v.KeywordsText.text;
            Check(v.DescriptionSuffix == null, "K03/148 初始态：没有后缀（`DescriptionSuffix == null`）");
            Check(!string.IsNullOrEmpty(bare), "K03/148 前提：裸描述非空（这张卡本来就有正文）");
            if (string.IsNullOrEmpty(bare)) return;

            v.SetDescriptionSuffix("(Boss Nob)");
            Check(v.DescriptionSuffix == "(Boss Nob)", "K03/148 写口记下了后缀");
            Check(v.KeywordsText.text == bare + "<i>(Boss Nob)</i>",
                  "K03/148 ★ 描述尾部 = 原版那句 `desc + \"<i>\" + 那段 + \"</i>\"`（**无空格**）"
                + "｜现读 `" + v.KeywordsText.text + "`");
            string withSuffix = v.KeywordsText.text;

            v.SetDescriptionSuffix("(Boss Nob)");
            Check(v.KeywordsText.text == withSuffix, "K03/148 幂等：同一个后缀再调一次 ⇒ 文本逐字不变（不重复追加）");

            v.SetData(BattleDriver.ToCardData(sample, sample.Faction));   // = 战场每次刷新走的那条路
            Check(v.KeywordsText != null && v.KeywordsText.text == withSuffix,
                  "K03/148 ★ 灭自证：`SetData`（每刷一次棋盘都走）之后后缀**仍在**"
                + "｜🧨 只在建层时拼一次、`SetData` 那条路漏掉 ⇒ 红");

            v.SetDescriptionSuffix(null);
            Check(v.DescriptionSuffix == null && v.KeywordsText.text == bare, "K03/148 复原：传 null ⇒ 回到**裸描述**（逐字相同）");

            // 场上形态没有文字层 ⇒ 写口是空操作（⛔ 别为它硬建一层）
            var b = CardView.Create(root.transform, BattleDriver.ToCardData(sample, sample.Faction), "K03DescBoard", CardFace.Board);
            Check(b != null, "K03/148 前提：场上形态那张建出来了");
            if (b != null)
            {
                b.SetDescriptionSuffix("(x)");
                Check(b.KeywordsText == null, "K03/148 场上形态没有文字层 ⇒ 写口空操作（一个层都不建）");
            }
        }
        catch (System.Exception e)
        {
            Check(false, "K03/148 抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message);
        }
        finally { Object.DestroyImmediate(root); }
    }

    /// <summary>`HUD:189` 原版 `BattleCardUI.ChangedLanguage()`（`BattleHud.BroadcastChangedLanguage` 的实现者）：
    /// 换语档 ⇒ `SetCardTextsExcludingStats` 重写**四颗 TMP**（卡名/描述/兵种/阵营），**数值一个字节不动**。</summary>
    static void RunK03Language(List<CardDef> pool)
    {
        // 样本：中英两列都有值、且**真的不同**的那张（前提写死；⛔ 别拿「表里没值」的卡判）
        CardDef sample = null;
        foreach (var d in pool)
        {
            if (d == null) continue;
            if (string.IsNullOrEmpty(d.Name) || string.IsNullOrEmpty(d.NameZh) || d.NameZh == d.Name) continue;
            if (string.IsNullOrEmpty(d.Desc) || string.IsNullOrEmpty(d.DescZh) || d.DescZh == d.Desc) continue;
            sample = d; break;
        }
        Check(sample != null, "K03/189 前提：找到中英两列都有值且不同的样本（实得 "
              + (sample != null ? sample.Name : "null") + "）");
        if (sample == null) return;

        var keepLang = Loc.Current;
        bool keepPersist = Loc.PersistOverride;
        var root = new GameObject("E5_K03_189_root");
        try
        {
            Loc.PersistOverride = true;              // 只改内存（自检不许动玩家的真设置）
            Loc.SetLanguage(AvailableLanguages.English);
            Check(!CardText.Zh, "K03/189 前提：英文档下 `CardText.Zh == false`（否则下面的「变了」判不出来）");

            var v = CardView.Create(root.transform, BattleDriver.ToCardData(sample, sample.Faction), "K03Loc");
            Check(v != null && v.TitleText != null && v.KeywordsText != null,
                  "K03/189 前提：卡名 / 描述两颗 TMP 建出来了");
            if (v == null || v.TitleText == null || v.KeywordsText == null) return;

            string enTitle = v.TitleText.text, enDesc = v.KeywordsText.text;
            string enRace = v.RaceText, enArmy = v.ArmyText;
            var statsBefore = new string[5];
            for (int s = CardView.StatMelee; s <= CardView.StatHealth; s++) statsBefore[s] = v.FaceStatTextNow(s);

            Loc.SetLanguage(AvailableLanguages.Chinese);
            Check(CardText.Zh, "K03/189 前提：中文档下 `CardText.Zh == true`（中文语档 + 中文字体都在）");
            Check(v.RefreshLocalizedText(), "K03/189 ★ `RefreshLocalizedText()` 返回真（= 真重建了文字层）");

            Check(v.TitleText.text != enTitle && !string.IsNullOrEmpty(v.TitleText.text),
                  "K03/189 ★ 卡名按新语档重取（英 `" + enTitle + "` → 中 `" + v.TitleText.text + "`）");
            Check(v.KeywordsText.text != enDesc,
                  "K03/189 ★ 描述按新语档重取（长度 " + (enDesc ?? "").Length + " → "
                + (v.KeywordsText == null ? -1 : v.KeywordsText.text.Length) + "）");

            bool statSame = true; string statDiff = "";
            for (int s = CardView.StatMelee; s <= CardView.StatHealth; s++)
            {
                string now = v.FaceStatTextNow(s);
                if (now != statsBefore[s]) { statSame = false; statDiff += s + ":`" + statsBefore[s] + "`→`" + now + "` "; }
            }
            Check(statSame,
                  "K03/189 ★ 数值四格**逐字节不变**（原版 `SetCardTextsExcludingStats` 的 `ExcludingStats`）"
                + "｜🧨 顺手重烘数值层 ⇒ 红" + (statSame ? "" : "；变了：" + statDiff));

            Debug.Log("[E5] K03/189 兵种行 `" + enRace + "` → `" + v.RaceText + "`；阵营行 `" + enArmy + "` → `" + v.ArmyText + "`");
            Check(!string.IsNullOrEmpty(v.RaceText) || string.IsNullOrEmpty(enRace),
                  "K03/189 兵种行重建后**没有丢**（`" + v.RaceText + "`）");
            Check(!string.IsNullOrEmpty(v.ArmyText) || string.IsNullOrEmpty(enArmy),
                  "K03/189 阵营行重建后**没有丢**（`" + v.ArmyText + "`）");
        }
        catch (System.Exception e)
        {
            Check(false, "K03/189 抛异常（后面的断言**没跑到**，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message);
        }
        finally
        {
            Loc.RestoreForTest(keepLang);            // ⛔ 不动 `PlayerPrefs`
            Loc.PersistOverride = keepPersist;
            Object.DestroyImmediate(root);
        }
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
