using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MakeReadyExcel.Helpers
{
    internal class ImageConverter : AxHost
    {
        private ImageConverter() : base(null) { }

        public static stdole.IPictureDisp Convert(Image image)
        {
            return (stdole.IPictureDisp)GetIPictureDispFromPicture(image);
        }
    }
}
