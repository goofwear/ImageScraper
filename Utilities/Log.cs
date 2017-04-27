namespace Utilities
{
    public class Log
    {
        public string ModuleName;
        public string Description;

        public Log(string module, string desc)
        {
            ModuleName = module;
            Description = desc;
        }

        public string[] ToArray()
        {
            return new string[] { ModuleName, Description };
        }
    }
}
