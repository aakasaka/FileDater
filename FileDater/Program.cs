using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FileDater
{
    class Program
    {
        static void Main(string[] args)
        {
            var results = args.Select(a => rename(a)).ToArray();

            if (results.All(a => a.ResultKind == RenameResult.Kind.Success)) return;

            var msgs = results.Where(r => !string.IsNullOrEmpty(r.Message))
                              .Select(r => string.Format("{0}{1} {2}",
                                                        r.ResultKind == RenameResult.Kind.Error ? "▲" : "△",
                                                        r.File,
                                                        r.Message));

            Console.WriteLine(string.Join(Environment.NewLine, msgs));
            Console.ReadLine();
        }

        private static RenameResult rename(string file)
        {
            try
            {
                file = Path.GetFullPath(file);

                if (!File.Exists(file))
                {
                    return new RenameResult(file, RenameResult.Kind.Error, "見つかりません");
                }

                var newPath = createNewFilePath(file);
                if (newPath == null) return new RenameResult(file, RenameResult.Kind.Error, "生成可能な追番の上限を超えています");

                File.Move(file, newPath);
                return new RenameResult(file, RenameResult.Kind.Success);
            }
            catch(Exception ex)
            {
                if (ex is SecurityException
                    || ex is IOException
                    || ex is UnauthorizedAccessException
                    || ex is ArgumentException
                    || ex is PathTooLongException
                    || ex is NotSupportedException)
                {
                    return new RenameResult(file, RenameResult.Kind.Error, ex.Message);
                }
                return new RenameResult(file, RenameResult.Kind.Error, ex.ToString());
            }
        }

        private static string createNewFilePath(string baseFile)
        {
            var fileDate = File.GetLastWriteTime(baseFile);
            var dateStr = fileDate.ToString("yyyyMMdd");

            var baseFileName = Path.GetFileNameWithoutExtension(baseFile);
            var ext = Path.GetExtension(baseFile);
            var dir = Path.GetDirectoryName(baseFile);

            var newPathNoSuffix = Path.Combine(dir, string.Format("{0}_{1}{2}", baseFileName, dateStr, ext));

            if (!File.Exists(newPathNoSuffix)) return newPathNoSuffix;

            var suffix = 1;
            while (suffix <= 99999)
            {
                var path = Path.Combine(dir, string.Format("{0}_{1}_{2}{3}", baseFileName, dateStr, suffix, ext));
                if (!File.Exists(path)) return path;

                suffix++;
            }

            return null;
        }
    }

    class RenameResult
    {
        public enum Kind
        {
            Success,
            Warning,
            Error,
        }
        public string File { get; private set; }
        public Kind ResultKind { get; private set; }
        public string Message { get; private set; }

        public RenameResult(string file, Kind kind, string message = null)
        {
            this.File = file;
            this.ResultKind = kind;
            this.Message = message;
        }
    }
}
