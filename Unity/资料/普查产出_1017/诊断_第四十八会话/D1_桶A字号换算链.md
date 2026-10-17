# D1 · 桶 A（字号/自适应窗口 ×0.912 / ×0.898）只读诊断

> 只读诊断代理 D1，2026-10-17。**没有改任何代码、没跑 Unity、没动 git。**
> 输入 = `资料/普查产出_1017/自检_第四十七会话_结果与分诊.md` 桶 A / 桶 D + 四个宿主的运行日志
> （`d:/4/_tmp_view/{battle,menu,rewards,shop,settings}.log`，第四十七会话 19:14–19:52 那一轮）。

---

## 一、结论（先给答案）

1. 🔴 **根因只有一条（换算尺随字体面变了）**：`TmpFont.EmPerFontSize(face)` 的**两支量的是两个不可通约的量** ——
   `Face.Cjk` 那一支返回的是 **"国"字墨高**（`WorldGlyphPerFontSize = 0.0948` ≈ 0.898 em），
   而三个拉丁面那一支算的是 **"大写墨高 ÷ capLine/pointSize"**（≈ 1.0 em）。
   两者差 **9–11%**，于是"1 px 折多少 fontSize"这把尺**随 `Face` 变化**：
   `Em(Cjk)=0.0948` · `Em(LatinCardBody/Pragati)=0.103906` · `Em(LatinUi/Asar)=0.105507`。
   **0.9122 和 0.8983 就是 `Em(Cjk) ÷ Em(拉丁面)` 这两个比值**（精确值 0.912361 / 0.898517，见 §三）。

2. 🔴 **根因在生产侧的落地形态（真缺陷，不是断言错）**：所有"**先建空标签 → `SetFace(拉丁)` + `SetAutoFitBox`
   → 之后才把文本填进来**"的站，写四格时 `ActualFace = 拉丁`，文本到达后是汉字 ⇒ `ActualFace` 翻回 `Cjk`
   （字体也跟着 `SyncFace` 换回 Noto）⇒ **四格是按拉丁 Em 写的、渲染/读回却按 Cjk Em** ⇒ 整体 ×0.912/×0.898。
   **实测受影响 = 战斗侧那 27 条**（`TitleText_Me/Foe` · `EnemyPlateText` · `PlayerPlateText` ·
   `My/FoePileLabel` · 联盟窗 `Name Text`/`Title Text`/`Alliance Name`）—— 它们的 `m_fontSizeMin/Max/Base`
   真的写小了 9–11%，**实渲字号也比原版小同样比例**。

3. 🔴 **根因在断言侧的落地形态（纯测量错）**：另有四个宿主用 **`Label.FontSizeToPx(...)`（静态口，恒按 Cjk）**
   或 **单实参 `Label.FontSizeToPxNow(x)`（`Label.cs:852-856` 转调时写死 `Face.Cjk`）** 去量**拉丁面标签**的
   TMP 字段/收敛值。生产完全正确也会读出 ×0.912/×0.898。
   受影响：`ShopScene` 176 红的大头 · `SettingsScene` 那 6 颗 32.84 · `MainMenuScene` 约 27 条 · `RewardsScene` 的 A468 那几族。
   `ShopScene.RulerFontSize`（`:632-644`）还额外恒按 Cjk 建"标尺"，等于**拿两把尺比**。

4. ✅ **桶 D 与桶 A 同根，且不是"漏乘 0.9"**：那 6 颗 `Text=32.84` 正是 `AcInput` 的输入值标签
   （`SettingsWindow.cs:6143` / `:6229`，`face: LatinCardBody`，`FontPx = 40×0.9 = 36`），
   **0.9 那层已经乘过了**；32.84 = **36 × 0.912361**，是静态口按 Cjk 折拉丁面字段的结果。

5. 🔑 **修法：改一处共用件就够** —— 让 `EmPerFontSize` **与字体面无关**（恒 = `WorldGlyphPerFontSize`）。
   Cjk 侧逐位不变；拉丁面标签的字号回到"同一个 px = 同一个 em"（= A1024③ 之前的口径），
   静态口与实例口自动一致 ⇒ 桶 A / 桶 D 的 ~280 条一起消失，**不需要逐站点改断言**。

