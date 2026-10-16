# FC · AlliancePanelWindow + AttackSelector 修（F7 / F8 / G1-1 / G1-2）（修波）

> 执行：修波子代理 `FC` · 2026-10-16 · 白名单 = `Shell/AlliancePanelWindow.cs` · `Battle/AttackSelector.cs` · 本报告。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改正本与任何 `任务总表.md` · ⛔ 未越白名单。
> ✅ 改完跑过 **2 次**秒级类型检查（`TMPDIR=/tmp/wf_fc bash 工具/typecheck.sh`）：两次都 **`运行时错误数: 0` / `编辑器错误数: 0`**。
> ⚠️ **引用行号一律现核**（本批 §⑥·4 记了：`BattleScene.cs` 的行号在本会话内漂过 ≥3 次，因为 `G2` 一直在写它）。

---

## ① 结论（四条逐条）

| 条 | 落点（现核） | 结论 | 一句话 |
|---|---|---|---|
| **F7** | `Shell/AlliancePanelWindow.cs` 末尾「入口」一节 | ✅ **做了** | 「🔴 待接线」那一段就地订正成「✅ 已接线」+ 三件逐条给现核落点 + 留痕；**同一错话的副本也一并清**（文件头 `:64` 那句「由收口代理接线」） |
| **F8** | `Shell/AlliancePanelWindow.cs:286-301` | ✅ **做了**（⚠️ 有一处**有据的收窄**，见下） | `OnCanvas` 从 `return _ugui;`（= 建件开关）改成**逐件走节点的 `GetComponentInParent<Canvas>()`**；⚠️ 只查「**活性不受数据驱动**」的 3 件（根 + 那一图一字），**理由见 ② 与 ④** |
| **G1-1** | `Battle/AttackSelector.cs:390-400` | ✅ **做了** | 补 `public const string SubCanvasName = "Drag Attack Selector Canvas";`（**27 字符，逐字节核过** = `FrameName + " Canvas"`）；白名单外该改的那一处 → §③ |
| **G1-2** | `Battle/AttackSelector.cs:402-417`（`_host` 的 doc） | ✅ **做了** | 「我们**不许**给它起一块自己的画布」+「并进哪一块**待主对话裁**」两句就地订正为**现行裁定**（另起 `"Drag Attack Selector Canvas"`，`OverHand`/order 0，排 `SubCanvasRank` 的 `OverHand` 段最前）+ 留痕 + 错因 |

**F8 为什么「收窄」而不是把 14 件逐颗 `Find` 一遍**（这是本件唯一一处**偏离字面指令**的地方，先摆判据再摆结论）：

- **判别力一分不差**：`Build` 里那 **7 图 + 7 字全部建在 `_root` 这棵子树里**（每个 `Node`/`Img`/`Txt` 的 `parent` 都是 `_root` 或它的子件）⇒ 它们**共用同一条父链**：根不在画布上，那 14 件一件都不可能是在。
- **而多查那 11 件会引入一个假答案**：`TitleHolder` / `Alliance` / `NotInaAllianceText` 那三棵的**显隐是数据驱动的**（`Apply` 按「有没有称号 / 有没有联盟」关它们，**单机 bot 恒走空串那一支**）。`GetComponentInParent` 的**无参**重载默认 `includeInactive = false`（判据 = IL，见 ④）⇒ 对**没激活**的件，它答的是「**此刻可不可见**」而不是「**建在哪个空间**」。一起纳进来 ⇒ `OnCanvas` 会在「对手没称号」时变假，而唯一的消费者正是拿它**选左缘公式的档**（`BattleScene.cs` 的 `left985`）⇒ 会把**画布档判成世界档**（假红）。
- ⇒ 本件只查 **`_root` / `_background` / `_nameLb`** 三件（都是「建出来就一直激活」的），**两档语义下答案都对**；余下 11 件由「同父链」覆盖。⛔ 这不是「漏做」：见 ⑤·1 那条「该语义在本机**查不到**」的如实说明。

---

## ② 改动清单

**改动文件 2 个（都在白名单内）· 净 +58 行 · 全是注释 / 一个常量 / 一个 getter + 两个静态助手 · 零行为改动。**

