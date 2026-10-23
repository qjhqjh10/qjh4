# 卡牌线 · `新账-89`（中文卡面切 CJK 面 + 重标 `CalibScale`）—— 执行报告 2026-10-23

> 执行代理 `a89`。**没跑 Unity / player / `-executeMethod`**（只有调度台能跑）。
> 跑过：`TMPDIR=/tmp/wf_a89 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> **没动 git**。读了 `d:/2` 的 bundle（只读，用 UnityPy）+ 参考图（只读）。

---

## 〇 一句话结论（先看这段）

🔴 **简报里的前提被原件推翻了两条，所以本轮【没有】做「正文切 CJK 面」这件事。**
硬证据（用 UnityPy 解开原版 bundle 现读，不是推断）：**原版卡面正文的 `m_fontAsset` 就是 `Pragati-Regular SDF`**。
再按参考图反推，`CalibScale` 的「数字客户端」口径值是 **≈0.985（≈原版原样的 1.0）**，不是 0.74；
但把它改成 1.0 会让**拉丁卡面图标同步 +19%**、并把 ⑨ 那条 PnP 锚点顶红 ⇒ **那是「换尺子」级别的决定，留给调度台裁**。

本轮实际交付：① 把证据与两把尺子的适用边界写进 `IconSetup.cs` 的 `CalibScale` doc（铁律 5，就地订正）；
② 在 `Verify` 末尾加 **⑪ 只读数、不断言的诊断**（把「图标跟拉丁后 / 跟汉字后」两个面各量一遍）。
behavior **零变化**。

---

## 一 结论（各项：改前 → 改后）

| # | 项 | 简报要求 | 本轮实际 | 依据 |
|---|---|---|---|---|
| 1 | 卡面正文/关键词的**主字体面** | 中文 ⇒ `Face.Cjk` | **⛔ 不改，保持 `Face.LatinCardBody`** | 原版卡预制体 `DescTextUnit`/`DescTextTactic` 的 TMP `m_fontAsset` = `Pragati-Regular SDF`（§二·1） |
| 2 | `KeywordFontSize` 的 em 口径 | 「按 `Face.Cjk` 的 em 重算」 | **⛔ 不改，仍是 `FontSizeForEm(LatinCardBody, …)` = 2.3552** | 换成 CJK em 会得到 2.760，中文正文**比原版大 17%**（§二·4） |
| 3 | `CalibScale` | 重标到「图标÷汉字 = 1.29~1.375」 | **读数与推算已给：≈0.985**；**没改常数**（要调度台裁，见 §三·5） | 参考图 1.292；模型 1.307（差 1.2%） |
| 4 | 断言 | 正例 + 灭自证 | **只加了 ⑪【读数】**，**没加 Check**（理由见 §五） | 目标值本身有两把互斥的尺子，先裁再断 |
| 5 | 文档 | 写清两把尺子的边界 | ✅ 已写进 `IconSetup.cs` 的 `CalibScale` doc | §二 |

**图标**这一侧本轮**一个字都没改**（`CalibScale` 仍 0.842，资产不用重建）。

---

## 二 为什么「切 CJK 面」这条前提不成立（逐条对简报的三条「硬证据」）

### 1 🔑 原版卡面正文的字体资产 = **Pragati**（原件读数，最硬）

用 `UnityPy` 打开 `d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle`：

* `GameObject/DescTextUnit` 的组件 `1782256530001533888` 是那枚 TMP：
  `m_fontAsset` = **PPtr(FileID 11, PathID 3485036404935369831)** · `m_spriteAsset` = **(0,0) = null** ·
  `m_fontSize 23.55` · `enableAutoSizing 1` · `min/max 1/24` · `lineSpacing 5` · `m_isOrthographic 1`
* 那个 PathID 落到 `fonts_assets_all.bundle` 里 = **`Pragati-Regular SDF`**（同 bundle 里四个字体资产的 PathID 全读了一遍）。

同一次现核的**对照**（证明这套解析没走偏）：

| 层 | 原版 `m_fontAsset` | 我们代码里那一处 |
|---|---|---|
| `NameTextUnit` / `NameTextTactic*`（含 `Big` / `No description`） | **Asar-Regular SDF** | `Face.LatinUi` ✔ |
| `DescTextUnit` / `DescTextUnit Big` / `DescTextTactic*` | **Pragati-Regular SDF** | `Face.LatinCardBody` ✔ |
| `ArmyTextUnit` / `ArmyTextTactc` / `RaceText*` | **Pragati-Regular SDF** | `Face.LatinCardBody` ✔ |

⇒ **我们的选择是对的，原版也没在中文卡上换主字体。**
（另外查过「运行期换字体」这条路：`grep -rn "set_font|\.font *=" d:/2/tools/decomp_full` 只命中
`I2.Loc` 对 **legacy `TextMesh` / `UI.Text`** 的两处，**没有一处 TMP 换面** ⇒ 不是运行期换的。）

⚠️ **原版中文到底靠哪条兜底渲出来的 —— 没查到**：`Pragati-Regular SDF.json` 的 `m_FallbackFontAssetTable` 是 `[]`（这条简报说的是对的），
而 `TMP_Settings`（全局 fallback 表）**在 `resources.assets` 里读不出来**（`read ok 0` —— 那份文件没有 type tree，
UnityPy 拿不到 DummyDll 就解不出字段名）。**如实记「没查到」**，⛔ 别把它当成「所以主字体一定是 CJK」——
那一步推理缺一环，而上面那条 `m_fontAsset = Pragati` 是**直接读数**。

### 2 ❌「`1` 带脚衬线 ⇒ 只有 CJK 能画」—— 不成立

把原版 `Pragati-Regular SDF Atlas.png` 里 `1` 的字形（`m_GlyphRect (920,556,35,60)`）按 alpha 渲出来看：
**Pragati Narrow 的 `1` 本身就有脚衬线**（竖干 + 左上小旗 + 底横）。
参考图 `抽 1 张牌` 里那个 `1` 的形状与它**不矛盾** ⇒ 这条证据不能用来判面。

### 3 ❌「行距 1.52 em 只有 CJK 能解释」—— 不能判面

中文那一行里，每个字的 ascent/descent 都取自**实际解析到的那一份资产**（走兜底时就是 CJK 那一份），
所以**两种假设下算出来的行距是同一个数**：

| 假设 | 行距/em |
|---|---|
| CJK 当主面 | (71.85 + 5)/50 × 0.9 ÷ (50×0.9)/50 = **1.537 em** |
| Pragati 当主面 + CJK 兜底 | 汉字仍走 CJK 的 ascent/descent ⇒ **同一个 1.537 em** |

实测：行距 **40 px** ÷ em **≈26 px** = **1.54 em** ⇒ 与**两者都吻合**。
⚠️ 简报里「Pragati 只有 0.947 em」那句只对**纯拉丁行**成立。

### 4 `KeywordFontSize` **不能**按 `Face.Cjk` 的 em 重算（会大 17%）

原版 `DescTextUnit`：`m_fontSize 23.55` · `m_isOrthographic 1` · CJK 兜底面的 `scale 0.9`
⇒ 中文那个字的 em = `23.55 / 50 × 0.9 × 1(orthographic) = 0.4239`（局部单位）× `localScale 0.01` × 卡高 3.3313 ⇒ **0.212 卡单位**。

我们的 `KeywordFontSize = FontSizeForEm(LatinCardBody, 0.2355) = 2.3552`，3D TMP（`×0.1`）下：
CJK 兜底字的 em = `2.3552/50 × 0.9 × 0.1 = 0.004239` ×… ⇒ **同样是 0.212 卡单位** ✔ **逐位对上**。

若改成 `FontSizeForEm(Face.Cjk, 0.2355) = 0.2355 / 0.08532 = **2.760**`：字形框 = `0.08532 × 2.760 = 0.2355`（比原版 0.2009 **大 17.2%**）。
⇒ **`KeywordFontSize` 保持现状才是对的**，简报那条「按 CJK 的 em 重算」会把中文正文整体放大 17%。

---

## 三 量法与读数（参考图逐项）

图：`C:\Users\qjh36\Desktop\warpforge参考图\点击卡片查看详情的参考.png`（1577×887 = 16:9）。
卡面效果文字块（卡内深色板）在 `x 620..950 · y 570..690`；第二行是 `到卡组。抽 1 张牌。获得 ①`。

| 量 | 读数 | 怎么量的 |
|---|---|---|
| 汉字 `获得` 墨高 | **24 px**（y 621..644） | 亮度阈值 70/80/90 三档都给 24（100/110 给 23） |
| 第 1 行 / 第 2 行 字顶 | 582 / 621 ⇒ **行距 40 px** | 同行剖面的首行墨顶 |
| `Atlas_SpiritStone_1` 绿环墨高 | **31 px**（x 916..944 · y 618..648） | 绿色掩码 `G>R+6` + `G>B+3`（阈值 6~10 都给 h=31；阈值 14 给 30、20 给 28） |
| 同行 em | **≈26 px** | 两条独立路：① 24 ÷ 0.93（Noto TTF 实测汉字墨占 `获 0.93 / 得 0.94`）② 40 ÷ 1.537 |

**图标 ÷ 汉字 = 31 / 24 = 1.292**（与简报给的 1.29~1.375 一致，落在**下沿**）。

**换成 quad/quad 才能和代码比**（实拍量的都是**墨**）：

| 项 | 墨占 | quad |
|---|---|---|
| 图标 | 图集里绿环 72 / 80 = **0.90**（`alpha>10` 现量） | 31 / 0.90 = **34.44 px** |
| 汉字 | 0.93 em ÷ 0.948 em（`WorldGlyphPerFontSize` 那个字形框）= **0.981** | 24 / 0.981 = **24.46 px** |

⇒ **目标 quad 比 = 34.44 / 24.46 = `1.404`**。

### 3·1 反推 `CalibScale`（量纲写死）

```
图标 quad = ascentLine × 字号/pointSize × face.scale × 0.1 × 字符scale(1.0) × 字形scale(1.65 × C)
          = A(面) × 1.65C × 字号
