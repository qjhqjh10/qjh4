// PlayerState.cs — 一方玩家的全部状态
using System.Collections.Generic;

namespace RuleEngine
{
    public class PlayerState
    {
        public string Name = "Player";

        // 🔴 **2026-09-18（待办第 7 行 · 第 2 步）：三个区域的元素类型从 `CardDef` 换成了
        //    `CardInstance`** —— 手牌/牌库/弃牌堆里存的是**具体的那一份**，不再是卡模板。
        //    这样「只手牌里这一张降费」「同名两张里只有这一份临时」才做得到。
        //    · **「新造一张」**（初始牌库 / `create a copy` / 造衍生物 / 天赋生成）→
        //      必须 `ctx.NewInstance(card)` 发**新的一份**；
        //    · **「挪一份」**（抽牌 / 打出 / 回手 / 洗回牌库 / 复活的还是那一张）→
        //      **把同一个 `CardInstance` 搬过去**，不要再发新的。
        //    用户 2026-09-18 拍的口径：回手 / 洗回牌库**默认保留实例态**、变身另发新实例
        //    （见 `CardInstance.cs` 文件头）。
        public readonly List<CardInstance> Deck = new List<CardInstance>();
        public readonly List<CardInstance> Hand = new List<CardInstance>();
        public readonly List<CardInstance> Discard = new List<CardInstance>();

        /// <summary>
        /// 🆕 **2026-10-18（`A885` ② · `W4` 整改 · 审查问题 8 / 账 5）：这一方手牌上「还在生效」的
        /// 手牌效果【记录表】** —— 原版 `PlayerHand.activeEffects // +0x48`（`List&lt;HandEffect&gt;`）。
        ///
        /// **它解决的是哪一件事**：原版 `PlayerHand.SetupCardInHand`（`:38`）遍历的是**这张记录表**，
        /// 不是手牌本身 ⇒ **某条效果的载体全部离开手牌之后，记录还在**，后来进手牌的牌**照样**吃得上。
        /// 我们原来是从手牌实例上**现场扫**（`HandEffectRegistry`）⇒ 那一刻登记表上就看不见它了
        /// （`Beast Snagga Nob` 给手牌 +1、手里那张唯一的 Beast 被打出去之后，新抽到的 Beast 就吃不上）。
        ///
        /// 🔴 **它不是「第二份状态」**：表里装的就是**逐张牌身上那一条条 `HandEffect` 本身**
        /// （**同一批对象引用**，`RuleCore.AttachHandEffect` 建好后 `Add` 进来），
        /// 不是拷贝、不是另一份载荷。⇒ 不会出现「两处各存一份、迟早不一致」。
        /// ⚠️ 与此不同：2026-10-18 早先删掉的那张**对局级** `BattleContext.HandBuffs`
        /// （`class HandBuff { Instance; Ops; Source; }`）是**逐实例的载荷副本**，那才是错的形状
        /// —— ⛔ **别把它复活**。
        ///
        /// **生命周期**：
        ///   · **加** —— 🔴 **2026-10-18（`G8`）就地补全**：**两个**入口都会登记，**都按 `RecordId` 去重** ——
        ///     ① `RuleCore.AttachHandEffect`（**找得到载体**那条：建好记录 → 贴到那张牌上 → 登记一次）；
        ///     ② `RuleCore.RegisterHandEffectRecord`（⚠️ **无载体那条路** —— `G3` 2026-10-18 新加的：
        ///        **只登记、不贴牌**，因为它内部就是拿 `inst == null` 调 ①）。
        ///     ⛔ 只写 ① 会让「挂载那一刻手里没有符合条件的牌」那一路（`GOF81 Beast Snagga Nob` 那种）
        ///     看起来没人登记 —— 而**那正是这一格要修的形状**（记录先于贴牌，见 ② 的注释）；
        ///   · **移** —— 只有「**这条效果的次数用尽**」时移（`RuleCore.RemoveHandEffectFromHand`，
        ///     原版 `PlayerHand.RemoveHandEffectAt`）；
        ///   · ⛔ **「那一份牌把效果兑现掉了（兑现即摘）」不移这张表** —— 原版那条记录也还在。
        /// </summary>
        public readonly List<CardInstance.HandEffect> HandEffectRecords =
            new List<CardInstance.HandEffect>();

        /// <summary>
        /// **「这一回合从牌库抽到的牌」的账记在哪儿**（待办第 7 行 · 第 3 步，2026-09-18 搬完）。
        ///
        /// 🔴 **这里原来有一个 `Dictionary&lt;CardDef,int&gt; DrawnThisTurn` 字段 —— 已删。**
        ///    它就是「没有卡实例身份」时代的近似：原名注释自认
        ///    「同名两张里抽到一张、打出另一张也会算抽到的那张」。
        ///    现在这个位是 <see cref="CardInstance.DrawnThisTurn"/>（**一份一个布尔**）：
        ///    `RuleCore.Draw` 置位 · `BeginTurn` 按本方区域清 · `PlayCard` 判传送（`Teleport`）时就地清。
        ///
        /// ⚠️ **更正痕迹**（同一天早些时候这里写过一句反话）：2026-09-18 上午这一格曾写着
        ///    「`DrawnThisTurn` **已删**、现在是 `CardInstance.DrawnThisTurn`」而**当时那句话是错的**
        ///    —— 那天先试了一版实例化、写完注释后**整版回退**（爆炸半径太大），注释没跟着回退。
        ///    ⇒ 下午真正做完之后，这句才成立。**留个痕：同一句话在一天里既假又真，别只信文字、要看字段。**
        /// </summary>

