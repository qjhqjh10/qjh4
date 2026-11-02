// B117_WGB_Assert.cs — 执行代理 `B117-WGB`（2026-11-02 · 第一百一十七会话 · 完全复刻）
//
//   A · `B117-18`  `ASH15` 的「部署语音」是**按名猜的孤儿行** ⇒ `VoiceLines.Has` 判「这张卡有没有**卡 SO 归属**行」
//   B · `B117-35`  拖拽跟手换成原版 `CardScript.SetCardMovement` 的两档（硬吸附 `d2 < 0.01 / > 1000`、平滑 `10.8 × dt` **不 clamp**）
//
// 用法（⚠️ 宿主由**调度台**接 —— 本文件没有改任何 `Editor/*Scene.cs`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B117_WGB_Assert.Run -logFile -
//   接线（任一条既有聚合行上追加）：
//     Guard("B117_WGB_Assert", B117_WGB_Assert.Run); _pass += B117_WGB_Assert.Pass; _fail += B117_WGB_Assert.Fail;
//
// 判绿红：合计行 `B117WGB === 合计：…`；逐条按**行首** `B117WGB ✓` / `B117WGB ✗` 数（⛔ 别 `grep -c ✗`）。
//
// 🔴 灭自证（哪一条改回旧写法就红）：
//   · A 段：把 `VoiceLines.Has` 改回 `_byId.ContainsKey(cardId)` ⇒ A4 **红**
//     （A2 已断「`ASH15` 的 `deploy` 行**还在表里**」⇒ 两条结构上不可能同时为真）。
//   · B 段：把跟手改回 `Vector3.Lerp(prev, target, 1f − Mathf.Exp(-20f*dt))` ⇒ B1/B2/B3 全红
//     （远跳只走 28.35%、0.05 那一步贴不住、系数 0.2835 ≠ 0.18）；
//     把硬吸附那一档的 `rx/ry 归零` 去掉 ⇒ B5 红（dy = 100 ⇒ rx 会到 20°）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B117-WGB`：`B117-18`（`ASH15` 语音 orphan）＋ `B117-35`（拖拽两档跟手）。</summary>
public static class B117_WGB_Assert
{
    const string P = "B117WGB ";

