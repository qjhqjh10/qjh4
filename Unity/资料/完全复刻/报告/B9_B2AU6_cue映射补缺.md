# B9 · `B2AU6` · cue → WindowSounds 缺口补齐（37 条）

> 执行代理 `B2AU` · 2026-10-27 · **只改了映射表一份文件**（`判据/音效cue到WindowSounds_映射.tsv`），⛔ 没有碰任何 `.cs`。
> 派活：把映射表里「**缺 UI 20 + 缺战场环境 17 = 37 条**」补齐。

---

## ① 逐条归类（37 条）

**结论：0 条「原版有、我们永远没有」—— 37/37 的音频**原版本地就有**。**

| 类 | 条数 | 含义 |
|---|---|---|
| **A · 我方已有承载同一 clip 的 cue**（只是没登记） | **2** | 状态改成 `对上·走clip代替`（表内已有 5 条前例） |
| **B · 原版音频在本地、但我们没导入** | **35** | 状态改成 `缺·原版资产在本地未导入`（比原来的「缺·UI / 缺·战场环境」准确） |
| C · 原版自己也没有 | **0** | — |

### A（2 条）—— 只缺登记，已补进表
| 序 | 原版 cue | clip | 我们的 key | 出处 |
|---|---|---|---|---|
| 147 | `Draft Pack Appear` | `CardMove` | **`CardMove`** | `animfx_sounds.json` + `Art/audio/sfx/CardMove.wav` |
| 149 | `Draft Pack Selected Moving` | `CardDraw` | **`drawCardFromDeck`** | `battle_sounds.json` + `Art/audio/sfx/CardDraw.wav` |

> ⚠️ 这两条我方**没有同名 cue**，是**按同一 clip 复用**（表里 `对上·走clip代替` 这一档本来就是这个意思）。
> 接的时候要按原表那三列（音高 / 音量 / ttp）覆盖 —— 复用那条 cue 自己的 pitch/volume 与原版这里**未必相同**（备注列已写）。

### B（35 条）—— **原版文件就在本地**，缺的是「导入 + 一条 cue 条目」
证据：全部能在 `d:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/AudioClip/<文件名>` 找到（本轮逐条现查，**37/37 命中**）。

| 序 | 原版 cue | clip（原版文件名） | 域 |
|---|---|---|---|
| 11 | `Asteroid Zone` | `Dark Angels Asteroid Zone.wav` | 战场 |
| 47 | `Booster Open AstraMilitarum` | `Open_Cardpack_AstraMilitarum.wav` | UI |
| 48 | `Booster Open BlackLegion` | `Open_Cardpack_BlackLegion.wav` | UI |
| 49 | `Booster Open Emperors Children` | `Open_Cardpack_EmperorsChildren.wav` | UI |
| 50 | `Booster Open Orks` | `Open_Cardpack_Goff.wav` | UI |
| 51 | `Booster Open SaimHann` | `Open_Cardpack_SaimHann.wav` | UI |
| 53 | `Booster Open Sororitas` | `Open_Cardpack_Sororitas.wav` | UI |
| 56 | `Booster Open Tau` | `Open_Cardpack_Tau.wav` | UI |
| 79 | `ButtonClick 1` | `UI Click 1.wav` | UI |
| 124 | `Daemonic Feast` | `Black Legion Daemonic Feast.wav` | 战场 |
| 129 | `Dawn Attack` | `Astra Militarum Dawn Attack.wav` | 战场 |
| 134 | `Deck Editing Valid Deck` | `Progress Jingle.ogg` | UI |
| 148 | `Draft Pack Finish Animation` | `UI Click Card 3.wav` | UI |
| 150 | `Draft Prebuilt Pack preview` | `UI Click Card 3.wav`（同 148 同一 clip） | UI |
| 170 | `Energy Accumulated` | `Energy 1.wav` | UI |
| 171 | `Energy Accumulated Used` | `Energy 2.wav` | UI |
| 172 | `Everstorm` | `Everstorm.wav` | 战场 |
| 192 | `Factory Overdrive` | `Astra Militarum Factory Overdrive.wav` | 战场 |
| 198 | `Firestorm Loop` | `Firestorm Long.wav` | 战场 |
| 200 | `First Light` | `First Light.wav` | 战场 |
| 207 | `Forge Card` | `Forge Card.wav` | UI |
| 211 | `Full Moon` | `Full Moon.wav` | 战场 |
| 213 | `Gacha vault open` | `Vault Crate Open Full.ogg` | UI |
| 261 | `Immortal Beams` | `Necrons Immortal Beams.wav` | 战场 |
| 335 | `Orbital Initial` | `Orbital Initial ringing.wav` | 战场 |
| 349 | `Orks Dust Storm Start` | `Orks Dust Storm Start.wav` | 战场 |
| 393 | `Ranked Seal SFX` | `UI Click 6.wav` | UI |
| 459 | `Star Orbit` | `Dark Angels Sun Orbit.wav` | 战场 |
| 472 | `Sump Overspill` | `Sump Overspill.wav` | 战场 |
| 505 | `Tau Electro-Static Interference` | `Tau Electro-Static Interference Initial.wav` | 战场 |
| 515 | `Toxic Fumes Start` | `Toxic Fumes Start.wav` | 战场 |
| 584 | `UI Raid Claim Reward Ching` | `UI RaidClaimRewardChing.wav` | UI |
| 585 | `UI Raid Claim Reward Wait` | `UI RaidClaimRewardWait.wav` | UI |
| 598 | `Warp Storm` | `Black Legion Warp Storm.wav` | 战场 |
| 601 | `Webway Rift` | `Aeldari Blackout.wav` | 战场 |