        /// <summary>战场 9 格。<see cref="BoardSpec.WarlordSlot"/> 上永远是督军，其余为 null 或单位</summary>
        public readonly UnitState[] Board = new UnitState[BoardSpec.Size];

        /// <summary>和 <c>Board[BoardSpec.WarlordSlot]</c> 是**同一个对象**（便于直接取用，别写成两份）</summary>
        public UnitState Warlord;

        public int Energy;
        public int MaxEnergy;

        // ---- 两套阵营资源（2026-09-13 第三十三轮）------------------------------------
        // 原版里它们都是 `PlayerManager` 上的**每玩家一个 int**：
        //   · 信仰 `faithMana`（`PlayerManager.cs:87` / `GetCurrentFaith():147` / `AddFaithMana(int):212`）
        //   · 灵魂石 `spiritStoneMana`（`:85` / `GetCurrentSpiritStone():142` / `AddSpiritStoneMana(int):208`）
        //     —— 它其实是**第二种能量货币**（`ManaType{ Normal=0, SpiritStone=5 }`）。
        //
        // ⚠️ **数值口径是用户 2026-09-12 亲口定的，别再去找「初始值 / 上限 / 增长表」**（那是白找）：
        //   · **信仰**：**没有上限、不会衰减**；只由「触发效果累加」涨，卡面在**达到阈值**（3 / 5 / 9 这种）
        //     时给更强的效果（规则书 `:184`「总信仰达到指定值时触发效果」）。⚠️ **不是货币，不花掉** ——
        //     `ResetMana(...)` 里**没有** `initialFaith` 参数，`ManaType` 里也没有 Faith。
        //   · **灵魂石**：**没有初始值、没有上限、没有每回合增长**；只由「路标石单位死亡 +1」
        //     （规则书 `:210`/`:225`）和卡面效果（`Gain N Spirit Stones`）增减，**扣减完全由效果决定**。
        //     ⚠️ 它**是**货币（可以 `Spend all your Spirit Stones`），所以和信仰在这一点上不一样。
        public int Faith;
        public int SpiritStones;

        /// <summary>
        /// **任务点**（暗黑天使的阵营资源，2026-09-13 第三十三轮）。
        ///
        /// 怎么查出来的：那批 DarkAngels 卡（`Ravenwing Champion` / `The Rock` / `Techmarine` /
        /// `Wages of Retribution` / `Reconnaissance Mission` / `Watcher in the Dark` …）的卡面
        /// 在「Gain N」后面画的都是**同一个锯齿圆环＋数字的图标**（图集 sprite `questPointsN`），
        /// 而 OCR 把图标丢了、只留下 `Gain 1` / `Gain 3` —— 有的还被误标成 `[Energy]`。
        /// 这**正好解释**了「为什么原版只给暗黑天使显示任务点图标」
        /// （`BattleDriver.ShowsQuestPoints` 记的机器码级出处：`cmp [督军+0x2c],0x6e` = DarkAngels）。
        ///
        /// 规则书 `:199`：「每获得 **3** 点任务：向牌库加入 1 张隐秘并洗牌」—— 判据见
        /// `RuleCore.DoFactionResource`（**只此一处**）。所以 HUD 上显示的是 `X/3`。
        /// </summary>
        public int QuestPoints;
        /// <summary>任务点**已经结算过几次**阈值（每满 3 点一次）。只由
        /// `RuleCore.QuestPointThreshold` 读写 —— **阈值判据只此一处**。</summary>
        public int QuestMilestone;

        /// <summary>
        /// 🆕 2026-10-17（B24）：**本局「打出过的隐秘（Secret）」张数** —— 一条**独立计数器**。
        ///
        /// 用处只有一个：卡面 `For each Secret you played this game`（实测全池只 1 处：
        /// `Relic Munitions`（DarkAngels 战术卡）的 `Repeat for each Secret you played this game`）。
        ///
        /// 🔴 **判据全在反编译，别拿 `BattleContext.PlayedCards` 顶替**（那是**另一件事** ——
        ///    它记的是「本局打出过的牌」这张流水表，尺寸/清点/用途都不同）：
        ///   · **字段** = `PlayerManager.secretsPlayed`
        ///     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/PlayerManager.cs:93`，类型 `ObscuredInt`，
        ///      实例偏移 **+0xEC** —— 读见 `PlayerManager__GetSecretsPlayed.c:16-25`）；
        ///   · **写点（全库唯一）** = `BattleManager._ResolvePlayCardFromHand__MoveNext.c:1267-1274`：
        ///     `EntityScript.get_spellType(card) == 0x96`（= **150 = Secret**）时调
        ///     `PlayerManager.AddSecretPlayed()`（`PlayerManager__AddSecretPlayed.c` 就是
        ///     `ObscuredInt.op_Increment`）—— 位置与 `AddPlayCardAction` 同一段协程
        ///     ⇒ **「从手牌打出」才算；「生成」一张不算**（那是 `BroadcastSecretCreated` 那条路）；
        ///   · **读点（全库唯一）** = `TargetsAffected.secretsPlayed = 245`（`TargetsAffected.cs:40`）
        ///     → `AbilityLogic__GetTargets.c:523`（那一支拿它当**循环次数**用）；
        ///   · **算谁的**：取**施放者那一方**的 `PlayerManager`（`GetPlayerManager(…, card+0x40)`）
        ///     —— 我们这边就是 `CountFor(ctx, owner, …)` 的 `owner`。
        /// ⚠️ 原版**只有这一个**「本局打出过某类牌」计数器（`TargetsAffected` 枚举里没有破坏卡、
        ///    也没有通用的那种）⇒ 别的引用词（`sabotage` / `any`）在 `CountFor` 里照旧**如实报
        ///    「数不出来」**，**不许拿这张数去顶**。
        ///
        /// 写点 = `EffectResolver.PlayTactic` · 读点 = `EffectResolver.CountFor` 的
        /// `CountScope == "played"`（**各一处**）。
        /// </summary>
        public int SecretsPlayed;

