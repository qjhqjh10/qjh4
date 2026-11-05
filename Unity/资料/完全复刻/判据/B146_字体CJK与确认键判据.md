# B146 · CJK +5.5% + 确认键 auto-size（2026-11-05）

> 只读判据代理 `R4`。未跑 Unity / 未跑 typecheck / 未动 git / 未改任何源码·资产·账·表。
> 判据来源：原版场景 YAML（`d:/2/tools/assetripper_out/…/Battle Arena 1.unity`）、原版字体资产
> （`d:/2/新解包资源/assets_full/bundle_fonts_assets_all/`）、运行期 dump（`…panel_0914b/*.tsv`）、
> TMP 源码（`D:/Unity/Hub/Editor/6000.3.23f1/…/com.unity.ugui/Runtime/TMP/TMP_Text.cs`）、我们的 `TmpFont/Label/MenuDraw/CardChoicePanel`。

## 〇、一句话结论

1. **`+5.5%` 是「我们的基线错」，不是原版中文本来就高。** 它只是「汉字墨高 ÷ em = 0.948」这一半；
   把原版字体的 `m_Scale = 0.9` 一并算进去，**我们中文实渲比原版大 17.2%**（em / 汉字墨高 / 行盒三样同比例）。
2. `SetGlyphHeight(33.7)` 是**设死字号**，**不是**等价 auto-size。原版 `ContinueText` 是 auto-size：
   **中文收敛 33.7、英文收敛 45（上限）** —— 我们写死 33.7 ⇒ 中文大 17.2%、**英文小 25%**。
   最小改法 = `SetAutoFitBox(372.857, 43.622, 18, 45, 36)`（§五）。

## 一、`+5.5%` 逐层归因（数值表）

**口径**（uGUI 画布档，单位 = 设计 px；3D 世界档只是多一个 ×0.1，比值全同）：
`em = fontSize × faceInfo.scale`；`汉字墨高 = 0.948 × em`（TTF `'国'` 898/1000 + SDF padding 1.25×2）；
`行盒 = fontSize × lineHeight/pointSize × scale` = `fontSize × 1.437 × 0.9`（Noto）。

| 层 | 原版（输入 = `m_fontSize` F） | 我们（输入 = 喂 `SetGlyphHeight(F)`） | 比值 |
|---|---|---|---|
| TMP `fontSize` | `F` | `F ÷ 0.0853 ÷ 10`（`Label.cs:1510`+`:1497`）= **1.172 F** | **+17.2%** |
| TMP `fontScale`（`TMP_Text.cs:7183`） | `fontSize/50 × 0.9 × 1` | 同式、同资产同 pointSize | **×1.000（无贡献）** |
| em | `0.9 F` | `0.9 × 1.172F = 1.055 F` | **+17.2%** |
| 汉字墨高（含 padding） | `0.853 F` | `1.000 F` | **+17.2%** |
| 行盒 | `1.2933 F` | `1.516 F` | **+17.2%** |
| 基线偏移（`Label.VOffsetWorldNow`） | 与尺寸无关 | 与尺寸无关 | **0（无贡献）** |

**为什么 B146-B 写的是 5.5%**：那用的是 `0.0948 vs 0.1`（= 墨高÷em 的口径差，`1/0.948 = 1.0549`）。
但 ① `WorldGlyphPerFontSize` 自 2026-10-21（`A1262`）起随资产 `m_Scale=0.9` 实测为 **≈0.0853**（`TmpFont.cs:441-448`），
`0.1 / 0.0853 = 1.172`；② 原版 `m_fontSize` 是 **fontSize 单位**、不是 em（`em = 0.9×fontSize`）。
⇒ **现读的跨面差 = +17.2%，不是 +5.5%**；5.5% 只是其中「墨高 vs em」那一半，剩下 11.1% 是 `m_Scale=0.9`。

**三样本（原版值 vs 我们现值，单位 = 设计 px）**

| 样本（原版 `m_fontSize` F） | 原版 fontSize / em / 墨高 / 行盒 | 我们 fontSize / em / 墨高 / 行盒 |
|---|---|---|
| 确认键「继续」F=33.7（`choose_tree_forced.tsv` 第 14 列） | 33.70 / 30.33 / 28.75 / **43.58** | 39.49 / 35.54 / 33.70 / 51.06 |
| 42px 滑块标签（`BattleScene.cs:8519` 的 `px=42`） | 42.00 / 37.80 / 35.84 / 54.32 | 49.22 / 44.30 / 42.00 / 63.65 |
| 选牌标题「选择一张牌」F=55（`ChooseText`，auto 18–55） | 55.00 / 49.50 / 46.93 / 71.13 | 64.47 / 58.02 / 55.00 / 83.35 |

