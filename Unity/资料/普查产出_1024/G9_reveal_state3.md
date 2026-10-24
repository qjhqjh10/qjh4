# G9 · 进攻卡 reveal `state 3` 接线 + 两处读法订正（2026-10-24）

> 执行代理 **G9**。任务来源 = `资料/普查产出_1024/G8_CreatedBy与RevealWait2.md` §四·1（判据 + 5 步接法）与 §五·2/3。
> 判据亲读：① `d:/2/tools/decomp_full/CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c:74-81`（state 3）
> ② `d:/2/tools/decomp_full/BattleCardUI__DisplayCreatedByText.c:14-16`（`+0xd8` 那个节点）
> ③ **独立旁证** `d:/2/tools/decomp_full/BattleCardUI__SetObjectVisibility.c:103-134`（见 §六·1）。
> ⛔ 未跑 Unity / 未跑 ruleprobe / 未动 git / 未改正本。
> ✅ **只改了白名单三件**：`Battle/BattleDriver.cs` · `Editor/BattleScene.cs` · `资料/普查产出_1024/G7_reveal_state2与相位.md`（只做铁律 5 订正）。
> ⛔ **没碰** `Core/**`（`CardView.cs` 一个字未动）· `Shell/**` · `Editor/CardBaseDemo.cs` / `CardFaceProbe.cs` · `RuleEngine/**` · `Net/**` · `工具/**` · `CLAUDE.md` · `项目任务.md`。
> ✅ `TMPDIR=/tmp/wf_g9 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> ✅ 两个 `.cs` 改完仍**纯 CRLF**（`BattleDriver.cs` 28244/28244 · `BattleScene.cs` 42696/42696，lone-LF = **0**）；`G7.md` 仍纯 LF（220/220）。

---

## 一 · 接线位置 / 闩 / 复位点

### 1·1 时刻常量（`BattleDriver.cs:7593-7602`）

```csharp
const float RevealInnerState3At = RevealWait1 + RevealScaleHold + RevealWait2;   // = 1.078 s
```

- 判据：原版三个 `WaitForSeconds` **串行** —— `_d__264__MoveNext.c:41-46`（state 0，0.1）+ `:61-65`（state 1，0.198）+ `:95-100`（state 2，0.78）。
- ⛔ **不是 `RevealWait2`（0.78）** —— 那是 **state 2 之后那段相对等待**，当阈值用会**早 0.298 s** 收字。
  顺手把 `RevealWait2` 那行的注释改清了（「相对等待，别当绝对时刻」），G7 §六·2 说的「只有定义、没有使用」现在**不再成立**。

### 1·2 动作（`BattleDriver.cs:7807-7823`，在 `TickOffensiveReveal` 内）

```csharp
if (_revealStage == 0 && _revealCard != null
    && !_revealInnerState3Done && _revealT >= RevealInnerState3At)
{
    _revealInnerState3Done = true;
    _revealInnerState3Calls++;
    _revealCard.SetCreatedBy(null, CardView.CreatedByState.Hidden, isLocalPlayer: false);
}
```

- 位置：**紧跟 state 2 那一块之后、四段 stage 判定之前**（同 G7 的形状与理由 —— 掉帧时一帧 `dt` 很大也不会被 stage 跃迁吞掉）。
- 动作语义：原版 state 3 = `BattleCardUI +0xd8` 的 `SetActive(false)`，而 `+0xd8` **就是**「由谁创建」那行字的节点
  ⇒ **收掉那行字**（⛔ 不是「清名字后重印」）。我们的等价物 = 同一个 `SetCreatedBy(…, Hidden, …)`
  （`CreatedByVisible` 谓词 false ⇒ 那一层 `Show(false)`）。
- 落在 `_revealStage == 0` 那 **2.0 s** 之内（1.078 < 2.0）⇒ ⛔ 没挪到外层 stage 2（那时卡已被 `Kill`、`_revealCard` 为 null）。

### 1·3 闩 / 计数 / 复位点

| 成员 | 位置 | 作用 |
|---|---|---|
| `bool _revealInnerState3Done` | `:7665` | **每张揭示卡一次**的闩 |
| `int _revealInnerState3Calls` | `:7667` | 真被调过**几次**（自检口：闩证明状态翻过，计数证明动作发生过） |
| `RevealInnerState3DoneForTest` / `RevealInnerState3CallsForTest` | `:7685` / `:7687` | 自检读口 |
| **复位点** | `:7752-7754`（`BeginOffensiveRevealOrApply` **建新卡那一格**，紧跟 G7 的 state 2 复位） | 与 state 2 **同一处** |

🔴 复位点选在「建新卡」而不是 `Begin`：`BeginOffensiveRevealOrApply` 在**联机局会被调两次**
（后手方先落防御卡、对面那条进攻卡到了再调一次）—— 只在 `Begin` 复位会让**第 2 张揭示卡静默不跑**。
这条理由与 G7 完全同源（G7 §1·2），我照抄并写进了代码注释。

### 1·4 自检出口

`RevealInnerPhaseForTest`（`:7691-7692`）**扩成 4 项**：
`{ state2 时刻, 缩放前等待, 缩放时长, **state3 时刻** }`。doc 里的判据行也补了 `:95-100`。
⚠️ 全仓 `grep RevealInnerPhaseForTest` 只有**两处**（定义 + `BattleScene.cs:4841`），后者那条 `Length == 3` 已同步改成 **4**。

---

## 二 · 断言逐条（`Editor/BattleScene.cs:4902-4933`，新增 **7 条**）

接在 G7 那一段的末尾（当时停在 t = 0.81）。原来那一句 `AdvanceOffensiveRevealForTest(1.09f)` 拆成
**0.26 + 0.02 + 0.5 + 0.31 = 1.09**，末拍仍落在 **t = 1.90** ⇒ 下面 D6 那一段（终点宽 / 缩放 1.4）的读数逐值不变。

| # | 行 | 断言 | 🧨 怎么让它红 |
|---|---|---|---|
| 1 | `:4902` | ★ `phase.Length == 4` 且 `phase[3] ≈ **1.078**`（= 0.1 + 0.198 + 0.78，**原版字面量相加**） | 把常量写错（例如只用 0.78）⇒ 红 |
| 2 | `:4906` | ★ **灭自证**：`phase[3]` **不是 0.78**（差 > 0.1） | 把 `RevealInnerState3At` 写成 `RevealWait2` ⇒ 本条与 #4 一起红 |
| 3 | `:4911` | （前提）t = 0.81 时闩 = false、计数 = 0 | —— |
| 4 | `:4915` | **①** t = **1.07**（< 1.078）⇒ **未触发**（闩仍 false、计数 0；`_revealStage` 仍 0） | 阈值写成 0.78 ⇒ 上一拍 0.81 就已触发 ⇒ 红；挪成「起手就跑」⇒ 红 |
| 5 | `:4920` | **①** t = **1.09**（≥ 1.078）⇒ **触发且计数 = 1**（闩 = true） | 删掉 `TickOffensiveReveal` 里那一段 ⇒ 红 |
| 6 | `:4924` | …那一行字**仍然收着**（`CreatedByShown == null`）—— 如实记「零可观测差异」，⛔ 别写成可见修复 | （弱的旁证，不是 ★） |
| 7 | `:4929` | **②** 再推 0.5 s（t = 1.59）⇒ 计数**仍为 1** | 去掉 `_revealInnerState3Done` 闩 ⇒ 第 0 段里每帧都调 ⇒ 红 |

**③「把阈值写成 0.78 ⇒ ① 红」怎么落地**：#2 是**结构性互斥**的一对（1.078 与 0.78 不可能同时满足，差 0.298）；
而 #4 是**行为上的那一红** —— 阈值一旦是 0.78，t 到 0.81（G7 的 ② 那一拍）就已跨过，t = 1.07 的「未触发」立刻破。
两条独立成立，不是同一句话写两遍。

**为什么 #1/#2 要用 `RevealInnerPhaseForTest` 而不用 `RevealInnerState3At` 自己**：那是被测常量，
读它 = 自证（G7 #1~#3 同口径）。这里 #1/#2 的期望值来自**原版三个 `WaitForSeconds` 的字面量相加**。

---

## 三 · `BattleDriver.cs:21570` 那处改动（显式档位）

**改前**
```csharp
if (creator != null) v.SetCreatedBy(null);     // 实参恒 null，档位吃默认参 InHandShowing
```
**改后**（`:21612-21619`）
```csharp
if (creator != null)
    v.SetCreatedBy(null, CardView.CreatedByState.Hidden, isLocalPlayer: false);
