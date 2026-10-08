# 交件：E3b · 排行榜同窗另三处命中区（页签 / 关闭键 / `Last season`）

> 🔴🔴 **2026-10-18 之后·第四会话【结论已作废 · 铁律 5 就地订正】—— 关闭键那处改动方向是错的，已回滚。**
> **错因**：**没读 `Graphic.m_RaycastPadding`**（本文 `:60` 自己写着「未查该字段」，**下一句却下了结论** ⇒ **那是红旗**：
> 代理承认「某字段没查」时，那条结论就不能用）。
> · 关闭键：**原版真值 = 96.86 × 98.13**（子件 `Background` 56.86 × 58.13 ＋ pad(−20)⁴）；
>   本文改成的 **56.86 ⇒ 每边小 20**，**比改前（74.38 × 75.61）更糟**；两者都偏小、真值最大。
> · 另两条「不用改」**仍然成立**（页签 **165.00** / `Last season` **245.00` —— 那两颗 pad 全 0，逐颗核过）。
> · ⚠️ 本文顺手查出的「**关闭键悬停换图换错层**」**仍然成立**（那是另一类缺陷，见 `A1058`）。
> ✅ **正确做法**：走 `PaddedRect`，先例 = `Editor/CollectionScene.cs` A180/A299。
> 📌 **本文的读法仍然有效**（三级跳读组件 · pid 实读 · 35 个实例结构逐位相同 · `m_TargetGraphic`/`m_RaycastTarget` 的并集口径）。
>
> 只改 `Shell/LeaderboardWindow.cs` 一个文件。⛔ 未跑 Unity · ⛔ 未碰 git（只跑过 `git diff --numstat` 只读）· ⛔ 未改正本/白名单外文件/断言。
> 秒级类型检查：`TMPDIR=/tmp/wf_e3b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（改前改后各一次）。

## ① 结论

**三处逐颗核完：一处要改（`:653` 关闭键），两处不用改（`:636` 页签 · `:674` `Last season`）。**
方法照 `交件_E3_army命中区修复.md`：读原版 GO 组件表 → `MonoBehaviour_<pid>.json` 的 `m_Script` → `monoscripts/MonoScript/MonoScript_<pid>.json` 的 `m_ClassName`；
**可射线件判据 = 子树里每一颗 `Graphic.m_RaycastTarget == 1` 的并集**（⭐ **不是 `m_TargetGraphic` 一处** —— 见下面两处的差别）。

## ② 逐处表（**行号为现读**；与交件给的行号一致）

| 处 | 现读行号 | 原版真正可射线的那颗（pid / GO / 实宽高） | 我们原来传的矩形 | 判定 | 改动 |
|---|---|---|---|---|---|
| 页签 | **:636** | 根 `Player`/`Armies`/`Alliances`（`Tab Buttons` 下）**根上没有任何 Graphic**（只有 `RectTransform·CanvasRenderer·EverguildToggle·<provider>`）；`EverguildToggle.m_TargetGraphic` → 子件 **`button_bg` 的 `Image`**（Player pid `6191962745462503908` · Armies `-705875593777989148` · Alliances `-8615426814078174748`），RT `anchors(0,0)-(1,1) sd=0,0` = **撑满父 = 165.00 × 157.68**，`m_RaycastTarget=1`。`Icon` 的 `Image` **`m_RaycastTarget=0`**（不是可射线件）；`Label`/`Tab Toggle Title` 祖先 inactive | `r` = `(45.59, t, 210.59, t+157.684)` = **165.00 × 157.684** | **不用改**（逐位相同） | 无 |
| 关闭键 | **:653**（改后落在 **:661**） | 根 `Generic Close Button Orange` 上那颗 `Image`（`UI_Button_Round_background`）**`m_RaycastTarget = 0`** ⇒ **不接受射线**（pid `4123737957516491236`）；`EverguildButton.m_TargetGraphic` → 子件 **`Background` 的 `Image`**（pid `7857352521433070052`，`m_RaycastTarget=1`），**`Icon` 与它同一个 RT**（`anchors 0.114→0.873366 / 0.12429→0.88814`，两者各自 `m_RaycastTarget=1`）⇒ **可射线区 = 56.86 × 58.13** | `CloseR` = `(1656.81, 9.19, 1731.19, 84.80)` = **74.38 × 75.61**（= 根矩形） | 🔴 **要改** —— **宽出 17.52 / 高出 17.48**（圆盘四角原版点不动） | **`:653` `CloseR` → `CloseInnerR`**（56.86 × 58.13） |
| `Last season` | **:674**（改后落在 **:682**） | 根 `Generic Simplified UI Button_updated` 自己那颗 **`Image`**（`UI_Button_Mulligan`，`m_RaycastTarget=1`）；`EverguildButton.m_TargetGraphic` **指回它自己那颗 Image**（四扇逐扇实读：「graphic → 本节点」，`pid` 与根 GO 同名同号）—— ⚠️ 那些 pid 超出了 float 精度，**本表不抄 pid**，判据是「它指回了自己这一颗」；子件 `Button Text` 的 TMP 虽 `m_RaycastTarget=1` 但在其内 ⇒ 并集 = 根矩形 **245.00 × 59.05**（嵌入版 **245.00 × 67.64**） | `btnR` = `SeasonBtnR` `(264.74, 62.21, 509.74, 121.26)` = **245.00 × 59.05**（嵌入版 `EmbBtnR` = **245.00 × 67.64**） | **不用改**（逐位相同） | 无 |

