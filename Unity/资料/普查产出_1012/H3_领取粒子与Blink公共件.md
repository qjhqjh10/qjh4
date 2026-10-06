# H3 · A312（领取粒子 + 音效）+ A315（`BlinkGraphic` 公共件）—— 写手报告（2026-10-12）

> 派单：A312 + A315（同宿主一件）。判据来源 = `资料/普查产出_1012/V8_判据补查.md` §A312 / §A315（**已逐字读**）。
> 本件**只改这三个文件**：`Shell/RewardWindow.cs` · `Shell/BoosterInfoPopup.cs` · **新建** `Shell/BlinkGraphic.cs`
> （+ 它的 `.meta`）。⛔ 没碰 `Editor/*`、没动 git、**没跑 Unity 批处理**。
> 类型检查（`TMPDIR=/tmp/wf_h3 bash d:/4/Unity/工具/typecheck.sh`）：**运行时 0 / 编辑器 0**（每一批改动后各跑一次，共 5 次全 0）。
> 行号是本件**收工时**的读数（本仓行号会漂 ⇒ 按符号认）。

---

## 一、结论（三条）

1. **A315 做完了**：`BlinkGraphic` 已从 `RewardWindow` 的 8 个私有成员**抬成公共件**（`Shell/BlinkGraphic.cs`，新文件），
   `RewardWindow` 改成**转发**，点名的消费方 **`BoosterInfoPopup` 的 `Artwork/background` 已接上**（此前一片静止）。
   36 个实例的清单见 §四（**新增**：每个实例的宿主/所属 prefab/父链，上一轮只聚合了宿主名）。
