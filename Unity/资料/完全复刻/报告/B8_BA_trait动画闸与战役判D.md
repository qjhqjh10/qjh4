# B8_BA —— `TraitFrames.DisplayStatus` 的 `_hasInPlayAnim` 闸 + 4 条战役行判 `D`

> 执行代理 `B8_BA`，2026-10-26。白名单：`Core/TraitFrames.cs` · `工具/gen_trait_effect_bindings.py` ·
> `账/_parts/B8_BA.tsv` · 本文件。
> 判据出处一律带**哪一份反编译**：🥇 = `d:/2/tools/decomp_full/*.c` · 🥈 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`。

---

## 一、结论（先给六条）

1. **闸的位置 = 【调用点】、不是建表时跳过。** 原版在**调 `BattleCardUI.DisplayStatusAnim` 之前**先查 SO 字段
   `CardTrait._hasInPlayAnim`（`trait + 0x20`）：🥇 `CardScript__ActivateTraitAnim.c:12-25` ·
   🥇 `CardScript__AddEffect.c:645-663`（`cardState ∈ {2,3,15,17}` ∧ `GetTrait != null` ∧ `trait+0x20` ⇒ 才调）。
   而 `BattleCardUI__DisplayStatusAnim.c`（314 行）**全篇不读这个 bool**（只有 TriggerType 0 / 2 两支）
   ⇒ 原账「用绑定表有行代替闸」那条等价**不成立**，缺的确实是一道**播之前的查询**。
2. 🔴 **但召唤那条路没有这道闸** —— `CardScript__ActivateTraitsOnSummonOrEnchantment.c` 遍历
   `CurrentTraits(+0x128)` 逐条 `DisplayStatusAnim`，**全篇没有 `+0x20` 读取**；调用点 =
   `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:179` · `…_HeroLandIntoField_d__318__MoveNext.c:352` ·
   `BattleManager__CreateStartingTroop.c:59` · `BattleManager__SetPlayerDataAndWarlord.c:28` ·
   `BattleManager.__c__DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:16`。
   ⇒ 原版语义 = 「**后加到一张在场的卡上**的 trait 才看这个 bool；**单位落场时所有 trait 照演**」。
   ⇒ 我们的实现**不能一刀切**：只在「这张卡第一次同步之后新出现的关键词」上拦。
3. **改动已落地**（`TraitFrames.cs` 播放侧 + 生成器带列），**typecheck 0 / 0**，**行号零漂移**
   （`TraitFrames.cs` 仍是 678 / 812 / 921 / 961 / 973 / 1150 —— 账里 7 处 A 行引用的行号一个没动）。
4. **读数**：`inPlayAnim=false` 且**有可播 `Start(0)`** 的 **6** 条 ⇒ 修后从「会播」变「不播」：
   `armour`(`ArmourTraitStart`) · `companion`(`CompanionTraitStart`) · `flank`(`FlankTraitStart`) ·
   `pack`(`Pack OnPlay Anim`) · `rally`(`RallyTraitStart`) · `tide`(`TideTraitStart`)。
   另有 `firstStrike` / `piercing` 两条 `inPlayAnim=false` 也有 `Start(0)` 行，但 `effectAnim` 是**空引用**
   ⇒ 两边本来都不播（闸只让那句「本档无动画」的出声不再走）⇒ **8 条「有 Start 行」里 6 条有可观察差**。
5. **反向证明（没误关）**：`inPlayAnim=true` 且有可播 `Start(0)` 的 **5** 条 ——
   `cantAttack`(`CantAttackAnim`) · `fast`(`FastTraitStart`) · `ferocity`(`FerocityAnim`) ·
   `huntMark`(`HuntMark Stack Anim`) · `markerlight`(`MarkerlightTraitStart`) —— **一条都没被拦**。
   `inPlayAnim=false` 却有 **OnPlay(2)** 行的 = **0 条** ⇒ 今天「闸整次调用」与「只闸 Start(0)」**行为等价**。
6. **4 条战役行判 `D`**（`LandWarlordsInCampaign` · `TimedCampaignSetup` · `TimedCampaignStartSequence` ·
   `ShowHeroesIntroMessageCampaign`），依据 = 原版客户端**没有任何一处构造战役局**（0 处赋值 `MatchType.Campaign`）
   + `CampaignWindowTab.OnNodeClicked` 只开奖励窗 + 数据源在服务端。**带时间限定**（见 §二·B）。

---

## 二、逐条证据

### A · `_hasInPlayAnim` 闸

| # | 结论 | 判据（`文件:行号`） |
|---|---|---|
| A1 | 闸是**调用点前置查询**，闸的是**整次** `DisplayStatusAnim`（Start(0) 也进不去） | 🥇 `CardScript__ActivateTraitAnim.c:12`（`GetTrait`）`:23`（`if (*(char *)(lVar1 + 0x20) != '\0')`）`:25`（`DisplayStatusAnim`） |
| A2 | 加 trait 那一支同一道闸 | 🥇 `CardScript__AddEffect.c:645`（`cardState = *(int*)(this+0x228)`）`:652`（`GetTrait`）`:657`（`trait+0x20`）`:661`（`DisplayStatusAnim(ui, traitId, 1)`） |
| A3 | `DisplayStatusAnim` 体内**没有**这个 bool（只有 TriggerType 0/2 两支）⇒ 不是建表时跳过 | 🥇 `BattleCardUI__DisplayStatusAnim.c` 全 314 行，`+0x20` 只出现在 `:273-278`（那是闭包对象，不是 `CardTrait`） |
| A4 | 字段身份 = `CardTrait._hasInPlayAnim`（序列化序第 2 个，`+0x20`） | 🥈 `/d/2/Warpforge_code/Scripts/Assembly-CSharp/CardTrait.cs:26-28` · 🥈 `dump.cs:21327-21346` |
| A5 | 🔴 **召唤那条路没有闸** | 🥇 `CardScript__ActivateTraitsOnSummonOrEnchantment.c` 全篇 `grep "0x20"` = **0 命中**；它 `for` 遍历 `CurrentTraits(+0x128)` 逐条 `DisplayStatusAnim` |
| A6 | `+0x20` 在**另一个类**上是别的意思（别混） | 🥇 `CardTraitCollection__GetTraitListForCardIcons.c:64` 的 `*(char *)(local_28 + 0x20)` 读的是 **`CardTraitData._hide`**（`dump.cs:21553-21562`：name(0x10) / _trait(0x18) / _traitValue(0x1C) / **_hide(0x20)**） |
| A7 | 数据真值（`数据/游戏数据/trait_collection.json`，77 条 trait / 75 条 effect） | `inPlayAnim` true **39** / false **36** / null **2**；`Start(0)` 行共 13 条（其中 2 条空引用） |

**null 的 2 条怎么处理（任务点名要给的依据）**：`berzerk` / `dodge` —— 它们在 `trait_collection.json` 里
`traitConfig == null` **且** `traitEffects == null` ⇒ 判据 = `工具/export_trait_collection.py:651`（默认 `"inPlayAnim": None`）
与 `:686`（有 SO 才写 `t["hasInPlayAnim"]`）。⇒ **null 不是「字段读不到」，是「这条 TraitPairs 根本没有 CardTrait SO」**；
它们一条 effect 都没有，闸对它们**没有行为影响**。生成器把 null 写成 `false`（JSON 里 bool 不能是 null），
消费端「表里没有这个 trait 的行 ⇒ **不拦**」，两种口径最终都不播，**结论一致**。

**实现（我们这一侧）**：
- `TraitFrames.DisplayStatus` 入口：`if (key.Length == 0 || !InPlayAnimGate(key)) return false;`（`:816`，**行内改，没加行**）。
- `InPlayAnimGate`：`_syncedOnce == false`（= 这张卡的第一次同步 = 落场那一拍）⇒ **放行**；否则问 `PlaysInPlayAnim(key)`。
- `_syncedOnce` 在 `SyncKeywords` 里置真（`:779`，同样**行内**）。
  ⚠️ **为什么不是「`_prev.Count == 0`」那种判法**：一个**本来没有关键词**的单位后来获得 `armour` 时，`_prev` 也是空的
  ⇒ 会被误判成「落场」⇒ 又变回多播。所以用一个显式布尔。
- 数据缺列（旧的 `trait_effect_bindings.json`）⇒ `ScanInPlayAnim` 出声一次 + **恒放行**（⛔ 不静默改成「全不播」）。

### B · 4 条战役行判 `D`

**共用证据（三条腿，都能复核）**：
1. **原版没有构造战役局的代码**：`grep -rnE "[^=!<>]= *MatchType\.Campaign" cpp2il/out_cpp2il_dev_ilspy` = **0 命中**；
   全树 26 处 `MatchType.Campaign` **全是读取/比较/switch**（`BattleManager.cs:15361/16523/18352/19613/35215/35222/36168/36185/42796/42853/42860/42998/43116` ·
   `BattleNetworkManager.cs:160/406` · `MatchData.cs:416/500/507/972/979/998/1068/1130` · `PlayerDataManager.cs:9774/9789` ·
   `SupportMethods.cs:3166`）。
2. **点战役节点只开奖励窗**：🥈 `CampaignWindowTab.cs:676-720` 的 `OnNodeClicked` 只组 `Action<RewardTier>` / `onCollect`
   回调（`premiumRewardsCollected` / `baseRewardsCollected` / `RewardTier`），**没有** `LoadScene` / `StartBattle` / `MatchType` 写入
   —— **与原版一致**，我们 `Shell/CampaignTab.cs:1148`（`OnNodeClicked → ClickNodeForTest`）`:1153-1181` 也是开
   `CampaignRewardWindow`（账 `战役.tsv:95` 已按此判 A）。
3. **数据源在服务端**：`LandWarlordsInCampaign` 的唯一调用方是 `AiScripted.cs:1978`
   （脚本动作 `ScriptedActionType.LandWarlords(160)`），其入参 `ScriptedActionCampaignData` 来自
   `WarlordPathData`（`判据/战役开局_判据与落点.md` §2·7 三条取证：35,014 份 MB 里 0 命中 · 只有
   `bundle_campaignrewardbackgrounds` 一个战役包 · 是 LiveOps 服务端载荷）⇒ 属 `P3`。

**我们侧同证据**：`RuleEngine/Core/RuleCore.cs:13587` 与 `RuleEngine/Core/BattleContext.cs:915` 两处注释已写明
「我们当前没有战役关（`GameMode.Campaign/CutScene` 无开战入口）」；`BattleContext.cs:2096` 只有
`case GameMode.Campaign → MatchType.Campaign` 的**派发表项**（不设 `PlayMode`）；`BattleDriver.SetPendingPlayMode(...)`
的全部调用点 = `Classic / Skirmish / Tutorial / Replay / ClosedDeck`
（`BattleDriver.cs:4681` · `Editor/BattleScene.cs:18206/18214/18262/18289` · `Shell/DraftModeWindow.cs:2291`）。

**⚠️ 措辞订正（铁律 5，就地写清）**：派单里的「**全仓 0 处写 `GameMode.Campaign`**」按字面**不成立** ——
`BattleContext.cs:2096` 有一处 `case GameMode.Campaign:`（派发表）。准确的表述是
「**全仓 0 处把 `PlayMode` 设成 `GameMode.Campaign`（= 0 处开战入口）**」。账的 `差异` 列按派单原话写，
本条订正写进 `备注`。

**时间限定**（已写进 4 行的 `差异` 列）：若日后发现战役关入口（原版覆盖只约 29% 的方法体，且战役局可能由
服务端 `MatchData` 载荷下发），本 4 行需重开。

---

## 三、改了哪些文件 / 哪些行（新增行数）

| 文件 | 改动 | 行数变化 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Core/TraitFrames.cs` | ① `BindingRow`：`public bool playIfWarlord, inPlayAnim;`（:153，**并到一行**）② `TraitEffectBinding`：`public bool PlayIfWarlord, InPlayAnim;`（:226，注释同一行内改）③ `EnsureBindings`：`PlayIfWarlord = r.playIfWarlord, InPlayAnim = r.inPlayAnim,`（:378）④ `SyncKeywords`：`_prev.Clear(); _syncedOnce = true;`（:779）⑤ `DisplayStatus` 入口：`if (key.Length == 0 \|\| !InPlayAnimGate(key)) return false;`（:816）⑥ **类尾新增一段**（:1204-1302：判据注释块 + `_syncedOnce` + `_inPlayAnimScanned/_inPlayAnimKnown` + `PlaysInPlayAnim` + `ScanInPlayAnim` + `InPlayAnimGate`） | **+98 行，全在 1203 之后**（文件 1205 → 1303）；**上面 ①–⑤ 全部行内改，0 行增减** ⇒ 账里 7 处 A 行落点（678/812/921/961/973/1150-1166）**零漂移** |
| `Unity/工具/gen_trait_effect_bindings.py` | ① 列说明表加 `inPlayAnim` 一行 ② 每行新增 `"inPlayAnim": t.get("inPlayAnim") is True` ③ 按 **trait** 汇总 `stats.inPlayAnim{true,false,null}` / `inPlayAnimNullTraits` / `inPlayAnimFalseWithStartPrefab` ④ `conventions.inPlayAnim` 说明闸口径 ⑤ 打印一行 `[inPlayAnim] …` | +33 行（278 → 311；工具，无行号约定） |
| `账/_parts/B8_BA.tsv` | 11 列表头（照抄 `战斗逻辑驱动.tsv` 第一行）+ **5 行**（1 行 B：`ActivateTraitAnim`；4 行 D：战役） | 新文件 |
| 本文件 | 报告 | 新文件 |

