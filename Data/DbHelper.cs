using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace Library_Management_system.Data
{
    public class DbHelper
    {
        private readonly IConfiguration _configuration;

        public DbHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection GetConnection()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            return new SqlConnection(connectionString);
        }

        public List<T> ExecuteReader<T>(string procName, Dictionary<string, object>? parameters = null)
            where T : new()
        {
            DataTable dt = ExecuteStoredProcedure(procName, parameters);
            List<T> list = new();

            foreach (DataRow row in dt.Rows)
            {
                T obj = new T();

                foreach (PropertyInfo prop in typeof(T).GetProperties())
                {
                    if (dt.Columns.Contains(prop.Name) && row[prop.Name] != DBNull.Value)
                    {
                        prop.SetValue(obj, row[prop.Name]);
                    }
                }

                list.Add(obj);
            }

            return list;
        }

        public DataTable ExecuteStoredProcedure(string procName, Dictionary<string, object>? parameters = null)
        {
            using SqlConnection con = GetConnection();
            using SqlCommand cmd = new SqlCommand(procName, con)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue("@" + param.Key, param.Value ?? DBNull.Value);
                }
            }

            using SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public int ExecuteNonQuery(string procName, Dictionary<string, object>? parameters = null)
        {
            using SqlConnection con = GetConnection();
            using SqlCommand cmd = new SqlCommand(procName, con)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue("@" + param.Key, param.Value ?? DBNull.Value);
                }
            }

            con.Open();
            return cmd.ExecuteNonQuery();
        }

        public string ExecuteNonQueryWithOutput(string procName, Dictionary<string, object>? parameters,
            string outputParamName, SqlDbType outputType, int size = 200)
        {
            using SqlConnection con = GetConnection();
            using SqlCommand cmd = new SqlCommand(procName, con)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue("@" + param.Key, param.Value ?? DBNull.Value);
                }
            }

            SqlParameter outputParam = new SqlParameter("@" + outputParamName, outputType, size)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outputParam);

            con.Open();
            cmd.ExecuteNonQuery();

            return outputParam.Value?.ToString() ?? string.Empty;
        }

        // New method for scalar operations
        public T ExecuteScalar<T>(string procName, Dictionary<string, object>? parameters = null)
        {
            using SqlConnection con = GetConnection();
            using SqlCommand cmd = new SqlCommand(procName, con)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue("@" + param.Key, param.Value ?? DBNull.Value);
                }
            }

            con.Open();
            var result = cmd.ExecuteScalar();

            if (result == DBNull.Value || result == null)
                return default(T);

            return (T)Convert.ChangeType(result, typeof(T));
        }
    }
}