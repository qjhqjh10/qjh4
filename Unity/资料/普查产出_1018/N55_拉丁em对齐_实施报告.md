# N55 · 拉丁面字号口径对齐原版 em（0.0948 → 0.1）实施报告

> 执行代理，2026-10-18。**没有跑 Unity 批处理、没动 git、没改正本。**
> 判据 = `资料/普查产出_1017/诊断_第四十八会话/R4_Rewards残差.md` §五·B ＋
> `D1_桶A字号换算链.md` §五·2 / §四 方案 B（B(a) 生产重放 + B(b) 断言换口）。
> 用户口径：2026-10-17 拍板方案 B，2026-10-18 再次点名先做 `T07`。

---

## 一、结论（一句话）

`TmpFont.EmPerFontSize` 从「所有面恒 0.0948（汉字墨高）」改成「**`Face.Cjk` 仍 0.0948（逐位不变）· 三个拉丁面 = 原版真 em 0.1**」，
并补上方案 B 的另外两半（`Label` 面翻转时重放自适应四格 · 自检按 `ActualFace` 折 px）——
**CJK 面（全仓 510+ 站点基线）逐位零变化**，拉丁面实渲 em 从 `1.052 × 名义 px` 回到 `1.000 × 名义 px`。

---

## 二、改动清单

### A. 生产侧（两处共用件）

| 文件:行 | 改前 | 改后 | 说明 |
|---|---|---|---|
| `Core/TmpFont.cs:245-258` | `EmPerFontSize(f) => WorldGlyphPerFontSize`（恒 0.0948） | `f == Face.Cjk ? WorldGlyphPerFontSize : TrueEmPerFontSize` | 本账的核心 |
| `Core/TmpFont.cs:260-265` | ——（新增） | `public const float TrueEmPerFontSize = 0.1f` | 判据 = TMP `fontScale = fontSize/pointSize × faceInfo.scale × 0.1`（`TMP_Text.cs:7183`，本机现读）· 4 份字体资产 `m_FaceInfo.m_Scale` **全是 1**（2026-10-18 现读 `Resources/Fonts/*.sdf.asset`） |
| `Core/TmpFont.cs:48-54 / 57-63` | 「拉丁面零消费者」「一个 Noto 打天下」 | 加 2026-10-18 订正痕迹 | 铁律 5：现读 `Face.Latin*` = **486 处（生产 462 处 / 85 文件，Editor 24 处）** ⇒ 原句已过期 |
| `Core/TmpFont.cs:267-275` | 「真正会变的量在**行盒**（见 `EmPerFontSize`）」 | 补注：本函数（大写墨高）不随面变、`EmPerFontSize` 自本日起按面变 | D1 §六·5 点名的误导句 |
| `Battle/Label.cs:400-415` | `CapOverGlyph` doc：拉丁面比值 = 0.692 / 0.7226 | 补 2026-10-18：拉丁面比值改为 `Cap ÷ em` = **0.656（Pragati）/ 0.685（Asar）**；语义 = 拉丁路保证「em = px」、CJK 路保证「汉字墨高 = px」 | 纯 doc，值随 `EmPerFontSize` 自动变 |
| `Battle/Label.cs:466-479` | `SetScriptHeight` doc / 行尾注释「大写高 = 汉字墨高 × Wcap/Wglyph」 | 同上口径订正 | 纯 doc |
| `Battle/Label.cs:881-889` | 静态 `FontSizeToPx` doc | 注明本口是 **CJK 那一支**；量拉丁面标签必须用 `lb.FontSizeToPxNow(x, lb.ActualFace)` | 防下一个会话再踩 |
| `Battle/Label.cs:918-925` | `FontPxNow` doc（= × WorldGlyphPerFontSize × 108） | 改成「按 `ActualFace` 的 em × 本档尺」 | 纯 doc |
| `Battle/Label.cs:98-117` | ——（新增） | 自适应四格记录：`_fitW/_fitH/_fitMinPx/_fitMaxPx/_fitBasePx/_fitFace/_fitValid/_fitReplaying` | **方案 B(a)** |
| `Battle/Label.cs:1211-1216` | `SetAutoFitBox` 末尾只写四格 | 末尾**记下实参 + 写入时的 `ActualFace`** | 方案 B(a) |
| `Battle/Label.cs:1218-1234` | ——（新增 `ReplayFitForFace()`） | 面翻转时按记下的实参重放 `SetAutoFitBox`（先清 `_fitValid`，防止重放早退后留旧记录） | 方案 B(a) |
| `Battle/Label.cs:519-522` | `SetWrapWidth` 不碰 `_fitValid` | 单独改折行宽 ⇒ **作废旧的四格记录**（否则重放会把框宽拨回旧值） | 方案 B(a) 的安全闸 |
| `Battle/Label.cs:1417-1420` | `SetTextTmp` 只 `SyncFace → text → fontSize` | `SyncFace → text →`（**面变了就先重放四格**）`→ fontSize → ForceMeshUpdate` | 方案 B(a)：战斗 HUD 那 27 条「先按拉丁建、后填汉字」的件（D1 §2·3）不再小 5.2% |

