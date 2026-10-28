# B89_RX · `🔶 自己设计` 标注独立审计（只读）

审计者：`RX`（只读代理）· 日期 2026-10-28 · 判据源：`d:/2/tools/decomp_full/` · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/` · `d:/2/tools/cpp2il/out_il2csharp/` · `d:/2/tools/il2cpp_out/dump.cs` · `d:/2/新解包资源/assets_full/` · `d:/2/tools/assetripper_out/ExportedProject/` · `D:/2/关服前存档/…`

> ⛔ 本报告只陈述结论与出处，**未改任何 `.cs` / 账 / 正本**。

---

## §0 总数

| 口径 | 数 |
|---|---|
| 全仓 `自己设计` 命中（含历史遗留） | **428** 行 |
| **本轮新增**（`git diff` 工作树新增行 ∩ 标记；含未跟踪新文件） | **37** 行 |
| ├ 纯引用（指向别处标记，不是标记本身） | 5（`CardDatabase.cs:3481` · `LoadableRefCheck.cs:48` · `EnchantmentTemplateCheck.cs:18` · `NetRuntime.cs:282` · `NetRuntime.cs:298`） |
| └ **实质标记** | **32** |
| 　├ **A 该打** | **15** |
| 　├ **B 不该打（判据其实查得到）** | **7** |
| 　└ **C 标错（复刻／结构差异／未查清占位）** | **10** |

枚举口径（重要）：本轮标记**不止一种写法** —— 除规范串 `🔶 自己设计` 外，还有 `🔶 我们挑的` / `🔶 我们挑的字号` 等变体（`grep "🔶"` 比 `grep "自己设计"` 多出 9 行本轮新增）。**建议**：这类变体也应归一到规范串，否则 `selfdesign_census.py` 扫不全。

新增清单（按文件）：`CardView.cs`(2) `LoadableRef.cs`(3) `AudioSetup.cs`(2) `NetRuntime.cs`(4) `LiveOpsHandler.cs`(1) `LiveOpsLocalCatalog.cs`(1) `CardDef.cs`(2) `RuleCore.cs`(1) `TutorialScript.cs`(1) `CardDatabase.cs`(1) `PlayerProfileState.cs`(5) `GameModesHandler.cs`(3) `PlayerDataMath.cs`(2) `LoadableRefCheck.cs`(1) `EnchantmentTemplateCheck.cs`(1) `WindowHeaderWithBackButton.cs`(3) `ItemDrawerCurrency.cs`(2)。

---

## §1 B 档（不该打 —— 判据其实查得到）· 7 条

### B-1 · `CardPresentation/Core/CardView.cs:15245`（`showStats` 的「组」）
- **标记原文**：「🔶 自己设计（非原版）：**「哪几个子对象算一组」是我们挑的** —— 原版那一族是 prefab 上序列化的若干 `CardTextCounterContainer`……**本机没有那张 prefab 的 MB JSON 可读**（`CardTextCountersController.containers` 是私有序列化数组）」
- **其实判据在**：`d:/2/新解包资源/assets_full/` 里 `CardTextCountersController` 的实例 **760 份**，`containers[].activationRules` **是明文序列化字段**，现读分布：
  - **608 份** = `[[0]]`（单容器 · `Normal(0)`）
  - **152 份** = `[[0,15],[5,10]]`（**两个**容器：`Normal|NoSimplifiedView` 与 `SmallScreen|SimplifiedView`）
  - 实例位置 e.g. `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_-3358613892933444672.json` · `…/MonoBehaviour_6320885271709195200.json` · `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1034429599904893608.json`
  - 脚本反查法：`CardTextCountersController` 的 MonoScript pathID = `-1746985122272567298`（`bundle_Waprforge_monoscripts/MonoScript/MonoScript_-1746985122272567298.json`），按它反查 MB 即上表 —— **按类名 grep 序列化数据永远是 0 命中**，这正是本项目踩过 4 次的那个坑。
- **该怎么改**：把 `StatContainerRuleForCell` 由 `NoSimplifiedView(15)` 改为 **`Normal(0)`**（= 608/760 的实值），并把「本机读不到 prefab」整段换成上面这组实数；若要忠实复刻 152 份那族，还要补**第二个容器**（`SmallScreen(5)|SimplifiedView(10)`）—— 我们无小屏档时它恒关，属**已知惰性差异**，应在注释里写明，而不是说成「判据拿不到」。
- **另注**：同段「我们的载体是一数值一图层 ⇒ 组只有一项」这个**结构化取舍本身**是 A 档（原版是 GameObject 列表），但**理由里那句「读不到 prefab」是错的**。

### B-2 · `CardPresentation/Core/CardView.cs:15265`
- **标记原文**：「🔶 自己设计（非原版）：本件每个数值只有一个「容器」，规则取 `NoSimplifiedView` —— **原版每个容器各带一个规则数组，本机读不到那张 prefab 的序列化值**（见上方长注）。」
- **其实判据在**：同 B-1（760 份实例、规则数组明文）。
- **该怎么改**：与 B-1 同批改；这一行是 B-1 的结论落地处，改值 + 改引文即可。

### B-3 · `CardPresentation/Net/NetRuntime.cs:262`（`Connecting` 横幅的**基串**）
- **标记原文**：「🔶 自己设计（非原版）：**基串** / 字号 / 颜色 / 文字在盒子里的对齐 —— 判据拿不到……**基串原版是托管字符串字面量 `DAT_1842b80e0`（`read_literal` 只能读 `_DAT_` 数值，读不到托管串）** ⇒ 取官方本地化里唯一那条连接态文案 `MenuLogin/Intro/Connecting`」
- **其实判据在**：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ShowStatusWhenConnecting.cs:48` —— `string text3 = "Connecting" + text;`；同一事实**本文件自己在 `NetRuntime.cs:298` 已经写出来了**（「2026-10-28 B89-E11e 订正：R4b 原来取 `Loc.T(...)`（🔶 猜的）—— 原版不本地化这一串」）。
- **该怎么改**：把 262 行里的**基串那半句删掉**（基串已由 298 行的 `const ConnectingBaseString = "Connecting"` 落地，属**复刻**），只保留「字号/颜色/对齐」自设计。这是典型的**同一条标记被订正过、但订正只加在下游、上游还在说反话**。

