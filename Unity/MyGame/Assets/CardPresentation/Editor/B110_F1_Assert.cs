// B110_F1_Assert.cs — 执行代理 `B110_F1`（2026-10-31 第一百一十会话）的断言：**🔴6 卡等级被打回 1 级**
//
// ============================ 它盯的是什么 ============================
//  ① `CardInventory.Add → UpdateCardTier`：`customData` 里**没有** `CardTier` 时，原版只写「运行时 `TierCollection`」
//     （对玩家可见状态零影响），我们却写进了**持久**的 `CardProgress` ⇒ 一次「领一张卡」就能把已升到 3 级的卡
//     **打回 1 级并落盘**。修法 = `Shell/Inventory.cs:1392` `if (s == null) s = "0";` → `if (s == null) return;`
//     （同一物理行、零行数变化）。
//  ② `CardProgress.SetLevel` 不调 `EnsureLoaded()`：`Save()` 会把当时的 `_lvl`/`_owned`/`_wild` **整份写回**
//     ⇒ 在任何人摸过 `Level()`/`Owned()` 之前调一次，就会把玩家的等级/拥有数/通配符**整份清空**。
//     修法 = `Shell/CardDetailPopup.cs:302` 同一物理行追加 `EnsureLoaded();`。
//
// ============================ 判据（原版侧）============================
//  · `Shell/CardInventory` 的原版判据 = `判据/B110_V1_真缺陷复核A.md` §🔴6：
//    原版 `UpdateCardTier` → `CardServices.RegisterCardTier` → `TierCollection`；而 `CardInventory` 的 ctor
//    每局 `CleanTierCollection()` 清空、由服务端 `CardTierConfig` 重建 ⇒ **不持久、玩家不可见**。
//  · 我们的落点 `CardProgress` 是**持久**等级（`WarpforgeCardLevels.json`），与原版那一跳不同义
//    ⇒ 缺档位时**必须不写**，否则就是把「运行时表」的语义硬塞进「持久表」。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **B 组**：把 `:1392` 改回 `s = "0"` ⇒ 等级 3 → 1 ⇒ B1 红。
//  · **C 组（判别力闸，起始等级与 B 不同）**：起始 = **1 级**、带 `CardTier=2` 加一张 ⇒ 期望 **3 级**。
//    ⚠️ 为什么必须换起始等级：若 C 也起于 3 级，则「把整个 `UpdateCardTier` 调用删掉」会让 **B 和 C 一起绿**
//    （等级停在 3 = 自证）。换到 1 级之后，「删掉调用」⇒ 停在 1 ⇒ C1 红；
//    而「只删 `SetLevel` 那一句」⇒ C1 红、B1 绿（两条各自有判别力）。
//  · **D 组**：谁为了「不动等级」把整个 `Add` 短路掉（不建条目 / 不加数量）⇒ D1/D2 红。
//  · **E 组**：去掉 `SetLevel` 里的 `EnsureLoaded()` ⇒ 内存表从空起步、`Save()` 把整份写成只剩这次写的那一条
//    ⇒ E2（盘上的等级 3）/E3（拥有数 7）/E4（通配符 42）全部读到缺省值 ⇒ 红。
//    ⚠️ E1 是 E 组的前提闸：这次写入**必须**真的发生（否则「什么都没做」也会让 E2–E4 变绿）。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（只要一个空场景就能跑；不建窗、不依赖 UI）：
//      任一宿主的聚合行、`//` **之前**同一物理行追加：
//      B110_F1_Assert.Run(); _sink.Pass += B110_F1_Assert.Pass; _sink.Fail += B110_F1_Assert.Fail;
//  建议落点：`Editor/CollectionScene.cs` 或 `Editor/RewardsScene.cs`（卡的等级 / 拥有数那两条线）。
//
// ============================ 副作用/恢复 ============================
//  · A–D 会临时改 `UM2` 的等级（**落盘**的持久正本）与卡背包数量，**finally 里把等级还原**；
//    背包数量按 +1/+1 走到自然终值（背包是「全解锁」播种的运行时表，不落盘到玩家存档的同一格）。
//  · E 用 `CardProgress.OverridePath` 指到 `%TEMP%` 的一次性文件，**跑完删掉 + 还原**；
//    另用反射临时摆 `_loaded`/`_persist` 与三张私有字典，**finally 里逐项还原**（不碰玩家真存档）。
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B110_F1`：`CardInventory.Add → UpdateCardTier` 不许把已升级的卡打回 1 级；
/// `CardProgress.SetLevel` 必须先 `EnsureLoaded()` 再写（否则整份存档被清空）。</summary>
public static class B110_F1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B110F1] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>被测的卡 id（`UM2` = C9 / 收藏线一直在用的那张，`CollectionData.Card` 认得）。</summary>
    const string Id = "UM2";

    /// <summary>卡背包里这个 id 的数量（没有这条 ⇒ 0）。读数口 = `Inventories.Cards`（拥有数的正本）。</summary>
    static int Amt(string id)
    {
        var inv = Inventories.Cards;
        if (inv == null || string.IsNullOrEmpty(id)) return 0;
        var info = inv.Get(id);
        return info == null ? 0 : info.Amount;
    }

    /// <summary>B 组前后的数量（D 组用它断「数量守恒」，两边隔着一个方法，单独存）。</summary>
    static int _amtBefore, _amtAfter;

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B110_F1 · 🔴6：`CardInventory.Add → UpdateCardTier` 不再把已升级的卡打回 1 级 · `SetLevel` 先读档");

        int orig = CardProgress.Level(Id);         // 先把原值读出来（顺带把档读进内存，后面的还原才有意义）
        bool setupOk = CollectionData.Card(Id) != null && Inventories.Cards != null;
        try
        {
            if (!setupOk)
            {
                Tr(CollectionData.Card(Id) != null,
                   "A0 **前提**：卡池认得 `" + Id + "`（`CollectionData.Card` —— 与 `ItemDrawer.Spec` 同一个取卡口）");
                Tr(Inventories.Cards != null, "A0b **前提**：`Inventories.Cards` 在（拥有数的正本）");
                return;
            }
            try { SectionA_Setup(); }
            catch (System.Exception e) { Tr(false, "「A 前提」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
            try { SectionB_NullCustomDataKeepsLevel(); }
            catch (System.Exception e) { Tr(false, "「B 结论」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
            try { SectionC_ExplicitTierStillWrites(); }
            catch (System.Exception e) { Tr(false, "「C 灭自证」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
            try { SectionD_AmountConserved(); }
            catch (System.Exception e) { Tr(false, "「D 数量守恒」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        }
        finally
        {
            CardProgress.SetLevel(Id, orig);       // ⚠️ `SetLevel` 会落盘 ⇒ 必须还原成进本组之前的值
        }

        try { SectionE_SetLevelLoadsBeforeWrite(); }
        catch (System.Exception e) { Tr(false, "「E 先读档」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }

        Debug.Log(_sink.P + "B110_F1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 前提：先把这张卡摆到 3 级
    static void SectionA_Setup()
    {
        MenuCheck.Section(_sink, "A 前提：`UM2` 摆到 3 级、卡背包条目在");

        var def = CollectionData.Card(Id);
        Tr(def != null, "A0 **前提**：卡池认得 `" + Id + "`");
        Tr(Inventories.Cards != null, "A0b **前提**：`Inventories.Cards` 在");
        if (def == null || Inventories.Cards == null) return;

        CardProgress.SetLevel(Id, 3);
        C(CardProgress.Level(Id), 3,
          "A1 **前提**：`SetLevel(\"" + Id + "\", 3)` 生效了（= **3 级**；⛔ 这是下面 B 组的起始等级）");
        C(Amt(Id) >= 0, true, "A2 **前提**：卡背包查得到这张卡的数量（" + Amt(Id) + "）");
    }

    // ============================================================ B · ★ 结论：customData = null（原版形态）⇒ 等级不动
    static void SectionB_NullCustomDataKeepsLevel()
    {
        MenuCheck.Section(_sink, "B 结论：`InventoryManager.Add<CardDef>(card)`（`customData` 传 null，= 原版 `CollectItemReward<T>` 的形态）⇒ 3 级**仍是 3 级**");

        var def = CollectionData.Card(Id);
        if (def == null) { Tr(false, "B0 **前提**：卡池认得 `" + Id + "`"); return; }

        _amtBefore = Amt(Id);

        // ⚠️ 走的是 public 门面（原版 `InventoryManager.Add<T>`），不是直接调 `UpdateCardTier` ——
        //    这条链 = `InventoryManager.Add<T>` → `Inventory<T>.Add`(virtual) → `CardInventory.Add` → `UpdateCardTier`。
        InventoryManager.Add<RuleEngine.CardDef>(def);

        C(CardProgress.Level(Id), 3,
          "B1 ★ 加一张之后**仍是 3 级**（原版这一跳只写运行时 `TierCollection`，对玩家可见等级零影响）"
        + "｜🧨 `Shell/Inventory.cs:1392` 改回 `s = \"0\"` ⇒ tier 0 ⇒ `SetLevel(id, 1)` ⇒ **变 1** ⇒ 本条红");

        _amtAfter = Amt(Id);
    }

    // ============================================================ C · ★ 灭自证：换一个起始等级，档位照写
    static void SectionC_ExplicitTierStillWrites()
    {
        MenuCheck.Section(_sink, "C 灭自证：起始 **1 级**、`customData[\"CardTier\"] = \"2\"` 加一张 ⇒ 写到 **3 级**（不是停在 1）");

        var def = CollectionData.Card(Id);
        if (def == null) { Tr(false, "C0 **前提**：卡池认得 `" + Id + "`"); return; }
        if (Inventories.Cards == null) { Tr(false, "C0b **前提**：`Inventories.Cards` 在"); return; }

        CardProgress.SetLevel(Id, 1);              // 🔴 与 B 组的起始等级（3）**必须不同** —— 见文件头「为什么必须换起始等级」
        C(CardProgress.Level(Id), 1, "C0 **前提**：起始等级摆到 **1 级**（与 B 组的 3 级不同，这样「删掉整条调用」才会被本条抓到）");

        int before = Amt(Id);
        var cd = new Dictionary<string, string> { { "CardTier", "2" } };
        Inventories.Cards.Add(def, 1, null, cd, false);

        C(CardProgress.Level(Id), 3,
          "C1 ★ 带 `CardTier=2` ⇒ 写到 **3 级**（1 → 3）—— 服务端真给了档位时照写这条门还在"
        + "｜🧨 删掉 `CardInventory.Add` 里那句 `UpdateCardTier(item, customData)`（或删掉 `SetLevel`）⇒ 停在 1 ⇒ 本条红");
        C(Amt(Id), before + 1,
          "C2 ★ 而且这条路口**数量照样 +1**（" + before + " → " + Amt(Id) + "）｜🧨 为了绕开等级把 `Add` 整条短路 ⇒ 红");
    }

    // ============================================================ D · ★ 数量守恒：不许为了「不改等级」把 Add 短路
    static void SectionD_AmountConserved()
    {
        MenuCheck.Section(_sink, "D 数量守恒：B 组那一次 `Add` **恰好**让卡背包 +1");

        var info = Inventories.Cards == null ? null : Inventories.Cards.Get(Id);
        Tr(info != null, "D0 **前提**：卡背包里**有** `" + Id + "` 这一条（不是「查不到 ⇒ 数量 0 也算过」）");
        C(_amtAfter, _amtBefore + 1,
          "D1 ★ `customData = null` 那一次 `Add` 之后数量**恰好 +1**（" + _amtBefore + " → " + _amtAfter + "）"
        + "｜🧨 为了让等级不变而把 `Add` 提前 return（不建条目 / 不加数量）⇒ 红");
    }

    // ============================================================ E · ★ `SetLevel` 先读档：盘上的等级/拥有数/通配符不许被清掉
    static void SectionE_SetLevelLoadsBeforeWrite()
    {
        MenuCheck.Section(_sink, "E 先读档：`CardProgress.SetLevel` 在「还没人摸过 `Level()`/`Owned()`」时也不许清空盘上的等级/拥有数/通配符");

        const BindingFlags F = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        var t = typeof(CardProgress);
        FieldInfo fLoaded = t.GetField("_loaded", F);
        FieldInfo fPersist = t.GetField("_persist", F);
        FieldInfo fLvl = t.GetField("_lvl", F);
        FieldInfo fOwned = t.GetField("_owned", F);
        FieldInfo fWild = t.GetField("_wild", F);
        bool ok = fLoaded != null && fPersist != null && fLvl != null && fOwned != null && fWild != null;
        Tr(ok, "E0 **前提**：反射拿得到 `CardProgress` 的五个私有静态（`_loaded`/`_persist`/`_lvl`/`_owned`/`_wild`）—— 拿不到说明实现被大改，回来重核这条断言");
        if (!ok) return;

        var lvl = fLvl.GetValue(null) as Dictionary<string, int>;
        var owned = fOwned.GetValue(null) as Dictionary<string, int>;
        var wild = fWild.GetValue(null) as Dictionary<string, int>;
        ok = lvl != null && owned != null && wild != null;
        Tr(ok, "E0b **前提**：三张字典的真身拿得到（`_lvl`/`_owned`/`_wild`）");
        if (!ok) return;

        string keepOverride = CardProgress.OverridePath;
        bool keepLoaded = (bool)fLoaded.GetValue(null);
        bool keepPersist = (bool)fPersist.GetValue(null);
        var lvlSnap = new Dictionary<string, int>(lvl);
        var ownedSnap = new Dictionary<string, int>(owned);
        var wildSnap = new Dictionary<string, int>(wild);

        string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wf_b110_f1_cardprogress.json");
        try
        {
            // 盘上摆一份「玩家真档」：等级 UM2=3 / 拥有数 B110F1_OWNED=7 / 通配符 epic=42
            // ⚠️ 拥有数用 `B110F1_OWNED`（**故意的**）：`Owned()` 先查卡背包、查不到才看 `_owned`
            //    —— 用 `UM2` 会被 A–D 组加进背包的数量盖住，那就测不到「整份清空」了。
            System.IO.File.WriteAllText(tmp,
                "{\"version\":1,\"levels\":[{\"id\":\"" + Id + "\",\"v\":3}]," +
                "\"owned\":[{\"id\":\"B110F1_OWNED\",\"v\":7}]," +
                "\"wildcards\":[{\"id\":\"epic\",\"v\":42}]}");

            CardProgress.OverridePath = tmp;
            lvl.Clear(); owned.Clear(); wild.Clear();      // 模拟「刚起进程、还没人摸过」
            fLoaded.SetValue(null, false);                 // 下一次 Level()/Owned()/SetLevel() 必须读盘
            fPersist.SetValue(null, true);                 // 允许落盘（写的是上面那个临时文件）

            CardProgress.SetLevel("UM3", 2);               // ⚠️ 之前**没有任何** `Level()` / `Owned()` 调用

            C(CardProgress.Level("UM3"), 2,
              "E1 **前提**：这次 `SetLevel(\"UM3\", 2)` 真的写进去了（不然「什么都没做」也会让 E2–E4 变绿）");

            C(CardProgress.Level(Id), 3,
              "E2 ★ 盘上 `" + Id + "` 的 **3 级没被清掉**（`SetLevel` 先 `EnsureLoaded()` 再写）"
            + "｜🧨 去掉 `Shell/CardDetailPopup.cs:302` 的 `EnsureLoaded()` ⇒ 内存表从空起步 ⇒ `Save()` 把整份写成只剩 UM3 ⇒ 读到缺省 1 ⇒ 红");

            C(CardProgress.Owned("B110F1_OWNED", "common"), 7,
              "E3 ★ 盘上的**拥有数 7 也没被清掉**（同一次 `Save()` 会连 `_owned` 一起写回）"
            + "｜🧨 同上 ⇒ 读到「给足」口径的缺省值（≥10）⇒ 红");

            C(CardProgress.Wildcards("epic"), 42,
              "E4 ★ 盘上的**通配符 42 也没被清掉**"
            + "｜🧨 同上 ⇒ 读到缺省 99 ⇒ 红");

            string written = System.IO.File.Exists(tmp) ? System.IO.File.ReadAllText(tmp) : "";
            Tr(written.Contains("\"" + Id + "\"") && written.Contains("\"UM3\""),
               "E5 ★ 落盘那一刻两份都在（文件里同时有 `" + Id + "` 与 `UM3`）—— 这是 E2–E4 的落盘面证据"
             + "｜🧨 不先读档 ⇒ 文件里只剩 `UM3` ⇒ 红");
        }
        finally
        {
            CardProgress.OverridePath = keepOverride;
            lvl.Clear(); foreach (var kv in lvlSnap) lvl[kv.Key] = kv.Value;
            owned.Clear(); foreach (var kv in ownedSnap) owned[kv.Key] = kv.Value;
            wild.Clear(); foreach (var kv in wildSnap) wild[kv.Key] = kv.Value;
            fLoaded.SetValue(null, keepLoaded);
            fPersist.SetValue(null, keepPersist);
            try { if (System.IO.File.Exists(tmp)) System.IO.File.Delete(tmp); }
            catch (System.Exception e) { Debug.LogWarning("[B110F1] 临时档删不掉（不影响断言）：" + tmp + " · " + e.Message); }
        }
    }
}
