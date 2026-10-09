# A 表已收口 · 第十一会话（2026-10-10）

> 🔴 **本文件的用法**：`项目任务.md` §三第 29 条 **§29·b** 是**唯一待办正本**；一条账**做完**就把它**整行原文**搬到这里（+ ✅ 注记），
> 正本里那行**删掉**。这样正本永远只等于「**还开着的**」。
> ⚠️ **本文件不是待办** —— 是**收口留痕**（「当时怎么销的」）。判据原文在各自那一行 / 各线报告里。

---

## 一、本次会话销掉的（逐行原文 + ✅ 注记）

### 1. `RankedRewardEventWindow.BuildCard` 那颗 `Debug.Log` 文案过期 —— ✅ **2026-10-10 已修**（**调度台自己动手**）

**原文（`项目任务.md` §29·b 第二节「没有独立编号的零碎」那一行，一字未改）**：

> 🔴 **`RankedRewardEventWindow.BuildCard` 那颗 `Debug.Log` 文案是过期的** —— 它印「后者那张图本仓没有 ⇒ 两张都只建节点、不画」，而它上面 `if (bgTex != null) MenuDraw.Rect(...)` **现在真会把 `Background` 画出来** ⇒ **这句话今天在骗人**（连带：那个 `if` 只看 `_warnedCardIcon`、**不看 `bgTex`**）

**✅ 收口内容**（`CardPresentation/Shell/RankedRewardEventWindow.cs`，**LF**）：

| 面 | 改前 | 改后 |
|---|---|---|
| **出声文案** | 印「`Army Icon` 与 `Background`…两张都**只建节点、不画**」 | 说 **`Army Icon` 只建节点不画**（原版由 `ArmyIconsSO.GetArmyIcon` 运行期灌、本地没那张表），**`Background` 那一半按实际走的那一支说** |
| **那道闸** | `if (!_warnedCardIcon)` —— 只看「报过没有」、**不看 `bgTex`** | 同上（保留一次性），但**文案里带上 `bgTex != null ? … : …`** ⇒ 两者一致、不再自相矛盾 |
| **代码注释** | 无 | 加了 **2026-10-10 订正痕**（铁律 5）：写明原句哪里错、为什么错、依据是本文件 `ArtCardBg` 那条 doc |

- **判据**：`ArtCardBg`（阵营卡底图）**本仓有** ⇒ `:565` 那颗 `if (bgTex != null) MenuDraw.Rect(go, bgTex, bgR, "Background", QCardBg)` **真会画**；只有取不到时才退化成 `MenuDraw.Node(go, "Background", bgR)` 空节点。
  而同文件 `:553-556` 的 doc **早就写过这条订正**，只有 `Debug.Log` 那句没跟着改 ⇒ 本次是**把落下的那一半补上**（铁律 5「把同一句话被复制到别处的地方一起改」）。
- **验证**：`TMPDIR=/tmp/wf_main bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误 0 / 编辑器错误 0**；
  `git diff --numstat` = `12 增 / 3 删`（**没翻行尾**）。

---

### 2. `A1191` —— `WindowHeader` 那一处「同形状再来一份」 —— ✅ **2026-10-10 裁定：框定不成立、无账可做**（**调度台自己动手**）

**原文（`项目任务.md` §29·b 第一节那一行，一字未改）**：

> ⚠️ **`WindowHeader` 那一处「同形状再来一份」**（`P1` 做 `A1173` 时顺手查出，⛔ 没改）—— `Shell/MenuWindowBase.cs:777` 的 `WindowHeader` 用 **`MenuDraw.Text(…, TitleFontPx, s.QTitle)` 不传 `wrapPx`**，随后**手工** `title.SetAutoFitBox(fitW, fitH, …)`（`:787` / `:792`）⇒ 这**正是 `A1173` 刚消灭掉的那种「两处写同一条规则」**。⚠️ 修它要么动 `MenuDraw.cs`（**共用件**）、要么改 `WindowHeader` 自己走本窗漏斗 ⇒ **做法/判据待定**。⇒ **要做**（铁律 11）。出处 → `资料/普查产出_第十会话/P1_A1173.md` §7·①。

**✅ 裁定（判据 = 现读源码 + 与 `A1195` 同一族的先例）**：**不是「两处写同一条规则」，是 `TextCore` 的【能力缺口】** ⇒ ⛔ **不该把形状统一**。

| 事实 | 现读判据 |
|---|---|
| `MenuDraw.Text` **十个形参里没有 `charSpacing`** | `Shell/MenuDraw.cs:1809-1812` 签名 —— `(parent, r, text, color, name, fontPx, q, wrapPx, autoMinPx, autoMaxPx, autoBasePx, clip, clipSoftness)` |
| 自适应被藏在 `wrapPx > 0f` 那道闸里 | `Shell/MenuDraw.cs` 的 `TextCore`：`if (wrapPx > 0f) { SetWrapWidth(…); if (autoMinPx > 0f && fontPx > autoMinPx) SetAutoFitBox(…); }` |
| 本处要的是**三档次序**（带字距） | `MenuWindowBase.TitleFit` 的 doc：`SpacingOnly`（**完全不要**自适应）· `FitBeforeSpacing`（自适应→字距）· `FitAfterSpacing`（字距→自适应）⇒ `TextCore` **一档都表达不出** |
| 「手工 `SetAutoFitBox`」是**既有且普遍**的写法，不是孤例 | 现读 `grep -rn "SetAutoFitBox("` ⇒ `Deck/DeckRuntime.cs` 5 处 · `Battle/UnitChatPanel.cs:344` · `Editor/ChatBoxProbe.cs:40/55` · `Editor/BattleScene.cs` 十几处 · … |
| 而**规则本身只有一处**（没有第二份） | 就是 `Battle/Label.cs` 的 `Label.SetAutoFitBox`；各站的 `min/max/base` 是**各自 prefab 的值**，不是规则的副本 |

⇒ **与 `A1195` 同一族的裁定**（两个漏斗契约语义不同 ⇒ **写清差异、⛔ 不统一形状**；统一 = 丢掉原版真有的组合）。
**本件的收口 = 把这条差异写进 `Shell/MenuWindowBase.cs:776` 之前那段注释**（含「真要合并得先给 `MenuDraw.Text` 加『字距 + 次序』两维、那是共用件的改动」这一条留痕），⛔ **不是**改形状。

- **验证**：`TMPDIR=/tmp/wf_main bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- ⚠️ **残留（如实登记，⛔ 不属本账）**：若将来真要给 `MenuDraw.Text` 加那两维，**那是一次共用件改造**，判据要另立（且必须先想清「`TextCore` 还是 `TextBox` 的内层」这一层影响面）。

