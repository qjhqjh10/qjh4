# B141 · W129 · 轮抽 Ongoing 页开战钮「要不要改回 `Battle Button Button`」

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 执行写手 `B141-W75`（任务号 `B141-DRAFT-BATTLE-STRUCT`）。
> 输入判据 = `报告/B141_W129_SelectedArt.md` §三 的 ❓1（W44）· `报告/B141_W129_Button506.md` §三 #8 ＋ §六 顺手发现 3（W52）
> ＋ **本写手对 `assets_full` / AssetRipper 导出 / 反编译的三源现读**（见 §一、§三）。
> ⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未改账主表 / 活状态表 / 所有权表。
> 写入 = `Shell/DraftModeWindow.cs` **一行注释就地改写（零行数变化）** ＋ 新建 `Editor/B141_W75_Assert.cs` ＋ 本文件。

---

## 〇、一句话结论

**不改。W44/W52 说的那颗 `Battle Button Button` 属于一份【没人引用的孤儿 prefab】，玩家看不到它；
活窗口里那一颗就是我们已经建好的 `To Battle Button`（`UI_Button_Mulligan` + 文字）。**
⇒ W44 §六 ❓1 的答案是「**不要补建 `CircleButton`**」；W52 §三 #8 那条「无落点」应改判为
「**判据本身错（选了孤儿 prefab 当判据）**」，不是缺口。

| 口径 | 值 |
|---|---:|
| 需要改动的 `.cs` | **0**（`DraftModeWindow.cs` 只改了一行注释、行数 4908 → 4908） |
| 波及的模式窗 | **0** |
| 新增断言 | **30 条**（`Editor/B141_W75_Assert.cs`，含 10 条前提） |
| `typecheck` | 运行时 **0/0** · 编辑器 **0/0**（两轮，末轮含注释改动） |

---

## 一、三份同名 prefab：哪一份在活树上（2026-11-04 现读）

`bundle_menus_assets_all` 里有 **3 份** `Draft Mode Ongoing State`（`GameObject/` 目录逐份比对，含父件反查）：

| # | 根 GO pathID（bundle） | 父件 | 里面的开战钮 | 谁在看它 |
|---:|---|---|---|---|
| A | `-8615549346732741910` | **`Draft Mode Timed Mode Window`**（RT `-7058060996923004182`） | `To Battle Button`（`UI_Button_Mulligan` · 440.33×120.60 · SpriteSwap） | ✅ **玩家看到的就是这一份**（轮抽窗实例化的是这扇窗） |
| B | `-1185220238903966882` | **无父件**（RT `5945483628974801758` 的 `m_Father = 0`） | `Battle Button Button ▸ Button`（绿 `m_ColorMultiplier 2` ColorTint）＋ `CircleButton`（`40k_UI_bt_play`） | ⛔ **孤儿**（见 §三），玩家看不到 |
| C | `-3337648187155318075` | `Draft Mode Menu Demo`（RT `-6078415646050387259`） | `To Battle Button`（同上） | 编辑器 demo 窗（原版调试用） |

> 口径提醒：AssetRipper 导出的 `Assets/Draft Mode Ongoing State.prefab` **只等于 B 这一份**（孤儿）。
> A / C 是**嵌在别的 prefab 里的拷贝**，要读 `Draft Mode Timed Mode Window.prefab` / `Draft Mode Menu Demo.prefab`。
> 🔴 **W44/W52 就是只读了 B 那一个导出文件**，没核「这一份实例挂在哪扇窗上」。

---

## 二、判据：活树上那颗钮逐字段 vs 我们

期望值**全部来自 prefab 字段**（`d:/2/tools/assetripper_out/ExportedProject/Assets/Draft Mode Timed Mode Window.prefab`），
⛔ 不是从我们的常量反推的：

