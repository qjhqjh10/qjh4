# B8_AR · 棋盘几何剩余 B 行（`CardPresentation/Board/**`）

> 代理 = **B8_AR** · 2026-10-26 · 白名单 = `CardPresentation/Board/**`
> 账补丁 = **两份**：`账/_parts/B8_AR.tsv`（**4 行，保持 B**）+ `账/_parts/B8_AR2.tsv`（**6 行，升 A**，按调度台 2026-10-26 裁定走 `方案 §4.1` 的「共享落点」条款）
> 本批**不动主表**、**不跑 Unity**、**不动 git**。`TMPDIR=/tmp/wf_b8ar bash 工具/typecheck.sh` = **0 / 0**。

---

## 一 结论

| 项 | 结果 |
|---|---|
| 本批负责的行 | **10 行**（`ArenaSlots` 落点 9 行 + `BoardLayout.TryResolveSlot` 1 行），见 §一·1 |
| 状态变动 | **4 行保持 B**（`Awake` / `FillMinionPositions` / `GetNextSlotPos` / `SlotPosToBoardPos`）+ **6 行升 A**（`GetLeftSlotPos` / `GetRightSlotPos` / `GetLeftMostPos` / `GetRightMostPos` / `GetSlotPos` / `GetSlots`）—— 按调度台 **2026-10-26 裁定**走 `方案 §4.1` 的「共享落点」条款（①行号范围 ②判据要点逐条说清「这段代码如何分别满足 6 个方法」③每条一个可观察量，期望值来自原版产物、实际值来自我们运行时）⇒ 补丁 `_parts/B8_AR2.tsv` |
| 判据订正 | **2 条事实错误**已就地改掉：① `FillMinionPositions`「没有数组分配」→ **它有**（`.c:14-19` / ILSpy `:398-401`）；② `GetNextSlotPos` 的「`nextSlotRequired>1` 放不下 ⇒ 回督军」→ **那一路是 LogError + 右最外一格**，回督军只在 `==1` 且两侧满时 |
| 代码补齐 | **2 处真缺口**已在 `ArenaSlots.cs` 实现：① `GetLeft/RightSlotPos` 的**越界夹取 + LogError**；② `SlotPosToBoardPos` 的 **`+up×ExtraYOnBoard`**（常数 0）。**零行号变化**（`Position` 仍是 `:98-110`） |
| 值级对照 | **18 格（9 槽 × 两侧）xyz 逐格一致，Δ ≤ 2.5e-7**（原版侧由**预制体字段 + `Awake.c` 公式**算，我们侧由 `ArenaSlots.cs` **源码常量**算）—— §二 |
| 断言 | 已有断言覆盖 6 组值；**新增 4 条草稿**（内/外圈两侧精确值 · y 全 0 · 越界夹取）—— §四 |

### 一·1 本批的行（挑行命令的并集，去掉 3 行纯表现层）

| 物理行 | 方法 | 落点 |
|---|---|---|
| 1244 | `MinionManager.Awake()` | `ArenaSlots.cs:98-110` |
| 1246 | `MinionManager.FillMinionPositions()` | 同上 |
| 1255 | `MinionManager.GetLeftMostPos()` | 同上 |
| 1256 | `MinionManager.GetLeftSlotPos(int)` | 同上 |
| 1265 | `MinionManager.GetNextSlotPos(Vector2,int)` | `BoardLayout.cs:487-517` |
| 1269 | `MinionManager.GetRightMostPos()` | `ArenaSlots.cs:98-110` |
| 1270 | `MinionManager.GetRightSlotPos(int)` | 同上 |
| 1271 | `MinionManager.GetSlotPos(int)` | 同上 |
| 1272 | `MinionManager.GetSlots(int)` | 同上 |
| 1292 | `MinionManager.SlotPosToBoardPos(Vector3)` | 同上 |

