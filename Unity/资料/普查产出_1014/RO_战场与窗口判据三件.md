# RO · 战场与窗口判据三件（A555 · A383 · A380④）

> ⚠️ **本文件由主对话代录**：跑这一趟的代理**没有写文件工具** ⇒ 它把正文写在交件消息里，**由主对话落盘**（结论/VA/数字/清单**一字未改**，只压掉了它的过程叙述与重复引用）。
> ⛔ 原报告自陈：未改任何工程文件 · 未跑 Unity · 未动 git · 未改 `d:/2/`。

## 判据原文出处（定位）
- `普查产出_1013/批次计划_1013.md` §六·B 的 A555 · A380④ · A383（`grep -n` 定位）
- `普查产出_1014/清单_A表全量.md` 里 A380 / A383 / A555 三行
- A555 来源 = `普查产出_1013/WB2_场景AnimFX静默口.md` §顺手发现④ · A380④ 来源 = `普查产出_1011/R1_每日骷髅与登录卡.md` §八·6 · A383 来源 = `普查产出_1013/A表现核_块3.md` §A383

---

# 一、A555 · `ApplyGroupNodes` 的分组节点 × `nodes[]` 重叠面

## ① 现状
**A. 建场侧建法**（`MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs`）
- 调用点 `:2496`（在 `ApplyLightAndAmbient` 之后）；本体 `:2543-2631`。
- ①建节点段 `:2559-2577`：对 `sc.nodes[]` 每条**无条件 `new GameObject(n.name)`**（`:2568`），**没有「父下已有同名子件就复用」的检查**；父解析失败 ⇒ 挂场根并 `LastGroupNodeMissed++` 出声。
- 🔴 `:3201-3203` 的注释**已过期**：仍写「13 场里只有 `battlearenadarkangels` / `battlearenatauviorla` 有这件旁挂」—— 现读是 **13/13**（生成器头注 `工具/gen_arena_groups.py:51`）。

**B. 每场：建了几颗 / 与 `nodes[]` 重合几颗**（现读 13 份 `<场>_groups.json` × `Resources/EnvBlendables.json` 的 `sceneStandaloneBuild[].nodes[]`，按整条路径求交）

| 场 | 分组节点 | targets | adds | 运行时 `nodes[]` | 重合 |
|---|---|---|---|---|---|
| battlearena1 | 21 | 62 | 0 | 0 | 0 |
| battlearena2 | 13 | 62 | 0 | 3 | **3** |
| battlearena3 | 29 | 91 | 0 | 4 | **4** |
| battlearenaaeldari | 13 | 63 | 0 | 0 | 0 |
| battlearenaastramilitarum | 7 | 102 | 0 | 0 | 0 |
| battlearenablacklegion | 16 | 87 | 0 | 0 | 0 |
| battlearenadarkangels | 17 | 84 | 0 | 0 | 0 |
| battlearenaemperorschildren | 6 | 94 | 0 | 0 | 0 |
| battlearenagenestealers | 9 | 83 | 0 | 0 | 0 |
| battlearenaleviathan | 14 | 89 | 0 | 0 | 0 |
| battlearenasororitas | 10 | 130 | 0 | 0 | 0 |
| battlearenaspacewolves | 12 | 82 | 0 | 0 | 0 |
| battlearenatauviorla | 96 | 135 | 0 | 1 | **1** |
| **合计** | **263** | **1164** | **0** | **8** | **8** |

重合明细（8 条全中）：arena2 `Scenario` / `Scenario/Battle Arena 2 Particles` / `…/RocketTrail`；arena3 `Scenario` / `Scenario/Particle Effects` / `…/Lightning_Green` / `…/Lightning_Green (1)`；tauviorla `Scenario/Battle Arena Tau Viorla Baked/Railgun BIG (1)/Big Gun Effect`。

