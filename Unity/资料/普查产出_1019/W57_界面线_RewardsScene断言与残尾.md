# W57 · 界面线 · RewardsScene 断言与残尾（TOOL-5）

> 2026-10-19 · 收尾执行代理 TOOL-5 · 只做不验（未跑 Unity / 未起 player / 未跑 `*.Run`；只跑秒级 typecheck）
> 白名单 3 文件：`Editor/RewardsScene.cs` · `Editor/NetSelfTest.cs` · `Shell/DeckSelectionPopup.cs`。⛔ 未动 git、未改正本、未删文件。

## 一、三件逐项结论

### A · `Editor/RewardsScene.cs` 两条必红断言（锻造 `Help Icon` tooltip）

**改前**（UI-3 报告 `W57_界面线_ShopForgeTooltip与滚动.md` §一·B 判定「本批唯一已知红」）：

- `Editor/RewardsScene.cs:4412`（旧行号）：
  `CheckTrue(!string.IsNullOrEmpty(ForgeTab.HelpTipBody) && ForgeTab.HelpTipBody.Trim() == "", …)` —— 钉「空白占位」。
- `Editor/RewardsScene.cs:4427`（旧行号）：
  `CheckTrue((Tooltip.ShownBody ?? "").Trim() == "", …)` —— 钉「面板正文空」。
- 判据现读：`Shell/ForgeTab.cs:568` `public static string HelpTipBody { get { return Loc.T("MainMenu/Forge/Help"); } }`
  ⇒ 两条**必红**（与 UI-3 报告一致）。

**改后**（现行号）：

- `:4410` 测试侧**独立取判据**：`string helpOfficial = Loc.T("MainMenu/Forge/Help");`
- `:4411-4413` 前提：`Loc.HasEntry("MainMenu/Forge/Help") && !string.IsNullOrEmpty(helpOfficial)`
  （缺键时 `Loc.T` 回的是**键名本身**，不先断前提的话下面那条等于没验）。
- `:4414-4418` 实现值：`!string.IsNullOrEmpty(ForgeTab.HelpTipBody) && ForgeTab.HelpTipBody == helpOfficial`（非空 + 等于官方词条值）。
- `:4429-4433` 面板值：`(Tooltip.ShownBody ?? "") == ForgeTab.HelpTipBody && (Tooltip.ShownBody ?? "") == helpOfficial`。

**灭自证（结构性，不是同义反复）**：期望值不是被测实现上的常量，而是测试侧从官方表取的 `Loc.T(键)` ⇒

| 改坏法 | 哪条红 |
|---|---|
| `HelpTipBody` 改回 `" "`（空白占位） | `:4414` |
| `HelpTipBody` 写成别的串 | `:4414` |
| `Show()` 里传错串 / 正文回空 | `:4429` |
| **两边一起**改回 `" "` | `:4414`（`helpOfficial` 仍是官方非空值）· `:4429` 第二半 |

- 词条值出处：`Core/Loc.cs:2745`（`MainMenu/Forge/Help`，中英两列都有值：ZH「在你的收藏中升级张牌，通过每个军队的锻造厂轨迹进行进步并获得大奖。」/ EN `Upgrade cards in your Collection to progress through the Forge track for each faction and earn great rewards.`）。
- 同段过期注释 `:4403-4409`（「词条表在远端 CCD、本地没有 / 空白占位才是裁定过的形态」）已就地订正（铁律 5）。

**顺手 grep（`Editor/` + `Shell/` 的 `HelpTipBody` / `ForgeTip` / 「正文空」）**：

