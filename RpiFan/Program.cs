using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace RpiFan
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}] Starting fan driver v0.1 ...");
            var pin = 18;

            var targetHi = 60;
            var targetLow = 40;
            var fanStarted = false;

            var p = new OptionSet()
            {
                { "p|pin=", "the input {PIN} number on RPi", (int v) => pin= v }
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("greet: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `greet --help' for more information.");
                return;
            }

            Console.WriteLine($"GPIO: {pin}");

            using (var controller = new GpioController())
            {
                controller.OpenPin(pin, PinMode.Output);

                controller.Write(pin, PinValue.Low);

                while (true)
                {
                    var temperature = GetTemp();

                    if (temperature >= targetHi && !fanStarted)
                    {
                        fanStarted = true;
                        controller.Write(pin, PinValue.High);
                        Console.WriteLine($"[{DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}] Temperature is [{temperature}°] setting PIN {pin} to HI");
                    }
                    else if (temperature <= targetLow && fanStarted)
                    {
                        fanStarted = false;
                        controller.Write(pin, PinValue.Low);
                        Console.WriteLine($"[{DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}] Temperature is [{temperature}°] setting PIN {pin} to LOW");
                    }

                    Thread.Sleep(1000);
                    
                }
            }
        }

        internal static float GetTemp()
        {

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"/opt/vc/bin/vcgencmd measure_temp\"",
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }
            };
            process.Start();

            var result = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            var tempResult = result.Split("=")[1].Split("'")[0];

            if (float.TryParse(tempResult, out float temperature))
                return temperature;
            else
                return 0.0f;
        }
    }
}
