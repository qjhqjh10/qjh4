// B142_E73_Assert.cs — 执行写手 `B142-E73`（2026-11-04 · 第一百二十九会话 · 批次 `B142`）
//
// ============================ 它盯的是什么（4 条任务）============================
//  A `B142-WALLET-PERSIST`：`Wallet`（`Shell/DailyData.cs` 类尾）接进 `SaveCenter` 的 `_ours_Wallet` ——
//    领到的水晶**重启后还在**（改前：纯内存，重启回 `StartAmount`=2000）。
//  B `B142-CARDBACK-ID`：`CardbackTable.FindByUniqueId` 认得出战役奖励里那 2 条**卡背 id**
//    （32 位 hex）；`RewardService.RewardKindOf` 把她们归到 `Cosmetic`（改前落 `rs-item:unresolved`）。
//  C `B142-CURRENCY-CONDITION`：`CurrencyCondition.Validate` 与
//    `CurrencyExtensions.GetOwnedCount` **同一个读口**（`Wallet`）—— 改前 `HasEnough<Currency>` 恒 0 ⇒ 条件恒 false。
//  D `B142-LSTHANDLE`：`SettingsWindow.LstHandleH` 从手抄 `533.69f` 改成算式
//    `(DdPanelHFull − 20) × 0.9273074865341187 + 20`（与 `Battle/SettingsPanel.cs:1790` 同一口径）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A：摘掉 `Wallet.Grant` 里那句 `Save()` / `Of` 里那句 `EnsureLoaded()` ⇒ A5（文件不存在）/ A6（重启回 2000）红。
//    把落盘门 `SaveCenter.StoreOn` 的判据删掉 ⇒ A9b 红（批处理会去写玩家真档）。
//  · B：把 `IsCosmeticId` 里那句 `CardbackTable.FindByUniqueId` 删掉 ⇒ B1d/B3/B4 红（`RewardKindOf` 落 `Unknown`）。
//    把 `FindByUniqueId` 改成「按图名查」⇒ B2b（拿图名当 id 必须**查不到**）红。
//  · C：把 `Validate` 改回 `InventoryManager.HasEnough<Currency>(...)` ⇒ C1/C3 红（恒 false）。
//    把比较方向写反 / 把 `minimumAmount` 当 `>` ⇒ C2 红。
//  · D：把常量改回 `533.69f` ⇒ D2（算式逐位相等）仍绿、但 D3/D4 的「漏项」对照与 D1 的数值仍绿 ——
//    **这一组只钉「值 + 算式来源」**，⚙️ 手抄 vs 算式在**编译产物**里无法区分 ⇒ 见报告 §4 的如实说明。
//
// ============================ ⚠️ 顺序与副作用（接宿主时必读）============================
//  · A 段会**写临时目录**（`SaveCenter.OverrideRoot` 指到 `%TEMP%/wf_e73_*`），跑完删除、并把
//    `OverrideRoot` / `SaveCenter` 的进程内状态还原；`Wallet.Gold` 在 `finally` 里逐键还原。
//  · A 段会 `Wallet.ResetForTest()`（清内存 + 复位懒加载闩）—— 这是「模拟重启」的唯一手段。
//  · **前提**：进程里**没有**登记 `Inventory<Currency>`（登记了 `C` 组走原版支，本组如实跳过并记一条前提失败）。
//  · ⚠️ **未接宿主**（本代理白名单不含 `Editor/*Scene.cs`）。接法见报告 `报告/B142_E73.md` §5。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B142-E73`：Wallet 落盘 / 卡背 id 认领 / 货币条件与 `GetOwnedCount` 同账 / `LstHandleH` 算式。</summary>
public static class B142_E73_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B142E73] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Nr(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>`CounterSpecs` 里这一种币的 `WalletKey` —— **独立取一次**（⛔ 不走被测的 `CurrencyCatalogSeed.WalletKeyOf`）。</summary>
    static string WalletKeyOf(int currency)
    {
        var all = MainMenuRuntime.CounterSpecs;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].Currency == currency) return all[i].WalletKey;
        if (currency == MainMenuRuntime.CurEnergy) return MainMenuRuntime.EnergySpec.WalletKey;
        return null;
    }

    /// <summary>自包含：只要一个空场景就能跑（A 段会写临时目录 / 清拨盘，见文件头）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B142-E73 · Wallet 落盘 · 卡背 id · 货币条件同账 · `LstHandleH` 算式");

        MainMenuRuntime.ClearCounterForTest();     // 残余拨盘会让 `OwnedOf` 绕过 `Wallet`（C 组假红）

        try { SectionA_WalletPersist(); }
        catch (Exception e) { Tr(false, "「A Wallet 落盘」抛异常：" + e.GetType().Name + "：" + e.Message); }

        try { SectionB_CardbackId(); }
        catch (Exception e) { Tr(false, "「B 卡背 id」抛异常：" + e.GetType().Name + "：" + e.Message); }

        try { SectionC_CurrencyCondition(); }
        catch (Exception e) { Tr(false, "「C 货币条件」抛异常：" + e.GetType().Name + "：" + e.Message); }

        try { SectionD_LstHandleH(); }
        catch (Exception e) { Tr(false, "「D LstHandleH」抛异常：" + e.GetType().Name + "：" + e.Message); }

        Debug.Log(_sink.P + "B142-E73 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · Wallet 落盘
    static void SectionA_WalletPersist()
    {
        MenuCheck.Section(_sink, "A Wallet 落盘：领到的水晶**重启后还在**（`_ours_Wallet` · 判据 = 简报 `B142-WALLET-PERSIST`）");

        var savedGold = new Dictionary<string, int>(Wallet.Gold);
        string oldRoot = SaveCenter.OverrideRoot;
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wf_e73_" + Guid.NewGuid().ToString("N"));
        string key = WalletKeyOf(MainMenuRuntime.CurCrystals);
        try
        {
            System.IO.Directory.CreateDirectory(dir);
            SaveCenter.OverrideRoot = dir;
            SaveCenter.ResetForTest();
            Wallet.ResetForTest();

            Tr(SaveCenter.StoreOn, "A0 **前提**：设了 `SaveCenter.OverrideRoot` ⇒ 落盘门开着（`StoreOn == true`）");
            Tr(!string.IsNullOrEmpty(key), "A0b **前提**：水晶有 `WalletKey`（现读 = `" + key + "`）");
            Tr(SaveKeys.IsKnown(Wallet.SaveKey), "A1 键表放行 `" + Wallet.SaveKey + "`（`_ours_` 前缀，⛔ 不用改 `SaveKeys.All`）");
            Tr(!SaveCenter.Exists(Wallet.SaveKey), "A2 **前提**：临时目录里还没有这份档");
            C(Wallet.Of(key), Wallet.StartAmount, "A3 旧档无键 ⇒ `Of` 用 `StartAmount` 初始化（" + Wallet.StartAmount + "）");

            Wallet.Grant(key, 137);
            C(Wallet.Of(key), Wallet.StartAmount + 137, "A4 领 137 ⇒ " + (Wallet.StartAmount + 137));
            Tr(SaveCenter.Exists(Wallet.SaveKey), "A5 ★ 盘上真的出现了 `" + Wallet.SaveKey
               + ".json`（改前：纯内存、一个文件都没有）｜🧨 不接 `SaveCenter` ⇒ 红");

            Wallet.ResetForTest();                    // = 「重开一次进程」
            C(Wallet.Of(key), Wallet.StartAmount + 137,
               "A6 ★★ **重启后仍是 " + (Wallet.StartAmount + 137) + "**（改前会回到 " + Wallet.StartAmount + "）"
             + "｜🧨 不读盘 ⇒ 红");
            C(Wallet.Of("wf_e73_no_such_key"), Wallet.StartAmount, "A7 没有记账的格仍读 `StartAmount`（⛔ 不把别的格串进来）");

            Wallet.Grant(key, 1);                     // 灭自证：累加在**读回来的那一份**上
            Wallet.ResetForTest();
            C(Wallet.Of(key), Wallet.StartAmount + 138,
               "A8 ★ 重启后再领 1 ⇒ " + (Wallet.StartAmount + 138) + "（读盘没接上会退到 " + (Wallet.StartAmount + 1) + "）");

            SaveCenter.OverrideRoot = null;           // 落盘关着（= 批处理 / 没注入）
            Wallet.ResetForTest();
            Wallet.Grant("wf_e73_off_key", 5);
            C(Wallet.Of("wf_e73_off_key"), Wallet.StartAmount + 5,
               "A9 落盘关着 ⇒ 只在内存里（+5 读得到）");
            SaveCenter.OverrideRoot = dir;
            Wallet.ResetForTest();
            C(Wallet.Of("wf_e73_off_key"), Wallet.StartAmount,
               "A9b ★ 重启后那一格**不存在** ⇒ 证明批处理 / 无 `OverrideRoot` 时**没写盘**｜🧨 把落盘门删掉 ⇒ 红");
        }
        finally
        {
            Wallet.Gold.Clear();
            foreach (var kv in savedGold) Wallet.Gold[kv.Key] = kv.Value;
            SaveCenter.OverrideRoot = oldRoot;
            SaveCenter.ResetForTest();
            try { if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); } catch { /* 删不掉不算缺陷 */ }
        }
    }

    // ============================================================ B · 卡背 id
    /// <summary>独立读 `Resources/Cardbacks.json` 的 `uniqueId → 图名`（**不走被测的 `FindByUniqueId`**）。</summary>
    static Dictionary<string, string> ReadCardbackIds()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var ta = Resources.Load<TextAsset>(CardbackTable.ResourcePath);
        if (ta == null) return map;
        var f = JsonUtility.FromJson<CbFile>(ta.text);
        if (f == null || f.items == null) return map;
        for (int i = 0; i < f.items.Length; i++)
        {
            var it = f.items[i];
            if (it == null || string.IsNullOrEmpty(it.uniqueId)) continue;
            if (!map.ContainsKey(it.uniqueId)) map[it.uniqueId] = it.name;
        }
        return map;
    }

    [Serializable]
    class CbFile { public CardbackTable.Item[] items; }

    static bool IsHex32(string s)
    {
        if (s == null || s.Length != 32) return false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!hex) return false;
        }
        return true;
    }

    static void SectionB_CardbackId()
    {
        MenuCheck.Section(_sink, "B 卡背 id：`CardbackTable.FindByUniqueId` + `RewardService.RewardKindOf` 认得出战役奖励里那 2 条（判据 = `Resources/Cardbacks.json` 的 `uniqueId`）");

        var map = ReadCardbackIds();
        Tr(map.Count >= 200, "B0 **前提**：独立读 `Resources/Cardbacks.json` 数出 " + map.Count
           + " 条 `uniqueId`（现读 233；表变了回来重核，⛔ 别删这条）");
        if (map.Count == 0) return;

        // 从**真数据**里取那几条卡背奖励（⛔ 不写死 hex —— 数据变了这条会自己跟着走）
        var cbIds = new List<string>();
        for (int i = 0; i < CampaignData.NodeCount; i++)
        {
            var arr = CampaignData.RewardsOf(i);
            for (int k = 0; k < arr.Length; k++)
            {
                string id = arr[k].Id;
                if (!IsHex32(id) || !map.ContainsKey(id) || cbIds.Contains(id)) continue;
                cbIds.Add(id);
            }
        }
        Tr(cbIds.Count >= 2, "B0b **前提**：战役奖励里数出 " + cbIds.Count
           + " 条卡背 id（现读 2 —— `CampaignData.cs:439` 那条节点的两个 hex；数据变了回来重核）");
        if (cbIds.Count == 0) return;

        for (int i = 0; i < cbIds.Count; i++)
        {
            string id = cbIds[i];
            string want = map[id];
            var it = CardbackTable.FindByUniqueId(id);
            Tr(it != null, "B" + (i + 1) + "a ★ `FindByUniqueId(\"" + id + "\")` 命中｜🧨 没有 id 查询口 ⇒ 红（改前就是这样）");
            C(it != null ? it.name : null, want, "B" + (i + 1) + "b ★ 图名 = 独立读到的 `" + want + "`");
            C(CardbackTable.NameOfUniqueId(id), want, "B" + (i + 1) + "c `NameOfUniqueId` 同值");
            C(RewardService.RewardKindOf(id), RewardService.RewardKind.Cosmetic,
               "B" + (i + 1) + "d ★ `RewardKindOf` 归到 `Cosmetic`（⛔ 不再落 `Unknown`/`rs-item:unresolved`）");
        }

        // 灭自证：uniqueId 查询口**只认 id**，不认图名 / 空前缀
        string firstName = null;
        foreach (var kv in map) { firstName = kv.Value; break; }
        Tr(!string.IsNullOrEmpty(firstName) && CardbackTable.FindByUniqueId(firstName) == null,
           "B2b ★ **图名**当 id 传进去 ⇒ **查不到**（证明这是 id 口、不是 `Find(图名)` 的别名）");
        Tr(CardbackTable.FindByUniqueId("") == null, "B2c 空 id ⇒ null（⛔ 不拿第一张顶替）");

        // 净零：卡背奖励**认得出**、但本工程全解锁 ⇒ 钱包 / 卡背包一分不动
        string walletKey = WalletKeyOf(MainMenuRuntime.CurCrystals);
        var cards = Inventories.Cards;                        // 先摸一次：触发静态构造/读档，别把「懒加载」算成变化
        int w0 = string.IsNullOrEmpty(walletKey) ? 0 : Wallet.Of(walletKey);
        int bag0 = cards != null ? cards.Count : 0;
        RewardService.CollectItemReward(new RewardInfo(cbIds[0], 1));
        C(string.IsNullOrEmpty(walletKey) ? 0 : Wallet.Of(walletKey), w0,
           "B3 ★ 领一条卡背奖励 ⇒ 钱包**一分不动**（净零）");
        C(cards != null ? cards.Count : 0, bag0, "B4 ★ 也不进卡背包（它不是卡）");
    }

    // ============================================================ C · 货币条件与 GetOwnedCount 同账
    static void SectionC_CurrencyCondition()
    {
        MenuCheck.Section(_sink, "C 货币条件：`CurrencyCondition.Validate` 与 `CurrencyExtensions.GetOwnedCount` **同一个读口**（判据 = `报告/B142_E54.md` §6·3 + `报告/B142_E65.md`）");

        var inv = InventoryManager.GetInventory<Currency>();
        Tr(inv == null,
           "C0 **前提**：进程里**没有**登记 `Inventory<Currency>`（本工程从未登记；登记了就照原版 `HasEnough` 那一支）");
        if (inv != null) return;

        string crystalKey = WalletKeyOf(MainMenuRuntime.CurCrystals);
        Tr(!string.IsNullOrEmpty(crystalKey), "C0b **前提**：水晶有 `WalletKey`（现读 = `" + crystalKey + "`）");
        if (string.IsNullOrEmpty(crystalKey)) return;

        var saved = new Dictionary<string, int>();
        bool hadKey = Wallet.Gold.TryGetValue(crystalKey, out int oldVal);
        if (hadKey) saved[crystalKey] = oldVal;
        try
        {
            var cond = new CurrencyCondition();
            cond.targetCurrency = new LoadableRef<Currency>("C2");

            Wallet.Gold[crystalKey] = 1234;
            cond.minimumAmount = 1234;
            Tr(cond.Validate(null), "C1 ★ 水晶 1234 ≥ 1234 ⇒ **true**（改前 `HasEnough<Currency>` 恒 0 ⇒ 恒 false）"
              + "｜🧨 不改读口 ⇒ 红");
            cond.minimumAmount = 1235;
            Tr(!cond.Validate(null), "C2 ★ 1235 ⇒ **false**（方向没写反、不是恒 true）");

            // 逐币种：`Validate(owned)` 真 / `Validate(owned+1)` 假 —— owned 取自**另一个读口**
            var types = new[] { GameCurrency.crystals, GameCurrency.blackStones, GameCurrency.gachaTickets };
            for (int i = 0; i < types.Length; i++)
            {
                var cur = CurrencyCatalog.Find(types[i]);
                Tr(cur != null, "C" + (3 + i * 3) + "a **前提**：名册里查得到 `" + types[i] + "`（三份 SO 播种）");
                if (cur == null) continue;
                int owned = CurrencyExtensions.GetOwnedCount(types[i]);
                cond.targetCurrency = new LoadableRef<Currency>(cur.Code);
                cond.minimumAmount = owned;
                Tr(cond.Validate(null), "C" + (4 + i * 3) + " ★ `" + cur.Code + "`：`Validate(赚有数 " + owned
                   + ")` == true（与 `GetOwnedCount(" + types[i] + ")` 同值）");
                cond.minimumAmount = owned + 1;
                Tr(!cond.Validate(null), "C" + (5 + i * 3) + " ★ `" + cur.Code + "`：`Validate(拥有数 + 1)` == false"
                   + "（两边**同一条账**；任一侧写死 ⇒ 红）");
            }

            // 灭自证：把 Wallet 拨动 300 ⇒ 「刚好够」的阈值跟着动 300
            Wallet.Gold[crystalKey] = 1534;
            cond.targetCurrency = new LoadableRef<Currency>("C2");
            cond.minimumAmount = 1534;
            Tr(cond.Validate(null), "C12 ★ 钱包拨到 1534 ⇒ `Validate(1534)` true（阈值跟着账走）");
            cond.minimumAmount = 1535;
            Tr(!cond.Validate(null), "C13 ★ 1535 false ｜🧨 写死 `StartAmount` ⇒ 红");
            cond.minimumAmount = 1;
            Tr(!InventoryManager.HasEnough<Currency>(cond.targetCurrency, 1),
               "C14 ★ **灭自证**：旧读口 `HasEnough<Currency>` 仍恒 false（0 ≥ 1 不成立）"
             + "—— `Validate` 现在已经**不走**它 ⇒ C1/C12 才可能是 true");

            // 反例：空 id / 名册里没有的 code ⇒ false（⛔ 不许静默 true）
            cond.targetCurrency = new LoadableRef<Currency>("");
            cond.minimumAmount = 0;
            Tr(!cond.Validate(null), "C15 空 `targetId` + 数量 0 ⇒ **false**（⛔ 不许因 0 ≥ 0 就静默 true）");
            cond.targetCurrency = new LoadableRef<Currency>("ZZ_NOT_A_CODE");
            cond.minimumAmount = 0;
            Tr(!cond.Validate(null), "C16 名册里没有的 code ⇒ **false**（⛔ 不猜）");
            cond.targetCurrency = null;
            Tr(!cond.Validate(null), "C17 `targetCurrency` 为 null ⇒ **false**（原版同一档会 NRE，我们不崩）");
        }
        finally
        {
            if (hadKey) Wallet.Gold[crystalKey] = saved[crystalKey]; else Wallet.Gold.Remove(crystalKey);
        }
    }

    // ============================================================ D · LstHandleH
    static void SectionD_LstHandleH()
    {
        MenuCheck.Section(_sink, "D `SettingsWindow.LstHandleH`：手抄值 → 算式（与 `Battle/SettingsPanel.cs:1790` 同一口径）");

        float handle = SettingsWindow.LstHandleH;
        float slide = SettingsWindow.DdPanelHFull - 20f;
        Nr(handle, 533.69f, 0.02f, "D1 值仍是 **533.69**（原版 `Handle.sizeDelta.y`）");
        Tr(handle == slide * 0.9273074865341187f + 20f || Math.Abs(handle - (slide * 0.9273074865341187f + 20f)) <= 1e-3f,
           "D2 ★ 等于 `(DdPanelHFull − 20) × 0.9273074865341187 + 20`（滑区高 × `m_Size` + `sizeDelta.y`）"
         + "｜容差 1e-3 只为吸收「编译期常量 vs 运行期 float」的 1 ULP 差，算式差一项会差 1.4 / 20");
        Tr(handle != SettingsWindow.DdPanelHFull * 0.9273074865341187f,
           "D3 **灭自证**：不是「面板高 × `m_Size`」（漏掉滑区那 −20 ⇒ 会得 532.24）");
        Tr(handle != slide * 0.9273074865341187f,
           "D4 **灭自证**：不是「滑区高 × `m_Size`」（漏掉 `sizeDelta.y +20` ⇒ 会得 513.69）");
        Tr(handle > 0f && handle < slide, "D5 把手高在 (0, 滑区高) 之间（" + handle + " < " + slide + "）");
    }
}
