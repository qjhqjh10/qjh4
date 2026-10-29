# B97_RBCARD 报告（把 `RankedArmyBonusCard` 接进主菜单 · 账 `个人资料_成就_排行:198`）

> **代理**：`B97_RBCARD`（执行）· **日期**：2026-10-29 · **判据**：`报告/B97_PROFILE.md` §7（照抄，未重查）
> **typecheck**：`TMPDIR=/tmp/wf_rbcard bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完后又跑了一遍 `wf_rbcard2`，仍 0/0）
> **账补丁**：`账/_parts/B97_RBCARD.tsv`（11 列 · 首行表头 · 1 行 · `方法` = `OnInitialize(LiveOpsEvent evt)`）

## 1 一句话结论

§7 的补丁**已落地**，但**没有照抄「在 `_placeAllCards` 里插 28 行」那一段** —— 该文件被 **79 处账落点**按 `文件:行号` 引用，
插行会让 `2680` / `2676` / `2686` / `2469` 等整批漂移。改成 **调用点一行追加（零行数变化）+ 方法体放文件末尾新增的第三个 `partial` 段**，
语义与 §7 逐句等价（`first-fit` 下一格 · `1×1` 只占 `row0` · 没数据不建卡）。

## 2 改了什么（只有 2 处，文件 = `Shell/MainMenuRuntime.cs`）

| # | 位置 | 改动 | 行数 |
|---|---|---|---|
| 1 | `:2630`（第五张 `Put` = `Tutorial` 那一行的**行尾**） | 追加 `PlaceRankedArmyBonusCard(content, used);` | **±0**（就地改写已有行） |
| 2 | `:5136-5201`（**文件末尾第三个 `partial` 段**，`namespace CardPresentation { public partial class MainMenuRuntime { … } }`） | 字段 `_rankedArmyBonusCard`（`:5160`）+ 方法 `PlaceRankedArmyBonusCard(Transform content, bool[,] used)`（`:5171-5199`） | **+66**（全部在旧末行 `:5135` 之后） |

**行数 +66，位于第 5135 行之后（= 文件最末尾）** ⇒ 旧文件的**任何**一行行号都没变（`git diff --numstat` = `420/12`，
其中 `353/11` 是本会话早些的 `B97_MENU`；我这部分是 **+67 新增 / 1 行改写**，净 +66）。

### 2.1 为什么不是「照 §7 插 28 行」

- 本文件现读有 **79 处**账落点（`python` 现算，最大 `MainMenuRuntime.cs:5028`）⇒ 在 `:2630` 之后插 28 行 = 后面全部 +28。
- `B97_MENU` 已经为同一件事立了先例（`:48` 的 `partial` + 文件末尾第二个 `namespace` 段，注释里写死「⛔ 不插进类体」）——
  本段照同一种做法，只是**第三个** partial 段。
- 调用点必须留在 `_placeAllCards` 委托里（`used` / `_modeOffsetPx` / `content` 只有那儿拿得到，且首次摆放与滚动重建共用同一份代码）。

### 2.2 方法体（与 §7.2 的逐句对应）

| §7.2 原文 | 落地 | 说明 |
|---|---|---|
| `RankedArmyBonusData.FirstValid(Load())`；`null` ⇒ 不建 | `:5180-5181` | 同 |
| `for (i < MaxCols && colB < 0)` 找 `!used[i,0]` | `:5183-5185` | 用 `used.GetLength(0)` 取列上限 **= 不另抄一份 `MaxCols`**（CLAUDE.md §三「两处写同一条规则 = 迟早不一致」） |
| 排不下 ⇒ `LogWarning` | `:5186-5190` | 同（红线：不许静默） |
| `used[colB, 0] = true`（不占整列） | `:5191` | 同 |
| `Create(content)` + `ApplyPxRect(...)` | `:5193-5197` | 同（`CardCol0 + col*CardColStep − _modeOffsetPx`, `CardRow0Top`, `CardW × CardH`） |
| `Initialize(bonusEvent)` | `:5198` | 同 |
| （§7.3[3] 建议在复位段补 `_rankedArmyBonusCard = null;`） | `:5174-5178` | **改成方法内先收**：全量重建那条路已由 `DestroyAllChildren(_modeContent)` 收走（引用变「已销毁的 Unity 对象」⇒ `!= null` 为假，不会重复销毁）；增量那条路不销毁子件 ⇒ `DestroySafe` 兜住。**比「只把引用置空」更安全**（置空会让那颗节点留在场上） |

## 3 验了什么

| 闸 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_rbcard bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（改前一次、改后再一次） |
| `python -I -X utf8 工具/check_ledger_points.py` | **本域 `A=223 通过=223 漂移=0 告警=10 失败=0`**；输出里 **`MainMenuRuntime` 一条都没有**（= 我这个文件零漂移零失败） |
| `python -I -X utf8 工具/check_patch_conflicts.py` | `✅ 没有跨补丁重复键（2 份补丁）` |
| 行尾机检（`io.open(p,'rb')`） | `crlf 0 / lf 5201 / 裸 CR 0`；改前 `5135` 行 ⇒ **+66** |
| 别人在飞的活有没有被我碰坏 | `BindMainMenuOfferContainer` / `RefreshLiveOpContainers` / `PlaceModeCardViaContainer` 共 23 处仍在；`git diff --numstat` 的 `353/11` 原样保留 |
| 账补丁往返校验 | 读（裸 `split("\t")`）→ 写 → 再读：**11 列全等**，未改的 6 列逐字等于主表原值 |

⚠️ **没跑的**：Unity 批处理 / 真界面（本代理禁跑）⇒ 卡片在屏幕上的实际位置与外观**没有肉眼核过**。

## 4 账

- 补丁 `账/_parts/B97_RBCARD.tsv` **只写了一行**（简报指定）：`个人资料/成就/排行 · RankedArmyBonusContainer · OnInitialize(LiveOpsEvent evt)`。
- **状态 `B → A`**：`B` 的原定义 = 已实现未核（§4.1）；`B97_PROFILE` 当时留 `B` 的唯一阻塞就是「未接线」（同批 `221` 在没跑 Unity 的情况下已判 `A`，标准一致）。
  现在调用点落地 ⇒ 差的那一步已清。差异列**明写「尚未在真界面跑到」**，没有拿它冒充已验收。
- 落点列 = `Shell/RankedArmyBonusCard.cs:247（Initialize）· Shell/MainMenuRuntime.cs:5171（PlaceRankedArmyBonusCard）（等价实现 = 调用点：…）`。
  ⚠️ 按 `方案 §4.1` 的格式要求「第一个括号只放裸声明名」，第一个落点（也是机检会看的那个）仍是**方法级**的 `Initialize`。

## 5 顺手发现 / 交回调度台

1. 🔴 **账 `:197`（`Clean()`）的「未接线」阻塞也被清掉了** —— `Initialize` 内部第一拍就是 `Clean()`（`RankedArmyBonusCard.cs:247` 起）。
   简报只授权我写 `:198`，所以 **`:197` 一个字没改**，请调度台裁是否一并 `B→A`（同一颗调用点）。
2. ⚠️ **已知边界（写进了代码段文件头）**：本地 liveop 事件表恒空 ⇒ `ModeCardsFromLiveOpEvents` 恒 `null` ⇒ 只走手挑 5 张 + 这张卡的兜底。
   将来事件数据到账、且**正好走增量复用那条路**（`LiveOpContainerTable` 非空、`forceClean == false`）时，这张卡会留在场上
   （原版那一刻它本该是一张事件卡）。今天的代码里这条路不可达，但它是**口径缺口**，记在这里而不是埋着。
3. `Resources/Art/ranked_army_bonus.json` **在 `.gitignore` 里**（`B97_PROFILE` §6.6 已记）⇒ 换机器 / 清工作树之后必须重跑
   `工具/gen_ranked_division_particles.py`（那份是粒子树）＋ 手工恢复奖励表，否则这张卡**永远不出现**（只出一条出声日志）。建议调度台把「资源件复现命令」补进交接。

## 6 改动清单

- `Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs`（`:2630` 行尾 + `:5136-5201` 新 partial 段，+66 行）
- `资料/完全复刻/账/_parts/B97_RBCARD.tsv`（新建，1 行 + 表头；由「读主表该行 → 改 5 列 → 写补丁 → 往返校验」的一次性脚本产出，
  ⚠️ 脚本按红线**不入库**、跑完即删 ⇒ 要重建就照本报告 §2 + §3 的往返校验重做）
- `资料/完全复刻/报告/B97_RBCARD.md`（本文件）
- ⛔ 没碰：`RankedArmyBonusCard.cs` / `ShellScene.cs` / 账主表 / 正本 / `RuleCore*` / `EffectResolver*` / `SimpleAI.cs` / `BattleDriver.cs` / `UnitState.cs` / `Aura.cs` / `GachaEventWindow.cs` / `ShopData.cs` / `ShopTabPage.cs` / `Editor/RuleEngineTest.cs` / `Pinned*` / `工具/ruleprobe/` / git / Unity
