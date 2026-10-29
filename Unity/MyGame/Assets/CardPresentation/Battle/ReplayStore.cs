// ReplayStore.cs — **本地「录像」的存放与读写**（一局一个文件）
//
// ============================ 这东西是什么 ============================
// 原版的回放**整套在服务器上**（PlayFab CloudScript 430/440/620/650），本地录不了也放不了
// （判据 → `资料/普查产出_0927/回放_入口与数据链.md` §C/§D）。用户 2026-09-27 拍板：
// **我们自己做一份本地的** —— 这是**加功能、不是复刻**（同「结算写本地对局记录」那条）。
//
// 🔴 **录的不是画面，是「动作流 + 起始条件」** —— 与原版 `BattleRecordData` 同一个形状
//    （两条 `PlayerAction` 列表 + `rngNum` + 双方牌组 + 换牌）。为什么这样就够：
//    · 引擎的随机走 `Ctx.Rng`（`System.Random(seed)`，种子定死）⇒ 同样的动作序列消耗同样多次;
//    · **AI 的骰子走 `Ctx.AiRng`，而它只用在「挑哪条动作」上**（`SimpleAI.cs` 的 `TweakAvailableActions` 里那句 `ctx.AiRng.Next` 那一处），
//      `ExecuteAction` 不碰它 ⇒ **把挑出来的动作原样重放即可**，AI 不必重跑。
//    ⇒ **一条动作 = 一个 `MsgAction`**，这正是联机重连重放用的那个结构（`NetApply.Apply` 是唯一的落地实现）。
//
// ---- 🔴 记账口一共【三个】（改动时照这张表核对，正本 = `BattleDriver.cs` 文件头那张清单）----
//   ① **玩家动作** —— `BattleDriver.LocalAct`（出牌 / 技能 / 攻击 / 收集灵魂石）+ `EndPlayerTurn` 里那条；
//   ② **AI 动作** —— `SimpleAI.Executed`（挂在引擎边界上，见 `SimpleAI.Executed` 那条字段）；
//   ③ 🆕 **2026-10-18（A938）教程脚本动作** —— `TutorialScript.Executed`
//      （`RuleEngine/Core/TutorialScript.cs`，**形状与 ② 一样**：执行器在引擎边界上发「已执行的动作」）。
//      🔴 **为什么非有它不可**：教程脚本驱动的那些动作**既不是玩家挑的、也不是 AI 挑的**
//      （原版没有这个概念）⇒ ①②**一个都盖不到** ⇒ 不记的话**教程局录像从那里开始演成另一局**。
//      ⚠️ 它带的那份 `ScriptedActionDone` **不是 `AiAction`**（`Core/` 不许引 `Data/`，
//        离屏探针只编 `Core/*.cs`）—— 由 `BattleDriver.OnTutorialScriptExecuted` 翻成
//        `AiAction`（`PlayCard` / `Attack` 走正常那条）或**两个伪 kind**
//        （`RecKindTutorialDraw 201` / `RecKindTutorialAttackType 202`，与 `RecKindForfeit` 同族，
//         `PlayReplay` 里单独一支落回引擎）。
//      ⚠️ **「等玩家」的那 110 条不在 ③ 上** —— 它们由玩家自己做，照旧走 ①。
//    ⚠️ **怎么验「三个口都记全了」**：结算时记 `NetProtocol.StateHash(Ctx)`，回放完再算一次对比
//       （`BattleDriver.PlayReplay` 末了一段）；不一致就出声。教程局这条链的自检落点 =
//       `Editor/BattleScene.cs` 的「教程局」那一段（`回放 → 比终局局面哈希`）。
//
// ---- 🆕 2026-10-10（`A985⑬`）：**理由码走录像自己的两条记录，⛔ 不走 `MsgAction`** ----
//   背景：`BattleDriver.PlayReplay` 重放 `RecKindForfeit` 那一条时**拿不到理由码**（只能传
//   `BattleResult.Undefined`）。为什么不顺手加在动作上：`RecRaw` 写的是 `MsgAction`，而**同一结构也上网**
//   （`NetKind.Action`/`Applied` 发整个 `MsgAction`）⇒ 给 `MsgAction` 加字段 = **把码送上网络**
//   （原版 `BattleCommsManager.SendForfeit` 发的是 `System.Array.Empty<T>()` 空参数表、码**从来不过网**
//    —— 见 `RuleEngine/Core/RuleCore.cs` 里 `BattleResult` 的注）。
//   ⇒ 最小设计（判据 = `项目任务.md` 第 373 行 `A985` 尾巴 + §四·16）：**录像头尾各加一条【只落盘】的
//      `[BattleResult]` 记录** = `ReplayRecord.resultHead` / `resultTail` 两格**字符串**（⛔ 不进 `actions`、
//      ⛔ 不上网），**回放时读它**（`ReadResultCode`）。
//   ⚠️ **只有一处拼那行字**：`ReplayStore.ResultLine`（`WriteResultHead` / `WriteResultTail` 都走它）。
//      形状的唯一出处是 `RuleCore` 那两句 `ctx.Log($"[BattleResult] {reason}({(int)reason}) seat={player}")`
//      （`Forfeit` 与 `CheckWinner`）；录像侧这一份是**孪生**，两者同形由 `Editor/NetSelfTest.cs` 的断言
//      拿**字面量**钉住（⛔ 别在别处再拼一遍 —— 两份写法迟早不一致）。
//   ⚠️ **两个调用点（`RecBegin` 写头 / `RecFinish` 写尾）在 `BattleDriver.cs`** —— 本文件只提供读写口。
//      取码桥 = `LastResultFromEvents(Ctx.Events, out seat)`：码**不在 `ctx` 的任何字段上**
//      （`BattleResult` 的注：⛔ 不写进 `ctx`、免得旧录像/旧联机局假红），它只活在 `ctx.Events` 的那一行里。
//
// ---- 存在哪（用户 2026-09-27 问的「专门的文件夹」）----
// `Application.persistentDataPath/WarpforgeReplays/` —— 与 `NetConfig`（`WarpforgeNet.json`）·
// `DeckStore` 同一套（`JsonUtility` + 一个给自检用的 `OverrideDir`）。
// ⚠️ 编辑器里 `persistentDataPath` 在 `C:/Users/<你>/AppData/LocalLow/<公司>/<产品>/` ——
//    **看不见** ⇒ 每次存盘/读盘都 `Debug.Log` **完整路径**（用户要能找得到）。
// ⚠️ 出 player 之后这个路径才会变到真正的存档位置；**这是第一处会写用户数据的地方**，
//    所以：只写这一个目录、文件名里不带路径分隔符、写失败一律出声。
//
// ---- 留多少局（用户 2026-09-27 问的「50 局合不合理、多大」）----
// `MaxKept = 50`，**超出就删最旧的**（按文件名的时间戳排序 —— 文件名自带时间，不靠文件系统时间）。
// 📏 **实测一局多大**见 `ReplayStore.LastSavedBytes`（自检每次打印），别拍脑袋估。
//
// ---- 版本（`ReplayRecord.version`）----
// **规矩**：改结构【或改结算/对账语义】就 +1，读到不认识的版本**出声跳过**、⛔ 别硬解。
//
// 🔴 **2026-10-20（`W58`）：2 → 3** —— 本批改的是【结算语义】，不是文件结构：
//   · **为什么不兼容**：录像只存「起始条件 + 动作流」，**同一串动作在新引擎里会算出另一种局面**
//     ⇒ 旧档放出来是**另一局**。而旧的闸只在**终局 `StateHash` 不一致**时才出声 ——
//     那时整局已经演完了（正是审查 `P2-4` 那条）。⇒ 只能靠版本号在**入口处**拦。
//   · **版本 3 的改动清单**（每一条都会改变一次动作流的结算结果）：
//     ① **加时触发点**（判据从「能量」改成「全局回合号」⇒ 同一回合数下 `overtime` 的早晚变了）；
//     ② **死亡 / 触发次序**（D4：`Backlash → Unstable → Die 广播`；D5；`FlushDeaths` 逐死者交错记账）；
//     ③ **手牌上限时机**（先判上限、再 Add；满了送**刚进手牌的那张**去墓地，不回头截断旧牌）；
//     ④ **`Gain N Energy` 的语义**（只结算一次、改的是 `MaxEnergy`，不随目标数翻倍）；
//     ⑤ **受伤 / 攻击 / 部署 / 猎杀标记广播的「预调主体 + 排除」次序**；
//     ⑥ **伏击揭示**（删掉「回合开始自动揭示」⇒ 只在原版那 7 个 `SetAmbush(false)` 调用点翻面）。
//
// 🔴 **2026-10-23（引擎线「新账-88」）：3 → 4** —— 本批改的是【对账的尺子】，局面不变：
//   · `trace`（逐条 `BattleDriver.DeepHash`）与 `finalHash`（`NetProtocol.StateHash`）**换了** ——
//     `DeepHash` 的卡 id 从 `string.GetHashCode()`（进程随机化）换成 `NetProtocol.Hash`（跨进程稳定）；
//     `StateHash` 补了一批维度（信仰/灵魂石/任务点/疲劳/费用修正/抢来的单位/手牌记录/隐秘/附魔/
//     环境卡选择/牌库与弃牌**全量**…，清单在 `NetProtocol.PlayerHash` 的 doc 上）。
//   · ⇒ 旧档放出来会在**第 0 条**就报分叉（`rec.trace[i]` 跟新算法算出来的数永远不等）、末尾再报
//     「不是同一局」—— 那不是「演错了」，是**尺子换了**。按上面的规矩：**入口处拦掉**、如实说版本不认识。
//   · ⚠️ **这不是结算语义变化**：同一串动作在新引擎里算出的**局面**与 3 版相同。但**不升版本**的话，
//     玩家看到的是「回放第 0 条就 break + 一堆红字」而不是一句「版本不认」⇒ 升版本是更诚实的那一档。
//
// **旧版本怎么处理（现有逻辑，一字未改）**：`Load` 拿 `rec.version` 比 `new ReplayRecord().version`，
// 不等就 `Debug.LogWarning("[Replay] 版本 N 不认识（本代码只认 M）⇒ 不播：<完整路径>")`
// 并返回 **null**；`BattleDriver.PlayReplay(fileName)` 拿到 null 直接 `return false`，
// 列表那几处 `continue` ⇒ **不硬解、不会演成另一局**。⚠️ 这不是静默失败：警告带完整路径与两头的版本号。
// ⚠️ **`JsonUtility` 读不到 `version` 键时给 `0`**（`int` 的 CLR 默认值）⇒ 比当前值不等 ⇒ 同样走拒绝路。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CardPresentation.Net;
using RuleEngine;      // 🆕 2026-10-10（`A985⑬`）：`BattleResult`（理由码枚举）—— 同目录 / 同族的
                      // `BattleDriver.cs` / `EndPanel.cs` 等也都 `using` 它；本文件原来不用它，是因为
                      // 录像过去与理由码**一个字都不搭**（这账要的就是把它接上）。

