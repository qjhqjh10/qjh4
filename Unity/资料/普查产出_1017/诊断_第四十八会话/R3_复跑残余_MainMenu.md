# R3 · 复跑残余 —— MainMenuScene（只读诊断）

> 代理：只读诊断 R3。不改代码、不跑 Unity、不动 git。
> 日志：`d:/4/_tmp_view/menu.log`（**复跑已完成**，2026-10-17 21:2x 收尾）。
> 简报给的是「其余 6 条」，**实际列表是 7 条**（编号 ①–⑦），下面逐条处理。

## 〇、复跑现状（先回答「是否已消失」）

`_tmp_view/menu.log` 复跑收尾行：

```
[Menu] === 合计：3197 通过 / 7 失败 ===   （menu.log:68405）
```

7 条**全部仍在**，一条都没消失（对照第一跑 3189/14，已修的 8 条确认转绿）：

| # | 复跑日志行 | 文案 |
|---|---|---|
| ① | `:9218` | `顶栏的文字也跟着走（1 段排在 QBarText）` |
| ② | `:35166` | `中间列 Main Icon 的 x = 591.29`（差 1.3743 世界单位 = 148.42px） |
| ③ | `:37845` | `开战钮文案 = Battle!` —— 实测「战斗」 |
| ④ | `:37947` | ``Timer`` 里没有画任何东西 |
| ⑤ | `:53248` | ★ 弹窗整棵带子 —— 实得 `[3000,3326]` |
| ⑥ | `:53276` | ★ tooltip 每一块在窗之上 —— 实得 `tip=3000 > 窗=3326` |
| ⑦ | `:67937` | 标题框宽 `1337.600 ≈ 1337.670±0.050` |

---

## ① 顶栏文字没跟着走（1 段 vs ≥3）

**结论**：`Shell/MainMenuRuntime.cs` 的文字漏斗**先设队列、后换字体面**，换面把 TMP 材质整份换掉 ⇒ 队列回到默认 3000。与 `MenuDraw.TextCore` 本轮修的 G6 **完全同一个 bug**，只是 G6 只修了 `TextCore` 这一处，**同族的另外四个漏斗没修**。

**证据**
- 断言：`Editor/MainMenuScene.cs:990-995`（`labInBand >= 3`）；期望的 `QBarText = 2997`（`Shell/MainMenuRuntime.cs:356`、`Shell/TopBar.cs:110`）。
- 出错次序：`Shell/MainMenuRuntime.cs:152` `lb.SetRenderQueue(queue)` → `:155` `if (face != Cjk) lb.SetFace(face)`。
- 机制：`Battle/Label.cs:892-903` `SetFace → SyncFace`；`:906-912` `SyncFace` 里 `_tmp.font = want`；`Label.RenderQueue` 读的是 `_tmp.fontMaterial`（`:216`）。TMP 换 `font` 会连带换回该字体资产自带的共享材质 ⇒ `SetRenderQueue` 之前实例化的那份材质丢失，队列回到 3000。
- 数量自洽：`TopBar.cs:368/419/546` + `MainMenuRuntime.cs:1867/1876` 共 5 段要 `QBarText`，复跑只剩 **1** 段 ⇒ 4 段被压回 3000。中文那一段（`ActualFace` 回 CJK ⇒ `SyncFace` 不换字体）队列得以保留，正好解释「1」。
- 来源：`face:` 形参是 **A1024③（`3990ca6`，第四十七会话）** 批量加的；在此之前这些文字恒为 CJK 面、`SetFace` 根本没被调过。**不是本批（第四十八）引入。**

**分类**：β 实现缺陷（队列/换面次序），由第四十七会话 A1024③ 引入；本批 G6 只修了 `MenuDraw.TextCore`。
**修法**：见「合并建议 M1」的粘性队列（一次修掉 ①⑤⑥）。最小改法也可把 `MainMenuRuntime.cs:152` 那行挪到 `:155` 之后。

**没查清**：复跑没有逐段打印各 Label 的队列，具体是哪一段留在 2997 未直接读（推断为中文文本那一段）。

---

## ② 中间列 `Main Icon` 的 x