2. **A312 的时序链做完了**（排期 = 归一化距离 × 0.75、到点挂到**那一格**、音高 = 1.0 + 0.5×该距离、声音走 cue 那一条 clip）；
   **素材侧没做**（`RewardAppearParticle` 还没进 `CardPresentation/Effects/` + 效果库）——
   那三步是 Editor 侧的活，见 §三·B。**没做的那条链会出声**（`[RewardWindow]` 一条警告，写清缺哪一步）。
   > 🔴 **2026-10-15 就地订正（铁律 5 · A425）—— 上面「素材侧没做」那半句【已收口】，而且它写的落点是错的：**
   > · 实况 **四步全齐**（① 重打进包 · ② prefab · ③ 效果库 · ④ 音频）：prefab = `MyGame/Assets/WarpforgeVFX/Prefabs/RewardAppearParticle.prefab`
   > （guid `9d4947584ac615e49a0b764f2aba470c`）· 效果库 `MyGame/Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 里
   > `- name: RewardAppearParticle` 那条指着它（库现读 **968** 条 = 963 原版 + 5 自制）· `Resources/Art/audio/sfx/Add card to deck.wav` 在盘
   > （`工具/import_original_sfx.py:57` 新增 `EXTRA_CUES = ["Reward open item by item"]`，窗口级 cue）。
   > · ⛔ **落点不是 `CardPresentation/Effects/`** —— 那是 `EffectLibraryBuilder.UserPrefabDir`、给**自制特效**用的；
   > `EffectExporter.ListedPrefabs` → `RunListed` 的真实出口 = `EffectExporter.PrefabDir = Assets/WarpforgeVFX/Prefabs`（`MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs:30`）。
   > · 判据全文 → `资料/普查产出_1015/R1a_A段F段表外_现核.md`「必须知道的三条硬结论」第 2 条 · `资料/历史/A表已收口_1015.md` §一 A425。
3. 🔴 **一处判据订正（铁律 5）**：`pitch` **不是随机区间** —— 反编译里写进 `+0x34` 的就是**同一个归一化距离**
   `fVar17`，随机源一处都没有。「越靠外 ⇒ 弹得越晚 **且** 音越高」。详见 §五·1。

---

## 二、改动清单（`文件:行号`，每处一句为什么）

### `Shell/BlinkGraphic.cs`（**新建**，197 行 + `.meta`：`guid 2a4010eabf204aa3a74cc67133e7d1ea`）

| 位置 | 为什么 |
|---|---|
| 文件头（`:1-60`） | 判据全文（`Update.c` 逐句 + 36 实例普查）+ **两处与原版的有意不同**（挂窗口根而不是挂目标节点 · 不抛 NRE 改出声） |
| `T()` `:118` | `Clamp01(\|cos(speed×clock)\|)` —— **只此一份**（原版 `Update` 第 ① 步逐字） |
| `AlphaOf()` `:125` | `Lerp(a, a×variation, t)` —— 原版第 ② 步（只改 alpha） |
| `Tinted()` `:132` | 新 alpha 填进原色，其余三位原样带过去（= 反编译里那个 `CONCAT44`） |
| `Bind()/Restart()` `:146/:163` | `Restart` = 原版 `Start()`：取原色 + 时钟归零、**当帧不写色**（自检有一条「建完还没 Tick ⇒ 颜色还是原色」盯着） |
| `Tick()` `:184` | 顺序照原版：**先按此刻时钟写色 → 算完才** `clock += dt`；目标不在 ⇒ 不写色不推时钟 + **出声一次** |

### `Shell/RewardWindow.cs`

| 位置 | 为什么 |
|---|---|
| `PunchDelayAt` `:301` / **新增 `PunchNormAt`** `:312` | 把那条归一化距离**单独抬出来**（原版 punch 的 `SetDelay` 与 A312 的延迟/音高读的是**同一个 `fVar17``）；`PunchDelayAt` 数值**逐位不变**（自检 5 处引用不动） |
| A312 常量块 `:322-396` | `ParticleOnAppear` `:379` / `SoundCueOnAppear` / `SoundClipOnAppear` / `SoundVolumeOnAppear` / `FxPitchBase·Span`，每条带出处与偏移 |
| A312 状态 + 可观测点 `:398-427` | `_fxAt/_fxFired/_fxPlayer` `:401` + `AppearFxPlayed/AppearSfxPlayed/AppearFxAt/AppearFxFired/AppearFxScheduled/AppearFxPitch`（自检要读的都在这里，⛔ 别去读私有字段） |
| `ScheduleAppearFx()` `:439` | 排期 = `PunchNormAt × PunchDelayMax`；**预览态不排**（`RewardWindow__Open.c:283`） |
| `ClearAppearFx()` `:454` | `Build()` 开头作废上一轮；**主动 `Kill()`**（不 Kill 的话它会随子件被 `DestroySafe` —— 那算不上收尾） |
| `TickAppearFx()` `:471` | 判据 = **当前时钟 ≥ 触发时刻**（不是「这一帧正好到」；自检的 `dt` 是一大步一大步给的） |
| `FireAppearFx()` `:484` | `MoveNext.c` state 1 的**同一拍两件**：`WarpforgeEffectPlayer.Play`（挂到 `_itemNodes[i]`）+ `WFSoundPlayer.Play`；粒子取不到 ⇒ **出声**(只一次) |
| `DetachLiveFx()` `:535` / `ReattachLiveFx()` `:551` | **我们这条管线独有的坑**：物品格每次 `Tick` 重建 ⇒ 不摘的话揭示收尾那一拍（t≈0.8s）会把已飞 0.75s 的粒子连节点一起销毁（静默）；`SetParent(x,false)` 往返 ⇒ local TRS 逐位不变 |
| `BlinkSpeed/BlinkVariation` `:603` | 改成**转发**公共件的出厂值（⛔ 不再各写一份字面量） |
| `_blink` 字段 `:631` / `TapLabel` `:627` | 公共件挂**本窗自己**的 GameObject 上（挂 `Tap Text` 上会随重建一起死 ⇒ 时钟会跟着断）；`TapLabel` 是给自检读的那颗字（= 原版 `graphic` 的落点） |
| `BlinkT/BlinkAlpha/BlinkClock` `:689/:691/:694` | 改成转调公共件；读数与改件前**逐位相同**（§九(f) 五条期望值一个都没动） |
| `EnsureBlink()` `:713` + `Create()` `:705` | 在 `Create()` 里先挂一次 ⇒「还没 `Build()` 就读 `BlinkT`」的路径不变档（时钟 0 ⇒ `\|cos 0\| = 1`） |
| `BlinkTick()` `:934` / `BindBlink()` `:946` | 只剩转发 + 绑定；`Restart()` 每次重建都调（= 原版 `Start()`） |
| `DetachLiveFx/ReattachLiveFx` 调用点 `:1170/:1213` | `BuildItems()` 的头/尾成对（删旧件之前摘、建好新件之后挂回） |
| `ScheduleAppearFx()` 调用点 `:1120` | 紧跟在 `StartPunches()` 后面 —— 原版那一圈循环里它俩就在同一处（`:167-174`） |
| 末尾那段 `Debug.Log` `:1152` | **就地订正（铁律 5）**：原写「还有 **2 处**没做」，其中 `particleOnAppear` 与 `soundOnAppear` 两条**这一批做掉了**；剩 `Reward Claim` **1 处**，且原句「只 `SetActive` 不播放」**不准确** —— 我们**连那个节点都没建** |

