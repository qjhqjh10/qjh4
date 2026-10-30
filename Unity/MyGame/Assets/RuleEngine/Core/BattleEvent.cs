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
        /// `CardId` = 卡名 · `Effect` = **哪条出口**：`"damage"` / `"attack"`；
        /// `"window"` 是**历史值、今天不可达**，见下）。
        ///
        /// **判据 = 原版**：词条 `Battle/Cemetery/ActionExitAmbush` 由
        /// `CemeteryManager.AddExitAmbushActionToCemetery` 产出（`CemeteryManager__AddExitAmbushActionToCemetery.c`，
        /// 记录里第一个 int = `0x28`），而它**唯一的调用点**是 `CardScript.SetAmbush(card, **false**)`
        /// 的尾部（`CardScript__SetAmbush.c:105`）—— 即「**把伏击状态撤掉**」那一条；
        /// 该方法头部（`param_2 != 0`）是**设**为伏击，不记日志。
        /// 🔴 **2026-10-20（`W58-AMBUSH`）就地订正（铁律 5）**：本段原写
        /// 「`SetAmbush(card, 0)` 的调用点三个，**全是伤害路**」—— **「全是伤害路」不成立**：
        /// 三个调用点是 `CardScript__ResolveUnitAttacked.c:111` · `CardScript__ResolveDamageDealt.c:173`
        /// （**这两条**才是伤害路）· `BattleManager._ResolveAttack_d__438__MoveNext.c:305`
        /// （**面朝下的攻方出手 ⇒ 取消这一记、再由续攻补打一记**，不是伤害路）。
        /// 我们对应的两条出口：
        ///   · `"damage"` → `RuleCore.ApplyDamage`（真掉血、所有减免之后）；
        ///   · `"attack"` → `RuleCore.DeclareAttack`（伏击单位面朝下攻击 ⇒ 翻开 + 取消这一记、立刻补打一记）。
        ///
        /// 🔴 **2026-10-20 同一笔订正**：本段原写「`"window"` 那一档是【我们模型的出口】……
        /// `RuleCore.RevealAmbush`（撑到控制者下个回合开始 → 翻开并触发）……仍然**两条出口都发**」
        /// —— **已作废**：① 原版 `SetAmbush` 的 7 个调用点里**没有一个在回合开始** ⇒
        /// 「窗口自动揭示」**不是原版行为**，`RuleCore.RevealAmbush` **已删除**；
        /// ② `"window"` 这一档因此**今天不可达**（保留这个字符串只为读旧数据/旧断言，⛔ 别新增发送点）。
        /// ⇒ **今天实际只有 `"damage"` / `"attack"` 两档**，两档的语义都是「这个单位不再面朝下」。
        /// ⚠️ 翻开的**触发**不是本事件的职责：触发闸是回合归属
        /// （`CardScript__SetAmbush.c:69-129` 的 `isPlayer == IsPlayerTurn()`），由 `RuleCore` 在发出本事件后自己判。
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

        // ==================================================================
        //  🆕 2026-10-20（战斗线 `W58`）：**治疗** —— 原版唯一显示口 `CardScript.Heal`
        //  ⚠️ **加在这张表的【末尾】**（枚举值只许追加，见上面 `Draw` 那段：插队会让
        //     别处存下来的整数值错位）。
        // ==================================================================

        /// <summary>
        /// 🆕 **治疗真的落地了**（`Amount` = **实际**治疗量，恒 &gt; 0）。
        ///
        /// <para>**判据 = 原版唯一显示口 `CardScript.Heal` 协程**
        /// （`d:/2/tools/decomp_full/CardScript._Heal_d__468__MoveNext.c:107-225`；逐拍表 →
        /// `资料/普查产出_1020/W58_战斗线_HealText与InBattleId判据.md` §A）：</para>
        /// <list type="bullet">
        /// <item>实际量 = `min(请求量, MaxHealth − Health)`（`:109-112`）；**实际量 &lt; 1 一律不显示**（`:136`）；</item>
        /// <item>显示点 = **被治疗那张卡自己的** `BattleCardUI.healText`（`+0x138`，`dump.cs:26173`）；
        ///   文案 = `"+" + 实际量`，`DOScale 0.05→1.0`（0.7 s · OutQuad · Yoyo ×2）后 `WaitForSeconds(1.5)` 隐藏；</item>
        /// <item>多目标 = `ResolveHeal` 对每个目标 `yield return StartCoroutine(CardScript.Heal)`
        ///   ⇒ **严格串行，每目标 1.5 s**（`BattleManager._ResolveHeal_d__491__MoveNext.c:114-147`）。</item>
        /// </list>
        ///
        /// <para>**发出点 = `RuleCore.HealUnit`**（治疗的唯一落地口）：`heal` op（`EffectResolver.DoHeal`）·
        /// `heal` 能力（`RuleCore.ResolveEffect`）· 回合末再生 · 猎杀标记 —— 四条原版都进
        /// `CardScript.Heal` 的路径，全部经它。只有**实际治疗量 &gt; 0** 才发。</para>
        ///
        /// <para>⚠️ **不是 `Hit` 的负数那一档**：负数 `Hit` 今天只剩「**幸存者广播**」一个发出点
        /// （原版 `UseSurvivor.c:87 BroadcastUnitHealed`）—— 原版 Survivor **不进** `CardScript.Heal`、
        /// **不飘治疗字**（`CardScript__UseSurvivor.c:36-40` 直接写血），所以表现层**不显示**它。</para>
        /// </summary>
        Heal,

        // ==================================================================
        //  🆕 2026-10-22（`W64` · 战斗线「反击演出」）：**反击（retaliation）**
        //  ⚠️ **加在这张表的【末尾】**（枚举值只许追加，见上面 `Draw` 那段：插队会让
        //     别处存下来的整数值错位）。
        // ==================================================================

        /// <summary>
        /// 🆕 **守方还手了（反击）** —— 表现层要播的**唯一**时机：**守方自己**那一发远程攻击
        /// （守方自己的远程攻击动画 + 守方自己那张卡的 `customRangedAttackParticles`）。
        ///
        /// <para>🔴 **判据（第一权威）= 原版 `BattleManager._ResolveAttack_d__438__MoveNext.c:1114-1152`**
        /// —— 这一整段在**主伤害 / 爆裂 / 践踏之后**（`:999` / `:1030` / `:1089`），条件 = 那个
        /// 「允许反击」位 `+0x138`（`:616` 置 1，`:921/935/949/968` 被 LongRange / Sniper / Terror 那几支清 0）：</para>
        /// <code>
        /// iVar14 = EntityScript__GetCombatDamage(守方 /* +0x130 */, 本次打法 /* +0x58 */, 0);   // :1116
        /// if (0 &lt; iVar14) {
        ///   if (本次打法 == 2 /* 远程 */) {
        ///     CardScript__ResolveAttackRangedAnim(守方, 攻方, 0, 0);   // :1121 —— 守方播【自己的】远程攻击
        ///     yield return …;                                          // 协程跑完才继续
        ///   }
        ///   damages = BattleManager__ResolveAttackDamage(守方, 攻方, …); // :1136（近战直接到这里）
        ///   CardScript__ReceiveDamage(攻方, 10 /* combatAttacker */, damages); // :1142
        /// }
        /// </code>
        /// <para>⇒ **只有「本次打法是远程」时守方才播攻击动画 + 攻击粒子**；近战档守方**什么动画都不播**
        /// （原版那一支只做「结算伤害 + 攻方 `ReceiveDamage`」）⇒ 见 `BattleEvent.Ranged`。</para>
        ///
        /// <para>🔴 **字段的归属（与 `Attack` **相反**，别串了）**：</para>
        /// <list type="bullet">
        /// <item>`Player` / `Slot` / `CardId` / `InstanceId`
        ///   = **守方**（= 还手的那一方，原版 `param_1 + 0x130`）；</item>
        /// <item>`TargetPlayer` / `TargetSlot` / `TargetCardId` /
        ///   `TargetInstanceId` = **攻方**（= 被反击的那一方，原版 `param_1 + 0x28`）；</item>
        /// <item>`Ranged` = **这一发反击走远程件吗** —— 原版判的是 **`本次打法 == 2`**
        ///   （`ResolveAttackRangedAnim` 那一支），**不是**「守方自己有没有远程攻击力」
        ///   （`GetCombatDamage` 那一句只决定伤害量、决定不了播不播）；</item>
        /// <item>`Amount` 恒 `0`（伤害量走紧跟其后那条 `Hit`，⛔ 别在这里再记一份）。</item>
        /// </list>
        ///
        /// <para>⚠️ **它只是信号**：发出点 = `RuleCore.DeclareAttack` 里反击那一句 `Hurt` **之前**
        /// （原版也是「先播守方的远程动画、协程跑完才落伤害」）—— 结算顺序与数值**一个字节都没变**，
        /// 既有 `Hit` / 死亡链照旧。⚠️ 也**不进动作流**（不是 `LocalAct` / 不是 `SimpleAI.Executed`），
        /// 录像/联机不受影响（本地录像存的是「起始条件 + 动作流」，`BattleEvent` 不进 `.json`）。</para>
        ///
        /// <para>⚠️ 原版那一段里**没有**任何 `Play2D` / `AttackHitSmall`（对比同文件哨戒 `:1054`、
        /// 践踏 `:1112` 那两处都有 `SoundManager.Play2D(ActivateBlast)`）⇒ 我们这边**不许**给它补
        /// 命中音或通用命中粒子（那两样属于近战命中回调 `ResolveAttackAnimationEffects`，
        /// 反击的远程那一支**不经过**它）。</para>
        /// </summary>
        Retaliation,

        // ==================================================================
        //  🆕 2026-10-24（引擎线 `J2` · 战斗线 `W64` `B-30`）：**多卡展示窗**（效果 470）
        //  ⚠️ **加在这张表的【末尾】**（枚举值只许追加，见上面 `Draw` 那段：插队会让
        //     别处存下来的整数值错位）。
        // ==================================================================

        /// <summary>
        /// 🆕 **摊开一批卡给玩家看**（原版 `AbilityEffect.showCardsWindow = 470` → 动作 86
        /// → `BattleHud.DisplayMultiCardView`）。
        ///
        /// <para>**判据（逐段读过的原版链）**：</para>
        /// <code>
        /// AbilityLogic__PlayAbility.c  case 0x1d6 (470):
        ///     BattleManager.AddShowCardsWindowAction(bm, *targets, *(ability+0x50) /* effectText */, priority, 0)
        ///   → BattleManager__AddShowCardsWindowAction.c   // 建 BattleAction(type = 0x56)，打进「卡列表 + effectText」
        ///   → AddAutoActionToQueue
        ///   → BattleManager__ResolveAction.c  case 0x56:
        ///        if (IsPlayerTurn(bm))                       // ← **只在本地玩家的回合**才开窗
        ///            BattleHud.DisplayMultiCardView(hud, cards, Loc("Card_Abilities/" + effectText))
        ///   → BattleHud__DisplayMultiCardView.c  → UIMultiCardDisplay.Initialize + WindowsManager.OpenWindow
        /// </code>
        ///
        /// <para>**字段**：`Player` = 谁摊开（= 打这张牌的那一方）· `Slot` 恒 `-1`（不在格位上）·
        /// `CardId` = **触发它的那张牌**的卡名 · `Effect` = **标题键**
        /// （`Card_Abilities/&lt;effectText&gt;`，现池唯一一档 = `Card_Abilities/EffectDisplayCardsAddToDeck`
        /// = `These cards have returned to your deck` / `这些张牌已返回到你的卡组`）·
        /// `CardNames` = **摊开的那批卡的卡名**（原版列表序）。</para>
        ///
        /// <para>🔴 **「只在 `IsPlayerTurn` 时显示」这一道闸【不在引擎里】**：引擎是无头的、同时驱动两个座位，
        /// 它没有「本机是哪个座位」这个概念（`BattleContext` 上**没有** `LocalSeat`）。原版那句判的是
        /// **本地玩家**，⇒ **归表现层**（消费端拿 `Player` 与本机座位比即可，与 `Draw` 选
        /// `DealDuration` / `DealDurationFoe` 同一套口径）。引擎这一侧只保证「**该发的时候发、不该发的时候不发**」。</para>
        ///
        /// <para>⚠️ **纯展示**：整条原版链**一个状态写入都没有** ⇒ 本事件**不改任何局面**
        /// （`RuleEngineTest.TestW64ShowCardsWindow` 的 ③④ 钉着「`CardNames` 就是刚回牌库的那批、
        /// 棋盘/牌库的改动全部来自 `return` 那一条 op」）。
        /// 🔴 **2026-10-24（引擎线 `N1`）就地订正（铁律 5）**：本行原来写的是
        /// 「③④ 钉着『打 GSC55 的全池结算与改前**逐字相同**』」—— 那句**已经过期**：
        /// `N1` 修了 `GSC55` 的结算（`return` 的候选从**棋盘**改成**墓地**，
        /// 见 `EffectResolver.ReturnFromCemetery`），全池 `play all dump` 的差异**只落在 `GSC55` 这一行**。
        /// ⇒ 「本事件纯展示」这个结论**不变**，变的是「打这张卡的局面读数」。
        /// ⚠️ 也**不进动作流**（不是 `LocalAct` / 不是 `SimpleAI.Executed`）—— `BattleEvent` 不进 `.json`，
        /// 录像 / 联机不受影响。</para>
        /// </summary>
        ShowCardsWindow,

        // ==================================================================
        //  🆕 2026-10-24（引擎线 `G1b` · 战斗线 `C2` §6）：**关键词被施加到这张卡上**
        //  ⚠️ **加在这张表的【末尾】**（枚举值只许追加，见上面 `Draw` 那段：插队会让
        //     别处存下来的整数值错位）。
        // ==================================================================

        /// <summary>
        /// 🆕 **一个关键词（状态）被加到了这张卡身上** —— `CardId` = **关键词本身**
        /// （`"huntmark"` / `"armour"` / `"stun"` …），`Player` / `Slot` = **被施加的那张卡**。
        ///
        /// <para>**判据 = 原版 `BattleCardUI.DisplayStatusAnim`**
        /// （`BattleCardUI__DisplayStatusAnim.c`）：它在「某条 effect / trait 被加到这张卡上」那一刻被调
        /// （`CardScript__Stun.c:125` · `CardScript__AddEffect.c:678/:890`），而**没有去重闸**：
        ///   · `TriggerType == 0 (Start)` 那一档**每次都演**（`:78-119`，不进表、不查重）；
        ///   · `TriggerType == 2 (OnPlay)` 那一档**只首次演**（`:121-198`，字典里已有 ⇒ 只归位）。</para>
        ///
        /// <para>**要补的缺口**：我们原来唯一入口 = `SyncBoard` → `TraitFrames.SyncKeywords` 的
        /// **集合对差**（只有「新出现」才演）⇒ **同一个关键词第二次以后一次都不演**。</para>
        ///
        /// <para>🔴 **`Amount` 的约定（防「同一次施加演两遍」，⛔ 别改）**：
        /// `0` = 这次是**首次获得** —— 表现层**不**由本事件演（既有的 `SyncKeywords`「新出现」那条路
        /// 本来就会演）；`&gt; 0` = 施加**之前就已经在身上**（重施加）—— 只有这一档由本事件重演。
        /// 两条路**结构上互斥** ⇒ 与 `PlaySignals` / `SyncBoard` 谁先跑无关，一次施加**恰好演一次**。</para>
        ///
        /// <para>⚠️ **纯表现信号**：本事件**不改任何局面**（关键词在发出点已经写完了）；
        /// 也**不进动作流**（录像存的是「起始条件 + 动作流」，`BattleEvent` 不进 `.json`）。</para>
        /// </summary>
        StatusApplied,

        /// <summary>
        /// 🆕 **2026-10-25（`E1` · 第四段 · W9）：一个关键词（trait）**从**这张卡身上被摘掉了** ——
        /// `CardId` = **关键词本身**（`"markerlight"` / `"stun"` …），`Player` / `Slot` = **被摘的那张卡**，
        /// `Amount` = 摘掉**之前**的值（`0` = 不知道）。
        ///
        /// <para>**为什么要单开一种**（E5 的 W9 请求）：原版 `CardScript.RemoveTrait` 之后卡片会进
        /// **`Expiring State`**（`MarkerlightController.MarkerLightUsed()` 那一支：
        /// `alreadyAttacked = true; Normal off; Expiring on;` —— 判据
        /// `判据/TraitEffectController_钩子层.md` §6.1③ / §6·1 `MarkerLightUsed():88-93`）。
        /// 我们这边原来**没有任何「摘掉了」的信号**（`UnitState.RemoveAll` 直接改字典），
        /// 表现层只能靠 `SyncKeywords` 的**集合对差**猜 —— 而那个对差**分不出**「到期摘掉」
        /// 与「换了一份」⇒ `Expiring State` 永远看不见。</para>
        ///
        /// <para>⚠️ **与 <see cref="StatusApplied"/> 成对**：那条管「挂上」、这条管「摘掉」。
        /// ⛔ **别把两条合并成一条带 `bool` 的事件** —— 两条的 `Amount` 语义不同
        /// （挂上那条的 `Amount` 是「重施加」标记，不是值）。</para>
        ///
        /// <para>⚠️ **纯表现信号**：本事件在**摘除已经发生之后**发，不改任何局面，也不进动作流。</para>
        /// </summary>
        TraitRemoved,

        /// <summary>
        /// 🆕 **一条附魔「效果」被摘掉了**（`Enchantments.Remove` 成功那一跳）——
        /// `Player` / `Slot` = **载体（被摘效果的那张卡）**在棋盘上的格位（不在棋盘上 = `-1`）·
        /// `CardId` = **trait 关键词名**（`"pray"` / `"vulnerable"`，与 <see cref="StatusApplied"/> /
        /// <see cref="TraitRemoved"/> 同一约定：引擎把关键词塞 `CardId`，⛔ 不是 `Keyword`）·
        /// `Amount` = **buffType**（15/16；消费端据此判原版那道闸 `(buffType-15) &gt; 1`）·
        /// `Effect` = 附魔卡名（诊断）· `InstanceId` = 载体实例号。
        ///
        /// <para>⚠️ **与 <see cref="TraitRemoved"/> 不是一件事**：那条是「**关键词**从 `UnitState` 被摘」，
        /// 这条是「**一条附魔 effect** 被摘」—— 两条的 `Amount` 语义也不同（那条是「值」、这条是 `buffType`）
        /// ⇒ 单开（与仓里「别把两条合并成一条带 `bool` 的事件」的纪律一致）。</para>
        ///
        /// <para>⚠️ **纯表现信号**：摘除已经发生之后才发，不改任何局面、不进动作流。</para>
        /// </summary>
        // 🔶 自己设计（非原版）：原版靠单体对象图直接调方法、不经事件；我们开这条窄口给表现层 · 判据 = 报告 §1b（`判据/T4_532与PremiumCampaign_判据.md` §1·②：原版 `ResolveAction` case 0x10 内联，无广播） · 调度台 2026-10-27 裁定
        EnchantmentRemoved,

        // ==================================================================
        //  🆕 2026-10-29（战斗线 C21 · 执行代理 `B96_CB` · 账 921）：**手牌被「抽出来给人看」**
        //  ⚠️ **加在这张表的【末尾】**（枚举值只许追加，见上面 `Draw` 那段：插队会让别处存下来的整数值错位）。
        //
        //  为什么是两条而不是一条：原版**同一个方法**（`CardScript.ShowCardBeingDrawn(float stepTime)`）
        //  被两个调用点用**两个不同的常量**调 —— 0.2 与 0.4 —— 而 `BattleEvent.Amount` 是 `int`，
        //  装不下 `0.2f`/`0.4f` 这种浮点时长（装 0/1 就是又造一套自建编码）。
        //  ⇒ 时长由 **Kind 自己**表达（每个 Kind 一个 `CardFeel` 常量），消费端不必解码、也不会错档。
        //  ⛔ 别合并成一条带 `bool` 的（与本文件 `StatusApplied` / `TraitRemoved` 那次同一条纪律）。
        // ==================================================================

        /// <summary>
        /// 🆕 **弃牌揭示** —— 这张手牌被「抽出来给人看」那一下（`stepTime = 0.2`）。
        ///
        /// <para>**判据 = 原版** `CardScript.ShowCardBeingDrawn`（`decomp_full/CardScript__ShowCardBeingDrawn.c:9-14` 全文）：
        /// `cardState = 9(inHandMoving)` + `SetSortingLayer(HandMoving)` + `if (isPlayer == 0) TurnCardAround(this, stepTime, stepTime)`。
        /// 本档的调用点 = `BattleManager._ResolveDiscardFromHand_d__544__MoveNext.c:95`，实参
        /// `.rdata 0x1834b2bb0` 实读 **0.2f**（我们的常量 = `CardFeel.DiscardRevealStepTime`）。</para>
        ///
        /// <para>**字段**：`Player` = **原持有方**（= 原版 `EntityScript.isPlayer(+0x40)`）·
        /// `Slot` **恒 -1** · `CardId` = 卡名 · `InstanceId` = 这一份的实例号。
        /// ⚠️ 发出点 = `RuleCore.EmitCardBeingDrawn(ctx, seat, inst, isSteal: false)`（**唯一发射口**）。</para>
        ///
        /// <para>⚠️ **纯表现信号**：不改任何局面、不进动作流（`BattleEvent` 不进 `.json`）。</para>
        /// </summary>
        DiscardReveal,

        /// <summary>
        /// 🆕 **偷牌揭示** —— 从对手**牌库**偷来的那张被抽出来给人看那一下（`stepTime = 0.4`）。
        ///
        /// <para>**判据 = 原版** 同一条方法（见 <see cref="DiscardReveal"/>），本档调用点 =
        /// `BattleManager._ResolveStealFromDeck_d__547__MoveNext.c:243`，实参 `.rdata 0x1834b3158` 实读
        /// **0.4f**（我们的常量 = `CardFeel.StealRevealStepTime`）。
        /// 🔴 **与弃牌那一档不同值、⛔ 别统一**（两条链各用自己的字面量，不是笔误）。</para>
        ///
        /// <para>**字段**：`Player` = **被偷那一方**（= 原版 `isPlayer`，因为牌彼时还在他的牌库里）·
        /// `Slot` **恒 -1** · `CardId` = 卡名 · `InstanceId` = 这一份的实例号。
        /// ⚠️ 发出点同 <see cref="DiscardReveal"/>（`isSteal: true`）。</para>
        /// </summary>
        StealReveal,

        /// <summary>🔴 **2026-10-30（`B102_506` · 账 `战斗逻辑驱动:506` R1）：一张破坏卡被造进了某人的手牌、要在中央亮出来** ——
        /// 原版 `BattleManager._ResolveCreateHandCard_d__512__MoveNext.c:100-102` 对 `spellType == 0xe6(230)` 的造牌**直接调**
        /// `CardScript.DisplayEnemyCardInCenter(targetCard, timeToDraw: 0.3f, stepTime: 0.3f, changeVisibility: true)`（原版**没有广播**，这条是我们跨层用的窄口）。
        /// **字段**：`Player` = 造牌方 · `Slot` 恒 `-1`（展示的是**卡**、不是单位）· `CardId` = 卡名 · `TargetPlayer` = **收牌方**（卡落进谁的手里 ⇒ 从那一侧手牌飞出）。
        /// 发出点 = `EffectResolver.DoCreate` 的 `case "hand" / "enemyhand"` 破坏那一支（唯一）。⚠️ 纯表现信号：不改局面、不进动作流。
        /// 🔴 但它进 `ctx.Events.Count` ⇒ **改 `NetProtocol.Fingerprint`**（须与一次 `play check` 重钉同批）。</summary>
        SabotageCreated,
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
        /// （`DoReanimate` 的判据）；表现层按「只响落地两条」处理（B3 报告 §三·2 的消费约定）。
        ///
        /// <para>🔴🆕 **2026-10-24（引擎线 `W12`）：这一档在引擎里还多一条语义 —— 它让
        /// `RuleCore.DeployFreeCore` **整组跳过部署广播**（`When deployed` 的 195 / `When you deploy …`
        /// 的 190 / 常驻部署效果 / `UseFaith` 605）。判据 = 原版 `ResolveTransformFromRemnant` 的回调清单
        /// （`…DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31`，**整条链没有
        /// `ResolveUnitSummoned` / `BroadcastUnitSummoned`**）⇒ 逐句写在 `DeployFreeCore` 那段长注释里。</para></summary>
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
        /// 「**只有 `Attack` 用**」—— **已过期**。现在**四种都填**：
        /// `Attack`（跨半场，**必填**）· `Play`（**卡面要求选目标**的战术卡才填）·
        /// `Ability`（`EffectTargets.NeedsPick` 的技能才填）·
        /// 🆕 `Retaliation`（**语义与 `Attack` 相反**：`Player/Slot` = **守方**、`Target*` = **攻方**，
        /// 见 <see cref="EvtKind.Retaliation"/> 的 doc）。
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
        /// `card.Has(Ambush)`（🔴 **2026-10-26 订正（B9 `A3`）**：原版只判 trait、**不要求**正文，旧写「且 `TriggerOps` 非空」已作废）。它同时也是
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

        /// <summary>
        /// 🔴🆕 **2026-10-25（执行代理 `E4` · `E4-3`）：这一条 <see cref="EvtKind.Hit"/> 是不是
        /// 易伤那【第二拍】**（原版 `CardScript._ReceiveDamage_d__381__MoveNext.c:188-257`）。
        ///
        /// <para>**原版是两拍、两条 `DamageInfo`**：主伤害那一条走完（`:188` 之后血已扣），
        /// 若目标还活着且带 `CurrentVulnerable > 0`，就再造一条
        /// `DamageInfo(actingCard, damage = CurrentVulnerable, type = (本次 type==Attack(4)) + 7)`
        /// 交给 `BattleManager.DealMultiDamage(..., priority:2)`（`:226` / `:252-254`）——
        /// 它**再走一遍** dodge/shield/invuln → 堡垒 → 护甲 → 血，所以第二拍落到血上的是
        /// `max(1, 易伤 − 护甲)`（我们的 `RuleCore.VulnerableDamagePart`）。
        /// 时长上：主拍 `_ReceiveDamage` = **1.0 s**、易伤拍 = **1.4 s**（`:291-303` 的 0.4 不跳过 1.0）
        /// ⇒ 合计 **2.4 s**（判据全表 → `资料/普查产出_1025/J3_判据_战斗剩余四条.md` §3）。</para>
        ///
        /// <para>**为什么要这一位**：我们的引擎原来把易伤那份**折进**同一条 `Hit` 的 `Amount` 里、
        /// 只发一条事件 ⇒ 表现层（`EventTiming.HitIsVulnerable`）只能按「受击方现在带不带 `vulnerable`」
        /// 猜，于是**唯一的**那条被排成 1.4 s（主拍那 1.0 s 根本没建模）。
        /// 现在引擎发**两条** `EvtKind.Hit`：第 1 条 = 主伤害（本字段 `false`）、第 2 条 = 易伤那份
        /// （本字段 `true`，`Amount` = <see cref="RuleCore.VulnerableDamagePart"/>）。
        /// ⇒ 表现层应当**只按本字段**判 1.4 s（⛔ 别再按「受击方带不带 `vulnerable`」猜，
        /// 那会把主拍也排成 1.4）。</para>
        ///
        /// <para>⚠️ 只有 `Hit` 会填；别的种类**保持 `false`**（⛔ 别拿它当「这次伤害含易伤」的判据 ——
        /// 那是两条事件之间的关系，不是单条事件的属性）。</para>
        /// </summary>
        public bool IsVulnerableSecondHit;

        /// <summary>`Ability` / `Trigger` 专用：哪个关键词（`rally` / `slay` / `ability`…）</summary>
        public string Keyword;
        /// <summary>`Ability` / `Trigger` 专用：效果原文（`Damage 2 EnemyUnit`）；
        /// 🆕 `ShowCardsWindow` 也用这一位 —— 装的是**标题键**（`Card_Abilities/&lt;effectText&gt;`）。</summary>
        public string Effect;

        /// <summary>
        /// 🆕 **2026-10-24（`ShowCardsWindow` 专用）：这一窗要摊开的全部卡名**（原版列表序；`null` = 没写）。
        ///
        /// <para>**判据**：原版 `BattleManager__AddShowCardsWindowAction.c` 把**整个 `List`**
        /// 打进 `BattleAction`（逐项 `List.Add`），`ResolveAction` case `0x56` 再整份交给
        /// `BattleHud__DisplayMultiCardView` ⇒ 是**一批**，不是一条事件一张卡。
        /// 我们的 `BattleEvent` 是标量结构，所以单开这一格装批（⛔ 不要改成「一张卡发一条事件」——
        /// 消费端要能一眼看出**这一窗总共几张**，否则它得自己猜组边界）。</para>
        ///
        /// <para>⚠️ 别的种类**保持 `null`**（今天只有 `ShowCardsWindow` 会填）。
        /// ⚠️ 它不进录像 / 联机（`BattleEvent` 不进 `.json`，见 `DeploySource` 那段同一条说明）。</para>
        /// </summary>
        public System.Collections.Generic.List<string> CardNames;

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
        /// 🆕 **`Heal` 专用：这次治疗是不是 Syphon（吸魂）**（原版 `HealType.Syphon = 1`）。
        ///
        /// <para>**判据 = 原版** `CardScript._Heal_d__468__MoveNext.c:201-225`：
        /// 治疗特效（`BattleAnims.heal` → prefab **`Healing_Circles`**）**只在 `HealType.Normal(0)`
        /// 那一支**实例化；Syphon（`CardScript__ResolveDamageDealt.c:49-59` 的 `HasCurrentTrait(0x6e)`
        /// → `HealOneCharacter(..., healType = 1)`）**有飘字、无治疗圈**。</para>
        ///
        /// <para>⚠️ **生产侧今天没有任何发出点会把它置 `true`**：我们引擎**没有 syphon 词条/机制**
        /// （`KeywordTable` 无此键、卡池现读 0 张带它）⇒ 这一位是「Syphon 落地那天」的接口，
        /// 另加自检覆盖（`BattleDriver.GenericVfxFor` 对 `HealSyphon` 回 `null`）。
        /// ⛔ **不许拿它当「这次治疗要不要显示」的开关** —— 两种治疗都显示 `+N`。</para>
        /// </summary>
        public bool HealSyphon;

        /// <summary>
        /// 伤害 / 治疗的数值。护盾挡下 = 0。
        /// 🔴 **2026-10-20（`W58`）就地订正（铁律 5）**：这一行原来写「**负数 = 治疗**
        /// （`Regeneration` 回合结束回血走这条）…… 卡面效果造成的治疗走 `heal` 那条 op，不发这个事件」——
        /// **前半句已过期**：再生 / 猎杀标记 / `heal` op / `heal` 能力**现在全部发
        /// <see cref="EvtKind.Heal"/>**（`Amount` = 实际治疗量，正数）。
        /// 负数 `Hit` 今天**只剩一个发出点 = 幸存者的 `BroadcastUnitHealed` 广播**
        /// （`RuleCore.cs` 的 `CleanupDeaths`），**表现层不显示它**（原版 Survivor 不飘治疗字）。
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
                case EvtKind.Retaliation:
                    // 🆕 2026-10-22（`W64`）：主语 = **守方**、`Target*` = 攻方（见 `Retaliation` 的 doc）
                    sb.Append(Ranged ? " 远程反击→" : " 近战反击→")
                      .Append('P').Append(TargetPlayer + 1).Append('@').Append(TargetSlot);
                    break;
                case EvtKind.Hit:
                    sb.Append(" 受 ").Append(Amount).Append(" 伤");
                    break;
                case EvtKind.Heal:
                    sb.Append(" 回 ").Append(Amount).Append(" 血")
                      .Append(HealSyphon ? "（Syphon）" : "");
                    break;
                case EvtKind.Ability:
                case EvtKind.Trigger:
                    sb.Append(" [").Append(Keyword).Append("] ").Append(Effect);
                    break;
            }
            return sb.ToString();
        }

        /// <summary>
        /// 🆕 **2026-10-27（执行代理 `B9_W54ENG2`）：这一条 `Ability` 事件要播的**目标列表**
        /// —— 原版 `AbilityLogic.PlayAbility` 里 `abilityTargets = GetTargets(...)` 的结果
        /// （判据 = `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:262`；`PlayAnim.c:194/203/215`
        /// 再**逐目标**各解一次位置、`:217-222` 只有最后一个目标带 `waitAnimToApplyEffect`）。
        ///
        /// <para>**为什么要这一位**：我们原来只有 <see cref="TargetPlayer"/> / <see cref="TargetSlot"/> /
        /// <see cref="TargetCardId"/> **一个**目标三位组 ⇒ 多目标能力只能播一份，而且拍序是
        /// 「每目标 × 全部拍」而不是原版的「每拍 × 全部目标」
        /// （缺口分析 → `资料/完全复刻/报告/B9_W10BDR_施工单8-9-12.md` §#9 `AbilityLogic.PlayAnim`）。</para>
        ///
        /// <para>⚠️ **与那三位组的分工（两件事，⛔ 别合并）**：`TargetPlayer` 那三位 =
        /// 「**玩家点没点目标**」的判别式（原版 `CemeteryManager.GetActionText` case `0xF` 读
        /// `+0x28` 那个引用是不是 `null` ⇒ `ActionTargetedAbility` / `ActionAbility` 二选一）；
        /// 本列表 = 「**这次结算实际落到哪些牌上**」（原版 `GetTargets` 自己选的也算 ——
        /// 例如目标选择方式是「随机一个」时）⇒ 一个玩家**没点**目标的能力，本列表**仍可能有 1 条**。</para>
        ///
        /// <para>⚠️ **`null` = 没写**（哨兵，与 <see cref="CardNames"/> 同一条约定）；
        /// **空表** = 这一条能力没有目标（`Draw` 那一类）。今天只有 `EvtKind.Ability` 会填
        /// （`RuleCore.EmitAbilityEvent` 一处，⛔ 别在别处再拼一份）。
        /// ⚠️ **长度 = 目标个数**：今天我们的封闭文法（`EffectSpec` / `EffectTargets`）**每条能力至多一个目标**
        /// ⇒ 现读只会是 **0 或 1**；多目标档要等文法扩档，本字段是那一层的**接口**（⛔ 别按它现在的长度
        /// 反推「原版也只有 1 个」）。
        /// ⚠️ 命名避开 `CardDef.AbilityTargets`（那是**卡表的 `abilityTargets` 判据行**，两回事）
        /// 和 `ctx.AbilityTargetsSaved`（那是**一次 play 里所有 op 目标的并集** = 原版 `abilityTargetsToSave`）。
        /// ⚠️ 它**不进录像 / 联机**（同 <see cref="CardNames"/>：`BattleEvent` 不进 `.json`）。</para>
        /// </summary>
        public System.Collections.Generic.List<BattleEventTarget> AbilityTargetList;

        /// <summary>
        /// 🆕 **`EvtKind.Ability` 专用：这条能力的原版 `priority`**（`CardAbility.priority(+0x18)`，
        /// 即 `ActionPriority`：`0 Player / 1 Instant / 2 Interrupt / 3 DelayedInstant`）。
        ///
        /// <para>判据（逐条）：
        /// ① 原版 `BattleManager.AddCardAnim(…, int priority, …)` 的两处 `AbilityLogic.PlayAnim` 调用点
        ///    传的是 **`AbilityData.priority`**（`decomp_full/AbilityLogic__PlayAnim.c:223` 的
        ///    `*(undefined4 *)(param_3 + 0x44)`，`param_3 = AbilityData`）；
        /// ② `AbilityData.priority` 由 `RawCardScript.TriggerAbility` 从 **`CardAbility.priority`** 灌入
        ///    （`decomp_full/RawCardScript__TriggerAbility.c:32` 的 `uVar11 = *(uint *)(param_2 + 0x18)`）；
        ///    主动技那一支是**固定 1**（`RawCardScript__ResolveActiveAbility.c:47` 的 `CONCAT44(uVar7,1)`）；
        /// ③ 缺省 = `1`（`CardAbility__ctor.c:5` 写 `+0x18 = 1`）—— 所以**填 1 与不填在语义上不等价**
        ///    （不填 = 0 = `Player` 道 = 消费序**最后**），但**在本字段接入之前**所有事件都不填 ⇒
        ///    表现层看到的**仍然是 0**，与改动前逐字等价（零回归是硬要求）。</para>
        ///
        /// <para>消费端 = `BattleDriver` 把 `PlaySignals` 的每条信号排进时间线时带
        /// `PendingSignal.lane = e.Priority` ⇒ 同一时刻多条时按 `AnimLaneRank`
        /// （`2→1→3→0`）定先后；`at` 不同时仍是绝对时刻优先（我们本来就比原版细）。</para>
        ///
        /// <para>⚠️ **纯表现提示位，不进录像 / 联机**（同 <see cref="CardNames"/> / <see cref="AbilityTargetList"/>：
        /// `BattleEvent` 不进 `.json`）。⚠️ 别的 `EvtKind` 不填这一位（保持 0）。</para>
        /// </summary>
        public int Priority;
    }

    /// <summary>
    /// 🆕 **2026-10-27（执行代理 `B9_W54ENG2`）：<see cref="BattleEvent.AbilityTargetList"/> 的一项**
    /// —— 原版那个列表的元素是 `CardScript`（**实体引用**），我们的等价物 = 身份四元组。
    ///
    /// <para>⚠️ **必须四样一起带**：`Slot` 不是身份（单位离场/换位时格号会变，见
    /// <see cref="BattleEvent.InstanceId"/> 的 doc），`InstanceId` 才是稳定号；
    /// `CardId` 是留档用的（回看时那个格位可能已经换人了，同 <see cref="BattleEvent.TargetCardId"/>）。
    /// `Slot = -1` = 不在棋盘上（本引擎的督军在 `BoardSpec.WarlordSlot` 上，所以正常不会有这种情况）。</para>
    /// </summary>
    public struct BattleEventTarget
    {
        public int Player;
        public int Slot;
        public string CardId;
        public int InstanceId;
    }
}
// ==========================================================================================
//  ⚠️ **`EvtKind`（现读 23 个成员；⛔ 别写死条数，`RuleEngineTest` 那条计数断言才是真值）不是「原版广播族」的对应物 —— ⛔ 别拿两边的条数比**
//  （2026-10-26 · 完全复刻批次 8 · 执行代理 `B8_R` 查证，判据见下；2026-10-27 `E7ENG` 加 `EnchantmentRemoved` 后就地订正「20 → 21」）
// ==========================================================================================
//  起因：一轮只读普查怀疑「`EvtKind` 现读 20 个成员、而原版广播有 35 种 ⇒ 这是
//  『广播族升 A』的真缺口」。**查证结论：那是两条不同的轴，条数无可比性。**
//
//  · **原版那一侧是「监听者派发器」，不是事件枚举**：
//    `BattleManagerSupport__Broadcast*` **43 个**（🥇 `d:/2/tools/decomp_full/`）＋
//    `BattleManager__Broadcast*` **30 个**（其中 **24 个是纯转调**到 Support 那一份）＋
//    `MinionManager__BroadcastActivateMinion` **1 个**。它们的做法是逐个
//    `CardScript.ReactTo* / Trigger* / Resolve*` 调下去，最终由
//    `RawCardScript.OnTrigger(id, …)` 带出 **57 个不同的 `AbilityTrigger` id**（id → 名字全表 =
//    `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs`）。
//    ⇒ **原版根本没有「事件种类枚举」这种对象**；「35 种」是某种数法下的中间数，不是可对账的口径。
//
//  · **我们的对应物是 `WhenEventKind`**（29 种 + `triggers:<kw>` 家族，见 `Core/WhenEvent.cs`）
//    ＋ `EffectResolver.BroadcastWhen(kind, …)`（60 余个调用点）。**那本账** =
//    `资料/完全复刻/账/战斗逻辑驱动.tsv` 里 `BattleManagerSupport.Broadcast*` 那 43 行。
//
//  · **`EvtKind` 是【表现层词汇】**（哪个时机能播什么），消费端 = `CardPresentation/Core/VfxMap.cs`
//    ＋ `CardPresentation/Battle/BattleDriver.cs`。原版没有对应枚举 ⇒ 给它加成员必须**同时**
//    有发射口与消费口，否则是死代码（`VfxMap.cs` 的 `TriggersPrefix` 一节明写
//    「⛔ 不许照着原版 trigger 名字表补齐 —— 加了 = 永远等不到」）。
//
//  · **原版那 57 个 trigger id 今天全都走同一条通用事件 `EvtKind.Trigger`**
//    （「哪个关键词」装在 `CardId`／`Keyword`，见 `BattleEvent.CardId` 的约定）
//    ⇒ 表现层不缺「种类」，缺的是 **`WhenEventKind` → 原版 trigger 名的映射**；
//      那一处 = `EffectResolver.TriggerNameOfWhenKind` ＋ `WhenEvents.ExtraTriggerNameForKind`
//      （2026-10-26 已由 `B8_C` 补 `kills`→`Slay(120)` / `losestealth`→`UnitLoseStealth(647)`）。
//
//  · **可达性结论（现读 `RuleEngine/Resources/cards_engine.json` = 1,128 张）**：
//    原版**未实现**的那几条广播 trigger —— `155 TurnSetup` · `160 TurnStarted` ·
//    `210 CardCreatedInHand` · `270 TrapResolved` · `290 UnitPoisoned` · `430 Requiem` ·
//    `185/187` · `250` · `15` · `641` · `550` · `230` —— **命中卡数全部是 0**
//    ⇒ 今天**没有任何一张卡**会因为「补上它们」而从『不响』变『响』。
//    它们是**休眠机制**（铁律 11：休眠机制也要做），但**不构成本账升 A 的判据**。
//
//  逐条去向（原版 49 条 `Broadcast*` 各自的跳数 / trigger id / 我们落点 / 可达卡数）与
//  「哪几条我们有真跳数差异」→ `资料/完全复刻/报告/B8_R_事件族补全.md`。
//  账补丁 → `资料/完全复刻/账/_parts/B8_R.tsv`（`BattleManager.Broadcast*` 那 30 行，键 = (原版类, 方法)）。
