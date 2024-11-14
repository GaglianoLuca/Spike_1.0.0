using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel;


using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using TextBox = System.Windows.Forms.TextBox;

namespace _003_APP_EDOMETRICA
{
   
    public partial class Form1 : Form
    {
        private FlowLayoutPanel flowLayoutPanel;

        List<Panel> listPanel = new List<Panel>();
        
        public Form1()
        {
            InitializeComponent();
            InitializeFlowLayoutPanel(); 
        }
        DataSet result;
        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------
        private void InitializeFlowLayoutPanel()
        {
            flowLayoutPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, 
                Width = 600, 
                AutoScroll = true 
            };
            this.Controls.Add(flowLayoutPanel); 
        }
        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            BindDati();
        }
        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------
        private void BindDati()
        {
            var dati = GetDatiInizialis();
            var combobox = (DataGridViewComboBoxColumn)dataGridView2.Columns["UM"];
            combobox.DisplayMember = "Name";
            combobox.ValueMember = "ID";
            combobox.DataSource = GetMUs();
            dataGridView2.DataSource = dati;
        }
        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------

        private List<DatiIniziali> GetDatiInizialis()
        {

            return new List<DatiIniziali>
                {
                     new DatiIniziali{Dati="Profondità",UM=4,Valori=10.2},
                     new DatiIniziali{Dati="Sezione provino",UM=2, Valori=39.04},
                     new DatiIniziali{Dati="Altezza iniziale",UM=2, Valori=20},
                     new DatiIniziali{Dati="Altezza finale",UM=2, Valori=17.04},
                     new DatiIniziali{Dati="Peso Tara 1",UM=6, Valori=479},
                     new DatiIniziali{Dati="Peso Tara 1 + peso umido iniziale",UM=6, Valori=613.1},
                     new DatiIniziali{Dati="Peso Tara 2",UM=6, Valori=32.047},
                     new DatiIniziali{Dati="Peso Tara 2 + peso umido finale",UM=6, Valori=155.1},
                     new DatiIniziali{Dati="Peso Tara 2 + peso umido secco",UM=6, Valori=128.8},
                     new DatiIniziali{Dati="Peso specifico grani",UM=7, Valori=2.709},
                };

        }
        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------
        private List<MU> GetMUs()

        {
            return new List<MU>
                        {   new MU{ID=1, Name="mm"},
                            new MU{ID=2, Name="cm"},
                            new MU{ID=3, Name="dm"},
                            new MU{ID=4, Name="m"},
                            new MU{ID=5, Name="s"},
                            new MU{ID=6, Name="g"},
                            new MU{ID=7, Name="g/cm^3"},
                            new MU{ID=8, Name="kPa"},
                            new MU{ID=9, Name="cm^2"},
                            new MU{ID=10, Name="numero puro"},

                            };
        }
        //----------------------------------------------------------------------------------------------
        // creo in modo dinamico le tabelle per ogni passo della prova eseguita, ricevo in input passi e step
        //----------------------------------------------------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int numPassi) && numPassi > 0)
            {
                if (int.TryParse(textBox2.Text, out int numLetture) && numLetture > 0)
                {
                    flowLayoutPanel.Controls.Clear();

                    for (int i = 0; i < numPassi; i++)
                    {
                        // Creazione di un TableLayoutPanel per ogni passo
                        TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
                        {
                            AutoSize = true,
                            AutoSizeMode = AutoSizeMode.GrowAndShrink,
                            ColumnCount = 1,
                            RowCount = 3, 
                            Margin = new Padding(0, 0, 0, 10) // Margine per distanziare i passi
                        };

                        // Creazione dell'etichetta
                        Label passoLabel = new Label
                        {
                            Text = $"Passo {i + 1:00}",
                            AutoSize = true,
                            Font = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Bold),
                            Dock = DockStyle.Top 
                        };
                        tableLayoutPanel.Controls.Add(passoLabel, 0, 0);

                        // Creazione della DataGridView
                        DataGridView dataGridView = new DataGridView
                        {
                            Name = $"dataGridView{i + 1}",
                            Width = 500,
                            Height = 100 + (numLetture * 22),
                            AllowUserToAddRows = false,
                            Dock = DockStyle.Fill 
                        };

                        // Aggiunta delle colonne
                        dataGridView.Columns.Add("Lettura", "Lettura");
                        dataGridView.Columns.Add("Tempo", "Tempo");
                        dataGridView.Columns.Add("Cedimento", "Cedimento");
                        dataGridView.Columns.Add("DH_H0", "DH/H0");
                        dataGridView.Columns.Add("De", "De");
                        dataGridView.Columns.Add("e", "e");
                        dataGridView.Columns.Add("log_t", "log(t)");
                        dataGridView.Columns.Add("radq_t", "radq(t)");
                        dataGridView.Columns.Add("Calfa", "Calfa");
                        dataGridView.Columns.Add("CalfaEps", "CalfaEps");

                        for (int j = 0; j < numLetture; j++)
                        {
                            dataGridView.Rows.Add();
                            dataGridView.Rows[j].Cells[0].Value = j + 1;
                        }

                        tableLayoutPanel.Controls.Add(dataGridView, 0, 1);

                        // Creazione di un pannello per il pulsante e la TextBox
                        TableLayoutPanel controlPanel = new TableLayoutPanel
                        {
                            AutoSize = true,
                            ColumnCount = 3, 
                            Dock = DockStyle.Top
                        };

                        // Creazione del pulsante per incollare i dati
                        System.Windows.Forms.Button pasteButton = new System.Windows.Forms.Button
                        {
                            Text = "Importa Dati",
                            AutoSize = true,
                            Margin = new Padding(0, 5, 0, 0) 
                        };

                        // incollo i dati nella tabella
                        pasteButton.Click += (s, ev) => PasteData(dataGridView);

                        // Creazione della TextBox per inserire un numero
                        System.Windows.Forms.TextBox numberTextBox = new System.Windows.Forms.TextBox
                        {
                            Width = 50,
                            Margin = new Padding(5, 5, 0, 0) // Margine per distanziare dal pulsante
                        };

                        // Creazione della Label "Pressione Assiale"
                        Label pressioneLabel = new Label
                        {
                            Text = "Pressione Assiale",
                            AutoSize = true,
                            Margin = new Padding(5, 5, 0, 0) // Margine per distanziare dalla TextBox
                        };
                        
                        controlPanel.Controls.Add(pasteButton, 0, 0);
                        controlPanel.Controls.Add(numberTextBox, 1, 0);
                        controlPanel.Controls.Add(pressioneLabel, 2, 0);
                        tableLayoutPanel.Controls.Add(controlPanel, 0, 2);
                        flowLayoutPanel.Controls.Add(tableLayoutPanel);
                    }
                }
                else
                {
                    MessageBox.Show("Inserisci un numero valido di Letture.");
                }
            }
            else
            {
                MessageBox.Show("Inserisci un numero valido di Passi.");
            }
        }

        //----------------------------------------------------------------------------------------------
        // metodo per incollare nelle tabelle
        //----------------------------------------------------------------------------------------------
        private void PasteData(DataGridView activeGrid)
        {
            int rowIndex = activeGrid.CurrentCell.RowIndex;
            int columnIndex = activeGrid.CurrentCell.ColumnIndex; 
            string s = Clipboard.GetText();
            string[] lines = s.Replace("\n", "").Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string[] fields;

            foreach (string l in lines)
            {
                fields = l.Split('\t');
                if (rowIndex < activeGrid.RowCount)
                {
                    for (int col = 0; col < fields.Length; col++)
                    {
                        int targetCol = columnIndex + col;
                        if (targetCol < activeGrid.ColumnCount) 
                        {
                            activeGrid.Rows[rowIndex].Cells[targetCol].Value = fields[col];
                        }
                        else
                        {
                            break; 
                        }
                    }
                    rowIndex++; 
                }
            }

        }
        //----------------------------------------------------------------------------------------------
        // metodo per importare i file excel
        //----------------------------------------------------------------------------------------------
            
        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel|*.xls;*.xlsx*", ValidateNames = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    FileStream fs = File.Open(ofd.FileName, FileMode.Open, FileAccess.Read);
                    IExcelDataReader reader = ExcelReaderFactory.CreateBinaryReader(fs);
                    reader.IsFirstRowAsColumnNames = true;
                    result = reader.AsDataSet();
                    comboBox1.Items.Clear();
                    foreach (DataTable dt in result.Tables)
                    comboBox1.Items.Add(dt.TableName);
                    reader.Close();
                    textBox3.Text = ofd.SafeFileName;

                }
            }
        }

        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.DataSource = result.Tables[comboBox1.SelectedIndex];
        }
        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------
        private void button3_Click(object sender, EventArgs e)
        {
            CalcolaCedimento(sender, e);
            PopolaPrimaColonnaTabella();
            
        }
        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        //----------------------------------------------------------------------------------------------
        // calcolo i parametri derivati
        //----------------------------------------------------------------------------------------------

        private void button5_Click(object sender, EventArgs e)

        {
            
            double TaraUmido1 = Convert.ToDouble(dataGridView2.Rows[5].Cells[2].Value);
            double Tara1 = Convert.ToDouble(dataGridView2.Rows[4].Cells[2].Value);
            double Sezione = Convert.ToDouble(dataGridView2.Rows[1].Cells[2].Value);
            double Altezza1 = Convert.ToDouble(dataGridView2.Rows[2].Cells[2].Value);
            double Altezza2 = Convert.ToDouble(dataGridView2.Rows[3].Cells[2].Value);
            double TaraUmido2 = Convert.ToDouble(dataGridView2.Rows[7].Cells[2].Value);
            double Tara2 = Convert.ToDouble(dataGridView2.Rows[6].Cells[2].Value);
            double TaraSecco2 = Convert.ToDouble(dataGridView2.Rows[8].Cells[2].Value);
            double PesoSpecGrani = Convert.ToDouble(dataGridView2.Rows[9].Cells[2].Value);
            double DensUm1 = (TaraUmido1 - Tara1) / (Sezione * Altezza1 / 10);
            double DensUm2 = (TaraUmido2 - Tara2) / (Sezione * Altezza2 / 10);
            double DensSec1 = (TaraSecco2 - Tara2) / (Sezione / 10 * Altezza1);
            double DensSec2 = (TaraSecco2 - Tara2) / (Sezione / 10 * Altezza2);
            double AcquaIniz = ((TaraUmido1 - Tara1) - (TaraSecco2 - Tara2)) / (TaraSecco2 - Tara2);
            double AcquaFin = ((TaraUmido2 - Tara2) - (TaraSecco2 - Tara2)) / (TaraSecco2 - Tara2);
            double SatIniz = (PesoSpecGrani * DensSec1 * AcquaIniz) / (PesoSpecGrani - DensSec1);
            double SatFin = (PesoSpecGrani * DensSec2 * AcquaFin) / (PesoSpecGrani - DensSec2);
            double VuotiIniz = PesoSpecGrani / DensSec1 - 1;
            double VuotiFin = PesoSpecGrani / DensSec2 - 1;
            int cU = Convert.ToInt32(dataGridView2.Rows[1].Cells[1].Value);
            
            dataGridView3.Columns.Clear();
            dataGridView3.Rows.Clear();
            dataGridView3.Columns.Add("Nome", "Nome");
            dataGridView3.Columns.Add("Valore", "Valore");
            dataGridView3.Rows.Add("Densità Umida 1", DensUm1.ToString("F2"));
            dataGridView3.Rows.Add("Densità Umida 2", DensUm2.ToString("F2"));
            dataGridView3.Rows.Add("Densità Secca 1", DensSec1.ToString("F2"));
            dataGridView3.Rows.Add("Densità Secca 2", DensSec2.ToString("F2"));
            dataGridView3.Rows.Add("Acqua Iniziale", AcquaIniz.ToString("F2"));
            dataGridView3.Rows.Add("Acqua Finale", AcquaFin.ToString("F2"));
            dataGridView3.Rows.Add("Saturazione Iniziale", SatIniz.ToString("F2"));
            dataGridView3.Rows.Add("Saturazione Finale", SatFin.ToString("F2"));
            dataGridView3.Rows.Add("Vuoti Iniziali", VuotiIniz.ToString("F2"));
            dataGridView3.Rows.Add("Vuoti Finali", VuotiFin.ToString("F2"));


        }
        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------

        private void button6_Click(object sender, EventArgs e)
        {
            PasteData(dataGridView2);
        }

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        //----------------------------------------------------------------------------------------------
        // metodo per calcolare i parametri derivati nelle fasi della prova edometrica
        //----------------------------------------------------------------------------------------------

        public void CalcolaCedimento(object sender, EventArgs e)
        {
            foreach (Control control in flowLayoutPanel.Controls)
            {
                if (control is TableLayoutPanel tableLayoutPanel)
                {
                    foreach (Control ctrl in tableLayoutPanel.Controls)
                    {
                        if (ctrl is DataGridView dataGridView)
                        {
                            double altezzaIniziale = Convert.ToDouble(dataGridView2.Rows[2].Cells[2].Value);

                            for (int i = 0; i < dataGridView.Rows.Count; i++)
                            {
                                var cedimentoCell = dataGridView.Rows[i].Cells[2].Value;
                                
                                if (cedimentoCell == null || string.IsNullOrWhiteSpace(cedimentoCell.ToString()))
                                {
                                    break;
                                }
                             
                                double cedimento = Convert.ToDouble(dataGridView.Rows[i].Cells[2].Value);

                                // Calcolo del rapporto
                                if (altezzaIniziale != 0)
                                {

                                    double risultato = cedimento / altezzaIniziale;
                                    double indiceVuoti = Convert.ToDouble(dataGridView3.Rows[8].Cells[1].Value);
                                    dataGridView.Rows[i].Cells[3].Value = risultato;
                                    double risultato2 = risultato * (1 + indiceVuoti);
                                    dataGridView.Rows[i].Cells[4].Value = risultato2;
                                    string rapportoCedimento = dataGridView.Rows[i].Cells[3].Value.ToString();
                                    double rapportoCedimento2 = Convert.ToDouble(rapportoCedimento);
                                    double risultato3 = indiceVuoti - (1 + indiceVuoti) * rapportoCedimento2;
                                    dataGridView.Rows[i].Cells[5].Value = risultato3;
                                    double secondi = Convert.ToDouble(dataGridView.Rows[i].Cells[1].Value);
                                    double risultato4 = Math.Log(secondi);
                                    dataGridView.Rows[i].Cells[6].Value = risultato4;
                                    double risultato5 = Math.Sqrt(secondi);
                                    dataGridView.Rows[i].Cells[7].Value = risultato5;
                                }
                                else
                                {
                                    dataGridView.Rows[i].Cells[3].Value = "N/A"; 
                                }
                                }
                            }
                        }
                    }
                }
            }

        //------------------------------------------------------------------------------------------
        // metodo per ottenere le pressioni assiali per ogni fase della prova edometrica
        //------------------------------------------------------------------------------------------
        private double[] EstraiValoriNumberTextBox()
        {
           
            int count = 0;

            // Conta quanti numberTextBox ci sono
            foreach (Control control in flowLayoutPanel.Controls)
            {
                if (control is TableLayoutPanel tableLayoutPanel)
                {
                    foreach (Control ctrl in tableLayoutPanel.Controls)
                    {
                        if (ctrl is TableLayoutPanel innerPanel)
                        {
                            foreach (Control innerCtrl in innerPanel.Controls)
                            {
                                if (innerCtrl is TextBox numberTextBox)
                                {
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            // Crea un array per i valori
            double[] valori = new double[count];
            int index = 0;

            // prendi i valori della pressione assiale
            foreach (Control control in flowLayoutPanel.Controls)
            {
                if (control is TableLayoutPanel tableLayoutPanel)
                {
                    foreach (Control ctrl in tableLayoutPanel.Controls)
                    {
                        if (ctrl is TableLayoutPanel innerPanel)
                        {
                            foreach (Control innerCtrl in innerPanel.Controls)
                            {
                                if (innerCtrl is TextBox numberTextBox)
                                {
                                 
                                    if (double.TryParse(numberTextBox.Text, out double valore))
                                    {
                                        valori[index] = valore;
                                        index++;
                                    }
                                    else
                                    {
                                       
                                        valori[index] = 0; 
                                        index++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return valori; 
        }

        //------------------------------------------------------------------------------------------
        // metodo per prendere i valori deltaE
        //------------------------------------------------------------------------------------------

        private double[] EstraiUltimiValoriDeltaE()
        {
            List<double> valoriDeltaE = new List<double>();

          
            foreach (Control control in flowLayoutPanel.Controls)
            {
                if (control is TableLayoutPanel tableLayoutPanel)
                {
                    foreach (Control ctrl in tableLayoutPanel.Controls)
                    {
                        if (ctrl is DataGridView dataGridView)
                        {
                       
                            if (dataGridView.Rows.Count > 0)
                            {

                                for (int rowIndex = dataGridView.Rows.Count - 1; rowIndex >= 0; rowIndex--)
                                {
                                    var cellValue = dataGridView.Rows[rowIndex].Cells[4].Value;

                                    if (cellValue != null && double.TryParse(cellValue.ToString(), out double deltaE))
                                    {
                                        valoriDeltaE.Add(deltaE);
                                        break; 
                                    }
                                }
                            }
                        }
                    }
                }
            }

          
            return valoriDeltaE.ToArray();
        }

        //------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------

        private void ApriGrafico()
        {
            int rowCount = dataGridView4.Rows.Count;
            double[,] dati = new double[rowCount, 2];

            for (int i = 0; i < rowCount; i++)
            {
                if (dataGridView4.Rows[i].Cells[0].Value != null && dataGridView4.Rows[i].Cells[1].Value != null)
                {
                    dati[i, 0] = Convert.ToDouble(dataGridView4.Rows[i].Cells[0].Value); // Sigma
                    dati[i, 1] = Convert.ToDouble(dataGridView4.Rows[i].Cells[1].Value); // DeltaE
                }
            }

            // Crea e mostra il nuovo form per il grafico
            GraficoForm graficoForm = new GraficoForm(dati);
            graficoForm.ShowDialog(); // Usa ShowDialog() per aprire il form come modale
        }

        //------------------------------------------------------------------------------------------
        // metodo per popolare la tabella con valori di Sigma e deltaE
        //------------------------------------------------------------------------------------------


        private void PopolaPrimaColonnaTabella()
        {
            double[] valori = EstraiValoriNumberTextBox();
            double[] valori2 = EstraiUltimiValoriDeltaE();
            Console.WriteLine("Valori Sigma: " + string.Join(", ", valori));
            Console.WriteLine("Valori DeltaE: " + string.Join(", ", valori2));

     
            if (dataGridView4.Columns.Count == 0)
            {
                dataGridView4.Columns.Add("Colonna1", "Sigma");
                dataGridView4.Columns.Add("Colonna2", "DeltaE");
            }

            dataGridView4.Rows.Clear();

            int rowCount = Math.Min(valori.Length, valori2.Length);

            for (int i = 0; i < rowCount; i++)
            {
                dataGridView4.Rows.Add(valori[i], valori2[i]);
            }
        }

        //------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------

        private void button7_Click(object sender, EventArgs e)
        {
            ApriGrafico();
        }
        //------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------


        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
