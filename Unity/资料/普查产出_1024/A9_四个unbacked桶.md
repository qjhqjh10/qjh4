# A9 · 4 个 `unbacked` 桶的第二轮溯源（继续挖卡 SO 之外的引用链）

> 执行代理 **A9**（特效数据）。日期 **2026-10-24**。仓库根 `d:/4`。
> ⛔ 未跑 Unity / player / `-executeMethod`；⛔ 未动 git；⛔ 未改正本（`CLAUDE.md` / `项目任务.md`）；
> ⛔ 未碰 `CardPresentation/**` 的任何 `.cs`（只写了白名单里的那份 `Resources/card_vfx_by_card.json`）。
> 判据优先级：`d:/2/tools/decomp_full/`（第一）→ `d:/2/新解包资源/assets_full/` + 卡 SO 原始类型树 → 导出表。

---

## 〇、一句话结论

**4 个桶一条都没补回，维持静音 + 出声** —— 但这一轮把「为什么挖不到」挖到了底，并**改掉了其中 3 条的错假设**：

| 桶 | 判据（本轮新增） | 结论 |
|---|---|---|
| `AM30 / play`（`Manticore Board Down`） | 它落的 prefab `Artillery Manticore down 1x` **其实在播** —— 由 CardAnim `Artillery Ground Big` 引用，而后者是 **`AM53`（play）/ `AM18`（ability）的权威件** | **不播正确**（AM30 没有 `ThisCardPlayed` 能力） |
| `EC29 / attack_melee`（`HeldrakeStrike`） | 全库唯一卡引用 = **`BL53 Heldrake Strike`**（别的卡） | **不播正确**（补回 = 把 BL53 的演出错播一遍） |
| `SAU3 / deploy`（`Flayed One Summon`） | 全库唯一卡引用 = **`SAU69 Flayed Ones Pack`**（`Artifice(560)` → `summonUnit(70)`） | **不播正确**（那是召唤法术的件，不是单位登场件） |
| `a84a… / hit`（`Master Interromancer Hit`） | 它的 prefab `Psychic_Lightning_down_Intense_white` 与这张卡 `play` 的权威件 `Atk_SmiteLightning_white_Down` **是同一件** | **不播正确**（视觉没丢，已在 `play` 播；保留会播两遍） |

改动：**只把 `notPlayed[].why` 写具体**（生成器新增 `UNBACKED_EVIDENCE` 表），**事件桶、条目数、`notPlayed` 条数全部不变**
（`entryCount` 2672 · `notPlayedCount` 22 · `problems` 0 · `leftInferred` 0）。

---

## 一、这一轮到底搜了什么（4 层，逐层可核）

| # | 搜法 | 范围 | 命中 |
|---|---|---|---|
| ① | **UnityPy 重读类型树**，逐字段列 PPtr / `AssetReference` | 当前 build 的 allcards（`D:/2/关服前存档/新建文件夹/Everguild_Warpforge/54bdb7b1…/ea8bca4f…/__data`，1,351 张 `RawCardScript`）里 4 张卡的 SO | 4 张卡的**引用集与 `cards_raw.json` 的 `full_raw` 逐条相同** ⇒ `full_raw` 没漏字段 |
| ② | **GUID 32 位 + 名字**双扫全部解码资产 | `d:/2/新解包资源/assets_full/**/*.json`（每个资产一份解码记录，含 GameObject / MonoBehaviour / 场景） | 4 件 CardAnim + 4 件 prefab：只有**它们自己的记录**和 **AssetBundle 容器条目**；另加 `Atk_SmiteLightning_white_Down`（见 §二·4） |
| ③ | 同上 | `sharedassets0` / `sharedassets1` 的解码导出 | **0** |
| ④ | 字段级 | `数据/游戏数据/trait_collection.json`（73 `CardTrait` / 75 `TraitEffect`，含 `effectAnim` / `overrideByArmies`） | **0**（连 `Manticore` / `Heldrake` / `Flayed` / `Interromancer` 这几个词都没有） |
| ⑤ | 另一代真值交叉 | 2022 时代 1,195 张卡 SO 快照 → `数据/游戏数据/card_anim_order.json` | **同一结论**：只有 `BL53` / `SAU69` 两处引用 |

