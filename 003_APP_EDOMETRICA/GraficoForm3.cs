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

public class GraficoForm : Form
{
    private System.Windows.Forms.Button button1;
    private NumericUpDown numericUpDown1;
    private NumericUpDown numericUpDown2;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label6;
    private NumericUpDown numericUpDown3;
    private NumericUpDown numericUpDown4;
    private System.Windows.Forms.DataVisualization.Charting.Chart chart1;

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

        int contatore=0;
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
                           
                            tempX = new List<double> { data[i-1, 0] };
                            tempY = new List<double> { data[i-1, 1] };
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
        int Cc= 0;
        int Cr= 0;   
      // per tutti i segmenti di spline
        for (int k = 0; k < segmentiX.Count; k++)
        {
            double[] xSegmento = segmentiX[k];
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
            (double maxCurvature, double xMaxCurvature) = MaxCurvatura(xSegmento, lastX, splineY);


           /* //--------------------------------------------------------------------------
            //--------------------------------------------------------------------------
            // 3 DISEGNO LA TANGENTE, L'ORIZZONTALE E LA BISETTRICE NEL PUNTO DI MAX CURVATURA
            // 3.1 trovo il punto di max curvatura
            //--------------------------------------------------------------------------
            //--------------------------------------------------------------------------

            double maxCurvature = 0;
            double xMaxCurvature = 0;
            

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
           */


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
                Series tangentSeries = new Series("Tangente al max. curvatura")
                {
                    Name = $"tangente al punto di max curvatura",
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

                double bisectorSlope = adjustedSlope / 2.0;
                


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

            if (Cc == 0)
            {
                int maxCurvatureIndex = Array.FindIndex(xSegmento, x => x >= xMaxCurvature);

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
                Series serieCc = new Series("Tangente a partire dal punto successivo al max. curvatura")
                {
                    Name = $"Cc",
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
              //  numericUpDown1.Value = (decimal)pendenzaCcLog;
              //  numericUpDown2.Value = (decimal)tangenteCcY;
                // Aggiungi la serie della tangente al grafico
                chart1.Series.Add(serieCc);
                chart1.Invalidate();

                Cc = 1;
            }

            if (Cr == 0)
            {
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
             //   numericUpDown3.Value = (decimal)tangenteCrY;

                //definisco la funzione della tangente
                Func<double, double> tangenteCr = x => tangenteCrY + pendenzaCrLog * (Math.Log10(x) - Math.Log10(tangenteCrX));

                // Intervallo 
                double tangenteCrEnd = xSegmento[maxCurvatureIndex + 2];

                // Creazione della serie per la tangente
                Series serieCr = new Series("Tangente a partire dal punto precendete al max. curvatura")
                {
                    Name = $"Cr",
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
            }

            /* 


    //---------------------------------------------------------------------------------
    // Trova l'intersezione tra l'ultima tangente e la bisettrice
    double tolerance = 0.001; // tolleranza per determinare il punto di intersezione
    double tangencyX = 0;
    double tangencyY = 0;
    bool foundTangencyPoint = false;

    for (double beta = tangentEnd2; beta <= tangentStartX; beta += 0.01)
    {
     // Valori di y per la tangente e la bisettrice
     double yTangent = tangentLine2(beta);
     double yBisector = yStart + bisectorSlope * (Math.Log10(beta) - Math.Log10(xStart));

     // Calcola la differenza tra la tangente e la bisettrice
     if (Math.Abs(yTangent - yBisector) < tolerance)
     {
         // Se la differenza è minore della tolleranza, abbiamo trovato l'intersezione
         tangencyX = beta;
         tangencyY = yTangent; // o yBisector, sono praticamente uguali in questo punto
         foundTangencyPoint = true;
         break;
     }
    }

    // Plotta il punto di tangenza, se trovato
    if (foundTangencyPoint)
    {
     Series tangencyPointSeries = new Series("Punto di tangenza")
     {
         ChartType = SeriesChartType.Point,
         Color = Color.Blue,
         MarkerSize = 10,
         MarkerStyle = MarkerStyle.Diamond
     };

     tangencyPointSeries.Points.AddXY(tangencyX, tangencyY);
     chart1.Series.Add(tangencyPointSeries);
     Console.WriteLine($"Punto di tangenza trovato a x = {tangencyX}, y = {tangencyY}");
    }

    chart1.Invalidate();

    // Trova l'indice del punto precedente a maxCurvature nella sottoserie
    int prevMaxCurvatureIndex = maxCurvatureIndex - 1;
    if (prevMaxCurvatureIndex < 0  prevMaxCurvatureIndex >= xSegmento.Length - 1)
    {
     Console.WriteLine("Errore: punto precedente al max. curvatura non valido o non c'è un punto a sinistra nella sottoserie.");
    }
    else
    {
     // Punto di partenza per la tangente: il primo punto precedente al maxCurvature
     double tangentPrevX = xSegmento[prevMaxCurvatureIndex];
     double tangentPrevY = splineY.Interpolate(tangentPrevX);

     // Ottieni la derivata (pendenza) al punto di max curvatura
     double slopeAtMaxCurvaturePrev = splineY.Differentiate(xMaxCurvature);
     double adjustedSlopePrev = slopeAtMaxCurvaturePrev * tangentPrevX * Math.Log(10);

     // Definisci la funzione della tangente al punto di partenza
     Func<double, double> tangentLinePrev = x => tangentPrevY + adjustedSlopePrev * (Math.Log10(x) - Math.Log10(tangentPrevX));

    LukeX, [12/11/2024 11:35]
    // Intervallo per tracciare la tangente a partire da tangentPrevX
     double tangentEndPrev = tangentPrevX+200; // o un altro valore minimo secondo le tue esigenze

     // Creazione della serie per la tangente
     Series tangentSeriesPrev = new Series("Tangente a partire dal punto precedente al max. curvatura")
     {
         Name = $"tangente_prev_{k}",
         ChartType = SeriesChartType.Line,
         Color = Color.Purple,
         BorderWidth = 1
     };

     // Aggiungi i punti della tangente alla serie
     for (double x = tangentPrevX; x <= tangentEndPrev; x += 0.01)
     {
         double y = tangentLinePrev(x);
         tangentSeriesPrev.Points.AddXY(x, y);
     }

     // Aggiungi la serie della tangente al grafico
     chart1.Series.Add(tangentSeriesPrev);
     chart1.Invalidate();
    }
              */

        }
    }
        
    
    private void InitializeComponent()
    {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.button1 = new System.Windows.Forms.Button();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.numericUpDown3 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown4 = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown4)).BeginInit();
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
            // numericUpDown1
            // 
            this.numericUpDown1.DecimalPlaces = 4;
            this.numericUpDown1.Increment = new decimal(new int[] {
            2,
            0,
            0,
            131072});
            this.numericUpDown1.Location = new System.Drawing.Point(689, 108);
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 22);
            this.numericUpDown1.TabIndex = 2;
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.DecimalPlaces = 4;
            this.numericUpDown2.Increment = new decimal(new int[] {
            2,
            0,
            0,
            131072});
            this.numericUpDown2.Location = new System.Drawing.Point(689, 136);
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(120, 22);
            this.numericUpDown2.TabIndex = 3;
            this.numericUpDown2.ValueChanged += new System.EventHandler(this.numericUpDown2_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(664, 77);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 16);
            this.label1.TabIndex = 4;
            this.label1.Text = "y1=aX+b";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(661, 113);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(15, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "a";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(661, 142);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(15, 16);
            this.label3.TabIndex = 6;
            this.label3.Text = "b";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(661, 260);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(15, 16);
            this.label4.TabIndex = 11;
            this.label4.Text = "b";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(661, 231);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(15, 16);
            this.label5.TabIndex = 10;
            this.label5.Text = "a";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(664, 195);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 16);
            this.label6.TabIndex = 9;
            this.label6.Text = "y2=aX-b";
            // 
            // numericUpDown3
            // 
            this.numericUpDown3.DecimalPlaces = 4;
            this.numericUpDown3.Increment = new decimal(new int[] {
            2,
            0,
            0,
            131072});
            this.numericUpDown3.Location = new System.Drawing.Point(689, 254);
            this.numericUpDown3.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown3.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numericUpDown3.Name = "numericUpDown3";
            this.numericUpDown3.Size = new System.Drawing.Size(120, 22);
            this.numericUpDown3.TabIndex = 8;
            this.numericUpDown3.ValueChanged += new System.EventHandler(this.numericUpDown3_ValueChanged);
            // 
            // numericUpDown4
            // 
            this.numericUpDown4.DecimalPlaces = 4;
            this.numericUpDown4.Increment = new decimal(new int[] {
            2,
            0,
            0,
            131072});
            this.numericUpDown4.Location = new System.Drawing.Point(689, 226);
            this.numericUpDown4.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown4.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numericUpDown4.Name = "numericUpDown4";
            this.numericUpDown4.Size = new System.Drawing.Size(120, 22);
            this.numericUpDown4.TabIndex = 7;
            this.numericUpDown4.ValueChanged += new System.EventHandler(this.numericUpDown4_ValueChanged);
            // 
            // GraficoForm
            // 
            this.ClientSize = new System.Drawing.Size(868, 489);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.numericUpDown3);
            this.Controls.Add(this.numericUpDown4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDown2);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.chart1);
            this.Name = "GraficoForm";
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown4)).EndInit();
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
        // Ottieni i valori di a e b dai controlli NumericUpDown
        double a = (double)numericUpDown1.Value;
        double b = (double)numericUpDown2.Value;

        // Crea la serie per la retta se non esiste già
        Series lineSeriesTAN = chart1.Series.FindByName("Retta");
        if (lineSeriesTAN == null)
        {
            lineSeriesTAN = new Series("Retta")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Red,
                BorderWidth = 2
            };
            chart1.Series.Add(lineSeriesTAN);
        }

        // Pulisci i punti precedenti della serie
        lineSeriesTAN.Points.Clear();

        // Definisci l'intervallo di x da tracciare
        double xStart = 25; // o il valore minimo della tua scala
        double xEnd = 400;  // o il valore massimo della tua scala

        // Calcola e aggiungi i punti alla serie
        for (double x = xStart; x <= xEnd; x += 1)
        {
            double X = Math.Log10(x);
            double y = a * X + b;
            lineSeriesTAN.Points.AddXY(x, y);
        }

        // Aggiorna il grafico
        chart1.Invalidate();
    }
    private void DrawLine2()
    {
        // Ottieni i valori di a e b dai controlli NumericUpDown
        double c = (double)numericUpDown4.Value;
        double d = (double)numericUpDown3.Value;

        // Crea la serie per la retta se non esiste già
        Series lineSeriesTAN2 = chart1.Series.FindByName("Retta2");
        if (lineSeriesTAN2 == null)
        {
            lineSeriesTAN2 = new Series("Retta2")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Red,
                BorderWidth = 2
            };
            chart1.Series.Add(lineSeriesTAN2);
        }

        // Pulisci i punti precedenti della serie
        lineSeriesTAN2.Points.Clear();

        // Definisci l'intervallo di x da tracciare
        double xStart = 25; // o il valore minimo della tua scala
        double xEnd = 1000;  // o il valore massimo della tua scala

        // Calcola e aggiungi i punti alla serie
        for (double t = xStart; t <= xEnd; t += 1)
        {
            double T = Math.Log10(t);
            double s = c * T - d;
            lineSeriesTAN2.Points.AddXY(t, s);
        }

        // Aggiorna il grafico
        chart1.Invalidate();
    }

    private (double maxCurvature, double xMaxCurvature) MaxCurvatura(double[] xSegmento, double lastX, CubicSpline splineY)
    {
        //--------------------------------------------------------------------------
        //--------------------------------------------------------------------------
        // 3 DISEGNO LA TANGENTE, L'ORIZZONTALE E LA BISETTRICE NEL PUNTO DI MAX CURVATURA
        // 3.1 trovo il punto di max curvatura
        //--------------------------------------------------------------------------
        //--------------------------------------------------------------------------

        double maxCurvature = 0;
        double xMaxCurvature = 0;


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

    }

    private void Bisettrice()
    {

    }

    private void TangenteMaxCurvatura()
    {

    }

    private void TangenteCc()
    {

    }

    private void TangenteCr()
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
}