**四处 `MenuDraw.Hit` 只有这四个**（`:555` 已由 E3 修过）⇒ **没有第五处**（`grep -n 'MenuDraw\.Hit'` 全文命中 = 555 / 636 / 653 / 674）。

**三扇弹窗（`RankedSkirmish…` / `RankedClassic… Variant` / `DraftLeaderboardPopup`）这三处的结构逐位相同**（只有 pid 不同）⇒ 一条改动覆盖三扇。嵌入版 `Ranked Leaderboard Display` **不建页签、不建关闭键**（`BuildEmbedded` 只调 `BuildSeasonPieces(true)`），season 那颗已被上面那格覆盖。

## ③ 改了哪几处（贴 diff，1 行 → 9 行）

```diff
@@ -643 +653,9 @@ namespace CardPresentation
-            MenuDraw.Hit(close, "Hit", CloseR, QHit, () => Close(), closeBaseQ, null, "40k_general_bt_yellow_hover");
+            // 🔴 **2026-10-08（E3b · 真缺陷修复）**：命中区**从「整颗根矩形」换成「`Background`/`Icon` 那一颗」**。
+            //   判据（第一权威 = 原版 prefab 实读，三扇弹窗逐位相同）→
+            //   `资料/普查产出_第四会话/交件_E3b_同窗另三处命中区.md` §②：
+            //   根 `Generic Close Button Orange` 上的 `Image`（`UI_Button_Round_background`）**`m_RaycastTarget = 0`**
+            //   ⇒ **原版那颗圆底盘不接受射线**；`EverguildButton.m_TargetGraphic` 落在 **`Background`**（`40k_general_bt_yellow`，
+            //   `m_RaycastTarget = 1`）上，`Icon` 与它**同一个矩形**（anchors 0.114→0.873366 / 0.12429→0.88814）⇒
+            //   **原版可射线区 = 那颗 = 56.86 × 58.13**（= `CloseInnerR`），不是根矩形（74.39 × 75.61）——
+            //   我们原来用根矩形 ⇒ **宽出 17.52 / 高出 17.48**（圆盘四角原版点不动）。
+            MenuDraw.Hit(close, "Hit", CloseInnerR, QHit, () => Close(), closeBaseQ, null, "40k_general_bt_yellow_hover");
```

- **只用了本文件既有常量**（`CloseInnerR`，:188 就在那儿、已用于 `Background`/`Icon` 两层）⇒ **没有新造第二套换算**。
- 行尾**仍是纯 LF**（`CRLF 0 · LF 767`，⛔ 未用 `sed -i`）。`git diff --numstat` ⇒ **27 加 / 5 删**，其中 **19 加 / 2 删是别人先前那两段**（E3 army `@@ -543` `@@ -545` + 波 1b · P2b 双语 `@@ -684`）；**本件 = `@@ -643 +653,9` 单独一个 hunk（9 加 / 1 删）**。

## ④ 判不了的是哪几处 + 为什么