### B-4 · `CardPresentation/Net/NetRuntime.cs:268`（显示时机）
- **标记原文**：「🔶 自己设计（非原版）：**只对 `NetState.Connecting` 显示** · ……原版那个组件的「什么时候启用」是**场景里的 `enabled`**（**本机没读到控制它的那段**）」
- **其实判据在**：原版 `OnGUI` **末尾**就是那个控制段 ——
  - `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ShowStatusWhenConnecting.cs:60-63`：`if (PhotonNetwork.inRoom) { enabled = false; }`
  - `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/ShowStatusWhenConnecting.cs`（OnGUI 尾）同句
  - `d:/2/tools/decomp_full/ShowStatusWhenConnecting__OnGUI.c`（函数只有这 3 个成员：`.ctor` / `GetConnectingDots` / `OnGUI`，别处无启用点 —— 我用 `grep -rl ShowStatusWhenConnecting decomp_full/` 只命中这 2 个 .c）
- **该怎么改**：原版语义 = **组件活着就一直画，直到 `PhotonNetwork.inRoom` 才自己关** ⇒ 我们的等价物应是「**未进房**（Connecting / Handshaking / 大厅排队）都亮，进房即灭」，而不是「只有 `Connecting`」。改 `ShouldShowConnectingBanner(NetState)` 与注释；`Handshaking` 要不要亮**已有判据**（亮）。

### B-5 · `CardPresentation/Net/NetRuntime.cs:313`（同上的常量说明处）
- **标记原文**：「🔶 我们挑的**显示条件**（原版组件的启用源没读到 —— 见类尾）：只对 `Connecting` 亮。」
- **其实判据在**：同 B-4（`if (PhotonNetwork.inRoom) enabled = false;`）。
- **该怎么改**：随 B-4 一起改；「启用源没读到」这句删除。

### B-6 · `CardPresentation/Shell/GameModesHandler.cs:454`（草稿闸**默认值**的理由）
- **标记原文**：「🔶 自己设计（非原版）：**本机没有 `demoData` 这一层**（**全仓 `canEnterDraft` 0 命中**，只有 `Shell/DraftEventBase.cs:23` 的一句注释提到它）……**默认值 `true` 也是我们挑的**」
- **其实判据在**：判定源里这一层**是有的**，只是不在 `decomp_full`：
  - `d:/2/tools/il2cpp_out/dump.cs:79354` `public class DemoData` · `:79362` `public bool canEnterDraftMode; // 0x28`
  - `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/DemoData.cs`（含 `ToggleCanEnterDraftMode(bool)`）
  - `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/PracticeEvent.cs:97-101`：`demoData.canEnterDraftMode = true; PlayerDataManager.singletonManager.UploadOnlineDemoData();`
  - 我搜过的词与结果：`canEnterDraft` → `decomp_full` **0 命中**（标记说的就是这一处）、`cpp2il/out_cpp2il_dev_ilspy` **4 文件命中**、`dump.cs` **1 命中** ⇒ **「全仓 0 命中」只对 `decomp_full` 成立**，不能写成「本机没有这一层」。
