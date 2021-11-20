using Kompas6API5;
using KompasAPI7;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using forms = System.Windows.Forms;

namespace FillStamp3000.Models
{
    class Kompas
    {
        public string[] Files { get; set; }
        public ObservableCollection<string> FileNames { get; internal set; }

        public string SaveKompasFolder { get; set; }
        public string SavePdfFolder { get; set; }
        public bool IsPDFSave { get; set; }
        public bool IsKompasSave { get; set; }

        private readonly Type t;
        private readonly Type t7;
        public ObservableCollection<StampCell> Cells { get; set; }

        public string TemplatePath { get; set; }

        private KompasObject kompas;
        private IApplication kompas7;
        private ksDocument2D doc2D;
        private ksStamp stamp;
        private ksTextItemParam item;
        private ksTextLineParam line;
        private ksSpcDocument docSpec;

        public Kompas()
        {
            t = Type.GetTypeFromProgID("KOMPAS.Application.5");
            t7 = Type.GetTypeFromProgID("KOMPAS.Application.7");

            Cells = new ObservableCollection<StampCell>();
            FileNames = new ObservableCollection<string>();
        }

        public void OpenFiles()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Чертеж и спецификация (*.cdw;*.spw)|*.cdw;*.spw|Чертеж (*.cdw)|*.cdw|Спецификация (*.spw)|*.spw"
            };

