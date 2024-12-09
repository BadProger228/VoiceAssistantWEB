using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.SqlTypes;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
namespace Testing_for_WEB
{
    public partial class ServerConnect
    {
        public string _connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\LogicMan\\source\\repos\\Server\\Users.mdf;Integrated Security=True";
        SqlConnection _connection;
        public ServerConnect() {
            _connection = new(_connectionString);
            _connection.Open(); 
        }
       

        public bool AddUser(string login, string password, XmlDocument config)
        {
            password = HashPassword(password);
            string query = "INSERT INTO Users (Login, Password, XmlConfig) VALUES (@login, @password, @xmlConfig)";
            
            try
            {
                using (SqlCommand command = new(query, _connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);
                    command.Parameters.AddWithValue("@xmlConfig", ConvertToSqlXml(config));

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (SqlException ex)
            {
                // Логирование ошибки SQL
                Console.WriteLine("SQL Error: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Логирование других ошибок
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
        }
        public XmlDocument SignIn(string Login, string Password)
        {
            XmlDocument config = null;
            string query = "SELECT xmlConfig FROM Users WHERE Login = @Login AND Password = @Password";

            try
            {
                using (SqlCommand command = new SqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@Login", Login);
                    command.Parameters.AddWithValue("@Password", HashPassword(Password));

                    // Получение xmlConfig как SqlXml
                    SqlDataReader reader = command.ExecuteReader();
                    reader.Read();

                    if (!reader.IsDBNull(reader.GetOrdinal("xmlConfig")))
                    {
                        SqlXml xmlValue = reader.GetSqlXml(reader.GetOrdinal("xmlConfig"));
                        
                        config = new XmlDocument();
                        config.LoadXml(xmlValue.Value);
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            

            return config;
        }

        public bool SaveConfig(string Login, XmlDocument config)
        {
            string updateQuery = "UPDATE Users SET xmlConfig = @xmlConfig WHERE Login = @login";

            using (SqlCommand command = new SqlCommand(updateQuery, _connection))
            {
                // Параметр @login
                command.Parameters.AddWithValue("@login", Login);

                // Конвертация XmlDocument в SqlXml
                SqlXml sqlXml = ConvertToSqlXml(config);

                // Параметр @xmlConfig
                command.Parameters.Add("@xmlConfig", SqlDbType.Xml).Value = sqlXml;

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }
        private SqlXml ConvertToSqlXml(XmlDocument xmlDocument)
        {
            // Преобразование объекта XmlDocument в строку XML
            string xmlString = xmlDocument.OuterXml;

            // Создание объекта SqlXml из строки XML
            return new SqlXml(new XmlTextReader(xmlString, XmlNodeType.Document, null));
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new();

                foreach (var _byte in bytes)
                    builder.Append(_byte.ToString("x2"));
                
                return builder.ToString();
            }
        }
        ~ServerConnect()
        {
            _connection.Close();
        }

    }
}
