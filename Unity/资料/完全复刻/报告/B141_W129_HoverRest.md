# B141 · W129 · 5-17 剩余三处 —— 成就页签悬停显式接 / 排行榜常态图**反证不动** / 聊天页签四态复核（`B141-W60`）

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 执行写手 `B141-W60`。
> 输入判据 = `报告/B141_W129_HoverArt.md` §❓1/2/3 ＋ `报告/B141_W129_ButtonFx.md` §5-17
> ＋ **本写手对原版 prefab 与反编译的第二轮现读**（`d:/2/tools/assetripper_out/ExportedProject/Assets/*.prefab`
> 的 `Selectable.m_SpriteState` **和** `EverguildToggle.onSprite/offSprite/spriteToChange` **和** `Toggle.m_IsOn`；
> ＋ `d:/2/tools/decomp_full/EverguildToggle__Awake.c` / `__ToggleSprite.c` / `__RefreshVisuals.c`）。
> ⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未改账主表 / 活状态表 / 所有权表。
> 写入 = 3 个白名单 `.cs`（**全部 0 行数变化**）＋ 新建 `Editor/B141_W60_Assert.cs` ＋ 本文件。

---

## 〇、一句话结论

**三处里只有第 1 处真改代码**：成就页 5 颗页签把悬停图 `40K_settings_button_selected` 从「靠全局表隐式兜底」改成**逐颗显式传**。
**第 2 处（排行榜常态图）回源复核后【拒做】** —— 施工单说的「原版常态是 `_hover`」只读了 `m_TargetGraphic.m_Sprite`，
那是 prefab 在 **`m_IsOn = 1` 存盘**时留下的快照；真正驱动常态/选中的是 `EverguildToggle.offSprite/onSprite`
（`offSprite = 40K_settings_button`）⇒ 改成 `_hover` 会把「选中/未选中」画成同一张图（**画面回归**）。
**第 3 处（聊天页签）现读四态与原版逐值相同** ⇒ 零改动，只补标记。

| # | 站点 | 原版判据（2026-11-04 现读） | 我们改前 | 本批 |
|---:|---|---|---|---|
| 1 | `Shell/AchievementsMenu.cs`（5 颗 `Achievement Type Toggle (0..4)`） | `m_HighlightedSprite = 40K_settings_button_selected` ×5 | 不传 `hoverArt`（靠 `HoverNames` 表兜） | ✅ **改成显式传** `ArtTabHover`；新增常量（同一行） |
| 2 | `Shell/LeaderboardWindow.cs`（三份 prefab 7 颗页签） | `offSprite = 40K_settings_button` · `onSprite/m_SelectedSprite = 40K_settings_button_hover` · `m_HighlightedSprite = _selected` | `ArtTabOff = 40K_settings_button` · `ArtTabOn = _hover` · `ArtTabHover = _selected` | ⛔ **零改动（反证，见 §二）**；补注释 + 断言守卫 |
| 3 | `Shell/ChatPanel.cs`（`Orange Tab Toggle` 1 颗 · 两频道共用） | off `40K_settings_button` · on/selected `_hover` · hover `_selected` · pressed `_pressed` | 与上**逐值相同** | ✅ 零改动（只补标记 + 断言） |

| 项 | 数 |
|---|---:|
| 代码改动站点 | **1**（成就页 5 颗） |
| 复核后零改动站点 | 2（排行榜 7 颗 · 聊天 1 颗） |
| 行数变化 | 三个 `.cs` **全部 0**（常量追加在既有声明行 · 注释全部就地改写 · 调用点在同一行内补一个实参） |
| `typecheck` | `TMPDIR=/tmp/wf_b141_w60 bash 工具/typecheck.sh` ⇒ 运行时 **0/0** · 编辑器 **0/0** |
| 断言 | **24 条**（A 组 14 · B 组 10），`Editor/B141_W60_Assert.cs`，`Run()` 零前提 |

---

## 一、① 成就页 5 颗页签 —— 悬停图改成**逐颗显式传**

**原版判据（逐颗现读，`Player Profile Window.prefab`）**

