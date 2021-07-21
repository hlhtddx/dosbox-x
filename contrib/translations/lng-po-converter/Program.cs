using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable StringLiteralTypo

namespace lng_po_converter
{
    internal static class Program
    {
        private static void Main()
        {
            var directory = Path.GetFullPath(@"..\..\..\..\");

            var files = Directory.GetFiles(directory, "*.lng", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                using var reader = new StringReader(File.ReadAllText(file, Encoding.UTF8));

                var builder = new StringBuilder();

                builder.Append("msgid \"\"\r\n" +
                               "msgstr \"\"\r\n" +
                               "\"Project-Id-Version: DOSBox-X\\n\"\r\n" +
                               "\"POT-Creation-Date: \\n\"\r\n" +
                               "\"PO-Revision-Date: \\n\"\r\n" +
                               "\"Last-Translator: \\n\"\r\n" +
                               "\"Language-Team: \\n\"\r\n" +
                               "\"MIME-Version: 1.0\\n\"\r\n" +
                               "\"Content-Type: text/plain; charset=UTF-8\\n\"\r\n" +
                               "\"Content-Transfer-Encoding: 8bit\\n\"\r\n" +
                               "\"Language: " + Path.GetFileNameWithoutExtension(file) + "\\n\"\r\n" +
                               "\"X-Generator: aybe\\n\"\r\n");

                builder.AppendLine();

                for (var i = 0; i < 4; i++)
                {
                    builder.AppendLine($"# {reader.ReadLine()}");
                }

                builder.AppendLine();

                var entries = new List<Entry>();

                while (true)
                {
                    var line = reader.ReadLine();

                    if (line == null)
                    {
                        break;
                    }

                    if (line == string.Empty)
                    {
                        continue; // handle stupid shit
                    }

                    if (line.StartsWith(":"))
                    {
                        var entry = new Entry {Identifier = line.Substring(1)};

                        while (true)
                        {
                            line = reader.ReadLine();

                            if (line == ".")
                            {
                                break;
                            }

                            entry.Lines.Add(line);
                        }

                        entries.Add(entry);
                    }
                    else
                    {
                        throw new InvalidDataException();
                    }
                }

                foreach (var entry in entries)
                {
                    builder.AppendLine($@"msgid ""{entry.Identifier}""");

                    switch (entry.Lines.Count)
                    {
                        case 0:
                            throw new InvalidDataException();
                        case 1:
                            builder.AppendLine($@"msgstr ""{entry.Lines.Single()}""");
                            break;
                        default:
                            builder.AppendLine(@"msgstr """"");

                            foreach (var line in entry.Lines)
                            {
                                builder.AppendLine($@"""{line}\n""");
                            }

                            break;
                    }

                    builder.AppendLine();
                }

                var path = Path.Combine(directory, Path.ChangeExtension(Path.GetFileNameWithoutExtension(file), "po")!);
                var data = builder.ToString();

                File.WriteAllText(path, data);
            }
        }

        private class Entry
        {
            public string Identifier { get; set; }

            public List<string> Lines { get; } = new List<string>();
        }
    }
}