    static int _pass, _fail;
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F4") + "）");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        try
        {
            // ⚠️ 每组各包一层：一组炸掉**不许**把另一组一起拖没（否则报告里只剩一条异常、别的全是「没验」）。
            Group("A·ASH15 语音归属", A_VoiceAttribution);
            Group("B·拖拽两档跟手", B_DragFollow);
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message);
        }
        finally
        {
            Debug.Log(P + "=== 合计：" + _pass + " 通过 / " + _fail + " 失败（共 " + (_pass + _fail) + " 条）===");
        }
    }

    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    // ==================================================================
    //  A · `B117-18`：`ASH15` 的部署语音是**按名猜的孤儿行** ⇒ 语音钮不该建
    // ==================================================================
    //  判据（第一权威，亲读）：`d:/2/tools/decomp_full/CardDisplayWindow__SetCardVoiceOver.c`
    //    → `RawCardScript__GetAvailableSounds.c`（把 `defaultChatSound` + 11 个 `*ChatSound`
    //      + `summon/attack/deathAudio` 逐个收进列表 → `Where(clip != null)` → `SetActive(0 < Count)`）；
    //    `ASH15` 的卡 SO **15 个音频字段全是空 GUID** ⇒ 列表空 ⇒ **钮不建**。
    //  我们的落点 = `CardPresentation/Core/VoiceLines.cs` 的 `Has()`（改前判「表里有没有这张卡」）。
    //  ⛔ 本批**没有**改 `voice_lines.json`（那条行 `src` 留空是生成器的正确标法）。
    static void A_VoiceAttribution()
    {
        Check(VoiceLines.Ready, "（前提）语音表载入成功（LoadError=" + (VoiceLines.LoadError ?? "无") + "）");

        // 🔴 **前提先断**（铁律：`if (x != null) Check(...)` 那种写法 = 前提不成立时静默跳过）
        Check(VoiceLines.LineRowCount("ASH15", "deploy") == 1,
              "（前提）`voice_lines.json` 里 `ASH15` 的 `deploy` 行**仍然在**（1 条） —— 本批没有动数据文件"
            + "｜实测 " + VoiceLines.LineRowCount("ASH15", "deploy"));
        Check(VoiceLines.SoRowCount("ASH15", "deploy") == 0,
              "（前提）那条行**没有卡 SO 归属**（`so` 空且 `src != \"cardSO\"`）—— 原版对这张卡一个音频字段都没填"
            + "｜实测 " + VoiceLines.SoRowCount("ASH15", "deploy"));

        Check(!VoiceLines.Has("ASH15"),
              "★ B117-18：`Has(\"ASH15\")` = **false** ⇒ 详情窗 / 放大窗的语音钮**不建**"
            + "（原版判据 = `GetAvailableSounds().Count == 0`）"
            + "｜🧨 改回 `_byId.ContainsKey(cardId)` ⇒ 与上面那条「行还在」**结构上不可能同时为真** ⇒ 红");

        // 正控：有 SO 归属行的卡照旧有语音钮（这两条挡的是「为了让它绿把 Has 直接写死 false」）
        Check(VoiceLines.Has("ASH2"),
              "正控：`Has(\"ASH2\")` = true（它有 `src=cardSO` 的行 ⇒ 语音钮照旧建）");
        Check(VoiceLines.Has("UM88"),
              "正控：`Has(\"UM88\")` = true（同上；`so=deathAudio` 一族）");

        // 边界
        Check(!VoiceLines.Has(null), "边界：`Has(null)` = false（不抛）");
        Check(!VoiceLines.Has("__NOPE__"), "边界：表里没有的 id = false");
    }

    // ==================================================================
    //  B · `B117-35`：拖拽跟手 = 原版 `CardScript.SetCardMovement` 的两档
    // ==================================================================
    //  判据（二进制读过 + 指令流核对）：`decomp_full/CardScript__SetCardMovement.c` + VA `0x1805FC4E0`
    //    · `d2 = Σ(target − cur)²`；`0x1805FC5C2 comiss [0x1834B2DBC = 0.01]` / `0x1805FC5D3 comiss [0x1834B3124 = 1000]`
    //      ⇒ **`d2 < 0.01` 或 `> 1000` ⇒ `position = target`（硬吸附）**；
    //    · 否则 `pos += (target − pos) × (cardMovementSpeed × dt)`，**没有 `Clamp01`**；
    //    · `cardMovementSpeed`(`+0x2E8`) = 10.8（ctor `0x412ccccd` / `CardPrefab.prefab:2855`）⇒ 60fps 系数 **0.18**（改前 0.2835）。
    //    · 硬吸附那一支还把 `rotationTarget` 拷成静态 `Vector3`（`0x1805FC7A6..`）—— 静态度没落地核，
    //      按「与 ctor 的 `targetPosDuringAnim` 同源」**推断 = `Vector3.zero`** ⇒ rx/ry 写 0（⚠️ 推断，未 100% 钉死）。
    //  ⚠️ 世界单位不用换算：手牌同样是 108 px/单位（`Hand/HandLayout.cs:96 / :457`）。
    static void B_DragFollow()
    {
        var root = new GameObject("B117WGB_drag");
        CardView card = null;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            // 批处理没有帧循环 ⇒ 补间只能手动推；本组不推 ⇒ 拿到的就是「这一帧 `UpdateDrag` 写完的值」。
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            CardTween.LinkEnabled = false;

            var layout = root.AddComponent<HandLayout>();
            var it = root.AddComponent<CardInteraction>();
            it.hand = layout;
            it.dragLiftZ = 0.6f;
            it.SetRightButtonHeldForTest(false);      // 批处理里没有鼠标设备 ⇒ 把闸钉成「没按右键」
            card = CardView.Create(root.transform, CardData.Placeholder(1), "B117WGB_C0", CardFace.Hand);
            it.SetCards(new List<CardView> { card }, false);
            it.PickUpForTest(card, card.transform.position);
            Check(it.IsDragging, "（前提）`PickUp` 进了拖拽态（否则下面四条全是空验）");
            if (!it.IsDragging) return;

            float dt = 1f / 60f;
            Vector3 lift = new Vector3(0f, 0f, -it.dragLiftZ);
            Vector3 p0 = card.transform.position;

            // ① 远距硬吸附：d2 > 1000（y 取 +5 世界单位 ⇒ 也在手牌限位线上方，让位那一段不参与）
            Vector3 w1 = p0 + new Vector3(100f, 5f, 0f);
            it.SimulateDrag(w1, dt);
            Vector3 t1 = w1 + lift;
            Check(Vector3.Distance(card.transform.position, t1) < 1e-3f,
                  "★ B117-35：`d2 > 1000` ⇒ **硬吸附**（位置 == target；实得 " + card.transform.position
                + "，期望 " + t1 + "）"
                + "｜判据 VA `0x1805FC5D3 comiss [0x1834B3124(=1000)]`"
                + "｜🧨 旧公式只走 28.35% ⇒ 差约 71 世界单位 ⇒ 红");

            // ② 近距硬吸附：d2 < 0.01（0.05 世界单位 ≈ 5.4 px）
            Vector3 w2 = w1 + new Vector3(0.05f, 0f, 0f);
            it.SimulateDrag(w2, dt);
            Vector3 t2 = w2 + lift;
            Check(Vector3.Distance(card.transform.position, t2) < 1e-4f,
                  "★ B117-35：`d2 < 0.01` ⇒ **硬吸附**（0.05 世界单位直接贴住）"
                + "｜判据 VA `0x1805FC5C2 comiss [0x1834B2DBC(=0.01)]`"
                + "｜🧨 旧公式会残留约 0.036 的指数尾 ⇒ 红");

            // ③ 平滑档：`pos += (target − pos) × (10.8 × dt)` —— 60fps 走 18%
            Vector3 w3 = w2 + new Vector3(0.5f, 0f, 0f);
            Vector3 prev3 = card.transform.position;
            it.SimulateDrag(w3, dt);
            Vector3 t3 = w3 + lift;
            Vector3 want3 = prev3 + (t3 - prev3) * (10.8f * dt);
            Check(Vector3.Distance(card.transform.position, want3) < 1e-4f,
                  "★ B117-35：平滑档 `pos += (target−pos) × (10.8 × dt)`（实得 " + card.transform.position
                + "，期望 " + want3 + "）");
            float moved = card.transform.position.x - prev3.x;
            CheckClose(moved, 0.09f, 1e-3f, "★ B117-35：0.5 世界单位的位移里这一帧走 **0.09**（= 0.5 × 0.18）");
            Check(Mathf.Abs(moved - 0.14175f) > 0.03f,
                  "🧨 灭自证：旧公式 `1 − exp(−20·dt)` = 0.2835 会给 **0.14175** ⇒ 与上一条结构上不可能同时绿");

            // ④ 平滑档**仍然**按原版算倾斜（dy = 0.2 ⇒ rx = 40 × clamp01(0.02 + 0.5) − 20 = 0.8）
            Vector3 w4 = w3 + new Vector3(0f, 0.2f, 0f);
            it.SimulateDrag(w4, dt);
            float rx4 = Mathf.DeltaAngle(0f, card.transform.localEulerAngles.x);
            CheckClose(rx4, 0.8f, 0.05f,
                       "B117-35：平滑档仍算倾斜（`rx` = 0.8°；d2 = 0.208 落在平滑档）"
                     + "｜🧨 把倾斜整段删掉 ⇒ 0 ⇒ 红");

            // ⑤ 硬吸附档把 rx/ry 归零（dy = 100 ⇒ 不归零的话 rx 会到 20°）
            Vector3 w5 = w4 + new Vector3(0f, 100f, 0f);
            it.SimulateDrag(w5, dt);
            float rx5 = Mathf.DeltaAngle(0f, card.transform.localEulerAngles.x);
            float ry5 = Mathf.DeltaAngle(0f, card.transform.localEulerAngles.y);
            Check(Mathf.Abs(rx5) < 0.01f && Mathf.Abs(ry5) < 0.01f,
                  "★ B117-35：硬吸附那一档 **rx/ry 归零**（原版把 `rotationTarget` 拷成静态 `Vector3`，"
                + "按 ctor 同源推断 = `Vector3.zero`；实得 rx=" + rx5.ToString("F3") + " ry=" + ry5.ToString("F3") + "）"
                + "｜🧨 不归零 ⇒ 这一档 dy = 100 ⇒ rx 到 20° ⇒ 红（⚠️ 推断强度：未 100% 钉死）");
            Check(Vector3.Distance(card.transform.position, w5 + lift) < 1e-3f,
                  "★ B117-35：该档位置同样瞬移到位（与 ① 同一判据，用大 dy 再验一次）");
        }
        finally
        {
            CardTween.Mode = keepMode;
            CardTween.LinkEnabled = keepLink;
            if (card != null) Object.DestroyImmediate(card.gameObject);
            Object.DestroyImmediate(root);
        }
    }
}
