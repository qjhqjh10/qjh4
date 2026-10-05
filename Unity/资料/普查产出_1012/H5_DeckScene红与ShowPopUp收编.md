# H5 · A415（`DeckScene.Run` 那条红）+ A416（`ShowPopUp` 收编）（写手 · 2026-10-12）

> 白名单内改动：`Editor/DeckScene.cs`（A415 那一节 + 新增断言）· `Shell/WindowsManager.cs`（**只有注释**，A416 的事实订正）。
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改正本 · ⛔ 没越白名单 · ⛔ 没碰 `Battle/*` / 别人的落点。

---

## ① 结论（逐件）

| 件 | 结论 |
|---|---|
| **A415** | ✅ **做完了**（`Editor/DeckScene.cs` 的「拖出侧栏 = 删除」那一节，`:3159-3198`）。口径 = **改夹具的期望** —— 原版就是「**非法卡组不进库**」（判据逐句见 §②）。旧的 `!DeckDirty` 与「盘上 = n2−1」两条 → 换成「**脏标记留着** + **盘上照旧 n2 张** + **出声（模态窗）**」，另加「补回一张 ⇒ 合法 ⇒ 这一下**才**落盘」的反证 + 收尾（收窗、把牌还回去）。🔴 **闸一个字没动、`Validate()` 的规则一个字没动**。 |
| **A416** | 🔴 **代码改动【没有落地】**。收编本身只差几行（§③ 给了**逐字现成改法**），但它是一件**天然跨 3 个自检宿主**的活，而那 3 个文件**全在白名单外**（`Editor/{ShellScene,MainMenuScene,CollectionScene}.cs`）。按铁律 13·3「**一个文件同一时刻只有一个写手**」+ 本件红线「⛔ 不越白名单」⇒ **必须先派那 3 份宿主同步改，再落那一句**。本件做了能做的两件事：**订正那句已被推翻的注释**（两处）· 把**逐行现成改法**写进 §③。<br>⛔ **这不是「影响小 / 成本高所以不做」**（铁律 11 禁那种写法）—— 是**先后**问题，铁律 11 自己写着「**只有先后之分**」。 |

---

## ② A415：口径怎么定的（判据逐句 · 铁律 2）

**出处**：`d:/2/tools/decomp_full/DeckEditingWindow__TrySaveDeck.c`（全文 112 行，本件逐句读过）

```
:80   cVar3 = DeckUtility__ValidateDeck(param_1[0x23], local_res8, 1, 0);   // deck, out err, validateOwnership:1, 0
:81   if (cVar3 == '\0') {                       // ← 不合法
:82-93    … 建两颗 GameWindowButton（左 MainMenu/General/Discard / 右 MainMenu/General/Cancel）…
:94       WindowsManager__ShowPopUp(lVar7, uVar6, 1, 0, lVar5, lVar4, 0);   // 文案键, localizeTexts:1, closeOnEsc:0
:95       return;                                                          // 🔴 绝不走到 :100
:98   }
:99   else {                                     // ← 合法
:100      DeckEditingWindow__UploadDeck(param_1, 0);
:103      WindowsManager__HidePopUp(lVar4, 0);
:104      return;
:106  }
```

旁证（同一条链的两端）：`DeckEditingWindow__ESCPressed.c:5` 那一行就是 `DeckEditingWindow__TrySaveDeck(param_1, 0)` ⇒ **`Done` 与 `ESC` 是同一个函数**；`DeckEditingWindow__UpdateDoneButton.c:24` 点亮 `Done` 用的是**同一个** `ValidateDeck` ⇒「灯亮」与「放行」同判据。

**⇒ 原版语义 = 「非法卡组不进库」**（既不落盘、也不上传），并且**出声**。
⇒ ✅ **照调度台口径做候选①**：**改夹具的期望**（盘上不变 + 出声），⛔ 不放宽闸、⛔ 不改 `Validate()` 去迁就夹具。

