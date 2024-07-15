using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NSwagTsSplitter.Utils;

public class StringHelper
{
    public static bool IsJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();
        if ((input.StartsWith("{") && input.EndsWith("}")) || // 对象
            (input.StartsWith("[") && input.EndsWith("]"))) // 数组
        {
            try
            {
                var obj = JToken.Parse(input);
                return true;
            }
            catch (JsonReaderException)
            {
                // 不是JSON格式
                return false;
            }
        }

        return false;
    }
}