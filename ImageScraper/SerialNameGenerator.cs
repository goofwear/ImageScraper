using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImageScraper
{
    public class SerialNameGenerator
    {
        int mDigits;
        int mInitialMax;
        string mOldDirectory;
        string mFormatPattern;
        string mTagName;

        public SerialNameGenerator(string tagName, int digits, string [] formats)
        {
            this.mInitialMax = -1;
            this.mOldDirectory = "";
            this.mFormatPattern = String.Join("|", formats);
            this.mTagName = tagName;
            this.mDigits = digits;
        }

        public string Generate(string dir)
        {
            if (this.mOldDirectory != dir)
            {
                string pattern = String.Format(@"(?i)_(\d{{{0}}})\.({1})$", mDigits, mFormatPattern);
                DirectoryInfo di = new DirectoryInfo(dir);

                mInitialMax = di.GetFiles(mTagName + "_*.*")			// パターンに一致するファイルを取得する
                    .Select(fi => Regex.Match(fi.Name, pattern))		// ファイルの中で数値のものを探す
                    .Where(m => m.Success)                              // 該当するファイルだけに絞り込む
                    .Select(m => Int32.Parse(m.Groups[1].Value))        // 数値を取得する
                    .DefaultIfEmpty(0)                                  // １つも該当しなかった場合は 0 とする
                    .Max();                                             // 最大値を取得する
            }
            string serial = (++mInitialMax).ToString().PadLeft(mDigits, '0');
            string fileName = mTagName + "_" + serial;

            return fileName;
        }
    }
}
