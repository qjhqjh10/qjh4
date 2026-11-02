# B121-D2 · 音效 cue 覆盖（A 类 `B121-08` / D 类 `B121-31`）

> 执行写手 `B121-D2` · **2026-11-03** · 改：`WarpforgeVFX/Runtime/WFSoundBank.cs`（**只改注释，行数不变**）· `Resources/battle_sounds.json`（+6 条）· 新建 `Editor/B121_D2_Assert.cs` · `Editor/B117_G10_12_Assert.cs`（C1 计数 27→33 + 新增 C1b）· 本报告 · 账补丁 `账/_parts/B121_D2.tsv`。
> 未跑 Unity · 未动 git · 未碰账主表 / 活状态表 / 正本 / `WFSoundPlayer` / `BattleSfx` / `RemnantSfx`。

## §一 结论（先看这 5 条）

1. **「73 条」这个数要改口径**：`610` 是 **cue 对象数**，不是名字数。现读 = **610 对象 / 608 个不同 `m_Name`**（2 对同名：`Stun Effect`×2 · `Will of Gork`×2），另有 **4 条 cue 在 `bundle_battleprefabs_vfxandmisc_assets_all`**（`Buff Black Legion 3` / `Helbrute_plasma` / `Meltagun_Chaos` / `Sororitas Shrine Bombardment Audio`，我们表里已有）⇒ **原版 cue 全量 = 614 对象 / 612 个名字**。旧「73 = 610−537」把 3 个**重名文件**当成独立条目、又把 battleprefabs 那 4 条漏在分母外。
2. 本批**补进表 6 条**（全部**有持有者**）。覆盖 **542 → 548 / 612 = 89.5%**；**剩 64 条**。其中 **`Blackout` 是「补前真的不响」的缺陷修复**（我们的 `Effects/ShakeAndSoundObject.cs` 已接线、会按名字查表，见 §四·4），另 5 条是数据完备（消费者都在，但都不走 cue，见 §七）。
3. 剩 64 条的定性：**62 条全库零引用**（PPtr 与 AssetGUID **双 0 命中**，本批独立复核过）· **2 条 `clipList` 是指向 0 的空 PPtr**（`AudioCue Instance` / `unitDamageHuge` ⇒ **原版自己就哑**）。⇒ **610/610 不可达，也不该达**：给「原版没有任何消费者」的资产造表项就是硬凑（铁律 2/11 例外①）。
4. **`Trait *` 9 条 = 原版本地无任何引用**（`B121-31` 结案）。「7 条对应 trait 有特效 prefab ⇒ 嫌疑最大」这条线索**方向对、结论不成立**，逐条反证见 §四。
5. 顺带**推翻一条旧记录**：`OpenSound` 不是「prefab 里 null ⇒ 无音可播」（`日常_奖励.tsv:5` / `Shell/BaseEventPopup.cs:41`）—— 原版读的是**事件数据** `EventElements.EventAssets[key=8]`，本地 2 个活动 SO **真的带** OpenSound（见 §四·④）。

## §二 现读更正（铁律 5，就地改在 §一 + 账补丁）

| 项 | 旧口径 | 现读（本批） | 错因 |
|---|---|---|---|
| cue 总量 | 610 | **610 对象 / 608 名字**（+ battleprefabs 4） | 按文件名数，把 2 对重名当成 4 个 |
| 表内覆盖 | 537 | **542**（补前）/ **548**（补后） | 旧数漏算 battleprefabs 那 4 条 |
| 缺口 | 73 | **70**（补前）/ **64**（补后） | 同上 |

## §三 缺口清单（`名字 = 资产 PathID`；PathID 由 UnityPy 直读 `soundcollection_assets_all.bundle`）

### A 已补 6 条（逐条持有者见 §四）

