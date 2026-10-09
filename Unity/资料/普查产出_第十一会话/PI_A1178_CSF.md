# PI · `A1178` —— 复刻原版那两颗的 `ContentSizeFitter(h: PreferredSize)`（框宽跟文字走）

> 执行代理 **P-I** · 2026-10-10（第十一会话）。只碰了白名单那一份 `.cs`；⛔ 没跑 Unity / 没动 git / 没碰正本 / `d:/2/**` 只读。
> 判据先读了 `资料/普查产出_第十会话/R4_布局刻度族查实.md` §一（**没重查它已经在的结论**）。

---

## 1. 一句话结论

**做了**（`Shell/EnergySinglePlayerOnlyEventWindow.cs`，`vtW/txW` 两处 + 一个量宽助手 `PreferredWidthPx`，共 +126/−14 行）：
两颗的框宽改成**建树期用我方字体的 TMP 量一次 preferred width 再写回**，取代原来写死的 `254.112` / `289.72`。
类型检查过（运行时 0 / 编辑器 0）。

🔴 **但量出来的宽【比旧常量宽得多】**（离线估算：`Victories title` ≈ **243–293px**、`Timer` ≈ **330–396px**，
旧值 254.112 / 289.72）⇒ 两个后果必须跟着处理，**其中一条不在本件白名单里**：

1. **`Timer` 这一颗今天其实是「被自适应压小」的**（我方字体排 `'Termina en: 23d 5h'` 需要 330–396px，
   而框只有 289.72 ⇒ TMP 二分把字缩到 **27.8–33.4px**，不是原版的 38px）。本件把它**放大回 38px** ——
   这是**修掉一个真缺陷**（不是"零位移重构"）。`Victories title` 那一颗落在边界上（估算 243–293 vs 框 254.1）。
2. 🔴 **`Editor/ShellScene.cs` 的两条自检会因此变红**（`:6624` `Timer` / `:6628` `Victories title`，
   A1126 那一节）：它们把**旧框宽当字面量**传进去断「渲出来 ≤ 框」。**这份文件不在我的白名单** ⇒
   ⛔ 没动，见 §7·① 的处置建议（改两个字面量即可，但更该换掉断言形态）。

---

## 2. 🔑 字体查实（本件要求的「先查实」那一条）

### 2·1 这两颗**用哪个字体资产**：**`NotoSerifCJK-Regular SDF`**

| 环节 | 实据 |
|---|---|
| 全仓给 TMP 赋字体的**只有一处** | `Core/TmpFont.cs:192` `t.font = Font;`（在 `NewText` 里）—— `TmpFont.cs` 头部自陈：「全仓只有一个字体」 |
| 那个字体是谁 | `Core/TmpFont.cs:63` `ResourcePath = "Fonts/NotoSerifCJK-Regular SDF"`（`Resources.Load<TMP_FontAsset>`，`:78`） |
| 本窗的文字走不走它 | 走：两颗都是 `MenuDraw.Text(...)` → `MenuDraw.TextCore`（`Shell/MenuDraw.cs:1843`）→ `Label.Create` → `Label.BuildTmp` → `TmpFont.NewText` |
| 资产实体 | `Assets/CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset`（**Dynamic**、图集 1×1 占位 ⇒ 字形按需从 `Assets/CardPresentation/Fonts/NotoSerifCJK-Regular.ttf` 光栅化） |
| 原版那两颗用的是**另一个** | `Pragati-Regular SDF`（`pointSize 95`、无 kerning）—— 判据 → `R4` §1·2 |

⇒ 🔴 **`R4` §六·1 那条「没查清」现在结清了**：量的必须是**我方（NotoSerifCJK）**的宽，
⛔ 不是 `R4` 算出的 176.98 / 248.44（那是 Pragati 的宽）。**这两个数本件一个都没写进代码。**

### 2·2 量出来的 preferred width 是多少

⚠️ **如实说：没能拿到 Unity 的实读数**（红线：本件不许跑 `-executeMethod`）。
下面给的是**离线估算**（判据链逐条给全，标明哪一步是实读、哪一步是推算）：

