using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace exDBF
{
    class WorkDBF
    {
        private OdbcConnection Conn = null;

        private List<int> _indexes;
        public List<int> Indexes
        {
            get { return _indexes; }
            set { _indexes = value; }
        }


        public DataTable Execute(string Command)
        {
            DataTable dt = null;
            if (Conn != null)
            {
                try
                {


                    Conn.Open();
                    dt = new DataTable();
                    System.Data.Odbc.OdbcCommand oCmd = Conn.CreateCommand();
                    oCmd.CommandText = Command;
                    dt.Load(oCmd.ExecuteReader());
                    Conn.Close();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            return dt;
        }

        public async Task<int> FindTextAsync(string text, string path, bool isContinue=true)
        {
            if (isContinue)
            {
                return await Task.Run(() => FindText(text, path));
            }
            else
            {
                return -1;
            }
        }

        public int FindText(string text, string path)
        {
            int nIndex = 0;

            try
            {
                Encoding cp866 = Encoding.GetEncoding("cp866");
                if (path.ToLower().IndexOf(".dbf") == -1) cp866 = Encoding.GetEncoding(1251);
                using (StreamReader sr = new StreamReader(path, cp866))
                {
                    string str = sr.ReadToEnd();
                    _indexes = str.ToUpper().AllIndexesOf(text.ToUpper());
                    nIndex = _indexes.Count == 0 ? -1 : _indexes[0];
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return nIndex;
        }

        public DataTable GetAll(string DB_path)
        {
            return Execute("SELECT * FROM " + DB_path);
        }

        public async Task<List<string>> GetFilesAsync(string start_path)
        {
            return await Task.Run(() => GetRecursFiles(start_path));
        }

        public List<string> GetRecursFiles(string start_path)
        {
            string[] extentions = { ".dbf"};
            List<string> ls = new List<string>();
            try
            {
                string[] folders = Directory.GetDirectories(start_path);
                foreach (string folder in folders)
                {
                    if (start_path.Length > 3)
                    {
                        ls.Add("Папка: " + folder);
                        ls.AddRange(GetRecursFiles(folder));
                    }
                }
                string[] files = Directory.GetFiles(start_path);
                foreach (string filename in files)
                {
                    if (filename.IndexOfAny(extentions) != -1)
                    {
                        ls.Add("Файл: " + filename);
                    }

                }
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return ls;
        }

        public bool SaveToFile(string path, string filename, byte[] array, FileMode mode = FileMode.Create)
        {
            if (array.Length == 0)
            {
                return false;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            using (FileStream fstream = new FileStream($"{path}\\{filename}", mode))
            {
                fstream.Write(array, 0, array.Length);
            }

            return true;
        }

        public bool SaveRecordToFile(string path, string filename, List<string> lists, FileMode mode = FileMode.CreateNew)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string text = "";
                foreach(string item in lists)
                {
                    text += item + ";";
                }

                if (mode == FileMode.CreateNew)
                {
                    File.AppendAllText($"{path}\\{filename}", $"{text}\r\n", Encoding.GetEncoding("Windows-1251"));
                }
                else
                {
                    File.WriteAllText($"{path}\\{filename}", $"{text}\r\n", Encoding.GetEncoding("Windows-1251"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return true;

        }
        
        public WorkDBF() { }

        public WorkDBF(string path)
        {
            this.Conn = new System.Data.Odbc.OdbcConnection();
            Conn.ConnectionString = @"Driver={Microsoft dBase  Driver (*.dbf)};" +
                   "SourceType=DBF;Exclusive=No;" +
                   "Collate=Machine;NULL=NO;DELETED=NO;" +
                   "BACKGROUNDFETCH=NO; SourceDB=" + path + ";";
        }
    }
}
