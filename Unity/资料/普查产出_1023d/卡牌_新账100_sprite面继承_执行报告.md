# 卡牌线 `新账-100`：`<sprite>` 的「面」继承 —— 执行报告（2026-10-23）

> 执行代理产出。判据起点 = `资料/普查产出_1023d/卡牌_新账89_CJK面_执行报告.md` §7·3。

---

## 一 结论（选的修法 + 为什么）

**修法 = 简报里「选项 1/2 的同一族」，但不用占位字符，用 TMP 自己的 `<font=Default>…</font>` 零宽作用域。**
落点在 **`CardView.Fill` 组字那一步**（`t.text = CardIcons.PinSpriteFace(text)`），
`CardIcons.Rewrite` 的产物**逐字节不变**。

### 1·1 病灶（判据，现读源码）

TMP 画 `<sprite>` 时，**sprite asset 自己没有 faceInfo** 就拿 `m_currentFontAsset.faceInfo` 算缩放：

```
spriteScale        = fontSize / fontFace.pointSize * fontFace.scale * orthographicMultiplier
currentElementScale = fontFace.ascentLine / glyph.metrics.height * sprite.scale * glyph.scale * spriteScale
```

* `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs`（`CalculatePreferredValues` 那一段）
* 渲染那两处同构：`TextMeshPro.cs:2554-2574`（3D）· `TextMeshProUGUI.cs:2903-2925`（UI）
* 分支条件 = `spriteFace.pointSize > 0` 才走「图集自己的度量」。我们的
  `Resources/Fonts/Warpforge Trait TextSprites.asset` 现读 `m_FaceInfo.m_PointSize: 0`
  （`Editor/IconSetup.cs` 的 `Build()` **照原版故意留 0**），⇒ 走 `else` 那支。
* `<sprite>` **不更新 `m_currentFontAsset`** ⇒ 图标继承**前一个字符**解析到的字体资产：
  · 拉丁/空格 ⇒ `Pragati`（`ascentLine 70 / pointSize 95 / scale 1`）⇒ A = `0.073684`
  · 汉字/中文标点（拉丁面没字形 ⇒ CJK 兜底）⇒ `NotoSerifCJK`（`57.55 / 50 / 0.9`）⇒ A = `0.103590`
  ⇒ 差 **1.406 倍**（与 `CalibScale` 无关：两条读数乘同一个常数）。

### 1·2 影响面（现读，2026-10-23）

| 生产者 | 是否受影响 | 说明 |
|---|---|---|
| `CardIcons.Rewrite` 方括号 / 符号 / 「数字烘在图里」三支 | **是** | `tag` 直接落在 token 原位置，前面是什么就继承什么 |
| `CardIcons.Rewrite` 裸关键词那支（`bare`） | **是** | `<nobr>` 不改 `m_currentFontAsset`，等效看**词前面那个字符** |
| `CardIcons.RewriteMarkTraits`（`[[枚举名]]`） | **是** | `<link=…><nobr><sprite …>` 前面全是标签，等效看 `[[` 前面那个字符 |
| `CardIcons.RewriteMarkModular`（`((Key,…))` → `QuoteSprites`） | **是** | 同上。官方 zh 列里 `((`/`[[` **紧贴汉字**的 **965 处 / 2370 处开口**（`数据/游戏数据/官方本地化_en+zh-CN.json` 的 `Card_Description/*`；例 `UM57` = `…使你的所有载具获得((Ranged_attack_icon_x,+1))`） |
| `CardIcons.RewriteFaithCost` | 否 | 留下的 `4 `（数字+空格）⇒ 空格是 Pragati 画得出来的 |
| `CardIcons.RewriteMarkMana` | 否 | 原版串自带前导空格（`" <sprite name=ManaN>"`），且今天 0 处输入 |
| `CardText.KeywordSegment`（关键词行） | **是**（且**同行内就不一致**） | 中文档各项用 **`。` 直接拼**（`string.Join("", …)`，`Core/CardText.cs` 末尾）⇒ **第 1 项**图标继承 Pragati、**第 2 项起**继承 CJK |

### 1·3 为什么不用简报里的两个具体手段

* **插 `U+200B` 占位字符**：`U+200B` **不在 Pragati 字表里** —— 现读
  `Resources/Fonts/Pragati-Regular SDF.asset`：250 个字符、码位 `32..382` + `8211..8482`，**没有 8203**
  ⇒ 插了要么落到 CJK 兜底（白插）、要么画 `.notdef`。
