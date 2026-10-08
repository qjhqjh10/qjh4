# W · `m_RaycastPadding` 符号缺陷 + `cameraReset` 档 + 死码（2026-10-18 第三会话）

改动白名单内三件：`Battle/BattleDriver.cs` · `Editor/BattleScene.cs` · `Shell/BoosterInfoPopup.cs`（第三个**未改**，见 §③末尾）。
⛔ 全程未跑 Unity · 未动 git · 未改正本。

---

## ① padding 符号：改前 / 改后

### 判据（写进代码注释的两点独立出处）
- ① 官方单测 `Library/PackageCache/com.unity.ugui@27635d171b1a/Tests/Runtime/UGUI/EventSystem/GraphicRaycasterTests.cs:82-101`：
  `raycastPadding = (−50,−50,−50,−50)` + 指针在 **rect 外 60 px** ⇒ 断言**命中**。
- ② 官方 editor gizmo `…/Editor/UGUI/UI/GraphicEditor.cs` 的 `DrawRect`：`p0 = rect.x + offset.x` · `p2.x = rect.xMax − offset.z`
  ⇒ 负 offset **各自向外**；同一函数也服务 `RectMask2D.cs:178-183` ⇒ `m_Padding` 同符号。

### 改前（**错**）
```csharp
const float CameraResetHitPxW = CameraResetRectPxW - 16f;    // = 48.442993…
const float CameraResetHitPxH = CameraResetRectPxH - 16f;    // = 45.846008…
```
注释写着「负 = 往里缩」「口径同 `Shell/MenuDraw.PaddedHitRect`」—— **口径恰恰是反的**（那份 helper 的正确口径是「正缩负扩」）。

### 改后（**现算，不写死**）
```csharp
const float CameraResetPadPx = 8f;                                            // 单侧量（L=B=R=T=8）
const float CameraResetHitPxW = CameraResetRectPxW + CameraResetPadPx * 2f;   // 64.4429931640625 + 16 = 80.442993…
const float CameraResetHitPxH = CameraResetRectPxH + CameraResetPadPx * 2f;   // 61.84600830078125 + 16 = 77.846008…
```
⇒ 命中区 **80.443 × 77.846**（原来 48.443×45.846：每边少 8 px、面积少 ≈42% ⇒ 正是「看着在钮上、点不动」那一族）。

### 注释改写（留痕，铁律 5）
- `BattleDriver.cs:3042-3069`（常量上方那段）：改写成「四边各**外扩**」+ 「🔴 2026-10-18 就地订正：原文写『负 = 往里缩』，**符号是错的**」+ 上面两条判据 + 写法参照（`MenuDraw.PaddedRect:474` · `BoosterInfoPopup` · `CollectionScene` 的 `(−20)×4`）+ **错因**（把常识当判据）。
- `BattleDriver.cs:3141-3152`（`CameraResetButtonHit` 的 doc）：`负 = 往里缩` → `负 = 往外扩`，值 48.443 → 80.443。
- `BattleDriver.cs:11620-11623`（`BuildHudExtras` 里另一处提到「命中区 48.443×45.846 那两个常量」）：一并订正（这是**第三处**被复制到的同一句，grep 才捞出来）。

### 断言重写（`Editor/BattleScene.cs` A423 段）
- **删掉**旧判别式：`off423 = 25f/108f` + 「偏中心 25 px 打不中」—— 它钉的是错值（25 > 错半宽 24.2215 ⇒ 当时是绿的）。
- **新增两态、且互为灭自证**：
  - 半 A（**命中**）：偏中心 **36 px**（x、y 各一次）⇒ **打得中**。
  - 半 B（**不命中**）：偏中心 **44 px**（x、y 各一次）⇒ **打不中**。
- **新期望值怎么算的（现算）**：
  - rect 半宽 = 64.4429931640625 / 2 = **32.22150** px；外扩后半宽 = (64.4429931640625 + 8 + 8) / 2 = **40.22150** px。
  - y 向同带：rect 半高 = 61.84600830078125 / 2 = **30.92300**；外扩后半高 = (61.84600830078125 + 16) / 2 = **38.92300**。
  - 取 **36**：在 rect **外** 3.78 px、在命中区**内** 4.22 px（y 向：外 5.08 / 内 2.92）⇒ 两轴余量都 ≥2.9 px（≈0.027 世界单位，远大于浮点噪声）。
    ⚠️ 现核给的 **35 px** 替代档我复算过：最差余量 2.78 px，**比 36 略小** ⇒ 我取 36（**这是我按真值现算挑的，不是原版字段**）。
  - 44 同理：y 向余量 5.08 px、x 向 3.78 px。
- 断言里 ⛔ **不引用** `BattleDriver` 那两个常量 —— 36/44 是从原版字段（rect 64.443 + padding 8×2）现算的。
- 🧨 **改坏法**（写进断言文案）：① 常量写回 `Rect − 16` ⇒ 半宽 24.2215 < 36 ⇒ 红；② 不加 padding（= rect 本身）⇒ 32.2215 < 36 ⇒ 红；
  ③ 拿画出来的 `ImageQuad.Contains`（61.846² 半宽 30.923）顶替 ⇒ 红；④ 界去掉（恒 true）或 padding ≥ 44 ⇒ 半 B 红。
