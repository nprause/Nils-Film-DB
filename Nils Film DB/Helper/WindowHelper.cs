using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Nils_Film_DB.Helper
{
    //
    // Provides methods to open and close UI Windows
    //

    class WindowHelper
    {

        Dictionary<int, Window> win = new Dictionary<int, Window>();

        public int Open(object source, int w = -1, int h = -1, bool resize = true)
        {
            int key = 0;
            while (win.ContainsKey(key))
                ++key;
            win.Add(key, new Window());
            win[key].Content = source;           
            if ( w >= 0 ) win[key].Width = w;
            if (h >= 0) win[key].Height = h;
            if (w<0 && h<0)
                win[key].SizeToContent = SizeToContent.WidthAndHeight;
            if (!resize)
            {
                win[key].ResizeMode = ResizeMode.NoResize;
                win[key].SizeToContent = SizeToContent.WidthAndHeight;
            }
            //win.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;          
            win[key].Show();
            return key;
        }

        public void Close(int key)
        {
            if (win.ContainsKey(key))
            {
                win[key].Close();
                win.Remove(key);
            }
        }
    }
}