**未动的 3 行**（`--file BoardLayout` 切片里带出来的）：`1274 HideCardShadow()` · `1285 ReassembleMinions(...)` · `1291 SetCardShadow(...)`
—— 这三条的落点是 **`—`**，按 `判据/战斗逻辑驱动_B2A_口径.md` **§六「不覆盖纯表现层」** 根本不属本域；它们只是**落点文字里提到了 `BoardLayout.cs`** 才被切片命中。**本批不动**（改也无从改：`cardShadow` 是原版 `MinionManager` 上的一个 `GameObject`，我们没有对应件；我们的等价物是 `Core/BlobShadow` + `BoardLayout.AttachDropSlotTo`）。**建议调度台把它们移交 `战斗界面_HUD` / CardPresentation 线**。

### 一·2 为什么是「6 升 A + 4 保持 B」（调度台 2026-10-26 裁定后）

**升 A 的依据 = `方案 §4.1` 的「共享落点」条款**（`方案:173-176`）：多个原版方法**确实由同一段代码实现**时，落点可以共享，条件是 ① 落点给**行号范围** ② `判据要点` 说清「这段代码如何**分别**满足各方法」；调度台另加第 ③ 条 —— **每条一个可观察量，期望值来自原版产物、实际值来自我们运行时**。

`ArenaSlots.cs:98-110` 这一条解析式按 `k = slot − WarlordSlot` 分流，6 个方法各用其中一支（判据要点里逐条写死）：

| 原版方法 | 这段代码里对应哪一支 |
|---|---|
| `GetLeftSlotPos(i)` | `k<0`：`n = i+1`（`slot = 3 − i`） |
| `GetRightSlotPos(i)` | `k>0`：`n = i+1`（`slot = 5 + i`） |
| `GetLeftMostPos()` | `k<0` 且 `n = SlotsPerSide` |
| `GetRightMostPos()` | `k>0` 且 `n = SlotsPerSide` |
| `GetSlotPos(s)` | `k` 的符号/绝对值分派本身（`s ↔ slot = 4 + s`） |
| `GetSlots(dir)` | `n = 1..SlotsPerSide` 整排（方向符 = `side`） |

**剩下 4 行保持 B 的理由**（都是**真差异**，不是口径）：

- `Awake`：`slotsPerSide` 是运行时从 `GameplayVariables` SO 读的（**离线包里没有那个 SO**），判据侧只能取到序列化值 4；另有 `cardShadow.SetActive(false)` 我们无对应物。
- `FillMinionPositions`：与 `Awake` 的**调用关系**查不到（全树 0 调用者，两种解释都站得住）。
- `GetNextSlotPos`：落点只覆盖「位置→格号」那一半，「多格需求选边」在引擎层、「满盘 ⇒ 督军锚点」那一路无等价物。
- `SlotPosToBoardPos`：它是**并入** `Position` 的 y 项（不是一条干净的分派支），且判据侧 0 调用者 ⇒ 认不出在哪条链上。
- 另：这 4 行也都还欠 G3 的**非自证**我们侧读数（槽位坐标是纯函数，从我们自己代码算 = 自证）。

---

## 二 逐格对照表（可复算）