namespace CardPresentation
{
    /// <summary>一局录像。**头 = `MsgStart`（种子/模式/双方卡组/战场/名字/先手），身子 = 动作序列**。</summary>
    [Serializable]
    public class ReplayRecord
    {
        /// <summary>格式版本。**改结构【或改结算语义】就 +1**，读到不认识的版本**出声跳过**，别硬解。
        /// 🔴 **2026-10-01：1 → 2** —— 棋盘从「固定 9 格、可留洞」改成**连续无洞**模型
        /// （`RuleEngine/Core/BoardSlots.cs`），**同一串动作在新引擎里会落到不同的格号**
        /// （旧档里「打到 0 号格」现在会夹到最里那一格）⇒ 旧档放出来是**另一局**，必须拒绝而不是硬解。
        /// 🔴 **2026-10-20（`W58`）：2 → 3** —— 本批改了一整串**结算语义**（加时触发点 · 死亡/触发次序 ·
        /// 手牌上限时机 · `Gain N Energy` · 广播预调 · 删掉回合开始自动揭示伏击）⇒ 旧档同样会演成另一局。
        /// 🔴 **2026-10-23（引擎线「新账-88」）：3 → 4** —— **对账的尺子换了**（`DeepHash` 改稳定哈希 +
        /// `StateHash` 补维度）⇒ 旧档会在第 0 条报分叉、末了报「不是同一局」。**不代表旧档演错了**。
        /// 完整清单与「旧版怎么被拒」→ **本文件头部「版本」那一节**（判据只写一份，⛔ 别在这里抄第二份）。</summary>
        public int version = 4;
        /// <summary>存盘时刻（`yyyy-MM-dd HH:mm:ss`，**本地时间**，只给人看）。</summary>
        public string savedAt = "";
        /// <summary>本机那局的座位（**绝对编号**）—— 放的时候要 `FromStart(start, isHost: mySeat == 0)`。</summary>
        public int mySeat;
        /// <summary>结果，照 `BattleLogData.Outcome`：0 胜 / 1 负 / 2 平。</summary>
        public int result;
        /// <summary>🔴 **结算那一刻的引擎指纹** = `NetProtocol.StateHash(Ctx)` —— **就这一个**。
        /// 放的时候再算一次对比：**不相等就说明有动作没录全**，回放看到的是**另一局**
        /// （判据与用法 → `BattleDriver.PlayReplay`；这是「不许静默失败」那条红线的落地）。
        ///
        /// <para>⚠️ **2026-10-18（铁律 5 订正）**：本行原文写的是 `NetProtocol.Fingerprint(Ctx)` —— **是错的**，
        /// 而**本文件头部那句**（「怎么验『三个口都记全了』」那一段）写的是 `StateHash`，**同文件两说打架**。
        /// 实测**两头本来都是 `StateHash`**，错的自始至终**只有本行这个 `Fingerprint` 字样**。</para>
        /// <para>🔴 **判据（两个口都在 `Net/NetProtocol.cs`，⛔ 只差一格、别认错）**：
        /// 写入 = `BattleDriver.RecFinish`（`_rec.finalHash = NetProtocol.StateHash(Ctx)`）；
        /// 比对 = `BattleDriver.PlayReplay` 末尾那一次 `int got = NetProtocol.StateHash(Ctx);`
        /// （**按符号找，⛔ 别记行号** —— 行号每轮都变）。</para>
        /// <para>🔴 **为什么不能用 `Fingerprint`（`StateHash` 自己的 doc 写着）**：`Fingerprint` 把表现层的
        /// 待办队列也算进哈希，而表现层随时在抽干它 ⇒ **同一个局面在「刚做完动作」与「表现层跑过一轮」
        /// 两个时刻 `Fingerprint` 不相等** —— 拿它当录像指纹会**假红**（明明同一局却报「不是同一局」）。
        /// ⛔ **录像一律 `StateHash`**；这一条**不用再怀疑**。</para></summary>
        public int finalHash;
        /// <summary>🔴 **2026-10-24（执行代理 `W9`）：开局那一刻的全量状态指纹** =
        /// `NetProtocol.StateHash(Ctx)`（**与终局那一个同一个口径**）。写入 = `BattleDriver.RecBegin`
        /// （`RuleCore.NewBattle` 之后、**第一条动作之前**）；比对 = `BattleDriver.PlayReplay` 的
        /// `ReplayFingerprintTrace.CheckStart`（它内部因此能**区分**「开局就不同源」与「中途分叉」两种病 ——
        /// 判据 → `RuleEngine/Core/ReplayFingerprintTrace.cs` 的文件头）。
        ///
        /// <para>⚠️ **`0` = 「没记」**（老录像的 JSON 里没有这个键）—— 这一格**不单独判**：
        /// 「这份录像有没有逐动作全量指纹」一律看 <see cref="traceFull"/> 有没有内容
        /// （两列**同生同死**，写点只有 `RecFinish` 一处）。⛔ 别拿 `startFullHash == 0` 当「没记」的判据
        /// —— 一个合法的 `StateHash` 本来就可能算成 0。</para>
        /// <para>⚠️ **`version` 不 +1**（纯新增字段，同 `tutorialStage` / `botSeatPlus2` / `mulliganPlus1`）：
        /// 老录像读出来是「没记」= 它本来的行为（下面 <see cref="traceFull"/> 那一档）。
        /// 「改结构就 +1」那条防的是**同一格换语义**。</para></summary>
        public int startFullHash;
        /// <summary>🔴 **2026-10-24（执行代理 `W9`）：逐动作【全量状态】指纹** —— `traceFull[i]` =
        /// **第 i 条动作落地之后**的 `NetProtocol.StateHash(Ctx)`（**与终局判据同一个口径**）。
        ///
        /// <para>🔴 **与既有的 `trace`（`DeepHash`）并存、分工不同**（⛔ 别把任何一个换掉）：
        /// · <see cref="trace"/> = 细粒度、**含实例号**，定位更细（还有字面量锚点钉着）；**保留不动**；
        /// · 本列 = 与**终局**同口径 —— 回答的是「轨迹摘要相同、终局却不同」那种病（真机 `F7` 的实测形状），
        ///   以及 `DeepHash` 覆盖不到的那几维（信仰 / 灵魂石 / 任务点 / 已出隐秘 / 疲劳 / 能量上限 /
        ///   费用修正 / 手牌效果记录 / 附魔计数 / 残骸与 `Exhausted` / 弃牌堆身份 …）。
        /// 采样点**必须与回放侧同一个**（录制 = `RecAct`/`RecRaw`/`RecMsg` 里紧挨着 `trace.Add(DeepHash)`
        /// 那一句；回放 = `BattleDriver.ApplyReplayActionAt` 里动作落地之后）⇒ 两边逐条对得上。</para>
        ///
        /// <para>🔴 **`null` / 空 = 「这份录像没有逐动作全量指纹」**（老档、录制端没接上这一列）
        /// ⇒ 回放侧 `ReplayFingerprintTrace.ForReplay` 会置 `NoExpectedTrace = true`，
        /// `Finish` **出声说明「本层对账没做」** —— ⛔ **那不是「通过」**（红线：不许静默失败）。
        /// 缺键的 JSON 读出 `null`（`int[]` 的 CLR 默认值 = 初始化器的值 = `null`，两条路相同）。</para>
        /// <para>⚠️ **`version` 不 +1**（同 <see cref="startFullHash"/> 那条）。</para></summary>
        public int[] traceFull;
        /// <summary>两个督军名（**列表里显示用**；引擎不看它）。</summary>
        public string myHero = "", foeHero = "";
        /// <summary>开局参数（**重建这一局要的全部东西**）。</summary>
        public MsgStart start;
        /// <summary>🆕 2026-10-18（A938）：**这一局是教程第几关**（`stage`，**1..6**；
        /// **`0` = 不是教程局**）。
        ///
        /// 🔴 **为什么非记不可**：教程局的开局**不是**「种子 + 两副牌」能重建出来的 ——
        ///   它少洗一次牌、起始单位是关卡摆的、起手是关卡点名的那几张、连「谁先手」都照关卡
        ///   （`RuleCore.NewBattle` 那六条开关**全读 `ctx.Tutorial`**）。
        ///   ⇒ 不记这一格，回放放出来的**根本不是同一局**（棋盘一开始就不一样）。
        ///   ⚠️ 它**不属于 `MsgStart`**（那个结构在 `Net/NetProtocol.cs`，是联机协议的一部分，
        ///     原版也没有「教程关号」这个字段）⇒ 放在录像自己的头里。
        ///
        /// 🔴 **哨兵取 `0` 而不是 `-1`（审查 R10/K7 的整改）**：这一格是**新加的**，老录像的 JSON 里
        ///   **没有这个键**，读出来是什么取决于 `JsonUtility.FromJson` 会不会跑字段初始化器。
        ///   而如果哨兵写成 `-1`，那一刻**所有老录像都会被当成「教程第 1 关」放**（静默错）。
        ///
        /// <para>🔴 **2026-10-24（`A8`）就地结案 —— 原来这里写「本地没有判据」，现已查清**：
        ///   `JsonUtility.FromJson` **会**跑目标类型的**无参构造**（因而跑字段初始化器）⇒
        ///   **缺键的字段留着初始化器写的值**，⛔ **不是 `default(T)`**。
        ///   · **引擎侧判据**（两处互相印证）：① `Unity 6000.3.4f1` 实跑（`kan-kikuchi.hatenablog.com/entry/JsonUtility_Ctor`，
        ///     2026-03-05，与本工程同一个 6000.3 小版本线）；② `Unity 6000.6 / 7000` 的
        ///     `JsonUtility.FromJson` 官方 ScriptReference 已把旧版那句「构造函数不执行」**更正**成
        ///     「有**无参构造**就调它、字段初始化器一起跑；**没有**则字段落 `default`」。
        ///   · ⚠️ 上面两条都是**引擎行为**、不是本工程的数据判据 ⇒ 本工程自己的**封印** =
        ///     `BattleScene.Run` 里的 `★ A8 JsonUtility 初始化器`（探针字段的初始化器**故意 ≠ 默认值**；
        ///     缺键读出来不是初始化器值就红 —— 将来 Unity 换版本改了这条行为，它会当场出声）。
        ///   · ⇒ **本条哨兵纪律照旧成立、而且更结实**：`0` **既是 `int` 的 CLR 默认值、也是本字段初始化器写的值**
        ///     ⇒ 「初始化器跑不跑」**两条路都给 `0`**。</para>
        ///   ⇒ 把「不是教程局」定成 **`0`（= `int` 的默认值）**：**初始化器跑不跑都给 `0`**，
        ///     这条兼容**两条路都成立**。⛔ 别把它改成 `-1`，也别改成 `0` 表示别的意思。
        ///   用 <see cref="TutorialStageIndex"/> 读它（那里面才是「0..5 的关号 / -1 = 不是教程局」的换算）。
        ///   ⚠️ **`version` 不 +1**：这是**纯新增**字段，老录像读出来是 `0` = 「不是教程局」
        ///     —— 正好是它们本来的行为。与该字段那条「改结构就 +1」的规矩不冲突（那条防的是**同一格换语义**）。</summary>
        public int tutorialStage = 0;

