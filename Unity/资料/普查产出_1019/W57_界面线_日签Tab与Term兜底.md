# W57 界面线 · 日签 Tab `wrapPx` + 事件弹窗词条兜底（UI-7）报告

> 会话：2026-10-19 第五十七会话 · 执行代理 UI-7 · 口径 = **只做不验**（不跑 Unity / 不起 player / 不跑 `*.Run`；只跑秒级 typecheck）。
> 本文件是执行代理报告。**正本由主对话合并**，本文件不代替正本。
> 判据来源 = `资料/普查产出_1019/W57_界面线_日签与事件弹窗残尾.md` **§5（一·C 末段）与 §6（四·1）**。
> 白名单 = `Shell/DailyStreakTab.cs` · `Shell/BaseEventPopup.cs`；**只改了这两个文件**。

---

## 一、两件结论

### A. `DailyStreakTab` 的说明句补 `wrapPx`（已改）

- **原值**（改前 `Shell/DailyStreakTab.cs:103-105`）：
  ```csharp
  // 说明句（复用 `DailyData.StreakInfoText()`，同样「我们挑的」）
  _win.Text(_root, DailyData.StreakInfoText(), InfoR.x1, InfoR.x2, InfoR.y1, InfoR.y2,
            6, Grey, "Daily Streak Info", 36f, face: TmpFont.Face.LatinCardBody);
  ```
  `MainMenuSubmenuWindow.Text` 的 `wrapPx` 默认 `0f` ⇒ **不调 `SetWrapWidth`**；而 `Battle/Label.cs` 的 `BuildTmp` 对单行标签硬写 `NoWrap`
  （判据 = `Label.SetWrapWidth` 的方法头，`Battle/Label.cs:486-518`）⇒ 官方英文长串（约 103 字符）**不折行、一路画出框外**。
- **新值**（现读 `:114`）：`... 36f, wrapPx: InfoR.W, face: TmpFont.Face.LatinCardBody);`
  - `InfoR`（`:53`）= `new PxRect(600f, 700f, 1650f, 760f)`；`PxRect` 构造 = `(x1, y1, x2, y2)`（`Core/UguiRect.cs:30`），`W = x2 − x1`
    （同文件 `:31`）⇒ **`InfoR.W` = 1050 px**（与本条任务的「框宽仅 1050px」一致）。
  - `wrapPx` 的语义 = `lb.SetWrapWidth(LayoutSpace.Px(wrapPx))`（`Shell/MainMenuSubmenuWindow.cs:384-386`）→
    `TmpFont.SetWrapWidthRect`（`textWrappingMode = Normal` + `sizeDelta.x = width`，`Core/TmpFont.cs:524-529`）
    —— 与连登弹窗那两颗原版 TMP 的 `m_TextWrappingMode = 1` 同档（判据/改动 → `Shell/DailyStreakPopup.cs:487-493` · `:605-613`）。
- **注释订正（铁律 5，保留更正痕迹）**：
  - `:105-112`：原来写「同样『我们挑的』」——**已过期**（该串自 UI-6 起 = 官方词条 `Rewards Menu/DailyStreak/Help`），
    就地写成「已过期 + 判据指回 `DailyData.StreakInfoText()` 的 doc」。
  - 类注释 `:26-29`：原写「文案复用 `DailyData` 里那两条**已经标着「我们挑的」**的串」——对 `StreakInfoText` 已不成立；
    改为「`StreakCurrentLabel` 仍是我们挑的 · `StreakInfoText` 自 2026-10-19 起 = 官方词条」，并标注原文已过期。

### B. `BaseEventPopup.ResolveTerm` 加官方 13 语言全表兜底（已改，现有返回值零变化）

- **原值**（改前 `:459-464`）：`bulk（Loc.HasBulk → Loc.BulkOr）` → `手写 Table（Loc.HasEntry → Loc.T）` → **空串 + 出声**。
- **新值**（现读 `:463-475`）：在上述两档之后、告警之前插入：
  ```csharp
  string official = Loc.OfficialCurrent(term);
  if (!string.IsNullOrEmpty(official)) return FillArgs(official, which, term);
  ```
  告警文案从「两处表里都没有」改为「三处表里都没有（bulk · `Loc.Table` · 官方 13 语言全表）」。
