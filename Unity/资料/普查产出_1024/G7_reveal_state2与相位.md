# G7 · 进攻卡 reveal 的「内层 state 2」接线 + 相位常数核对（2026-10-24）

> 执行代理 **G7**。判据来源 = ① `D:/4/Unity/资料/普查产出_1024/C6_最后三条判据.md` §3 与 §四·5
> ② `d:/2/tools/decomp_full/CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c`（亲读全文 105 行）
> ③ `d:/2/tools/decomp_full/BattleCardUI__DisplayCreatedByText.c`（亲读）④ `GameAssembly.dll` 浮点字面量现读。
> ⛔ 未跑 Unity / 未跑 ruleprobe / 未动 git / 未改正本。
> ✅ **只改了两个白名单文件**：`Battle/BattleDriver.cs` · `Editor/BattleScene.cs`。
> ⛔ **没碰** `Core/**`（H5 正在改 `CardView.cs`）· `Shell/**` · `CardBaseDemo.cs` / `CardFaceProbe.cs`（H6）· `RuleEngine/**` · `Net/**` · `工具/**`。
> ✅ `TMPDIR=/tmp/wf_g7 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**；两个文件仍全是 CRLF（lone-LF = 0）。

---

## 一 · 接线位置与闩

### 1·1 位置（`BattleDriver.cs:7774-7781`）

`TickOffensiveReveal` 里，紧跟在 `if (_revealStage == 0 && _revealCard != null) ApplyRevealInnerPose(_revealT);`
（原 `:7711`，现 `:7750`）**之后**、四段 stage 判定**之前**：

```csharp
if (_revealStage == 0 && _revealCard != null
    && !_revealInnerState2Done && _revealT >= RevealInnerState2At)
{
    _revealInnerState2Done = true;      // 内层 state 2 只跑一回
    _revealInnerState2Calls++;          // 自检读「动作真发生过几次」
    _revealCard.SetCreatedBy(null, CardView.CreatedByState.Hidden, isLocalPlayer: false);
    _revealCard.SetHighlight(CardHighlightState.Normal);
}
```

- **顺序**与原版一致：先 `DisplayCreatedByText`、后 `ToggleHighlight(false)`（`_d__264__MoveNext.c:88` → `:92`）。
  ⚠️ 反编译里这两句之间还有一句 `NetworkingPeer__OnMessage(cardUI, 0, 1, 0, 0)`（`:86`），
  那是 Ghidra 把某个 vtable 调用认错了名字（签名与调用点都不像 `OnMessage`）—— 与本条无关，**没深究**（见 §五）。
- **放在 stage 判定之前**是有意的：真机一帧 `dt` 很大（掉帧）时 `_revealT` 可能从 < 0.298 直接 ≥ 2.0，
  放前面才能保证 state 2 不会被这一帧的 stage 跃迁吞掉（原版也是内层先跑完）。

### 1·2 闩

| 成员 | 位置 | 作用 |
|---|---|---|
| `bool _revealInnerState2Done` | `BattleDriver.cs:7648` | **每张揭示卡一次**的闩 |
| `int _revealInnerState2Calls` | `:7650` | 真被调过的**次数**（自检口：闩只能证明「状态翻过了」，计数证明「动作发生过」） |
| 复位点 | `:7729-7730`（`BeginOffensiveRevealOrApply` 建新卡那一处，紧跟 `_revealStage = 0; _revealT = 0f;`） | 见下 |

🔴 **复位点为什么不在 `Begin` 里跟那一族**：`BeginOffensiveRevealOrApply` 在**联机局会被调两次**
（后手方自己那条防御卡先落地、对面那条进攻卡到了再调一次 —— 该方法的 doc 写着）。
若只在 `Begin` 复位，**第 2 张揭示卡的内层 state 2 会静默不跑**。选在「建新卡」这一处 = 每张卡各自算一次，
而且 `_revealCard == null` 时那段代码本来就不可达 ⇒ 不存在「闩悬着」的窗口。

### 1·3 那两件为什么今天是空操作（照实写，不许当「已复刻」）

- **创建者文字**：`ChosenOffensiveCard()` 返回的是 `OffensiveCards.Card`（数据记录），**不是 `CardInstance`**
  ⇒ 连 `Ctx.CreatedByOf`（`Core/BattleContext.cs:679-745`）都无从查起，只能**显式传 `null`**。
  原版同一格：`CardScript.createdBy` 只由「造牌 / 召唤 / 加牌 / 剧本」四条路写（C6 §3·2②），
  揭示卡走 `BattleCardManager.CreateCard` ⇒ **恒 null**，而 `BattleCardUI__DisplayCreatedByText.c:18`
  在 `createdBy == null` 那一步就 bail（**亲读确认**：先 `SetActive(false)`，再判 null 直接 return）。
  ⇒ 两边都是「不点亮」。
  🔴 **档位传的是 `Hidden`，不是 `InHandPlaying`** —— 这一格是我判的，理由写在这里：
  原版那次 bail **发生在看档位之前**（「有没有创建者」是更早的一道闸），所以行为上的等价物是「这一档不印」；
  而 `CardView.CreatedByVisible` 的谓词**只看档位、不看名字**
  （`state == InHandShowing || (state == InHandPlaying && !isLocalPlayer)` ⇒ 点亮）
  ⇒ 传 `InHandPlaying` 会把一个**空文字节点点亮**（真机看不见，但「该藏的时候没藏」正是本工程点名的缺陷形状）。
  ⚠️ `isLocalPlayer` 传 `false` 只是为了不撞上 `SetCreatedBy` 的去重早退（默认值 `true`）—— 档位已是 `Hidden`，它不影响结果。
- **高亮**：`ToggleHighlight(false)` 改的是卡【正面】材质的 `_Outline.a`（C6 §3·2③，与 `CardView` 的 `_rim`
  同一属性）。这张卡出生即 `Normal`（alpha 0）、state 0 只动正面/卡背的可见性 ⇒ **归零 = 无变化**。
  用 `SetHighlight(CardHighlightState.Normal)` 而不是新造一个 `ToggleHighlight(bool)`（判据只留一处）。
- ⚠️ **原版那一档 `CardStateOptions` 的运行时值没判出来**（`lVar5 + 0x228`，静态读不到）。
  今天不影响结果（`creator == null` 时原版在更早一道闸就 bail）⇒ 我按「bail = 不印」传 `Hidden`
  （理由见上一条）。**这一格如实标「原版档位值没判出来，且对今天的表现无影响」。**
- 🔮 **将来真有 `revealOrder`（`AbilityEffect.revealOrder = 280`）的卡走到这条链上时，这两句必须变成真接线**
  （`SetCreatedBy` 那时要传真的创建者）—— 已写进代码注释（`BattleDriver.cs:7771-7773`），与 C6 §3·3 末段同口径。

---

## 二 · 相位常数实读值（**读到了，已按真值改**）

### 2·1 现读（可复现命令）

```text
PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
  d:/4/Unity/工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" \
  0x1834b2dc4 0x1834b3184 0x1834b3190 0x1834b3178 0x1834b2bbc
