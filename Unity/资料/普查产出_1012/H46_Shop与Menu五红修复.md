# H46 · `ShopScene.Run` 3 红 + `MainMenuScene.Run` 2 红 —— 照 D11 落地修复（2026-10-12）

> **角色**：写手代理（A507 = Shop 3 条 · A508 = Menu 2 条）。
> **判据来源 = `资料/普查产出_1012/D11_Shop与Menu红诊断.md`**（只读诊断，本轮**没有**重新查根因）。
> ✅ **改了生产/自检代码**（白名单内的两个 `Editor/*.cs`）· ⛔ **没跑 Unity**（一次 `-executeMethod` 都没调）· ⛔ 没动 git
> · ⛔ 没改 `Shell/*`（`ShopWindow.cs:662` 那个 `30` 一个字没动）· ⛔ 没改两张正本。
> ✅ **秒级类型检查 = `0 / 0`**（`TMPDIR=/tmp/wf_h46 bash d:/4/Unity/工具/typecheck.sh`，改完跑过两次，都干净）。
> 📌 **验收点 = `ShopScene.Run` + `MainMenuScene.Run` 两条**（由调度台在同步点复跑；本轮按用户口径不跑自检）。

---

## 〇、结论（一句话 ×2）

* **Shop 那 3 条 = 一条断言量错了对象**：它量的 `CatalogItemShopContainer_0/Name` 是 `Shell/ShopWindow.BuildCell`
  **我们自加**的一行字（上限写死 `30`），而判据文案说的是 `OfferContainer` 的 `name`（上限 `42`，**另一棵树**）。
  ⇒ 把这条断言**整条搬到真树上**（§A8 那 19 份变体的循环里，紧挨 `name` 标称字号那条），删掉错误落点。
* **Menu 那 2 条 = 一处「清场」关掉了被测的窗**（第 2 条是它的下游）：A416 把 `:3866` 改成
  `Manager.popUpWindow.Close()`，而那一刻 `popUpWindow` **装的就是被测的排位窗**（`type = Popup` 的窗一开就写它）
  ⇒ 排位窗被关 + 被摘出 `openWindows` ⇒ 后面没人能把它带回来。
  ⇒ 改回**按类型清**，并把**两类模态宿主**（`PopUpGameWindow` + `PromptPopup`）一起覆盖；
  同批那一处「同样脆」的（练习窗段）一并改掉。

**两条都没有放宽任何断言**（Shop 那条是**换了个被量对象**、期望值仍是原版字面量 `42`；
Menu 那两条是**夹具/清场**修好，被测的断言一个字没动）。

---

## 一、改动清单（`文件:行号`，每处一句为什么）

| # | 落点（本轮实读的行号） | 改了什么 | 为什么 |
|---|---|---|---|
| 1 | `Editor/ShopScene.cs:1357-1367` | **删掉**商店格循环里那条「`name` 上限 = 42」断言（`if (i == 0) { … }` 整块），**留 11 行指针注释** | 它量的是 `CatalogItemShopContainer_*/Name` = `ShopWindow.BuildCell` 自加的字（上限写死 `30`）⇒ 与判据 `OfferContainer` 的 `name`（`42`）**是两棵树** ⇒ 结构上**永远不可能绿**；注释留着是为了让下一个会话**别再挂回来**（`ShopWindow` 那三处没有原版判据，`F1:194`） |
| 2 | `Editor/ShopScene.cs:2564-2580` | **新增**一条：`CheckNear(Label.FontSizeToPx(lbName.FontSizeMax), 42f, 0.35f, …)` | 判据（原版 21 颗 `'Legendary Wildcard Bundle'` 的 `m_fontSizeMax` **全是 42**）该断的**就是这颗 label** —— 它就在**生产调用点**上（`OfferContainer.cs:1081-1082` 的 `LabelFit(…, 42f, 46f)`），同段 `Timer Text` 的 max/min 是现成范式 |
| 3 | `Editor/MainMenuScene.cs:201-234` | **新增两个私有助手**：`CloseModalPopups()`（`:217`，按类型清两类模态宿主）· `AnyModalPopupLeft(wm)`（`:229`，清完之后**还有没有模态宿主挂着**，只用来出声） | 「两处写同一条规则 = 迟早不一致」：两处清场共用**一份**定义；顺带把「出声」的判据从 `popUpWindow != null`（在那两处**恒真**，见下）换成**只认两类模态宿主** |
| 4 | `Editor/MainMenuScene.cs:1783-1798`（练习窗段 · 同批那处「同样脆」的） | 把 `wmFixA.popUpWindow.Close()` 换成 `CloseModalPopups()`（`:1795`） | D11 §2.6 第 4 条点名的同批写法：**这一处本轮没红**（那扇模态窗当时确实就是 `popUpWindow`），但只要它被**任何后来的弹窗**顶掉就重演排位窗那一条 ⇒ 一并改 |
| 5 | `Editor/MainMenuScene.cs:3897-3914`（排位窗段 · **红 1 的事故点**） | 同上：`wmFixB.popUpWindow.Close()` → `CloseModalPopups()`（`:3911`） | 这一刻 `popUpWindow` 里装的就是**被测的排位窗**（`Shell/WindowsManager.cs:858-865` 的弹窗支）⇒ 按它清场 = 把被测的窗关掉（红 1），下游连带红 2 |
| — | `Shell/*` | **一个字没改** | D11 明写 ⛔ 不许把 `ShopWindow.cs:662` 的 `30` 改成 `42`（那是**凭空编一个原版值**）；两边都**没有**必须动生产代码的迹象 ⇒ 没有触发「停手」条件 |

