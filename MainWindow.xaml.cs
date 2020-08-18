using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace poeFilterSoundCopy
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private string mSoundPath;
        private string mMainPath;
        private string mOutputPath;
        private List<string> mResultData= null;

        public MainWindow()
        {
            InitializeComponent();
            List<string> mResultData = new List<string>();
            Title = $"poeFilterSoundCopy {ApplicationHelper.GetVersion()}";
        }

        private void InputSectionDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data != null)
                HandleDataObject(1, e.Data);

            InputSectionDragLeave(sender, e);
        }
        private void InputSectionDragDrop2(object sender, DragEventArgs e)
        {
            if (e.Data != null)
                HandleDataObject(2, e.Data);

            InputSectionDragLeave(sender, e);
        }

        private async void HandleDataObject(int index, IDataObject data)
        {
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filepaths = (string[])data.GetData(DataFormats.FileDrop);
                foreach (var filepath in filepaths)
                {
                    string extension = System.IO.Path.GetExtension(filepath);

                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() => MessageBox.Show(this, "Folders aren't supported.  Instead, select all of the files inside the folder you wish to convert.", "There was a problem"));
                    }
                    else
                    {
                        if (index == 1)
                        {
                            var soundBox = (TextBox)this.FindName("soundPath");
                            mSoundPath = soundBox.Text = filepath;
                        }
                        else if (index == 2)
                        {
                            var outPathBox = (TextBox)this.FindName("outputPath");
                            mOutputPath = outPathBox.Text = createOutputFileName(filepath);

                            var mainBox = (TextBox)this.FindName("mainPath");
                            mMainPath = mainBox.Text = filepath;
                        }
                        break;
                    }
                }
            }
        }

        private string createOutputFileName(string path)
        {
            string outputFileName = System.IO.Path.GetFileNameWithoutExtension(path) + "_CustomSound";
            return System.IO.Path.GetDirectoryName(path) + "\\" + outputFileName + System.IO.Path.GetExtension(path);
        }

        private void InputSectionDragEnter(object sender, DragEventArgs e)
        {
            (sender as Grid).Opacity = 0.75;
        }

        private void InputSectionDragLeave(object sender, DragEventArgs e)
        {
            (sender as Grid).Opacity = 0.5;
        }

        private void HyperlinkClick(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        private bool isStartWithShow(string line)
        {
            return (line.StartsWith("Show") || line.StartsWith("#Show"));
        }

        private async void StartButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(mSoundPath) || string.IsNullOrWhiteSpace(mMainPath) || string.IsNullOrWhiteSpace(mOutputPath))
            {
                await Application.Current.Dispatcher.InvokeAsync(() => MessageBox.Show(this, "Please drag&drop Sound / Main filter to copy.", "There was a problem"));
                return;
            }

            paragraphLoop();
            saveToNewFile();
            await Application.Current.Dispatcher.InvokeAsync(() => MessageBox.Show(this, "Copy Sound information Finish !! ", "Success :)"));
        }

        private void saveToNewFile()
        {
            System.IO.File.WriteAllLines(mOutputPath, mResultData);
        }

        private void paragraphLoop()
        {
            string[] soundFilter = System.IO.File.ReadAllLines(mSoundPath);
            string[] mainFilter = System.IO.File.ReadAllLines(mMainPath);

            mResultData = copyHeaderPart(mainFilter);
            for (int mainCount = 100;mainCount < 9900; mainCount+=100)
            {
                List<string> soundParagraph = findCurrentParaGraph(mainCount, soundFilter);
                List<string> mainParagraph = findCurrentParaGraph(mainCount, mainFilter);

                if (soundParagraph.Count == 0) return;
                for(int subCount = mainCount; subCount < mainCount+100; subCount++)
                {
                    List<string> soundSubParagraph = findCurrentSubParaGraph(subCount, soundParagraph);
                    List<string> mainSubParagraph = findCurrentSubParaGraph(subCount, mainParagraph);

                    if (soundSubParagraph.Count == 0) break;
                    mResultData.AddRange(copySoundLines(soundSubParagraph, mainSubParagraph));
                }
            }
        }

        private List<string> copyHeaderPart(string[] mainFilter)
        {
            List<string> result = new List<string>();
            int findCount = 0;
            for(int i=0;i<mainFilter.Length;i++)
            {
                if (mainFilter[i].StartsWith("# [[0100]]"))
                {
                    if (findCount == 0)
                        findCount++;
                    else
                        return result;
                }
                result.Add(mainFilter[i]);
            }
            return result;
        }

        private List<string> findCurrentSubParaGraph(int paraPosition, List<string> filter)
        {
            string startTag = string.Format("# [[{0:0000}]]", paraPosition);
            string endTag = string.Format("# [[{0:0000}]]", paraPosition + 1);
            List<string> result = new List<string>();

            for (int i = 0; i < filter.Count; i++)
            {
                if (!filter.ElementAt(i).StartsWith(startTag)) continue;
                for (int j = i; j < filter.Count; j++)
                {
                    if (filter.ElementAt(j).StartsWith(endTag)) break;
                    result.Add(filter.ElementAt(j));
                }
                break;
            }
            return result;
        }

        private List<string> findCurrentParaGraph(int paraPosition, string[] filter)
        {
            string startTag = string.Format("# [[{0:0000}]]", paraPosition);
            string endTag = string.Format("# [[{0:0000}]]", paraPosition+100);
            List<string> result = new List<string>();
            bool isFirst = true;

            for(int i=0; i<filter.Length;i++)
            {
                if (filter[i].StartsWith(startTag))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        continue;
                    }
                    else
                    {
                        for (int j = i; j < filter.Length; j++)
                        {
                            if (filter[j].StartsWith(endTag)) break;
                            result.Add(filter[j]);
                        }
                        break;
                    }
                }
            }
            return result;
        }

        private List<string> copySoundLines(List<string> sound, List<string> main)
        {
            int j = 0;
            List<string> result = main;
            for (int i = 0; i < sound.Count; i++)
            {
                if (isStartWithShow(sound.ElementAt(i)))
                {
                    int blockLineCount = 0;
                    string customSound = "";
                    for (blockLineCount = i + 1; blockLineCount < sound.Count; blockLineCount++)
                    {
                        if (isStartWithShow(sound.ElementAt(blockLineCount))) break;

                        if (sound.ElementAt(blockLineCount).Contains("CustomAlertSound"))
                        {
                            customSound = sound.ElementAt(blockLineCount);
                            break;
                        }
                    }

                    for (; j < main.Count; j++)
                        if (isStartWithShow(main.ElementAt(j))) break;
                    j++;

                    if (!string.IsNullOrWhiteSpace(customSound))
                    {
                        bool isInserted = false;
                        for (blockLineCount = j; blockLineCount < main.Count; blockLineCount++)
                        {
                            if (isStartWithShow(main.ElementAt(blockLineCount))) break;

                            if (main.ElementAt(blockLineCount).Contains("PlayAlertSound"))
                            {
                                if (main.ElementAt(blockLineCount).StartsWith("#"))
                                    customSound = "#" + customSound;

                                result[blockLineCount] = customSound;
                                isInserted = true;
                                break;
                            }
                        }
                        if (isInserted == false)
                        {
                            for (int cnt = j; j < main.Count; cnt++)
                            {
                                if (string.IsNullOrWhiteSpace(result.ElementAt(cnt)))
                                {
                                    result[cnt] = customSound;
                                    break;
                                }
                            }

                        }
                    }
                }

            }
            return result;
        }
    }


}
