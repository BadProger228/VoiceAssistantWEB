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
using System.Data.SqlTypes;
using System.Xml.Linq;

namespace KursovWork
{
    public class VoiceAssistant
    {
        private ActivateSound? activateSound;
        private SpeechRecognitionEngine recognizer = new();
        private SpeechRecognitionEngine recognizerQuery;
        private SpeechSynthesizer synth = new();
        public List<OpenCommand> openCommands { get; set; }
        private Action<string> command;
        private bool _isStarted = false;
        private string pathToConfig;
        public string nameUser { get; set; }
        
        public class OpenCommand
        {
            public string FileName { get; set; }
            public string Path { get; set; }

            public OpenCommand(string programName, string pathToFile)
            {
                FileName = programName;
                Path = pathToFile;
            }
            public override string ToString() => FileName + ", " + Path;
            
        }
        private class ActivateSound
        {
            string _path;
            public ActivateSound(string path)
            {
                if (!File.Exists(path))
                    return;
             
                _path = path;
            }
            public void Play()
            {
                using (var audioFile = new AudioFileReader(_path))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();

                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                        Thread.Sleep(1000);
                    
                }
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
        
        
        
        
        public async void Start()
        {
            await Task.Run(() => { 

                if (_isStarted)
                    return;
                else
                    _isStarted = true;

                var grammarBuilder = new GrammarBuilder();
                grammarBuilder.Append(new Choices("Can you find in Google", "Stop recognition", "open program", "Help"));
                var grammar = new Grammar(grammarBuilder);
                recognizer.LoadGrammar(grammar);
                recognizer.SetInputToDefaultAudioDevice();
                recognizer.SpeechRecognized += RecognizerCommand_SpeechRecognized;
                recognizer.RecognizeAsync(RecognizeMode.Multiple);

            
                DefaultRecognizerQuery();

                synth.Speak($"Hi {nameUser}, I'm your voice assistant. If you need help for use me just say help");

                activateSound.Play();
            
            });
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

        public void SetConfiguration(XmlDocument config)
        {

            activateSound = new(Directory.GetCurrentDirectory() + "\\Sounds\\Activate Voice.mp3");

            openCommands = new();
            try
            {
                XmlNode settingsNode = config.SelectSingleNode("/Settings");
                int voiceGender = int.Parse(settingsNode.SelectSingleNode("VoiceGender").InnerText);
                int voiceAge = int.Parse(settingsNode.SelectSingleNode("VoiceAge").InnerText);
                int voiceSpeed = int.Parse(settingsNode.SelectSingleNode("VoiceSpeed").InnerText);

                ChangeSpeachConfiguration((VoiceGender)voiceGender, (VoiceAge)voiceAge, voiceSpeed);

                
                

                XmlNodeList openCommandsTMP = config.SelectNodes("/Settings/OpenCommand");
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
                activateSound.Play();
                command = SearchInGoogle;
                recognizerQuery.RecognizeAsync(RecognizeMode.Multiple);
            }
            else if (e.Result.Text == "open program")
            {
                recognizer.RecognizeAsyncCancel();
                synth.Speak("Yes, what do you want to open");
                activateSound.Play();
                command = OpenProgram;
                AddChoicesForQuery();
                recognizerQuery.RecognizeAsync(RecognizeMode.Multiple);
            }
            
            else if (e.Result.Text == "Stop recognition")
            {
                synth.Speak("OK, bye!");
                Environment.Exit(0);
            }
            else if (e.Result.Text == "Help")
            {
                synth.Speak(File.ReadAllText(Directory.GetCurrentDirectory() + "\\read me.txt"));
            } 
        }

        public XmlDocument GetXmlConfig()
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

            return xmlDoc;
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
                    {
                        Process.Start(program.Path);
                        synth.Speak("Program opend");
                    }
                    else
                        synth.Speak("Didn't find the program");

                    break;
                }
            }
            DefaultRecognizerQuery();
        }

        private void SearchInGoogle(string query)
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
