# DIAG-A · ShellScene 4 + CollectionScene 4 红诊断（只读 · 2026-10-11）

> 只读诊断代理。**没改任何文件**（唯一新文件 = 本报告）· **没跑 Unity** · **没动 git 写命令**。
> 现场 = `d:/4/_tmp_view/shell.log`（`[Shell] === 合计：590 通过 / 4 失败 ===`，`:10312`）·
> `d:/4/_tmp_view/collection.log`（`[Collection] === 结束：940/944 通过，**4 条失败** ❌ ===`，`:24225`）。

---

## 一、结论（8 条各归哪一档）

**8 条红是同一处错，全部 = (δ) 夹具前提不成立 + (α) 断言期望值写错（同一条错的两面）；⛔ 不是 (β) 实现缺陷、⛔ 不是 (γ) 本批回归。**

一句话根因：
`CheckScaleTwo` 断的 `p2 == M × p1` **只在「窗根落在世界原点」时成立**（`p1`/`p2` 是**世界坐标**），
而这四个夹具为了让「基准件离窗根 ≥1 单位」这条前提成立，**把窗根自己挪到了 (2,1.5)**
⇒ 断言的那条恒等式被夹具自己破坏：实测偏差**逐条等于 `(1−M) × 窗根位移` = (−0.400, −0.300)**。

| # | 处 | 日志行 | 归属 | 偏差（实测 vs 期望） |
|---|---|---|---|---|
| 1 | `ShellScene` A306① x | `shell.log:10102`（重打 `:10323`） | δ+α | 2.000 vs 2.400 ⇒ **−0.400 = (1−1.2)×2** |
| 2 | `ShellScene` A306① y | `shell.log:10117`（重打 `:10334`） | δ+α | 0.778 vs 1.078 ⇒ **−0.300 = (1−1.2)×1.5** |
| 3 | `ShellScene` A306③ x | `shell.log:10243`（重打 `:10345`） | δ+α | 2.000 vs 2.400 ⇒ **−0.400** |
| 4 | `ShellScene` A306③ y | `shell.log:10258`（重打 `:10356`） | δ+α | 2.389 vs 2.689 ⇒ **−0.300** |
| 5 | `CollectionScene` A306② x | `collection.log:23994`（重打 `:24226`） | δ+α | 5.199 vs 5.60 ⇒ **−0.401** |
| 6 | `CollectionScene` A306② y | `collection.log:24009`（重打 `:24227`） | δ+α | −2.79 vs −2.49 ⇒ **−0.300** |
| 7 | `CollectionScene` A306④ x | `collection.log:24167`（重打 `:24228`） | δ+α | −6.911 vs −6.51 ⇒ **−0.401** |
| 8 | `CollectionScene` A306④ y | `collection.log:24182`（重打 `:24229`） | δ+α | 1.60 vs 1.90 ⇒ **−0.300** |

> ⚠️ **日志里 8 条 `✗` 只对应 4+4 条失败**：`ShellScene.cs:2726` 与 `CollectionScene.cs:4893` 在收尾时把失败表**再打一遍**
> ⇒ 数「红」要按断言文案去重（或看 `合计` / `结束` 那一行）。⛔ 别 `grep -c ✗`（`✗` 也出现在断言文案里）。

**排除 (β)/(γ) 的两条硬证据**（都在日志里，不是推演）：
1. 同一夹具的**前提三条与收尾那条全绿**（`✓ 态一窗根没被谁乘过(1.000)` · `✓ 态二窗根 localScale = M(1.200)` · `✓ 窗根放回 1 之后位置也回到态一那一份`）
   ⇒ 缩放器、还原、量法都正常，只有「比值」这一条红。
2. **偏差的数值签名是夹具自己的**：4 处 `x` 全差 −0.400、4 处 `y` 全差 −0.300，正好 = `(1−M)·(2,1.5)`，
   而且**逐条**满足 `p2 = W + M·(p1 − W)`（W = 夹具亲手写的 `Vector3(2f, 1.5f, 0f)`，M = 1.2；`p1` 由日志反解）——
   见 §二 每条的「回代」列。实现侧的任何缺陷都**不会**给出这么整齐的 `(1−M)·W`。

