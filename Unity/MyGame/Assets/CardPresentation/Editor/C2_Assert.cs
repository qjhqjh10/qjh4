// C2_Assert.cs — 执行代理 `C2`（2026-10-31 · 第一百零九会话 · C 组）落地的两条断言
//
// ============================ 覆盖（逐条对判据）============================
//  · `Run()` 的 A 段 —— **读档之后把 `vsync` / 帧率应用出去**（缺陷：`LocalPlayerDataStore.Apply()` 只灌
//    `GameStaticData` 镜像 ⇒ 重开游戏后 vSync / 帧率仍是平台默认，要开一次设置窗才生效）。
//      判据 = `d:/2/tools/decomp_full/PlayerDataManager__Awake.c:110-116`
//        （ILSpy 同处 `cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:7807-7809`）：
//          LoadPlayerData() → SetTargetFramerate(DEFAULT_TARGET_FRAMERATE)
//          → `SingletonInitializableBehaviour<QualitySettingsManager>.Instance.Initialize()`
//            → `QualitySettingsManager.cs:82-83`：`QualitySettings.vSyncCount = GameStaticData.vsync ? 1 : 0`
//            → `PlayerDataManager.singletonManager.ApplyGraphicsQuality()`
//            → `PlayerDataManager__ApplyGraphicsQuality.c`：FPSLimit 0⇒30 / 1⇒60 / 2⇒−1 → `SetTargetFramerate`
//        ⇒ 期望值一律用**字面量 30 / 60 / −1**（来自原版 `.c`），⛔ 不拿 `TargetFramerateFor` 证明它自己。
//  · `Run()` 的 B 段 —— **`ItemDisplayWindow.Context.Amount`**（缺陷：原版 `quantity` 传不进物品窗）。
//      判据 = `ItemDisplayWindowContext__.ctor.c`（`+0x10 = item` · `+0x18 = amount`）＋
//        `ItemDisplayWindow__Open.c` 的 `ItemDrawer__Draw(itemHolder, context.item, context.amount)`；
//        画不画由抽屉 prefab 决定（`decomp_generic_trial/out/ItemDrawer_object___Draw.c` 只在
//        `ItemDrawerComponents.quantity != null` 时 `set_text(amount.ToString())`）。
//        `ForCardback` 的实参 = **1**（`CardbackCollectionTab__OnItemSelected.c` = `ItemDisplayWindowContext(item, 1)`）。
//
// ============================ 口径 ============================
//  · 前提断言与结论断言**分开**：⛔ 没有 `if (x != null) Check(...)` 这种静默跳过。
//  · 灭自证：同一件事**换一组相反的值**再走一遍（挡「恒返回某个固定值」）。
//  · A 段要跑 `PersistOverride = false` 那一条**真应用**支 ⇒ 备份并还原：
//      `SaveCenter.OverrideRoot`（临时目录，跑完删）· `LocalPlayerDataStore.PersistOverride` ·
//      `SettingsWindow.VSyncSetterOverride`（拦下 `QualitySettings.vSyncCount`，⛔ 不写脏工程资产）·
//      `Application.targetFrameRate` · `GameStaticData` 那一组静态量 · `CardView.HandTextFormat`。
//    底板 = `LocalPlayerDataStore.Collect()`（= 当前状态）⇒ 只改 `vsync` / `fpsLimit`，
//    其余字段的写回都是**同值写**（不改玩家真设置）。
//  · `_last`（进程内缓存）跑完是最后一份 `d`（其余 38 格 = 当前状态）—— 如实记在这里。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/<某个宿主>.cs` 里：`C2_Assert.Run(); _sink.Pass += C2_Assert.Pass; _sink.Fail += C2_Assert.Fail;`
//  ⚠️ 本段**不需要**任何已建好的窗口（B 段自己 `Create(null)` 建、跑完拆）。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `C2`：① 读档后应用 vSync / 帧率；② `ItemDisplayWindow.Context.Amount`。</summary>
public static class C2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[C2] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    /// <summary>全部两段（自包含：不需要任何已建好的窗口）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "C2 · A（读档后应用 vSync / 帧率 = 原版 `PlayerDataManager.Awake` 那一跳）");
        try { A_ApplyDisplaySettings(); }
        catch (Exception e) { Tr(false, "A 段抛异常：" + e.GetType().Name + "：" + e.Message); }

        MenuCheck.Section(_sink, "C2 · B（`ItemDisplayWindow.Context.Amount` = 原版 `ItemDisplayWindowContext.+0x18`）");
        try { B_ItemDisplayAmount(); }
        catch (Exception e) { Tr(false, "B 段抛异常：" + e.GetType().Name + "：" + e.Message); }

        Debug.Log(_sink.P + "C2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A：读档后应用显示设置

    static void A_ApplyDisplaySettings()
    {
        string prevRoot = SaveCenter.OverrideRoot;
        bool prevPersist = LocalPlayerDataStore.PersistOverride;
        Action<int> prevVsync = SettingsWindow.VSyncSetterOverride;
        int tf0 = Application.targetFrameRate;

        // 🔴 本段**必须**跑 `PersistOverride = false` 那一条（被验的就是它）⇒ 其余各族的「自检不许动玩家真设置」
        //    注入点**全部临时打开**（写回的都是 `Collect()` 取到的当前值，打开它们是为了连「同值写」也不发生）。
        bool pAccState = SettingsWindow.AccountState.PersistOverride;
        bool pTextInHand = SettingsWindow.TextInHand.PersistOverride;
        bool pGenFlags = SettingsWindow.GeneralFlags.PersistOverride;
        bool pAutoZoom = CardPresentation.AutoZoom.PersistOverride;
        bool pSuperSampling = CardPresentation.SuperSampling.PersistOverride;
        bool pGraphicQuality = GraphicQuality.PersistOverride;
        bool pSmallScreen = SmallScreenUI.PersistOverride;
        bool pLoc = Loc.PersistOverride;

        // `Apply()` 会灌这一组镜像 —— 全备份，跑完还原（⛔ 别把别的段的前提改掉）
        bool v0 = GameStaticData.vsync;
        int f0 = GameStaticData.FPSLimit;
        bool dp0 = GameStaticData._disableParticles;
        bool hi0 = GameStaticData.hiFPS;
        bool ss0 = GameStaticData.useSuperSampling;
        bool ec0 = GameStaticData.graphicsExtendedCompatibility;
        long lr0 = GameStaticData.lastRegistrationShowTime;
        long ln0 = GameStaticData.lastTimeAskedForNotifications;
        float mv0 = GameStaticData.MusicVolume, sv0 = GameStaticData.SoundFxVolume, vv0 = GameStaticData.VoiceOversVolume;
        int lv0 = GameStaticData.languageVersion;
        int htf0 = CardView.HandTextFormat;

        string root = Path.Combine(Path.GetTempPath(), "wf_c2_" + Guid.NewGuid().ToString("N"));
        int asked = -12345;                       // 哨兵：`VSyncSetterOverride` 没被调过
        LocalPlayerData dOrig = null;             // `Collect()` 的原始快照（收尾放回进程内 `_last`）
        try
        {
            Directory.CreateDirectory(root);
            SaveCenter.OverrideRoot = root;       // 临时档根 ⇒ 末句那次重写不碰玩家真档
            SettingsWindow.VSyncSetterOverride = c => asked = c;   // 拦下 `QualitySettings.vSyncCount`（⛔ 不写脏工程资产）
            SettingsWindow.AccountState.PersistOverride = true;
            SettingsWindow.TextInHand.PersistOverride = true;
            SettingsWindow.GeneralFlags.PersistOverride = true;
            CardPresentation.AutoZoom.PersistOverride = true;
            CardPresentation.SuperSampling.PersistOverride = true;
            GraphicQuality.PersistOverride = true;
            SmallScreenUI.PersistOverride = true;
            Loc.PersistOverride = true;
            Tr(SaveCenter.StoreOn, "（前提）临时档根已注入 ⇒ `SaveCenter.StoreOn` = true");

            var d = LocalPlayerDataStore.Collect();
            Tr(d != null, "（前提）`Collect()` 取到当前总档底板（后面的写回 = 同值写）");
            if (d == null) return;
            dOrig = LocalPlayerDataStore.CopyOf(d);   // `Apply` 会写进程内 `_last` ⇒ 收尾放回去（不污染后面的段）
            // ---- ① `vsync = false` · `fpsLimit = 0` ⇒ 原版会写 `vSyncCount = 0` / `targetFrameRate = 30`
            LocalPlayerDataStore.PersistOverride = false;      // ← 要的就是「真应用」那一支
            d.vsync = false; d.fpsLimit = 0;
            asked = -12345;
            LocalPlayerDataStore.Apply(d);
            C(asked, 0, "A1 `vsync = false` ⇒ 向 `QualitySettings.vSyncCount` 的写入请求 = **0**"
                      + "（原版 `QualitySettingsManager.cs:82-83` `vSyncCount = vsync ? 1 : 0`；🧨 删掉 `Apply` 尾部那一跳 ⇒ 哨兵 -12345 ⇒ 红）");
            C(Application.targetFrameRate, 30, "A2 `fpsLimit = 0` ⇒ `Application.targetFrameRate = **30**`"
                      + "（`PlayerDataManager__ApplyGraphicsQuality.c` 那一支；🧨 换成别的档 ⇒ 红）");

            // ---- ② 灭自证：换一组**相反**的值，必须跟着变
            d.vsync = true; d.fpsLimit = 2;
            asked = -12345;
            LocalPlayerDataStore.Apply(d);
            C(asked, 1, "A3（灭自证）`vsync = true` ⇒ 写入请求 = **1**（挡「恒写 0 / 恒不写」）");
            C(Application.targetFrameRate, -1, "A4（灭自证）`fpsLimit = 2` ⇒ `targetFrameRate = **-1**`（不限帧）");

            // ---- ③ 第三档：`fpsLimit = 1` ⇒ 60
            d.vsync = false; d.fpsLimit = 1;
            asked = -12345;
            LocalPlayerDataStore.Apply(d);
            C(Application.targetFrameRate, 60, "A5 `fpsLimit = 1` ⇒ `targetFrameRate = **60**`（三档最后一条）");

            // ---- ④ `PersistOverride`（自检档）⇒ 一个都不许写
            LocalPlayerDataStore.PersistOverride = true;
            int tfBefore = Application.targetFrameRate;
            d.vsync = true; d.fpsLimit = 0;
            asked = -12345;
            LocalPlayerDataStore.Apply(d);
            C(asked, -12345, "A6（前提/反例）`PersistOverride = true` ⇒ **一次都不请求**写 `vSyncCount`（哨兵原样）");
            C(Application.targetFrameRate, tfBefore, "A7（前提/反例）`PersistOverride = true` ⇒ `targetFrameRate` **一个字节都不动**");
        }
        finally
        {
            SettingsWindow.VSyncSetterOverride = prevVsync;
            LocalPlayerDataStore.PersistOverride = prevPersist;
            LocalPlayerDataStore.SetLastForTest(dOrig);   // 把进程内 `_last` 放回本段开始前那一份
            SettingsWindow.AccountState.PersistOverride = pAccState;
            SettingsWindow.TextInHand.PersistOverride = pTextInHand;
            SettingsWindow.GeneralFlags.PersistOverride = pGenFlags;
            CardPresentation.AutoZoom.PersistOverride = pAutoZoom;
            CardPresentation.SuperSampling.PersistOverride = pSuperSampling;
            GraphicQuality.PersistOverride = pGraphicQuality;
            SmallScreenUI.PersistOverride = pSmallScreen;
            Loc.PersistOverride = pLoc;
            SaveCenter.OverrideRoot = prevRoot;
            Application.targetFrameRate = tf0;
            GameStaticData.vsync = v0;
            GameStaticData.FPSLimit = f0;
            GameStaticData._disableParticles = dp0;
            GameStaticData.hiFPS = hi0;
            GameStaticData.useSuperSampling = ss0;
            GameStaticData.graphicsExtendedCompatibility = ec0;
            GameStaticData.lastRegistrationShowTime = lr0;
            GameStaticData.lastTimeAskedForNotifications = ln0;
            GameStaticData.MusicVolume = mv0;
            GameStaticData.SoundFxVolume = sv0;
            GameStaticData.VoiceOversVolume = vv0;
            GameStaticData.languageVersion = lv0;
            CardView.SetHandTextFormat(htf0);      // `HandTextFormat` 是 `private set` ⇒ 只能走它的 setter 还原
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { /* 临时目录删不掉不算缺陷 */ }
        }
    }

    // ============================================================ B：`Context.Amount`

    static void B_ItemDisplayAmount()
    {
        ItemDisplayWindow w1 = null, w2 = null, w3 = null;
        try
        {
            // 前提：窗建得出来、那一格建得出来（`ShowAmount = true`）
            w1 = ItemDisplayWindow.Create(null, new ItemDisplayWindow.Context
            {
                Title = "C2", Type = "C2", ArtName = "c2", Amount = 1, ShowAmount = true,
            });
            Tr(w1 != null, "（前提）`ItemDisplayWindow.Create(null, ctx)` 建得出来");
            if (w1 == null) return;
            Tr(w1.AmountLabel != null, "（前提）`ShowAmount = true` ⇒ 数量那一格建出来了（`AmountLabel != null`）");

            C(w1.Ctx.Amount, 1, "B1 `Context.Amount` 原样进了窗（原版 `ItemDisplayWindowContext.+0x18`）");
            if (w1.AmountLabel != null)
                C(w1.AmountLabel.Text, "1", "B2 `Amount = 1` ⇒ 那一格写 **`1`**（原版 `amount.ToString()`，裸数字、无 `x` 前缀）");

            // 灭自证：换一个值
            w2 = ItemDisplayWindow.Create(null, new ItemDisplayWindow.Context
            {
                Title = "C2", Type = "C2", Amount = 7, ShowAmount = true,
            });
            Tr(w2 != null && w2.AmountLabel != null, "（前提）第二扇同样建得出数量那一格");
            if (w2 != null && w2.AmountLabel != null)
                C(w2.AmountLabel.Text, "7", "B3（灭自证）`Amount = 7` ⇒ 那一格写 `7`（挡「恒写某个固定值」）");

            // 反例：`ShowAmount = false`（= `Cardback Drawer` / `Icon Currency Drawer Variant` 两族的真实情形）
            w3 = ItemDisplayWindow.Create(null, new ItemDisplayWindow.Context
            {
                Title = "C2", Type = "C2", Amount = 3, ShowAmount = false,
            });
            Tr(w3 != null, "（前提）第三扇建得出来");
            if (w3 != null)
                Tr(w3.AmountLabel == null, "B4 `ShowAmount = false` ⇒ **不建**数量那一格（原版这两族：`Cardback Drawer` 的 "
                                         + "`Content/Label` `m_IsActive = 0` · `Icon Currency Drawer Variant` 的 `quantity` = null ⇒ 本来就不画）");

            // `ForCardback` 的实参 = 1（原版 `ItemDisplayWindowContext(item, 1)`），且不显示数量
            string cb = FirstCardbackName();
            Tr(cb != null, "（前提）`Resources/Cardbacks.json` 里至少有一条卡背（否则下面两条无从验）");
            if (cb != null)
            {
                var c = ItemDisplayWindow.ForCardback(cb);
                C(c.Amount, 1, "B5 `ForCardback` 的 `Amount = 1`（= 原版 `CardbackCollectionTab__OnItemSelected` 的实参）");
                Tr(!c.ShowAmount, "B6 `ForCardback` 的 `ShowAmount = false`（卡背抽屉那一行整组 INACT ⇒ 原版不画数量）");
            }
        }
        finally
        {
            DestroySafe(w1); DestroySafe(w2); DestroySafe(w3);
        }
    }

    /// <summary>从 `Resources/Cardbacks.json` 取第一条卡背的图名（**独立读数据**，不走被测的取数口）。</summary>
    static string FirstCardbackName()
    {
        var ta = Resources.Load<TextAsset>(CardbackTable.ResourcePath);
        if (ta == null) return null;
        var f = JsonUtility.FromJson<CbFile>(ta.text);
        return f != null && f.items != null && f.items.Length > 0 ? f.items[0].name : null;
    }

    [Serializable]
    class CbFile { public CardbackTable.Item[] items; }

    static void DestroySafe(UnityEngine.Object o)
    {
        if (o == null) return;
        try
        {
            if (o is Component) UnityEngine.Object.DestroyImmediate(((Component)o).gameObject);
            else UnityEngine.Object.DestroyImmediate(o);
        }
        catch { /* 批处理下已销毁的对象再 DestroyImmediate 会抛 —— 不算缺陷 */ }
    }
}
