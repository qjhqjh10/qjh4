# 卡牌效果里的「选择 / 随机 / 条件换数值」—— 全池审计与裁决（2026-09-14 起，2026-09-15 按用户指正重写）

> **用户 2026-09-14 定的口径**：卡面**明确写了让玩家选**才是玩家选，否则随机（§一）。
> **用户 2026-09-15 追加的方法（并定为长期规矩）：判据以中文为准。**
> 🔴 **中文一直都有，而且卡面印的就是它**（实测 2026-09-15）：
>   · `cards_engine.json` **`nameZh` 1130/1130**、**`descZh` 1124/1124**（`desc` 非空的那些全有），
>     而且**没有一条是「与英文一模一样」**（不是占位）—— 源头是 `数据/卡牌翻译/zh_cards.json`（1196 条，
>     `n`=中文名 / `d`=中文效果），由 `工具/gen_cards_engine.py` 读进来。
>   · **游戏里卡面走的就是它**：`BattleDriver:2480` ——
>     `string body = string.IsNullOrEmpty(c.DescZh) ? c.Desc : c.DescZh;`（卡名同理走 `CardText.Name`）。
>   ⇒ **看中文 = 看玩家实际看到的东西**，不是「翻译参考」。
> 本文档里每一条**都并排给 英文原文 / 中文 / 我们怎么读**。
>
> ⚠️ 我在这件事上**读错过两次**，都在 §六 留了更正痕迹（铁律 5：保留痕迹，下个会话才知道核过）。

---

## 一、口径（两条，互相独立）

**① 谁挑（用户 2026-09-14）**：

> 「二选一、三选一或者更多类似的说法，以及从卡组、墓地或者其他地方布置部队、加入手卡等各种情况，
> **除非它的效果明确说明是让玩家选择或者有类似让玩家选择的语义**，这个时候才是让玩家自行选择，
> **否则就是随机**。」

正面出处：规则书英文版 `:475` / 中文版 `:233` ——
「**Whenever a card uses the word "choose"**, randomly select **3** cards from the set of
possibilities and the player chooses which one」⇒ 没有那个词就没有「玩家选」。

| 卡面 | 谁挑 |
|---|---|
| 写了 `choose`（`choose one` / `choose a …` / `choose an effect`），或**直接给出具体的几张卡/几个效果** | **玩家**（候选多时**先随机抽 3 张**，见 §五） |
| **没写**（`A or B` / `become A or B` / `Deploy a …` 之类） | **随机** |

**② 条件换数值（用户 2026-09-15 指正）**：
`…, or <另一个数> if <条件>` **不是二选一** —— 卡面说的是「**若…则把那一个数换掉**」。
四条实例见 §三。**判断这一族只能看中文**，英文那个 `or` 与 §一的 `A or B` 长得一模一样。

---

## 二、全池实测：`desc` 含 ` or ` 的 37 张

| 堆 | 张数 | 例 | 走哪条 |
|---|---|---|---|
| 含 `choose` | 6 | `Craftworld Convergence`（择一）· `Carnifex`（选择并获得一项增益） | §一① ⇒ 玩家选 |
| 比较式（`5 or less` / `6 or more`） | 9 | `Rok Invasion` | 不是选择，别切 |
| **真 `A or B`** | **22** | 见下表 | 逐张判是「二选一」还是「条件换数值」 |

### 22 张逐条（**中文是判据**）

