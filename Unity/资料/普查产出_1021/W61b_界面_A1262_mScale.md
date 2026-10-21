# 执行代理 L · 界面线 `A1262`（CJK 行盒：落地原版 `m_Scale = 0.9`）

> 2026-10-21（会话跨零点，报告归 `_1021` 这一批）· 工作目录 `d:/4`
> ⛔ 未跑 Unity / 未跑任何 `*.Run` / 未动 git / 未改正本 / 未越白名单。
> 判据 = ①原版资产字段（本次**亲自复读**）②工程自带 TMP 包源码 ③我们源码现读。
> 上一轮只读代理的报告：`资料/普查产出_1021/W61b_界面_T07尾巴.md`（§四 是补丁原文）。

---

## 一、结论

1. ✅ **已落地（本件白名单内、两处，缺一不可）**
   - `Resources/Fonts/NotoSerifCJK-Regular SDF.asset:134`：`m_Scale: 1` → **`0.9`**（单行改动，`git diff --numstat` = `1 1`）。
   - `Editor/TmpSetup.cs`：烘焙参数表补 `m_FaceInfo.m_Scale = 0.9`；`BuildCjkFontAsset` 建完**补一刀**
     `var fi = fa.faceInfo; fi.scale = 0.9f; fa.faceInfo = fi;`（否则下次重烘又回到 1）。
   - 原版值本次**亲自复读**：`bundle_fonts_assets_all/MonoBehaviour/NotoSerifCJK-Regular SDF.json` 的
     `m_FaceInfo.m_Scale = 0.8999999761581421` · `m_PointSize 50` · `m_LineHeight 71.85` · `m_AscentLine 57.55` ·
     `m_DescentLine −14.3`（`m_GlyphTable`/`m_CharacterTable` 都是 0 条 = Dynamic，与我们同构）。

2. 🔴🔴 **必须上报的一条更正（铁律 5）：上一轮的「修法外推」是错的 —— 改 `m_Scale` 之后
   `Timer` / `Victories title` **不会**回到 38 / 53.5，而且**整幅画面逐位不变**。**
   上一轮写的是「同一管线里 `m_Scale` 是纯乘子 ⇒ 1.517 × 0.9 = 1.365 ⇒ 装得下 ⇒ 收敛回 38 / 53.5」。
   它漏了一步：**我们的 `fontSize` 不是不变量**。本工程的 px 口径 = **汉字墨高**
   （`Label.FontSizeToPx` = `fontSize × TmpFont.WorldGlyphPerFontSize × 108`），而
   `WorldGlyphPerFontSize` 是**运行时实测**的 `MeasureGlyph('国')`（`Core/TmpFont.cs:590-605`，∝ `m_Scale`）。
   于是 `SetAutoFitBox` 里

   ```
   fontSizeMax = maxPx ÷ (Wglyph × 108)                      ← 反比于 m_Scale
   一行字的世界高 = fontSize × face 行盒(1.437) × m_Scale × 0.1  ← 正比于 m_Scale
   ⇒ 乘积与 m_Scale 无关 ⇒ 实渲逐位不变
   ```

   实测坐实：我们那个「渲出比 1.52」**根本不是「我们的行盒」**，而是 **行盒 ÷ 汉字墨高**
   = `1.437 ÷ 0.948 = 1.5158` —— 分子分母**都含 `m_Scale`**，约掉了。这正是四条读数
   `1.5170 / 1.5252 / 1.5470 / 1.5252` 的来历（`D1_九条红诊断.md` §证据 A），
   而上一轮把它当成了「我们的行盒低于原版」的证据。

3. ⛔ **因此本轮【没有】把 `A1178` 那两条收紧成 `38 ± 0.06` / `53.5 ± 0.06`**
   （上一轮 §4·4「回归牙口」建议的那样）。按上面的算式，收紧 = **必红**，
   而且那会是用一个错模型去改期望值（铁律：⛔ 别为了让断言绿/紧去调期望）。
   取而代之：① 在 `Editor/ShellScene.cs` **新增 `A1262` 资产级断言**（5 条，读原版资产字段，可判）；
   ② 把 `A1178` 那段过期的推断**就地订正**（原文写着「原版回退字体行盒本地拿不到 = 没有判据」，已不成立）。