| cue | PathID | clip | 持有者 |
|---|---|---|---|
| Victory | -8834901031682234264 | MatchVictory | `EndBattleDoors.victoryCue`×13 场 |
| Defeat | -1641929618139842138 | MatchDefeat | `EndBattleDoors.defeatAudioCue`×13 场 |
| OvertimeStart | 6374961739927449780 | OvertimeStart | `OvertimeUi.enteringOvertimeSound`×13 场 |
| Blackout | -8352762669946992964 | Aeldari Blackout | `ShakeAndSoundObject.sounds[0]`（Saim Hann Blackout 预制体） |
| Ork Expansion 1 jingle | 292006263889922574 | Ork expansion pop up jingle | `ParentEventData` 的 `OpenSound`(key 8) |
| Space Wolves Relase Jingle | 7732099252535305473 | Ragnar Howl | 同上（Release Space Wolves） |

### B `Trait *` 9 条 —— **全部无持有者**（判 D：数据在、本 build 无消费者）

| cue | PathID | clip | clip 已在 `Resources/`？ |
|---|---|---|---|
| Trait Blast | 4875080757702425956 | Blast | ✅（且 **62 条 trait 里没有 `blast`**） |
| Trait Front Line | -5252998228852206687 | Front Line | ✅（同上，**没有 `frontLine`**） |
| Trait Camouflage | -2624111157062738506 | Camouflage | ✅（prefab `CamouflageEffect`，`sounds: []`） |
| Trait Concussive | 2089475385675373269 | Concussive | ✅（`ConcussiveEffect`，`sounds: []`） |
| Trait Rally | -785414771625291375 | Rally | ✅（`RallyEffect`，`sounds: []`） |
| Trait Relentless | -4004975206310548827 | Relentless | ✅（`RelentlessEffect`，`sounds: []`） |
| Trait Stealth | 6136990572187540509 | Stealth | ✅（`StealthEffect`，`sounds: []`、`exitSounds`→`Stealth Break`） |
| Trait Tide | -5994051479720947568 | Tide | ✅（`TideEffect`，`sounds: []`） |
| Trait Resolution | -5799238647640510521 | Resolution | ✅（`trait_effect_bindings` 里**没有 prefab**） |

### C 其余 55 条 —— 全部 **零引用**（PPtr + AssetGUID 双 0 命中）；clip 已有 53 条

```text
ASH RC 2 jingle=-1198252403816842224 · Add card to deck=2819243950734289649 · Airstrike Gatling=-383953477227782316 · AudioCue Instance=-9033089951924250772
Backlash Effect=-8533237093348335852 · Bolter_1Shot_Chaos_superquick=178463876588059859 · Bolter_1Shot_Nurgle=8863134242351094001 · Bolter_4shot=-7884623770981375213
ButtonClick 2=-7073068878337194448 · ButtonClick 3=4661624690209085189 · ButtonClick 4=-965728437639877262 · ButtonClick 5=-7303172427455452040
CardClick 3=-1374357157672124206 · CardClick 4 cancel=-5163268181372701676 · Create Card GSC=1037466723067106589 · CreateCard_4=-8041601516753144427
Cut Quick Variation=-6659030101443292434 · EC Expansion 1 jingle=-5269354625826179320 · Electric Cut 1=-7357364145830156646 · Electric Cut 2=7239416533693395782
End Turn Button Move=-8308497759842384964 · Exorcist_MissileChoir=3192084291574617058 · Exorcist_Start=877694649598115742 · Flamer Sweep=3494761384187334493
Forge Collect=-5338995450082681464 · Frenzied Effect=-1770376853203920891 · Gauss Attack Basic=8492046555722236480 · Gauss Attack Deathmark=-6947675291385526190
Gauss Attack Hexmark=-5426600045113791056 · Gauss Attack Tesla=6484343544885521165 · Invulnerable Effect=-3361427385510183452 · Mulligan Replace=2141169132338210572
MultiplayerModeHover=-3701871049006545423 · Orks Harpoon Shot Huh=2505274520956987301 · Particle Whip=907287950878022234 · Pin Down Effect=693276519123066562
Plasma Explosion=2521770203750446341 · Plasma_Shot 3=9204824336343407529 · PracticeModeHover=-6614552912725634266 · Pulse Explosion=3081801890720153413
Remove card from deck=7579188896786826273 · Shoota Long Low=-2512714328254685478 · Shoota_5shot=-5559454674961088792 · Shuriken_Hit_1=-3975138760981622433
Tactical Insight=-2713084836379275430 · Talent Effect=-8290034957713906734 · Talent Hexscorn=6971012111725102402 · Tide of Muscle=-6570672862583674332
UI Cancel 1=-8778754749830856374 · UI Confirm 1=-8409643946902471767 · UI Confirm 2=6685340878524734565 · UI Confirm 3=-2679987779774848088
UI Purchase 1=4237275976936076308 · Void Combat=-6201434847764308613 · unitDamageHuge=-6582725487622734354
```