---

## 二、证据

### 2·1 换算链（生产，按代码顺序）

| 位置 | 内容 |
|---|---|
| `Core/TmpFont.cs:236-249` | `EmPerFontSize(Face f)`：`if (f == Face.Cjk) return WorldGlyphPerFontSize;`（**特例**）其余面 `MeasureGlyph(a,'M') ÷ (faceInfo.capLine / pointSize)` |
| `Core/TmpFont.cs:258-273` | `CapPerFontSize(Face f)`：拉丁面 = `MeasureGlyph(a,'M')`（**不除 capEm**） |
| `Core/TmpFont.cs:287-308` | `WorldGlyphPerFontSize` = `MeasureGlyph('国')` = **0.0948**；`WorldCapPerFontSize` = `MeasureGlyph('M')` = 0.0779 |
| `Core/TmpFont.cs:542-578` | `MeasureGlyph` = `(characterInfo.topLeft.y − bottomLeft.y) ÷ 100`，**含 SDF padding `2×1.25`** |
| `Battle/Label.cs:1220-1233` | `TmpFontSize()`：`_glyphHeight > 0 ⇒ FontSizeForEm(ActualFace, _glyphHeight)`（**读的是"现在这一刻"的 `ActualFace`**） |
| `Battle/Label.cs:877-880` | `ActualFace` = `(_face != Cjk && Loc.HasCjk(_text)) ? Cjk : _face`（**文本变 ⇒ 面变**） |
| `Battle/Label.cs:1280-1287` | `SetTextTmp` = `SyncFace()` → `_tmp.text` → `_tmp.fontSize = TmpFontSize()` → `ForceMeshUpdate` |
| `Battle/Label.cs:1049-1136` | `SetAutoFitBox`：`cur = TmpFontSize()`（:1080）· `nomPx = NominalPx()`（:1092，= `FontSizeToPxNow(cur, ActualFace)`）· `baseCur = cur×(basePx/nomPx)`（:1125）· 写 `m_fontSizeMax/Min`（:1130-1131） |
| `Battle/Label.cs:251-255` | `SetText` 首句 `if (text == _text) return;` |
| `Battle/Label.cs:883-889` | `SetFace`：`_face=f; SyncFace(); if(_tmp!=null) SetText(_text);` —— 注释说"字号按新面重算"，但**同串会早退**（见 §六） |

### 2·2 资产的实测值（判据不是我们的常量，是字体资产本身的字段）

| 面 | 资产 | `m_PointSize` | `m_CapLine` | `'M'` 度量高 | 材料 padding | `MeasureGlyph('M')` | `EmPerFontSize` |
|---|---|---|---|---|---|---|---|
| Cjk | `Resources/Fonts/NotoSerifCJK-Regular SDF.asset` | 50 | 36 | `'国'` 898/1000 em | `_GradientScale 6`, 其余 0 ⇒ 1.25 | `'国'` = **0.0948** | **0.0948**（特例，不走算式） |
| LatinUi | `Asar-Regular SDF.asset` | 94 | 61 | 61.859375 | 1.25 | 0.0684674 | **0.105507** |
| LatinCardBody | `Pragati-Regular SDF.asset` | 95 | 60 | 59.84375 | 1.25 | 0.0656250 | **0.103906** |

- 算式：`MeasureGlyph = (glyphHeight + 2×1.25) × 0.1 ÷ pointSize`（3D `fontScale = fontSize/pointSize×0.1`）；
  `Em = MeasureGlyph ÷ (capLine/pointSize)`。
- 资产出处：`m_FaceInfo`（`Asar…asset:16-37` / `Pragati…asset:16-37` / `Noto…asset:129-150`）、
  `m_GlyphTable`（`Asar…asset:739` 起 `m_Index 49` / `Pragati…asset:649` 起 `m_Index 68`，`m_Unicode 77` = 'M'）、
  材料 `WarpforgeVFX/Materials/{Asar,Pragati}-Regular Atlas Material.mat`（`_FaceDilate 0` / `_OutlineWidth 0` ⇒ padding 只有 1.25）。
