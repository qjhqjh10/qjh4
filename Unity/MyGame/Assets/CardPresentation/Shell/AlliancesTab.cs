// AlliancesTab.cs — 社交窗第 1 页：**联盟**（原版 `AlliancesTab`，`WindowTabBase<SocialMenuWindow>`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/社交_联盟与好友页.md` §A·1（主树的 **47–379 行**）· §A·2·1/§A·2·2（两款行族的独立根）·
// §B·4/§B·6/§B·7/§B·11/§B·12/§B·13 · §C。**入口/调用/监听** → `资料/普查产出_0927/多人界面_入口与调用.md` §③。
//
// ---- 🔴 这一页的结构（读原始 JSON 定的，别按印象改）----
// `Alliances Tab`
//   ├ `AllianceNotMemberVariant`（`AllianceSearchTab`）**act = T** ← 默认就是这一支
//   │    ├ `Alliance Header Buttons` → `Divisor line` + `Tab buttons`（**两个 `EverguildToggle`**，
//   │    │     文案 `Join` / `Create` —— ⚠️ 它们**不是 `Button`**，是页签键，`AllianceSearchTab`
//   │    │     的两个字段 `joinButton`/`createButton` 指它们，点了切下面两个视图）
//   │    ├ `List View`（`JoinAllianceMenu`）—— 搜索框 + `Invitations` 列表 + `Open Alliances` 列表
//   │    ├ `Create Alliance View`（`CreateAllianceMenu`）**act = F** ← 点 `Create` 才亮
//   │    └ `GeneralDetails`（= 原版 `AllianceView`，**act = F**，RT `-8680982849342087005`）
//   │           ← 🆕 2026-10-04（A55②）：「**点公开列表里某个盟 → 看它的详情**」那一态，
//   │             原版由 `AllianceSearchTab.HandleDisplayAlliance` 点亮 —— 见 `BuildGeneralDetails()`
//   └ `AllianceMemberVariant`（`AllianceMemberTab`）**act = F** ← 已在盟里那一支（见 `AllianceMemberTab.cs`）
//
// 🔴 **两个 `GeneralDetails` 不是同一份**（§B·5）：`AllianceMemberVariant>GeneralDetails`
//    （RT `-4327119531760820061`，act T）与 `AllianceNotMemberVariant>GeneralDetails`
//    （RT `-8680982849342087005`，act **F**），同名不同 pid，靠 `AllianceMemberTab.generalView` /
//    `AllianceSearchTab.allianceView` 两个字段分清 —— **合并成一份会同时弄错两态**。
//    我们的做法：**一份 builder、建两棵**（`AllianceGeneralDetails.Build(..., Variant)`）。
//    ⚠️ **2026-10-04 更正（A35③ 审查查出）**：这一行原来写着「**建两棵独立的树**」—— 当时**不成立，实际只建了一棵**
//    （挂在 `AllianceMemberVariant` 下的那一棵，`AllianceSearchTab` 那一侧**一个 `GeneralDetails` 都没有**）；
//    更要紧的是那**一棵**的几何还是**两份实例各取一半**（上半段 = `AllianceNotMemberVariant` 那份 act F，
//    下半段 = `AllianceMemberVariant` 那份 act T）。✅ **两笔都在 2026-10-04 收掉了**：
//    A29 把那一棵整套换成 act T；**A55②** 把**缺的 act F 那棵**补上（本文件的 `BuildGeneralDetails()`，
//    几何 = `GeoF`、出厂 act F，由 `HandleDisplayAlliance` 点亮）。逐节点清单与两套几何的判据见
//    `AllianceMemberTab.cs` 的 `AllianceGeneralDetails`（`GeoT` / `GeoF` 各一行带普查行号）。
//
// ---- 出厂 act=F 的件：**按「可切到的状态 / 装饰」二分**（别一刀切）----
//   · 可切到的状态（点一下就会亮）⇒ **建**：`Create Alliance View`（点 `Create` 键）；
//     🆕 `AllianceNotMemberVariant>GeneralDetails`（**未入盟支那一棵**，出厂 act F —— 见下面 `BuildGeneralDetails()`）；
//   · 只是「编辑态/未激活态」的件 ⇒ **不建**，逐条列在下面（每处都写明为什么）：
//     `Config fields` 下的 `LanguagesDropdown` / `Privacy Dropdown` / `Edit` / `Confirm` / `Cancel`
//     （它们在**未入盟支那一棵** `GeneralDetails` 里是 act F —— 那五个属于**改盟设置**那一态，改设置要服务器）、
//     两处 `Secondary Icon`（act F）、
//     🔴 **2026-10-04 更正（A55①）** —— 这一条原来写「**五个全是 act F**」，**是把另一份实例的值当成了通用值**：
//        `Config fields` 在 prefab 里**有两份**（同 `GeneralDetails` 一样同名不同 pid，见 §B·5）：
//        · 未入盟支那份（`AllianceNotMemberVariant>GeneralDetails`，普查 `社交_联盟与好友页.md:186-222`）：
//          `extra_info` = **T**、上面那五个 = **F**（`:187` vs `:188,201,214,217,220`）；
//        · 已入盟支那份（`AllianceMemberVariant>GeneralDetails`，普查 `:279-315`）：**正好相反** ——
//          `extra_info` = **F**（`:280`）、那五个 = **T**（`:281,294,307,310,313`）。
//        ⇒ 「五个全是 act F」**只对未入盟支那一棵成立**；`AllianceMemberTab` 那一棵（act T 那份）
//        **五个全建**（A29 已按 act T 接上，见 `AllianceMemberTab.cs` 的 `AllianceGeneralDetails.Build`）。
//        本条留在「不建」这一节，管的是**未入盟支那棵树**（本文件辖内，见下面 `BuildGeneralDetails()`）。
//     `GeneralDetails>DEBUG_TEXTS`（原档调试残留，§C·2 明说**别照抄**）、
//     两个 `TMP_Dropdown` 的 `Template`（**Unity 内置模板**，`Item Label = 'Option A'` —— §B·13 明说别当业务节点）。
//
// ---- 数据 ----
// 邀请 / 公开联盟 / 成员**全在服务器** ⇒ `SocialData` 三张表**默认恒空** ⇒ 三个列表**一行都不建**
// （原版那几个行实例是美术原位参照，运行期 `FillGroupInvitations`/`FillOpenAlliances` 会清掉重填，
//   判据 → `多人界面_入口与调用.md` §③ 社交那一条）。留白、不编。
//
// ============================ 🔴 对齐（`alignLeft`）那 15 处：判据只写在这里 ============================
// **A255（2026-10-11 落地）**。原版那批 TMP 的 `m_HorizontalAlignment` **逐件不同**，而
// `SocialWindow.Text` 的 `alignLeft` **曾经缺省 `true`** ⇒ 当时不显式声明的调用点**一律被左对齐**（= 真偏离）。
// 🔴 **2026-10-12（A323 · 收尾半）**：那个缺省**已经删掉、形参必填** ⇒ 本文件**15 处全部显式声明**
//   （下面那张表左列 6 处 + 右列 9 处 —— 右列那 9 处本批按原版现读结果各补了 `alignLeft: true`，
//   **零行为变化**，只是把「跟着缺省走」改成「写明」）。
// 判据命令（一条，逐行读 `对齐=` 列）：
//   `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant" --depth 16 --md`
// ⛔ **当初不能把缺省翻过来**（那会一次改掉 9 处**本来对**的）⇒ **逐处显式声明**。
//
// | 原版 `Center` ⇒ 本文件传 `alignLeft: false`（**6 处**，A255 那次改的） | 原版 `Left` ⇒ 传 `alignLeft: true`（**9 处**，A323 补的显式声明） |
// |---|---|
// | `Alliance Header Buttons/Tab buttons/Generic Tab UI Button {Search,Create}/Button Text`（`Center/Midline`，fs60 auto[12~60]） | `List View/Search Field/…/Placeholder`（`Left/Midline`，`Search`） |
// | 行 `Members Header`（`Center/Middle`，fs38.35） | `Invitations/Title` · `Open Alliances/Title`（`Left/Middle`，fs47.5） |
// | 行 `Member Count`（`Center/Middle`，fs50） | 行 `Title`（`Left/Midline` fs55.9）· 行 `Region`（`Left/Midline` fs47.05） |
// | 行 `Ranking Header`（`Center/Middle`，fs38.35） | 行 `Ranking Value`（`Left/Midline`，fs50） |
// | 行尾 `Join`/`Reject` 的 `Button Text`（`Center/Midline`，fs36.65/44） | `Create Alliance Text`（`Left/Middle`，fs40） |
// | `…/Price Display Button/…/Price Display/text`（`'1000'`，`Center/Capline`） | `Name input title` · `Desc input title`（`Left/Midline`）· `Select language`/`Select privacy`（`Left/Middle`） |
//
// 🔴 **`SocialWindow.Text` 的 `alignLeft: false` 只是「不挪节点」**（`MenuDraw.AlignLeft` 会把整块字推到矩形
// 左边缘）⇒ 标签留在 `MenuDraw.Text` 建的**矩形中心**，而 `TmpFont.NewText` 一律建 `Center` ⇒ 画出来就是原版的居中。
// ⚠️ **连锁**：`Editor/MainMenuScene.cs` 那条钉 `Price Display … text` **左边缘 = 546.75** 的断言（A214③）
//   是「`alignLeft: true` 之下」的写法 ⇒ 这一处改居中之后它**要按新判据改写成量中心**（A255 那一行预告过）。
//
// ============================ 🔴 字号窗口（`autoMaxPx`/`autoBasePx`）那 15 处：判据只写在这里 ============================
// **A414（2026-10-13 落地）**。原版 `SocialWindow.Text` 那个口 A406 就开好了（尾参 `autoMaxPx` /
// `autoBasePx` = 原版 `m_fontSizeMax` / `m_fontSizeBase`，**都 `<= 0` ⇒ 旧行为**），但 A406 当时
// 白名单里没有本文件 ⇒ **本文件 15 处一个都没传**（`SocialWindow.cs` 里那句「既有调用点一个都不用改」
// 只对「加形参不会编不过」成立、**不等于「没有真值可填」** —— A414 已就地订正那句话）。
// 判据命令（同上那一条，逐行读 `基准=` 与 `auto[…]` 两列）：
//   `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant" --depth 16 --md`
// 🔴 **下面这些数一律是【原版读数】，⛔ 不是我们自己的常量** —— 所以调用点上写的是**字面量**
//   （写成 `TabBtnPx` 那种就是拿我们的常量当判据 = **自证**）。
// ⚠️ **上限 ≠ 标称**才是这一批的要害（旧写法 `autoMaxPx <= 0` ⇒ 上限被写成 `fontPx` ⇒ **短文案永远画小一档**）。
//
// | # | 建在哪（调用点） | 原版节点（`Social Submenu Variant/…`） | max | base |
// |---|---|---|---|---|
// | 1 | `TabToggle` | `…/Alliance Header Buttons/Tab buttons/Generic Tab UI Button {Search,Create}/Button Text` | **60.0** | 12.0 |
// | 2 | `BuildList` 搜索框 | `…/AllianceNotMemberVariant/List View/Search Field/Text Area/Placeholder` | **50.0** | **26.0** |
// | 3 | `BuildList` | `…/List View/List Area/Invitations/Title` | **72.0** | 36.0 |
// | 4 | `BuildList` | `…/List View/List Area/Open Alliances/Title` | **72.0** | 36.0 |
// | 5 | `RowTexts` | `…/{Invitation List Entry,Entry}/Title` | **72.0** | 36.0 |
// | 6 | `RowTexts` | `…/{Invitation List Entry,Entry}/Region` | **72.0** | 36.0 |
// | 7 | `RowTexts` | `…/{Invitation List Entry,Entry}/Members Header` | **72.0** | 36.0 |
// | 8 | `RowTexts` | `…/{Invitation List Entry,Entry}/Member Count` | 50.0 | 36.0 |
// | 9 | `RowTexts` | `…/{Invitation List Entry,Entry}/Ranking Header` | **72.0** | 36.0 |
// | 10 | `RowTexts` | `…/{Invitation List Entry,Entry}/Ranking Score/Ranking Value` | 50.0 | 36.0 |
// | 11 | `RowButton` | `…/{Invitation List Entry/Join,Reject,Entry/Generic UI Button}/Button Text` | 44.0 | 12.0 |
// | 12 | `BuildCreateView` | `…/Create Alliance View/TopAnchor/Create Alliance Text` | 40.0 | 36.0 |
// | 13 | `BuildCreateView` | `…/Create Alliance Text/Price Display Button/…/Price Display/text` | 40.0 | **39.0** |
// | 14 | `Field` | `…/Create Alliance View/TopAnchor/{Name,Desc} input title`（两处共用本行） | 40.0 | 36.0 |
// | 15 | `Dropdown` | `…/Create Alliance View/TopAnchor/{Select Language,Select Privacy}`（两处共用本行） | 40.0 | 36.0 |
//
// ⚠️ **#13 的 base 是 39.0**（不是 36、也不是标称 40）—— 同族在别处是 36（铁律 5·c：**一个值 ≠ 全部情况**，
//   逐站现读，⛔ 别一刀切）。
// ⚠️ `autoBasePx` 只改自适应的**二分起点**（`MenuDraw.TextBox` 的头写着：终端两侧都收敛到
//   「装得下的最大号」）⇒ 它**不改变**上限那一档的效果；两格都要填是因为**两格都是原版字段**。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `AlliancesTab`。持有**两支**（`allianceNotMemberVariant` / `allianceMemberVariant`）。</summary>
    public class AlliancesTab : SocialPage
    {
        public override WindowTabType Type { get { return WindowTabType.SocialAlliances; } }
        protected override int PageIndex { get { return 0; } }

        // 队列档：本页 10 个号里，**两支各占 5 个**（未入盟支 0–4、入盟支 5–9）——
        // 两支互斥，但这样即使同时亮也不会打架（入盟支整片盖住未入盟支）。
        public const int QBandSearch = 0, QBandMember = 5;

        /// <summary>`AllianceNotMemberVariant`（`AllianceSearchTab`，**act T**）—— 默认这一支。</summary>
        public AllianceSearchTab Search { get; private set; }
        /// <summary>`AllianceMemberVariant`（`AllianceMemberTab`，**act F**）—— 已在盟里那一支。</summary>
        public AllianceMemberTab Member { get; private set; }

        public void SetHost(SocialWindow win) { Win = win; }

        public override void Setup()
        {
            // ---- `AllianceNotMemberVariant`：332.17,70.90→1919.50,1080.02（§A·1 第 48 行）----
            var nm = Node(Root, "AllianceNotMemberVariant", new PxRect(332.17f, 70.90f, 1919.50f, 1080.02f));
            Search = nm.gameObject.AddComponent<AllianceSearchTab>();
            Search.Page = this;
            Search.Build();

            // ---- `AllianceMemberVariant`：332.67,162.04→1919.00,1080.02（§A·1 第 302 行附近）----
            //   ⚠️ 出厂 act=F ⇒ 建完就关掉（它的内容仍然建出来，「可切到的状态」那一类）。
            var mv = Node(Root, "AllianceMemberVariant", new PxRect(332.67f, 162.04f, 1919.00f, 1080.02f));
            Member = mv.gameObject.AddComponent<AllianceMemberTab>();
            Member.Page = this;
            Member.Build();
            Member.gameObject.SetActive(false);

            Debug.Log("[Social] 联盟页：默认 `AllianceNotMemberVariant`（未入盟）。"
                    + "⚠️ 原版按 `AlliancesManager.CurrentGroupCached`（服务器）二选一 ⇒ **本地恒走未入盟支**，"
                    + "`AllianceMemberVariant` 建了但**走不到**（如实记着，不是漏做）。");
        }

        /// <summary>联盟名（`Alliance name text`）—— 两支共用一份数据源时用得上；本地恒空。</summary>
        public string AllianceName { get { return SocialData.AllianceName; } }
    }

}
