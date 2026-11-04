# B141 · W129/W131 → 执行代理 `B141-W20`：MainMenuRuntime 一整包（4-06 · 4-10 · 4-19 · TOOL-03 剩余 · BadgeDy doc）

> 执行写手 `B141-W20` · 第一百二十八会话 · 2026-11-04 · 批次 `B141`
> 判据输入 = `判据/B141_W129_对账_UI动画.md`（R8 · W5 包）· `判据/B141_W131_对账_工具.md`（R10 · TOOL-03 剩余）· `报告/B138_审计汇总_问题清单.md` §4。
> 红线遵守：⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `d:/2` · ⛔ 未改账/活状态表/正本/所有权表。
> 行号一律 **2026-11-04 现读**；原版值一律**回源现读**（下列每条都写清文件 / 对象 / 字段）。

---

## 〇 · 一句话结果

四条全部落地：**4-06 网格列上限 8 → 20**（原版 `columns: 20` 现核）· **4-10 左导航纵向档 → `Capline`**（原版五颗 TMP `m_VerticalAlignment = 8192` 现核）· **4-19 保留 120 px/格并把「原版 1.0 能不能滚」现核清楚**（原版**能滚，但一格只 1 px、实际靠拖拽**；120 是 2026-10-18 用户拍板值）· **TOOL-03 两缺口补齐**（`MainMenuCarouselDrawer.Tick` 接进 `Update` 每帧泵；`RankedArmyBonusCard` 复用 `InfiniteScrollRing`，speed ❓ **已解出**）。顺手订正 `BadgeDy` 的 doc 符号（原写「`pos.y` 取负」是反的）。

---

## 一 · 逐条：原版值 vs 改后值

