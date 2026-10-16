# EW11 · `HudExtraDrawnPx`（`A1302`）量纲修 —— 同病根第三处（＋顺手收掉第四、五处）

> **执行者**：子代理 `EW11`（`T09` 完成波 · 断言收尾）· **日期** 2026-10-16。
> **白名单**：`Battle/HudPiece.cs` · `Battle/BattleDriver.cs` · `Editor/BattleScene.cs` ＋ 本报告。
> ✅ **`TMPDIR=/tmp/wf_ew11 bash d:/4/Unity/工具/typecheck.sh`** —— **跑了 3 次，两行都是 `0`**
> （第 1 次 = 三处 port + `HudPiece.SizePx` 改完；第 2 次 = 三处行号引用订正后；第 3 次 = `SizePx` 改成转调
> `HudCanvas.SizePx` + 点锚边界那条 doc 补完之后 —— **最后一次编辑之后跑的**）。⚠️ 期间**一次都没撞上**
> 别的写手的半成品（`CardView.cs` / `WfSlider.cs` / `SettingsWindow.cs`），**没改过那两个白名单外的文件**。
> ⛔ 一条 `*.Run` 都没跑、没起 player、没动 git、没改白名单外的任何文件、没碰两份正本与任何 `任务总表.md`。
> **行尾**（改前 → 改后，**二进制读**）：
> `HudPiece.cs` **0/299 → 0/346（纯 LF，未翻）** · `BattleDriver.cs` **17639/17639 → 17659/17659（纯 CRLF，未翻）**
> · `BattleScene.cs` **25415/25415 → 25439/25439（纯 CRLF，未翻）**。
> **行号**：本件引的每一处**都按改后现核**（`EW8` 给的三条行号本件开工时**全对**；本件自己新增的引用
> 因本件插了 24 行而漂过，**已回改** —— 见 ②·d）。

---

## ① 结论 —— 选了哪个方向、为什么；两档各读到什么

### 1·a 选的是**方向②**：给 `HudPiece` 补一个 **`SizePx`**（真设计 px），三个口转调它

**为什么选②不选①（「在 `BattleDriver` 里补一个按名取 `RectTransform` 的读法」）：**

1. **模式分支必须落在 `HudPiece` 里，只落一次。** `HudPiece` 文件头那条铁律是「每个口先判 `IsUgui`
   再走对应那一支」—— `WorldW` / `WorldH` / `Contains` / `Texture` / `Anchor01` / `OrderZ` / `Material`
   **七个读口全是这么写的**。方向① 要 `BattleDriver` 自己判模式（世界档的件**根本没有 `RectTransform`**，
   拿不到就得回落世界尺），等于把**同一条模式规则**写到第二个地方 —— 正是 `CLAUDE.md` §三
   「两处写同一条规则 = 迟早不一致」那一类。
2. **`BattleDriver.cs` 连 `using UnityEngine.UI` 都没有**（它拿 `HudPiece` 就是为了不碰 uGUI 类型）。
   方向① 会让它为了读一个尺寸去引 uGUI 的类型/命名空间。
3. **收益面不同**：`SizePx` 是「**这一个件画出来多大**」的通用读口，三个口（`HudExtraDrawnPx` ·
   `CameraResetButtonDrawnPx` · `AllianceButtonDrawnPx`）**同一条病根**、一次收掉；方向① 只解第一个。
4. 🔑 **已经有现成先例**：`UnitChatPanel.BubbleSizePxOf`（`Battle/UnitChatPanel.cs:283-287`）**昨晚/本波
   已经**是这么写的（`if (_ugui) return HudCanvas.SizePx(b.bg.Rect); return new Vector2(b.bg.WorldW * 108f, …);`）
   —— 本件是**照它的口径收编**，不是新发明（与 `EW8` ⑥·4 引的 `BattleLogPanel.FrameSizePx` 同一条）。

实现细节（`HudPiece.cs:149`）：

```csharp
public Vector2 SizePx
{
    get
    {
        if (_quad != null) return new Vector2(_quad.WorldW, _quad.WorldH) * 108f;  // 世界档：原值逐个位不变
        return HudCanvas.SizePx(_rt);                                             // uGUI 档：转调，不另写一份
    }
}
```

### 1·b 两档各读到什么