汉字 quad = WorldGlyphPerFontSize × 字号 = 0.08532 × 字号      ← 0.948 em × m_Scale 0.9（2026-10-21 起）

A(Pragati) = 70 / 95 × 1   × 0.1 = 0.073684  → 比 = 1.4255 × C
A(CJK)     = 57.55 / 50 × 0.9 × 0.1 = 0.103590 → 比 = 2.0035 × C
```

| 图标用哪一面的 faceInfo | 目标 1.404 ⇒ `CalibScale` | 现 0.842 下的读数 |
|---|---|---|
| **Pragati**（= 原版卡正文主面，实测最可能） | **0.985** | 1.200 |
| CJK（= 图标紧贴汉字时 TMP 会继承的那一面） | **0.701** | 1.687 |

🔴 **0.985 ≈ 1.0 = 原版根本不做标定**（原版字形表里 `Atlas_SpiritStone_1` 的 `m_Scale` 就是 **1.65**，
`Warpforge Trait TextSprites.json` 直接现读，没有任何标定系数）。这条「凑巧一样」是支持 Pragati 面的又一票。

### 3·2 为什么「Pragati 面」是更可能的那一支

`TMP_Text.cs:4018-4036`：`<sprite>` 走 `ValidateHtmlTag` 那一支，**不更新 `m_currentFontAsset`**
⇒ 图标的 `fontFace` = **它前一个字符**解析到的字体资产。
参考图里 `获得 ①` 之间那段空隙（得 的墨止于 x≈908，绿环墨起于 916，**≈8 px** ≈ 半个 em 的 0.3）**看得出有一个空格**；
空格是 Pragati 画得出来的 ⇒ 图标按 **Pragati** 的 `ascentLine 70` 算 ⇒ 0.985。

⚠️ **不确定性**：这是一个**空格**还是一次字距，图上分不绝对清；所以 ⑪ 把**两个面都量一遍**，实机读数一出来就能定。

---

## 四 改动清单（`文件:符号` + 为什么）

| 文件:符号 | 改动 | 为什么 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Editor/IconSetup.cs:CalibScale`（doc） | **+33 行注释**（纯注释） | 铁律 5：这个常数旁边原来只有 PnP 那一把尺子；把「数字客户端 1.292 / quad 1.404」「原版卡正文 = Pragati（PPtr 读数）」「两把尺子分母不同、⛔ 别互相折算」写死在它自己身上 |
| `Unity/MyGame/Assets/CardPresentation/Editor/IconSetup.cs:Verify` ⑪ | **+62 行新块**（**只 `Log`、不 `Check`**） | 把「图标跟拉丁后 / 跟汉字后」两个面的**渲染 quad 比**打出来 + 反推各自需要的 `CalibScale`。实测一出来，上面那条不确定性当场可判 |

