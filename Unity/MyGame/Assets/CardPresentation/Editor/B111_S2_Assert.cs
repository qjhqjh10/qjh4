// B111_S2_Assert.cs — 执行代理 `B111_S2`（2026-11-01 第一百一十一会话第 1 波）的断言：Shell 侧四条小账
//
// ============================ 它盯的是什么 ============================
//  A 组（`B-14`）：卡背包的「全解锁」播种是懒的（唯一生产点 = `CollectionWindow.CardsState`），
//    而播种那句 `if (Cards.Get(c) != null) continue;` 会**跳过**已有条目 ⇒ 玩家在第一次开收藏窗**之前**
//    领到一张卡时，那张卡会永远停在 `x1`。修法 = `CardInventory.Add` 之前先播种
//    （`Shell/Inventory.cs` 的 `Inventories.EnsureCardsSeededForCardAdd()`，追加在文件末尾的 partial 里）。
//  B 组（`B-29`）：「新卡角标 `GetOwnedCount < 2`」不是恒真、也不是恒假 —— 它读的是**真实拥有数**；
//    在全解锁口径下（≥10）恒灭，唯一的例外正好是 A 组那个 `x1`。
//  C 组（`B-16`）：`CardProgress.SetLevel` 原来**丢掉** `Save()` 的返回值 ⇒ 写盘失败也当成功（关游戏就丢）。
//    现在回 `bool` 且失败时**撤回内存**，两个调用点（`DoUpgrade` / `DoDowngrade`）据此不报成功。
//
// ============================ 判据 ============================
//  · A 组 = `判据/B110_R2_小账时效复核.md` 四① · `判据/B110_V2_真缺陷复核B.md` 顺手发现 §1
//    （施工单 B 案「把已有条目补到 n」**不成立**：播种的 amount 回调是 `CardProgress.Owned`，
//     它对已存在的条目直接回条目数量 ⇒ n 就是那个 1，补了等于没补）。
//  · B 组 = `out_il2csharp/Assembly-CSharp/RewardWindow.cs:162`（`GetOwnedCount<ObtainableItem>(item) < 2`）
//    · `判据/B110_V2` 顺手发现 §2（「恒为真」那句与现读不符 —— 实为恒假，例外 = A 组）。
//  · C 组 = `Shell/CardDetailPopup.cs` 的 `Save()` 文档：「`false` = 真的写失败（已 `LogError` 出声，
//    调用方据此**撤销内存改动 / 不报成功**）」；同族先例 = `B104_P3` 的 `AddOwned` / `TrySpendWildcard`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **A3/A4（B-14 的真正判别力）**：
//      - 先把「还没开过收藏窗」这个状态**造出来**（`_cardsState` 反射置 null + 清背包 + 清播种位），
//        并**先断前提**（背包里确实没有这张卡）；
//      - 只调**一次** `CardInventory.Add`，然后拿另一个**同稀有度同卡型**的卡当尺子：
//        `被测卡数量 == 尺子数量 + 1`。
//      - 🧨 摘掉 `EnsureCardsSeededForCardAdd()` ⇒ 背包里只有那一条、数量 = 1、尺子**没有条目** ⇒ A3 红（尺子为空）、A4 红。
//      - 🧨 把播种改成「补到 n」⇒ 那个 n 就是 1 ⇒ A4 仍红（这一条正是施工单 B 案行不通的实证）。
//  · **B1–B4**：把判据改成恒 `true` ⇒ B3/B4 红；改成恒 `false` ⇒ B1/B2 红；改成 `== 0` ⇒ B2 红。
//  · **C1/C2**：把 `SetLevel` 改回 `Save();`（不看返回值）⇒ C2 恒绿变恒红（它断的是 `false`）；
//    只 `return false` 不撤回内存 ⇒ C2b 红（等级停在 3）。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（不建窗、不依赖 UI）。**建议接在某个宿主聚合行的最末尾**（`//` 之前、同一物理行）：
//      B111_S2_Assert.Run(); _sink.Pass += B111_S2_Assert.Pass; _sink.Fail += B111_S2_Assert.Fail;
//  ⚠️ **放在最末尾的理由**：C 组会 `CardProgress.ResetForTest()`（清内存等级/拥有数/通配符，并把落盘关掉）
//     —— 同 `Editor/B104_P3_Assert.cs` 的既有做法。放在它后面的断言读不到玩家的真实等级。
//
// ============================ 副作用/恢复 ============================
//  · A 组：反射把 `CollectionWindow._cardsState` 置 null（**finally 里换回原对象**）· 卡背包
//    `SnapshotAmounts()` 快照 → 清空 → 测完「重新播种 + `RestoreAmounts`」还原；播种位 `ResetCardsSeedFlagForTest()` 一并还原。
//  · B 组：只改一张卡的条目数量，**finally 里 `SetAmount` 回原值**（`SetAmount` 不发事件，不会反推 UI）。
//  · C 组：`OverridePath` 指到 `%TEMP%` 下的一次性目录（写盘失败靠「路径是个目录」造出来，同 `B104_P3`），
//    **finally 里删临时目录 + 还原 `OverridePath` / 落盘档 / `ResetForTest()`**。⛔ 全程不碰玩家的真存档。
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B111_S2`：① `B-14` 卡背包「先播种、再加」；② `B-29` 新卡角标读真实拥有数；
/// ③ `B-16` `CardProgress.SetLevel` 判落盘返回值并撤回内存。</summary>
public static class B111_S2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111S2] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>被测的卡 id（`UM2` = C9 / 收藏线一直在用的那张，`CollectionData.Card` 认得）。</summary>
    const string Id = "UM2";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B111_S2 · Shell 侧三条：卡背包先播种（B-14）· 新卡角标口径（B-29）· `SetLevel` 判落盘（B-16）");

        try { SectionA_SeedBeforeAdd(); }
        catch (System.Exception e) { Tr(false, "「A 先播种」（B-14）这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionB_NewCardBadge(); }
        catch (System.Exception e) { Tr(false, "「B 新卡角标」（B-29）这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionC_SetLevelChecksSave(); }
        catch (System.Exception e) { Tr(false, "「C `SetLevel` 判落盘」（B-16）这一组抛异常：" + e.GetType().Name + "：" + e.Message); }

        Debug.Log(_sink.P + "B111_S2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · B-14：先播种、再加
    static void SectionA_SeedBeforeAdd()
    {
        MenuCheck.Section(_sink, "A（B-14）**先播种、再加**：第一次开收藏窗前领的那张卡不许停在 x1");

        var bag = Inventories.Cards;
        var def = CollectionData.Card(Id);
        Tr(bag != null, "A0 **前提**：`Inventories.Cards` 在（拥有数的正本）");
        Tr(def != null, "A0b **前提**：卡池认得 `" + Id + "`（`CollectionData.Card` —— 与 `ItemDrawer.Spec` 同一个取卡口）");
        if (bag == null || def == null) return;

        // 尺子：另一张**同稀有度**的卡。播种给它的基准量与给被测卡的那一份同源
        // （`CardProgress.Owned` = `DeckCap(rarity) + (NeedCopies[MaxLevel] - NeedCopies[1])`，只按稀有度走）。
        RuleEngine.CardDef ruler = null;
        foreach (var c in CollectionWindow.CardsState.Pool)
        {
            if (c == null || string.IsNullOrEmpty(c.Id) || c.Id == Id) continue;
            if (c.Rarity != def.Rarity) continue;
            ruler = c; break;
        }
        Tr(ruler != null, "A0c **前提**：卡池里找得到一张与 `" + Id + "` **同稀有度**的对照卡（它的播种基准量 = 被测卡那一份）");
        if (ruler == null) return;

        var fState = typeof(CollectionWindow).GetField("_cardsState", BindingFlags.NonPublic | BindingFlags.Static);
        Tr(fState != null, "A0d **前提**：拿得到 `CollectionWindow._cardsState`（反射；不然「还没人开过收藏窗」这个状态造不出来）");
        if (fState == null) return;

        object keptState = fState.GetValue(null);
        Dictionary<string, int> kept = bag.SnapshotAmounts();
        try
        {
            fState.SetValue(null, null);                  // 「这一进程还没人开过收藏窗」
            Inventories.ResetCardsSeedFlagForTest();      // 「这一次 Add 还没播过种」
            bag.ClearForTest();                           // 背包回到冷启动 = 空

            Tr(bag.Get(def) == null,
               "A1 **前提**：背包里现在**没有** `" + Id + "`（= 玩家在第一次开收藏窗**之前**领卡时的真实状态）");

            // 只调一次 5 参 `Add`（= `InventoryManager.Add<CardDef>` / 原版 `CollectItemReward` 那一支）。
            // ⚠️ `customData` 传 null（与 `RewardService.CollectItemReward<T>` 同形）⇒ 不碰等级。
            var info = bag.Add(def, 1, null, null, false);
            Tr(info != null, "A2 **前提**：这一次 `Add` 真的建了条目（null ⇒ 后面的数量断言无从谈起）");

            var rulerInfo = bag.Get(ruler);
            Tr(rulerInfo != null,
               "A3 ★ 只加**一张**卡，整个卡池也已经被播过种了（对照卡 `" + ruler.Id + "` 有条目）"
             + "｜🧨 摘掉 `Inventories.EnsureCardsSeededForCardAdd()`（或 `CardInventory.Add` 里那一句）⇒ 背包里只有那 1 条 ⇒ 红");

            if (info != null && rulerInfo != null)
                C(info.Amount, rulerInfo.Amount + 1,
                  "A4 ★★ 被测卡数量 = **播种基准量 + 1**（" + info.Amount + " = " + rulerInfo.Amount + " + 1）"
                + " ⇒ 领一张就 +1，⛔ 不是停在 x1"
                + "｜🧨 播种的 `if (Cards.Get(c) != null) continue;` 把已有条目跳过 ⇒ 1 ⇒ 红；"
                + "🧨 改成「已有条目补到 n」也照样红（n 由 `CardProgress.Owned` 给 = 那个 1，补了等于没补）");

            Tr(bag.Count > 100,
               "A5 ★ 这一次 `Add` 顺带把**整池**播进来了（世界条目数 = " + bag.Count + "）"
             + "｜🧨 只给被测卡建条目（= 懒播种的原状）⇒ 1 ⇒ 红");
        }
        finally
        {
            // 还原：`RestoreAmounts` 的契约是「**已在的改、不在的跳过**」⇒ 必须**先播种、再灌快照**。
            fState.SetValue(null, keptState);
            Inventories.ResetCardsSeedFlagForTest();
            bag.ClearForTest();
            Inventories.SeedCardsAllUnlocked(CollectionWindow.CardsState.Pool, c => CardProgress.Owned(c.Id, c.Rarity));
            bag.RestoreAmounts(kept);
            Debug.Log("[B111_S2] A 组收尾：卡背包已按快照还原（" + kept.Count + " 条）");
        }
    }

    // ============================================================ B · B-29：新卡角标读真实拥有数
    static void SectionB_NewCardBadge()
    {
        MenuCheck.Section(_sink, "B（B-29）「新卡角标」= 原版 `GetOwnedCount < 2`，读的是**真实拥有数**（既不是恒真也不是恒假）");

        var def = CollectionData.Card(Id);
        Tr(def != null, "B0 **前提**：卡池认得 `" + Id + "`");
        if (def == null) return;

        var info = Inventories.Cards != null ? Inventories.Cards.Get(def) : null;
        Tr(info != null,
           "B0b **前提**：背包里有这张卡的条目（角标数的就是它 —— `CardProgress.Owned` 先读背包）");
        if (info == null) return;

        int keep = info.Amount;
        try
        {
            info.SetAmount(0);
            Tr(RewardWindow.NewCardBadgeOn(Id),
               "B1 ★ 拥有数 **0** ⇒ 角标**亮**（原版 `GetOwnedCount < 2` 把「一件都没有」也算新卡）"
             + "｜🧨 把判据改成 `== 1` 或恒 `false` ⇒ 红");

            info.SetAmount(1);
            Tr(RewardWindow.NewCardBadgeOn(Id),
               "B2 ★ 拥有数 **1** ⇒ 角标**亮**（= `B-14` 那个 x1 —— 全解锁口径下唯一的例外）"
             + "｜🧨 把判据改成 `== 0` ⇒ 红");

            info.SetAmount(2);
            Tr(!RewardWindow.NewCardBadgeOn(Id),
               "B3 ★ 拥有数 **2** ⇒ 角标**灭**（`2 < 2` 为假）｜🧨 判据恒 `true` ⇒ 红");

            info.SetAmount(10);
            Tr(!RewardWindow.NewCardBadgeOn(Id),
               "B4 ★ 全解锁口径（≥10）⇒ 角标**恒灭** —— 这就是账里那句「恒**真**」的**实为恒假**"
             + "｜🧨 判据恒 `true` ⇒ 红（`B-14` 修好之后 x1 例外也消失，本组是唯一能证明「它不是写死的」的地方）");
        }
        finally { info.SetAmount(keep); }
    }

    // ============================================================ C · B-16：SetLevel 判落盘
    static void SectionC_SetLevelChecksSave()
    {
        MenuCheck.Section(_sink, "C（B-16）`CardProgress.SetLevel` 判 `Save()` 的返回值（写不进盘 ⇒ 回 false 且内存撤回）");

        string prevPath = CardProgress.OverridePath;
        bool prevPersist = CardProgress.PersistOn;
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wf_b111s2");
        try
        {
            try { System.IO.Directory.CreateDirectory(root); } catch { }
            CardProgress.ResetForTest();                 // 内存清零 + 放弃真存档（同 `B104_P3` 的写法）
            CardProgress.SetPersistForTest(true);        // 再拨回「落盘」（`ResetForTest` 会把它拨成 false 且没有恢复口）
            Tr(CardProgress.PersistOn, "C0 **前提**：本进程的落盘档**开着**（否则 `Save()` 是设计上的空操作、测不到失败支）");

            CardProgress.OverridePath = System.IO.Path.Combine(root, "cardlevels_ok.json");
            Tr(CardProgress.SetLevel(Id, 2), "C1 ★ 正常路径 ⇒ `SetLevel` 回 **true**（成功支没被误杀）");
            C(CardProgress.Level(Id), 2, "C1b 等级真的写成 2（回 true 不是空口）");

            CardProgress.OverridePath = root;            // 一个**目录** ⇒ `File.WriteAllText` 抛 ⇒ `Save()` 回 false
            Tr(!CardProgress.SetLevel(Id, 3),
               "C2 ★ 写盘失败 ⇒ `SetLevel` 回 **false**"
             + "（原来签名是 `void` 且直接丢掉 `Save()` 的返回值 ⇒ 调用方永远以为写成功了）"
             + "｜🧨 改回 `Save();` 不看返回值 ⇒ 恒 true ⇒ 红");
            C(CardProgress.Level(Id), 2,
               "C2b ★★ 写盘失败 ⇒ 内存等级**撤回**到 2（⛔ 不留「内存是新值 / 盘上还是旧值」这种假成功）"
             + "｜🧨 只 `return false` 不撤回 ⇒ 停在 3 ⇒ 红");
        }
        finally
        {
            CardProgress.OverridePath = prevPath;
            CardProgress.ResetForTest();
            CardProgress.SetPersistForTest(prevPersist);
            try { if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true); } catch { }
        }
    }
}
