# C · chip 行落地（照抄溢出）+ `CardServices` 判 A 的措辞 · 第 1 轮

> 2026-10-25 · 执行代理 C。裁定的两条都落了。
> `TMPDIR=/tmp/wf_c bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。⛔ 未跑 Unity / 未动 git。

---

## ① chip 行 —— **已按 (b) 网格算术实现，溢出照抄**

### ①·1 做了什么（`Core/FilterPanelModel.cs`，本轮最后一次改它）
1. **chip = 同一批 cell**：新增 `AppendChips(...)` —— 把一行里**已选**的那几格**原样再复制一份**（`Cell` 是 struct ⇒ 值拷贝，`R`/`Bg`/`Lab` 按格位位移），
   key = **`$chip:` + 原 key**，`On = true`。**长得跟选项格一模一样**（原版 `FillToggleList` 派生的就是**同一个** `CollectionFilterToggle` prefab）。
2. **四条行各按 prefab 的参数排**：
   - **Army**：`cell 100` · `spX 7` · `pad 14` · **3/行** —— 且 **`ComputeLayout(facs.Count + armySel)`**（已选几个就多几个槽）
     ⇒ **行会撑高**（原版 `Army Filter/Content` 挂 `ContentSizeFitter(MinSize)`）。
   - **Rarity**：`cell 100` · `spX = raritySpX`（两窗 5/7）· `pad 14` · 3/行 —— **`L.RarityTop` 不动** ⇒ chip 落在行框外。
   - **Cost**：`cell 65` · `spX 15` · `spY 20` · `pad 15` · **4/行** —— 8 档已占 2 行 ⇒ chip 从**第 3 行**起 ⇒ **直接压到 Type 标题上**。
   - **Type**：`cell 80×100` · `spX 0` · `pad 15` · 3/行（HLG）—— chip 从第 4 格起（横向溢出）。
   ⚠️ **Rarity / Cost / Type 三行没有 CSF ⇒ 溢出是照抄的**，注释里写死了这条事实与判据路径（`CardsTab ▸ … ▸ {行}/Content` 的组件表）。
3. **点 chip = 取消这一项**（原版 `ForceOff()`）：`Click` 开头把 `$chip:` 前缀**剥掉**、走**同一条** toggle 路径（那一项当前是选中的 ⇒ toggle 会删掉它）—— 一份逻辑两处复用。

### ①·2 断言（`DeckScene.cs` 新一节，2 + 1 条）
- **Army 行长高**：多选 4 个阵营 ⇒ 槽位 13+4 = 17 ⇒ 行数 5→6 ⇒ 下面三行整体下移 **`ArmyCell = 100px`**（`ComputeLayout` 两跑相减）。🧨 把 `armySel` 从 `ComputeLayout` 实参里去掉 ⇒ 红。
- **费用 chip 溢出照算**：两档 ⇒ chip 落在 `y ≥ CostTop + CostContentTop + 165`（**行框之外**），且压在 Type 那一行上。🧨 把它「按 CSF 撑高」修好 ⇒ 红。
- **前提**：费用行**真的生出 chip 格**（`$chip:$cost:` 存在）。

### ①·3 📌 **`Core/FilterPanelModel.cs` 归还 E**
chip 行做完 ⇒ 本轮起**我不再改这个文件**。本批对它累计的改动（全加法）：四处格子 `On` + `Click` 三支改 toggle 集合 + `$cost:` 支改集合 + `$chip:` 剥离 + `ToggleMember` + `AppendChips`/`Shift` + `using System;` + 一处 `RuleEngine.` 限定。
⛔ 其它 `Core/**` 一行没碰。

---

## ② `CardServices.CraftCard` / `UpgradeCard` —— 按裁定**判 A（含如实差异）**

### ②·1 但要落到**E 的账**（`账/卡牌面_渲染.tsv`），我无权写
那两行在 E 的账里（代理 C 不碰别人的 TSV）⇒ **改动请求（照抄即可）**：

| 列 | 写法 |
|---|---|
| **状态** | `A` |
| **差异** | 「原版这两个方法**没有本地业务错误分支** —— 判定（通配符 / 货币 / 上限）**全在 CloudScript 服务端**（`CraftCard` → 函数 **900** · `UpgradeCard` → **901**）；本地只有两条「不走网络」的：**#2 库存里没这条 item ⇒ 静默 `return`**（`CardServices__CraftCard.c:52-53`）· **#5 原卡为 null ⇒ 落到不返回的 helper（崩/断言）**（`CardServices__UpgradeCard.c:28-32`）。**我们都不照抄**：本地实现里照抄那两条只会让玩家看到更差的行为 —— 我们把服务端的判定在本地**模拟了一层**（更严、且出声）」 |
| **备注** | 「2026-10-25 由 C 复核：E 原记「错误分支比原版简」的**真实内容**是「我们把服务端判定在本地模拟了一层」，**不是少了分支** ⇒ 判 A。⛔ 别去补更多错误分支。」 |

### ②·2 判据（我逐行读的）
`decomp_full/CardServices__CraftCard.c`（68 行）· `CardServices__UpgradeCard.c`（43 行）—— 两条方法体的**全部分支只有上表那 6 行**（含入口与网络调用）。

---

## ③ 没查清 / 下一步
1. **chip 的可见长相**：我们复制的是「选项格」（含图标/文字/底图），与原版「同一个 toggle prefab」**结构一致**；但**点完之后的观感（chip 与选项格并排重复）**只能等**并排比**（自检跑不了 UI 观感）。
2. **Type 行的 chip 横向溢出**（第 4 格起超出 335 宽）—— 按网格算术算出来了，但**没有单独断言**（费用那条已经代表「溢出照抄」这一族；Type 只是同一机制的另一种算子）。
3. 之后继续补 C：收藏 **68** · 卡组编辑 **18** · 商店 **52**（其中活动线 36 条只记账）。

---

## ④ 顺手
1. **chip = 复制同一个 prefab** 这件事把「多选 UI」的成本压到很低 —— 不需要新的视觉件，只要**同一份网格算术**多算几个格位。这也是原版的做法（`Instantiate(option.toggle, parent)`）。
2. **`ComputeLayout(facs.Count + armySel)`** 是「CSF 会长高」这件事的最小等价：把 chip 当**额外的 cell** 喂给行高公式，下面三行自然下移 —— ⛔ 不需要真的引入 `ContentSizeFitter`。
3. **`$chip:` 前缀剥掉再走同一条 toggle** = `ForceOff` 的等价物，且保证「一份逻辑两处用」。
