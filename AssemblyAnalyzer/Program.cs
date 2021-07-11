using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;


namespace AssemblyAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = FileOperater.GetFilePathFromConsole();
            Console.WriteLine(filename);
            FileOperater.GetInstructionList(filename);
            Console.WriteLine("EnterKey押すと終了します");
            //EnterKeyを押すまで待機
            while (Console.ReadKey().Key != ConsoleKey.Enter) { }
        }
    }
}