**算法**（脚本 `_tmp_view/B8_AR/b8ar_table.py`，只读两个数据源，不读我们代码的注释）：
- **原版侧**：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4372.json`（我）/ `_4373.json`（敌）的 `MinionSeparation` / `minionExtraDistanceFromHero` / `slotsPerSide` / `heroExtraOffset` / `ExtraYOnBoard`，套 `MinionManager__Awake.c:59-62`（左）/ `:101-103`（右）/ `BattleManager__GetHeroBoardPos.c:16-28`（督军）的式子；世界 z 再叠加 `Transform/Transform_1309.json`（**−6.655**）/ `_1314.json`（**+1.043**）的 local z。
- **我们侧**：从 `ArenaSlots.cs` **源码文本**里正则抓常量（`PlayerPitch/PlayerExtra/PlayerZ/PlayerHeroExtraZ/Enemy*/WarlordSlot/SlotsPerSide/ExtraYOnBoard`），套 `Position` 的式子。

| 我们槽 | 原版有符号槽 | 原版取法 | 原版 x | 原版 y | 原版 z(世界) | 我们 x | 我们 y | 我们 z | Δx | Δy | Δz |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | −4 | 我 · GetLeftSlotPos(3) → left[3] | −3.370000 | 0 | −6.655000 | −3.370000 | 0 | −6.655000 | −2.5e-08 | 0 | +2.1e-07 |
| 1 | −3 | 我 · GetLeftSlotPos(2) → left[2] | −2.550000 | 0 | −6.655000 | −2.550000 | 0 | −6.655000 | −1.8e-08 | 0 | +2.1e-07 |
| 2 | −2 | 我 · GetLeftSlotPos(1) → left[1] | −1.730000 | 0 | −6.655000 | −1.730000 | 0 | −6.655000 | −1.1e-08 | 0 | +2.1e-07 |
| 3 | −1 | 我 · GetLeftSlotPos(0) → left[0] | −0.910000 | 0 | −6.655000 | −0.910000 | 0 | −6.655000 | −3.6e-09 | 0 | +2.1e-07 |
| 4 | 0 | 我 · **BattleManager.GetHeroBoardPos**（+zeroVector） | 0 | 0 | −7.075000 | 0 | 0 | −7.075000 | 0 | 0 | +2.0e-07 |
| 5 | +1 | 我 · GetRightSlotPos(0) → right[0] | +0.910000 | 0 | −6.655000 | +0.910000 | 0 | −6.655000 | +3.6e-09 | 0 | +2.1e-07 |
| 6 | +2 | 我 · GetRightSlotPos(1) → right[1] | +1.730000 | 0 | −6.655000 | +1.730000 | 0 | −6.655000 | +1.1e-08 | 0 | +2.1e-07 |
| 7 | +3 | 我 · GetRightSlotPos(2) → right[2] | +2.550000 | 0 | −6.655000 | +2.550000 | 0 | −6.655000 | +1.8e-08 | 0 | +2.1e-07 |
| 8 | +4 | 我 · GetRightSlotPos(3) → right[3] | +3.370000 | 0 | −6.655000 | +3.370000 | 0 | −6.655000 | +2.5e-08 | 0 | +2.1e-07 |
| 0 | −4 | 敌 · GetLeftSlotPos(3) → left[3] | −6.250000 | 0 | +1.043000 | −6.250000 | 0 | +1.043000 | −1.2e-07 | 0 | +1.7e-08 |
| 1 | −3 | 敌 · GetLeftSlotPos(2) → left[2] | −4.720000 | 0 | +1.043000 | −4.720000 | 0 | +1.043000 | −9.1e-08 | 0 | +1.7e-08 |
| 2 | −2 | 敌 · GetLeftSlotPos(1) → left[1] | −3.190000 | 0 | +1.043000 | −3.190000 | 0 | +1.043000 | −6.2e-08 | 0 | +1.7e-08 |
| 3 | −1 | 敌 · GetLeftSlotPos(0) → left[0] | −1.660000 | 0 | +1.043000 | −1.660000 | 0 | +1.043000 | −3.3e-08 | 0 | +1.7e-08 |
| 4 | 0 | 敌 · **BattleManager.GetHeroBoardPos** | 0 | 0 | +0.293000 | 0 | 0 | +0.293000 | 0 | 0 | +1.7e-08 |
| 5 | +1 | 敌 · GetRightSlotPos(0) → right[0] | +1.660000 | 0 | +1.043000 | +1.660000 | 0 | +1.043000 | +3.3e-08 | 0 | +1.7e-08 |
| 6 | +2 | 敌 · GetRightSlotPos(1) → right[1] | +3.190000 | 0 | +1.043000 | +3.190000 | 0 | +1.043000 | +6.2e-08 | 0 | +1.7e-08 |
| 7 | +3 | 敌 · GetRightSlotPos(2) → right[2] | +4.720000 | 0 | +1.043000 | +4.720000 | 0 | +1.043000 | +9.1e-08 | 0 | +1.7e-08 |
| 8 | +4 | 敌 · GetRightSlotPos(3) → right[3] | +6.250000 | 0 | +1.043000 | +6.250000 | 0 | +1.043000 | +1.2e-07 | 0 | +1.7e-08 |

**不一致格数 = 0**（Δ 全是 float32 尾数噪声）。

**原版数组（由预制体字段直接算）**：

| 侧 | left[0..3] | right[0..3] | heroExtraOffset | ExtraYOnBoard | 节点 z |
|---|---|---|---|---|---|
| 我 | −0.91 / −1.73 / −2.55 / −3.37 | +0.91 / +1.73 / +2.55 / +3.37 | (0, 0, **−0.42**) | **0** | −6.655 |
| 敌 | −1.66 / −3.19 / −4.72 / −6.25 | +1.66 / +3.19 / +4.72 / +6.25 | (0, 0, **−0.75**) | **0** | +1.043 |

**序列化死值**（会被 `Awake` 整组重建 ⇒ 谁照它做谁错）：
- 我：`leftSlotPosNormal` = (−3, −0.6) (−6, −0.6) (−9, −0.6) · `rightSlotPosNormal` = (+3, −0.6)…
- 敌：`leftSlotPosNormal` = (**+2.667**, 5.0) (**+5.333**, 5.0) (**+8**, 5.0) · `rightSlotPosNormal` = (−2.667 …)
- 两份都**只有 3 项**（`slotsPerSide = 4`）、y = 5.0 / −0.6。

---

## 三 改了哪个文件、哪些行（净 **+49** 行，`Position` **零行号漂移**）

文件：`Unity/MyGame/Assets/CardPresentation/Board/ArenaSlots.cs`（173 → 222 行）

| 位置 | 改法 | 行数 |
|---|---|---|
| `:94-97` | `Position` 的 XML doc 就地重写：`y = ExtraYOnBoard`（并写明**督军支不吃这一项**，因为它走的是另一个原版方法 `BattleManager.GetHeroBoardPos`） | **4 → 4 行**（零变化） |
| `:104` | `int n = Mathf.Abs(k);` → `int n = ClampSideIndex(slot, Mathf.Abs(k));` | **1 → 1 行** |
| `:109` | `return new Vector3(x, 0f, z);` → `return new Vector3(x, ExtraYOnBoard(enemy), z);` | **1 → 1 行** |
| `:173-220`（类尾新增） | `SlotsPerSide` / `PlayerExtraYOnBoard` / `EnemyExtraYOnBoard` / `ExtraYOnBoard(bool)` / `ClampSideIndex(int,int)` + `Vector3.up` 的**推导链**注释 | **+49 行** |

**为什么故意做成零漂移**：`Position` 是 `:98-110`，被 **10 条账行 + `Editor/BattleScene.cs:20187` 的注释**按行号引用（那句写的是 `见 ArenaSlots.cs:102` = 督军位多一个 z 偏移那一行）。改动全部塞在 `:104` / `:109` 的**行内**与**类尾** ⇒ `:102`、`:98-110` 全部原地不动。

**补齐的两条缺口（都有判据）**：

1. **越界夹取 + 出声**（`GetLeftSlotPos` / `GetRightSlotPos`）
   判据 = `MinionManager__GetLeftSlotPos.c:25-42` / `__GetRightSlotPos.c:25-42`：下标 ≥ 数组长度 ⇒
   `CustomDebug.LogError("Trying to GetLeftSlotPos with slotNum too high " + slot)`（原串见 `cpp2il/out_cpp2il_dev_ilspy/MinionManager.cs:466 / :507`）**然后夹到 `Length − 1`**；负下标经 `uint` 变极大 ⇒ 走**同一条**支。
   我们等价：`ClampSideIndex` 把侧内下标夹到 `SlotsPerSide`（= 最外一格）并打一条 `Debug.LogError`（串里带原版英文原句与出处）。⇒ **两侧越界都落在最外一格**，与原版同值同声。
2. **`ExtraYOnBoard`**（`SlotPosToBoardPos` / `GetNextSlotPos`）
   判据 = `MinionManager__SlotPosToBoardPos.c:15-21` + `__GetNextSlotPos.c:266-273`：`落点 + up × ExtraYOnBoard`。两份预制体的 `ExtraYOnBoard` 都是 **0**（13 场 × 2 实例逐场实读）⇒ 净恒等，但按铁律 11 照原版把这一跳留着（`Position` 的 y）。
   **顺手把「那枚静态向量 = `Vector3.up`」证了**（旧账只是断言）：`GetHeroBoardPos.c:19-27` / `GetLeftMostPos.c:54-56` 读同一 `static_fields` 的 **+0x00** 那枚、而它们语义上是「加一个零」⇒ +0x00 = `zeroVector`；`Vector3` 静态字段按 12B 紧排（zero→one→up→…）⇒ **+0x18 = up**（独立佐证：ILSpy 对 z 分量注的 `+20` 读出 0 ⇒ 排除 one/forward）。推导写进 `PlayerExtraYOnBoard` 的 doc。

---

## 四 断言草稿（**需插进 `Editor/BattleScene.cs`，不在我白名单**）

插在 `Editor/BattleScene.cs:20681-20682` 那一条（`Position(5,false).z` 那个 `Check`）**之后**，同一段「纯函数判据」里。
⚠️ 现有那几条断言把期望值写成**字面量** `4 * 0.82f + 0.09f`（不是引用我们的常量）⇒ 这是**好**的（灭自证）；下面 4 条照这个写法。

```csharp
// ① 内圈那一对（现有只测了槽 0 / 5，缺 left[0] 与敌方内圈）
Check(Mathf.Abs(ArenaSlots.Position(3, false).x + (1 * 0.82f + 0.09f)) < 1e-4f
      && Mathf.Abs(ArenaSlots.Position(7, true).x - (3 * 1.53f + 0.13f)) < 1e-4f,
      "★ 内/外圈两侧：我槽 3 x=−0.91（left[0] 贴督军）· 敌槽 7 x=+4.72（right[2]）"
    + "｜判据 = `MinionManager__Awake.c:59-62 / :101-103`");

