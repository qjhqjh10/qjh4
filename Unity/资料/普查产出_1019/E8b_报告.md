# E8b · 第五十五会话 · 引擎线（批次 3）—— `A2009` 三支真打 · 三条对齐规则 · `A2001` 覆盖

> 状态：**已交**（2026-10-19）· 写手 `E8b` · 白名单 = `工具/ruleprobe/**` ＋ `工具/_a1759_*.py` ＋ `资料/普查产出_1019/**`
> 未跑 Unity / 未跑任何 `*.Run` / 未起 player / 未动 git（铁律 12 / 13）。
> ⛔ **基线/指纹一字节未动**：`工具/ruleprobe/基线/out_baseline.txt`（md5 `9e98a496…`）与
> `out_play_baseline.txt` **都不在 `git status` 里**；`PinnedParseMd5` / `PinnedPlayMd5` / `PinnedPoolMd5` 未改。

---

## §一 结论

| 交办项 | 状态 | 一句话 |
|---|---|---|
| **`A2009` 剩余 3 支** | **✅ 3/3 真打通过** | `deploy`（`SOR42`）· `return`（`UM61`）· `shuffle`（`ASH82`）全部有「选了哪张 → 去哪」的引擎日志 + 变化逐条 |
| ├ 夹具预置局面 | **✅ 已落** | `play <id> preset dead[=N][:Subtype] / enemyhand=N / chipenemy=1`（`PlayProbe.PresetSpec` + `ApplyPreset`）；⛔ 不传 = 夹具一字不变 |
| └ `shuffle` 的「hero 档前提」 | **❌ 前提不存在（E9 误判）** | `ASH82` 在池与 SO 真值里都是 **unit**（`cardType=0`）；`2 [Spirit Stone]: …` 是**部署时**结算的灵魂石能力 ⇒ `play ASH82` 本来就能跑到 `shuffle` |
| **三条对齐规则** | **✅ 已落** | 红 **141 → 18**（−123）；110 条逐条改判 ⚠️（78 张卡）＋ 13 条「我们多」红行因对照 op 被认领而消失 |
| ├ 反例（规则没吞真缺口） | **✅ 逐条留红** | β 7 张（含 5 张 `260`、`SAU11` `320`、`SW38` 判据冲突）· γ `GOF12` 3 条 · `GOF38` 2 条（`give/gain` 的**值核对**拒绝）· `SAU44` 1 条（词典缺口） |
| **`A2001` 覆盖** | **✅ 能补的都补了** | `UM2` 用 `preset chipenemy=1` 覆盖（Rally 找到 1 个受伤敌人）· `BL32` 由事件电池覆盖（净差 **+4**，`BL51` 造牌）· **`SW22` 仍是盲区**（电池缺「非致命攻击」那一发），逐条理由见 §四 |
| **复跑读数** | **✅ 已跑** | `check`：1121 张 · 解析差异 **17** 行 · 改名 63 · 哨兵失败 1（输出指纹，既定）· `play all`：rc=0、抛异常 0、电池自检全过 · `reconcile`：rc=0（见 §五） |

---

## §二 `A2009` —— 剩余 3 支逐支真打

> 做法：`bash d:/4/Unity/工具/ruleprobe.sh play <id> preset <spec>`；看**引擎自己的日志**与**变化逐条**。
> ⚠️ 夹具预置是**量具干预**（`FIX_DEAD*` / `FIX_EH*` **不在卡池里**）：它只让「候选非空之后选牌→动作这一跳」照得到，
> ⛔ 别把读数读成「原版开局墓地/手牌长这样」。

### 2·1 `deploy`（`SOR42 Rite of Restoration`）—— ✅ 通过

```
bash 工具/ruleprobe.sh play SOR42 preset dead=1
```
- 打出去之前：`P0 弃=[FIX_DEAD0]`。
- 日志：`免费部署 FIX_DEAD0（2/3）到槽 2`；`从自你上个回合起阵亡的部队选了「FIX_DEAD0」（friendly troop）→ 部署到槽 2`。
- 变化：`✚ 落场/生成 P0[2] . → FIX_DEAD0 2/0/0/3` ＋ `🗑 弃牌 P0 -「FIX_DEAD0」×1` ⇒ **同一个实例从墓地搬上棋盘**。
- 实得变化 2 条 · rc=0。