### 红的形状（改前 → 改后）

| | 改前（D2 §六·3 记的那两条红） | 改后 |
|---|---|---|
| `_rt.EscPressed()` 之后 | `CheckTrue(!_rt.DeckDirty)` ← **红**（闸不放行，脏清不掉） | `CheckTrue(_rt.DeckDirty)` —— **脏标记就该留着** |
| 盘上张数 | `Check(…Count, n2 - 1)` ← **红**（没落盘） | `Check(…Count, n2)` —— **盘上就该一个字节不动** |
| 出声 | （原来没查） | `CheckTrue(_rt.ModalPopupOpen)` + （页脚那行另有 A330 段盯着） |

**为什么 `n2 − 1` 那条在任何实现下都不该绿**：`n2` 是满编（30），删一张 ⇒ 29 ⇒ `Validate()` = `TooFewCards`（A330 段 `:2719` 对**同一副牌**早就断过这一条）⇒ 原版 `:95 return`。**「删一张不再合法」是本实现的固有性质**（张数必须**恰好等于** deckSize ⇒ 原版的 `IncompleteDeck`）⇒ 「删牌之后仍然合法」这一态**不存在**，「删牌 ⇒ 落盘」在物理上走不到（见 §⑥·1）。

### 收尾那三条为什么必须有（不是凑数）

1. **收窗**（`:3188-3191`）：窗开着时 `EscPressed()` 只会把**同一扇窗**再配一遍（D2 §五·5），不会落盘 ⇒ 顺序不能反；而且**模态窗留着**会让后面几节里**任何**走 `HandlePointer` / `UiClickPx` 的断言**静默退化成空断**（本工程最怕的那种「绿得没道理」）+ 盖住 `Run()` 末尾的截图。
2. **补回一张 ⇒ 合法 ⇒ 这一下才落盘**（`:3196-3198`）：这是**反证** —— 没有它，一个「保存恒被挡死」的实现也能让上面三条全绿。
3. 收尾同时把牌还给后面几节（本节原来就是把牌留在 29、盘上留 29 的）。

---

## ③ A416：为什么没落地 + 逐行现成改法

### 3·1 事实（`ShowPopUp` 的宿主到底是什么）

| 项 | 原版真值 | 我们 |
|---|---|---|
| `WindowsManager.ShowPopUp` 的宿主 | `WindowsManager__ShowPopUp.c` → `_LoadPopUpAndShow_d__59__MoveNext.c` → 按「按钮数 > 1」挑 **`popupWindowOneButton` / `popupWindowTwoButtons`** 两扇 prefab（= `MessagePopupWindow` / `MessagePopupWindow2Buttons`，类 **`PopUpGameWindow`**，在 `bundle_generalgamewindows_assets_all`，**全库只有这 2 个实例**） | **`PromptPopup`**（= `GenericPromptWindow`，`bundle_menus_assets_all`）—— **一处【已知偏离】** |
| 按钮名 | `ConfigurePopUp` 逐颗 `buttons[i].Text` 贴标签 | `ButtonLeft` / `ButtonRight`（2 钮）· `Generic UI Button`（1 钮）· `OkButton` / `CancelButton`（`PromptPopup`） |
| 正文节点名 | `MessageText` | **两边同名**（`MessageText`）⇒ 按名读的那几处**不受影响** |

🔴 **`ShowPopUp` 那句过期注释**（本件已订正）：`Shell/WindowsManager.cs` 的 `ShowPopUp` 文档段 + 文件头 ① 原来都写着「原版那两个 `popupWindowOneButton/TwoButtons` **本地没有**」—— D2 现核已推翻（两扇都在）。**同族的 `PromptPopup` 有** 只解释了「当年为什么挑了它」，**不解释「原版是哪一扇」**（铁律 5·c：一个值 ≠ 全部情况）。

### 3·2 🔴 为什么不能只改那一句（blast radius，**已全量普查**）

