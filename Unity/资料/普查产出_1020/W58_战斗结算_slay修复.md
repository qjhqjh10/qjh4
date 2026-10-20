# W58 战斗结算 · Slay 修复（W58-SLAYFIX）

> 执行代理：`W58-SLAYFIX`（引擎线）。按只读核查报告 `W58_战斗结算_slay触发核查.md` 修 D1/D2/D3/D4/D5/D7，D6 只交「待接点」。
> **未跑 Unity**（⛔ 无 `*.Run` / 无截图）· **未动 git** · **未重钉任何基线/指纹**。
> 验证用了：纯 dotnet `工具/ruleprobe`（`play` / `play check`）+ 一个临时 scratch 探针（在系统临时目录里编译 Core 的同一份源码，复刻 D7 夹具）+ 秒级 typecheck。

---

## 〇、结论

| 项 | 状态 |
|---|---|
| **D1** `Blast 2 and Slay: …` 丢前缀 | ✅ **已修**。三张受影响卡（Helspear / Ferocious Rage / 'Uge Choppa）两段都挂上了 |
| **D2** Blast 溅射死者不记 Slay | ✅ **已修**（每个溅射死者各记一次） |
| **D3** Stomp 溅射死者不记 Slay | ✅ **已修** |
| **D4** 多死者顺序 / 次数 | ✅ **已改**：`FlushDeaths` 不再按 (方,格号) 排序，改按**死亡入队顺序**；同一结算死 N 个 ⇒ Slay **N 次** |
| **D5** 溅射击杀不记猎杀标记 | ✅ **已修**：`Hurt(..., killer: p)` 传下去了（实测回血/对面督军掉血都对了） |
| **D7** 断言 | ✅ **已写**（`TestW58BlastStompSlay`，9 组夹具；只写不跑 Unity） |
| **D6** 表现（状态动画） | ⏭ **未改**（`BattleDriver.cs` 有别的写手）；待接点见 §四 |

**关键实测（ruleprobe `play`，改前 → 改后）**

| 卡 | id | 改前 | 改后 |
|---|---|---|---|
| Helspear Assault | `61046837c330b48978b4af5201230b6e` | 战将 `{slay1}` | 战将 **`{blast2, slay1}`** |
| Ferocious Rage | `84d4d192e0f57a34a90ac0f6172c3358` | `{slay1}` + 攻击 2 | `{slay1}` + **攻击 3**（`+1 fist` 回来了） |
| ’Uge Choppa | `6899b199f5e36144ebbdd7ed9d9e7afe` | `{slay1}` + 攻击 2 | `{slay1}` + **攻击 4**（`+2 attack` 回来了） |

---

## 一、D1 解析修法 + 受影响卡清单

### 1·1 修法（`Core/GivePayload.cs`）

原逻辑（`ParseIntoCore` 的「嵌入效果」分支）：`:` 前面的头 `head` 从**后往前**逐词试，哪一段能被
`KeywordTable.Normalize` **整段**吃掉就当成嵌入关键词；命中后 **直接 `ops.Add(Embedded); return true`**
⇒ `hitHead` **之前**那截（`Blast 2 and` / `+1 fist and ' skull` / `+2 attack and slay`）静默消失。

现在：命中后，若 `hitAt > 0`，把 `words[0 .. hitAt-1]` 拼回一串，交给
`TrimPrefixTail(...)` 做**机械收尾**，再**递归 `ParseInto`** 解释成一或多个普通 op，最后才加嵌入 op。

`TrimPrefixTail` 只做三件「机械」事（⛔ 不另写语义）：

1. 去掉尾部连接词 `and` / `or` / `,`；
2. 去掉 `IconNames` 里的图标名残留（`skull` / `wings` / …）与首尾引号；
3. 去掉**与嵌入关键词重复的尾词**（`[Slay] Slay` 上游脱方括号后成了 `slay slay`，
   命中的是最后一个 `slay`，前一个只是图标残留）。⚠️ 必须去：`AddKeyword` 是**累加**的，
   多算一层会把 `slay 1` 抬成 `slay 2`（静默错）。

前缀若**解释不出来** ⇒ 加一条 `PayloadOp { Unresolved = true }`（与多段拆分那条分支同一口径，
⛔ 不许静默丢半句）。实测这 3 张的前缀都解得开，不产生 Unresolved。

