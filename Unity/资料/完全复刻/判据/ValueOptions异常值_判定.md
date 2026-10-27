# `*ValueOptions` 的异常值 `8` / `17` —— 判定与施工结论

> 代理：`W70VOPT`（只读为主）· 日期：2026-10-27
> 上游账：`资料/完全复刻/判据/引擎真缺口_施工判据.md` §四·K / §四 第 6 条（G26 / G27 的**前置阻塞项**）。
> 复现脚本（全部只读、不写盘）：`d:/4/_tmp_view/w70/` —— `dump_rows.py`（卡表分布）· `verify_src2.py`（**直接读 bundle typetree**）· `byte_check.py` / `byte_check2.py`（**原始字节**）· `node_order.py`（typetree 字段序）· `chosen.py` / `r17.py` / `dmgdist.py`（按 chosenAbility 归类）· `mktable.py`（§一 两张表）。

---

## 〇 · 一句话结论

**这 46 行都是【原版数据里就有的「超纲值」】，不是我们抄错、不是字段类型用错、也不是枚举没抄全。**
原版运行时对它们**一律走 `default` 支**，而且**这 46 行在可观测行为上全部是死数据（0 影响）**。

⇒ **施工结论：枚举不改、字段类型不改、数据不改。要改的只有「G26/G27 里那三条 `default` 支必须逐字照抄」**（见 §三）。
⛔ 尤其**不许**把它们「按 0 静默处理」——原版不是 0，是「取 `minValue` / 取原始 `change`」。

---

## §一 46 行逐条表

统计口径：`cards_truth.json`（🥇 每卡一份、无 `full_raw` 翻倍问题）递归扫 `*ValueOptions`，
再用 UnityPy **直接读原版 SO bundle 的 typetree** 复核（`verify_src2.py`，1351 张、**typetree 异常 0**）——
两路结果**逐值一致**（`0:3476 / 8:38 / 10:1 / 17:8 / 30:1 / 40:2 / 41:1 / 50:1 / 80:10 / 90:88 / 101:1 / 140:1 / 201:3 / 202:1 / 203:1 / 204:3 / 210:2`）。

- **`8`：38 行 / 25 张卡**（其中 `4b8214d3…` Storm of Silence **不在池内**，其余 24 张在池）。
- **`17`：8 行 / 5 张卡**（`GOF30/31/32/36/43`，**全在池**）。
- `ab/lo` = `abilities[ab]/logic[lo]`；`chosenAbility` 名取自 `AbilityEffect.cs`；`buffType` 名取自 `BuffType.cs`。

### A. `damage.valueOptions = 8`（38 行）