**普查办法**：全 `Assets` 树 grep `PromptPopup`（**按【类型】找窗/读窗，只有这一种写法**）+ grep `ShowPopUp`（调用点）⇒ 交叉出**唯一**受影响的集合：

| 宿主 | 处 | 会怎样 | 为什么 |
|---|---|---|---|
| `Editor/ShellScene.cs` | `:818` | **红** | `popup.GetComponentInChildren<WindowButton>()` 在 `PopUpGameWindow` 上**先撞上压暗层那颗吸收层**（`Build()` 第 1 步 `MenuDraw.Absorb(root,"AbsorbHit",…)`，`Absorb` 内部 `Hit(...)` 会挂 `WindowButton` 且**置 `absorbOnly = true`**，见 `Shell/MenuDraw.cs:1856-1880`）⇒ `ClickForTest()` 被 `absorbOnly` 早退 ⇒ `okFired` 恒 false |
| | `:820` `:821` | **红** | 承上：回调没触发 + `openWindows.Count` 停在 2 |
| | `:829-831` | **红** | `var pp = shell.Windows.popUpWindow as PromptPopup;` ⇒ null |
| | `:832-910` | **整段静默跳过** | ⑤b 那一整段（≈15 条）全在 `if (pp != null)` 里 ⇒ `PromptPopup` 的**版面覆盖会整体损失**（比红更糟） |
| `Editor/MainMenuScene.cs` | `:3626-3627` | **红** | （夹具）`sk.Manager.TopWindow as PromptPopup` ⇒ null |
| | `:3629-3631` | **红** | `if (promptMode != null) promptMode.Close();` 变空做 ⇒ 弹窗留着 ⇒ `sk` 停在 `Background` ⇒ 下面两组 `CheckAbsorbRule` 的 `found` 假（那一节的注释自己写着「两条红，各打两次」） |
| | `:1749` `:3851` | **静默漏关** | `FindObjectsByType<PromptPopup>` 清场循环 ⇒ 找不到新窗 ⇒ 模态窗留着**盖截图**（那里的注释写着「第一版就是这样」/「看图才发现 —— 断言全绿」） |
| `Editor/CollectionScene.cs` | `:2472-2478` | **红** | 按类型读 `MessageText` 断「…而"隐藏卡"」⇒ 找不到窗 |
| | `:2451` | **静默漏关** | 同上的清场循环 |

**⛔ 没受影响的（逐条核过，别误伤）**：
- `Editor/ShopScene.cs:1454-1455` —— 它用的是 **`wm.popUpWindow != null` + `.Close()`**（**类型无关**），照旧绿 ✅。
- `Editor/ShellScene.cs:2133` / `:2759` —— 那两处是 `PromptPopup.Create(...)` **自己建的探针**，与 `ShowPopUp` 无关 ✅。
- `Shell/MainMenuRuntime.cs:621` —— 「退出游戏」弹窗是它**自己 `PromptPopup.Create`** 的（判据全文见那里的注释：原版 `SettingsMenu.ExitGamePopup` 传 `closeOnEsc:1`，而 `ShowPopUp` 的签名里**没有** `closeOnEsc`）⇒ 与 `ShowPopUp` 的宿主**无关** ✅。
- `Editor/RewardsScene.cs:5095-5102` —— 只读 `PromptPopup.QShade / QText` 两个**静态层档常量**，与宿主无关 ✅。
- 其余 40+ 处 `PromptPopup` 命中全是**注释**（`DeckInfoPopup` / `CollectionWindow` / `Label` / `MenuDraw` …）✅。

**⇒ 那 3 个宿主全在白名单外**（`Editor/{MainMenuScene,CollectionScene}.cs` 是「别人的落点」、`Editor/ShellScene.cs` 属「其余一切」）⇒ 本件按红线**没动它们**，也就**不能**落那一句转调（落了 = 让 3 个正在被别人写的文件同时变红/丢覆盖 = 铁律 13·3 第 1 条说的「有交集」）。

