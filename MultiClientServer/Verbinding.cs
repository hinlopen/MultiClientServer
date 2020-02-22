using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace MultiClientServer {

public class Verbinding {

public StreamReader Read;
public StreamWriter Write;
int verbonden_knoop;

// Deze constructor wordt gebruikt als wij CLIENT worden bij een andere SERVER
public Verbinding (int buur_poort) 
{
    TcpClient client;
    bool verbonden = false;
    while (!verbonden)
    {
        try
        {
            client = new TcpClient("localhost", buur_poort);
            Read = new StreamReader(client.GetStream());
            Write = new StreamWriter(client.GetStream());
            Write.AutoFlush = true;
            Write.WriteLine("K: " + Program.knoop.u);
            Console.WriteLine("Verbonden: " + buur_poort);

            verbonden_knoop = buur_poort;
            verbonden = true;
        }
        catch { Thread.Sleep(50); }
    }

    new Thread(Lees).Start();
}

// Deze constructor wordt gebruikt als wij SERVER zijn en een CLIENT maakt met ons verbinding
public Verbinding (int buur_poort, StreamReader read, StreamWriter write) 
{
    verbonden_knoop = buur_poort;
    Read = read; 
    Write = write;

    new Thread(Lees).Start();
}

public void Lees () 
{
    try {
        while (true) 
        {
            if (!Program.knoop.Klaar) 
            {
                Thread.Sleep(50);
                continue;
            }
            
            string invoer  = Read.ReadLine();
            lock(Program.knoop.lock_) {
                string[] delen = invoer.Split(new char[] { ' ' }, 4);
                int doel_poort = int.Parse(delen[1]);

                switch (invoer[0]) {
                    case 'M': Program.knoop.Bijwerken(verbonden_knoop, doel_poort, int.Parse(delen[2])); break;
                    case 'B': Program.knoop.BerichtAfhandelen(doel_poort, invoer);      break;
                    case 'D': Program.knoop.VerwerkOntbinding(doel_poort);                        break;
                    default : Console.WriteLine("//Onbekende opdracht: " + invoer);     break;
                }
            }
        }
    }
    catch { } // Verbinding is kennelijk verbroken
}

}
}
