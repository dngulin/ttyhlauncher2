using System;
using System.Runtime.InteropServices;

namespace TtyhLauncher.GTK {
    public static class Tr {
        public static void InitCatalog(string domain, string directory) {
            setlocale(6, ""); // LC_ALL
            
            bindtextdomain(domain, directory);
            bind_textdomain_codeset(domain, "UTF-8");
            textdomain(domain);
        }
        
        public static string _(string msg) {
            using (var cStrMsg = new CStringHolder(msg)) {
                var ptr = gettext(cStrMsg.Ptr);
            
                if (ptr == cStrMsg.Ptr)
                    return cStrMsg.Str;

                // The resulting string is statically allocated and must not be modified or freed
                return Marshal.PtrToStringAnsi(ptr);
            }
        }

        public static string _n(string msgSingle, string msgPlural, ulong n) {
            using (var cStrMsgSingle = new CStringHolder(msgSingle))
            using (var cStrMsgPlural = new CStringHolder(msgPlural)) {
                var ptr = ngettext(cStrMsgSingle.Ptr, cStrMsgPlural.Ptr, n);
            
                if (ptr == cStrMsgSingle.Ptr)
                    return cStrMsgSingle.Str;
            
                if (ptr == cStrMsgPlural.Ptr)
                    return cStrMsgPlural.Str;
            
                // The resulting string is statically allocated and must not be modified or freed
                return Marshal.PtrToStringAnsi(ptr);
            }
        }
        
        private class CStringHolder : IDisposable {
            public readonly string Str;
            public readonly IntPtr Ptr;

            private bool _released;

            public CStringHolder(string str) {
                Str = str;
                Ptr = Marshal.StringToHGlobalAnsi(str);
            }

            public void Dispose() {
                if (_released)
                    return;
        
                _released = true;
                Marshal.FreeHGlobal(Ptr);
            }
        }

        [DllImport("libc", CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr setlocale(int category, string locale);

        [DllImport("libc", CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr bindtextdomain(string domain, string directory);
        
        [DllImport("libc", CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr bind_textdomain_codeset(string domain, string encoding);
        
        [DllImport("libc", CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr textdomain(string domain);
        
        [DllImport("libc", CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr gettext(IntPtr text);

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ngettext(IntPtr text, IntPtr pluralText, ulong n);
    }
}