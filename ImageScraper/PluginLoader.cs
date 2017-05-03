using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;

namespace ImageScraper
{
    /// <summary>
    /// プラグインに関する情報
    /// </summary>
    public class PluginInfo
    {
        private string _location;
        private string _className;

        /// <summary>
        /// PluginInfoクラスのコンストラクタ
        /// </summary>
        /// <param name="path">アセンブリファイルのパス</param>
        /// <param name="cls">クラスの名前</param>
        private PluginInfo(string path, string cls)
        {
            this._location = path;
            this._className = cls;
        }

        /// <summary>
        /// アセンブリファイルのパス
        /// </summary>
        public string Location
        {
            get { return _location; }
        }

        /// <summary>
        /// クラスの名前
        /// </summary>
        public string ClassName
        {
            get { return _className; }
        }

        /// <summary>
        /// 有効なプラグインを探す
        /// </summary>
        /// <returns>有効なプラグインのPluginInfo配列</returns>
        public static PluginInfo[] FindPlugins()
        {
            ArrayList plugins = new ArrayList();
            List<string> failedPlugins = new List<string>();
            // IPlugin型の名前
            string ipluginName = typeof(Plugins.PluginInterface).FullName;

            // プラグインフォルダ
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            folder = Path.Combine(folder, "plugins");
            if (!Directory.Exists(folder))
            {
                string msg = "プラグインフォルダが見つかりませんでした";
                MessageBox.Show(msg, "通知",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // .dllファイルを探す
                string[] dlls = Directory.GetFiles(folder, "*.dll");

                foreach (string dll in dlls)
                {
                    try
                    {
                        // アセンブリとして読み込む
                        Assembly asm = Assembly.LoadFrom(dll);
                        foreach (Type t in asm.GetTypes())
                        {
                            // アセンブリ内のすべての型について，プラグインとして有効か調べる
                            if (t.IsClass && t.IsPublic && !t.IsAbstract &&
                                t.GetInterface(ipluginName) != null)
                            {
                                // PluginInfoをコレクションに追加する
                                plugins.Add(new PluginInfo(dll, t.FullName));
                            }
                        }
                    }
                    catch (FileLoadException)
                    {
                        failedPlugins.Add(Path.GetFileName(dll));
                    }
                    catch (ReflectionTypeLoadException) { }
                }

                if (failedPlugins.Count > 0)
                {
                    string msg = "以下のプラグインの読み込みに失敗しました\n";
                    msg += ("-------------------------------------------------------\n");
                    foreach (string fp in failedPlugins)
                        msg += ("\"" + fp + "\"\n");
                    msg += ("-------------------------------------------------------\n");
                    msg += ("該当dllのプロパティから\"ブロックの解除\"を適用してください\n");
                    MessageBox.Show(msg, "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // コレクションを配列にして返す
            return (PluginInfo[])plugins.ToArray(typeof(PluginInfo));
        }

        /// <summary>
        /// プラグインクラスのインスタンスを作成する
        /// </summary>
        /// <returns>プラグインクラスのインスタンス</returns>
        public Plugins.PluginInterface CreateInstance()
        {
            try
            {
                // アセンブリを読み込む
                Assembly asm = Assembly.LoadFrom(this.Location);
                // クラス名からインスタンスを作成する
                Plugins.PluginInterface plugin = (Plugins.PluginInterface)asm.CreateInstance(this.ClassName);
                return plugin;
            }
            catch
            {
                return null;
            }
        }
    }
}
