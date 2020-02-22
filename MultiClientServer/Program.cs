using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace MultiClientServer {
class Program {

public static Knoop knoop;

static void Main(string[] args) {
    int de_poort = int.Parse(args[0]);
    Console.Title = "NetChange " + de_poort;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("// " + de_poort);
    Console.ForegroundColor = ConsoleColor.White;

    knoop = new Knoop(de_poort);

    for (int i = 1; i < args.Length; i++) 
    {
        int poort = int.Parse(args[i]);
        if (poort > de_poort) continue;
        knoop.Verbind2(poort, new Verbinding(poort));
    }

    knoop.Initialiseer();

    new Thread(VerwerkOpdrachten).Start();

    bool opgestart = false; // Als de socket al in gebruik is, proberen we het later nogmaals
    while (!opgestart)
    {
        try {
            TcpListener server = new TcpListener(IPAddress.Any, de_poort);
            server.Start();
            new Thread(() => AcceptLoop(server)).Start();
        }
        catch { Thread.Sleep(500); }
    }
}

static private void VerwerkOpdrachten()
{
    while (true)
    {
        string invoer = Console.ReadLine();
        char opdracht = invoer[0];

        lock(knoop.lock_)
        {
            string[] invoers = invoer.Split(new char[] { ' ' }, 3);

            int p = 0;
            if (invoers.Length > 1)
                p = int.Parse(invoers[1]);

            switch (opdracht)
            {
                case 'B': knoop.BerichtAfhandelen(p, invoer);  break;
                case 'C': knoop.Verbind(p, new Verbinding(p)); break;
                case 'D': knoop.Ontbind(p);                    break;
                case 'R': knoop.PrintRouteTabel();             break;
                case 'L': knoop.PrintLog();                    break;

                default : knoop.Log("Onbekende opdracht");     break;
            }

            Thread.Sleep(30);                
        }
    }
}

static private void AcceptLoop(TcpListener handle) 
{
    while (true) 
    {
        // Luister naar berichten van processen die verbinding willen maken, kopieer hun stream
        TcpClient client = handle.AcceptTcpClient();
        StreamReader clientIn  = new StreamReader(client.GetStream());
        StreamWriter clientOut = new StreamWriter(client.GetStream());
        clientOut.AutoFlush = true;

        string[] delen = clientIn.ReadLine().Split();
        if (delen[0] != "K:") continue;
        int zijnPoort = int.Parse(delen[1]);
        lock(knoop.lock_) knoop.Verbind(zijnPoort, new Verbinding(zijnPoort, clientIn, clientOut));
    }
}

}
}