### `Shell/BoosterInfoPopup.cs`

| 位置 | 为什么 |
|---|---|
| 文件头 `:26-31` | 补上「那颗 `BlinkGraphic` 2026-10-12 接上了」（此前被画成静止） |
| `Blink` / `_blink` / `Update` / `Tick` `:261-270` | 消费方的三个口：可观测点（自检读 `Blink`）+ 帧循环（`Update`）+ 批处理直调（`Tick`） |
| `BindBlink(ImageQuad)` `:275` | 目标 = `bgQ`（`Artwork/background` 那颗 `Image` 的 quad）；`quad == null` ⇒ **解绑 + 静音**（那种情况上面刚报过缺图） |
| `BindBlink(bgQ)` 调用点 `:354` | `Build()` 里紧跟 `ArtFgNode` 之后（重开一件商品 = 时钟归零） |
| `Dump()` `:614` | 加一段「主图闪烁 时钟/t」（接上了才报，没接读「**没接**」） |

---

## 三、断言 / 待接线

### A · `Editor/RewardsScene.cs` §九(f)（`BlinkGraphic` 那一组）

**现有 5 条期望值一条都不用改**（读数逐位相同：`BlinkT`/`BlinkAlpha`/`BlinkClock` 都是同两条公式 + `Σdt`）。
建议**只加两条**（都盯**原版立即数**，⛔ 不读 `BlinkGraphic.DefaultSpeed`）：

| 建议位置 | 判据 | 改坏法 |
|---|---|---|
| §九(f) 开头（`rwB` 建完、`OpenWindow` 之后） | `rwB.GetComponent<BlinkGraphic>() != null` 且 `blinkSpeed == 1.0f` · `colorVariation == 0.5f`（期望值 = 原版 `.ctor` 的 `0x3f800000` / `0x3f000000`） | 把 `BindBlink` 里那两个赋值删掉 / 换值 ⇒ 红（组件取默认值时**恰好也是**这一对 ⇒ 顺带钉住「默认值没被改」） |
| 同段，紧接「`Tap Text` 不在了」那条之后 | `rwN.Blink.Target == null`（Unity 伪 null）**且** `AppearFx`-无关：`rwN.Blink.Clock` 停在 0.5 —— 已有的后一半就在那儿，只补前半条当**前提**（让人一眼看出「停住」是因为**目标没了**，不是因为别的） | 把 `BlinkGraphic.Bound` 改成只看 `_write != null` ⇒ 目标没了也照样推时钟 ⇒ 红 |

### B · `Editor/ShopScene.cs`（**新一段**：「卡包详情窗的主图会呼吸」）

夹具走既有那条路：`BoosterInfoPopup` 建出来 + `Show(page, idx)`（`ShopScene` 已有这一族夹具）。

| # | 判据（期望值 = **原版立即数 / 手算值**） | 改坏法 |
|---|---|---|
| ① | `pop.Blink != null`（接上了） | 删 `BindBlink(bgQ)` 调用 ⇒ 红 |
| ② | `pop.Blink.Bound`（目标在 = 原版那颗 `Image`） | 把 `Bind` 的目标换成 `transform`（窗根）⇒ `Target` 是活的但**没有 Image** ⇒ 这一条仍绿 ⇒ **所以它单独不算数**，必须配 ③ |
| ③ | 出厂那一档 α = 原色（`Restart()` **不写色**）：读 `Artwork/background/Image` 那个 `ImageQuad.Tint.a`，应等于 `Bind` 前记下的那一档 | 把 `Restart()` 改成也写一次色 ⇒ 时钟 0 那一档（0.5A）会盖掉出厂色 ⇒ 红 |
| ④ | `pop.Tick(0f)` 之后 α = **0.5 × 出厂 α**（`\|cos 0\| = 1` ⇒ `Lerp(A, 0.5A, 1)`；0.5 与原色都来自原版 ctor 立即数） | 把 `cos` 写成 `sin` ⇒ 这里 = 1.0× ⇒ 红 |
| ⑤ | `pop.Tick(Mathf.PI * 0.5f)` 那一拍写进去的仍是**推进前**那一档（0.5A）；再 `Tick(0f)` 才回到原色（`\|cos π/2\| = 0`） | 把 `_clock += dt` 挪到写色之前 ⇒ 红 |
| ⑥ | `CheckNear(pop.Blink.Clock, Mathf.PI * 0.5f, 1e-4f)` —— 时钟**只在 `Tick` 里**推 | 把 `Tick` 里那句删掉 ⇒ ④⑤⑥ 全红 |
| ⑦ | **相对断言**（一个常量都不引）：`Tick(0f)` 那一档必须**严格小于**出厂那一档 | 反向实现（`sin`）⇒ 红 |