**没有用「按 `and` 字符串 split 一刀切」**：`and` / `:` 会出现在**效果正文**里
（`Slay: Heal 2 and gain a Dark Pact of Excess`）—— 这里只切 `head`（第一个 `:` 之前）且只取
`hitHead` 之前那截，正文一个字都不动。

### 1·2 哪些卡面写法会被这条分支命中（从 `cards_engine.json` / 解析产物扫）

扫描口径：`ruleprobe layers` dump 的 `willrun` op 载荷（1127 张卡，1336 条 op）+ 光环载荷；
再按「第一个 `:` 之前的头能被 `KeywordTable` 整段吃掉」复算。**全池命中 = 3 张**：

| 卡 | 载荷原文 | 前缀段（原来丢的） | 嵌入段 |
|---|---|---|---|
| `Helspear Assault`（`61046837…`） | `blast 2 and slay: deploy a black legionary` | `Blast 2` | `slay: …` |
| `Ferocious Rage`（`84d4d192…`） | `+1 fist and ' skull slay: draw a beast'` | `+1 fist`（含 `' skull` 图标残渣） | `slay: …` |
| `'Uge Choppa`（`6899b199…`） | `+2 attack and slay slay: heals 3` | `+2 attack`（含 `[Slay]` 图标重复词） | `slay: …` |

**不属于这一支、本来就没事的同族写法**（它们因为头里带引号，后缀匹配失败 ⇒ 走「按 `and` 拆分」
那条分支，前缀段早就在）—— 记下来免得下一个人误改：
`vulnerable 2 and "backlash: …"`（`Litany of Despair`）· `vulnerable 4 and "backlash: …"`（`Hunters of Heretics`）·
`sniper and "slay: …"`（`Master Outrider`）· `flank and 'strike: …'`（`Enhanced Aggression`）·
`+2 health and "strike: …"`（`Oath of Moment`）。
纯嵌入头（`"slay: …"` / `"💀 backlash: …"` / `"mob: …"` / `"penitence: …"`）不受影响。
光环载荷里含 `:` 的只有 `GOF91 Beastboss on Squigosaur`（纯嵌入头），不受影响。

---

## 二、D2–D5 改动（`Core/RuleCore.cs`）

### D2/D3：溅射死者逐个记账

`DeclareAttack` 里新增局部 `List<UnitState> splashKills`（**按身份**、只记 `!IsAlive`、去重）：

- Blast 循环：`Hurt(ctx, au, blast, …, p)` 之后 `if (bd > 0 && !au.IsAlive) splashKills.Add(au)`；
- Stomp 循环：同上（`sd`）。

批末（`using (SimultaneousDamage)` 之后、`CheckWinner` 之前）**按入队顺序**逐个走唯一判定口：

```csharp
TryCreditKill(ctx, attacker, tgtP, target);          // 主目标（原有）
foreach (var v in splashKills) TryCreditKill(ctx, attacker, tgtP, v);
```

- **为什么不在循环里当场记账**：原版死亡广播发生在伤害段全部算完之后；`Slay` 会部署/伤害/改盘面，
  放进循环会插进「同时伤害」中间，还可能改掉后面 Blast/Stomp 要用的目标表。
- **为什么不另写判据**：督军/残骸/已复活/凶手离场都由 `TryCreditKill` 自己挡（同一判定口）。
- 顺序 = **主伤害 → 爆裂（相邻表顺序，左→右）→ 践踏**，与 `CheckIfDead` 的死亡入队顺序一致。
- 阵营用 `tgtP`（溅射目标必然属于被攻击方）——不需要在 `Hurt` 之前现取 `OwnerOf`
  （死亡延后到批末才离场，且本次全部来自同一侧）。

### D4：`FlushDeaths` 改按死亡入队顺序

删掉 `pending.Sort((a,b) => (Player, SlotAtDeath))`，直接按 `TakePendingDeaths()` 的
`List` 顺序处理。判据写进方法 doc：
`CardScript__TriggerUnitBacklashActions.c:181`（每个死者判死那一刻各入队一条 action，
`AddAutoActionToQueue`）+ FIFO ⇒ 入队顺序 = 伤害顺序（主伤害 `:999` → 爆裂 `:1030` → 践踏 `:1089` → 反击 `:1142`）。
入队顺序同样确定（随机只走 `ctx.Rng`），不破坏可复现性。

