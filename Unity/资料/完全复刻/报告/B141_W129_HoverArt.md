# B141 · W129 · 5-17 —— 三扇窗页签**悬停图**改为显式传 `m_HighlightedSprite` 报告（`B141-W54`）

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 执行写手 `B141-W54`。
> 输入判据 = `报告/B141_W129_ButtonFx.md` §5-17（W34 的 649 颗三元组全表）＋ `报告/B141_W129_SelectedArt.md` §顺手发现（W44）
> ＋ **本写手对原版 prefab 的第三次逐颗现读**（`d:/2/tools/assetripper_out/ExportedProject/Assets/*.prefab` 的
> `m_SpriteState.m_HighlightedSprite` → sprite guid → `Sprite/*.asset.meta` 反查切片名；脚本见 §六）。
> ⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未改账主表 / 活状态表 / 所有权表。
> 写入 = 3 个白名单 `.cs` ＋ 新建 `Editor/B141_W54_Assert.cs` ＋ 本文件。

---

## 〇、一句话结论

**三扇窗（设置 / 玩家档案 / 排行榜）的页签生成器现在都显式传 `hoverArt = 40K_settings_button_selected`**
（= 原版 18 颗页签逐颗现读的 `m_SpriteState.m_HighlightedSprite`），并配 18 条断言。

| 项 | 数 |
|---|---:|
| 接线站点 | **3**（设置窗 5 颗 · 档案窗 6 颗 · 排行榜三份 prefab 7 颗，共 **18 颗页签实例**） |
| 原版悬停图 | **18/18 = `40K_settings_button_selected`**（逐颗现读，无例外） |
| 行数变化 | 三个 `.cs` **全部 0 行**（新常量接在既有声明行上、注释就地改写） |
| `typecheck` | `TMPDIR=/tmp/wf_b141_w54 bash 工具/typecheck.sh` ⇒ 运行时 **0/0** · 编辑器 **0/0** |
| 断言 | **18 条**（A 组 9 · B 组 9），`Editor/B141_W54_Assert.cs`，`Run()` 零前提 |

🔴 **一条必须点名的事实订正（铁律 5）**：W44 报告 §顺手发现 说「悬停图**目前是** `40K_settings_button_hover`
（= 后备规律推出的）」—— **按今天的盘上代码，这句不成立**：`WindowButton.HoverNames` 从**第十七会话（A17）**
起就有 `{ "40K_settings_button" → "40K_settings_button_selected" }`（`git log -S` 落点 `21724955`），
而这 3 个站点传的常态图正是 `40K_settings_button` ⇒ **改前实际渲染的悬停图已经是 `_selected`**。
本批的真实价值 = **把「隐式依赖全局表」改成「逐站显式声明」＋补断言**（行为上零变化，结构上不再依赖那张表）——
如实写在这里，⛔ 不要当成「修好了一张错图」去记账。

---

## 一、判据：原版逐颗现读（2026-11-04）

扫描法：解析 `ExportedProject/Assets/<prefab>.prefab` 的 YAML 文档块，取每份 `Selectable` 的
`m_SpriteState`，由 `m_GameObject` + `m_Father` 拼父链、由 sprite `guid` 反查 `Sprite/*.asset.meta` 的切片名
（脚本 `_tmp_view/b141_w54/dump_tabs.py`，只读、`python -I` 跑）。

| prefab | 页签节点 | 颗数 | 常态图（`m_TargetGraphic.m_Sprite`） | **悬停图（`m_HighlightedSprite`）** | 选中图（`m_SelectedSprite`） |
|---|---|---:|---|---|---|
| `Main Menu Settings Window` | `Menu Area/Mask Tabs buttons/Tab Buttons/{General,Media,Account,Graphics,Support}` | 5 | `General` = `40K_settings_button_hover`（默认选中那颗）· 其余 4 颗 = `40K_settings_button` | **`40K_settings_button_selected` ×5** | `40K_settings_button_hover` ×5 |
| `Player Profile Window` | `Menu Area/Tab Buttons/{Profile Button,Avatar Button,Title Button,Battle Log Button,Trophies,Ranked}` | 6 | `40K_settings_button` ×6 | **`40K_settings_button_selected` ×6** | `40K_settings_button_hover` ×6 |
| `RankedSkirmishLeaderboardPopup` | `Tab Buttons/{Player,Armies,Alliances}` | 3 | `40K_settings_button_hover` ×3 | **`40K_settings_button_selected` ×3** | `40K_settings_button_hover` ×3 |
| `RankedClassicLeaderboardPopup Variant` | `Tab Buttons/{Player,Armies}` | 2 | `40K_settings_button_hover` ×2 | **`40K_settings_button_selected` ×2** | `40K_settings_button_hover` ×2 |
| `DraftLeaderboardPopup` | `Tab Buttons/{Armies,Alliances}` | 2 | `40K_settings_button_hover` ×2 | **`40K_settings_button_selected` ×2** | `40K_settings_button_hover` ×2 |

