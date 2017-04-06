using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ImageScraper
{
    public class Common
    {
        [Serializable]
        public struct KeyAndValue<TKey, TValue>
        {
            public TKey Key;
            public TValue Value;

            public KeyAndValue(KeyValuePair<TKey, TValue> pair)
            {
                Key = pair.Key;
                Value = pair.Value;
            }
        }

        public static List<KeyAndValue<TKey, TValue>>
            ConvertDictionaryToList<TKey, TValue>(Dictionary<TKey, TValue> dic)
        {
            List<KeyAndValue<TKey, TValue>> lst =
                new List<KeyAndValue<TKey, TValue>>();
            foreach (KeyValuePair<TKey, TValue> pair in dic)
            {
                lst.Add(new KeyAndValue<TKey, TValue>(pair));
            }
            return lst;
        }

        public static Dictionary<TKey, TValue>
            ConvertListToDictionary<TKey, TValue>(List<KeyAndValue<TKey, TValue>> lst)
        {
            Dictionary<TKey, TValue> dic = new Dictionary<TKey, TValue>();
            foreach (KeyAndValue<TKey, TValue> pair in lst)
            {
                dic.Add(pair.Key, pair.Value);
            }
            return dic;
        }

        /// 文字列中の指定した文字を全て"+"に置き換える
        public static string RemoveChars(string s, char[] characters)
        {
            StringBuilder buf = new StringBuilder(s);
            foreach (char c in characters)
            {
                buf.Replace(c.ToString(), "+");
            }
            return buf.ToString();
        }

        public static void WriteLineList(List<string> list)
        {
            foreach(var value in list)
            {
                Console.WriteLine(value);
            }
        }

        public static void WriteLineUrlContainers(List<UrlContainer.UrlContainer> list)
        {
            foreach (var value in list)
            {
                Console.WriteLine(value.Url);
            }
        }

        public static string GetImageFormatString(string path)
        {
            Bitmap bitmap = new Bitmap(path);
            var decoders = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders();

            foreach (var ici in decoders)
            {
                if (ici.FormatID == bitmap.RawFormat.Guid)
                {
                    bitmap.Dispose();
                    switch (ici.FormatDescription)
                    {
                        case "BMP":
                            return ".bmp";
                        case "JPEG":
                            return ".jpg";
                        case "GIF":
                            return ".gif";
                        case "TIFF":
                            return ".tiff";
                        case "PNG":
                            return ".png";
                        default:
                            break;
                    }
                }
            }
            return "";
        }

        public static bool IsEmptyDirectory(string dir)
        {
            if (!Directory.Exists(dir))
            {
                // ディレクトリが存在しなければ空でないとする
                return false;
            }
            try
            {
                string[] entries = Directory.GetFileSystemEntries(dir);
                return entries.Length == 0;
            }
            catch
            {
                // アクセス権がないなどの場合は空でないとする
                return false;
            }
        }

        public static void OpenExplorer(string path)
        {
            if (File.Exists(path) == true)
            {
                string cmd = String.Format(@"/select,""{0}""", path);
                System.Diagnostics.Process.Start("explorer", cmd);
            }
            else
            {
                MessageBox.Show("ディレクトリが存在しません", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void OpenFile(string path)
        {
            if (File.Exists(path) == true)
            {
                string cmd = String.Format(@"""{0}""", path);
                System.Diagnostics.Process.Start(cmd);
            }
            else
            {
                MessageBox.Show("ファイルが存在しません", "エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
