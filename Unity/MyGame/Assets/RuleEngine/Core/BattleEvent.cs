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
        /// <summary>单位落到格位上（出牌结算完的那一刻，不是拖拽松手）。
        /// 🆕 2026-10-19：「**怎么进来的**」看 <see cref="BattleEvent.DeployFrom"/>（战斗线 B3 的接口）。</summary>
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
        ///
        /// ✅ **2026-10-19（第五十五会话 · 账 `A1819`/`A1985`）就地更新（铁律 5）**：
        ///   · 🔴 **上一段引错了对象**（`A1985`）：`+0x90 == 0x3c` 读的**不是 `CardEffect`** ——
        ///     `EntityScript__HasSecretOrders.c:31` 的链是
        ///     **`ActiveEnchantment.enchantmentCard(+0x10) → RawCardScript.spellType(+0x90) == 0x3c(60)`**
        ///     （`dump.cs:118926-118930`）；`CardEffect + 0x90` 是另一回事（`RevealedSecret` 里那个
        ///     **「已揭示」bool**，见 `EntityScript__RevealedSecret.c`）。
        ///   · **附魔层已经建起来了**（`Core/Enchantment.cs` + `CardInstance.ActiveEnchantments`），
        ///     所以「`RuleEngine` 里根本没有这一层」这句**已过期**。
        ///   · **发出点也有了** = `RuleCore.RevealSecretOrders`（照
        ///     `BattleManager._ResolveRevealOrders_d__549.MoveNext` 那条链：揭示 → 必要时摘附魔 → 发本事件）。
        ///   · ⚠️ **仍然不可达**：触发「揭示」的上游是能力逻辑
        ///     `AbilityLogic.chosenAbility == revealOrder(280)`（`AbilityLogic__PlayAbility.c:2492-2506`），
        ///     而我们的能力逻辑**没有数据层**（现读 `cards_raw.json` **0 张**卡带 `revealOrder`）
        ///     ⇒ **没有任何调用点**。判据与接口需求写在 `资料/普查产出_1019/E3_报告.md`。
        /// ⚠️ **它不是我们那张 `Secret` 兵种牌**（秘仪 / `spellType 150`）：原版打出秘仪走
        ///   `PlayerManager.AddSecretPlayed`（`_ResolvePlayCardFromHand_d__447:1274`），**与密令无关**。
        /// </summary>
        SecretOrder,
    }

    /// <summary>
    /// 🆕 **`Deploy` 事件：这个单位是【怎么】进到格位上的**（战斗线 B3 的接口需求，2026-10-19）。
    ///
    /// <para>为什么需要它（判据 = 原版两条**完全不同**的协程，`d:/2/tools/decomp_full/` 逐行读过）：
    /// 战斗线要决定「这一手要不要演手牌飞行 / 起飞两条 cue」，而原版那两件事挂在**不同的路由**上：
    ///   · `BattleManager._ResolveCardAutoSummoned_d__511__MoveNext.c`（= 我们 `DeployFree(fromHand:true)`，
    ///     卡面 `from your deck`，池里 2 张 `BL55` / `SW57`）—— **有**手牌飞行 + `moveCardIntoBattle` /
    ///     `playMinionFromHand` 两条起飞 cue（`:104,122,187`）；
    ///   · `BattleManager._ResolveSummonUnit_d__510__MoveNext.c`（= 我们 `DeployFree` 的默认档，效果召唤 /
    ///     复制 / 凭空造）—— `MinionManager.SummonMinion` **直接插进格位**，两个声音调用点**0 个**；
    ///   · `RuleCore.PlayCard`（手牌打出）—— 走玩家拖拽/`CardInteraction.Release` 那条，飞行由输入层给票；
    ///   · `EffectResolver.DoReanimate`（残骸 / 墓地翻回来）—— 见那一处自己的注释。
    /// ⇒ 这几条路在引擎里发出的 `Deploy` 事件**逐字相同**，表现层分不出（B3 报告 §三·2）。</para>
    ///
    /// <para>🔴 **`Unknown = 0` 是哨兵，不是 `HandPlayed`**：本工程对「新字段的默认值」有既定纪律
    /// （见 `CardPresentation/Battle/ReplayStore.cs` 的 `tutorialStage` / `botSeatPlus2`）——
    /// 新增字段的 **CLR 默认值**必须表示「**没写**」，⛔ 不能让一个忘了赋值的 `Deploy` 事件
    /// 静默冒充「手牌打出」。消费端拿到 `Unknown` ⇒ 按**改前的老行为**处理（所有 `Deploy` 一视同仁）。
    /// （⚠️ 与战斗线 B3 报告 §三·2 建议的 `0 = PlayCard / 1 = autoPlay / 2 = 召唤 / 3 = 复活` 差一位：
    /// 这里把 `0` 留给了哨兵。**消费端以本枚举为准**。）</para>
    ///
    /// <para>⚠️ **回放格式不受影响**：本地录像存的是「起始条件 + 动作流」
    /// （`ReplayStore.ReplayRecord.actions`，见 `Battle/ReplayStore.cs` 文件头），
    /// **`BattleEvent` 不进 `.json`** ⇒ 加这一位不需要 `version +1`，旧录像照读。</para>
    /// </summary>
    public enum DeploySource
    {
        /// <summary>没写（哨兵）。旧路径 / 自检里手工合成的 `Deploy` 事件。消费端按老行为处理。</summary>
        Unknown = 0,
        /// <summary>**从手牌打出**（`RuleCore.PlayCard`）—— 飞行由输入层给票（`CardInteraction.Release`）。</summary>
        HandPlayed = 1,
        /// <summary>**从手牌免费打出**（`RuleCore.DeployFree(fromHand:true)`）—— 原版
        /// `_ResolveCardAutoSummoned`：有手牌飞行 + 起飞两条 cue，而且会触发 Rally / 兑现手牌加成。
        ///
        /// <para>🔴 **「哪一方的手牌」不另开一档**（2026-10-19 · `E12` · `A2143` 查证）：
        /// 原版 `_ResolveCardAutoSummoned_d__511__MoveNext.c:74/92/101` 读的是
        /// **`card + 0x40`（`EntityScript.isPlayer`）**，据此选
        /// `playerMinionManager(+0xe0)` / `enemyMinionManager(+0xe8)`（`dump.cs:30742-30743`）
        /// ⇒ **牌从谁的手里拿出来，就落在谁的棋盘上** ⇒ 本事件的
        /// <see cref="BattleEvent.Player"/> **就是那只手的主人**。
        /// 表现层挑 `PlayEnemyCard`（`DisplayEnemyCardInCenter` + `TurnCardAround`）/
        /// `PlayPlayerCard` 的判别式 = **`Player == 本机座位`**（`:110` / `:128` 两支）。
        /// ⛔ 别为「敌方手牌」再加一个枚举值 —— 那是同一条规则的第二个表示。</para></summary>
        AutoPlayFromHand = 2,
        /// <summary>**效果召唤 / 复制 / 凭空造**（`RuleCore.DeployFree` 默认档）—— 原版
        /// `_ResolveSummonUnit`：直接插进格位，**没有**飞行、没有起飞 cue、不触发 Rally。</summary>
        Summon = 3,
        /// <summary>**复活 / 再造**（`EffectResolver.DoReanimate`，从残骸或墓地翻回来）—— 不飞、不触发 Rally
        /// （`DoReanimate` 的判据）；表现层按「只响落地两条」处理（B3 报告 §三·2 的消费约定）。</summary>
        Reanimate = 4,
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

        /// <summary>
        /// 🆕 **`Deploy` 专用：这个单位是【怎么】进到格位上的**（2026-10-19 · 战斗线 B3 的接口需求）。
        /// 取值与各档的判据见 <see cref="DeploySource"/> 的 doc。
        ///
        /// <para>默认 = <see cref="DeploySource.Unknown"/>（哨兵，= 「没写」）⇒ 别的种类（`Play`/`Attack`/…）
        /// 与自检里手工合成的 `Deploy` 都不填这一位；消费端读到 `Unknown` 就按**改前的老行为**处理。</para>
        /// </summary>
        public DeploySource DeployFrom;

        /// <summary>
        /// 🆕 **这件事的主语（/目标）是哪一份牌** —— 稳定实例号 = `UnitState.Instance.Id`
        /// （= `CardInstance.Id`）。**`0` = 没写**（哨兵）；正数 = 真对局里的那一份；
        /// 负数 = 没有 `BattleContext` 的场合（测试 / 演示自建单位，`CardInstance.Detached`）。
        ///
        /// <para>🔴 **为什么必须补这一位**（判据 = 原版**实体访问链**，`d:/2/tools/decomp_full/` +
        /// `d:/2/tools/il2cpp_out/dump.cs`）：
        ///   · 原版每张场上牌身上带一个**本局唯一号** `CardScript + 0x22c` —— 写点
        ///     `CardScript__SetInBattleId.c:8`（唯一写点），发号方
        ///     `BattleCardManager__RegisterCardInPlay.c:58` / `__CreateCard.c:152`
        ///     （`BattleCardManager + 0x28..0x38` 那枚 `ObscuredInt` 单调递增）；
        ///   · 所有**跨对象引用**都拿这个号反查，而不是格位：
        ///     `BattleCardManager__GetCardFromUniqueId.c:33`（`*(int*)(card + 0x22c) == uniqueId`）、
        ///     `PlayerAction` 的 `act` / `tget`（`dump.cs:119892`，`0x14` / `0x18` 两格就是两张牌的
        ///     这个号，`BattleManager__ParseEnemyAction.c:64,69` 都用 `GetCardFromUniqueId` 解）；
        ///   · 语义上格位**不是身份**：原版 `MinionManager` 是两条 `List`，单位离场/换位时格号会变
        ///     （我们的 `BoardSlots` 同一套「插入推人 / 外侧补位」，见 `CLAUDE.md` §三）。
        ///   ⇒ 表现层拿「座位 + 格号」对账，**同名单位同格换人**（先死后补位）时会把两件事记到
        ///     同一个键上 —— 那正是「同名单位血账」错挂的那条路（战斗线 B3/B4 的接口需求）。
        ///     **不许用「座位 + 格号」冒充**这一位。</para>
        ///
        /// <para>填法：单位相关的事件由 `<see cref="RuleEngine.BattleContext"/>` 的发送方填
        /// （`RuleCore.EmitUnit` 一处 + 少数直接 `Emit` 的落点）；`Attack` 的**目标**填
        /// <see cref="TargetInstanceId"/>。别的种类（`Play` 战术卡 / `Draw` / 资源类）保持 `0`。</para>
        /// </summary>
        public int InstanceId;

        /// <summary>
        /// 🆕 **被指向的那一份牌**（只看 `Attack` 今天填；`Ability` / `Play` 的靶向暂时不填 ——
        /// 它们的 `Target` 三件套本来就只在「有目标」时有值，实例号这一维等消费端要了再加）。
        /// 语义与哨兵同 <see cref="InstanceId"/>（`0` = 没写）。</summary>
        public int TargetInstanceId;

        /// <summary>`Ability` / `Trigger` 专用：哪个关键词（`rally` / `slay` / `ability`…）</summary>
        public string Keyword;
        /// <summary>`Ability` / `Trigger` 专用：效果原文（`Damage 2 EnemyUnit`）</summary>
        public string Effect;

        /// <summary>
        /// 🆕 **2026-10-20（第五十七会话 `FACTION-DOUBLE605`）：这一拍是哪一条原版 `CardAbility`**
        /// （= `cardAbilities[i]` 的 **0 基下标**；`-1` = 没写）。
        ///
        /// <para>**判据（原版）**：`RawCardScript__TriggerAbilities.c:59-88,207-265` —— 它**逐条**
        /// 遍历这张卡的 `cardAbilities`，对每一条调 `CardAbility__CanTriggerAbility`，通过就
        /// `TriggerAbility/PlayAbility` **那一条**（每条各播自己的 `abilityLogic[].cardAnimations[]`）。
        /// 605 那一条在 `CanTriggerAbility.c:114-126` 收口到同一段：
        /// `GetCurrentFaith(...) >= ability.triggerValue`（`param_1 + 0x20`）。
        /// ⇒ **同一张卡有两条 `trigger = 605` 的能力、且门槛都过时，原版各发一次**。</para>
        ///
        /// <para>现读全池只有 3 张（`资料/普查产出_1018/allcards_1350/cards_raw.json` 逐张核过）：
        /// `SOR23 Canoness`（能力 0/1，门槛 2/4）· `SOR67 Sacred Rose Sister`（能力 1/2，门槛 2/4）·
        /// `SOR56 Beacon of Faith`（能力 1/2，门槛 4/7）。</para>
        ///
        /// <para>⚠️ **今天只有 605（`UseFaith`）填这一位**；其余事件保持 `-1`。
        /// ⚠️ **`Keyword` 里同时带一个 `#&lt;i&gt;` 后缀**（<see cref="KeywordWithAbility"/>）——
        /// 那是**同一条信息的传输副本**：`BattleDriver` 只把 `Keyword` 转给表现层
        /// （`PlaySignal` → `FireEventAll(keyword:)`），不加新字段就过不去。</para>
        ///
        /// <para>🔴 **2026-10-20 复核后就地订正（铁律 5）**：本段原来写「**读判据一律读这一位**
        /// （`VfxMap` 也先看它，接口里没给才解析后缀）」—— **前半句不成立**：生产路径
        /// `BattleDriver.PlaySignal` **不转这个字段**（它在 T09-WIRE 手里，本笔不许改），
        /// `VfxMap.ResolveAllTimed` 拿到的只有 `Keyword` ⇒ **生产实际只解析 `#i` 后缀**。
        /// `VfxMap` 的同名形参（`abilityIndex`）今天**只有自检/探针在用**，是给
        /// 「将来 `BattleDriver` 肯把这一位传下来」留的入口（两条入口同一条判据，见 `VfxMap` 的 doc）。
        /// ⇒ 真正的**数据源**仍是这一位；`Keyword` 后缀是**它在今天唯一能过河的形态**，
        /// 两者在 `EmitCardTrigger` 一处同时写（⛔ 别在别处再拼一份）。</para>
        /// </summary>
        public int AbilityIndex = -1;

        /// <summary>🆕 `Keyword` 里「trigger 名 + 能力序号」用的分隔符（见 <see cref="AbilityIndex"/>）。</summary>
        public const char AbilityKeywordSeparator = '#';

        /// <summary>🆕 把 `触发名` + `能力序号` 拼成**事件 `Keyword` 的传输形态**：
        /// `AbilityIndex &lt; 0` ⇒ **原样返回 `triggerName`**（= 改前的字节，⛔ 别给单能力卡加后缀）。
        /// 解码在 <see cref="SplitKeywordAbility"/>（**同一处**，⛔ 别在表现层另写一份解析）。</summary>
        public static string KeywordWithAbility(string triggerName, int abilityIndex)
        {
            if (string.IsNullOrEmpty(triggerName) || abilityIndex < 0) return triggerName;
            return triggerName + AbilityKeywordSeparator
                 + abilityIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>🆕 <see cref="KeywordWithAbility"/> 的反向：拆出 trigger 名与能力序号。
        /// 没有后缀 / 后缀不是非负整数 ⇒ **`abilityIndex = -1`、原样返回**（旧事件的字节不变）。</summary>
        public static string SplitKeywordAbility(string keyword, out int abilityIndex)
        {
            abilityIndex = -1;
            if (string.IsNullOrEmpty(keyword)) return keyword;
            int at = keyword.LastIndexOf(AbilityKeywordSeparator);
            if (at <= 0 || at >= keyword.Length - 1) return keyword;
            int idx;
            if (!int.TryParse(keyword.Substring(at + 1), System.Globalization.NumberStyles.None,
                              System.Globalization.CultureInfo.InvariantCulture, out idx))
                return keyword;
            abilityIndex = idx;
            return keyword.Substring(0, at);
        }

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
