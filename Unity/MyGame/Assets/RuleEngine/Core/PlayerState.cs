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
        /// ⚠️ 语义按「**一次性结转**」实现（下回合最大能量 +它，之后清零）—— 规则书只写了
        /// 「保存 1 点 → 下回合最大 +1」，**是临时还是永久没写死**，我们按更保守的那种做，注释标着。</summary>
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

        /// <summary>可部署的空格数</summary>
        public int FreeSlots()
        {
            int n = 0;
            for (int i = 0; i < Board.Length; i++)
                if (BoardSpec.IsDeployable(i) && Board[i] == null) n++;
            return n;
        }

        /// <summary>有空的部署格吗</summary>
        public bool HasFreeSlot()
        {
            for (int i = 0; i < Board.Length; i++)
                if (BoardSpec.IsDeployable(i) && Board[i] == null) return true;
            return false;
        }

        public override string ToString()
        {
            return $"{Name} 能量 {Energy}/{MaxEnergy} 手牌 {Hand.Count} 牌库 {Deck.Count} 场 {Board.Length - FreeSlots() - 1}";
        }
    }
}
