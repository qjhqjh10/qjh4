# B8_M · 原版是否镜像敌方槽位 —— 独立复核 + 落地

> 执行代理 **B8_M**，2026-10-26。任务 = 独立复核 F（`报告/B8_F_表现层.md` §五-1）的结论并落地。
> 结论一句话：**F 的结论成立 —— 原版【不镜像】敌方槽位，我们的镜像已删。但 F 的「`FillMinionPositions` 是死代码」这一条要改判（见 §一·1）。**

---

## 一 结论（F 的三步，逐条给证据）

### 1-F「`leftSlotPosNormal` 全 ILSpy 只有两处赋值；`FillMinionPositions()` 0 调用者 ⇒ 活写者只有 Awake」

**「只有两处赋值」= 成立。** 我自己取到：

| 查什么 | 命令 / 文件 | 结果 |
|---|---|---|
| 全树有几个文件提到 `leftSlotPosNormal` | `grep -rl "leftSlotPosNormal" D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/` | **只有 `MinionManager.cs` 一个文件**（⇒ 没有任何外部类写这两条数组） |
| 赋值点 | `MinionManager.cs:164,166`（在 `Awake()` 内）· `:399,401`（在 `FillMinionPositions()` 内） | 2 处，与 F 一致 |
| `FillMinionPositions` 调用者 | `grep -rl "FillMinionPositions" D:/2/tools/decomp_full/` | 只命中 `MinionManager__FillMinionPositions.c` 自己 |
| 同上（ILSpy 侧） | `grep -rn "FillMinionPositions" D:/2/tools/cpp2il/` | 只有 3 份产物的**定义行**（`out_cpp2il_dev_ilspy/MinionManager.cs:381` 等），**0 个调用点** |
| 外部能不能碰 `isWeaponSetup`（决定 weapon 那两条数组会不会被用） | `grep -rn "isWeaponSetup *=" <全 ILSpy 树>` | **0 处**（只有声明与读，两个实例序列化都是 0）⇒ weapon 数组离线**恒不可达** |

**「0 调用者」= 成立；但「死代码」这个定性 = 🔴 驳回。** 三条：

1. 它的函数体与 **`Awake` 里的内联块逐句相同**（`FillMinionPositions.c:33-35,55-56` vs `Awake.c:54-56,99-101`）。
2. 但它**没有** Awake 的 prologue —— Awake 里 `new Vector2[slotsPerSide]` 那两次分配（`.c:38-52`）它一句都没有。若真是「写着没用的副本」，抄的人没理由把分配漏掉。
3. ⇒ 最合理的解释是：**`Awake` 在分配完数组之后调用 `FillMinionPositions()`，被 IL2CPP 的 C++ 编译期（-O2）内联**，所以 Cpp2IL/Ghidra 重建出来的 `Awake` 里看不到 `call`，而函数本身仍以自己的 RVA 存在（`all_methods.txt:6546` = `6458288 MinionManager$$FillMinionPositions`）。
4. minion 侧同理已排除「另有隐藏写者」：`MinionManager.cs` 里**没有任何空方法体**（脚本扫 `{ }` = 0 条）⇒ 不存在「某个没反编译出来的方法偷偷写数组」。

> ⇒ 对账的影响：**F 那句「活写者只有 Awake」仍然可用**（内联体就在 Awake 里），但账里**不能**据「0 调用者」把它判 `D`（细则见 §四）。

### 2-F「`Awake` 两个实例跑同一套式子（left 恒 −x、y=0）」

**成立。** 逐行抄出来的式子（`decomp_full/MinionManager__Awake.c`）：

```text
循环 1（写 0x20 = leftSlotPosNormal，:54-56）
    left[i].x = (float)(*(uint *)(param_1+0x90) ^ DAT_1834b2c80) * (i+1) − *(float *)(param_1+0x8c)
    left[i].y = 0
循环 2（写 0x28 = rightSlotPosNormal，:99-101）
    right[i].x = (i+1) * *(float *)(param_1+0x90) + *(float *)(param_1+0x8c)
    right[i].y = 0
```

