using System.Text.RegularExpressions;

namespace UrlContainer
{
    public class UrlParser
    {
        string _url;
        string _rawurl;
        string _scheme;
        string _localpath;
        string _authority;
        string _filename;
        string _extension;

        public UrlParser(string url)
        {
            this._url = url;
        }

        public string RawUrl
        {
            get
            {
                if (_rawurl == null)
                {
                    _rawurl = "";
                    Regex re = new Regex(@"^(?<Url>.+?)(#.*)?$");
                    Match m = re.Match(_url);

                    if (m.Success == true)
                        _rawurl = m.Groups["Url"].Value;
                }
                return _rawurl;
            }
        }

        public string Scheme
        {
            get
            {
                if (_scheme == null)
                {
                    _scheme = "";
                    Regex re = new Regex(@"^(\w+)://");
                    Match m = re.Match(_url);

                    if (m.Success == true)
                        _scheme = m.Groups["Scheme"].Value;
                }
                return _scheme;
            }
        }

        public string LocalPath
        {
            get
            {
                if (_localpath == null)
                {
                    _localpath = "";
                    Regex re = new Regex(@"//(?<Path>[^/]+.*?)[^/]*$");
                    Match m = re.Match(_url);

                    if (m.Success == true)
                        _localpath = m.Groups["Path"].Value;
                }
                return _localpath;
            }
        }

        public string Authority
        {
            get
            {
                if (_authority == null)
                {
                    _authority = "";
                    Regex re = new Regex(@"//(?<Domain>[^/]+)");
                    Match m = re.Match(_url);

                    if (m.Success == true)
                        _authority = m.Groups["Domain"].Value;
                }
                return _authority;
            }
        }

        public string FileName
        {
            get
            {
                if (_filename == null)
                {
                    _filename = "";
                    Regex re = new Regex(@".+/(?<FileName>.+?)([#\?].*)?$");
                    Match m = re.Match(_url);

                    if (m.Success == true)
                        _filename = m.Groups["FileName"].Value;
                }
                return _filename;
            }
        }

        public string Extension
        {
            get
            {
                if (_extension == null)
                {
                    _extension = "";
                    Regex re = new Regex(@".+?(?<Ext>\.[^\.].+)?$");

                    if (FileName != null)
                    {
                        Match m = re.Match(FileName);
                        if (m.Success == true)
                            _extension = m.Groups["Ext"].Value.Replace(".", "");
                    }
                }
                return _extension;
            }
        }
    }
}
