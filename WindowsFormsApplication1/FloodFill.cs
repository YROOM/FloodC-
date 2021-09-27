using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 定义一个类：填充栅格像素类
    /// </summary>
    class FloodFill
    {
        /// <summary>
        /// 变量：中心点，左间距，右间距
        /// </summary>
        public int Centeridx, leftinterval, rightinterval;
        /// <summary>
        /// 河流长度，总的淹没面积
        /// </summary>
        public double Len, TotalSquare;
        public int[] Demtest;
        public int DemCount;

    }
}