| # | 文件 : 行（现核） | 一句话 |
|---|---|---|
| 1 | `Shell/AlliancePanelWindow.cs:64-66` | 文件头「上游调用点 `BattleDriver.cs:15084` **不在本件白名单** ⇒ ……由收口代理接线」→ 改成 **✅ 已接线（`G1`）** + 现核 `:15272-15286` + 「旧行号已漂」留痕 |
| 2 | `Shell/AlliancePanelWindow.cs:73-77` | 文件头那两条**本文件行号**自陈（`:370-372`/`:486-488`）**改成按内容指路**（原号因本件注释改动而漂；⛔ 别再往本文件补行号） |
| 3 | `Shell/AlliancePanelWindow.cs:255-301` | **F8**：`OnCanvas` = `OnCanvasOf(_root) && OnCanvasOf(_background) && OnCanvasOf(_nameLb)`；新增两个静态助手 `OnCanvasOf(Transform)` / `OnCanvasOf(Label)`（形状照另三扇，判据只此一份）；doc 里写清「为什么原来是自证」「为什么只查这三件」 |
| 4 | `Shell/AlliancePanelWindow.cs:750-774` | **F7**：「🔴 待接线」整段 → 「✅ 已接线」+ ①接线 ②`SubCanvasRank` ③断言两处，各带现核落点 + 原句原文留痕 + 「这几个号还会漂，按内容对」 |

> ⚠️ 上表第 1/2 条 = **F7 同一条「已接线/行号过期」类叙述的另外两处**（铁律 5：同一句话在别处的副本一起改）。我 grep 过全仓 `.cs` 的「待接线」：其余 15 处**讲的都是别的事**（`RewardsScene` / `CombatCameraZoom` / `ViewportClip` 等），**不是本件的副本**，没动。

| # | 文件 : 行（现核） | 一句话 |
|---|---|---|
| 5 | `Battle/AttackSelector.cs:390-400` | **G1-1**：新增 `public const string SubCanvasName = "Drag Attack Selector Canvas";`（与 `SkillPanel:291` / `UnitChatPanel:132` / `ChatPopupPanel:157` / `AlliancePanelWindow:137` **同形**）+ doc（判据 = `T09c` §一；⚠️ 明写「价值是**单一来源**，⛔ **不是**会拼错」） |
| 6 | `Battle/AttackSelector.cs:402-417` | **G1-2**：`_host` 的 doc 就地订正（两句过期话 → 现行裁定 + 错因 + 留痕）+ 补一句「宿主由驱动层登记，见 `UseHost`」 |

### `git diff --numstat` + 行尾核对（**已做**）

```
$ git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Shell/AlliancePanelWindow.cs \
                       Unity/MyGame/Assets/CardPresentation/Battle/AttackSelector.cs
395     43      Unity/MyGame/Assets/CardPresentation/Shell/AlliancePanelWindow.cs
354     27      Unity/MyGame/Assets/CardPresentation/Battle/AttackSelector.cs
```

- ⚠️ **这两个数**不只是本件的：**这两个文件在开会话前就已经是 `M`**（`T09` 各批写手的未提交改动）⇒ numstat 是「对 `HEAD`」的总量。**本件自己的净增量**用「改前读到的行数」算：`AlliancePanelWindow.cs` **717 → 775（+58）** · `AttackSelector.cs` **1026 → 1049（+23）**。
- ✅ **判据**：`+58 / −?`（远小于 775）与 `+23`（远小于 1049）⇒ **不是整篇重写**。
- ✅ **行尾没翻**（逐字节数，`io.open(p,'rb')`）：
  - `Shell/AlliancePanelWindow.cs`：`LF 775 · CRLF 0`（**纯 LF**，与改之前一致）
  - `Battle/AttackSelector.cs`：`LF 1049 · CRLF 1049 · bare_LF 0`（**纯 CRLF**，与改之前一致 —— 我插进去的那些行**也是 CRLF**，`bare_LF = 0` 就是证据）
- ⚠️ **与简报不符的两处（如实记）**：简报说「`AlliancePanelWindow.cs` 应是 LF、`AttackSelector.cs` 应是 CRLF —— **以现核为准**」⇒ **实测正是这样** ✓（这条简报说对了，特此确认，免得下一位再核一遍）。

### `typecheck`

