# 查证 · `§26 ⑥` 逐卡 AnimInfo 的本地可达性（第五会话 · 主对话自做）

> 触发：用户 2026-10-18 之后问「⑥ 拿到逐卡 `AnimInfo`。现在服务器已经关闭，可能永远不会有这些东西了，请你看看是否可以调整」。
> **结论 = 确认拿不到**，但**能做的部分要跟拿不到的部分分开**。本文是那三处文档订正的证据底本。

---

## 一、链路（6 跳）—— **只有第 2 跳断**

正本 = `数据/游戏数据/card_vfx_by_card.json` 自带的 `chain` / `broken_hop2` / `stats`（**生成器自己写的诊断**，不是我们的推测）。

| 跳 | 从 → 到 | 判据 | 状态 |
|---|---|---|---|
| 1 | 我们的卡 id → 原版卡 | **同一套 id**（`bundle_prebuiltdecks_assets_all/MonoBehaviour/ASH_SK_2.json` 与 `bundle_draftpacks_assets_all/MonoBehaviour/Shoulders of Giants.json` 的 `cardIds` 都是 `ASH53/ASH32` 这种串） | ✅ exact |
| **2** | **原版卡 → 它自己的 animInfo 列表** | **卡预制体**（`RawCardScript` / `AbilityLogic` / `CardTrait`）⇒ 只在远端 CCD 的 **`allcards_assets_all.bundle`** | 🔴 **断** |
| 3 | animInfo → animAdressable GUID | `animinfo_0824.json`(591) / `card_anim_map.json`(506 有名) / `animinfo_lookup.json`(418) 三表互校 = 0 冲突 | ✅ exact |
| 4 | GUID → CardAnim / VFX prefab | `数据/索引/anim_address_map.json`（`cardAnimGuidIndex` = 1099 / `cardAnimResolved` = 1067） | ✅ exact |
| 5 | CardAnim 名 → prefab 名 | 同表 `cardanim_to_asset` | ✅ exact |
| 旁路 | 卡名 / 关键词 → 动画名 | 推断 | ⚠️ inferred |
| 旁路 | 通用挂点（`CardScript.normalSummon` 等 5 条） | **本地、逐条可查** | ⚠️ `exact-not-per-card` |

🔑 **后三跳全通 ⇒ 我们手上有「动画 → 特效资源」的完整链；缺的只是「哪张卡用哪个动画」。**

---

## 二、三条补救路 —— **全试过、全断**

### 路 ① 名字推（卡名 ↔ 动画名）
- 归一化（去非字母数字 + 小写）后，把 **1099 条 CardAnim 名**的**头部**（`_` 前那段）对 **1213 个卡名**：
  **命中 48 条（4.3%）**。
- 命中样例：`CosmicSerpent` → `Cosmic Serpent` · `PathOfTheSeer` → `Path of the Seer` · `WailingDoom` → `Wailing Doom` · `Godspear Warhead` → `Godspear Warhead` · `InfinityCircuit` → `Infinity Circuit`。
- 另：`animinfo_lookup.json`（418 条键）对卡名**只中 5 条**（`Birth of a Saga` / `Tidewall Gunrig` / `Bore Through` / `Pulse Onslaught` / `Relentless Fusillade`）。
- ⚠️ **同一数字**：`§29·d` 的 `A897` · `A970` 那行也写着「真值表只采到 **~4.3%（48/1126）**」—— **两处是同一批 48 条**（同一套名字匹配法）。

### 路 ② CardAnim 资产自带 owner 字段 —— 🔴 **当场证伪**
抽 `bundle_aeldarisaimhanncardanims_assets_all/MonoBehaviour/AeldariRecall_2_ability.json` 逐字段读：

```
m_GameObject : { m_FileID: 0, m_PathID: 0 }   ← null
m_Enabled    : 1
m_Script     : { m_FileID: 1, m_PathID: 8650191106899952245 }
m_Name       : "AeldariRecall_2_ability"
animInfo     : { animAdressable{GUID,…}, startPosOption, endPosOption, pointTowardsOption,
                 vfxDelayTime, startDelay, timeAtStartPos, timeMoving, timeAtEndPos,
                 shouldMoveVFX, easeCurve{…}, orientToVelocity, applyMovementOffsets,
                 offsetMovementAxis{…}, useNonModifiedAnimTime, … }
```

⇒ **CardAnim 资产是【无主】的**：`m_GameObject` 为 null、**全资产没有任何卡 id / owner 字段**。owner 只写在卡预制体上。