```

| 地址 | 4B float | 8B double | 取 | 它是什么 |
|---|---|---|---|---|
| `0x1834b2dc4` | **0.1** ✅ | 3.8147e-07 ⚠️ | **0.1** | `DORotate` 的时长 **并且** state 0 的 `WaitForSeconds` |
| `0x1834b3184` | **0.66** ✅ | 0.000292969 ✅ | **0.66** | state 1 的 `WaitForSeconds` 乘数（× `param_2` 0.3） |
| `0x1834b3190` | **2.6** ✅ | 5.31742e-315 ⚠️ | **2.6** | state 2 的等待乘数 |
| `0x1834b3178` | **0.15** ✅ | 1.95503e-08 ⚠️ | **0.15** | `DOMove` 时长 |
| `0x1834b2bbc` | **2** ✅ | 1.39612e+17 ⚠️ | **2.0** | 外层 hold |

**宽度判定**：`DORotate` / `DOScale` 的 duration 参数是 C# `float`，`WaitForSeconds` 也是 `float`
⇒ 取 **4B**；8B 那档在 0x1834b2dc4 读出 3.8e-07 是垃圾（典型「宽度取错」）。
`0x1834b3184` 两档都像字面量，但 `0.66` 正好使 `0.3 × 0.66 = 0.198` 与 C6 的换算吻合 ⇒ 4B 才是它。

### 2·2 结论：C6 的低置信怀疑**成立**，`RevealWait1` 原来是错的

- 原版内层时间线（三个 `WaitForSeconds` **串行**，补间不阻塞）：
  `state 0` 起手 → 等 **0.1** → `state 1`：`DOScale(0.3)` + 等 **0.198** → `state 2`：`DisplayCreatedByText`
  + `ToggleHighlight(false)`（t = **0.298**）+ 等 **0.78** → `state 3`（t = **1.078**）。
- 我们原来 `RevealWait1 = 0.198`（注释写「`DAT_1834b3184` × 0.66」）—— 那是 **state 1 缩放之后**的等待，
  被误当成**缩放之前**的延迟 ⇒ 缩放跑 `0.198 → 0.498`，**整体后移 0.098 s**。

### 2·3 改了什么（`BattleDriver.cs:7575-7594`）

| 常量 | 改前 | 改后 | 依据 |
|---|---|---|---|
| `RevealWait1` | 0.198（注释：`DAT_1834b3184`×0.66） | **0.10** | `DAT_1834b2dc4` 现读 = 0.1；原版 state 0 的 `WaitForSeconds` 用的就是它 |
| `RevealScaleHold` | —（新） | **0.198** | state 1 的 `WaitForSeconds(0.3 × 0.66)` —— 在**缩放之后** |
| `RevealInnerState2At` | —（新） | **`RevealWait1 + RevealScaleHold` = 0.298** | 内层 state 2 的触发时刻 |
| `RevealMoveDur` / `RevealRotDur` / `RevealScaleDur` / `RevealWait2` | 0.15 / 0.10 / 0.30 / 0.78 | **一个字没改** | 现读逐个吻合 |

⇒ **那个 0.198 本身没错，错的是它被用在了相位的前一格。** 现在它有了正确的名字（`RevealScaleHold`）。
`ApplyRevealInnerPose` 的注释里也补了 `<remarks>`：缩放跑 `0.10 → 0.40`。

---

## 三 · 断言逐条（`Editor/BattleScene.cs:4840-4892`，共 **11 条**）

全部在 reveal 那一节内。**期望值一律写现读到的原版字面量**，⛔ 不读 `BattleDriver.RevealInnerState2At`
来证明它自己（那是被测常量 ⇒ 自证）。这一程把原来的 `AdvanceOffensiveRevealForTest(1.9f)` 拆成
`0.29 + 0.02 + 0.5 + 1.09 = 1.90`，**合计与原来对齐** ⇒ 后面 D6 那一段仍停在 t = 1.9 的同一拍。

| # | 断言 | 读的是什么 | 🧨 灭自证（怎么让它红） |
|---|---|---|---|
| 1 | ★ 内层相位：缩放**前**的等待 = **0.1**（`GameAssembly.dll` 现读 4B float，与 `DORotate` 同一个常数）· 缩放时长 0.3 | `RevealInnerPhaseForTest[1] / [2]` | 把 `RevealWait1` 改回 0.198 ⇒ 红 |
| 2 | ★ 灭自证：缩放前的等待**不是 0.198**（差 0.098 s，两档结构上不可能同时满足） | 同上 | 同上（1 与 2 是**互斥**的一对） |
| 3 | ★ 内层 state 2 落在 t ≈ **0.298** = 0.1 + 0.3×0.66 | `[0]` vs `0.1f + 0.198f` | 把 `RevealScaleHold` 或 `RevealWait1` 改掉 ⇒ 红 |
| 4 | （前提）内层 state 2 还没跑过（闩 = false、调用数 = 0） | 两个自检口 | —— |
| 5 | **①** t = 0.29（< 0.298）⇒ 内层 state 2 **还没跑**（闩仍 false） | 闩 + 计数 | 挪到「起手就跑」⇒ 红 |
| 6 | （前提）t = 0.29 时那张揭示卡还在 | `RevealCardForTest` | —— |
| 7 | （灭自证锚）测试先把高亮拨到 `Selected` | `CardView.State` | 见 #9 |
| 8 | **①** 跨过 0.298 ⇒ 接线**真被调用过一次**（闩 = true、调用数 = 1） | 闩 + 计数 | 删掉那个调用点 ⇒ 红 |
| 9 | ★ 那一拍把高亮拨回 `Normal`（原版 `ToggleHighlight(false)` = `_Outline` alpha → 0） | 揭示卡的 `State` | **只翻闩、不调 `SetHighlight`** ⇒ 停在 `Selected` ⇒ 红 |
| 10 | ★ 创建者文字**没亮**（`CreatedByShown == null`；档位传 `Hidden`，见 §1·3） | `CardView.CreatedByShown` | 传一个真创建者、或把档位改成 `InHandPlaying`（会把空节点点亮）⇒ 红 |
| 11 | **②** 再推 0.5 s ⇒ 调用数**仍为 1**（同一张揭示卡只跑一回） | 计数 | 去掉 `_revealInnerState2Done` 闩 ⇒ 第 0 段里每帧都调 ⇒ 红 |

🔴 **#7/#9 是这一批里唯一「结构上不可能同时满足」的一对**：`CardView.State` 的默认值**就是 `Normal`**
（枚举 0）⇒ 不先拨到 `Selected` 的话，「== Normal」是同义反复（＝自证）。
所以测试先把它拨到 `Selected`，之后它变回 `Normal` **只可能**是 state 2 那一拍做的。
⚠️ `SetHighlight(Selected)` 不改变缩放：`CardHighlight.ScaleOf(Selected, false)` = 1（原版 `selected` 不缩放）
⇒ 不会污染下面 D6 那条「卡保持 1.4」的断言。

---

## 四 · 会不会改时间线 / 要不要重跑

**会改时间线（内层观感），外层四段一秒没动。**

- 缩放起点 `0.198 → 0.10`、终点 `0.498 → 0.40` ⇒ 整条缩放轨迹前移 **0.098 s**（真机上肉眼可辨：
  卡更早开始长大、更早长到满）。**这是照原版改对的**，不是新功能。
- 外层 `RevealHoldDur 2.0 / RevealFlyHold 0.5 / RevealTailDur 3.0 / RevealFlyDur 1.0` 与 stage 跃迁**未动**；
  D6 那批在 t = 1.9 拍的读数（终点宽 / 缩放 1.4 / 进第 1 段保持 1.4）**逐值不变**。
- **必须重跑 `BattleScene.Run`**：① 本件改了 reveal 泵 + 新增 11 条断言；② D9 刚改完开场白顺序也压着一次复跑。
  宿主现读 **4813 / 0**（G1b 之后）⇒ 本件 **+11 条**，**预期 4824 / 0**。
  ⚠️ 我不知道另一个代理本轮是否也在 `BattleScene.cs` 里加了断言（我只加了自己那 11 条），
  以复跑报出的「断言合计」为准；**判绿红看断言合计、不看退出码**（`BattleScene.Run` 的 139 是已知间歇）。
- **没有跑 Unity**（红线），所以上面「预期 4824/0」是算术，不是实测。

---

## 五 · 没查清 / 没做

1. **原版 state 2 传的那一档 `CardStateOptions` 没判出来**（`CardScript + 0x228`，运行时值）。
   今天它与结果无关（`DisplayCreatedByText` 在 :18 就 bail），但**将来 `revealOrder` 真接线时必须把它判清**
   （那时档位决定「印不印」）。我按「bail ⇒ 不印」传了 **`Hidden`**（理由见 §1·3），**属判断**。
2. **`_d__264__MoveNext.c:85` 那句 `NetworkingPeer__OnMessage(cardUI, 0, 1, 0, 0)` 没判** ——
   参数形态（`this` = `BattleCardUI`、后面 4 个全 0/1）不像真的网络调用，判为反编译认错名字；**未深究**。
3. **「每张新揭示卡都会重新触发」没有断言**（要起第 2 局 / 第 2 张揭示卡，成本远大于收益）。
   复位点写在「建新卡」那一处并说明了理由；本件**只有 #4/#5/#8/#11 这一局的读数**。
4. **`_revealInnerState2Calls` 是「本张卡」的计数**（每次建卡归零），不是全局计数 —— 断言口径按此读，
   ⛔ 别拿它当「这一局一共调了几次」。
5. **没验真机**（红线）。🔴 更要紧的是：**本件新增的 11 条断言一条都没在 Unity 里跑过**
   （只过了秒级 typecheck）—— 复跑 `BattleScene.Run` 时若其中有红，**先看是哪一条**：
   #1~#3 是常量、#5/#8/#11 是闩与计数、#9 是 `State` 翻转；**它们互相独立**，红哪条就指哪一处。
   缩放前移 0.098 s 的实际观感也没有截图证据。

---

## 六 · 顺手发现（只改了两处与本次判据直接相关的注释，其余只报）

1. ✅ **`BattleDriver.cs:7740-7746` 那句「内层协程合计 1.758 s」算错了，已就地订正（铁律 5）**：
   三个 `WaitForSeconds` 相加 = `0.1 + 0.198 + 0.78` = **1.078 s**（逐行读 `_d__264__MoveNext.c:41-46 / :61-65 / :95-100`）。
   1.758 大概是 `0.198 + 0.78 + 0.78` 这种串法。**这个数不影响任何行为**（我们只模拟到缩放），但会误导下一次核对相位。
2. ⚠️ **`RevealWait2 = 0.78` 全仓只有定义、没有使用**（`grep` 过 `RevealWait2`：只命中定义行）。
   它本该是 state 2 之后的等待。今天不是缺陷（外层 2.0 s 一到就把卡收掉了，内层那 0.78 s 本来也观测不到），
   但**名字像「第二段等待」会误导** —— 建议将来动内层时把它接上，或改名成 `RevealState2Hold`。
3. ⚠️ **`CardView.State` 默认值 = `Normal`（枚举 0）** ⇒ 任何「断言临时卡 `State == Normal`」的写法都是同义反复。
   本件已用「先拨 `Selected` 再验」绕开（`BattleScene.cs:4865-4880`）。
   同族的「高亮类」断言以后都要按这个形状写（先拨非默认档）。
4. ⚠️ **`RevealDurationsForTest` 只有外层四段**，内层相位没有出口 ⇒ 本件新增 `RevealInnerPhaseForTest`
   （`{state2 时刻, 缩放前等待, 缩放时长}`）。建议后续内层断言都从这里读，⛔ 别再各写一份常量。
   🔴 **2026-10-24（`G9` 就地补注）**：本数组**已扩成 4 项** —— 第 4 项 = 内层 **state 3** 的触发时刻
   （**1.078 s**），因为 state 3 已接线（见 `资料/普查产出_1024/G9_reveal_state3.md`）。
   ⇒ 沿用本数组的断言里 `Length` 要按 **4** 读（`Editor/BattleScene.cs` 那条已同步改过）。
5. 🔴 **`CardView.CreatedByVisible` 的谓词只看档位、不看名字**（`Core/CardView.cs:2989-2997`）——
   原版是**两道闸**：`createdBy == null` **先** bail，然后才看档位。我们只有后面那道。
   ~~⇒ 任何「传 `null` 创建者 + `InHandShowing`/`InHandPlaying` 档」的调用点，都会把一个**空文字节点点亮**。~~
   🔴 **2026-10-24（`G9` · 铁律 5 订正）：「会点亮一个空文字节点」不成立** —— `Fill`（`CardView.cs:4385-4393`）
   收到空文本会 `t.text = ""` + `TmpFont.Kill(t.gameObject)` 并把 `_createdBy` 置 **null** ⇒ **节点被拆掉**，
   不存在「亮着的空节点」。真后果只是**谓词值错**（`CreatedByVisible` 返回 `true`），**今天的画面上完全不可观测**。
   **错因**：只读了谓词、没读消费方 `Fill` 的空文本分支。
   （判据 + 逐调用点核查 → `资料/普查产出_1024/G8_CreatedBy与RevealWait2.md` §一 / §二 / §五·1。）
   ~~今天**没有**这样的调用点（手牌那条 `PushCreatedByStates` 传的是真有创建者的卡；打出的卡那条在
   `BattleDriver.cs:21558 / :21570`（另一条链在 state 2 用 `if (creator == null) return;`、state 3 用
   `if (creator != null)` 挡住了）⇒ **潜伏**，不是现行缺陷。~~
   🔴 **2026-10-24（`G9` · 铁律 5 订正）：「今天没有这样的调用点」不准** —— 有**两处**「`null` 名字 + 展示档」的真调用：
   ① `BattleDriver.cs:21570`（本件已改为显式 `Hidden`）原来是 `if (creator != null) v.SetCreatedBy(null);`
   —— **实参就是 `null`**、档位吃默认参 `InHandShowing`（`CardView.cs:2955`），那个守卫守的是**外层变量**；
   ② `BattleDriver.cs` 的 `PushCreatedByStates`：`Ctx.CreatedByOf(...)` 对**不是造出来的牌**返回 `null`，
   而悬停那一档是 `InHandShowing`。
   它们今天不发字**只是因为 `Fill` 把节点拆了**，**不是**被那道闸挡住 ⇒ 那道闸是**真在挡谓词**。
   **错因**：只看了 state 2 那一格（`:21558`）的早退，没读 `:21570` 的实参。
   建议由 `CardView` 的负责人（H5）在 `CreatedByVisible` 里补一句「名字为空 ⇒ 不印」（判据 = 原版那一道 bail）。
   ✅ **2026-10-24（`G8`）已补上**：`Core/CardView.cs:3008` `if (string.IsNullOrEmpty(_createdByName)) return false;`
   ＋ 新增 `CreatedByVisibleForTest`（`:3029`）。⚠️ 只是**谓词**修正 —— **零行为变化**（见 G8 §二）。
6. ℹ️ **`BattleCardUI__DisplayCreatedByText` 的「先 `SetActive(false)` 再设文案」那个怪顺序**（C6 标为「没判死」）：
   我这边**不复刻**该顺序 —— 直接「有创建者就点亮」（`CardView.SetCreatedBy` 的既有口径）。今天 `creator == null`，
   两者等价；`revealOrder` 真接上时**这条要重判**（原版那句 `SetActive(false)` 之后到底谁把它点亮，反编译里看不到）。