- **没有一处「判不了」** —— 三处都读到了确定的原版可射线件（pid + GO + 实宽高 + `m_RaycastTarget`）。
- ⚠️ 唯一**没实测**的：`ArmyIconR` 那 0.04px 取整（`交件_E3` §⑥已记）**与本件无关**，本件一个字没动它。
- ⚠️ 我**没有**真鼠标证据（红线禁跑 Unity）⇒「改后圆盘四角点不动、中间 56.86×58.13 点得着」是**推的**（判据 = `m_RaycastTarget`），归真 Play。

## ⑤ 建议加的断言（本件不写；宿主建议 `Editor/MainMenuScene.cs` —— 那儿已有「排行榜弹窗」那两节 `CheckShadeRule`/`CheckAbsorbRule`）

- **A · 灭自证（结构式，不看「有没有重叠」）**：断关闭键那颗 `Hit` 的**宽 == 56.86**（= `CloseInnerR.W`；原版实读 56.86）**且根矩形 `CloseR.W == 74.38`**。⚠️ 只断「A 不再多报」不够：把 `Hit` 改回 `CloseR` **+ 同时**把那一对塞进白名单 ⇒ 一起变绿；A 两边一起改回去就裂开。
- **B · 反向锚（证明 A 不是常量比对）**：断**画出来的** `Background`/`Icon` 两颗 quad 的矩形**逐位 == `CloseInnerR`** —— 于是「命中那颗 == 原版可射线那颗 == 画的那颗」三件事绑在一起。
- **C · 判别式夹具两态（一报一不报）**：态一「外框 74.38 但命中 56.86、两颗同心」⇒ **必须不报** E3 重叠；态二「命中区 = 外框 74.38」⇒ **必须报**（差 17.52 只在第二态出现）。
- **D · 页签/season 两颗「不用改」也要钉住**：断 `Hit` 宽 == **165.00**（页签）/ **245.00**（season），**且**同节点上**另有一颗** `Image` 的 `raycastTarget == 0`（`Icon` / 无）—— 防后人「照关闭键那件把这两颗也缩小」。

## ⑥ 没做到 / 拿不准

- **没跑 Unity**（红线）⇒「改后四角点不动」是算式/字段结论，不是实测。
- ⛔ **没做**：把 `m_RaycastTarget` 这套口径推广到**别的窗**（好几个窗都有 `Generic Close Button Orange` 同族）。**同族多半同病**，但本件白名单只这一个文件 ⇒ 只报不做（见 ⑦-①）。
- **拿不准**：本件只覆盖 `m_RaycastTarget`（U3D 射线**开关**那一层）；`Graphic.m_RaycastPadding`（**外扩/内缩**那个字段）没逐颗读 —— 若原版这几颗带非零 `m_RaycastPadding`，命中区会与本表不同。**本轮没查**（搜过什么：`m_RaycastPadding` 只在 `Shell/DeckInfoPopup.cs` 的 `WarlordPad` 有过使用者，本窗四处未见）。

## ⑦ 顺手发现（⛔ 都没改）

1. 🔴 **关闭键的「悬停换图」换错了层**（**不是命中区，是另一类缺陷**，故不动）：原版 `m_TargetGraphic` = **`Background`**（`40k_general_bt_yellow`，71×71）⇒ `SpriteSwap` 换的是**黄圆那一层**（`→ 40k_general_bt_yellow_hover`）；我们 `:653` 传给 `MenuDraw.Hit` 的 `target` 是 `closeBaseQ` = **根 `Image` 层**（`ArtCloseBg = UI_Button_Round_background`，画在 `CloseR`）⇒ 悬停会把**圆底盘**换成黄圆图，**而黄圆本身不变**。判据同一份（上面 §② 那两颗 pid）。
2. ⚠️ **同族窗口可能同病**：`Generic Close Button Orange` 这个 prefab 族在别的窗里也建（本件白名单外）。**建议另开一件逐窗核**（同 E3/E3b 的方法）—— 我没查别的窗，**不知道它们几扇**。
3. ℹ️ 页签的 `Icon` `Image` 是 `m_RaycastTarget=0`（我们照旧画它、不拿它当命中区）—— 与我们的写法**一致**，不是缺陷；记下来是因为**它正是「不能拿 `TargetGraphic` 一处代替并集」的反例**（页签的 `TargetGraphic` 指 `button_bg`，军种条的指 `Icon`，两族不一样）。