**结论**：**断言过期（α）**。原版那一格是 **HLG `align=4 (MiddleCenter)`** ⇒ 图标摆在行心；实现算出的 `591.29 + (355.44−58.6)/2 = 739.71` **与原版 dump 现读逐位相同**。`591.29` 是 2026-09-27 旧工具把「不该算进主轴的件」算进去之后的修正值，方向错了（它按左对齐算）。

**证据**
- 断言：`Editor/MainMenuScene.cs:5793-5795`（期望 `591.29…649.89`）。
- 实现：`Shell/RankedTab.cs:388-392`，`gx = 591.29 + (946.73−591.29−58.6)/2 = 739.71`。
- **原版现读**（本轮实跑 `menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`）：
  ```
  Global Rating           591.29,807.07→946.73,864.42   355.44×57.36  align=4
  Main Icon               739.71,807.07→798.31,864.42    58.60×57.36
  Individual rating value 798.31,…（0 宽）
  ```
  即 (355.44−58.6)/2 = 148.42 ⇒ 图标**居中**，与复跑报的差 148.42px 逐位吻合。
- 同类交叉验证（同一次 dump）：
  · `align=4` 的 `Legendary Counter/Rating Text`：图标左沿 − 行左沿 = 135.34 = (330.67−60)/2 —— 居中；
  · `align=3` 的 `Alliance Rating Display`：图标左沿 = 行左沿 —— 左对齐。
  ⇒ 「align=4 ⇒ 居中」这条规则在两个独立实例上都成立。
- `资料/普查产出_0927/档案窗_Ranking页与图名表.md:317-331` 的「Unity 实际」那一列是**用减法**（651.29−60）推的，隐含「左对齐」，与同一行旁边写着 `align=4` **自相矛盾**；该表值不可再当尺子（铁律 5 建议就地标注）。

**分类**：α 断言错（期望值来源过期）。
**修法**：断言改成 `739.71, 798.31`（x 一对）。y 先保持 `770.59, 827.94`（见「没查清」）。
**没查清**：新版 dump 的「激活后」布局把同一棵子树整体下移了 **36.48px**（`footer` 805.58..865.91 vs 我们 `FtT=732.62`；`Global Rating` 807.07..864.42 vs 我们 `770.59`）。`DivisionImage` 也差得很大（dump 421.65..805.58 ×1.2 vs 断言 599.34..732.62）。**这是独立于 x 的另一笔**，需要单开一条核 `center` 列 VerticalLayoutGroup 的解析结果 —— 本轮没查透，不作结论。

---

## ③ 开战钮文案 = `Battle!`

**结论**：**断言把英文写死了（α）**。实现走词条 `Demo/ToBattle`，中文档 = 「战斗」。原版那颗 TMP 的 `mTerm = Demo/ToBattle`、`m_text = 'Battle!'`（出厂英文），所以「战斗」是**对的**，断言该断词条而不是断 `m_text`。

**证据**
- 断言：`Editor/MainMenuScene.cs:6240-6241`（硬写 `"Battle!"`）。
- 实现：`Shell/LiveOpsEventWindow.cs:915-917` —— 注释写明 `mTerm = Demo/ToBattle`（en `Battle` · zh `战斗`），实际调 `Loc.T("Demo/ToBattle")`。
- 同文件**已经有正确写法**：`Editor/MainMenuScene.cs:2310-2312`（练习窗那颗：先断 `Loc.HasEntry("Demo/ToBattle")`，再断 `Loc.T(...)`），复跑日志 `:16720` 显示这条**绿**（实测「战斗」期望「战斗」）。⇒ 一处对、一处旧，**同一个文件里两套口径**。

**分类**：α 断言错。
**修法**：照 2310-2312 的写法改 6240-6241：
```
CheckTrue(Loc.HasEntry("Demo/ToBattle"), "（前提）词条 `Demo/ToBattle` 在表里");
CheckText(TextOf(… "Button Text"), Loc.T("Demo/ToBattle"),
          "开战钮文案 = 原版 TMP 的 `mTerm`（`Demo/ToBattle`；`m_text` 出厂是 `Battle!`）");
```

---

## ④ `Timer` 里「没有画任何东西」

**结论**：**断言过期（α）**。实现在第四十七会话的 `A1248` 里**改成真的画**了：`Timer Icon`（`WF_icon_clock`）+ 倒计时文字，位置按「跑过布局的 prefab 真值」摆、量到字宽后按 HLG 居中重排；截止时刻未知时由 `TimerDisplay` 写 `Termina en: --`。断言那两行仍是 A1248 之前的「只建空节点 + 出声」口径。