| 字段 | 原版（行号 = 上面那份 prefab） | 我们（`Shell/DraftModeWindow.cs`） | 判定 |
|---|---|---|---|
| 节点名 | `To Battle Button`（`:4180-4194`） | `MenuDraw.Node(page, "To Battle Button", …)` `:1711` | ✅ |
| 父件 | 状态页根 `Draft Mode Ongoing State` | `page`（`"Draft Mode Ongoing State"`，`:1660`） | ✅ |
| RT | `anchor (0.5,0.5) · apos (715.9,−448) · sd 440.333×120.604`（`:30629-30656`） | `OnBattleR = (1455.70, 927.70, 1896.10, 1048.30)`（屏 px，y 向下）＝ 中心 (1675.90, 988.00)、半宽 220.17、半高 60.30 | ✅ 差 < 0.5px |
| Image | sprite `UI_Button_Mulligan`（guid `b5ad344d…`）· `m_Type 0`(Simple) · **`PreserveAspect 1`** · 白 · `RaycastPadding (0,0,0,0)`（`:30658-30686`） | `MenuDraw.Rect(..., Tex("UI_Button_Mulligan"), OnBattleR, "Bg", QArt, null, /*keepAspect*/ true)` `:1712` | ✅ |
| transition | `m_Transition 2`（SpriteSwap）· `ColorMultiplier 1`（`:30688-30746`） | `WindowButton` 换图那一路（`Bind(target, art, hoverArt, pressedArt)`） | ✅ |
| 悬停图 | `UI_Button_Mulligan_hover`（`4b35b9c7…`） | 同一串名字，`:1717` | ✅ |
| 按下图 | `UI_Button_Mulligan_Pressed`（`5c4b866c…`） | 同一串名字，`:1718` | ✅ |
| 选中图 | **`m_SelectedSprite = {fileID: 0}`（null）** · Disabled 也 null（`:30704-30705`） | **不传 `selectedArt`** ⇒ `_selectedTex = null` | ✅（**这条同时回答了 W44 的 ❓1**） |
| 点击音 | `m_onClickSound` = `ButtonClick Positive`（guid `5c7d1fc3…`） | `clickCue: WindowSounds.PositiveCue`（`= "ButtonClick Positive"`，`Shell/WindowSounds.cs:64`）` :1719` | ✅ |
| 文案 | `Button Text`：`m_text "Battle!"` · **`mTerm Demo/ToBattle`**（`:30794` 起） | `Loc.T("Demo/ToBattle")` `:1713` | ✅ |
| 字号 | `m_fontSize 74.25` · `Base 12` · auto[10, 74.25]（同处） | `74.25f` `:1714` | ✅ |
| 对齐 | `HAlign 2`(Center) · `VAlign 8192`(Capline) | 默认居中 ＋ `SetVAlign(Capline)` `:1715` | ✅ |
| 唯一 id | `GameObjectUniqueIdentifier.id = "PlayRankedButton"`（`Draft Mode Timed Mode Window.prefab` 的 GO 上） | 由 `Shell/MenuTutorialFlow.cs:875` 在解析点 `RegisterUnique("PlayRankedButton", toBattle)`（`B94_B18`） | ✅ 等价 |
| 状态脚本耦合 | `DraftModeMenuOngoingState.playButton: {fileID: 114421391099281046}`（`:27014`）**指着 `To Battle Button` 的 EverguildButton** | `_toBattleNode`（`OnOpen`/`Finished` 那两支 `SetActive`）；`DraftAssertD3.cs:520` 已断 | ✅ |

**也就是说：这颗钮我们已经逐字段对齐了活树那一份，没有任何一处要改。**

---

## 三、孤儿 prefab 的三条反证（为什么 ⛔ 不能照 `Battle Button Button` 改）

1. **根没有父件。** 三份同名根里，B 的 RT（`5945483628974801758`）在 `bundle_menus_assets_all/RectTransform/`
   全量扫 `m_Children` 后**查不到任何父件**；A 挂在 `Draft Mode Timed Mode Window`、
   C 挂在 `Draft Mode Menu Demo`（两份都是 `To Battle Button`）。
2. **全库 0 处 guid 引用。** `Draft Mode Ongoing State.prefab.meta` 的 guid = `b2f2be91d4e448b43809066fdd684cf0`；
   对 `d:/2/新解包资源/assets_full/` 全量（`--include=*.json`）grep ⇒ **0 命中**；
   对 AssetRipper 导出工程（除它自己的 `.meta`）grep ⇒ **0 命中**。
3. **反编译 0 处字符串引用。** `d:/2/tools/decomp_full/` 与 `cpp2il/out_il2csharp/` 里字符串
   `"Draft Mode Ongoing State"` / `"Draft Mode Timed Mode Window"` 都是 **0 命中**（窗口是按窗类型走
   `WindowsManager` 那一套，不是按名字 `Resources.Load`）。

⇒ **B 那份是「留着但没接线」的资产**。按铁律 11 例外①（原版本身就没有 / 玩家看不到）**不建**。

