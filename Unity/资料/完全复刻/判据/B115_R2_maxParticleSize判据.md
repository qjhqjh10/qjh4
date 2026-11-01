# `B115_R2` · `maxParticleSize` 判据（逐颗真值 / 该不该设 / 与 09-21 A/B 的冲突裁决）

> 只读代理 `B115_R2` · 2026-11-01 · **唯一产出 = 本文件**。
> ⛔ 没改任何源码 / 资产 / 清单 / 账 / `活状态表.md` · ⛔ 没跑 Unity（一条 `-executeMethod` 都没跑）· ⛔ 没动 git。
> 数据全部**现读**（2026-11-01 20:2x），行号按现读给，**用之前请再核一次**。

---

## 判决

### 一句话
**该设，粒度 = 逐颗**；**而且它在代码与产物两层【已经落地了】**（`B114_W14` 改的代码 + 调度台 19:16–19:17 重建的 13 场 prefab）。
**但「我们小 10 倍」这个说法是错的** —— `maxParticleSize` 不是「尺寸倍数」，是**屏幕空间（viewport 比例）的上限夹取**，所以**它修不好 `T05` 那团火**。

### 一、`maxParticleSize` 到底管什么（这是本次最要紧的一条更正）

| 项 | 结论 | 出处 |
|---|---|---|
| 官方描述 | `Clamp the maximum particle size.`「Particles can become very heavy on fillrate…」 | 本机 Unity 程序集自带 XML 文档：`D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Managed/UnityEngine/UnityEngine.ParticleSystemModule.xml:4351-4355`（`P:UnityEngine.ParticleSystemRenderer.maxParticleSize`）；同文件 `:4371-4375` 是 `minParticleSize` |
| 单位 | **不是世界单位**，是**视口（screen / viewport space）比例**：`1` = 整个视口，`0.5` = 半个视口 | Unity Scripting API 同一条描述（docs.unity3d / docs.unity.com 6000.x 与 7000.0 均为此口径）＋ Unity 官方示例用 0.0–1.0 滑条驱动（「Maximum Screen Space Size」） |
| 默认值 | **0.5**（= 半视口） | 同上；且我方清单侧默认值也是 0.5（`gen_unity_arena_manifest.py:2128`） |
| 语义 = 夹取还是剔除 | **夹取（clamp）**，不是剔除 | XML 文档 `Clamp …`；`min/maxParticleSize` 成对出现 |
| 原版有没有在别处覆盖它 | **没有**。`d:/2/tools/decomp_full/`（26,282 个方法体）全树 `grep -i maxParticleSize` = **0 命中**；原版 13 场 `AnimationClip/` + `Animation/` 里也 **0 命中** ⇒ 原版就是**吃序列化值**，没有运行时改写 | 本次现搜 |

🔴 **后果（直接推翻 `B114_R3` 的那句「小 10 倍」）**：
- 原版 `5 / 10 / 12 / 20 / 25 / 50 / 100 / 500` 全部 ≥ 5 个视口 ⇒ **等于「不夹」**；
- 我们按 `0.5` 跑 = **只在「单颗粒子投影超过半个视口」时才咬**，**不会**把一颗火苗按比例缩小；
- 所以 `TinyFlames` 的 `0.5 → 5.0` **不是「放大小 10 倍」**，而是「取消一个**它根本够不着**的上限」。

### 二、它咬得到谁（13 场全量估算）

按 `camera.fov` / 相机位置 / 粒子 `pos` / `scale` / `startSize` 算「初始投影占视口高的比例」（**未含 `sizeOverLifetime` 的增长**，所以是**下限**）：

**全场只有 4 颗**的初始投影 > 0.5 视口 —— 也就只有这 4 颗会被 `0.5` 的默认夹取咬到：

| 场 | 索引 | GO | 原版值 | 初始投影（视口高占比） |
|---|---|---|---|---|
| `battlearena2` | 0 | `PoisonGas Black` | 20.0 | 0.80 |
| `battlearena2` | 44 | `DustStorm (1)` | 5.0 | 0.75 |
| `battlearenablacklegion` | 17 | `PoisonGas Black` | 20.0 | 0.93 |
| `battlearenaleviathan` | 19 | `Explosion flash` | 50.0 | 0.81 |

