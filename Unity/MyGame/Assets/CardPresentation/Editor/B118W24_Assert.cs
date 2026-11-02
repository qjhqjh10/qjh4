// B118W24_Assert.cs — 第一百一十八会话 · 批 4（活动 / 锻造 / 商店 / 设备变量）· 执行写手 `B118_W24`
//
// ============================ 它盯的是什么 ============================
// 输入 = `资料/完全复刻/判据/B118_J10_215条真缺口分诊.md` §2 的批 4：#118 · #119 · #211/#212 · #15 · #142/#143。
// 每组一条「前提 + 结论 + 灭自证」，⛔ 不用 `if (前提) Check(结论)` 那种会静默跳过的写法。
//
//   A · #118 `GetEventDeck` 的三支 / 活动卡组槽 → 窗口
//       A1 基类 `PlayEvent<,>.GetEventDeck()` **有**第 ③ 支（`GetEventDeckRankedOrNull`）；
//       A2 🔴 **反证**：FastMode 的 override **没有** 第 ③ 支 —— 判据 `.c` 就在函数结尾
//          （`decomp_generic_trial/out/FastModeBaseEvent_object__object___GetEventDeck__1a0fdf0.c`
//           末段直接 `return FirstOrDefault(…)`，没有 `GetActiveEvent<IRankedEvent>()`）⇒ 分诊里
//          「`FastModeBaseEvent.cs:608 GetEventDeck` 缺第三支」**这一半判据本身错**，⛔ 别去补。
//       A3/A4 行为：事件存档里的 `defaultDeckId` 必须过原版第 ① 支的**模式闸**（`deck.GameMode == EventPlayMode`）
//          才当「当前那一副」；不合闸 ⇒ 回落，⛔ 不能因为「这个活动上次记过」就把**别的模式**的卡组画出来。
//          ⚠️ ① 的另一半 `ValidateDeck` **没落**（残差，理由 = `Shell/LiveOpsEventWindow.cs` 类尾那段：
//             合法性在本仓只作排序键），所以本组**不**断言不合法卡组会被挡 —— ⛔ 别把它写成「两口闸都在」。
//
//   B · #119 `FastModeBaseEvent.TryCollect` 的失败支
//       判据 = `cpp2il/out_cpp2il_dev_ilspy/FastModeBaseEvent.cs:162-170`（`_003CTryCollect_003Eb__20_1`）：
//       **只** `ErrorPopupWindowController.ShowErrorMessage(CustomError.GenericConnectionError)`，⛔ **不调** `onComplete`。
//       本地那一路的「失败」= 活动存档没落盘（`SaveEventProgress` 的 `onFail`）。
//
//   C · #142/#143 `VarsMobile` 数据件
//       分诊写「`VarsMobile`（25/12）数据件未建」—— 🔴 现读**已建**（`Core/VarsDevice.cs:106-119`）；
//       本组把它钉成硬断言（值逐项 = 两份 JSON 的实读），并断 `HandLayout` 那一格**收口到数据件**。
//
//   D · #15 卡包 6 只依赖包
//       `B102_D10` 的根因 = 「同包兄弟 CAB 的引用 Unity 不认」⇒ 修法 = 拆成 6 只**独立 bundle** 并**先于主包加载**。
//       本组断接线（6 只名字 + 文件在 + 调用次序），⛔ 不重复 `B97W32Assert`（它断包内槽位，要 Unity 跑）。
//
// ============================ 怎么跑（⚠️ 本代理无权跑 Unity，本轮未跑）============================
//   `int n = B118W24_Assert.Run(CheckTrue);` —— 把这一行加进宿主（建议 `Editor/ShellScene.cs`）的
//   合计行**之前**（该行以 `//` 结尾 ⇒ 追加必须落在**它第一个 `//` 之前**，CLAUDE.md「同一物理行」那条坑）。
//   ⛔ 本类**不跑 Unity 也能读**：所有断言都是反射 / 源码 / 纯数据；只有 A3/A4 会 **建一扇卡组选择窗**
//      （同 `ModeWindowAssertE11` 的做法，跑完自己销毁）。
//
// ============================ 灭自证（每条「什么改动能让它红」）============================
//   · 把 `PlayEvent.GetEventDeck()` 的 `GetEventDeckRankedOrNull(src)` 删掉 ⇒ A1 红。
//   · 给 FastMode 的 override 补一条 ③（分诊字面建议）⇒ A2 红（它会打印出 `.c` 的出处）。
//   · 把 `EventDeckIndexFromEventSave` 的模式闸改回 `if (i >= 0) return i;` ⇒ A3 红（窗会画 Goff 那套经典卡组）。
//   · 把模式闸改成「恒 false」⇒ A4 红（上次记过的那副小冲突卡组认不出来）。
//   · 把 TryCollect 的失败支去掉（回到 `SaveEventProgress(null, null);`）⇒ B2/B3 红。
//   · 把失败支改成「失败也照跑 onComplete」⇒ B2 红；改成「成功也回滚」⇒ B4 红。
//   · 把 `VarsDevice.Mobile` 的 25/12 改成 30/20（拿桌面值顶）⇒ C1 红。
//   · 把 `HoverLiftPxForDevice` 写回 `… > 7.0f ? 20f : 30f` ⇒ C4 红（源码里又出现那个三元）。
//   · 把 `EnsureDepBundlesLoaded()` 挪到主包 `LoadFromFile` 之后 ⇒ D2 红。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B118_W24`：批 4（活动 / 锻造 / 商店 / 设备变量）的观测口。</summary>
public static class B118W24_Assert
{
    const string P = "[B118W24] ";