---

## 二、逐条

**共用证据（8 条同源）**

| 判据 | 出处 |
|---|---|
| 夹具本体 `CheckScaleTwo`（4 份逐字同源） | `Editor/ShellScene.cs:287-315` · `Editor/CollectionScene.cs:594-622` · `Editor/RewardsScene.cs:656-687` · `Editor/ShopScene.cs:639-667` |
| ★ 断言 = `CheckNear(p2.x, m*p1.x, 0.02f, …)` | `Editor/ShellScene.cs:306,310` · `Editor/CollectionScene.cs:613,617` |
| 前提 = `Mathf.Abs(b1.x) > 1f \|\| Mathf.Abs(b1.y) > 1f`（`b1 = basis.position`） | `Editor/ShellScene.cs:294-296` · `Editor/CollectionScene.cs:601-603` |
| 四个调用点（`basis` 一律 = **窗根本身**） | `Editor/ShellScene.cs:2693`（`ppA.transform`）· `:2713`（`ipA.transform`）· `Editor/CollectionScene.cs:4857`（`dipA.transform`）· `:4877`（`pmpA.transform`） |
| 夹具亲手挪窗根 `localPosition = (2,1.5)` | `Editor/ShellScene.cs:2689,2709` · `Editor/CollectionScene.cs:4853,4873` |
| 被量的件是**窗根的直接子件**、其 `Local*` 的实参也是窗根 | `Shell/PromptPopup.cs:95`(`root = transform`) → `:207,209`(`MakeButton(root,…)`→`Node(root,"OkButton",…)`) · `Shell/ImportDeckPopup.cs:94,108` · `Shell/DeckInfoPopup.cs:716,833` · `Shell/PracticeModePopup.cs:758`(`sel.localPosition = Local3(root, ArmL,…)`) |
| `PosInDesignSpace` **除的是【父节点】的 `lossyScale`** | `Shell/MenuDraw.cs:71-79`（`var above = t.parent; var k = above.lossyScale;`）+ `:57-61`（原文自认「**只在「窗根在世界原点」时精确**」） |
| 生产里窗根的父级 = Holder（单位缩放）、窗根 `localPosition = 0` | `Shell/WindowsManager.cs:805-808`（`AttachToAnchor`：`SetParent(anchor,false)` + `localPosition=zero` + `localScale=one`）· `:603-607,622-630`（`EnsureHost`/`MakeHolder` 建三个 Holder，未写 scale） |
| 缩放器挂在**窗根自己**身上 | `Shell/WindowsManager.cs:375-387`（`ApplySmallScreenScale` → `gameObject.AddComponent<…>`）· `Shell/TransformScalerBySmallScreenUI.cs:143-150`（`Tick`：`localScale = cur × menuScale`） |

**代数（8 条共用）**：设窗根世界位置 `W`（= 夹具写的 (2,1.5)，Holder 在原点 ⇒ `root.position = (2,1.5)`）。
态二只改了 `winRoot.localScale`（1→1.2），**没有任何一处重建**（`measure` 是 `() => xxx.position` 的纯读）
⇒ 被量节点在窗根局部系里的偏移 `v` 是**常量** ⇒ `p(M) = W + M·v`，即

```
p2 = W + M·(p1 − W)
⇒ p2 == M·p1  当且仅当  W == 0（或 M == 1）
```

**W = (2,1.5) 是夹具自己写的** ⇒ 两条只能红，且偏差必然 = `(1−M)·W = (−0.4, −0.30)`。逐条回代：

