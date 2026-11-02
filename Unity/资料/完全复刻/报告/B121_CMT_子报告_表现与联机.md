# B121-CMT-C · 表现 / 输入 / 联机块 —— 功能级注释现状普查

副报告（只读）。本块扫描了 **49 个在册 .cs / 50,708 行**（Hand 2 · Core 在册 25 · Board 3 · Effects 8 · Net 11），另抽查相邻文件 17 个（Battle/ 的 ReplayStore·ReplayBar·BattleSfx·BattleMusic·AnimFXController·BattleHudAnim·BattleEventSystem·ArenaRuntimeLoader；Core 的 TraitFrames·TraitParticles·TraitUnreachable·Badges·CardIcons·CardFaceRules·AudioSourceMixerGroupFixer·VoiceWaveDriver·VoiceWaveMaterial）。

**一句话结论**：这一块**不是"有没有注释"的问题，注释已经很厚** —— 文件头 49/49 全有，690 个公开方法零注释只占 6.4%。真正该补的是**少数"一整族公开 API 没有一句话交代"的簇**（主要在 `Net/NetTransport.cs` 的候选连接族），以及 7 处 `TODO(谁定的)` 半成品标记。

## 一、现状量化

| 目录 | 文件 | 行数 | 注释行 | 注释密度 | 有文件头功能说明 |
|---|---|---|---|---|---|
| `Hand/` | 2 | 3,649 | 1,979 | 54.2% | 2 / 2 |
| `Core/`（在册 25，不含 Loc/CardIcons 等数据表） | 25 | 35,084 | 17,416 | 49.6% | 25 / 25 |
| `Board/` | 3 | 1,403 | 823 | 58.7% | 3 / 3 |
| `Effects/` | 8 | 1,893 | 568 | 30.0% | 8 / 8 |
| `Net/` | 11 | 8,679 | 3,578 | 41.2% | 11 / 11 |
| **合计** | **49** | **50,708** | **24,364** | **48.0%** | **49 / 49** |

方法级：在册 38 个主要文件里，签名同行以 `{` 结束的**公开方法 690 个 ⇒ 零前置注释 44 个（6.4%）· 1–2 行 240 个（34.8%）**。

密度最低三个在册文件 = `Effects/ShakeAndSoundObject.cs` 18.7% · `Net/NetConfig.cs` 24.2% · `Effects/UIEffectTogglerController.cs` 25.7%（正文多是逐行照搬原版字段，判据都在文件头）。

**Top 10 大文件**（行数 / 注释行 / 密度 / 文件头行数）：

| # | 文件 | 行数 | 注释行 | 密度 | 头 |
|---|---|---|---|---|---|
| 1 | `Core/CardView.cs` | 16,309 | 7,624 | 46.7% | 20（含层结构 ASCII 图） |
| 2 | `Core/CardFeel.cs` | 4,461 | 2,387 | 53.5% | 20 |
| 3 | `Core/VfxMap.cs` | 4,292 | 2,444 | 56.9% | 19 |
| 4 | `Hand/CardInteraction.cs` | 2,584 | 1,384 | 53.6% | 12 |
| 5 | `Net/NetBattle.cs` | 2,283 | 1,086 | 47.6% | 30 |
| 6 | `Core/EventTiming.cs` | 1,546 | 885 | 57.2% | 48 |
| 7 | `Core/CardArt.cs` | 1,424 | 656 | 46.1% | 10 |
| 8 | `Core/Tooltip.cs` | 1,354 | 596 | 44.0% | 53 |
| 9 | `Net/NetTransport.cs` | 1,306 | 436 | 33.4% | 13 |
| 10 | `Net/NetSession.cs` | 1,186 | 435 | 36.7% | 12 |

## 二、七条链的主入口 + 注释是否说清「这是什么 / 对应原版哪个类」