⚠️ `Editor/ShopScene.cs` 里**别断「静止」**（那是旧行为的断言，今天已经不成立）。

### C · `Editor/RewardsScene.cs`（**新一段**：A312 的排期与音高 —— **今天就能验**）

夹具：非预览 + 若干条奖励；`dt` 一大步一大步给（`Tick` 的语义是「至少等这么久」）。

| # | 判据 | 改坏法 |
|---|---|---|
| ① | `rw.AppearFxScheduled == true`（非预览）；**预览态那一档 == false** | 把 `ScheduleAppearFx` 里的 `if (IsPreview) return;` 删掉 ⇒ 预览那一条红 |
| ② | **排期值 = 手算的原版式**（`count = 12`：`AppearFxAt(0) == AppearFxAt(11) == 0.75f` —— 归一化距离 1320/960 = 1.375 被 `clamp01` 到 1；`AppearFxAt(5) == 0.09375f` = 120/960 × 0.75） | 把 `× PunchDelayMax` 写成 `× 1` ⇒ 红；把 `clamp01` 删掉 ⇒ 第 0/11 格变 1.03125 ⇒ 红 |
| ③ | **到点才发**：`Tick(0.05f)` ⇒ `AppearFxFired(5)` 真、`AppearFxFired(0)` 假；再 `Tick(0.7f)` ⇒ 第 0/11 格真 | 把 `_animT < _fxAt[i]` 那半句删掉 ⇒ 第一拍就全发 ⇒ 红 |
| ④ | **只发一次**：连推几次后 `AppearFxFired` 不重复（配 `AppearFxPlayed == 0` —— 素材还没进工程，`Play` 返回 null，所以**今天**这一格读 0；素材落地后改成 `== 12`） | 删掉 `_fxFired[i] = true;` ⇒ 每帧都发 ⇒ 出声音频爆掉 |
| ⑤ | **音高（原版字面量手算）**：`AppearFxPitch(0f) == 1.0f` · `AppearFxPitch(0.25f) == 1.125f` · `AppearFxPitch(1f) == 1.5f` · `AppearFxPitch(2f) == 1.5f`（`clamp01`）· `AppearFxPitch(-1f) == 1.0f` | 把 `FxPitchSpan` 改成别的数 / 去掉 `clamp01` ⇒ 红 |
| ⑥ | **取不到素材要出声**（挂 `Application.logMessageReceived` 抓，写法照本文件 §② 那条 `border 比图还大`）：跑一次到点，应有一条 `[RewardWindow]` + 含 `RewardAppearParticle` 的 warning；**再推一拍仍然只有一条**（只响一次） | 把那条 `Debug.LogWarning` 删掉 ⇒ 红线「不许静默失败」⇒ 红 |

### D · **素材落地后**才能加的断言（**待接线**，记着做）

- `AppearFxPlayed == 格数`（每格一次）· `AppearSfxPlayed == 格数`。
- **重建不掉粒子**（`DetachLiveFx/ReattachLiveFx` 那对）：到点之后接着 `Tick` 到揭示结束（`_animT ≥ 0.8`），
  断言那一刻那个 `WarpforgeEffectPlayer` **还活着**且 `transform.parent == ListHolder 下那一格`。
  **改坏法**：把 `ReattachLiveFx()` 那行删掉 ⇒ 粒子留在窗根下（位置错）⇒ 断 parent 那半条红。
- 粒子**跟着那一格的 punch 缩放**（原版）—— `Tick` 到 punch 在飞的那一拍，读 `粒子.transform.lossyScale`。