### 3·3 现成改法（调度台派工时**逐字照抄**即可）

**(a) `Shell/WindowsManager.cs` 的 `ShowPopUp` 本体**（现在在 `:1003-1008`，`ShowMessagePopUp` 在 `:1050+`）：

```csharp
        public void ShowPopUp(string text, string okText = null, System.Action onOk = null,
                              string cancelText = null, System.Action onCancel = null)
        {
            // 🆕 A416：**收编**到原版那扇（宿主 `PopUpGameWindow`）—— 上面那段注释记着为什么拖到现在。
            PopUpGameWindow win = null;
            // ⚠️ 原版那扇窗**自己不关**（`PopUpGameWindow` 的两颗钮只调调用方给的委托；`TrySaveDeck` 那两个
            //    lambda 就是各自 `HidePopUp()` 的）。而本方法这 11 个调用点是按**旧宿主**的行为写的
            //    （`PromptPopup.Choose` = **先 `Close()` 再回调**）⇒ 这里照旧包一层、**逐字保持那个顺序**；
            //    ⛔ 不包的话它们点完钮窗还挂着（`Editor/ShellScene.cs:821`「只剩 1 个窗」那条也会红）。
            //    ⚠️ 第二颗钮**只要给了 `cancelText` 就一定要挂回调** —— 否则那颗钮的 `onClick` 是 null，
            //       点了**什么都不发生**（窗关不掉），与旧宿主「点 Cancel 就关」不等价。
            win = ShowMessagePopUp(text, okText,
                                   () => { win.Close(); if (onOk != null) onOk(); },
                                   cancelText,
                                   cancelText == null ? null
                                       : (System.Action)(() => { win.Close(); if (onCancel != null) onCancel(); }));
        }
```

**(b) `Editor/ShellScene.cs`**

```csharp
// :818 那一行 —— 换成「跳过吸收层」的取法（吸收层排在最前，见 §3·2）：
var btn = popup != null ? popup.GetComponentInChildren<WindowButton>() : null;
if (btn != null && btn.absorbOnly)
{
    var b2 = FindChildIn(popup.transform, "Generic UI Button");      // 1 按钮版那颗的名字
    btn = b2 != null ? b2.GetComponentInChildren<WindowButton>(true) : null;
}
if (btn != null) btn.ClickForTest();

// :829-831 —— ⑤b 这一整节**验的是 `PromptPopup` 自己的版面**（不是 `ShowPopUp` 的宿主）
//         ⇒ 直建，别再从 `ShowPopUp` 拿：
var pp = PromptPopup.Create(shell.Windows, "（自检示例文案：本窗只负责排版，正文由调用方给。）",
                            "知道了", null, null, null);
shell.Windows.OpenWindow(pp);
CheckTrue(pp != null, "`PromptPopup` 建出来了（⚠️ 它**不再是** `ShowPopUp` 的宿主 —— 原版那条走 `MessagePopupWindow`，见 A416）");
```

⚠️ 顺带：`:864-866` 那段注释「`extraScaleSmallScreen` 在我们这套里没有消费者……小屏缩放器没实现」**已过期**（D2 §六·5 早记过，A165/2026-10-06 就做完了）。不在本件白名单 ⇒ 未动。

**(c) `Editor/MainMenuScene.cs`**

```csharp
// :3626-3627 —— 夹具不需要认类型，只要「最上面那一扇」：
var promptMode = sk.Manager != null ? sk.Manager.TopWindow : null;      // GameWindow（不 as PromptPopup）
CheckTrue(promptMode != null, "（夹具）模式不对那一句真的弹了窗（下面那句 `Close()` 才不是空断）");
if (promptMode != null) promptMode.Close();

// :1749 与 :3851 两处清场循环 —— 换成**类型无关**的那一句（本仓现成先例 = `Editor/ShopScene.cs:1454-1455`）：
var wmFix0 = WindowsManager.Instance;
if (wmFix0 != null && wmFix0.popUpWindow != null) wmFix0.popUpWindow.Close();
```