### D5：猎杀标记

Blast / Stomp 的 `Hurt` 补第 5 个实参 `killer: p`（攻击方玩家号）。
实测（scratch）：Blast 溅死 `Hunt Mark 2` 的单位 ⇒ 我方督军 `20→22`、对方督军 `30→28`；
Stomp 溅死 `Hunt Mark 1` ⇒ `20→21` / `30→29`。

---

## 三、测试清单（D7，写进 `RuleEngine/Editor/RuleEngineTest.cs`）

新增 `TestW58BlastStompSlay`，注册在 `TestStrikeAndSlay` 之后（`Step(...)`）。9 组：

| # | 夹具 | 断言 |
|---|---|---|
| ① | D1 解析：三条真载荷 | `blast=2 + EMB slay`；`attack=1 + EMB slay`；`attack=2 + EMB slay` 且 **slay 关键词 = 0**（图标残留不得变成第二层） |
| ①b | 真卡 `Helspear Assault`（**直接塞手牌 + 给能量**，原因是它带 `Ephemeral`、回合 0 末会被扫走） | 打出后战将 `KwValue("blast")==2 && KwValue("slay")==1` |
| ② | Blast 2、主目标 9 血、相邻 2 血 | 相邻死、主目标活；督军 **-2**；Slay 事件 **1** 条 |
| ②′ | 同上，两侧相邻各 2 血 | 两个都死；督军 **-4**；Slay **2** 条 |
| ②″ | 主目标 1 血 + 两侧各 2 血（同一结算死 3 个） | 三个都死；督军 **-6**；Slay **3** 条 |
| ③ | Blast 2；相邻 A=3 血 + `Vulnerable 1`（死）、B=3 血无易伤（剩 1） | A 死、B 活；督军 **-2**；Slay **1** 条（挡住「Blast 一开就记」） |
| ④ | Stomp、5 攻打 2 血主目标（溢出 3）、相邻 3 血 | 两者都死；督军 **-4**；Slay **2** 条 |
| ⑤ | 1 攻 2 血攻击者 vs 5 攻 9 血目标（**反击打死攻击者**） | 攻击者死、目标活；督军 **不变**；Slay **0** 条 |
| ⑥ | 战术卡 `Deal 3 damage to an enemy` + 场上有 `Slay: Draw a card` 的旁观者 | 目标死；Slay **0** 条；手牌数不变 |
| ⑦ | 直接攻击 1 血敌方督军（带 `Blast 1` + `Slay: Draw a card`） | 督军倒下；Slay **0** 条；手牌数不变 |
| ⑧ | Blast 2 溅死一具**残骸** | 残骸进弃牌堆；Slay **0** 条；手牌数不变 |
| ⑨ | Blast 溅死 `Hunt Mark 2` 的单位 | 我方督军 +2、对方督军 -2（D5） |

**灭自证（双向）**：

- ② 正例与 ⑤ 反例走**同一发 `DeclareAttack`、同一张 `Slay` 卡**，只差「死的是谁」：
  「谁死都记账」⇒ ⑤ 红；「Blast 不记账」⇒ ② 红。
- ③ 的「无易伤、只剩 1 血、活着」那一半挡住「Blast 一开就记账」（否则督军会 -4）。
- ⑥⑦⑧ 各钉原版一道闸（`tactic(30)` / `cardType=10` / `isRemnant`）。
- ⑨ 只要把 `Hurt` 的第 5 个实参删掉就红。

> ⚠️ 所有「谁死了」的断言一律用**捕获的 `UnitState` 引用**（`!u.IsAlive` / `u.Health`），
> **不按格号断** —— 连续棋盘在死亡后会补位（实测：主目标会从 2 号格挪到 3 号格）。

---

## 四、D6 待接点（表现，本轮未改 `BattleDriver.cs`）

判据：`CardScript__AddEffect.c:889-891` —— Slay 附魔（`trigger == 0x78 = 120`）加入时，
先移除宿主身上其它 Slay 附魔，然后 `BattleCardUI.DisplayStatusAnim(0x2ee)` + `CardScript.UpdateTraitIcons()`。

我们这边：