        /// <summary>教程关号（`tutorialIndex`：**0..5**；**`-1` = 不是教程局**）—— 唯一读法。
        /// ⚠️ 它是**属性**：`JsonUtility` 只序列化**字段**（public 字段 / `[SerializeField]`）⇒
        ///    它不会进 `.json`，也不会在 `FromJson` 时被写（所以不存在「读出来是 0」那一类问题）。</summary>
        public int TutorialStageIndex { get { return tutorialStage >= 1 ? tutorialStage - 1 : -1; } }

        /// <summary>🆕 2026-10-19（`A1087`）：**这一局「哪一个座位是电脑」** —— 直接照
        /// `RuleCore.NewBattle(botSeat:)` 的编码，但**整体 +2**（把 `0` 空出来当哨兵）：
        ///   `0` = **没记**（老录像的 JSON 里没有这个键）· `1` = `-1`（**这一局没有电脑** —— 联机的实情）·
        ///   `2` = `0` · `3` = `1`。**唯一读法** = <see cref="BotSeatForNewBattle"/>。
        ///
        /// 🔴 **为什么非记不可**：这一格在引擎里**有后果** —— `RuleCore.GoesSecondCard` 拿
        ///   `ctx.IsBotSeat(seat)` 决定「后手那张防御卡从哪来」：**是电脑** ⇒ 从后手方阵营防御池
        ///   **随机**取（`RandomSecondSeatDefence`，**吃 `ctx.Rng`**）；**是真人** ⇒ 用卡组自带那张。
        ///   而它原来的判据是 `BattleDriver.AiShouldDriveOpponent`（`= _net == null`）——
        ///   **放录像时 `_net` 恒 null** ⇒ 一份记着「-1（对手是真人）」的录像放出来会被当成
        ///   「1（座位 1 是电脑）」⇒ 后手那张卡不同 + 随机流错位 ⇒ **回放演成另一局**。
        ///
        /// 🔴 **哨兵取 `0` 而不是 `-1`**（与 <see cref="tutorialStage"/> 同一条纪律）：这一格是**新加的**，
        ///   老录像的 JSON 里**没有这个键** ⇒ 只有把「没记」定成 **`0`（= `int` 的默认值）**，两条路才都给 `0`。
        ///   🔴 **2026-10-24（`A8`）结案**：原来这里写「`JsonUtility` 跑不跑初始化器**本地没有判据**」——
        ///   **已查清**：它**会**跑无参构造（⇒ 跑字段初始化器）⇒ 缺键字段**留着初始化器值**、⛔ 不是 `default(T)`。
        ///   证据 + 本工程自己的封印断言 → 见 <see cref="tutorialStage"/> 那段 2026-10-24 的注。
        ///   ⇒ 本条照旧正确：`0` **既是默认值、也是初始化器值** ⇒ 两条路都给 `0`。
        ///   ⛔ **别存引擎值本身** —— `-1`/`0`/`1` 三个都合法，`0` 会被哨兵吃掉 ⇒ 所以存**值 + 2**。
        ///   ⚠️ **`version` 不 +1**：纯新增字段，老录像读出来 `0` = 「没记」= 它本来的行为（同 `tutorialStage`）。</summary>
        public int botSeatPlus2 = 0;

        /// <summary>读 <see cref="botSeatPlus2"/> 的**唯一**读法 —— 要喂 `RuleCore.NewBattle(botSeat:)` 的那个值。
        /// `null` = **这份录像没记**（老录像）⇒ 调用方退回老口径，⛔ **别拿 `null` 当「没有电脑」**。
        /// ⚠️ 它是**属性**（`JsonUtility` 只序列化字段 ⇒ 不进 `.json`、也不会在 `FromJson` 时被写，同 `TutorialStageIndex`）。</summary>
        public int? BotSeatForNewBattle { get { return botSeatPlus2 == 0 ? (int?)null : botSeatPlus2 - 2; } }