| # | 处 | 日志实测 `p2` | 由日志反解 `p1` | 回代 `W + M(p1−W)` | 反解的**设计点** `p1−W` |
|---|---|---|---|---|---|
| 1 | A306① x | 2.000 | 2.000 | 2 + 1.2(0) = **2.000** ✓ | (0.000, …) |
| 2 | A306① y | 0.778 | 0.8983 | 1.5 + 1.2(−0.6017) = **0.778** ✓ | (…, −0.6017) |
| 3 | A306③ x | 2.000 | 2.000 | 2 + 1.2(0) = **2.000** ✓ | (0.000, …) |
| 4 | A306③ y | 2.389 | 2.2408 | 1.5 + 1.2(0.7408) = **2.389** ✓ | (…, 0.7408) |
| 5 | A306② x | 5.199 | 4.6667 | 2 + 1.2(2.6667) = **5.200** ✓ | (2.6667, …) |
| 6 | A306② y | −2.79 | −2.075 | 1.5 + 1.2(−3.575) = **−2.79** ✓ | (…, −3.575) |
| 7 | A306④ x | −6.911 | −5.425 | 2 + 1.2(−7.425) = **−6.91** ✓ | (−7.425, …) |
| 8 | A306④ y | 1.60 | 1.5833 | 1.5 + 1.2(0.0833) = **1.60** ✓ | (…, 0.0833) |

> 反解 `p1 = W + (p2 − W)/1.2`，只用了日志里的 `p2`、夹具自己写的 `W` 和 `M`（⛔ 没读我们的常量）。
> 8 条**逐条对到小数点后 3 位**。另注：A306① 的 OkButton 设计 x = 0.000 —— 探针 `PromptPopup` 只给一颗钮，
> 走的是「只给 Ok ⇒ 藏掉 Cancel、Ok 居中」那一支（`Shell/PromptPopup.cs:197-204`）⇒ 是预期值，不是异常。

**为什么这不是「实现没问题、夹具没问题、只是运气不好」**（把话说死）：
1. `MenuDraw.PosInDesignSpace` 的适用范围**由它自己的文档写着**：`Shell/MenuDraw.cs:57-61`
   「⚠️ **只在「窗根在世界原点」时精确**（同 A228）：根一旦有偏移 `Rx`，正确的设计位置是 `(position − Rx)/k`，而这里拿不到 `Rx`」。
   夹具把 `Rx` 造了出来 ⇒ 断言量的是一个**不在契约内的态**，期望值 undefined。
2. 「基准件离窗根 ≥1 单位」这条前提对这四个调用点是**恒不成立**的：`basis` **就是**窗根 ⇒ 距离恒 0。
   夹具用一种错的办法去满足它（挪窗根），于是**同时**破坏了断言。
   （对照：真正满足这条前提的 `ShopScene` A306⑥ `basis = TimeCounter` 实测 `(-4.04,3.97)` 绿 ·
   `RewardsScene` A306⑤ `basis = Content` 实测 `(1.53,-1.16)` 绿 —— 都是**窗根的子件**，同一份夹具代码。）

**⛔ 为什么不是 (γ)**：`CheckScaleTwo` 与这四个调用点**全在 HEAD 里不存在**（`git show HEAD:…ShellScene.cs | grep -c CheckScaleTwo` = **0**），
是本批新加的（W6 的 A327，见 `资料/普查产出_1011/W6_A167_A232_A327.md:48-56` 的改动清单 + `:64-72` 的夹具表）
⇒ 红的是**新增断言**，生产行为这一批**没被它改坏**（前提前提+收尾全绿可证）。
⚠️ 更关键：**生产里这两式本来就逐位相同**（见 §六·1），所以「实现改坏了被夹具照出来」这个剧本在本处根本不成立。

---

## 三、若是 (α)：逐条给【可执行的正确判据文字】

### 3.1 前提那条写错了（8 条共用的根）

现状（`Editor/ShellScene.cs:294-296` · `Editor/CollectionScene.cs:601-603`）：

```
CheckTrue(Mathf.Abs(b1.x) > 1f || Mathf.Abs(b1.y) > 1f,
          $"（前提）{what}：**基准件离窗根 ≥1 设计单位**（实测 {b1.x:F2},{b1.y:F2} 世界）…");
```

