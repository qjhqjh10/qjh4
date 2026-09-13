// BattleEvent.cs — 规则引擎的**结构化事件流**
//
// 为什么要有它：
//   表现层要「在正确的格位播正确的特效」，只有两条路知道场上发生了什么 ——
//     ① 对比同步前后两份战场快照（v1 的做法，2026-09-12 已删掉）——
//        只看得出「掉血了 / 人没了」，分不出是挨刀、被技能打、还是疲劳；
//        而且**根本看不出「谁发动了技能」**
//     ② 引擎把发生的事**说出来**（本文件）
//   选 ②。当初「部队卡在场上发动技能」「触发效果」这两类特效接不上，卡的就是 ① 走不通 ——
//   引擎里压根没有这两种事件，表现层无从得知。
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。
//
// ⚠️ 这里的 `Kind` 是**规则语义**（发生了什么），不是特效名。
//    「哪种事件播哪个特效」在表现层的 `CardPresentation/Core/VfxMap.cs` 里映射 ——
//    引擎不认识特效库，也不该认识。
//
// 消费方式：`BattleContext.Signals` 累积，表现层用 `DrainSignals()` **搬走并清空**。
// 搬而不是游标，是因为搬完就没有「读到哪了」的账要记，也不会因为裁剪旧事件而错位。
using System.Text;

namespace RuleEngine
{
    /// <summary>引擎事件种类。少而稳定 —— 加一种就意味着表现层多一种可播的时机。</summary>
    public enum EvtKind
    {
        /// <summary>**打出了这张牌**（手牌 → 场上 / 结算）。`PlayCard` 里发 —— 单位卡随后还会发一条
        /// `Deploy`（那条是「进格位」，效果召唤来的单位**只有它**）。
        /// 2026-09-13 加：以前战术卡打出去**一个事件都没有**，战斗日志和表现层都看不到它。</summary>
        Play,
        /// <summary>单位落到格位上（出牌结算完的那一刻，不是拖拽松手）</summary>
        Deploy,
        /// <summary>攻击宣言 —— 伤害之前发，表现层才有「抬手 → 命中」的余地</summary>
        Attack,
        /// <summary>挨伤害。**含护盾挡下（Amount = 0）和疲劳**，都是「这个单位被打了一下」</summary>
        Hit,
        /// <summary>阵亡离场。督军倒下也发（槽位仍是 4）</summary>
        Death,
        /// <summary>**主动技能发动** —— 部队卡在场上花掉一次行动放技能</summary>
        Ability,
        /// <summary>**触发效果** —— Rally / Strike / Slay / Backlash / Penitence</summary>
        Trigger,
        /// <summary>
        /// **离开格位但不是阵亡** —— 被 `Return X to your hand / to the top of their deck` 挪回去了
        /// （DarkAngels `Master of Manoeuvre` / `Covert Operation`，规则书英文版 `:455-457`）。
        ///
        /// 为什么要单开一种而不是复用 <see cref="Death"/>：回手/回牌库**不进弃牌堆**、
        /// 也不该播阵亡特效 —— 表现层拿 `Death` 会把它消散掉，那是**错的画面**。
        /// ⚠️ 表现层目前只是**把视图摘掉**（`BattleDriver.PlayReturnFeel`），
        ///    「飞回手牌」的位移动画**没做**（原版有没有、什么参数，没查到）。
        /// </summary>
        Return,

        /// <summary>
        /// **阵营资源变化**（2026-09-13 第三十三轮）。`Amount` = 变化量（正数）。
        ///
        /// 为什么单开两种而不是复用 `Trigger`：卡面**真的**以它为时机写效果 ——
        ///   · `When you gain Faith, deal 4 damage to the enemy warlord`（`Paragon Warsuit`）
        ///   · `When you collect a Spirit Stone, gain Shield` 等 **4 张灵族单位**（`Farseer` /
        ///     `Spiritseer Qelenaris` / `Warp Spider Exarch` / `Warlock Skyrunner`）
        /// ⇒ `WhenEvent` 认这两个事件名，效果才有挂载点。
        /// ⚠️ 和 `Hit` 一样是**给玩家**的（`Slot` = -1），不是给某个单位的。
        /// </summary>
        GainFaith,
        /// <inheritdoc cref="GainFaith"/>
        GainSpirit,
        /// <summary>**任务点**（暗黑天使的阵营资源，2026-09-13 第三十三轮）。见 `PlayerState.QuestPoints`。</summary>
        GainQuest,
    }

    /// <summary>一条已经发生的事。字段全是**引擎知道的事实**，表现层只管往画面上翻译。</summary>
    public class BattleEvent
    {
        public EvtKind Kind;

        /// <summary>归属方 0/1。`Attack` 时 = **攻击者**那方</summary>
        public int Player = -1;
        /// <summary>在己方的第几格（-1 = 不在场上）</summary>
        public int Slot = -1;
        /// <summary>卡名（和 `CardDef.Name` / `CardData.id` 一致）</summary>
        public string CardId;

        /// <summary>只有 `Attack` 用：被打的那一方（攻击永远是跨半场的，这里显式写出来，别让表现层去猜）</summary>
        public int TargetPlayer = -1;
        public int TargetSlot = -1;

        /// <summary>被指向的那张卡的名字（`Attack` 用）。2026-09-13 加：战斗日志要写「谁打谁」，
        /// 而**留档以后再回看时那个格位早就换人了** —— 目标卡名必须在发事件这一刻就记下来。</summary>
        public string TargetCardId;

        /// <summary>`Ability` / `Trigger` 专用：哪个关键词（`rally` / `slay` / `ability`…）</summary>
        public string Keyword;
        /// <summary>`Ability` / `Trigger` 专用：效果原文（`Damage 2 EnemyUnit`）</summary>
        public string Effect;

        /// <summary>
        /// 伤害 / 治疗的数值。护盾挡下 = 0。
        /// ⚠️ **负数 = 治疗**（`Regeneration` 回合结束回血走这条）——
        ///    表现层据此把反馈画成绿的。卡面效果造成的治疗走 `heal` 那条 op，不发这个事件。
        /// </summary>
        public int Amount;
        /// <summary>`Attack` 专用：这一刀是远程还是近战（表现层据此挑特效）</summary>
        public bool Ranged;

        /// <summary>发生时的全局回合序号 —— 只用于日志和排查</summary>
        public int Turn;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("T").Append(Turn).Append(' ').Append(Kind).Append(' ');
            sb.Append("P").Append(Player + 1).Append('@').Append(Slot);
            if (!string.IsNullOrEmpty(CardId)) sb.Append(' ').Append(CardId);
            switch (Kind)
            {
                case EvtKind.Attack:
                    sb.Append(Ranged ? " 远程→" : " 近战→")
                      .Append('P').Append(TargetPlayer + 1).Append('@').Append(TargetSlot);
                    break;
                case EvtKind.Hit:
                    sb.Append(" 受 ").Append(Amount).Append(" 伤");
                    break;
                case EvtKind.Ability:
                case EvtKind.Trigger:
                    sb.Append(" [").Append(Keyword).Append("] ").Append(Effect);
                    break;
            }
            return sb.ToString();
        }
    }
}