**量化（拉丁面，同一名义 px）**：
`fontSize` ×1.05485（0.1 ÷ 0.0948）· **实渲 em 1.05485 → 1.000 名义 px** ·
`FontSizeToPxNow` / `FontPxNow` 的读数**语义**从「CJK 墨高 px」变成「原版 `m_fontSize` 的 em px」（数值仍 = 名义 px）·
`CapOverGlyph` Pragati 0.6922→0.65625 · Asar 0.72257→0.684674。

### B. 断言侧（只改「量拉丁面标签」的那些；CJK 目标逐位不变）

| 文件:行（改后） | 改前 | 改后 |
|---|---|---|
| `Editor/ShopScene.cs:631-636` | `RulerFontSize(host, fontPx)` 恒按 CJK 建标尺 | 加 `face` 形参，`MenuDraw.Text(..., face: face)` |
| `Editor/ShopScene.cs:2943-2951` | `RulerFontSize(_offerRuler, 42f)` 等 7 条 | 全部传 `TmpFont.Face.LatinCardBody`（`OfferContainer` 那 6 颗的来源面） |
| `Editor/ShopScene.cs:2094-2103` | 静态 `Label.FontSizeToPx(priceLb.FontSizeMin/Max/Base)` | `priceLb.FontSizeToPxNow(…, priceLb.ActualFace)` |
| `Editor/ShopScene.cs:2170-2175` | 同上（`WebShop Button/Button Text`） | 同上 |
| `Editor/ShopScene.cs:3072` | `Label.FontSizeToPx(lbName.FontSizeMax)` | `lbName.FontSizeToPxNow(…, lbName.ActualFace)` |
| `Editor/ShopScene.cs:3092-3093` | 同上（`lbTimer`） | 同上 |
| `Editor/SettingsScene.cs:361-374` | `CheckAutoFit` 的 min/max/base（静态口） | `lb.FontSizeToPxNow(…, lb.ActualFace)`（本窗输入框 = `LatinCardBody`） |
| `Editor/SettingsScene.cs:4621` | 全窗扫描 `Label.FontSizeToPx(lb.FontSizeBase)` | 同上（CJK 标签读数逐位不变） |
| `Editor/SettingsScene.cs:4649-4650` | 同上（`lo/hi` 两格） | 同上 |
| `Editor/MainMenuScene.cs:1575-1581` | `Resource QuantityText` 的 42/10（静态口） | 同上（该颗 = `LatinCardBody`，`Shell/TopBar.cs:547`） |
| `Editor/MainMenuScene.cs:3573-3580` | A1126 通用助手 min/max/base | 同上 |
| `Editor/MainMenuScene.cs:4140-4146` | `Completed Text` min/max/base | 同上 |
| `Editor/MainMenuScene.cs:5959-5965` | `Deck Name/Deck Warlord` 18/72/36 | 同上 |
| `Editor/MainMenuScene.cs:13519-13524` | 通用 `CheckFontWindow` | 同上 |
| `Editor/MainMenuScene.cs:13542-13543` | 通用 `CheckFontBase` | 同上 |
| `Editor/RewardsScene.cs:6728-6732` | A459 `FontSizeBase`（静态口） | 同上（标题来自 `MenuWindowBase` = `LatinUi`） |
| `Editor/RewardsScene.cs:7489-7495` | A468 族 min/max/base | 同上 |
| `Editor/RewardsScene.cs:7723-7729` | `Streak Failed/Button Text` 10/55/12 | 同上（`LatinCardBody`） |
| `Editor/RewardsScene.cs:7940-7941` | A968 `Window Title` base 36 | 同上 |
| `Editor/RewardsScene.cs:9819-9820` | A468 `m_fontSizeBase` | 同上 |
| `Editor/RewardsScene.cs:10106-10107` | A459 `m_fontSizeBase` | 同上 |
| `Editor/RewardsScene.cs:11571-11582` | 通用 A1226/A1219 助手 min/max/base + `lo/hi` | 同上 |
| `Editor/DeckScene.cs:7098-7101` | 类型族上限 27 | 同上（筛选格 = `LatinCardBody`，`DeckRuntime.cs:7243/7494`） |
| `Editor/DeckScene.cs:7129-7132` | 稀有度族 27 / 36 | 同上 |
| `Editor/DeckScene.cs:7168-7177` | 卡背页 `Owned only` 26 / 32 | 同上 |
| `Editor/CollectionScene.cs:6958-6968` | CardDetailPopup `Counter/Slash/Duplicates` min/max | 同上（`Shell/CardDetailPopup.cs:1398-1424` = `LatinCardBody`） |
| `Editor/CollectionScene.cs:7020-7022` | CardDetailPopup `m_fontSizeBase` | 同上 |
| `Editor/CollectionScene.cs:7172-7178` | `sgLb` 35/25/36 | 同上 |
| `Editor/ShellScene.cs:7183-7196` | A1126 通用助手 min/max/base | 同上 |
| `Editor/BattleScene.cs:23419-23426` | 单实参 `FontSizeToPxNow(x)`（内部写死 `Face.Cjk`） | 双实参 `(x, lb1192.ActualFace)`（本族 = `LatinUi`，uGUI 档） |
| `Editor/DeckScene.cs:4122-4126` | （注释）fontSizeMax 4.88 / 实得 4.65 | 补注：N55 后 ≈4.63 / ≈4.41，关系式含义不变、余量 ≈0.03 |