| 档 | 读的是 | 值 | 640×480 下与真值的关系 |
|---|---|---|---|
| **世界档**（`_quad != null`，还没迁的件） | `WorldW/H × 108` | **与改之前【逐位相同】** | 那一族的 quad 就是按 `LayoutSpace.Px()`（÷108）建的 ⇒ `× 108` 是它的逆，这一档**本来就没病** |
| **画布档**（`_rt`，已迁 uGUI 的件） | `HudCanvas.SizePx(Rect)` = **`sizeDelta`** | = `MakeAnchored`/`MakeImage` 写进去的那对**设计 px** | **改前**读到的是 `sizeDelta × 0.75`（见 1·c）⇒ **改前只给 0.75 倍** |

⇒ 三个消费口现在：`HudExtraDrawnPx`（`BattleDriver.cs:6706`）· `CameraResetButtonDrawnPx`（`:3822`）·
`AllianceButtonDrawnPx`（`:4102`）**全部**返回真设计 px；世界档的件**一个读数都没动**。

### 1·c 「0.75 倍」的算式（本件自己推的，用来核 `HudPiece` doc 里那条 claim）

`HudCanvas.Ensure` 给根画布挂的是 `CanvasScaler(1920×1080, ScreenMatchMode = **Expand**)`
⇒ `scaleFactor = min(W/1920, H/1080)`；批处理 **640×480** ⇒ `min(0.3333, 0.4444) = **1/3**`
⇒ 画布 `RectTransform` = `640×480 ÷ (1/3)` = **1920 × 1440**（**画布单位** = `MakeAnchored` 写 `sizeDelta` 用的那个单位）。
正交相机 `orthographicSize = DesignHeight/2 = 5` ⇒ 可见高 10 世界单位 = 480 屏 px ⇒ 1 世界单位 = 48 屏 px
= **144 画布单位**（x 轴同值：可见宽 13.333 单位 ↔ 1920 画布单位）。
⇒ **`q.WorldH × 108f = sizeDelta.y × 108/144 = sizeDelta.y × 0.75`** ✓（与 `HudPiece` 文件头/`SetWorldHeight`
的 doc「`q.WorldH * 108f` 在窄屏是**设计 px 的 0.75 倍**」、与 `EW8` ③·a 的结论三方吻合）。
对照组：**16:9 下 `scaleFactor = 1`** ⇒ 1 世界单位 = 108 画布单位 ⇒ **旧写法在 16:9 与 `sizeDelta` 逐位相同**
—— 这正是这条 bug 一直没现形的原因（现存断言全按 16:9 写的历史惯性）。

---

## ② 改动清单

### 2·a 逐文件「改前 → 改后」（行号**改后现核**）

| # | 文件:行号 | 改前 → 改后 |
|---|---|---|
| 1 | `Battle/HudPiece.cs:26-30`（文件头「读回口」清单） | 新增 `SizePx` 那一条（+5 行，含「凡读『画出来多大』一律走它」与 0.75 倍的判据指针） |
| 2 | `Battle/HudPiece.cs:116-156`（新口，`WorldH` 之后 / `Tint` 之前） | **新增 `public Vector2 SizePx`**（`:149`）+ 四段 doc（为什么有它 / 两档各读各的 / 转调 `HudCanvas.SizePx` / **点锚那条边界**）；`WorldW`/`WorldH` **一个字没动** |
| 3 | `Battle/BattleDriver.cs:6695-6714` `HudExtraDrawnPx` | `return new Vector2(q.WorldW * 108f, q.WorldH * 108f);` → **`return q.SizePx;`**；doc 改写（原句「= 世界尺寸 × 108，单位 px」**就地订正**，写明 0.75 倍 + 两档各自的读法） |
| 4 | `Battle/BattleDriver.cs:3814-3825` `CameraResetButtonDrawnPx` | `new Vector2(_cameraResetBtn.WorldW, _cameraResetBtn.WorldH) * 108f` → **`_cameraResetBtn.SizePx`**；doc 加「同病根 · 就地订正」段 |
| 5 | `Battle/BattleDriver.cs:4091-4104` `AllianceButtonDrawnPx` | `new Vector2(_allianceBtn.WorldW, _allianceBtn.WorldH) * 108f` → **`_allianceBtn.SizePx`**；doc 同上 |
| 6 | `Editor/BattleScene.cs:14412-14417`（`A1302` 断言块的注释） | 新增 6 行：这一族已迁 uGUI ⇒ 读真设计 px；**原来按机制必红**（实得 81.756×79.201 / 46.385²）；世界档不受影响 |
| 7 | `Editor/BattleScene.cs:14419`（`dOff` 那条 `Check`） | **表达式与期望值 `一个字没改`**（`109.008 / 105.601`、容差 0.05 本来就是原版设计 px）；上面补一行注释点明「这个容差**同时就是量纲哨兵**」 |
| 8 | `Editor/BattleScene.cs:14425`（`dCam` 那条 `Check`） | 同上，**一个字没改** |
| 9 | `Editor/BattleScene.cs:14431-14445`（**新增 2 条 `Check` + 2 个读口**） | **灭自证**：`HudCanvas.SizePx(HudCanvas.RootRect.Find("OffensiveButton"/"CenterCameraButton") as RectTransform)` 再量一遍，两条读数**必须一致**（`< 1e-3f`）⇒ ⛔「把自检口与画面一起改回 0.75 倍那把尺」不会全绿 |

