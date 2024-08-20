using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrimsonStainedLands
{
    class SHA384
    {

        public static string ComputeHash(string data)
        {

            var SHA384 = System.Security.Cryptography.SHA384.Create();
            var hash = SHA384.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(data));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
