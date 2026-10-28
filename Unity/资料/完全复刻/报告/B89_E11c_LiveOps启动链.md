# B89-E11c · LiveOps 启动链接线（`LiveOpsManager.Instance` → 运行期）

> 执行代理 `B89-E11c` · 2026-10-28（接 `B89-E11b`；判据沿用）
> 前两批：`报告/B89_E11_GameModes.md` · `报告/B89_E11b_GameModes接线.md`

---

## §0 一句话答案

**接到了 `MainMenuRuntime.Awake`**（`Shell/MainMenuRuntime.cs:55`，一行内追加，**行数不变**）→ 调 `LiveOpsManager.EnsureStarted()`；
**幂等** = 单例 getter 自带 `_instance == null` 闸 + `EnsureStarted` 里 `_bootLogged` 只打一次日志（重复调用返回同一实例，断言 G5）；
**断言** = `GameModesCheck` G1–G5：`ResetBootForTest()` 先把单例复位（G1 前提：此时必须还是 null）→ 建一个 `MainMenuRuntime` 并**手动 `SendMessage("Awake")`**（编辑模式下 `AddComponent` 不跑 `Awake`）→ G2 `Instance != null` / G3 `GetHandler<GameModesHandler>() != null`；**灭自证 = 把 Awake 里那句注掉，G2 必红**。

---

## §1 接在哪、为什么是这儿

| 候选 | 判定 | 依据 |
|---|---|---|
| **`MainMenuRuntime.Awake`** ✅ 本批接在这里 | **进主菜单之前跑过**，且两条进主菜单的路（壳 `LoadSceneWithFade` 进 `MainMenu.unity` / 自检 `MainMenuScene` 直接建场）都覆盖 | `MainMenuRuntime` 是 `MainMenu.unity` 根上的组件（`Editor/MainMenuScene.cs:635` `AddComponent<MainMenuRuntime>()`），`Awake` 早于 `Start()` 里的 `Build()` |
| `ShellRuntime.Awake`（**真正的第 0 个场景**） | ⚠️ **更早的等价入口** —— Build Settings 里 `Shell.unity` 是**索引 0**（`ProjectSettings/EditorBuildSettings.asset`），`ShellRuntime` 还 `DontDestroyOnLoad` | 该文件本批**无权限** ⇒ 已在 §5 报给调度台：给它扩权后在那里再调一次同一个幂等口（两处并存无副作用） |
| `Core/PlayerBoot.Boot`（`RuntimeInitializeOnLoadMethod`） | ❌ 不用 | 它第 2 句就 `if (Application.isEditor) return;`、且**没命令行参数就 return**（`PlayerBoot.cs:70/97-98`）⇒ 编辑器/普通启动都不跑 |

**加的成员（全部在类尾，⛔ 没动任何已有行）**
- `Shell/LiveOpsManager.cs:369` `EnsureStarted()`：建单例 → 登记内建 handler；**失败 `LogError`**（⛔ 不静默）；`:398` `ResetBootForTest()`（只给断言）。
- `Shell/MainMenuRuntime.cs:3865` `ReadTextInHandFormatAtBoot()`（见 §3）。

**行数变化（`MainMenuRuntime.cs` 被账按行号引用 ⇒ 逐条报）**：`3853 → 3874`（**+21 行，全部追加在类尾**）；`Awake` 那一行仍是**1 行 → 1 行**（`git diff --numstat` = `22 1`，那 1 删 1 增就是它）。`LiveOpsManager.cs` `356 → 404`（+48，同样全在类尾 ⇒ `:92` / `:143` / `:151` / `:329` 这些账落点**一个都没漂**）。

---

## §2 🔴 关键问题：`LiveOpsLocalHandler` 登记之后，活动线 UI 会真的读到数据吗？

**答案：会读到 —— 但它本来就读得到；这次登记改变的不是「有没有数据」，是「原版形状的取口有没有」。**

分三层说清（都现读）：

1. **数据早就在（与本批无关，已接）**：`LiveOpsLocalBoot.BootLocalLiveOps` 挂着
   `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`（`Shell/LiveOpsHandler.cs:1510-1516`）——
   player / 编辑器 Play 启动第一次场景加载完就 `LiveOpsLocalHandler.Default.LoadFromCatalog(NowUtc)`，
   把 13 份本地 SO 装成事件实例（**按 `ShouldLoadEvent` 滤掉 Expired/NotAvailable**）。
   ⇒ **不存在「登记了但 catalog 是空的」这个问题**。
2. **UI 根本没走注册表**：本仓活动线窗口读的是 **`LiveOpsLocalCatalog` / `LiveOpsLocalHandler.Default`（直读）**，
   不是 `LiveOpsManager.GetHandler<…>()` —— 全仓 `LiveOpsManager.GetHandler` 的命中**全部是注释里引用原版**
   （`ExpansionPassEventWindow.cs:2491`、`FastModeBaseEvent.cs:588`、`InboxWindow.cs:423` … 都是在解释「原版本该这么取」）。
   ⇒ **本批的接线不会让现有活动窗口多显示或少显示任何东西**（如实记，别当成「活动线修好了」）。