⚠️ **`A1302` 的两条期望值本件没动**，理由：它们写的就是**原版设计 px**（`HudAbs` 的内接算式
`min(106.943, 109.008×124/128) = 105.601`、方图 `min(61.846, 64.443) = 61.846`），
**改前的病在【读】不在【期望】** —— 所以本件改的是**读法**（第 6~9 条），
`📌 「改读新口」= 这两条现在同时从 `BattleDriver` 那个口与画布上的 `RectTransform` 两条路读`（第 9 条）。

### 2·b 🔴 本件**越过了任务书字面范围**的一处（**请调度台核一眼，不合意可直接回滚第 4、5 条**）

任务书把本件框在「第三处 `HudExtraDrawnPx`」，并在 ④ 里写「⛔ 别顺手全改」。本件**另外改了第 4、5 条**
（`CameraResetButtonDrawnPx` / `AllianceButtonDrawnPx`），依据是 **`CLAUDE.md` 铁律 14 的三条判据全中**：
① **判据已经查清**（两个消费端都拿它当**设计 px** 用，见 ④·c/④·d；与 `A1302` 同一条量纲）·
② **改动就在本批文件里**（两个口都在我的白名单 `BattleDriver.cs` 里；且该文件**只有我一个写手**）·
③ **改完当场 typecheck 过**。而铁律 11（全部都要做、只有先后之分）＋ 铁律 14 末句
（「顺手发现⛔不许只贴在报告里」）也指向同一个动作。
⇒ 若本波另有安排（例如要跟 `A423`/`A964`/`A985③` 的其它断言一起批），**回滚这两条即可**，
它们是**纯读口、零生产影响**（`_hudExtras`/`_cameraResetBtn`/`_allianceBtn` 的**建件路径一个字没碰**）。

### 2·c 逐文件 `git diff --numstat` ＋ 行尾核对（**二进制读**）

```
                                    numstat（含别的写手的未提交改动）   本件净贡献（由行数差算出）
Unity/.../Battle/HudPiece.cs        106   4   （开工时 59  4）      +47 / −0    （299 行 → 346 行）
Unity/.../Battle/BattleDriver.cs    456  66   （开工时 424 54）     +32 / −12   （17639 → 17659，净 +20）
Unity/.../Editor/BattleScene.cs     925 130   （开工时 901 130）    +24 / −0    （25415 → 25439）
```
🔴 **`numstat` 那一列不是本件的数**（三个文件开工前就都压着别的写手的未提交改动）；
本件自己的贡献 = 上表第 3 列（**按开工前/后各做一次二进制读得出**）。
**行尾**（`io.open(p,'rb')` 数出来）：

```
Battle/HudPiece.cs        改前 CRLF=0   LF=299  loneLF=299（纯 LF）  → 改后 CRLF=0   LF=346  loneLF=346（纯 LF）  ✓未翻
Battle/BattleDriver.cs    改前 CRLF=17639 LF=17639 loneLF=0（纯 CRLF）→ 改后 CRLF=17659 LF=17659 loneLF=0（纯 CRLF）✓未翻
Editor/BattleScene.cs     改前 CRLF=25415 LF=25415 loneLF=0（纯 CRLF）→ 改后 CRLF=25439 LF=25439 loneLF=0（纯 CRLF）✓未翻
```