- 参考：**同一算式套到 Noto 的 'M' 上会得到 `0.0779 ÷ 0.72 = 0.108194`** —— 比 `WorldGlyphPerFontSize 0.0948` 高 14%。
  这就是"两支不可通约"的量化证据（Cjk 支量的是**字墨**，拉丁支量的是 **em**）。

### 2·3 生产侧的触发顺序（以战斗 HUD 为例）

- `Battle/BattleDriver.cs:16901` `Label Hud(root, text, …)` → `:16917` `Label.CreateUgui(cv, text, …)`；
  `:16925` `if (face != Cjk) l.SetFace(face);` → `:16934` `l.SetAutoFitBox(boxWpx, boxHpx, autoMinPx, autoMaxPx, autoBasePx);`
  —— **四格写下的这一刻 `_text` 还是 `""` ⇒ `ActualFace = 拉丁`**。
- 建这些标签时传的文本全是 `""`：`BattleDriver.cs:14478`（`EnemyPlateText`）· `:14491`（`PlayerPlateText`）·
  `:14901-14905`（`My/FoePileLabel`）· `:15537-15542`（`TitleText_Me/Foe`）。
- 之后才填汉字：`:17225-17227`（`_myText` = 「玩家123  星际战士」、`_enemyText` = 「<中文阵营>」）·
  `:17260-17261`（`Loc.T(DeckCountTerm)+": "+n`）· 称号（`猎女`，见日志 `battle.log:162972`）。
  ⇒ `SetTextTmp → SyncFace` 把字体换回 Noto、字号按 Cjk 重算，但 **min/max/base 仍是拉丁面写的**，
  TMP 自适应把它们当墙 ⇒ **实渲字号被压在 0.898–0.912 × 原版**。
- 同一形态在联盟窗：`Shell/AlliancePanelWindow.cs:513-514`（`Name Text` 以 `""` 建 + `LatinUi`）、
  `:522`（`Title Text`）、`:531`（`Alliance Name`），`Apply("", "", "")` 之后才填名字。

### 2·4 读回证据（日志逐条都能被 §三 的比值复现）

- `d:/4/_tmp_view/battle.log:163084-163110`：`TitleText_Me` 下界 `2 → 1.80`、上界 `35 → 31.44`、base `36 → 32.34`
  （= ×0.8985，Asar）。
- `battle.log:164667-164693`：`MyPileLabel` `10 → 9.12` · `42 → 38.31` · `49.63 → 45.27`（= ×0.9124，Pragati）。
- `battle.log:309859-310123`：A1192 联盟窗 3 颗 × 3 格，`14.4 → 12.94` · `32.0 → 28.75` · `28.8 → 25.87`（= ×0.8985，Asar）。
- `d:/4/_tmp_view/shop.log:36336`：`name 的自适应上限 42px（实得 38.31）`；`:22415`：`40px（36.49）`（= ×0.9124）。
- `d:/4/_tmp_view/settings.log:21462`：`有 6 个不在里面：Text=32.84 ×6`（= 36 × 0.9124）。
- `d:/4/_tmp_view/menu.log`：`9.122 / 38.312 / 20.980 / 30.102 / 16.170 / 64.680 / 60.683 …`（×0.898–0.912）。
- `d:/4/_tmp_view/rewards.log`：`50.17 / 145.95 / 35.58 / 32.84 / 26.27`（×0.912）· `30.99 / 37.19`（×0.898）。

### 2·5 断言侧用的口（这一半与生产无关，必须单独记）

