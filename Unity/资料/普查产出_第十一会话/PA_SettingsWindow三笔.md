# P-A · `SettingsWindow.cs` 三笔（与 ARF 那一族的第 4 件）

> 执行代理 P-A · 2026-10-10（第十一会话）
> 白名单 = **只有** `Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs`。
> ⛔ 没跑 Unity（`-executeMethod` / `_run_8_checks.sh` 一次都没跑）· ⛔ 没动 git · ⛔ 没碰两张正本 · `d:/2/**` 只读。
> ✅ **跑了秒级类型检查**（唯一挡「编不过」的）：`TMPDIR=/tmp/wf_pa bash d:/4/Unity/工具/typecheck.sh`
> → **运行时错误数 0 · 编辑器错误数 0**（跑了两趟：改中一趟、收尾一趟）。
> `git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs` = **`200  46`**；
> 行尾**仍是 LF**（`b.count(b'\r\n') = 0`、`b.count(b'\n') = 5420`）⇒ 没被翻。

---

## 一、一句话结论

**四件全做完**（三笔 + 那笔「低优先」的第 4 件——它本来就是第 3 件的一个副作用）。
每一处都写了**改动理由 + 判据出处**的注释；同时按铁律 5 把**本文件里 5 处被这一笔推翻的旧说法就地改掉**。
🔴 **但有一笔必须由调度台转交**：`CardPresentation/Editor/SettingsScene.cs` 里 **6 处 `CheckAutoFit` 的框宽 / 框高常量**
仍是**改前那一档**（它们是**字面量**，不引用本文件的常量）—— ⛔ 不在我的白名单，**一个字没动**，见 §四·1。

---

## 二、逐处改动清单（行号 = 改后 `SettingsWindow.cs` 的行号）