**`battlearena3` 那 5 颗（09-21 A/B 的嫌疑对象）全部只有 0.3%–2.6% 视口**（见「与 09-21 的冲突裁决」）。

**`T05` 那团火（`battlearena1` idx 19 `TinyFlames`）**：
相机 `(100, 2.222, -13.572)` / `fov 46.397` · 粒子 `pos (103.386, 0.0098, -6.308)` / `scale 0.48734` / `startSize 0.75`
⇒ 距离 ≈ 7.59，半高 ≈ 3.25，粒子宽 ≈ 0.366 ⇒ **占视口高 ≈ 5.6%**；
与实拍互证：在 1280×720 的 `preview_battlearena1.png` 上这团火只有 **54×34 px ≈ 4.2%×4.7%** 视口。
⇒ **`0.5` 的夹取离咬到它还差 ~9 倍。把它设成 5.0 对 T05 这团火的像素，理论上零变化。**

---

## 原版真值分布

### 0. 字段位置更正（`B114_R3` 那句「去 `ParticleSystem/*.json` 抽」找错了目录）
`m_MaxParticleSize` **不在** `d:/2/解包整理/07_场景/<场>/ParticleSystem/*.json`（那里只有 `multiplyColliderForceByParticleSize` / `multiplyDragByParticleSize` 两个同名字段），
而在 **`d:/2/解包整理/07_场景/<场>/ParticleSystemRenderer/ParticleSystemRenderer_<pid>.json`** 的 `m_MaxParticleSize`（`:65-66` 相邻的 `m_MinParticleSize`）。
配对方式：`GameObject/<名>.json` 的 `m_Component[].m_PathID` → `ParticleSystemRenderer_<该 pid>.json`。
（实测 `GameObject/TinyFlames.json` 的第 3 个 component = `1703` ⇒ `ParticleSystemRenderer_1703.json` 的 `m_MaxParticleSize: 5.0` —— ✅ `B114_W14` 那条判据亲核无误。）

### 1. 全池（14 个场景目录，992 个 `ParticleSystemRenderer`，去重后）
现读脚本：按 `ParticleSystemRenderer_*_*.json` 是重复变体，只取 `ParticleSystemRenderer_<pid>.json`。

| `m_MaxParticleSize` | 颗数 |
|---|---|
| 0.5 | 496 |
| 100.0 | 340 |
| 2.0 | 58 |
| 5.0 | 29 |
| 20.0 | 26 |
| 10.0 | 17 |
| 500.0 | 12 |
| 12.0 | 7 |
| 4.0 | 3 |
| 25.0 | 2 |
| 1.0 | 1 |
| 50.0 | 1 |
| **合计** | **992** |

- **`m_MinParticleSize` 992/992 = `0.0`** ⇒ 原版从不夹下限，我们保持 Unity 默认 0 是对的。
- 其中 **340 个 `100.0`** 是两坨不在清单里的：**13 场每场 26 个**（= 2D UI 卡牌特效 `Accumulated Energy` / `Glow Acummulated*` / `Energy Moving Left` 那一家，被生成器的 `UI_PS_ROOTS` 排除，**不是漏做**）＋ `astra`/`tauviorla` 各多 1 个（这两个在清单里，见下）＋ **`mainmenuwarpforge` 的 58 个 `2.0`**（主菜单粒子，不在 13 场范围）。

### 2. 13 场清单口径（我们真正会建的 578 颗）
`Unity/MyGame/Assets/WarpforgeArena1/arenas/*/*_manifest.json` 的 `particles[].maxParticleSize` **578/578 全有值**：