```

- 歧义在哪：**守卫**看的是外层变量「有创建者才走到这一拍」，而**实参**恒 `null`、档位又吃默认参
  `InHandShowing`（= *展示档*）⇒ 下一个读代码的人会以为「传进去的是创建者」。
- 语义：原版这一拍就是把那行字收掉（`…MoveNext.c:79-80` 的 `SetActive(false)`）⇒ 显式 `Hidden`
  与 reveal 那边 state 2/3 同口径（判据只留一种写法）。
- **零行为变化**：改前 `null` + `InHandShowing` 在 G8 补了「名字空 ⇒ 不印」那道闸之后也是**不印**；
  改后 `null` + `Hidden` 同样不印。`Fill` 的落点（拆节点）也一样。
- ⛔ **`CreatedByState` 的默认参一个字没动**（`CardBaseDemo` 有断言靠它，见 G8 §五·5）。

---

## 四 · G7 报告订正了两处（铁律 5，原文划掉 + 更正痕）

改的是 `资料/普查产出_1024/G7_reveal_state2与相位.md` §六·5（原文 `~~删除线~~` 保留，后面接更正）：

### 4·1 「会点亮一个**空文字节点**」→ **不成立**

- **原来写**：任何「传 `null` 创建者 + 展示/打出档」的调用点，都会把**一个空文字节点点亮**。
- **实际**：`Fill`（`CardView.cs:4385-4393`）收到空文本会 `t.text = ""` + `TmpFont.Kill(t.gameObject)`
  并把 `_createdBy` 置 **null** ⇒ **节点被拆掉**，不存在「亮着的空节点」。
- **真后果**：只是**谓词值错**（`CreatedByVisible` 返回 `true`），**今天画面上完全不可观测**。
- **错因**：只读了谓词、没读消费方 `Fill` 的空文本分支。

### 4·2 「今天**没有**这样的调用点」→ **不准**

- **原来写**：手牌那条传的是真有创建者的卡；打出的卡那条被 `if (creator == null) return;` / `if (creator != null)` 挡住了 ⇒ 潜伏。
- **实际**：有**两处**「`null` 名字 + 展示档」的真调用 ——
  ① `BattleDriver.cs:21570`（本件已改）原来 `if (creator != null) v.SetCreatedBy(null);` —— **实参就是 `null`**，
  档位走默认参 `InHandShowing`；那个守卫守的是**外层变量**；
  ② `PushCreatedByStates`：`Ctx.CreatedByOf(...)` 对**不是造出来的牌**返回 `null`，悬停那一档是 `InHandShowing`。
- 它们今天不发字**只是因为 `Fill` 把节点拆了**，**不是**被那道闸挡住 ⇒ G8 补的那道闸是**真在挡谓词**。
- **错因**：只看了 state 2 那一格（`:21558`）的早退，没读 `:21570` 的实参。

顺带在 §六·4 补了一条**事实更新**（不是订正，是数组变长了）：`RevealInnerPhaseForTest` 现在 **4 项**，
`Length` 要按 4 读 —— 否则下一个会话照 G7 的「3 项」去写断言会红。

---

## 五 · 预期读数（算术，**未实测**）

| 项 | 值 |
|---|---|
| 宿主基线（G1b 后） | 4813 |
| G7（内层 state 2 + 相位） | +11 ⇒ 4824 |
| G8（名字闸） | +2 ⇒ 4826 |
| **G9（本件）** | **+7 ⇒ 4833** |

⚠️ 简报按「本件 +3」算是 **4829**；我实际加了 **7** 条（brief 说「至少 3 条」，而 G8 §四·1 的第 4 步还要求
「`RevealInnerPhaseForTest` 加第 4 项、断言写 1.078」，那条本身就是一条断言）——
多出来的 4 条 = **phase 常量** / **灭自证 0.78** / **前提（闩起始为 false）** / **「那行字仍收着」的旁证**。
⇒ **以复跑报出的「断言合计」为准**（`BattleScene.Run` 的退出码 139 是已知间歇，⛔ 别看退出码）。

**要不要重跑**：要（`BattleScene.Run`）—— 本件动了 reveal 泵 + 新增 7 条断言。**没有跑 Unity**（红线）。

---

## 六 · 顺手发现（只报，未改）

1. ✅ **`+0xd8` = 「由谁创建」节点，有独立旁证**（加强 §1·2 的判据，不是新结论）：
   `BattleCardUI__SetObjectVisibility.c:103-134` 里有一段**与 `DisplayCreatedByText` 逐句同形**的代码 ——
   先 `SetActive(+0xd8, 0)`，再取 `*(BattleCardUI + 0x248) + 0x268`（= `createdBy`）判 `op_Equality(…, 0)`，
   过闸后 `RawCardScript__GetLocalizedCardName` → `SupportMethods__GetCreatedByText` → `BasicCardUI__SetupCreatedByText`。
   `grep -l "0xd8" BattleCardUI__*.c` 只有这两个文件 ⇒ **`+0xd8` 就是那一层的 `Text` 组件**，没有第二个读者。
   （`SetObjectVisibility` 里那段大概是编译器把 `DisplayCreatedByText` 内联进去了 —— 未深究。）
2. ⚠️ **本件接的 state 3 在今天的路径上恒为空转，且这不只是「值没变」而是「结构上不可能变」**：
   state 2（0.298 s）已经把三元组设成 `(null, Hidden, false)`，state 3 再传**完全相同的三元组**
   ⇒ `SetCreatedBy` 的去重（`CardView.cs:2962-2963`）直接早退 ⇒ 连 `Fill` 都不会跑。
   ⇒ **计数 `_revealInnerState3Calls` 是这条线今天唯一的可观测证据**（这也是断言 #5/#7 只能盯计数的原因，如实写清）。
   🔮 将来 `revealOrder`（`AbilityEffect.revealOrder = 280`）真接线时，state 2 会传**真创建者 + 打出档**（文字**真的亮**），
   state 3 才会变成**看得见的一收** —— 那时两句都要改（G7 §1·3 已记同一条）。
3. ⚠️ **两条链的相位是同一组数，但各写一份常量**：
   reveal 内层用 `RevealWait1 0.1 / RevealScaleHold 0.198 / RevealWait2 0.78`；
   「打出的卡居中展示」那条（`BattleDriver.cs:21450/21452/21454`）用
   `PlayedCardCenterStraightenTime 0.1 / HoldMul 0.66 / WaitMul 2.6`（`hold = 0.3` ⇒ 0.198 / 0.78）—— **逐值相等**。
   按「两处写同一条规则 = 迟早不一致」这算同族；但原版它们是**两个不同协程**
   （`_DisplayOffensiveCardInCenter_d__264` vs `_DisplayEnemyCardInCenter…`），
   **是否该收口到一份判据没查清**（要逐句读另一个协程的方法体）。⇒ **只报不改**（也不在本件判据内）。
4. ℹ️ `RevealInnerPhaseForTest` 从 3 项变 4 项是**破坏性**的（任何 `Length == 3` 的旧断言会红）。
   全仓只有一处消费（`BattleScene.cs:4841`），我改了；⛔ 后续别再写死长度去数它的语义。

---

## 七 · 没查清 / 没做

1. **没跑 Unity**（红线）⇒ §二那 7 条断言**一行都没在编辑器里跑过**；§五的 4833 是**算术**。
   ⚠️ 复跑 `BattleScene.Run` 时若其中有红，先看是哪一条：#1/#2 是常量、#3/#4/#5/#7 是闩与计数、
   #6 是 `CreatedByShown` —— **它们互相独立**，红哪条就指哪一处。
2. **没有真机截图/观感**：本件在今天的路径上**零画面差异**（§六·2），本来也拍不出东西。
3. **「每张新揭示卡都会重新触发」没有断言**（要起第 2 张揭示卡 / 第 2 局，成本远大于收益）。
   复位点写在「建新卡」那一处并写明了理由（与 G7 同源）；本件只有**这一局**的读数。
4. **`_revealInnerState3Calls` 是「本张卡」的计数**（每次建卡归零），⛔ 别拿它当「这一局一共调了几次」。
5. **`SetObjectVisibility` 里那段内联的 created-by 逻辑没有深究**（§六·1 旁证之外的部分）：
   它有 `param_2`（`CardStateOptions`）这一档，可能与 `DisplayCreatedByText` 的调用点**成对**出现在原版的
   每一次状态切换上（即「换档就重算这一层」）。我们这边由 `SetFace` / `FillCreatedBy` 两处读同一个谓词覆盖，
   **是否覆盖了所有换档点没查**（不属本件判据，也未在白名单外动手）。
6. **`_d__264__MoveNext.c:86` 的 `NetworkingPeer__OnMessage(cardUI, 0, 1, 0, 0)` 仍未判**（G7 §五·2 已记，我没深究）。