- **灭自证**：半 A 要求半宽 ≥ 36、半 B 要求 < 44 —— 这个窗口**结构上不可能**被「错符号」「无 padding」「无界」任何一种实现同时满足；
  且两个数与原版字段同源、不读实现常量 ⇒ **把实现与判别式一起改回旧写法不会全绿**。

---

## ① 爆炸半径表（`BattleDriver.cs` / `Editor/BattleScene.cs` 逐处）

| # | 位置 | 是不是 padding | 符号 | 处置 |
|---|---|---|---|---|
| 1 | `BattleDriver.cs:3042-3069` 两个命中常量 | ✅ `Image.m_RaycastPadding (−8×4)` | **错**（按内缩） | **已修**（外扩，现算） |
| 2 | `BattleDriver.cs:3141-3157` `CameraResetButtonHit` 文档+实现 | ✅ 同上 | **错**（文档写错符号；实现吃常量） | **已修**（文档+值） |
| 3 | `BattleDriver.cs:11620-11623` `BuildHudExtras` 注释 | ✅（同一句话的第 3 份拷贝） | **错**（只是注释） | **已修** |
| 4 | `BattleDriver.cs:2912-2918` `HandleSettings` | ❌ 走 `Contains`（画出来的内接框） | 不适用 | 未改实现；**已加注释**标明它没有 `activeSelf` 守卫（隐患非活缺陷，⛔ 不顺手补） |
| 5 | `BattleDriver.cs:2967` `HandleOffensiveButton` | ❌ `Contains` | 不适用 | ⚠️ **判不准**：那颗的原版 `m_RaycastPadding` **我没读**（不在本次判据内） |
| 6 | `BattleDriver.cs:3260` / `:3649` chat / cemetery 钮 | ❌ `Contains` | 不适用 | ⚠️ **判不准**（同上，未读那两个节点的 padding） |
| 7 | `BattleDriver.cs:6814-6864` 结束回合钮 | ❌ `Contains` | 不适用 | 同上，未核 |
| 8 | `BattleDriver.cs:12696` `HitTip` / `:12970` `HitSlot` | ❌ `Contains` | 不适用 | 同上，未核 |
| 9 | `BattleScene.cs:13015` 改坏法 ④ 文案 | ✅（引错值） | **错** | **已修** |
| 10 | `BattleScene.cs:13088-13100+` A423 命中区断言 | ✅ | **错**（钉错值） | **已重写**（36/44 两态 + 灭自证） |
| 11 | `BattleScene.cs:6216-6270` 卡牌展示窗「点击区」 | ❌ 走的是 `2DCard/UI Collider` 的 `m_SizeDelta(−0.2,−0.44)`（**拉伸锚下的负 sizeDelta**） | ✅ **对**（拉伸锚里负 sizeDelta = 缩；与 padding 是**两套机制**，⛔ 别按 padding 那套去改） | 未改 |
| 12 | `BattleScene.cs:8580-8581` 下拉框内缩 10,7 / `:10916` `m_LocalScale 1.25` | ❌ 布局/缩放 | 不适用 | 未改 |

**全仓横向核对**（只读，未改他人文件）：`Shell/MenuDraw.PaddedRect:474`「正值缩小、负值扩大」· `Shell/BoosterInfoPopup.cs:493,504`（`(−15)` ⇒ 外扩）·
`Shell/DeckInfoPopup.cs:219`（`(−20)` ⇒ 外扩）· `Editor/ShopScene.cs:2095`（`(−15)` 外扩）· `Editor/CollectionScene.cs:1981`（`(−20)` 外扩）——
**口径一致**；唯一那条**正值**的是 `DeckInfoPopup` 的 `WarlordPad (+246.8,…)`，正 = 缩，与订正后的口径自洽。
⇒ **本次符号缺陷【只】出现在 `BattleDriver.cs` 这一处**（加上被它带错的那条断言）。

---

## ② `cameraReset` 档（A964 前置）

- `HudButtonWorldPosForTest` / `HudButtonActiveForTest`：各加 `case "cameraReset": q = _cameraResetBtn; break;`（两张 switch 都对上，键表逐字同形）。
- 新增 `SetHudButtonActiveForTest(string which, bool on)`：同形键表 + **只 `SetActive`、⛔ 不起 tween**（注释写明「⛔ 别改走 `ToggleCameraResetButton`：
  那条是产品语义、会 `DOTween.Kill` + 每显示弹一次 `DOPunchScale`，会污染 `CameraResetPunchCount` 那个 A423 正在用的判别式」）；未知键/null ⇒ 返回 false、什么都不做。