        /// <summary>本方自己的回合计数（能量 = 它 + 1）。**不是全局回合数** —— 见 RuleCore.BeginTurn</summary>
        public int TurnCount;

        /// <summary>🆕 2026-09-26：**上一次自己回合结束时没花完的能量结转**（遭遇模式的 `manaAccumulation`）。
        /// 经典恒 0（`GameplayVariables.manaAccumulation = 0` ⇒ 这条路一次都不走）。
        /// 🔴 判据（2026-10-26 `W12MN`/`W15MN` 实读）= `decomp_full/BattleManager__UpdateManaLeftAccumulation.c:51-57`：`SetSavedMana(min(manaAccumulation(+0x40), GetUnusedMana()), 1)`，`+0x40 == 0` 时**整条早退不写** ⇒ 我们那一行整数域**逐项等价**（原文引的非官方规则书 `:60` 已作废）。
        /// ⚠️ 语义 = **一次性结转**（下回合最大能量 +它、之后清零）＝ 原版 `_NextTurn:344-352` 的 `RefillCustomMana(saved)` + `SetSavedMana(0)`；今天只有遭遇真的用上（`manaAccumulation = 1`）。</summary>
        public int ManaCarry;

        /// <summary>
        /// 本方**最近一次回合开始时**的全局回合号（由 `RuleCore.BeginTurn` 写）。
        ///
        /// 用途只有一个：划出 `Choose a friendly troop that died **since your last turn**`
        /// 的窗口 —— 候选 = `DeadUnits` 里 `Owner == 自己 &amp;&amp; DeathTurn >= LastTurnStartMark`。
        /// 这样「自己回合里死的」和「对手回合里死的」都落在窗口内，而**上一个回合之前死的**被排除。
        /// 我们上一版 Godot 复刻 `rule_core.gd:1006-1010` 用「`_died_base_prev` 计数 + 切片」记同一件事
        /// （⚠️ 那是**我们自己**的复刻、**旁证**，不是原版；⚠️ 2026-10-18 更正：原来写「**原版**
        /// `rule_core.gd:1006-1010`」）。
        /// </summary>
        public int LastTurnStartMark;

        /// <summary>牌库抽空后每抽一次 +1，并让督军挨这么多伤害</summary>
        public int Fatigue;

        public bool IsDefeated { get { return Warlord == null || Warlord.Health <= 0; } }

        public UnitState At(int slot)
        {
            return BoardSpec.IsValid(slot) ? Board[slot] : null;
        }

        /// <summary>场上活着的单位（不含督军）</summary>
        public IEnumerable<UnitState> Units()
        {
            for (int i = 0; i < Board.Length; i++)
            {
                var u = Board[i];
                if (u != null && !u.IsWarlord) yield return u;
            }
        }

        /// <summary>
        /// 场上活着的单位（不含督军），**按进场序**（`UnitState.PlayOrder`）—— 原版
        /// `MinionManager.GetMinionList` 的等价物（🆕 2026-10-25 · 完全复刻 · 执行代理 A · 账 2a）。
        ///
        /// <para>**判据（第一权威，现读）**：`MinionManager__GetMinionList.c` 先把
        /// **左侧列表 ++ 右侧列表**（各自 `IsInPlay()` 过滤）收进来，末尾
        /// `Enumerable.OrderBy(card =&gt; card.orderInPlay)` 再 `ToList()` ——
        /// 即**按入场序号升序**（不是格号序）。</para>
        ///
        /// <para>⚠️ **我们没有 `orderInPlay` 这个名字，但有同一件事**：`UnitState.PlayOrder`
        /// （`ctx.TakePlayOrder()` 在部署那一刻发号，判据 → `UnitState.PlayOrder` 的 doc）。
        /// 没取号的（`-1`：自检夹具 / 合成单位）**排在末尾**（`StableSortByPlayOrder` 的口径）。</para>
        ///
        /// <para>🔴 **原版 `GetMinionList` 只有 3 个调用点**（全量反编译逐条核过），
        /// 这就是本方法存在的全部理由 —— ⛔ 别在别处「顺手」改枚举顺序：
        /// ① `BasicBattleEndSequence` / ② `DestroyUnitsAfterBattleEnd`（**收局摧场的演出顺序**，表现层）；
        /// ③ `_ResolveForceAttackFriendly_d__506__MoveCount.c:39-59` —— 取「这一方所有在场小兵」
        ///     **减掉施放者自己**，然后 `GetRandomInt(0, count)` **随机挑一个** ⇒ **顺序无关**
        ///     （真正决定结果的是**种群**，不是次序）。</para>
        ///
        /// <para>⚠️ 另一族 `MinionManager.GetUnitList()`（**不排序**、左列表++右列表）的调用点只有
        /// `CardScript.UpdateWhileInPlay`（数 `pack` 的兵力）与 `CheckAndApplyCurrentPack` ——
        /// 那两处都**只看 `.Count`**（顺序同样无关）。⇒ 全仓今天**没有任何一处**依赖
        /// 「格号序 vs 进场序」的差别；本方法只为把判据收成一处、并在将来真需要时不再各写一份。</para>
        /// </summary>
        public List<UnitState> UnitsByPlayOrder()
        {
            var list = new List<UnitState>();
            for (int i = 0; i < Board.Length; i++)
            {
                var u = Board[i];
                if (u != null && !u.IsWarlord) list.Add(u);
            }
            // 稳定排序：`PlayOrder < 0` 的一律当作「最后」（`StableSortByPlayOrder` 同口径）
            list.Sort((a, b) =>
            {
                int oa = a.PlayOrder < 0 ? int.MaxValue : a.PlayOrder;
                int ob = b.PlayOrder < 0 ? int.MaxValue : b.PlayOrder;
                if (oa != ob) return oa.CompareTo(ob);
                return a.InstanceId.CompareTo(b.InstanceId);   // 同号（不该发生）⇒ 定序，保证可复现
            });
            return list;
        }