**旁证（正向）**：活拷贝 A 的状态脚本字段 `playButton` 指着 `To Battle Button` 的那颗 `EverguildButton`
（bundle MB `-7775495065151051030` 的 `playButton -> 4694025927165700842`；导出侧 `Draft Mode Timed Mode Window.prefab:27014`），
而孤儿 B 的 `playButton -> -8916591837734272162` 指的是 `Battle Button Button/Button`。
两边各自自洽，但**只有 A 会显示**；`DraftModeMenuOngoingState__OnEnable.c` 把 `playButton.OnClick`
接到 `PlayButtonOnClick`、`OnOpen` 的 `Finished` 档把它 `SetActive(false)` —— 与 A 一致。

---

## 四、改动面评估（按简报的阈值）

- **若照孤儿 prefab 改建**：要动 `Shell/DraftModeWindow.cs`（换节点结构 + 换底图 + 补 `CircleButton`），
  并会让 **3 处按名字找它的代码**语义错位（`Editor/DraftAssertD3.cs:520` · `Shell/MenuTutorialFlow.cs:872`（排位窗那一支按名找）·
  `Editor/ShellScene.cs:11186`），还要给 `Battle Button Button` 补一套新的四态。
  即便只算 `.cs` 也是 **≥ 3 个文件** ⇒ **超过阈值（>2 `.cs`）⇒ 本来就该停手**。
- **按现结论**：**0 个文件**（只有一行注释就地改写、零行数变化）。

⇒ 本单**不落地任何行为改动**，只落「判据订正 + 断言」。

---

## 五、两份报告为什么会判错（铁律 5 就地订正）

| 报告 | 原文 | 实际情况 | 错因 |
|---|---|---|---|
| `B141_W129_SelectedArt.md` §三 #32-33 | 「`Draft Mode Ongoing State ▸ Battle Button Button/Button/CircleButton` … ❌ 我们用 `UI_Button_Mulligan`（见 §三）」＋ §六 ❓1「要不要补建 `CircleButton`」 | 活拷贝里**没有** `CircleButton`，也没有 `Battle Button Button`；选中图 `m_SelectedSprite` 是 **null** | 只读了 `Draft Mode Ongoing State.prefab`（孤儿 B），没核「这一份挂在哪扇窗上」 |
| `B141_W129_Button506.md` §三 #8 ＋ §六 顺手发现 3 | 「原版这一级叫 `Battle Button Button` … 我们这扇窗的节点名可能是照 `Draft Mode Timed Mode Window` 取的」 | **反了**：`Battle Button Button` 才是那份**没被实例化**的；名字照 Timed Mode Window 取是**对的** | 同上；且把「两份 prefab 不一致」判成「我们的名字抄错了」 |
| 同报告 §三 #9/#10-11（`Debug…`） | 「不建（原版 `OnEnable` 强制 `SetActive(false)`）」 | **仍然成立** ✅（`DraftModeMenuOngoingState__OnEnable` 倒数几行对 `debugButton` / `debugPlayButton` 两句 `SetActive(0)`） | —— |

**附带订正（同族、顺手记一句）**：孤儿 B 的 `MainContent.localScale = 1.0`，活拷贝 A 是 **1.06**
（`MainContent` 的布局值在两份里也不同：`Game Mode Title` apos `(1.00, 339.55)` vs `(257, 308.55)`）。
我们的整页坐标一直是按 **1.06 的视觉值**摆的（`B101_R3` / `C5` 那两轮改的就是这个）
⇒ 🔴 **⛔ 别拿孤儿 B 重排版面**，否则 13 处坐标会整体小 5.7% 且逐件向左上漂。

---

## 六、断言：`Editor/B141_W75_Assert.cs`（30 条，含 10 条前提）

| 组 | 条数 | 盯什么 |
|---|---:|---|
| 前提 | 10 | `WindowsManager` · `DraftModeWindow.Create` · `ToBattleNode` · Ongoing 页上它是激活的 · `Button Text` · `Label` · `Bg` · `Hit` · `WindowButton` · `HitBoxForTest` |
| **A** 结构 | 13 | A1 节点名 · A2 父件 · A3 钮下无 `Battle Button Button` · A4 钮下无 `CircleButton` · A5 **整窗**都没有 `Battle Button Button` · A6 词条查得到 ＋ 文案 = `Demo/ToBattle` · A7 字号 ≈ 74.25px · A8 底图 = `UI_Button_Mulligan` · A9 常态图 · A10 悬停图 · A10 按下图 · A11 **不绑选中图** |
| **A** 版面 | 4 | A12a-d 命中区中心 (1675.90, 988.00) ＋ 宽 440.333 ＋ 高 120.604（±0.6 / ±0.8 px） |
| **B** 口径 | 3 | B1 悬停/按下图都取得到（按**这一颗**查缺图表，⛔ 不断全局表）· B2 点击音 = `ButtonClick Positive` · B3 没人在这一颗上绑孤儿那张选中图 |
| 合计 | **30** | 运行时条数（前提全成立时） |