```
$ TMPDIR=/tmp/wf_fc bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

- 跑了两轮（第一轮在 4 处编辑之后、第二轮在补完行号类注释之后），**两轮都是 0 / 0**；过程中**没出现**「错误集中在别人的文件上」那一档（简报预告的那个现象本波没遇到）。

---

## ③ 接线清单（**白名单外、只报不改**）

| # | 落点（现核） | 该改什么 | 为什么 |
|---|---|---|---|
| **FC-1** 🔴 | `Battle/BattleDriver.cs:14651`（`var selectorCv = EnsureRankedSubCanvas("Drag Attack Selector Canvas", …)`） | 改成 **`EnsureRankedSubCanvas(AttackSelector.SubCanvasName, HudCanvas.SortingLayer.OverHand, 0)`** | **`G1-1` 的直接配套**：常量已经在 `AttackSelector.cs:400` 就位 ⇒ 这处该改成**引用**它，那串名字才有单一来源。（`G1` §③ `G1-1` 的原话就是「`BattleDriver.cs:14651` 改回引用它」。） |
| **FC-2** | `Battle/BattleDriver.cs:14630-14644`（同上的注释块） | 把那句「⚠️ **这里为什么不用 `AttackSelector.FrameName + " Canvas"` 拼**……📌 **接线清单**：更彻底的做法是给 `AttackSelector` 补一个 `public const string SubCanvasName = …`」**就地订正**（⛔ 别让「建议补常量」和「常量已补」并存） | `G1-1` 落地后那一整段的**前提已成立**；特别是 `:14643-14644` 那句「⛔ `AttackSelector.cs` 不在本波白名单里 ⇒ 没动它（它 `:391-393` 那句「我们**不许**给它起一块自己的画布」**已过期**）」——**现在已过期的是这句本身**（`:391-393` 那两句本件已订正）。 |
| **FC-3**（可选） | `Battle/HudCanvas.cs:565` 那一行（表里 `"Drag Attack Selector Canvas", // 0/14`） | **保持字面量**（⛔ 不建议改成引用常量） | 表是 `static readonly string[]`，引用常量后**反而更难一眼核**（`G1` §③ `G1-1` 已定这条口径）⇒ 只需 FC-1 那一处改引用即可。 |
| **FC-4**（一句） | `Battle/HudCanvas.cs:578-580`（表尾「重名/漏项的自查：本表每一项都必须是**某扇面板 `SubCanvasName` 的字面量**」） | **不用改了** —— `G1-1` 落地后这条自查法**不再误伤** `"Drag Attack Selector Canvas"` | `G1` §⑥·1 当时记的是「照那条自查法会把它判成『写坏了』」；现在 `grep -rn "SubCanvasName = " Battle/ Shell/` 能命中 `AttackSelector.cs:400` ✓（我核过全清单：11 个面板常量 + 表里各项**逐一对得上**）。 |

> ⛔ **本件没碰**上面任何一处（全在白名单外）。**F7 那三件的「已完成」判据我逐条现核过**，见 ④。

---

## ④ 判据与出处

### F7 —— 「三件今天都做完了」逐条现核（⛔ 不是转抄台账）

| 件 | 现核落点 | 实读 |
|---|---|---|
| ① 接线 | `Battle/BattleDriver.cs:15272-15286` | `HudCanvas.Ensure(LayoutSpace.Cam)` → `var allianceCv = EnsureRankedSubCanvas(AlliancePanelWindow.SubCanvasName, HudCanvas.SortingLayer.OverHand, 0)` → `if (allianceCv == null) { LogError(**本扇窗这次不建**、不退回世界档) } else { _alliancePanel = AlliancePanelWindow.Create(root, uguiHost: allianceCv); }` ✓ |
| ② `SubCanvasRank` 补一条 | `Battle/HudCanvas.cs:574` | `"Alliance Panel Canvas",             // 13/14` ✓（与它上头那句「将来迁 `DebugButtons`(7)/`PopupAnchor`(9)/`Tutorial`(11)/`LoadingMenu`(12) 时插在它**前面**」同在） |
| ③ 断言两处改画布档 | `Editor/BattleScene.cs:22040` · `:22094-22113` | `float left985 = nodePx985 - (panel985.OnCanvas ? 0f : panel985.NameLabel.WorldW * 54f);`（**按档**）· `RW1192` 里 `if (!lb.IsUgui) { w *= sc * pxPerWorld1192; }`（**按档取尺**）✓ |
| （附加）`G2` 新加的三条 | `Editor/BattleScene.cs:21927-21980` | `if (panel985.OnCanvas) { … `overrideSorting` / 层 / order … · … 无 `CanvasScaler` … }` + **逐件 `GetComponentInParent<Canvas>()` 灭自证**（7 颗字，`apN == 7 && apBad == 0`）✓ |

