// B117_WPOPUP_Assert.cs — 执行代理 `B117-WPOPUP`（菜单 / 收藏详情窗的卡面档位）的断言宿主
//
// ============================ 判据（一手，YAML 亲解）============================
//   `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`
//   → `Card Displayer Menu For Menu / Card Display / Cards / CardUI … CardUI (4)`（5 张，逐张同值）：
//     单位卡四行 `NameTextUnit` / `DescTextUnit` / `MinionOrWarlord Container/RaceText` / `ArmyTextUnit`
//       全是 **m_fontSize = m_fontSizeMax = 16**（对照标准档 19/19 · 23.55/24 · 18/18 · 15/15）
//     无描述两颗 `NameTextUnit No description` = **19/19** · `Army No Desciption` = **15/15**（不跟着降）
//     战术卡三行 = 标准档同值（NameTextTactic 21.5/**24** · DescTextTactic 19/19 · ArmyTextTactc 14/**15**）
//     阴影 = `CardDisplayWindow__InitializeCardForDisplay.c:46-51` 无条件 `(0,0,0,0.8)`
//   战斗放大窗那一棵（**不同树**）= `16 / max 19`（`资料/普查产出_1015战斗/C1_逐格对账…` 表三·1）
//
// ============================ 为什么这不是「我们自己的常量」============================
//   期望值一律写成**原版 prefab 的 pt 字面量**（16 / 19 / 24 / 0.8）。
//   我们的实现单位（band 1.595、em 0.16/3.3313）通过**标准档那一格**折算回 pt 再比：
//     pt = 本档 bandMax ÷ (标准档 bandMax ÷ 标准档 pt)
//   ⇒ 若把菜单档改回标准档，得到 19（≠16）⇒ 红；若把标准档一并改小，折算会跟着变 ⇒ 也红（不是自证）。
//
// ============================ 断言一览（21 条 Check 调用）============================
//   接线 1 · 前提 4 · 菜单档 7 · 战术不动 2 · 灭自证 2 · 战斗放大窗回归 5 = 21
//   （正文编号 ①–⑯ 只覆盖「被测项」；接线 + 4 条前提不计编号）
//
// 用法（⚠️ 要主对话跑 —— 子代理不许 -executeMethod）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod CollectionScene.Run -logFile -
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B117_WPOPUP_Assert
{
    const string P = "B117WPOPUP ";

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5") + "；实得 " + got.ToString("F5") + "）");
    }

    // ---- 原版 prefab 的字面量（⛔ 别从实现常量反推）----
    const float PtUnitRow      = 16f;    // 菜单详情窗：NameTextUnit / DescTextUnit / RaceText / ArmyTextUnit 的 m_fontSizeMax
    const float PtStdName      = 19f;    // 标准档 CardUI：NameTextUnit 的 m_fontSizeMax
    const float PtStdDesc      = 24f;    // 标准档 CardUI：DescTextUnit 的 m_fontSizeMax
    const float PtZoomNameMax  = 19f;    // 战斗放大窗：NameTextUnit 的 m_fontSizeMax（16/max 19）
    const float AlphaMenu      = 0.8f;   // InitializeCardForDisplay 无条件写的 (0,0,0,0.8)
    const float AlphaStd       = 0.604f; // 标准档材质默认

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GameObject root = null;
        try
        {
            // ---- 0. 接线：宿主真的传了这一档 ----
            Check(CardDetailPopup.PopupCardUse == CardUse.MenuDetail,
                  "接线：`CardDetailPopup` 建卡走 `CardUse.MenuDetail`（实得 " + CardDetailPopup.PopupCardUse + "）");

            // ---- 1. 前提（硬检查，⛔ 不用 `if (前提) Check(...)` 那种会静默跳过的写法）----
            Check(TmpFont.Available, "前提：字体资产取得到（取不到时 title/keywords 根本不建，下面全空转）");
            if (!TmpFont.Available) return;

            var pool = CardDatabase.Load();
            CardDef def = null;
            for (int i = 0; i < pool.Count; i++) if (pool[i].Name == "Uriel Ventris") { def = pool[i]; break; }
            Check(def != null, "前提：卡池里有 `Uriel Ventris`（B117_F14 用的同一张真卡）");
            if (def == null) return;

            root = new GameObject("b117wpopup_probe");
            var disp = new GameObject("Card Display");              // 原版那层祖先（`IsBigCardView` 的第二条判据靠它）
            disp.transform.SetParent(root.transform, false);

            var cd = BattleDriver.ToCardData(def, def.Faction);
            var std  = CardView.Create(disp.transform, cd, "CardUI");                              // 对照：标准档
            var menu = CardView.Create(disp.transform, cd, "CardUI", use: CardUse.MenuDetail);      // 被测
            var zoom = CardView.Create(disp.transform, cd, "CardUI", use: CardUse.ZoomWindow);      // 回归：战斗放大窗
            Check(std != null && menu != null && zoom != null, "前提：三张探针卡都建出来了");
            if (std == null || menu == null || zoom == null) return;
            Check(menu.Use == CardUse.MenuDetail,
                  "前提：MenuDetail 探针没被 `IsBoosterCardView` 改写（实得 " + menu.Use + "）");

            float stdNameBand = CardView.BandMaxForTest("NameUnit");   // 标准档 19pt 折我们单位（= 1.595）
            float stdDescBand = CardView.BandMaxForTest("DescUnit");   // 标准档 24pt 折我们单位（= 1.560）

            // ---- 2. 菜单详情档：六个值（期望值 = 原版 prefab 字面量）----
            CheckNear(menu.NameFontSize / std.NameFontSize, PtUnitRow / PtStdName, 1e-4f,
                      "① 详情窗卡名**名义**字号 = 标准档 × 16/19（旧实现 = 1.0 ⇒ 红）");
            CheckNear(menu.NameBandMaxForTest / (stdNameBand / PtStdName), PtUnitRow, 1e-3f,
                      "② 详情窗卡名 **autosize 上界 = 16 pt**（原版 `NameTextUnit` 16/16；旧实现 19 ⇒ 红）");
            CheckNear(menu.DescBandMaxForTest / (stdDescBand / PtStdDesc), PtUnitRow, 1e-3f,
                      "③ 详情窗效果 **autosize 上界 = 16 pt**（原版 `DescTextUnit` 16/16；旧 24 ⇒ 红）");
            CheckNear(menu.ArmyFontSizeForTest(true), 1.6f, 1e-4f,
                      "④ 详情窗阵营行字号 = 16 pt（原版 `ArmyTextUnit` 16/16；旧 15 ⇒ 红）");
            CheckNear(menu.RaceFontSizeForTest(true), 1.6f, 1e-4f,
                      "⑤ 详情窗兵种行字号 = 16 pt（原版 `RaceText` 16/16；旧 18 ⇒ 红）");
            Check(menu.CardShadowLayerBuilt,
                  "⑥ 前提：MenuDetail 探针的阴影层建出来了（shader/SDF 取不到时这条红，红过才谈下面的 α）");
            CheckNear(menu.CardShadowAlphaForTest, AlphaMenu, 1e-3f,
                      "⑦ 详情窗阴影 alpha = 0.8（旧 0.604 ⇒ 红）");

            // ---- 3. 战术卡三行**不跟着降**（原版与标准档同值）----
            CheckNear(menu.ArmyFontSizeForTest(false), 1.5f, 1e-4f,
                      "⑧ 战术 `ArmyTextTactc` 仍是 max 15（原版 14/15 ⇒ 不动）");
            CheckNear(menu.RaceFontSizeForTest(false), 1.8f, 1e-4f,
                      "⑨ 战术 `RaceText` 仍是 18（原版 18/18 ⇒ 不动）");

            // ---- 4. 灭自证：同一组读口在标准档上必须给出**旧值** ----
            CheckNear(std.NameBandMaxForTest / (stdNameBand / PtStdName), PtStdName, 1e-3f,
                      "⑩ 灭自证 · 标准档卡名上界 = 19（证明读口能分档，不是恒返回 16）");
            CheckNear(std.CardShadowAlphaForTest, AlphaStd, 1e-3f,
                      "⑪ 灭自证 · 标准档阴影 = 0.604（不是恒返回 0.8）");

            // ---- 5. 回归：战斗放大窗那一条没被改坏 ----
            CheckNear(zoom.NameFontSize / std.NameFontSize, PtUnitRow / PtStdName, 1e-4f,
                      "⑫ 战斗放大窗卡名名义字号仍是 16/19 比");
            CheckNear(zoom.NameBandMaxForTest / (stdNameBand / PtStdName), PtZoomNameMax, 1e-3f,
                      "⑬ 战斗放大窗卡名上界仍是 **19**（那一棵原版 max=19，⛔ 别被菜单档带到 16）");
            CheckNear(zoom.CardShadowAlphaForTest, AlphaMenu, 1e-3f,
                      "⑭ 战斗放大窗阴影仍 0.8");
            CheckNear(zoom.ArmyFontSizeForTest(true), 1.5f, 1e-4f,
                      "⑮ 战斗放大窗阵营行仍 15 pt（放大窗不降）");
            CheckNear(zoom.DescBandMaxForTest, stdDescBand, 1e-4f,
                      "⑯ 战斗放大窗效果上界仍 = 标准档（放大窗不动效果行）");
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            Debug.Log(P + "合计：通过 " + _pass + " · 失败 " + _fail);
        }
    }
}
