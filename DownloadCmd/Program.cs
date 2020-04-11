using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DownloadCmd
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var batchNotFound = new Exception("Batch file not found.");

                var stackItems = new Stack<string>(args.Reverse());
                var batchFileScan = Directory.GetFiles(Environment.CurrentDirectory, "*.txt", SearchOption.TopDirectoryOnly).FirstOrDefault();
                var batch = Path.GetFullPath(stackItems.Any() ? stackItems.Pop() : (batchFileScan ?? throw batchNotFound));
                var outputFolder = Path.GetFullPath(stackItems.Any() ? stackItems.Pop() : Environment.CurrentDirectory);
                var MaxDegreeOfParallelism = Convert.ToInt32(stackItems.Any() ? stackItems.Pop() : "7");
                var skippExistFile = Convert.ToBoolean(stackItems.Any() ? stackItems.Pop() : "true");

                Console.WriteLine("Input Url File: " + batch);
                Console.WriteLine("Output Folder: " + outputFolder);
                Console.WriteLine("Max Degree Of Parallelism: " + MaxDegreeOfParallelism);

                Console.WriteLine("Key Y or Enter to continue, other to exit");
                var inputKey = Console.ReadLine();
                var allowContinue = inputKey.ToUpper() == "Y" || inputKey == string.Empty;
                if (!allowContinue)
                {
                    Console.WriteLine("Exit the program");
                    return;
                }
                if (!File.Exists(batch))
                {
                    throw batchNotFound;
                }

                if (!Directory.Exists(outputFolder))
                {
                    Console.WriteLine("Create the new Directory: " + outputFolder);
                    Directory.CreateDirectory(outputFolder);
                }

                Directory.GetFiles(outputFolder).Where(x => new FileInfo(x).Length == 0).ToList().ForEach(x =>
                {
                    File.Delete(x);
                    Console.WriteLine(x + "\t\tDELETED.");
                });

                var urls = File.ReadAllLines(batch).Where(x => !string.IsNullOrWhiteSpace(x));
                Console.WriteLine("Total number of url: " + urls.Count());

                Parallel.ForEach(urls, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, x =>
                {
                    var fileName = GetFileNameFromUrl(x);
                    var time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");

                    var destFile = Path.Combine(outputFolder, fileName);
                    if (skippExistFile && File.Exists(destFile))
                    {
                        Console.WriteLine(time + "\t\t" + x + "\t SKIP...");
                        return;
                    }
                    using (var client = new WebClient())
                    {
                        try
                        {
                            var start = DateTime.Now;
                            // client.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadDataCompleted);
                            Console.WriteLine(time + "\t\t" + x + "\t DOWNLOADING...");
                            client.DownloadFile(x, destFile);
                            Console.WriteLine(time + "\t\t" + x + "\t COMPLETED\t\t" + (DateTime.Now - start).ToString("c"));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(time + "\t\t" + x + "\t FAIL: " + ex.Message);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Programm error. Exit now");
            }
        }

        private static void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var client = (WebClient)sender;

            Console.WriteLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + "\t" + client.BaseAddress + "\t\t" + e.Result.Length + "byte\t COMPLETED");
        }


        readonly static Uri SomeBaseUri = new Uri("http://canbeanything");

        static string GetFileNameFromUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(SomeBaseUri, url);

            var fileName = Path.GetFileName(uri.LocalPath);

            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
            {
                return string.Join("_", url.Split(Path.GetInvalidFileNameChars())) + ".html";
            }

            return fileName;
        }
    }
}
