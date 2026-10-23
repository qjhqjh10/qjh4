# 卡牌线 · 收尾两件（`CalibScale` 定值 + 并排探针断言）—— C2 执行报告（2026-10-23）

> 执行代理 C2。**没跑 Unity / player / `-executeMethod`**（只有主对话能跑）。
> 唯一允许的检查：`TMPDIR=/tmp/wf_c2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（连跑 3 次，均 0）。

---

## 〇、一句话结论

1. **A：`CalibScale` `0.776 → 0.842`**（+8.5%）。旧值的根因不是「2.85× 的量级差」，而是**三个口径错误叠在一起**（`2.0` 是「图标 ÷ **x-height**」不是「÷ 大写」、探针用的是 **CJK 面**而卡面正文面是 **Pragati**、以及把**名义值**当卡面值）。
2. **A②：`IconSetup.cs:67` 那条换算已整段重写**，每个数的量纲写死（见 §二·3）。
3. **A③：新增断言 `IconSetup.Verify` ⑨** —— 量**渲染出来的**「图标 ÷ 拉丁大写」，锚在成品卡图的 1.65 ±10%。
   ⚠️ **改常数后必须重跑 `IconSetup.Run` 重建资产**，否则 ⑨ 会红（资产里还是旧 scale）。
4. **B：`CardFactionProbe.Run` 末尾新增断言**「Picks 表 13 条全渲出 且 全程 0 失败」。
5. 🔴 **必须转给调度台的一条**：`IconSizeProbe.cs` 现在**用错字体面**（`TmpFont.Font` = CJK 面），它的绝对读数对卡面**偏大 1.41×** —— 探针要按面改（不在本代理白名单）。

---

## 一、A① 定值的推导（用哪两个读数、为什么）

### 1·1 先把「尺子」重新量一遍（这一步改了结论）

`IconSetup.cs` 原注释自称的尺子是 `Warpforge_12_Aggressor.png` 的「图标 48 px ÷ 大写 24 px = 2.0」。
**亲测（本轮，PIL + 连通域，`d:/2/Warpforge部队卡片/Dark Angels/3部队/Warpforge_12_Aggressor.png`，900×1200）**：

| 量 | 读数 | 怎么量的 |
|---|---|---|
| `questPoints1` 图标墨高 | **51 px**（阈值 90/110）；**48 px**（阈值 150） | 窗口 `x 550..650 · y 790..880`，`x[559..611] y[819..869]` |
| 同行**拉丁大写** `S` / `G` | **31 px** | 连通域 `(360,830,22,31)` / `(474,830,22,31)` |
| 同行**小写** x-height（`a/i/n`） | **23~24 px** | `(395,837,10,23)` / `(407,837,4,24)` |

⇒ **图标 ÷ 大写 = 51/31 = `1.65`**；**图标 ÷ x-height = 48/24 = `2.0`**。

🔴 **原注释「图标 ÷ 大写 ≈ 2.0」「大写高 h≈24 px」里那个 24 其实是一行的【小写字母】高度**。
所以「2.0」不是错的量级、是**标错了分母**（x-height 当大写）。这一条同时解释了「2.85× 矛盾」的一半：
`2.0`（x-height 口径）× `0.19`（大写口径）= 把两个量纲乘在了一起。

### 1·2 第二个口径错误：探针量的面 ≠ 卡面正文的面

`IconSizeProbe.Measure` 里写的是 `tmp.font = CardPresentation.TmpFont.Font`（**CJK 面**，`NotoSerifCJK`：
`pointSize 50 / scale 0.9 / ascentLine 57.55 / capLine 36`），而**卡面效果文字的正文字体是
`Face.LatinCardBody`（`Pragati`：`pointSize 95 / scale 1 / ascentLine 70 / capLine 60`）**
（`CardView.cs:4033-4035`）。

而原版 sprite asset 的 `m_FaceInfo` 是 **0** ⇒ TMP 走「**按正文面 faceInfo** 算缩放」那一支
（`Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TMP_Text.cs:4164`）：

```
spriteScale        = fontSize / fontFace.pointSize * fontFace.scale * 0.1
currentElementScale = fontFace.ascentLine / glyph.metrics.height * sprite.scale * glyph.scale * spriteScale
```

⇒ 同一字号下两面的图标比例 = `70×0.1/95` ÷ `57.55×0.9×0.1/50` = **0.711**。
即探针报的 `questPoints1` 墨高 **0.32 卡单位**（CJK 面）在**卡面真实那一面**下只有 **≈0.23**。

**这条模型被实物验证过**：按公式算 `DA65` 的 `armour` 图标 ÷ 同行 CJK 墨高 = **0.968**，
而 13 张并排里那张渲图实测 = **0.98**（图标 25.5 px ÷ CJK 26 px）⇒ **差 1%**，说明公式与口径都对。

### 1·3 为什么「图标 ÷ 卡高」不能当标定目标（A 的关键判断）

卡面效果文字要过 `CardView.FitToBox`（`CardView.cs:4267`）按版面框回缩，**正文字号不是名义值**：

* `DA65` 渲图：CJK 墨高 **26 px** / 卡高 **1001 px**（`CardView.Height 3.3313 × 300.54 px/单位`）= **2.6%**；
* 名义 em = `DescEmOfCard 0.0707` ⇒ **7.07% 卡高** ⇒ **回缩到名义的 ≈0.40**
  （这正是已记录的 `A4`「效果文字比原版小一大截」，`CardView.cs:4239-4246` 的注释里写着同一个形状）。

⇒「图标 ÷ 卡高」**逐卡不同**（文本越长越小），**不能**当 `CalibScale` 的锚。
原注释那句「目标 0.38 卡单位 = 卡高的 **11.4%**」只在**名义字号**下成立，与成品图的 4.0% **本来就不可比**
（成品图那 4.0% 是**印刷版式**；铁律 10 第 3 条：PnP 量「内容」可以、量「版式」不行）。

### 1·4 定值

名义字号下（卡面正文面 Pragati）：

```
图标 quad  = ascentLine(70) × glyph.scale × (fontSize/95 × 0.1)
大写 quad  = capLine(60)/95 × fontSize × 0.1
⇒ 图标 ÷ 大写 = (70/60) × glyph.scale = 1.1667 × glyph.scale
```

要等于成品卡图的墨比 **1.65**，且 `questPoints1` 的墨只占字形框 **79/80 = 0.9875**：

```
1.1667 × glyph.scale × 0.9875 = 1.65  ⇒  glyph.scale = 1.432
glyph.scale = 原版 m_Scale(1.7) × CalibScale  ⇒  CalibScale = 1.432 / 1.7 = 0.842
```

**为什么不用探针的绝对数直接算**：探针在 CJK 面下报 0.32/0.49 → 得 0.776，那是**CJK 面**的比例；
换到卡面真实那一面（Pragati）后同一组测量会给 0.23/0.35 → 系数 **0.84**（与上面独立算出的 0.842 一致）。
⇒ 两条路在今天这个字体配置下是**同一个答案**，`0.776` 是「正文面还不是 Pragati」那个年代的遗留值。

**旁证（不作判据，只说明方向对）**：13 张并排里把「文本被 FitToBox 缩小」这件事除掉之后，
我们的图标相对文本比 PnP 小 **18%**（armour）/ 大 **24%**（teleport），**两个样本的均值 ≈ 1.0** ——
即 PnP **逐图标**的尺寸并不守 atlas 自己的相对比例（见 §五·2），单张卡的读数不能当锚。

**最终值 = `0.842`（取 0.84）**，落在从三条独立路径（PnP 1.65 尺子 → 0.842 / 数字版客户端截图 → 0.85~1.15 /
「原版参数原样」→ 1.0）共同覆盖的低端，**是唯一有实物对齐验证的那一个**（§1·2 的 1% 吻合）。

---

## 二、A② 注释里的换算写清楚了什么（`IconSetup.cs:54-115`）

新增/改写的内容（全部带量纲）：

1. **`1.65`** = 成品卡图「图标墨高 ÷ **大写**墨高」，**无量纲**（51 px ÷ 31 px，可复量）。
2. **`0.155`** = 我们卡面效果文字**名义字号**下的**大写**墨迹高，单位 = **卡单位**
   （`KeywordFontSize 2.3552 × CapPerFontSize(LatinCardBody) 0.0656`）。
   ⚠️ 旧值 **0.19** 是 `TmpFont.Font`（CJK 面）量的，已作废。
3. `1.65 × 0.155 ≈ 0.255 卡单位` = 目标图标墨高（**名义**）；÷ `Height 3.3313` = **7.7% 卡高** —— **名义值**，
   `FitToBox` 之后逐卡不同，**不能当标定目标**。
4. 原链「`2.0 × 0.19 = 0.38` = 11.4% 卡高」逐条点名错在哪（x-height 当大写 / 面不对 / 名义当卡面）。
5. 探针口径与卡面口径差 **0.711 倍**的判据（`TMP_Text.cs:4164` + 两份字体资产的 faceInfo 数值）。
6. `⚠️ 改常数必须重跑 IconSetup.Run`（`Build()` 才把 `glyph.scale × CalibScale` 写进资产）。

---

## 三、A③ 新增断言（怎么才会红）

**位置**：`IconSetup.Verify`，⑨（`IconSetup.cs`，`③ 真渲一张` 之后）。

**量什么**（**不是**断常数本身）：

* 建一个 `TextMeshPro`，`font = TmpFont.FaceAsset(Face.LatinCardBody)`（**卡面正文面**）、
  `spriteAsset = 我们的资产`、`fontSize = CardView.KeywordFontSize`、`text = "H<sprite name=\"questPoints1\">"`；
* **图标**：取**渲染出来的网格 quad 高**（`InkQuadH`，sprite 的 quad 就是它 80×80 字形框，墨占 79/80 ⇒ 差 1%）；
* **大写**：用**正文字体自己的 `faceInfo`** 解析折算 `capLine/pointSize × scale × 0.1 × fontSize`
  （**不用字符 quad** —— 实测 `H` 的 quad 比 `capLine` 高 ≈11%，会把判据带偏 11%）；
* **判据**：`图标 ÷ 大写 ∈ 1.65 × (1 ± 10%)`，即 **[1.485, 1.815]**。

**预测值（`CalibScale = 0.842` + 资产已重建）**：`1.1667 × 1.4314 = 1.670` ⇒ **绿**（离上限 8%）。

**改坏法（逐条都必须红）**：

| 改坏 | 实测会变成 | 红？ |
|---|---|---|
| `Build()` 里 `g.scale * CalibScale` 改回 `g.scale`（= CalibScale 1.0） | 1.98 | ✅ 红（>1.815） |
| 正文面换成 `TmpFont.Font`（CJK 面，`TMP_Text.cs:4164` 用正文面） | 2.29（`ascentLine 57.55 / capLine 36`） | ✅ 红 |
| **改了 `CalibScale` 却没重跑 `IconSetup.Run`**（资产还是 0.776） | 1.54 | ✅ 红（<1.485） |
| 字形表里 `questPoints1` 的 `m_Scale` 被改成别的数 | 按比例变 | ✅ 红 |
| `sprite name` 写错 ⇒ TMP 不生成 sprite 字形 | 前提检查报错 | ✅ 红 |

⚠️ **如实写清判别力**：≈ **±10%**。它抓的是结构性漂移（换面 / 资产没重建 / 常数改了 10% 以上），
**不抓 ±3% 以内的调参**（那本来也不需要护栏）。

---

## 四、B：`CardFactionProbe` 的那条断言

**位置**：`CardFactionProbe.Run` 末尾（写完 `_manifest.tsv`、打完「完成 N 张」之后）。

* 新增计数 `okPicks`：只数 **① 循环**（`Picks` 表那 13 条）里渲成功的张数；
* 判据：`okPicks == Picks.Length && fail == 0`；
  * `fail` 是**全程**的（② 兜底循环失败也算）⇒「任意一张渲失败就红」；
  * ⚠️ **不拿 `ok` 总数比 13**：池里以后新增阵营会走 ② 循环多渲一张，那属于「多渲了」、不该假红；
* 打印：`[cardfaction] OK/!! ...`（`Check` 写法与 `IconSetup.Verify` 同一套）+ 末尾一行
  「断言全过（13 张全部渲出）」/ LogError「**断言没过 N 条**」；
* `Run()` 开头 `_assertBad = 0`（同一会话跑两次不累计）。

**改坏法**：① `Picks` 任一条 id 改成池里不存在的 ⇒ 兜底也选不出 ⇒ `fail++` ⇒ 红；
② 某个 `Pick.Faction` 写成不存在的阵营 ⇒ 该格 `continue`、`okPicks` 少 1 ⇒ 红；
③ `RenderOne` 里 `CardView.Create` 抛异常 ⇒ 红。

---

## 五、改动清单（逐文件 ±行，`git diff --numstat` 现读）

| 文件 | ±行 | 改了什么 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Editor/IconSetup.cs` | **+156 / −27** | `CalibScale 0.776 → 0.842`；整段重写它的 doc（量纲/两条仪器口径/为什么不能按卡高标定/新推导）；`Verify` 末尾加 ⑨ 断言；新增 `InkQuadH` |
| `Unity/MyGame/Assets/CardPresentation/Editor/CardFactionProbe.cs` | **+42 / −1** | `okPicks` 计数 + B 断言 + `Check` 辅助 + `_assertBad` 复位 |
| `Unity/资料/普查产出_1023c/卡牌线_CalibScale与断言_执行报告.md` | 新增 | 本报告 |

