// NetChat.cs — 战斗内「对手聊天」（督军台词）的**联机那一段**：协议形状 + 收发语义的唯一一份判据。
//
// 判据（第一权威 = 反编译方法体，`d:/2/tools/decomp_full/`；逐句读过的三份）：
//   · `BattleCommsManager__SendChatMessage.c:20-37` —— 把**一个 int（台词枚举号）装箱**成
//     `object[]{ int }` 走 `PhotonView.RPC(<方法名>, RpcTarget.All, 那个数组)`。
//     ⇒ **线上只有「一个 int」**（没有文本、没有座位、没有时长）。
//     ⚠️ **那个方法名字符串查不到**（反编译里只是 metadata 引用 `DAT_18427c0d8`，字面量没导出）；
//       我们不是 Photon ⇒ **不需要照抄名字**，只要消息语义与顺序一致（同 `资料/普查产出_1025/
//       E_判据_引擎与战斗缺口.md` §11 的裁定）。
//   · `BattleCommsManager__PUNReceiveChatMessage.c:20-26` —— 收到后先比**发送者标识**与
//     本地玩家（`param_3+8 → +0x18` vs 本地 `+0x40`），**不等**才
//     `DisplayWarlordChatMessage(chatId, isLocalPlayer: 0, force: 0)`。
//     🔴 **口径说明（别照着反编译那一行的字面读）**：那一行印出来是
//        `cVar2 = String_op_Inequality(sender, local); if (cVar2 == 0) { 显示 }`
//        —— 按名字读等于「**相等**才显示」，那会把**自己那条回显**显示两遍、而**对面那条永不显示**
//        （自己那条在 `DisplayWarlordChatMessage` 的 `isLocal != 0` 支里**已经显示过**了，见下一行），
//        与 Photon `RpcTarget.All` 的广播形状直接矛盾 ⇒ **判据取「不等才显示」**
//        （同 `E_判据…` §11 的结论：那一处是 il2cpp 包装函数名在反编译产物里的错配）。
//   · `VoiceLinesController__DisplayWarlordChatMessage.c`（整支现读）——
//       ① `param_4 == 0 && !CanChat(isLocal) ⇒ return`（`CanChat` = 在对局中；`isLocal == 0` 那一档
//          还要求 `chatMuted(+0x40) == 0`）；
//       ② `DisplayWarlordRegularChatMessage(chatId, isLocal)` 播台词 + 出气泡；
//       ③ **只有 `isLocal != 0`（本地玩家自己开口）**那一支才 `SendChatMessage`（`:65-67`，
//          条件是联机 + Photon 已连 + **`chatId != 0 && chatId != 1`**），并 `AIResponse(..., 1)`；
//          ⇒ **收到别人的消息那一档根本不进这个分支 ⇒ 原版的「回环防护」就是这一条**：
//             **谁显示谁不回发**；
//       ④ 第 5 条 SmallTip：`isLocal == 0 && +0x68==0 && chatId != 0 && +0x40==0` 时才把 `+0x6c` 自增，
//          自增后 `4 <` 就 `+0x6c=0`、`+0x68=1` 并弹一次 `Battle/Tips/MuteEnemyChat`；
//       ⑤ 记 `PlayerAction` 类型 **`0x37`**（warlord in-battle id `+0x22c`、chatId、isLocal、
//          当前 clip 长度 `+0x4e0`）进**当前回合方**的动作流水；**`chatId == 0` / `chatId == 4` /
//          `matchType == 0xa0`（回放）不记**（`:138`）。
//
// 我们的分工（⛔ 别把判据搬第二份到这里）：
//   · **本文件 / `NetBattle`** 只管**线上那一条**：发一条「我的督军说了第几句」、收一条并交给宿主；
//   · **宿主（`BattleDriver`）**管**显示 / 静音闸 / 第 5 条 SmallTip / 动作流水录制** ——
//     那四件在原版里也全在 `VoiceLinesController` + `BattleManager` 那一侧，而且我们**已经有**了
//     （`BattleDriver.SpeakChat` / `CanAiChat` / `NoteEnemyWarlordChat` / `RecRaw`）⇒ 复用，⛔ 不新造。
//
// ⚠️ **仍然查不到的**（⛔ 不许猜，见铁律 2）：`PhotonView.RPC` 的方法名字符串 ·
//   `PhotonMessageInfo` 的字段布局 · `BotEmotesConfig` 四个概率值的真值（远端 RemoteConfig）。
//   前两条我们**用不着**（不是 Photon）；第三条只影响 AI 自发发言的频率，与本件无关。
namespace CardPresentation.Net
{
    /// <summary>宿主（`BattleDriver`）要接的那一半：**对面督军说了一句，你去显示**。
    ///
    /// <para>🔴 为什么单开一个接口、而不往 <see cref="INetBattleHost"/> 上加：那个接口**已经**在
    /// `BattleDriver` 上实现着，多一个成员就等于**强制**所有既有宿主（含自检里那些只关心动作流的
    /// `BareHost`）立刻跟着改。聊天是**表现层的一条旁路**（不落地、不改局面、不参与指纹）⇒
    /// 用「**可选**接口 + `is` 判一下就出声」的接法才是同一族（先例：`NetBattle` 里那些
    /// `_d == null` 的告警分支）。</para></summary>
    public interface INetChatHost
    {
        /// <summary>对面督军说了 `chatId` 那一句（原版 `DisplayWarlordChatMessage(chatId, 0, 0)`）。
        /// 🔴 收侧**只负责显示**：⛔ 不许回发（原版那条防回环就是「收到那一档不进发送支」，
        /// 见 <see cref="NetChat"/> 文件头 ③）、⛔ 不许记账成「对面做了什么动作」（它不改局面）。</summary>
        void NetRemoteChat(int chatId);
    }

