# W · 旧卡 id 迁移闸（存档 / 回放 · 读时迁移 · 2026-10-18）

> 执行代理产出。判据输入 = `资料/普查产出_1018/R_引擎线_完整ID与SO后的调整评估.md` §2 / §5（**未重新查证**，按任务给的口径）。
> 本笔**没跑任何 `*.Run` 自检、没进游戏**（铁律 12）；只跑了秒级类型检查（`TMPDIR=/tmp/wf_f4`，见 §5）。

---

## 1. 扫描结果（只读，未改动任何存档/录像文件）

**扫描范围**（`Application.persistentDataPath` 下，编辑器口径）：
`C:/Users/qjh36/AppData/LocalLow/Unity Technologies/com.unity.template.urp-blank/` 与
`…/com_unity_template_urp-blank/`：

| 对象 | 份数 | 说明 |
|---|---|---|
| `WarpforgeDecks.json` | **2** | 各 **2 套 / 1 套**卡组；两家都有一副「复仇者之刃」 |
| `WarpforgeReplays/*.json` | **4** | 2026-10-10 / 10-11 / 10-13 / 10-15 各一局 |
| `WarpforgeDaily/daily.json` | 1 | 另扫：**不含任何卡 id**（正则扫旧 token 与 `ASH83` 形真 id 均 0 命中） |

**扫描口径**：把存档的 `WarlordId` / `DefensiveId` / `CardIds`、回放的
`start.hostDeckJson` / `start.clientDeckJson`（内嵌整份 `PlayerDeck` JSON）的同样三个字段、
以及 `actions[].pickIds` / `actions[].defId` **逐个**对
`MyGame/Assets/RuleEngine/Resources/cards_engine.json`（1,127 张，**全部有 id**）校验。

### 失效 id 全表（**只有这 4 个**）

| 旧 token | 出现处 | 每个位置的出现次数 | 映射到（已确认） | 判据 |
|---|---|---|---|---|
| `ASH_Autarch` | 2 份存档的「复仇者之刃」+ **3 份回放**的 `hostDeckJson` | **×2** | **`ASH83`**（`Autarch`） | ① 旧 token = `make_id("SaimHann","Autarch")`（`工具/gen_cards_engine.py:842-845` + `ID_PREFIX`：`ASH`→`SaimHann`）② `card_ids.json.mapping` 里 `ASH83`=`Autarch`，**SaimHann 阵营内该名唯一** |
| `ASH_Death_Spinner_Warp_Spider` | 同上 5 处 | **×2** | **`ASH77`**（`Death Spinner Warp Spider`） | 同上（唯一） |
| `ASH_Farseer` | 同上 5 处 | **×2** | **`ASH80`**（`Farseer`） | 同上（唯一） |
| `ASH_Orian_Laratharjos` | 同上 5 处 | **×1** | **`ASH79`**（`Orian Laratharjos`） | 同上（唯一） |

- **每副牌 7 处失效**（30 张里 7 张）—— 与 `R` 报告 §5 的「每副 30 张丢 7 张」逐字吻合。
- **回放的 `actions[].pickIds` / `defId`**：4 份回放里 `pickIds` **0 条**、`defId` **1 条且是真 id**
  ⇒ **没有**失效 token。
- **没有第 5 个失效 id**。`ASH_…` 形态之外的旧池 id（`poolTrueIds` 那 114 条）**0 处** —— 与 `K06` §五 的扫描一致。
  本次之所以能扫到 `K06` 漏掉的那 4 个：`K06` 只比了「旧 id 集合 ∩ 现 id」，**没扫自造形态**
  （`<前缀>_<卡名 slug>`，与真 id `ASH83` 长得完全不一样）。

### 映射为什么不是「猜」

- 旧 token 的**生成规则是确定的**（`make_id`：非字母数字 → `_`，首尾 `_` 去掉；`SaimHann` 的前缀 = `ASH`），
  逐字可复算：`make_id("SaimHann","Autarch") = "ASH_Autarch"`。
- 真 id 侧：`card_ids.json.mapping`（真 id → 原版资产名）里这四个名字在 **SaimHann 阵营内各只有一张**，
  且四个目标 id 都在现卡池 `cards_engine.json` 里逐条反查存在（`ASH83` unit cost6 · `ASH77` unit cost1 ·
  `ASH80` unit cost3 · `ASH79` unit cost4）。
- ⛔ **运行时没有做任何「按卡名匹配」兜底** —— 表是写死的。

---

## 2. 改动清单

> ⚠️ 任务白名单里写的 `CardPresentation/Battle/DeckBuilder.cs` / `CardPresentation/Deck/DeckStore.cs`
> **这两个路径不存在**：真正的 `DeckBuilder` / `DeckStore` / `DeckLibrary` 都在 **`RuleEngine/Data/`**。
> 按「或同类读档入口」执行；**只碰了这 4 个数据层文件 + 1 个新建**，未碰任何 `RuleEngine/Core`、
> 未碰 `Editor/**`、未碰 `Scenes/`、未碰玩家存档/录像文件、未动 git。