**为什么字段要并到同一行**：`TraitFrames.cs` 被账按行号引用（`战斗界面_HUD.tsv:109/117/118/131/146/156/162` ·
`特效动画.tsv:233/414`）。加一行就会让这些 A 行整体漂移（CLAUDE.md §三 2026-10-25 `N9` 的实测代价：
`check_ledger_points` 报 10 条漂移 + 2 条失败）。所以 `inPlayAnim` 这个 bool **并到 `playIfWarlord` 同一行**声明，
`DisplayStatus` 那句也是行内替换。

---

## 四、没查清的部分（⛔ 没拿猜测填空）

1. **`ActivateTraitsOnSummonOrEnchantment` 里那个 `param_2`/`param_3` 的确切含义**：`param_3` 传进
   `DisplayStatusAnim` 的 `playSound` 位很清楚；`param_2` 只在「cardState 不在那四档」时决定要不要
   `CustomDebug.LogError`（`:60-62`）。我按「召唤/附魔调用者传 1、`AddEffect`/`AddEnchantment` 传 0」记录，
   **没有**证明它是语义开关。
2. **「第一次同步 = 落场」这条近似的边界**：如果某个单位**在手牌里就被 `SyncBoard` 同步过一次**
   （现行唯一调用点 `BattleDriver.cs:25508` 在棋盘那一圈里，看起来只对棋盘格跑），那它落场时会走「已同步」分支 ⇒ 会被闸。
   本轮没去逐行证「`SyncBoard` 只遍历棋盘格」；⛔ 这条**列为待核**，不写成结论。
