# `A1076` — 换牌面板 `MulliganText/TurnText` 那一行的 rect（第六会话）

> 任务代号 `换牌那行 rect` · 白名单 = `Battle/MulliganPanel.cs` · `资料/加时与冲突模式_原版规格.md` · `资料/选牌_数据与规格.md`
> ⛔ 本会话**没跑任何 Unity 自检**（用户本轮口径）；只跑了秒级类型检查（自己的 `TMPDIR`）。

---

## ① 结论

**取到了。** 而且 **`MulliganPanel.cs` 的摆位早就已经是真值**（2026-09-29 那一轮改的），**本轮不需要改摆位**。

**任务简报的前提与现读不符 —— 以现读为准：**

| | 简报/文档说的 | 现读实际是 |
|---|---|---|
| rect 取到没 | `加时与冲突模式_原版规格.md:875` 写「**没取到**（不在那 988 个 RT 里，怀疑挂在预制体那边）」 | **就在那个包里** —— `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3403.json`（**它本来就是那 988 个之一**） |
| 代码摆位 | 「先贴着提示行下方摆，取到真 rect 之后要改」 | **2026-09-29 已按真 rect 改完**（`TurnCx/TurnCy/TurnW/TurnH`），断言在 `Editor/BattleScene.cs:11861` |
| 账在哪 | 「已记进 `项目任务.md` §〇」（空指针 → 补开成 `A1076`） | 属实 —— **`A1076` 是一条「已经做完、只是文档没跟上」的账** |

**「没取到」的错因（铁律 2 那条老坑的又一例）：** RT 类资产在解包里按 **PathID** 命名
（`RectTransform_<pid>.json`），**没有名字索引** ⇒ 按「TurnText」搜文件名**必然零命中**；
当年据此写成「不在那 988 个 RT 里、怀疑挂在预制体那边」= **把「搜不到」记成了「本地没有」**。
节点名在 `GameObject/TurnText_293.json`（它的 `m_Component[0]` 就是 `RT_3403`）。

---

## ② rect 真值（逐字段 + 父链 + 出处）

**父链**（自下而上，全部现读 `bundle_scenes_scenes_battlearena1/`）：

```
TurnText        GameObject PathID 293  · RectTransform 3403   ← 这一行
  └ MulliganText  GameObject PathID 319 · RectTransform 2828   （容器）
      └ Mulligan      GameObject          · RectTransform 3344   （整屏 1920×1080）
          └ Safe area FrontCanvas         · RectTransform 2862
              └ FrontCanvas               · RectTransform 2900
                  └ Canvas                · RectTransform 2759
```

**`TurnText`（`RectTransform_3403.json`）逐字段：**

| 字段 | 原版值 |
|---|---|
| `m_AnchorMin` / `m_AnchorMax` | `(0,0)` / `(1,1)`（**拉伸**） |
| `m_AnchoredPosition` | `(-0.971923828125, -49.999969482421875)` |
| `m_SizeDelta` | `(-36.93600082397461, -25.27199935913086)` |
| `m_Pivot` | `(0.5, 0.5)` |
| `m_LocalScale` | `(1,1,1)` |
| `m_LocalRotation` | 单位四元数 `(0,0,0,1)` |
| `m_Children` | `[]` |
| `m_Father` | `2828`（= `MulliganText`） |

**算出来的绝对矩形**（父 `Mulligan` = 整屏 1920×1080，故 1 单位 = 1 设计像素）：

| | 值 |
|---|---|
| 子件矩形 = `sd + 父矩形` | **1307.064 × 54.168** |
| 左上（y 从上） | **(312.50, 129.42)** |
| 右下（y 从上） | **(1619.56, 183.58)** |
| **中心（y 从上）** | **(966.028, 156.500)** |
| 父 `MulliganText` | 左上 **(295.00, 66.78)** · 1344 × 79.44 · **中心 (967, 106.5)** ← **不是 156.5**，别与子件混 |

**🔑 「就是这一行、不是别的」的判据（比按名字找强得多，本轮新做）：**
原版 `MulliganManager` 组件（`MonoBehaviour_4352.json`，挂在同级 GameObject `Mulligan` 上）的三个字段直接把节点钉死：

- `mulliganTurnTextLocalize` → MB **5183**，**正是 `TurnText`(GO 293) 身上那颗 `Localize`**（`mTerm = "Battle/Mulligan/secondTurn"`）
- `mulliganTextLocalize` → MB 5197（提示行那颗）
- `mulliganTextObj` → **GO 319 = `MulliganText` 容器本身**