### 2·2 `return`（`UM61 The Chapter's Due`）—— ✅ 通过

```
bash 工具/ruleprobe.sh play UM61 preset dead=1:Infantry
```
- 日志：`把「FIX_DEAD0」洗回自己牌库（friendly infantry）` + `「Draw a card」抽了 1 张`。
- 变化：`🂠 牌库 P0 +「FIX_DEAD0」×1` · `✋ 手牌 P0 +「filler23」×1` · `🗑 弃牌 P0 -「FIX_DEAD0」×1`。
- 说明：`dead=1:Infantry` 的 `:Infantry` 是**必要**的 —— `UM61` 的筛选词是 `friendly Infantry`，
  默认 `FIX_DEAD*` 也是 Infantry，但显式写出来才能证明**筛选条件参与了选择**。
- 实得变化 4 条 · rc=0。

### 2·3 `shuffle`（`ASH82 Farseer Skyrunner`）—— ✅ 通过（**并更正 E9 的一处误判**）

```
bash 工具/ruleprobe.sh play ASH82 preset enemyhand=1
```
- 预置前 P1 手牌 `[FIX_EH0,filler27,filler28,filler29]`；打出后 `[FIX_EH0,filler28,filler29]`、`牌库 P1 27 → 28`。
- 日志：`灵魂石能力 付了 2 点灵魂石激活「choose a card in the enemy hand and shuffle it into their deck」`；
  `…把对手手里的「filler27」洗回对手牌库（card）`。
- 🔴 **E9 的「hero 档前提」不成立**：`cards_engine.json` 里 `ASH82.type = unit`；
  当前 build 的 SO 真值 `cards_raw.json` 里 `cardType = 0`（= Minion）。
  `2 [Spirit Stone]: …` 是**部署那一刻**由 `RuleCore.PlayCard` → `EffectResolver.ResolveSpiritAbility`
  结算的灵魂石能力（`RuleProbe` 的 `MeasurePlay` 打出去就经过它）⇒ **不需要 `MeasureHero`**。
  E9 报告 §二·2 第 11 行已就地打更正痕（铁律 5）。

### 2·4 夹具能力（`工具/ruleprobe/**`）

| 写法 | 做什么 | 判据 / 形状 |
|---|---|---|
| `dead[=N][:Subtype]` | 往 **P0 墓地**放 N 张 `FIX_DEAD*` | **两笔账一起写**：`Discard` 实例 ＋ `DeadUnits{Owner,DeathTurn=ctx.Turn}` —— 正是 `BattleContext.TakeFromGraveyard` 的判据；`DeathTurn` 取当前回合 ⇒ `since_last_turn` 窗口（`DeathTurn >= LastTurnStartMark`）也满足 |
| `enemyhand=N` | 往 **P1 手牌**放 N 张 `FIX_EH*` | `ctx.Players[1].Hand.Add` ＋ `RuleCore.SetupCardInHand`（与引擎发牌同形） |
| `chipenemy=1` | P1 的**非督军**单位各扣 `FixtureChip` 血 | 走引擎 `RuleCore.ApplyDamage`（⛔ 不手改 `Health`）；用途见 §四 `UM2` |

⚠️ 预置在 `BuildFixture` **取快照之前**做完 ⇒ 预置本身**不出现在「变化逐条」里**；
⛔ `preset` **只接受单卡** `play <id>`：`play all` / `play check` 传了会**出声并停**（rc=2），
因为全池读数必须保持同一套夹具。

---

## §三 三条对齐规则（`工具/_a1759_reconcile.py`）

> **总读数**：`141 🔴 → 18 🔴`（−123）。其中 **110 条逐条改判 ⚠️**（78 张卡）+ **13 条「我们多」红行消失**
> （对照 op 被规则认领后不再重复出声）；副作用 = **1 条 ✅ → ⚠️「拆条差异」**（`SW56 a0.1`，见 §3·4）+ 2 条旧的
> `⚠️承载不同`（`DA42` 的 `choosecard` / `BL41` 的 `double`）被认领后并入改判行。
> **A/B 证据**：`A1759_NO_E8B=1`（规则整段关闭）= `✅1161 / 🔴141 / ⚠️504 / ❓1` —— **与加规则前逐数相同**；
> 打开 = `✅1160 / 🔴18 / ⚠️614 / ❓1`。⇒ **规则只在红行的兜底段生效，没动任何既有 ✅**。
>
> 三条规则的实现位置：`_a1759_reconcile.py` 的 `e8b_alignment()`（规则①/②/③）+ `_e8b_value_rules()`（③的取值两支）。
> 每条都在**兜底段**（`best is None`）或**已配到 op 但 `same=False`** 那一格才跑。

