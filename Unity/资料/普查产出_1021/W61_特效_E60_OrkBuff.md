# W61 · 特效 E60 · OrkBuff 骷髅精灵（2026-10-21 · 执行代理 A）

> 范围：`OrkBuff_Snakebitez_2_Intense` / `OrkBuffGreenWaaagh` / `OrkBuff_Recall` 的
> 骷髅粒子发射器 `UVModule.sprites`。
> ⛔ 本文件只记事实与判据；逐条待办以 `资料/任务线/特效线/任务总表.md` §一 为准（铁律 6）。
> 本轮**没跑 Unity**（重导由主对话在同步点跑）。

---

## 1. 结论（一句话）

**判据闭合；而且这三件在盘上已经是对的。**
W60 §9·5 的「G1 未生效 / 原版实例上这一格本身就是 null」**至少已经过期**（20:11 之后不再是事实），
其中「这一格本身就是 null」这半句还被 20:11 的日志**直接推翻**（`GetSprite→ Skulltop(Clone)`，非 null；
真正的失败点是 `SpriteRenderDataKey(s) == null` 那句静默 return，当天已修）。同一次修复运行
（20:10–20:11 `EffectExporter.RunListed`）**已经把 8 个槽全部解析出来**（真 guid + 逐像素正确的 PNG）。
本轮做的事 = ① 把槽位映射**从原包里证明死**（16 条，不猜）；② 把「`GetSprite(i) == null`」这条
**静默丢图**的路补成「有证据的兜底 + 出声」；③ 顺手把同族另外两件的同一个病纳入本批重导。

---

## 2. 证据

### 2·1 槽位顺序怎么证明的（原包直读，不是猜）

原包：`D:/2/10.13游戏/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle`
（同名解包产物：`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`；
全部对象在同一个 CAB `CAB-d47690319398b604c3bb5a35a8ed2499`，PPtr `m_FileID` 全 **0**）。

读法：`GameObject → Transform 逐级` → 每个 `ParticleSystem` 的 `UVModule.sprites[i].sprite`（PPtr）
→ 解到同 CAB 的 `Sprite` 对象，读它的 `m_Name`。**每个槽只有一张精灵（`spriteCount == 1`）**：

| prefab | 相对根的路径 | Sprite pathID | 精灵名 |
|---|---|---|---|
| `OrkBuff_Recall` | `Sparks/Skulls` | -1938810849948507115 | **Skulltop** |
| `OrkBuff_Recall` | `Sparks/Jaw` | -4750293957403969043 | **Skull_jaw** |
| `OrkBuffGreenWaaagh` | `Border Glow/Skulls` | -1938810849948507115 | **Skulltop** |
| `OrkBuffGreenWaaagh` | `Border Glow/Skulls/Jaw` | -4750293957403969043 | **Skull_jaw** |
| `OrkBuffGreenWaaagh` | `Border Glow/Skulls/Left` | -100631838266213495 | **BW_Trim3** |
| `OrkBuffGreenWaaagh` | `Border Glow/Skulls/Right` | -3146409238998086377 | **BW_Trim2** |
| `OrkBuff_Snakebitez_2_Intense` | `Sparks/Skulls` | -1938810849948507115 | **Skulltop** |
| `OrkBuff_Snakebitez_2_Intense` | `Sparks/Skulls/Jaw` | -4750293957403969043 | **Skull_jaw** |
| `OrkBuff_KrumpDaGitz` | `Border Glow/Skulls` / `/Jaw` / `/Left` / `/Right` | 同上四张 | Skulltop / Skull_jaw / **BW_Trim3** / **BW_Trim2** |
| `OrkBuff_Snakebitez_2` | `Sparks/Skulls` / `/Jaw` / `/Left` / `/Right` | 同上四张 | Skulltop / Skull_jaw / **BW_Trim3** / **BW_Trim2** |

⚠️ `Left` = **BW_Trim3**、`Right` = **BW_Trim2**（原包就长这样，⛔ 别按「左小右大」猜）。
⚠️ 这些 prefab 里 `enabled` / `mode` 也逐条核过：`OrkBuff_Recall` 与 `_Snakebitez_2_Intense` 的
`Skulls` 是 `enabled=false`（`mode=1` Sprites）；其余骷髅槽 `enabled=true`。我们导出的 prefab 与之一致。