> ⚠️ **`148` 与 `150` 是同一个 clip**（`UI Click Card 3.wav`）但**两条 cue**（原版两个不同 prefab 引用）⇒ 导入一次、建两条条目。

---

## ② 证据（怎么判的，可复跑）

| 判据 | 做法 | 结果 |
|---|---|---|
| 我们的 cue 表里有没有这个 **cue 名** | 读 `Resources/animfx_sounds.json`（473 条）+ `Resources/battle_sounds.json`（27 条）的 `cues[].name` | 37 条**全部没有**同名 cue |
| 我们的 cue 表里有没有 **承载同一 clip** 的 cue | 扫两张表的 `cues[].clips[]` | **命中 2 条**（`CardMove` / `CardDraw`） |
| 我们的**音频文件**有没有 | 扫 `Resources/Art/audio/**`（2459 个文件：`sfx` 539 / `vo` 1893 / `bgm` 25 / `music` 2），并按「去空格/下划线/大小写」模糊再查一遍 | 只命中那 2 个 clip；**其余 35 个模糊也找不到** |
| **原版**有没有这个音频 | `d:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/AudioClip/` 逐个对名（含模糊） | **37/37 全在**（`.wav` 35 个 / `.ogg` 2 个：`Progress Jingle` · `Vault Crate Open Full`） |

复跑脚本（本轮写的只读探针，留在 `工具/` 下）：
```bash
D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/_b2au6_classify.py     # 初次分类
D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/_b2au6_orig.py         # 原版 bundle 有没有
D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/_b2au6_fill_map.py     # dry-run（默认）
D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/_b2au6_fill_map.py --apply
```

---

## ③ 补不了的原因（这 35 条为什么没在本轮补完）

**两条都在我的白名单外，且都不是「随手加一行」能了事的：**

1. **导入音频文件**（35 个）：我们这一侧的导入器是 `工具/import_original_audio.py`（本波白名单只开到 `gen_*sound*.py` / `gen_*cue*.py`）。
   ⛔ 我也**没有**手工 `copy` 那些 wav —— 导入器有自己的规则（文件名去尾随空格 / 格式与导入设置），手抄会产出与其它 539 个音效**不一致**的资产。
2. **加 cue 条目**（35 条）：`Resources/animfx_sounds.json` 的头部与派活包 §3·1 都写着「**要加 key 时由 `工具/import_original_sfx.py` 重生，⛔ 别手改**」。
   该生成器也不在 `gen_*sound*` 这一档白名单里。
3. **UI 那一半（20 条里除去 2 条 A 的 18 条）还要「我们的 key」**：UI 音效的 key 登记在 `Shell/WindowSounds.cs`（C#，本波 ⛔ 只读）。
   好消息：`WindowSounds` 的文档里**已经写下了其中一族的消费者**——`Booster Open *` → `BoosterPackVisualItem.openBoosterPack`（`WindowSounds.cs:28`），
   所以这 7 条**接的时候不用再翻 prefab**，只差「clip + cue 条目 + 一个常量」。

> 换句话说：这 35 条**不是「判据为空」也不是「原版没有」**，是**「数据 + 生成器」两步在白名单外**。按铁律 11 属「排先后」，⛔ 不是不做。

---

## ④ 待接线清单（精确到文件与动作）

| # | 动作 | 文件 | 备注 |
|---|---|---|---|
| 1 | 跑音频导入器，把上面 35 个文件导进 `Resources/Art/audio/sfx/` | `工具/import_original_audio.py` | 源目录 = `bundle_soundcollection_assets_all/AudioClip/`；名字以该目录的**文件名为准**（原版 `clipList` 解出来的名字与文件名逐条对得上，本轮已核） |
| 2 | 在音效生成器里加 35 条 cue（`name` = 原版 cue 名、`clips` = 上面的 clip、`minPitch/maxPitch/minVolume/maxVolume/timeToPlayAgain` = 映射表本行那三列），然后重生 `Resources/animfx_sounds.json` | `工具/import_original_sfx.py` | ⛔ 别手改 JSON |
| 3 | UI 那 18 条的 **key 常量**（`WindowSounds` 里加 `public const string …Cue = "原版 cue 名";`） | `Shell/WindowSounds.cs`（`B1UI`/`E3CMP`） | 其中 7 条 `Booster Open *` 的消费者已写在 `WindowSounds.cs:28` |
| 4 | 战场 17 条：把 cue 名接到战场环境音（`EnvironmentApplier` / `ArenaBuilder` 那一侧） | 战场线 | ⚠️ 这 17 条里有 11 条是「环境事件音」（Asteroid Zone / Everstorm / Warp Storm …），属**战场环境循环/触发**，不是一次性 UI 音 |
| 5 | `148` / `150` 共用 `UI Click Card 3` ⇒ **导入 1 次、建 2 条 cue** | 同上 | 别导两遍 |

