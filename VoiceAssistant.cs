using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Net;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;
using static System.Net.Mime.MediaTypeNames;
using System.Xml;
using static KursovWork.VoiceAssistant;
using NAudio.Wave;
using Microsoft.AspNetCore.Routing.Constraints;

namespace KursovWork
{
    public class VoiceAssistant
    {
        private Action<bool> ChangeVisible;
        private SpeechRecognitionEngine recognizer = new();
        private SpeechRecognitionEngine recognizerQuery;
        private SpeechSynthesizer synth = new();
        public List<OpenCommand> openCommands { get; set; }
        private Action<string> command;
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private bool _isStarted = false;
        private string pathToConfig;
        public class OpenCommand
        {
            public string FileName { get; set; }
            public string Path { get; set; }

            public OpenCommand(string programName, string pathToFile)
            {
                FileName = programName;
                Path = pathToFile;
            }
            public override string ToString()
            {
                return FileName + ", " + Path;
            }
        }
        public bool SetOpenCommand(string NameProgram, string pathToProgram)
        {
            if (NameProgram == null || NameProgram == string.Empty) 
                return false;
            
            if (!File.Exists(pathToProgram))
                return false;

            openCommands.Add(new OpenCommand(NameProgram, pathToProgram));

            return true;
        }
        
        public VoiceAssistant(Action<bool> changeVisible)
        {
            OpenConfiguration(Directory.GetCurrentDirectory() + "\\Configuration.xml");
            ChangeVisible = changeVisible;
        }
        public VoiceAssistant(string configPath, Action<bool> changeVisible)
        {
            OpenConfiguration(configPath);
            pathToConfig = configPath;
            ChangeVisible = changeVisible;
        }
        
        public void Start()
        {

            if (_isStarted)
                return;
            else
                _isStarted = true;
            
            

            outputDevice.Play();



            var grammarBuilder = new GrammarBuilder();
            grammarBuilder.Append(new Choices("Can you find in Google", "Stop recognition", "open program", "Save settings", "Help"));
            var grammar = new Grammar(grammarBuilder);
            recognizer.LoadGrammar(grammar);
            recognizer.SetInputToDefaultAudioDevice();
            recognizer.SpeechRecognized += RecognizerCommand_SpeechRecognized;
            recognizer.RecognizeAsync(RecognizeMode.Multiple);

            
            DefaultRecognizerQuery();

            synth.Speak("Hi, I'm your voice assistant. If you need help for use me just say help");

        }
        public void TestSpeach(string text) => synth.Speak(text);




        private void CreateRecognizerQuery()
        {
            recognizerQuery = new();
            recognizerQuery.SetInputToDefaultAudioDevice();
            recognizerQuery.SpeechRecognized += RecognizerQuery_SpeechRecognized;
        }
        private void DefaultRecognizerQuery() 
        {
            if(recognizerQuery != null)
                recognizerQuery.Dispose();

            CreateRecognizerQuery();
            recognizerQuery.LoadGrammar(new DictationGrammar());    
        }
        public void ChangeSpeachConfiguration(VoiceGender voiceGender, VoiceAge voiceAge, int voiceSpeed)
        {   
                synth.SelectVoiceByHints(voiceGender, voiceAge);
                synth.Rate = voiceSpeed;   
        }