**名字 ↔ 图集矩形** 另用两条独立证据交叉核过（图集 `Skull Particle icons` · pathID 1583738769577704870）：

- `SpriteAtlas.m_PackedSprites[i]` 解引用后的 `Sprite.m_Name` == `m_PackedSpriteNamesToIndex[i]` —— **5/5 全等**
  （`bundle_.../SpriteAtlas/SpriteAtlas_1583738769577704870.json`）；
- 每张 Sprite 自己的 `m_RenderDataKey` == `m_RenderDataMap` 里第 i 条的 Hash128 —— **5/5 全等**
  （`bundle_.../Sprite/{BW_Trim2,BW_Trim3,BW_Trim1,Skull_jaw,Skulltop}.json`）。

即：名字顺序、渲染数据顺序、精灵对象本身三条互相独立，都对得上。

### 2·2 盘上现状：三件的 8 个槽**已经是真 guid**

`Unity/MyGame/Assets/WarpforgeVFX/Prefabs/`（mtime 20:10 / 20:11）：

| prefab | Skulls | Jaw | Left | Right |
|---|---|---|---|---|
| `OrkBuffGreenWaaagh.prefab` | `d588fbf8dceb94347831c56f1a6a019b` = `Skulltop_sprite.png` | `b5f3df098d7fef248a10b58bd375face` = `Skull_jaw_sprite.png` | `087c27ddd66c86c42a1a65b67a5218ff` = `BW_Trim3_sprite.png` | `524b081990825394c9ceca29399203e2` = `BW_Trim2_sprite.png` |
| `OrkBuff_Recall.prefab` | 同上 Skulltop | 同上 Skull_jaw | — | — |
| `OrkBuff_Snakebitez_2_Intense.prefab` | 同上 Skulltop | 同上 Skull_jaw | — | — |

### 2·3 修复发生在哪一次运行（日志）

`d:/4/_tmp_view/export_listed_1021.log`：

- `:5574` `A1325 图集精灵 \`Skulltop\` **走退路** m_RenderDataMap（图集 \`Skull Particle icons\` · 纹理 \`sactx-0-1024x512-BC7-Skull Particle icons-5e776a28\` 1024x512）⇒ 裁切 (638.0761,259.0129,175.911,178.911)｜… 名字表 idx=4`
- `:5596` `Skull_jaw` … `名字表 idx=3`
- `:6387` `BW_Trim3` … `名字表 idx=1`
- `:6409` `BW_Trim2` … `名字表 idx=0`
- 对应导出：`:5622` `已导出 \`OrkBuff_Recall\``、`:6435` `已导出 \`OrkBuffGreenWaaagh\``、`:6499` `已导出 \`OrkBuff_Snakebitez_2_Intense\``。

⇒ **走的是 ③ `m_RenderDataMap`**（不是 ④ 已知兜底）。日志原文 `GetSprite→\`Skulltop(Clone)\`(texture=null …)`
说明**精灵对象不是 null**（W60 §9·5 的「这一格本身就是 null」不成立），失败点只是 `SpriteRenderDataKey(s)`
那句静默 return —— 当天已修。

### 2·4 导出的 PNG 与原图集**逐像素相同**

`Unity/MyGame/Assets/WarpforgeVFX/Textures/*_sprite.png` vs 原图集 `sactx-0-1024x512-BC7-Skull Particle icons`
按 `m_RenderDataMap.textureRect` 裁出来的块（左下原点换算到 PIL 的 top-left）：

| 精灵 | 尺寸 | 逐像素差 |
|---|---|---|
| `Skulltop` | 176×179 | **0 / 31504 像素**（max channel diff 0） |
| `Skull_jaw` | 233×223 | **0 / 51959** |
| `BW_Trim2` | 227×334 | **0 / 75818** |
| `BW_Trim3` | 212×277 | **0 / 58724** |