| 链 | 主入口（文件:行） | 说清了？ |
|---|---|---|
| **卡面渲染** | `Core/CardView.cs:2209 Create` → `:4251 Build` | ✅ 文件头 `:1-20` 画了层结构，`Create` 的 6 个 `<param>` 逐条带原版判据 |
| **手牌交互** | `Hand/CardInteraction.cs:872 PickUp` / `:975 UpdateDrag` / `:1380 Release`；`Hand/HandLayout.cs:375 TryLastPose` | ✅ `HandLayout.cs:10-14` 明写「原版 `CardsHorizontalLayout` 的 2D 移植 + 权威实例 MB_5271」；`CardInteraction.cs:1-12` 写清 指针→悬停→拖拽→落位/回弹 |
| **部署** | `Board/DeploySequence.cs:237 / :249 Play`；`Board/BoardLayout.cs:1-42` | ✅ `DeploySequence.cs:1` 头明写「原版五段：飞 → 等 0.1 → 换 3D 体 → 悬停 → 落下 → 尾拍」，逐段带 `MinionManager__PlayMinionFromHand.c` 行号 |
| **攻击动画** | `Core/CardFeel.cs:1121 MeleeAttack` · `:1204 HitReact` · `:1887 ReturnToHand` · `:2065 HandToBoard` · `:2450 DeathExplosion`；`Core/VfxMap.cs:1-19` | ✅ 头写「六件事 + 参数从原版抄」；`MeleeAttack` 摘要标了「段1～段5」并给 `.rdata` 常量地址 |
| **音效** | `Core/WarpforgeAudio.cs:44 Ensure`（mixer）· `Core/VoiceLines.cs:224 EnsureExactLoaded`（单位语音）· `Core/AudioSourcesController.cs` · `Effects/UIEffectTogglerPlaySound.cs` · `Battle/BattleSfx.cs`（时机→cue） | 🟠 **文件级全说清，方法级缺 3 处摘要**（见 §三 5/6） |
| **回放** | `Battle/ReplayStore.cs`（1,031 行）· `Battle/ReplayBar.cs` | ✅ 头 `:1-6` 明写「原版整套在服务器（PlayFab CS 430/440/620/650），本地录不了 ⇒ 用户 2026-09-27 拍板自己做 —— **这是加功能、不是复刻**」 |
| **联机** | `Net/NetMatchmaking.cs:560 TryStart` · `Net/NetSession.cs:210 Pump` · `Net/NetBattle.cs:234/247 Attach` · `Net/NetApply.cs` · `Net/NetTransport.cs:376/883/1041` | 🟠 **文件级很强（NetSession 头画了完整状态机），公开方法级最弱**：`NetTransport.cs` 9 个公开方法 0 注释 |

七条链里唯一"没有原版可对照"的是联机（自建 P2P）—— 它不引"原版"是**对的**，不是缺注释（`NetTransport.cs` 全文只 7 次"原版" = 0.54/100 行）。

## 三、反例：最该补注释的 10 处

> ⚠️ **每处都必须【零行数变化】**：就地改写该处**已有的**注释行，或追加到**类尾**；⛔ 别在方法签名上方新起 `///`（`check_ledger_points` 会报整片行号漂移）。