### F8 —— 「另三扇 `OnCanvas` 实际怎么写」（**照抄的形状**，均现核）

```csharp
// SkillPanel.cs（_cv = 本面板那块子画布）：
public bool OnCanvas {
    get { if (_cv == null || _cv.GetComponentInParent<Canvas>() == null) return false;
          return IsUgui(_bg) && IsUgui(_light) && IsUgui(_name) && IsUgui(_cost) && IsUgui(_desc) && IsUgui(_targets); } }
static bool IsUgui(HudPiece q) { return q != null && q.IsUgui && q.Rect != null && q.Rect.GetComponentInParent<Canvas>() != null; }
static bool IsUgui(Label   l) { return l != null && l.IsUgui && l.transform is RectTransform rt && rt.GetComponentInParent<Canvas>() != null; }

// UnitChatPanel.cs：三颗气泡逐颗走（Bubble 里 4 件）：
public bool OnCanvas { get { return OnCanvasOf(_player) && OnCanvasOf(_enemy) && OnCanvasOf(_radio); } }
static bool OnCanvasOf(Bubble b) { … IsUguiOnCanvas(b.bg) && IsUguiOnCanvas(b.portrait) && IsUguiOnCanvas(b.wave) && b.text != null && b.text.IsUgui; }
static bool IsUguiOnCanvas(HudPiece q) { return q != null && q.IsUgui && q.Rect != null && q.Rect.GetComponentInParent<Canvas>() != null; }

// ChatPopupPanel.cs：6 框 + 6 颗钮逐件走（两个 helper 逐字相同）。
```

**本扇照抄到的形状**（`Shell/AlliancePanelWindow.cs:286-301`）：

```csharp
public bool OnCanvas { get { return OnCanvasOf(_root) && OnCanvasOf(_background) && OnCanvasOf(_nameLb); } }
static bool OnCanvasOf(Transform t) { return t != null && t.GetComponentInParent<Canvas>() != null; }
static bool OnCanvasOf(Label   l) { return l != null && l.transform.GetComponentInParent<Canvas>() != null; }
```

- ✅ **同的部分**：**同一个口、同一条式子**（节点的 `GetComponentInParent<Canvas>()`）、**判据只此一份**（helper 单点）、`null` ⇒ 假、任一该有的件缺 ⇒ 假。
- ⚠️ **不同的部分（如实记，两条）**：
  1. **另三扇的式子有两半** = 「本件是 uGUI 后端」（`HudPiece.IsUgui` / `Label.IsUgui`，**建件口给的**）**且**「本件挂在画布父链下」；本扇的图/容器件是**裸 `Transform`**（本类没存 `HudPiece`）⇒ **只留父链那一半**。这一半取舍**正是 `F8` 要的**（台账原话：「与建件口同源 ⇒ 写成恒真的风险最高」）——把建件开关那一半拿掉，剩下那半才与建件口**不同源**。
  2. **只查 3 件不查 8 件**：理由见 ① 末（含「多查会把活性答成空间」这条实证），以及 ⑤·1 那条「无参重载的默认语义本机查不到」。

### ④·补 「`includeInactive = false`」这条是怎么核出来的（**本件新查，判据可复现**）

1. 本机 `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.xml` 的
   `M:UnityEngine.Component.GetComponentInParent` 那一条：**只列了形参说明**「Whether to include inactive parent
   GameObjects in the search」，**⛔ 不写默认值**（⇒ 本机文档**查不到**语义，与 `资料/普查产出_1012/H10_ViewportClip阶段1.md:46`
   当年那条结论一致）。
2. 于是**读 IL**（新写了一个 16 行的临时工具，**放在 `/tmp/wf_fc_il/`、⛔ 没进仓库**，用 `d:/2/tools/dotnet10/dotnet.exe` +
   `System.Reflection.Metadata` 读 `MethodBody.GetILBytes()`）：
   - `Component.GetComponentInParent<T>()`（无参泛型）= `00 02 D0 06 00 00 1B 28 1C 00 00 0A **16** 28 51 26 00 06 A5 …`
     —— 解码 = `nop / ldarg.0 / ldtoken T / call Type.GetTypeFromHandle / **ldc.i4.0** / call GetComponentInParent(Type,bool) / unbox.any T / ret`
     ⇒ **无参重载给 `includeInactive` 压的就是 `false`**（`16` = `ldc.i4.0`）。
   - 非泛型 `Component.GetComponentInParent(Type)` 同样压 `16`；`GameObject` 那两个同形（真身
     `GetComponentInParent_Injected` 是 `InternalCall`、**没有 IL**）。
