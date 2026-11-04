// EnergyContext.cs — 原版 `EnergyContext : SharedDataContext`（跨事件共享数据层里的「能量」这一块）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源，逐条现读 2026-11-05 · B143-W7）：
//   · 方法体：`d:/2/tools/decomp_full/EnergyContext__{GetCurrentEnergy,RefreshEnergy,GrantEnergy,
//     SpendEnergy,HasEnoughEnergy,UnpackData,Update}.c`（**七份全在**）
//   · 字段表：`cpp2il/out_cpp2il_dev_ilspy/EnergyContext.cs`（`energyData +0x20` · `energyConfig +0x28` ·
//     `static Action<int> m_OnEnergyUpdated`）
//   · 数据 DTO：`cpp2il/out_cpp2il_dev_ilspy/EnergyData.cs`（`{ int energy; long lastUpdated }`）
//   · 生产点（**原版确实活的**，不是死件）：
//       ① `Data/Scenes/Intro.unity` 里 GameObject 98 上挂着本组件（`m_Enabled: 1`，`m_Script` guid
//          `515abff5bccd3b61fd270362f5cca79f`）⇒ `SharedEventDataManager.Initialize()` 的
//          `GetComponentsInChildren<SharedDataContext>()` 会收到它；
//       ② `CurrencyExtensions.GetOwnedCount / AddCurrency / CanGetMore` 三处
//          `SharedEventDataManager.GetContext<EnergyContext>()`；
//       ③ `RewardService.CollectCurrencyReward` 的 energy 支。
//
// ============================ 行为（逐句照 `.c`，与那句「不许崩宿主」的差异）============================
//   `UnpackData(sharedEventData)`：
//     ① `sharedEventData.energyData == null` ⇒ **造一个** `EnergyData { energy = 配置的最大值 ?? 100,
//        lastUpdated = 现在 }` 并**写回** `sharedEventData.energyData`（`.c` 里那句 `*(param_2 + 0x10) = lVar2`）；
//     ② `this.energyData = sharedEventData.energyData`；
//     ③ `this.energyConfig = ConfigManager.GetConfig<EnergyConfig>()`（`.c` 的 `ConfigManager__GetConfig`）。
//   `GetCurrentEnergy()`   = `RefreshEnergy(); return energyData?.energy ?? 0;`
//   `HasEnoughEnergy(n)`   = `n <= energyData.energy`（原版空引用是 NRE）
//   `GrantEnergy(n)`       = `n == 0 ⇒ return; energy = max(0, energy + n); 广播`
//   `SpendEnergy(n)`       = `n == 0 ⇒ return; energy = max(0, energy - n); 广播`
//   `RefreshEnergy()`      = 每满一个 `cooldown` 补 `rechargeAmount`，**补到上限为止**；
//                            同时把 `lastUpdated` 前移「整周期」那么多（**不整除的部分保留**）
//   `Update()`             = `lastUpdated + cooldown <= now` 才 `RefreshEnergy()`
//
// 🔴 **与原版的唯一行为差异（⛔ 不是「我们挑的值」，是「不许崩宿主」那条红线）**：
//   原版 `RefreshEnergy` 在 `energyConfig == null` 时**走到 `FUN_1803f47a0()` = 抛 NRE**
//   （`.c` 末尾那一句，`energyData` 与 `energyConfig` 合成的 `if` 不成立就直接抛）。
//   本仓**没有 `EnergyConfig` 资源**（`find d:/2/新解包资源/assets_full -iname "*EnergyConfig*"` = 0；
//   也不存在 `ConfigManager`）⇒ 这一支会变成**每次读能量都抛异常**。按工程红线改成
//   **出声一次 + 不做回复**（返回当前值），与 `Core/MaterialChangerByQuality.cs` 处理同类 NRE 的口径一致。
//   ⇒ 今天观测到的行为 = **初始 100（原版缺配置时的默认值）且不回复**。
//
// ⚠️ **接线状态（如实记）**：本类**已经建好、但还没登记进 `LiveOpsSharedDataManager`** ——
//   装配处是 `LiveOpsSharedDataManager.Initialize(IEnumerable<LiveOpsSharedContext>)`，
//   它在本仓**生产 0 调用**，且 `Shell/LiveOpsSharedData.cs` 不在本批白名单
//   ⇒ 接线补丁写在 `资料/完全复刻/报告/B143_W7_R20九缺口.md` §③。
//   登记之后 `CurrencyExtensions.EnergyOf / SpendEnergyOf / GrantEnergyOf`（`Shell/Inventory.cs`）
//   可以把 `EnergyContext` 作为真源注入（现在那三个钩子生产从未注入）。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `Everguild.LiveOps.EnergyData`（`cpp2il/out_cpp2il_dev_ilspy/EnergyData.cs`）：
    /// `int energy`（`+0x10`）· `long lastUpdated`（`+0x18`，毫秒时间戳）。</summary>
    [Serializable]
    public sealed class EnergyData
    {
        public int energy;
        public long lastUpdated;
    }

    /// <summary>原版 `Everguild.LiveOps.Config.EnergyConfig`（字段偏移从 `EnergyContext__RefreshEnergy.c` /
    /// `__UnpackData.c` 反推）：`maxEnergy`（`+0x10`）· `cooldown`（`+0x18`，毫秒）· `rechargeAmount`（`+0x20`）。
    /// <para>⚠️ 本地**没有这份配置资源**（`ConfigManager` 也不存在）⇒ 生产路径上它恒为 `null`；
    /// 这个类只给自检/将来接配置用。</para></summary>
    [Serializable]
    public sealed class EnergyConfig
    {
        public int maxEnergy = 100;          // `UnpackData` 里配置取不到时的默认值 = **100**（`.c` 实读）
        public long cooldown = 300000L;      // 5 分钟；🔶 自己设计（非原版）：本地无该配置，取 `判据/日常与活动.md` §三 记的 cooldown
        public int rechargeAmount = 1;       // 每次回复 1 点；🔶 同上
    }

    /// <summary>原版 `EnergyContext : SharedDataContext`（见文件头）。</summary>
    public sealed class EnergyContext : LiveOpsSharedContext
    {
        /// <summary>原版 `private EnergyData energyData`（`+0x20`）。</summary>
        public EnergyData EnergyData { get; private set; }
        /// <summary>原版 `private EnergyConfig energyConfig`（`+0x28`）。本地恒 `null`（见文件头）。</summary>
        public EnergyConfig EnergyConfig { get; private set; }

        /// <summary>原版 `public static event Action&lt;int&gt; OnEnergyUpdated`（`+0x0`，静态）。
        /// 原版在 `GrantEnergy` / `SpendEnergy` / `RefreshEnergy` 三处广播**当前的 energy 值**。</summary>
        public static event Action<int> OnEnergyUpdated;

        /// <summary>原版 `UnpackData` 里「配置取不到 ⇒ 100」那个**魔数**（`.c` 实读 `uVar1 = 100`）。</summary>
        public const int DefaultEnergyWhenNoConfig = 100;

        bool _warnedNoConfig;

        /// <summary>给自检/将来接配置用：塞一份配置（原版是 `ConfigManager.GetConfig&lt;EnergyConfig&gt;()`）。</summary>
        public void SetConfigForTest(EnergyConfig config) { EnergyConfig = config; }

        /// <summary>原版 `public override void UnpackData(SharedEventData sharedEventData)`（`EnergyContext__UnpackData.c`）。
        /// <para>① 没有 `energyData` ⇒ 造一个（`energy = 配置最大值 ?? 100`、`lastUpdated = 现在`）并**写回**
        /// `sharedEventData.energyData`；② 绑定；③ 重新取配置。</para>
        /// <para>⚠️ 本地 `sharedEventData.energyData` 是 <see cref="LiveOpsJson"/>（原版是强类型 `EnergyData`，
        /// 差异 ③ 见 `Shell/LiveOpsSharedData.cs` 文件头）⇒ 读写键 `energy` / `lastUpdated`。</para></summary>
        public override void UnpackData(SharedEventData sharedEventData)
        {
            if (sharedEventData == null)
            {
                // 原版这一支是 NRE（`*(param_2 + 0x10)` 解引用）⇒ 按红线出声、不绑
                Debug.LogError("[Energy] UnpackData：`sharedEventData` 是 null（原版这一跳 NRE）⇒ 不解");
                return;
            }
            if (sharedEventData.energyData == null)
            {
                int max = EnergyConfig != null ? EnergyConfig.maxEnergy : DefaultEnergyWhenNoConfig;
                var fresh = LiveOpsJson.NewObject();
                fresh.Set("energy", (long)max);
                fresh.Set("lastUpdated", NowMs());
                sharedEventData.energyData = fresh;
            }
            EnergyData = FromJson(sharedEventData.energyData);
            EnergyConfig = null;      // 原版 = `ConfigManager.GetConfig<EnergyConfig>()`；本仓没有那一层 ⇒ 恒 null
        }

        /// <summary>原版 `public int GetCurrentEnergy()`（`.c` 全文两句）：
        /// `RefreshEnergy(); return energyData != null ? energyData.energy : 0;`</summary>
        public int GetCurrentEnergy()
        {
            RefreshEnergy();
            return EnergyData != null ? EnergyData.energy : 0;
        }

        /// <summary>原版 `public bool HasEnoughEnergy(int amount)`（`.c` 全文一句 + 一道空判）：
        /// `amount &lt;= energyData.energy`。原版空引用 ⇒ NRE；这里出声 + `false`（不许崩宿主）。</summary>
        public bool HasEnoughEnergy(int amount)
        {
            if (EnergyData == null)
            {
                Debug.LogWarning("[Energy] HasEnoughEnergy：`energyData` 是 null（原版这一跳 NRE）⇒ 按「不够」处理");
                return false;
            }
            return amount <= EnergyData.energy;
        }

        /// <summary>原版 `public void GrantEnergy(int amount)`（`.c` 逐句）：`amount == 0 ⇒ return`；
        /// `energy = max(0, energy + amount)`；广播。</summary>
        public void GrantEnergy(int amount)
        {
            if (amount == 0) return;
            if (EnergyData == null) { WarnNull("GrantEnergy"); return; }
            int v = EnergyData.energy + amount;
            EnergyData.energy = v < 0 ? 0 : v;
            RaiseUpdated();
        }

        /// <summary>原版 `public void SpendEnergy(int amount)`（`.c` 逐句）：`amount == 0 ⇒ return`；
        /// `energy = max(0, energy - amount)`；广播。
        /// <para>⚠️ `CurrencyExtensions.AddCurrency` 的 energy 支**走的就是 `SpendEnergy`（减）** —— 原版就这么写的，
        /// ⛔ 别「顺手修好」成 `GrantEnergy`（判据 → `Shell/Inventory.cs:1773` 那一行）。</para></summary>
        public void SpendEnergy(int amount)
        {
            if (amount == 0) return;
            if (EnergyData == null) { WarnNull("SpendEnergy"); return; }
            int v = EnergyData.energy - amount;
            EnergyData.energy = v < 0 ? 0 : v;
            RaiseUpdated();
        }

        /// <summary>原版 `public void RefreshEnergy()`（`EnergyContext__RefreshEnergy.c`，见文件头算式）。
        /// <para>① `energyData == null` ⇒ `CustomDebug.LogError` + return（**原版就是出声返回**，不是抛）；
        /// ② `energyConfig == null` ⇒ **原版抛 NRE**，本仓出声一次 + 不回复（红线，见文件头）；
        /// ③ 否则 `elapsed = now - lastUpdated`；`elapsed &lt; cooldown ⇒ return`；
        /// `ticks = floor(elapsed / cooldown)`；`gain = min(max(0, maxEnergy - energy), ticks × rechargeAmount)`；
        /// `energy += gain`；`lastUpdated += ticks × cooldown`；广播。</para></summary>
        public void RefreshEnergy()
        {
            if (EnergyData == null)
            {
                Debug.LogError("[Energy] RefreshEnergy：`energyData` 是 null（原版这一跳 `CustomDebug.LogError` 后 return）⇒ 不刷新");
                return;
            }
            if (EnergyConfig == null)
            {
                if (!_warnedNoConfig)
                {
                    _warnedNoConfig = true;
                    Debug.LogWarning("[Energy] RefreshEnergy：`energyConfig` 是 null —— 原版这一支**抛 NRE**（`.c` 末尾 `FUN_1803f47a0`），"
                                   + "本仓没有 `EnergyConfig` 资源也没有 `ConfigManager` ⇒ 按红线**出声 + 不做回复**（能量保持当前值）。");
                }
                return;
            }
            long cooldown = EnergyConfig.cooldown;
            if (cooldown <= 0) return;
            long elapsed = NowMs() - EnergyData.lastUpdated;
            if (elapsed < cooldown) return;                        // 原版：`lVar6 < config+0x18 ⇒ return`
            long ticks = elapsed / cooldown;                       // 原版：`floor((float)elapsed / (float)cooldown)`
            int space = EnergyConfig.maxEnergy - EnergyData.energy;
            if (space < 0) space = 0;                              // 原版：`iVar7 = max(0, iVar5)`
            long gain = ticks * EnergyConfig.rechargeAmount;
            int add = gain > space ? space : (int)gain;            // 原版：`min(space, ticks*amount)`
            EnergyData.energy += add;
            EnergyData.lastUpdated += ticks * cooldown;            // 原版：`lastUpdated += ticks * cooldown`（余数保留）
            RaiseUpdated();
        }

        /// <summary>原版 `private void Update()`（`EnergyContext__Update.c`）：
        /// `energyData == null ⇒ return`；`lastUpdated + cooldown &lt;= now ⇒ RefreshEnergy()`。
        /// <para>⚠️ 本仓没有 MonoBehaviour 生命周期（本类是普通类）⇒ 由调用方按帧调 `Update()`，
        /// 或干脆只走 `GetCurrentEnergy()` 里那一跳（原版两条路都通）。</para></summary>
        public void Update()
        {
            if (EnergyData == null) return;
            if (EnergyConfig == null) return;                      // 原版这一跳在 config 为 null 时同样会抛（同 `RefreshEnergy`）
            if (EnergyData.lastUpdated + EnergyConfig.cooldown <= NowMs()) RefreshEnergy();
        }

        // ------------------------------------------------------------ 工具

        /// <summary>原版 `TimeHelper.ServerTime`；关服后 = 本机时钟（全工程口径，同 `Shell/DailyData.cs`）。</summary>
        static long NowMs() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        static void WarnNull(string where)
        {
            Debug.LogWarning("[Energy] " + where + "：`energyData` 是 null（原版这一跳 NRE）⇒ 不做");
        }

        void RaiseUpdated()
        {
            var cb = OnEnergyUpdated;
            if (cb != null) cb(EnergyData != null ? EnergyData.energy : 0);
        }

        static EnergyData FromJson(LiveOpsJson json)
        {
            var d = new EnergyData();
            long v;
            if (json.TryGetLong("energy", out v)) d.energy = (int)v;
            if (json.TryGetLong("lastUpdated", out v)) d.lastUpdated = v;
            return d;
        }

        /// <summary>自检收尾用：清掉静态订阅（⛔ 生产不调）。</summary>
        public static void ResetForTest() { OnEnergyUpdated = null; }
    }
}