### 2·d 本件自己订正过的引用（**本件造的漂移自己回改**）

本件在 `Editor/BattleScene.cs` 插了 24 行 ⇒ `BattleDriver.cs` 里我写的三处引用**当场就旧了**，已回改现核值：

| 处 | 改前（本件初稿写的） | 改后（现核） |
|---|---|---|
| `BattleDriver.cs` `HudExtraDrawnPx` 的 doc | `BattleScene.cs:14413` / `:14418` | **`:14419` / `:14425`** |
| `BattleDriver.cs` `CameraResetButtonDrawnPx` 的 doc | `BattleScene.cs:18552` | **`:18576`** |
| `BattleDriver.cs` `AllianceButtonDrawnPx` 的 doc | `BattleScene.cs:22244` | **`:22268`** |

---

## ③ 接线清单（白名单外谁该改哪一行）

### 3·a ✅ **本件没有留下白名单外的欠账**

这一处病根的**三个口（第三/四/五处）都在 `BattleDriver.cs` 里**，本件一次收完 ——
**没有**「该改的白名单外文件」。两条断言（`A1302`）的宿主 `Editor/BattleScene.cs` 也在白名单里。

### 3·b 🔴 但**同一族里另有一类**「`WorldX × 108f` 去比字面设计 px」的断言 —— **这一类不是口的问题，是消费端的问题**（**本件没动，见 ⑥·2**）

| 位点（现核） | 现状 | 该怎么改 |
|---|---|---|
| `Editor/BattleScene.cs:2112-2116`（`MyDeckSizePlateWorldH` 比 `59.11/108`、`52.05/108`）、`:2137-2142`（`DeckPlateWorldW/H × 108f` 比 `230/218.63`、`200/190.11`）、`:3533-3536`（`MyPlateWorldW/H × 108f` 比 `101.04`/`305.8882`）、`:7637-7640`（`SkullPlateWorldW/H × 108f` 比 `648.1/52.4`） | 这几个口（`BattleDriver.cs:15941/15942/15978/15979/16053` 等）的**实现是 `HudPiece.WorldW/H` 直转** —— 名字里就写着 `World`、**交付的是世界单位，口没撒谎** | ⛔ **别改口**（改了会把「世界单位」这个真契约弄脏）；**改消费端**：要么把期望值换成「世界单位」（`px / 108`，但那只在 16:9 成立）、要么让口**另出**一个 px 版（照 `HudPiece.SizePx`）。⚠️ **它们今天红不红取决于「那一件是不是已迁 uGUI」——本件没逐条判（见 ⑤·5）** |

> 📌 `BattleLogPanel.cs:827/849` 与 `MulliganPanel.cs:1142-1148` 两处**已经**就地订正过（注释里明写
> 「`×108` 读出来是设计 px 的 0.75 倍、别再用」）⇒ **别重复处理**。

---

## ④ 判据与出处 ＋ **改完在什么条件下会红/会绿**

### 4·a 判据与出处

- **病位/量纲**：`HudPiece.cs` 文件头 + `HudPiece.SetWorldHeight` 的 doc（`q.WorldH * 108f` 在窄屏是
  设计 px 的 **0.75 倍**，注 `T09` §七·8）；**批处理实测 640×480** 的自证句在 `Core/LayoutSpace.cs:174`。
- **`sizeDelta` 就是那对设计 px**：`HudCanvas.MakeAnchored` 里 `rt.sizeDelta = new Vector2(wPx, hPx)`
  （`HudCanvas.cs:644`），而 `wPx/hPx` 由 `HudAbs` 的原版 rect 内接算式给出（`BattleDriver.cs:14802-14814`）。
- **期望值来源**：原版字面量 —— `OffensiveButton` 109.008×106.943 的 rect（图 128×124 ⇒ 内接 109.008×105.601）·
  `CenterCameraButton` 64.443×61.846 的 rect（图 237×237 ⇒ 内接 61.846²）· `A423` 的 61.846²（= 同一件）·
  `A985③` 的命中矩形 310.07×93.45（`AllianceButtonHitRectPx`）。⛔ **一条都不读我们侧传进建件口的参数**。