* **插普通空格 / `U+00A0`**：改排版宽度（简报明令禁止）。
* **`<font="Pragati-Regular SDF">`（按名字）**：也能钉住，但它会
  `AddMaterialReference(tempFont.material, …)` —— 卡面文字层的 `m_sharedMaterial` 是
  `ApplyOutline` 之后的**实例材质**（`TmpFont.ApplyOutline` 用 `t.fontMaterial`）⇒ 字体资产那份**共享**材质被登记成
  **新**材质引用 ⇒ `m_textInfo.materialCount` 多一格（`TextMeshPro.cs:1915`）⇒ TMP 给每条文字**多造一个空子网格**，
  `CardView.StampTmpQueue` / `SubtreeQueues` 还会把它一起盖章、克隆材质。**`<font=Default>` 不登记任何新材质**。
* **给 sprite asset 补 faceInfo**：那是**尺子 / `CalibScale`** 那一笔（会改**绝对尺寸**），本轮不碰。

### 1·4 为什么落在 `CardView.Fill` 而不是 `CardIcons.Rewrite`

1. 卡面正文 = `CardText.KeywordSegment(...)` + `CardIcons.Rewrite(...)` **两段拼的**
   （`BattleDriver.FaceTextFull`）⇒ ① 只改 `CardIcons` 覆盖不到关键词行；② `Core/CardText.cs`
   **本轮不在白名单**；③ `Fill` 是这张卡**所有文字层唯一的 `t.text` 写入点**（`CardView.cs` 里 `t.text = …` 只此一处）。
2. 不改 `CardIcons.Rewrite` 的产物 ⇒ `IconSetup` 的 ②c / ②i / ④a / ⑫ 那些**按字符串断言**的判据**逐字节不变**。

### 1·5 关于 `<font=Default>` 的两个「万一」

* **万一标签不被识别** ⇒ TMP 会把 `<font=Default>` **当普通文字印出来**。已按源码核实：
  `MarkupTag.FONT = 2586451 = GetHashCode("font")`、`MarkupTag.DEFAULT = -620974005 = GetHashCode("Default")`
  （`TMP_RichTextTagsCommon.cs:38,171`；哈希复算过），命中 `TMP_Text.cs:7385` 那个**特判分支**（不查表、不走 `Resources.Load`）。
* **万一是 static 数组拿到别的对象的字体** ⇒ 不会：`TMP_Text.m_materialReferences` /
  `m_materialReferenceIndexLookup` 确实是 static（`TMP_Text.cs:195-196`），但**每一条会解析标签的路都先过**
  `ParseInputText()` → `SetArraySizes()`（`TMP_Text.cs:2016-2044`；`GetPreferredWidth/Height` 那几个口也都先调它），
  而 `SetArraySizes` 开头就 `Clear()` + 把**本对象**的 `m_sharedMaterial`/`m_fontAsset` 登记成 index 0
  （`TextMeshPro.cs:1457-1460` · `TextMeshProUGUI.cs:1757-1761`）。

---

## 二 改动清单（符号名）

| 文件:符号 | 改动 | 备注 |
|---|---|---|
| `Core/CardIcons.cs` : **`PinSpriteFace(string text)`**（新增，public）+ 它上面那段判据 doc | **+83 行**（纯新增） | 幂等（已在 `<font=Default>` 后面的原样保留）；没有 `<sprite>` ⇒ **原样返回**（零拷贝）。位置在 `StripTags` 之后 |
| `Core/CardView.cs` : **`Fill(...)`** 里 `t.text = text;` → `t.text = CardIcons.PinSpriteFace(text);` | **1 行（1/1）** | 行尾 **CRLF 未变**（`git diff --numstat` = `1 1`）；`Fill` 是这张卡所有文字层唯一的 `t.text` 写入点 |
| `Editor/IconSetup.cs` : **`Verify()` 末尾新增 ⑬** + 5 个私有辅助（`SpriteNameAt` / `FindNthSprite` / `PrevVisibleChar` / `IsCjkChar` / `MeasureSpriteQuad`） | **+246 行**（相对开工时那版） | ⛔ **⑨ / ⑪ / ⑫ 一个字没动**；⛔ `CalibScale` / `KeywordFontSize` / `TmpFont.cs` 没动；⛔ 资产不用重建 |

`git diff --numstat`（我这三个文件，含别人先前的改动）：`CardIcons.cs 83/0` · `CardView.cs 1/1` · `IconSetup.cs 579/5`
（其中 `IconSetup` 的 5 行删除是**开工前** `新账-90` 代理留下的，不是本轮的）。

**行为变化（要如实告诉用户）**：以前「图标紧跟在汉字/`。` 后面」的那一批图标（官方 zh 里 965 处开口那一族 + 中文关键词行第 2 项起）
**会缩小到原来的 1/1.406**，与「前面是空格」的那一批**对齐**。这是本条的目的（一致性），但**画面确实会变**——
真机并排要比一次（见 §五·3）。