### 3·1 ① choose 分支逐支配 —— **22 条 / 12 张卡**

- **判据**（结构事实）：我们的 `EffectText` 把 N 个分支解析成**一条** op —— `chooseone` 的
  `EffectOp.Payload` 用 `|` 分；`choosecard` 的动作在 `ChooseAct`（`EffectResolver.DoChooseCard` 的 `switch`）。
  原版把同一件事写成**同一条 ability 的 N 条 `abilityLogic`** ⇒ 按「分支 × step」逐条比（族 + 值）。
- **值核对**：`_e8b_branch_value_ok()` —— 伤害/治疗比数值（含 `{AM:}` 上界），属性 buff 比 `nums` 与 trait 名
  （trait 用**词边界**匹配，⛔ 不是裸 `in`：`vulnerable` 不会命中 `invulnerable`）。
- **改判**：`ASH39`(2) · `GSC67` · `9f75…`(2) · `97e959…`(2) · `GSC29` · `6ed51…` · `AM22`(2) · `GOF72`(2) ·
  `SAU40`(3) · `SW43`(2) · `760bd…`(2) · `GOF12`(2) —— 22 条 / 12 张。
- **反例**：**`GOF12 Ork Spanner`** —— 我们只有 3 个分支（+2 melee / +2 ranged / +1 armour），原版 **5 条**；
  `+2 health`、`+2 melee+1 health`、`+2 ranged+1 health` 三条**仍然红**（值核对找不到对应分支）。
  **`TL49` / `TL50`** 虽然有 `chooseeffect`，但载荷 `give` / `hand` **不表达** `triggerEffectInCard` 的语义 ⇒ 仍红。

### 3·2 ② `give`/`gain` ↔ `buffSelf`/`buffOthers` —— **16 条 / 9 张卡**

- **判据**：原版把「自己/全体获得」编成 `buffSelf(40)` / `buffOthers(30)`，我们解析成 `give`（buff_other）或
  `gain`（buff_self）。`eq_set()` 原来只放行 `buff_other→buff_self`（且只对 `targetsAffected ∈ {20,120}`）；
  本规则补**另一个方向**（`give` ↔ `buffSelf`）与 `gain` 的**全体档**：
  `gain` 的 `{T:…}` 里 `Subjectless=1`（自指）**一律不放行** —— 否则「给一个友方单位」的 `buffOthers` 会被蒙混。
- **值核对**：`compare()` ＋ 本规则新加的一句 —— `bt=3 addTrait` 那条短路**只看 trait 名**，
  原版同一块里的 `nums`（例 `melee+2 + Blood Thirst`）会漏 ⇒ 原版带 `nums` 时我们的载荷必须**逐项含有**它们。
- **改判**：`SW57`(4) · `SOR41` · `SW56` · `GSC24`（`buff 族对齐` 4 条）＋ 4 张 Dark Pact 附魔卡的 `拆条差异` 12 条
  （`Mark of Slaanesh/Nurgle/Tzeentch/Khorne`，单条 `give` 合并承载了原版拆开的多步）—— 共 16 条 / 9 张。
- **反例**：**`GOF38 Proper Killy`** —— 原版 `melee+2 + addTrait bloodThirst`，我们载荷只有 `blood thirst`
  （卡面也只印了 Blood Thirst）⇒ 值核对不通过，**仍留红 2 条**。这就是「规则不吞真缺口」的那一半。

### 3·3 ③ 别层承载 / 编码不同 —— **72 条 / 57 张卡**（17 个子规则）

