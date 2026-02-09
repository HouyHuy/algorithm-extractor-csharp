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
        Console.WriteLine("Nhập một dòng đề bài và nhấn Enter để chạy.");

        Console.Write("\n>> ");
        var lineInput = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(lineInput)) return;

        ProcessAndExtract(lineInput);
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