// ② 最外一格【精确值】（现在只有符号断言，没有数值）
Check(Mathf.Abs(ArenaSlots.Position(8, false).x - (4 * 0.82f + 0.09f)) < 1e-4f
      && Mathf.Abs(ArenaSlots.Position(8, true).x - (4 * 1.53f + 0.13f)) < 1e-4f,
      "★ 最外一格：我 +3.37 / 敌 +6.25（`GetRightMostPos` = right[slotsPerSide−1]）"
    + "｜🧨 把 `Position` 的 n 写死成 4 而丢掉 extra ⇒ 两条一起红");

// ③ 落点 y 恒 0（原版 ExtraYOnBoard = 0）
int badY = 0;
for (int s = 0; s < ArenaSlots.SlotCount; s++)
    if (Mathf.Abs(ArenaSlots.Position(s, false).y) > 1e-6f
        || Mathf.Abs(ArenaSlots.Position(s, true).y) > 1e-6f) badY++;
Check(badY == 0 && ArenaSlots.PlayerExtraYOnBoard == 0f && ArenaSlots.EnemyExtraYOnBoard == 0f,
      "★ 落点 y 全 0 —— 原版 `SlotPosToBoardPos` 加的是 `up × ExtraYOnBoard`，"
    + "两份预制体实读 0（13 场 × 2 实例）｜🧨 把 ExtraYOnBoard 当非 0 填 ⇒ 卡浮起来 + 这条红");

