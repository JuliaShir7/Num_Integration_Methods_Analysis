using Mathos.Parser;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;
using System.Reflection;

namespace KR_ChM
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Method method;

        Dictionary<int, double> Results;
        Dictionary<int, double> Speeds;
        Dictionary<int, double> Errors;
        List<Dictionary<int, double>> AllResults = new List<Dictionary<int, double>>();
        List<Dictionary<int, double>> AllSpeeds = new List<Dictionary<int, double>>();
        List<Dictionary<int, double>> AllErrorLimits = new List<Dictionary<int, double>>();
        SeriesCollection series;

        ColumnSeries ColumnSeries;
        DataSet dataSet;
        DataTable ResultTable = new DataTable();
        DataTable SpeedTable = new DataTable();
        DataTable ErrorTable = new DataTable();

        public MainWindow()
        {

            InitializeComponent();

            CreateDataTable(ResultTable, DG_Results);
            CreateDataTable(SpeedTable, DG_Speeds);
            CreateDataTable(ErrorTable, DG_ErrorLimits);
            ResultTable.TableName = "RT";
            SpeedTable.TableName = "ST";
            ErrorTable.TableName = "ET";

            //tb_SegmentNum.Text = "1000";
            //tb_PointsNum.Text = "1000";
            //tb_A.Text = "3";
            //tb_B.Text = "1";
            //tb_Equation.Text = "x^2";
            //tb_IdealResult.Text = "x^3/3";
            //tb_Step.Text = "0,01";
            //tb_Pow.Text = "3";

        }

        private void Button_DoAnalysis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Speeds = new Dictionary<int, double>();
                Results = new Dictionary<int, double>();
                Speeds.Add(-1, FindSpeedForIdeal());
                Results.Add(-1, FindIdealResult());
                if (FindCheckedValues().Count == 0)
                    throw new NotSelectedMethodException("Вы не выбрали методы для анализа!");
                foreach (int m in FindCheckedValues())
                {
                    foreach (Method v in Enum.GetValues(typeof(Method)))
                    {
                        if (m == (int)v)
                            method = v;
                    }
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    ChooseMethod();
                    stopwatch.Stop();
                    var time = stopwatch.ElapsedTicks;
                    Speeds.Add((int)method, time);

                }
                AllErrorLimits.Add(FindErrors());
                AllResults.Add(Results);
                AllSpeeds.Add(Speeds);

                AddResults(ResultTable, Results);
                AddResults(SpeedTable, Speeds);
                AddResults(ErrorTable, Errors);

                DrawGraphic(Results, Results_Chart);
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Проверьте введенные данные!");
            }
            catch (FormatException)
            {
                MessageBox.Show("Проверьте введенные данные!");
            }
            catch (NullEquationTextException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (NullIdealEquationTextException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (NotSelectedMethodException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (NotAvailableChebyshevStepException ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AllResults.Count == 0 && AllSpeeds.Count == 0 && AllErrorLimits.Count == 0)
                    throw new NoAnalisysResultsYetException();
                string tabItem = ((sender as TabControl).SelectedItem as TabItem).Name.ToString();
                switch (tabItem)
                {
                    case "TabItem_Results":
                        {
                            if (series is null) break;
                            DrawGraphic(Results, Results_Chart);
                            Results_Chart_Graph.IsSelected = true;
                            break;
                        }
                    case "TabItem_ErrorLimits":
                        {
                            DivideErrors(Errors, out Dictionary<int, double> absolute, out Dictionary<int, double> relative);
                            DrawGraphic(absolute, ErrorsAbsolute_Chart);
                            DrawGraphic(relative, ErrorsRelative_Chart);
                            Errors_Chart_Graph.IsSelected = true;
                            break;
                        }
                    case "TabItem_Speeds":
                        {
                            DrawGraphic(Speeds, Speeds_Chart);
                            Speedss_Chart_Graph.IsSelected = true;
                            break;
                        }
                }
            }
            catch (NoAnalisysResultsYetException) { }
        }

        private void DG_Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string si = ReturnSelectedItem(sender as DataGrid), sr = null;

                foreach (var r in AllResults)
                {
                    foreach (var res in r)
                    {
                        sr += res.Value;
                    }
                    if (si == sr)
                    {
                        Results = r;
                        DrawGraphic(Results, Results_Chart);
                        break;
                    }
                }
            }
            catch (NotSelectedRowException) { }
            catch (Exception) { }
        }

        private void DG_ErrorLimits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string si = ReturnSelectedItem(sender as DataGrid), sr = null;

                foreach (var r in AllErrorLimits)
                {
                    foreach (var res in r)
                    {
                        sr += res.Value;
                    }
                    if (si == sr)
                    {
                        Errors = r;
                        DivideErrors(Errors, out Dictionary<int, double> abs, out Dictionary<int, double> rel);
                        DrawGraphic(abs, ErrorsAbsolute_Chart);
                        DrawGraphic(rel, ErrorsRelative_Chart);
                        break;
                    }
                }
            }
            catch (NotSelectedRowException) { }
            catch (Exception) { }
        }

        private void DG_Speeds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string si = ReturnSelectedItem(sender as DataGrid), sr = null;

                foreach (var r in AllSpeeds)
                {
                    foreach (var res in r)
                    {
                        sr += res.Value;
                    }
                    if (si == sr)
                    {
                        Speeds = r;
                        DrawGraphic(Speeds, Speeds_Chart);
                        break;
                    }
                }
            }
            catch (NotSelectedRowException) { }
            catch (Exception) { }
        }
        string ReturnSelectedItem(DataGrid dataGrid)
        {
            if (ResultTable.Rows.Count == 0 || SpeedTable.Rows.Count == 0 || ErrorTable.Rows.Count == 0)
                throw new NotSelectedRowException();
            string si = null;
            foreach (DataRowView item in dataGrid.SelectedItems)
            {
                for (int i = 1; i < item.Row.ItemArray.Length; i++)
                {
                    si += item.Row.ItemArray[i];
                }
            }
            return si;
        }
        void DivideErrors(Dictionary<int, double> dictionary, out Dictionary<int, double> absolute, out Dictionary<int, double> relative)
        {
            absolute = new Dictionary<int, double>();
            relative = new Dictionary<int, double>();
            foreach (var v in dictionary)
            {
                if (v.Key >= 20)
                {
                    relative.Add(v.Key - 20, v.Value);
                }
                else
                {
                    if (v.Key == -1 || v.Key == 19) continue;

                    absolute.Add(v.Key, v.Value);
                }
            }
        }
        List<string> ReturnNamesFromCheckBoxes()
        {
            string[] s = new string[CheckBoxes.Children.Count];
            List<string> names = new List<string>();
            for (int i = 0; i < CheckBoxes.Children.Count; i++)
            {
                s[i] = (CheckBoxes.Children[i] as CheckBox).Content.ToString();
            }
            string[] n;
            for (int j = 0; j < s.Length; j++)
            {
                n = s[j].Split('(', ')');
                for (int i = n.Length - 2; i > 0; i--)
                {
                    names.Add(n[i]);
                    break;
                }
            }
            return names;
        }

        void CreateDataTable(DataTable table, DataGrid grid)
        {
            DataColumn column;
            List<string> list = ReturnNamesFromCheckBoxes();
            column = new DataColumn();
            column.ColumnName = "Параметры";
            table.Columns.Add(column);
            column = new DataColumn();
            column.ColumnName = "Ожидаемый результат";
            table.Columns.Add(column);
            foreach (Method m in Enum.GetValues(typeof(Method)))
            {
                column = new DataColumn();
                for (int i = 0; i < list.Count; i++)
                {
                    if (i == (int)m)
                        column.ColumnName = list[i];
                }
                table.Columns.Add(column);
            }
            dataSet = new DataSet();
            dataSet.Tables.Add(table);
            grid.DataContext = table.DefaultView;
        }
        void AddResults(DataTable table, Dictionary<int, double> dictionary)
        {
            DataRow row;
            List<string> list = ReturnNamesFromCheckBoxes();
            row = table.NewRow();
            row["Параметры"] = $"Степень: {tb_Pow.Text}\nШаг: {tb_Step.Text}\nЧисло сегментов: {tb_SegmentNum.Text}\nЧисло точек: {tb_PointsNum.Text}";
            row["Ожидаемый результат"] = dictionary[-1];
            if (table.TableName == "ET")
            {
                foreach (var x in dictionary)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i == x.Key)
                            row[list[i]] = $"Aбсолютная: {x.Value}\nОтносительная: {dictionary[x.Key + 20]}";
                    }
                }
            }
            else
            {
                if (table.TableName == "ST")
                {
                    row["Ожидаемый результат"] = $"{((1000L * 1000L * 1000L) * dictionary[-1]) / (Stopwatch.Frequency * 1000000000)} секунд";
                    foreach (var x in dictionary)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (i == x.Key)
                                row[list[i]] = $"{((1000L * 1000L * 1000L) * x.Value) / (Stopwatch.Frequency * 1000000000)} секунд";
                        }
                    }
                }
                else
                {
                    foreach (var x in dictionary)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (i == x.Key)
                                row[list[i]] = x.Value;
                        }
                    }
                }

            }
            table.Rows.Add(row);
        }
        Dictionary<int, double> FindErrors()
        {
            Errors = new Dictionary<int, double>();
            foreach (var res in Results)
            {
                Errors.Add(res.Key, ErrorLimits.Absolute(FindIdealResult(), res.Value));
                Errors.Add(res.Key + 20, ErrorLimits.Relative(FindIdealResult(), res.Value));
            }
            return Errors;
        }
        //void Draw(Dictionary<int, double> dictionary, CartesianChart Chart)
        //{

        //    Chart.LegendLocation = LegendLocation.Top;
        //    Chart.ChartLegend.FontSize = 10;
        //    Chart.ChartLegend.MaxHeight = 100;
        //    series = new SeriesCollection();
        //    Chart.Series = series;

        //    List<string> labels = ReturnNamesFromCheckBoxes();

        //    line = new LineSeries();
        //    line.LineSmoothness = 0;
        //    line.PointGeometrySize = 10;
        //    line.PointGeometry = DefaultGeometries.Circle;
        //    line.StrokeThickness = 3;
        //    Chart.ChartLegend.Visibility = Visibility.Collapsed;
        //    //line.Values = new ChartValues<ObservablePoint>();
        //    line.Values = new ChartValues<double>();
        //    //line.DataLabels = true;
        //    var tooltip = (DefaultTooltip)Chart.DataTooltip;
        //    tooltip.SelectionMode = TooltipSelectionMode.OnlySender;
        //    line.Title = "";
        //    foreach (var r in dictionary)
        //    {

        //        for (int i = 0; i < labels.Count; i++)
        //        {
        //            if (r.Key == i)
        //            {
        //                line.DataLabels = false;
        //                //line.LabelPoint = point => $"{r.Value.ToString()}"; ;

        //                //line.Title = labels[i];
        //                break;
        //                //line.LabelPoint = r.Value , MethodValueConverter.GetDescription(m);
        //            }
        //        }
        //        //line.Values.Add(new ObservablePoint(r.Key + 2, r.Value));
        //        line.Values.Add(r.Value);
        //    }
        //    series.Add(line);
        //    //Chart.AxisX.Clear();
        //}
        void DrawGraphic(Dictionary<int, double> dictionary, CartesianChart Chart)
        {
            Chart.Series.Clear();
            Chart.ChartLegend.FontSize = 10;
            Chart.ChartLegend.MaxHeight = 100;
            series = new SeriesCollection();
            Chart.LegendLocation = LegendLocation.Top;
            List<string> labels = ReturnNamesFromCheckBoxes();
            foreach (var r in dictionary)
            {
                ColumnSeries = new ColumnSeries();
                ColumnSeries.Values = new ChartValues<double>();
                ColumnSeries.PointGeometry = DefaultGeometries.Circle;
                ColumnSeries.StrokeThickness = 2;
                Chart.Series = series;
                ColumnSeries.DataLabels = false;
                ColumnSeries.Title = "Ожидаемый результат";
                for (int i = 0; i < labels.Count; i++)
                {
                    if (r.Key == i)
                    {
                        ColumnSeries.DataLabels = false;
                        ColumnSeries.LabelPoint = point => $"{labels[i]}: {r.Value.ToString()}";
                        break;
                    }
                }
                foreach (Method m in Enum.GetValues(typeof(Method)))
                {
                    if (r.Key == (int)m)
                    {
                        ColumnSeries.Title = MethodValueConverter.GetDescription(m);
                    }
                }

                var tooltip = (DefaultTooltip)Chart.DataTooltip;
                ColumnSeries.Values.Add(r.Value);
                series.Add(ColumnSeries);

            }
        }
        double FindSpeedForIdeal()
        {
            var timer = Stopwatch.StartNew();
            MathParser mathParser = new MathParser();
            double integral = 0;
            mathParser.LocalVariables.Add("x", Convert.ToDouble(tb_B.Text));
            double yb = mathParser.Parse(tb_IdealResult.Text);
            mathParser.LocalVariables.Clear();
            mathParser.LocalVariables.Add("x", Convert.ToDouble(tb_A.Text));
            double ya = mathParser.Parse(tb_IdealResult.Text);
            mathParser.LocalVariables.Clear();
            integral = yb - ya;
            timer.Stop();
            var time = timer.ElapsedTicks;

            return time;
        }
        double FindIdealResult()
        {
            if (tb_IdealResult.Text == "")
                throw new NullIdealEquationTextException("Вы не ввели аналитически вычисленную функцию!");
            MathParser mathParser = new MathParser();
            double integral = 0;
            mathParser.LocalVariables.Add("x", Convert.ToDouble(tb_B.Text));
            double yb = mathParser.Parse(tb_IdealResult.Text);
            mathParser.LocalVariables.Clear();
            mathParser.LocalVariables.Add("x", Convert.ToDouble(tb_A.Text));
            double ya = mathParser.Parse(tb_IdealResult.Text);
            mathParser.LocalVariables.Clear();
            integral = yb - ya;

            return integral;
        }
        List<int> FindCheckedValues()
        {
            List<int> checkedMethodsIndexes = new List<int>();
            for (int i = 0; i < CheckBoxes.Children.Count; i++)
            {
                if (CheckBoxes.Children[i] is CheckBox)
                {
                    if ((CheckBoxes.Children[i] as CheckBox).IsChecked == true)
                    {
                        if (checkedMethodsIndexes.Count == 0)
                            checkedMethodsIndexes.Add(i);
                        else
                        {
                            if (CheckForRepeatValue(i, checkedMethodsIndexes) == false)
                                checkedMethodsIndexes.Add(i);
                        }
                    }
                }
            }

            return checkedMethodsIndexes;
        }
        bool CheckForRepeatValue(int i, List<int> list)
        {
            bool check = false;
            foreach (int c in list)
            {
                if (c == i)
                    check = true;
            }
            return check;
        }
        public void ChooseMethod()
        {
            int[] availableStepForChebyshev = new int[] { 2, 3, 4, 5, 6, 7, 9 };
            if (tb_Equation.Text == "")
                throw new NullEquationTextException("Вы не ввели подынтегральную функцию!");
            switch ((int)method)
            {
                case 0:
                    {
                        double r = Integral.RectangleStraightMethod(Convert.ToDouble(tb_Step.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                        Results.Add((int)method, r);
                        break;
                    }
                case 1:
                    {
                        double r = Integral.RectangleReverseMethod(Convert.ToDouble(tb_Step.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                        Results.Add((int)method, r);
                        break;
                    }
                case 2:
                    {
                        double r = Integral.RectangleMiddleMethod(Convert.ToDouble(tb_Step.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                        Results.Add((int)method, r);
                        break;
                    }
                case 3:
                    {
                        double r = Integral.TrapezoidMethod(Convert.ToDouble(tb_Step.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                        Results.Add((int)method, r);
                        break;
                    }
                case 4:
                    {
                        double r = Integral.SimpsonMethod(Convert.ToDouble(tb_Step.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                        Results.Add((int)method, r);
                        break;
                    }
                case 5:
                    {
                        double r = Integral.SplainMethod(Convert.ToDouble(tb_Step.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                        Results.Add((int)method, r);
                        break;
                    }
                case 6:
                    {
                        double r = Integral.MonteKarloMethod(Convert.ToInt32(tb_SegmentNum.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                        Results.Add((int)method, r);
                        break;
                    }
                case 7:
                    {
                        double r = Integral.GeometricMonteCarloMethod(Convert.ToInt32(tb_PointsNum.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                        Results.Add((int)method, r);
                        break;
                    }
                case 8:
                    {
                        double r = Integral.GaussMethod(Convert.ToInt32(tb_Pow.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                        Results.Add((int)method, r);
                        break;
                    }
                case 9:
                    {
                        bool check = false;
                        foreach (int value in availableStepForChebyshev)
                        {
                            if (Convert.ToInt32(tb_Pow.Text) == value)
                                check = true;
                        }
                        if (check == false)
                            throw new NotAvailableChebyshevStepException("Недопустимая степень для метода Чебышева!\n Допустимые значения: 2; 3; 4; 5; 6; 7; 9");
                        else
                        {
                            double r = Integral.ChebyshevMethod(Convert.ToInt32(tb_Pow.Text), Convert.ToDouble(tb_A.Text), Convert.ToDouble(tb_B.Text), tb_Equation.Text);
                            Results.Add((int)method, r);
                            break;
                        }
                    }
            }
        }
        void MakeExtraDataAvailable()
        {
            foreach (int i in FindCheckedValues())
            {
                if (i >= 0 && i <= 5)
                    tb_Step.IsEnabled = true;
                else
                {
                    if (i == 8 || i == 9)
                        tb_Pow.IsEnabled = true;
                    else
                    {
                        if (i == 6)
                            tb_SegmentNum.IsEnabled = true;
                        else
                        {
                            if (i == 7)
                                tb_PointsNum.IsEnabled = true;
                        }
                    }
                }
            }
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MakeExtraDataAvailable();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            tb_Pow.IsEnabled = false; tb_Step.IsEnabled = false;
            tb_PointsNum.IsEnabled = false; tb_SegmentNum.IsEnabled = false;
            MakeExtraDataAvailable();

        }
    }
    public enum Method
    {
        [Description("Метод левых прямоугольников")] RectangleStraightMethod,
        [Description("Метод правых прямоугольников")] RectangleReverseMethod,
        [Description("Метод средних прямоугольников")] RectangleMiddleMethod,
        [Description("Метод трапеций")] TrapezoidMethod,
        [Description("Метод парабол (Симпсона)")] SimpsonMethod,
        [Description("Метод сплайнов")] SplainMethod,
        [Description("Метод Монте-Карло")] MonteKarloMethod,
        [Description("Геометрический  метод Монте-Карло")] GeometricMonteCarloMethod,
        [Description("Метод Гаусса")] GaussMethod,
        [Description("Метод Чебышева")] ChebyshevMethod
    };
    public class MethodValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Method format)
            {
                return GetString(format);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return Enum.Parse(typeof(Method), s.Substring(0, s.IndexOf(':')));
            }
            return null;
        }

        public string[] Strings => GetStrings();

        public static string GetString(Method method)
        {
            return GetDescription(method);
        }

        public static string GetDescription(Method format)
        {
            return format.GetType().GetMember(format.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description;

        }
        public static string[] GetStrings()
        {
            List<string> list = new List<string>();
            foreach (Method format in Enum.GetValues(typeof(Method)))
            {
                list.Add(GetString(format));
            }

            return list.ToArray();
        }
    }
    public static class Integral
    {
        public static double RectangleStraightMethod(double h, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;
            for (double x = a; x <= b; x += h)
            {
                mathParser.LocalVariables.Add("x", x);
                integral += mathParser.Parse(equation) * h;
                mathParser.LocalVariables.Clear();
            }
            return integral;
        }
        public static double RectangleReverseMethod(double h, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;
            for (double x = b; x >= a; x -= h)
            {
                mathParser.LocalVariables.Add("x", x);
                integral += mathParser.Parse(equation) * h;
                mathParser.LocalVariables.Clear();
            }
            return integral;
        }
        public static double RectangleMiddleMethod(double h, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;
            for (double x = a + h / 2; x <= b; x += h)
            {
                mathParser.LocalVariables.Add("x", x);
                integral += mathParser.Parse(equation) * h;
                mathParser.LocalVariables.Clear();
            }
            return integral;
        }
        public static double TrapezoidMethod(double h, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;
            for (double x = a; x <= b; x += h)
            {
                mathParser.LocalVariables.Add("x", x);
                double y = mathParser.Parse(equation);
                mathParser.LocalVariables.Clear();
                mathParser.LocalVariables.Add("x", x + h);
                double y1 = mathParser.Parse(equation);
                integral += (y + y1) * h;
                mathParser.LocalVariables.Clear();
            }
            integral *= 0.5;
            return integral;
        }
        public static double SimpsonMethod(double h, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;
            double evensum = 0, unevensum = 0;
            int count = 0;
            //ищем у для а и в
            mathParser.LocalVariables.Add("x", a);
            double y = mathParser.Parse(equation);
            integral = y;
            mathParser.LocalVariables.Clear();
            mathParser.LocalVariables.Add("x", b);
            y = mathParser.Parse(equation);
            integral += y;
            mathParser.LocalVariables.Clear();
            /////
            for (double x = a; x < b; x += h)
            {
                mathParser.LocalVariables.Add("x", x);
                y = mathParser.Parse(equation);

                if (count % 2 == 0)
                {
                    evensum += y;
                }
                else
                {
                    unevensum += y;
                }
                mathParser.LocalVariables.Clear();
                count++;
            }
            evensum *= 2;
            unevensum *= 4;
            integral += evensum + unevensum;
            integral *= h / 3;
            return integral;
        }
        public static double MonteKarloMethod(int n, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;
            Random r = new Random();
            for (int i = 0; i < n; i++)
            {
                double x = r.NextDouble();
                x += r.Next((int)a, (int)b);
                if (x < a || x > b)
                {
                    i--;
                    continue;
                }
                mathParser.LocalVariables.Add("x", x);
                integral += mathParser.Parse(equation);
                mathParser.LocalVariables.Clear();
            }
            integral *= (b - a) / n;
            return integral;
        }

        public static double GaussMethod(int n, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;
            double koef = (b - a) / 2;

            for (int i = 1; i <= n; i++)
            {
                double t = FindT(n, i, n - 1);
                double x = ((a + b) / 2) + (((b - a) * t) / 2);
                double f = 0;

                mathParser.LocalVariables.Add("x", x);
                f = mathParser.Parse(equation);
                mathParser.LocalVariables.Clear();
                double A = 2 / ((1 - Math.Pow(t, 2)) * Math.Pow(FindPr(t, n), 2));
                integral += f * A;
            }

            integral *= koef;
            return integral;
        }

        static double FindPr(double ti, int n)
        {
            return (n * (FindP(ti, n - 1) - ti * FindP(ti, n)) / (1 - Math.Pow(ti, 2)));
        }
        static double FindP(double t, int n)
        {
            if (n == 0)
                return 1;
            else
            {
                if (n == 1)
                    return t;
                else
                {
                    return (((2 * (n - 1)) + 1) * t * FindP(t, n - 1) / ((n - 1) + 1)) - (((n - 1) * FindP(t, n - 2)) / ((n - 1) + 1));
                }
            }
        }

        static double FindT(int n, int i, int k)
        {
            if (k == 0)
                return Math.Cos(Math.PI * (4 * i - 1) / (4 * n + 2));
            else
            {
                double nt = FindT(n, i, k - 1);
                double p = FindP(FindT(n, i, k - 1), n);
                double pr = FindPr(FindT(n, i, k - 1), n);
                return (nt - (p / pr));
            }
        }
        public static double GeometricMonteCarloMethod(int n, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;
            Random r = new Random();
            Random random = new Random();
            double k = 0;
            FindMaxMin(out double max, out double min, a, b, equation);
            double[] y = new double[n], x = new double[n];
            for (int i = 0; i < n; i++)
            {
                x[i] = r.Next((int)a, (int)b) + random.NextDouble();
                y[i] = r.Next((int)min, (int)max) + random.NextDouble();
            }

            for (int i = 0; i < x.Length; i++)
            {
                mathParser.LocalVariables.Add("x", x[i]);
                double f = mathParser.Parse(equation);
                mathParser.LocalVariables.Clear();
                if (f >= y[i])
                    k++;
            }
            double S = (max - min) * (b - a);
            double koef = k / n;
            integral = koef * S;
            return integral;
        }
        static void FindMaxMin(out double max, out double min, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double y = 0;
            max = Double.MinValue;
            min = Double.MaxValue;
            for (double x = a; x <= b; x += 0.01)
            {
                mathParser.LocalVariables.Add("x", x);
                y = mathParser.Parse(equation);
                mathParser.LocalVariables.Clear();
                if (y > max)
                    max = y;
                if (y < min)
                    min = y;
            }
        }

        public static double SplainMethod(double h, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;

            double sum = 0, p = 0;//p - погрешность, sum - первая сумма

            for (double x = a + h; x <= b; x += h)
            {
                mathParser.LocalVariables.Add("x", x);
                double y1 = mathParser.Parse(equation);
                mathParser.LocalVariables.Clear();
                mathParser.LocalVariables.Add("x", x - h);
                double y2 = mathParser.Parse(equation);
                mathParser.LocalVariables.Clear();
                sum += h * (y2 + y1);
                double proizvodnaya = HighOrderDerivatives(equation, x - h, h);
                p += Math.Pow(h, 3) * proizvodnaya;

            }
            integral = 0.5 * sum - (p / 12);
            return integral;
        }

        public static double HighOrderDerivatives(string equation, double x, double step)
        {
            double[,] points = LineMiddleInterpolation(equation, x, step);
            for (int i = 1; i < 2; i++)
            {
                points = LineMiddleInterpolation(equation, x, step);
            }

            return points[0, 1];
        }

        public static double[,] LineMiddleInterpolation(string equation, double x, double step)
        {
            double[,] pairList = new double[3, 2];
            double[,] points = new double[1, 2];
            MathParser mathParser = new MathParser();
            for (int i = 0; i < 3; i++)
            {
                mathParser.LocalVariables.Add("x", x);
                double y = mathParser.Parse(equation);
                mathParser.LocalVariables.Clear();
                pairList[i, 0] = x;
                pairList[i, 1] = y;
                x = x + step;
            }
            for (int i = 1; i < pairList.GetLength(0) - 1; i++)
            {
                double ypr = (pairList[i + 1, 1] - pairList[i - 1, 1]) / (2 * step);
                points[0, 0] = pairList[i, 0];
                points[0, 1] = ypr;
            }

            return points;
        }
        public static double ChebyshevMethod(int n, double a, double b, string equation)
        {
            MathParser mathParser = new MathParser();
            double integral = 0;

            double A = (2 / (double)n);
            double[][] ti =
            {
                new double[] { 0.5773350, -0.5773350 },
                new double[] { 0.707107, 0, -0.707107 },
                new double[] { 0.794654, 0.187592, -0.794654, -0.187592},
                new double[] { 0.832497, 0.374541, 0, -0.374541, -0.832497 },
                new double[] { 0.866247, 0.422519, 0.266635, -0.266635, -0.422519, -0.866247},
                new double[] { 0.883862, 0.529657, 0.323812, 0, -0.323812, -0.529657, -0.883862},
                new double[] { 0.911589, 0.601019, 0.528762, 0.167906, 0, -0.167906 , -0.528762 , -0.601019 , -0.911589 }
            };
            for (int i = 1; i <= n; i++)
            {
                double f = 0;
                double x = (a + b) / 2 + (b - a) * ti[n - 2][i - 1] / 2;
                mathParser.LocalVariables.Add("x", x);
                f = mathParser.Parse(equation);
                mathParser.LocalVariables.Clear();
                integral += f * A;
            }
            integral *= (b - a) / 2;
            return integral;
        }
    }
    public static class ErrorLimits
    {
        public static double Relative(double idealresult, double result)
        {

            double errorLimit = Absolute(idealresult, result) / Math.Abs(idealresult);
            return errorLimit;
        }
        public static double Absolute(double idealresult, double result)
        {
            double errorLimit = Math.Abs(idealresult - result);
            return errorLimit;
        }
    }

    public class NotAvailableChebyshevStepException : Exception
    {
        public NotAvailableChebyshevStepException() { }
        public NotAvailableChebyshevStepException(string message) : base(message) { }
        public NotAvailableChebyshevStepException(string message, Exception inner) : base(message, inner) { }
        public NotAvailableChebyshevStepException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    public class NotSelectedMethodException : Exception
    {
        public NotSelectedMethodException() { }
        public NotSelectedMethodException(string message) : base(message) { }
        public NotSelectedMethodException(string message, Exception inner) : base(message, inner) { }
        public NotSelectedMethodException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    public class NullEquationTextException : Exception
    {
        public NullEquationTextException() { }
        public NullEquationTextException(string message) : base(message) { }
        public NullEquationTextException(string message, Exception inner) : base(message, inner) { }
        public NullEquationTextException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    public class NullIdealEquationTextException : Exception
    {
        public NullIdealEquationTextException() { }
        public NullIdealEquationTextException(string message) : base(message) { }
        public NullIdealEquationTextException(string message, Exception inner) : base(message, inner) { }
        public NullIdealEquationTextException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    public class NotSelectedRowException : Exception
    {
        public NotSelectedRowException() { }
        public NotSelectedRowException(string message) : base(message) { }
        public NotSelectedRowException(string message, Exception inner) : base(message, inner) { }
        public NotSelectedRowException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    public class NoAnalisysResultsYetException : Exception
    {
        public NoAnalisysResultsYetException() { }
        public NoAnalisysResultsYetException(string message) : base(message) { }
        public NoAnalisysResultsYetException(string message, Exception inner) : base(message, inner) { }
        public NoAnalisysResultsYetException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