| 卡 | 中文（`descZh`，判据） | 归类 |
|---|---|---|
| `Deadly Ambush` | 给予 1 个友方部队侧翼，**或**眩晕 1 个敌方部队 | 二选一·随机 |
| `Primaris Psyker` | 对一个敌人造成 2-3 点伤害，**或**给予一个友方单位护盾 | 二选一·随机 |
| `Protection Protocol` | 给予一个友方部队 +3 生命和先锋，**或**复生一个友方残骸 | 二选一·随机 |
| `Dead Choppy` | 给予一个友方载具嗜血，**或**给予另一个友方部队 +2[攻击] | 二选一·随机 |
| `Anvil of Endurance` | 给予 1 个友方部队护甲 1，**或**为你的督军治疗 4 点 | 二选一·随机 |
| `Hero of the Empire` | 给予 1 个敌方部队标记光 1，**或**给予 1 个友方单位本回合 +1 远程攻击 | 二选一·随机 |
| `Spirit Leech` | 对 1 个敌人造成 1 点伤害，**或**给予 1 个友方单位 +1 生命 | 二选一·随机 |
| `Ancient Reliquary` | 给予一个我方部队 +3 攻击、+3 护甲**或** +3 生命 | 二选一·随机（载荷三选一） |
| `Catechism of Death` | 给予 1 个友方部队 +2，**或本回合给予你的督军 +2** | 二选一·随机（目标二选一） |
| `Hrolf the Ironhowl` | 你手牌中的战术**变为猎狼或芬里斯狼** | 二选一·随机 |
| `Drone Companion` | 创建 1 个枪无人机、守护无人机**或**标记无人机 | 名字列表·随机 |
| `Zodgrod Wortsnagga` | **随机**部署一个地精**或**蛇咬地精 | 名字列表·随机（卡面直接写「随机」） |
| `Special Dose` | 给予一个友方步兵**或**野兽 +3[攻击] 和 +1 生命值 | 目标类别枚举（**两个都算**） |
| `Deploy Anchors` | 给予 1 个友方战斗服**或**载具 +5 远程攻击 | 目标类别枚举 |
| **`Vindicator`** | 集结：对 1 个敌人造成 3 点伤害；**若其带猎杀标记，则造成 6 点** | **条件换数值** |
| **`Wulfen Pack Leader`** | 对 1 个随机敌方部队造成 3 点伤害；**若你未控制其他部队，则造成 5 点** | **条件换数值** |
| **`Monster Hunters`** | 部署一个猎兽小子；**若对手控制一个 5 生命以上的部队，则改为部署 3 个** | **条件换数值** |
| **`Disruption Blades`** | 本回合给你的单位 +1 攻击；**若其为毁灭者，则给 +3 攻击** | **条件换数值** |
| `Craftworld Convergence` / `Hymn of Battle` / `The Fang` / `Inscrutable Cunning` / `Duelist's Hubris` / `Carnifex` | 择一 / 选择一项 / 选择其一 / 选择并获得一项增益 | 已有 `chooseone` ⇒ 玩家选（`Carnifex` 走 `ReChooseBonus` 归一） |

---

## 三、🔴 「条件换数值」—— 四条，**2026-09-15 已实现**

### 3.1 卡面对照

| 卡 | 英文 | 中文（判据） | 条件 |
|---|---|---|---|
| `Vindicator` | `Rally: Deal 3 damage to an enemy, or 6 if it has Hunt Mark` | 集结：对 1 个敌人造成 3 点；**若其带猎杀标记，则造成 6 点** | 目标带猎杀标记 |
| `Wulfen Pack Leader` | `Deal 3 damage to a random enemy troop, or 5 if you control no other troops` | 对 1 个随机敌方部队造成 3 点；**若你未控制其他部队，则造成 5 点** | 自己场上没有**其他**部队 |
| `Monster Hunters` | `Deploy a Beast Snagga Boy, or 3 if your opponent controls a troop with 5 or more Health` | 部署一个猎兽小子；**若对手控制一个 5 生命以上的部队，则改为部署 3 个** | 对手有 ≥5 生命的部队 |
| `Disruption Blades` | `Give +1 [Attack] to your units this turn, or +3 [Attack] if they are Destroyer.` | 本回合给你的单位 +1 攻击；**若其为毁灭者，则给 +3 攻击** | 被加的那批单位是毁灭者 |

**共同形状**：一个数值在条件成立时被换掉 —— 换的可能是
**伤害值**（`Amount`）· **部署数量**（`Amount`）· **载荷里的数**（`payload` 的 `+1`）。

### 3.2 落点

| 环节 | 落点 |
|---|---|
| 解析 | `EffectText.TryOrAltIf` —— 两种形态：①基数里已有数字（`ReOrAltNum`）②基数里没有数字 ⇒ 那个数是**数量**（`ReOrAltCount`） |
| | 做法：**基数句与备选句都真 `Dispatch` 一遍**，要求两句话**动词相同**，再把「替换值 + 条件」挂到**第一条 op** 上（`EffectOp.AltAmount` / `AltPayload` / `AltCondition` / `AltConditionKind`） |
| 条件归一 | `EffectCondition.Normalize` 新增两条：`ownnoothertroops` · `enemyhightoughness`；`targethaskw` 改用 `ClauseKeyword`（**两词关键词** `Hunt Mark` 原来被截成 `hunt`、认不出） |
| 结算 | `EffectResolver.AltHolds`（**判不了 → 用基数 + 如实打日志**）+ 三个消费点：`DoDeal`（逐目标换伤害）· `DoDeployOnce`（换数量）· `DoGive`（逐目标换载荷） |
| 断言 | `RuleEngineTest.TestOrAltIf` —— **四条各跑正反两遍**，只看血量 / 场上数 / 攻击值 |