**(d) `Editor/CollectionScene.cs`**

```csharp
// :2451 清场 —— 同 (c) 的替换。
// :2472-2478 —— 类型改成 GameWindow，其余照旧（正文节点两边同名 `MessageText`，读法不用改）：
GameWindow hp = win.Manager != null ? win.Manager.popUpWindow : null;
var hpTxt = hp != null ? TextOf(FindChild(hp.transform, "MessageText")) : null;
CheckTrue(hpTxt != null && hpTxt.Contains("隐藏卡"), "…并且**弹出提示说清原因**（不许静默）—— 实测文案「" + (hpTxt ?? "<没有提示窗>") + "」");
```

**(e) 注释侧（同步改，铁律 5）**
- `Shell/PromptPopup.cs` 文件头 / 类注释：`:35` 那句「也是边界③「点了如实提示」的**唯一宿主**」—— 收编后**不再是**；`:1` 的「（「暂无服务器」等的宿主）」同理。
- `Editor/DeckScene.cs` 的 A364 段注释 `:2773` 那句「与 `Shell/PromptPopup.cs` 是两扇窗，⛔ 别混」**照样成立**，不用动。
- 落完 (a)~(d) 之后，`Shell/WindowsManager.cs` 里 A416 那两段「为什么还没收编」要按 §⑧ 的口径**就地删掉**（那两段是**临时说明**，不是知识）。

**(f) 配的断言（落在白名单内的 `Editor/DeckScene.cs`，A364 段旁边）**：

```csharp
// ---- A416：`ShowPopUp` **收编之后**的宿主与「点钮 ⇒ 关窗」 ----
var wmH = WindowsManager.EnsureHost();
bool firedH = false;
wmH.ShowPopUp("A416·明文正文", "确定", () => firedH = true, "取消");
var hp2 = wmH.popUpWindow as PopUpGameWindow;
CheckTrue(hp2 != null, "★ A416：`ShowPopUp` 的宿主 = **`PopUpGameWindow`**（原版 `popupWindowOneButton/TwoButtons`）"
      + " —— 改回 `PromptPopup` ⇒ 这条红");
Check(hp2.MessageShown, "A416·明文正文",
      "★ …正文 = 调用方给的**明文**（明文不是术语键 ⇒ `Term()` 原样返回，11 个调用点一行都不用改）");
CheckTrue(hp2.TwoButtons, "★ …给了两颗钮 ⇒ 2 按钮版 prefab");
Check(hp2.PrimaryShown, "确定", "★ …左钮 = `okText`");
Check(hp2.SecondaryShown, "取消", "★ …右钮 = `cancelText`");
ClickPopupButton(hp2, "ButtonLeft");
CheckTrue(firedH, "★ …点左钮 ⇒ **回调执行了**（只关窗不回调 / 挂成吸收层 ⇒ 这条红）");
CheckTrue(!hp2.gameObject.activeSelf, "★ …而且**窗自己关掉了**（收编时漏掉那层 `Close()` 包装 ⇒ 这条红）");
```

---

## ④ 改动清单（文件:行号 · 每处一句为什么）

**`Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs`（+44 / −6，净 +38）**