- 五份 prefab 的页签**全部** `m_Transition = 2`（SpriteSwap）· 按下图 = `40K_settings_button_pressed`。
- 与 W34 §5-17 那两条表项**互相印证**：23 颗「常态 `40K_settings_button` → `_selected`」里，设置窗 4 + 档案窗 6
  就在这里；13 颗「常态 `40K_settings_button_hover` → `_selected`」里，设置窗 General 1 + 排行榜 7 在这里。
- ⚠️ **「常态图」与「悬停图」两件事别混**：排行榜三份 prefab 的**常态**图是 `_hover`（原版把「选中态」那张当常态用），
  而**悬停**图仍是 `_selected` —— 我们这一侧常态图按既有接线保持 `ArtTabOff = 40K_settings_button`（未改动），
  **本批只补悬停那一格**。

---

## 二、改动（3 站点 · 行数全部 0 变化）

| # | 文件:行 | 改前 | 改后 |
|---:|---|---|---|
| 1 | `Shell/SettingsWindow.cs:1334` | `ArtTabBgSel = "40K_settings_button_hover";` | 同一行追加 `public const string ArtTabHover = "40K_settings_button_selected";`（+ 出处注释） |
| 2 | `Shell/SettingsWindow.cs:2007-2008` | `Hit(..., bg, ArtTabBg,` / `clickCue: …, selectedArt: ArtTabBgSel)` | 第二行改为 `hoverArt: ArtTabHover, clickCue: …, selectedArt: ArtTabBgSel` |
| 3 | `Shell/PlayerProfileWindow.cs:124` | `ArtTabOn = "40K_settings_button_hover";` | 同一行追加 `, ArtTabHover = "40K_settings_button_selected";`（+ 出处注释） |
| 4 | `Shell/PlayerProfileWindow.cs:415` | `Bind(_btnBg[i], ArtTabOff, null, null, ArtTabOn)` | `Bind(_btnBg[i], ArtTabOff, ArtTabHover, null, ArtTabOn)` |
| 5 | `Shell/LeaderboardWindow.cs:121` | `ArtTabOn = …, ArtTabOff = "40K_settings_button";` | 同一行追加 `, ArtTabHover = "40K_settings_button_selected";` |
| 6 | `Shell/LeaderboardWindow.cs:673` | `Bind(bgQ, ArtTabOff, null, null, ArtTabOn)` | `Bind(bgQ, ArtTabOff, ArtTabHover, null, ArtTabOn)` |

- 另在 4 处既有注释里就地补 `✅ 已复刻（2026-11-04 · B141 · 5-17）` 标记（不新增行）：
  `SettingsWindow.cs:2004-2006` · `LeaderboardWindow.cs:658-659` · `PlayerProfileWindow.cs:415` 行尾 · `LeaderboardWindow.cs:673` 行尾。
- **行数**：`SettingsWindow.cs` 7438 → **7438** · `PlayerProfileWindow.cs` 633 → **633** · `LeaderboardWindow.cs` 944 → **944**
  ⇒ 账的 `文件:行号` 落点**不漂**（本批零行数变化是刻意为之，见 CLAUDE.md §18·b/§20 的同族纪律）。
- 行尾：三个文件均 **LF · 0 个 CRLF · 0 个裸 CR**（二进制计数复核）。

---

## 三、断言（`Editor/B141_W54_Assert.cs` · 18 条 · `Run()` 零前提）

| 组 | 条 | 盯什么 |
|---|---|---|
| A | 9 | **逐站接线**（只读 3 份 `.cs` 文本）：A1/A1b 设置窗两个常量（悬停 ≠ 选中）· A2 设置窗 5 颗页签显式传 `hoverArt` · A3/A3b 档案窗常量 + `Bind` 第三格 · A4/A4b 排行榜常量 + `Bind` 第三格 · A5/A5b 三份文件悬停图 ≠ 选中图（文本 + 常量两档） |
| B | 9 | **运行时探针 / 灭自证 / 口径**：B1/B1b 两张图取得到 · **B2/B2b** 显式绑 `_selected` ⇒ `HoverTexForTest.name == _selected` 且 ≠ `_hover` · B3/B3b 真图不记 `MissingSwapArt` · **B4 🧨 灭自证**（同探针改绑 `_hover` ⇒ 读数口看得见变化，证明 B2 不是恒真）· **B5 口径**：不传 `hoverArt` 时全局表仍命中 `_selected`（如实记「改前渲染已是 `_selected`」） |