> 判据总纲：`EffectText.WillRunOps`（`EffectText.cs:2018`）把七个来源（`WhenTriggers`/`TriggerTexts`/
> `SpiritOps`/`OathOps`/`FaithOps`/`AttackedOps`/`AuraSpecs`）汇成**执行层**，而 `scan` 只看主解析器
> ⇒ 「效果在另一层」这件事可以被机器判（族 + 值 + 关键词/层标记），不必靠猜。

| 子规则 | 行数 | 卡 | 判据要旨 |
|---|---|---|---|
| 关键词（Unstable / Pray） | **21** | `GOF10 GOF63 GOF69 GOF25 GOF70 GOF61 GOF17 GOF58 GOF92 GOF26 GOF66`（Unstable 11）· `SOR3 SOR30 SOR35 SOR29 SOR69 SOR2 SOR13 SOR17 SOR20 SOR6`（Pray 10） | Unstable = `doDamage + addTrait fast + valueOptions`（`RuleCore.UnstableBlast` `:8936`）；Pray = 原版 `singleUseEnchantment` / `triggerPray(462)`，我们走关键词触发表 |
| 事件层 | **5** | `BL69 BL3`（MarkOfChaos）· `TL9`（OtherUnitDeath）· `TL19`（TriggeredSynapse）· `GOF68`（OtherCardMob） | 我方 `WhenTriggers` 非空（`ch.when>0`）且 `willrun` 里有同族 op |
| 附魔载体 | **5** | `AM51 DA42 GOF91 EC51 SW46` | 原版 `buffType ∈ {12,15,16}` = 载体；我们是 `persist`/`whenclause` 信封、光环或选区入口 |
| 静态规则 | **5** | `SAU27 SAU33 GSC37 UM106 UM93` | 逐卡表 → `CardDef.MatchStaticBattleRule`（`CardDef.cs:1421-1462`；表外一张不放行） |
| 包装码（260/320） | **7** | `GOF31`(2) · `UM82` · `ASH43` · `SAU52` · `SW56` · `DA39` | **白名单 = E9 逐条裁定 α 的那批**；β 的 5 张 + `SAU11` 不在表里 |
| draw+autoPlay | **10** | `SW57`(5) · `BL55`(4) | 原版 `drawCard` 那一步带 `summonCriteria` ⇒ 抽到即打出（`A1571` 的 `EffectOp.AutoPlay`） |
| 编码等价（270/420/90/360） | **6** | `DA72` · `SW18` · `GSC55` · `UM117` · `UM115` · `UM118` | `createCardInDeck(270)` ↔ `return` · `setCardDeckToTop(420)` ↔ `create … top of deck` · `v90` ↔ `become` · `playChosenCardAbility(360)` = Oath 包装码 |
| 资源（mana） | **4** | `DA18`(2) · `SOR31`(2) | 原版 `buffMana`；我们 `gain` 的载荷同时写了 `vanguard / 2 quest points` / `1 energy` |
| 副本修饰 | **2** | `TL69`(2) | 原版 `bt=25 ephemeralCopy` / `bt=95 setCost` 是「造副本」的修饰块；我们 `whenclause/create` 承载 |
| ferocitystay | **2** | `SW40 SW36` | 原版 `addTrait dontReturnFerocity`；我们 `ferocitystay` / `whenclause "it stays in play"`（`EffectResolver.cs:8226`） |
| double | **1** | `BL41` | 原版属性 buff ↔ 我们 `double` 动词（`EffectResolver.cs:874`） |
| 纯表现 v480 | **1** | `SW34` | `highlightBloodThirst(480)` 是表现（特效线）；棋局那半边已由同族 op 承载 |
| 取值编码 | **4** | `SW4 SW33`（卡面为准）· `TAU55`（拆分求和 2+2=4） | ①我们的取值与 `{SRC:}` **卡面原文**一致且 ≠ SO ⇒ 以卡面为准（铁律 2/7）；②多条同族 op 合计 = 原版绝对值 |

**反例集（本条规则不吞的真缺口，全部仍留红）**：
- **β**：`GOF56` / `TL49` / `TL50` / `ASH46` / `GOF41`（`triggerEffectInCard(260)`）· `SAU11`（`followAfterResolution(320)`）·
  `SW38`（判据冲突）—— 它们**不在 `E8B_WRAPPER_ALPHA` 白名单里**，这就是「白名单不是按族一刀切」的证据。
