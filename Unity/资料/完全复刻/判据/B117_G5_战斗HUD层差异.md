# B117_G5 · 战斗 HUD 层差异（第一份）

> 🔴🔴 **2026-11-02 就地订正（`G10-04` 现核 · 铁律 5）：下面那份 7,889 行的 `.tsv` 的「我们值」列【不是我们的值】。**
> 它取自 `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`，而那份是**原版自己跑的运行时 dump**
> （表里 0 命中我们独有的 `HandLabel` / `MyPileLabel` / `EndTurnButton`，却有 `CardsPlayedInTurn1` / `Player Deck Size Tex`）。
> ⇒ 那 7,359 行「一致」是因为**两边都是原版**。它**仍有用**（它证伪了 `Player Deck Size Container.sizeDelta` 那两条「不一致」只是 `AspectRatioFitter` 在运行时把高撑成 59.1/52.05），
> 但 ⛔ **不能当「我们 vs 原版」的基线**。要「我们 vs 原版」必须用**我们侧**的 dump（`Editor/NodeDump.cs` + `prefab_layer_diff.py --ours-dead-cols ""`）。

工具 = `d:/4/Unity/工具/prefab_layer_diff.py`（可复跑，裸跑只打用法）
表 = `判据/B117_G5_战斗HUD层差异.tsv`（7 列 · 7,888 行 · 1.9 MB） · 报告 = `报告/B117_G5_层差异工具.md`

## 0. ⚠️ 先说清「我们侧」是什么

简报把 `runtime_ui_dump_*.tsv` 写成「我们的侧」。**它其实是原版游戏自己的运行时 dump**
（`SceneJumpShot` mod · 2026-08-25 · 根节点 `BattlePrefab` · 与 `Battle Arena 1.unity` 同一棵树）。
⇒ 本表 = **原版静态 YAML ↔ 原版运行时 dump**，不是「原版 ↔ 我们的实现」。
它仍必须先做：① 校准解析器（两条互不相干的源对上 **1081/1081**）；② 交出**原版层基线**（`原版值` 列 = HUD 全树的尺子）；③ 列出「静态值不能当判据」的清单（§2 全部 26 条都是运行时被改掉的）。真「原版 ↔ 我们」还差一份我们侧的树 → 报告 §五。

## 1. 规模

原版 HUD 子树：静态 **1081** 节点 · 运行时 **1090** · 路径对齐 **1081 对上 / 0 仅原版有 / 9 仅我们有（运行时新建）**
属性比对 **7,825** 条：**一致 7,359 · 不一致 26 · `?` 440**
同名兄弟 `Glow Orange` ×4 → 按内容配对（不按次序），已消掉 4 条假差异。

## 2. 按属性聚类（全部 26 条「不一致」）

| 属性 | 条数 | 成因（已逐条核） |
| --- | --- | --- |
| `anchoredPosition` | 10 | 运行时脚本定位：Safe area 的 Left/Right Anchor、EnemyInfo/PlayerInfo、两个 Deck、SettingsBtn、攻击选择器两颗钮 |
| `activeSelf` | 9 | 该帧真实显隐：LoadingMenu 在、Drag Attack Selector 不在、Pause 在 Play 不在、TitleBackground/OvertimeIndicator 不在 |
| `sizeDelta` | 6 | CanvasScaler 撑满屏 + 按文字高度自适应的两个 TutorialTip 底板 + 两个 Player Deck Size Container |
| `pivot` | 1 | 只有 `Canvas`（静态 `0,0` → 运行时 `0.5,0.5`） |

## 3. 最该记住的 10 条

1. **`Canvas` 的 `pivot`/`sizeDelta`/`anchoredPosition` 静态全是 0**，运行时被 CanvasScaler 改成 `0.5/0.5` · `1920×1080` · `-343,0` ⇒ 抄 HUD 时 **⛔ 这三个静态值不能当判据**。
2. `LoadingMenu/Panel`：静态 `activeSelf=False` → 运行时 `True`，高度 10394.9 → **10061.1**。
3. `Drag Attack Selector` 及 `Select Active Skill Button` / `ValueText`：静态 `True` → 运行时 `False`。
4. `ReplayButtons/Holder`：`Pause` 静态 `False`→`True`，`Play` 反之（一对互斥钮）。
5. `EnemyInfo/EnemyName/TitleBackground` 与 `PlayerInfo/PlayerName/TitleBackground`：静态都在，运行时都关。
6. `Energy And turn holder/OvertimeIndicator`：静态 `True` → 运行时 `False`。
7. `Left Anchor` / `Right Anchor`：静态 `0,0` → 运行时 `-449,0` / `476,0` ⇒ 两块 Safe area 的横向锚点是脚本算的。
8. `PlayerDeck` / `EnemyDeck`：静态 `-190,0` / `-217.7,0` → 运行时 **`-190,-461`** / **`-190,461`** ⇒ 上下镜像由脚本给。
9. `TutorialTip/Generic Popup Background` 与 `/TipText`：静态高 `0` → 运行时 `110` / `90` ⇒ **底板高度按文字量**。
10. **运行时比静态多 9 个节点**：`Error Mensage_Ref(Clone)` ×5（错误提示**池**，开局就建 5 个）+ `New Game Object` ×4（挂在几处 TMP 下，脚本 `new GameObject()` 的产物）⇒ 复刻这两条链要自己建。

## 4. 这次**没法比**的属性（如实写，⛔ 不是「一致」）

`text`（dump 324/324 全空）· `fontSize`（324/324 全 `0.0`）· `localPosition`/`localScale`/`localEuler`（dump 不写）·
`fontColor`/`imageColor`/`imageType`/`renderQueue`/`raycastTarget`/`sortingOrder`（dump 不写）。
（`text`/`fontSize` 根因推测：`GetField("m_text")` 不爬基类，而这两个字段声明在 `TMP_Text` 上。高置信、**未在运行时验证**。）

## 5. 复跑

```bash
python -I -X utf8 d:/4/Unity/工具/prefab_layer_diff.py \
  --orig "<原版 .unity|.prefab>" --root-orig BattleHud \
  --ours "<.tsv|.unity>" --root-ours BattleHud \
  --out <tsv> [--summary <md>] [--only-diff] [--props all] [--dry]
```