- **只读口现核**：`Loc.OfficialCurrent(string key)` 存在（`Core/Loc.cs:3003-3014`），签名为 `public static string OfficialCurrent(string key)`；
  口径 = 中文档取 `zh` 列（该列空 ⇒ 回退 `en` 列）· 英文档取 `en` 列 · **其余 10 档恒 `null`** · 缺键/两列都空 ⇒ `null`
  （不出声、不记账）。⇒ 不需要新造一层，也不需要改 `Core/Loc.cs`。
- **顺序**：**保持**原顺序「bulk → `Table`」。⛔ 没有改成先走 `Loc.T` —— `T()` 是 `Table` 优先（`Core/Loc.cs:3334-3336`），
  对「两处都有值」的键会翻转取值。新加的一档**只接住「前两处都没有、本来会留空」**的那一类。
- **哪些现有返回值会变？—— 实测：一个都不变。** 判据（现读）：
  1. `ResolveTerm` 的入参只有 `TitleTerm` / `DescriptionTerm`，两者 `private set`、只在 `BaseEventPopup.Create` 里赋值
     （`Shell/BaseEventPopup.cs:201-203` · `:273-274`）；唯一 `Create` 调用点 = `LiveOpsEventWindow.OpenHelp`
     （`Shell/LiveOpsEventWindow.cs:1037`）。
  2. 词条来源 = `LiveOpsLocalCatalog` 的 `L(...)` 标签表 / `HelpTitleTerm` · `HelpDescriptionTerm` / `SkirmishEventWindow`
     覆写的两条。用正则把 `LiveOpsLocalCatalog.cs` 里所有 `"Event_Description/*"` 字面量取出共 **16 条**，
     逐条查 `Resources/official_names.json` 的 bulk：**16/16 命中**（bulk 里 `Event_Description/*` 共 **144 条**）。
     `SkirmishEventWindow` 的两条（`Event_Description/ExpansionPass/Title` · `.../GeneralHelp`）也在其中。
  3. `bulk` 命中即早退（`:463`），走不到新加的那一档 ⇒ **16 个键今天全部走老路，返回值逐字节不变**。
- **是否「加了会更差」？—— 否**：
  - 语档：中/英档取官方 `zh`/`en`（与 `T()` 在 `N71` 之后对这两档的官方兜底同一条口、同一口径）；其余 10 档 `OfficialCurrent` 恒 `null`
    ⇒ 那 10 档行为**与改前完全一致**（非回归）。若要覆盖那 10 档，唯一现成的口是 `Loc.Lang13Current`——
    它缺键时会出声（虽按 `_warnedLangFallback` 去重），本笔按调度口径**只加 `OfficialCurrent`**、没链它。
  - 与 `Loc.cs` doc 的关系：`OfficialCurrent` 的 doc 写着「⛔ 除此之外别再开第三条显示取值路」。本笔**不是**开第二条取值逻辑
    —— 复用的就是 `T()` 自己在中/英档用的那一个口（`Core/Loc.cs:3381-3384`），且只用在「本来会留空」的最后档；
    ✅ 判断 = 可以加。

---

## 二、改动清单（全部在白名单内；行号 = 改后现读）

