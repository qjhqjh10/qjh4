# G8 · `CardView.CreatedByVisible` 的「名字闸」+ `RevealWait2` 判据结论（2026-10-24）

> 执行代理 **G8**。任务来源 = `资料/普查产出_1024/G7_reveal_state2与相位.md` 的**顺手发现 2 与 3**（正文里是 §六·2 与 §六·5）。
> 判据 = ① `d:/2/tools/decomp_full/BattleCardUI__DisplayCreatedByText.c`（亲读全文 49 行）
> ② `d:/2/tools/decomp_full/CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c`（亲读全文 107 行）
> ③ 现读代码：`Core/CardView.cs` · `Battle/BattleDriver.cs`（只读）· `Editor/BattleScene.cs`。
> ⛔ 未跑 Unity / 未跑 ruleprobe / 未动 git / 未改正本。
> ✅ **只改了两个白名单文件**：`Core/CardView.cs` · `Editor/BattleScene.cs`。
> ⛔ **没碰**其它 `Core/**`、`Battle/**`（含 `BattleDriver.cs`，见 §四·1）、`Shell/**`、
> `Editor/CardBaseDemo.cs` / `CardFaceProbe.cs`、`RuleEngine/**`、`Net/**`、`工具/**`、`CLAUDE.md`、`项目任务.md`。
> ✅ `TMPDIR=/tmp/wf_g8 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> ✅ 两个文件改完仍是**纯 CRLF**（`CardView.cs` 11364/11364、`BattleScene.cs` 42655/42655，lone-LF = **0**）。

---

## 一 · 逐件改动

### 1·1 `Core/CardView.cs` —— `CreatedByVisible` 补上原版**第一道闸**

**判据（亲读 `BattleCardUI__DisplayCreatedByText.c`，逐行）**

| 行 | 原文要点 | 含义 |
|---|---|---|
| `:14-16` | `if (*(longlong *)(param_1 + 0xd8) != 0 && ...) SetActive(gameObject, 0, 0)` | **先无条件把节点关掉** |
| `:17-23` | `uVar3 = *(*(param_1 + 0x248) + 0x268)`（= `createdBy`）→ `op_Equality(uVar3, 0)`；相等则**整段跳过** → `:43 return` | **第一道闸：`createdBy == null` ⇒ 不印** |
| `:24-31` | `if (param_2 != 8) { if (createdBy->+0x40 != 0) return; if (param_2 != 10) return; }` | **第二道闸：档位**（8 = `inHandShowing`；10 = `inHandPlaying` 且 `isPlayer == 0`） |
| `:33-41` | 取名字 → `GetCreatedByText` → `SetupCreatedByText` | 两道闸都过了才点亮 |

⇒ 原来我们**只有第二道**。现在 `CreatedByVisible` 变成：

```csharp
if (_faceMode == CardFace.Board) return false;                  // 场上（inPlay 那几档）—— 原有
if (string.IsNullOrEmpty(_createdByName)) return false;         // ← 🆕 原版第一道闸（`:22`）
if (_createdByState == CreatedByState.InHandShowing) return true;
return _createdByState == CreatedByState.InHandPlaying && !_createdByIsLocalPlayer;
```

- ⛔ **档位那半一个字没动**（`InHandShowing` / `InHandPlaying && !isLocalPlayer` 原样）。
- **为什么放在 `CreatedByVisible` 而不是 `FillCreatedBy`**：这个谓词是全仓**唯一显隐口**
  ——`FillCreatedBy`（`CardView.cs:4130`）与 `SetFace`（`CardView.cs:1906`）两处 `Show(...)` 都只读它
  ⇒ 「判据只此一处」，不会再长出第二个分叉。
- 顺手在这个属性 doc 里写清了「今天不可观测」的原因（见 §五·1），免得下一个会话把它当成「已修可见缺陷」。

### 1·2 `Core/CardView.cs` —— 新增 `public bool CreatedByVisibleForTest`

放在 `CreatedByShown` 后面（`:3022-3029`）。**它是这条断言能成立的前提**，理由：

> `CreatedByShown` **证明不了**这道闸 —— 名字为空时 `Fill`（`CardView.cs:4385-4393`）会
> `t.text = ""` + `TmpFont.Kill(t.gameObject)` 并把 `_createdBy` 置 **null** ⇒ 有没有那道闸，
> `CreatedByShown` 都是 `null`。拿它写断言 = **恒绿 = 自证**。
> `CreatedByVisibleForTest` 读的是**真正驱动两处 `Show(...)` 的谓词本身**，所以「名字空 ⇒ false」才是一条真能红的断言。
> ⚠️ 只是**读**；产品代码 ⛔ 别用它。

### 1·3 `Editor/BattleScene.cs` —— 在 `W62/D5①` 那一块尾部加 **2 条**断言

位置：`BattleScene.cs:38490-38512`（紧跟在原来的「判别式：只改状态就有两种结果」之后、
`if (v != null)` 块内、`finally { DestroyImmediate(probe); }` 之前）。

| # | 断言 | 读的是什么 |
|---|---|---|
| ① | 打出档（`InHandPlaying`）+ 对手（`isLocalPlayer: false`）、**没有创建者** ⇒ `CreatedByVisibleForTest == false` **且** 节点没亮 | 谓词 + `CreatedByShown` |
| ② | **同一张卡、同一档**，**只把创建者填上** ⇒ `CreatedByVisibleForTest == true` **且** `CreatedByShown` 非空 | 同上 |

### 1·4 断言条数（算术，**未实测**）

宿主现读 4813 / 0（G1b 之后）⇒ G7 **+11** = 4824 ⇒ 本件 **+2** = **预期 4826 / 0**。
⚠️ 我只加了自己那 2 条；若另一个代理本轮也在 `BattleScene.cs` 里加过断言，以复跑报出的「断言合计」为准
（**判绿红看断言合计、不看退出码** —— `BattleScene.Run` 的 139 是已知间歇）。

---

## 二 · 调用点核查（全仓 `SetCreatedBy` / 两个属性）

`grep -rn "SetCreatedBy|CreatedByShown|CreatedByVisible" MyGame/Assets` 的全部命中，逐条判：

| 调用点 | 传的名字 | 档位 | 本改动后谓词 | 可见文本 / `activeSelf` 变不变 |
|---|---|---|---|---|
| `BattleDriver.cs:7779`（G7 的 reveal state 2） | `null` | `Hidden` | false → false | **不变**（两道闸都挡住） |
| `BattleDriver.cs:21559`（中央展示 state 2） | `creator`，**前一格 `:21558` 已 `if (creator == null) return`** | `InHandPlaying`，`isLocalPlayer: false` | true → true | **不变** |
| `BattleDriver.cs:21570`（中央展示 state 3） | **`null`**（实参就是空！） | **默认参 `InHandShowing`**（`:2955`） | **true → false** | **不变**（`Fill` 收到空文本拆节点；见 §五·1） |
| `BattleDriver.cs:22698-22701` `PushCreatedByStates` | `Ctx.CreatedByOf(h[i])`，**可能为 null**（`RuleEngine/Core/BattleContext.cs:734-739`） | 悬停 `InHandShowing` / 未悬停 `InHand` | 悬停时 **true → false** | **不变**（同上） |
| `CardView.cs:1906`（`SetFace`，消费方） | —— | 读谓词 | 名字空时 true→false | **不变**（此时 `_createdBy` 本来就是 null） |
| `CardView.cs:4130`（`FillCreatedBy`，消费方） | —— | 读谓词 | 同上 | **不变** |
| `Editor/CardBaseDemo.cs:1572/1573/1626` | 真 `CardDef` | 默认 `InHandShowing` | true → true | **不变** |
| `Editor/CardBaseDemo.cs:1595` | `null` | 默认 `InHandShowing` | **true → false** | **不变**（它自己断的就是 `CreatedByShown == null`，`:1596`，仍绿） |
| `Editor/CardBaseDemo.cs:1607` 那条写的是 `SetFace(Board)` 后 `CreatedByShown == null` | —— | —— | —— | **不变** |

**结论：零行为变化** —— 画面、文字、`activeSelf` 一处都不变（这符合简报的预期「本改动应零行为变化」）；
变的只是**谓词在「名字空 + 展示/打出档」这一格的返回值**（`true → false`）。
⚠️ 但**机制**和 G7 写的不一样，见 §五·1 / §五·2。

---

## 三 · 断言 + 灭自证

| # | 断言 | 🧨 怎么让它红 | 灭自证怎么成立 |
|---|---|---|---|
| ① | 打出档 + 对手 + **无创建者** ⇒ 不印（`CreatedByVisibleForTest == false`、节点没亮） | 把 `CreatedByVisible` 里 `IsNullOrEmpty(_createdByName)` 那句删掉 ⇒ `createdByState == InHandPlaying && !isLocalPlayer` 那一支返回 true ⇒ **红** | 与 ② 构成一对：**同一张卡、同一档、只差「名字有没有」** |
| ② | 同上那一档 + **有创建者** ⇒ 印（`CreatedByVisibleForTest == true`、`CreatedByShown` 非空） | 把档位那半写死成「总不印」⇒ **红** | 同上 |

🔴 **为什么非要用 `CreatedByVisibleForTest` 而不能用 `CreatedByShown`**：
名字为空时 `Fill` 会把节点整个拆掉（`CardView.cs:4385-4393`）⇒ 有没有那道闸 `CreatedByShown` 都是 `null`
⇒ 那两条就变成**恒绿的自证**（本仓 CLAUDE.md 第三节「断言要盯原版参数，不要自证」）。
这一点是本次与简报给的字面要求**唯一一处偏离**，理由与实测代码如上。

⚠️ 两条断言的**期望值来自原版**（`DisplayCreatedByText.c:22` 那道 `op_Equality` bail + `:24-31` 的档位），
⛔ 不是回读 `CreatedByVisible` 自己证明自己。

---

## 四 · 没查清 / 没做

### 4·1 `RevealWait2 = 0.78`：**「该用而没用」= 真缺陷，但今天不可观测 ⇒ 按简报停手，没接**

**判据链（亲读 `CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c`）**

| state | 行 | 动作 |
|---|---|---|
| 0 | `:29-48` | `SetCardVisibility(card2d, 0)` · `DOMove(0.15)` · `DORotate(0.1)` · `WaitForSeconds(DAT_1834b2dc4 = 0.1)` |
| 1 | `:56-68` | `DOScale(目标, param_0x2c = 0.3)` · `WaitForSeconds(0.3 × 0.66 = 0.198)` |
| 2 | `:85-99` | `DisplayCreatedByText(state+0x228)` · `ToggleHighlight(false)` · `WaitForSeconds(0.3 × 2.6 = **0.78**)` |
| 3 | `:74-81` | **`BattleCardUI +0xd8` 的 `SetActive(false)`** → `return 0`（协程结束） |

- `+0xd8` 这个偏移**就是** `DisplayCreatedByText.c:14` 开头那个被 `SetActive(false)` 的节点（同一个 `BattleCardUI` 实例）
  ⇒ **state 3 的动作 = 把「由谁创建」那行字结点收掉**。
- state 3 的时刻 = `0.1 + 0.198 + 0.78` = **1.078 s** = `RevealWait1 + RevealScaleHold + RevealWait2`。
- 我们 `TickOffensiveReveal` **只模拟到 state 2**（`RevealInnerState2At = 0.298`，`BattleDriver.cs:7774-7781`），
  **state 3 从来没有接线** ⇒ `RevealWait2` 只被 `RevealInnerPhaseForTest` 之外的地方引用不到（全仓只有定义行 `:7589`）。
  ⇒ **判为「该用而没用」= 真缺陷**（铁律 11：与原版不符就记下来、之后完全复刻）。
- **为什么今天没有可观测差异**：揭示卡恒 `createdBy == null` ⇒ 节点从没被点亮 ⇒ 1.078 s 那次 `SetActive(false)` 是空操作。
- **为什么我没有接上（也没有删）**：
  - 简报与白名单明写 `BattleDriver.cs` **只读** —— `RevealWait2` / `TickOffensiveReveal` /
    `RevealInnerPhaseForTest` 三处全在 `Battle/BattleDriver.cs` ⇒ **越白名单**。
  - **也不能删**：0.78 是现读核实的原版常数（`DAT_1834b3190 × 2.6`，G7 §2·1），删了就丢判据；
    按铁律 11，这条的正解是**接上**而不是删。
  - ⇒ 按简报「若发现该用而没用 ⇒ 停手写进报告（别自己接上）」处理。
- **建议做法（留给 `BattleDriver` 的负责人 / reveal 线）**：
  1. 加 `const float RevealInnerState3At = RevealWait1 + RevealScaleHold + RevealWait2;`（= 1.078）
     —— ⛔ **别直接用 `RevealWait2`**（那一段是**相对等待**，不是绝对时刻）；
  2. 加一个「每张揭示卡一次」的闩 + 一个计数（同 `_revealInnerState2Done` / `_revealInnerState2Calls` 的形状，
     复位点同 `BeginOffensiveRevealOrApply` 建卡那一处）；
  3. 动作 = `SetCreatedBy(null, CardView.CreatedByState.Hidden, isLocalPlayer: false)`
     （与 state 2 同一个动作 —— 原版 state 3 就是收掉那行字）；
  4. `RevealInnerPhaseForTest` 加第 4 项（state 3 时刻），断言写 **1.078**（原版字面量相加，⛔ 别回读常量）；
  5. ⚠️ 这一拍一定落在 `_revealStage == 0` 期间（外层 2.0 s 里），所以放在 state 2 那块旁边即可，
     **别挪到外层 stage 2**（那时揭示卡已被 `Kill`、`_revealCard` 已 null —— `:7794`）。

### 4·2 没查清：原版 state 0 的 `Card2DController__SetCardVisibility(card2d, 0)`（`:29`）

原版起手**先把 2D 卡体设成某个可见性**（实参 `0`），而我们的揭示卡 `CardView.Create(...)` 建出来就是可见的。
那个 `0` 到底是「隐藏 / 正面 / 背面」**没查**（需要翻 `Card2DController__SetCardVisibility.c` 的枚举与其它调用点）⇒ **未查清**。
⚠️ 若 `0` = 隐藏，那我们这条 reveal 缺了「起手先藏、后面某处再显」的一拍；那条链不属本件白名单（在 `BattleDriver`），
**只报不改**，留给 reveal 线。

### 4·3 没做：真机 / Unity

红线：没跑 Unity ⇒ §1·3 那 2 条断言**一行都没在编辑器里跑过**；§1·4 的「4826」是算术不是实测。
也没有真机截图（这道闸今天不改变任何画面，本来也拍不出差异）。

---

## 五 · 顺手发现

1. 🔴 **G7 §六·5 的机制描述要订正（铁律 5 —— 原记录与事实不符）**：
   G7 写「任何『传 `null` 创建者 + `InHandShowing`/`InHandPlaying` 档』的调用点，都会把一个**空文字节点点亮**」。
   **不成立**：`Fill`（`CardView.cs:4385-4393`）遇到空文本会 `t.text = ""` + `TmpFont.Kill(t.gameObject)`
   并把 `_createdBy` 置 **null** ⇒ 节点**被拆掉**，不存在「亮着的空节点」。
   真实后果是**谓词值错**（`CreatedByVisible` 返回 `true`），今天**在画面上完全不可观测**。
   ⇒ 这仍然是该补的一格（谓词错是下一次照着它写调用点的源头），但**它不是现行可见缺陷**，别在台账里写成「已修的可见 bug」。
   （本件的代码注释与断言文案都按「谓词闸」写，没有沿用「点亮空节点」这个说法。）
2. 🔴 **G7 写「今天**没有**这样的调用点」也不准** —— 有**两处**「null 名字 + 展示档」的真调用：
   - `BattleDriver.cs:21570`：`if (creator != null) v.SetCreatedBy(null);` —— **实参就是 `null`**，
     档位走默认参 `InHandShowing`（`CardView.cs:2955`）。那个守卫守的是**外层变量**（「有创建者才走到这一拍」），
     不是「传进去的创建者」。
   - `BattleDriver.cs:22698` `PushCreatedByStates`：`Ctx.CreatedByOf(h[i])` 对**不是被造出来的牌**返回 `null`
     （`BattleContext.cs:734-739`），而悬停那一档是 `InHandShowing`。
   ⇒ 它们今天**不发字**只是因为 `Fill` 把节点拆了，**不是因为被那道闸挡住**。所以这次补的闸是**真的在挡东西**（挡谓词）。
3. ⚠️ **`BattleDriver.cs:21570` 那个「守卫与实参不一致」的写法**建议将来顺手改成显式档位
   （例如 `v.SetCreatedBy(null, CardView.CreatedByState.Hidden, isLocalPlayer: false)`，与 G7 在 `:7779` 的口径一致），
   否则下一个读代码的人会以为它「传的是创建者」。**只报不改**（`Battle/**` 不在本件白名单）。
4. ℹ️ `PushCreatedByStates` 对「没有创建者」的手牌**每帧仍会推一次** `SetCreatedBy(null, …)`；
   `SetCreatedBy` 的去重（名字 + 状态 + `isLocalPlayer` 三者都没变就早退，`CardView.cs:2962-2963`）
   让它在稳态是空转 ⇒ 不构成缺陷，只是知道了这条路径**确实会走到**。
5. ℹ️ `CreatedByState` 的默认参是 `InHandShowing`（`CardView.cs:2955`，doc 已解释「给自检/非手牌形态用」）——
   这正是 `:21570` 传空却被判进「展示档」的根因。改默认值会波及 `CardBaseDemo` 那几条断言（它们靠这个默认档），
   ⇒ **不建议动**，建议按第 3 条在调用点显式写档。