- **advance 和（以 em 计，实读）**：用 Pillow/FreeType 读工程里那份 TTF（`Fonts/NotoSerifCJK-Regular.ttf`，`size=10000` 消舍入）：
  `'Victories: '` = **4.910 em** · `'Termina en: 23d 5h'` = **9.363 em** · `'751'` = **1.617 em**
  交叉验证：同一次读出 `'国'` 墨宽 = **1.000 em**（全角字，符合预期）、墨高 **0.898 em**、`'M'` 大写高 **0.729 em**（与代码里「拉丁大写 ≈ 0.72 em」那条吻合）。
- **px / em 这一个系数有 4 个候选**（本工程自己就**自相矛盾**，见 §7·⑤）：
  `px = Σadv(em) × (px/em) × 标称 px`，其中 `px/em` 的四个读数：

| 口径 | 出处 | px/em（相对标称 px） | `'Victories: '`@53.5 | `'Termina en: 23d 5h'`@38 |
|---|---|---|---|---|
| (d) `em = fontSize × W × 100` | `Battle/BattleDriver.cs:11931` 明写 | 0.926 | **243.2** | **329.4** |
| (c) `W` 就是 em 比（glyph quad = em 框） | `Label.FontSizeToPx` 口径 | 1.000 | **262.7** | **355.8** |
| (a) 行盒 14.37@fs100 ÷ 1.437 em ⇒ em = 0.1×fontSize | `Core/TmpFont.cs:94-99` 那段注释 | 1.055 | **277.1** | **375.3** |
| (b) TTF 的「国」墨高 = 0.898 em ⇒ em = W/0.898 | 本件实读（Pillow） | 1.114 | **292.5** | **396.2** |

⇒ **我方 preferred width 的落点区间**：
**`Victories title` ≈ 243–293px**（旧常量 **254.112**）· **`Timer` ≈ 330–396px**（旧常量 **289.72**）。
🔑 **结论对上面四个口径都成立**：`Timer` 无论按哪一个都**比旧框宽** ⇒ 那一颗今天**一定被压小了**；
`Victories title` 落在边界上（(d) 口径下不需要缩，另三个口径下要缩一点）。

---

## 3. 逐处改动清单

文件：`d:/4/Unity/MyGame/Assets/CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs`（**LF 保持**，`841→851` 行，`git diff --numstat` = **126 / 14**）

