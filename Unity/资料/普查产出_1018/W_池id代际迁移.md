# W · 池 id 代际迁移（2026-10-14 `601005b` 换号造成的旧存档 / 旧回放错卡）

> 执行代理产出 · 2026-10-18。判据来源 = `资料/普查产出_1018/R_防御卡位错卡_成因.md`（下文简称 **R**）。
> ⛔ 未跑 Unity / 未跑任何 `*.Run`；只跑了秒级类型检查（§5）。

## 1. 迁移表：1102 条，**对不上的 0 条**

| 分块 | 条数 | 判据 |
|---|---|---|
| **主表** | **1092** | `git show 601005b^:…/cards_engine.json` ↔ **当前** `cards_engine.json` 按 `(name, faction)` **精确 join**，只收 `旧 id != 现 id` 的 |
| **改名表** | **10** | 旧名与现名不同（名字 join 接不上），判据 = **PnP 编号链**（见 §1.2） |
| **合计** | **1102** | 写进 `RuleEngine/Data/LegacyCardIds.cs` 的静态表（产物 **104 KB / 1467 行**） |

- **为什么是 1092 而不是任务里估的 955**：955 = `601005b^ → 601005b` 那一步改的张数。
  但 **`K06`（2026-10-18，`3251df0`）又改了 137 张**，而我们 join 的是「**10-14 之前的旧池 ↔ 现池**」
  ⇒ 1092 = 955 ∪ 那 137（两张集合的交集已去重）。**表的目标必须是【现池 id】**，否则迁完仍然是死 id
  （例：`ASH_Empyric_Ambush` 那一批现在是 32 位 GUID 形态）。
- 两个文件里 `(name, faction)` **各自都没有重复**（1126 / 1127 张）⇒ 没有一对多歧义。
- 旧池 1116 张按名字在现池找得到，剩下 **10 张是改名**（下表），**再没有别的对不上的**。
- 现池**新增 1 张** `TL40 Infestation Node`（Leviathan，旧池确实没有）⇒ 表里没有它，正常。
- 表里 **890/1102 个「新 id」本身又是另一条映射的 key**，**最长链 31 跳**
  （`ASH20→ASH15` 与 `ASH15→ASH11` 这种）。这就是「必须一次遍历、只看原始值」的定量依据。

### 1.1 对不上的（改名表 10 条，逐条列）

| 阵营 | 旧 id | 旧卡名 | 现 id | 现卡名 | 判据（PnP 卡图编号链） |
|---|---|---|---|---|---|
| AstraMilitarum | `AM1` | Commissar Elan | `AM3` | Commissar Denkler | `Warpforge_01_Commissar-Denkler.png` + 官方 `Card_Name/AM3`；`Warlord_Lore/AM3` = 「Commissar **Elan Denkler**」（Elan 是名、Denkler 是姓） |
| TauEmpire | `TAU64` | Experimental Drone | `TAU42` | Experimental Drones | `Warpforge_64_Experimental-Drones.png` |
| TauEmpire | `TAU63` | Saviour Protocolst | `TAU51` | Saviour Protocols | `Warpforge_63_Saviour-Protocols.png` |
| Sautekh | `SAU45` | Extermination Protocols | `SAU38` | Extermination Protocol | `Warpforge_45_Extermination-Protocol.png` |
| Sautekh | `SAU47` | Reconstitution Protocols | `SAU39` | Reconstitution Protocol | `Warpforge_47_Reconstitution-Protocol.png` |
| Genestealers | `GSC68` | Rusted Vents | `GSC64` | Rusted Vent | `Warpforge_68_Rusted-Vent.png` |
| Genestealers | `GSC64` | Telephatic Domination | `GSC49` | Telepathic Domination | `Warpforge_64_Telepathic-Domination.png` |
| Sororitas | `SOR19` | Sisters Repentia | `SOR10` | Sister Repentia | `Warpforge_19_Sister-Repentia.png` |
| BlackLegion | `BL5` | Sylar Hexcorn | `BL36` | Sylar Hexscorn | `Warpforge_5_Sylar-Hexscorn.png` |
| DarkAngels | `DA11` | Sergeant Naaman | `DA6` | Sergeant Taaman | `Warpforge_11_Sergeant-Taaman.png`（旧 id 的数字后缀 = PnP 编号） |

