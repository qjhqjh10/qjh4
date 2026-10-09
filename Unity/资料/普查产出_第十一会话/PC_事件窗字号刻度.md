# P-C · `A1197` —— 两扇事件窗**缩放子树里的字号**刻度

> 执行代理 **P-C** · 2026-10-10。只改白名单那两件；⛔ 没跑 Unity、⛔ 没动 git、⛔ 没碰正本 / 任何别的 `.cs`。
> 全部结论都是**现读**（命令 / `文件:行号` 逐条给在下面）。临时脚本与两份 dump 落在 `d:/4/_tmp_view/pcscratch/`。

---

## 一、一句话结论

1. **`EnergySinglePlayerOnlyEventWindow` = 真偏离，已修**：`Score`×5 与 `Collect/Button Text`
   四格字号**一起乘**各自窗的祖先刻度积（**1.1478264** / **1.32**），**框一个都没动**。
2. 🔴 **`SkirmishEventWindow` 这条链里的两颗 TMP，我们【一件都没建】**
   ⇒ 本笔在该窗的**代码改动 = 0 处**（不是我漏了，现读实据见 §二·2）。
   `SkirmishModeEventWindow` prefab 里确实有这两颗（`Score` 祖先积 **1.0924350** · `Collect` 祖先积 **1.2563**），
   但我们的 `SkirmishEventWindow.BuildScoreBar/BuildMilestones` **有意只画图、不画这两处字**
   （文件自己写着「分数文字与已达成那一档的高亮不画」「那颗 `Collect` 钮没建」）⇒ 没有字号可乘。
   **已在文件里留判据注释**：将来补这两颗时，四格字号按 **1.0924350 / 1.2563** 乘（⛔ 别一刀切）。
3. 🔴 **附带一笔必须跟着走的账（不在我白名单里，交调度台）**：`Editor/ShellScene.cs` 有 **6 处 `fitCase`**
   把这两颗的 `min/max/base` **钉在原值**（`18/48/36` · `10/55/12`）⇒ 不同时改，
   下一次 Unity 自检会**多出 18 条红**，而那是**修好之后的正确值**，不是新缺陷。**数在 §4·b。**

---

## 二、状态 → 参数对照表（逐窗 × 逐颗）

### 2·1 现读方法（可复跑）

```bash
# 树 + 每行的 `⇲ls=`（= 祖先刻度积，工具已把矩形换算成屏值）
python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "EnergySinglePlayerOnlyEventWindow" \
       --depth 14 --relative --no-sprite        # 输出落 d:/4/_tmp_view/pcscratch/energy.txt（124 行）
python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow" \
       --depth 14 --relative --no-sprite        # 输出落 d:/4/_tmp_view/pcscratch/skirmish.txt（177 行）
# 逐层读 `m_LocalScale`（工具只印 3~4 位小数，这里要的是精确值）
python -I d:/4/_tmp_view/r4scratch/chain2.py "Score"       | grep -E "Energy|Skirmish"
python -I d:/4/_tmp_view/r4scratch/chain2.py "Button Text" | grep -E "Energy|Skirmish"
```

### 2·2 逐颗（**只覆盖「已建出来的件」这一种情况**；未建的两颗另列在末两行）