| # | 位置 | 改前 → 改后 | 判据 |
|---|---|---|---|
| ① | `:68` | 加 `using TMPro;` | 量 `GetPreferredValues()` 要 TMP 的类型 |
| ② | **新增助手 `PreferredWidthPx(lb, who)` `:809`**（约 60 行含注释） | 无 → 取 `lb.GetComponentInChildren<TextMeshPro>(true)`、量 `GetPreferredValues().x`（**世界单位**）⇒ `× (DesignPxH/DesignHeight)` 换成画布 px；量不到 ⇒ **出声 + 返回 0** | 与原版 CSF **同一个函数**：`TMP_Text.cs:3684-3699`（字号 = `enableAutoSizing ? m_fontSizeMax : m_fontSize`、margin = ∞）。取 TMP 的方式**照抄同宿主既有先例** `Editor/ShellScene.cs:348`（`ShellLabelRenderedPx` 也是这么取的）—— `Label._tmp` 私有、共用件不在白名单，所以没碰 `Battle/Label.cs` |
| ③ | `Victories title` `:587-590` | `MenuDraw.AlignRight(vt, vtR)` → 先 `vtW = PreferredWidthPx(...)`、`vtFit = (VTitleX2−vtW → VTitleX2)`、`vt.SetAutoFitBox(框=vtFit, 18/53.5/36)`、再 `AlignRight(vt, vtFit)` | 生长方向 = **右沿不动**（原版这一颗 `H=Right`、我们也是右对齐；`VTitleX2 = 1574.60` 是现读值）。折行档**不还原**（原版这一颗就是 `折行=1`） |
| ④ | `Timer` `:693-703` | `if (tx != null) tx.SetWrapping(false);` → 再 `txW = PreferredWidthPx(...)`、`txFit = (TimerTxX1 → TimerTxX1+txW)`、`SetAutoFitBox(框, 10/38/38)`、**紧接着再 `SetWrapping(false)`** | 生长方向 = **左沿不动**（`TimerTxX1 = 981.95` = 图标右沿；原版这一颗在 HLG 里排在图标**之后**、框本就在右侧）。`SetAutoFitBox` 内部会 `SetWrapWidth` ⇒ **无条件**把模式开成 `Normal`，而原版是 `折行=0` ⇒ 成对还原（先例 A404/A205/A34-F4） |
| ⑤ | `Open()` `:304-313` | 无 → 一句 `Debug.Log` 出声：**做了什么 + 已知差异 + 判据路径**（红线「不许静默失败」） | 见 §5 |
| ⑥ | **就地订正两处错的旧结论**（铁律 5）：`:558-562` 那段「这两格是我们摆的」、`:571-575` 与 `:659-665` 两段「254.11 若比原版 preferred 窄 ⇒ 会缩字」/「这条边界我们复刻不了」 | 保留更正痕迹（写「**2026-10-10 就地订正（A1178）**：原来写 X，实际 Y」） | ① 方向反了（`R4` §1·3 现算：254.112 vs 176.98 = 我们**宽 44%**）；② 「复刻不了」**已不成立**（本件做了） |

⛔ **没碰**：`Total Victories`（`:594-606`，**同一扇窗的第三颗 CSF**，见 §7·①）· 任何别的 `.cs` · 两张正本。

---

## 4. 次序：`SetAutoFitBox` 的框 vs CSF 撑出来的框，谁说了算

**一句话：框宽 = 量出来的 preferred width 说了算；字号只是「框定完之后」跟着收敛的那个结果。**

1. **量（第一步）**：`GetPreferredValues()` 用的是 **`m_enableAutoSizing ? m_fontSizeMax : m_fontSize`** +
   margin ∞（`TMP_Text.cs:3684-3699` 的 `GetPreferredWidth()`）⇒ 量出来的宽是「**按上限字号排一行**」的宽。
   🔑 这一步**与当时的框宽无关**（不是量"现在渲多宽"）⇒ 就算初值框把字压小了，量出来的仍是正确值（本件的量法因此是稳的）。
2. **写回（第二步）**：`SetAutoFitBox(框 = 上一步的宽, ...)` ⇒ 走 `SetWrapWidth`（写 `sizeDelta.x`）+
   写 `sizeDelta.y` + 重设 `[min,max]/base` + `ForceMeshUpdate` ⇒ **重新二分一次**。
3. **为什么是同一个不动点（字不会被缩）**：框正好 = 「上限字号一行的宽」⇒ 二分一上来就在上限、
   **装得下 ⇒ 不缩**。两条判据（都实读包源码）：
   - `TextMeshPro.cs:2342` `widthOfTextArea = marginWidth + 0.0001f − …`（**自带 0.0001 富余**，
     折行/溢出判据是 `textWidth > widthOfTextArea`，`TextMeshPro.cs:3213`）；
   - `TMP_Text.cs:4843` `m_RenderedWidth = (int)(m_RenderedWidth*100 + 1f)/100f`（**向上取整到 2 位小数**）。
   ⇒ 框 ≥ 真需要的那条线 + 小富余 ⇒ 既不折行也不缩字。