### E · **只能真 Play 验的**（如实标注，给「怎么验」）

1. **整条链真跑一遍**：进游戏 → 日常领奖（`DailyData.CollectReward` → `RewardWindow.ShowCollected`）→
   看**逐格**的烟 + 听声音（越靠外音越高）。判据 = `资料/真Play待验清单.md` 那条（**归 A 组**）。
2. **粒子的尺寸/朝向**：我们**逐字保留**了 prefab 自己的 `localPosition (0,0,0)` · `localRotation (−1,0,0,~0)` ·
   **`localScale 14.838`**（`Transform_45891158476419445.json` 实读；`Play` 的 `scale` 形参我们传 1 ⇒ 只当乘数）。
   ⚠️ 导出成工程 prefab 之后这三个数**能不能原样活下来**，只有把 prefab 拉进场景看一眼才算数
   （本仓的已知坑：`Unity 导入器会静默改坏原版资产`）。
3. **`WaitForSeconds` 的 scaled 语义**：我们走 `Tick(Time.deltaTime)`（scaled）⇒ 与 `WaitForSeconds` 同基；
   `timeScale = 0` 时两边都停。真 Play 里拉一下 `Time.timeScale` 即可证。

---

## 四、其余 `BlinkGraphic` 实例清单（**36/36**，逐个带出处）

> 由本件的普查子代理实读（UnityPy 直读 `bundle_menus_assets_all`）。**36 个实例 → 36 个互不相同的根 prefab**
> （没有一扇窗里有两颗）。**`graphic` 指向谁**：**36/36 都指向「宿主 GameObject 自己身上那颗 `Graphic`」**，
> 类别 = `Image` 24（22 `background` + 2 `Glow`）· `TextMeshProUGUI` 11 · `EverguildTextMeshPro` 1。

### 4·1 「我们已经接了 / 本轮接了」的（2 条）

| 宿主 | 根 prefab | 父链 | 我们的状态 |
|---|---|---|---|
| `Tap To Continue` | **Reward Window** | `Reward Window / Content / Tap To Continue` | ✅ **已接**（A310⑤ 做的行为，本轮 A315 抬成公共件） |
| `background` | **Booster Info Popup** | `Booster Info Popup / window / Artwork / background` | ✅ **本轮接上**（`BoosterInfoPopup.BindBlink`） |

### 4·2 剩下 34 条（按族聚合；**哪一扇窗有会呼吸的件**）

| 族 | 条数 | 宿主 | 根 prefab（逐条） | 我们有没有这扇窗 |
|---|---|---|---|---|
| `General Basic Offer Popup *` | **20** | `background` 全 20 | `General Basic Offer Popup Booster_CardOrAltArt` · `…_AvatarORTitle` · `…_Cardback_Avatar_Title` · `… Just Foreground` · `… Variant 2 Currencies` · `… Variant Booster + 2 Currencies` · `… Variant Booster_avatar_cardback_title` · `… Variant Booster_avatar_resource` · `… Variant Booster_cardback_resource` · `… Variant Booster_title_resource` · `… Variant Deck_cardback_avatar` · `… Variant Expansion pass` · `… Variant Premium_Booster_avatar_cardback_title` · `… Variant Premium_Booster_avatar_cardback_title_resource` · `… Variant Premium_Premium_cardback_avatar` · `… Variant Premium_Resource` · `… Variant Premium_booster_title_avatarOrResource` · `… Variant Single Item Type` · `… Variant avatarOrTitle_resource` · `… Variant cardback_premiumOrAvatarOrResource_titleOrResource` | ❓**没查**（那是「商店 offer 弹窗」那一大族，**不在本件白名单**，也没在这条线上核过我们对到了哪一扇） |
| `Tap To continue` 那 9 条 | **9** | `Tap To continue` | `Base Event Popup` · `Base Expiration Popup` · `Event Help Popup Small Image Variant` · `Event Help Popup Variant` · `Full Screen Event Announcement Variant` · `Liveop Event Help Variant` · `Ranked Division Change Window` · `Tutorial Message Window PopUp` · `WelcomeScreen Alpha` | ❓**没查**（父链全是 `<prefab> / Texts / Tap To continue` —— 形态高度一致，**像是一族共用的小件**） |
| `Tap to close` | **1** | `Tap to close` | `Booster Pack Open Window` | ✅ 我们**有**这扇窗（`Shell/BoosterPackOpenWindow.cs`）—— ⚠️ 但**原版这一件是 `EverguildTextMeshPro`（不是 TMP）**，接之前要确认我们那边画的是不是同一颗字 |
| `Glow` | **2** | `Glow` | `Forge Tab` **与** `Rewards Base Submenu Variant`（同一棵子树 `… / Forge Tab / Ready for level up / Glow` 被两处各实例化一次） | ✅ 我们**有**（`Shell/ForgeTab.cs` 的 `Ready for level up / Glow`） |
| `background`（非 General Basic 族） | **1** | `background` | `Base Offer Popup` | ❓**没查** |