`git diff --numstat` = **`121 / 0`**（**纯增**，没删改任何既有断言的判据）。
行尾：现读 `CRLF 0 / LF 1803+95`，与 HEAD 一致（没翻行尾）。

**没改的**（逐个说明）：

* `Core/CardView.cs` —— 一个字节没动（正文面维持 `Face.LatinCardBody`）。
* `Core/TmpFont.cs` —— 没动。
* `Editor/CardBaseDemo.cs` —— 没动。
* `工具/gen_icon_plan.py` —— 没动（本条与图标计划表无关）。
* **`CalibScale` 常数本身** —— 见 §三·5 的理由，等调度台裁。

---

## 五 断言：**本轮只加了读数，没加 `Check`**（理由 + 应该怎么补）

### 5·1 为什么没加

简报要的正例是「中文卡面的正文/关键词确实用了 CJK 面」，灭自证是「把主面改回 Pragati + 兜底 ⇒ 同一条必须红」。
**这条断言的前半句现在站不住**（§二·1：原版本来就是 Pragati），写成 `Check` 会**把一个错误的口径钉进自检**
——正是 `CLAUDE.md` 记过的那个坑（「一条老断言把这个错口径钉死了，自检绿不等于口径对」）。

尺度那条也一样：目标值有 **1.404（数字客户端）/ 1.65（PnP）** 两把互斥的尺子，
`CalibScale` 只能取一个；在裁之前写成 `Check` 必红其中一条。