| # | 卡 id | 卡名 | in pool | ab/lo | chosenAbility | min/max/type/opt | 判定 |
|---:|---|---|:--:|:--:|---|---|---|
| 1 | `20d0f4451f16fc441836a02f99bf77da` | Da Red Waaagh | Y | 0/0 | 0 drawCard | 3/5/5/8 | 死数据（非伤害动作） |
| 2 | `20d0f4451f16fc441836a02f99bf77da` | Da Red Waaagh | Y | 0/1 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 3 | `26d4ded12b28a3742b5c8fc155e29803` | Righteous Fury | Y | 0/0 | **20 doHeal** | 3/5/5/8 | 死数据（真正用的是 `heal`） |
| 4 | `26d4ded12b28a3742b5c8fc155e29803` | Righteous Fury | Y | 0/1 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 5 | `3ebb159036778d14ebd1c48451aa6ca0` | Greatest Warboss | Y | 0/0 | 70 summonUnit | 3/5/5/8 | 死数据（非伤害动作） |
| 6 | `3ebb159036778d14ebd1c48451aa6ca0` | Greatest Warboss | Y | 0/1 | 320 followAfterResolution | 3/5/5/8 | 死数据（非伤害动作） |
| 7 | `3ebb159036778d14ebd1c48451aa6ca0` | Greatest Warboss | Y | 1/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 8 | `4b8214d3b5e589f42beab20e56ca21b0` | Storm of Silence | **N** | 0/0 | 30 buffOthers | 3/5/5/8 | 非池卡 · 死数据 |
| 9 | `4cdc1b5f965f99945b685bca7660e9fa` | Exemplar of Hate | Y | 1/0 | 60 buffMana | 1/2/5/8 | 死数据（非伤害动作；该行另有 `heal=(1,5,0)`） |
| 10 | `508002f5d7ee54f4686aefa2e6adce41` | Wreckin Ball | Y | 0/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 11 | `508002f5d7ee54f4686aefa2e6adce41` | Wreckin Ball | Y | 0/1 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 12 | `6899b199f5e36144ebbdd7ed9d9e7afe` | Uge Choppa | Y | 0/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 13 | `6899b199f5e36144ebbdd7ed9d9e7afe` | Uge Choppa | Y | 0/1 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 14 | `6ba4b132d24528844a86304f8ce67833` | Follower of Gork | Y | 0/0 | 30 buffOthers | 1/2/5/8 | 死数据（非伤害动作） |
| 15 | `8425b809e9e64e24f9035f709f5aaee4` | Worst Temper | Y | 0/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 16 | `8425b809e9e64e24f9035f709f5aaee4` | Worst Temper | Y | 0/1 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 17 | `8425b809e9e64e24f9035f709f5aaee4` | Worst Temper | Y | 0/2 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 18 | `851f73daf747eb642934a16429b391db` | Storm of Silence | Y | 0/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 19 | `98d3c65702746b04baf7f5ead1a9e507` | Ramshackle | Y | 0/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 20 | `9dcbe3d0c04b02f4fbb19974ae0e37b2` | Mekaniak | Y | 0/0 | 100 createCardInHand | 3/5/5/8 | 死数据（非伤害动作） |
| 21 | `9f4e1b7ea9f00da458c6a859a4e958c8` | More Dakka | Y | 0/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 22 | `9f4e1b7ea9f00da458c6a859a4e958c8` | More Dakka | Y | 0/1 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 23 | `BL45` | Spawndom | Y | 0/0 | 70 summonUnit | 3/5/5/8 | 死数据（非伤害动作） |
| 24 | `GOF39` | Tide of Muscle | Y | 0/1 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 25 | `GOF45` | Get'em ladz! | Y | 0/0 | 30 buffOthers | 2/4/5/8 | 死数据（非伤害动作） |
| 26 | `GOF46` | Stormboyz Strike | Y | 0/0 | 70 summonUnit | 3/5/5/8 | 死数据（非伤害动作） |
| 27 | `GOF46` | Stormboyz Strike | Y | 1/0 | 70 summonUnit | 3/5/5/8 | 死数据（非伤害动作） |
| 28 | `GOF48` | Rok Invasion | Y | 0/0 | 70 summonUnit | 3/5/5/8 | 死数据（非伤害动作） |
| 29 | `TL52` | Leviathan's Tendrils | Y | 0/1 | 30 buffOthers | 1/3/5/8 | 死数据（非伤害动作） |
| 30 | `TL69` | Neurotyrant | Y | 0/1 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 31 | `TL69` | Neurotyrant | Y | 0/2 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 32 | `b14d1db2b0730d649b3bec35af100363` | Master of Battle | Y | 0/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 33 | `b4d6d186eb95a3f468013c5be649ee3b` | Sawbonez | Y | 1/0 | **20 doHeal** | 1/2/5/8 | 死数据（真正用的是 `heal=(1,5,0)`） |
| 34 | `d94f91051ecfe014f97577ff573571aa` | Spear of Macragge | Y | 0/0 | 0 drawCard | 0/10/5/8 | 死数据（非伤害动作） |
| 35 | `d94f91051ecfe014f97577ff573571aa` | Spear of Macragge | Y | 0/1 | 30 buffOthers | 1/3/5/8 | 死数据（非伤害动作） |
| 36 | `e874c2bd84895e64694daa44bd5fd5c2` | Primarch of the XIII | Y | 0/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 37 | `ef596edae1828bd46bfc4348535a3acc` | Supreme Strategist | Y | 0/0 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |
| 38 | `ef596edae1828bd46bfc4348535a3acc` | Supreme Strategist | Y | 0/1 | 30 buffOthers | 3/5/5/8 | 死数据（非伤害动作） |

> `chosenAbility` 分布：`{0:2, 20:2, 30:26, 60:1, 70:5, 100:1, 320:1}` —— **一个伤害族（10/12/13/15）都没有**。
> `damageType` 38/38 全是 `5`（`DamageType.Random`）。

### B. `buff.healthValueOptions = 17`（8 行）

