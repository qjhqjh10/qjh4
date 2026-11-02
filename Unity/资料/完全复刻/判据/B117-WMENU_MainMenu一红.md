# B117-WMENU · `MainMenuScene.Run` 唯一一红（C1 官方中文收口）修复报告

日期：2026-11-02 · 执行代理 `B117-WMENU` · 独占文件 `Assets/CardPresentation/Editor/MainMenuScene.cs`

## 〇、结论一句话

基线那唯一一条红（`★（C1）手写表中文列逐条 = 官方 zh 列：460 条全一致（不一致 1 条：MainMenu/NavigationPanel/HomeButton 我们「游戏」≠ 官方「玩」）`）
**不是缺陷，是用户 2026-11-03 亲自裁定的一处主动偏离**。已把那条断言从「全等」改成**单条白名单式**，
并用三条硬断言实现**三向灭自证**；`Core/Loc.cs` 一个字没动。

## 一、断言位置与改后的写法

- 原断言（失败行）：`Assets/CardPresentation/Editor/MainMenuScene.cs` 的 `CheckB106W29` → `Section("C1 官方中文全量收口：手写表逐条 = 官方 zh 列")`
  - 改前位于 `:17024-17043`（`int c1Covered = 0, c1Bad = 0;` 起、`CheckTrue(c1Bad == 0, …)` 止）。
  - 改后同一节扩到 `:17024-17069`（`+25` 行；详见 §四 的行号漂移提醒）。
- 白名单条目的自证写在代码里（`MainMenuScene.cs:17024-17034`）：

```csharp
// 🔶 自己设计（非原版）：白名单 —— 手写表允许**恰好一条**已知偏离，且只允许这一条。
//   为什么必须偏离：用户 2026-11-03 亲自裁定「改为游戏，官方翻译太差了」（账 `B117-R12`）；
//   官方 zh 列 `MainMenu/NavigationPanel/HomeButton` 仍是「玩」、原版实拍也是「玩」
//   ⇒ 这是我们**主动**偏离原版，**不是**原版值变了。取值落点 = `Core/Loc.cs:501`（`B117_F10`）。
//   三个值写死在这里（⛔ 不从 `Loc` 读回来 —— 读回来就是拿表证明表），构成三向灭自证：
//     ① 偏离的键**必须**是这一条、值**必须**是「游戏」⇒ 下面 `c1WhiteHits == 1`；
//     ② 把它改回官方「玩」⇒ 命中 **0** 条 ⇒ 红（⛔ 「没有偏离」不等于「白名单通过」）；
//     ③ 偏离的键换成**另一条** ⇒ 那条不进白名单 ⇒ 下面 `c1Bad == 0` 红。
const string c1WhiteKey = "MainMenu/NavigationPanel/HomeButton";
const string c1WhiteOurs = "游戏";
const string c1WhiteOfficial = "玩";
int c1Covered = 0, c1Bad = 0, c1WhiteHits = 0;
```

- 循环里只有「**键 + 值**都对得上」才放过（`MainMenuScene.cs:17043-17045`）：

```csharp
// 🔶 唯一一条白名单（用户 2026-11-03 裁定）：**键 + 值**都对得上才放过，否则照旧算偏离
if (k == c1WhiteKey && ours == c1WhiteOurs) { c1WhiteHits++; continue; }
c1Bad++;
```

- 三条自证断言（`MainMenuScene.cs:17053-17068`）：
  - `CheckTrue(c1WhiteHits == 1, …)` —— 白名单**恰好命中 1 条**；
  - `CheckTrue(Loc.Lang13Of(c1WhiteKey, "zh") == c1WhiteOfficial, …)` —— 官方 zh 列**仍是「玩」**；
  - `CheckTrue(c1Bad == 0, …)` —— 除白名单外**零偏离**（失败文案已改写为「除**白名单 1 条**……外全一致」）。

## 二、灭自证怎么实现的（两个方向都会红）

