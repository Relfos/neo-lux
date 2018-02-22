using System;

namespace NeoLux
{
    public static class LuxUtils
    {
        public static string reverseHex(string hex)
        {

            string result = "";
            for (var i = hex.Length - 2; i >= 0; i -= 2)
            {
                result += hex.Substring(i, 2);
            }
            return result;
        }

        public static string num2fixed8(decimal num)
        {
            long val = (long)Math.Round(num * 100000000);
            var hexValue = val.ToString("X16");
            return reverseHex(("0000000000000000" + hexValue).Substring(hexValue.Length));
        }
    }
}