- **该怎么改**：**默认值 `true` 本身仍是 A 档**（原版默认 false + 首胜解锁路径，我们无 `PracticeEvent` ⇒ 照原版会变死状态），但理由要改成「**判据已可读（dump.cs:79362 / PracticeEvent.cs:97），我们只是还没实现 `DemoData` 与 `PracticeEvent`**」，并保留「首胜落地后翻回 false」的待办指针。

### B-7 · `CardPresentation/Core/PlayerDataMath.cs:295`（`ExtractDeckInfo` 三格）
- **标记原文**：「🔶 自己设计（非原版）：**三格没有数据源** —— 我们的 `PlayerDeck` 没有「卡组 id」、没有「最后编辑时间」、没有「战吼（warcry）」……因为它们**离线没有来源**（卡组 id 由服务器给、warcry 是服务器侧的装饰项）」
- **其实（部分）判据在**：原版这三格都是 **`CardDeck` 的序列化字段**（`+0x18` deckId · `+0x68` lastEditTime · `+0x30` warcryId，见该 doc 自己引的那段 `.c`）⇒ **`lastEditTime` 是客户端可写的本地时间戳**（原版就是本地 DateTime），「离线没有来源」对它不成立；真正只能来自服务器的只有 **deckId**。
- **该怎么改**：把「三格都没有数据源」拆成「**1 格（deckId）确实只有服务器**」＋「**2 格是我们 `PlayerDeck` 少了列**（lastEditTime 本地可填、warcry 待查清，见 §5）」；后两格应进待办（给 `PlayerDeck` 补列），而不是记成「拿不到」。
- **同族**：`PlayerDataMath.cs:329`（同一取舍的实参处）随本条一起改口径；它本身算 A（填 null 的动作是我们挑的）。

---

## §2 C 档（标错）· 10 条

> 判据：项目规矩（`CLAUDE.md` 铁律 18）里 **⛔ 不许打标** 的三类 —— ① 原版就是那样；② 只是命名/结构不同但语义一致（实现风格）；③ 还没查清就占位（该写 TODO）。

### C-1 · `CardPresentation/Core/LoadableRef.cs:198` / `:220` / `:236`（3 行）
- **标记原文**：「🔶 自己设计（非原版）：**token 类型用 `object` 代替 Newtonsoft 的 `JToken`**……语义逐句照抄。」/「返回类型 `object` 代替 `JToken`」/「见文件头「🔶 自己设计」」
- **判**：属 ② **载体/类型替代**（`JToken` → `object`），方法体逐句照抄原版（`OnBeforeRead` 只认 string、`WriteJson` 写裸 id、null 抛 NRE 全是原样）。工程确实没有 Newtonsoft（`MyGame/Packages/manifest.json` 无该包，已核）。
- **该怎么改**：降级成普通 ⚠️ 说明（「载体替代：无 Newtonsoft，类型用 `object`，语义逐句照抄原版 `.c:795-806`」），**去掉 `🔶` 前缀** —— 否则以后会被当成「我们编了一个值」去怀疑本来正确的复刻。

### C-2 · `RuleEngine/Core/RuleCore.cs:15995`（`EphemeralMarked`）
- **标记原文**：「🔶 自己设计（非原版）：`CardInstance.EphemeralMarked` 是我们「**按份**」记临时卡的第二个载体 · **原版只有 trait 5 一个载体、没有「份」这个概念**」
- **判**：属 ② —— 我们的 `CardInstance`（`RuleEngine/Core/CardInstance.cs:78`）本身就是**每份一个实例**，原版 trait 5 也是挂在 `CardScript` 实例上（`CardScript__ResolveCardPlayed.c:42` `RemoveTraitSilently(card, 5)`），**「按份」这件事原版同样成立**；我们之所以要第二个布尔，是因为 `Card` 是共享的 `CardDef`（`BattleContext.cs:1544` 用 `inst.Card.Has(...) || inst.EphemeralMarked` 合判）—— 这是**我们 `CardDef` 共享**带来的结构替身，不是我们挑的语义。
- **该怎么改**：改写成「结构替身：原版 trait 在 `CardScript` 实例上，我们的 `CardDef` 是共享的 ⇒ 另挂一个每份布尔（`CardInstance.EphemeralMarked`）与其等价」，去掉 `🔶`。