| 窗 | 节点（原版全路径） | 祖先链 `m_LocalScale`（父 ← 子，逐层现读） | **祖先刻度积** | 原版序列化字号（本地值） | 原版**屏上**字号 = 本地 × 积 | 我们改前 | 我们改后 | 出处 |
|---|---|---|---|---|---|---|---|---|
| Energy | `…/Score Bar Line Level 1..5/Skull/Score` | `Scoring Bar Event Score Info` **1.3200000524520874** ← `Score Levels` 1.0 ← `Score Bar Line Level k` 1.0 ← `Skull` **0.8695654273033142** ← `Score` 1.0 | **1.1478264**（= 1.32 × 0.86956543） | 48 / 18~48 / base 36 | **55.0957** / **20.6609**~**55.0957** / base **41.3218** | 48 / 18~48 / base 36（**相对框小 −12.9%**） | 55.0957 / 20.6609~55.0957 / base 41.3218 | `energy.txt:30,35,40,45,50`（5 颗逐值一致）+ `chain2.py` |
| Energy | `…/Scoring Bar Event Score Info/Generic Simplified UI Button/Button Text`（`'Collect'`） | `Scoring Bar Event Score Info` **1.3200000524520874** ← `Generic Simplified UI Button` 1.0 ← `Button Text` 1.0 | **1.32** | 55 / 10~55 / base 12 | **72.6000** / **13.2000**~**72.6000** / base **15.8400** | 55 / 10~55 / base 12（**相对框小 −24.2%**） | 72.6000 / 13.2~72.6 / base 15.84 | `energy.txt:51,52` + `chain2.py` |
| **Skirmish** | `…/Score Bar Line Level 1..5/Skull/Score` | `Scoring Bar Event Score Info` **1.2562999725341797** ← … ← `Skull` **0.8695654273033142** ← `Score` | **1.0924350** | 48 / 18~48 / base 36 | 52.4369 / 19.6638~52.4369 / base 39.3277 | **本窗没建这一颗** ⇒ 无字号可改 | 同左（**0 处改动**） | `skirmish.txt:51,56,61,66,71` + `chain2.py` |
| **Skirmish** | `…/Scoring Bar Event Score Info/Generic Simplified UI Button/Button Text`（`'Collect'`） | `Scoring Bar Event Score Info` **1.2562999725341797** ← `Generic Simplified UI Button` 1.0 ← `Button Text` 1.0 | **1.2563** | 55 / 10~55 / base 12 | 69.0965 / 12.5630~69.0965 / base 15.0756 | **本窗没建这一颗** ⇒ 无字号可改 | 同左（**0 处改动**） | `skirmish.txt:72,73` + `chain2.py` |

**框一个都没动**（对照表里不列「改后框」就是因为**它没变**）：
- Energy `Score` 的框 = 代码里的 `ScoreW`/`ScoreH` = **133.32 × 57.39** —— **已经是屏值**：
  = 原版本地 `m_SizeDelta` **116.149 × 50.00** × 1.1478264（现读屏值 `energy.txt:30` 的 `133.32 × 57.39` 逐位对上）。
- Energy `Collect` 的框 = 代码里的 `CollectTxR` = **280.88 × 73.17** —— 同上已是屏值（现读 `energy.txt:52` 逐位对上）。
- 🔴 判据：**框与字号是同一件事的两半** —— 上面那些 `Vis*` / `ScoreW` / `CollectTxR` 常量烘的是框，这一笔补的是字号。
  ⛔ 别把框再乘一遍（那会让它比原版大 1.1478264 / 1.32 倍）。

**「原版屏上字号」那两列怎么来的（判据，不是推论）**：
- `com.unity.ugui@27635d171b1a/Runtime/TMP/TextMeshProUGUI.cs:2333-2345` `ComputeMarginSize()`：
  `Rect rect = m_rectTransform.rect;` —— `RectTransform.rect` 是**局部**尺寸，不含任何祖先缩放；
  `m_marginWidth = rect.width - m_margin.x - m_margin.z`。
- 二分两端都拿 `m_fontSize`（本地字号）与那个**局部** `marginWidth` 比：缩 `:3424-3428` · 放 `:4488-4502`；
  起点 `:2500` `Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)`。
- ⇒ 祖先缩放在「局部 → 屏」那一步把 rect 与字形**一起**乘，**改不了 fit 的结果**；而 prefab 里序列化的是
  **未缩放的** `m_fontSize` ⇒ **原版屏上字号 = 本地字号 × 祖先刻度积**。
- ⚠️ `d:/2/tools/decomp_full/` 里**没有 TMP**（只覆盖 `Assembly-CSharp`；`grep -rl "enableAutoSizing"` 零命中）
  ⇒ 判据取的是**工程自带的包源码**（上面那条路径）。这一条与 R4 报告一致，我复核过。

