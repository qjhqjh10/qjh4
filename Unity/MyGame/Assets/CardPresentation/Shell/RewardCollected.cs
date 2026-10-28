// RewardCollected.cs — 原版 `RewardCollected : MissionChallenge`（「领过 N 条奖励」这类任务挑战）
// 🆕 2026-10-28 · 执行代理 `B91_W7` · 账 `日常_奖励` 653 / 654 / 655 / 656 / 657 / 658 / 659
//   （G1 奖励事件层，裁定 1：按原版类型名独立成文件，类名 == 文件名）
//
// ── 判据（逐条现读）──────────────────────────────────────────────────────────
//   · 字段：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/RewardCollected.cs`
//       `anyItemType(0x58)` · `anyArmy(0x59)` · `ignoreDuplicates(0x5a)` · `checkAlreadyCollected(0x5b)`
//       `List<TypeReference> itemType(0x60)` · `CardArmy army(0x68)`（偏移取自 `__OnInitialize.c` 的实读）
//   · `OnInitialize()` = `decomp_full/RewardCollected__OnInitialize.c` ＋（结构更可读的）
//       `cpp2il/out_cpp2il_dev_ilspy/RewardCollected.cs:75-230`：
//       <code>
//       Signal.Register&lt;CollectRewardsSignal&gt;(OnTrigger);          // ★ 无条件，先注册
//       if (anyItemType || !checkAlreadyCollected) return;              // ★ 唯一早退条件
//       total = 0;
//       foreach (t in itemType) total += Sum(GetInventory(t).items, x =&gt; 谓词(x), 选择器);
//       if (total &gt; 0) UpdateProgress(total, shouldOverride: true);      // ★ 「已经领过的」直接盖上去
//       </code>
//       谓词（b__6_0）= `anyArmy ? true : ((PlayerItem)x).army == this.army`；
//       选择器（b__6_1）= `ignoreDuplicates ? 1 : <该条目数量>`。
//   · `OnReset()` = `decomp_full/RewardCollected__OnReset.c` = **全部内容一句**
//       `Signal.Unregister&lt;CollectRewardsSignal&gt;(OnTrigger)`。
//       🔴 **账 654 的原判词「缺 Reset(清已收集标记)」是错的** —— `.c` 里只有反注册，没有任何清标记动作（见下方差异列）。
//   · `OnTrigger(CollectRewardsSignal obj)` = `decomp_full/RewardCollected__OnTrigger.c`：
//       <code>
//       list = new List&lt;RewardInfo&gt;(); list.AddRange(obj.Items);
//       list = list.Where(b__8_0);                   // b__8_0 = 「背包里**还没有**这件物品」
//       if (ignoreDuplicates) list = list.Where(b__8_1);   // 同一件只算一次
//       sum = list.Sum(b__8_2);                      // b__8_2 = 数量
//       if (sum != 0) UpdateProgress(sum, shouldOverride: false);
//       </code>
//       ⚠️ 另外 `.c` 里那段 `PrebuiltDeck` 展开（`b__8_3/b__8_4`：把卡组奖励拆成「每张卡 + 卡背 + 督军」）
//          **本机没有 PrebuiltDeck 奖励条目**（`RewardSpec.Id` 全指向卡/货币）⇒ 未搬，出声一次（见下）。
//
// ── 与我们的架构的对接 ─────────────────────────────────────────────────────────
//   · `Signal`（`Scaffold.Core.Events` 那一层全局总线）本工程没有；等价物 = `Shell/RaidEvent.cs` 的
//     `CollectRewardsSignal` + `RaidSignals`（🔶 自己设计（非原版）：调度台 2026-10-27 授权按本地等价建）。
//     ⛔ 别为奖励层再造第二条总线（两处写同一条规则 = 迟早不一致）。
//   · `MissionChallenge.UpdateProgress(value, shouldOverride, …)` 本工程没有（挑战引擎在 `Shell/ChallengeEngine.cs`，
//     只到「计数口」那一层）⇒ 这里落成本地 `Progress` + 一个可注入出口 <see cref="ProgressSink"/>。
//     🔶 自己设计（非原版）：为什么必须自己定 —— `MissionChallenge` 基类（任务进度/目标/领取）本机没建 ·
//     调度台 2026-10-28 定。
//   · `List<TypeReference>`：本工程没有 `TypeReferences` 包 ⇒ 用**类型全名**字符串（`Type.GetType` 走本程序集）。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RewardCollected`（「共领到 N 条奖励」这类任务挑战）的本地等价物。
    /// <para>⚠️ 它只做**挑战进度那一半**：`Signal` 注册 / 反注册 + 计数 + 推进进度。
    /// 任务条目的展示、目标值、领奖都在别处（`Shell/DailyData.cs` / `Shell/MissionsTab.cs`）。</para></summary>
    [Serializable]
    public class RewardCollected
    {
        /// <summary>原版 `bool anyItemType`（`+0x58`，缺省 **true**）：物品类型**不限**（= 不看 `itemType` 表）。</summary>
        public bool anyItemType = true;
        /// <summary>原版 `bool anyArmy`（`+0x59`）：阵营不限（= 谓词恒真）。</summary>
        public bool anyArmy;
        /// <summary>原版 `bool ignoreDuplicates`（`+0x5a`）：同一件物品**只算一次**（`OnTrigger` 的第二道 Where）。</summary>
        public bool ignoreDuplicates;
        /// <summary>原版 `bool checkAlreadyCollected`（`+0x5b`）：初始化时把**已经拥有的**也计入（`OnInitialize` 那一段）。</summary>
        public bool checkAlreadyCollected;
        /// <summary>原版 `List&lt;TypeReference&gt; itemType`（`+0x60`）。本工程用**物品类型全名**（见文件头）。</summary>
        public List<string> itemType = new List<string>();
        /// <summary>原版 `CardArmy army`（`+0x68`）：`anyArmy == false` 时只数这个阵营的。</summary>
        public CardArmy army;

        // ============================================================ 进度（本地等价，见文件头）
        /// <summary>当前进度。原版这个量在 `MissionChallenge` 基类里（`ShowProgress`/`GetTarget` 一族）。</summary>
        public int Progress { get; private set; }
        /// <summary>进度出口（(挑战, 新进度, 是否覆盖写入)）。没接 ⇒ 只记本地 + 出声一次。</summary>
        public static Action<RewardCollected, int, bool> ProgressSink;

        /// <summary>原版 `MissionChallenge.UpdateProgress(int value, bool shouldOverride, …)` 的本地等价：
        /// `shouldOverride` ⇒ 直接盖成 `value`；否则累加。负数夹到 0（原版 `QuantityBound` 同口径）。</summary>
        public void UpdateProgress(int value, bool shouldOverride)
        {
            int next = shouldOverride ? value : Progress + value;
            if (next < 0) next = 0;
            Progress = next;
            if (ProgressSink != null) { ProgressSink(this, next, shouldOverride); return; }
            WarnNoSinkOnce();
            Debug.Log("[RewardCollected] 进度 → " + next + "（" + (shouldOverride ? "覆盖" : "累加 " + value) + "）");
        }

        // ============================================================ 三条生命周期（原版逐句）
        bool _subscribed;

        /// <summary>原版 `protected override void OnInitialize()`（判据见文件头）。</summary>
        public void OnInitialize()
        {
            if (!_subscribed) { RaidSignals.CollectRewards += OnTrigger; _subscribed = true; }

            // ★ 唯一早退条件（原版就是这一句，⛔ 别加别的闸）
            if (anyItemType || !checkAlreadyCollected) return;

            int total = 0;
            for (int i = 0; i < itemType.Count; i++)
            {
                string typeName = itemType[i];
                total += SumOwnedOfType(typeName);
            }
            if (total > 0) UpdateProgress(total, true);                     // shouldOverride: true
        }

        /// <summary>原版 `protected override void OnReset()`：**只有一句** `Signal.Unregister&lt;CollectRewardsSignal&gt;(OnTrigger)`。</summary>
        public void OnReset()
        {
            if (!_subscribed) return;
            RaidSignals.CollectRewards -= OnTrigger;
            _subscribed = false;
        }

        /// <summary>原版 `private void OnTrigger(CollectRewardsSignal obj)`（判据见文件头）。</summary>
        public void OnTrigger(CollectRewardsSignal obj)
        {
            if (obj == null) { Debug.LogWarning("[RewardCollected] OnTrigger(null) ⇒ 不推进（原版这一档 NRE）"); return; }

            var list = new List<CampaignData.RewardSpec>();
            if (obj.Items != null) list.AddRange(obj.Items);
            ExpandPrebuiltDecks(list);                                      // b__8_3 / b__8_4（B93_D4 2026-10-28 补）

            // b__8_0：`anyItemType` 短路真；否则 `itemType.Contains(item.GetType())`；再 `anyArmy` 短路真；否则 `item.cardArmy == army`
            //   🔴 **2026-10-28（B93_D4）就地订正**：旧版把这一道写成「背包里还没有的」—— **错**（那是 b__8_1）。
            var filtered = new List<CampaignData.RewardSpec>();
            for (int i = 0; i < list.Count; i++)
                if (PassesItemTypeAndArmy(list[i])) filtered.Add(list[i]);

            // b__8_1：**只挂一道、且只在 `ignoreDuplicates` 时挂**，判据 = `!InventoryManager.HasItem(i.Item)`
            //   🔴 **同上就地订正**：旧版把它当「无条件过滤」、又把 `ignoreDuplicates` 做成「按 id 去重」—— 两句都反了。
            if (ignoreDuplicates) filtered.RemoveAll(r => !IsNotOwnedYet(r.Id));

            // b__8_2：`ignoreDuplicates ? 1 : x.Quantity`（判据 = `RewardCollected__<OnTrigger>b__8_2.c`：
            //   `if (*(char *)(param_1 + 0x5a) != '\0') return 1;` 否则走 `RewardInfo` 虚槽 `+0x218` = `Quantity`）
            int sum = 0;
            for (int i = 0; i < filtered.Count; i++) sum += ignoreDuplicates ? 1 : filtered[i].Quantity;

            if (sum != 0) UpdateProgress(sum, false);                       // shouldOverride: false
        }

        // ============================================================ 三个谓词 / 一个求和
        //   原版：b__6_0 / b__6_1（`OnInitialize` 那一路）＋ b__8_0 / b__8_1 / b__8_2（`OnTrigger` 那一路）
        //   🔴 2026-10-28（B93_D4）：旧注释把下面这个谓词记成 `b__8_0` —— 它是 **`b__8_1`**（`b__8_0` = itemType/army 那道，
        //   现由 `PassesItemTypeAndArmy` 承担）。判据 = `RewardCollected.__c___OnTrigger_b__8_1.c`；
        //   同一份判据还说明它**只在 `ignoreDuplicates` 为真时**才挂进 `Where`（`OnTrigger` 里现读的次序）。
        /// <summary>原版 `_003COnTrigger_003Eb__8_1`：`!InventoryManager.HasItem(i.Item)`（「背包里还没有」）。</summary>
        protected virtual bool IsNotOwnedYet(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return true;
            // 本工程没有「按 id 查任意类型背包」的统一口 ⇒ 走 ICurrency 这一路（有 id 语义的那一份）。
            var cur = InventoryManager.GetInventory<ICurrency>();
            if (cur == null) return true;
            var info = cur.Get(itemId);
            return info == null || info.Amount <= 0;
        }

        /// <summary>原版 `_003COnInitialize_003Eb__6_1` 的求和 (= `Sum(选择器)`，选择器 = `ignoreDuplicates ? 1 : 数量`)，
        /// 外加 b__6_0 的阵营谓词。见文件头「与我们的架构的对接」：这里按类型名走反射读 `GetItems()`。</summary>
        protected virtual int SumOwnedOfType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return 0;
            Type t = ResolveType(typeName);
            if (t == null)
            {
                Debug.LogWarning("[RewardCollected] itemType 里的类型名 '" + typeName + "' 在本程序集解析不到 ⇒ 这一类跳过");
                return 0;
            }
            object inv = InventoryManager.GetInventory(t);
            if (inv == null) return 0;                                     // 没登记这份背包 ⇒ 计 0（InventoryManager 已出声）

            var getItems = inv.GetType().GetMethod("GetItems", Type.EmptyTypes);
            if (getItems == null) return 0;
            var items = getItems.Invoke(inv, null) as IEnumerable;
            if (items == null) return 0;

            int total = 0;
            foreach (var info in items)
            {
                if (info == null) continue;
                var itemProp = info.GetType().GetProperty("Item");
                var amountProp = info.GetType().GetProperty("Amount");
                object item = itemProp != null ? itemProp.GetValue(info) : null;
                if (!ArmyPasses(item)) continue;                            // b__6_0
                if (ignoreDuplicates) total += 1;                           // 选择器第一支
                else total += amountProp != null ? ToInt(amountProp.GetValue(info)) : 0;
            }
            return total;
        }

        /// <summary>原版 `_003COnInitialize_003Eb__6_0`：`anyArmy ? true : ((PlayerItem)x).army == this.army`。
        /// ⚠️ 本工程物品侧没有统一的 `army` 成员 ⇒ 反射读 `army` 字段/属性；读不到时**出声一次并放行**
        /// （⛔ 不静默把整类排除，也不静默少算）。</summary>
        protected virtual bool ArmyPasses(object item)
        {
            if (anyArmy) return true;
            if (item == null) return false;
            var ty = item.GetType();
            var f = ty.GetField("army", BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
            {
                object v = f.GetValue(item);
                if (v is CardArmy) return (CardArmy)v == army;
            }
            var p = ty.GetProperty("army", BindingFlags.Public | BindingFlags.Instance);
            if (p != null)
            {
                object v = p.GetValue(item, null);
                if (v is CardArmy) return (CardArmy)v == army;
            }
            WarnNoArmyOnce(ty);
            return true;
        }

        static int ToInt(object o)
        {
            if (o is int) return (int)o;
            if (o == null) return 0;
            try { return Convert.ToInt32(o); } catch { return 0; }
        }

        /// <summary>类型名解析（本程序集优先；再不行走 `Type.GetType`）。</summary>
        static Type ResolveType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = Type.GetType(name);
            if (t != null) return t;
            return typeof(RewardCollected).Assembly.GetType(name);
        }

        // ============================================================ 一次性出声
        static bool _warnedSink;
        static readonly HashSet<string> _warnedNoArmy = new HashSet<string>();
        void WarnNoSinkOnce()
        {
            if (_warnedSink) return; _warnedSink = true;
            Debug.Log("[RewardCollected] 没有接 `ProgressSink` ⇒ 进度只存在本对象上（原版由 `MissionChallenge` 基类持有）");
        }
        /// <summary>原版 `OnTrigger` 里那段 **PrebuiltDeck 展开**（`b__8_3` / `b__8_4`）——
        /// 判据 = `decomp_full/RewardCollected__OnTrigger.c` 的 `LAB_1806aae19` 那一段 + `.c` 尾三跳：
        /// <code>
        /// foreach (RewardInfo r in obj.Items)
        ///   if (r.Item is PrebuiltDeck deck) {
        ///       list.AddRange(deck.GetCardLibrary()
        ///                        .GroupBy(b__8_3: x =&gt; x.uniqueId)
        ///                        .Select (b__8_4: g =&gt; new RewardInfo(g.Key, g.Count(), RewardTier.Basic, null, null)));
        ///       list.Add(new RewardInfo(deck.GetDeckCardback(), 1, Basic, null, null));
        ///       list.Add(new RewardInfo(deck.DeckHero,         1, Basic, null, null));
        ///   }
        /// </code>
        /// ⚠️ 原版靠 **`r.Item is PrebuiltDeck`** 判类型；我们的 `RewardSpec` 只有一个 id ⇒ 用两条判据认：
        /// ① `ItemDrawer.Spec(id).Type` 含 `PrebuiltDeck`（`IdTypes` 真表）· ② `PrebuiltDecks.ById(id) != null`。
        /// 🔴 **2026-10-28（B93_D4）**：原来这里只有一句 `WarnPrebuiltDeckOnce` 出声「未搬」—— **已搬完**，出声删掉。</summary>
        void ExpandPrebuiltDecks(List<CampaignData.RewardSpec> list)
        {
            if (list == null || list.Count == 0) return;
            int n = list.Count;                                   // 只展开**传进来的那些**（原版遍历的就是 `obj.Items`）
            for (int i = 0; i < n; i++)
            {
                var deck = DeckOf(list[i].Id);
                if (deck == null) continue;
                // `GroupBy(uniqueId).Select(g => new RewardInfo(key, g.Count(), Basic))`
                if (deck.cardIds != null)
                {
                    var seen = new Dictionary<string, int>(StringComparer.Ordinal);
                    var order = new List<string>();
                    for (int c = 0; c < deck.cardIds.Length; c++)
                    {
                        string id = deck.cardIds[c];
                        if (string.IsNullOrEmpty(id)) continue;
                        int k;
                        if (!seen.TryGetValue(id, out k)) { seen[id] = 1; order.Add(id); }
                        else seen[id] = k + 1;
                    }
                    for (int c = 0; c < order.Count; c++)
                        list.Add(new CampaignData.RewardSpec(order[c], seen[order[c]], CampaignData.TierBasic));
                }
                if (!string.IsNullOrEmpty(deck.cardback))
                    list.Add(new CampaignData.RewardSpec(deck.cardback, 1, CampaignData.TierBasic));
                if (!string.IsNullOrEmpty(deck.heroId))
                    list.Add(new CampaignData.RewardSpec(deck.heroId, 1, CampaignData.TierBasic));
            }
        }

        /// <summary>这一条奖励的 item 是不是一副预组卡组（判据见 <see cref="ExpandPrebuiltDecks"/>）。
        /// 认不出 ⇒ `null`（⛔ 不当成卡组）。</summary>
        static PrebuiltDecks.Deck DeckOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string type = ItemDrawer.Spec(id, null, null).Type;
            bool byType = !string.IsNullOrEmpty(type) && type.IndexOf("PrebuiltDeck", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!byType && !PrebuiltDecks.Available) return null;
            return PrebuiltDecks.ById(id);                        // 两条判据任一命中 ⇒ 再让 `ById` 确认真有这副牌
        }

        /// <summary>原版 `_003COnTrigger_003Eb__8_0`（`RewardCollected.cs:599` 一带）：
        /// `anyItemType` 短路真 → 否则 `itemType.Contains(item.GetType())` → 再 `anyArmy` 短路真 → 否则 `((PlayerItem)item).cardArmy == army`。
        /// <para>我们的 `itemType` 存的是**物品类型全名**（见文件头）；`RewardSpec` 没有类型 ⇒ 用 `ItemDrawer.Spec(id).Type`
        /// （= `IdTypes` 真表里的原版 `ObtainableItem` 子类名）当 `item.GetType()` 的等价物。</para>
        /// <para>⚠️ 判不出类型/阵营时**出声一次并放行**（同 <see cref="ArmyPasses"/> 的口径：⛔ 不静默少算）。</para></summary>
        protected virtual bool PassesItemTypeAndArmy(CampaignData.RewardSpec r)
        {
            var spec = ItemDrawer.Spec(r.Id, null, null);
            if (!anyItemType)
            {
                if (string.IsNullOrEmpty(spec.Type))
                {
                    WarnNoTypeOnce(r.Id);
                }
                else
                {
                    bool hit = false;
                    for (int i = 0; i < itemType.Count; i++)
                        if (string.Equals(itemType[i], spec.Type, StringComparison.Ordinal)) { hit = true; break; }
                    if (!hit) return false;                       // 类型不在白名单 ⇒ 这一条不算
                }
            }
            if (anyArmy) return true;
            if (spec.CardArmy <= 0) { WarnNoArmyOnce(typeof(CampaignData.RewardSpec)); return true; }
            return spec.CardArmy == (int)army;
        }

        static readonly HashSet<string> _warnedNoType = new HashSet<string>();
        static void WarnNoTypeOnce(string id)
        {
            if (!_warnedNoType.Add(id ?? "")) return;
            Debug.LogWarning("[RewardCollected] `itemType` 白名单在用，但奖励 '" + id + "' 的 item 类型在本机判不出来"
                           + "（`ItemDrawer.IdTypes` 没这一行）⇒ 这一条**放行**（⛔ 不是判它有效，是判不了）");
        }

        void WarnNoArmyOnce(Type t)
        {
            string k = t != null ? t.Name : "?";
            if (!_warnedNoArmy.Add(k)) return;
            Debug.LogWarning("[RewardCollected] 物品类型 '" + k + "' 上没有 `army` 字段/属性 ⇒ "
                           + "`anyArmy == false` 的阵营过滤对它**放行**（⛔ 不是判它有效，是判不了）");
        }

        /// <summary>自检收尾。</summary>
        public static void ResetForTest() { ProgressSink = null; _warnedSink = false; _warnedNoArmy.Clear(); _warnedNoType.Clear(); }
    }
}