- `param_1+0x90` = **`MinionSeparation`**、`+0x8c` = **`minionExtraDistanceFromHero`**（偏移按 ILSpy 声明序逐项对上：0x20/0x28/0x30/0x38 四条数组 · 0x40 `isWeaponSetup` · 0x48 `cardShadow` · 0x58 `slotsPerSide` · 0x84/0x88 `origPosReduction` · 0x8c/0x90/0x94 · 0x9c-0xa4 `heroExtraOffset`）。
- `DAT_1834b2c80` 用 `工具/read_literal.py` 按 **4B float** 实读 = `float -0` / `int32 -2147483648`（= `0x80000000`）⇒ `^` 就是**取负**。所以 **left 恒负、right 恒正**，两个 `MinionArea` 实例跑的是**同一段代码**、没有按 `isPlayer` 分叉。
- ⚠️ 一句更正：这不是「一套式子」，是**一对**（left 负 / right 正）。F 的表述不影响结论。

**两个实例的实读值**（`MonoBehaviour_4372` = 我 · `_4373` = 敌）：

| | MinionSeparation | minionExtraDistanceFromHero | 运行时 left[k]（k=1..4） |
|---|---|---|---|
| 我 | 0.82 | 0.09 | −0.91 · −1.73 · −2.55 · −3.37 |
| 敌 | 1.53 | 0.13 | −1.66 · −3.19 · −4.72 · **−6.25** |

### 3-F「两个 `MinionArea` 的 `Transform_1309/1314` 是 identity、scale 1 ⇒ 没有 180° 把它翻回去」

**成立，而且我把它扩到了整条父链 + 全部 13 个战场。**

```
MinionArea(玩家, transform 1309)  pos(0,0,−6.655)  rot identity  scale 1
  ← 1319  pos(0,0,0)   rot identity  scale 1
      ← 1301  pos(100,0,0)  rot identity  scale 1      ← 两行共同的父节点
          ← 1300 (根)  pos(0,0,0)  rot identity  scale 1

MinionArea(敌方, transform 1314)  pos(0,0,+1.043)  rot identity  scale 1
  ← 1315  pos(0,0,0)   rot identity  scale 1
      ← 1301  （同上）
```

- 13 个 `battlearena*` bundle 逐场扫（每场 2 个 `MinionManager`，各向上走 4 层到根）：**26/26 全部 identity / scale 1，无一例外**；两行 z 恒为 −6.655 / +1.043。
- ⇒ 局部 −x **原样**就是世界 −x。

### 4-F（反证）「别处（`BattleArena` 根 / 相机 / `Canvas`）有没有做翻转？」

**没找到；而且结构上不可能造成「行与行之间」的镜像：**

- 两行**共用同一个父节点 1301**。任何祖先 / 相机 / Canvas 的翻转或负缩放都**同时**作用于两行 ⇒ 只能整体翻，翻不出「我左敌右」。
- 唯一可能产生相对镜像的，只有敌方自己那条支链（`1315 → 1314`）—— 已核 = identity/scale 1。
- `CardScript__UpdateMinionInPlayPosition.c:76-77,124-127`：落位是 `set_localPosition(slotPos)` + `set_rotation(静态四元数/identity)` ⇒ 位置与朝向都不翻。

### 🔑 我自己补的一条决定性判据（F 没用）

序列化里敌方 `leftSlotPosNormal` **确实是 +x**（`[(2.67,5),(5.33,5),(8,5)]`），13 场**场场如此**。但它**只有 3 项**，而 `slotsPerSide` 序列化是 **4**。

