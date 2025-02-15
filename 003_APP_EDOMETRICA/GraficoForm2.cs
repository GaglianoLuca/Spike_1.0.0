using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MathNet.Numerics.Interpolation;

using MathNet.Numerics;

using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;
using Series = System.Windows.Forms.DataVisualization.Charting.Series;
using TinySpline;
using System.Threading;
using Microsoft.Office.Interop.Excel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

public class GraficoForm : Form
{
    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.CheckBox checkBox1;
    private System.Windows.Forms.CheckBox checkBox2;
    private System.Windows.Forms.CheckBox checkBox3;
    private System.Windows.Forms.CheckBox checkBox4;
    private System.Windows.Forms.CheckBox checkBox5;
    private NumericUpDown numericUpDown5;
    private System.Windows.Forms.DataVisualization.Charting.Chart chart1;

    double[] xSegmento;
    int maxCurvatureIndex;
    CubicSpline splineYsinistra;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.Label label8;
    private NumericUpDown numericUpDown6;
    private PictureBox pictureBox1;
    double xMaxCurvature;

    public GraficoForm(double[,] data)
    {
        //--------------------------------------------------------------------------
        // INIZIALIZZO GRAFICO

        InitializeComponent();
        chart1.Titles.Add("Prova edometrica");
        
        // Imposta lo zoom per l'asse X
        chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
        chart1.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;

        // Imposta lo zoom per l'asse Y
        chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
        chart1.ChartAreas[0].AxisY.ScrollBar.IsPositionedInside = true;

        // Abilita la selezione dell'intervallo per lo zoom
        chart1.ChartAreas[0].CursorX.IsUserEnabled = true;
        chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
        chart1.ChartAreas[0].CursorY.IsUserEnabled = true;
        chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;

        //--------------------------------------------------------------------------
        //--------------------------------------------------------------------------
        // 1 CREO SPLINE CON IMPOSTAZIONI DI BASE DI WINDOWSCHART
        // 1.1 Creazione della serie per i valori registrati dalla prova edometrica
        //--------------------------------------------------------------------------
        //--------------------------------------------------------------------------
        var dataSeries = new System.Windows.Forms.DataVisualization.Charting.Series
        {
            Name = "Step registrati",
            ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline,
            MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle,
            MarkerSize = 8,
            MarkerColor = Color.Red

        };

        List<double> xList = new List<double>();
        List<double> yList = new List<double>();


        for (int i = 0; i < data.GetLength(0); i++)
        {
            if (data[i, 0] > 0 && data[i, 1] > 0)
            {
                double sigma = data[i, 0];
                double deltaE = data[i, 1];
                dataSeries.Points.AddXY(sigma, deltaE);
                xList.Add(sigma);
                yList.Add(deltaE);
            }
        }
        //--------------------------------------------------------------------------
        //--------------------------------------------------------------------------
        // 1 CREO SPLINE CON IMPOSTAZIONI DI BASE DI WINDOWSCHART
        // 1.2 Aggiungo la serie al grafico
        //--------------------------------------------------------------------------
        //--------------------------------------------------------------------------

        chart1.Series.Add(dataSeries);
        chart1.ChartAreas[0].AxisY.IsReversed = true;
        chart1.ChartAreas[0].AxisY.IsReversed = true;
        chart1.ChartAreas[0].AxisX.IsLogarithmic = true;
        chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = true;
        chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = true;
        chart1.ChartAreas[0].AxisX.MinorGrid.LineDashStyle = ChartDashStyle.Solid;
        chart1.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.LightGray;

        //--------------------------------------------------------------------------
        //--------------------------------------------------------------------------
        // 2 CREO LA MIA SPLINE CON LA LIBRERIA MATHNET
        // 2.1 divido il mio dominio in segmenti crescenti e
        // decrescenti di carico (fasi della prova edometrica)
        // quando ho una fase di scarico, inverto quel vettore per generare la spline
        //--------------------------------------------------------------------------
        //--------------------------------------------------------------------------



        List<double[]> segmentiX = new List<double[]>();
        List<double[]> segmentiY = new List<double[]>();

        List<double> tempX = new List<double>();
        List<double> tempY = new List<double>();

        int contatore = 0;
        bool crescente = true;
        int numero = 0;

        for (int i = 0; i < data.GetLength(0); i++)
        {
            if (data[i, 0] > 0 && data[i, 1] > 0)
            {
                numero = numero + 1;

                if (tempX.Count == 0) // Inizia una nuova serie
                {
                    if (contatore == 1)
                    {
                        tempX.Add(data[i - 1, 0]);
                        tempY.Add(data[i - 1, 1]);
                        contatore = 0;
                    }
                    tempX.Add(data[i, 0]);
                    tempY.Add(data[i, 1]);
                }
                else
                {
                    // Controlla se la serie è crescente o decrescente
                    bool nuovoCrescente = data[i, 0] >= tempX[tempX.Count - 1];

                    if (nuovoCrescente == crescente)
                    {
                        // Continua la serie corrente
                        tempX.Add(data[i, 0]);
                        tempY.Add(data[i, 1]);
                    }
                    else
                    {

                        // Cambiamento di tendenza: salva la serie corrente e inizia una nuova
                        segmentiX.Add(tempX.ToArray());
                        segmentiY.Add(tempY.ToArray());
                        contatore = 1;

                        // Reinizializza tempX e tempY con il nuovo inizio di serie
                        if (contatore == 1)
                        {

                            tempX = new List<double> { data[i - 1, 0] };
                            tempY = new List<double> { data[i - 1, 1] };
                            tempX.Add(data[i, 0]);
                            tempY.Add(data[i, 1]);
                        }

                        // Cambia il trend
                        crescente = nuovoCrescente;
                    }
                }
            }
        }

        // ultimo segmento
        if (tempX.Count > 0)
        {
            segmentiX.Add(tempX.ToArray());
            segmentiY.Add(tempY.ToArray());
        }
        int contatoreTangente = 0;
        int Cc = 0;
        int Cr = 0;
        // per tutti i segmenti di spline
        for (int k = 0; k < segmentiX.Count; k++)
        {
           xSegmento = segmentiX[k];
            double[] ySegmento = segmentiY[k];
            // Inverto i vettori decrescenti
            if (k % 2 == 0)
            { }
            else
            {
                Array.Reverse(xSegmento);
                Array.Reverse(ySegmento);
            }

            // stampo i miei segmenti per un controllo
            for (int kk = 0; kk < segmentiX.Count; kk++)
            {
                double[] xSegmento1 = segmentiX[kk];
                double[] ySegmento1 = segmentiY[kk];

                Console.WriteLine($"Segmento {kk + 1}:");

                for (int j = 0; j < xSegmento1.Length; j++)
                {
                    Console.WriteLine($"  Sigma: {xSegmento1[j]}, DeltaE: {ySegmento1[j]}");
                }

                Console.WriteLine();
            }


            // Crea una spline y(x) per la sottoserie corrente
            // e proposta di interpolazione con una funzione diversa
            // var splineY = CubicSpline.InterpolatePchip(xSegmento, ySegmento);
            var splineY = CubicSpline.InterpolateNatural(xSegmento, ySegmento);
            if (contatoreTangente == 0)
            {
               splineYsinistra=CubicSpline.InterpolateNatural(xSegmento, ySegmento);
                //sto salvando la prima per il comando di disegnaSinistra
            }

            // Crea una nuova serie per la spline della sottoserie          
            double lastX = xSegmento[xSegmento.Length - 1];
            var splineSeries = new Series
            {
                Name = $"splineSeries_{k}",
                ChartType = SeriesChartType.Line,
                Color = Color.Blue,
                BorderWidth = 2
            };

            // Aggiungi i punti interpolati della spline alla serie
            for (double xi = xSegmento[0]; xi <= lastX; xi += 0.01)
            {
                double yi = splineY.Interpolate(xi);
                splineSeries.Points.AddXY(xi, yi);
            }


            // Aggiungi la serie della spline al grafico
            chart1.Series.Add(splineSeries);

            //--------------------------------------------------------------------------
            //--------------------------------------------------------------------------
            // 3 DISEGNO LA TANGENTE, L'ORIZZONTALE E LA BISETTRICE NEL PUNTO DI MAX CURVATURA
            // 3.1 trovo il punto di max curvatura
            //--------------------------------------------------------------------------
            //--------------------------------------------------------------------------

            double maxCurvature = 0;
           


            // Scansiona xSegmento per trovare il punto di massima curvatura
            for (double xi = xSegmento[0]; xi <= lastX; xi += 0.01)
            {
                double y1 = splineY.Differentiate(xi);
                double y2 = splineY.Differentiate2(xi);

                // Calcola la curvatura usando la formula |y''| / (1 + y'^2)^(3/2)
                double denominator = Math.Pow(1 + y1 * y1, 1.5);
                if (denominator != 0)
                {
                    double curvature = Math.Abs(y2) / denominator;

                    if (curvature > maxCurvature)
                    {
                        maxCurvature = curvature;
                        xMaxCurvature = xi;


                    }
                    Console.WriteLine("controllo " + xMaxCurvature);
                    Console.WriteLine(" curvatura " + maxCurvature);
                }
            }

            //--------------------------------------------------------------------------
            //--------------------------------------------------------------------------
            // 3 DISEGNO LA TANGENTE, L'ORIZZONTALE E LA BISETTRICE NEL PUNTO DI MAX CURVATURA
            // 3.2 traccio la tangente solo per il primo segmento interpolato
            // (suppongo di avere max curvatura in questa prima fase di carico)
            //--------------------------------------------------------------------------
            //--------------------------------------------------------------------------

            if (contatoreTangente == 0)
            {
                //-------------------------------------------------------------------------
                // Ottieni i valori della funzione e della derivata nel punto di massima curvatura
                double yAtMaxCurvature = splineY.Interpolate(xMaxCurvature);
                double slopeAtMaxCurvature = splineY.Differentiate(xMaxCurvature);
                double adjustedSlope = slopeAtMaxCurvature * xMaxCurvature * Math.Log(10);
                Console.WriteLine("Pendenza (slope) della tangente al punto di massima curvatura: " + slopeAtMaxCurvature);
                Console.WriteLine("ascissa: " + xMaxCurvature);

                // Definisci la funzione della tangente al punto di massima curvatura
                Func<double, double> tangentLine = x => yAtMaxCurvature + (adjustedSlope * ((Math.Log10(x) - Math.Log10(xMaxCurvature))));

                // Intervallo per tracciare la tangente
                double tangentStart = xMaxCurvature - 10;
                double tangentEnd = xMaxCurvature + 200;

                // Creazione della serie per la tangente
                Series tangentSeries = new Series("TanMax")
                {
                    Name = $"TanMax",
                    ChartType = SeriesChartType.Line,
                    Color = Color.Red,
                    BorderWidth = 1
                };

                // Aggiungi i punti della tangente alla serie
                for (double x = tangentStart; x <= tangentEnd; x += 0.01)
                {
                    double y = tangentLine((x));
                    tangentSeries.Points.AddXY(x, y);
                }

                // Aggiungi la serie della tangente al grafico

                chart1.Series.Add(tangentSeries);
                chart1.Invalidate();

                //--------------------------------------------------------------------------
                //--------------------------------------------------------------------------
                // 3 DISEGNO LA TANGENTE, L'ORIZZONTALE E LA BISETTRICE NEL PUNTO DI MAX CURVATURA
                // 3.3 traccio la retta orizzontale
                //--------------------------------------------------------------------------
                //--------------------------------------------------------------------------
                //-------------------------------------------------------------------------


                var horizontalLine = new Series
                {
                    Name = $"orizzontale",
                    ChartType = SeriesChartType.Line,
                    Color = Color.Red, // Cambia il colore come preferisci
                    BorderWidth = 1 // Spessore della linea
                };
                horizontalLine.Points.AddXY(xMaxCurvature, yAtMaxCurvature);
                horizontalLine.Points.AddXY(xMaxCurvature * 50, yAtMaxCurvature); // Estendi la retta a destra

                // Aggiungi la serie al grafico
                chart1.Series.Add(horizontalLine);

                //--------------------------------------------------------------------------
                //--------------------------------------------------------------------------
                // 3 DISEGNO LA TANGENTE, L'ORIZZONTALE E LA BISETTRICE NEL PUNTO DI MAX CURVATURA
                // 3.3 traccio la bisettrice
                //--------------------------------------------------------------------------
                //--------------------------------------------------------------------------
                //-------------------------------------------------------------------------
                //-------------------------------------------------------------------------

                double bisectorSlope = Math.Tan(0.5 * Math.Atan(adjustedSlope));



                // Crea una nuova serie per la bisettrice
                var bisectorLine = new Series
                {
                    Name = $"bisettrice",
                    ChartType = SeriesChartType.Line,
                    Color = Color.Green, // Colore della bisettrice
                    BorderWidth = 1 // Spessore della linea
                };

                // Punto di partenza della bisettrice
                double xStart = xMaxCurvature;
                double yStart = yAtMaxCurvature;

                // Punto finale della bisettrice (a destra)
                double xEnd = xMaxCurvature * 50;
                double yEnd = yStart + bisectorSlope * (Math.Log10(xEnd) - Math.Log10(xStart));

                // Aggiungi i punti alla serie della bisettrice
                bisectorLine.Points.AddXY(xStart, yStart);
                bisectorLine.Points.AddXY(xEnd, yEnd);

                // Aggiungi la serie al grafico
                chart1.Series.Add(bisectorLine);

                contatoreTangente = 1;

                //---------
                //----------------------------------------------------------------

            }
            // Trova l'indice del punto di massima curvatura
            //--------------------------------------------------------------------------------------
            //--------------------------------------------------------------------------------------

            //--------------------------------------------------------------------------------------

            //--------------------------------------------------------------------------------------

            if (Cc == 0)
            {
                /*
                
                maxCurvatureIndex = Array.FindIndex(xSegmento, x => x >= xMaxCurvature);

                if (maxCurvatureIndex < 0 && maxCurvatureIndex >= xSegmento.Length - 1)
                {
                    Console.WriteLine("Errore: punto di massima curvatura non valido o non c'è un punto a destra nella sottoserie.");
                    return;
                }

                //traccio la tangente dal punto successivo
                double tangenteCcX = xSegmento[maxCurvatureIndex + 1];


                double tangenteCcY = splineY.Interpolate(tangenteCcX);


                // Ottieni la derivata (pendenza) al punto di max curvatura
                double slopeAtMaxCurvatureCc = splineY.Differentiate(tangenteCcX);

                double pendenzaCcLog = slopeAtMaxCurvatureCc * tangenteCcX * Math.Log(10);



                //definisco la funzione della tangente
                Func<double, double> tangenteCc = x => tangenteCcY + pendenzaCcLog * (Math.Log10(x) - Math.Log10(tangenteCcX));

                // Intervallo 
                double tangenteCcEnd = xMaxCurvature;

                // Creazione della serie per la tangente
                Series serieCc = new Series("TanCc")
                {
                    Name = $"TanCc",
                    ChartType = SeriesChartType.Line,
                    Color = Color.Purple,
                    BorderWidth = 1
                };

                //inserisco i valori nella serie
                for (double x = tangenteCcX; x >= tangenteCcEnd; x -= 0.01)
                {
                    double y = tangenteCc(x);
                    serieCc.Points.AddXY(x, y);


                }
                //    numericUpDown1.Value = (decimal)pendenzaCcLog;
                // numericUpDown2.Value = (decimal)tangenteCcY;
                // Aggiungi la serie della tangente al grafico
                chart1.Series.Add(serieCc);
                chart1.Invalidate();

                Cc = 1; */
            }

            if (Cr == 0)
            { 
                /*
                int maxCurvatureIndex = Array.FindIndex(xSegmento, x => x <= xMaxCurvature);

                if (maxCurvatureIndex < 0 && maxCurvatureIndex >= xSegmento.Length - 1)
                {
                    Console.WriteLine("Errore: punto di massima curvatura non valido o non c'è un punto a destra nella sottoserie.");
                    return;
                }

                //traccio la tangente dal punto successivo
                double tangenteCrX = xSegmento[maxCurvatureIndex];


                double tangenteCrY = splineY.Interpolate(tangenteCrX);


                // Ottieni la derivata (pendenza) al punto di max curvatura
                double slopeAtMaxCurvatureCr = splineY.Differentiate(tangenteCrX);



                double pendenzaCrLog = slopeAtMaxCurvatureCr * tangenteCrX * Math.Log(10);

                //  numericUpDown4.Value = (decimal)pendenzaCrLog;
                //  numericUpDown3.Value = (decimal)tangenteCrY;

                //definisco la funzione della tangente
                Func<double, double> tangenteCr = x => tangenteCrY + pendenzaCrLog * (Math.Log10(x) - Math.Log10(tangenteCrX));

                // Intervallo 
                double tangenteCrEnd = xSegmento[maxCurvatureIndex + 2];

                // Creazione della serie per la tangente
                Series serieCr = new Series("TanCr")
                {
                    Name = $"TanCr",
                    ChartType = SeriesChartType.Line,
                    Color = Color.Purple,
                    BorderWidth = 1
                };

                //inserisco i valori nella serie
                for (double x = tangenteCrX; x <= tangenteCrEnd; x += 0.01)
                {
                    double y = tangenteCr(x);
                    serieCr.Points.AddXY(x, y);

                    Console.WriteLine(x);
                    Console.WriteLine(y);
                }

                // Aggiungi la serie della tangente al grafico
                chart1.Series.Add(serieCr);
                chart1.Invalidate();

                Cr = 1;
                */
            }

            /* 
  


   
              */

        }
    }