        /// <summary>
        /// 可部署的空格数 —— 原版 `BattleManager.GetSlotsLeftInBattlefield(bool isPlayer)`
        /// 的等价物。
        ///
        /// <para>**判据（第一权威，逐句）**：
        /// ① `decomp_full/BattleManager__GetSlotsLeftInBattlefield.c:1-19` —— 全文只做
        ///    「按方取那个 `MinionManager`（`+0xe8`/`+0xe0`）+ 空检 + 转调 `MinionManager.AvailableSlotsCount`」，
        ///    自己**不算任何一个数**；
        /// ② `decomp_full/MinionManager__AvailableSlotsCount.c:26-27` —— 唯一的算式
        ///    `(左容量 − 左人数) + (右容量 − 右人数)`（容量 = `left/rightSlotPos.Length`，
        ///    人数 = `left/rightMinions.Count`，即 `+0x20/+0x28` 与 `+0x60/+0x68` 那两个 `Count`）。</para>
        ///
        /// <para>🔴 **2026-10-26（`B8_E`）就地收口**：本方法原来是**第二份**同式实现
        /// （自己数「可部署格且为空」），与 `BoardSlots.RoomOnSide`（`BoardSlots.cs:87`，
        /// 已是 `MinionManager.AvailableSlotsCount` 的 A 档落点）各写一遍 ——
        /// 按工程铁律「两处写同一条规则 = 迟早不一致」**改成转发**。
        /// ⚠️ 两者**逐字等价**（不是「连续无洞时才等价」）：`BoardSpec.IsDeployable` 覆盖的正是
        /// 督军格之外的 **8 格 = 左 4 + 右 4**，而 `CountOnSide` 数的「非空」与 `Board[i] == null`
        /// 互补 ⇒ `(4−左人数)+(4−右人数)` 与「可部署空格数」恒等。</para>
        /// </summary>
        public int FreeSlots()
        {
            return BoardSlots.RoomOnSide(this, BoardSlots.Left)
                 + BoardSlots.RoomOnSide(this, BoardSlots.Right);
        }

        /// <summary>有空的部署格吗（= 两侧合起来还放得下 1 个；唯一判据同 <see cref="FreeSlots"/>，
        /// 转发到 `BoardSlots.IsAvailableSlot`）</summary>
        public bool HasFreeSlot()
        {
            return BoardSlots.IsAvailableSlot(this, 1);
        }

        public override string ToString()
        {
            return $"{Name} 能量 {Energy}/{MaxEnergy} 手牌 {Hand.Count} 牌库 {Deck.Count} 场 {Board.Length - FreeSlots() - 1}";
        }

        // ==================================================================
        //  🆕 2026-10-26（`B8_E`）：「这张牌在哪一个区域」＋「这一方用不用任务点」
        //  —— 四个**原版方法的等价物**。判据逐条见各自 doc；
        //  ⛔ 别在别处再写第二份成员资格判断（`Hand.Contains(...)` / `Deck.Contains(...)` /
        //     `Discard.Contains(...)` 这类散写法应当改成调这里）。
        //  ⚠️ 本段**追加在类尾**（铁律 18·b）：上面那些行号是账里 `A`/`B` 行的落点，插在中间会让它们漂移。
        // ==================================================================

        /// <summary>
        /// 原版 `CardScript.IsInDeck()`（🥇 `decomp_full/CardScript__IsInDeck.c:1-13`，全文 13 行）：
        /// 读状态位 `+0x228`，`== 0`（在牌库）**或** `== 0x10`（16 = 正在从牌库抽出）⇒ 真，其余假。
        ///
        /// <para>**落点 / 机制差异**：原版是**一个 int 状态机**，把「正在抽出」也算在牌库里；
        /// 我们是**一次搬移**（`RuleCore.Draw` 直接把那一份实例从 `Deck` 摘进 `Hand`）⇒
        /// **没有那个中间态**，抽出那一刻两侧会短暂不一致（我们立刻为假、原版在动画期间仍为真）。
        /// 该差异**不可观察**：我们的引擎是同步结算，没有帧间的中间状态；
        /// 「静息态」下（在牌库 / 不在牌库）两侧一一对应。</para>
        /// </summary>
        public bool IsInDeck(CardInstance inst)
        {
            return inst != null && Deck.Contains(inst);
        }

