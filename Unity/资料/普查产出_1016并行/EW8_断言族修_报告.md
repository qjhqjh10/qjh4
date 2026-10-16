# EW8 · `hudRoot.Find` 两族断言修复（`T09` 断言收尾）+ `WaitBanner`（§4.5）一组

> **执行者**：子代理 `EW8`（`T09` 完成波 · 断言收尾）· **日期** 2026-10-16。
> **白名单**（只改了这两个）：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` + 本报告。
> ✅ **`TMPDIR=/tmp/wf_ew8 bash d:/4/Unity/工具/typecheck.sh`** —— **本件关键检查 3 次，两行都是 `0`**
> （分别在：族 A 改完 · 族 B 改完 · `WaitBanner` 组改完 = **全部编辑之后**）；**收尾为确认又复跑了 2 次**
> （一次撞上别的写手半成品 `WfSlider.cs` 的 2 个错、**隔 75 秒复跑又是 0/0** —— 详见 ⑤·9）。
> ⛔ 一条 `*.Run` 都没跑、没起 player、没动 git、没改白名单外的任何文件、没碰两份正本与任何 `任务总表.md`。
> **行尾**：`BattleScene.cs` 改后二进制读 = `CRLF=25415 / LF=25415 / loneLF=0 / 25416 行` ⇒ **纯 CRLF，未翻** ✓。
> **行号**：本件开工时逐处**按标识符现核过**（`EW6` 给的行号**当时全对**；`WaitBanner` 组那三条
> `EW7` 给的行号**已漂**，按内容搜到实位：`4446` / `4513` / `4528`，不是在它写的 `4419` / `4486` / `4501`）。

---

## ① 结论（逐条「做了 / 没做」）

### 1·a 族 A · `hudRoot.Find` 找字 —— ✅ **做了**（16 前提 + 144 静默 + 4 前提 + 4 静默，全清）

| # | 处（改后行号） | 做了什么 | 结论 |
|---|---|---|---|
| A1 | `CheckHudBox` 的查找 `:479-483` | `hd.hudRoot.Find(name)` → **`HudCanvas.RootRect.Find(name)`**；顺手删掉那一句**已无消费者**的 `var hd = Object.FindObjectOfType<BattleDriver>();`（它只为那句查找存在） | ✅ |
| A2 | 框宽/框高 `:494-504` | `LayoutSpace.Px(bw)` / `Px(bh)` → **`bw` / `bh` 原样**（设计 px）；消息里 `sd.x * HudPxPerWorld` → `sd.x` | ✅ |
| A3 | 三个字号 `:521-530` | 静态 `Label.FontSizeToPx(...)` → **实例口 `lb.FontSizeToPxNow(..., lb.ActualFace)`**（下界/上界/起点各一条） | ✅ |
| A4 | `hudNoBox` 那圈查找 `:14693` | `drv.hudRoot.Find(n)` → **`HudCanvas.RootRect.Find(n)`** | ✅ |

⇒ **原来 20 条前提必红 + 148 条被 `return` 静默跳过**，现在这一族**恢复成真跑**。

### 1·b 族 B · `Quad()` 找头像 —— ✅ **做了**，**确切条数 = 12 条**（⚠️ `EW6` 的「约 8 条」是估的、**偏小**）

`Quad()` 助手（旧 `ImageQuad Quad(string nm)`）**整个换成** `RectTransform Rt(string nm)`
（= `HudCanvas.RootRect.Find(nm) as RectTransform`）+ 新增 `QHpx`（设计 px）/ 改写 `QZ` / 改写 `IsDrawn`。

**12 条**逐条（改后行号；全部**原来恒红**）：

| # | 行号 | 断言（简） | 原恒红的原因 |
|---|---|---|---|
| 1-2 | `IsDrawn` 那 2 条 · 消息 `:15303` | ① 两层立绘真的画出来了 | `Quad()` 恒 null ⇒ `"节点或 ImageQuad 不在"` ⇒ `bad == null` 假 |
| 3-4 | ② 1.6 倍 · 消息 `:15321` | 立绘高 / 框高 = **1.6 倍** | `QH` 恒 `0` ⇒ `bh > 1e-3f` 假 |
| 5-6 | ② `Border` 绝对尺寸 · 消息 `:15339` | `AvatarItemSmall_{Me,Foe}` = 原版 **88.7274449 × 99.1251923 px** | `q == null` ⇒ `gw/gh = −1` |
| 7-8 | ② 立绘绝对高 · 消息 `:15352` | `AvatarImage_{Me,Foe}` 高 = **158.6003077 px** | 同上 |
| 9 | ③ `kMe` · 消息 `:15379` | `kMe > 0`（**哨兵**，应 1.0000） | `QH` 恒 0 ⇒ `kMe = 0` |
| 10 | ③ `kFo` · 消息 `:15389` | `kFo > 0` | 同上 |
| 11 | ④ z 序（我）`:15407-15409` | `azMe < bzMe && cz < azMe` | `Quad` 恒 null ⇒ `QZ` 恒 **`NaN`**（NaN 比较恒假） |
| 12 | ④ z 序（敌）`:15412-15414` | `azFo < bzFo` | 同上 |

**据以数条数的 grep**（本件现跑，`Editor/BattleScene.cs`）：
`RectTransform Rt(` · `string IsDrawn(` · `float QZ(` · `float QHpx(` · `string bad = IsDrawn(nm)` ·
`float ah = QHpx(pr[0])` · `var sz = rt != null ? HudCanvas.SizePx(rt) : new Vector2(-1f, -1f)` ·
`float gh = rt != null ? HudCanvas.SizePx(rt).y : -1f;` · `float kMe = QHpx(` · `float kFo = QHpx(` ·
`float azMe = QZ(` · `float azFo = QZ(`
⇒ 6 个消费点（`IsDrawn` / `QHpx`×3 / `QHpx`×2 / `QZ`×2）**落在 8 个 `Check` 语句里，其中 4 个在 `foreach` 里、每个跑 2 次 ⇒ 12**。
`grep -n "Quad("` 现在**只剩 `HudQuad(`**（那一处是**好的**，见 ⑥·2）。

### 1·c `WaitBanner`（§4.5，协调者当日追加）—— ✅ **做了**

| 类 | 处（改后行号） | 做了什么 |
|---|---|---|
| 必红 ① | `:4446-4461` | `TextWorldW/H ≤ 框`：期望**分档**——画布档用**字面量 1270 / 70**（画布 px），世界档维持 `LayoutSpace.Px(...)` |
| 必红 ② | `:4538-4551` | `BarFramePieces >= 6`：画布档改断 **`FrameIsSliced`**；世界档照旧 |
| 必红 ③ | `:4566-4583` | `BarFillPieces == 42`：画布档改断 **`FillIsTiled` + `FillSizePx = 1323×90` + `FillPpuMulValue = 2`**；世界档照旧 |
| 🔴 恒真（`EW7` 标「可选」） | `:4516-4534` | `BarWorldW/H` 那两条**读的是本件自己的常量**（`U(PopupW)`）⇒ 世界档下**恒真 / 同义反复**、画布档下量纲还不对 ⇒ 画布档改读**真读回** `FrameSizePx`（期望写**字面量** 1323 / 90），世界档保留原两条 |
| 🆕 新增组 | `:4589-4643` | **6 条**画布档判别式：`OnCanvas`（前提）+ `BarCenterPx ≈ (967,175.1)` + `RootSizePx = (1344,79.44)` + `ShadeSizePx = (3963.5,3366)` **且 `!ShadeHasSprite`** + `RootOrderZ < DefaultZ && < 0` + **不变量三段** `ShadeOrderZ > PopupOrderZ > TextOrderZ` |

⛔ **`EW7` §3·3 / §3·4（画布档「美术取不到」探针 · `§A218` 并排画布探针）没做** —— 协调者只点名了 §③·1 / §③·2（见 ⑤·4）。
ℹ️ `EW7` 给的可粘贴写法里：`FrameSizePx` 那半条**我已放在**上面的「恒真」那一组里断过一次 ⇒ ②里**不再重复断同一个量**
（②只断 `Sliced`），这是与它建议的唯一一处有意偏离。

---

## ② 改动清单（`文件:行号` · 改前 → 改后 · 行尾核对）

### 2·a `Editor/BattleScene.cs` —— 本件共 **19 个 hunk**、**+210 / −45（净 +165）**

> 该文件在本件**开工前就已经是 `M`**（工作区里压着别的写手的未提交改动）⇒ 下面只列**本件的** hunk
> （判据 = 逐个 hunk 的**新侧起始行**落在本件那 5 个区段里；脚本切出来的，不是眼估）。

| 区段 | hunk（新侧起始行 · `+/-`） | 一句话 |
|---|---|---|
| **族 A · 助手** | `463 +1-1` · `466 +13-1` · `482 +2-4` · `494 +6-0` · `501 +4-4` · `510 +15-4` · `529 +2-2` | doc 扩写（机制/查找口/两把尺）· `hd`+`hudRoot.Find` → `HudCanvas.RootRect.Find` · `Px()` → 原样 px · 静态口 → 实例口 |
| **族 A · `hudNoBox`** | `14690 +4-1` | `drv.hudRoot.Find` → `HudCanvas.RootRect.Find` |
| **族 B** | `15239 +17-3` · `15257 +1-2` · `15262 +8-5` · `15272 +6-2` · `15293 +9-5` | `Quad()`→`Rt()`/`QHpx`/`QZ`/`IsDrawn` 重写 · 三处消费点换成 `sizeDelta` 读法 · `kMe/kFo` 分母去掉 `/108f` |
| **`WaitBanner` 组** | `4446 +10-1` · `4459 +2-1` · `4516 +19-4` · `4538 +15-2` · `4566 +20-3` · `4588 +56-0` | 分档期望 · 恒真改真读回 · 三个必红分档 · 新增 6 条那一段 |

**改前 → 改后**（逐条原文，够粘）：

```csharp
// 族 A-1（:483）
-            var hd = Object.FindObjectOfType<BattleDriver>();
-            var t = hd != null && hd.hudRoot != null ? hd.hudRoot.Find(name) : null;
+            var t = HudCanvas.RootRect != null ? HudCanvas.RootRect.Find(name) : null;

// 族 A-2（:501 / :503）
-            Check(Mathf.Abs(sd.x - LayoutSpace.Px(bw)) <= 1e-4f,
-                  $"{name} 框宽 {bw}px = …（实得 {sd.x * HudPxPerWorld:F2}px）");
-            Check(Mathf.Abs(sd.y - LayoutSpace.Px(bh)) <= 1e-4f,
-                  $"{name} 框高 {bh}px = …（实得 {sd.y * HudPxPerWorld:F2}px）");
+            Check(Mathf.Abs(sd.x - bw) <= 1e-4f,
+                  $"{name} 框宽 {bw}px = …（实得 {sd.x:F2}px）");
+            Check(Mathf.Abs(sd.y - bh) <= 1e-4f,
+                  $"{name} 框高 {bh}px = …（实得 {sd.y:F2}px）");

// 族 A-3（:521 / :523 / :529）
-            Check(Mathf.Abs(Label.FontSizeToPx(lb.FontSizeMin) - mn) <= 0.05f, …);
+            Check(Mathf.Abs(lb.FontSizeToPxNow(lb.FontSizeMin, lb.ActualFace) - mn) <= 0.05f, …);
   （`FontSizeMax` / `FontSizeBase` 两条同形）

// 族 A-4（:14693）
-                            var t0 = drv.hudRoot != null ? drv.hudRoot.Find(n) : null;
+                            var t0 = HudCanvas.RootRect != null ? HudCanvas.RootRect.Find(n) : null;

// 族 B（:15255 起）
-                ImageQuad Quad(string nm)
-                {
-                    var t = drv.hudRoot != null ? drv.hudRoot.Find(nm) : null;
-                    return t != null ? t.GetComponent<ImageQuad>() : null;
-                }
+                RectTransform Rt(string nm)
+                {
+                    return HudCanvas.RootRect != null ? HudCanvas.RootRect.Find(nm) as RectTransform : null;
+                }
-                float QZ(string nm) { var q = Quad(nm); return q != null ? q.transform.localPosition.z : float.NaN; }
-                float QH(string nm) { var q = Quad(nm); return q != null ? q.WorldH : 0f; }
+                float QZ(string nm) { var rt = Rt(nm); return rt != null ? HudCanvas.OrderZOf(rt) : float.NaN; }
+                float QHpx(string nm) { var rt = Rt(nm); return rt != null ? HudCanvas.SizePx(rt).y : 0f; }

// 族 B 尺寸三处（:15336 / :15350 / :15373-15384）
-                    float gw = q != null ? q.WorldW * 108f : -1f, gh = q != null ? q.WorldH * 108f : -1f;
+                    var sz = rt != null ? HudCanvas.SizePx(rt) : new Vector2(-1f, -1f);
+                    float gw = sz.x, gh = sz.y;
-                    float gh = q != null ? q.WorldH * 108f : -1f;
+                    float gh = rt != null ? HudCanvas.SizePx(rt).y : -1f;
-                    float kMe = QH("AvatarItemSmall_Me") / (SpecBorderHpx / 108f);
+                    float kMe = QHpx("AvatarItemSmall_Me") / SpecBorderHpx;      // 两边同尺 ⇒ 那个 /108f 去掉

// WaitBanner 组（:4446 起）
-                float bw = LayoutSpace.Px(WaitBanner.WaitTextBoxW), bh = LayoutSpace.Px(WaitBanner.WaitTextBoxH);
+                bool waitUgui = wait.OnCanvas;
+                float bw = waitUgui ? 1270f : LayoutSpace.Px(1270f);
+                float bh = waitUgui ? 70f : LayoutSpace.Px(70f);
-            Check(Mathf.Abs(wait.BarWorldW - 1323f / 108f) < 0.01f, …);   // 读的是本件自己的常量 ⇒ 恒真
+            if (wait.OnCanvas) { var fs = wait.FrameSizePx;
+                Check(Mathf.Abs(fs.x - 1323f) < 0.01f && Mathf.Abs(fs.y - 90f) < 0.01f, …); }
+            else { （原两条原样保留） }
-            Check(wait.BarFramePieces >= 6, …);
+            if (wait.OnCanvas) Check(wait.FrameIsSliced, …); else Check(wait.BarFramePieces >= 6, …);
-            Check(wait.BarFillPieces == 42, …);
+            if (wait.OnCanvas) Check(wait.FillIsTiled && FillSizePx==1323×90 && FillPpuMulValue==2, …);
+            else Check(wait.BarFillPieces == 42, …);
```

### 2·b 行尾核对（**二进制读**，改完当场对）

```
MyGame/Assets/CardPresentation/Editor/BattleScene.cs   CRLF=25415  LF=25415  loneLF=0   ⇒ 纯 CRLF，未翻 ✓
（改前 = 25245/25245 纯 CRLF，与本件开工时现核一致；`+170` 行与 2·a 相加吻合）
```

### 2·c 逐文件 `git diff --numstat`

```
901	130	Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
```
🔴 **这个 901/130 不是本件的**（该文件开工前就压着别的写手的活）——
**本件自己的贡献 = +210 / −45（净 +165）**，切法见 2·a（按 hunk 新侧起始行分）。
其余（`WaitBanner.cs` · `BattleDriver.cs` 那些）**都是别人的**（`EW7` 等）。

### 2·d ✅ 顺带核过、**判定「不用改」**的（省下一轮的重查）

| 处 | 结论（判据） |
|---|---|
| `CheckHudBox` 的 ④ `HudBlockRel`（`:536` 一带） | ✅ **两档都自洽、不用改**：`Middle` 档下 `VOffsetWorld` **恒 0**（`VOffsetWorldNow()` 里那句 `if (tier != Top && tier != Bottom) return 0f;`），剩下的两项（`localPosition` + `textBounds`）**在同一个局部空间**里比 ⇒ 迁移不改语义 |
| ⑤ `lb.FontPxNow >= mn-0.06 && <= mx+0.06`（`:545` 一带） | ✅ 已经是**实例口**（`FontPxNow` → `FontSizeToPxNow(..., ActualFace)`）⇒ 画布档本来就对 |
| `HudPxPerWorld`（`:446`） | ⚠️ 本件改完之后**全文件零代码引用**（只剩本件写的那句「⛔ 别拿它折回去」）⇒ **候选删除**，但**没删**（它是有名有据的换算常量，删它不算「修断言」；留给收口波一次性清） |
| `Quad()` 那一族的**前提** | ✅ **原来就没有前提条**、也没有 `return` 静默跳过的写法 ⇒ 12 条是**直接红**、不是「前提红 + 静默」两段 |

---

## ③ 接线清单（白名单外谁该改哪一行）

### 3·a 🔴 `Battle/BattleDriver.cs` 的 `HudExtraDrawnPx`（**本件没动** —— 白名单外、且不在点名的两族里）

| 位点 | 该改什么 | 判据 |
|---|---|---|
| `Battle/BattleDriver.cs:6686-6694` `HudExtraDrawnPx` | 实现是 `(q.WorldW * 108f, q.WorldH * 108f)` —— 对**已迁画布**的件，那个「世界尺寸 ×108」**在非 16:9 下不等于设计 px**。应改读 `HudCanvas.SizePx(rt)`（但它拿的是 `_hudExtras` 里的 `HudPiece`，要补一个「按名取 `RectTransform`」的口，或在 `HudPiece` 上补一个 `SizePx`） | `HudPiece` 文件头 + `SetWorldHeight` 的 doc 明文：「`q.WorldH * 108f` 在窄屏是**设计 px 的 0.75 倍**（`T09` §七·8）」；而**批处理实测就是 640×480**（`Core/LayoutSpace.cs:174` 自证句）⇒ 那两条断言**现在按 0.75 倍量、必红** |
| `Editor/BattleScene.cs:14413` / `:14418`（`A1302` 的两条 `Check`） | 期望值 109.008×105.601 / 61.846² 写的是**原版设计 px** ⇒ 与上面同一个病 | 同上 |

### 3·b ✅ 核过「本来就是对的」（⛔ 别去"顺手修"）

| 处 | 为什么不用改 |
|---|---|
| `Editor/BattleScene.cs:14828` `ImageQuad HudQuad(n)`（`Aspect Ratio Filler{Top,Bottom}`） | 🔴 **那两件【没迁】** —— `BattleDriver.BuildOneAspectFiller` 里白纸黑字写着「⚠️ 这一件**没迁**（不在那一族里）⇒ 仍走旧口」⇒ 它仍在 `hudRoot` 下、**仍是 `ImageQuad`** ⇒ `hudRoot.Find` + `GetComponent<ImageQuad>()` + `WorldW * 108f` **三样都对** |
| `Editor/BattleScene.cs:11401` / `:11555`（`GetComponent<ImageQuad>()` 找 `slider_handle`） | ✅ `WfSlider.cs:406` 那句话**仍是** `s._handle = ImageQuad.Create(s._root.transform, handleTex, …, "slider_handle")` ⇒ 世界档、有 `ImageQuad` 组件 ⇒ 读法对 |
| `Editor/BattleScene.cs:14361` / `:14386`（`A513` / `A531`，走 `HudExtraWorldPos`） | ✅ **迁移不改它的语义**（本件推导过，见 ④·c）—— 不用改，⛔ 也别被「世界」两个字骗着去改 |
| `HudExtraPosPx` / `HudExtraZDelta` / `HudExtraVisible` / `HudExtrasMissingArt` | ✅ 分别是 `Anchor01()`（**归一化、无量纲**）· `OrderZ`（**画布档的 z 档**）· `activeSelf` · `Texture` ⇒ 四个口**两档同义** |

---

## ④ 判据与出处（每族：期望值来源 · 改完什么条件下红/绿 · 数条数的 grep）

### 4·a 族 A（20 前提 + 148）

- **查找口的判据**（为什么必须换）：`BattleDriver.Hud()` 的 `cv = root as RectTransform; if (cv == null) cv = HudCanvas.Ensure(cam);`
  —— 生产路传的是**裸 `Transform` 的 `hudRoot`** ⇒ 落在**根画布**下；而 `HudCanvas.Ensure()` 里是
  `new GameObject("BattleHud Canvas")`、**没有 `SetParent`** ⇒ 根对象（`HudCanvas.cs` 文件头第 2 条明文
  「⛔ 根 canvas 不能挂在 HUD 根下 ⇒ 建成根对象」）。⇒ `hudRoot.Find`（`Transform.Find` **不递归**）**恒 null**。
- **期望值来源**：`bw/bh` 与会话里那 16 组 `CheckHudBox(...)` 的**实参原文**（原版 prefab 解算的屏幕矩形 = 设计 px）；
  `mn/mx/bs/wrap` 同理是原版 TMP 字段原文。⛔ 一条都不读 `BattleDriver.Hud` 的注释表。
- **改完会绿的条件**：① 根画布建起来了（`cam != null` 且 TMP 字体资产在）② 这 20 颗**都建出来**、且都是
  **根画布的直接子件**、**名字逐字相同** ③ `Hud()` 把 `boxWpx/boxHpx/auto*Px` **原样（设计 px）** 喂给 `SetAutoFitBox`
  ④ `SetWrappingMode(wrapMode)` 照旧还原折行档 ⑤ `stretch → anchor 摆回节点原点`（④那条）成立。
- **改完会红的条件**（逐条坏法）：
  · 根画布或某一颗件没建（`Hud()` 早退）⇒ **前提红**（且它下面 9 条不跑 —— 前提那一条自己就是红的，⛔ 不是静默）；
  · 谁把 `Hud()` 的框参**改回 `LayoutSpace.Px(...)`** ⇒ `sizeDelta` 变 ÷108 ⇒ **32 条红**（16 颗 × 2）；
  · 谁把字号口径**改回静态 `Label.FontSizeToPx`** ⇒ **48 条红**（16 颗 × 3）；
  · 折行档/四格/摆位回退 ⇒ 对应的那几条红。
- **⚠️ 一个已知的**弱化**（如实记）**：换口之后「**根画布不许挂在 `hudRoot` 下**」这条**不再由本助手守着**
  —— 但 **`EW6` 在 `:1920-1975` 加的那条「鉴别力前提」**（`drv.hudRoot != HudCanvas.RootRect.transform` 等 3 项）
  **正是守它的**，两条配套 ⇒ **覆盖没有丢**。

### 4·b 族 B（12 条）

- **期望值来源**：`AvatarItemSmall_{Me,Foe}` = **88.7274449 × 99.1251923 px**、`AvatarImage_{Me,Foe}` 高 = **158.6003077 px**
  （`menu_dump` 视觉框 · 父链 0.8 已含，逐值写在原断言的注释里，**本件一个字没改**）；`k` 的口径 =「我们那面框 ÷ 原版框架」⇒ 应 ≈ 1.0000。
- **读法判据**（本件的核心选择）：`HudCanvas.MakeAnchored` 把 `hPx/wPx` **原样**写进 `sizeDelta`（`MakeAnchored` 里那句
  `rt.sizeDelta = new Vector2(wPx, hPx)`），而 `HudImageTex` 的 `hPx = worldHeight * 108f` ⇒ **`sizeDelta` 就是那对设计 px**。
  ⛔ **不许用 `WorldH × 108f` 反推** —— 同族 doc 明文：那个在非 16:9 下是**设计 px 的 0.75 倍**（`T09` §七·8；批处理 = 640×480）。
- **改完会绿的条件**：① 四件都建出来、都是**根画布的直接子件**、名字逐字相同 ② 每件都有 `Image` + `sprite` + `texture`
  ③ `sizeDelta` 非退化 ④ `Avatar Tex` 取得到（`Resources/Art/avatars/` 在）⑤ **`Player_Profile_Border` 那张图的宽高比 = 256/286**
  （第 5、6 条钉的那对 88.7274449 / 99.1251923 就是按这个比例算的 —— 比例变了那是**真差异**、该红，⛔ 不是本件算错）
  ⑥ 三个 z 档没被人写反。
- **改完会红的条件**：删掉 `HudAbsTex` 那两句 ⇒ 4 条红；`HudAvatarSize` 去掉 `× HudPlateScale` ⇒ 绝对值那 4 条红；
  `HudAvatarZ` 改回 `HudImageZ` ⇒ ④ 两条红；`ChatButton` 的 z 改回默认 ⇒ ④ 前半红。
- **数条数的 grep**：见 ①·1·b 那张表下面列出的 12 个标识符（`IsDrawn`/`QHpx`/`QZ` 的**消费点**逐个 grep，
  再按「`Check` 在不在 `foreach` 里 × 循环长度」乘出来 = **8 个 `Check` 语句 → 12 次执行**）。

### 4·c ✅ 顺带推导过的：`HudExtraWorldPos`（`A513`/`A531`）**为什么不用改**

`HudExtraWorldPos` 给**世界坐标**，断言比的是 `drv.hudRoot.TransformPoint(LayoutSpace.ToWorld(x01, y01))`。
画布档下那一件是根画布的子件 ⇒ 它的世界坐标 = `(x01 − 0.5) × 画布的可见宽`（根画布铺满相机视野，
中心 = 画面中心）。而 `LayoutSpace.ToWorld` 的 x 项**用的也是 `VisibleWidth`**（`:129-131`，
`(x01 − 0.5) * VisibleWidth`）⇒ **两边逐位同式**。
逐档验过：`1920×1080`（1/108）· `640×480`（1/144，两边同为 1/144）· `2560×1080`（1/108）**都相等** ⇒ **不用改**。
（⚠️ 与 `HudExtraDrawnPx` 的差别：那个是 `世界尺寸 × 108`，把 **1/144 当成 1/108** ⇒ 才会 0.75 倍。）

### 4·d `WaitBanner` 组

- **期望值来源**（一律原版字面量，出处 = `EW7_WaitBanner迁画布_报告.md` ④·3 +
  `bundle_scenes_scenes_battlearena1` / `runtime_ui_dump_drive_0912.tsv:350-355`）：
  `anchor (0.5,1)` + `ap (7.0, −175.1)` ⇒ 条心 **(967, 175.1)**；根 `WaitText` `sd` = **1344 × 79.44**；
  框 `Generic Popup Background` = **1323 × 90** · `m_Type = 1`；填充 `Background fill` = `m_Type = 2 (Tiled)` · `m_PixelsPerUnitMultiplier = 2.0`；
  压暗 `Dark Shade` = **3963.5 × 3366** · **`m_Sprite = null`**；文字框 **1270 × 70**。
- **改完会绿的条件**：生产实例**真的登记了画布宿主**（`BattleHud.BuildHud` 传 `HudCanvas.RootRect`）、
  三层都建出来、`PlaceAtBarCenter` 走的是 `SetAnchorPx`（**不是裸 `localPosition`**）、
  `HudCanvas.SetPxSize` 在根上写过、`Add(根, Z=-0.45)` 那一档没被写回默认。
- **改完会红的条件**：`BuildHud` 那个实参被删 ⇒ ① 红（且 ②~⑥ 不跑 —— **前提条自己红**）；
  落点改 `localPosition` ⇒ ② 红；删 `SetPxSize` ⇒ ③ 红；给压暗层塞图 ⇒ ④ 后半红；
  `Add` 那一档写回 0.3 ⇒ ⑤ 红；三层任意两个 z 写反 ⇒ ⑥ 红。
- **⚠️ 一条**量纲安全**但**判别力弱**的，本件就地加强了**：原 `BarWorldW/H` 读的是 `U(PopupW)`（本件自己的常量）
  ⇒ 世界档下**恒真**。本件把它的画布档那一支换成 `FrameSizePx`（真读回 `sizeDelta`）—— 世界档那一支**原样保留**
  （`§4.5b` / `§A218` 那些世界档探针靠它）。

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **一条 `*.Run` 都没跑**（本波口径）⇒ **上面所有「会红/会绿」都是静态推导**，本件**没有**任何一个绿/红读数。
   要真数只能等 `T09` 的统一验收（`BattleScene.Run`）。
2. **族 B 的「确切条数 = 12」是按代码静态数出来的**（见 ①·1·b），我没有执行过任何一条 ⇒ 那个 12 是**执行次数**
   （8 个 `Check` 语句 × 4 个 `foreach` 里各跑 2 次），**不是**「今天实测红了 12 条」。
3. **`A1302`（3·a）那两条到底今天红不红，我没跑** —— 机制与量纲是核过的（`HudPiece` doc 的 0.75 倍那条 + 批处理
   实测 640×480），但**条数与红绿未验**。⛔ 本件按授权**没改**它（白名单外 + 不在点名的两族里）。
4. **`EW7` §3·3（画布档「美术取不到」那一档的探针）与 §3·4（`§A218` 并排画布探针）没做** —— 不在协调者点名的
   §③·1 / §③·2 里；`§4.5b` / `§A218` 那两节本身**仍是世界档**（`CreateWithArt` 不传宿主）⇒ 照旧绿 ✓。
5. **`EW7` §3·5（`HudCanvas` 缺一个正式平铺口 `MakeTiledPiece`）没做** —— 那是 `HudCanvas.cs`，白名单外。
6. **`HudPxPerWorld`（`BattleScene.cs:446`）现在零引用**（见 2·d）—— **没删**。
7. **`WaitBanner` 那三层 z 值本身是「我们挑的」**（原版那三层的 z 关系无判据，`V4b_三件口径.md` §Q2）——
   本件那两条层级断言只保证**相对次序**与原版一致（压暗盖图盖字），⛔ **不声称「原版就是这个数」**。
8. **没进真机、没截图**：这一波的判据全在**读回口**（`sizeDelta` / `anchorMin` / `OrderZ`）上，与「画出来像不像」
   是两件事 —— 「4:3 / 21:9 下图与字同尺」那条仍要真机三档验（`EW4` ④·b 末段的承诺）。
9. ⚠️ **收尾复跑时出现过 2 个运行时程序集错，全部在 `Battle/WfSlider.cs`**（`CS0103: ApplyPpuMul` 找不到 ×2），
   **不是本件的** —— 那是**另一个写手正在改 `WfSlider.cs`** 的半成品状态（本件对那个文件**一个字没碰**，
   只**读过**它）。判据：① 报错文件是 `WfSlider.cs`；② **编辑器程序集 = 0**（本件改的 `BattleScene.cs` 就在
   编辑器程序集里、编得过）；③ **75 秒后复跑 = 0/0**（对方写完了）。本件的四次数值：**0/0 · 0/0 · 0/0 · （3 次 0/0）**，
   末次 = **运行时 0 / 编辑器 0**。

---

## ⑥ 顺手发现（只报不改）

1. 🔴 **还有**第三处**「找已迁件、却还在用旧口」—— `HudExtraDrawnPx`（`A1302`）**（接线 → ③·a）。
   它拿的是 `_hudExtras` 里的 `HudPiece`，实现 = `q.WorldW * 108f`，而 `HudPiece` 自己的文件头写着
   「`q.WorldH * 108f` 在窄屏是**设计 px 的 0.75 倍**」；**批处理正好是 640×480**（`LayoutSpace.cs:174`
   那行自证句：「实测 `Screen.width/height = 640×480`」）⇒ `Editor/BattleScene.cs:14413` / `:14418` 那两条
   （`A1302` 的进攻卡钮 109.008×105.601 与相机钮 61.846²）**现在按 0.75 倍在量、按机制必红**。
   ⚠️ 这一处**可能是 `T09` 批 3 的同批连带**（`HudAbs` 迁画布之后这个口才失准）——
   与 `EW6` ⑥·1 记的 `Quad()` 那一族是**同一个病根**，只是入口换成了 `BattleDriver` 的读口。
2. ✅ **`hudRoot.Find` 那个坑已经清零**：现读全文件**只剩一处活的代码引用 = `:14827`**（`HudQuad` 里那句，
   而那两件 `Aspect Ratio Filler` **没迁、本来就对**，见 3·b），另有 **5 处纯注释**（`:468` / `:475` / `:14690` /
   `:15244` / `:17198`）。⇒ 「**迁了一族之后，`grep hudRoot.Find` 是最好的抓手**」这句仍然成立（`EW6` ⑥·2 记的）。
3. ⚠️ **`HudCanvas.RootRect` 是【进程级静态】的根画布** ⇒ `RootRect.Find(name)` 取的是**第一个**同名件。
   同一进程里**建第二个 `BattleDriver`** 时，根画布**不会**跟着销毁（它是根对象）⇒ 新实例的同名件会与
   **上一实例的残留**同名、而 `Find` 返回**旧的那个**。**本宿主安全**（`BattleScene.Run` 只在 `:24560` 建一次驱动），
   但**别的宿主别照抄**：`Editor/NetBattleTest.cs:516 / :798 / :826` 那几处会建多个驱动
   （旧法 `hudRoot.Find` 是**按实例**的、没有这个问题；换口之后就有了）。
   ⇒ 若哪天要在这类宿主上用本读法，得先给它一个「按实例过滤」的判据（例如记下建件时的 `transform.parent`）。
4. ⚠️ **`BattleScene.cs:4041-4050` 有一个可抄的先例**（`BattleLogPanel` 的 `FrameSizePx`）：
   那边 2026-10-16 已经**改成读 `HudCanvas.SizePx`（真 `sizeDelta`）**、注释里还明写「改走 `FrameSizePx`」——
   本件族 B 的口径与它**同一条**（不是新发明的）。⇒ 下次遇到「已迁件的尺寸读回」照它抄。
5. ℹ️ **`EW6` ④·e 那条「`LayoutSpace.Px` 在 `BattleScene.cs` 只剩 11 处世界档探针」已过期**：
   本件现读 **`grep -c "LayoutSpace.Px(" = 14`**（`EW6` 之后又有别的写手改过）。
   本件只动了其中**指向已迁件**的那几处（族 A 的 2 处 + `WaitBanner` 的 2 处）；其余 10 处**没逐个复核**（如实记）。
6. 🔴 **`WaitBanner.BarHasArt`（= `BarFramePieces == 9`）现在是【死口】** —— `grep -rn "BarHasArt"` 现读：
   只剩它自己的声明（`WaitBanner.cs:159`）＋ `MulliganPanel.cs:1133` / `CardChoicePanel.cs:591` 那两个
   **同名但是另一件**的口（`:15921` / `:16297` 用的是它们）。⇒ 既没人读、画布档下又恒 `false`
   ⇒ ⛔ **别拿它当「美术取不到」的判据**（画布档会误判）；本件 §4.5 **没用它**（画布档那边用 `FrameIsSliced` / `FillIsTiled`）。

---

## 可直接粘进总表的一行（≤3 行）

| **`EW8`** | **`T09` 断言收尾：`hudRoot.Find` 两族 + `WaitBanner`（§4.5）** | ✅ **族 A**：`CheckHudBox` 16 个调用点（**16 前提红 + 144 静默**）+ `hudNoBox` 4 颗（**4 + 4**）全清（查找口 → `HudCanvas.RootRect` · `Px()` → 原样设计 px · 静态字号口 → 实例口 `FontSizeToPxNow`）· ✅ **族 B**：`Quad()` 恒 null 那一族 **确切 12 条**全修（`sizeDelta` 设计 px + `OrderZOf` z 档；⛔ 不用 `WorldH×108`，那在批处理 640×480 下是 0.75 倍）· ✅ **`WaitBanner` §4.5**：3 条必红分档修 + 1 条**恒真**（`BarWorldW/H` 读的是自己的常量）改真读回 + **新增 6 条**画布档判别式 · `typecheck` **0/0 ×3** · 行尾**纯 CRLF 未翻** · 本件贡献 `+210/−45`（19 个 hunk）· ⛔ 未跑 Unity / 未真机 |
| | 🔴 **须跟一笔**：**`HudExtraDrawnPx`（`A1302`）是同一个病根的第三处** —— `q.WorldW*108f` 对已迁件在 640×480 下只给 0.75 倍 ⇒ `BattleScene.cs:14413/:14418` 两条按机制必红（判据 = `HudPiece` 文件头 + `LayoutSpace.cs:174` 的「批处理实测 640×480」）；⛔ 本件未改（白名单外、非点名两族）。另：`HudCanvas.RootRect` 是**进程级静态** ⇒ 多驱动宿主（`NetBattleTest`）**别照抄** `RootRect.Find` 读法（本件 ⑥·3） |