- **γ**：`GOF12` 的 3 条 health 组合（值核对拒绝）；`9dcbe3d0… Mekaniak`（选区动作不符，桥判过 `False`）。
- **其它**：`GOF38`×2（规则②的值核对拒绝）· `SAU44`（见 §五·3）· `36892cd Bitter Blows`（E9 判 α 但只到「**疑似**模板残留」⇒ 不替它翻绿）。

### 3·4 被改判的行数与卡名单（全文）

> 与 `_a1759_reconcile.py` 文件尾打印的 `E8b` 段**同一份**（⛔ 不另抄一份口径）。

```
改判合计 110 条 · 涉及 78 张卡 · 按子规则：
· 取值以卡面为准            2 条 · SW4、SW33
· 别层承载·附魔载体          5 条 · AM51、DA42、GOF91、EC51、SW46
· 别层承载·关键词          21 条 · SOR3、SOR30、GOF10、GOF63、GOF69、GOF25、GOF70、SOR35、GOF61、SOR29、
                                     SOR69、GOF17、SOR2、SOR13、GOF58、GOF92、SOR17、GOF26、SOR20、SOR6、GOF66
· 编码不同·draw+autoPlay  10 条 · SW57、BL55
· buff 族对齐           4 条 · SW57、SOR41、SW56、GSC24
· 拆条差异              12 条 · SW57、b8e81b2f…(Slaanesh)、1dfc9bc7…(Nurgle)、4a48457c…(Tzeentch)、9a2cadc6…(Khorne)
· 选牌分支对齐            22 条 · ASH39、GSC67、9f75a6fe…、97e95944…、GSC29、6ed51d59…、AM22、GOF72、
                                     SAU40、SW43、760bd3af…、GOF12
· 别层承载·包装码           7 条 · GOF31(2)、UM82、SW56、ASH43、DA39、SAU52
· 编码不同·资源            4 条 · DA18、SOR31
· 编码不同               6 条 · DA72、SW18、UM117、UM115、GSC55、UM118
· 别层承载·静态规则          5 条 · SAU27、UM106、SAU33、UM93、GSC37
· 编码不同·ferocitystay   2 条 · SW40、SW36
· 编码不同·副本修饰          2 条 · TL69
· 别层承载·事件层           5 条 · BL69、TL9、TL19、GOF68、BL3
· 编码不同·拆分求和          1 条 · TAU55
· 编码不同·double        1 条 · BL41
· 别层·纯表现             1 条 · SW34
```

**另有 13 条红行「消失」**（=「我们多」那一格，对照 op 被规则认领后不再重复出声）：
`DA42` · `SW57` · `ASH52` · `SOR41` · `UM82` · `UM117` · `SW56` · `ASH43` · `GSC24` · `SAU52`×2 · `GSC55` · `SOR31`。

**副作用（如实记）**：`SW56 a0.1`（`followAfterResolution +1 melee`）从 **✅ 掉成 ⚠️「拆条差异」** ——
原版把 `+1 melee` 写成两条（`buffOthers` + `followAfterResolution`），我们只有一条 `gain`；
第一条被规则②认领后，第二条走既有的「原版拆条」那一支。**语义没变**，只是表示从「精确一致」变成「拆条同义」。

---

## §四 `A2001` —— 夹具能覆盖的档 / 覆盖不到的盲区