            if (ofd.ShowDialog() == true)
            {
                SaveKompasFolder = Path.GetDirectoryName(ofd.FileNames[0]);
                SavePdfFolder = Path.GetDirectoryName(ofd.FileNames[0]);

                Files = ofd.FileNames;
                FileNames.Clear();
                Array.ForEach(Files, f => FileNames.Add(f.Substring(f.LastIndexOf(@"\") + 1)));
            }
        }
        public void OpenFolder()
        {
            var answer = MessageBox.Show("Искать файлы в подкаталогах?", "Выбор каталога", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);


            forms.FolderBrowserDialog fbd = new forms.FolderBrowserDialog()
            {
                Description = "Выберите каталог"
            };

            if (fbd.ShowDialog() == forms.DialogResult.OK)
            {
                SaveKompasFolder = fbd.SelectedPath;
                SavePdfFolder = fbd.SelectedPath;

                if (answer == MessageBoxResult.Yes)
                    Files = Directory.GetFiles(fbd.SelectedPath, "*.cdw", SearchOption.AllDirectories).Concat(Directory.GetFiles(fbd.SelectedPath, "*.spw", SearchOption.AllDirectories)).ToArray();
                else
                    Files = Directory.GetFiles(fbd.SelectedPath, "*.cdw", SearchOption.TopDirectoryOnly).Concat(Directory.GetFiles(fbd.SelectedPath, "*.spw", SearchOption.TopDirectoryOnly)).ToArray();
                FileNames.Clear();
                Array.ForEach(Files, f => FileNames.Add(f.Substring(f.LastIndexOf(@"\") + 1)));
            }
        }

        public void SelectSaveKompasFolder()
        {
            forms.FolderBrowserDialog fbd = new forms.FolderBrowserDialog()
            {
                Description = "Выберите каталог для сохранения"
            };

            if (fbd.ShowDialog() == forms.DialogResult.OK)
            {
                SaveKompasFolder = fbd.SelectedPath;
            }
        }

        public void SaveIni()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "Файл конфигурации (*.ini)|*.ini"
            };

            if (sfd.ShowDialog() == true)
            {
                var ini = sfd.FileName;

                string text = "";
                foreach (var cell in Cells)
                {
                    text += cell.Key.ToString() + "=" + cell.Value + "\n";
                }

                using (StreamWriter sw = new StreamWriter(ini))
                {
                    sw.WriteLine(text);
                }
            }
        }

        public void LoadIni()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Файл конфигурации (*.ini)|*.ini"
            };

            if (ofd.ShowDialog() == true)
            {
                Cells.Clear();
                var ini = ofd.FileName;

                using (StreamReader reader = new StreamReader(ini))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null && line != "")
                    {
                        var lineArray = line.Split('=');
                        StampCell cell = new StampCell()
                        {
                            Key = Int32.Parse(lineArray[0]),
                            Value = lineArray[1]
                        };
                        Cells.Add(cell);
                    }
                }
            }
        }

        public void LoadTemplate(BackgroundWorker backgroundWorker)
        {
            int value = 40;
            backgroundWorker.ReportProgress(value);

            kompas = (KompasObject)Activator.CreateInstance(t);
            if (TemplatePath.EndsWith(".cdw"))
            {
                doc2D = (ksDocument2D)kompas.Document2D();
                doc2D.ksOpenDocument(TemplatePath, false);

                item = (ksTextItemParam)kompas.GetParamStruct(31);
                line = (ksTextLineParam)kompas.GetParamStruct(29);

                stamp = (ksStamp)doc2D.GetStamp();
                stamp.ksOpenStamp();

                int n = 0;

                ksDynamicArray dynArLines = ((ksDynamicArray)stamp.ksGetStampColumnText(ref n));

                while (dynArLines != null)
                {
                    StampCell cell = new StampCell { Key = n };

                    for (int i = 0; i < dynArLines.ksGetArrayCount(); i++)
                    {
                        dynArLines.ksGetArrayItem(i, line);
                        ksDynamicArray dynArItems = (ksDynamicArray)line.GetTextItemArr();

                        if (dynArItems == null)
                            continue;

                        for (int j = 0; j < dynArItems.ksGetArrayCount(); j++)
                        {
                            dynArItems.ksGetArrayItem(j, item);
                            cell.Value = item.s;
                            backgroundWorker.ReportProgress(value++, cell);
                        }
                        dynArItems.ksDeleteArray();
                    }

                    dynArLines.ksDeleteArray();
                    dynArLines = ((ksDynamicArray)stamp.ksGetStampColumnText(ref n));
                }

                stamp.ksCloseStamp();

                doc2D.ksCloseDocument();
            }
            else if (TemplatePath.EndsWith(".spw"))
            {
                docSpec = (ksSpcDocument)kompas.SpcDocument();
                docSpec.ksOpenDocument(TemplatePath, 0);

                item = (ksTextItemParam)kompas.GetParamStruct((short)Kompas6Constants.StructType2DEnum.ko_TextItemParam);
                line = (ksTextLineParam)kompas.GetParamStruct((short)Kompas6Constants.StructType2DEnum.ko_TextLineParam);

                stamp = (ksStamp)docSpec.GetStamp();
                stamp.ksOpenStamp();

                int n = 0;

                ksDynamicArray dynArLines = ((ksDynamicArray)stamp.ksGetStampColumnText(ref n));

                while (dynArLines != null)
                {
                    StampCell cell = new StampCell { Key = n };

                    for (int i = 0; i < dynArLines.ksGetArrayCount(); i++)
                    {
                        dynArLines.ksGetArrayItem(i, line);
                        ksDynamicArray dynArItems = (ksDynamicArray)line.GetTextItemArr();

                        if (dynArItems == null)
                            continue;

                        for (int j = 0; j < dynArItems.ksGetArrayCount(); j++)
                        {
                            dynArItems.ksGetArrayItem(j, item);
                            cell.Value = item.s;
                            backgroundWorker.ReportProgress(value++, cell);
                        }
                        dynArItems.ksDeleteArray();
                    }

                    dynArLines.ksDeleteArray();
                    dynArLines = ((ksDynamicArray)stamp.ksGetStampColumnText(ref n));
                }

                stamp.ksCloseStamp();

                docSpec.ksCloseDocument();
            }

            kompas.Quit();
        }

        public void Save(string kompasPrefix, string kompasPostfix, string pdfPrefix, string pdfPostfix, BackgroundWorker backgroundWorker)
        {
            if (!IsPDFSave && !IsKompasSave)
                return;

            double progValue = 0;
            if (IsPDFSave && SavePdfFolder == null)
            {
                MessageBox.Show($"Вы не выбрали папку для сохранения PDF", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (IsKompasSave && SaveKompasFolder == null)
            {
                MessageBox.Show($"Вы не выбрали папку для сохранения чертежей", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            kompas = (KompasObject)Activator.CreateInstance(t);
            kompas7 = (IApplication)Activator.CreateInstance(t7);
            kompas7.HideMessage = Kompas6Constants.ksHideMessageEnum.ksHideMessageNo;

            var text = (ksTextItemParam)kompas.GetParamStruct(31);
            string fileName = null;

            foreach (var file in Files)
            {
                progValue++;
                backgroundWorker.ReportProgress((int)((progValue / Files.Count()) * 100));
                fileName = file.Substring(file.LastIndexOf(@"\") + 1);

                if (file.EndsWith(".cdw"))
                {
                    doc2D = (ksDocument2D)kompas.Document2D();
                    doc2D.ksOpenDocument(file, false);

                    stamp = (ksStamp)doc2D.GetStamp();
                    stamp.ksOpenStamp();

                    foreach (var cell in Cells)
                    {
                        stamp.ksColumnNumber(cell.Key);
                        text.s = cell.Value;
                        stamp.ksTextLine(text);
                    }
                    stamp.ksCloseStamp();

                    if (IsKompasSave)
                    {
                        doc2D.ksSaveDocument(SaveKompasFolder + @"\" + kompasPrefix + fileName.Substring(0, fileName.LastIndexOf('.')) + kompasPostfix + ".cdw");
                    }

                    if (IsPDFSave)
                        doc2D.ksSaveDocument(SavePdfFolder + @"\" + pdfPrefix + fileName.Substring(0, fileName.LastIndexOf('.')) + pdfPostfix + ".pdf");

                    doc2D.ksCloseDocument();
                }
                else if (file.EndsWith(".spw"))
                {
                    docSpec = (ksSpcDocument)kompas.SpcDocument();
                    docSpec.ksOpenDocument(file, 0);

                    stamp = (ksStamp)docSpec.GetStamp();
                    stamp.ksOpenStamp();

                    foreach (var cell in Cells)
                    {
                        stamp.ksColumnNumber(cell.Key);
                        text.s = cell.Value;
                        stamp.ksTextLine(text);
                    }
                    stamp.ksCloseStamp();

                    if (IsKompasSave)
                    {
                        docSpec.ksSaveDocument(SaveKompasFolder + @"\" + kompasPrefix + fileName.Substring(0, fileName.LastIndexOf('.')) + kompasPostfix + ".spw");

                    }

                    if (IsPDFSave)
                        docSpec.ksSaveDocument(SavePdfFolder + @"\" + pdfPrefix + fileName.Substring(0, fileName.LastIndexOf('.')) + pdfPostfix + ".pdf");

                    docSpec.ksCloseDocument();
                }
            }

            text = null;
            stamp = null;
            doc2D = null;
            docSpec = null;
            kompas.Quit();
            kompas = null;
            kompas7 = null;
        }

        public void DeleteRow(int selectedIndex)
        {
            if (Cells.Count > 0 && selectedIndex != -1 && selectedIndex != Cells.Count)
                Cells.RemoveAt(selectedIndex);
        }

        public void ClearRows()
        {
            Cells.Clear();
        }

        public void SelectSavePdfFolder()
        {
            forms.FolderBrowserDialog fbd = new forms.FolderBrowserDialog()
            {
                Description = "Выберите каталог для сохранения"
            };

            if (fbd.ShowDialog() == forms.DialogResult.OK)
            {
                SavePdfFolder = fbd.SelectedPath;
            }
        }

        public void ClearFiles()
        {
            Files = new string[0];
            FileNames.Clear();
        }
    }
}
