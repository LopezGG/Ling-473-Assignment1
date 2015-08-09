using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace PTBMatch
{
    class Program
    {
        static int Main(string[] args)
        {
            string folderPath = "";
            if (args.Length == 0)
            {
                folderPath = @"/corpora/LDC/LDC99T42/RAW/parsed/prd/wsj/14";
            }
            else
            {
                folderPath = @args[0];
            }


            string text = CollateFiles(folderPath);

            //Looking for Sentences
            string pattern = @"\(S ";
            int Count = FindMatches(text, pattern);
            Console.WriteLine("Sentences : " + Count);

            //Noun Phrase
            pattern = @"\(NP ";
            Count = FindMatches(text, pattern);
            Console.WriteLine("Noun Phrase : " + Count);

            //Verb Phrase which  may be nested
            pattern = @"\(VP ";
            Count = FindMatches(text, pattern);
            Console.WriteLine("Verb Phrase : " + Count);

            //Ditransitive Verb Phrase 
            Count = FindDVP(text);
            Console.WriteLine("DVP : " + Count);


            //Intransitive Verb Phrase 
            pattern = @"\(VP ([^(\)\()]+)\)";
            Count = FindMatches(text, pattern);
            Console.WriteLine("IVP : " + Count);


#if DEBUG
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
#endif
            return 0;

        }

        static int FindMatches(string text, string pattern)
        {
            Regex regex = new Regex(@pattern);
            MatchCollection mc = regex.Matches(text);
            return mc.Count;
        }

        static string CollateFiles(string folderPath)
        {
            CheckDir(folderPath);
            StringBuilder contents = new StringBuilder();
            foreach (string file in Directory.EnumerateFiles(folderPath, "*.prd"))
            {
                string tempcontents = File.ReadAllText(file);
                contents.Append(tempcontents);

            }
            return contents.ToString();
        }

        //Ref: https://stackoverflow.com/questions/1073038/c-should-i-throw-an-argumentexception-or-a-directorynotfoundexception/1073349#1073349
        static void CheckDir(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Incorrect Path");
            }
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException();
            }
        }

        // Given a text this function return the count of times we have a  Ditransitive Verb Phrase of type (VP verb (NP ...) (NP ...) )
        static int FindDVP(string input)
        {


            int totalMatch = 0;
            Regex rx = new Regex(@"\(VP ");

            foreach (Match match in rx.Matches(input))
            {
                int index = match.Index;
                //if I get a grouped construct then I miss construct like (VP was (Vp God (NP...)(NP..) so I have a custom function to all occurances of VP
                string newValue = getWholeConstruct(input, index);

                //Ref for getting grouped constructs: https://msdn.microsoft.com/en-us/library/bs2twtah.aspx#balancing_group_definition
                //Ref: http://stackoverflow.com/questions/19693622/how-to-get-text-between-nested-parentheses

                var regex1 = new Regex(@"
                        \(NP[\s]                    # Match (
                        (
                            [^()]+            # all chars except ()
                            | (?<Level>\()    # or if ( then Level += 1
                            | (?<-Level>\))   # or if ) then Level -= 1
                        )+                    # Repeat (to go from inside to outside)
                        (?(Level)(?!))        # zero-width negative lookahead assertion
                        \)                    # Match )", RegexOptions.IgnorePatternWhitespace);
                MatchCollection mc = regex1.Matches(newValue);
                int li = 0;

                if (mc.Count == 2)
                {
                    foreach (Match m in mc)
                    {
                        string newM = Regex.Replace(m.Value, @"\s+", "");
                        li = li + newM.Length;
                    }

                    newValue = Regex.Replace(newValue, @"\s+", "");
                    //Remove the first parantheis
                    newValue = newValue.Substring(1);

                    //This is to avoid count verb in "VP verb"
                    int length = newValue.LastIndexOf(")") - newValue.IndexOf("(");

                    //Compare length of parts to see if they make up the whole VP clause
                    if (length == li)
                    {
                        totalMatch++;
                    }
                }
            }

            return totalMatch;

        }

        static string getWholeConstruct(string input, int index)
        {
            string temp = "";
            int openBr = 1;
            int runner;
            char[] inputChar = input.ToCharArray();
            for (runner = index + 4; runner < inputChar.Length; runner++)
            {
                if (inputChar[runner].CompareTo('(') == 0)
                {
                    openBr = openBr + 1;

                }
                else if (inputChar[runner].CompareTo(')') == 0)
                {
                    openBr = openBr - 1;
                }

                if (openBr == 0)
                {
                    break;
                }
            }
            if (runner < inputChar.Length)
            {
                temp = input.Substring(index, runner - index + 1);
            }

            return temp;
        }
    }
}