导入设置也对：`spriteMode: 1` · `spritePixelsToUnits: 100`（原版 `m_PixelsToUnits` = 100）·
`spritePivot 0.5/0.5`（原版四张 pivot 都是 0.5）· `textureType: 8`。
（脚本：`_tmp_view/w61/verify_crop.py`，UnityPy 1.25.3 直读原包，不影响工程。）

### 2·5 台账那三条读数**是修复前**的

`资料/比对基线/sweep_exp.tsv` mtime **19:42**、`sweep_orig_1021.tsv` **18:45**；
修复运行 `export_listed_1021.log` **20:11** ⇒ 台账里 `1.46 / 1.48 / 1.54×` 早于修复，**必须重扫才算数**。

---

## 3. 改了什么

只改一个文件：`Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs`（+157 / −18）。

1. **`:1879-1929` `Export()` 的 `textureSheetAnimation` 循环** —— `s == null` 不再静默 `continue`：
   - 命中 `KnownUvModuleSprites`（16 条已证明的槽）**且 `spriteCount == 1`** ⇒ 用
     `KnownRects_SkullParticleIcons` 的真值 rect（来自 `m_RenderDataMap`）从图集纹理裁一张精灵、
     `SetSprite(i, …)`，并打一行 `走已知槽位表`；
   - 表外 ⇒ **只有**「`mode == Sprites` 且模块启用」才 `LogWarning`（点名 prefab/路径/槽号）。
     `mode == Grid` 的那些是 Unity 默认的「单元素空表」，本来就不该有精灵 —— 出声只会刷满日志。
   - ⛔ 两条都不拿别的图顶上。
2. **`:3123-3146` `ImportSprite`** → 拆出 `:3150 ImportSpriteFromRect(name, tex, rect, ppu, pivot, fullSize, border)`。
   行为**一字未改**（同一段裁切/落 PNG/导入），只是让「只有名字 + rect、没有 Sprite 对象」的兜底路能复用。
3. **`:3728 KnownUvModuleSprites`**（16 条）+ `:3749 TransformRelPath` + `:3759 TryKnownUvModuleSprite`。
   键 = `prefab 根名 + "/" + 相对根的 Transform 路径`。表头写了每一条怎么从原包解出来的。
4. **`:774-775` `ListedPrefabs`** 增加 `OrkBuff_KrumpDaGitz` / `OrkBuff_Snakebitez_2`（同族同一个病，见 §5）。
5. **订正三处过期注释**（铁律 5，保留更正痕迹）：`ListedPrefabs` 段（`:759`）、④ 段（`:3314`）、
   `KnownRects_SkullParticleIcons` 段（`:3681`）—— 原文都写着「①②③ 给不出来 / 走已知图集兜底」，
   实际是 ③ 给得出、走的是 ③。

**秒级类型检查**：`bash Unity/工具/typecheck.sh` → 运行时错误 **0** · 编辑器错误 **0**。

---

## 4. 没查清的部分（诚实记）

1. **`tsa.GetSprite(i)` 什么时候会真的返回 null（而不是返回 `texture=null` 的 Clone）没定位到触发条件。**
   已知它至少发生过：`OrkBuff_KrumpDaGitz` / `OrkBuff_Snakebitez_2` 的 prefab 停在 **10-10 16:08**，
   里面留着 `sprites: - sprite: {fileID: <原包 pathID>, guid: 00000000000000000000000000000000, type: 0}`
   —— 这正是「`ImportSprite` 返回 null ⇒ 没调 `SetSprite` ⇒ 原样的伪引用被存进 prefab」留下的痕迹。
2. **新兜底里 `FindTextureInPacks("sactx-0-1024x512-BC7-Skull Particle icons")` 在 null 槽场景下取不取得到那张图集纹理，
   本轮没法验**（不跑 Unity）。同步点重导时看日志：出现 `走已知槽位表` 且后面没有「图集纹理取不到」即通过；
   若报「取不到或 rect 越界」，就是这条缺口，下一步改走「按图集对象（`SpriteAtlas`）取纹理」。

---

## 5. 顺手发现

### 5·1 同一病**全工程还有 35 件 / 50 格**（⚠️ 不在本批范围，但都要做 —— 铁律 11）