**量纲自证（改完的比值 ≈ 原版本地比值）**：
`改后字号 ÷ 我们的框 = 55.0957 ÷ 133.32 = 0.4132588` vs `原版本地 48 ÷ 116.149 = 0.4132623` ——
五位吻合（差在 133.32 只印到两位）。改前是 `48 ÷ 133.32 = 0.36004`，差 12.9%。

---

## 三、逐处改动清单

**白名单两件 · 共 3 处代码改动（全在 Energy 窗）＋ 1 处判据注释（Skirmish 窗）**

| # | 文件:行号（改后） | 改前 | 改后 | 判据 |
|---|---|---|---|---|
| 1 | `Shell/EnergySinglePlayerOnlyEventWindow.cs:114-149` | 无（新增常量段） | `SBarFontScale = 1.32f` · `SkullFontScale = 0.8695654273033142f` · `ScoreFontScale = SBarFontScale × SkullFontScale`（= 1.1478264） | `energy.txt` 行末 `⇲ls=` + `chain2.py` 逐层 `m_LocalScale`；「为什么字号也要乘」的 TMP 判据（包源码行号）写在这一段 |
| 2 | `Shell/EnergySinglePlayerOnlyEventWindow.cs:423-426`（`BuildScoreBar`） | `MenuDraw.Text(_collect, CollectTxR, "Collect", …, "Button Text", **55f**, QText, CollectTxR.W, **10f, 55f, 12f**)` | 同样那一行，四格字号改为 `55f*SBarFontScale` · `10f*SBarFontScale` · `55f*SBarFontScale` · `12f*SBarFontScale`（= **72.6 / 13.2 / 72.6 / 15.84**） | `energy.txt:52` 那一行的 `⇲ls=1.32` + `chain2.py` 链（`… ← Generic Simplified UI Button ← Scoring Bar Event Score Info`） |
| 3 | `Shell/EnergySinglePlayerOnlyEventWindow.cs:525-532`（`BuildMilestones`，一行建 5 颗） | `MenuDraw.Text(row, sr, "1256", …, "Score", **48f**, QText, sr.W, **18f, 48f, 36f**)` | 四格字号改为 `48f*ScoreFontScale` · `18f*ScoreFontScale` · `48f*ScoreFontScale` · `36f*ScoreFontScale`（= **55.0957 / 20.6609 / 55.0957 / 41.3218**，5 颗同值） | `energy.txt:30,35,40,45,50` 的 `⇲ls=1.148` + `chain2.py` 链（⭐ 那个 `0.8696` 长在 **`Skull`** 上、`Score` 是它的**子件**） |
| 4 | `Shell/SkirmishEventWindow.cs:163-176`（`BuildScoreBar` 的 XML 注释） | 无 | **只加注释**（零代码改动）：登记那两颗的祖先链与积（**1.0924350** / **1.2563**）+ 「本窗一件都没建」+ 「将来补时四格字号一起乘」 | `skirmish.txt:51,56,…,71,72,73` + `chain2.py` |

**每处注释都写了**：理由 · 判据出处（`文件:行号` / 资产路径）· 「**逐窗取值、⛔ 别一刀切**」这件事
（同一个子 prefab 在 Skirmish / Two-Sides 窗里是 **1.2563** 不是 1.32）。
形状照先例 `Shell/MissionsTab.cs` 的 `FS()`（我**只读**它，⛔ 没改）＋ `Shell/DailyStreakPopup.cs` 的 `BuildEntry`。