---

## ⑤ 本轮改动清单

| 文件 | 变化 |
|---|---|
| `资料/完全复刻/判据/音效cue到WindowSounds_映射.tsv` | **37 行**更新：`我们的key` / `我们侧出处` / `状态` / `备注`。总行数不变（615 = 表头 + 614）。<br>状态分布（现读）：`对上` 506 · **`对上·走clip代替` 7**（原 5 + 本轮 2）· **`缺·原版资产在本地未导入` 35**（新值，替换原 `缺·UI` 20 / `缺·战场环境` 17）· `缺·本地无任何引用` 64 · `缺·活动数据(GUID)引用` 2 |
| `工具/_b2au6_probe.py` / `_b2au6_classify.py` / `_b2au6_orig.py` / `_b2au6_fill_map.py`（新） | 只读探针 + 映射表补写器（**默认 dry-run，`--apply` 才写**） |

- ✅ **`缺·UI` 与 `缺·战场环境` 两个状态在本表里已归零**。
- ⛔ 没有改任何 `.cs`、没动 git、没改正本与账主表。
- 表仍是 **纯 LF、12 列**（现读校验：`rows 615 · cols [12] · bareCR 0`）。

---

# 补缺实施（2026-10-27 · 白名单放开到导入器/生成器/`WindowSounds.cs`）

## ① 真的导进去了：跑 `工具/import_original_sfx.py`（⛔ 没手工 copy）

在 `EXTRA_CUES` 里追加了这 **37 条 cue**（带判据注释：`37/37` 原版音频在
`bundle_soundcollection_assets_all/AudioClip/`、`148`/`150` 共用 `UI Click Card 3`、8 处 cue 名 ≠ clip 名），然后跑**原生成器**：

```
特效在用：… ⇒ 1224 条，涉及 **510** 个不同 cue（其中 `EXTRA_CUES` 硬编 80 个）   ← 改前 473 / 43
解析成功 510/510 个 cue；要用到 542 个音频文件
已拷 **34** 条 · 已存在跳过 508 条 · 补 meta **34** 条 → Resources/Art/audio/sfx（{.wav: 537, .ogg: 5}）
表已写：Resources/animfx_sounds.json
```

- ✅ **拷的是 34 个文件、不是 35** —— 正是预判的「`148`/`150` 共用同一个 clip」⇒ **导入 1 次、建 2 条 cue**。
- ✅ `.meta` 是生成器顺带补的（共用实现 `工具/_sfx_meta.py`，模板 = 同目录同扩展名众数、只换 guid、已存在的不碰）
  ⇒ 不会出现「新文件用内置默认导入（`loadType: 0`）」那种与既有 510 条不一致的情况。
- ✅ **反向查漏**（脚本自带）候选 **6 → 3**：`Asteroid Zone` / `ButtonClick 1` / `Dawn Attack` 三条已被本批收掉。

## ② 校验（现读，脚本 `工具/_b2au6_verify.py`）

| 检查 | 结果 |
|---|---|
| `animfx_sounds.json` 条数 | **510**（473 + 37） |
| 37 条 cue 是否都在表里 | **缺 0** |
| 这些 cue 指向的 clip 是否都有文件 | **缺 0** |
| `Art/audio/sfx/` clip 文件数 | **573**（不含 `.meta`） |
| `battle_sounds.json` | 27（本条线未动） |

抽样参数（生成器**从原版 cue 资产实读**，不是我们填的）：
`Asteroid Zone` → clip `Dark Angels Asteroid Zone`，pitch 1.0–1.0 / vol 1.0–1.0 / ttp 0.5；
`Booster Open *` → clip `Open_Cardpack_*`，pitch 1.0 / vol **0.75** / ttp 1.0。

## ③ 映射表收口

- 那 **37 行** 全部改成 **`对上`**（`我们的key` = cue 名、`我们侧出处` = `animfx_sounds.json` + 具体 clip 文件路径、
  备注里记下 pitch/vol/ttp 与「已落地」）。
- 现读状态分布：`对上` **543**（506 + 37）· `缺·本地无任何引用` 64 · `缺·活动数据(GUID)引用` 2 · **`缺·原版资产在本地未导入` 0** · `缺·UI` 0 · `缺·战场环境` 0。

## ④ `Shell/WindowSounds.cs`：20 条 UI key 常量（追加在类尾）

7 条 `Booster Open *` + 13 条其余（`ButtonClick 1` / `Deck Editing Valid Deck` / Draft 四条 / Energy 两条 /
`Forge Card` / `Gacha vault open` / `Ranked Seal SFX` / Raid 两条），每条注释里带 **clip 名**与三处**易错点**：
① `ButtonClick 1` 的 clip `UI Click 1` **≠** `ButtonClick` 的 `UI Select Toggle`；
② `Draft Pack Finish Animation` 与 `Draft Prebuilt Pack preview` **同一个 clip**（两条 cue）；
③ `Booster Open Orks` 的 clip 叫 `Open_Cardpack_Goff`（名字对不上，⛔ 别按名字猜文件）。