扫描 `Unity/MyGame/Assets/WarpforgeVFX/Prefabs/*.prefab`（964 件），匹配
`- sprite: {fileID: <非0>, guid: 00000000000000000000000000000000, type: 0}`（= 原包 PPtr 没导入）：

```text
  5  Environmental Condition Necrons Immortal Beams     2  ArdAsNailsEffect
  4  Buff_Red_ork                                       2  Environmental Condition Necrons Solar Storm
  4  OrkBuff_KrumpDaGitz          ← 本批已纳入          1  BeastSnagga_Summon / Birth of a Saga / CrueltyEffect
  4  OrkBuff_Snakebitez_2         ← 本批已纳入          1  DaIrongobEffect / EcstasyEffect / Emperors Children Generic Buff(+Record)
  1  Environmental Condition Emperor's Children 1 Green / 1 Pink REJECTED
  1  Execution_BlackLegion / Explosion Fenrisian Monstrosities / Explosion Hive Fleet Arrival Explosions only
  1  Explosion Hive Fleet Arrival Tendrils / FerocityEffect / Goff_GreatestWarboss / Goff_ProphetOfDaWaaagh
  1  Goff_WaaaghRed_skulls / Immortal Beam / Lightning_Green / MoreDakkaEffect
  1  Psychic_Lightning_blast(+chaos) / Psychic_Lightning_chaos / Psychic_Lightning_warlord_chaos / …warlord_OLD
  1  Scream_SW_Ragnar / StimulateEffect / Trait_GainQuest / Uprising Trait
```

**建议**：另开一批（不塞进本批）走 `RunListed` 逐件重导 —— 按 §2·3 的机制，重导一次就会被 ③ 修好；
不在 `KnownUvModuleSprites` 表里的件，新循环也会在失败时**出声**，不会再静默。
（`OrkBuff_Snakebitez_1` / `_1_2D_SFX` / `_1_NoIcon` / `_1_Quick` 原包**本来没有** UVModule 精灵，不是缺口。）

### 5·2 原包交叉核对时确认的旁证（不用改，但别再被误导）

- `OrkBuff*` 这些 prefab 的 `UVModule.sprites` **不是空表**，`Skull Particle icons` 里是 5 张（含没用到的 `BW_Trim1`）。
- 我们导出的 `OrkSkullParticle.mat` `_MainTex: {fileID: 0}` **与原版一致**（原版 Material 的 `_MainTex`
  也是 `m_PathID: 0`）—— 粒子贴图靠 TextureSheetAnimation 的精灵走，不是材质缺图。

---

## 6. 主对话同步点要跑什么

1. **重导这 5 件**（3 件本批 + 2 件顺手，`ListedPrefabs` 已含）：
   ```bash
   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
     -executeMethod EffectExporter.RunListed -logFile "d:/4/_tmp_view/export_listed_w61.log"
   ```
   ⛔ **别跑 `Run()`**（958 件全量），只跑 `RunListed`。
2. **跑完看四个数**：
   - 日志 `指定 prefab 导出完成：**N/N**`（本批名单那几件要全中）；
   - `OrkBuff_KrumpDaGitz.prefab` / `OrkBuff_Snakebitez_2.prefab` 里 `sprites:` 下的
     `guid: 0000…` 变成真 guid（判据：`grep -c "guid: 00000000000000000000000000000000"` 应归零，
     ⚠️ 同 prefab 别的 0 guid 不算）；
   - 日志里应**仍走** `A1325 图集精灵 … 走退路 m_RenderDataMap`；只有真的 null 槽才出现 `走已知槽位表`；
   - **重新扫一遍 sweep**（`WFSWEEP_SIDE=both`）→ 看 `OrkBuff_Snakebitez_2_Intense` /
     `OrkBuffGreenWaaagh` / `OrkBuff_Recall` 的亮度比是否从 **1.46 / 1.48 / 1.54** 回落（目标 ≈1.0）。
     若回落不到位，下一个嫌疑就**不再是精灵**（重点看 shader/混合与「多发射器」那几层）。
3. 另外两件顺手重导的（`OrkBuff_KrumpDaGitz` / `OrkBuff_Snakebitez_2`）**台账里没有独立条目**，
   重导后如需验收，走同一套 sweep 的 `OrkBuff_*` 行对比即可。