| 文件 | 行 | 改动 |
| --- | --- | --- |
| `Shell/DailyStreakTab.cs` | `:26-29` | 注释订正：类头「两条都标着『我们挑的』」→ 分清哪条仍是自拟、哪条已是官方词条（保留更正痕迹） |
| `Shell/DailyStreakTab.cs` | `:105-113` | 注释订正 + 判据：原「同样『我们挑的』」已过期；写明为何必须传 `wrapPx` |
| `Shell/DailyStreakTab.cs` | `:114` | 🔴 **行为修复**：`_win.Text(...)` 补 `wrapPx: InfoR.W`（1050px） |
| `Shell/BaseEventPopup.cs` | `:410-414` | 注释：`ResolveTerm` 取值来源从「两处」改为「三处（依次）」，并订正 `Event_Description/*` 条数 111 → **144**（现读） |
| `Shell/BaseEventPopup.cs` | `:463-475` | 🔴 **兜底新增**：第三档 `Loc.OfficialCurrent(term)`（顺序仍为 bulk → Table → 官方全表）；告警文案改「三处」 |

- 未触碰：`Core/Loc.cs`（只读现核）· `Shell/DailyData.cs` · `Shell/DailyStreakPopup.cs` · `Shell/LiveOpsLocalCatalog.cs` ·
  `Editor/**` · `RuleEngine/**` · `Resources/*.json`。
- 行尾：两个文件改前改后均为**纯 LF**（二进制实读 `CRLF 0`）；`git diff --numstat` = `DailyStreakTab 12/3` · `BaseEventPopup 25/7`
  （后者含 UI-6 已落盘的 8/3 注释改动；本笔在其上 +17/+4）—— **没有整篇翻**。

---

## 三、没查清的部分 / 残留

1. **A 的视觉结果（必须真机）**：官方英文句（约 103 字符）在 **1050px / 36px** 下折成几行、折行后是否溢出 `InfoR` 的 60px 高框，
   本波**不验**（口径 = 只做不验）。已知与原版的差别：**本页是自建页**（原版没有这一页），`InfoR` 本身是「我们挑的」，
   原版可照的只有连登弹窗那两颗 TMP（1825px 框）。
2. **水平起点会随折行变化**：`Label.BuildTmp` 的默认对齐是 `TextAlignmentOptions.Left`（`Battle/Label.cs:635`）；
   不折行时 `sizeDelta.x` 未写（文字以锚定中心为起点向右铺），补 `wrapPx` 后折行框以中心为枢轴、宽 1050 ⇒ 文字左缘落在 `InfoR.x1`。
   这是「框语义」应有的结果，但**与改前的视觉起点不同**，需真机并排确认一次。
3. **B 的非中/英 10 档**：`Loc.OfficialCurrent` 恒 `null` ⇒ 那 10 档下「只在官方全表」的键**仍留空**（与改前一致、非回归）。
   是否要覆盖那 10 档 = **待主对话裁**（候选口 `Loc.Lang13Current`，但它缺键会出声；本笔未加）。
4. **`Core/Loc.cs` 里 bulk 的族/总数注释已过期（白名单外，只报不改）**：`Core/Loc.cs:3050-3053` 写
   `Titles 368 · Avatars 419 · Achievements 198 · Cardbacks 157 · PrebuiltDecks 74 · Event_Description 111`、共 **2,758 条**；
   **2026-10-19 现读** `Resources/official_names.json` = **3,156 条（键唯一）**，各族
   `Card_Name 1198 · Avatars 498 · Titles 459 · Modular_Description 342 · Achievements 212 · Cardbacks 181 · PrebuiltDecks 95 · Event_Description 144 · Card_Trait 27`。
   ⇒ 「2,758 / 111」等数字全部过期（本笔已在 `BaseEventPopup` 的注释里把 111 订正为 144；`Loc.cs` 未动）。
5. **`Loc.OfficialCurrent` 的 doc 与我们这处用法**：doc 说「除此以外别再开第三条显示取值路」，本笔把它用在**最后兜底**
   （只接住本来会留空的一档），复用它与 `T()` 同一份实现 ⇒ 判断为不冲突；若主对话口径更严，可回退这一档（行为今天的 16 个键零影响）。

---

## 四、typecheck 结果

- 命令：`TMPDIR=/tmp/wf_ui7 bash d:/4/Unity/工具/typecheck.sh`
- **运行时错误数 0 · 编辑器错误数 0** ✅
- 未出现其它代理的瞬时红（本波时段无他人文件报错）。