        /// <summary>🆕 2026-10-24（执行代理 `A2` · `F2` 顺手发现 3）：**开局有没有换牌阶段**
        /// （= 录制那一刻 `BattleDriver.mulliganEnabled` 的值）—— 直接照 `tutorialStage` / `botSeatPlus2`
        /// 的哨兵纪律，存**值 + 1**：`0` = **没记**（老录像）· `1` = **false** · `2` = **true**。
        ///
        /// <para>🔴 **为什么非记不可**：`mulliganEnabled` **不在 `MsgStart` 里**（那是联机协议结构，
        /// 原版也没有这一格 —— ⛔ 往那儿加字段 = 把码送上网），所以回放原来**要靠 driver 当时的字段值**。
        /// 产品那条路今天恰好安全（场景里存着 `mulliganEnabled: 1`，
        /// `CardPresentation/Scenes/Battle.unity:2527`），但**夹具很容易踩**：
        /// 一份「录制时开着换牌」的录像，在 `mulliganEnabled` 默认 **false** 的 self-check driver 上放
        /// ⇒ 开局少一轮换牌 ⇒ 动作流从第一条就错位（判据 → `资料/普查产出_1023e/F2_战斗红修复.md`
        /// 的「顺手发现 3」）。⇒ 把这一位**记进录像头**，回放时按它恢复。</para>
        ///
        /// <para>⚠️ **哨兵取 `0` 而不是 `-1`**（与 `tutorialStage` / `botSeatPlus2` 同一条纪律）：
        /// 老录像的 JSON 里没有这个键 ⇒ 只有把「没记」定成 `0`（= `int` 的默认值），两条路都给 `0`。
        /// 🔴 **2026-10-24（`A8`）结案**：原来这里写「`JsonUtility` 跑不跑初始化器**本地没有判据**」——
        /// 已查清：它**会**跑无参构造（⇒ 跑字段初始化器）⇒ 缺键字段留着初始化器值。
        /// 证据与封印断言 → 见 `tutorialStage` 那段 2026-10-24 的注。⇒ `0` **既是默认值、也是初始化器值**。</para>
        /// ⚠️ **`version` 不 +1**：纯**新增**字段，老录像读出来 `0` = 「没记」= 它本来的行为（退回
        /// 「用 driver 当时的字段值」）。⛔ 别把它改成 `bool`（`bool` 默认 `false` 会和「真的 false」混）。</para></summary>
        public int mulliganPlus1 = 0;

        /// <summary>读 <see cref="mulliganPlus1"/> 的**唯一**读法。
        /// `null` = **这份录像没记**（老录像）⇒ 回放时退回「driver 当时的字段值」；
        /// `true` / `false` = 录制那一刻的实际值。⛔ **别拿 `null` 当 `false`**。</summary>
        public bool? MulliganEnabledForReplay { get { return mulliganPlus1 == 0 ? (bool?)null : mulliganPlus1 == 2; } }

        /// <summary>🔴🆕 **2026-10-25（执行代理 `C` · 账 `RE1` §1.4-A2）：双方【实际用的牌】的稳定卡 id。**
        ///
        /// <para>**为什么非记不可**：录像头原来只有 `MsgStart.hostDeckJson/clientDeckJson`（= **入口参数**，
        /// 即「玩家存档里那副 `PlayerDeck`」），而**引擎自动凑的那副一张都没记** ⇒ 回放时靠
        /// `StarterDeck`/`ResolveDeck` **重新算一遍**。原版不是这样：`BattleRecordData` **双方各存一份牌组**
        /// （`playerData +0x40` / `oppData +0x38`，`DUMP:77673`；映射见 `资料/普查产出_0927/回放_入口与数据链.md` §C·①）。
        /// 后果（今天可复现）：`StarterDeck` 顺序 / 卡池 / 补防御卡逻辑一改，**旧档就静默演成另一局**
        /// —— 终局指纹会报「不是同一局」，但没人知道是「录像过期」还是「引擎分叉」。</para>
        ///
        /// <para>**取值 = `Begin` 里解析完成、真正喂给 `RuleCore.NewBattle` 的那两份 `List&lt;CardDef&gt;`**
        /// 的 `CardDef.Id`（含督军卡与防御卡 —— 引擎按 `Type` 分流，尺寸/顺序都要原样）。
        /// 回放时按 id 展开成 `CardDef` 列表，走 `Begin(exactMine:, exactFoe:)` **原样重建**。</para>
        ///
        /// <para>⚠️ **`null` / 空 = 「没记」**（老录像）⇒ 回放退回「按 `MsgStart` 的卡组重算」那条老路
        /// （行为与加这两列之前**逐字相同**）。⚠️ **`version` 不 +1**：纯**新增**字段（纪律同
        /// `tutorialStage` / `botSeatPlus2` / `mulliganPlus1`）；「改结构就 +1」那条防的是**同一格换语义**。
        /// ⛔ **不进 `MsgStart`** —— 那是联机协议结构，往里加字段 = 把码送上网。</para></summary>
        public List<string> hostCardIds;
        /// <summary>见 <see cref="hostCardIds"/>（这一列 = 座位 1 / client）。`null` = 没记。</summary>
        public List<string> clientCardIds;

        /// <summary>🆕 **2026-10-10（`A985⑬`）：录像【头】那一条 `[BattleResult]` 记录**（**只落盘**：
        /// ⛔ 不进 `actions`、⛔ 不上网 —— 理由见本文件头部那一节）。
        ///
        /// <para>语义 = **开局那一刻**（`WriteResultHead`，调用点 `BattleDriver.RecBegin`）。那一刻**还没有结果**
        /// ⇒ 正常是 <see cref="BattleResult.Undefined"/>(0) 那条（`ResultLine` 不写 `seat=`）。
        /// 它同时是**「这份录像是从头开始记的」**那个记号（读 <see cref="ReplayStore.HasResultHead"/>）。</para>
        ///
        /// <para>🔴 **空串 / `null` 都表示「没记」**（老录像、以及还没接上写点的那条路）—— 与
        /// <see cref="tutorialStage"/> 那条**同一条哨兵纪律**：老录像的 JSON 里**没有这个键** ⇒ 把「没记」
        /// 同时定成 **`""`（初始化器的值）与 `null`**，两档都安全。
        /// 🔴 **2026-10-24（`A8`）结案**：原来这里写「`JsonUtility` 跑不跑初始化器**本地没有判据**」——
        /// **已查清：它跑**（无参构造 ⇒ 字段初始化器）⇒ **缺键这一格读到的是 `""`（初始化器值）**，不是 `null`。
        /// 证据 → 见 `tutorialStage` 那段 2026-10-24 的注。
        /// ⇒ ⛔ **别把 `null` 那一档删掉**：它现在不是「另一种引擎行为」，而是**显式写 `null` 的老/坏数据**那一档
        /// （以及 `string` 的 CLR 默认值 —— 别的序列化路径仍可能给）。两档都当「没记」是**更宽的安全档**。</para>
        /// ⛔ 别把「没记」改成别的写法（`"-"` 之类），也别拿空串当「未指定」那一档用
        /// ——「未指定」有它自己的写法（`[BattleResult] Undefined(0)`）。</para>
        /// ⚠️ **`version` 不 +1**：纯**新增**字段（同 `tutorialStage` / `botSeatPlus2`），老录像读出来是「没记」
        /// = 它本来的行为；「改结构就 +1」那条防的是**同一格换语义**。</summary>
        public string resultHead = "";
        /// <summary>🆕 **2026-10-10（`A985⑬`）：录像【尾】那一条 `[BattleResult]` 记录**（**只落盘**，同上）。
        ///
        /// <para>语义 = **结算那一刻**（`WriteResultTail`，调用点 `BattleDriver.RecFinish`）= **这一局是「怎么」结束的**
        /// （`Forfeit`(2) 投降 / `Disconnect`(3) 掉线 / `BattleVictory`(1) 督军倒下 / `Cancelled`(5) 取消匹配…）。
        /// 存进去的是 <see cref="ReplayStore.ResultLine"/> 拼的那一行，**回放时用
        /// <see cref="ReplayStore.ReadResultCode"/> 读它**（`PlayReplay` 重放 `RecKindForfeit` 时喂给引擎，
        /// ⛔ 别再传 `Undefined`）。</para>
        ///
        /// <para>🔴 **空串 / `null` = 「没记」**（纪律同 <see cref="resultHead"/>）。⚠️ **`version` 不 +1**（纯新增字段）。</para>
        /// </summary>
        public string resultTail = "";

