# R2 · 第四十八会话复跑残余 —— Shop 19 红 + Rewards 8 红（只读诊断）

> 只读诊断代理 · ⛔ 没改任何 `.cs` / 场景 / 正本 · ⛔ 没跑 Unity · ⛔ 没动 git。
> 数据源：`d:/4/_tmp_view/shop.log`（19 红，全部同形）· `d:/4/_tmp_view/rewards.log`（8 红，另有 1 条主对话已改、本报告不覆盖）。
> 判据顺序：原版解包资源（`d:/2/新解包资源/assets_full/`）→ 反编译/源码 → 我们自己的自检宿主。

---

## 一、Shop：19 条 `name` 颜色（**一个根因**）

### 结论
`background/name-bg/name` 那颗 `Label` 的 `color` 实得 **`OfferContainer.LightCreamInk = (0.992, 0.868, 0.733)`**，
断言却拿 **`Color.white`** 去比 ⇒ `ColorEq` 差 0.008（容差 `1e-3`）⇒ 19 份逐份红。
**实现是对的，断言过期**：原版屏上颜色 = 顶点色（白）× 材质 `_FaceColor`，而 `_FaceColor` 不是白。

### 证据
- 断言：`Unity/MyGame/Assets/CardPresentation/Editor/ShopScene.cs:3087-3088`
  `CheckTrue(lbName != null && ColorEq(lbName.color, Color.white), … "原版**白**")`。
- 取对象：`ShopScene.cs:663-667` `LabelAt` → `ShopScene.cs:3010` 取 `background/name-bg/name`。
- 实现（传的就是**屏上色**，不是顶点色）：`Shell/OfferContainer.cs:334`（`LightCreamInk` 常量）·
  `:1106`（`LabelFit(..., LightCreamInk, NName, …, TmpFont.Face.LatinCardBody)`）。
- `MenuDraw.Text` 的 `color` 是**唯一种颜色**、直接落到 `Label.Create(..., color, …)`
  （`Shell/MenuDraw.cs:1890` / `:1925-1931`；`Battle/Label.cs:45` `public Color color`）——
  本工程**没有**「顶点色 × 材质 `_FaceColor`」这一层，所以传进来的必须就是**屏上色**。
- 原版判据（亲读）：`bundle_fonts_assets_all/Material/Material_122820007967670363.json`
  `m_Name = "Pragati-Regular Light Cream"`、`_FaceColor = (0.992157, 0.867587, 0.733333, 1)`；
  顶点色 `m_fontColor` 为白 ⇒ 屏上 = `(0.992, 0.868, 0.733)`。
  同口径见 `资料/普查产出_1017/UI/R_T07_剩余四项.md` §4·1 行 6（`Name` / `Timer Text` / `Price Display/text` = Light Cream）。
- 同批 `type` 那颗（`0.717`）**绿**，说明比较通道没走错、只是这一条的期望值停在了换色之前。

### 分类
**α 断言错**（期望值过期）。非本批回归：`LightCreamInk` 在 HEAD 里就已在用（`git diff` 这一行不在改动集内），
断言没跟着改。

### 修法
`ShopScene.cs:3087-3088` 改判据为**原版屏上色字面量**（⛔ 不要写 `OfferContainer.LightCreamInk` —— 那是被测实现）：
```csharp
CheckTrue(lbName != null && ColorEq(lbName.color, new Color(0.992f, 0.868f, 0.733f)),
          $"[{i + 1}] `name` 的**屏上色** = 原版材质 `Pragati-Regular Light Cream` 的 `_FaceColor` "
        + "(0.992,0.868,0.733) × 顶点白（实得 " + (lbName != null ? lbName.color.ToString() : "<没有 label>") + "）");
```
并把「原版**白**」那句换掉 —— 白是**顶点色**，不是屏上色（这正是本条红了一整轮的原因）。
顺手：消息里带上实得值（现在 `name` 这条不打印实得值，`type` 那条打印了）。

### 没查清
无。19 条 `✗` 全同形（`grep '✗' shop.log` 只有这一句），确认单根因。

---

## 二、Rewards

### ② · ③ `count 0` / `count 1` 框高 = 0（`rewards.log:4768` / `:5000`）

