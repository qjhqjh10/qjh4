# FX3 · 夹具砸脚八条红修复（写手 · 2026-10-11）

> **只改了白名单里的两个文件**：`Editor/ShellScene.cs` · `Editor/CollectionScene.cs`。
> ⛔ 没跑 Unity / 自检（只有主对话能跑）· ⛔ 没动 git（只用过只读的 `git diff --numstat`）· ⛔ 没碰 `资料/**` 既有文件与两张正本。
> 判据 = `资料/普查产出_1011/DIAG-A_Shell与Collection八条红.md`（**§三 / §四 的可执行版**）。
> **走的是「有牙口」那一版**，⛔ 不是「删两句挪窗根 + 改文案」那一版（DIAG-A §3.3）。

---

## 一、结论（四个调用点各一句）

1. **`ShellScene` A306①（`PromptPopup.Local` → `OkButton`）**：夹具姿势换掉 —— **M 加在「窗根的父级」那一颗探针根上**、窗根留在它下面且 `localPosition = (2,1.5)`、态二的 `measure` 里 `TryOpen` **重建** ⇒ 那两条红既消失、又**照得出改坏法**。
2. **`ShellScene` A306③（`ImportDeckPopup.Local3` → `Window`）**：同上（探针 `probeC`）。
3. **`CollectionScene` A306②（`DeckInfoPopup.Local3` → `Buttons`）**：同上（探针 `probeB`）。
4. **`CollectionScene` A306④（`PracticeModePopup.Local3` → `Army Selector`）**：同上（探针 `probeD`）。
   ⛔ 四处**都没有**用「删掉挪窗根那两句」去换绿 —— 那样做窗根回到原点，`p2 == M × p1` 对**任何**实现都成立（假绿）。

---

## 二、改法（逐处：原写法 / 新写法 / 为什么有牙口 / 改坏法）

### 2.0 共用夹具 `CheckScaleTwo`（两份：`ShellScene.cs:303` · `CollectionScene.cs:610`）

| # | 原写法 | 新写法 | 为什么有牙口 |
|---|---|---|---|
| A | 前提 = `\|b1.x\| > 1 \|\| \|b1.y\| > 1`（`b1 = basis.position`，量的是**离世界原点**） | `\|b1.x − w1.x\| > 1 \|\| \|b1.y − w1.y\| > 1`（`w1 = scaleRoot.transform.position`，量的是**基准相对被乘那一级的位移**） | 决定「除不除缩放有没有差别」的是这个位移（偏差 ≈ `M(M−1)×\|位移\|`），不是基准离原点的距离；旧式**逼着调用方去挪窗根**，一挪恒等式就断 |
| B | 无 | 新增（态二）：`CheckNear(basis.parent.lossyScale.x, m, 1e-3)` | `PosInDesignSpace` 除的正是 `basis.parent.lossyScale`（`Shell/MenuDraw.cs:74-78`）；那一级是单位缩放时新旧两式**逐位相同** ⇒ ★ 等于没查 |
| C | 参数 1 叫 `winRoot` | 改名 `scaleRoot`（**纯改名，签名位置不变**） | 它现在是**窗根的父级探针根**，再叫 `winRoot` 会误导下一个读者（⛔ 缩放那几步一行没变：`SetScale` + `Tick` + 放回 1） |

> ⚠️ 简报写「`CheckScaleTwo` **函数体一行不用改**」—— 那指的是 **★ 那条恒等式与量法**（量什么 / 期望式 / 容差 0.02 / 还原六步**全没动**）；
> **前提那两句是简报 ①.4 点名要改的**（上表 A/B），改名 C 只是跟着 A/B 一起对齐措辞。⇒ 与简报**不冲突**，但确实动了 3 处，如实记在这里。