⇒ 改成 **⑪【只读数】**：`Log` 出
`图标 quad（跟拉丁后）/ 图标 quad（跟汉字后）/ 汉字字形框 / 两个比 / 各自需要的 CalibScale`，
外加一句「两条读数差 1.41 倍 = 两个面 `ascentLine` 之比」。**不产生红**，但读数一到手就能裁。

### 5·2 裁完之后应该补的两条（现成写法）

假设裁定 **「数字客户端为准，`CalibScale = 1.0`」**：

1. **正例**（⑪ 改成 `Check`）：把 ⑪ 里那条「图标跟**拉丁**后的比」改成
   `Check(Mathf.Abs(rLatin / 1.404f - 1f) <= 0.10f, …)` —— 量的是**渲染几何**，⛔ 不断常数。
2. **灭自证**（结构上不可能同时满足）：
   * 把 `CalibScale` 改回 **0.842** ⇒ 比变 **1.200** ⇒ 红（< 1.264 = 1.404×0.90）；
   * 把探针的图标**改成跟在汉字后**（`"获<sprite …>"`）⇒ 比变 **2.00** ⇒ 红（> 1.545）；
     这条同时把「面的继承」这件事钉住；
   * **改了常数却没重跑 `IconSetup.Run`**（资产里还是 0.842）⇒ 量到 1.200 ⇒ 红。