        /// <summary>动作流（含换牌那两条：`kind = 100/101`）。</summary>
        public List<MsgAction> actions = new List<MsgAction>();
        /// <summary>🆕 2026-09-27：**逐条动作之后的引擎指纹**（`trace[i]` = 第 i 条动作落地后的状态）。
        /// 用途只有一个但很关键：**回放时找「第一条对不上的动作」** —— 光看终局指纹只知道「不一样」，
        /// 不知道**从哪一步开始不一样**（2026-09-27 就是靠它把分叉点钉出来的）。
        /// ⚠️ 它让每局多约 `4×N` 字节（N=50 时约 200 字节/局）—— 值。</summary>
        public List<int> trace = new List<int>();
        /// <summary>🔴 **诊断用**（2026-09-27）：每条动作落地后 `ActionLog` 尾那一句。
        /// 光看哈希只能知道「哪一条分叉」，看这一句才知道**引擎当时干了什么不一样的事**。
        /// ⚠️ 正式收口时可以去掉（它让每局多十几 KB）—— 留着的话就当「出问题时的黑匣子」。</summary>
        public List<string> traceLogTail = new List<string>();
        /// <summary>🆕 **诊断用**（2026-09-27）：每条动作落地后的**局面速写**
        /// （`BattleDriver.StateBrief`：双方每格的卡名+血+有没有动过、手牌/牌库/弃牌张数、能量、
        /// 任务点、督军血、问了几次选择、面板答案队列还剩几条）。
        ///
        /// 为什么要它：`trace`（哈希）只能把分叉**定位到第几条**，看不出「哪里不一样」——
        /// 2026-09-27 实测：分叉点那一行光有两个 `int`（`827436913` vs `-23128108`），
        /// 人读不出「录的时候 2 号格站着 `Ravenwing Bikes`、回放时那一格是空的」。
        /// ⚠️ 与 `traceLogTail` 一样是**黑匣子**，正式收口时可以一起去掉（每局约 +5 KB/50 条）。</summary>
        public List<string> traceState = new List<string>();
    }

    /// <summary>本地录像库（一局一个 `.json`）。</summary>
    public static class ReplayStore
    {
        public const string DirName = "WarpforgeReplays";
        public const string Ext = ".json";

        /// <summary>最多留几局（**超出就删最旧的**）。用户 2026-09-27 定的是 **50**。
        /// 📏 一局多大见 `LastSavedBytes`；50 局的总占用 = 50 × 那个数。</summary>
        public const int MaxKept = 50;

        // ============================================================ 「有没有回放」的门槛（原版 `replaysVersion`）
        //
        // 🔴 **原版判据（第一权威 = 反编译 + 字符串字面量表，2026-10-25 亲读）**：
        //   · 客户端**唯一**使用点 = `BattleLogItem__Initialize.c:206`：
        //       replayButton.interactable = (match.replaysVersion != <const>)
        //   · `<const>` = `DAT_184288ae8` → RVA `0x4288AE8` → `il2cpp_out/stringliteral.json` = **`"-1"`**。
        //   · ⇒ **`"-1"` = 没有回放**（钮不可点）；**其它任何值**（真版本号 / 录像 id）= 有回放。
        //     原版**只**跟 `"-1"` 比：`""` 与 `null` 在它那儿**都算「有」**（服务端总会下发一个值）。
        //   · 字段来源 = `BattleLogMatch.replaysVersion`（`BattleLogMatch.cs:42`，`+0x78`），**服务端 → 客户端**；
        //     客户端**不做**「本机版本 vs 服务端版本」的比较（`ServerVersion(replaysVersion)` 那两个
        //     字段在客户端**没有读取点**，只有遥测模板）。
        //
        // 🔴 **我们这一侧的映射（如实标注：这不是原版，是等价物）**：我们没有服务端，判据是**本地录像文件名**
        //   （`BattleLogData.Match.ReplayFile`）⇒「没有」= **`null` / 空串**；另外把原版那个哨兵 `"-1"`
        //   **一并认**（将来若从远端拿回真的 `replaysVersion`，直接喂进来即可）。
        //   ⛔ **别把 `ReplayRecord.version`（文件格式版本，现读 4）当这一格** —— 两件事，
        //     `C5_引擎战斗剩余判据.md` Q5.3 明写「别把两条账混在一起」。
        //   ⚠️ 「灰」的落地：原版那颗钮 `m_Transition = 2 (SpriteSwap)`，`m_SpriteState.m_DisabledSprite`
        //     **有值**（pid `5693181797853584851`，与 `m_SelectedSprite` 同一颗）——
        //     但**那个 pid 反查不到名字**（`资料/普查产出_0927/回放_界面真值.md:221` 已记「只查到 pid、没查到名」）
        //     ⇒ 我们**拿不到原版那张「禁用态」图**（铁律 11 例外①：判据是空的）。
        // 🔶 自己设计（非原版）：禁用态回放钮「不建命中区 ＋ 出声」的处置 · 原版禁用态 sprite 只查到 pid、反查不到名字（资料/普查产出_0927/回放_界面真值.md:221） · TODO(谁定的)｜原注：我们这一档的处置 = **不建命中区（点不到）** ＋ **出声**；要灰就只能是「我们挑的」灰，
        //     ⛔ 别把它写成「原版灰」。
        public const string NoReplaySentinel = "-1";

        /// <summary>= 原版 `replayButton.interactable = (replaysVersion != "-1")`（`BattleLogItem__Initialize.c:206`）。
        /// <para>`replaysVersionOrFile` 可以喂原版那格版本串，也可以喂我们的本地录像文件名 ——
        /// 两者「没有」的判据见 <see cref="NoReplaySentinel"/> 那段注释。｜🔴 **2026-10-29（`B95_WBD`）就地订正 —— 本方法【不是】`MatchData.SetMatchRecording` 的等价物**（账 `战斗逻辑驱动:1081` 的原落点指错了）：`SetMatchRecording(BattleRecordData, PlayerDataManager)` = 「**录像 → 装成一份 MatchData 再开局**」（唯一调用点 `Everguild.MatchMakerManager__StartReplayMatch.c:50`），我们的等价物 = `BattleDriver.PlayReplay`（经 `NetPendingBattle.FromStart` + `TryExpandRecordedDecks` 重建这一局）＋本文件的 `Load`；本方法只对应 `BattleLogItem.Initialize` 那颗回放钮的**可点态**。</para></summary>
        public static bool ReplayAvailable(string replaysVersionOrFile)
            => !string.IsNullOrEmpty(replaysVersionOrFile) && replaysVersionOrFile != NoReplaySentinel;

        static readonly HashSet<string> _noReplayWarned = new HashSet<string>();

        /// <summary>「这一行没有回放 ⇒ 回放钮不可点」—— **出声一次**（每个 `where|值` 一条，免得每帧刷日志）。
        /// <para>⛔ 红线：**不许静默**。原版 `interactable = false` 是静默的，但本仓规矩是「没做/不可用要说出来」。</para></summary>
        public static void NoteNoReplay(string replaysVersionOrFile, string where)
        {
            string k = (where ?? "") + "|" + (replaysVersionOrFile ?? "<null>");
            if (!_noReplayWarned.Add(k)) return;
            Debug.LogWarning("[Replay] " + (where ?? "回放钮") + "：**这一行没有回放**（判据 `"
                           + (replaysVersionOrFile ?? "<null>") + "`）⇒ 回放钮按原版**不可点**"
                           + "（`BattleLogItem__Initialize.c:206`：`interactable = replaysVersion != \"-1\"`；"
                           + "我们这一侧的映射：空 / `-1` = 没有）");
        }

        /// <summary>自检用：清掉「没有回放」的去重表（每个宿主开头调一次，免得跨用例互相屏蔽）。</summary>
        public static void ResetNoReplayWarningsForTest() { _noReplayWarned.Clear(); }

        /// <summary>🔴 **黑匣子开关**：开着才逐条记 `traceLogTail` / `traceState`（那两份字符串速写）。
        /// 关着只留 `trace`（每条 4 字节的哈希，够定位到「第几条分叉」）。
        /// 📏 实测：45 条动作 —— **关 9.0 KB / 开 23.1 KB**（差的那十几 KB 全是这两份速写）。
        /// ⇒ **平时关**（真打的录像要省），**自检与查问题时开**。
        /// ⚠️ 开不开**不影响回放能不能演**（`trace` 总是记的）。</summary>
        public static bool VerboseTrace;

        /// <summary>自检用的目录改写口（设了就不碰玩家的真录像）—— 与 `NetConfig.OverridePath` 同一套路。</summary>
        public static string OverrideDir;

