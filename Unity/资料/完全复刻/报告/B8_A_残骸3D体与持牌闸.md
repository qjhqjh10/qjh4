# B8_A — 残骸 3D 卡体那 5 条红 + `TryUsingWaystone` 缺口 ③

日期：2026-10-26 · 执行代理 `B8_A` · 白名单：`Core/CardView.cs`（**未改动**）· `Battle/BattleDriver.cs` · `Editor/BattleScene.cs`

---

## ① 结论

**根因（任务 1）一句话**：那 5 条红**不是产品缺陷、也不是「认领支」**，而是**夹具卡的阵营是假的** —— `ProbeRemnantView` 造的合成 `CardDef` 阵营写死 `"Test"`，
`Resources/Art/cards/` 里没有 `frame_test_*` ⇒ `CardArt.Frame("Test", …)` 逐级退回后返回 **null** ⇒ `CardView.Build` 走**占位卡面**那条 `else`
⇒ **那条支路根本不调 `BuildBody3D`**（3D 卡身只在 `frameTex != null` 那一支里建）⇒ 夹具卡永远 `_body3D == null`。

这一条**同时解释了探针的三个读数**，而且不需要任何「警告被吞」的假设：

| 读数 | 解释 |
|---|---|
| `Face=Board` | `SyncBoard` 的新建路确实传的是 `CardFace.Board`（`BattleDriver.cs:25430-25431`）—— 面是对的 |
| `同一对象=False` | 确实是**新建**出来的视图（不是认领支）—— 上一轮「警告没响 ⇒ 是认领支」的推理**作废** |
| `Body3DWorldScale=(0,0,0)` | `_body3D` 是 null ⇒ getter 走 `: Vector3.zero`（`CardView.cs:3065`） |
| `[CardView] 建不出 3D 卡体` **0 条** | 那句 `Debug.LogWarning` 在 `BuildBody3D` **里面**（`CardView.cs:7712-7716`）；占位支路**根本不进这个函数** ⇒ 不是通道被吞，是**没有人报警** |
| 残骸体缩放 `0.31891` = `0.88586 × 0.36` | `SetRemnantBody` 的 `_body3D != null ? … : Vector3.one * Body3DScale` 取的是**退回档常量**（`CardView.cs:2628-2630`） |

**改了 3 处**：
1. `BattleScene.ProbeRemnantView` 加一个 `faction` 形参（默认 `"SaimHann"`，死灵那两处传 `"Sautekh"`），合成卡不再用 `"Test"`。两个默认值都是**卡池里真有卡框的阵营**（`frame_saimhann_tier1..4` / `frame_sautekh_tier1..4` 全在）。
2. `BattleDriver.TapBoardCard` 补上原版第 ③ 道闸 `UIstate == normalBattle`，落成 `BattleUiIsNormalBattle`（属性**追加在类尾**，零漂移）。
3. `BattleScene` 的 G16 块新增 **2 条断言**（封锁档下点灵魂石什么都不做 / 前提），给 ③ 一个可观察量。

**预期**：那 5 条红**全部转绿**（4 条残骸体断言 + 1 条 G21 夹具前提）；夹具从「静默测错档」变成「真测 3D 档」，2D 组也**第一次真的**走到 `DebugNoBody3D` 那条退回支（会打 1 条 `[CardView] DebugNoBody3D 开着` 警告，**这是新增的、预期内的**，不是失败）。

---

## ② 逐条证据

### 2·1 为什么 `Body3DWorldScale` 恒 0（任务 1 第 1 问：所有返回 `Vector3.zero` 的分支）

getter 只有**两条**路（`Core/CardView.cs:3063-3066`）：

```
get { return _body3D != null ? _body3D.transform.lossyScale : Vector3.zero; }
```

即 ① `_body3D == null` ⇒ `zero`；② `_body3D != null` 但某个祖先 `lossyScale` 为 0 ⇒ `zero`。
**本次是 ①**（② 不成立：卡根 `lossyScale = 0.36`，实测 `0.31891 / 0.88586 = 0.36`）。

`_body3D` 的**全部赋值点只有两处**（`grep -n "_body3D" Core/CardView.cs`）：
- `CardView.cs:2322`（`SetFace`：`if (board && _body3D == null) _body3D = BuildBody3D(…)`）
- `CardView.cs:4361`（`Build`：`if (board) _body3D = BuildBody3D(artTex)`）

⇒ 剩下的唯一问题就是「`BuildBody3D` 为什么一次都没被调用 / 返回 null」。

