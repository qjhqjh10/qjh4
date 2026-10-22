# G1 · `MainMenuScene.Run` 17 红诊断（只读，2026-10-22）

> 现场：`_tmp_view/w65c_menu.log`（17 红）· 同日上午基线 `_tmp_view/w65b_menu.log`（3716 / 0）。
> 期间唯一改动 = 练习窗左列数据源换成原版 78 对 `DemoDeckInfoSO`（`demo_deck_pairs.json`）。
> **结论：17 条里真缺陷 0 条**（α 10 · γ 2 · δ 5），全在夹具/期望值侧。

## 结论表

| # | 条数 | 类 | 一句话成因 | 判据（文件:行号） | 建议改法 |
|---|---|---|---|---|---|
| 1 | ×10 | α | 软边断言只登记上内沿 285.64；78 对 ⇒ 39 行后**下内沿 780.43（= 803.43 − 23）确实被切到** ⇒ 切线 #7..#16 全红。实现正确 | `Editor/MainMenuScene.cs:3198`（`want = new[]{285.64f}`）+ `:3177-3179` 旧注释；`CheckSoftCuts` `:16746-16762` | `want` 改 `{285.64f, 780.43f}`，文案改「39 行 ⇒ 下带够得到」 |
| 2 | ×2 | γ | 级联假红：#6 点开且**从未关闭**的 `DeckSelectionPopup` 在 ds4 关闭后被 `ShowPreviousWindow` 提回前台，其压暗层 **3125 > 练习窗吸收层 3107** ⇒ 候选点全被它吃掉 | `MainMenuScene.cs:4652-4658` → `Shell/MenuDraw.cs:2846-2850`；遗留窗来源 `Shell/DeckInfoPopup.cs:2286` | 修 #6 夹具并在该点击后关掉挑对手窗；吸收层/`MenuDraw` 本身不用改 |
| 3 | ×1 | δ | Deck info 已是预组窗：`OverrideDeck≠null` ⇒ `Edit Deck` 恒藏；`IsPlayerDeck=false` ⇒ 4 圆钮恒藏 ⇒「state 改回 0」复合断言不可能成立 | `Shell/DeckInfoPopup.cs:698,704-712`；输入源 `Shell/PracticeModePopup.cs:808-817` | 两态对照改用**玩家卡组窗**（`DeckInfoPopup.Create(Manager, 0, …)`） |
| 4 | ×1 | δ | 练习开战改走预组支、不再执行 `CollectionData.Select(0)`（旧夹具靠它把当前套变成 0）⇒ `CurrentIndex` 停在 3；`FirstDeckInMode` 无 13 模式卡组时回落 `prefer=3` | `Shell/LiveOpsEventWindow.cs:384,274-284`；`Shell/PracticeModePopup.cs:2459-2478`；断言 `MainMenuScene.cs:6780` | 本节前显式 `CollectionData.Select(0)`，或期望值改从 `sk.DeckIndex` 取 |
| 5 | ×1 | δ | 详情窗 `CurDeck` = 第 1 对当前模式那副（`UM_SK_1_2` · 12 张 · 合法）⇒ `Validate=None` ⇒ `interactable=true`；「第 0 套 4 张」已不喂这扇窗 | `DeckInfoPopup.cs:907-910,951-952`；断言 `MainMenuScene.cs:3809-3811` | 阴性对照改用 4 张卡的**玩家卡组窗**（照 `:3828` 阳性对照的做法） |
| 6 | ×1 | δ | 同上 ⇒ 钮真可点，走真路径开出挑对手窗（窗仍开），「什么都没发生」为假 | `DeckInfoPopup.cs:2253,2286`；断言 `MainMenuScene.cs:3817-3820` | 同 #5；若刻意走真路径则断言「开了挑对手窗」并收尾关窗 |
| 7 | ×1 | δ | 预组档 `IsPlayerDeck=false` ⇒ `mine && state∈{0,2}` 为假，`Practice Deck` 按判据藏（= 原版 `HasItem(预组)=false`） | `DeckInfoPopup.cs:704,712`；`PracticeModePopup.cs:817`；断言 `MainMenuScene.cs:3780-3781` | 可见性断言改用玩家卡组窗 |

## 关键日志原文

- `16643/16749 ✓`「切线实测 **285.64×6、780.43×10**」；`16755..16899 ✗`「切线 #7..#16 在 780.43 ⇒ 必须是带的内沿（285.64）」
- `16959/16975 ✓`「78 对 ⇒ **39 行** ⇒ 内容高 **6222**」「可滚范围 **5681.21**」—— 内容高 / `MaxOffset` **没坏**
- `19804`「数据 = 预组「第四連隊」（`OverrideDeck` 那一支）」；`19718`「[DeckInfo] state=View(2)…藏…Practice Deck」；`19878 ✗`
- `19908`「state=Edit(0)…藏：Edit Deck/…/Practice Deck」⇒ `19921 ✗`
- `19761`「接线层…`Practice Deck` 可点 = True」⇒ `20035 ✗`；`20088`「Practice Deck ⇒ 开选卡组窗挑对手」；`20065` 该窗 Build 后**无关闭记录** ⇒ `20105 ✗`
- `24021`「[Win] Deck Selection Popup with Tabs：TryOpen 命中 Background ← ShowPreviousWindow」；`24251/24488` 两条吸收层 ✗
- `37187`「实测「菜单测试卡组 合法30」，期望「菜单测试卡组 1」」；新 `19543`「[Practice] 开战：「第四連隊」」vs 旧 `18391`「开战：「菜单测试卡组 1」」

## 四个重点核实

1. **软边断的是**「每条切线落在 `want` 内沿之一 + 每个 `want` 都出现」（`CheckSoftCuts`），**不是**内容高；内容高/`MaxOffset` 另在 `:3224-3227`，本轮**绿**。首格 y 仍 262.64；变的是 **39 行使下带 780.43 够得到**。
2. 三条 `Practice Deck` 用的是 `ShowDeckContent` 开出的详情窗，新数据下 = 第 1 对 · Skirmish 副 `UM_SK_1_2`（12 张）；判据不是「卡数 < 20」，而是 `DeckRules.Validate(deck, skirmish=true)` ⇒ 合法 ⇒ `true`。
3. 「第 1 套塞了督军」的夹具**还在**（菜单测试卡组 1，4 unit + 督军），只是**不再当前**；旧批次是练习窗 `Battle!` 的 `CollectionData.Select(0)` 顺手改的。
4. `State` 改回 0 与数据源**有关**，但**不是 `PrebuiltDecks` 接口回归**：`DeckInfoPopup.cs` / `MenuDraw.cs` / `LiveOpsEventWindow.cs` 本批 `git diff` **为空**；变的是输入（`OverrideDeck` + `IsPlayerDeck=false`）。

> 代理类型：`Explore`（只读，不能写文件）⇒ 本文件由主对话按其 hand-back 原文落盘（铁律 5·b）。