> 若 `Awake` 不跑、数组留着序列化值 ⇒ `SpacesInTheBoard`（= `leftSlotPos.Length`）返回 **3**，而所有以 4 为上限的读取（`GetNextSlotPos` 里 `leftSlotPos.Length <= rightMinionsOccupation.Length` 那一支、`GetLeft/RightSlotPos` 的越界分支）都会走进「贴督军」兜底 ⇒ 牌位会坏。
> 游戏是好的 ⇒ **`Awake` 的重填必须发生** ⇒ 序列化那套（含「敌方镜像」）是**死值**。

这条与**早于 F** 的独立结论一致：`资料/普查产出_1015战斗/B4_战斗相机与棋盘几何.md` §2.4（标题就是「序列化值 ≠ 运行时值」，而且抄出了同一对公式与同一个 `0x1834b2c80`）。

### ✅ 结论

**原版【不镜像】敌方槽位。我们原来的 `enemy ? −x : x` 是错的，已删。**

---

## 二 改了什么

### 代码（全部在 `Board/**`，白名单内；**净 0 行变化**，`ArenaSlots.cs` 仍 173 行 / `BoardLayout.cs` 仍 634 行）

| 文件:行 | 改法 |
|---|---|
| `Board/ArenaSlots.cs:110` | `return new Vector3(enemy ? -x : x, 0f, z);` → **`return new Vector3(x, 0f, z);`**（真正的修复） |
| `Board/ArenaSlots.cs:106-108` | 3 行注释就地重写：把「敌方额外镜像」改成「原版不镜像」+ 判据（公式/取负常量/父链 identity/报告指针） |
| `Board/ArenaSlots.cs:80-81` | 父链证据补全（原来只写「两个 MinionArea 是 identity」，现在写到根 + 标出 1301 是共同父节点） |
| `Board/BoardLayout.cs:4` | 文件头「**敌方那个实例要镜像**（原版就是镜像的）」→「原版**不镜像**」 |
| `Board/BoardLayout.cs:39` | 「只镜像**顺序**」→「**也不镜像左右**；`mirror` 只表示『这是敌方那一行』」 |
| `Board/BoardLayout.cs:97` | `mirror` 的 Tooltip 重写（⛔ 不再左右翻槽号） |
| `Board/BoardLayout.cs:483` | `NxOf`：`0.5f + (mirror ? -d : d)` → **`0.5f + d`**（2D 兜底 / 命中判定同步） |
| `Board/BoardLayout.cs:570` | 「按镜像位取」→「`mirror` 位 = 敌方那一行」 |

**没有动 `mirror` 字段本身** —— 它还有两个合法用途：`QueueOf`（行分层：敌方整行号段 +9）与 `MinionConversionHeight` / `HeroScale` / `CardScale`（取敌方那份参数）。

### 可观察量（改前 / 改后 / 原版）

| 量 | 改前 | **改后** | 原版（推导） |
|---|---|---|---|
| `ArenaSlots.Position(0,false).x` | −3.37 | **−3.37** | −(4×0.82+0.09) = **−3.37** |
| `ArenaSlots.Position(0,true).x` | **+6.25** | **−6.25** | −(4×1.53+0.13) = **−6.25** |
| `ArenaSlots.Position(5,true).x` | −0.91 | **+0.91** | +(1×1.53+0.13) = **+0.91** |
| `ArenaSlots.Position(8,true).x` | −6.25 | **+6.25** | +(4×1.53+0.13) = **+6.25** |
| `BoardLayout.NxOf(0)`（敌方那份 board） | 0.8112 | **0.1888** | （2D 兜底，与 3D 同向） |

⇒ **判据一句话：`Position(0,enemy)` 与 `Position(8,enemy)` 对两侧必须同号（0 号恒负、8 号恒正）。**

### 落地校验