3. **`PlayerDataManager.cs:9774/9789` 那两个 `MatchType.Campaign`**：从上下文看是「读 matchData 后按模式分支」，
   没读到构造点；本机反编译覆盖约 29%，**「0 处赋值」只能说明「已反编译的那部分没有」**。
4. **`introStageStepTime` 的运行时值**（继承 `B8_Y` 的未查清项）：`VarsGlobal` 是 SO，值在 prefab/SO 实例里，
   本轮没取（战役行已判 D，暂不影响）。
5. **`inPlayAnim` 在「卡牌面/收藏」那里有没有第二个消费点**：本轮只在 `TraitFrames` 一侧落地；
   `CardTrait.HasInPlayAnim` 属性在原版还有别的读者（`GetTraitInPlayListForIcons` 之类），我们那边走的是图标逻辑，
   **没查**（不在本批白名单，也不在本批派单里）。

---

## 五、顺手发现（交调度台分流）

1. 🔴 **`B8_AP` 那条「反向 = 0 条」的括注写反了**（`报告/B8_AP_卡牌面与卡组编辑剩余B.md:195`）：
   它写「`inPlayAnim=false` 却**没有** OnPlay(2) 行的 = 0」，实测是「`inPlayAnim=false` 却**有** OnPlay(2) 行的 = 0」。
   这个方向很要紧：正因为**没有** false-trait 带 OnPlay 行，「闸整次调用」与「只闸 Start(0)」今天**行为等价**
   ⇒ 我们照原版闸整次调用不会多关任何东西。
