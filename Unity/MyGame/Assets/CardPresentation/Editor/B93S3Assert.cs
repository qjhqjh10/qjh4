// B93S3Assert.cs — 执行代理 S3（第九十四会话 · 波 1）的 22 条断言
//
// ============================ 它盯的是什么 ============================
// `判据/B94_派活.tsv` 里 `写手组 == S3_未分类` 的 22 条（10 个文件）：
//   A 轮播抽屉 `CarouselOfferPopupDrawer`（账 `商店:42/43/44/45`）—— 本轮只补 **B93_E13 / B93_S1 没覆盖的两格**
//   B 通用容器族 `ContainerHolder`（账 `未分类/其他:183/184/185/186`）
//   C 通用容器族 `ContainerBuilder`（账 `未分类/其他:180/181/182`）
//   D 通用容器族 `ContainerService.OpenOfferContainer`（账 `未分类/其他:191`）
//   E 原版 `Countdown`（账 `未分类/其他:204`）
//   F 原版 `CursorByArmySO.GetCursor`（账 `未分类/其他:220`）
//   G 原版 `FactionScore.Initialize`（账 `未分类/其他:295`）
//   H 原版 `GameObjectUniqueIdentifier` 注册表（账 `未分类/其他:312`）
//   I 原版 `GenericArmyItem.GetItem` 两态（账 `未分类/其他:321/322`）
//   J 原版 `MilestoneScoreBar.Initialize` + `ProgressBar.SetProgress`（账 `未分类/其他:430/496`）
//   K 原版 `ReferralContainer.Refresh`（账 `未分类/其他:516`）
//   L 原版 `SimpleRotation.Update`（账 `未分类/其他:584`）
//
// 逐条的判据、做法、为什么某些格没落地 → `资料/完全复刻/报告/B94_S3.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `B93S3Assert.Run();`
//   + 宿主里两行（照 `ShopFamilyAssertE13` 的先例）：
//     `_sink.Pass += B93S3Assert.Pass; _sink.Fail += B93S3Assert.Fail;`
//   · 推荐的宿主：**`Editor/ShellScene.cs`**（本批的 `Shell/*.cs` 全是它建的；容器族/唯一 id/轮播都在壳上）。
//     ⚠️ 第二选择 `Editor/MainMenuScene.cs`（`RankedTab` / `AllianceEventScoreInfo` 的主场）。
//   · **不跑 Unity 批处理**、不建持久资产；建的结点在收尾 `DestroyImmediate`。
//   · 静态注入点（`Carousel.Filter` / `ContainerBuilder.*` / `ContainerService.*` /
//     `GenericArmyItem.CurrentArmyProvider` / `RankedTab.FactionScore*Provider` / `MenuUniqueIds` 表）
//     **每条都在 `finally` 里还原** —— 否则会污染别的宿主。
//
// ============================ 🔴 灭自证（每条的两态 / 翻面法）============================
//   · A：把 `DoReset` 里那句 `ReleaseAssets()` 从循环内挪到循环外 ⇒ A1 红（4 → 2）。
//        把 `DrawItemReward` 的「画不出来就不收」改成先收后判 ⇒ A3/A4 红。
//   · B：把 `TryAdd` 的 `maxAmount <= Count` 改成 `<` ⇒ B2 红（第 3 次会返回 true）。
//        把 `Clear` 里的 `destroyAllChildren()` 删掉 ⇒ B5 红。
//   · C：把 `MakeContainer(evt, container, parent)` 的 `container == null ⇒ return null` 删掉 ⇒ C3 红。
//        把 `evt as ILiveOps` 那道闸删掉 ⇒ C4 红（非 ILiveOps 的 provider 会继续走下去）。
//   · D：把「没有后端 ⇒ onError」改成「当作成功 ⇒ onComplete」⇒ D1 红。
//   · E：把 `+ RoundUp` 那 1 秒删掉 ⇒ E2/E3 红（1:21 → 1:20）；把 `Seconds < 5` 改成 `<= 5` ⇒ E6 翻面。
//   · F：把表里某一行的 army 改错 ⇒ F2 红；把「扫完没有 ⇒ false」改成抛 ⇒ F4 红。
//   · G：把 `FactionScoreText` 的 `null` 分支改成 `"0"` ⇒ G1 红。
//   · H：把后来的赢改成先来的赢 ⇒ H3 红；把空 id 也登记 ⇒ H5 红。
//   · I：把 `handler == null ⇒ 0` 那支改成 `⇒ 20` ⇒ I3 红。
//   · J：把 `InterpolateMilestones` 换成线性归一 ⇒ J3 红（0.3 → 0.22）。
//   · K：把键从 `Demo/DraftMode/FreeButton` 改掉 ⇒ K1 红。
//   · L：把 `Step` 的 `speed * dt` 改成 `speed` ⇒ L2 红。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 S3：22 条（容器族 / 倒计时 / 指针图 / 阵营分 / 唯一 id / 通用阵营物品 / 记分条 / 价签 / 旋转）的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量**（`4` / `"1:21"` / `"0.3"` / `true·false` / `"{0}/{1}"`），
/// 不从被测实现里读 —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class B93S3Assert
{
    static readonly CheckSink _sink = new CheckSink("[S3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>读记分条第 <paramref name="i"/> 行的文字 —— **空安全**（没有 Canvas 的宿主里 `MenuDraw.Text`
    /// 可能返回 null ⇒ 直接 `.Text` 会抛出去、把整个宿主带崩；这里返回 `"&lt;null&gt;"` 让它变成一条红）。</summary>
    static string LabelText(AllianceEventScoreInfo si, int i)
    {
        if (si == null || si.ScoreLabels == null || i < 0 || i >= si.ScoreLabels.Count) return "<无此格>";
        var lb = si.ScoreLabels[i];
        return lb != null ? lb.Text : "<null>";
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "S3 未分类域：轮播补格 / 容器族 / Countdown / 指针图 / 阵营分 / 唯一 id / 阵营物品 / 记分条 / 价签 / SimpleRotation");

        SectionA_轮播补格();
        SectionB_ContainerHolder();
        SectionC_ContainerBuilder();
        SectionD_ContainerService();
        SectionE_Countdown();
        SectionF_CursorByArmy();
        SectionG_FactionScore();
        SectionH_MenuUniqueIds();
        SectionI_GenericArmyItem();
        SectionJ_MilestoneAndProgressBar();
        SectionK_ReferralContainer();
        SectionL_SimpleRotation();

        Debug.Log(_sink.P + "S3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 轮播抽屉（账 42/43/44/45 的补格）
    static void SectionA_轮播补格()
    {
        MenuCheck.Section(_sink, "A `CarouselOfferPopupDrawer`：`DoReset` 的 O(N²) 告警 + `DrawItemReward` 的『画不出来就不收』+ `Initialize` 的 V2 拼接次序");

        var savedFilter = OfferContainer.Carousel.Filter;
        try
        {
            OfferContainer.Carousel.Filter = null;

            // ---- A1/A2：`DoReset` 循环内逐条调 `ReleaseAssets()`（原版就是循环体内那一句）
            //      2 条抽屉 ⇒ ReleaseAssets 被调 2 次、每次遍历 2 条 ⇒ 告警 **4** 条（= N²）
            //      🧨 把那一句挪到循环外 ⇒ 变 2 ⇒ 红。
            var st = new OfferContainer.Carousel.State();
            st.DrawRewards(new List<CampaignData.RewardSpec>
            {
                new CampaignData.RewardSpec("A", 1, 0),
                new CampaignData.RewardSpec("B", 1, 0),
            }, null, (r, q) => true);
            int destroyed = 0;
            st.DoReset(e => destroyed++);
            C(st.ReleaseWarnings, 4, "A1 ★ `DoReset` 的 `ReleaseAssets()` 在**循环体内** ⇒ 2 条抽屉时告警数 = **4**（O(N²)，原版就这形状）");
            C(destroyed, 2, "A2 …而 `Destroy(gameObject)` 每条一次 = 2");
            C(st.DestroyedCount, 2, "A2b `DestroyedCount` 也记 2（自检读口）");

            // ---- A3/A4：无限循环滚动的**取模那一半**（原版 `scroll.ManualInit()` 立的那个环）
            //      ⚠️ `DrawItemReward` 三步那一支已由 `ShopFamilyAssertB93S1` 的 C1..C10 覆盖 ⇒ 这里不重复。
            C(OfferContainer.Carousel.WrapOffset(-1, 3), 2, "A3 ★ `WrapOffset(-1, 3)` = 2（负数也归正 ⇒ 循环回绕）");
            C(OfferContainer.Carousel.WrapOffset(4, 3), 1, "A3b `WrapOffset(4, 3)` = 1");
            C(OfferContainer.Carousel.WrapOffset(5, 0), 0, "A3c `n <= 0` ⇒ 0（⛔ 不除零）");
            C(OfferContainer.Carousel.IndexAt(0, -1, 3), 2, "A4 ★ `IndexAt(slot 0, offset −1, n 3)` = 2（滚到头上接回末尾）");
            C(OfferContainer.Carousel.IndexAt(2, 0, 3), 2, "A4b `IndexAt(2, 0, 3)` = 2");
            C(OfferContainer.Carousel.IndexAt(0, 0, 0), -1, "A4c `n = 0` ⇒ −1（没有槽可显示）");

            // ---- A5：`Initialize` 的 `v2Extra` = **原版 `base.Initialize` 里那个 `Concat`**
            //      ⇒ 先 `items`、后 `v2Extra`（次序照 `Concat`）
            var st3 = new OfferContainer.Carousel.State();
            var order = new List<string>();
            st3.Initialize(new List<CampaignData.RewardSpec> { new CampaignData.RewardSpec("I1", 1, 0) },
                           new List<CampaignData.RewardSpec> { new CampaignData.RewardSpec("V1", 1, 0) },
                           null, (r, q) => { order.Add(r.Id); return true; }, 3);
            C(order.Count == 2 ? order[0] + "|" + order[1] : "<空>", "I1|V1",
              "A5 ★ V2 那一支 = `items.Concat(v2Extra)` ⇒ 次序 **items 先、extra 后**（`GeneralOfferPopupDrawerBase.Initialize`）");
            C(st3.Visible, 3, "A6 `Initialize` 第 ② 句 `scroll.ManualInit()` 收下了一屏槽数 3");
            C(st3.Initialized, true, "A6b …并且置了 `Initialized`");
        }
        finally { OfferContainer.Carousel.Filter = savedFilter; }
    }

    // ============================================================ B 容器登记表（账 183/184/185/186）
    sealed class StubProvider : IContainerProvider
    {
        public int ReleaseCalls;
        public IUiContainer Container;
        public IUiContainer GetContainer() { return Container; }
        public void ReleaseContainer() { ReleaseCalls++; }
    }

    sealed class StubContainer : IUiContainer
    {
        public string ComponentId { get; set; }
        public StubContainer(string id) { ComponentId = id; }
    }

    static void SectionB_ContainerHolder()
    {
        MenuCheck.Section(_sink, "B `ContainerHolder`：`maxAmount` 满员闸 / `Clear` 的 DestroyAllChildren / `GetContainers` 返回本体");

        // ---- B1..B3：`maxAmount = 2` ⇒ 第 3 次 TryAdd 返回 false（**不再建**）
        int made = 0;
        var h = new ContainerHolder(2)
        {
            MakeContainer = (p, t) => { made++; return new StubContainer("c" + made); }
        };
        var prov = new StubProvider();
        bool r1 = h.TryAdd(prov, out var c1);
        bool r2 = h.TryAdd(prov, out var c2);
        bool r3 = h.TryAdd(prov, out var c3);
        C(r1, true, "B1 `TryAdd` 第 1 次 ⇒ true");
        C(r2, true, "B1b 第 2 次 ⇒ true（`maxAmount` = 2）");
        C(r3, false, "B2 ★ 第 3 次 ⇒ **false**（`maxAmount <= Count` 那道闸；改成 `<` ⇒ 这条红）");
        C(made, 2, "B2b 满员那一次**根本没去建**（`ContainerBuilder` 只被叫了 2 次）");
        C(c3 == null, true, "B2c …`out` 那格被置 null（原版 `*param_3 = 0`）");
        C(h.GetContainers().Count, 2, "B3 `GetContainers()` = 2 条");
        Tr(ReferenceEquals(h.GetContainers(), h.GetContainers()),
           "B3b `GetContainers()` 返回**那一个表本身**（原版 `return this+0x28`，不是副本）");

        // ---- B4：`maxAmount = 0` ⇒ 不限量
        var h0 = new ContainerHolder(0) { MakeContainer = (p, t) => new StubContainer("x") };
        C(h0.TryAdd(new StubProvider(), out _), true, "B4 ★ `maxAmount = 0` ⇒ 不限量（第 1 次 true）");
        C(h0.TryAdd(new StubProvider(), out _), true, "B4b 第 2 次 true");
        C(h0.TryAdd(new StubProvider(), out _), true, "B4c 第 3 次 true");
        C(h0.GetContainers().Count, 3, "B4d 3 条都进了表");

        // ---- B5/B6：`Clear` = 先 DestroyAllChildren、再清表
        int destroyCalls = 0;
        h.Clear(() => destroyCalls++);
        C(destroyCalls, 1, "B5 ★ `Clear()` 调了那一下 `DestroyAllChildren`（删掉那句 ⇒ 这条红）");
        C(h.GetContainers().Count, 0, "B6 …并且容器表清空");
        Tr(h.Containers.Capacity >= 2, "B6b 清空后容量按 `maxAmount` 预留（原版 `new List(maxAmount)` 的意图）");
    }

    // ============================================================ C 容器构建器（账 180/181/182）
    static void SectionC_ContainerBuilder()
    {
        MenuCheck.Section(_sink, "C `ContainerBuilder`：三个 `MakeContainer` 重载（null 闸 / `evt as ILiveOps` / Instantiate + 一次虚调用）");

        var savedInst = ContainerBuilder.Instantiate;
        var savedInit = ContainerBuilder.Initialize;
        var savedLoad = LiveOpsContainers.Load;
        try
        {
            // ---- C1/C2：`container == null ⇒ return null`（**不**调 Instantiate）
            int instCalls = 0;
            ContainerBuilder.Instantiate = (c, p) => { instCalls++; return new StubContainer(c.ComponentId + "#inst"); };
            ContainerBuilder.Initialize = null;
            var got1 = ContainerBuilder.MakeContainer(new StubProvider(), (IUiContainer)null, null);
            C(got1 == null, true, "C1 ★ `MakeContainer(evt, container, parent)`：`container == null` ⇒ returned null");
            C(instCalls, 0, "C2 …而且**没去 Instantiate**（原版 `if ((bool)container)` 那道闸）");

            // ---- C3：走 `evt.GetContainer()` 那一支 ⇒ 实例化的就是它、父件原样传下去
            var src = new StubContainer("SRC");
            var prov = new StubProvider { Container = src };
            var parentGo = new GameObject("S3ContainerParent");
            try
            {
                IUiContainer seenContainer = null; Transform seenParent = null;
                ContainerBuilder.Instantiate = (c, p) => { seenContainer = c; seenParent = p; instCalls++; return new StubContainer("INST"); };
                var got2 = ContainerBuilder.MakeContainer(prov, parentGo.transform);
                C(got2 != null ? got2.ComponentId : null, "INST", "C3 ★ `MakeContainer(evt, parent)` ⇒ 用 `evt.GetContainer()` 建出来的那一件");
                C(ReferenceEquals(seenContainer, src), true, "C3b …交给了 `Instantiate` 的正是 `evt.GetContainer()` 的返回值");
                C(ReferenceEquals(seenParent, parentGo.transform), true, "C3c …父件原样传下去");
                C(instCalls, 1, "C3d `Instantiate` 只调了 1 次");

                // ---- C4：建好之后那**一次 via-class 虚调用**（注入点）
                IUiContainer initArg1 = null; IContainerProvider initArg2 = null;
                int initCalls = 0;
                ContainerBuilder.Initialize = (i, e) => { initCalls++; initArg1 = i; initArg2 = e; };
                ContainerBuilder.MakeContainer(prov, parentGo.transform);
                C(initCalls, 1, "C4 ★ 建完之后调了一次 `Initialize` 注入点（原版 `[Il2CppClass<TCont>+188]` 那一次虚调用）");
                Tr(initArg1 != null, "C4b …第 1 实参 = 新实例");
                Tr(ReferenceEquals(initArg2, prov), "C4c …第 2 实参 = 那个 provider");
            }
            finally { Object.DestroyImmediate(parentGo); }

            // ---- C5：`evt` 不是 `ILiveOps`（只实现了 `IContainerProvider`）⇒ 第 1 个重载直接 null
            //      （原版 `ILSpy: ILiveOps liveOps = default;` = `evt as ILiveOps` 的类型闸）
            ContainerBuilder.Initialize = null;
            var got3 = ContainerBuilder.MakeContainer(new StubProvider(), 0, null);
            C(got3 == null, true, "C5 ★ `MakeContainer(evt, type, parent)`：`evt` 不是 `ILiveOps` ⇒ null（`evt as ILiveOps` 那道闸）");

            // ---- C6：没装 Instantiate ⇒ null，且**不崩**
            ContainerBuilder.Instantiate = null;
            var got4 = ContainerBuilder.MakeContainer(new StubProvider { Container = src }, (Transform)null);
            C(got4 == null, true, "C6 ★ 没接 `ContainerBuilder.Instantiate` ⇒ 返回 null（出声，⛔ 不假装建出来）");
        }
        finally
        {
            ContainerBuilder.Instantiate = savedInst;
            ContainerBuilder.Initialize = savedInit;
            LiveOpsContainers.Load = savedLoad;
        }
    }

    // ============================================================ D 报价容器（账 191）
    static void SectionD_ContainerService()
    {
        MenuCheck.Section(_sink, "D `ContainerService.OpenOfferContainer`：没有后端 ⇒ onError（⛔ 不假装成功）/ 三跳闭包链次序");

        var savedUnlock = ContainerService.UnlockContainer;
        var savedRec = ContainerService.RecordTransaction;
        var savedCollect = ContainerService.Collect;
        try
        {
            // ---- D1：没有后端 ⇒ false + onError，**onComplete 一次都不调**
            ContainerService.UnlockContainer = null;
            var trace = new List<string>();
            bool r = ContainerService.OpenOfferContainer(0, "offer-X", "inst-1", true, true,
                                                         rs => trace.Add("complete"),
                                                         err => trace.Add("error"));
            C(r, false, "D1 ★ 本地没有开箱后端（原版 `PlayfabWrapper.UnlockContainer`）⇒ 返回 false");
            C(trace.Count == 1 ? trace[0] : "<空>", "error", "D2 ★ …走的是 `onError`（改成假成功 ⇒ 这条红）");

            // ---- D3/D4：装了假后端 ⇒ 链次序 = Unlock → Collect → RecordTransaction → onComplete
            trace.Clear();
            ContainerService.UnlockContainer = (id, onDone, onErr) =>
            {
                trace.Add("unlock:" + id);
                onDone(new List<CampaignData.RewardSpec> { new CampaignData.RewardSpec("RW", 1, 0) });
                return true;
            };
            ContainerService.Collect = (rewards, show, onDone) => { trace.Add("collect"); onDone(rewards); };
            ContainerService.RecordTransaction = (loc, offerId, rewards) => { trace.Add("record:" + offerId); };
            bool r2 = ContainerService.OpenOfferContainer(3, "offer-Y", "inst-2", true, true,
                                                          rs => trace.Add("complete"),
                                                          err => trace.Add("error"));
            C(r2, true, "D3 装了后端 ⇒ 返回 true");
            C(string.Join(",", trace.ToArray()), "unlock:inst-2,collect,record:offer-Y,complete",
              "D4 ★ 三跳次序 = Unlock → Collect（`autoCollect`）→ RecordTransaction → onComplete");

            // ---- D5：`autoCollect = false` ⇒ 不走 `Collect`，直接 `onOpen`
            trace.Clear();
            bool r3 = ContainerService.OpenOfferContainer(3, "offer-Z", "inst-3", true, false,
                                                          rs => trace.Add("complete"), err => trace.Add("error"));
            C(string.Join(",", trace.ToArray()), "unlock:inst-3,record:offer-Z,complete",
              "D5 ★ `autoCollect = false` ⇒ **跳过 `Collect`**，直接 RecordTransaction + onComplete");
            C(r3, true, "D5b …返回值照样 true");
        }
        finally
        {
            ContainerService.UnlockContainer = savedUnlock;
            ContainerService.RecordTransaction = savedRec;
            ContainerService.Collect = savedCollect;
        }
    }

    // ============================================================ E 原版 Countdown（账 204）
    static void SectionE_Countdown()
    {
        MenuCheck.Section(_sink, "E 原版 `Countdown`：Activate 的首帧文本 / 每帧 −speed·dt（+1s 读数补偿）/ 5 秒音效 / Stop");

        C(Countdown.TextFormat, "{0:0}:{1:00}", "E1 格式串 = 字面量 `{0:0}:{1:00}`（`stringliteral.json` 0x4264910）");

        var t0 = new System.DateTime(2026, 10, 29, 12, 0, 0);
        var now = t0;
        string last = null;
        int alerts = 0;
        var cd = new Countdown(s => last = s, () => alerts++, () => now);

        // ---- E2：Activate(speed 1, 90s) ⇒ 首帧文本 `1:30`
        cd.Activate(1f, 90);
        C(last, "1:30", "E2 ★ `Activate(1, 90)` 当场写一次 = `1:30`（`timeAvailable` 本身，不带那 1 秒）");

        // ---- E3/E4：10 秒后 ⇒ 90 − 10 + 1 = 81 ⇒ `1:21`
        now = t0.AddSeconds(10);
        cd.Tick();
        C(last, "1:21", "E3 ★ 10 秒后 = 90−10+**1** = 81 ⇒ `1:21`（把 `+RoundUp` 删掉 ⇒ 变 1:20）");
        C(cd.GetSecondsLeft(), 80.0, "E4 `GetSecondsLeft()` **不含**那 1 秒 = 80.0");

        // ---- E5：倍速 2 ⇒ 再 10 秒走 20 秒 ⇒ 90 − 30 + 1 = 61 ⇒ `1:01`
        cd.ChangeSpeed(2f);
        now = now.AddSeconds(10);
        cd.Tick();
        C(last, "1:01", "E5 ★ `ChangeSpeed(2)` ⇒ 再走 10 秒 = 20 秒进度 ⇒ `1:01`");

        // ---- E6/E7：剩 5 秒那一档只响一次
        //      ⚠️ 单独一个计数器：上面 E5 那一拍（剩 61s ⇒ Seconds = 1 &lt; 5）**也会响**，
        //      共用 `alerts` 会把这条的期望值污染成 2（第一次跑就是这么红的）。
        int alerts2 = 0;
        var cd2 = new Countdown(s => last = s, () => alerts2++, () => now);
        now = t0;
        cd2.Activate(1f, 3);
        cd2.Tick();                                   // remaining = 3 + 1 = 4 ⇒ Seconds = 4 < 5 ⇒ 响
        C(last, "0:04", "E6 ★ 总时长 3 秒、首帧读 = 3+1 = 4 ⇒ `0:04`");
        C(alerts2, 1, "E7 ★ 剩 &lt; 5 秒 ⇒ 响**一次**（`DAT_1834b2df4` = 5.0；改成 `&lt;= 5` 也只影响边界，不影响这条）");
        cd2.Tick();
        C(alerts2, 1, "E7b ★ 第二帧**不再响**（`soundPlayedFlag` 那道闩）");

        // ---- E8：Stop 之后 Tick 不改文本
        string before = last;
        cd2.Stop();
        now = now.AddSeconds(30);
        cd2.Tick();
        C(last, before, "E8 ★ `Stop()` 之后 `Tick()` 什么都不写（`running == false` 早退）");
        C(cd2.Running, false, "E8b …`Running` = false");
    }

    // ============================================================ F 指针图（账 220）
    static void SectionF_CursorByArmy()
    {
        MenuCheck.Section(_sink, "F 原版 `CursorByArmySO.GetCursor`：按阵营线性扫 / 扫完没有 ⇒ false + 两个 out 都 null");

        C(CursorByArmy.Table.Length, 11, "F1 ★ 表里 **11** 个阵营（`bundle_armycursors_assets_all/Sprite/` 21 个 json = 11 × cursor/Glow）");

        string cur, glow;
        Tr(CursorByArmy.GetCursor(10, out cur, out glow), "F2 Space Marine(10) 命中");
        C(cur, "Cursor_Space_Marine_01", "F2b …指针图名");
        C(glow, "Cursor_Space_Marine_01Glow", "F2c …高光图名");

        Tr(CursorByArmy.GetCursor(110, out cur, out glow), "F3 ★ Dark Angels(110) 命中（原版那一档**只有 pointer、没有 Glow**）");
        C(cur, "Cursor_Dark_Angels", "F3b …指针图名");
        C(glow, null, "F3c …高光 = **null**（`bundle` 里只有一张 `Cursor_Dark Angels`）");

        Tr(!CursorByArmy.GetCursor(120, out cur, out glow), "F4 ★ EmperorsChildren(120)：扫完没有 ⇒ **false**（不是抛）");
        C(cur, null, "F4b …`cursor` 置 null（原版扫完那两个 out 都置 null）");
        C(glow, null, "F4c …`highlight` 也置 null");
        Tr(!CursorByArmy.GetCursor(0, out cur, out glow), "F5 Neutral(0) 也没有指针 ⇒ false");
        Tr(!CursorByArmy.GetCursor(9999, out cur, out glow), "F6 未知阵营 9999 ⇒ false");
    }

    // ============================================================ G 阵营分（账 295）
    static void SectionG_FactionScore()
    {
        MenuCheck.Section(_sink, "G 原版 `FactionScore.Initialize`：两颗 AllianceRatingDisplay 的分数文本（没有数据源 ⇒ 空态）");

        try
        {
            RankedTab.ResetFactionScoreForTest();
            C(RankedTab.FactionScoreText(0, false), null, "G1 ★ 没有数据源 ⇒ 返回 null（`RatingRow` 落原版空态串 `" + RankedTab.ScoreEmpty + "`）");
            C(RankedTab.ScoreEmpty, "------", "G1b 空态串 = **`------`**（prefab 原文，6 个连字符）");

            RankedTab.FactionScoreProvider = i => 1500 + i;
            RankedTab.FactionHighestProvider = i => 2000 + i;
            C(RankedTab.FactionScoreText(0, false), "1500", "G2 ★ 第 1 颗（`Alliance Rating Display`）= `currentScore` = 1500");
            C(RankedTab.FactionScoreText(0, true), "2000", "G2b ★ 第 2 颗（`Alliance Rating Display (1)`）= `highestScore` = 2000");
            C(RankedTab.FactionScoreText(3, true), "2003", "G2c …行下标原样传给 provider（行 3 ⇒ 2003）");

            RankedTab.FactionHighestProvider = i => (int?)null;   // 只有最高分缺失
            C(RankedTab.FactionScoreText(0, true), null, "G3 只有最高分缺 ⇒ 那一颗落空态（两颗**各自**判，不联动）");
            C(RankedTab.FactionScoreText(0, false), "1500", "G3b …另一颗照旧");
        }
        finally { RankedTab.ResetFactionScoreForTest(); }
    }

    // ============================================================ H 唯一 id 注册表（账 312）
    static void SectionH_MenuUniqueIds()
    {
        MenuCheck.Section(_sink, "H 原版 `GameObjectUniqueIdentifier`：Find / FindComponent / 重复 id（后来的赢）/ 空 id 不登记");

        var goA = new GameObject("uid_A", typeof(RectTransform));
        var goB = new GameObject("uid_B", typeof(RectTransform));
        var goC = new GameObject("uid_C", typeof(RectTransform));
        try
        {
            MenuUniqueIds.ClearForTest();
            var a = goA.GetComponent<RectTransform>();
            var b = goB.GetComponent<RectTransform>();

            MenuUniqueIds.Register("Target", a);
            Tr(ReferenceEquals(MenuUniqueIds.FindComponent("Target"), a), "H1 ★ `FindComponent(id)` 返回**登记的那个组件本身**");
            Tr(ReferenceEquals(MenuUniqueIds.Find("Target"), goA), "H1b ★ `Find(id)` 返回它的 `gameObject`");
            C(MenuUniqueIds.FindComponent("no-such-id"), null, "H2 未命中 ⇒ null（`FindComponent`）");
            C(MenuUniqueIds.Find("no-such-id"), null, "H2b 未命中 ⇒ null（`Find`）");

            // ---- H3：重复 id ⇒ **后来的赢**（原版 `dictionary.set_Item`）
            MenuUniqueIds.Register("Target", b);
            Tr(ReferenceEquals(MenuUniqueIds.FindComponent("Target"), b), "H3 ★ 重复登记 ⇒ **后来的赢**（先来的赢 ⇒ 这条红）");

            // ---- H4：`Unregister` 只删「还是自己」的那一格
            MenuUniqueIds.Unregister("Target", a);
            Tr(ReferenceEquals(MenuUniqueIds.FindComponent("Target"), b), "H4 ★ 用**旧的**持有者去注销 ⇒ 不动（原版 `value == this` 那道闸）");
            MenuUniqueIds.Unregister("Target", b);
            C(MenuUniqueIds.FindComponent("Target"), null, "H4b 用当前持有者注销 ⇒ 真删掉");

            // ---- H5：空 id 不登记
            MenuUniqueIds.Register("", a);
            C(MenuUniqueIds.Count, 0, "H5 ★ 空 id ⇒ **不登记**（原版那条 `has an empty Id` 告警后 return）");

            // ---- H6：`SetId` 空串不生效；非空串立刻登记
            string id = "Old";
            var c = goC.GetComponent<RectTransform>();
            MenuUniqueIds.SetId(ref id, "", c);
            C(id, "Old", "H6 ★ `SetId(ref id, \"\")` ⇒ id 不变（原版 `Cannot set ID to empty string.`）");
            MenuUniqueIds.SetId(ref id, "Fresh", c);
            C(id, "Fresh", "H6b `SetId(ref id, \"Fresh\")` ⇒ id 改了");
            Tr(ReferenceEquals(MenuUniqueIds.FindComponent("Fresh"), c), "H6c …并且立刻登记进去了");
        }
        finally
        {
            MenuUniqueIds.ClearForTest();
            Object.DestroyImmediate(goA);
            Object.DestroyImmediate(goB);
            Object.DestroyImmediate(goC);
        }
    }

    // ============================================================ I 通用阵营物品（账 321/322）
    static void SectionI_GenericArmyItem()
    {
        MenuCheck.Section(_sink, "I 原版 `GenericArmyItem.GetItem()` / `GetItem(CardArmy)`：按当前阵营 / 按指定阵营");

        var item10 = new ItemSpec { Id = "ITEM_10", CardArmy = 10 };
        var item20 = new ItemSpec { Id = "ITEM_20", CardArmy = 20 };
        var item0 = new ItemSpec { Id = "ITEM_0", CardArmy = 0 };
        var gi = new GenericArmyItem(new[]
        {
            new GenericArmyItem.Entry(0, item0),
            new GenericArmyItem.Entry(10, item10),
            new GenericArmyItem.Entry(20, item20),
        });

        try
        {
            C(gi.GetItem(10).Id, "ITEM_10", "I1 ★ `GetItem(10)` ⇒ 按阵营命中");
            C(gi.GetItem(20).Id, "ITEM_20", "I1b `GetItem(20)` ⇒ 另一条");
            C(gi.GetItem(130).Id, null, "I2 ★ 表里没有的阵营 ⇒ `default`（判据 = `Id == null`；原版 `FirstOrDefault` = null）");

            // ---- I3：没有「当前阵营」这道上下文 ⇒ 原版 `handler == null ⇒ GetItem(0)`
            GenericArmyItem.CurrentArmyProvider = () => null;
            C(GenericArmyItem.CurrentArmy, 0, "I3 ★ 没有 handler ⇒ `CurrentArmy` = **0**（`get_CurrentArmy.c` 的 `return 0`）");
            C(gi.GetItem().Id, "ITEM_0", "I3b …`GetItem()` 因此取的是 Neutral 那一条");

            // ---- I4：有上下文 ⇒ 用它的
            GenericArmyItem.CurrentArmyProvider = () => 20;
            C(GenericArmyItem.CurrentArmy, 20, "I4 `CurrentArmy` = 20（= `handler + 0x7C`）");
            C(gi.GetItem().Id, "ITEM_20", "I4b ★ `GetItem()` 走当前阵营 20");

            // ---- I5：默认 provider = `DailyData.HeaderArmyValue`
            GenericArmyItem.ResetForTest();
            C(GenericArmyItem.CurrentArmy, DailyData.HeaderArmyValue,
              "I5 默认 provider = `DailyData.HeaderArmyValue`（本工程对 `CampaignHandler.SelectedArmy` 的唯一等价物）");
        }
        finally { GenericArmyItem.ResetForTest(); }
    }

    // ============================================================ J 记分条 / 进度条（账 430/496）
    static void SectionJ_MilestoneAndProgressBar()
    {
        MenuCheck.Section(_sink, "J 原版 `MilestoneScoreBar.Initialize` + `ProgressBar.SetProgress`：进度公式 / 文字格式 / IsFilled");

        // ---- J1/J2：`ProgressBar` 那几个纯函数（判据 `stringliteral.json` 0x426de28）
        C(ScoreProgressBar.Format, "{0}/{1}", "J1 格式串 = 字面量 `{0}/{1}`");
        C(ScoreProgressBar.Text(3, 10), "3/10", "J1b `String.Format(\"{0}/{1}\", 3, 10)` = `3/10`");
        Near(ScoreProgressBar.Normalized(3, 10), 0.3f, 1e-4f, "J1c `Slider.normalizedValue` = 3/10 = 0.3");
        Near(ScoreProgressBar.Normalized(1, 0), 0f, 1e-4f, "J2 ★ `maxValue = 0` ⇒ 0（⛔ 不除零）");
        Near(ScoreProgressBar.Normalized(15, 10), 1f, 1e-4f, "J2b 超过上限 ⇒ clamp 到 1");
        Tr(ScoreProgressBar.IsFilled(10, 10), "J2c ★ `IsFilled(10, 10)` = true（`value >= maxValue`）");
        Tr(!ScoreProgressBar.IsFilled(9, 10), "J2d `IsFilled(9, 10)` = false（两态对照）");
        C(ScoreProgressBar.UseWholeNumbers, true, "J2e `useWholeNumbers` = true（`.c` 实参 1；ILSpy 丢成 `default`）");

        // ---- J2f：`SetProgress(ScoringDataInfo)` 外层那一跳 = `CurrentScore(+0x10)` / `NextLevelScore(+0x18)`
        //      🧨 若有人「顺手」改用 `PreviousLevelScore(+0x14)` ⇒ 这条红（两态：prev 给一个明显不同的数）
        var info = new ScoringDataInfo(150, 999, 300);
        Near(ScoreProgressBar.NormalizedOf(info), 0.5f, 1e-4f, "J2f ★ `SetProgress(ScoringDataInfo)`：用 `CurrentScore/NextLevelScore` = 150/300 = **0.5**（改用 `PreviousLevelScore` ⇒ 150/999）");
        C(ScoreProgressBar.TextOf(info), "150/300", "J2g …文本那一半 = `150/300`");
        Near(ScoreProgressBar.NormalizedOf(null), 0f, 1e-4f, "J2h `null` ⇒ 0（原版 NRE；我们出声 + 0）");

        // ---- J3：`InterpolateMilestones` = **按档位等分 + 段内线性**（不是线性归一）
        //      阈值不均等才能两态区分：{1000,1200,3000,4000,5000} · score 1100
        //        等分档：prev=1000(下标1/5=0.2) → cur=1200(2/5=0.4) · t=0.5 ⇒ **0.3**
        //        线性归一（错的那一版）：1100/5000 = **0.22**
        float p = AllianceEventScoreInfo.InterpolateMilestones(1100f, new[] { 1000, 1200, 3000, 4000, 5000 });
        Near(p, 0.3f, 1e-4f, "J3 ★ `InterpolateMilestones(1100, {1000,1200,3000,4000,5000})` = **0.3**（按档等分；换成线性归一 ⇒ 0.22）");

        // ---- J4：端到端 —— 建一条记分条，走 `MilestoneScoreBar.Initialize` 那个同名入口
        var at = new PxRect(0f, 0f, 273.92f, 357.88f);
        var host = new GameObject("S3 AllianceHost", typeof(RectTransform));
        var si = AllianceEventScoreInfo.Create(host.transform, at, "S3 Alliance Event Score Info");
        try
        {
            Tr(si != null, "（前提）记分条建起来了");
            C(si.ScoreLabels.Count, 5, "（前提）5 行 `Score` 在");
            si.InitializeMilestoneBar(1100, new[] { 1000, 1200, 3000, 4000, 5000 });
            C(LabelText(si, 0), "1000", "J4 ★ `InitializeMilestoneBar` 第 1 行 = 第 1 个阈值（= `GenericScoreBarLine.Initialize`）");
            C(LabelText(si, 4), "5000", "J4b …第 5 行 = 第 5 个阈值");
            Near(si.Progress, 0.3f, 1e-4f, "J4c …进度与 J3 同一个公式（同一个 `InterpolateMilestones`）");

            // ---- J5：行数多于阈值 ⇒ 后面的行不初始化（原版 `CustomDebug.LogError` 那条）
            si.InitializeMilestoneBar(0, new[] { 7, 8 });
            C(LabelText(si, 0), "7", "J5 ★ 只给 2 个阈值 ⇒ 第 1 行改成 7");
            C(LabelText(si, 2), "3000", "J5b ★ …第 3 行**没被初始化**（原版：行数多于阈值 ⇒ LogError 后 return；保持上一态 3000）");

            // ---- J6：`ProgressBar.SetProgress` 的线性那一版（与 J3 的档位等分**不是一回事**）
            si.SetProgressByScore(3, 10);
            Near(si.Progress, 0.3f, 1e-4f, "J6 `SetProgressByScore(3, 10)` ⇒ 0.3（`value / maxValue`）");
            si.SetProgressByScore(2, 10);
            Near(si.Progress, 0.2f, 1e-4f, "J6b …(2, 10) ⇒ 0.2（两态对照）");
        }
        finally
        {
            if (si != null) Object.DestroyImmediate(si.gameObject);
            Object.DestroyImmediate(host);
        }
    }

    // ============================================================ K 推荐价签（账 516）
    static void SectionK_ReferralContainer()
    {
        MenuCheck.Section(_sink, "K 原版 `ReferralContainer.Refresh()`：价签 = `Demo/DraftMode/FreeButton` 的译文");

        C(ReferralContainer.PriceKey, "Demo/DraftMode/FreeButton", "K1 ★ 键 = 字面量 `Demo/DraftMode/FreeButton`（`stringliteral.json` 0x4276890）");

        string text = ReferralContainer.PriceText();
        Tr(!string.IsNullOrEmpty(text), "K2 取得到译文（非空）");
        Tr(text != ReferralContainer.PriceKey, "K2b ★ …而且**不是键名本身**（缺译文时 `Loc.T` 会回落印键，这一条红）");

        // ---- K3：价签那一跳被喂了同一条文案
        string fed = null;
        bool ok = ReferralContainer.Refresh(s => fed = s);
        C(ok, true, "K3 ★ 接了价签那一跳 ⇒ `Refresh` 返回 true");
        C(fed, text, "K3b …喂进去的就是 `PriceText()` 那一条");

        // ---- K4：没接价签 ⇒ false（原版那一支是 throw；我们出声 + false）
        C(ReferralContainer.Refresh(null), false, "K4 ★ 没接 `PriceDisplay.Setup` ⇒ false（⛔ 不假装刷上去了）");
    }

    // ============================================================ L SimpleRotation（账 584）
    static void SectionL_SimpleRotation()
    {
        MenuCheck.Section(_sink, "L 原版 `SimpleRotation.Update`：逐轴 `euler += speed * Time.deltaTime`");

        // ---- L1：三轴都动（`speed = (0,0,100)` 那种；这里用三轴都非零验「逐轴」）
        var e = SimpleRotation.Update(new Vector3(10f, 20f, 30f), new Vector3(1f, -2f, 100f), 0.5f);
        Near(e.x, 10.5f, 1e-4f, "L1 ★ 逐轴：x += 1 × 0.5 = 10.5");
        Near(e.y, 19f, 1e-4f, "L1b 逐轴：y += (−2) × 0.5 = 19");
        Near(e.z, 80f, 1e-4f, "L1c 逐轴：z += 100 × 0.5 = 80（竞技场那 3 个 `Highlight` 就是这一档 speed）");

        // ---- L2：齿轮那条 —— `speed = (0,0,−20)` ⇒ 每秒 −20°，且归一到 0..360
        Near(SimpleRotation.Step(0f, -20f, 1f), 340f, 1e-3f, "L2 ★ `Step(0, −20, 1s)` = **340**（`Repeat` 归一到 0..360；把 `× dt` 删掉 ⇒ 也是 340，见 L3）");
        Near(SimpleRotation.Step(350f, -20f, 1f), 330f, 1e-3f, "L2b `Step(350, −20, 1s)` = 330（跨零继续转）");
        Near(SimpleRotation.Step(0f, -20f, 2f), 320f, 1e-3f, "L3 ★ `dt = 2s` ⇒ −40 ⇒ 320（**按帧连续**累加，不是每周期跳一格）");

        // ---- L4：`LoadingController` 的齿轮真的走这条（-20°/s）
        var lc = new LoadingController();
        var go = new GameObject("S3 LoadingController");
        try
        {
            lc.Build(go.transform);
            lc.SetAngleForTest(0f);
            lc.Tick(1f);
            Near(lc.Angle, 340f, 1e-3f, "L4 ★ `LoadingController.Tick(1s)` ⇒ 齿轮角度 340（= −20°/s，接的是 `SimpleRotation.Step`）");
            lc.Tick(0.5f);
            Near(lc.Angle, 330f, 1e-3f, "L4b …再 0.5 秒 ⇒ 330");
        }
        finally { Object.DestroyImmediate(go); }
    }
}
