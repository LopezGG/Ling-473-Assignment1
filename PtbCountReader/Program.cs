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
            
            if (args.Length == 0)
            {
                    System.Console.WriteLine("Please enter the directory path to read files");
                    return 1;      
            }

            string folderPath = @args[0];
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

            //Ref: https://msdn.microsoft.com/en-us/library/bs2twtah.aspx#balancing_group_definition
            //Ref: http://stackoverflow.com/questions/19693622/how-to-get-text-between-nested-parentheses

            int totalMatch = 0;
            //This regex will match the outer most pattern VP
            var regex = new Regex(@"
                        \(VP [^\(]+                    # Match (Vp<space><anything other than open paranthesis>
                        (
                            [^()]+            # all chars except ()
                            | (?<Level>\()    # or if ( then Level += 1
                            | (?<-Level>\))   # or if ) then Level -= 1
                        )+                    # Repeat (to go from inside to outside)
                        (?(Level)(?!))        # zero-width negative lookahead assertion
                        \)                    # Match )", RegexOptions.IgnorePatternWhitespace);

            // This is to match with the inner Noun Phrase clause we need
            foreach (Match c in regex.Matches(input))
            {
                
                var regex1 = new Regex(@"
                        \(NP                    # Match (
                        (
                            [^()]+            # all chars except ()
                            | (?<Level>\()    # or if ( then Level += 1
                            | (?<-Level>\))   # or if ) then Level -= 1
                        )+                    # Repeat (to go from inside to outside)
                        (?(Level)(?!))        # zero-width negative lookahead assertion
                        \)                    # Match )", RegexOptions.IgnorePatternWhitespace);
                MatchCollection mc = regex1.Matches(c.Value);
                int li = 0;
                // Here we verify that we have only two NP 
                if (mc.Count == 2)
                {
                    
                    foreach (Match m in mc)
                    {
                        string newM = Regex.Replace(m.Value, @"\s+", "");
                        li = li + newM.Length;
                    }
                }

                string value = c.Value;
                //Eliminate the space before checking for length
                value = Regex.Replace(value, @"\s+", "");

                //Remove the first parantheis
                value = value.Substring(1);
                int length = value.LastIndexOf(")") - value.IndexOf("(");

                if (length == li)
                {
                    totalMatch++;
                }
            }

            return totalMatch;

        }
    }
}