# 633 条 `inferred-keyword` 升 `authoritative` · 执行报告

> 执行代理（可写）。日期 **2026-10-23**。仓库根 `d:/4`。
> ⛔ 未跑 Unity、未动 git、未改正本（`CLAUDE.md` / `项目任务.md`）、未改其他线台账。
> 判据 = 同目录 `安卓存档_CardTraitSO普查.md`（安卓 1.35.0 的 `sharedassets0.assets` 与
> `数据/游戏数据/trait_collection.json` **逐字段 0 差异**）。

---

## 〇、一句话

`card_vfx_by_card.json` 的 **638 条 `inferred-keyword` 已经逐条用 `CardTrait` SO 权威数据复核**：
**633 条升 `authoritative`**（`src: cardtrait-so`）、**5 条 `penitence` 标 `unknown`**、**0 条"动画名对不上"**、
**0 条仍留在 `inferred-keyword`**；**每一条的 `cardanim` / `prefab` 一个字节没动**。
生成器里那段「600+ 条升不了级」的过期结论**已就地订正**（含错因与判据路径）。

---

## 一、升级规则（写死在 `工具/gen_card_vfx_by_card.py` 的 `promote_keyword_traits`，`:1277`）

### 1·1 判据从哪来

| 输入 | 说明 |
|---|---|
| `Unity/数据/游戏数据/trait_collection.json` | `CardTrait` SO 的权威导出（77 个 `TraitPair` / 73 个 `CardTrait` SO / **75 条 `TraitEffect`**；生成于 2026-10-22） |
| `cards_engine.json` 的 `keywords` / `faction` | 这张卡有哪些关键词（**必须是卡自己写着的那个关键词**）、它是哪个阵营 |
| `stats.factionArmyIds` | 阵营名 → 原版 army id（`overrideByArmies` 用的就是这组 id） |

### 1·2 匹配算法（一行一条，⛔ 不猜）

1. 取条目上**推导时用的那个关键词**（② 生成时新记的 `keyword` 字段，`:747`），按与 ② **同一口径**归一化
   （只取空格 / 冒号 / 括号之前的第一段：`Talent: Serene Unifier` → `talent`）。
2. 在 `trait_collection.json` 里找**同名 trait**（归一化后同名）。
3. 在该 trait 的 `traitEffects[]` 里找**与条目 `cardanim` 同名的 `effectAnim`**
   （**命中 `effectAnim` 本身，或命中它任一 `overrideByArmies[].effectAnim`** —— 后者正是普查 §四说的「含按阵营覆盖」）。
4. 权威触发档 = 那条 effect 的 `triggerType` / `triggerName`；**事件桶必须与现在所在桶一致**才升
   （`0:Start→trigger · 2:OnPlay→ability · 3:Trigger→trigger · 6:TriggerOnPlay→trigger`）。

### 1·3 四条出路

| 情况 | 处理 | 今天条数 |
|---|---|---|
| 动画名吻合 | **升 `authoritative`**（`src: cardtrait-so`）+ 带 `traitName` / `traitEffectName` / `traitTriggerName` / `traitTriggerType`（权威档位）/ `playIfWarlord` / `authoritativeAnim` / `keyword` / `confBefore` | **633** |
| 关键词**不是**任何 trait（`penitence`） | **标 `unknown`** + `why` 写清「无权威依据，不是没查」 | **5** |
| 关键词是 trait，但**动画名对不上** / 多条 effect 争不清 | **原样留 `inferred-keyword`** + `PROBLEMS` 出声 | **0** |
| 权威 triggerType 归到的事件桶 ≠ 当前桶 | **不升** + 出声（挪桶要人裁） | **0** |

### 1·4 升的时候**不顺带改**的两件（写进代码注释了）

- **`prefab` / `cardanim` 一律不动**（本件口径）。按阵营走 `overrideByArmies` 的 **6 条**
  （`Shield` 的 EC/TAU 变体）真实该播的是变体 ⇒ 权威名记在 `authoritativeAnim` 上并打
  `animDiffersFromCardanim: true`；**运行时改播变体是另一件事**（要真机验收），见 §七·3。