行尾：两份 `.cs` 都是 **LF**（`file -b` 与 `b.count(b'\r\n') = 0` 现核），与 HEAD 一致（diff 行数小，**没翻行尾**）。

---

## 六、调度台必须做的两件事（不跑就等于没改）

1. **`-executeMethod IconSetup.Run`** —— `Build()` 才会把新的 `CalibScale` 写进
   `Assets/CardPresentation/Resources/Fonts/Warpforge Trait TextSprites.asset`；
   **只跑 `Verify` 的话资产还是旧 scale，⑨ 会红**（这是设计好的行为，不是回归）。
2. **`-executeMethod CardFactionProbe.Run`** —— 验 B 那条断言；顺带出新一版 13 张并排图
   （旧图是 2026-10-23 14:08 的，早于本次图标尺度变化）。

---

## 七、没查清 / 建议另开的账

1. 🔴 **`IconSizeProbe.cs` 用错字体面**（`TmpFont.Font` = CJK 面，`Measure:` 里那一行）。
   它的绝对读数对卡面对**偏大 1.41×**，而它自称是「标定卡面图标」的仪器。
   **建议**：改成 `TmpFont.FaceAsset(Face.LatinCardBody)`，并在文档里写清「本探针只给**比例**，绝对值要按面换算」。
   ⛔ 不在本代理白名单（白名单只有 `IconSetup.cs` / `CardFactionProbe.cs` / `CardFaceProbe.cs`），**没动**。
