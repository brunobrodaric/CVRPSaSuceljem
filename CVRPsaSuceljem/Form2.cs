using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CVRP1;

namespace CVRPsaSuceljem
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            textBox1.Text = "1";
            textBox2.Text = "5";
            textBox3.Text = "5";
            textBox4.Text = "25";
            textBox5.Text = "5";
            textBox6.Text = "0.1";
            textBox7.Text = "100";
        }

        OpenFileDialog openFileDialog = new OpenFileDialog();

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "VRP|*.vrp";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox8.Text = openFileDialog.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
            
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            long kolikoIteracija = Convert.ToInt32(textBox7.Text);
            string fileName = textBox8.Text;
            double alfa = Convert.ToDouble(textBox1.Text);
            double beta = Convert.ToDouble(textBox2.Text);
            double gama = Convert.ToDouble(textBox3.Text);
            double lambda = Convert.ToDouble(textBox5.Text);
            int brojMrava = Convert.ToInt32(textBox4.Text);
            double parametarEvaporacije = Convert.ToDouble(textBox6.Text.Split('.').ElementAt(1)) / 10;
            // ucitavanje testnih podataka
            TestniPodaci podaci = new TestniPodaci(fileName);

            int brojVrhova = podaci.brojVrhova;
            Vrh[] vrhovi = new Vrh[brojVrhova + 1];
            double kapacitetVozila = podaci.kapacitetVozila;
            for (int i = 0; i <= brojVrhova; ++i)
            {
                vrhovi[i] = podaci.vrhovi[i];
            }

            double[,] feromoni = new double[brojVrhova + 1, brojVrhova + 1];
            double[,] eta = new double[brojVrhova + 1, brojVrhova + 1];

            //dodani parametri mi i ka, objasnjeno u dokumentu "improvedVRP"
            double[,] mi = new double[brojVrhova + 1, brojVrhova + 1];
            double[,] ka = new double[brojVrhova + 1, brojVrhova + 1];
            double miMin = 0.2;
            double miMax = 0.9;


            /* svakom vrhu pridruzujemo listu 15 najblizih vrhova i spremamo te podatke u rjecnik
             * najbliziVrhovi.
             * za sad iskoristeno samo kod trazenja mogucih vrhova, vidi popunjavanje liste moguciVrhovi
             */
            Dictionary<Vrh, List<Vrh>> najbliziVrhovi = new Dictionary<Vrh, List<Vrh>>();
            foreach (var vrh in vrhovi)
            {
                najbliziVrhovi[vrh] = vrh.vratiNajblize(vrhovi.ToList(), 15);
            }

            for (int i = 0; i <= brojVrhova; i++)
            {
                for (int j = 0; j <= brojVrhova; j++)
                {
                    feromoni[i, j] = 0.001;
                    feromoni[j, i] = 0.001;
                    eta[i, j] = 1 / (vrhovi[i].udaljenost(vrhovi[j], 0));
                    mi[i, j] = (vrhovi[i].udaljenost(vrhovi[1]) + vrhovi[1].udaljenost(vrhovi[j]) - vrhovi[i].udaljenost(vrhovi[j])) / 100;
                    if (mi[i, j] < miMin) mi[i, j] = miMin;
                    if (mi[i, j] > miMax) mi[i, j] = miMax;
                    ka[i, j] = (vrhovi[i].potraznja + vrhovi[j].potraznja) / kapacitetVozila;
                }
            }

            Random random = new Random();

            int brojIteracije = 1;
            int boljeRjesenjePrijeKoliko = 0;
            Obilazak globalniNajboljiPut = new Obilazak();
            double globalnaMinDuljina = 1000000;

            // svaki prolazak kroz sljedecu petlju predstavlja jednu iteraciju algoritma
            while (brojIteracije - 1 < kolikoIteracija || kolikoIteracija < 0)
            {
                double[] ukupniPut = new double[brojMrava];
                Obilazak[] prijedeniPut = new Obilazak[brojMrava];

                // svaki prolazak kroz sljedecu petlju izvrsava posao jednog mrava
                for (int mrav = 0; mrav < brojMrava; ++mrav)
                {
                    List<Vrh> neposjeceniVrhovi = new List<Vrh>();
                    prijedeniPut[mrav] = new Obilazak();
                    int brojPotpunoSlucajnih = 0;

                    foreach (var vrh in vrhovi)
                    {
                        if (vrh.oznaka != 0) neposjeceniVrhovi.Add(vrh);
                    }
                    neposjeceniVrhovi.Remove(vrhovi[1]);

                    prijedeniPut[mrav].dodajVrh(vrhovi[1]);  // vrhovi[1] = skladiste, odatle pocinje svaki obilazak

                    // slijedi konstrukcija kompletnog rjesenja... (to svaki mrav radi)
                velikaPetlja: while (neposjeceniVrhovi.Count() > 0)
                    {
                        int brojPocetnogVrha = random.Next(0, neposjeceniVrhovi.Count());

                        Vrh pocetniVrh = neposjeceniVrhovi[brojPocetnogVrha];
                        Vrh prosliVrh = pocetniVrh;
                        double preostaliKapacitet = kapacitetVozila - prosliVrh.potraznja;
                        prijedeniPut[mrav].dodajVrh(pocetniVrh);
                        neposjeceniVrhovi.Remove(pocetniVrh);

                        while (true)
                        {
                            List<Vrh> moguciVrhovi = new List<Vrh>();

                            foreach (var vrh in neposjeceniVrhovi)
                            {
                                if (preostaliKapacitet - vrh.potraznja >= 0 && vrh.oznaka != prosliVrh.oznaka &&
                                    najbliziVrhovi[prosliVrh].Contains(vrh))
                                    moguciVrhovi.Add(vrh);
                            }

                            if (moguciVrhovi.Count() == 0)
                            {
                                foreach (var vrh in neposjeceniVrhovi)
                                {
                                    if (preostaliKapacitet - vrh.potraznja >= 0 && vrh.oznaka != prosliVrh.oznaka)
                                        moguciVrhovi.Add(vrh);
                                }

                            }


                            double qParametar = random.NextDouble();
                            double q = 0.6;
                            double q2 = 1 / ((brojPotpunoSlucajnih * 5) / brojVrhova + 6);

                            // ako mrav vise ne moze prijeci ni u jedan vrh a da ne dostavi vise nego sto ima, krece opet iz skladista
                            if (moguciVrhovi.Count() == 0)
                            {
                                prijedeniPut[mrav].dodajVrh(vrhovi[1]);
                                goto velikaPetlja;
                            };
                            Vrh sljedeciVrh = null;
                            // imamo dva moguca nacina za izbor sljedeceg vrha, tj. po dvije razlicite formule
                            // (vidi ANT COLONY SYSTEM, npr. u stuzle-99) i slucajno biramo na koji cemo od ta dva nacina
                            // (za to sluze qParametar i q)
                            // UPDATE: dodan treci nacin: potpuno slucajan izbor! (za potrebe toga dodan parametar q2)
                            if (qParametar > q)
                            {
                                double r = random.NextDouble();

                                double[] vjerojatnosti = new double[brojVrhova + 1];
                                double[] rasponOd = new double[brojVrhova + 1];
                                double[] rasponDo = new double[brojVrhova + 1];

                                double suma = 0;

                                foreach (var vrh in moguciVrhovi)
                                {
                                    suma += (Math.Pow(feromoni[prosliVrh.oznaka, vrh.oznaka], alfa) * Math.Pow(eta[prosliVrh.oznaka, vrh.oznaka], beta))
                                         * Math.Pow(mi[prosliVrh.oznaka, vrh.oznaka], gama) * Math.Pow(ka[prosliVrh.oznaka, vrh.oznaka], lambda);
                                }

                                foreach (var vrh in moguciVrhovi)
                                {
                                    vjerojatnosti[vrh.oznaka] =
                                        (Math.Pow(feromoni[prosliVrh.oznaka, vrh.oznaka], alfa) *
                                        Math.Pow(eta[prosliVrh.oznaka, vrh.oznaka], beta)) *
                                        Math.Pow(mi[prosliVrh.oznaka, vrh.oznaka], gama) *
                                        Math.Pow(ka[prosliVrh.oznaka, vrh.oznaka], lambda) / suma;
                                }

                                double rasponDoProsli = 0;

                                foreach (var vrh in moguciVrhovi)
                                {
                                    rasponOd[vrh.oznaka] = rasponDoProsli;
                                    rasponDo[vrh.oznaka] = rasponOd[vrh.oznaka] + vjerojatnosti[vrh.oznaka];
                                    rasponDoProsli = rasponDo[vrh.oznaka];
                                }
                                rasponDo[moguciVrhovi[moguciVrhovi.Count() - 1].oznaka] = 1;

                                double randomDouble = random.NextDouble();

                                foreach (var vrh in moguciVrhovi)
                                {

                                    if (randomDouble >= rasponOd[vrh.oznaka] && randomDouble <= rasponDo[vrh.oznaka])
                                    {
                                        sljedeciVrh = vrh;
                                        break;
                                    }
                                }
                            }
                            else if (qParametar > q2)
                            {
                                double najvecaVrijednost = 0;
                                foreach (var vrh in moguciVrhovi)
                                {
                                    double vrijednostZaOvajVrh =
                                        Math.Pow(feromoni[prosliVrh.oznaka, vrh.oznaka], alfa) *
                                        Math.Pow(eta[prosliVrh.oznaka, vrh.oznaka], beta) *
                                        Math.Pow(mi[prosliVrh.oznaka, vrh.oznaka], gama) *
                                        Math.Pow(ka[prosliVrh.oznaka, vrh.oznaka], lambda);

                                    if (vrijednostZaOvajVrh >= najvecaVrijednost)
                                    {
                                        najvecaVrijednost = vrijednostZaOvajVrh;
                                        sljedeciVrh = vrh;
                                    }
                                }
                            }
                            else
                            {
                                brojPotpunoSlucajnih++;
                                int indeksSljedeceg = random.Next(0, moguciVrhovi.Count());
                                sljedeciVrh = moguciVrhovi[indeksSljedeceg];
                            }
                            preostaliKapacitet -= sljedeciVrh.potraznja;
                            prijedeniPut[mrav].dodajVrh(sljedeciVrh);
                            neposjeceniVrhovi.Remove(sljedeciVrh);
                        }
                    }

                    // mrav je konstruirao neko rjesenje, slijedi dosta agresivno i vremenski skupo lokalno pretrazivanje
                    // stavljeno je da se vrsi samo u svakoj desetoj iteraciji jer bi inace bilo jos puno sporije
                    // ako se zakomentira taj dio koda, ide puno brze, ali i rezultati su losiji
                    // EDIT: dodano da program pamti do sada poznata poboljsanja i onda ne pretrazuje ako smo za neki put vec prije vrsili 
                    // dvaOpt ili triOpt... medutim, cini se da to ne ubrzava program (mozda nesto nije dobro napravljeno?)... ali ga ni ne
                    // usporava... znaci, to sad nije toliko bitno, prouciti kasnije.

                 //   if (brojIteracije % 20 == 10)
                    if (mrav == (int)( ( (double)brojIteracije / kolikoIteracija) * brojMrava))
                    {
                        Obilazak ulazniObilazak = new Obilazak();
                        foreach (var vrh in prijedeniPut[mrav].put)
                        {
                            ulazniObilazak.dodajVrh(vrh);
                        }
                        int p = 1;
                        while (p == 1)
                        {
                            p = 0;
                            Obilazak mozdaBoljiPut1 = prijedeniPut[mrav].dvaOpt(kapacitetVozila, prijedeniPut[mrav].duljinaObilaska(), najbliziVrhovi);

                            if (mozdaBoljiPut1 != null)
                            {
                                p = 1;
                                prijedeniPut[mrav] = mozdaBoljiPut1;
                            }
                        }
                    }

                    // stutzle-99, 7. str. skroz dolje, local pheromone update: (znaci, put koji mrav izabere gubi dio svojih feromona,
                    // na taj nacin poticemo istrazivanje novih puteva...

                    ukupniPut[mrav] = prijedeniPut[mrav].duljinaObilaska();
                    double ksi = 0.1;
                    double tau0 = 0.001;

                    int prosli = 1;
                    for (int i = 1; i < prijedeniPut[mrav].put.Count(); i++)
                    {
                        feromoni[prijedeniPut[mrav].put[i].oznaka, prosli] = (1 - ksi) * (feromoni[prijedeniPut[mrav].put[i].oznaka, prosli]) + ksi * tau0;
                        feromoni[prosli, prijedeniPut[mrav].put[i].oznaka] = (1 - ksi) * (feromoni[prosli, prijedeniPut[mrav].put[i].oznaka]) + ksi * tau0;
                        prosli = prijedeniPut[mrav].put[i].oznaka;
                    }
                }

                // iteracija je zavrsila i trazimo najbolje rjesenje iz te iteracije, a zatim ga usporedujemo s najboljim koje do sada znamo

                double ukupniPutIteracijskiMin = ukupniPut[0];
                int indeksMin = 0;
                for (int i = 1; i < brojMrava; i++)
                {
                    if (ukupniPut[i] < ukupniPutIteracijskiMin)
                    {
                        ukupniPutIteracijskiMin = ukupniPut[i];
                        indeksMin = i;
                    }
                }

                if (ukupniPutIteracijskiMin < globalnaMinDuljina)
                {

                    boljeRjesenjePrijeKoliko = 0;
                    globalniNajboljiPut = new Obilazak();

                    foreach (var vrh in prijedeniPut[indeksMin].put)
                    {
                        globalniNajboljiPut.dodajVrh(vrh);
                    }

                    globalnaMinDuljina = ukupniPutIteracijskiMin;

                }

                // azuriramo feromone samo po najboljem do sada poznatom rjesenju, za iteracijsko bi islo ovako nekako:
                //   double feromonskiDelta = 1 / ukupniPutIteracijskiMin;
                // ... plus morale bi se promijeniti jos neke stvari malo nize  
                // u nekom od onih PDF-ova pise da je najbolje prvo azurirati najbolji iteracijski put a onda postepeno sve cesce samo globalni
                // najbolji put... a inace je, ako biramo samo jedno od tog dvoje, bolje stalno azurirati samo globalni. za sada stoji tako,
                // radi jednostavnosti.

                double feromonskiDelta = (1 / globalnaMinDuljina) * parametarEvaporacije;

                for (int i = 1; i <= brojVrhova; i++)
                {
                    for (int j = 1; j <= brojVrhova; j++)
                    {
                        feromoni[i, j] *= 1 - parametarEvaporacije;
                        feromoni[j, i] *= 1 - parametarEvaporacije;
                    }
                }

                int prosli2 = 1;
                for (int i = 1; i < globalniNajboljiPut.put.Count(); i++)
                {
                    feromoni[globalniNajboljiPut.put[i].oznaka, prosli2] += feromonskiDelta;
                    feromoni[prosli2, globalniNajboljiPut.put[i].oznaka] += feromonskiDelta;
                }
                boljeRjesenjePrijeKoliko++;
                if (kolikoIteracija < 0 && boljeRjesenjePrijeKoliko == Math.Abs(kolikoIteracija)) break ;
                if (kolikoIteracija != 0) brojIteracije++;

                int napredak = (int)((((double)brojIteracije-1)/(double)kolikoIteracija) * 100);
                backgroundWorker1.ReportProgress(napredak);

            }

            Obilazak o = globalniNajboljiPut;
            o.nacrtaj("nacrtaj");
            string optimalnoDatoteka = fileName.Substring(0, fileName.Length - 4) + ".opt";
            System.IO.StreamReader file = new System.IO.StreamReader(optimalnoDatoteka);
            Obilazak optimalniObilazak = new Obilazak();
            optimalniObilazak.dodajVrh(vrhovi[1]);
            string redak = file.ReadLine();

            while (true)
            {
                string[] rijeciURedku = redak.Split(' ');
                int kolikoRijeci = rijeciURedku.Count();

                for (int i = 2; i < kolikoRijeci; ++i)
                {
                    optimalniObilazak.dodajVrh(vrhovi[Convert.ToInt32(rijeciURedku[i]) + 1]);
                }
                optimalniObilazak.dodajVrh(vrhovi[1]);
                redak = file.ReadLine();
                if (redak.Split(' ').ElementAt(0) == "cost") break;
            }
            
            file.Close();

            System.Threading.Thread.Sleep(1000);

            optimalniObilazak.nacrtaj("optimalni"); 

            System.Threading.Thread.Sleep(1000);
            pictureBox1.ImageLocation = @"C:\Users\b\Documents\Visual Studio 2010\Projects\CVRPsaSuceljem\CVRPsaSuceljem\bin\Debug\nacrtaj.png";
            pictureBox2.ImageLocation = @"C:\Users\b\Documents\Visual Studio 2010\Projects\CVRPsaSuceljem\CVRPsaSuceljem\bin\Debug\optimalni.png";
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Change the value of the ProgressBar to the BackgroundWorker progress.
	    progressBar1.Value = e.ProgressPercentage;
	    // Set the text.
        this.Text = e.ProgressPercentage.ToString();
        }
    }
}