- **不挪事件桶**（即使权威档位暗示该挪）—— 换桶 = 换播放时机。

---

## 二、结果：638 → 633 / 5

### 2·1 总数

| 项 | 升之前 | 升之后 |
|---|---|---|
| `inferred-keyword` | **638** 条 / 474 卡 | **0** 条 |
| `authoritative` | 3,949 条 | **4,582** 条（+633） |
| `unknown` | —（新档） | **5** 条 / 5 卡 |
| `inferred-name` | 59 条 / 42 卡 | 59 条 / 42 卡（**没动**） |
| 卡池 | 1,127 张（覆盖 1,127） | 1,127 张（覆盖 1,127） |

有被升级条目的卡 = **471 张**（474 − 3：`SOR10` / `SOR32` / `SOR27` 的推断行**只有** `penitence`）。

### 2·2 按 trait 分布（19 个 trait，合计 633）

| trait | 条数 | trait | 条数 | trait | 条数 |
|---|---|---|---|---|---|
| rally | 105 | tide | 23 | regeneration | 9 |
| talent | 96 | camouflage | 21 | companion | 8 |
| armour | 81 | artifice | 19 | concussive | 6 |
| flying | 77 | mob | 16 | backlash | 6 |
| flank | 41 | shield | 13 | | |
| codex | 31 | ecstasy | 10 | | |
| stealth | 31 | cruelty | 9 | | |
| strike | 31 | | | | |

### 2·3 顺带订正的两样（权威数据比推断多给的信息）

1. **`traitTriggerType` 被订正 10 条** —— 全是 `EcstasyTraitTrigger`（EC5/7/9/12/19/21/25/30/31/57）：
   构词法按 `…TraitTrigger` 后缀推的是 `EffectTriggerType.Trigger(3)`，
   **权威是 `TriggerOnPlay(6)`**。两者都归 `trigger` 桶 ⇒ **事件桶不变**，只订正档位。
2. **`overrideByArmies` 带出 13 条**（全是 `Shield` 行 —— 这条 trait 有 4 个阵营变体）。
   `Invulnerable` 那条 trait 也有 6 个变体，但**我们池子里带 `Invulnerable` 关键词的卡 = 0 张**
   （现读 `cards_engine.json`）⇒ 这次一行都没命中，不是漏了），
   其中 **6 条**本阵营真的命中 override ⇒ 打 `animDiffersFromCardanim` 并写 `authoritativeAnim`：

| 卡 | 桶 | 本行 `cardanim` | 权威（本阵营） | 阵营 |
|---|---|---|---|---|
| `EC20` | ability | `ShieldTraitPlay` | **`ShieldTraitECPlay`** | EmperorsChildren |
| `TAU17` / `TAU36` / `TAU32` / `TAU19` / `TAU3` | ability | `ShieldTraitPlay` | **`ShieldTraitTAUPlay`** | TauEmpire |

（其余 7 条 `Shield` 行的阵营不在 override 列表里 ⇒ 权威就是基础名，未打标。）

### 2·4 5 条 `penitence` 怎么标

| 卡 | 关键词 | 原 `cardanim` / `prefab` | 新 `conf` |
|---|---|---|---|
| `SOR8` | `Penitence` | `PenitenceTraitTrigger` / `PenitenceEffect` | **`unknown`** |
| `SOR21` | `Penitence` | 同上 | **`unknown`** |
| `SOR10` | `Penitence` | 同上 | **`unknown`** |
| `SOR32` | `Penitence` | 同上 | **`unknown`** |
| `SOR27` | `Penitence` | 同上 | **`unknown`** |

`why` 原文（生成器逐条写入）：**「⛔ 无权威依据（2026-10-23）：`trait_collection.json`（73 个 `CardTrait` /
77 个 `TraitPairs`，安卓 1.35.0 交叉验证逐字段 0 差异）里没有关键词 `Penitence` 这个 trait ⇒
「`<关键词>Trait*`」这条构词法推断（= `PenitenceTraitTrigger`）升不了级。`prefab` 保留只为可查，⛔ 不许当权威播。」**

