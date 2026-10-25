// CardTierConfig.cs — 原版 `Everguild.LiveOps.Config.CardTierConfig` 的等价实现
//                    （玩家 XP 等级曲线 + 卡牌升级梯度表 + 收藏总 XP）
//
// ============================ 判据（唯一正本）============================
// **`资料/完全复刻/判据/CardTierConfig_服务端数据_取证_第3轮.md`**（2026-10-26）。
// 原版源码：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps.Config/CardTierConfig.cs`
//   （⚠️ 只有这一份 cpp2il 产物有真方法体 —— 见 `CLAUDE.md` §三「四份 Cpp2IL 产物只有
//    `out_cpp2il_dev_ilspy` 有真方法体」）。
//
// ============================ 三件东西的来源（逐件标清）============================
// ① **算式 = 原版，逐句照抄**（`GetXPRequired` / `GetAccumulatedXPForLevel` / `GetLevelForXP`）。
//    `RoundPrecision = 5f` 也是原版构造函数里**唯一的写死值**（`CardTierConfig.cs:543`），照写。
// ② 🔴 **4 个标量是自拟的，不是原版**：`Y_Max` / `X_Inflection` / `Offset` / `Base` 在远端 LiveOps
//    配置（`ConfigKey = "CardTierConfig"`）里，本机**四条独立取证全负**（`assets_full` 全量搜 /
//    `catalog_main.json` / 三份 `__data` 包 / `Player.log`——判据文件 §三）。
//    **用户 2026-10-26 裁定 (b)**：算式照原版、4 个数值自拟、代码里写明「数值自拟，非原版」。
//    ⇒ **数值自拟，非原版。** 选了 `Y_Max=100 · X_Inflection=20 · Offset=0 · Base=0.15`，理由见
//    构造函数上方那一段（含「为什么 Base 必须 > 0」）。
// ③ **`tiersByRarity` 那张表 = 读 `CardProgress`，不另造**：用户 2026-09-22 已裁决「升级表数值自拟
//    梯度」（`Shell/CardDetailPopup.cs:40/61` 的注释 + 判据文件 §四）。本文件**只做接线**：
//    `NeedCopies` → `RequiredCopies` · `NeedGold` → `PriceAmount` · `Points` → `PointReward` ·
//    **`XpAward` → `XPReward`**。
//    历史（保留更正痕迹）：本行原来写「🔴 `XPReward` 这一维 `CardProgress` 没有 ⇒ 本文件填 0 并出声，
//    后果 = 收藏总 XP 恒 0、玩家等级恒 1」—— **2026-10-26（批次 4 · F7）已销**：
//    `CardProgress.XpAward` 这一列补上了（同样是**自拟**的 XP 梯度，判据与取值理由写在那条数组上），
//    `XPReward` 现在直读它，警告已删。⚠️ 那一列**下标 1 仍是 0**（Tier1 = 新卡出厂档，没有「升到 1 级」）
//    ⇒ **新号玩家等级仍是 1**（与原版一致）；只有**升级过的卡**才产生 XP。
//
// ============================ 与原版的结构性差异（⛔ 不是「已经一样」）============================
//   · 原版 `CardTier` / `CardRarity` 是枚举；我们全工程走「**tier = 1 基 int**（`CardProgress.Level`）+
//     **rarity = 小写字符串**（`CardDef.Rarity`）」⇒ 方法形参跟着我们这边。等价关系
//     `ourTier = 原版 CardTier + 1`（`CardTier{Tier1=0..Tier4=3}`；同口径见 `Core/CardArt.cs` 的档位注释）。
//   · 原版 `CardTierInfo.Price` 里还有 `LoadableReference<Currency> Currency` 这一维；我们只有金币
//     （用户边界②「不做真实经济」）⇒ 只留 `PriceAmount`，⛔ 不建空的 Currency 壳。
//   · 原版由 `ConfigProvider<CardTierConfig>` 从远端下发一份实例；我们没有 provider 层 ⇒
//     进程级一份 `Instance`（构造时填自拟标量 + 建表）。取数路径不同，**算式与语义相同**。
//   · 原版 `GetTotalPlayerXPFromInventory` 遍历 `InventoryManager.Instance.GetInventory<RawCardScript>()`；
//     我们的等价物 = `Inventories.Cards`（`Shell/Inventory.cs` 的 `CardInventory`）。
//   · 原版缺表/缺格时**抛 NullReferenceException**；我们 `LogWarning` + 跳过该格（不崩、不静默）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `CardTierConfig.CardTierInfo` —— **一格**升级信息。
    /// 字段名照原版（`CardTier` / `RequiredCopies` / `PointReward` / `XPReward`）；
    /// `PriceAmount` 是原版 `Price.Amount`（原版那层还有 `Currency` 引用，见文件头「结构性差异」）。</summary>
    [System.Serializable]
    public sealed class CardTierInfo
    {
        /// <summary>**1 基**的档位（1..<see cref="CardProgress.MaxLevel"/>）。等价原版 `CardTier + 1`。</summary>
        public int CardTier;
        /// <summary>小写稀有度（`common` / `rare` / `epic` / `legendary` / `special`），与原版的分组键等价。</summary>
        public string Rarity;
        /// <summary>= 原版 `Price.Amount`（金币）。来源 = `CardProgress.NeedGold[CardTier]`。</summary>
        public int PriceAmount;
        /// <summary>= 原版 `RequiredCopies`。来源 = `CardProgress.NeedCopies[CardTier]`（**累计**要几张副本）。</summary>
        public int RequiredCopies;
        /// <summary>= 原版 `PointReward`。来源 = `CardProgress.Points[CardTier]`。</summary>
        public int PointReward;
        /// <summary>= 原版 `XPReward`。来源 = `CardProgress.XpAward[CardTier]`（🔶 自拟梯度，理由见那条数组）。</summary>
        public int XPReward;
    }

    /// <summary>原版 `CardTierConfig.CardTierByRarity` —— 一个稀有度一组。</summary>
    [System.Serializable]
    public sealed class CardTierByRarity
    {
        public string Rarity;
        public List<CardTierInfo> TierInfo = new List<CardTierInfo>();
    }

    /// <summary>原版 `Everguild.LiveOps.Config.CardTierConfig` 的等价实现。用法：
    /// <c>CardTierConfig.PlayerLevel</c>（读 `TopBar` / `ProfileTab` 那一格）·
    /// <c>CardTierConfig.Instance.GetTierInfo(tier, rarity)</c>（升级面板那一族）。
    /// 判据与「哪些数是自拟的」→ 文件头。</summary>
    public sealed class CardTierConfig
    {
        // ⚠️ **必须声明在 `Instance` 之前**：C# 的静态字段初始化按**文本顺序**跑 —— `Instance` 那一句会
        //    立刻 `new CardTierConfig()`，而构造函数要读 `Rarities` 建表；把它写在 `Instance` 后面
        //    ⇒ 构造函数里读到的还是 `null`、`Rarities.Length` 直接 `TypeInitializationException`
        //    （**编译期抓不到**，只有第一次用到本类时才炸）。⛔ 别把这条挪到「建表」那一段去。
        /// <summary>我们这份配置认的五个稀有度（理由 → 「建表」那一段；⛔ 别在方法里再写第二份）。</summary>
        static readonly string[] Rarities = { "common", "rare", "epic", "legendary", "special" };

        /// <summary>我们这份配置的唯一实例（原版是远端下发；见文件头「结构性差异」）。</summary>
        public static readonly CardTierConfig Instance = new CardTierConfig();

        /// <summary>原版字段（名字照抄）：按稀有度分组的升级梯度表。</summary>
        public List<CardTierByRarity> tiersByRarity = new List<CardTierByRarity>();

        // ---- 原版那 5 个标量（名字照抄）----
        // 🔶 自己设计（非原版）：玩家等级曲线 4 个标量 `Y_Max` / `X_Inflection` / `Offset` / `Base`
        //    （赋值在构造函数里）· 原版在远端 liveops 的 `CardTierConfig` 配置里，本机**四条独立取证全负**
        //    （`assets_full` 全量搜 / Addressables `catalog_main.json` / 3 份关服前 `__data` 包 / `Player.log`
        //    —— 判据 → `资料/完全复刻/判据/CardTierConfig_服务端数据_取证_第3轮.md` §三）· 用户 2026-10-26 裁定 (b) 自拟
        //    （**算式仍逐句照原版**，只有这 4 个数是本项目挑的）。
        //    ⚠️ 同段的 `RoundPrecision = 5f` **不在自设计之列** —— 那是原版构造函数里唯一的写死值（照抄）。
        /// <summary>🔴 **数值自拟，非原版**（判据文件 §三：远端 LiveOps 数据本机四条取证全负）。
        /// 曲线在高等级的**上界**（每级所需 XP 趋近它）。</summary>
        public float Y_Max;
        /// <summary>🔴 **数值自拟，非原版**。logistic 的**拐点等级**（这一级附近曲线最陡/最缓的转折）。
        /// ⚠️ 必须 &gt; 0：算式里有 `X_Inflection / 5f` 做分母（原版同样没有除零保护）。</summary>
        public float X_Inflection;
        /// <summary>🔴 **数值自拟，非原版**。曲线整体的**纵向平移**（原版算式最后 `+ Offset`）。</summary>
        public float Offset;
        /// <summary>🔴 **数值自拟，非原版**。logistic 的**斜率系数**（决定从低到高爬得多快）。</summary>
        public float Base;
        /// <summary>= 原版构造函数里**唯一写死的值**（`CardTierConfig.cs:543` `RoundPrecision = 5f`）
        /// ⇒ 这一格**不是自拟的**，照抄。</summary>
        public float RoundPrecision;

        // ============================================================ 选参理由（为什么是这 4 个数）
        //
        // 判据文件 §五 给的例子是 `Y_Max=100 · X_Inflection=20 · Base=-0.15 · Offset=0`（原文写「如」）。
        // 🔴 **`Base` 取负会让曲线【递减】**：`num4 = Base·((level−XI)/(XI/5))`，Base<0 时 level 越大
        //    `num4` 越负 ⇒ `Exp` 越小 ⇒ `1/(Exp+1)` 越大 ⇒ `1−num6` 越小 ⇒ **每级所需 XP 越来越低**
        //    （上界退化成 `Offset`），那与字段名 `Y_Max`（= 最高每级需求）自相矛盾。
        //    `Base>0` 时高等级趋近 `Y_Max`、低等级最小 ⇒ 曲线单调不减、`Y_Max` 名副其实。
        //    ⇒ **取 `Base = 0.15`**（正）。
        // 具体取值与量级（实算，`RoundPrecision=5` 取整后）：
        //    等级 1 → **35** · 6 → 35 · 7 → 40 · 20 → 50 · 60 → 80 · 上界 100（= `Y_Max`）。
        //    累计：1→2 级 35 · 2→3 级 70 · 3→4 级 105（前期每级 35–40，等级越高越贵）。
        //    为什么这个量级合理：原版每张卡最多给 **4 次** `xpReward`（`CardTier` 只有 Tier1..Tier4）
        //    ⇒ 一个收集满 1,127 张卡的账号，每级 35–100 的 XP 是「千位数总量」的量级，不会一进游戏就
        //    冲过 9999 那道上限保护（`GetLevelForXP` 里那条 `num > 9999`）。
        // ⚠️ **这 4 个数是我们挑的**：将来远端真配置出现（或用户另定），**只改这 4 个赋值**，算式不动。

        public CardTierConfig()
        {
            // ---- 原版写死值（照抄）----
            RoundPrecision = 5f;
            // ---- 自拟 4 标量（🔴 非原版；理由见上面那一段）----
            Y_Max = 100f;
            X_Inflection = 20f;
            Offset = 0f;
            Base = 0.15f;

            BuildTiersFromCardProgress();
        }

        // ============================================================ 建表（`tiersByRarity`）
        //
        // 🔶 自己设计（非原版）：`tiersByRarity` 的**取数结构** —— 原版走远端配置表（`IConfigData` /
        //    `ConfigProvider<CardTierConfig>` 下发一份 `List<CardTierByRarity>`），我们没有 provider 层
        //    ⇒ 直连本工程**自拟**的升级表 `CardProgress`（`NeedCopies` / `NeedGold` / `Points` 三条数组）。
        //    数值本身**不是本文件设计的**（用户 2026-09-22 裁决自拟梯度；2026-10-26 裁定 (b) 沿用同一口径）
        //    —— 这条标的是「原版没有的这条直连结构」，⛔ 不是把那些数说成本文件编的。
        //
        // 🔴 **不重新设计那张表**：数据源 = `CardProgress` 的三条数组（用户 2026-09-22 裁决自拟梯度）。
        //    下标 = **1 基档位**（`[0]` 两个都不用；`MaxLevel = 4` ⇒ 有效档 1..4，与原版 Tier1..Tier4 同数量）。
        // ⚠️ 原版是按稀有度**分组**的；`CardProgress` 那三条数组**不分稀有度**（五档共用同一组数）
        //    ⇒ 五组的**内容相同**。这不是我们偷懒，是数据源就长这样（如实标）。
        //    哪五档：判据 = `Assets/RuleEngine/Resources/cards_engine.json` 的 `rarity` 列
        //    （现读：common / rare / epic / legendary / special）。
        //    ⚠️ 那五档的数组本体声明在类头 `Instance` 之前（**不能挪到这里**）—— 见那边那条注释。

        void BuildTiersFromCardProgress()
        {
            tiersByRarity.Clear();
            for (int r = 0; r < Rarities.Length; r++)
            {
                var group = new CardTierByRarity { Rarity = Rarities[r] };
                int max = CardProgress.MaxLevel;
                for (int tier = 1; tier <= max; tier++)
                {
                    int copies = (tier < CardProgress.NeedCopies.Length) ? CardProgress.NeedCopies[tier] : 0;
                    int gold = (tier < CardProgress.NeedGold.Length) ? CardProgress.NeedGold[tier] : 0;
                    int pts = (tier < CardProgress.Points.Length) ? CardProgress.Points[tier] : 0;
                    // 🆕 **2026-10-26（批次 4 · F7 · 任务 ②）**：XP 奖励那一维**有数据源了** ——
                    //   `CardProgress.XpAward`（🔶 自拟梯度，取值理由写在那边那条数组上）。
                    //   本文件仍然**不编第二份梯度表**：只做 `XpAward[tier]` → `XPReward` 的转发。
                    //   ⚠️ 同一处另有一条自设计：**按稀有度分五组、五组内容相同** —— 原版每组数不同，
                    //   而 `CardProgress` 那四条数组不分稀有度（数据源就长这样，如实标）。
                    int xp = (tier < CardProgress.XpAward.Length) ? CardProgress.XpAward[tier] : 0;
                    var info = new CardTierInfo
                    {
                        CardTier = tier,
                        Rarity = Rarities[r],
                        PriceAmount = gold,
                        RequiredCopies = copies,
                        PointReward = pts,
                        XPReward = xp,
                    };
                    group.TierInfo.Add(info);
                }
                tiersByRarity.Add(group);
            }
        }

        // ============================================================ 玩家等级（接线口）
        //
        // 原版：`PlayerProfileUIController.SetPlayerLevel(PlayerDataManager.CurrentLevel)`，而
        // `CurrentLevel` 是**服务端**下发的收藏等级。我们这边把它换成「用本地库存 + 本文件的曲线
        // **算出来**」—— 算式是原版的、4 个标量是自拟的（用户 2026-10-26 裁定 (b)）。
        // ⚠️ 两处显示（顶栏 `Shell/TopBar.cs` 与档案窗 `Shell/ProfileTab.cs`）走**同一个口**
        //    （⛔ 别各算一份 —— 那迟早不一致）。

        /// <summary>当前玩家的收藏等级（= `GetLevelForXP(GetTotalPlayerXPFromInventory())`）。
        /// 🔴 **新号恒为 1**（原版也是）：收集总 XP 只来自**升过级的卡**（`XpAward` 下标 1 = 0），
        /// 出厂每张卡都是 Tier1 ⇒ 总 XP = 0。🆕 2026-10-26（F7）：XP 那一维**已有数据源**
        /// （`CardProgress.XpAward`），升一张卡就会动（旧文档「恒为 1」已过期）。</summary>
        public static int PlayerLevel
        {
            get { return Instance.GetLevelForXP(Instance.GetTotalPlayerXPFromInventory()); }
        }

        /// <summary>那一格要印的串（原版 `playerLevelText.text = level.ToString()`）。
        /// 用不变文化（等级不参与本地化，别让区域设置把它变成别的数形）。</summary>
        public static string PlayerLevelText
        {
            get { return PlayerLevel.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        }

        // ============================================================ 原版的 6 个方法
        //
        // 下面 `GetXPRequired` / `GetAccumulatedXPForLevel` / `GetLevelForXP` 三件是
        // **逐句照原版**（判据文件 §二 的算式与 `CardTierConfig.cs:150-414` 逐行对过）。

        /// <summary>原版 `GetXPRequired(float level)`：**这一级**需要多少 XP。
        /// <para>算式（照原版逐句，中间量名字也照抄）：</para>
        /// <code>
        /// num4  = Base * ((level - X_Inflection) / (X_Inflection / 5f))
        /// num5  = Exp(num4) + 1
        /// num6  = 1 / num5
        /// num7  = Y_Max - Offset
        /// num10 = ((1 - num6) * num7) / RoundPrecision
        /// num12 = round-half-away-from-zero(num10)     // 原版那两段 `if (num10 &lt; 0)` 是 Math.Round 的手工内联
        /// return num12 * RoundPrecision + Offset
        /// </code>
        /// <para>⚠️ 原版 `num4` 之后那个 `Method not found @18048B040` = `Mathf.Exp`（后面紧接 `+1f` 再 `1f/`，
        /// 就是标准 logistic）；`@180486C38` / `@180486BBC` / `@180486D18` = `Math.Round` 手工内联用的
        /// `floor/ceil` 族。⚠️ 三处都不是「缺失的业务逻辑」（判据文件 §二 末）。</para></summary>
        public float GetXPRequired(float level)
        {
            float num = level - X_Inflection;
            float num2 = X_Inflection / 5f;
            float num3 = num / num2;
            float num4 = Base * num3;
            float num5 = Mathf.Exp(num4) + 1f;
            float num6 = 1f / num5;
            float num7 = Y_Max - Offset;
            float num8 = 1f - num6;
            float num9 = num8 * num7;
            float num10 = num9 / RoundPrecision;
            // 原版 = `Math.Round(num10)` 的手工内联 = **四舍五入、.5 远离零**（不是银行家舍入）。
            float num12 = (float)System.Math.Round((double)num10, System.MidpointRounding.AwayFromZero);
            float num15 = num12 * RoundPrecision;
            return num15 + Offset;
        }

        /// <summary>原版 `GetAccumulatedXPForLevel(int level)` = `Σ_{i=1..level} GetXPRequired(i)`；
        /// `level &lt; 1` ⇒ `0`。
        /// ⚠️ 原版把这个求和**内联**在循环里（`CardTierConfig.cs:221-313`）；逐句等价，我们直接调
        /// `GetXPRequired`（同一个函数、同一组 float 运算 ⇒ 值逐位相同）。</summary>
        public float GetAccumulatedXPForLevel(int level)
        {
            if (level < 1) return 0f;
            float sum = 0f;
            for (int i = 1; i <= level; i++) sum += GetXPRequired(i);
            return sum;
        }

        /// <summary>原版 `GetLevelForXP(float xp)`：逐级累加，**第一次超过 xp** 时返回「上一级」（下限 1）。
        /// <para>⚠️ 两点照原版：① 判定是 `sum &gt; xp`（**严格大于**）⇒ 恰好等于累计值的那一档**算作已达成**；
        /// ② 上限保护 `num &gt; 9999` ⇒ `Debug.LogWarning("XP exceeds defined curve range.")` 并返回 `num`。</para></summary>
        public int GetLevelForXP(float xp)
        {
            int num = 0;
            float sum = 0f;
            while (true)
            {
                num++;
                sum += GetXPRequired(num);
                if (!(sum > xp))
                {
                    if (num > 9999)
                    {
                        Debug.LogWarning("[CardTierConfig] XP exceeds defined curve range.（xp=" + xp
                                       + " 累加到第 " + num + " 级仍未超过 ⇒ 原版同一句警告，返回 " + num + "）");
                        return num;
                    }
                    continue;
                }
                int num19 = num - 1;
                return num19 < 1 ? 1 : num19;
            }
        }

        /// <summary>原版 `GetTierInfo(CardTier tier, CardRarity rarity)`：`tiersByRarity` 里
        /// **先按 rarity 找那一组、再按 tier 找那一格**（原版是两条 `Enumerable.FirstOrDefault`）。
        /// <para>⚠️ 形参跟我们这边：`tier` = **1 基** int · `rarity` = 小写字符串（文件头「结构性差异」）。
        /// 找不到 ⇒ 返回 `null`（原版这里会 `throw NullReferenceException` —— 铁律「不许静默失败」+
        /// 「不崩」⇒ 我们出声 + null，调用方自己判）。</para></summary>
        public CardTierInfo GetTierInfo(int tier, string rarity)
        {
            var group = FindGroup(rarity);
            if (group == null || group.TierInfo == null) return null;
            for (int i = 0; i < group.TierInfo.Count; i++)
            {
                var t = group.TierInfo[i];
                if (t != null && t.CardTier == tier) return t;
            }
            return null;
        }

        /// <summary>原版 `GetNextTierInfo(CardTier currentTier, CardRarity rarity)` = `GetTierInfo(currentTier + 1, rarity)`。
        /// 满级 ⇒ `null`（原版同样返回 null，因为它那一格里没有 Tier5）。</summary>
        public CardTierInfo GetNextTierInfo(int currentTier, string rarity)
        {
            return GetTierInfo(currentTier + 1, rarity);
        }

        /// <summary>原版 `GetTotalPlayerXPFromInventory()`：遍历玩家收藏里每张卡，对每张卡从
        /// **Tier1 到它自己的档位**逐档累加 `xpReward`。
        /// <para>**原版谓词**（`CardTierConfig.cs:437`）与我们的等价：</para>
        /// <list type="number">
        /// <item>`card.IsOnCatalog` —— 我们的 `Inventories.Cards` **只装卡池里的卡**
        ///   （播种源 = 收藏窗传进来的池）⇒「进了背包 = 在目录上」，这一条恒真。</item>
        /// <item>`cardType != Tactic || (spellType != DefensiveCard &amp;&amp; spellType != OffensiveCard)`
        ///   —— 枚举真值（`dump.cs`）：`CardTypeOptions.Tactic = 20` · `SpellType.OffensiveCard = 200` ·
        ///   `SpellType.DefensiveCard = 210`。我们的等价 = **排除 `spellType ∈ {200, 210}` 的战术族卡**
        ///   （现读卡池：`210` 那 39 张正是 `type == "defence"` 的防守卡；`200` 本池 0 张 —— 进攻卡是
        ///   13 场 × 4 的战场环境卡，不在池里）。</item>
        /// <item>`!card.IsAlternateArt()` —— 判据 = 异画唯一正本 `CollectionWindow.AltArtCards`
        ///   （`Shell/Inventory.cs` 的 `CardInventory.IsAlternateArt` 用的是同一张表；它现在是 `private`，
        ///   所以本文件直接查表 —— **同一份判据，不是第二份规则**；已记改动请求）。</item>
        /// </list>
        /// <para>🆕 **2026-10-26（F7）**：`XPReward` 已有数据源（`CardProgress.XpAward`）⇒ 本方法**不再是恒 0**
        /// （旧文档「今天它恒返回 0」已过期）。仍恒 0 的只有「所有卡都停在 Tier1」的新号。</para></summary>
        public int GetTotalPlayerXPFromInventory()
        {
            var inv = Inventories.Cards;
            if (inv == null)
            {
                // 原版这里 throw NRE。我们出声 + 0（不崩、不静默）。
                Debug.LogWarning("[CardTierConfig] `Inventories.Cards` 为 null ⇒ 收藏总 XP 按 0 算");
                return 0;
            }
            int total = 0;
            foreach (var entry in inv.GetItems())
            {
                var card = entry != null ? entry.Item : null;
                if (card == null) continue;
                if (!IsCountedForPlayerXP(card)) continue;
                // 原版读 `rawCardScript.CardTier`（0 基）；我们的档位正本 = `CardProgress.Level`（1 基）。
                int tier = Mathf.Clamp(CardProgress.Level(card.Id), 1, CardProgress.MaxLevel);
                for (int t = 1; t <= tier; t++)
                {
                    var info = GetTierInfo(t, card.Rarity);
                    if (info == null)
                    {
                        // 原版这里 throw NRE。我们出声（同一格只报一次）+ 跳过。
                        WarnOnce("missing-tier:" + card.Rarity + ":" + t,
                            "[CardTierConfig] 稀有度 `" + card.Rarity + "` 的档位 " + t
                          + " 在 tiersByRarity 里没有 ⇒ 该格 XP 按 0 算（原版这里会抛 NRE）");
                        continue;
                    }
                    total += info.XPReward;
                }
            }
            return total;
        }

        // ============================================================ 内部小件

        CardTierByRarity FindGroup(string rarity)
        {
            if (string.IsNullOrEmpty(rarity)) return null;
            string key = rarity.ToLowerInvariant();
            for (int i = 0; i < tiersByRarity.Count; i++)
            {
                var g = tiersByRarity[i];
                if (g != null && g.Rarity == key) return g;
            }
            return null;
        }

        /// <summary>原版 `_003CGetTotalPlayerXPFromInventory_003Eb__11_0`（`CardTierConfig.cs:75-89`）的等价谓词。
        /// 三条判据的来源 → `GetTotalPlayerXPFromInventory` 的 doc。</summary>
        static bool IsCountedForPlayerXP(RuleEngine.CardDef card)
        {
            if (card == null) return false;
            // ② 战术族里排除 Defensive(210) / Offensive(200)。我们表里防守卡 `type == "defence"`；
            //    这一条同时写 `type` 是为了在数据将来改成 `tactic + 210` 时**行为不变**。
            bool tacticFamily = card.Type == "tactic" || card.Type == "defence";
            if (tacticFamily && (card.SpellType == 200 || card.SpellType == 210)) return false;
            // ③ 异画不算（原版 `!IsAlternateArt()`）；判据 = 唯一正本那张表。
            if (IsAltArt(card)) return false;
            return true;
        }

        /// <summary>这张卡在不在异画表里。**判据 = `CollectionWindow.AltArtCards`（唯一正本）**；
        /// 与 `Shell/Inventory.cs` 的 `CardInventory.IsAlternateArt` 同源（那份是 private，见改动请求）。
        /// ⛔ 别按「id 里有 AA」猜（那是文件名约定，不是数据 —— 该文件的注释也这么写）。</summary>
        static bool IsAltArt(RuleEngine.CardDef card)
        {
            if (card == null) return false;
            var table = CollectionWindow.AltArtCards;
            if (table == null) return false;
            for (int i = 0; i < table.Length; i++)
                if (table[i].CardId == card.Id) return true;
            return false;
        }

        static readonly HashSet<string> _warned = new HashSet<string>();
        /// <summary>同一个键只出声一次（逐卡逐格 `LogWarning` 会刷屏）。⚠️ 只是去噪，**不是静默**。</summary>
        static void WarnOnce(string key, string msg)
        {
            if (!_warned.Add(key)) return;
            Debug.LogWarning(msg);
        }

        // ============================================================ 自检（宿主不在本批白名单）
        //
        // 🔴 本批**不跑 Unity**（批次 3 的硬红线），而且自检宿主不在白名单 ⇒ 这里只提供
        //   **纯函数式**的检查口：返回失败清单（空 = 全过）。将来宿主（`Editor/ShellScene.cs` 之类）
        //   只要 `foreach (var f in CardTierConfig.SelfCheck()) Check(false, f);` 就接上了。
        //   断言内容与「为什么这么断」→ 报告 `资料/完全复刻/报告/E8_第3轮.md` §四。

        /// <summary>等级曲线自检。返回**失败项**（空 list = 全过）。
        /// <para>① 单调性：`GetLevelForXP(GetAccumulatedXPForLevel(n)) == n`（n = 1..50）+
        /// `GetXPRequired` 随 level 单调不减。</para>
        /// <para>② **灭自证**：等级**不是常数** —— `lvl(0)==1 &amp;&amp; lvl(acc(2))==2 &amp;&amp; lvl(acc(5))==5`
        /// 三个不同值同时成立（把 `GetLevelForXP` 改回「返回固定 1」必红）。</para>
        /// <para>③ 取整语义：`RoundPrecision == 5f` 且 `GetXPRequired` 的返回值**一定是 5 的倍数加 `Offset`**
        /// （把 `Math.Round` 拿掉必红 —— 那样返回值就不是整数倍了）。</para></summary>
        public static List<string> SelfCheck()
        {
            var fail = new List<string>();
            var c = Instance;

            // ①-a 往返
            for (int n = 1; n <= 50; n++)
            {
                float acc = c.GetAccumulatedXPForLevel(n);
                int back = c.GetLevelForXP(acc);
                if (back != n)
                    fail.Add("① " + n + " 级往返不等：`GetLevelForXP(GetAccumulatedXPForLevel(" + n + "))` = " + back
                           + "（累计 XP = " + acc + "）");
            }
            // ①-b 单调不减
            float prev = c.GetXPRequired(1f);
            for (int l = 2; l <= 50; l++)
            {
                float cur = c.GetXPRequired(l);
                if (cur < prev - 1e-3f)
                    fail.Add("① `GetXPRequired` 在 level=" + l + " 下降：" + prev + " → " + cur
                           + "（自拟标量选错，判据 = 报告 §末「选参理由」）");
                prev = cur;
            }
            // ② 灭自证：三个不同值同时成立
            if (!(c.GetLevelForXP(0f) == 1
               && c.GetLevelForXP(c.GetAccumulatedXPForLevel(2)) == 2
               && c.GetLevelForXP(c.GetAccumulatedXPForLevel(5)) == 5))
                fail.Add("② 等级是常数（灭自证失败）：`lvl(0)` = " + c.GetLevelForXP(0f)
                       + " · `lvl(acc(2))` = " + c.GetLevelForXP(c.GetAccumulatedXPForLevel(2))
                       + " · `lvl(acc(5))` = " + c.GetLevelForXP(c.GetAccumulatedXPForLevel(5)));
            // ③ 取整语义
            if (c.RoundPrecision != 5f)
                fail.Add("③ `RoundPrecision` = " + c.RoundPrecision + "，原版构造函数写死 5f");
            for (int l = 1; l <= 60; l++)
            {
                float xp = c.GetXPRequired(l);
                float units = (xp - c.Offset) / c.RoundPrecision;
                if (Mathf.Abs(units - Mathf.Round(units)) > 1e-3f)
                    fail.Add("③ level=" + l + " 的 `GetXPRequired` = " + xp
                           + " 不是 `RoundPrecision` 的整数倍 + Offset（取整没做上）");
            }
            return fail;
        }
    }
}
