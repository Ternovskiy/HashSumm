using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HashSumm
{

    public static class Functions
    {
        public static Action<object> SaveDatas = delegate(object paramObj)
        {
            var param = paramObj as Parametrs;
            if (param == null) return;
            Queue<ResultData> souceData = param.Source as Queue<ResultData>;
            
            IRepository Repository=new RepositorySql();
            
            while (!param.FlagStop)
            {
                if (souceData.Any())
                {
                    param.SourceMutex.WaitOne();
                    var d=souceData.Dequeue();
                    param.SourceMutex.ReleaseMutex();
                    if (d.IsError)
                    {
                        if (!Repository.SaveError(d.Value, d.Path))
                        {
                            param.OnErrorMessage("Ошибка при работе с базой, запись в файл");
                            Repository=new RepositoryFile();
                            if (!Repository.SaveError(d.Value, d.Path)) return;
                        }
                    }
                    else
                    {
                        if (!Repository.SaveHash(d.Value, d.Path))
                        {
                            param.OnErrorMessage("Ошибка при работе с базой, запись в файл");
                            Repository = new RepositoryFile();
                            if (!Repository.SaveHash(d.Value, d.Path))return;
                        }
                    }
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        };


        public static Action<object> CalculateHashSumm = delegate (object paramObj)
        {

            var param = paramObj as Parametrs;
            if (param == null) return;

            Queue<string> soucePaths = param.Source as Queue<string>;
            Queue<ResultData> output = param.Output as Queue<ResultData>;

            int count = 0;
            param.OnInfoMessage("Количество обработанных файлов: " + count);
            while (!param.FlagStop)
            {
                
                if (!soucePaths.Any())
                {
                    Thread.Sleep(500);continue;
                }
                param.SourceMutex.WaitOne();
                var fileName = soucePaths.Dequeue();
                param.SourceMutex.ReleaseMutex();
                
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(fileName))
                    {
                        string result = Encoding.Default.GetString(md5.ComputeHash(stream));
                        param.OutputMutex.WaitOne();
                        output.Enqueue(new ResultData(){Value =result,Path = fileName});
                        param.OutputMutex.ReleaseMutex();

                        count++;
                        param.OnInfoMessage("Количество обработанных файлов: " + count);
                    }
                }
            }
        };


        public static Action<object> SearchFiles = delegate (object paramObj)
        {

            var param = paramObj as Parametrs;
            if (param == null) return;
            var SourcePath = (string)param.Source;
            int count = 0;
            RunSeach(SourcePath, param, ref count);
        };

        static void RunSeach(string path, Parametrs param, ref int count)
        {
            var OutPut = param.Output as Queue<string>;
            try
            {
                var files = Directory.GetFiles(path);

                foreach (string file in files)
                {
                    param.OutputMutex.WaitOne();
                    OutPut.Enqueue(file);
                    param.OutputMutex.ReleaseMutex();

                }
                count += files.Length;
                param.OnInfoMessage("Количество файлов для обработки: " + count);
                if (param.FlagStop) return;
                var dirs = Directory.GetDirectories(path);
                foreach (var dir in dirs)
                {
                    RunSeach(dir, param, ref count);
                }
            }
            catch (Exception e)
            {
                param.OnErrorMessage(e.Message);
            }
        }

    }

    public class ResultData
    {
        public string Path { get; set; }
        public string Value { get; set; }
        public bool IsError { get; set; } = false;
    }
}