**刻意没动（目标是 `Face.Cjk` 的标签，换成按面口也是逐位相同——按「CJK 断言的断言不动」留着）**：
`Editor/DeckScene.cs:4168-4172`（`row_x*` = `TxtPx` 未换面）· `Editor/CollectionScene.cs:5295 / 6676-6679 / 7853-7872 / 8066-8081`（`CollectionWindow` 的筛选格/搜索框**没有** `SetFace`）· `Editor/SettingsScene.cs:1803-1816 / 4724-4736`（语言下拉 / VSync 行）· `Editor/SettingsScene.cs:4786-4815` 与 `Editor/BattleScene.cs:4939-5145`（探针，`Label.Create` 默认面）· `Editor/MainMenuScene.cs:14029-14042` 的 `NominalFontPx`（唯一调用点 `:10204` 的文本是汉字 ⇒ `ActualFace = Cjk`）。

### C. 判据文件里点名的「会因此改变行为的消费点」核对（任务 §一）

| 消费点 | 是否变 | 说明 |
|---|---|---|
| `Battle/Label.cs:1315-1316` `TmpFontSize()`（glyph 路） | **变**（仅拉丁面） | `_glyphHeight / Em` ⇒ fontSize ×1.05485，实渲 em 回到名义 px |
| `Battle/Label.cs:1316` `TmpFontSize()`（cap 路） | 不变 | `CapPerFontSize` 没动；上游 `CapOverGlyph` 见下 |
| `Battle/Label.cs:408` `CapOverGlyph` | **变**（仅拉丁面） | 分母 0.0948→0.1；`SetScriptHeight` 拉丁路落点从 1.055px 收到 1.000px |
| `Battle/Label.cs:907-910` `FontSizeToPxNow` · `:923` `FontPxNow` | **变**（仅拉丁面） | 读数 ×1.05485（读的是「真 em px」，比原来更接近原版语义） |
| `Battle/Label.cs:1211-1216` `SetAutoFitBox` 写的四格 | **变**（仅拉丁面） | 四格 ×1.05485；**实渲窗口上下限回到传入的 px** |
| `Battle/Label.cs:410-420` `GlyphHeightWorld` | 值变（仅拉丁面） | 但唯一消费者 `Battle/SkillPanel.cs:825` 经 `SetGlyphHeight` 往返，**结果不变量**（已代数核过） |
| `TmpFont.FontSizeForGlyphHeight` / `FontSizeForCapHeight` / `World*PerFontSize` | 不变 | 没碰 |
| `Label.FontSizeToPx(fontSize)`（单实参静态） | 不变 | 仍是 CJK 那一支（doc 已注明） |
| 462 处 `Face.Latin*` 生产站点（85 文件） | **全部受影响**（字号 ×1.05485 / 实渲 em ÷1.05485） | 逐件面不同，但**收敛方向统一**：实渲 = 原版 `m_fontSize` 的 em |

---

## 三、顺手发现（看到但没改）

1. 🔴 **工作树里另有一处不属于本账的改动**：`Unity/资料/任务线/界面线/任务总表.md`（`N50` 那一行被改写）——
   本轮**没有碰它**（我只读代码 + 改白名单里的 `.cs`），疑似别的代理在飞；请调度台合并时留意。