> ⚠️ 第 3 行是**通用 `SetGlyphHeight(F)` 口径**；标题生产代码现在走的是另一条 `SetCapHeight(39.6)`
> （实际 fontSize 56.5 / 60.4，见 §五 顺手发现 1/2）—— 两行不冲突，只是变换不同。

## 二、原版中文 / 拉丁字体与行盒（判据）

**原版字体分配**（`Assets/Resources/TMP Settings.asset`）：默认 `Pragati-Regular SDF`（guid `8185497b…`）；
全局 fallback = `RobotoCondensed-Regular SDF`（`14ecc461…`）+ **`NotoSerifCJK-Regular SDF`（`9d09a5b0…`，`:38`）**
⇒ **原版中文 = 运行期由 Noto 兜底**（不是另一份中文预制体）。

| 资产 | pointSize | `m_Scale` | lineHeight/pointSize | 有效行盒 | capLine/pointSize |
|---|---|---|---|---|---|
| NotoSerifCJK（中文兜底） | 50 | **0.9** | 71.85/50 = 1.437 | **1.2933 em** | 36/50 = 0.72 |
| Pragati（正文/按钮/卡面正文） | 95 | 1 | 90/95 = 0.9474 | 0.9474 em | 60/95 = 0.6316 |
| Asar（卡名/窗标题） | 94 | 1 | 110/94 = 1.1702 | 1.1702 em | 61/94 = 0.6489 |
| RobotoCondensed | 35 | 1 | 41.0156/35 = 1.1719 | 1.1719 em | 20/35 = 0.5714 |

原版侧判据 = `bundle_fonts_assets_all/MonoBehaviour/NotoSerifCJK-Regular SDF.json`（`m_FaceInfo.m_Scale 0.8999999…` ·
`m_PointSize 50` · `m_LineHeight 71.85` · `m_CapLine 36`）+ 导出工程同名 `.asset`；我们侧 = `Resources/Fonts/NotoSerifCJK-Regular SDF.asset:20-25`（**已对齐 0.9**）。

**运行期铁证（中文侧）**：`panel_0914b/choose_tree_forced.tsv`（同值 5 份）里 `ContinueText` = `继续`、`fontSize 33.7`。
框高 43.622 ÷ 1.2933 = **33.729** ⇒ TMP 自适应量化到 1/20（`TMP_Text.cs:4542-4545` 的 `(int)(fontSize*20+0.5)/20`）
⇒ **33.70**。这条等式同时证明了两件事：① 原版中文走 Noto 的 **1.2933 em 有效行盒**（`m_Scale` 真的乘在渲染上）；
② 原版中文的 **em = 30.33 px、墨高 = 28.75 px**，而 `m_fontSize` 仍是 33.7。

## 三、结论：修正式 / 或维持现状反证

**结论 = 我们的基线错（不是原版中文高 5.5%，也不是高 17.2%）。** 反证链：原版同一颗 TMP
拉丁档 `em = fontSize`（Pragati scale=1）、中文档 `em = 0.9×fontSize`（Noto scale=0.9）⇒ **原版中文比同字号的拉丁更小 10%**；
我们反而把 `m_fontSize` 当作「汉字墨高」再反折字号 ⇒ 中文 `fontSize = 1.172F`。二者方向相反。

**精确修正式（二选一，语义不同，⛔ 别混）**

- **首选（= 复刻原版 `m_fontSize`）**：让「px」= TMP `fontSize`，与拉丁面同口径：
  `TmpFont.EmPerFontSize(Face.Cjk)` 返回 **`TrueEmPerFontSize`（0.1）**，而不是 `WorldGlyphPerFontSize`（≈0.0853）。
  效果：`SetGlyphHeight(F)` ⇒ `fontSize = F`、`em = 0.9F` ⇒ 与原版逐位一致（`Label.cs:383` 一行）。
- **备选（不动共用件）**：逐站把实参折成原版墨高：`SetGlyphHeight(0.853 × F)`，**`minPx/maxPx/basePx` 同乘 0.853**。
- ⚠️ **与 `A1262` 报告建议的 (A) 不同**：那份写「换成 `0.1 × faceInfo.scale` = 0.09」（= em 口径）——
  那只修掉 5.5%，仍留 11.1%（因为原版 `m_fontSize` 不是 em）。要真对齐必须用 **0.1**。
