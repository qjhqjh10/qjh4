# W · 标题档（`A971` 第 11 处 + 三扇 `Window Title` 垂直档漏网）

**范围**：只动了 4 个文件（`Shell/{TutorialModePopup,LiveOpsEventWindow,EnergySinglePlayerOnlyEventWindow}.cs` + `Editor/MainMenuScene.cs`）。
`Editor/RewardsScene.cs` **没动**（三扇的断言宿主都在 `MainMenuScene.cs`，不必再开第二个宿主）。
`Shell/MenuWindowBase.cs` **没动**（`Spec.TitleVAlign` 那个字段本来就有，共件不用改 —— 见下）。

---

## 一、`A971` 第 11 处（纯注释）

**文件**：`Shell/TutorialModePopup.cs`（`Build()` 的注释块里，原来在文件头 `:57`）

- **原句**：`… 聊天三档含 `RadioChat` / 音效 / 督军两拍落场 / 五个 `hide*`）与 **`A939` 胜利脚本 13 条**。`
- **新句**：`… 聊天**五档**含 `RadioChat` / 音效 / 督军两拍落场 / 五个 `hide*`）与 **`A939` 胜利脚本 13 条**。`
  ＋ 就地补一段留痕（判据五档：`50 PlayerChat` · `55 PlayerChatBig` · `60 AiChat` · `65 AiChatBig` · `90 RadioMessage`）。

**没误伤**：本文件里其余「三档」只剩一处 `LiveOpsEventWindow` 的**渲染队列梯子**描述（不在本文件）；
本文件内的「三颗气泡」类数量词一个没碰。

✅ **全仓 `.cs` 里 `聊天三档` 现在零命中**（`grep -rn 聊天三档 Unity/MyGame/Assets/` 只剩 `资料/` 下 6 处**文档**，
不在本代理白名单内 ⇒ 由调度台处置）。⇒ 本条断言的「本处是全仓最后一处 `.cs`」成立。

---

## 二、三扇 `Window Title` 垂直档（← 这是真差异，铁律 11）

**判据（亲读原版，未复核）**：`bundle_menus_assets_all` 里 **8 颗 `Window Title`** 的 TMP 逐颗现读、逐值相同 ——
`m_VerticalAlignment = 8192 (Capline)` · `m_enableAutoSizing 1` · `m_fontSizeMin 18.0` · `m_fontSizeMax 67.55`
· `m_fontSizeBase 36.0` · `m_characterSpacing 5.0` · `hA 1 (Left)`。

**共件不用改**：`Shell/MenuWindowBase.cs` 的 `WindowHeader.Spec` **早就有** `public Label.VAlign? TitleVAlign;`
（`:681`），`WithBackButton` 里 `if (s.TitleVAlign.HasValue) MenuDraw.SetVAlign(title, …)`（`:766`）
——**排在 `AlignLeft` 之后**（与 `DailyStreakPopup` 原来的次序逐句相同）。⇒ 三扇各补一行即可，**零共件改动**。

| 扇 | 改动 | 行 |
|---|---|---|
| `TutorialModePopup` | `Spec` 补 `TitleVAlign = Label.VAlign.Capline,` | `Shell/TutorialModePopup.cs`（`BuildHeader` 的 `TitleFitW/H` 之后） |
| `LiveOpsEventWindow` | 同上（**遭遇战 / 排位共用这一个 `BuildHeader`**） | `Shell/LiveOpsEventWindow.cs`（同位置） |
| `EnergySinglePlayerOnlyEventWindow` | 同上 | `Shell/EnergySinglePlayerOnlyEventWindow.cs`（同位置） |

三处都**只加这一档**：`TitleMode`（`FitAfterSpacing`×2 / `FitBeforeSpacing`×1）· `TitleFitW/H` ·
`BackButtonStyle` · `WingName` · 队列一个字节没动（**`A944`/`A834`/`A967` 的成果未被碰**）。
各自的 `BuildHeader` remarks（「本窗这一档的差异」清单）**各补一条**说清「原文没列这一条 ⇒ A866 时走的是
`Middle`，与原版不符」；`TutorialModePopup` 那句**「没有 `TitleVAlign` 那一档」已就地改掉**（铁律 5）。

### 断言（3 条，全在 `Editor/MainMenuScene.cs`）

新增助手 `CheckTitleVAlign(string what, Transform winRoot)`（放在 `CharSpacingOf` 之后）：
取 `FindChild(winRoot,"Window Title")` → `GetComponentInChildren<Label>(true)` → 断 `lb.VAlignTier == Label.VAlign.Capline`。
**期望值 = 原版那个 int 对应的档**（`8192 Capline`），⛔ 不是我们的常量。