- **改坏法 A**：把四个调用点的 `localPosition = (2,1.5)` 删掉 ⇒ 这条前提红（基准落在被乘那一级的原点上）。
- **改坏法 B**：M 仍加在 `basis` 自己身上（= 上一版那种塞法）⇒ `basis.parent.lossyScale` 仍是 1 ⇒ 这条红。
- ⚠️ **`RewardsScene` / `ShopScene` 那两份副本没动**（不在白名单）：它们的调用点 `basis` 是**窗根的子件**（`TimeCounter` / `Content`），旧前提 `|b1| > 1` 对它们仍成立 ⇒ 不会因本件而红。**四份夹具的前提文案现在不一致**（见 §四）。

### 2.1 四个调用点（每个三样：探针根 / 态二重建 / 文字）

- **原写法**：`TryOpen` → `窗根.localPosition = (2,1.5)` → `CheckScaleTwo(窗根, 窗根.transform, () => 子件.position, 1.2f, …)`。
- **新写法**：`探针根 = new GameObject("A327①/②/③/④ probe root")` → `窗根.SetParent(探针根, false)` → `窗根.localPosition = (2,1.5)` → `TryOpen` → 找到子件 → `CheckScaleTwo(探针根, 窗根.transform, () => { 窗根.TryOpen(null); n = 找子件; 断「重建过」; return n.position; }, 1.2f, …)`。
- **为什么有牙口**（代数，`M = 1.2`、`W = (2,1.5)`、`d` = 子件的设计点）：
  · 态一 `p1 = d`（两式同值）；· 态二好式 `p2 = M·d = M·p1` ✅；
  · 态二坏式（**重建之后**）`p2 = M·d − M(M−1)·W = M·p1 − (0.48, 0.36)` ⇒ 偏 **0.6 单位 = 65px** ≫ 容差 0.02（2.2px）⇒ 红。
  · ⚠️ **不重建就没牙口**：`localPosition` 冻结在 `k == 1` 那一趟时，好坏两式都给 `M·p1` ⇒ 恒真（这就是 ③ 必须成立的原因）。
- **新增的第 5 条断言（在 `measure` 里，每次调用跑一次）**：「态二的 measure **真的重建了**」`n != null && n != 旧节点`。
  · **改坏法**：把 `measure` 改回纯读（`() => okA.position`）⇒ 这条红；同时 ★ 会退化成假绿（两件事一起被挡住）。
  · 实据：四个窗的 `Build()` 首句都清空子件（`PromptPopup.cs:93-96` · `ImportDeckPopup.cs:92-95` · `DeckInfoPopup.cs:714-717` · `PracticeModePopup.cs:688-692`），`RewardsWindow.DestroySafe` 在批处理（not playing）走 `DestroyImmediate`（`Shell/MenuWindowBase.cs:155-161`）⇒ 旧节点当场变假 null。
- 🔑 **可复核的预测（拿旧日志回代）**：新夹具的 **`p1` 与旧夹具逐条同值**（态一都是「窗根在 (2,1.5)、k=1」⇒ `p1 = d`），
  而新 `p2` 应当**正好等于旧日志里那个「期望」列**（= `M × 旧 p1`）：A306① `(2.400, 1.078)` · A306③ `(2.400, 2.689)` ·
  A306② `(5.600, −2.49)` · A306④ `(−6.51, 1.90)`（数字取自 DIAG-A §一 那张表）。**若跑出来不是这四个数，就是本件没做对。**
- **★ 四条的「改坏法」文字（已逐条写进断言文案，日志里看得见）**：

| 处 | 改坏法（写进断言里的原文） |
|---|---|
| A306① | `PromptPopup` 里把 `Local` 换回裸 `parent.position` ⇒ 偏 `M(M−1)×\|位移 (2,1.5)\|` = 0.24×2.5 = **0.6 单位 = 65px**（容差 0.02 = 2.2px）⇒ 红；⛔ 别把窗根挪走换绿 |
| A306③ | `ImportDeckPopup` 里把 `Local3` 换回裸 `basis.position` ⇒ 同上 ⇒ 红 |
| A306② | `DeckInfoPopup` 里把 `Local3` 换回裸 `basis.position` ⇒ 同上 ⇒ 红 |
| A306④ | `PracticeModePopup` 里把 `Local3` 换回裸 `basis.position` ⇒ 同上 ⇒ 红 |

