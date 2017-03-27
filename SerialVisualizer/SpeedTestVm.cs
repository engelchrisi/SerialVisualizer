using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts.Geared;
using LiveCharts.Wpf;
using static System.Int64;
using CartesianChart = LiveCharts.WinForms.CartesianChart;

// ReSharper disable InconsistentNaming

namespace SerialVisualizer
{
    public class SpeedTestVm
    {
        public SpeedTestVm()
        {
            SensorValues = new GearedValues<long>().WithQuality(Quality.High);
            MaxSensorValues = new GearedValues<long>().WithQuality(Quality.High);
            NoiseOffsetValues = new GearedValues<long>().WithQuality(Quality.High);
            rawSensorValues = new GearedValues<long>().WithQuality(Quality.High);
            songAvgValues = new GearedValues<long>().WithQuality(Quality.High);
            ModeValues = new GearedValues<long>().WithQuality(Quality.Low);
        }


        public struct DataItem
        {
            public long Time;
            public long Value;
        };

        public bool IsReading { get; set; }
        public GearedValues<long> SensorValues { get; set; }
        public GearedValues<long> MaxSensorValues { get; set; }
        public GearedValues<long> NoiseOffsetValues { get; set; }
        public GearedValues<long> rawSensorValues { get; set; }
        public GearedValues<long> songAvgValues { get; set; }
        public GearedValues<long> ModeValues { get; set; }


        public double Count { get; set; }

        public bool Start(string COMPort)
        {
            try
            {
                InitializeSerial(COMPort);
                _mySerialPort.Open();
                IsReading = true;
            }
            catch (IOException)
            {
            }

            return IsReading;
        }

        public void Stop()
        {
            IsReading = false;
            try
            {
                _mySerialPort?.Close();

            }
            catch (IOException)
            {
                //throw;
            }
        }

        public void Pause()
        {
            IsReading ^= true;
        }

        public void Clear()
        {
            SensorValues.Clear();
            MaxSensorValues.Clear();
            NoiseOffsetValues.Clear();
            rawSensorValues.Clear();
            songAvgValues.Clear();
            ModeValues.Clear();

        }

        private SerialPort _mySerialPort;