| # | 卡 id | 卡名 | in pool | ab/lo | chosenAbility | buffType | healthChange | armourChange | 判定 |
|---:|---|---|:--:|:--:|---|---|---:|---:|---|
| 1 | `GOF30` | Attack Squig | Y | 0/0 | 10 doDamage | 1 addStats | 3 | 1 | 死：该行是 doDamage，`buffEffect` 不应用 |
| 2 | `GOF30` | Attack Squig | Y | 0/1 | 130 stun | 3 addTrait | — | — | 死：`healthChange` 缺（0） |
| 3 | `GOF31` | Cloud of Smoke | Y | 0/0 | 30 buffOthers | 3 addTrait | — | — | 死：`healthChange` 缺（0） |
| 4 | `GOF31` | Cloud of Smoke | Y | 0/1 | 320 followAfterResolution | 3 addTrait | — | — | 死：`healthChange` 缺（0） |
| 5 | `GOF32` | Grot Bomb | Y | 0/0 | 10 doDamage | 1 addStats | 3 | 1 | 死：该行是 doDamage，`buffEffect` 不应用 |
| 6 | `GOF36` | Scrag ’Em | Y | 0/0 | 30 buffOthers | 3 addTrait | 3 | 1 | 死：`addTrait` **只加 trait，不应用属性**（`CardScript__AddEffect.c:460` `case 3`） |
| 7 | `GOF36` | Scrag ’Em | Y | 0/1 | 30 buffOthers | 3 addTrait | 3 | 1 | 死：同上 |
| 8 | `GOF43` | Gretchin Mob | Y | 0/0 | 75 summonBuffedUnit | 3 addTrait | 3 | 1 | 死：同上 |

> 8 行的卡面原文（`cards_engine.json` 的 `desc` / `descZh`）**没有一条**写「+3 生命 / +1 护甲」：
> `GOF30` "Deal 1 damage to an enemy and Stun it" · `GOF31` "Blind a random enemy…" · `GOF32` "Deal 3 damage to a random enemy" ·
> `GOF36` "Give Flank to a friendly troop…Vanguard" · `GOF43` "Deploy 3 Grot and give them Vanguard."
> ⇒ `armourChange=1 / healthChange=3 / healthValueOptions=17` 这三格是**同一坨 editor 残留**，原版根本没打算让它们生效。

---

## §二 分类与根因

### A. 数据真值：**原版 SO 里就是 8 / 17**（三层独立证据）

1. **读 bundle 的 typetree（1394 张卡的 `MonoBehaviour`，`serialized_type.node` 存在）**
   — `verify_src2.py`：`typetree 异常 0` · 全部 `*ValueOptions` 51720 格 · 分布里 **`8:38` / `17:8` 都在**。
   - `damageCriteria` 的 typetree 字段序 = `valueOptions, minDamage, maxDamage, damageType`（`node_order.py`），
     与 `DamageCriteria.cs` 的声明序**逐字一致** ⇒ 名字↔位置不会错位。
   - `buffEffect`（类型 `CardEffect`）的 31 个字段序也与 `CardEffect.cs` 声明序逐字一致，`healthValueOptions` 紧跟 `healthChange`。
2. **原始字节**（`byte_check.py` / `byte_check2.py`，直接读对象 `get_raw_data()`）：
   - `Spawndom`：出现 1 次 `08 00 00 00 | 03 00 00 00 | 05 00 00 00 | 05 00 00 00`
     = **`valueOptions=8, minDamage=3, maxDamage=5, damageType=5`**。
   - `Attack Squig`：出现 1 次 `01 00 00 00 | 00 00 00 00 | 03 00 00 00 | 11 00 00 00`
     = `armourChange=1, armourValueOptions=0, healthChange=3, healthValueOptions=0x11=17`。
3. **卡表与原始读一致**：`cards_truth.json` 与 `cards_raw.json`（同一 typetree 读）与上面两路**逐值相同** ⇒
   **不是我们抽取串位 / 抄错**。（⚠️ 已知坑：`cards_raw.json` 每卡两份同构树会翻倍；本条统计用的是 `cards_truth.json` 与**直接读**，都不受影响。）

### B. 原版代码没有这两个值 —— **三份反编译一致**

| 源 | 成员数 | 有 8？ | 有 17？ |
|---|---:|---|---|
| `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs` | 26 | 无 | 无 |
| `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/ChooseValueOptions.cs` | 26 | 无 | 无 |
| `d:/2/tools/il2cpp_out/dump.cs:20865-20894`（Ghidra，TypeDefIndex 387） | 26 | 无 | 无 |
| `d:/2/tools/assetripper_out/…/ChooseValueOptions.cs` | 26 | 无 | 无 |