**结论**：`MissionsTab.BuildRewardCell` 末尾「A1289 重摆块」那一段在量完 `numW` 之后调
`cLb.SetWrapWidth(...)`，而 `SetWrapWidth` 会把 TMP 子节点的 `sizeDelta` **整体写成 `(width, 0)`**
⇒ **框高被清零**，且此后再没写回去（`SetAutoFitBox` 才是写高的那一步，它排在前面）。
不是 `TmpFont` 改动引起的，也不是「收敛算不出来」—— 是**框高真的变成 0**。

**证据**
- 失败：`rewards.log:4768`（登录卡 `count 0`）· `:5000`（骷髅卡 `count 1`），都是 `框高 = 89.29px（实得 0）`。
- 断言：`Editor/RewardsScene.cs:1845` / `:1852`（`boxH: 89.29f`）；测量在 `CheckA1212` `RewardsScene.cs:11534-11536`
  （`Mathf.Abs(tmp.rectTransform.sizeDelta.y) * k`）。
- 实现：`Shell/MissionsTab.cs:1935-1957`（`sideIconW > 0` 那一支）——
  `:1939` 量 `numW` → `:1955` `cLb.SetWrapWidth(LayoutSpace.Px(f2c.W))` → `:1956` `SetWrapping(false)` → `:1957` `AlignL`。
- 清零处：`Core/TmpFont.cs:498-503` `SetWrapWidthRect`：`t.rectTransform.sizeDelta = new Vector2(width, 0f);`
  （doc 明写「会把 sizeDelta 写成 `(width, 0)`」）。
- 为什么竖向档（每日行 `count 0`）绿：`sideIconW == 0` 走不进这一段（`MissionsTab.cs:1934`）。
- 为什么只在这两个站红：`BuildRewardCell` 只有「横向变体」那三个调用点（登录卡两格 / 骷髅卡一格）。
- 旁证：`rewards.log` 里 `[Missions]` 没有任何「量不出 preferred width」告警 ⇒ `numW > 0` ⇒ 这一段确实跑了。

**分类**：**β 实现缺陷**（A1289 那段，2026-10-17 起就在；`git log -S PreferredWidthPx` → `ed2bbe0` 第四十五会话引入）。
**不是本批回归**：本批对 `MissionsTab.cs` 的 diff 只有 4 行 `Loc.T("MainMenu/General/Collect")`。

**修法**：`MissionsTab.cs:1955` 那一句改成**同时把高写回去**那段；优先复用本文件现成的 `FitWindow`（`:460-466`，
它内部就是 `SetAutoFitBox(worldW, worldH, FS(min), FS(max), FS(base))`，会把宽+高一起写）：
```csharp
if (autoMinPx > 0f) FitWindow(cLb, c2, autoMinPx, autoMaxPx > 0f ? autoMaxPx : 40f, autoBasePx);
else cLb.SetWrapWidth(LayoutSpace.Px(f2c.W));
cLb.SetWrapping(false);   // SetAutoFitBox 内部仍会把折行开成 Normal ⇒ 这一句必须留在它之后
AlignL(cLb, c2);
```
⚠️ 两个残留风险（写手要知道）：① `SetAutoFitBox` 会再跑一次二分 ⇒ 收敛字号理论上可能与量 `numW` 时那一档有极小差；
② `autoMaxPx` 为 0 时 `Txt` 本来就不接自适应，此时用 `40f`（= 标称）兜底与旧行为一致。
判据不变：原版 `count` 的框高 = **整格高 77.643 设计 × 1.15 = 89.29 画布 px**（原版 `count` 的 RT 是
`aMin==aMax`、`sd=(0,0)`，高由父 HLG 的 `ctrlH=1 ∧ expandH=1` 给）。

**没查清**：无（不跑 Unity 无法验证修完的实得值，但因果链是闭合的：`SetWrapWidthRect` 逐字把 y 写 0）。

---