⚠️ 补一条本次顺手查到的事实（**不是**这次的判据，但别丢）：这 5 张卡的 **`ability` 桶里另有一条卡 SO 的
`authoritative` 行**（`PenitenceEffect` / `PenitenceTraitTrigger`），所以「这个特效该不该播」在卡 SO 那一层
其实是有依据的；这 5 条缺的只是 **`CardTrait` 那一层的依据**。

---

## 三、逐条不变量证明（自检 ①：卡 id / 动画名逐条不变）

方法：把**改前**（`git HEAD` 那一版，`generatedAt` 2026-10-20 19:30:00）与**改后**的 `byCard` 逐卡 / 逐事件 /
**逐条按下标 zip** 比（升权威**不增删、不重排**条目，只改字段）。

读数：

```
cards 1127 -> 1127 , same keys: True
structure / animation mismatches: 0          ← 1127 张卡的 (卡,事件) 集合、条目数、顺序、cardanim、prefab 全等
field deltas:
  ADDED   keyword 638 · confBefore 638
  ADDED   traitName 633 · traitEffectName 633 · traitTriggerName 633 ·
          playIfWarlord 633 · authoritativeAnim 633 · overrideByArmies 13 · animDiffersFromCardanim 6
  CHANGED conf 638 · why 638 · src 633 · traitTriggerType 10
  REMOVED （无）
```

`conf` 的 638 次变化 **全部**是 `inferred-keyword → authoritative`（633）与 `inferred-keyword → unknown`（5），
**没有第三条路径**。

**独立复核**（另写一段代码，不读本报告的结论）：633 条逐条核对
`keyword → trait`（卡自己写着吗 / 归一化后同名吗）· `traitEffectName → effectAnim`（含本阵营 override）·
`triggerType` / `triggerName` / `playIfWarlord` 与权威逐字段相等 ⇒ **633 条复核，0 条不符**。

---

## 四、幂等（复跑逐字节相同）

- 固定时间戳跑两遍：`python -I -X utf8 Unity/工具/gen_card_vfx_by_card.py --generated-at "2026-10-23 12:00:00"`
  ⇒ **两份产物 `cmp` 逐字节相同**（`FULL_IDEMPOTENT` / `RT_IDEMPOTENT`）。
- 交付态的最后一次是**真实时间戳**跑（`generatedAt = 2026-10-23 15:13:03`）；除 `generatedAt` 外与固定时间戳那版一致。
- `--no-truth` 路径也实跑过一次（确认升权威那一步被正确跳过、不炸），随后用正常参数重新生成覆盖回来。
- 行尾：三份文件跑完仍是 **LF、0 个 CRLF**（`gen_card_vfx_by_card.py` 1,977 行 · 全量 JSON 85,638 行 ·
  运行时 JSON 单行 388,371 B）。

---

## 五、生成器改了什么（`Unity/工具/gen_card_vfx_by_card.py`）