| # | 位置 | 改前 → 改后 | 判据出处 |
|---|---|---|---|
| **1** | `:2580` `SetAct(terms, false);`（新增一行）+ 注释块 `:2553-2579` 重写 | 「停在出厂值 1 + PC 档 1（本地判不了）」→ **`SetActive(false)` + 出声**（那条 `Debug.Log` 在 `:2621-2626`） | `R4_布局刻度族查实.md` §四 · `SupportTab__OnSetup.c` 末尾 `SetActive(TOS, *(*(GameStaticData.staticFields)+0x70) != 0)` · `dump.cs:119411`（字段表 `TermsOfServiceUrl // 0x70`）· `GameStaticData__.cctor.c:147`（**显式赋 `0`**） |
| 1·b | `:1018-1029`（表下「另有两处**代码**判据」①） | 「拿到的是**运行时才算得出来的 URL**…**本地判不了**」→ 「本地判得了、**恒 `false`**」 | 同上 |
| 1·c | `:1050-1055`（「四颗外链钮的 URL」段末） | 「⛔ 一条都对不上 ⇒ 不打开外链、只出声」→ 「**理由错了**：原版那一颗恒关」 | 同上 |
| 1·d | `:1105-1107`（`SpFaqUrl` 的 doc） | 「不在这张表里：查不到 terms 字样的 URL ⇒ 不打开」→ 「不在这张表里，**但理由不是查不到**：原版恒关」 | 同上 |
| 1·e | `:1115-1123`（`SpTermsUrlNote` 的 doc + **那个字符串**） | 英文串「`no constant URL exists locally (the original passes a runtime-built string)`」→ 「`the original keeps this button disabled (TermsOfServiceUrl is explicitly nulled in the static ctor)`」 | 同上 |
| 1·f | `:2496-2500`（`BuildSupportPage` 头部「哪一半是本地模拟」） | 「那条 URL 本地查不到 ⇒ 只出声、不打开」→ 「原版恒关 / 我们照此关掉 ⇒ `SpTermsNoUrl` 跑到等于出了问题」 | 同上 |
| 1·g | `:2612-2626`（⑩ 平台档块末 + 那条 `Debug.Log`） | 「`Tab Title` 与 `bottom links` 三档都是 1 ⇒ 不碰」→ 补一句「ToS 是**另一条轴**关的、已在 ⑧·a 关掉」；**log 里加了 ToS 恒关这一条**（= 出声） | 同上 |
| 1·h | `:2696-2703`（`SpTermsNoUrl` 的 doc） | 「本地没有可用的常量 URL」→ 「原版那一颗恒关 ⇒ 这句跑到 = 有颗本该关着的钮亮着」 | 同上 |
| **2** | 新常量 `:302` `QualLabelT = 274.70f, QualLabelB = 321.10f`（doc 见 `:283-301`） | —— 新增 | `R4_布局刻度族查实.md` §3·② · 我**现读复核**：`menu_dump … "Main Menu Settings Window" --depth 12 --no-sprite --no-ancestor-scale` 印 `Quality DropDown > Label` = **`561.5…942.2 × 274.7…321.1`**（= 380.67 × 46.40 设计 px） |
| 2·b | `:3395-3396` 调用点 | `QualL+20 … QualBoxR-40`（571.52…912.18，宽 **340.66**）· 字号 **`FontLabel`(40)** · **白** → `QualL+GenCapInset … QualBoxR-GenCapInset`（561.52…942.18，宽 **380.67**）· `GenCapFontPx`(**18**) · `GenCapColor`(**灰 0.67**) · auto `18/40/14` · 折行 0 | 同上（原版那颗 = `TMP_Dropdown.m_CaptionText`：`18 / base 14 / auto[18~40] / 折行 0 / Left-Middle / (0.67,0.67,0.67,1)`） |
| 2·c | `:3397-3398` 新增 `AlignLeft(_qualityLabel, …)` | 原来**没有**左对齐 → 补上（原版 `Left/Middle`） | 同 `_langCap`（`MenuDraw.Text` 默认把文字块**居中**放在锚点上） |
| 2·d | `:1030-1036`（`FontRowLabel` 的 doc） | 「`FontLabel` 上还站着**两个判据未定**的：① `Quality Value` …」→ 「① **已查定、已不站在 `FontLabel` 上**；现在只剩 ② 联机页」 | 铁律 5（就地更正） |
| **3** | 新常量 `:141` `ArfAspectRatio = 5.140573024749756f`（doc `:131-140`） | —— 新增（本窗 `Button Text` 那一族共用的 ARF 比值） | `R3_三笔查实.md` §三 · 我**现读复核**原字段：`MonoBehaviour_-1179297001111847002.json` = `m_Enabled 1 · m_AspectMode 1 · m_AspectRatio 5.140573024749756` |
| 3·b | 新常量 `:566-572` `GenBtnTxtW/H/Apx/Apy/L/T`（doc `:543-565`） | 新增：`W = GenBtnW − 26` = 274 · `H = W ÷ ArfAspectRatio` = **53.3015** · `L = 12.6960` · `T = 17.7403`（四个数**全从原版字段推**） | 现读原版 RectTransform `RectTransform_7891468692848082854`：`m_AnchorMin (0,0) / Max (1,1)` · `m_AnchoredPosition (−0.304016, +0.608994)` · `m_SizeDelta (−26, 0)` |
| 3·c | `:2894-2898`（`GenButton` 的 `Button Text`） | 框 `x1…x2 × GenBtnT…y2` = **300 × 90** → `tx1/tx2/ty1/ty2` = **274 × 53.3015** | `R3` §三·逐颗表；**我复核**：跑 ARF 之后实测 = `609.2137…883.2137 × 797.3283…850.6297`，与上面那组算式**逐位吻合**（算出来 609.2138/883.2138/797.3283/850.6297） |
| 3·d | `:880-882`：`AcSocTxtDy`(**30.12**) 与 `AcSocTxtH[]`(**{33.55,13.55,13.55,13.55,18.55}**) → `AcSocTxtT = 696.71f, AcSocTxtB = 716.45f`（两个旧符号**删掉**，数组消掉） | 五颗从「钮顶 +30.12 / 逐颗不同高」→ 一对**绝对值**（ARF 后五颗全同） | `R3` §三；**我复核**：现读五颗 = `696.7128 … 716.4472`（高 19.7344），**五颗逐位相同**（原版 ARF 保中心，五颗 prefab 框中心本来就都是 706.585） |
| 3·e | `:2271`（那五颗 `Button Text` 的调用点） | `AcSocY1[i]+AcSocTxtDy … +AcSocTxtH[i]` → `AcSocTxtT, AcSocTxtB` | 同 3·d（`x` 那半一个字没动——ARF 是宽控高，不动 x） |
| 3·f | `:921-933`（`AcBtnText` 数组） | 7 格里**改 4 格**的 `y1/y2`（见 §三 的表）；**3 格没动**（prefab 本来就烘成 ARF 值） | `R3` §三·逐颗表；**我复核**逐颗跑 ARF 的实测值与 7 格逐一对过（差 ≤ 0.002） |
| **4** | （`A1210②`） | 原版 `GenButton > Button Text` 内缩到 `274 × 53.3`，我们原来摊满整颗钮 `300 × 90` | **本笔已随 3·c 一并做完**（`R4` §3·③ 判「画面无差」，但铁律 11 ⇒ 要做；现在做完了） |