### ① 奖励块居中 −44.6px（`rewards.log:3832`）· ④ 登录卡第 0 格 81.93px（`:5619`）· ⑤ 两格间距 159.34px（`:5634`）
**这三条是同一个根因，且与 ②③ 不同**：`count` 那颗数字用的是 **CJK 字体面**，
`PreferredWidthPx` 量出来的 `numW` 比原版 Pragati 宽 **≈36%**，A1289 按它重摆块 ⇒ 块整体左移。

**证据**
- 断言：`Editor/RewardsScene.cs:1611-1616`（居中窗口 ±34.5px）· `:1921-1927`（第 0 格 102.94px）· `:1929-1934`（间距 138.33px）。
- 实参来源：`Shell/MissionsTab.cs:1910-1911`
  `Txt(parent, c, countText, Color.white, "count " + key, 40f, autoMinPx, autoMaxPx, autoBasePx, wrapOff: true)`
  —— **没传 `face`** ⇒ 落到 `Txt` 的缺省 `TmpFont.Face.Cjk`（`MissionsTab.cs:416-418`）。
- 原版判据（亲读 MB）：
  · 骷髅卡 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_1175879379365507769.json`（`m_text="200"`, fs40/base36/min10）
  · 登录卡 `…/MonoBehaviour_1234353403895984825.json`（`m_text="100"`, fs40/base36/min20）
  两份 `m_fontAsset` 都是 `m_PathID 3485036404935369831`、`m_sharedMaterial` 都是 **`-5050103597772954521`**
  = `bundle_fonts_assets_all/Material/Material_-5050103597772954521.json` → `m_Name = "Pragati-Regular Atlas Material"`
  ⇒ **原件是 Pragati（= `LatinCardBody`）**；同族 14 颗 `'100'` 数字逐颗同材质。
- 数值核对（三者自洽，可逐字复核）：
  · 骷髅卡：一格 325 设计、`sideAlignT=0.5`、`padL=0`、图标 55、间距 6
    ⇒ `dx = −4.52 − numW/2`（设计）⇒ `numW=0` 时 `−5.2px`（**注释里那个「按现在这一档 ≈ −5px」就是它**）、
    `numW=68.56` 时 `−44.6px`（**实得**）；原版 `numW=50.29` 时 `−34.12px`（= 断言窗口 34.5 的来历）。
  · 登录卡第 0 格：`sideAlignT=1` ⇒ 落点位移 = **整个 ΔnumW**：`Δ = 68.56 − 50.29 = 18.27` 设计 = **21.01px**
    ⇒ `102.94 − 21.01 = 81.93`（**实得**）。
  · 登录卡第 1 格：`sideAlignT=0` ⇒ 落点不随 `numW` 动，所以「两格间距」正好被撑大**同一个 21.01px**
    ⇒ `138.33 + 21.01 = 159.34`（**实得**）。三条读数互相咬合，根因唯一。

**分类**：**β 实现缺陷**（`count` 缺 `face:`，A1024③ 换面表里**根本没有 `count` 这几站** ——
`资料/普查产出_1017/UI/R_A1024_字体按用途普查.md` 的 `Shell/MissionsTab.cs` 一节只列了 `name` 两站）。
非本批回归。

**修法**：`MissionsTab.cs:1910-1911` 那句补 `face: TmpFont.Face.LatinCardBody`
（`Txt` 的 `face` 会经 `MenuWindowBase.TextBox` → `MenuDraw.TextBox` → `TextCore` 的 `SetFace` 落到 TMP，链路已核：
`Shell/MainMenuSubmenuWindow.cs:351-367` · `Shell/MenuDraw.cs:1940`）。
⚠️ **修完必须复跑确认 #1 的余量**：带上 Pragati 后理论值 `−34.12px`，而窗口是 **34.5px ⇒ 只剩 0.38px 余量**；
`PreferredWidthPx` 用的是 `GetPreferredValues()`（当前字号下），与原版 CSF 的取法未必逐位相同
⇒ 若仍红，先量实得的 `numW` 再决定是「宽 0.5px 的字体度量残差（可把窗口按实测残差改，并在断言里写明判据）」
还是真缺陷，⛔ 不要直接把窗口放宽了事。

**没查清**：带 Pragati 后 `GetPreferredValues()` 的实得值（要跑 Unity）。

---

### ⑦ `A516` 真渲染右缘 932.79 vs 935.0（`rewards.log:29315`）

**结论**：**两把尺子混用** —— 断言用 `TmpEdgePx`（mesh **墨迹**右缘），期望值 935.0 来自原版 **排版框**右沿
⇒ 差 2.21px 是末字（`n`）的**右侧边距**，不是摆位错。同文件里 2026-10-14 已经为 A517 修过**一模一样**的错。

**证据**
- 断言：`Editor/RewardsScene.cs:7507` `CheckNear(TmpEdgePx(moreN, true), 935.0f, 1.5f, …)`。
- 两把尺子：`:7307-7315` `TmpEdgePx`（`meshInfo.vertices` 的最大 x = **墨迹**）·
  `:7318-7330` `edgePx`（`tmp.textBounds` 四角 = **排版框**）。
- 同文件既有判例（逐字相同的情形）：`:7808-7821`「**两把尺子差一个『首字左边距』**…实得 `45.22` vs 期望 43.00
  ⇒ 实现没错、只是尺子换了」；那一批已改用 `edgePx(node, true)`。
- 期望值来源：同段注释 `:7493-7495` —— 原版 dump `245.5 990.8 → 935.0 1054.1`、`对齐=Right/Capline`；
  Right 对齐时 `textBounds.max.x` 落在框右沿 935.0，墨迹则少一个右边距。

**分类**：**α 断言错**（尺子与期望值不配对）。

**修法**：`RewardsScene.cs:7507` 把 `TmpEdgePx(moreN, true)` 换成 `edgePx(moreN, false)`
（⚠️ 两个助手布尔参**极性相反**，见 `:7821` 的告示）；判据仍是原版 dump 的右沿 **935.0**（`Right/Capline`）。
判别力不变：写成 `AlignLeft` 时右缘会变成「245.5 + 文字宽」。

**没查清**：无（改后是否正好 935.0 要跑一次；但机制与 A517 那条已验过的先例同形）。

---

### ⑧ · ⑨ `Daily Streak Timer Text` 期望 Pragati，实得 Noto（`rewards.log:29691` / `:29718`）

**结论**：**站点的 `face:` 接对了**（`Shell/DailyStreakPopup.cs:488` 明确传了 `LatinCardBody`）；
红的是**断言前提**：这一格的字**是中文**（日志里真实文本 = `2小时 34分钟`），
`Label.ActualFace` 的**汉字闸**把它强制折回 CJK，这是设计好的红线（拉丁资产没有汉字字形）。
断言里那句「纯拉丁文本，不触发汉字闸」不成立。

**证据**
- 断言：`Editor/RewardsScene.cs:7568`（`FontName(node) == "Pragati-Regular SDF"`）· `:7572`（`lb.ActualFace == LatinCardBody`）。
- 实现：`Shell/DailyStreakPopup.cs:488` `MenuDraw.Text(t, S_TimerText, DailyData.StreakTimerText(), …,
  face: TmpFont.Face.LatinCardBody)`；同段 `:469/:482/:581/:583/:585` 五站同面全绿 ⇒ 换面链路本身没问题。
- 文本是中文：`Shell/DailyData.cs:988-989` `StreakTimerText()` → `TimerDisplay.TextFor(...)`；
  `Shell/TimerDisplay.cs:216` `Zh = Loc.Current == Chinese`，`:218-223` 中文格式串（`"{0:0}小时 {1:0}分钟"`）；
  `Core/Loc.cs:149` `Default = AvailableLanguages.Chinese`（默认就是中文）。
  日志实证：`rewards.log` 里出现 `2小时 34分钟`（`grep 小时` 命中 4 次）。
- 汉字闸：`Battle/Label.cs:880-887` `_face != Cjk && Loc.HasCjk(_text) ⇒ Cjk`（注释写明「拉丁资产没有汉字字形，
  拿它们渲汉字会出一排方框，而且那是静默的」）。

**分类**：**α 断言错 + δ 夹具前提不成立**（断言假设了英文运行语种）。非本批回归。

**修法**（二选一，推荐 A）：
- **A（推荐，判别力不降）**：把这一条拆成两格 ——
  · 断**声明的面**：`Check(lb.Face, TmpFont.Face.LatinCardBody, "…指派面 = Pragati（原版 `m_fontAsset`）")`
    （`Battle/Label.cs:880` 有公开 `Face` getter）；
  · 断**实际面**：`Check(lb.ActualFace, Loc.HasCjk(lb.Text) ? TmpFont.Face.Cjk : TmpFont.Face.LatinCardBody,
    "…实际面（文本含汉字 ⇒ 汉字闸折回 CJK，判据 = 拉丁资产无汉字字形）")`。
  ⇒ 无论语种怎么变，这一条都还咬得住「漏传 `face:`」（漏传时 `Face == Cjk` ⇒ 红）。
- **B**：这一节临时把语言切到英文（`Loc.PersistOverride = true` + `SetLanguage(English)`，用完还原），
  让文本变成 `2h 34m` 再断 Pragati。⚠️ 会影响同节其它文案断言，风险比 A 大。

**没查清**：原版在中文语种下这一格的**回退字形**走的是 TMP fallback 还是整面切换
（我们没解 `m_fallbackFontAssetTable`）—— 但这不影响本条修法（我们自己的口径就是整面折回 CJK）。

---

## 三、同文件合并建议

| 文件 | 涉及条目 | 建议一次改完 |
|---|---|---|
| `Editor/ShopScene.cs` | Shop 19 条 | 只动 `:3087-3088` 两行（期望值 + 文案） |
| `Editor/RewardsScene.cs` | ① ④ ⑤（只改**期望/窗口**？——不，这三条**不改**，修实现后会绿）· ⑦ ⑧ ⑨ | 实际要改的只有 **⑦ `:7507`** 和 **⑧⑨ `:7568/:7572`**；①④⑤ 先改实现再看 |
| `Shell/MissionsTab.cs` | ② ③（框高清零）+ ① ④ ⑤（`count` 缺 `face:`） | **同一个函数 `BuildRewardCell`，一次改完**：`:1910-1911` 补 `face:`、`:1955-1957` 修框高。两处**互相独立**，但同文件同函数 ⇒ 一个写手一次做完最省 |
| `Shell/DailyStreakPopup.cs` | ⑧⑨ | **不需要改**（`face:` 已在 `:488`） |

**建议顺序**（同一批复跑一次 `RewardsScene.Run` + `ShopScene.Run`）：
1. `Shell/MissionsTab.cs`（一处补面 + 一处修框高）→ 预期消掉 Rewards ① ② ③ ④ ⑤；
2. `Editor/RewardsScene.cs`（改两把尺子 + 汉字闸）→ 预期消掉 ⑦ ⑧ ⑨；
3. `Editor/ShopScene.cs`（期望值换屏上色）→ 预期消掉 Shop 19。
⛔ 改 `MissionsTab` 时**不许**顺手把 ①④⑤ 的断言窗口放宽 —— 那是把「字体度量残差」当判据；
若带 Pragati 后仍红，先量 `numW` 再裁。

## 四、总表

| # | 位置 | 期望 | 实得 | 分类 | 改哪 |
|---|---|---|---|---|---|
| Shop×19 | `ShopScene.cs:3087` | `Color.white` | `(0.992,0.868,0.733)` | α | 期望值改屏上色字面量 |
| ① | `RewardsScene.cs:1611-1616` | ≤34.5px | −44.6px | β | `MissionsTab.cs:1910` 补 `face: LatinCardBody` |
| ②③ | `RewardsScene.cs:1845/1852` | 89.29px | 0 | β | `MissionsTab.cs:1955` 用 `FitWindow` 保高 |
| ④⑤ | `RewardsScene.cs:1921-1934` | 102.94 / 138.33 | 81.93 / 159.34 | β（同①） | 同上补 `face:` |
| ⑦ | `RewardsScene.cs:7507` | 935.0 | 932.79 | α（尺子） | 换 `edgePx(moreN, false)` |
| ⑧⑨ | `RewardsScene.cs:7568/7572` | Pragati / LatinCardBody | Noto / Cjk | α+δ（中文语种） | 改断 `Face` + 条件 `ActualFace` |