### C-3 · `CardPresentation/Shell/PlayerProfileState.cs:131`（`ResetForTest`）
- **标记原文**：「🔶 自己设计（非原版）：原版是单例、进程结束即销毁，**没有这个入口**；我们是静态 ⇒ 自检之间必须显式清」
- **判**：属 ② **测试钩子**（原版没有自检，当然没有 Reset）。玩家不可观测、语义无差异。
- **该怎么改**：改成普通注释（`// 自检用：原版是单例、无此入口`），去掉 `🔶`。同族 `:241 ReloadCollectorXpFromSave` 同理（见 C-4）。

### C-4 · `CardPresentation/Shell/PlayerProfileState.cs:241`（`ReloadCollectorXpFromSave`）
- **标记原文**：「🔶 自己设计（非原版）：原版没有这个口（它每次登录由服务器灌）；本仓是静态 + 懒加载 ⇒ 要验往返必须有它」
- **判**：同 C-3（测试钩子）。
- **该怎么改**：普通注释。

### C-5 · `CardPresentation/Shell/GameModesHandler.cs:428`（`RankedEventInterfaceType`）
- **标记原文**：「🔶 自己设计（非原版）：本机**还没有** `IRankedEvent` 接口（`Shell/PlayEvent.cs` 里只有……没有接口声明；本批不改那个文件）⇒ 用注入点代替那个 `typeof`。」
- **判**：属 ③ —— `IRankedEvent` 在**原版里是确凿存在的接口**（`dump.cs:57719` `RankedFastMode … , IRankedEvent, …`；`:62773` `[Inherits(typeof(IRankedEvent), new[]{})]`）。这不是「我们挑了一个做法」，而是**我们少建了一个类型**。
- **该怎么改**：改成 TODO/欠账（「待建 `IRankedEvent` 接口 → 直接 `typeof(IRankedEvent)`，删掉本注入点」），去掉 `🔶`；判据写 `dump.cs:57719/62773`。

### C-6 · `CardPresentation/Shell/WindowHeaderWithBackButton.cs:24` / `:31` / `:68`（3 行）
- **标记原文**：`// ===== 我们的对应 / 🔶 我们挑的 =====` · 「④ `GetLabel(baseData, Title, [])` → 🔶 **我们挑的**：……改留一个静态接线口 `TitleTermResolver`：`null` ⇒ 标题写**空串 + 出声**」· 「🔶 我们挑的：默认 `null` ⇒ 标题写空串 + 出声」
- **判**：属 ② —— **「取不到标签 ⇒ 写空串」这个行为是原版的**（文件自己引了 `Shell/BaseEventPopup.cs:42` 的判据；`GetLabel` 取不到时写空串）；我们挑了**只有接线方式**（用 `Func<ILiveOps,string>` 而不是直接调 `LiveOpsLocalCatalog`）。§31 那句「`LiveOpsLocalCatalog` 里那套取器**正在被重构**」更是**我们自己的代码组织状态**，不是原版语义。
- **该怎么改**：改成「接线口（我们特有）：行为照原版空串档，接线方式因 `LiveOpsLocalCatalog` 重构而自定」，去掉 `🔶`；`null` 默认值应写成**待接**（TODO）而不是自设计。

### C-7 · `CardPresentation/Shell/ItemDrawerCurrency.cs:28` / `:85`（2 行）
- **标记原文**：「原版 ARF 在这种容器下的行为**没有判据（未读）**」/「🔶 自己设计（非原版）：容器比抽屉**更窄**时按宽缩 —— 原版 ARF 在这种容器下的行为**没有判据（未读）**」
- **判**：属 ③ —— 标记**自己写着「未读」**。`AspectRatioFitter` 是 Unity 内置组件，行为**一定可查**（引擎语义 + 原版 prefab 上的 `m_*` 字段都能读），属于「**还没查**」，不是「查不到」。
- **该怎么改**：改成「还没查清：原版 `AspectRatioFitter` 在此容器的表现未读，暂按 `min(H, W/0.66)` 近似」＋ 待办；⛔ 不用 `🔶` 蒙混（`CLAUDE.md` 铁律 18 明确点名这一档）。

