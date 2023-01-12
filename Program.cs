using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using RestSharp;

namespace office_console
{
    class Program
    {
        private int hWidth;
        private int hHeight;
        private Point hLocation;
        private ConsoleColor hBorderColor;
        private static string[] lines = {
                    "[Delay]",
                    "600000",
                    "[CheckTime]",
                    "12:00:00",
                    "[HttpMethod]",
                    "GET",
                    "[HttpRequest]",
                    "https://api.github.com/users/francivaldo4334"
                };
        static void Main(string[] args)
        {
            if(!validConfFile()){
                Console.WriteLine("Virifique o arquivo conf.ini");
                return;
            }
            int timeDelay = 600000;
            bool running = true;
            DateTime CheckTime = DateTime.Parse("1/01/1000 12:00:00");
            if(args.Length > 0)
                switch (args[0])
                {
                    case "start":{
                        Process[] localByName = Process.GetProcessesByName("office-console");
                        if(localByName.Length > 1){
                            PrintMensage("Exist um processo ativo.");
                            return;
                        }
                        
                        PrintMensage("Running...");
                        //Load Delay value
                        var delayString = GetConfigIni("Delay");
                        if(delayString != "null")
                            try{
                                timeDelay = Int32.Parse(delayString);
                            }
                            catch (FormatException e){Console.WriteLine(e.Message+"\nVerifique o arquivo de configuracao.");running = false;}
                        //Load CheckTime value
                        var checkTimeValue = GetConfigIni("CheckTime");
                        if(checkTimeValue != "null")
                            try{
                                CheckTime = DateTime.Parse($"1/01/1000 {checkTimeValue}");
                            }
                            catch (FormatException e){Console.WriteLine(e.Message+"\nVerifique o arquivo de configuracao.");running = false;}
                            
                        //<Loop
                        while (running)
                        {
                            //<Actions
                            var timeIni = CheckTime.AddMinutes(-15);
                            var timeEnd = CheckTime.AddMinutes(15);
                            var timeNow = DateTime.Now;
                            var dateCompare = new DateTime(
                                CheckTime.Year,
                                CheckTime.Month,
                                CheckTime.Day,
                                timeNow.Hour,
                                timeNow.Minute,
                                timeNow.Second
                            );
                            if(
                                DateTime.Compare(timeIni,dateCompare) < 0 &&
                                DateTime.Compare(timeEnd,dateCompare) > 0 
                            ){
                                bodyActions();
                            }
                            //Actions>
                            var tastDelay = Task.Run(async delegate
                            {
                                await Task.Delay(timeDelay);
                                return 0;
                            });
                            tastDelay.Wait();
                        }
                        //Loop>
                        break;
                    }
                    case "status":{
                        Process[] localByName = Process.GetProcessesByName("office-console");
                        if(localByName.Length > 1)
                            PrintMensage("Ativo");
                        else 
                            PrintMensage("Inativo");
                        break;
                    }
                    case "-h":{
                        CommandList();
                        break;
                    }
                    case "--help":{
                        CommandList();
                        break;
                    }
                    default:{
                        CommandList();
                        break;
                    }
                }            
            else CommandList();
        }
        static void CommandList(){
            Console.WriteLine(
            $"comandos:\n"+
            "   start     Inicia o processo de verificacao.\n"+
            "   status    Verifica se o programa ja esta em execucao.\n"+
            "   -h|--help Mostrar lista de comandos."
            );
        }
        static void PrintMensage(string s){
            string ulCorner = "╔";
            string llCorner = "╚";
            string urCorner = "╗";
            string lrCorner = "╝";
            string vertical = "║";
            string horizontal = "═";

            string[] lines = s.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            

            int longest = 0;
            foreach(string line in lines)
            {
                if (line.Length > longest)
                    longest = line.Length;
            }
            int width = longest + 2; // 1 space on each side

            
            string h = string.Empty;
            for (int i = 0; i < width; i++)
                h += horizontal;

            // box top
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ulCorner + h + urCorner);

            // box contents
            foreach (string line in lines)
            {
                double dblSpaces = (((double)width - (double)line.Length) / (double)2);
                int iSpaces = Convert.ToInt32(dblSpaces);

                if (dblSpaces > iSpaces) // not an even amount of chars
                {
                    iSpaces += 1; // round up to next whole number
                }

                string beginSpacing = "";
                string endSpacing = "";
                for (int i = 0; i < iSpaces; i++)
                {
                    beginSpacing += " ";

                    if (! (iSpaces > dblSpaces && i == iSpaces - 1)) // if there is an extra space somewhere, it should be in the beginning
                    {
                        endSpacing += " ";
                    }
                }
                // add the text line to the box
                sb.AppendLine(vertical + beginSpacing + line + endSpacing + vertical);
            }

