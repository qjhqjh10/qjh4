// B120_W15_Assert.cs — 执行写手 `B120-W15`（2026-11-02）· 账 `D118-64` / `D118-66` / `B120-Q04d`
//
// ============================ 它盯的是什么 ============================
// 三件互不相干的「口径对齐」，共同点是**都靠判据而不是靠手感**：
//   ① `D118-64`  满手抽牌整段建模：引擎发一条 `EvtKind.Draw` + `Effect == "handFull"` 的信号，
//                表现层演「飞牌 → 抬头提示（**仅我方**）→ 等 0.5 s → 送墓地」三拍。
//   ② `D118-66`  ① 一次抽 2+ 张的间隔 = `timeToPositionCard(0.2) + TimeToDrawCard`
//                （我方 0.5 / 敌方 0.35 s）；② 抽卡**起点** = 原版 `DeckManager.cardBackReference`
//                那个参考件（(1711.3, 90) / (1688.2, 1011.4) px），⛔ 不是牌堆底板中心。
//   ③ `B120-Q04d` `HitSlotHitCountForTest` 与 `HitSlot` **跑同一份生产码**（3D 档 = 同一条射线链）。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · `d:/2/tools/decomp_full/PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c`（state0/1/2 全文，本次现读）
//   · `d:/2/tools/decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c:233-249`
//     （`WaitForSeconds(fVar2 + fVar11)`；`fVar2 = *(float*)(globalVars + 0xa4) = timeToPositionCard`）
//   · `d:/2/tools/il2cpp_out/dump.cs:120112` `VarsGlobal + 0xA4 = timeToPositionCard`
//     · 值 0.2 → `资料/VarsGlobal_原版数值.md` §一
//   · `d:/2/tools/decomp_full/CardScript__SetInDeckPosition.c`（把 `DeckReferenceObject` 的 local 位姿拷给卡）
//     + `DeckManager__get_DeckReferenceObject.c`（= `cardBackReference.transform`，`DeckManager + 0x20`）
//     + `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_{4452,4681}.json` 的 `cardBackReference`
//       → `Transform_{1334,1369}.json` 的 `m_LocalPosition` → 父 `RectTransform_{3292,2964}.json`
//   · `d:/2/tools/decomp_full/BattleManager__GetTargetInPos.c:250-265`（射线链；判据全文见 `B120_W13_Assert`）
//   · `d:/2/tools/decomp_full/CardScript__SendToCemetery.c`（`DissolveWholeCard(timeToDissolveCard 0.2)` + `GoToCemetery`）
//
// ============================ 怎么用 ============================
//   `B120_W15_Assert.Run();`   （静态类 + 无参 `Run()`）
//   **接线位**：`Editor/BattleScene.cs` 里「`B120_W13_Assert` 那一段之后」（那一处已经有 `BattleDriver drv`
//   与打起来的一局）。A/B 两组是纯常量 + 源码闸，不需要驱动；**C 组需要一局真打起来的 `BattleDriver`**。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A1 ← 把 `HitSlotHitCountForTest` 的 3D 档改回「逐卡投影 `CardHit`」⇒ 方法体里不再出现
//          `TryHitSlotByBoardRay` ⇒ 红（这正是 `B120-Q04d` 那个缺陷的形状）
//   · A2 ← 把 `TryHitSlotByBoardRay` 的 `out int cardHitCount` 删掉 / 把 `return found` 改回 `return true`
//          （提前 return ⇒ 计数永远 ≤ 1）⇒ 红
//   · A3 ← 把 `RuleCore` 里 `effect: "handFull"` 那一句删掉，或把它抬进 `AddCardToHandWithinLimit`
//          （那条也服务**造牌**，原版那条路没有抬头提示）⇒ 红
//   · A4 ← 把 `PlayHandFullDrawFeel` 里 `mine` 那道闸拆掉（敌方也弹提示）/ 删掉 `DissolveInHand` 那一拍 ⇒ 红
//   · B2 ← 把 `DrawBeatSeconds` 改回「只算 `DealSeconds`」（= 本轮修的那个错）⇒ 0.3/0.15 ⇒ 红
//   · B5 ← 把抽牌那 0.5 系数塞回 `DealStaggerDelay`（`D118-66` ① 的原错）⇒ 红
//   · B6 ← 把 `DrawStartWorld` 改回牌堆底板中心 `MyDeckX01/MyDeckY01` ⇒ 与「参考件」那对 px 差开 ⇒ 红
//   · C2 ← 给计数或槽号任一侧加「没命中就兜底」⇒ 红
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环、不跑动画**：A/B 是源码闸 + 常量；C 只用**当帧的命中读数**。
//   · **不断言「真机上飞牌好不好看」**：那要真 Play（`资料/真Play待验清单.md`）。
//   · **B7 的「同父」前提未核**：`SetInDeckPosition` 拷的是 **localPosition**，绝对换算依赖
//     「卡的父变换 == 参考件的父」；本件只钉**我们用的那对常量与参考件的 local 值一致**，
//     真机上若飞牌起点偏，先回源核那条（⛔ 别改常量去凑）。
//   · **A3 的源码闸只认 `RuleCore.cs` 里那一句的字面形状**：⛔ 不从我们的行为反推原版。
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