- 影响面：全仓 **510+ 站点**（`A1262` §七·1）；CJK 实渲整体 ×0.853（−14.7%）。**必须真机并排验收后再落**。
- ⛔ 与另一件事别混：`B131` 审计的「我们缺省 `Face.Cjk` 渲拉丁 ⇒ 行盒 1.2933 让自适应偏小」是**拉丁文本用错面**，
  本条是**中文文本字号换算**；两条都要做、互不抵消。

## 四、确认键 auto-size 的原版字段（`Battle Arena 1.unity`）

节点 `ButtonsGroup/Continue Button/ContinueText`（GO **444** / RT **2741** / TMP **3994**）：

| 字段 | 值 | 出处 |
|---|---|---|
| `m_AnchorMin/Max` · `m_Pivot` | (0.5,0.5) · (0.5,0.5) | RT 2741（`:180230-180234`） |
| `m_AnchoredPosition` | (−4.60, −5.2628174) | 同上（父 = `Continue Button` 548.015×75.905、`a=(1,0)`、`ap=(−396.40,105.00)`） |
| `m_SizeDelta` | **372.857 × 43.622** | 同上（`:180233`） |
| `m_text` / 字体 | `Continue` / **Pragati**（guid `2548b731…`） | `:225328` / `:225330` |
| `m_fontSize` / `Base` / `Auto` | **45 / 36 / 1** | `:225355` / `:225356` / `:225358` |
| `m_fontSizeMin/Max` | **18 / 45** | `:225359-225360` |
| `m_HorizontalAlignment` / `Vertical` | **4 (Right)** / 512 (Middle) | `:225362-225363` |
| `m_TextWrappingMode` / `m_overflowMode` / `m_margin` | 1 (Normal) / 0 / 全 0 | `:225371` / `:225373` / `:225302` |
| 运行期（中文语档） | `继续` · `fontSize 33.7` | `panel_0914b/choose_tree_forced.tsv`（5 份同值） |

**auto-size 的输入** = 容器 rect（372.857×43.622）+ 零 margin + `[18,45]` + base 36；TMP 按行盒二分，
**中文卡在行盒（43.622/(1.437×0.9)=33.73→33.70）**、**英文卡在上限 45**（`45×0.9474=42.6 ≤ 43.622`）。

## 五、我们的最小改法与影响断言

**现状**（`Battle/CardChoicePanel.cs:506-509`）：`MakeText(..., scale=6, face: LatinCardBody)` 后只 `SetGlyphHeight(33.7f)`。
`SetGlyphHeight` = 定一个标称字号（`Label.cs:562-565` → `:1510`），**不开 `enableAutoSizing`、不设框、无 min/max** ⇒ 设死。
后果：英文 `fontSize = 33.7`（原版 45，**−25%**）；中文 `ActualFace` 翻 CJK（`Label.cs:1108-1111`）
⇒ `fontSize = 39.49`（原版 33.7，**+17.2%**）。⚠️ 所以 B146-B 判据 ③「`SetGlyphHeight(33.7)` 换回 fontSize 正好 33.7」
**只对拉丁文本成立**，中文那一档不成立，需就地订正。
⚠️ 现状还有一个附带偏差：这个 Label **根本没有框**（`MakeText` 只建节点，`sizeDelta` 由 `Label.CreateUgui` 置 0）
⇒ 没有折行、没有 `fontSizeMin/Max`；而原版 `m_TextWrappingMode = 1`（Normal）。`SetAutoFitBox` 一并补上这两样。

**最小改动（不动共用件，照抄同树的换牌面板先例）**

> 🔑 **现成先例**：`Battle/MulliganPanel.cs:946` 那颗 `MulliganDoneText` **就是同一套**——
> `SetAutoFitBox(DoneBoxW = 368.929f, DoneBoxH = 62.242f, DoneFsMin 18f, DoneFsMax 45f, DoneFsBase 36f)`
> （常量在 `:367-368`；注释明写「框 = **设计 px**（去掉 `U()`）」「放在 `SetDoneText` 之后 —— 那一句只提供起点」）。
> 选牌那颗只是**同一个机制缺了后半句**。

1. `CardChoicePanel.cs:1518` 常量区追加 `public const float ConfirmBoxH = 43.622f;`（类尾区，⛔ 别插到中间 —— 本文件 230 处账按行号引用）。
2. `:509` 一行替换：
   `if (_confirm != null) _confirm.SetAutoFitBox(ConfirmBoxW, ConfirmBoxH, 18f, 45f, 36f);`
   （`SetAutoFitBox` 见 `Label.cs:1293-1385`；uGUI 档 `worldW/worldH` = **设计 px** —— 同 `MulliganPanel.cs:915/946`。）