### `AcBtnText` 那 4 格的 `y`（改前 → 改后，设计 px）

| # | 节点 | 改前 `y1..y2` | 改后 `y1..y2` | ARF 后高 |
|---|---|---|---|---|
| 1 | `Unregistered Login > Button Text` | `564.37 … 614.95`（高 50.58） | **`563.01 … 616.31`** | 53.3015 |
| 2 | `Twitch > Button Text` | `824.72 … 874.78`（高 50.06） | **`823.36 … 876.14`** | 52.7787 |
| 5 | `Logout > Button Text` | `824.46 … 875.04`（高 50.58） | **`823.10 … 876.40`** | 53.3015 |
| 6 | `Login Window > Login Button > Button Text` | `499.31 … 551.68`（高 52.37） | **`497.95 … 553.04`** | 55.0857 |

（`Register` / `Delete` / `Switch` 三格的 prefab 字段**本来就是 ARF 值** ⇒ 一格没动。四格的 `x` 也一格没动。）

---

## 三、验证

| 项 | 读数 |
|---|---|
| 秒级类型检查（改中） | 运行时错误数 **0** · 编辑器错误数 **0** |
| 秒级类型检查（收尾） | 运行时错误数 **0** · 编辑器错误数 **0** |
| `git diff --numstat -- …/SettingsWindow.cs` | **`200  46`** |
| 行尾 | **LF 未被翻**（`CRLF=0` / `LF=5420`） |
| Unity 自检 | ⛔ **一条都没跑**（红线：同一工程只能一个实例；且按铁律 12 本笔也不该跑——由调度台在同步点按覆盖面跑） |
| 判据现读（我自己跑过的只读命令） | `menu_dump.py bundle_menus_assets_all "Main Menu Settings Window" --depth 12 --no-sprite [--no-ancestor-scale]` · `menu_rect.py … "Quality DropDown" --depth 2` · `menu_dump … "Redeem Code"/"Logout Button"/"Twitch Button"/"Discord Button"/"Login Button " --no-layout --no-ancestor-scale` · 一个只读探针（`sys.path` 引 `menu_dump`/`menu_rect`，逐节点打**全精度**设计矩形）· 直接读 `RectTransform_7891468692848082854.json` 与那颗 ARF 的 `MonoBehaviour_*.json` |

🔴 **`R4` §3·② 那句「框照 `380.67×46.4`（现读 601.4→944.0 屏 ÷ 0.9）」的后半句不算数**：
屏幕帧回设计帧**不是简单除 0.9**（本窗根 `m_LocalScale = 0.9` 是**绕窗根中心**缩的，见 `Screen()`）
⇒ 那个 `÷ 0.9` 会得到 `668.2 / 1048.9`。**正解** = `x: 960 + (屏−960) ÷ 0.9`、`y: 540 + (屏−540) ÷ 0.9`
⇒ `601.37→561.52` · `943.97→942.19` · `301.23→274.70` · `342.99→321.10`，与 `menu_dump --no-ancestor-scale`
现读的 `561.5 / 942.2 / 274.7 / 321.1` **同一对数**。**宽度 380.67 那个数两法都得同一个值**（不受中心影响），
所以 `R4` 的**结论**没错，只是那句换算式写错。我按**设计帧那一对值**落的代码。

---

## 四、没查清 / 停手的部分（⛔ 一律不猜）

### 1. 🔴 `Editor/SettingsScene.cs` 有 **6 处 `CheckAutoFit` 的常量已过期**（**不在我白名单，一个字没动**）

它们收的是**字面量**（不引用 `SettingsWindow` 的常量），所以本笔改完**不会**让它们自动跟上：

