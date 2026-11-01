// C9_Assert.cs — 第一百零九会话 · 执行代理 `C9`（「卡奖励画对了卡面，但领了进不了收藏」的三处断点）
//
// ============================ 它盯的是什么 ============================
//  判据 = `资料/完全复刻/判据/B109_R22_卡奖励收藏链.md`（原版 11 跳逐跳 + 我们断在哪 3 处）。
//  本轮补的三跳：
//    ① `CampaignTab.ClaimForTest` 里 J4 的**发奖循环**（原版 `RewardService__Collect.c:117`）
//    ② `RewardService.CollectItemRewardById` 认不出卡（原版 `Add<RawCardScript>` 那一句，`RewardService.cs:890`）
//    ③ `CampaignTab.SetFirstTutorialTrackArmyNodeDone` 里 J11 的**首节点补发**（原版 CloudScript `0x45e`）
//  实现落点：`Shell/RewardServicePartial_C9.cs` · `Shell/CampaignTabPartial_C9.cs`（两个 partial 文件）
//            + `Shell/Inventory.cs:2036` / `Shell/CampaignTab.cs:1216,:1334` 的**同一物理行**追加（零行数变化）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A 组：把 `CollectItemRewardCard` 的调用从 `CollectItemRewardById` 摘掉 ⇒ A1/A2 红（今天改前就是这个状态：
//          `Note("rs-item:unresolved")` 之后 return，卡背包零变化）。
//  · A 组：让 `CollectItemRewardCard` 对任何 id 都返回 true（= 不查卡池）⇒ E 组红（预组 / 异画被当成卡）。
//  · B 组：`customData` 传 null（= 原版写法但**在我们的落点上**会触发 `SetLevel(id,1)`）⇒ B1 红。这就是判据 §3·4 那条副作用。
//  · C 组：把 `CollectSingleReward` 的 ④ 支直接改成 `CollectItemReward`（跳过卡解析口）⇒ C2 红（C2 是货币 code，不是卡）。
//  · D 组：把 `ClaimForTest` 里的 `GrantCampaignRewards(...)` 删掉 ⇒ D2 红（端到端 +0）。
//  · G 组：把 `SetFirstTutorialTrackArmyNodeDone` 里那句删掉 ⇒ G2 红（首节点基础档的 `ClaimedCount` 不再 +1 之前
//          那条 `Claim` 断言仍绿 —— 所以 G2 断的是**发放那一刻不抛且状态照旧**，它守的是「这一行没把教程链改坏」）。
//           ⚠️ 节点 0 的基础档只有 `UM_SK_Starter`（预组，不是卡）⇒ 这一组**断不出「发了什么」**，如实写在这里。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（只要一个空场景；`WindowsManager.EnsureHost` 会自己建壳）：
//      任一宿主的聚合行、`//` **之前**同一物理行追加：
//      C9_Assert.Run(); pass += C9_Assert.Pass; fail += C9_Assert.Fail;
//  建议落点：`Editor/ShellScene.cs`（战役页 + 背包框架都在外壳侧）或 `Editor/RewardsScene.cs`。
//
// ============================ 副作用 / 恢复 ============================
//  · B 组会临时把 `UM2` 的等级设成 3，**finally 里恢复原值**（`CardProgress.Level` 是**落盘**的持久正本）。
//  · D/G 组会 `CampaignData.ResetForTest()` + `SetPointsForTest`，**finally 里都还原**。
//  · D/G 会真的开 `RewardWindow`（`ShowCollected`）——`EnsureHost` 在编辑模式下是安全的（它显式登记 `Instance`）。
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `C9`：战役奖励 → `CollectSingleReward` → `CardInventory.Add` → 卡背包（= 「进收藏」的唯一判据）。</summary>
public static class C9_Assert
{
    static readonly CheckSink _sink = new CheckSink("[C9] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>卡背包里这个 id 的数量（没有这条 ⇒ 0）。**读数口 = `Inventories.Cards`**（拥有数的正本）。</summary>
    static int Amt(string id)
    {
        var inv = Inventories.Cards;
        if (inv == null || string.IsNullOrEmpty(id)) return 0;
        var info = inv.Get(id);
        return info == null ? 0 : info.Amount;
    }

    /// <summary>自包含：只要一个空场景就能跑。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "C9 · 卡奖励领取链：`CollectSingleReward → CollectItemReward → CardInventory.Add`（判据 = `判据/B109_R22_卡奖励收藏链.md`）");
        try { SectionA_Primitive(); }
        catch (System.Exception e) { Tr(false, "「A 原语」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionB_TierPreserved(); }
        catch (System.Exception e) { Tr(false, "「B 等级」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionC_Dispatch(); }
        catch (System.Exception e) { Tr(false, "「C 分派」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionE_Negative(); }
        catch (System.Exception e) { Tr(false, "「E 反例」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionG_TutorialFirstNode(); }
        catch (System.Exception e) { Tr(false, "「G 教程首节点」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionD_EndToEnd(); }
        catch (System.Exception e) { Tr(false, "「D 端到端」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionF_Regression(); }
        catch (System.Exception e) { Tr(false, "「F 回归」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "C9 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 原语：`CollectItemReward(RewardInfo)` 发得进卡背包
    static void SectionA_Primitive()
    {
        MenuCheck.Section(_sink, "A 原语：`CollectItemReward(new RewardInfo(\"UM2\", 1))` ⇒ 卡背包恰好 +1");

        Tr(CollectionData.Card("UM2") != null,
           "A0 **前提**：卡池认得 `UM2`（`CollectionData.Card` —— 与 `ItemDrawer.Spec` / `CardDrawerItem` **同一个取卡口**）");
        if (CollectionData.Card("UM2") == null) return;
        Tr(Inventories.Cards != null, "A0b **前提**：`Inventories.Cards` 在（拥有数的正本）");
        if (Inventories.Cards == null) return;

        // 🔴 2026-11-01（`B113W` 就地订正 · 夹具前提）：**第一次发奖会把卡背包懒加载**（存档里 UM2/UM3 本来就有 11 张）
        //   ⇒ 直接量基线会读到 0、发完读到「存档值 + 1」，A1/A2 假红（实测 0→12 / 0→11）。
        //   先发一次把加载走掉，再量基线、再发一次断 +1。
        RewardService.CollectItemReward(new RewardInfo("UM2", 1));

        int beforeUm2 = Amt("UM2"), beforeUm3 = Amt("UM3");

        // 判据 §3·5 A：public 入口，⛔ 不直接调 private 的 `CollectItemRewardById`
        RewardService.CollectItemReward(new RewardInfo("UM2", 1));

        int afterUm2 = Amt("UM2"), afterUm3 = Amt("UM3");
        C(afterUm2, beforeUm2 + 1,
           "A1 ★ `CollectItemReward(RewardInfo(\"UM2\",1))` ⇒ `Inventories.Cards.Get(\"UM2\").Amount` **恰好 +1**"
         + "（" + beforeUm2 + " → " + afterUm2 + "）｜🧨 摘掉 `CollectItemRewardById` 里那句 `CollectItemRewardCard` ⇒ 本条红（+0）");
        C(afterUm3, beforeUm3,
           "A2 ★ 另一张卡 `UM3` **一点没动**（" + beforeUm3 + " → " + afterUm3 + "）｜🧨 全池 +1 ⇒ 红");
    }

    // ============================================================ B · 等级：发了卡不许把玩家升过的档打回 1 级
    static void SectionB_TierPreserved()
    {
        MenuCheck.Section(_sink, "B 等级：已升到 3 级的卡发一张之后**仍是 3 级**（判据 §3·4 的副作用）");

        if (CollectionData.Card("UM2") == null) { Tr(false, "B0 **前提**：卡池认得 `UM2`"); return; }

        int orig = CardProgress.Level("UM2");
        try
        {
            CardProgress.SetLevel("UM2", 3);
            C(CardProgress.Level("UM2"), 3, "B0 **前提**：`SetLevel(\"UM2\", 3)` 生效了");

            RewardService.CollectItemReward(new RewardInfo("UM2", 1));

            C(CardProgress.Level("UM2"), 3,
               "B1 ★ 发一张 `UM2` 之后**仍是 3 级**（原版这一跳只写运行时 `TierCollection`，对玩家可见等级零影响）"
             + "｜🧨 `CollectItemRewardCard` 里传 `customData: null` ⇒ `SetLevel(id, 1)` ⇒ 本条红（这正是判据 §3·4 那条）");
        }
        finally
        {
            CardProgress.SetLevel("UM2", orig);      // ⚠️ `SetLevel` 会落盘 ⇒ 必须还原
        }
    }

    // ============================================================ C · 分派：`CollectSingleReward` 的两条支
    static void SectionC_Dispatch()
    {
        MenuCheck.Section(_sink, "C 分派：`CollectSingleReward` 的卡支 +1；货币支不进卡背包、也不抛");

        if (CollectionData.Card("UM2") == null) { Tr(false, "C0 **前提**：卡池认得 `UM2`"); return; }
        if (Inventories.Cards == null) { Tr(false, "C0 **前提**：`Inventories.Cards` 在"); return; }

        int before = Amt("UM2");
        RewardService.CollectSingleReward(RewardInfo.FromSpec(new CampaignData.RewardSpec
        { Id = "UM2", Quantity = 1, Tier = CampaignData.TierBasic }));
        C(Amt("UM2"), before + 1,
           "C1 ★ `CollectSingleReward(UM2)` ⇒ 卡背包 +1（" + before + " → " + Amt("UM2") + "）"
         + "｜🧨 ④ 支直接 `CollectItemReward`（绕过卡解析口）⇒ 红");

        // C2：货币 code `C2` —— 本条要求「卡背包零变化」且「不抛」（`CollectSingleReward` 对它是货币/判不出那一档）
        int bagCount = Inventories.Cards.Count;
        bool threw = false;
        try
        {
            RewardService.CollectSingleReward(RewardInfo.FromSpec(new CampaignData.RewardSpec
            { Id = "C2", Quantity = 1, Tier = CampaignData.TierBasic }));
        }
        catch (System.Exception e) { threw = true; Tr(false, "C2 收到异常：" + e.GetType().Name + "：" + e.Message); }

        Tr(!threw, "C2 ★ 货币 code `C2` 走 `CollectSingleReward` **不抛**（原版这一档走 `CollectCurrencyReward`；我们没有货币背包 ⇒ 出声、不发）");
        C(Inventories.Cards.Count, bagCount,
           "C3 ★ `C2` 之后**卡背包条目数不变**（" + bagCount + " → " + Inventories.Cards.Count + "）"
         + "｜🧨 把非卡 id 也当卡加 ⇒ 红");
    }

    // ============================================================ E · 反例：预组 / 异画包装不许当卡发
    static void SectionE_Negative()
    {
        MenuCheck.Section(_sink, "E 反例：`UM_SK_Starter`（预组，`UM` 前缀诱饵）· `#HB_SW1`（异画包装）都不进卡背包");

        if (Inventories.Cards == null) { Tr(false, "E0 **前提**：`Inventories.Cards` 在"); return; }

        // E1：预组 id —— 战役真数据里就有（节点 0 的基础档），但它**不是卡**
        int bag1 = Inventories.Cards.Count;
        C(CollectionData.Card("UM_SK_Starter"), null,
           "E0a **前提**：`UM_SK_Starter` 在卡池里**查不到**（它是 `PrebuiltDeck`）");
        RewardService.CollectItemReward(new RewardInfo("UM_SK_Starter", 1));
        C(Inventories.Cards.Count, bag1,
           "E1 ★ `UM_SK_Starter` 之后**卡背包条目数不变**（" + bag1 + " → " + Inventories.Cards.Count + "）"
         + "｜🧨 把判据放宽成「`UM` 开头就是卡」⇒ 红");

        // E2：异画包装 `#HB_SW1` —— `CollectionData.Card` 会剥 `#` 查回 `SW1`，但类型是 `AlternateArtCard` ⇒ 不是 RawCardScript
        var def = CollectionData.Card("#HB_SW1");
        if (def != null)
        {
            C(def.Id, "SW1", "E0b **前提**：`CollectionData.Card(\"#HB_SW1\")` 剥前缀查回 `SW1`（`def.Id != \"#HB_SW1\"`）");
            int bag2 = Inventories.Cards.Count, sw1 = Amt("SW1");
            RewardService.CollectItemReward(new RewardInfo("#HB_SW1", 1));
            C(Inventories.Cards.Count, bag2,
               "E2 ★ `#HB_SW1` 之后**卡背包条目数不变**（" + bag2 + " → " + Inventories.Cards.Count + "）"
             + "｜🧨 判据写成「`CollectionData.Card(id) != null` 就发」（不看 `def.Id == id`）⇒ 本条红");
            C(Amt("SW1"), sw1,
               "E3 ★ 而且**没有**顺手把原卡 `SW1` 加 1（" + sw1 + " → " + Amt("SW1") + "）—— 异画不是 `RawCardScript`");
        }
        else Tr(true, "E0b（跳过）`#HB_SW1` 在卡池里查不到 —— 卡池不含异画，反例这一条无从构造（如实标，不假装验过）");
    }

    // ============================================================ G · 教程首节点（J11）没被改坏
    static void SectionG_TutorialFirstNode()
    {
        MenuCheck.Section(_sink, "G 教程首节点：`SetFirstTutorialTrackArmyNodeDone` 仍能领到（J11 的补发没把这条链改坏）");

        CampaignData.ResetForTest();
        CampaignData.SetPointsForTest(CampaignData.Points);
        var go = new GameObject("C9_Assert_CampaignTab");
        CampaignTab tab = null;
        try
        {
            tab = go.AddComponent<CampaignTab>();
            Tr(tab != null, "G0 **前提**：`CampaignTab` 挂得上去（编辑模式 `AddComponent`）");
            if (tab == null) return;
            C(CampaignData.StateOf(0), CampaignData.Unlocked, "G0b **前提**：`ResetForTest` 之后根节点 = `Unlocked`");
            C(CampaignData.ClaimedCount, 0, "G0c **前提**：起手一格都没领过");

            bool ok = false;
            try { ok = tab.SetFirstTutorialTrackArmyNodeDone(); }
            catch (System.Exception e) { Tr(false, "G1 抛异常：" + e.GetType().Name + "：" + e.Message); }

            Tr(ok, "G1 ★ `SetFirstTutorialTrackArmyNodeDone()` 返回 true（首节点确实领到了）");
            C(CampaignData.ClaimedCount, 1,
               "G2 ★ 基础档领了一格（" + CampaignData.ClaimedCount + "）—— 新加的那句 `GrantCampaignRewards(0, Basic)` 没有打断这条链"
             + "｜⚠️ 节点 0 的基础档只有 `UM_SK_Starter`（预组，不是卡）⇒ **这一组断不出「发了什么」**，如实标");
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
            CampaignData.ResetPointsForTest();
            CampaignData.ResetForTest();
        }
    }

    // ============================================================ D · 端到端：`ClaimForTest` 前后目标卡 +1
    static void SectionD_EndToEnd()
    {
        MenuCheck.Section(_sink, "D 端到端：`ClaimForTest` 一个**基础档含卡**的节点 ⇒ 那张卡 +1（J4 的发奖循环接上了）");

        CampaignData.ResetForTest();
        CampaignData.SetPointsForTest(1000000);           // 逐节点领的时候点数闸别挡路（`Claim` 不扣点）
        var go = new GameObject("C9_Assert_CampaignTab_D");
        CampaignTab tab = null;
        try
        {
            tab = go.AddComponent<CampaignTab>();
            Tr(tab != null, "D0 **前提**：`CampaignTab` 挂得上去");
            if (tab == null) return;

            int nodes = CampaignData.NodeCount;
            Tr(nodes > 0, "D0b **前提**：`CampaignData.NodeCount = " + nodes + "`");
            if (nodes <= 0) return;

            string targetId = null; int targetNode = -1; int walked = 0;
            for (int i = 0; i < nodes && targetId == null; i++)
            {
                if (!CampaignData.Claimable(i)) continue;            // 还没解锁 ⇒ 跳过（领过的成功者会解锁后继）
                var rs = CampaignData.RewardsOf(i, CampaignData.TierBasic);
                string cid = FirstCardId(rs);
                if (cid != null)
                {
                    int before = Amt(cid);
                    bool ok = tab.ClaimForTest(i, CampaignData.TierBasic);
                    // 🔴 2026-11-01（`B-88` 接线前置）：`ClaimForTest` 已改成「**关窗回调里才 `Claim`**」
                    //   （原版次序：先发奖 → 再开领奖窗 → 关窗那一拍才翻节点，`Shell/CampaignTab.cs:1326`）。
                    //   不关窗 ⇒ 节点永远不翻 ⇒ 后继不解锁（D 组的 `Claimable(i)` 恒 false），
                    //   而且会留一扇开着的领奖窗污染后面的段。⇒ **每次 `ClaimForTest` 之后立刻关掉**。
                    RewardWindowFixture.DismissRewardWindows();
                    int after = Amt(cid);
                    targetId = cid; targetNode = i;
                    Tr(ok, "D1 **前提**：`ClaimForTest(" + i + ", Basic)` 领到了（节点 " + CampaignData.NodeName(i) + "）");
                    C(after, before + 1,
                       "D2 ★ 端到端：领了节点 `" + CampaignData.NodeName(i) + "`（" + i + "）之后，它基础档里的卡 `" + cid
                     + "` **恰好 +1**（" + before + " → " + after + "）"
                     + "｜🧨 删掉 `ClaimForTest` 里那句 `GrantCampaignRewards(i, tier)` ⇒ 本条红");
                }
                else
                {
                    if (tab.ClaimForTest(i, CampaignData.TierBasic)) walked++;   // 只为解锁后继
                    RewardWindowFixture.DismissRewardWindows();                  // 🔴 同上：不关窗 ⇒ 节点不翻 ⇒ 后继不解锁
                }
            }

            Tr(targetId != null,
               "D3 ★ 从根一路领过去找到了第一个「基础档含卡」的节点（" + (targetId == null ? "一个都没找到" :
               "节点 " + targetNode + " ⇒ 卡 `" + targetId + "`；之前领了 " + walked + " 个无卡节点") + "）"
             + "｜⚠️ 一个都没找到 = 战役数据变了（`CampaignData` 的 `Rewards[]`），回来重核这条断言，⛔ 别删它");
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
            RewardWindowFixture.DismissRewardWindows();   // 🔴 兜底：这一组中途抛异常时也别把开着的领奖窗留给后面几段
            CampaignData.ResetPointsForTest();
            CampaignData.ResetForTest();
        }
    }

    /// <summary>`RewardsOf` 里第一个**真卡** id（判据同实现：`CollectionData.Card(id)` 且 `def.Id == id`）。</summary>
    static string FirstCardId(CampaignData.RewardSpec[] rs)
    {
        if (rs == null) return null;
        for (int k = 0; k < rs.Length; k++)
        {
            string id = rs[k].Id;
            if (string.IsNullOrEmpty(id)) continue;
            var def = CollectionData.Card(id);
            if (def != null && def.Id == id) return id;
        }
        return null;
    }

    // ============================================================ F · 回归：卡面那一侧（E26）还成立
    static void SectionF_Regression()
    {
        MenuCheck.Section(_sink, "F 回归：`ItemDrawer.Spec(\"UM2\")` 仍是 `RawCardScript`（`B109_E26` 那一侧没被这次改动碰到）");

        C(ItemDrawer.Spec("UM2", null, null).Type, ItemDrawer.TypeRawCardScript,
          "F1 ★ `Spec(\"UM2\").Type` 仍是 `RawCardScript` —— 「画对了卡面」那一跳（`Shell/ItemDrawerPartial_B109E26.cs`）与「发得进背包」这一跳用的是**同一个取卡口**");
    }
}
