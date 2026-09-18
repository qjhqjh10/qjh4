# 11⑤ `ResolveTargets` 那条「固定个数（非随机）」支服务哪些卡 —— 普查 2026-09-18

> 日期 **2026-09-18** · 新增一个脚本：`Unity/工具/scan_fixedcount_targets.py`（**没碰任何 `.cs`、没跑 Unity**）。
> 脚本 + 全量清单：`_tmp_view/fixedcount_targets_0918.md`（按目标短语归并的逐张清单）
> · `_tmp_view/fixedcount_targets_0918.tsv`（全量 333 行，带出处列）

---

## 一、结论（一句话 + 数字）

**那条支服务 333 处目标 / 361 个卡名**：`give` 147 · `deal` 102 · `gain` 30 · `heal` 25 · `destroy` 8 ·
`stun` 6 · `return` 5 · `lose` 4 · `forceattack` 2 · `triggerability` 2 · `double` 1 · `takecontrol` 1。

**全部是 `×1`** —— 所以这条支在今天的池子里实际做的事只有一件：**取目标池里槽号最小的那一个**
（池序 = `AddSide` 的棋盘顺序，`EffectResolver.cs:1118-1128` / 调用点 `:635-636`）。

⚠️ 顺带量出**一个真差异候选**（不是「卡不能用」，是「打得跟卡面/原版不一样」）：
**45 处**带 `自动`（`Auto`）的 `deal` —— 它们的 `Raw` 写的是「**按原版规则自动选敌方最弱单位**」，
但引擎**从来没实现过「最弱」**，走的正是这条支 ⇒ 实际打的是**槽号最小的那个敌人**。
证据链见 §五，**本次没改代码**，留给你裁定。

---

## 二、那条支是什么（先看代码）

`Core/EffectResolver.cs:704-749`：

```csharp
if (spec.Count == 0)                              list.AddRange(pool);      // 704 `all` / 复数
else if (!string.IsNullOrEmpty(spec.PickMost))    … 挑最高/最低，不掷骰        // 708
else if (spec.Random && spec.Count < pool.Count)  … 用 ctx.Rng 抽 Count 个    // 730
else {                                                                        // 741 ★ 退化支
    int n = 0;
    foreach (var u in pool) { list.Add(u); if (++n >= spec.Count) break; }     // 池序取前 Count 个
}
```

⇒ **`!Random && Count > 0` ⇒ 取池序前 `Count` 个**。池序由 `AddSide`（`:1118`）决定 ——
它（`:1118`）按 `for (int s = 0; s < BoardSpec.Size; s++)` 扫 `PlayerState.Board`，
**槽号小的在前**（`AddSide` 同时做两道筛：`troopOnly` 排除督军、敌方的 Stealth/Camouflage 跳过）。

⚠️ 这条支的**结果什么时候真的算数**：`chosen`（表现层已选定的目标）会在 `:752` **覆盖**它 ——
但那个覆盖有两个条件：`chosen != null` **且** `spec.Count == 1`。
`chosen` 只有**战术卡那条路**才传（`ResolveOps(ctx, owner, source, ops, chosen, out …)` 的签名注释
明说「**需要选目标的战术卡才有**」，`:29`）；单位能力 / 事件层 / 死亡转走的那条 6 参重载（`:3137`）
**没有 `chosen` 这个口子** ⇒ 那些路上这条支挑出来的就是最终结果。

---

## 三、判据（怎么从卡表里认出来）

**不去猜英文句子**（那是「认得出 ≠ 判得了」），而是读**解析器自己的输出**：
逐句探针把每一条目标打成 `目标[<方>/<类> ×<Count> <标记> 「<原文>」]`
（`EffectParseProbe.Target`，`Assets/RuleEngine/Editor/EffectParseProbe.cs:177`）。于是：

| 条件 | 对应字段 | 说明 |
|---|---|---|
| 有 `×N`（N≥1） | `spec.Count != 0` | 探针**只在非 0 时**打 `×`（`EffectParseProbe.cs:182`） |
| 没有 `随机` | `spec.Random == false` | |
| 没有 `挑=` | `spec.PickMost` 为空 | |

三条同时成立 ⇒ **正好落进 `:741`**。

### 两道闸（少了就会多算，**都量过**）