| 宿主 | 位置 | 用的口 |
|---|---|---|
| `Editor/ShopScene.cs` | `:3030` `:3049-3050` | **静态** `Label.FontSizeToPx(...)` |
| `Editor/ShopScene.cs` | `:632-644` `RulerFontSize` | 标尺恒按 **Cjk** 建（`MenuDraw.Text` 不传 face），而 19 个变体的标签全是 `LatinCardBody`（`Shell/OfferContainer.cs:1107-1202`） |
| `Editor/SettingsScene.cs` | `:361-370`（`CheckAutoFitPx`）· `:4575`（全窗 112 颗扫）· `:4601-4602` | **静态** |
| `Editor/MainMenuScene.cs` | `:1575-1577` · `:3570-3574` · `:4135-4139` · `:5951-5955` · `:13469-13470`（`CheckFontWindow`）· `:13488`（`CheckFontBase`） | **静态** |
| `Editor/RewardsScene.cs` | `:7410-7415` · `:11468-11472` · `:11486-11487` · `:6652` · `:7631-7635` | **静态** |
| `Editor/BattleScene.cs` | `:23390-23392`（A1192） | **单实参实例口** `FontSizeToPxNow(x)`（`Label.cs:852-856` 内部写死 `Face.Cjk`） |
| `Editor/BattleScene.cs` | `:582-591`（CheckHudBox） | **双实参实例口**（传 `lb.ActualFace`）—— 所以它量到的是"真渲出来的 px"，才会照出 §一·2 的真缺陷 |

---

## 三、0.9122 / 0.8983 的来源（逐项点名）

```
0.912361 = EmPerFontSize(Cjk) ÷ EmPerFontSize(LatinCardBody/Pragati)
         = 0.094800000 ÷ 0.103906250
         = WorldGlyphPerFontSize('国' 墨高) ÷ [ MeasureGlyph(Pragati,'M') ÷ (m_CapLine 60 ÷ m_PointSize 95) ]
         = 0.0948 ÷ [ ((59.84375 + 2×1.25) × 0.1 ÷ 95) ÷ 0.631578947 ]

0.898517 = EmPerFontSize(Cjk) ÷ EmPerFontSize(LatinUi/Asar)
         = 0.094800000 ÷ 0.105507172
         = 0.0948 ÷ [ ((61.859375 + 2×1.25) × 0.1 ÷ 94) ÷ (61 ÷ 94) ]
```

代入逐条对得上的读数（期望 → 实得）：

| 原版值 | 面 | ×比值 | 计算 | 日志 |
|---|---|---|---|---|
| 42 | Pragati | 0.912361 | 38.3192 | 38.31 / 38.312 |
| 10 | Pragati | 0.912361 | 9.1236 | 9.122 |
| 33 | Pragati | 0.912361 | 30.1079 | 30.102 |
| 40 | Pragati | 0.912361 | 36.4944 | 36.49 |
| 36 | Pragati | 0.912361 | 32.8450 | 32.84（Settings 那 6 颗） |
| 35 | Asar | 0.898517 | 31.4481 | 31.44 |
| 36 | Asar | 0.898517 | 32.3466 | 32.34 |
| 67.55 | Asar | 0.898517 | 60.6948 | 60.683 |

⚠️ "36 → 32.34" 是 **Asar**（`.898517`），"36 → 32.84" 是 **Pragati**（`.912361`）—— 同一个 36 在两个面上落到两个数，
**这本身就是"尺随面变"的直接证据**。**没有任何常量被"同时改了两个"**：A1024③ 只**新增了 `EmPerFontSize` 的按面分支**，
其余（`WorldGlyphPerFontSize` 0.0948 / `PxPerWorld` 108 / `UguiFontScale` 10）一个都没动。

> 为什么观测到的是"更小"而不是"更大"：日志里的读数都发生在 **`ActualFace = Cjk` 的那一侧**
> （生产 = 文本翻面后渲染/读回；断言 = 静态口硬按 Cjk）⇒ 恒为 `Em(Cjk)/Em(拉丁)` < 1。
> 反方向（写入用 Cjk、读回用拉丁）会是 ×1.096 / ×1.113，本轮日志里**没有**这种读数。

---

## 四、修复方案

### ✅ 方案 A（推荐 · **一处共用件，零逐站点**）

**改 `Core/TmpFont.cs:236-249`：`EmPerFontSize(Face)` 与面无关，恒返回 `WorldGlyphPerFontSize`。**
连带：`Label.cs:832-835` 的 `FontSizeToPx(fontSize, face)`、`Label.cs:1220-1233` 的 `TmpFontSize()` 自动跟上；
`_faceEm` 缓存（`TmpFont.cs:168/239-247`）变成死码，一并删掉（⛔ 别留两份）。
`CapPerFontSize`（`TmpFont.cs:258-273`）**保持按面** —— 那是"大写高"语义，本来就该按面（`SetCapHeight` 那一路要用）。