        /// <summary>🔴 **只给自检**：把「玩家目录的根」也改写掉（`Application.persistentDataPath` 本身改不了）。
        /// <para>用途只有一个：`Editor/ReplayStoreGuardAssertE11.cs` 要验证 <see cref="ResetForTest"/> 的
        /// **「没设 `OverrideDir` 就拒绝」那道硬挡** —— 那条路必须**真的**在 `OverrideDir = null` 下走一次。
        /// 若让 <see cref="Dir"/> 那时直接落到玩家的真目录，万一哪天硬挡被人删掉，**这条断言自己就会删掉玩家的真录像**
        /// （＝它本想防的那个事故，2026-10-24 实录 → `资料/普查产出_1024c/RE1_战斗回放完整审查.md` B1）。
        /// ⇒ 把「根」指到临时目录，「删不到玩家真档」就**由目录结构保证**，而不是靠断言写得小心。</para>
        /// <para>⚠️ 只许自检用，用完**必须还原成 null**（`Save` / `List` / `Load` 全都走 <see cref="Dir"/>）。</para></summary>
        public static string PlayerRootForTest;

        /// <summary>🔴 **跨场景交接**：下一局战场要放哪一份录像。与 `NetPendingBattle.Current` 同一条路数
        /// （`LoadScene` 会清掉普通静态量之外的东西 ⇒ 只能这么带过去）。
        /// ⚠️ 战场那边**取一次就清**（`TakePending`），别留着 —— 不然「放完一局再开新局」会又放一遍。</summary>
        public static ReplayRecord PendingPlay;

        public static ReplayRecord TakePending() { var p = PendingPlay; PendingPlay = null; return p; }

        /// <summary>最近一次写盘的字数（自检与「50 局占多大」都用它，别猜）。</summary>
        public static long LastSavedBytes { get; private set; }
        /// <summary>最近一次写盘的文件名。</summary>
        public static string LastSavedFile { get; private set; }

        /// <summary>录像目录（**日志里会把完整路径打出来** —— 编辑器里它在 AppData 深处，不好找）。</summary>
        public static string Dir
        {
            get
            {
                // 玩家目录的「根」：平时就是 `persistentDataPath`；只有自检验 `ResetForTest` 的硬挡时才被改到临时目录
                // （见 `PlayerRootForTest` 的注 —— 那一格不是产品路径，⛔ 运行时没人设它）。
                var root = string.IsNullOrEmpty(PlayerRootForTest)
                         ? Application.persistentDataPath
                         : PlayerRootForTest;
                var d = string.IsNullOrEmpty(OverrideDir)
                      ? Path.Combine(root, DirName)
                      : OverrideDir;
                return d;
            }
        }

        // ---------------------------------------------------------- 写

        /// <summary>存一局。返回文件名（失败返回 null 并**出声**）。存完顺手按 `MaxKept` 修剪。</summary>
        public static string Save(ReplayRecord rec)
        {
            if (rec == null) { Debug.LogError("[Replay] `Save(null)` —— 不存"); return null; }
            try
            {
                Directory.CreateDirectory(Dir);
                string name = FileNameFor(rec);
                name = UniqueNameFor(name);              // 🔴 2026-10-25（C · RE1 §3-E3）：同一秒不许互相覆盖
                string path = Path.Combine(Dir, name);
                string json = JsonUtility.ToJson(rec);           // **紧凑格式**（不 prettyPrint：省一半）
                File.WriteAllText(path, json);
                LastSavedBytes = new System.Text.UTF8Encoding(false).GetByteCount(json);
                LastSavedFile = name;
                Debug.Log($"[Replay] 已存：`{path}`（{LastSavedBytes / 1024f:F1} KB · "
                        + $"{rec.actions.Count} 条动作 · {rec.myHero} vs {rec.foeHero}）");
                TrimToLast(MaxKept);
                return name;
            }
            catch (Exception e)
            {
                // 🔴 写盘失败**必须出声**（红线）—— 玩家以为录上了、其实没有，是最坏的一种静默
                Debug.LogError("[Replay] 写盘失败：" + e.Message + "（目录 " + Dir + "）");
                return null;
            }
        }

        /// <summary>文件名：`时间_我_vs_敌.json`。⚠️ 名字里**只留字母数字**（督军名里可能有空格/斜杠，
        /// 带进去会变成子目录或非法路径）。
        /// <para>🆕 2026-10-25（执行代理 `C` · `RE1` §3-E3）：**自检可以钉住「现在」** ——
        /// `ClockForTest` 非 null 时用它（⛔ 生产恒 null）。用途 = 让「同一秒存两局」这条断言**不跨秒翻车**。</para></summary>
        public static Func<DateTime> ClockForTest;

        static string FileNameFor(ReplayRecord rec)
        {
            var now = ClockForTest != null ? ClockForTest() : DateTime.Now;
            string t = now.ToString("yyyyMMdd_HHmmss");
            return t + "_" + Safe(rec.myHero) + "_vs_" + Safe(rec.foeHero) + Ext;
        }

        /// <summary>🔴🆕 **2026-10-25（执行代理 `C` · `RE1` §3-E3）**：同名时加 `_2` / `_3` …（**并出声**，⛔ 不静默覆盖）。
        /// <para>病灶：文件名精确到**秒**，同一秒里打两局、双方督军名又相同 ⇒ 后写**覆盖**前一份
        /// （用户数据静默丢失）；而 `battlelog.json` 会留下**两行指向同一个文件**。</para>
        /// <para>⚠️ 后缀法（不是「加毫秒」）：毫秒只是把窗口缩小 1000 倍、同一毫秒仍可能撞；后缀是**确定性**的。</para></summary>
        static string UniqueNameFor(string name)
        {
            string path = Path.Combine(Dir, name);
            if (!File.Exists(path)) return name;
            string stem = Path.GetFileNameWithoutExtension(name);
            for (int i = 2; i < 1000; i++)
            {
                string candName = stem + "_" + i + Ext;
                if (File.Exists(Path.Combine(Dir, candName))) continue;
                Debug.LogWarning($"[Replay] 同名录像已存在（`{name}`）⇒ 这一局改存 `{candName}`"
                               + "（同一秒两局、双方督军名又相同的形状；⛔ 绝不覆盖前一份）");
                return candName;
            }
            string ticks = DateTime.Now.Ticks.ToString();
            string fallback = stem + "_" + ticks + Ext;
            Debug.LogWarning($"[Replay] 同名录像的后缀已经用到 999 ⇒ 改存 `{fallback}`");
            return fallback;
        }

        static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }

        // ==================================================================
        //  🆕 2026-10-10（`A985⑬`）：**头尾两条【只落盘】的 `[BattleResult]` 记录**
        //      —— 理由码的唯一通道（⛔ 不进 `MsgAction`、⛔ 不上网）。判据与背景 → 本文件头部那一节。
        //  ⚠️ 三个口分工，⛔ 别在别处再造一份：
        //     · **写** = `WriteResultHead` / `WriteResultTail` → `ResultLine`（**唯一**拼那行字的地方）
        //     · **读** = `ReadResultCode`（尾优先、退头）——「回放时读它」就是读它
        //     · **取码桥** = `LastResultFromEvents`（结算时从 `ctx.Events` 里把码捞出来喂给写口）
        // ==================================================================

        /// <summary>理由码那条记录的 token 头 —— **与 `RuleCore.Forfeit` / `RuleCore.CheckWinner`
        /// 那两句 `ctx.Log($"[BattleResult] {reason}({(int)reason}) seat={player}")` 同形**。
        /// ⚠️ 形状的唯一出处是**那两句**；这里是录像侧的孪生，两者同形由 `Editor/NetSelfTest.cs` 的断言
        /// 拿**字面量**钉住（那条断言不比本常量的输出 —— 否则就是自证）。</summary>
        public const string ResultToken = "[BattleResult]";

        /// <summary>把理由码拼成**引擎那套 token 形态**的一行。⚠️ **唯一**的书写口（见上面那张分工表）。
        /// <para>🔴 `seat &lt; 0` ⇒ **不写 `seat=`**（⛔ 别拿 `-1` 顶替一个座位）：匹配层那一刻
        /// **还没有 `ctx`、没有座位**（判据 = `项目任务.md` §四·15），如实不写，而不是编一个出来。
        /// 引擎那两句**总是带着 `seat=`**（那边一定有座位）。</para>
        /// <para>码不是 <see cref="BattleResult"/> 里定义的值时**出声**（不许静默）—— 写出来的会是数字形态，
        /// <see cref="TryParseResultLine"/> 认不出来 ⇒ 读侧会如实报「没记」。</para></summary>
        public static string ResultLine(BattleResult r, int seat)
        {
            if (!Enum.IsDefined(typeof(BattleResult), r))
                Debug.LogWarning($"[Replay] 理由码 {(int)r} 不在 `BattleResult` 里 —— 照写，但读侧认不出（如实报「没记」）");
            return ResultToken + " " + r + "(" + (int)r + ")" + (seat >= 0 ? " seat=" + seat : "");
        }

