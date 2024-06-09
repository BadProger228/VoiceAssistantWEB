using Catharsis.Commons;
using KursovWork;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Synthesis;
using Testing_for_WEB.Models;
using static KursovWork.VoiceAssistant;
using System.Data.SqlTypes;
using System.Xml;

namespace Testing_for_WEB.Controllers
{
    public class AllPageConfiguration
    {
        public List<OpenCommand> ProgramList { get; set; }
        public int Speed { get; set; }
        public VoiceAge Age { get; set; }
        public VoiceGender Gender { get; set; }
        public bool IsPhone { get; set; } = false;
        public string? pathToCSS { get; set; }
        
    }
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private VoiceAssistant _voiceAssistant;
        private ServerConnect _serverConnect;
        private AllPageConfiguration AllPageConfiguration { get; set; }
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, VoiceAssistant voiceAssistant)
        {
            _logger = logger;
            _configuration = configuration;
            _voiceAssistant = voiceAssistant;
            _serverConnect = new();
        }
        public IActionResult Register(string Username, string Password, string ConfirmPassword)
        {
            if (Password != ConfirmPassword)
                return NoContent();

            _serverConnect.AddUser(Username, Password, SetDefoultValueForVoiceAssistant());
            _voiceAssistant.SetConfiguration(_serverConnect.SignIn(Username, Password));
            _voiceAssistant.nameUser = Username;
            _voiceAssistant.Start();

            return RedirectToAction("main");
        }
        private XmlDocument SetDefoultValueForVoiceAssistant()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlElement rootElement = xmlDoc.CreateElement("Settings");
            xmlDoc.AppendChild(rootElement);

            XmlElement voiceSpeedElement = xmlDoc.CreateElement("VoiceSpeed");
            voiceSpeedElement.InnerText = "0";
            rootElement.AppendChild(voiceSpeedElement);

            XmlElement voiceGenderElement = xmlDoc.CreateElement("VoiceGender");
            voiceGenderElement.InnerText = ((int)VoiceGender.NotSet).ToString();
            rootElement.AppendChild(voiceGenderElement);

            XmlElement voiceAgeElement = xmlDoc.CreateElement("VoiceAge");
            voiceAgeElement.InnerText = ((int)VoiceAge.NotSet).ToString();
            rootElement.AppendChild(voiceAgeElement);
            
            return xmlDoc;
        }
        public AllPageConfiguration DataWebSite()
        {
            VoiceGender voiceGender = new();
            VoiceAge voiceAge = new();
            int voiceSpeed;
            _voiceAssistant.GetStartConfiguration(out voiceGender, out voiceAge, out voiceSpeed);


            var viewModel = new AllPageConfiguration
            {
                ProgramList = _voiceAssistant.openCommands,
                Speed = voiceSpeed,
                Age = voiceAge,
                Gender = voiceGender
            };

            return viewModel;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult AddProgramForm()
        {
            return View(DataWebSite());
        }
        public IActionResult Settings()
        {
            return View(DataWebSite()); 
        }
        public IActionResult main()
        {
            return View(DataWebSite());
        }
        public IActionResult RegisterForm()
        {
            return View(DataWebSite());
        }

        public IActionResult RedirectToPage(string button)
        {
            if (button == "settings")
            {
                return RedirectToAction("Settings");
            }
            else if (button == "Add programs")
            {
                return RedirectToAction("AddProgramForm");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }
        public IActionResult AddProgram(string NameProgram, string pathToProgram)
        {
            foreach (var item in _voiceAssistant.openCommands)
                if(item.FileName == NameProgram)
                    return NoContent();

            if (_voiceAssistant.SetOpenCommand(NameProgram, pathToProgram))
            {
                _serverConnect.SaveConfig(_voiceAssistant.nameUser, _voiceAssistant.GetXmlConfig());
                return NoContent();
            }
            return NoContent();
        }
        public IActionResult DeleteProgram(string NameProgram) {
            foreach(var command in _voiceAssistant.openCommands)
            {
                if (command.FileName == NameProgram)
                {
                    _voiceAssistant.openCommands.Remove(command);
                    return NoContent();
                }
            }
            _serverConnect.SaveConfig(_voiceAssistant.nameUser, _voiceAssistant.GetXmlConfig());
            return NoContent();
        }
        public IActionResult Testing(string text)
        {
            _voiceAssistant.TestSpeach(text);
            return NoContent();
        }
        public IActionResult ChangeVoiceConf(int Speed, string Age, string Gender) {
            _voiceAssistant.ChangeSpeachConfiguration((VoiceGender)Enum.Parse(typeof(VoiceGender), Gender), (VoiceAge)Enum.Parse(typeof(VoiceAge), Age), Speed);
            _serverConnect.SaveConfig(_voiceAssistant.nameUser, _voiceAssistant.GetXmlConfig());
            return NoContent();
        }
        public IActionResult Login(string Login, string Password)
        {
            _voiceAssistant.SetConfiguration(_serverConnect.SignIn(Login, Password));
            _voiceAssistant.nameUser = Login;
            _voiceAssistant.Start();
            return RedirectToAction("main");


        }
        public IActionResult Privacy()
        {

            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult YourAction()
        {
            List<OpenCommand> programList = _voiceAssistant?.openCommands ?? new List<OpenCommand>();
            return View(programList);
        }
    }
}