    /// <summary>「对手聊天」在**我们这条线上**的常量与判据（⛔ 别在别处再写一份判断）。</summary>
    public static class NetChat
    {
        /// <summary>台词枚举的第一格：`ChatMessage.Greet = 5`。
        /// 判据 = `VoiceLinesPopupSelector__SetupChatOptions.c:54,112`（写死起点 **5**，按按钮下标 +1；
        /// 与 `ChatPopupPanel` 那 6 颗钮、`VoiceLines.ForChatButton` 的 6 条**逐条对齐**）。
        /// ⇒ `chatId = FirstButtonChatId + 按钮下标`。</summary>
        public const int FirstButtonChatId = 5;

        /// <summary>台词枚举的最后一格：`ChatMessage.Oops = 10`（`Random.Range(5, 11)` 那个上界）。
        /// 判据 = `VoiceLinesController._ChatAIResponse_d__60__MoveNext.c:47` 上面那两行。</summary>
        public const int LastButtonChatId = 10;

        /// <summary>原版 `MatchType.Replay = 0xA0` —— 那一档**不记**聊天动作（`:138`）。
        /// 我们的回放走的是「本机重放，不挂联机层」⇒ 这一档**天然**不会走到
        /// <see cref="ShouldRecord"/> 的调用点；常量留着是给宿主那条记录判据一个**可核对的锚**。</summary>
        public const int ReplayMatchType = 0xA0;

        /// <summary>原版记进动作流水的那条 `PlayerAction` 的**类型码**（`VoiceLinesController__
        /// DisplayWarlordChatMessage.c:147` 的第一个实参 `0x37`）。</summary>
        public const int ActionType = 0x37;

        /// <summary>这条 `chatId` 是不是「6 颗台词钮」能说的那一档（5…10）。
        /// 🔴 **收侧必须过这一道**：对面可以发任意 int，而我们拿它去索引台词表/语音表 ——
        /// 越界不许静默落到某一句上（那会把一句伪造的话说成督军的正常台词）。</summary>
        public static bool IsButtonLine(int chatId)
        {
            return chatId >= FirstButtonChatId && chatId <= LastButtonChatId;
        }

        /// <summary>`chatId` → 按钮下标（0…5）。**不是**那一档 ⇒ `-1`（调用方要出声）。</summary>
        public static int ButtonIndex(int chatId)
        {
            return IsButtonLine(chatId) ? chatId - FirstButtonChatId : -1;
        }

        /// <summary>原版 `SendChatMessage` 之前那两道闸（`:65`）：**`chatId == 0` / `chatId == 1`
        /// 不往外发**（0 = Intro、1 = MirrorMatch，这两句是「开幕台词」，由主机/双方各自本地演）。
        /// ⚠️ 我们今天的发送口只有 6 颗钮（5…10）⇒ 这条闸恒真；留着是因为它是**原版判据原件**，
        /// 将来若把开场白也搬上线，⛔ 不要重新发明一遍。</summary>
        public static bool IsRelayable(int chatId)
        {
            return chatId != 0 && chatId != 1;
        }

        /// <summary>原版记动作流水的那道闸（`DisplayWarlordChatMessage.c:138`）：
        /// **回放（`matchType == 0xA0`）不记** · **`chatId == 0` 不记** · **`chatId == 4` 不记**。
        ///
        /// <para>⚠️ `chatId == 4` 那一格原版**没有解释**（枚举里 4 是 `HurryUp`，那句由
        /// `BattleManager__DisplayHurryUpChatMessage` 走另一条路驱动）—— 我们**照抄那个数**，
        /// 不替它编理由。判据全或不写的两档都记在 `VoiceLinesController__DisplayWarlordChatMessage.c:138`。</para>
        ///
        /// <para>🔴 调用方（宿主）必须在**录制那一刻**问它，⛔ 别把结果缓存 —— `isReplay` 是**这一局**的属性。</para></summary>
        public static bool ShouldRecord(int chatId, bool isReplay)
        {
            if (isReplay) return false;      // matchType == 0xA0
            if (chatId == 0) return false;
            if (chatId == 4) return false;   // HurryUp：原版那一格没有理由，照抄数字
            return true;
        }
    }
}
