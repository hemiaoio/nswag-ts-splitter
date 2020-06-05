using System.Collections.Generic;

namespace NSwagTsSplitter
{
    public static class Constant
    {

        public static readonly List<string> IgnoreModules = new List<string>()
        {
            "jQuery"
        };

        public static readonly List<string> UtilitiesModules = new List<string>()
        {
            "throwException",
            "FileParameter",
            "FileResponse",
            "SwaggerException",
        };

        public static readonly List<string> TsBaseType = new List<string>()
        {
            "string","number","Date","undefined","any","boolean","void","{ [key: string]: any; }"
        };


    }
}