        /// <summary>写【录像头】那一条（**只落盘**）。调用点 = `BattleDriver.RecBegin`
        /// （⚠️ 那一跳在 `BattleDriver.cs`，不在本文件 —— 本文件只给口）。</summary>
        public static void WriteResultHead(ReplayRecord rec, BattleResult r, int seat)
        {
            if (rec == null) { Debug.LogWarning("[Replay] `WriteResultHead(null)` —— 不记"); return; }
            rec.resultHead = ResultLine(r, seat);
        }

        /// <summary>写【录像尾】那一条（**只落盘** = 这一局是「怎么」结束的）。调用点 = `BattleDriver.RecFinish`
        /// （⚠️ 同上，那一跳在 `BattleDriver.cs`）。码从哪来 = <see cref="LastResultFromEvents"/>。</summary>
        public static void WriteResultTail(ReplayRecord rec, BattleResult r, int seat)
        {
            if (rec == null) { Debug.LogWarning("[Replay] `WriteResultTail(null)` —— 不记"); return; }
            rec.resultTail = ResultLine(r, seat);
        }

        /// <summary>读一行 `[BattleResult] …` 记录。**行尾可以跟人话**（引擎那两句 `ctx.Log` 与
        /// `NetMatchmaking.Cancel` 那一条都带着），只认 token 那一段。
        /// <para>返回 `false` = **这一行不是一条理由码记录**（空串 / 认不出码名 / 根本不是这个 token）。
        /// ⚠️ **名字认不出就返回 `false`**（⛔ 不猜一档 —— 铁律 2）；名字与括号里的数字**对不上**时
        /// **按名字取并出声**（两个写法打架比没有更糟，但也不该因此把整条丢掉）。</para>
        /// <para>`seat` = `-1` 表示**这一行没写座位**（不是「座位是 -1」）。</para></summary>
        public static bool TryParseResultLine(string line, out BattleResult r, out int seat)
        {
            r = BattleResult.Undefined; seat = -1;
            if (string.IsNullOrEmpty(line)) return false;
            string s = line.TrimStart();
            if (!s.StartsWith(ResultToken + " ", StringComparison.Ordinal)) return false;
            s = s.Substring(ResultToken.Length + 1);

            int lp = s.IndexOf('('), rp = lp < 0 ? -1 : s.IndexOf(')', lp + 1);
            if (lp <= 0 || rp < 0) return false;
            string name = s.Substring(0, lp).Trim();
            string num = s.Substring(lp + 1, rp - lp - 1).Trim();
            if (!Enum.TryParse<BattleResult>(name, false, out r) || !Enum.IsDefined(typeof(BattleResult), r)) return false;

            int n;
            if (int.TryParse(num, out n) && n != (int)r)
                Debug.LogWarning($"[Replay] `{ResultToken}` 那一行的名字与数字对不上（{name}({num})）"
                               + $" —— 按名字取 {(int)r}（两个写法打架 = 得修数据，别两边都信）");

            int sp = s.IndexOf("seat=", rp + 1, StringComparison.Ordinal);
            if (sp >= 0)
            {
                int q = sp + 5, e = q;
                while (e < s.Length && (char.IsDigit(s[e]) || (s[e] == '-' && e == q))) e++;
                int sv;
                if (e > q && int.TryParse(s.Substring(q, e - q), out sv)) seat = sv;
            }
            return true;
        }

        /// <summary>录像【头】那一条的码。**「没记」与「认不出的坏行」都返回
        /// <see cref="BattleResult.Undefined"/>(0)**，但坏行**会出声**（空 = 老录像，不吵）。</summary>
        public static BattleResult ReadResultHead(ReplayRecord rec)
        {
            return ReadOne(rec == null ? null : rec.resultHead, "头");
        }

        /// <summary>录像【尾】那一条的码（判据同上）。</summary>
        public static BattleResult ReadResultTail(ReplayRecord rec)
        {
            return ReadOne(rec == null ? null : rec.resultTail, "尾");
        }

        /// <summary>🔴 **「回放时读它」的那一个口** —— **尾优先、退头**（尾 = 这一局怎么结束的；
        /// 头只在尾「没记」时才有意义）。⚠️ 判的是**尾记过没有**（<see cref="HasResultTail"/>），
        /// ⛔ **不是**「尾的码是不是 `Undefined`」—— 「记过、码是 `Undefined(0)`」也是一条**如实**的记录，
        /// 不该被头的值盖掉。读不出 ⇒ <see cref="BattleResult.Undefined"/>(0) = **「未指定」**，
        /// ⛔ 调用方**别拿它冒充某一档**（`BattleResult` 的注：`Undefined` 有它自己的意思）。
        /// ⚠️ 老录像两条都是空 ⇒ 这里返回 `Undefined`（= 它本来的行为，与只传 `Undefined` 逐字相同）。</summary>
        public static BattleResult ReadResultCode(ReplayRecord rec)
        {
            return HasResultTail(rec) ? ReadResultTail(rec) : ReadResultHead(rec);
        }

        /// <summary>头那条**记过**吗（「这份录像是从头记的」那个记号）。⚠️ 与
        /// <see cref="ReadResultHead"/> 不同源：那条看**码值**（`Undefined(0)` 也是一条记录），
        /// 这条看**有没有那一格**。</summary>
        public static bool HasResultHead(ReplayRecord rec)
        {
            return rec != null && !string.IsNullOrEmpty(rec.resultHead);
        }

        /// <summary>尾那条**记过**吗（`false` = 老录像 / 没接上写点 / 中途断了）。</summary>
        public static bool HasResultTail(ReplayRecord rec)
        {
            return rec != null && !string.IsNullOrEmpty(rec.resultTail);
        }

        static BattleResult ReadOne(string field, string which)
        {
            if (string.IsNullOrEmpty(field)) return BattleResult.Undefined;      // 没记（老录像）—— 不吵
            BattleResult r; int seat;
            if (TryParseResultLine(field, out r, out seat)) return r;
            // 🔴 记过、但认不出来 ⇒ **出声**（不许静默：这正是「两边写法不一致」会现形的地方）
            Debug.LogWarning($"[Replay] 录像{which}那条 `{ResultToken}` 记录认不出来：「{field}」⇒ 当「未指定」"
                           + "（⛔ 不猜一档；查 `ReplayStore.ResultLine` 与 `RuleCore` 那两句 `ctx.Log` 是不是同形）");
            return BattleResult.Undefined;
        }

