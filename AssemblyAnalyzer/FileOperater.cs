using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace AssemblyAnalyzer
{
    static class FileOperater
    {
        public static string GetFilePathFromConsole()
        {
            Console.WriteLine("=============================================================================================");
            Console.WriteLine("注意:実行ファイルが存在しているディレクトリ内に解析対象のバイナリファイルを配置してください。");
            Console.WriteLine("注意:対象のバイナリファイルはMach-O 64bit x86-64とします。それ以外の実行可能ファイルはうまく解析かもしれません");
            Console.WriteLine("PE形式のファイルを解析する場合は.exeでなく.objファイルを指定してください。");
            Console.WriteLine("=============================================================================================");
            Console.WriteLine("解析対象のオブジェクトファイル名を指定してください");
            string filePath = Console.ReadLine();
            return filePath;
        }

        public static void GetDisassembleFile(string filePath)
        {
            if(File.Exists(filePath))
            {
                var process = new Process();
                process.StartInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.Arguments = @"/c objdump -d " + filePath + "> a.txt";
                process.Start();
                string results = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                process.Close();
                Console.WriteLine("書き込み成功");
            }
            else
            {
                Console.WriteLine("ERROR:指定されたファイルが存在しません");
                Console.WriteLine("ファイル名が誤っていないか、またはこの実行ファイルと同じディレクトリに解析対象のファイルが存在するかを確かめてください");
            }
        }

        public static List<string> GetInstructionList(string filePath)
        {
            var list = new List<string>();
            GetDisassembleFile(filePath);
            using (var reader = new StreamReader("c.txt"))
            {
                while(!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] token = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string t in token)
                    {
                        //関数が定義されている部分
                        if(t.IndexOf("<") != -1)
                        {
                            string insutrucution = t.Substring(1, t.Length - 2);
                            Console.WriteLine(insutrucution);
                            list.Add(insutrucution);
                        }
                    }
                }
            }
            return list;
        }
        
        public static Dictionary<string,int> GetRankingOfCPUInstruction(string filePath)
        {
            var dict = new Dictionary<string, int>();
            return dict;
        }
    }
}