        /// <summary>
        /// 原版 `CardScript.IsInHand()`（🥇 `decomp_full/CardScript__IsInHand.c:1-11`，全文 11 行）：
        /// 状态位 `+0x228` 落在 **{1, 4, 8, 9, 10, 12}** 六档里 ⇒ 真
        /// （1 = 手牌 · 4 = 展示中 · 8 = 离场回手 · 9 / 10 / 12 = 飞向战场那一族过渡档）。
        ///
        /// <para>**落点 / 机制差异**：同 <see cref="IsInDeck"/> —— 原版把 8/9/10/12 那几个
        /// **过渡档**也算「在手」，我们出牌即从 `Hand` 摘除 ⇒ 过渡期为假。
        /// 静息态（1 号档 / 非手牌态）两侧一致；过渡态在同步引擎里不存在。</para>
        /// </summary>
        public bool IsInHand(CardInstance inst)
        {
            return inst != null && Hand.Contains(inst);
        }

        /// <summary>
        /// 原版 `CardScript.IsInCemetery()`（🥇 `decomp_full/CardScript__IsInCemetery.c:1-7`，全文 7 行）：
        /// 状态位 `+0x228 == 6`（inCemetery）⇒ 真。
        ///
        /// <para>**落点 / 机制差异**：原版是**单值状态机**、我们是「三张表之一」的成员资格；静息态一一对应。</para>
        /// 🔴 2026-10-29 就地订正（铁律 5 · R-B1 现核）：`+0x24b` 真名 `shouldCheckIfIsInCemetery` = **永久放逐闩**
        /// （`CardScript__Awake.c:40` 置1 · `__RemoveCardFromCemetery.c:5` 置0 · 全库无写回1）⇒ `GetCardsInCemetery.c:54` 按它过滤 ⇒ 放逐的牌从所有墓地查询消失。原「正在取出 / 可观测为零」**不成立**，账 534 按 **B**。
        ///
        /// 等价物 = 类尾 `_banishedFromCemetery` 闩 + `MarkRemovedFromCemetery` 写口（写口调用方在 F1/F2，未接）。
        /// </summary>
        public bool IsInCemetery(CardInstance inst)
        {
            return inst != null && Discard.Contains(inst) && !_banishedFromCemetery.Contains(inst.Id);
        }

        /// <summary>
        /// 原版 `RawCardScript.UsesQuestPoints()`（🥇 `decomp_full/RawCardScript__UsesQuestPoints.c:5-7`，
        /// 全文 7 行）：`cardArmy(+0x2c) == 0x6e`（= **110 = DarkAngels**）⇒ 这一方用「任务点」
        /// 而不是普通法力槽；消费者 `PlayerManager__ResetMana.c:135` 据此
        /// `ManaManager.ToggleQuestPoints`。
        ///
        /// <para>**判定对象是【督军卡】的阵营**（原版那个消费者读的就是场上的督军）。我们这边
        /// 同一条判据原本只写在表现层 `BattleDriver.ShowsQuestPoints(string faction)`
        /// （`CardPresentation/Battle/BattleDriver.cs:187`，按 `_me` 取督军阵营）——
        /// 本方法把它收到**引擎侧一处**，表现层应当转发过来（⛔ 别再各写一份）。
        /// ⚠️ 与 `EffectResolver.cs:12899` 是同一串 `"DarkAngels"`（那边是
        /// 「有没有任务点那条能力」的判据，同样按督军阵营）。</para>
        /// </summary>
        public bool UsesQuestPoints()
        {
            return Warlord != null && Warlord.Card != null
                && UsesQuestPointsFor(Warlord.Card.Faction);
        }

        /// <summary>任务点那一条判据的**唯一一份**：`cardArmy(+0x2c) == 0x6e`（110 = DarkAngels）。
        /// <para>🥇 `decomp_full/RawCardScript__UsesQuestPoints.c:5-7`（全文 7 行）；消费者
        /// `PlayerManager__ResetMana.c:135` 据此 `ManaManager.ToggleQuestPoints`。</para>
        /// <para>🔴 2026-10-26（B8_X 接线）：表现层 `BattleDriver.ShowsQuestPoints(string)` 现在**转发到本方法**
        /// （常量也转发到 <see cref="QuestPointsFaction"/>）⇒ 全仓这一条判据只有一处（⛔ 别再各写一份）。</para></summary>
        public static bool UsesQuestPointsFor(string faction)
        {
            return string.Equals(faction, QuestPointsFaction, System.StringComparison.Ordinal);
        }

        /// <summary>原版 `+0x2c == 0x6e` 里的那个 `0x6e`（= 110 = DarkAngels）。枚举名见
        /// `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs`。</summary>
        public const string QuestPointsFaction = "DarkAngels";

