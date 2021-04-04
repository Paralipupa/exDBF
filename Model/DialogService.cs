using Microsoft.Win32;
using System.Windows;

namespace exDBF
{
    public class DialogService
    {
        public string File { get; set; }
        public string Path { get; set; }

        public bool OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Выберите файл";
            if (string.IsNullOrWhiteSpace(File) == false)
            {
                openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(File);
            }
            openFileDialog.Filter = "DBF(*.dbf)|*.dbf|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == true)
            {
                File = openFileDialog.FileName;
                return true;
            }
            return false;
        }

        public bool OpenPathDialog()
        {
            System.Windows.Forms.FolderBrowserDialog openPathDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (string.IsNullOrWhiteSpace(Path))
            {
                Path = System.AppDomain.CurrentDomain.BaseDirectory;
            }
            openPathDialog.SelectedPath = Path;

            if (openPathDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Path = openPathDialog.SelectedPath;
                return true;
            }
            return false;
        }

        public bool SaveFileDialog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                File = saveFileDialog.FileName;
                return true;
            }
            return false;
        }

        public static void ShowMessage(string message)
        {
            MessageBox.Show(message, "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static MessageBoxResult MsgBox(string message)
        {
            return MessageBox.Show(message, "Внимание", MessageBoxButton.YesNo);
        }
    }
}