2. 🔴 **我们 vs 原版**数字版客户端**的图标比**：`桌面/warpforge参考图/点击卡片查看详情的参考.png`（中文客户端的卡详情窗）
   里，`SpiritStone_1` 图标 **31~33 px** ÷ 同行 CJK **24 px** = **1.29~1.375**；
   我们 `DA65` 渲图里 `questPoints1` **27 px** ÷ CJK **26 px** = **1.038**；
   按字形 `m_Scale × 墨占` 归一（1.464 vs 1.679）后，**我们 ≈ 原版中文客户端的 0.70**。
   **但这条与 PnP（1.65）不同向**，根因大概是**同一支 `TMP_Text.cs:4164`**：原版**中文客户端**的正文面是 CJK 字体资产，
   我们是 Pragati + CJK 兜底 ⇒ 图标按 CJK 面算、比我们大 **1.41×**（0.70 × 1.41 ≈ 0.99，正好对上）。
   ⇒ **两件事要分开记**：① 图标尺度（本次已按 PnP 定值）；② **卡面正文的「主字体面」到底该是 Pragati 还是 CJK**
   （`CardView.Fill(... face: LatinCardBody)` + `TmpFont.FaceAsset` 的兜底关系）—— 那是**另一条账**，
   影响的不只是图标（字号墨高、行盒也跟着走）。**本轮没动**（`Core/**` 不在白名单）。
3. **PnP 逐图标的相对尺寸不守 atlas 的 `m_Scale` 关系**（实测：`armour` 我们 ÷ PnP = 1.84×，
   `teleport` = 1.21×，而两者原版 `m_Scale` 都是 1.65）⇒ **PnP 不能当「逐个图标」的尺子**，
   只能当「同一张卡上图标 ÷ 文字」的比例尺。这也是为什么本次只取 `questPoints1` 这一枚（原注释用的就是它）。
4. **没跑到实况**：本次全部是静态读数（PIL 量图 + TMP 源码公式），**⑨ 的实际读数要等 `IconSetup.Run`**。
   预测 1.670（±5%），若实测落在 [1.485, 1.815] 之外，先看是不是「资产没重建」。