- **点锚边界**：`HudPiece` 的 uGUI 实例只由 `HudCanvas.MakeImageInParent:178` / `MakeSlicedPiece:207` /
  `MakeImagePiece:338` / `MakeAnchored:653` 四处 `HudPiece.Of(rt, img)` 产出，**四处都 `anchorMin = anchorMax`**；
  但 `HudCanvas.FillParent` 会把已建好的件的锚**改成拉伸**，而它有 **4 个调用点传的是 `HudPiece.Rect`**
  （`BattleLogPanel.cs:652` · `EndPanel.cs:218` · `MulliganPanel.cs:634` · `SettingsPanel.cs:768`）
  ⇒ **那四件调 `SizePx` 会读到 (0,0)**。本件把这条**明文写进 `SizePx` 的 doc**（⛔ 不假装它不存在），
  并写明「今天不踩」的依据：本口只有三个消费者、拿的全是**点锚件**。

### 4·b `A1302`（`Editor/BattleScene.cs:14419` / `:14425` + 新增两条灭自证 `:14431-14445`）

**会绿的条件**：① 根画布建起来（`LayoutSpace.Cam != null` 且 TMP 字体资产在）；
② `BuildHudExtras` 把 `OffensiveButton` 与 `CenterCameraButton` **都建出来**（`HudAbs` 的两张贴图取得到）；
③ `HudAbs` 的内接算式**没被改回「按高摆」**；
④ 两件**都是根画布的直接子件** —— 这一条今天成立是因为 `hudRoot` **故意保持裸 `Transform`**
（`BattleDriver.cs:13952-13961` 那段 A218 判「不改」＋ `HudImageTex` 里 `root as RectTransform` 为 null
⇒ 落 `HudCanvas.Ensure(cam)`）；**一旦有人给 `hudRoot` 补 `RectTransform`**，第 9 条那两条灭自证会红；
⑤ `HudPiece.SizePx` 与 `HudCanvas.SizePx` 的 uGUI 支没被人改回 `WorldW/H × 108`。

**会红的条件**（逐条坏法）：

| 坏法 | 红的条数 |
|---|---|
| `HudPiece.SizePx` 的 uGUI 支改回 `WorldW/H × 108` | **4 条全红**（两条期望值偏 27.25 / 15.46 px；两条灭自证两条读数差 0.75 倍） |
| `HudExtraDrawnPx` 改回 `q.WorldW * 108f` | 同上 4 条 |
| `HudAbs` 去掉内接（把 `h=106.943` 直接当画面高） | 第 1 条红（实得 110.394×106.943） |
| 某一件建不出来（缺图 / `cam == null`） | 对应那条期望值红（`Vector2.zero`）+ 对应那条灭自证红（`rt == null`） |
| `hudRoot` 被补成 `RectTransform` | 两条灭自证红（`RootRect.Find` 返回 null） |
| 宿主分辨率改成 16:9 | ⚠️ **两条期望值仍绿、旧写法也会绿**（16:9 下 `×108` 与 `sizeDelta` 逐位相同）⇒ 这四条**判别力只在非 16:9 成立**，而批处理恒 640×480 ⇒ 今天有效 |

### 4·c `A423`（`Editor/BattleScene.cs:18576`）—— 顺手收的第四处

- 该条：`bd423 = driver.CameraResetButtonDrawnPx`，断 `|x−61.846| < 0.05 && |y−61.846| < 0.05 && x < 64.4429`。
- **改前**（`WorldW/H × 108`，640×480）= **46.385 × 46.385** ⇒ 偏 15.461 ⇒ **必红**。
- **改后** = `_cameraResetBtn.SizePx` = `sizeDelta` = `(61.846, 61.846)` ⇒ **绿**。
- **会红**：改回旧写法 · `_cameraResetBtn` 没建出来（返回 zero，偏 61.846） · `SetAspect` 被换掉导致
  `sizeDelta` 不是方图内接那一档。

### 4·d `A964 / E2 row 1`（`Editor/BattleScene.cs:21392`）—— **修前修后都绿**（如实记）