| 账 | 原版值（判据 = 现读出处） | 我们（改前） | 改后 | 落点 |
|---|---|---|---|---|
| **4-06** 模式卡网格列上限 | **`columns: 20`**（`rows: 2` · `itemSize 535×414.4` · `spacing 20×20`）— 判据 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`，GameObject `Content`(&28) 的 MB `&1961`（`:69480-69504` 现读） | **8**（`const int MaxCols = 8`，我们挑的） | **20**（新增 `public const int ModeGridMaxCols = 20`；`const int MaxCols = ModeGridMaxCols;`） | `Shell/MainMenuRuntime.cs:2530` + 类尾新增常量 |
| **4-10** 左导航纵向档 | **`Capline`** — 判据 = `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1716·1749·1750·1751·1752.json`，`m_VerticalAlignment = 8192`（= `0x2000`）；字体 pid `3485036404935369831`（Pragati） | 出厂 **`Middle`**（无 `SetVAlign`） | **`Capline`**（框高取文案条 `textBgR` = 39.4；原版 TMP 自身 `sd.y = 39.39`） | `Shell/MainMenuRuntime.cs:1275`（取句柄）+ `:1286`（同一行追加 `MenuDraw.SetVAlign(...)`） |
| **4-19** 模式区横滚步长 | **`m_ScrollSensitivity = 1.0`** — 判据 = `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1982.json`：`m_Horizontal 1` · `m_Vertical 0` · `m_MovementType 1` · `m_ScrollSensitivity 1.0` | **120 px/格**（`120f` 字面量） | **120 px/格**（`ModeScrollNotchPx`），并把原版 1.0 留痕成 `OriginalModeScrollSensitivity`；**结论见 §二** | `Shell/MainMenuRuntime.cs:2659` + 类尾新增两个常量 |
| **TOOL-03①** `MainMenuCarouselDrawer.Tick` 生产泵 | 原版 `UI_InfiniteScroll.Update()`：`initialized && AutoScroll ⇒ content.anchoredPosition += AutoScrollSpeed × dt → OnScroll()`（`…__Update.c`，判据见 `MainMenuOfferContainer.cs:200-214`） | **生产 0 调用**（只有自检直调 `ring3.Tick`） | `Update()` 每帧 `StepOfferRings(Time.deltaTime)`：逐颗已登记抽屉 `Tick(dt)` + 第 2 实例 `RankedArmyBonusCard.Tick(dt)`；建抽屉时 `RegisterOfferDrawerForTick(d)` 登记（销毁的每帧摘掉） | `Shell/MainMenuRuntime.cs:2282` + `:4884` + 类尾新 partial 段 |
| **TOOL-03②** 第 2 实例 `Reward Event Container 1x1` | `UI_InfiniteScroll`（prefab `&114397768327470545`）：**`AutoScroll: 1`** · **`AutoScrollSpeed: {x: -50, y: 0}`** · `AllowManualScroll: 0` · `InitByUser: 1`；容器字段 `thresholdForAutoScroll: 2`；`_threshold` 未序列化 ⇒ 脚本 `Reset()` 的 **100**（`UI_InfiniteScroll.cs:367`） | 只算 `AutoScrollEnabled`，**未复用环**（文件头自陈「⛔ 不假装滚得动」） | **复用 `InfiniteScrollRing`**：`Ring.SetItems(count, 254.90341+20)` · `Ring.AutoScroll = AutoScrollEnabled` · `Clean()` 时 `SetItems(0,0)` · `Tick(dt)` 由宿主泵 | `Shell/RankedArmyBonusCard.cs:287`（同 `Initialize` 行尾）· `:297`（`Clean` 行尾）· 类尾新增 `Ring` + `Tick` |
| **顺手订正** `BadgeDy` doc | 原版四颗 `Badge Highlight` 的 `m_AnchoredPosition`：**三颗 `{x: 51.7, y: -27.2}` + 一颗 `{x: 51.7, y: 47.9}`** — 判据 = `Rewards Base Submenu Variant.prefab`；摆放式 = `cy − spec.BadgeDy`（`:852` / `:866`） | doc 写「原版 `Badge Highlight` 的 `pos.y` **取负**」（**符号反了**） | 改写为「**原版 uGUI `pos.y` 原值**（+ = 偏上；像素帧 y 向下时由摆放式 `cy − BadgeDy` 换向）」 | `Shell/MainMenuSubmenuWindow.cs:248`（**只改这一条注释**，两行换两行） |

---

## 二 · 4-19 现核结论：「原版 `Sensitivity 1.0` 到底能不能滚？」（回答 R8）

**能滚，但一格只走 1 px ⇒ 肉眼几乎不动；原版这一段实际靠拖拽。**

证据链（全部现读，不是推断）：

1. 原版那台 `ScrollRect` 的序列化字段：`MonoBehaviour_1982.json` = `m_Horizontal 1` · `m_Vertical 0` · `m_MovementType 1` · `m_Elasticity 0.1` · `m_Inertia 1` · `m_DecelerationRate 0.135` · **`m_ScrollSensitivity 1.0`**。
2. uGUI `ScrollRect.OnScroll`：`horizontal && !vertical` 时把 `delta.y` **搬进** `delta.x`（`Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/ScrollRect.cs:665-670`），然后 `position += delta × m_ScrollSensitivity`（:675-676）⇒ **纵向滚轮是参与的**，一格 = 1 px。
3. 我们的 `MenuScroll` 口径：`ScrollUnitsPerNotch = 1`（2026-10-22 真机 `[SCROLLPROBE] raw=1.0000` 实测，一格 = ±1 个输入单位）⇒ 传 `1.0` 就是 `NotchK = 1`、**一格 1 px**。
4. 结论：原版 11134 px 宽的内容要 1 万格滚轮才到底 ⇒ **滚轮实用价值 ≈ 0**，玩家实际用**拖拽**（我们的 `MenuScroll.BeginDrag/DragTo` + `PointerLayer` 5px 阈值照旧，原版 `m_DragThreshold = 5`）。

**改法裁定（本写手执行）**：**保留 120 px/格**，不改回 1.0。理由：`Shell/MenuScroll.cs` 的 `NotchPx` doc 白纸黑字记着 **「2026-10-18 用户拍板：明确保留 120」**（并登记为「待将来评估的偏离」）；本轮只是把评估所需的那条现核做掉了。同时把 2026-10-18 那段注释里**两条已过期的说法就地订正**（铁律 5）：

- ❌ 原写「纵向滚轮在原版本来就不参与」→ ✅ 错，`delta.y` 会被搬进 `delta.x`（`ScrollRect.cs:665-670`）；
- ❌ 原写「`NotchK = 1/120` ⇒ 一格滚轮（`WHEEL_DELTA = 120`）只走 1 px」→ ✅ 量纲已过期（2026-10-22 实测一格 = ±1），传 1.0 是 `NotchK = 1`、同样 1 px/格（结论不变、理由换成正确的那条）。

**若调度台要改判成原版 1.0**：只需把 `MainMenuRuntime.ModeScrollNotchPx` 从 `120f` 改成 `1f`，并同步 `Editor/B141_W20_Assert.cs` 的 A3-1（一处常量 + 一条断言），**不需要动任何调用点**。

---

## 三 · TOOL-03 两缺口与 speed ❓

1. **缺口①（生产 0 调用）已补**：`MainMenuRuntime.Update()` 末尾泵 `StepOfferRings(Time.deltaTime)`；`StepOfferRings` = 逐颗已登记 `MainMenuCarouselDrawer.Tick(dt)` + `_rankedArmyBonusCard.Tick(dt)`。登记表 `static List<MainMenuCarouselDrawer>`：`BuildOfferDrawerForMenuCard` 建好即登记，每帧 `Prune` 摘掉已销毁的（Unity 已销毁对象 `== null` 为真）。
2. **缺口②（第 2 实例只算不算滚）已补**：`RankedArmyBonusCard` 新增 `public readonly InfiniteScrollRing Ring` + `Tick(dt)`；`Initialize` 里 `Ring.SetItems(_currentArmyImages.Count, ArmyIconW + ArmySpacing)`、`Ring.AutoScroll = AutoScrollEnabled`；`Clean()` 里 `Ring.SetItems(0, 0f)`。
3. **❓ 原版 `AutoScrollSpeed` —— 已解，不需要猜**：`R10` 只查到 `thresholdForAutoScroll = 2`，本轮回源 `d:/2/tools/assetripper_out/ExportedProject/Assets/Reward Event Container 1x1.prefab` 现读 `UI_InfiniteScroll`（`&114397768327470545`）：**`AutoScroll: 1` · `AutoScrollSpeed: {x: -50, y: 0}`**（与第 1 实例 `Main Menu Offer Container*` 同值）。⇒ **无 ❓ 遗留**。
4. `_threshold`（= `Threshold`）在 prefab 里**未序列化**，取脚本 `Reset()` 的 **100**（`out_il2csharp/…/UI_InfiniteScroll.cs:367`）—— 第 1 实例（`B128`）也是这么取的，口径一致。
5. **如实记**：这条环是**纯数据等价物**（不移动真正的图标/奖励 quad）—— 与第 1 实例同一口径。今天的可见效果 = 0（3 枚图标本来就没溢出 `Dynamic Content`；报价内容在远端 ⇒ 抽屉一条不画）。补齐的是**语义与读数**，远端数据到账那天直接生效。

---

## 四 · 行数变化（账按 `文件:行号` 引用 ⇒ 全部「零漂移」）

| 文件 | 改前行数 | 改后行数 | 手法 |
|---|---|---|---|
| `Shell/MainMenuRuntime.cs` | 5804 | 5896 | **改动全部是「同物理行改写 / 行尾追加」**；新增成员放**文件末尾新 partial 段**（`:5806+`）。机械核对：HEAD vs 现读逐行比 ⇒ 有变化的行号 = `{1275, 1286, 2282, 2530, 2647–2659, 4884}`，**与账里 74 处 `MainMenuRuntime.cs:<行号>` 落点交集 = ∅**（脚本现比，见 §六）。 |
| `Shell/RankedArmyBonusCard.cs` | 389 | 407 | 同上：`Initialize`/`Clean` 行尾追加；`Ring`/`Tick` 追加在**类尾**（`MaskPadding` 之后）。有变动的行 = `{41,42,43,287,297,388+}`，与账里 3 处落点（`:247/:254/:291`）**交集 = ∅**。 |
| `Shell/MainMenuSubmenuWindow.cs` | 923 | 923 | 两行注释换两行注释（**行数零变化**）；账里对该文件无行号引用。 |
| `Editor/B141_W20_Assert.cs` | —— | 435（新建） | 新文件。 |

---

## 五 · 断言（新建 `Editor/B141_W20_Assert.cs`）

- **调用点 64 处**，运行期展开约 **75 条**（A2 的两条在 5 颗 TMP 循环里跑 ⇒ ×5）。
- 分节：**A1** 4-06（8）· **A2** 4-10（7 站点 / 19 条）· **A3** 4-19（8）· **A4** TOOL-03①（12）· **A5** TOOL-03②（21）· **A6** BadgeDy doc（7）。
- **类型**：① 源码闸（读 `Assets/...` 源码，**先剥 `//` 行注释** ⇒ 挡「代码被追加到行尾 `//` 之后」那一类静默失效）；② 原版证据闸（读 `D:/2` 的 prefab / 场景 YAML / MB JSON，逐字段比对）；③ 行为闸（`InfiniteScrollRing` 的推进 / 搬件 / 清空，纯数据，不建菜单）。
- **灭自证**（每条都在文件头列了「哪处改动会让哪条红」）：常量改回旧值 ⇒ 红；`Update` 里删泵 ⇒ 红；泵里不调 `d.Tick` ⇒ `ContentX` 停在 0 ⇒ 红；`Ring.AutoScroll` 不跟 `AutoScrollEnabled` ⇒ 2 阵营那档红；doc 退回「取负」⇒ 红；原版字段变了 ⇒ 证据闸红。
- ⚠️ A1/A2/A3 的原版证据读 `D:/2/...`（本机档案库）；文件不存在时那几条**当场红**（⛔ 不静默跳过）。
- **本代理未跑 Unity** ⇒ 这些断言**本会话没有执行**；宿主接线 = 调度台（文件头写了两种接法，也可 `-executeMethod B141_W20_Assert.Run` 单独跑）。