**错在两处**：① 量的是「离**世界原点**」而不是「离窗根」；② 真正决定「除不除缩放有没有差别」的**不是基准的位置**，
而是 **`basis` 的父级那一级有没有被乘 M**（`PosInDesignSpace` 除的正是 `t.parent.lossyScale`，`Shell/MenuDraw.cs:74-78`）。

**替换成（三条一起，缺一条就是假绿）**：

```csharp
// 前提①：被除的那一级真的被乘了 M（= 两式之差从哪来的那一级）
CheckNear(basis.parent != null ? basis.parent.lossyScale.x : 1f, m, 1e-3f,
          $"（前提）{what}：**基准件的【父级】在态二被乘了 M**（`PosInDesignSpace` 除的正是这一级，"
        + "`Shell/MenuDraw.cs:74-78`）—— 父级单位缩放时新旧两式**恒等**，这条断言就等于没查");
// 前提②：可观测余量 —— 基准的设计位置离原点 ≥1 单位（偏差 = M(M−1)·|基准设计位置| = 0.24·|·|，容差 0.02）
CheckTrue(Mathf.Abs(b1.x - w1.x) > 1f || Mathf.Abs(b1.y - w1.y) > 1f,
          $"（前提）{what}：**基准件离【窗根】≥1 设计单位**（实测 {b1.x - w1.x:F2},{b1.y - w1.y:F2}）；{w1} = 窗根世界位置");
// 前提③：态二量到的是【k ≠ 1 下重算过的】局部位置（否则量的是同一份冻结值 ⇒ 恒等式）
```

### 3.2 ★ 那条（8 条逐条）

**A306① `PromptPopup.Local`（`OkButton` 的落位）**
```
★ A306① 态二 == M × 态一（x）：在「窗根挂在被乘 M 的父件下 + 窗根自身 localPosition=(2,1.5) + 态二重建过 OkButton」
   三条前提都成立时，`OkButton.position.x` 必须 == 1.2 × 态一那一份。
   改坏法：`PromptPopup.Local` 换回裸 `parent.position` ⇒ 偏 M(M−1)×2 = 0.48 世界单位 = **51.8px** ⇒ 红。
   ⛔ 不许把窗根挪走后来断 `p2 == M·p1`（窗根在原点之外时该式**恒不成立**，`Shell/MenuDraw.cs:57-61`）。
```
**A306③ `ImportDeckPopup.Local3`（`Window` 那层的落位）**：同上，把 `OkButton` 换成 `Window`、把 `PromptPopup.Local` 换成 `Local3`。
**A306② `DeckInfoPopup.Local3`（`Buttons` 那层的落位）** / **A306④ `PracticeModePopup.Local3`（`Army Selector` 那层）**：同上，宿主换 `Editor/CollectionScene.cs`。
（四条的**偏差**都写成 `M(M−1)×|基准的设计位置|`，取值见 §四 表——⛔ 别再写「0.24 × |窗根挪到的设计点|」，那是把**位移**当成了**基准的设计位置**。）

### 3.3 若只想让它**变绿**（⛔ 但必须同时改文案，否则是假绿）

1. 删掉 `Editor/ShellScene.cs:2689,2709` / `Editor/CollectionScene.cs:4853,4873` 那两句 `localPosition = new Vector3(2f,1.5f,0f);`
   （并且把 3.1 的前提②改成「**恒不成立 ⇒ 本夹具对 `basis == 窗根` 的调用点没有牙口**」）；
   则 `W == 0` ⇒ `p2 == M·p1` 成立并转绿。
2. 🔴 **同时必须把四处的「改坏法：换回裸 `parent.position` ⇒ 偏 …⇒ 红」删掉或改成**
   「本夹具只锁『整棵子树被刚性乘了 M』（`p2 = M·p1` 在窗根位于原点时对**任何**实现都成立）——
   ⛔ 它**照不出** `Local`/`Local3` 的改坏法；照得出那一档的探针在 `Editor/ShopScene.cs:2888-2958`（A294/A297/A298，**在 k≠1 下重建**）」。
   （不改文案 = 留一条「看着有依据」的假绿，正是 `CLAUDE.md` 铁律 12 点名过的病。）

