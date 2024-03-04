using System;
using System.Runtime.InteropServices;

namespace ImageCvt
{
    internal static class SHELL32
    {
        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shparsedisplayname
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern void SHParseDisplayName(string name, IntPtr bindingContext, out IntPtr pidl,
            uint sfgaoIn, out uint psfgaoOut);

        /// <summary>
        /// http://www.pinvoke.net/default.aspx/shell32/SHOpenFolderAndSelectItems.html
        /// https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shopenfolderandselectitems
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, IntPtr[] apidl,
            uint dwFlags);
    }
}