**挂法**（同一物理行追加；⛔ 别插进 `//` 之后）：
```csharp
B141_W75_Assert.Run(); pass += B141_W75_Assert.Pass; fail += B141_W75_Assert.Fail;
```
判绿红按行首 `B141W75 ✓` / `B141W75 ✗` 数（⛔ 别 `grep -c ✗`）。
⚠️ 它会建一次 `DraftModeWindow`（与 `DraftAssertD3` 同一个夹具），收尾自动 `DestroyImmediate` 并还原
`RunForTest / PendingPlayMode / PendingSource / DraftPacks 缓存 / WindowsManager 复用缓存`。

**灭自证（🧨 什么坏法会让哪条红）**

| 改坏法 | 变红的断言 |
|---|---|
| 照孤儿 prefab 补建 `Battle Button Button` / `CircleButton` | **A3 / A4**（钮下）· **A5**（整窗） |
| 底图换成孤儿那颗绿钮的 `40k_bt_underbutton` | **A8** |
| SpriteSwap 的悬停/按下图换成 `40k_UI_bt_play` 一族 | **A9 / A10** |
| 给这一颗绑选中图（原版是 null） | **A11** |
| 文案改回硬编码英文 `Battle!` | **A6** |
| 字号改回 42（或拿别的钮那档） | **A7** |
| 挪位置 / 改尺寸 / 改父件 | **A12a-d / A2** |
| 删 `Resources/Art/ui*` 里那三张图之一 | **A8 / A9 / A10 / B1** |
| 点击音换成别的 cue | **B2** |

---

## 七、行数变化（相对本写手开工那一刻，非 `HEAD`）

| 文件 | 开工 | 现在 | Δ |
|---|---:|---:|---:|
| `Shell/DraftModeWindow.cs` | 4908 | 4908 | **0**（`:1710` 那行注释就地改写，加了 `✅ 已复刻（2026-11-04 · B141-W75 · …）`） |
| `Editor/B141_W75_Assert.cs` | —— | 新建 **215** 行 | +215 |

> `:1711~:1719`（那 8 行建法）**一个字节没动** ⇒ 账里若按 `DraftModeWindow.cs:1714` / `:1744` 的落点不受影响。

---

## 八、❓ / 给调度台的移交项

1. **账**：若 `账/*.tsv` 里有「轮抽那颗 `CircleButton` / `Battle Button Button` 未接」的 A 行，
   应判 **⛔「判据本身错」**（判据是孤儿 prefab）并就地改掉描述，**⛔ 不要补建**。本次未动账主表。
2. **W44 表要改一格**：`B141_W129_SelectedArt.md` 的 #32-33 两行（`Battle Button Button/Button/CircleButton` 与
   `Debug Battle Button Button/CircleButton`）应改判「**孤儿 prefab，不建**」——那两颗 `40k_UI_bt_play`
   属于没人实例化的那份 prefab，**应从 77 的分母里去掉**（精确重算值以那份表逐行为准；
   本写手未改那份文件，也⛔ 未擅自改它的分母）。
3. **`MenuTutorialFlow.cs:870-875` 的注释**现在写「原版 `playRankedButton.id = PlayRankedButton` → GO `To Battle Button`（menus 包实读）」
   —— 与本次结论一致，**不用改**（只是它没写清楚那 6 份实例全在模式窗、不含轮抽窗）。
4. **顺手发现（未做，⛔ 不在白名单）**：活拷贝那颗 `To Battle Button` 的 `Image.m_Material` 是
   `6ef1a6acedd47434d89c8087abfbcff3`（一份 UI 材质），我们用的是默认材质 —— 与「原版每颗钮都挂
   `EverguildButtonMaterialModifier`」那族是同一件账（若已由别的批覆盖请合并，别开第二条）。
5. **本次没有跑 Unity**：断言 30 条**一次都没执行过**（只过了 `typecheck`）。接线后第一次跑
   `DraftScene.Run`（或任一含 Draft 的宿主）时，请按行首 `B141W75 ✓` 数一遍；若 A8/A9/A10 红，
   先看 `Resources/Art/ui_menu/UI_Button_Mulligan*.png` 是否真在（现读：三张都在）。
