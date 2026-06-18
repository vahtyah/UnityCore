using System;
using UnityEngine;

namespace StandardAssets
{
    /// <summary>
    /// Mô tả một "feature" được mở khoá theo khoảng level (rangeMin..rangeMax).
    /// Gồm hình ảnh, văn bản điều kiện và nội dung hiển thị khi unlock.
    /// </summary>
    [Serializable]
    public class FeatureDefinition
    {
        public string name;

        public int rangeMin;

        public int rangeMax;

        public Sprite sprite;

        public Sprite spriteDark;

        public string conditionText;

        public string unlockTitle;

        public string unlockDescription;
    }
}