1. `Net/NetTransport.cs:742 / :766 / :775 / :813` —— `SendToPendingReplacement` / `PumpPendingReplacement` / `PromotePendingReplacement` / `RejectPendingReplacement` **四个公开方法零注释**。这段其实是「**重连时把新连接收作待替换候选**」的整条生命周期：写候选 → 取候选帧 → 提升为当前连接 → 作废候选。**本块最该补的一簇**。做法：确认这四行不是账的 A 档落点后，在各自**行尾**追加 `// 候选连接替换族（第 N/4 步）：…`；若命中 A 档，改到上一行已有注释。
2. `Net/NetSession.cs:462 HandlePendingFrame`（136 行，内联 5%）—— 候选连接握手处理器：`Challenge → Proof → Ack → Reconnect{sessionToken}` 四跳。做法：改写 `:464` 的 `switch` 前一行（若无注释则并进 `:460` 的 `///`）。
3. `Core/CardFeel.cs:3375 DealIn`（发牌入场：从牌库位置淡入 + 位移，**没有 `<summary>`**，做法：就地扩写 `:3381` 那句「alpha 要当场置 0」）· `:3150 Dissolve`（阵亡溶解主入口：上浮 + 缩 0.85 + 材质溶解，失败退回整卡淡出，做法：就地扩写 `:3160`）。两处都只有体内零散 ⚠️。
4. `Core/CardView.cs:4251 Build` —— **16,309 行里最核心的方法**（按层建整卡的视觉树），本身无摘要（上一行 `:4245` 讲的是分段计时开关）。做法：把 `:4245` 改写成「`Build` = 按层建整卡视觉树；下面是分段计时…」；⛔ 别插新行（`Build` 是 A 档热点落点）。
5. `Core/WarpforgeAudio.cs:189 / :199 / :208` —— `SetMusic` / `SetSoundFx` / `SetVoiceOver` 三个公开音量入口零摘要。做法：并进 `:185-187` 那段已有注释。
6. `Core/VoiceLines.cs:224 EnsureExactLoaded`（71 行，内联 4%）—— 逐卡精确语音表的懒加载（`card_vfx_by_card.json` 的 `cards[].audio`），失败只试一次。做法：扩写 `:227` 的行尾注释。
7. `Effects/UIEffectTogglerImage.cs:54 / :134 / :150` —— `Capture` / `ToggleGameObjects` / `RefreshTargets` 零注释，**`ToggleGameObjects` 与基类 `Toggle` 的区别（只切 active vs 带时长补间）一句话都没有**。做法：`ToggleGameObjects` 行尾追加。
8. `Net/NetBattle.cs:1289 OnLocalResign` / `:1471 Tick` —— 认输发包、重连倒计时泵，两个公开入口零注释（同类 `:1222 OnLocalAction` 有 1 行）。做法：行尾追加。
9. `Core/CardArt.cs:29 Load` —— 原版美术的一次性懒加载（`Backdrop` + `Available`），而 `Available` 决定自检/HUD 走哪条路。做法：并进 `:26` 已有 `<summary>`。
10. `Core/EventTiming.cs:1172 SourceOf`（114 行，内联 20%）—— 「这条事件那一格时长出自哪条判据」的查询口，是自检排错入口。做法：并进 `:1169 IsSourced` 的摘要。

## 四、正例（好注释原文）

