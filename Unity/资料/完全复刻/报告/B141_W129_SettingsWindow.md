# B141 · W129 · `SettingsWindow` 四条 UI 参数（执行代理 `B141-W13`）

> 2026-11-04 · 批次 `B141` · 会话 第一百二十八 · 波次 `W129`
> 范围 = **只做 `Shell/SettingsWindow.cs` 自己那一份**（B138 §4 的 `4-05` · `4-10` · `4-13` · `4-01`）。
> 判据 = `判据/B141_W129_对账_UI动画.md`（R8 报告 W1 包）+ `报告/B138_审计汇总_问题清单.md` §4 +
> **原版 prefab 现读**（`d:/2/tools/assetripper_out/ExportedProject/Assets/Main Menu Settings Window.prefab` ，
> 102 颗 TMP 逐颗解析：`m_fontAsset` / `m_lineSpacing` / `m_characterSpacing` / `m_overflowMode` /
> `m_fontStyle` / `m_VerticalAlignment`）。
> ⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未动账/活状态表/所有权表 · 未碰 `Core/TmpFont.cs`。

---

## 一、结论（一句话）

四条全部落地，`工具/typecheck.sh` **0/0**（运行时 0 · 编辑器 0）；四条都配了断言与「灭自证」，
`Editor/SettingsScene.cs` 里那条**钉死满高 573.96 的旧断言已同批改成按内容高**（不改必红）。

| # | 项 | 原版值 | 改前 | 改后 | 落点（现读） |
|---|---|---|---|---|---|
| `4-05` | 语言下拉面板高 | 按内容收缩：**n × 40.8707 + 0.8519**<br>12 项 = **491.30** · 13 项 = **532.17** | 恒 **573.96**（+82.66） | **按内容收缩**（13 项 = 532.17） | `Shell/SettingsWindow.cs:3670`（`BuildLangListSubtree`）· `:3764`（`ShowLangList` 克隆体） |
| `4-10` | 页标题垂直档 | **`m_VerticalAlignment = 8192 (Capline)`**（5 颗 `Tab Title` 逐颗实读） | 只 `AlignLeft`（档停在 `Middle`） | **Capline** | `Shell/SettingsWindow.cs:2264`（General 内联建）· `:6407`（`PageTitle` 漏斗，覆盖 Media/Graphics/Account/Support/Online） |
| `4-13` | 行距 / 字距 / 溢出 | 行距非 0 **只有 2 颗 = 15**（`Faq Text` · `Faq Text Mobile`）；字距 **102/102 = 0**；溢出 **102/102 = 0（Truncate 0 颗）** | 行距 **0**（没接） | 行距 **15**（那两颗）；字距/溢出**原版本来就是 0 ⇒ 做的是「确认 + 断言」** | `Shell/SettingsWindow.cs:3144`（`SpText` 加形参）· `:3018`/`:3101`（两颗调用点传 15） |
| `4-01` | 字体面 | 整窗 **102/102 颗 TMP = `Pragati-Regular SDF`**（guid `2548b731a71f84745a1b13c211c923ca`） | 漏斗缺省 `Face.Cjk`（Noto，行盒 **1.2933 em**）⇒ 同框自适应系统性偏小 | 漏斗缺省 **`Face.LatinCardBody`**（= `Fonts/Pragati-Regular SDF`）⇒ 141 个 `Text(` 调用点一次覆盖；另外 3 颗直调 `MenuDraw.Text` 的**本来就传了**同一面 | `Shell/SettingsWindow.cs:6316`（形参）· `:6336`（`face: face` 透传） |

---

## 二、逐条（判据 → 改法 → 灭自证）

### `4-05` 语言下拉面板高按内容收缩