- `TMPDIR=/tmp/wf_b8m bash d:/4/Unity/工具/typecheck.sh` ⇒ 我这一批跑过三次：
  - 第一次（改完代码立刻）**0 / 0**；
  - 中途一次报 `CardView.cs:7236/7242 缺少 SetStatsHidden`（**不是我的文件** —— 别的代理正在写 `Core/CardView.cs`），随后那次改动落地后自行消失；
  - 收尾那次报 `BattleDriver.cs:152` 找不到 `PlayerState.QuestPointsFaction`（**同样不是我的文件** —— `RuleEngine/Core/PlayerState.cs` 上有人并行在改）。
  - **判据**：`grep "Board/|ArenaSlots|BoardLayout"` 过滤 typecheck 输出 = **空** ⇒ **我这两个文件零错误**（按 `CLAUDE.md` §13·3 第 2 条：错误全部集中在别人的文件上 ⇒ 不是我的问题，别去改别人的文件）。
- `python -I -X utf8 工具/check_ledger_points.py` ⇒ 输出里 `grep "ArenaSlots|BoardLayout"` **0 行**（我这两处改动零漂移）。
- 行尾：两个 `.cs` 二进制读 `CRLF 0 / LF n / 裸 CR 0`（本来就全 LF，没被翻）。
- `check_patch_conflicts.py` ⇒ 我的 `B8_M.tsv` **无跨补丁重复键**（唯一一条冲突在 B8_L / B8_U 之间，不是我）。

---

## 三 重建单（⛔ 我没跑 Unity / 没跑批处理）

### 3.1 要不要重建场景？

- **严格说不必须**：`mirror` / `lineY` 等字段的**值**没变，变的只是代码怎么用它们；运行时读的是代码。
- **建议跑一次** `BattleScene.BuildAndSaveScene`（**由调度台跑**），理由：这一批同时改了 `Board/**` 的注释与默认语义，重建能让场景里的 `BoardLayout` 组件与新代码在同一次里对齐，并且顺带把下面 3.2 的断言红暴露出来。

### 3.2 重跑后**一定会红**的一处（必须同批改掉，但它不在我白名单）

`Assets/CardPresentation/Editor/BattleScene.cs`：

| 行 | 现状 | 要改成 |
|---|---|---|
| `:20600`（注释） | `⚠️ 敌方 x 要**镜像**（Position 最后那一步），所以槽 0 的敌行 x 是 **+**6.25。` | 删掉，换成「原版**不镜像**：两侧槽 0 同在画面左（判据 `MinionManager__Awake.c:54-56` + 父链 identity）」 |
| `:20601`（断言） | `Check(Mathf.Abs(ArenaSlots.Position(0, true).x - (4 * 1.53f + 0.13f)) < 1e-4f, "★ 敌行落点 x = +(4×1.53 + **0.13**)…")` | `Check(Mathf.Abs(ArenaSlots.Position(0, true).x + (4 * 1.53f + 0.13f)) < 1e-4f, "★ 原版**不镜像**：敌行槽 0 x = **−**6.25（与槽 8 同号）")` |

> 其余断言不受影响：`:20589-20591`（我方槽 0/5/督军）不变；`:20593-20594` / `:20603` 只比 z；`:11115` / `:20249` / `:6509` / `:6571` 走的是 z 或 `CardCenter(..., mirror, …)`（后者现在自动跟着新代码走）。

**建议再补一条只盯「符号」的断言**（它比数值断言更抗后续改动，也直接钉住本结论）：

```csharp
Check(ArenaSlots.Position(0, true).x < 0f && ArenaSlots.Position(8, true).x > 0f
   && ArenaSlots.Position(0, false).x < 0f && ArenaSlots.Position(8, false).x > 0f,
      "★ 原版**不镜像**：敌我两侧「槽 0 恒在画面左、槽 8 恒在画面右」（判据 MinionManager__Awake.c:54-56 + 两个 MinionArea 父链全 identity）");
```

### 3.3 重跑后要核什么

1. `BattleScene.Run` 的格位断言组（上面那条已改的前提下）**绿**。
2. `grep "ArenaSlots.cs:98-110"` 那两条账行（`FillMinionPositions` / `GetLeftMostPos`）**不漂移**。
3. 真机（可选，调度台的批量验收轮）：进一场对局，敌方单位四张下的「最左/最右」与我方同侧；拖拽落点的透视判据仍然对得上（`ArenaSlots.TryResolveSlot` 与 `DropTargetWorld` 用的是同一个 `Position`，自洽）。