| 位置 | 改动 |
|---|---|
| 文件头 ⑨（`:44-52`） | 新增输入 ⑨ = `trait_collection.json`，写明判据路径、两个 build 交叉验证、以及「不在束里是对的」 |
| 文件头「四层」段（`:94-100`） | `inferred-keyword` 说明改成「2026-10-23 起已复核」；新增 `unknown` 一档 |
| `TRAIT_COLLECTION`（`:165`） | 新常量 |
| `CONF_PRIORITY`（`:169-173`） | 末尾加 `"unknown"`（判据为空那一档；⛔ 不排在推断前面） |
| `build_by_card` ②（`:743-747`） | 推断条目**新记 `keyword`**（下游按它找 trait，⛔ 不从 `why` 文案反解） |
| `TRAIT_TRIGGER_EVENT` / `TRAIT_TRIGGER_NAME`（`:1233-1240`） | 只用于**校验事件桶不变**；注释写明这不是原版语义 |
| `load_trait_authority`（`:1244`） | 读 `trait_collection.json` |
| **`promote_keyword_traits`（`:1277-1455`）** | 本次主件：四条出路 + 统计 + 出声 |
| `main` 里的调用（`:1534-1570`） | 在**权威档之后**跑（只改 `inferred-keyword`，不重叠），打印 `[3·b/4]` 一段，并把 `--no-truth` 一起闸住 |
| `hop2_corrected.limits`（`:1616-1630`） | 🔴 **订正那段过期结论**：原文「两个 build 的本地束里都没有 `CardTrait` 束 ⇒ 600+ 条升不了级」→ 改为「束里没有是对的（它不是 Addressables 资产）；数据在 player 自带的 `sharedassets0.assets`，已导出成 `trait_collection.json`，双 build 逐字段相同 ⇒ 633 条已升权威」+ **错因**（把「本地束里没有」当成「原版数据里没有」）+ 日期 |
| `chain` 的「旁路·关键词」（`:1658-1666`） | 同步改口径（原写「本地 0 命中」），`conf` 由 `inferred` 改 `authoritative` |
| `_sources`（`:1688`） | 加 `traitCollection` |
| `_headline`（`:1702-1707`） | 补一句「638 → 633/5」 |
| `_conventions.conf`（`:1715-1720`） | 新增 `unknown` 的释义；`inferred-keyword` 注明今天剩 0 条 |
| `stats`（`:1771`） | 新增 `stats.traitAuthority`（见 §六） |
| 运行时 `_note`（`:1924-1930`） | 说明 `c` 现在会出现关键词天赋的 `authoritative` 行与 `unknown` 行 |

`stats.traitAuthority` 现读：

```json
{"source":"d:/4/Unity/数据/游戏数据/trait_collection.json","sourceGenerated":"2026-10-22",
 "sourceCollection":"TraitsCollection","traitPairs":77,"cardTraitObjects":73,"traitEffects":75,
 "inferredKeywordBefore":638,"promoted":633,"promotedCards":471,"markedUnknown":5,"leftInferred":0,
 "triggerTypeCorrected":[…10], "armyOverrideRows":[…6], "unknownRows":[…5],
 "multiPlayBucketsBefore":1074,"multiPlayBucketsAfter":1192}
```

---

## 六、自检读数（自检 ①–④）

| # | 要求 | 实读 |
|---|---|---|
| ① | 633 条升级前后**卡 id / 动画名逐条不变** | ✅ 1,127 张卡结构 0 差异；**`cardanim`/`prefab` 变化 0 条**；改动字段只有 `conf`/`src`/`why`/`traitTriggerType`(+10) 与新增字段（见 §三） |
| ② | 5 条 `penitence` 变 `unknown` | ✅ `SOR8/SOR21/SOR10/SOR32/SOR27`，`conf == "unknown"` 计数 = **5** |
| ③ | 全池卡数不变 | ✅ 全量件 `cardsInPool 1127 / cardsCovered 1127 / cardsUncovered 0`；运行时件 `cardCount 1098 / entryCount 2694`（与改前**逐字段相同**） |
| ④ | 脚本回读 638 / 633 / 5 | ✅ `stats.traitAuthority = before 638 · promoted 633 · unknown 5 · left 0`；`byCard` 里 `src == "cardtrait-so"` = **633**、`conf == "unknown"` = **5**、`conf == "inferred-keyword"` = **0**；`stats.entriesByConf = {inferred-name 59, authoritative 4582, unknown 5}` |

运行时那份（`Resources/card_vfx_by_card.json`）逐条对比改前：**只有 638 个条目的 `c` 变了**
（633 → `authoritative`、5 → `unknown`）+ `confPriority` 末尾多一个 `"unknown"`；
`n`（prefab）/ `a`（cardanim）/ `audio` / 各项计数**全部相同**。

---

## 七、⚠️ 运行时后果（**要主对话拍板的三件**，本次只记不改）

