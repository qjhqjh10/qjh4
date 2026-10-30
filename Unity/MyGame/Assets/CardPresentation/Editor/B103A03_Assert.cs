// B103A03_Assert.cs — 执行代理 `B103_A03`（2026-10-30 · 第一百零三会话）
//   AUD03「复用 / 生命周期」本批 **#21 / #77 / #94** 三条的断言。
//   （其余 13 条 #5 #7 #8 #14 #23 #24 #28 #29 #38 #74 #75 #76 #120 的断言已在 `Editor/B103R1_Assert.cs`，
//     ⛔ 本件**不重复**它们 —— 重复断言 = 维护两份、迟早打架。）
//
// ============================ 本件盯的三件 ============================
//  · **#77**（`Battle/CardDisplayWindow.SolidTexFor`）：遮罩纯色贴图从**单值缓存**改成**按宽高比分桶**。
//      🧨 灭自证：改回单值缓存 ⇒「A → B → 再取 A 拿到的是**新**实例」⇒ A3 红（A1/A2 仍绿）。
//  · **#94**（换语言 ⇒ 窗内文案按来源重取，⛔ 不重建整窗）：
//      B 段 = **机制**（`Loc` 广播 ⇒ `Label.Relocalize`；登记者为 0 也不报警）；
//      C 段 = **源码级锁**（5 个窗的 `OnLanguageChanged` 必须走 `Label.RelocalizeUnder`，
//             ⛔ 不许退回 `{ Open(); }` —— 那是整窗重建，滚动位回顶）。
//  · **#21**（`DeckScene` 那三条验收面，施工单 §#21）：`RunDeck(rt)` 需要**宿主那扇真卡组库建出来的窗**
//      （`DeckScene` 的 `_rt`）；①「第二次 Build 后滚动位/抽屉复位」已由 `B103R1_Assert` 的 A4/A5 咬住，本件只补
//      ②「`UiCosmCellCount == CosmoRows × CosmoCols`（**恰好**，不是只断「不增长」）」与
//      ③「滚一格后被保护的槽 == 数据行 − firstRow」（A9 只断过「firstRow == 0」那一档 ⇒ 滚起来那一档没验）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  自包含那部分（A 段 + B 段 + C 段）接进任一宿主的聚合行（同一物理行、**第一个 `//` 之前**）：
//      B103A03_Assert.Run(); _sink.Pass += B103A03_Assert.Pass; _sink.Fail += B103A03_Assert.Fail;
//  带窗那部分接在 **`Editor/DeckScene.cs`**（它手里有 `_rt`，且已经是 `B103R1_Assert` 的宿主）：
//      B103A03_Assert.RunDeck(_rt); _sink.Pass += B103A03_Assert.Pass; _sink.Fail += B103A03_Assert.Fail;
//  ⚠️ `RunDeck` 会**临时**置脏 `rt` 的拖拽/滚动位再原样还原（不改数据、不重建整窗）。
//
// ============================ 口径 ============================
//  · 每条期望值都来自**原版判据或施工单**，⛔ 没有一条是「读我们的实现再断我们自己」。
//  · 前提断言与结论断言分开写：⛔ 不用 `if (x != null) Check(...)` 把断言空转掉（前提不成立时**当场红**）。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B103_A03`：AUD03 #21 / #77 / #94 的断言。</summary>
public static class B103A03_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103A03] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口

    /// <summary>自包含（不建窗、不写盘）：#77 贴图分桶 · #94 机制 + 5 窗源码锁。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B103A03 · AUD03 #77（遮罩贴图分桶）");
        try { A_SolidTexBuckets(); }
        catch (Exception e) { Tr(false, "「#77 贴图分桶」抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }

        MenuCheck.Section(_sink, "B103A03 · AUD03 #94（换语言 ⇒ 按来源重取，⛔ 不重建整窗）");
        try { B_LocalizeChannel(); }
        catch (Exception e) { Tr(false, "「#94 机制」抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { C_WindowOverrides(); }
        catch (Exception e) { Tr(false, "「#94 五窗源码锁」抛异常：" + e.GetType().Name + "：" + e.Message); }

        Debug.Log(_sink.P + "B103A03 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ #77
    static void A_SolidTexBuckets()
    {
        // ---- 前提：两个**互不相同**的宽高比，且都不与常见档（1.0 / 1.77）撞桶 ----
        const float ratioA = 0.37f, ratioB = 0.79f;
        int keyA = Mathf.RoundToInt(ratioA * 100f), keyB = Mathf.RoundToInt(ratioB * 100f);
        Tr(keyA != keyB, "A0-前提①：两个探针宽高比落**不同**的桶（" + keyA + " / " + keyB + "）");

        int c0 = CardDisplayWindow.UiSolidTexBucketCountForTest;
        var t1 = CardDisplayWindow.SolidTexFor(ratioA);
        var t2 = CardDisplayWindow.SolidTexFor(ratioB);
        Tr(t1 != null && t2 != null, "A0-前提②：两次都建出了贴图（为 null ⇒ 下面几条无意义）");
        Tr(!ReferenceEquals(t1, t2), "A1 ★ #77 两个**不同**宽高比**不共用**同一张贴图（旧写法是单值缓存 ⇒ 第二张会覆盖第一张）");

        int c1 = CardDisplayWindow.UiSolidTexBucketCountForTest;
        C(c1, c0 + 2, "A2 ★ #77 两个新宽高比各占一个桶（" + c0 + " → " + c1 + "）");

        var t3 = CardDisplayWindow.SolidTexFor(ratioA);   // 回到第一个宽高比
        Tr(ReferenceEquals(t3, t1),
           "A3 ★ #77（灭自证）回头再取第一个宽高比 ⇒ 拿到的是**第一张的同一个实例**"
         + "｜🧨 改回单值缓存（`_solid` + `_solidAspect`）⇒ 中间那次 ratioB 已把 `_solid` 换掉 ⇒ 这里是一张**新**图 ⇒ 红（而 A1/A2 仍绿：单值缓存两份也「不共用」「不计数」）");
        C(CardDisplayWindow.UiSolidTexBucketCountForTest, c0 + 2,
          "A4 ★ #77 回头取同上一个宽高比**不再新建**（桶数仍 " + (c0 + 2) + "）");

        // ---- 前提：桶数上界是「distinct 宽高比个数」，不是调用次数 ----
        for (int i = 0; i < 5; i++) CardDisplayWindow.SolidTexFor(ratioA);
        C(CardDisplayWindow.UiSolidTexBucketCountForTest, c0 + 2,
          "A5 ★ #77 同一宽高比连取 5 次**桶数不变**（⛔ 不是「每次一张」的泄漏形状）");
    }

    // ============================================================ #94 机制
    static void B_LocalizeChannel()
    {
        const string term = "MainMenu/General/Claim";   // 官方表两列都在、且 en ≠ zh（下方前提会当场核，不靠记忆）
        string en = Loc.Lang13Of(term, "en"), zh = Loc.Lang13Of(term, "zh");
        Tr(!string.IsNullOrEmpty(en) && !string.IsNullOrEmpty(zh),
           "B0-前提①：`" + term + "` 在官方 13 语言表里 en/zh 两列都非空（en=[" + en + "] zh=[" + zh + "]）"
         + " —— 缺一列 ⇒ 下面「换语言真的换了字」那条等于没验");
        Tr(en != zh, "B0-前提②：en 与 zh 的字面**不同**（[" + en + "] ≠ [" + zh + "]）；相同 ⇒ 下面那条空断");

        var go = new GameObject("B103A03_Label");
        try
        {
            var lb = Label.Create(go.transform, "init", Vector3.zero, 3, Color.white, new Vector2(0.5f, 0.5f), "probe");
            Tr(lb != null, "B0-前提③：`Label.Create` 建出来了");
            if (lb == null) return;

            var prevLang = Loc.Current;
            bool prevPersist = Loc.PersistOverride;
            Loc.PersistOverride = true;                    // 自检不许写 `PlayerPrefs`
            try
            {
                Loc.SetLanguage(AvailableLanguages.English);
                int c0 = Loc.LocalizedBindingCount;
                lb.BindText(() => Loc.T(term));
                C(Loc.LocalizedBindingCount, c0 + 1,
                  "B1 ★ #94 `Label.BindText` 把自己登进 `Loc` 的登记表（原版 = 每颗 TMP 挂一颗 `Localize`）");
                Tr(lb.HasTextProvider, "B2 ★ #94 这条标签登记了来源（`HasTextProvider`）");
                // 🔴 **2026-10-30（`B103_D4` 订正 · 铁律 5）**：`BindText` 的契约是「**只登记、不立刻取**」
                //   （`Battle/Label.cs` 的 `BindText` doc：调用方通常刚把同一句传给 `MenuDraw.Text`，
                //   再取一次是白量一遍版面；想立刻对齐就自己调 `Relocalize()`）⇒ 绑完 `lb.Text` 仍是创建时
                //   那句 `init`，**不是**英文 —— 原来这条直接断英文，断的是实现没承诺的事（实得 `init`）。
                //   这里补上「重取的口」那一跳（= `Loc` 换语言广播里对每颗标签做的同一件事），
                //   断的仍是「provider 真能落到 `Loc.T` 的 en 列」这一条（不是自证：走的是
                //   provider → `SetText` → TMP 文本 的整条链，期望值来自官方 13 语言表）。
                lb.Relocalize();
                C(lb.Text, en, "B3 ★ #94 英文档下，重取的口给出英文（`Loc.T` 三档的 en 列）");

                Loc.SetLanguage(AvailableLanguages.Chinese);   // 真广播（`SetLanguage` 改了值才发）
                C(lb.Text, zh,
                  "B4 ★ #94 **换语言那一跳就把字换掉了**（⛔ 不是「下次开窗才换」）—— 判据 = I2 `Localize.OnLocalize` 的形状；"
                + "🧨 把 `NotifyLanguageChanged` 里那句 `RelocalizeAll()` 删掉 ⇒ 这里仍是英文 ⇒ 红");
                Tr(lb.RelocalizeCountForTest >= 1,
                   "B5 ★ #94 这条标签确实被「重取」过（`RelocalizeCountForTest == " + lb.RelocalizeCountForTest + "`）");

                int n = Loc.RelocalizeAll();
                Tr(n >= 1, "B6 ★ #94 `RelocalizeAll()` 至少重取一颗（实得 " + n + "）—— 它是换语言广播的下游唯一入口");
                C(lb.Text, zh, "B7 ★ #94 重复重取**幂等**（仍是同一个中文串，没有叠加/清空）");

                lb.BindText(null);                              // 退绑
                C(Loc.LocalizedBindingCount, c0, "B8 ★ #94 `BindText(null)` 退登（登记表回到绑之前，不留死订户）");
                Tr(!lb.HasTextProvider, "B9 ★ #94 退绑之后这条标签不再自称「跟着语言走」");
            }
            finally
            {
                Loc.SetLanguage(prevLang);                      // 还原语言（会再广播一次，无副作用）
                Loc.PersistOverride = prevPersist;
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ============================================================ #94 五窗源码锁
    static void C_WindowOverrides()
    {
        // 施工单 #94 的五个窗（`Shell/` 下的文件，路径相对 `Application.dataPath`）。
        string[] files =
        {
            "RewardsWindow", "InboxWindow", "DailyRewardPopup", "DailyStreakPopup", "RewardWindow",
        };
        const string GoodLine = "if (Label.RelocalizeUnder(transform) == 0) Open();";
        const string BadLine = "OnLanguageChanged() { Open(); }";     // 整窗重建（滚动位回顶）= 本条要修掉的形状
        for (int i = 0; i < files.Length; i++)
        {
            string rel = "CardPresentation/Shell/" + files[i] + ".cs";
            string path = Path.Combine(Application.dataPath, rel);
            bool ok = File.Exists(path);
            Tr(ok, "C0-前提：读得到 " + rel);
            if (!ok) continue;
            string text = File.ReadAllText(path);
            Tr(text.IndexOf(GoodLine, StringComparison.Ordinal) >= 0,
               "C1 ★ #94 `" + files[i] + "` 的 `OnLanguageChanged` 走**按来源重取**通道（`" + GoodLine + "`）"
             + "｜🧨 删掉/改回整窗重建 ⇒ 红");
            Tr(text.IndexOf(BadLine, StringComparison.Ordinal) < 0,
               "C2 ★ #94 `" + files[i] + "` 里**不再**有 `" + BadLine + "`（整窗重建 = 滚动位回顶、页签重算，原版那一跳不重建）");
        }
    }

    // ============================================================ #21（需要宿主那扇真卡组库建出来的窗）
    /// <summary>#21 的 ②③ 两面。⚠️ 会**临时**置脏拖拽态与滚动位，结束时原样还原（不重建整窗、不动数据）。</summary>
    public static void RunDeck(DeckRuntime rt)
    {
        MenuCheck.Section(_sink, "B103A03 · AUD03 #21（DeckRuntime 两次 Build 的验收面：卡背格数恰好 · 拖拽保护槽跟着滚）");
        Tr(rt != null, "D0-前提：宿主把建好的 `DeckRuntime` 交进来了（为 null ⇒ 本节等于没验）");
        if (rt == null) { Debug.Log(_sink.P + "B103A03 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败"); return; }

        // ---- ②「两次 Build 之后卡背格数**恰好** == CosmoRows × CosmoCols」（A8 只断了「不增长」，两条都留着：
        //       A8 防增长、本条防「一开始就没填满」） ----
        int want = DeckRuntime.CosmoRows * DeckRuntime.CosmoCols;
        Tr(want > 0, "D1-前提①：`CosmoRows × CosmoCols` 是个正数（实得 " + want + "）");
        C(rt.UiCosmCellCount, want,
          "D2 ★ #21 卡背格子表**恰好** = CosmoRows × CosmoCols（`UiCosmCellCount == " + rt.UiCosmCellCount + "`，期望 " + want + "）"
        + "｜🧨 把 `BuildCosmeticsPage` 里那句改成 `for (… ) Add(null)` ⇒ 第 2 次 Build 之后是本数的 2 倍 ⇒ 红");

        // ---- ③「滚一格之后，被保护的槽 == 数据行 − firstRow」 ----
        float pitch = rt.UiRowPitchForTest;
        Tr(pitch > 0f, "D3-前提②：一行高 `RowPitch` > 0（实得 " + pitch.ToString("F2") + "）");
        if (pitch <= 0f) { Debug.Log(_sink.P + "B103A03 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败"); return; }

        float saveScroll = rt.DeckScrollPx;           // 原样还原用
        int saveDragSlot = rt.UiDragSlotViewForTest;
        const int dragRow = 4;
        try
        {
            rt.UiSetTab(0);                            // 回卡组列表页（`SetTab` 会把滚动位归零）
            rt.UiSetDragStateForTest(true, dragRow);   // 只置「正在拖 + 被拖数据行」两格（⛔ 不建件）
            rt.UiScrollDeck(pitch * 2f);               // 往下滚两行 ⇒ firstRow 应 == 2
            float now = rt.DeckScrollPx;
            int first = Mathf.FloorToInt(now / pitch);
            Tr(now > 0f && first >= 1,
               "D4-前提③：滚动真的动了（`DeckScrollPx == " + now.ToString("F2") + "` ⇒ firstRow == " + first + "）；"
             + "为 0 ⇒ 宿主那副卡组库行数太少 ⇒ 本条只能验「firstRow == 0」那一档（= `B103R1_Assert` A9 已覆盖）⇒ 当场红");
            C(rt.UiDragSlotViewForTest, dragRow - first,
              "D5 ★ #21 滚到第 " + first + " 行之后，**被保护的槽** == 数据行 − firstRow（实得 "
              + rt.UiDragSlotViewForTest + "，期望 " + (dragRow - first) + "）"
            + "｜🧨 删掉 `RefreshDeckList` 里那句 `if (_dragging) _dragSlotView = _dragRow - firstRow;` ⇒ 它停在起拖那一刻的槽号 ⇒ 红"
            + "（错位后果：保护的是**别的行**、被拖那行反而被裁 —— 原版被拖件 reparent 到 root，不存在这种错位）");
        }
        finally
        {
            rt.UiSetDragStateForTest(false, -1);
            rt.UiScrollDeck(saveScroll - rt.DeckScrollPx);   // 还原滚动位
            if (rt.UiDragSlotViewForTest != saveDragSlot) { /* 视图槽是派生量，由还原后的滚动位自然重算 */ }
        }

        Debug.Log(_sink.P + "B103A03 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }
}
