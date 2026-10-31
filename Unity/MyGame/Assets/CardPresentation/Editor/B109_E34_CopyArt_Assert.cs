// B109_E34_CopyArt_Assert.cs — 执行代理 `B109_E34`（2026-10-31 · 第一百零九会话）
//
// 修 `B109_D1` 诊断 §8 的「**附带真 β**」：`Copy@3x` 在解包资源里**存在**
// （`d:/2/新解包资源/assets_full/bundle_menus_assets_all/Sprite/Copy@3x.json`，`m_Rect` 48×48
//  == `m_RD.textureRect` 48×48、`textureRectOffset` 全 0），但**没进 `Resources/`**
//  ⇒ 三处宿主都打了 `[Settings] 图缺了：Copy@3x`（`host_battlescene.log:257700 / :277668` ·
//  `host_shellscene.log:82970`），且 `MenuDraw.Rect` 头一句 `tex == null ⇒ return null`
//  （账号页那颗 `External Link Icon` 只能靠 `SettingsWindow.cs:2552` 的兜底空节点建出来）。
//
// 本件 = 这条 β 的**独立复核断言**（导图 ≠ 画出来）。
//
// ============================ 期望值从哪来（⛔ 灭自证）============================
//   | 项                 | 期望值                                    | 出处 |
//   |--------------------|-------------------------------------------|------|
//   | 原版 sprite 名      | `Copy@3x`                                 | `assets_full/bundle_menus_assets_all/Sprite/Copy@3x.json` 的 `m_Name` |
//   | 贴图尺寸            | 48×48                                     | 同上 `m_Rect.width/height`（且 == `m_RD.textureRect`） |
//   | 染色                | (1,1,1,1)                                 | `menu_dump.py bundle_menus_assets_all "Player Id" --depth 3 --relative` 的 `External Link Icon` 行（`Simple … (1,1,1,1) preserveAspect`） |
//   | 挂载点              | `Account Tab > Player Id > External Link Icon` | 同上 dump（`Player Id` 面板 `act=INACT`、子件 `ANC✗`） |
//
//   ⛔ **不从 `SettingsWindow.AcPidIconArt` 反算** —— 那样把常量改成一个「存在、但画错」的图名时本件照样绿
//      （= 拿我们自己的实现证明我们自己）。图名 / 尺寸 / 染色全部是**字面量**。
//
//   复跑判据（只读）：
//     python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Player Id" --depth 3 --relative
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B109_E34_CopyArt_Assert.Run(); pass += B109_E34_CopyArt_Assert.Pass; fail += B109_E34_CopyArt_Assert.Fail;`
//   接在某个宿主的「合计」聚合行**之前、同一物理行追加**（⇒ 行数零变化）。
//   🔴 追加前先看清那一行第一个 `//` 在哪：落在它**之后** = 代码进注释，**这条断言一次都不会跑**
//      （`typecheck`/账闸全绿、退出码 0，而一条都没执行）。
//
// ============================ 边界 ============================
//   · 只读（结点树 / `ImageQuad.Texture` / `ImageQuad.Tint` / `MissingArtForTest`），**不写任何游戏状态**；
//   · 建出来的窗在 `finally` 里 `DestroyImmediate` + 还原 `WindowsManager` 的复用缓存；
//   · ⚠️ `CardArt.Get` 会**缓存 null**（与 `CardArt` 的既有口径一致）⇒ 图导进来之后**必须先重开
//     编辑器/宿主**再跑本件，否则读到的还是上一次的 null（`Assets/Resources` 走磁盘、不在运行时热更）。
using System;
using CardPresentation;
using UnityEngine;

