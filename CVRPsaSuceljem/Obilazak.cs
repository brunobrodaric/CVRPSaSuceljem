using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CVRP1
{
    class Obilazak
    {
        public List<Vrh> put;

        public Obilazak()
        {
            put = new List<Vrh>();
        }

        public Obilazak(List<Vrh> listaVrhova)
        {
            put = listaVrhova;
        }

        public void dodajVrh(Vrh vrh)
        {
            put.Add(vrh);
        }

        public bool jeLiJednak(Obilazak o)
        {
            if (o == null && this == null) return true;
            if (o == null && this != null) return false;
            if (o != null && this == null) return false;
            int brojCvorova1 = o.put.Count();
            int brojCvorova2 = this.put.Count();
            if (brojCvorova1 != brojCvorova2) return false;
            else
            {
                for (int i = 0; i < brojCvorova1; ++i)
                {
                    if (o.put[i] != this.put[i]) return false;
                }
                return true;
            }

        }

        public void ispisi()
        {
            if (put == null) return;
            foreach (var vrh in put)
            {
                Console.Write(vrh.oznaka + " ");
            }
            Console.WriteLine();
        }

        public double duljinaObilaska(int smijeNula = 1)
        {
            double duljina = 0;
            if (put == null) return 0;
            for (int i = 1; i < put.Count(); i++)
            {
                duljina += put[i - 1].udaljenost(put[i], smijeNula);
            }
            return duljina;
        }

        public bool dopustiv(double dopustenaCijena)
        {
            if (put[0].oznaka != 1) return false;
            if (put[put.Count() - 1].oznaka != 1) return false;
            double cijena = 0;
            foreach (var vrh in put)
            {
                cijena += vrh.potraznja;
                if (cijena > dopustenaCijena) return false;
                if (vrh.oznaka == 1) cijena = 0;
            }
            return true;
        }

        // nije doslovce 2-opt, tj. prikladniji naziv bi mozda bio kvaziDvaOpt... ipak, trebalo bi raditi *vise* od pravog dvaOpta (ali sporije)
        // pokusa zamijeniti *svaka* dva vrha u obilasku, cak ako su isti ili zamjena nije dopustiva itd. a onda gleda je li ta zamjena dopustiva
        // pa na kraju medju svim dopustivim zamjenama vraca onu koja najvise poboljsava rjesenje AKO ga ijedna poboljsava.
        // moze se poboljsati tako da ima manje *potpuno* nepotrebnih operacija...
        // treci parametar za sada neiskoristen, moze se iskoristiti za to da gledamo zamjene samo najblizih vrhova, ali
        // to moze dovesti do toga da funkcija ne nadje neko bolje rjesenje koje je mogla. prednost bi bila povecanje brzine, ali za sada je
        // ipak kvaliteta rjesenja prioritet...
        public Obilazak dvaOpt(double dopustenaCijena, double ulaznaDuljina, Dictionary<Vrh, List<Vrh>> najbliziVrhovi = null)
        {
            if (put == null) return null;

            Obilazak obilazak;
            Obilazak izlazniObilazak = null;
            List<Obilazak> listaObilazaka = new List<Obilazak>();

            for (int i = 1; i < put.Count(); i++)
            {
                for (int j = i; j < put.Count(); j++)
                {
                    {
                        obilazak = new Obilazak();
                        foreach (var vrh in put) obilazak.dodajVrh(vrh);
                        Vrh temp = obilazak.put[i];
                        obilazak.put[i] = obilazak.put[j];
                        obilazak.put[j] = temp;
                        if (obilazak.dopustiv(dopustenaCijena))
                            listaObilazaka.Add(obilazak);
                    }
                }
            }

            double najboljaDuljina = ulaznaDuljina;
            foreach (var ob in listaObilazaka)
            {
                if (ob.duljinaObilaska() < najboljaDuljina)
                {
                    najboljaDuljina = ob.duljinaObilaska();
                    izlazniObilazak = ob;
                }
            }

            if (najboljaDuljina < ulaznaDuljina)
                return izlazniObilazak;
            else return null;
        }

        public Obilazak optimalniObilazak(string instanca, string optimalnoRjesenje)
        {
            TestniPodaci podaci = new TestniPodaci(instanca);

            int brojVrhova = podaci.brojVrhova;
            Vrh[] vrhovi = new Vrh[brojVrhova + 1];
            double kapacitetVozila = podaci.kapacitetVozila;
            for (int i = 0; i <= brojVrhova; ++i)
            {
                vrhovi[i] = podaci.vrhovi[i];
            }

            System.IO.StreamReader file = new System.IO.StreamReader(optimalnoRjesenje);
            Obilazak optimalniObilazak = new Obilazak(); 
            optimalniObilazak.dodajVrh(vrhovi[1]);
            string redak = file.ReadLine();
            while (redak != null)
            {
                string[] rijeciURedku = redak.Split(' ');
                int kolikoRijeci = rijeciURedku.Count();
                if (kolikoRijeci < 3) continue;
                for (int i = 2; i < kolikoRijeci; ++i)
                {
                    optimalniObilazak.dodajVrh(vrhovi[Convert.ToInt32(rijeciURedku[i]) + 1]);                
                }
                optimalniObilazak.dodajVrh(vrhovi[1]);
            }
            file.Close();
            return optimalniObilazak;
        }



        // funkcija za crtanje rjesenja... potrebno imati instaliran graphviz
        // uglavnom, skoro sve je podesivo, guglati npr. graphviz attributes
        // generira sliku rjesenja (fileName.png) u bin\debug folderu projekta
        public void nacrtaj(string fileName)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(fileName + ".txt");

            string[] boje = { "red", "navy", "green", "pink", "yellow", "tomato", "blue", "purple", "teal", "black", "violet", "crimson" };

            int brojBoje = 0;
            bool nijePocetakObilaska = false;
            file.WriteLine("graph{");
            foreach (var cvor in this.put)
            {
                file.WriteLine("resolution=500;");
                file.WriteLine(cvor.oznaka + "[");
              //  file.WriteLine("label = " + cvor.oznaka);
                file.WriteLine("pos = \"" + cvor.x * 6 + "," + cvor.y * 6 + "!\"");
                file.WriteLine("width = 0.002");
                file.WriteLine("height = 0.002");
                file.WriteLine("fixedsize=true");
                file.WriteLine("fontsize = 8");
                file.WriteLine("color =" + boje[brojBoje]);
                if (cvor.oznaka == 1) file.WriteLine("penwidth = 0.1, color = black, shape = box, width = 0.07, height = 0.07, label = \"\""); else file.WriteLine("shape = point");
                file.WriteLine("]");
                if (cvor.oznaka == 1 && nijePocetakObilaska) brojBoje++; else { nijePocetakObilaska = true; }
            }

            brojBoje = 0;
            
            for (int i = 1; i < this.put.Count(); ++i)
            {
                file.WriteLine(this.put[i - 1].oznaka + " -- " + this.put[i].oznaka + "[penwidth = 0.099, color=\"" + boje[brojBoje] + "\"]");
                if (this.put[i].oznaka == 1) brojBoje++;
            }

            file.WriteLine("}");
            file.Close();
            ProcessStartInfo startInfo = new ProcessStartInfo("dot.exe");
            startInfo.Arguments = "-Kneato -Goverlap=scaling -Tpng " + fileName + ".txt -o " + fileName + ".png";
            Process.Start(startInfo);
        }

    }
}