**证据**
- 断言：`Editor/MainMenuScene.cs:6270-6276`（`GetComponentInChildren<ImageQuad>() == null`）。
- 实现：`Shell/SkirmishEventWindow.cs:340-389`（`BuildTimer`；`:341-346` 就是 A1248 的就地说明；`:356` 建 `Timer Icon`；`:362` 建文字；`:386-388` 挂 `TimerDisplay`）。
- 兜底口径：`Shell/TimerDisplay.cs:128-133` —— `target == null` ⇒ 出声一次 + `_prefix + "--"`。
- 来源：A1248 在 `3990ca6`（第四十七会话）已提交，断言没跟着改。

**分类**：α 断言错。
**修法**：把 6275-6276 换成（判据 = A1248 + `TimerDisplay`）：
- `Timer Icon` 存在且贴图名 = `WF_icon_clock`；
- `Timer` 里那颗 `Label.Text == "Termina en: --"`（= `TimerPrefix + "--"`，截止时刻本地没有）；
- 可选：再断 `TimerIcon`/`Timer` 的 HLG 落位（`SkirmishEventWindow.cs:85-88` 有运行期真值）。
**没查清**：placeholder `Termina en: 23d 5h` 会不会在 `Attach` 之前被画出来一帧（批处理无帧循环，未能实测）。

---

## ⑤ 弹窗整棵带子 `[3000,3326]`

## ⑥ tooltip `tip=3000 > 窗=3326`

**⑤⑥ 同一根因**：**某几颗 Label 的队列被「换字体面」冲回 3000**。不是「队列没设」，也不是本批 `TextCore` 次序改动冲掉的（那一次改动方向是对的，只修了 `TextCore`）。

**证据（⑤）**
- 断言：`Editor/MainMenuScene.cs:10267-10280`（`pMin >= 3310`）。
- 实现口径：`Shell/TrophyInfoPopup.cs:79-94`（本窗 3310–3325）；创建时**次序是对的**（`MenuDraw.TextBox` → `MenuDraw.TextCore` 本批已把 `SetRenderQueue` 挪到 `SetFace` 之后，`:1932-1941`）。
- 真正的丢队列点在**之后**：`Shell/TrophyInfoPopup.cs:509-511`
  `_title.SetText("自检奖杯") / _desc.SetText("自检描述")`；
  `Battle/Label.cs:1331-1338` `SetTextTmp` 第一句 `SyncFace()`；`:883-912` 文本含汉字 ⇒ `ActualFace` 回 CJK ⇒ `_tmp.font` 换成 CJK 资产 ⇒ **材质整份更换，队列 3000**。
- 自检喂的就是中文（`Editor/MainMenuScene.cs:10148-10152`：`Name="自检奖杯"`、`Description="自检描述"`）⇒ 这两颗（LatinUi / LatinCardBody）必然掉到 3000。`_counter` 只有数字，不受影响。

**证据（⑥）**
- 断言：`Editor/MainMenuScene.cs:10289-10311`。
- `Core/Tooltip.cs:214-221`：`Label.Create(… "")` → `SetFace(LatinCardBody)` → `SetRenderQueue(QTipText=3607)`（次序**正确**）。
- `Tooltip.Show("自检提示（层带）", …)`（`Editor/MainMenuScene.cs:10289`）→ `_body.SetText(中文)` → `SetTextTmp → SyncFace` → 回 CJK ⇒ 队列 3607 被冲成 3000 ⇒ `tipMin=3000`。中文文本这一段是关键：英文文本不会触发回切。

**分类**：β 实现缺陷（`Label` 没有「队列粘性」），由 A1024③（`3990ca6`）给这些件指派拉丁面后暴露；**不是本批引入**，本批 G6 也覆盖不到（它只管创建那一刻的次序）。
**修法**：见 M1（`Label.SetRenderQueue` 粘性 + 换字体后重放）。最小改法也可在 `TrophyInfoPopup.Apply` 的 `SetText` 之后、`Tooltip.Show/Layout` 里补一次 `SetRenderQueue`，但那是逐站打补丁，不建议。
**没查清**：只拿到 `pMin`，没有逐颗打印弹窗里哪些 Label 是 3000（按代码只有 `_title` / `_desc` 两颗符合触发条件）。