- `_settingsBtn` 没有 `activeSelf` 守卫：**已在 `HandleSettings` 就地加注释**（标明「生产不可达 ⇒ 隐患非活缺陷；A964 的探针会翻出来；⛔ 别顺手补」），并在新键表的 doc 里再点一句。**行为未改。**
- **配的断言**（`Editor/BattleScene.cs`，A423 段收尾前，共 **5 条**）：
  1. `HudButtonWorldPosForTest("cameraReset") == CameraResetButtonWorldPos`（**原来这里零断言**）；此刻那颗钮**是关着的** ⇒ 顺带证明「关着也取得到坐标」。
  2. 灭自证：`HudButtonWorldPosForTest("__no_such_button__") == Vector3.zero` —— 挡「无视 `which`、恒返回 `_cameraResetBtn`」那一手。
  3. `SetHudButtonActiveForTest("cameraReset", true)` ⇒ 亮（**两态**：之前是关着的），`HudButtonActiveForTest` 与 `CameraResetButtonVisible` 同时翻。
  4. 灭自证：摆状态那一拍 `CameraResetPunchCount` **不动** —— 挡「改走 `ToggleCameraResetButton`」。
  5. 灭自证：未知键 `SetHudButtonActiveForTest("__no_such_button__", true)` 返回 false、不做事。

---

## ③ 死码：`TutorialOverlay` 那四个转发口

**grep 证据**（2026-10-18，`--include=*.cs` 全 `CardPresentation`）：
`SkipPanel` 只剩 `TutorialOverlay.cs:130`（字段定义）与 `BattleDriver.cs` 那两处注入；`SkipVisible` / `SkipWorldPos` / `ClickSkipAt`
只剩 `TutorialOverlay.cs` 自己的 doc 与 `BattleScene.cs:14719`/`:14917` 的**注释** ⇒ **零代码调用**（`BattleScene.cs` 已改用 `driver.Settings.SkipTutorial*`）。

**删了什么（只能删我这边的两处）**：
- `BattleDriver.cs` `EnsureTutorialOverlay` 里 `_tutOverlay.SkipPanel = _settingsPanel;` + 它那 4 行注释 ⇒ 换成一段留痕注释（写明「A991 加的、今天删、为什么」）。
- `BattleDriver.cs` `BuildHud` 里那次幂等注入 `if (_tutOverlay != null) _tutOverlay.SkipPanel = _settingsPanel;` + 注释 ⇒ 同上。

**还剩什么（⛔ 不在白名单）**：`Battle/TutorialOverlay.cs` 里那 **4 个口**——
字段 `SkipPanel:130` · 属性 `SkipVisible:214` · 属性 `SkipWorldPos:222` · 方法 `ClickSkipAt:526`（以及 `:114-129`/`:210`/`:221`/`:345` 的 doc）
⇒ **需要另派一件**才能删干净（现在它们全null/失效也不会有人察觉，因为零调用）。
- ⛔ `minTimeBeforeSkip` 那道闸**未动**（有断言钉着，且已如实标注为真偏离）。
- `Shell/BoosterInfoPopup.cs` **未改**（通读后确认它本来就是外扩口径：`:504` 用 `TooltipR ± 15f`，与 `(−15×4)` 一致）。

---

## 类型检查 · 行尾 · 没查清 / 停手

- **类型检查**：`TMPDIR=/tmp/wf_rp bash d:/4/Unity/工具/typecheck.sh` 跑**两次**（`BattleDriver` 改完一次、`BattleScene` 改完一次）
  ⇒ 两次都是 **运行时错误数 0 · 编辑器错误数 0**（⚠️ 期间没有别人正在写的半成品报错混进来）。
- **行尾**：二进制现数 —— `BattleDriver.cs` CRLF=13289 / LF=13289（**bare LF = 0**）· `BattleScene.cs` CRLF=18081 / LF=18081（**bare LF = 0**）
  ⇒ **两件都是纯 CRLF、一个字节没翻**（全程用 Edit 工具，⛔ 未用 `sed -i`）。
- ⛔ **没跑 Unity**：所以 A423 那 5 条新断言 + 2 条重写断言**只是静态核对过，未经实跑验证**。
  ⚠️ **我这批的静态依据**：`ImageQuad.Create` 从不设 `localScale`（`HudAbs` → `HudImageTex` → `ImageQuad.Create` 一路只写 `localPosition`；
  全文件 grep `localScale` 只有 `ToggleCameraResetButton` 那句「归位 one」）⇒ `InverseTransformPoint` 的局部偏移 = 世界偏移，
  `px/108` 那条换算成立（旧 25 px 判别式也是靠这一条绿的）。**若实跑红，先怀疑这条而不是值本身。**
- ⚠️ **判不准（未查、如实标注）**：`_offensiveBtn` / `_chatBtn` / `_cemeteryBtn` / `_endTurn*` / `HitTip` / `HitSlot` 这六处**走的是
  `ImageQuad.Contains`（画出来的框），压根没读原版的 `m_RaycastPadding`** ⇒ 「它们该不该也外扩」**我没查**（那要逐个读对应节点的 MB JSON，**不在本次判据内**）。
  这批的符号缺陷对它们**不适用**；但它们是否漏了 padding 是**另一件**（建议另开一条待办，别当成已修）。
- **停手点**：白名单外一律没碰；建议 → 另派一件删 `TutorialOverlay.cs` 那四个口。