2. 🔴 **两个类共用 `+0x20` 偏移**（见 §二 A6）：`CardTrait._hasInPlayAnim` 与 `CardTraitData._hide` 都是 `+0x20`。
   以后按「`+0x20` 是个 bool」搜反编译时**必须先确认结构体**，否则会把 `Hide` 当成「有没有 in-play 动画」
   （`CardTraitCollection__GetTraitListForCardIcons.c:64` 就是 `_hide`）。
3. `CardTrait` 的两个近义字段（`_hasInPlayAnim(+0x20)` 与 `_isInPlayIcon(+0x22)`）在原版**分工不同**：
   前者管「播不播状态帧」，后者管「在场图标」。我们目前只消费前者，后者**没接**（若账里有对应行，按各线口径处理）。
4. `Editor/TraitEffectAssert.cs:597`（`tf.DisplayStatus("armour", 3)` 期望 `UpdateValueCalls +1`）与
   `WarpforgeArena1/Editor/AnimFXCheck.cs:1396`（armour 的 Start(0) 再调一次又播一次）**不会因本次改动变红** ——
   它们都在**刚 `Attach` 出来的实例**上直接调 `DisplayStatus`，`_syncedOnce` 还是 `false`（= 落场那一拍）。
   但这**正是**要请调度台在同步点确认的事（见 §六）。

