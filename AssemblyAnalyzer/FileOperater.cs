using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace AssemblyAnalyzer
{
    static class FileOperater
    {
        public static string GetFilePathFromConsole()
        {
            Console.WriteLine("=============================================================================================");
            Console.WriteLine("注意:実行ファイルが存在しているディレクトリ内に解析対象のバイナリファイルを配置してください。");
            Console.WriteLine("注意:対象のバイナリファイルはPE x86-64でgccでコンパイルされた実行ファイルとします。");
            Console.WriteLine("clコマンドでコンパイルされた実行ファイルは解析可能ですが、うまく行かない可能性が高いです。");
            Console.WriteLine("=============================================================================================");
            Console.WriteLine("解析対象のバイナリファイル名を指定してください");
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
                process.StartInfo.Arguments = @"/c objdump -d --no-show-raw-insn " + filePath + "> c.txt";
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

        public static Dictionary<string, string> GetInstructionDictionary(string filePath)
        {
            var dict = new Dictionary<string, string>();
            int a = 0;
            GetDisassembleFile(filePath);
            using (var reader = new StreamReader("c.txt"))
            {
                while(!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] tokens = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                    //トークン数が2の列のみ操作する
                    if (tokens.Length == 2 && line.IndexOf("<") != -1 && line.IndexOf(".") == -1)
                    {
                        string address = tokens[0];
                        string instruction = tokens[1].Substring(2, tokens[1].Length - 4);
                        if(!dict.ContainsValue(instruction))
                        {
                            dict.Add(address, instruction);
                        }
                    }
                }
            }
            
            foreach(var item in dict)
            {
                Console.WriteLine("ADDRESS:" + item.Key + " INSTRUCTION:" + item.Value);
            }
            
            return dict;
        }
        
        public static Dictionary<string,int> GetRankingOfCPUInstruction(Dictionary<string,string> instructions)
        {
            var dict = new Dictionary<string, int>();
            int line_count = 1;
            try
            {
                using (var reader = new StreamReader("c.txt"))
                {
                    while(!reader.EndOfStream)
                    {
                        if(line_count >= 7)
                        {
                            string line = reader.ReadLine();
                            string[] tokens = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var pattern = new Regex(@"[a-zA-Z]");
                            //instructionにアドレスが登録されていない行を調査する
                            if (tokens.Length >= 2)
                            {
                                if (!instructions.ContainsKey(tokens[0]))
                                {
                                    //CPU命令が登録されていないかつ、<.text>のような行でなければ登録する
                                    if (!dict.ContainsKey(tokens[1]) && pattern.IsMatch(tokens[1]) && tokens[1].IndexOf("<") == -1 && tokens[1].IndexOf("(") == -1)
                                    {
                                        dict.Add(tokens[1], 1);
                                    }
                                    else if(pattern.IsMatch(tokens[1]) && tokens[1].IndexOf("<") == -1 && tokens[1].IndexOf("(") == -1)
                                    {
                                        dict[tokens[1]]++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            line_count++;
                            reader.ReadLine();
                        }
                    }
                }
            }
            catch(IOException e)
            {
                Console.WriteLine("File Reading Error");
                Console.WriteLine("生成されるc.txtファイルが削除されているかまたは、objdumpの実行に失敗しました");
            }
            foreach (var item in dict)
            {
                Console.WriteLine("命令:" + item.Key + "　回数:" + item.Value);
            }
            return dict;
        }

        public static void AnalyzeBinaryFile(string filePath)
        {
            var dict = new Dictionary<string, string>();
            dict = GetInstructionDictionary(filePath);
            GetRankingOfCPUInstruction(dict);
        }
    }
}
