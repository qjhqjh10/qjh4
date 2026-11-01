# B116_W35 · 账 `B-319`：`CardDef.Prefixes` 补 `transient`

**结论：补了。** `Prefixes` 加一条；`Implemented` **⛔ 不加**（徽标判据见 §二）。机制那一半原本就在（`RuleCore.cs:2312`），补的是「`keywords` 列进得来」这一层。

## 一、两表各改了没有

| 表 | 位置 | 改了？ | 说明 |
|---|---|---|---|
| `KeywordTable.Prefixes` | `CardDef.cs:8686`（同物理行） | ✅ 加了 `new[] { "transient", "transient" }` | 行数 **9526 → 9526（守恒）** |
| `KeywordTable.Implemented` | `CardDef.cs:7963` 起 | ⛔ **没加** | 见 §二 |

- 写法：**同一物理行追加**（紧跟 `massive` 那条，两者同为「隐藏 trait、只进 `Prefixes`」）。判据注释用 `/* … */` 内联块，**落在代码之后、第一个 `//` 之前**（避免 B102_R1 那种「代码落进行末 `//`」）。
- `CardDef.cs` 行尾未变（LF-only，`CRLF=0`，裸 CR=0）。
- 判据（写进注释）：`CardScript__OnTurnStart.c:255-261` —— `cardState ∈ {2,0xf,3,0x11}` ∧ `HasCurrentTrait(0x1c7)` ⇒ `DestroyUnit(…, 0x14)`。

## 二、徽标判据：`trait_collection.json` **没有** `transient` 的 SO

现读 `d:/4/Unity/数据/游戏数据/trait_collection.json`（`TraitsCollection`，**77 条** trait 项）：
`transient` **不在** `traits[].name` 里；对照 `massive` / `coward` / `precognition` / `requiem` / `jam` **也都不在**（`massive` 正是同一条判据下的先例）。

⇒ 按施工单口径：**没有 SO ⇒ ⛔ 不进 `Implemented`**。进那一栏会让 `BattleDriver` 的外观过滤多印一枚原版没有的徽标。

> 顺带钉住的另一条事实：`cardTraits` 列**本来就可达**（`TraitKeyOf` 在 `Normalize` 返回 null 时回退「小写净名」），所以本笔修的是 `keywords` 列那一半。

## 三、断言（共 **7** 条 · 1 个新入口）

新入口 `CheckB116W35TransientParses()`，落在既有 `RuleEngine/Editor/RuleEngineTest_B115S7.cs`（该文件 104 → 173 行，**账不按行号引用它**，已核 0 处）。

| # | 断言 | 灭自证作用 |
|---|---|---|
| A1 | `KeywordTable.Normalize("transient") == "transient"` | 删掉 `Prefixes` 那条 ⇒ 红 |
| A2/A3 | 两张夹具部署成功 / 回合开始前都在场 | 前提写成**硬 Check**（不用 `if(x!=null) Check(…)`） |
| B1 | `keywords="transient"` 的卡 `CardDef.Has` 为真 | 不被 `NoteDropped` 丢弃 |
| B2 | 不写这个词的卡 `Has` 为**假** | 🧨 挡「`Has` 恒真」 |
| B3 | 场上那张 `UnitState.Has("transient")` 也为真 | 挡「只登记不灌进单位」 |
| B4 | `BeginTurn` 后离场 | 端到端：机制从卡数据**可达**（不是死代码） |
| B5 | 同批对照卡**仍在场** | 🧨 挡「无条件清场」 |

接线：`RuleEngineTest.cs:1288` 的 `{ … }` 内**同物理行**追加 `Guard("CheckB116W35TransientParses", …)` —— 已验证落在第一个 `//` **之前**（该行 `//` 在字符 158，`Guard` 在 121 附近），行数 39738 未变、CRLF 未翻。

⚠️ **未执行**：本轮红线 **⛔ 不跑 Unity** ⇒ `RuleEngineTest.Run` 没跑过，7 条断言**只做到「接线 + 编译过」**。请在下一个 Unity 同步点跑一次，确认日志里印出 `--- 🆕 B116_W35 · transient 从 keywords 列进得来 ---`（CLAUDE.md：唯一硬证据是宿主把它印出来）。

## 四、各闸读数

| 闸 | 读数 |
|---|---|
| `工具/typecheck.sh` | **运行时 0 / 编辑器 0** ✅（首次跑时 `B116W31_Assert.cs` 有 2 条别人的在飞错，等 20s 复跑即 0/0） |
| `工具/check_ledger_points.py` | **A 5831 · 通过 5831 · 漂移 0 · 失败 0** ✅ |
| `工具/ruleprobe.sh play check` | **rc=1**（见下）· 现算指纹 `3decaacbc96b524d04e4b41b9e8fd9e8`，基线 `a8a2a2184aa970d543f11c86e3c7c5ce` |

**rc=1 与本笔无关（已用 A/B 证明）**：把 `transient` 那条**临时撤掉**再跑一次，指纹、4 张差异卡（`UM110` / `UM111` / `UM118` / `8482077f…`）与差异段**逐字节相同**。差异内容是能量 / 手牌总价 / 落场，来自工作树里其他在飞写手（`git status` 显示数十个引擎文件已改），⛔ 不是本笔引入。

## 五、顺手发现（未动，交调度台分派）

1. `RuleCore.cs:2312` 行尾注释仍写「`keywords` 列的真值需在 `CardDef.Prefixes` 登记 `transient`（不在本笔白名单）**——今天走 `cardTraits` 列即可达**」⇒ **已过期**（本笔已登记）。不在我的白名单（只许改 `CardDef.cs` + 断言），请派给 `RuleCore.cs` 的写手按铁律 5 就地订正。
2. `CardDef.cs:8655-8660` 那张「为什么不补另外 45 个」的名单里仍列着 `transient`（`massive` 也一样，`B116_W23` 同样没划）—— 施工单明确 **⛔ 不动那份名单**，按要求保留。
3. `RuleEngineTest_B115S7.cs:74` 的旧注释「`keywords` 列的真值今天进不来（`Prefixes` 未登记，账 `B-319`）」**已就地改写**为本笔后的口径（铁律 5）。