**C. 会不会同一颗被建两遍**（三条独立口径）
1. **建场侧（prefab 里）= 0 颗**：263 条 `nodes[].path` 在 13 份 prefab 里**逐条各命中 1 次**，`t.get(p)>1` 全 0。prefab mtime 全 = 2026-10-06 12:42。
2. **运行时侧 = 会撞，但已有守卫**：`CardPresentation/Battle/ScenarioBlendables.cs:2329-2347`（2026-10-13 · A514）先 `FindChildByName`，**有就复用 + 出声**（`SceneAnimFxNodesReused++`），没有才建；断言 `Editor/BattleScene.cs:11042-11082`（`wantNodes{3,4,1}` / `wantReused{3,4,1}` / `:11079` 钉「**新建必须 == 0**」）。
3. 🔴 **已发生 1 类真·同名两遍（静默风险）= `reflection camera`**：
   - 我们这侧：`ArenaBuilder.BuildMirror :3934-3961` 在**场根**建 `new GameObject(mf.go)`（8 场全是 `reflection camera`）。
   - 旁挂那侧：`groups.json` 的 `nodes[]` 里**又各有一条同名空节点** —— arena3 `…/Ground/reflection camera` · aeldari `…/Floor (1)/reflection camera` · tauviorla `…/Floor/reflection camera`。
   - 实测：这 3 场 prefab 里**各有 2 颗 `reflection camera`**；arenadarkangels 只有 1 颗。
   - **根因**：`工具/gen_arena_groups.py:404-425` 的 `wanted_paths()` 只把清单的 `meshes`/`particles`/`light`/`camera` 当「已建」，**没把 `<场>_mirror.json` 那台算进去** ⇒ 原版镜相机的路径被判成「我们没建」而进 `nodes[]`。
   - 后果**静默**：那一族查找是「名字 + 最近位置」，同名两颗命中哪颗不定。
4. 另两条口径也查了、**都是 0**：`groups.json` 内部重复 `path` = 0 · 重复 `(parent,name)` = 0；`nodes[] ∩ targets[]` = 13 场全 0；生成器自报 `_stats.coverage.unmatched = 0 · suspect = 0`。

**D. 现读 `gen_env_blendables.py` 的输出**
- 守卫：`工具/gen_env_blendables.py:832-850` `group_node_paths(arena)` + `:942-943 if paths[i] in groups_paths: continue`；调用点 `:1334-1335`。
- 🔴 **现读的 `MyGame/Assets/Resources/EnvBlendables.json` 比两份输入都旧**：`EnvBlendables.json` mtime 2026-10-05 22:31 · 13 份 `_groups.json` 10-06 10:14 · `gen_env_blendables.py` 10-06 13:37 ⇒ 那 8 条重合是**旧产物**。按代码，重跑后 3 场的 `nodes[]` 会**归零**（8 条 path 全落在该场 `groups_paths` 里 ⇒ 全被 `continue` 跳过）。
- ⚠️ 本件**没跑** `--check`（属 A418② 主对话 Unity 腿）。

## ② 结论
**能定案**：`nodes[]` 与建场侧面**重合 8/8**、**建场侧 0 颗重复**、**运行时那 8 颗由 A514 复用支挡住**（+ A431 断言钉死「新建 == 0」）。
**还缺什么（不是「判不了」，是「要一起动」）**：
1. A418② 一跑，`sceneStandaloneBuild[].nodes[]` 3/4/1 → **0/0/0** ⇒ `Editor/BattleScene.cs:11044-11045` 的 `wantNodes{3,4,1}` / `wantReused{3,4,1}` 与 `:11079` 会**红** ⇒ **两处必须同步改**。
2. `ArenaBuilder.cs:3201-3203` 那句「只有 2 场有旁挂」要订正成 13 场。
3. `reflection camera` 那 3 颗同名空节点：**要不要从 `nodes[]` 排除**（= 把 `_mirror.json` 的对象算进 `built_paths`）—— **口径裁定**，本件只报不改。

## ③ 建议派什么活
- **写手件 A（纯注释/文档，1 文件）**：订正 `ArenaBuilder.cs:3201-3203` + `ApplyGroupNodes` 头注补「13 场各一份、共 263 颗」。零自检。
- **写手件 B（`Editor/BattleScene.cs`，占一次宿主）**：把 A431 的 `wantNodes/wantReused` 改成**由旁挂条数驱动**（读 `sceneStandaloneBuild[].nodes.Length`），并加「`nodes[] == 0` 时也必须绿」的断言 ⇒ A418② 跑完不会红。
- **写手件 C（`工具/gen_arena_groups.py` + 重生成 13 份 `_groups.json`；⚠️ 牵动 `ApplyGroupNodes` 与 prefab 重烘）**：把 `<场>_mirror.json` 的 `go` 算进 `built_paths`。⚠️ **先要调度台裁一句**「那台镜相机我们已经在场根建了 ⇒ 原版那条路径要不要保留一个空壳」。
- **顺带**：跑 `--check` 把「重跑后 3 场 `nodes[]` 归零」变成实测。

