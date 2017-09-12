using System;
using System.Windows.Forms;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Geared;
using SerialVisualizer.Properties;

// ReSharper disable All

namespace SerialVisualizer
{
    public partial class Form1 : Form
    {
        private SpeedTestVm _viewModel = new SpeedTestVm();

        public Form1()
        {
            InitializeComponent();

            for (int i = 1; i < 25; ++i)
            {
                comboCOM.Items.Add("COM" + i);
            }
            comboCOM.SelectedItem = Settings.Default.COMPort;

            var strokeDashArray= DoubleCollection.Parse("4,2");
            var strokeDashArray2 = DoubleCollection.Parse("2,2");


#if false
            //AddSeries(_viewModel.ModeValues, "Mode Normal vs. High", Colors.Blue, 2, Brushes.Transparent, null);
            AddSeries(_viewModel.SensorValues, "Vol. Avg", Color.FromArgb(0xff, 0xff, 0xA5, 0), 0.5, Brushes.Transparent, null);
            AddSeries(_viewModel.MaxSensorValues, "Vol. Max", Color.FromArgb(0xFF, 0xFF, 0x00, 0x00), 1.5, Brushes.Transparent, null);
            AddSeries(_viewModel.NoiseOffsetValues, "RMS Flt.", Colors.LightGreen, 1.5, Brushes.Transparent, strokeDashArray);
            AddSeries(_viewModel.rawSensorValues, "RMS", Colors.Green, 1.5, Brushes.Transparent, null);
            AddSeries(_viewModel.songAvgValues, "dB", Colors.Yellow, 1.5, Brushes.Transparent, null);
#else
            AddSeries(_viewModel.MaxSensorValues, "Max Sensor", Color.FromArgb(0xFF, 0xFF, 0x0, 0x00), 1.5, Brushes.Transparent, strokeDashArray);
            AddSeries(_viewModel.rawSensorValues, "Sensor raw", Color.FromArgb(0xff, 0xff, 0, 0), 1.0, Brushes.Transparent, strokeDashArray2);
            AddSeries(_viewModel.SensorValues, "Sensor", Color.FromArgb(0xff, 0xff, 0, 0), 1.0, new SolidColorBrush(Color.FromArgb(0x3f, 0xff, 0, 0)), null);
            AddSeries(_viewModel.NoiseOffsetValues, "Noice", Color.FromArgb(0xFF, 0xFF, 0x0, 0x00), 1.5, Brushes.Transparent, strokeDashArray);
            AddSeries(_viewModel.songAvgValues, "Song Avg Sensor", Colors.Yellow, 1.5, Brushes.Transparent, strokeDashArray);
            AddSeries(_viewModel.ModeValues, "Mode Normal vs. High", Colors.Blue, 2, Brushes.Transparent, null);

#endif

            cartesianChart1.DisableAnimations = true;
            cartesianChart1.Zoom= ZoomingOptions.Xy;
            //cartesianChart1.Pan= PanningOptions.X;

    //        var mapper = Mappers.Xy<Data>() //in this case value is of type <ObservablePoint>
    //.X(value => value.X) //use the X property as X
    //.Y(value => value.Y); //use the Y property as Y

#if false
            //the ChartValues property will store our values array
            ChartValues = new ChartValues<MeasureModel>();
            cartesianChart1.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Values = ChartValues,
                    PointGeometrySize = 18,
                    StrokeThickness = 4
                }
            };
            cartesianChart1.AxisX.Add(new Axis
            {
                DisableAnimations = true,
                LabelFormatter = value => value.ToString(), // new DateTime((long)value).ToString("mm:ss"),
                Separator = new LiveCharts.Wpf.Separator
                {
                    Step = 500 // ms
                }
            });

            cartesianChart1.AxisY.Add(new Axis
            {
                DisableAnimations = true,
                LabelFormatter = value => value.ToString(), // new DateTime((long)value).ToString("mm:ss"),
                Separator = new LiveCharts.Wpf.Separator
                {
                    Step = 50 // ms
                }
            });

            cartesianChart1.AxisY[0].MaxValue = 1050;
            cartesianChart1.AxisY[0].MinValue = 0;

#endif
            // ------------------------

        }

        private void AddSeries(GearedValues<long> values, string title, Color color, double strokeThickness, SolidColorBrush fillBrush, DoubleCollection strokeDashArray=null)
        {
            cartesianChart1.Series.Add(new GLineSeries
            {
                Values = values,
                Title = title,
                Stroke = new SolidColorBrush(color),
                Fill = fillBrush,
                StrokeThickness = strokeThickness,
                StrokeDashArray = strokeDashArray,
                PointGeometry = null //use a null geometry when you have many series
            });
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            buttonStop_Click(sender, e);

            var settings = Settings.Default;
            settings.Save();
        }


        private void buttonStart_Click(object sender, EventArgs e)
        {

            cartesianChart1.AxisX[0].Sections.Clear();
            cartesianChart1.VisualElements.Clear();

            _viewModel.Clear();
            _viewModel.CartesianChart= cartesianChart1;
            if (_viewModel.Start((string) comboCOM.SelectedItem))
            {
                buttonStart.Enabled = false;
                buttonStop.Enabled = true;
                buttonPause.Enabled = true;
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            buttonStart.Enabled = true;
            buttonPause.Enabled = false;
            buttonStop.Enabled = false;

            _viewModel.Stop();

        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            buttonStart.Enabled = false;
            buttonPause.Enabled = true;
            buttonStop.Enabled = true;

            _viewModel.Pause();

        }

        private void comboCOM_SelectionChangeCommitted(object sender, EventArgs e)
        {
            Settings.Default.COMPort= (string) comboCOM.SelectedItem;

        }
    }
}