> ① 的做法：`UnityPy` 打开那只 `__data` → 按 `uniqueId` 取 4 个 `MonoBehaviour` → `read_typetree()` →
> 递归收所有 `m_AssetGUID` 与 `m_PathID/m_FileID`。**这一步专门用来堵「grep 字段名 = 无效否定」这个坑**。

---

## 二、逐条

### 1) `AM30` `Manticore` / `play` —— `Manticore Board Down` → `Artillery Manticore down 1x`

**卡 SO 的引用字段（UnityPy 现读，共 7 条，其中 6 条是资产）**

| 字段 | 值 | 我们的事件桶 |
|---|---|---|
| `cardImageReference` | `ebe4a35c…`（立绘） | `_assetRefs` |
| `summonAudio` | `2951e117…` | `audio.deploy` |
| `customRangedAttackParticles` | `80977c39…` = `Atk_Artillery_manticore` | `attack_ranged` |
| `activeAbility.activeAbilityLogic[0].cardAnimations[0..2]` | `a8c3f891…` / `9e089ab7…` / `568f7200…` = `DutyAnim` · `Artillery Up Manticore 2x` · `Artillery Ground Manticore 2x spread` | `ability` |
| `summonAnimDelayed` / `attackAudio` / `deathAudio` / `customDeathParticles` / `remnantBodyPrefab` / `targetedSpellAnim` / `nonTargetedSpellAnim` | **全空** | —— |
| `cardAbilities` | **空数组**（`whileInPlayAnim = 0`） | —— |

**链到谁（本轮新增的硬事实）**

- `Manticore Board Down`（CardAnim `2bb742a0a43301848864f7d803c9a0b2`）→ prefab `e3fb08046c9644b40941bb6d870bcc7c`（`Artillery Manticore down 1x`）。
- 这个 prefab **有两件 CardAnim 指向它**：`Manticore Board Down` 与 `Artillery Ground Big`（`39e1aa4ad19a695488a3541822124d0a`，Ultramarines 卡包）。
- `Artillery Ground Big` **是有主儿的**：当前卡 SO 里 **`AM53 Artillery Strike`（`play`）** 与 **`AM18 Master of Ordnance`（`ability`）** 都引用它，
  运行期表里这两条都在 `authoritative` 档（`Resources/card_vfx_by_card.json` 的 `AM53` / `AM18`）。
- `Manticore Board Down` 本身：**1,351 张卡 SO 零引用**（字段级 + 类型树级都核过），`CardTrait` 0，资产全扫只有它自己。

**结论**：**不播正确**，⛔ 别恢复。原版 AM30 的这一拍本来就没有逐卡件（没有 `ThisCardPlayed(0)` 能力；
登场走共享 `normalSummon`）。**注意**：同一件视觉并没有从游戏里消失 —— 它在 AM53 / AM18 那两张卡上播。

---

### 2) `EC29` `Heldrake` / `attack_melee` —— `HeldrakeStrike`

**卡 SO 的引用字段（现读 5 条，其中 4 条资产）**

| 字段 | 值 | 桶 |
|---|---|---|
| `cardImageReference` | `0d720e0b…` | `_assetRefs` |
| `summonAudio` | `eac200e1…` | `audio.deploy` |
| `customRangedAttackParticles` | `564af14f…` = `EC Heldrake Attack` | `attack_ranged` |
| `cardAbilities[0]`（trigger `ThisCardPlayed(0)` / `doDamage(10)`） | `bc43540f…` = `Rally EC Heldrake` | `play` |
| 近战专用字段 | **原版不存在**（`RawCardScript.cs` 里没有这一族字段） | —— |

**链到谁**

- `HeldrakeStrike`（CardAnim `39dca21155c8d3f41b8d34567db3e7ea`，落在 **Black Legion 的** cardanims 包里）→ prefab `ee767291ce7bb6044bbdf6038f25ff59`。
- **全库唯一的卡 SO 引用 = `BL53 Heldrake Strike`**（Black Legion 法术，`ThisCardPlayed(0)` → `doDamage(10)`）。
  两代快照（当前 1,351 / 2022 的 1,195）都是这一处。
- 近战协程 `CardScript._AttackMeleeAnim_d__357__MoveNext.c` 全文 **313 行、0 处 `CardAnim` / `AddCardAnim`**
  （判据由 `普查产出_1023c/Trait_pentitence与静音桶溯源.md:74-79` 给出，本轮复核引用链一致）。

