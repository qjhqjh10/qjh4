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
        /// ✅ **2026-09-29：那条位移动画已经做了** —— 表现层 = `CardFeel.ReturnToHand`
        ///    （原版 `BattleCardUI.PlayBackToHandAnimation`：**倒放 `Card Hand To Board`**，
        ///     `speed = −1`、`time = AnimationState.length`，外加 `DOMove(up × localScale.x × 3.0, 0.208 s, 线性)`；
        ///     逐帧判据在那个方法的注释里）。**回牌库也走同一条**（原版两个调用者）。
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

        /// <summary>
        /// 🆕 **玩家点了一颗灵魂石、把它收走了**（2026-09-25）。`Player` = 收集方 · `Slot` = 那颗石头
        /// 原来在**哪一格**（**不是** -1 —— 这条事件是**格位上**的，表现层要在这儿播收集特效）。
        ///
        /// 为什么要单开一种而不是复用 <see cref="Death"/>：和 <see cref="Return"/> 同一条理由 ——
        /// 收走一颗石头**不是阵亡**（不该播阵亡消散）。
        /// **原版也是分开的**：`BattleManager.DestroyUnit(…, UnitDeathType.collectWaystone = 50, …)`，
        /// 而收走那一下播的是 `RemnantAeldari.CollectWaystoneEffect()`
        /// （`d:/2/tools/decomp_full/RemnantAeldari__CollectWaystoneEffect.c`，逐行读过）：
        /// `AudioCue.Play3D(collectSound)` + **在残骸原位** `Instantiate` 那件收集粒子
        /// （`param_1[0xf]`）+ 立刻 `Destroy(残骸体)`。
        ///
        /// ⚠️ **和 `GainSpirit` 是两条事件，都要发**：`GainSpirit` 管「玩家资源变了 + 触发
        /// `When you collect a Spirit Stone, …`」，而且按那族的约定 `Slot = -1`；这一条只管
        /// 「**在哪一格**播收集特效」。两条别合并（合并会破掉 `GainFaith` 那族的 `Slot = -1` 约定）。
        /// </summary>
        CollectWaystone,

        // ==================================================================
        //  🆕 2026-10-18（`A985⑥` · 前置那一半）：**战斗日志（`Battle/Cemetery/`）要的那 3 种**
        //
        //  为什么加这三条：原版那一族 19 条词条的**产出方**是 `CemeteryManager.GetActionText`
        //  （`decomp_full/CemeteryManager__GetActionText.c` 的 `switch`，逐条解出来的映射表 →
        //   `CardPresentation/Battle/BattleDriver.cs` 的 `RefreshBattleLog` 头注释）。
        //  其中 **7 条**我们引擎**根本不发对应事件** —— 逐条对下来，缺的就是下面这 3 种
        //  （另外 4 条 = `Display{Your,Enemy}CardInHand` ×2 · `AmbushedTroop` · 以及
        //   `SecretOrder` 今天没有发出点，见它自己的注释）。
        //  ⚠️ **加在这张表的【末尾】** —— 枚举值只许追加，不许插队/重排
        //  （`EvtKind` 是「少而稳定」的一张表，插队会让别处存下来的整数值错位）。
        // ==================================================================

        /// <summary>
        /// 🆕 **从牌库抽了一张牌**（`Player` = 抽牌那一方 · `Slot` = **-1**（不在场上）·
        /// `CardId` = 抽到的那张）。
        ///
        /// **判据 = 原版**（`BattleManager._ResolveDrawCard_d__432__MoveNext.c:283-295`）：
        /// ```
        /// if (IsDuringMulligan == 0) {
        ///     BroadcastCardDrawn(card);                       // ← 广播「抽到了」
        ///     … BroadcastTrapResolved … CemeteryManager.AddDrawTrapAction(cemetery, card);
        ///     SetCardTurnDrawn(card);                         // ← 记「本回合抽到的」
        /// }
        /// ```
        /// ⇒ 我们这一侧的对应物 = `RuleCore.Draw` 里那三句的**中间那一句**
        ///   （`BroadcastWhen(WhenEventKind.Draw, …)` → **本事件** → `inst.DrawnThisTurn = true`）。
        /// 词条 = `Battle/Cemetery/ActionYouDraw` / `ActionOpponentDraws`（按 `EntityScript.isPlayer`
        ///   （`dump.cs:21973` `// 0x40`）二选一），**事件不需要带这个位**：消费端有 `Player` 就够
        ///   （`CardPresentation` 那边按 `e.Player == driver._me` 判）。⚠️ 这个映射是本次**现核**的：
        ///   `GetActionText` 的 `0x14` 支里 `(char)*(param_2+6)=='\0'` 取的是 `DAT_184285c40`，
        ///   解出来 = `ActionOpponentDraws`（不是 `ActionYouDraw`）—— 顺序与 `G5` 报告里那张表的
        ///   列法**相反**，以字符串表实读为准（`il2cpp_out/stringliteral.json`）。
        ///
        /// ⚠️ **原版那一行只在「抽到的是【已布设的陷阱】」时才记**（`AddDrawTrapAction` 的**唯一**
        ///   调用点就包在 `if (card.armedTrap != 0)` 里，`EntityScript.armedTrap` = `dump.cs:21983`
        ///   `// 0x54`）—— **我们没有陷阱机制**，所以**本事件对每一次牌库→手牌的抽牌都发**。
        ///   消费端若要严格照原版只印陷阱那一档，今天**判不出来**（没有那个位）⇒
        ///   这一条**如实挂着**，接日志时由调度台裁（要么我们的日志为普通抽牌也印一行 = 与原版不同，
        ///   要么这个事件今天没人用）。⛔ 别为此先加一个恒假的字段（那是静默的假数据）。
        /// ⚠️ **两个已知会「多记」的入口**（都是上面那条口径的连带，如实记着、⛔ 别猜）：
        ///   ① **教程局的起手**（`TutorialRules.SetupInitialHand` → `RuleCore.Draw`）——
        ///      它不是「抽牌」而是**发牌**，可它的 `MulliganOpen` 是**假**（教程不跑换牌）⇒ 照发。
        ///      原版那一刻 `IsDuringMulligan` 是真是假**判不出来**（W5 的记录：那个协程的启动点
        ///      grep 不到）⇒ **不猜**，照发。
        ///   ② **普通局的起手发牌【不发】**（走 `DealOpeningHand`、不走 `Draw`，判据见它的头注释）✓。
        /// </summary>
        Draw,

        /// <summary>
        /// 🆕 **撤除伏击 —— 面朝下的单位翻开了**（`Player`/`Slot` = 它还在场上那一格 ·
        /// `CardId` = 卡名 · `Effect` = **哪条出口**：`"damage"` / `"window"`）。
        ///
        /// **判据 = 原版**：词条 `Battle/Cemetery/ActionExitAmbush` 由
        /// `CemeteryManager.AddExitAmbushActionToCemetery` 产出（`CemeteryManager__AddExitAmbushActionToCemetery.c`，
        /// 记录里第一个 int = `0x28`），而它**唯一的调用点**是 `CardScript.SetAmbush(card, **false**)`
        /// 的尾部（`CardScript__SetAmbush.c:105`）—— 即「**把伏击状态撤掉**」那一条；
        /// 该方法头部（`param_2 != 0`）是**设**为伏击，不记日志。
        /// `SetAmbush(card, 0)` 的调用点**三个，全是伤害路**：
        /// `CardScript__ResolveUnitAttacked.c:111` · `CardScript__ResolveDamageDealt.c:173` ·
        /// `BattleManager._ResolveAttack_d__438__MoveNext.c:305` ⇒ 对应我们 `ApplyDamage` 里那一句
        /// `u.FaceDown = false`（「挨到伤害就翻开、那次伏击效果作废」）。
        ///
        /// ⚠️ **`"window"` 那一档是【我们模型的出口】，原版没有**：我们另一条出口是
        /// `RuleCore.RevealAmbush`（撑到控制者下个回合开始 → 翻开并触发，判据 = 粉丝实体版规则书 `:166`），
        /// 而原版那条路走 `CardScript.TriggerAmbush`（`RawCardScript.OnTrigger(0x29e, …)`）——
        /// **它不翻面**（`SetAmbush` 的调用点里没有它；`BattleManager__ResolveAction.c:7616`
        /// 调它时也不伴随 `SetAmbush`）。⇒ 我们这条出口**与原版不同形**（另一条待办，已报给调度台）。
        /// 仍然**两条出口都发**：本事件的名字/语义 = 「**这个单位不再面朝下**」这个状态迁移，
        /// 而两条出口都是它；**用 `Effect` 记下是哪一档**，消费端要严格照原版就**只印 `"damage"`**。
        /// </summary>
        AmbushExit,

        /// <summary>
        /// 🆕 **密令（`Card_Trait/secretOrder`）被揭示并执行**。词条 = `Battle/Cemetery/ActionSecretOrder`。
        ///
        /// **判据 = 原版**：`CemeteryManager.AddExecuteSecretOrderAction`（记录里第一个 int = `0x19`，
        /// `CemeteryManager__AddExecuteSecretOrderAction.c:90`）**唯一的调用点**在
        /// `BattleManager.<ResolveRevealOrders>d__549.MoveNext:76` —— 那一刻已经
        /// `DisplayRevealedCard`（亮出那张牌）→ `CardScript.DestroyCard` → 必要时
        /// `BattleManager.RemoveEnchantment(…, 2)`，**最后**才记这一行；`param_2` = 身上挂着密令的
        /// **督军**、`param_3` = 那张密令（记录第 2 张卡名 = `param_3 + 0x18 → +0x28`）。
        /// 上游：`AbilityLogic.PlayAbility` 的 `spellId == 0x118`（280）支对每个目标
        /// `BattleManager.AddRevealOrders`（`AbilityLogic__PlayAbility.c:2495`）—— 后者往动作队列里
        /// 压一条 `BattleAction`，类型 = **`0x31`**（`BattleManager__AddRevealOrders.c:32`）。
        /// ⚠️ **`ResolveRevealOrders` 在 26,282 个方法体里 grep 不到任何调用点** ⇒
        ///   「`0x31` → `ResolveRevealOrders`」这一跳是**从动作类型推的，不是读出来的**
        ///   （如实标着，别当成直证）。
        ///
        /// 🔴 **我们引擎今天【没有发出点】**（如实挂着，⛔ 不猜）：密令 = **战将身上的一件面朝下的
        ///   附魔**（`EntityScript__HasSecretOrders.c` 数的是 `+0x110` 那串 activeEnchantments 里
        ///   `CardEffect + 0x90 == 0x3c`(60) 的那些；`AI__ValueOfEnchantments.c:65` 同一个判据），
        ///   而 `RuleEngine` 里**根本没有「战将身上的附魔」这一层**（`grep Enchant` 全仓零模型，
        ///   只有注释）、`KeywordTable` 里也**没有 `secretOrder`**（`Card_Trait/secretOrder` 是我们
        ///   还没实现的词条）。⇒ 要让这一条真发出来，**先得有密令那套机制**（面朝下放置 + 揭示时机），
        ///   那是**另一件活**（判据已全部记在上面）。**今天这一条只是把表补全**，让消费端可以引用它。
        /// ⚠️ **它不是我们那张 `Secret` 兵种牌**（秘仪 / `spellType 150`）：原版打出秘仪走
        ///   `PlayerManager.AddSecretPlayed`（`_ResolvePlayCardFromHand_d__447:1274`），**与密令无关**。
        /// </summary>
        SecretOrder,
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

        /// <summary>**被指向**的那一方 0/1（没目标 = `-1`）。
        ///
        /// 🔴 **2026-10-18（`A985⑥①`）就地订正（铁律 5）**：这一行原来写的是
        /// 「**只有 `Attack` 用**」—— **已过期**。现在**三种都填**：
        /// `Attack`（跨半场，**必填**）· `Play`（**卡面要求选目标**的战术卡才填）·
        /// `Ability`（`EffectTargets.NeedsPick` 的技能才填）。
        /// 判据 = 原版 `CemeteryManager.GetActionText` 的 **case `0xF`**（`Ability`）与
        /// **case `10`**（`Play`），两处的判别式**同一条** —— 记录里那个目标引用
        /// （**字节 `0x28`**）是不是 `null`：
        /// `UnityEngine_Object__op_Equality(uStack_50, 0, 0)`，
        /// 而 `uStack_50 = *(undefined8 *)(param_2 + 10)`（⚠️ `param_2` 是 **`int*`** ⇒ 下标 10 = 字节 `0x28`）。
        /// （`d:/2/tools/decomp_full/CemeteryManager__GetActionText.c`，方法与行号见
        ///  `RuleCore.UseAbility` / `EffectResolver.PlayTactic` 里各自那段注释。）
        /// ⚠️ **没目标就保持 `-1`**（⛔ 别拿哨兵值顶替 —— 那会让「有目标」在记录上**永远为真**）。
        /// ⚠️ **消费面不是诊断**：`BattleDriver.BuildCardContext` 拿它算
        /// `targetIsPlayer`（→ `WFModuleCollisions` / `WFModuleTransformModifier`）。</summary>
        public int TargetPlayer = -1;
        public int TargetSlot = -1;

        /// <summary>被指向的那张卡的名字（`Attack` / 带目标的 `Play` / 带目标的 `Ability` 用）。
        /// 2026-09-13 加：战斗日志要写「谁打谁」，
        /// 而**留档以后再回看时那个格位早就换人了** —— 目标卡名必须在发事件这一刻就记下来。</summary>
        public string TargetCardId;

        /// <summary>
        /// 🆕 **`Play` 专用：这一次「打出一张牌」是【伏击】（面朝下打出）吗**（`A985⑥①`）。
        ///
        /// **判据 = 原版**（`d:/2/tools/decomp_full/CemeteryManager__GetActionText.c` 的 **case `10`**，
        /// 即 `Play` 那一条）：那一支把记录分成 **6 档** = `2(靶向) + 2(普通) + 2(伏击)`：
        ///   · 「靶向」= 目标引用 `+0x28` 是不是 `null`（**与 `Ability` 同一条判别式**，见上面
        ///     `TargetPlayer` 的 doc）；**是靶向就按 `isPlayer` 二选一，不再看伏击位**；
        ///   · 「伏击」= **它自己的一个位** —— **字节 `0x1C`**：`local_18` 取自 `param_2 + 0x18`，
        ///     读的是 `local_18._4_1_`；那两个不靶向的分支落在 `LAB_18061b41d`，
        ///     再按 `isPlayer`（`(char)*(param_2 + 6)` = 字节 `0x18` 首字节）各分两档。
        ///   ⇒ 原版**伏击有自己的位、不是从 `isPlayer` 推出来的** ⇒ 我们照它**加一个专用 `bool`**。
        ///   ⛔ **不许**复用 `Effect`（那是字符串**诊断**位、是自造编码）或 `Keyword`。
        ///
        /// **我们这边的判别式**（全工程只有一处算它 —— `RuleCore.PlaysFaceDown(CardDef)`）=
        /// `card.Has(Ambush)` **且** `card.TriggerOps(Ambush) != null`。它同时也是
        /// 「**这个单位面朝下上场**」那一支的条件（`RuleCore.PlayCard`）⇒ 两个字段**同真同假**。
        ///
        /// ⚠️ **不是伏击就保持 `false`**（⛔ 别拿哨兵值顶替）；别的种类（`Attack`/`Ability`/…）不填这一位。
        /// </summary>
        public bool PlayAmbush;

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