`StatFilterOptions` 也只有 12 个成员（`RandomChance = 8` 是它的值，但**字段类型是 `ChooseValueOptions`**，见 `DamageCriteria.cs:8`）。

> **版本配对已核**：本地可运行原版 = **Warpforge 1.35.0 · Unity 6000.2.6f2**
> （`d:/2/unity_run_ref/Warpforge_Data/globalgamemanagers` 里现读；`GameAssembly.dll` 同目录）；
> allcards bundle `__data` 头也是 **UnityFS `6000.2.6f2`**。
> ⇒ 反编译用的 DLL 与产出这批 SO 的 build **同代**，「8/17 是旧版枚举成员」这个假设不成立。

### C. 原版运行时对「未知档」到底做什么（三条支，逐字）

| 函数 | 消费场景 | 未知档的行为 | 判据 |
|---|---|---|---|
| `AbilityLogic.GetValueFromCriteria` | **伤害 / 治疗执行取值** | **`return minValue`**（不随机、不乘系数） | `decomp_full/AbilityLogic__GetValueFromCriteria.c` —— 全部 15 处 `return uVar6`，而 `uVar6 = param_3 = minValue`（`:25`） |
| `AbilityLogic.NewBuffStat` | **buff 五对属性折叠** | **先 `CustomDebug.Log(前缀 + Enum.ToString(option))`，再 `return minValue`**（⚠️ **不乘 multiplier**，与 `Normal(0)` 支的 `minValue × multiplier` 不同） | `AbilityLogic__NewBuffStat.c:275-279` vs `:61-68` |
| `AbilityLogic.GetAverageValue` | **AI 评分**（`DamageCriteria.GetAverageDamage` 唯一调用者就是 AI） | `Math.Round((min+max)/2)` 银行家舍入 | `AbilityLogic__GetAverageValue.c:28`（`LAB_18081692f`）+ `:136`（唯一特例 `80`） |

> `GetAverageDamage` 全仓调用者 = **只有** `AI__ScoreFromCriteria.c`（`grep -l` 实测）。
> 我们 `AiScoring.AverageValue`（`RuleEngine/Data/AiScoring.cs:770-775`）**已经与之逐字一致**（`80` 特例 + 其余全走平均值）⇒ **AI 侧无需改动**，
> 而且 `ai_scoring.json` 现读 `valueOptions` 只有 `{0:380, 80:1, 90:1, 204:1}` —— **根本没有 8/17 进来**。

### D. 可达性：这 46 行**全都不被执行**（不是「碰巧数值一样」，是结构上读不到）

结构依据（`d:/2/tools/il2cpp_out/dump.cs:19456-19470`，字段偏移是真值）：

```
AbilityLogic.chosenAbility   // 0x10   这一行「做什么」的唯一真值
AbilityLogic.damageCriteria  // 0x40   ← valueOptions=8 在这
AbilityLogic.healCriteria    // 0x48   ← doHeal 用的是这个，不是 damageCriteria
AbilityLogic.buffEffect      // 0x50   ← healthValueOptions=17 在这
```

- **38 行 `valueOptions=8`**：`chosenAbility` 分布 `{0,20,30,60,70,100,320}`，**没有一个是伤害族（10/12/13/15）**。
  `damageCriteria(+0x40)` 只在伤害族被读。`PlayAbility` 里 4 处 `GetValueFromCriteria` 调用点**已逐个核过归属**：
  `:1274` 在治疗支、读的是 `+0x48 healCriteria`；`:1475` 在 `switch` 的 `case 10 / 0xd`；`:1546` 在 `case 0xc`；`:1642` 在 `case 0xf`
  —— 后三处都 `lVar11 = *(param_1 + 0x40)`（= `damageCriteria`），且 case 值就是 `doDamage 10 / doDamageMultiRandom 12 / doDamageFast 13 / doDamageAndDestroySelf 15`。
  ⇒ **38 行全部读不到**。
- **8 行 `healthValueOptions=17`**：
  - 2 行（`GOF30 0/0`、`GOF32 0/0`）是 `doDamage` ⇒ `buffEffect` 不应用；
  - 3 行 `healthChange` 缺（0）⇒ `NewBuffStat` 直接吐 0；
  - 3 行（`GOF36 ×2`、`GOF43`）是 `buffType=3 addTrait` 且 `healthChange=3` —— 这几行的 `NewBuffStat` **会**被调用（`AdjustCardEffect` 的闸含 `3`），
    但**应用阶段** `CardScript__AddEffect.c:460 case 3` 只 `AddTraitSilently`，**从不读 `+0x58`(armourChange) / `+0x60`(healthChange)**
    （属性应用只在 `case 1 addStats`：`:454-455` 的 `CardScript__AddArmour`）。
  ⇒ **8 行全部无效果**。