**结论**：**不播正确**。补回 EC29 = 把 **BL53 的**演出错播一遍（并可能与 BL53 同时播形成双份）——
这正是「按名字猜」会犯的静默错。原版近战只有攻击音 + 位移 tween，没有逐卡 VFX。

---

### 3) `SAU3` `Flayed One` / `deploy` —— `Flayed One Summon`

**卡 SO 的引用字段（现读 6 条，其中 5 条资产）**

| 字段 | 值 | 桶 |
|---|---|---|
| `cardImageReference` | `9ede9613…`（`Necron_Sautekh_inf_Flayed One` sprite） | `_assetRefs` |
| `summonAudio` | `321cee73…` | `audio.deploy` |
| `customDeathParticles` | `ee2cef11…` = `Necrons death explosion` | `death` |
| `remnantBodyPrefab` | `5041a124…` = `RemnantBody3D Necrons` | `death`（运行时档外，见 `RT_SKIP_SOFIELDS`） |
| `cardAbilities[0]`（trigger `Reanimated(570)` / `buffSelf(40)`） | `6144c1d4…` = `FastTraitStart` | `ability` |
| **`summonAnimDelayed`** | **空 GUID**（类型树现读，不是「没读到」） | —— |

**链到谁**

- `Flayed One Summon`（CardAnim `53f3e4ca8fd14c34a8a9efcd97818c42`）→ prefab `2088de4c895a85743944489a0e983b70`。
- **全库唯一的卡 SO 引用 = `SAU69 Flayed Ones Pack`**，落在 `cardAbilities[0]`：
  `trigger = Artifice(560)` → `chosenAbility = summonUnit(70)` ⇒ 它是**「召唤 1 个 Flayed One」那张法术的登场演出**。
- SAU3 自己是单位卡（`cardType = 0`），登场走共享 `normalSummon`（运行期表 `generic.byEvent.deploy` = `Invoke Minion Card Fade Default/Legendary`）。

**结论**：**不播正确**。把「召唤法术的件」安到「被召唤单位的登场」上 = 事件归属错，且那张法术一旦打出就会**同一件播两遍**。

---

### 4) `a84a5b49f0ca9144585058db6987c2a0` `Master Interromancer` / `hit` —— `Master Interromancer Hit`

**卡 SO 的引用字段（现读 4 条，其中 3 条资产）**

| 字段 | 值 | 桶 |
|---|---|---|
| `cardImageReference` | `25daaeca…` | `_assetRefs` |
| `cardAbilities[0]`（trigger `ThisCardPlayed(0)` / `doDamage(10)`） | `39d3e084…` = `Atk_SmiteLightning_white_Down` | **`play`（权威件）** |
| `cardAbilities[1]`（trigger `OtherUnitDeath(90)` / `buffMana(60)`） | `b39a2a1f…` = `Trait_GainQuest_self`（第二条 logic 的 `cardAnimations` 为空） | `ability` |
| 三个语音 / `customRangedAttackParticles` / `customDeathParticles` | **全空** | —— |
| `UnitDamaged(60)` 能力 | **没有** | —— |

**链到谁（本轮新增的硬事实）**

- 猜的 `Master Interromancer Hit`（CardAnim `7a4f21e3f313f8b49bb9c5583331228a`）**全库零卡引用**；
- 但它的 prefab = `9d7fa450783823848b546a73cd6e0f54`（`Psychic_Lightning_down_Intense_white`），
  而 `Atk_SmiteLightning_white_Down`（`39d3e084…`）的 `animInfo.animAdressable` **也是 `9d7fa450…`** ——
  逐字比对两件 CardAnim 的 dump（`bundle_spacemarinesdarkangelscardanims_assets_all/MonoBehaviour/Atk_SmiteLightning_white_Down.json`
  与 `bundle_necronssautekhcardanims_assets_all/MonoBehaviour/Master Interromancer Hit.json`）。

**结论**：**不播正确**。这件视觉**已经在 `play` 那一拍播**；`hit` 再补 = 同一件播两遍。

---

## 三、顺带核对：`notPlayed[]` 另外的条目

> 现读 `notPlayedCount = 22` = **16 `duplicate` + 6 `unbacked`**（⚠️ 台账写的是「4 unbacked」，见 §七·1）。