（`AudioCue Instance` / `unitDamageHuge` 的 `clipList = [{m_FileID:0, m_PathID:0}]` = 空引用 ⇒ 无 clip 可导；其余 53 条的 clip **全部已在** `Resources/Art/audio/sfx/`。）

## §四 补了哪些（6 条逐条：判据 → 落点）

1. **Victory**：13 份 arena 的 `EndBattleDoors`（`m_Script` 82936325632428873）`victoryCue`(`+0xB0`) → PPtr pid `-8834901031682234264`（例 `…battlearena1/MonoBehaviour/MonoBehaviour_5238.json`）。消费点 `decomp_full/EndBattleDoors__SetupDoor.c:113` = `SoundManager.Play2D(cue, MixerType.Jingles)`。参数：clip `MatchVictory` · pitch 1.0 · **vol 0.7** · ttp 0.1。
2. **Defeat**：同上 `defeatAudioCue`(`+0xB8`) → pid `-1641929618139842138` · clip `MatchDefeat` · **vol 0.7**。
3. **OvertimeStart**：13 份 arena 的 `OvertimeUi`（`m_Script` 4970624611011624522）`enteringOvertimeSound` → pid `6374961739927449780` · clip `OvertimeStart` · vol 1.0。
4. **Blackout**：`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour_9031176634359415475.json` 的 `sounds[0].sound`（`m_Script` 8184164905518033017 → 类 **`ShakeAndSoundObject`**）→ pid `-8352762669946992964` · clip `Aeldari Blackout` · **vol 0.5 · ttp 0.5**。**🔴 本批唯一「补前真的不响」的一条**：我们的 `Effects/ShakeAndSoundObject.cs` 已接线 → 读 `Resources/env_shake_sounds.json` → `PlaySoundOnTime.Play` → `AnimFXController.PlayCue` → `WFSoundBank.TryGetCue`，而 Saim Hann Blackout 那条 `sound = "Blackout"` ⇒ 补前**查不到 = 只有一句警告、没声音**，补后该环境族 **20/20**（剩 1 条 `PID:0` 是原版空 PPtr）。
5. **Ork Expansion 1 jingle** / 6. **Space Wolves Relase Jingle**：`bundle_cosmeticsso_assets_all/MonoBehaviour/{Expansion Orks1,Release Space Wolves} Parent Event.json` 的 `drawData.EventAssets` 里 **`key = 8`（`EventAssetReferenceType.OpenSound`）** = 容器键 `11c215f6…` / `baa1a292…` → 上面两条 pid。消费点 = `cpp2il/out_il2csharp/Assembly-CSharp/BaseEventPopup.cs:56-60`（`HasAsset(…, OpenSound)` ⇒ `LoadAsset<AudioCue>` ⇒ **`Play2D(MixerType.Jingles)`**）。
   ⇒ 落点 = `Resources/battle_sounds.json` 的 `group=scene`(4) / `group=event`(2) 六条（`name` = **cue 资产名**，与同表另外 27 条「字段名」不同，已在表 `note` 与 `WFSoundBank.BattleTablePath` 的 doc 里写死这一处差异）。

## §五 找不到持有者的（逐条写清「搜过什么」）

