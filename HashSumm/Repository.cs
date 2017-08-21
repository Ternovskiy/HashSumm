using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashSumm
{
    interface IRepository
    {
        bool SaveHash(string hashSumm, string path);

        bool SaveError(string erMes, string path = "");
    }

    public class RepositorySql : IRepository
    {
        private string conStr = Properties.Settings.Default.ConnectionString;
        public bool SaveHash(string hashSumm, string path)
        {
            using (var conn = new SqlConnection(conStr))
            {
                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    return false;
                }
                SqlTransaction transaction = conn.BeginTransaction();
                using (var cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = "exec dbo.saveHash @hashSumm, @path";
                        cmd.Parameters.AddWithValue("@hashSumm", hashSumm);
                        cmd.Parameters.AddWithValue("@path", path);
                        
                        cmd.ExecuteScalar();
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
            return true;
        }

        public bool SaveError(string erMes, string path = "")
        {
            using (var conn = new SqlConnection(conStr))
            {
                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    return false;
                }
                SqlTransaction transaction = conn.BeginTransaction();
                using (var cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = "exec dbo.saveError @erMes, @path";

                        cmd.Parameters.AddWithValue("@erMes", erMes);
                        cmd.Parameters.AddWithValue("@path", path);
                        
                        cmd.ExecuteScalar();
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public class RepositoryFile : IRepository
    {
        private string pathEr = "Err.txt";
        private string pathHash = "Hash.txt";
        public bool SaveHash(string hashSumm, string path)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(pathHash, true, System.Text.Encoding.Default))
                {
                    sw.WriteLine($"{DateTime.Now}   {path}  {hashSumm}");
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public bool SaveError(string erMes, string path = "")
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(pathEr, true, System.Text.Encoding.Default))
                {
                    sw.WriteLine($"{DateTime.Now}   {path}  {erMes}");
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
    }
}
