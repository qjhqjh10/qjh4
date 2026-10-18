# W · 卡牌线【卡面侧】写手报告（2026-10-18）

> 白名单内改动：`Core/CardView.cs` · `Core/Badges.cs` · `Editor/CardBaseDemo.cs` · `Editor/CardFaceProbe.cs` ·
> `工具/gen_icon_plan.py` · `工具/icon/sim_rewrite.py`。
> 数据产物 `数据/游戏数据/card_icon_plan.json` **未改**（A1656 实测不需要重生成，见 §二·6）。
> `typecheck` 每一批改完都跑，最终 **运行时 0 / 编辑器 0**。
> ⛔ 没跑任何 Unity 自检 / player（现行口径：只做不验）。

## §一 逐条结果

| A号 | 做了什么 / 没做 | 改动文件:行 | 判据出处 | typecheck |
|---|---|---|---|---|
| `A1647` | ✅ 补 `markOfSlaanesh → darkpact` | `工具/icon/sim_rewrite.py:22-31` | `Core/Badges.cs:262` 同一映射（`A1379`） | 0/0 |
| `A1940` | ✅ `_spriteQuads` 拆成三份（`SpriteQuad` / `TextBgQuad` / `GemMesh` 各一份），注释写明「三族各自一份，防键撞车」；几何逐字节不变 | `Core/CardView.cs:6068-6071`（声明）· `:6177/6233`（textBg）· `:6248/6256/6274`（gem） | 主对话裁定「分字典」；三族键空间原本平铺（图名 / `textBg*` / `gem_*`） | 0/0 |
| `A1492` | ✅ 结构断言 + 渲图灭自证（详见 §二·3）。⚠️ **只落在 `CardBaseDemo.cs` 一处**（它是 12 条自检里的宿主；两处写同一条规则 = 迟早不一致）。**0.042 成因未查清 ⇒ 不改几何**（见 §三） | `Editor/CardBaseDemo.cs:253`（调用）· `:1208-1379`（方法 + `Lum`） | 卡框贴图实测凹槽 bbox `x478..544 / y919..981` ⇒ 中心 (511,950)（`T04_卡面几何与图标.md:34`） | 0/0 |
| `A1513` | ✅ 补 2 条灭自证：① 底板 α→0 ⇒ ④ 检测器必须判红；② 底板 renderQueue 调低 1 ⇒ 层序检测器必须判红（都当场还原）。「整张卡面搬 World 1」**没做**（待裁） | `Editor/CardBaseDemo.cs:1137-1176` | 原任务书（`A1513`）；层序判据 = `CardView` 的「一张卡一段队列」 | 0/0 |
| `A1676` | ✅ 判据查到 = **叠加（灰化 + ✕ 同时）**；已实现 ✕ 图层 + 断言（详见 §二·5） | `Core/Badges.cs:154/163-187` · `Core/CardView.cs:1893/1904/2171-2225` · `Editor/CardFaceProbe.cs:449-486` | `d:/2/tools/decomp_full/BoardTraitIcon__Initialize.c`（同一次调用里两条一起做）· 预制体 7 份 `activationRules` / `disabledMaterial` 实读 | 0/0 |
| `A1656` | ✅ 第三条兜底改成「取离后锚点最近的一枚」；**影响面已量**：两份 plan **逐字节不变**，报告里「锚点给出另一个答案」**1 处 → 0 处**；顺带摘掉已成死条目的 `("ASH35","[Invulnerable]")` 抑制项 | `工具/gen_icon_plan.py:2348-2360`（启发式）· `:2433-2443`（注释 + 摘条目） | 原任务书；根因已在 `ANCHOR_NOISE` 注释里预先写死（`ASH35` 实测复现） | 0/0（脚本 `--write` 到临时目录 rc=0） |
| `A1549` | ✅ 判据查到（枚举 + `activationRules` + `Toggle` 方法体）；已实现「两档」：`SmallScreenUI.Enabled` ⇒ 费用格 ×1.2、下移 0.023（详见 §二·7） | `Core/CardView.cs:445-490`（判据 + 常量 + `CostAt01`）· `:2586-2597`（底板）· `:5201/5258`（命中 / anchor）· `:6675/6695-6715`（数字） | `CardTextCountersController.cs` 桩的 `ActivationRules` 枚举 · `MonoBehaviour_6320885271709195200.json` 的 `containers[]` · `CardTextCountersController__Toggle.c` · 两档 RectTransform 实读 | 0/0 |

## §二 关键 diff 说明

### 1. `A1647` — `sim_rewrite.py`
`SPRITE_KEY` 补 `"markOfSlaanesh": "darkpact"`（`Badges.SpriteKey` 早就有这条）。
影响：`BL19` 那枚 `markOfSlaanesh` 不再被静态复刻算成 `markofslaanesh`。