> ⚠️ 原文那句「偏 `0.24 × |窗根挪到的设计点 (2,1.5)|`」按 DIAG-A §四 的订正改成了
> `M(M−1)×|基准相对被乘那一级的位移|` —— **旧句把「位移」和「基准的设计位置」混为一谈**，数值 0.6/65px 恰好相同是巧合。

---

## 三、改动清单

**`Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs`**（+ 约 60 行 / − 约 9 行，**全是 4/8 空格缩进的注释与自检代码**）

| 行（改后） | 改了什么 |
|---|---|
| `:270-279` | 文件头「为什么要它」补一段**口径收窄**：那 8 处里「基准恰好就是窗根」的几处在生产里**永远**观测不到 ⇒ 是 **no-op**，不是「潜伏」 |
| `:286-303` | 夹具文件头**三处订正**（M 加在父级 · 可观测余量 = 相对位移 · 态二必须重建）+ 参数改名 `winRoot`→`scaleRoot` |
| `:303-341` | 夹具体：加 `w1`、前提②改写、新增前提①（`basis.parent.lossyScale`）、★ 文案改「被乘 M 的那一级」、收尾行改名 |
| `:2701-2711` | A327 段头**订正「两处都是潜伏缺陷」**（铁律 5）+ 指出现生产带电的是**非根基准**那一族（`PracticeModePopup.cs:494/502/792/796/853/924/1238`） |
| `:2712-2751` | A306① 调用点（探针 `probeA` + 重建 + 前提③断言 + ★ 文案） |
| `:2754-2788` | A306③ 调用点（探针 `probeC` + 重建 + 前提③断言 + ★ 文案） |

**`Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`**（+ 约 65 行 / − 约 10 行）

| 行（改后） | 改了什么 |
|---|---|
| `:577-587` | 同上：文件头口径收窄 |
| `:593-610` | 同上：夹具文件头三处订正 + 参数改名 |
| `:610-648` | 同上：夹具体（`w1` / 前提② / 前提① / ★ 文案 / 收尾改名） |
| `:4866-4876` | A327 段头订正 |
| `:4877-4917` | A306② 调用点（探针 `probeB` + 重建 + 前提③断言 + ★ 文案） |
| `:4919-4957` | A306④ 调用点（探针 `probeD` + 重建 + 前提③断言 + ★ 文案，另加一句 `1.07` 的注释） |

- 探针 `GameObject` 一律 **`DestroyImmediate`**（先销窗、再销探针）⇒ 不留残骸给后面的断言。
- 行尾：两个文件改前改后都是 **纯 LF**（`CRLF = 0`）；`git diff --numstat` 数字远小于文件行数 ⇒ **没翻行尾**（文件里另有本批别的改动，故绝对值不等于本件行数）。

---

## 四、没做 / 判不了的（⛔ 不猜）