1. 🔴 **同档多播桶 1,074 → 1,192（+118）**。
   `VfxMap` 的规则是「同档 `authoritative` **全播**」（`Core/VfxMap.cs:700-760`，判据 = 原版
   `CardAbility.cs:27-29`）⇒ 这 118 个 (卡, 事件) 会从「播 1 件」变成「**按序播 2 件**」。
   典型 = `BL17 Warp Talon` 的 `ability`（`FlyingTraitPlay` + `StealthTraitPlay`）。
   **这可能是对的**（原版天赋 + 卡能力本来就可能各播一件），但**必须真机看一遍**再落。
2. 🔴 **会红一条现成断言**（不在我的白名单，故未动）：
   `CardPresentation/Editor/BattleScene.cs:33116-33119` 的
   「`★ B3·T13（灭自证）`：`Warp Talon/ability` 两条 `inferred-keyword` 只播第一条 `FlyingEffect`」——
   升权威后那两条**都成了 `authoritative`** ⇒ `ResolveAll` 必然回 **2 条** ⇒ 该断言会红。
   **改法建议**：把「灭自证」的靶子换成**仍然只有推断档**的卡（如 `inferred-name` 或 `_unclassified`
   那一族），或改成「推断档平票只留第一条」的正向断言 —— 由战斗线/特效线决定。
3. ⚠️ **6 条 `Shield` 行的语义仍差一格**：`EC20` / `TAU17/36/32/19/3` 的 `prefab` 还是基础
   `ShieldTraitEffect_Idle`，而原版对本阵营播的是变体（`ShieldTraitECPlay` → `ShieldTraitEffect_Idle
   Emperor's Children Variant` / `ShieldTraitTAUPlay` → `… TAU Variant`，两件 prefab **都在我们的效果库里**、
   `anim_address_map.json` 都能解）。本件按「升级不动动画名」的口径**没改**；要改是另一件事（改的是播放内容，
   要 Unity / 真机验收）。
4. ⚠️ **`unknown` 那 5 条仍会进运行时表**（`c: "unknown"`）。`VfxMap` 把不认识的 `conf` 排最后，
   但**桶里只有它一条时仍会播** = **行为与改前完全一样**（改前是 `inferred-keyword`，也只有它一条）。
   「`unknown` 一律不播」需要额外改消费端（会改变这 5 张卡的播放），本次**没做**。
5. ⚠️ **下游工具会多出一批噪声**：`工具/gen_ability_anim_durations.py:137-138` 只按
   `conf == "authoritative"` 过滤 `byCard[*].ability[]`，现在会把 **142 条**关键词天赋行也捞进来，
   而它们没有 `abilitySlot` / `assetBundle` ⇒ 会被记进它的 `unresolved`（"CardAnim 名或束查不到"）。
   修法 = 加一条 `src != "cardtrait-so"`（或 `soField` 非空）的过滤；**该文件不在本次白名单**，未动。
6. ℹ️ `VfxMap.TriggerNameOf` / `TryResolveTriggerPicks` 的行为**不变**：这几条新权威行虽然带了 `a`
   （cardanim），但它们在 `AnimTimes` 里查不到对应条目 ⇒ 归口时照旧「证明不了自己属于这个 trigger」→ 丢掉
   （`Core/VfxMap.cs:1725-1760` 那段注释说的正是这一族）。

---

## 八、没查清 / 没做

1. **`penitence` 从哪来** 仍未查清（普查 §六·4 原话保留）：`PenitenceTraitTrigger` 这个 CardAnim 确实存在、
   5 张卡也确实带 `Penitence` 关键词、卡 SO 的 `ability` 桶里也真的引用了它 —— 但 73 个 `CardTrait` 里没有它。
   是代码里直接播的？还是别的 SO 配的？**本次没查**。
2. **多播桶 +118 的实际观感**没验（要 Unity / 真机）。
3. §七·3（6 条 Shield 变体）/ §七·4（unknown 是否该完全不播）/ §七·5（下游工具过滤）**都没做**，见上。
4. 本次**没有**跑任何 Unity 自检（红线）；§七·2 那条断言「会红」是**静态推的**（`ResolveAll` 的实现 +
   数据里两条同档 `authoritative`），**不是实跑结果** —— 落盘前请以实跑为准。