| 行 | 改动 |
|---|---|
| `:3159-3171` | 新增 A415 段头：**记清口径**（判据逐句 + 「为什么原版是非法不进库」+ `n2` 为什么是满编 + ⛔ 不许放宽闸/改 `Validate`） |
| `:3172-3174` | **新增前提断言** `Validate() == TooFewCards` —— 把「夹具处在它该处的状态」钉住（不前置的话，「不放行」在一个本来就存不进去的实现下也会绿） |
| `:3175-3177` | `EscPressed()` 之后 `!DeckDirty` → **`DeckDirty`**（不改：脏标记**就该留着**） |
| `:3178-3180` | 盘上 `n2 − 1` → **`n2`**（不改：不合法 ⇒ 不进库） |
| `:3181-3182` | **新增**「出声」：`ModalPopupOpen`（原版 `:94` 那扇窗） |
| `:3184-3191` | **新增收尾①**：点右钮（`Cancel`，= 原版那颗只调 `HidePopUp()` 的 lambda）把窗收掉 —— 理由见 §②「收尾那三条为什么必须有」 |
| `:3192-3195` | **新增收尾②**：`UiAddCard` 补回一张 ⇒ 又合法 ⇒ `EscPressed()` 这一下**真落盘** |
| `:3196-3198` | **新增反证**：`!DeckDirty` + 盘上满编（谁把闸写成恒关 ⇒ 红） |

**`Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs`（+34 / −3，净 +31，⛔ 纯注释、**0 行代码**）**

| 行 | 改动 |
|---|---|
| `:28-31` | 文件头 ① 补第二次更正：`GenericPromptWindow` 有 **≠** 「原版 `ShowPopUp` 用它」；原版那两扇是 `PopUpGameWindow` |
| `:1003-1025` | `ShowPopUp` 文档段：**订正被推翻的那条前提**（原文「原版那两个 popup prefab 仍然本地没有」）+ 写清**今天仍是 `PromptPopup` = 已知偏离** + **为什么还没收编**（3 个宿主逐处点名到 `:行号`） |
| `:1043-1050` | `ShowMessagePopUp` 文档段：订正「**8 个**既有调用点」这个数字 → 实测 **11 个生产 + 2 个自检站点**（逐处列出）；并说明「调用点传的是**明文** ⇒ 收编时一行都不用改」 |

---

## ⑤ 断言（改坏法 · 逐条）

| # | 断言什么 | **怎么改会红** |
|---|---|---|
| 1 | `（前提）删到 29/30 ⇒ TooFewCards` | —— （前提条；夹具被改成「删完仍合法」时它会红，提醒下一轮换夹具） |
| 2 | ★ **不合法 ⇒ 按 Done 也不清脏标记** | `SaveAndSay()` 里那道 `if (err != DeckError.None) { … return; }` 整段删掉 / 改成 `if (false)` ⇒ 红（与 A330 段那两条**同一条实现**，两处一起红） |
| 3 | ★ **盘上照旧 n2 张** | 同上；或在那道闸里改成「不合法也 `CommitDeck()`」⇒ 红 |
| 4 | ★ **出声**（模态窗开着） | 把 `ShowInvalidDeckPopUp(err)` 那一句删掉（只剩 `Say(...)`）⇒ 红 |
| 5 | （收尾）点右钮 ⇒ 窗收掉 | 把 `HideDeckPopUp()` 从右钮回调里摘掉 / 把两颗钮的回调接反 ⇒ 红（接反还会让 #6 红） |
| 6 | ★ 反证：**合法之后同一颗 Done 照样落得下去** | 把闸写成恒关（无条件 `return`）、或 `CommitDeck()` 写坏 ⇒ 红 |
| 7 | （收尾）盘上满编 | 承 #6 —— 只把「不落盘」那半边做对、保存整个坏掉 ⇒ 红 |

**⛔ 没有自证 / 没有弱断言**：#2#3#4 三条的**另一半**（「合法时必须存得下去」）由 #6#7 与 A330 段 `:2758` 那条 `A330·闸不是恒关` 共同钉住；`TooFewCards` 这个期望来自 `DeckRules.Validate`（**原版 `ValidateDeck` 的那条**，A330 段的判据表），不是从本节的实现里读出来的。

---

## ⑥ 没查清 / 待判据（⛔ 不猜）