    /// <summary>跑全部断言。`check(条件, 文案)` 由宿主提供；返回**执行过的条数**（打印它 = 自证真的跑了）。</summary>
    public static int Run(Action<bool, string> check)
    {
        if (check == null) { Debug.LogError(P + "Run(null) ⇒ 不跑"); return 0; }
        int n = 0;
        Action<bool, string> C = delegate (bool ok, string msg) { n++; check(ok, msg); };
        C(true, P + "A0 前提：本类被真的调到（这一条恒真，只用来证明其余断言不是「没跑」）");

        GroupAEventDeck(C);
        GroupBTryCollect(C);
        GroupCDeviceVars(C);
        GroupDBoosterDeps(C);

        Debug.Log(P + "共跑 " + n + " 条断言");
        return n;
    }

    // ============================================================ A · #118
    static string ReadSource(string relUnderCardPresentation)
    {
        try
        {
            string p = System.IO.Path.Combine(Application.dataPath, "CardPresentation", relUnderCardPresentation);
            return System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p) : null;
        }
        catch (Exception e) { Debug.LogError(P + "读源码失败：" + e.Message); return null; }
    }

    static void GroupAEventDeck(Action<bool, string> C)
    {
        // ---- A1/A2：三支的结构（判据 = `.c` 末段，见文件头）
        string pe = ReadSource("Shell/PlayEvent.cs");
        C(pe != null, P + "A1（前提）读到 `Shell/PlayEvent.cs`");
        if (pe != null)
        {
            int i0 = pe.IndexOf("public virtual string GetEventDeck()", StringComparison.Ordinal);
            int i1 = i0 < 0 ? -1 : pe.IndexOf("public virtual string GetEventDeck(CardArmy army)", i0, StringComparison.Ordinal);
            C(i0 >= 0 && i1 > i0, P + "A1（前提）在基类里切得出 `GetEventDeck()` 的方法体");
            string body = (i0 >= 0 && i1 > i0) ? pe.Substring(i0, i1 - i0) : "";
            C(body.Contains("GetEventDeckRankedOrNull(src)"),
              P + "A1 基类 `PlayEvent.GetEventDeck()` **有**第 ③ 支（`GetEventDeckRankedOrNull` = `GameModes.GetActiveEvent<IRankedEvent>()`）");
        }

        string fm = ReadSource("Shell/FastModeBaseEvent.cs");
        C(fm != null, P + "A2（前提）读到 `Shell/FastModeBaseEvent.cs`");
        if (fm != null)
        {
            int j0 = fm.IndexOf("public override string GetEventDeck()", StringComparison.Ordinal);
            int j1 = j0 < 0 ? -1 : fm.IndexOf("public override string GetEventDeck(CardArmy army)", j0, StringComparison.Ordinal);
            C(j0 >= 0 && j1 > j0, P + "A2（前提）在 FastMode 里切得出 `GetEventDeck()` 的 override 体");
            string body = (j0 >= 0 && j1 > j0) ? fm.Substring(j0, j1 - j0) : "";
            C(!body.Contains("GetEventDeckRankedOrNull") && !body.Contains("PlayEventRankedDeck"),
              P + "A2 🔴 反证：FastMode 的 override **没有**第 ③ 支（判据 = `FastModeBaseEvent_object__object___GetEventDeck__1a0fdf0.c` "
            + "末段直接 `return FirstOrDefault(…)`）⇒ 分诊「FastMode 缺第三支」判据本身错，⛔ 别去补");
        }

        // ---- A3/A4：行为（事件存档里的 defaultDeckId 过不过闸）
        RunEventDeckWindow(C);
    }

    /// <summary>A3/A4 的现场：3 套夹具卡组（Goff 经典 / Ultramarines 小冲突 / Goff 小冲突），
    /// 事件存档里记的那副**模式不对**时不能当「当前那一副」（原版 `GetEventDeck` 第 ① 支的两口闸）。</summary>
    static void RunEventDeckWindow(Action<bool, string> C)
    {
        string keepDeckPath = DeckStore.OverridePath;
        string keepRoot = SaveCenter.OverrideRoot;
        var keepLast = LiveOpsEventWindow.LastOpened;
        SkirmishEventWindow sk = null;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wf_b118w24");
        try
        {
            System.IO.Directory.CreateDirectory(dir);
            DeckStore.OverridePath = System.IO.Path.Combine(dir, "decks.json");
            try { System.IO.File.Delete(DeckStore.OverridePath); } catch { }
            SaveCenter.OverrideRoot = dir;
            SaveCenter.ResetForTest();
            CollectionData.ResetForTest();

            string umHero = null, goffHero = null;
            foreach (var c in CardDatabase.Load())
            {
                if (c.Type != "hero") continue;
                if (umHero == null && DeckRules.SameFaction(c.Faction, "Ultramarines")) umHero = c.Id;
                if (goffHero == null && DeckRules.SameFaction(c.Faction, "Goff")) goffHero = c.Id;
                if (umHero != null && goffHero != null) break;
            }
            C(umHero != null && goffHero != null, P + "A3（前提）卡池里有 Ultramarines / Goff 的督军");
            if (umHero == null || goffHero == null) return;

            var lib = DeckLibrary.Load();
            var dClassicGoff = lib.Create("W24 经典-Goff", (int)GameMode.Classic);
            dClassicGoff.WarlordId = goffHero;
            var dSkUm = lib.Create("W24 小冲突-UM", (int)GameMode.Skirmish);
            dSkUm.WarlordId = umHero;
            var dSkGoff = lib.Create("W24 小冲突-Goff", (int)GameMode.Skirmish);
            dSkGoff.WarlordId = goffHero;
            lib.Save();

            C(CollectionData.DeckCount() == 3, P + "A3（前提）临时库有 3 套（实得 " + CollectionData.DeckCount() + "）");
            if (CollectionData.DeckCount() != 3) return;
            int iClassic = 0, iSkUm = 1, iSkGoff = 2;
            string idClassic = CollectionData.EnsureDeckId(iClassic), idSkGoff = CollectionData.EnsureDeckId(iSkGoff);
            C(!string.IsNullOrEmpty(idClassic) && !string.IsNullOrEmpty(idSkGoff) && idClassic != idSkGoff,
              P + "A3（前提）两套各有不同的 id");
            if (string.IsNullOrEmpty(idClassic) || string.IsNullOrEmpty(idSkGoff) || idClassic == idSkGoff) return;

            // 「当前」摆到 UM 那套小冲突 —— 闸把 Goff 经典那副挡掉之后，回落到这里。
            C(CollectionData.Select(iSkUm), P + "A3（前提）`CollectionData.Select(1)` 成功（当前 = UM 小冲突）");
            C(CollectionData.CurrentIndex() == iSkUm, P + "A3（前提）当前下标 = " + iSkUm);

            // ---- A3：活动存档记的是**经典**那副（Goff）⇒ 不能画它
            string evA = "B118W24_A_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var sA = new PlayEventSaveGame { eventId = evA, defaultDeckId = idClassic };
            C(LiveOpsSaveStore.Save(sA), P + "A3（前提）把活动存档写进临时目录（`" + evA + "`）");

            sk = SkirmishEventWindow.Create(null);
            C(sk != null, P + "A3（前提）建出小冲突事件窗");
            if (sk != null)
            {
                sk.EventId = evA;
                sk.Open();
                C(sk.DeckIndex == iSkUm,
                  P + "A3 事件存档里的 `defaultDeckId` 是**经典**那一副 ⇒ 原版第 ① 支的**模式闸**不过（`GameMode != EventPlayMode`）"
                + " ⇒ 本窗回落画 **UM 小冲突**那套（实得 " + sk.DeckIndex + "）；🧨 把闸改回 `if (i >= 0) return i;` ⇒ 画出来的是 Goff 那套小冲突（2）不是 1");
                C(sk.DeckIndex != iClassic,
                  P + "A3c（灭自证·形状相反）画出来的**不是**存档里记的那一套（" + iClassic + "）");
                SafeDestroy(sk.gameObject);
                sk = null;
            }

            // ---- A4：同一扇窗，存档记的是**本模式**那副（Goff 小冲突）⇒ 必须认
            string evB = "B118W24_B_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var sB = new PlayEventSaveGame { eventId = evB, defaultDeckId = idSkGoff };
            C(LiveOpsSaveStore.Save(sB), P + "A4（前提）把第二份活动存档写进去（`" + evB + "`，Goff 小冲突）");
            sk = SkirmishEventWindow.Create(null);
            if (sk != null)
            {
                sk.EventId = evB;
                sk.Open();
                C(sk.DeckIndex == iSkGoff,
                  P + "A4 存档记的是**本模式**那副 ⇒ 认它（原版第 ① 支过模式闸 ⇒ 不打回扫表），实得 " + sk.DeckIndex
                + "；🧨 把模式闸改成「恒 false」⇒ 会回落到 UM（1），这条红");
                SafeDestroy(sk.gameObject);
                sk = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(P + "A3/A4 抛异常（宿主不该被它掀掉）：" + e);
            C(false, P + "A3/A4 不应抛异常（实抛：" + e.GetType().Name + "）");
        }
        finally
        {
            if (sk != null) SafeDestroy(sk.gameObject);
            LiveOpsEventWindow.LastOpened = keepLast;
            LiveOpsSaveStore.ResetForTest();
            SaveCenter.OverrideRoot = keepRoot;
            SaveCenter.ResetForTest();
            DeckStore.OverridePath = keepDeckPath;
            CollectionData.ResetForTest();
        }
    }

    /// <summary>`DestroyImmediate`（编辑模式 `Destroy` 不生效）。</summary>
    static void SafeDestroy(GameObject go) { if (go != null) UnityEngine.Object.DestroyImmediate(go); }

    // ============================================================ B · #119
    /// <summary>`FastModeBaseEvent` 的最小具体子类：把 `SaveEventProgress` 的成败接到一个开关上
    /// （本地那一路的「服务器失败」= 存档写不进去）。</summary>
    sealed class ProbeFast : FastModeBaseEvent<FastModeSave, FastModeData>
    {
        public bool FailSave;
        public int SaveCalls;
        public ProbeFast(FastModeData d, FastModeSave s) { LoadEventData(d, s, null); }
        public override void SaveEventProgress(Action onSuccess, Action onFail)
        {
            SaveCalls++;
            if (FailSave) { if (onFail != null) onFail(); }
            else if (onSuccess != null) onSuccess();
        }
    }

    static void GroupBTryCollect(Action<bool, string> C)
    {
        var d = new FastModeData { eventId = "B118W24_T", activationType = LiveOpsActivationType.Permanent };
        d.rewardsPerTier.Add(new MilestoneReward { milestone = 10, rewardIds = new[] { "r0" } });
        d.rewardsPerTier.Add(new MilestoneReward { milestone = 20, rewardIds = new[] { "r1" } });

        var s = new FastModeSave { eventId = "B118W24_T", IsActive = true };
        s.UpdateLocalJoinState(PlayEventSaveGame.JoinStates.Joined);
        s.localMilestoneScore = 25;                 // 两档都够得到 ⇒ pending = 2
        s.collectedRewards = 0;
        var ev = new ProbeFast(d, s);

        C(d.rewardsPerTier.Count == 2 && ev.CurrentScore == 25,
          P + "B1（前提）夹具：两档里程碑、当前分 25 ⇒ 两档都可领");

        // 失败支：把 WindowsManager 暂时摘掉（不然 `ShowErrorMessage` 会真的开一扇模态弹窗，
        // 在宿主里留下一个没人关的窗）。摘掉时它走「出声 + 照跑回调」那一支，弹窗文案仍入日志。
        object keepWm = null; MethodInfo setWm = null;
        try
        {
            var prop = typeof(WindowsManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            setWm = prop != null ? prop.GetSetMethod(true) : null;
            if (setWm != null) { keepWm = prop.GetValue(null); setWm.Invoke(null, new object[] { null }); }
        }
        catch (Exception e) { Debug.LogWarning(P + "B（记录）摘 `WindowsManager.Instance` 失败：" + e.Message); }

        try
        {
            ev.FailSave = true;
            bool completed = false;
            ev.TryCollect(delegate { completed = true; });
            C(ev.SaveCalls >= 1, P + "B1b（前提）失败支真的走到了落盘那一步（`SaveCalls = " + ev.SaveCalls + "`）");
            C(!completed, P + "B2 ★ 落盘失败 ⇒ **不调** `onComplete`（原版失败支 `_003CTryCollect_003Eb__20_1` 只弹错误）；"
                            + "🧨 改回 `SaveEventProgress(null, null);`（不看成败）⇒ 这条红");
            C(s.collectedRewards == 0, P + "B3 ★ 落盘失败 ⇒ `collectedRewards` **回滚**到 0（内存不许跑在盘前面），实得 " + s.collectedRewards);
        }
        finally
        {
            if (setWm != null) { try { setWm.Invoke(null, new object[] { keepWm }); } catch { } }
        }

        // 成功支：⛔ 别把失败支做成「恒失败」
        ev.FailSave = false;
        bool done = false;
        ev.TryCollect(delegate { done = true; });
        C(done, P + "B4 落盘成功 ⇒ `onComplete` 照跑（失败支没把成功那一路误杀）");
        C(s.collectedRewards == 2, P + "B4b 成功 ⇒ `collectedRewards` 前进到 2（实得 " + s.collectedRewards + "）");
    }

    // ============================================================ C · #142/#143
    static void GroupCDeviceVars(Action<bool, string> C)
    {
        VarsDevice m = VarsDevice.Mobile, dk = VarsDevice.Desktop;
        C(m != null && dk != null, P + "C1（前提）`VarsDevice.Mobile` / `.Desktop` 两份都在");
        if (m == null || dk == null) return;

        C(Near(m.cardInHandShownScale, 1.7f) && Near(m.cardInHandShownYOffset, 25f) && Near(m.CardInHandYOffsetMin, 12f),
          P + "C1 ★ `VarsMobile` 数据件**已在**（分诊写「未建」，已过期）：`cardInHandShownScale/YOffset/YOffsetMin` = 1.7 / 25 / 12"
        + "（实得 " + m.cardInHandShownScale + " / " + m.cardInHandShownYOffset + " / " + m.CardInHandYOffsetMin + "）");
        C(Near(m.warlordStartSizeFactor, 1.5f) && Near(m.tutorialTipHorizontalOffset, 18f) && Near(m.tutorialTipHorizontalOffsetForElements, 16f)
          && Near(m.moveCardYOffset, 0.7f) && Near(m.targetYOffset, 0.2f),
          P + "C1b `VarsMobile` 其余五格 = 1.5 / 18 / 16 / 0.7 / 0.2（`VarsMobile.json` 实读）");
        C(m.extraSpaceOnSelectedCard != null && m.extraSpaceOnSelectedCard.length >= 2
          && Near(m.extraSpaceOnSelectedCard.Evaluate(0f), 2f) && Near(m.extraSpaceOnSelectedCard.Evaluate(1f), 5f),
          P + "C1c `VarsMobile.extraSpaceOnSelectedCard` = (0,2) → (1,5)（桌面那一份是平线 2.0）");
        C(Near(dk.cardInHandShownYOffset, 30f) && Near(dk.CardInHandYOffsetMin, 20f),
          P + "C1d（对照）`VarsDesktop` = 30 / 20（⛔ 别把两份的值互相顶）");

        // 生产读点与数据件同源
        float want = VarsDevice.Current.GetCardInHandYOffsetTouchInput();
        C(Near(HandLayout.HoverLiftPxForDevice, want),
          P + "C3 `HandLayout.HoverLiftPxForDevice` = `VarsDevice.Current.GetCardInHandYOffsetTouchInput()`（实得 "
        + HandLayout.HoverLiftPxForDevice + " vs " + want + "）—— 单源，⛔ 别在本行再写死 20/30");
        C(Near(HandLayout.HoverLiftPxForDevice, 30f) || Near(HandLayout.HoverLiftPxForDevice, 20f),
          P + "C3b 取值只能是 30（≤7 吋）/ 20（>7 吋）两档");

        string hl = ReadSource("Hand/HandLayout.cs");
        C(hl != null, P + "C4（前提）读到 `Hand/HandLayout.cs`");
        if (hl != null)
            C(!hl.Contains("? 20f : 30f"),
              P + "C4 ★（灭自证）源码里**不再**出现写死的 `? 20f : 30f` —— 改回写死 ⇒ 红（两处写同一条规则迟早不一致）");
    }

    static bool Near(float a, float b) { return Mathf.Abs(a - b) <= 1e-4f; }

    // ============================================================ D · #15
    static void GroupDBoosterDeps(Action<bool, string> C)
    {
        var t = typeof(BoosterPackAssets);
        var f = t.GetField("DepBundleNames", BindingFlags.NonPublic | BindingFlags.Static);
        C(f != null, P + "D1（前提）反射到 `BoosterPackAssets.DepBundleNames`");
        string[] names = null;
        if (f != null) names = f.GetValue(null) as string[];
        C(names != null && names.Length == 6, P + "D1 6 只依赖包表在（实得 " + (names == null ? -1 : names.Length) + " 只）");
        if (names != null)
        {
            int missing = 0;
            for (int i = 0; i < names.Length; i++)
            {
                string p = System.IO.Path.Combine(Application.streamingAssetsPath, "WarpforgeVFX/" + names[i]);
                if (!System.IO.File.Exists(p)) { missing++; Debug.LogWarning(P + "D1 缺文件：" + p); }
            }
            C(missing == 0, P + "D1b 6 只依赖包文件**都在** `StreamingAssets/WarpforgeVFX/`（缺 " + missing + " 只）"
                            + " —— 判据 = `B102_D10` §3.1（拆成独立 bundle 才是原版可用的跨包路）");
        }

        string src = ReadSource("Core/BoosterPackAssets.cs");
        C(src != null, P + "D2（前提）读到 `Core/BoosterPackAssets.cs`");
        if (src != null)
        {
            int dep = src.IndexOf("EnsureDepBundlesLoaded();", StringComparison.Ordinal);
            int main = src.IndexOf("_bundle = AssetBundle.LoadFromFile(path)", StringComparison.Ordinal);
            C(dep >= 0 && main > dep,
              P + "D2 ★ 依赖包**先于**主包加载（`EnsureDepBundlesLoaded()` 在 `LoadFromFile(path)` 之前；"
            + "依赖包 " + dep + " vs 主包 " + main + "）—— 🧨 调换次序 ⇒ `B102_D10` 的 200 个空材质槽回来");
        }
    }
}