| 断言名 | 宿主里的调用点 | 断的是 |
|---|---|---|
| `教程模式窗` | `tut`（`TutorialModePopup`，原 `Window Title` 文案那条之后） | `VAlignTier == Capline` |
| `遭遇战窗` | `sk`（`SkirmishEventWindow.LastOpened`，原 `characterSpacing` 那条之后） | 同上（这条路走的就是 `LiveOpsEventWindow.BuildHeader`） |
| `能源活动窗` | `en`（`WindowsManager.OpenEnergyEvent()`，`Header Back Button` 几何那条之后） | 同上 |

**两态**：传了 `TitleVAlign` ⇒ `Capline` 绿 / 不传 ⇒ `Label._vTier` 停在其出厂档 `Middle` ⇒ 红（实测值会印 `Middle`）。
**改坏法（写进了报文）**：删掉某一扇 `Spec` 里那行 `TitleVAlign = Label.VAlign.Capline,` ⇒ 该窗那条红。
**前提守卫**：节点不在 / 没有 `Label` / `!lb.CanRenderChinese`（点阵后端下 `Label.SetVAlign` 只出声、**不写档**）
⇒ 各一条显式**红**、⛔ 不静默跳过（同 `CheckHAlign` 的口径）。
**取节点写法**：照 `MainMenuScene.cs` 里读 `tut` 那颗 `Header Background (1)` 的同族写法（`FindChild` 深搜）。

---

## 三、类型检查

`TMPDIR=/tmp/wf_title bash d:/4/Unity/工具/typecheck.sh` —— **运行时 0 错 / 编辑器 0 错**（改完两轮都跑了，含最后一次）。
**没跑任何 Unity 批处理**。
**行尾**：四个文件 `git diff --numstat` = 8/0 · 25/6 · 14/2 · 380/38（后两个含**别人早先就有的未提交改动**：`LiveOpsEventWindow.cs` 里是 `A967` 删 `WingName`；`MainMenuScene.cs` 会话开头就已是 `M`），
`b.count(b'\r\n')==0`、`b.count(b'\n')==行数` ⇒ **四个文件都是纯 LF、一个都没被翻**（⛔ 全程没用 `sed -i`）。

---

## 四、没查清 / 停手的地方

1. 🔴 **`Shell/MenuWindowBase.cs:685` 那句注释已过期（本文件不归我 ⇒ 没改，请调度台转派）**：
   `Spec.WingName` 的 doc 仍写「⚠️ `LiveOpsEventWindow` 今天沿用 `MenuDraw.Nine` 的缺省名 **`"Nine"`**（见 §5 的差异清单）」
   —— **`A967` 把那颗 `WingName = "Nine"` 删了之后这句不成立**。
2. ⚠️ **没有加「渲出来那一块」的物理判别式**（只有 `Label.VAlignTier` 这一口）。
   理由：`Capline` 与 `Middle` 的**落点差**是一个**已知量**（`Label.OrigInkCenterPx(Middle) = (c/2−(a+d)/2)/p × F`
   = `0.05263 × 67.55px ≈ 3.56px`），**但「Capline 下渲出来的墨心离框心还有多远」这一格判据是空的**
   （取决于我们字体资产的 `capLine/ascentLine`，只在运行时读得到）⇒ 写一个容差就是**发明判据**，
   所以只写档位、并把差距登记在此。要补得先跑一次探针量 `Label.VOffsetWorld` 的两态读数。
3. ⚠️ `资料/普查产出_1018/S5_A866窗头收口.md` §2/§4 那份「逐格对照表」里，
   三扇的 `TitleVAlign` 一格现在**已过期**（该文件不在我的白名单，没改）。
4. ⚠️ `LiveOpsEventWindow` 那一族**排位窗（`RankedEventWindow`）走的是同一个 `BuildHeader`** ⇒ 一并被修好，
   但断言只挂了遭遇战那一支（`sk`）—— 同一条实现、不必两条断言（若要求「每扇一份」请另行派单）。

**顺手发现（只报告、没改）**：`MainMenuScene.cs` 里**全仓只有一条**读 `Header Background (1)` 的断言
（`TutorialModePopup` 那颗）；`LiveOpsEventWindow` 与 `EnergySinglePlayerOnlyEventWindow` 的尖角节点名
**至今没有断言盯着**（`A967` 的成果靠人工核）—— 那是另一笔账。
