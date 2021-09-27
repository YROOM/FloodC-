using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 洪水的一些设置
    /// </summary>
    class floodsettings
    {
        /// <summary>
        /// 声明一个高程索引类型的集合
        /// </summary>
        protected List<ElevationIdx> _elevationidx;
        /// <summary>
        /// 中心格子的索引
        /// </summary>
        protected int _centeridx;

        /// <summary>
        /// 单个长度
        /// </summary>
        public double PerLen;

        /// <summary>
        /// 所有的高程索引的信息
        /// </summary>
        public List<ElevationIdx> AllElevationIdxInfo
        {
            get
            {
                return _elevationidx;
            }
        }
        /// <summary>
        /// 获取左边最大值的信息
        /// </summary>
        public ElevationIdx MaxLeftInfo
        {
            get
            {
                return _elevationidx.FindAll(e => e.Direct == -1).OrderByDescending(e => e.WaterElevation).FirstOrDefault();
            }
        }
        /// <summary>
        /// 获取右边最大值的信息
        /// </summary>
        public ElevationIdx MaxRightInfo
        {
            get
            {
                return _elevationidx.FindAll(e => e.Direct == 1).OrderByDescending(e => e.WaterElevation).FirstOrDefault();
            }
        }
        /// <summary>
        /// 获取左边最大值的索引
        /// </summary>
        public int MaxLeftIdx
        {
            get
            {
                return MaxLeftInfo.Idx;
            }
        }
        /// <summary>
        /// 获取右边最大值的索引
        /// </summary>
        public int MaxRightIdx
        {
            get
            {
                return MaxRightInfo.Idx;
            }
        }
        /// <summary>
        /// 获取左边最大值的水位
        /// </summary>
        public double MaxLeftElevation
        {
            get
            {
                return MaxLeftInfo.WaterElevation;
            }
        }
        /// <summary>
        /// 获取右边最大值的水位
        /// </summary>
        public double MaxRightElevation
        {
            get
            {
                return MaxRightInfo.WaterElevation;
            }
        }
        /// <summary>
        /// 获取装有短索引的集合
        /// </summary>
        public List<ElevationIdx> ShortIdx
        {
            get
            {
                List<ElevationIdx> result = new List<ElevationIdx>();
                //如果左右最大的高程值相等
                if(MaxRightElevation==MaxLeftElevation)
                {
                    result.Add(MaxLeftInfo);
                    result.Add(MaxRightInfo);
                }
                else
                {
                    //加入左右点高程值中小的那一个
                    result.Add(MaxRightElevation < MaxLeftElevation ? MaxRightInfo : MaxLeftInfo);
                }
                return result;
            }
        }
        /// <summary>
        /// 获取淹没的面积
        /// </summary>
        public double FullSquare
        {
            get
            {
                double result = 0;
                foreach (ElevationIdx eleidx in _elevationidx)
                {
                    if (eleidx.WaterElevation < MaxElevation)
                        result += (MaxElevation - eleidx.WaterElevation) * PerLen;
                }
                return result;
            }
             
        }
        /// <summary>
        /// floodsettings的无参构造函数
        /// </summary>
        public floodsettings() { }
        /// <summary>
        /// floodsettings的有参构造函数
        /// </summary>
        /// <param name="perlen"></param>
        /// <param name="centeridx"></param>
        /// <param name="centerelevation"></param>
        /// <param name="centerwaterheight"></param>
        public floodsettings(double perlen,int centeridx,double centerelevation,double centerwaterheight)
        {
            
            PerLen = perlen;
            _elevationidx = new List<ElevationIdx>();
            _elevationidx.Add(new ElevationIdx() { 
                Direct=0,
                Idx=centeridx,
                Elevation = centerelevation,
                WaterHeight=centerwaterheight
            });
            _centeridx = centeridx;
           
        }
        /// <summary>
        /// 添加索引
        /// </summary>
        /// <param name="addidx">要添加的索引</param>
        /// <param name="elevation">高程</param>
        /// <param name="waterheight">水位</param>
        public void Addidx(int addidx,double elevation,double waterheight)
        {           
            _elevationidx.Add(new ElevationIdx()
            {
                Idx = addidx,
                Elevation = elevation,
                //添加的索引小于中心点索引赋值为-1，大于则赋值为1
                Direct=addidx<_centeridx?-1:1,
                WaterHeight=waterheight
            });
            //根据高程值中最小的值获取索引
            double max1 = _elevationidx.Min(e => e.Elevation);
            double max2 = max1;
            try
            {
                max1 = _elevationidx.FindAll(e => e.Direct == 1).Max(e => e.Elevation);
            }
            catch (Exception ee) { }
            try
            {
                max2 = _elevationidx.FindAll(e => e.Direct == -1).Max(e => e.Elevation);
            }
            catch (Exception ee) { }
            
          
          
        }
        /// <summary>
        /// 获取索引集合
        /// </summary>
        /// <returns></returns>
        public List<int> GetIdxs()
        {
            return _elevationidx.Select(e => e.Idx).ToList();
        }
        /// <summary>
        /// 获取最大的高程值
        /// </summary>
        public double MaxElevation
        {
            get
            {
                return MaxLeftElevation < MaxRightElevation ? MaxLeftElevation : MaxRightElevation;//最大值由最短边决定
            }
        }

        /// <summary>
        /// 获取高度
        /// </summary>
        /// <param name="square">总面积</param>
        /// <returns></returns>
        public double GetHeight(double square)
        {
            double result = 0;
            List<ElevationIdx> elevationids = _elevationidx.OrderBy(e => e.WaterElevation).ToList();
            double nowsquare = 0;
           
            for(int i=1;i<elevationids.Count;i++)
            {
                double nowheight = (elevationids[i].WaterElevation - elevationids[i - 1].WaterElevation);
                double newsquare =nowheight* i*PerLen;
                if(newsquare+nowsquare>square)
                {
                    result = (square - nowsquare) * nowheight / newsquare + elevationids[i - 1].WaterElevation;
                    break;
                }
                nowsquare += newsquare;
            }
            return result;
        }
       
    }

  /// <summary>
  /// 高程索引类
  /// </summary>
    public class ElevationIdx
    {
        public int Idx;//初步确定：像素栅格的编号，索引
        public double Elevation; //地形的高程值
        public int Direct;//0代表自己,1代表大于中心点，-1代表小于中心点
        public double WaterHeight;//水的高度
        //获取水位
        public double WaterElevation
        {
            get
            {
                //获取地形高程+水深，即当前水位
                return Elevation + WaterHeight;
            }
        }
    }
   
}