**第二、第三份独立印证：**

| 来源 | 值 | 出处 |
|---|---|---|
| **静态**（13 个战场场景） | 逐字段相同。arena1 `RT_3403` · arena2 `RT_3598` · arena3 `RT_3516` · aeldari `RT_3364` · astra `RT_3567` · blacklegion `RT_3479` · darkangels `RT_3454` · emperorschildren `RT_3458` · genestealers `RT_3420` · leviathan `RT_3566` · sororitas `RT_3624` · spacewolves `RT_3423` · tauviorla `RT_4000` | 本轮 13 个 `bundle_scenes_scenes_battlearena*` 全扫 |
| **运行期实况** | `ap -0.97,-50.00` · `sd -36.94,-25.27` · **rect 1307.06,54.17** ⇒ **与静态逐位相同** | `资料/原版参照图/Unity参照管线_0825/data/panel_0914/runtime_rect_mulligan.tsv:13` · `panel_0914b/p3_mulligan_tree.tsv:15` |
| **不动画** | `grep -rl TurnText` 整个 arena1 包只 3 命中（`GameObject/TurnText.json` · `GameObject/TurnText_293.json` · `MonoBehaviour/MonoBehaviour_4352.json`）⇒ **`Animation/AnimationClip/Animator/AnimatorController/CanvasGroup` 一个都不引用它** | 本轮现查（包内 Animation 3 · AnimationClip 2 · Animator 2 · AnimatorController 1 · CanvasGroup 20） |

**其他字段（同一颗 TMP / Localize）：**

| 字段 | 值 | 出处 |
|---|---|---|
| `m_text` | `You go second`（英文兜底） | `MonoBehaviour_3856.json` |
| `mTerm` | `Battle/Mulligan/secondTurn` | `MonoBehaviour_5183.json` |
| `m_fontSize` / `m_fontSizeMin` / `m_fontSizeMax` | **55** / 18 / 55，`m_enableAutoSizing = 1` | `MonoBehaviour_3856.json` |
| `m_HorizontalAlignment` / `m_VerticalAlignment` | **2（Center）** / **512（Middle）** | 同上 |
| `m_fontColor32` / `m_fontColor` | **4294967295（纯白）** / `(1,1,1,1)` | 同上 |

**「没查清」的一格（如实）**：`TurnText_293` 的组件表里还有 **MB 2260**（文件存在但**内容为空/零字节**）与 **MB 3606**
（`maxFontSize: 55.0` / `minFontSize: 0.0`，像是个字号钳位器）—— 两者的脚本身份没查，**不影响 rect**。

---

## ③ 改动清单

**摆位一个字都没动**（它本来就是真值）。改的全是**记录**（铁律 5）：

| 文件 | 改了什么 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Battle/MulliganPanel.cs` | `TurnCx/TurnCy` 那段 doc 注释：① 补 `A1076` 现读复核（13 场 + 运行期 dump 数字）② 补 `MulliganManager` 三字段的「就是这一行」判据 ③ **订正**「父 `MulliganText` ⇒ 中心 (967, 156.5)」→ 父中心是 **(967, 106.5)**（156.5 是**子件**的中心，原文把两者混了）④ **订正**那句「已知未改：原版纯白、我们用的是暖色 (1,0.94,0.82)」—— **早已不成立**（2026-09-30 两行都改成 `Color.white` 了，本文件 `Create()` 里现读得到）⑤ 新增「字号 auto→实渲 41.8」那条（见 ⑥） |
| `Unity/资料/加时与冲突模式_原版规格.md` | 换牌那节（`:875` 起）：把「**没取到**」整段换成**收口** —— 取到在哪、错因、真值逐字段、13 场 / 运行期印证、`MulliganManager` 字段判据、以及「代码 2026-09-29 就已改完 + 断言在 `BattleScene.cs:11861`」 |
| `Unity/资料/选牌_数据与规格.md` | `:143` 那一行（本轮简报点名要核的那条旁证）：订正「**逐节点**同构」→ **面板层**同构；第三个元组标的是 **anchor** 不是 `pivot`；**子树并不同构**（`ChooseText` 只 1 个子件，`MulliganText` 有 2 个：`Text` + `TurnText`）⇒ **明确写死「本行的数只到 `MulliganText` 这一层，不是 `TurnText` 那一行」** |

**没动**：`TurnW` / `TurnH` 两个常量（只作记录用，全仓无引用）；代码逻辑；落位数值。

---

## ④ 验证

**秒级类型检查**（`TMPDIR=/tmp/wf_mull bash d:/4/Unity/工具/typecheck.sh`，改完 `MulliganPanel.cs` 后跑）：
```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