### 1.1 新加的断言长什么样（`Editor/ShopScene.cs:2562-2580`，只列要点）

```csharp
CheckNear(NominalFontSize(lbName), rName, 1e-3f, …);              // 已有：标称（36.7 / 42）
float nMaxPx = Label.FontSizeToPx(lbName != null ? lbName.FontSizeMax : 0f);
CheckNear(nMaxPx, 42f, 0.35f, …);                                 // 新增：自适应上限 = 42（19 份恒等）
```

* **期望值 = 字面量 42**（原版资产字段）—— ⛔ 不回读 `OfferContainer` 里那个 `42f` 实参（= 自证）、
  ⛔ 也不用 `R42` 那条标尺（标尺是 **TMP `fontSize` 量纲**，这里要的是**画布 px**，两条口径别混 ——
  同段 `Timer Text` 那条用的就是 px 字面量）。
* `lbName == null` ⇒ 读出 `0` ⇒ **照红**，不静默（同 `Timer Text` 那两条的写法）。

### 1.2 D11 §三·3 要求「落地时现核 `VExpAll` 那 19 行」—— 核过了，结果如下

D11 说它**没逐份复算**这 19 份变体的 `name` 上限是否**每一份都该断 42**。本件核了三条：

1. **实现侧**：`name` 的 `LabelFit` 全族**只有一个调用点**（`Shell/OfferContainer.cs:1081-1082`），
   末两位 `maxPx / basePx` 写的是**常量 `42f, 46f`** ⇒ **不随变体变**（逐变体变的是 `g.NameFs`，那是**标称**）。
2. **它一定被调用到**：`LabelFit` 里 `SetAutoFitBox` 的门槛是 `minPx > 0 && fontPx > minPx`
   （这里是 `10 > 0` ∧ `36.7/42 > 10` ⇒ 恒成立）；且 `c.Name = DefNameText = "Legendary Wildcard Bundle "`
   （非空 ⇒ label 建得出来、不会 `return null`）。
3. **原版侧**：判据本身就是**一个常数** —— `F1_字号线.md:64` #20 行：那 21 颗的 `m_fontSizeMax` **逐颗全是 42**
   （3 份标称 42 + 18 份标称 36.7，同一份里「标称 ≠ 上限」）。
   **旁证（本轮日志）**：`_tmp_view/shop.log:26014…` 那 19 行「`name` 字号」读出 **4.10（42 那 3 份）/ 3.59（36.7 那 16 份）**，
   而 `FontSizeToPx` 是**线性、无偏移**的（`Label.cs:481-484`）⇒ 「上限」那一路必然落到同一个 `42px`。

⇒ **42 对 19 份全都成立**，不需要按变体分档。`VExpAll` 表里 3 份 `Geo391` / 16 份 `Geo778`/`GeoSmall` 的
`NameFs` 分布（`Editor/ShopScene.cs:66-145`）**与断言无关**（标称另有那条在断）。

---

## 二、逐条：修的是什么 · 怎么咬住（改坏法）

| 红 | 修的是什么 | **改坏法（改回去 ⇒ 这条红回来）** |
|---|---|---|
| **Shop ×3**（日志 `1624`/`3245`/`7048`，文案逐字相同、因外层按页循环 3 次） | 断言**换了个被量对象**：从 `ShopWindow.BuildCell` 自加的 `Name`（上限 30）挪到 `OfferContainer` 真树上那颗 `name` 的 `Label` | 把 `Shell/OfferContainer.cs:1082` 的 `LabelFit(…, 42f, 46f)` 那**两个实参去掉**（或 `42f` 写 `0`）⇒ `LabelFit` 里 `maxPx` 退回 `fontPx`（= 标称 `g.NameFs`）⇒ `Geo778`/`GeoSmall` 那 **16 份**量出 **36.7** ≠ 42 ⇒ **红**（3 份 `Geo391` 标称本来就是 42，那 3 条照样绿 —— 牙口在这 16 份上） |
| **Menu 1**（日志 `21046`，`:3957`「排位窗还在」） | 清场**改回按类型**（`CloseModalPopups()`，两类宿主一起覆盖） | 把 `:3907-3911` 那两句退回 A416 那一版（`wmFixB.popUpWindow.Close()`）⇒ 那一刻它 = 排位窗 ⇒ 排位窗被关 + `NotifyClosed` 摘出 `openWindows` ⇒ 「排位窗还在」**红** |
| **Menu 2**（日志 `21342`，`:153` 吸收层 `found=false`） | **不单独修** —— 它是红 1 的下游（窗被 `SetActive(false)` ⇒ `HitQuad` 的 `isActiveAndEnabled` 门槛把整棵子树的钮全刷掉） | 同上（红 1 一红，这条**必然**跟着红；本轮日志里两条同时出现，正是这个因果顺序） |

