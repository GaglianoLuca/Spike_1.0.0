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
    private System.Windows.Forms.DataVisualization.Charting.Chart chart1;

    public GraficoForm(double[,] data)
    {
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

        // Creazione della serie per i punti originali
        var dataSeries = new System.Windows.Forms.DataVisualization.Charting.Series
        {
            Name = "dataSeries",
            ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Spline, // Mostra solo i punti originali
            MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle, // Imposta il marcatore
            MarkerSize = 8, // Dimensione del marcatore
            MarkerColor = Color.Red // Colore dei punti
            
        };
        List<double> xList = new List<double>();
        List<double> yList = new List<double>();

        // Aggiungi i punti originali alla serie
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

        // Aggiungi la serie di punti originali al grafico
        chart1.Series.Add(dataSeries);
        chart1.ChartAreas[0].AxisY.IsReversed = true;
        chart1.ChartAreas[0].AxisY.IsReversed = true;
        chart1.ChartAreas[0].AxisX.IsLogarithmic = true;
        chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = true; // Linee della griglia principale
        chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = true;
        chart1.ChartAreas[0].AxisX.MinorGrid.LineDashStyle = ChartDashStyle.Dot;
        chart1.ChartAreas[0].AxisX.MinorGrid.LineColor = Color.LightGray;
        // provo a fare ciclo dinamico
        // Definisci i punti della tua spline
        List<double[]> segmentiX = new List<double[]>();
        List<double[]> segmentiY = new List<double[]>();

        List<double> tempX = new List<double>();
        List<double> tempY = new List<double>();
        int contatore=0;
        bool crescente = true; // Indica se stiamo attualmente in un segmento crescente
        int numero = 0;
        
        for (int i = 0; i < data.GetLength(0); i++)
        {
            if (data[i, 0] > 0 && data[i, 1] > 0) // Considera solo i valori positivi
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

        // Aggiungi l'ultimo segmento
        if (tempX.Count > 0)
        {
            segmentiX.Add(tempX.ToArray());
            segmentiY.Add(tempY.ToArray());
        }
        int contatoreTangente = 0;
        // Supponiamo che segmentiX e segmentiY contengano le sottoserie crescenti o decrescenti
        for (int k = 0; k < segmentiX.Count; k++)
        {
            double[] xSegmento = segmentiX[k];
            double[] ySegmento = segmentiY[k];
            if (k % 2 == 0)
            { }
            else
            {
                Array.Reverse(xSegmento);
                Array.Reverse(ySegmento);
            }
            // Crea una spline y(x) per la sottoserie corrente
           // var splineY = CubicSpline.InterpolatePchip(xSegmento, ySegmento);
            var splineY = CubicSpline.InterpolateNatural(xSegmento, ySegmento);
                
            double maxCurvature = 0;
            double xMaxCurvature = 0;
            double lastX = xSegmento[xSegmento.Length - 1];

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
                }
            }
            if (contatoreTangente == 0)
            {
                //-------------------------------------------------------------------------
                // Ottieni i valori della funzione e della derivata nel punto di massima curvatura
                double yAtMaxCurvature = splineY.Interpolate(xMaxCurvature);
                double slopeAtMaxCurvature = splineY.Differentiate(xMaxCurvature);
                double adjustedSlope = slopeAtMaxCurvature * xMaxCurvature * Math.Log(10);
                Console.WriteLine("Pendenza (slope) della tangente al punto di massima curvatura: " + slopeAtMaxCurvature);

                // Definisci la funzione della tangente al punto di massima curvatura
                Func<double, double> tangentLine = x => yAtMaxCurvature + (adjustedSlope * ((Math.Log10(x) - Math.Log10(xMaxCurvature))));

                // Intervallo per tracciare la tangente
                double tangentStart = xMaxCurvature - 10;
                double tangentEnd = xMaxCurvature + 200;

                // Creazione della serie per la tangente
                Series tangentSeries = new Series("Tangente al max. curvatura")
                {
                    Name = $"tangente_{k}",
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
                //-------------------------------------------------------------------------

                // Aggiungi i punti per creare la retta orizzontale
                var horizontalLine = new Series
                {
                    ChartType = SeriesChartType.Line,
                    Color = Color.Red, // Cambia il colore come preferisci
                    BorderWidth = 1 // Spessore della linea
                };
                horizontalLine.Points.AddXY(xMaxCurvature, yAtMaxCurvature);
                horizontalLine.Points.AddXY(xMaxCurvature * 50, yAtMaxCurvature); // Estendi la retta a destra

                // Aggiungi la serie al grafico
                chart1.Series.Add(horizontalLine);


                //-------------------------------------------------------------------------
                double bisectorSlope =  Math.Tan(0.5 * Math.Atan(adjustedSlope));
                


                // Crea una nuova serie per la bisettrice
                var bisectorLine = new Series
                {
                    ChartType = SeriesChartType.Line,
                    Color = Color.Green, // Colore della bisettrice
                    BorderWidth = 1 // Spessore della linea
                };

                // Punto di partenza della bisettrice
                double xStart = xMaxCurvature;
                double yStart = yAtMaxCurvature;

                // Punto finale della bisettrice (spostato a destra)
                double xEnd = xMaxCurvature * 50; // Puoi modificare questo per estendere la linea
                double yEnd = yStart + bisectorSlope * (Math.Log10(xEnd) - Math.Log10(xStart));

                // Aggiungi i punti alla serie della bisettrice
                bisectorLine.Points.AddXY(xStart, yStart);
                bisectorLine.Points.AddXY(xEnd, yEnd);

                // Aggiungi la serie al grafico
                chart1.Series.Add(bisectorLine);
              



                Console.WriteLine($"Sottoserie {k}: Punto di massima curvatura a x = {xMaxCurvature}, curvatura = {maxCurvature}");

                //-------------------------------------------------------------------------
                // Trova l'indice del punto di massima curvatura
                int maxCurvatureIndex = Array.FindIndex(xSegmento, x => x >= xMaxCurvature);
                if (maxCurvatureIndex < 0 || maxCurvatureIndex >= xSegmento.Length - 1)
                {
                    Console.WriteLine("Errore: punto di massima curvatura non valido o non c'è un punto a destra nella sottoserie.");
                    return;
                }

                // Punto di partenza per la tangente: il primo punto successivo al maxCurvature
                double tangentStartX = xSegmento[maxCurvatureIndex + 1];
                double tangentStartY = splineY.Interpolate(tangentStartX);

                // Ottieni la derivata (pendenza) al punto di max curvatura
                double slopeAtMaxCurvature2 = splineY.Differentiate(xMaxCurvature);
                double adjustedSlope2 = slopeAtMaxCurvature * tangentStartX * Math.Log(10);

                // Definisci la funzione della tangente al punto di partenza
                Func<double, double> tangentLine2 = x => tangentStartY + adjustedSlope2 * (Math.Log10(x) - Math.Log10(tangentStartX));

                // Intervallo per tracciare la tangente a partire da tangentStartX
                double tangentEnd2 =25;

                // Creazione della serie per la tangente
                Series tangentSeries2 = new Series("Tangente a partire dal punto successivo al max. curvatura")
                {
                    Name = $"ultima",
                    ChartType = SeriesChartType.Line,
                    Color = Color.Red,
                    BorderWidth = 1
                };

                // Aggiungi i punti della tangente alla serie
                for (double x = tangentStartX; x >= tangentEnd2; x -= 0.01)
                {
                    double y = tangentLine2(x);
                    tangentSeries2.Points.AddXY(x, y);
                }

                // Aggiungi la serie della tangente al grafico
                chart1.Series.Add(tangentSeries2);
                chart1.Invalidate();

                // Trova l'intersezione tra l'ultima tangente e la bisettrice
                double tolerance = 0.001; // tolleranza per determinare il punto di intersezione
                double tangencyX = 0;
                double tangencyY = 0;
                bool foundTangencyPoint = false;

                for (double x = tangentEnd2; x <= tangentStartX; x += 0.01)
                {
                    // Valori di y per la tangente e la bisettrice
                    double yTangent = tangentLine2(x);
                    double yBisector = yStart + bisectorSlope * (Math.Log10(x) - Math.Log10(xStart));

                    // Calcola la differenza tra la tangente e la bisettrice
                    if (Math.Abs(yTangent - yBisector) < tolerance)
                    {
                        // Se la differenza è minore della tolleranza, abbiamo trovato l'intersezione
                        tangencyX = x;
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
                if (prevMaxCurvatureIndex < 0 || prevMaxCurvatureIndex >= xSegmento.Length - 1)
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


            }

        }
    }
        /* double[] x = xList.ToArray();
        double[] y = yList.ToArray();

        for (int i = 0; i < data.GetLength(0); i++)
        {
            if (data[i, 0] > 0 && data[i, 1] > 0) // Considera solo i valori positivi come nel tuo codice
            {
                x[i] = data[i, 0]; // Popola x con i valori di sigma
                y[i] = data[i, 1]; // Popola y con i valori di deltaE

            }
        }

        // Crea una spline y(x)
        var splineY = CubicSpline.InterpolateNatural(x, y);

        double maxCurvature = 0;
        double xMaxCurvature = 0;
        double lastX = x[x.Length - 1];


        // Scansiona x per trovare il punto di massima curvatura
        for (double xi = x[0]; xi <= lastX; xi += 0.01)
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

            }

        }

        var splineSeries = new Series
        {
            Name = "splineSeries",
            ChartType = SeriesChartType.Line,
            Color = Color.Blue,
            BorderWidth = 2
        };
        // Aggiungi punti interpolati della spline alla serie
        for (double xi = x[0]; xi <= x[x.Length - 1]; xi += 0.01)
        {
            double yi = splineY.Interpolate(xi);
            splineSeries.Points.AddXY(xi, yi);
        }

        // Aggiungi la serie della spline al grafico
        chart1.Series.Add(splineSeries);

        Console.WriteLine($"Punto di massima curvatura a x = {xMaxCurvature}, curvatura = {maxCurvature}");
        }

*/
    
    private void InitializeComponent()
    {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
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
            // GraficoForm
            // 
            this.ClientSize = new System.Drawing.Size(799, 489);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.chart1);
            this.Name = "GraficoForm";
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);

    }

    private void chart1_Click(object sender, EventArgs e)
    {

    }

    private void button1_Click(object sender, EventArgs e)
    {
        chart1.Printing.PrintPreview();
    }
}