4. ⚠️ **次序上的两个硬约束**（都写在代码注释里）：
   - **量必须在「字体资产 + 文本都就位」之后**（`Label.Create` 内已建 TMP，可在 `MenuDraw.Text` 之后立刻量）；
   - **`SetAutoFitBox` 要在 `SetWrapping(false)` 之前**（后者会 `ForceRelayout`，顺序反了模式会被下一次重排顶掉）；
   - **不能只调 `SetWrapWidth` 收窄框**：那会把 `sizeDelta.y` 写成 `0` ⇒ `marginHeight = 0` ⇒
     **竖直方向的自适应会误判「装不下」而缩字**（`TextMeshPro.cs:3049` 那条 `textHeight > marginHeight + 0.0001f`）
     ⇒ 必须走 `SetAutoFitBox`（它把 `sizeDelta.y` 补回原版框高）。
5. **和 `Align*` 的次序**：`AlignRightOn/AlignLeftOn` 内部各自 `RefreshBounds()`，所以**重排之后再对齐**（本件就是「先 `SetAutoFitBox`、后 `AlignRight`」）。

---

## 5. 已知差异怎么如实标注（+ 一处对简报措辞的订正）

已写进 `Open()` 的出声（`:304-313`）与两处代码注释：

- **原版那个 fitter 是运行期活的**：换语言 / 倒计时文案一变，框跟着变；**我们的框是建树期量一次写死的**
  ⇒ 文案在**运行期变长**时：**原版撑框、我们缩字**（`R4` §1·4 那条残差**仍然成立**）。
  本窗文案目前全走 prefab 出厂原文、没有换语言的路 ⇒ 这条今天**够不到**（如实写"够不到"，不假装已解决）。
- 🔴 **对简报里那句话的订正（铁律 2/5）**：简报写「**换语言/换字体时我们不会自动变**」——
  **「换字体」那一半不成立**：量就发生在**建树那一刻**、用的是**当时那个字体资产**（`TmpFont.Font`）⇒
  换字体资产，量出来的宽跟着变。
  ⛔ 真正不会自动变的**只有「运行期换语言/换文案」这一条**。代码注释与出声里都按这个口径写的。

---

## 6. 验证

| 项 | 读数 |
|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_pi bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数: 0 · 编辑器错误数: 0**（跑了 **2 次**：加完 `using` 后一次、全部改完后一次） |
| `git diff --numstat` | `126 14 Unity/MyGame/Assets/CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs`（**没有整篇重写**；行尾复核 `b'\r\n' = 0 / b'\n' = 851` ⇒ **LF 保持**） |
| Unity 自检 | ⛔ **没跑**（红线：本件不许跑 `-executeMethod`；且铁律 12 的最新口径 = 活没干完不跑） |
| 断言 | ⛔ **本件一条都没加**（自检宿主 `Editor/ShellScene.cs` 不在白名单）⇒ §7·① 给了要改的那两条与**更好的断言形态** |

---

## 7. 没查清 / 停手的部分

1. **`Editor/ShellScene.cs` 那两条 `fitCase` 没改（不在白名单）—— 它们会红。**
   - 位置：`:6624`（`Timer`，`boxW = 289.72f`）· `:6628`（`Victories title`，`boxW = 254.112f`）。
   - 红的机制：`fitCase` 断言的是 **`ShellLabelRenderedPx(lb).x ≤ boxW + 1.5`**（`:6540`），
     而 `boxW` 是**旧常量**。本件把框放/收之后 `Timer` 的渲染宽会到 **330–396**（> 291.2）⇒ **一条必红**；
     `Victories title` 落在 243–293（> 255.6 才红）⇒ **大概率红**。
   - 🔑 **别只改字面量**：新框是**我们自己量出来的**，把期望值换成量出来的数就是同义反复（CLAUDE.md §三「灭自证」）。
     建议改成断**关系**：① 「框宽 ≈ 在上限字号上量出的 preferred width（±1.5px）」；
     ② 「**字没被缩**：`Label.FontPxNow` ≈ `FontSizeToPx(FontSizeMax)`（= 原版 53.5 / 38px）」——
     第②条才是本件真正建立的性质，而**今天没有任何一条断言咬它**（`fitCase` 只断 min/max/base，不断收敛后的 `FontSize`）
     ⇒ 这正是「Timer 长期被压小 12–26% 却全绿」的原因。**建议在新账里补这两条**。
