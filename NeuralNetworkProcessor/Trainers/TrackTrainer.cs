using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace NeuralNetworkProcessor.Trainers;

public class Track : List<string> 
{
    public static implicit operator Track(string text)=>new() { text };
    public static Track operator +(Track t, string s) => new(t) { s };
    public Track() { }
    public Track(IEnumerable<string> collection) : base(collection) { }
    public override bool Equals(object obj) 
        => obj is Track t && this.SequenceEqual(t);
    public override int GetHashCode()
        => this.Aggregate(0, (a, b) => a ^ b.GetHashCode());
}
public class TrackTrainer : WordTrainer
{
    public HashSet<Track> FreqentlySeenTracks { get; } = new();
    public Dictionary<Track, int> TracksCount { get; } = new();
    public int MaxTrackLength { get; set; } = DefaultMaxSequenceLength;
    public int MaxExtractedTrackLength { get; set; } = 32;
    public override void Read(int ch, long cpos)
    {
        var words = new List<(string word,long pos)>();

        if (this.CollectWords(ch,words))
        {
            this.Read(words,cpos);
        }
        //we can build while recognize
        base.Read(ch, cpos);
    }

    protected string CurrentWindow = "";
    protected virtual bool CollectWords(int ch, List<(string,long)> words)
    {
        if (this.CurrentWindow.Length < MaxSequenceLength*2)
        {
            this.CurrentWindow += ch;
        }
        else
        {
            this.CurrentWindow = this.CurrentWindow[1..] + ch;
        }
        for(int i = 0; i < this.CurrentWindow.Length - this.MaxSequenceLength; i++)
        {
            for(int j = Math.Min(i+ this.MaxSequenceLength, this.CurrentWindow.Length); j>=i+1; j--)
            {
                var CurrentWord = this.CurrentWindow[i..j];
                if (this.FrequentlySeenWords.Contains(CurrentWord))
                {
                    words.Add((CurrentWord,j));
                }
            }
        }
        return words.Count > 0;
    }

    public ListLookups<long, Track> CurrentTracks { get; } = new ();
    public void Read(List<(string word,long pos)> words, long pos)
    {
        foreach(var w in words)
        {
            this.OnTrackBuilding(w.word, w.pos);
        }
        this.TryCleanupTracks(pos);
    }
    protected virtual void OnTrackBuilding(string word,long pos)
    {
        foreach (var t in this.CurrentTracks.ToArray())
        {
            if (pos - t.Key > this.MaxTrackLength) 
                this.CurrentTracks.Remove(t.Key);
            else 
                this.CurrentTracks[t.Key].Add(
                    (t.Value.LastOrDefault() ?? new Track()) + word);
        }
        this.CurrentTracks.Add(pos, word);
    }
    protected virtual void TryCleanupTracks(long pos)
    {
        this.CurrentTracks.Values.SelectMany(s => s).ToList()
            .ForEach(t =>
                {
                    if(!this.TracksCount.TryGetValue(t, out var count))
                    {
                        this.TracksCount.Add(t, 0);
                    }
                    else
                    {
                        this.TracksCount[t] = count + 1;
                    }
                }
            );
        this.TracksCount.OrderByDescending(p => p.Value)
            .Take(MaxExtractedTrackLength).ToList().ForEach(
            p => this.FreqentlySeenTracks.Add(p.Key));
    }
}