| 文件:行号 | 改动 |
|---|---|
| **`RuleEngine/Data/LegacyCardIds.cs`**（**新建**，175 行） | 静态映射表（4 条）+ 读入口用的四个口：`Migrate(id)`（无日志）· `Migrate(id, where)`（单条出声）· `MigrateDeck(PlayerDeck, where)`（整副牌 + 一行汇总）· `MigrateArray(string[], where)`（`pickIds`）· `MigrateDeckJson(string, where)`（`JsonUtility` 化的 `PlayerDeck`；不含旧 token 时**原样返回**）。表头写清了每条的判据与扫描范围。 |
| `RuleEngine/Data/DeckStore.cs:190-198` | `LoadAll` 的归一化循环里，`CardIds` 补齐之后调 `LegacyCardIds.MigrateDeck(d, "存档「"+d.Name+"」")`。**这是玩家卡组的唯一读档口**（卡组列表 / 编辑 / 开局 `PickSavedDeck` 全经 `DeckLibrary.Load` → 这里）。 |
| `RuleEngine/Data/DeckBuilder.cs:249-256` | `FromDeck` 拿到 `deck` 之后、查卡池之前再兜一次 `MigrateDeck`。**最后一道闸**：`PlayerDeck` 还能从联机对端下发的 `hostDeckJson` / 导入串 / 自检夹具构造出来，而下面那一圈遇到查不到的 id **只 `LogError` 后 `continue`**（静默丢牌）。 |
| `CardPresentation/Battle/ReplayStore.cs:482-491` + `:497-523` | `Load` 里版本校验通过、`actions` 补齐后调新方法 `MigrateLegacyIds(rec, fileName)`：迁 `start.hostDeckJson` / `start.clientDeckJson`（整副牌）、`actions[].pickIds`（数组）、`actions[].defId`（单条）。**只改内存里的 `ReplayRecord`，录像文件不写**。 |
| `RuleEngine/Data/DeckLibrary.cs:414-418` | `ImportString`（原版格式卡组串的导入）里，`lookup` 之前把每个 token 过一遍 `Migrate`。不迁的话查不到会被当成「卡池没这张」**悄悄丢掉**（只记 `LastDroppedIds`）。 |

**类型检查**：`TMPDIR=/tmp/wf_f4 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（原文见 §5）。

---

## 3. 迁移后的行为

### 3.1 日志长什么样

**命中才出声；干净的存档/录像一行都不打。**

读存档（`DeckStore.LoadAll`，整副汇总成**一行**）：

```
[LegacyCardIds] 旧 id 迁移（存档「复仇者之刃」）：`ASH_Autarch` → `ASH83` ×2 · `ASH_Death_Spinner_Warp_Spider` → `ASH77` ×2 · `ASH_Farseer` → `ASH80` ×2 · `ASH_Orian_Laratharjos` → `ASH79` ×1 —— 共 7 处（迁移自存档/回放；⛔ 只改内存，文件不动）
```

读回放（每个卡组串一行）：

```
[LegacyCardIds] 旧 id 迁移（回放 `20261011_134736_安维尔_凯尔托克_vs_战头目戈德朗.json` · hostDeckJson）：…同上 4 条… —— 共 7 处（迁移自存档/回放；⛔ 只改内存，文件不动）
```

单条（`defId` / 导入串）：

```
[LegacyCardIds] 旧 id 迁移（回放 `X.json` · actions[12].defId）：`ASH_XXX` → `ASH77`（迁移自存档/回放；⛔ 只改内存，文件不动）
```

键按字典序排（日志稳定、方便比对），同一个 token 多次出现合并成 `×N`（**不会**一次读档刷 7 行）。
日志前缀唯一：`grep LegacyCardIds` 可定位。

### 3.2 幂等怎么保证

- 映射表的 **key 全是自造形态**（`ASH_xxx`），而**现卡池的 id 全是 `<前缀><数字>`** ⇒ 真 id **不可能**命中 key
  ⇒ 迁第二次时 `TryGetValue` 落空、**一个字段都不改**（Python 侧按 C# 同一张表模拟：二次迁移逐字段相同，见 §4）。
- **不写盘**：迁移只改内存里的 `PlayerDeck` / `ReplayRecord`；同一份存档/录像反复打开结果完全一致。
- 玩家下一次正常保存卡组时，迁移后的真 id 会经既存的 `DeckStore.SaveAll` 自然写回（**那是存档自己的路，不是这里改盘**）。
- `MigrateDeckJson` 先走 `ContainsAny` 短路：**干净的 JSON 连一次解析/重序列化都不做**（原字符串原样返回）。

### 3.3 对「丢牌 / 回放不可复现」的直接影响

- 卡组：「复仇者之刃」30 张里那 7 张现在解析得出 ⇒ `DeckBuilder.FromDeck` 不再进 `LogError` 那一支，
  牌数从 **23+督军** 回到 **30+督军**。
- 回放：`finalHash` 是按**满 30 张**那个局面录的 ⇒ 迁移后重建出来的开局牌库才和录制时一致
  （否则必报「不是同一局」）。**是否真能逐条对上还要等一次回放验收**（本笔按铁律 12 没跑）。

---

## 4. 只读验证（按 C# 源码里的表模拟，未改数据）

脚本：`d:/4/_tmp_view/f4/scan_legacy_ids.py`（扫描）+ `d:/4/_tmp_view/f4/verify_migration.py`
（**直接从 `LegacyCardIds.cs` 正则抠出映射表**来跑，避免「另抄一份表」的自证）。

```
=== 存档（迁移前 → 迁移后）===
  迁移前 复仇者之刃   牌数 30 | 失效 7   <<< CardIds[0,1]=ASH_Autarch · [2,3]=ASH_Death_Spinner_Warp_Spider
                                          · [8,9]=ASH_Farseer · [18]=ASH_Orian_Laratharjos
  迁移后 复仇者之刃   牌数 30 | 失效 0
     幂等（二次迁移逐字段相同）: True
  迁移前 新卡组       牌数  0 | 失效 0
  迁移后 新卡组       牌数  0 | 失效 0
  （第二份存档：同一副本，结果同上）
