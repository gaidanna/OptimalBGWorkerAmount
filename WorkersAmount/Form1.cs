using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        object locker;
        int timeDifference;
        List<string> folderList;
        List<string> listOfDirectories = new List<string>();
        List<TimeSpan> execusionTimeList = new List<TimeSpan>();
        private List<BackgroundWorker> workersList = new List<BackgroundWorker>();
        BackgroundWorker bgWorker = new BackgroundWorker();
        BackgroundWorker bgWorkerForSubdirectories = new BackgroundWorker();
        Stopwatch watch = new Stopwatch();

        public Form1()
        {
            locker = new object();

            InitializeComponent();
            InitializeBackgroundWorker(bgWorker);
            buttonCancel.Enabled = false;
        }

        private void InitializeBackgroundWorker(BackgroundWorker worker)
        {
            worker.DoWork += new DoWorkEventHandler(backgroundWorkerFiles_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorkerFiles_RunWorkerCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorkerFiles_ProgressChanged);
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
        }

        private void OnButtonClick(object sender, EventArgs e)
        {
            workersList.Clear();
            textBoxForResult.Clear();
            textBoxSummary.Clear();
            execusionTimeList.Clear();

            if (Directory.Exists(textBoxForPath.Text) != true)
            {
                textBoxForResult.Text = "Path is invalid.";
            }
            else
            {
                buttonCancel.Enabled = true;
                buttonGetInfo.Enabled = false;
                buttonBrowseFolder.Enabled = false;

                folderList = GetAllDirectories(textBoxForPath.Text);
                listOfDirectories = folderList.ToList();
                workersList.Add(bgWorker);
                textBoxForResult.AppendText("1 worker\r\n");
                watch.Restart();
                workersList[0].RunWorkerAsync();
            }
        }

        public List<string> GetAllDirectories(string path)
        {
            List<string> listOfDirectories;
            listOfDirectories = new List<string>();

            listOfDirectories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories).ToList();
            listOfDirectories.Add(path);

            return listOfDirectories;
        }

        public string ConvertSize(double totalSize)
        {
            double byteEquivalent;
            string lengthType;
            double megaByte;
            double gigaByte;

            byteEquivalent = 1024;
            megaByte = Math.Pow(byteEquivalent, 2);
            gigaByte = Math.Pow(byteEquivalent, 3);

            if (totalSize >= gigaByte)
            {
                totalSize = totalSize / gigaByte;
                lengthType = totalSize.ToString("F") + " GB";
            }
            else if (totalSize >= megaByte)
            {
                totalSize = totalSize / megaByte;
                lengthType = totalSize.ToString("F") + " MB";

            }
            else if (totalSize >= byteEquivalent)
            {
                totalSize = totalSize / byteEquivalent;
                lengthType = totalSize.ToString("F") + " KB";
            }
            else
            {
                lengthType = totalSize.ToString() + " bytes";
            }
            return lengthType;
        }


        public string GetAllExtentionsOfFiles(string directory)
        {
            List<string> FolderInfo;
            List<string> ListOfExtensions;
            string[] fileEntries;

            FolderInfo = new List<string>();
            ListOfExtensions = new List<string>();
            long size = 0;

            fileEntries = Directory.GetFiles(directory);
            ListOfExtensions.Sort();

            if (fileEntries.Length == 0)
            {
                return string.Format("Folder - {0}\r\nEmpty\r\n", directory);
            }
            else
            {
                for (int i = 0; i < fileEntries.Length; i++)
                {
                    size += new FileInfo(fileEntries[i]).Length;
                }
                return string.Format("Folder - {0}\r\nNumber of files - {1}, total size - {2}\r\n", directory, fileEntries.Length, ConvertSize((double)size));
            }
        }

        private void backgroundWorkerFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            string folderPath;
            List<string> ListToReportExtentions = new List<string>();

            BackgroundWorker worker = sender as BackgroundWorker;

            while (folderList.Count > 0)
            {
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
                lock (folderList)
                {
                    folderPath = folderList[0];
                    folderList.RemoveAt(0);
                }
                (sender as BackgroundWorker).ReportProgress(0, GetAllExtentionsOfFiles(folderPath));
            }
        }

        private void backgroundWorkerFiles_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string line;
            lock (locker)
            {
                line = (string)e.UserState;

                textBoxForResult.AppendText(line + Environment.NewLine);
            }
        }

        private void backgroundWorkerFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                textBoxSummary.Text = "Canceled!";
                buttonGetInfo.Enabled = true;
                buttonCancel.Enabled = false;
                buttonBrowseFolder.Enabled = true;

            }
            else
            {
                bool StopWatchCount;
                int AmountOfBG;
                StopWatchCount = true;

                foreach (BackgroundWorker BG in workersList)
                {
                    if (BG.IsBusy)
                    {
                        StopWatchCount = false;
                        break;
                    }
                }

                if (StopWatchCount)
                {
                    watch.Stop();
                    var ts = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
                    execusionTimeList.Add(ts);
                    timeDifference = 1;

                    if (execusionTimeList.Count > 1)
                    {
                        timeDifference = TimeSpan.Compare(execusionTimeList[execusionTimeList.Count - 2], execusionTimeList[execusionTimeList.Count - 1]);
                    }

                    if (timeDifference == 1)
                    {
                        folderList = listOfDirectories.ToList();
                        BackgroundWorker NewBGWorker = new BackgroundWorker();
                        workersList.Add(NewBGWorker);
                        textBoxForResult.AppendText(string.Format("{0} workers\r\n", workersList.Count));
                        InitializeBackgroundWorker(NewBGWorker);
                        watch.Restart();

                        for (int a = 0; a < workersList.Count; a++)
                        {
                            workersList[a].RunWorkerAsync();
                        }
                    }
                    else
                    {
                        if (workersList.Count > 1)
                        {
                            AmountOfBG = workersList.Count - 1;

                            for (int i = 0; i < execusionTimeList.Count; i++)
                            {
                                textBoxSummary.AppendText(String.Format("Time for {0} BG - {1:D2}m:{2:D2}s:{3:D2}ms\r\n", (i + 1), execusionTimeList[i].Minutes, execusionTimeList[i].Seconds, execusionTimeList[i].Milliseconds));
                            }

                            textBoxSummary.AppendText("Optimal amount of BG -" + (AmountOfBG.ToString()) + Environment.NewLine);
                        }
                        else
                        {
                            textBoxSummary.AppendText("Optimal amount of BG is 1. " + Environment.NewLine);
                        }
                        buttonGetInfo.Enabled = true;
                        buttonCancel.Enabled = false;
                        buttonBrowseFolder.Enabled = true;
                    }
                }
            }
        }

        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = textBoxForPath.Text;
                folderDialog.Description = "Please select a folder";
                folderDialog.ShowNewFolderButton = false;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxForPath.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void OnCancelButtonClick(object sender, EventArgs e)
        {
            foreach (var worker in workersList)
            {
                worker.CancelAsync();
            }
        }
    }
}



