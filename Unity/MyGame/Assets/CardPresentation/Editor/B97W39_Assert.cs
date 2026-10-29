// B97W39_Assert.cs — 执行代理 `B97_W39`（第九十七会话 · 波 4 · 轮抽线）的断言
//
// ============================ 它盯的是什么 ============================
//   施工单 `判据/B97_R6_轮抽P2P房施工单.md` 的 **步骤 3 + 步骤 4**：
//     · 步骤 3：`GameStaticData.newbieELO` 归位（原版那一格就在 `GameStaticData` 上）；
//     · 步骤 4：轮抽房的**房属性 C1/C4/C5 过网**（`MsgDeck` 客机→主机 · `MsgStart` 主机→客机）。
//
// 判据（⛔ 别在这里抄第二份）：
//   · 施工单 = `资料/完全复刻/判据/B97_R6_轮抽P2P房施工单.md` §1·1 / §1·2 / §2·4 / §2·5 / §3·1 第 9 行
//   · 原版真体 = `d:/2/tools/decomp_generic_trial/out_b94/DraftEventBase_object__object_.SetCustomMatchOptions__1813cb0.c`
//   · `newbieELO` = ILSpy `GameStaticData.cs:360`（声明）/ `:2424`（`.cctor` = 800）
//
// ============================ 四组 ============================
//   A **往返一致**：本机三个值 → 生产代码填包 → **真的过一遍 `JsonUtility`** → 收包侧吸收 → 读回来逐格相等。
//   B **缺键（老报文）**：完全没有这三个字段的 JSON ⇒ 收包侧**出声 + 三格按默认 0**，且**不抛**、不留上一包的残值。
//   C **不在指纹的输入面**：三个值不许落到 `NetPendingBattle`（进战场的唯一桥）或 `BattleContext/PlayerState/BattleEvent`
//     （`Fingerprint` / `StateHash` 的全部输入）。
//   D **步骤 3 单源**：`GameStaticData.newbieELO == 800`，且与 `NetRuntime.DraftNewbieElo` **同值**（两份字面量不许分叉）。
//
// ============================ 🔴 灭自证（每条怎么才能让它红）============================
//   · A2/A3/A4：`FillDraftRoomProps` 填错一格 / 少填一格 / 忘了置 `draftPropsVer = 1` ⇒ 对应那条红。
//   · A5：把 `FillDraftRoomProps(MsgStart)` 删掉或漏填 ⇒ 红（两条消息各验一次，⛔ 不靠「另一条填过」).
//   · B1：把 `draftPropsVer` 那一格去掉（退回「靠值本身判在不在场」）⇒ 老报文读出的 ver 就不再可判别 ⇒ B1/B2 红。
//   · B3：`AbsorbFoeDraftRoomProps` 在 `ver == 0` 时**不**清旧值（照抄上一包）⇒ 先喂一包有效值再喂老报文 ⇒ 红。
//   · C2：把 `draftC1` 之类塞进 `NetPendingBattle`（想让它跟着进战场）⇒ 红。
//   · C3：把房属性塞进 `BattleContext` / `PlayerState` / `BattleEvent`（= 指纹的输入面）⇒ 红。
//         ⚠️ 这一条是**结构性**的：`NetProtocol.Fingerprint` 只吃 `ctx`，所以「加进指纹」必然要动这三个类型之一。
//   · D1/D2：把 `newbieELO` 或 `NetRuntime.DraftNewbieElo` 改成别的数（哪怕只改一侧）⇒ D1/D2 里必有一条红。
//
// ============================ 怎么用（宿主）============================
// `B97W39Assert.Run();`
// `_sink.Pass += B97W39Assert.Pass; _sink.Fail += B97W39Assert.Fail;`
//   · 建议宿主 = `Editor/ShellScene.cs`（轮抽那几条断言都在这一条线；追加到它合计行**之前**那一行 ⇒ 行数零变化）
//     —— 宿主接线由**调度台**做（本代理不碰宿主文件）。
//   · 不跑 Unity 批处理、不建 GameObject、不碰任何会话（`NetMatchmaking` 只用它的**静态**房属性格，
//     跑完 `ClearDraftRoomProps()` 还原，⛔ 不动 `_myDeck`/`_foeDeck` 那套 —— 那会打乱同宿主其它联机断言）。
using System;
using System.Reflection;
using CardPresentation;
using CardPresentation.Net;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B97_W39`：轮抽 P2P 房的步骤 3（`newbieELO` 归位）+ 步骤 4（房属性 C1/C4/C5 过网）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W39Assert
{
    static readonly CheckSink _sink = new CheckSink("[B97W39] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W39 · 步骤 3/4（`newbieELO` 归位 + 房属性 C1/C4/C5 过网 · 账 轮抽:30）");

        // ⚠️ 先清一遍：本文件只碰房属性静态格，但上一轮宿主里可能有残留（幂等）
        NetMatchmaking.ClearDraftRoomProps();

        SectionA_往返一致();
        SectionB_缺键老报文();
        SectionC_不在指纹输入面();
        SectionD_newbieELO单源();

        NetMatchmaking.ClearDraftRoomProps();   // 还原：不给同宿主其它断言留状态
        Debug.Log(_sink.P + "B97_W39 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 往返一致（**真的过一遍线上格式**）
    static void SectionA_往返一致()
    {
        MenuCheck.Section(_sink, "A 本机三个值 → 生产代码填包 → `JsonUtility` 往返 → 收包侧吸收；逐格相等");

        C(NetMatchmaking.DraftPropsVersion, 1,
          "A0 `DraftPropsVersion` == 1（0 是「没带」的语义位，⛔ 别把它当有效版本）");

        // —— 交牌包（客机→主机）——
        NetMatchmaking.SetLocalDraftRoomProps(5, 820, 2);   // 字面量：胜 5 / ELO 820 / 连败 2
        int mc1, mc4, mc5;
        Tr(NetMatchmaking.MyDraftRoomProps(out mc1, out mc4, out mc5),
           "A1 前提：`SetLocalDraftRoomProps` 之后本机那三格**是「已设」**（`_myPropsSet`）—— 不成立 ⇒ A2..A4 无意义");
        C(mc1, 5, "A1b `MyDraftRoomProps` 回读 C1 == 5");
        C(NetMatchmaking.OutgoingDraftPropsVer, 1, "A1c 已设 ⇒ 出站版本标记 == 1");

        var dm = new MsgDeck { deckJson = "{}", gameMode = "ClosedDeck", faction = "UM" };
        NetMatchmaking.FillDraftRoomProps(dm);
        C(dm.draftPropsVer, 1, "A2 ★ 填包时 `draftPropsVer` == 1（⛔ 忘了置 ⇒ 对面会把「带了」当成「没带」）");

        // ★ 真的编一次 JSON 再解回来 —— 这是线上唯一那条路（`NetProtocol.Pack/Unpack` = `JsonUtility`）
        string deckJson = NetProtocol.Pack(dm);
        var deckBack = NetProtocol.Unpack<MsgDeck>(deckJson);
        Tr(deckBack != null, "A2b 前提：`MsgDeck` 的 JSON 解得回来（解不回 ⇒ 下面三条红）");
        bool took = NetMatchmaking.AbsorbFoeDraftRoomProps(deckBack.draftPropsVer, deckBack.draftC1,
                                                          deckBack.draftC4, deckBack.draftC5);
        int c1, c4, c5;
        bool has = NetMatchmaking.FoeDraftRoomProps(out c1, out c4, out c5);
        Tr(took && has, "A2c 收包侧认了「带了」（`AbsorbFoeDraftRoomProps` 回 true 且 `FoeDraftRoomProps` 回 true）");
        C(c1, 5, "A3 ★ **往返后** C1(胜场) == 5（`MsgDeck` 那条路）");
        C(c4, 820, "A4 ★ **往返后** C4(ELO) == 820");
        C(c5, 2, "A4b ★ **往返后** C5(连败) == 2");

        // —— 开局包（主机→客机）—— 两条消息各验一次（⛔ 不靠「另一条填过」）
        var st = new MsgStart { seed = 4242, mode = "ClosedDeck", arena = "arena1" };
        NetMatchmaking.FillDraftRoomProps(st);
        C(st.draftPropsVer, 1, "A4c 开局包也置了 `draftPropsVer` == 1");
        string startJson = NetProtocol.Pack(st);
        var startBack = NetProtocol.Unpack<MsgStart>(startJson);
        Tr(startBack != null, "A4d 前提：`MsgStart` 的 JSON 解得回来");
        NetMatchmaking.AbsorbFoeDraftRoomProps(-1, 0, 0, 0);            // 先清干净，免得 A5 读的是上面那条的残值
        bool took2 = NetMatchmaking.AbsorbFoeDraftRoomProps(startBack.draftPropsVer, startBack.draftC1,
                                                            startBack.draftC4, startBack.draftC5);
        NetMatchmaking.FoeDraftRoomProps(out c1, out c4, out c5);
        Tr(took2, "A5 前提：开局包那条也认了「带了」");
        C(c1, 5, "A5b ★ **往返后** C1 == 5（`MsgStart` 那条路；⛔ 别只测一条消息）");
        C(c4, 820, "A5c ★ **往返后** C4 == 820");
        C(c5, 2, "A5d ★ **往返后** C5 == 2");

        // —— 没设 ⇒ 出站必须 ver = 0（⛔ 不许「带三个 0 冒充报了 0」）——
        NetMatchmaking.SetLocalDraftRoomProps(-1, 0, 0);
        var dm2 = new MsgDeck { deckJson = "{}", gameMode = "ClosedDeck", faction = "UM" };
        NetMatchmaking.FillDraftRoomProps(dm2);
        C(dm2.draftPropsVer, 0,
          "A6 ★ 本机没设 ⇒ `draftPropsVer` == 0（**没带**），⛔ 不是「带了 0/0/0」—— 两者对收侧的含义完全不同");

        NetMatchmaking.ClearDraftRoomProps();
    }

    // ------------------------------------------------------------ B 缺键 / 老报文
    static void SectionB_缺键老报文()
    {
        MenuCheck.Section(_sink, "B 老报文（完全没有这三个字段）⇒ 出声 + 三格按默认 0 + 不抛、不留残值");

        // ★ 逐字打一份**改前格式**的 JSON（没有 `draftPropsVer/draftC1/draftC4/draftC5`）
        const string oldDeck = "{\"deckJson\":\"{}\",\"gameMode\":\"Classic\",\"faction\":\"NE\"}";
        MsgDeck om = null;
        bool threw = false;
        try { om = NetProtocol.Unpack<MsgDeck>(oldDeck); } catch (Exception) { threw = true; }
        Tr(!threw && om != null, "B0 ★ 老报文**解得出、不抛**（`JsonUtility` 对缺字段宽容）");
        C(om != null ? om.draftPropsVer : -1, 0,
          "B1 ★ 老报文里 `draftPropsVer` 读出来 == 0 = **没带**（这就是「缺键」的**可判别**标记；⛔ 别拿「三格都是 0」当判据）");

        // 先喂一包**有效**的，再喂老报文 ⇒ 必须清回 0（灭自证：不清 ⇒ B3 红）
        NetMatchmaking.AbsorbFoeDraftRoomProps(1, 9, 1500, 6);
        int c1, c4, c5;
        bool hasGood = NetMatchmaking.FoeDraftRoomProps(out c1, out c4, out c5);
        Tr(hasGood && c1 == 9, "B2 前提：先记下了一包有效值（C1 == 9），B3 才有「残值」可查");

        bool took = NetMatchmaking.AbsorbFoeDraftRoomProps(om != null ? om.draftPropsVer : 0,
                                                          om != null ? om.draftC1 : 0,
                                                          om != null ? om.draftC4 : 0,
                                                          om != null ? om.draftC5 : 0);
        bool has = NetMatchmaking.FoeDraftRoomProps(out c1, out c4, out c5);
        Tr(!took, "B2b 老报文 ⇒ `AbsorbFoeDraftRoomProps` 回 **false**（= 「没带」，调用方/日志据此出声）");
        C(c1, 0, "B3 ★ 老报文 ⇒ C1 回**默认 0**");
        C(c4, 0, "B3b ★ 老报文 ⇒ C4 回**默认 0**");
        C(c5, 0, "B3c ★ 老报文 ⇒ C5 回**默认 0**");
        Tr(!has, "B4 ★ 老报文 ⇒ 「已收到对面房属性」是 **false**（⛔ 不许把默认 0 说成「对面报了 0」）");

        // 反例：坏的 / 空载荷不许把宿主弄崩
        threw = false;
        try { NetProtocol.Unpack<MsgStart>(""); } catch (Exception) { threw = true; }
        Tr(!threw, "B5 空载荷解 `MsgStart` **不抛**（`Unpack` 对空串返回 null）");

        NetMatchmaking.ClearDraftRoomProps();
    }

    // ------------------------------------------------------------ C 不在指纹的输入面
    static void SectionC_不在指纹输入面()
    {
        MenuCheck.Section(_sink, "C 房属性 ⛔ 不进 `NetPendingBattle` / `BattleContext` / `PlayerState` / `BattleEvent`（指纹的全部输入）");

        // C1：开局包交接那条**唯一**的桥 —— 三个值不同，交出来的开局参数必须**逐格相同**
        var s1 = NewStart(draftVer: 0, c1: 0, c4: 0, c5: 0);
        var s2 = NewStart(draftVer: 1, c1: 99, c4: 1500, c5: 7);
        var p1 = NetPendingBattle.FromStart(s1, true);
        var p2 = NetPendingBattle.FromStart(s2, true);
        Tr(p1 != null && p2 != null, "C0 前提：`FromStart` 两份都造得出来（否则 C1 空转）");
        if (p1 != null && p2 != null)
        {
            C(p2.Seed, p1.Seed, "C1a 开局交接：`Seed` 不随房属性变");
            Tr(p2.Mode == p1.Mode, "C1b 开局交接：`Mode` 不随房属性变");
            Tr(p2.Arena == p1.Arena, "C1c 开局交接：`Arena` 不随房属性变");
            C(p2.FirstSeat, p1.FirstSeat, "C1d ★ 开局交接：`FirstSeat` 不随房属性变（先手是主机掷的，与房属性无关）");
            C(p2.MySeat, p1.MySeat, "C1e 开局交接：`MySeat` 不随房属性变");
            Tr(p2.Seat0Faction == p1.Seat0Faction && p2.Seat1Faction == p1.Seat1Faction,
               "C1f 开局交接：两侧阵营不随房属性变");
        }

        // C2/C3：结构性 —— 类型上**根本没有**那一格（`Fingerprint` 只吃 `ctx` ⇒ 想加进指纹必然要动这几个类型之一）
        Tr(!HasDraftMember(typeof(NetPendingBattle)),
           "C2 ★ `NetPendingBattle` 上**没有**任何 draft* 成员（⛔ 谁把房属性挂上去跟着进战场，这条就红）");
        Tr(!HasDraftMember(typeof(BattleContext)),
           "C3 ★ `BattleContext` 上**没有**任何 draft* 成员（= 指纹 `Fingerprint` / `StateHash` 的输入面）");
        Tr(!HasDraftMember(typeof(PlayerState)),
           "C3b ★ `PlayerState` 上**没有**任何 draft* 成员（`PlayerHash` 读的就是它——两端各人不同，进去必分叉）");
        Tr(!HasDraftMember(typeof(BattleEvent)),
           "C3c ★ `BattleEvent` 上也**没有**（`Fingerprint` 含 `ctx.Events.Count`，塞进来同样会分叉）");
    }

    // ------------------------------------------------------------ D 步骤 3：`newbieELO` 单源
    static void SectionD_newbieELO单源()
    {
        MenuCheck.Section(_sink, "D 步骤 3：`GameStaticData.newbieELO`（原版那一格就在那儿）与暂存值不许分叉");

        C(GameStaticData.newbieELO, 800,
          "D1 ★ `GameStaticData.newbieELO` == 800（判据 = ILSpy `GameStaticData.cs:360` 声明 + `:2424` `.cctor` 写死）");
        C(GameStaticData.newbieELO, NetRuntime.DraftNewbieElo,
          "D2 ★ `GameStaticData.newbieELO` == `NetRuntime.DraftNewbieElo`（**两份字面量不许分叉** —— 改任一侧就红；"
        + "本代理白名单不含 `Net/NetRuntime.cs`，那一行「改指过去」的可粘贴补丁见 `报告/B97_W39.md` §三，落地后本格应改成常量引用）");
        C(NetRuntime.DraftNewbieElo, 800, "D3 `NetRuntime.DraftNewbieElo` == 800（暂存那侧也不能被单独改掉）");
    }

    // ============================================================ 工具
    /// <summary>造一份只差房属性三格（+ ver）的开局包，其余逐格相同（含 null 的两副牌 —— 本组不验牌）。</summary>
    static MsgStart NewStart(int draftVer, int c1, int c4, int c5)
    {
        return new MsgStart
        {
            seed = 20261029,
            mode = "ClosedDeck",
            arena = "arena1",
            hostFirst = 1,
            hostFaction = "Ultramarines",
            clientFaction = "Necrons",
            myName = "Host",
            foeName = "Client",
            hostDeckJson = null,
            clientDeckJson = null,
            draftPropsVer = draftVer,
            draftC1 = c1,
            draftC4 = c4,
            draftC5 = c5,
        };
    }

    /// <summary>这个类型上有没有任何**名字里带 `draft`** 的字段/属性（`Fingerprint` 输入面的结构判据）。
    /// <para>⚠️ 只看成员名，不看值 —— 房属性在三格里叫 `draftC1/draftC4/draftC5`，进指纹的人只能沿用这套名字
    /// （⛔ 换成 `roomProps` 之类的话这条会漏；那一天要同步改这里，别当成「已经不可能发生」）。</para></summary>
    static bool HasDraftMember(Type t)
    {
        if (t == null) return false;
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic
                             | BindingFlags.Instance | BindingFlags.Static;
        foreach (var f in t.GetFields(F)) if (LooksDrafty(f.Name)) return true;
        foreach (var p in t.GetProperties(F)) if (LooksDrafty(p.Name)) return true;
        return false;
    }

    static bool LooksDrafty(string n)
    {
        return !string.IsNullOrEmpty(n) && n.ToLowerInvariant().Contains("draft");
    }
}