**为什么这是"对"的那一处**（判据是 TMP 自己的换算，不是我们的常量）：
`fontScale = fontSize / pointSize × (orthographic ? 1 : 0.1)` ⇒ **1 em 永远 = fontSize（uGUI）/ 0.1×fontSize（3D）**，
**与字体无关**。`EmPerFontSize` 想量的本来就是"1 em 折多少 px"；拉丁支算出来的 0.1039/0.1055 与 Cjk 支的 0.0948
差的那 9–11%，全部来自"`国` 的字墨只有 0.898 em"和"SDF padding 2×1.25"这两笔，**不是物理差异**。
A1024③ 的动机（Pragati 行盒 0.947 em 让自适应不被压）是 **`lineHeight`** 的事，与换算尺无关 ⇒ 本方案**不动**那个收益。

**影响面：**

| 面 | 现状 | 改后 | 说明 |
|---|---|---|---|
| `Face.Cjk`（全仓绝大多数站） | 0.0948 | 0.0948 | **逐位不变**（特例分支就是同一个值） |
| `LatinCardBody/Pragati` | 0.103906 | 0.0948 | fontSize **+9.56%**，回到"A1024③ 之前同一个 px 的字号" |
| `LatinUi/Asar` | 0.105507 | 0.0948 | fontSize **+11.28%** |
| `LatinNarrow/Roboto`（**零消费者**） | 未量（`m_CapLine 20 ÷ m_PointSize 35` = 0.5714，`'M'` 度量高本笔没读） | 0.0948 | 无影响（全仓零消费者） |

- 断言：静态口与实例口**自动一致** ⇒ 桶 A 的 30+46+56+176、桶 D 的 6 颗**全部同时转绿**，
  **不需要改任何一条 Check**（`ShopScene` 的标尺也会自动对上，因为两侧 Em 同值）。
- 需要重跑的宿主（动了 `Core/TmpFont.cs` = 共用件）：`BattleScene` · `MainMenuScene` · `RewardsScene` ·
  `ShopScene` · `SettingsScene` · `DeckScene` · `CollectionScene` · `ShellScene` · `CardBaseDemo`。
- ⚠️ **要真机看一次**：那 ~510 个换面站点的字号会 **+10%**（这是修正，不是回归 —— 它与 Cjk 站口径一致了）。
  真机只看"有没有溢出框 / 折行变了"这类版面后果。

### ⚠️ 方案 B（备选 · 保留按面 Em，则**必须两处都改**）

- **B(a) 生产（一处，`Battle/Label.cs`）**：`SetAutoFitBox` 把实参 + 写入时的面记下来
  （`_fitW/_fitH/_fitMin/_fitMax/_fitBase/_fitFace`），在 `SetTextTmp`（`Label.cs:1280`，`SyncFace` 之后）与
  `SetFace`（`:883`）里发现 `ActualFace != _fitFace` 就**按记下的实参重放一次** `SetAutoFitBox`。
  重放会再进 `SetWrapWidth` / `RefreshBounds`，注意先清标记再干活（同 `TryApplyPendingWrap` 的写法）。
- **B(b) 断言（逐站点，约 40 处）**：全部换成**双实参**实例口 `lb.FontSizeToPxNow(x, lb.ActualFace)` ——
  `ShopScene:3030/3049/3050` · `SettingsScene:361-370/4575/4601-4602` · `MainMenuScene:1575-1577/3570-3574/
  4135-4139/5951-5955/13469-13470/13488` · `RewardsScene:7410-7415/11468-11472/11486-11487/6652/7631-7635` ·
  `BattleScene:23390-23392`；另外 **`ShopScene.RulerFontSize` 必须按目标标签同一面建**（加一个 `face` 形参）。
- 缺点：**同一把"px"在三个面上仍是三个数**（0.0948 / 0.1039 / 0.1055），本轮这类红以后还会以别的形式再来；
  且 B(a) 是新机制（多一份状态），比 A 的"一处常量口径"更容易再漂。

### ⛔ 不推荐