| 卡 | E9 的结论 | 本轮 | 说明 |
|---|---|---|---|
| `GOF18` / `DA5` | 效果响了、`实得变化 0` 因目标满血 | **已覆盖（读数口径）** | 日志里治疗条数在；⛔ `raw==ctrl` 不是缺陷判据 |
| `TAU67` | 免费部署 Kroot Hound | **已覆盖** | 日志 + 变化逐条都有 |
| `EC17` | 夹具阵营 = `Fixture` ⇒ 造牌筛空 | **夹具盲区（不改）** | 真实督军是真阵营；`Fixture` 是**故意**的（`BuildFixture` 的 `HeroOf`）；它另一半（手牌扫描）与 `SOR70` 同源，属**真偏离**，改引擎要动 `RuleEngineTest`（红线） |
| `SOR70` / `TAU10` | `ResolveAtTurn` 从**手牌**扫未出场单位 | **真偏离已照到** | 引擎侧问题，不是量具；修法见 E9 §三·8（被自检钉住） |
| `BL32` | **未查清**（没看到 Artifice 段） | **✅ 已覆盖（电池）** | `play BL32` 事件电池 **净差 +4**：`✋ 手牌 P0 +「BL51」×1` + 三条手牌总价 ⇒ Artifice 真响了。E9 只看了被截断的 14 条日志。⚠️ `steps` 里 `abil=14` 只是 `UseAbility`；**`UseAlternative` 的返回码没打出来**（`RunEventPass` 第 ④ 步）——这是**显示缺口**，建议下一轮补进 `steps` |
| `UM2` | 夹具里没有**受伤的**敌人 | **✅ 已覆盖** | `preset chipenemy=1`：日志 `RALLY：「Give Blind to a damaged enemy」给了 1 个目标`、实得变化 1 条 |
| `GSC35` | 真缺陷（已修） | **已覆盖** | 修后 Rally = 3 点 |
| `SW22` | 量具覆盖不到 | **仍是盲区（原因已钉死）** | `Strike: … if the target **survives** …`。事件电池的两发攻击排在 `chip2`（把两个敌人削到 1 血）**之后**，按设计都是**致命一击** ⇒ 「survives」**结构上不可能为真**（`play SW22` 电池净差 0）。要覆盖得给电池加**一发非致命攻击**（新步骤/变体）—— 那会改全池电池读数 ⇒ 属收口统一重钉那一批，本轮未动 |

**⇒ 覆盖结论**：`A2001` 10 张里 **8 张已被某种量具覆盖**（主档/电池/preset），
**1 张夹具盲区**（`EC17` 的阵营，故意不改）、**1 张电池逻辑盲区**（`SW22`，缺非致命攻击那一发，已写清为什么）。

---

## §五 改动清单 · 复跑读数 · 没查清 / 接口需求

### 5·1 改动清单（**只在探针与仪器**；⛔ 未动 `Assets/**`）

| 文件 | 改了什么 |
|---|---|
| `工具/ruleprobe/PlayProbe.cs` | 新增 `PresetSpec` / `ApplyPreset`（`dead / enemyhand / chipenemy`）；`Run` / `Measure` / `MeasurePlay` / `MeasureHero` / `HeroPass` / `BuildFixture` 各加一个**可选** `preset` 形参（默认 `null` ⇒ 夹具一字不变）；`PresetSpec.Parse` 认不出的 token **抛错**（⛔ 不静默忽略） |
| `工具/ruleprobe/Program.cs` | `play <id> preset <spec>` 选项；缺值/`all|check` 传 preset ⇒ **出声 + rc=2** |
| `工具/ruleprobe/README.md` | `play` 段补 `preset` 用法与三条语义（含「只用于单卡」的红线） |
| `工具/_a1759_reconcile.py` | 三条对齐规则：`e8b_alignment()` + `_e8b_value_rules()` + 四个小 helper；`emit` 段与 `best is None` / `not same` 两处接线；`orig_value` 增 `meta['dmg_opt']`（只增可判据、不改渲染）；`CARRY_VERB` 加 `spendspirit`（受 `E8B_ON` 控制）；`A1759_NO_E8B=1` A/B 开关；文件尾「按子规则」改判汇总 |
| `资料/普查产出_1019/E9_报告.md` | 就地更正 3 处（铁律 5）：`ASH82` **不是 hero**（unit + 灵魂石能力）· `BL32` **已由电池覆盖**（净差 +4）· `SW22` 盲区原因 + `UM2` 已覆盖 |
| `资料/普查产出_1018/W5_对账_1019.tsv` | **仪器自己的输出**（跑 `_a1759_reconcile.py` 时按默认 `OUT` 重写；⛔ 不是基线/指纹） |

⚠️ 行尾：三个源文件均为 **LF**（HEAD 也是 LF，改后仍 LF，`CRLF=0`）；
`git diff --numstat`：`PlayProbe.cs 648/36`（含 E8 之前未提交的改动）· `Program.cs 79/11` ·
`README.md 19/0` · `_a1759_reconcile.py 472/1`。

### 5·2 复跑读数（原样摘要）

