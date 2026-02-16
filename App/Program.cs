using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DACN_Algorithms;

internal class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        if (args.Length > 0 && File.Exists(args[0]))
        {
            var fileInput = File.ReadAllText(args[0]);
            ProcessAndExtract(fileInput);
            return;
        }

        if (Console.IsInputRedirected)
        {
            var redirectedInput = Console.In.ReadToEnd();
            ProcessAndExtract(redirectedInput);
            return;
        }

        Console.WriteLine("=== GRAPH PARSER (C# EDITION) ===");
        Console.WriteLine("Nhập nhiều dòng đề bài (kết thúc bằng dòng trống).");

        Console.Write("\n>> ");
        var sb = new StringBuilder();
        while (true)
        {
            var line = Console.ReadLine();
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) break;
            sb.AppendLine(line);
        }
        var input = sb.ToString();
        if (string.IsNullOrWhiteSpace(input)) return;

        ProcessAndExtract(input);
    }

    static void ProcessAndExtract(string input)
    {
        try
        {
            Console.WriteLine("\n[1] Đang phân tích đề bài...");
            var extractionResult = GraphExtractor.Extract(input);

            var json = JsonSerializer.Serialize(extractionResult, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("--> Kết quả phân tích (JSON):");
            Console.WriteLine(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR] Đã xảy ra lỗi: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

}