3. 同时 **⑨ 必须重锚**（现在锚 PnP 1.65）：`CalibScale = 1.0` 下 ⑨ 会读到 `1.9833 × 1.0 = 1.98`，
   落在 `1.65 ± 10%` 之外 ⇒ 需改成「数字客户端口径」（或直接把 ⑨ 换成上面的新 ⑪ 断言，**别两条并存**）。

---

## 六 主对话要跑什么（命令 + 期望读数）

1. **`-executeMethod IconSetup.Run`**（或只 `IconSetup.Verify`，**资产不用重建** —— `CalibScale` 没变）
   * 期望新增一行：
     ```
     ICONSETUP ⑪ 读数（**只读、不断言**）：`CalibScale` 现读 0.842 · 汉字 `获`（解析到 `NotoSerifCJK-Regular SDF`）的字形框 0.20094 ·
       图标 quad：跟拉丁后 0.24112（比 1.200）· 跟汉字后 0.33896（比 1.687）
       —— 目标 … 1.404；要落到目标上，`CalibScale` 应各为 0.985（拉丁面） / 0.701（CJK 面）
     ```
     （两个 quad 的绝对数按 float 误差 ±2% 都算正常；**比值**是判据。
     括号里那句「解析到 `…`」是兜底闸：若是 `NotoSerifCJK-Regular SDF` ⇒ 汉字兜底挂上了；若是 `Pragati-Regular SDF` 或 `null`
     ⇒ `EnsureCjkFallback` 没生效，那一刻的「跟汉字后」读数不作数。）
   * ⑨ 那条应当**仍然绿**（资产没重建、常数没动）。
2. 若 ⑪ 的「跟拉丁后」读数落在 **1.20 ± 5%** ⇒ §三 的模型成立 ⇒ 可以按 §五·2 的裁法落 `CalibScale`。
   若它落在 **1.69 附近** ⇒ 说明探针里的图标被当成 CJK 面了，先把探针的写法（前一个字符）对一遍再谈。
3. **真机并排**（铁律 10 第 6 条）：`IconSetup.Run` 之后拿一张**有 `SpiritStone_1`/`questPoints1` 的中文卡**
   与 `点击卡片查看详情的参考.png` 并排比一次 —— 这一条是唯一能验「像不像」的，断言不算数。

---

## 七 没查清 / 判不准 / 顺手发现

1. 🔴 **原版中文到底靠哪条兜底渲出来 —— 没查到**。`Pragati-Regular SDF.json` 的 `m_FallbackFontAssetTable = []`（已核），
   `TMP_Settings` **读不出来**（`resources.assets` 无 type tree，`read ok 0`）。搜过：`fallbackFontAsset` / `m_FallbackFontAssetTable`
   在 `decomp_full`（25,096 个方法）只命中 `TestLoginManager__Start.c`；`set_font`/`.font =` 只命中 I2 的两处 legacy 组件。
   ⇒ **不影响图标那侧的结论**（图标按「前一个字符」的面算），但「原版中文用什么兜底」仍是空白。
2. ⚠️ **参考图里那个 `①` 的**艺术**与我们的图集**不完全像**：图集里 `Atlas_SpiritStone_1` 是**实心发光圆盘**，
   参考图里看着像**细绿环**（我用全部 87 个字形做轮廓 IoU 模板匹配，最高只有 **0.356**，没有强匹配）。
   但**名字是确定的**：官方本地化 `Modular_Description/Spirit_x` = `<link=spiritStone><sprite name=Atlas_SpiritStone_{0}></link>`
   ⇒ `((Spirit_x,1))` 就是 `Atlas_SpiritStone_1`。**差异大概是 31 px 小尺寸 + 截图压暗下辉光的观感**，没查实。
   🔴 **风险**：如果参考图那个符号的**墨占**不是 0.90，§三 的 `CalibScale` 会按比例偏（墨占 0.75 就变 0.82）。