| 节点（5/5 颗逐值相同） | 值 |
|---|---|
| 父链 | `Player Profile Window/Menu Area/Tab  Area/Tab Content/Trophies Tab/buttons/Achievement Type Toggle (0..4)` |
| 组件 | `EverguildToggle`（派生 `Toggle`）· `m_Transition = 2`（SpriteSwap）· `changeSpriteOnValueChange = 1` |
| `spriteToChange` | **= `m_TargetGraphic`**（同一 pathID，节点 `…/button_bg` 的 Image） |
| `offSprite`（未选中） | `40K_settings_button` |
| `onSprite`（选中） | `40K_settings_button_hover` |
| `m_SpriteState.m_HighlightedSprite`（**悬停**） | **`40K_settings_button_selected`** |
| `m_SpriteState.m_PressedSprite` | `40K_settings_button_pressed` |
| `m_SpriteState.m_SelectedSprite` | `40K_settings_button_hover`（= `onSprite`） |
| `m_IsOn` | **0** ×5（存盘时是未选中态 ⇒ `m_TargetGraphic.m_Sprite` 也存着 `40K_settings_button`）· `allowClickWhenAlreadySelected = 1` ×5 |

**改动**

| 文件:行 | 改前 | 改后 |
|---|---|---|
| `Shell/AchievementsMenu.cs:85-86` | `const ... ArtTab = "40K_settings_button", ArtTabOn = "40K_settings_button_hover";` | 同一行追加 `, ArtTabHover = "40K_settings_button_selected"`（+ 出处写进 :85 那行 `/// <summary>`） |
| `Shell/AchievementsMenu.cs:271` | `clickCue: WindowSounds.SelectToggleCue);` | `ArtTabHover, clickCue: WindowSounds.SelectToggleCue);   // ✅ 已复刻（2026-11-04 · B141-W60 · W129 5-17）…` |

- 🔴 **如实记（与 W54 §〇 同一条口径）**：**删掉这个显式实参不会让画面变** —— `WindowButton.HoverNames` 从 A17 起就有
  `{ "40K_settings_button" → "40K_settings_button_selected" }` ⇒ 改前实际渲染的悬停图**已经是 `_selected`**。
  本批的价值 = **把「隐式依赖全局表」改成「逐站显式声明」＋补断言**，**行为零变化**。⛔ 别记账成「修好了一张错图」。
- `pressedArt` 仍不传：`PressedNameFor("40K_settings_button")` = `40K_settings_button_pressed`，与原版逐字相同。
- `selectedArt` 本批**不接**（见 §五 ❓1）。

---

## 二、② 排行榜三份 prefab 的「常态图」—— **回源复核后拒做（反证）**

### 施工单说的
`报告/B141_W129_HoverArt.md` §❓3：「排行榜三份 prefab 的常态图是 `40K_settings_button_hover`，我们这一侧仍画 `40K_settings_button`」。
它的依据是 §一 那张表的「常态图（`m_TargetGraphic.m_Sprite`）」列 —— **只读了一层**。

### 现读到的（同一份 prefab，往下再读两层）

| prefab ▸ `Tab Buttons/*` | `m_IsOn` | `changeSpriteOnValueChange` | `offSprite` | `onSprite` | `spriteToChange` | `m_TargetGraphic.m_Sprite`（**存盘快照**） |
|---|---:|---:|---|---|---|---|
| `RankedSkirmishLeaderboardPopup`（3 颗） | **1** ×3 | 1 | `40K_settings_button` | `40K_settings_button_hover` | **= `m_TargetGraphic`** | `_hover` |
| `RankedClassicLeaderboardPopup Variant`（2 颗） | **1** ×2 | 1 | 同上 | 同上 | 同上 | `_hover` |
| `DraftLeaderboardPopup`（2 颗） | **1** ×2 | 1 | 同上 | 同上 | 同上 | `_hover` |

**运行时证据（反编译）**

