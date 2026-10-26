# B8_T — 自检夹具三条结构性缺陷（`BattleScene.cs`）

> 执行代理 `B8_T` · 2026-10-26 · 白名单只碰 `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`
> ⛔ 没跑 Unity（`BattleScene.Run` 会 `NewScene → SaveScene` 覆盖 `Battle.unity`）⇒ 只跑了秒级类型检查。
> ✅ `TMPDIR=/tmp/wf_b8t bash d:/4/Unity/工具/typecheck.sh` → **编辑器程序集 0 错**（本文件所在程序集）。
> ⚠️ 同期 `CardView.cs` 正被别的代理改（运行时程序集报 3→8 个 `CS0103`，全部落在那份文件上）⇒ 与本批无关。

---

## ① 结论（四条各改成什么样）

### 任务 1 — `ProbeRemnantView` 的 `ctx.Players[0]` 写死 ✅

- 新增形参 `int seat = -1`：**缺省 `-1` = 当前视角**（`driver.MyIndex`），不是写死的 0。
- 摆卡：`ctx.Players[seat].Board[slot]`。
- 读视图：`driver.BoardViewAt(slot, mine: seat == driver.MyIndex)` —— `mine` 由「这一侧是不是本机」算，
  ⛔ 不再假定恒 `true`（原来缺省就是 `true`）。
- **读不到就出声**：`seat` 越界 / `ctx` 为空 / `ctx.Players` 为空 / `BoardSpec.IsValid(slot)` 为假 ⇒
  `Debug.LogError(...)` + **返回 null**（⛔ 不静默退回 0 号座位）。
- 5 个现有调用点**一个都不用改**（都不传 `seat` ⇒ 取 `MyIndex`，与改前逐位相同）。

### 任务 2 — `SimulateTapUnit` 的 bool 不可区分「被封锁 / 窗没开」 ✅（自检侧根治；驱动侧留改动请求）

🔴 **为什么不是改 `SimulateTapUnit` 本身**：它在 `CardPresentation/Battle/BattleDriver.cs`，
**本批没有那个文件的写权限**（有专人同时在改）。⇒ 在 `BattleScene` 类尾补一层：

- `enum TapOutcome { NotBlockedAndOpened, Blocked, NotBlockedButNotOpened }`
- `static TapOutcome TapUnit(BattleDriver driver, BattleContext ctx, int side, int slot)`
  —— **先读前置闸、再点**；`opened` ⇒ 第一态；否则 `blockedUpFront ? Blocked : NotBlockedButNotOpened`。
  **不是 `NotBlockedAndOpened` 时打一行 `[自检·轻点] … 三态 = …`**（连演出闸/回放/选择器三个读数一起打），
  ⇒ `Check(...)` 只印文案、读不到三态值那个盲区补掉了。
- `static bool TapIsBlockedUpFront(...)` —— 逐条对应驱动里那几道**早退闸**（每条都写了出处）：
  `Ctx.IsOver` · `WaitingForBlockingAction`（= 原版 `UIstate != normalBattle`，`CanUseWaystone.c:44-47`）·
  `ReplaySession` · `SelectorOpen`（`BoardPress` ① 段）· 那格没单位（`BoardPress` ⑤）·
  **敌方那侧的灵魂石**（`TapBoardCard` 的 `if (side != _me) return;`）。

**调用点**（用返回值的 9 处全部换掉；**9 处纯拨开关的没动** —— 见 §④）：

| 位置 | 旧 | 新 | 通过条件变没变 |
|---|---|---|---|
| 轻点我方 / 有 buff / 态 A / 态 B / 对手 5 处 | `Check(SimulateTapUnit(…))` | `== NotBlockedAndOpened` | **不变**（等价于旧 bool == true） |
| `G16` 演出期点灵魂石 | `Check(!opened && 三条旁证)` | `== Blocked && 三条旁证` | **不变**（旧绿 ⇒ 新绿；删掉那道闸仍红，靠三条旁证） |
| `G16` 正常点灵魂石（收集） | `Check(!opened)` | `== NotBlockedButNotOpened` | **不变**（且更强：被闸挡掉也会红） |
| `G16` 点敌方灵魂石 | `Check(!opened && 三条旁证)` | `== Blocked && 三条旁证` | **不变** |
| `G16` 点督军 | `Check(opened && 指针不动)` | `== NotBlockedAndOpened && 指针不动` | **不变** |

### 任务 3 — 2D 退回支「碰巧绿」 ✅（补上了判据本身）

原来**一条断言都没有**判「这一组到底有没有真走到 `DebugNoBody3D` 那条支路」。

