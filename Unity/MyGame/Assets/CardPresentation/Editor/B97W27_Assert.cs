// B97W27_Assert.cs — 执行代理 `B97_W27`（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么（以及为什么【一行产品代码都没改】）============================
//   派活前提（施工单 `判据/B97_R5_零散剩余施工单.md` §四·4 + 账 `未分类/其他:584` 的尾部备注）：
//     「`Shell/ProfileTab.cs:1298` 已建 `ChooseNameWindow`，真缺口 = **它内层**的 `Login Window` 分支
//      （`ChooseNameWindow / Login Window / ErrorMensajeContainer / Animated Loading Image / Cog`）没建。」
//   🔴 **本轮回源把这个前提推翻了**（三个独立源，逐条都在 `报告/B97_W27.md` §二）：
//     ① `Player Profile Window.prefab`（AssetRipper 导出工程）：内嵌那颗 `ChooseNameWindow`
//        节点挂的脚本是 **`ChangeNameWindow`**（guid `1f3eac8290896c7b31cd391cec766d93`），
//        它的子树里 **`Login` 开头的节点一个都没有**（`grep -c Login` = 0）；
//     ② `Scripts/Assembly-CSharp/ChangeNameWindow.cs` 的字段 = `changeNameButton` / `closeButton` /
//        `nameInput` / `priceDisplayInButton` / `freeTextInButton` —— **没有 `loginWindow`**；
//     ③ 原版包侧复核（`工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 14 --md`）：
//        同一节点 `·····5 | ChooseNameWindow … | UIAnchorAvoidSafeArea,ChangeNameWindow`，
//        子节点只有 `Choose Name Input Field` / `MessageText` / `Change Name Button` 那一套。
//     ⇒ **含 `Login Window` 分支的是另一扇窗**：`ChooseNameWindow.prefab`（根脚本 **`ChooseNamePopup`**，
//       guid `9e0933d43bad33c489009d3b2dd2a151`；字段 `loginWindow → BasicLoginWithEmailWindow`）。
//       那是「开局没有显示名时向玩家要名字」的弹窗，本仓**整扇窗都没建**（账 `菜单_外壳.tsv:77-88`
//       全部按「等价实现 = `ProfileTab.BuildNameWindow`」记着）。
//     ⇒ 而且那一支**永不显示**：`Login Window` 在两个实例里都是 `m_IsActive = 0`，唯一打开链 =
//       `ChooseNamePopup.LoginButtonClick()` → `WindowsManager.OpenWindow(loginWindow)`，
//       而那条账 `菜单_外壳.tsv:80` 已判 **D**（离线无登录）。
//     ⇒ **结论：不建**（往 `ChangeNameWindow` 里塞这棵分支 = 造原版没有的节点）。
//       同族先例 = 同一份施工单 §四·2（「永久 inactive 的死支 ⇒ 不建 = 无观测差异，不是未接线的缺口」）。
//       逐字段施工规格留在 `报告/B97_W27.md` §三 —— 若调度台改判要建，照那份做，⛔ 别凭印象加。
//
//   于是本文件不退化成「什么都不断」，而是把**这条事实钉死**：谁哪天把 `Login Window`（或那颗 `Cog`）
//   塞进改名窗，B 段立刻红，逼他先读 `报告/B97_W27.md` 再决定。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   宿主里加一行（`pf` = 该宿主里已有的 `Profile Tab` 那一层 `Transform`）：
//       B97W27Assert.Run(pf);
//   ＋ 两句计数转发（照 `B94B18Assert` / `B97W14Assert` 的先例）：
//       _sink.Pass += B97W27Assert.Pass; _sink.Fail += B97W27Assert.Fail;
//   · **推荐宿主 = `Editor/MainMenuScene.cs`（`MainMenuScene.Run`）** —— 它本来就建了整座档案窗，
//     并在 ⑤ 那一节已经断言过 `ChooseNameWindow`（`var cnw = FindChild(pf, "ChooseNameWindow");`）。
//     接在那一节**之后**即可；`CheckSink` 的 `Near` 口径本文件按 `Compact3` 设。
//   · 本件**不建持久资产**、不碰 `PlayerPrefs`、不跑 Unity 批处理；建的 `GameObject` 全在 `finally` 里
//     `DestroyImmediate`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `BuildNameWindow` 整段删掉 ⇒ A1 红；把窗改成出厂开 ⇒ A2 红（原版 `m_IsActive=0`）。
//   · B 段（**本文件的主断言**）：往 `BuildNameWindow` 里加任何一个 `Login Window` / `Login Button ` /
//           `Logged Text` / `Link Accounts message` / `OkButton` / `ErrorMensajeContainer` / `Cog` 节点
//           ⇒ 对应那条**立刻红**（按名字逐条断，红的那条直接点名是哪个节点）。
//           这一条**不是**「我们没做所以断没有」——它断的是**原版 `ChangeNameWindow` 变体确实没有这些东西**
//           （判据见本节头 ①②③），所以「加进去」才是偏离。
//   · C 段：把 `SettingsWindow.CogDegPerSec` 改成 `0f` / `+20f` ⇒ C1 / C2 红（两个方向都红）；
//           把 `AdvanceCog` 里那句 `activeInHierarchy` 闸删掉 ⇒ C3 红（关着也转）。
//   · 期望值全部是**字面量**（`340` / `320`），来自原版 prefab 的 `speed = (0,0,-20)` 与
//     `decomp_full/SimpleRotation__Update.c`，⛔ 一个都不从被测实现里读（不读 `CogDegPerSec`、不反算 `Step`）。
//   · ⛔ 没有 `if (x != null) Check(...)` 这种「前提不成立就静默跳过」的写法：前提也是硬 `CheckTrue`。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_W27`：`Profile Tab` 那扇改名窗**不该**有 `Login Window` 分支
/// （= 原版 `ChangeNameWindow` 变体的事实；含该分支的是**另一扇** `ChooseNamePopup` 窗，本仓未建、
/// 且它的唯一打开链已判 D）。外加一条「兄弟实例」（设置窗那颗 `Cog`）的转速回归。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W27Assert
{
    static readonly CheckSink _sink = new CheckSink("[W27] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Nr(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>原版 `SimpleRotation.speed.z`（判据 = `ChooseNameWindow.prefab` 的 `Cog` 组件
    /// `114274303670636188` 的 `speed = {0,0,-20}`；与设置窗那颗 `MonoBehaviour_-2329547480181669978` 同值）。
    /// ⛔ 刻意写成字面量：别改成读 `SettingsWindow.CogDegPerSec`（那是被测实现 = 自证）。</summary>
    const float ExpectDegPerSec = -20f;

    // ============================================================ 原版 `ChangeNameWindow` 变体的节点名清单
    //
    // 🔴 **B 段断的是「这些名字在我们这扇窗里一个都不该出现」** —— 全部来自
    //    `ChooseNamePopup.prefab`（**另一扇窗**）的 `Login Window` 子树 + 它自己的兄弟节点。
    //    判据 = 报告 §三 的节点树（逐节点取自 `ChooseNameWindow.prefab`）＋ §二 的变体取证。
    static readonly string[] PopupOnlyNames =
    {
        "Login Window",          // BasicLoginWithEmailWindow 窗体（m_IsActive = 0）
        "Login Button",          // ChooseNamePopup 自己那颗（把 Login Window 打开）
        "Login Button ",         // ⚠️ 名字**末尾带一个空格**（原版真值，`m_Name: 'Login Button '`）
        "Logged Text",           // 已登录时替换成的那句
        "Link Accounts message", // 关联账号那段
        "OkButton",              // popup 的确认钮（变体里叫 `Change Name Button`）
        "Backgroun filler",      // Login Window 内部的填充层（m_IsActive = 0）
        "EmailText",             // ┐
        "InputEmail",            // │ Login Window 那一支的
        "PasswordText",          // │ 输入件与错误框
        "InputPassword",         // │
        "Forgot Password",       // │
        "ErrorMensajeContainer", // │
        "Animated Loading Image",// │
        "Cog",                   // ┘ ← 第 3 颗 Cog 就在这里面
    };

    // ============================================================ 入口
    /// <summary>跑本节全部断言。<paramref name="pageRoot"/> = 档案窗那一层（`Profile Tab` 或它的父），
    /// 本方法自己往下找 `ChooseNameWindow`（⛔ 找不到就红，不静默跳过）。</summary>
    public static void Run(Transform pageRoot)
    {
        MenuCheck.Section(_sink, "W27 `Profile Tab` 改名窗**不该**有 `Login Window` 分支（原版 `ChangeNameWindow` 变体）");

        Tr(pageRoot != null,
           "前提 W27-0：宿主传进来的档案窗根 `Transform` 不是 null"
         + "（宿主那一行 = `B97W27Assert.Run(pf);`；传 null ⇒ 红，⛔ 不静默跳过）");
        if (pageRoot == null) return;

        var cnw = FindDeep(pageRoot, "ChooseNameWindow");
        Tr(cnw != null,
           "前提 W27-1：`ChooseNameWindow` 建出来了（`ProfileTab.BuildNameWindow` 那一棵）—— "
         + "🧨 整段删掉 ⇒ 红");
        if (cnw == null) return;

        SectionA_变体身份(cnw);
        SectionB_没有LoginWindow分支(cnw);
        SectionC_兄弟实例的Cog仍在转();
    }

    // ============================================================ A 变体身份
    static void SectionA_变体身份(Transform cnw)
    {
        MenuCheck.Section(_sink, "A 这扇窗是「改名」变体（不是 `ChooseNamePopup`）");

        C(cnw.name, "ChooseNameWindow",
          "A0 节点名 == `ChooseNameWindow`（原版 `Player Profile Window` 里那颗内嵌节点的名字）");

        Tr(!cnw.gameObject.activeSelf,
           "A1 出厂 **关着**（原版 prefab `m_IsActive = 0` + `ProfileTab.Start/BuildNameWindow` 末尾 SetActive(false)；"
         + "🧨 改成出厂开 ⇒ 红）");

        Tr(FindDeep(cnw, "Change Name Button") != null,
           "A2 有 `Change Name Button`（= `ChangeNameWindow` 变体的标志节点；原版 `ChooseNamePopup` 那颗叫 `OkButton`）"
         + "｜🧨 换成 `OkButton` ⇒ 这一条红、B8 也红");

        // 面板的**坐标**归 `Editor/MainMenuScene.cs` ⑤ 那一节断（519.49,395.00→1400.51,696.03）；
        // 这里只断「这一棵是我们建过的那一棵」——⛔ 不重复第二份坐标期望值（两处写 = 迟早不一致）。
        Tr(FindDeep(cnw, "Generic Popup Background") != null, "A3 有 `Generic Popup Background`（面板九宫格 `40k_popup`）");
        Tr(FindDeep(cnw, "MessageText") != null, "A4 有 `MessageText`（原版 `m_text = 'Choose your player name'` 那一格）");
        Tr(FindDeep(cnw, "Choose Name Input Field") != null, "A5 有 `Choose Name Input Field`（`40K_dropdown_bg` 输入框）");
    }

    // ============================================================ B 灭自证：分支一个都不该有
    static void SectionB_没有LoginWindow分支(Transform cnw)
    {
        MenuCheck.Section(_sink, "B `Login Window` 那一支**一个节点都不该有**（它属于另一扇窗 `ChooseNamePopup`）");
        for (int i = 0; i < PopupOnlyNames.Length; i++)
        {
            string n = PopupOnlyNames[i];
            var hit = FindDeep(cnw, n);
            Tr(hit == null,
               "B" + (i + 1) + " 改名窗子树里**没有** `" + n + "` —— 它属于 `ChooseNamePopup.prefab` 的 `Login Window` 那一支"
             + "（判据 = 报告 §二：`Player Profile Window.prefab` 内嵌节点脚本 = `ChangeNameWindow`、子树 0 个 Login 节点；"
             + "`ChangeNameWindow.cs` 无 `loginWindow` 字段）｜🧨 往 `BuildNameWindow` 里加它 ⇒ 这条红："
             + "先读 `资料/完全复刻/报告/B97_W27.md`，别凭施工单旧话加");
        }
    }

    // ============================================================ C 兄弟实例：设置窗那颗 Cog（唯一已建的那一颗）
    static void SectionC_兄弟实例的Cog仍在转()
    {
        MenuCheck.Section(_sink, "C 同一颗 `Cog`（`BasicLoginWithEmailWindow` 的另一实例）在设置窗里仍按原版转：−20°/s 且关着冻住");
        SettingsWindow w = null;
        try
        {
            w = SettingsWindow.BuildCogFixtureForTest(null);
            Tr(w != null, "前提 C-0：`SettingsWindow.BuildCogFixtureForTest` 建出了窗（⛔ 不走 `Create` ⇒ 不留 `Instance` 副作用）");
            if (w == null) return;
            Tr(w.CogNodeForTest != null,
               "前提 C-0b：`Cog` 那颗**真的建出来**了（贴图 `40K_icon_searching_cog` 缺失 ⇒ 红，⛔ 不静默跳过）");
            if (w.CogNodeForTest == null) return;
            C(w.CogNodeForTest.name, "Cog", "前提 C-0c：节点名 == `Cog`");

            // 出厂态：`ErrorMensajeContainer` 关着（= 原版 `BuildLoginWindow` 末尾那句 SetActive(false)
            //          + `BasicLoginWithEmailWindow.Open` 里的 `waitingResponseObject.SetActive(0)`）
            Tr(!w.CogNodeForTest.gameObject.activeInHierarchy,
               "前提 C-0d：建完时 `Cog` **不在**活跃层级里（原版 `Open()` 就把 `waitingResponseObject` 关掉）");

            w.PumpCogForTest(1f);
            C(w.CogAngleForTest, 0f,
              "C3 框关着时推进 1 秒 ⇒ **仍是 0°**（原版 `SimpleRotation.Update` 在 inactive 物体上根本不被调；"
            + "🧨 删掉 `AdvanceCog` 里那句 `activeInHierarchy` 闸 ⇒ 得 340 ⇒ 红）");

            // 亮起来（= 原版 `waitingResponseObject.SetActive(1)` 那一刻）⇒ 从 0 接着转
            var load = w.CogNodeForTest.parent;
            var box = load != null ? load.parent : null;
            Tr(box != null, "前提 C-1：`Cog → Animated Loading Image → ErrorMensajeContainer` 那条父链还在");
            if (box == null) return;
            // 🔴 **2026-10-30（`B100_F6`）就地订正（铁律 5）**：祖先 `Login Window` **出厂是关着的**
            //   （`BuildLoginWindow` 末句 `lw.gameObject.SetActive(false)` = `Shell/SettingsWindow.cs:3169`）
            //   ⇒ 只亮 `ErrorMensajeContainer` 时 `activeInHierarchy` 仍是 **false**（祖先 inactive）。
            //   实测（2026-10-30 `b100_menu.log:161928` 起）：C-2 红 ⇒ 下面 C1/C2/C4 三条**恒 0**。
            //   ⛔ 别改用「Cog 自己 activeSelf」判活跃 —— 那正是本条要挡的假绿。
            var lwNode = box.parent;
            Tr(lwNode != null && lwNode.name == SettingsWindow.LwNodeName,
               "前提 C-1b：`ErrorMensajeContainer` 的父节点 == `Login Window`（整棵弹窗根；出厂 inactive）");
            if (lwNode == null) return;
            lwNode.gameObject.SetActive(true);              // = 原版 `LoginButtonClick` 把整扇弹窗亮起来那一刻
            box.gameObject.SetActive(true);
            Tr(w.CogNodeForTest.gameObject.activeInHierarchy,
               "前提 C-2：亮起来之后 `Cog` 在活跃层级里（否则下面在转空气）");

            w.PumpCogForTest(1f);
            Nr(w.CogAngleForTest, 340f, 0.001f,
               "C1 亮着推进 1 秒 ⇒ 340°（= 0 + (−20)×1 归一；🧨 转速改 0 ⇒ 0、改 +20 ⇒ 20 ⇒ 红）");
            Nr(Mathf.DeltaAngle(0f, w.CogAngleForTest), ExpectDegPerSec, 0.001f,
               "C2 **有符号变化量 = −20°/s**（🧨 改 +20 ⇒ +20 ⇒ 红；改 0 ⇒ 0 ⇒ 红 —— 两个方向都红）");

            w.PumpCogForTest(2f);
            Nr(w.CogAngleForTest, 300f, 0.001f,
               "C4 再单帧推进 2 秒 ⇒ **300°**（340 + (−20)×2；🔴 2026-10-30 订正：原期望写 320 —— 那正是**漏乘 dt** 的坏读数，把改坏法的值当成了期望值）"
             + "（🧨 把 `speed * dt` 写成 `speed` ⇒ 一帧只减 20 ⇒ 得 320 ⇒ 红）");
        }
        finally
        {
            if (w != null) Object.DestroyImmediate(w.gameObject);
        }
    }

    // ============================================================ 工具
    /// <summary>在 <paramref name="root"/> 的**整棵子树**里找第一个叫 <paramref name="name"/> 的节点（不含 root 自己）。</summary>
    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name == name) return c;
            var deep = FindDeep(c, name);
            if (deep != null) return deep;
        }
        return null;
    }
}