### 路 ③ 本地找那个包 —— **五条证据，全否**
1. 本地 **84 个 `.bundle`**（两份游戏安装同源、文件集完全相同）里**没有** allcards 包
2. `<LocalLow>/Everguild/Warpforge/com.unity.addressables/` 里**只有 catalog（2.9 MB）**，没有已下载的包
3. `catalog_main.json` 把 `allcards_assets_all.bundle` 列在 **4 个「走 CCD 的包」**里（另 3 个：alternateartstyles / localization / remotemisccontentdata）
4. 全 `assets_full`（**24.7 万文件**）搜 `cardShortId` / `normalSummon` 等 `RawCardScript` 字段 ⇒ **0 命中**（只有共享 `CardPrefab` 那一份）
5. 把 **1100 个 CardAnim 资产 guid** 当模式全库搜 ⇒ **只有 AssetBundle 容器引用它们**（= 本地没有任何内容资产引用 CardAnim）

⇒ `card_vfx_by_card.json` 的 **`authoritativePerCardEntries` = 0**；466/1126 全是带 `conf` 的推断（`inferred-keyword` 555 / `inferred-name` 59）。

---

## 三、我们**有**的东西（比记录写的多）—— 以及 ④ 那个 `1.0` 的真身

- `card_anim_map.json` 实为 **908 条**（= **506 条有名字** + **402 条光 GUID**），**每条带完整四个计时字段**
  （`vfxDelayTime` / `timeAtStartPos` / `timeMoving` / `timeAtEndPos`）。
  记录里的「506」= **有名字那一半**。
- **实算**：506 条有名条目的「四字段之和」——

| 事件类别（按名字里的词分） | 条数 | **中位** | P25 | P75 | 最大 |
|---|---|---|---|---|---|
| **全部** | **506** | **1.00** | 0.60 | 1.20 | 6.50 |
| board | 14 | 1.50 | 1.00 | 2.00 | 2.00 |
| warlord | 59 | 0.75 | 0.50 | 1.00 | 2.00 |
| self | 44 | 0.80 | 0.50 | 1.00 | 2.00 |
| target | 47 | 0.60 | 0.40 | 1.00 | 3.00 |
| ability | 17 | 0.60 | 0.60 | 1.00 | 1.80 |
| summon | 21 | 0.40 | 0.30 | 0.75 | 1.50 |
| 其余 | 246 | 1.00 | 0.60 | 1.20 | 6.50 |

⇒ 🔴 **`Core/EventTiming.cs:108` 那个 `1.0` 恰好就是全局中位数** —— 它**不是乱拍的**，只是**没有分档**。

---

## 四、裁定（已落盘到 `项目任务.md`）

| 项 | 决定 |
|---|---|
| **①通用挂点** | **从「卡死」里摘出来** —— 它是 `exact-not-per-card`、判据在本地、**已经在跑**；⛔ 别和「逐卡绑定」共用「卡死」的判词 |
| **② `④` 的 `1.0`** | **保留数值不改**；把 `Core/EventTiming.cs` 的注释从「临时中值」升级为「**= 本地 506 条有名 AnimInfo 时长的中位数（可复查）+ 分档表**」，并写明「**不是逐卡真值**」。⛔ **不按档取值**（全局中位恰好 1.0；分档收益小、改它 = 改手感、且无逐卡判据兜底） |
| **③ 逐卡绑定** | **并入 `§29·d` 的 `A897` · `A970` 那一行**（同一个包、同一笔账）⇒ **不删账、也不当待办**；🔴 **补一句「CCD 主机已无内容可下」** ⇒ 性质从「等条件」变成「**等外部备份 / 大概率永久缺失**」 |

**条件（写死，供下个会话识别）**：判据在 **关服前玩过原版的人的** `<LocalLow>/Everguild/Warpforge/com.unity.addressables/` **缓存目录**。

---

## 五、搜过哪些（供对账）

- `数据/游戏数据/`：`card_vfx_by_card.json`(其 `chain`/`broken_hop2`/`stats`) · `card_anim_map.json` · `animinfo_lookup.json` · `card_stats.json`
- `数据/索引/anim_address_map.json`（`stats` / `guid_to_asset` / `cardanim_to_asset` / `cardanim_guid_to_name`）
- `d:/2/新解包资源/assets_full/bundle_*cardanims_assets_all/MonoBehaviour/*.json`（逐字段读一条）
- 对照：`项目任务.md` §三 第 26 条 ④/⑥ · `资料/待办判据_战场与战斗视图.md` 末节

## 六、没查清的（如实）

1. **那 402 条「光 GUID」条目**为什么当年就解析不出名字 —— **没查**（不影响结论）。
2. **`A897`/`A970` 的 48 与本次的 48 是否逐条同一批** —— 数字与方**法**都对得上，但**没逐条比对**；⛔ 别当成已证。
3. **`waitAnimation == 1` 的 tween 序列长度**那一项**没算进上表**（那是另一份数据：`tween/*.json` 74 条）⇒ 上表的「1.0」只覆盖 `AnimInfo` 四字段那半。