- 按「正文空」写死的**断言**只有上述两条（本文件），均已改；`Editor/ShopScene.cs:2220-2222 / :2279-2280` 已由 UI-3 改成同形断言（「非空 = 官方词条」+「`ShownBody == TipBody`」）。
- ⛔ **白名单外、仍写着相反说法的注释/运行日志（未改，逐条列此）**：
  1. `Shell/ForgeTab.cs:492-494` —— 构建期 `Debug.Log` 仍说「**词条表在远端 CCD、本地一个 value 都没有 ⇒ 正文留空、不自己编一个字**（见 `HelpTipBody`）」，与 `:568` 现实现相反。
  2. `Shell/BoosterInfoPopup.cs:44`（文件头）同一句过期；另 `:687-692` `HintGaps()` 的**运行期出声**仍说「`Tooltip` … **正文是空的** … 词条表在远端 CCD、本地一个 value 都没有」，与 `:524` 现实现（`Loc.T`）相反。
  3. `Core/Loc.cs:2777` —— 注释「副作用：其中 `ForgeTab.HelpTipBody = " "`（**故意留空**，配一条 `ShownBody.Trim() == ""` 的断言）」已过期（实现与断言本波都已改）。
  4. UI-3 报告 §四 提到的 `Shell/MissionsTab.cs:1578`：**现读该文件已无 `HelpTipBody` 字样**（grep 0 命中）——那条过期说法现已不存在（或行号已漂、未再定位到）。

### B · `Editor/NetSelfTest.cs` 的 `Pump:213` / `Pump:213-215`

**改前**（旧行号）：`:1686` `Pump:213` · `:1687` `Pump:232` · `:1693` `Pump:213-215`；另有 `:1684` 的 `NetTransport.cs:220` 也是漂过的行号。

**现核**（`Net/NetSession.cs`）：

- `Pump()` 方法起点 `:195`；
- **计数读**（握手边沿那一支）：`:241` `if (Role == NetRole.Host && _t.AcceptedCount != _lastAccepted)`；`_lastAccepted = _t.AcceptedCount` 在 `:250`；
- **连接位读**（静默超时条件）：`:293` `if (_t.IsConnected && (Role != NetRole.Host || _t.AcceptedCount == _lastAccepted) && now - _lastRecvMs >= SilentTimeoutMs)`。

⇒ 旧 `:213` / `:232` / `:213-215` **全部已漂**。

**改后**（现行号 `:1684-1694`）：去行号、改符号锚点 ——

- `:1684` `AcceptedCount++`（`Setup` 里那两句）
- `:1686-1688` `Pump` 的**握手边沿**那一支（`Role == NetRole.Host` 且 `_t.AcceptedCount != _lastAccepted`）… 同一轮 `Pump` 的**静默超时**条件里读到 `IsConnected` 已立
- `:1694` `Pump` 的握手边沿那一支：看到计数变了就 `_lastAccepted = …` 并**立刻**发 `Challenge`

### C · `Shell/DeckSelectionPopup.cs:521` 两处裸 `.cs`

**现核 + git 判定**：

- 该行由 commit `2cfb3574` 引入；`git blame`/`git show 2cfb3574:<文件>` 显示**首次出现就是裸 `.cs`** ⇒ 「更早替换事故」在 git 里**找不到被替换的原文**（无法恢复原始路径串）。
- **语义可确定**：引号里的原句「`.cs` 说「文案取不到」**不成立**」**逐字**出自
  `资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md` 第 81 行（D6 行）；句中那个 `.cs` 指的是
  `Shell/DeckSelectionPopup.cs` 的旧文件头（旧说 `Instructions 2` 文案取不到；本文件 `:101-107` 已就地更正）。
- **改后**（现 `:521-522`）：改成完整锚点 + 自足语义 ——
  `资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md` 的 D6 行：「本文件旧注释说『文案取不到』—— 那句**不成立**，指的就是这件事」。

## 二、改动清单（3 文件；行尾 LF，Python 二进制复核：CRLF 0）

