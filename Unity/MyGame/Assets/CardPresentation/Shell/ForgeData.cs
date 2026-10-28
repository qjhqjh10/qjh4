// ForgeData.cs — 锻造厂页的**本地数据源**（单机版）
//
// ============================ 这是**我们自建**的，不是原版的做法 ============================
// 🔴 原版这一整套是**服务端**的：反编译实证 `ForgeWindowTab.SelectArmy` 取的是
//    `LiveOpsManager.GetHandler<ForgeHandler>()` 里的 Forge 存档，`currentPoints` / `rewardsCollected` /
//    每级所需 `Points` / 奖励物品 `RewardInfo` **全在远端**；`Debug Set Forge` 走的是
//    PlayFab 云脚本 `"SetForgeLevel"`。原版已关服 ⇒ **本地一条都取不到**。
//    ⇒ 照「日常」那一层的先例（`Shell/DailyData.cs`）：**数据由我们自建，并在这里逐条标明「我们挑的」**。
//
// ✅ **照原版的**：底层的**状态机语义** —— 四态 `Locked(0) / InProgress(1) / ToCollect(2) / Collected(4)`
//    （枚举出处 `d:/2/tools/il2cpp_out/dump.cs:68148-68157`，判定公式出处
//    `d:/2/tools/decomp_full/ForgeRewardItemContainer__Initialize.c:57-75` 与 `__GetLevelState.c:6-15`）：
//      · `rewardsCollected == index`（这格是下一个该领的）且 `currentPoints >= 本格 Points` → **ToCollect**
//      · `currentPoints < 本格 Points` → **InProgress**
//      · 前一格还没达标 → **Locked**
//      · `index < rewardsCollected` → **Collected**
//    ⇒ **经验是累计的、领取只把 `rewardsCollected` 加一**（不扣经验）—— 这条照原版。
//    · 升级文案 `"{0} {1}/{2}"` + 本地化键 `"MainMenu/Level"`（出处 `ForgeWindowTab__RefreshLevel.c:40-50`，
//      三个字面量地址已解析）⇒ 显示成 `Level 3/50`。
//
// ❌ **我们挑的**：阵营列表的初值、每级所需经验的曲线、每级奖励的图标与数量、起始等级与经验。
//
// ⏭ **接线点**：以后若做 P2P / 存档，这里是唯一要换掉的地方（调用方只认下面这几个静态入口）。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>单机版的「锻造厂」状态。**纯内存 + 可复现**（不用 `UnityEngine.Random`）。</summary>
    public static class ForgeData
    {
        /// 🔶 自己设计（非原版）：锻造轨道格数 50 · 唯一依据是 prefab 占位文案 `Level 1/50`，原版真实格数在服务端 · TODO(谁定的)｜原注：<summary>轨道格数（= `LevelText` 的分母）。⚠️ **我们挑的 50** ——
        /// 唯一的依据是 prefab 里那句占位文案 `Level 1/50`（`l3_rect_full.txt:229` 那一格，
        /// 原版真实格数在服务端）。**别把它当成原版数字。**</summary>
        public const int MaxLevel = 50;

        /// <summary>13 个阵营。**顺序照卡池实测**（`Assets/RuleEngine/Resources/cards_engine.json` 里 faction 的出现频次序）。
        /// ⚠️ 图标**不在这里写死** —— 走 `DeckRuntime.FactionIcon`（那条映射是全工程唯一一份，别抄第二份）。</summary>
        public static readonly string[] Armies =
        {
            "Ultramarines", "Goff", "SaimHann", "BlackLegion", "DarkAngels", "Genestealers", "TauEmpire",
            "Sautekh", "AstraMilitarum", "Leviathan", "Sororitas", "EmperorsChildren", "SpaceWolves",
        };

        /// <summary>一格奖励。⚠️ **「哪一级给什么」是我们挑的**（原版 `RewardInfo` 在服务端）。
        /// <para>🔴 **2026-10-12（A401）就地订正**：原来这个结构里只有 `Text`（显示用的**字符串**），
        /// 于是「领到多少」这件事**在数据侧不是一个数** —— 接领奖窗时要再 `int.Parse` 一次字符串
        /// （两个地方各认一次同一个数 = 迟早不一致）。现在数量就是 `Qty` 一个 `int`，
        /// `Text` 由它派生。</para>
        /// <para>🔴 **2026-10-17（界面线 T21）**：加 `WildcardRarity` —— 原版锻造奖励不是「一张图标」，
        /// 而是**野牌 / 增强包**（实拍 `奖励—锻造厂的参考图.png`：紫/绿野牌 + 一包卡），由
        /// `ItemDrawer.Draw` 整层画出来（卡面图 + 品阶 + 阵营名 + 数量 + 阵营小徽）。
        /// ⇒ 我们的奖励改成**真野牌**（`40k_general_wildcard_*` 四档图本地都在），
        /// 「哪一级给哪档」仍是我们挑的（服务端数据本地没有）。</para></summary>
        public struct Reward
        {
            /// <summary>奖励图名（野牌 = `40k_general_wildcard_{common,rare,epic,legendary}`；
            /// 领奖窗也读它）。</summary>
            public string Art;
            /// <summary>发多少（原版 `RewardInfo.quantity` 那一格）。**领奖窗与卡面下方那行字都读它**。</summary>
            public int Qty;
            /// <summary>野牌稀有度 **1..4**（`cardRarity` 那一档）；`0` = 不是野牌（走旧那条裸图）。
            /// ⚠️ 渲染时拼成 `Wildcard<阵营><稀有度>` 交给 `ItemDrawer.Spec` —— 阵营取**当前选中的阵营**。</summary>
            public int WildcardRarity;
            /// <summary>兜底短名（抽屉取不到图时占位板上用；野牌那两档用不到）。</summary>
            public string Label;
            /// <summary>显示在奖励下方的数量。**由 `Qty` 派生** —— ⛔ 别在 `Palette` 里再手写一个数字。</summary>
            public string Text { get { return Qty.ToString(); } }
        }

        // 🔶 自己设计（非原版）：锻造奖励表（哪一级给哪档） · 原版在服务端 · TODO(谁定的)｜原注：⚠️ **我们挑的奖励表**：按「每级轮换」编。🔴 **2026-10-17（T21）**：从四张货币图标
        //    改成**四档真野牌**（图 = 原版 `40k_general_wildcard_*`，判据 = 实拍那三张卡 + `ItemDrawer`
        //    的 `Wildcard` 抽屉）。**「哪一级给哪档」仍是我们挑的**（原版在服务端）—— 如实标。
        static readonly Reward[] Palette =
        {
            new Reward { Art = "40k_general_wildcard_epic",      Qty = 1, WildcardRarity = 3 }, // 史诗通用牌（实拍紫色那张）
            new Reward { Art = "40k_general_wildcard_rare",      Qty = 1, WildcardRarity = 2 }, // 稀有通用牌（实拍绿色那张）
            new Reward { Art = "40k_general_wildcard_legendary", Qty = 1, WildcardRarity = 4 }, // 传说通用牌
            new Reward { Art = "40k_general_wildcard_common",    Qty = 1, WildcardRarity = 1 }, // 普通通用牌
        };

        /// <summary>第 `level` 格（0 基）的奖励。⚠️ 我们挑的。</summary>
        public static Reward RewardAt(int level) { return Palette[((level % Palette.Length) + Palette.Length) % Palette.Length]; }

        /// <summary>第 `level` 格（0 基）**累计**需要的经验。⚠️ **我们挑的曲线**（原版曲线在服务端）：
        /// 每级 +100，每满 5 级再叠一个 20·n·(n/5) 的台阶 —— **单调递增**、好读。
        /// **累计**语义照原版（判据是 `currentPoints >= 本格 Points`，本格 Points 就是累计门槛 —— 见文件头）。</summary>
        public static int NeededAt(int level)
        {
            int n = level + 1;
            return 100 * n + 20 * n * (n / 5);
        }

        // 🔶 自己设计（非原版）：每阵营的锻造进度初值 · 原版在服务端存档 · TODO(谁定的)｜原注：---- 每个阵营的进度。⚠️ **初值全是我们挑的**（原版在服务端存档里）。
        //      故意造出四种状态都出现的情况，好让四种格子都能在画面上验到：
        //        第 1 个：已满级（全 Collected）· 第 2 个：正好可领（ToCollect）
        //        第 3 个：差一点（InProgress）· 其余：按索引错开
        static readonly Dictionary<string, int> _level = new Dictionary<string, int>();
        static readonly Dictionary<string, int> _points = new Dictionary<string, int>();

        static ForgeData()
        {
            for (int i = 0; i < Armies.Length; i++) Init(Armies[i], i);
        }

        static void Init(string army, int i)
        {
            // ⚠️ 我们挑的初值。`NeededAt` 是累计门槛，所以「正好可领」= points 恰好等于 NeededAt(level)。
            switch (i)
            {
                case 0: _level[army] = MaxLevel; _points[army] = NeededAt(MaxLevel - 1); break;          // 满级
                case 1: _level[army] = 3;        _points[army] = NeededAt(3); break;                     // **正好可领**
                case 2: _level[army] = 3;        _points[army] = NeededAt(3) - 40; break;                // 差 40
                default: _level[army] = 1 + (i % 6); _points[army] = NeededAt(_level[army]) - 30 * (i % 5); break;
            }
        }

        /// <summary>当前选中的阵营（照原版：开窗时从战役/存档取，我们给个固定的起点）。</summary>
        public static string Selected { get; private set; } = Armies[1];

        public static void Select(string army)
        {
            if (string.IsNullOrEmpty(army)) return;
            foreach (var a in Armies) if (a == army) { Selected = a; return; }
        }

        /// <summary>已领格数（原版 `rewardsCollected`）。</summary>
        public static int LevelOf(string army) { int v; return _level.TryGetValue(army, out v) ? v : 0; }

        /// <summary>自检用：把某个阵营的「已领格数」直接置成 <paramref name="level"/>。
        /// <para>🔴 **2026-10-13（`A1088`）新增** —— 防御池那道 `forgeLevel` 闸的**右值**就是这个数
        /// （原版 `GetEnvCardList(…, currentForgeLevelObtained)` → `ForgePersistence.rewardsCollected`），
        /// 而初值里**没有 20 / 40**（见 `Init`）⇒ 不置一下，闸的边界**测不出来**，
        /// 面板那条路的接线（面板到底用没用闸过那份）也**没法判别**。</para>
        /// <para>⚠️ <see cref="ForgeData"/> 是**全局静态** ⇒ 用完**必须还原**（`SetLevelForTest(army, 原值)`
        /// 或 <see cref="ResetForTest"/>），否则会漏到后面的自检格。</para></summary>
        public static void SetLevelForTest(string army, int level)
        {
            if (string.IsNullOrEmpty(army)) return;
            if (level < 0) level = 0;
            if (level > MaxLevel) level = MaxLevel;
            _level[army] = level;
        }

        /// <summary>累计经验（原版 `currentPoints`）。</summary>
        public static int PointsOf(string army) { int v; return _points.TryGetValue(army, out v) ? v : 0; }

        /// <summary>第 `level` 格（0 基）的状态 —— **逐句照原版**
        /// `ForgeRewardItemContainer__GetLevelState.c`（签名 = `dump.cs:68144`：
        /// `GetLevelState(int previousNeededPoints, int currentPoints, int currentLevelPointsToLevelUp,
        /// int numberOfCollectedRewards, int currentRewardIndex)`）：
        /// <code>
        /// if (collected >= index + 1)                              return Collected;                            // 4
        /// if (previousNeeded &lt;= points) {
        ///     if (thisLevelNeeded &lt;= points) return (collected != index) ? ToCollectButFirstRedeemThePrevious : ToCollect;  // 3 / 2
        ///     return InProgress;                                                                                    // 1
        /// }
        /// return Locked;                                                                                            // 0
        /// </code>
        /// 🔴 **2026-10-27（B88-E7 · 铁律 5）**：原来这一格只有**四态**，把「经验够到某格、但前面还有没领的」
        /// 那一档当成 `Locked` —— 原版那一档是 **`3 = ToCollectButFirstRedeemThePrevious`**（真差异）。</summary>
        public static int StateAt(string army, int level)
        {
            int collected = LevelOf(army), points = PointsOf(army);
            int prev = level <= 0 ? 0 : NeededAt(level - 1);          // `previousNeededPoints`
            if (level + 1 <= collected) return Collected;
            if (prev <= points)
            {
                if (NeededAt(level) <= points)
                    return collected != level ? ToCollectButFirstRedeemThePrevious : ToCollect;
                return InProgress;
            }
            return Locked;
        }

        /// <summary>五态（**值照原版** `ForgeRewardItemContainer.LevelState`，`dump.cs:68096-68104`）。</summary>
        public const int Locked = 0, InProgress = 1, ToCollect = 2, ToCollectButFirstRedeemThePrevious = 3, Collected = 4;

        /// <summary>现在有没有「可领」的格子（原版 `ForgeRewardSelector.hasToCollectReward`）——
        /// **`Ready for level up` 那团光效的判据就是它**（出处 `ForgeRewardSelector__CreateRewards.c:83-87`
        /// + `__FinishedSnapping.c:5`）。</summary>
        public static bool HasToCollect(string army)
        {
            int lv = LevelOf(army);
            return lv < MaxLevel && PointsOf(army) >= NeededAt(lv);
        }

        /// <summary>领第 `level` 格。**照原版语义：只把 `rewardsCollected` 加一，不扣经验**。
        /// 返回「真的领到了吗」（领不到时**出声**，不静默）。</summary>
        public static bool Claim(string army, int level, out string why)
        {
            why = null;
            if (level < 0 || level >= MaxLevel) { why = "格号越界"; return false; }
            int st = StateAt(army, level);
            if (st == Collected) { why = "这一格已经领过了"; return false; }
            if (st == Locked) { why = "前面还有没领的格"; return false; }
            if (st == InProgress) { why = "经验还不够"; return false; }
            _level[army] = level + 1;
            RaiseRewardsCollected(army);   // = 原版 `Signal.Raise(UpgradeForgeSignal)`（见 `RewardsCollected` 的 doc）
            return true;
        }

        /// <summary>升级文案。**照原版**：`"{0} {1}/{2}"` 套本地化键 `"MainMenu/Level"`（我们这里直接用英文词 `Level`，
        /// 原版那句是本地化词条）⇒ `Level 3/50`。</summary>
        public static string LevelText(string army) { return "Level " + LevelOf(army) + "/" + MaxLevel; }

        /// <summary>阵营 → **锻造点图标**（原版 `ForgePointIconDrawer.DrawForArmy` 换的那张）。
        /// 🔴 切片名里有**空格**，导入器把空格换成下划线 ⇒ 这里给的已经是**工程里的文件名**。
        /// ⚠️ 与 `DeckRuntime.FactionIcon`（**阵营徽记**，另一族图）**不是一回事** —— 那个是 `40k_DeckSelection_icon_*`。
        /// 两族图各自的映射**各留一份**，别互相抄。</summary>
        public static string ForgePointIcon(string army)
        {
            // 🔴 **2026-10-28（B88_B2）：收口到 `Core/ArmyIcons.ForgeIconName`** ——
            //   原来这里内联了一张 13 条的 `case` 表，与 `Core/ArmyIcons.cs` 的 `ForgeTable` **是同一份映射**
            //   （两张表逐条相同：`Ultramarines→…_Ultramarines` … `SpaceWolves→…_SpaceWolves`）。
            //   铁律「两处写同一条规则 = 迟早不一致」＋用户 2026-10-28 点名 ⇒ **只留 `ArmyIcons` 那一份**。
            //
            // 🔴 **同时删掉原来那条兜底** `default: return "40K_general_icon_Forge_points";`
            //   （= 不带阵营后缀的那张通用图标）：
            //   原版 `ArmyUtilities.GetForgeIconForArmy` **查不到就返回 null sprite**（判据 → `ArmyIcons.ForgeIconName`
            //   的 doc：`ArmyIconsSO.TryGetItem` 没命中 ⇒ null），**没有**「退回通用图标」这一档；
            //   那条兜底是**我们自己加的**，而且当时**没打 `🔶 自己设计`** ⇒ 按「一切照原版复刻」的口径删掉。
            //   ⇒ 认不出的阵营现在返回 **null**：调用方（`ForgeTab` 的 `Points/Army` 那一格）按「取不到图就不画」处理
            //     —— 原版那一档也是「没有图」，⛔ 不拿别的阵营的图标顶替（同 `ArmyIcons` 里那几条 doc 的口径）。
            //
            // ⚠️ `Neutral(0)` / 空串 / 未知名 ⇒ 一律 `null`（原版同形）；本方法的 13 个生产调用点传的都是
            //   `ForgeData.Armies` 里的合法阵营名 ⇒ 生产路径**逐像素不变**（13 条映射与旧表逐条相同）。
            //   ⚠️ 行号纪律（§18·b）：本块与旧块**同为 19 行** —— `AddPoints` 的账落点 `:300` 才不漂。
            return ArmyIcons.ForgeIconName(army);
        }

        /// <summary>一格的经验文案。**照原版** `"{currentPoints}/{neededPoints}"`（`ForgeRewardItemContainer__Initialize.c:200-202`）。</summary>
        public static string PointsText(string army, int level) { return PointsOf(army) + "/" + NeededAt(level); }

        /// <summary>格内进度条比例。**照原版公式**（`__Initialize.c:213-215`）：
        /// `(currentPoints − 上一格 Points) / (本格 Points − 上一格 Points)`，夹到 0..1。</summary>
        public static float Bar01(string army, int level)
        {
            float lo = level == 0 ? 0f : NeededAt(level - 1);
            float hi = NeededAt(level);
            if (hi - lo <= 0f) return 1f;
            float v = (PointsOf(army) - lo) / (hi - lo);
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        /// <summary>自检用：把状态推回初值（自检之间互不影响）。</summary>
        public static void ResetForTest()
        {
            _level.Clear(); _points.Clear();
            for (int i = 0; i < Armies.Length; i++) Init(Armies[i], i);
            Selected = Armies[1];
        }

        // ============================================================ 🆕 2026-10-27（B88-E7）
        /// <summary>**一次领掉全部可领的格子** —— 原版 `Forge.CollectRewards` 的等价物
        /// （`out_cpp2il_dev_ilspy/Everguild/LiveOps/Forge.cs:54-61`：
        ///  `GetRewardsToCollect() = rewards.Skip(rewardsCollected).Where(r =&gt; currentPoints &gt;= r.Points)`
        ///  ⇒ **经验够几档就一次领几档**，⛔ 不是「一次只领一格」）。
        /// <para>我们这一版每格的门槛是**累计值**（`NeededAt`，判据见文件头），而 `rewardsCollected` 就是
        /// <see cref="LevelOf"/> ⇒ 那条 `Skip(collected).Where(points &gt;= 本格 Points)` 等价于
        /// 「从 `collected` 起，只要够门槛就连续领」—— 逐格调 <see cref="Claim"/> 走同一个状态闸（不另写一套）。</para>
        /// <para>返回**真的领到几格**；一格都领不到时 <paramref name="why"/> 说明原因（⛔ 不静默）。</para></summary>
        public static int ClaimAll(string army, out string why)
        {
            why = null;
            int n = 0;
            while (true)
            {
                int lv = LevelOf(army);
                if (lv >= MaxLevel) { why = "已经满级"; break; }
                if (PointsOf(army) < NeededAt(lv)) { why = "经验还不够下一格"; break; }
                string cellWhy;
                if (!Claim(army, lv, out cellWhy)) { why = cellWhy; break; }
                n++;
            }
            if (n == 0 && why == null) why = "没有可领的格";
            return n;
        }

        /// <summary>**领奖成功的信号** —— 原版 `UpgradeForgeSignal` 的等价物：
        /// `Forge.CollectRewards` 的成功回调 `Signal.Raise(new UpgradeForgeSignal { army })`
        /// （`Everguild/LiveOps/Forge.cs:121-122`，全树唯一 Raise 点），消费者 = `UpgradeForgeLevel.OnTrigger`
        /// （`MissionChallenge.UpdateProgress(this, 1, false)` ⇒ **成就「锻造升级」进度 +1**）。
        /// <para>⚠️ **消费者还没接**：`UpgradeForgeLevel` 这个组件在本地 build 里**0 实例**
        /// （`grep -rl UpgradeForgeLevel assets_full` 只命中 `MonoScript_4027.json`），
        /// 102 条成就里也**没有锻造相关的那条**（判据 → `Resources/profile_cosmetics.json` 的 `c` 列全表）
        /// ⇒ 拿不到 `Ach_&lt;锻造升级&gt;` 的 id 与阈值 ⇒ **不编一个 id 去写进度**（铁律 2）。
        /// 接线条件：拿到那条成就的 id 后，在这里挂一个 `AddAchievementValue(id, 1)` 即可。</para></summary>
        public static event System.Action<string> RewardsCollected;
        static void RaiseRewardsCollected(string army)
        {
            var h = RewardsCollected;
            if (h != null) h(army);
        }

        /// <summary>战斗/任务给锻造点（原版 `ForgeHandler.CollectPoints(Points point, int amount)`，
        /// `Everguild/LiveOps/ForgeHandler.cs:324` —— 原版是**服务端**加点）。
        /// 🔴 **2026-10-27（B88-E7）**：本仓原来**没有生产侧的加点入口**（只有 `SetLevelForTest` 改数）
        /// ⇒ 补这一格。⚠️ **生产调用点还欠**：`BattleDriver` 的结算那一拍（本波那文件有别的写手，
        /// 见 `资料/完全复刻/调度台日志.md` 的文件所有权表）⇒ 现在**谁都不会调它**，如实记在这里。</summary>
        public static void AddPoints(string army, int amount)
        {
            if (string.IsNullOrEmpty(army) || amount == 0) return;
            int cur = PointsOf(army);
            _points[army] = System.Math.Max(0, cur + amount);   // 原版是累计加，不封顶（封的是显示档）
            // 🆕 2026-10-28（B92_N3）：原版 `CollectPoints` 的**第二半**（`out_cpp2il_dev_ilspy/Everguild.LiveOps/ForgeHandler.cs:324` 现读）：
            //   `if (forge.GetRewardsToCollect().Any()) UiBadgeNotificationManager.Refresh<ForgeBadge>();`
            //   ⇒ 加完之后**如果变成可领**，就刷一次锻造红点。我们的 `HasToCollect` = `GetRewardsToCollect().Any()`。
            if (HasToCollect(army)) UiBadgeNotificationManager.Refresh(typeof(ForgeBadge));
        }
    }
}