| 破坏方式 | 触发哪条 | 结果 |
|---|---|---|
| 把 `Core/Loc.cs` 那一条的中文改回官方「玩」 | `c1WhiteHits == 0`（②） | **红**（文案：「把它改回官方「玩」…⇒ 命中 0 条 ⇒ 本条红」） |
| 把表里那条**删掉**（`HasEntry` 假） | 同上，`c1WhiteHits == 0` | **红**（文案里点名了这一种） |
| 偏离的键换成**另一条** | 那条不进白名单 ⇒ `c1Bad > 0`（③） | **红** |
| 官方 zh 表本身变了（不再是「玩」） | `c1WhiteTheirs == "玩"` 失败 | **红**（「我们是主动偏离」这个前提不成立） |
| 白名单值改成别的字（不是「游戏」） | `c1WhiteHits == 0` + `c1Bad > 0` | **红**（两条同时红） |

- 期望值（键 / 「游戏」 / 「玩」）是**写死的裁定值**，⛔ 不是 `Loc.T` / `Loc.ZhOf` 读回来的 —— 否则就是拿表证明表。
- 断言条数：**3 条**（原 1 条 `c1Bad == 0` 保留未删，另加 2 条自证；`c1Covered ≥ 447` 那条未动）。
- ⛔ 没把整条断言放宽成「允许任意偏离」：白名单是 `const string` 的单键 + 单值，任何第二条偏离都会落进 `c1Bad`。

## 三、typecheck

```
TMPDIR=/tmp/wf_wmenu bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 首跑曾出 4 条运行时错（`Battle/SettingsPanel.cs` · `Battle/CardChoicePanel.cs`）+ 1 条编辑器错（`Editor/B117_WCOLL_Assert.cs` CS0102 `Unverified` 重名）——
  **全部落在别的代理正在写的文件上**（mtime = 当时那几秒），等它们停手后复跑 ⇒ **0/0**（符合铁律 13·3「错误集中在不是你负责的文件上 ⇒ 不是你的事」）。
- 行尾：`MainMenuScene.cs` 现读 **LF 21220 行 / CRLF 0**（编辑前后一致，未被翻行尾）。
- `git diff --numstat`：`28 / 3`（全是本节的两处编辑，无整篇重写）。

## 四、⚠️ 账的行号漂移提醒（给调度台）

- 本文件行数 **+25**（`28` 插 / `3` 删）。`资料/完全复刻/账/*.tsv` 里指向 `Editor/MainMenuScene.cs` 的引用共 **19** 处，
  其中**落在 `:17024` 之后的有 4 处**（最深 `:20285`）⇒ 这 4 处会漂 `+25`。
- 建议在同步点（无 `.cs` 写手时）跑一次 `check_ledger_points --all-A --fix` 收口。本代理按红线**没有**跑账闸、**没有**动账。

## 五、`MainMenu/MainButtons/ButtonLabel/Play` 消费点核查

**结论：仍是全仓零消费点（0 处代码读取、0 处手写表条目）。**

- `.cs` 全仓命中 **1** 处，且是**注释**：`Editor/MainMenuScene.cs:21153`（B117_F10 的说明文字里列举「三处官方来源」时才提到）。
- 手写本地化表 `Core/Loc.cs` 的 `Table` **没有**这条键（`MainMenu/MainButtons/` 那一族现读只有 `Deck` 与 `Back`）。
- 其余命中全在**官方原始数据文件**（不是消费点）：`Resources/official_loc_13lang.json` · `数据/游戏数据/官方本地化_13lang.json` · `官方本地化_en+zh-CN.json`。
- ⇒ 按简报口径**只记这一句，不立账**。

## 六、红线遵守

- 只写了 `Assets/CardPresentation/Editor/MainMenuScene.cs` 与本报告。
- ⛔ 未改 `Core/Loc.cs`（`MainMenu/NavigationPanel/HomeButton`「游戏」保持原样）· ⛔ 未跑 Unity 批处理 · ⛔ 未动 git · ⛔ 未碰 `d:/2`。
- 改 `.cs` 前 `tasklist | grep -ci "Unity.exe"` = **1** ⇒ 后台等待到 **0**（`UNITY_GONE after 8 checks`，约 7 分钟）后才动手。