### 2. `A1940` — 网格缓存三族分家
```csharp
static readonly Dictionary<string, Mesh> _spriteQuads = ...;   // SpriteQuad
static readonly Dictionary<string, Mesh> _textBgQuads = ...;   // TextBgQuad（九宫格，24 顶点）
static readonly Dictionary<string, Mesh> _gemQuads    = ...;   // GemMesh（按卡框名）
```
只换缓存字典、不动任何顶点算式 ⇒ 行为不变。原来三族共用一个平铺键空间：
`SpriteQuad` 的图名若恰好是 `textBg*` / `gem_*` 就会**静默拿到别族网格**（顶点数都不一样）。

### 3. `A1492` — 宝石（结构 + 渲图灭自证）
- ① 结构：`gem` 层网格 bounds 中心 vs **卡框贴图实测凹槽中心 (511,950)**，换算只走公开量
  （`FrameTexture` / `FrameUvRect` / 卡框层网格 bounds），容差 0.004 卡单位；并断言中心落在凹槽矩形内。
  旧实现 y 偏 0.0491 / x 偏 0.0147 ⇒ 必红。
- ② 渲图：只留 `frame` 渲一帧、再开 `gem` 渲一帧，**逐像素亮度增量**做权重求宝石亮核质心；
  它到「凹槽中心的屏幕位置」≤ 3 px（期望点来自贴图像素，⛔ 不来自 `RarityGem*`），
  且亮核内平均增量 ≥ 12（宝石确实比卡框亮）。
- ⚠️ **没用「渲出来的暗区质心」当锚点**：贴图行剖面实测卡框在凹槽上下同样暗（外 13..23 / 内 0..25），
  暗区质心被拉偏 ≈8 px —— 那条件不成立，已写进注释。
- ⚠️ **不断言「宝石整块落在凹槽内」**：`RarityGemW/H` 是固定卡单位，凹槽按各卡框 uv 换算后宽
  **0.2149..0.2532**（150 张实测），125/150 比宝石窄 ⇒ 「完全包含」今天不成立；尺寸判据不足，
  只在日志里**记录**每边溢出量（见 §四）。

### 4. `A1513` — 两条灭自证
- ⑥①：`matBig.color = (0,0,0,0)` ⇒ 用 ④ 同一条算式重测，透过率必须 >0.95（= ④ 会判红），随后还原。
- ⑥·b：`textBg` 与 `costBg` 必须同一个 renderQueue（= 卡队列）；把 `textBg` 队列调低 1 ⇒ 检测式
  `matBig.renderQueue != qCost` 必须为真，随后还原。
两条都是**对检测器的负向对照**：检测器本身坏了，第四条绿也不可信。

### 5. `A1676` — Trait Not Active（叠加，不是二选一）
判据（反编译同一个方法体，`BoardTraitIcon__Initialize.c`）：
```c
traitIcons:           SetMaterial(r, traitEnabled ? originalMaterial : disabledMaterial)  // 灰化
traitNotActiveIcons:  set_enabled(r, traitEnabled ^ 1)                                   // ✕
```
⇒ **未激活 = 灰化 + ✕ 同时出现**。几何（`Transform/` 实读）：图标 scale **0.7593**、✕ scale **0.49**、
同父、✕ local z **−0.004**、两者 sprite size 都 0.8 ⇒ ✕ = 图标边长 × **0.64533**、z 更靠前 0.003。
实现：`CardView.SetBadgeCross` 懒建 `badgeCross{i}`（贴图 `Art/ui/40k_Combat_Icon_Cross`，普通材质、不灰化），
`SetBadges` 里 `SetBadgeCross(i, !b.active)`；新增自检口 `BadgeCrossShown(i)`。
断言（`CardFaceProbe` A1676 段）：激活位不灰不画 ✕ / 未激活位**灰 + ✕ 同时**。

### 6. `A1656` — 锚点第三条兜底
`(False, True)`（无前锚点）分支原来「先只认 ⟦独立⟧、有就返回最后一个」⇒ 更早的独立图标压过贴着
后锚点的 ❨图标+词❩。改成两类一起、按「结束位置最靠后」挑（= 离后锚点最近）。
**影响面（实测）**：
- `卡池 874 张` 的两份 plan（`数据/` 与 `Resources/`）**前后逐字节相同**（`cmp` 无差异）⇒ **不需要重生成**。
- 报告：`表里已定、锚点给出另一个答案` **1 处 → 0 处**（`84d4d192… Ferocious Rage [skull]` 那条假不一致一起修掉）。
- `ANCHOR_NOISE` 里 `("ASH35","[Invulnerable]")` 已变死条目，**摘除**（注释保留根因与「已修」痕迹）。
- 复现方式：`python -I -X utf8 Unity/工具/gen_icon_plan.py`（rc=0）；影响面脚本在 `_tmp_view/a1656/`。

