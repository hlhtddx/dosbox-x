using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using DeepL;
using JetBrains.Annotations;

// ReSharper disable IdentifierTypo

// TODO code page and language on top should be updated, if possible else manually

namespace dosbox_translation_deepl
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);

            await result.WithParsedAsync(Action);
        }

        private static async Task Action([NotNull] Options options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            await using var sourceStream = File.OpenRead(options.SourceFile);

            var lf = new LanguageFile(sourceStream);

            using var client = new DeepLClient(options.ApiKey, true);

            var index = 0;

            foreach (var element in lf.Elements)
            {
                try
                {
                    var translations = await client.TranslateAsync(element.SourceLines, options.SourceLanguage,
                        options.TargetLanguage);

                    foreach (var translation in translations)
                    {
                        element.TargetLines.Add(translation.Text);
                    }
                }
                catch (DeepLException e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                Console.WriteLine($"{1.0d / (lf.Elements.Count) * ++index:P2}: {element.Identifier}");
            }

            await using var targetStream = File.Create(options.TargetFile);

            lf.Write(targetStream);
        }
    }

    internal sealed class LanguageFile
    {
        public LanguageFile([NotNull] Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var reader = new StreamReader(stream, Encoding.UTF8, true);

            var summary = new List<string>();

            for (var i = 0; i < 4; i++)
            {
                summary.Add(reader.ReadLine());
            }

            var elements = new List<LanguageElement>();

            while (reader.EndOfStream is false)
            {
                var element = new LanguageElement(reader.ReadLine());

                while (true)
                {
                    var line = reader.ReadLine();

                    if (line == string.Empty)
                    {
                        continue;
                    }

                    if (line == ".")
                    {
                        break;
                    }

                    element.SourceLines.Add(line);
                }

                elements.Add(element);
            }

            Summary = new ReadOnlyCollection<string>(summary);

            Elements = new ReadOnlyCollection<LanguageElement>(elements);
        }

        public ReadOnlyCollection<string> Summary { get; }

        public ReadOnlyCollection<LanguageElement> Elements { get; }

        public void Write([NotNull] Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

            foreach (var item in Summary)
            {
                writer.WriteLine(item);
            }

            foreach (var element in Elements)
            {
                writer.WriteLine(element.Identifier);

                foreach (var line in element.TargetLines)
                {
                    writer.WriteLine(line);
                }

                writer.WriteLine(".");
                writer.WriteLine();
            }
        }
    }

    internal sealed class LanguageElement
    {
        public LanguageElement(string identifier)
        {
            Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        }

        public string Identifier { get; }

        public List<string> SourceLines { get; } = new List<string>();

        public List<string> TargetLines { get; } = new List<string>();

        public override string ToString()
        {
            return $"{nameof(Identifier)}: {Identifier}";
        }
    }

    [UsedImplicitly]
    internal sealed class Options
    {
        [Option('k', "key", Required = true, HelpText = "DeepL API key")]
        public string ApiKey { get; set; }

        [Option('s', "source", Required = true, HelpText = "Source .LNG file")]
        public string SourceFile { get; set; }

        [Option('t', "target", Required = true, HelpText = "Target .LNG file")]
        public string TargetFile { get; set; }

        [Option('i', "input", Required = true, HelpText = "Source language")]
        public Language SourceLanguage { get; set; }

        [Option('o', "output", Required = true, HelpText = "Target language")]
        public Language TargetLanguage { get; set; }
    }
}