3. 保留 `face: LatinCardBody` 与 `ConfirmTextAnchor`(=1,0.5) 不变；`scale=6` 只提供标称起点（`cur`）。

**为什么能对上原版**：TMP 的自适应终点只由「自身行盒 × 框高」决定，与我们的 px 换算无关
（min/max 只夹边界）⇒ 中文落在 **33.70**（= 原版 dump 值）、英文落在 **45**。中文那一档的 min/max 折算后是
`[21.1, 52.7]`（CJK 面），**不夹住**收敛值，所以口径偏差在这里不显形 —— 这也解释了为什么这个站点特别适合先做。

**验收口径（建议新增断言，宿主 `Editor/BattleScene.cs` 的选牌面板那一节）**
- `_confirm.AutoSizing == true`；`FontSizeToPxNow(FontSizeMin/Max, ActualFace)` = 18 / 45；
- 中文文本下 **`_confirm.FontSize`（`Label.cs:1016`，裸 TMP `m_fontSize`）≈ 33.7 ± 0.1**；
  英文文本下 ≈ **45**；
- ⛔ **别断 `FontPxNow ≈ 33.7`**：修复后中文会读 **28.75**（= 原版墨高），那正是 §一/§三 的基线问题，不是回归。
- 灭自证：把 `SetAutoFitBox` 改回 `SetGlyphHeight(33.7)` ⇒ 英文那条从 45 掉到 33.7 ⇒ 红。

**会变的现有断言：查过了，0 条**（`grep` 全仓 `FontPxNow` 112 处 / `33.7` 1 处 / `_confirm` 只在 `CardChoicePanel`）。
- `B145_E6_Assert` A-3a/b/c/d 只读 `ConfirmBoxW` / `ConfirmRightEdgePx` / `ConfirmTextAnchor` —— 不动。
- `B142_E6_Assert` 的 B8/H 是 **Tooltip 正文**（`Tooltip.cs`），与选牌面板无关。
- 「42 条宽断言」在仓里查不到对应批次（42 出现在 `B104_A2` / `B117-G10-13` / 账务失败数，均无关）；
  全仓也没有扫 `Label.WorldW` 的通用宽断言覆盖到这个面板。

**顺手发现（不在本条白名单，只报不改）**
1. 🔴 **同族第二处**：选牌标题 `ChooseText` 也是 auto-size（`m_fontSize 55 / base 36 / auto 1 / [18,55]`，
   `:201208-201213`），我们 `:494` 用静态 `SetCapHeight(39.6)` 设死 ⇒ 中文 fontSize 56.5（原版 55，+2.7%）、
   英文 60.4（**+9.8%**）。同一个最小改法适用：`SetAutoFitBox(1344, 79.44, 18, 55, 36)`。
2. 🔴 **`TitleCapPerFontSize = 0.72` 取错了字体**（`:1505-1514`）：0.72 = `capLine 36/pointSize 50` 是 **Noto** 的；
   而 `ChooseText` 的 `m_fontAsset` 实读 = **Pragati**（guid `2548b731…`，`:201183`）⇒ 正确 cap 比例 = `60/95 = 0.6316`，
   `55 × 0.6316 = 34.74 px`，不是 39.6（+14%）。`B145_E6_Assert` A-2a/A-2b 钉的正是 0.72/39.6 ⇒ **会一直绿（自证）**。

## 六、没查清的

1. **没有原版中文的像素级实测**：`panel_0914b` 里能出 UI 的截图（`cap4_*` / `capE3_*`）在 ContinueText 区域
   全无亮像素（UI 相机纯黑那几张），所以 §一/§三 的 17.2% 是**算术 + 运行期 dump**，不是量图。
   ⇒ 落 §三 的全局修法前，必须真机并排一次中文 UI（建议拿选牌面板 + 设置窗各一处）。
2. 原版中文运行期走 Noto 是**从 `TMP Settings.asset` 的全局 fallback 静态推定**的（`:38`），
   没有原版中文语档的 `m_currentFontAsset` 现场 dump。「33.7 = 43.622/1.2933」这条数学吻合是强旁证，但仍非直读。
3. `A1262` §七·1 的账（CJK 口径 ≈17%）我没在 `活状态表` 找到对应行 —— 需调度台确认它是否还没立行（本条 `B146-CJK-BASELINE-55` 可能就是它）。
4. 标题 cap 那条（顺手发现 2）只核了 `m_fontAsset` 与 faceInfo，**没核原版英文实拍的 cap 像素**；若要动它需先拍一张英文语档的选牌标题。