3. ⇒ **能确定的**：默认**不把未激活的件当候选**。**不能确定的**：「链上遇到未激活件时是**跳过继续**还是**就地停**」
   —— 这两档对**本件**的差别写在 ⑤·1。⚠️ 本机另有一条**同族**先例可作交叉印证（不是判据）：
   `Shell/MenuDraw.cs:1406` 的注释「沿父链把 `Label` 取回来（**带 `true`**：未激活的页里那层父链**是关着的**，不带就取不到）」。

### G1-1 —— 「27 字符」逐字节核过（脚本产出）

```
FrameName      = 'Drag Attack Selector'          (20)
SubCanvasName  = 'Drag Attack Selector Canvas'   (27)
FrameName + " Canvas" == SubCanvasName ?  True
"Drag Attack Selector Canvas" 出现在 HudCanvas.cs 表里 ?  True
```

⇒ 与 `G1` 现核一致：**值本来就对**，本件的价值是**单一来源**。

### G1-2 —— 现行裁定的出处

`资料/任务线/战斗线/任务/T09_整层换uGUI.md` **§八·3**（该节自己已就地订正过原句「⛔ 不是另起一块画布」）：
裁定 = **另起 `"Drag Attack Selector Canvas"`（`OverHand`/order 0）、并在 `SubCanvasRank` 的 `OverHand` 段【最前】插它一条**；
判据 = 原版 `Canvas_2759`（`m_RenderMode 2` · `m_OverrideSorting False` · `m_SortingLayerID 0` · `m_SortingOrder 0` · `m_PlaneDistance 100`）
+ `T09c` §一（原版子件序 **0/14**）；**错因** = 跨画布先后**只由 `SubCanvasRank` 定**，并进 `Mulligan Canvas`(3/14) 会被
`ActiveSkillDesc`(1/14) / `Unit Chat`(2/14) **一起盖住**。本件把那两句过期 doc 换成这套口径。

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **`GetComponentInParent<Canvas>()`（无参）在「链上有未激活节点」时的语义 —— 本机查不到，我没把它当判据。**
   - 已确定：默认传 `false`（IL 实证，见 ④·补）；**未确定**：**跳过继续** vs **就地停**（web 上查得到的只有标题/链接，**本机 WebFetch 对所有域名都被拦**（网络策略），拿不到正文 ⇒ 没采信）。
   - **对本件的两个后果**（两档都写了，⛔ 不挑一个当结论）：
     · **「跳过继续」档**：本扇 `OnCanvas` 在画布档恒真（窗开着 / 关着都一样）⇒ `BattleScene.cs` 那两条子画布结构断言会跑 ✓。
     · **「就地停」档**：窗**关着**时本扇 `OnCanvas` 答假 —— 与 `G2` 那条逐件断言**同向**（那一侧也是无参重载、也答假）⇒ **不会假红**；但 `if (panel985.OnCanvas)` 那两条结构断言会被**跳过**（**覆盖变弱，不是假红**）。且该处窗口此时确实是**关着的**（`panel985.Open("测试名","","")` 在那一段**之后**才调）。
   - ⚠️ **这是跨四扇的事、不止本扇**：`UnitChatPanel` 的 6 颗件逐颗 `SetActive(false)`（`UnitChatPanel.cs:159-164`）、
     `ChatPopupPanel` 的**根** `gameObject.SetActive(false)`（`:604`/`:642`）—— 那两扇的 `OnCanvas` 已经无条件依赖这一层语义。
     ⇒ **建议**（不属本件、也不属本波授权范围）：由主对话定一次口径；若要**确定性**，把它统一成
     **`GetComponentInParent<Canvas>(true)`**（并在 `BattleScene.cs` 那条逐件断言上**同批**改，否则两处语义会分家）。
     ⛔ 本件**没有**单方面改成 `(true)` —— 那会与消费者的式子不一致。