---

## 四、若是 (β)/(γ)：最小改法 + 归属哪一件

**没有 (β)，没有 (γ)。** 但按上面的诊断给「**有牙口**」的修法（推荐，形状照判据原文
`资料/普查产出_1011/W4_子3.md:91-106`「**在 ×1.2 根下**开一个 `PromptPopup`」+ `Editor/ShopScene.cs:2888-2958` 的 A294 探针）：

**共用件 `CheckScaleTwo` 的四份**：**函数体一行都不用改**（它的签名本来就把 `winRoot` 与 `basis` 分开：
`CheckScaleTwo(GameObject winRoot, Transform basis, Func<Vector3> measure, float m, string what)`），
只改 ① 前提那两句文案（§3.1）② 四个调用点。

**四个调用点各改三样**（以 A306① 为模板，`Editor/ShellScene.cs:2680-2700`）：

```csharp
// ① 把 M 挪到【基准的父级】那一级（判据：PosInDesignSpace 除的是 t.parent.lossyScale）
var probe = new GameObject("A306① probe root");                  // 这一颗才是"被乘 M 的根"
var ppA = PromptPopup.Create(WindowsManager.EnsureHost(probe.transform), "A327 探针", "OK", null, null, null);
CheckTrue(ppA != null, "（前提）`PromptPopup` 建出来了");
if (ppA != null)
{
    // ② 窗根自己【留在被乘 M 的那一级下面】、(2,1.5) 是它相对那一级的位移（可观测余量 0.6 单位 = 65px）
    ppA.transform.SetParent(probe.transform, false);
    ppA.transform.localPosition = new Vector3(2f, 1.5f, 0f);      // ⛔ 别再加在窗根自己的 localScale 上
    ppA.TryOpen(null);                                            // 态一（开关关）建一遍 ⇒ p1 == 设计点
    // ③ 态二的 measure 里【重建】：`Open() → Build()` 先清空子件再建（`Shell/PromptPopup.cs:93-96`）
    CheckScaleTwo(probe, ppA.transform,
                  () => { ppA.TryOpen(null); return FindChildIn(ppA.transform, "OkButton").position; },
                  1.2f, "A306① `PromptPopup.Local`（`OkButton` 的落位）…");
}
```

- ② 之所以两边都要（**父级被乘 M** + **窗根在被乘那一级下有一个非零位移**）：`PosInDesignSpace(basis) = basis.position / M`
  只有这时才 ≠ `basis.position`；偏差 = `M(M−1)·|位移| = 0.24×2.5 = 0.6 单位 = 65px`（**正是断言原文写的那个 65px**）。
- ③ 之所以必须重建：不重建时被量的局部位置是 `k == 1` 那一趟冻结下来的值，新旧两式在那时**逐位相同** ⇒ 断言恒真。
- 四处重建口都在：`Shell/PromptPopup.cs:93-96` · `Shell/ImportDeckPopup.cs:92-95` · `Shell/DeckInfoPopup.cs:714-717` · `Shell/PracticeModePopup.cs:688-692`（四处的 `Build()` 首句都清空子件）。
- **归属**：夹具与四个调用点 = **W6（A327）**（`资料/普查产出_1011/W6_A167_A232_A327.md:48-56`、`:64-72`）；
  被它测的代码 = **W4（A306①②③④）**（`资料/普查产出_1011/W4_子3.md:43-61`）。
  ⚠️ W6 自己在 `:88-98`（§五·1/2）已如实记「8 处一条都没实跑」「A306①②③④ 的条件是我造的、且我推的」——
  本件把那条推演**复核成了结论**，并指出「造的姿势不对」。
- **四处同改**：4 份 `CheckScaleTwo` 是逐字同源（`ShellScene`/`CollectionScene`/`RewardsScene`/`ShopScene`），
  ⛔ 只改红掉的那两份会让同一夹具在四个宿主里两种行为。