---

## ⑦ 标题框宽 1337.600 vs 1337.670

**结论**：**实现常量把一个 2 位小数抄成了 1 位（β）**，断言是对的。原版 `Title` = `291.16 → 1628.84`（宽 1337.67）；我们的 `P1TitleR` 写成 `291.2 → 1628.8`（宽 1337.6），差 0.07px。中心不变（两边和都是 1920.0），所以只有宽度这一条红。

**证据**
- 断言：`Editor/MainMenuScene.cs:13184-13185`；期望来自 `Tooltip…` 现场 dump。
- 实现：`Shell/ParentEventWindow.cs:216` `P1TitleR = new PxRect(291.2f, 31.0f, 1628.8f, 131.0f)`。
- 原版现读（本轮实跑）：
  ```
  python 工具/menu_dump.py bundle_menus_assets_all "Two Sides Event Window" --depth 12 --md
  | ···3 | Title | 291.16,31.00→1628.84,131.00 | 1337.67×100.00 |
  ```
- 来源：`ParentEventWindow.cs` 与这条断言同在 `3990ca6`（第四十七会话 T28）落地，抄值时被四舍五入。

**分类**：β 实现缺陷（常量精度）。
**修法**：`Shell/ParentEventWindow.cs:216` 改成 `291.16f … 1628.84f`（宽 1337.68，±0.05 容差内；中心仍 1920.0，不动其它落位）。
**没查清**：同节 `P1TitleBgR`（465.8..1454.2）是否也有同类四舍五入 —— 没有断言、未核。

---

## 合并建议

### M1（一处修 3 条 + 一批潜在未爆弹）：`Battle/Label.cs` 加「队列粘性」
给 `Label` 存一个 `int _queue`：
- `SetRenderQueue(q)`：先记 `_queue = q`，再走现有实例化逻辑（`:229-249`）。
- `SyncFace()`（`:906-912`）：`_tmp.font` **真的换了**之后，若 `_queue` 有值就重放一次 `SetRenderQueue(_queue)`。
- `RebindAfterClone()`（`:1293-1319`）里的 `SyncFace()` 自动受益。

这直接修掉 ①⑤⑥；并且**不需要**再去记「队列必须在换面之后」这条次序纪律。

> **顺手发现 S1（比 ①⑤⑥ 更大）**：A1024③ 给**四个同族漏斗**都加了 `face:`，但都保留了 `SetRenderQueue → SetFace` 的旧次序，而本批只修了 `MenuDraw.TextCore` 一处：
> · `Shell/MainMenuRuntime.cs:152/155`（QBarText 2997）
> · `Shell/MainMenuSubmenuWindow.cs:323/326`（QText **3011**）
> · `Shell/DeckInfoPopup.cs:2259/2262`（`Q(q)`，基档 `QDI = 3120`）
> · `Shell/PracticeModePopup.cs:2283/2286`（`q`，如 `QPrText = 3107`）
> ⇒ 凡走这些漏斗且 `face != Cjk` 的文字，队列全部静默落到 3000，**低于它自己的面板**（「面板在、字不在」那一族）。M1 一样能救；如果不做 M1，这四个漏斗必须逐个把 `SetRenderQueue` 挪到 `SetFace` 之后。

### M2（`Editor/MainMenuScene.cs` 四处断言，建议一次改完）
- ② `:5793-5795`：x 改 `739.71 / 798.31`（并把 0927 表那句「§A·4 的修正值」标注为过期）。
- ③ `:6240-6241`：改走 `Loc.T("Demo/ToBattle")` + `Loc.HasEntry`（照 `:2310-2312`）。
- ④ `:6270-6276`：改成断 `Timer Icon`（`WF_icon_clock`）+ `Timer` 文本 = `Termina en: --`。
- ⑦：**断言不用改**；改实现（M3）。
- 顺手：② 的 y 与 `DivisionImage` 那 36.48px 疑点建议单开一条（见「没查清」）。

### M3（实现常量）
- `Shell/ParentEventWindow.cs:216`：`291.2/1628.8 → 291.16/1628.84`。

### 不做的事
- ② 的 y 暂不动（没有查透是「旧表过期」还是「我们真错」）；只改 x，留一条可验证的增量。
