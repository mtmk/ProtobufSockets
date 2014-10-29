using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace ProtobufSockets.Tests
{
    public class XunitLogger : IRunnerLogger
    {
        private readonly Stopwatch _sw = new Stopwatch();
        private readonly TextWriter _textWriter;
        public int Total = 0;
        public int Failed = 0;
        public int Skipped = 0;
        public double Time = 0.0;
        
        public XunitLogger(TextWriter textWriter)
        {
            _textWriter = textWriter;
        }

        public void AssemblyFinished(string assemblyFilename, int total, int failed, int skipped, double time)
        {
            _textWriter.WriteLine("Tests: {0}, Failures: {1}, Skipped: {2}, Time: {3} seconds",
                total,
                failed,
                skipped,
                time.ToString("0.000"));

            Total += total;
            Failed += failed;
            Skipped += skipped;
            Time += time;
        }

        public void AssemblyStart(string assemblyFilename, string configFilename, string xUnitVersion)
        {
            _textWriter.WriteLine("Started: {0}", assemblyFilename);
        }

        public bool ClassFailed(string className, string exceptionType, string message, string stackTrace)
        {
            _textWriter.WriteLine("[CLASS] {0}: {1}", className, Escape(message));
            _textWriter.WriteLine(Escape(stackTrace));
            return true;
        }

        public void ExceptionThrown(string assemblyFilename, Exception exception)
        {
            _textWriter.WriteLine(exception.Message);
            _textWriter.WriteLine("While running: {0}", assemblyFilename);
        }

        public void TestFailed(string name, string type, string method, double duration, string output, string exceptionType, string message, string stackTrace)
        {
            _textWriter.WriteLine("{0}: {1}", name, Escape(message));
            _textWriter.WriteLine(Escape(stackTrace));
            WriteOutput(output);
        }

        public bool TestFinished(string name, string type, string method)
        {
            _sw.Stop();
            _textWriter.WriteLine("  took: {0:0.000}s", _sw.Elapsed.TotalSeconds);
            return true;
        }

        public virtual void TestPassed(string name, string type, string method, double duration, string output)
        {
            _textWriter.WriteLine("Running test: {0}", name);
            WriteOutput(output);
        }

        public void TestSkipped(string name, string type, string method, string reason)
        {
            _textWriter.WriteLine("Skipped: {0}: {1}", name, Escape(reason));
        }

        public virtual bool TestStart(string name, string type, string method)
        {
            _sw.Restart();
            return true;
        }

        static string Escape(string value)
        {
            if (value == null)
                return string.Empty;

            return value.Replace(Environment.NewLine, "\n");
        }

        protected void WriteOutput(string output)
        {
            if (output != null)
            {
                _textWriter.WriteLine("    Captured output:");
                foreach (string line in output.Trim().Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                    _textWriter.WriteLine("      {0}", line);
            }
        }
    }
}