**行尾**（二进制读，改前 → 改后；三个文件**改前都是纯 LF**）：

| 文件 | 改前 CRLF/LF | 改后 CRLF/LF |
|---|---|---|
| `Battle/MulliganPanel.cs` | `0 / 564` | `0 / 582` |
| `加时与冲突模式_原版规格.md` | `0 / 891` | `0 / 908` |
| `选牌_数据与规格.md` | `0 / 692` | `0 / 692` |

（**没翻行尾**。⛔ 没用 `sed -i`，全走 Edit 工具。）

**`git diff --numstat`**：
```
26	8	Unity/MyGame/Assets/CardPresentation/Battle/MulliganPanel.cs
22	3	Unity/资料/加时与冲突模式_原版规格.md
1	1	Unity/资料/选牌_数据与规格.md
```
hunk 范围逐条核过：`加时` 只有 `@@ -875,3 +875,18 @@`（**全在换牌那节**）；`选牌` 只有 `@@ -143 +143 @@`；
`MulliganPanel.cs` 三处全在 `TurnText` 那段 doc 注释内（`177`/`182`/`186` 一带）。表格竖线数复核：`:143` = **4 条**（3 列），与上下行一致。

---

## ⑤ 没查清 / 停手的

**没有停手 —— 第一问有结论（取到了）。** 以下是如实标着的「没查清」：

1. **`MB 2260`（空文件）与 `MB 3606`（字号钳位器）的脚本身份**没查 —— 与本件（rect）无关，未展开。
2. 「我们自己原先放的那一版中心在 y=178」**无从复核**：该值只来自 2026-09-29 那轮的注释与断言文案（`BattleScene.cs:11874`），
   **代码里已找不到旧常量**（早被覆盖）⇒ 我在两处都改写成「该值今日已无从复核」，⛔ 没有当证据用。
3. **原版中文表仍然没有**（见 ⑥·A）—— 只拿到「运行期渲染出来的那两句」。

**搜过什么（审计留痕）**：`bundle_scenes_scenes_battlearena*` **13 个包全扫**（arena1 `RectTransform/` **988 个**，
其余 988–990）；`GameObject/` 按名 grep（`MulliganText` 1 命中 · `TurnText` 2 命中）；
`grep -rl TurnText` 扫**整个 arena1 包**（3 命中）；RT 父链按 `m_Father` 逐级上溯到 `Canvas`。
⚠️ **没有**去别的 `bundle_*` 找 —— **因为第一个包里就找到了**（本轮不需要「怀疑在预制体那边」那条路）。

---

## ⑥ 顺手发现的（都**没改**，只登记）

**A. 🔴 原版运行期的中文，和我们表里的中文**不一样**（这一条最值得开账）**

2026-09-14 那次**原版实跑**的节点树把 TMP 的 `text` 直接读出来了（`data/panel_0914b/p3_mulligan_tree.tsv`）：

| 节点 | 原版**运行期实渲** | 我们的 `Loc`（来源 = `数据/本地化/i18n/zh_CN.csv`） |
|---|---|---|
| `/MulliganText/TurnText` | **你是第二个行动**（fs 41.8） | `你后手`（`zh_CN.csv:198`，`Loc.cs:669`） |
| `/MulliganText/Text`（提示行） | **选择要在首轮替换的牌**（fs 53.7） | `选择首局替换的卡牌`（`zh_CN.csv:75`，`Loc.cs:671`） |
| `…/MulliganContinueButton/Text` | **继续**（fs 45.0） | `继续` ✅ 对得上 |

⚠️ `zh_CN.csv` **不是原版表**（定案在 `资料/全量反编译复核_靠推断的清单.md:96` —— 那是**我们自己 2026-08-27 译的**）
⇒ 上表左列**比右列更接近原版**（左列是原版客户端**真渲出来的字**）。
🔴 **但改不了**：`Core/Loc.cs` **不在本件白名单**（且被列为 ⛔ 禁区）⇒ **停手、只登记**。要做的话是另一条账。

