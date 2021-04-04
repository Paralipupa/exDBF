using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace exDBF
{
    static class StringExtention
    {
        /// <summary>
        /// Поиск всех вхождений подстроки в строку
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static List<int> AllIndexesOf(this string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                MessageBox.Show("the string to find may not be empty", "value");

            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }
        /// <summary>
        /// Поиск любого вхождения коллекции подстрок в строку
        /// </summary>
        /// <param name="str"></param>
        /// <param name="lists"></param>
        /// <returns></returns>
        public static int IndexOfAny(this string str, string[] lists)
        {
            int index = -1;
            foreach (string item in lists)
            {
                index = str.ToLower().IndexOf(item.ToLower());
                if (index != -1)
                {
                    break;
                }
            }
            return index;
        }
    }
}
