using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Newtonsoft.Json;
using Server;
using Server.Models;

TcpListener listener = new TcpListener(IPAddress.Any, 8080);
listener.Start();

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();

    await Task.Run(() =>
    {
        using (NetworkStream stream = client.GetStream())
        {
            byte[] buffer = new byte[8192];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var ilMessage = JsonConvert.DeserializeObject<ILMessage>(message);
            if (ilMessage == null)
                return;

            var method = new DynamicMethod(
                $"_eventMethod{ilMessage.Event.ToString()}",
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                typeof(void),
                new[] { typeof(MyEventArgs) },
                typeof(Program).Module,
                true);

            var ilGenerator = method.GetILGenerator(ilMessage.Instructions.Length);
            foreach (var localVar in ilMessage.LocalsVariables.OrderBy(x => x.Index))
            {
                ilGenerator.DeclareLocal(localVar.Type.ResolveType(), localVar.IsPinned);
            }

            ilMessage.EmitTo(ilGenerator, method.Module);

            var eventMethod = (Action<MyEventArgs>)method.CreateDelegate(typeof(Action<MyEventArgs>));
            var ev = new MyEventArgs();

            try
            {
                Console.WriteLine($"Pre EventMethod: {ev.Damage}");
                eventMethod(ev);
                Console.WriteLine($"Post EventMethod: {ev.Damage}");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ResetColor();
            }
        }
    });

    Console.WriteLine("Waiting for clients...");
}