# W_战斗断言宿主 —— `A991` 三处断言宿主 + `A985⑧` 三个调用点（执行写手）

白名单只两个文件：`Editor/BattleScene.cs` · `Battle/BattleDriver.cs`（另建本报告）。上游：`W_战斗侧3.md` §1（A991 逐行改法 +「缺那条正例」）· `W_引擎族3.md` §③（`TermKey` 已落）。

---

## ① `A991` —— 「跳过教程」钮搬进设置面板后的三处宿主

### 1) 亮的判据（`:14718-14752`，原 `:14675`）

- **改前（原文）**：`Check(driver.TutorialView.SkipVisible, "★ 教程局：**跳过钮**亮着（原版 `SkipTutorial Button`）");`
- **改后**：读**真身** `driver.Settings.SkipTutorialShown`，**四态一组** —— ① `!Shown`（面板关着 ⇒ 钮不画）；② `Show()` ⇒ `Shown`（开面板 ⇒ 画出来）；③ `Built && Text == Loc.T(SkipTutorialTerm)`；④ `Hide()` ⇒ `!Shown`（收尾）。
  🧨 改坏法 = 删掉 `SettingsPanel.SetActive` 里那句 `_skipBtn.SetActive(on)` ⇒ ② 红（两态同值）。⚠️「此刻面板是关的」是**自然状态**：`:9087` 那条 `!sp.Visible` 钉着，`:13203/:13668`（A462）开完放回。
- 🔴 **灭自证（结构）**：`TutorialView.transform.Find("SkipTutorial Button") == null`
  **且** `Settings.transform.Find("settings_skip_tutorial") != null`。
  只断「`Shown` 跟着面板走」**不够**：把钮建回 HUD 子树、再把 `Shown` 改读 HUD 那颗，**两边一起改仍全绿**；
  这一条钉的是**它在哪棵树里**（原版那颗不在 HUD 上，父链 = `…/BattleSettingsPanel/Bottom buttons`）。

### 2) 点的命中（`:14914-14928`，原 `:14837-14840`）

- **改前**：`Check(ov.ClickSkipAt(ov.SkipWorldPos), …)` + 偏 3 单位的反例。
- **改后**：读真身 `driver.Settings`（`spSkip`）：`Show()` → `HitSkipTutorial(SkipTutorialWorldPos)` 命中；
  `+ (3,0,0)`（≈324 px）不命中；再 `Hide()` → **同一坐标也不命中**（判据里 `!Visible` 那道闸，与 `HitResign` 逐字同形）。
- 🧨 改坏法 = 删掉 `HitSkipTutorial` 里 `if (!Visible …) return false;` ⇒ 第 3 条红。
- ⚠️ 这三条走的是**与真实输入同一条判定**（`SettingsClickAt` 用的就是它），不是自写一份矩形。

### 3) `SkipAllowed` / `TrySkip` / `SkipClickedByPlayer`（`:14930-14938`）

**一个字没动、照旧应绿**（`W_战斗侧3` 那张表第 3 行判的也是「不变」）。

### 4) 🆕 新增那条正例（`:14952-14988`，插在 §⑩ 之后、⑭ 块收尾 `}` 之前）

走**产品那条链** `driver.SettingsClickAt(SkipTutorialWorldPos)`（真鼠标那条），**两态 = 只翻 `minTimeBeforeSkip` 那道闸**：

- **档①**`BeginTutorial(0)` + `SetShownForTest(0f)` + `Show()` ⇒ 被吃掉 · **`!Settings.Visible`（先关窗）** · **`!Ctx.IsOver`**（🧨 改坏法 = 删掉 `SkipTutorialFromSettings` 里 `if (ov != null && !ov.TrySkip()) return;`）。
- **档②**`BeginTutorial(0)` + `SetShownForTest(2f)` + `Show()` ⇒ 还是先关窗 · **`Ctx.IsOver && Players[0].Warlord.Health == 0`** · **`Winner == 1 - MySideForTest + 1`（对侧胜）**。
- 「先关窗」那一半**两档都断**（原版那两行的**第一行** `CloseWindow`，⛔ 别把先后倒过来）。

