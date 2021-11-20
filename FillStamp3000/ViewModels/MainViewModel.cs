using FillStamp3000.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using forms = System.Windows.Forms;

namespace FillStamp3000.ViewModels
{
    class MainViewModel : BaseViewModel
    {
        public Command OpenFolder { get; }
        public Command OpenFiles { get; }
        public Command ClearFiles { get; set; }
        public Command SelectSaveKompas { get; set; }
        public Command SelectSavePDF { get; set; }
        public Command LoadTemplate { get; set; }
        public Command SaveIni { get; set; }
        public Command LoadIni { get; set; }
        public Command OpenPattern { get; set; }
        public Command DeleteRow { get; set; }
        public Command ClearRows { get; set; }
        public Command Save { get; set; }
        public Command KompasCheck { get; set; }
        public Command PDFCheck { get; set; }


        public bool IsKompasSave { get => kompas.IsKompasSave; set { kompas.IsKompasSave = value; OnPropertyChanged(); } }
        public bool IsPDFSave { get => kompas.IsPDFSave; set { kompas.IsPDFSave = value; OnPropertyChanged(); } }
        public string ProgressText { get => progressText; set { progressText = value; OnPropertyChanged(); } }
        public double ProgressValue { get => progressValue; set { progressValue = value; OnPropertyChanged(); } }
        public string SaveKompasFolder { get => kompas.SaveKompasFolder; set { kompas.SaveKompasFolder = value; OnPropertyChanged(); } }
        public string SavePdfFolder { get => kompas.SavePdfFolder; set { kompas.SavePdfFolder = value; OnPropertyChanged(); } }
        public string KompasPrefix { get; set; }
        public string KompasPostfix { get; set; }
        public string PdfPrefix { get; set; }
        public string PdfPostfix { get; set; }

        public int SelectedIndex { get; set; }

        public ObservableCollection<string> Files { get => kompas.FileNames; }
        public ObservableCollection<StampCell> Cells { get => kompas.Cells; set { kompas.Cells = value; OnPropertyChanged(); } }

        private Kompas kompas;
        private string progressText;
        private double progressValue;
        private readonly BackgroundWorker worker;
        public MainViewModel()
        {
            kompas = new Kompas();

            OpenFolder = new Command(OpenFolderExecute);
            OpenFiles = new Command(OpenFilesExecute);
            ClearFiles = new Command(ClearFilesExecute);
            SelectSaveKompas = new Command(SelectSaveKompasExecute);
            SelectSavePDF = new Command(SelectSavePDFExecute);
            LoadTemplate = new Command(LoadTemplateExecute);
            SaveIni = new Command(SaveIniExecute);
            LoadIni = new Command(LoadIniExecute);
            OpenPattern = new Command(OpenPatternExecute);
            DeleteRow = new Command(DeleteRowExecute);
            ClearRows = new Command(ClearRowsExecute);
            Save = new Command(SaveExecute);
            KompasCheck = new Command(KompasCheckExecute);
            PDFCheck = new Command(PDFCheckExecute);

            worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
        }

        private void PDFCheckExecute(object obj)
        {
            IsPDFSave = IsPDFSave == false ? true : false;
        }

        private void KompasCheckExecute(object obj)
        {
            IsKompasSave = IsKompasSave == false ? true : false;
        }

        private void SaveIniExecute(object obj)
        {
            kompas.SaveIni();
        }

        private void LoadIniExecute(object obj)
        {
            kompas.LoadIni();
        }

        private void OpenPatternExecute(object obj)
        {
            Pattern pattern = new Pattern();
            pattern.Show();
        }

        #region LoadTemplateBackground
        private void LoadTemplateRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressText = "Готово";
            ProgressValue = 0;
            worker.DoWork -= LoadTemplateDoWork;
            worker.ProgressChanged -= LoadTemplateOnProgressChanged;
            worker.RunWorkerCompleted -= LoadTemplateRunWorkerCompleted;
        }
        private void LoadTemplateOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressValue = e.ProgressPercentage;
            if (e.UserState != null)
                Cells.Add((StampCell)e.UserState);
        }
        private void LoadTemplateDoWork(object sender, DoWorkEventArgs e)
        {
            kompas.LoadTemplate(sender as BackgroundWorker);
        }
        private void LoadTemplateExecute(object obj)
        {
            ProgressText = "Загрузка шаблона";
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Чертеж (*.cdw)|*.cdw|Спецификация (*.spw)|*.spw"
            };

            if (ofd.ShowDialog() == false)
                return;

            kompas.TemplatePath = ofd.FileName;

            kompas.ClearRows();

            worker.DoWork += LoadTemplateDoWork;
            worker.ProgressChanged += LoadTemplateOnProgressChanged;
            worker.RunWorkerCompleted += LoadTemplateRunWorkerCompleted;
            worker.RunWorkerAsync();
        }
        #endregion

        private void ClearRowsExecute(object obj)
        {
            kompas.ClearRows();
        }

        private void DeleteRowExecute(object obj)
        {
            kompas.DeleteRow(SelectedIndex);
        }

        private void SaveExecute(object obj)
        {
            worker.DoWork += SaveDoWork;
            worker.ProgressChanged += SaveOnProgressChanged;
            worker.RunWorkerCompleted += SaveRunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        private void SaveRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressValue = 0;
            ProgressText = "Готово";
            worker.DoWork -= SaveDoWork;
            worker.ProgressChanged -= SaveOnProgressChanged;
            worker.RunWorkerCompleted -= SaveRunWorkerCompleted;
        }

        private void SaveOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressValue = e.ProgressPercentage;
        }

        private void SaveDoWork(object sender, DoWorkEventArgs e)
        {
            ProgressText = "Сохранение файлов";
            kompas.Save(KompasPrefix, KompasPostfix, PdfPrefix, PdfPostfix, sender as BackgroundWorker);
        }

        private void SelectSavePDFExecute(object obj)
        {
            kompas.SelectSavePdfFolder();
        }

        private void SelectSaveKompasExecute(object obj)
        {
            kompas.SelectSaveKompasFolder();
        }

        private void ClearFilesExecute(object obj)
        {
            kompas.ClearFiles();
        }

        private void OpenFilesExecute(object obj)
        {
            kompas.OpenFiles();
        }

        private void OpenFolderExecute(object obj)
        {
            kompas.OpenFolder();
        }
    }
}
