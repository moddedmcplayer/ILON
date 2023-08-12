using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Server;

namespace Client;

using System.Diagnostics;
using Server.Models;

public class Program
{
    public static async Task Main(string[] args)
    {
        TcpClient client = new TcpClient();
        await client.ConnectAsync("localhost", 8080);

        using (NetworkStream stream = client.GetStream())
        {
            var method = typeof(Program).GetMethod(nameof(EventMethod), BindingFlags.Public | BindingFlags.Static)!;
            byte[] buffer = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(new ILMessage(Event.Example, method))
                );

            await stream.WriteAsync(buffer);
        }
    }

    public static void EventMethod(MyEventArgs ev)
    {
        ev.Damage = 1000;
        var local = 0;
        local++;
        Console.WriteLine($"call test {local}");
        var stopwatch = Stopwatch.StartNew();
        var generic = new List<string>();
        generic.Add("generic 1");
        generic.Add("generic 2");
        Console.WriteLine(generic.ElementAt(0));
        for (int i = 0; i < 1000; i++)
        {
            ev.Message += i;
        }
        Console.WriteLine("Elapsed: " + stopwatch.ElapsedMilliseconds);
        Console.WriteLine("Result: " + ev.Message);
    }
}