⇒ **能顺手接的 3 条**（`Forge Tab / Ready for level up / Glow` ×2 + `Booster Pack Open Window / Tap to close`），
**都在本件白名单之外**（`Shell/ForgeTab.cs` · `Shell/BoosterPackOpenWindow.cs` 都不是我独占的文件）
⇒ 按派单「能顺手接的就接，接不了的在报告里列成表（由调度台开账）」⇒ **本件没接，列在上面**。
🔴 **另开账时的一条硬前提**：`Forge Tab` 那两条是**同一段子树被两处实例化**
（`Forge Tab` 独立 prefab + 嵌在 `Rewards Base Submenu Variant` 里那个）—— 改的时候**两处都会受影响**。

### 4·3 宿主的「还有谁写 color」这一条（上一轮明说没查，本件补查）

- **22 个 `background`**：`RectTransform + CanvasRenderer + Image + BlinkGraphic` —— **除它之外没有任何 MonoBehaviour**。
- **2 个 `Glow`**：同上 **+ 一个 `Canvas`**（嵌套画布），没有别的 MonoBehaviour。
- **12 个文本宿主**：`… + Localize + BlinkGraphic + [ContentSizeFitter] + [EverguildTextController] + [LocalizationKeyChangerByPlatform] + [UIGenericEventCatcher]`。
  **查过方法体、确认不写 color 的**：`EverguildTextController`（只 `set_fontSize*`）·
  `LocalizationKeyChangerByPlatform`（只 `set_Term`）· `UIGenericEventCatcher`（全文件 `color` 零命中）。
  ❓ `ContentSizeFitter` / `Localize` **不在 `decomp_full`** ⇒ **会不会写 color：没查到**（序列化字段里没有 color 字段，但**那不是判据**）。
- ⇒ **我们这两处接的目标上都只有 `BlinkGraphic` 一个写色者**（`background` 那一族）⇒ **不会互相盖**。

---

## 五、没查清 / 订正

### 5·1 🔴 判据订正：`pitch` **不是随机区间**（铁律 5）

- **原文（V8 §A312 与派单）**：「`SoundManager.Play2D(soundOnAppear)` 且 **pitch ∈ [1.0, 1.5]**」+ 派单「**pitch 随机区间**」。
- **实读**（`RewardWindow._DelayedParticlePlay_d__22__MoveNext.c:52-58`）：
  `fVar8 = clamp01(*(float*)(param_1 + 0x34)) * 0.5f + 1.0f` —— 而 `+0x34` 的**唯一写入点**是
  `RewardWindow__DoRewardAnimation.c:172`：`*(float *)(lVar11 + 0x34) = **fVar17**`
  （`fVar17` = 那条归一化距离，**同一圈循环里 `+0x20 = fVar17 * 0.75` 就是延迟**）。
- **结论**：`pitch = 1.0 + 0.5 × clamp01(归一化距离)` —— **确定性、单调**，区间 [1.0, 1.5] 对，**「随机」不成立**。
  全 `.c` 里 `System.Random` / `UnityEngine.Random` 零命中。
- **落点**：实现按订正后的口径写（`RewardWindow.AppearFxPitch`）；订正痕迹留在 `RewardWindow.cs:345-361` 那段注释里。
- ⚠️ **V8 §A312 与派单的原文本件没改**（不是我的文件）⇒ 请调度台顺手订正，或在正本里指一句。

### 5·2 查不到的

