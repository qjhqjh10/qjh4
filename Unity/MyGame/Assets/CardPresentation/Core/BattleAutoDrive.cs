// BattleAutoDrive.cs — 「构建后 player 验证」用的**自动打一局**驱动
//
// ── 为什么有它 ──────────────────────────────────────────────────────────
// `项目任务.md` 顶部待办表第 1 行的验收是「在 player 里**打一局对战**」，
// 而 player 里**没有任何输入驱动**（原版有 `SceneJumpShot` 的 `drive` 动作，我们没有）。
// 加上 `-wfdrive` 之后，player 会自己把一局打完 ⇒ 「发牌 → 部署 → 攻击 → 阵亡 →
// 回合流转 → 结算/开门视频」整条链**在真包里**都会被跑到、被拍到、被 `Player.log` 记下来。
//
// ── 怎么驱动 ────────────────────────────────────────────────────────────
// **复用现成的 `BattleDriver.SimulateAiTurn()`** —— 它内部就是
// `SimpleAI.PlayTurn(Ctx)`（`RuleEngine/Data/SimpleAI.cs`）+ `EndTurn` + `BeginTurn`。
// 也就是说：**谁在行动就替谁打**，反复调就推完一整局。
//
// 🔴 **它是「AI 代打」，不是「模拟人类操作」** —— 人类那一侧的拖拽 / 攻击方式选择器 /
//    选牌面板 / 拖到场上这些**不会被走到**。这是**有意的**：
//    本轮要验的是「**真包里的资源与渲染**」（shader / bundle / 字体 / Resources），
//    人类交互由编辑器自检（`BattleScene.Run`，52 条断言 + 52 张图）覆盖。
//    ⚠️ **别把这里的绿灯读成「交互在真包里也没问题」**。
//
// ── 用法 ────────────────────────────────────────────────────────────────
//   WarpforgePlayer.exe -wfscene Battle -wfdrive -wfshot <目录>/x.png -wfquit 150
//   截图落在 `-wfshot` 的同目录下，文件名 `auto_<序号>_<阶段>.png`；
//   没给 `-wfshot` 就不截图，只写日志。
using System.Collections;
using System.IO;
using CardPresentation;   // ⚠️ `BattleDriver` 在这个命名空间里 —— 漏了就是 `error CS0246`
using UnityEngine;

public class BattleAutoDrive : MonoBehaviour
{
    /// <summary>每一步之间等多久（秒）—— 留时间给画面，也留时间给截图。</summary>
    public float stepGap = 1.0f;

    /// <summary>保险丝：最多驱多少步。跑不完就**如实报**，别静默停在那儿。</summary>
    public int maxSteps = 200;

    /// <summary>截图目录（`PlayerBoot` 从 `-wfshot` 的目录取；空 = 不截图）。</summary>
    public string shotDir;

    int _shot;
    bool _running;

    public void Begin()
    {
        if (_running) return;
        _running = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // ---- 1. 等战斗场景就绪 -------------------------------------------------
        // 不是 Battle 场景的话这里永远等不到 —— 所以给 60 s 上限并**报出来**（不静默挂着）
        BattleDriver drv = null;
        for (float t = 0f; t < 60f; t += Time.unscaledDeltaTime)
        {
            drv = Object.FindFirstObjectByType<BattleDriver>();
            if (drv != null && drv.Ctx != null) break;
            yield return null;
        }
        if (drv == null || drv.Ctx == null)
        {
            Debug.LogError($"[AutoDrive] 60 s 内没等到 BattleDriver/BattleContext —— " +
                           $"这场没驱起来（`-wfdrive` 只对 Battle 场景有意义）");
            yield break;
        }
        Debug.Log($"[AutoDrive] 接上了：我是 {(drv.MyIndex == 0 ? "P1" : "P2")}，换牌中={drv.InMulligan}");
        yield return Shot("01_接上");

        // ---- 2. 换牌：直接「完成」 ---------------------------------------------
        // 换牌面板的**观感**在 `资料/特效还原_进度与交接.md` §七 记着（与编辑器基线有两处差异，待判）；
        // 这里不挑牌，直接把这一阶段推过去。
        for (float t = 0f; drv.InMulligan && t < 10f; t += Time.unscaledDeltaTime)
        {
            if (drv.SimulateMulliganDone()) Debug.Log("[AutoDrive] 换牌：直接完成（不挑牌）");
            yield return null;
        }
        yield return Shot("02_换牌之后");

        // ---- 3. 一步步驱到结束 -----------------------------------------------
        int n = 0;
        while (!drv.Ctx.IsOver && n < maxSteps)
        {
            n++;
            int turnBefore = drv.Ctx.Turn;
            string who = drv.Ctx.Active == drv.MyIndex ? "我" : "对手";
            try
            {
                drv.SimulateAiTurn();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AutoDrive] 第 {n} 步抛异常，停下：{e}");
                yield break;
            }

            Debug.Log($"[AutoDrive] {n,3}. {who} 的回合（{turnBefore} → {drv.Ctx.Turn}）"
                      + $"　督军血 P1={Hp(drv, 0)} P2={Hp(drv, 1)}");

            if (n == 6) yield return Shot("03_打了六步");
            yield return new WaitForSeconds(stepGap);
        }

        // ---- 4. 收尾：如实报 ---------------------------------------------------
        if (drv.Ctx.IsOver)
        {
            Debug.Log($"[AutoDrive] ✅ 对局结束：共 {n} 步 · 结果 Winner={drv.Ctx.Winner}"
                      + $"（1=P1 胜 · 2=P2 胜 · 3=平局）· 我是 P{drv.MyIndex + 1}");
            // 结算面板 / 开门视频要几秒才铺开，等一会儿再拍
            for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime) yield return null;
            yield return Shot("04_结算");
        }
        else
        {
            Debug.LogWarning($"[AutoDrive] ⚠️ 到保险丝（{maxSteps} 步）还没结束 —— **如实报，别当跑完了**");
            yield return Shot("04_保险丝");
        }

        // ---- 5. 打完了就自己退 ------------------------------------------------
        // 不停在这儿等 `-wfquit`：那一局 12 步、二十来秒就打完了，而 `-wfquit 240` 是**保险丝**
        //（万一这局永远打不完）。跑批的等待时间差 10 倍。
        // ⚠️ 想打完还留着看，就别给 `-wfdrive`（或者给 `-wfquit` 但自己去掉这一句 —— 这是工具，不是产品逻辑）。
        Debug.Log("[AutoDrive] 收工，退出 player");
        Application.Quit(0);
    }

    static int Hp(BattleDriver drv, int player)
    {
        var w = drv.Ctx.Players[player] != null ? drv.Ctx.Players[player].Warlord : null;
        return w != null ? w.Health : -1;
    }

    IEnumerator Shot(string what)
    {
        if (string.IsNullOrEmpty(shotDir)) yield break;
        try
        {
            Directory.CreateDirectory(shotDir);
            var path = $"{shotDir}/auto_{++_shot:00}_{what}.png";
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[AutoDrive] 截图 → {path}");
        }
        catch (System.Exception e) { Debug.LogError($"[AutoDrive] 截图失败：{e.Message}"); }
        // `CaptureScreenshot` 在帧末才写盘 —— 多等几帧，否则文件是空的
        for (int i = 0; i < 5; i++) yield return new WaitForEndOfFrame();
    }
}