| `maxParticleSize` | 颗数 | 备注 |
|---|---|---|
| 0.5 | **479** | = Unity 默认，本来就不用设 |
| 5.0 | 29 | |
| 20.0 | 25 | |
| 10.0 | 17 | |
| 500.0 | 12 | 全在 `darkangels`（`Star twinkle` ×10 + `Smoke Traily Background` ×2） |
| 12.0 | 7 | 全在 `arena3` |
| 4.0 | 3 | |
| 25.0 | 2 | `arena3` 两颗 `Lightning Head`（`renderMode=5`，**不建**） |
| 100.0 | 2 | `astra` `Particle System` · `tauviorla` `Huge glow`（后者 `active=false`，**不建**） |
| 1.0 | 1 | `arena1` `WildFire` |
| 50.0 | 1 | `leviathan` `Explosion flash` |

**非 0.5 = 99 颗**（与 `B114_R3` 的数字一致）。

### 3. 逐场明细（非 0.5 的全部，含索引）

| 场 | 颗数 | 索引 → GO(值) |
|---|---|---|
| `battlearena1` | 17 | 5 `TinyExplosion_Far`(5) · 12 `Shockwave`(20) · 16/17/18 `Steam (2)/(1)/Steam`(5) · **19 `TinyFlames`(5)** · 20/21/22 `SmokeEffect (1)/SmokeEffect/SmokeEffect`(10) · 23 `RisingSteam`(20) · 25 `WildFire`(1) · 26 `TinyExplosion_Close`(5) · 27 `Shockwave`(20) · 29/30/31/33 `Dust Floor (2)/(3)/(1)/Dust Floor`(5) |
| `battlearena2` | 11 | 0 `PoisonGas Black`(20) · 2 `SmokeEffect`(10) · 6 `TinyFlames`(5) · 7 `SmokeEffect`(10) · 14 `ToxicPoolSplash`(4) · 16 `AdditonalSmoke`(5) · 18 `Shockwave`(20) · 21 `BigExplosion`(5) · 23 `Posion Gas`(5) · 36 `SmokeEffect`(10) · 44 `DustStorm (1)`(5) |
| `battlearena3` | 9 | 3 `Scarab swarm 2`(12) · 6 `Scarab Swarm 1`(12) · 7 `Chasm glow`(12) · 8/13 `Stuff (1)`(12) · 15/19 `Stuff`(12) · 21/30 `Lightning Head`(25) |
| `battlearenaaeldari` | 6 | 3/4 `Heat Distortion (2)/(1)`(20) · 7 `SmokeEffect`(10) · 9 `SmokeEffect`(10) · 18 `Heat Distortion (3)`(20) · 22 `Heat Distortion`(20) |
| `battlearenaastramilitarum` | 5 | 6 `Particle System`(100) · 14/15/16/17 `Steam (5)/(2)/(3)/(4)`(5) |
| `battlearenablacklegion` | 13 | 17 `PoisonGas Black`(20) · 18/24/25/32 `Heat Distortion`(20) · 19/20/23/29 `Smoke 2`(20) · 21/26/27/30 `Floor Heat Grill (+1/+3/+2/+0)`(10) |
| `battlearenadarkangels` | 12 | 2/3/5/6/7/10/12/13/16/18 `Star twinkle (+4/+6/+8/+2/+7/+5/+9//+1/+3)`(500) · 14/19 `Smoke Traily Background (1)`(500) |
| `battlearenaemperorschildren` | **0** | 全场 13 颗都是 0.5 |
| `battlearenagenestealers` | 6 | 22/30/31/32/33/34 `Dust Floor (9)/(8)/(11)/(12)/(2)/(10)`(5) |
| `battlearenaleviathan` | 11 | 3/4 `Floor Smoke 2/Floor Smoke`(20) · 5 `Green Vapours Toxic Pool`(20) · 13 `Green Smoke Toxi pool`(20) · 16 `SmokeEffect`(10) · 19 `Explosion flash`(50) · 21 `ToxicPoolSplash`(4) · 24 `Floor Smoke 3`(20) · 26 `Green Smoke`(20) · 29 `Green Vapours`(20) · 37 `ToxicPoolSplash Behind`(4) |
| `battlearenasororitas` | **0** | 全场 49 颗都是 0.5 |
| `battlearenaspacewolves` | **0** | 全场 31 颗都是 0.5 |
| `battlearenatauviorla` | 9 | 2/17 `SmokeEffect (1)`(10) · 37 `SmokeEffect (2)`(10) · 58 `Huge glow`(100) · 110 `Muzzle Flash view`(10) · 144/145/146/157 `Dust Floor (2)/(3)/(1)/Dust Floor`(5) |