4. 🔴 **真正的残差是【口径】，不是缺 `m_Scale`**：我们的「px」= 汉字墨高，而原版 `m_fontSize` 对应的墨高只有
   `0.948 × 0.9 = 0.853 × px` ⇒ **同一句话我们排出来比原版大 ≈17%**，行盒也大同样的比例，所以原版框装得下、
   我们装不下。要回到 38 / 53.5 只有两条路（都属**另一笔**、都要单独一批 + 真机并排验收）：
   - (A) 改口径：`EmPerFontSize(Face.Cjk)` 从「汉字墨高」换成「原版真 em（`0.1 × faceInfo.scale`）」；
   - (B) 逐站改实参：把 510+ 站的 `minPx/maxPx` 从「原版 `m_fontSize`」折算成「原版墨高 px」（×≈0.853）。
   ⇒ 按铁律 11「全部都要做、只有先后」，**已记账**（见 §七·1），本轮只落资产字段（「先完全复刻」）。

---

## 二、三列表

### 2·1 资产字段（原版 / 旧 / 新）

| 项 | 原版（判据 = 解包 JSON，本次亲读） | 旧（我们 `…SDF.asset`） | 新（现读） |
|---|---|---|---|
| `m_FamilyName` | **Noto Serif CJK JP** | Noto Serif CJK SC | SC（**另一笔**，见 §六·1） |
| `m_PointSize` | 50 | 50 | 50 |
| **`m_Scale`** | **0.9** | **1** | **0.9** |
| `m_UnitsPerEM` | 1000 | 1000 | 1000 |
| `m_LineHeight` | 71.85 | 71.85 | 71.85 |
| `m_AscentLine` / `m_DescentLine` | 57.55 / −14.3 | 同 | 同 |
| **face 行盒**（`lineHeight ÷ pointSize`） | **1.437 em** | 1.437 em | 1.437 em |
| **有效行盒**（face × `m_Scale`） | **1.2933 em** | 1.437 em | **1.2933 em** |

### 2·2 派生量（原版 / 旧 / 新）—— 注意「新」= 与旧**逐位相同**

| 量 | 原版 | 旧（我们，`m_Scale 1`） | 新（`m_Scale 0.9`，期望） |
|---|---|---|---|
| `TmpFont.WorldGlyphPerFontSize`（实测 `国` 墨高 ÷ fontSize） | —（JP 面未测） | **0.0948** | **≈0.08532**（= ×0.9） |
| 渲出比 = 行盒 ÷ 墨高 | 1.2933 ÷ 0.8532 = **1.5156**（= 原版 `m_fontSize` 口径下的「1 行 ÷ 墨高」） | 1.437 ÷ 0.948 = **1.5158** | **1.5158（不变）** |
| `Timer` 收敛（`FontPxNow`，**我们口径 = 汉字墨高 px**） | 原版 `m_fontSize 38` ⇒ 折成我们口径 = 38 × 0.8532 = **32.42** | **36.851** | **36.851（±0.05）** |
| `Victories title` | 53.5 ⇒ 折 = **45.65** | **48.111** | **48.111（±0.05）** |
| 内部 TMP `fontSize`（同一串） | 38 / 53.5 | 3.712 / 5.226（世界档） | **× 1/0.9 = 4.124 / 5.807** |

> 「±0.05」= TMP 竖界二分的 1/20 fontSize 粒度（`TextMeshProUGUI.cs:3428-3430`
> `sizeDelta = max((fontSize−min)/2, 0.05); fontSize = (int)(fontSize*20+0.5)/20`）在 px 上的折算
> —— 旧 0.51px、新 0.46px，故收敛值可能有 ≤0.05px 抖动。

---

## 三、受影响断言清单（`文件:行号` · 旧期望 · 新期望 · 为什么这么算）

先给**扫描口径**（全仓 `--include=*.cs`，我们这一侧的计数）：
`FontSizeToPx*` 182 处 · `FontPxNow` 109 处 · `RenderedPx` 87 处 · `WorldW/H` 644 处 ·
`CheckNear(...FontSizeMax/Min/Base/…)` 76 处 · 其中**真按 TMP 原始 `fontSize` 字面量断的只有 3 处**。