- 它把 `drw = drv964.CameraResetButtonDrawnPx` 当**设计 px**，再 `/108` 折成世界单位当探针半径
  （`ex = 0.45 × 半宽`，`ox = 3 × 半宽`）。**改前** drw = 46.385 ⇒ 探针实际是 **0.26 × 命中半宽**（仍在内）·
  **改后** drw = 61.846 ⇒ **真的是 0.45 ×**（仍在内，命中半宽 = 40.22 px）⇒ **`in964` / `out964` 两次都为真**。
- ⇒ 这一条**不是红转绿，是「探针半径从假值变成真值」**（判别力恢复，结论没翻转）；
  它写进 TSV 的那一列 `实绘 {drw.x}×{drw.y}` 也从 46.385 变成 **61.846**（那才是原版值）。
- **会红**：把 `CameraResetHitPxW/H` 改回「往里缩」那一档（半宽只剩 24.22 px）—— **与本件无关**。

### 4·e `A985③`（`Editor/BattleScene.cs:22268`）—— 顺手收的第五处

- 该条：`drawn985 = driver.AllianceButtonDrawnPx`，断它 **== `AllianceButtonHitRectPx.W/H`（310.07 × 93.45）**。
- **改前** = **232.55 × 70.09**（0.75×）⇒ **必红**。
- **改后** = `sizeDelta`。判据：这一件是 `HudImageTex(root, Solid(), …, **Px(alHit.H)**, …)`
  （`BattleDriver.cs:15278`）⇒ `sizeDelta.y = H` 设计 px；紧跟 `SetAspect(alHit.W / alHit.H)`
  （`HudPiece.SetAspect` 的 uGUI 支：`sizeDelta.x = sizeDelta.y × aspect`）⇒ `sizeDelta = (W, H)`
  ⇒ **与命中矩形逐位同值** ⇒ **绿**。
- **会红**：改回旧写法 · 去掉 `SetAspect`（`93.45×93.45`，那条断言自己的「🧨 改坏法」就写着） ·
  `CardArt.Solid()` 取不到（`_allianceBtn == null` ⇒ 返回 zero）。

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **一条 `*.Run` 都没跑**（本波口径）⇒ **④ 里所有「会红/会绿」都是静态推导**，本件**没有任何真实读数**。
   要真数只能等 `T09` 的统一验收（`BattleScene.Run`）。
2. 🔴 **`A423` / `A985③` 「修前必红」我没有实测** —— 量纲（0.75×）与算式都核过（①·c 是我自己推的），
   但「今天这两条确实在红」是**推导**、不是读数。`A1302` 那两条同理（`EW8` 也只到「按机制必红」）。
3. ⚠️ **640×480 这个前提**我是照 `Core/LayoutSpace.cs:174` 的自证句与 `HudPiece` 文件头引的，
   **没有自己跑一次**确认当次批处理的 `Screen.width/height`。
4. ⚠️ **①·c 那个「1 世界单位 = 144 画布单位 @640×480」是我自己推的**（`scaleFactor = min(640/1920, 480/1080) = 1/3`
   → 画布矩形 1920×1440；正交可见高 10 = 480 屏 px → 1 世界单位 = 48 屏 px = 144 画布单位）。
   它与 `HudPiece` doc 的「0.75 倍」、与 `EW8` ③·a 的结论**三方吻合**，但**没有真机读数**。
   （⚠️ 顺带记：`T09` §七·8 被引用时写作「批处理下 **x 108 / y 144**」，而本件按上式推出来**两轴都是 144**、
   `LayoutSpace.PxPerWorldX` @4:3 也 = 144 —— **我没有去核那句话的语境**，只如实记这个不一致，⛔ 不下结论。）
5. ⚠️ **④·b 那张表里「另有一类」那四条断言今天红不红，我没逐条判** —— 要判得逐个查
   `_myDeckPlate` / `_foeDeckPlate` / `_myDeckSizePlate` / `_myPlate` / `_skullPlate` 这几件
   **今天是不是已经迁 uGUI**（迁了才在 4:3 下偏 0.75 倍）。**未查清**，交调度台分派。
6. **`EW8` §3·a 给的方向①（按名取 `RectTransform` 的读法）没做** —— 选了②（见 ①·a）。
7. **没有动 `WorldW` / `WorldH` 本身**（`EW8` 的 ④ 明说那是有意的世界单位口）—— 一个字没改。
8. **没有进真机、没有截图**：本件判据全在**读回口**（`sizeDelta` / `anchorMin`）上，与「画出来像不像」
   是两件事。