- **判据**：原版 `TMP_Dropdown.Show()`（本机 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Dropdown.cs:926-932`，**未改动的官方 ugui**）：
  ```
  sizeDelta.y = itemSize.y * m_Items.Count + offsetMin.y - offsetMax.y;
  extraSpace = dropdownRectTransform.rect.height - contentRectTransform.rect.height;
  if (extraSpace > 0) dropdownRectTransform.sizeDelta.y -= extraSpace;
  ```
  ⇒ **面板高 = 内容高**。`Item` 高 = 40.8707、`Content` 模板高 41.7226 ⇒ 常量项 0.8519。
- **实现**：`BuildLangListSubtree` 里新算 `panelB = withRows ? min(LstB, LstT + DropdownContentH(total)) : LstB`
  （`withRows = false` = **`Template` 原型，仍是序列化的 573.96**）；`bg` / `Viewport` / `Scrollbar` / `Sliding Area` / `Handle`
  五处底边全部改用 `panelB`；`ShowLangList` 建克隆体时用同一条算式（**同一份 `DropdownContentH`**，⛔ 不抄第二份）。
- **数值**：13 项（现读 `Loc.Languages.Length = 13`）⇒ 内容高 **532.171**（屏幕 **478.95**）、面板底设计 **922.838**（屏幕 **884.55**）。
  12 项那一档 = **491.30**（B138 给的参考值），公式同一个。
- **同批改掉的旧断言**：`Editor/SettingsScene.cs:1847-1866`（原期望 `…–[989.00,**922.16**]` = 满高手算）
  ⇒ 改成 `…–[989.00,**884.55**]`，并在文案里写清「改回 573.96 ⇒ 本行红」。
  `Template` 那一节的 `CheckAtS(vp, …, 964.627)`（`SettingsScene.cs:1830`）**不动** —— 量的是原型，本来就该是满高。
- **灭自证**：A9 断「面板高 < 满高 516.56px」，与 A7/A8 的期望值**不同源**（A9 是严格不等式）。

### `4-10` 页标题 `Capline`

- **判据**：prefab 逐颗实读，5 颗 `Tab Title` 的 `m_VerticalAlignment` 全是 **8192 (Capline)**
  （General / Media / Graphics / Account / Support 各一颗）。
- **实现**：两处建标题的地方各加一句 `MenuDraw.SetVAlign(…, Label.VAlign.Capline, new PxRect(TitleL, TitleT, TitleR, TitleB))`
  —— General 页是**内联建**的（`:2264`）、其余页走 `PageTitle()` 漏斗（`:6407`）。
  `AlignLeft` 只改 x（`MenuDraw.AlignLeft:2123` → `Label.AlignLeftOn(x)`），与垂直档互不影响。
- **灭自证**：B3 断 `VAlignTier != Middle`（删掉那句 ⇒ 回到 `Label` 出厂档 ⇒ 红）。

### `4-13` 行距 / 字距 / 溢出

- **判据（整窗 102 颗逐颗现读）**：
  - `m_lineSpacing` 非 0 的**只有两颗**：`Support Tab/Faq Text` = **15**、`Support Tab/Faq Text Mobile` = **15**（其余 100 颗 = 0）；
  - `m_characterSpacing` **102/102 = 0**；
  - `m_overflowMode` **102/102 = 0（Overflow）** ⇒ **本窗没有 Truncate**（4-13 那 5 颗 Truncate 不在设置窗）。
- **实现**：`SpText(...)` 加 `lineSpacingPx = 0f` 形参，`> 0` 时调 `MenuDraw.SetLineSpacing`（`Shell/MenuDraw.cs:2166`，公共口已有，**没新造 API**）；
  两颗 Faq 调用点传 `15f`。字距/溢出**不动**（原版就是 0）⇒ 只加断言守「别被后人写成非 0」。
- **单位**：TMP 的 `lineSpacing` 与 `fontSize` 同档（字体单位）⇒ **原版值直接传，⛔ 不乘 0.9**（`MenuDraw.SetLineSpacing` 的 doc 同此口径）。
- **灭自证**：C3 拿邻位 `Contact Text`（原版 0）做对照 ⇒ 防「整页一律 15」那种假绿。

### `4-01` 字体面（本窗 = Pragati）

- **判据**：`Main Menu Settings Window.prefab` 里 **102 颗 TMP 的 `m_fontAsset` 全指向同一份**：
  `{fileID: 11400000, guid: 2548b731a71f84745a1b13c211c923ca, type: 2}`；
  该 guid 的 `.meta` = **`Pragati-Regular SDF.asset.meta`**。`Settings Menu Tab Toggle.prefab` 同值。
  ⇒ 我们的等价物 = `TmpFont.Face.LatinCardBody`（`Core/TmpFont.cs` 的 `FaceResource[2] = "Fonts/Pragati-Regular SDF"`）。
- **实现**：本地 `Text(...)` 漏斗加形参 `TmpFont.Face face = TmpFont.Face.LatinCardBody` 并透传给
  `MenuDraw.Text(..., face: face)`（`MenuDraw.TextCore:2063` 的 `lb.SetFace(face)` 在 `SetGlyphHeight` **之前**，次序正确）。
  覆盖**走本漏斗的全部调用点**（B138 §4 `4-01` 口径：144 处 `Text(` − 3 处直调 = **141**，本次逐处核过）；
  另外 3 处直调 `MenuDraw.Text` 的（`SettingsWindow.cs:6761` / `:6762` / `:6882`，输入框）
  **本来就传了** `LatinCardBody` ⇒ 全窗同一个面。
- **汉字兜底**：`Label.ActualFace` 见 `Loc.HasCjk(文本)` 就翻回 `Cjk`（三份拉丁资产没有汉字字形）⇒ 中文文案不会变方框，这条**没动**、也不用动。
- **灭自证**：D1 钉「`Face.LatinCardBody` 取到的资产名含 `Pragati`」；D3 断「窗里 **全部** `Label.Face == LatinCardBody`」
  （拿掉 `face: face` ⇒ 只剩那 4 颗直调的还是拉丁面 ⇒ 红）。
- **⚠️ 未做的一半（不是本窗的活）**：`Core/TmpFont.cs` 的**「站点→面」选择表**属 `W23`，本代理**只按 prefab 事实给本窗传面**。

---

## 三、行数变化（逐文件）

| 文件 | HEAD | 现读 | 变化 |
|---|---|---|---|
| `Shell/SettingsWindow.cs` | 7,341 | **7,384** | **+43** |
| `Editor/SettingsScene.cs` | 5,842 | **5,854** | **+12**（含 1 行接线） |
| `Editor/B141_W13_Assert.cs` | —— | **208** | 新建 |

- 行尾：三份**全是纯 LF**（HEAD 也是 LF，逐字节核过 `b.count(b'\r\n')` = 0）⇒ **没有翻行尾**。
- 账落点：`grep -rn "SettingsWindow" 账/*.tsv` = **0 命中** ⇒ 本次行号位移**不会**让 `check_ledger_points` 漂移。

---

## 四、断言（`Editor/B141_W13_Assert.cs`，已在 `SettingsScene.RunCore()` 接线）

接线位置 = A862 语言下拉那一节之后、图像页之前（一行）：
`B141_W13_Assert.Run(win); _sink.Pass += B141_W13_Assert.Pass; _sink.Fail += B141_W13_Assert.Fail;`

| 段 | 断什么 | 灭自证 |
|---|---|---|
| A1/A1b | 公式：12 项 = **491.30** · 13 项 = **532.17** | 写死单值 ⇒ 另一条红 |
| A2 | `Template` 原型仍 573.96 | 把原型一起缩 ⇒ 红 |
| A6–A8 | 活克隆体 `bg` 的 [左/顶/右] 不变、底 = **884.55**、高 = **478.95** | 改回 573.96 ⇒ 红 |
| A9 | 面板高 **< 满高 516.56** | 与 A7/A8 不同源 |
| B2/B3 | 5 页 `Tab Title` 的 `VAlignTier == Capline` 且 `!= Middle` | 删 `SetVAlign` ⇒ 红 |
| C1/C2 | `Faq Text` · `Faq Text Mobile` 的 TMP `lineSpacing == 15` | 拿掉形参 ⇒ 红 |
| C3 | `Contact Text` 仍 0（对照） | 写成「整页 15」⇒ 红 |
| C5/C6 | 整窗**无**非 0 字距、无改过溢出（原版 102/102 = 0） | —— |
| D1 | `Face.LatinCardBody` 的资产名含 `Pragati` | 换枚举序/换绑定 ⇒ 红 |
| D3 | 窗里**全部** `Label.Face == LatinCardBody` | 去掉 `face: face` ⇒ 红 |

---

## 五、❓ 查不到 / 未做（如实登记）

1. **`4-13` 的「字体样式」那一格（`m_fontStyle`）不在本次派单里，故未改** —— 但 prefab 已逐颗读出：
   本窗 **11 颗**非 0：**Underline(4)** ×7 = `Privacy Policy Button/Button Text` · `Terms of Service Text` ·
   `Account Form/Reset Password` · `Forgot Password`(两处) · `Subscribe Newsletter` · `old menu/Steam Button/Button Text`；
   **Italic(2)** ×4 = 两对输入框 `Placeholder`。其中**我们真建的**有 `Privacy Policy` / `Terms of Service` /
   `Reset Password` / `Forgot Password` / `Subscribe Newsletter` / 输入框占位 6 类。
   `Label.SetFontStyle`（`Battle/Label.cs:2547`）**已有**，只差调用点 ⇒ **可以直接派一条小活**（判据已齐）。
2. **`4-05` 的同族缺陷在【战斗内设置面板】也有一份**：`Battle/SettingsPanel.cs:686`（`const float LstHPx = 573.96f`）+
   `:1730`（`BuildLangListSubtree`，`bot = top - LstHPx` 直接画满高），而它同样是 **13 行 = 531.32 < 573.96** ——
   原版 `Show():929-932` 一样会收缩 ⇒ **同一个缺陷、另一个文件**（不在本代理白名单，未改）。建议按铁律 21 横切，另派一份。
3. **`4-01` 的行盒差异未在本机实测**：我按 prefab 事实把面换成 Pragati，但「换面后本窗各站点自适应收敛值是否与
   原版逐颗一致」需要跑一次 `SettingsScene.Run` 的「全窗字号扫描」那几段看读数（本代理不跑 Unity）。
   预期不红：那些断言按 `lb.ActualFace` 折算、`FontSizeBase` 是**按写入时的面**折出来的 ⇒ 面变、px 读数不变。
4. **`4-05` 的 12 项 / 13 项口径差**：B138 §4 `4-05` 写的原版值是「12 项 = 491.30」，而**现读**
   `Loc.Languages.Length = 13`（`Core/Loc.cs:212`，B106_W15 已按原版下拉 13 项订正）⇒
   本窗实际收缩到 **532.17**。两者**不是矛盾**（同一条公式的不同 n），断言两档都断。

---

## 六、`4-10` 其余落点（⛔ 不归本代理 —— 交调度台）

| 处 | 现读落点 | 原版档 | 建议归属 |
|---|---|---|---|
| 主菜单左导航钮 | `Shell/MainMenuRuntime.cs:1285` 附近（`NavButton` 那族文案，无 `SetVAlign`） | Capline | **W5**（`MainMenuRuntime.cs` 独占） |
| 卡组编辑「行卡名」 | `Deck/DeckRuntime.cs:2081`（`rowNmLb`，仅 `SetAlignLeft + SetAutoFitBox`） | Capline | **W17**（`DeckRuntime.cs` + `DeckScene.cs`） |
| END TURN | `Battle/BattleDriver.cs`（判据 `:29152`；现读 `_endTurnLabel` 在 `:1916` 声明，无 `SetVAlign`） | Midline | **W22**（`BattleDriver.cs` 独占热点） |
| ManaText 数字 | `Battle/BattleDriver.cs`（判据 `:29062-29066`；现读那一段在 `:29063-29075`） | Midline | **W22** |
| ~~战斗 HUD 双方标题~~ | `Battle/BattleDriver.cs:30057-30058` | Midline | ✅ **已接**（判据里的「1/6 已接」就是它） |

> ⚠️ 行号取自 `判据/B141_W129_对账_UI动画.md` §4 `4-10`（2026-11-04 现读）；文件今日仍在飞，
> **动手前现读一次**（`grep -n` 那个符号）。

---

## 七、顺手发现（⛔ 未改，只报）

1. **`Battle/SettingsPanel.cs` 有一份一模一样的 `4-05`**（见 §五·2）—— 原版语义相同、缺陷相同。
2. **`4-13` 的字体样式缺调用点**（见 §五·1）—— 判据已齐，`Label.SetFontStyle` 已有。
3. `SettingsScene.cs` 顶部那段「克隆体与 `Template` **同格**」的注释（`:1849` / `:1861` 附近）在本批之后**只对左/顶/右成立**
   —— 已就地改写，⛔ 别再按「同格」推底边。