### 2·2 3D 卡身整条链的前置条件（任务 1 第 2 问）

`BuildBody3D`（`CardView.cs:7697-7717`）只有三道门，**没有一道与批处理有关**：
1. `if (DebugNoBody3D)`（`:7702`，产品恒 false，诊断开关）；
2. `mesh = CardArt.Card3DMesh()` / `sh = Shader.Find("CardPresentation/Card3D")`，任一 null ⇒ 打警告 + 返回 null（`:7708-7717`）；
3. 之后是纯 `new GameObject` + `MeshFilter/MeshRenderer` + 三张贴图，**不依赖 `Camera` / `RenderTexture` / `GraphicsBuffer`**，也**没有 `Application.isBatchMode` 分支**。

**「批处理里 3D 体建得出来」有现成实证**：`Editor/BattleScene.cs:15790` 那条「手牌打出去的卡也有 3D 卡体」断的是**真打出去的那张真卡**，它现在**是绿的** ⇒ 网格/shader 在批处理下都在。

⇒ 所以问题不在 `BuildBody3D` 体内，而在**它有没有被调用**。

### 2·3 「同一对象=False」+「Face=Board」为什么能同时成立（任务 1 第 3 问）

`SyncBoard` 的新建路（`BattleDriver.cs:25416-25434`）传的是 `CardFace.Board`（`:25430-25431`）⇒ `Create` 里 `v._faceMode = face`（`CardView.cs:2157`）**在 `Build` 之前**就定了
⇒ `Build` 里 `bool board = _faceMode == CardFace.Board`（`:4339`）为真，`Face` 自然是 `Board`。
`同一对象=False` 只是说这一格原来是空的、视图是新建的 —— **和 Face 无关**。

**新建路径确实会建 3D 体**，但**只在 `frameTex != null` 那一支里**：

- `CardView.cs:4263` `if (frameTex != null) { … }`
  - `:4361` `if (board) _body3D = BuildBody3D(artTex);`
  - `:4362-4363` 建不出来才补一层 2D 立绘
- `CardView.cs:4542` `else { // ---- 没有原版卡框：退回单层占位卡面 ---- }`
  - `:4559` `_face = GetComponent<MeshRenderer>();`（**根节点那个** MeshRenderer 当卡面）
  - `:4564` `_art = AddLayer("art", …)`
  - **整支里没有任何 `BuildBody3D` 调用**（也**没有任何警告**）

`CardArt.Frame("Test", null, false, tier)` 的退回链（`Core/CardArt.cs:71-89`）：`frame_test_tier4` → `frame_test_tier1` → `frame_test` → 三者皆无、且 `tactic == false` ⇒ **`return null`**。

⇒ **根因坐实**：`ProbeRemnantView` 的合成卡阵营 `"Test"` 使 `frameTex == null` ⇒ 走占位支 ⇒ 不建 3D 体 ⇒ 5 条红。

### 2·4 那 5 条红分别是哪几条（改前 → 改后）

| 行（改后行号） | 断言 | 改前 | 改后 |
|---|---|---|---|
| `BattleScene.cs:15982` | ★ 夹具前提（3D 组）：那一格的卡**有 3D 卡身** | 🔴 `Body3DWorldScale = 0` | 🟢 `0.31891` |
| `BattleScene.cs:16032` | ★ 残骸体的缩放 = 原卡卡身那个值 | 🔴 `bs == zero` | 🟢 `ws == bs == 0.31891` |
| `BattleScene.cs:16525` | ★ 不再是残骸时撤掉残骸体、原卡卡身回来 | 🔴 `Body3DVisible == false` | 🟢 |
| `BattleScene.cs:16544` | ★ （对照组）撤掉 ⇒ 回来的是 **3D 卡身** | 🔴 同上 | 🟢 |
| `BattleScene.cs:16647` | ★ 死灵到点之后原卡卡身回来 | 🔴 同上 | 🟢 |

（另：`16540` 「盖残骸体 ⇒ 3D 关、2D 也关」改前**是绿的但绿得没意义** —— `_body3D` 本来就是 null，`Show(null,…)` 是空操作；改后它才真的在测「有 3D 体时也把它关掉」。）

### 2·5 2D 组「碰巧绿」的定性（任务 1 第 6 问）

改前：`DebugNoBody3D = true` 对这张卡**完全无效** —— 它在 `BuildBody3D` **门口之前**就已经走了占位支，所以那句「`DebugNoBody3D` 开着」的警告一次都发不出来；
`16568` 那条「前提：确实处在没有 3D 卡体那一态」是**恒真**（`_body3D` 永远是 null）⇒ 一整组 2D 退回断言**测的是「占位卡」而不是「3D 建失败」**。