| 文件 | 位置（现行号） | 改了什么 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs` | `:4403-4418` | 注释订正 + 旧「空白占位」断言 → 「非空 + == 官方词条值（独立取判据）」+ `Loc.HasEntry` 前提 |
| 同上 | `:4429-4433` | 旧「面板正文空」断言 → 「== `HelpTipBody` 且 == 官方词条值」 |
| `Unity/MyGame/Assets/CardPresentation/Editor/NetSelfTest.cs` | `:1684-1689` · `:1694` | `NetTransport.cs:220` / `Pump:213` / `Pump:232` / `Pump:213-215` → 符号锚点（`Setup` / `AcceptedCount` / `IsConnected`） |
| `Unity/MyGame/Assets/CardPresentation/Shell/DeckSelectionPopup.cs` | `:521-522` | 两处裸 `.cs` → 完整文档锚点 + 自足语义 |

`git diff --numstat`（**含 TOOL-3/TOOL-4 在本分支同一文件上的既有改动**，非本轮单独增量）：
`RewardsScene.cs 143/62` · `NetSelfTest.cs 43/35` · `DeckSelectionPopup.cs 13/12`。
行尾复核（二进制）：RewardsScene CRLF 0 / LF 11,869 · NetSelfTest CRLF 0 / LF 2,578 · DeckSelectionPopup CRLF 0 / LF 1,055 —— **一个都没被翻**。

## 三、没查清的部分

1. **C 件原始串不可恢复**：git 里该行一出生就是裸 `.cs`（见上），只能按「逐字匹配 1017 对账 D6 行」判定原意；第一个 `.cs` 究竟是该文档路径还是本文件路径的缩写，**无法用 git 证实**（两种读法指向同一事实，不影响修法）。
2. **`Shell/MissionsTab.cs:1578` 的过期说法未能定位**：UI-3 报告提到它仍写 `ForgeTab.HelpTipBody = " "`，但现读该文件 grep `HelpTipBody` 0 命中 —— 已不存在或已改，未再追是哪一轮改掉的。
3. 本波**未跑任何 Unity 自检**（红线），断言只做静态核对。

## 四、顺手发现（只报不改）

1. **`Net/NetTransport.cs:103-109`**：`Pump:213` / `Pump:232` / `NetSession.Pump:213-215` —— 与 B 件同源的漂移行号仍在（白名单外）。
2. **`Net/NetSession.cs:280` 与 `:290`**：注释里仍写 `:213`（「上面那一支（`:213`）读计数」/「下一帧 `:213` 那支」）—— 同一个漂移（白名单外）。
3. **`Shell/ForgeTab.cs:492-494`**：构建期 `Debug.Log` 仍宣称「正文留空、不自己编」（与现实现相反）。
4. **`Shell/BoosterInfoPopup.cs:44` + `:687-692`**：文件头与 `HintGaps()` 出声仍宣称「正文是空的 / 词条表本地没有」。
5. **`Core/Loc.cs:2777`**：`HelpTipBody = " "` 的过期副作用说明。
6. **typecheck 红在别人的文件**：`Assets/RuleEngine/Core/RuleCore.cs:8981-8983` 报 `CS0103: 当前上下文中不存在名称"EffectResolver"`（3 条，运行时程序集）。该文件 mtime 22:51:39、内容含「2026-10-19 战斗线 `T21`」的新代码 ⇒ 是战斗/引擎线在飞的半成品，**与本代理 3 个文件无关**（编辑器程序集 0 错）。已按 13·3 第 2 条隔 60 秒重跑两次，仍红。

## 五、typecheck 结果

```
# 第一次（本轮改动落盘后）
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0

# 第二次 / 第三次（等 60 秒后重跑，结果相同）
--- 运行时程序集 ---
Assets\RuleEngine\Core\RuleCore.cs(8981,32): error CS0103: ...
Assets\RuleEngine\Core\RuleCore.cs(8982,47): error CS0103: ...
Assets\RuleEngine\Core\RuleCore.cs(8983,21): error CS0103: ...
运行时错误数: 3
--- 编辑器程序集 ---
编辑器错误数: 0
```

结论：**编辑器程序集 0 / 0**（覆盖 `RewardsScene.cs` + `NetSelfTest.cs`）；运行时程序集那 3 条全在**白名单外的 `RuleEngine/Core/RuleCore.cs`**（战斗线 T21 在飞），3 个白名单文件**零错误**。
