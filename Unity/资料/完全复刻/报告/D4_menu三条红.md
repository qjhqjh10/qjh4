# D4 · `MainMenuScene.Run` 3 条红定性（只读诊断 · 2026-10-27）

- 读数来源：`d:/4/_tmp_view/menu.log`（19:57 同步点实跑）· 合计 **5,036 通过 / 3 失败**
- 上一份交接读数：`资料/交接_第八十五会话.md:172` = **5,027 / 2**（2 条有据的预期红）
- 预测口径（本会话同步点清单）：`资料/完全复刻/判据/同步点执行清单_第八十六会话.md:233,244` = **5,027 / 2**

## 结论表

| # | 红 | 定性 | 是不是「既有预期红」 |
|---|---|---|---|
| 1 | `In Alliance` 直接子件 | **(β) 实现缺陷（已知未实现 = 账 128 / 界面线 `J-05`）**，不是回归 | ✅ 是（= 上一份交接那 2 条之一） |
| 2 | `No Alliance` 直接子件 | **(β) 同上**，同一条 `J-05` | ✅ 是（另一条） |
| 3 | 升段窗段位大图**不画** | **(γ) 本批回归**（本会话 `W28MOUNT` + `W39MAT` 换了载体），但红的是**断言用的代理量**、不是行为目标 ⇒ **修法是改断言（α 式修），⛔ 不回退实现** | ❌ **不是**。预期红只有 2 条，这条是本会话新出现的 |

---

## ① `In Alliance` 的直接子件

**原文**（`menu.log:203625`，日志被截断，下面是断言源码里的完整串）：

```
✗ ★ `In Alliance` 的直接子件（**顺序照 `m_Children`**；`Alliance Event Score Info` 是**嵌套子 prefab**）
  —— 期望 [Alliance Name|*View Leaderboard Button|Alliance Event Score Info]
     实得 [Alliance Name|View Leaderboard Button|Alliance Event Score Info]
```

**位置**：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:16122-16123`

**期望的完整子件清单**（按 `m_Children` 序，`*` = 自己 `activeSelf == false`，见 `DirectKids` 定义 `:18416-18426`）：
`Alliance Name` · `*View Leaderboard Button` · `Alliance Event Score Info`

**实得**：`Alliance Name` · `View Leaderboard Button` · `Alliance Event Score Info`

> 🔴 **不是顺序问题、也不是多/少节点** —— 名字与次序**逐字相同**，只差 `View Leaderboard Button` **自己 `activeSelf` 是 `T` 而不是 `F`**（`*` 前缀）。

**定性**：**(β) 实现缺陷**，且是**已知未实现**（账 128 / 界面线 `J-05`），**不是本批回归**。

**证据**：
- 断言行内注释就是判据（`MainMenuScene.cs:16122`）：`leaderBoardID 空 ⇒ 两钮 self activeSelf=false（AlliancesEventScorePanel__Initialize.c:123-137）⇒ 期望值＝目标态；J-05 落地前这两条**预期红**`
- 原版判据现读：`d:/2/tools/decomp_full/AlliancesEventScorePanel__Initialize.c` 里那两个分支都是
  `cVar3 = System_String__IsNullOrEmpty(param_5,0); UnityEngine_GameObject__SetActive(lVar4, cVar3 == '\0', 0);`
  —— 两颗榜钮的 `activeSelf = !IsNullOrEmpty(leaderBoardID)`。
- 我们的实现（`Shell/AllianceEventScorePanel.cs:122` / `:173`）用 `MenuDraw.Node(...)` 建出来就**没再动过 `SetActive`**；
  唯一的状态口 `SetInAlliance(bool)`（`:194-199`）只切 `In Alliance` / `No Alliance` **两支**，**没有 `leaderBoardID` 这个概念**（方法签名里就没有那个参数）。
- 既有记录（4 处口径一致）：`资料/完全复刻/报告/B9_D3OT_其余宿主红定性.md:17-18,149`（判 B1/B2 = β 已知未实现、不是回归）· `交接_第八十四会话.md:48,226` · `任务线/界面线/任务总表.md:377` · `交接_第八十五会话.md:172`

**该怎样**：落地 `J-05` —— `Shell/AllianceEventScorePanel.cs` 增加「`leaderBoardID` 空 ⇒ `_viewLb` / `_noLb` 各自 `SetActive(false)`」（照 `__Initialize.c` 那两个 `IsNullOrEmpty` 分支），改完这两条**自动转绿**（`DirectKids` 会给关着的件加 `*`），⛔ 不要改断言。

---

## ② `No Alliance` 的直接子件

**原文**（`menu.log:203639`，完整串取自源码）：

```
✗ ★ `No Alliance` 的直接子件（**那 6 件「自己 active、祖先 inactive」的都在它底下**）｜🔴 账128：同上（`*` = 自己 activeSelf=false）
  —— 期望 [Background|Join Alliance text|Join Alliances Button|*Leaderboard Button]
     实得 [Background|Join Alliance text|Join Alliances Button|Leaderboard Button]