1. `EverguildToggle__Awake.c` 末尾：`changeSpriteOnValueChange(+0x17c) != 0` 时
   `onValueChanged.AddListener(ToggleSprite)` **之后立刻** `Image.set_sprite(spriteToChange, isOn ? onSprite : offSprite)`
   （`+0x120` = `m_IsOn`，`+0x180/+0x188` = `onSprite/offSprite`，`+0x190` = `spriteToChange`）。
2. `EverguildToggle__ToggleSprite.c` / `__RefreshVisuals.c`：同一个式子，值一变就换。
3. ⇒ **未选中页签运行时画 `offSprite = 40K_settings_button`**；`m_TargetGraphic.m_Sprite` 里那颗 `_hover`
   是「作者在 `m_IsOn = 1` 状态下存的盘」，**开机第一帧就会被 Awake 重写成 on/off 两档之一**。

**同一族的两处对照（正好互相反证）**：`Player Profile Window` 的 **11/11 颗 `m_IsOn = 0`** ⇒ 那 11 颗的
`m_TargetGraphic.m_Sprite` 存的都是 `40K_settings_button`；而它们与排行榜页签**是同一对 `on/offSprite`**。
⇒ 存盘快照随「存盘时 on 还是 off」变，**不是常态图定义**。

### 结论
- **`ArtTabOff = "40K_settings_button"` 是对的，保持原样**（改前改后都是它）。
- ⛔ 若按施工单改成 `_hover`，则 `on ? ArtTabOn : ArtTabOff` 两档同图 ⇒ **选中页签不再有视觉区分** = 画面回归。
- 已在 `LeaderboardWindow.cs:121` 与 `:650` 两处既有注释里就地写死这条判据（**零行数变化**），
  并在断言里加了正向守卫 `A3`（`ArtTabOff` 必须是 `40K_settings_button`）+ `A6`（三份文件同族逐字一致）——
  将来谁按旧报告「修」回去，**A3/A6 会红**。

---

## 三、③ 聊天页签 `TabArtHover` —— 现读复核：**逐值相同，零改动**

原版 `ChatPanel.prefab/Holder/Chat/Tab Buttons/Orange Tab Toggle`（1 颗模板节点，两个频道各克隆一份）：

| 格 | 原版 | 我们（`ChatPanel.cs:432-435`） | 判 |
|---|---|---|---|
| `offSprite`（未选中） | `40K_settings_button` | `TabArtOff = 40K_settings_button` | ✅ |
| `onSprite`（选中） | `40K_settings_button_hover` | `TabArtOn = 40K_settings_button_hover` | ✅ |
| `m_HighlightedSprite`（悬停） | `40K_settings_button_selected` | `TabArtHover = 40K_settings_button_selected` | ✅ |
| `m_PressedSprite`（按下） | `40K_settings_button_pressed` | `TabArtPressed = 40K_settings_button_pressed` | ✅ |
| `m_SelectedSprite` | `40K_settings_button_hover`（= `onSprite`） | `Bind(..., selectedArt: TabArtOn)` | ✅ 已接（W44） |
| `m_IsOn` / `allowClick` | 1 / 0 | （运行时按当前频道定，`allowClickWhenAlreadySelected = false`） | ✅ |

⇒ **本批只补了一行标记**（`ChatPanel.cs:432` 行尾，零行数变化），值一个字节没动。

---

## 四、验证