| 行 | 断的是谁 | 现在写的 | 应改成 |
|---|---|---|---|
| `:4506-4507` | `General Tab > Bottom Buttons > Redeem Code > Button Text` | `…, 0, **300f, 90f**` | `…, 0, **274f, 53.3015f**` |
| `:4509-4510` | `… > Close Game Button > Button Text` | `…, 0, **300f, 90f**` | `…, 0, **274f, 53.3015f**` |
| `:4626-4627` | `Social Media Links > Discord Button > Button Text` | `…, 0, 101.45f, **33.55f**, true` | `…, 0, 101.45f, **19.7344f**, true` |
| `:4628-4629` | `… > IG Button > Button Text` | `…, 0, 101.45f, **13.55f**, true` | `…, 0, 101.45f, **19.7344f**, true` |
| `:4633-4634` | `Buttons > Twitch Button > Button Text` | `…, 0, **271.32f, 50.06f**` | `…, 0, **271.31f, 52.7787f**`（宽那一格原版是 271.3129） |
| `:4646-4647` | `Login Window > Login Button > Button Text` | `…, 0, 283.17f, **52.36f**, true` | `…, 0, 283.17f, **55.0857f**, true` |
| （对照，**不用改**） | `:4631-4632` `Register`(277.6/54) | 原版同值 | 不动 |

🔴 **为什么必须改、而不是「反正会绿」**：`CheckAutoFit` 的框那两条是**单边上限**
（`sz.x <= boxW*RS + 1.5` / `sz.y <= boxH*RS + 1.5`，见 `:425` / `:442`）。本笔把框**改小**之后，
**旧的（更大的）常量仍然放行** ⇒ 那几条会**静默变成空判据**（再也抓不住「字溢出原版框」）。
其中 `Twitch` 那一格**没有** `skipRendered`、而且框高是**变大**的（45.05→47.5 屏），
有**可能**真的转红 —— 我没跑 Unity，**这一格是红是绿没验**，如实登记。

### 2. 别的「没验」

- **改小 `GenButton` 的框（300×90 → 274×53.3015）会不会让字被自适应缩小**：**没验**（要跑 Unity）。
  判据上不会——原版跑起来就是**同一个框**（`fitW` / `r.H` 现在与原版逐值相同），
  且原版那颗的 `m_fontSize` 存的是**上限**（40 / 38，=`max`）而非收敛残值 ⇒ **原版在这个框里也没缩**。
  但这是**推理**，不是实测，⛔ 别当已验。
- **五颗社交钮的 `Button Text` 是 `INACT`（`m_IsActive = 0`）**，我们的也 `SetActive(false)`（`:2285`）——
  **原版那五颗在关着的状态下 ARF 到底跑不跑**，**没查清**（`AspectRatioFitter` 是
  `ILayoutSelfController`，`CanvasUpdateRegistry` 只收 `activeInHierarchy` 的件）。
  我按「`menu_dump` 算出来的 ARF 值」落地（= `R3` 的口径），**没有推翻它**，但如果原版关着时不跑 ARF，
  这个值只会在**它被打开时**才生效 —— 两种解释下**打开后的值都一样**，所以落地值不受影响。
  **没查清的是「哪一刻算的」，不是「值是多少」。**
- **`Quality Value` 改灰之后有没有别的断言盯着它的颜色**：我只 grep 到 `SettingsScene.cs:1698`
  断「非空」、`wantPx` 那 11 档字号表**已含 16.2**（18 × 0.9）⇒ 字号那一格不红。
  **颜色没有任何断言**这件事我是按 grep 结论记的，**没逐个跑断言**（没跑 Unity）。
- **`A1199`/`R2` 提到的那几笔 `ItemDrawer` 的 ARF**（`{Converted,Ephemeral} Drawer > Price Display`
  ratio **4.8848028**）：在 `Shell/ItemDrawer.cs`，**不在我白名单**，一个字没动。

---

## 五、🔑 顺手发现（⛔ 只报不改，由调度台分流）

