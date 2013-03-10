﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Xunit;

namespace Selenite.Client.ViewModels.WebAutomation
{
    public class ResultsViewModel : ViewModelBase, ITestMethodRunnerCallback
    {
        private bool _isRunning = false;

        public ResultsViewModel()
        {
            UseFirefox = true;

            RunTestsCommand = new RelayCommand(RunTests, t => UseAny && !_isRunning);
        }

        public bool UseFirefox
        {
            get { return Get(() => UseFirefox); }
            set { Set(value, () => UseFirefox); }
        }

        public bool UseChrome
        {
            get { return Get(() => UseChrome); }
            set { Set(value, () => UseChrome); }
        }

        public bool UseInternetExplorer
        {
            get { return Get(() => UseInternetExplorer); }
            set { Set(value, () => UseInternetExplorer); }
        }

        private bool UseAny
        {
            get { return UseFirefox || UseChrome || UseInternetExplorer; }
        }

        public string ResultText
        {
            get { return Get(() => ResultText); }
            set { Set(value, () => ResultText); }
        }

        public ICommand RunTestsCommand { get; set; }

        private void RunTests(object parameter)
        {
            _isRunning = true;
            ResultText = string.Empty;
            Task.Factory.StartNew(DoRunTests);
        }

        public void DoRunTests()
        {
            var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();

            try
            {
                using (var wrapper = new ExecutorWrapper(executingAssembly.Location, null, true))
                {
                    var assembly = TestAssemblyBuilder.Build(wrapper);
                    var methods = assembly
                        .EnumerateTestMethods()
                        .ToList();

                    var activeMethods = methods
                        .Where(m =>
                               UseFirefox && m.TestClass.TypeName.Contains("Firefox") ||
                               UseChrome && m.TestClass.TypeName.Contains("Chrome") ||
                               UseInternetExplorer && m.TestClass.TypeName.Contains("Internet")
                        )
                        .ToList();

                    if (activeMethods.Count > 0)
                        assembly.Run(activeMethods, this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                Application.Current.Dispatcher.BeginInvoke(new DoDoneDelegate(DoDone));
            }
        }

        delegate void DoDoneDelegate();
        public void DoDone()
        {
            _isRunning = false;
        }

        public void AssemblyFinished(TestAssembly testAssembly, int total, int failed, int skipped, double time)
        {
            Application.Current.Dispatcher.BeginInvoke(new DoStatsDelegate(DoStats), total, failed, skipped, time);
        }

        private delegate void DoStatsDelegate(int total, int failed, int skipped, double time);
        public void DoStats(int total, int failed, int skipped, double time)
        {
            ResultText += String.Format("{0} of {1} Succeeded, {2} Failed, {3} Skipped", total - failed - skipped, total, failed, skipped);
            ResultText += Environment.NewLine;
            ResultText += String.Format("Ellapsed Time: {0} seconds", time);

            ScrollToBottom();
        }

        public void AssemblyStart(TestAssembly testAssembly)
        {
        }

        public bool ClassFailed(TestClass testClass, string exceptionType, string message, string stackTrace)
        {
            return true;
        }

        public void ExceptionThrown(TestAssembly testAssembly, Exception exception)
        {
            Application.Current.Dispatcher.BeginInvoke(new DoExceptionThrownDelegate(DoExceptionThrown), testAssembly, exception);
        }

        delegate void DoExceptionThrownDelegate(TestAssembly testAssembly, Exception exception);
        public void DoExceptionThrown(TestAssembly testAssembly, Exception exception)
        {
            ResultText += exception + Environment.NewLine + Environment.NewLine;

            ScrollToBottom();
        }

        public bool TestFinished(TestMethod testMethod)
        {
            Application.Current.Dispatcher.BeginInvoke(new DoProcessTestDelegate(DoProcessTest), testMethod);
            return true;
        }

        public bool TestStart(TestMethod testMethod)
        {
            return true;
        }

        delegate void DoProcessTestDelegate(TestMethod testMethod);
        private void DoProcessTest(TestMethod testMethod)
        {
            var lastRunResult = testMethod.RunResults.Last();

            if (lastRunResult is TestPassedResult)
                TestPassed(lastRunResult as TestPassedResult);
            else if (lastRunResult is TestFailedResult)
                TestFailed(lastRunResult as TestFailedResult);
            else
                TestSkipped(lastRunResult as TestSkippedResult);
        }

        private void TestFailed(TestFailedResult testResult)
        {
            string result = testResult.DisplayName + " : " + testResult.ExceptionMessage + Environment.NewLine;

            if (!string.IsNullOrEmpty(testResult.ExceptionStackTrace))
                result += "Stack Trace:" + Environment.NewLine + testResult.ExceptionStackTrace + Environment.NewLine;

            if (!string.IsNullOrEmpty(testResult.Output))
                result += "Output:" + Environment.NewLine + FormatOutput(testResult.Output);

            ResultText += result + Environment.NewLine;

            ScrollToBottom();
        }

        private void TestPassed(TestPassedResult testResult)
        {
            if (string.IsNullOrEmpty(testResult.Output))
                return;

            ResultText += "Output from " + testResult.DisplayName + ":" + Environment.NewLine + FormatOutput(testResult.Output) + Environment.NewLine;

             ScrollToBottom();
        }

        private void TestSkipped(TestSkippedResult testResult)
        {
            ResultText += testResult.DisplayName + " : " + testResult.Reason + Environment.NewLine + Environment.NewLine;

            ScrollToBottom();
        }

        private string FormatOutput(string output)
        {
            string result = "";
            string trimmedOutput = output.Trim().Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");

            foreach (string line in trimmedOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                result += String.Format("  {0}", line) + Environment.NewLine;

            return result;
        }

        private void ScrollToBottom()
        {
            
        }
    }
}