把 **Cjk 支也改成 `MeasureGlyph('M') ÷ capEm`**（= 0.108194）：那会让**全仓所有 Cjk 字号 +14%**，
推翻全仓版面基线（且与"汉字墨高 = px"的既有口径打架）。若将来要动"px 到底该是 em 还是字墨"，
那是**独立一笔账**（见 §五），⛔ 别混进桶 A 一起改。

---

## 五、没查清的部分（⛔ 不猜）

1. **Rewards / MainMenu 那批到底是"面真的翻了"还是"只用了静态口"** —— 两种都给出同一个 0.912/0.898，
   光凭读数**分不开**。要分清得在检查那一刻打印 `lb.ActualFace` + `TMP.font.name` + `Loc.HasCjk(lb.Text)`。
   **不影响修法**（方案 A 对两种都成立；方案 B 则要求先分清）。
2. **"px 应该 = 原版 em，还是 = 汉字字墨"** —— 现在这套口径（`Em(Cjk)=0.0948`）实渲出来 em ≈ **1.055×px**
   （比原版 `m_fontSize` 大 ~5%）。若按原版 em 对齐，需要把口径整体改成 0.1/1.0 —— 那是全仓级改动，
   **需要用户/调度台拍板**，本轮没动。
3. **拉丁面"实渲 vs 原版"的实际偏差**只有代数推算（≈ −5% 账面 / +5% 若改 A），**没有实拍**。
   要量得跑一次 TMP `textBounds` 探针或真机截图。

---

## 六、顺手发现（即使跑题也报）

1. 🔴 **`Label.SetFace` 的注释与实现不符**（`Battle/Label.cs:883-889` vs `251-255`）：
   `SetText(_text)` 的**首句就是 `if (text == _text) return;`** ⇒ 同串时**字号不会按新面重算**，
   注释里那句"字号按新面重算 + 重新排"**不成立**。本轮没炸是因为后面紧跟 `SetGlyphHeight` / `SetAutoFitBox`
   会强制重算；**单独 `SetFace` 的站**（例 `Core/Tooltip.cs:220`）字号会停在旧面。⛔ 别把注释当行为。
2. 🔴 **`FontSizeToPxNow(fontSize)` 单实参重载默认 `Face.Cjk`**（`Label.cs:852-856`）——
   `BattleScene.cs:23390-23392` 正拿它量 Asar 面标签（`12.94 = 14.4×0.8985`）。这是同一坑的**第二形态**：
   换口时只换了"静态→实例"，没注意实例口还有个默认面。方案 A 后它变无害，但建议在签名上加 `[Obsolete]` 式注释。
3. ⚠️ **`RewardsScene.cs:7565` 有一条 `ActualFace` 期望 `Pragati`、实得 `Cjk`**，而同一颗的
   `TMP.font.name` 那条是绿的（`rewards.log:29654`）⇒ 要么 `_text` 含汉字而 TMP 没被 `SyncFace` 同步（真不一致），
   要么 `GetComponentInChildren<Label>(true)` 抓到了**父级另一颗 Label**（断言假红）。**没查清**，别当结论。
4. ⚠️ **`SetScriptHeight` 的 `CapOverGlyph` 是 Cjk 专用比值**（`Label.cs:361-368`），
   而 `ErrorMessageBanner.cs:561-562 / 650-655` 是 **`SetFace(LatinCardBody)` 之后紧跟 `SetScriptHeight`** ⇒
   那一族的 px 会比意图**大 ~19–30%**（代数：`px × 0.8217 × Em(面)/Cap(面)`，Pragati = 1.30、Cjk = 1.00）。
   方案 A 后降到 ~1.19 —— **仍未对齐**，应单独记账（正确的比值是 `CapPerFontSize(f)/EmPerFontSize(f)`）。
5. 📌 `TmpFont.cs:260-263` 那句"三份拉丁字体这一项实测很接近 ⇒ 换面本身几乎不改变字形大小"
   —— 说的是 `CapPerFontSize`（0.0656/0.0685），**不是 `EmPerFontSize`**；真正差 9–11% 的正是后者。
   方案 A 落地时请把这段 doc 一起订正（打上 2026-10-17 更正痕迹，⛔ 只删不改成两句）。