---

## 真缺陷清单

### 定义与口径
「真缺陷」= **原版 ≠ 0.5** 且 **我们会把这一颗建出来**（建不出来的那颗，`maxParticleSize` 与画面无关）。
共 **99 颗原版非 0.5 → 90 颗会建 → 9 颗本来就不建**。

### A. 会建出的 90 颗（= 修复前「被默认 0.5 静默夹着」的全部）

| 场 | 颗数 | 索引 |
|---|---|---|
| `battlearena1` | **17** | 5,12,16,17,18,**19**,20,21,22,23,25,26,27,29,30,31,33 |
| `battlearena2` | **10** | 0,2,6,7,14,16,21,23,36,44 |
| `battlearena3` | **7** | 3,6,7,8,13,15,19 |
| `battlearenaaeldari` | **6** | 3,4,7,9,18,22 |
| `battlearenaastramilitarum` | **5** | 6,14,15,16,17 |
| `battlearenablacklegion` | **9** | 17,19,20,21,23,26,27,29,30 |
| `battlearenadarkangels` | **12** | 2,3,5,6,7,10,12,13,14,16,18,19 |
| `battlearenaemperorschildren` | 0 | — |
| `battlearenagenestealers` | **6** | 22,30,31,32,33,34 |
| `battlearenaleviathan` | **11** | 3,4,5,13,16,19,21,24,26,29,37 |
| `battlearenasororitas` | 0 | — |
| `battlearenaspacewolves` | 0 | — |
| `battlearenatauviorla` | **7** | 2,17,37,144,145,146,157 |
| **合计** | **90** | |

### B. 不建的 9 颗（**不是缺陷**，别去补）
判据 = `ArenaBuilder.cs:2384`（无贴图且非 Mesh 模式不建）· `:2393`（`active=false` 不建）· `:2399`（画质档不建）· `:2407`（`renderMode=5 (None)` 不建）。

| 场 | 索引 | GO | 原版值 | 不建的原因 |
|---|---|---|---|---|
| `battlearena2` | 18 | `Shockwave` | 20.0 | `active=false`（原版就关着） |
| `battlearena3` | 21 / 30 | `Lightning Head` ×2 | 25.0 | `renderMode=5 (None)` = 原版根本不画 |
| `battlearenablacklegion` | 18 / 24 / 25 / 32 | `Heat Distortion` ×4 | 20.0 | 材质没贴图（扭曲类，`noTex` 闸） |
| `battlearenatauviorla` | 58 / 110 | `Huge glow` / `Muzzle Flash view` | 100.0 / 10.0 | `active=false` |

### C. 现读状态：**残留 = 0 条**（代码 + 产物两层都已消除）

| 层 | 现读 | 判据 |
|---|---|---|
| 生成器（清单） | `gen_unity_arena_manifest.py:2128-2129` 已逐颗出 `maxParticleSize` / `minParticleSize`（缺键默认 0.5 / 0.0 = Unity 运行时默认，正确） | 13 场清单 578/578 有值（本文件 §「原版真值分布·2」） |
| 建场代码 | `ArenaBuilder.cs:2711` `if (p.maxParticleSize > 0f) rend.maxParticleSize = p.maxParticleSize;`（`:2714` 是 `minParticleSize`） | 现读 |
| 产物 prefab | **13/13 场的 `Resources/ArenaPrefabs/<场>.prefab` 里 `m_MaxParticleSize` 的分布，与「按建场闸门筛出来的清单分布」逐格吻合**（无一场例外） | 本次全量对账（见下表） |

**13 场 prefab 现读 vs 清单（建得出来的那些）** —— 逐场 **13/13 `OK`**：

