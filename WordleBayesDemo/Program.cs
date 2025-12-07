using System;
using System.Collections.Generic;
using Word_Analyzer;

class Program
{
    static void Main()
    {
        var candidates = new List<string> { "arise", "stale", "point", "crane", "slate", "clamp", "proud", "vigor", "tangy", "cider" };
        var bayes = new BayesianSearch(candidates);
        Console.WriteLine("First guess: " + bayes.Run());

        var feedback = WordleProcessor.Process("arise", "crane"); // simulate feedback
        string pattern = string.Join("", feedback.ConvertAll(f => f.Item2));
        Console.WriteLine("Feedback pattern for arise vs crane: " + pattern);

        var filtered = new List<string>();
        foreach (var w in candidates)
        {
            if (string.Join("", WordleProcessor.Process("arise", w).ConvertAll(f => f.Item2)) == pattern)
                filtered.Add(w);
        }

        var bayes2 = new BayesianSearch(filtered);
        Console.WriteLine("Next guess: " + bayes2.Run());
    }
}