> ⚠️ 本段只是 **key 注册表**：`WindowSounds.Play(cue)` 是唯一播放口，谁去播由各自的窗决定（`B1UI`）。

## ⑤ 战场那 17 条：cue 现在**可解析**了，但我们的战场数据里还没有引用 —— **待战场线接线**

- cue 已进 `animfx_sounds.json` ⇒ `WarpforgeEffectPlayer` 的解析口（`sound = "@asset:MonoBehaviour:<cue 名>"`，
  `CardPresentation/Battle/AnimFXController.cs:705-757`，诊断口 `UnwiredSoundCues` / `BadCueList`）**现在能解出来了**。
- 🔴 **但战场/环境数据里没有这几条 cue 的引用**：现读 `数据/游戏数据/env_blendables.json` 全文件只有 **1 个**
  `@asset:MonoBehaviour:` 引用（`Shake Earthquake`）；那 17 个名字在文件里出现，是作为**效果 prefab 名**的子串
  （例：`Environmental Condition Dark Angels Asteroid Zone`），**不是** `sound` 字段。
- 原版这一族的载体是 `ShakeAndSoundObject`（`AnimFXCheck.cs:2139-2140` 的断言就是「起得来（带 `ShakeAndSoundObject`）」），
  而**我们工程里没有这个类**（`grep "class ShakeAndSoundObject"` = 0）⇒ 环境音那一跳**根本没复刻**。
- 精确接线（战场线）：
  ① 给环境效果 prefab/数据补一个 cue 字段，值 = `@asset:MonoBehaviour:<cue 名>`（17 条名字见本报告 §① B 表）；
  ② 消费口用现成的 `WarpforgeEffectPlayer` 那条解析路（⛔ 别另开一套播放入口）；
  ③ ⚠️ **11 条是环境事件音**（Asteroid Zone / Everstorm / Warp Storm / Dawn Attack / Firestorm Loop /
     Full Moon / Immortal Beams / Sump Overspill / Toxic Fumes Start / Warp Storm / Webway Rift …），
     与 6 条一次性音**形状不同**（事件触发 vs 一次性播放）⇒ 别用一个模板批量接。

## ⑥ 本轮改动清单 / 自检

| 文件 | 变化 |
|---|---|
| `工具/import_original_sfx.py` | `EXTRA_CUES` 追加 37 条（带判据注释块） |
| `Resources/animfx_sounds.json` | 473 → **510** cue（生成器重生，⛔ 没手改） |
| `Resources/Art/audio/sfx/**` | **+34 个音频 + 34 个 `.meta`**（`Resources/Art/` 被 `.gitignore` 排除，不进仓库） |
| `Shell/WindowSounds.cs` | 类尾 +20 条 UI cue 常量（带 clip 名与易错点注释） |
| `判据/音效cue到WindowSounds_映射.tsv` | 37 行 → `对上`；`缺·原版资产在本地未导入` 归 0 |
| `工具/_b2au6_verify.py` / `_b2au6_mark_done.py`（新） | 校验器 + 映射收口器（**默认 dry-run，`--apply` 才写**） |

