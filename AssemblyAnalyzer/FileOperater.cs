using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;


namespace AssemblyAnalyzer
{
    static class FileOperater
    {
        public static string GetFilePathFromConsole()
        {
            Console.WriteLine("====================================================================================================================");
            Console.WriteLine("注意:実行ファイルが存在しているディレクトリ内に解析対象のバイナリファイルを配置してください。");
            Console.WriteLine("注意:対象のバイナリファイルはPE x86-64でgccでコンパイルされた実行ファイルとします。");
            Console.WriteLine("clコマンドでコンパイルされた実行ファイルは解析可能な場合もありますが、ツール自体がうまく動作しない可能性が高いです。");
            Console.WriteLine("また、解析結果や逆アセンブル結果をa.txt,c.txtに書き込みます。該当テキストファイルが上書きされたくない場合はこのディレクトリから" +
                "退避させてください。");
            Console.WriteLine("=====================================================================================================================");
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
                Console.WriteLine("============逆アセンブリ結果の書き込み成功(c.txtに書き込みました)============");
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
                        //最初の6行は解析しない
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
                                    if (!dict.ContainsKey(tokens[1]) && pattern.IsMatch(tokens[1]) && tokens[1].IndexOf(".") == -1 && tokens[1].IndexOf("(") == -1)
                                    {
                                        dict.Add(tokens[1], 1);
                                    }
                                    else if(pattern.IsMatch(tokens[1]) && tokens[1].IndexOf(".") == -1 && tokens[1].IndexOf("(") == -1)
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
                Console.WriteLine("=============CPU命令の出現回数の調査完了==========");
                //Console.WriteLine("===========CPU命令出現回数ランキング作成完了=================");
            }
            catch(IOException e)
            {
                Console.WriteLine("File Reading Error");
                Console.WriteLine("生成されるc.txtファイルが削除されているかまたは、objdumpの実行に失敗しました");
            }
            return dict;
        }

        private static void OutputResultToFile(Dictionary<string, string> dict, Dictionary<string, int> cpuDict, 
            Dictionary<string, List<string>> functionRelationship)
        {
            var currentTime = DateTime.Now;
            Console.WriteLine("a.txtに書き込みを開始します。");
            using (var writer = new StreamWriter("a.txt"))
            {
                writer.WriteLine("========================解析結果==========================");
                writer.WriteLine("File Written: " + currentTime);
                writer.WriteLine("");
                writer.WriteLine("");
                writer.WriteLine("定義されている関数一覧");
                foreach (var item in dict)
                {
                    writer.WriteLine(item.Value);
                }
                writer.WriteLine("==========================================================");
                writer.WriteLine("");
                writer.WriteLine("");
                writer.WriteLine("登場するCPU命令ランキング及び、登場回数");
                IOrderedEnumerable<KeyValuePair<string, int>> sorted = cpuDict.OrderByDescending(pair => pair.Value);
                int rank = 1;
                bool isFirstLoop = true;
                int tmpValue = 0;
                foreach (var item in sorted)
                {
                    if (isFirstLoop)
                    {
                        tmpValue = item.Value;
                        isFirstLoop = false;
                    }
                    if (tmpValue != item.Value)
                    {
                        rank++;
                        tmpValue = item.Value;
                    }
                    writer.WriteLine(rank + "位　命令:" + item.Key + "　回数:" + item.Value);
                }
                writer.WriteLine("==========================================================");
                writer.WriteLine("");
                writer.WriteLine("");
                writer.WriteLine("関数の呼び出し関係");
                foreach(var item in functionRelationship)
                {
                    writer.Write(item.Key+ ": ");
                    //writer.Write(item.Value.Count);
                    foreach(var functions in item.Value)
                    {
                        writer.Write(", " + functions);
                    }
                    writer.Write("\n");
                }
            }
            Console.WriteLine("書き込みが完了しました。");
        }

        private static Dictionary<string, List<string>> AnalyzeMutualRelationshipOfFunction(Dictionary<string, string> functionDict)
        {
            var dict = new Dictionary<string, List< string > >();
            Console.WriteLine("関数の呼び出し関係を調査します");
            try
            {
                using (var reader = new StreamReader("c.txt"))
                {
                    string currentFunctionName = "NULL";
                    //関数の呼び出し関係を調査するかどうか
                    bool doSurvey = false;
                    var functions = new List<List<string>>();
                    functions.Add(new List<string>());
                    int index = 0;

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] tokens = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        //宣言されている関数とアドレスが一致した場合に調査を開始する。
                        if(tokens.Length >= 2)
                        {
                           
                            if (functionDict.ContainsKey(tokens[0]) && line.IndexOf("<") != -1)
                            {
                                //解析対象->解析対象のセクション移動の場合
                                if(doSurvey)
                                {
                                    dict.Add(currentFunctionName, functions[index]);
                                    string address = tokens[0];
                                    currentFunctionName = functionDict[address];
                                    index++;
                                    functions.Add(new List<string>());
                                }
                                //解析対象外->解析対象のセクション移動
                                else 
                                {
                                    string address = tokens[0];
                                    currentFunctionName = functionDict[address];
                                    doSurvey = true;
                                }
                            }
                            //<.text>のようなセクションにマッチした
                            else if (!functionDict.ContainsKey(tokens[0]) && tokens[1].IndexOf("<") != -1 && doSurvey)
                            {
                                doSurvey = false;
                                dict.Add(currentFunctionName, functions[index]);
                                index++;
                                functions.Add(new List<string>());
                            }

                            if (doSurvey)
                            {
                                //call命令を含んだ行にマッチした
                                if (line.IndexOf("call") != -1)
                                {
                                    if(tokens.Length >= 4)
                                    {
                                        if(tokens[3].IndexOf(".") == -1)
                                        {
                                            string functionName = tokens[3].Substring(2, tokens[3].Length - 3);
                                            functions[index].Add(functionName);
                                            Console.WriteLine(currentFunctionName + ":" + functionName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch(IOException e)
            {
                Console.WriteLine("File Reading Error");
                Console.WriteLine("生成されるc.txtファイルが削除されているかまたは、objdumpの実行に失敗しました");
            }
            return dict;
        }

        public static void AnalyzeBinaryFile(string filePath)
        {
            var dict = new Dictionary<string, string>();
            var cpuDict = new Dictionary<string, int>();
            dict = GetInstructionDictionary(filePath);
            cpuDict = GetRankingOfCPUInstruction(dict);
            var functionDict = AnalyzeMutualRelationshipOfFunction(dict);
            OutputResultToFile(dict, cpuDict, functionDict);
        }
    }
}

