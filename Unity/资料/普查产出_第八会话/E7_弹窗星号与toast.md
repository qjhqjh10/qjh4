# E7 · 弹窗星号与 toast（`A1049`/`A1091` + `A1050`）

> 白名单内**实际写了 2 个文件**：`Shell/MessageToast.cs`（**新建**，200 行）· `Shell/DeckInfoPopup.cs`（+39/−4）。
> ⛔ 没跑 Unity（红线）· ✅ 秒级类型检查跑了两次（`TMPDIR=/tmp/wf_e7`，改前/改后都是 **0 / 0**）· ⛔ 没碰 git 的写操作（只 `diff --numstat` / `status` 只读）。
> 行尾：新文件纯 LF（`b'\r\n' = 0`）· `DeckInfoPopup.cs` 改前 `CRLF 0 / LF 1577` → 改后 `0 / 1612`（**未翻**）。

---

## ① 结论（两笔各一句）

| 笔 | 结论 |
|---|---|
| **`A1049` → `A1091`**（`**` 会不会印出来） | 🔴 **会印**（判据见 ②·A）。🔴 **并且「原版也这么印」这个前提【不成立】** —— 原版本地资源里**一个 `**` 都没有**（`assets_full` 全树 json/txt/csv **0 命中**），原版那条链**也没有任何 `**`→标签 的转换**。⇒ **那 27 条星号是【我们自己的】**（其中 1 条连英文列都是我们译的、其余 26 条两列都是我们写的）。按简报的判据「会印 **且** 原版不带 ⇒ **该去掉**」。<br>⛔ **本笔没改** —— 修法**必须**改 `Core/Loc.cs`（**黑名单，本波另有写手**）⇒ 按红线**停手**，把清单列在 ②·C 交主对话落。 |
| **`A1050`**（补 toast 通道） | ✅ **做完了**：新建 `Shell/MessageToast.cs`（外壳侧 toast 通道，宿主 = **复用** `Battle/ErrorMessageBanner.cs`，即原版 `UIMessageController`），`Shell/DeckInfoPopup.cs` 的 `Share` 从「模态弹窗」**换成 toast**。<br>⚠️ **一处已知偏离如实标注**（条心 y 差 213.6 px，根因在 `Battle/` 里、不在本批白名单）→ ③·2。<br>⚠️ **另一个宿主改不了**（`Deck/DeckRuntime.cs` 在黑名单）→ ③·3。 |

---

## ② 第一笔：`**` 星号

### ②·A 「会不会印」—— 逐跳实读（不是看函数名猜）

| # | 落点 | 读到的东西 |
|---|---|---|
| 1 | `Shell/WindowsManager.cs:1492-1501` | `ShowPopUp(text, …)` → **转调** `ShowMessagePopUp(text, okText, …)`（`:1496`） |
| 2 | `Shell/WindowsManager.cs:1547-1556` | `ShowMessagePopUp` → `PopUpGameWindow.Create(…)`（`:1554`）/ `win.Configure(…)`（`:1559`） |
| 3 | `Shell/PopUpGameWindow.cs:371` | `_msgLb = MenuDraw.Text(winNode, msgR, Term(_msgKey), …)` —— **正文那一颗 = `MenuDraw.Text`** |
| 4 | `Shell/PopUpGameWindow.cs:295` / `:319` | 复用/刷新那一支 = `SetLabelText(_msgLb, Term(_msgKey))` → `lb.SetText(s)` **原文传下去** |
| 5 | `Shell/MenuDraw.cs:1753`（`Text`）→ `:1787`（`TextCore`） | `TextCore` 直接 `Label.Create(parent, text, …)` ⇒ **不转换、不预处理** |
| 6 | `Battle/Label.cs:1011-1017` | `SetTextTmp(string text) { _tmp.text = text; … }` ⇒ **原文赋值**（三跳都不转换） |
| 7 | `Core/TmpFont.cs:184-213` | `NewText` **没有**动过 `richText` ⇒ TMP 出厂 `richText = true`；而 **TMP 的富文本只认 `<…>`** ⇒ `**` 逐字画在屏幕上 |