        private void InitializeSerial(string comPort)
        {
            _mySerialPort = new SerialPort(comPort)
            {
                BaudRate = 38400,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            _mySerialPort.DataReceived += DataReceivedHandler;

        }

        private const char SEP_LINES = '\n';
        private const char SEP_TIME_VALUE = ':';
        private const string COMMENT_PREFIX = "#"; // lines with this will be ignored


        private long _lastTime;

        private const int _capacity = 10;

        private readonly List<long> modeValueBuf = new List<long>(_capacity);
        private readonly List<long> sensorValueBuf = new List<long>(_capacity);
        private readonly List<long> maxSensorValueBuf = new List<long>(_capacity);
        private readonly List<long> noiceOffsetValueBuf = new List<long>(_capacity);
        private readonly List<long> rawSensorValueBuf = new List<long>(_capacity);
        private readonly List<long> songAvgValueBuf = new List<long>(_capacity);
        private string _lastFragment = "";
        private string _animationTitle;
        public CartesianChart CartesianChart { get; set; }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            var sp = (SerialPort)sender;
            string indata = _lastFragment + sp.ReadExisting();
            sp.DiscardInBuffer();

            if (!IsReading)
                return;

            indata = indata.Replace("\r\n", "\n");

            var lines = indata.Split(SEP_LINES);
            var noOfLines = lines.Length;
            if (!indata.EndsWith(new string(SEP_LINES, 1)))
            {
                // last fragment is not complete
                --noOfLines;
                _lastFragment = lines[noOfLines];
            }
            else
            {
                _lastFragment = "";
            }

            const string suffixTime = "| ";

            for (int index = 0; index < noOfLines; index++)
            {
                var line = lines[index];
                var pos = line.IndexOf(suffixTime, StringComparison.Ordinal);
                if (pos >= 0)
                    line = line.Substring(pos + suffixTime.Length);

                if (line.StartsWith(COMMENT_PREFIX))
                    continue;

                const string animationPrefix = @"=== ";
                if (line.StartsWith(animationPrefix))
                {
                    _animationTitle = line.Substring(animationPrefix.Length);
                    _animationTitle = _animationTitle.Replace(@" ===", "");
                }

                var fragments = line.Split(SEP_TIME_VALUE);
                try
                {
                    switch (fragments.Length)
                    {
                        case 1:
                            fragments = fragments[0].Split(';');

                            if (fragments.Length != 6)
                                continue;

                            AddValue(fragments[0], modeValueBuf, ModeValues);
                            AddValue(fragments[1], noiceOffsetValueBuf, NoiseOffsetValues);
                            AddValue(fragments[2], sensorValueBuf, SensorValues);
                            AddValue(fragments[3], rawSensorValueBuf, rawSensorValues, _animationTitle);
                            _animationTitle = null;
                            AddValue(fragments[4], maxSensorValueBuf, MaxSensorValues);
                            AddValue(fragments[5], songAvgValueBuf, songAvgValues);
                            break;
                        case 2:
                            DataItem di;
                            di.Time = Parse(fragments[0]);
                            di.Value = Parse(fragments[1]);

                            if (di.Time < _lastTime)
                                continue;
                            _lastTime = di.Time;

                            SensorValues.Add(di.Value);
                            break;
                    }
                }
                catch (FormatException)
                {
                }
            }

            ModeValues.AddRange(modeValueBuf); modeValueBuf.Clear();
            SensorValues.AddRange(sensorValueBuf); sensorValueBuf.Clear();
            MaxSensorValues.AddRange(maxSensorValueBuf); maxSensorValueBuf.Clear();
            NoiseOffsetValues.AddRange(noiceOffsetValueBuf); noiceOffsetValueBuf.Clear();
            rawSensorValues.AddRange(rawSensorValueBuf); rawSensorValueBuf.Clear();
            songAvgValues.AddRange(songAvgValueBuf); songAvgValueBuf.Clear();

        }

        private void AddAnimationTitle(int xValue, string animationTitle)
        {
            if (CartesianChart.InvokeRequired)
            {
                CartesianChart.Invoke(new Action<int, string>(AddAnimationTitle), xValue, animationTitle);
                return;
            }
            var axis = CartesianChart.AxisX[0];

            if (axis.Sections.Count > 0)
            {
                var previousSection = axis.Sections.Last();
                previousSection.SectionWidth = xValue - previousSection.Value;

                var previousElem= CartesianChart.VisualElements.Last();
                previousElem.X = previousSection.Value + previousSection.SectionWidth/2;
            }
            var color = ((axis.Sections.Count & 1) != 0)? Colors.LightSkyBlue : Colors.Aqua;
            axis.Sections.Add(
                    new AxisSection
                    {
                        Label = animationTitle,
                        Value = xValue,
                        SectionWidth = 200,
                        Fill = new SolidColorBrush
                        {
                            Color = color,
                            Opacity = 0.4
                        }
                    });

            CartesianChart.VisualElements.Add(new VisualElement
            {
                X = xValue + 100,
                Y = 150,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                UIElement = new TextBlock //notice this property must be a wpf control
                {
                    Text = animationTitle,
                    //FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(color),
                    FontSize = 10,
                    //Opacity = 0.6
                }
            });
        }

        private void AddValue(string longFrag, ICollection<long> valueBuffer, GearedValues<long> sensorValues, string animationTitle= null)
        {
            var value = Parse(longFrag);

            if (valueBuffer.Count >= _capacity)
            {
                sensorValues.AddRange(valueBuffer);
                valueBuffer.Clear();
            }

            valueBuffer.Add(value);
            if (animationTitle != null)
            {
                sensorValues.AddRange(valueBuffer);
                valueBuffer.Clear();

                AddAnimationTitle(sensorValues.Count, animationTitle);
            }
        }
    }
}
