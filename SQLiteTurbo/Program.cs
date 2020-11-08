using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using log4net;
using log4net.Config;
using System.Drawing;

// Configure log4net using the .config file
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace SQLiteTurbo
{
    class ToolStripRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            // Suppress pressedness visualization while doing PerformClick()
            if (e.Item.Pressed && !e.Item.Selected)
                return;
            base.OnRenderButtonBackground(e);
        }
    }

    static class Program
    {
        static Program()
        {
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; // real numbers using "." decimal separator
            Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture; //.net exceptions in English
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            // Configure log4net
            BasicConfigurator.Configure();
            var arguments = new ArrayList(args);
            int i;
            if ((i = arguments.IndexOf("/About")) != -1)
            {
                arguments.RemoveAt(i);
                var dlg = new AboutDialog();
                if (i < arguments.Count)
                    dlg.Text = (string)arguments[i];
                dlg.ShowDialog();
                return 0;
            }
            var mainForm = new MainForm();
            DialogResult result = mainForm.ParseArguments(arguments);
            if (mainForm.FormBorderStyle == FormBorderStyle.None)
            {
                if (result != DialogResult.Cancel)
                    mainForm.Show();
                return (int)result;
            }

            try
            {
                _mutex = Mutex.OpenExisting("SQLiteCompare");
                MessageBox.Show("Another instance of SQLiteCompare is already active.\r\n" +
                    "Please close it first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
            }
            catch (Exception ex)
            {
                _mutex = new Mutex(false, "SQLiteCompare");
            }

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            // Issue a log message that contains the version of the application.
            _log.Info("===========================================================================");
            _log.Info(" SQLite Compare [" + mainForm.ProductVersion + "]");
            _log.Info("===========================================================================");

            // Remove any stale table change files
            TableChanges.RemoveStaleChangeFiles();

            try
            {
                _mainForm = mainForm;
                Application.Run(_mainForm);
                _mainForm = null;
            }
            catch (Exception ex)
            {
                _mainForm = null;
                _log.Error("Got exception from main loop", ex);
                ShowUnexpectedErrorDialog(ex);
            }
            finally
            {
                // Remove all active change files
                TableChanges.RemoveActiveChangeFiles();
            } // finally
            return 0;
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            _log.Error("thread exception", e.Exception);

            // Show error dialog
            ShowUnexpectedErrorDialog(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _log.Error("Unhandled exception ("+(e.IsTerminating?"terminating":"non-terminating")+")", 
                (Exception)e.ExceptionObject);

            ShowUnexpectedErrorDialog((Exception)e.ExceptionObject);
        }

        private static void ShowUnexpectedErrorDialog(Exception error)
        {
            // Prevent multiple unexpected-error-dialogs
            lock (typeof(Program))
            {
                if (_mainForm != null)
                {
                    if (_mainForm.InvokeRequired)
                    {
                        _mainForm.Invoke(new MethodInvoker(delegate
                        {
                            UnexpectedErrorDialog dlg = new UnexpectedErrorDialog();
                            dlg.Error = error;
                            dlg.ShowDialog(_mainForm);
                        }));
                    }
                    else
                    {
                        UnexpectedErrorDialog dlg = new UnexpectedErrorDialog();
                        dlg.Error = error;
                        dlg.ShowDialog(_mainForm);
                    } // else
                }
                else
                {
                    UnexpectedErrorDialog dlg = new UnexpectedErrorDialog();
                    dlg.Error = error;
                    Application.Run(dlg);
                } // else

                Environment.Exit(1);
            } // lock
        }

        private static Mutex _mutex = null;
        private static MainForm _mainForm = null;
        private static ILog _log = LogManager.GetLogger("Program");
    }
}