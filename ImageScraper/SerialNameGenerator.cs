using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImageScraper
{
    public class SerialNameGenerator
    {
        int digits;
        int initialMax;
        string oldDir;
        string formatPattern;
        string tagName;

        public SerialNameGenerator(string tagName, int digits, string formatPattern)
        {
            this.initialMax = -1;
            this.oldDir = "";
            this.formatPattern = formatPattern;
            this.tagName = tagName;
            this.digits = digits;
        }

        public string Generate(string dir)
        {
            if (this.oldDir != dir)
            {
                string pattern = String.Format(@"(?i)_(\d{{{0}}})\.({1})$", this.digits, this.formatPattern);
                DirectoryInfo di = new DirectoryInfo(dir);

                initialMax = di.GetFiles(this.tagName + "_*.*")			// パターンに一致するファイルを取得する
                    .Select(fi => Regex.Match(fi.Name, pattern))		// ファイルの中で数値のものを探す
                    .Where(m => m.Success)                              // 該当するファイルだけに絞り込む
                    .Select(m => Int32.Parse(m.Groups[1].Value))        // 数値を取得する
                    .DefaultIfEmpty(0)                                  // １つも該当しなかった場合は 0 とする
                    .Max();                                             // 最大値を取得する
            }
            string serial = (++initialMax).ToString().PadLeft(this.digits, '0');
            string fileName = this.tagName + "_" + serial;

            return fileName;
        }
    }
}