---

## §3 A 档（该打）· 15 行 · 一句话确认

> 计数：下表 A-1 与 B-1 是**同一行**（`CardView.cs:15245`）的附加说明、不另计行；A-15 覆盖 2 行（`:365` / `:370`）。

| # | 位置 | 一句话（判据确实拿不到 / 确实是我们挑的） |
|---|---|---|
| A-1 | `Core/CardView.cs` 段内「一数值一图层」的**结构化取舍**（15245 的后半段）· **与 B-1 同一行，不另计** | 原版是 GameObject 列表、我们每数值一图层 —— **结构差异本身**属实（但理由那句「读不到 prefab」见 B-1） |
| A-2 | `Editor/AudioSetup.cs:412` | 原版快照落**子 mixer**（FX Master / Music BattleGroup*），我们是一份混好的 mixer ⇒ **投影层**只能自定，且代价（① Low/Med/High 同值 ② 与 `VolumeFX/VolumeMusic` 滑块抢同一根浮点）已如实写 |
| A-3 | `Editor/AudioSetup.cs:747` | 我们 mixer **没有「组默认值」这一格**（组音量只按快照存）⇒ 只能往 6 条快照各写一份；机制差异成立 |
| A-4 | `Shell/LiveOpsHandler.cs:105` | 原版 `ActionScheduler.Update()` **每帧**检查，我们 **1 秒一拍** —— 这是**我们主动的节流**（行为等价性有论证：活动刷新是小时/天尺度） |
| A-5 | `Shell/LiveOpsLocalCatalog.cs:1146` | 原版校验 `RuntimeKeyIsValid`（Addressables），本机无那套装载层 ⇒ 用「子对象名非空」当**代理判据** |
| A-6 | `RuleEngine/Core/CardDef.cs:9007` | 原版是**逐 `CardEffect` 对象**的 bool（`+0x74`），我们无 `activeEffects` 列表 ⇒ 只能按 `(at,ab,lg)` 落索引；形状差异属实且**不可当语义等价**（已写死） |
| A-7 | `RuleEngine/Core/CardDef.cs:9063` | `HasPermanentHandCostBuff` 是**卡级汇总**，原版没有这一格（我们的 `CardInstance.HandEffect` 不带 `(ab,lg)`）⇒ 确实是我们的补丁 |
| A-8 | `RuleEngine/Core/TutorialScript.cs:2010` | 原版判等 = `UnityEngine_Object__op_Equality`（**引用**），我们用 `CardDef.Id` 字符串 ⇒ 同 Id 两份卡在原版不等、在我们相等 —— 差异属实（⚠️ 引文出处见 §4-2） |
| A-9 | `Shell/PlayerProfileState.cs:184` | 原版 `CurrentXp` 是**一个可写整数**（服务器每次登录下发），我们的总 XP 是**从背包反推的只读派生值** ⇒ 只能加一层增量 |
| A-10 | `Shell/PlayerProfileState.cs:261` | 原版 raise `CollectorXPChangeSignal`/`CollectorLevelChangeSignal`，本仓**无信号层** ⇒ 改直接调两处 toast（机制不同、端点相同） |
| A-11 | `Shell/PlayerProfileState.cs:285` | 「值从哪来」那半条链（服务器 `RecordMatchFinished` 回包 → `RewardService.Collect`）离线**不存在** ⇒ 用原版公式 `GetXPFromMatch` 充作服务器给的数；公式本身逐句原版（已写明⛔ 别拆） |
| A-12 | `Shell/GameModesHandler.cs:36` | `GameModesConfig` 是**远端 TitleData**：`assets_full` 只有 MonoScript 类定义（`grep -rl "GameModesConfig"` → 2 命中，全是 `MonoScript/*.json`）、`grep -rl "mainRankedType"` → **0 命中**（若远端资产在本地，序列化 JSON 会带字段名）⇒ 无实例、只能留注入点 |
| A-13 | `Core/PlayerDataMath.cs:329` | 实参填 `null / default / null` 的动作是我们挑的（口径随 B-7 一起修） |
| A-14 | `Net/NetRuntime.cs:301` | GUI 字号 28 px —— 原版是 IMGUI 默认/自带 skin，无像素判据（`Skin` 字段未在本地找到实例，见 §5） |
| A-15 | `Net/NetRuntime.cs:365` / `:370` | `sortingOrder = 30000`（压在壳所有窗之上）与「画布中心锚点」：原版 `GUILayout` 是区内**左上**默认对齐、IMGUI 恒在最上层 ⇒ 我们的对齐/层级是自定（**这是真实可见偏差，保留标记是对的**） |