```text
$ bash d:/4/Unity/工具/ruleprobe.sh check
=== 全池 1121 张 · 解析差异 17 行 · 改名 63 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
=== 哨兵合计：失败 1 条 ===           ← 输出指纹（既定），与加规则前相同
=== 07:43:02 结束（rc=1）

$ bash d:/4/Unity/工具/ruleprobe.sh play all
=== play all · 逐卡真打（不跑 Unity）· 池 1127 张 ===
    引擎抛异常              0 张
  判据：**抛异常 = 0 张** ⇒ rc=0
    ⇒ 自检 **全过 ✅**（红的每一条 = 那个事件电池没抬起来）
=== 结束（rc=0）

$ bash d:/4/Unity/工具/ruleprobe.sh play check
  基线 out_play_baseline.txt（主档 1127 行 ＋ 事件电池 586 行）· 当次现算 主档 1127 ＋ 电池 586
  结算指纹：当次现算 = **3eb44428889659d9edd77683a8f949a5** · 基线文件 = 07aba22133b70af4fe609d37b00a88bf（**不同**）
  --- ①-a 逐卡差异（主档 · 打出去那一拍）---
     🔴 **13 张卡变了** · 新增 0 · 消失 0
       BL38 BL55 GOF77 GOF87 GSC35 GSC59 SOR14 SW1 SW38 SW57 TL37 TL49 TL8
=== 结束（rc=1）

$ python -I -X utf8 d:/4/Unity/工具/_a1759_reconcile.py
stat = {'✅一致': 1160, '⚠️表示不同·语义同': 614, '🔴真差异': 18, '❓查不清': 1}
   改判合计 110 条 · 涉及 78 张卡 · 按子规则：…
（rc=0）
```

- `play check` 那 **13 张**是**本轮之前**就存在的漂移（E8/E9 的引擎改动 + 三条 `Talent:` 数据补丁 +
  `A1571` autoPlay），与 `preset` 无关 —— **`preset` 默认 `null` 时 `Sweep`/`Check` 一个字节都不经过它**。
  按既定口径，**基线重生成 + 重钉留到引擎线收口统一做**（本轮⛔ 未重生成、⛔ 未重钉）。
- **基线/指纹未动**：`out_baseline.txt` md5 = `9e98a4962b7cf3282e8070595f7ae912`（与开工时相同）；
  `out_play_baseline.txt` 不在 `git status`；`PinnedParseMd5 = 2519b591…` / `PinnedPlayMd5 = 07aba221…` 未改。

### 5·3 没查清 / 接口需求（⛔ 不猜）

1. **`SAU44 Disruption Blades` 仍红 1 条**：原版两条 `buffOthers`，第二条带 `traitsFilter=680`；
   `DefinedTrait` 里 **680 = `frenzied`**，而卡面写的是 **“… if they are Destroyer”** ⇒
   规则③·t（条件变体）要求「筛选 trait 名出现在卡面/op 原文里」，**这里对不上**。
   缺的是 **`frenzied ↔ Destroyer` 的印刷名映射**（数据侧/词典）。⛔ 本轮没有硬编码这条别名（会变成假绿）。
2. **`36892cd Bitter Blows` 仍红 1 条**：E9 判 α，但判据只到「**疑似**模板残留」（`chosenAbility=110 destroy`
   同一 logic 里带 `damage/heal/buff` 块）。机器判不了「`destroy` 这一支读不读那两个块」⇒ 留红。
3. **`SW22` 事件电池缺「非致命攻击」**：加一发不致命的攻击（或一个变体）才能验 `Strike … survives`。
   这会改全池电池读数 ⇒ 必须与基线重生成/重钉同一批做（收口）。
4. **`UseAlternative` 的返回码没进 `steps`**：`BL32` 的 Artifice 实际上响了（净差 +4），但 `steps` 只打 `abil=`（`UseAbility`）。
   ⇒ 建议 `RunEventPass` 第 ④ 步把替代动作的 rc 也写进 `steps`（纯显示，不改结算）。
5. **`EC17` 的阵营盲区**：默认夹具督军阵营 = `Fixture`（`HeroOf(..., "Fixture")`）。若将来要验「按阵营造牌」那一族，
   需要一个 `preset warlordfaction=<阵营>`（本轮**没做** —— 它会改 `BuildFixture` 的核心形状，超出本笔范围）。