---

# 二、A383 · 排位窗在原版的模式号

> 方法：`d:/2/tools/il2cpp_out/script.json`（方法名 ↔ RVA）取地址 → PE 节表换文件偏移 → `capstone` 反汇编 `d:/2/unity_run_ref/GameAssembly.dll`（imagebase 0x180000000）。

## ① 现状（逐条带 VA）
**A. 槽位尺子**：`dump.cs:130005-130050` `interface IPlayEvent` —— **Slot 0 = `PlayModes EventPlayMode`** · Slot 3 = `StartMatch()` · Slot 8 = `SetDefaultDeck` · Slot 9 = `GetEventDeck()` · Slot 11 = `GetBaseData()`。`FUN_1800021F0`（VA 0x1800021F0）= 「按 (接口 TypeInfo, slot) 取实现」。`script.json`：**0x42AB400 = `IPlayEvent_TypeInfo`** · 0x42AD4F0 = `IRankedEvent_TypeInfo`。

**B. 排位那条链**
1. `RankedEventWindowV2$$BattleButtonClick` 与 `$$StartBattle` **同址**（VA **0x1806C6070**）：取 `[this+0x70]` → `IPlayEvent` **slot 9** `GetEventDeck()` → **slot 8** `SetDefaultDeck(deck, true)` → **尾跳 slot 3** `StartMatch()`。三个调用的目标都 = **0x1842AB400**（`IPlayEvent_TypeInfo`），与槽位编号吻合。
2. `RankedV2Event$$StartMatch`（VA **0x1808BA300**）：在 0x1808BA59D `call 0x180895FD0` = `MatchMakerManager$$FindMatch` 的**不带模式号那个重载** ⇒ **排位这条路从头到尾没写死任何模式号**。
3. `FindMatch`（不带模式号）@VA **0x180895FD0**：0x180896024 `call 0x1800021F0`，`ecx=0` · `rdx=[0x1842AB400]` · `r8=currentEvent` ⇒ **取 `IPlayEvent` slot 0 = `EventPlayMode`**；返回值直接当下一跳的 `playMode` 参数。
4. `FindMatch`（带模式号）→ `MatchData..ctor`（RVA 0x736B70）把 `playMode` 写进 `MatchData.playMode`（字段 0x18）。
5. ★ **答案那一条**：`MatchData..ctor` 里 `matchType`（字段 0x1C）**由 `playMode` 查一张 14 项跳表派生**（VA 0x180736CAE，表在 RVA 0x736FE0）：

| `PlayModes` | → `MatchType` |
|---|---|
| **0 Classic** | **10 Ranked** |
| 1 Duel | 150 Duel |
| 2 PracticeLodge | 90 Practice |
| 3 Dungeon | 80 EventAI |
| 4 Tutorial | 100 Tutorial |
| 5 CutScene | 190 Scene |
| 6 OfflinePractice | 50 PracticeOffline |
| 7 ClosedDeck | 110 ClosedDeck |
| 8 Campaign | 130 Campaign |
| 9 TutorialReplay | 140 TutorialReplay |
| 10 Replay | 160 Replay |
| 11 RankedFriendly | **170 Unranked** |
| 12 OwnDeckTraining | 50 PracticeOffline（与 6 同址） |
| 13 Skirmish | 200 FastMode |

6. ★ **模式号本体**：`RankedV2Event$$get_EventPlayMode` = VA **0x1804BD440**，机器码 **`33 C0 C3`** = `xor eax,eax; ret` ⇒ **返回 0 = `PlayModes.Classic`**；`RankedV3Event` **同一地址**。
   横向对照（全是 3 字节常量返回）：`PracticeEvent` → **6 OfflinePractice**（VA 0x1808B66B0）——**与 A 表已知的「练习 = OfflinePractice 6」逐字吻合**（校验项）；`TutorialEvent` → 4 · `DraftEventBase` → 7 · **`FastModeBaseEvent` → 13 Skirmish**（`RankedFastMode : FastModeBaseEvent`）。
