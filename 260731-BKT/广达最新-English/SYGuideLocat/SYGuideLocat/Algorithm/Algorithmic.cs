using HalconDotNet;
using HZH_Controls;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using SY.Common;
using SY.UICommon.Controls;
using SY.UICommon.Controls.ViewROI;
using SYGuideLocat;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace QuantaApply.Algorithm
{
    public class Algorithmic
    {
        public SYHalconTool _win = null;

        public void disp_message(HTuple hv_WindowHandle, HTuple hv_String, HTuple hv_CoordSystem,
            HTuple hv_Row, HTuple hv_Column, HTuple hv_Color, HTuple hv_Box)
        {
            HTuple hv_GenParamName = null, hv_GenParamValue = null;
            HTuple hv_Color_COPY_INP_TMP = hv_Color.Clone();
            HTuple hv_Column_COPY_INP_TMP = hv_Column.Clone();
            HTuple hv_CoordSystem_COPY_INP_TMP = hv_CoordSystem.Clone();
            HTuple hv_Row_COPY_INP_TMP = hv_Row.Clone();
            if ((int)((new HTuple(hv_Row_COPY_INP_TMP.TupleEqual(new HTuple()))).TupleOr(
                new HTuple(hv_Column_COPY_INP_TMP.TupleEqual(new HTuple())))) != 0)
            {

                return;
            }
            if ((int)(new HTuple(hv_Row_COPY_INP_TMP.TupleEqual(-1))) != 0)
            {
                hv_Row_COPY_INP_TMP = 12;
            }
            if ((int)(new HTuple(hv_Column_COPY_INP_TMP.TupleEqual(-1))) != 0)
            {
                hv_Column_COPY_INP_TMP = 12;
            }
            hv_GenParamName = new HTuple();
            hv_GenParamValue = new HTuple();
            if ((int)(new HTuple((new HTuple(hv_Box.TupleLength())).TupleGreater(0))) != 0)
            {
                if ((int)(new HTuple(((hv_Box.TupleSelect(0))).TupleEqual("false"))) != 0)
                {
                    //Display no box
                    hv_GenParamName = hv_GenParamName.TupleConcat("box");
                    hv_GenParamValue = hv_GenParamValue.TupleConcat("false");
                }
                else if ((int)(new HTuple(((hv_Box.TupleSelect(0))).TupleNotEqual("true"))) != 0)
                {
                    //Set a color other than the default.
                    hv_GenParamName = hv_GenParamName.TupleConcat("box_color");
                    hv_GenParamValue = hv_GenParamValue.TupleConcat(hv_Box.TupleSelect(0));
                }
            }
            if ((int)(new HTuple((new HTuple(hv_Box.TupleLength())).TupleGreater(1))) != 0)
            {
                if ((int)(new HTuple(((hv_Box.TupleSelect(1))).TupleEqual("false"))) != 0)
                {
                    //Display no shadow.
                    hv_GenParamName = hv_GenParamName.TupleConcat("shadow");
                    hv_GenParamValue = hv_GenParamValue.TupleConcat("false");
                }
                else if ((int)(new HTuple(((hv_Box.TupleSelect(1))).TupleNotEqual("true"))) != 0)
                {
                    //Set a shadow color other than the default.
                    hv_GenParamName = hv_GenParamName.TupleConcat("shadow_color");
                    hv_GenParamValue = hv_GenParamValue.TupleConcat(hv_Box.TupleSelect(1));
                }
            }
            //Restore default CoordSystem behavior.
            if ((int)(new HTuple(hv_CoordSystem_COPY_INP_TMP.TupleNotEqual("window"))) != 0)
            {
                hv_CoordSystem_COPY_INP_TMP = "image";
            }
            //
            if ((int)(new HTuple(hv_Color_COPY_INP_TMP.TupleEqual(""))) != 0)
            {
                //disp_text does not accept an empty string for Color.
                hv_Color_COPY_INP_TMP = new HTuple();
            }
            //
            HOperatorSet.DispText(hv_WindowHandle, hv_String, hv_CoordSystem_COPY_INP_TMP,
                hv_Row_COPY_INP_TMP, hv_Column_COPY_INP_TMP, hv_Color_COPY_INP_TMP, hv_GenParamName,
                hv_GenParamValue);
        }
        public static void gen_arrow_contour_xld(out HObject ho_Arrow, HTuple hv_Row1, HTuple hv_Column1,
    HTuple hv_Row2, HTuple hv_Column2, HTuple hv_HeadLength, HTuple hv_HeadWidth)
        {



            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_TempArrow = null;

            // Local control variables 

            HTuple hv_Length = null, hv_ZeroLengthIndices = null;
            HTuple hv_DR = null, hv_DC = null, hv_HalfHeadWidth = null;
            HTuple hv_RowP1 = null, hv_ColP1 = null, hv_RowP2 = null;
            HTuple hv_ColP2 = null, hv_Index = null;
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Arrow);
            HOperatorSet.GenEmptyObj(out ho_TempArrow);
            //This procedure generates arrow shaped XLD contours,
            //pointing from (Row1, Column1) to (Row2, Column2).
            //If starting and end point are identical, a contour consisting
            //of a single point is returned.
            //
            //input parameteres:
            //Row1, Column1: Coordinates of the arrows' starting points
            //Row2, Column2: Coordinates of the arrows' end points
            //HeadLength, HeadWidth: Size of the arrow heads in pixels
            //
            //output parameter:
            //Arrow: The resulting XLD contour
            //
            //The input tuples Row1, Column1, Row2, and Column2 have to be of
            //the same length.
            //HeadLength and HeadWidth either have to be of the same length as
            //Row1, Column1, Row2, and Column2 or have to be a single element.
            //If one of the above restrictions is violated, an error will occur.
            //
            //
            //Init
            ho_Arrow.Dispose();
            HOperatorSet.GenEmptyObj(out ho_Arrow);
            //
            //Calculate the arrow length
            HOperatorSet.DistancePp(hv_Row1, hv_Column1, hv_Row2, hv_Column2, out hv_Length);
            //
            //Mark arrows with identical start and end point
            //(set Length to -1 to avoid division-by-zero exception)
            hv_ZeroLengthIndices = hv_Length.TupleFind(0);
            if ((int)(new HTuple(hv_ZeroLengthIndices.TupleNotEqual(-1))) != 0)
            {
                if (hv_Length == null)
                    hv_Length = new HTuple();
                hv_Length[hv_ZeroLengthIndices] = -1;
            }
            //
            //Calculate auxiliary variables.
            hv_DR = (1.0 * (hv_Row2 - hv_Row1)) / hv_Length;
            hv_DC = (1.0 * (hv_Column2 - hv_Column1)) / hv_Length;
            hv_HalfHeadWidth = hv_HeadWidth / 2.0;
            //
            //Calculate end points of the arrow head.
            hv_RowP1 = (hv_Row1 + ((hv_Length - hv_HeadLength) * hv_DR)) + (hv_HalfHeadWidth * hv_DC);
            hv_ColP1 = (hv_Column1 + ((hv_Length - hv_HeadLength) * hv_DC)) - (hv_HalfHeadWidth * hv_DR);
            hv_RowP2 = (hv_Row1 + ((hv_Length - hv_HeadLength) * hv_DR)) - (hv_HalfHeadWidth * hv_DC);
            hv_ColP2 = (hv_Column1 + ((hv_Length - hv_HeadLength) * hv_DC)) + (hv_HalfHeadWidth * hv_DR);
            //
            //Finally create output XLD contour for each input point pair
            for (hv_Index = 0; (int)hv_Index <= (int)((new HTuple(hv_Length.TupleLength())) - 1); hv_Index = (int)hv_Index + 1)
            {
                if ((int)(new HTuple(((hv_Length.TupleSelect(hv_Index))).TupleEqual(-1))) != 0)
                {
                    //Create_ single points for arrows with identical start and end point
                    ho_TempArrow.Dispose();
                    HOperatorSet.GenContourPolygonXld(out ho_TempArrow, hv_Row1.TupleSelect(hv_Index),
                        hv_Column1.TupleSelect(hv_Index));
                }
                else
                {
                    //Create arrow contour
                    ho_TempArrow.Dispose();
                    HOperatorSet.GenContourPolygonXld(out ho_TempArrow, ((((((((((hv_Row1.TupleSelect(
                        hv_Index))).TupleConcat(hv_Row2.TupleSelect(hv_Index)))).TupleConcat(
                        hv_RowP1.TupleSelect(hv_Index)))).TupleConcat(hv_Row2.TupleSelect(hv_Index)))).TupleConcat(
                        hv_RowP2.TupleSelect(hv_Index)))).TupleConcat(hv_Row2.TupleSelect(hv_Index)),
                        ((((((((((hv_Column1.TupleSelect(hv_Index))).TupleConcat(hv_Column2.TupleSelect(
                        hv_Index)))).TupleConcat(hv_ColP1.TupleSelect(hv_Index)))).TupleConcat(
                        hv_Column2.TupleSelect(hv_Index)))).TupleConcat(hv_ColP2.TupleSelect(
                        hv_Index)))).TupleConcat(hv_Column2.TupleSelect(hv_Index)));
                }
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.ConcatObj(ho_Arrow, ho_TempArrow, out ExpTmpOutVar_0);
                    ho_Arrow.Dispose();
                    ho_Arrow = ExpTmpOutVar_0;
                }
            }
            ho_TempArrow.Dispose();
        }
        public static void pts_to_best_circle(out HObject ho_Circle, HTuple hv_Rows, HTuple hv_Cols,
    HTuple hv_ArcType, HTuple hv_ActiveNum, out HTuple hv_RowCenter, out HTuple hv_ColCenter,
    out HTuple hv_Radius, out HTuple hv_StartPhi, out HTuple hv_EndPhi, out HTuple hv_PointOrder,
    out HTuple hv_ArcAngle)
        {



            // Local iconic variables 

            HObject ho_Contour = null, ho_Circle1 = null, ho_Circle2 = null;

            // Local control variables 

            HTuple hv_Length = null, hv_Length1 = new HTuple();
            HTuple hv_DistanceMin1 = new HTuple(), hv_DistanceMax1 = new HTuple();
            HTuple hv_DistanceMin2 = new HTuple(), hv_DistanceMax2 = new HTuple();
            HTuple hv_Sum1 = new HTuple(), hv_Sum2 = new HTuple();
            HTuple hv_Row = new HTuple(), hv_Col = new HTuple(), hv_CircleLength = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Circle);
            HOperatorSet.GenEmptyObj(out ho_Contour);
            HOperatorSet.GenEmptyObj(out ho_Circle1);
            HOperatorSet.GenEmptyObj(out ho_Circle2);
            hv_StartPhi = new HTuple();
            hv_EndPhi = new HTuple();
            hv_PointOrder = new HTuple();
            hv_ArcAngle = new HTuple();
            //初始化
            hv_RowCenter = 0;
            hv_ColCenter = 0;
            hv_Radius = 0;
            //产生一个空的直线对象，用于保存拟合后的圆
            ho_Circle.Dispose();
            HOperatorSet.GenEmptyObj(out ho_Circle);
            //计算边缘数量
            HOperatorSet.TupleLength(hv_Cols, out hv_Length);
            //当边缘数量不小于有效点数时进行拟合
            if ((int)((new HTuple(hv_Length.TupleGreaterEqual(hv_ActiveNum))).TupleAnd(new HTuple(hv_ActiveNum.TupleGreater(
                2)))) != 0)
            {
                //halcon的拟合是基于xld的，需要把边缘连接成xld
                if ((int)(new HTuple(hv_ArcType.TupleEqual("circle"))) != 0)
                {
                    //如果是闭合的圆，轮廓需要首尾相连
                    ho_Contour.Dispose();
                    HOperatorSet.GenContourPolygonXld(out ho_Contour, hv_Rows.TupleConcat(hv_Rows.TupleSelect(
                        0)), hv_Cols.TupleConcat(hv_Cols.TupleSelect(0)));
                }
                else
                {
                    ho_Contour.Dispose();
                    HOperatorSet.GenContourPolygonXld(out ho_Contour, hv_Rows, hv_Cols);
                }
                //拟合圆。使用的算法是''geotukey''，其他算法请参考fit_circle_contour_xld的描述部分。
                HOperatorSet.FitCircleContourXld(ho_Contour, "geotukey", -1, 0, 0, 3, 2, out hv_RowCenter,
                    out hv_ColCenter, out hv_Radius, out hv_StartPhi, out hv_EndPhi, out hv_PointOrder);
                //判断拟合结果是否有效：如果拟合成功，数组中元素的数量大于0
                HOperatorSet.TupleLength(hv_StartPhi, out hv_Length1);
                if ((int)(new HTuple(hv_Length1.TupleLess(1))) != 0)
                {
                    ho_Contour.Dispose();
                    ho_Circle1.Dispose();
                    ho_Circle2.Dispose();

                    return;
                }
                //根据拟合结果，产生直线xld
                if ((int)(new HTuple(hv_ArcType.TupleEqual("arc"))) != 0)
                {
                    //判断圆弧的方向：顺时针还是逆时针
                    //halcon求圆弧会出现方向混乱的问题
                    ho_Circle1.Dispose();
                    HOperatorSet.GenCircleContourXld(out ho_Circle1, hv_RowCenter, hv_ColCenter,
                        hv_Radius, hv_StartPhi, hv_EndPhi, "positive", 1);
                    ho_Circle2.Dispose();
                    HOperatorSet.GenCircleContourXld(out ho_Circle2, hv_RowCenter, hv_ColCenter,
                        hv_Radius, hv_StartPhi, hv_EndPhi, "negative", 1);

                    HOperatorSet.DistancePc(ho_Circle1, hv_Rows, hv_Cols, out hv_DistanceMin1,
                        out hv_DistanceMax1);
                    HOperatorSet.DistancePc(ho_Circle2, hv_Rows, hv_Cols, out hv_DistanceMin2,
                        out hv_DistanceMax2);
                    HOperatorSet.TupleSum(hv_DistanceMin1, out hv_Sum1);
                    HOperatorSet.TupleSum(hv_DistanceMin2, out hv_Sum2);
                    if ((int)(new HTuple(hv_Sum1.TupleLess(hv_Sum2))) != 0)
                    {
                        hv_PointOrder = "positive";
                    }
                    else
                    {
                        hv_PointOrder = "negative";
                    }
                    ho_Circle.Dispose();
                    HOperatorSet.GenCircleContourXld(out ho_Circle, hv_RowCenter, hv_ColCenter,
                        hv_Radius, hv_StartPhi, hv_EndPhi, hv_PointOrder, 1);
                    HOperatorSet.GetContourXld(ho_Circle, out hv_Row, out hv_Col);
                    HOperatorSet.AngleLl(hv_RowCenter, hv_ColCenter, hv_Row.TupleSelect(0), hv_Col.TupleSelect(
                        0), hv_RowCenter, hv_ColCenter, hv_Row.TupleSelect((new HTuple(hv_Row.TupleLength()
                        )) - 1), hv_Col.TupleSelect((new HTuple(hv_Row.TupleLength())) - 1), out hv_ArcAngle);
                    if ((int)(0) != 0)
                    {
                        HOperatorSet.LengthXld(ho_Circle, out hv_CircleLength);
                        hv_ArcAngle = hv_EndPhi - hv_StartPhi;
                        if ((int)(new HTuple(hv_CircleLength.TupleGreater(((new HTuple(180)).TupleRad()
                            ) * hv_Radius))) != 0)
                        {
                            if ((int)(new HTuple(((hv_ArcAngle.TupleAbs())).TupleLess((new HTuple(180)).TupleRad()
                                ))) != 0)
                            {
                                if ((int)(new HTuple(hv_ArcAngle.TupleGreater(0))) != 0)
                                {
                                    hv_ArcAngle = ((new HTuple(360)).TupleRad()) - hv_ArcAngle;
                                }
                                else
                                {
                                    hv_ArcAngle = ((new HTuple(360)).TupleRad()) + hv_ArcAngle;
                                }
                            }
                        }
                        else
                        {
                            if ((int)(new HTuple(hv_CircleLength.TupleLess(((new HTuple(180)).TupleRad()
                                ) * hv_Radius))) != 0)
                            {
                                if ((int)(new HTuple(((hv_ArcAngle.TupleAbs())).TupleGreater((new HTuple(180)).TupleRad()
                                    ))) != 0)
                                {
                                    if ((int)(new HTuple(hv_ArcAngle.TupleGreater(0))) != 0)
                                    {
                                        hv_ArcAngle = hv_ArcAngle - ((new HTuple(360)).TupleRad());
                                    }
                                    else
                                    {
                                        hv_ArcAngle = ((new HTuple(360)).TupleRad()) + hv_ArcAngle;
                                    }
                                }
                            }

                        }
                    }
                }
                else
                {
                    hv_StartPhi = 0;
                    hv_EndPhi = (new HTuple(360)).TupleRad();
                    hv_ArcAngle = (new HTuple(360)).TupleRad();
                    ho_Circle.Dispose();
                    HOperatorSet.GenCircleContourXld(out ho_Circle, hv_RowCenter, hv_ColCenter,
                        hv_Radius, hv_StartPhi, hv_EndPhi, hv_PointOrder, 1);
                }
            }

            ho_Contour.Dispose();
            ho_Circle1.Dispose();
            ho_Circle2.Dispose();

            return;
        }
        public static void spoke(HObject ho_Image, out HObject ho_Regions, HTuple hv_Elements,
    HTuple hv_DetectHeight, HTuple hv_DetectWidth, HTuple hv_Sigma, HTuple hv_Threshold,
    HTuple hv_Transition, HTuple hv_Select, HTuple hv_ROIRows, HTuple hv_ROICols,
    HTuple hv_Direct, out HTuple hv_ResultRow, out HTuple hv_ResultColumn, out HTuple hv_ArcType)
        {
            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_Contour, ho_ContCircle, ho_Rectangle1 = null;
            HObject ho_Arrow1 = null;

            // Local control variables 

            HTuple hv_Width = null, hv_Height = null, hv_RowC = null;
            HTuple hv_ColumnC = null, hv_Radius = null, hv_StartPhi = null;
            HTuple hv_EndPhi = null, hv_PointOrder = null, hv_RowXLD = null;
            HTuple hv_ColXLD = null, hv_Length2 = null, hv_i = null;
            HTuple hv_j = new HTuple(), hv_RowE = new HTuple(), hv_ColE = new HTuple();
            HTuple hv_ATan = new HTuple(), hv_RowL2 = new HTuple();
            HTuple hv_RowL1 = new HTuple(), hv_ColL2 = new HTuple();
            HTuple hv_ColL1 = new HTuple(), hv_MsrHandle_Measure = new HTuple();
            HTuple hv_RowEdge = new HTuple(), hv_ColEdge = new HTuple();
            HTuple hv_Amplitude = new HTuple(), hv_Distance = new HTuple();
            HTuple hv_tRow = new HTuple(), hv_tCol = new HTuple();
            HTuple hv_t = new HTuple(), hv_Number = new HTuple(), hv_k = new HTuple();
            HTuple hv_Select_COPY_INP_TMP = hv_Select.Clone();
            HTuple hv_Transition_COPY_INP_TMP = hv_Transition.Clone();

            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_Contour);
            HOperatorSet.GenEmptyObj(out ho_ContCircle);
            HOperatorSet.GenEmptyObj(out ho_Rectangle1);
            HOperatorSet.GenEmptyObj(out ho_Arrow1);
            hv_ArcType = new HTuple();
            //获取图像尺寸
            HOperatorSet.GetImageSize(ho_Image, out hv_Width, out hv_Height);
            //产生一个空显示对象，用于显示
            ho_Regions.Dispose();
            HOperatorSet.GenEmptyObj(out ho_Regions);
            //初始化边缘坐标数组
            hv_ResultRow = new HTuple();
            hv_ResultColumn = new HTuple();

            //产生xld
            ho_Contour.Dispose();
            HOperatorSet.GenContourPolygonXld(out ho_Contour, hv_ROIRows, hv_ROICols);
            //用回归线法（不抛出异常点，所有点权重一样）拟合圆
            HOperatorSet.FitCircleContourXld(ho_Contour, "algebraic", -1, 0, 0, 1, 2, out hv_RowC,
                out hv_ColumnC, out hv_Radius, out hv_StartPhi, out hv_EndPhi, out hv_PointOrder);
            //根据拟合结果产生xld，并保持到显示对象
            ho_ContCircle.Dispose();
            HOperatorSet.GenCircleContourXld(out ho_ContCircle, hv_RowC, hv_ColumnC, hv_Radius,
                hv_StartPhi, hv_EndPhi, hv_PointOrder, 3);
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_Regions, ho_ContCircle, out ExpTmpOutVar_0);
                ho_Regions.Dispose();
                ho_Regions = ExpTmpOutVar_0;
            }

            //获取圆或圆弧xld上的点坐标
            HOperatorSet.GetContourXld(ho_ContCircle, out hv_RowXLD, out hv_ColXLD);

            //求圆或圆弧xld上的点的数量
            HOperatorSet.TupleLength(hv_ColXLD, out hv_Length2);
            if ((int)(new HTuple(hv_Elements.TupleLess(3))) != 0)
            {
                //    disp_message (WindowHandle, '检测的边缘数量太少，请重新设置!', 'window', 52, 12, 'red', 'false')
                ho_Contour.Dispose();
                ho_ContCircle.Dispose();
                ho_Rectangle1.Dispose();
                ho_Arrow1.Dispose();

                return;
            }
            //如果xld是圆弧，有Length2个点，从起点开始，等间距（间距为Length2/(Elements-1)）取Elements个点，作为卡尺工具的中点
            //如果xld是圆，有Length2个点，以0°为起点，从起点开始，等间距（间距为Length2/(Elements)）取Elements个点，作为卡尺工具的中点
            HTuple end_val27 = hv_Elements - 1;
            HTuple step_val27 = 1;
            for (hv_i = 0; hv_i.Continue(end_val27, step_val27); hv_i = hv_i.TupleAdd(step_val27))
            {

                if ((int)(new HTuple(((hv_RowXLD.TupleSelect(0))).TupleEqual(hv_RowXLD.TupleSelect(
                    hv_Length2 - 1)))) != 0)
                {
                    //xld的起点和终点坐标相对，为圆
                    HOperatorSet.TupleInt(((1.0 * hv_Length2) / hv_Elements) * hv_i, out hv_j);
                    hv_ArcType = "circle";
                }
                else
                {
                    //否则为圆弧
                    HOperatorSet.TupleInt(((1.0 * hv_Length2) / (hv_Elements - 1)) * hv_i, out hv_j);
                    hv_ArcType = "arc";
                }
                //索引越界，强制赋值为最后一个索引
                if ((int)(new HTuple(hv_j.TupleGreaterEqual(hv_Length2))) != 0)
                {
                    hv_j = hv_Length2 - 1;
                    //continue
                }
                //获取卡尺工具中心
                hv_RowE = hv_RowXLD.TupleSelect(hv_j);
                hv_ColE = hv_ColXLD.TupleSelect(hv_j);

                //超出图像区域，不检测，否则容易报异常
                if ((int)((new HTuple((new HTuple((new HTuple(hv_RowE.TupleGreater(hv_Height - 1))).TupleOr(
                    new HTuple(hv_RowE.TupleLess(0))))).TupleOr(new HTuple(hv_ColE.TupleGreater(
                    hv_Width - 1))))).TupleOr(new HTuple(hv_ColE.TupleLess(0)))) != 0)
                {
                    continue;
                }
                //边缘搜索方向类型：'inner'搜索方向由圆外指向圆心；'outer'搜索方向由圆心指向圆外
                if ((int)(new HTuple(hv_Direct.TupleEqual("inner"))) != 0)
                {
                    //求卡尺工具的边缘搜索方向
                    //求圆心指向边缘的矢量的角度
                    HOperatorSet.TupleAtan2((-hv_RowE) + hv_RowC, hv_ColE - hv_ColumnC, out hv_ATan);
                    //角度反向
                    hv_ATan = ((new HTuple(180)).TupleRad()) + hv_ATan;
                }
                else
                {
                    //求卡尺工具的边缘搜索方向
                    //求圆心指向边缘的矢量的角度
                    HOperatorSet.TupleAtan2((-hv_RowE) + hv_RowC, hv_ColE - hv_ColumnC, out hv_ATan);
                }


                //产生卡尺xld，并保持到显示对象
                ho_Rectangle1.Dispose();
                HOperatorSet.GenRectangle2ContourXld(out ho_Rectangle1, hv_RowE, hv_ColE, hv_ATan,
                    hv_DetectHeight / 2, hv_DetectWidth / 2);
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.ConcatObj(ho_Regions, ho_Rectangle1, out ExpTmpOutVar_0);
                    ho_Regions.Dispose();
                    ho_Regions = ExpTmpOutVar_0;
                }
                //用箭头xld指示边缘搜索方向，并保持到显示对象
                if ((int)(new HTuple(hv_i.TupleEqual(0))) != 0)
                {
                    hv_RowL2 = hv_RowE + ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleSin()));
                    hv_RowL1 = hv_RowE - ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleSin()));
                    hv_ColL2 = hv_ColE + ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleCos()));
                    hv_ColL1 = hv_ColE - ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleCos()));
                    ho_Arrow1.Dispose();
                    gen_arrow_contour_xld(out ho_Arrow1, hv_RowL1, hv_ColL1, hv_RowL2, hv_ColL2,
                        25, 25);
                    {
                        HObject ExpTmpOutVar_0;
                        HOperatorSet.ConcatObj(ho_Regions, ho_Arrow1, out ExpTmpOutVar_0);
                        ho_Regions.Dispose();
                        ho_Regions = ExpTmpOutVar_0;
                    }
                }


                //产生测量对象句柄
                HOperatorSet.GenMeasureRectangle2(hv_RowE, hv_ColE, hv_ATan, hv_DetectHeight / 2,
                    hv_DetectWidth / 2, hv_Width, hv_Height, "nearest_neighbor", out hv_MsrHandle_Measure);

                //设置极性
                if ((int)(new HTuple(hv_Transition_COPY_INP_TMP.TupleEqual("negative"))) != 0)
                {
                    hv_Transition_COPY_INP_TMP = "negative";
                }
                else
                {
                    if ((int)(new HTuple(hv_Transition_COPY_INP_TMP.TupleEqual("positive"))) != 0)
                    {

                        hv_Transition_COPY_INP_TMP = "positive";
                    }
                    else
                    {
                        hv_Transition_COPY_INP_TMP = "all";
                    }
                }
                //设置边缘位置。最强点是从所有边缘中选择幅度绝对值最大点，需要设置为'all'
                if ((int)(new HTuple(hv_Select_COPY_INP_TMP.TupleEqual("first"))) != 0)
                {
                    hv_Select_COPY_INP_TMP = "first";
                }
                else
                {
                    if ((int)(new HTuple(hv_Select_COPY_INP_TMP.TupleEqual("last"))) != 0)
                    {

                        hv_Select_COPY_INP_TMP = "last";
                    }
                    else
                    {
                        hv_Select_COPY_INP_TMP = "all";
                    }
                }
                //检测边缘
                HOperatorSet.MeasurePos(ho_Image, hv_MsrHandle_Measure, hv_Sigma, hv_Threshold,
                    hv_Transition_COPY_INP_TMP, hv_Select_COPY_INP_TMP, out hv_RowEdge, out hv_ColEdge,
                    out hv_Amplitude, out hv_Distance);
                //清除测量对象句柄
                HOperatorSet.CloseMeasure(hv_MsrHandle_Measure);
                //临时变量初始化
                //tRow，tCol保存找到指定边缘的坐标
                hv_tRow = 0;
                hv_tCol = 0;
                //t保存边缘的幅度绝对值
                hv_t = 0;
                HOperatorSet.TupleLength(hv_RowEdge, out hv_Number);
                //找到的边缘必须至少为1个
                if ((int)(new HTuple(hv_Number.TupleLess(1))) != 0)
                {
                    continue;
                }
                //有多个边缘时，选择幅度绝对值最大的边缘
                HTuple end_val120 = hv_Number - 1;
                HTuple step_val120 = 1;
                for (hv_k = 0; hv_k.Continue(end_val120, step_val120); hv_k = hv_k.TupleAdd(step_val120))
                {
                    if ((int)(new HTuple(((((hv_Amplitude.TupleSelect(hv_k))).TupleAbs())).TupleGreater(
                        hv_t))) != 0)
                    {

                        hv_tRow = hv_RowEdge.TupleSelect(hv_k);
                        hv_tCol = hv_ColEdge.TupleSelect(hv_k);
                        hv_t = ((hv_Amplitude.TupleSelect(hv_k))).TupleAbs();
                    }
                }
                //把找到的边缘保存在输出数组
                if ((int)(new HTuple(hv_t.TupleGreater(0))) != 0)
                {

                    hv_ResultRow = hv_ResultRow.TupleConcat(hv_tRow);
                    hv_ResultColumn = hv_ResultColumn.TupleConcat(hv_tCol);
                }
            }


            ho_Contour.Dispose();
            ho_ContCircle.Dispose();
            ho_Rectangle1.Dispose();
            ho_Arrow1.Dispose();
        }

        public static void BKTAlgorithmicdown(string singernum, String JZ,SYHalconTool _win, HObject ho_Image, int num, double len, out HObject ho_Cross,
    out HObject ho_Cross1, HTuple hv_areamin, HTuple hv_aramax, HTuple hv_circularitymin,
    HTuple hv_circularitymax, HTuple hv_Circlepixnumber,HObject ReduImage, out HTuple hv_Indices,
    out HTuple hv_Indices1, out HTuple hv_angledown, out HTuple hv_distance, out HTuple hv_rowcenter,
    out HTuple hv_colcenter, out bool State, double angledown, out HTuple area, out HTuple outlen)
        {
            area = 0;
            outlen = 0;
            string path = Path.Combine(AppContext.BaseDirectory, "PDWON", DateTime.Now.ToString("yyyyMMdd"));
            //if (!File.Exists(path))
            //{
            //    Directory.CreateDirectory(path);
            //}
            //HOperatorSet.WriteImage(ho_Image,"bmp",0, path+"\\"+DateTime.Now.ToString("yyyyMMddHHmmss")+".bmp");

            State = true;
            HObject ho_Region, ho_ConnectedRegions, ho_SelectedRegions;
            HObject ho_RegionFillUp, ho_SelectedRegions1, ho_ObjectSelected = null;
            HObject ho_ContCircle = null, ho_Regions = null;
            HTuple hv_UsedThreshold1 = null, hv_Number = null;
            HTuple hv_Index = new HTuple(), hv_Row = new HTuple();
            HTuple hv_Column = new HTuple(), hv_Radius = new HTuple();
            HTuple hv_Row1 = new HTuple(), hv_Col = new HTuple(), hv_ResultRow = new HTuple();
            HTuple hv_ResultColumn = new HTuple(), hv_ArcType = new HTuple();
            HTuple hv_Radius1 = new HTuple(), hv_StartPhi = new HTuple();
            HTuple hv_EndPhi = new HTuple(), hv_PointOrder = new HTuple();
            HTuple hv_ArcAngle = new HTuple(), hv_Distance = new HTuple();
            HTuple hv_Min = new HTuple(), hv_Max = new HTuple(), hv_Angle = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Cross);
            HOperatorSet.GenEmptyObj(out ho_Cross1);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected);
            HOperatorSet.GenEmptyObj(out ho_ContCircle);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            hv_Indices = new HTuple();
            hv_Indices1 = new HTuple();
            hv_angledown = new HTuple();
            ho_Region.Dispose();
            HObject hObject = null;

            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_ImageGauss = null, ho_ImageMean, ho_ImageEmphasize;

            HObject ho_ImageResult, ho_ImageResult1 = null, ho_Region1;
            HObject ho_RegionDilation, ho_Region2, ho_ConnectedRegions2;
            HObject ho_SelectedRegions4, ho_SelectedRegions3;
            HObject ho_Circle1 = null, ho_RegionErosion = null, ho_RegionDifference;
            HObject ho_ConnectedRegions1, ho_SelectedRegions2;

            // Local control variables 

            HTuple hv_UsedThreshold = null;
            HTuple hv_UsedThreshold2 = null, hv_Row2 = null, hv_Column1 = null;
            HTuple hv_Radius2 = null, hv_Number1 = null;
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_ImageGauss);
            HOperatorSet.GenEmptyObj(out ho_ImageMean);
            HOperatorSet.GenEmptyObj(out ho_ImageEmphasize);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_ImageResult);
            HOperatorSet.GenEmptyObj(out ho_ImageResult1);
            HOperatorSet.GenEmptyObj(out ho_Region1);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_Region2);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions4);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_Circle1);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions2);
            //gauss_filter (Image, ImageGauss, 5)
            HObject ho_Image1 = null;
          //  HOperatorSet.ReadImage(out ho_Image, "C:\\Users\\11814\\Desktop\\316c6a000e278a4f2b5d323a32130f9.jpg");

            ho_Image1 = ho_Image;
           
            if (ReduImage!=null)
            {
                HOperatorSet.ReduceDomain(ho_Image, ReduImage, out ho_Image1);
                _win.displayObj(ReduImage);
            }

            SYJsonObject sYJsonObject = new SYJsonObject();
            String datapath = Path.Combine(AppContext.BaseDirectory, "DATA", JZ);
            string widthpat = Path.Combine(datapath, $"DownPar{singernum.ToString()}.json");

            if (!Directory.Exists(datapath))
            {
                Directory.CreateDirectory(datapath);
            }

            sYJsonObject["卡尺"] = "45";
            sYJsonObject["寻找方式"] = "max";
            sYJsonObject["卡尺极性"] = "negative";
            sYJsonObject["筛选面积"] = "1800";
            sYJsonObject["圆度"] = "0.4";
            sYJsonObject["第一次筛选颜色"] = "light";
            sYJsonObject["第二次筛选颜色"] = "dark";
            sYJsonObject["膨胀系数"] = "5";
            sYJsonObject["白True黑False"] = "False";
            sYJsonObject["找圆半径最小值"] = "60";
            sYJsonObject["找圆半径最大值"] = "90";

            sYJsonObject["是否开启标头定义"] = "false";
            sYJsonObject["矩形框0"] = "1";
            sYJsonObject["矩形框1"] = "2";

            if (!File.Exists(widthpat))
            {
                sYJsonObject.Save(widthpat);

            }
            else {
                SYJsonObject sYJsonObject1 = new SYJsonObject();
                sYJsonObject1.FromFile(widthpat);

               List<string> _data1 =  sYJsonObject.GetKeys();
               List<string> _data2 = sYJsonObject1.GetKeys();

                if (_data1.Count != _data2.Count)
                {
                    foreach (var item in _data1)
                    {
                        if (_data2.Find(s => s == item)== null)
                        {
                            sYJsonObject1[item] = sYJsonObject[item];
                        }
                    }

                    sYJsonObject1.Save(widthpat);
                }

                sYJsonObject = sYJsonObject1;
            }


            try {

                if (Convert.ToBoolean(sYJsonObject["白True黑False"]))
                {
                    FindDownCliTwo(ho_Image1, hv_areamin, hv_aramax, Convert.ToDouble(sYJsonObject["圆度"]), out ho_SelectedRegions1, out ho_ImageResult1, Convert.ToDouble(sYJsonObject["找圆半径最小值"]), Convert.ToDouble(sYJsonObject["找圆半径最大值"]), Convert.ToInt16(sYJsonObject["膨胀系数"]));
                }
                else {
                    FindDownCli(ho_Image1, sYJsonObject["第一次筛选颜色"], sYJsonObject["第二次筛选颜色"], Convert.ToInt32(sYJsonObject["筛选面积"]), Convert.ToDouble(sYJsonObject["圆度"]), out ho_SelectedRegions1, out ho_ImageResult1, hv_areamin, hv_aramax, Convert.ToInt16(sYJsonObject["膨胀系数"]));
                }

            } catch { FindDownCli(ho_Image1, sYJsonObject["第一次筛选颜色"], sYJsonObject["第二次筛选颜色"], Convert.ToInt32(sYJsonObject["筛选面积"]), Convert.ToDouble(sYJsonObject["圆度"]), out ho_SelectedRegions1, out ho_ImageResult1, hv_areamin, hv_aramax, Convert.ToInt16(sYJsonObject["膨胀系数"])); }

            
            
            //FindDownCli(ho_Image1, out HObject ho_SelectedRegions13, out ho_ImageResult1, 15);
            HOperatorSet.CountObj(ho_SelectedRegions1, out hv_Number1);
            //HOperatorSet.CountObj(ho_SelectedRegions13, out HTuple hv_Number2);
            //if (hv_Number2> hv_Number1)
            //{
            //    ho_SelectedRegions1 = ho_SelectedRegions13;
            //    hv_Number2 = hv_Number1;
            //}


            hv_rowcenter = new HTuple();
            hv_colcenter = new HTuple();
            hv_distance = new HTuple();
            HTuple Radius = new HTuple();
            if (hv_Number1.D < num)
            {
                _win.displayText("存在污垢,Mark点未找完", Color.Red, 500, 500);
                State = false;
                return;
            }

            HTuple end_val10 = hv_Number;
            HTuple step_val10 = 1;
            int width = 50;

            for (hv_Index = 1; hv_Index <= hv_Number1; hv_Index++)
            {
                ho_ObjectSelected.Dispose();
                HOperatorSet.SelectObj(ho_SelectedRegions1, out ho_ObjectSelected, hv_Index);

                HOperatorSet.AreaCenter(ho_ObjectSelected, out area, out HTuple row, out HTuple col);
                
                HOperatorSet.SmallestCircle(ho_ObjectSelected, out hv_Row, out hv_Column,
                    out hv_Radius);
                ho_ContCircle.Dispose();
                HOperatorSet.GenCircleContourXld(out ho_ContCircle, hv_Row, hv_Column, hv_Radius,
                    0, 6.28318, "positive", 1);
                HOperatorSet.GetContourXld(ho_ContCircle, out hv_Row1, out hv_Col);
                ho_Regions.Dispose();
                spoke(ho_ImageResult1, out ho_Regions, 100, Convert.ToInt32(sYJsonObject["卡尺"]), 15, 1, 20, sYJsonObject["卡尺极性"].ToString(), sYJsonObject["寻找方式"].ToString(), hv_Row1,
                    hv_Col, "inner", out hv_ResultRow, out hv_ResultColumn, out hv_ArcType);//30
                pts_to_best_circle(out HObject ho_circle, hv_ResultRow, hv_ResultColumn, "circle",
                    hv_Circlepixnumber, out HTuple hv_Rowcenter, out HTuple hv_Colcenter, out hv_Radius1,
                    out hv_StartPhi, out hv_EndPhi, out hv_PointOrder, out hv_ArcAngle);

                _win.displayObj(ho_circle);

                if (hv_rowcenter == null)
                    hv_rowcenter = new HTuple();
                hv_rowcenter[hv_Index - 1] = hv_Rowcenter;
                if (hv_colcenter == null)
                    hv_colcenter = new HTuple();
                hv_colcenter[hv_Index - 1] = hv_Colcenter;
                HOperatorSet.DistancePp(0, 0, hv_Rowcenter, hv_Colcenter, out hv_Distance);
                if (hv_distance == null)
                    hv_distance = new HTuple();
                hv_distance[hv_Index - 1] = hv_Distance;
                if (Radius == null)
                    Radius = new HTuple();
                Radius[hv_Index - 1] = hv_Radius1;
            }



            if (Convert.ToBoolean(sYJsonObject["是否开启标头定义"]))
            {
                //sYJsonObject["矩形框0"] = "1";
                //sYJsonObject["矩形框1"] = "2";

                try
                {
                    string Pathstr = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", JZ, "标头" + singernum, "Down", "裁剪");


                    HOperatorSet.ReadRegion(out HObject regions, Path.Combine(Pathstr, "0.hobj"));
                    HOperatorSet.SmallestRectangle1(regions, out HTuple row1, out HTuple Col1, out HTuple Row2, out HTuple Col2);
                    int idex = 0;
                    for (int i = 0; i < hv_rowcenter.Length; i++)
                    {
                        if (hv_rowcenter[i] >= row1 && hv_rowcenter[i] <= Row2 && hv_colcenter[i] >= Col1 && hv_colcenter[i] <= Col2)
                        {
                            idex = i;
                            break;
                        }
                    }



                    hv_Indices = Convert.ToInt32( sYJsonObject["矩形框0"])-1;

                    HOperatorSet.ReadRegion(out HObject regions1, Path.Combine(Pathstr, "1.hobj"));
                    HOperatorSet.SmallestRectangle1(regions1, out HTuple row11, out HTuple Col11, out HTuple Row21, out HTuple Col21);
                    int idex1 = 0;
                    for (int i = 0; i < hv_rowcenter.Length; i++)
                    {
                        if (hv_rowcenter[i] >= row11 && hv_rowcenter[i] <= Row21 && hv_colcenter[i] >= Col11 && hv_colcenter[i] <= Col21)
                        {
                            idex1 = i;
                            break;
                        }
                    }

                    hv_Indices1 = idex1;



                    hv_Indices1 = Convert.ToInt32(sYJsonObject["矩形框1"])-1;
                }
                catch { }

            }
            else 
            {

                HOperatorSet.TupleMin(hv_distance, out hv_Min);
                HOperatorSet.TupleFind(hv_distance, hv_Min, out hv_Indices);
                HOperatorSet.TupleMax(hv_distance, out hv_Max);
                HOperatorSet.TupleFind(hv_distance, hv_Max, out hv_Indices1);

            }







            if (!(hv_distance.TupleSelect(hv_Indices) > 0 && hv_distance.TupleSelect(hv_Indices) < 100000))
            {
                _win.displayText("Mark点找错", Color.Red, 500, 500);
                State = false;
                return;
            }
            if (!(hv_distance.TupleSelect(hv_Indices1) > 0 && hv_distance.TupleSelect(hv_Indices1) < 100000))
            {
                _win.displayText("Mark点找错", Color.Red, 500, 500);
                State = false;
                return;
            }
            if (!(Radius.TupleSelect(hv_Indices) > 0 && Radius.TupleSelect(hv_Indices) < 111111))
            {
                _win.displayText("Mark尺寸异常", Color.Red, 500, 500);
                State = false;
                return;
            }


            ho_Cross.Dispose();
            HOperatorSet.GenCrossContourXld(out ho_Cross, hv_rowcenter.TupleSelect(hv_Indices),
                hv_colcenter.TupleSelect(hv_Indices), 100, 0.0);
            ho_Cross1.Dispose();
            HOperatorSet.GenCrossContourXld(out ho_Cross1, hv_rowcenter.TupleSelect(hv_Indices1),
                hv_colcenter.TupleSelect(hv_Indices1), 100, 0.0);
            HOperatorSet.AngleLl(0, 0, 50, 0, hv_rowcenter.TupleSelect(hv_Indices), hv_colcenter.TupleSelect(
                hv_Indices), hv_rowcenter.TupleSelect(hv_Indices1), hv_colcenter.TupleSelect(
            hv_Indices1), out hv_Angle);

            //_win.displayObj(ho_Cross1);
            hv_angledown = hv_Angle.TupleDeg();

            HOperatorSet.DistancePp(hv_rowcenter.TupleSelect(hv_Indices), hv_colcenter.TupleSelect(
          hv_Indices), hv_rowcenter.TupleSelect(hv_Indices1), hv_colcenter.TupleSelect(
          hv_Indices1), out HTuple Dis);

            _win.displayObj(ho_Cross);
            _win.displayObj(ho_Cross1);


            _win.displayText("1", Color.Red, Convert.ToInt32(hv_rowcenter.TupleSelect(hv_Indices).O), Convert.ToInt32(hv_colcenter.TupleSelect(hv_Indices).O));

            _win.displayText("2", Color.Red, Convert.ToInt32(hv_rowcenter.TupleSelect(hv_Indices1).O), Convert.ToInt32(hv_colcenter.TupleSelect(hv_Indices1).O));

            outlen = Dis.D;
            if (num == 2)
            {
                if ((Math.Abs(Dis.D - len) > 500.0))
                {
                    _win.displayText("Mark点距离超限", Color.Red, 700, 500);
                    State = false;
                    return;
                }
                if (hv_angledown < (angledown - 20) || hv_angledown > (angledown + 20))
                {
                    _win.displayText($"角度超限,{hv_angledown}", Color.Red, 900, 500);
                    State = false;
                    return;
                }

            }
            

            _win.displayText("角度：" + Convert.ToDouble( hv_angledown.ToString()).ToString("0.000"), Color.Green, 700, 500);
            _win.displayText("X:" + Convert.ToDouble(hv_rowcenter.TupleSelect(hv_Indices).ToString()).ToString("0.000"), Color.Green, 900, 500);
            _win.displayText("Y:" + Convert.ToDouble(hv_colcenter.TupleSelect(hv_Indices).ToString()).ToString("0.000"), Color.Green, 900, 1500);
            ho_Region.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_SelectedRegions.Dispose();
            ho_RegionFillUp.Dispose();
            ho_SelectedRegions1.Dispose();
            ho_ObjectSelected.Dispose();
            ho_ContCircle.Dispose();
            ho_Regions.Dispose();
        }



        private static void FindDownCli(HObject Image, string ColourOne, string Colourtwoe, HTuple SelectShapeOne,HTuple CircleScore, out HObject data, out HObject ResImage, HTuple hv_areamin, HTuple hv_aramax, int DilationCircle = 12)
        {


            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_Image, ho_ImageEmphasize, ho_Region;
            HObject ho_ConnectedRegions, ho_SelectedRegions, ho_ImageResult;
            HObject ho_ImageResult1, ho_Region1, ho_RegionDilation;
            HObject ho_Region2, ho_ConnectedRegions2, ho_SelectedRegions4;
            HObject ho_SelectedRegions3, ho_RegionFillUp, ho_Circle1 = null;
            HObject ho_RegionErosion = null, ho_RegionDifference, ho_ConnectedRegions1;
            HObject ho_SelectedRegions1, ho_SelectedRegions2;

            // Local control variables 

            HTuple hv_UsedThreshold = null, hv_UsedThreshold1 = null;
            HTuple hv_UsedThreshold2 = null, hv_Row2 = null, hv_Column1 = null;
            HTuple hv_Radius2 = null, hv_Number1 = null;
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Image);
            HOperatorSet.GenEmptyObj(out ho_ImageEmphasize);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_ImageResult);
            HOperatorSet.GenEmptyObj(out ho_ImageResult1);
            HOperatorSet.GenEmptyObj(out ho_Region1);
            HOperatorSet.GenEmptyObj(out ho_RegionDilation);
            HOperatorSet.GenEmptyObj(out ho_Region2);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions4);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_Circle1);
            HOperatorSet.GenEmptyObj(out ho_RegionErosion);
            HOperatorSet.GenEmptyObj(out ho_RegionDifference);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions2);
            //Image Acquisition 01: Code generated by Image Acquisition 01
            //open_framegrabber ('File', 1, 1, 0, 0, 0, 0, 'default', -1, 'default', -1, 'false', 'C:/Users/Tsechung/Downloads/20221025 (1)/20221025/标头3', 'default', 1, -1, AcqHandle)

            //* grab_image_start (AcqHandle, -1)
            //* while (true)
            //* grab_image_async (Image, AcqHandle, -1)

            ho_Image.Dispose();
            ho_Image = Image;

            ho_ImageEmphasize.Dispose();
            HOperatorSet.Emphasize(ho_Image, out ho_ImageEmphasize, 17, 17, 10);


            ho_Region.Dispose();
            HOperatorSet.BinaryThreshold(ho_ImageEmphasize, out ho_Region, "max_separability",
                "light", out hv_UsedThreshold);

            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_Region, out ho_ConnectedRegions);


            ho_SelectedRegions.Dispose();
            HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area",
                "and", 0, SelectShapeOne);

            ho_ImageResult.Dispose();
            HOperatorSet.PaintRegion(ho_SelectedRegions, ho_ImageEmphasize, out ho_ImageResult,
                0, "fill");
            ho_ImageResult1.Dispose();
            HOperatorSet.PaintRegion(ho_SelectedRegions, Image, out ho_ImageResult1, 0,
                "fill");
            ResImage = ho_ImageResult1;
            ho_Region1.Dispose();
            HOperatorSet.BinaryThreshold(ho_ImageResult, out ho_Region1, "max_separability",
                ColourOne, out hv_UsedThreshold1);
            ho_RegionDilation.Dispose();
            HOperatorSet.DilationCircle(ho_Region1, out ho_RegionDilation, DilationCircle);

            ho_Region2.Dispose();
            HOperatorSet.BinaryThreshold(ho_ImageResult, out ho_Region2, "max_separability",
               Colourtwoe, out hv_UsedThreshold2);
            ho_ConnectedRegions2.Dispose();
            HOperatorSet.Connection(ho_Region2, out ho_ConnectedRegions2);
            ho_SelectedRegions4.Dispose();
            HOperatorSet.SelectShape(ho_ConnectedRegions2, out ho_SelectedRegions4, "area",
                "and", hv_areamin, hv_aramax);
            ho_SelectedRegions3.Dispose();
            HOperatorSet.SelectShape(ho_SelectedRegions4, out ho_SelectedRegions3, "circularity",
                "and", 0.5, 1);
            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_SelectedRegions3, out ho_RegionFillUp);
            HOperatorSet.SmallestCircle(ho_RegionFillUp, out hv_Row2, out hv_Column1, out hv_Radius2);
            HOperatorSet.CountObj(ho_RegionFillUp, out hv_Number1);
            //此处需判断是否有对象
            if ((int)(new HTuple(hv_Number1.TupleNotEqual(0))) != 0)
            {
                ho_Circle1.Dispose();
                HOperatorSet.GenCircle(out ho_Circle1, hv_Row2, hv_Column1, hv_Radius2);
                ho_RegionErosion.Dispose();
                HOperatorSet.ErosionCircle(ho_Circle1, out ho_RegionErosion, 6);
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.PaintRegion(ho_RegionErosion, ho_ImageResult1, out ExpTmpOutVar_0,
                        0, "fill");
                    ho_ImageResult1 = ExpTmpOutVar_0;
                }

            }

            ho_RegionDifference.Dispose();
            HOperatorSet.Difference(ho_Region2, ho_RegionDilation, out ho_RegionDifference
                );


            ho_ConnectedRegions1.Dispose();
            HOperatorSet.Connection(ho_RegionDifference, out ho_ConnectedRegions1);


            ho_SelectedRegions1.Dispose();
            HOperatorSet.SelectShape(ho_ConnectedRegions1, out ho_SelectedRegions1, "area",
                "and", 800, 12000);


            ho_SelectedRegions2.Dispose();
            HOperatorSet.SelectShape(ho_SelectedRegions1, out ho_SelectedRegions2, "circularity",
                "and", CircleScore, 1);


            data = ho_SelectedRegions2;

            ho_ImageEmphasize.Dispose();
            ho_Region.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_SelectedRegions.Dispose();
            ho_ImageResult.Dispose();
            ho_Region1.Dispose();
            ho_RegionDilation.Dispose();
            ho_Region2.Dispose();
            ho_ConnectedRegions2.Dispose();
            ho_SelectedRegions4.Dispose();
            ho_SelectedRegions3.Dispose();
            ho_RegionFillUp.Dispose();
            ho_Circle1.Dispose();
            ho_RegionErosion.Dispose();
            ho_RegionDifference.Dispose();
            ho_ConnectedRegions1.Dispose();
            ho_SelectedRegions1.Dispose();
        }
        
        public static void Filmalgo(HObject image,string JZ,string sigle,out HTuple max, SYHalconTool _win) 
        {
            max = 0;
            SYJsonObject sYJsonObject = new SYJsonObject();
            String datapath = Path.Combine(AppContext.BaseDirectory, "DATA", JZ);
            string widthpat = Path.Combine(datapath, $"film{sigle}.json");
            string Pathstr = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", JZ, "标头" + sigle, "Up");
            if (!File.Exists(widthpat))
            {
                sYJsonObject["阈值临界点"] = "110";
                sYJsonObject["离心纸检测是否开启"] = "true";
                sYJsonObject["离心纸管控面积"] = "0";
                sYJsonObject["离心纸宽松面积"] = "50000";
                sYJsonObject.Save(widthpat);
            }
            sYJsonObject.FromFile(widthpat);
            HOperatorSet.ReadRegion(out HObject region, Path.Combine(Pathstr, "filmRegion.hobj"));
            HOperatorSet.ReduceDomain(image, region,out HObject imagereduced);
            HOperatorSet.Threshold(imagereduced,out HObject throregion, 
                (int.Parse(sYJsonObject["阈值临界点"])),255);
            HOperatorSet.OpeningCircle(throregion,out HObject regionopening,25);
            HOperatorSet.Connection(throregion,out HObject connectedregion);
            HOperatorSet.SelectShapeStd(connectedregion,out HObject selectdregions, "max_area",70);
            HOperatorSet.AreaCenter(connectedregion, out HTuple area,out HTuple row,out HTuple col);
            HOperatorSet.TupleMax(area,out max);
            _win.displayText(max.ToString(), Color.Green, 500, 500);
            _win.displayObj(selectdregions);

        }


        public static void Filmalgo_PCB(HObject image, string JZ, string sigle, out HTuple max, SYHalconTool _win)
        {
            try
            {
                max = 0;
                SYJsonObject sYJsonObject = new SYJsonObject();
                String datapath = Path.Combine(AppContext.BaseDirectory, "DATA", JZ);
                string widthpat = Path.Combine(datapath, $"PCBCheck{sigle}.json");
                string Pathstr = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", JZ, "标头" + sigle, "Up");
                if (!File.Exists(widthpat))
                {
                    sYJsonObject["组装检测是否开启"] = "true";
                    sYJsonObject["组装面积管控面积"] = "0";
                    sYJsonObject["组装面积宽松面积"] = "5000";
                    sYJsonObject.Save(widthpat);
                }
                sYJsonObject.FromFile(widthpat);
                HOperatorSet.ReadRegion(out HObject region, Path.Combine(Pathstr, "PCBCheck.hobj"));
                HOperatorSet.ReduceDomain(image, region, out HObject imagereduced);                
                HOperatorSet.BinaryThreshold(imagereduced, out HObject ho_Region, "max_separability",
            "dark", out HTuple hv_UsedThreshold);
                HOperatorSet.Connection(ho_Region, out HObject connectedregion);
                HOperatorSet.AreaCenter(connectedregion, out HTuple area, out HTuple row, out HTuple col);
                HOperatorSet.TupleMax(area, out max);
                HOperatorSet.TupleFind(area, max, out HTuple hv_Indices);
                HOperatorSet.SelectObj(connectedregion, out HObject objectselect, hv_Indices + 1);
                HOperatorSet.FillUp(objectselect, out HObject regionfillup);
                HOperatorSet.Difference(region, regionfillup, out HObject regiondifference);
                HOperatorSet.PaintRegion(regiondifference, image,out HObject imageresult,255,"fill");
                HOperatorSet.PaintRegion(regionfillup, imageresult, out HObject imageresult1, 0, "fill");
                HOperatorSet.BinaryThreshold(imageresult1, out HObject ho_Region11, "max_separability",
           "light", out HTuple hv_UsedThreshold1);
                HOperatorSet.ClosingCircle(ho_Region11,out HObject regionclosing,25);
                HOperatorSet.PaintRegion(regionclosing, imageresult1, out HObject imageresult2, 255, "fill");
                HOperatorSet.Difference(region, regionclosing, out HObject regiondifference1);
                HOperatorSet.Boundary(regiondifference1, out HObject regioborder, "inner");
                HOperatorSet.DilationRectangle1(regioborder, out HObject regiondilation, 10, 10);
                HOperatorSet.ReduceDomain(imageresult2, regiondilation, out HObject imagereduced1);
                HOperatorSet.EdgesSubPix(imagereduced1, out HObject ho_Edges, "canny", 15, 20, 120);
                HOperatorSet.FitRectangle2ContourXld(ho_Edges, "tukey", -1, 0, 0, 3, 2, out HTuple hv_Row,
                    out HTuple hv_Column, out HTuple hv_Phi, out HTuple hv_Length1, out HTuple hv_Length2, out HTuple hv_PointOrder);
                HOperatorSet.GenRectangle2ContourXld(out HObject ho_Rectangles, hv_Row, hv_Column, hv_Phi,
                    hv_Length1, hv_Length2);
                //_win.displayText(hv_Row.ToString(), Color.Green, 1500, 500);
                //_win.displayText(hv_Column.ToString(), Color.Green, 2500, 500);
                HOperatorSet.GenRegionContourXld(ho_Rectangles, out HObject ho_Region1, "filled");
                HOperatorSet.AreaCenter(ho_Region1, out max, out HTuple r, out HTuple c);
                _win.displayText(max.ToString(), Color.Green, 800, 500);
                _win.displayObj(ho_Region1);
            }
            catch (Exception)
            {
                _win.displayText("参数设置错误", Color.Green, 800, 500);
                max = 100000000;
            }
           

        }




        public static void Filmalgo_PCBTest1(HObject image, string JZ, string sigle, out HTuple max, SYHalconTool _win)
        {
            try
            {
                max = 0;
                SYJsonObject sYJsonObject = new SYJsonObject();
                String datapath = Path.Combine(AppContext.BaseDirectory, "DATA", JZ);
                string widthpat = Path.Combine(datapath, $"PCBCheck{sigle}.json");
                string Pathstr = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", JZ, "标头" + sigle, "Up");
                if (!File.Exists(widthpat))
                {
                    sYJsonObject["组装检测是否开启"] = "true";
                    sYJsonObject["组装面积管控面积"] = "0";
                    sYJsonObject["组装面积宽松面积"] = "5000";

                    sYJsonObject["组装面积最大"] = "5000";
                    sYJsonObject["组装面积最小"] = "-5000";


                    sYJsonObject.Save(widthpat);
                }
                sYJsonObject.FromFile(widthpat);



                HOperatorSet.ReadRegion(out HObject region, Path.Combine(Pathstr, "PCBCheck.hobj"));
                HOperatorSet.ReduceDomain(image, region, out HObject imagereduced);


                //threshold(Image, Region1, 128, 255)
                //connection(Region1, ConnectedRegions1)
                //select_shape_std(ConnectedRegions1, SelectedRegions1, 'max_area', 70)
                //shape_trans(SelectedRegions1, RegionTrans, 'convex')
                //reduce_domain(Image, RegionTrans, ImageReduced)

                //threshold(ImageReduced, Region, 0, 128)
                //connection(Region, ConnectedRegions)
                //select_shape_std(ConnectedRegions, SelectedRegions, 'max_area', 70)

                HOperatorSet.Threshold(imagereduced, out HObject regions, 128,255);
                HOperatorSet.Connection(regions, out regions);
                HOperatorSet.SelectShapeStd(regions, out regions, "max_area", 70);

                HOperatorSet.ShapeTrans(regions, out regions, "convex");
                HOperatorSet.ReduceDomain(imagereduced, regions, out HObject nreImage);

                HOperatorSet.Threshold(nreImage, out HObject regions1, 0, 200);
                HOperatorSet.Connection(regions1, out regions1);

                HOperatorSet.SelectShapeStd(regions1, out regions1, "max_area", 70);

                HOperatorSet.AreaCenter(regions1, out max, out HTuple r, out HTuple c);

                _win.displayText(max.ToString(), Color.Green, 800, 500);
                _win.displayObj(regions1);
            }
            catch (Exception)
            {
                _win.displayText("参数设置错误", Color.Green, 800, 500);
                max = 100000000;
            }


        }

        public static void Filmalgo_PCBTest(HObject image, string JZ, string sigle, out HTuple max, SYHalconTool _win)
        {
            try
            {
                max = 0;
                SYJsonObject sYJsonObject = new SYJsonObject();
                String datapath = Path.Combine(AppContext.BaseDirectory, "DATA", JZ);
                string widthpat = Path.Combine(datapath, $"PCBCheck{sigle}.json");
                string Pathstr = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", JZ, "标头" + sigle, "Up");
                if (!File.Exists(widthpat))
                {
                    sYJsonObject["组装检测是否开启"] = "true";
                    sYJsonObject["组装面积管控面积"] = "0";
                    sYJsonObject["组装面积宽松面积"] = "5000";
                    sYJsonObject["组装面积最大"] = "5000";
                    sYJsonObject["组装面积最小"] = "-5000";

                    sYJsonObject["腐蚀宽度"] = "5";
                    sYJsonObject["腐蚀高度"] = "5";

                    sYJsonObject["膨胀宽度"] = "5";
                    sYJsonObject["膨胀高度"] = "5";


                    sYJsonObject.Save(widthpat);
                }
                sYJsonObject.FromFile(widthpat);



                HOperatorSet.ReadRegion(out HObject region, Path.Combine(Pathstr, "PCBCheck.hobj"));
                HOperatorSet.ReduceDomain(image, region, out HObject imagereduced);



                HObject  ho_ROI_0;
                HObject ho_ImageReduced, ho_Region, ho_ConnectedRegions;
                HObject ho_SelectedRegions, ho_Rectangle, ho_Line1, ho_Contour;
                HObject ho_Rectangle2;

                // Local control variables 

                HTuple hv_UsedThreshold = null, hv_Row1 = null;
                HTuple hv_Column1 = null, hv_Row2 = null, hv_Column2 = null;
                HTuple hv_daterow1 = null, hv_datecol1 = null, hv_ResultRow = null;
                HTuple hv_ResultColumn = null, hv_Row111 = null, hv_Column111 = null;
                HTuple hv_Row211 = null, hv_Column211 = null, hv_ResultRow1 = null;
                HTuple hv_ResultColumn1 = null, hv_ResultRow2 = null, hv_ResultColumn2 = null;
                HTuple hv_ResultRow3 = null, hv_ResultColumn3 = null, hv_Row4 = null;
                HTuple hv_Column4 = null, hv_Phi1 = null, hv_Length11 = null;
                HTuple hv_Length21 = null, hv_PointOrder = null, hv_Area = null;
                HTuple hv_Row = null, hv_Column = null, hv_PointOrder1 = null;
                // Initialize local and output iconic variables 
                HOperatorSet.GenEmptyObj(out ho_ROI_0);
                HOperatorSet.GenEmptyObj(out ho_ImageReduced);
                HOperatorSet.GenEmptyObj(out ho_Region);
                HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
                HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
                HOperatorSet.GenEmptyObj(out ho_Rectangle);
                HOperatorSet.GenEmptyObj(out ho_Line1);
                HOperatorSet.GenEmptyObj(out ho_Contour);
                HOperatorSet.GenEmptyObj(out ho_Rectangle2);

                ho_Region.Dispose();
                HOperatorSet.BinaryThreshold(imagereduced, out ho_Region, "max_separability",
                    "dark", out hv_UsedThreshold);


                // 
                // erosion_rectangle1(Region, RegionErosion, 5, 5)

                HOperatorSet.ErosionRectangle1(ho_Region, out ho_Region, Convert.ToInt32(sYJsonObject["腐蚀宽度"]), Convert.ToInt32(sYJsonObject["腐蚀高度"]));
                HOperatorSet.DilationRectangle1(ho_Region, out ho_Region, Convert.ToInt32(sYJsonObject["膨胀宽度"]), Convert.ToInt32(sYJsonObject["膨胀高度"]));



                ho_ConnectedRegions.Dispose();
                HOperatorSet.Connection(ho_Region, out ho_ConnectedRegions);

                ho_SelectedRegions.Dispose();
                HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area",
                    "and", 30000, 9999999);

                HOperatorSet.SmallestRectangle1(ho_SelectedRegions, out hv_Row1, out hv_Column1,
                    out hv_Row2, out hv_Column2);
                ho_Rectangle.Dispose();
                HOperatorSet.GenRectangle1(out ho_Rectangle, hv_Row1, hv_Column1, hv_Row2, hv_Column2);


                hv_daterow1 = new HTuple();


                hv_datecol1 = new HTuple();


                ho_ROI_0.Dispose();
                HOperatorSet.GenRegionLine(out ho_ROI_0, hv_Row1, hv_Column1, hv_Row2 - (hv_Row2 - hv_Row1),
                    hv_Column2);

                ho_ROI_0.Dispose();
                rake(image, out ho_ROI_0, 300, 40, 15, 1, 20, "all", "max",
                    hv_Row1, hv_Column1, hv_Row2 - (hv_Row2 - hv_Row1), hv_Column2, out hv_ResultRow,
                    out hv_ResultColumn);


                // pts_to_best_line (Line, ResultRow, ResultColumn, 2, Row11, Column11, Row21, Column21)

                pts_to_best_line(out HObject holine, hv_ResultRow, hv_ResultColumn, 2, out HTuple de1, out HTuple de2, out HTuple de3, out HTuple de4);

                HOperatorSet.TupleConcat(hv_daterow1, hv_ResultRow, out hv_daterow1);
                HOperatorSet.TupleConcat(hv_datecol1, hv_ResultColumn, out hv_datecol1);


                ho_Line1.Dispose();
                pts_to_best_line(out ho_Line1, hv_ResultRow, hv_ResultColumn, 2, out hv_Row111,
                    out hv_Column111, out hv_Row211, out hv_Column211);


                ho_ROI_0.Dispose();
                HOperatorSet.GenRegionLine(out ho_ROI_0, hv_Row1, hv_Column1, hv_Row1 + (hv_Row2 - hv_Row1),
                    hv_Column1);

                ho_ROI_0.Dispose();
                rake(image, out ho_ROI_0, 300, 40, 15, 1, 20, "all", "max",
                    hv_Row1, hv_Column1, hv_Row1 + (hv_Row2 - hv_Row1), hv_Column1, out hv_ResultRow1,
                    out hv_ResultColumn1);

                HOperatorSet.TupleConcat(hv_daterow1, hv_ResultRow1, out hv_daterow1);
                HOperatorSet.TupleConcat(hv_datecol1, hv_ResultColumn1, out hv_datecol1);

                ho_ROI_0.Dispose();
                HOperatorSet.GenRegionLine(out ho_ROI_0, hv_Row1 + (hv_Row2 - hv_Row1), hv_Column1,
                    hv_Row1 + (hv_Row2 - hv_Row1), hv_Column2);

                ho_ROI_0.Dispose();
                rake(image, out ho_ROI_0, 300, 40, 15, 1, 20, "all", "max",
                    hv_Row1 + (hv_Row2 - hv_Row1), hv_Column1, hv_Row1 + (hv_Row2 - hv_Row1), hv_Column2,
                    out hv_ResultRow2, out hv_ResultColumn2);
                HOperatorSet.TupleConcat(hv_daterow1, hv_ResultRow2, out hv_daterow1);
                HOperatorSet.TupleConcat(hv_datecol1, hv_ResultColumn2, out hv_datecol1);


                ho_ROI_0.Dispose();
                HOperatorSet.GenRegionLine(out ho_ROI_0, hv_Row2, hv_Column2, hv_Row2 - (hv_Row2 - hv_Row1),
                    hv_Column2);

                ho_ROI_0.Dispose();
                rake(image, out ho_ROI_0, 300, 40, 15, 1, 20, "all", "max",
                    hv_Row2, hv_Column2, hv_Row2 - (hv_Row2 - hv_Row1), hv_Column2, out hv_ResultRow3,
                    out hv_ResultColumn3);
                HOperatorSet.TupleConcat(hv_daterow1, hv_ResultRow3, out hv_daterow1);
                HOperatorSet.TupleConcat(hv_datecol1, hv_ResultColumn3, out hv_datecol1);


                ho_Contour.Dispose();
                HOperatorSet.GenContourPolygonXld(out ho_Contour, hv_daterow1, hv_datecol1);

                HOperatorSet.FitRectangle2ContourXld(ho_Contour, "regression", -1, 0, 0, 3, 2,
                    out hv_Row4, out hv_Column4, out hv_Phi1, out hv_Length11, out hv_Length21,
                    out hv_PointOrder);

                ho_Rectangle2.Dispose();
                HOperatorSet.GenRectangle2ContourXld(out ho_Rectangle2, hv_Row4, hv_Column4,
                    hv_Phi1, hv_Length11, hv_Length21);

                HOperatorSet.AreaCenterXld(ho_Rectangle2, out max, out hv_Row, out hv_Column,
                    out hv_PointOrder1);

                ho_ROI_0.Dispose();
                ho_ImageReduced.Dispose();
                ho_Region.Dispose();
                ho_ConnectedRegions.Dispose();
                ho_SelectedRegions.Dispose();
                ho_Rectangle.Dispose();
                ho_Line1.Dispose();
                ho_Contour.Dispose();
                string ShowMax = Convert.ToDouble(max.ToString()).ToString("0.000");
                _win.displayText(ShowMax, Color.Green, 800, 500);
                _win.displayObj(ho_Rectangle2);
                SYGlobal.AddLogFrom($"组装到位Max:{ShowMax}", BgColorGrade.Red);
            }
            catch (Exception)
            {
                _win.displayText("参数设置错误", Color.Green, 800, 500);
                max = 100000000;
            }


        }



        #region


        public static void pts_to_best_line(out HObject ho_Line, HTuple hv_Rows, HTuple hv_Cols,
            HTuple hv_ActiveNum, out HTuple hv_Row1, out HTuple hv_Column1, out HTuple hv_Row2,
            out HTuple hv_Column2)
        {



            // Local iconic variables 

            HObject ho_Contour = null;

            // Local control variables 

            HTuple hv_Length = null, hv_Nr = new HTuple();
            HTuple hv_Nc = new HTuple(), hv_Dist = new HTuple(), hv_Length1 = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Line);
            HOperatorSet.GenEmptyObj(out ho_Contour);
            //初始化
            hv_Row1 = 0;
            hv_Column1 = 0;
            hv_Row2 = 0;
            hv_Column2 = 0;
            //产生一个空的直线对象，用于保存拟合后的直线
            ho_Line.Dispose();
            HOperatorSet.GenEmptyObj(out ho_Line);
            //计算边缘数量
            HOperatorSet.TupleLength(hv_Cols, out hv_Length);
            //当边缘数量不小于有效点数时进行拟合
            if ((int)((new HTuple(hv_Length.TupleGreaterEqual(hv_ActiveNum))).TupleAnd(new HTuple(hv_ActiveNum.TupleGreater(
                1)))) != 0)
            {
                //halcon的拟合是基于xld的，需要把边缘连接成xld
                ho_Contour.Dispose();
                HOperatorSet.GenContourPolygonXld(out ho_Contour, hv_Rows, hv_Cols);
                //拟合直线。使用的算法是'tukey'，其他算法请参考fit_line_contour_xld的描述部分。
                HOperatorSet.FitLineContourXld(ho_Contour, "tukey", -1, 0, 5, 2, out hv_Row1,
                    out hv_Column1, out hv_Row2, out hv_Column2, out hv_Nr, out hv_Nc, out hv_Dist);
                //判断拟合结果是否有效：如果拟合成功，数组中元素的数量大于0
                HOperatorSet.TupleLength(hv_Dist, out hv_Length1);
                if ((int)(new HTuple(hv_Length1.TupleLess(1))) != 0)
                {
                    ho_Contour.Dispose();

                    return;
                }
                //根据拟合结果，产生直线xld
                ho_Line.Dispose();
                HOperatorSet.GenContourPolygonXld(out ho_Line, hv_Row1.TupleConcat(hv_Row2),
                    hv_Column1.TupleConcat(hv_Column2));
            }

            ho_Contour.Dispose();

            return;
        }

        public static void rake(HObject ho_Image, out HObject ho_Regions, HTuple hv_Elements,
            HTuple hv_DetectHeight, HTuple hv_DetectWidth, HTuple hv_Sigma, HTuple hv_Threshold,
            HTuple hv_Transition, HTuple hv_Select, HTuple hv_Row1, HTuple hv_Column1, HTuple hv_Row2,
            HTuple hv_Column2, out HTuple hv_ResultRow, out HTuple hv_ResultColumn)
        {




            // Stack for temporary objects 
            HObject[] OTemp = new HObject[20];

            // Local iconic variables 

            HObject ho_RegionLines, ho_Rectangle = null;
            HObject ho_Arrow1 = null;

            // Local control variables 

            HTuple hv_Width = null, hv_Height = null, hv_ATan = null;
            HTuple hv_i = null, hv_RowC = new HTuple(), hv_ColC = new HTuple();
            HTuple hv_Distance = new HTuple(), hv_RowL2 = new HTuple();
            HTuple hv_RowL1 = new HTuple(), hv_ColL2 = new HTuple();
            HTuple hv_ColL1 = new HTuple(), hv_MsrHandle_Measure = new HTuple();
            HTuple hv_RowEdge = new HTuple(), hv_ColEdge = new HTuple();
            HTuple hv_Amplitude = new HTuple(), hv_tRow = new HTuple();
            HTuple hv_tCol = new HTuple(), hv_t = new HTuple(), hv_Number = new HTuple();
            HTuple hv_j = new HTuple();
            HTuple hv_DetectWidth_COPY_INP_TMP = hv_DetectWidth.Clone();
            HTuple hv_Select_COPY_INP_TMP = hv_Select.Clone();
            HTuple hv_Transition_COPY_INP_TMP = hv_Transition.Clone();

            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_RegionLines);
            HOperatorSet.GenEmptyObj(out ho_Rectangle);
            HOperatorSet.GenEmptyObj(out ho_Arrow1);
            //获取图像尺寸
            HOperatorSet.GetImageSize(ho_Image, out hv_Width, out hv_Height);
            //产生一个空显示对象，用于显示
            ho_Regions.Dispose();
            HOperatorSet.GenEmptyObj(out ho_Regions);
            //初始化边缘坐标数组
            hv_ResultRow = new HTuple();
            hv_ResultColumn = new HTuple();
            //产生直线xld
            ho_RegionLines.Dispose();
            HOperatorSet.GenContourPolygonXld(out ho_RegionLines, hv_Row1.TupleConcat(hv_Row2),
                hv_Column1.TupleConcat(hv_Column2));
            //存储到显示对象
            {
                HObject ExpTmpOutVar_0;
                HOperatorSet.ConcatObj(ho_Regions, ho_RegionLines, out ExpTmpOutVar_0);
                ho_Regions.Dispose();
                ho_Regions = ExpTmpOutVar_0;
            }
            //计算直线与x轴的夹角，逆时针方向为正向。
            HOperatorSet.AngleLx(hv_Row1, hv_Column1, hv_Row2, hv_Column2, out hv_ATan);

            //边缘检测方向垂直于检测直线：直线方向正向旋转90°为边缘检测方向
            hv_ATan = hv_ATan + ((new HTuple(90)).TupleRad());

            //根据检测直线按顺序产生测量区域矩形，并存储到显示对象
            HTuple end_val18 = hv_Elements;
            HTuple step_val18 = 1;
            for (hv_i = 1; hv_i.Continue(end_val18, step_val18); hv_i = hv_i.TupleAdd(step_val18))
            {
                //RowC := Row1+(((Row2-Row1)*i)/(Elements+1))
                //ColC := Column1+(Column2-Column1)*i/(Elements+1)
                //if (RowC>Height-1 or RowC<0 or ColC>Width-1 or ColC<0)
                //continue
                //endif
                //如果只有一个测量矩形，作为卡尺工具，宽度为检测直线的长度
                if ((int)(new HTuple(hv_Elements.TupleEqual(1))) != 0)
                {
                    hv_RowC = (hv_Row1 + hv_Row2) * 0.5;
                    hv_ColC = (hv_Column1 + hv_Column2) * 0.5;
                    //判断是否超出图像,超出不检测边缘
                    if ((int)((new HTuple((new HTuple((new HTuple(hv_RowC.TupleGreater(hv_Height - 1))).TupleOr(
                        new HTuple(hv_RowC.TupleLess(0))))).TupleOr(new HTuple(hv_ColC.TupleGreater(
                        hv_Width - 1))))).TupleOr(new HTuple(hv_ColC.TupleLess(0)))) != 0)
                    {
                        continue;
                    }
                    HOperatorSet.DistancePp(hv_Row1, hv_Column1, hv_Row2, hv_Column2, out hv_Distance);
                    hv_DetectWidth_COPY_INP_TMP = hv_Distance.Clone();
                    ho_Rectangle.Dispose();
                    HOperatorSet.GenRectangle2ContourXld(out ho_Rectangle, hv_RowC, hv_ColC,
                        hv_ATan, hv_DetectHeight / 2, hv_Distance / 2);
                }
                else
                {
                    //如果有多个测量矩形，产生该测量矩形xld
                    hv_RowC = hv_Row1 + (((hv_Row2 - hv_Row1) * (hv_i - 1)) / (hv_Elements - 1));
                    hv_ColC = hv_Column1 + (((hv_Column2 - hv_Column1) * (hv_i - 1)) / (hv_Elements - 1));
                    //判断是否超出图像,超出不检测边缘
                    if ((int)((new HTuple((new HTuple((new HTuple(hv_RowC.TupleGreater(hv_Height - 1))).TupleOr(
                        new HTuple(hv_RowC.TupleLess(0))))).TupleOr(new HTuple(hv_ColC.TupleGreater(
                        hv_Width - 1))))).TupleOr(new HTuple(hv_ColC.TupleLess(0)))) != 0)
                    {
                        continue;
                    }
                    ho_Rectangle.Dispose();
                    HOperatorSet.GenRectangle2ContourXld(out ho_Rectangle, hv_RowC, hv_ColC,
                        hv_ATan, hv_DetectHeight / 2, hv_DetectWidth_COPY_INP_TMP / 2);
                }

                //把测量矩形xld存储到显示对象
                {
                    HObject ExpTmpOutVar_0;
                    HOperatorSet.ConcatObj(ho_Regions, ho_Rectangle, out ExpTmpOutVar_0);
                    ho_Regions.Dispose();
                    ho_Regions = ExpTmpOutVar_0;
                }
                if ((int)(new HTuple(hv_i.TupleEqual(1))) != 0)
                {
                    //在第一个测量矩形绘制一个箭头xld，用于只是边缘检测方向
                    hv_RowL2 = hv_RowC + ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleSin()));
                    hv_RowL1 = hv_RowC - ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleSin()));
                    hv_ColL2 = hv_ColC + ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleCos()));
                    hv_ColL1 = hv_ColC - ((hv_DetectHeight / 2) * (((-hv_ATan)).TupleCos()));
                    ho_Arrow1.Dispose();
                    gen_arrow_contour_xld(out ho_Arrow1, hv_RowL1, hv_ColL1, hv_RowL2, hv_ColL2,
                        25, 25);
                    //把xld存储到显示对象
                    {
                        HObject ExpTmpOutVar_0;
                        HOperatorSet.ConcatObj(ho_Regions, ho_Arrow1, out ExpTmpOutVar_0);
                        ho_Regions.Dispose();
                        ho_Regions = ExpTmpOutVar_0;
                    }
                }
                //产生测量对象句柄
                HOperatorSet.GenMeasureRectangle2(hv_RowC, hv_ColC, hv_ATan, hv_DetectHeight / 2,
                    hv_DetectWidth_COPY_INP_TMP / 2, hv_Width, hv_Height, "nearest_neighbor",
                    out hv_MsrHandle_Measure);

                //设置极性
                if ((int)(new HTuple(hv_Transition_COPY_INP_TMP.TupleEqual("negative"))) != 0)
                {
                    hv_Transition_COPY_INP_TMP = "negative";
                }
                else
                {
                    if ((int)(new HTuple(hv_Transition_COPY_INP_TMP.TupleEqual("positive"))) != 0)
                    {

                        hv_Transition_COPY_INP_TMP = "positive";
                    }
                    else
                    {
                        hv_Transition_COPY_INP_TMP = "all";
                    }
                }
                //设置边缘位置。最强点是从所有边缘中选择幅度绝对值最大点，需要设置为'all'
                if ((int)(new HTuple(hv_Select_COPY_INP_TMP.TupleEqual("first"))) != 0)
                {
                    hv_Select_COPY_INP_TMP = "first";
                }
                else
                {
                    if ((int)(new HTuple(hv_Select_COPY_INP_TMP.TupleEqual("last"))) != 0)
                    {

                        hv_Select_COPY_INP_TMP = "last";
                    }
                    else
                    {
                        hv_Select_COPY_INP_TMP = "all";
                    }
                }
                //检测边缘
                HOperatorSet.MeasurePos(ho_Image, hv_MsrHandle_Measure, hv_Sigma, hv_Threshold,
                    hv_Transition_COPY_INP_TMP, hv_Select_COPY_INP_TMP, out hv_RowEdge, out hv_ColEdge,
                    out hv_Amplitude, out hv_Distance);
                //清除测量对象句柄
                HOperatorSet.CloseMeasure(hv_MsrHandle_Measure);

                //临时变量初始化
                //tRow，tCol保存找到指定边缘的坐标
                hv_tRow = 0;
                hv_tCol = 0;
                //t保存边缘的幅度绝对值
                hv_t = 0;
                //找到的边缘必须至少为1个
                HOperatorSet.TupleLength(hv_RowEdge, out hv_Number);
                if ((int)(new HTuple(hv_Number.TupleLess(1))) != 0)
                {
                    continue;
                }
                //有多个边缘时，选择幅度绝对值最大的边缘
                HTuple end_val100 = hv_Number - 1;
                HTuple step_val100 = 1;
                for (hv_j = 0; hv_j.Continue(end_val100, step_val100); hv_j = hv_j.TupleAdd(step_val100))
                {
                    if ((int)(new HTuple(((((hv_Amplitude.TupleSelect(hv_j))).TupleAbs())).TupleGreater(
                        hv_t))) != 0)
                    {

                        hv_tRow = hv_RowEdge.TupleSelect(hv_j);
                        hv_tCol = hv_ColEdge.TupleSelect(hv_j);
                        hv_t = ((hv_Amplitude.TupleSelect(hv_j))).TupleAbs();
                    }
                }
                //把找到的边缘保存在输出数组
                if ((int)(new HTuple(hv_t.TupleGreater(0))) != 0)
                {
                    hv_ResultRow = hv_ResultRow.TupleConcat(hv_tRow);
                    hv_ResultColumn = hv_ResultColumn.TupleConcat(hv_tCol);
                }
            }

            ho_RegionLines.Dispose();
            ho_Rectangle.Dispose();
            ho_Arrow1.Dispose();

            return;
        }
        #endregion



        public static void FindDownCliTwo(HObject Image, HTuple hv_areamin, HTuple hv_aramax, HTuple CircleScore, out HObject data, out HObject ResImage, HTuple ramin, HTuple ramax, int DilationCircle = 1)
        {

            // Local iconic variables 

            HObject ho_Image = null, ho_ImageEmphasize, ho_Region;
            HObject ho_ConnectedRegions1, ho_RegionFillUp, ho_SelectedRegions2;
            HObject ho_SelectedRegions3, ho_Circle1, ho_ImageResult;
            HObject ho_Region1, ho_ConnectedRegions, ho_SelectedRegions;
            HObject ho_SelectedRegions1;

            // Local control variables 

            HTuple hv_UsedThreshold = null, hv_Row2 = null;
            HTuple hv_Column1 = null, hv_Radius2 = null, hv_UsedThreshold1 = null;
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Image);
            HOperatorSet.GenEmptyObj(out ho_ImageEmphasize);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions2);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions3);
            HOperatorSet.GenEmptyObj(out ho_Circle1);
            HOperatorSet.GenEmptyObj(out ho_ImageResult);
            HOperatorSet.GenEmptyObj(out ho_Region1);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            //read_image (Image, ImageFiles[Index])
            //Image Acquisition 01: Do something
            ho_ImageEmphasize.Dispose();
            ho_Image = Image;

            HOperatorSet.Emphasize(ho_Image, out ho_ImageEmphasize, 50, 50, 50);
            ho_Region.Dispose();
            HOperatorSet.BinaryThreshold(ho_ImageEmphasize, out ho_Region, "max_separability",
                "light", out hv_UsedThreshold);
            ho_ConnectedRegions1.Dispose();
            HOperatorSet.Connection(ho_Region, out ho_ConnectedRegions1);
            ho_SelectedRegions2.Dispose();
            HOperatorSet.SelectShape(ho_ConnectedRegions1, out ho_SelectedRegions2, "roundness",
                "and", CircleScore, 1);
            ho_SelectedRegions3.Dispose();
            HOperatorSet.SelectShape(ho_SelectedRegions2, out ho_SelectedRegions3, "area",
                "and", hv_areamin, hv_aramax);
            HOperatorSet.SelectShape(ho_SelectedRegions3, out HObject ho_SelectedRegions4, "ra",
    "and", ramin, ramax );
            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_SelectedRegions4, out ho_RegionFillUp);
            HOperatorSet.SelectShape(ho_RegionFillUp, out HObject ho_SelectedRegions8, "roundness",
"and", CircleScore+0.2, 1);
            HOperatorSet.SmallestCircle(ho_SelectedRegions8, out hv_Row2, out hv_Column1,
                out hv_Radius2);
            ho_Circle1.Dispose();
            HOperatorSet.GenCircle(out ho_Circle1, hv_Row2, hv_Column1, hv_Radius2);
            ho_ImageResult.Dispose();
            HOperatorSet.PaintRegion(ho_Circle1, ho_Image, out ho_ImageResult, 255, "fill");
            //ho_Region1.Dispose();
            //HOperatorSet.BinaryThreshold(ho_ImageResult, out ho_Region1, "max_separability",
            //    "light", out hv_UsedThreshold1);
            //ho_ConnectedRegions.Dispose();
            //HOperatorSet.Connection(ho_Region1, out ho_ConnectedRegions);
            //ho_SelectedRegions.Dispose();
            //HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area",
            //    "and", hv_areamin+5000, hv_aramax);
            //ho_SelectedRegions1.Dispose();
            //HOperatorSet.SelectShape(ho_SelectedRegions, out ho_SelectedRegions1, "circularity",
            //    "and", CircleScore, 1);
            //HOperatorSet.SmallestCircle(ho_SelectedRegions1, out hv_Row2, out hv_Column1,
            //    out hv_Radius2);
            data = ho_Circle1;
            ResImage = ho_ImageResult;


            ho_ImageEmphasize.Dispose();
            ho_Region.Dispose();    
            ho_ConnectedRegions1.Dispose();
            ho_RegionFillUp.Dispose();
            ho_SelectedRegions2.Dispose();
            ho_SelectedRegions3.Dispose();
            ho_Region1.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_SelectedRegions.Dispose();

        }

        public static void BKTAlgorithmicUp(string singernum, String JZ, SYHalconTool _win,HObject ho_Image, int num, double len, out HObject ho_Cross11, 
     out HObject ho_Cross22, HTuple hv_areamin, HTuple hv_aramax, HTuple hv_circularitymin,
     HTuple hv_circularitymax, HTuple hv_Circlepixnumber, HObject ReduImage, out HTuple hv_Indicess,
     out HTuple hv_Indicess11, out HTuple hv_angelup, out HTuple hv_distance1, out HTuple hv_rowcenter1,
     out HTuple hv_colcenter1, out bool State ,double angleup, out HTuple area, out HTuple Dislen)
        {
            area = 0;
            Dislen = 0;
            State = true;
            HObject ho_Region, ho_ConnectedRegions, ho_SelectedRegions;
            HObject ho_SelectedRegions1, ho_ObjectSelected = null, ho_ContCircle = null;
            HObject ho_Regions = null;

            // Local control variables 

            HTuple hv_UsedThreshold = null, hv_Number = null;
            HTuple hv_Index = new HTuple(), hv_Row = new HTuple();
            HTuple hv_Column = new HTuple(), hv_Radius = new HTuple();
            HTuple hv_Row1 = new HTuple(), hv_Col = new HTuple(), hv_ResultRow = new HTuple();
            HTuple hv_ResultColumn = new HTuple(), hv_ArcType = new HTuple();
            HTuple hv_RowCenter = new HTuple(), hv_ColCenter = new HTuple();
            HTuple hv_Radius1 = new HTuple(), hv_StartPhi = new HTuple();
            HTuple hv_EndPhi = new HTuple(), hv_PointOrder = new HTuple();
            HTuple hv_ArcAngle = new HTuple(), hv_Distance1 = new HTuple();
            HTuple hv_Min = new HTuple(), hv_Max = new HTuple(), hv_Angle = new HTuple();
            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Cross11);
            HOperatorSet.GenEmptyObj(out ho_Cross22);
            HOperatorSet.GenEmptyObj(out ho_Region);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions1);
            HOperatorSet.GenEmptyObj(out ho_ObjectSelected);
            HOperatorSet.GenEmptyObj(out ho_ContCircle);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            hv_angelup = new HTuple();
            hv_Indicess = new HTuple();
            hv_Indicess11 = new HTuple();
            ho_Region.Dispose();
          
            if (ReduImage!=null)
            {
                HOperatorSet.ReduceDomain(ho_Image, ReduImage, out ho_Image);
                _win.displayObj(ReduImage);
            }


            SYJsonObject sYJsonObject = new SYJsonObject();
            String datapath = Path.Combine(AppContext.BaseDirectory, "DATA", JZ);
            string widthpat = Path.Combine(datapath, $"UpPar{singernum.ToString()}.json");

            if (!Directory.Exists(datapath))
            {
                Directory.CreateDirectory(datapath);
            }

            sYJsonObject["卡尺"] = "45";
            sYJsonObject["寻找方式"] = "max";
            sYJsonObject["卡尺极性"] = "negative";
            sYJsonObject["筛选面积"] = "1800";
            sYJsonObject["圆度"] = "0.4";
            sYJsonObject["第一次筛选颜色"] = "light";
            sYJsonObject["第二次筛选颜色"] = "dark";
            sYJsonObject["膨胀系数"] = "5";
            sYJsonObject["白True黑False"] = "False";
            sYJsonObject["找圆半径最小值"] = "60";
            sYJsonObject["找圆半径最大值"] = "90";

            sYJsonObject["是否开启标头定义"] = "false";
            sYJsonObject["矩形框0"] = "1";
            sYJsonObject["矩形框1"] = "2";

            if (!File.Exists(widthpat))
            {
                sYJsonObject.Save(widthpat);

            }
            else
            {
                SYJsonObject sYJsonObject1 = new SYJsonObject();
                sYJsonObject1.FromFile(widthpat);

                List<string> _data1 = sYJsonObject.GetKeys();
                List<string> _data2 = sYJsonObject1.GetKeys();

                if (_data1.Count != _data2.Count)
                {
                    foreach (var item in _data1)
                    {
                        if (_data2.Find(s => s == item) == null)
                        {
                            sYJsonObject1[item] = sYJsonObject[item];
                        }
                    }

                    sYJsonObject1.Save(widthpat);
                }

                sYJsonObject = sYJsonObject1;
            }




            //HOperatorSet.BinaryThreshold(ho_Image, out ho_Region, "max_separability", "dark",
            //    out hv_UsedThreshold);
            HOperatorSet.Threshold(ho_Image,out ho_Region,0,22);
            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_Region, out ho_ConnectedRegions);
            ho_SelectedRegions.Dispose();
            HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area",
                "and", hv_areamin, hv_aramax);
            ho_SelectedRegions1.Dispose();
            HOperatorSet.SelectShape(ho_SelectedRegions, out ho_SelectedRegions1, "circularity",
                "and", hv_circularitymin, hv_circularitymax);
            HOperatorSet.CountObj(ho_SelectedRegions1, out hv_Number);
            HOperatorSet.AreaCenter(ho_SelectedRegions1[1], out area, out HTuple row, out HTuple col);
            hv_rowcenter1 = new HTuple();
            hv_colcenter1 = new HTuple();
            hv_distance1 = new HTuple();
            HTuple Radius = new HTuple();
            if (hv_Number.D < num)
            {
                _win.displayText("Mark点未找完", Color.Red, 500, 500);
                State = false;
                return;
            }

                HTuple end_val9 = hv_Number;
                HTuple step_val9 = 1;
            for (hv_Index = 1; hv_Index.Continue(end_val9, step_val9); hv_Index = hv_Index.TupleAdd(step_val9))
            {
                ho_ObjectSelected.Dispose();
                HOperatorSet.SelectObj(ho_SelectedRegions1, out ho_ObjectSelected, hv_Index);
                HOperatorSet.SmallestCircle(ho_ObjectSelected, out hv_Row, out hv_Column,
                    out hv_Radius);
                ho_ContCircle.Dispose();
                HOperatorSet.GenCircleContourXld(out ho_ContCircle, hv_Row, hv_Column, hv_Radius,
                    0, 6.28318, "positive", 1);
                HOperatorSet.GetContourXld(ho_ContCircle, out hv_Row1, out hv_Col);
                ho_Regions.Dispose();
                spoke(ho_Image, out ho_Regions, 100, 30, 15, 1, 20, "negative", "max", hv_Row1,
                    hv_Col, "inner", out hv_ResultRow, out hv_ResultColumn, out hv_ArcType);
                pts_to_best_circle(out HObject ho_Circle, hv_ResultRow, hv_ResultColumn, "circle",
                    hv_Circlepixnumber, out hv_RowCenter, out hv_ColCenter, out hv_Radius1,
                    out hv_StartPhi, out hv_EndPhi, out hv_PointOrder, out hv_ArcAngle);
                if (hv_rowcenter1 == null)
                    hv_rowcenter1 = new HTuple();
                hv_rowcenter1[hv_Index - 1] = hv_RowCenter;
                if (hv_colcenter1 == null)
                    hv_colcenter1 = new HTuple();
                hv_colcenter1[hv_Index - 1] = hv_ColCenter;
                HOperatorSet.DistancePp(0, 0, hv_RowCenter, hv_ColCenter, out hv_Distance1);
                if (hv_distance1 == null)
                    hv_distance1 = new HTuple();
                hv_distance1[hv_Index - 1] = hv_Distance1;
                if (Radius == null)
                    Radius = new HTuple();
                Radius[hv_Index - 1] = hv_Radius1;
            }





            if (Convert.ToBoolean(sYJsonObject["是否开启标头定义"]))
            {
                //sYJsonObject["矩形框0"] = "1";
                //sYJsonObject["矩形框1"] = "2";

                try
                {
                    string Pathstr = Path.Combine(AppContext.BaseDirectory, "SYVisionModel", JZ, "标头" + singernum, "Up", "裁剪");


                    HOperatorSet.ReadRegion(out HObject regions, Path.Combine(Pathstr, "0.hobj"));
                    HOperatorSet.SmallestRectangle1(regions, out HTuple row1, out HTuple Col1, out HTuple Row2, out HTuple Col2);
                    int idex = 0;
                    for (int i = 0; i < hv_rowcenter1.Length; i++)
                    {
                        if (hv_rowcenter1[i] >= row1 && hv_rowcenter1[i] <= Row2 && hv_colcenter1[i] >= Col1 && hv_colcenter1[i] <= Col2)
                        {
                            idex = i;
                            break;
                        }
                    }



                    hv_Indicess = Convert.ToInt32(sYJsonObject["矩形框0"]) - 1;

                    HOperatorSet.ReadRegion(out HObject regions1, Path.Combine(Pathstr, "1.hobj"));
                    HOperatorSet.SmallestRectangle1(regions1, out row1, out Col1, out Row2, out Col2);
                    int idex1 = 0;
                    for (int i = 0; i < hv_rowcenter1.Length; i++)
                    {
                        if (hv_rowcenter1[i] >= row1 && hv_rowcenter1[i] <= Row2 && hv_colcenter1[i] >= Col1 && hv_colcenter1[i] <= Col2)
                        {
                            idex1 = i;
                            break;
                        }
                    }

                    hv_Indicess11 = Convert.ToInt32(sYJsonObject["矩形框1"]) - 1;
                }
                catch { }

            }
            else
            {

                HOperatorSet.TupleMin(hv_distance1, out hv_Min);
                HOperatorSet.TupleFind(hv_distance1, hv_Min, out hv_Indicess);
                HOperatorSet.TupleMax(hv_distance1, out hv_Max);
                HOperatorSet.TupleFind(hv_distance1, hv_Max, out hv_Indicess11);

            }


            if (!(hv_distance1.TupleSelect(hv_Indicess) > 0 && hv_distance1.TupleSelect(hv_Indicess) < 100000))
                {
                    _win.displayText("Mark点找错", Color.Red, 500, 500);
                    State = false;
                    return;
                }
                if (!(hv_distance1.TupleSelect(hv_Indicess11) > 0 && hv_distance1.TupleSelect(hv_Indicess11) < 100000))
                {
                    _win.displayText("Mark点找错", Color.Red, 500, 500);
                    State = false;
                    return;
                }
                if (!(Radius.TupleSelect(hv_Indicess) > 0 && Radius.TupleSelect(hv_Indicess) < 111111))
                {
                    _win.displayText("Mark尺寸异常", Color.Red, 500, 500);
                    State = false;
                    return;
                }
                ho_Cross11.Dispose();
                HOperatorSet.GenCrossContourXld(out ho_Cross11, hv_rowcenter1.TupleSelect(hv_Indicess),
                    hv_colcenter1.TupleSelect(hv_Indicess), 50, 0.0);
                ho_Cross22.Dispose();
                HOperatorSet.GenCrossContourXld(out ho_Cross22, hv_rowcenter1.TupleSelect(hv_Indicess11),
                    hv_colcenter1.TupleSelect(hv_Indicess11), 50, 0.0);
                HOperatorSet.AngleLl(0, 0, 50, 0, hv_rowcenter1.TupleSelect(hv_Indicess), hv_colcenter1.TupleSelect(
                    hv_Indicess), hv_rowcenter1.TupleSelect(hv_Indicess11), hv_colcenter1.TupleSelect(
                    hv_Indicess11), out hv_Angle);

            HOperatorSet.DistancePp(hv_rowcenter1.TupleSelect(hv_Indicess), hv_colcenter1.TupleSelect(
               hv_Indicess), hv_rowcenter1.TupleSelect(hv_Indicess11), hv_colcenter1.TupleSelect(
               hv_Indicess11), out HTuple Dis);
            hv_angelup = hv_Angle.TupleDeg().ToDouble();
            if (num==2)
            {
                    if ((Math.Abs(Dis.D - len) > 500.0))
                    {
                        _win.displayText("Mark点距离超限", Color.Red, 700, 500);
                        State = false;
                        return;
                    }
                    if (hv_angelup < (angleup - 15) || hv_angelup > (angleup + 15))
                    {
                        _win.displayText("角度超限", Color.Red, 900, 500);
                        State = false;
                        return;
                    }
            }
            else
            {
                _win.displayText("长度：" + Dis.D, Color.Red, 10000, 500);
            }
                  Dislen = Dis.D;
                
                _win.displayObj(ho_Cross11);
                _win.displayObj(ho_Cross22);

            _win.displayText("1", Color.Red, Convert.ToInt32(hv_rowcenter1.TupleSelect(hv_Indicess).O), Convert.ToInt32(hv_colcenter1.TupleSelect(hv_Indicess).O));

            _win.displayText("2", Color.Red, Convert.ToInt32(hv_rowcenter1.TupleSelect(hv_Indicess11).O), Convert.ToInt32(hv_colcenter1.TupleSelect(hv_Indicess11).O));



            _win.displayText("角度：" + Convert.ToDouble( hv_angelup.ToString()).ToString("0.000"), Color.Green, 700, 500);
                _win.displayText("X:" + Convert.ToDouble(hv_rowcenter1.TupleSelect(hv_Indicess).ToString()).ToString("0.000"), Color.Green, 900, 500);
                _win.displayText("Y:" + Convert.ToDouble(hv_colcenter1.TupleSelect(hv_Indicess).ToString()).ToString("0.000"), Color.Green, 900, 1500);
           
            ho_Region.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_SelectedRegions.Dispose();
            ho_SelectedRegions1.Dispose();
            ho_ObjectSelected.Dispose();
            ho_ContCircle.Dispose();
            ho_Regions.Dispose();
        }


        public static void Contraposition(SYHalconTool _win,HTuple DownWx,HTuple DownWy, HTuple DownWa, HTuple UpWx,
            HTuple UpWy,HTuple UpWa,HTuple FWx,HTuple FWy, HTuple FWa,HTuple startmarkx, HTuple startmarky,string downhomat,string uphomat,string uphomat1
            ,HTuple downshijiaoangle,HTuple upshijiaoangle,HTuple downrunangle,HTuple uprunangle,HTuple downshijiaoPx,HTuple downshijiaoPy,
            HTuple downrunPx,HTuple downrunPy,HTuple upshijiaoWx,HTuple upshijiaoWy,HTuple uprunPx,HTuple uprunPy,out HTuple endFWx,out HTuple endFWy,out HTuple endFWa) 
        {
            HOperatorSet.HomMat2dIdentity(out HTuple hv_HomMat2DIdentity1);
            HOperatorSet.HomMat2dIdentity(out hv_HomMat2DIdentity1);
            HOperatorSet.HomMat2dRotate(hv_HomMat2DIdentity1, (new HTuple(FWa - DownWa)).TupleRad()
                , DownWx, DownWy, out HTuple hv_HomMat2DRotate1);
            HOperatorSet.AffineTransPoint2d(hv_HomMat2DRotate1, startmarkx, startmarky,
                out HTuple hv_Qx5, out HTuple hv_Qy5);
            HTuple hv_markx = hv_Qx5 + (FWx - DownWx);// 示教时上视觉mark点机器人坐标X Y 
            HTuple hv_marky = hv_Qy5 + (FWy - DownWy);
            //下视觉纠偏
            HOperatorSet.HomMat2dIdentity(out HTuple hv_HomMat2DIdentity2);
            HOperatorSet.HomMat2dRotate(hv_HomMat2DIdentity2, ((((FWa - DownWa) - (downshijiaoangle - downrunangle)) + (uprunangle - upshijiaoangle))).TupleRad()
                , DownWx, DownWy, out HTuple hv_HomMat2DRotate2);
            HOperatorSet.ReadTuple(downhomat,out HTuple downtuple);
            //运行
            HOperatorSet.AffineTransPoint2d(downtuple, downrunPy, downrunPx, out HTuple hv_Qx7, out HTuple hv_Qy7);
            //示教
            HOperatorSet.AffineTransPoint2d(downtuple,downshijiaoPy, downshijiaoPx, out HTuple hv_Qx8,out HTuple hv_Qy8);
            HTuple hv_hv_xsjmarkx = startmarkx + (hv_Qx7 - hv_Qx8);//运行时下视觉mark点机器人坐标
            HTuple hv_hv_xsjmarky = startmarky + (hv_Qy7 - hv_Qy8);
            HOperatorSet.AffineTransPoint2d(hv_HomMat2DRotate2, hv_hv_xsjmarkx, hv_hv_xsjmarky,out HTuple hv_Qx9, out HTuple hv_Qy9);//运行时上视觉mark点旋转后机器人坐标
            //上视觉纠偏
            HOperatorSet.ReadTuple(uphomat, out HTuple uptuple1);
            HOperatorSet.AffineTransPoint2d(uptuple1, UpWx, UpWy, out HTuple hv_Qx2, out HTuple hv_Qy2);
            HTuple hv_hv_targetrow1 = (3000/2) - uprunPx;
            HTuple hv_hv_targetcol1 = (4000/2) - uprunPy;
            HOperatorSet.ReadTuple(uphomat1, out HTuple uptuple);
            HOperatorSet.AffineTransPoint2d(uptuple, hv_hv_targetcol1 + hv_Qx2, hv_hv_targetrow1 + hv_Qy2, out HTuple hv_Qx111, out HTuple hv_Qy111);//QX111 QY111 是mark点走到中心位置时 机器人坐标

            HTuple hv_hv_ssjcurruntmarkx = hv_markx + (hv_Qx111 - upshijiaoWx);
            HTuple hv_hv_ssjcurruntmarky = hv_marky + (hv_Qy111 - upshijiaoWy);
            endFWx= DownWx + (hv_hv_ssjcurruntmarkx - hv_Qx9);
            endFWy= DownWy + (hv_hv_ssjcurruntmarky - hv_Qy9);
            endFWa= (FWa - (downshijiaoangle - downrunangle)) + (uprunangle - upshijiaoangle);
            _win.displayText("贴合位："+"X:"+ Convert.ToDouble( endFWx.ToString()).ToString("0.000")+";"+"Y:"+ Convert.ToDouble( endFWy.ToString()).ToString("0.000")+";"+"A:"+ Convert.ToDouble( endFWa.ToString()).ToString("0.000"),Color.Lime,1000,1000);
            
        }




    }
}