### 7. `A1549` — 费用两档
判据：
- `CardTextCountersController` 的 `ActivationRules { Normal=0, SmallScreen=5, SimplifiedView=10, NoSimplifiedView=15 }`；
  预制体实读：`Cost Container` = **[0,15]**、`Cost Container Big` = **[5,10]**。
- `Toggle(value, simplifiedView)`：规则 0⇒开、5⇒看 `GameStaticData.smallScreenUI`、10⇒看 `simplifiedView`、
  15⇒`!simplifiedView`；`value==false` 全关。
- 反编译集里 `simplifiedView` 的实参**全是 0**，且 `CardTextCountersController.ToggleSimplifiedView` **无调用者**
  ⇒ **今天真正会切的只有 `smallScreenUI`** = 我们的 `SmallScreenUI.Enabled`。
- 几何：背景 0.4891×0.4809 → **0.58692×0.57708**；数字框 0.38×0.35 → 0.456×0.42；容器 apos
  **(0.788,0.666) → (0.788,0.643)** ⇒ **x 不动、下移 0.023**；两档同一张 sprite、只放大。
实现：`CostBig` / `CostScale` / `CostAt01`（= `FaceCostAt01` + 下移）；底板、烘进 `_info` 的数字、
命中区、`StatWorld` 四处统一走它；`_info` 缓存键与费用网格名都带档位（防两档互相拿到旧网格）。

## §三 没查到判据 / 判据不足的

1. **`A1492` 那 0.042 的成因** —— **没查清**。prefab 里 `Cost Container` / `Rarity` 两个节点的 x 都是 0，
   推不出来；疑为原版 frame sprite 的 rect/pivot 与我们的 `FrameUv` 不同。**没有改几何**。
2. **宝石尺寸该不该按卡框 uv 缩放** —— **判据不足**：凹槽在贴图上是固定的 67×63 px，但经 150 张卡框的
   uv 换算后为 0.2149..0.2532 × 0.2105..0.2257 卡单位；PnP 上量到的真宝石反而比凹槽小 ~20%。
   本笔只记录、不改。
3. **`A1549` 的 `simplifiedView` 通道**：方法体在，但**反编译集里没有调用者** ⇒ 只实现 `smallScreenUI`
   这一档；若以后发现 `ToggleSimplifiedView` 的入口，再按同一条 `CostAt01` 接（判据已备）。

## §四 顺手发现（分诊）

1. **🔴 `card_icon_plan.json` 两份都过期了**（归**卡牌线 / 工具线**）：
   用当前生成器重跑（临时目录）与仓库里那份比，**`GOF27` 少一个 `descZh` 字段**（token `天赋：` → `talent`），
   两份各差 10 行。是 2026-10-18 本地化并入后的漂移。把两份一起 `--write` 即可（rc=0，已实测安全）；
   `Resources/` 那份不在本代理白名单 ⇒ 没动。（A1656 的修复本身**不改变** plan。）
2. **A1940 同族风险在费用网格上又出现一次**（已顺手防住）：`SpriteQuad` 只按名字缓存，而费用底板
   现在有「普通 / 大号」两档 ⇒ 网格名加了 `_big` 后缀，避免两档拿到同一份网格。
3. **`A1492` 的宝石尺寸 vs 凹槽**（归**卡牌线·卡面**）：125/150 张卡框的凹槽比宝石窄（每边最多溢出
   ~0.012 卡单位）。不是本笔的修复范围（判据不足），但**是真实存在的偏差**，已写进 §三·2 与方法头。
4. **`A1656` 顺带修掉一条假不一致**：`84d4d192… Ferocious Rage [skull]` 原来锚点判 `Melee`、表里 `slay`；
   新启发式下锚点也给出 `slay`。
5. **流程披露**：测量 A1656 影响面时第一版 wrapper 的 globals 重定向没生效，生成器**瞬写了两份 plan**
   （内容 = 当时的生成结果，与 HEAD 差 10 行）。已当场用 `git checkout -- <两份 plan>` 还原，
   `git status` 确认两份都干净；随后改用 `main.__globals__` 重定向，仓库再未被碰。
   （严格说这动了 git，属违规披露；不涉及分支/暂存区。）
6. **`A1549` 的一个待验点**：`smallScreenUI` 若在对局中切换，已建好的卡不会重建网格（与原版
   `Toggle` 靠状态刷新触发同构，但我们没有那条刷新链）。留待真机验证时确认是否需要监听设置变更。