**灭自证（什么坏法 → 哪条红）**

| 改坏法 | 变红的断言 |
|---|---|
| 删 `SettingsWindow` 的 `hoverArt: ArtTabHover` | **A2** |
| `PlayerProfileWindow` / `LeaderboardWindow` 把 `ArtTabHover` 改回 `null` | **A3b / A4b** |
| 三个 `ArtTabHover` 常量改成 `40K_settings_button_hover` | **A1 / A3 / A4 + A5 / A5b** |
| 删 `Resources/Art/ui_menu/40K_settings_button_selected.png` | **B1** |
| `WindowButton.Bind` 改成忽略 `hoverArt`（`hn = HoverNameFor(art)` 恒成立） | **B2** |
| 探针读数口（`HoverTexForTest`）改成恒返 `_normalTex` | **B2 + B4** |

⚠️ **一处必须说清**：**删掉显式实参不会让画面变**（全局表兜着）⇒ 只靠运行时读数**无法**区分
「显式传了」与「靠表兜底」；那一档由 **A2/A3b/A4b（读源码文本）** 兜 —— 这是本批把两种断言都写上的原因。

---

## 四、验证

1. `TMPDIR=/tmp/wf_b141_w54 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（本写手 3 个 `.cs` + 新断言文件）。
2. 未跑 Unity（红线）。新断言文件**尚未接线**到任何宿主 —— 挂法见文件头（由调度台在同一物理行追加
   `B141_W54_Assert.Run(); pass += …; fail += …;`，⛔ 别 `grep -c ✗` 数，按行首 `B141W54 ✓/✗` 数）。

---

## 五、没做 / 没查到（❓）

1. ❓ **`AchievementsMenu` 的 5 颗成就页签**（原版 `Player Profile Window/Trophies Tab/buttons/Achievement Type Toggle (0..4)`，
   悬停图同为 `40K_settings_button_selected`）—— 落点 `Shell/AchievementsMenu.cs` **不在本包白名单**，本批 0 动。
2. ❓ **`ChatPanel` 两个频道键**（`Orange Tab Toggle` 族）—— 同族映射，但它是 W44 的 `TabArtHover` 口径，
   已有显式传值（`ChatPanel.cs` 的 `TabArtHover`），**本批未复核**其值是否 = `_selected`（属别包）。
3. ❓ **排行榜三份 prefab 的「常态图」**：原版是 `40K_settings_button_hover`，我们这一侧仍画 `40K_settings_button`。
   这是**另一条账**（常态图，不是本批的悬停图），本批不动、如实记在这里。
4. ⚠️ **账可能已经被别的写手引用的行号**：本批三文件零行数变化 ⇒ 不引入漂移；但调度台合并仍建议在无写手时跑一次
   `check_ledger_points`（本写手无权动账）。

---

## 六、顺手发现（只记，不改）

1. **W44 报告 §顺手发现 的那句「目前是 `_hover`」需要就地订正**（本文件 §〇 已给反证）：实际改前渲染已经是 `_selected`，
   `HoverNames` 那张表从 A17 起就有一行 `40K_settings_button → 40K_settings_button_selected`。
   原因大概是 W44 按「后备规律（`art + "_hover"`）」推断、没核表里的显式项。
2. **`WindowButton.HoverNames` 的「同一常态图两个悬停图」那一族**（`40k_UI_bt_back` / `40K_settings_button`）现在
   `40K_settings_button` 这一条**已被本批 3 个站点全部显式覆盖**；表里那一行只服务其它未显式传的调用点。
   将来若发现某站点传 `40K_settings_button` 却要 `_hover`，**必须显式传**（表值会把它带成 `_selected`）。
3. **现读用的脚本**：`d:/4/_tmp_view/b141_w54/dump_tabs.py`（只读、可复跑：
   `python -I -X utf8 dump_tabs.py <prefab> --substr "Tab Buttons"`）—— 输出「父链 / 常态图 / 悬停图 / 按下图 / 选中图」，
   guid → 切片名走 `Sprite/*.asset.meta` 反查。下次要核别的页签族可直接跑。
