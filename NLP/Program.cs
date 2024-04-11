using NNP.Trainers;
using System;
using System.IO;
using System.Linq;

namespace NLP
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment.CurrentDirectory += "..\\..\\..\\..\\cps\\";
            var files = File.ReadAllText("__FileList.txt").Split(Environment.NewLine)
                .Select(f=>f.Trim()).Where(f=>f.Length>0).ToArray();
            var Trainer = new WordTrainer();

            foreach(var file in files)
            {
                var reader = new StreamReader(file);
                Trainer.Train(InputStreamReader.CreateInput(reader));
            }
        }
    }
}