| 场 | prefab 里的 `m_MaxParticleSize` 分布 |
|---|---|
| `battlearena1` | 0.5×17 · 5×10 · 10×3 · 20×3 · 1×1 |
| `battlearena2` | 0.5×34 · 5×5 · 10×3 · 20×1 · 4×1 |
| `battlearena3` | 0.5×35 · 12×7 |
| `battlearenaaeldari` | 0.5×15 · 10×2 · 20×4 |
| `battlearenaastramilitarum` | 0.5×15 · 5×4 · 100×1 |
| `battlearenablacklegion` | 0.5×26 · 10×4 · 20×5 |
| `battlearenadarkangels` | 0.5×12 · 500×12 |
| `battlearenaemperorschildren` | 0.5×13 |
| `battlearenagenestealers` | 0.5×29 · 5×6 |
| `battlearenaleviathan` | 0.5×33 · 4×2 · 10×1 · 20×7 · 50×1 |
| `battlearenasororitas` | 0.5×49 |
| `battlearenaspacewolves` | 0.5×25 |
| `battlearenatauviorla` | 0.5×76 · 5×4 · 10×3 |

**`T05` 那团火现读**：`Resources/ArenaPrefabs/battlearena1.prefab` 的 `TinyFlames`（GO 在 `:56307`）→ 它的 `ParticleSystemRenderer` 块（`--- !u!199 &7075687060443341216`，`:61053`）→ **`m_MaxParticleSize: 5`（`:61106`）**。✅ 不再是 0.5。

> prefab mtime = **2026-11-01 19:16–19:17**（13 件），`ArenaBuilder.cs` mtime = 17:01 ⇒ **产物比代码新**，重建已发生在改动之后。
> 断言已接：`B114_W14_Assert.cs:114` `RunMaxParticleSize()`（4 条用例 / 10 条断言）→ 接线在 `BattleScene.cs:46896`。

---

## 与 09-21 那次 A/B 的冲突裁决

### 那次做了什么（原文）
- `资料/战场13场_逐场对账_0920.md:1080-1085`「§3 `maxParticleSize` 故意不设（别再动）」：原版字段 12/20，照抄后 `battlearena3` 亮度比 **1.032 → 1.181**；把 5 颗（`Stuff`/`Stuff (1)`/`Chasm glow`/`Scarab swarm 1/2`）`WF_HIDE` 掉后回到 **1.037**。
- `arena_particle_audit.py:9` 同时留着另一条：「已经排除过：`maxParticleSize`（**`WF_MAXPS=20` 实测无变化**）」。
- `资料/战场13场_逐场对账_0920.md:1749` 又写明：**量这张表必须开 `WF_PSFIXSEED=1`，不开时同一份构建两次能差 6%**（而那次要判的偏差是 +15 个点，不是 6 个点，但量级已经接近）。

### 裁决：那次**不足以**否证「逐颗真值」——理由三条

1. **语义上不可能有 18% 的差**：`maxParticleSize` 是**视口比例上限**。`arena3` 那 5 颗的初始投影现读只有 **0.3%–2.6% 视口**（`Chasm glow` 1.8% · `Stuff` 2.6% · `Scarab Swarm 1` 0.8% · `Scarab swarm 2` 0.2% · `Stuff (1)` 0.5%）——`0.5` 的夹取**根本没碰到它们**，`12.0` 与 `0.5` 在语义上是**同一个结果**。⇒ 那次读数**不可能**由这个字段产生。
2. **同族的反面证据已经存在**：`arena_particle_audit.py:9` 写的 `WF_MAXPS=20` 实测**无变化**，与「屏幕空间夹取」完全自洽。
3. **那次改的是「一个全局值套全 13 场」，不是逐颗**；且**没有固定种子**。`battlearena3` 同时有别的改动（`ColorLookup`、材质、`.meta` 重导那几批）也都在同期落地。

