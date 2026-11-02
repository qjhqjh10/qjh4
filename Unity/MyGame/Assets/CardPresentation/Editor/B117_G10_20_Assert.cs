// B117_G10_20_Assert.cs — 执行代理 `B117-G10-20`（2026-11-02）· 战斗域第 20 部分
//                            「战斗内设置 · 投降 · 退出 · 语言」
//
// ============================ 它盯的是什么（三条，判据全在下面头）================================
//  A. **点面板外的压暗（原版 `Dark Shade`）= 关设置窗**
//     判据：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BattleSettingsWindow.cs:67-69`（`Awake` 里
//     `backgroundCloseButton.onClick.AddListener(CloseBackgroundButton)`）→ `:126-129` `CloseBackgroundButton()`
//     → `:131-137` `CloseWindow()`。窗口字段 `backgroundCloseButton = 4927`
//     （`assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4034.json`），
//     4927 那颗 `BackgroundCloseButton` 挂在 GO `Dark Shade` 上（`GameObject/Dark Shade.json` 组件表）。
//     ⚠️ 序列化 `m_PersistentCalls` 是**空**的 —— 监听是**运行期**加的，⛔ 不能拿它当「没有监听」的证据。
//  B. **ESC**：语言列表开着 ⇒ **只收列表、窗不关**；列表没开 ⇒ **关窗**
//     判据：`out_cpp2il_dev_ilspy/WindowsManager.cs:995-1006`
//     （`if (GetKeyDown(Escape) && currentWindow && currentWindow.IsOpen()) currentWindow.ESCPressed();`）
//     → `BattleSettingsWindow.ESCPressed()` `:139-151` → `base.ESCPressed()` → `GameWindow.cs:318-330`
//     `if (closeOnESC) Close()`；该窗 **`closeOnESC = 1`**（`MonoBehaviour_4034.json`）。
//     列表那一档另有一条判据：`TMP_Dropdown.OnCancel → Hide()`。
//  C. **每次打开都回填三根音量滑块 + 语言那一行**
//     判据：`BattleSettingsWindow.Open()` `:239-249`（`musicSlider.value = GameStaticData.MusicVolume` …
//     `languageSelector.Initialize()` → `ResetLanguagesDropdown` 把下拉 `value` 对回当前语言）。
//     我们这边面板 `Create` 一次、跨局复用（`BattleDriver.BuildHud` 被 `_hudBuilt` 闩住）⇒ 没有这一步就会
//     显示**建面板那一刻**的旧值。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）================================
//  🧨 A2 / A3 成对：A2 = 列表开着时点压暗**只收列表、窗不关**；A3 = 列表关着时点它**窗关**。
//     「无条件关窗」⇒ A2 红；「什么都不做」⇒ A3 红；「无条件不关」⇒ A3 红。两条一起才判得出对错。
//  🧨 B1 / B2 成对：B1 = 列表关着时 ESC **关窗**；B2 = 列表开着时 ESC **只收列表**。
//     `EscPressed()` 写成「无条件关窗」⇒ B2 红；写成「列表没开就不吃」⇒ B1 红。
//  🧨 C2 / C3 / C4 成对：C2 先把滑块摆到一个**明显不等于当前音量**的值（前提：确实摆进去了）；
//     C3 打开后必须回到**当前音量**（回填生效）；C4 音量本身**没被回填改动**（`fire: false`）。
//     「`Show()` 什么都不做」⇒ C3 红；「回填用 `fire: true`」⇒ C4 红。
//
// ============================ 怎么接（⚠️ 要主对话接 · 本代理不许改宿主）============================
//  在 `Editor/BattleScene.cs` 的方法尾**聚合行、合计那一条之前**（同一物理行追加，⛔ 行数零变化）：
//      B117_G10_20_Assert.Run(); pass += B117_G10_20_Assert.Pass; fail += B117_G10_20_Assert.Fail;
//  `Run()` **零前提**：不读 `BattleDriver`、不读场景、不读存档；自己建相机/面板，`finally` 里逐样还原并销毁。
//  ⚠️ 它会**建一块 `PopUps` 子画布**（`SettingsPanel.Create` 自己建的，按名字幂等）—— 与战斗 HUD 用的是同一块，
//     所以**不销毁**它（销毁会把真面板一起带走）。
//
// ============================ 与既有断言的一处冲突（交调度台处理）================================
//  `Editor/BattleScene.cs:20224-20228` 那条 ★ A921 判别式写的是「列表没开时 ESC 本件不吃（返回 false、窗照旧开着）」
//  —— 那是**按 2026-10-20 的旧口径**写的，与本文件 B1 的判据相反。替换文本见
//  `资料/完全复刻/判据/B117-G10-20_战斗内设置投降退出.md` §三。本代理**无权重写那份宿主**。