### 5) `minTimeBeforeSkip` 那处真偏离 —— **已在注释里如实标注**（`:14982-14988`）

`minTimeBeforeSkip` 是 `TutorialTipScript` 的字段（管**提示自己**何时能消），**原版那颗钮没有这道闸**；
我们把它接在 `TrySkip` 上 ⇒ **一处真偏离**（影响 ≈ 0：设置窗得先点齿轮，那时 `_shownFor` 早过 1 秒）。
注释写明「⛔ 别为了对齐原版把它删掉」+「要删得**两处一起改**」（`TutorialOverlay.TrySkip` + 这里 §⑨/§⑩-b 的断言）。**没删。**

### 6) 三个转发口删了没有 —— ❌ **没删，删不了**（＋转交）

`TutorialOverlay.cs` 的 `SkipVisible` / `SkipWorldPos` / `ClickSkipAt`（+ `SkipPanel` 那一格）
**不在我的白名单** ⇒ ⛔ 没动。本批改完后它们**全仓零代码调用**（`grep` 只剩注释）⇒ **可安全删**：
📌 请**主对话**（或另派一个拿 `TutorialOverlay.cs` 的写手）删这三口 + `SkipPanel` 字段 +
`BattleDriver.cs:7721-7728`（`BuildTutorialView` 那段注释 + 注入）与 `:11348`（`BuildHud` 里那次幂等注入）。

## ② `A985⑧` —— 三个调用点

**落点现读**（`grep 'RuleCodes.Describe'`，全文共 5 处；我只动了「玩家动作被引擎拒」那 3 处）：

| # | 行（改后） | 改前 → 改后 |
|---|---|---|
| ① 收集灵魂石（`SetHint`） | `BattleDriver.cs:6980` | `SetHint(RuleCodes.Describe(rc))` → `SetHint(HintForCode(rc))` |
| ② `DoPlay` 落位被拒 | `:6407` | `…（{RuleCodes.Describe(code)}）…` → `…（{HintForCode(code)}）…` |
| ③ `DoResolve` 打不出去 | `:7508` | `…：{RuleCodes.Describe(code)}` → `…：{HintForCode(code)}` |

- ⛔ **没动**另两处（`:635` / `:717`，**联机重放对账**的 `LogError`）：那不是「出牌被拒」，换键名是退化。
- ⚠️ **如实标**：三处里**只有 ① 会写到玩家看的提示行**（②③ 是 `Debug.LogError/Log`）。仍三处都改 ——
  口径按简报「出牌被拒那三个调用点」，且**三处同源同判**（只改一处 = 两条路不一致）。
  📌 若主对话认为 ②③ 该退回 `Describe`（保开发日志的人话），**退回是一行的事**（判据就是那三行本身）。

### 新助手 `BattleDriver.cs:7535-7568`

```csharp
public static string HintForCode(int rc) {
    string key = RuleCodes.TermKey(rc);
    return key != null ? Loc.T(key) : RuleCodes.Describe(rc);   // ← 兜底必须留（24 条键今天都没值）
}
```
- 🔴 **没按字面写成 `Loc.T(RuleCodes.TermKey(rc)) ?? RuleCodes.Describe(rc)`**（那是 `W_引擎族3` §③ 与
  `RuleCodes.cs:90` 给的形状）—— **那一句有个洞**：`Loc.T` **从不返回 null**（空键给 `""`、**缺键给键名本身**，
  `Core/Loc.cs:902-933`）⇒ 无键的码会拿到 **`""`**（提示行**全空** = 静默失败），而 `??` 那一半**永不触发**。
  ⇒ 改为**先判键、再取值**。
