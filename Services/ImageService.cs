using System.IO;
using System.Windows.Media.Imaging;

namespace TourAgency2018.Services
{
    public static class ImageService
    {
        public static BitmapImage ToBitmap(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;

            var image = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
            }
            return image;
        }

        public static byte[] ToBytes(BitmapImage image)
        {
            if (image == null) return null;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }
    }
}
