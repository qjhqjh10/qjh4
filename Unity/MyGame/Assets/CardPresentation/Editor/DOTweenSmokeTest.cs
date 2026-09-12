// DOTweenSmokeTest.cs — DOTween 装好没有，用「tween 跑不跑」来验，别只看编译过没过
//
// 批处理下没有 play 循环，所以用 `SetUpdate(UpdateType.Manual)` + `DOTween.ManualUpdate(dt)`
// 手动推进 —— 这样不进出 play 模式也能确定性地验完。
//
// 验三件事：
//   1. DLL 能不能加载、`DG.Tweening` 命名空间能不能用（编译期就验了）
//   2. tween 会不会真的按时间推进到目标值
//   3. 连 DOTweenSettings 都没建的情况下能不能工作（能的话就不用麻烦去点 Utility Panel）
//
// 用法：Unity.exe -batchmode -quit -projectPath ... -executeMethod DOTweenSmokeTest.Run -logFile -
//   筛输出：grep "^DWT " d:/4/_tmp_view/dotween_smoke.log
using System;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

public static class DOTweenSmokeTest
{
    const string P = "DWT ";

    public static void Run()
    {
        Debug.Log(P + "=== DOTween 冒烟测试 开始 ===");
        Debug.Log(P + $"DOTween 版本：{DOTween.Version}");

        // 1) 序列能不能建、会不会按时间走
        var go = new GameObject("TweenTarget");
        go.transform.position = Vector3.zero;
        var tw = go.transform.DOMove(new Vector3(3f, 0f, 0f), 1f).SetUpdate(UpdateType.Manual).SetEase(Ease.OutQuad);
        Debug.Log(P + $"建了 tween：{tw.Duration()}s，目标 x=3，缓动 OutQuad");

        float t = 0f, step = 0.1f;
        int steps = 0;
        while (!tw.IsComplete() && steps < 100)
        {
            DOTween.ManualUpdate(step, step);
            t += step; steps++;
        }
        // ⚠️ 判据用**位置**，不要用 IsComplete()：DOTween 默认 autoKill，
        //    完成后 tween 会被回收，此时 IsComplete() 反而返回 false（踩过）
        bool ok = Mathf.Abs(go.transform.position.x - 3f) < 0.01f;
        Debug.Log(P + $"推进 {t:F1}s 后：x={go.transform.position.x:F3}（目标 3.000）  {(ok ? "✅ 对" : "❌ 不对")}");
        Debug.Log(P + $"  参考：IsComplete()={tw.IsComplete()} IsActive()={tw.IsActive()}（autoKill 回收后都是 false/空，正常）");

        // 2) 缓动有没有生效（OutQuad 在 t=0.5 时应超过线性的 1.5）
        var go2 = new GameObject("TweenTarget2");
        go2.transform.position = Vector3.zero;
        var tw2 = go2.transform.DOMove(new Vector3(2f, 0f, 0f), 1f).SetUpdate(UpdateType.Manual).SetEase(Ease.OutQuad);
        DOTween.ManualUpdate(0.5f, 0.5f);
        float half = go2.transform.position.x;
        Debug.Log(P + $"缓动检查：t=0.5s 时 x={half:F3}（线性是 1.000，OutQuad 应明显更大）"
                    + $"  {(half > 1.2f ? "✅ 缓动生效" : "⚠️ 和线性差不多")}");

        // 3) DOTweenSettings 到底在不在、叫什么名字
        DOTween.KillAll(false);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(go2);

        Debug.Log(P + "--- DOTweenSettings 探测 ---");
        var res = Resources.Load("DOTweenSettings");
        Debug.Log(P + $"Resources.Load(\"DOTweenSettings\") = {(res == null ? "null（没建）" : res.GetType().FullName)}");
        var names = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
            .Where(t => t.Name.Contains("DOTweenSettings") || t.Name.Contains("TweenSettings"))
            .Select(t => t.FullName + "  [" + t.Assembly.GetName().Name + "]")
            .Distinct().ToList();
        Debug.Log(P + (names.Count > 0
            ? "找到的类型：" + string.Join(" / ", names)
            : "**整个 AppDomain 里都没有 DOTweenSettings 类型** —— 说明运行时不需要它"));

        Debug.Log(P + "=== 结束 ===");
    }
}