=== 回放 ===
  20261010_225437_…  hostDeckJson <empty> · clientDeckJson <empty> · pickIds 0 · defId 0
  20261011_134736_…  迁移前 hostDeckJson 牌数 30 | 失效 7 → 迁移后 失效 0 · pickIds 0 · defId 1（真 id）
  20261013_215704_…  迁移前 hostDeckJson 牌数 30 | 失效 7 → 迁移后 失效 0 · pickIds 0 · defId 0
  20261015_142708_…  迁移前 hostDeckJson 牌数 30 | 失效 7 → 迁移后 失效 0 · pickIds 0 · defId 0
✅ 迁移后 0 处失效；卡组张数不变；二次迁移逐字段相同（幂等）；回放 pickIds/defId 无未知 token。
```

---

## 5. typecheck 原文

命令：`TMPDIR=/tmp/wf_f4 bash d:/4/Unity/工具/typecheck.sh`

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

---

## 6. 没查清 / 未覆盖（⛔ 不猜）

1. **回放迁移后是否真能逐局复现，没验**（本笔不跑 `*.Run`、不进游戏）。迁移只保证「开局两副牌与录制时一致」；
   引擎从 10-11 到 10-18 之间的其它改动仍可能让 `trace`/`finalHash` 分叉。**验收办法**：`BattleDriver.PlayReplay`
   放那 3 份回放，看末行「终局指纹 = 录制时的 ⇒ 同一局」。
2. **旧 id 空间远不止这 4 个**：全池 1,127 张**每一张**都曾可能以 `make_id(阵营, 卡名)` 形态出现过。
   本轮只把「本机真实数据里出现过、且逐条确认过」的 4 条写进表（任务口径：只放已确认的映射）。
   以后若扫出新 token，**先核 `make_id` 形态 + `mapping` 里同阵营同名唯一**，再往表里加。
3. **`poolTrueIds` 里那 114 条旧池 id 的「旧号撞真号」那一族**（旧 `SW4` 是 High King of Fenris、现 `SW4` 是 Blood Claw）
   **不能按 token 单独判** —— 必须连同卡名一起核。本机存档/回放里 **0 处**命中，本轮未处理；
   若将来出现，需要一个「带卡名的迁移」，不是这张表能覆盖的。
4. **`WarpforgeDaily/daily.json`**：扫过，**没有卡 id**，未加迁移（加了也是空转）。
5. **联机对端下发的卡组**（`MsgStart.hostDeckJson` 走网络、不经 `ReplayStore`）：由 `DeckBuilder.FromDeck`
   那道最后闸兜住（同进程内已迁）；但**对端如果发来我们不认识的第三种 token**，仍然只会在那一圈 `LogError` 丢牌
   —— 与本次改动无关，如实记在这里。
6. **`RuleEngine/Editor/*` 的断言没加、也没跑**（白名单外、且本笔按铁律 12 不做验证）。若要钉「幂等 / 7→0」，
   落点应是 `RuleEngineTest`/`DeckScene` 的脚本/存档断言那一节 —— **留给主对话排**。

---

## 7. 越界 / 并行情况（如实报）

- 开工时 `git status` 里 `BattleDriver.cs` / `PopNumber.cs` 在跑本笔期间被**别的代理**改成 `M`
  （本笔**没碰**这两个文件，也没碰 `Label.cs` / `TmpFont.cs` / `Editor/**` / `Shell/**`）。
- 本笔只写了：`RuleEngine/Data/LegacyCardIds.cs`（新）· `DeckStore.cs` · `DeckBuilder.cs` · `DeckLibrary.cs` ·
  `CardPresentation/Battle/ReplayStore.cs`。`git diff --numstat` = **9/0 · 9/0 · 5/1 · 37/0**（没有整篇行尾翻转）。
- 新建的 `LegacyCardIds.cs` 已按本仓 `.cs` 惯例写成 **CRLF**（176 行 / 176 CRLF）；
  `ReplayStore.cs` 在 HEAD 里本来就是 **LF**（518 行 / 0 CRLF），本笔没有翻它的行尾
  （判据：`git show HEAD:<path>` 的字节计数）。