改后：阵营有了真卡框 ⇒ `Build` 进 `frameTex != null` 那一支 ⇒ `board` ⇒ `BuildBody3D` ⇒ 被 `DebugNoBody3D` 拦下、**打 1 条警告**、返回 null ⇒ `:4362-4363` 补建 2D 立绘层
⇒ `_body3D == null && _art != null` 正是那两条断言要的态。**它第一次真的走到了「3D 建失败的退回支」。**

---

## ③ 改了哪些文件哪些行（含新增行数 —— 判账漂移用）

| 文件 | 改动 | 新增行 |
|---|---|---|
| `CardPresentation/Battle/BattleDriver.cs` | `TapBoardCard` 内补 ③ 那道闸：**32560-32571**（12 行） | **+12** |
| `CardPresentation/Battle/BattleDriver.cs` | 类尾追加属性 `BattleUiIsNormalBattle`：**32844-32878**（36 行，含 doc） | **+36** |
| `CardPresentation/Editor/BattleScene.cs` | `ProbeRemnantView` 加 `faction` 形参 + `<param>` doc：**47227-47236**（10 行，签名在 47237-47240） | **+10** |
| `CardPresentation/Editor/BattleScene.cs` | 合成卡阵营 `"Test"` → `faction`；死灵两处传 `faction: "Sautekh"`（**就地改写，零行数变化**） | 0 |
| `CardPresentation/Editor/BattleScene.cs` | G16 块新增 2 条断言：**36520-36542**（24 行） | **+24** |
| `Core/CardView.cs` | **一个字未改**（只读来定位根因） | 0 |

`git diff --numstat`：`BattleDriver.cs 48/0` · `BattleScene.cs 38/4`。行尾全部仍为 **CRLF**（`bare CR = 0`，两文件 `CRLF == LF`）。

**账漂移预期**（主表 `文件:行号` 锚点）：
- `BattleDriver.cs`：插入点在 **32560** ⇒ 该行之后的所有锚点 **+12**。现读受影响的 = **6 行**（全在 `战斗界面_HUD.tsv`）：`32584→32596` · `32596→32608` · `32673→32685`（×2）· `32713→32725` · `32790→32802`。
  改前的 `32541`（×2）与 `32552`（×2）在插入点**之前**，**不动**。
- `BattleScene.cs`：两处插入（36520 / 47227）都在既有锚点（`13558` · `24068-24076`）**之后** ⇒ **零漂移**。
- 类尾那个属性（32843 起）在其它锚点之后 ⇒ 零漂移。

---

## ④ 没查清的部分

1. **「占位卡面」这一支在**真机**下是什么样，没有实拍证据**（本地没有「阵营取不到卡框」的原版画面可对照 —— 原版根本没有这条兜底路）。
2. **`_face`（根节点那个 MeshRenderer）在 `Board` 档下没人关** —— 全仓 `Show(_face, …)` **零调用**（`grep` 实核）。所以占位档的卡在场上会同时露着「占位卡面 + 数值层」。这条我只是**读出来**，没有改（判据不明：原版没有这条兜底路，属 🔶 自设计地段）。
3. **`ProbeRemnantView` 造的卡立绘仍是占位渐变**（`artId = "Probe_Waystone"` 在 `CardArt.Portrait` 里查不到）——不影响本组断言（`_CardImage` 只要非 null），但**别把它当「有立绘的卡」**用。
4. **`UIstate` 那 15 档的写入点**我只核了与本闸相关的 6 档（见 `BattleUiIsNormalBattle` 的 doc）；`movingHandCard` 是**推断**（`DrivePlayerTurn` 的 `interaction.IsDragging` 早退），没有逐行读手牌拖拽那条链去证。

---

## ⑤ 顺手发现（**只报，没改**）

1. 🔴 **`CardView.Build` 的「占位卡面」支路不建 3D 卡身，而且不报警告**（`CardView.cs:4542-4565`）。
   一张 `CardFace.Board` 的卡，只要 `CardArt.Frame(d.faction, …)` 返回 null（`faction` 为 null / 未知阵营），场上就是**一张平面占位卡**，既没有 3D 体也没有任何提示 —— 而原版 `BattleCardUI.ChangeCardToMinion` 换 3D 体**与卡框贴图无关**。
   产品今天**不可达**（`ToCardData` 用的是卡自己的阵营，13 个卡池阵营 + 自设计的 `Ember`/`Tide` 卡框全在），所以不是 P0；
   但它属于「不许静默失败」那条红线的边上（静默降级）。**要不要补、怎么补，请调度台裁**（补法大致 = 把 `:4361` 那两句挪到 `if/else` 之外，再顺手处理 `_face` 在 Board 档的显隐）。