**另一个展示口也同结论**：联机页状态行 = `SettingsWindow.cs:746` `_statusLabel.SetText(_flash + "\n" + 会话状态)` —— 同样是 `Label`（TMP 那条路）。
**第三个**：「怎么联机」弹窗 = `Shell/SettingsWindow.cs:3034` `wm.ShowPopUp(t, Loc.T(lkOk), null)` ⇒ 回上面第 1 跳的同一条链。

### ②·B 「原版自己带不带星号」—— 只读核（简报要求的那个问题）

| 查法 | 结果 |
|---|---|
| `D:/2/新解包资源/assets_full/`（4.4 GB / 24.7 万文件）全树搜 `**`，**只搜文本类**（`--include=*.json --include=*.txt --include=*.csv`） | **0 个文件命中** |
| 同一条命令的**对照**（证明搜法有效） | 搜 `Touch input` → 命中 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_187991213036175270.json` ✅ |
| `D:/2/tools/il2cpp_out/stringliteral.json`（26,507 条字符串常量） | `**` 命中 **9 条，全是引擎/日志串**：`"**"`(:41743) · `"**ANY CAMERA**"`(:41983) · DOTween 的 `*** Cancelling operation ***` / `**************** Starting new turn…` / `**********` 等 —— **没有一条是 I2 词条值** |
| **最强的一条**：`MainMenu/PurchasePremium/Description` —— 我们表里**自己的注释**（`Core/Loc.cs:1136`）写着「它**既是 prefab 原文**、也是我们该印的那句」 | 它的 **EN 列**（`Loc.cs:1144-1146`，= 原版 prefab `m_text` **逐字**）**一个 `**` 都没有**（原文是 `…you get this bonus for ALL of them!`）；星号**只在我们自译的 ZH 列**（`Loc.cs:1142`） |
| 原版那条**链**有没有把 `**` 转成标签 | `decomp_full/PopUpGameWindow__SetText.c` 逐句：`localizeTexts != 0` ⇒ `I2_Loc_LocalizationManager__GetTranslation(text,…)` → 然后**间接调 TMP 的 `set_text`**（`(**(code**)(*plVar1 + 0x558))(plVar1, param_2, …)`）⇒ **整条链只有「查词条 + 原文赋值」，没有任何 markdown/加粗处理**。全仓 `Markdown` / `ToRichText` 也 **0 命中**；`<b>` 在整个 `stringliteral.json` 里 0 命中（只在 `dump.cs:1013207` 的 DOTween 日志前缀里出现过一次） |

⇒ **结论：原版不带、也不转**。我们那 27 条星号是**我们自己写进去的**（不是「抄进表时带进来的原版写法」——
而是**从我们自己的 C# 字面量搬进表时带进来的**，`A1057` 那句「它们就是调用点 C# 字面量里本来就有的字符」**说的是这个**）。

> 🔴 **与既有裁定的冲突（请调度台裁，我没动）**：`A1091` 的裁定写着「**没有 ⇒ 原版就是逐字打印 ⇒ 保留现状**，
> 并在代码注释里如实标注『**原版也这么印**』」。本件查出 **「原版也这么印」这句是错的**（原版根本没有 `**`），
> 所以那条裁定的**前提**不成立。而 `A1057` 的裁定「星号一律保留」是基于「它们是原版文本里的字符」——
> 由 ②·B 那三条看，**这些串是我们自写的**（26/27 连 EN 列都是我们写的）。⇒ 两条裁定的共同前提都塌了。

### ②·C 修法清单（**本笔不修，交主对话落** —— 落点在黑名单 `Core/Loc.cs`）

**改法（一句话）**：把下面 27 条键的 **ZH / EN 两列里所有 `**`（成对的两个字符）删掉**，**其余字符一字不动**
（⛔ 不是删整句、⛔ 不是加 `<b>`、⛔ 不是加一个 markdown→TMP 转换口 —— 那都会变成「我们挑的」）。
**实测：27 条键 · 26 条「两列都有」+ 1 条「只有 ZH 有」 · `**` 共 146 处（= 73 个强调对）。**
⚠️ 行号是**现读**（`Core/Loc.cs` 此刻正被另一个写手改，**行号一定会漂 ⇒ 按 key 找**）。

| 键 | 行 | 哪一列 | `**` 处数 |
|---|---|---|---|
| `MenuDeck/Error/EffectOnlyCard` | 1005 | ZH+EN | 2+2 |
| `MainMenu/PurchasePremium/Description` | 1140 | **只有 ZH** | 2+0 |
| `MenuDeck/Error/ImportNotPersisted` | 1227 | ZH+EN | 2+2 |
| `Settings/General/RedeemCodeUnavailable` | 1252 | ZH+EN | 2+2 |
| `Settings/Online/PublicAddress/Mismatch` | 1268 | ZH+EN | 4+6 |
| `Settings/Online/PublicAddress/BothOk` | 1270 | ZH+EN | 2+2 |
| `Settings/Online/HowToConnect/PublicDirect` | 1293 | ZH+EN | 6+6 |
| `Settings/Online/HowToConnect/DontUseTestSite` | 1295 | ZH+EN | 8+8 |
| `Settings/Online/HowToConnect/PublicV6No` | 1302 | ZH+EN | 6+6 |
| `Settings/Online/HowToConnect/UpnpNote` | 1308 | ZH+EN | 2+2 |
| `Settings/Online/MatchCancelled` | 1372 | ZH+EN | 2+2 |
| `MenuDeck/Error/WrongGameModeDeck` | 1381 | ZH+EN | 2+2 |
| `MenuDeck/Error/NoUsablePrebuilt` | 1400 | ZH+EN | 2+2 |
| `Settings/Online/Lobby/PlayedVsBot` | 1609 | ZH+EN | 2+2 |
| `Settings/Online/Lobby/StartAfterCancel` | 1613 | ZH+EN | 2+2 |
| `Settings/Online/Lobby/MissedCancel` | 1615 | ZH+EN | 2+2 |
| `Settings/Online/Lobby/PeerCancelled` | 1617 | ZH+EN | 2+2 |
| `Settings/Online/Lobby/ModeMismatch` | 1619 | ZH+EN | 2+2 |
| `Settings/Online/Cancel/WhyStarted` | 1630 | ZH+EN | 2+2 |
| `Settings/Online/Echo/NoEcho` | 1639 | ZH+EN | 2+4 |
| `Settings/Online/Upnp/NoResponse` | 1655 | ZH+EN | 2+2 |
| `Settings/Online/Upnp/NoService` | 1657 | ZH+EN | 2+2 |
| `Settings/Online/Upnp/PortTaken` | 1659 | ZH+EN | 2+2 |
| `Settings/Online/Upnp/Rejected` | 1663 | ZH+EN | 2+2 |
| `Settings/Online/Upnp/Cgnat` | 1665 | ZH+EN | 4+4 |
| `Settings/Online/Upnp/Ok` | 1667 | ZH+EN | 2+2 |
| `Settings/General/LangHasNoTable` | 1733 | ZH+EN | 2+2 |

> 🔴 **订正一处计数（铁律 5）**：`A1091` 与 `W_设置窗双语线_四笔.md:114` 都写「**26 条**」—— **现读是 27 条**
> （多的是 `Settings/General/LangHasNoTable`，`Loc.cs:1733`；另几条分列的计数也一并列在上面了）。
> `A1049` 原始记录写的「14 条」也过时。
> 📌 复算办法（可复现）：`Core/Loc.cs` 里 `new Entry(` 共 **429** 处；本件写了一个**按 token 切（跳过注释/字符串转义）**
> 的扫描器逐个 `Entry` 取字符串参数，再按 `**` 是否出现、以及参数里有没有汉字（判 ZH/EN 列）分类 ⇒ 27 条。
> ⛔ `grep -c '\*\*' Loc.cs` 出来的 **812** 是**含注释**的数，**不能当条目数**。

---

## ③ 第二笔：`A1050` toast 通道

### ③·1 判据（原版）

| 事实 | 出处 |
|---|---|
| 分享那条提示 = **`UIMessageController.ShowMessage(键, localize:1)`** | `decomp_full/DeckInfoPopup__ShareDeck.c:16-24` 三句：`MakeDeckString()` → `GUIUtility.set_systemCopyBuffer` → `UIMessageController__ShowMessage(...)` |
| 🔴 **尾参那个 `0` = IL2CPP 的 `MethodInfo*`，不是第 4 个实参** | `dump.cs:104624/104628/104632` 三个重载：`ShowError(string,bool)` · `ShowMessage(string,bool)` · `private ShowMessage(string,bool,Color)`；旁证 = 另 3 处 `ShowError` 调用点都写成 `(this, 串, 0, 0)`（`DeckEditingWindow__OnCardClick.c:27` · `__CheckCardDrag.c:92` · `DeckInfoPopup__EditDeck.c:40`）⇒ 第 3 个 `0` = `localize`，第 4 个恒是 `MethodInfo*` |
| ⇒ **色档 = `messageColor`**（场景值 `(1,1,1,1)` 白），**不是** `errorColor` | `dump.cs:104590/104596` + 两场景 `MonoBehaviour` 实读 |
| 原版这颗控制器**两个场景都有**（同一个类）：13 个 `battlearena*` + **`scenes_mainmenuwarpforge`**（主菜单） | 全库 14 份 `GameObject/UI Error Message Controller (MUST BE ENABLED).json` + 14 份含 `errorMessageItemRef` 的 `MonoBehaviour` |
| 主菜单里它的**父节点 = `Safe area Only Horizontal`** | `bundle_scenes_scenes_mainmenuwarpforge`：`RectTransform_1206`(控制器根) `m_Father = 1540` = `Safe area Only Horizontal`；链往上 `MainMenu_38`(1114) → `Main  Canvas`(1514) → `Game UI`(1314)，**四层全 stretch 满屏、`ap=(0,0)`** ⇒ 横幅上缘贴安全区顶 |
| 🔴🔴 **【2026-10-09 就地订正（铁律 5）】原来这条写「两场景的子树逐字段相同，只有根自己的 `m_AnchoredPosition` 不同」—— **不成立**。实读共差 **6 项**，其中 **item 的 `m_SizeDelta`（战斗 `1439.16×80` vs 菜单 `1310×50`）【直接决定位置】**（容器 `VerticalLayoutGroup` 的 `m_ChildControlHeight = 0` ⇒ uGUI 取**子件自己的 `sizeDelta`** 当堆叠步长 ⇒ 条心 = `根上缘 + RootH − 项高/2`） | 根 `sd` 都是 `1920.699951171875 × 336.6400146484375`；容器 `RT 1205` = `anchor(0,0)-(1,0)` · `ap(0,133.14500427246094)` · `sd(0,266.2900085449219)`；条目 `MB 2087` = `appearScaleFromMultiplier 0.3` · `appearAnimationTime 0.15` · `timeToStartFading 2.0` · `timeToFade 0.25`；**根的 `ap`：战斗 `(0,−213.5997314453125)` / 菜单 `(−0.00010299999848939478, 0.0)`**。另 4 项实读差：文字 `m_fontSize` **60 vs 36**（`MB_3740`/`MB_1740`）· 文字 `sd.y` **56.849998474121094 vs 34.11000061035156**（`RT_2942`/`RT_1207`）· `Background` `sd` **870.98×60.85 vs 618.59×38.11**（`RT_2989`/`RT_1204`）· item 宽 1439.16 vs 1310 |
| ⇒ 条心（自上而下）：**【订正后】战斗 `510.23974609375` px · 菜单 `311.6400146484375` px**（差 **198.5997314453125** = 根那档 213.5997 − 项高那档 15） | ⚠️ 原记「菜单 `296.63974609375`（差 213.6 px）」**已作废**。**硬旁证（不靠算法推）**：菜单那条 item 的**存档 `ap.y = −241.29000854492188`** 正是 h=50、N=1 时布局会写出的值（`−(266.2900… − 50) − 50×0.5`）；若 h=80 该是 `−226.29`。且菜单那份 item 的 GO 是 `m_IsActive = true`（战斗模板 `false`）⇒ **布局真跑过** |
| 原版**词条键** = `MenuDeck/Share/ExportSuccesful` | `d:/2/tools/il2cpp_out/stringliteral.json:98759`（同族三条 `MenuDeck/Share/{Title,ShareOnAlliance,ShareOnGlobal}` 紧邻；⚠️ 原版自己把 `Successful` **拼成一个 s**） |
| ⚠️ **`m_IsActive`**：13 个战场是 `true`，**主菜单那份是 `false`** | 两份 GO json 实读 —— **没查清**：`Instance` 是 `Awake` 里赋的静态单例（`__Awake.c` 单例守卫），`act=F` 的 GO 不跑 `Awake`；但主菜单侧有 **14 个脚本**在读 `Instance`（逐个 `grep` 过），且 `ShareDeck.c:21` 对 null 走的是**抛异常**那一支 ⇒ 运行时必然非 null ⇒ 那颗 GO 运行时必须是活的。**本地没有主菜单的实况 dump**，⛔ 不猜 |

### ③·2 做了什么 —— `Shell/MessageToast.cs`（新建）

**复用而不是重写**：宿主 = `Battle/ErrorMessageBanner.Create(host)`（= 原版 `UIMessageController` 的**整件实现**：
5 条一池 / 轮转 / 入场 0.15 s / 停 2.0 s / 淡出 0.25 s / 底图 `40k_bt_underbutton` / 字号 60 全都有，判据在它自己的文件头）。
`MessageToast` **只做外壳侧那三件**：挂到哪（照原版挂 `Safe area Only Horizontal`）、色档（`messageColor`）、谁泵。

```csharp
public static bool Show(string text, bool localize = false, string fallback = null)
    => … t._banner.ShowMessage(text, ErrorMessageBanner.MessageColor, localize, fallback);
```
· 泵：真机 `Update()` → `Step(Time.deltaTime)`；🔴 **批处理没有帧循环 ⇒ 自检走 `Step(dt)`**（专门为它留的口）。
· 自检口：`Instance` / `Banner` / `Built` / `ShowCount` / `LastShownText` / `LastRawText` / `Step(dt)` / `DisposeForTest()`。

🔴 **一处已知偏离（如实标注，非静默）**：`ErrorMessageBanner` 把**容器与条目**的位置写成**绝对屏坐标**
（`Build()` 里容器 = `FromPixel(…ContTopY…) − transform.localPosition`；`RelayoutActive()` 里条目 = `_container.InverseTransformPoint(FromPixel(960, centerY))`）
⇒ **从外面挪不动**（改宿主节点的位置对条目的世界坐标没有影响，我验过这条不变量：`InverseTransformPoint` 之后世界点仍是目标点）。
⇒ 本通道的横幅落在**战斗那一档**（条心屏顶下 **510.23974609375** px），而原版**主菜单**是 **296.63974609375**（**差 213.6 px**）。
**要消掉它只能改 `Battle/ErrorMessageBanner.cs`**（把 `ContTopY` / `ItemApY` / 根位从 `const` 改成可传的「位置档」字段）——
`Battle/*.cs` **不在本批白名单** ⇒ 如实上报，⛔ 没自己发明一套坐标。

### ③·3 调用的改动 —— `Shell/DeckInfoPopup.cs`（+39/−4）

| 落点（改后） | 改前 | 改后 |
|---|---|---|
| `:1429`（`:1388 ShareDeck` 内） | `if (Manager != null) Manager.ShowPopUp(DeckRuntime.ShareCopiedText(s.Length) + "\n\n" + s, Loc.T("MainMenu/General/OK"), null);` | `MessageToast.Show(ShareSuccessTerm, true, DeckRuntime.ShareCopiedText(s.Length));` |
| `:1438`（新增） | — | `public const string ShareSuccessTerm = "MenuDeck/Share/ExportSuccesful";`（原版键，**拼写照抄**） |
| `:1392-1397`（doc 新增一段） | — | 「第 ③ 句现在是**同一形**了 —— 改前不是」+ 另一宿主仍没接上 |
| 文件头 `:32-34` | 「`Share` = 写剪贴板 + 弹一条提示」 | 补「🔴 2026-10-09（`A1050`）：第 ③ 句现在与原版**同形**（走 `Shell/MessageToast`）；**改前**是 `Manager.ShowPopUp(…)` = **要玩家点 OK 的模态弹窗**（那句『我们是模态弹窗』的标注就是它），已按铁律 5 就地改掉」 |

**两处有意保留/差别（如实标）**：
1. 🔴 **文案照原版的形状**：键 = 原版键、`localize = true`；键**不在本地词条表**（6 条 `MenuDeck/Share/*` 全无 value，值在远端 I2）
   ⇒ 今天恒走**兜底句**（`DeckRuntime.ShareCopiedText`，两个宿主共用的那一份文案）。⛔ 不会印键名（`ShowMessage` 有 `fallback` 就不出声印键）。
2. ⚠️ **不再把卡组串印出来**（旧弹窗印了）—— 原版那条提示**只有一句话**（串已经进系统剪贴板了）；旧写法是「让玩家自己抄」的权宜做法。
3. ⛔ **`Share On Chat` 那一颗 `ShowPopUp` 没动** —— 那是「面板没建」的出声（`A1045`），原版那条走的是**开面板**、不是 toast。

### ③·4 那个「另一个宿主」—— 改不了，如实上报（黑名单）

`Deck/DeckRuntime.cs:3892-3894`（**`Deck/*` 在黑名单**）写着：
> ⚠️ 原版第 ③ 句（那条 toast）**不在本函数里**：两个宿主的出声通道不同
> （卡组编辑 = `Say()` / Info 弹窗 = `WindowsManager.ShowPopUp`）⇒ 各出各的，但**文案共用** `ShareCopiedText`

**这句现在有一半过期**（Info 弹窗那半已经是 toast 了）。建议改成：
> …两个宿主的出声通道不同（卡组编辑 = `Say()` / Info 弹窗 = `Shell/MessageToast`，2026-10-09 `A1050` 起与原版同形）…

⚠️ 另外：`DeckRuntime.cs:3927 ShareDeckString()` 那颗（D35 删件后**没有生产入口**）走的是 `Say()`
—— 而 `Say()`（`:3114-3123`）在 D35 之后**只写日志、屏幕上什么都不画**（`Debug.Log("[Deck] " + _noticeText)`）。
⇒ 原版那个动作在这一侧**屏幕上没有任何提示**。**归调度台**（不在本批白名单）。

---

## ④ 没查清的部分

1. 🔴 **主菜单那颗控制器的 `m_IsActive = false`** —— 与「14 个脚本读它的静态 `Instance`」+「null 会抛异常」两件事对不上
   （②·A 那张表最后一行）。**本地资源推不出解释**：搜过 `assets_full` 全树的 `UI Error Message Controller`（14 份，只有场景、**没有 prefab**）
   与 `errorMessageItemRef`（14 份，同上）；`mainmenualwaysloaded` / `menusharedresources` 等包里**没有第二份**。
   ⇒ 记「没查清」，**没有拿猜测填空**。要判只能进原版实况（`真Play待验清单` 那一类）。
2. ⚠️ **toast 的条心 y 差 213.6 px**（③·2）—— 根因已查清（常量写死在 `Battle/ErrorMessageBanner.cs`），
   但**改法要动 `Battle/`**（黑名单）⇒ 未做。
3. ⚠️ **`MenuDeck/Share/ExportSuccesful` 的 value 拿不到**（远端 I2）⇒ 今天恒走兜底句。
   若主对话要把它加进 `Core/Loc.cs`（**黑名单，我没动**）：键名 = `MenuDeck/Share/ExportSuccesful`（出处 `stringliteral.json:98759`），
   **EN 值本地查不到**（⛔ 不猜）；ZH 可用现成的 `ShareCopiedText` 那一句（⚠️ 它带运行期字符数，与「词条值」形状未必一致 —— 要加请先裁这个形状问题）。
4. ⚠️ **批处理里 toast 会「停一拍再走完」**：`Update()` 在 `-batchmode` 下不跑 ⇒ 自检必须显式 `Step(dt)`。
   `MessageToast.Step` 就是那个口，但**我一条断言都没写**（`Editor/*.cs` 全部在黑名单）。
5. ⚠️ **`Shell/MessageToast.cs` 的 `.meta` 还没生成** —— 本批不跑 Unity，下次 Unity 导入时自动生成（本仓每个 `.cs` 都有 `.meta`，属正常流程）。

---

## ⑤ 顺手发现的东西（**一处都没改**）

1. 🔴 **没有第二个 toast 设施 —— 但原版确实有另一套叫 `Toast Notification` 的东西，⛔ 别混**：
   `d:/2/tools/decomp_full/` 里有整族 `ToastNotificationController__{ShowNotification,ScheduleNotification,…}.c` +
   `ToastNotification__.c`；主菜单场景里也真有一个节点 **`Toast Notification Controller → Holder`（634.5×752.6）**
   （`资料/主菜单_原版规格.md:68,73,238`，6 个 prefab 根：`Mission Toast Notification` / `Toast Collection Level Up` /
   `Toast` / `Toast Collection Get XP` / `Collector Level Tooltip` / `Resource Counter Item`）。
   ⇒ 那是**任务/等级/经验的飘窗**那一族，和**分享提示**（= `UIMessageController`）是**两个不同的系统**。
   `资料/阶段二_Shell_原版规格.md:180` 也早写着「`Tooltip` / `Toast` / `UI Error Message Controller` / `LoadingMenu`
   这些**壳还没建**」⇒ **壳侧那两套（`Toast Notification Controller` 与 `UI Error Message Controller`）都还没建**，
   本件只补了后者（分享那条判据要的就是它），⚠️ 前者仍是**一笔没开的账**。
2. ⚠️ `SupportMethods__TurnToAsterisk.c` —— 名字里带 Asterisk，与星号无关（是打码用的），**排除过**，免得别人再查一遍。
3. ⚠️ `Core/Loc.cs` 此刻**正被另一个写手改**（`git status`：`+23/−6`）⇒ ②·C 那 27 条的**行号会漂**，请按 key 改。

---

## 需要哪几条自检覆盖（宿主估计）

| 改动 | 宿主 | 理由 |
|---|---|---|
| `Shell/MessageToast.cs`（新建）+ `Shell/DeckInfoPopup.cs` | **`ShellScene.Run`** | `MessageToast.Ensure` 靠壳根找 `Safe area Only Horizontal`；`ShellScene` 是唯一建壳的宿主 ⇒ 通道能不能挂上只在这里验得出来 |
| 同上 | **`MainMenuScene.Run`** | 分享的**真实入口**在卡组线（主菜单族的窗口），且这颗挂 `DontDestroyOnLoad` 的壳上 |
| 同上 | **`CollectionScene.Run`** | 卡组线那一族的窗口宿主（`DeckInfoPopup` 的调用点所在族）—— ⚠️ **若要补「点名 → 断 toast 文案/条数」的断言，落点会在这里**（`Editor/*.cs` 全部不在我的白名单，本件没写） |
| ⛔ **一条都不用跑**的是第一笔 | —— | 第一笔**零代码改动**（只读核查），跑什么都不会变 |
