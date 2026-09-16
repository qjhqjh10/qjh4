// BattleAutoDrive.cs — 「构建后 player 验证」用的**自动打一局**驱动（两种路子）
//
// ── 为什么有它 ──────────────────────────────────────────────────────────
// `项目任务.md` 顶部待办表第 1 行的验收是「在 player 里**打一局对战**」，
// 而 player 里**没有任何输入驱动**（原版有 `SceneJumpShot` 的 `drive`，我们没有）。
// 加上 `-wfdrive`（或 `-wfhuman`）之后，player 会自己把一局打完 ⇒ 「发牌 → 部署 → 攻击 →
// 阵亡 → 回合流转 → 结算/开门视频」整条链**在真包里**都会被跑到、被拍到、被 `Player.log` 记下来。
//
// ── 两种路子（`-wfdrive` / `-wfhuman`）──────────────────────────────────
// | | `-wfdrive`（默认） | `-wfhuman` |
// |---|---|---|
// | 怎么走 | 反复调 `BattleDriver.SimulateAiTurn()` = `SimpleAI.PlayTurn` + 换边 | **我的回合走 UI 那条路**：`SimulatePlay` / `SimulateOpenCommand`+`SimulateCommand`+`SimulateResolve`（**真·攻击方式选择器**）/ `SimulateUseAbility` / `SimulateEndTurn`；对手的回合仍用 `SimulateAiTurn` |
// | 覆盖面 | 引擎 + 渲染整条链 | 上面那些 **UI 入口**（选择器、技能面板、结束回合、换牌面板） |
// | 风险 | 最低 | 高一些（UI 有前提条件，任何一步不合法就**如实记下来**并结束该回合） |
//
// 🔴 **两条路子都走不到的**（**别把它们读成「已验」**）：
//   · **拖拽手势本身** —— `CardInteraction` 的拖拽吃的是真实指针输入，
//     `SimulatePointerAt` 只喂选择器/准星，**不驱动拖拽**；
//   · 卡组编辑场景的按钮（那个场景**根本没有交互组件**，见 `DeckScene.cs`）。
//
// ── 用法 ────────────────────────────────────────────────────────────────
//   WarpforgePlayer.exe -wfscene Battle -wfdrive  -wfshot <目录>/x.png -wfquit 240
//   WarpforgePlayer.exe -wfscene Battle -wfhuman  -wfshot <目录>/x.png -wfquit 240
//   截图落在 `-wfshot` 的同目录下，文件名 `auto_<序号>_<阶段>.png`；
//   没给 `-wfshot` 就不截图，只写日志。跑完**自己退**（`-wfquit` 只是保险丝）。
using System.Collections;
using System.IO;
using CardPresentation;    // ⚠️ `BattleDriver` / `AttackKind` 在这个命名空间里（漏了 = error CS0246）
using RuleEngine;          // `SimpleAI` / `RuleCodes` / `BattleContext`
using UnityEngine;

public class BattleAutoDrive : MonoBehaviour
{
    /// <summary>每一步之间等多久（秒）—— 留时间给画面，也留时间给截图。</summary>
    public float stepGap = 1.0f;

    /// <summary>保险丝：最多驱多少步。跑不完就**如实报**，别静默停在那儿。</summary>
    public int maxSteps = 200;

    /// <summary>我的回合里最多做几个动作（防「什么都做不了却一直转」）。</summary>
    public int maxActionsPerTurn = 30;

    /// <summary>截图目录（`PlayerBoot` 从 `-wfshot` 的目录取；空 = 不截图）。</summary>
    public string shotDir;

    /// <summary>true = 我的回合走**人类那条路**（面板 / 选择器 / 结束回合按钮），见文件头那张表。</summary>
    public bool humanStyle;

    BattleDriver _drv;
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
        for (float t = 0f; t < 60f; t += Time.unscaledDeltaTime)
        {
            _drv = Object.FindFirstObjectByType<BattleDriver>();
            if (_drv != null && _drv.Ctx != null) break;
            yield return null;
        }
        if (_drv == null || _drv.Ctx == null)
        {
            Debug.LogError($"[AutoDrive] 60 s 内没等到 BattleDriver/BattleContext —— " +
                           $"这场没驱起来（`-wfdrive`/`-wfhuman` 只对 Battle 场景有意义）");
            yield break;
        }
        Debug.Log($"[AutoDrive] 接上了：我是 P{_drv.MyIndex + 1}，换牌中={_drv.InMulligan}"
                  + $"，路子={(humanStyle ? "人类（走 UI 入口）" : "AI 代打")}");
        DumpSpace();
        yield return Shot("01_接上");

        // ---- 2. 换牌 ----------------------------------------------------------
        yield return Mulligan();
        // ⚠️ **等发牌补间跑完再拍**（`CardFeel.DealIn` 0.55 s + 手牌重排 0.18 s）。
        //    第一版拍完就拍，结果 `auto_02` 里「手牌 5」只画出了 3 张 ——
        //    **看着像「真包里少了两张牌」，其实是我的截图太早**（尺子的假象，本工程第 N 次）。
        yield return new WaitForSeconds(2.0f);
        DumpSpace();
        yield return Shot("02_换牌之后");

