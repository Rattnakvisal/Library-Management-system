using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace Library_Management_system.Data
{
    public class DbHelper
    {
        private readonly string _connectionString;

        public DbHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public SqlConnection GetConnection() => new SqlConnection(_connectionString);

        // -------------------------
        // Reader -> List<T>
        // -------------------------
        public List<T> ExecuteReader<T>(string procName, Dictionary<string, object?>? parameters = null)
            where T : new()
        {
            var dt = ExecuteStoredProcedure(procName, parameters);
            var list = new List<T>();

            var props = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToList();

            foreach (DataRow row in dt.Rows)
            {
                var obj = new T();

                foreach (var prop in props)
                {
                    if (!dt.Columns.Contains(prop.Name)) continue;

                    var value = row[prop.Name];
                    if (value == DBNull.Value) continue;

                    // handle Nullable<T>
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                    try
                    {
                        var safeValue = Convert.ChangeType(value, targetType);
                        prop.SetValue(obj, safeValue);
                    }
                    catch
                    {
                        // if conversion fails, skip or throw (your choice)
                        // throw new InvalidOperationException($"Cannot map column '{prop.Name}' to '{targetType.Name}'.");
                    }
                }

                list.Add(obj);
            }

            return list;
        }

        // -------------------------
        // Stored Procedure -> DataTable
        // -------------------------
        public DataTable ExecuteStoredProcedure(string procName, Dictionary<string, object?>? parameters = null)
        {
            using var con = GetConnection();
            using var cmd = new SqlCommand(procName, con) { CommandType = CommandType.StoredProcedure };

            AddParameters(cmd, parameters);

            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        // -------------------------
        // NonQuery
        // -------------------------
        public int ExecuteNonQuery(string procName, Dictionary<string, object?>? parameters = null)
        {
            using var con = GetConnection();
            using var cmd = new SqlCommand(procName, con) { CommandType = CommandType.StoredProcedure };

            AddParameters(cmd, parameters);

            con.Open();
            return cmd.ExecuteNonQuery();
        }

        // -------------------------
        // Output Param
        // -------------------------
        public string ExecuteNonQueryWithOutput(
            string procName,
            Dictionary<string, object?>? parameters,
            string outputParamName,
            SqlDbType outputType,
            int size = 200)
        {
            using var con = GetConnection();
            using var cmd = new SqlCommand(procName, con) { CommandType = CommandType.StoredProcedure };

            AddParameters(cmd, parameters);

            var outputParam = new SqlParameter("@" + outputParamName, outputType)
            {
                Direction = ParameterDirection.Output,
                Size = size
            };
            cmd.Parameters.Add(outputParam);

            con.Open();
            cmd.ExecuteNonQuery();

            return outputParam.Value?.ToString() ?? string.Empty;
        }

        // -------------------------
        // Scalar
        // -------------------------
        public T? ExecuteScalar<T>(string procName, Dictionary<string, object?>? parameters = null)
        {
            using var con = GetConnection();
            using var cmd = new SqlCommand(procName, con) { CommandType = CommandType.StoredProcedure };

            AddParameters(cmd, parameters);

            con.Open();
            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value) return default;

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(result, targetType);
        }

        // -------------------------
        // Helper: safer parameters (avoid AddWithValue)
        // -------------------------
        private static void AddParameters(SqlCommand cmd, Dictionary<string, object?>? parameters)
        {
            if (parameters == null) return;

            foreach (var (key, value) in parameters)
            {
                // DBNull.Value for null
                var p = cmd.Parameters.Add("@" + key, SqlDbType.Variant);
                p.Value = value ?? DBNull.Value;
            }
        }
    }
}