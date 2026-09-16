# W 组（「两边全程空」190 个效果）分类 —— 汇总（2026-09-16）

> **这份是唯一汇总**。分块原始产出（含逐条判据与 `file:行号`）在
> `普查产出_0916/W组分类_块1~5.md`。任务来源 = `特效还原_进度与交接.md` §七 **步 3**。
> ⚠️ **没重算台账数字**（数字唯一出处仍是 `资料/特效还原台账.tsv`），**没碰亮度比 / `|ln|`**。

## 一、总数（五块加总 = 190，与台账 `分组 == "W"` 的行数一致）

| 块 | 切片 | 条数 | (a) 本来就该空 | (b) 在等某个事件 | 查不到 |
|---|---|---|---|---|---|
| 1 | `BulletImpact*` | 68 | 3 | 65 | 0 |
| 2 | `Lasrifle*` · `NecronGauss*` · `FlamethrowerSingleAttack*` | 32 | 3 | 29 | 0 |
| 3 | `Blast*` · `Ork*` · `Atk*` · `Explosion*` | 20 | 0 | 19 | 1 |
| 4 | 其余（字母序前 36） | 36 | 1 | 33 | 2 |
| 5 | 其余（字母序后 34 + 1 条边界行） | 35 | 2 | 33 | 0 |
| | **合计** | **190** | **9** | **178** | **3** |

⚠️ **边界行**：8 个前缀族共占 120 条，其余是 **70** 条（不是 71）⇒ 块 4/5 会**重叠 1 条**
（`Orbital Bombardment Enemy Warlord`）。两块结论一致（都是 (b)/ENV），已去重。
⚠️ 块 4 的 `Lasrifle` 实数是 **19** 条（多出 `Lasrifle_auto OLD`）。

## 二、🔴 三条跨块结论（比逐条清单更重要）

### 1. `vfx_wiring_j1..5.tsv` 的覆盖面**比想象的窄得多**，别把它当第一判据

| 块 | 在那份台账里的命中率 |
|---|---|
| 块 1（`BulletImpact` 68） | **0**（精确 + 去后缀模糊都是 0） |
| 块 2（32） | 4（全是 `offscreen`） |
| 块 5（35） | 12 |

原因：那份台账**只收当时「未接线」的 448 名**。**真正的证据链是四张 JSON 表**（都在 `d:/4/Unity/数据/游戏数据/`
或 `d:/warpforge/data/`，原版「动画定义 → VFX root」的映射）：

| 表 | 覆盖 | 出处 |
|---|---|---|
| `card_vfx_tree.json`（400 条） | 卡牌动画名 → VFX 根 | 块 3/5 |
| `atk_vfx_map.json`（297 条） | **攻击动画名** → VFX 根（`Atk_*` 族的主证据） | 块 2 |
| `anim_goid_map_0824.json`（591 条） | 动画名 → guid + root | 块 1（30 条硬证） |
| `animinfo_lookup.json` | VFX root → 挂点动画名 + `sp/ep/move`（**能区分「投掷物飞向目标」与「原地爆」**） | 块 2/3 |

再加「逐阵营动画定义」`d:/2/新解包资源/assets_full/bundle_*cardanims*/MonoBehaviour/*.json`（1070 个）。
⚠️ **缺口**：逐阵营动画包的 **GUID→根名** 映射没解出来（AssetBundle 容器只有 GUID→PathID）——
这就是 38 条只能判「置信中」的全部原因（不是证据不足，是缺一张索引表）。

### 2. W 组的「空」**大部分是尺子造的**，不是「效果没做」

块 1 的 65 条 (b) 在导出侧各有 12–48 个发射器，**挂点在中段动画时间轴上** ⇒ 独立 sweep 打不到它。
这与 2026-09-13 那两半「W 判定是噪声门假阳性」的结论**同向**（但块 1 不依赖那个结论，靠的是原版动画定义本身）。
⇒ **判定「这个效果是不是真的没还原」之前，先排除「挂点时机」这个尺子问题。**

### 3. (a) 的 9 条里其实有**三种形状**，建议把口径拆开

| 形状 | 条数 | 例子 | 判据 |
|---|---|---|---|
| **旧版/未投产资产**（`OLD` / `UNUSED`） | 8 | `BulletImpact_Kroot_RepeaterCannon OLD` · `Lasrifle_* OLD` ×3 · `Space Wolves Summon OLD` · `Tau_RailBombardment UNUSED` · `Tau_Pulse Carbine * OLD` ×2 | 0824 台账一律 `DEAD_SKIP`；同名 live 版另存且多为 **Z（对得上）** |
| **无引用的存量资产** | 1 | `Meteor Angled` | 全资源零动画/卡名引用；⚠️ **它根级有 PS/PSR** ⇒ 「无触发源」而不是「无粒子」 |
| ~~纯脚本占位 / 无可见粒子~~ | **0** | —— | **本批一条都没有**：190 条在台账「技术构成」列里全带真发射器 |

⇒ 原来的 (a) 定义（「纯脚本占位、原版没有可见粒子」）**在 W 组里一条都没命中**。
**建议**：把 (a) 拆成 `(a1) 旧版/未投产` 与 `(a2) 无触发源`，别混成一句「本来就该空」。

## 三、⚠️ 建议复核的 3 条（**主对话未裁定**，下个会话可改判）

- `BulletImpact_Tau_Pulse Carbine OLD` · `BulletImpact_Tau_Pulse Carbine Dual OLD` —— 块 1 自陈：
  live 版**自己也在 W 组**⇒「动画到底挂 OLD 还是 live」**从数据里判不出来**，两条判「中」。
  认为不充分就改判**「查不到」**。
- `Meteor Angled` —— 若 (a) 口径收紧成「只收无粒子」，这条要移出 (a)（见上表）。
- `Ork_Squiggoth_Charge` —— 块 3 把它从「高」**降为「中」**：`vfx_wiring_j4.tsv:43` 说它挂
  `Atk_Squiggoth_Charge`，而原版数据里该动画**指向 `Ork_Trampla_Charge`**（两个动画共用一个 prefab）。

## 四、(b) 那 178 条怎么用

它们的「等的是哪个事件」**多数不是规则书里的 61 关键词**，而是**原版的动画挂点**：
「某单位攻击 → 播 `Atk_*` 动画 → 动画上的 `animInfo` 组件在 `timeAtStartPos` 把效果挂出」（t≈0s 到 t≈3s）。
⇒ **喂给 AnimFX 路 B 的是这份「动画 → 效果」的映射表**，不是关键词表。
出处：`d:/2/Warpforge_tools/data/anim_goid_map_0824.json` + `bundle_*cardanims*` 的 1070 个动画定义。
另有 4 条属 **ENV 环境条件卡**（`Orbital Bombardment` 3 条 + `GSC Rockfall`）—— 原版该功能被取消、
本工程弃做 ⇒ **要修得先做 ENV**，不是特效层的事。
另有 6 条是 **3D 战场点击反馈**（`Tap Hit Metal*` / `Tap Monolith *` / `Tap Toxic Sludge` / `Railgun turret`）——
触发源是玩家点击 3D 碰撞体（`TapParticleController`），**我们的 2D 战斗没有这个触发源**。
