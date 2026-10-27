// Points.cs —— 原版 `Points` / `Points<T>`（活动积分 · 赛季积分）＋ `IPointReceiver`（账 `未分类_其他:490/491`，B26）
//
// ============================ 这是什么 / 不是什么 ============================
// 原版三件（判据 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Points.cs` 真体 + `d:/2/tools/il2cpp_out/dump.cs:19104-19198`）：
//   · `abstract class Points : ObtainableItem` —— 只有两样：`RewardType => "points"`（常量，真身 `decomp_full/Points__get_RewardType.c`）
//     ＋ `public abstract void Collect(int amount)`。
//   · `abstract class Points<T> : Points where T : IPointReceiver, ILiveOpsHandler` —— `pointNameKey`(LocalizedString) /
//     `protected virtual string Id` / `private string Type` / `LocalizedName` / `Collect` / `GetID`。
//   · `interface IPointReceiver { void CollectPoints(Points point, int amount); }`（`dump.cs:19198-19203`）。
//
// 🔴 **两个方法体是 VA 反汇编读出来的**（泛型类的方法体从没被导出过 —— 这是 CLAUDE.md 记的 G6 那一类，`dump.cs` 里 `// RVA: -1`
//    但 `GenericInstMethod` 块给了真地址）：
//   · `Points<object>.get_Id` @ **VA 0x18112CF60**：`typeof(T).Name` → `String.Replace("Handler","")` → `+ " Points " + ` `(枚举 [this+0x2C]).ToString()`
//     （`[this+0x2C]` = `ObtainableItem.cardArmy`；`CampaignPoints.get_Type` 是真体 `return 0x47`(=`campaignPoints`) 的**常量**，不是字段读 ⇒ 枚举确实是 `cardArmy`）。
//   · `Points<object>.Collect` @ **VA 0x18112CD30**：`LiveOpsManager.GetHandler<T>()`（@0x180D13600）→ 接口查 `IPointReceiver`
//     → `CollectPoints(this, amount)`（尾部 `jmp [rax]`，`rcx`=handler / `rdx`=this / `r8d`=amount）。
//
// 🔶 **自己设计（非原版）两处**（调度台 2026-10-27 裁定，B26）：
//   ① **`Id` 里那个枚举在本地是「阵营名字串」**（`Army`）—— 原版是 `CardArmy` 枚举；本项目阵营身份一律用名字串
//      （与 `ForgeBadge.army` / `RewardsBadge.parentId` 同一套先例），语义一致、表示法不同。
//   ② **`PointsCatalog` 是名册** —— 原版 `ToCurrency(campaignPoints)` 走 `ConfigManager.GetConfig<PointsConfig>().points`；
//      我们没有 ConfigManager（同 `CurrencyCatalog`）。B12/B25 接入真配置后把 `Register` 换成从 SO 播种即可。
//
// ⚠️ **不建的东西**：具体子类（原版 `CampaignPoints : Points<CampaignHandler>` / `ForgePoints : Points<ForgeHandler>`）——
//    它们要 `CampaignHandler` / `ForgeHandler` 那两件（归 B09/B12），本批不越界；`Points<T>` 的约束已经就位，谁做 handler 谁继承。
using System;
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>原版 `IPointReceiver`（`il2cpp_out/dump.cs:19198-19203`，唯一成员）。</summary>
    public interface IPointReceiver
    {
        void CollectPoints(Points point, int amount);
    }

    /// <summary>原版 `abstract class Points : ObtainableItem` —— 积分项的**非泛型脸**。
    /// <para>`RewardType` 是常量 `"points"`（`Points__get_RewardType.c` 只有一句返回）。</para>
    /// <para>⚠️ 原版还有一个 `Points` 没声明、但 `Points&lt;T&gt;` 用到的字段 `cardArmy`（在 `ObtainableItem` 上，`+0x2C`）——
    /// 我们落在本类的 <see cref="Army"/>（原因见文件头 🔶 ①）。</para></summary>
    public abstract class Points
    {
        /// <summary>这一份积分属于哪个阵营（原版 `ObtainableItem.cardArmy`；空 = 中立那份，`ToCurrency(campaignPoints)` 找的就是它）。</summary>
        public string Army;

        /// <summary>原版 `Points.RewardType` = `"points"`（常量）。</summary>
        public virtual string RewardType { get { return "points"; } }

        /// <summary>原版 `Points.Collect(int)`（抽象）：把 amount 记到**接收方**账上。</summary>
        public abstract void Collect(int amount);

        /// <summary>原版 `IdentifiableSO.GetID()`（`GetID` 在 `Points&lt;T&gt;` 里 override 成 `Id`）。</summary>
        public abstract string GetID();

        public override string ToString() { return GetID() + " (" + Army + ")"; }
    }

    /// <summary>原版 `abstract class Points&lt;T&gt; where T : IPointReceiver, ILiveOpsHandler`。
    /// <para>`Id` / `Collect` 的判据 = 文件头那两条 VA 反汇编（泛型体从没被导出）。</para></summary>
    public abstract class Points<T> : Points where T : class, IPointReceiver, ILiveOpsHandler
    {
        /// <summary>原版 `[SerializeField] private LocalizedString pointNameKey`（`+0x00`）。我们存**键**，取词走 `Loc.T`。</summary>
        public string PointNameKey;

        /// <summary>原版 `Points&lt;T&gt;.Id`（**liveops 存储键**）：`typeof(T).Name.Replace("Handler","") + " Points " + &lt;阵营枚举&gt;`。
        /// 例：`T = CampaignHandler` + 中立 ⇒ `"Campaign Points "`（尾部为空串，与原文逐字一致 —— 原版那句就是在拼一个空枚举名）。</summary>
        protected virtual string Id
        {
            get
            {
                string n = typeof(T).Name.Replace("Handler", "");
                return n + " Points " + (Army ?? "");
            }
        }

        /// <summary>原版 `Points&lt;T&gt;.LocalizedName` = `pointNameKey` 的本地化串。</summary>
        public virtual string LocalizedName { get { return Loc.T(PointNameKey); } }

        /// <summary>原版 `Points&lt;T&gt;.GetID()` = `Id`（VA 0x18112CE30 只有一句虚调用 `get_Id`）。</summary>
        public override string GetID() { return Id; }

        /// <summary>原版 `Points&lt;T&gt;.Collect(int)`：`LiveOpsManager.GetHandler&lt;T&gt;().CollectPoints(this, amount)`。
        /// ⚠️ 原版 handler 为 null 时**抛 NRE**（`Collect` 尾部 `je` → `call 0x1803f47a0`）；我们**出声 + 返回**（本项目红线：不许崩、也不许静默）。</summary>
        public override void Collect(int amount)
        {
            T receiver = LiveOpsManager.GetHandler<T>();
            if (receiver == null)
            {
                InventoryManager.Note("points-no-receiver:" + typeof(T).Name,
                    "积分 `" + GetID() + "` 找不到接收方（`LiveOpsManager.GetHandler<" + typeof(T).Name + ">()` = null）⇒ "
                    + amount + " 点没有记到任何地方（原版同一情形会抛 NRE）");
                return;
            }
            receiver.CollectPoints(this, amount);
        }
    }

    /// <summary>🔶 自己设计（非原版）：积分名册 = 原版 `ConfigManager.GetConfig&lt;PointsConfig&gt;().points` 的等价物
    /// （判据 `decomp_full/CurrencyExtensions__ToCurrency.c` 的 `campaignPoints` 那一支）。调度台 2026-10-27 裁定（B26）。</summary>
    public static class PointsCatalog
    {
        static readonly List<Points> _all = new List<Points>();
        public static IReadOnlyList<Points> All { get { return _all; } }

        public static void Register(Points p) { if (p != null && !_all.Contains(p)) _all.Add(p); }
        public static void ClearForTest() { _all.Clear(); }

        /// <summary>原版 `ToCurrency(campaignPoints)` 找的那一份：`OfType&lt;CampaignPoints&gt;().FirstOrDefault(p =&gt; p.cardArmy == 0)`。
        /// 我们这一侧的等价条件 = **`Type == campaignPoints` 且 `Army` 为空**（中立那份）。查不到给 null（⛔ 不造假的）。</summary>
        public static ICurrency NeutralCampaignPoints()
        {
            for (int i = 0; i < _all.Count; i++)
            {
                Points p = _all[i];
                if (p == null) continue;
                ICurrency c = p as ICurrency;
                if (c == null) continue;
                if (c.Type != GameCurrency.campaignPoints) continue;
                if (!string.IsNullOrEmpty(p.Army)) continue;
                return c;
            }
            return null;
        }
    }
}