1. **动词闸**：只有这些动词的 handler 会调 `ResolveTargets`（出处 = `EffectDispatch` 表 `:328-380`
   + 逐个调用点）：`deal:1138` · `eachunitdeal:1519` · `forceattack:1268`（`Target2` 走 `DefenderPool:1343`）·
   `heal:1571` · `return:2926` · `destroy:1875` · `stun:1906` · `blind:1937` · `sethealth:5245` ·
   `double:5317` · `ferocitystay:5356` · `extratrigger:3462` · `takecontrol:3507` · `triggerability:5406` ·
   `give/gain/lose:4253`。
   ⚠️ **反例**：`gainquest` / `gainenergy` / `gainfaith` / `gainspirit` 是**专用 handler**
   （给**玩家**加计数，不碰场上单位），它们的 `目标[own/player ×1 自动 「(玩家任务点)」]`
   看着完全符合三条判据 —— **不看动词会多算 38 处**。
2. **提前 `return` 闸**：这些分支在退化支**之前**就返回了 ⇒ 即使带 `×N` 也不算：
   `只在手牌`(:528) · `prev`(:536) · `eventtarget`(:555) · `没写主语`(:566) · `已部署`(:592) ·
   `被本单位打过`(:612) · `相邻(`(:660，含 `AnchorInSet`)。
   ⚠️ **`自动`（`Auto`）不在这张名单里** —— `ResolveTargets` 里**一行都没读它**（见 §五）。
   ⚠️ `第二目标[…]`（`op.Target2`）与 `死后转给[…]`（`op.DeathWatchTarget`）**不在名单里**、
   也算（前者经 `DefenderPool`，后者被 `FlushDeathWatches` 当成新 op 的 `Target` 再走一遍，`:4301-4315`）
   —— **实测各 0 处**（探针里 `第二目标` 都带 `随机`；`死后转给` 在逐句探针里根本不打印）。

---

## 四、结果

- 探针 desc 块 **1095** · 会调 `ResolveTargets` 的目标段 **842**（另 **432 处**被动词闸挡掉）
- **固定个数（退化支）：333 处 / 361 个卡名**，**全部 `×1`**
- 另一条脸（`随机` 且池子不够大时漏下来的）**82 处** —— **结果等价**（两条支在那种情形下取的都是整池，
  只差顺序），见 `fixedcount_targets_0918.md` 文末附注

按目标短语（前几名，全表在 md 里）：

| 目标段 | 处数 |
|---|---|
| `own/unit ×1 「a friendly unit」` | 62 |
| `own/troop ×1 「a friendly troop」` | 55 |
| `enemy/any ×1 自动 「(未写目标：按原版规则自动选敌方最弱单位)」` | **45**（见 §五） |
| `enemy/any ×1 「an enemy」` | 41 |
| `own/warlord ×1 「your warlord」` | 38 |
| `enemy/troop ×1 「an enemy troop」` | 36 |

---

## 五、⚠️ 量出来的真差异候选：`自动` 那 45 处（**没改代码，留你裁定**）

> ✅ **2026-09-18 已拍板并落地（用户选「改成打生命最低」）** —— 本节以下的调查**保持原样**（它是裁定的依据），
> 结果与改动写在这里：
> - **原版权威证据换成正本**：原来引的是 `d:/warpforge/scripts/rule_core.gd:2692`（**我们自己的 Godot 复刻**，
>   按 `CLAUDE.md` 只能当旁证）。现在 = `dump.cs:20817 TargetsAffected.lowestHealth = 240`
>   → `AbilityLogic__GetTargets.c:837` 分派 → **`BattleManager__GetLowestHealthUnit.c:153`**：
>   逐单位比 `currentHealth` 取最小、**严格小于**（并列留列表序先者）、**空场回落到督军**。
> - **改法（1 行）**：`Core/EffectText.cs` 那个 spec 字面量补 `PickMost = "-health"`，
>   复用**已经唯一存在**的 `PickMost` 挑法（`EffectResolver` 的 `spec.PickMost` 分支 + `StatOf("health")`）。
>   平手规则**天然一致**（都是严格小于 ⇒ 槽号小者胜），不是随手定的。
>   `Auto = true` 保留 ⇒ 仍然「不问玩家」。
> - **断言**：`RuleEngineTest` 加了两条端到端 —— ① 槽号小但血多的敌人 **不挨打**（能区分「打最弱」与「打槽号最小」）；
>   ② 生命并列时取**槽号在前**的。
> - ⚠️ **没能逐卡证明**这 45 处就是 `240`（卡片 ability 数据在服务端）⇒ 定案口径是
>   「**原版确有此规则、且原版没有任何一处按槽号挑**」，**不是**「已逐卡核对」。
> - ⚠️ **不顺手一起改的**：原版 `GetLowestHealthUnit` 自己只筛 `IsInPlay/remnant/cardType`，
>   **不当场滤 Stealth/Camouflage**，而我们的池子由 `AddSide` 滤 —— 那是另一个既有口径。