---

## 四 升了几条 A / 留几条 B

### 升 A：**0 条**

**原因（不是「没做完」）：** 这批几何方法**全部共用同一个落点** `ArenaSlots.cs:98-110`（`Position` 一条解析式同时表达 left/right、signed→index、most-pos）。按**方案 §4.1**「同一个 `文件:行` 覆盖多个不同方法 ⇒ 那是类级命中 ⇒ **只能记 B**」——即使我在「判据要点」里逐条写清（§4.1 同日补充允许共享落点，但本次简报明确要求按「只能 B」执行）。

> **要升 A 的唯一出路**：把 `Position` 拆成与方法一一对应的入口（例：`SlotAt(bool enemy, int side, int index)` / `LeftMost` / `RightMost` / `SlotFromSigned(int)`），让每条账行指到各自的**行号范围**。这是一次结构性重构，建议由调度台统一裁定后再做（否则账行会大面积重排）。

### 改档：2 条

| 方法 | 原 | 新 | 理由 |
|---|---|---|---|
| `MinionManager.FillMinionPositions()` | **C** | **B** | 缺口（敌方取负）**已修**；且「死代码」定性不成立（§一·1）⇒ 不判 D |
| `MinionManager.Awake()` | **D** | **B** | 原判 D 的理由是「纯 Unity 生命周期，我们无 MonoBehaviour 对应物」—— **错的**：它的行为就是「按 `slotsPerSide` 重填两条槽位数组」，我们已用 `ArenaSlots.Position` 实现；而且它是**镜像结论的活判据**，判 D 会让这条链看起来没有依据 |

### 保持 B：11 条（本次都补了真落点 / 真机制差异）

`GetSlotPos` · `GetLeftSlotPos` · `GetRightSlotPos` · `GetLeftMostPos` · `GetRightMostPos` · `GetSlots` · `SlotPosToBoardPos` · `GetNextSlotPos` · `AreMinionsOutOfPos` · `AdjustMinionBoardPos` · `GetDummyDistance`

其中：
- **10 条**现在有**真行号落点**（其中 **7 条**原来是一句「—（表现层/几何：…；口径六『不覆盖表现层』）」的占位）。
- `AreMinionsOutOfPos` 只给到**类级**（`BattleDriver.cs（SyncBoard）`，行号未核 —— 不在我白名单）⇒ G1 ✗。
- `AdjustMinionBoardPos` / `GetDummyDistance` 仍无落点 ⇒ G1 ✗ ⇒ 只能 B（理由各不相同，写在各自行的备注里）。

账补丁：`账/_parts/B8_M.tsv`（**13 行** · 11 列 · LF，`check_a_vs_diff` 无新增候选、`verify_annot` 缺注解 0、`check_patch_conflicts` 本补丁无重复键）。

### 为「`PlayerHand` 21 条几何」留的说明

我把**全部账**（22 个 `.tsv`）按「`我们的落点` 里含 `CardPresentation/Board/`」筛了一遍：命中 **20 行** —— `MinionManager` **15 行** + `CardScript` **5 行**（后者的真落点在 `Board/DeploySequence.cs`，本来就有行号，不属本次几何族）。

- 我这 13 行补丁 = 那 15 行里的 **12 行** + `Awake`（它原来的落点是「—」，不在那 20 行里）。
- 那 15 行里**我没动**的 3 行 = `HideCardShadow` / `SetCardShadow` / `ReassembleMinions` —— 它们的**真落点不在 `Board/`**（H 的定位表给的是 `Core/BlobShadow.cs:1-211` / `Core/CardFeel.cs:3289-3300`），占位口径过期，应由对应写手改。