---

## 六、给调度台的请求

1. **重跑生成器（这一步必须做，否则闸不生效）**：
   ```
   "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_trait_effect_bindings.py
   ```
   ⚠️ 现在是 `--check` = `rows 与判据一致：False`（exit 3）：现有 `Resources/trait_effect_bindings.json`
   **还没有 `inPlayAnim` 这一列**（本代理按派单**不跑写产物的生成**）。
   干跑（写到 `d:/4/_tmp_view/B8_BA_dry_bindings.json`）已验证：75 行全带该列，
   `stats.inPlayAnimFalseWithStartPrefab` 正好是那 6 条，`stats.inPlayAnim` = `{true:39,false:36,null:2}`。
2. **同步点断言（我不自证、不自己跑）**：`B8_AP` §四 C 那两条（`!PlaysInPlayAnim("armour")` / `PlaysInPlayAnim("stealth")`），
   建议再加一条**灭自证**：`PlaysInPlayAnim("flank") == false` **且** `PlaysInPlayAnim("fast") == true`
   —— 这两条**结构上不可能同时**由一个「一刀切」实现满足。
3. **复跑面**：只动了 `Core/TraitFrames.cs`（表现层共用件）+ 一个纯工具脚本 ⇒ 按铁律 12 判据，建议同步点跑
   **`BattleScene.Run`**（唯一会走到 `TraitFrames` 的宿主；`AnimFXCheck` ⑧ 就在它里面）。
   `RuleEngine/**` 一行未动 ⇒ 指纹预期不变（本轮**没跑** `ruleprobe check`，避免与其它代理的 dotnet 构建并发）。
4. **账**：`_parts/B8_BA.tsv` 已按 5 个键逐条核过主表存在（`战斗逻辑驱动.tsv:480/661/688/689/765`），
   `check_patch_conflicts.py` = 「没有跨补丁重复键」，`check_ledger_points.py` 对本补丁 = A 0 条 / 失败 0。
   合并时请 **`--only B8_BA`**。
5. **报告级订正请转达**：`报告/B8_AP_卡牌面与卡组编辑剩余B.md:195` 那句括注写反（§五·1）；
   `判据/战役开局_判据与落点.md` §一那 4 条 `C` 已由本批改判 `D`（该文件不在我白名单，未改）。

---

## 七、本批自查命令与输出（照抄）

```
$ TMPDIR=/tmp/wf_b8ba bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0

$ python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py d:/4/Unity/资料/完全复刻/账/_parts/B8_BA.tsv
合计：A 0 条 · 通过 0（其中告警 0 条）· 漂移 0 · 失败 0

$ python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py
✅ 没有跨补丁重复键（2 份补丁）

$ "D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_trait_effect_bindings.py --check
[check] rows 与判据一致：False          # ← 预期：产物还没有 inPlayAnim 列，等同步点重跑生成器
exit=3

$ 干跑（OUT 改到 d:/4/_tmp_view/B8_BA_dry_bindings.json，不写工程产物）
[out] ..\_tmp_view\B8_BA_dry_bindings.json  31434 B  rows=75  withPrefab=68  unresolved=7
[stats] effects=75 byTriggerType={'6': 7, '1': 6, '0': 13, '3': 21, '2': 18, '5': 10} guidChecked=68 guidMismatch=0 overrides=10
[inPlayAnim] true=39 false=36 null=2  闸关掉后不再播 Start(0) 的 6 条=armour:ArmourTraitStart, companion:CompanionTraitStart, flank:FlankTraitStart, pack:Pack OnPlay Anim, rally:RallyTraitStart, tide:TideTraitStart
[inPlayAnim] null（TraitPairs 里没有 CardTrait SO）= berzerk, dodge
```

**行号零漂移自检**（改完现读）：

```
$ grep -n "public static TraitFrames Attach(CardView card)\|public bool DisplayStatus(string trait, int value)\|public bool DisplayTrigger(string trait\|public void Remove(string trait)\|public void Clean(ICollection<string> stillHas\|public int MarkerLightUsed(string trait)" TraitFrames.cs
678:        public static TraitFrames Attach(CardView card)
812:        public bool DisplayStatus(string trait, int value)
921:        public bool DisplayTrigger(string trait, bool isWarlord = false, int value = NoTraitValue)
961:        public void Remove(string trait)
973:        public void Clean(ICollection<string> stillHas, bool clearAll)
1150:        public int MarkerLightUsed(string trait)
```
⇒ 与账里引用的 `678 / 812 / 921 / 961 / 973 / 1150-1166` **逐一相同**（零漂移）。