1. **没实跑突变**：本件证明「现在照得出改坏法」靠的是**代数 + 与四个 `Build()` 的重建口逐条核对**，不是跑一次真突变 —— 红线不许跑 Unity ⇒ **缺一次「把 `Local` 换回裸 `parent.position` 再跑 `ShellScene.Run` / `CollectionScene.Run`」的实锤**（同 DIAG-A §五·1）。
2. **另两份副本未同步**：`Editor/RewardsScene.cs` · `Editor/ShopScene.cs` 的 `CheckScaleTwo` **仍是旧前提（无前提①、前提②量的是离原点）、`measure` 里也没有重建** ⇒ 四份夹具现在**两种行为**（DIAG-A §四·1 说的「同一夹具两种行为」仍然成立，只是反过来：这次是那两份落后）。**不在本件白名单**，只报。
3. **`ShopScene` / `RewardsScene` 的两处 `basis` 是否够远**没核（它们今天绿，且旧前提对它们成立；若将来换成新前提，`ShopScene` A306⑥ 实测 `(-4.04,3.97)`、`RewardsScene` A306⑤ 实测 `(1.53,-1.16)` 都 ≥1 ⇒ 大概率照样绿，**但没实跑**）。
4. **没跑 `SettingsScene` / `MainMenuScene`**（DIAG-A §五·4 已 grep 过：全库只有 Shell/Collection 有「挪窗根」形状；本件不重复）。
5. **四窗的「更贴生产」形状没做**：DIAG-A §六·3 建议 A306④ 改量 `Army Selector/Viewport`（那是**非根基准**、生产上真带电）。本件按简报走「探针根 + 态二重建」那一版；要不要再补一条**非根基准**的探针（照 `Editor/ShopScene.cs:2888-2958` A294），**留给调度台定**。

---

## 五、顺手发现（⛔ 只报不改）

1. ⚠️ **失败表重打保留未动**：`ShellScene.cs` 收尾的 `if (_fail > 0) foreach (…) Debug.LogError(P + "   ✗ " …)` 与 `CollectionScene.cs` 的 `StringBuilder` 那一段，会在每条失败**已在现场打过一次**之后再打一遍（`Check` 里就 `Debug.LogError(P + "   ✗ " …)`）⇒ 日志里 `✗` 行数 = 失败数 ×2。
   **判断**：本件**没动**它 —— 它不是正确性缺陷（不产生假绿/假红），而**改它会动到主对话的读日志习惯**（尾部失败表）；且 DIAG-A 已把「按文案去重」写进判据。**建议**：要么保留、要么把重打那几行的行首标记从 `✗` 改成 `失败重列：`（让 `grep -c ✗` 数得对），**由调度台拍**。
2. ⚠️ **`PracticeModePopup.extraScaleSmallScreen = 1.07` 的副作用（本件新发现）**：态二那次 `TryOpen` 会走生产那段 `ApplySmallScreenScale`（`Shell/WindowsManager.cs:375-387`），照原版往**窗根**上挂一颗 `TransformScalerBySmallScreenUI`（`menuScale = 1.07`、`enabled = true`）。批处理**没有帧循环**、产品路径只在 `LateUpdate` 里乘 ⇒ **今天无害**（量到的仍是 1.0 那档）。⚠️ 但**谁要是把这套夹具放进 Play 模式跑、或在夹具之后手动 `Tick()` 它**，那个数会变成 1.07 倍 ⇒ 量到的就不是 1.0 那档。已在调用点注释里写明（不是断言，**只是如实标注**）。
3. **断言条数会变（供主对话核数字）**：每个调用点 **+4 条**（前提① 1 条 + `measure` 的三次重建前提各 1 条）⇒ Shell 一侧 2 个调用点 **+8**、Collection 一侧 **+8**。
   ⇒ 预期：`ShellScene` **590 通过 / 0 失败**（→ 598 通过）· `CollectionScene` **948/952 通过**（现 940/944）。⛔ 这是**算出来的**，不是跑出来的。
4. ℹ️ 探针根是**裸 `Transform`**（照 DIAG-A §四 模板），而 `PracticeModePopup` 的窗根是 `RectTransform` —— 因为 `MenuDraw.SetPxSize` 给的是**重合锚点**，`localPosition` 语义不受影响（不是「锚点偏移被吃掉」）。

---

## 六、跑过的检查

```text
$ TMPDIR=/tmp/wf_fx3 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（改**每一批** `.cs` 之后都跑过一次，最后一次结果即上面这两行；⛔ 没跑 Unity / 自检。）