2. ⚠️ **没跑 Unity** ⇒ 上面那些断言（含 `G2` 新增的三条）**实际绿红没验**（按现行口径也不该由本波跑）。
3. ⛔ **`AlliancePanelWindow` 那四件老缺口仍开着**（按简报要求**本件不碰**）：头像立绘 / 两处评级 / 徽章 `Frame`·`Badge` / 出场 `DOFade`+`DOScale`（三个 DOTween 常量没读）。它们仍在文件头「已知缺口 ①②③④」那一节 ✓。
4. ⛔ **`_ugui` 那个 bool 我**没有删**（建件口 `Build`/`Node`/`Img`/`Txt` 都要用它）—— 本件只是**不再拿它当自检判据**。若将来要把「建件开关」彻底私有化，那是另一件事（不在 `F8` 范围内）。
5. ⛔ **没做**（白名单外，见 §③）：`BattleDriver.cs` 那两处（FC-1/FC-2）· `HudCanvas.cs` 表里那条字面量（FC-3，且**建议保持**）· `AlliancePanelWindow.SubCanvasName` 的**上游**引用（本来就已经是引用常量 ✓，无需改）。

---

## ⑥ 顺手发现（**只报不改**）

1. 🔴 **`T09` §八·2 的行号注已过期**：那节写「**建点 = `:15084`**（`:15060` 是注释行）·`HandleAlliancePanel` 的方法声明 = `:4051`」—— 现核 **建点 = `BattleDriver.cs:15282`**（`:15084` 那个位置现在是别的东西）；`:4051` 仍然对 ✓。⇒ 那份任务文件（`任务/T0N_*.md`）**不在我白名单**，只在报告里报。
2. ⚠️ **`BattleScene.cs` 的行号在本会话内漂过 ≥3 次**（我亲历：`21775-21829` → `21927-21980`；`21902` → `22040`），根因 = `G2` 还在写那份文件。⇒ 我已在 `AlliancePanelWindow.cs` 的注释里写明「**按内容对**（`left985` 那一句 / `RW1192` 那个 `if` / `// 13/14` 那一行）」。**给调度台的建议**：本波收口时，凡引用这三份文件的落点，要么现核、要么按内容描述。
3. ⚠️ **`G1` §⑥·1 那条「自查法会误伤」现在已闭环**：`HudCanvas.cs` 表尾那句「本表每一项都必须是某扇面板 `SubCanvasName` 的字面量」在 `G1-1` 落地后**不再误伤** `"Drag Attack Selector Canvas"`（我全清单核过：11 个 `SubCanvasName` 常量 **vs** 表里各项，两项都对得上）。
4. ⚠️ **`AlliancePanelWindow._notInAlliance` 存的是 `Transform`、丢掉了 `Label`**（`Build` 里 `msg != null ? msg.transform : null`）—— 若将来要把 `F8` 那条做成「7 颗字逐颗全覆盖」，得先把它改成存 `Label`（本件**没做**，⛔ 不在 `F8` 范围内）。
5. ✅ **顺手复核到一条「已闭环」的**：`BattleDriver.cs:14640` 那句建议（补 `SubCanvasName`）**本件已经兑现** ⇒ 那句本身现在成了「历史建议」（见 FC-2，该由主对话订正，⛔ 我没动）。
6. ⚠️ **`HudCanvas.cs` 本会话还在被 `G3` 改**（我现核时它的 `SubCanvasRank` 表整体比 `G1` 报告里说的下移了 ~17 行，且多出了 `IsRanked(name)` 那个公共口 = `G1` §③ `G1-5` 那条建议**已落地**）⇒ 引它一律现核。

---

### 可直接粘进总表的一行（≤3 行）

```
[FC] Alliance 窗 + AttackSelector 修 ✅ —— F7（「待接线」→ 已接线，含文件头同句副本）· F8（OnCanvas 由 `_ugui` 改成逐件走 `GetComponentInParent<Canvas>()`，
只查活性不受数据驱动的 3 件：根+Background+Name 字 ⇒ 判别力不差、且不会把「对手没称号」答成世界档）· G1-1（补 `SubCanvasName`，27 字符逐字节核过）· G1-2（`_host` doc 两句过期话按裁定订正）。
改 2 文件（都在白名单内）净 +58 行 · 行尾纯 LF / 纯 CRLF 均未翻 · typecheck 0/0 ×2 · 未跑 Unity · 未动 git。⚠️ 待办：`BattleDriver.cs:14651` 该改成引用新常量（§③ FC-1）。
```