🔴 **`PlayerHand` 21 条几何行里，落在 `Board/` 的是 0 条** —— 它们的落点全是 `Hand/HandLayout.cs` / `Hand/CardInteraction.cs`（不在我白名单，也没被我动过）。所以「PlayerHand 21 条」这一半在本代理这里**没有可对拍的行**；如果调度台那张清单确实这么分过块，那 21 条要另派给手牌线的写手。

---

## 五 没查清的部分（如实记录）

1. **`FillMinionPositions` 究竟「死」还是「被内联」** —— 重建产物里看不到 `call` 是**内联后**的必然现象，所以两者长相一样。我用「同体 + 缺 prologue」判「被内联」，但**没有 VA 级铁证**。要钉死：反汇编 `Awake`（RVA 待查）看是否有 `call 6458288`。**没做**（要动 Ghidra 工程，超出本代理范围）。
2. **`AdjustMinionBoardPos` / `GetDummyDistance` / `GetSlotPos` / `SlotPosToBoardPos` 的 0 调用者** —— 同上，不区分「内联」与「真死」。四个都记 B，没判 D。
3. **`origPosReduction` 的语义** —— 值 `(0.66, 2.75)` / `(0.65, 0.275)`，其中 2.75 不像比例。0 调用者 ⇒ 没深挖。
4. 🔴 **原版实战画面没验** —— 离线版手牌注入失败（`inj FAIL: empty Data`），拿不到「原版真的把敌方槽 0 画在左边」的实拍。**上面全部是静态判据**（反编译方法体 + 场景 JSON + 常量实读），与铁律 3/4 的「能跑就跑」差一档。**若有朝一日能在原版里打一局，第一件事就是核这条。**
5. **`slotsPerSide` 的运行时真值** —— 来自 `GameplayVariablesData.slotsPerSide`，那个 SO **不在离线包里**（B4 §2.4(c) 已记）。我们按序列化的 4 走，与 `ArenaSlots.SlotCount=9` 一致。

---

## 六 顺手发现（不属本任务，但值得记）

1. **账里三个占位落点是过期的**：`MinionManager.HideCardShadow` / `SetCardShadow` / `ReassembleMinions` 的「我们的落点」仍写着 `BoardLayout.cs · CardFeel.cs` 那句通稿，而 H 的定位表已给出真落点（`Core/BlobShadow.cs:1-211` / `Core/CardFeel.cs:3289-3300`）。**它们的落点其实不在 `Board/`**，应由对应写手改掉（不在我白名单）。
2. **两份文档仍在传播「敌方镜像」**（与 B4 §2.4 冲突，建议就地加更正行 · 铁律 5）：
   - `资料/对战排版_原版数值与改造方案.md:189-191,209`（把「敌方 leftSlotPosNormal 是 +x」写成了结论）
   - `资料/普查产出_1015战斗/B6_逐格对账_棋盘与手牌我们vs原版.md:55`（拿序列化死值当「支持镜像」的论据；同文 `:166` 自己又说过期了）
3. **`ExtraYOnBoard` 在 13 场 × 2 实例全为 0**（逐场实读）⇒ `SlotPosToBoardPos` 与 `GetNextSlotPos` 里那一项**当前恒等**，这也是我们没实现它而净效果相同的原因。
4. **`AreMinionsOutOfPos` 的两个常量**（本次新读出）：阈值 `VA 0x1834b2bb0` = **0.2f** · 距离用**曼哈顿**（掩码 `VA 0x1834b2e60` = int32 `0x7FFFFFFF`）。将来若做「按需重组」，量要照这两个来。
5. `GetSlots(direction)` 的方向符是**真·±1**（`0x1834b2bb8` = +1.0f · `0x1834b2bc8` = −1.0f），且 `direction < 1` 时返回顺序是**由外到内**（i 从 `slotsPerSide` 递减）。我们没这个顺序语义（无调用者需要）。
6. `ArenaSlots` 文件头记的「父链 → `BattleBoardElements`(100, 0, 0)」经本次核**成立**：节点 1301 的 `m_LocalPosition = (100,0,0)`，且是两行的**共同父节点**。