`A` 已把夹具阵营从假的 `"Test"` 换成真阵营（`ProbeRemnantView` 缺省 `"SaimHann"`），
静态走查已通：假阵营时 `CardArt.Frame` 回 null ⇒ `Build` 走**占位卡面** `else`，
而 `BuildBody3D` **只在 `frameTex != null` 那条支路里被调**（`Core/CardView.cs:4361`，`Build` 内）
⇒ 开关那句 warning（`Core/CardView.cs:7704`）**发不出来**。

现在补的是**判据本身**：用 `Application.logMessageReceived` 把 `ProbeRemnantView` 那一趟的 warning
抓下来（与该文件 1295/2716 两处已有写法同一条路），断言
`v2D != null && Body3DWorldScale == 0 && nD2D >= 1`：

- `BuildBody3D` **第一句**就是那个开关的 `return null` ⇒ warning 出现即**同时**证明
  「走的是真卡框支路」+「`Body3DWorldScale == 0` 是这个开关造成的」，**不是**占位支路（那条根本不调它）。
- 对照 = 上面 3D 组那条既有断言 `vA.Body3DWorldScale != 0`（同一套夹具、开关关着）。两条合起来是完整对照实验。
- ⛔ 没有放宽任何断言；原来那两条 2D 竖线原样保留。

### 任务 4 — `[G21·探针]` 汇总 ✅

在三处原始日志之后补了一条 `[G21·探针·汇总]`（组名 / slot / 认领前 / 认领后 / 同一对象 / Face / Body3DWorldScale），
格式化收在类尾 `G21ProbeRow(...)` 一处。**原始三行照旧保留**（它们打在读数产生的那一刻）。

---

## ② 改了哪些行（供判账漂移）

- 文件行数：**47,492 → 47,644（净 +152）**。行尾仍是**纯 CRLF**（`crlf=47644 / lf=47644 / 裸 CR=0`，二进制核过）。

| 段（当前行号） | 增量 | 是什么 |
|---|---|---|
| `:16559-16581` | **+21** | 2D 组 warning 捕获（注释 9 行 + 捕获 14 行） |
| `:16589` 那条 `Check` | +1 | 加了第二/第三个判据子句与说明 |
| `:16646-16653` | **+8** | `[G21·探针·汇总]` |
| `:36569` / `:36595` / `:36609` / `:36621` 四条 `Check` | **+9** | 三态断言与文案（四条的 `var xxx = …` 是 1:1 换行） |
| `:47236` 起（`ProbeRemnantView`） | **+16** | `<param name="seat">` 5 行 + 越界守卫 10 行 + 参数 1 行 |
| `:47510` 之后（类尾） | **+97** | `enum TapOutcome` + `TapUnit` + `TapIsBlockedUpFront` + `G21ProbeRow`（含注释） |

**账漂移：0 条新增。**
判据：全账里对本文件的 A 档落点**只有 1 条** —— `战斗逻辑驱动.tsv` 的
`CardPresentation/Editor/BattleScene.cs:13558`，它在**所有改行点之前**（最小改点 16,559）⇒ 不受影响。
`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py`（只读、没加 `--fix`）现读：
该表现在仍是 `A=300 通过=299 漂移=1`，那 1 条漂移是 `RuleEngine/Core/AchievementTally.cs` 的（既有，非本批）。
⚠️ **同一时刻 `CardView.cs` 正被别的代理改**，全表漂移数在两次核账之间从 9 涨到 61、失败 2
—— **全部落在 `CardView.cs`**（那份文件此刻还编不过），与本批无关；等它落盘后要重跑一次核账。

---

## ③ 要重跑什么 / 期望读数

- **必跑**：`TMPDIR=/tmp/wf_b8t bash d:/4/Unity/工具/typecheck.sh` → 期望 **运行时 0 / 编辑器 0**
  （我最后一次跑是 `编辑器 0`、`运行时 8`，8 条全在 `CardView.cs` 的 `CS0103`，是别人的在飞改动）。
- **必跑**：`BattleScene.Run`（唯一受影响宿主；这次改的全在它里面）。
  - 断言合计应比改前 **不变**（新增的只有 2D 组**那一条**从加强 → 还是 1 条）。⚠️ 实际总数以调度台现读为准。
  - 日志里应出现 **恰好 1 条** `[G21·探针·汇总]`，其中 2D 组那行 `Body3DWorldScale=0.00000`、
    3D 组那行**非 0**。
  - 日志里应出现 `[CardView] `DebugNoBody3D` 开着`（若没有 ⇒ 任务 3 的判据当场红，见下）。
