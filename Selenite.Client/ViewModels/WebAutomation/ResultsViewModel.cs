﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Selenite.Models;
using Xunit;

namespace Selenite.Client.ViewModels.WebAutomation
{
    public class ResultsViewModel : ViewModelBase, ITestMethodRunnerCallback
    {
        public ObservableCollection<TestResultCollectionViewModel> TestResults { get; set; }

        public int SucceededTests
        {
            get { return Get(() => SucceededTests); }
            set { Set(value, () => SucceededTests); }
        }

        public int FailedTests
        {
            get { return Get(() => FailedTests); }
            set { Set(value, () => FailedTests); }
        }

        public int SkippedTests
        {
            get { return Get(() => SkippedTests); }
            set { Set(value, () => SkippedTests); }
        }

        public double TimeElapsed
        {
            get { return Get(() => TimeElapsed); }
            set { Set(value, () => TimeElapsed); }
        }

        private bool _isRunning;

        public ResultsViewModel()
        {
            UseFirefox = true;

            TestResults = new ObservableCollection<TestResultCollectionViewModel>();

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

        public ICommand RunTestsCommand { get; set; }

        private void RunTests(object parameter)
        {
            _isRunning = true;
            TestResults.Clear();
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
                Application.Current.Dispatcher.BeginInvoke((Action)(DoDone));
            }
        }

        public void DoDone()
        {
            _isRunning = false;
        }

        public void AssemblyFinished(TestAssembly testAssembly, int total, int failed, int skipped, double time)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    SucceededTests = total - failed - skipped;
                    FailedTests = failed;
                    SkippedTests = skipped;
                    TimeElapsed = time;
                }));
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
            Application.Current.Dispatcher.BeginInvoke((Action)(() => DoExceptionThrown(testAssembly, exception)));
        }

        public void DoExceptionThrown(TestAssembly testAssembly, Exception exception)
        {
            var testResultViewModel = new TestResultViewModel
                {
                    Status = ResultStatus.Failed,
                    Name = "Test Runner Error",
                    StackTrace = exception.StackTrace,
                    ResultOutput = exception.Message,
                };

            AddResult("Test Failure", testResultViewModel);
        }

        public bool TestFinished(TestMethod testMethod)
        {
            Application.Current.Dispatcher.BeginInvoke((Action) (() => DoProcessTest(testMethod)));
            return true;
        }

        public bool TestStart(TestMethod testMethod)
        {
            return true;
        }

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
            var result = JsonConvert.DeserializeObject<Models.TestResult>(testResult.Output);
            var testResultViewModel = new TestResultViewModel
                {
                    Status = result.Status,
                    Name = result.TestName,
                    ResultOutput = result.TraceResult,
                    StackTrace = testResult.ExceptionStackTrace
                };

            var collectionName = result.CollectionName;

            AddResult(collectionName, testResultViewModel);
        }

        private void AddResult(string collectionName, TestResultViewModel testResultViewModel)
        {
            var collection = TestResults.FirstOrDefault(c => c.Name == collectionName);

            if (collection != null)
            {
                collection.TestResults.Add(testResultViewModel);
            }
            else
            {
                TestResults.Add(new TestResultCollectionViewModel
                    {
                        Name = collectionName,
                        TestResults = new ObservableCollection<TestResultViewModel> {testResultViewModel}
                    });
            }
        }

        private void TestPassed(TestPassedResult testResult)
        {
            var result = JsonConvert.DeserializeObject<Models.TestResult>(testResult.Output);
            var testResultViewModel = new TestResultViewModel
                {
                    Status = result.Status,
                    Name = result.TestName,
                    ResultOutput = result.TraceResult,
                };

            var collectionName = result.CollectionName;

            AddResult(collectionName, testResultViewModel);
        }

        private void TestSkipped(TestSkippedResult testResult)
        {
            // CCHINN: Add support for skipped tests.
            //var result = JsonConvert.DeserializeObject<Models.TestResult>(testResult.Output);
            //var testResultViewModel = new TestResultViewModel
            //{
            //    Status = result.Status,
            //    Name = result.Test.Name,
            //    ResultOutput = testResult.Reason,
            //};

            //var collectionName = result.Test.CollectionName;

            //AddResult(collectionName, testResultViewModel);
        }
    }
}