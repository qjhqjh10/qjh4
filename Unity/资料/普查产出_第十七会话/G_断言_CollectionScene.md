# G·断言 · CollectionScene —— 「末键被当母版关掉」补断言

**只改一个文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`（LF 原样：改前 CRLF=0/LF=8457，改后 CRLF=0/LF=8502 ⇒ **我这一批净 +45 行**）。
⚠️ `git diff --numstat` 报的是 **60 / 4** —— 那里面**含主对话 A1498 那笔的 11 行**（我动手之前就已未提交），不是我加的，别当成我的量。
⛔ 没跑 Unity（简报禁）· 没动 git · 没碰 `Shell/*` · `CLAUDE.md` / `项目任务.md` / `Editor/MainMenuScene.cs`。

## 加了哪几条（本文件哪一行 + 一句话）

| 行 | 一条话 |
|---|---|
| `:931-935` | 给原来那句 `Check(keys.Count, 4, …)` **加注**：`GetComponentsInChildren(true)` **含 inactive** ⇒ 它只证「建出来了」、**不证「看得见」**（判据句本身没动，文案里写清了这个弱点）。 |
| `:959-965` | 新增 `var tabTerms`：四颗键名**写死在本文件**（`MainMenu/MainButtons/ButtonLabel/Deck` · `MenuShop/ShopItemType/Cards` · `MenuShop/ShopItemType/Cosmetics` · `MenuShop/AlternateArtStyle`）。 |
| `:975` | ★（灭自证）**原版字面量 vs 实现表**：`string.Equals(CollectionWindow.Buttons[i].Term, tabTerms[i], Ordinal)` —— 期望值是写死的串，被测的是实现那张表。 |
| `:978` | `Loc.HasEntry(tabTerms[i])` 前提（表里真有这条词条）。 |
| `:998-999` | 前提：`FindChild(bar, "CollectionTabButton_" + i)` 取得到（⛔ **按名字取**，不按 `keys[i]` 的遍历次序）。 |
| `:1000-1003` | ★★ **本批主要交付**：`kb.gameObject.activeSelf == true` —— 四颗键**逐颗**断「真在画面上」。 |
| `:1006-1010` | 四颗**文案**逐颗断 `Loc.T(tabTerms[i]).ToUpperInvariant()`（键名来自上面那张写死的表，⛔ **不再读** `Buttons[i].Term`）。 |

> 原来那条 `for (int i = 0; i < 4 && i < keys.Count; i++)` 的文案循环**已并入**上面这个循环 ——
> 顺带把「第 i 颗」从**遍历次序**改成**按名字钉**（原来 `keys[i]` 的次序恰好对，但那是运气）。
> `Shell/CollectionWindow.cs` 本身**一个字没动**（它 `:2594` 已经是 `BuildShell(…, lastIsMaster: false)`）。

## 每条「改回什么 ⇒ 这条红」

| 改回什么（在哪个文件） | 变红的那条 |
|---|---|
| 🔴 `Shell/CollectionWindow.cs:2594` 删掉 `lastIsMaster: false`（= 走 `MenuWindowBase.BuildBar` 的缺省 `true`）⇒ `BuildBar:490` 把 `tabButtonPrefab = roots[3].gameObject`，`TabButtons.Initialize`（`Shell/RewardsWindow.cs:129`）第一件事 `SetActive(false)` | **`:1000`** 第 4 颗 `activeSelf`（其余三颗照旧绿 —— 缺陷就是**只有末项**被关） |
| `Shell/CollectionWindow.cs:2594` 把 `btnPrefix` 从 `"CollectionTabButton_"` 改掉 | **`:999`** 前提红 ⇒ `:1000` / `:1006` 一起红 |
| `Shell/CollectionWindow.cs` 的 `Buttons[i].Term` 改成 `null` / 换键（「实现少一条词条」） | **`:975`** 红（`:1006` 在**英文档**下仍绿 —— 见下节） |
| `Core/Loc.cs` 删掉那四条词条（表缺键） | **`:978`** 前提红（⇒ `:1006` 的期望值退化成键名，两边一起绿的假象被这条挡住） |
| `Shell/MenuWindowBase.cs:564` 把 `Loc.T(spec.Term)` 改回 `spec.Label` | **中文档**：`:1006` 红（屏上 `DECK` ≠ `卡组`）；**英文档**：本条**咬不住**（两个串相同）—— 如实标在下一节 |
| `Shell/MenuWindowBase.cs:490` 把 `lastIsMaster ? … : null` 写死成 `…` | **`:1000`** 红（与第 1 行同一条路，只是从基类这一侧改） |

⚠️ **这些「红」是静态推的、没有实跑**（本会话禁跑 Unity）：`BuildBar:490` → `Initialize:129`（`SetActive(false)` 打在 `res.roots[3].gameObject` 上，那颗 GameObject 就叫 `CollectionTabButton_3`）是**逐行读出来的**，`d:/2/tools/decomp_full/TabButtons__Initialize.c:35-44` 是它照抄的原版指令流。**主对话在同步点跑 `CollectionScene.Run` 时请重点看 `:1000` 那四行**。

## 没做到的（为什么）

1. **英文语档下咬不住「实现不再走词条」**。`Core/Loc.cs:84` 是 `Default = Chinese`，但 `Current` 从 `PlayerPrefs` 载入 ⇒ 批处理的语档**不写死**。四种英文串（`Deck`/`Cards`/`Cosmetics`/`Styles`）与 `TabBtnSpec.Label` **逐位相同** ⇒ 英文档下 `Loc.T(键).ToUpperInvariant()` 与「直接画 `Label`」在屏上是**同一个串**，`:1006` 分不出来。补了一条 `:975`（原版字面量 vs `Buttons[i].Term`）挡住「表里的键被删/改」那一半，但「`BuildTabButton` 那一行被改回 `spec.Label`」这一半在英文档下仍不可检 —— 要真咬住得在断言里切一次语档（`Loc.SetLanguage` + `PersistOverride`），**那是新的全局状态改动、不在本批授权内**，如实留着。
2. **没跑 Unity 自检**（简报明令禁跑；本活的自检宿主只能是 `CollectionScene.Run`，由主对话在同步点跑）。
3. **「摆回基线」做成了「证明不需要摆」**：本档只读**构建期** `SetActive` —— `SetHighlight`（`MenuWindowBase:500-505`）切的是 `Highlight` **子件**、不是键本身，所以「当前选哪一页」漏不过来；且本档紧跟 `Build()` 跑（`CollectionWindow.Build` 末尾 `tabButtons.Click(0)` = 出厂态）。按本仓规矩我把这条**写进了注释**（`:991-993`）而不是加一句无用的复位 —— 因为**本档前面没有任何会改页签的单元格**。👉 如果主对话认为仍要显式复位，说一声我加。

## 顺手发现（尤其别的「恒绿」断言）

1. 🔴 **同族恒绿 · `Editor/CollectionScene.cs:3695-3704`**（`Highlight` 那三条：`Check(hi.Count, 4, "左栏四个键的选中底图都建了")` · `Check(on, 1, "恰好一个键亮着")` · `hi[0] 亮 / hi[1] 不亮`）
   —— 它们同样用 `GetComponentsInChildren<Transform>(true)` 收 `Highlight`，而 `Highlight` 是**键的子件**、`SetHighlight` 改的是它**自己的** `activeSelf`（**与父件是否被关无关**）⇒ **第 4 颗被 `SetActive(false)` 关掉时，这三条照样全绿**。这是本次修复的**同一根因的下一处**，⛔ 我没改（不在本批交付、也不许我顺手改）。
2. 🔴 **`Shell/MenuWindowBase.cs:443-444` 的 doc 已过期（「当时对、现在不对」）**：它写「⚠️ `Shell/SocialWindow.cs` 那一扇**今天还在按旧口径建**（传缺省 `true`）⇒ 它的第 2 颗（`Friends`）被当母版关掉了 —— 已记进本会话报告」。**实测**：`Shell/SocialWindow.cs:178` 已经传 `lastIsMaster: false`（`git status` 里它是 `M`，同批 2026-10-11 改的）⇒ 那句 **`Friends` 被关掉的事实已不成立**。建议主对话就地改（`Shell/*` 不在我白名单）。
3. ⚠️ **`MenuWindowBase.BuildBar` 的缺省是「危险的那一档」**：`lastIsMaster = true` 对奖励窗/商店是对的、对收藏窗/社交窗是错的，而**忘了传 = 静默少一个键**（正是本次的缺陷形状）。四扇窗里**只有两扇**该用缺省。这不是缺陷（原版就是这样分档），但**新加一扇带左栏的窗时踩坑概率很高** —— 值不值得加一条「窗数 × 是否传参」的静态断言，请主对话裁。
4. ℹ️ `Editor/CollectionScene.cs:935` 的 `Check(keys.Count, 4, …)` 我**只加注、没删** —— 它挡的是「`Buttons` 表被加到 5 项而末项当母版」那一支（那时 `_3` 仍激活、`:1000` 绿，只有它红），还有用。

## 类型检查读数

```
TMPDIR=/tmp/wf_g bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```
（改完立刻跑的；无「别人半成品文件」型假错。）
