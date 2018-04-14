using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace hosts.Common
{
    public static class FileSystem
    {
        public static Task<string[]> GetLinesAsync(FileInfo file) => GetLinesAsync(file, FileMode.Open);

        public static Task<string[]> GetLinesAsync(FileInfo file, FileMode mode)
        {
            if (file == null) { throw new ArgumentNullException(nameof(file)); }

            return GetLinesAsyncImpl(file, mode);
        }

        private static async Task<string[]> GetLinesAsyncImpl(FileInfo file, FileMode mode)
        {
            var lines = new List<string>();

            var fsAsync = new FileStream(
                file.FullName,
                mode,
                FileAccess.Read,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            using (StreamReader sr = new StreamReader(fsAsync))
            {
                fsAsync = null;

                string line = string.Empty;

                while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    lines.Add(line);
                }
            }

            fsAsync?.Dispose();

            return lines.ToArray();
        }


        public static Task WriteLinesAsync(FileInfo file, IEnumerable<string> lines)
            => WriteLinesAsync(file, lines, FileMode.OpenOrCreate);

        public static Task WriteLinesAsync(FileInfo file, IEnumerable<string> lines, FileMode mode)
        {
            if (file == null) { throw new ArgumentNullException(nameof(file)); }
            if (!lines.Any()) { return Task.CompletedTask; }

            return WriteLinesAsyncImpl(file, lines, mode);
        }

        private static async Task WriteLinesAsyncImpl(FileInfo file, IEnumerable<string> lines, FileMode mode)
        {
            FileStream fsAsync = new FileStream(
                file.FullName,
                mode,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous);

            using (StreamWriter sw = new StreamWriter(fsAsync))
            {
                fsAsync = null;

                foreach (string line in lines)
                {
                    await sw.WriteLineAsync(line).ConfigureAwait(false);
                }

                await sw.FlushAsync().ConfigureAwait(false);
            }

            fsAsync?.Dispose();
        }
    }
}