3. **真正的收益**：注册表从「永远空」变成「有两个 handler」，于是**按原版形状写的那批代码**才有东西可取 ——
   例如 `PracticeEvent.OnMatchFinished` 的 `LiveOpsManager.GetHandler<GameModes>().SetResultCount(...)`（账 584 的 GameModes 半），
   以及将来 `GetHandler<Missions>()` / `<CampaignHandler>` 那一族移植。
   ⚠️ 反过来说：**谁都不是靠注册表拿数据的**，所以「本批做完 = 活动线跑通」是**错的**说法。

> ⚠️ 两条边界：① `[RuntimeInitializeOnLoadMethod]` 在 **batchmode `-executeMethod`** 下**不跑**（不进 Play 模式）⇒ 自检宿主里 local handler 是靠宿主自己 `LoadFromCatalog`；② 本批只登记、**不跑** `BuildHandlers` / `Unpack`（那两条要服务器载荷，原版由 `PlayerDataManager.InitializeLiveOpsAndConfig()` → `Unpack(HandlerData)` 驱动，`out_cpp2il_dev_ilspy/PlayerDataManager.cs:4572`）。

---

## §3 顺带补的那一条：开机读一次「手牌文本」档（E12b 交回的待接点）

`MainMenuRuntime.Awake` 里 `GameStaticData.LoadSharedDefaultValues()` **之后**追加 `ReadTextInHandFormatAtBoot()`
（⛔ 没动 `LoadSharedDefaultValues` 那一行本身；它仍是 1 行 1/1）。

**判据（两份 `.c` 都是本轮现读，不是转述）**：
- 写点 `decomp_full/GraphicsTab__CardsTextInHandDropdownChange.c:35-41`：`GameStaticData(+0x140) = 值` · `(+0x144) = 1` · `LogWarning` → **直接 `return`，不重刷任何卡面**。
- 读点 `decomp_full/BasicCardUI__SetCardTextInHand.c:10-14`：**每张卡自己被建出来时**读 `+0x140` → `CardTextsController.ToggleSimplifiedView(...)`。

⇒ 原版是「卡面到处自己来拿」；我们的 `Core/CardView.HandTextFormat` 是**全局静态**、只在 `SetHandTextFormat(...)` 时重刷（`CardView.cs:5118`）⇒ **没有那条路**，所以开机必须读一次并应用一次。
实现 = `SettingsWindow.TextInHand.Value`（读档）+ `CardView.SetHandTextFormat(v)`（幂等应用；不依赖 `Load()` 的副作用）。
断言 G4：`TextInHand.RestoreForTest(2)` → 跑 `Awake` → `CardView.HandTextFormat == 2`（**注掉 Awake 那句必红**）；`finally` 里还原玩家真档。

---

## §4 typecheck

```
TMPDIR=/tmp/wf_B89E11c bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```
（本轮唯一一次 **0/0** 的干净读数 —— 前几轮一直被 `EffectResolver.cs`(E3b) / `BattleContext.cs`(E14) / `BaseEventPopup.cs` 的半成品挡住，现在那几家都存盘了。）

---

## §5 要调度台做的 / 如实记的缺口

1. **`ShellRuntime.Awake` 是更早的等价入口**（`Shell.unity` = Build Settings 索引 0、且 `DontDestroyOnLoad`）。本批没权限 ⇒ **请扩权后在那里加同一行 `LiveOpsManager.EnsureStarted();`**（幂等，两处并存无副作用）。若不加，壳里的窗口（商店/日常/收藏）在**进主菜单之前**仍然取不到 handler —— 目前它们也不靠注册表取数，所以不是回归，只是「原版形状」那批接不进来。
2. **账补丁 `账/_parts/B89_E11c.tsv`（3 行）**：
   - 新增 `LiveOpsManager.get_Instance()`（A）—— 记「改前 0 调用方 ⇒ 注册表恒空」这件事 + 本批接线。
   - 更新 `InitializeHandlers(FeatureConfig config)`（原 A）：登记职责挪到 `Instance` getter，本方法仍要 payload（本地 0 调用方）。
   - 更新 `GraphicsTab.CardsTextInHandDropdownChange(int)`：补开机读那条链，并订正原备注「手牌卡面还没有消费者」**已过期**。
   - ⚠️ **GraphicsTab 那行与 `B89_E12b.tsv` 撞键**（`check_patch_conflicts.py` 报：E12b 按字典序会赢）。**请先合 `B89_E12b`、后合 `B89_E11c`**（我这条是当天更晚的补充 + 一条订正）；另两行**无冲突**。
3. **没查清**：`GameStaticData.LoadSharedDefaultValues()` 会不会把 `cardsTextInHandFormat` **重置成默认 0**（它写的是 13 个默认值，注释说「有独立存档的那几格不会被冲掉」，手牌文本档在 `PlayerPrefs` 里、大概率不在那 13 格里）—— 本轮没逐格核；若它会写这一格，我这次「先 LoadSharedDefaultValues、再读 TextInHand」的**顺序正好是对的**（读的是存档值），但**无法证明顺序反过来也安全**。