### E. `(3,5,5,8)` 是一坨**非伤害行的 editor 模板**（统计佐证）

按 `chosenAbility` 分组看 `damageCriteria` 的四元组（`dmgdist.py`）：

| chosenAbility | `(3,5,5,8)` 出现 | 该组总行数 |
|---|---:|---:|
| `0 drawCard` | 1 | 12 |
| `20 doHeal` | 1 | 10 |
| `30 buffOthers` | **22** | 103 |
| `60 buffMana` | 1 | 15 |
| `70 summonUnit` | 5 | 17 |
| `100 createCardInHand` | 1 | 13 |
| **`10 doDamage`** | **0** | **283** |

⇒ 这个四元组**从不**出现在真正的伤害行上；真正的伤害行用的是 `(n,n,0,0)` 或 `(1,k,5,90)`。
这与 `gen_cards_engine.py:1802` 已有的注释（「非伤害行的 damageCriteria 别当数值用」）**方向一致**，
只是那条注释只提到了 `(1,1,0,0)` 这一种模板，**漏了 `(3,5,5,8)` 这一种**（见 §五-2）。

### F. 分类定论

| 候选分类 | 是否成立 | 依据 |
|---|:--:|---|
| 枚举缺项（原版真有 8/17 语义，我们没抄全） | ❌ | 四份反编译（含 Ghidra dump.cs）全部 26 成员；且**没有任何代码路径**把 8/17 当成员处理 |
| 字段类型用错（那格其实是 `StatFilterOptions` / 别的枚举） | ❌ | bundle 的 typetree 明确写 `DamageCriteria.valueOptions` / `CardEffect.healthValueOptions`，两者在原版 C# 里都是 `ChooseValueOptions`（`DamageCriteria.cs:8` / `CardEffect.cs:50`）；且字节位置与字段序逐字对上 |
| 我们抽取串位 / 抄错 | ❌ | 原始字节直接命中 `08 00 00 00` / `11 00 00 00`；卡表两路一致 |
| **✅ 原版数据里就有超纲值（editor 残留模板），代码走 `default` 支** | ✅ | 上面全部 |

---

## §三 施工结论（改哪一层、改什么）

### 1. 枚举层 —— **不动**（⛔ 不许补 8 / 17）

- 判据 = §二·B 四份反编译。补一个「8」会让 G26 的分派表凭空多出一档**没有原版实现**的分支
  （= 静默自设计，且以后再也分不清「复刻的」与「我们编的」）。
- 若为了「出声」需要在文档/代码里给个名字，写成 `8/17 = Unknown（原版数据超纲值）` 放在**对照表的备注列**，
  ⛔ **不要写进枚举定义**。

### 2. 解析 / 运行层（G26 / G27）—— **必须逐字实现 `default` 支**（这是本判定的唯一施工项）

| 落点 | 要写的代码 | 判据 |
|---|---|---|
| `EffectResolver.StatOf` / 新的取值器（G26） | 未识别的 option ⇒ **返回 `minValue`（`damageCriteria.minDamage` / `healCriteria.minValue`）**，并且**出声**（日志一条，⛔ 不静默） | `AbilityLogic__GetValueFromCriteria.c:23` + 15 处 `return uVar6`（`:40,47,75,84,114,131,134,141,165,178,196,203,224,330,364`） |
| buff 折叠（G27，`ApplyOneGain` 那一路） | 未识别的 option ⇒ **返回原始 `*Change` 值，⛔ 不乘 `multiplier`**，并先打一条日志（原版打的是 `CustomDebug.Log`，不是 Error） | `AbilityLogic__NewBuffStat.c:275-279`（fallback）vs `:61-68`（`Normal` 支会乘） |
| AI 评分 | **已经对了**（`AiScoring.AverageValue` = `80` 特例 + 其余 `round((min+max)/2)`） | `AbilityLogic__GetAverageValue.c:28,136` |

