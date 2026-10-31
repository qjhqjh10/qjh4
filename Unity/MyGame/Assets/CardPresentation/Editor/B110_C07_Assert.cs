// B110_C07_Assert.cs — 执行代理 `B110_D2`（2026-10-31）的断言：死码 **C07**
//   `CampaignPointDrawer.DrawForArmy(CardArmy)` —— 战役奖励窗 `Unlock Button` 里那一格点数图标，
//   原版画的是**该战役阵营的徽记**，不是那张通用的「战役点」图。
//
// ============================ 判据（三句，逐字现读）============================
//   `d:/2/tools/decomp_full/CampaignPointDrawer__DrawForArmy.c:41-55`：
//     ① `armyText.text = GameStaticData.CardArmyToString(army)`（本格不画字，那一半不在这里）
//     ② `image.sprite = ArmyUtilities.GetArmyIcon(army)`      ← `ArmyIconsSO` 的 13 条 `armyIcons`
//     ③ `image.enabled = (sprite != null)`                   ← 阵营认不出 ⇒ **这一格不显示**（不是「画别人的」）
//   ⚠️ 施工单 `判据/B109_R16_死码C45.md:39` 已就地订正：原写「高级列」是错的 —— 原版只有**基础列**走这里
//      （`SetBaseButton.c:68-90` 内联调用），高级列根本不显点数（`A538` 已证）。
//   我们改前：`CampaignRewardWindow.cs` 的 `BuildUnlockButton` 恒 `DrawRect(pi, Art(ArtPointIcon), …)`，
//     而 `CampaignRewardsContext.Army` 那一格**全窗零消费点** ⇒ 任何阵营都长一样（真偏离）。
//
// ============================ 🔴 灭自证（怎么才能红）============================
//   · A6：把那一句改回 `Art(ArtPointIcon)`（固定通用图）⇒ 两个阵营的贴图**相等** ⇒ A6 红，A4/A5 也一起红
//     （它们的期望值是**原版 `IconsByArmySO` 的 sprite 名**，不是我们实现里的任何常量）。
//   · A7：**直怼旧现状** —— 两格贴图只要等于 `CampaignRewardWindow.ArtPointIcon` 就红（这条与 A6 结构上分开：
//     A6 管「两者必须不同」，A7 管「不能是那张通用图」，只改其中一半会各自红一条）。
//   · B3/B4：把 `pi.gameObject.SetActive(pointIcon != null)` 改回 `SetActive(true)` ⇒ 非法阵营那两档**开着** ⇒ 红。
//   · B2 是 B3/B4 的**判别力闸**：合法阵营那一档必须**开着** —— 否则「恒关」也会让 B3/B4 变绿（那不是复刻）。
//
// ============================ 怎么跑 / 怎么接（⚠️ 本代理不动宿主）============================
//   单独跑（编辑模式/批处理都行）：
//     unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//       -projectPath "D:\4\Unity\MyGame" -executeMethod B110_C07_Assert.Run -logFile -
//   接宿主（推荐 = `Editor/RewardsScene.cs`，战役奖励窗那一节就在那儿）：
//     **同一物理行**末尾追加（行数零变化，先例见 B102RAID / B108_A5 的接法）：
//       B110_C07_Assert.Run(); _sink.Pass += B110_C07_Assert.Pass; _sink.Fail += B110_C07_Assert.Fail;
//   ⚠️ 本件**自建/自拆**窗（`Create` + `OpenWindow` + `Close` + `DestroySafe`），不依赖宿主已开着什么窗；
//      收尾把 `WindowsManager` 的 `currentWindow` / `popUpWindow` 与上层的激活态**原样还回去**（见 `Run`）。
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B110_D2`：死码 C07 —— 战役奖励窗点数格 = 该战役阵营的徽记（原版 `CampaignPointDrawer.DrawForArmy`）。</summary>
public static class B110_C07_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B110D2] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // 期望值 = **原版 `IconsByArmySO` 逐条实读的 sprite 名**（`Core/ArmyIcons.cs` 文件头 ② 的原始字节：
    // `sharedassets0.assets` 对象 422 / `m_Script` = MonoScript 660「ArmyIconsSO」）。
    // ⛔ 不从 `ArmyIcons.IconName` 现算（那是被测实现自己那一份 = 自证）。
    const string IconUM = "40k_DeckSelection_icon_FactionUM";           // CardArmy 10  Ultramarines
    const string IconDA = "40k_DeckSelection_icon_FactionDarkAngels";   // CardArmy 110 DarkAngels

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B110_D2 · 死码 C07：战役奖励窗点数格 = 该战役阵营的徽记（原版 `CampaignPointDrawer__DrawForArmy.c`）");

        var wm = WindowsManager.Get();
        Tr(wm != null, "A0 **前提**：拿得到 `WindowsManager`（拿不到 ⇒ 下面全部不可信，⛔ 不是绿）");
        if (wm == null)
        {
            Debug.Log(_sink.P + "B110D2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
            return;
        }

        var saveCur = wm.currentWindow;
        var savePop = wm.popUpWindow;
        bool saveCurActive = saveCur != null && saveCur.gameObject != null && saveCur.gameObject.activeSelf;
        var saveSfx = WarpforgeVFX.WFSoundPlayer.Enabled;
        WarpforgeVFX.WFSoundPlayer.Enabled = false;   // 批处理不真发声（同 `Editor/DeckScene.cs` / `B109_E23` 的先例）；窗会走 `Close()` ⇒ `PlayCloseSound`
        try
        {
            SectionA_TwoArmiesDiffer(wm);
            SectionB_InvalidArmyHides(wm);
        }
        catch (System.Exception ex)
        {
            Tr(false, "（不静默）夹具抛了异常：" + ex.GetType().Name + "：" + ex.Message);
        }
        finally
        {
            WarpforgeVFX.WFSoundPlayer.Enabled = saveSfx;
            // 把 `WindowsManager` 的「当前/弹窗」两格与上层的激活态原样还回去（开场窗会把它压 Background）。
            if (saveCur != null && saveCur.gameObject != null) saveCur.gameObject.SetActive(saveCurActive);
            wm.currentWindow = saveCur;
            wm.popUpWindow = savePop;
        }

        Debug.Log(_sink.P + "B110D2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 两个阵营 ⇒ 两张不同的图
    static void SectionA_TwoArmiesDiffer(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "A 两个不同 `ctx.Army` ⇒ 点数格贴图**必须不同**（原版 `image.sprite = ArmyUtilities.GetArmyIcon(army)`）");

        var pUM = Probe(wm, 10);
        var pDA = Probe(wm, 110);

        Tr(pUM.NodeFound && pDA.NodeFound,
           "A1 **前提**：两扇窗里都建出了 `Icon Campaign Points Drawer Variant`（不在 ⇒ 后面几条等于没比）");
        Tr(pUM.NodeActive && pDA.NodeActive,
           "A2 **前提（判别力）**：两档都是**付点支**（`PointCost=100` / 未领 / 基础列）⇒ 节点**开着**"
        + "（关着的话贴图再对也不会画出来）");
        Tr(pUM.QuadBuilt && pDA.QuadBuilt, "A3 **前提**：那一格都真的建了 `ImageQuad`（拿不到 ⇒ 比贴图无从谈起）");

        C(pUM.TexName, IconUM,
          "A4 Army=10(Ultramarines) ⇒ 贴图 = `" + IconUM + "`（**原版 `IconsByArmySO` 那一条**，⛔ 不是通用战役点图）");
        C(pDA.TexName, IconDA,
          "A5 Army=110(DarkAngels) ⇒ 贴图 = `" + IconDA + "`（同表另一条）");

        Tr(!string.IsNullOrEmpty(pUM.TexName) && pUM.TexName != pDA.TexName,
           "A6 ★ **灭自证**：两个阵营的贴图**必须不同** —— 🧨 改回 `Art(ArtPointIcon)` ⇒ 两条都等于 `"
         + CampaignRewardWindow.ArtPointIcon + "` ⇒ 本条与 A4/A5 一起红");

        Tr(pUM.TexName != CampaignRewardWindow.ArtPointIcon && pDA.TexName != CampaignRewardWindow.ArtPointIcon,
           "A7 ★ **直怼旧现状**：两格贴图都**不是** `" + CampaignRewardWindow.ArtPointIcon + "`"
         + "（改前那一版两格恒等于它 —— 这一条就是给那个现状判红的）");
    }

    // ============================================================ B · 阵营非法 ⇒ 这一格不显示
    static void SectionB_InvalidArmyHides(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "B `ctx.Army` 非法 ⇒ 那一格**不显示**（原版 `image.enabled = (sprite != null)` —— 认不出阵营就没有图）");

        var pNeutral = Probe(wm, 0);        // `CardArmy.Neutral` —— 原版 `IconsByArmySO` 里**没有**这条
        var pBad = Probe(wm, 999);          // 不存在的枚举值
        var pOK = Probe(wm, 10);            // 对照组：合法阵营

        Tr(pNeutral.NodeFound && pBad.NodeFound,
           "B1 **前提**：两扇「非法阵营」窗里节点都**建出来了**（节点在、只是不显示 —— 与「压根没建」是两回事）");
        Tr(pOK.NodeActive,
           "B2 **前提（判别力）**：合法阵营（Army=10）那一档**是开着的** —— 否则「恒关」也会让 B3/B4 变绿，那不叫复刻");
        Tr(!pNeutral.NodeActive,
           "B3 Army=0（`Neutral`，原版没有它的徽记）⇒ 节点 **activeSelf == false**"
         + " —— 🧨 把那句改回 `SetActive(true)` ⇒ 本条红");
        Tr(!pBad.NodeActive,
           "B4 Army=999（不存在的枚举值）⇒ 节点 **activeSelf == false**");
        Tr(!pNeutral.QuadBuilt && !pBad.QuadBuilt,
           "B5 这两档**一块图都没建**（`ImageQuad` 数量 0 —— 取不到图就不该凭空建一块）");
    }

    // ============================================================ 夹具
    struct ProbeResult
    {
        public bool NodeFound;      // 节点建出来了
        public bool NodeActive;     // 节点自己 `activeSelf`
        public bool QuadBuilt;      // 节点下有 `ImageQuad`
        public string TexName;      // 那张图的贴图名（没有 ⇒ null）
    }

    /// <summary>建一扇**真实**的战役奖励窗（生产入口 `Create` + `OpenWindow`，⛔ 不复制建法），
    /// 读那一格点数图标的现场，然后关窗销毁。</summary>
    static ProbeResult Probe(WindowsManager wm, int army)
    {
        var res = new ProbeResult();
        CampaignRewardWindow win = null;
        try
        {
            win = CampaignRewardWindow.Create(wm);
            if (win == null) return res;
            var ctx = new CampaignRewardsContext
            {
                // 只喂**基础档**一条 ⇒ 只建基础列（高级列不建 ⇒ 整窗里那一格名字唯一，不会抓错）
                Rewards = new[] { new CampaignData.RewardSpec("UM_SK_Starter", 1, CampaignData.TierBasic) },
                BaseCollected = false,
                PremiumCollected = false,
                Claimable = true,
                IsPremiumLocked = false,
                PointCost = 100,                    // ≥1 ⇒ 走「付点支」（原版那一支才会开 pointDrawer）
                Army = army,
            };
            wm.OpenWindow(win, ctx);

            var pi = MenuCheck.FindChild(win.transform, "Icon Campaign Points Drawer Variant");
            res.NodeFound = pi != null;
            if (pi == null) return res;
            res.NodeActive = pi.gameObject.activeSelf;
            var q = pi.GetComponentInChildren<ImageQuad>(true);
            res.QuadBuilt = q != null;
            if (q != null && q.Texture != null) res.TexName = q.Texture.name;
            return res;
        }
        finally
        {
            if (win != null)
            {
                try { win.Close(); } catch { }
                MainMenuSubmenuWindow.DestroySafe(win.gameObject);
            }
        }
    }
}
