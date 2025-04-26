using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using NCalc;
using System.Windows.Forms.DataVisualization.Charting;

namespace False_Position_Method
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }


        private double Evaluate(string expression, double x)
        {
            Expression expr = new Expression(expression);
            expr.Parameters["x"] = x;
            return Convert.ToDouble(expr.Evaluate());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void calculateBtn_Click(object sender, EventArgs e)
        {
            output.Clear();
            chart2.Series.Clear();

            string rawInput = txtEquation.Text.Trim();
            string equationInput = ConvertExponents(rawInput);

            if (!double.TryParse(txtA.Text, out double a) ||
                !double.TryParse(txtB.Text, out double b) ||
                !double.TryParse(txtTolerance.Text, out double tolerance))
            {
                MessageBox.Show("Please enter valid numbers for a, b, and tolerance.");
                return;
            }

            double fa = Evaluate(equationInput, a);
            double fb = Evaluate(equationInput, b);

            if (fa * fb > 0)
            {
                MessageBox.Show("Function has same signs at a and b. Try different values.");
                return;
            }

            InitializeChart(a, b);

            Series funcSeries = CreateFunctionSeries(equationInput, a, b);
            chart2.Series.Add(funcSeries);

            Series zeroLine = CreateZeroLine(a, b);
            chart2.Series.Add(zeroLine);

            Series rootSeries = CreateRootSeries();
            chart2.Series.Add(rootSeries);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Iter\t a\t b\t c\t fc \tError");

            double c = a;
            double fc = 0;
            double prevC = a;
            double error = double.MaxValue;
            int iter = 0;

            int maxIter = 100;
            int stuckCounter = 0;
            while (iter < maxIter)
            {
                prevC = c;
                c = (a * fb - b * fa) / (fb - fa);
                fc = Evaluate(equationInput, c);

                iter++;

                if (iter > 1)
                {
                    error = Math.Abs((c - prevC) / c) * 100;
                    sb.AppendLine($"{iter,-4}{a,10:F6}{b,10:F6}{c,10:F6}{fc,10:F6}{error,10:F2}%");

                    if (error <= tolerance)
                    {
                        break;
                    }
                }
                else
                {
                    sb.AppendLine($"{iter,-4}{a,10:F6}{b,10:F6}{c,10:F6}{fc,10:F6}{"-",10}");
                }

                rootSeries.Points.Clear();
                rootSeries.Points.AddXY(c, 0);
                Application.DoEvents();

                if (fa * fb < 0)
                {
                    b = c;
                    fb = fc;
                    stuckCounter = 0;
                }
                else
                {
                    a = c;
                    fa = fc;
                    stuckCounter++;

                    if (stuckCounter >= 2)
                    {
                        fb /= 2;
                    }
                }
            }

            sb.AppendLine($"\nRoot found: x = {c:F6} after {iter} iterations");
            output.Text = sb.ToString();

            rootSeries.MarkerSize = 12;
            rootSeries.Color = Color.Green;
            rootSeries.Points[0].Color = Color.Green;
        }

        private void InitializeChart(double a, double b)
        {
            var chartArea = chart2.ChartAreas[0];
            chartArea.AxisX.Title = "x";
            chartArea.AxisY.Title = "y";
            chartArea.AxisX.Crossing = 0;
            chartArea.AxisY.Crossing = 0;
            chartArea.AxisX.IsMarginVisible = false;
            chartArea.AxisY.IsMarginVisible = false;
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisX.Minimum = a - 1;
            chartArea.AxisX.Maximum = b + 1;
        }

        private Series CreateFunctionSeries(string equation, double a, double b)
        {
            Series funcSeries = new Series("f(x)")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Red,
                BorderWidth = 2
            };

            for (double x = a - 1; x <= b + 1; x += 0.1)
            {
                double y = Evaluate(equation, x);
                funcSeries.Points.AddXY(x, y);
            }

            double yMin = funcSeries.Points.Min(p => p.YValues[0]);
            double yMax = funcSeries.Points.Max(p => p.YValues[0]);
            chart2.ChartAreas[0].AxisY.Minimum = Math.Floor(yMin) - 1;
            chart2.ChartAreas[0].AxisY.Maximum = Math.Ceiling(yMax) + 1;

            return funcSeries;
        }

        private Series CreateZeroLine(double a, double b)
        {
            Series zeroLine = new Series("y=0")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Gray,
                BorderWidth = 1,
                BorderDashStyle = ChartDashStyle.Dash
            };
            zeroLine.Points.AddXY(a - 1, 0);
            zeroLine.Points.AddXY(b + 1, 0);
            return zeroLine;
        }

        private Series CreateRootSeries()
        {
            return new Series("Root")
            {
                ChartType = SeriesChartType.Point,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 8,
                Color = Color.Blue
            };
        }

        private string ConvertExponents(string input)
        {
            input = Regex.Replace(input, @"(?<num>\d)(?=[a-zA-Z(])", "${num}*");
            input = Regex.Replace(input, @"(?<var>[a-zA-Z])(?=\()", "${var}*");
            input = Regex.Replace(input, @"(?<=\))(?=\()", ")*(");
            input = Regex.Replace(input, @"(?<=\))(?=[a-zA-Z])", ")*");

            // Fix function names for NCalc
            input = Regex.Replace(input, @"\bsin\b", "Sin", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"\bcos\b", "Cos", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"\btan\b", "Tan", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"\blog\b", "Log", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"\bsqrt\b", "Sqrt", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"\bexp\b", "Exp", RegexOptions.IgnoreCase);
            input = Regex.Replace(input, @"\babs\b", "Abs", RegexOptions.IgnoreCase);

            // Handle exponents like x^3
            string pattern = @"(?<base>(\([^()]+\)|[a-zA-Z0-9_\.]+))\s*\^\s*(?<exp>-?\d+)";
            input = Regex.Replace(input, pattern, m =>
            {
                string baseExpr = m.Groups["base"].Value;
                string exponent = m.Groups["exp"].Value;
                return $"Pow({baseExpr},{exponent})";
            });

            return input;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}