| # | 位置 | 旧期望 | 新期望 | 为什么这么算 |
|---|---|---|---|---|
| 1 | `Editor/ShellScene.cs:8168-8174`（`A1178` 上下限，`Timer`；说明在 `:8149-8167`） | `[36.07, 38]px`（下限 = 原版框高 55.905 ÷ 1.55） | **不变** | 下限那个 1.55 是**渲出比**（行盒÷墨高）的上界 = 1.547 向上取整；该比值与 `m_Scale` 无关 ⇒ 上下限一字不用改。实得仍 36.851 |
| 2 | 同上（`Victories title`） | `[47.35, 53.5]px` | **不变** | 同上（框高 73.385 ÷ 1.55） |
| 3 | `Editor/ShellScene.cs:7994-8032`（**本件新增** `A1262`） | —（原本没有） | 新增 **5 条**：前提 1 + `m_PointSize 50` / `m_Scale 0.9` / face 行盒 1.437 / **有效行盒 1.2933** | 期望值**全部取自原版资产**（pointSize 50 · lineHeight 71.85 · **scale 0.9**），判据 = `TMP_Text.cs:4188/4190`（`adjustedScale` 乘 `fontFace.scale`）+ `:4391/:4396`（ascender/descender 用它折算） |
| 4 | `Editor/ShellScene.cs:7860-7868`（`fitCase` 四格，8 站×4 条） | 原版 `m_fontSizeMin/Max/Base`（如 `10 / 38 / 38`、`2 / 32 / 36`） | **不变** | 走 `lb.FontSizeToPxNow(FontSizeMin, ActualFace)`：`fontSizeMin = minPx ÷ (Wglyph×108)`、`FontSizeToPx` 再乘回 `Wglyph×108` ⇒ **往返恒等**，与 `m_Scale` 无关 |
| 5 | `Editor/MainMenuScene.cs:14928-14930`（R01 评级三件套） | `FontSizeMax 36` · `FontSizeMin 14.4` · `FontSizeBase 25.104`（**TMP 原始 fontSize 字面量**） | **不变** | 该颗 `face: TmpFont.Face.LatinCardBody`（`Shell/AlliancePanelWindow.cs:755` 现读）+ uGUI 档 ⇒ `fontSizeMax = maxPx ÷ (0.1 × 1 × 10) = maxPx`（恒等）；**拉丁资产 `m_Scale` 仍是 1** ⇒ 不受本件影响 |
| 6 | `Editor/BattleScene.cs:29114-29124`（`A1192` 七颗） | `14.4 / 28(32) / 28.8`，走 `FontSizeToPxNow(…, ActualFace)` | **不变** | 同 #4（往返恒等）+ 该族是 `LatinUi` 面 |
| 7 | `Editor/BattleScene.cs:16275-16282`（`A1213②` 飘字四格） | `PopNumber` 的 `5.5 / 0.35 / …` 常量 | **不变** | 断的是 `PopNumber` 的**常量**（`Battle/PopNumber.cs`），不是渲染值；且两条飘字都是拉丁面 |
| 8 | `Battle/Label.cs:2165`（VAlign 三比值，`OurInkCenterWorld`） | 由 `faceInfo` 现算 | **不变** | `(capLine/2 − (ascent+descent)/2) / (ascent − descent)` ⇒ `scale` 在分子分母里约掉（上一轮已判，本件复核成立） |
| 9 | `WorldW/WorldH/RenderedPx` 那一族（644 处，含 `Editor/ShellScene.cs` 的 `ShellLabelRenderedPx`） | 原版框/px | **不变** | 渲染尺寸与 `m_Scale` 无关（§一·2 的代数），二分量法也只差 1/20 粒度 |
| 10 | `Core/TmpFont.cs` 的 `WorldGlyphPerFontSize` / `WorldCapPerFontSize` 的**读数**（`ChatBoxProbe` 等的日志、`TmpSetup.Verify` ⑨ 的拉丁读数） | 0.0948 / 0.0779 | **≈0.0853 / ≈0.0701** | 这两个是**实测值**（∝ `m_Scale`）；它们**不是断言期望**，只有日志/推导会变。⛔ 但**所有把它们写成常量注释的地方都过期了**（本件只能改 `Core/TmpFont.cs`，其余见 §七·2） |

> ✅ **结论**：**没有任何一条断言的期望值需要改数**（唯一变的是「新增 5 条」+ 大段注释订正）。
> 这正是上面第 2 条「改 `m_Scale` 渲染逐位不变」的直接推论 —— 若将来做 §一·4 的 A/B 方案，
> 那才是需要**成批重算**断言期望的那一批。

---

## 四、同步点要跑的宿主（与期望的通过数变化）

| 宿主 | 为什么 | 期望通过数变化 |
|---|---|---|
| **`ShellScene.Run`** | 本件新增 `A1262` 5 条 + 订正 `A1178` 注释 | **+5**（其余逐条不变；`A1178` 那两条仍绿、实得应与上一轮同为 36.851 / 48.111，容差内） |
| `MainMenuScene.Run` | 共用件 `Core/TmpFont.cs`（只改注释）+ 全局字体资产 | **+0**（含 R01 那三条 raw fontSize 断言，应当仍绿） |
| `RewardsScene.Run` / `ShopScene.Run` / `CollectionScene.Run` / `DeckScene.Run` / `SettingsScene.Run` / `BattleScene.Run` | 同上（全仓 CJK 站点都吃这份字体资产） | **+0**（逐条期望值不变） |
| `RuleEngineTest.Run` / `NetSelfTest.Run` / `NetBattleTest.Run` | 不碰渲染层（引擎/传输） | **+0**，可**不跑** |