- ⚠️ **与同族 `HandFullText()`（A985⑦，同一文件）写法不同**，已写进 `HintForCode` 的 doc：那条走
  `Loc.HasEntry` 兜底，本条**只判 `TermKey` 非 null**（键在、值不在 ⇒ 印**键名**）。两条都对，差别只在
  「今天没值的那两条键印什么」；⛔ 别把这一处单方面改成 `HasEntry`（有断言钉着当前口径）。

### 断言（`BattleScene.cs` §3b · `:3002-3047`，紧跟「3. 费用约束」之后）

- ① 有键那条：`ErrCost` 键 = `Battle/Tips/NotEnoughMana`；`HintForCode(ErrCost)` **非空**。
- ① **灭自证（结构）**：`Loc.HasEntry(键) || Loc.HasWarnedMissing(键) || Loc.MissingCount > missBefore`
  —— 钉「**真的问了词条层**」。⛔ 不断 `!= Describe`（**值比**：原版 `NotEnoughMana` 的译名哪天与我们兜底**撞字**
  就假红）；⛔ 也不断 `== Loc.T(键)`（**同义反复**，与实现同一个表达式）。🧨 改坏法 = 换成直接 `Describe` ⇒ 红。
- ② 无键那条（`ErrSlot`，另断 `TermKey == null` 作前提）：`HintForCode == Describe` **且非空**，
  外加 `Loc.MissingCount` **不变**（一次都没碰 `Loc`）。
  🧨 改坏法（**专抓那个洞**）：字面写成 `Loc.T(TermKey(rc)) ?? Describe(rc)` ⇒ 无键时得空串 ⇒
  ①「== Describe」与 ②「MissingCount 不变」**两条一起红**，而真机上提示行**全空**。

## 类型检查 · 行尾 · 没查清 / 停手

- **类型检查**：`TMPDIR=/tmp/wf_bs bash 工具/typecheck.sh` ⇒ **运行时 0 错 · 编辑器 0 错**（最后一次跑覆盖全部改动）；
  期间**没有**「错在别人在写的文件上」那种情况（本批无并发写手动我这两个文件）。
- **行尾**：两文件**仍是纯 CRLF**（`BattleDriver.cs` CRLF 13232 / LF 13232 · `BattleScene.cs` 18020/18020 ⇒ 相等 = 没翻）；
  ⛔ 全程用 Edit 工具，**没用 `sed -i`**。
- **改动量**：`BattleDriver.cs` +40/−3（33 行是助手 + doc）；`BattleScene.cs` +130/−3。
  ⚠️ `BattleScene.cs` 的 numstat 里另有 `:2610/:2622/:2668/:5986/:13898` 五处**不是我改的**
  （行号→符号的注释重构，改动前就在工作区，mtime 07:15）。
- 🔴 **本批断言没跑过 Unity**（红线：写手不许 `-executeMethod`）⇒ **请主对话收口时跑 `BattleScene.Run`**。
  残余风险两处，都**只能靠跑一次消掉**：① §⑭ 第 1 条断「面板此刻是关的」—— 依据是 `:9087` 与 `:13668`
  两处把状态放回（**静态读出来的**）；② `settings_skip_tutorial` 这个**节点名**能否被 `transform.Find` 找到 ——
  依据 = `MenuDraw.Nine` 的 `name` 参数（`SettingsPanel.cs:546`，**没跑过**）。
- **转交主对话**：① 上述两处残余风险（跑一次即消）；② **`TutorialOverlay.cs` 那三个转发口要删**（§①·6）；
  ③ `:6407` / `:7508` 两处**开发者日志**是否退回 `Describe`（§②表 ②③），⚖️ 请裁；
  ④ 上游 `W_战斗侧3.md`「顺手发现」那条（`Skip/Resign` 两颗钮 `Simple + preserveAspect` 与 `MenuDraw.Nine` 冲突）**不属本批**。