1. **「删牌 ⇒ Done ⇒ 落盘」这一条在物理上走不到**（不是本件漏做）：张数必须**恰好等于** deckSize（原版 `IncompleteDeck`；A330 段 `:2719` 实测 29 ⇒ `TooFewCards`）⇒ **不存在「删一张之后仍然合法」的卡组状态**（在 30 张固定制的经典/单机卡组里）。本件能用最近的形态覆盖它 = 收尾那三条（删一张被挡 + 补回一张 ⇒ 落盘）。⚠️ **若将来出现「张数是一个区间」的模式**（例如原版某些 LiveOps 模式），这一条才变成可测 —— 那时要补。
2. **A416 落地的**那一轮**必须实跑三条自检**（`ShellScene` / `MainMenuScene` / `CollectionScene`）—— 本件按红线**没跑**，§3·2 那张「会怎样」的表是**推演**（依据 = 代码路径 + `PopUpGameWindow.Build()` 的建树次序 + `MenuDraw.Absorb` 的实读）**不是跑出来的结论**。推演与实跑不符时**以实跑为准**。
3. **`PopUpGameWindow` 当 `ShowPopUp` 宿主之后的两处观感差**（记在案，不算缺陷）：
   - 面板宽高比不同（`PromptPopup` 是「宽 900 定死、高按文案长」的自适应；`PopUpGameWindow` 是 **850×430 死尺寸** + 正文框 `770×272` + `auto[4,40]`）⇒ **长文案会被缩到很小**。最长的那个调用点是 `Shell/DeckInfoPopup.cs:1302`（`ShareDeck` 把**整条卡组串**当正文 + `\n\n` 拼进去）。
     ⚠️ **但那条消息是我们自己发明的**（原版 `Share` 走服务端/平台分享，**不存在**这条弹窗）⇒ **没有原版对照**；两种宿主对它都不合适（`PromptPopup` 会算出一个比屏幕还高的面板）。**如实记，⛔ 不拿它当「不收编」的理由**；真要做的是给那一处换个能滚的宿主（另开账）。
   - 层带会从 `PromptPopup` 的 **3140-3144** 换到 `PopUpGameWindow` 的 **3560-3565**（仍在 `Core/Tooltip` 3605+ 之下 ⇒ 合法）；`Editor/RewardsScene.cs:5095-5102` 那两条断的是 `PromptPopup.QText > CampaignRewardWindow.QVignette`（**静态常量之间**），换宿主**不影响**它们。
4. **`ShowPopUp` 的 `closeOnEsc` 参数**：原版签名里有（`SettingsMenu.ExitGamePopup` 传 **1**），我们这个方法**没有**这个形参（今天的 13 个站点全是 0 ⇒ prefab 值）。唯一需要 `1` 的那一处（`MainMenuRuntime.EscPressed:621`）是**自己直建 `PromptPopup`** 的 ⇒ 收编时**不必**给 `ShowPopUp` 加形参；真要收编那一处，得连 `ShowMessagePopUp` / `PopUpGameWindow.Create` 一起开这个口（**另开账**，本件没做）。

---

## ⑦ 顺手发现（⛔ 一个都没在本件里改，只报）

1. 🔴 **「8 个既有调用点」这个数字是错的**（两处都写了 8）：D2 报告 §六·1 与 `Shell/WindowsManager.cs` 那段
   `ShowMessagePopUp` 文档（**改前在 `:1017`**）。**实测 = 11 个生产调用点 + 2 个自检站点**（清单见 §3·3 与已订正的那段注释，现在在 `:1044`）。已就地改掉 `WindowsManager.cs` 那一处（铁律 5）；**D2 报告那一处归调度台**（本件白名单外）。