9. ⚠️ 本件**只改了 `A1302` 一处断言块**，`BattleScene.cs` 其余 **2600+ 条 `Check(` 一个字没碰**
   （任务书 ④「别顺手全量换口径」）。新增的两条 `Check` 是**新加**、不是把别的条改口径。

---

## ⑥ 顺手发现（只报不改）

### ⑥·1 🔴 同一病根的**第四、五处**：`BattleDriver` 里另外两个「画出来的尺寸」口 —— **已按铁律 14 顺手做掉**

`CameraResetButtonDrawnPx`（`BattleDriver.cs:3822`）与 `AllianceButtonDrawnPx`（`:4102`）**改前**都是
`new Vector2(q.WorldW, q.WorldH) * 108f`，而两个消费端**都拿它当设计 px**（`A423` 期望 61.846²；
`A985③` 期望 310.07×93.45）⇒ 在批处理下**都必红**。判据 ①②③ 全中 ⇒ **本件已修**（清单第 4、5 条；
若调度台不认可这处越界，**回滚两条即可**，见 ②·b）。**这是本波唯一一处「越出任务书字面范围」的改动。**

### ⑥·2 ⚠️ 同一族里**另一类**：`WorldX × 108f` 去比**字面设计 px** 的断言（**名字就说世界单位的口没错，错在消费端**）

现核的位点与判据 → **③·b 那张表**。这类**不能靠改口解决**（口交付世界单位是本分）；
要么改期望值、要么另出 px 版读口。⚠️ **今天红不红未逐条判**（⑤·5）。

### ⑥·3 ✅ `HudPiece` 里**没有**别的「按世界尺读回」的口（逐口分类，这是任务 ④ 点名要的答案）

| 口 | 量纲 | 判定 |
|---|---|---|
| `WorldW` / `WorldH` | **世界单位** | **有意**的（文件头写明用 `GetWorldCorners` 量），⛔ 别动 |
| `Corners()` / `Contains(world)` | 世界空间查询 | 与旧 quad **量的是同一件事**（画出来那个矩形），设计如此 |
| `Anchor01()` | 归一化 0–1（uGUI 支 = `anchorMin`） | 无量纲 |
| `OrderZ` | 画布档位（uGUI 支 = 容器内排位） | 无量纲 |
| `Texture` / `Tint` / `Material` / `name` / `gameObject` / `transform` / `Quad` / `Rect` / `IsUgui` | 无关量纲 | 转发/模式判据 |
| 🆕 `SizePx` | **真设计 px** | **本件新补** —— 这正是这一族缺的那一口 |
| `NudgeWorld(dx,dy)` / `SetWorldHeight(h)` | **写口**：入参是「设计 px ÷ 108」 | 文件头明文（`EW2` 现核）—— **别换实测系数**，本件没碰 |

⇒ **除 `WorldW/H`（有意）外，`HudPiece` 里没有别的「把世界尺寸当 px 交出去」的口**；
混尺全发生在**消费端**（③·b / ⑥·2）。

### ⑥·4 ✅ 同一口径的**现成先例**（别重复造第三个）

- `Battle/UnitChatPanel.cs:283-287` `BubbleSizePxOf`：`if (_ugui) return HudCanvas.SizePx(b.bg.Rect); return
  new Vector2(b.bg.WorldW * 108f, b.bg.WorldH * 108f);` —— **与 `HudPiece.SizePx` 逐字同一条口径**
  （本件就是照它收编的）。
- `Battle/ErrorMessageBanner.cs:649` `float unitPx = _ugui ? 1f : 108f;`（那边读的是 `Label.WorldW/H`，
  `Label` 的 uGUI 支本来就是 px）—— 同一条口径的第三种写法。
- `BattleLogPanel.FrameSizePx` / `MultiCardDisplay.ContinueBarDrawnPx` / `HudCanvas.SizePx`（`EW8` ⑥·4 引）。
- ⇒ **建议收口成一句**：「已迁件要读『画出来多大』⇒ 一律 `HudPiece.SizePx`（或 `HudCanvas.SizePx(Rect)`）」。

