using UnityEngine;

namespace Bubbles
{
    [CreateAssetMenu(menuName = "Bubbles/Play tuning")]
    public sealed class BubbleTuning : ScriptableObject
    {
        [Header("入力（Web版はCSSピクセル）")]
        public float holdSeconds = .28f, tapSeconds = .28f, movePixels = 12, settingsHoldSeconds = .75f;
        [Header("シャボン玉（画面ピクセル）")]
        [UnityEngine.Serialization.FormerlySerializedAs("initialCount")]
        public int refillMaximumCount = 3;
        public int maximumCount = 100;
        public float minimumRadius = 19, maximumDiameterWidthFraction = 2f / 3, growthPerSecond = 72, autoSpawnSeconds = 7;
        [Header("風と慣性")]
        public float windRadiusScreenFraction = .25f, windGain = .38f, maximumSpeed = 310, drag = 1.25f, edgeRestitution = .55f;
        public float windResponse = 4;
        [Header("タップへの応答")]
        public float tapSquash = .24f, tapDecay = 1.8f, tapFrequency = 18;
        [Header("待機中の揺れ")]
        public float idleSwayPixels = 10;
    }
}