---

## 五、判不了的（缺什么才能判）

1. **「这套夹具照不出改坏法」是代数推导 + 日志数值吻合（8 条逐条对到 3 位小数），不是实跑突变**。
   要实锤得做一次「把 `Local` 换回裸 `parent.position` + 跑 `ShellScene.Run` / `CollectionScene.Run`」——
   本件红线不许跑 Unity。⇒ 缺的是**一次突变实跑**。
2. `AttachToAnchor` 的父件（三个 Holder）`lossyScale == 1`、且 Holder 落在世界原点：本件是**读代码**得出
   （`Shell/WindowsManager.cs:603-607,622-630,805-808`），旁证 = `Editor/ShopScene.cs:2884-2886` 两条「商店窗根在世界原点」断言实测 0.00。
   ⇒ 缺的是**一次运行时 dump**（若要把它当成 §六·1 的判据用）。
3. 本件**没碰** `rewards.log` 的 11 条红与 `battle.log` 的 7 条红（不在简报范围）。
4. 没核 `Editor/SettingsScene.cs` / `Editor/MainMenuScene.cs` 是否也有「挪窗根」形状（grep 全库只命中 Shell/Collection 两处，
   见 §七 命令；那两处日志分别是 378/0 与 1834/0）。

---

## 六、顺手发现（⛔ 只报不改）

1. 🔴 **A306①～⑥ 这 6 条（含**已绿**的 A306⑤⑥）结构上都照不出「改坏法」** —— 它们量的是
   「**一个活着的、父链被刚性乘了 M 的子树里的节点**」，而 `p(M) = W + M·v`（`v` 冻结于 `k == 1` 那一趟）
   ⇒ `p2 == M·p1` 在窗根位于原点时对**任何**实现都成立（恒等式）。带牙口的只有**在 `k ≠ 1` 下重建**的探针：
   `Editor/ShopScene.cs:2888-2958`（A294：态二 `MenuDraw.Rect(a294p2,…)` 现建）·`:3180-3206`（A298：态二 `MenuDraw.Nine(a298p2,…)` 现建）。
   ⇒ 建议（只报）：A306⑤⑥ 的 `measure` 里各加一次重建就有牙口 —— `Editor/RewardsScene.cs:6252`（`measure` 里再调 `campTabA.RefreshNodes()`）·
   `Editor/ShopScene.cs:3238`（`measure` 里再调 `pgA327.Setup()`）；否则请在文案里删掉「改坏法 ⇒ 红」。
2. 🔴 **A306①②③④（W4 那四处码的改动）在生产里是 no-op，且不只是「今天观测不到」**：
   `basis == 窗根` ⇒ `PosInDesignSpace(basis)` 除的是**窗根的父级** = Holder（恒 1，`WindowsManager.cs:622-630`）
   ⇒ 新旧两式**在开关任何状态下都逐位相同**（`AttachToAnchor` 还把窗根钉在 `localPosition = 0`）。
   W6 §五·2 已记「三处今天在 M≠1 下也观测不到差异」，本件把范围**收窄到确定**（不是「今天」，是**该调用形状下永远**）。
   它们仍然值得留着（防御性的、口径更对），但**不能被说成「修好了一个带电的潜伏缺陷」**。
3. ℹ️ **`PracticeModePopup` 里确有「非根基准」的调用点**，那些**生产上真带电**（basis 的父级就是被乘 M 的窗根）：
   `Shell/PracticeModePopup.cs:494`（`Local3(_info,…)`）· `:502` · `:792`（`Local3(sel,…)`）· `:796`（`Local3(vp,…)`）·
   `:853`（`Local3(holder,…)`）· `:924` · `:1238`。
   ⇒ 若要把 A306④ 做成「既贴生产、又有牙口」，**改量 `Army Selector/Viewport`**（`vp.localPosition = Local3(sel, …)`，`:792`）
   比挪窗根更对；⚠️ 仍必须**在 k ≠ 1 下重建**（`ppA.TryOpen(null)` ⇒ `Build()`）。
