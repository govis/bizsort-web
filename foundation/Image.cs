using System;
using BizSrt.Model;

namespace BizSrt.Foundation.Entity
{
    public static class Image
    {
        public readonly struct ImageMetadata
        {
            private readonly byte[] _metadata;

            public ImageMetadata(byte[] metadata)
            {
                _metadata = metadata;
            }

            public ushort Width => _metadata != null && _metadata.Length >= 3 ? BitConverter.ToUInt16(_metadata, 1) : (ushort)0;
            public ushort Height => _metadata != null && _metadata.Length >= 5 ? BitConverter.ToUInt16(_metadata, 3) : (ushort)0;
        }

        public static ImageSizeType ResolveSize(ImageEntity entity, byte[] imageMetadata)
        {
            if (imageMetadata == null || imageMetadata.Length < 5) return ImageSizeType.None;

            var meta = new ImageMetadata(imageMetadata);
            var width = meta.Width;
            var height = meta.Height;

            if (width == 0 || height == 0) return ImageSizeType.None;

            // Mapped thresholds to modern ImageSizeType enum
            const double threshold = 0.8;

            if (width >= 800 * threshold) return ImageSizeType.View;
            if (width >= 400 * threshold) return ImageSizeType.Card;
            if (width >= 150 * threshold) return ImageSizeType.List;
            if (width >= 48 * threshold)  return ImageSizeType.Icon;

            return ImageSizeType.None; // Unknown or too small
        }
    }
}