- 判据（铁律 12 三条）：**动了共用件**（`Core/TmpFont.cs` 的文档 + 全局字体资产）⇒ 受影响的是**全部 8 个 UI 宿主**
  ⇒ 同步点用 `bash d:/4/Unity/工具/_run_8_checks.sh`（12 条、**必须串行**）最省事；
  只挑第一条也行（`ShellScene.Run` 是唯一有断言变化的宿主）。
- ⚠️ **判绿红看「断言合计」**，不看退出码；`BattleScene.Run` 的 **139 是已知间歇**；
  ⛔ 别用 `grep -c ✗` 数失败（`✗` 会出现在断言文案里）。
- ⚠️ 跑 `*.Run` 前照铁律留场景副本，跑完 `git diff --numstat` 对一眼（那些宿主会 `SaveScene`）。

---

## 五、没查清（逐条写「差什么」）

1. **原版 JP 面（`Noto Serif CJK JP`）的「汉字墨高 ÷ em」** 没量到。本报告里凡用 0.948（SC 面的实测值）
   去折原版 `m_fontSize` 的地方都标了「≈」。差什么：拿原版那份 TTF 建一份临时 Dynamic 资产、跑一次
   `MeasureGlyph('国')`（**要跑 Unity**，本件禁止）。影响的只有 §一·4 那两条**修法**的系数，不影响本轮结论。
2. **`m_Scale` 那个 0.9 的来历**（TMP 的 FontAssetCreator 正常写 1）仍未查——上一轮已记为「可能是 legacy 迁移
   留下的（`TMP_FontAsset.cs:3650` `m_FaceInfo.scale = m_fontInfo.Scale;`）」。⛔ 但「它会被乘进去」不依赖这个
   （包源码 `:4188/4190` 现读）。
3. **渲染「逐位不变」我只做了代数 + 包源码推导，没有实跑**（铁律 12）。差什么：同步点那次
   `ShellScene.Run` 的 `A1178` 两条读数 + `fitCase` 那 32 条（8 站×4）—— 这就是本报告最强的验证手段，
   若它们由绿变红，说明我的推导漏了一项，**请以实跑为准并回滚本件的断言段**（资产那一行可保留）。
4. ⚠️ 顺带一处**与本件无关**但会干扰同步点判读：`typecheck` 第一次跑时若报
   `RuleEngine/**` 的错，那是**别的代理正在写**的文件（会话开始时就是 `M`）—— 不是本件引入。

---

## 六、改动清单（逐文件 + 行尾）