**补充的「静默口」守卫**（两处都有）：清完之后 `if (wm == null || AnyModalPopupLeft(wm)) Debug.LogWarning(…)`。
判据 = `WindowsManager.popUpWindow` **仍指着 `PopUpGameWindow` / `PromptPopup`** ⇒ 要么有宿主不在这两类里
（宿主又换了、我们的清单没跟上）、要么 `Close()` 没摘表。
⛔ **没有**退回「`popUpWindow != null` 就出声」—— 那在那两处是**恒真**的（排位窗正常地占着这个字段），
一动就误报（这正是 A416 那版守卫「看着有依据」却量错东西的同族错误）。

### 2.1 复跑时该看到什么（**算出来的**，不是实测）

| 宿主 | 日志里那一行（改前） | 改动对**条数**的影响 | 改后应看到 |
|---|---|---|---|
| `ShopScene.Run` | `合计：1828 通过 / 3 失败`（`_tmp_view/shop.log:42756`） | 删掉 **6** 条（每页 2 条 × 3 页：`CheckTrue(nmLb != null)` 3 通过 + 那条 `CheckNear` 3 失败）· 新增 **19** 条 | **`1844 通过 / 0 失败`**（1831 − 6 + 19） |
| `MainMenuScene.Run` | `合计：2008 通过 / 2 失败`（`_tmp_view/menu.log:34562`） | **条数不变**（只动清场与夹具，没加/删断言） | **`2010 通过 / 0 失败`**（2008 + 2） |

⚠️ 条数对不上 ⇒ 说明有人在这两个宿主上又加了/删了别的断言（**不是**本件这条的错，照实核）。

---

## 三、没查清（如实）

1. **A416 原本想在排位窗那处清掉的那扇「模式不对」模态窗，那一刻到底在不在场**（D11 §三·1 也没能断定）。
   ⚠️ 但它**不影响**本轮的改法：按类型清**在场就关掉、不在场就是空做**，两种都安全
   （`Close()` 首句 `!gameObject.activeSelf` 早退，且 `FindObjectsByType` 默认**不找 inactive**）。
2. **`ShowPreviousWindow` 那一刻挑中了哪一扇**（列表尾是谁）—— 日志里没有。本轮改法**不依赖**它：
   关掉的若是非顶窗（`wasTop=false`）⇒ 连 `ShowPreviousWindow` 都不会跑，排位窗纹丝不动。
3. ⚠️ **新增的 19 条断言本轮没跑过**（用户口径：自检留到收尾）⇒ 「实得正好 42.00」是**推导**，
   不是实测（推导链见 §1.2；同一条读口在 `Timer Text` 上已被日志证实可用）。**真值以同步点那次 `ShopScene.Run` 为准。**
4. 两处清场的**覆盖类型清单**（`PopUpGameWindow` / `PromptPopup`）是照 `Shell/WindowsManager.cs` 的
   `ShowPopUp`/`ShowMessagePopUp` 文档注释与 `Shell/` 目录下现存的 `GameWindow` 子类取的；
   **没有**逐个生产调用点去追「还有没有第三类模态宿主」（本轮没查）。

---

## 四、顺手发现（**只报不改**）

1. 🔴 **同一族的「按 `popUpWindow` 清场」写法还在另外两处**（本件白名单外，**没动、也没查证**它们当时装着谁）：
   * `Editor/CollectionScene.cs:2488` —— `if (wmFixC != null && wmFixC.popUpWindow != null) wmFixC.popUpWindow.Close();`
     （它在**改被测的窗**之前还是之后？若那一刻 `popUpWindow` 指向集合窗自己，就是同一条红）
   * `Editor/RewardsScene.cs:4424` —— `if (wm2.popUpWindow != null) wm2.popUpWindow.Close();`
     （⚠️ 该文件此刻由写手 **H45** 在改 —— 本件一个字没碰）
   ⇒ 建议：按本件的判据（**那一刻 `popUpWindow` 里可能是被测的窗**）各派一次复核，**先查后改**。
2. `Editor/CollectionScene.cs:2514` 读 `wmFixD.popUpWindow` 当**探针**（不是清场入口）—— 同族但语义不同，
   **不确定**是否也需要换（未细读）。
3. `Editor/MainMenuScene.cs:3899` 那句「同 `:401` 那个口子」里的坐标**已经漂了**（`menu_dump` 时代的旧行号）
   —— 本件顺手把两处清场的注释都改成了**写死语义、不写行号**；这一句留着没动（不是本轮的事）。
4. `Editor/ShopScene.cs:2596-2602`（`Timer Text` 的 max/min）已经用 `Label.FontSizeToPx(FontSizeMax)` 这个读口
   —— 本轮新增那条（`:2577`）**是它的同形**，将来若要收口成共用助手，这两处是一对（本件没做，避免动到别人刚落的代码）。
