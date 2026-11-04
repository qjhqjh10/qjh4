// B142_E65_Assert.cs — 写手 `B142-E65`（2026-11-04）：货币名册播种 + 「`GetOwnedCount` 与顶栏读同一账」
//
// ============================ 它盯的是什么 ============================
//  判据 = `资料/完全复刻/判据/B142_R18_CHK05块6.md` §二·1（🟠 商店#L55，连带 #L62）
//        + `资料/完全复刻/报告/B142_E54.md` §六·3（货币出现「两处读数」）
//        + `decomp_full/CurrencyExtensions__{ToCurrency,GetOwnedCount,AddCurrency}.c`
//        + 三份 Currency SO 的 `code`/`type` 字段实读（`Crystals` C2/10 · `Blackstone` C3/20 · `Gacha tickets` C5/70）。
//  实现落点：`Shell/CurrencyCatalogSeed_B142E65.cs`（播种表 + `Wallet` 映射）+ `Shell/Inventory.cs`
//            `CurrencyCatalog._all` 字段初始化器（同一物理行改写）+ `GetOwnedCount` / `AddCurrency` 两个分支（**行数零变化**）。
//
// ============================ 组 ============================
//  A 播种：名册里**真有** C2/C3/C5（改前一个都没有 ⇒ `Find` 恒 null）；只播这三份（`gold` 必须仍查不到）。
//  B 同一账：`GetOwnedCount(crystals) == MainMenuRuntime.OwnedOf(10) == Wallet.Of(水晶键)`；
//            把 `Wallet` 拨到 1777 ⇒ 两个读口**同时**是 1777（灭自证：任一侧写死 0/2000 ⇒ 红）。
//  C 卖卡阈值：`ShouldSellCardsFromCrate()` 跟着真账走 —— 2000 ⇒ false · 1601 ⇒ false（严格小于）· 1600 ⇒ true。
//            **改前它恒真**（`GetOwnedCount` 恒 0）。
//  D 分币种隔离：动水晶**不动**黑石/票据；C3/C5 与顶栏一致（今天 `CounterSpecs` 没给它们 `WalletKey` ⇒ 两边都 0）。
//  E 写口同账：`AddCurrency(ToCurrency(crystals), 456)` ⇒ `Wallet` 与 `GetOwnedCount` **同步 +456**；
//             `AddCurrency(黑石, 5)`（没有记账路）⇒ 三个读口**全不动**（⛔ 不是静默写进水晶那一格）。
//  F 回归：`campaignPoints` / `energy` 两条**注入钩子**照旧优先（我的改动没把它们挤掉）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · 摘掉 `_all = CurrencyCatalogSeed.LocalDefaults()` ⇒ A 组红（`Find` 返回 null），B/C/E 连带红。
//  · 把 `GetOwnedCount` 的兜底写死 0（改前就是这样）/ 写死 2000 ⇒ B1/B2 红（拨到 1777 就露）。
//  · `ShouldSellCardsFromCrate` 恒真（改前）⇒ C1/C2 红；恒假 ⇒ C3 红。
//  · 三种货币共用一个 `Wallet` 键 ⇒ D 组红。
//  · 保留改前的 `AddCurrency`（无背包 ⇒ 只出声、不加）⇒ E1 红。
//
// ============================ ⚠️ 顺序与副作用（接宿主时必读）============================
//  · 本件**会写 `Wallet`**（内存字典，不落盘）⇒ 进 `try/finally`，`RestoreWallet()` 逐键还原（原本不存在的键会被**删掉**）。
//  · 本件开头 `MainMenuRuntime.ClearCounterForTest()` 清两个自检拨盘（`_ownedForTest/_maxForTest`）——
//    否则残余拨盘会让 `OwnedOf` 绕过 `Wallet`，B 组会假红。
//  · **前提**：进程里**没有**登记 `Inventory<ICurrency>`（登记了就走原版背包支，与 `Wallet` 无关）。有 ⇒ B–E 如实跳过并计一条前提失败。
//  · ⚠️ **未接宿主**（本次白名单不含 `Editor/*Scene.cs`）。接法见报告 `报告/B142_E65.md` §5。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>写手 `B142-E65`：本地三份货币 SO 播种 + `CurrencyExtensions.GetOwnedCount` 与顶栏**读同一账（`Wallet`）**。</summary>
public static class B142_E65_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B142E65] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具：Wallet 快照 / 还原
    static readonly Dictionary<string, int> _savedValue = new Dictionary<string, int>();
    static readonly HashSet<string> _savedHad = new HashSet<string>();

    static void SetWallet(string key, int value)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!_savedHad.Contains(key) && !_savedValue.ContainsKey(key))
        {
            int cur;
            if (Wallet.Gold.TryGetValue(key, out cur)) _savedValue[key] = cur;
            else _savedHad.Add(key);                        // 记「原本没有这一格」⇒ 还原时删掉
        }
        Wallet.Gold[key] = value;
    }

    static void RestoreWallet()
    {
        foreach (var kv in _savedValue) Wallet.Gold[kv.Key] = kv.Value;
        foreach (var k in _savedHad) Wallet.Gold.Remove(k);
        _savedValue.Clear();
        _savedHad.Clear();
    }

    /// <summary>`CounterSpecs` 里这一种币的 `WalletKey` —— **独立取一次**（⛔ 不走被测的 `CurrencyCatalogSeed.WalletKeyOf`）。</summary>
    static string WalletKeyOf(int currency)
    {
        var all = MainMenuRuntime.CounterSpecs;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].Currency == currency) return all[i].WalletKey;
        if (currency == MainMenuRuntime.CurEnergy) return MainMenuRuntime.EnergySpec.WalletKey;
        return null;
    }

    /// <summary>`Wallet` 里这一种币的读数（键为 null ⇒ 0，与 `MainMenuRuntime.OwnedOf` 同一口径）。</summary>
    static int WalletOf(int currency)
    {
        string k = WalletKeyOf(currency);
        return k == null ? 0 : Wallet.Of(k);
    }

    /// <summary>自包含：只要一个空场景就能跑（但会写 `Wallet` / 清拨盘，见文件头）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B142-E65 · 货币名册播种 + `GetOwnedCount` 与顶栏读同一账（判据 `判据/B142_R18_CHK05块6.md` §二·1）");

        MainMenuRuntime.ClearCounterForTest();     // 见文件头「顺序与副作用」
        try
        {
            SectionA_Seeded();
            SectionB_SameLedger();
            SectionC_SellThreshold();
            SectionD_PerCurrency();
            SectionE_WritePath();
            SectionF_HookRegression();
        }
        finally { RestoreWallet(); }

        Debug.Log(_sink.P + "B142-E65 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 播种（本地三份 SO）
    static void SectionA_Seeded()
    {
        MenuCheck.Section(_sink, "A 播种：名册里真有 C2/C3/C5（`code`/`type` = 三份 SO 字段实读），且**只播这三份**");

        CurrencyCatalogSeed.EnsureSeeded();        // 幂等：生产靠字段初始化器，这里只是防「同一进程里被 ClearForTest 清过」

        string[] codes = { "C2", "C3", "C5" };
        GameCurrency[] types = { GameCurrency.crystals, GameCurrency.blackStones, GameCurrency.gachaTickets };

        for (int i = 0; i < types.Length; i++)
        {
            ICurrency c = CurrencyCatalog.Find(types[i]);
            Tr(c != null,
               "A" + (i + 1) + " ★ `CurrencyCatalog.Find(" + types[i] + ")` **不为 null**（改前名册恒空 ⇒ 本条红）"
             + "｜🧨 去掉 `_all` 字段初始化器里的 `LocalDefaults()` ⇒ 红");
            if (c == null) continue;
            C(c.GetID(), codes[i],
              "A" + (i + 1) + "b ★ `Find(" + types[i] + ").GetID()` == `" + codes[i] + "`（= 该 SO 的 `code` 字段实读）"
            + "｜🧨 code/type 配错（例如把 `blackStones` 配成 C2）⇒ 红");
            Tr(c.MaxAmount < 0,
               "A" + (i + 1) + "c `MaxAmount` = -1（三份 SO **没有** `maxAmount` 字段 ⇒ 原版默认的「不封顶」；现读 " + c.MaxAmount + "）");
        }

        // 只播这三份（灭自证：有人把整个 `GameCurrency` 枚举都播一遍 ⇒ 本条红）
        Tr(CurrencyCatalog.Find(GameCurrency.gold) == null,
           "A4 ★ `Find(gold)` 仍为 **null** —— 本地只有那三份货币 SO，⛔ 别按枚举补假货币"
         + "｜🧨 把 `GameCurrency` 全枚举播一遍 ⇒ 红");
        Tr(CurrencyCatalog.All.Count >= 3,
           "A5 名册条目数 ≥ 3（现读 " + CurrencyCatalog.All.Count + "）");
    }

    // ============================================================ B · 同一账（`Wallet`）
    static void SectionB_SameLedger()
    {
        MenuCheck.Section(_sink, "B 同一账：`GetOwnedCount(crystals)` == `MainMenuRuntime.OwnedOf(10)` == `Wallet.Of(水晶键)`");

        Tr(InventoryManager.GetInventory<ICurrency>() == null,
           "B0 **前提**：进程里没有登记 `Inventory<ICurrency>`（登记了 `GetOwnedCount` 就走原版背包支，与 `Wallet` 无关；生产本来就没登记）"
         + "｜🧨 在别处登记了货币背包 ⇒ 本组与 C/E 组的前提不成立");
        if (InventoryManager.GetInventory<ICurrency>() != null) return;

        string key = WalletKeyOf(MainMenuRuntime.CurCrystals);
        Tr(!string.IsNullOrEmpty(key),
           "B1 **前提**：`CounterSpecs` 给 `CurCrystals` 配了 `WalletKey`（现读 = `" + key + "`）");
        if (string.IsNullOrEmpty(key)) return;

        SetWallet(key, 1777);                      // 挑一个既不是 StartAmount(2000) 也不是 0 的数 ⇒ 写死值必露
        C(CurrencyExtensions.GetOwnedCount(GameCurrency.crystals), 1777,
           "B2 ★ `GetOwnedCount(crystals)` == **1777**（`Wallet` 拨到 1777 之后）"
         + "｜🧨 写死 0（改前）或写死 `StartAmount` ⇒ 红");
        C(MainMenuRuntime.OwnedOf(MainMenuRuntime.CurCrystals), 1777,
           "B3 ★ 顶栏那个读口（`MainMenuRuntime.OwnedOf`）**同一个数** ⇒ 两处读数已收成一处");
        C(Wallet.Of(key), 1777,
           "B4 ★ `Wallet.Of(水晶键)` 也是 1777（`GetOwnedCount` 落的就是这一格）");

        SetWallet(key, 2000);
        C(CurrencyExtensions.GetOwnedCount(GameCurrency.crystals), 2000,
           "B5 ★ 拨回 2000（= 用户定的「资源固定 2000」）⇒ 读口跟着回 2000");
    }

    // ============================================================ C · 卖卡阈值（`ShouldSellCardsFromCrate`）
    static void SectionC_SellThreshold()
    {
        MenuCheck.Section(_sink, "C 卖卡阈值：`ShouldSellCardsFromCrate()` = `GetOwnedCount(crystals) < 1601` **跟着真账走**");

        string key = WalletKeyOf(MainMenuRuntime.CurCrystals);
        if (string.IsNullOrEmpty(key)) { Tr(false, "C0 **前提**：`CurCrystals` 有 `WalletKey`（没有 ⇒ 本组没法摆数）"); return; }
        if (InventoryManager.GetInventory<ICurrency>() != null) { Tr(false, "C0 **前提**：没有登记 `Inventory<ICurrency>`（理由同 B0）"); return; }

        C(CardSell.CrystalThreshold, 1601,
           "C0b 阈值常量 = **1601**（`decomp_full/PlayerDataManager__ShouldSellCardsFromCrate.c` 的 `0x641`）");

        SetWallet(key, 2000);
        Tr(!CardSell.ShouldSellCardsFromCrate(),
           "C1 ★ 2000 ⇒ **false**（2000 不 < 1601）｜🧨 改前 `GetOwnedCount` 恒 0 ⇒ 这里恒 **true**，本条红");

        SetWallet(key, 1601);
        Tr(!CardSell.ShouldSellCardsFromCrate(),
           "C2 ★ **1601** ⇒ false（原版 `<` 是严格小于）｜🧨 改成 `<=` ⇒ 本条红");

        SetWallet(key, 1600);
        Tr(CardSell.ShouldSellCardsFromCrate(),
           "C3 ★ **1600** ⇒ true（边界下方一个数）｜🧨 写死 false / 不读 `Wallet` ⇒ 红");

        SetWallet(key, 2000);                      // 复位（避免影响后面 E 组与宿主后续断言）
    }

    // ============================================================ D · 分币种隔离
    static void SectionD_PerCurrency()
    {
        MenuCheck.Section(_sink, "D 分币种：C3/C5 与顶栏一致；动水晶**不动**它们");

        C(CurrencyExtensions.GetOwnedCount(GameCurrency.blackStones), MainMenuRuntime.OwnedOf(MainMenuRuntime.CurBlackStones),
           "D1 ★ `GetOwnedCount(blackStones)` == 顶栏同种读数（现读 " + CurrencyExtensions.GetOwnedCount(GameCurrency.blackStones) + "）"
         + "｜🧨 三种币共用一个 `Wallet` 键 ⇒ 红");
        C(CurrencyExtensions.GetOwnedCount(GameCurrency.gachaTickets), MainMenuRuntime.OwnedOf(MainMenuRuntime.CurGachaTickets),
           "D2 ★ `GetOwnedCount(gachaTickets)` == 顶栏同种读数（现读 " + CurrencyExtensions.GetOwnedCount(GameCurrency.gachaTickets) + "）");

        int stone0 = CurrencyExtensions.GetOwnedCount(GameCurrency.blackStones);
        int ticket0 = CurrencyExtensions.GetOwnedCount(GameCurrency.gachaTickets);
        string ck = WalletKeyOf(MainMenuRuntime.CurCrystals);
        SetWallet(ck, 900);
        C(CurrencyExtensions.GetOwnedCount(GameCurrency.blackStones), stone0,
           "D3 ★ 水晶拨到 900 ⇒ **黑石读数不动**（" + stone0 + "）｜🧨 映射落到同一格 ⇒ 红");
        C(CurrencyExtensions.GetOwnedCount(GameCurrency.gachaTickets), ticket0,
           "D4 ★ 同上：**票据读数不动**（" + ticket0 + "）");
        SetWallet(ck, 2000);
    }

    // ============================================================ E · 写口同账
    static void SectionE_WritePath()
    {
        MenuCheck.Section(_sink, "E 写口：`AddCurrency` 与 `GetOwnedCount` 落**同一格**；没有记账路的币种**不加**（⛔ 不静默）");

        if (InventoryManager.GetInventory<ICurrency>() != null) { Tr(false, "E0 **前提**：没有登记 `Inventory<ICurrency>`（理由同 B0）"); return; }

        ICurrency crystals = CurrencyCatalog.Find(GameCurrency.crystals);
        Tr(crystals != null, "E0b **前提**：`ToCurrency(crystals)` 查得到（= A 组播种成功）");
        if (crystals == null) return;

        string key = WalletKeyOf(MainMenuRuntime.CurCrystals);
        SetWallet(key, 2000);
        int w0 = Wallet.Of(key);
        int o0 = CurrencyExtensions.GetOwnedCount(GameCurrency.crystals);
        CurrencyExtensions.AddCurrency(crystals, 456);
        C(Wallet.Of(key), w0 + 456,
           "E1 ★ `AddCurrency(水晶, 456)` ⇒ `Wallet` **+456**（" + w0 + " → " + Wallet.Of(key) + "）"
         + "｜🧨 保留改前的「没有背包 ⇒ 只出声、不加」⇒ 红");
        C(CurrencyExtensions.GetOwnedCount(GameCurrency.crystals), o0 + 456,
           "E2 ★ 读口同步 **+456**（写与读同一格，⛔ 不是两处记账）");

        // 没有记账路的币种：三个读口都不许动（⛔ 不许「顺手记进水晶」）
        ICurrency black = CurrencyCatalog.Find(GameCurrency.blackStones);
        string blackKey = WalletKeyOf(MainMenuRuntime.CurBlackStones);
        Tr(blackKey == null,
           "E3b **前提**：`CurBlackStones` 今天**没有** `WalletKey`（= 我们没有这一路记账；哪天配上了，E3/E4 的期望值要跟着改）");
        if (blackKey == null)
        {
            int c0 = Wallet.Of(key), stone0 = CurrencyExtensions.GetOwnedCount(GameCurrency.blackStones);
            if (black != null) CurrencyExtensions.AddCurrency(black, 5);   // 会让 `cur-add:nokey` 出一次声（预期）
            C(Wallet.Of(key), c0,
               "E3 ★ 加黑石 **不动水晶那一格**（" + c0 + "）｜🧨 映射写错种 ⇒ 红");
            C(CurrencyExtensions.GetOwnedCount(GameCurrency.blackStones), stone0,
               "E4 ★ 黑石读数也不动（它没有 `WalletKey` ⇒ 加不上；⛔ 不是静默假装加上）");
        }

        SetWallet(key, 2000);
    }

    // ============================================================ F · 两条注入钩子回归
    static void SectionF_HookRegression()
    {
        MenuCheck.Section(_sink, "F 回归：`campaignPoints` / `energy` 两条注入钩子照旧优先（我的兜底排在它们之后）");

        var savedCp = CurrencyExtensions.CampaignPointsOf;
        var savedEn = CurrencyExtensions.EnergyOf;
        try
        {
            CurrencyExtensions.CampaignPointsOf = () => 4242;
            CurrencyExtensions.EnergyOf = () => 77;
            C(CurrencyExtensions.GetOwnedCount(GameCurrency.campaignPoints), 4242,
               "F1 ★ `campaignPoints` 仍走钩子（现读 4242）｜🧨 把兜底提到钩子之前 ⇒ 红");
            C(CurrencyExtensions.GetOwnedCount(GameCurrency.energy), 77,
               "F2 ★ `energy` 仍走钩子（现读 77）｜🧨 同上 ⇒ 红");
        }
        finally
        {
            CurrencyExtensions.CampaignPointsOf = savedCp;
            CurrencyExtensions.EnergyOf = savedEn;
        }
    }
}
