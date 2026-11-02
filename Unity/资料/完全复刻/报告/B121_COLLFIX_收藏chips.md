# B121_COLLFIX · 收藏 chip 三条后续（`B121-COLL-21` / `-22` / `-23`）

> 会话 2026-11-03 · 执行代理 `E121-COLLFIX` · 施工来源 = `报告/B121_COLL16_页头chip.md` §四 / §五
> 判据一手：`decomp_generic_trial/out/CollectionFilterController_object___NotifyFilterChange__15f01a0.c` ·
> `decomp_full/CardArmyFilter__*` · `bundle_menus_assets_all`（`Header/Filters` 的 GridLayoutGroup + CSF）
> 白名单内改动：`Core/FilterPanelModel.cs` · `Deck/DeckRuntime.cs` · `Editor/DeckScene.cs`（只改第 1 条的既有断言）·
> `Editor/B121_C1_Assert.cs`（改写 COLL-07 的 `AppendChips` 直测）· 新建 `Editor/B121_COLLFIX_Assert.cs` · 账补丁 `账/_parts/B121_COLLFIX.tsv`。
> ⛔ 未动：`Editor/CollectionScene.cs` · 其它 `*Scene.cs` · 账主表 · 活状态表 · 正本 · Unity · git。

## 一、逐条

| ID | 改了什么 | 判据 | 落点 | 验证 | 还差什么 |
|---|---|---|---|---|---|
| B121-COLL-21 | 抽屉行内的 chip 副本按原版删掉：`Build` 的 4 处 `AppendChips` 调用点退场（改成说明），`ComputeLayout` 由 `facs.Count + armySel` 回到 `facs.Count` | 原版 `FillToggleList` 在行内 Instantiate 完，`NotifyFilterChange` **立刻 `SetParent(filterIconHolder)` 搬进页头**（`.c:154`）⇒ 行内不留 chip | `Core/FilterPanelModel.cs:647`（Build）· 同文件 `:829`（AppendChips 保留，doc 已就地订正） | 模型层断言「选 4 个阵营 ⇒ 行内 `$chip:` = 0 颗」「稀有度第一格 y 与不选时逐位相同」；`DeckScene` 的 chip 行那节按新语义整段改写（原「Army 行长高 / 三行溢出」两条判据随行内 chip 一起退场） | 无（`BuildTitles` 本来就走 `ComputeLayout(state)` = 阵营数 ⇒ 修完与 `Build` 一致；见 §三.1） |
| B121-COLL-22 | 卡组编辑窗页头 chip 从 0 到有：新增 `RefreshHeaderChips` / `MoveHdrClear` / `HandleHeaderChipClick` + 三个自检读数；卡牌页读 `State.Filter`、卡背页读 `_cosmoFilter` + `armyOnly`；`Clear filters` 的图/字/命中矩形随 chip 数一起右移 | 同 `.c:154` + `.c:190`（`SetActive(0 < toggleOptions.Count)`）；容器 `GridLayoutGroup 85×70 / spacing 5,0 / FixedRowCount(1) / MiddleLeft` + `CSF(MinSize)` ⇒ 宽 `90n−5`、钮锚右 `pos(20,0)` 跟着右移 | `Deck/DeckRuntime.cs:10308`（RefreshHeaderChips）· `:10348`（MoveHdrClear）· `:10359`（HandleHeaderChipClick）· `:10376`（UiHeaderChipCount）；挂接点 = `RefreshHeader` 末句、`RefreshCosmoFilters` 末句、`HandlePointer` 的 `downL` 那行、`Build` 的清表那行（全部同物理行追加 ⇒ 行号零漂移） | `typecheck` 0/0；`check_ledger_points` 漂移 0 / 失败 0 | **节点层未验**（要 `DeckRuntime` 实例：读 `UiHeaderChipCount` / `UiHeaderChipRect(i)`、点一颗再看）—— 本批不建窗，已在断言里记「未验」；卡组编辑窗 `Clear filters` 的显隐仍只看 `State.Filter`（见 §三.2） |
| B121-COLL-23 | `BuildCosmetics` 的 `On` 由单值 `f.Faction == facs[i]` 改成 `DeckFilter.FactionSelected(f, facs[i])`（集合优先、单值兜底） | 阵营格亮不亮 = 这一项自己的 IsOn（`CardArmyFilter__FillToggleList.c` 只 clone `IsOn` 的选项 + `CollectionFilterToggle__SetValue.c`）；而卡背页点阵营走 `FilterPanelModel.Click("$fac:")` 写的是**集合** `f.Factions` | `Core/FilterPanelModel.cs:1127`（BuildCosmetics） | 断言：集合 `{Goff}` ⇒ Goff 那格亮、Ultramarines 不亮（灭自证）；单值 `Faction = Goff`（卡组编辑窗那条）照旧亮 | 无 |

