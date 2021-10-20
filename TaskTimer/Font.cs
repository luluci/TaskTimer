using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;

namespace TaskTimer
{
    class Font
    {
        public FontItem[] FontList;
        public int FontIndex = 0;

        public string Select
        {
            get { return FontList[FontIndex].LocalFontName; }
        }

        public Font(string selectFont)
        {
            int idx = 0;
            var lang = XmlLanguage.GetLanguage(Thread.CurrentThread.CurrentCulture.Name);
            var fonts = Fonts.SystemFontFamilies;
            FontList = Fonts.SystemFontFamilies
            .Select((i) => {

                return new FontItem() { FontFamily = i, LocalFontName = i.Source };
            })
            .Where((i) =>
            {
                bool check = false;

                foreach (var f in i.FontFamily.FamilyNames)
                {
                    if (f.Key == lang)
                    {
                        check = true;
                        i.LocalFontName = f.Value;

                        if (f.Value.Equals(selectFont))
                        {
                            FontIndex = idx;
                        }

                        idx++;
                    }

                }

                return check;
            })
            .ToArray();
        }
    }

    class FontItem
    {
        public FontFamily FontFamily { get; set; }  //フォントファミリー
        public string LocalFontName { get; set; }   //フォント名
    }
}