### 反过来也**不能**用逐颗真值去解释/预测任何画面变化
- **不是**「设了就一定更亮」，也**不是**「设了就一定退步」——**绝大多数场设完就是零变化**（现读只有 4 颗真的够得着 0.5，见「判决·二」）。
- `T02 §六 第 4 条` / `0920:1743` 那句「起作用的不是**哪个值**而是**设了**这件事本身」——**本次可以撤掉**：那是当时没吃透语义时的中间结论，现由 Unity 语义 + 13 场屏幕占比算据解释清楚。
- **要坐实**只能做**双边固定种子**的 A/B：两次都开 `WF_PSFIXSEED=1`（`ArenaBuilder.cs:1369-1375`），一次基线、一次 `WF_MAXPS=0.5`（`:1123-1135`，**出图时覆盖、不用重建**）。

---

## 施工建议

### 粒度：**逐颗**（不是按族）
- 理由 1：真值本来就是**逐颗**的（同一族 `SmokeEffect` 在 arena1 是 10、在 `Steam` 是 5、`WildFire` 是 1）——按族取会造出原版没有的值。
- 理由 2：生成器**早就逐颗出值了**，逐颗是**零额外成本**。

### 两处的位置（现读）

| 处 | 位置 | 该不该改 |
|---|---|---|
| 生成器 | `Unity/工具/scripts快照/gen_unity_arena_manifest.py:2128-2129`（`'maxParticleSize': r6(_f(psr.get('m_MaxParticleSize'), 0.5))` / `'minParticleSize': …0.0`） | **⛔ 不用改** —— 已经在逐颗出值；缺键默认 0.5 / 0.0 **正好等于** Unity 运行时默认（本机 992/992 个渲染器都带这两个字段，缺键这条路实际不会走） |
| 建场代码 | `Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs:2711`（`maxParticleSize`）· `:2714`（`minParticleSize`） | **✅ 已经改好**（2026-11-01 `B114_W14`）。DTO 字段在 `:324`（`public float maxParticleSize = 0.5f; public float minParticleSize;`） |

⚠️ **`> 0f` 这个闸口（两处都用）严格说不是原版口径**：原版若真有一颗 `m_MaxParticleSize: 0`（= 全夹成 0 尺寸、等于不显示），我们会**静默回落 0.5**。
现读 **578/578 最小就是 0.5**、全池 992/992 最小也是 0.5 ⇒ **现实影响 0**；要「零自证」可以改成「键在就设」（manifest 侧已经是「键在就有值」）。
**建议**：不改（改了要动 DTO 的 `float` → `float?` 才能区分「缺键」与「写了 0」，成本 > 收益）。如要记，就记成「已知的极端值不覆盖」。

### 验证方式（不跑 Unity 就能做的，本次已做）
1. 清单 `particles[]` vs 原版 `ParticleSystemRenderer/*.json`：逐颗对（本文件 §1/§2）。
2. prefab vs 清单：按 `ArenaBuilder.cs:2384/2393/2399/2407` 四条建场闸门先筛「会建的颗」，再比 `m_MaxParticleSize` 分布 —— **13/13 场逐格吻合**。
3. 断言层：`B114_W14_Assert.cs` 的 4 条用例（`TinyFlames` 5 · `SmokeEffect` 10 · `WildFire` 1 · arena2 `TinyFlames` 5）已接 `BattleScene.cs:46896`。

---

## 顺手发现

1. 🔴 **`ArenaBuilder.cs:2815` 的注释与 `:2711` 自相矛盾**：那里写着「**只改这一个变量**：`maxParticleSize` **照旧不动**（2026-09-21 实测设了会退步）」——而这句所在的正是 `applyActiveColorSpace` 那一节。**同一文件里一句「不设」、一句「逐颗设」**，下个会话必然被误导。建议由该文件的写手**就地改成**「（2026-11-01 起已逐颗设，见 `:2687-2711`）」，**行数不变**。

2. 🔴 **`任务线/战场线/任务/T02_粒子侧.md:35`（③ 行）双过期**：仍写「⚪ `ArenaBuilder.cs:2432` 明写『**故意不设**』」——**行号错**（现读是 `:2711` 那一组，`2432` 早已是别的代码）且**状态错**（已经设了）。