## 二、验证

- `TMPDIR=/tmp/wf_cfx bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- `python -I -X utf8 工具/check_ledger_points.py` ⇒ **A 5876 · 通过 5876 · 漂移 0 · 失败 0**（与改前同一个数；本批全部改动做成**净零行**：`FilterPanelModel` / `DeckScene` 的改写逐块对平，`DeckRuntime` 只做同物理行追加，新成员放**文件尾的独立 partial 段**）。
- 断言宿主 **未接线**（`Editor/*Scene.cs` 除第 1 条那条既有断言外不在白名单）⇒ 调度台在 `CollectionScene.Run`（或任一宿主）加一句 `B121_COLLFIX_Assert.Run();` 即可跑；判绿红按行首 `B121CF ✓` / `B121CF ✗`。
- `decomp` 判据只读，未跑 Unity、未改 `d:/2`。

## 三、顺手发现（只报不改）

1. 🔴 **修前 `Build` 与 `BuildTitles` 本来就不同源**：`Build` 用 `ComputeLayout(facs.Count + armySel)`、`BuildTitles` 用 `ComputeLayout(state)`（= 阵营数）⇒ 一旦有 chip，**格子和四个小标题的纵向位置会错开 100px 的整数倍**。本批把 `Build` 收回 `facs.Count`，两边自动一致 —— 这同时是「为什么必须改 `Build` 而不只是删四行」的证据。
2. ⚠️ **卡组编辑窗 `Clear filters` 的显隐仍是「只看卡牌那套 `State.Filter`」**（`RefreshHeader` 里 `clearFiltersOn = !State.Filter.IsEmpty`）。原版判据是 `0 < toggleOptions.Count`，**卡背页选了阵营 ⇒ 页头有 chip ⇒ 那颗钮也该出现**。旧注释已标「卡背这一份算不算数：判据不足」；本批只补了 chip 与位移，**显隐那条没动**，留给下一批（要一起看 `_cosmoFilter` 与 `KeyLive`）。
3. ⚠️ **`n == 0` 时 `Clear filters` 的静态位置有两说**：`DeckRuntime.HdrClearX = 592.2`（2026-10-22 `D1` 按 Grid 左对齐推的）vs `B121_COLL16` 报告 §一.3 的 **612.17**（`m_IgnoreLayout = 1` + 锚右 `pos(20,0)` ⇒ 容器右缘 + 20）。本批**保留 592.2**（`Editor/DeckScene.cs:3286` 的 `CheckRect("hdr_clear", 592.2 …)` 正盯着它），⛔ 别两头各改一半 —— 要定就得同时改常量 + 断言 + 判据段。
4. 📌 **`AppendChips` 现在没有生产调用点**，但账里有 **8 行** `FillToggleList` 把它当「等价实现」落点（`收藏.tsv:25/74/80/85/132/312` + `泛型与启动链_补读.tsv:58/136`）。本批**保留**它并在 doc 里写清（克隆到格位那一步确实没变、上限 5 也在这里）；调度台若愿意，可另起一批把这 8 行的落点改指 `BuildHeaderChips`（那才是「搬进页头」后的全貌）。
5. ❓ **页头 chip 的悬停**：原版 chip 是 `CollectionFilterToggle`（toggle 族），本批只接了**点击**（`ForceOff`），没读它的 `m_Transition` —— 悬停有没有反馈**未查**，⛔ 别先按「没有」写死。
6. 📌 `RefreshHeaderChips` 每次 `RefreshHeader` 都拆/重建 chip 节点（`RefreshHeader` 在每次加牌 / 标脏都跑）。与收藏窗 `RefreshHeaderChips` 同口径，单窗口 chip ≤ 5 颗，暂按可接受；真机若见卡顿再看。
7. 📌 卡组编辑窗的卡背页 chip 用 `armyOnly`（原版那棵树 `filters[]` 里只有 `CardArmyFilter`）；**卡牌页**用全族（Army → Rarity → Cost → Type）。tab 1（Deck info）沿用卡牌页那一套 —— 原版 `CardDisplay` 在 Cards / Deck info 两页都显示（`A1497④` 的同一份判据），本批照此。