### 3.3 ⚠️ 为什么**不走**已有的 `EffectOp.Instead`（那套更省事）

`Instead` 那套在**结算前**就把条件判完、决定跳哪一条。而这一族的条件说的是
**「这一下要打的那个目标」**（`it has Hunt Mark` / `they are Destroyer`）—— 那个目标**结算前还不存在**。
用那套会走到「条件判不了 ⇒ 两条都跑」，**伤害叠加**（3+6=9），比原来更糟。
⇒ 改在**数值被消费的地方**逐目标判。见 `AltHolds` 的注释。

### 3.4 ✅ 「5 生命以上」= **当前生命**（用户 2026-09-15 亲口裁决）

`Monster Hunters` 的「若对手控制一个**生命值 5 或以上**的部队」——
**判的是当前生命，不是生命上限**（用户 2026-09-15 明确：「是当前生命」）。
我们的实现正好就是这样（`enemyhightoughness` 读 `u.Health`），
和本工程已有的镜像写法 `If your Warlord has 10 or less Health`（`At All Costs`，读的也是当前值）同口径。
**这条已经定了，不用再核。**

---

## 四、已修的其他 13 张（2026-09-14）

判据落点：`EffectText.TryEitherOr`（新）· `EffectOp.RandomPick`（新）· `EffectResolver.DoChooseOne`。
**做法**：产出形状与 `chooseone` **完全一致**的 op，只多一个 `RandomPick = true` ——
结算流程一个字不改，**只把「谁来挑」从面板换成 `ctx.Rng`**；`PlayerChooseOps` 见到它**跳过**（不开面板）。

**它们原来在静默错**：`or` 的后半句被吞进**目标文本或载荷**里，而探针整句报「**干净**」
（不认识 0 · 半懂 0）⇒ **卡面不打 `*`**、覆盖率算它完全解析。

| 卡 | 原来 | 现在 |
|---|---|---|
| `Deadly Ambush` | 只给 Flank，`Stun` 那半丢 | 二选一·随机 |
| `Primaris Psyker` · `Spirit Leech` | `deal` 的目标被判成 `own/unit` ⇒ **打自己人** | 二选一·随机 |
| `Anvil of Endurance` | 给**督军**加护甲 | 二选一·随机 |
| `Protection Protocol` · `Dead Choppy` · `Hero of the Empire` | 后半句丢 / 目标 Side 判反 | 二选一·随机 |
| `Ancient Reliquary` | 三个选项黏成一段载荷 | 载荷三选一·随机 |
| `Catechism of Death` | 只给督军 | 目标二选一·随机 |
| `Special Dose` · `Deploy Anchors` | 只筛到第一个兵种，另一个**静默漏掉** | 兵种筛 `A\|B`（两个都算） |
| `Zodgrod Wortsnagga` | 载荷带 `at random` ⇒ 两个名字**都查不到** | `CleanListName` 剥「句尾 at random + 冠词」 |
| `Hrolf the Ironhowl` | 🔴 上一轮被我改成「问玩家」（**错的**） | **随机**（撤掉面板分支与 `BecomeTitle`） |

---

## 五、「先随机抽 3 张再让玩家挑」—— 已落地

**规则书 `:475`**：`choose` 的候选要**先随机抽 3 张**。用户 2026-09-14 第 4 条点名的就是它。

| 落点 | 做什么 |
|---|---|
| `BattleDriver.ChooseShowMax = 3` + `DrawForDisplay` | 面板**只摆 3 张**（从候选里不放回地随机抽） |
| `BattleContext.ChooseCardIds` | 面板把**选中那张的 `CardDef.Id`**报回来 —— 下标对不上（候选表长短不一） |
| `EffectResolver.TakePickCard` | 有 `Id` 按 `Id` 找，没有才退回下标/`ctx.Rng` |
| `BattleContext.ShowRng` | **只给面板抽「给你看哪几张」用**，与 `Rng` 分开 —— 否则「有没有面板」会改变引擎的随机数序列 |

⚠️ **哪些来源抽 3 张是我们挑的**：`pool`（全卡池）与 `deck`（牌库）**抽**；
`hand` / `enemyhand` / `dead` **不抽**（池子小；自己手牌看得见）。规则书没分来源。
⚠️ **引擎侧不真的抽 3 张**：「从 N 张里随机抽 3 再挑 1」与「等概率挑 1」**同一个分布**。