3. 🔴 **`任务线/战场线/任务/T02_粒子侧.md:114`（§六 第 4 条）** 仍写「口径已被推翻一半 / 语义仍未吃透 / 改成逐场逐颗看」——**语义本次已定死**（屏幕空间夹取），这条可以整条改写。

4. 🔴 **`资料/待办判据_战场与战斗视图.md:45`**「🚫 别再动的两条：`maxParticleSize` **故意不设**（照抄 arena3 1.032→1.181）」——**该条作废**，建议连同「别再动」的措辞一起删掉（铁律 11：不许再写「不做」类措辞）。

5. `资料/战场13场_逐场对账_0920.md:1080-1085` §3 仍是「故意不设（别再动）」的原文，虽然 `:1741` 有 2026-10-03 的更正节，但正文与更正**并存**（铁律 5：两份说法打架比没有更糟）。

6. **`arena_particle_audit.py:9` 的旁证值得升格成判据**：「`WF_MAXPS=20` 实测无变化」——它其实是「屏幕空间夹取」语义的**直接实验证据**，当年被当成「这条路走不通」而埋掉了。

7. **不算缺陷、但容易被下一个人当漏做**：13 场**每场 26 个** `m_MaxParticleSize = 100.0` 的渲染器**不在清单里** —— 它们是 2D UI 卡牌特效（`Accumulated Energy` / `Glow Acummulated*` / `Energy Moving Left` / `Shrink glow` / `Energy Accumulation VFX On/Off`），被生成器的 `UI_PS_ROOTS` 排除（`gen_unity_arena_manifest.py:164-165, 1932`）。另有 `mainmenuwarpforge` 的 **58 个 `2.0`** 是主菜单粒子，同样不在 13 场口径内。

8. **19:17 那次重渲无法用来判本次改动**：与 `_tmp_view/b114_preview_bak/`（16:29，改前）逐像素比，`T05` 框 `(1020,450)-(1074,484)` 的均值**反而变暗**（`(134.2, 98.8, 44.8)` → `(107.0, 83.4, 39.3)`，全图平均 |差| 2.63、>60 的像素 34,404 个）——但那一版**同时**含 `ColorLookup` 补入（6 场）且**没有固定粒子种子**，⇒ **不能归因于 `maxParticleSize`**，也不能拿它当「改了变差」的证据。要判只能双边 `WF_PSFIXSEED=1` 重测。

9. **`T05` 那团火的真因仍未被本次覆盖**：`maxParticleSize` 语义上够不着它（见「判决·二」）。剩下的同源候选按现读排：`startColor` = `[1.0, 0.982, 0.976, 1.0]`（几乎纯白）× 贴图 `TinyFlame.png` × `colorOverLifetime` × `applyActiveColorSpace`（`B-167` Linear→Gamma 那批）—— 以及 `B114` 提到的「同源候选 `Barrels Light FX` 发光板我们偏亮 +44」。**建议把 `T05` 从「`maxParticleSize` 头号候选」改挂到这三项上**（`B114_战场五条时效核查.md:85` 那条建议现在不成立）。

---

## 就地订正

> 2026-11-02 调度台转岗：把本文件点名的**过期文档**按铁律 5 就地改掉（**只改 `.md`**；⛔ 没碰 `ArenaBuilder.cs` —— `:2815` 那条注释由该文件的写手单独处理）。
> 五份文件改前/改后均确认为 **纯 LF**（`b.count(b'\r\n') == 0`、裸 CR `== 0`），改后复测一致。

