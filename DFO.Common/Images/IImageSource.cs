using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO.Common.Images
{
    public interface IImageSource : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imgPath">The Npk Path of the .img file, WITHOUT the leading sprite/</param>
        /// <param name="frameIndex"></param>
        /// <returns></returns>
        /// <exception cref="System.IO.FileNotFoundException">The img file does not exist in this .npk file
        /// or no frame with the given index exists in the img file.</exception>
        Image GetImage(NpkPath imgPath, int frameIndex);
    }
}