- 判据链：「旧 id 的数字后缀 = PnP 卡图编号」是本仓既有口径（`A970`）；
  该编号那张卡**印的名字** = 现池卡名（铁律 7：卡面是印刷品上的字，可逐张核）。
- ⚠️ **唯一一条原先在文档里标「未决」的是 `DA11 Sergeant Naaman → DA6 Sergeant Taaman`**
  （`资料/卡表核对_卡图提取/_裁定_图标丢失.md:75` 写「Naaman 与 Taaman 是不是同一张卡 —— 未决」）。
  那句是**按卡名查**得出的；按 **PnP 编号**（`DarkAngels__1.md:37` = `Warpforge_11_Sergeant-Taaman.png`）
  就是同一张。**如果调用方认为这仍属「猜」，删掉表里那一行即可**（`{ "DA11", "DA6" }`，改名为 `Sergeant Taaman` 那行注释
  写着证据），其余 9 条不受影响。

## 2. 双解验证：判定规则与日志样例

### 2.1 规则（`LegacyCardIds.MigrateDeckVerified`，唯一的整副入口）

```
① 防御卡位（无歧义，**总是先跑**；现核：旧池 39 个防御 id 与现池 39 个**一个都不重合**）
   现池里这条引用【是防御卡】            ⇒ 不动（干净数据 / 已迁过）
   现池里不是、旧代同 id 是防御卡        ⇒ 按旧代表迁
   查得到、但不是防御卡（两代都不是）    ⇒ 清空（Validate ③ 允许无防御卡）
   彻底查不到（id / 卡名 / 旧代表都没有）⇒ **原样保留 + 出声**（见 §5 的偏离②）

② 督军 + 普通卡位（整副双解；⚠️ 一个 id 单看无解 —— 891/1102 个旧 id 今天仍是合法 id）
   现代解合法                          ⇒ 不动（这也是**幂等**的判据）
   现代不合法 ∧ 旧代解合法             ⇒ **迁**（出声）
   两解都合法 / 都非法 / 没有可迁的旧键  ⇒ **不动**（两解都合法时出声说明是代际歧义）
```

- **一次遍历、只看原始值**（`ApplyMap`）：写第 i 格时读的仍是**替换前**那一格 ⇒ 链式 id 不会二次替换。
- **只改内存**：`DeckStore` / `ReplayStore` 的玩家文件一个字都不碰。
- **幂等**：⚠️ **不是**靠「新 id 里查不到旧键」（那句今天**不成立** —— 890/1102 个值本身就是 key），
  而是靠②的**双解闸门**：迁完之后这副牌在**现代解**下就合法了 ⇒ 再进这个函数第一步就返回。
  实测见 §4 的「再迁一次：0 处」。

### 2.2 日志样例（本机那副「复仇者之刃」读档时会打出来的两行）

```
[LegacyCardIds] 防御卡位旧 id 迁移（存档「复仇者之刃」）：`ASH67` → `ASH55`（现池里 `ASH67` 不是防御卡、
    **旧代同 id 是** ⇒ 无歧义；⛔ 只改内存）
[LegacyCardIds] 旧 id 迁移（存档「复仇者之刃」）：现代解不合法（UnknownCard）、旧代解合法 ⇒ 按**双解**判定迁移
    30 处（`ASH19` → `ASH62` ×2 · `ASH20` → `ASH15` ×2 · … · `ASH_Autarch` → `ASH83` ×2）—— ⛔ 只改内存，文件不动
```

回放侧（**动作里的 id 只在「这份牌被判定为旧代」时才迁**）：

```
[LegacyCardIds] 旧 id 迁移（回放 `20261011_…json` · actions[..].defId）：`GOF61` → `GOF49`
    （同代整份录像；⛔ 只改内存，文件不动）
```
（`GOF61` 旧 = `Gretchin Tower`(defence) → 现 `GOF49` = `Gretchin Tower`(defence)，逐张核过。）