2. **`Victories title` 今天到底有没有被缩** —— 落在估算边界上（见 §2·2）。要么等我方 `px/em` 系数定死（§7·⑤），
   要么跑一次读 `Label.FontPxNow`。
3. **原版那两颗的 `pivot` / `anchor` 没查** —— 框往哪个方向长，原版是 pivot + HLG 决定的。
   我们这两处按「**我们这边已经在对齐的那条边**」定向（title 右沿 / Timer 左沿），理由写在注释里（§3·③④）。
   这**不是**原版读数，如实登记。
4. **`SetAutoFitBox` 被调第二次的副作用只做了静态核对**（`Battle/Label.cs:826-913` 逐句读：先关自适应再写 base、
   早退那一格由 `EnsureFontSizeBase` 反射补写）—— **没实跑**（红线）。理论上与第一次同路（收敛到同一个上限），
   风险点是 `m_fontSizeBase` 被写两次（同值）；**要真确认得跑一次**。
5. **`GetPreferredValues()` 对活 TMP 的副作用**：它内部 `ParseInputText()` 会重填 `m_TextProcessingArray`、
   并把 `m_isCalculatingPreferredValues` 置真再于 `TMP_Text.cs:4833` 置假；本件紧接着就 `SetAutoFitBox`（内含
   `ForceMeshUpdate`）⇒ 中间态被冲掉。**静态判据齐，但同样没实跑。**

---

## 8. 🔑 顺手发现（⛔ 一律只报不动）

### ① 同一扇窗的**第三颗** CSF：`Total Victories`（`EnergySinglePlayerOnlyEventWindow.cs:594-606`，`'751'` @77px）

- 判据：`R4` §1·1 第 2 行明写「`PLayer Victories/Total Victories`（另一颗同样 CSF）」+
  `资料/普查产出_第十会话/W2_A1126A1.md:163` 也点了它；代码注释自己写着「原版框 **宽 0** + CSF，
  下面传的 `ttR.W` = **213.99 是我们摆的**」。
- 简报说「本件只做这两颗」，所以**没动**。但要改是**现成三行**（左沿 `VTotalX1` 不动）：
  ```csharp
  float ttW = PreferredWidthPx(tt, "Total Victories");
  var ttFit = ttW > 0f ? new PxRect(VTotalX1, VTitleY1, VTotalX1 + ttW, VTitleY2) : ttR;
  if (ttW > 0f) tt.SetAutoFitBox(LayoutSpace.Px(ttFit.W), LayoutSpace.Px(ttFit.H), 18f, 77f, 36f);
  MenuDraw.AlignLeft(tt, ttFit);      // 替掉现在的 `MenuDraw.AlignLeft(tt, ttR);`
  ```
  （估算 `'751'` 我方 preferred ≈ **120–163px** < 旧框 213.99 ⇒ 今天**没被压小**，这一处**画面上是零位移**，纯结构复刻。）

### ② 🔴 **这两颗的**位置**本身也来自「零宽模板位」——比框宽更值得立账**

- 现读（`menu_dump.py bundle_menus_assets_all "EnergySinglePlayerOnlyEventWindow" --no-layout`）：
  原版 `Timer` 节点是 **`HorizontalLayoutGroup`**（`spacing 0 · align=4 MiddleCenter · pad 0,0,5,0`），
  两个子件 `Timer Icon`(43.91) 与 `Timer`(TMP, 宽 0 + CSF)。
- 我们的常量 `TimerIcR.x1 = 938.05` **恰好等于**「按 CSF 宽度 = **0** 跑 HLG」的结果：
  `648.33 + (623.34 − 43.91)/2 = 938.05` ⇒ 这是 CLAUDE.md §三最后一条点名的那种**模板位**。
- 原版**运行时**（它自己字体 pref = 248.44）应当是：图标 `648.33+(623.34−(43.91+248.44))/2 = 813.8`、
  文字 `857.7 → 1106.1`。旁证：prefab 里 `Timer`(TMP) 的原始 x 就是 **857.7**。