        // ==================================================================
        //  🆕 2026-10-26（`W12MN` · 账 556/563/565/566/573/574/576/577 那 7 条）：
        //  **原版 `PlayerManager` 的 7 个纯数值取值口** —— 逐一转发到本类字段。
        //
        //  判据（第一权威；7 份 `.c` **全文实读**，2026-10-26）：
        //   · `decomp_full/PlayerManager__GetCurrentFaith.c:1-28`       → `faithMana(+0xB0)` 解混淆返回
        //   · `decomp_full/PlayerManager__GetCurrentSpiritStone.c:1-28` → `spiritStoneMana(+0x9C)`
        //   · `decomp_full/PlayerManager__GetCurrentQuestPoints.c:1-28` → `questPoints(+0xC4)`（**减过阈值的余数**）
        //   · `decomp_full/PlayerManager__GetSecretsPlayed.c:1-28`      → `secretsPlayed(+0xEC)`
        //   · `decomp_full/PlayerManager__GetSavedMana.c:1-28`          → `savedMana(+0xD8)`
        //   · `decomp_full/PlayerManager__GetTotalMana.c:1-28`          → `totalMana(+0x38)`
        //   · `decomp_full/PlayerManager__GetUnusedMana.c:1-28`         → `unusedMana(+0x4C)`
        //  这 7/.c 的**方法体都只做一件事**：把那个 `CodeStage.ObscuredInt` 解混淆后 return，
        //  **没有任何计算 / 上限 / 衰减 / 修正** ⇒ 我们的等价物就是字段本身，一行转发即逐字等价。
        //
        //  🔴 **为什么要有这层转发（而不是让调用方直读字段）**：
        //   ① 施工单 W22 ③ 明写「13 个 getter 直接转发」；
        //   ② 账的 **A 档**要求落点**是方法级声明** —— `工具/check_ledger_points.py` 的
        //      `looks_like_decl` 不认 `public int Faith;` 这种「无 `(` / 无 `{` / 无 `=>`」的字段行
        //      ⇒ 只有转发方法能让那 7 条成为**可机检**的 A（先例：`Deck` 那条能过是因为
        //      `= new List<CardInstance>()` 里带了括号）；
        //   ③ 原版这 7 个名字是对局内读这几条计数的**唯一官方口**；将来把
        //      `EffectResolver` / `RuleCore` / `AiTargets` 里那些**直读字段**的调用方
        //      迁移过来时，一次只改一处。
        //  ⚠️ **今天没有调用点**（消费者仍直读字段）—— 这是**取值口**、不是「死机制」：
        //     它自己不做任何判断，⛔ **别在它里面加规则**（那会变成第二份规则）。
        //  ⚠️ `GetCurrentMaintenance()` **故意没有**：原版那条是 private 且**三源零调用**
        //     （decomp / ILSpy / assets_full 全 0），照铁律 11 例外① 判 `D`；同一条
        //     「维护费合计」的活路径内联在 `RuleCore.BeginTurn`（⛔ 别为它建第二份）。
        //  ⚠️ 本段**追加在类尾**（铁律 18·b）：上面那些行号是账里 A 行的落点，
        //     插在中间会让它们整体漂移。
        // ==================================================================

        /// <summary>原版 `PlayerManager.GetCurrentFaith()`（`decomp_full/PlayerManager__GetCurrentFaith.c:1-28`，
        /// 全文 28 行）：把 `faithMana(+0xB0)` 解混淆后**原样**返回。
        /// 语义 = **无上限、不衰减、不是货币**（不花掉）—— 见 <see cref="Faith"/>。</summary>
        public int GetCurrentFaith()
        {
            return Faith;
        }

        /// <summary>原版 `PlayerManager.GetCurrentSpiritStone()`（`decomp_full/PlayerManager__GetCurrentSpiritStone.c:1-28`）：
        /// 把 `spiritStoneMana(+0x9C)` 解混淆后返回。它**是货币**（`ManaType.SpiritStone = 5`，能花）。</summary>
        public int GetCurrentSpiritStone()
        {
            return SpiritStones;
        }

        /// <summary>原版 `PlayerManager.GetCurrentQuestPoints()`（`decomp_full/PlayerManager__GetCurrentQuestPoints.c:1-28`）：
        /// 把 `questPoints(+0xC4)` 解混淆后返回。🔴 **存的是「减过阈值之后的余数」**
        /// （阈值那一步在 `AddQuestPoints` 内部就减掉了）—— 见 <see cref="QuestPoints"/>。</summary>
        public int GetCurrentQuestPoints()
        {
            return QuestPoints;
        }

        /// <summary>原版 `PlayerManager.GetSecretsPlayed()`（`decomp_full/PlayerManager__GetSecretsPlayed.c:1-28`）：
        /// 把 `secretsPlayed(+0xEC)` 解混淆后返回。唯一读点 = `AbilityLogic__GetTargets.c:523`
        /// （`TargetsAffected.secretsPlayed = 245`，拿它当循环次数）—— 见 <see cref="SecretsPlayed"/>。</summary>
        public int GetSecretsPlayed()
        {
            return SecretsPlayed;
        }

        /// <summary>原版 `PlayerManager.GetSavedMana()`（`decomp_full/PlayerManager__GetSavedMana.c:1-28`）：
        /// 把 `savedMana(+0xD8)` 解混淆后返回。值由两处写：`BattleManager__UpdateManaLeftAccumulation.c:51-57`
        /// （`min(manaAccumulation, unusedMana)`，`manaAccumulation == 0` 整条不写）与
        /// `_NextTurn:296-334`（后手一次性补贴）—— 见 <see cref="ManaCarry"/>。</summary>
        public int GetSavedMana()
        {
            return ManaCarry;
        }

        /// <summary>原版 `PlayerManager.GetTotalMana()`（`decomp_full/PlayerManager__GetTotalMana.c:1-28`）：
        /// 把 `totalMana(+0x38)` 解混淆后返回 = **永久**法力槽数（跨回合保留）。
        /// 我们的等价物 = <see cref="MaxEnergy"/>（原版是累加器、我们每回合从式子重算，
        /// 逐项等价 —— 推导见 `RuleCore.BeginTurn` 的 `MaxEnergy` 式子那一段）。</summary>
        public int GetTotalMana()
        {
            return MaxEnergy;
        }