| # | 文件 : 行 | 改前 | 改后 |
|---|---|---|---|
| 1 | `Unity/资料/任务线/战场线/任务/T02_粒子侧.md:35` | 表格 ③ 行 =「原版 `0.5`×17 / `5`×10 / `10`×3 / `20`×3；⚪ `ArenaBuilder.cs:2432` 明写『**故意不设**』」/ 状态列「⛔ 别当漏了」 | ③ 行改写 = 补「13 场全池非 0.5 = **99/578**」＋ **2026-11-02 更正块**（原写 `:2432`「故意不设」→ 实为 **`:2711` 已逐颗设值**，错因 = 写在修复之前没回改）＋ 字段语义（视口比例夹取、不是尺寸倍数）＋ 判据指针；状态列改成「✅ 已做并在产物里验过（13/13 场 prefab 与清单逐格吻合）」。**表格仍 4 列（竖线 5 条）** |
| 2 | 同上 `:114-122`（§六 第 4 条） | 「🔴 `m_MaxParticleSize` 的口径已经【被推翻一半】…（darkangels 设 20 或 0.1 结果一样 ⇒ 起作用的不是『哪个值』而是『设了』）⇒ 结论改成『逐场/逐颗看』」 | 整条改写为「✅ 语义已吃透、已落地」：更正直写原句 + 错因（只在天平两头做 A/B、没读字段语义、把「设置动作」当变量）；补「该设 & 逐颗 & 已落地（`:2711`）」「只 4 颗够得着 0.5」「`T05` 修不好（5.6% 视口）」「09-21 读数不可归因」＋ 判据指针 |
| 3 | `Unity/资料/待办判据_战场与战斗视图.md:45-46` | 「**🚫 别再动的两条**：`maxParticleSize` **故意不设**（照抄 arena3 1.032→**1.181**）· `WFUnlitAmbient`…」 | 改成「**🚫 别再动的一条**：`WFUnlitAmbient`…」＋ **2026-11-02 更正块**：该条**作废**（已逐颗设值、13/13 吻合）、arena3 读数不可归因、错因（写于「清单无逐颗真值」的年代）＋ 判据指针 |
| 4 | `Unity/资料/战场13场_逐场对账_0920.md:1080-1085`（§3） | 「`maxParticleSize` 故意不设（**别再动**）… 照抄反而更差 1.032→1.181 … 字段语义还没吃透 … 这条可以安心留着」 | 小节标题下**插入 2026-11-02 更正块**（结论作废 / ① 语义 = 视口夹取 ② 该读数不可归因，5 颗仅 0.3%–2.6% 视口、且没开 `WF_PSFIXSEED` ③ 旁证 `arena_particle_audit.py:9`）＋ 现行口径 = 逐颗照抄（`:2711` 已落地）；**原四行保留在下面作留痕**，并标明「⛔ 不要当现状读」。净 **+11/−0**（未删原文） |
| 5 | `Unity/资料/完全复刻/判据/B114_战场五条时效核查.md:80-85`（§二·2） | 「`TinyFlames` … 我们现在按 0.5 跑（**小 10 倍**）」＋「**建议把它列成 `T05` 的头号候选**…做 `WF_MAXPS` 单颗 A/B」 | 在其下**追加 2026-11-02 更正块**：两条结论作废；① 不是尺寸倍数 = 视口比例夹取 ② `TinyFlames` 占 5.6% 视口、离 0.5 差 ~9 倍 ⇒ 本字段对 `T05` 理论零变化、`WF_MAXPS` 改不出变化 ③ 该设且已设（`:2711` + 13/13 重建吻合）、全 13 场只 4 颗够得着；**`T05` 改挂**：`startColor`(`[1.0,0.982,0.976,1.0]`) / `colorOverLifetime` / `applyActiveColorSpace`(Linear→Gamma) / `Barrels Light FX` 偏亮 +44。净 **+13/−0**（未删原文） |

**没改的两处（有意）**
- `ArenaBuilder.cs:2815`（与 `:2711` 自相矛盾的注释）—— 调度台已单独派给该文件的写手，**本代理未碰源码**。
- `Unity/工具/arena_particle_audit.py:9`（「`WF_MAXPS=20` 实测无变化」）—— 它**本来就对**，只是当年被当成「这条路走不通」；本文件已把它升格为判据旁证。

**改后自检**：`git diff --numstat` = `T02 粒子侧 16/6` · `待办判据 5/2` · `0920 逐场对账 11/0` · `B114 时效核查 13/0`；四份全部 **纯 LF / 裸 CR 0**；T02 表格行竖线仍 5 条（4 列）。
