using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace NeuralNetworkProcessor.Trainers;

public class WordTrainer
{
    public const int DefaultMaxSequenceLength = 14;
        //"pneumonoultramicroscopicsilicovolcanoconiosis".Length == 45
    public virtual int MaxSequenceLength { get; set; } = DefaultMaxSequenceLength;

    protected virtual ListLookups<long, string> CurrentWords { get; } = [];

    public int CharSweepLimit { get; set; } = 1024 * 1024;
    protected int InputCharsAfterLastSweep = 0;
    public int SequenceSweepLimit { get; set; } = 1024 * 1024;
    public int SequenceSweepLength { get; set; } = 16;

    public int WordsLimit { get; set; } = 4096;

    public int TopExractingCharsCount { get; set; } = 16;
    public int TopExtractingSequencesCount { get; set; } = 16;
    public int TopExtractingWordCount { get; set; } = 32;

    protected Dictionary<int, int> CharCounts { get; } = [];
    protected Dictionary<string, int> WordCounts { get; } = [];
    public HashSet<int> FrequentlySeenChars { get; } = [];
    public HashSet<string> FrequentlySeenWords { get; } = [];
    //This position is used for infinite input
    public virtual long Position { get; set; } = 0L;
    public virtual void Train(Input input)
    {
        foreach (var (ch, final) in input()) this.Read(ch, this.Position++);
    }

    public virtual void Read(int ch, long pos)
    {
        var t = char.ConvertFromUtf32(ch);
        this.OnCharRecording(ch);
        this.OnSequenceBuilding(t,pos);
        this.OnCollecting();
        this.TryCleanupChars();
        this.TryCleanupSequences(pos);
    }
    protected virtual void OnCharRecording(int ch)
    {
        if (!this.CharCounts.TryGetValue(ch, out var count))
        {
            this.CharCounts.Add(ch, 1);
        }
        else
        {
            this.CharCounts[ch] = count + 1;
        }
    }
    protected virtual void TryCleanupChars()
    {
        if (++this.InputCharsAfterLastSweep > this.CharSweepLimit)
        {
            foreach (var ch in this.CharCounts.OrderByDescending(p => p.Value)
                .Select(p => p.Key).Take(this.TopExractingCharsCount))
                this.FrequentlySeenChars.Add(ch);

            this.InputCharsAfterLastSweep = 0;
        }
    }

    protected virtual void OnSequenceBuilding(string t, long pos)
    {
        foreach(var c in this.CurrentWords.ToArray())
        {
            if (pos - c.Key > MaxSequenceLength)
                this.CurrentWords.Remove(c.Key);
            else 
                this.CurrentWords[c.Key].Add(
                    (c.Value.LastOrDefault() ?? "") + t);
        }
        this.CurrentWords.Add(pos, t);
    }
    protected virtual void TryCleanupSequences(long pos)
    {
        if (CurrentWords.TotalValuesCount > SequenceSweepLimit)
        {
            //TODO: better way to cleanup?
            CurrentWords.Where(c => c.Key - pos > SequenceSweepLength)
                .Select(c => c.Key)
                .ToList()
                .ForEach(k => CurrentWords.Remove(k));                
        }
    }

    protected virtual void OnCollecting()
    {
        foreach(var s in CurrentWords.SelectMany(p => p.Value))
        {
            if(!this.WordCounts.TryGetValue(s,out var count))
            {
                this.WordCounts.Add(s, 0);
            }
            else
            {
                this.WordCounts[s] = count + 1;
            }
        }
        if (this.WordCounts.Count > this.WordsLimit)
        {
            this.WordCounts.OrderByDescending(p => p.Value)
                .Take(TopExtractingWordCount).Select(p => p.Key)
                .ToList().ForEach(w => FrequentlySeenWords.Add(w));

            //TODO:
        }
    }
}