2. ⚠️ **同一族筛选格两个窗口的字体面不一致**：`Deck/DeckRuntime.cs:7243/7494` 显式 `SetFace(LatinCardBody)`，
   而 `Shell/CollectionWindow.cs:2490` 的 `TextAligned` 建同一族（`FilterPanelModel.Cell`）的标签**没有换面** ⇒
   卡组编辑窗用 Pragati、收藏窗用 Noto 渲同一批 `Common` / `Owned only`。
   像是 `A1024③` 没扫到的一处（`Shell/` 属黑名单，且改面是另一笔账）⇒ **只报不改**。
3. ⚠️ `Label.FontSizeToPxNow(float)` 单实参重载**默认 `Face.Cjk`**（`Label.cs:912-915`）——
   D1 §六·2 建议加 `[Obsolete]` 式注释；本轮只改了它唯一的 Editor 误用点（`BattleScene.cs:23419`），
   **没有**去改这个 API 的语义（怕影响别的宿主）。
4. ⚠️ `Editor/MainMenuScene.cs:14029-14042` `NominalFontPx` 仍用 `TmpFont.WorldGlyphPerFontSize × 108`（CJK 常数）。
   它唯一的调用点（`:10211`）那颗标题的文本是汉字 ⇒ `ActualFace = Cjk`、今天正确；
   但**换成 EN 语言**（拉丁文本）时会读小 5.2%。我**没改**（不在 D1 §四 B(b) 的清单里，且不知道该断哪一档）。
5. 📌 `Editor/DeckScene.cs:4122`（A1735⑥）的容差余量从「按 4.65/4.88」变成「按 ≈4.41/≈4.63」⇒
   **差值恒 ≈4.7%，落在 0.25 容差里只剩 ≈0.03**（见 §四·2）。
6. 📌 `TmpFont.cs` 文件头那段历史（`A1024③` 的 ①②③）里还有「今天全仓仍然是一个 Noto 打天下」一类措辞，
   本轮已就地打上 2026-10-18 订正痕迹（未整段重写，保留历史）。

---

## 四、没查清 / 判不准（逐条写缺哪条判据）

1. 🔴 **一律没有实跑**（红线：不许跑 Unity 批处理）。所有「这条断言会绿」都是**静态推导**，
   不是实测。**下一次同步点该跑**：`ShellScene` · `MainMenuScene` · `RewardsScene` · `ShopScene` ·
   `SettingsScene` · `DeckScene` · `CollectionScene` · `BattleScene`（`Core/TmpFont.cs` + `Battle/Label.cs` = 共用件 ⇒ 覆盖面最广的 8 条）。
2. ⚠️ **`Editor/DeckScene.cs:4122` 余量只剩 ≈0.03**（tolerance 0.25 vs 估算差 0.218）。
   判不准的原因是：`fontSizeMax` 与「被框轻压」的实得**都要 TMP 真跑**才知道（批处理）。若红，
   **⛔ 不许放宽容差**，正确修法是按 R4 §五 的思路把它也改成与字号无关的关系式（框宽 ↔ 行盒）。
3. ⚠️ **装不装得下 / 折行 / 溢出这一族没有逐条复核**：拉丁面实渲小 5.2% 会让「渲出来 ≤ 框」更容易过，
   但**任何拿「渲染宽/墨迹左缘」当字面量**的断言都可能位移（我只核了
   `CollectionScene:2435/2440/1539/6583` · `SettingsScene:2218` · `MainMenuScene:3014/4161` · `CollectionScene:7027`：
   它们要么是**上界**、要么是**对齐钉边**（宽度变、节点跟着挪 ⇒ 边缘不动）—— 判定为**安全**，但没有实测）。
   **剩下没逐条扫的那一类**（拿 `textBounds` 宽度当精确期望的）缺一次真跑才能确认。
4. ⚠️ **方案 B(a) 的重放没有实测**。风险点两条（都已做了静态防护，但没跑过）：
   ① `SetWrapWidth` 单独改宽后 `_fitValid` 作废 ⇒ 那些标签**会保持旧行为（差 5.2%）**；
   ② 若某站在 `SetAutoFitBox` 之后又调 `SetGlyphHeight`（没重设四格），重放会用**新 glyph 高 + 旧 px 窗口**——
      语义上合理，但该站的行为变化没有实测。
5. ⚠️ **`Face.LatinNarrow`（RobotoCondensed）零生产消费者**：按 0.1 折算没有实测对象（`m_Scale` 已现读 = 1）。
6. ⚠️ **`50.2901` 占位串到底是哪一串**（R4 §六·1 的遗留）不影响本账，仍未查。

---

## 五、typecheck 结果（最后一次，原文）

```text
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（命令：`TMPDIR=/tmp/wf_w1 bash d:/4/Unity/工具/typecheck.sh`）