    private void InitializeComponent()
    {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GraficoForm));
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.button1 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.numericUpDown5 = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.numericUpDown6 = new System.Windows.Forms.NumericUpDown();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // chart1
            // 
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(12, 12);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(642, 465);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "chart1";
            this.chart1.Click += new System.EventHandler(this.chart1_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(679, 23);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Stampa";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(679, 351);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(93, 20);
            this.checkBox1.TabIndex = 12;
            this.checkBox1.Text = "orizzontale";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Location = new System.Drawing.Point(679, 378);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(83, 20);
            this.checkBox2.TabIndex = 13;
            this.checkBox2.Text = "bisettrice";
            this.checkBox2.UseVisualStyleBackColor = true;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Checked = true;
            this.checkBox3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox3.Location = new System.Drawing.Point(679, 404);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(81, 20);
            this.checkBox3.TabIndex = 15;
            this.checkBox3.Text = "tangente";
            this.checkBox3.UseVisualStyleBackColor = true;
            this.checkBox3.CheckedChanged += new System.EventHandler(this.checkBox3_CheckedChanged);
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Checked = true;
            this.checkBox4.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox4.Location = new System.Drawing.Point(679, 430);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(45, 20);
            this.checkBox4.TabIndex = 14;
            this.checkBox4.Text = "Cc";
            this.checkBox4.UseVisualStyleBackColor = true;
            this.checkBox4.CheckedChanged += new System.EventHandler(this.checkBox4_CheckedChanged);
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Checked = true;
            this.checkBox5.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox5.Location = new System.Drawing.Point(679, 456);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(42, 20);
            this.checkBox5.TabIndex = 16;
            this.checkBox5.Text = "Cr";
            this.checkBox5.UseVisualStyleBackColor = true;
            this.checkBox5.CheckedChanged += new System.EventHandler(this.checkBox5_CheckedChanged);
            // 
            // numericUpDown5
            // 
            this.numericUpDown5.Location = new System.Drawing.Point(669, 87);
            this.numericUpDown5.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown5.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numericUpDown5.Name = "numericUpDown5";
            this.numericUpDown5.Size = new System.Drawing.Size(120, 22);
            this.numericUpDown5.TabIndex = 17;
            this.numericUpDown5.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown5.ValueChanged += new System.EventHandler(this.numericUpDown5_ValueChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(669, 65);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(154, 16);
            this.label7.TabIndex = 18;
            this.label7.Text = "AncoraggioPendenzaCc";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(669, 128);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(151, 16);
            this.label8.TabIndex = 20;
            this.label8.Text = "AncoraggioPendenzaCr";
            // 
            // numericUpDown6
            // 
            this.numericUpDown6.Location = new System.Drawing.Point(669, 150);
            this.numericUpDown6.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown6.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numericUpDown6.Name = "numericUpDown6";
            this.numericUpDown6.Size = new System.Drawing.Size(120, 22);
            this.numericUpDown6.TabIndex = 19;
            this.numericUpDown6.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown6.ValueChanged += new System.EventHandler(this.numericUpDown6_ValueChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.InitialImage")));
            this.pictureBox1.Location = new System.Drawing.Point(826, 211);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(242, 265);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 21;
            this.pictureBox1.TabStop = false;
            // 
            // GraficoForm
            // 
            this.ClientSize = new System.Drawing.Size(1080, 489);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.numericUpDown6);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.numericUpDown5);
            this.Controls.Add(this.checkBox5);
            this.Controls.Add(this.checkBox3);
            this.Controls.Add(this.checkBox4);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.chart1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GraficoForm";
            this.Text = "Spike Chart 1.0.0";
            this.Load += new System.EventHandler(this.GraficoForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    private void chart1_Click(object sender, EventArgs e)
    {

    }

    private void button1_Click(object sender, EventArgs e)
    {
        chart1.Printing.PrintPreview();
    }

    private void DrawLine()
    {
        
    }
    private void DrawLine2()
    {
      
    }


    private void numericUpDown1_ValueChanged(object sender, EventArgs e)
    {
        DrawLine();
    }

    private void numericUpDown2_ValueChanged(object sender, EventArgs e)
    {
        DrawLine();
    }

    private void numericUpDown4_ValueChanged(object sender, EventArgs e)
    {
        DrawLine2();
    }

    private void numericUpDown3_ValueChanged(object sender, EventArgs e)
    {
        DrawLine2();
    }

    private void GraficoForm_Load(object sender, EventArgs e)
    {

    }
    private void GeneraSerie() { }

    private void DisegnaDestra(double[] xSegmento, int maxCurvatureIndex, decimal i, CubicSpline splineY, double xMaxCurvature)
    {
         maxCurvatureIndex = Array.FindIndex(xSegmento, x => x <= xMaxCurvature);

        if (maxCurvatureIndex < 0 && maxCurvatureIndex >= xSegmento.Length - 1)
        {
            Console.WriteLine("Errore: punto di massima curvatura non valido o non c'è un punto a destra nella sottoserie.");
            return;
        }

        //traccio la tangente dal punto successivo
        double tangenteCrX = xSegmento[maxCurvatureIndex];
        double f = Decimal.ToDouble(i);
        double tangenteCrXvar = tangenteCrX + f;

        double tangenteCrY = splineY.Interpolate(tangenteCrXvar);


        // Ottieni la derivata (pendenza) al punto di max curvatura
        double slopeAtMaxCurvatureCr = splineY.Differentiate(tangenteCrXvar);



        double pendenzaCrLog = slopeAtMaxCurvatureCr * tangenteCrXvar * Math.Log(10);

        //  numericUpDown4.Value = (decimal)pendenzaCrLog;
        //  numericUpDown3.Value = (decimal)tangenteCrY;

        //definisco la funzione della tangente
        Func<double, double> tangenteCr = x => tangenteCrY + pendenzaCrLog * (Math.Log10(x) - Math.Log10(tangenteCrXvar));

        // Intervallo 
        double tangenteCrEnd = xMaxCurvature + (0.8 * xMaxCurvature);

        // Creazione della serie per la tangente
        Series serieCr = new Series("TanCr")
        {
            Name = $"TanCr",
            ChartType = SeriesChartType.Line,
            Color = Color.Purple,
            BorderWidth = 1
        };

        //inserisco i valori nella serie
        for (double x = tangenteCrXvar; x <= tangenteCrEnd; x += 0.01)
        {
            double y = tangenteCr(x);
            serieCr.Points.AddXY(x, y);

          
        }

        List<Series> seriesToRemove = new List<Series>();

        foreach (var series in chart1.Series)
        {
            if (series.Name.StartsWith("TanCr"))
            {
                seriesToRemove.Add(series);

            }


        }
        // Rimuovi le serie raccolte
        foreach (var series in seriesToRemove)
        {
            chart1.Series.Remove(series);
        }

        // Aggiungi la serie della tangente al grafico
        chart1.Series.Add(serieCr);
        chart1.Invalidate();

      
    }
    private void DisegnaSinistra(double[] xSegmento, int maxCurvatureIndex, decimal i, CubicSpline splineY, double xMaxCurvature )
    {
        
        //traccio la tangente dal punto successivo
        double tangenteCcX = xSegmento[maxCurvatureIndex + 1];
        double f = Decimal.ToDouble(i);
        double tangenteCcXvar = tangenteCcX + f;

        double tangenteCcY = splineY.Interpolate(tangenteCcXvar);


        // Ottieni la derivata (pendenza) al punto di max curvatura
        double slopeAtMaxCurvatureCc = splineY.Differentiate(tangenteCcXvar);

        double pendenzaCcLog = slopeAtMaxCurvatureCc * tangenteCcXvar * Math.Log(10);



        //definisco la funzione della tangente
        Func<double, double> tangenteCc = x => tangenteCcY + pendenzaCcLog * (Math.Log10(x) - Math.Log10(tangenteCcXvar));

        // Intervallo 
        double tangenteCcEnd = xMaxCurvature-(0.2*xMaxCurvature);

        // Creazione della serie per la tangente
        Series serieCc = new Series("TanCc")
        {
            Name = $"TanCc",
            ChartType = SeriesChartType.Line,
            Color = Color.Purple,
            BorderWidth = 1
        };

        //inserisco i valori nella serie
        for (double x = tangenteCcXvar; x >= tangenteCcEnd; x -= 0.01)
        {
            double y = tangenteCc(x);
            serieCc.Points.AddXY(x, y);


        }

        List<Series> seriesToRemove = new List<Series>();

        foreach (var series in chart1.Series)
        {
            if (series.Name.StartsWith("TanCc"))
            {
                seriesToRemove.Add(series);
                
            }


        }
        // Rimuovi le serie raccolte
        foreach (var series in seriesToRemove)
        {
            chart1.Series.Remove(series);
        }

        chart1.Series.Add(serieCc);
        chart1.Invalidate();

    }
    private void checkBox1_CheckedChanged(object sender, EventArgs e)
    {
        // Controlla se la serie con il prefisso "splineSeries" esiste e aggiorna la visibilità
        foreach (var series in chart1.Series)
        {
            if (series.Name.StartsWith("orizzontale"))
            {
                series.Enabled = checkBox1.Checked;
            }

        }
    }

    private void checkBox2_CheckedChanged(object sender, EventArgs e)
    {
        // Controlla se la serie con il prefisso "splineSeries" esiste e aggiorna la visibilità
        foreach (var series in chart1.Series)
        {
            if (series.Name.StartsWith("bisettrice"))
            {
                series.Enabled = checkBox2.Checked;
            }

        }

    }

    private void checkBox3_CheckedChanged(object sender, EventArgs e)
    {
        // Controlla se la serie con il prefisso "splineSeries" esiste e aggiorna la visibilità
        foreach (var series in chart1.Series)
        {
            if (series.Name.StartsWith("TanMax"))
            {
                series.Enabled = checkBox3.Checked;
            }

        }
    }

    private void checkBox4_CheckedChanged(object sender, EventArgs e)
    {
        // Controlla se la serie con il prefisso "splineSeries" esiste e aggiorna la visibilità
        foreach (var series in chart1.Series)
        {
            if (series.Name.StartsWith("TanCc"))
            {
                series.Enabled = checkBox4.Checked;
            }

        }
    }

    private void checkBox5_CheckedChanged(object sender, EventArgs e)
    {
        // Controlla se la serie con il prefisso "splineSeries" esiste e aggiorna la visibilità
        foreach (var series in chart1.Series)
        {
            if (series.Name.StartsWith("TanCr"))
            {
                series.Enabled = checkBox5.Checked;
            }

        }
    }

    private void numericUpDown5_ValueChanged(object sender, EventArgs e)
    {
        DisegnaSinistra(xSegmento, maxCurvatureIndex, numericUpDown5.Value, splineYsinistra, xMaxCurvature);

    }

    private void numericUpDown6_ValueChanged(object sender, EventArgs e)
    {
        DisegnaDestra(xSegmento, maxCurvatureIndex, numericUpDown6.Value, splineYsinistra, xMaxCurvature);

    }
}