**为什么四格一起乘（不只乘 `fontPx`）**：`MenuDraw.TextCore` 把 px 折成 TMP 的 fontSize 单位是走
`fontSizeMax = cur × (maxPx / nomPx)` 这条**比值**（`Battle/Label.cs:902-908`），而 `nomPx` 本身也是从
`fontPx` 折出来的 ⇒ 只乘 `fontPx` 会让 `min/max/base` 与标称字号的**比例走样**；
四格一起乘，TMP 那边正好等比放大同一倍 ⇒ 与「原版整棵子树被 scale 包着」逐位等价。
（另：`TextCore` 的自适应开闸条件是 `autoMinPx > 0f && fontPx > autoMinPx` —— 四格同乘不改变这个判断。）

---

## 四、验证

### 4·a 我这边的读数

```text
TMPDIR=/tmp/wf_pc bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

（两次跑都 0 —— 第 2 次是补完 Skirmish 那条注释之后复跑。⚠️ **秒级类型检查不算自检**，没跑任何 Unity 批处理。）

```text
git diff --numstat -- Shell/EnergySinglePlayerOnlyEventWindow.cs Shell/SkirmishEventWindow.cs
55      4       Unity/MyGame/Assets/CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs
16      1       Unity/MyGame/Assets/CardPresentation/Shell/SkirmishEventWindow.cs
```

行尾复核（**判据是数出来**，二进制读）：两件 **CRLF = 0 · LF = 739 / 317** ⇒ **纯 LF，没被翻**（与简报说的一致）。
改 `.cs` 全程用 Edit 工具，⛔ 没用 `sed -i`。

### 4·b 🔴 必须跟着改的断言（**不在我白名单里**，请调度台派活）

`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs` 的 `fitCase`（`:6509-6546`）**把原版字体值当屏值钉住了**——
那正是 `A1197` 这个 bug 的期望值。**6 处调用里各 3 条（min/max/base）会红 = 共 18 条**，
而 `AutoSizing` / 折行档 / 「渲出来 ≤ 框」那三条**照旧绿**（后者的容差是 `box + 1.5f`，字号变大反而更早收敛）。

| 位置（现读行号） | 现在写的期望 | 应改成 | 依据 |
|---|---|---|---|
| `ShellScene.cs:6671`（`Score`，`for k = 1..5` ⇒ **5 处**） | `133.32f, 57.39f, 18f, 48f, 36f, 1` | `133.32f, 57.39f, **20.6609f, 55.0957f, 41.3218f**, 1` | `18/48/36 × 1.1478264` |
| `ShellScene.cs:6681`（`Collect/Button Text`） | `280.88f, 73.17f, 10f, 55f, 12f, 0` | `280.88f, 73.17f, **13.2f, 72.6f, 15.84f**, 0` | `10/55/12 × 1.32` |

⚠️ **框宽/框高那两格不用动**（`133.32×57.39` 与 `280.88×73.17` 是屏值，本来就对）。
⚠️ 这两处 `fitCase` 的 `evidence` 文案里「实读 `auto[18~48]`」也得跟着写清「**那是本地值，屏上是 ×1.1478264**」，
否则下个会话会以为断言写错了。`CheckNear` 容差 0.05f ⇒ 上面四位小数够用。
⚠️ 我**没去改** `ShellScene.cs`（白名单只两件、且铁律 13·3「一个文件同一时刻只有一个写手」）—— 如实交回。

---

## 五、没查清 / 停手的部分（如实）

1. 🔴 **我们的字体换过 ⇒ 原版那个「屏上字号」在我们这边不保证逐像素同宽**。
   原版屏上字号这一侧是**硬结论**（TMP 在本地单位里二分，见 §二），但**渲染宽度**取决于字体资产：
   原版 = `Pragati-Regular SDF`（`bundle_fonts_assets_all`，`m_PointSize 95`），我们工程里装的是
   `NotoSerifCJK-Regular SDF`（`Resources/Fonts/`）。⇒ 改后「相对框的比例」与原版**逐位一致**，
   但「这一串字画出来多宽」这一层**仍可能与原版不同**（这一条 `R4` §六·1 也标着「没查清」，我**没去追** ——
   不属本笔，且要读 `Battle/TmpFont.cs` 的字体解析 + fallback 表）。
   ⚠️ 连带的一个**具体风险**（未验，因为不许跑 Unity）：`'1256'` @55.0957px 会不会撑破 133.32 宽的框？
   若撑破，TMP 的自适应会把**收敛结果**压小（**但 `m_fontSizeMin/Max/Base` 那三个字段不变** ⇒ §4·b 那 18 条红照旧是那个值）。
   **没查清**：`NotoSerifCJK` 的 digit advance 我**没量**（要跑 TMP 才量得出）。
2. **`Skirmish` / `Two Sides` 两窗那颗 `Score` 与 `Collect` 我们都没建** —— 这是**复刻缺漏**（铁律 11 该做的），
   但**不属 `A1197`**（那两笔在 `BuildMilestones` 里已经挂了两条 `Debug.Log` 出声）。我只在现场留了判据注释，
   ⛔ **没有顺手把它建出来**（那是另一笔账，且要动 `BuildMilestones` 的建法）。→ 见 §六·1。
3. `Skull` 那个 `0.8695654273033142`（≈ 1/1.15）**怎么来的没查清**（原版作者意图无据；`R4` §六·2 同）。
   我**按现读值照乘**，没有替它找一个「整分档」。
4. **`SBarScale`（Skirmish 窗）那组常量是四舍五入过的**：`SBarScale = 1.2563f` 经 float 解析**恰好**
   等于现读的 `1.2562999725341797` ✔；但 `ProgrScale = 0.8695655f` vs 现读 `0.8695654273033142`
   差 **≈ 7.3e-8 相对**（在 500px 上 = 4e-5 px）—— **不是缺陷、不值得改**，仅登记以免下个会话重复核。

---

## 六、顺手发现（⛔ 我只报不改）

1. 🔴 **`SkirmishEventWindow` 的记分条是「半棵」**：`Scoring Bar Event Score Info` 那棵子树里，
   原版有 `Score`（5 颗，`'1256'`）与 `Collect/Button Text`（`'Collect'`）**两颗 TMP**，
   我们**只画了图（Skull/Chest/Highlight Crate）没画这两处字**（`Shell/SkirmishEventWindow.cs:204-206` 两条 `Debug.Log` 出声）。
   而**能量窗把同样两颗都建了**（同一份子 prefab）⇒ 两扇窗对「要不要建这两颗」的处置**不一致**。
   ⚠️ 判据上是**能建的**：那两颗的字（`'1256'` / `'Collect'`）就是 **prefab 出厂原文**，
   不依赖活动数据（能量窗就是这么做的，本文件也写着「分数文字…要活动数据」——
   但**能源窗同样没有活动数据、照样按出厂原文画了**）⇒ 这条「不建」的理由**站不住**。
   建议**另立一笔**（不在 `A1197` 范围内）。
2. ⚠️ `Shell/EnergySinglePlayerOnlyEventWindow.cs` 里那两处 `A1126/A1178` 残差注释（`Victories title` / `Total Victories`
   与 `Timer` 三段）都写着「我们摆的框宽**若比原版 preferred 窄**，开自适应会让字比原版小一点」——
   `R4` §1·3 现读已把那个条件判成**用 prefab 出厂文案时不成立**（我们 254.112 / 289.72 **比原版宽** +44% / +17%），
   但 `R4` 同时列了三条限定（字体不同 / 运行期文案会换）⇒ **悬念收窄了、没清零**。
   ⚠️ 那几段是 `A1126` / `A1178` 的账，**不是 `A1197`** ⇒ 我**没改**（虽然文件在我白名单里，⛔ 不越界顺手改），
   请那一笔的负责人按 `R4` §1·3 就地更新。
3. `资料/普查产出_第十会话/R4_布局刻度族查实.md` §八 那张建议表里写的 **`Skirmish 窗 Score ×1.0926`**
   现核 = **1.0924350**（工具 `⇲ls=1.092` 是截断印法）—— 差在**我给的这一笔**里已按精确值登记，
   R4 那份是四舍五入，不影响结论。