| 文件 | 改动 | 行尾 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset` | `:134` `m_Scale: 1` → **`0.9`**（YAML，**只此一处**；`:74` 的 `m_Scale: {x: 1, y: 1}` 是纹理设置，没碰） | **LF**（`crlf 0 / lf 224218`，改后复核） · `git diff --numstat` = **1 1** |
| `Unity/MyGame/Assets/CardPresentation/Editor/TmpSetup.cs` | `:49-52` 烘焙参数表补 `m_FaceInfo.m_Scale = 0.9` + 有效行盒算式；`:171-179` `CreateFontAsset` 之后补 `fi.scale = 0.9f; fa.faceInfo = fi;`（含「整份写回」的理由） | **LF** · `13 0` |
| `Unity/MyGame/Assets/CardPresentation/Core/TmpFont.cs` | 过期文档订正 5 处：`:66-75`（0.0948 逐位不变 → 口径不变/数字随资产）· `:152-156`（1.437 = face，有效 1.2933）· `:254` + `:257-262`（「本机 4 份 m_Scale 全是 1」→ 3 份拉丁是 1、CJK 是 0.9）· `:280-284` + `:286`（`TrueEmPerFontSize` 同理）· `:320` + `:324-328`（「行高 1.437 em」→ face 行盒 + 有效行盒）· `:586-588`（`MeasureGlyph` 的两个读数同步缩） | **LF** · `38 9` |
| `Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs` | `:7994-8032` **新增** `③′ A1262` 一节省（5 条断言，期望全部取原版资产）；`:8149-8167` 把 `A1178` 那段「原版回退字体行盒本地拿不到 / 残差 1.52 vs ≤1.47 / 外推 1.517×0.9」**就地订正**成 §一·2/§一·4 的结论 | **LF** · `58 2` |
| `Unity/MyGame/Assets/CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs` | `:918-958`（`PreferredWidthPx` 头那段）把「修法 = 改 m_Scale ⇒ 收敛回 38 / 53.5 / 实渲缩 ≈10%」改成「已落资产字段 + 渲染逐位不变 + 真残差是口径（另一笔）」 | **LF** · `41 1` |
| `Unity/MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs` | `:3074-3079` 订正 `A1262` 那段：`1.516` 是**渲出比（行盒÷墨高）**、对齐后**不变**，别改成 1.293；资产已改为 0.9 | **LF** · `6 0` |
| 本报告 | `资料/普查产出_1021/W61b_界面_A1262_mScale.md` | 新建 · LF |

- 全部改动 = **1 行资产 + 代码 2 处（TmpSetup）+ 1 段断言（ShellScene）+ 纯注释若干**。
- `Core/TmpFont.cs` 与 `Shell/**` 里**没有一行逻辑改动**（`git diff` 里 `-` 的全是注释）。
- ⛔ **没碰**（黑名单）：`RuleEngine/**` · `Editor/BattleScene.cs` · `Battle/**` · `Core/CardView.cs` ·
  `WarpforgeArena1/**` · `工具/**` · `CLAUDE.md` · `资料/项目任务.md` · `资料/任务线/**` · `d:/2/**` · git。

---

## 七、顺手发现（只报不改 —— 本件白名单外）

1. 🔴 **要立账（铁律 5·b + 11）：CJK px 口径 = 汉字墨高，与原版 `m_fontSize` 差 ≈17%**（§一·4）。
   它不是「字体资产缺一格」，而是**全仓 510+ 站点的系统性偏大**，且**改它是另一笔**（改口径或逐站改实参，
   都要真机并排验收）。建议主对话记进 `项目任务.md` §三（`A1262` 那一行下面单开一笔，或直接扩写它）。
2. 🔴 **「0.0948 / 0.0779 逐位不变」这个说法在 Battle / Deck / MainMenu / Rewards / Shop / Settings 六个宿主里
   还有约 20 处注释**（`Battle/Label.cs:105/462/950/1217/1318/1378`、`Editor/BattleScene.cs:716/5436/5455` 等）。
   它们现在**数字过期**（应变 ≈0.0853 / ≈0.0701）、**口径仍然对**。本件只能改 `Core/TmpFont.cs`，
   `Battle/**` 与各 `Editor/<宿>.cs` 不在白名单 ⇒ **留给白名单内的下一批**（纯注释，机械可改）。
3. 🟡 **原版 JP 面 vs 我们的 SC 面**（上一轮 §六·1 已记，本件复核成立）：原版
   `bundle_fonts_assets_all/Font/NotoSerifCJK-Regular.ttf` = **JP**；我们 `CardPresentation/Fonts/…ttf` = **SC**；
   且 `Editor/TmpSetup.cs:40` 的 `SrcTtf` 还指着**旧解包目录** `d:/2/解包整理/10_字体/fonts/`（SC）。
   ⇒ 「完全对齐原版」还有**换面**这一笔（会动汉字字形：直/骨/次 等），要重烘 + 单独验收。
4. 🟡 `Shell/SkirmishEventWindow.cs:167` 的注释「38px × 1.437 行高 ≈ 54.6px」用的也是 **face 行盒**；
   对齐后同一 fontSize 的实排是 ×1.2933。该注释是**推算**不是常量 ⇒ 数字已经不准（结论未变）。
5. 🟡 `Core/TmpFont.cs` 顶部那段「一个 Noto 打天下」等结论仍然成立，但 2026-10-18 的 `N55` 注里
   「CJK 0.0948 逐位不变」是**同一族的过期措辞**（本件已在 `:66-75` 就地订正并留痕）。
6. ⚠️ `TmpSetup.Verify` 的第 ⑨ 格只打**拉丁三份**的比值 —— 原版 CJK 的 `m_Scale` 这一格它**从来没查过**
   （所以漏了这么久）。本件把这条判据搬进了 `ShellScene.Run` 的 `A1262`；
   若想让 `TmpSetup.Verify` 自己也能拦住，可在 ⑨ 后面加一格读 `faceInfo.scale`（**本件白名单里有 TmpSetup**，
   但它属于「验证」而不是复刻 ⇒ 按铁律 12 的口径没顺手加，留作建议）。

---

## 八、typecheck

```
TMPDIR=/tmp/wf_l12 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 落地前后各跑过一次，都是 **0/0**（`Core/TmpFont.cs` 在运行时程序集、`TmpSetup.cs` / `ShellScene.cs` 在编辑器程序集）。
- ⛔ 按铁律 12：**没有跑任何 `*.Run`**；也没有进游戏、没有截图。
