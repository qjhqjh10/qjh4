# B124-W1 · `DeckScene` 2 红（α 1 · δ 1）· 2026-11-03

> 直属调度台。来源 = `判据/B124_ASSERTDEBT_3_CollectionDeck.md` §B1 / §B2（⛔ 未重新分诊，直接照做）。
> 独占文件 = `CardPresentation/Editor/DeckScene.cs`（本次**只动了它**）。

## 〇 结论

| 任务 | 类的 | 结果 |
|---|---|---|
| 1 · α 四条恒不可能绿的断言 | B1 | ✅ **已改**：`DeckScene.cs:2077-2080` → 4 行等行数注释（行号、行数全不动） |
| 2 · δ `B121WC C1` 夹具补到 30 张 | B2 | ✅ **已改**（补派后白名单已扩到该文件）：`Editor/B121_WC_Assert.cs:131` → 5 行，净 **+2**（文件 218 → 220），补满到 `DeckRules.CardCount(s.Skirmish)` 张并保留 `notDeckable` 那张；断言正文未动。 |
| 3 · 过期 🧨 注释订正（补派） | — | ✅ **已改**：`DeckScene.cs:2058-2059` 等行数改写，指向现存真判据 `:2074` / `:2085` |

## 一 任务 1（已完成）

**文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs`
**改动**：`:2077-2080`（4 行）原地改写成 4 行 `//` 注释。`:2076` 的 `var LNo = …`（下游 `:2085` 在用）**保留不动**；`:2081-2087` 的真判别式**保留不动**。

改写前（4 行）：
- `:2077` `var LSel = FilterPanelModel.ComputeLayout(nFac + 4);`
- `:2078-2080` `CheckNear(LSel.RarityTop - LNo.RarityTop, 0f, 0.001f, "（判据）多选 4 个 ⇒ 若还按旧写法把 chip 算进格数，Rarity 行顶会往下挪 100px —— 现在必须**一动不动**");`

改写后（4 行注释，逐字）：
- `:2077` `// ⛔ 2026-11-03（B124-W1）· 这里原有 LSel + 三条断言，它们永远不可能绿，已删成这 4 行等行数注释（保行号、不动下游账落点）。`
- `:2078` `//   原因：ComputeLayout(nFac + 4) 的语义就是「多 4 个阵营格」（FilterPanelModel.cs:65 rows=ceil(n/3) · :82 RarityTop 随 rows 走，ArmyCell = 100）`
- `:2079` `//   ⇒ 13 → 17 必然跨一行，RarityTop 差值恒 +100 —— 与 chip 选没选毫无关系，写成什么样都必红。`
- `:2080` `//   真正的判别式在下一段（拿真 Build 输出的 $rar:common y 比 LNo.RarityTop + RarityContentTop）—— 它✓，够了。`

**判据**（已写进注释）：`Core/FilterPanelModel.cs:65`（`rows = ceil(n/3)`）+ `:82`（`RarityTop = RowArmy + ArmyRowH(cells)`）+ `ArmyCell = 100`（同文件）。

**怎么验的（不靠肉眼）**：
- 改动前先把原文件逐字节备份，改完逐行比：**变化的行恰好是 2077/2078/2079/2080 这 4 行**，其余 10,482 行逐字节相同。
- 行数 **10,486 → 10,486**（零变化）；`\r\n` 计数 10,486、裸 `\r` = **0**（行尾没翻：全程二进制读 + `\r\n` 切/拼，⛔ 没用 `sed -i` / 文本模式写）。
- 该文件在账里的落点是 `1401 / 2048 / 2281 / 2340 / 2384 / 2394 / 2712 / 4739 / 756 / 772`，**没有一个落在 2077-2080**（也不在 2080 之后被前移）—— 但因为是等行数改写，下游整体不受影响。

## 二 任务 2（补派后已落地）