1. 🔴 **`ARF 宽控高` 这一族远不止我改的 9 颗 —— 本文件还有一大半没改**。
   我现读 `Main Menu Settings Window` 全树：**36 个 `Button Text`** 每一颗都挂同一颗
   `AspectRatioFitter(m_Enabled=1 · Mode=1 · 5.140573)`（`R3` §四·1 也数到 36）。
   我这一笔只落了 **R3 点名的 9 颗**（判据齐的那 9 颗）。**其余没落的**在本文件里至少有：
   - `Support Tab` 那四颗外链钮的 `Button Text`（`SpTxtFaqL/T/R/B`、`SpTxtContact*`、`SpTxtSupport*`、`SpPrivacyTxt*`）
     —— **现读它们的框高**：`Faq/Contact` = `55.3246`（宽 284.40 ÷ 5.140573 = **55.3244** ✅ **已经是 ARF 值**，
     作者当年大概正好抄对了）；`Support` = `61.7342`（宽 317.35 ÷ 5.140573 = **61.7344** ✅ 也对）；
     `Privacy Policy` = `80.9248`（宽 416.00 ÷ 5.140573 = **80.9249** ✅ 也对）⇒ **这一族四颗不用改**。
   - `GenOnlineEntry`（我们自加那颗钮）：它的 `Button Text` 是 `y1…y2` = 整颗钮 90 高
     ⇒ **同一族**（我们自加的件没有原版判据，属「我们的选择」，但**形状与原版那一族不一致**）。
   - `Login Window > Login Button > Button Text` —— 已改（见 §二 3·f）。
   ⇒ **建议**：`R3` §四·1 说的「按**类**立账」是对的 —— 可以派一个只读代理把
   **本窗（以及 `ItemDrawer` / 各窗）所有挂 ARF 的 `Button Text` 逐颗核一遍「我们传的框 == ARF 之后的值」**，
   别再按「点名的那几颗」推。
2. ✅ **`R3` §四·1 的「本窗 36 颗 `Button Text` 全部 `m_Enabled=1`」经我独立复核，成立**。
   我自己扫了一遍（`sys.path` 引 `menu_rect.Bundle`，逐颗解 `m_Father` 父链、只收
   「祖先里有 `Main Menu Settings Window`」的件 + 按 `m_AspectRatio = 5.140573…` 筛）：
   本窗子树 **647 个 RectTransform**，其中挂这颗 ARF 的 = **36 颗，`m_Enabled=1` 的 36 颗、0 颗禁用** ✔
   ⚠️ **顺带查到的量级**（**不是本窗的**，别读混）：**整个 `bundle_menus_assets_all` 里**同一颗 ARF
   （同比值、同 `m_AspectMode=1`）共 **283 个组件**（**261 个 `m_Enabled=1` / 22 个 `0`**），
   几乎全在 `Button Text` 上（另有一颗 `Claimed Text`）。
   🔴 **我第一次扫的时候按「整个包」读，得出过「至少有 3 颗禁用」这个错结论** ——
   那 22 颗**不在本窗**（我没查它们在哪些树里）。**记在这里当反例**：报「全族都开着」之前先把**范围**钉住
   （本窗 36/36 成立 ≠ 全包都开着）。
3. 🔴 **`Terms of Service` 的「我们这边其实点不到」是个结构事实，不是偏离**：
   关掉之后 `SpTermsNoUrl()`（`:2702`）**0 调用点可及**（`Hit` 挂在关着的节点上）。
   我**留着**它 + `SpTermsUrlNote`（判据：原版那一颗也是恒关 ⇒ 「结构照原样」）。
   如果调度台想按本仓「死代码删」的规矩清掉，**那是另一笔**（要删两处 + 那条 `SpTermsL/T/R/B` 常量
   就只剩「占位」意义了）—— ⛔ 我没自行动手。
4. **`R4` §七·顺手发现 1 的另一半还在**：`Shell/EnergySinglePlayerOnlyEventWindow.cs` 里那两处
   （`SpFaqUrl` 那段注释 + `SpTermsUrlNote`）**仍写着「原版运行时拼出来的 URL」**——
   与本笔结论冲突，**不在我白名单**，一个字没动。**请分流。**
5. **`General Tab > Online Button`（我们自加）的 `Button Text` 框 = 整颗钮**（`:2899-2903` 的 `GenOnlineEntry`），
   而它**右边那颗兄弟是原版钮**（现在是 `274 × 53.3015`）⇒ 两颗**同一排、宽高形态不一致**。
   修法有两种：① 按 `GenBtnTxt*` 那组内缩；② 它没有原版判据、维持现状并在注释里写明。
   **我没动**（不在本次三笔里），**登记给调度台**。
6. 🔴 **这 6 处 `CheckAutoFit` 的常量是「第二份」**（原值散落在 `Editor/SettingsScene.cs` 的字面量里）——
   `AcBtnText` / `GenBtnTxtW` 改了，断言那份**不会跟着动**。这正是本仓那条
   「**两份迟早不一致**」的现场。**建议**：让断言**引用** `SettingsWindow.GenBtnTxtW` 这类常量，
   或至少加一条「断言里的框 == 我们实际传的框」的自检。