2. 🔴 **`CardView.Build` 的根节点 `MeshRenderer`（`_face`）在 `Board` 档没人关**（同第 1 条的连带）：`_face` 只在占位支非 null，而 `SetFace` / `ApplyFace` 里**没有一处** `Show(_face, …)`。
3. `SimulateTapUnit` 的返回值是「大卡窗现在开着吗」，新闸让它对「被封锁」与「窗没开」**不可区分**。测试里靠 `SpiritStones` 与 `ActionCounter` 两条**独立**判据区分，够用；但将来谁想用它判「这一下被吃了没有」会踩坑。
4. `ProbeRemnantView` 的 `ctx.Players[0]` 是写死的（`BattleScene.cs:47219`），而 `driver.BoardViewAt(slot)` 默认 `mine = true` —— 两者靠「自检里 `_me` 恒 0」自洽。换座位（`SetSeat` 那类）之后这个夹具会**静默读错侧**。

---

## ⑥ 给调度台的请求

### 6·1 要跑的自检

```
TMPDIR=/tmp/wf_run bash d:/4/Unity/工具/typecheck.sh        # 我已跑：0 / 0
<E> -executeMethod BattleScene.Run                          # 只有这一条
```
（我没跑 Unity；`typecheck.sh` 前后各跑一次，都是 **运行时 0 / 编辑器 0**。）

### 6·2 期望读数

- 断言合计：**5,502 → 5,504**（原 5,497 通过 + 5 红；改后 **5,504 通过 / 0 失败**）。
  ⚠️ 若 `BattleScene.Run` 的合计不是 5,504，先看是不是**别人同期改了 BattleScene**（我的白名单只有本文件，但批次里可能有人动过）。
- 新增警告（**预期内、不是失败**）：**1 条** `[CardView] \`DebugNoBody3D\` 开着 ⇒ 场上退回 2D 立绘`。
- 新增日志（预期内）：`[G16]`/`[Battle]` 那一侧可能出现 `点了自己那侧的灵族残骸，但战斗 UI 不在 \`normalBattle\` 档` **1 条**（我新加的那条封锁档断言）。

### 6·3 `[G21·探针]` 三行应变成

| 探针 | 改前 | 应变成 |
|---|---|---|
| A 组（灵族 / slot=`free0`） | `同一对象=False Face=Board Body3DWorldScale=(0.00000, 0.00000, 0.00000)` | `同一对象=False Face=Board Body3DWorldScale=(0.31891, 0.31891, 0.31891)` |
| 2D 组（`DebugNoBody3D` / slot=`free1`） | `Body3DWorldScale=(0.00000, …)`（**假绿**：占位卡本来就没体） | **仍是** `(0.00000, …)` —— 但这一次是**真的**由 `DebugNoBody3D` 拦住 `BuildBody3D` 得到的，且日志里**多了 1 条** `DebugNoBody3D 开着` 的警告 |
| 第三具（死灵 / slot=`slotN`） | `Body3DWorldScale=(0.00000, …)` | `(0.31891, 0.31891, 0.31891)` |

（`0.31891 = Body3DScale 0.88586 × 卡根 lossyScale 0.36`，与我方单位那一档的 `PoseFor` 缩放一致；若读数是个别的别的非零值，只要**与残骸体缩放逐位相等**即为对。）

### 6·4 账

- 补丁 = `账/_parts/B8_A.tsv`（1 行，`BattleManager.TryUsingWaystone`，状态 **C**，理由见下）。
- 跑过：`check_a_vs_diff.py` ⇒ **0 条命中**；`check_patch_conflicts.py` ⇒ 无跨补丁重复键。
- **保持 C 的理由**：③ 已落地且有可观察量（新加 2 条断言），但**我拿不到「我们侧实测值」**（不跑 Unity）⇒ 按 G0–G8 不满足 A。等 `BattleScene.Run` 跑绿后，把读数填进 `差异` 列即可升 A。
- 需要 `--fix` 的锚点：6 条，**+12**（清单见 §③）。⚠️ `--fix` 请在「没有 `.cs` 写手 + 补丁已合并」的窗口里跑（本批其它代理可能正在写）。