2. ⚠️ **`Editor/ShellScene.cs:864-866` 的注释过期**（「小屏缩放器没实现」—— A165 / 2026-10-06 已做完，`WindowsManager.ApplySmallScreenScale`）。D2 §六·5 已记过，**现在还没人改**。白名单外。
3. ⚠️ **`Shell/WindowsManager.cs:531` 那句列了「`closeOnEsc == false` 的窗」**（`MissionRerollPopup`(0) / `PromptPopup`(0) / …）—— 收编之后 `ShowPopUp` 开的窗是 `PopUpGameWindow`（`closeOnESC = 0`，**同值**）⇒ 那句话**不用改**，但下一轮落 A416 时**顺手核一眼**（免得有人按「列了哪几扇」去反推宿主）。
4. ⚠️ **`PopUpGameWindow.Term()` 对【明文】是恒等映射** —— 这条**必须写进 A416 落地那轮的注释**（11 个调用点传的是明文，不是术语键）：它保证了两件事：① 收编**不用改调用点**；② 将来真拿到 I2 词条表往 `Terms` 里填，**明文永远不是键 ⇒ 不会误伤**。（本件已写进 `ShowMessagePopUp` 的文档段。）
5. ⚠️ **`PopUpGameWindow` 的 1 按钮版在收编后会第一次拿到【生产】消费者**（今天只有 `DeckScene.cs` 的 A364 段 `:3007` 那个自检站点点它）⇒ 落地那轮要把 §3·3(f) 的断言补齐到「只给一颗 ⇒ 1 按钮版」那一档（`ShowPopUp(text, "知道了", null)` 就是这一档，13 个站点里占 **11** 个）。

---

## ⑧ 该跑哪几条自检（📌 调度台定复跑时机）

**本件（A415 + 注释）覆盖的面**：
- 改了 `Editor/DeckScene.cs`（**该条自检自己的宿主**）⇒ **`DeckScene.Run` 必跑**（也**只**跑它）。
- 改了 `Shell/WindowsManager.cs` —— ⚠️ **它是共用件**（全壳都引用）。但本件对它**只动注释（+31/−0，0 行代码）** ⇒ **行为零变化** ⇒ 受影响集合**只有** `DeckScene.Run`。
- 没动 `Deck/DeckRuntime.cs`、没动任何 `Battle/*`、没动别的自检宿主 ⇒ 按铁律 12 判据②，**不需要**加跑 `ShellScene` / `MainMenuScene` / `CollectionScene` / `RewardsScene`。
- 纯文档（`资料/*.md`）本件还多了这一份报告 ⇒ **零条**。

**A416 落地那一轮（另一件，还没做）覆盖的面 —— 🔴 必跑 4 条**：
`DeckScene.Run`（新断言 + `ShowPopUp` 是本场景的宿主之一）· **`ShellScene.Run`** · **`MainMenuScene.Run`** · **`CollectionScene.Run`**
（按铁律 12 那三条判据：动了共用件 `WindowsManager` 的**行为** ⇒ 要 grep 谁在用；§3·2 那张表就是 grep 的结果）。
⚠️ 那 4 条**必须与 §3·3 的 (a)~(d) 一起跑** —— 只落 (a) 不落 (b)(c)(d) ⇒ 3 个宿主必红。

### 行尾复核（`io.open(...,'rb')`，⛔ 不是文本模式的假读数）

```
Assets/CardPresentation/Editor/DeckScene.cs    CRLF 3543 / LF 3543（改前 3505/3505 —— **纯 CRLF，一行没翻**）
Assets/CardPresentation/Shell/WindowsManager.cs CRLF 0   / LF 1112（改前 1081    —— **纯 LF**）
本报告 = LF（与同目录其它报告一致 —— 本件实测过一遍：那一批**全是纯 LF**、一份含 CRLF 的都没有）
```
`git diff --numstat` 对眼：`DeckScene 628/9`（本件自己那部分 = **+44/−6**，净 +38 ⇒ 3505→3543 行）·
`WindowsManager 94/3`（本件 = **+34/−3**，净 +31 ⇒ 1081→1112 行）—— 两个**总数**的大头都是
**本仓此前批次留下的在飞改动**，与删除数对眼（删除数远小于行数）⇒ **无整篇重写**。

### 秒级类型检查

`TMPDIR=/tmp/wf_h5 bash d:/4/Unity/工具/typecheck.sh` —— **0/0**
```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