---

## 三 探针 / 断言（`IconSetup.Verify` ⑬）

### 3·1 夹具怎么挑（**不写死卡 id**）

扫 `RuleEngine.CardDatabase.Load()`，对每张卡取**卡面唯一那条组字路** `BattleDriver.FaceTextFull(c).body`，
在其中找**第一枚「有效」图标**（图名在 sprite asset 里，TMP 真生成得出字形）**且它前面最后一个真字符是汉字/中文标点**
（`PrevVisibleChar` 会跳过 `<link=…>` / `<nobr>` / `<b>` 这些标签）。两遍：
先要「≥2 枚有效图标」，没有再退到「≥1 枚」。

### 3·2 四条读数 + 五条 Check

| 记号 | 怎么造 | 期望 |
|---|---|---|
| `qRaw` | 真卡正文**不钉面**（= 今天之前的形状） | 大（继承 CJK 面） |
| `qPin` | `CardIcons.PinSpriteFace(真卡正文)`（= `CardView.Fill` 今天的形状） | 小（Pragati 面） |
| `qSpace` | 在 `qPin` 那枚图标**前面插一个普通空格**（模拟原版带空格的排法） | 与 `qPin` **相同** |
| `qLatin` | `"H<sprite name=\"<同一枚>\">"`（拉丁面参照） | 与 `qPin` **相同** |

1. ★ **正例**：`qPin / qSpace = 1.000 ± 2%` —— **图标大小与前面的空格无关**。
2. ★ **钉的是拉丁面**：`qPin / qLatin = 1.000 ± 2%` —— 挡「两边都钉成 CJK」那种假绿。
3. ★【**灭自证**】`qRaw / qPin > 1.20`（理论 1.406）—— **不钉面时确实更大**。
   ⚠️ 若哪天 sprite asset 自己带上 `faceInfo`（`pointSize > 0`，= 尺子那一笔），这一条**自动跳过并打印理由**（不是放宽）。
4. ★【端到端 · wiring】真建一个 `CardView`（`CardView.Create` + `BattleDriver.ToCardData`），
   它的关键词层文本里必须含 **`<font=Default><sprite`** —— 挡「探针量的和卡面用的不是同一条路」。
5. ★【端到端 · 一致】那一层够 **2 枚**图标时，`最大 ÷ 最小 ≤ 1.02`；只有 1 枚时**打印「量不到」**（不是假绿）。
6. 另外一条**前提** Check：卡池里找得到夹具（换数据后红了 = 要重新挑，不是放宽）。