1. `TMPDIR=/tmp/wf_b141_w60 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（3 个 `.cs` + 新断言文件）。
2. **行数守恒**（二进制计数）：`AchievementsMenu.cs` 636→**636** · `LeaderboardWindow.cs` 944→**944** ·
   `ChatPanel.cs` 1352→**1352**；三个文件均 **LF · CRLF 0 · 裸 CR 0** ⇒ 账的 `文件:行号` 落点**不漂**。
3. 断言 **24 条**（A 组 14 · B 组 10），`Editor/B141_W60_Assert.cs`，`Run()` 零前提。
   **尚未接线到任何宿主** —— 挂法见文件头（调度台在同一物理行追加
   `B141_W60_Assert.Run(); pass += B141_W60_Assert.Pass; fail += B141_W60_Assert.Fail;`；
   判绿红按行首 `B141W60 ✓/✗` 数，⛔ 别 `grep -c ✗`）。
4. 未跑 Unity（红线）。

**灭自证（🧨 什么坏法 → 哪条红）**

| 改坏法 | 变红 |
|---|---|
| 删成就页签 `Hit(...)` 里的 `ArtTabHover,` | A2 |
| `AchievementsMenu.ArtTabHover` 改成 `_hover` | A1 · A2b |
| 🔴 `LeaderboardWindow.ArtTabOff` 改成 `_hover`（= 照旧报告做） | **A3 · A6** |
| 排行榜画底图改成恒 `ArtTabOn` | A4 |
| `ChatPanel.TabArtHover` 改成 `_hover` | A5 |
| 删 `Resources/Art/ui_menu/40K_settings_button_selected.png` | B1 |
| `WindowButton.Bind` 忽略 `hoverArt` 实参 | B4 · B6 |
| `HoverTexForTest` 改成恒返 `_normalTex` | B6 · B7 |

---

## 五、没做 / 没查到（❓）

1. ❓ **成就页 5 颗页签的 `m_SelectedSprite` 未接**（原版 = `40K_settings_button_hover` = `onSprite`）。
   `ProfilePage.Hit(...)` **不收 `selectedArt`**，要接得照排行榜那一步「建完再 `Bind` 一次」。
   - **为什么不顺手做**：那是 **5-07 的 77 颗**之一（`报告/B141_W129_ButtonFx.md` §5-07 明写「77 颗的落点全在别包」），
     属**别的包的落点**；本批白名单里没有它，且同时可能有别的写手在动这一族 ⇒ 只记录不动手。
   - **可观测性**：uGUI 的 「Selected」，= **键盘/手柄选中态**（指针操作下不出现，见 `ChatPanel.cs` 那段注释）。
     选中页签无差异（`m_SelectedSprite` 与 `onSprite` 同图）；**未选中页签被键盘焦点选中时会差一张图**。
   - 落点与判据都已写全，谁做 5-07 直接照办即可。
2. ⚠️ **顺手发现（只记，不改）**：`AchievementsMenu.cs:272-273` 的注释写「原版这一族 `allowClickWhenAlreadySelected = 0`」，
   而 prefab 现读是 **`1`**（5/5 颗）；同一份 prefab 的**左栏 6 颗页签（`Menu Area/Tab Buttons/*`）才是 `0`**。
   我们代码里给成就页签设的是 `false`（= 不允许重复点） ⇒ **可能与原版相反**（原版允许再点已选中的分类、会重放回调+音效）。
   ⚠️ **本批未改**：那属于音效线 `AUD §三 C9` 的落点，不在本包白名单，且要连带核 `Select()` 里的、
   `if (_filter == type) return;` 守卫（否则「允许重复点」也无事发生）。判据已给全，留给那一包。
3. ✅ 已确认**不需要**改的东西（防止下一轮重复查）：
   - 排行榜三份 prefab 的 `m_HighlightedSprite` = `_selected`（W54 已接，本批复核仍成立）；
   - 聊天页签四态与按下/选中两档都已在位。

---

## 六、现读脚本（只读 · 可复跑 · 都在 `d:/4/_tmp_view/b141_w60/`）

| 脚本 | 干什么 |
|---|---|
| `dump_tabcomp.py <prefab> --name "<节点名>"` | 打印页签节点上的 `Selectable`（`m_SpriteState` 四档 + `m_TargetGraphic` 及其所属 GO 路径）**和** `EverguildToggle`（`changeSpriteOnValueChange` / `onSprite` / `offSprite` / `spriteToChange` 及该 Image 当前 sprite / `allowClickWhenAlreadySelected`） |
| `list_toggles.py <prefab>` | 逐颗列出该 prefab 的全部 `EverguildToggle`：父链 · `m_IsOn` · `allowClick` · `changeSprite` · on/off · `m_TargetGraphic.m_Sprite` |

两条都 `python -I -X utf8` 跑、只读 `ExportedProject`，guid → 切片名走 `Sprite/*.asset.meta` 反查。
