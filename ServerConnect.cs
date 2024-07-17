using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Text;
namespace Testing_for_WEB
{
    public partial class ServerConnect
    {
        public string connectingString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\LogicMan\\source\\repos\\Server\\Users.mdf;Integrated Security=True";
        SqlConnection _connection;
        public ServerConnect() {
            _connection = new(connectingString);
            _connection.Open(); 
        }
       

        public bool AddUser(string login, string password, SqlXml sqlXml)
        {
            password = HashPassword(password);
            string query = "INSERT INTO Users (Login, Password, XmlConfig) VALUES (@login, @password, @xmlConfig)";
            
            try
            {
                using (SqlCommand command = new(query, _connection))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);
                    command.Parameters.AddWithValue("@xmlConfig", sqlXml);

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
        public SqlXml SingIn(string Login, string Password)
        {
            SqlXml config = new();
            string query = $"SELECT * FROM Users Where Login = {Login}";

            using (SqlCommand command = new(query, _connection))
            {
                SqlDataReader reader = command.ExecuteReader();
                reader.Read();
                if ((string)reader["password"] == HashPassword(Password))
                {
                    config = (SqlXml)reader["xmlConfig"];

                }
            }
            return config;



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