        /// <summary>原版 `PlayerManager.GetUnusedMana()`（`decomp_full/PlayerManager__GetUnusedMana.c:1-28`）：
        /// 把 `unusedMana(+0x4C)` 解混淆后返回 = **当前可用**能量。
        /// 我们的等价物 = <see cref="Energy"/>（唯一扣口 = `RuleCore.SpendNormalMana`）。</summary>
        public int GetUnusedMana()
        {
            return Energy;
        }

        // ==================================================================
        //  🆕 2026-10-26（`W15MN` · 账 558/562/581/587 那 4 条）：原版 `PlayerManager` 的
        //  **【写入侧】四个口** —— 与上面那 7 个【取值口】一一配对（`AddFaithMana` / `AddSpiritStoneMana` /
        //  `SetSavedMana` / `RefillCustomMana`）。
        //
        //  判据（第一权威；4 份 `.c` **方法体实读**，2026-10-26）：
        //   · `decomp_full/PlayerManager__AddFaithMana.c`           → `faithMana(+0xB0) += n`（再 `ManaManager.SetFaithMana`）
        //   · `decomp_full/PlayerManager__AddSpiritStoneMana.c`     → `spiritStoneMana(+0x9C) += n`（再 `ManaManager.SetSpiritStoneMana`）
        //   · `decomp_full/PlayerManager__SetSavedMana.c`           → `savedMana(+0xD8) = n`（再 `ManaManager.SetManaSaved`，第 3 个实参只喂 HUD）
        //   · `decomp_full/PlayerManager__RefillCustomMana.c:35-41` → `unusedMana(+0x4C) += n`（**无封顶**，`totalMana` 不动；第 2 个实参只喂 HUD）
        //  **四个方法体都只是「读 ObscuredInt → 加减 → 写回」**，没有上限 / 下限 / 衰减 / 修正
        //  ⇒ 我们的等价物就是字段本身加减，一行转发即逐字等价。末尾那一跳 `ManaManager.*` 是
        //  **HUD 刷新**（`ManaManager__SetAccumulationMana` 那种只有 `GameObject.SetActive` + 重排）
        //  ⇒ 引擎侧不建模，表现层按 `EvtKind` / `ctx.Signals` 自己刷。
        //
        //  ⚠️ 与上面 7 个取值口同一条纪律：**这四个方法自己不做任何判断**
        //     （上限 / 来源 / 触发 / 广播全在调用方 `RuleCore` / `EffectResolver` 那一侧），
        //     ⛔ 别在它们里面加规则 —— 那会变成同一条判据的第二份实现。
        //  ⚠️ 本段**追加在类尾**（铁律 18·b）：上面那些行号是账里 `A` 行的落点，
        //     插在中间会让它们整体漂移。
        // ==================================================================

        /// <summary>原版 `PlayerManager.AddFaithMana(int)`（`decomp_full/PlayerManager__AddFaithMana.c`）：
        /// `faithMana(+0xB0) += newMana`，随后 `ManaManager.SetFaithMana`（HUD）。
        /// **无上限、不衰减、不是货币**（`ResetMana` 里没有 faith 参数、`ManaType` 里也没有 Faith）—— 见 <see cref="Faith"/>。</summary>
        public void AddFaithMana(int newMana)
        {
            Faith += newMana;
        }

        /// <summary>原版 `PlayerManager.AddSpiritStoneMana(int)`（`decomp_full/PlayerManager__AddSpiritStoneMana.c`）：
        /// `spiritStoneMana(+0x9C) += newMana`，随后 `ManaManager.SetSpiritStoneMana`（HUD）。
        /// **无初始值、无上限** —— 见 <see cref="SpiritStones"/>；花费方向由 `EffectResolver` 的几处花费点自己扣。</summary>
        public void AddSpiritStoneMana(int newMana)
        {
            SpiritStones += newMana;
        }

        /// <summary>原版 `PlayerManager.SetSavedMana(int value, bool useNumberChange)`（`decomp_full/PlayerManager__SetSavedMana.c`）：
        /// `savedMana(+0xD8) = value`，随后 `ManaManager.SetManaSaved(...)` —— 第二个实参（`useNumberChange`）
        /// **只控制 HUD 数字跳动**，状态侧就是一句赋值。
        /// 我们只有值这一半 —— 见 <see cref="ManaCarry"/>。写点两处（都在 `RuleCore`）：
        /// · `BattleManager__UpdateManaLeftAccumulation.c` 的 `min(manaAccumulation, GetUnusedMana)`（`EndTurnCore`）；
        /// · `_NextTurn:344-352` 兑现后的 `SetSavedMana(0)`（`BeginTurnCore`）。</summary>
        public void SetSavedMana(int value)
        {
            ManaCarry = value;
        }

        /// <summary>原版 `PlayerManager.RefillCustomMana(int manaToRefill, bool silently)`（`decomp_full/PlayerManager__RefillCustomMana.c:35-41`）：
        /// `unusedMana(+0x4C) += manaToRefill` —— **没有封顶**（`totalMana` / 最大能量**一点不动**），
        /// 第 2 个实参只控制 HUD。唯一调用点 = `RuleCore.BeginTurnCore` 兑现 `addedManaSecond`
        /// （=`_NextTurn:344-352` 的 `RefillCustomMana(saved)`；今天两个模式都是 0 ⇒ 休眠）。</summary>
        public void RefillCustomMana(int manaToRefill)
        {
            Energy += manaToRefill;
        }