- **16 条 `duplicate` 与这 4 条无关**（没有一条共用这 4 件的 prefab 或 CardAnim），且**逐条机械复核过**：
  用 `why` 里写的 guid 回查运行期表，**16/16 都能在同一张卡的另一个事件桶里找到 authoritative 条目**
  （`BL55/BL42/68909c…/SAU26/SAU50×2/30007162…/d8a14171…/SOR8×2/SOR51/TL72/SOR21/SOR10/SOR32/SOR27`）。
- **另外 2 条 `unbacked` 不属于本次任务**（`ASH86 Forewarned / play`、`AM34 Valkyrie / attack_ranged`）——
  它们的 `why` 保持通用文案，未改、未补（等派给对应代理）。

---

## 四、改动清单（全部在白名单内）

| 文件 | 改了什么 |
|---|---|
| `D:/4/Unity/工具/gen_card_vfx_by_card.py` | 新增 `UNBACKED_EVIDENCE` 表（`+54` 行，见 `:1553-1601`）：`(卡 id, CardAnim 名) → 逐条证据字符串`；`strip_unbacked_inference` 里 `_why2 = UNBACKED_EVIDENCE.get((cid, anim), _why2)`（`:1677-1679`）。**只改文字，不改任何判定分支** |
| `D:/4/Unity/数据/游戏数据/card_vfx_by_card.json` | 同一次运行产出：`stats.inferenceGone.unbacked[].why` 4 条 + `generatedAt`（`9/9` 行，`git diff --numstat`） |
| `D:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json` | 同一次运行产出：`notPlayed[].why` 4 条 + `generatedAt`（单行文件，`1/1`） |
| `D:/4/Unity/资料/普查产出_1024/A9_四个unbacked桶.md` | 本报告 |

**没有改**：任何 `.cs`（`VfxMap` / `TraitFrames` / `AnimFXCheck` / `BattleScene` 全未碰）· `RuleEngine/**` · 正本 · git。
**没有补回任何条目**（没有「按名字猜」的补回）。

### 生成器「`--check`」这条

🔴 ~~**`gen_card_vfx_by_card.py` 没有 `--check` 开关**（全文件 `grep check` 0 命中）—— 简报里的这条在**本脚本上不成立**。~~ ✅ **2026-10-24 订正（铁律 5 · A12 已实现）**：调用台裁定后已加 `--check`（只算不写、与盘上两份产物逐字节比、差异打印结构路径+字节偏移、rc=1），并补了通用 `-h/--help` 早退块；默认路径输出逐字节不变。判据 → `资料/普查产出_1024/A12_生成器check与坑落盘.md`。以下为当时的等价验收（留痕）。
等价验收改成**确定性复跑**：固定 `--generated-at`，连跑两次，两份产物 `cmp` 逐字节相同（结果见 §五）。

---

## 五、验收怎么跑

```bash
# 0) 先确认当前产物 = 生成器输出（做基线对拍；这一步本轮已过：改前连跑两次逐字节相同）
cd d:/4/Unity
cp 数据/游戏数据/card_vfx_by_card.json /tmp/full_prev.json
cp MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json /tmp/rt_prev.json

# 1) 重跑（固定时间戳，便于逐字节对拍）
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" 工具/gen_card_vfx_by_card.py --quiet \
  --generated-at "2026-10-24 09:41:12"
cp 数据/游戏数据/card_vfx_by_card.json /tmp/full_run1.json
cp MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json /tmp/rt_run1.json

# 2) 再跑一次 + 逐字节比（确定性）
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" 工具/gen_card_vfx_by_card.py --quiet \
  --generated-at "2026-10-24 09:41:12"
cmp /tmp/full_run1.json 数据/游戏数据/card_vfx_by_card.json && echo FULL_IDENTICAL
cmp /tmp/rt_run1.json   MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json && echo RT_IDENTICAL

# 3) 计数闸（必须：22 / 2672 / 0 / leftInferred 0）
python -I -X utf8 -c "import json,io; rt=json.load(io.open('MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json',encoding='utf-8')); f=json.load(io.open('数据/游戏数据/card_vfx_by_card.json',encoding='utf-8')); print(rt['notPlayedCount'], rt['entryCount'], len(f['stats']['problems']), f['stats']['inferenceGone']['leftInferred']); print(f['stats']['notPlayed']==rt['notPlayed'])"

# 4) 行尾别翻（两份都是 LF；运行时那份无换行）
python -I -X utf8 -c "import io;[print(p, io.open(p,'rb').read().count(b'\r\n')) for p in ['数据/游戏数据/card_vfx_by_card.json','MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json']]"
```