**两条 ⛔ 硬禁止（旧文档 §四·K 的担心就出在这）**：
- ⛔ **不许把未识别档「按 0」**：原版不是 0（伤害取 `minValue`、buff 取原始 `change`）。
- ⛔ **不许把它「当 `Normal`」**：两者在 `NewBuffStat` 里**不等价** —— `Normal` 会乘 `multiplier`，未知档不乘
  （实据：`PlayAbility` 会把 `furtherMultiplier` 乘进 `multiplierFactor`，`AbilityLogic__PlayAbility.c:235-236`；
  `GOF43` 的 `furtherMultiplier=3` 正好落在这条线上）。
  对池内这 46 行**碰巧无影响**（§二·D），但取值器是给**全池 26 档**用的，写错会污染将来出现的真未知档。

### 3. 数据层 —— **一律不改**

- `cards_truth.json` / `cards_raw.json` / `cards_engine.json` / `ai_scoring.json` **保持 8 / 17 原样**。
- ⛔ 不许把 8 改成 0、不许删掉这两类行 —— 它们是「原版数据里就有超纲值」的**唯一活标本**，
  正好用来验证 §三-2 的 `default` 支实现得对不对；抹平了以后就无法验收。

### 4. 验收口（G26/G27 落地时加）

1. **表驱动单测（26 档 + 2 个未知档）**：给定 thisCard/target 后断言取值；
   其中 `8 → minValue`、`17 → 原始 change（不乘 multiplier）` 两条是**本判定的专用回归**。
2. **一条「出声」断言**：未知档必须产生一条日志（对应原版 `CustomDebug.Log`），⛔ 断言「静默」。
3. `ruleprobe check` 仍须「解析差异 0 行 · 哨兵 0」。

---

## §四 还差什么（本代理没查的部分，别当成已核）

1. **没有穷举 `AbilityLogic.damageCriteria(+0x40)` 的全部读取点** —— 本轮核过的是
   `PlayAbility` 的 4 处 `GetValueFromCriteria` 调用点 + `GetAverageDamage`（唯一调用者 = AI）。
   理论上可能还有 UI/卡面文本渲染读它（未查）；给 G26 的保险做法：**落地前用反汇编扫一遍 `+0x40` 的读**。
2. **没有核原版编辑器为什么会产生 `(3,5,5,8)`** —— 本机没有原版编辑器，只能记成「原版数据既有」，
   ⛔ 不要写成「我们发现原版有 bug」这种因果断言。
3. **没有判定 `damageType=5`（`DamageType.Random`）本身的语义** —— 它和这两个异常值 38/38 同时出现，
   但那是 G26 之外的另一条线（`PlayedThirdCardInTurn.cs:200-209` 里已有 90 档的近似实现），本文件**不涉及**。
4. **`4b8214d3…` Storm of Silence 为何不在池内**（且 `desc` 为空）—— 与本任务无关，未查。

---

## §五 顺手发现

1. 🔴 **2 条 `doHeal` 行的 `damage` 是 `(3,5,5,8)` 垃圾，真正生效的是 `heal`** ——
   `Righteous Fury` 的 `heal=(min 1,max 1,opt 0)` 与卡面「Heals **1**」吻合；`Sawbonez` 的 `heal=(1,5,0)` 与「heal **1-5**」吻合。
   **任何把 `damage` 当「通用取值结构」的实现都会把这两张的治疗算成 3** —— 这是 G26 落地最高危的一处，
   建议在施工单里点名（`AbilityLogic.healCriteria` = `+0x48`，判据 `dump.cs:19468`）。
2. ⚠️ **`gen_cards_engine.py:1802` 的注释不完整**：「非伤害行里那对 `(1,1,0,0)` 是 SO 的序列化默认值」——
   实际非伤害行还有**第二种**模板 `(3,5,5,8)`（22+ 行，§二·E），并且它**从不**出现在伤害族行。
   （本轮按铁律 13·3 **未改** `.py`，留给调度台派执行代理补注释。）
3. ℹ️ `ai_scoring.json` 现读只有 `{0:380, 80:1, 90:1, 204:1}` —— 与 `AiScoring.cs:763` 注释说的「非 0 只有 3 条（80/90/204）」一致，
   **8/17 从未进入 AI 数据**。这条同时证明：我们 AI 侧不会因为这两个值产生偏差。
4. ℹ️ 版本配对（`app 1.35.0 · Unity 6000.2.6f2` ↔ allcards bundle `6000.2.6f2`）本轮现读自
   `d:/2/unity_run_ref/Warpforge_Data/globalgamemanagers` 与 bundle 头的明文串；**这条以前没有落在任何文档里**，建议并入
   `资料/完全复刻/判据/` 的公共前提。