- **若任务 3 那条红**：说明夹具**还是没走到**真卡框支路。先看那条的 `nD2D`（0 = 警告一条没发）：
  - `nD2D == 0` ⇒ `frameTex == null` 或视图是被**认领**来的（没重新 `Build`）⇒ 查
    `Resources/Art/cards/frame_saimhann_tier4.png` 在不在、以及 `free1` 那一格有没有被 `SyncBoard` 认领。
  - `nD2D >= 1` 但 `Body3DWorldScale != 0` ⇒ `BuildBody3D` 里开关那段被人挪到网格检查之后了。
- **不用跑**：其余宿主（本批只碰了 `Editor/BattleScene.cs`，它是纯自检宿主，不是共用件）。

---

## ④ 没查清 / 没做的部分（如实）

1. **`SimulateTapUnit` 本身还是 bool** —— `BattleDriver.cs` 不在本批白名单。
   **改动请求（交给调度台排）**：等那个文件空下来，把 `BoardPress` / `TapBoardCard` 的「这一下归谁」
   逐层 return 出来，让 `SimulateTapUnit` 直接返回三态；届时**删掉** `TapIsBlockedUpFront`
   （那里现在是「判据的再述」= 自检侧预言机，长期放着有漂移风险）。
2. **`TapIsBlockedUpFront` 的覆盖面**是按**本文件现有夹具用得到的那几道闸**列全的，不是「驱动里所有
   可能的吞点」。已知没覆盖：`AwaitingPlayTarget`（`BattleUiIsNormalBattle` 也认它，但它是 private）、
   打牌/拖拽那些路径。⇒ 将来谁用 `TapUnit` 造新夹具，**先看那条注释里的闸清单**。
3. **9 处纯拨开关的 `driver.SimulateTapUnit(…)` 没改成 `TapUnit`**（`:9492/9529/9530/9556/9557/9572/9651/9678/9693`）：
   它们**不用返回值**，而且期望的正是「窗关着」—— 套上 `TapUnit` 只会每关一次窗就多一行
   `三态 = NotBlockedButNotOpened` 的噪声日志。每条后面本来就跟着自己的 `Check(!CardDisplay.Visible, …)`。
4. **没跑 Unity ⇒ 「2D 组真走到那条支路」是静态结论 + 一条新断言**，不是实跑读数。
   静态依据：`ProbeRemnantView` 该调用点没传 `faction:` ⇒ 缺省 `"SaimHann"` ⇒
   `frame_saimhann_tier1..4` 全在 `Resources/Art/cards/` ⇒ `frameTex != null` ⇒ `Build` 里 `:4361` 被调。
5. **`CardView.cs` 的行号在我注释里是快照**（`:4361` / `:7704`）。那份文件正被别人改，可能位移；
   注释里同时写了方法名（`BuildBody3D`），按名字也能找到。

---

## ⑤ 顺手发现

1. 🔴 **`CardView._frame` 在场上形态下恒为 `null`**（`Core/CardView.cs:4412`：`if (!board) _frame = AddLayer("frame", …)`）。
   ⇒ **`FrameTexture` / `FrameDrawSize` / `FrameUvRect` 这三个「自检用」口，在 `CardFace.Board` 的卡上恒答 null/零值**。
   我第一版任务 3 的判据就是拿 `v2D.FrameTexture != null` 判「不是占位支路」——**会恒红**，已改掉。
   📌 值得记一条：**场上卡没有「框贴图」这个可读口**；要验「场上是不是走了真卡框支路」，
   目前只能靠 `BuildBody3D` 那条 warning（本批用的办法）或根 `MeshRenderer.enabled`（框支路会把它关掉，
   占位支路会把它当 `_face` 用）—— 后者更隐晦，所以没采用。
2. `ProbeRemnantView` 的 `face`/`rarity` 都走 `CardData` 那条链；它的 `rarity = null` ⇒
   `CardArt.TierOf(null, faction)` 决定卡框档。**夹具卡框档不是显式给的**，将来若有人改 `TierOf` 的缺省，
   这条夹具的卡框会跟着变（今天不影响任何断言，因为只判「在不在」）。
3. `BattleScene.cs` 里对 `ctx.Players[0]` / `FreeSlot(ctx, 0)` 的**写死座位**还有多处
   （例如 `:9476-9482` 的轻点/拖拽那一组、`:15957` 的 `emptyCount` 扫描）。
   本批只按指派修了 `ProbeRemnantView` 一处 —— 其余**同样会被翻座位夹具静默读错侧**，建议另立账。