        /// <summary>🆕 2026-10-10（`A985⑬` 的**取码桥**）：从引擎那串 `ctx.Events` 里取**最后一条**
        /// `[BattleResult] …`（`ctx.Log` 是只增不减的，所以要**从尾往前**找）。
        /// <para>🔴 为什么要它：码**不在 `ctx` 的任何字段上**（`BattleResult` 的注：⛔ 不写进 `ctx`、
        /// 免得旧录像/旧联机局假红）—— 它只以那一行的形式活在 `ctx.Events` 里
        /// （`RuleCore.Forfeit` 与 `RuleCore.CheckWinner` 各写一条）⇒ 结算时想把它记进录像，**只能**回头读这一串。</para>
        /// <para>一条都没有 ⇒ 返回 `Undefined` + `seat = -1`（**如实说「未指定」**，⛔ 别猜一档）。
        /// `events` 传 `null` 也走这一支（不炸）。</para>
        /// <para>⚠️ 调用点 = `BattleDriver.RecFinish`（在 `BattleDriver.cs`，不在本文件）。</para></summary>
        public static BattleResult LastResultFromEvents(List<string> events, out int seat)
        {
            seat = -1;
            if (events == null) return BattleResult.Undefined;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                BattleResult r; int s;
                if (TryParseResultLine(events[i], out r, out s)) { seat = s; return r; }
            }
            return BattleResult.Undefined;
        }

        // ---------------------------------------------------------- 读 / 列 / 删

        /// <summary>目录里全部录像的文件名（**按时间倒序** —— 文件名自带时间戳，不靠文件系统时间）。</summary>
        public static List<string> List()
        {
            var outp = new List<string>();
            try
            {
                if (!Directory.Exists(Dir)) return outp;
                foreach (var f in Directory.GetFiles(Dir, "*" + Ext)) outp.Add(Path.GetFileName(f));
                outp.Sort();
                outp.Reverse();
            }
            catch (Exception e) { Debug.LogError("[Replay] 列目录失败：" + e.Message); }
            return outp;
        }

        public static int Count { get { return List().Count; } }

        /// <summary>读一局。**读不出来返回 null 并出声**（版本不认识 / 文件坏了 / 被删了）。</summary>
        public static ReplayRecord Load(string fileName)
        {
            try
            {
                string path = Path.Combine(Dir, fileName);
                if (!File.Exists(path)) { Debug.LogWarning("[Replay] 没有这个文件：" + path); return null; }
                var rec = JsonUtility.FromJson<ReplayRecord>(File.ReadAllText(path));
                if (rec == null) { Debug.LogWarning("[Replay] 解不出内容：" + path); return null; }
                // 🔴 **2026-10-01：改成比 `new ReplayRecord().version`** —— 原来写死的那个 `1`
                //    在棋盘模型换代时**不会跟着变**（判据只写一份：`ReplayRecord.version`）。
                if (rec.version != new ReplayRecord().version)
                {
                    // ⚠️ 版本不认识就**别硬解**（字段对不上会静默读成默认值；而引擎换代之后
                    //    同一串动作会落到**别的格号** ⇒ 硬解出来的是**另一局**）
                    Debug.LogWarning($"[Replay] 版本 {rec.version} 不认识（本代码只认 "
                                   + $"{new ReplayRecord().version}）⇒ 不播：" + path);
                    return null;
                }
                if (rec.actions == null) rec.actions = new List<MsgAction>();
                // 🔴 🆕 2026-10-18：**旧卡 id 的读时迁移闸**（卡池换过两代：`601005b` 一次性换 955 张 +
                //   `K06` 换掉最后那批自造 token，两次都没迁玩家存档）。
                //   录像里三处会带卡 id：`MsgStart.hostDeckJson/clientDeckJson`（`JsonUtility` 化的
                //   `PlayerDeck`）· `MsgAction.pickIds` · `MsgAction.defId`。
                //   不迁的话：那副牌在 `DeckBuilder.FromDeck` 里查不到 ⇒ 只 `LogError` 后 continue
                //   （静默少牌），而 `finalHash` 是按**满 30 张**那个局面算的 ⇒ 这份录像**必然报「不是同一局」**。
                // ⚠️ **只改内存里的这一份 `ReplayRecord`，录像文件一个字都不碰**；
                //    **幂等**靠 `LegacyCardIds` 的**整副双解**闸门（迁完现代解合法 ⇒ 再进一次第一步就返回）
                //    —— 🔴 **不是**「真 id 不可能是映射表的 key」（那句已不成立：890/1102 个值本身又是 key）。
                //    ⚠️ 干净的录像（不含任何旧 token）走的是 `ContainsAny` 短路 ⇒ **零改动**。
                //    映射表与逐条判据 → `RuleEngine/Data/LegacyCardIds.cs`。
                MigrateLegacyIds(rec, fileName);
                return rec;
            }
            catch (Exception e) { Debug.LogError("[Replay] 读盘失败：" + e.Message); return null; }
        }

        /// <summary>旧卡 id 的读时迁移（调用点 = <see cref="Load"/>；判据与映射表 → `RuleEngine/Data/LegacyCardIds.cs`）。
        /// <para>**两份牌**（`MsgStart.hostDeckJson` / `clientDeckJson`）各走一次**双解验证**
        /// （`MigrateDeckJson`：现代解不合法 ∧ 旧代解合法才迁）；**动作里的 id**
        /// （`MsgAction.pickIds` / `MsgAction.defId`）**只有在那份牌真的被判成旧代时**才按同一张表迁
        /// —— 一整份录像是一代到底的，牌是旧代 ⇒ 动作里的 id 也一定是旧代。</para>
        /// <para>⚠️ 为什么动作**不能**无条件查表：卡池换过两代、且新表**不是单射**
        /// （890/1102 个值本身又是 key、最长链 31 跳）⇒ 对一份**现代**录像盲套表会把它改成另一局。
        /// 空的 / 干净的（不含旧 token）一律**原样不动**（`ContainsAny` 短路 + 双解第一步就返回）。</para></summary>
        static void MigrateLegacyIds(ReplayRecord rec, string fileName)
        {
            string where = "回放 `" + fileName + "`";
            var lookup = RuleEngine.LegacyCardIds.PoolLookup();     // 池子没加载 ⇒ null ⇒ 下面整段跳过
            bool oldGen = false;
            if (rec.start != null)
            {
                bool a, b;
                rec.start.hostDeckJson = RuleEngine.LegacyCardIds.MigrateDeckJson(
                    rec.start.hostDeckJson, lookup, where + " · hostDeckJson", out a);
                rec.start.clientDeckJson = RuleEngine.LegacyCardIds.MigrateDeckJson(
                    rec.start.clientDeckJson, lookup, where + " · clientDeckJson", out b);
                oldGen = a || b;
            }
            if (!oldGen || rec.actions == null) return;             // 现代局 ⇒ 动作里的 id 原样不动
            for (int i = 0; i < rec.actions.Count; i++)
            {
                var m = rec.actions[i];
                if (m == null) continue;
                if (m.pickIds != null && m.pickIds.Length > 0)
                    m.pickIds = RuleEngine.LegacyCardIds.MigrateArray(
                        m.pickIds, where + " · actions[" + i + "].pickIds");
                m.defId = RuleEngine.LegacyCardIds.Migrate(
                    m.defId, where + " · actions[" + i + "].defId");
            }
        }

        /// <summary>超过 `keep` 局就**删最旧的**。返回删掉了几个。
        /// ⚠️ 删的是**录像文件**（用户数据）—— 只在超出上限时删，且**删之前先出声**。</summary>
        public static int TrimToLast(int keep)
        {
            int removed = 0;
            try
            {
                var all = List();                       // 已按新→旧
                for (int i = keep; i < all.Count; i++)
                {
                    string p = Path.Combine(Dir, all[i]);
                    Debug.Log("[Replay] 超出上限（" + keep + " 局）⇒ 删最旧的一份：" + all[i]);
                    File.Delete(p);
                    removed++;
                }
            }
            catch (Exception e) { Debug.LogError("[Replay] 修剪失败：" + e.Message); }
            return removed;
        }

        /// <summary>全删（自检用；**也顺手清掉目录**）。
        /// <para>🔴 **硬挡（2026-10-24 热修 `E11`）**：<see cref="OverrideDir"/> 没设 ⇒ **拒绝执行** + `Debug.LogError` 出声。
        /// 口径照 `BattleLogData.DeleteStoreForTest`（`Shell/BattleLogData.cs:313-324`）、`DailyData.DeleteStoreForTest`：
        /// **自检不许碰玩家的真档**。</para>
        /// <para>⚠️ **为什么非有这道挡不可**：`Editor/ProfileDataAssertE13.cs` 的 `finally` 曾经先把 `OverrideDir` 置回 null、
        /// **再**调本方法 ⇒ <see cref="Dir"/> 落回 `persistentDataPath/WarpforgeReplays` ⇒ **玩家的真录像被整目录清空**，
        /// 而且本方法当时**一行日志都不打**（静默）。事故与证据 → `资料/普查产出_1024c/RE1_战斗回放完整审查.md` B1/B2/B3；
        /// 被删的就是 `C:/Users/<你>/AppData/LocalLow/Everguild/Warpforge/WarpforgeReplays/`（目录还在、文件已空、mtime 2026-10-24 17:12）。</para>
        /// <para>✅ **正确用法**（照 `Editor/NetSelfTest.cs:925-929`）：**先** `ResetForTest()`（此时 `OverrideDir` 还指着临时目录），
        /// **再**还原 `OverrideDir`。⛔ **顺序反了 = 删掉玩家的真录像**。</para></summary>
        public static void ResetForTest()
        {
            if (string.IsNullOrEmpty(OverrideDir))
            {
                Debug.LogError("[Replay] 自检：**拒绝清空录像** —— 没设 `OverrideDir` 时删的就是玩家的真录像（`"
                             + Dir + "`）。先 `ReplayStore.OverrideDir = <临时目录>`，清完**再**还原它"
                             + "（⛔ 顺序反了 = 删掉玩家的真录像）。");
                return;
            }
            try
            {
                if (!Directory.Exists(Dir)) return;
                foreach (var f in Directory.GetFiles(Dir, "*" + Ext))
                {
                    Debug.Log("[Replay] 自检清空：删掉 " + f);   // 删的是文件 ⇒ 出声（不许静默，口径同 `TrimToLast`）
                    File.Delete(f);
                }
            }
            catch (Exception e) { Debug.LogError("[Replay] 清理失败：" + e.Message); }
        }
    }
}