### 证据链（三条独立证据）

1. **注释与 `Raw` 都写着「最弱」** —— `Core/EffectText.cs:4375-4382`：
   > `// 裸 Deal N damage（如 Fire prism）—— 原版是定死的规则：自动选敌方最弱单位`
   > `Raw = "(未写目标：按原版规则自动选敌方最弱单位)"`
   `EffectTargetSpec.Auto` 的文档（`:615-617`）同样写着「如裸 `Deal N damage` → 敌方最弱单位」。
2. **`Auto` 全项目只有 5 个读点，没有一个挑「最弱」**
   （`grep -rn "\.Auto\b" --include=*.cs Assets/`）：
   `EffectResolver.cs:4286`（在 `DoGive` 里，只是「别把多于一个目标裁掉」的护栏）·
   `EffectText.cs:847`（`PickTarget`：**表示「不用问玩家」**）·
   `EffectParseProbe.cs:198` 与 `RuleEngineTest.cs:3884/5452`（打印/断言）。
   `DoDeal`（`:1135`）**从头到尾没读 `Auto`** ⇒ 落到 `:741` 那条支 ⇒ **池序第一个**；
   而 `AddSide` 的顺序是**槽号升序**（`:1118-1128`）。
3. **原版语义在参考实现里查得到** —— `d:/warpforge/scripts/rule_core.gd:2692-2696` 调
   `_auto_fx_target(ctx, p, TACTIC_ENEMY_PICK)`，而 `:3206-3216` 那个函数**逐个比 `health`、取最小的那个**，
   连「敌方场上空 ⇒ 打督军格」都写了。

⇒ 现状 = **卡面/注释承诺「最弱」，引擎给「槽号最小」**。二者在「敌方只有 1 个单位」时一致，
多于 1 个时**打得不一样**（例如 `Fire prism` 的 `Deal 3 damage`、`Shock Trooper` 的 `Deal 1 damage`）。

> ⚠️ 这条**不是**「卡不能用」，也**不是**「认不出」—— 是**结算层少了一个挑法**。
> 按项目规矩（不许静默失败、以原版为准）本该修；**但修法有两种口径**（改 `ResolveTargets` 里加一条
> `Auto` 分支、还是在 `DoDeal` 那一层挑），**且会动到引擎**，所以本次**只记不动**，请主对话裁定。

### 这 45 处逐条（卡名（ID）· op · 出处）