**B. 原版中文表的下落（新线索，值得单开一笔）**
本轮在**本机**找到了原版的 Addressables **目录**：
`%USERPROFILE%/AppData/LocalLow/Everguild/Warpforge/com.unity.addressables/catalog_main.json`（1.7 MB · `m_InternalIds` **10490** 条 · 另有 `catalog_main.bin`）。
它**只有目录、没有内容**，但把那条链钉死了：
```
https://d03469d2-142f-4466-b3c4-e42115b76bac.client-api.unity3dusercontent.com/client_api/v1/environments/{…}/buckets/{…}/release_by_badge/{…}/entry_by_path/content/?path=/localization_assets_all.bundle
```
⚠️ **该 bundle 本机没有**（`find AppData/Local AppData/LocalLow -iname '*localization*'` 零命中；
`d:/2/unity_run_ref` 全目录按 UTF-8 搜「你是第二个行动」/「选择要在首轮替换的牌」**零命中**）
⇒ 2026-09-14 那两句中文多半是**当时临时下载/缓存**下来的，**现在已不在盘上** ⇒ **那份 dump 是现存唯一记录**，别删。

**C. 字号：原版是 autoSizing，实渲比我们小 ≈32%**
`TurnText` / `Text` 两颗 TMP 都是 `m_enableAutoSizing = 1`（18~55），**运行期实测渲染字号 = 41.8 / 53.7**；
我们写死的是 **55 / 43.67** ⇒ 先后手行**比原版大 ≈32%**、提示行**小 ≈19%**（方向相反）。
**本轮未改** —— 因为「改成 41.8」未必对：auto 的产物**随语种与字体资产变**，钉死一个数只对一种情况成立
（铁律 5·c：一个值 ≠ 全部情况）。**要做的话要先定口径**（照 auto 逼高 / 钉运行期值 / 分工种两档），
判据与数字见本文件 ②·「其他字段」那张表。

**D. `选牌_数据与规格.md:143` 那条「旁证」本轮简报问过 —— 答案是「**不是** `TurnText` 那一行」**
它给的是 **`MulliganText` 容器**（父）的 rect；`TurnText` 是它的**子件**，另有自己的 `ap/sd/anchors`。已就地订正（见 ③）。
顺带查出：原文把 `1.0`（= **anchor**）标成了 `pivot`（真 `pivot` 是 `0.5,0.5`）。

**E. `MulliganPanel.cs` 的 `TurnW` / `TurnH` 两个常量全仓无引用**（只作记录）—— 未动，也不想让它们被「顺手化简」掉。

---

## 📌 300 字以内摘要

`A1076` **不用改代码** —— 换牌面板 `MulliganText/TurnText` 那一行的 rect **早就取到并改完了**（2026-09-29），
任务简报引的那句「没取到、是我们放的」是**文档没跟上**。

rect 真值：`RT_3403`（`bundle_scenes_scenes_battlearena1`）· `ap(-0.9719,-50.0)` · `sd(-36.936,-25.272)` ·
`anchor(0,0)-(1,1)` · `pivot(.5,.5)` ⇒ **绝对左上 (312.50,129.42) · 1307.06×54.17 · 中心 (966.03,156.50)**；
父 `MulliganText` 中心是 **(967,106.5)**（原文混成了子件的 156.5）。
**13 个战场逐字段相同**，**运行期 dump 与静态逐位相同**。
**错因 = RT 文件按 PathID 命名、没有名字索引 ⇒ 按名字搜必然零命中，被记成了「本地没有」。**
本轮新增一条硬判据：原版 `MulliganManager.mulliganTurnTextLocalize` → 就是这颗节点上那颗 Localize。

改动只在**记录**：`MulliganPanel.cs` doc 注释 · `加时与冲突模式_原版规格.md` 换牌那节 · `选牌_数据与规格.md:143`。
类型检查 **0 错**；三个文件**行尾仍纯 LF**。

顺手发现（**未改**）：原版**运行期中文**「你是第二个行动」/「选择要在首轮替换的牌」**与我们 `zh_CN.csv` 不同**（那份表是我们自己译的）；
找到了原版 Addressables **目录**（`…/Everguild/Warpforge/com.unity.addressables/`，只 2.9 MB、**无内容**）；
该行 TMP 是 **autoSize**，实渲 **41.8**、我们写死 **55**（大 ≈32%）。