---

## 六、更正记录（我读错过的两处 —— 留痕迹，别删）

> 🔴 **2026-09-15 更正 ①：`Monster Hunters` 我说「载荷糊了 ⇒ 查不到卡、这条不生效」—— 错了一半。**
> · **「载荷糊了」是对的**（探针实测：载荷是 `beast snagga boy, or 3 if your opponent controls
>   a troop with 5 or more health` 整句）。
> · **「查不到卡」是错的**：`CreatePool.Resolve` 见到 ` or ` 会走**名字列表**那一支，
>   第一段 `beast snagga boy` **查得到**（`Orks/3部队/Warpforge_04_Beast-Snagga-Boy.png`，
>   权威表第 568 行，卡池里也有）⇒ 实际是「**部署 1 个、条件那半被丢掉**」，不是「什么都没做」。
> · **错因**：我只看探针的载荷**长什么样**就推结论，**没去看解析层下游怎么用它**，
>   也没查权威表里有没有这张卡。**这正是「拿不准就该去查」的那一步没做。**
>
> 🔴 **2026-09-15 更正 ②：`Vindicator` / `Wulfen Pack Leader` / `Disruption Blades` 被我归成
> 「待用户裁、先挂起」—— 该做的。** 我当时在英文里把它们读成「二选一」，于是去纠结「谁挑」；
> **中文一看就清楚是「条件换数值」**，与 §一① 的口径根本无关。四条**本次全部实现**（§三）。

---

## 七、补做的两条 + 还没做的

**✅ 2026-09-15 当天补做（用户点名，两句我都是「懂但没做」，报成了「理解不了」—— 见 §六）**：

| 事项 | 中文（判据） | 落点 |
|---|---|---|
| `Maulerfiend` 的 `Ecstasy 5: Double this troop's [Melee] and [Ranged]` | 狂喜 5：**使本部队的近战和远程翻倍**（卡面两个图标就是**拳**和**枪**，**没有生命图标**） | `ReDouble` 改成**两种写法都认**、属性组合由 `Payload` 承载（`melee,health` / `melee,ranged`）；`DoDouble` 照载荷翻。`TestDoubleAndCreateCost` ①：近战 3→6、远程 2→4、**生命 3/4 不变** |
| `Runtherd` 的 `Create a random Ork Beast in your hand that costs 5 or less` | 群体：在手牌中生成一个**费用 5 或以下**的随机兽人野兽（**0~5**） | `TryCreate` 抽出 `CostMin/CostMax`（与 `deploy` 共用 `ReCostLimit`）+ `DoCreate` 传给 `CreatePool.Resolve`。`TestDoubleAndCreateCost` ②：载荷不再夹 cost 子句、候选里**一张超 5 费的都没有** |

**还没做的**：

**没有别的未决项了** —— §3.4 那条（「5 生命」= 当前生命）用户 2026-09-15 已裁决，实现本来就是对的。

---

## 八、出处

- **规则书**：`资料/规则书/Warpforge_Offline_Rulebook_1_5-3_英文原版.md` `:471-477`（`"Choose" Effects`）·
  `:463-464` · `:915-960`（附录 B/D，**附录 D 的 `Original` 栏就是卡面原文**）· 中文版 `:233`
- **中文判据**：`cards_engine.json` 的 **`descZh` 字段**（本文档 §二的整张表都是它）
- **卡图核对（铁律 7）**：`d:/2/Warpforge部队卡片/Orks/4计策/Warpforge_31_Monster-Hunters.png`
  （4 费，正文无图标、无 `choose`）· `Orks/3部队/Warpforge_04_Beast-Snagga-Boy.png` ·
  `d:/2/Warpforge部队卡片/卡牌信息权威表_0824.md:568,688`
- **探针**：`_tmp_view/probe_in.txt` → `probe_out.txt`（`Editor/EffectParseProbe.cs`，**已加印 `Alt*` 四个字段**）
- **代码**：`EffectText.TryOrAltIf` / `EmitOrAlt` / `TryEitherOr` · `EffectOp.AltAmount` / `RandomPick` ·
  `EffectResolver.AltHolds` / `DoDeal` / `DoGive` / `DoDeployOnce` / `TakePickCard` · `EffectCondition.ClauseKeyword` ·
  `BattleDriver.DrawForDisplay` · `BattleContext.ChooseCardIds` / `ShowRng`
- **断言**：`RuleEngineTest.TestOrAltIf`（8 条，正反各四）· `TestChoosePick` · `BattleScene` 21-d
