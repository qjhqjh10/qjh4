# B8_BI —— `CardPresentation/Shell/**` 剩余 B 行往下推（**28 条真升 A** + 2 条改判据仍留 B + 1 个读口 + 1 条 B 行行为差异新查）

> 执行代理 `B8_BI` · 2026-10-26 · 白名单 = `CardPresentation/Shell/**` + `账/_parts/B8_BI.tsv` + 本报告
> ⛔ 未跑 Unity / 未动 git / 未改主表 / 未改正本 / 未越白名单。
> `TMPDIR=/tmp/wf_b8bi bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（每改一次 `.cs` 都跑过）。

---

## 一、结论

| 项 | 数字 |
|---|---|
| 本轮扫过的 `Shell/**` B 行（`菜单_外壳` 108 + 收藏 6 + 卡组编辑 7 + 商店 16 + 锻造 32 + 战役 1 + 轮抽 7 = **177**，另有 2 个域（`日常_奖励` 89 + `个人资料_成就_排行` 206 = 295 行）落点也在 `Shell/`，但**不属本轮派单域**，未动；这两个域里 `?` 类无 `.cs` 落点的 9 行不计） | **177** |
| **真升 A** | **28** |
| 改判据后仍留 **B**（写清缺什么，并**新查出一条真行为差异**） | **2** |
| **加的读口** | **1**（`ToastNotifications.InternalShowCount`，调度台已批准） |
| **加的门面方法** | **2**（`PrebuiltDecks.DeckName` / `HasHidden` —— 上一路点名要的）＋ **1**（`GameWindowWithTabs.OpenTabsForTest`，只为让 `OpenTabs` 的断言行得通） |
| 账补丁 | `账/_parts/B8_BI.tsv` = **30 行**（28 A + 2 B）· 11 列 / **纯 LF** / 每行 11 字段 · `merge --dry` = **更新 30 · 新增 0** |
| 代码改动 | 3 个文件，**全部追加在类尾**（零行号漂移）：`ToastNotifications.cs` +5 行 · `PrebuiltDecks.cs` +27 行 · `GameWindowWithTabs.cs` +3 行 |

**三条闸（在**临时副本**上跑真工具，主表未被碰过）**

| 闸 | 结果 |
|---|---|
| `check_ledger_points`（真工具，`LEDGER_DIR` 指向临时副本） | `菜单_外壳` A **307 → 333**（通过 332 / **失败 0**）· `卡组编辑` A **241 → 243**（通过 242 / **失败 0**）。两处「漂移」都是**本轮之前就有**的（`OpenWindowButton.OpenWindow` · `DrafModeDeckInfoCardListPanel.OnEnable`），不是我加的 |
| `verify_annot`（真工具） | **缺注解 0**（同名 747 · 异名已注解 2470） |
| `check_a_vs_diff`（真工具） | 我这两张表 +28 条 A 行后，**新行 0 命中**（唯一含我类名的是既有行 `PrebuiltDeck._003CGetDeckCardback_003Eb__26_0`，与本轮无关） |
| `check_patch_conflicts` | ✅ 没有跨补丁重复键 |

> 复跑办法（**只读主表、写临时目录**，不改任何工具）：脚本在 `_tmp_view/b8bi/`（`gen_patch.py` 生成补丁 / `selfcheck.py` 本地复刻三条闸的核心判法）。
> 真工具那三步的跑法：`cp 账/*.tsv → _tmp_view/b8bi/tmp/ledger` → `merge_ledger_parts.py --only B8_BI --parts <tmp>/parts --ledger <tmp>/ledger` → 把 `check_ledger_points.py` / `verify_annot.py` / `check_a_vs_diff.py` 各复制一份、只把 `D = r'D:/4/Unity/资料/完全复刻/账/'` 换成临时目录再跑（**工具本体未改**）。

---

## 二、逐条证据（28 条 A）

> 格式：`块 | 方法 | 判据出处 | 我们落点（行号范围） | 观测量 + 两侧数值 | 差异`。
> 判据前缀：🥇 = `d:/2/tools/decomp_full/*.c`（Ghidra，真方法体）· 🥈 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`（ILSpy，真方法体）。
> 「既有断言」= `Editor/ShellScene.cs` 里**已经写好并在跑**的断言（本轮把观测量挂在它上面）；「草稿」= 见 §四。

### 2·1 `UIImageMaterialColorChanger`（7 条 · 原版 666 处实例）

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 | 差异 |
|---|---|---|---|---|
| `Awake()` | 🥈 `:21-24` · 🥇 `__Awake.c:1-5`（首行名 `UIImageGreyscaleController__Awake` = **ICF 折叠**，体只有 `enabled=false`） | `:88`（Awake）· 入口 `Attach` `:78-86` | `enabled` / `Running`：原版 `Awake` 后 `enabled==false`；我们 `Attach` 后两项都 false。**既有断言** `:10122` | 逐句对过（1 句）无差异；多出的 `Attach` 是「本仓无 prefab 挂载」的等价入口 |
| `DoColorChange(...)` | 🥈 `:53-81` · 🥇 `__DoColorChange.c:26-80` | `:140-185`（三个重载 + 核心） | 材质色 + `InstancedMaterial`：`instant=true ⇒ 材质色=目标色` 且原材质未动。**既有断言** `:10124-10128` · `:10151-10155` | ① 原版读写 `Image.material`、我们 `Renderer.sharedMaterial`；② `time<=0` 原版算 NaN 色、我们直接落终值；③ 无该属性原版静默、我们出声一次 |
| `Initialize()` | 🥈 `:95-101` · 🥇 `__Initialize.c:1-20` | `:90-115`（EnsureMaterial） | `sharedMaterial` 换成新实例、原材质不动。**既有断言** `:10126-10128` | 原版一台 Image 一颗组件；我们允许同 Renderer 多颗**共用一份**实例材质 + 引用计数（卡面要同时补 `_Outline`/`_ShadowColor`） |
| `OnDestroy()` | 🥈 `:26-29` · 🥇 `__OnDestroy.c:1-12` | `:215-220` | 销毁组件后那份材质变成 Unity 假 null。**草稿①** | 原版无条件 `Destroy`；我们引用计数「最后一颗才销毁」+ 编辑模式 `DestroyImmediate` 分岔 |
| `SetColor(...)` | 🥈 `:83-88` · 🥇 `__SetColor.c:1-16` | `:188-196` | 材质属性色：`SetColor(green) ⇒ green`。**既有断言** `:10144-10145` | 原版假定实例材质已建（为空会走 `FUN_1803f47a0` 抛）；我们先建再写 |
| `Stop()` | 🥈 `:90-93` · 🥇 `__Stop.c:1-5`（同样 ICF 折叠，体 `enabled=false`） | `:199` | `Running`/`enabled`/材质色：`Stop()` 后 `enabled==false`、颜色**停在**当前值。**草稿②** | 逐句对过（1 句）无差异；多出的 `_running` 只是自检读口 |
| `Update()` | 🥈 `:31-51` · 🥇 `__Update.c:1-45` | `:201-213`（Update → Step） | 三拍材质色：首拍取 `t=0`、第二拍 `t=0.5 ⇒ Lerp`、到点写终值并自停。**既有断言** `:10134-10143` | 我们用注入 `dt` 的 `Step` 代替 `Time.deltaTime`（批处理无帧循环）；**先算 t、后累加**的顺序逐句照抄 |

### 2·2 `UIScaleToFit`（9 条 A · 原版 183 处实例）

> 🔴 一个漂亮的交叉证据：`decomp_full/UIScaleToFit__Start.c` / `__OnEnable.c` / `__SetLayoutHorizontal.c` /
> `__SetLayoutVertical.c` **四份的首行名全是 `UIScaleToFit__Start`** —— 它们的机器码**完全相同**（都是一句 `Fit()`）⇒ **ICF 折叠**。
> 这不是「文件名与内容不符」，而是「四个方法体一样」的**直接机器码证据**。判据口径：首行名不是自己时先看体，体与被请求的方法语义一致 ⇒ 折叠、不是错。

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 |
|---|---|---|---|
| `Awake()` | 🥈 `:28-53` · 🥇 `__Awake.c` | `:89` → `:94-103` | `fit.targetRect == 父节点`。**既有断言** `:10231` |
| `Fit()` | 🥈 `:108-182` · 🥇 `__Fit.c:30-67` | `:124-169` | `localScale`：400×200 框装 100×100 ⇒ **2**；`Flat(-100,-50)` ⇒ **1**；`Multiplier 0.25` ⇒ **0.5**；无目标 ⇒ 不写且 `LastScale=NaN`。**既有断言** `:10233 / :10245 / :10252 / :10258` |
| `InitializeComponents()` | 🥈 `:84-106` · 🥇 `__InitializeComponents.c:1-32` | `:94-103` | `targetRect` / `Rect`。**既有断言** `:10231` |
| `OnEnable()` | 🥈 `:55-58` · 🥇 `__OnEnable.c`（ICF） | `:91` | `localScale`：启用后按父框算。**草稿③** |
| `OnRectTransformDimensionsChange()` | 🥈 `:184-188` · 🥇 `__OnRectTransformDimensionsChange.c` | `:175` | 改父框后 `localScale` 跟上。**草稿③** |
| `SetLayoutHorizontal()` | 🥈 `:192-195` · 🥇 `__SetLayoutHorizontal.c`（ICF） | `:172` | 调它之后 `localScale`。**草稿③** |
| `SetLayoutVertical()` | 🥈 `:197-200` · 🥇 `__SetLayoutVertical.c`（ICF） | `:173` | 同上。**草稿③** |
| `Start()` | 🥈 `:49-52` · 🥇 `__Start.c:1-5` | `:90` | `localScale`。**草稿③** |
| `Update()` | 🥈 `:73-82` · 🥇 `__Update.c:1-30` | `:105-113` | `localScale` + `hasChanged`：脏了才跑、跑完清脏。**草稿③** |

**共同差异（都在文件头「结构性适配」段）**：本仓无 uGUI 包 ⇒ ① `DrivenRectTransformTracker`（`Awake`/`InitializeComponents` 各两句、`OnDisable` 一句）零对应物；② `OnRectTransformDimensionsChange` 原版是「`MarkLayoutForRebuild` 标脏 → 稍后重建 → 再 Fit」，我们直接 `Fit()`（终态相同、时差一拍，而那一拍在本仓不存在）；③ 我们把「目标/自身尺寸 ≤ 0」这一档**出声一次**（原版两道保护是静默的）—— 我们的父节点常是 0×0 锚点，必须让调用方看见。
**`Fit()` 的算式是逐句核过的**（`__Fit.c:29-45`）：`Flat ⇒ +padding`、`Multiplier ⇒ ×padding`、`s = min(目标宽/自身宽, 目标高/自身高)`、`|s| < 0x7f800001 && ≠INFINITY` 才继续、`|s| ≥ Mathf.Epsilon×8` 才写。

### 2·3 `TabButtons`（3 条）

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 | 差异 |
|---|---|---|---|---|
| `AssignTabButton(tab, toggle)` | 🥈 `:409-438` · 🥇 `__AssignTabButton.c` | `:181-185` | `options` 条数 +1、`ButtonFor(tab)` 反查到那颗键。**既有断言** `:10345-10346` | 原版 `TabButton{tab, Toggle toggle}`；我们 `Option{type, WindowButton button, tab}` —— 前两个是历史字段（`BuildBar`/`ChatPanel` 在写），追加语义逐句相同 |
| `RemoveTabButton(tab)` | 🥈 `:440-466` · 🥇 `__RemoveTabButton.c` | `:189-205` | `ButtonFor(tab)==null` 且键被销毁、找不到静默返回。**既有断言** `:10351-10353` | 匹配条件：原版只比引用 `o.tab==tab`；我们放宽到「引用 **或** 类型」（历史条目无原版 `tab` 字段） |
| `ToggleInteractable(tab, state)` | 🥈 `:468-492` · 🥇 `__ToggleInteractable.c` | `:209-220` | `wb.Interactable`：false ⇒ 置灰不可点、true ⇒ 恢复。**既有断言** `:10347-10350` | 承载件：原版 `Selectable.interactable`、我们 `WindowButton.Interactable`；匹配条件同上 |

### 2·4 `GameWindowWithTabs`（5 条 A）

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 | 差异 |
|---|---|---|---|---|
| `ChangeTab<TTab>()` | 🥈 `:276-306` | `:140-150` | 返回值 / `CurrentTabView`。**既有断言** `:10317-10319` | 查不到时原版静默返回 null、我们出声 |
| `GetStartingTab()` | 🥈 `:207-274` · 🥇 `__GetStartingTab.c` | `:133-137` | 新窗 `OpenTabs()` 后 `CurrentTabView == tabs[0]`。**草稿⑤** | ① 本仓无 `GameWindowWithTabsOptions` 资产 ⇒ 原版 `startTab` 那条分支到不了；② 回落写法原版 `FirstOrDefault()` 会把 `tabs[0]`（哪怕空）返回，我们跳过空项 |
| `GetTab(Type)` | 🥈 `:337-361` · 🥇 `__GetTab.c` | `:165-177` | 返回值 = `tabs` 里第一条可赋值的。**既有断言** `:10315` | 原版第二步扫**缓存字典**、我们扫 **`tabs` 活表`**；`type==null` 原版会进 `ReflectionHelper.Is`、我们直接 null |
| `GetTab<TTab>()` | 🥈 `:313-335` | `:159-162` | 与 `GetTab(Type)` 同一条。**既有断言** `:10316` | 逐句对过（2 句）无差异 |
| `OpenTabs()` | 🥈 `:187-205` · 🥇 `__OpenTabs.c` | `:120-130` | `CurrentTabView==tabs[0]`、`OnOpen` 1 次、`OnTabChanged` 1 次、其它页不被关。**草稿⑤** | 我们多同步一份历史枚举 `CurrentTab`（原版只有对象版） |

### 2·5 `WindowTabBase`（2 条 · 共享落点 `:45-49`，方案 §4.1 允许）

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 | 差异 |
|---|---|---|---|---|
| `WindowTabBase.Setup(GameWindow)` | 🥈 `:64-68` · 🥇 `WindowTabBase__Setup.c` | `:45-49` | `tab.Window == win` + `OnSetup` 钩 1 次。**草稿⑥** | 逐句对过（2 句）无差异 |
| `WindowTabBase<T>.Setup(GameWindow)` | 🥈 `:26-50`（泛型中间层：强类型化 + `base.Setup(window)`） | `:45-49`（等价实现 = 非泛型 `Setup`） | 同上（**同一段行为**）。**草稿⑥** | 原版多一层泛型中间类把 `Window` 强类型化；我们全部页签直接继承非泛型基类，`Window` 统一返回 `GameWindow` |

### 2·6 `DemoDeckInfoSO.GetName` / `PrebuiltDeck.HasHiddenCards`（2 条 · 卡组编辑）

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 | 差异 |
|---|---|---|---|---|
| `DemoDeckInfoSO.GetName(PlayModes)` | 🥈 `:118-131` · 🥇 `DemoDeckInfoSO__GetName.c:7-27` | `PrebuiltDecks.cs:496`（**DeckName**，本轮新增门面） | 显示名：`deckId=AM_SK_1` ⇒ 官方 `PrebuiltDecks/AM_SK_1` 的 **zh =「进攻」**（`official_loc_13lang.json` 现读，10,862 键里 215 条 `PrebuiltDecks/*`）；我们 `DeckName(ById("AM_SK_1"))` **同值**。现读 **101/103** 副有官方词条 | ① 原版同一 SO 上挂 classic/skirmish 两字段、我们生成期拆成两个 `Deck` 条目；② 官方表无该键的 2 副（`DA_SK_4` · `DarkAngelsDeck7`）原版印键名本身、我们回落自译中文（**🔶 自己设计**，已在 `DeckName` 头注释写全理由）；③ deck 为空原版抛、我们返回 null |
| `PrebuiltDeck.HasHiddenCards()` | 🥈 `:256-312` · 🥇 `PrebuiltDeck__HasHiddenCards.c:16-63` | `PrebuiltDecks.cs:505`（**HasHidden**，本轮新增门面）· 筛子本体 `Tab` 的 `:346` | `All=103 · Tab=103 · NotListed=0`（`prebuilt_decks.json` 现读 `complete=103/103`）；原版 `Where(!HasHiddenCards)` 同样是 103（卡 id 全解析得出 ⇒ 无 hidden） | 原版**运行期逐张扫** `IsHidden()`；我们**生成期**一次性判一位 ⇒ 卡表在生成之后再变时我们不会跟着变；`d==null` 原版抛、我们返回 true |

---

## 三、改了哪些文件 / 哪些行 / 新增行数

| 文件 | 改动 | 新增行数 |
|---|---|---|
| `Shell/ToastNotifications.cs` | **类尾**（`WarnFancyBackgroundMissingOnce` 之后）追加 `public int InternalShowCount { get { return ShowCount; } }` + 3 行说明（`:1458-1462`） | **+5** |
| `Shell/PrebuiltDecks.cs` | **类尾**（`ResetForTest` 之后）追加门面 `DeckName(Deck)`（`:496`）与 `HasHidden(Deck)`（`:505`）+ 理由/🔶 标记（`:480-507`） | **+27** |
| `Shell/GameWindowWithTabs.cs` | **类尾**（`NotifyNotBuilt` 之后）追加 `public void OpenTabsForTest() { OpenTabs(); }`（`:317`，零逻辑、同 `TabButtons.ToggleForTest` 先例） | **+3** |

- 三个文件**行尾都是纯 LF**（`b.count(b'\r\n')==0` · 裸 CR = 0），改完逐个数过。
- **全部追加在类尾** ⇒ 账里所有按行号引用的落点（`ToastNotifications.cs:721-738 / :842-1055 / :1063-1083` 等、
  `PrebuiltDecks.cs:118 / :337 / :346 / :419`、`GameWindowWithTabs.cs:120-250`）**行号一个没动** —— 真工具 `check_ledger_points` 复跑后两表**漂移数与改前相同**（各 1 条，都是别人的旧账）。

---

## 四、断言草稿（`Editor/` 不在我白名单 ⇒ 只给草稿 + 该插在哪）

**回填口径**：`Editor/ShellScene.cs` 的「🆕 复刻第 3 轮：三个原版通用件 + 页签生命周期」那一节（`Section(...)` 在 `:10097`）。

**① `ShellScene.cs` §①（`UIImageMaterialColorChanger`）块尾（现 `:10155` 之后、`:10156` 的 `}` 之前）—— 两段**

> ⚠️ **次序**：`Stop` 那段必须在 `OnDestroy` 那段**之前**（后者会把共享实例材质销毁掉）。

```csharp
// ── Stop()：原版 `:90-93` 只有一句 `enabled = false`，**不还原**颜色
ch.DoColorChange(Color.magenta, "_Color", 1f, instant: false);
ch.Step(0f);                                   // t=0 ⇒ 起始色
Color mid = mr.sharedMaterial.color;
ch.Stop();
CheckTrue(!ch.Running && !ch.enabled, "① `Stop()` ⇒ 停补间（原版 `UIImageMaterialColorChanger.cs:90-93` 只有 `enabled = false`）");
Ck(mr.sharedMaterial.color, mid, "① `Stop()` **不还原**颜色（停在当前值；🧨 顺手写回起始色 ⇒ 红）");

// ── OnDestroy()：原版 `:26-29` = `Object.Destroy(instancedMaterial)`
var matKeep = ch.InstancedMaterial;
CheckTrue(matKeep != null, "① （前提）共享实例材质在");
Object.DestroyImmediate(ch2);                  // ch2 = 同 Renderer 上第二颗（上一段建的）
CheckTrue(matKeep != null,
          "① 两颗共用一份实例材质 ⇒ 销毁其中一颗**不**销毁材质（🧨 无条件 Destroy ⇒ 这里红）");
Object.DestroyImmediate(ch);
CheckTrue(matKeep == null, "① 最后一颗走了才 `Destroy` 材质（原版 `OnDestroy` 那一句的等价物；🧨 `Release()` 换空 ⇒ 红）");
```
**灭自证**：把 `Release()` 里的 `if (n > 1) return;` 删掉 ⇒ 第二句红（材质提前死掉）；
把最后那句 `Destroy(_instanced)` 删掉 ⇒ 第三句红（材质仍在）。

**② = ① 的第二段（同一个块，已含）**

**③ `ShellScene.cs` §③（`UIScaleToFit`）块尾（现 `:10259` 之后、`:10260` 的 `}` 之前）—— 六个生命周期入口**

> 🔴 **为什么必须 `SendMessage`**：编辑模式 / 批处理里 `AddComponent` **不会**触发 `Awake/Start/OnEnable/Update`
> （没有 `[ExecuteInEditMode]`）⇒ 任何「靠生命周期自跑」的观测量在自检里恒为 0。这是本批踩到的**通用坑**（见 §六·2）。

```csharp
// 原版这四个入口的机器码**完全相同**（Ghidra 首行名都折叠成 `UIScaleToFit__Start`）⇒ 都是「一句 Fit()」
var pgo2 = new GameObject("ScaleParent2", typeof(RectTransform));
pgo2.transform.SetParent(probeRoot.transform, false);
var prt2 = (RectTransform)pgo2.transform; prt2.sizeDelta = new Vector2(400f, 200f);
var cgo2 = new GameObject("ScaleChild2", typeof(RectTransform));
cgo2.transform.SetParent(prt2, false);
var crt2 = (RectTransform)cgo2.transform; crt2.sizeDelta = new Vector2(100f, 100f);
var f2 = UIScaleToFit.Attach(crt2);

crt2.localScale = Vector3.one; cgo2.SendMessage("OnEnable");
CheckNear(crt2.localScale.x, 2f, 2e-4f, "③ `OnEnable` ⇒ Fit（原版 :55-58；🧨 方法体换空 ⇒ 红）");
crt2.localScale = Vector3.one; cgo2.SendMessage("Start");
CheckNear(crt2.localScale.x, 2f, 2e-4f, "③ `Start` ⇒ Fit（原版 :49-52）");
crt2.localScale = Vector3.one; f2.SetLayoutHorizontal();
CheckNear(crt2.localScale.x, 2f, 2e-4f, "③ `SetLayoutHorizontal` ⇒ Fit（原版 :192-195）");
crt2.localScale = Vector3.one; f2.SetLayoutVertical();
CheckNear(crt2.localScale.x, 2f, 2e-4f, "③ `SetLayoutVertical` ⇒ Fit（原版 :197-200）");

// Update 的去抖：脏了才跑、跑完清脏（原版 :73-82）
prt2.sizeDelta = new Vector2(200f, 200f);      // 200/100 ⇒ min(2,2) = 2
crt2.localScale = Vector3.one; crt2.hasChanged = true;
cgo2.SendMessage("Update");
CheckNear(crt2.localScale.x, 2f, 2e-4f, "③ `Update`：脏标为真 ⇒ Fit");
CheckTrue(!crt2.hasChanged, "③ …跑完把 `hasChanged` 清成 false（🧨 删那一句 ⇒ 红）");
crt2.localScale = Vector3.one;
cgo2.SendMessage("Update");
CheckNear(crt2.localScale.x, 1f, 2e-4f,
          "③ 脏标已 false ⇒ `Update` **什么都不做**（阴性对照；🧨 无条件 Fit ⇒ 这里变 2 ⇒ 红）");

// OnRectTransformDimensionsChange（原版 `MarkLayoutForRebuild` 标脏；我们直接 Fit，终态同）
prt2.sizeDelta = new Vector2(100f, 100f); crt2.localScale = Vector3.one;
cgo2.SendMessage("OnRectTransformDimensionsChange");
CheckNear(crt2.localScale.x, 1f, 2e-4f, "③ 尺寸变化回调 ⇒ Fit（100/100 ⇒ 1）");
```
**灭自证**：把 `UIScaleToFit.Update` 的 `transform.hasChanged = false;` 删掉 ⇒ 第三句红；
把 `Fit` 里 `_rect.localScale = ...` 删掉 ⇒ 全组红。

**④ `ShellScene.cs` §④（页签生命周期）块尾（现 `:10353` 之后、`:10354` 的 `}` 之前）—— `OpenTabs` / `GetStartingTab`**

```csharp
// 原版 `OpenTabs()`（:187-205）：页为空 ⇒ 直接返回；当前页为空 ⇒ 取起始页 + `OnTabChanged`；最后只 `TryOpenTab()`
var w2 = new GameObject("TabsWin2"); w2.transform.SetParent(probeRoot.transform, false);
var tw2 = w2.AddComponent<GameWindowWithTabs>();
var t1 = new GameObject("T1").AddComponent<ProbeTab>(); t1.type = WindowTabType.Missions;
t1.transform.SetParent(w2.transform, false); t1.gameObject.SetActive(false);
var t2 = new GameObject("T2").AddComponent<ProbeTab>(); t2.type = WindowTabType.Forge;
t2.transform.SetParent(w2.transform, false); t2.gameObject.SetActive(false);
tw2.tabs.Add(t1); tw2.tabs.Add(t2);
int ev2 = 0; tw2.OnTabChanged += _ => ev2++;
tw2.OpenTabsForTest();
CheckTrue(tw2.CurrentTabView == t1, "④ `OpenTabs()` ⇒ `GetStartingTab()` = `tabs[0]`（原版 :207-274 的回落；🧨 取最后一页 ⇒ 红）");
CheckTrue(t1.opened == 1 && t1.gameObject.activeSelf, "④ …并 `TryOpenTab()` 了第一页（原版最后一句）");
Check(ev2, 1, "④ …`OnTabChanged` 触发 **1** 次（在 `TryOpenTab` **之前**；🧨 次序反 ⇒ 事件里读到旧 CurrentTab）");
CheckTrue(!t2.gameObject.activeSelf, "④ `OpenTabs` **不关别的页**（原版只 `CurrentTab.TryOpenTab()`；🧨 顺手 ForceClose ⇒ 红）");
tw2.OpenTabsForTest();
CheckTrue(tw2.CurrentTabView == t1 && ev2 == 1, "④ 再调一次：当前页已在 ⇒ 不重取起始页、不再发事件（原版 `if (CurrentTab == null)` 闸）");
```
**灭自证**：把 `OpenTabs` 的 `if (CurrentTabView == null)` 闸删掉 ⇒ 最后一句红（事件变 2）。

**⑤ `ShellScene.cs` §④ 之后 —— `WindowTabBase.Setup(GameWindow)`**

```csharp
// 原版 `WindowTabBase.cs:64-68` = `baseWindow = window; OnSetup();`（无去重闸）
// 前提：给 `ProbeTab`（`:1011`）加 `public int setups; protected override void OnSetup() { setups++; }`
var w3 = new GameObject("TabsWin3"); w3.transform.SetParent(probeRoot.transform, false);
var tw3 = w3.AddComponent<GameWindowWithTabs>();
var p3 = new GameObject("TabC").AddComponent<ProbeTab>();
p3.type = WindowTabType.Missions; p3.transform.SetParent(w3.transform, false);
CheckTrue(p3.Window == null, "⑤ （前提）`baseWindow` 出厂为空");
p3.Setup(tw3);
CheckTrue(p3.Window == tw3, "⑤ `Setup(win)` ⇒ `Window` 就是那一扇（原版 `baseWindow = window`）");
Check(p3.setups, 1, "⑤ …并调了一次 `OnSetup()`（原版第二句；🧨 漏调 ⇒ 红）");
p3.Setup(tw3);
Check(p3.setups, 2, "⑤ 再调一次 ⇒ 再钩一次（原版**无**去重闸；🧨 加个 `if (baseWindow != null) return;` ⇒ 红）");
```

**⑥ `ShellScene.cs` 飘窗那一节 —— 上一路 `B8_AQ` §四·④ 的草稿现在可以插了**（读口已加）
```csharp
int hit = 0, shownBefore = ToastNotifications.InternalShowCount;
ToastNotifications.RegisterNotificationHandler("t.route", new TestHandler(() => hit++));
ToastNotifications.ShowNotification(
    new ToastNotifications.Context("x", ToastNotifications.ToastNotificationType.Achievement) { ControllerId = "t.route" });
Check(hit, 1, "注册了 handler 的 ControllerID ⇒ **handler 收到**（原版 `TryShowNotification` 的 `TryGetValue` 命中支）");
Check(ToastNotifications.InternalShowCount, shownBefore, "…而且内部展示**一次都没跑**（🧨 把 handler 支挪到内部支之后 ⇒ 红）");
```
> ⚠️ 上一路草稿里的 `ToastNotifs.Type` 在仓里不存在，枚举真名是 `ToastNotifications.ToastNotificationType`（已在上面的草稿里改正）。
> 🔵 **读口名字与语义**：`public int InternalShowCount`（`Shell/ToastNotifications.cs:1461`）—— **`ShowInternal` 被进入的次数**，
> 也就是「**绕过 handler 表、真自己弹了几条**」。它与 `ShowCount` **同源**（同一处 `ShowInternal` 里的 `ShowCount++`：
> `:916`），加它只是因为原版 `TryShowNotification` 的 handler 命中支**不**走内部展示 ⇒ 断言需要一个语义明确的读口。
> ⛔ 生产代码一律不读它（只给断言）。

---

## 五、没查清的部分（⛔ 别当结论用）

1. **`UIScaleToFit` 那 6 个生命周期入口的观测量目前在编辑模式只有 `SendMessage` 一条路**。它们在**真 Play** 里是自跑的（`Awake/OnEnable/Start/Update`），但本仓自检是**编辑模式**，MonoBehaviour 消息不触发。⇒ §四·③ 的草稿是**唯一能离线验的写法**，它验的是「方法体对」，**不是**「Unity 会在正确的时机调它」。真 Play 那半边仍要一次 `ShellScene.Play` 截图/读数确认。
2. **`PrebuiltDeck.HasHiddenCards` 的筛子在现数据上恒真（103/103 complete）** ⇒ `Tab` 今天从不筛掉任何一副。要验「筛子本身」得**造一个 `complete=false` 的假 `Deck`** 再调 `HasHidden`（这条我没写成断言，因为那要动 `PrebuiltDecks.ResetForTest` 的缓存注入；建议下轮补一个 `SetAllForTest(Deck[])` 口，同 `ItemDrawer.HandleCount` 的先例）。
3. **`DemoDeckInfoSO.GetName` 的 2 副无官方词条（`DA_SK_4` / `DarkAngelsDeck7`）**：我们印自译中文，原版印的是**键名本身**（I2 `GetTranslation` 缺键返回 term）。这条是**按我们挑的做的**（已在代码里 🔶 标记）。若调度台认为必须逐字照抄原版，那要改 `Deck.DisplayName` 的回落 —— 但玩家会看到 `PrebuiltDecks/DA_SK_4` 这种字符串，**我判断不该照抄**，等裁定。
4. **`UIImageMaterialColorChanger` 的 `HasProperty` 闸**：原版没有这一句（材质的 shader 没有该属性时 `SetColor` 静默无效）。我们出声一次并**提前返回**（不建实例材质、不改色）。行为差异只在「属性不存在」这一档，且生产材质都有 `_Color` ⇒ 到不了。没在真机上验过。
5. **`GameWindowWithTabs.GetTab(Type)` 的扫描对象**：原版扫 `cachedTabs` 字典、我们扫 `tabs` 活表。我把「同一时刻内容一致」当作等价，但**没有**逐场景核过 `SetupTabs` 之后 `tabs` 是否会被就地改动（运行期 `CreateTab` 那一族还没接）。若将来接上运行期加页，这条要重新核。

---

## 六、顺手发现（⛔ 只报；已修的写明修在哪）

1. 🔴 **`EverguildButton.DragDelegate` 的形参语义与我们的登记表不一致（这条是新的，原账没写）** —— 补丁里那条（`EverguildButton.DragDelegate`，落点 `Shell/PointerLayer.cs:313`）**不在**本轮 30 行内，所以这条**只写在本报告**，请调度台转给收藏线 / 拖拽线的写手：
   · 原版 `public delegate void DragDelegate(GameObject source, Vector2 delta)`（`cpp2il/out_cpp2il_dev_ilspy/EverguildButton.cs:16`）—— 第二个形参是 **`delta`（本次位移量）**；
   · 我们的 `PointerLayer.RegisterDrag(WindowButton, Action<Vector2> onDrag, Action<Vector2> onDrop)`（`Shell/PointerLayer.cs:313`）传的是 **指针当前绝对设计 px**；
     既有断言 `Editor/ShellScene.cs:10389` 明确钉的就是绝对值（`CheckNear(lastDrag.y, 580f, …)`）。
   · ⇒ **接生产（收藏拖拽重排）之前必须定口径**：要么在派发处多给一个 `delta`，要么每个调用方自己减起点。
     ⚠️ 这条不影响本轮任何一条 A 行（那条 B 行本来就是 B）。

2. 🔴 **「编辑模式不自跑 MonoBehaviour 消息」是本批最大的隐性坑** —— `AddComponent` 不会触发
   `Awake / OnEnable / Start / Update`（没有 `[ExecuteInEditMode]`）。本批 6 条 `UIScaleToFit` 生命周期入口、
   `UIImageMaterialColorChanger.Awake` 都踩在这上面（所以 `Attach` 里要**显式**再置一次 `enabled=false`）。
   建议进 `资料/已知的坑.md`（不在我白名单）。

3. 🔵 **ICF 折叠的良性反例（建议记进反编译坑表）**：本批有 6 个方法的 Ghidra 产物**首行名不是它自己** ——
   `UIImageMaterialColorChanger__Awake.c` / `__Stop.c` → `UIImageGreyscaleController__Awake`；
   `UIScaleToFit__OnEnable.c` / `__SetLayoutHorizontal.c` / `__SetLayoutVertical.c` → `UIScaleToFit__Start`；
   `UIScaleToFit__OnDisable.c` 体里是 `NetworkingPeer__OnMessage(+0x48, 0)`。
   **这六个全是 ICF（相同机器码共用一份地址）**，不是「文件名与内容不符」：核法 = **读体**，
   体里干的事与被请求的方法**语义一致**（前五个都是一句 `enabled=false` / `Fit()`；第六个是一句 `tracker.Clear()`）。
   ⇒ 判据补一句：**首行名不是自己时，先读体再下结论；体与被请求方法语义一致 ⇒ 折叠，不是缺产物。**

4. 🔴 **`GameWindowWithTabs.DestroyTab` 的真行为差异（本轮新查，已写进补丁的 B 行）**：原版销毁当前页时
   **不清** `CurrentTab`（留悬空，之后 `OpenTabs` 会 `CurrentTab.TryOpenTab()` 打到已销毁对象上）；我们主动清成
   `null / None`。**这是我们把原版的悬空档补掉了** ⇒ 保持 B，等调度台裁定要不要照抄。

5. 🔴 **账里有一条**现存的**真失败**（不是我这轮的）：`战斗逻辑驱动.tsv` 的
   `CardScript.TriggerUprising()` 落点 `RuleEngine/Core/RuleCore.cs:13019-13057`，机检报
   **「注释行：`/// </summary>`」** ⇒ `check_ledger_points` 该表 **失败=1**。建议转给战斗逻辑驱动的写手（我越不了白名单）。

6. `头像/边框` 那批（`个人资料_成就_排行` 域 206 行落点在 `Shell/ProfileData.cs` 等）**不属本轮派单域**，我没动；
   它们的差异列大多是「类级命中；方法级对应未核」⇒ 需要单独的「逐方法核」批次。

---

## 七、给调度台的请求

1. **合并补丁**（⚠️ `--only` 写 `B8_BI`，末尾**没有**下划线）：
   ```bash
   python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry  --only B8_BI   # 已跑：更新 30 · 新增 0
   python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py       --only B8_BI
   ```
   合并完按规矩归档到 `_parts/_merged_B8_BI/`。
2. **跑 `ShellScene.Run`（9 s）** 并加 §四 的 6 条草稿（①②③④⑤⑥ 全在 `Editor/ShellScene.cs`）。
   其中 ①②③④ 有现成块可以就地追加；⑤ 需要先给 `ProbeTab` 加一个 `setups` 计数（3 行）。
   ⚠️ §四·⑥ 现在**可以插了**（`InternalShowCount` 已加）；上一路草稿里的 `ToastNotifs.Type` 已改正为
   `ToastNotifications.ToastNotificationType`。
3. **三条待裁定**：
   · §五·3 的「官方表无该键时印自译中文 vs 印键名」（我判**保持我们的**，但这是口径）；
   · §六·4 的 `DestroyTab` 要不要照抄原版的「不清 CurrentTab」；
   · §五·1 的「生命周期入口只有 `SendMessage` 能离线验」——**是否需要一次 `ShellScene.Play`** 真跑一遍这 6 个入口。
4. **两条知识请入坑表**（不在我白名单）：§六·2（编辑模式不自跑 MonoBehaviour 消息）· §六·3（ICF 折叠的良性反例与判法）。
5. **一条别人的失败**请转给对应写手：§六·5（`CardScript.TriggerUprising` 的落点落在注释行）。
6. **一条口径提醒**：本轮 28 条 A 里有 **26 条**属于「**方法级已对齐、但生产实例/接线 0 处**」的通用件
   （`UIImageMaterialColorChanger` 666 处 · `UIScaleToFit` 183 处 · `TabButtons` / `GameWindowWithTabs` / `WindowTabBase` 的页签族）。
   我按「A = 已对齐且有可观察量」的口径判了 A，观测量挂在**直接调用的既有断言 / 草稿**上，接线状态写进了 `备注`。
   若调度台的口径是「**必须生产接线才算 A**」，这 26 条要退回 B —— **请一次性回退，不要逐条议**。
