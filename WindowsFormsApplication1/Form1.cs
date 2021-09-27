using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;

using Oracle.DataAccess.Client;
using System.Threading;


namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// m_XSize：图像的宽度（列数），m_YSize：图像的高度（行数）
        /// </summary>
        int m_XSize, m_YSize;
        /// <summary>
        /// 影像坐标变化参数
        /// </summary>
        double[] m_adfGeoTransform = new double[6];
        /// <summary>
        /// 投影
        /// </summary>
        string projection;
        /// <summary>
        /// 缓冲区，按行优先存储
        /// </summary>
        double[] m_FloodBuffer;
        //存放大坝经纬度坐标，可以是多个种子点
        List<PoiD> dabapois = new List<PoiD>();
        double peradddem = 2;//设定死的每秒单个格子上升多少m
        double perextend = 1;//设定死一次扩充1个格子
        //窗体无参构造函数
        public Form1()
        {
            //初始化窗体
            InitializeComponent();
            //修改此处的大坝口坐标值
            //此处是大坝位置坐标（x,y）(经度，纬度)
            //dabapois.Add(new PoiD(575523.443,3286402.631));
            //以下为假数据
            //屯溪的点
            dabapois.Add(new PoiD(118.3315,29.709083));
          //  dabapois.Add(new PoiD(118.307448, 29.714445));
            //dabapois.Add(new PoiD(628115.131, 3287011.174));
           // dabapois.Add(new PoiD(118.307448,29.714445));

        }
        /// <summary>
        /// 窗体加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                OSGeo.GDAL.Gdal.AllRegister();
                //屯溪 30m DEM tif图片所在文件位置
                //OSGeo.GDAL.Dataset dataSet = OSGeo.GDAL.Gdal.Open(@"C:\Users\yunso\Desktop\论文相关\实验\最新-屯溪 DEM数据30m 90m 1km tiff文件及asc文件\30m\处理完成\TIFF文件\30m.tif", Access.GA_ReadOnly);
                //屯溪 90m DEM tif图片所在文件位置
                //OSGeo.GDAL.Dataset dataSet = OSGeo.GDAL.Gdal.Open(@"C:\Users\yunso\Desktop\论文相关\实验\最新-屯溪 DEM数据30m 90m 1km tiff文件及asc文件\90m\处理完成\TIFF文件\90m.tif", Access.GA_ReadOnly);
                //屯溪 1km DEM tif图片所在文件位置
                OSGeo.GDAL.Dataset dataSet = OSGeo.GDAL.Gdal.Open(@"E:\2021毕业\屯溪流域\屯溪DEM数据Z\30m\处理完成\TIFF文件\30m.tif", Access.GA_ReadOnly);
                //列数
                m_XSize = dataSet.RasterXSize;
                Console.WriteLine("m_XSize:" + m_XSize);
                Console.WriteLine("-----------------------------------");
                //行数
                m_YSize = dataSet.RasterYSize;
                Console.WriteLine("m_YSize:" + m_YSize);
                //波段数即图像每个像素点所含的颜色种类，物理中的光学中学过颜色就是某频率的光波。波段少则一个，多则很多个，在遥感影像中波段通常有多个。
                Console.WriteLine("总的波段数："+dataSet.RasterCount);
                m_FloodBuffer = new double[m_XSize * m_YSize];
            
               dataSet.GetRasterBand(1).ReadRaster(0, 0, m_XSize, m_YSize, m_FloodBuffer, m_XSize, m_YSize, 0, 0);
               Console.WriteLine(dataSet.GetRasterBand(1).GetUnitType());
                Console.WriteLine("80行");
                Console.WriteLine("m_adfGeoTransform: "+m_adfGeoTransform);
                Console.WriteLine("81行");
               dataSet.GetGeoTransform(m_adfGeoTransform);
                //参考系
                projection=dataSet.GetProjection();
                Console.WriteLine("projection: "+projection);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            } 
        }


        private void button1_Click(object sender, EventArgs e)
        {
            /*foreach(double i in m_FloodBuffer)
            {
                if(i == -9999.0)
                {
                    continue;
                }
                Console.WriteLine(i);
            }*/
            //实例化一个计时器
            Stopwatch watch = new Stopwatch();
            //開始计时
            watch.Start();
            double floodheight = Convert.ToDouble(textBox1.Text);
            for (int i = 90; i <= Convert.ToInt16(floodheight); i++)
            {
               FloodWithoutSeedPoint(i,"1km");
                FloodFill8Direct(i,"1km");
                Console.WriteLine("水位：" + i);
            }      
            //FloodFill8Direct(floodheight); 
            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            Console.WriteLine("Time elapse:" + watch.Elapsed);
            MessageBox.Show("创建完毕");
        }
     
        /// <summary>
        /// 获取某点的索引
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private int getIndex(PointEleva point)
        {
            return point.Y * m_XSize + point.X;
        }
        private int getIndex(NewPoint point)
        {
            return point.Y * m_XSize + point.X;
        }
        private double GetElevation(PointEleva m_point)
        {
            int idx = getIndex(m_point);
            if(idx>=m_FloodBuffer.Length||idx<0)
            {
                return -32768;
            }
            return m_FloodBuffer[idx];
        }
        private double GetElevation(NewPoint m_point)
        {
            int idx = getIndex(m_point);
            if (idx >= m_FloodBuffer.Length || idx < 0)
            {
                return -32768;
            }
            return m_FloodBuffer[idx];
        }

        /// <summary>
        /// 无源淹没算法分析
        /// </summary>
        /// <param name="floodLevel">淹没水位</param>
        public void FloodWithoutSeedPoint(double floodLevel, String size)
        {
            double[,] floodDepth;
            //洪水淹没的水位数组，行在前，列在后
            floodDepth = new double[m_YSize, m_XSize];
            //横向初始化数组，外循环控制行，内循环控制列
            for (int i = 0; i < m_YSize; i++)
            {
                for (int j = 0; j < m_XSize; j++)
                {
                    floodDepth[i, j] = -9999;
                }
            }
            //外循环控制行，内循环控制列
            for (int i = 0; i < m_YSize; i++)
            {
                for (int j = 0; j < m_XSize; j++)
                {
                    NewPoint temp_point = new NewPoint();
                    temp_point.X = j;
                    temp_point.Y = i;
                    temp_point.Elevation = GetElevation(temp_point);
                    if(temp_point.Elevation == -32768)
                    {
                        continue;
                    }
                    if (temp_point.Elevation < floodLevel)
                    {
                        floodDepth[temp_point.Y, temp_point.X] = floodLevel - temp_point.Elevation;
                    }
                }
            }
            //设置缓冲数组
            double[] waterbuffer = new double[m_FloodBuffer.Length];
            //对缓冲数组赋值操作
            for (int i = 0; i < m_YSize; i++) //行
                for (int j = 0; j < m_XSize; j++) //列
                {
         
                    waterbuffer[i * m_XSize + j] = floodDepth[i, j];
                }

            writegeotif(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\FloodSimulation\\water_floodNoSeed_" + size + "_" + floodLevel.ToString() + ".tif",
                  waterbuffer, m_adfGeoTransform, m_XSize, m_YSize);

        }


        /// <summary>  
        /// 种子扩散算法淹没分析  
        /// </summary>  
        /// <param name="m_Lat">种子点纬度</param>  
        /// <param name="m_Log">种子点经度</param>  
        /// <param name="m_FloodLevel">淹没水位</param>  
        public void FloodFill8Direct(double m_FloodLevel, String size)
        {
            //淹没缓冲区堆栈
            List<PointEleva> m_FloodBufferList = new List<PointEleva>();
            //淹没区域标记二维数组，用于标记是否可淹没  
            bool[,] IsFlood;
            //洪水的级别
            double[,] IsFloodLevel;
            //洪水淹没区域的水深
            int[,] floodDepth;
            //最大洪水级别
            double maxfloodlevel = 1;
            //实例化是否淹没,boolean类型的数组
            IsFlood = new bool[m_XSize, m_YSize];
            //洪水淹没的级别数组
            IsFloodLevel = new double[m_XSize, m_YSize];
            //洪水淹没区域的水深数组
            floodDepth = new int[m_XSize, m_YSize];
            //横向初始化数组，外循环控制行，内循环控制列
            for (int i=0;i<m_YSize;i++) //75
            {
                for (int j = 0; j < m_XSize; j++)  //100
                {
                    IsFlood[j, i] = false;
                    //floodDepth[j, i] = -32768;
                    floodDepth[j, i] = -9999;
                }
            }
            foreach(PoiD daba in dabapois)
            {
                Console.WriteLine(daba.X);
                Console.WriteLine(daba.Y);
                //首先根据种子点经纬度获取其所在行列号  
                PointEleva pFloodSourcePoint = new PointEleva();
                int col, row, idx;
                geoToImageSpace(m_adfGeoTransform, m_XSize, m_YSize, daba.X, daba.Y, out col, out row, out idx);
                Console.WriteLine("******************************************");
                Console.WriteLine("---idx----:" + idx);
                Console.WriteLine("---x----:" + col);
                Console.WriteLine("---y----:"+row);
                Console.WriteLine("******************************************");
                pFloodSourcePoint.X = col;
                pFloodSourcePoint.Y = row;
                pFloodSourcePoint.IsDaba = true;
            
                pFloodSourcePoint.originPoi = pFloodSourcePoint;
                pFloodSourcePoint.FloodLevel = pFloodSourcePoint.PerDistance;
                //获取种子点高程值 
                //int countimes = 0;
                //foreach(double h in m_FloodBuffer)
                //{
                //    if (h > 0)
                //    {
                //       // Console.WriteLine(h);
                //    }
                //    else
                //    {
                //       // Console.WriteLine(h);
                //        ++countimes;
                //    }
                    
                //}
                //Console.WriteLine("负值："+countimes);
                Console.WriteLine(m_FloodBuffer.Length);
                Console.WriteLine("-************************");
               ///////////////////////////////////////////////////出现问题
                pFloodSourcePoint.Elevation = m_FloodBuffer[(m_XSize * row + col)];
                Console.WriteLine(pFloodSourcePoint.Elevation);
                //////////////////////////////////////////////////
                m_FloodBufferList.Add(pFloodSourcePoint);
            }
            //判断堆栈中是否还有未被淹没点，如有继续搜索，没有则淹没分析结束。  
            while (m_FloodBufferList.Count != 0)
            {
                PointEleva pFloodSourcePoint_temp = m_FloodBufferList[0];
                int colX = pFloodSourcePoint_temp.X;
                int rowY = pFloodSourcePoint_temp.Y;
                bool isdaba = pFloodSourcePoint_temp.IsDaba;
                //标记可淹没,并从淹没堆栈中移出  
                IsFlood[colX, rowY] = true;
                IsFloodLevel[colX, rowY] = pFloodSourcePoint_temp.FloodLevel;
                floodDepth[colX, rowY] = Convert.ToInt16(m_FloodLevel - pFloodSourcePoint_temp.Elevation);
                //m_FloodBuffer[getIndex(pFloodSourcePoint_temp)] = 1;
                m_FloodBufferList.RemoveAt(0);
                ////向中心栅格单元的8个临近方向搜索连通域  
                for (int i = colX - 1; i < colX + 2; i++)
                {
                    for (int j = rowY - 1; j < rowY + 2; j++)
                    {
                        if (Math.Sqrt((i - colX) * (i - colX) + (j - rowY) * (j - rowY)) > 3) continue;
                        //if ((i == rowX - 2 || i == rowX + 2) && j != colmY) continue;
                        //if ((i == rowX - 1 || i == rowX + 1) && (j == colmY - 2 || j == colmY + 2)) continue;
                        if (isdaba && (i + j) <= (colX + rowY))
                            continue;
                        //判断是否到达栅格边界  
                        if (i < m_XSize && i >= 0 && j < m_YSize && j >= 0)
                        {
                            PointEleva temp_point = new PointEleva();
                            temp_point.X = i;
                            temp_point.Y = j;
                            temp_point.Elevation = GetElevation(temp_point);
                            temp_point.IsDaba = false;
                            temp_point.parentPoint = pFloodSourcePoint_temp;
                            if (temp_point.Elevation <= 0)
                                continue;
                            bool isflood = IsFlood[temp_point.X, temp_point.Y];
                            //搜索可以淹没且未被标记的栅格单元  
                            if ((temp_point.Elevation <= m_FloodLevel || temp_point.Elevation <= pFloodSourcePoint_temp.Elevation) && IsFlood[temp_point.X, temp_point.Y] == false)
                           // if (temp_point.Elevation <= m_FloodLevel || temp_point.Elevation <= pFloodSourcePoint_temp.Elevation)
                            {
                                IsFlood[temp_point.X, temp_point.Y] = true;
                                temp_point.IsFlooded = true;
                                PointEleva flagpoi = pFloodSourcePoint_temp;                                
                                PointEleva newflagpoi = pFloodSourcePoint_temp;
                                bool flag = true;
                                while (flag && newflagpoi.FloodLevel > 1)
                                {
                                    flagpoi = newflagpoi;
                                    newflagpoi = flagpoi.originPoi;
                                    flag = PointsCanReturn(IsFlood, new Point(temp_point.X, temp_point.Y), new Point(newflagpoi.X, newflagpoi.Y));  
                                }
                                temp_point.originPoi = flagpoi;                                
                                temp_point.FloodLevel = temp_point.originPoi.FloodLevel + temp_point.PerDistance;
                                //将符合条件的栅格单元加入堆栈，标记为淹没，避免重复运算  
                                m_FloodBufferList.Add(temp_point);
                                IsFloodLevel[temp_point.X, temp_point.Y] = temp_point.FloodLevel;
                                if (temp_point.Elevation < m_FloodLevel)
                                {
                                    floodDepth[temp_point.X, temp_point.Y] = Convert.ToInt16(m_FloodLevel - temp_point.Elevation);
                                }
                                
                                if (IsFloodLevel[temp_point.X, temp_point.Y] > maxfloodlevel)
                                {
                                    maxfloodlevel = IsFloodLevel[temp_point.X, temp_point.Y];
                                }                               
                            }
                        }
                    }
                }             
            }
            //设置缓冲数组
            double[] waterbuffer = new double[m_FloodBuffer.Length];
            //对缓冲数组赋值操作
            for (int i = 0; i < m_XSize; i++)
                for (int j = 0; j < m_YSize; j++)
                {
                    //waterbuffer[j * m_XSize + i] = IsFloodLevel[i, j];
                    waterbuffer[j * m_XSize + i] = floodDepth[i, j];
                }
         
       
            writegeotif(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\FloodSimulation\\floodWithSeed_"+ size + "_" + m_FloodLevel.ToString() +".tif",
                  waterbuffer, m_adfGeoTransform, m_XSize, m_YSize);          
        } 
        /// <summary>
        /// 判断点是否可以返回
        /// </summary>
        /// <param name="isflood">该点是否淹没</param>
        /// <param name="poi1">第一个点</param>
        /// <param name="poi2">第二个点</param>
        /// <returns></returns>
        private bool PointsCanReturn(bool[,] isflood,Point poi1,Point poi2)
        { 
            //点2到点1，X方向上的距离
            int dx = poi2.X - poi1.X;
            //点2到点1，Y方向上的距离
            int dy = poi2.Y - poi1.Y;
            List<Point> poi = new List<Point>();
            //获取微分，即∆x
            int dtx = (dx==0)?0:dx/Math.Abs(dx);
            int dty = (dy==0)?0:dy/Math.Abs(dy);
            //判断点1到点2的路径上的所有点，将点添加到集合中点
            if(Math.Abs(dx)>Math.Abs(dy))
            {
                //初始化第一个待求点的x坐标
                int xmid = poi1.X+dtx;
                while(xmid!=poi2.X)
                {
                    //获取待求点的y坐标
                    int ymid = (xmid - poi1.X) * dy / dx + poi1.Y;
                    //向点集合中添加该点
                    poi.Add(new Point(xmid, ymid));
                    //待求点的x坐标移动一个单位
                    xmid += dtx;
                }
            }
            else
            {
                //初始化第一个待求点的y坐标
                int ymid = poi1.Y + dty;
                while(ymid!=poi2.Y)
                {
                    //获取待求点的x坐标
                    int xmid = (ymid - poi1.Y) * dx / dy + poi1.X;
                    //向点集合中添加该点
                    poi.Add(new Point(xmid, ymid));
                    //待求点的y坐标移动一个单位
                    ymid += dty;
                }               
            }
            //判断集合中的点是否都被标记为true，如果存在没有被标为true的点，则返回false。
            foreach(Point p in poi)
            {
                if(isflood[p.X,p.Y]==false)
                {
                    return false;
                }
            }
            return true;
        }
       /// <summary>
       /// 创建影像
       /// </summary>
       /// <param name="path">路径</param>
       /// <param name="data">数据</param>
       /// <param name="transform">转换参数</param>
       /// <param name="width">宽度</param>
       /// <param name="height">高度</param>
        private void writegeotif(string path,double[] data,double[] transform,int width,int height)
        {
            //在GDAL中创建影像,先需要明确待创建影像的格式,并获取到该影像格式的驱动
            OSGeo.GDAL.Driver driver = Gdal.GetDriverByName("GTiff");
            //调用Creat函数创建影像
            //如果路径存在，则删除路径
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            //OSGeo.GDAL.Driver driver.Create(路径，x方向的尺寸，y方向的尺寸，像素带，数据的类型，一些配置选项：内容是字符串类型的数组);
            Dataset m_FloodSimulatedDataSet = driver.Create(path, width, height, 1, DataType.GDT_Float32, null);
            //以下是设置影像属性
            m_FloodSimulatedDataSet.SetGeoTransform(transform); //影像转换参数
            m_FloodSimulatedDataSet.SetProjection(projection); //投影  y
            //writeRaster(x方向的偏移量，y方向的偏移量，x方向上的尺寸，y方向上的尺寸，缓存数据，缓存数据的宽，缓存数据的高，像素空间，线空间)
            m_FloodSimulatedDataSet.GetRasterBand(1).WriteRaster(0, 0, width, height, data, width, height, 0, 0);
            //刷新像素带的缓存
            m_FloodSimulatedDataSet.GetRasterBand(1).FlushCache();
            //刷新影像数据的缓存
            m_FloodSimulatedDataSet.FlushCache();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }



        /// <summary>
        /// 从地理空间转换到像素空间
        /// </summary>
        /// <param name="adfGeoTransform">影像坐标转化参数</param>
        /// <param name="x">经度</param>
        /// <param name="y">纬度</param>
        /// <param name="col">像素所在列</param>
        /// <param name="row">像素所在行</param>
        public void geoToImageSpace(double[] m_GeoTransform,int sizex,int sizey, double x, double y, out int col, out int row,out int totalindx)
        {
            Console.WriteLine("x:" + x);
            Console.WriteLine("y:" + y);
            foreach(double t in m_GeoTransform)
            {
                Console.WriteLine("m_GeoTransform的属性：" + t);
            }
            //新加代码
            //line = (int)((m_GeoTransform[4] * (x - m_GeoTransform[0]) - m_GeoTransform[1] * (y - m_GeoTransform[3])) / (m_GeoTransform[2] * m_GeoTransform[4] - m_GeoTransform[1] * m_GeoTransform[5]));
            //pixel = (int)((m_GeoTransform[5] * (x - m_GeoTransform[0]) - m_GeoTransform[2] * (y - m_GeoTransform[3])) / (m_GeoTransform[1] * m_GeoTransform[5] - m_GeoTransform[2] * m_GeoTransform[4]));
            ///*******************///

            row = (int)((y * m_GeoTransform[1] - x * m_GeoTransform[4] + m_GeoTransform[0] * m_GeoTransform[4] - m_GeoTransform[3] * m_GeoTransform[1]) / (m_GeoTransform[5] * m_GeoTransform[1] - m_GeoTransform[2] * m_GeoTransform[4]));
            col = (int)((x - m_GeoTransform[0] - row * m_GeoTransform[2]) / m_GeoTransform[1]);
            Console.WriteLine("row: " + row);
            Console.WriteLine("col: " + col);
            totalindx = sizex * row + col;
        }

     

    }

    /// <summary>
    /// 经纬度坐标类
    /// </summary>
    public class PoiD
    {
        public double X;
        public double Y;
        public PoiD(double x, double y)
        {
            X = x;
            Y = y;
        }
    }   
    /// <summary>
    /// 像素点的结构体
    /// </summary>  
    public class PointEleva
    {
        ////列号  
        public int X;
        //行号
        public int Y;            
        public double Elevation;  //像素值(高程值)  
        public bool IsFlooded; //淹没标记  
        public bool IsDaba;//标记是否为大坝出水口
        public double PerDistance
        {
            get
            {
                //算新点与原始点之间的距离，√x²+y²
                return Math.Sqrt((X - originPoi.X) * (X - originPoi.X) + (Y - originPoi.Y) * (Y - originPoi.Y));
            }
        }
        public double FloodLevel; //淹没层级
        public PointEleva parentPoint;//父亲节点 
        public PointEleva originPoi;//可识别原始点
        
    };

    public class NewPoint
    {
        public int X;          //列号  
        public int Y;          //行号  
        public double Elevation;  //像素值(高程值)

    };
}