3. 🆕 **顺手发现（真缺陷，建议立账）**：`<sprite>` 的缩放**继承它前一个字符的字体面**（`TMP_Text.cs:4018-4036`）。
   ⇒ **同一张中文卡里，图标前面是不是一个空格，会让图标差 1.406 倍**。
   我们现在没有任何地方钉这件事（`CardIcons` 的替换是「吃掉数字+空格」还是「保留空格」按 token 分支走）。
   证据：`Core/CardIcons.cs:334-346` 两条正则把「数字 + 空格 + token」整段换掉 ⇒ 换完图标前是**中文**；
   而 `①` 那种符号分支（`:347` 起）前面是什么就留什么。**两条路的图标会不一样大** —— 这是静默的。
4. 🆕 **旧读数不可再用**：`资料/普查产出_1023c/卡牌线_三条收尾_执行报告.md` §三 拿
   「我们 `questPoints1` 27 ÷ 26 = 1.038」当现状。按模型，`CalibScale 0.842` + Pragati 面应该是 **1.10 上下**，
   + CJK 面是 **1.55 上下**；1.038 两边都不贴 ⇒ 那次量的**不是同一对东西**（大概率把别处的字框当分母了）。⛔ 别拿它当锚。
5. 🆕 **`资料/普查产出_1023c/卡牌线_三条收尾_执行报告.md` §三 的三条「硬证据」需要就地订正**（铁律 5）：
   ① 「`1` 带脚衬线 ⇒ CJK」→ **Pragati 的 `1` 本身就有脚**；
   ② 「行距 1.52 em 只有 CJK 能解释」→ **两种假设同值**；
   ③ 「fallback 表是空 ⇒ 不可能靠 fallback」→ 只排除了**每份资产自己的** fallback，**全局 `TMP_Settings` 没查到**。
   该文件**不在本代理白名单**（只允许 `IconSetup.cs` / `CardView.cs` / `TmpFont.cs` / `CardBaseDemo.cs` / `gen_icon_plan.py`），
   ⇒ **请调度台改**；证据已写进 `IconSetup.cs:CalibScale` 的 doc（那条是白名单内、可长期引用的落点）。
6. `IconSizeProbe` 那条「绝对读数偏大 1.406×」的结论**仍然成立**（面差没变），本轮没动它。

---

## 八 一句话复述

**改了什么**：只改了 `Unity/MyGame/Assets/CardPresentation/Editor/IconSetup.cs` ——
`CalibScale` 的 doc 加了一段「原件读数 + 两把尺子边界 + 反推 0.985」的订正注释（纯注释），
`Verify` 末尾加了一块 **⑪ 只读数不断言**的两个面诊断；**behavior 零变化**（面没切、常数没动、资产不用重建）。

**验了什么**：`TMPDIR=/tmp/wf_a89 bash d:/4/Unity/工具/typecheck.sh` ⇒ **0 / 0**（改完 `.cs` 立刻跑的，含最后一次）；
⚠️ 中途有一次重跑报 **5 条红，全部集中在 `Assets/RuleEngine/Core/CountEval.cs`（引擎线别的代理正在写，不是我的）**
—— 隔 75 秒重跑即 **0 / 0**，与本条无关（按简报纪律如实记下，没有去动别人的文件）；
参考图自己重量了一遍（汉字墨高 24 px · 绿环 31 px · 行距 40 px · em ≈26 px ⇒ **图标÷汉字 = 1.292**，换算成 quad 比 **1.404**）；
原版 bundle 里 `DescTextUnit` 的 `m_fontAsset` 现读 = **`Pragati-Regular SDF`**（连带 Asar/Pragati 四个层级全部对上我们的 `Face` 选择）。

**还差什么**：① 调度台跑一次 `IconSetup.Verify` 收 ⑪ 的两个实测比值（期望 1.200 / 1.687）；
② **裁尺子**（PnP 1.65 vs 数字客户端 1.404）—— 裁完 `CalibScale` 才有唯一值（本报告推算 **0.985**，CJK 面那支是 0.701）；
③ 裁完把 ⑪ 改成 `Check` 并把 ⑨ 重锚（⛔ 别两条并存）；④ 顺手发现第 3 条（图标大小取决于前置字符的面）需要立账。
