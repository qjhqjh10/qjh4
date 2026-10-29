// B97W14_Assert.cs — 执行代理 `B97_W14`（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `未分类/其他:584`（原版 `SimpleRotation.Update()`）在**设置窗**的那一份：
//   `Main Menu Settings Window / Login Window / ErrorMensajeContainer / Animated Loading Image / Cog`
//   那颗齿轮 —— 图早就建了（`Shell/SettingsWindow.cs` 的 `BuildLoginWindow` 里那一行），**缺的是驱动**。
//   本轮接上：`SettingsWindow.AdvanceCog`（`SimpleRotation.Step` + `localRotation`）· 真 Play 由 `Update()` 调 ·
//   自检走 `PumpCogForTest`。
//
// 判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_R5_零散剩余施工单.md` 的 `未分类:584` 那一行
//   ＋ 本轮报告 `资料/完全复刻/报告/B97_W14.md`。要点（都是**逐字段实读**）：
//   · 原版 prefab `bundle_menus_assets_all` 的 `Main Menu Settings Window` 里那颗
//     `GameObject/Cog_-3685809886447435866.json`（父链 Cog → `Animated Loading Image` → `ErrorMensajeContainer`
//     → `Login Window` → … → `Menu Area`）；
//   · 它的 `SimpleRotation` 实例 `MonoBehaviour_-2329547480181669978.json`：**`speed = {0, 0, -20}`**；
//   · 方法体 `d:/2/tools/decomp_full/SimpleRotation__Update.c` **全文 26 行**：
//     `euler = transform.eulerAngles` → 逐轴 `euler[i] += speed[i] * Time.deltaTime` → `set_eulerAngles`
//     ⇒ **按帧连续**（不是按周期定步长）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   宿主里加一行：
//       B97W14Assert.Run();
//   ＋ 两句计数转发（照 `B94B18Assert` 的先例）：
//       _sink.Pass += B97W14Assert.Pass; _sink.Fail += B97W14Assert.Fail;
//   · **推荐宿主 = `Editor/SettingsScene.cs`（`SettingsScene.Run`）** —— 它本来就跑设置窗那一族，
//     `CheckSink` 的 `Near` 口径是 `Compact3`（本文件已按它设）。
//   · 本件**不建持久资产**、不碰 `PlayerPrefs`、不跑 Unity 批处理；建的 `GameObject` 全在 `finally` 里
//     `DestroyImmediate`。夹具走 `SettingsWindow.BuildCogFixtureForTest(...)` —— 它**不调 `Create`**
//     ⇒ 不写静态 `Instance`、不建 `NetRuntime`（跨宿主副作用一个都不留）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `SettingsWindow.CogDegPerSec` 从 `-20f` 改成 **`0f`** ⇒ 推进 1 秒后仍是 0°（期望 340）⇒ A1 红；
//           改成 **`+20f`** ⇒ 得 20°（期望 340、有符号量 +20）⇒ A1 **和** A2 **都**红（两个方向都红）。
//   · A 段：把 `SimpleRotation.Step` 里的 `speed * dt` 写成 `speed`（漏了按帧）⇒ 1 秒得 -20（=340）**仍绿**，
//           但 **B1 的 4×0.25 秒会得 340 一样绿**……⇒ 所以 B 段另配一条「dt=2 秒 ⇒ 320°」：
//           漏 `dt` 时 2 秒得 340（= 1 秒的值）⇒ **B2 红**。
//   · C 段（**灭「只记 float 不写 transform」**）：把 `AdvanceCog` 里那两行 `localRotation` 删掉 ⇒
//           节点角恒 0°（期望 340）⇒ C1/C2 红 —— 只留累加器是过不了的。
//   · D 段：把 `Build()` 里那句 `_cogAngle = 0f` 删掉 ⇒ 第二棵夹具的出生角继承上一棵的值 ⇒ D1 红。
//   · 期望值全部是**字面量**（`-20` / `340` / `320` / `350`），来自**原版 prefab 与反编译**，
//     ⛔ 一个都不从被测实现里读（不读 `SettingsWindow.CogDegPerSec`、不反算 `Step`）。
//   · ⛔ 没有 `if (x != null) Check(...)` 这种「前提不成立就静默跳过」的写法：前提也是硬 `CheckTrue`。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_W14`：设置窗那颗 `Cog` 齿轮的驱动（原版 `SimpleRotation`，`speed.z = -20`）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W14Assert
{
    static readonly CheckSink _sink = new CheckSink("[W14] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Nr(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>原版 `SimpleRotation.speed.z`（判据 = `MonoBehaviour_-2329547480181669978.json` 的 `speed.z`，**实读 -20**）。
    /// ⛔ 刻意写成字面量：⛔ 别改成读 `SettingsWindow.CogDegPerSec`（那是被测实现 = 自证）。</summary>
    const float ExpectDegPerSec = -20f;

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "W14 设置窗那颗 `Cog` 齿轮（原版 `SimpleRotation.Update()`，speed.z = -20）");

        SectionA_一秒二十度();
        SectionB_多帧与跨零();
        SectionC_真写进节点();
        SectionD_两棵夹具互不传染();
        SectionE_关着就冻住();
    }

    /// <summary>建一棵夹具并跑完「前提」那几条；建不出来 ⇒ 返 null（**前提已红**，调用方直接 return）。
    /// ⚠️ 建完时 `ErrorMensajeContainer` 是**关着**的（= 原版 `BuildLoginWindow` 末尾那句 `SetActive(false)`）
    /// —— 要转齿轮得先 <see cref="ShowErrorBox"/>。</summary>
    static SettingsWindow BuildWithPremises(string tag)
    {
        var w = SettingsWindow.BuildCogFixtureForTest(null);
        Tr(w != null, "前提 " + tag + "-0a：`SettingsWindow.BuildCogFixtureForTest` 建出了窗（⛔ 不走 `Create` ⇒ 不留 `Instance` 副作用）");
        if (w == null) return null;
        Tr(w.CogNodeForTest != null,
           "前提 " + tag + "-0b：`Cog` 那颗被**真的建出来**了（`CogNodeForTest != null`）—— "
         + "取不到（贴图 `40K_icon_searching_cog` 缺失）这条就红，⛔ 不静默跳过");
        if (w.CogNodeForTest == null) return null;
        C(w.CogNodeForTest.name, "Cog", "前提 " + tag + "-0c：节点名 == `Cog`（原版那一颗）");
        var parent = w.CogNodeForTest.parent;
        Tr(parent != null, "前提 " + tag + "-0d：`Cog` 有父节点");
        if (parent != null)
            C(parent.name, "Animated Loading Image",
              "前提 " + tag + "-0e：父节点 == `Animated Loading Image`（原版父链 Cog → Animated Loading Image）");
        if (parent != null)
        {
            var grand = parent.parent;
            Tr(grand != null, "前提 " + tag + "-0f：`Animated Loading Image` 有父节点");
            if (grand != null)
                C(grand.name, "ErrorMensajeContainer",
                  "前提 " + tag + "-0g：祖父节点 == `ErrorMensajeContainer`（原版那一整条父链）");
        }
        C(w.CogAngleForTest, 0f, "前提 " + tag + "-0h：出生角 = 0°（新一棵树的 `Cog` 旋转是 identity）");
        return w;
    }

    /// <summary>把 `ErrorMensajeContainer` 亮起来（= 原版 `ErrorMensajeContainer.SetActive(1)` 那一刻）——
    /// 齿轮只在框亮着时才转（`AdvanceCog` 那道 `activeInHierarchy` 闸 = 原版 `Update` 不收 inactive 物体的帧）。</summary>
    static void ShowErrorBox(SettingsWindow w, string tag)
    {
        var cog = w != null ? w.CogNodeForTest : null;
        Tr(cog != null, "前提 " + tag + "-1a：节点还在（要拿它往上找 `ErrorMensajeContainer`）");
        if (cog == null) return;
        var load = cog.parent;
        var box = load != null ? load.parent : null;
        Tr(box != null, "前提 " + tag + "-1b：`Cog → Animated Loading Image → ErrorMensajeContainer` 那条父链还在");
        if (box == null) return;
        box.gameObject.SetActive(true);
        Tr(cog.gameObject.activeInHierarchy, "前提 " + tag + "-1c：把框亮起来之后 `Cog` 在活跃层级里（否则下面全在转空气）");
    }

    // ============================================================ A 一秒钟 = −20°
    static void SectionA_一秒二十度()
    {
        MenuCheck.Section(_sink, "A 推进 1 秒 ⇒ −20°（期望值来自原版 prefab 的 speed.z = -20，⛔ 不是读实现）");
        SettingsWindow w = null;
        try
        {
            w = BuildWithPremises("A");
            if (w == null) return;
            ShowErrorBox(w, "A");

            w.PumpCogForTest(1f);
            // `SimpleRotation.Step` 走 `Mathf.Repeat(…, 360)` ⇒ 0 + (−20)×1 = −20 归一到 **340**
            Nr(w.CogAngleForTest, 340f, 0.001f,
               "A1 推进 1 秒 ⇒ 累计角 = 340°（= 0 + (−20)×1，归一后；🧨 转速改成 0 ⇒ 0、改成 +20 ⇒ 20 ⇒ 红）");
            // 有符号变化量：340° 与 0° 的有符号差就是 −20°（这一条把「340 是不是真的等于 −20」钉死）
            Nr(Mathf.DeltaAngle(0f, w.CogAngleForTest), ExpectDegPerSec, 0.001f,
               "A2 **有符号变化量 = −20°/s**（🧨 改成 +20 ⇒ +20 ⇒ 红；改成 0 ⇒ 0 ⇒ 红 —— 两个方向都红）");
        }
        finally { DestroyFixture(w); }
    }

    // ============================================================ B 多帧累计 / 跨零 / 按帧连续
    static void SectionB_多帧与跨零()
    {
        MenuCheck.Section(_sink, "B 多帧累计不回绕、不错号（4×0.25s == 1.0s · 跨 0/360 · dt=2s 真的按 2 秒走）");

        // ---- B1：四小步 == 一大步（按帧连续 ⇒ 线性可加）----
        {
            SettingsWindow w = null;
            try
            {
                w = BuildWithPremises("B1");
                if (w != null)
                {
                    ShowErrorBox(w, "B1");
                    for (int i = 0; i < 4; i++) w.PumpCogForTest(0.25f);
                    Nr(w.CogAngleForTest, 340f, 0.001f,
                       "B1 4 帧 × 0.25 秒 ⇔ 与 A1 的「1 帧 × 1 秒」同值（340°）—— "
                     + "🧨 若按帧数而不是按时间累加（`speed` 漏乘 `dt`），这一条与 A1 会分岔");
                }
            }
            finally { DestroyFixture(w); }
        }

        // ---- B2：dt = 2 秒 ⇒ 真的走 2 秒（= 320°），不是「一帧一步」----
        {
            SettingsWindow w = null;
            try
            {
                w = BuildWithPremises("B2");
                if (w != null)
                {
                    ShowErrorBox(w, "B2");
                    w.PumpCogForTest(2f);
                    Nr(w.CogAngleForTest, 320f, 0.001f,
                       "B2 单帧 dt = 2 秒 ⇒ 320°（= 0 + (−20)×2）—— 🧨 把 `speed * dt` 写成 `speed` ⇒ 得 340 ⇒ 红");
                }
            }
            finally { DestroyFixture(w); }
        }

        // ---- B3：跨 0/360 那一圈（−10° ≡ 350°），且第二次推进接着数、不重置 ----
        {
            SettingsWindow w = null;
            try
            {
                w = BuildWithPremises("B3");
                if (w != null)
                {
                    ShowErrorBox(w, "B3");
                    w.PumpCogForTest(0.5f);
                    Nr(w.CogAngleForTest, 350f, 0.001f,
                       "B3 推进 0.5 秒 ⇒ 350°（0 − 10 = −10，归一后 = 350）—— 🧨 不归一（直接留 −10）⇒ 红");
                    w.PumpCogForTest(0.5f);
                    Nr(w.CogAngleForTest, 340f, 0.001f,
                       "B3b 再推进 0.5 秒 ⇒ 340°（**接着数**、⛔ 不是从 0 重来、也⛔ 不是跳回 350）");
                }
            }
            finally { DestroyFixture(w); }
        }
    }

    // ============================================================ C 真写进节点（灭「只记 float 不写 transform」）
    static void SectionC_真写进节点()
    {
        MenuCheck.Section(_sink, "C 角度**真的写进了** `Cog` 节点的 `localRotation`（⛔ 不是只留一个 float）");
        SettingsWindow w = null;
        try
        {
            w = BuildWithPremises("C");
            if (w == null) return;
            ShowErrorBox(w, "C");

            w.PumpCogForTest(1f);
            var node = w.CogNodeForTest;
            Tr(node != null, "前提 C-0：节点还在");
            if (node == null) return;
            // 父链恒等旋转 ⇒ `localRotation` 与 `eulerAngles` 同值（判据见 `Shell/SettingsWindow.cs` 那一节头 ①）
            Nr(node.localRotation.eulerAngles.z, 340f, 0.01f,
               "C1 节点的 `localRotation.eulerAngles.z` = 340°（🧨 删掉 `AdvanceCog` 里写 `localRotation` 那一行 "
             + "⇒ 节点角恒 0 ⇒ 红）");
            Nr(Mathf.DeltaAngle(0f, node.localRotation.eulerAngles.z), ExpectDegPerSec, 0.01f,
               "C2 节点角的**有符号变化量** = −20°（方向与速率都对着原版 `speed.z = -20`）");
            // 另外两轴必须没被碰（原版 speed.x / speed.y 都是 0）
            Nr(Mathf.DeltaAngle(0f, node.localRotation.eulerAngles.x), 0f, 0.01f,
               "C3 `x` 轴不转（原版 `speed.x = 0`）");
            Nr(Mathf.DeltaAngle(0f, node.localRotation.eulerAngles.y), 0f, 0.01f,
               "C4 `y` 轴不转（原版 `speed.y = 0`）");
        }
        finally { DestroyFixture(w); }
    }

    // ============================================================ D 两棵夹具互不传染
    static void SectionD_两棵夹具互不传染()
    {
        MenuCheck.Section(_sink, "D 新一棵夹具的出生角仍是 0°（`Build` 里清过 `_cogAngle`，⛔ 不继承上一棵树）");
        SettingsWindow first = null, second = null;
        try
        {
            first = BuildWithPremises("D-1");
            if (first == null) return;
            ShowErrorBox(first, "D-1");
            first.PumpCogForTest(1f);
            Nr(first.CogAngleForTest, 340f, 0.001f, "D0 前提：第一棵转到了 340°");

            second = BuildWithPremises("D-2");
            if (second == null) return;
            C(second.CogAngleForTest, 0f,
              "D1 第二棵夹具的出生角 = 0°（🧨 把 `Build()` 里 `_cogAngle = 0f` 删掉 ⇒ 一建出来就是 340 ⇒ 红）");
            // 光看出生角还不够（「关着就冻住」那道闸也会让角度不动）⇒ **亮起来再推 1 秒**，仍是 340 才算真归零
            ShowErrorBox(second, "D-2b");
            second.PumpCogForTest(1f);
            Nr(second.CogAngleForTest, 340f, 0.001f,
               "D1b 亮起来再推 1 秒 ⇒ 340°（🧨 若 `_cogAngle` 从上一棵树继承 340，这里会得 320 ⇒ 红）");
            Nr(first.CogAngleForTest, 340f, 0.001f, "D2 建第二棵、推它，**没有**动到第一棵的角度（仍是 340°）");
            Tr(!ReferenceEquals(first, second), "D3 两棵是两个不同实例（`BuildCogFixtureForTest` 每次新建）");
        }
        finally
        {
            DestroyFixture(second);
            DestroyFixture(first);
        }
    }

    // ============================================================ E 关着就冻住（= 原版 Update 不收 inactive 物体的帧）
    static void SectionE_关着就冻住()
    {
        MenuCheck.Section(_sink, "E `ErrorMensajeContainer` 关着时齿轮**冻住**（原版 `SimpleRotation` 收不到帧）");
        SettingsWindow w = null;
        try
        {
            w = BuildWithPremises("E");
            if (w == null) return;

            // 出厂态：框是关的（`BuildLoginWindow` 末尾那句 `SetActive(false)`）
            Tr(!w.CogNodeForTest.gameObject.activeInHierarchy,
               "E0 前提：建完时 `Cog` **不在**活跃层级里（`ErrorMensajeContainer` 出厂关着）");
            w.PumpCogForTest(1f);
            C(w.CogAngleForTest, 0f,
              "E1 框关着时推进 1 秒 ⇒ **仍是 0°**（🧨 删掉 `AdvanceCog` 里那句 `activeInHierarchy` 闸 ⇒ 得 340 ⇒ 红；"
            + "原版 `SimpleRotation.Update` 在 inactive 物体上根本不被调）");
            Nr(w.CogNodeForTest.localRotation.eulerAngles.z, 0f, 0.01f,
               "E2 而且节点的 `localRotation` 也没被动过（仍是 identity 的 0°）");

            // 亮起来 ⇒ 从 0 开始正常转
            ShowErrorBox(w, "E");
            w.PumpCogForTest(1f);
            Nr(w.CogAngleForTest, 340f, 0.001f,
               "E3 亮起来之后推进 1 秒 ⇒ 340°（= 从冻住的 0° 接着转 —— 原版「重新可见时从原角度继续」）");
        }
        finally { DestroyFixture(w); }
    }

    // ============================================================ 收尾
    static void DestroyFixture(SettingsWindow w)
    {
        if (w == null) return;
        Object.DestroyImmediate(w.gameObject);
    }
}
