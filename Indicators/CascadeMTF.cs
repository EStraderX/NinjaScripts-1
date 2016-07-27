#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
    public class CascadeMTF : Indicator
    {
        private List<Series<double>>                    _lines;         // �X�e�b�v���C���p�o�b�t�@
        private List<Series<int>>                       _trends;        // �g�����h�p�o�b�t�@
        private Series<int>                             _trend;         // �g�����h�p�o�b�t�@����
            
        private Series<double>                          _atr;           // ATR
        private int                                     _atrPeriod=100; // LasyATR�p     
        private double                                  _atrAlpha;      // LasyATR�p            
        private double                                  _exposedVariable;
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                 = @"Cascade MTF";
                Name                                        = "CascadeMTF";
                Calculate                                   = Calculate.OnBarClose;
                IsOverlay                                   = false;
                DisplayInDataBox                            = true;
                DrawOnPricePanel                            = true;
                DrawHorizontalGridLines                     = true;
                DrawVerticalGridLines                       = true;
                PaintPriceMarkers                           = true;
                ScaleJustification                          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                // �p�����[�^�̏����l���Z�b�g
                StepSize=0.8;
                TF=60;
                //See Help Guide for additional information. 
                IsSuspendedWhileInactive                    = true;
                // �C���W�P�[�^�[�̏o�͐ݒ�
                AddPlot(new Stroke(Brushes.Blue),  PlotStyle.Bar,"CascadeMTF LINE"); // ���C��
            }
            else if (State == State.Configure)
            {
                //�w�肳�ꂽ�^�C���t���[���̃o�[�I�u�W�F�N�g��ǉ����܂��B
                AddDataSeries(BarsPeriodType.Minute, TF);
                
                _atrAlpha=2.0/(_atrPeriod+1.0);
                Plots[0].Width=5;           

            } 
            else if (State == State.Historical)     //�q�X�g���J���f�[�^�̏������A���A���^�C���ł͂P�x�����o�b�N�e�X�g�̍œK�����͂P�œK�����ɌĂ΂�܂��B      
            {   
                //�f�t�H���g�^�C���t���[���p�̃o�b�t�@�A�I�[�i�[��this�ɂ��܂��B 
                _trend          = new Series<int>(this);

                //MTF�p�̃o�b�t�@�A�I�[�i�[�� BarsArray[1] ���w�肵�܂��B
                _atr            = new Series<double>(BarsArray[1]);

                // Series<T>�^�C�v�̃��X�g
                _lines          = new List<Series<double>>();
                _trends         = new List<Series<int>>();
                
                // Series<T>�� �D���Ȃ����ǉ��o����B
                for(int i=0;i<6;i++)    _lines.Add(new Series<double>(BarsArray[1]));
                for(int i=0;i<6;i++)    _trends.Add(new Series<int>(BarsArray[1]));
    

            }
        }

        protected override void OnBarUpdate()
        {
            
            //Add your custom indicator logic here.
            if (BarsInProgress == 1) //BarsArray[1]�̍X�V��
            {
              
                // �ŏ��̃o�[           
                if(CurrentBars[1]==0){
                    _atr[0]=Highs[1][0]-Lows[1][0]; 
                    return;
                }

                // LasyATR���v�Z
                double tr = Math.Max(Highs[1][0],Closes[1][1])-Math.Min(Lows[1][0],Closes[1][1]);
                tr=Math.Max(_atr[1]*0.667,Math.Min(tr,_atr[1]*1.333));
                _atr[0]=_atrAlpha*tr+(1.0-_atrAlpha)*_atr[1];           
                // StepLine���v�Z
                calcStepLines();
                //---
                int bull=0;
                int bear=0;
                double sum=0.0;
                // Series<T>�����[�v���ň�����
                for(int i=0;i<6;i++)
                {
                    //Series<T>��2�����z����ۂ��g�����Ƃ��o���܂��B
                    sum+=_lines[i][0];
                    if(_trends[i][0]== 1)bull++;
                    if(_trends[i][0]== -1)bear++;
                }
                if(CurrentBars[0]==0)return;
                // �����Ńf�t�H���gTF�p�̃o�b�t�@�ɂ���������
                Line[0]=1.0;
                _trend[0]=(bull==6) ? 5 :
                          (bull==5) ? 4 :
                          (bear==6) ? 1 :
                          (bear==5) ? 2 :
                          3;
                
            }   
            if (BarsInProgress == 0)// BarsArray[0](�f�t�H���gTF)�̍X�V��
            {
                if(CurrentBars[0]>0 )  
                {
                    // ��ʑ��̌��ʂ��R�s�[����B
                    Line[0] = Line[1];
                    _trend[0] = _trend[1];
                    
                    // �g�����h�ɂ���Đ��̐F��ύX
                    if(_trend[0]==1)        PlotBrushes[0][0] = Brushes.Maroon;
                    else if(_trend[0]==2)   PlotBrushes[0][0] = Brushes.Orange;
                    else if(_trend[0]==3)   PlotBrushes[0][0] = Brushes.Yellow;
                    else if(_trend[0]==4)   PlotBrushes[0][0] = Brushes.LimeGreen;
                    else if(_trend[0]==5)   PlotBrushes[0][0] = Brushes.DarkGreen;
                }   
                // ���܂��Ȃ�
                _exposedVariable = Close[0];
            }
        }   
        #region indicators
        //--- calc step lines
        private void calcStepLines()
        {
         
            double price=Typical[0];
            double rate=1.0;
            for(int i=0;i<6;i++)
            {
                double sz =_atr[0]*StepSize*rate;
                //--- 
                if((price-sz)>_lines[i][1]) _lines[i][0]=price-sz;
                else if((price+sz)<_lines[i][1]) _lines[i][0]=price+sz;
                else _lines[i][0]=_lines[i][1];
                //---
                if(_lines[i][0]>_lines[i][1]) _trends[i][0]=1;
                else if(_lines[i][0]<_lines[i][1])_trends[i][0]= -1;
                else _trends[i][0]=_trends[i][1];
                //---
                rate+=0.25;
            }  

        }

        #endregion

        
        #region Properties
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Line
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<int> Trend
        {
            get { return _trend; }
        }

        // �^�C���t���[��
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "TimeFrame",
                                GroupName = "NinjaScriptParameters", Order = 0)]
        public int TF
        { get; set; }

        public double ExposedVariable
        {
            // We need to call the Update() method to ensure our exposed variable is in up-to-date.
            get { Update(); return _exposedVariable; }
        }
        
        // �p�����[�^ �X�e�b�v�T�C�Y 
        [Range(0.01, 9.99), NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Step Size",
                                GroupName = "NinjaScriptParameters", Order = 1)]
        public double StepSize
        { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private CascadeMTF[] cacheCascadeMTF;
        public CascadeMTF CascadeMTF(int tF, double stepSize)
        {
            return CascadeMTF(Input, tF, stepSize);
        }

        public CascadeMTF CascadeMTF(ISeries<double> input, int tF, double stepSize)
        {
            if (cacheCascadeMTF != null)
                for (int idx = 0; idx < cacheCascadeMTF.Length; idx++)
                    if (cacheCascadeMTF[idx] != null && cacheCascadeMTF[idx].TF == tF && cacheCascadeMTF[idx].StepSize == stepSize && cacheCascadeMTF[idx].EqualsInput(input))
                        return cacheCascadeMTF[idx];
            return CacheIndicator<CascadeMTF>(new CascadeMTF(){ TF = tF, StepSize = stepSize }, input, ref cacheCascadeMTF);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.CascadeMTF CascadeMTF(int tF, double stepSize)
        {
            return indicator.CascadeMTF(Input, tF, stepSize);
        }

        public Indicators.CascadeMTF CascadeMTF(ISeries<double> input , int tF, double stepSize)
        {
            return indicator.CascadeMTF(input, tF, stepSize);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.CascadeMTF CascadeMTF(int tF, double stepSize)
        {
            return indicator.CascadeMTF(Input, tF, stepSize);
        }

        public Indicators.CascadeMTF CascadeMTF(ISeries<double> input , int tF, double stepSize)
        {
            return indicator.CascadeMTF(input, tF, stepSize);
        }
    }
}

#endregion