**① `Hand/HandLayout.cs:10-14`** —— 文件头把「这是谁的移植 + 权威实例在哪」一次说死：
```
// HandLayout.cs — 手牌扇形布局（原版 `CardsHorizontalLayout` 的 2D 移植）
// **数值不是拍的，是从原版序列化数据里搬的**，字段名和原版一一对应，方便回头对账。
//   类定义：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardsHorizontalLayout.cs`
//   权威实例：`d:/2/解包整理/07_场景/battlearena1/MonoBehaviour/MonoBehaviour_5271.json`
```

**② `Core/CardFeel.cs:1116-1120`** —— 方法级：几段、每段回哪、参数语义、⛔ 别写第二份推断：
```
/// <summary>近战攻击的整条序列（段1～段5）。`endPos` 由 <see cref="MeleeEndPos"/> 算好传进来；
/// `home` = 出手前那个**静止位**（局部坐标）—— 段4 回的就是它，**不是出手那一帧的位置**。</summary>
/// <param name="isPlayer">有没有「我方」—— 原版是卡自己的 `isPlayer` 字段（`CardScript+0x40`）。</param>
```

**③ `Board/DeploySequence.cs:243-247`** —— 原版五段时序直接写成一行时间轴：
```
/// <summary>同 <see cref="Play(CardView, Vector3, Vector3, float, Action)"/>，
/// 🆕 带 0.95 s 的**交接**（`T05(c)(d)`）：0.3 飞 → 0.65 等 → **0.95 `handoff.Apply()` 换成 3D 体** →
/// 0.05 悬停 → 0.2 `DOLocalMove`（竞技场局部坐标）→ **0.1 落地尾拍** → 1.3 `onDone`。
```

**④ `Net/NetSession.cs:500-503`** —— 一句话交代「为什么这里必须置 null」，把安全边界钉在代码旁：
```
// 🔴 **2026-10-20（W58-S6FIX · 安全边界）**：候选**永远不**在 `Ack` 里拿到 `sessionToken`。
//   重连的客机本来就有它；下发只会让「知道密码 / 空密码房间」的候选白拿这把钥匙 ⇒ 等于把替换门槛降到零。
```

**⑤ `Core/CardText.cs:348-356`** —— 把「为什么不能只做纯文本比对」的踩坑写进代码：官方正文写 `[[frenzied]]`、渲染出来是 `Destroyer`（`Card_Trait/*`），**纯文本比对只看得到 `frenzied` ⇒ 会把同一枚关键词再补一遍（卡面印两遍）**。

## 五、完全没有文件头注释的文件

**`Hand/` `Core/`（在册部分）`Board/` `Effects/` `Net/` 合计 49 个 .cs —— 0 个缺文件头。** 唯一"薄"的是 `Core/Driver.cs`（2 行），但那 2 行正好说清它为什么存在（`A1506`：MonoBehaviour 类名必须等于文件名）。
**头最薄（< 10 行）的 8 个**，属"能补但不紧急"：`Core/CardDraggingController.cs:1-9` · `Core/CardPreview.cs:1-9` · `Core/CardEffects.cs:1-9` · `Core/CardArt.cs:1-10` · `Core/CardTween.cs:1-10` · `Net/NetApply.cs:1-8` · `Net/NetRuntime.cs:1-8` · `Net/NetConfig.cs:1-10`。共同点：都是"拆文件 / 薄壳"产物，真说明在宿主文件里（如 `CardPreview` 的全部内容就是「别把次要类写进别的文件」）⇒ **优先级排在 §三 之后**。

## §顺手发现

1. **7 处 `🔶 自己设计（非原版）` 缺「谁定的 + 日期」，留的还是 `TODO(谁定的)`**，违反 `CLAUDE.md` §18 三样俱全：`Hand/HandLayout.cs:299` · `Core/CardText.cs:76/:82/:84/:809` · `Net/NetProtocol.cs:166` · `Net/NetSession.cs:24`。样板已存在于 `Board/BoardLayout.cs:230`（"项目 2026-10-01 定 · 2026-11-02 补齐标记"），照它就地改写即可（零行数）。
2. **只数 `//` 会低估两个文件**：`Hand/HandLayout.cs:156-209` 与 `Board/BoardLayout.cs` 把参数判据写进了 `[Tooltip("…")]`（含原版字段名与单位）—— 那是真判据，但按行首 `//` 数不出来。以后做注释度量要把 `[Tooltip]`/`[Header]` 的字符串算进去。
3. **`Net/*` 的"原版引用率低"是特性不是缺陷**：`NetTransport.cs` 0.54/100 行、`NetSession.cs` 1.69/100 —— 整套是我们自建 P2P（原版走 Photon），没有原版可引。做全仓引用率排名时把 `Net/*` 单列，⛔ 别让它拉低整块分数。
4. **本块没有阵营专属逻辑**：`grep` `faction ==` / `army ==` / `case "Sororitas"` 在 `Hand/ Core/ Board/ Effects/ Net/` **0 命中**。⇒ 用户举例的「这段是修女阵营祈祷功能的实现」**不属于这一块** —— 它该在 `Battle/BattleDriver.cs`（那里确有 `:149 FaithFaction`、`:34663 trait 1185 pray`）与 `RuleEngine/`。本块对应的是**通用机制**：`Core/HuntMarkController.cs`（trait 1260）· `Core/MarkerlightController.cs`（trait 1170）· `Core/TraitFrames.cs` · `Core/TraitParticles.cs` · `Core/Badges.cs`，五个文件头都写清了对应哪个原版类。
5. **名字易混**：`Core/CardEffects.cs`（卡牌表现↔特效库的**连接点**，205 行）与 `Effects/` 目录（原版 `UIEffectToggler*` 五个组件的等价物）是两件事，两个头都写清了，但搜索时容易串。
6. **"公开 API 无注释"在本块是成簇的**：`NetTransport.cs` 9 个公开方法 0 注释 = 全块 44 个零注释方法的 20%，且同属"候选连接替换"一族 ⇒ 一次补齐收益最高。
7. **没查的**：本报告未逐条核对"每条注释是否与原版判据一致"（那是账/审查的活）；也未统计 `Effects/` 下 5 个 prefab 与代码的对应关系（.prefab 不在本次范围）。