        private void OpenConfiguration(string configPath)
        {


            outputDevice = new WaveOutEvent();
            audioFile = new AudioFileReader(Directory.GetCurrentDirectory() + "\\Sounds\\Activate Voice.mp3");
            outputDevice.Init(audioFile);

            openCommands = new();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(configPath);

                XmlNode settingsNode = xmlDoc.SelectSingleNode("/Settings");
                int voiceGender = int.Parse(settingsNode.SelectSingleNode("VoiceGender").InnerText);
                int voiceAge = int.Parse(settingsNode.SelectSingleNode("VoiceAge").InnerText);
                int voiceSpeed = int.Parse(settingsNode.SelectSingleNode("VoiceSpeed").InnerText);

                ChangeSpeachConfiguration((VoiceGender)voiceGender, (VoiceAge)voiceAge, voiceSpeed);

                
                

                XmlNodeList openCommandsTMP = xmlDoc.SelectNodes("/Settings/OpenCommand");
                foreach (XmlNode openCommand in openCommandsTMP)
                {
                    string programName = openCommand.SelectSingleNode("programName").InnerText;
                    string pathToFile = openCommand.SelectSingleNode("pathToFile").InnerText;
                    openCommands.Add(new OpenCommand(programName, pathToFile));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void AddChoicesForQuery()
        {

            CreateRecognizerQuery();

            Choices choices = new();

            foreach(var program in openCommands)
                choices.Add(program.FileName);

            choices.Add("Nothing");

            var grammarBuilder = new GrammarBuilder();
            grammarBuilder.Append(choices);
            var grammar = new Grammar(grammarBuilder);
            recognizerQuery.LoadGrammar(grammar);

        }
        

        public void GetStartConfiguration(out VoiceGender voiceGender, out VoiceAge voiceAge, out int voiceSpeed)
        {
            voiceGender = synth.Voice.Gender;
            voiceAge = synth.Voice.Age;
            voiceSpeed = synth.Rate;
        }
        private void RecognizerCommand_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence <= 0.7)
                return;
            


            if (e.Result.Text == "Can you find in Google")
            {

                recognizer.RecognizeAsyncCancel();
                synth.Speak("Yeah, search in Google");
                outputDevice.Play();
                command = SearchInGoogle;
                recognizerQuery.RecognizeAsync(RecognizeMode.Multiple);
            }
            else if (e.Result.Text == "open program")
            {
                recognizer.RecognizeAsyncCancel();
                synth.Speak("Yes, what do you want to open");
                outputDevice.Play();
                command = OpenProgram;
                AddChoicesForQuery();
                recognizerQuery.RecognizeAsync(RecognizeMode.Multiple);
            }
            else if (e.Result.Text == "Save settings")
            {
                synth.Speak("Settings saved");
                SaveConfiguration();
            }
            else if (e.Result.Text == "Stop recognition")
            {
                synth.Speak("OK, bye!");
                SaveConfiguration();
                Environment.Exit(0);
            }
            else if (e.Result.Text == "Help")
            {
                synth.Speak(File.ReadAllText(Directory.GetCurrentDirectory() + "\\read me.txt"));
            } 
        }

        public void SaveConfiguration()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement rootElement = xmlDoc.CreateElement("Settings");
            xmlDoc.AppendChild(rootElement);
            
            XmlElement voiceSpeedElement = xmlDoc.CreateElement("VoiceSpeed");
            voiceSpeedElement.InnerText = synth.Rate.ToString();
            rootElement.AppendChild(voiceSpeedElement);

            XmlElement voiceGenderElement = xmlDoc.CreateElement("VoiceGender");
            voiceGenderElement.InnerText = ((int)synth.Voice.Gender).ToString();
            rootElement.AppendChild(voiceGenderElement);

            XmlElement voiceAgeElement = xmlDoc.CreateElement("VoiceAge");
            voiceAgeElement.InnerText = ((int)synth.Voice.Age).ToString();
            rootElement.AppendChild(voiceAgeElement);

            foreach (var command in openCommands)
            {
                XmlElement openCommandElement = xmlDoc.CreateElement("OpenCommand");

                XmlElement programNameElement = xmlDoc.CreateElement("programName");
                programNameElement.InnerText = command.FileName;
                openCommandElement.AppendChild(programNameElement);

                XmlElement pathToFileElement = xmlDoc.CreateElement("pathToFile");
                pathToFileElement.InnerText = command.Path;
                openCommandElement.AppendChild(pathToFileElement);

                rootElement.AppendChild(openCommandElement);
            }

            xmlDoc.Save(pathToConfig);
        }


        private void RecognizerQuery_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            command(e.Result.Text);
            recognizerQuery.RecognizeAsyncCancel();
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void OpenProgram(string programName)
        {
            if (programName == "Nothing") {
                synth.Speak("OK, close open program command");
                DefaultRecognizerQuery();
                return;
            }
            foreach (var program in openCommands)
            {
                if (program.FileName == programName)
                {
                    if (File.Exists(program.Path))
                        Process.Start(program.Path);
                    else
                        Console.WriteLine("Didn't find the program");

                    break;
                }
            }
            DefaultRecognizerQuery();
        }

        private async void SearchInGoogle(string query)
        {

            string url = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
