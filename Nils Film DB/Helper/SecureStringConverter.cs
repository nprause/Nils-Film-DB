using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Nils_Film_DB.Helper
{
    static class SecureStringConverter
    {
        // This method uses a pointer to unmanaged memory to allow a secure removal of the data from memory. 
        public static string SecureStringToString(SecureString securePassword)
        {
            if (securePassword != null)
            {
                IntPtr unmanagedString = IntPtr.Zero;
                try
                {
                    unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                    return Marshal.PtrToStringUni(unmanagedString);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }
            else return "";
        }

        public static SecureString StringToSecureString(string unsecurePassword)
        {
            if (unsecurePassword != null)
            {
                SecureString securePassword = new SecureString();
                foreach (char c in unsecurePassword)
                {
                    securePassword.AppendChar(c);
                }
                return securePassword;              
            }
            else return null;
        }
    }
}
