// CardInstance.cs — **一张具体的牌**（手牌 / 牌库 / 弃牌堆 / 移出游戏 / 死单位表里存的应该是它）
//
// ✅ **2026-10-17 订正（铁律 5）：这一节原来写「第 1 步已做，**第 2/3/4 步还没做**」—— 【整段过期】。**
//    `资料/卡实例身份_爆炸半径.md` §八 记着 **2026-09-18 当天五步全部做完**（`RuleEngineTest` **2928/2928** 全过、
//    一号验收靶 `Master of Manoeuvre` 绿）；本轮现核也对得上：`PlayerState.Deck/Hand/Discard`
//    **三处都已经是 `List<CardInstance>`**、`DrawnThisTurn` 已挪进 `CardInstance`
//    （`PlayerState.cs` 里那几行也留着同样的订正痕迹）。⇒ **⛔ 别照下面那段去改类型。**
//
// ✅ **2026-10-18（`A885` + `A886`）：手牌效果的存放处也搬到【实例】上了**（原来那张对局级的
//    `BattleContext.HandBuffs` 已删）—— 手牌效果**挂在 `CardInstance` 自己身上**
//    （= 原版 `CardScript +0x108` 那个 `List<CardEffect>`）：`HandBuffOps` / `HandBuffSource` /
//    `HandBuffExpire` / `HandBuffExpireTurn` / `HandBuffUsesRef` 就是那一格。
//    三个登记点与兑现点见 `RuleCore.AttachHandEffect` / `RuleCore.ApplyHandBuffs`，
//    到期清扫见 `RuleCore.ExpireHandBuffs`（原版 `PlayerHand.UpdateCardEffects`）。
//    ⚠️ **仍然没有消费者的**：「在手里就看得见」的**卡面 / 查询**那一半（把效果画出来）——
//       我们不做假装有。🆕 **2026-10-18（`A885` ②）补上了另一半**：**后进手牌的牌**
//       也会吃上既有手牌效果（`RuleCore.SetupCardInHand`，原版 `PlayerHand__SetupCardInHand.c`）。
//
// 📌 **历史留痕（2026-09-18 之前的状态，只留结论、正文已删）**：那一步之所以没一次切完 ——
//    `Deck/Hand/Discard` 换类型会报 **120 个去重错误点**（`EffectResolver` 68 · `RuleCore` 38 · `SimpleAI` 7 ·
//    `BattleDriver` 7），而且**不全是机械替换**（`ps.Hand.Add(pick)` 要先判「已存在的一份 / 新造的一张」，
//    猜错就是一个静默的语义 bug）。⇒ 完整计划与爆炸半径 → `资料/卡实例身份_爆炸半径.md`。
//
// 为什么要有它（= 待办第 7 行「卡实例身份」）：
//   手牌/牌库/弃牌堆存的是 `CardDef`（**卡模板**）—— 同名两张是**同一个对象**，
//   于是「只降**这一张**复制品」「只把**这一份**变成临时卡」做不到，
//   只能靠**按份数记账**近似（`PlayerState.DrawnThisTurn` / `BattleContext._markedEphemeral` /
//   `HandBuff.Count` / `CostMod.Key` 四处，注释里都写着「我们没有卡实例身份」）。
//
// 和 `UnitState` 的关系（它是现成范式，**别去动棋盘那套**）：
//   `CardDef`（不可变定义） → `CardInstance`（手里的那一份） → `UnitState`（场上的那个单位）
//   `UnitState` 本来就是「定义 + 实例态」；缺的只是中间这一层。
//   ⚠️ 但**接过来的时候 `UnitState` 也要带上实例引用**，否则手牌→场上那一跳会断链（见上）。
//
// 🔴 **实例态怎么走 —— 用户 2026-09-18 拍了：由效果自己决定**。引擎的默认是：
//    · **回手 / 洗回牌库** → **同一个 `CardInstance`**（不新建），实例态**默认保留**；
//      效果的分句可以在之后显式改它（例：`Master of Manoeuvre` 的 `It costs 4 less`）。
//    · **变身**（`become a …`，全卡池只有 `Hrolf the Ironhowl`）→ **另发新实例、状态不跟**
//      （卡都换了；改前那套按 `CardDef` 键，换 def 之后键就变了 ⇒ 旧行为本来也是「不跟」）。
//    · 「新造一张」（初始牌库 / `create a copy` / 造衍生物）→ 当然发新实例。
//    ⚠️ 默认「保留」的理由：改前「按份数记账」的键是 `CardDef`，回手后卡还是那个 def
//       ⇒ **效果默认就是跟着走的**。选它 = 与改前行为一致，只是从「按份数」变精确。
//    🎯 **一号验收靶**：`Master of Manoeuvre`（`Return a friendly Vehicle to your hand. It costs 4 less`）——
//       现在 `EffectResolver.DoReturn` 末尾用 `ctx.LastCreated`（**存 `CardDef`**）当「它」的引用位，
//       **手里两张同名载具时两条都会降价**；做完实例身份之后必须变成「只有那一份」。
//       口径与验收集见 `资料/卡实例身份_爆炸半径.md` §七。
//
// 🔴 **实例 id 必须可复现**：由 `BattleContext` 的计数器发（`NextInstanceId++`），
//    **不用 Guid / 不用 `UnityEngine.Random`** —— 同种子必须得到同一局（见 `BattleContext` 头注释）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>一张**具体的**牌。字段语义见文件头。</summary>
    public sealed class CardInstance
    {
        /// <summary>它是哪张卡（不可变定义，全场同名卡共享同一个对象）。</summary>
        public readonly CardDef Card;

        /// <summary>本局唯一、**可复现**。排查日志 / 表现层配对用。</summary>
        public readonly int Id;

        // ---- 实例态：原来靠「按份数记账」近似的那几样，现在一人一份 ----

        /// <summary>
        /// **这一份**是本回合从牌库抽到的（`Teleport` 判据）。
        /// 替掉原来的 `PlayerState.DrawnThisTurn`（`Dictionary&lt;CardDef,int&gt;` 计数）。
        /// 规则书 `:219`「当回合从牌库抽到即打出时触发能力」。
        /// </summary>
        public bool DrawnThisTurn;

        /// <summary>
        /// **这一份**被标记成临时卡（回合结束未打出即移出游戏）。
        /// 替掉原来的 `BattleContext._markedEphemeral`（计数）。
        /// ⚠️ 和「卡自带 `Ephemeral` 关键词」不是一回事：那个是**这张卡的所有份**都临时。
        ///    判据合并在一处：`BattleContext.IsEphemeral`。
        /// </summary>
        public bool EphemeralMarked;

        /// <summary>
        /// 🆕 **2026-10-25（引擎线 `A2` · `A2-3`）：换牌档位**（原版 `CardScript.mulliganOption`，`+0x260`）。
        /// `-1` = **没打过档**（本局没跑换牌阶段 / 不是起手牌 / 旧数据）；其余 = 原版四档之一。
        ///
        /// <para>**判据**（第一权威 = `PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:104-117`）：
        /// 记 `pos = max(督军 initialHandModifier, 0)`、`e = secondExtraCards`（**只给非先手那一侧**）、
        /// `base = startingHand + min(mod, 0)`、`count = pos + base + e`，逐张按**换牌列表下标** `i`：
        /// `i &lt; +0x3c` → **30** · `i &lt; +0x3c + pos` → **20** · 否则 **10**，且
        /// `e + +0x3c + pos &lt;= i` → **0**。⚠️ 那个 `+0x3c` 在同一协程里被写死成 **0**
        /// ⇒ **30 档本路径不可达**（`P` 报告 §2/§8 同结论）。⇒ 实际 = `i &lt; pos → 20` ·
        /// `pos &lt;= i &lt; pos+e → 10` · `i &gt;= pos+e → 0`。
        /// <br>⚠️ **`PlayerHand__GetMulliganOption.c:5-16` 那份同族实现的标签不一样**（同样三个区间
        /// 分别给 30 / 20 / 10），但它在全量反编译里**没有任何调用点** ⇒ 死代码，以协程为准。
        /// 本条差异已记进 A2 报告「顺手发现」。</para>
        /// <para>**消费点只有两处**（全反编译对 `+0x260` 的读取）：`CardScript__ClickMulliganSelected.c:27`
        /// 与 `AI__GetAiMulliganCards.c:39`，**都是 `==30` 才拦** ⇒ 今天 30 不可达、20/10 没有消费者，
        /// 所以这条字段**现读零可观测差异**（`play check` 指纹不变）。</para>
        /// <para>⚠️ **档值不是坐标**：换牌摆位只由 `MoveCardToMulliganPos(hand, card, i, count)`
        /// （= `CardsHorizontalLayout.GetPosition(i, count, …)`）吃 `i`/`count` 决定 —— 表现层照旧按
        /// 下标摆，⛔ 别拿这个字段去算位置。</para>
        /// </summary>
        public int MulliganOption = -1;

        /// <summary>
        /// 挂在这一份上的**手牌加成**（打出时以新上场的单位为靶跑一遍）。
        ///
        /// ✅ **2026-10-18（`A885`）：这里就是原来的那个存放处** ——
        /// 替掉了 `BattleContext.HandBuff{ CardDef Card; int Count; }`，
        /// 再在 2026-10-18 替掉了它自己那一版对局表 `BattleContext.HandBuffs`。
        ///
        /// 🔴 **为什么搬上来**：原版这套效果**长在牌自己身上** ——
        /// `CardScript.AddEffect(那张牌, handEffect.cardEffect)`（`PlayerHand__SetupCardInHand.c:71` ·
        /// `PlayerHand__AddHandEffect.c:126-135`），存在 `CardScript` 的 `+0x108`（`List&lt;CardEffect&gt;`，
        /// `d:/2/tools/il2cpp_out/dump.cs` 核过偏移）。挂了之后：
        ///   · **在手里那一段就属于那张牌**（`EntityScript.HasCurrentTrait` 同时读 `+0x128` 与 `+0x108`）；
        ///   · **打出时跟着上场** —— `PlayerHand.RemoveCardFromHand`（`…:20`）**只把这张牌从
        ///     `currentHand` 里 `Remove`**，不销毁、不重建 ⇒ 手上那张牌打到场上**还是同一个对象**。
        /// ⇒ 所以它必须挂在 `CardInstance` 上，**不是**一张对局级的表：
        ///   对局表那版「同名两张各挂各的」只是**近似**，且「打出的那一份」得靠
        ///   `ReferenceEquals(h.Instance, inst)` 去对局表里回查 —— 表一被别处清掉就静默丢。
        ///
        /// ⚠️ 载荷在这一刻**不解析**（只留原文），兑现时交给 `give` 那条路
        ///    （词表只有 `GivePayload` 一份判据，见 `EffectResolver.AttachHandEffect`）。
        /// </summary>
        public readonly List<HandEffect> HandEffects = new List<HandEffect>();

        /// <summary>
        /// 🔴 **2026-10-19（第五十五会话 · `A1567`）：附魔层的两条并列 list。**
        /// 原版 `EntityScript` 上：`activeEffects // +0x108`（`List&lt;CardEffect&gt;`）
        /// 与 `activeEnchantments // +0x110`（`List&lt;ActiveEnchantment&gt;`，
        /// `dump.cs:118926-118930`）。这里照搬那两条：
        ///   · <see cref="ActiveEffects"/> = 附魔那一族的 `CardEffect` 记录；
        ///   · <see cref="ActiveEnchantments"/> = 「这是哪张附魔卡 + 本局第几件」的轻量条目。
        /// **挂 / 取 / 花 / 结算 / 开局** 五个入口全在 `Enchantments`（`Core/Enchantment.cs`）。
        ///
        /// ⚠️ **和 <see cref="HandEffects"/> 的关系**（别混）：两者**都**对应原版 `+0x108`，
        ///    按**消费者**拆开 —— 手牌加成那一族在 `HandEffects`（打出时**兑现即摘**），
        ///    附魔那一族在这里（跟着牌在场、**不按回合清**）。理由与判据 → `Enchantment.cs` 文件头。
        /// </summary>
        public readonly List<ActiveCardEffect> ActiveEffects = new List<ActiveCardEffect>();

        /// <summary>附魔条目（原版 `EntityScript.activeEnchantments // +0x110`）。见 <see cref="ActiveEffects"/>。</summary>
        public readonly List<ActiveEnchantment> ActiveEnchantments = new List<ActiveEnchantment>();

        /// <summary>🆕 **2026-10-24（引擎线 `G2`）：原版 `CardScript.questCounter // +0x270`。**
        ///
        /// <para>语义（判据 = `d:/2/tools/decomp_full/EntityScript__ReachedQuestCounter.c`，全文 15 行）：
        /// 载体身上**每触发一次**「任务附魔」（`cardType == 40 Whispers` ∧ `spellType == 120 Quest`
        /// ∧ `questTimes > 1`）就 **+1**；**还没到 `questTimes` 时那条附魔的 ability 不结算**
        /// （`return 0` + `UpdateFigures`），**到了之后每次照常结算**（计数器**不清零**）。
        /// ⇒ `questTimes` 不是「任务卡要做的次数」，是**这条附魔能力的前置蓄力次数**。</para>
        ///
        /// <para>⚠️ **计数位在【载体】上、不在附魔条目上**（原版 `ReachedQuestCounter(entity, enchRaw, entity)`
        /// 第三次实参就是 `entity`）⇒ 同一张牌身上挂两条任务附魔时**共用一个计数器**（原版如此，照搬）。
        /// 🔴 **放在 `CardInstance` 而不是 `UnitState`**：原版 `CardScript` 的对应物是
        /// `CardInstance`（见 `Enchantment.cs` 文件头那一段），单位离场再回来仍是**同一份牌**。</para>
        /// <para>⚠️ **今天恒 0 / 无人可达**：`cards_raw.json` 1,351 张里 `_quest` / `questTimes` **全是 0**、
        /// `spellType == 120` **0 张** ⇒ 消费点（`Enchantments.FilterByQuestCounter`）跑不到它。
        /// 这是**数据事实**，⛔ 不是「原版没有这条规则」。</para>
        /// </summary>
        public int QuestCounter;

        /// <summary>
        /// 🔴 **2026-10-18（`A885` ④ · 铁律 5 就地订正）：到期位从「一份牌一格」改成「一条效果一格」。**
        ///
        /// 原版一条 `CardEffect` **自己带**那几个到期旗标（`PlayerHand__UpdateCardEffects.c` 逐条读
        /// `entry.cardEffect` 的 `+0x32/+0x33/+0x34/+0x35`），而**牌身上是一张 `List&lt;CardEffect&gt;`**
        /// （`CardScript +0x108`；`PlayerHand__RemoveHandEffectAt.c:30-40` 用 `List.Contains` 逐条摘）
        /// ⇒ **一份牌可挂多条、各带各的到期**。
        /// 改之前这里是「4 个标量 + 1 条平表（`HandBuffOps`）」：每次挂载都把这一份的到期位**整个覆盖**
        /// ⇒ 只记得**最后挂上那条** ⇒ 一份牌上两条不同到期的效果会**一起**被摘 / **一起**活着。
        /// </summary>
        public sealed class HandEffect
        {
            /// <summary>载荷（`give` 那条路解释；登记这一刻**不解析** —— 词表只有 `GivePayload` 一份判据）。</summary>
            public EffectOp Op;

            /// <summary>
            /// 🆕 2026-10-18（`W4` 整改 · 审查问题 6）**「同一次挂载」的记录号** ——
            /// 一次 `AddHandEffect` 发给 N 张牌，N 张身上那 N 条**共用一个 `RecordId`**
            /// （= 原版那**一个 `HandEffect` 记录 / 一个 `CardEffect` 对象**发给 N 张牌的形状，
            /// 见 `PlayerHand__AddHandEffect.c:112-139` 与 `CardInstance.HandBuffUses` 的注释）。
            ///
            /// 🔴 **为什么要它**：手牌登记表（`RuleCore.HandEffectRegistry`）要按**记录**去重，
            /// 而不是按「来源 + 载荷」那种字符串键 —— 后者会把 `Beast Snagga Nob`
            /// **撑三个回合挂的那三条**（三个独立记录）压成一条，后进手牌的牌就**只吃 +1**。
            /// 号由 `BattleContext.NextHandEffectRecord()` 发（跟着对局走 ⇒ **可复现**，不用 Guid）。
            /// </summary>
            public int RecordId;

            /// <summary>来源卡名（日志用）= 原版 `CardEffect.effectName / enchantingCard` 那一格的对应物。</summary>
            public string Source;

            /// <summary>到期方式 —— 照原版 `PlayerHand.UpdateCardEffects` 的那几条支线。**逐条**。</summary>
            public HandBuffExpiry Expire = HandBuffExpiry.Never;

            /// <summary>到期回合号（配合 <see cref="Expire"/>；口径 = `BattleContext.Turn`）。</summary>
            public int ExpireTurn;

            /// <summary>共享计数盒（`null` = 不限次）。见 <see cref="HandBuffUses"/>。</summary>
            public HandBuffUses Uses;

            /// <summary>
            /// 原版 `CardEffect.extrinsic // +0x30`。
            /// 出处（逐条核过）：`dump.cs:119071` 的字段 + 它的 tooltip
            /// `Effect is tied to acting card, not target card`；写点 =
            /// `AbilityLogic__PlayAbility.c:1197 / :1354`
            /// （`cardEffect.extrinsic = abilityData.whileInPlay || abilityData.whileInPlayVariable`，
            ///  `dump.cs:19557-19566` 那两格就是 `AbilityData` 上的 `+0x40/+0x41`）
            /// ⇒ **语义 = 「这条是【在场上才成立】的能力给的」**。
            /// </summary>
            public bool Extrinsic; public bool Permanent;   // 🆕 2026-10-28（B91·W4b）`Permanent` = 原版 `CardEffect.permanentBuff(+0x74)` —— 该条【摘不掉】（判据 = `CardScript__RemoveEffect.c` 第一句 `permanentBuff != 0 ⇒ return` / `RemoveBuffHandCostEffects.c:35` 最外层闸）。⚠️ 数据槽而已：生产点 = `RuleCore.AttachHandEffect`、消费点 = `RuleCore.ExpireHandBuffs` / `ClearHandEffect`（均 W1 的文件）—— 账 L846 的「记录层灌值」另一半在那两处，⛔ 别在此文件另造判断

            /// <summary>
            /// 🆕 **2026-10-23（`W64 B-09`）：这条记录只补给【这一份】实例**（`0` = 不限）。
            ///
            /// 判据 = `cards_raw.json` 的 `BL6`（旧 id `BL15`，`Rubric Marine`）：
            /// `cardAbilities[0].abilityLogic[0].chosenAbility = 40 = buffSelf`（`AbilityEffect.cs:12`）
            /// ⇒ 原版那条手牌监听效果只加**它自己那一份**，不是「同名全部」。
            /// ⚠️ 旧做法靠 `Target.NameFilter = 卡名` 收窄 ⇒ 手里两张同名时**两张一起吃**（静默偏多）。
            /// 本字段按 `CardInstance.Id`（一局唯一）钉。消费口 = `RuleCore.HandEffectFits`
            /// （后进手牌的牌那一趟）；条目的**首次挂载**本来就只有 `inst` 自己，不受影响。
            /// ⚠️ 它**不是** `EffectTargetSpec` 上的字段 —— 那是原版 `targetCriteria` 的镜像，
            /// 而这是**我们**为了区分「哪一份」加的实现位（放进 `EffectTargetSpec` 会污染
            /// 「目标规格 ↔ 原版字段」的一一对应，`ruleprobe` 的 `TargetCols` 哨兵也会报）。
            /// </summary>
            public int OnlyInstanceId;

            /// <summary>施放者（原版 `CardEffect.enchantingCard // +0x18`）。
            /// extrinsic 剪枝要认**它**还在不在场上（`UpdateCardEffects.c` 那两条无条件支线）。</summary>
            public UnitState ActingUnit;

            /// <summary>
            /// 原版 `HandEffect.targetCriteria` —— **后进手牌的牌要过这一关**才知道吃不吃得上
            /// （`PlayerHand__SetupCardInHand.c:58` 的 `FilterMethods.CheckIfMeetsCriteria`）。
            /// `null` = 这一条没有可判的规格 ⇒ **这份记录【不补给】后进手牌的牌**
            /// （`HandEffectFits` 判不出来就不放行 ⇒ `SetupCardInHand` 只出声、不贴牌；
            ///  ⚠️ **2026-10-18 订正（铁律 5）**：这一格原来只写「没有可判的规格（见
            ///  `RuleCore.SetupCardInHand` 的如实说明）」—— **没写后果**，而 `SetupCardInHand` 那儿一度
            ///  还写着「退回『单位卡』这一档」（**那个退路 `W5` 当天已收窄掉**）。
            ///  正确口径 = 原版 `FilterMethods__CheckIfMeetsCriteria.c` 头两句：criteria 缺失 ⇒
            ///  `LogError` + `return 0`（拒）；只有「criteria 存在但为空」才全放行）。
            /// 📌 真对局里四个生产者都传 spec ⇒ 本值只由自检默认值产生。
            /// </summary>
            public EffectTargetSpec Target;
        }

        /// <summary>
        /// **兼容视图：这一份身上各条 `give` 载荷的扁平表**。
        /// ⚠️ 它**不是**存放处（存放处 = <see cref="HandEffects"/>）—— 每次读都现拼一份，
        /// ⛔ 别拿它当稳定的表身份用，也别再往它上面 `Add`。
        /// </summary>
        public List<EffectOp> HandBuffOps
        {
            get
            {
                WarnCompatView("HandBuffOps");
                var r = new List<EffectOp>(HandEffects.Count);
                foreach (var e in HandEffects) if (e != null && e.Op != null) r.Add(e.Op);
                return r;
            }
        }

        /// <summary>**兼容视图**：最近挂上那条的来源 / 到期 / 次数盒（= 改之前那 4 个标量的语义）。
        /// ⛔ **判据别再用它** —— 逐条判要用 <see cref="HandEffects"/> 里每一条自己的格子。</summary>
        public HandEffect LastHandEffect
        {
            get { return HandEffects.Count == 0 ? null : HandEffects[HandEffects.Count - 1]; }
        }

        /// <summary>
        /// 🆕 2026-10-18（`W4` 整改 · 审查问题 10 的护栏）**兼容视图的读取告警** ——
        /// 一份牌身上挂着**多条**效果时，`HandBuff*` 那几个视图**只反映最后挂上那条**，
        /// 拿它当判据就会**读错**（本轮新写的 ②′ 断言就往同一份实例上挂两条）。
        /// ⇒ 读到「>1 条」时**当场出声**（每个实例只喊一次，不刷屏）。
        /// ⛔ 这不是「静默失败」的例外：喊过之后请**改读 `HandEffects`**。
        /// </summary>
        bool _compatViewWarned;
        void WarnCompatView(string who)
        {
            if (_compatViewWarned || HandEffects.Count <= 1) return;
            _compatViewWarned = true;
            UnityEngine.Debug.LogWarning(
                $"[RuleEngine] `CardInstance.HandBuff*` 兼容视图（{who}）在这一份「{Card?.Name}」上"
                + $"读到 {HandEffects.Count} 条效果 —— 它**只反映最后挂上那条**。"
                + "⛔ 判据请改读 `HandEffects`（逐条自己的格子）。");
        }

        /// <summary>手牌加成的来源卡名（日志用）。多条来源时记**最后**挂上的那一条。</summary>
        public string HandBuffSource
        {
            get { WarnCompatView("HandBuffSource"); var e = LastHandEffect; return e != null ? e.Source : null; }
        }

        /// <summary>
        /// **这份手牌效果什么时候到期**（`BattleContext.Turn` 的回合号；配合 <see cref="HandBuffExpire"/>）。
        /// 对应原版 `CardEffect` 上那四个 `until*` 标志位里**我们解析层能产出的两档**
        /// （偏移都在 `d:/2/tools/il2cpp_out/dump.cs` 核过）：
        ///   · 卡面 `this turn` ⇒ `untilEndOfTurn // +0x32`
        ///   · 卡面 `until your next turn` ⇒ `untilPlayerTurnStart // +0x33`
        ///     / `untilFollowingPlayerTurnStart // +0x35`（原版把这两个放在**同一条判断**里）
        /// ⚠️ 剩下两档（`untilEnemyTurnStart // +0x34`）**我们的解析层产不出来** ⇒ 没有写点，
        ///    清扫器里如实标着「没做」；⛔ 别以为它做了。
        /// </summary>
        public int HandBuffExpireTurn
        {
            get { WarnCompatView("HandBuffExpireTurn"); var e = LastHandEffect; return e != null ? e.ExpireTurn : 0; }
        }

        /// <summary>**兼容视图**：到期方式 = **最后挂上那条**的（= 改之前那个标量的语义）。
        /// ⛔ 逐条判用 <see cref="HandEffect.Expire"/>，别用这个。</summary>
        public HandBuffExpiry HandBuffExpire
        {
            get { WarnCompatView("HandBuffExpire"); var e = LastHandEffect; return e != null ? e.Expire : HandBuffExpiry.Never; }
        }

        /// <summary>这份手牌效果怎么到期（原版 `PlayerHand__UpdateCardEffects.c` 的三条支线）。</summary>
        public enum HandBuffExpiry
        {
            /// <summary>不过期 —— 原版四个 `until*` 全为假的那种
            /// （`Beast Snagga Nob` 的 `At the end of your turn, give +1 …` 那一族：**每回合结束再挂一份**、
            /// 一直攒着，见 `RuleEngineTest.TestBeastbossAndPayloadSegments` ④）。</summary>
            Never = 0,

            /// <summary>`CardEffect.untilEndOfTurn // +0x32` —— 回合结束时**无条件**摘
            /// （`PlayerHand__UpdateCardEffects.c:100` 那条**不比较谁的回合**）。</summary>
            EndOfTurn = 1,

            /// <summary>`untilPlayerTurnStart // +0x33` / `untilFollowingPlayerTurnStart // +0x35` ——
            /// **不是拥有者的回合**结束时摘（那一刻的下一回合就轮到拥有者了）。
            /// 判据 = `PlayerHand__UpdateCardEffects.c:119-137`（`IsPlayerTurn() != originalIsPlayer`）。</summary>
            OwnerTurnStart = 2,
        }

        /// <summary>
        /// 一条手牌效果「**还能打出几次**」的**共享计数盒**（原版 `HandEffect.numberOfUses // +0x2c`，
        /// 开关 `limitedUses // +0x28`）。
        ///
        /// 🔴 **额度是【共享】的，不是一份一份** —— 与原版那两张表的结构一致：
        ///   · `PlayerHand.activeEffects // +0x48` = `List&lt;HandEffect&gt;`（**记录**，计数器长在这上面）；
        ///   · `CardScript.activeEffects`/`+0x108` = `List&lt;CardEffect&gt;`（**逐张牌挂的那份**）。
        ///   一条 `HandEffect` 发给 N 张牌（`PlayerHand__AddHandEffect.c:126-135`）⇒
        ///   **N 张共用同一个 `numberOfUses`**。判据：
        ///   · `PlayerHand__CardPlayedWithEffects.c:28`（`limitedUses` 是开关）· `:33-35`（打出的那张牌
        ///     身上有没有这条效果）· `:39 numberOfUses--` · `:43-45 &lt; 1 ⇒ RemoveHandEffectAt`；
        ///   · `PlayerHand__RemoveHandEffectAt.c:30-40` 遍历**整只手牌**逐张 `CardScript.RemoveEffect`
        ///     ⇒ 摘的是**所有还带着它的牌**（不是打出的那一张 —— 那张已经上场了）。
        /// ⇒ 所以存的是**一个对象**：同一次挂载发给的每一份**引用同一个盒子**（<see cref="HandBuffUsesRef"/>）。
        ///
        /// ⚠️ 与 `RuleCore.ApplyHandBuffs` 那条既有口径「**兑现即摘 —— 额度钉在这一份上**」
        ///    **并存、不互相取代**，哪条优先写在那个方法的注释里。
        /// </summary>
        public sealed class HandBuffUses
        {
            /// <summary>原版 `HandEffect.limitedUses // +0x28`：这条效果**限次**（`false` = 不限）。</summary>
            public bool Limited;
            /// <summary>原版 `HandEffect.numberOfUses // +0x2c`：还剩几次。</summary>
            public int Left;
            /// <summary>⚠️ **我们加的**（原版没有这个字段）：起始次数，只用于日志与断言。</summary>
            public int Max;
        }

        /// <summary>**兼容视图**：最后挂上那条共用的「还能打出几次」盒子（`null` = 不限次）。
        /// 见 <see cref="HandBuffUses"/>。⛔ 逐条判用 <see cref="HandEffect.Uses"/>。</summary>
        public HandBuffUses HandBuffUsesRef
        {
            get { WarnCompatView("HandBuffUsesRef"); var e = LastHandEffect; return e != null ? e.Uses : null; }
        }

        // ---- 🆕 2026-10-18（`A920` 的续）伴生卡的两个回填位 -------------------
        //
        // 原版 `ProcessCompanion.c:88-89` 在**刚造出来的那张新卡**上打两个标记：
        //   · `*(newCard + 0x64) = 1`                    ⇒ `isCompanion`
        //     （`dump.cs:21997` `<isCompanion>k__BackingField; // 0x64`）
        //   · `*(newCard + 0x60) = *(源卡 + 0x60) - 1`   ⇒ `companionCounter` **递减**
        //     （`dump.cs` `<companionCounter>k__BackingField; // 0x60`）
        // 它们是**递归链**的载体：那张新卡再被打出时，`ProcessCompanion`
        // 因为自己**不带 `Companion` 词条**而走 `companionCounter > 0` 那一支
        // （`ProcessCompanion.c:29`），于是**再造一张自己**、计数再减一。
        // ⇒ `Companion N: X` 一共带出 **N 张 X**，但不是一次给 N 张 —— 是**打一次给一张**。
        // -----------------------------------------------------------------------

        /// <summary>原版 `CardScript.isCompanion // +0x64` —— 这一份是**伴生生成出来的**。</summary>
        public bool IsCompanion;

        /// <summary>原版 `CardScript.companionCounter // +0x60` —— 这条递归链**还剩几环**。</summary>
        public int CompanionCounter;

        /// <summary>
        /// **只作用在这一份上的费用修正**（`CostMod.HandInstanceId` 指向它）。
        /// 「只降这一张复制品」靠它 —— 这是本行的**主要靶子**。
        /// ⚠️ 它是**累计**的：多条修正命中同一份时累加（原版 `AddEffect` 也是累加）。
        /// </summary>
        public int CostDelta;

        public CardInstance(CardDef card, int id)
        {
            Card = card;
            Id = id;
            ArmedTrap = TrapArmedOf(card);   // 🆕 2026-10-27（`E7ENG` · 第三批）：原版 `armedTrap(+0x54)` 的唯一写点 = 造牌那一刻（见 `ArmedTrap` 的 doc）
        }

        /// <summary>是不是某张卡（按**定义**比，不是按份）。</summary>
        public bool Is(CardDef c) { return ReferenceEquals(Card, c); }

        // ---- 谁来发 id：真对局 vs 没有对局的场合 -------------------------------
        //
        // 🔴 **真对局只有一处发号**：`BattleContext.NewInstance`（`_nextInstanceId++`）。
        //    它跟着 `BattleContext` 走 ⇒ **每一局都从 1 开始**、同种子必然同序号（对局可复现）。
        //
        // 下面这个 `Detached` 是给**没有 `BattleContext` 的场合**兜底的：
        // 测试自己拼棋盘（`new UnitState(card, false)`）、演示场景塞探针、卡池预览 ——
        // 那些地方拿不到 ctx。它发的是**负数 id**，所以**永远不会**和对局发的正数撞号。
        // ⚠️ 别拿它当「真实例」用：它没有对局里那份「手牌 → 场上 → 弃牌堆」的连续身份，
        //    只是「一个够用的身份位」。真对局的部署一律走 `UnitState(CardInstance)`。

        static int _detachedNext = 0;

        /// <summary>没有 `BattleContext` 时用的实例（负数 id）。语义见上面那段注释。</summary>
        public static CardInstance Detached(CardDef card)
        {
            return new CardInstance(card, --_detachedNext);
        }

        /// <summary>是不是「没有对局上下文」的分离实例（报表 / 断言用）。</summary>
        public bool IsDetached { get { return Id < 0; } }

        public override string ToString()
        {
            return (Card != null ? Card.Name : "?") + "#" + Id;
        }

        // ================================================================================================
        //  🆕 2026-10-27（批次 9 · 执行代理 `E7ENG` · 第三批：`OnTrapDrawn(250)` 的两格数据位之二）
        //  —— **追加在类尾**（⛔ 别插到中间：`账/*.tsv` 按 `文件:行号` 引，插一行就漂）
        // ================================================================================================

        /// <summary>`armedTrap` 三态的「未知」（见 <see cref="ArmedTrap"/>）。</summary>
        public const int ArmedTrapUnknown = -1;

        /// <summary>
        /// 🆕 **原版 `CardScript.armedTrap`（`+0x54`）** —— 「这张牌是**已布设的陷阱**」。
        ///
        /// <para>**判据（🥇 全量反编译，本批亲读）**：
        /// · **唯一写点** = `CardScript__SetAsActiveTrap.c:16`（`*(undefined1 *)(param_1 + 0x54) = 1;`，
        ///   外加一句 `CustomDebug.Log`），**唯一调用者** = `BattleCardManager__CreateCard`（VA 直调 `0x18099185B`）
        ///   ⇒ **陷阱卡一被造出来就是「已布设」**，原版没有单独的「布设动作」；
        /// · **没有任何清零点**：`EntityScript__set_armedTrap` 在 `decomp_full/` 里**无按名调用点**
        ///   （全树只有 `CardScript__CardDrawn.c` 的读 `*(char*)(param_1 + 0x54) != 0` 与上面那一处内联写）；
        /// · 读点两处：`CardScript__CardDrawn.c:23`（⇒ `OnTrapDrawn` = trigger 250）与
        ///   `BattleManager._ResolveDrawCard_d__432__MoveNext.c:288`（⇒ `BroadcastTrapResolved` + 战斗日志）。</para>
        ///
        /// <para>🔴 **我们的落法 = 实例**出生那一刻**一次算定**，⛔ 不在「搬牌」的入口再置一次。
        /// 原因：原版那唯一写点就在 `CreateCard`（造牌），而**造牌入口我们全仓只有一处构造点**
        /// （`CardInstance(CardDef, int)`；两个工厂 = `BattleContext.NewInstance` 真对局发正号 /
        /// `CardInstance.Detached` 无对局发负号）—— `RuleCore.PlayCard` / `DeployFree` /
        /// `EffectResolver.DoCreate` / `RuleCore.Draw` / `EffectResolver.DoReanimate` /
        /// 教程 `SetupInitialHand` 全都是**搬同一份实例**、不是新建 ⇒ 在那里再置位会在
        /// 「同一张陷阱被造一次、抽多次」时与原版不一致（原版只在造牌时置一次）。</para>
        ///
        /// <para>取值（三态）：`1` = 已布设 · `0` = **判得出不是** · `-1` = **未知**
        /// （<see cref="CardDef.IsTrapCard"/> 判不了：卡表 &lt; v34 没 `cardType` 列 / 这张卡不在真值快照里）。
        /// ⚠️ `-1` 时**消费端必须出声、不许当 0**（否则「表是旧的」会被静默读成「这张不是陷阱」）。</para>
        /// <para>📌 今天可达性 = 0：`cards_raw.json` 1,351 张 `spellType == 70` **0 张** ⇒ 全池
        /// `ArmedTrap` 恒 `0`（判得出，不是未知）。</para>
        /// </summary>
        public int ArmedTrap { get; private set; } = ArmedTrapUnknown;

        /// <summary>`CardDef.IsTrapCard` → `armedTrap` 三态（唯一调用点 = 构造函数）。
        /// 判不了 ⇒ 出声一次类的日志（由 `CardDef.IsTrapCard` 的 `why` 说明原因），并落 `-1`（⛔ 不猜 0）。</summary>
        static int TrapArmedOf(CardDef card)
        {
            if (card == null) return ArmedTrapUnknown;
            string why;
            bool? trap = card.IsTrapCard(out why);
            if (trap == null) return ArmedTrapUnknown;
            return trap.Value ? 1 : 0;
        }
    }
}
