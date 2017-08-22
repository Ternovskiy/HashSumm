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
            param.SourceMutex.WaitOne();
            ResutsHash souceData = param.Source as ResutsHash;
            param.SourceMutex.ReleaseMutex();
            IRepository Repository=new RepositorySql();
            
            while (!param.FlagStop)
            {
                param.SourceMutex.WaitOne();
                var f = souceData.Datas.Any();
                param.SourceMutex.ReleaseMutex();
                if (f)
                {
                    param.SourceMutex.WaitOne();
                    var d=souceData.Datas.Dequeue();
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
            param.SourceMutex.WaitOne();
            Queue<string> soucePaths = param.Source as Queue<string>;
            param.SourceMutex.ReleaseMutex();
            param.OutputMutex.WaitOne();
            ResutsHash output = param.Output as ResutsHash;
            param.OnInfoMessage("Количество обработанных файлов: " + output.Count);
            param.OutputMutex.ReleaseMutex();
            
            
            while (!param.FlagStop)
            {
                param.SourceMutex.WaitOne();
                var f = !soucePaths.Any();
                param.SourceMutex.ReleaseMutex();
                if (f)
                {
                    Thread.Sleep(500);continue;
                }
                param.SourceMutex.WaitOne();
                var fileName = soucePaths.Dequeue();
                param.SourceMutex.ReleaseMutex();
                
                using (var md5 = MD5.Create())
                {
                    try
                    {
                        using (var stream = File.OpenRead(fileName))
                        {
                            string result = Encoding.Default.GetString(md5.ComputeHash(stream));
                            param.OutputMutex.WaitOne();
                            output.Datas.Enqueue(new ResultData(){Value =result,Path = fileName});
                            output.Count++;
                            param.OnInfoMessage("Количество обработанных файлов: " + output.Count);
                            param.OutputMutex.ReleaseMutex();
                        }
                    }
                    catch (Exception e)
                    {
                        param.OnErrorMessage(e.Message, fileName);
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
            param.OutputMutex.WaitOne();
            var OutPut = param.Output as Queue<string>;
            param.OutputMutex.ReleaseMutex();
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

    public class ResutsHash
    {
        public Queue<ResultData> Datas { get; set; }
        public int Count { get; set; }
    }
}
