using Activities_Inspector.Constants;
using System.Text;

namespace Activities_Inspector.Utils
{
    public static class EncodingProvider
    {
        private static readonly Encoding AnsiEncoding = CodePagesEncodingProvider.Instance.GetEncoding(AppConstants.Encoding.DefaultCodePage);

        static EncodingProvider()
        {
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static Encoding Ansi => AnsiEncoding;
        public static Encoding Utf8 => Encoding.UTF8;
        public static Encoding Unicode => Encoding.Unicode;

        public static string GetString(byte[] bytes, int index, int count, bool isUnicode)
            => isUnicode ? Unicode.GetString(bytes, index, count) : Ansi.GetString(bytes, index, count);

        public static string GetString(byte[] bytes, int index, int count, Encoding encoding)
            => encoding.GetString(bytes, index, count);
    }
}