using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B117-G10-20`：战斗内设置的「点外关窗 / ESC 关窗 / 打开回填」三条。</summary>
public static class B117_G10_20_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1020 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1020 ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F4") + "）");
    }

    /// <summary>三条一起跑。零前提：自己备相机与面板，收尾原样还原。</summary>
    public static void Run()
    {
        Camera camWas = LayoutSpace.Cam;
        GameObject camGo = null;
        SettingsPanel sp = null;
        bool? escWas = SettingsPanel.EscapePressedForTest;
        bool persistWas = Loc.PersistOverride;
        AvailableLanguages langWas = Loc.Current;
        try
        {
            SettingsPanel.EscapePressedForTest = null;      // 生产恒 null（本方法内按需设/清）
            Loc.PersistOverride = true;                     // ⛔ 自检不许改玩家的真语档

            // ---- 前提：uGUI 层要一块相机（`HudCanvas.Ensure` 没有相机就**不建**，见它的 doc）----
            if (LayoutSpace.Cam == null)
            {
                var found = Object.FindObjectOfType<Camera>();
                if (found != null) LayoutSpace.Cam = found;
                else
                {
                    camGo = new GameObject("B117_G10_20_Cam");
                    var c = camGo.AddComponent<Camera>();
                    c.orthographic = true;
                    LayoutSpace.Cam = c;
                }
            }
            Check(LayoutSpace.Cam != null, "（前提）`LayoutSpace.Cam` 有着落（否则 `HudCanvas` 建不出来，下面全白测）");
            if (LayoutSpace.Cam == null) return;

            sp = SettingsPanel.Create(null, null);
            Check(sp != null, "（前提）`SettingsPanel.Create` 建出面板（建不出来 = 下面全白测；看上面那几行 LogError）");
            if (sp == null) return;
            Check(sp.ClickCountForTest("Dark Shade") == 0,
                  "（前提）压暗那颗可点件**挂上了** uGUI 点击（`-1` = 没挂上 ⇒ 下面 A 段等于没验）");

            // ═══════════════════════════════════════════════════════════════════════════
            //  A 段：点面板外的压暗 —— 列表开着只收列表；列表关着则关窗
            // ═══════════════════════════════════════════════════════════════════════════
            sp.Show();
            Check(sp.Visible, "（A 前提）面板开出来了");
            sp.ShowLangList();
            Check(sp.LangListOpen, "（A 前提）语言下拉列表开出来了");

            sp.ClickForTest("Dark Shade");
            Check(!sp.LangListOpen && sp.Visible,
                  "★ A2：列表开着时点压暗 ⇒ **只收列表、窗不关**"
                + "（原版那颗透明 `Blocker` 在 `Dark Shade` 之上，`Button.onClick → Hide`）"
                + " —— 写成「无条件关窗」这条就红");

            sp.ClickForTest("Dark Shade");
            Check(!sp.Visible,
                  "★ A3：列表**关着**时点压暗 ⇒ **关设置窗**"
                + "（原版 `BattleSettingsWindow.Awake` 把 `CloseBackgroundButton` 挂在"
                + " `backgroundCloseButton.onClick` → `CloseWindow()`；序列化 `m_PersistentCalls` 空不算反证）"
                + " —— 写成「点了什么也不做」这条就红");

            // ═══════════════════════════════════════════════════════════════════════════
            //  B 段：ESC —— 列表关着 ⇒ 关窗；列表开着 ⇒ 只收列表
            // ═══════════════════════════════════════════════════════════════════════════
            sp.Show();
            Check(sp.Visible && !sp.LangListOpen, "（B 前提）面板开着、列表关着");
            SettingsPanel.EscapePressedForTest = true;      // 批处理里没有键盘设备 ⇒ 把这一帧钉死
            sp.PointerFrame(Vector3.zero, false);           // 走**生产那条**每帧入口（驱动层就是这么调的）
            SettingsPanel.EscapePressedForTest = null;
            Check(!sp.Visible,
                  "★ B1：列表**没开**时 ESC ⇒ **关设置窗**"
                + "（原版 `WindowsManager.Update` → `BattleSettingsWindow.ESCPressed` → `base.ESCPressed`"
                + " → `GameWindow.Close`；该窗 `closeOnESC = 1`）"
                + " —— 写成「列表没开就不吃」这条就红");

            sp.Show();
            sp.ShowLangList();
            Check(sp.Visible && sp.LangListOpen, "（B 前提）面板开着、列表也开着");
            SettingsPanel.EscapePressedForTest = true;
            sp.PointerFrame(Vector3.zero, false);
            SettingsPanel.EscapePressedForTest = null;
            Check(!sp.LangListOpen && sp.Visible,
                  "★ B2：列表**开着**时 ESC ⇒ **只收列表、窗不关**"
                + "（原版 `TMP_Dropdown.OnCancel → Hide()`，`Cancel` 先被下拉吃掉）"
                + " —— 写成「无条件关窗」这条就红");

            // ═══════════════════════════════════════════════════════════════════════════
            //  C 段：每次打开回填三根音量滑块（原版 `Open()` 那三行 `slider.value = …`）
            // ═══════════════════════════════════════════════════════════════════════════
            Check(sp.SliderCount == 3, "（C 前提）面板上有三根音量滑块");
            if (sp.SliderCount == 3 && sp.SliderAt(0) != null)
            {
                float audioBefore = WarpforgeAudio.Music;
                // 摆一个**明显不等于当前音量**的值（万一本机音乐刚好就是 0.123，就换 0.877）
                float bogus = Mathf.Abs(WarpforgeAudio.Music - 0.123f) > 0.01f ? 0.123f : 0.877f;
                sp.SliderAt(0).SetValue(bogus, false);      // `fire:false` —— 自检不改总线/存档
                CheckNear(sp.SliderAt(0).ValueForTest, bogus, 1e-3f,
                          "（C 前提）滑块的值**确实被摆成了** `" + bogus.ToString("F3") + "`"
                        + "（没摆动 ⇒ 下面那条 C3 等于没验）");

                sp.Hide();
                sp.Show();
                CheckNear(sp.SliderAt(0).ValueForTest, WarpforgeAudio.Music, 1e-3f,
                          "★ C3：**打开面板 ⇒ 音乐滑块回填成当前音量**"
                        + "（原版 `BattleSettingsWindow.Open`：`musicSlider.value = GameStaticData.MusicVolume`）"
                        + " —— `Show()` 里去掉那句回填这条就红（滑块停在 " + bogus.ToString("F3") + "）");
                CheckNear(WarpforgeAudio.Music, audioBefore, 1e-4f,
                          "★ C4：回填**不回写**音量（`fire: false`）—— 总线上的音乐量一个数都没动"
                        + " —— 回填写成 `SetValue(v, fire: true)` 这条就红");
            }

            // ═══════════════════════════════════════════════════════════════════════════
            //  D 段：语言那一行 —— 打开时对回**当前语言**
            //  （原版 `Open()` 末尾 `languageSelector.Initialize()` → `ResetLanguagesDropdown`）
            // ═══════════════════════════════════════════════════════════════════════════
            Loc.SetLanguage(AvailableLanguages.Chinese);    // 真广播（`PersistOverride` 已开 ⇒ 不落盘）
            sp.Hide();
            sp.Show();
            Check(sp.LanguageCaptionText == Loc.LanguageName(Loc.Current),
                  "★ D1：打开面板后「当前语言名」= 当前语言（" + Loc.LanguageName(Loc.Current) + "）"
                + " —— 原版 `Open()` 的 `languageSelector.Initialize()` 干的就是这一下");
        }
        catch (System.Exception e)
        {
            Check(false, "🧨 本件抛异常（下面那些断言**没跑**，别当它们绿了）：" + e);
        }
        finally
        {
            SettingsPanel.EscapePressedForTest = escWas;
            Loc.SetLanguage(langWas);          // 还原语档（会再广播一次，无副作用）
            Loc.PersistOverride = persistWas;
            if (sp != null) Object.DestroyImmediate(sp.gameObject);
            LayoutSpace.Cam = camWas;
            if (camGo != null) Object.DestroyImmediate(camGo);
        }
        Debug.Log($"[B117G1020] 合计：{_pass} 通过 / {_fail} 失败");
    }
}