        // ---- 3. 一步步驱到结束 -----------------------------------------------
        int n = 0;
        while (!_drv.Ctx.IsOver && n < maxSteps)
        {
            n++;
            int turnBefore = _drv.Ctx.Turn;
            string who = _drv.Ctx.Active == _drv.MyIndex ? "我" : "对手";

            // ⚠️ **`yield return` 不能写在带 `catch` 的 `try` 里**（`error CS1626`）——
            //    所以「会抛异常的引擎调用」一律走不 yield 的普通方法，协程这层只负责推进。
            if (humanStyle && _drv.Ctx.Active == _drv.MyIndex)
            {
                yield return MyTurnByUi();
            }
            else
            {
                bool ok = true;
                try { _drv.SimulateAiTurn(); }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AutoDrive] 第 {n} 步抛异常，停下：{e}");
                    ok = false;
                }
                if (!ok) yield break;
            }

            Debug.Log($"[AutoDrive] {n,3}. {who} 的回合（{turnBefore} → {_drv.Ctx.Turn}）"
                      + $"　督军血 P1={Hp(0)} P2={Hp(1)}");

            if (n == 6) { DumpSpace(); yield return Shot("03_打了六步"); }
            yield return new WaitForSeconds(stepGap);
        }

        // ---- 4. 收尾：如实报 ---------------------------------------------------
        if (_drv.Ctx.IsOver)
        {
            Debug.Log($"[AutoDrive] ✅ 对局结束：共 {n} 步 · 结果 Winner={_drv.Ctx.Winner}"
                      + $"（1=P1 胜 · 2=P2 胜 · 3=平局）· 我是 P{_drv.MyIndex + 1}");
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
        // 不停在这儿等 `-wfquit`：那一局十来步、二十来秒就打完了，而 `-wfquit 240` 是**保险丝**。
        // ⚠️ 想打完还留着看，就别给 `-wfdrive` —— 这是跑批工具，不是产品逻辑。
        Debug.Log("[AutoDrive] 收工，退出 player");
        Application.Quit(0);
    }

    // ── 换牌 ────────────────────────────────────────────────────────────────
    IEnumerator Mulligan()
    {
        if (!_drv.InMulligan) yield break;

        if (humanStyle)
        {
            // 🔴 **走面板那颗「换」钮**（`SimulateMulliganToggle` 就是它的入口），
            //    顺便验一下「点了再点 = 取消」——那是 AI 那条路完全走不到的。
            _drv.SimulateMulliganToggle(0);
            _drv.SimulateMulliganToggle(0);
            bool ok = _drv.SimulateMulliganToggle(1);
            Debug.Log($"[AutoDrive] 换牌：点了第 1 张（再点取消）、第 2 张（保留标记={ok}）");
            yield return new WaitForSeconds(0.4f);
        }

        for (float t = 0f; _drv.InMulligan && t < 10f; t += Time.unscaledDeltaTime)
        {
            if (_drv.SimulateMulliganDone()) Debug.Log("[AutoDrive] 换牌：完成");
            yield return null;
        }
    }

    // ── 我的回合：走 UI 那条路 ───────────────────────────────────────────────
    //
    // ⚠️ **分成两层是有原因的**：`yield return` **不能出现在带 `catch` 的 `try` 里**
    //    （`error CS1626: Cannot yield a value in the body of a try block with a catch clause`）。
    //    所以「挑一个动作并执行」放在普通方法 `NextUiAction()` 里（那儿可以放心 try/catch），
    //    协程这层只负责「等一下、再挑下一个」。
    IEnumerator MyTurnByUi()
    {
        var ctx = _drv.Ctx;
        int acted = 0;

        while (!ctx.IsOver && ctx.Active == _drv.MyIndex && acted < maxActionsPerTurn)
        {
            string msg; float wait;
            if (!NextUiAction(out msg, out wait)) break;
            acted++;
            Debug.Log($"[AutoDrive·UI] {msg}");
            yield return new WaitForSeconds(wait);
        }

        if (acted >= maxActionsPerTurn)
            Debug.LogWarning($"[AutoDrive·UI] 本回合动作到上限（{maxActionsPerTurn}）就收手 —— 别在这儿空转");

        Debug.Log($"[AutoDrive·UI] 本回合做了 {acted} 个动作 → 结束回合");
        try { _drv.SimulateEndTurn(); }
        catch (System.Exception e) { Debug.LogError($"[AutoDrive·UI] 结束回合抛异常：{e}"); }
    }

    /// <summary>挑一个能做的动作并执行。`false` = 三样都做不了（外层去结束回合）。
    /// **这里不 yield**，所以可以放心 try/catch —— 见 `MyTurnByUi` 头上那条注释。</summary>
    bool NextUiAction(out string msg, out float wait)
    {
        msg = null; wait = 0.35f;
        var ctx = _drv.Ctx;
        try
        {
            // ① 出牌（走 UI 的出牌入口；⚠️ 跳过拖拽手势 —— 那个模拟不了，见文件头）
            int handIdx, slot;
            if (SimpleAI.NextPlay(ctx, out handIdx, out slot))
            {
                int code = _drv.SimulatePlay(handIdx, slot);
                if (code == RuleCodes.OK) { msg = $"出牌：手牌 #{handIdx} → 槽 {slot}"; return true; }
                Debug.LogWarning($"[AutoDrive·UI] 出牌被拒（码 {code}）：手牌 #{handIdx} → 槽 {slot}");
            }

            // ② 攻击 —— **真·攻击方式选择器**（开面板 → 点近战/远程 → 点目标）
            int atkSlot, targetP, targetSlot; bool ranged;
            if (SimpleAI.NextAttack(ctx, out atkSlot, out targetP, out targetSlot, out ranged)
                && _drv.SimulateOpenCommand(atkSlot))
            {
                // ⚠️ 近战还是远程**只有 `SimpleAI.UseRanged` 一处判据**，别在这儿另写一份
                var kind = ranged ? AttackKind.Ranged : AttackKind.Melee;
                if (_drv.HasCommand(kind))
                {
                    _drv.SimulateCommand(kind);
                    int code = _drv.SimulateResolve(targetSlot);
                    msg = $"攻击：槽 {atkSlot} 用 {kind} 打槽 {targetSlot}（码 {code}）";
                    wait = 0.45f;
                    return true;
                }
                Debug.LogWarning($"[AutoDrive·UI] 选择器上没有可点的「{kind}」（槽 {atkSlot}）");
                _drv.SimulateDeselect();      // 别把面板留在屏幕上
                wait = 0.2f;
            }

            // ③ 技能（走面板那条路）
            int abSlot, abTarget;
            if (SimpleAI.NextAbility(ctx, out abSlot, out abTarget))
            {
                int code = _drv.SimulateUseAbility(abSlot, abTarget);
                if (code == RuleCodes.OK) { msg = $"技能：槽 {abSlot} → 目标 {abTarget}"; wait = 0.45f; return true; }
                Debug.LogWarning($"[AutoDrive·UI] 技能被拒（码 {code}）：槽 {abSlot} → {abTarget}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AutoDrive·UI] 动作抛异常，本回合收手：{e}");
        }
        return false;
    }

    // ── 诊断：把「布局尺子」的当前状态打出来 ─────────────────────────────────
    //
    // 🔴 **为什么要有它**：player 里出现过「手牌挤在右下角、编辑器里是底部居中扇形」。
    //    手牌坐标全走 `LayoutSpace.VisibleWidth`，而它取决于 `LayoutSpace.Cam`；
    //    光看代码推不出差别（`cam` 是序列化的、`DesignAspect` 也是 16:9），
    //    ⇒ **量出来**（本工程的规矩：读代码只能确认你想到的那一种情况，量能发现想不到的）。
    void DumpSpace()
    {
        try
        {
            var cam = LayoutSpace.Cam;
            Debug.Log($"[AutoDrive·诊断] {LayoutSpace.Describe()}"
                      + $" · Screen={Screen.width}x{Screen.height}({Screen.currentResolution.width}x{Screen.currentResolution.height})"
                      + $" · VisibleWidth={LayoutSpace.VisibleWidth:F3} · Scale={LayoutSpace.Scale:F3}"
                      + $" · Cam={(cam != null ? cam.name + " aspect=" + cam.aspect.ToString("F3") : "<空>")}");

            var hand = Object.FindFirstObjectByType<HandLayout>();
            if (hand != null)
            {
                var hc = hand.GetComponentsInChildren<CardView>(true);
                Debug.Log($"[AutoDrive·诊断] HandLayout 下面 {hc.Length} 张");
            }

            // 🔴 **把场上所有卡连同父节点倒出来** —— 光看 `HandLayout` 会得出「0 张」这种没用的结论
            //    （换牌阶段卡是挂在换牌面板下面的）。要判「位置错没错」，得知道**它挂在谁下面**。
            var all = Object.FindObjectsByType<CardView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var sb = new System.Text.StringBuilder();
            foreach (var c in all)
            {
                var par = c.transform.parent;
                sb.Append($"\n      {c.name,-28} 父={(par != null ? par.name : "<无>"),-22}"
                        + $" 世界=({c.transform.position.x,6:F2},{c.transform.position.y,6:F2})"
                        + $" 缩放={c.transform.localScale.x:F3} 激活={c.gameObject.activeInHierarchy}");
            }
            Debug.Log($"[AutoDrive·诊断] 场上 CardView 共 {all.Length} 个：{sb}");
        }
        catch (System.Exception e) { Debug.LogError($"[AutoDrive·诊断] 抛异常：{e}"); }
    }

    int Hp(int player)
    {
        var p = _drv.Ctx.Players[player];
        var w = p != null ? p.Warlord : null;
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