4. ℹ️ **数红要按文案去重**：`Editor/ShellScene.cs:2726` 与 `Editor/CollectionScene.cs:4893-4894` 会把失败表再打一遍
   ⇒ 8 条 `✗` = 4 条失败（Shell）/ 4 条失败（Collection）。
5. ℹ️ **`CheckScaleTwo` 的容差 0.02 单位（2.16px）**：本件的四条偏差（0.40/0.30 世界单位 = 43.2/32.4px）**远超**它
   ⇒ 这四条不是「擦边假红」，是**整条恒等式不成立**，别拿调容差去糊。

---

## 七、我读过的文件与命令

**日志**（只读）
- `d:/4/_tmp_view/shell.log`（10402 行；`合计` 在 `:10312`；失败在 `:10102/10117/10243/10258`，重打在 `:10323-10356`）
- `d:/4/_tmp_view/collection.log`（24277 行；`结束` 在 `:24225`；失败在 `:23994/24009/24167/24182`，重打在 `:24226-24229`）
- 对照（证明「同夹具 + 内层基准 ⇒ 绿」）：`d:/4/_tmp_view/shop.log`（`1787 通过 / 0 失败`）· `rewards.log`（A306⑤ 那 7 行全绿）· `settings.log`（`通过 378 · 失败 0`）· `menu.log`（`1834 通过 / 0 失败`）

**源码**（只读）
- `Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs`（`:287-315` `CheckScaleTwo` · `:2675-2728` 夹具与收尾 · `:2726` 重打）
- `Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`（`:577-622` · `:4840-4895`）
- `Unity/MyGame/Assets/CardPresentation/Editor/ShopScene.cs`（`:620-667` · `:2860-2960` A294 · `:3155-3255` A298/A306⑥）
- `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs`（`:6240-6300` A306⑤/C）
- `Shell/MenuDraw.cs`（`:24-84` `Local` / `PosInDesignSpace`）· `Shell/TransformScalerBySmallScreenUI.cs`（`:56-151`）
- `Shell/WindowsManager.cs`（`:346-387` `TryOpen`/`ApplySmallScreenScale` · `:595-630` `EnsureHost`/`MakeHolder` · `:792-809` `AttachToAnchor`）
- `Shell/PromptPopup.cs`（`:93-96` · `:197-215` · `:258-292`）· `Shell/ImportDeckPopup.cs`（`:92-129` · `:220-260`）
- `Shell/DeckInfoPopup.cs`（`:714-720` · `:828-845` · `:1310-1345`）· `Shell/PracticeModePopup.cs`（`:653-690` · `:755-760` · `:474/494/792/796/853/921/924/1238` · `:1593-1605`）
- `资料/普查产出_1011/W4_子3.md`（`:91-106` §四·b 判据）· `W6_A167_A232_A327.md`（`:20-46` §二·3 · `:48-56` 改动清单 · `:64-86` 夹具表 · `:88-98` §五·1/2）

**命令**（全部只读）
```
grep -n "✗\|合计\|结束" /d/4/_tmp_view/{shell,collection}.log
awk 'NR>=X && NR<=Y {printf "%d: %s\n", NR, substr($0,1,80)}' …      # 定位 ✗ / 合计 的真实行号
grep -rn "CheckScaleTwo|PosInDesignSpace|A306" --include=*.cs Shell/ Editor/ Core/
grep -rn "把窗根挪到|窗根挪到的设计点" Editor/*.cs                      # 全库只有 Shell/Collection 两处
grep -n "static void AttachToAnchor" -A 30 Shell/WindowsManager.cs
git show HEAD:Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs | grep -c CheckScaleTwo   # = 0 ⇒ 本批新增
git diff --stat -- Editor/*Scene.cs                                  # 只读
```

**没做的**：⛔ 没改任何文件（除本报告）· ⛔ 没跑 Unity / 自检 / 类型检查 · ⛔ 没动 git 写命令 · ⛔ 没改两张正本与 `资料/` 既有文件。
