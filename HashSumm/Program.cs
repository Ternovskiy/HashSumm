using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HashSumm
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string pathToCalk = @"d:\";

            FolderBrowserDialog FBD = new FolderBrowserDialog();
            if (FBD.ShowDialog() == DialogResult.OK)
            {
                pathToCalk=FBD.SelectedPath;
            }
            else
            {
                Console.WriteLine("Программа завершена.");
                Console.ReadKey();
                return;
            }



            Queue<string> Files = new Queue<string>();
            Mutex FilesMutex = new Mutex();

            Queue<ResultData> Datas = new Queue<ResultData>();
            Mutex DatasMutex = new Mutex();

            var ParametrsSearchFile = new Parametrs();
            ParametrsSearchFile.Action = Functions.SearchFiles;
            ParametrsSearchFile.Source = pathToCalk;
            ParametrsSearchFile.SourceMutex = null;
            ParametrsSearchFile.Output = Files;
            ParametrsSearchFile.OutputMutex = FilesMutex;
            ParametrsSearchFile.InfoMessage += delegate (string mes, string path) { SearchMes = mes; };
            ParametrsSearchFile.ErrorMessage += Parametrs_ErrorMessage;



            var ParametrsCalcSumm = new Parametrs();
            ParametrsCalcSumm.Action = Functions.CalculateHashSumm;
            ParametrsCalcSumm.Source = Files;
            ParametrsCalcSumm.SourceMutex = FilesMutex;
            ParametrsCalcSumm.Output = Datas;
            ParametrsCalcSumm.OutputMutex = DatasMutex;
            ParametrsCalcSumm.InfoMessage += delegate (string mes, string path) { CalculateMes = mes; };
            ParametrsCalcSumm.ErrorMessage += Parametrs_ErrorMessage;

            var ParametrsSave = new Parametrs();
            ParametrsSave.Action = Functions.SaveDatas;
            ParametrsSave.Source = Datas;
            ParametrsSave.SourceMutex = DatasMutex;
            ParametrsSave.ErrorMessage += Parametrs_ErrorMessage;


            List<Parametrs> parametrs = new List<Parametrs>();
            parametrs.Add(ParametrsSearchFile);
            parametrs.Add(ParametrsCalcSumm);
            parametrs.Add(ParametrsSave);

            foreach (Parametrs parametr in parametrs)
            {
                parametr.ErrorMessage += delegate (string mes, string path)
                        {
                            var d = new ResultData { IsError = true, Path = path, Value = mes };
                            DatasMutex.WaitOne();
                            Datas.Enqueue(d);
                            DatasMutex.ReleaseMutex();
                        };
                parametr.Start();
            }


            var t = new Thread(GetInput);
            t.Start();
            while (ParametrsSearchFile.Thread.IsAlive || Datas.Any() || Files.Any())
            {
                InfoMessage();
                Console.WriteLine("Для выхода нажните Esc");
                Thread.Sleep(1000);
                if (exit) break;
            }

            foreach (Parametrs parametr in parametrs)
                parametr.FlagStop = true;

            if (exit)
            {
                InfoMessage();
                Console.WriteLine("Вычисления прерваны. Дождитесь завершения опереции и программа закроется.");
            }
            else
            {
                exit = true;
                InfoMessage();
                Console.WriteLine("Вычисления завершены.");
            }

        }


        static bool exit = false;
        static void GetInput()
        {
            while (!exit)
            {
                var a = Console.ReadKey(true);
                if (a.Key == ConsoleKey.Escape)
                    exit = true;
            }
        }

        private static string SearchMes = "";
        private static string CalculateMes = "";
        private static string ErMes = "";
        private static void Parametrs_ErrorMessage(string message, string path = "")
        {
            ErMes += "\n" + message;
        }
        private static void InfoMessage()
        {//Количество обработанных файлов/Количество файлов для обработки
            Console.Clear();
            Console.WriteLine($"{SearchMes} / {CalculateMes}");
            //Console.WriteLine("\nОшибки:" + ErMes);
        }
    }
}