**过程**：第一次交回时按白名单（只 `DeckScene.cs`）停手、只交补丁；调度台随即把 `CardPresentation/Editor/B121_WC_Assert.cs` 扩进我的白名单（所有权表已更新，本波只有我写它）⇒ **本次已按那段补丁落地**。

### 已落地的改动（`Editor/B121_WC_Assert.cs:131`，`C_ValidateGate`）

OLD（1 行）：`        s.Deck.CardIds.Add(UnitA);`

NEW（5 行，净 **+2**，从 `:131` 起）：
```
        // 2026-11-03（B124-W1 · δ）：原来只放 1 张 ⇒ `DeckRules.Validate` 在 ④ 张数档（`DeckRules.cs:486-488`）就回 `TooFewCards`，走不到 ⑥b（`:502` 的 `IsNotDeckable`）。
        //   补满到合法张数（经典 30）：⑥b 在 ⑦ 同名上限**之前**返回 ⇒ 30 张同名不会先撞 `CopyLimitExceeded`；要测的那张 `notDeckable` 卡仍在。
        for (int i = 0; i < DeckRules.CardCount(s.Skirmish); i++) s.Deck.CardIds.Add(UnitA);
```
（上一行 `s.Deck.WarlordId = HeroA;`、下一行 `u.AttachNotDeckable(true);` 均原样保留 —— 断言正文 `:133-135` 一个字没动，**没放宽断言去迁就夹具**。）

**行数**：218 → **220**（+2，从 `:131` 起，其下整体 +2）。该文件是**纯 LF**（`\r` 计数 0），已按 LF 切/拼写回，**没翻行尾**。
**账**：`grep "B121_WC_Assert\.cs:" 账/*.tsv` = **0 命中** ⇒ 这 2 行不产生账漂移。

### 为什么这条能走到 ⑥b（逐闸自证，照 `DeckRules.cs:459-527` 现读）

| 闸 | 结果 |
|---|---|
| ① lookup | 30 张 `UnitA` 都在池里（`Fresh()` 的 pool 有 `Unit(UnitA, FacA)`） |
| ② 督军 | `WarlordId = HeroA`，`Type == "hero"` ✅ |
| ③ 防御卡 | `null` ⇒ 合法（可带可不带，`:473-483`） |
| ④ 张数 | `want = DeckRules.CardCount(false)` = `GameplayVariables.For(Classic).deckSize` = **30**（`:163-164`）；`cards.Count == 30` ⇒ 不 `TooMany` 不 `TooFew` ✅ |
| ⑤ 阵营 | `deckArmy` = `HeroA.Faction` = `SpaceWolves`；每张 `UnitA.Faction` 同值 ✅ |
| ⑥ 督军/防御混入 | 全是 `"unit"` ✅ |
| **⑥b** | `u.AttachNotDeckable(true)` ⇒ `IsNotDeckable` 真 ⇒ **`InvalidCard`**（`:501-502`，且 `NotDeckable` 排在 `CannotAddToDeck` 之前） |
| ⑦ 同名上限 | **到不了**（⑥b 已返回） |

- 用 `DeckRules.CardCount(s.Skirmish)` 而不是写死 `30`：`Skirmish` 是 `DeckEditorState` 的公开属性（`Deck/DeckEditorState.cs:249`），经典档 = 30。若要绝对保险也可写成字面 `30`。
- **行号影响**：`B121_WC_Assert.cs` 在账里**零落点**（`grep "B121_WC_Assert\.cs:" 账/*.tsv` = 0 命中）⇒ 加这 2 行**不产生账漂移**；文件内下游行号 +2（该文件内无「按行号引用」的自检）。
- 断言本身（`:133-135`）**一个字不动** —— 不放宽、不改文案（文案已经会打印实读值）。

## 三 typecheck