- **搜法①（PPtr / pathID）**：70 个 cue 的 pathID 做 `"m_PathID": (…)` 正则，扫 **`d:/2/新解包资源/assets_full` 全树**（247,612 对象 / 84 包）⇒ 只命中 13 份 arena 场景 + 那 1 个 `ShakeAndSoundObject` + 各包 `PreloadData`/`AssetBundle.m_PreloadTable`（预载表 ≠ 引用）。
- **搜法②（AssetGUID）**：70 个**容器键 GUID**（`bundle_soundcollection` 的 `m_Container`）`rg -lF` 扫同一棵树 ⇒ 只命中 2 个活动 SO。
- **搜法③（AssetRipper 导出工程）**：`d:/2/tools/assetripper_out/ExportedProject`（5.9 GB / 1,314 prefab + 13 场景）按导出 guid 扫 ⇒ 只命中 arena 场景 + `EnvironmentalCondition Saim Hann Blackout.prefab`。**方法自检**：拿已确认的 `Trait Swarm Tyranid`（→`Swarm Card Merge VFX.prefab`）与 `CardStartDrag`（→`Deck Editing Menu.prefab`）各跑一次，都命中 ⇒ 不是搜法失灵。
- **`Trait *` 9 条的三条反证**：① 上面三种搜法全 0；② `trait_effect_bindings.json` 里 6 个有 prefab 的（camouflage/concussive/rally/relentless/stealth/tide）**逐颗读过**：`sounds: []`；Camouflage/Stealth 的 `exitSounds[0].sound` = **`Stealth Break`**（另一条 cue，**早已在 `animfx_sounds.json`**）；③ 62 条 trait 里**根本没有 `blast` / `frontLine`**，`resolution` 没有 prefab。
- ⛔ **没有硬塞**：这 64 条一条都没进表。

## §六 验证

- `TMPDIR=/tmp/wf_d2 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（注：并发写手 `CollectionWindow.cs` 中途报过一条 `CS0103 CardOwned`，复跑已消失，非本批）。
- `python -I -X utf8 工具/check_ledger_points.py` ⇒ 见交回话（本批不 `--fix`）。
- 新增断言 `Editor/B121_D2_Assert.cs`（**未接线**，按所有权表由调度台接）：A 三表合一（union 数 vs `CueCount`，⛔ 不写死历史数）· B 六条 cue 在表 / clip 能 `Resources.Load` / 参数逐值 / `group` 形状 · C 反证（查不到 ⇒ `TryGetCue=false` + `BadCues+1` + `PickClip(null)=null`）· **D 环境音表 20 条 cue 名全部查得到**（`PID:` 占位跳过）。
- 既有断言 `B117_G10_12_Assert` C1：`==27` → **`==33`**，并加 **C1b 逐名钉住那 6 条**（重跑 `import_battle_sfx.py` 会丢它们 ⇒ 当场红）。

## §七 顺手发现

1. 🔴 **`battle_sounds.json` 是 100% 生成物**（`import_battle_sfx.py` 整篇重写）⇒ 本批 6 条**手写**会被下次重生成冲掉；C1b 是唯一防丢闸。**根治 = 在生成器里加一个手写组**（该文件不在本批白名单，⛔ 我没动）。
2. 🔴 **`BaseEventPopup` 的 `OpenSound` 那一跳没实现**（我们 `Shell/BaseEventPopup.cs:41` 还写着「prefab 里 null ⇒ 无音可播」这条错口径）。原版 `BaseEventPopup__Open` 第 ⑤ 步 = `HasAsset(liveOps, 8)` ⇒ `Play2D(Jingles)`；本地 2 个活动有音 ⇒ 这是**真缺一声音**（账补丁已记）。
3. 🟠 **`BattleDoors` 绕过 cue**：`Battle/BattleDoors.cs:470-476` 直接 `Resources.Load<AudioClip>("Art/audio/sfx/MatchVictory")`，而原版走 cue（**vol 0.7**）⇒ 我们这一声**比原版响约 1.43 倍**（cue 现在已在表里，改法 = 走 `WFSoundBank.TryGetCue("Victory")`）。
4. 🟡 `Draw Card` 的 `minPitch 1.0 / maxPitch 0.897`（**min > max**）是**资产原值**（`bundle_soundcollection…/Draw Card.json` 逐字段实读）⇒ 表里照抄是对的，⛔ 别「修正」。
5. 🟡 `AudioCue Instance` / `unitDamageHuge` 的 `clipList` 是**空 PPtr**（不是「我们没导」）⇒ 任何「补齐 64 条」的活都该把它们单独排除。
