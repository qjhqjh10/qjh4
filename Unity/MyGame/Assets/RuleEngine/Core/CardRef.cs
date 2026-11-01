// CardRef.cs — **原版「目标」的等价物**：一个 `CardScript`（手牌 / 牌库 / 墓地 / 场上同型）。
//
// 🔴 **为什么需要这一层**（判据全部现读，2026-11-01 `B115_W23`）：
//   · 原版 `AbilityLogic.abilityTargetsToSave // +0x80`、静态 `pooledPossibleTargets`、
//     `CardScript.previousAbilityTargets`、`CardScript.referencedCards // +0x278`
//     **全是 `List<CardScript>`**（`d:/2/tools/il2cpp_out/dump.cs:19482 / :19484 / :30823 / :35480 / :35549`）；
//   · `GetCardsInHandList` / `GetCardsInCemetery` / `GetCardsInDeck` / `GetUnitsInPlayList`
//     **返回的也是 `List<CardScript>`**（`:32595 / :32610 / :32613 / :32574`）；
//   · `AbilityLogic__AddDrawCard.c:46-49` 把**抽到手牌的那几张**直接 `AddRange` 进目标表
//     ⇒ 「上一跳抽到的 3 张」天然可以是下一条效果的合法目标（`ta = 150 previousAbilityFiltered`）。
//
//   我们把这些拆成了两个类：场上 = `UnitState`、其余（手牌 / 牌库 / 弃牌堆）= `CardInstance`
//   ⇒ **原来是结构性偏差**（不是漏接线）：`List<UnitState>` 结构上装不下手牌实例。
//   本类型把两者收成**一个身份位**，让同一张表既能装棋盘单位、也能装手牌实例。
//
// ⚠️ **它不是「原版的第三个字段」，是我们这边的桥**：
//   `CardInstance` 是唯一身份（`UnitState.Instance` 也是它，见 `UnitState.cs:32`），
//   `UnitState` 只是**棋盘载体**（不在场 = `null`），`CardDef` 模板只留给「数据里只有卡定义」的
//   夹具/降级路径（一旦真的走到，消费端**必须出声**，⛔ 不许静默当场上单位）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>原版 `CardScript` 的等价物：<see cref="CardInstance"/>（唯一身份）
    /// ＋ 可选的 <see cref="UnitState"/>（棋盘载体）。</summary>
    public readonly struct CardRef : System.IEquatable<CardRef>
    {
        /// <summary>唯一身份（手牌 / 牌库 / 弃牌堆 / 场上同一个对象）。`null` 只有「模板降级」一档。</summary>
        public readonly CardInstance Inst;

        /// <summary>棋盘载体；**不在场 = `null`**（原版没有「没有实例的棋盘单位」，见 `UnitState.cs:940`）。</summary>
        public readonly UnitState Unit;

        /// <summary>只有「没有实例的模板」才非 `null`（原版不会有 ⇒ 正常路径不走；走到必须出声）。</summary>
        public readonly CardDef Template;

        CardRef(CardInstance inst, UnitState unit, CardDef template)
        {
            Inst = inst;
            Unit = unit;
            Template = template;
        }

        /// <summary>棋盘单位 → `CardRef`（`Inst` 取 `u.Instance`，`Unit` 取它自己）。
        /// ⚠️ `u == null` ⇒ 全空（调用方自己判空）。</summary>
        public static CardRef Of(UnitState u)
        {
            if (u == null) return default(CardRef);
            return new CardRef(u.Instance, u, null);
        }

        /// <summary>一张牌（手牌 / 牌库 / 弃牌堆）→ `CardRef`（`Unit = null`）。</summary>
        public static CardRef Of(CardInstance i)
        {
            if (i == null) return default(CardRef);
            return new CardRef(i, null, null);
        }

        /// <summary>只有卡定义时 → `CardRef`（`Template` 非 `null`，`Inst/Unit` 全空）。
        /// ⛔ 只给「数据里本来就没有实例」的夹具用；消费端遇到它**必须出声**。</summary>
        public static CardRef Of(CardDef d)
        {
            if (d == null) return default(CardRef);
            return new CardRef(null, null, d);
        }

        /// <summary>兼容旧调用点（旧断言 `List&lt;CardRef&gt;.Contains(UnitState)`）：按 <see cref="Of(UnitState)"/> 隐式转。</summary>
        public static implicit operator CardRef(UnitState u) { return Of(u); }

        /// <summary>同上（`CardInstance` 侧）。</summary>
        public static implicit operator CardRef(CardInstance i) { return Of(i); }

        /// <summary>这张牌的**卡定义**（`Inst?.Card ?? Template`）。</summary>
        public CardDef Card { get { return Inst != null ? Inst.Card : Template; } }

        /// <summary>在不在棋盘上（判据 = `Unit != null`；**不是** `Card` 非空）。</summary>
        public bool IsOnBoard { get { return Unit != null; } }

        /// <summary>稳定身份号（`Inst.Id`；模板降级 = `0`）。⚠️ 手牌实例号恒正，`Detached` 是负号。</summary>
        public int InstanceId { get { return Inst != null ? Inst.Id : 0; } }

        /// <summary>卡名（日志 / 事件 `CardId` 用；模板降级也能给）。</summary>
        public string Name { get { var c = Card; return c != null ? c.Name : "?"; } }

        /// <summary>**按实例号现扫棋盘**找回载体 —— 换位 / 补位 / 延后死亡之后仍然对。
        /// ⚠️ 已离场 / 待死 / 从来不在棋盘上的 ⇒ `null`（原版 200 那一档**保留已离场的**，
        /// 所以调用方不能把 `null` 当「这张不存在」丢掉，见消费端注释）。</summary>
        public UnitState ResolveUnit(BattleContext ctx)
        {
            if (Unit != null) return Unit;
            if (ctx == null || ctx.Players == null || Inst == null) return null;
            for (int p = 0; p < ctx.Players.Length; p++)
            {
                var ps = ctx.Players[p];
                if (ps == null || ps.Board == null) continue;
                for (int s = 0; s < ps.Board.Length; s++)
                {
                    var u = ps.Board[s];
                    if (u == null || u.Instance == null) continue;
                    if (object.ReferenceEquals(u.Instance, Inst)) return u;
                }
            }
            return null;
        }

        /// <summary>判据 = **`ReferenceEquals(Inst)`**（同一份实例）；两个都空时比 `Template`。
        /// ⚠️ 不比 `Unit`/`Id` 数值 —— `Detached` 的负号在夹具里可以重复。</summary>
        public bool Equals(CardRef o)
        {
            if (Inst != null || o.Inst != null) return object.ReferenceEquals(Inst, o.Inst);
            return object.ReferenceEquals(Template, o.Template);
        }

        public override bool Equals(object o) { return o is CardRef && Equals((CardRef)o); }

        public override int GetHashCode()
        {
            if (Inst != null) return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Inst);
            return Template != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Template) : 0;
        }

        public override string ToString()
        {
            return Name + (IsOnBoard ? "@board" : (Inst != null ? "#" + Inst.Id : "@template"));
        }
    }
}