```
TMPDIR=/tmp/wf_b124w1 bash d:/4/Unity/工具/typecheck.sh
运行时错误数: 0
编辑器错误数: 0
```
（注释掉 4 行断言后 `LSel` 无残留引用 ⇒ 无 `CS0219`/`CS0168` 类警告级错误；`LNo` 仍被 `:2085` 使用。）

补派两件落地后**复跑**：`TMPDIR=/tmp/wf_b124w1b bash d:/4/Unity/工具/typecheck.sh` ⇒ 运行时 **0** · 编辑器 **0**。

## 四 顺手发现（未动手，交调度台分流）

1. ~~**`DeckScene.cs:2058-2059` 的 🧨 注释已过期**~~ ⇒ ✅ **补派时已订正**（见 §六·2），此处保留作留痕。
2. **`B121_WC_Assert.cs:129`** `if (u == null) { Check(false, "C0…"); return; }` 是正确写法（显式前提断言）—— 与本批纪律一致，**无需改**，仅登记。
3. **分诊报告 §B2 末段的「原版先逐卡判、后数张数」**（`DeckUtility__ValidateDeck.c:44-56`）与我们 `DeckRules.Validate` 的 ④→⑥b 次序不一致 —— 只影响错误码优先级。本补丁**顺着现状**（先补满张数）绕开它，不改实现；若要按原版调序，属另一笔（会动一批错误码期望）。

## 五 账

本次**不动账主表**、不写 `_parts` 补丁（无落点变化，等行数改写 ⇒ 零漂移；`B121_WC_Assert.cs` 本身零落点）。

## 六 补派两件（2026-11-03 · 白名单扩到含 `B121_WC_Assert.cs` 后）

### 1 · δ 夹具落地 = §二（`Editor/B121_WC_Assert.cs:131`）
- 行数 **218 → 220（+2，从 `:131` 起）**；纯 LF 文件按 LF 写回（`\r` 计数 0）。
- 逐行比对备份：变化的**只有** `:131` 那一处（1 行 → 3 行），其余逐字节相同。
- `CardIds` 直接 `for (… i < DeckRules.CardCount(s.Skirmish) …) Add(UnitA)` → 经典档 30 张；未引入 `wantCards` 局部变量（把声明并进 `for` 条件，保住 +2 而不是 +3）。

### 2 · 过期 🧨 注释订正（`Editor/DeckScene.cs:2058-2059`）· **等行数**
- 改写前：`① Build 里把任一 AppendChips(...) 写回来 ⇒ 下面第一条红；② 把 ComputeLayout(facs.Count) 改回 facs.Count + armySel ⇒ 第二条红。`
- 改写后（2 行，行数不变）：
  - `:2058` `🧨 改坏法：① 在 FilterPanelModel.Build 里把 $chip: 格写回抽屉行（AppendChips）⇒ :2074 红（行内必须 0 颗）；`
  - `:2059` `② 让 Build 传给 ComputeLayout 的格数把 chip 算进去 ⇒ :2085 红（稀有度格 y 必须与 LNo 逐位相同）。〔原 ①② 旧写法「比 ComputeLayout(nFac+4) 行顶」已于 2026-11-03（B124-W1）删除 —— 恒不可能绿〕`
- 行数 **10,486 → 10,486（0）**；CRLF 计数 10,486、裸 `\r` = 0；逐行比对：变化的**只有 2058/2059 两行**。
- 指向的两条现存真判据：`:2074`（`chipInRow == 0`）· `:2085`（稀有度格 y 与 `LNo.RarityTop + RarityContentTop` 逐位相同）。

### 3 · 复跑 typecheck
`TMPDIR=/tmp/wf_b124w1b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

### 4 · 汇总（两件合计）
| 文件 | 行数变化 | 账漂移 |
|---|---|---|
| `Editor/DeckScene.cs` | 0（2077-2080 注释化 · 2058-2059 订正，两处均等行数） | 无 |
| `Editor/B121_WC_Assert.cs` | +2（`:131` 起，218 → 220） | 无（该文件账落点 0） |