```

**位置**：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:16124-16126`

**期望的完整子件清单**：`Background` · `Join Alliance text` · `Join Alliances Button` · `*Leaderboard Button`

**实得**：同上但 `Leaderboard Button` **无 `*`**（自己 `activeSelf == true`）

**定性 / 证据 / 该怎样**：与 ① **完全同一条根因**（同一次 `J-05` 落地一起转绿）。见 `Shell/AllianceEventScorePanel.cs:173`（建 `Leaderboard Button`）与 `:194-199`（状态口无 `leaderBoardID`）。

---

## ③ 升段窗段位大图**不画** —— 本会话新红（γ）

**原文**（`menu.log:208228`）：

```
✗ 升段窗段位大图**不画**（`divisionIcon.m_Sprite = 0`，段位图名对照表本地没有） —— 期望 [True]，实得 [False]
```

**位置**：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:16875-16877`

```csharp
CheckTrue(dc.DivisionArtBlank && dc.DivisionNode != null
          && dc.DivisionNode.GetComponentsInChildren<ImageQuad>(true).Length == 0,
          "升段窗段位大图**不画**（`divisionIcon.m_Sprite = 0`，段位图名对照表本地没有）");
```

**逐项拆这条（哪一项为假）**：
1. `dc.DivisionArtBlank` = **true** ✅（日志 `menu.log:207980` 有 `[RankedDivChange] 留空：Division Image …`，即 `RankedDivisionChangeWindow.cs:279` 那一支）
2. `dc.DivisionNode != null` = **true** ✅（日志里**没有**「载体 quad 没建出来」告警，`grep -c "载体 quad 没建出来"` = **0** ⇒ 三次 `MenuDraw.Rect` 都成功返回了 quad）
3. `GetComponentsInChildren<ImageQuad>(true).Length == 0` = **假** ❌ ← **就是这一项**

**实得清单（`dc.DivisionNode` 子树里的 `ImageQuad`，推导自建树代码，本机未跑 Unity）**：

| 序 | 节点 | 建法 | 出处 |
|---|---|---|---|
| 1 | `Division Image`（= `_division` 本身） | `MenuDraw.Rect(...)` ⇒ **ImageQuad** | `Shell/RankedDivisionChangeWindow.cs:1261` |
| 2 | `Division Image/Rank Icon` | `MenuDraw.Rect(_division, ...)` ⇒ ImageQuad | `:1280` |
| 3 | `Division Image/Rank Icon/Rank Icon Glow` | `MenuDraw.Rect(_rankIcon, ...)` ⇒ ImageQuad | `:1310` |

⇒ `Length == 3`（≥1 即红）。其余子树（`Sparks Burn` 等）都是 `MenuDraw.Node` 的裸节点，不计入。

**定性**：**(γ) 本批回归** —— 本会话的两处**合法**改动把这个「代理量」改掉了：
- `W28MOUNT`：`Division Image` / `Rank Icon` 由 `MenuDraw.Node(...)`（裸节点）换成 `BuildDivisionImage()` / `BuildRankIcon()`（`ImageQuad` 载体，tint **α = 0**）—— `Shell/RankedDivisionChangeWindow.cs:276` · `:293`（判据与设计全文见该文件 `:1233-1256`）
- `W39MAT`：`Rank Icon Glow` 同批换成 `BuildRankIconGlow()`（`ImageQuad` + `UI Additive` 材质，节点照旧 `SetActive(false)`）—— `:294`

**证据**：
- `git diff -- Assets/CardPresentation/Shell/RankedDivisionChangeWindow.cs`：`- _division = MenuDraw.Node(transform, "Division Image", DivisionR);` → `+ _division = BuildDivisionImage();`；`- _rankIcon = MenuDraw.Node(...)` / `- _rankGlow = MenuDraw.Node(...)` → `+ BuildRankIcon()` / `+ BuildRankIconGlow()`
- 派活/回执：`资料/完全复刻/调度台日志.md:6208-6219`（「12·49 回执 ㉛（`W28MOUNT`）」对 D 项写的就是「`Division Image` / `Rank Icon` 补上 `ImageQuad + MeshRenderer` 载体」）· `:6338-6341`（`W39MAT` 把 `Rank Icon Glow` 的载体重建）
- 🔴 **为什么没人发现**：`资料/完全复刻/报告/B9_W28MOUNT_特效挂载载体.md:264` 写「`DivisionNode`/`RankIconNode` 两个属性**全仓 0 使用者**」——**这条是错的**：`RankedDivisionChangeWindow.DivisionNode` 有 1 个使用者，就是这条断言（`Editor/MainMenuScene.cs:16876`）。那句误判正是漏检的直接原因（顺手订正该行）。
- **行为目标没变**：三个载体**都画不出东西** —— `Division Image` / `Rank Icon` 的 tint = `new Color(1,1,1,0f)`（`α=0`，`:1262` / `:1281`），`Rank Icon Glow` 节点 `activeSelf=false`（`:295`）；图仍然空（`Build()` 的 `DivisionArtBlank = true` + `NoteNoArt` 每次出声）。**观感与原版一致**（原版 `sprite` 取不到时 `DivisionImageDisplay__Initialize.c` 也是 `SetActive(false)`）。
- **不是预期红**：预期红只有 2 条（`交接_第八十五会话.md:172` · `同步点执行清单_第八十六会话.md:233,244`），本会话读数从 `5,027/2` → `5,036/3`，这一条是本会话才出现的。

**该怎样**：改**断言**（`MainMenuScene.cs:16875-16877`），把「不画」的表达从 **`ImageQuad 数 == 0`** 换成**新目标态**，例如：
> `dc.DivisionArtBlank && dc.DivisionNode != null` 且 `dc.DivisionNode.GetComponentsInChildren<ImageQuad>(true)` 里**每一个**都 `q.Tint.a <= 0f || !q.gameObject.activeInHierarchy`（`ImageQuad.Tint` getter 在 `Battle/ImageQuad.cs:410`）。

⛔ **不要回退 `BuildDivisionImage` / `BuildRankIcon` / `BuildRankIconGlow`**（那是照原版 `GameObject/Division Image.json` 的 `Image + AnimateUIMaterialProperty` 组件序做的，回退等于把材质挂载点又删掉）；⛔ 也别改成 `GetComponentsInChildren<ImageQuad>(false)` —— 那样两个 α=0 的载体仍会被数到（它们 `activeSelf=true`），照样假。

---

## 附：本次诊断没做的事

- 没跑 Unity、没改任何 `.cs` / 账 / 正本 / 场景、没动 git（只 `git diff` 读）。
- ③ 的 `Length == 3` 是**读建树代码推出来的**（本机未跑 Unity 复核），但「≥1」这一点由「日志无载体失败告警 + 三处 `MenuDraw.Rect` 都返回 `quad.transform`」两条独立佐证。