7. 不涉及开战的两条：`IRankedEvent`（`dump.cs:130533`）Slot 4 `get_IsRanked()` · Slot 7 `ToggleRankedMode(bool)` ⇒ **那颗钮改的是 `IsRanked` 那个 bool、不产生任何 `PlayModes` 号**；`IRankedEvent$$MainPlayModeBySeason(int)`：`season < 25 → 0`，否则对得上回 `EventPlayMode`（= 0）、对不上回 `13`。
8. 「找对手」那半：旁证 = `SearchingMatchWindowDemo` 11 个方法**没有任何 `StartMatch`/`FindMatch` 调用点**。
9. ⚠️ **读不到**：现役跑哪个 ranked event 由**服务端** LiveOps 数据决定 —— **不影响结论**（两条都判死：0 / 13）。

## ② 结论
- **排位窗（原版 `RankedEventWindowV2`，= 我们 `Shell/RankedEventWindow.cs` 那一族）= `PlayModes.Classic = 0`**；**不是 `RankedFriendly 11`**。`RankedFastMode`（另一支 ranked 事件）= **13 Skirmish**。
- **`MatchType` 与 `PlayModes` 的关系确定**：`MatchData` 上两个并排字段，`matchType` **由 `playMode` 派生** ⇒ `MatchType.Ranked 10` 恰是 `PlayModes.Classic 0` 派生出来的；`Unranked 170` 派生自 `RankedFriendly 11`（**这一版没有任何 event 返回 11**）。
- **要订正的旧说法**：原版同名类 `RankedRewardEventWindow : GameWindow`（`dump.cs:64610`）**只有 4 个方法** + 一个 context 属性、**没有开战入口** ⇒ **它没有模式号**。带「开始对战」的是 `RankedEventWindowV2`。
- 原 A 表那条「先要裁一件（取 `MatchType` 还是 `PlayModes`）」**可以撤了** —— 原版给了现成答案。

## ③ 建议派什么活
1. `MyGame/Assets/RuleEngine/Core/GameplayVariables.cs:25-30` 的 `GameMode`（现只有 `Classic 0 / Skirmish 13`）补全成原版 15 档（判据 `dump.cs:46188-46204`）；
2. **五个入口窗**的模式号按现读判据写死并带注释：练习 `OfflinePractice 6`（VA 0x1808B66B0）· Practice Deck `OwnDeckTraining 12` · 遭遇战 `Skirmish 13` · **排位 `Classic 0`（判据 = VA 0x1804BD440）**；
3. `Battle/BattleDriver.cs`（+ `:1734/:1917/:2132`）那四处二选一改成带真模式进对局；`Net/NetBattle.cs` 若模式要过网一并。
**口径提示**：`MatchType` **不要**由入口窗给 —— 要复刻就复刻在「`MatchData` 等价物」那一层（`RuleEngine/Core/BattleContext.cs`）做同一张 14 项派发表。
**顺手核一条**：`Shell/DailyData.cs:1198-1210` 的 `ModeGivesSkulls` ⇒ **排位取 0 落在「给骷髅」那 9 档里**，与 R1 记的原版规则一致，不用改。

---

# 三、A380④ · 桌面另两张实拍逐张对账

