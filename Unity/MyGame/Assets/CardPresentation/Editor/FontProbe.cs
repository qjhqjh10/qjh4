// FontProbe.cs — 卡面渲染探针：把几张卡的卡面拼成一张图看效果
//
// 卡面是程序生成的黑卡白字（`CardView.FaceTexture`），文字走自写的 5×7 点阵 ASCII 字库。
// 这个探针把测试卡组里挑几张渲出来，检查版面（费用/卡名/插图位/关键词/三围）有没有排对。
//
// 用法：-executeMethod FontProbe.Run -logFile -
using System.IO;
using CardPresentation;
using UnityEditor;
using UnityEngine;

public static class FontProbe
{
    const string P = "FONTPROBE ";
    const string OutDir = @"d:/4/_tmp_view/cardface";

    [MenuItem("Tools/CardPresentation/卡面渲染探针")]
    public static void Run()
    {
        Directory.CreateDirectory(OutDir);

        var ember = new Color(0.85f, 0.38f, 0.25f);
        var tide = new Color(0.28f, 0.55f, 0.85f);

        var cards = new[]
        {
            new CardData { title = "Ember Warlord", cost = -1, melee = 2, ranged = 0, health = 30,
                           keywords = "", isUnit = true, frame = ember },
            new CardData { title = "Scavenger", cost = 1, melee = 2, ranged = 0, health = 2,
                           keywords = "", isUnit = true, frame = ember },
            new CardData { title = "Bulwark", cost = 1, melee = 1, ranged = 0, health = 4,
                           keywords = "VANGUARD", isUnit = true, frame = ember },
            new CardData { title = "Longbowman", cost = 3, melee = 1, ranged = 3, health = 3,
                           keywords = "LONG RANGE", isUnit = true, frame = ember },
            new CardData { title = "Molten Colossus", cost = 7, melee = 8, ranged = 0, health = 8,
                           keywords = "ARMOUR 2", isUnit = true, frame = ember },
            new CardData { title = "Reef Guard", cost = 2, melee = 0, ranged = 0, health = 7,
                           keywords = "VANGUARD", isUnit = true, frame = tide },
            new CardData { title = "Ballista", cost = 3, melee = 0, ranged = 4, health = 3,
                           keywords = "LONG RANGE", isUnit = true, frame = tide },
            new CardData { title = "Abyss Titan", cost = 7, melee = 9, ranged = 0, health = 9,
                           keywords = "", isUnit = true, frame = tide },
        };

        // 每张卡面 256×358，横排 4 张一行
        const int CW = 256, CH = 358, Cols = 4;
        int rows = (cards.Length + Cols - 1) / Cols;
        int W = CW * Cols, H = CH * rows;
        var canvas = new Color32[W * H];
        for (int i = 0; i < canvas.Length; i++) canvas[i] = new Color32(40, 42, 50, 255);

        for (int i = 0; i < cards.Length; i++)
        {
            var tex = CardFaceFor(cards[i]);
            var src = tex.GetPixels32();
            int ox = (i % Cols) * CW;
            int oy = (rows - 1 - i / Cols) * CH;      // 画布 row 0 在底部
            for (int y = 0; y < CH; y++)
                for (int x = 0; x < CW; x++)
                {
                    var s = src[y * CW + x];
                    if (s.a == 0) continue;            // 圆角外不填
                    canvas[(oy + y) * W + ox + x] = s;
                }
        }

        var outTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        outTex.SetPixels32(canvas);
        outTex.Apply();
        File.WriteAllBytes(Path.Combine(OutDir, "faces.png"), outTex.EncodeToPNG());
        Object.DestroyImmediate(outTex);

        Debug.Log(P + $"{cards.Length} 张卡面 → {OutDir}/faces.png  ({W}×{H})");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    /// <summary>借 CardView 的静态卡面生成器（造一个临时物体，取完贴图就销毁）</summary>
    static Texture2D CardFaceFor(CardData d)
    {
        var go = new GameObject("probe");
        var view = CardView.Create(go.transform, d, "probe");
        var tex = view.GetComponent<MeshRenderer>().sharedMaterial.mainTexture as Texture2D;
        Object.DestroyImmediate(go);
        return tex;
    }
}
