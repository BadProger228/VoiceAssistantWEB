# VoiceAssistantWEB

## Description
**VoiceAssistantWEB** is a web-based voice assistant that allows users to register on the website and log in using their credentials. User data is stored in an XML file within the database. The assistant can open user-defined programs via the command **"open program"** and supports customizable voice output.

Additionally, users can trigger a Google search by saying **"can you find in Google"** followed by their query.

## Features
- User registration and authentication.
- User data storage in an XML database.
- Ability to add and open programs via voice commands.
- Customizable voice output settings.
- Google search functionality via voice command.

## Technologies Used
- **Backend:** C#, ASP.NET
- **Frontend:** JavaScript, HTML, CSS
- **Data Storage:** XML, SQL

## Libraries Used
- **HtmlAgilityPack** – for web scraping.
- **Selenium (OpenQA.Selenium, OpenQA.Selenium.Chrome)** – for browser automation.
- **System.Speech.Recognition, System.Speech.Synthesis** – for speech recognition and synthesis.
- **NAudio.Wave** – for audio processing.
- **System.Media** – for media playback.
- **System.Xml, System.Xml.Linq** – for XML processing.
- **Microsoft.AspNetCore.Routing.Constraints** – for routing constraints in ASP.NET.

## How to Run
1. Install the required dependencies (.NET, ASP.NET, and necessary NuGet packages).
2. Configure the database and ensure XML data storage is properly set up.
3. Open the project in **Visual Studio**.
4. Build and run the application.
5. Access the web interface via the provided localhost URL.