// ④ 越界夹取（原版 `GetLeft/RightSlotPos.c:25-42`：夹到最外一格 + LogError）
string clampLog = null;
Application.LogCallback clampCb = (cond, stk, type) =>
{ if (type == LogType.Error && cond.Contains("ArenaSlots") && cond.Contains("槽号越界")) clampLog = cond; };
Application.logMessageReceived += clampCb;
var badLow  = ArenaSlots.Position(-1, false);
var badHigh = ArenaSlots.Position(ArenaSlots.SlotCount, false);
Application.logMessageReceived -= clampCb;
Check(Mathf.Abs(badLow.x  - ArenaSlots.Position(0, false).x) < 1e-4f
      && Mathf.Abs(badHigh.x - ArenaSlots.Position(8, false).x) < 1e-4f,
      "★ 越界夹取：Position(−1) ≡ 槽 0（左最外 −3.37）· Position(9) ≡ 槽 8（右最外 +3.37）");
Check(clampLog != null,
      "★ 越界**出声**了（原版同一情形打 \"Trying to GetLeftSlotPos with slotNum too high\"）"
    + "｜🧨 删掉 `ClampSideIndex` 那一声 ⇒ 这条红（位置那条仍可能绿）");
```

⚠️ **④ 会真的打一条 `Debug.LogError`**。若宿主把 LogError 计进「失败」，请先用 `Application.logMessageReceived` 接住（草稿里已经这么写了）；**不要**因为怕脏日志就不加这条 —— 「不许静默失败」是项目红线。

---

## 五 没查清 / 做不到的部分

1. **`slotsPerSide` 的运行时值拿不到**：`Awake` 是从 `PlayerDataManager.singletonManager.matchData.GameplayData.slotsPerSide`（`MinionManager.cs:156-162`）读的，那个 SO 在**运行时数据包**里、离线包里没有。两份预制体序列化都是 **4** ⇒ 我们固定 4，与之一致；**但若线上 SO 给别的值，我们会与它不一致**（这条如实记在 `Awake` 行的「差异」里）。
2. **`isWeaponSetup` 分支永久不可达（判据侧）**：全 ILSpy 树 **0 处赋值**（只有声明 + 读），两份实例序列化都是 `0` ⇒ 按铁律 11 例外①不复刻。**如果**将来发现有赋值点（比如某个模式），这条要翻案。
3. **`FillMinionPositions` 到底谁调**：`decomp_full` 与 ILSpy 全树 **0 处调用**。它**自己会分配数组**（`.c:14-19`），所以旧记「只是被内联」不成立为**证据**、只是**一种解释**。真正的实现关系（Awake 调它 / 它调 Awake 的私有体 / 两者都被同一段源码展开）**没查到**。
4. **`GetNextSlotPos` 的 `AvailableLeft/RightSlot` 语义没逐句核**：只确认了「首选侧满 ⇒ 落对侧下一个空位」。那两个 helper 的实现（`MinionManager__AvailableLeftSlot.c` 14 行）**没读**。
5. **`ArenaSlots.TryResolveSlot` 的容差（`pitchPx * 0.55` / `tolY = 行距 × 0.5`）是我们挑的**，不是原版值 —— 原版走的是 `GetClosestAvailableSlot` 的「合法插入区间 + 最近」，与我们不同构（那一半在账行 1250，已由 F3 记 B）。本批没动它。
6. **表示法差异（不打算改，但要记）**：原版 `GetSlotPos` / `GetLeft|RightMostPos` 返回的是 **`MinionArea` 局部**坐标（`Vector2` ⇒ z = 0），我们的 `Position` 把**节点 z 烘进了返回值**（我 −6.655 / 敌 +1.043）。同一空间约定下等价（所有调用点都 `boardRoot.TransformPoint`），**但跨层复用时要注意**。

---

## 六 顺手发现（**未越白名单，请调度台分流**）

1. **两个报告的 `ArenaSlots.cs` 行号已过期**（不是本次改坏的，是它们写完之后文件又动过）：
   - `报告/B8_M_敌方槽位镜像.md:102-104` 写「`ArenaSlots.cs:110` = `return new Vector3(enemy ? -x : x, 0f, z)`」——现在 **`:109` 是 return、`:110` 是 `}`**，而且 `:109` 的内容已再次变成 `return new Vector3(x, ExtraYOnBoard(enemy), z)`。
   - `报告/B8_F_表现层.md:49/91/154` 同样引用 `ArenaSlots.cs:109` 当「敌方取负那一行」。
   ⇒ 建议把那几处改成「**改成 `Position` 的 return 行（2026-10-26 起 = `:109`）**」，别写死行号。
2. **`Editor/CardRootAssert.cs:306-308`** 的断言文案里写「判据 = `ArenaSlots.Position` 的 **y 恒 0**」——现在更准确的说法是「**y = `ExtraYOnBoard`，13 场实读全 0**」。**断言本身仍然成立**（值没变），只是措辞可以升级。
3. **`GetLeftMostPos` / `GetRightMostPos` 的真实消费者是 `CardAnimController.cs:100/104`**（`RotateAnim`，`pointTowardsOption == boardCenter`，用它算「朝棋盘中心」的方向）。我们的 `Position(0/8, enemy)` 与它**逐值同**（§二），所以**只要 `RotateAnim` 那条链在我们这边也取到同样的两端点，就没有缺口** —— 这属于**特效/表现线**，建议那边核一眼。
4. **`GetSlots` 不是死代码**：调用点在 `MinionManager.cs:317/318`（`GetClosestAvailableSlot`）与 `:556/558`（`OnDrawGizmosSelected`）。我们只是**没有等价的批量 API**，几何本身由 `Position` 逐格覆盖。
5. **`GetNextSlotPos` 的「满盘 ⇒ 督军 `GetEffectAnchor().position`」那一路会跳过 `ExtraYOnBoard`**（`.c:207` 直接跳到 `:274` 的 return）。今天无害（ExtraYOnBoard ≡ 0），但**将来若它非 0，这一支的 y 会与另外两支不同** —— 真要复刻时别统一加。
6. **`AdjustMinionBoardPos` / `GetDummyDistance` / `SlotPosToBoardPos` / `FillMinionPositions` 在 ILSpy 全树都是 0 调用**（我顺手 grep 过）。前两条不在本批行内（落点 `—` 或 `BattleDriver.cs`），**留给对应线的代理**，别据此判 D（0 调用 ≠ 死代码，`FillMinionPositions` 就仍可能是被内联的活代码）。

---

## 七 给调度台的请求（1 / 2 已裁定，3–5 仍待办）

1. ✅ **已裁定（2026-10-26）**：这 6 行**按 §4.1「共享落点」升 A** ⇒ 已交 `_parts/B8_AR2.tsv`（判据要点逐条写清「这段代码如何分别满足 6 个方法」+ 每条一个可观察量）。`Awake` / `FillMinionPositions` / `GetNextSlotPos` / `SlotPosToBoardPos` 按裁定**仍记 B**（§一·2 的五条真差异）。
2. ✅ **已裁定（2026-10-26）**：**不要**给 `ArenaSlots` 加 1:1 入口（`LeftSlotPos` 那六个）—— 今天无消费者，加了就是死件，撞「⛔ 别建死件」。
3. **§四 的 4 条断言草稿由调度台插进 `Editor/BattleScene.cs`**（不在我白名单）；原文已随回复贴出。
4. **`1274 / 1285 / 1291` 三行建议移交 `战斗界面_HUD` / CardPresentation 线**（§一·1 末段）。
5. **§六·1 的两份报告行号**（`B8_M` / `B8_F`）调度台已答：**收口时统一改成符号名**，本代理不动。

---

## 附：本次核查读过的原版判据（可复现）

```
decomp_full/MinionManager__Awake.c                    1-107   （全文）
decomp_full/MinionManager__FillMinionPositions.c      1-61    （全文）
decomp_full/MinionManager__GetLeftSlotPos.c           1-70    （全文）
decomp_full/MinionManager__GetRightSlotPos.c          1-70    （全文）
decomp_full/MinionManager__GetSlotPos.c               1-11    （全文）
decomp_full/MinionManager__GetSlots.c                 1-61    （全文）
decomp_full/MinionManager__GetLeftMostPos.c           1-58    （全文）
decomp_full/MinionManager__GetRightMostPos.c          1-58    （全文）
decomp_full/MinionManager__SlotPosToBoardPos.c        1-23    （全文）
decomp_full/MinionManager__GetNextSlotPos.c           1-276   （全文，旧记只读到 120）
decomp_full/BattleManager__GetHeroBoardPos.c          全文（督军位 = heroExtraOffset + zeroVector）
cpp2il/out_cpp2il_dev_ilspy/MinionManager.cs          131-200 / 218-272 / 381-438 / 440-531 /
                                                      1648-1749 / 1750-2000 / 3069-3085
bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4372.json · _4373.json   （字段全读）
bundle_scenes_scenes_battlearena1/Transform/Transform_1309.json · _1314.json            （节点 z / 旋转 / scale）
工具/read_literal.py：0x1834b2c80 = −0.0f · 0x1834b2bb8 = +1.0 · 0x1834b2bc8 = −1.0 · 0x1834b2bb0 = 0.2
```