### 3·3 主对话怎么跑 / 期望读数

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod IconSetup.Verify -logFile -
```

**不用跑 `IconSetup.Run`**：`CalibScale` 与 sprite asset 一个字节都没动，**不需要重建资产**。

期望在 `ICONSETUP` 日志里看到（数字按 float 误差 ±2% 都算正常）：

```
ICONSETUP ⑬ 夹具：`<卡 id>` · 全文有 N 枚有效图标（≥2 才能量「同一层大小一致」那一半）
ICONSETUP ⑬ 读数：夹具 `…` · 图标 `…`（正文第 k 枚有效图标）· quad 不钉面 0.0xxx · 钉面 0.0yyy · 钉面+空格 0.0yyy · 拉丁 `H` 后 0.0yyy
ICONSETUP OK ★ ⑬ **图标大小与它前面有没有空格无关** … = 1.000（目标 1.000 ±2%）
ICONSETUP OK ★ ⑬ 钉的是**拉丁正文面** … = 1.000（目标 1.000 ±2%）
ICONSETUP OK ★ ⑬【灭自证】**不钉面**时那枚图标明显更大 … = 1.406（必须 > 1.20，理论 1.406）
ICONSETUP OK ★ ⑬【端到端】真 `CardView` 的关键词层文本里**图标都钉住了** …
ICONSETUP OK ★ ⑬【端到端】同一张卡的关键词层里 N 枚图标**一样大** … ≤ 1.02
```

**⑨ / ⑪ 的读数不会变**：它们自己造串（`"H<sprite …>"` / `"获<sprite …>"`），**不过 `CardView.Fill`**
⇒ ⑨ 仍 ≈ 1.67、⑪ 仍是「跟拉丁 1.20 / 跟汉字 1.69」。⑪ 继续是「尺子那一笔」的诊断，本条没动它。

**怎么亲眼看到「改坏了会红」**：把 `CardView.cs` 的 `Fill` 里那句改回 `t.text = text;` ⇒ ⑬ 第 1 条读到 **1.406**、
第 4 条（wiring）也红。改回来即绿。

---

## 四 没查清 / 判不准 / 顺手发现

1. **没跑 Unity**（子代理不许跑批处理）⇒ 上面所有「期望读数」都是**源码判据 + 离线模拟**得来的，实机读数要主对话跑一次 ⑬。
   离线模拟口径：`cards_engine.json` + `card_icon_plan.json` + `官方本地化_en+zh-CN.json` + `official_names.json`，
   按 `Rewrite` / `RewriteMarks` 的规则在 python 里重放（脚本在 `_tmp_view/probe100/`，未进仓库）。
2. **夹具「图标紧跟汉字」的真卡数量不多**：离线模拟**官方正文那条路**只有 **30 张**卡命中、每张正文里 **1 枚**；
   **计划表那条路**（`descZh` 走自造记号）另有 **6 处**（`ASH86` / `851f73da…` / `EC30`×2 / `UM40` / `UM118`）。
   所以 ⑬ 的端到端「同一层大小一致」那半条**可能**只量到 1 枚 ⇒ 那种情况会**打印「量不到」**（不是假绿）。
   ⚠️ 真机上 `FaceTextFull` 还会**前置关键词段**（`KeywordSegment`），中文档同行的第 2 项起都吃 `。` ⇒ 实际枚数**很可能 ≥2**。
3. **两条 sprite 生产者的覆盖**：本轮的 `CardView.Fill` 是**卡面**的收口点，覆盖 `CardIcons` 全部 5 支 + `CardText.KeywordSegment`。
   **覆盖不到**：`Core/Tooltip.cs:791`（trait tooltip 自己拼 `<sprite>`，走 `Label`）—— 不在本轮白名单，提请立账（形态是「单行内部一致、与卡面可能不同大小」）。
4. 🆕 **顺手发现（数据侧，建议立账）**：`cards_engine.json` 里有卡片的 `descZh` **本身就带已渲染的标签**，
   例 `GSC65` = `选择一张<link=sabotage><sprite name=Atlas_trait_icon_sabotage>Sabotage</link>牌并将其加入对手的手牌`
   （`GSC7` / `GSC13` / `GSC40` / `GSC50` / `GSC56` / `GSC68` / `GSC70` / `c433e3dd…` 同族）。
   这是「自造记号那一代 / 官方渲染那一代」**混进数据层**的一种形态 —— 本轮没动（不在白名单）。
5. **判不准的一处**：`<font=Default>` 钉的是**这个 TMP 自己的字体**。今天卡面正文层恒为 `Face.LatinCardBody`（Pragati），
   所以等效「钉 Pragati」；**将来若有宿主用 CJK 面画卡面正文，图标会一起钉成 CJK**（仍然「一致」，只是更大）。
   本工程里没有这种宿主（`CardData.keywords` 的渲染只有 `CardView` 一处，`CardDisplayWindow.ShownBody` 只存不渲染）。
6. 风格问题（无害）：`Verify` 的 ⑨ 用 `enableWordWrapping = false`、⑪ 用 `textWrappingMode = NoWrap`，两种写法并存。

---

## 五 一句话复述

**改了什么**：`Core/CardIcons.cs` 新增 `PinSpriteFace`（把每个 `<sprite …>` 包进零宽的 `<font=Default>…</font>`）；
`Core/CardView.cs` 的 `Fill` 那**一行** `t.text = …` 改成走它；`Editor/IconSetup.cs` 的 `Verify` 末尾新增 **⑬** 探针
（4 条读数 + 5 条 Check + 前提那条）。⑨/⑪/⑫、`CalibScale`、`KeywordFontSize`、`TmpFont.cs`、`RuleEngine/**`、`Battle/**`、`Shell/**`、
`WarpforgeVFX/**` **一个字没动**。

**验了什么**：`TMPDIR=/tmp/wf_card2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（每次改完 `.cs` 都跑了，
最后一版也是 0/0）；`PinSpriteFace` 的插入/幂等用 python 镜像单测过 6 个用例（含「已钉/未钉混排」那个以前会**吞前缀**的坑，
已在本轮自查中修掉）；TMP 侧三条源码判据逐处现读（`TMP_Text.cs:7385` 特判分支 · `TextMeshPro.cs:2554` 渲染那支 ·
`TextMeshPro.cs:1457` index 0 登记）。行尾：`CardView.cs` CRLF 未翻（diff `1/1`）、另两份 LF 未翻。

**主对话要跑什么**：`-executeMethod IconSetup.Verify`（**不用** `Run`，资产不用重建）——
看 `⑬` 那一组 5 条 `OK`；然后按铁律 10 第 6 条**拿一张中文卡真机并排**比一次（这一条只能人眼验「像不像」，
而且本轮的可见变化就是「以前跟在汉字后的那批图标**缩小 1/1.406**」）。