---

## §4 顺手发现（不属于「该不该打标」，但值得进账）

1. **原版横幅还有第二行文字，我们没做**：`ShowStatusWhenConnecting.OnGUI` 在正文之后还有 `GUILayout.Label("Status: " + <ClientState>)`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ShowStatusWhenConnecting.cs:56-58`；`out_il2csharp` 版是 `"Status: " + typeof(ClientState).ToString()`）。R4b 的横幅只有一行 ⇒ **缺一条**（是否要复刻请按铁律 11 立账）。
2. **`TutorialScript.cs:2010` 的引文指错了方法**：引用相等那句话在 **`AiScripted__CheckIfWaitingToUseAbility.c:121-128`**（`UnityEngine_Object__op_Equality(chosen, cardSelected)`）；而 `AiScripted__GetChosenCard.c` 里用的是 **`System_String__Equals(+0x38, +0x38)`**（`+0x38` = `PlayerItem.uniqueId`，`dump.cs:18813`）。结论（我们与"引用相等"不同）**不变**，但「`GetChosenCard` 用引用相等」这句是错的，应订正出处。
3. **`CardView` 的 152 份双容器是真实存在的第二档**：`[[0,15],[5,10]]` 那 152 份（= 有「简化视图/小屏」两档的卡）我们塌成了单容器 ⇒ 无小屏档时**惰性无害**，但应写成「已知惰性差异」而不是「判据拿不到」。
4. **`BattleDriver.cs:34667` 是一处会随 B-1/B-2 一起过期的引用**（「判据与 🔶 标记见 CardView 那个方法」）—— 改 CardView 时别漏。
5. **`NetRuntime.cs` 同一事实存在三套说法**（`:262` 说基串拿不到 / `:282` 说「基串是 🔶」/ `:298` 说基串已订正为 `"Connecting"`）⇒ 这正是「两份说法打架比没有更糟」的活标本，建议一并收口。
6. **未打标但已核为「复刻」的两处**（说明没漏标）：`Shell/CurrencyNames.cs`（货币名走原版 `Currency__get_DisplayName` 词条、图标走三段 guid 链，判据齐全）与 `Shell/ItemDrawer.cs` 本轮新增那一行（`CurrencyNames.Of/Icon` 接线）—— 这两处**不该**打 `🔶`，现状正确。

---

## §5 没查清（如实记）

1. **`PlayerDataMath` 的 `warcry(+0x30)` 是不是玩家可选**：`CardDeck` 的成员表我这次只读到内部类（`dump.cs:20416-20460` 全是 `<>c` / `DisplayClass`），没拿到 `CardDeck` 主类字段区 ⇒ 「warcry 属服务器侧装饰项」这条**未证实**。若要收口，读 `dump.cs` 的 `class CardDeck`（TypeDefIndex 376 附近）＋ 卡组编辑窗是否有该控件。
2. **`NetRuntime` 的字体/颜色**：全 `assets_full` 里 **没有任何 `GUISkin` 资产**（`find … -iname "*GUISkin*"` 0 命中；AssetRipper 工程只有 `reporterScrollerSkin.guiskin`，是调试插件皮肤）⇒ 原版 `Skin` 字段大概为 null、走 Unity 自带 skin。但**自带 skin 的 `customStyles[0]` 具体值我没核**（要跑一次编辑器）+ 我也没在 `assets_full` 找到 `ShowStatusWhenConnecting` 的**实例**（按其 MonoScript pathID `927` 反查 MB = 0 命中 ⇒ 那条组件可能在未导出的场景里）。⇒ 「字号/颜色无像素判据」这句**方向可信、未闭环**。
3. **`D:/2/关服前存档/…xapk`** 我**没有解包**（红线：不动 `d:/2` 一个字节；解包要开新目录）。若 B-1/B-5 那两类还想再坐实「远端也没有别的说法」，可在 `_tmp_view/apk135/` 里解 `UnityDataAssetPack.apk` 复核 —— 但现有本地证据已足够改。
4. **`AudioSetup` 的「原版快照落子 mixer」**：我采信该文件上方那段带 `transitionIndices: [4,6,8]` 的反证（分析自洽、数据来源标明为原版 mixer 资产），**未独立重读原版 mixer 资产**。