public static class B109_E34_CopyArt_Assert
{
    const string P = "[B109_E34] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    /// <summary>唯一入口。段级 catch：抛异常**算红**（⛔ 不当跳过）。</summary>
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            RunAccountCopyIcon();
        }
        catch (Exception e)
        {
            Ok(false, "段异常（算红，⛔ 不是跳过）: " + e);
        }
        finally
        {
            Debug.Log(P + $"=== B109_E34 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    static void RunAccountCopyIcon()
    {
        Debug.Log(P + "--- `Copy@3x`：账号页 `Player Id > External Link Icon` 的贴图 ---");

        // ---- ① 图本身在 `Resources/` 里（= 这条 β 的正面判据；图缺时 `MenuUi` 返 null）----
        //   先读这一条：它红了就没必要再读窗（窗那半边只会是同一个 null 的二次表现）。
        var tex = CardArt.MenuUi("Copy@3x");
        if (!Ok(tex != null,
                "①a ★★ `CardArt.MenuUi(\"Copy@3x\")` 取到图 —— 以前是 null（图没进 `Resources/` ⇒ 三处宿主都打 `[Settings] 图缺了：Copy@3x`）"))
            return;
        Ok(tex.name == "Copy@3x",
           "①b ★ 贴图资产名 = `Copy@3x`（实得 `" + tex.name + "`）—— 落盘名照原版切片名（本表只把空格换下划线，`@` 原样）");
        Ok(tex.width == 48 && tex.height == 48,
           "①c ★ 贴图尺寸 = 48×48（实得 " + tex.width + "×" + tex.height + "）—— 判据 = 原版 sprite 的 `m_Rect`");

        // ---- ② 账号页那颗节点（生产那一行 = `SettingsWindow.cs:2545` 的 `Rect(...)`）----
        SettingsWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "②-前提：拿得到 `WindowsManager`")) return;

            win = SettingsWindow.Create(wm);
            if (!Ok(win != null, "②-前提：`SettingsWindow.Create` 建出窗")) return;
            // ⚠️ `Create` **不建树** —— `Build()` 只在 `Open()` 第一句里跑。
            win.Open();

            var account = Deep(win.transform, "Account Tab");
            if (!Ok(account != null, "②a 前提：`Account Tab` 在（账号页那棵树建出来了）")) return;
            var pid = Child(account, "Player Id");
            if (!Ok(pid != null, "②b 前提：`Account Tab > Player Id` 在")) return;

            // 原版 `menu_dump` 的这一行是 `act=INACT`（出厂 `m_IsActive=0`、`AccountTab.Refresh` 从不激活它）
            Ok(!pid.gameObject.activeSelf,
               "②c ★ `Player Id` 出厂 inactive —— 与原版 `m_IsActive=0` 一致（⛔ 别为了「看得见」把它打开）");

            var icon = Child(pid, "External Link Icon");
            if (!Ok(icon != null, "②d ★ 账号页 `Player Id > External Link Icon` 建出来了")) return;

            // 🔴 灭自证：`Rect()` 在 `tex == null` 时**根本不建 `ImageQuad`**（只留 `SettingsWindow.cs:2552`
            //    兜底的空节点）⇒「节点在」本身不能证明图接上了，必须看组件与贴图。
            var q = icon.GetComponent<ImageQuad>();
            if (!Ok(q != null,
                    "②e ★ `External Link Icon` 上挂着 `ImageQuad` —— 裸节点 = 兜底那一支（图当时缺失），图不在"))
                return;
            if (!Ok(q.Texture != null, "②f ★ `ImageQuad.Texture != null`（真的画了图，不是空 quad）")) return;

            string got = q.Texture.name;
            Ok(got == "Copy@3x",
               "②g ★★ 贴图 = `Copy@3x`（实得 `" + got + "`）—— 期望值是**原版 prefab 的 `m_Sprite.m_Name`**，⛔ 不是我们代码里的常量");
            Ok(q.Texture.width == 48 && q.Texture.height == 48,
               "②h ★ 贴图尺寸 = 48×48（实得 " + q.Texture.width + "×" + q.Texture.height + "）—— 判据 = 原版 sprite 的 `m_Rect`");
            Ok(q.Tint == Color.white,
               "②i ★ 染色 = (1,1,1,1)（实得 " + q.Tint + "）—— 判据 = `menu_dump` 那行的 `(1,1,1,1)`（本颗不是半透明底板）");

            // ---- ③ 缺图表里不该再有它（= 三处宿主那条 warning 的正面判据）----
            var miss = win.MissingArtForTest;
            Ok(miss != null, "③-前提：拿得到 `MissingArtForTest`（`MissingArt` 的只读出口）");
            if (miss != null)
                Ok(!miss.Contains("Copy@3x"),
                   "③a ★★ `MissingArt` 里没有 `Copy@3x` —— `[Settings] 图缺了：Copy@3x` 从此不该再出现");
        }
        finally
        {
            if (win != null) { try { win.Close(); } catch { } try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            try { WindowsManager.ClearReuseCacheForTest(); } catch { }
        }
    }

    // ============================================================ 小工具
    static Transform Child(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == name) return c;
        }
        return null;
    }

    static Transform Deep(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = Deep(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }
}