## ①-1 那两张实拍在本地哪里（**只在桌面**）
| 文件 | 大小 · mtime | 位置 |
|---|---|---|
| `奖励—活动的参考图.png` | 1,616,790 B · 2026-10-05 19:31 | **`C:\Users\qjh36\Desktop\`（本地唯一一份）** |
| `奖励—锻造厂的参考图.png` | 1,531,930 B · 2026-10-05 19:32 | 同上 |

搜过、**都没有副本**：`资料/原版实拍/` · `资料/原版参照图/` · `d:/4/Unity/素材/` · `d:/4` 全盘 `-iname "*奖励*"`（只命中文档与 `_tmp_view/rewards/*.png` —— 那是我们自己的 dump）。
（同批另两张也在桌面、R1 已用：`奖励—布道所（每日任务）参考图.png` · `奖励-每日连胜的参考图（…翻译错误）.png`。）
⇒ **对账报告里要写全路径，⛔ 别当成仓库资产。**

**两张图里有什么（亲读）**：照 1（活动）= 左栏 5 键 + 奖励 4 子页签（活动红底选中）· 顶栏（`YPY20241115`/28/邮件/5 个货币 `300/2000`·`2223`·`252`·`1950`）· 设置齿轮 · `太空野狼` + `积分：0` + 徽 + 进度条 · 一排 8 个阵营图标（各带进度条）· 星图节点地图（绿连线、节点是**太空野狼卡背**）· 左下 `高级战役每日奖励` + `下一个 : 4分钟4秒`。
照 2（锻造厂）= 同左栏与顶栏 · 顶部阵营条（8 颗、`太空野狼` 金框选中）· `太空野狼` + `等级 2/50` · 中间**三张奖励卡**（左紫「1 史诗通用牌」· 中黑「1 增强群组」· 右绿「1 稀有通用牌」，各带阵营小徽）· 底部轨道 `2/3/4` 三格圆牌（2 高亮）+ `50/110` + 紫宝石 · 左右机械柱/齿轮框 · 右上书形 Help 图标 · 左栏旁一颗紫色 `0` 角标。

## ①-2 对账口径（照 R1 §六）
- **六列表**：`# | 项 | 我们值 | 原版值 | 判据 | 是否已标注「我们挑的」`（`资料/普查产出_1011/R1_每日骷髅与登录卡.md:229-244`）；配套 `§判不了的（缺什么才能判）` + `§顺手发现（⛔ 只报不改）`。
- **判据优先级**（R1:4）：**反编译方法体 > 解包资产实读 > 用户自备实拍**；只有实拍一处 ⇒ 写「实拍所见」并注明「未证」。
- **「我们挑的」先例**：`Shell/ForgeData.cs:1-20` · `资料/日常_原版规格.md:218-226 / :300-316`。
- ⛔ 不许拿我们的实现当期望值（自证）。

## ①-3 「每张要量哪几项」清单（每项 = 一行待填的对账行）
**照 1 · 活动**（`Shell/CampaignTab.cs` + `CampaignData.cs`；规格本 `资料/阶段二_锻造厂与战役页_原版规格.md`）
1. 顶栏货币：带上限那颗（`300/2000`）—— **A374 已处理，⛔ 别重做**；其余三颗是否裸数/千分位。
2. 顶栏其余：等级徽 · 玩家名 · 邮件 · 设置齿轮。
3. 左栏 5 键 + 奖励 4 子页签与**红底选中态**。
4. `Campaign Army Selector`：实拍那排是 **8 个**阵营 · 选中态 · 每格底下进度条（我们现读 `"Points: " + Points`）。
5. `Campaign Header`：`Title`（实拍 `太空野狼`；我们出厂文案 `ULTRAMARINES`）· `积分：0`（我们英文 `Points: ` —— **本地化词条在远端是已裁定的偏离**）· 徽 · Info 按钮。
6. `Campaign Track` 地图：**⚠️ 只能量结构/样式，⛔ 不能逐节点对数据**（实拍是太空野狼、我们只有 UM 的 47 颗）。要量：节点颗数/拓扑 · 六状态色 · `Premium Mark`（`localScale (2,2,2)`）· 奖励图标 · 连线颜色/线宽 · 连线在节点图形**之下**。
7. 🔴 **`Premium Panel` 三态（实拍是 `state 2`）**：实拍只有「标题 + 倒计时」= 原版已领态（`CampaignPremiumPointsPanel__ChangeState.c` 在 `state 2` 里把 `collectButton` **关**、`pointsObject` **关**、`timerDisplay` **开**、换 `collectedSprite`）。**我们 `CampaignTab.BuildPremiumPanel` 把四件全建、恒显示** ⇒ **一条实打实的差异**。
8. `Timer Text`：实拍 `下一个 : 4分钟4秒`（真倒计时 + 中文 + 冒号两侧各一空格）vs 我们现读写死的出厂占位 `"Siguiente: 5d 20h 15m"` ⇒ 差异行。
9. `Points`：实拍那一档被隐藏（见 7）⇒ 要判 `200` 得另找「可领态」实拍；我们现读写死 `"200"`。
10. `Campaign Background` / `Background Image`。
（清单第 7 + 第 8 项**合成一条差异**：同一个面板同一档状态。）

**照 2 · 锻造厂**（`Shell/ForgeTab.cs` + `ForgeData.cs`）
1. 阵营条：实拍 **8 颗**（金框选中）vs 我们 **13 个阵营** ⇒ 量格数与选中框图。
2. `Selected Army Info`：`等级 2/50` —— 格式 `"{0} {1}/{2}"` + 本地化键 `MainMenu/Level`；量**分子**与中文「等级」。⚠️ `MaxLevel=50` 是**我们挑的**。
3. 中间三张奖励卡：量**卡底/边框色 ↔ 品阶映射**（紫=史诗 / 绿=稀有）· 文案格式 · 每张底部阵营小徽 · 中间那张是 booster 图。⚠️ 卡里**内容是我们挑的**（`ForgeData.Palette` 自建）。
4. 底部轨道：量可见格数 · 圆牌两态图 · 当前格高亮 · `50/110` 格式 · 宝石图标。
5. 状态对映：`Locked/InProgress` → `_milestone_off`；`ToCollect/Collected` → `_milestone_on`。
6. `Ready for level up` 光效：原版出厂激活、`Initialize` 第一件事 `Toggle(false)` ⇒ 实拍该格有没有光效 = 顺带验我们「建完立刻关掉」。
7. 左右机械柱/齿轮框：量它们**盖住轨道的范围**。
8. 右上书形 `Help Icon`：图/位置/悬停面板（我们**面板照弹、正文留空** = 已裁定偏离）。
9. 左栏旁那颗**紫色 `0` 角标**：量那是什么，我们**有没有**。
10. 页面底图是否与 `Forge Tab/Background` 对上。

## ② 结论
**能定案**（判据齐）。**要如实标的局限**：活动那张是**太空野狼** ⇒ 地图只能对结构/样式（其余 12 阵营的节点图在服务端）。**本件没做**逐项对账（判据原文要求的就是「先派只读普查」）。

## ③ 建议派什么活
**切两件、可并行**（两页文件零交集）：`CampaignTab.cs/CampaignData.cs` 一件 · `ForgeTab.cs/ForgeData.cs` 一件；产出落 `资料/阶段二_锻造厂与战役页_原版规格.md` 的新 §。
**活动件开头就读**：`decomp_full/CampaignPremiumPointsPanel__ChangeState.c` · `CampaignWindowTab__{SetActiveCampaign,TryCollect,OnNodeClicked}.c`。
断言落点 = `CardPresentation/Editor/RewardsScene.cs`。⛔ 别重做清单第 1 项。

---

## 附 · 原报告读过什么（判据留痕）
**工程侧**：`ArenaBuilder.cs`（`:2494-2631`/`:3169-3211`/`:3890-3961`）· `ScenarioBlendables.cs`（`:2195-2246`/`:2290-2420`）· `Editor/BattleScene.cs`（`:10992-11090`）· `Shell/{CampaignTab,ForgeTab,ForgeData,DailyData}.cs` · `RuleEngine/Core/GameplayVariables.cs:20-40` · `工具/gen_env_blendables.py` · `工具/gen_arena_groups.py` · 13 份 `arenas/<场>/*.json` · `Resources/EnvBlendables.json` · 13 份 `Resources/ArenaPrefabs/<场>.prefab`（自写只读解析器，未写盘）。
**文档**：`批次计划_1013.md` · `WB2_场景AnimFX静默口.md` · `A表现核_块1/块3.md` · `清单_A表全量.md` · `R1_每日骷髅与登录卡.md` · `S6_剩余现核_前半.md` · `阶段二_锻造厂与战役页_原版规格.md` · `历史/会话_2026-10-11_批次2.md` · `五张参考图_复刻现状_0928.md`。
**反编译/元数据**：`il2cpp_out/{script.json,dump.cs,stringliteral.json}` · `decomp_full/{IRankedEvent__MainPlayModeBySeason, RankedV2Event__StartMatch, MatchMakerManager__FindMatch, MatchMakerManager__StartMatch, ChangeRankedModeToggle__Clicked, RankedFastMode__ToggleRankedMode, CampaignPremiumPointsPanel__ChangeState}.c` · `GameAssembly.dll`（capstone 反汇编 VA 0x1806C6070 / 0x1808BA300 / 0x180895FD0 / 0x1808960F0 / 0x1808AEB10 / 0x1804BD440 / 0x1808B66B0 / 0x180736B70 / 0x1800021F0 等）。
**两张实拍**：`C:\Users\qjh36\Desktop\奖励—活动的参考图.png` · `奖励—锻造厂的参考图.png`（已亲读）。
**未做**：没跑 Unity · 没跑 `gen_env_blendables.py --check` · 不知道服务端当前活跃的是哪个 ranked event（不影响结论）· 两张实拍的逐项对账本件没做。