### 2.3 五个接入点

| 入口 | 做法 |
|---|---|
| `DeckStore.LoadAll` | 卡池查表口**只建一次**（`LegacyCardIds.PoolLookup()`，池子没加载 ⇒ null ⇒ 整段跳过），每套牌跑一次 `MigrateDeckVerified` |
| `ReplayStore.Load` | `MigrateDeckJson`（两份牌各一次，带 `out bool migrated`）；`pickIds/defId` **只有 `migrated` 为真时**才按同表迁 |
| `DeckBuilder.FromDeck` | 最后一道闸（联机对端下发的 `hostDeckJson` / 自检夹具也会走到），查表口用本函数自己的 `index` |
| `DeckLibrary.ImportString` | 先按**现代**解分类并 `Validate`；不合法再按**旧代**解（整串迁一次）分类 + `Validate`；只有「现代不合法 ∧ 旧代合法」才返回旧代解 |
| `DeckRuntime.PlayerDeckForDemo` | 防御卡改成 `pick_defensive` 同一规则（本阵营 defence 里 **cost 最低 → id 最小**），落库前断言 `Type == "defence"` |

## 3. 改动清单

| 文件 | 行 | 改动 |
|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Data/LegacyCardIds.cs` | 51–1154（表）· 1189 `IsLegacy` · 1196 `ContainsAny` · **1218 `PoolLookup`** · **1241 `MigrateDeckVerified`** · **1265 `MigrateDefensive`** · **1315 `MigrateByDualSolution`** · 1360 `ApplyMap` · 1404 `MigrateDeckJson`（加 `out bool`）· 1440 `MigrateArray` · 1464 `MigrateIds` | 整表重写：4 条 token → **1102 条代际表**；迁移口从「无条件查表」改成「**双解 + 防御位无歧义**」 |
| `Unity/MyGame/Assets/RuleEngine/Data/DeckStore.cs` | 174–185（查表口）· 201–203（调用） | `LoadAll` 里每套牌过 `MigrateDeckVerified`；池子没加载 ⇒ 跳过 |
| `Unity/MyGame/Assets/RuleEngine/Data/DeckBuilder.cs` | 249–265 | `FromDeck` 的旧闸改用 `MigrateDeckVerified`（查表口 = 本函数的 `index`） |
| `Unity/MyGame/Assets/RuleEngine/Data/DeckLibrary.cs` | 407–452（`ImportString`）· 455–476（新 `SplitByType`） | 导入串：现代解 / 旧代解两路，双解判定 |
| `Unity/MyGame/Assets/CardPresentation/Battle/ReplayStore.cs` | 482–493（`Load`）· 497–531（`MigrateLegacyIds`） | 两份牌走双解；`pickIds/defId` 只在旧代局里迁 |
| `Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs` | 8460–8486（`PlayerDeckForDemo`） | 防御卡 = `pick_defensive` 规则（cost 最低 → id 最小）+ `Type == "defence"` 断言 |
| `Unity/_tmp_view/gen_legacy_card_ids.py`（**不入 git**） | — | 离线生成表（可复算） |
| `Unity/_tmp_view/verify_legacy_migration.py`（**不入 git**） | — | 只读自验（照 C# 算法逐句复刻） |

## 4. 只读自验（`_tmp_view/verify_legacy_migration.py`，**只读、不写任何玩家文件**）

对象 = 本机 **2 个 profile 的 2 套「复仇者之刃」+ 3 份回放**（10-10 那份两副牌都空，不涉及；
10-11 / 10-13 / 10-15 三份 `hostDeckJson` 都是同一副旧代牌）。**合计 5 个卡组实例。**

| 实例 | 迁移前 | 迁移后 | 迁移处数 | 防御位 |
|---|---|---|---|---|
| 主 profile「复仇者之刃」 | `UnknownCard`（`ASH_Autarch`） | **`None`** | **31**（30 张 + 防御位） | `ASH55` **Aspect Shrine / defence** |
| 副 profile「复仇者之刃」 | 同上 | **`None`** | 31 | `ASH55` Aspect Shrine |
| 回放 10-11 `hostDeckJson` | 同上 | **`None`** | 31 | `ASH55` Aspect Shrine |
| 回放 10-13 `hostDeckJson` | 同上 | **`None`** | 31 | `ASH55` Aspect Shrine |
| 回放 10-15 `hostDeckJson` | 同上 | **`None`** | 31 | `ASH55` Aspect Shrine |

- 迁移后 = **30 张 = 17 种 · 全 `SaimHann`**（同名上限全过）⇒ 与 **R §4** 预期的结果**逐项吻合**。
- **幂等实测**：迁移后的牌再跑一次 ⇒ **0 处**（现代解合法 ⇒ 第一步返回）。
- **回放动作里的 id**：10-11 那份有 1 个（`defId=GOF61`）⇒ 旧代局 ⇒ `GOF61 → GOF49`（同为 `Gretchin Tower`/defence）；
  另外两份动作里没有卡 id。
- **按卡名的旧存档**（2026-09-13 之前那一代，`DeckRulesTest` 的 `_deckstore_selftest` 夹具形式：
  `WarlordId="Anvirr Keltoc"` / `DefensiveId="Aspect Shrine"`）：**迁移 0 处、防御位原样保留**（没有被误清）。
- 现池 SaimHann 防御卡三张：`ASH56 Inspiring Legacy`(cost 0) · `ASH57 Webway Entrance`(cost 0) · `ASH55 Aspect Shrine`(cost 1)
  ⇒ `pick_defensive` 规则挑 **`ASH56`**（与 103 副预组一致）—— 这是**新建演示卡组**的结果；
  旧存档迁出来的是 `ASH55`（保留玩家原来那张），两者不同是**预期**。

### 与任务书的三处有意偏离（都写进了代码注释）

1. **表的条数 1092（≠ 955）** —— 见 §1，目标必须是**现池** id（`K06` 之后又改了 137 张）。
2. **「幂等 = 新代表里查不到旧键」这条做不到** —— 890/1102 个值本身又是 key、最长链 31 跳。
   改成「**双解闸门**」实现幂等，实测 0 处（§4）。这条必须这样，否则第二次迁移会**静默改成第三张卡**。
3. **`DefensiveId`「查不到就清空」→ 改成「查得到但不是防御卡才清；彻底查不到 ⇒ 原样保留 + 出声」**。
   理由：`CardDatabase.DeckLookup` 的第 ② 条（按卡名）是给 **2026-09-13 之前按卡名存的旧存档**留的，
   「彻底查不到」分不出「垃圾 id」和「更老的按卡名存档」，而**清空是不可逆的**（玩家下次保存就写回空）；
   留着它的代价只是「这一局少一张防御卡」（`DeckBuilder.FromDeck` 会 `LogError` 后丢牌，**不静默**）。

## 5. 没查清 / 没做

- **未跑 Unity、未跑 `*.Run`、未真机**（任务红线）。⇒ **回放的 `finalHash` 是否已恢复可复现，没实测**；
  只能说「迁完这副牌回到满 30 张、`DeckRules.Validate` 合法」，与 `finalHash` 的算法前提一致。
- 只扫了本机 **2 个 persistentDataPath profile**；其它机器 / 路径的存档未覆盖（迁移是代码行为，不依赖本机扫描）。
- **两解都合法 / 都非法的卡组**：按任务书口径**不动并出声**（日志见 §2.1）；
  本机数据里没有落进这一档的实例。
- `K06` 之后（10-14 ~ 10-18）**新写出来**的卡组若带的是**中间代** id：本表是「旧池 → 现池」，
  **不专门覆盖中间代**。实测本机 4 份回放 + 2 份存档里**没有中间代 id**（都只带旧代 id），
  且中间代 id 与旧代 id 的 key 集合**零冲突**（脚本里 assert 过）；以后扫到再单独加一张表。
- `_tmp_view/*.py` 两个脚本**按任务要求不入 git**；表本身已固化进 `LegacyCardIds.cs`（离线可复算）。

## 6. 类型检查原文

```
$ TMPDIR=/tmp/wf_f6 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