        // ==================================================================
        //  🆕 2026-10-29（B94 · F4 · 账 534 / R-B1 补）：原版 `CardScript.RemoveCardFromCemetery`
        //     的「永久放逐闩」等价物 —— **追加在类尾**（铁律 18·b：上面那些行号是账里落点，插中间会漂）。
        //  判据：`decomp_full/CardScript__RemoveCardFromCemetery.c:5`（实体只一句 `+0x24b = 0`）；
        //        `CardScript__Awake.c:40`（置 1）；`BattleManager__GetCardsInCemetery.c:54`（按它过滤）。
        //  语义 → IsInCemetery 的 doc。写口调用方（把卡从墓地取走的路径）在 RuleCore/EffectResolver = F1/F2，未接。
        // ==================================================================

        /// <summary>
        /// 原版 `CardScript.RemoveCardFromCemetery()`（🥇 `decomp_full/CardScript__RemoveCardFromCemetery.c:1-8`，
        /// 全文只一句：`*(undefined1*)(card + 0x24b) = 0;`）的等价写口 —— **把这张卡永久放逐出墓地查询**。
        /// <para>判据 / 语义 → <see cref="IsInCemetery"/> 的 doc（`+0x24b` = `shouldCheckIfIsInCemetery`，
        /// 一次性置 0、全库无写回 1 ⇒ 永久闩）。⚠️ 调用方在 `RuleCore` / `EffectResolver`（F1 / F2 的文件）⇒
        /// **本代理只提供写口、不接线**（账 534 记 B）。⛔ 别在别处另建第二份闩。</para>
        /// </summary>
        public void MarkRemovedFromCemetery(CardInstance inst)
        {
            if (inst == null) return;
            _banishedFromCemetery.Add(inst.Id);
        }

        /// <summary>「永久放逐闩」的簿记（≡ 原版 `+0x24b`，见 <see cref="IsInCemetery"/>）。
        /// 记的是**实例 id**（不是模板）—— 原版那格也是**逐卡实例**的状态位。</summary>
        readonly HashSet<int> _banishedFromCemetery = new HashSet<int>();

        // ==================================================================
        //  🆕 2026-10-29（B94 · 波 2 · 执行代理 `B1` · 账 `MinionManager.GetMinionList` 的残差）：
        //  **原版 `CardScript.IsInPlay()` 那一档过滤** —— 追加在类尾（铁律 18·b：`PlayerState.cs:172/206`
        //  是账里落点，插中间会让它们整体漂移）。
        //
        //  判据（第一权威，2026-10-29 现读）：
        //   · `MinionManager__GetMinionList.c:35`（左表 `leftMinions(+0x60)`）/ `:78`（右表 `rightMinions(+0x68)`）
        //     —— **逐条** `CardScript.IsInPlay(card)` 为真才收进临时表，之后才 `OrderBy(orderInPlay)`；
        //   · `CardScript__IsInPlay.c` 全文 = `cardState(+0x228) ∈ {2 inPlay, 15 inPlayTransforming,
        //     3 inPlayAttacking, 17 inPlayAminingAttack}` ⇒ **`5 waitingToDie` 为假**
        //     （`CardStateOptions` 枚举见 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardStateOptions.cs`）。
        //  ⇒ 我们的等价物 = `!ctx.IsPendingDeath(u)`（`BattleContext.IsPendingDeath` 的 doc 就是
        //     「原版 `waitingToDie = 5`」）。**不带 `ctx` 的 `UnitsByPlayOrder()` 少了这一档**，本重载补上。
        // ==================================================================

        /// <summary>
        /// 原版 `MinionManager.GetMinionList()` 的**完整判据版**：<see cref="UnitsByPlayOrder()"/> 的
        /// 排序口径 ＋ **原版 `IsInPlay()` 那一档过滤**（`MinionManager__GetMinionList.c:35` / `:78`）。
        ///
        /// <para>🔴 **为什么必须单独一个重载**：不带 `ctx` 的那份拿不到「这一格是不是 `waitingToDie`」——
        /// 待死单位**仍然挂在 `Board` 上**（见 `BattleContext.IsPendingDeath` 的 doc），不滤就会多收进种群。
        /// 原版三个调用点里 `_ResolveForceAttackFriendly`（`BattleManager._ResolveForceAttackFriendly_d__506__MoveCount.c:39-59`）
        /// 是 **`GetRandomInt(0, count)` 随机挑一个** ⇒ **种群多几席就会改变结果**，不是「只影响顺序」。
        /// （另两个调用点 `BasicBattleEndSequence` / `DestroyUnitsAfterBattleEnd` 是收局摧场的演出顺序。）</para>
        ///
        /// <para>⚠️ **排序口径只此一处**：本方法先调 <see cref="UnitsByPlayOrder()"/> 再摘除待死项
        /// （`RemoveAll` 不改变剩余元素的相对次序）⇒ ⛔ 别在这里重写一遍排序。</para>
        ///
        /// <para>⚠️ `ctx == null` ⇒ 逐字等价于 <see cref="UnitsByPlayOrder()"/>（纯数据/老夹具场景）。
        /// ⚠️ `IsInPlay` 的等价物**只有** `!ctx.IsPendingDeath(u)` 这一个（⛔ 别在别处另写一份）。</para>
        /// </summary>
        public List<UnitState> UnitsByPlayOrder(BattleContext ctx)
        {
            // `UnitsByPlayOrder()` 每次都返回**新表** ⇒ 就地 `RemoveAll` 安全（不会动到棋盘或别的调用方的表）
            var list = UnitsByPlayOrder();
            if (ctx == null) return list;
            list.RemoveAll(u => ctx.IsPendingDeath(u));      // 原版 IsInPlay() == false
            return list;
        }
    }
}