| # | 卡名（ID） | op | 出处 |
|---|---|---|---|
| 1 | Empyric Ambush（ASH_Empyric_Ambush） | `deal n=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:233 |
| 2 | Deff Dread/Eldritch Storm/Smite/Toxic Vapours/Witchfire/Zoanthrope（ASH24/ASH59/GOF33/TL29/TL66/UM9） | `deal n=1-3 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:403 |
| 3 | Crimson Hunter（ASH31） | `deal n=6 设为-1费 【替换】 条件=targethaskw → 目标池 = 敌方全体` | probe_out.txt:426 |
| 4 | Fire prism（ASH36） | `deal n=3 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:443 |
| 5 | Fire prism（ASH36） | `deal n=8 设为-1费 【替换】 条件=targethasarmour → 目标池 = 敌方全体` | probe_out.txt:443 |
| 6 | Shock Trooper（AM10） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:678 |
| 7 | Born Soldier（AM44） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:751 |
| 8 | Efficiency and Excellence（AM67） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:771 |
| 9 | Baneblade Tank（AM43） | `deal n=8 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:920 |
| 10 | Leman Russ Vanquisher（AM40） | `deal n=8 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:950 |
| 11 | Leman Russ Vanquisher（AM40） | `deal n=12 设为-1费 【替换】 条件=targethasarmour → 目标池 = 敌方全体` | probe_out.txt:950 |
| 12 | Lord Solar Leontus（AM3） | `deal n=3 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:1015 |
| 13 | Rites of Possession（BL35） | `deal n=2-4 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:1313 |
| 14 | Grim Resolve（DA48） | `deal n=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:1604 |
| 15 | Nephilim Jetfighter（DA37） | `deal n=5 设为-1费 【替换】 条件=targethaskw → 目标池 = 敌方全体` | probe_out.txt:1770 |
| 16 | Ravenwing Speeder（DA18） | `deal n=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:1798 |
| 17 | Embrace the Pain（EC46） | `deal n=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:2174 |
| 18 | Euphoric Strike (Lord Exultant's Talent)（EC_Euphoric_Strike_Lord_Exultant_s_Talent） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:2188 |
| 19 | Rapid Evisceration（EC49） | `deal n=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:2250 |
| 20 | Backstab（GSC48） | `deal n=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:2503 |
| 21 | Bore Through（GSC54） | `deal n=3 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:2510 |
| 22 | Bore Through（GSC54） | `deal n=6 设为-1费 【替换】 条件=targethasarmour → 目标池 = 敌方全体` | probe_out.txt:2510 |
| 23 | Extermination Protocols/Razorshark（SAU45/TAU_Razorshark） | `deal n=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:2981 |
| 24 | Kill Rig（GOF94） | `deal n=5-7 设为-1费 【替换】 条件=targethasarmour → 目标池 = 敌方全体` | probe_out.txt:3238 |
| 25 | Da Hunt is On（GOF101） | `deal n=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:3296 |
| 26 | Sawbonez（GOF_Sawbonez） | `deal n=1-2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:3704 |
| 27 | Dominion Superior（SOR28） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:3956 |
| 28 | Dominion Superior（SOR28） | `deal n=1 付费=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:3956 |
| 29 | Dogmata（SOR32） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:4028 |
| 30 | Sister Superior（SOR25） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:4045 |
| 31 | Antrak Silk/Aquilon Servo-Sentry/Arco-Flagellant/Armoury of Excess/Attack Squig/Awakened Obelisk/Big Shoota Boy/Born Soldier/Da Bigger Dey Iz... (Mozrog's Talent)/Darkened Skies/Deathwolf Scout/Defence Batteries/Dogmata/Dominion Superior/Drag it Down/Efficiency and Excellence/Euphoric Strike (Lord Exultant's Talent)/Ferren Areios/Fury of the Righteous/Gargantuan Squiggoth/Hallowed Martyrs/Havoc/Hexmark Destroyer/Incursor/Intercessor Sergeant/Internal Rivalry/Jain Zar/Kasrkin/Kelermorph/Living Lightning/Lokhust Destroyer/Lord of the Storm/Lucius The Eternal/Master of Repentance/Master of the Fleet/Maulerfiend/Morvenn Vahl/Neophyte Initiate/Orbital Defences/Plains of Excess/Ratling/Roaming Outriders/Savage Legacy/Scout/Scout Sniper/Shock Trooper/Shock Troops/Sister Superior/Skrag Every Stash!/Spirit Leech/Spirit of the Martyr/Storm Speeder Hailstrike/Terminator Champion/Tesla Carbine Immortal/Thrill Seekers/Trial of Suffering/Tyrannofex/Waaagh! Energy (Weirdboy Talent)/War Howl/Zealot Legionary（AM10/AM14/AM44/AM49/AM64/AM67/AM68/AM9/ASH74/BL32/BL46/BL_Zealot_Legionary/DA2/EC3/EC33/EC44/EC45/EC50/EC62/EC72/EC_Euphoric_Strike_Lord_Exultant_s_Talent/GOF102/GOF27/GOF40/GOF96/GOF98/GOF_Da_Bigger_Dey_Iz_Mozrog_s_Talent/GOF_Waaagh_Energy_Weirdboy_Talent/GSC42/GSC78/GSC8/SAU12/SAU2/SAU32/SAU65/SAU70/SOR2/SOR25/SOR28/SOR32/SOR49/SOR5/SOR56/SOR57/SOR8/SW10/SW15/SW50/SW67/SW8/TL2/TL43/TL49/UM11/UM18/UM4/UM71/UM89/UM_Scout_Sniper/UM_Storm_Speeder_Hailstrike） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:4162 |
| 32 | Fury of the Righteous（SOR2） | `deal n=2 付费=4faith 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:4167 |
| 33 | Morvenn Vahl/Sister Superior（SOR25/SOR5） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:4358 |
| 34 | Savage Legacy（SW50） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:4668 |
| 35 | Pulse Onslaught（TAU51） | `deal n=3 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:5065 |
| 36 | 1st Company Terminator（UM83） | `deal n=3 付费=2oath 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:5622 |
| 37 | Ferren Areios（UM89） | `deal n=1 付费=1oath 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:5729 |
| 38 | Incursor（UM71） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:5757 |
| 39 | Scout Sniper（UM_Scout_Sniper） | `deal n=1 付费=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:5817 |
| 40 | Death from Above/Hydra/No Mercy/Paragon Warsuit/Primaris Eradicator/Tankbusta/Veteran Havoc/Wailing Doom（AM33/ASH64/BL72/GOF30/SOR40/UM99/UM_Death_from_Above/UM_Primaris_Eradicator） | `deal n=4 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:5975 |
| 41 | Death from Above（UM_Death_from_Above） | `deal n=1 设为-1费 条件=energyzero → 目标池 = 敌方全体` | probe_out.txt:5980 |
| 42 | Point-Blank Shot（UM54） | `deal n=2 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:6123 |
| 43 | Smite（UM9） | `deal n=1-3 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:6171 |
| 44 | Master of Repentance（DA2） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:6381 |
| 45 | Armoury of Excess（EC62） | `deal n=1 设为-1费 → 目标池 = 敌方全体` | probe_out.txt:6403 |

> ⚠️ **表中「卡名」一列有两处要留神**：
> · **第 31 行是子句级**的（探针块就是 `Deal 1 damage.` 一句，`probe_out.txt:4162`）⇒
>   那 60 个卡名只表示「**谁的 desc 里有这句**」，**不是 60 张卡都写成这样**；
> · 其余 44 行是**整条 desc**，`卡名(ID)` 就是真卡。
> ⇒ **45 处的「涉及卡名」并集是 96，去掉第 31 行那 60 个只剩 51**；
>   要判「哪些卡」请以 **tsv 的行** + 出处行号为准（每行一个目标段）。

---

## 六、已知限制 / 没量的

1. 🔴 **探针快照比数据旧**（同 `11③` 那份）：`probe_out.txt` 是 **09-16 19:33**，
   `cards_engine.json` 最后提交 **09-16 20:55**（commit `6439425`）⇒ 本普查**按旧快照**。
   治法只有重跑探针（`EffectParseProbe.Run`，本次**没跑**：主对话占着 Unity）。
2. ⚠️ **「361 个卡名」含子句级的膨胀**：探针输入是**子句级**的，很短的句子
   （`Deal 4 damage.` / `Ephemeral.`）会挂到几十张卡上 ⇒ **卡名列表只表示「谁的 desc 里有这句」**。
   判「哪些卡」时请以**行**为准（`fixedcount_targets_0918.tsv` 每行一个目标段 + 出处行号）。
3. ⚠️ **没有跑实局**：「池子里到底有几个单位」是运行期的事，本普查只判「**会不会**走这条支」，
   不判「那一局走了谁」。要验 §五 那条，最小的实局是：场上放两个敌人（槽号小的血更多），
   打一张裸 `Deal N damage`，看打的是哪个。
4. ⚠️ **`Count > 1` 在本池 0 处**（所以「前 N 个」的 N 分支没被量到）—— 若将来出现
   `Deal 2 damage to two enemies` 这种写法，这条支会取前两个，而 `:752` 的 `chosen` 覆盖
   **只在 `spec.Count == 1` 时生效** ⇒ 那时玩家的选择会被整个忽略。**记在这儿，等它出现再量。**

---

## 七、怎么复跑

```bash
# 全量清单（按目标短语归并 + 逐张列 id/名字/卡面那句话/出处）
PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/scan_fixedcount_targets.py
#   → _tmp_view/fixedcount_targets_0918.md / .tsv
# 汇总口径：333 处 / 361 个卡名（全部 ×1）· 自动 45 · 内层 1 · 其余 287 · 随机那一批 82
#
# 探针读法与「desc→卡」的映射**不另写一份**：转调 工具/zh_crosscheck.py 的
# parse_probe / make_cards_for（本工程规矩：同一条判据别写两份）。
```