- **换成我方字体**（pref 330–396）后，**原版那套算法**会给：图标 ≈ `750–772`、文字 ≈ `794–816 → 1150–1212`。
- ⇒ 严格复刻的眼睛应该看**这一条**（图标 + 文字整组在容器里居中），而它**超出本件范围**（会同时挪图标）。
  同理 `Player victories` 也是 HLG（`Victories Background` / `Victories title` / `Skull Victories` / `Total Victories`），
  那四颗的绝对位同样是从「CSF 宽 = 0」的模板位推出来的。**建议单开一笔账。**

### ③ 简报前提被现读推翻一条：`SkirmishEventWindow` **其实建了** `Victories title`

简报写「那两颗**这扇窗一件都没建** ⇒ 大概率不用动它」。现读：
`Shell/SkirmishEventWindow.cs:132` 建了 `Victories title`（`'Victories: '` · fs **48** · `auto[18~48]` · 右对齐），
`:244` 也建了 `Timer` **节点**（只是**没画字**，`:245` 出声说明）。
⇒ 那一扇的 `Victories title` 的框也是**我们摆的常量**，同一条账；⛔ 本件按简报**没动**它。

### ④ `Victories title` 的「框宽 0」在 R4/`W2` 里被当成「算不出」，其实**代码里能算**

`R4` §1·2 已经算出了 Pragati 的 176.98；本件的 §2·2 又给出了**我方**的落点区间。
⇒ 「算不出」这句话在两个文档里都该更新（`W2_A1126A1.md:129-131` 还写着「我**量不出**原版 preferred……
要…重摆（那要先在真机上量一次）」—— **本件已经把这一半落地了**；那份文件不在我白名单，**没改**）。

### ⑤ 本工程「px ↔ em」的口径**自己有两套**（估算时撞出来的，⛔ 只报不改）

- `Battle/Label.cs:669` `FontSizeToPx = fontSize × W × 108`，注释称它 = 「**字形**世界高（≈0.0948）× 108」；
- `Battle/BattleDriver.cs:11931` 却写「**em** = 3.0 × 0.0948 × **100** = 28.4px ✓（自洽）」。
- 两者相差 8%（108 vs 100），而且**语义不同**（一个是字形墨高、一个是 em）。
  用 TTF 实读交叉核过：`'国'` 墨高 = **0.898 em**、`'M'` 大写高 = **0.729 em**（与 `Label.cs:924` 记的
  `Wcap/Wglyph = 0.0779/0.0948 = 0.822` **对不上**：0.729/0.898 = **0.812**）。
  ⇒ `W` 到底是「墨高/fontSize」还是「em/fontSize」**两处说法打架**，而它下游挂着**全仓字号**。
  **要定案得跑一次**（渲染 `'国'`/`'M'` 各量一次墨高与行盒）。本件只用到它的**相对**关系，所以估算给了 4 个口径的区间。

---

## 9. 建议立的新账（给调度台）

| # | 账 | 判据 / 做法 |
|---|---|---|
| 1 | `Editor/ShellScene.cs` 那两条 `fitCase` **重基线 + 换断言形态** | §7·① （**必做**：本件落地后不重基线就会红） |
| 2 | 同一扇窗的**第三颗** CSF（`Total Victories`） | §8·①（三行，脚本已给） |
| 3 | 🔴 **HLG 组的真实位置**（`Timer` / `Player victories` 两族，含图标） | §8·②：按 `container.x1 + (containerW − Σ子件首选宽)/2` 算，别再用零宽模板位 |
| 4 | `SkirmishEventWindow` 的 `Victories title`（+ `Timer`）同一条 CSF 账 | §8·③ |
| 5 | 「px ↔ em」两套口径定案（影响全仓字号换算） | §8·⑤ |
| 6 | 断言补「**字没被缩**」（`FontPxNow ≈ FontSizeToPx(FontSizeMax)`） | §7·① 第②条 —— 这一类"框太窄把字压小"的缺陷今天**没有任何断言能咬到** |