---

## 六 · 验证记录

- `TMPDIR=/tmp/wf_b141_w20 bash 工具/typecheck.sh` ⇒ **本包最后一次完整通过 = 运行时 0 错 / 编辑器 0 错**（在 `StepOfferRings` 重构 + 断言 A4-7a 之后跑的；之后只改了注释与本文档）。
  ⚠️ 收尾前又跑了三轮，均撞上**别的代理在飞的文件**（都不在本包白名单、报错行也不指向本包任何文件）：
  ① `RuleEngine/Core/RuleCore.cs` 的 `ApplyWarlordTalentHandBuffOnPlay` 未定义 ·
  ② `Editor/B141_W8b_Assert.cs` 引用 `SettingsPanel`/`BattleDriver` 尚未存在的成员 ·
  ③ `Shell/MissionsTab.cs(2849,1)` 花括号未闭合（CS1022）+ `Editor/B141_W25_Assert.cs` 引用 `TextInputField.AreaNodeName` 尚未存在。
  ⇒ 按 `CLAUDE.md`「错误全集中在不是你负责的文件上 ⇒ 不是你的问题，隔一会儿重跑」处理；**本包四个文件在所有轮次里 0 错**。
- 落点漂移核对（现跑）：`HEAD` 与现读逐行比 + 账 `*.tsv` 里 `MainMenuRuntime.cs:<行号>` / `RankedArmyBonusCard.cs:<行号>` 全量正则 ⇒ **交集 = ∅**（`MainMenuRuntime` 101 处引用 / 74 个不同行号、`RankedArmyBonusCard` 3 处）。
- 报告表格：`python -I -X utf8 工具/check_md_tables.py 资料/完全复刻/报告/B141_W129_MainMenu.md <起> <止>` ⇒ 两段表 `bad rows: 0`。
- 行尾：四个文件均 **LF / 无裸 CR**（`b.count(b'\r\n')=0`，`bare_cr=0`），未翻行尾。

---

## 七 · 边界 / 没做的

- **4-19 没有把 120 改成 1.0**：这是**保留用户 2026-10-18 的拍板**，不是「没查到」。判断链与改判办法写在 §二；请调度台按「是否覆盖用户旧裁定」决定（若要改，一处常量 + 一条断言）。
- **4-10 只做了本包那一处**（`MainMenuRuntime` 左导航）。审计 4-10 是一条**跨 4 文件**的账（`SettingsWindow` · `DeckRuntime` · `BattleDriver` ×2），其余 4 处归 W1/W17/W22。
- **TOOL-03 的「可见效果」仍是 0**（环不移动 quad；远端报价数据未到账）—— 与第 1 实例（`B128`）同一口径，属如实登记而非缺陷掩盖。
- **未动**：`MainMenuOfferContainer.cs`（只读）· 账主表 · 活状态表 · `CLAUDE.md` · 文件所有权表 · 其他任何文件。