- ✅ `.cs` 改完跑了 `TMPDIR=/tmp/wf_B2AU bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**。
- ⛔ 没跑 Unity（`.meta` 由生成器按模板补好；Unity 侧下次批处理会正常导入）；⛔ 没动 git；⛔ 没改正本与账主表。
- ⚠️ **反向查漏还剩 3 个候选**（本轮之前就有，不属于这 37 条）：`Cardmove`（疑为 `CardMove` 的大小写变体噪声）、
  `Draft Mode Loss Jingle` / `Draft Mode Warlord Appear`（`Shell/DraftModeWindow.cs:2648/3536` 在用）——
  **没顺手收**：收了要连判据一起写，且第 2/3 条属轮抽线，⛔ 别混进本批。

---

# 环境音载体（第九批 · 2026-10-27）

## ① 那 3 个漏：逐条判成「**真缺**」，已收（⛔ 不是噪声）

| cue | 判 | 判据（原版 cue 资产实读 + 我们代码） |
|---|---|---|
| `Cardmove` | **真缺 · 另一条真 cue** | 包里 `Cardmove.json` 与 `CardMove_4812904303840730115.json` **是两个资产**（前者 `m_Name = Cardmove`）⇒ 🔴 **不是 `CardMove` 的大小写笔误**。pitch 1.0–1.0 · vol 1.0–1.0 · ttp 0.1。我们代码按名字播它：`Editor/SoundCueAssertT.cs:77` |
| `Draft Mode Loss Jingle` | **真缺** | 同名 cue 包装在本地；pitch 1.0 · vol 1.0 · ttp 0.1；消费点 `Shell/DraftModeWindow.cs:2648` |
| `Draft Mode Warlord Appear` | **真缺** | pitch 0.8–1.199 · vol 0.8 · ttp **0**；消费点 `Shell/DraftModeWindow.cs:3536`。⚠️ 它与 `Cardmove` **共用同一个 clip**（pid `-6740773956303463874`） |

运行结果（`工具/import_original_sfx.py` 再跑一次）：
```
解析成功 513/513 个 cue；要用到 543 个音频文件
已拷 1 条 · 已存在跳过 542 条 · 补 meta 1 条
查漏（反向）：… ⇒ **0 个候选**     ← 改前 3
```
⇒ cue **510 → 513**；因为 `Cardmove` 与 `Warlord Appear` 共用一个 clip、`Loss Jingle` 的 clip 已在，**只拷了 1 个新音频**。**反向查漏归 0**。

## ② 新建 `CardPresentation/Effects/ShakeAndSoundObject.cs`（原版类体逐句照抄）

判据 = ILSpy `out_cpp2il_dev_ilspy/ShakeAndSoundObject.cs` 全文 41 行（`decomp_full` 无同名产物，纯托管）：
```csharp
public class ShakeAndSoundObject : MonoBehaviour {
    private OverwriteCameraShakePreset[] onEnableShakes;
    private PlaySoundOnTime[] sounds;
    void OnEnable() { foreach (var s in onEnableShakes) s.Play(); }
    void Update()   { foreach (var s in sounds) s.Update(Time.deltaTime, base.transform); }
}
```
- ✅ `sounds` 那一半**逐句照抄**，**包括那个怪癖**：`Update` 传的是 **`Time.deltaTime`**（不是累计时间）——
  对 `PlaySoundOnTime.Update` 的判据来说等于「每帧都只拿到 ~1/60 s」⇒ 只有 `time ≈ 0` 的条目播**一次**。
  🔴 **注释里写死「照抄、⛔ 别顺手修正成累加」**（否则等于换一套行为）。
- ⚠️ **`onEnableShakes` 那一半没接**：原版类型 `OverwriteCameraShakePreset` **我们工程里没有**
  （`grep -rl` 只命中 `BattleDriver.cs` / `CardAnimClips.cs` / `CardFeel.cs` 的**注释**）；我们的震屏在 `Core/CardFeel.cs`（`E2UDW` 白名单外）。
  ⇒ 本件**不发明震屏**：`onEnableShakeNames`（🔶 自己设计：用名字数组带住数据）只**出声一次**并计入 `ShakesUnwired`，
  ⛔ 不假装震了。接线：把那串名字交给 `CardFeel` 的震屏口。
- 自检口：`EnableForTest()` / `UpdateForTest()`（编辑模式 `AddComponent` **不触发** `OnEnable/Update`）+ `UpdateTicks` / `SoundUpdates` / `ShakesUnwired`。

## ③ 🔴 **重要的否定结果：那 17 条环境 cue 并不是这 35 个 `ShakeAndSoundObject` 承载的**

我原本的假设是「环境效果 prefab 上带 `ShakeAndSoundObject` ⇒ 补上组件就能响」。本轮**现查两条都否掉了**：

1. `数据/游戏数据/env_blendables.json`（旁挂表）里 **`ShakeAndSound` 出现 0 次** —— 那张表只枚举
   **blendable** 组件（`ScenarioParticleSystemBlender` 62 / `ScenarioParticleSystemToggler` 36 / `ScenarioMaterialFader` 23 / `AnimFXController` **5** / …），
   `ShakeAndSoundObject` 不是 blendable ⇒ **不在表里**。
2. 直接扫原版 prefab 包：`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/*` 里带 `onEnableShakes`/`sounds` 字段的组件 = **35 个**；
   逐个把 `sounds[].sound` 的 PPtr 解回去 ⇒ **17 条环境 cue 一条都没被它们引用**（0/17）。
   探针：`工具/_b2au9_shake_probe.py`（只读，可复跑）。

⇒ **环境音那 17 条的真正载体还没定位到**（既不是 `ShakeAndSoundObject` 的 35 个实例，也不在 `env_blendables` 里）。
下一步建议（战场线 / 导出侧）：
- 用同一支探针把 **全部 612 个 cue** 反查「谁引用我」（`sounds[].sound` PPID → 组件），看这 17 条究竟挂在哪个组件的哪个字段上；
- 或者按 `AnimFXCheck.cs:2139` 那条断言的路子看 `Environmental Condition …` prefab 上「谁的 `AudioCue` 字段」指向它们；
- ⛔ 在定位到之前**不要**往 `env_blendables.json` 硬加 `sound` 字段 —— 那张表的 schema 是 blendable 的，塞进去没人读（等于死数据）。

## ④ 自检
- `TMPDIR=/tmp/wf_B2AU bash d:/4/Unity/工具/typecheck.sh` = **运行时 0 / 编辑器 0**（新类编译通过）。
- `animfx_sounds.json` **513** cue；反向查漏 **0**；`Art/audio/sfx/` 扩展名分布 `{.wav: 537, .ogg: 6}`（多 1 个 ogg = 新拷的那条）。
- ⛔ 没跑 Unity；⛔ 没动 git、没改正本与账主表。

---

# 环境音载体定位（第十批 · 2026-10-27）→ **17/17 定位到了，载体就是 `ShakeAndSoundObject`**

判据全文：**`判据/环境音17条_载体定位.md`**（逐条表 + schema 提案 + 订正）。摘要：

## ① 结论
- **17 条全部命中**，载体 = **`ShakeAndSoundObject.sounds[i].sound`**（`m_Script` pathID `8184164905518033017`），
  挂在 **19 个「环境条件」prefab** 上（`Environmental Condition …`）。
- 表格见判据件；三个非平凡点：
  🔴 **`Factory Overdrive` 与 `Toxic Fumes Start` 各有 2 个宿主**（共用环境！）⇒ 一个 cue 要挂两个 prefab；
  ⚠️ `Firestorm Loop` 在 **`sounds[1]`**（其余 16 条都在 `sounds[0]`）；
  ⚠️ **prefab 名 ≠ cue 名**（至少 5 处对不上，例如 `Star Orbit` 挂的是 `… Necrons Solar Storm`）⇒ 别按名字猜。
- **不在 `env_blendables.json` 里**（那张表只枚举 blendable 组件、`ShakeAndSound` 0 次）⇒ 载体信息只在
  **prefab 的序列化组件**里 ⇒ 我方要落表得**新开一张**旁挂表（`判据件 §③` 给了 schema 提案）。

## ② 🔴 订正上一批的假结论（我自己的错）
上一批我写「17 条**一条都没被**那 35 个 `ShakeAndSoundObject` 引用（0/17）」——**那是假阴性**。
**错因**：我用「文件名 `<cue 名>_<pid>.json`」建 `pathID → cue 名` 索引，而那 17 个 cue 的文件名是
**纯 `<cue 名>.json`（无 pid 后缀）** ⇒ 索引里没有它们 ⇒ 解成裸 `PID:…` ⇒ 误判「没引用」。
**正确做法**（本批）：直接拿映射表那列的 **`PathID` 全文反查引用方**，再顺着 `m_Component` 反查归属 GameObject。
⇒ `工具/_b2au9_shake_probe.py` 的结论**作废**，以 `_b2au10_{reverse,locate,owner}.py` 为准。

## ③ 接线方案（⛔ 本波只写不接）
1. 导出侧新增旁挂表（schema 见判据件 §③，`env_shake_sounds/1`）——⛔ 别塞 `env_blendables.json`（那是 blendable 专用表 = 死数据）；
2. 运行时把表里的 `sound` 名字经 `WFSoundBank` 填进 `PlaySoundOnTime.sound`（`Effects/ShakeAndSoundObject.cs` 已建）；
3. `onEnableShakes`（震屏）仍待接 `Core/CardFeel.cs`（`E2UDW`）。

## ④ 复跑
`_b2au10_reverse.py`（全树反查）→ `_b2au10_locate.py`（定位到组件+字段）→ `_b2au10_owner.py`（`m_Component` 反查 prefab 名）。
扫描范围 = `assets_full` 全部 `MonoBehaviour/` + `AudioSource/`；`AudioSource.clip` 直挂路径 **0 命中**。

---

# 旁挂表（第十一批 · 2026-10-27）：`数据/游戏数据/env_shake_sounds.json` 已生成

## ① 产物
`数据/游戏数据/env_shake_sounds.json`（**18,421 B**，`_schema = env_shake_sounds/1`）
生成器 = **`工具/gen_env_shake_sounds.py`**（新写，**默认 dry-run**；`--apply` 写、`--check` 复验 idempotent）。

```
组件 = 35 · prefab = 35（全部 environmental）· sounds = 25 · shakes = 9
未解析：cue 1 · owner 0 · preset 9
--check： 一致        ← 生成器可重复跑，逐字节稳定
```

## ② 17 条**全部落在表里**（19 行，因为两条 cue 各两个宿主）
`Asteroid Zone` / `Daemonic Feast` / `Dawn Attack` / `Everstorm` / **`Factory Overdrive`(×2)** / `Firestorm Loop` /
`First Light` / `Full Moon` / `Immortal Beams` / `Orbital Initial` / `Orks Dust Storm Start` / `Star Orbit` /
`Sump Overspill` / `Tau Electro-Static Interference` / **`Toxic Fumes Start`(×2)** / `Warp Storm` / `Webway Rift`
—— 逐条带 `prefab 名 + time + is2d + soundPid`（判据件 §① 有同样的表）。

🔴 **两个接线时才知道的细节**：
- **全部 `time = 0.0`** ⇒ 配合 `ShakeAndSoundObject.Update(Time.deltaTime, …)` 那个照抄的怪癖，
  这些条目**正好会各播一次**（`numberOfTimesPlayed` 一置 1 就再达不到门槛）—— 与原版行为一致，⛔ 别去"修"。
- **`is2d` 逐条不同**：`Asteroid Zone` / `Dawn Attack` / `Immortal Beams` / `Orbital Initial` / `Star Orbit` /
  `Toxic Fumes Start`(Leviathan 那个) 是 **`is2d = false`（3D，用组件 transform 定位）**，其余是 2D ⇒ 填 `PlaySoundOnTime.is2d` 时**逐条抄**，别一律 true。

## ③ 两处「未解析」都写清了（⛔ 不是静默）
| 项 | 数量 | 是什么 | 处理 |
|---|---|---|---|
| `unresolvedCue = 1` | 1 | `EnvironmentalCondition Leviathan Acid Rain` 的 `sounds[0].sound` = **空 PPtr（`PID:0`）** | 🔴 **原版那一格本来就是空的**（铁律 11 例外①），不是我们解不出 ⇒ 表里保留 `PID:0` + 计入 stats |
| `unresolvedPreset = 9` | 9 | `onEnableShakes[i].presetSO` 指向的是 **`CameraShakePreset` SO**（另一种资产），我没有它的 pid→名索引 | 表里记 `PID:<n>`；**但整份覆盖参数（delay/sustain/amplitude/frequency/direction/attack/decay + 各 overwrite 开关）已全部内联进表** ⇒ 运行时**不需要那个 SO 也能实现震屏** |

## ④ 为什么另开一张表（再确认一次）
`env_blendables.json` 的 schema 是 `IScenarioEnvironmentBlendeable` 旁挂（现读 `ShakeAndSound` **0 次**）⇒
塞进去没人读 = 死数据。本表与它**并列**，字段名照原版序列化（`onEnableShakes` / `sounds`），⛔ 两者互不覆盖。

## ⑤ 本批改动 / 纪律
| 文件 | 说明 |
|---|---|
| `工具/gen_env_shake_sounds.py`（新） | 只读 `assets_full`；默认 dry-run、`--apply` 写、`--check` 校验 |
| `数据/游戏数据/env_shake_sounds.json`（新） | 35 prefab · 25 sounds · 9 shakes |

- ⛔ 没跑 Unity、没动 git、**没改任何 `.cs`**、**没动 `env_blendables.json`**、没改正本与账主表。
- 接线（下一批）：`Effects/ShakeAndSoundObject.cs` 读这张表 → 填 `PlaySoundOnTime`（`sound` 走 `WFSoundBank`、`is2d` 逐条抄）；
  震屏那一半按表里的内联参数实现（不再依赖 `OverwriteCameraShakePreset` SO）。



---

# 接线（第十二批 · 2026-10-27）：`ShakeAndSoundObject` 读表

## ① 做了什么
`CardPresentation/Effects/ShakeAndSoundObject.cs`（重写）：
- **声音那一半已接**：`Awake()` 里 `BindFromTable()` 读 `Resources/env_shake_sounds.json`
  （`entries[]` 扁平视图 —— `JsonUtility` 不支持字典），按 **`gameObject.name`**（或显式 `tableEntryName`）取自己那条，
  逐条填 `PlaySoundOnTime{ time, sound, is2d, repeat, loops, timeInterval }`。
  🔴 **`sound` / `is2d` 逐条抄**（⛔ 不按 prefab 名猜；环境这批 6 条是 3D）。
  `sound` 是 `PID:<n>` 的条目（= 原版空引用/未解）⇒ **出声且该条不播**（⛔ 不拿别的 cue 顶）。
- **震屏那一半：只解析 + 出声，⛔ 没接**。查清了入口 = **`Core/CardFeel.ShakeCamera(Camera, Transform worldAmp, float delay, …)`**（`CardFeel.cs:1378`），
  但 `CardFeel` 归 `E2UDW`、且它后面还有我没读全的形参 ⇒ **不猜签名**：表里的 `delay/sustain/amplitude/frequency/direction/attack/decay + 各 overwrite*` 全解析进
  `shakeParams`（读得到），然后每个 prefab **出声一次**「参数齐了，喂给 `CardFeel.ShakeCamera` 那一步待接」，⛔ 不假装震了。
  ⚠️ 原版那个 `presetSO`（`CameraShakePreset` SO）**不需要** —— 表里那份内联参数就是它的覆盖结果。
- 自检口：`BoundSounds` / `BoundShakes` / `LastBindCount` / `LastBindError` / `AllTimesZero` / `Is2dAt(i)` / `EnableForTest()` / `UpdateForTest()`。

## ② 生成器补了一条：同一份内容再落 `Resources/`
`工具/gen_env_shake_sounds.py` 现在写**两份**（同源、同字节）：`数据/游戏数据/env_shake_sounds.json`（给人看/分析）+ 
`MyGame/Assets/CardPresentation/Resources/env_shake_sounds.json`（运行时 `Resources.Load<TextAsset>` 只认这里）。
`--check` 两份都比；刚跑 `--apply`：`35,887 B × 2`（多了 `entries[]` 扁平视图）。

## ③ 自检
- `TMPDIR=/tmp/wf_B2AU bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**。
- 断言配方：`obj.BindFromTable()` ⇒ `BoundSounds == 表里该 prefab 的 sounds 条数` · `AllTimesZero == true` ·
  `Is2dAt(i)` 与表逐条一致 · 未接震屏 ⇒ `EnableForTest()` 后 `ShakesUnwired == shakeParams.Count`。
- ⛔ 没跑 Unity；⛔ 没动 git、没碰 `CardFeel.cs`、没改 `env_blendables.json`。

---

# A-02 快照脚本（第十三批 · 2026-10-27）

`CardPresentation/Editor/AudioSetup.cs`（**已存在**，本批在**类尾追加**两个 `-executeMethod` 入口）：

| 入口 | 做什么 |
|---|---|
| **`AudioSetup.VerifySnapshots`** | **只读**：列出 mixer 里现有快照（`AssetDatabase.LoadAllAssetsAtPath` 里的 `AudioMixerSnapshot`），逐个查 `WarpforgeAudio.SnapshotNames` 的 6 个名字，打 `✅/❌` 与 `命中数/6` |
| **`AudioSetup.CreateSnapshots`** | 建**缺的**那些（已存在 → **跳过、不改值** = 幂等），建完自动跑一次 `VerifySnapshots` |

- **名字只此一份** = `Core/WarpforgeAudio.SnapshotNames`（`SFX On` / `SFX Off` / `Music Low` / `Music Medium` / `Music High` / `Music None`），⛔ 脚本里没抄第二份。
- 建法 = 反射 `UnityEditor.Audio.AudioMixerController` 上「名字含 Snapshot+Add 且无参」的实例方法（与既有 `CreateMixer` 同一套反射路子）；**内部 API 改名/形态不符 ⇒ 出声**并打印 1 分钟手工做法（Audio Mixer 窗口点 `+` 改名），⛔ 不静默。
- 🔴 **数值本批没设**（如实）：派活包 A-02 只给了「6 个快照名字 + 3 个 `BattleGroup*` 组」，**没给 dB 值**；原版的每快照组音量在 mixer 资产里（我们还没有那些快照）。⇒ 脚本**只建壳、一个音量都不动**（⛔ 不编数值）。数值要么主对话在 Mixer 窗口调一次并存盘，要么等原版 mixer 各快照的 dB 解出来再补一版（`_stats` 之外没有猜的痕迹）。
- ⚠️ **未在 Unity 里跑过**（本波纪律：不跑 Unity）。若 `CreateSnapshots` 报「内部 API 形态不符」，用手工做法即可，`VerifySnapshots` 照样能验。
- `TMPDIR=/tmp/wf_B2AU bash 工具/typecheck.sh` = **运行时 0 / 编辑器 0**。

---

# `Cardmove` 撤销（第十四批 · 2026-10-27 · 铁律 5 就地订正）

## ① 撤销了什么
第九批我把 **`"Cardmove"`（小写 m）** 收进了 `工具/import_original_sfx.py` 的 `EXTRA_CUES` —— **错了**，本批删除，并**重跑生成器**。

## ② 错在哪（三条，全部现核）
1. **真正的换位 cue 是 `CardMove`（大写 M）**，它**本来就在** `EXTRA_CUES` 里 ——
   而且 import 脚本 **191-192 行早就写明**「小写 `Cardmove` 是另一个 1-clip cue，**本笔不要它**」（我没看那一段）。
2. 我给的依据 **读反了**：`Editor/SoundCueAssertT.cs:77` 是**禁止**这条 cue 的断言文案，不是「按名字播它」。
3. **全仓零生产消费点**。

## ③ 两个资产分别是什么（回答「那两个分别是什么」）
| 资产 | 内容 | 引用情况 |
|---|---|---|
| **`CardMove`**（`CardMove_4812904303840730115.json`，`m_Name = CardMove`） | **4 个 clip**：`CardMove` / `CardMove_1` / `CardMove_2` / `CardMove_3`，pitch **0.8–1.1**，ttp 0.1 | **有真消费点**：`Battle/CardDisplayWindow.SwapSoundCue = "CardMove"`（`CardDisplayWindow.cs:249`；断言 `SoundCueAssertT.cs:90`/`:104`）。另 `Draft Mode Warlord Appear` 用它 4 条里的第 1 条 |
| **`Cardmove`**（`Cardmove.json`，`m_Name = Cardmove`） | **1 个 clip** = pid `-6740773956303463874`（就是 `CardMove` 的第 1 条），pitch 1.0，ttp 0.1 | **零生产引用** ⇒ 属**反向查漏的报表**那一档，⛔ 不入 cue 表 |

🔴 **当时的直接错因（值得记）**：本机**文件名大小写不敏感** —— 我打开 `CardMove.json` 时，它实际解析到 `Cardmove.json`（Windows 上 `CardMove.json` 这个文件名并不存在），于是把两个资产看成了一个。
⇒ 判「这两个文件是不是同一个」时，**必须看文件里的 `m_Name`**，⛔ 不能只看路径。

## ④ 撤销后的登记（正路）
`Cardmove` 现在写在 import 脚本的 **`CODE_ONLY_KNOWN`**（只报告、不入表）里，带完整理由 ⇒ 反向查漏**回到 0 个候选**（脚本 docstring 说的常态）。

## ⑤ 重跑结果
```
改前：解析 513 个 cue · 反向查漏 3 个候选（其中 Cardmove 是误报）
改后：解析 **512** 个 cue · 反向查漏 **0 个候选** · 已拷 0 条 · 跳过 543 条（⇒ clip 文件一个都没删/没动）
```
- ✅ `animfx_sounds.json` = **512** 条；`CardMove` 在表里（4 clips / 0.8–1.1）；`Cardmove` **不在表里**。
- ✅ **那个 `.wav` 没删**（它与 `Draft Mode Warlord Appear` 共用；且 `Draft Mode Warlord Appear` 仍在表里、仍指向它）。
- ⚠️ `CardFaceAssertE5` 那条之前被 `Cardmove` 打红的断言应随之恢复（**未跑 Unity 验证**，按纪律由主对话统一复跑）。
