using System;
using System.Collections.Generic;
using System.IO.Ports;
using LiveCharts.Geared;
using static System.Int64;

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

            InitializeSerial();
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

        public void Start()
        {
            _mySerialPort.Open();
            IsReading = true;

        }

        public void Stop()
        {
            IsReading = false;
            _mySerialPort.Close();
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

        private void InitializeSerial()
        {
            _mySerialPort = new SerialPort("COM14") // "COM8")
            {
                BaudRate = 38400, //,115200
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
        private string _lastFragment= "";

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

            for (int index = 0; index < noOfLines; index++)
            {
                var line = lines[index];

                if (line.StartsWith(COMMENT_PREFIX))
                    continue;

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
                            AddValue(fragments[3], rawSensorValueBuf, rawSensorValues);
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

        private static void AddValue(string longFrag, List<long> sensorValueBuf, GearedValues<long> sensorValues)
        {
            long value = Parse(longFrag);

            if (sensorValueBuf.Count < _capacity)
            {
                sensorValueBuf.Add(value);
            }
            else
            {
                sensorValues.AddRange(sensorValueBuf);
                sensorValueBuf.Clear();
            }
        }
    }
}