**本轮实际结果**：`FULL_IDENTICAL` / `RT_IDENTICAL` · `22 2672 0 0` · `stats.notPlayed == rt.notPlayed` → `True` ·
两份 `CRLF = 0` · `git diff --numstat` = `54/0`（生成器）· `9/9`（全量件）· `1/1`（运行时件）。

---

## 六、没查清 / 没做

1. **没有原版实况**（原版已关服、后端不可达）能证明「这 4 拍在真实对局里到底长什么样」。
   本报告的结论全部来自**当前 build 的卡 SO 原始类型树 + 全量资产 GUID/名字扫描 + 反编译**，不是实拍。
   ⇒ 若有朝一日能进原版看，优先看 **AM30 打出时**（预期无逐卡件、只有共享淡入）与 **EC29 近战**（预期只有位移+音效）。
2. ~~**「`--check`」不存在**（见 §四末）。没有去给生成器**新增** `--check` 开关 —— 那会改脚本的对外接口，属于「发明口径」，留给调度台裁。~~ ✅ **2026-10-24 订正：已由 A12 实现**（见 §四末的更正；`--check` 现读 rc=0/rc=1 都实跑验证过）。
3. **`ASH86` / `AM34` 两条 `unbacked` 未处理**（不在本任务 4 条内，见 §七·1）。
4. **`VfxMap` / `TraitFrames` 侧一行没动** —— 本任务不需要（没有任何补回），也超出白名单。
5. **`AM30` 的 `activeAbility` 三件是否在「主动技能」那一拍按序全播** —— 属战斗线
   （`ability` 桶 + 多播），本轮只核到「它们在 `ability` 权威桶里」，没有验证播放序。

---

## 七、顺手发现（交给调度台分流）

1. 🔴 **台账口径要改：`unbacked` 是 6 条，不是 4 条。**
   现读 `Resources/card_vfx_by_card.json` 的 `notPlayed[]`：16 `duplicate` + **6 `unbacked`**。
   多出来的两条是 **`ASH86 Forewarned / play`（`Forewarned Board`）** 与 **`AM34 Valkyrie / attack_ranged`（`Valkyrie_MultiLaser_Hover_Target`）**。
   本次简报写的「另外 18 条（16 duplicate + 2 其余）」正好漏了这两个 —— 它们同样是「按名字猜、无权威源」，
   要不要走同一条溯源，请调度台定（本轮**没动**它们的 `why`，避免与别的代理撞车）。
2. 🔴 **就地订正（铁律 5）：`普查产出_1023c/Trait_pentitence与静音桶溯源.md:70` 那句「两者都不被任何卡引用」是错的。**
   原文说 `Artillery Manticore down 1x` 的两件 CardAnim（`Manticore Board Down` + `Artillery Ground Big`）**都不被任何卡引用**；
   实际 **`Artillery Ground Big` 被 `AM53`（play）与 `AM18`（ability）引用，且两条都是 `authoritative`** ——
   也就是说**这件 prefab 一直在播**，只是不在这 4 张卡上。
   ⇒ 结论（AM30 不播）**不变**，但理由要换成本报告 §二·1 的说法。
   ⚠️ 那份文件不在我的白名单（`普查产出_1023c/`），**没有就地改**，请调度台落盘。
3. **`notPlayed[]` 的 16 条 `duplicate` 全部机械复核通过**（见 §三）—— 与 `Trait权威化_执行报告` 的说法一致，可继续引用。
4. **两个名字撞车实例值得记一笔**：`HeldrakeStrike` 在 `assets_full` 里**有两个同名资产**
   （`GameObject/HeldrakeStrike.json` = prefab `ee767291…`；`bundle_chaosspacemarinesblacklegioncardanims/MonoBehaviour/HeldrakeStrike.json` = CardAnim `39dca211…`）。
   生成器 `cardanim_guid_by_name()` 按 `className == "CardAnim"` 过滤，**这一点是对的**；
   但以后若有人按「名字 → GUID」手工补表，**必须带 className 过滤**，否则会把 GameObject 的 guid 当 CardAnim 用。
5. **`deploy` 桶现在全池只有 1 张卡**（运行期表 `事件分布` 里 `deploy: 1`）。这不是缺陷
   （其余单位走 `generic.byEvent.deploy` 共享档），但若以后有人拿「`deploy` 桶条数」当覆盖率指标，会误判。