            // box bottom
            sb.AppendLine(llCorner + h + lrCorner);

            // the finished box
            Console.WriteLine(sb.ToString());
        }
        static bool validConfFile(){
            bool result = true;
            //<Check file conf exist
            string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string path = $"{home}/officesafe-console/conf.ini";
            if(!File.Exists(path)){
                Directory.CreateDirectory($"{home}/officesafe-console");
                File.WriteAllLines(path, lines);
            }
            lines = File.ReadAllLines(path);
            //Check file conf exist>
            //validate
            if(lines.Length > 0){
                for (int i = 0; i < lines.Length; i++)
                {
                    switch(lines[i]){
                        case "[Delay]":{
                            if(lines.Length > i+1){
                                var value = lines[i+1];
                                if(!int.TryParse(value, out _))
                                    result = false;
                            }
                            break;
                        }
                        case "[CheckTime]":{
                            if(lines.Length > i+1){
                                var value = lines[i+1];
                                var r = new Regex("^\\d(\\d|(?<!:):)*\\d$|^\\d$");
                                if(!r.IsMatch(value))
                                    result = false;
                            }
                            break;
                        }
                        case "[HttpRequest]":{
                            if(lines.Length > i+1){
                                var value = lines[i+1];
                                var r = new Regex("(https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\\.[^\\s]{2,}|www\\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\\.[^\\s]{2,}|https?:\\/\\/(?:www\\.|(?!www))[a-zA-Z0-9]+\\.[^\\s]{2,}|www\\.[a-zA-Z0-9]+\\.[^\\s]{2,})");
                                if(!r.IsMatch(value))
                                    result = false;
                            }
                            break;
                        }
                        case "[HttpMethod]":{
                            if(lines.Length > i+1){
                                var value = lines[i+1];
                                switch(value){
                                    case "DELETE":{
                                        break;
                                    }
                                    case "PUT":{
                                        break;
                                    }
                                    case "GET":{
                                        break;
                                    }
                                    default:{
                                        result = false;
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                        default: break;
                    }
                
                }
            }
            return result;

        }
        static string GetConfigIni(string value){
            string result = "null";
            if(lines.Length > 0){
                for (int i = 0; i < lines.Length; i++)
                {
                    if(lines[i] == $"[{value}]"&&lines.Length > i+1){
                        result = lines[i+1];
                    }
                }
            }
            return result;
        }
        static void bodyActions(){
            //<Check file history exist
            string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string path = $"{home}/officesafe-console/history.txt";
            string[] lines = {""};
            if(!File.Exists(path)){
                Directory.CreateDirectory($"{home}/officesafe-console");
                File.WriteAllLines(path, lines);
            }
            lines = File.ReadAllLines(path);
            //Check file history exist>
            DateTime DateLast;
            if(lines.Length>0)
                try{
                    DateLast = DateTime.Parse(lines[^1]);
                }catch(FormatException e){
                    DateLast = DateTime.Now.AddDays(-1);
                }
            else DateLast = DateTime.Now.AddDays(-1);
            var DateNow = DateTime.Now;
            if(DateLast.Day < DateNow.Day){
                //<Http Request
                if(HttpRequest()){
                    //<Save Date in History
                    var list = new System.Collections.Generic.List<string>(lines);
                    list.Add(DateNow.ToString());
                    File.WriteAllLines(path, list);
                    //Save Date in History>
                }
                //Http Request>
            }

        }
        static bool HttpRequest(){
            //get in conf.ini
            var link = GetConfigIni("HttpRequest");
            var methodtxt = GetConfigIni("HttpMethod");
            Method method;
            switch(methodtxt){
                case "DELETE":{
                    method = Method.Delete;
                    break;
                }
                case "PUT":{
                    method = Method.Put;
                    break;
                }
                case "GET":{
                    method = Method.Get;
                    break;
                }
                default:{
                    method = Method.Get;
                    break;
                }
            }

            //request
            var client = new RestClient();
            var request = new RestRequest(new Uri(link),method);
            RestResponse restResponse = client.Execute(request);
            if(!restResponse.IsSuccessStatusCode){
                Console.WriteLine(link);
                Console.WriteLine(restResponse.Content);
            }
            return restResponse.IsSuccessStatusCode;
        }
    }
}