- **`Reward Claim`（`claimRewardParticles` 0xB8）那 6 套粒子的逐字段参数表**：V8 §A312 ③ 已明说「没做」，
  本件**也没做**（不在派单判据里）。⇒ 那是「完全复刻」的欠账（铁律 11），**要做，判据 = 那 6 个
  `ParticleSystem_<pid>.json` + 6 个 `ParticleSystemRenderer_<pid>.json`**（都在
  `assets_full/bundle_menus_assets_all/`）。本件只是把**旧的那句错记录改掉了**（原写「只 `SetActive` 不播放」，
  实际**我们连那个节点都没建**）。
- **那些 `General Basic Offer Popup *` 20 条对应我们哪一扇窗**：本件没查（不在白名单里，也没在这条线上核过）。
- **`ContentSizeFitter` / `Localize` 运行时会不会写 `Graphic.color`**：没查到（两个类都不在 `decomp_full`；
  本机也没找到插件的 Managed DLL）。⚠️ **没查到 ≠ 不会写**。
- **`RewardAppearParticle` 的材质 shader 名字**：它的 `m_Shader` 是**外链**（`m_FileID = 20`），
  本件**没有**去解那份 externals 表定名字（`Alpha Masks Two Layer` 那一族**像**，但**「像」不是判据**）。
  本件不依赖它 —— 粒子走 `WarpforgeEffectBinder` 那条既有机制（在导出/生成效果库时解决），
  ⛔ 我没自己写第二个材质重建路。**若导出那一步报「shader 取不到」，要查的就是这个名字。**

---

## 六、顺手发现（**只报不改**）

1. 🔴 **`Editor/RewardsScene.cs:5854` 那句「我们 `BlinkTick` 现在逐字同序」今天仍然成立，但主语变薄了** ——
   次序现在在 `Shell/BlinkGraphic.cs` 的 `Tick` 里。那句注释指的是本仓的实现位置，**没有错**，
   但下一个人改公共件时要能顺着找过去（`RewardWindow.BlinkTick` 的注释里已写明指向）。
2. ℹ️ **`RewardsScene` 会在 `Tick` 里多打一条 `[RewardWindow]` 警告**（粒子取不到，只响一次/每扇窗）。
   本件核过：那一族的 `Application.logMessageReceived` 处理器都是**按内容过滤 + 进出成对**的窄块
   （`:3857` 抓 `border 比图还大`、`:6126` 抓 `[ItemDrawer]`），**不会**被这条误伤。
3. ℹ️ **`WarpforgeEffectLibrary` 在自检里今天就会被加载**：`Editor/ShopScene.cs:2124` 那条
   「翻牌播了粒子（`bp.PlayedFx.Count`）」走的就是 `WarpforgeEffectPlayer.Play` ⇒ 本件没有新增
   「自检第一次加载 958 个 prefab」的风险。
4. ℹ️ **`PopUpGameWindow.cs`（别人的活）已经带了手写的 `.meta`** ⇒ 新文件配 `.meta` 是本仓现行做法，
   本件照办（`BlinkGraphic.cs.meta`，guid 已全库查过无冲突）。
5. ℹ️ **`RewardWindow` 的 `_itemNodes` 与 `_cells` 是同一套下标**（`BuildItems` 里逐个 `Add`，`null` 也占位）
   —— A312 的粒子、`ApplyPunchScales` 的缩放都靠这条不变量。改 `BuildItems` 的排版循环时**别把这个性质弄丢**。
6. ℹ️ **`Shell/MissionRerollPopup.cs:399` 那条注释提到还有第 37 处 `BlinkGraphic`**
   （「原版这一格是 `BlinkGraphic` 会呼吸的那一枚货币图标 —— 我们**没有那张图**」）——
   它**不在**本件普查的 36 个里（那 36 个全在 `bundle_menus_assets_all`）⇒ 要么它属于**另一个包**，
   要么那条注释记的是别的东西。**本件没查**（不在白名单）⇒ 请调度台定：是补一次全库普查，还是就按「图没有、接不了」销账。
7. ℹ️ **`BoosterInfoPopup` 现在多了一颗 `Update()`**（此前它没有帧循环入口）。
   同族窗（`CampaignRewardWindow` / `RewardsWindow` / …）**都没有** ⇒ 这不是「统一收口」，是**按需加的那一处**；
   要收口的话，落点应该是 `GameWindow`（但那是共用件，本件不碰）。

---

*（报告完 · 写手代理 · 未跑 Unity 批处理 / 未动 git / 未改正本 / 只写了本文件 + §二 那三个源文件）*