/// <summary>执行写手 `B120-W15`：满手抽牌演出链 / 抽牌节奏与起点 / `HitSlotHitCountForTest` 同源。</summary>
public static class B120_W15_Assert
{
    const string Prefix = "[B120W15] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    /// <summary>只记录、不计分（「前提读数」这类**不是断言**的量）。</summary>
    static void Tr(string what) { Debug.Log(Prefix + "· " + what); }

    /// <summary>本件唯一入口。</summary>
    public static void Run(BattleDriver drv = null)
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B120-W15：满手抽牌 / 抽牌节奏与起点 / 命中计数同源 开始 ===");
        A_SourceGate();
        B_Constants();
        C_SameSource(drv);
        Debug.Log(Prefix + "—— 合计：通过 " + _ok + " · 失败 " + _bad);
    }

    // ==================================================================
    //  A · 源码闸（形状；⛔ 不用我们的常量反推原版）
    // ==================================================================
    static void A_SourceGate()
    {
        string drvPath = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        string corePath = Path.Combine(Application.dataPath, "RuleEngine/Core/RuleCore.cs");
        Check(File.Exists(drvPath), "A0（前提）`BattleDriver.cs` 在：" + drvPath + "（⛔ 读不到就红，别改成跳过）");
        Check(File.Exists(corePath), "A0b（前提）`RuleCore.cs` 在：" + corePath);
        if (!File.Exists(drvPath) || !File.Exists(corePath)) return;

        string src = StripLineComments(File.ReadAllText(drvPath));
        string core = StripLineComments(File.ReadAllText(corePath));

        // ---- A1：`HitSlotHitCountForTest` 的 3D 档必须委托生产射线链 ----
        string body = ExtractMethodBody(src, "public int HitSlotHitCountForTest(int side, Vector3 world)");
        Check(body != null, "A1a（前提）在源码里找得到 `HitSlotHitCountForTest` 的方法体（找不到 ⇒ 改名了 ⇒ 红）");
        if (body != null)
        {
            string flatBody = Flatten(body);
            Check(flatBody.Contains("TryHitSlotByBoardRay"),
                  "A1 ★ `HitSlotHitCountForTest` 的 3D 档跑的是**同一条生产射线链** `TryHitSlotByBoardRay`"
                + "（`B120-Q04d`：改前它只逐卡投影 `CardHit`，而 `HitSlot` 已改射线 ⇒ 两条口径分家）"
                + "｜🧨 改回逐卡投影 ⇒ 方法体里就没有这个名字了 ⇒ 红");
            Check(flatBody.Contains("CardHit"),
                  "A1b `HitSlotHitCountForTest` 仍保留 **2D 烘图兜底档** 的逐卡 `CardHit` 分支"
                + "（那一档 `HitSlot` 用的也是同一谓词，⛔ 不是第二套判据）");
        }

        // ---- A2：射线链的计数出参 + 不再提前 return ----
        string ray = ExtractMethodBody(src, "bool TryHitSlotByBoardRay(Dictionary<int, CardView> views, Vector3 world, out int slot, out int cardHitCount)");
        Check(ray != null, "A2a（前提）`TryHitSlotByBoardRay` 的签名带 `out int cardHitCount`（去掉 ⇒ 找不到 ⇒ 红）");
        if (ray != null)
        {
            string flatRay = Flatten(ray);
            Check(flatRay.Contains("_boardRayHits[0]"),
                  "A2 ★ 射线链**只取缓冲第 0 条**（= 原版 `RaycastNonAlloc` 的 `hits[0]`；2026-11-03 `B120-W27` 层 10 上只有卡后回到字面形状）");
            Check(!flatRay.Contains("for ("),
                  "A2b 方法体里**不许再有循环**（「扫完缓冲去重」的变通已撤）");
            Check(flatRay.Contains("cardHitCount = 1"),
                  "A2c 计数出参语义 = **首条命中**（0/1，不再按卡去重）");
        }

        // ---- A3：引擎侧那条信号的发出点（且不在造牌那条共用口里）----
        string addBody = ExtractMethodBody(core, "public static bool AddCardToHandWithinLimit(BattleContext ctx, int p, CardInstance inst)");
        Check(addBody != null, "A3a（前提）在 `RuleCore.cs` 里找得到 `AddCardToHandWithinLimit`（找不到 ⇒ 改名了 ⇒ 红）");
        Check(core.Contains("effect: \"handFull\""),
              "A3 ★ `RuleCore` 里发出 `EvtKind.Draw` + `effect: \"handFull\"` 那条信号（满手抽牌的唯一发出点）"
            + "｜🧨 删掉它 ⇒ 表现层再也拿不到那三拍 ⇒ 红");
        Check(addBody != null && !Flatten(addBody).Contains("handFull"),
              "A3b 那条信号**不在** `AddCardToHandWithinLimit` 里 —— 它也服务造牌（`_AddCardNotDrawnToHand`），"
            + "原版那条路**没有**抬头提示 ⇒ 抬进去就是多演一次｜🧨 挪进去 ⇒ 红");

        // ---- A4：演出的三拍 + 只我方提示 ----
        string feel = ExtractMethodBody(src, "void PlayHandFullDrawFeel(BattleEvent e)");
        Check(feel != null, "A4a（前提）在源码里找得到 `PlayHandFullDrawFeel`（找不到 ⇒ 改名了 ⇒ 红）");
        if (feel != null)
        {
            string flatFeel = Flatten(feel);
            Check(flatFeel.Contains("CardFeel.DealInPose"),
                  "A4 ★ 第 1 拍「飞牌」在（`CardFeel.DealInPose`，判据 `CardScript.DrawCard`）");
            Check(flatFeel.Contains("SetHint(HandFullText())"),
                  "A4b ★ 第 2 拍「抬头提示」在（`SetHint(HandFullText())`，词条 `Battle/Tips/HandFull`）");
            Check(flatFeel.Contains("CardFeel.DissolveInHand"),
                  "A4c ★ 第 3 拍「送墓地」在（`CardFeel.DissolveInHand` ⇐ 原版 state2 `SendToCemetery` 的 `DissolveWholeCard`）");
            Check(flatFeel.Contains("HandFullCemeteryHold"),
                  "A4d 第 2 → 第 3 拍之间是 `HandFullCemeteryHold`（= 原版 `WaitForSeconds(DAT_1834b2bb4)` 0.5）");
            Check(flatFeel.Contains("if (mine)") && flatFeel.Contains("else"),
                  "A4e 提示那一段被 `mine`（= 「本机那一侧」）闸住 —— 原版 `PlayerHand + 0x28 (isPlayer)`"
                + "｜🧨 拆掉闸门（敌方也弹）⇒ 红");
            Check(flatFeel.Contains("SchedulePresentationCallback"),
                  "A4f 两拍都走 `SchedulePresentationCallback`（**不动队尾**）—— 原版外层动作 0.5 s 就 `FinishResolvingAction`，"
                + "送墓那一下发生在**动作结束之后**｜🧨 改用 `ScheduleAction`（推队尾）⇒ 红");
        }
    }

    // ==================================================================
    //  B · 常量与数值（判据见文件头；⛔ 不从我们的实现反推原版）
    // ==================================================================
    static void B_Constants()
    {
        // B1 —— 满手抽牌那 0.5 s（原版 `DAT_1834b2bb4` 4B 实读）
        Check(Mathf.Abs(BattleDriver.HandFullCemeteryHold - 0.5f) < 1e-6f,
              $"B1 ★ `HandFullCemeteryHold` = 0.5 s（实得 {BattleDriver.HandFullCemeteryHold:F3}）"
            + "（原版 `PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c` state1 末尾 `WaitForSeconds(DAT_1834b2bb4)`）");

        // B2 —— 一条抽卡动作 = timeToPositionCard + TimeToDrawCard
        float mine = EventTiming.DrawBeatSeconds(true);
        float foe = EventTiming.DrawBeatSeconds(false);
        Check(Mathf.Abs(mine - 0.5f) < 1e-6f && Mathf.Abs(foe - 0.35f) < 1e-6f,
              $"B2 ★ `DrawBeatSeconds` = `timeToPositionCard(0.2) + TimeToDrawCard` ⇒ 我方 0.5 / 敌方 0.35 s"
            + $"（实得 {mine:F3} / {foe:F3}）⋅ 判据 `_ResolveDrawCard_d__432__MoveNext.c:233-249`"
            + "｜🧨 改回「只算 `DealSeconds`」（0.3/0.15）= 本轮修的那个错 ⇒ 红");

        // B3 —— 只按 `EvtKind` 的那条重载给的兜底
        Check(Mathf.Abs(EventTiming.DrawDeal - mine) < 1e-6f,
              $"B3 `EventTiming.DrawDeal` == `DrawBeatSeconds(true)` = {mine:F3}（实得 {EventTiming.DrawDeal:F3}）");

        // B4 —— 逐张错开
        bool staggerOk = Mathf.Abs(BattleDriver.DrawStaggerDelay(true, 0) - 0.0f) < 1e-6f
                      && Mathf.Abs(BattleDriver.DrawStaggerDelay(true, 1) - 0.5f) < 1e-6f
                      && Mathf.Abs(BattleDriver.DrawStaggerDelay(true, 3) - 1.5f) < 1e-6f
                      && Mathf.Abs(BattleDriver.DrawStaggerDelay(false, 1) - 0.35f) < 1e-6f
                      && Mathf.Abs(BattleDriver.DrawStaggerDelay(false, 2) - 0.70f) < 1e-6f;
        Check(staggerOk,
              "B4 ★ 抽牌逐张错开：第 0 张不延时、我方 0.5/张、敌方 0.35/张"
            + $"（实得 我 {BattleDriver.DrawStaggerDelay(true, 1):F3} / 敌 {BattleDriver.DrawStaggerDelay(false, 1):F3} 起）");

        // B5 —— **灭自证**：换牌区填牌那一档必须**不受影响**（它才是 0.5 系数的主人）
        bool mulliganKept = Mathf.Abs(BattleDriver.DealStaggerFrac - 0.5f) < 1e-6f
                         && Mathf.Abs(BattleDriver.DealStaggerDelay(true, 1) - 0.15f) < 1e-6f
                         && Mathf.Abs(BattleDriver.DealStaggerDelay(false, 1) - 0.075f) < 1e-6f;
        Check(mulliganKept,
              "B5 ★ **换牌区填牌**仍是 `get_timeToDraw × 0.5`（我方 0.15 / 敌方 0.075）—— 两档不许互相套用"
            + $"（实得 {BattleDriver.DealStaggerDelay(true, 1):F3} / {BattleDriver.DealStaggerDelay(false, 1):F3}）"
            + "｜🧨 把抽牌那 0.5 s 塞进 `DealStaggerDelay`（`D118-66` ① 的原错）⇒ 红");

        // B6 —— 抽卡起点 = 参考件（⛔ 不是牌堆底板中心）
        Vector3 myStart = BattleDriver.DrawStartWorld(true);
        Vector3 foeStart = BattleDriver.DrawStartWorld(false);
        Vector3 myExpect = LayoutSpace.ToWorld(1711.3f / LayoutSpace.DesignPxW, 90.0f / LayoutSpace.DesignPxH);
        Vector3 foeExpect = LayoutSpace.ToWorld(1688.2f / LayoutSpace.DesignPxW, 1011.4f / LayoutSpace.DesignPxH);
        Check(Vector3.Distance(myStart, myExpect) < 1e-4f && Vector3.Distance(foeStart, foeExpect) < 1e-4f,
              $"B6 ★ 抽卡起点 = 原版 `cardBackReference`：(1711.3, 90.0) / (1688.2, 1011.4) px"
            + $"（实得 我 ({LayoutSpace.PxX(myStart.x):F1}, {LayoutSpace.PxY(myStart.y):F1}) 自左上 / 敌 ({LayoutSpace.PxX(foeStart.x):F1}, {LayoutSpace.PxY(foeStart.y):F1})）");

        // B7 —— **灭自证**：起点必须**不是**牌堆底板中心（1730, 115）
        Vector3 plateCentre = LayoutSpace.ToWorld(1730f / LayoutSpace.DesignPxW, 115f / LayoutSpace.DesignPxH);
        float dMy = Vector3.Distance(myStart, plateCentre) * 108f;   // 换回 px 好读数
        Check(dMy > 1f,
              $"B7 ★ 抽牌起点与牌堆**底板**中心**不是同一个点**（我方差 {dMy:F1} px ≈ √(18.7² + 25²) = 31.2）"
            + "｜🧨 把 `DrawStartWorld` 改回 `MyDeckX01/MyDeckY01` ⇒ 这一条红（底板对齐的是 230²/200² 那个 Image）");
        Tr($"B7 读数：我方起点与底板中心差 {dMy:F2} px（期望 ≈ 31.24）· 敌方起点 ({LayoutSpace.PxX(foeStart.x):F1}, {LayoutSpace.PxY(foeStart.y):F1}) 自左上");
    }

    // ==================================================================
    //  C · 行为：两个命中读口必须同源（需要一局真打起来的 `BattleDriver`）
    // ==================================================================
    static void C_SameSource(BattleDriver drv)
    {
        if (drv == null) drv = UnityEngine.Object.FindFirstObjectByType<BattleDriver>();
        Check(drv != null,
              "C0（前提）取得到 `BattleDriver`（⛔ 取不到就红，不静默跳过）—— 本组要接在有驱动的那一段之后");
        if (drv == null) return;
        Check(drv.Ctx != null, "C0b（前提）这局已经打起来（`Ctx != null`）—— 本组要读棋盘与场上视图");
        if (drv.Ctx == null) return;

        int side = drv.MySideForTest;
        var board = drv.Ctx.Players[side].Board;
        int points = 0;
        for (int s = 0; s < board.Length; s++)
        {
            if (board[s] == null) continue;
            var v = drv.BoardViewForTest(side, s);
            if (v == null) continue;
            Vector3 ptr = drv.PointerAtWorldForTest(v.transform.position);
            int slot = drv.HitSlotForTest(side, ptr);
            int n = drv.HitSlotHitCountForTest(side, ptr);
            Check((slot != -1) == (n >= 1),
                  $"C1 ★ 两个读口**同源**：`HitSlot` 答 {slot}、`HitSlotHitCountForTest` 计 {n}（格 {s}）"
                + " —— 「命中 ⟺ 计数 ≥ 1」必须同时成立"
                + "｜🧨 让计数走另一套判据（改前的逐卡投影）或给任一侧加兜底 ⇒ 红");
            points++;
            if (points >= 2) break;
        }
        Check(points >= 1,
              "C1（前提）至少验到一格（棋盘上没单位 / 没视图 ⇒ 红，别让它空转绿）");

        Vector3 far = new Vector3(-9000f, -9000f, 0f);
        Check(drv.HitSlotForTest(side, far) == -1 && drv.HitSlotHitCountForTest(side, far) == 0,
              "C2 ★ 远离所有卡的指针点：槽号 **-1** 且 计数 **0**（两侧都不许兜底）"
            + "｜🧨 给计数加「没命中就 +1」或给 `HitSlot` 加兜底格 ⇒ 红");
    }

    // ==================================================================
    //  小工具
    // ==================================================================

    /// <summary>去掉整行注释（`//` 之后的内容不算接线）—— 与 `B120_W13_Assert.StripLineComments` 同一口径。</summary>
    static string StripLineComments(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new System.Text.StringBuilder(s.Length);
        var lines = s.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string l = lines[i];
            int k = l.IndexOf("//", StringComparison.Ordinal);
            sb.Append(k >= 0 ? l.Substring(0, k) : l);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>压掉空白（跨行拼接后做子串匹配用）。</summary>
    static string Flatten(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new System.Text.StringBuilder(s.Length);
        bool lastSpace = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) { if (!lastSpace) { sb.Append(' '); lastSpace = true; } }
            else { sb.Append(c); lastSpace = false; }
        }
        return sb.ToString();
    }

    /// <summary>取「含 <paramref name="signatureHint"/> 的那一行」到它那对花括号的收尾（找不到 ⇒ null）。</summary>
    static string ExtractMethodBody(string src, string signatureHint)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(signatureHint)) return null;
        int at = src.IndexOf(signatureHint, StringComparison.Ordinal);
        if (at < 0) return null;
        int open = src.IndexOf('{', at);
        if (open < 0) return null;
        int depth = 0;
        for (int i = open; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}')
            {
                depth--;
                if (depth == 0) return src.Substring(open, i - open + 1);
            }
        }
        return null;
    }
}