1. **引擎侧不需要新事件**：`GrantEmbeddedCore` 已经 `u.AddKeyword("slay", 1)`；
   表现层的 `TraitFrames.SyncKeywords` 就是按**关键词集合对差**找「新挂上的 trait」的
   （`CardPresentation/Core/TraitFrames.cs:170-189`，调用点 `BattleDriver.cs:17442`
   `tf.SyncKeywords(u.Keywords.Keys)`）⇒ 授予后下一次刷新就能看见。
2. **缺的是绑定**：`TraitFrames.PlayFrames`（`TraitFrames.cs:55-74`）**没有 `slay` 条目** ⇒
   `PrefabFor("slay", false)` 返回 null、`DisplayStatus` 静默返回 false（`slay` 也不在
   `MissingTraits` 里，所以连告警都没有）。**要接的一处就是往这张表加一行**：
   `{ KeywordTable.Slay, "<框 prefab 名>" }`。
3. **还差「用哪件 prefab」的判据**：反编译里传的是 trait id `0x2ee = 750`，
   而本 build 的 `DefinedTrait.quest = 750`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:74`），
   与「Slay 状态框」字面对不上 ⇒ **先别直接把 quest 框绑上去**。
   要补的输入 = 解 `CardTraitCollection.GetTrait(750)` 那条 `CardTrait` 的 `animClips` / prefab 名字
   （`DisplayStatusAnim.c` 读的是 `CardTrait +0x38` 的 clip 列表），
   或直接看原版实况里 Helspear 附魔加身时卡上闪的是哪件。
4. 徽标那一半（`UpdateTraitIcons`）大概率已经自动跟上：`BattleDriver.ToCardData` 每次都按
   **当前关键词表**重建 `Badges.For(...)`（`BattleDriver.cs:18003-18012`）——需要真机确认一次。

---

## 五、ruleprobe 实测（改前 → 改后）+ 漂移归属

### 5·1 `play`（单卡真打）

```
改前 Helspear  → P0[4] WARLORD 2/0/0/28 {slay1}
改后 Helspear  → P0[4] WARLORD 2/0/0/28 {blast2, slay1}
改前 Ferocious → WARLORD 2/0/0/28 {slay1}
改后 Ferocious → WARLORD 3/0/0/28 {slay1}
改前 'Uge Choppa → WARLORD 2/0/0/28 {slay1}
改后 'Uge Choppa → WARLORD 4/0/0/28 {slay1}
```

### 5·2 `play check`（全池结算对拍）

```
当次现算指纹 = 0aa5c8442664796fd63814cec3388511
基线文件指纹 = 08d51f6d0b749e511f53088ef7fdd80f   （不同 —— 按指示**没有重钉**）
①-a 主档：3 张卡变了（= 上面三张）· 新增 0 · 消失 0
①-b 事件电池：3 张卡变了（DA19 / SAU32 / TAU70）· 新增 0 · 消失 0
```

**逐条归属**：

- ①-a 的 3 张 = **D1**（唯一可能来源；它们的差异恰好就是「前缀段回来」：`blast2` / 攻击 2→3 / 2→4）。
- ①-b 的 3 张 = **D4**（死亡处理顺序）。**做了 A/B 实证**：临时把旧排序（方,格号）加回去、重跑
  `play check` ⇒ **①-b 变成「没有一张卡与基线不同（586 张逐字节一致）」**，主档仍是那 3 张。
  ⇒ 电池漂移 100% 来自 D4，与 D1/D2/D3/D5 无关。
  - `DA19 Interrogator Chaplain`（`When an enemy dies, gain 1 Quest Point`）：`net 0 → 1`（任务点 +1）。
  - `TAU70 Flesh Shaper`（`When an enemy dies, give +1 attack to your units`）：`net 0 → 7`。
  - `SAU32 Skorpekh Lord`（`When an enemy dies, gain +1 Melee/+1 Ranged`）：`net 9 → 6`（终局数值 1/1→0/0）。
  - 成因：这三张都是「**同批里自己也快死**的死亡监听者」。旧排序把归属方（P0）的死亡排在前面 ⇒
    监听者先被移场、收不到后面敌方那次死亡广播；改成死亡入队顺序后，**先入队的敌方死亡先广播**，
    监听者此刻仍在场上 ⇒ 触发。判据（原版）：`TriggerUnitBacklashActions.c:181` 入队 + FIFO，
    每个死者的 `ResolveDeadCard` 在 `_ResolveBacklash…:88` / `BroadcastDeadUnit` `:144` 里跑。

### 5·3 scratch 探针（复刻 D7 夹具、直接调 Core）

```
D2   adjDead=True mainAlive=True                 lord 20→18  slay=1
D2'  lDead=True rDead=True mainAlive=True        lord 20→16  slay=2
D4   allDead=True                                lord 20→14  slay=3
VULN vDead=True pAlive=True pHp=1                lord 20→18  slay=1
STOMP mDead=True aDead=True                      lord 20→16  slay=2
COUNTER atkrDead=True bruteAlive=True            lord 20→20  slay=0
TACTIC preyDead=True                             slay=0  hand 2→1（打出战术卡本身）
WARLORD lordAlive=False over=True                slay=0  hand 0→0
REMNANT remInDiscard=True remAlive=False         slay=0  hand 0→0
HUNT  Blast 溅死 Hunt Mark 2 → 20→22 / 30→28；Stomp 溅死 Hunt Mark 1 → 20→21 / 30→29
live Helspear（手牌注入 + 能量）→ blast=2 slay=1
```

> scratch 探针放在系统临时目录（`C:/Users/qjh36/AppData/Local/Temp/w58probe/`），**没有写进仓库**。

---

## 六、没查清的部分

1. **D4 电池漂移的「原版到底该不该触发」还差一格实况判据。**
   我们能钉死的是「漂移由 D4 引起」（A/B 实证），也能钉死「死亡 action 入队是 FIFO」；
   但 `BroadcastDeadUnit` 遍历的 `bm + 0x470` 里**是否仍含 `cardState=5(waitingToDie)` 的监听者**
   这一点，和 `资料/普查产出_…` 里 `A962③` 记的「反噬结算 vs BroadcastDeadUnit 先后读不到」是同一格。
   ⇒ 这三张的终局差异**先记着**；要收口需要真机探针（在 `BroadcastDeadUnit` 与 death action 上各加一个）。
2. **跨死者的「反噬 vs Slay」交错顺序**仍不完全等同原版：
   原版逐个死者 `TriggerUnitDeathActions(:94) → BroadcastDeadUnit(:144)`（= 死者1反噬 → 死者1 Slay →
   死者2反噬 → 死者2 Slay）；我们是「全部死者的反噬跑完 → 再按入队顺序逐个 Slay」。
   **每个死者的 Slay 次数与先后已对**，只差这层交错。要完全对齐需给 `PendingDeath` 补「凶手牌」
   （`actingCard`）字段并在 flush 内逐个记账 —— `Core/BattleContext.cs` **不在本笔白名单**，故未动。
   顺带：`PendingDeath.SlotAtDeath` 现在**不再用于排序**，`BattleContext` 里「格号只在排序时用」那句注释已陈旧
   （同一原因没改）。
3. **D6 的 prefab 判据**：`0x2ee = 750` 在本 build 是 `DefinedTrait.quest`，不是 `slay(260)` ——
   没解 `CardTraitCollection` 里 id 750 的 `animClips`，所以「该绑哪件框」还没定（见 §四·3）。
4. **哨戒那一下的 `actingCard`**：沿用核查报告 §六·1 的未决项，本笔没有接哨戒路（按原口径「不记」）。
5. **未跑 Unity 断言**（按铁律 12 / 本笔指示）：`TestW58BlastStompSlay` 只做了 typecheck + scratch 等价验证。

---

## 七、改动清单

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/GivePayload.cs` | 嵌入分支保留前缀段（+ 新 `TrimPrefixTail` 辅助，66 增 / 1 删） |
| `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` | Blast/Stomp `Hurt` 传 killer + `splashKills` 批末记账；`FlushDeaths` 改入队顺序（48 增 / 10 删） |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | 新增 `CountSlaySignals` + `TestW58BlastStompSlay`（9 组）+ `Step(...)`（308 增） |
| （未改）`EffectResolver.cs` | D5 只需在 `Hurt` 调用点传 killer，不需要动 EffectResolver |

**typecheck**：`TMPDIR=/tmp/wf_slayfix bash d:/4/Unity/工具/typecheck.sh`
→ 运行时程序集 **0 错误** · 编辑器程序集 **0 错误**。
`git diff --numstat`：`GivePayload.cs 66/1` · `RuleCore.cs 48/10` · `RuleEngineTest.cs 308/0`（无整篇行尾翻转）。
