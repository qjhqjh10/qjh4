# EW3 · ChatPopupPanel 六钮文字的淡入淡出（既有缺陷 · 铁律 11）

> 白名单：`Battle/ChatPopupPanel.cs` + 本报告（新建）。⛔ 未跑 Unity · 未动 git · 未改两份正本。
> ✅ 秒级类型检查：`TMPDIR=/tmp/wf_ew3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> （中途有一次跑到 **8 条错、全在 `Battle/TutorialOverlay.cs`** —— 那是**别的代理当场在写的半成品**
> （`ImageQuad`→`HudPiece` 迁移中，⛔ 不在我白名单、我一个字节没碰）；隔 60 秒重跑**已归零**，
> 且全程 **`ChatPopupPanel.cs` 零错**。按简报那条：错全落在不是你负责的文件上 ⇒ 不是你的问题。）

---

## ① 结论

**改了 —— 「6 颗钮的文字也参与淡入淡出」。**

一句话理由：原版的淡入淡出不是「逐件 tween」而是 **`DOFade(this.group, …)` 作用在根节点自己身上的那个 `CanvasGroup`**
（`VoiceLinesPopupSelector.Show/Hide` + `MB_4429.group → CanvasGroup_3770`，而 `CanvasGroup_3770.m_GameObject = 1015`
= 托管 `MB_4429`/`RT_2881` 的 `GameObject/ChatPopup.json`），`CanvasGroup.alpha` 乘的是**整棵子树每一个 `Graphic`**、
且 `m_IgnoreParentGroups = false` ⇒ **那 6 个 `Text (TMP)`（都是 `TMPro.TextMeshProUGUI`，都是那棵树的后代）原版确实跟着淡**。
我们漏了 `_btns[i].text` ⇒ **与原版不符 → 按铁律 11 补掉**（R1 §④-4 记的就是这条）。

---

## ② 改动清单

**文件：`Unity/MyGame/Assets/CardPresentation/Battle/ChatPopupPanel.cs`**（只此一个文件；行号 = 改后现读）

| 位置 | 改前 | 改后 |
|---|---|---|
| `:625-634`（`SetAlpha`） | 循环体一行：`for (…) { SetAlphaOn(_btns[i].bg); SetAlphaOn(_btns[i].icon); }` | 展开成 5 行块，**多一句 `SetAlphaOn(_btns[i].text);`** + 3 行出处注释 |
| `:646-676`（新增） | ——（无这个重载） | 新增 **`void SetAlphaOn(Label t)`** + 一条带完整判据的 doc（原版实读出处见 ④） |
| `:267-276`（新增） | ——（`TextAt` 之后直接是 `Create`） | 新增 **`public float TextAlphaAt(int i)`**（自检/断言用的**灭自证口**，读真挂在标签上的 `Label.color.a`，⛔ 不重算 `_alpha`） |

**改后 `SetAlpha` 覆盖的件**（= 本面板建出来的**全部可绘制件**）：
`_close`（**故意不淡，见 ⑤**）· `_bg` · `_frameTop/Bottom/Left/Right` · 每颗钮的 `bg` / `icon` / **`text`（本次补的）**。

**`git diff --numstat` 行尾核对**（⛔ 二进制数，不是 `file` 命令）：

```
改前（我动手前现读）: 409  44   Unity/MyGame/Assets/CardPresentation/Battle/ChatPopupPanel.cs
改后（我动手后现读）: 459  45   Unity/MyGame/Assets/CardPresentation/Battle/ChatPopupPanel.cs
```
⇒ 本改动 = **+50 / −1 行**（净 +49）；文件 **818 → 867 行**。
行尾：**`b.count(b'\r\n') = 0` · `b.count(b'\n') = 867`**（改前 818 行时同样是 `CRLF 0 / LF 818`）
⇒ **纯 LF、未翻车**（`io.open(p,'rb')` 现读）。⚠️ 该 numstat 相对 `HEAD`，其中**大部分**是本会话别的代理
（T09 批 3 / A12 / G5）已在同一文件里改的，不是我这一笔；我这一笔只有上表三处。

**⛔ 我一个字没动的两处**（任务点名）：
1. **世界档那 6 个 z 常量**的排布（`ZClose 0.07 … ZBtnIcon 0.02`，`SetAlpha` 之外，`:194-195`）—— 未碰。
2. **世界档位置按原版锚点的换算**（`PanelAnchorX/Y` / `PanelPivotX/Y` / `PanelOriginPx` / `ApInPanel` /
   `Place` / `Put` 的世界那一支）—— 未碰（`SetAlphaOn(Label)` 里**没有**任何位置/尺寸语句）。

---

## ③ 接线清单（白名单外谁该改哪一行）

**欠一条断言**（`Editor/BattleScene.cs` 不在我的白名单 ⇒ 只报不改）。
🔴 **落点（现核）**：`Editor/BattleScene.cs` 的 **`:12525`（`$"★ 点面板外 → 面板关掉…"` 那一行的末行）
之后、`:12526` 那个 `}` 之前** —— 即 `④ 点面板外 → 关` 那一段 `Check(!pop.Visible, …)` 的**下一行**
（那时面板刚淡完、`gameObject` 已 `SetActive(false)`，但 `Label.color.a` 仍可读 —— 它是组件上的字段、不依赖激活态；
`:12524` = `Check(` 起始行、`:12525` = 文案行、`:12526/:12527` = 两个收尾 `}`、`:12530` = `drv.SetReplayPaused(false);`）：

```csharp
// ⑤ 🔴 EW3（既有缺陷补 · 铁律 11）：6 颗钮的**文字**也参与淡入淡出（原版根上的 CanvasGroup 乘整棵子树）。
//    灭自证：漏掉 `SetAlpha` 里那句 ⇒ 它会**恒等于 1**，而 `pop.Alpha` 照常走到 0。
for (int i = 0; i < ChatPopupPanel.ButtonCount; i++)
{
    float ta = pop.TextAlphaAt(i);
    Check(ta >= 0f && ta < 0.01f,          // ⚠️ `ta >= 0f` 那一半不能省：`-1`（那件没建出来）会假绿
          $"★ `EW3`：淡出后第 {i + 1} 颗钮的**字**也是透明的（实得 {ta:F2}；期望 0；"
        + "1 = 漏了 `SetAlpha` 里那句 `SetAlphaOn(_btns[i].text)`）");
}
```

**为什么这条不是自证**：期望值取自**原版判据**（`MB_4429.group → CanvasGroup_3770` 挂在 `ChatPopup` 根 ⇒ 文字跟着淡），
读数取自**真挂在标签上的** `Label.color.a`（`TMP._tmp.color`），⛔ 两者都**不**经过 `ChatPopupPanel._alpha`。
⚠️ 我把「`>= 0f`」写进了建议里 —— 否则 `TextAlphaAt` 的 `-1` 会让 `-1 < 0.01` 判真（**恒真断言会盖住真缺陷**，本会话教训）。

**（可选，更强的一条）** 淡入中途比一次「字与面板**同步**」：`pop.Show(); Step(1f/30f);` 之后
`Check(pop.Alpha > 0.01f && pop.Alpha < 0.99f, "（前提）淡入中途 Alpha 真在 0..1 之间");`
再逐颗 `Check(Mathf.Abs(pop.TextAlphaAt(i) - pop.Alpha) < 0.02f, …)`。
（⛔ 这只是「更精确」，**上面那 6 条已经能红**，别把两套都塞进去。）

---

## ④ 判据与出处

### ④-1 原版实读 —— 「该淡」（全部我自己现读 `battlearena2`，不是转抄报告）

| 事实 | 出处（现读） |
|---|---|
| `VoiceLinesPopupSelector` 序列化了一个 `CanvasGroup`（字段 `group`） | `battlearena2/MonoBehaviour/MonoBehaviour_4429.json` → `"group": {"m_PathID": 3770}` |
| 淡入/淡出 = **`DOFade(CanvasGroup, 目标 alpha, 时长)`**，不是逐件 tween | `d:/2/tools/decomp_full/VoiceLinesPopupSelector__Show.c`（`DG_Tweening_DOTweenModuleUI__DOFade(*(longlong*)(param_1+0x40), …)`，`0x40` 那个字段同函数里被 `CanvasGroup__set_interactable` 用过 ⇒ 就是那个 `CanvasGroup`）· 同 `__Hide.c` · `__OpenPopupHorizontal.c` · `__ClosePopupHorizontal.c` 四处**同形** |
| `Awake` 把 alpha 置 0（初始不可见） | `VoiceLinesPopupSelector__Awake.c`：`UnityEngine_CanvasGroup__set_alpha(*(longlong*)(param_1+0x40), 0, 0)` |
| ⭐ **那个 `CanvasGroup` 挂在 `ChatPopup`【根节点自己】身上** | `CanvasGroup/CanvasGroup_3770.json` 的 `m_GameObject.m_PathID = 1015`；而 `MonoBehaviour_4429.json` / `RectTransform/RectTransform_2881.json` 的 `m_GameObject.m_PathID` **也是 1015**；`GameObject/ChatPopup.json`（`m_Name = "ChatPopup"`，`m_IsActive = false`）的 `m_Component` = `[2881(RT), 2548, 4429(VoiceLinesPopupSelector), 3770(CanvasGroup), 4447]` ⇒ **三者同挂一个 GO** |
| 组里没有任何一层能豁免 | `CanvasGroup_3770.json`：`m_IgnoreParentGroups = false` · `m_BlocksRaycasts = true` · `m_Alpha = 1.0`（序列化默认，运行时被 `Awake` 改 0） |
| 6 个 `Text (TMP)` 是那棵树的后代（**一级不多、一级不少**） | 我逐级走 `m_Children` 现读：`RT 2881 ChatPopup` → `[3500 CloseChatPopup, 3363 ChatButtons]`；`3363` → `[2860 BG, 3169 BGFrame, 3644 Buttons]`；`3644` → 6 × `ChatButton`；每颗 → `[3489 bg, 3753 **Text (TMP)**, 3115 button]`。6 个文字节点的 RT = **3753 / 3742 / 3074 / 3050 / 2821 / 3046** |
| 那 6 个节点**确实是有 alpha 的 `Graphic`**（不是装饰件） | 它们的 `m_Script` pathID `7477354737935883349` → `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-…json` 的 `m_Name = "TextMeshProUGUI"`（命名空间 `TMPro`）；该 MB 的 `m_Color = (1,1,1,1)` · `m_fontSize = 30.0` · `m_enableAutoSizing = 1`（4…30）· `m_text = "Greetings"` |

⇒ **原版这 6 行字跟着淡。** 我们漏了 ⇒ 既有缺陷、**该改**（不是「原版如此、不用改」那一档）。

### ④-2 我们现状（改前现核 · R1 给的行号已漂移）

R1 §④-4 写的是 `SetAlpha:593-599`，**本会话该文件被 3 个代理改过、行号已漂** ⇒ 我现核的是
**`ChatPopupPanel.cs:611-617`（改前）**，逐字与 R1 的描述一致：

```csharp
void SetAlpha(float a)
{
    _alpha = Mathf.Clamp01(a);
    SetAlphaOn(_bg); SetAlphaOn(_frameTop); SetAlphaOn(_frameBottom);
    SetAlphaOn(_frameLeft); SetAlphaOn(_frameRight);
    for (int i = 0; i < _btns.Count; i++) { SetAlphaOn(_btns[i].bg); SetAlphaOn(_btns[i].icon); }
}
```
⇒ 覆盖 12 件（1 色块 + 4 框 + 6×2 钮图件）、**漏 6 件（钮文字）**。表现 = 淡入时字先满不透明、淡出时字最后消失。

### ④-3 **两档都要对**（这是本次「怎么改」的全部依据）

🔑 **本件不用分档 —— 因为「淡」那一路上两档本来就共用同一个后端**：

| 档 | 文字怎么建（`Build` 现读） | `SetColor` 落到哪 |
|---|---|---|
| **世界档**（`Create(parent)` ⇒ `uguiHost == null`） | `Label.Create(transform, LabelFor(i), Vector3.zero, 1, Color.white, (0,.5), …)`（`:367-368` 那一支） | `Label.SetColor`：`_tmp != null` ⇒ 写 **`_tmp.color`**；拿不到 TMP 才走点阵（`Label.cs:268-277`） |
| **画布档**（宿主 = `"ChatPopup Canvas"`） | `HudCanvas.MakeText(…)`（`:364-366`）→ **`Label.CreateUgui`**（`HudCanvas.cs:618-626` 现读） | 同上：**`_tmp` 一定非 null** —— 拿不到 TMP 字体时 `CreateUgui` **出声并返回 null**（`Label.cs:186-195`），⛔ **不退化成点阵** ⇒ 不存在「画布档没口子」这回事 |

⇒ 我加的重载 `SetAlphaOn(Label)` **只有一份、不带任何 `_ugui` 分支**，两档**逐字同一条路**。
（对比：图件那一路必须分档，因为 `ImageQuad.color` 与 `Image.color` 是两个口、由 `HudPiece.SetTint` 收口。）

**几何/位置**：本次改动**一行位置代码都没有**（`SetAlphaOn(Label)` 里只有 `c.a = _alpha; t.SetColor(c);`）
⇒ 世界档那 6 个 z 常量与「按原版点锚摆」那两件事**不可能被本改动碰到**（任务点名要保住的两处）。

**基色不丢**：`t.color` 的 RGB 原样带上、只改 `a`；`a` 每次**由 `_alpha` 重写**（不是「在当前值上乘」）⇒ 反复调**不会 ratchet**。
钮文字的基色 = 建时给的 `Color.white`（`Build` 两处工厂的实参，`:365` 与 `:367` 现读）⇒ 基透明恒 1，
与 `SetAlphaOn(HudPiece)` 里 `q == _bg` 那个特例**同理**，但**不做第三次特判**（没有第二件自带基色）。

---

## ⑤ 没查清 / 没做的

1. **`_close`（`CloseChatPopup`）不在 `SetAlpha` 里 —— 这是【故意的】，且已现核等价**：
   原版那个节点的 Graphic 是 **`NonDrawingGraphic`**（`CloseChatPopup` GO 的组件 `MB 4409`，`m_Script.m_PathID
   = -2844744054636863780` → `bundle_Waprforge_monoscripts` 里那份 `m_Name = "NonDrawingGraphic"` 现读），
   **不绘制、只吃射线** ⇒ `CanvasGroup.alpha` 对它**没有任何视觉效果**。我们的等效件（1×1 全透明图、
   `SetTint(alpha 0)`、永不变）视觉上同样不参与 ⇒ **不需要跟着淡**。（⛔ 别为了「对称」把它加进去 ——
   加了它也只是白写一次 `SetTint`。）
2. **本波没跑任何自检、没进游戏**（铁律 12：本波只保留秒级类型检查 + 「原版 vs 我们」的缺口普查）。
   ⇒ ③ 那条断言**还没落地**；本件的**画面**验收（淡入淡出看起来对不对）也**还没做**。
3. **世界档那 6 个 z 常量与画布档 `UZ*` 的「值本身」**我只做了「未被本改动触碰」的核对，
   **没有独立复核它们与原版子件序的对应** —— 那是 `A12` 那轮的账，`R1` 已判「通过（见 ⚠️ 四框 tie）」。
4. **`Label.SetColor` 在【点阵后端】的代价**：那一支会 `SetText` 重烘贴图（`Label.cs:268-277`）。
   `canvasGroup.alpha` 那一档原版是 O(1) 的，我们在「TMP 不可用」的机器上会变成每帧 ×6 次重烘。
   ⚠️ **这是 `Label` 既有行为**（`SkillPanel` / `Tooltip` / `TutorialOverlay` 每帧淡的扇都走它），
   **不是本波引入**、本波**没动** —— 如实记在这里，⛔ 不猜「实际会不会卡」（本项目 `TmpFont.Available`
   常态为真 ⇒ 走的是 `_tmp.color` 那一条）。
5. **我没有独立复核「原版这 6 个字在 `CanvasGroup` 之外还有没有别的 alpha 通道」**（例如
   `EverguildButtonMaterialModifier` 那类改材质的组件）—— 我读了它的名字但**没读它的方法体**，
   ⛔ **不知道它有不管 alpha**。若它有，本改动仍是对的（`CanvasGroup` 那一路照乘），
   但那会是一条**另一个方向的差异账**（见 ⑥-2，**待查**）。

---

## ⑥ 顺手发现（只报不改）

1. ⚠️ **原版 `ChatPopup` 根上还挂着 `TransformScalerBySmallScreenUI`（`menuScale = 1.3`），我们一扇都没实现。**
   出处：`GameObject/ChatPopup.json` 的组件列表里有 `MB 4447`，它的 `m_Script.m_PathID = 3361136396530371980`
   → MonoScript `m_Name = "TransformScalerBySmallScreenUI"`；该 MB 唯一的序列化字段 `menuScale = 1.2999999523162842`。
   行为（`decomp_full/TransformScalerBySmallScreenUI__Initialize.c` / `__LateUpdate.c` 现读）：
   `Initialize` 只在「`MenuScale != 1f` **且**某个静态开关为真（`*(char *)(*(longlong *)(DAT_18427be00 + 0xb8) + 0x11c)`）
   」时才 `enabled = true`，否则关掉；`LateUpdate` 把 `transform.localScale` **乘上 `menuScale`**（并缓存上一帧值防抖）。
   🔴 **这是跨件的账、不止本扇**：我 `battlearena2` 全场景普查 ⇒ **10 个节点**挂它，
   `menuScale` = `1.12 · 1.20 · 1.25 ×5 · 1.30 ×3`。⇒ **请主对话分流**（本波只报不改）。
   ⚠️ **我没查清**：那个静态开关到底是什么条件（小屏阈值？画质档？），以及**我们的非 16:9 路径是不是已经把
   它等效掉了**（`T09` 正在做的锚点/子画布迁移有可能覆盖了同一件事）—— ⛔ **不猜**，请查证后再决定立账方式。
2. ⚠️ **`Text (TMP)` 的 GO 上还挂着两个我们没建的组件**：`EverguildTextController`
   （`m_Script` `8686975098242931618`；序列化 `maxFontSize: 31, minFontSize: 4`）与
   `EverguildButtonMaterialModifier`（`-9096086197743689236`，**无序列化字段**）。
   而 TMP 自己的 `m_enableAutoSizing = 1`、`m_fontSizeMax = 30`、`m_fontSize = 30`（GO 89 那颗现读）。
   ⇒ **`31` 与 `30` 两个上限并存**（一个是 TMP 自己的、一个是那个控制器的）—— 我们实现是**硬写 30**（`SetGlyphHeight(30f)`）。
   🔴 **本轮不判缺陷**（⛔ 我没读 `EverguildTextController` 的方法体，不知道它是在 `Awake` 改 `m_fontSizeMax`
   还是做别的事）⇒ **只报，待查**。
3. ✅ 顺手确认（**不是缺陷**）：原版 `ChatButton` 每行的 `Text (TMP)` 的 `m_RaycastTarget = 1`
   —— 与本文件头部已记的「第 4 步要一起翻 `raycastTarget`」那条**同源**，⛔ 不另立账。
4. ✅ 顺手确认（**不是缺陷**）：原版 `ChatPopup` 的 `m_IsActive = false` + `Awake` 置 alpha 0
   —— 与我们的 `Build()` 末尾 `HideImmediate()` **同义**，无差异。

---

## 可直接粘进总表的一行

`| EW3 | ChatPopupPanel 6 钮文字淡入淡出（既有缺陷 · 铁律 11） | ✅ 已改（`SetAlpha` 补 `SetAlphaOn(_btns[i].text)` + 新增 `SetAlphaOn(Label)`/`TextAlphaAt`）；判据 = 原版 `MB_4429.group → CanvasGroup_3770` 挂在 `ChatPopup` 根、`m_IgnoreParentGroups=false`，6 个 `TextMeshProUGUI` 是其后代 ⇒ 原版确实跟着淡。⛔ 欠 `BattleScene.cs:12532` 后那条断言（接线清单见 EW3 报告 §③）；未跑自检/未真机。 | 报告 `资料/普查产出_1016并行/EW3_ChatPopup淡入修_报告.md` |`