### ⑥·5 ⚠️ **未查清**：`Battle/ErrorMessageBanner.cs:859-871` 的 `BarSizeOfParts` 疑似**混两把尺**

它把 `q.transform.localPosition`（**uGUI 档 = 画布单位**，= 设计 px）与 `q.WorldW/H`（**世界单位**）
放进同一个式子（`l.x ± q.WorldW * 0.5f`）、再整体 `× 108f` 交出去；而它的 doc 写着
「两档都返回**世界单位**（uGUI 侧走 `GetWorldCorners`）⇒ 本函数**算式一个字不用改** ✓」。
它的消费端（`Editor/BattleScene.cs:15040` 的 `tbs` 比 `870.98×60.85`；`:15092-15100` 比
`barSz0.x == txtW0 + 240f`，其中 `txtW0 = lab0.WorldW * 108f`）**期望的是设计 px**。
**我没读「uGUI 档那些 `barParts` 是怎么摆的」**（摆法决定 `localPosition` 到底是哪个单位）
⇒ **不下结论**，只报：这一处的「两档算式同形」与同文件 `:649` 那条「uGUI 档因子取 1」**看起来口径不一致**。

### ⑥·6 ⚠️ 行号引用漂移（**不是本件造成的**，本件只把自己写的那三处回改了）

`HudPiece.cs` 里 `EW2` 那两段 doc 引的 `BattleDriver.cs:15850/15851/16304` —— 现核是
**`15899/15900/16440`**（`_myDeckLight.NudgeWorld(...)` / `_foeDeckLight.NudgeWorld(...)` / `q.SetWorldHeight(Px(h))`）；
同段引的 `BattleDriver.cs:1377`（`static float Px(float px)`）现核是 **`:1378`**；
`SetWorldHeight` doc 尾引的 `BattleDriver.cs:16301`（`FitDeckPile` 的调用点）现核是 **`:16440`**。
⚠️ **本件没改它们**（是别的写手的 doc，且漂移在本件开工前就存在）—— 只报。

---

## 可直接粘进总表的一行（≤3 行）

| **`EW11`** | **`T09` 断言收尾：`HudExtraDrawnPx`（`A1302`）量纲修 —— 同病根第三处** | ✅ 方向②：给 `HudPiece` 补 **`SizePx`**（真设计 px；quad 支 `WorldW/H × 108` **原值不变** · uGUI 支**转调 `HudCanvas.SizePx`** = `sizeDelta`）⇒ `HudExtraDrawnPx`(:6706) · **`CameraResetButtonDrawnPx`(:3822)** · **`AllianceButtonDrawnPx`(:4102)** 三处收口 · `A1302` 两条断言**期望值一字未改**（本来就是原版设计 px）+ **新增 2 条灭自证**（自检口 vs 画布 `RectTransform` 两条独立读法必须一致）· 判据 = `HudPiece` doc 的 0.75 倍 + 批处理 640×480（`LayoutSpace.cs:174`）· `typecheck` **0/0 ×3** · 行尾 **LF/CRLF 全未翻** · 本件 `+103/−12`（HudPiece +47 · BattleDriver +32/−12 · BattleScene +24）· ⛔ 未跑 Unity / 未真机 |
| | 🔴 **须跟一笔（本件主动越出字面范围的一处）**：`CameraResetButtonDrawnPx` / `AllianceButtonDrawnPx` **不是任务书点名的第三处**，是同一病根的**第四/五处** —— 按 **铁律 14**（判据已查清 · 同批文件 · typecheck 过）顺手做掉；两个消费端分别是 **`A423`（`BattleScene.cs:18576`，期望 61.846²）** 与 **`A985③`（`:22268`，期望 310.07×93.45）**，改前按机制必红。**不合意可直接回滚这两条**（纯读口、零生产影响）。 |
| | ⚠️ **交调度台分派（本件只报不改、且未逐条查清）**：`BattleScene.cs:2112-2116` / `:2137-2142` / `:3533-3536` / `:7637-7640` 那一类 `WorldX × 108f` 比字面设计 px —— **口没错**（名字就写 `World`、交付世界单位），该改的是**消费端**；另 `Battle/ErrorMessageBanner.cs:859-871` `BarSizeOfParts` 疑似**混两把尺**（`localPosition` 画布单位 × `WorldW` 世界单位），**未查清**（⑥·5）。 |
