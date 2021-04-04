using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace exDBF.ViewModel
{
    class MainViewModel : INotifyPropertyChanged
    {
        const string TITLE_BUTTON_EXTRACT = " Выгрузить в файл ";

        private bool _isWait = false;
        public bool IsWait
        {
            get { return _isWait; }
            set { _isWait = value; }
        }

        private string _buttonExtractTitle;
        public string ButtonExtractTitle
        {
            get { return _buttonExtractTitle; }
            set
            {
                _buttonExtractTitle = value;
                OnPropertyChanged("ButtonExtractTitle");
            }
        }

        private bool _isvisiblelist;
        public bool IsVisibleList
        {
            get { return _isvisiblelist; }
            set { _isvisiblelist = value; OnPropertyChanged("IsVisibleList"); }
        }

        public string FileDBF
        {
            get => exDBF.Properties.Settings.Default.fileDBF;
            set
            {
                exDBF.Properties.Settings.Default.fileDBF = value;
                exDBF.Properties.Settings.Default.Save();
            }
        }

        public string PathCSV
        {
            get => exDBF.Properties.Settings.Default.pathCSV;
            set
            {
                exDBF.Properties.Settings.Default.pathCSV = value;
                exDBF.Properties.Settings.Default.Save();
            }
        }

        public string PathPIC
        {
            get => exDBF.Properties.Settings.Default.pathPIC;
            set
            {
                exDBF.Properties.Settings.Default.pathPIC = value;
                exDBF.Properties.Settings.Default.Save();
            }
        }

        public int MaxRecord
        {
            get => Int32.Parse(exDBF.Properties.Settings.Default.maxRecords);
            set
            {
                exDBF.Properties.Settings.Default.pathPIC = value.ToString();
                exDBF.Properties.Settings.Default.Save();
                OnPropertyChanged("MaxRecord");
            }
        }

        public string TitleButton
        {
            get => exDBF.Properties.Settings.Default.pathPIC;
            set
            {
                exDBF.Properties.Settings.Default.pathPIC = value;
                exDBF.Properties.Settings.Default.Save();
            }
        }

        private bool _isclickenabled = true;
        public bool IsClickEnabled
        {
            get { return _isclickenabled; }
            set { _isclickenabled = value; IsVisibleAnimation = !_isclickenabled; OnPropertyChanged("IsClickEnabled"); }
        }

        private bool _isNoEmptyPicture = true;
        public bool IsNoEmptyPicture
        {
            get { return _isNoEmptyPicture; }
            set { _isNoEmptyPicture = value; OnPropertyChanged("IsNoEmptyPicture"); }
        }

        private bool _isvisibleanimation = false;
        public bool IsVisibleAnimation
        {
            get { return _isvisibleanimation; }
            set { _isvisibleanimation = value; OnPropertyChanged("IsVisibleAnimation"); }
        }

        public ObservableCollection<string> Pictures { get; set; } = new ObservableCollection<string>();

        public WorkDBF DBF { get; set; } = new WorkDBF();

        public string SelectedFile { get; set; }

        public System.Windows.Input.ICommand SelectionDBFFileCommand => new Command(
            _ =>
            {
                DialogService dlg = new DialogService();
                dlg.File = FileDBF;
                if (dlg.OpenFileDialog())
                {
                    FileDBF = dlg.File;
                }
            }, (obj) => { return IsClickEnabled; });

        public System.Windows.Input.ICommand ExtractDBFCommand => new Command((obj) =>
        {
            string filename = FileDBF;
            if ((filename.IndexOf("Файл:") != -1))
            {
                filename = filename.Substring(filename.IndexOf("Файл:") + 6);
            }

            RecordsToFileAsync(filename);

        }, (obj) => { return FileDBF != null && (string.IsNullOrWhiteSpace(FileDBF) == false); });

        public async void RecordsToFileAsync(string filename)
        {
            if (IsWait)
            {
                IsWait = false;
                return;
            }

            IsClickEnabled = false;

            IsWait = true;

            ButtonExtractTitle = " Остановить ";


            try
            {
                DBFReader dbffile = new DBFReader(filename, Encoding.GetEncoding("cp866"));
                if (dbffile.IsDBFFile() == false)
                {
                    System.Windows.Forms.MessageBox.Show($"{filename} не является файлом формата DBF.");
                    throw new Exception($"{filename} не является файлом формата DBF.");
                }

                IEnumerable<Dictionary<string, object>> dictList = dbffile.ReadToDictionary();

                string pathPicture = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), PathPIC);
                string pathCSV = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), PathCSV);

                int index = 1;
                int indexPic = 1;
                int indexCSV = 0;

                Pictures.Clear();

                string typeoper = "";

                foreach (Dictionary<string, object> item in dictList)
                {

                    string fnamePicture = $"pic_{indexPic.ToString().PadLeft(4, '0')}.jpg";
                    string fnameCSV = indexCSV == 0 ? "file.csv" : $"file_{indexCSV.ToString().PadLeft(4, '0')}.csv";

                    await Task.Run(() =>
                    {
                        List<string> lists = new List<string>();
                        lists.Add(indexPic.ToString());

                        foreach (KeyValuePair<string, object> keyVal in item)
                        {
                            if (keyVal.Key.Contains("(M)"))
                            {

                                if (System.IO.File.Exists($"{pathPicture}\\{fnamePicture}") == false)
                                {
                                    byte[] buffer = new byte[0];

                                    byte[] row = dbffile.ReadByteFPTRecord(Int32.Parse(keyVal.Value.ToString()));

                                    //начальный блок файла .jpg/.gif/.png
                                    if ((row.Length > 20) &&
                                    (
                                        ((row[16] == 0xFF) && (row[17] == 0xD8) && (row[18] == 0xFF) && (row[19] == 0xE0)) || // jpg
                                        ((row[16] == 0x47) && (row[17] == 0x49) && (row[18] == 0x46) && (row[19] == 0x38)) || // gif
                                        ((row[16] == 0x89) && (row[17] == 0x50) && (row[18] == 0x4E) && (row[19] == 0x47)) || // png
                                        ((row[8] == 0x40) && (row[9] == 0x52) && (row[10] == 0x5F) && (row[11] == 0x42))      //@R_BLOB@
                                    )
                                    )
                                    {
                                        buffer = new byte[row.Length - 16];
                                        Array.Copy(row, 16, buffer, 0, row.Length - 16);
                                    }
                                    else
                                    {
                                        buffer = new byte[row.Length];
                                        Array.Copy(row, 0, buffer, 0, row.Length);
                                    }

                                    //если не jpg, то меняем тип и название файла
                                    if (!((buffer.Length > 4) && ((buffer[0] == 0xFF) && (buffer[1] == 0xD8) && (buffer[2] == 0xFF) && (buffer[3] == 0xE0))))
                                    {
                                        if (((buffer.Length > 4) && ((buffer[0] == 0x47) && (buffer[1] == 0x49) && (buffer[2] == 0x46) && (buffer[3] == 0x38))))
                                        {
                                            fnamePicture = (fnamePicture.Substring(0, fnamePicture.IndexOf(".jpg")) + ".gif");
                                        }
                                        else
                                        {
                                            if (((buffer.Length > 4) && ((buffer[0] == 0x89) && (buffer[1] == 0x50) && (buffer[2] == 0x4E) && (buffer[3] == 0x47))))
                                            {
                                                fnamePicture = (fnamePicture.Substring(0, fnamePicture.IndexOf(".jpg")) + ".png");
                                            }
                                            else
                                            {
                                                fnamePicture = (fnamePicture.Substring(0, fnamePicture.IndexOf(".jpg")) + ".txt").Replace("pic_", "file_");
                                            }
                                        }
                                    }


                                    if (DBF.SaveToFile(pathPicture, fnamePicture, buffer))
                                    {
                                        lists.Add(fnamePicture);
                                        typeoper = "создан";
                                    }
                                    else
                                    {
                                        lists.Add("no file");
                                        typeoper = "ошибка чтения";
                                    }
                                }
                                else
                                {
                                    lists.Add(fnamePicture);
                                    typeoper = "ok";
                                }

                                if (System.Windows.Application.Current != null)
                                {
                                    System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
                                    {
                                        Pictures.Insert(0, $"{fnamePicture} - {typeoper}");
                                    });
                                }

                            }
                            else
                            {
                                lists.Add(keyVal.Value.ToString());
                            }
                        }

                        //добавлять запись, только если создан файл jpg 
                        if (IsNoEmptyPicture == false || typeoper == "создан")
                        {
                            DBF.SaveRecordToFile(pathCSV, fnameCSV, lists);
                        }
                        if (IsNoEmptyPicture == false || typeoper == "создан" || typeoper == "ok")
                        {
                            index++;
                        }

                        typeoper = "";

                    });


                    if (IsWait == false)
                    {
                        break;
                    }

                    indexPic++;

                    //Разделить файл csv, если установлено ограничение записей 
                    if (MaxRecord != 0)
                    {
                        if (index % MaxRecord == 0)
                        {
                            indexCSV++;
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Log.Instance.Write(ex);
            }

            IsWait = false;
            IsClickEnabled = true;
            ButtonExtractTitle = TITLE_BUTTON_EXTRACT;

        }
          
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public MainViewModel()
        {
            ButtonExtractTitle = TITLE_BUTTON_EXTRACT;
        }

    }
}
