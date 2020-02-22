using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MultiClientServer {

public class Knoop {

public int u;
int N;
List<int>                       Knopen; // Alle bekende knopen
Dictionary<int, Verbinding>     Buren;  // Knopen waarmee wij een directe verbinding hebben
Dictionary<int, int>            D;      // Afstanden van u naar v, v uit Knopen
Dictionary<int, int>            Nb;     // Buur w op de route naar knoop v
Dictionary<Tuple<int,int>, int> ndis;   // Geschatte afstand van v naar w, d

Dictionary<int, object>         Sloten;
public object lock_ = new object();

const int udef = -7;
public bool Klaar = false;

public Knoop (int poort)
{
    u = poort;
    N = 20;
    Knopen = new List<int>();
    Buren  = new Dictionary<int, Verbinding>();
    D      = new Dictionary<int, int>();
    Nb     = new Dictionary<int, int>();
    Sloten = new Dictionary<int, object>();  
    ndis   = new Dictionary<Tuple<int, int>, int>();

    Knopen.Add(u);
}

public void Initialiseer()
{
    foreach (int v in Knopen)
        InitialiseerKnoop(v);
    
    D[u]  = 0;
    Nb[u] = u;

    foreach(int w in Buren.Keys)
        Buren[w].Write.WriteLine("M {0} 0", u);

    Klaar = true;
}

public void InitialiseerKnoop(int v)
{
    N = Knopen.Count;

    foreach(int w in Buren.Keys)
        ndis[Tuple.Create(w, v)] = N;

    D[v]  = N;
    Nb[v] = udef;
}


// Een andere knoop heeft een afstandsschatting gewijzigd
public void Bijwerken(int w, int v, int d)
{
    if (!Knopen.Contains(v))
    {
        Knopen.Add(v);
        InitialiseerKnoop(v);
    }

    ndis[Tuple.Create(w, v)] = d;
    ndis[Tuple.Create(v, w)] = d;

    Herbereken(v);
}

// Bereken de beste knoop om naar v te komen
void Herbereken(int v)
{
    int oud = D[v];

    if (v == u)
    {
        D[v]  = 0; 
        Nb[v] = u;
    }
    else
    {
        int keuze   = udef;
        int laagste = N;
        foreach (int w in Buren.Keys)
        {
            var k = Tuple.Create(w, v);
            if (!ndis.ContainsKey(k)) continue;

            int afstand = ndis[k];
            if (afstand < laagste)
            {
                keuze   = w;
                laagste = afstand;
            }
                
            if (v == w) break; // Afstand van 1 is minimaal
        }

        if (laagste < N)
        {
            D [v] = 1 + laagste;
            Nb[v] = keuze;    
        }
        else
        {   
            D [v] = N;
            Nb[v] = udef;    
        }
    }

    if (D[v] != oud)
    {
        if (D[v] >= N) Console.WriteLine("Onbereikbaar: " + v);
        else           Console.WriteLine("Afstand naar " + v + " is nu " + D[v] + " via " + Nb[v]);

        foreach(int b in Buren.Keys)
            Buren[b].Write.WriteLine("M {0} {1}", v, D[v]);
    }
}

public void Verbind2(int v, Verbinding ver)
{
    if (!Knopen.Contains(v))
        Knopen.Add(v);

    Buren[v] = ver;
}

public void Verbind(int v, Verbinding verbinding)
{
    if (!Knopen.Contains(v))
    {
        Knopen.Add(v);
        InitialiseerKnoop(v);
    }

    Buren[v] = verbinding;
    foreach (int w in Knopen)
    {
        ndis[Tuple.Create(w, v)] = N;

        if (!D.ContainsKey(w)) 
        {
            Console.WriteLine("// Knoop {0} niet gevonden", w);
            foreach (var x in D.Keys)
                Console.WriteLine("// D[{0}] = {1}", x, D[x]);
        }

        Buren[v].Write.WriteLine("M {0} {1}", w, D[w]);
    }
}

public void Ontbind(int w)
{
    if (!Buren.ContainsKey(w)) 
        Console.WriteLine("Poort {0} is niet bekend", w);
    else
    {
        Buren[w].Write.WriteLine("D " + u);
        Console.WriteLine("Verbroken: " + w);
        VerwerkOntbinding(w);
    }
}

public void VerwerkOntbinding(int w)
{
    Buren.Remove(w);
    D[w] = N;
    Nb[w] = udef;
    
    foreach(int v in Knopen) 
    {
        // ndis.Remove(Tuple.Create(u, v));
        ndis.Remove(Tuple.Create(v, u));
    }

    foreach(int v in Knopen) Herbereken(v);
}

public void BerichtAfhandelen(int v, string bericht) 
{
    string[] delen = bericht.Split(new char[] { ' ' }, 3);

    if      (v == u)             Console.WriteLine(delen[2]);
    else if (!Nb.ContainsKey(v)) Console.WriteLine("Poort {0} is niet bekend", v);
    else
    {
        int w = Nb[v];
        Buren[w].Write.WriteLine(bericht);
        Console.WriteLine("Bericht voor {0} doorgestuurd naar {1}", v, w);
    }
}

public void PrintRouteTabel() 
{
    foreach (int v in Nb.Keys)
    {
        if (Nb[v] == udef) continue;
        Console.WriteLine("{0} {1} {2}", v, D[v], D[v] > 0 ? Nb[v].ToString() : "local");
    }
}


public void Log(string s)
{
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("// " + s);
    Console.ForegroundColor = ConsoleColor.White;
}

public void Log(string s, int a, int b, int c)
{
    Log(s + " " + a + " " + b + " " + c);
}

public void PrintLog()
{
    Log("+++ROUTE TABEL+++");
    foreach( int x in Nb.Keys)
        Log("", x, D[x], Nb[x]);
    Log("+++NDIS+++");

    foreach( int x in Nb.Keys)
        foreach(int y in Nb.Keys)
        {
            var c = Tuple.Create(x, y);
            if (!ndis.ContainsKey(c)) continue;

            Log("", x, y, ndis[c]);
        }
}
}
}